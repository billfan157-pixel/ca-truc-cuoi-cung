using System;
using System.Collections.Generic;
using UnityEngine;

namespace CTCC
{
    public static class DreadCatalog
    {
        public const string B01SignKnock = "DREAD_B01_SIGN_KNOCK";
        public const string B02GlassShadow = "DREAD_B02_GLASS_SHADOW";
        public const string B02GlassStudied = "DREAD_B02_GLASS_STUDIED";
        public const string B03LanDrawerLook = "DREAD_B03_LAN_DRAWER_LOOK";
        public const string B04TrolleyStop = "DREAD_B04_TROLLEY_STOP";
        public const string B04TrolleyNear = "DREAD_B04_TROLLEY_NEAR";
        public const string B04TrolleyClose = "DREAD_B04_TROLLEY_CLOSE";
        public const string B04StaffTrolleyHalt = "DREAD_B04_STAFF_TROLLEY_HALT";
        public const string B04CurtainBreath = "DREAD_B04_CURTAIN_BREATH";
        public const string B05FanDecelerate = "DREAD_B05_FAN_DECELERATE";
        public const string B05DistantSteps = "DREAD_B05_DISTANT_STEPS";
        public const string B05CurtainSilhouette = "DREAD_B05_CURTAIN_SILHOUETTE";
        public const string B06PaInterference = "DREAD_B06_PA_INTERFERENCE";
        public const string B06ClockTick = "DREAD_B06_CLOCK_TICK";
        public const string B07GateRainHush = "DREAD_B07_GATE_RAIN_HUSH";
        public const string B07GateSlam = "DREAD_B07_GATE_SLAM";
        public const string Bag08Echo = "P_BAG08_ECHO";

        // Upgrade #2: "Shift Behind the Paper" DeferredMicroShift events & F101-branch dread variants
        public const string ShiftCurtainCreep = "DREAD_SHIFT_CURTAIN_CREEP";
        public const string ShiftTubeBlackout = "DREAD_SHIFT_TUBE_BLACKOUT";
        public const string ShiftHallChair = "DREAD_SHIFT_HALL_CHAIR";
        public const string F101AffirmedSuffocation = "DREAD_F101_AFFIRMED_SUFFOCATION";
        public const string F101UnverifiedStillness = "DREAD_F101_UNVERIFIED_STILLNESS";
        public const string F101RefutedCrack = "DREAD_F101_REFUTED_CRACK";

        private static readonly HashSet<string> AllIds = new(StringComparer.Ordinal)
        {
            B01SignKnock,
            B02GlassShadow,
            B02GlassStudied,
            B03LanDrawerLook,
            B04TrolleyStop,
            B04TrolleyNear,
            B04TrolleyClose,
            B04StaffTrolleyHalt,
            B04CurtainBreath,
            B05FanDecelerate,
            B05DistantSteps,
            B05CurtainSilhouette,
            B06PaInterference,
            B06ClockTick,
            B07GateRainHush,
            B07GateSlam,
            Bag08Echo,
            ShiftCurtainCreep,
            ShiftTubeBlackout,
            ShiftHallChair,
            F101AffirmedSuffocation,
            F101UnverifiedStillness,
            F101RefutedCrack
        };

        public static bool IsRegistered(string id) =>
            !string.IsNullOrEmpty(id) && AllIds.Contains(id);
    }

    public readonly struct DeferredMicroShift
    {
        public readonly string DreadId;
        public readonly string SourceObservationId;
        public readonly float DelaySeconds;

        public DeferredMicroShift(string dreadId, string sourceObservationId, float delaySeconds = 0.34f)
        {
            DreadId = dreadId;
            SourceObservationId = sourceObservationId;
            DelaySeconds = delaySeconds;
        }
    }

    /// <summary>
    /// Lightweight, strictly non-lethal CH01 psychological horror director cleanly separated from EnvironmentalAudio.
    /// Reads player movement, stillness, gaze alignment, interaction intensity, exploration history, narrative beat,
    /// and reality state to schedule restrained, spatially valid environmental responses with strict cooldowns,
    /// quiet periods, deterministic QA seeding, dialogue/inspection suppression, and persistent one-time tracking.
    /// </summary>
    public sealed class AmbientDreadDirector : MonoBehaviour
    {
        public int Seed { get; private set; } = 20051118;
        public int EventsFiredThisSession { get; private set; }
        public string LastTriggeredId { get; private set; } = "";
        public bool VisitedStaffAlcove { get; private set; }
        public bool VisitedRoom101 { get; private set; }
        public int BacktrackCount { get; private set; }

        private HospitalScene scene;
        private EnvironmentalAudio audioBus;
        private FirstPerson player;
        private Func<Investigation> stateGetter;
        private Func<bool> suppressionCheck;
        private Action<string> onOneTimeTriggered;
        private Action<string> onCaption;

        private System.Random rng;
        private float globalCooldown;
        private float quietTimer;
        private float stillnessSeconds;
        private float interactionIntensity;
        private Vector3 lastPlayerPos;
        private bool hasLastPos;
        private string lastZone = "";
        private float trolleyFlashTimer;
        private float trolleyFlashDuration;
        private readonly Queue<DeferredMicroShift> deferredShifts = new();
        private DeferredMicroShift? armedShift;
        private float armedShiftTimer;

        // Walkthrough Peak Horror #1 (P-SC05 Bed-4 silhouette) & Peak Horror #2 (B07 gate slam) coordination
        private GameObject bed4SilhouetteRoot;
        private Material bed4SilhouetteMat;
        private Texture2D bed4SilhouetteTex;
        private float bed4LureWindow;
        private float bed4SilhouetteTimer;
        private float bed4SilhouetteDuration;
        private GameObject gateBarShadow;
        private Material gateBarShadowMat;
        private Texture2D gateBarShadowTex;
        private float gateShadowTimer;
        public float LastPeakScareUnscaledTime { get; private set; } = -999f;

        private const float DefaultGlobalCooldown = 16f;
        private const float DefaultQuietWindow = 8f;
        private const int MaxSessionBudget = 8;

        internal bool AmbientBudgetExhausted => EventsFiredThisSession >= MaxSessionBudget;

        public bool HasPendingMicroShift => deferredShifts.Count > 0 || armedShift.HasValue;

        public void Initialize(
            HospitalScene world,
            EnvironmentalAudio audio,
            FirstPerson fps,
            Func<Investigation> getState,
            Func<bool> isSuppressed,
            Action<string> persistEvent,
            Action<string> captionCallback,
            int seed = 20051118)
        {
            scene = world;
            audioBus = audio;
            player = fps;
            stateGetter = getState;
            suppressionCheck = isSuppressed;
            onOneTimeTriggered = persistEvent;
            onCaption = captionCallback;
            ResetSeed(seed);
        }

        public void ResetSeed(int seed)
        {
            Seed = seed != 0 ? seed : 20051118;
            rng = new System.Random(Seed);
            EventsFiredThisSession = 0;
            LastTriggeredId = "";
            globalCooldown = 6f;
            quietTimer = 3f;
            stillnessSeconds = 0f;
            interactionIntensity = 0f;
            VisitedStaffAlcove = false;
            VisitedRoom101 = false;
            BacktrackCount = 0;
            lastZone = "";
            scene?.motion?.ClearFanSlowdown();
            scene?.motion?.ClearTubeBlackout();
            deferredShifts.Clear();
            armedShift = null;
            armedShiftTimer = 0f;
            trolleyFlashTimer = 0f;
            trolleyFlashDuration = 0f;
            bed4LureWindow = 0f;
            bed4SilhouetteTimer = 0f;
            gateShadowTimer = 0f;
            LastPeakScareUnscaledTime = -999f;
            if (bed4SilhouetteRoot != null) bed4SilhouetteRoot.SetActive(false);
            if (gateBarShadow != null) gateBarShadow.SetActive(false);
            hasLastPos = false;
        }

        public bool TryTriggerB01SignKnock()
        {
            var state = stateGetter?.Invoke();
            if (state == null || state.chapter != "CH01" || state.layer != Reality.R ||
                !state.Happened("CH01-T02") || state.Happened("CH01-T03") ||
                !CanFire(state, DreadCatalog.B01SignKnock))
                return false;
            Fire(state, DreadCatalog.B01SignKnock, persistent: true, () =>
            {
                audioBus?.From("signKnock", new Vector3(6.76f, 1.22f, -20.22f), 0.72f);
                onCaption?.Invoke("[Tấm biển tôn tháo dỡ ngoài cổng khẽ va một tiếng khô khốc trong gió.]");
            });
            return true;
        }

        public bool NotifyStaffDrawerForcePull()
        {
            var state = stateGetter?.Invoke();
            if (state == null || state.chapter != "CH01" || state.layer != Reality.B ||
                !state.Happened("CH01-T05") || state.Happened("CH01-T08") ||
                !state.HasDreadEvent(DreadCatalog.B04TrolleyStop) ||
                !CanFire(state, DreadCatalog.B04StaffTrolleyHalt))
                return false;
            if (suppressionCheck != null && suppressionCheck()) return false;
            Fire(state, DreadCatalog.B04StaffTrolleyHalt, persistent: true, () =>
            {
                audioBus?.From("trolleyRoll", new Vector3(2.9f, 0.65f, 7.6f), 0.44f);
                onCaption?.Invoke("[Tiếng bánh xe đẩy ngoài hành lang khựng lại ngay sát lối thông nhân viên.]");
            });
            return true;
        }

        public bool PlayPeakGateSlam(bool reducedMotion)
        {
            var state = stateGetter?.Invoke();
            if (state == null || !CanFire(state, DreadCatalog.B07GateSlam)) return false;
            bool recentlyPeaked = (Time.unscaledTime - LastPeakScareUnscaledTime) < 45f;
            float vol = recentlyPeaked ? 0.62f : 0.88f;
            float dip = recentlyPeaked ? 0.24f : 0.15f;
            LastPeakScareUnscaledTime = Time.unscaledTime;
            Fire(state, DreadCatalog.B07GateSlam, persistent: true, () =>
            {
                audioBus?.PulseAmbienceDip(1.4f, dip);
                audioBus?.From("gateSlam", new Vector3(4.0f, 1.35f, -20.65f), vol);
                onCaption?.Invoke("[Một cú đập kim loại nặng nề vang dội từ phía ngoài cánh cổng sắt!]");
                if (!reducedMotion)
                {
                    EnsureGateBarShadow();
                    if (gateBarShadow != null)
                    {
                        gateBarShadow.SetActive(true);
                        gateBarShadow.transform.position = new Vector3(3.15f, 1.15f, -20.52f);
                        gateShadowTimer = 0.58f;
                    }
                }
            });
            return true;
        }

        public void NotifyInteraction(float weight = 0.35f)
        {
            interactionIntensity = Mathf.Clamp01(interactionIntensity + weight);
            quietTimer = Mathf.Max(quietTimer, 2.5f);
        }

        public void NotifyMajorBeatQuietWindow(float seconds = DefaultQuietWindow)
        {
            quietTimer = Mathf.Max(quietTimer, seconds);
        }

        internal void NotifyF101Signing()
        {
            NotifyMajorBeatQuietWindow(10f);
            deferredShifts.Clear();
            armedShift = null;
            bed4LureWindow = 0f;
        }

        /// <summary>
        /// Upgrade #2: Instead of discarding dread while the 3D PhysicalInspector is open,
        /// queue a contextual DeferredMicroShift that triggers right after lowering the document.
        /// </summary>
        public void NotifyInspectorOpened(string observationId)
        {
            var state = stateGetter?.Invoke();
            if (state == null || state.chapter != "CH01" || state.layer != Reality.B ||
                !state.Happened("CH01-T05") || state.Happened("CH01-T08"))
                return;
            if (deferredShifts.Count >= 1 || armedShift.HasValue) return;
            string zone = player != null ? ClassifyZone(player.Feet) : "";
            string shiftId = PickDeferredShiftFor(state, zone, observationId);
            if (!string.IsNullOrEmpty(shiftId))
                deferredShifts.Enqueue(new DeferredMicroShift(shiftId, observationId ?? ""));
        }

        public void NotifyInspectorClosed(string observationId)
        {
            if (deferredShifts.Count == 0) return;
            var shift = deferredShifts.Dequeue();
            var state = stateGetter?.Invoke();
            if (state == null || state.chapter != "CH01" || state.layer != Reality.B || state.Happened("CH01-T08"))
                return;
            if (!CanFire(state, shift.DreadId)) return;
            armedShift = shift;
            armedShiftTimer = 0.42f;
        }

        private string PickDeferredShiftFor(Investigation state, string zone, string observationId)
        {
            // Inside Room 101: prioritize Bed-4 curtain creeping closed behind the paper, then tube blackout.
            if (zone == "LOC03" || observationId == "OBS101B" || observationId == "OBS_F101" || observationId == "OBS_BED4_SHEET")
            {
                bool curtainOpenEnough = scene?.interactables != null && scene.interactables.CurtainOpenness > 0.35f;
                if (curtainOpenEnough && CanFire(state, DreadCatalog.ShiftCurtainCreep))
                    return DreadCatalog.ShiftCurtainCreep;
                if (CanFire(state, DreadCatalog.ShiftTubeBlackout))
                    return DreadCatalog.ShiftTubeBlackout;
                if (CanFire(state, DreadCatalog.ShiftHallChair))
                    return DreadCatalog.ShiftHallChair;
                return "";
            }
            // In HALL / STAFF / LOC02: prioritize distant hallway chair scrape, then tube blackout.
            if (CanFire(state, DreadCatalog.ShiftHallChair))
                return DreadCatalog.ShiftHallChair;
            if (CanFire(state, DreadCatalog.ShiftTubeBlackout))
                return DreadCatalog.ShiftTubeBlackout;
            return "";
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || scene == null || player == null || stateGetter == null) return;

            UpdateActiveEffects(dt);

            var state = stateGetter();
            if (state == null || state.chapter != "CH01") return;

            Vector3 feet = player.Feet;
            if (!hasLastPos)
            {
                lastPlayerPos = feet;
                hasLastPos = true;
            }

            float speed = Vector3.Distance(feet, lastPlayerPos) / Mathf.Max(dt, 0.001f);
            lastPlayerPos = feet;

            if (speed < 0.20f) stillnessSeconds += dt;
            else stillnessSeconds = Mathf.Max(0f, stillnessSeconds - dt * 2.4f);

            interactionIntensity = Mathf.Max(0f, interactionIntensity - dt * 0.06f);
            UpdateZoneExplorationHistory(feet);

            // Never interrupt active dialogue, UI modals, PhysicalInspector, or F101 reasoning.
            if (suppressionCheck != null && suppressionCheck())
            {
                quietTimer = Mathf.Max(quietTimer, 2.2f);
                return;
            }

            // Fire any armed "shift behind the paper" micro-shift right as the player returns to live 3D view!
            if (armedShift.HasValue)
            {
                armedShiftTimer -= dt;
                if (armedShiftTimer <= 0f)
                {
                    var shift = armedShift.Value;
                    armedShift = null;
                    if (state.layer == Reality.B && !AmbientBudgetExhausted && CanFire(state, shift.DreadId))
                    {
                        ExecuteDeferredShift(state, shift.DreadId, state.settings.reduceMotion);
                        return;
                    }
                }
            }

            if (globalCooldown > 0f) globalCooldown -= dt;
            if (quietTimer > 0f) quietTimer -= dt;
            if (globalCooldown > 0f || quietTimer > 0f || AmbientBudgetExhausted)
                return;

            TryEvaluateBeat(state, feet, state.settings.reduceMotion);
        }

        private void UpdateZoneExplorationHistory(Vector3 feet)
        {
            string zone = ClassifyZone(feet);
            if (zone == lastZone || string.IsNullOrEmpty(zone)) return;
            if (zone == "STAFF") VisitedStaffAlcove = true;
            if (zone == "LOC03") VisitedRoom101 = true;
            if (!string.IsNullOrEmpty(lastZone) &&
                ((zone == "LOC02" && (VisitedRoom101 || VisitedStaffAlcove)) ||
                 (zone == "STAFF" && VisitedRoom101) ||
                 (zone == "LOC03" && VisitedStaffAlcove)))
            {
                BacktrackCount++;
            }
            lastZone = zone;
        }

        public static string ClassifyZone(Vector3 feet)
        {
            if (feet.z < -6.5f) return "LOC01";
            if (feet.z <= 0.2f) return "LOC02";
            if (feet.x < 1.9f && feet.z > 0.2f && feet.z < 16.5f) return "STAFF";
            if (feet.x > 6.0f && feet.z > 0.5f && feet.z < 10.5f) return "LOC03";
            if (feet.x >= 1.9f && feet.x <= 6.0f && feet.z > 0.2f && feet.z < 17f) return "HALL";
            return "";
        }

        private float GazeAlignment(Vector3 worldPoint)
        {
            if (player == null || player.Eye == null) return 0f;
            Vector3 to = worldPoint - player.Eye.transform.position;
            if (to.sqrMagnitude < 0.04f) return 1f;
            return Vector3.Dot(player.Eye.transform.forward, to.normalized);
        }

        private void TryEvaluateBeat(Investigation state, Vector3 feet, bool reducedMotion)
        {
            bool layerB = state.layer == Reality.B;
            bool enteredGate = state.Happened("CH01-T02");
            bool tookBoard = state.Happened("CH01-T05");
            bool g01Done = GateService.G01(state);
            bool collapsedToR = state.Happened("CH01-T08");
            string zone = ClassifyZone(feet);

            // 1. B02: A quiet exterior cue as Minh notices the reception glass.
            if (!layerB && enteredGate && !state.Happened("CH01-T03") &&
                feet.z > -15.5f && feet.z < -7.5f &&
                GazeAlignment(new Vector3(1.5f, 1.55f, -3.5f)) > 0.50f &&
                CanFire(state, DreadCatalog.B02GlassShadow))
            {
                Fire(state, DreadCatalog.B02GlassShadow, persistent: true, () =>
                {
                    audioBus?.From("distantThunder", new Vector3(4f, 5f, -24f), 0.32f);
                    onCaption?.Invoke("[Sau ô kính quầy tiếp nhận có bóng người.]");
                });
                return;
            }

            // 2. B04 Unified 3-Stage Trolley Dread (Far -> Near -> Close, strictly non-overlapping!)
            if (layerB && tookBoard && !g01Done && zone == "HALL" && feet.z > 1.8f && feet.z < 9.5f &&
                GazeAlignment(new Vector3(4.0f, 1.0f, 15.0f)) > 0.30f &&
                CanFire(state, DreadCatalog.B04TrolleyStop))
            {
                Fire(state, DreadCatalog.B04TrolleyStop, persistent: true, () =>
                {
                    if (scene.TrolleyShadowProp != null)
                    {
                        scene.TrolleyShadowProp.SetActive(true);
                        scene.TrolleyShadowProp.transform.position = new Vector3(1.2f, 0f, 17.2f);
                        trolleyFlashDuration = reducedMotion ? 0.55f : 2.8f;
                        trolleyFlashTimer = trolleyFlashDuration;
                    }
                    audioBus?.From("trolleyRoll", new Vector3(4.0f, 0.7f, 15.6f), 0.44f);
                    onCaption?.Invoke("[Tiếng bánh xe đẩy cáng lăn sau cửa kính mờ Cấp cứu... rồi đột ngột khựng lại.]");
                });
                return;
            }

            bool examinedWardClue = HypothesisService.HasExaminedInLayerB(state, ObservationRegistry.Bed4Traces) ||
                                    HypothesisService.HasExaminedInLayerB(state, ObservationRegistry.Bed4Card) ||
                                    HypothesisService.HasExaminedInLayerB(state, ObservationRegistry.Bed2Transfer) ||
                                    HypothesisService.HasExaminedInLayerB(state, ObservationRegistry.Bed3Meds);
            if (layerB && tookBoard && state.Happened("CH01-T06") && !g01Done && examinedWardClue &&
                state.HasDreadEvent(DreadCatalog.B04TrolleyStop) &&
                (zone == "LOC03" || (zone == "HALL" && feet.z > 2.5f && feet.z < 7.5f)) &&
                CanFire(state, DreadCatalog.B04TrolleyNear))
            {
                Fire(state, DreadCatalog.B04TrolleyNear, persistent: true, () =>
                {
                    audioBus?.From("trolleyRoll", new Vector3(4.5f, 0.65f, 5.1f), 0.54f);
                    onCaption?.Invoke("[Tiếng bánh xe đẩy vọng lại gần hơn dọc hành lang, rồi dừng hẳn ngoài cửa Phòng 101.]");
                });
                return;
            }

            if (layerB && tookBoard && !g01Done && zone == "LOC03" && stillnessSeconds > 1.2f &&
                state.HasDreadEvent(DreadCatalog.B04TrolleyNear) &&
                CanFire(state, DreadCatalog.B04TrolleyClose))
            {
                Fire(state, DreadCatalog.B04TrolleyClose, persistent: true, () =>
                {
                    audioBus?.From("trolleyRoll", new Vector3(5.45f, 0.62f, 4.75f), 0.36f);
                    onCaption?.Invoke("[Ngay ngoài ngưỡng cửa Phòng 101 khẽ có tiếng khung kim loại chạm nhẹ rồi im bặt.]");
                });
                return;
            }

            // 2b. Peak Horror #1 (P-SC05): Bed-4 Alcove Seated Silhouette (<1.8s, never forces camera turn!)
            if (layerB && tookBoard && !collapsedToR && zone == "LOC03" &&
                HypothesisService.HasExaminedInLayerB(state, ObservationRegistry.Bed4Card) &&
                (scene.deniedBay == null || !scene.deniedBay.activeInHierarchy) &&
                bed4LureWindow <= 0f && bed4SilhouetteTimer <= 0f &&
                CanFire(state, DreadCatalog.B05CurtainSilhouette))
            {
                // Subtle acoustic lure first; silhouette only manifests if Minh turns toward Bed 4 naturally.
                audioBus?.PulseAmbienceDip(2.2f, 0.24f);
                audioBus?.From("curtainSlide", new Vector3(11.75f, 1.38f, 3.85f), 0.32f);
                onCaption?.Invoke("[Từ góc Giường 4 khẽ vang lên tiếng móc rèm rung rất nhẹ.]");
                bed4LureWindow = 6.5f;
                globalCooldown = Mathf.Max(globalCooldown, 12f);
                return;
            }

            // 3. B04/B05: Subtle Bed-4 privacy curtain draft when standing attentively inside Room 101
            if (layerB && tookBoard && zone == "LOC03" &&
                (stillnessSeconds > 1.2f || interactionIntensity > 0.25f) &&
                GazeAlignment(new Vector3(11.0f, 1.3f, 4.7f)) > 0.25f &&
                CanFire(state, DreadCatalog.B04CurtainBreath))
            {
                Fire(state, DreadCatalog.B04CurtainBreath, persistent: true, () =>
                {
                    if (!reducedMotion) scene.interactables?.PulseCurtainDraft();
                    audioBus?.From("curtainSlide", new Vector3(10.6f, 1.5f, 4.7f), 0.36f);
                    onCaption?.Invoke("[Tấm rèm cuối phòng 101 khẽ lay dù không có gió lùa.]");
                });
                return;
            }

            // 4. B05: Ceiling fan deceleration when player has examined at least 1 clue in B
            if (layerB && tookBoard && state.observed.Count >= 1 && (zone == "LOC03" || zone == "STAFF") &&
                stillnessSeconds > 1.0f &&
                CanFire(state, DreadCatalog.B05FanDecelerate))
            {
                Fire(state, DreadCatalog.B05FanDecelerate, persistent: true, () =>
                {
                    scene.motion?.SlowFans(7.5f);
                    onCaption?.Invoke("[Tiếng quạt trần chậm hẳn đi vài nhịp, ánh đèn tuýp rung nhẹ.]");
                });
                return;
            }

            // 5. B05: Soft muffled distant footsteps on exploration backtrack (STAFF alcove or corridor) — NO key jingle!
            if (layerB && tookBoard && (zone == "STAFF" || (zone == "HALL" && BacktrackCount >= 1)) &&
                CanFire(state, DreadCatalog.B05DistantSteps))
            {
                Fire(state, DreadCatalog.B05DistantSteps, persistent: true, () =>
                {
                    Vector3 origin = zone == "STAFF" ? new Vector3(4.0f, 1.0f, 13.5f) : new Vector3(0.4f, 1.0f, 12.0f);
                    audioBus?.From("distantSteps", origin, 0.56f);
                    onCaption?.Invoke("[Vài bước chân rất khẽ vang lên từ đầu hành lang phía trên rồi tắt hẳn.]");
                });
                return;
            }

            // 6. B06: PA static crackle right after submitting F101 as player moves back through corridor
            if (layerB && g01Done && !collapsedToR && zone == "HALL" && feet.z < 6.5f &&
                CanFire(state, DreadCatalog.B06PaInterference))
            {
                Fire(state, DreadCatalog.B06PaInterference, persistent: true, () =>
                {
                    Vector3 paPos = scene.PaSpeakerAnchor != null ? scene.PaSpeakerAnchor.transform.position : new Vector3(4.0f, 2.7f, 2.5f);
                    audioBus?.From("paStatic", paPos, 0.48f);
                    onCaption?.Invoke("[Loa hành lang xẹt lên một nhịp rè ngắn rồi lặng ngắt.]");
                });
                return;
            }

            // 7. B05/B06: Single crisp tick from the stopped 03:17 Reception wall clock — STRICTLY LAYER B ONLY!
            if (layerB && !collapsedToR && tookBoard && zone == "LOC02" &&
                (BacktrackCount >= 1 || g01Done) &&
                GazeAlignment(new Vector3(1.2f, 2.0f, -5.8f)) > 0.20f &&
                CanFire(state, DreadCatalog.B06ClockTick))
            {
                Fire(state, DreadCatalog.B06ClockTick, persistent: true, () =>
                {
                    audioBus?.From("clockTick", new Vector3(1.2f, 2.0f, -5.8f), 0.72f);
                    onCaption?.Invoke("[Một tiếng tích khô khốc phát ra từ chiếc đồng hồ đứng kim ở 03:17.]");
                });
                return;
            }

            // 8. B07: Restrained distant thunder/rain hush as Minh steps out into empty R courtyard after collapse
            if (!layerB && collapsedToR && !state.Happened("CH01-T09") &&
                feet.z < -10.5f && feet.z > -18.5f &&
                CanFire(state, DreadCatalog.B07GateRainHush))
            {
                Fire(state, DreadCatalog.B07GateRainHush, persistent: true, () =>
                {
                    audioBus?.From("distantThunder", new Vector3(2.0f, 7.0f, -26.0f), 0.52f);
                    onCaption?.Invoke("[Tiếng sấm rền rất xa ngoài cổng sắt, mưa lạnh quất rào rạt lên sân vắng.]");
                });
            }
        }

        internal static string F101ReactionId(FormStatus status) => status switch
        {
            FormStatus.Affirmed => DreadCatalog.F101AffirmedSuffocation,
            FormStatus.Unverified => DreadCatalog.F101UnverifiedStillness,
            FormStatus.Refuted => DreadCatalog.F101RefutedCrack,
            _ => ""
        };

        internal bool TryPlayF101Reaction()
        {
            var state = stateGetter?.Invoke();
            if (state == null || state.chapter != "CH01" || state.layer != Reality.B ||
                state.Happened("CH01-T08") || !GateService.G01(state) || suppressionCheck?.Invoke() == true)
                return false;

            string id = F101ReactionId(state.f101.current);
            if (!CanFire(state, id)) return false;

            switch (state.f101.current)
            {
                case FormStatus.Affirmed:
                    audioBus?.From("partitionKnock", new Vector3(12.14f, 1.25f, 4.32f), 0.90f);
                    onCaption?.Invoke("[Phía sau vách ngăn vừa dựng kín Giường 4 vang lên hai tiếng gõ trầm, nghẹt hẳn lại.]");
                    break;
                case FormStatus.Unverified:
                    scene.motion?.SlowFans(4.5f);
                    audioBus?.From("clockTick", new Vector3(9.6f, 2.1f, 5.4f), 0.62f);
                    onCaption?.Invoke("[Trong phòng chợt lặng, cánh quạt trên trần quay chậm lại.]");
                    break;
                case FormStatus.Refuted:
                    scene.motion?.BlackoutTubes(0.35f);
                    audioBus?.From("distantThunder", new Vector3(9.8f, 3.2f, 10.5f), 0.34f);
                    onCaption?.Invoke("[Đèn đầu Giường 4 chớp một nhịp; thoảng nghe tiếng mưa lạnh từ hành lang.]");
                    break;
            }

            LastTriggeredId = id;
            onOneTimeTriggered?.Invoke(id);
            return true;
        }

        private bool CanFire(Investigation state, string id) =>
            DreadCatalog.IsRegistered(id) && !state.HasDreadEvent(id);

        private void Fire(Investigation state, string id, bool persistent, Action effect)
        {
            LastTriggeredId = id;
            EventsFiredThisSession++;
            // Slight deterministic jitter (18..23s) derived from Seed so QA runs remain 100% reproducible!
            float jitter = (float)(rng.NextDouble() * 5.0);
            globalCooldown = DefaultGlobalCooldown + jitter;
            quietTimer = DefaultQuietWindow;
            effect?.Invoke();
            if (persistent)
                onOneTimeTriggered?.Invoke(id);
        }

        private void ExecuteDeferredShift(Investigation state, string dreadId, bool reducedMotion)
        {
            Fire(state, dreadId, persistent: true, () =>
            {
                if (dreadId == DreadCatalog.ShiftCurtainCreep)
                {
                    scene.interactables?.NudgeCurtainClosed(0.32f, audioBus);
                    onCaption?.Invoke("[Tấm rèm Giường 4 vừa tự khép lại một đoạn ngay sau khi hạ tờ giấy xuống.]");
                }
                else if (dreadId == DreadCatalog.ShiftTubeBlackout)
                {
                    scene.motion?.BlackoutTubes(0.60f);
                    audioBus?.From("paStatic", new Vector3(9.2f, 3.4f, 5.0f), 0.42f);
                    onCaption?.Invoke(reducedMotion
                        ? "[Ánh đèn tuýp trên trần dịu sẫm xuống một nhịp kèm tiếng chấn lưu rè nhẹ.]"
                        : "[Đèn tuýp trên trần phụt tắt 0,6 giây rồi bật lại kèm tiếng chấn lưu rè nhẹ.]");
                }
                else if (dreadId == DreadCatalog.ShiftHallChair)
                {
                    audioBus?.From("chairScrape", new Vector3(4.0f, 0.45f, 5.6f), 0.78f);
                    onCaption?.Invoke("[Ngoài hành lang vang lên tiếng chân ghế kéo khẽ trên nền gạch bông… rồi im bặt.]");
                }
            });
        }

        /// <summary>
        /// Deterministic QA probe: forces evaluation of a specific dread event if eligible and unsuppressed.
        /// Returns true if triggered cleanly, false if already persisted or suppressed.
        /// </summary>
        public bool TryTriggerDeterministicForQA(string dreadId)
        {
            var state = stateGetter?.Invoke();
            if (state == null || !CanFire(state, dreadId)) return false;
            if (suppressionCheck != null && suppressionCheck()) return false;
            if (dreadId == DreadCatalog.B06ClockTick && (state.layer != Reality.B || state.Happened("CH01-T08")))
                return false;
            if (dreadId == DreadCatalog.B05CurtainSilhouette)
            {
                if (state.layer != Reality.B || state.Happened("CH01-T08") ||
                    !HypothesisService.HasExaminedInLayerB(state, ObservationRegistry.Bed4Card) ||
                    (scene.deniedBay != null && scene.deniedBay.activeInHierarchy))
                    return false;
                TriggerBed4Silhouette(state);
                return true;
            }
            if (dreadId is DreadCatalog.ShiftCurtainCreep or DreadCatalog.ShiftTubeBlackout or DreadCatalog.ShiftHallChair)
            {
                ExecuteDeferredShift(state, dreadId, state.settings.reduceMotion);
                return true;
            }
            Fire(state, dreadId, persistent: true, () =>
            {
                if (dreadId == DreadCatalog.B06ClockTick)
                    audioBus?.From("clockTick", new Vector3(1.2f, 2.0f, -5.8f), 0.55f);
                else if (dreadId == DreadCatalog.B05DistantSteps)
                    audioBus?.From("distantSteps", new Vector3(4.0f, 1.0f, 13.5f), 0.50f);
            });
            return true;
        }

        private void UpdateActiveEffects(float dt)
        {
            if (trolleyFlashTimer > 0f)
            {
                trolleyFlashTimer -= dt;
                if (scene.TrolleyShadowProp != null)
                {
                    float progress = trolleyFlashDuration > 0.01f
                        ? Mathf.Clamp01(1f - trolleyFlashTimer / trolleyFlashDuration)
                        : 1f;
                    scene.TrolleyShadowProp.transform.position = Vector3.Lerp(
                        new Vector3(1.2f, 0f, 17.2f), new Vector3(7.2f, 0f, 17.2f), progress);
                    if (trolleyFlashTimer <= 0f)
                        scene.TrolleyShadowProp.SetActive(false);
                }
            }

            if (bed4LureWindow > 0f)
            {
                bed4LureWindow -= dt;
                var state = stateGetter?.Invoke();
                bool validContext = state != null && state.layer == Reality.B && !state.Happened("CH01-T08") &&
                                    (scene.deniedBay == null || !scene.deniedBay.activeInHierarchy) &&
                                    (suppressionCheck == null || !suppressionCheck());
                if (!validContext)
                {
                    bed4LureWindow = 0f;
                }
                else if (GazeAlignment(new Vector3(11.82f, 1.05f, 3.68f)) > 0.66f &&
                         CanFire(state, DreadCatalog.B05CurtainSilhouette))
                {
                    bed4LureWindow = 0f;
                    TriggerBed4Silhouette(state);
                }
            }

            if (bed4SilhouetteTimer > 0f)
            {
                bed4SilhouetteTimer -= dt;
                if (bed4SilhouetteRoot != null && bed4SilhouetteMat != null)
                {
                    float u = 1f - Mathf.Clamp01(bed4SilhouetteTimer / Mathf.Max(0.01f, bed4SilhouetteDuration));
                    // Gentle bell curve: appear quickly, hold briefly, dissolve before 1.7s
                    float alpha = u < 0.22f ? Mathf.Lerp(0f, 0.56f, u / 0.22f)
                        : u < 0.58f ? 0.56f
                        : Mathf.Lerp(0.56f, 0f, (u - 0.58f) / 0.42f);
                    bed4SilhouetteMat.SetColor("_BaseColor", new Color(0.03f, 0.04f, 0.045f, alpha));
                    if (bed4SilhouetteTimer <= 0f)
                        bed4SilhouetteRoot.SetActive(false);
                }
            }

            if (gateShadowTimer > 0f)
            {
                gateShadowTimer -= dt;
                if (gateBarShadow != null && gateBarShadowMat != null)
                {
                    float u = 1f - Mathf.Clamp01(gateShadowTimer / 0.58f);
                    gateBarShadow.transform.position = Vector3.Lerp(
                        new Vector3(3.15f, 1.15f, -20.52f), new Vector3(4.95f, 1.15f, -20.52f), u);
                    float a = Mathf.Sin(u * Mathf.PI) * 0.52f;
                    gateBarShadowMat.SetColor("_BaseColor", new Color(0.02f, 0.025f, 0.03f, a));
                    if (gateShadowTimer <= 0f)
                        gateBarShadow.SetActive(false);
                }
            }
        }

        private void TriggerBed4Silhouette(Investigation state)
        {
            EnsureBed4Silhouette();
            LastPeakScareUnscaledTime = Time.unscaledTime;
            Fire(state, DreadCatalog.B05CurtainSilhouette, persistent: true, () =>
            {
                if (bed4SilhouetteRoot != null)
                {
                    bed4SilhouetteRoot.SetActive(true);
                    bed4SilhouetteDuration = state.settings.reduceMotion ? 1.25f : 1.62f;
                    bed4SilhouetteTimer = bed4SilhouetteDuration;
                }
                onCaption?.Invoke("[Trong góc Giường 4 thoáng hiện một bóng người ngồi lặng rồi tan hẳn vào khoảng tối.]");
            });
        }

        private void EnsureBed4Silhouette()
        {
            if (bed4SilhouetteRoot != null) return;
            bed4SilhouetteTex = BuildSilhouetteTexture(seated: true);
            bed4SilhouetteMat = CreateTransparentShadowMat("CTCC_Bed4SilhouetteMat", bed4SilhouetteTex);

            bed4SilhouetteRoot = new GameObject("P_SC05_Bed4Silhouette");
            bed4SilhouetteRoot.transform.position = new Vector3(11.82f, 0f, 3.68f);
            bed4SilhouetteRoot.transform.rotation = Quaternion.LookRotation(new Vector3(-1f, 0f, 0.25f));

            // Soft, unlit, edge-feathered seated human shadow card (no facial detail, no collider, vanishes <1.8s)
            AddShadowCard(bed4SilhouetteRoot, "SeatedShadowFront", new Vector3(0f, 0.88f, 0.02f), new Vector3(0.72f, 1.18f, 1f), Quaternion.identity, bed4SilhouetteMat);
            AddShadowCard(bed4SilhouetteRoot, "SeatedShadowDepth", new Vector3(0f, 0.88f, 0.02f), new Vector3(0.64f, 1.16f, 1f), Quaternion.Euler(0f, 34f, 0f), bed4SilhouetteMat);
            bed4SilhouetteRoot.SetActive(false);
        }

        private void EnsureGateBarShadow()
        {
            if (gateBarShadow != null) return;
            gateBarShadowTex = BuildSilhouetteTexture(seated: false);
            gateBarShadowMat = CreateTransparentShadowMat("CTCC_GateSlamShadowMat", gateBarShadowTex);
            gateBarShadow = new GameObject("B07_GateSlamShadow");
            AddShadowCard(gateBarShadow, "PassingShadow", Vector3.zero, new Vector3(0.95f, 1.88f, 1f), Quaternion.identity, gateBarShadowMat);
            gateBarShadow.SetActive(false);
        }

        private static Material CreateTransparentShadowMat(string name, Texture2D alphaMap)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Universal Render Pipeline/Lit");
            var m = new Material(shader) { name = name };
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Cull", 0f);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
            m.SetColor("_BaseColor", new Color(0.02f, 0.028f, 0.032f, 0f));
            if (alphaMap != null) m.SetTexture("_BaseMap", alphaMap);
            return m;
        }

        private static void AddShadowCard(GameObject parent, string name, Vector3 pos, Vector3 scale, Quaternion rot, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        private static Texture2D BuildSilhouetteTexture(bool seated)
        {
            const int w = 96;
            const int h = 144;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, mipChain: false)
            {
                name = seated ? "CTCC_SeatedShadowTex" : "CTCC_PassingShadowTex",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float v = (y + 0.5f) / h; // 0 bottom, 1 top
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w;
                    float dx = u - 0.5f;
                    float d;
                    if (seated)
                    {
                        // Soft hunched seated figure: head (0.78), sloped shoulders (0.60), torso & lap (0.36..0.58)
                        float head = EllipseSdf(dx, v - 0.77f, 0.125f, 0.115f);
                        float neck = EllipseSdf(dx, v - 0.67f, 0.075f, 0.06f);
                        float shoulders = EllipseSdf(dx, v - 0.56f, 0.28f, 0.135f);
                        float torso = EllipseSdf(dx, v - 0.40f, 0.24f, 0.22f);
                        float lap = EllipseSdf(dx - 0.02f, v - 0.22f, 0.30f, 0.14f);
                        d = Mathf.Min(head, Mathf.Min(neck, Mathf.Min(shoulders, Mathf.Min(torso, lap))));
                    }
                    else
                    {
                        // Tall fast-passing human figure outside gate bars (slight forward motion lean)
                        float lean = (v - 0.5f) * 0.07f;
                        float head = EllipseSdf(dx - lean, v - 0.83f, 0.105f, 0.088f);
                        float shoulders = EllipseSdf(dx - lean, v - 0.68f, 0.23f, 0.12f);
                        float coat = EllipseSdf(dx - lean * 0.5f, v - 0.42f, 0.20f, 0.30f);
                        float stride = EllipseSdf(dx, v - 0.16f, 0.22f, 0.16f);
                        d = Mathf.Min(head, Mathf.Min(shoulders, Mathf.Min(coat, stride)));
                    }
                    // Wide feathered edge so the silhouette reads as ambient darkness rather than a cutout sticker
                    float core = 1f - Mathf.SmoothStep(-0.08f, 0.14f, d);
                    float halo = (1f - Mathf.SmoothStep(0.0f, 0.30f, d)) * 0.28f;
                    float edgeFade = Mathf.SmoothStep(0f, 0.12f, u) * Mathf.SmoothStep(1f, 0.88f, u) *
                                     Mathf.SmoothStep(0f, 0.14f, v) * Mathf.SmoothStep(1f, 0.90f, v);
                    float a = Mathf.Clamp01((core + halo) * edgeFade);
                    px[y * w + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(px);
            tex.Apply(false, true);
            return tex;
        }

        private static float EllipseSdf(float x, float y, float rx, float ry)
        {
            float nx = x / rx;
            float ny = y / ry;
            return (Mathf.Sqrt(nx * nx + ny * ny) - 1f) * Mathf.Min(rx, ry);
        }

        private void OnDestroy()
        {
            if (bed4SilhouetteRoot != null) Destroy(bed4SilhouetteRoot);
            if (gateBarShadow != null) Destroy(gateBarShadow);
            if (bed4SilhouetteMat != null) Destroy(bed4SilhouetteMat);
            if (gateBarShadowMat != null) Destroy(gateBarShadowMat);
            if (bed4SilhouetteTex != null) Destroy(bed4SilhouetteTex);
            if (gateBarShadowTex != null) Destroy(gateBarShadowTex);
        }
    }
}
