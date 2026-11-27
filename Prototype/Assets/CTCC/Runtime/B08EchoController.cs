using UnityEngine;

namespace CTCC
{
    /// <summary>
    /// Core v3.6.2 / Phase 3 B08 Perceptual Echo Controller at LOC02 Reality R (Reception Counter AN01).
    ///
    /// Manifests a subtle, diegetic, canon-pure optical afterimage of Minh's F101 decision
    /// (Affirmed / Unverified / Refuted) over the stainless pass-through tray and center glass pane
    /// strictly between CH01-T09 and CH01-T10 in Reality R.
    ///
    /// Hard canon &amp; UX guarantees:
    /// - Pure perceptual manifestation: ZERO solid colliders, ZERO physical evidence grants,
    ///   ZERO observation grants, ZERO out-of-canon spoilers.
    /// - Preserves 100% player agency (no camera hijack); fades gracefully by proximity &amp; gaze.
    /// - Full Reduced Motion support: freezes shader drift/breath to a calm static afterimage.
    /// - Zero per-frame material allocation (uses reusable MaterialPropertyBlock) and zero shadow cost.
    /// </summary>
    public sealed class B08EchoController : MonoBehaviour
    {
        public Renderer trayRenderer;
        public Renderer glassRenderer;
        public Renderer haloRenderer;
        public Light echoFillLight;

        public Material trayAffirmedMat;
        public Material trayUnverifiedMat;
        public Material trayRefutedMat;
        public Material glassAffirmedMat;
        public Material glassUnverifiedMat;
        public Material glassRefutedMat;
        public Material haloMat;

        private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
        private static readonly int ReduceMotionId = Shader.PropertyToID("_ReduceMotion");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private MaterialPropertyBlock trayBlock;
        private MaterialPropertyBlock glassBlock;
        private MaterialPropertyBlock haloBlock;

        private FormStatus boundBranch = FormStatus.Pending;
        private float currentAlpha;
        private float baseLightIntensity = 0.58f;
        private bool whisperPlayed;
        private bool reducedMotionMode;

        public bool IsManifesting => gameObject.activeSelf && currentAlpha > 0.02f;
        public FormStatus ActiveBranch => boundBranch;
        public float CurrentAlpha => currentAlpha;
        public bool HasAnyCollider => GetComponentsInChildren<Collider>(true).Length > 0;
        public bool ReducedMotionApplied => reducedMotionMode;

        public static bool ShouldManifest(Investigation state) =>
            state != null &&
            state.chapter == "CH01" &&
            state.layer == Reality.R &&
            state.Happened("CH01-T09") &&
            !state.Happened("CH01-T10");

        private void Awake()
        {
            trayBlock = new MaterialPropertyBlock();
            glassBlock = new MaterialPropertyBlock();
            haloBlock = new MaterialPropertyBlock();
            // Guarantee zero colliders even if asset reimport ever attaches one.
            foreach (var col in GetComponentsInChildren<Collider>(true))
                Destroy(col);
            if (echoFillLight != null)
            {
                echoFillLight.shadows = LightShadows.None;
                echoFillLight.enabled = false;
            }
        }

        public void Sync(Investigation state, Transform viewerEye, EnvironmentalAudio audioBus, bool reducedMotion, bool instantForQa = false)
        {
            reducedMotionMode = reducedMotion;
            bool shouldShow = ShouldManifest(state);
            if (!shouldShow)
            {
                currentAlpha = 0f;
                whisperPlayed = false;
                if (echoFillLight != null) echoFillLight.enabled = false;
                if (gameObject.activeSelf) gameObject.SetActive(false);
                return;
            }

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            FormStatus branch = state.f101.current == FormStatus.Pending ? FormStatus.Unverified : state.f101.current;
            if (branch != boundBranch)
                ApplyBranch(branch);

            Vector3 anchor = transform.position + new Vector3(0.05f, 0.34f, 0f);
            float dist = viewerEye != null ? Vector3.Distance(viewerEye.position, anchor) : 2.2f;
            float proximity = 1f - Mathf.InverseLerp(2.8f, 9.2f, dist);
            float gaze = 0.72f;
            if (viewerEye != null)
            {
                Vector3 toEcho = (anchor - viewerEye.position).normalized;
                float dot = Vector3.Dot(viewerEye.forward, toEcho);
                gaze = Mathf.Lerp(0.58f, 1.0f, Mathf.InverseLerp(0.15f, 0.85f, dot));
            }

            // Visible from doorway as a faint invitation (0.34) and rich perceptual clarity at counter (0.94).
            float targetAlpha = Mathf.Clamp(Mathf.Lerp(0.32f, 0.94f, proximity) * gaze, 0.22f, 0.96f);
            if (instantForQa)
                currentAlpha = 0.92f;
            else
                currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.unscaledDeltaTime * (reducedMotion ? 3.2f : 1.35f));

            ApplyPropertyBlocks(branch, currentAlpha, reducedMotion);

            if (echoFillLight != null)
            {
                echoFillLight.enabled = currentAlpha > 0.04f;
                echoFillLight.intensity = baseLightIntensity * currentAlpha;
            }

            if (!whisperPlayed && audioBus != null && dist <= 5.2f)
            {
                whisperPlayed = true;
                audioBus.From("b08EchoWhisper", anchor, 0.88f);
            }
        }

        private void ApplyBranch(FormStatus branch)
        {
            boundBranch = branch;
            if (trayRenderer != null)
            {
                Material m = branch switch
                {
                    FormStatus.Affirmed => trayAffirmedMat,
                    FormStatus.Refuted => trayRefutedMat,
                    _ => trayUnverifiedMat
                };
                if (m != null) trayRenderer.sharedMaterial = m;
            }

            if (glassRenderer != null)
            {
                Material m = branch switch
                {
                    FormStatus.Affirmed => glassAffirmedMat,
                    FormStatus.Refuted => glassRefutedMat,
                    _ => glassUnverifiedMat
                };
                if (m != null) glassRenderer.sharedMaterial = m;
            }

            if (haloRenderer != null && haloMat != null)
                haloRenderer.sharedMaterial = haloMat;

            if (echoFillLight != null)
            {
                echoFillLight.color = branch switch
                {
                    FormStatus.Affirmed => new Color(0.74f, 0.89f, 0.92f),
                    FormStatus.Refuted => new Color(0.84f, 0.72f, 0.66f),
                    _ => new Color(0.92f, 0.82f, 0.62f)
                };
                baseLightIntensity = branch switch
                {
                    FormStatus.Affirmed => 0.56f,
                    FormStatus.Refuted => 0.62f,
                    _ => 0.54f
                };
            }
        }

        private void ApplyPropertyBlocks(FormStatus branch, float alpha, bool reducedMotion)
        {
            trayBlock ??= new MaterialPropertyBlock();
            glassBlock ??= new MaterialPropertyBlock();
            haloBlock ??= new MaterialPropertyBlock();

            float rm = reducedMotion ? 1f : 0f;

            if (trayRenderer != null)
            {
                trayRenderer.GetPropertyBlock(trayBlock);
                trayBlock.SetFloat(AlphaId, alpha);
                trayBlock.SetFloat(ReduceMotionId, rm);
                trayRenderer.SetPropertyBlock(trayBlock);
            }

            if (glassRenderer != null)
            {
                glassRenderer.GetPropertyBlock(glassBlock);
                glassBlock.SetFloat(AlphaId, alpha * 0.74f);
                glassBlock.SetFloat(ReduceMotionId, rm);
                glassRenderer.SetPropertyBlock(glassBlock);
            }

            if (haloRenderer != null)
            {
                Color haloTint = branch switch
                {
                    FormStatus.Affirmed => new Color(0.74f, 0.90f, 0.92f, 0.42f),
                    FormStatus.Refuted => new Color(0.86f, 0.68f, 0.60f, 0.44f),
                    _ => new Color(0.90f, 0.80f, 0.60f, 0.38f)
                };
                haloRenderer.GetPropertyBlock(haloBlock);
                haloBlock.SetColor(BaseColorId, haloTint);
                haloBlock.SetFloat(AlphaId, alpha * 0.55f);
                haloBlock.SetFloat(ReduceMotionId, rm);
                haloRenderer.SetPropertyBlock(haloBlock);
            }
        }
    }
}
