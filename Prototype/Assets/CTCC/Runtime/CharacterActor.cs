using System;
using System.Collections.Generic;
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

        private Transform neckBone;
        private Transform headBone;
        private Transform eyeLeft;
        private Transform eyeRight;
        private Transform jawBone;
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
            public int browInnerUp = -1, browOuterUp = -1, cheekRaise = -1;
        }

        private readonly List<FaceBinding> faceBindings = new();
        private static readonly Dictionary<string, LipSyncTrack> TrackCache = new(StringComparer.OrdinalIgnoreCase);

        private LipSyncTrack activeTrack;
        private string activeLineId;
        private float speechFallbackTime;
        private float forcedSpeechTime = -1f;

        private float blinkCooldown = 2.6f;
        private float blinkPhase = -1f;
        private float targetWarm, targetConcern, targetSorrow;
        private float curWarm, curConcern, curSorrow;
        private readonly float[] curVisemes = new float[12]; // PP,FF,TH,CH,AA,EE,IH,OH,OO,UH,MM,JawOpen
        private float curBrowAccent;
        private readonly float[] sampleBuffer = new float[256];

        private Renderer[] allRenderers = Array.Empty<Renderer>();
        private MaterialPropertyBlock mpb;
        private float apparitionAlpha = 1f;
        private static readonly int AlphaId = Shader.PropertyToID("_Alpha");

        public string ActiveActionName { get; private set; } = "";
        public float ApparitionAlpha => apparitionAlpha;

        private void Awake()
        {
            CacheBonesAndShapes();
            BuildPlayableGraph();
            if (heldClipboard != null)
                heldClipboard.SetActive(false);
            if (isApparition)
                SetApparitionAlpha(0f);
        }

        private void OnDestroy()
        {
            if (graph.IsValid()) graph.Destroy();
        }

        private void CacheBonesAndShapes()
        {
            allRenderers = GetComponentsInChildren<Renderer>(true);
            mpb = new MaterialPropertyBlock();

            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                string n = t.name;
                if (n == "mixamorig:Head" || n == "Head") headBone = t;
                else if (n == "mixamorig:Neck" || n == "Neck") neckBone = t;
                else if (n == "mixamorig:LeftEye" || n == "Eye_L") { eyeLeft = t; eyeLeftRestLocal = t.localRotation; }
                else if (n == "mixamorig:RightEye" || n == "Eye_R") { eyeRight = t; eyeRightRestLocal = t.localRotation; }
                else if (n == "mixamorig:Jaw" || n == "Jaw") jawBone = t;
            }

            faceBindings.Clear();
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
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
                    browOuterUp = mesh.GetBlendShapeIndex("Brow_OuterUp"),
                    cheekRaise = mesh.GetBlendShapeIndex("Cheek_Raise"),
                };
                faceBindings.Add(b);
            }
        }

        private void BuildPlayableGraph()
        {
            var animator = GetComponentInChildren<Animator>(true);
            if (animator == null) animator = gameObject.AddComponent<Animator>();
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
            speechFallbackTime = 0f;
            forcedSpeechTime = -1f;
            activeTrack = LoadLipSyncTrack(lineId);
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
        }

        private static LipSyncTrack LoadLipSyncTrack(string lineId)
        {
            if (string.IsNullOrEmpty(lineId)) return null;
            if (TrackCache.TryGetValue(lineId, out var cached)) return cached;
            var ta = Resources.Load<TextAsset>("CTCC/LipSync/" + lineId);
            LipSyncTrack parsed = null;
            if (ta != null && !string.IsNullOrEmpty(ta.text))
            {
                try { parsed = JsonUtility.FromJson<LipSyncTrack>(ta.text); }
                catch { parsed = null; }
            }
            TrackCache[lineId] = parsed;
            return parsed;
        }

        public void SetHeldClipboardVisible(bool visible)
        {
            if (heldClipboard != null)
                heldClipboard.SetActive(visible);
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
            apparitionAlpha = Mathf.Clamp01(alpha);
            if (mpb == null) mpb = new MaterialPropertyBlock();
            mpb.SetFloat(AlphaId, apparitionAlpha);
            for (int i = 0; i < allRenderers.Length; i++)
            {
                if (allRenderers[i] == null) continue;
                allRenderers[i].SetPropertyBlock(mpb);
                allRenderers[i].enabled = apparitionAlpha > 0.01f;
            }
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            UpdateMixerFade(dt);
            UpdateFacialAnimation(dt);
        }

        private void LateUpdate()
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
                if (fadeTimer >= fadeDuration) previousSlot = -1;
            }
            else
            {
                for (int i = 0; i < clips.Length; i++)
                    mixer.SetInputWeight(i, i == activeSlot ? 1f : 0f);
            }
        }

        private void UpdateFacialAnimation(float dt)
        {
            if (faceBindings.Count == 0) return;

            // 1. Emotional facial base (smoothly layered with Duchenne cheek raise on genuine warm smile!)
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
            float tBrow = 0f, tNod = 0f;

            bool forced = enableLipSync && forcedSpeechTime >= 0f && activeTrack != null && activeTrack.frames != null && activeTrack.frames.Length > 0;
            bool speaking = forced || (enableLipSync && voiceSource != null && voiceSource.isPlaying);
            if (speaking && activeTrack != null && activeTrack.frames != null && activeTrack.frames.Length > 0)
            {
                float playT = forced ? forcedSpeechTime : voiceSource.time;
                if (!forced && playT <= 0.0001f && speechFallbackTime > 0f) playT = speechFallbackTime;
                if (!forced) speechFallbackTime += dt;

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
            float[] targets = { tPP, tFF, tTH, tCH, tAA, tEE, tIH, tOH, tOO, tUH, tMM, tJaw };
            for (int i = 0; i < curVisemes.Length; i++)
                curVisemes[i] = Mathf.MoveTowards(curVisemes[i], targets[i], vRate);

            curBrowAccent = Mathf.MoveTowards(curBrowAccent, tBrow, dt * 160f);
            currentNodDeg = Mathf.MoveTowards(currentNodDeg, tNod, dt * 22f);
            currentJawRad = Mathf.MoveTowards(currentJawRad, (curVisemes[11] / 100f) * 0.065f, dt * 2.0f);

            // Push synced facial weights to ALL LOD face SkinnedMeshRenderers (LOD0 + LOD1)!
            for (int i = 0; i < faceBindings.Count; i++)
            {
                var b = faceBindings[i];
                if (b.smr == null) continue;
                SetShape(b.smr, b.warm, curWarm);
                SetShape(b.smr, b.cheekRaise, curWarm * 0.45f);
                SetShape(b.smr, b.concern, curConcern);
                SetShape(b.smr, b.sorrow, curSorrow);
                SetShape(b.smr, b.browInnerUp, Mathf.Clamp(curBrowAccent + curSorrow * 0.35f, 0f, 100f));
                SetShape(b.smr, b.browOuterUp, curBrowAccent * 0.65f);

                SetShape(b.smr, b.blinkL, blinkWeight);
                SetShape(b.smr, b.blinkR, blinkWeight);

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
        }

        private static void SetShape(SkinnedMeshRenderer smr, int idx, float weight)
        {
            if (idx >= 0) smr.SetBlendShapeWeight(idx, weight);
        }
    }
}
