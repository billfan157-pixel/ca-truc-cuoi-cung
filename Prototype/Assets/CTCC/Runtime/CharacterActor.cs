using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace CTCC
{
    [Serializable]
    public sealed class LipSyncFrame
    {
        public float t;
        public float pp, ff, th, ch, aa, ee, ih, oh, oo, uh, mm;
        public float jaw, brow, nod, breath;
    }

    [Serializable]
    public sealed class LipSyncTrack
    {
        public string lineId;
        public string speaker;
        public string text;
        public float duration;
        public float fps;
        public LipSyncFrame[] frames;
    }

    /// <summary>
    /// High-fidelity runtime controller for Blender 4.5 Digital Human characters (Lan, Bé An, Gatekeeper).
    /// Drives PlayableGraph clip crossfades, eye-lead head/neck look-at with natural micro-saccades,
    /// blink-on-gaze-shift, LOD-synced 29-blendshape facial expressions, offline 30 Hz Vietnamese phonetic
    /// lip-sync (with jaw articulation & phrase accents), held props, and soft memory apparition alpha.
    /// </summary>
    public sealed class CharacterActor : MonoBehaviour
    {
        public string characterId;
        public AnimationClip[] clips = Array.Empty<AnimationClip>();
        public AudioSource voiceSource;
        public GameObject heldClipboard;
        public bool enableLookAt = true;
        public bool enableBlink = true;
        public bool enableLipSync = true;
        public bool isApparition;

        [NonSerialized] public Transform LookTarget;
        [NonSerialized] public bool ReducedMotion;

        private PlayableGraph graph;
        private AnimationMixerPlayable mixer;
        private readonly Dictionary<string, int> clipIndex = new(StringComparer.OrdinalIgnoreCase);
        private AnimationClipPlayable[] clipPlayables;
        private int activeSlot = -1;
        private int previousSlot = -1;
        private float fadeTimer;
        private float fadeDuration = 0.25f;
        private bool mixerWeightsDirty;

        private Transform neckBone;
        private Transform headBone;
        private Transform eyeLeft;
        private Transform eyeRight;
        private Transform jawBone;
        private Transform rightArm, rightForeArm, rightHand, clipboardSocket;
        private Transform leftArm, leftForeArm, leftHand;
        private Transform leftFoot, rightFoot, leftToe, rightToe;
        private float propIkWeight;
        private Quaternion unmodifiedNeckRotation, appliedNeckRotation;
        private Quaternion unmodifiedHeadRotation, appliedHeadRotation;
        private Quaternion unmodifiedJawRotation, appliedJawRotation;
        private bool hasLookRotation;
        private bool hasJawRotation;
        private Quaternion eyeLeftRestLocal;
        private Quaternion eyeRightRestLocal;
        private float headYaw, headPitch;
        private float eyeYaw, eyePitch;
        private float saccadeYaw, saccadePitch, saccadeTimer = 1.2f;
        private float currentJawRad;
        private float currentNodDeg;

        private sealed class FaceBinding
        {
            public SkinnedMeshRenderer smr;
            public int blinkL = -1, blinkR = -1;
            public int vPP = -1, vFF = -1, vTH = -1, vCH = -1;
            public int vAA = -1, vEE = -1, vIH = -1, vOH = -1, vOO = -1, vUH = -1, vMM = -1;
            public int jawOpen = -1;
            public int warm = -1, concern = -1, sorrow = -1;
            public int browInnerUp = -1, browOuterUpL = -1, browOuterUpR = -1;
            public int smileL = -1, smileR = -1;
            public bool wasVisible;
        }

        private readonly List<FaceBinding> faceBindings = new();
        private static readonly Dictionary<string, LipSyncTrack> TrackCache = new(StringComparer.OrdinalIgnoreCase);

        private LipSyncTrack activeTrack;
        private string activeLineId;
        private float forcedSpeechTime = -1f;
        private bool speechPaused;

        private float blinkCooldown = 2.6f;
        private float blinkPhase = -1f;
        private float targetWarm, targetConcern, targetSorrow;
        private float curWarm, curConcern, curSorrow;
        private readonly float[] curVisemes = new float[12]; // PP,FF,TH,CH,AA,EE,IH,OH,OO,UH,MM,JawOpen
        private readonly float[] visemeTargets = new float[12];
        private bool poseDirty = true;
        private float currentAsymmetry;
        private float currentBlink;
        private static readonly ProfilerMarker FaceMarker = new("CTCC.Character.Face");
        private static readonly ProfilerMarker GazeMarker = new("CTCC.Character.Gaze");
        internal bool MeasureAnimationAllocations;
        internal long AnimationAllocationBytes { get; private set; }
        internal int MeasuredAnimationFrames { get; private set; }
        private float curBrowAccent;
        private readonly float[] sampleBuffer = new float[256];

        private Renderer[] allRenderers = Array.Empty<Renderer>();
        private Material[] apparitionMaterials = Array.Empty<Material>();
        private Animator rigAnimator;
        private float apparitionAlpha = 1f;
        private static readonly int AlphaId = Shader.PropertyToID("_Alpha");

        public string ActiveActionName { get; private set; } = "";
        public float ApparitionAlpha => apparitionAlpha;
        internal string ActiveLineId => activeLineId;

        private void Awake()
        {
            CacheBonesAndShapes();
            BuildPlayableGraph();
            if (heldClipboard != null)
                heldClipboard.SetActive(false);
            if (isApparition)
            {
                BindApparitionMaterials();
                SetApparitionAlpha(0f);
            }
        }

        private void OnDestroy()
        {
            if (graph.IsValid()) graph.Destroy();
            foreach (var material in apparitionMaterials)
                Destroy(material);
        }

        private void OnEnable()
        {
            poseDirty = true;
            if (graph.IsValid()) graph.Play();
        }

        private void OnDisable()
        {
            if (graph.IsValid()) graph.Stop();
            if (hasLookRotation)
            {
                if (headBone != null && Quaternion.Angle(headBone.localRotation, appliedHeadRotation) < 0.1f)
                    headBone.localRotation = unmodifiedHeadRotation;
                if (neckBone != null && Quaternion.Angle(neckBone.localRotation, appliedNeckRotation) < 0.1f)
                    neckBone.localRotation = unmodifiedNeckRotation;
                if (eyeLeft != null) eyeLeft.localRotation = eyeLeftRestLocal;
                if (eyeRight != null) eyeRight.localRotation = eyeRightRestLocal;
            }
            if (hasJawRotation && jawBone != null && Quaternion.Angle(jawBone.localRotation, appliedJawRotation) < 0.1f)
                jawBone.localRotation = unmodifiedJawRotation;
            hasLookRotation = hasJawRotation = false;
            EndSpeech();
            Array.Clear(curVisemes, 0, curVisemes.Length);
            currentJawRad = currentNodDeg = currentAsymmetry = 0f;
        }

        private void CacheBonesAndShapes()
        {
            allRenderers = GetComponentsInChildren<Renderer>(true);

            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                string n = t.name;
                if (n == "mixamorig:Head" || n == "Head") headBone = t;
                else if (n == "mixamorig:Neck" || n == "Neck") neckBone = t;
                else if (n == "mixamorig:LeftEye" || n == "Eye_L") { eyeLeft = t; eyeLeftRestLocal = t.localRotation; }
                else if (n == "mixamorig:RightEye" || n == "Eye_R") { eyeRight = t; eyeRightRestLocal = t.localRotation; }
                else if (n == "mixamorig:Jaw" || n == "Jaw") jawBone = t;
                else if (n == "mixamorig:RightArm") rightArm = t;
                else if (n == "mixamorig:RightForeArm") rightForeArm = t;
                else if (n == "mixamorig:RightHand") rightHand = t;
                else if (n == "ClipboardSocket_R") clipboardSocket = t;
                else if (n == "mixamorig:LeftArm") leftArm = t;
                else if (n == "mixamorig:LeftForeArm") leftForeArm = t;
                else if (n == "mixamorig:LeftHand") leftHand = t;
                else if (n == "mixamorig:LeftFoot") leftFoot = t;
                else if (n == "mixamorig:RightFoot") rightFoot = t;
                else if (n == "mixamorig:LeftToeBase") leftToe = t;
                else if (n == "mixamorig:RightToeBase") rightToe = t;
            }

            faceBindings.Clear();
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                // The imported animation bounds remain active even when Unity skips skinning.
                smr.updateWhenOffscreen = false;
                var mesh = smr.sharedMesh;
                if (mesh == null || mesh.blendShapeCount <= 0) continue;
                var b = new FaceBinding
                {
                    smr = smr,
                    blinkL = mesh.GetBlendShapeIndex("Blink_L"),
                    blinkR = mesh.GetBlendShapeIndex("Blink_R"),
                    vPP = mesh.GetBlendShapeIndex("Viseme_PP"),
                    vFF = mesh.GetBlendShapeIndex("Viseme_FF"),
                    vTH = mesh.GetBlendShapeIndex("Viseme_TH"),
                    vCH = mesh.GetBlendShapeIndex("Viseme_CH"),
                    vAA = mesh.GetBlendShapeIndex("Viseme_AA"),
                    vEE = mesh.GetBlendShapeIndex("Viseme_EE"),
                    vIH = mesh.GetBlendShapeIndex("Viseme_IH"),
                    vOH = mesh.GetBlendShapeIndex("Viseme_OH"),
                    vOO = mesh.GetBlendShapeIndex("Viseme_OO"),
                    vUH = mesh.GetBlendShapeIndex("Viseme_UH"),
                    vMM = mesh.GetBlendShapeIndex("Viseme_MM"),
                    jawOpen = mesh.GetBlendShapeIndex("Jaw_Open"),
                    warm = mesh.GetBlendShapeIndex("Mouth_Warm"),
                    concern = mesh.GetBlendShapeIndex("Brow_Concern"),
                    sorrow = mesh.GetBlendShapeIndex("Mouth_Sorrow"),
                    browInnerUp = mesh.GetBlendShapeIndex("Brow_InnerUp"),
                    browOuterUpL = mesh.GetBlendShapeIndex("Brow_OuterUp_L"),
                    browOuterUpR = mesh.GetBlendShapeIndex("Brow_OuterUp_R"),
                    smileL = mesh.GetBlendShapeIndex("Mouth_Smile_L"),
                    smileR = mesh.GetBlendShapeIndex("Mouth_Smile_R"),
                };
                faceBindings.Add(b);
            }
        }

        private void BuildPlayableGraph()
        {
            // Generic FBX rigs imported with avatarSetup=0 carry no Animator on the FBX root,
            // and their AnimationClip paths (e.g. "Lan_Armature/mixamorig:Root/...") are relative
            // to the FBX root child rather than the outer CharacterActor holder.
            Transform animRoot = transform;
            for (int c = 0; c < transform.childCount; c++)
            {
                var child = transform.GetChild(c);
                for (int g = 0; g < child.childCount; g++)
                {
                    if (child.GetChild(g).name.IndexOf("Armature", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        animRoot = child;
                        break;
                    }
                }
                if (animRoot != transform) break;
            }
            var animator = animRoot.GetComponent<Animator>();
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (animator == null) animator = animRoot.gameObject.AddComponent<Animator>();
            rigAnimator = animator;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            if (clips == null || clips.Length == 0) return;

            graph = PlayableGraph.Create("CTCC_Actor_" + name);
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            mixer = AnimationMixerPlayable.Create(graph, clips.Length);
            clipPlayables = new AnimationClipPlayable[clips.Length];
            var output = AnimationPlayableOutput.Create(graph, "Anim", animator);
            output.SetSourcePlayable(mixer);

            for (int i = 0; i < clips.Length; i++)
            {
                var c = clips[i];
                if (c == null) continue;
                var p = AnimationClipPlayable.Create(graph, c);
                p.SetApplyFootIK(false);
                p.SetApplyPlayableIK(false);
                graph.Connect(p, 0, mixer, i);
                mixer.SetInputWeight(i, i == 0 ? 1f : 0f);
                clipPlayables[i] = p;
                clipIndex[c.name] = i;
                int bar = c.name.LastIndexOf('|');
                if (bar >= 0 && bar + 1 < c.name.Length)
                    clipIndex[c.name.Substring(bar + 1)] = i;
            }

            if (clips.Length > 0 && clips[0] != null)
            {
                activeSlot = 0;
                ActiveActionName = clips[0].name;
            }
            graph.Play();
        }

        public void BeginSpeech(string lineId, AudioSource source)
        {
            voiceSource = source;
            activeLineId = lineId;
            forcedSpeechTime = -1f;
            speechPaused = false;
            activeTrack = LoadLipSyncTrack(lineId);
            if (activeTrack != null && source != null && source.clip != null &&
                Mathf.Abs(source.clip.length - activeTrack.duration) > 1f / activeTrack.fps)
                activeTrack = null;
        }

        public void ForceSpeechTime(string lineId, float timeSec)
        {
            if (activeLineId != lineId)
            {
                activeLineId = lineId;
                activeTrack = LoadLipSyncTrack(lineId);
            }
            forcedSpeechTime = timeSec;
        }

        public void EndSpeech()
        {
            voiceSource = null;
            activeLineId = null;
            activeTrack = null;
            forcedSpeechTime = -1f;
            speechPaused = false;
        }

        internal void SetSpeechPaused(bool paused) => speechPaused = paused;

        internal static LipSyncTrack LoadLipSyncTrack(string lineId)
        {
            if (string.IsNullOrEmpty(lineId)) return null;
            if (TrackCache.TryGetValue(lineId, out var cached)) return cached;
            var ta = Resources.Load<TextAsset>("CTCC/LipSync/" + lineId);
            LipSyncTrack parsed = null;
            if (ta != null && !string.IsNullOrEmpty(ta.text))
            {
                try
                {
                    parsed = JsonUtility.FromJson<LipSyncTrack>(ta.text);
                    if (!ValidLipSyncTrack(parsed, lineId)) parsed = null;
                }
                catch (ArgumentException) { parsed = null; }
            }
            TrackCache[lineId] = parsed;
            return parsed;
        }

        internal static bool ValidLipSyncTrack(LipSyncTrack track, string lineId)
        {
            if (track == null || track.lineId != lineId || string.IsNullOrWhiteSpace(track.text) ||
                track.fps != 30f || !float.IsFinite(track.duration) || track.duration <= 0f ||
                track.frames == null || track.frames.Length < 2 ||
                Mathf.Abs((track.frames.Length - 1) / track.fps - track.duration) > 1f / track.fps)
                return false;
            for (int i = 0; i < track.frames.Length; i++)
            {
                var f = track.frames[i];
                if (f == null || !float.IsFinite(f.t) || Mathf.Abs(f.t - i / track.fps) > 0.001f ||
                    !Unit(f.pp) || !Unit(f.ff) || !Unit(f.th) || !Unit(f.ch) || !Unit(f.aa) ||
                    !Unit(f.ee) || !Unit(f.ih) || !Unit(f.oh) || !Unit(f.oo) || !Unit(f.uh) ||
                    !Unit(f.mm) || !Unit(f.jaw) || !Unit(f.brow) || !Unit(f.nod) || !Unit(f.breath))
                    return false;
            }
            return true;
        }

        private static bool Unit(float x) => x >= 0f && x <= 1f;

        public void SetHeldClipboardVisible(bool visible)
        {
            if (heldClipboard != null)
            {
                if (visible)
                {
                    // Orient the printed F101 paper face upward toward Minh with metal clip at top
                    heldClipboard.transform.localPosition = new Vector3(0.02f, 0.07f, -0.02f);
                    heldClipboard.transform.localRotation = Quaternion.Euler(52f, 0f, -12f) * Quaternion.Euler(0f, 0f, 180f);
                }
                heldClipboard.SetActive(visible);
            }
        }

        public void SetActionTime(float seconds)
        {
            if (!graph.IsValid() || activeSlot < 0 || activeSlot >= clipPlayables.Length) return;
            previousSlot = -1;
            for (int i = 0; i < clips.Length; i++)
                mixer.SetInputWeight(i, i == activeSlot ? 1f : 0f);
            mixerWeightsDirty = false;
            var prevCull = rigAnimator != null ? rigAnimator.cullingMode : AnimatorCullingMode.CullUpdateTransforms;
            if (rigAnimator != null) rigAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            clipPlayables[activeSlot].SetTime(seconds);
            graph.Evaluate(0f);
            if (rigAnimator != null) rigAnimator.cullingMode = prevCull;
        }

        public void ForceOpenEyes()
        {
            blinkPhase = -1f;
            blinkCooldown = 3.5f;
            currentBlink = 0f;
            poseDirty = true;
            ApplyFaceWeights();
        }

        public void PlayAction(string actionName, float crossfade = 0.28f, bool restart = false)
        {
            if (!graph.IsValid() || string.IsNullOrEmpty(actionName)) return;
            int found = -1;
            if (!clipIndex.TryGetValue(actionName, out found))
            {
                found = -1;
                foreach (var kv in clipIndex)
                {
                    if (kv.Key.IndexOf(actionName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        found = kv.Value;
                        break;
                    }
                }
            }
            if (found < 0) return;
            if (found == activeSlot && !restart) return;

            previousSlot = activeSlot;
            activeSlot = found;
            ActiveActionName = actionName;
            fadeDuration = Mathf.Max(0.02f, ReducedMotion ? 0.04f : crossfade);
            fadeTimer = 0f;
            poseDirty = true;
            mixerWeightsDirty = true;
            if (restart)
                clipPlayables[found].SetTime(0d);
        }

        public void SetExpression(float warm, float concern, float sorrow)
        {
            targetWarm = Mathf.Clamp01(warm) * 100f;
            targetConcern = Mathf.Clamp01(concern) * 100f;
            targetSorrow = Mathf.Clamp01(sorrow) * 100f;
        }

        public void SetApparitionAlpha(float alpha)
        {
            float next = Mathf.Clamp01(alpha);
            if (next == apparitionAlpha) return;
            if (apparitionAlpha <= 0.01f && next > 0.01f) poseDirty = true;
            apparitionAlpha = next;
            if (!isApparition) return;
            foreach (var material in apparitionMaterials)
                material.SetFloat(AlphaId, apparitionAlpha);
            for (int i = 0; i < allRenderers.Length; i++)
            {
                if (allRenderers[i] == null) continue;
                bool visible = apparitionAlpha > 0.01f;
                if (allRenderers[i].enabled != visible) allRenderers[i].enabled = visible;
            }
        }

        private void BindApparitionMaterials()
        {
            // One owned material per surface across all LODs keeps the fade actor-local and batchable.
            var instances = new Dictionary<Material, Material>();
            foreach (var renderer in allRenderers)
            {
                var slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    var source = slots[i];
                    if (source == null || !source.HasProperty(AlphaId)) continue;
                    if (!instances.TryGetValue(source, out var instance))
                    {
                        instance = new Material(source);
                        // Front-to-back apparition queues pair with ApparitionDepthPrepass (ZWrite On) +
                        // ApparitionHumanForward (ZTest Equal) so outer dress/hair/skin occlude inner limbs & mouth!
                        string mn = source.name;
                        if (mn.IndexOf("Cotton", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            mn.IndexOf("Clothes", StringComparison.OrdinalIgnoreCase) >= 0)
                            instance.renderQueue = 3001;
                        else if (mn.IndexOf("Hair", StringComparison.OrdinalIgnoreCase) >= 0)
                            instance.renderQueue = 3002;
                        else if (mn.IndexOf("Skin", StringComparison.OrdinalIgnoreCase) >= 0)
                            instance.renderQueue = 3003;
                        else if (mn.IndexOf("Eye", StringComparison.OrdinalIgnoreCase) >= 0)
                            instance.renderQueue = 3004;
                        else if (mn.IndexOf("Teeth", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 mn.IndexOf("Tongue", StringComparison.OrdinalIgnoreCase) >= 0)
                            instance.renderQueue = 3005;
                        else
                            instance.renderQueue = 3006;
                        instances.Add(source, instance);
                    }
                    slots[i] = instance;
                }
                renderer.sharedMaterials = slots;
            }
            apparitionMaterials = new Material[instances.Count];
            instances.Values.CopyTo(apparitionMaterials, 0);
        }

        private void Update()
        {
            long before = MeasureAnimationAllocations ? GC.GetAllocatedBytesForCurrentThread() : 0;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            UpdateMixerFade(dt);
            using (FaceMarker.Auto()) UpdateFacialAnimation(dt);
            if (MeasureAnimationAllocations)
            {
                AnimationAllocationBytes += GC.GetAllocatedBytesForCurrentThread() - before;
                MeasuredAnimationFrames++;
            }
        }

        private void LateUpdate()
        {
            long before = MeasureAnimationAllocations ? GC.GetAllocatedBytesForCurrentThread() : 0;
            using (FaceMarker.Auto()) ApplyFaceWeights();
            if (HasVisibleSkin())
            {
                using (GazeMarker.Auto())
                {
                    ApplyGazeAndJaw();
                    ApplyCharacterPropIK(Mathf.Min(Time.unscaledDeltaTime, 0.05f));
                }
            }
            if (MeasureAnimationAllocations)
                AnimationAllocationBytes += GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private void ApplyCharacterPropIK(float dt)
        {
            bool handoverActive = heldClipboard != null && heldClipboard.activeSelf &&
                ActiveActionName.IndexOf("Handover", StringComparison.OrdinalIgnoreCase) >= 0;
            float targetIk = handoverActive ? 1f : 0f;
            propIkWeight = Mathf.MoveTowards(propIkWeight, targetIk, dt * 4.5f);
            if (propIkWeight <= 0.001f || rightArm == null || rightForeArm == null || rightHand == null)
                return;

            // Present F101 clipboard naturally at counter / chest height with relaxed downward elbows
            Vector3 fwd = transform.forward;
            Vector3 right = transform.right;
            Vector3 up = transform.up;
            Vector3 rGoal = transform.position + up * 1.11f + fwd * 0.37f + right * 0.09f;
            Vector3 rElbowHint = transform.position + up * 0.92f - fwd * 0.06f + right * 0.28f;
            SolveTwoBoneArmIK(rightArm, rightForeArm, rightHand, rGoal, rElbowHint, propIkWeight * 0.60f);

            if (leftArm != null && leftForeArm != null && leftHand != null)
            {
                Vector3 lGoal = transform.position + up * 1.07f + fwd * 0.34f - right * 0.12f;
                Vector3 lElbowHint = transform.position + up * 0.91f - fwd * 0.06f - right * 0.28f;
                SolveTwoBoneArmIK(leftArm, leftForeArm, leftHand, lGoal, lElbowHint, propIkWeight * 0.38f);
            }
        }

        private static void SolveTwoBoneArmIK(Transform upper, Transform lower, Transform hand,
            Vector3 targetPos, Vector3 elbowPole, float weight)
        {
            if (weight <= 0.001f) return;
            Quaternion uOrig = upper.rotation;
            Quaternion lOrig = lower.rotation;

            Vector3 pA = upper.position;
            Vector3 pB = lower.position;
            Vector3 pC = hand.position;
            float lenAB = Vector3.Distance(pA, pB);
            float lenBC = Vector3.Distance(pB, pC);
            if (lenAB < 0.01f || lenBC < 0.01f) return;

            Vector3 toTarget = targetPos - pA;
            float dist = Mathf.Clamp(toTarget.magnitude, Mathf.Abs(lenAB - lenBC) + 0.01f, (lenAB + lenBC) * 0.995f);
            Vector3 dirAT = toTarget.sqrMagnitude > 1e-6f ? toTarget.normalized : Vector3.forward;

            // Law of cosines in the (shoulder -> target, elbowPole) plane; pick bend direction closest to elbowPole
            float cosA = Mathf.Clamp((lenAB * lenAB + dist * dist - lenBC * lenBC) / (2f * lenAB * dist), -1f, 1f);
            float angleA = Mathf.Acos(cosA) * Mathf.Rad2Deg;
            Vector3 poleVec = elbowPole - pA;
            Vector3 bendAxis = Vector3.Cross(dirAT, poleVec);
            if (bendAxis.sqrMagnitude < 1e-5f) bendAxis = Vector3.right;
            else bendAxis.Normalize();

            Vector3 candPos = pA + Quaternion.AngleAxis(angleA, bendAxis) * dirAT * lenAB;
            Vector3 candNeg = pA + Quaternion.AngleAxis(-angleA, bendAxis) * dirAT * lenAB;
            Vector3 desiredB = (candPos - elbowPole).sqrMagnitude <= (candNeg - elbowPole).sqrMagnitude ? candPos : candNeg;

            Vector3 curAB = (lower.position - pA).normalized;
            Vector3 desAB = (desiredB - pA).normalized;
            if (curAB.sqrMagnitude > 1e-6f && desAB.sqrMagnitude > 1e-6f)
                upper.rotation = Quaternion.FromToRotation(curAB, desAB) * upper.rotation;

            Vector3 curBC = (hand.position - lower.position).normalized;
            Vector3 desBC = (targetPos - lower.position).normalized;
            if (curBC.sqrMagnitude > 1e-6f && desBC.sqrMagnitude > 1e-6f)
                lower.rotation = Quaternion.FromToRotation(curBC, desBC) * lower.rotation;

            if (weight < 0.999f)
            {
                upper.rotation = Quaternion.Slerp(uOrig, upper.rotation, weight);
                lower.rotation = Quaternion.Slerp(lOrig, lower.rotation, weight);
            }
        }

        internal void MeasureStanceSlip(string clipName, float worldSpeedMps,
            out float medianSlipMps, out float p95SlipMps, out float shoeSoleMedianSlipMps)
        {
            bool wasActive = gameObject.activeSelf;
            if (!wasActive) gameObject.SetActive(true);
            AnimatorCullingMode prevCull = AnimatorCullingMode.CullUpdateTransforms;
            bool wasPlaying = false;
            try
            {
                if (!graph.IsValid())
                {
                    CacheBonesAndShapes();
                    BuildPlayableGraph();
                }
                if (rigAnimator != null)
                {
                    prevCull = rigAnimator.cullingMode;
                    rigAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }
                PlayAction(clipName, 0.001f, restart: true);
                wasPlaying = graph.IsValid() && graph.IsPlaying();
                AnimationClip clip = (activeSlot >= 0 && activeSlot < clips.Length) ? clips[activeSlot] : null;
                const float fps = 30f;
                const int frames = 61;
                var slips = new List<float>(64);
                var soleSlips = new List<float>(64);
                Vector3 prevL = Vector3.zero, prevR = Vector3.zero;
                Vector3 prevSoleL = Vector3.zero, prevSoleR = Vector3.zero;
                bool hasPrev = false;

                Transform tL = leftToe != null ? leftToe : leftFoot;
                Transform tR = rightToe != null ? rightToe : rightFoot;
                Transform fL = leftFoot != null ? leftFoot : tL;
                Transform fR = rightFoot != null ? rightFoot : tR;

                for (int f = 0; f < frames; f++)
                {
                    SetActionTime(f / fps);
                    Vector3 pL = transform.InverseTransformPoint(tL.position);
                    Vector3 pR = transform.InverseTransformPoint(tR.position);
                    // Shoe sole contact point right below mid-foot/toe (y = 0 in character local space)
                    Vector3 sL = Vector3.Lerp(transform.InverseTransformPoint(fL.position), pL, 0.65f);
                    Vector3 sR = Vector3.Lerp(transform.InverseTransformPoint(fR.position), pR, 0.65f);

                    if (hasPrev)
                    {
                        float minCurY = Mathf.Min(pL.y, pR.y);
                        float minPrevY = Mathf.Min(prevL.y, prevR.y);
                        // In Unity local space, character faces +Z, so planted foot sweeps backward along -Z at -worldSpeedMps!
                        if (pL.y <= minCurY + 0.005f && prevL.y <= minPrevY + 0.005f && Mathf.Abs(pL.y - prevL.y) * fps <= 0.08f)
                        {
                            float backSpeed = -(pL.z - prevL.z) * fps;
                            slips.Add(Mathf.Abs(backSpeed - worldSpeedMps));
                            float soleBackSpeed = -(sL.z - prevSoleL.z) * fps;
                            soleSlips.Add(Mathf.Abs(soleBackSpeed - worldSpeedMps));
                        }
                        if (pR.y <= minCurY + 0.005f && prevR.y <= minPrevY + 0.005f && Mathf.Abs(pR.y - prevR.y) * fps <= 0.08f)
                        {
                            float backSpeed = -(pR.z - prevR.z) * fps;
                            slips.Add(Mathf.Abs(backSpeed - worldSpeedMps));
                            float soleBackSpeed = -(sR.z - prevSoleR.z) * fps;
                            soleSlips.Add(Mathf.Abs(soleBackSpeed - worldSpeedMps));
                        }
                    }
                    prevL = pL;
                    prevR = pR;
                    prevSoleL = sL;
                    prevSoleR = sR;
                    hasPrev = true;
                }
                slips.Sort();
                soleSlips.Sort();
                medianSlipMps = slips.Count > 0 ? slips[slips.Count / 2] : 99f;
                p95SlipMps = slips.Count > 0 ? slips[Mathf.Clamp(Mathf.RoundToInt((slips.Count - 1) * 0.95f), 0, slips.Count - 1)] : 99f;
                shoeSoleMedianSlipMps = soleSlips.Count > 0 ? soleSlips[soleSlips.Count / 2] : medianSlipMps;
                Debug.Log(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "CTCC_STRIDE_CHECK {0} animRoot={1} clip={2} samples={3} med={4:F4} p95={5:F4} sole={6:F4}",
                    characterId, rigAnimator != null ? rigAnimator.name : "null",
                    clip != null ? clip.name : "null", slips.Count, medianSlipMps, p95SlipMps, shoeSoleMedianSlipMps));
            }
            finally
            {
                if (wasPlaying && graph.IsValid()) graph.Play();
                if (rigAnimator != null) rigAnimator.cullingMode = prevCull;
                if (!wasActive) gameObject.SetActive(false);
            }
        }

        private bool HasVisibleSkin()
        {
            for (int i = 0; i < faceBindings.Count; i++)
                if (faceBindings[i].smr != null && faceBindings[i].smr.isVisible)
                    return true;
            return false;
        }

        private void ApplyGazeAndJaw()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);

            // 1. Eye-lead Head/Neck Gaze with organic micro-saccades (prevents static mannequin stare!)
            if (enableLookAt && headBone != null)
            {
                Vector3 toTarget = LookTarget == null ? Vector3.zero : LookTarget.position - headBone.position;
                Vector3 localDir = transform.InverseTransformDirection(toTarget.normalized);
                float goalYaw = 0f;
                float goalPitch = 0f;
                bool hasValidTarget = toTarget.sqrMagnitude >= 0.04f && toTarget.sqrMagnitude <= 225f && localDir.z >= 0.12f;

                if (hasValidTarget)
                {
                    goalYaw = Mathf.Clamp(Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg, -36f, 36f);
                    goalPitch = Mathf.Clamp(-Mathf.Asin(Mathf.Clamp(localDir.y, -0.95f, 0.95f)) * Mathf.Rad2Deg, -14f, 16f);

                    // Subtle living human micro-saccades between left eye / right eye / bridge of player's gaze
                    saccadeTimer -= dt;
                    if (saccadeTimer <= 0f)
                    {
                        float seed = Time.unscaledTime * 3.17f + transform.position.x;
                        saccadeYaw = ReducedMotion ? 0f : Mathf.Sin(seed * 2.3f) * 1.4f;
                        saccadePitch = ReducedMotion ? 0f : Mathf.Cos(seed * 1.9f) * 0.9f;
                        saccadeTimer = 0.85f + (Mathf.Sin(seed * 1.13f) * 0.5f + 0.5f) * 1.45f;
                    }
                    // Natural blink triggered when making a large sudden gaze shift (>18 deg)
                    if (Mathf.Abs(goalYaw - headYaw) > 18f && blinkPhase < 0f && enableBlink)
                        blinkPhase = 0f;
                }
                else
                {
                    saccadeYaw = 0f;
                    saccadePitch = 0f;
                }

                // Eyes lead swiftly (220 deg/s); neck + head follow smoothly (68 deg/s)
                float eyeSpeed = ReducedMotion ? 260f : 220f;
                float headSpeed = ReducedMotion ? 150f : 68f;
                eyeYaw = Mathf.MoveTowards(eyeYaw, goalYaw + saccadeYaw, dt * eyeSpeed);
                eyePitch = Mathf.MoveTowards(eyePitch, goalPitch + saccadePitch, dt * eyeSpeed);
                headYaw = Mathf.MoveTowards(headYaw, goalYaw, dt * headSpeed);
                headPitch = Mathf.MoveTowards(headPitch, goalPitch + currentNodDeg, dt * headSpeed);

                // Distribute gaze across Neck (32%) + Head (68%) for natural cervical posture!
                if (neckBone != null && neckBone.parent != null)
                {
                    Quaternion curN = neckBone.localRotation;
                    if (!hasLookRotation || Mathf.Abs(Quaternion.Dot(curN, appliedNeckRotation)) < 0.999999f)
                        unmodifiedNeckRotation = curN;
                    Quaternion offN = Quaternion.AngleAxis(headYaw * 0.24f, transform.up) *
                        Quaternion.AngleAxis(headPitch * 0.20f, transform.right);
                    Quaternion pN = neckBone.parent.rotation;
                    appliedNeckRotation = Quaternion.Inverse(pN) * offN * pN * unmodifiedNeckRotation;
                    neckBone.localRotation = appliedNeckRotation;
                }

                if (headBone.parent != null)
                {
                    Quaternion curH = headBone.localRotation;
                    if (!hasLookRotation || Mathf.Abs(Quaternion.Dot(curH, appliedHeadRotation)) < 0.999999f)
                        unmodifiedHeadRotation = curH;
                    Quaternion offH = Quaternion.AngleAxis(headYaw * 0.48f, transform.up) *
                        Quaternion.AngleAxis(headPitch * 0.42f, transform.right);
                    Quaternion pH = headBone.parent.rotation;
                    appliedHeadRotation = Quaternion.Inverse(pH) * offH * pH * unmodifiedHeadRotation;
                    headBone.localRotation = appliedHeadRotation;
                }
                hasLookRotation = true;

                // Eyes take the remaining difference + micro-saccade lead cleanly!
                float remYaw = Mathf.Clamp(eyeYaw - headYaw * 0.68f, -18f, 18f);
                float remPitch = Mathf.Clamp(eyePitch - headPitch * 0.60f, -11f, 11f);
                Quaternion eyeOff = Quaternion.Euler(remPitch, remYaw, 0f);
                if (eyeLeft != null) eyeLeft.localRotation = eyeLeftRestLocal * eyeOff;
                if (eyeRight != null) eyeRight.localRotation = eyeRightRestLocal * eyeOff;
            }

            // 2. Subtle articulated lower-jaw pitch coupled to phonetic lip-sync (moves lower teeth & tongue!)
            if (jawBone != null && jawBone.parent != null && enableLipSync)
            {
                Quaternion curJ = jawBone.localRotation;
                if (!hasJawRotation || Mathf.Abs(Quaternion.Dot(curJ, appliedJawRotation)) < 0.999999f)
                    unmodifiedJawRotation = curJ;
                Quaternion jawOff = Quaternion.AngleAxis(currentJawRad * Mathf.Rad2Deg, transform.right);
                Quaternion pJ = jawBone.parent.rotation;
                appliedJawRotation = Quaternion.Inverse(pJ) * jawOff * pJ * unmodifiedJawRotation;
                jawBone.localRotation = appliedJawRotation;
                hasJawRotation = true;
            }
        }

        private void UpdateMixerFade(float dt)
        {
            if (!graph.IsValid() || activeSlot < 0) return;
            if (previousSlot >= 0 && previousSlot != activeSlot)
            {
                fadeTimer += dt;
                float t = Mathf.Clamp01(fadeTimer / fadeDuration);
                t = t * t * (3f - 2f * t);
                for (int i = 0; i < clips.Length; i++)
                {
                    if (i == activeSlot) mixer.SetInputWeight(i, t);
                    else if (i == previousSlot) mixer.SetInputWeight(i, 1f - t);
                    else mixer.SetInputWeight(i, 0f);
                }
                if (fadeTimer >= fadeDuration)
                {
                    previousSlot = -1;
                    mixerWeightsDirty = false;
                }
            }
            else if (mixerWeightsDirty)
            {
                for (int i = 0; i < clips.Length; i++)
                    mixer.SetInputWeight(i, i == activeSlot ? 1f : 0f);
                mixerWeightsDirty = false;
            }
        }

        private void UpdateFacialAnimation(float dt)
        {
            if (faceBindings.Count == 0) return;

            // Smooth the expression base independently of the speaker's visibility.
            curWarm = Mathf.MoveTowards(curWarm, targetWarm, dt * 115f);
            curConcern = Mathf.MoveTowards(curConcern, targetConcern, dt * 115f);
            curSorrow = Mathf.MoveTowards(curSorrow, targetSorrow, dt * 115f);

            // 2. Natural non-rhythmic blinking (3.0 - 5.6 s cadence; quick close, softer open!)
            float blinkWeight = 0f;
            if (enableBlink)
            {
                if (blinkPhase >= 0f)
                {
                    blinkPhase += dt * (blinkPhase < 0.42f ? 9.5f : 6.0f);
                    blinkWeight = blinkPhase < 1f ? Mathf.Sin(blinkPhase * Mathf.PI) * 100f : 0f;
                    if (blinkPhase >= 1f)
                    {
                        blinkPhase = -1f;
                        blinkCooldown = 2.9f + (Mathf.Sin(Time.unscaledTime * 1.73f + transform.position.z) * 0.5f + 0.5f) * 2.7f;
                    }
                }
                else
                {
                    blinkCooldown -= dt;
                    if (blinkCooldown <= 0f) blinkPhase = 0f;
                }
            }

            // 3. Offline 30 Hz Vietnamese Phonetic Lip-Sync (with graceful acoustic fallback if track absent)
            float tPP = 0f, tFF = 0f, tTH = 0f, tCH = 0f;
            float tAA = 0f, tEE = 0f, tIH = 0f, tOH = 0f, tOO = 0f, tUH = 0f, tMM = 0f, tJaw = 0f;
            float tBrow = 0f, tNod = 0f, asymmetry = 0f;

            bool forced = enableLipSync && forcedSpeechTime >= 0f && activeTrack != null && activeTrack.frames != null && activeTrack.frames.Length > 0;
            bool speaking = forced || (enableLipSync && voiceSource != null &&
                (voiceSource.isPlaying || (speechPaused && activeTrack != null)));
            if (speaking && activeTrack != null && activeTrack.frames != null && activeTrack.frames.Length > 0)
            {
                AudioClip clip = forced ? null : voiceSource.clip;
                float playT = forced ? forcedSpeechTime :
                    clip != null ? voiceSource.timeSamples / (float)clip.frequency : 0f;

                float fps = activeTrack.fps > 1f ? activeTrack.fps : 30f;
                float fPos = Mathf.Clamp(playT * fps, 0f, activeTrack.frames.Length - 1);
                int i0 = Mathf.FloorToInt(fPos);
                int i1 = Mathf.Min(i0 + 1, activeTrack.frames.Length - 1);
                float a = fPos - i0;
                var f0 = activeTrack.frames[i0];
                var f1 = activeTrack.frames[i1];

                // Calm, natural conversational speech articulation (gentle realistic Vietnamese lip aperture, never shouting!)
                tPP = Mathf.Lerp(f0.pp, f1.pp, a) * 68f;
                tFF = Mathf.Lerp(f0.ff, f1.ff, a) * 62f;
                tTH = Mathf.Lerp(f0.th, f1.th, a) * 56f;
                tCH = Mathf.Lerp(f0.ch, f1.ch, a) * 56f;
                tAA = Mathf.Lerp(f0.aa, f1.aa, a) * 52f;
                tEE = Mathf.Lerp(f0.ee, f1.ee, a) * 58f;
                tIH = Mathf.Lerp(f0.ih, f1.ih, a) * 54f;
                tOH = Mathf.Lerp(f0.oh, f1.oh, a) * 52f;
                tOO = Mathf.Lerp(f0.oo, f1.oo, a) * 54f;
                tUH = Mathf.Lerp(f0.uh, f1.uh, a) * 50f;
                tMM = Mathf.Lerp(f0.mm, f1.mm, a) * 55f;
                tJaw = Mathf.Lerp(f0.jaw, f1.jaw, a) * 44f;
                tBrow = Mathf.Lerp(f0.brow, f1.brow, a) * 42f;
                tNod = ReducedMotion ? 0f : Mathf.Lerp(f0.nod, f1.nod, a) * 3.2f;
                ApplyReceptionAccent(activeLineId, playT, ref tBrow, ref tNod, out asymmetry);
                // A bilabial closure overrides the jaw aperture left by adjacent vowels.
                tJaw *= 1f - Mathf.Max(tPP / 68f, tMM / 55f) * 0.86f;
            }
            else if (speaking)
            {
                // Smooth RMS/ZCR fallback only when an unmapped clip plays without a JSON alignment track
                voiceSource.GetOutputData(sampleBuffer, 0);
                float sumSq = 0f;
                int zcr = 0;
                for (int i = 1; i < sampleBuffer.Length; i++)
                {
                    float s = sampleBuffer[i];
                    sumSq += s * s;
                    if ((s >= 0f) != (sampleBuffer[i - 1] >= 0f)) zcr++;
                }
                float rms = Mathf.Sqrt(sumSq / sampleBuffer.Length);
                float env = Mathf.Clamp01((rms - 0.008f) * 7.5f);
                float bright = Mathf.Clamp01((zcr / (float)sampleBuffer.Length - 0.04f) * 4.2f);
                if (env > 0.06f)
                {
                    tAA = env * (1f - bright * 0.45f) * 78f;
                    tEE = env * bright * 64f;
                    tOH = env * (1f - bright) * 50f;
                    tJaw = env * 62f;
                }
                else
                {
                    tMM = 18f;
                }
            }

            float vRate = dt * 780f;
            visemeTargets[0] = tPP;
            visemeTargets[1] = tFF;
            visemeTargets[2] = tTH;
            visemeTargets[3] = tCH;
            visemeTargets[4] = tAA;
            visemeTargets[5] = tEE;
            visemeTargets[6] = tIH;
            visemeTargets[7] = tOH;
            visemeTargets[8] = tOO;
            visemeTargets[9] = tUH;
            visemeTargets[10] = tMM;
            visemeTargets[11] = tJaw;
            for (int i = 0; i < curVisemes.Length; i++)
                curVisemes[i] = Mathf.MoveTowards(curVisemes[i], visemeTargets[i], vRate);

            curBrowAccent = Mathf.MoveTowards(curBrowAccent, tBrow, dt * 160f);
            currentAsymmetry = Mathf.MoveTowards(currentAsymmetry, asymmetry, dt * 55f);
            currentNodDeg = Mathf.MoveTowards(currentNodDeg, tNod, dt * 22f);
            currentJawRad = Mathf.MoveTowards(currentJawRad, (curVisemes[11] / 100f) * 0.065f, dt * 2.0f);
            currentBlink = blinkWeight;
        }

        private void ApplyFaceWeights()
        {
            // Synchronize each LOD as soon as Unity reports it visible. Prime all tiers on activation.
            for (int i = 0; i < faceBindings.Count; i++)
            {
                var b = faceBindings[i];
                if (b.smr == null) continue;
                bool visible = b.smr.isVisible;
                bool write = visible || b.wasVisible || poseDirty;
                b.wasVisible = visible;
                if (!write) continue;
                SetShape(b.smr, b.warm, curWarm);
                SetShape(b.smr, b.smileL, Mathf.Max(0f, currentAsymmetry));
                SetShape(b.smr, b.smileR, Mathf.Max(0f, -currentAsymmetry));
                SetShape(b.smr, b.concern, curConcern);
                SetShape(b.smr, b.sorrow, curSorrow);
                SetShape(b.smr, b.browInnerUp, Mathf.Clamp(curBrowAccent + curSorrow * 0.35f, 0f, 100f));
                SetShape(b.smr, b.browOuterUpL, curBrowAccent * 0.56f);
                SetShape(b.smr, b.browOuterUpR, curBrowAccent * 0.44f);

                SetShape(b.smr, b.blinkL, currentBlink);
                SetShape(b.smr, b.blinkR, currentBlink);

                SetShape(b.smr, b.vPP, curVisemes[0]);
                SetShape(b.smr, b.vFF, curVisemes[1]);
                SetShape(b.smr, b.vTH, curVisemes[2]);
                SetShape(b.smr, b.vCH, curVisemes[3]);
                SetShape(b.smr, b.vAA, curVisemes[4]);
                SetShape(b.smr, b.vEE, curVisemes[5]);
                SetShape(b.smr, b.vIH, curVisemes[6]);
                SetShape(b.smr, b.vOH, curVisemes[7]);
                SetShape(b.smr, b.vOO, curVisemes[8]);
                SetShape(b.smr, b.vUH, curVisemes[9]);
                SetShape(b.smr, b.vMM, curVisemes[10]);
                SetShape(b.smr, b.jawOpen, curVisemes[11] * 0.55f);
            }
            poseDirty = false;
        }

        private void ApplyReceptionAccent(string line, float time, ref float brow, ref float nod, out float asymmetry)
        {
            asymmetry = 0f;
            // Authored emotional beats aligned to VieNeu v3 Turbo voice timestamps.
            switch (line)
            {
                case "CH01-D03":
                    float chide = Accent(time, 0.14f, 0.62f, 1.45f);
                    brow += 9f * chide;
                    asymmetry = 6f * chide;
                    break;
                case "CH01-D05":
                    float redirect = Accent(time, 0.85f, 1.85f, 3.10f);
                    brow += 13f * redirect;
                    if (!ReducedMotion) nod -= 1.6f * redirect;
                    asymmetry = -5f * redirect;
                    break;
                case "CH01-D07":
                    float reluctance = Accent(time, 0.45f, 1.40f, 2.85f);
                    brow *= 1f - 0.35f * reluctance;
                    if (!ReducedMotion) nod += 2.4f * reluctance;
                    break;
                case "CH01-D08":
                    float gentleExplain = Accent(time, 0.35f, 1.25f, 2.60f);
                    brow += 8f * gentleExplain;
                    if (!ReducedMotion) nod += 1.4f * gentleExplain;
                    break;
                case "CH01-D09":
                    float quietCare = Accent(time, 0.30f, 1.10f, 2.20f);
                    brow += 7f * quietCare;
                    asymmetry = 4f * quietCare;
                    break;
                case "CH01-D16":
                    float urgency = Accent(time, 0.25f, 1.05f, 2.30f);
                    brow += 14f * urgency;
                    if (!ReducedMotion) nod -= 1.8f * urgency;
                    break;
                case "CH02-D03":
                    float childQuestion = Accent(time, 0.40f, 1.20f, 2.10f);
                    brow += 12f * childQuestion;
                    if (!ReducedMotion) nod -= 1.5f * childQuestion;
                    break;
                case "CH02-D05":
                case "CH03-D05":
                    float sternWarning = Accent(time, 0.15f, 0.75f, 1.60f);
                    brow += 11f * sternWarning;
                    if (!ReducedMotion) nod += 1.8f * sternWarning;
                    break;
            }
        }

        private static float Accent(float time, float start, float peak, float end) =>
            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, peak, time)) *
            (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(peak, end, time)));

        private static void SetShape(SkinnedMeshRenderer smr, int idx, float weight)
        {
            if (idx >= 0 && Mathf.Abs(smr.GetBlendShapeWeight(idx) - weight) > 0.05f)
                smr.SetBlendShapeWeight(idx, weight);
        }
    }
}
