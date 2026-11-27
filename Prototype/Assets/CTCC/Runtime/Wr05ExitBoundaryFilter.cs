using UnityEngine;

namespace CTCC
{
    /// <summary>
    /// Result of WR05 swept-boundary displacement projection applied immediately before CharacterController.Move.
    /// </summary>
    public readonly struct Wr05FilterResult
    {
        public readonly Vector3 AdjustedDelta;
        public readonly bool Blocked;
        public readonly bool ClampVelocityZ;

        public Wr05FilterResult(Vector3 adjustedDelta, bool blocked, bool clampVelocityZ)
        {
            AdjustedDelta = adjustedDelta;
            Blocked = blocked;
            ClampVelocityZ = clampVelocityZ;
        }
    }

    /// <summary>
    /// Core v3.6.2 / Phase 3 WR05 Exit Boundary Filter:
    /// Replaces legacy invisible seamBlocker collider and backward NudgeWithinScene pushback with
    /// continuous swept-segment intersection &amp; displacement projection across the outer hospital gate
    /// threshold (z = -19.95m, x in [2.45..5.55], y in [-0.45..0.45]).
    ///
    /// Contract invariants:
    /// - Active whenever CH01-T09 has committed AND GateService.G08(state) is false (in BOTH Reality R and Reality B).
    /// - Uses real GateService.G08(state) with zero stub; releases strictly when CH08 opens G08.
    /// - Zero overshoot even under worst-case low-FPS sprint steps (dt = 0.045s, 0.189m/frame).
    /// - Zero backward push (+Z); preserves full +/-X lateral slide, +Z return inward, and 360° camera look.
    /// </summary>
    public static class Wr05ExitBoundaryFilter
    {
        public const float ExitThresholdZ = -19.95f;
        public const float ApertureMinX = 2.45f;
        public const float ApertureMaxX = 5.55f;
        public const float ApertureMinY = -0.45f;
        public const float ApertureMaxY = 0.45f;
        public const float EdgeTolerance = 0.14f;
        private const float CampusSouthGuardZ = -25.0f;

        /// <summary>
        /// Real harness fault-injection switch for probe 'v36-wr05-bypass' (always false in gameplay).
        /// </summary>
        internal static bool QaInjectBypassForTesting;

        public static bool IsActive(Investigation state) =>
            !QaInjectBypassForTesting &&
            state != null &&
            state.Happened("CH01-T09") &&
            !GateService.G08(state);

        /// <summary>
        /// Evaluates WR05 against live Investigation state (uses real GateService.G08).
        /// </summary>
        public static Wr05FilterResult Filter(Investigation state, Vector3 startFeet, Vector3 displacement)
        {
            if (state == null)
                return new Wr05FilterResult(displacement, false, false);
            return Evaluate(startFeet, displacement, state.Happened("CH01-T09"), GateService.G08(state));
        }

        /// <summary>
        /// Pure deterministic swept-segment projection (unit-testable for both blocked and G08-open allow paths).
        /// </summary>
        public static Wr05FilterResult Evaluate(Vector3 startFeet, Vector3 displacement, bool t09Happened, bool g08Open)
        {
            if (QaInjectBypassForTesting || !t09Happened || g08Open)
                return new Wr05FilterResult(displacement, false, false);

            // Outward movement through the gate is strictly negative Z; moving inward (+Z) or purely lateral is always free.
            if (displacement.z >= 0f)
                return new Wr05FilterResult(displacement, false, false);

            Vector3 endFeet = startFeet + displacement;
            bool inVerticalBand =
                (startFeet.y >= ApertureMinY && startFeet.y <= ApertureMaxY) ||
                (endFeet.y >= ApertureMinY && endFeet.y <= ApertureMaxY);
            if (!inVerticalBand)
                return new Wr05FilterResult(displacement, false, false);

            // Case 1: Swept segment crosses z = ExitThresholdZ from inside courtyard (z >= ExitThresholdZ -> end.z < ExitThresholdZ).
            if (startFeet.z >= ExitThresholdZ && endFeet.z < ExitThresholdZ)
            {
                float t = Mathf.Clamp01((ExitThresholdZ - startFeet.z) / displacement.z);
                float xHit = startFeet.x + displacement.x * t;
                if (InApertureX(xHit) || InApertureX(startFeet.x) || InApertureX(endFeet.x))
                {
                    // Project Z exactly onto ExitThresholdZ (<= 0, NEVER pushes player backward +Z); preserve full X & Y.
                    float clampedDz = Mathf.Min(0f, ExitThresholdZ - startFeet.z);
                    return new Wr05FilterResult(new Vector3(displacement.x, displacement.y, clampedDz), true, true);
                }
                return new Wr05FilterResult(displacement, false, false);
            }

            // Case 2: Already resting on or within skin-width tolerance of ExitThresholdZ while pressing outward (-Z).
            if (startFeet.z < ExitThresholdZ && startFeet.z >= ExitThresholdZ - EdgeTolerance)
            {
                if (InApertureX(startFeet.x) || InApertureX(endFeet.x))
                    return new Wr05FilterResult(new Vector3(displacement.x, displacement.y, 0f), true, true);
                return new Wr05FilterResult(displacement, false, false);
            }

            // Case 3: Safety guard if a legacy/corrupted position somehow landed south of threshold while WR05 active:
            // never push backward, simply halt any further -Z travel while permitting free +Z return into courtyard.
            if (startFeet.z < ExitThresholdZ - EdgeTolerance && startFeet.z >= CampusSouthGuardZ &&
                (InApertureX(startFeet.x) || InApertureX(endFeet.x)))
            {
                return new Wr05FilterResult(new Vector3(displacement.x, displacement.y, 0f), true, true);
            }

            return new Wr05FilterResult(displacement, false, false);
        }

        /// <summary>
        /// True when player is at the open gate threshold pre-T09 (for scripted one-time B07 departure seam trigger).
        /// </summary>
        public static bool IsCrossingPreT09Seam(Investigation state, Vector3 feet) =>
            state != null &&
            state.chapter == "CH01" &&
            state.layer == Reality.R &&
            state.Happened("CH01-T08") &&
            !state.Happened("CH01-T09") &&
            feet.z <= ExitThresholdZ + 0.03f &&
            feet.y >= ApertureMinY && feet.y <= ApertureMaxY &&
            InApertureX(feet.x);

        private static bool InApertureX(float x) => x >= ApertureMinX && x <= ApertureMaxX;
    }
}
