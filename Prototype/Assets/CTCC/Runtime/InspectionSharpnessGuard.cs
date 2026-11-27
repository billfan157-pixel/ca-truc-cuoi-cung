using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CTCC
{
    /// <summary>
    /// Phase 2.5 Visual Clarity Guard for Physical Inspector 2.0, Diegetic Phone 3D, Envelope 3D, AN02 Clipboard, and Inventory 3D Preview.
    /// Eliminates BOTH primary root causes of oblique-angle inspection blur:
    /// 1. Ref-counted native 1.0x URP renderScale boost during close-up 3D reading (eliminating 0.88x/0.76x bilinear smear on Medium/Low).
    /// 2. Far-only Gaussian Depth of Field (> 1.30m background softening with 0.00 near-field blur across 0.20..1.25m at ALL tilt angles!),
    ///    using a runtime-instanced Volume profile so shared project .asset files are never mutated.
    /// </summary>
    internal static class InspectionSharpnessGuard
    {
        private static int activeCount;
        private static UniversalRenderPipelineAsset boostedUrp;
        private static float savedRenderScale = 1f;

        private static DepthOfField activeDof;
        private static bool savedDofActive;
        private static DepthOfFieldMode savedDofMode;
        private static float savedGaussianStart;
        private static float savedGaussianEnd;
        private static float savedGaussianRadius;

        public static bool IsActive => activeCount > 0;

        public static void Acquire(Volume activeGrade, bool allowBackgroundSoften)
        {
            activeCount++;
            if (activeCount == 1)
            {
                if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
                {
                    boostedUrp = urp;
                    savedRenderScale = urp.renderScale;
                    if (urp.renderScale < 0.999f)
                        urp.renderScale = 1.0f;
                }
            }

            // Configure safe far-only Gaussian DoF (ZERO blur on near inspected object [0.05..1.25m], soft background > 1.30m)
            if (activeDof == null && activeGrade != null)
            {
                // Always read runtime-instanced profile (never sharedProfile) so project .asset stays pristine!
                var prof = activeGrade.profile;
                if (prof != null && prof.TryGet<DepthOfField>(out var dof))
                {
                    activeDof = dof;
                    savedDofActive = dof.active;
                    savedDofMode = dof.mode.value;
                    savedGaussianStart = dof.gaussianStart.value;
                    savedGaussianEnd = dof.gaussianEnd.value;
                    savedGaussianRadius = dof.gaussianMaxRadius.value;

                    if (allowBackgroundSoften)
                    {
                        dof.mode.Override(DepthOfFieldMode.Gaussian);
                        dof.gaussianStart.Override(1.30f);
                        dof.gaussianEnd.Override(3.60f);
                        dof.gaussianMaxRadius.Override(0.70f);
                        dof.active = true;
                    }
                    else
                    {
                        dof.active = false;
                    }
                }
            }
        }

        public static void Release()
        {
            if (activeCount <= 0)
            {
                activeCount = 0;
                return;
            }
            activeCount--;
            if (activeCount > 0) return;

            if (boostedUrp != null)
            {
                boostedUrp.renderScale = savedRenderScale;
                boostedUrp = null;
            }
            if (activeDof != null)
            {
                activeDof.mode.Override(savedDofMode);
                activeDof.gaussianStart.Override(savedGaussianStart);
                activeDof.gaussianEnd.Override(savedGaussianEnd);
                activeDof.gaussianMaxRadius.Override(savedGaussianRadius);
                activeDof.active = savedDofActive;
                activeDof = null;
            }
        }

        public static void ResetAll()
        {
            activeCount = activeCount > 0 ? 1 : 0;
            Release();
        }
    }
}
