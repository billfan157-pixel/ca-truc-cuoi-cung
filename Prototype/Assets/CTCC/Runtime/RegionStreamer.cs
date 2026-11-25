using System.Collections;
using UnityEngine;

namespace CTCC
{
    /// <summary>
    /// 3D XYZ ZoneVolume & Portal-aware regional scene streamer for An Sinh Hospital Campus.
    /// Supports multi-wing same-Z disambiguation (K-B/K-A/K-D), multi-story elevation stacks
    /// (B1 -3.4m / Grade 0m / Floor 2 +4m / Floor 3 +8m), gate-locked thresholds, and unload hysteresis.
    /// </summary>
    internal sealed class RegionStreamer
    {
        internal const string Upper = "LOC05_ArchiveWing";
        internal const string Service = "LOC09_ServiceWing";
        internal const string CampusWings = "Campus_Interiors";

        private static readonly Bounds ReceptionS1Portal = new(new Vector3(-1.5f, 1.8f, -3.5f), new Vector3(7.5f, 4.6f, 9.5f));
        private static readonly Bounds EmergencyS3Portal = new(new Vector3(4.0f, 1.8f, 24.0f), new Vector3(12.0f, 4.8f, 16.5f));
        private static readonly Bounds EmergencyS2Portal = new(new Vector3(6.5f, 0.2f, 24.0f), new Vector3(8.5f, 8.0f, 16.5f));
        private static readonly Bounds UpperStackVolume = new(new Vector3(0.5f, 7.2f, 21.0f), new Vector3(38.0f, 8.4f, 62.0f));
        private static readonly Bounds BasementStackVolume = new(new Vector3(16.0f, -2.1f, 38.0f), new Vector3(46.0f, 4.2f, 36.0f));
        private static readonly Bounds WingsProximityCourtyard = new(new Vector3(4.0f, 2.0f, -12.0f), new Vector3(30.0f, 8.0f, 30.0f));
        private static readonly Bounds WingsProximityEmergency = new(new Vector3(8.0f, 2.0f, 18.0f), new Vector3(32.0f, 9.0f, 26.0f));

        private readonly HospitalScene scene;
        private float upperLastNeeded = float.NegativeInfinity;
        private float serviceLastNeeded = float.NegativeInfinity;
        private float wingsLastNeeded = float.NegativeInfinity;

        internal RegionStreamer(HospitalScene scene) => this.scene = scene;

        internal ZoneRegion ResolveZoneAt(Vector3 worldPos) => scene?.ResolveZoneAt(worldPos);

        internal IEnumerator Reconcile(Investigation state, Vector3 feet)
        {
            if (!scene.IsAdditiveMode) yield break;

            bool secondGate = GateService.G02(state);
            string checkpoint = SafeCheckpoints.Pad(state.checkpoint);
            bool upperPinned = checkpoint is "AP06" or "AN03" or "AP07" or "AP10" or "AP11";
            bool servicePinned = checkpoint is "AP08" or "AP09";

            bool atReceptionS1 = ReceptionS1Portal.Contains(feet);
            bool atEmergencyS3 = EmergencyS3Portal.Contains(feet);
            bool atEmergencyS2 = EmergencyS2Portal.Contains(feet);

            bool onS1OrUpperFloor = feet.y > 0.3f &&
                UpperStackVolume.Contains(new Vector3(feet.x, UpperStackVolume.center.y, feet.z));
            bool earlyS1Access = (state.layer == Reality.R || IsS1DischargeMemory(state)) && (atReceptionS1 || onS1OrUpperFloor);
            bool upperNeeded = secondGate
                ? upperPinned || UpperStackVolume.Contains(feet) || feet.y > 0.3f || atReceptionS1 || atEmergencyS3
                : earlyS1Access;
            bool serviceNeeded = secondGate && (servicePinned || BasementStackVolume.Contains(feet) || feet.y < -0.25f ||
                (state.chapter is ("CH04" or "CH05") && atEmergencyS2));

            if (upperNeeded)
            {
                upperLastNeeded = Time.unscaledTime;
                if (!scene.HasZone("LOC05")) yield return scene.LoadZoneAsync(Upper);
            }
            else if (Time.unscaledTime - upperLastNeeded > 20f && scene.HasZone("LOC05"))
                yield return scene.UnloadZoneAsync(Upper);
            if (serviceNeeded)
            {
                serviceLastNeeded = Time.unscaledTime;
                if (!scene.HasZone("LOC09")) yield return scene.LoadZoneAsync(Service);
            }
            if (!serviceNeeded && Time.unscaledTime - serviceLastNeeded > 20f && scene.HasZone("LOC09"))
                yield return scene.UnloadZoneAsync(Service);

            // Handle the walkable regions before yielding to optional visual streaming.
            if (!scene.HasConfiguredRegionalScene(CampusWings)) yield break;
            bool wingsNeeded = WingsProximityCourtyard.Contains(feet) || WingsProximityEmergency.Contains(feet) ||
                feet.y > 3.0f || feet.y < -1.0f || feet.x < -2.0f || feet.x > 13.0f;
            if (wingsNeeded)
            {
                wingsLastNeeded = Time.unscaledTime;
                if (!scene.HasZone("CAMPUS_WINGS")) yield return scene.LoadZoneAsync(CampusWings);
            }
            else if (Time.unscaledTime - wingsLastNeeded > 60f && scene.HasZone("CAMPUS_WINGS"))
                yield return scene.UnloadZoneAsync(CampusWings);
        }

        internal static bool IsS1DischargeMemory(Investigation state) => state.chapter == "CH05" && state.layer == Reality.M;
    }
}
