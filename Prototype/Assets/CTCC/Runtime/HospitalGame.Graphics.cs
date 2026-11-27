using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CTCC
{
    // Per-tier camera settings that cannot live in the RP asset (docs/graphics/graphics-upgrade-research-v1.md, G3/G4).
    // Only the High tier changes: SMAA High + dithering. Low and Medium restore the authored scene values.
    public sealed partial class HospitalGame
    {
        private bool tierBaselineCaptured;
        private AntialiasingMode tierBaselineAntialiasing;
        private AntialiasingQuality tierBaselineAntialiasingQuality;
        private bool tierBaselineDithering;

        private void ApplyGraphicsTierCameraSettings()
        {
            var eye = player != null ? player.Eye : null;
            if (eye == null) return;
            var data = eye.GetUniversalAdditionalCameraData();
            if (data == null) return;
            if (!tierBaselineCaptured)
            {
                tierBaselineAntialiasing = data.antialiasing;
                tierBaselineAntialiasingQuality = data.antialiasingQuality;
                tierBaselineDithering = data.dithering;
                tierBaselineCaptured = true;
            }
            if (QualitySettings.names[QualitySettings.GetQualityLevel()] == "High")
            {
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.antialiasingQuality = AntialiasingQuality.High;
                data.dithering = true;
            }
            else
            {
                data.antialiasing = tierBaselineAntialiasing;
                data.antialiasingQuality = tierBaselineAntialiasingQuality;
                data.dithering = tierBaselineDithering;
            }
        }
    }
}
