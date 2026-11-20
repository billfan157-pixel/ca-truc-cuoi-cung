using System.Collections;
using UnityEngine;

namespace CTCC
{
    internal sealed class RegionStreamer
    {
        internal const string Upper = "LOC05_ArchiveWing";
        internal const string Service = "LOC09_ServiceWing";

        private readonly HospitalScene scene;
        private float upperLastNeeded = float.NegativeInfinity;
        private float serviceLastNeeded = float.NegativeInfinity;

        internal RegionStreamer(HospitalScene scene) => this.scene = scene;

        internal IEnumerator Reconcile(Investigation state, Vector3 feet)
        {
            if (!scene.IsAdditiveMode || !GateService.G02(state)) yield break;
            string checkpoint = SafeCheckpoints.Pad(state.checkpoint);
            bool upperPinned = checkpoint is "AP06" or "AN03" or "AP07";
            bool servicePinned = checkpoint is "AP08" or "AP09";
            // Grade neighbours of the upper wing are Reception and Emergency.
            // Emergency is also the neighbour of the basement service staircase.
            bool atReceptionCore = feet.x < 2f && feet.z >= -8f && feet.z < 1f;
            bool atEmergencyCore = feet.z >= 16f && feet.z < 32.1f && feet.y > -3.5f && feet.y < 4.1f;
            bool upperNeeded = upperPinned || feet.y > 0.3f || atReceptionCore || atEmergencyCore;
            bool serviceNeeded = servicePinned || feet.y < -0.25f ||
                (state.chapter == "CH04" && atEmergencyCore);

            if (upperNeeded)
            {
                upperLastNeeded = Time.unscaledTime;
                if (!scene.HasZone("LOC05")) yield return scene.LoadZoneAsync(Upper);
            }
            if (serviceNeeded)
            {
                serviceLastNeeded = Time.unscaledTime;
                if (!scene.HasZone("LOC09")) yield return scene.LoadZoneAsync(Service);
            }
            if (!upperNeeded && Time.unscaledTime - upperLastNeeded > 20f && scene.HasZone("LOC05"))
                yield return scene.UnloadZoneAsync(Upper);
            if (!serviceNeeded && Time.unscaledTime - serviceLastNeeded > 20f && scene.HasZone("LOC09"))
                yield return scene.UnloadZoneAsync(Service);
        }
    }
}
