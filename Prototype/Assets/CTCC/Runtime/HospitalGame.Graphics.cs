using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CTCC
{
    // Per-tier camera & frame-pacing settings that cannot live in the RP asset (docs/graphics/graphics-upgrade-research-v1.md, G3/G4).
    // Only the High tier changes AA/dithering: SMAA High + dithering. Low and Medium restore the authored scene values.
    // On high-refresh (>=100 Hz) windowed/borderless displays, smooth 60 FPS DWM pacing prevents 120Hz vSync 60<->40<->30 FPS cliffs.
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

            // Smooth interactive frame pacing (untouched during benchmark/QA/shot capture which override vSyncCount themselves).
            bool automatedRun = qaFolder != null
                || Argument("-ctcc-benchmark") != null
                || Argument("-ctcc-input-qa") != null;
            if (!automatedRun)
            {
                double refreshHz = Screen.currentResolution.refreshRateRatio.value;
                if (refreshHz >= 100.0)
                {
                    if (Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen)
                    {
                        QualitySettings.vSyncCount = 2;
                        Application.targetFrameRate = -1;
                    }
                    else
                    {
                        // Windowed/Borderless DWM on Windows is already tear-free; pacing at 60 FPS avoids 120Hz vSync judder!
                        QualitySettings.vSyncCount = 0;
                        Application.targetFrameRate = 60;
                    }
                }
                else
                {
                    QualitySettings.vSyncCount = 1;
                    Application.targetFrameRate = -1;
                }
            }
        }
    }
}
