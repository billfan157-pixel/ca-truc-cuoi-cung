using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace CTCC
{
    /// <summary>
    /// Runtime controller for Blender 4.5 rigged/blendshaped characters (Lan, Gatekeeper, Bé An).
    /// Drives smooth PlayableGraph clip crossfades, anatomically clamped eye/head look-at,
    /// organic eye blinking, emotional facial expressions, and live VO audio-amplitude lip-sync.
    /// </summary>
    public sealed class CharacterActor : MonoBehaviour
    {
        public string characterId;
        public AnimationClip[] clips = Array.Empty<AnimationClip>();
        public AudioSource voiceSource;
        public bool enableLookAt = true;
        public bool enableBlink = true;
        public bool enableLipSync = true;
        public bool isApparition;

        [NonSerialized] public Transform LookTarget;
        [NonSerialized] public bool ReducedMotion;

        private PlayableGraph graph;
        private AnimationMixerPlayable mixer;
        private readonly Dictionary<string, int> clipIndex = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<AnimationClipPlayable> clipPlayables = new();
        private int activeSlot = -1;
        private int previousSlot = -1;
        private float fadeTimer;
        private float fadeDuration = 0.25f;

        private Transform headBone;
        private Transform neckBone;
        private Transform eyeLeft;
        private Transform eyeRight;
        private Quaternion headRestLocal;
        private Quaternion eyeLeftRestLocal;
        private Quaternion eyeRightRestLocal;
        private float smoothedYaw;
        private float smoothedPitch;

        private SkinnedMeshRenderer faceRenderer;
        private int idxBlinkL = -1, idxBlinkR = -1;
        private int idxAA = -1, idxOH = -1, idxEE = -1, idxMM = -1;
        private int idxWarm = -1, idxConcern = -1, idxSorrow = -1;

        private float blinkCooldown = 2.8f;
        private float blinkPhase = -1f;
        private float targetWarm, targetConcern, targetSorrow;
        private float curWarm, curConcern, curSorrow;
        private float lipAA, lipOH, lipEE, lipMM;
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
                if (t.name == "Head") { headBone = t; headRestLocal = t.localRotation; }
                else if (t.name == "Neck") neckBone = t;
                else if (t.name == "Eye_L") { eyeLeft = t; eyeLeftRestLocal = t.localRotation; }
                else if (t.name == "Eye_R") { eyeRight = t; eyeRightRestLocal = t.localRotation; }
            }

            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = smr.sharedMesh;
                if (mesh == null || mesh.blendShapeCount <= 0) continue;
                faceRenderer = smr;
                idxBlinkL = mesh.GetBlendShapeIndex("Blink_L");
                idxBlinkR = mesh.GetBlendShapeIndex("Blink_R");
                idxAA = mesh.GetBlendShapeIndex("Viseme_AA");
                idxOH = mesh.GetBlendShapeIndex("Viseme_OH");
                idxEE = mesh.GetBlendShapeIndex("Viseme_EE");
                idxMM = mesh.GetBlendShapeIndex("Viseme_MM");
                idxWarm = mesh.GetBlendShapeIndex("Mouth_Warm");
                idxConcern = mesh.GetBlendShapeIndex("Brow_Concern");
                idxSorrow = mesh.GetBlendShapeIndex("Mouth_Sorrow");
                break;
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
                clipPlayables.Add(p);
                clipIndex[c.name] = i;
                // Also register short suffix after '|' (Blender FBX exports "Armature|ActionName")
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

        public void PlayAction(string actionName, float crossfade = 0.28f, bool restart = false)
        {
            if (!graph.IsValid() || string.IsNullOrEmpty(actionName)) return;
            int found = -1;
            if (!clipIndex.TryGetValue(actionName, out found))
            {
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
            if (restart && found < clipPlayables.Count)
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
            if (!enableLookAt || headBone == null || LookTarget == null) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            Vector3 toTarget = LookTarget.position - headBone.position;
            if (toTarget.sqrMagnitude < 0.04f || toTarget.sqrMagnitude > 196f)
            {
                smoothedYaw = Mathf.MoveTowards(smoothedYaw, 0f, dt * 45f);
                smoothedPitch = Mathf.MoveTowards(smoothedPitch, 0f, dt * 35f);
            }
            else
            {
                Vector3 localDir = transform.InverseTransformDirection(toTarget.normalized);
                float yaw = Mathf.Clamp(Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg, -34f, 34f);
                float pitch = Mathf.Clamp(-Mathf.Asin(Mathf.Clamp(localDir.y, -0.95f, 0.95f)) * Mathf.Rad2Deg, -14f, 16f);
                float speed = ReducedMotion ? 160f : 72f;
                smoothedYaw = Mathf.MoveTowards(smoothedYaw, yaw, dt * speed);
                smoothedPitch = Mathf.MoveTowards(smoothedPitch, pitch, dt * speed);
            }
            // Apply subtle natural head turn atop animation pose without snapping neck
            headBone.localRotation = headBone.localRotation * Quaternion.Euler(smoothedPitch * 0.55f, smoothedYaw * 0.65f, 0f);
            if (eyeLeft != null)
                eyeLeft.localRotation = eyeLeftRestLocal * Quaternion.Euler(smoothedPitch * 0.35f, smoothedYaw * 0.30f, 0f);
            if (eyeRight != null)
                eyeRight.localRotation = eyeRightRestLocal * Quaternion.Euler(smoothedPitch * 0.35f, smoothedYaw * 0.30f, 0f);
        }

        private void UpdateMixerFade(float dt)
        {
            if (!graph.IsValid() || activeSlot < 0) return;
            if (previousSlot >= 0 && previousSlot != activeSlot)
            {
                fadeTimer += dt;
                float t = Mathf.Clamp01(fadeTimer / fadeDuration);
                for (int i = 0; i < clips.Length; i++)
                {
                    if (i == activeSlot) mixer.SetInputWeight(i, t);
                    else if (i == previousSlot) mixer.SetInputWeight(i, 1f - t);
                    else mixer.SetInputWeight(i, 0f);
                }
                if (t >= 1f) previousSlot = -1;
            }
            else
            {
                for (int i = 0; i < clips.Length; i++)
                    mixer.SetInputWeight(i, i == activeSlot ? 1f : 0f);
            }
        }

        private void UpdateFacialAnimation(float dt)
        {
            if (faceRenderer == null) return;

            // Smooth emotional blendshapes
            curWarm = Mathf.MoveTowards(curWarm, targetWarm, dt * 110f);
            curConcern = Mathf.MoveTowards(curConcern, targetConcern, dt * 110f);
            curSorrow = Mathf.MoveTowards(curSorrow, targetSorrow, dt * 110f);
            SetShape(idxWarm, curWarm);
            SetShape(idxConcern, curConcern);
            SetShape(idxSorrow, curSorrow);

            // Natural non-rhythmic blinking (3.2 - 5.6 s cadence)
            if (enableBlink && idxBlinkL >= 0 && idxBlinkR >= 0)
            {
                if (blinkPhase >= 0f)
                {
                    blinkPhase += dt * 7.2f;
                    float w = blinkPhase < 1f ? Mathf.Sin(blinkPhase * Mathf.PI) * 100f : 0f;
                    SetShape(idxBlinkL, w);
                    SetShape(idxBlinkR, w);
                    if (blinkPhase >= 1f)
                    {
                        blinkPhase = -1f;
                        blinkCooldown = 3.1f + (Mathf.Sin(Time.unscaledTime * 1.73f) * 0.5f + 0.5f) * 2.5f;
                    }
                }
                else
                {
                    blinkCooldown -= dt;
                    if (blinkCooldown <= 0f) blinkPhase = 0f;
                }
            }

            // Live audio-driven VO lip-sync (RMS energy + zero-crossing rate -> AA / OH / EE / MM)
            float tAA = 0f, tOH = 0f, tEE = 0f, tMM = 0f;
            if (enableLipSync && voiceSource != null && voiceSource.isPlaying)
            {
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
                float brightness = Mathf.Clamp01((zcr / (float)sampleBuffer.Length - 0.04f) * 4.2f);
                if (env > 0.06f)
                {
                    tAA = env * (1f - brightness * 0.45f) * 86f;
                    tEE = env * brightness * 72f;
                    tOH = env * (1f - brightness) * 58f;
                    tMM = 0f;
                }
                else
                {
                    tMM = 22f;
                }
            }
            float rate = dt * 520f;
            lipAA = Mathf.MoveTowards(lipAA, tAA, rate);
            lipOH = Mathf.MoveTowards(lipOH, tOH, rate);
            lipEE = Mathf.MoveTowards(lipEE, tEE, rate);
            lipMM = Mathf.MoveTowards(lipMM, tMM, rate);
            SetShape(idxAA, lipAA);
            SetShape(idxOH, lipOH);
            SetShape(idxEE, lipEE);
            SetShape(idxMM, lipMM);
        }

        private void SetShape(int idx, float weight)
        {
            if (idx >= 0) faceRenderer.SetBlendShapeWeight(idx, weight);
        }
    }
}
