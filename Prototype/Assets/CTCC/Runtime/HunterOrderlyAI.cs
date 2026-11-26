using System;
using UnityEngine;
using UnityEngine.AI;

namespace CTCC
{
    public enum OrderlyState
    {
        Patrol = 0,
        Suspicious = 1,
        Investigate = 2,
        Chase = 3,
        Search = 4,
        Return = 5
    }

    public enum AIRole
    {
        None = 0,
        Hunter = 1,
        Orderly = 2,
        Unrecorded = 3
    }

    public enum OrderlyStop
    {
        Corridor = 0,
        Pharmacy = 1,
        WingBDoor = 2
    }

    /// <summary>
    /// Regional AI controller for Ca Trực Cuối Cùng (TASK011-A..E, Core 02 §4.2, Core 06 §03).
    /// Cleanly separates three canonical roles without cross-floor/in-sight actor teleporting:
    /// - CHAR005 Hunter (GatekeeperActor): 6-state Patrol -> Suspicious (2s warn) -> Investigate -> Chase -> Search (12.5s) -> Return;
    ///   12m/100deg occluded LOS (0.70s), floor-bounded occluded hearing (walk 3m / crouch 1.5m / run 9m / door 12m / radio 14m),
    ///   Story Mode non-capture, safe-zone breakoff, anti-camp search, and non-lethal recovery.
    /// - CHAR006 Orderly (OrderlyActor + movable OrderlyCart): strict 3-waypoint schedule (Hành lang / Quầy thuốc / Cửa Khu B,
    ///   4-8s dwell), Route A (default) vs Route B (observable reroute after valid PZ04 room-status cross-check), 2s warning,
    ///   gentle station reset without evidence/journal loss, NEVER chases or hunts distractions.
    /// - CHAR007 Unrecorded (UnrecordedActor): harmless translucent apparition in LOC10 (strictly outside AP09 and never C12),
    ///   max 2 calm loops then fades out, NEVER chases, warns, or blocks the player.
    /// </summary>
    public sealed class HunterOrderlyAI : MonoBehaviour
    {
        public const float SightRange = 12.0f;
        public const float SightFovDegrees = 100.0f;
        public const float SightAccumulationSeconds = 0.70f;
        public const float PreChaseWarningSeconds = 2.00f;
        public const float LoseSightChaseSeconds = 2.10f;
        public const float SearchDurationSeconds = 12.5f;
        public const float ReturnCooldownSeconds = 2.50f;
        public const float PostRecoveryGraceSeconds = 60.0f;

        public const float NoiseWalkRadius = 3.0f;
        public const float NoiseCrouchRadius = 1.5f;
        public const float NoiseRunRadius = 9.0f;
        public const float NoiseDoorRadius = 12.0f;
        public const float NoiseDistractionRadius = 14.0f;

        public const float PatrolSpeed = 1.65f;
        public const float InvestigateSpeed = 1.85f;
        public const float ChaseSpeed = 3.60f;
        public const float OrderlySpeed = 1.24f;
        public const float UnrecordedSpeed = 0.78f;

        public const int MaxUnrecordedLoops = 2;
        private const float FloorIsolationMaxDeltaY = 2.2f;
        private const float OrderlySightRange = 5.5f;
        private const float OrderlyInterceptRange = 3.25f;
        private const float CartForwardOffset = 0.82f;

        private struct OrderlyWaypoint
        {
            public OrderlyStop Stop;
            public string Label;
            public Vector3 Position;
            public float DwellSeconds;
            public float FacingYaw;
        }

        private static readonly OrderlyWaypoint[] OrderlyDefaultRoute =
        {
            new() { Stop = OrderlyStop.Corridor, Label = "Hành lang phòng trực", Position = new(4.0f, 4.0f, 40.4f), DwellSeconds = 5.0f, FacingYaw = 180f },
            new() { Stop = OrderlyStop.Pharmacy, Label = "Quầy thuốc Khu B", Position = new(5.8f, 4.0f, 36.8f), DwellSeconds = 6.0f, FacingYaw = 90f },
            new() { Stop = OrderlyStop.WingBDoor, Label = "Cửa Khu B", Position = new(-0.8f, 4.0f, 36.6f), DwellSeconds = 4.5f, FacingYaw = -90f },
        };

        private static readonly OrderlyWaypoint[] OrderlyReroutedRoute =
        {
            new() { Stop = OrderlyStop.Pharmacy, Label = "Quầy thuốc Khu B", Position = new(5.8f, 4.0f, 36.8f), DwellSeconds = 7.0f, FacingYaw = 90f },
            new() { Stop = OrderlyStop.WingBDoor, Label = "Cửa Khu B", Position = new(-0.8f, 4.0f, 36.6f), DwellSeconds = 5.5f, FacingYaw = -90f },
        };

        // Canonical B1 Gatekeeper round: guards LOC11 (Khu C G06 vestibule, occluded from AP08/CH04_POWER)
        // and sweeps north LOC09 without staring straight down the empty south room on spawn.
        private static readonly Vector3[] ServiceRound =
        {
            new(13.2f, -3.4f, 40.2f),
            new(13.4f, -3.4f, 44.2f),
            new(12.6f, -3.4f, 42.2f),
            new(8.8f, -3.4f, 42.8f)
        };

        // CH05 B1 Gatekeeper round: patrols inside LOC11 (~5.4s window) before sweeping LOC09 south
        // along doorway centerline z=42.0 in both directions, staying clear of AP08, CH05_BLUEPRINTS,
        // LOC09_VALVE, and the LOC11 north door (x=12.5, z=45.0) to the E05b gallery.
        private static readonly Vector3[] Ch05ServiceRound =
        {
            new(13.2f, -3.4f, 40.2f),
            new(13.4f, -3.4f, 43.6f),
            new(13.2f, -3.4f, 40.2f),
            new(12.2f, -3.4f, 42.0f),
            new(9.1f, -3.4f, 42.0f),
            new(9.1f, -3.4f, 34.6f),
            new(9.1f, -3.4f, 42.0f),
            new(12.2f, -3.4f, 42.0f)
        };

        private static readonly Vector3[] Ch03FallbackRound =
        {
            new(2.6f, 8.0f, 31.0f),
            new(2.5f, 8.0f, 36.5f),
            new(-0.35f, 8.0f, 37.25f)
        };

        // Strictly outside AP09 safe pad (1.5, -3.4, 39.0; r=1.65m) and clear of dry cabinet (-0.57, 39.0).
        private static readonly Vector3[] MorgueTrace =
        {
            new(-0.2f, -3.4f, 41.15f),
            new(3.3f, -3.4f, 41.15f)
        };

        private static readonly string[] SafePads = { "AP02", "AP04", "AP05", "AP06", "AN03", "AP07", "AP08", "AP09", "AP10", "AP11" };

        private HospitalScene scene;
        private VoiceDirector voice;
        private FirstPerson player;
        private HospitalUI ui;
        private PhysicalInspector inspector;

        private AIRole activeRole = AIRole.None;
        private CharacterActor activeActor;
        private string activeFloorKey = "";
        private OrderlyState currentState = OrderlyState.Patrol;
        private int patrolIndex;
        private float sightTimer;
        private float stateTimer;
        private float loseSightTimer;
        private float keyJingleTimer;
        private float returnCooldownUntil;
        private float graceUntil;
        private bool waitingAtStop;
        private bool orderlyRouteAdjusted;
        private bool spokeCh03Warning;
        private bool suspiciousFromHearing;
        private Vector3 stimulusPosition;
        private Vector3 lastSeenPosition;
        private Vector3 searchSweepTarget;
        private bool hasSearchSweepTarget;
        private int searchSweepSteps;

        private NavMeshPath navPath;
        private Vector3[] corners = Array.Empty<Vector3>();
        private Vector3 pathDestination;
        private float repathAt;
        private float movementBlockedFor;
        private int cornerIndex;

        // Independent CHAR007 Unrecorded apparition lifecycle (max 2 loops, never shares Hunter/Orderly transform).
        private bool unrecordedActive;
        private bool unrecordedFinished;
        private bool unrecordedCaptioned;
        private int unrecordedTargetIdx = 1;
        private int unrecordedHalfLaps;
        private float unrecordedAlpha;
        private NavMeshPath unrecordedNavPath;

        public OrderlyState CurrentState => currentState;
        public AIRole ActiveRole => activeRole;
        public CharacterActor ActiveActor => activeActor;
        public bool IsActive => activeRole != AIRole.None && activeActor != null;
        public Vector3 StimulusPosition => stimulusPosition;
        public Vector3 LastSeenPosition => lastSeenPosition;
        public float SightAccumulated => sightTimer;
        public bool OrderlyRouteAdjusted => orderlyRouteAdjusted;
        public OrderlyStop CurrentOrderlyStop => ActiveOrderlyRoute[patrolIndex % ActiveOrderlyRoute.Length].Stop;
        public string CurrentOrderlyStopLabel => ActiveOrderlyRoute[patrolIndex % ActiveOrderlyRoute.Length].Label;
        public bool OrderlyWaitingAtStop => activeRole == AIRole.Orderly && waitingAtStop;
        public bool UnrecordedActive => unrecordedActive;
        public int UnrecordedLoopsCompleted => unrecordedHalfLaps / 2;
        public bool InReturnCooldown => Time.unscaledTime < returnCooldownUntil;
        public float GraceRemaining => Mathf.Max(0f, graceUntil - Time.unscaledTime);
        public int PathQueries { get; private set; }
        public int InvalidPaths { get; private set; }
        public int BlockedSteps { get; private set; }
        public int WarningCount { get; private set; }
        public int EncounterResetCount { get; private set; }
        public int DoorNoiseHeardCount { get; private set; }

        private OrderlyWaypoint[] ActiveOrderlyRoute => orderlyRouteAdjusted ? OrderlyReroutedRoute : OrderlyDefaultRoute;

        public void Initialize(HospitalScene world, VoiceDirector voiceDirector, FirstPerson fps,
            HospitalUI hospitalUi, PhysicalInspector physicalInspector)
        {
            navPath ??= new NavMeshPath();
            unrecordedNavPath ??= new NavMeshPath();
            scene = world;
            voice = voiceDirector;
            player = fps;
            ui = hospitalUi;
            inspector = physicalInspector;
        }

        public void ResetForLoad()
        {
            DeactivatePrimary();
            StopUnrecorded(resetCompleted: true);
            graceUntil = 0f;
            returnCooldownUntil = 0f;
            spokeCh03Warning = false;
            unrecordedCaptioned = false;
        }

        public void OnZoneUnregistering(ZoneRegion zone)
        {
            if (zone == null) return;
            if (activeActor != null && (zone.gatekeeperActor == activeActor || zone.orderlyActor == activeActor))
                DeactivatePrimary();
            else if (zone.zoneId == "LOC05" && (activeFloorKey is ("CH03-L3" or "CH05-L3") || activeRole == AIRole.Orderly))
                DeactivatePrimary();
            else if (zone.zoneId == "LOC09" && activeFloorKey is ("CH04-B1" or "CH05-B1"))
                DeactivatePrimary();

            if (zone.zoneId == "LOC09" || (zone.unrecordedActor != null && scene?.UnrecordedActor == zone.unrecordedActor))
                StopUnrecorded(resetCompleted: false);
        }

        public void SyncState(Investigation state)
        {
            if (scene == null || state == null) return;

            // Update CHAR007 Unrecorded independently so crossing LOC09 <-> LOC10 NEVER teleports Hunter!
            SyncUnrecorded(state);

            AIRole nextRole = AIRole.None;
            string nextFloorKey = "";
            if (state.layer == Reality.R && state.chapter == "CH03" && state.Happened("CH03-T06") &&
                !state.Happened("CH03-T07") && scene.HasZone("LOC05") &&
                (player == null || player.Feet.y > 6.2f))
            {
                nextRole = AIRole.Hunter;
                nextFloorKey = "CH03-L3";
            }
            else if (state.layer == Reality.R && state.chapter == "CH04" && !GateService.H05(state) && player != null)
            {
                if (player.Feet.y > 3.2f && player.Feet.y < 6.2f && scene.HasZone("LOC05"))
                {
                    nextRole = AIRole.Orderly;
                    nextFloorKey = "CH04-L2";
                }
                else if (player.Feet.y < -2.4f && scene.HasZone("LOC09"))
                {
                    nextRole = AIRole.Hunter;
                    nextFloorKey = "CH04-B1";
                }
            }
            else if (state.layer == Reality.R && state.chapter == "CH05" && player != null)
            {
                if (state.Happened(ChapterFive.EvReturnR) && !GateService.G05(state) &&
                    player.Feet.y < -2.4f && scene.HasZone("LOC09"))
                {
                    nextRole = AIRole.Hunter;
                    nextFloorKey = "CH05-B1";
                }
                else if (GateService.G05(state) && !state.Happened(ChapterFive.EvHandoff) &&
                    player.Feet.y > 6.2f && player.Feet.x > -4.2f && scene.HasZone("LOC05"))
                {
                    nextRole = AIRole.Hunter;
                    nextFloorKey = "CH05-L3";
                }
            }

            if (nextRole == AIRole.None)
            {
                DeactivatePrimary();
                return;
            }

            CharacterActor desiredActor = ResolveActorFor(nextRole);
            if (desiredActor == null)
            {
                DeactivatePrimary();
                return;
            }

            bool routeChanged = nextRole == AIRole.Orderly &&
                orderlyRouteAdjusted != state.Happened("PZ04.ScheduleUpdated");
            if (routeChanged)
            {
                orderlyRouteAdjusted = state.Happened("PZ04.ScheduleUpdated");
                if (activeRole == AIRole.Orderly && activeActor != null)
                {
                    patrolIndex = 0; // Pharmacy stop (clears LOC06 corridor observably!)
                    waitingAtStop = false;
                    TransitionTo(OrderlyState.Patrol, 0f);
                }
            }

            bool needsActivation = activeRole != nextRole || activeFloorKey != nextFloorKey ||
                activeActor != desiredActor || !desiredActor.gameObject.activeSelf;
            if (!needsActivation)
            {
                activeActor.ReducedMotion = state.settings.reduceMotion;
                return;
            }

            DeactivatePrimary();
            orderlyRouteAdjusted = state.Happened("PZ04.ScheduleUpdated");
            activeRole = nextRole;
            activeFloorKey = nextFloorKey;
            activeActor = desiredActor;
            currentState = OrderlyState.Patrol;
            sightTimer = 0f;
            stateTimer = 0f;
            loseSightTimer = 0f;
            waitingAtStop = false;
            suspiciousFromHearing = false;
            hasSearchSweepTarget = false;
            searchSweepSteps = 0;
            InvalidatePath();

            int chosenIndex = PickSafeSpawnWaypointIndex(nextRole, nextFloorKey);
            if (chosenIndex < 0)
            {
                activeRole = AIRole.None;
                activeFloorKey = "";
                activeActor = null;
                return;
            }

            patrolIndex = chosenIndex;
            Vector3 startPt = GetPatrolPoint(patrolIndex);
            float startYaw = nextRole == AIRole.Orderly ? ActiveOrderlyRoute[patrolIndex % ActiveOrderlyRoute.Length].FacingYaw : 180f;
            activeActor.gameObject.SetActive(true);
            activeActor.ReducedMotion = state.settings.reduceMotion;
            activeActor.LookTarget = player != null && player.Eye != null ? player.Eye.transform : null;
            activeActor.transform.SetPositionAndRotation(startPt, Quaternion.Euler(0f, startYaw, 0f));
            activeActor.PlayAction("GK_StalkWalk", 0.15f, restart: true);
            SyncOrderlyCartPose(instant: true);
        }

        private CharacterActor ResolveActorFor(AIRole role) => role switch
        {
            AIRole.Hunter => scene.GatekeeperActor,
            AIRole.Orderly => scene.OrderlyActor != null ? scene.OrderlyActor : scene.GatekeeperActor,
            AIRole.Unrecorded => scene.UnrecordedActor,
            _ => null
        };

        private void DeactivatePrimary()
        {
            if (activeActor != null)
                activeActor.gameObject.SetActive(false);
            activeRole = AIRole.None;
            activeFloorKey = "";
            activeActor = null;
            currentState = OrderlyState.Patrol;
            sightTimer = 0f;
            stateTimer = 0f;
            loseSightTimer = 0f;
            waitingAtStop = false;
            suspiciousFromHearing = false;
            InvalidatePath();
        }

        private int PickSafeSpawnWaypointIndex(AIRole role, string floorKey)
        {
            int count = GetPatrolPointCount(role, floorKey);
            if (count <= 0) return -1;
            if (player == null) return 0;

            Vector3 feet = player.Feet;
            int bestUnseen = -1;
            float bestUnseenDist = -1f;
            int bestFarthest = -1;
            float bestFarthestDist = -1f;

            for (int i = 0; i < count; i++)
            {
                Vector3 pt = GetPatrolPoint(role, floorKey, i);
                // Ensure Orderly starts away from corridor chokepoint when player is at AP07 so cart rolls naturally into view.
                float dist = Vector3.Distance(feet, pt);
                if (dist < 3.2f) continue;
                bool visibleToPlayer = PlayerHasDirectView(pt);
                if (!visibleToPlayer && dist > bestUnseenDist)
                {
                    bestUnseenDist = dist;
                    bestUnseen = i;
                }
                if (dist > bestFarthestDist)
                {
                    bestFarthestDist = dist;
                    bestFarthest = i;
                }
            }
            if (bestUnseen >= 0) return bestUnseen;
            return bestFarthest;
        }

        private bool PlayerHasDirectView(Vector3 worldPos)
        {
            if (player == null || player.Eye == null) return false;
            Vector3 eye = player.Eye.transform.position;
            Vector3 target = worldPos + Vector3.up * 1.45f;
            Vector3 toTarget = target - eye;
            float dist = toTarget.magnitude;
            if (dist < 0.25f) return true;
            if (Vector3.Angle(player.Eye.transform.forward, toTarget / dist) > 65f) return false;
            return !Physics.Linecast(eye, target, HospitalScene.SolidMask, QueryTriggerInteraction.Ignore);
        }

        private void SyncUnrecorded(Investigation state)
        {
            var ghost = scene.UnrecordedActor;
            if (ghost == null)
            {
                unrecordedActive = false;
                return;
            }
            bool shouldRun = state.layer == Reality.R && state.chapter == "CH04" &&
                ChapterFour.MorguePower(state.servicePower) &&
                scene.HasZone("LOC09") && player != null &&
                player.Feet.y < -2.4f && player.Feet.x < 4.5f && !unrecordedFinished;

            if (!shouldRun)
            {
                if (unrecordedActive) StopUnrecorded(resetCompleted: false);
                return;
            }
            if (!unrecordedActive)
            {
                unrecordedActive = true;
                unrecordedTargetIdx = 1;
                unrecordedAlpha = 0.05f;
                ghost.gameObject.SetActive(true);
                ghost.ReducedMotion = state.settings.reduceMotion;
                ghost.transform.SetPositionAndRotation(MorgueTrace[0], Quaternion.Euler(0f, 90f, 0f));
                ghost.PlayAction("GK_StalkWalk", 0.20f, restart: true);
                ghost.SetApparitionAlpha(unrecordedAlpha);
                if (!unrecordedCaptioned)
                {
                    unrecordedCaptioned = true;
                    ui?.Caption("[Dư ảnh mờ của bệnh nhân Khu C lướt qua phía bắc nhà xác — vô hại, không truy đuổi.]", 4.5f, false);
                }
            }
        }

        private void StopUnrecorded(bool resetCompleted)
        {
            unrecordedActive = false;
            unrecordedAlpha = 0f;
            if (resetCompleted)
            {
                unrecordedFinished = false;
                unrecordedHalfLaps = 0;
                unrecordedTargetIdx = 1;
            }
            if (scene?.UnrecordedActor != null)
            {
                scene.UnrecordedActor.SetApparitionAlpha(0f);
                scene.UnrecordedActor.gameObject.SetActive(false);
            }
        }

        public static bool IsInSafeZone(Vector3 feet, HospitalScene world)
        {
            if (world == null) return false;
            foreach (string padId in SafePads)
            {
                if (world.TryPad(padId, out var pad) && pad != null)
                {
                    Vector3 d = feet - pad.position;
                    if (Mathf.Abs(d.y) < 0.8f && d.x * d.x + d.z * d.z <= 1.65f * 1.65f)
                        return true;
                }
            }
            return false;
        }

        public void TriggerDistraction(Vector3 worldPos, float radius = NoiseDistractionRadius)
        {
            // Strictly Hunter-only: Orderly follows fixed/updated schedule; Unrecorded is harmless apparition.
            if (activeRole != AIRole.Hunter || activeActor == null) return;
            Vector3 agentPos = activeActor.transform.position;
            if (Mathf.Abs(agentPos.y - worldPos.y) > FloorIsolationMaxDeltaY) return;
            float dist = Vector3.Distance(agentPos, worldPos);
            bool occluded = Physics.Linecast(agentPos + Vector3.up * 1.5f, worldPos + Vector3.up * 0.8f,
                HospitalScene.SolidMask, QueryTriggerInteraction.Ignore);
            float effectiveRadius = occluded ? radius * 0.65f : radius;
            if (dist <= effectiveRadius && currentState != OrderlyState.Chase)
            {
                stimulusPosition = worldPos;
                TransitionTo(OrderlyState.Investigate, 6.5f);
            }
        }

        public void NotifyDoorNoise(Vector3 worldPos)
        {
            if (activeRole != AIRole.Hunter || activeActor == null) return;
            if (currentState == OrderlyState.Chase || currentState == OrderlyState.Suspicious) return;
            Vector3 agentPos = activeActor.transform.position;
            if (Mathf.Abs(agentPos.y - worldPos.y) > FloorIsolationMaxDeltaY) return;
            bool occluded = Physics.Linecast(agentPos + Vector3.up * 1.4f, worldPos + Vector3.up * 1.0f,
                HospitalScene.SolidMask, QueryTriggerInteraction.Ignore);
            float radius = occluded ? NoiseDoorRadius * 0.5f : NoiseDoorRadius;
            if (Vector3.Distance(agentPos, worldPos) <= radius)
            {
                DoorNoiseHeardCount++;
                stimulusPosition = worldPos;
                suspiciousFromHearing = true;
                TransitionTo(OrderlyState.Suspicious, 0.65f);
                EmitHunterWarningCue(agentPos, hearingAlert: true);
            }
        }

        public bool CheckLineOfSight(Vector3 agentPos, Vector3 agentForward, Vector3 targetPos, float range = SightRange)
        {
            if (Mathf.Abs(agentPos.y - targetPos.y) > FloorIsolationMaxDeltaY) return false;
            Vector3 toTarget = targetPos - agentPos;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;
            if (Vector3.Distance(agentPos, targetPos) > range || dist < 0.05f) return false;
            float angle = Vector3.Angle(agentForward, toTarget / dist);
            if (angle > SightFovDegrees * 0.5f) return false;
            Vector3 eyeA = agentPos + Vector3.up * 1.62f;
            Vector3 eyeB = targetPos + Vector3.up * 1.45f;
            return !Physics.Linecast(eyeA, eyeB, HospitalScene.SolidMask, QueryTriggerInteraction.Ignore);
        }

        public void Tick(Investigation state, float dt, bool playerRunning, bool playerCrouching, bool playerMoving,
            Func<string, int, string> lineLookup)
        {
            if (state == null || player == null) return;

            bool paused = (ui != null && ui.IsModal) || (inspector != null && inspector.IsInspecting) ||
                state.layer == Reality.M || state.layer == Reality.B;

            // Tick CHAR007 Unrecorded independently (pauses calmly while reading/modals).
            TickUnrecorded(dt, paused);

            if (activeRole == AIRole.None || activeActor == null || !activeActor.gameObject.activeInHierarchy)
                return;

            if (paused)
            {
                if (activeActor.ActiveActionName != "GK_IdleStill")
                    activeActor.PlayAction("GK_IdleStill", 0.20f);
                return;
            }

            if (activeRole == AIRole.Orderly)
                TickOrderly(state, dt);
            else if (activeRole == AIRole.Hunter)
                TickHunter(state, dt, playerRunning, playerCrouching, playerMoving, lineLookup);
        }

        private void TickUnrecorded(float dt, bool paused)
        {
            if (!unrecordedActive || scene?.UnrecordedActor == null) return;
            var ghost = scene.UnrecordedActor;
            if (!ghost.gameObject.activeInHierarchy) return;
            if (paused)
            {
                if (ghost.ActiveActionName != "GK_IdleStill") ghost.PlayAction("GK_IdleStill", 0.20f);
                return;
            }

            float targetAlpha = unrecordedHalfLaps >= MaxUnrecordedLoops * 2 ? 0f : 0.55f;
            unrecordedAlpha = Mathf.MoveTowards(unrecordedAlpha, targetAlpha, dt * 0.45f);
            ghost.SetApparitionAlpha(unrecordedAlpha);

            if (unrecordedHalfLaps >= MaxUnrecordedLoops * 2)
            {
                if (unrecordedAlpha <= 0.02f)
                {
                    unrecordedFinished = true;
                    StopUnrecorded(resetCompleted: false);
                }
                return;
            }

            Vector3 pos = ghost.transform.position;
            Vector3 dest = MorgueTrace[unrecordedTargetIdx % MorgueTrace.Length];
            if ((dest - pos).sqrMagnitude <= 0.16f * 0.16f)
            {
                unrecordedHalfLaps++;
                unrecordedTargetIdx = (unrecordedTargetIdx + 1) % MorgueTrace.Length;
                return;
            }

            // Stay strictly on valid NavMesh inside LOC10 (never teleports across walls).
            Vector3 next = Vector3.MoveTowards(pos, dest, UnrecordedSpeed * dt);
            if (NavMesh.SamplePosition(next, out var sampled, 0.50f, NavMesh.AllAreas))
            {
                FaceTarget(ghost, dest, dt * 180f);
                ghost.transform.position = sampled.position;
                if (ghost.ActiveActionName != "GK_StalkWalk")
                    ghost.PlayAction("GK_StalkWalk", 0.22f);
            }
            else
            {
                StandStill(ghost);
            }
        }

        private void TickOrderly(Investigation state, float dt)
        {
            var actor = activeActor;
            Vector3 agentPos = actor.transform.position;
            Vector3 agentFwd = actor.transform.forward;
            Vector3 playerFeet = player.Feet;
            bool playerSafe = IsInSafeZone(playerFeet, scene) || Time.unscaledTime < graceUntil;

            if (playerSafe && currentState == OrderlyState.Suspicious)
            {
                sightTimer = 0f;
                TransitionTo(OrderlyState.Return, 2.5f);
            }

            bool seesPlayer = !playerSafe && CheckLineOfSight(agentPos, agentFwd, playerFeet, OrderlySightRange) &&
                Vector3.Distance(agentPos, playerFeet) <= OrderlyInterceptRange;

            if (seesPlayer && currentState == OrderlyState.Patrol)
            {
                sightTimer += dt;
                stimulusPosition = playerFeet;
                if (sightTimer >= 0.35f)
                {
                    WarningCount++;
                    TransitionTo(OrderlyState.Suspicious, PreChaseWarningSeconds);
                    scene.atmosphere?.From("trolleyRoll", agentPos + Vector3.up * 0.9f);
                    string dir = scene.atmosphere?.DirectionTo(agentPos) ?? "phía hành lang";
                    ui?.Caption($"[Xe thuốc dừng lại · {dir} — Cảnh báo 2 giây. Lùi lại hoặc chờ xe rẽ về quầy thuốc.]", 3.2f, true);
                }
            }
            else if (!seesPlayer)
            {
                sightTimer = Mathf.Max(0f, sightTimer - dt * 1.6f);
            }

            stateTimer = Mathf.Max(0f, stateTimer - dt);
            var route = ActiveOrderlyRoute;
            var stop = route[patrolIndex % route.Length];

            switch (currentState)
            {
                case OrderlyState.Patrol:
                    if (seesPlayer)
                    {
                        FaceTarget(actor, stimulusPosition, dt * 180f);
                        StandStill(actor);
                    }
                    else if (waitingAtStop)
                    {
                        actor.transform.rotation = Quaternion.RotateTowards(actor.transform.rotation,
                            Quaternion.Euler(0f, stop.FacingYaw, 0f), dt * 140f);
                        StandStill(actor);
                        if (stateTimer <= 0f)
                        {
                            waitingAtStop = false;
                            patrolIndex = (patrolIndex + 1) % route.Length;
                            InvalidatePath();
                        }
                    }
                    else if (MoveAlongNavMesh(actor, stop.Position, OrderlySpeed, dt))
                    {
                        waitingAtStop = true;
                        stateTimer = stop.DwellSeconds;
                        StandStill(actor);
                    }
                    break;

                case OrderlyState.Suspicious:
                    FaceTarget(actor, stimulusPosition, dt * 180f);
                    StandStill(actor);
                    if (!seesPlayer && stateTimer <= PreChaseWarningSeconds - 0.5f)
                    {
                        // Player stepped clear during warning: resume scheduled route without resetting!
                        TransitionTo(OrderlyState.Patrol, 0f);
                    }
                    else if (stateTimer <= 0f)
                    {
                        if (seesPlayer && !state.settings.storyMode)
                        {
                            ResetEncounter(state);
                            // Advance past corridor chokepoint so player is never trapped on recovery.
                            patrolIndex = (patrolIndex + 1) % route.Length;
                            TransitionTo(OrderlyState.Return, 3.0f);
                        }
                        else
                        {
                            if (state.settings.storyMode && seesPlayer)
                                ui?.Caption("[Chế độ Cốt truyện · Xe thuốc nhường đường qua hành lang.]", 3.2f, false);
                            patrolIndex = (patrolIndex + 1) % route.Length;
                            TransitionTo(OrderlyState.Patrol, 0f);
                        }
                    }
                    break;

                case OrderlyState.Return:
                default:
                    // Orderly NEVER chases or searches; Return smoothly rejoins the next scheduled stop.
                    if (MoveAlongNavMesh(actor, stop.Position, OrderlySpeed, dt) || stateTimer <= 0f)
                        TransitionTo(OrderlyState.Patrol, 0f);
                    break;
            }

            SyncOrderlyCartPose(instant: false);
        }

        private void SyncOrderlyCartPose(bool instant)
        {
            if (activeRole != AIRole.Orderly || activeActor == null || scene?.OrderlyCart == null) return;
            Vector3 desiredPos = activeActor.transform.position + activeActor.transform.forward * CartForwardOffset;
            desiredPos.y = activeActor.transform.position.y;
            // Prevent cart pushing through solid walls when Orderly turns near corners.
            Vector3 origin = activeActor.transform.position + Vector3.up * 0.55f;
            if (Physics.Raycast(origin, activeActor.transform.forward, out var hit, CartForwardOffset + 0.32f,
                HospitalScene.SolidMask, QueryTriggerInteraction.Ignore))
            {
                float safeDist = Mathf.Max(0.32f, hit.distance - 0.34f);
                desiredPos = activeActor.transform.position + activeActor.transform.forward * safeDist;
                desiredPos.y = activeActor.transform.position.y;
            }
            Quaternion desiredRot = activeActor.transform.rotation * Quaternion.Euler(0f, 90f, 0f);
            if (instant)
                scene.OrderlyCart.SetPositionAndRotation(desiredPos, desiredRot);
            else
            {
                scene.OrderlyCart.position = Vector3.Lerp(scene.OrderlyCart.position, desiredPos, Mathf.Clamp01(Time.deltaTime * 12f));
                scene.OrderlyCart.rotation = Quaternion.Slerp(scene.OrderlyCart.rotation, desiredRot, Mathf.Clamp01(Time.deltaTime * 10f));
            }
        }

        private void TickHunter(Investigation state, float dt, bool playerRunning, bool playerCrouching, bool playerMoving,
            Func<string, int, string> lineLookup)
        {
            var actor = activeActor;
            Vector3 agentPos = actor.transform.position;
            Vector3 agentFwd = actor.transform.forward;
            Vector3 playerFeet = player.Feet;
            bool inMorgueRefuge = activeFloorKey is ("CH04-B1" or "CH05-B1") &&
                (playerFeet.x < 4.45f || (activeFloorKey == "CH05-B1" && playerFeet.z > 44.75f));
            bool playerSafe = IsInSafeZone(playerFeet, scene) || inMorgueRefuge || Time.unscaledTime < graceUntil;

            // Safe-zone non-intrusion rule & Story Mode immediate breakoff (TASK011-E).
            if ((playerSafe || state.settings.storyMode) && currentState == OrderlyState.Chase)
            {
                sightTimer = 0f;
                loseSightTimer = 0f;
                returnCooldownUntil = Time.unscaledTime + ReturnCooldownSeconds;
                TransitionTo(OrderlyState.Return, 5.0f);
            }
            else if (playerSafe && currentState == OrderlyState.Suspicious &&
                (!suspiciousFromHearing || IsInSafeZone(stimulusPosition, scene)))
            {
                sightTimer = 0f;
                TransitionTo(OrderlyState.Return, 4.0f);
            }

            // 1. Vision perception (<= 12m, 100deg FOV, SolidMask occlusion, 0.70s accumulation)
            bool seesPlayer = !playerSafe && CheckLineOfSight(agentPos, agentFwd, playerFeet, SightRange);
            if (seesPlayer)
            {
                sightTimer += dt;
                stimulusPosition = playerFeet;
                lastSeenPosition = playerFeet;
                loseSightTimer = 0f;
                if (currentState is OrderlyState.Patrol or OrderlyState.Investigate or OrderlyState.Search or OrderlyState.Return)
                {
                    if (sightTimer >= SightAccumulationSeconds && !InReturnCooldown)
                    {
                        suspiciousFromHearing = false;
                        WarningCount++;
                        TransitionTo(OrderlyState.Suspicious, PreChaseWarningSeconds);
                        EmitHunterWarningCue(agentPos, hearingAlert: false, state, lineLookup);
                    }
                }
            }
            else
            {
                sightTimer = Mathf.Max(0f, sightTimer - dt * 1.2f);
                if (currentState == OrderlyState.Chase) loseSightTimer += dt;
            }

            // 2. Hearing perception (same floor only, occluded 0.5x, walk 3m / crouch 1.5m / run 9m)
            if (!playerSafe && playerMoving && currentState is OrderlyState.Patrol or OrderlyState.Return &&
                Mathf.Abs(agentPos.y - playerFeet.y) <= FloorIsolationMaxDeltaY)
            {
                float radius = playerRunning ? NoiseRunRadius : playerCrouching ? NoiseCrouchRadius : NoiseWalkRadius;
                bool occluded = Physics.Linecast(agentPos + Vector3.up * 1.4f, playerFeet + Vector3.up * 1.0f,
                    HospitalScene.SolidMask, QueryTriggerInteraction.Ignore);
                if (occluded) radius *= 0.5f;
                if (Vector3.Distance(agentPos, playerFeet) <= radius)
                {
                    stimulusPosition = playerFeet;
                    suspiciousFromHearing = true;
                    TransitionTo(OrderlyState.Suspicious, 0.65f);
                    EmitHunterWarningCue(agentPos, hearingAlert: true);
                }
            }

            // 3. 6-State FSM Execution
            stateTimer = Mathf.Max(0f, stateTimer - dt);
            switch (currentState)
            {
                case OrderlyState.Patrol:
                    Vector3 wp = GetPatrolPoint(patrolIndex);
                    if (MoveAlongNavMesh(actor, wp, PatrolSpeed, dt))
                        patrolIndex = (patrolIndex + 1) % GetPatrolPointCount();
                    break;

                case OrderlyState.Suspicious:
                    FaceTarget(actor, stimulusPosition, dt * 240f);
                    if (actor.ActiveActionName != "GK_InspectReach")
                        actor.PlayAction("GK_InspectReach", 0.20f);
                    if (stateTimer <= 0f)
                    {
                        if (!suspiciousFromHearing && seesPlayer && !state.settings.storyMode)
                        {
                            loseSightTimer = 0f;
                            TransitionTo(OrderlyState.Chase, 12.0f);
                        }
                        else
                        {
                            TransitionTo(OrderlyState.Investigate, 6.0f);
                        }
                    }
                    break;

                case OrderlyState.Investigate:
                    if (MoveAlongNavMesh(actor, stimulusPosition, InvestigateSpeed, dt) || stateTimer <= 0f)
                        EnterSearch(stimulusPosition);
                    break;

                case OrderlyState.Chase:
                    if (state.settings.storyMode)
                    {
                        TransitionTo(OrderlyState.Return, 5.0f);
                        break;
                    }
                    if (!seesPlayer && loseSightTimer >= LoseSightChaseSeconds)
                    {
                        EnterSearch(lastSeenPosition);
                        break;
                    }
                    MoveAlongNavMesh(actor, lastSeenPosition, ChaseSpeed, dt);
                    if (seesPlayer && Vector3.Distance(actor.transform.position, playerFeet) <= 1.35f)
                    {
                        scene.atmosphere?.From("keyJingle", actor.transform.position + Vector3.up);
                        ResetEncounter(state);
                        patrolIndex = PickFarthestPatrolIndex(player.Feet);
                        returnCooldownUntil = Time.unscaledTime + ReturnCooldownSeconds;
                        TransitionTo(OrderlyState.Return, 6.0f);
                    }
                    break;

                case OrderlyState.Search:
                    if (hasSearchSweepTarget)
                    {
                        if (MoveAlongNavMesh(actor, searchSweepTarget, InvestigateSpeed * 0.85f, dt))
                        {
                            hasSearchSweepTarget = false;
                            searchSweepSteps++;
                        }
                    }
                    else
                    {
                        if (actor.ActiveActionName != "GK_IdleStill")
                            actor.PlayAction("GK_IdleStill", 0.25f);
                        actor.transform.Rotate(0f, Mathf.Sin(Time.unscaledTime * 2.1f) * 42f * dt, 0f);
                        if (searchSweepSteps < 2 && stateTimer < SearchDurationSeconds - (searchSweepSteps + 1) * 3.5f)
                            PickNextSearchSweepPoint(lastSeenPosition);
                    }
                    if (stateTimer <= 0f)
                    {
                        returnCooldownUntil = Time.unscaledTime + ReturnCooldownSeconds;
                        TransitionTo(OrderlyState.Return, 7.0f);
                    }
                    break;

                case OrderlyState.Return:
                    Vector3 home = GetPatrolPoint(patrolIndex);
                    if (MoveAlongNavMesh(actor, home, InvestigateSpeed, dt) || stateTimer <= 0f)
                        TransitionTo(OrderlyState.Patrol, 0f);
                    break;
            }
        }

        private void EmitHunterWarningCue(Vector3 agentPos, bool hearingAlert, Investigation state = null,
            Func<string, int, string> lineLookup = null)
        {
            scene.atmosphere?.From("keyJingle", agentPos + Vector3.up * 1.0f);
            string dir = scene.atmosphere?.DirectionTo(agentPos) ?? "gần đây";
            if (hearingAlert)
            {
                ui?.Caption($"[Tiếng chùm chìa khóa · {dir} — Kẻ Giữ Cửa quay về phía tiếng động.]", 3.0f, false);
                return;
            }
            if (state != null && state.chapter == "CH03" && !spokeCh03Warning && lineLookup != null)
            {
                spokeCh03Warning = true;
                voice?.Speak("CH03-D05", "Kẻ Giữ Cửa", lineLookup("CH03-D05", 1),
                    scene.GatekeeperVoiceAnchor, activeActor,
                    directionalPrefix: $"[Tiếng chùm chìa khóa · {dir}]");
            }
            else
            {
                ui?.Caption($"[Tiếng chùm chìa khóa · {dir} — Kẻ Giữ Cửa đã phát hiện Minh (cảnh báo 2 giây)!]", 3.2f, true);
            }
        }

        private void EnterSearch(Vector3 anchor)
        {
            lastSeenPosition = anchor;
            hasSearchSweepTarget = false;
            searchSweepSteps = 0;
            TransitionTo(OrderlyState.Search, SearchDurationSeconds);
            PickNextSearchSweepPoint(anchor);
        }

        private void PickNextSearchSweepPoint(Vector3 anchor)
        {
            // Grounded local sweep around lastSeenPosition; strictly avoids safe pads and walls (no camp).
            Vector3[] offsets =
            {
                new(2.0f, 0f, 0f), new(-2.0f, 0f, 0f), new(0f, 0f, 2.0f), new(0f, 0f, -2.0f)
            };
            int startIdx = (searchSweepSteps * 2) % offsets.Length;
            for (int k = 0; k < offsets.Length; k++)
            {
                Vector3 candidate = anchor + offsets[(startIdx + k) % offsets.Length];
                if (NavMesh.SamplePosition(candidate, out var hit, 1.0f, NavMesh.AllAreas) &&
                    Mathf.Abs(hit.position.y - anchor.y) < 0.6f &&
                    !IsInSafeZone(hit.position, scene))
                {
                    searchSweepTarget = hit.position;
                    hasSearchSweepTarget = true;
                    return;
                }
            }
            hasSearchSweepTarget = false;
        }

        private string ResolveSafeRecoveryPad(Investigation state)
        {
            if (activeRole == AIRole.Orderly)
                return player != null && player.Feet.z >= 38.2f ? "AP07" : "AP06";
            if (activeFloorKey is ("CH04-B1" or "CH05-B1") || (player != null && player.Feet.y < -1.0f))
                return "AP08";
            if (state != null && state.chapter == "CH03" && player != null && player.Feet.y < 6.0f)
                return "AP06";
            return "AN03";
        }

        private void ResetEncounter(Investigation state)
        {
            EncounterResetCount++;
            string padId = ResolveSafeRecoveryPad(state);
            if (scene.TryPad(padId, out var safePad) && safePad != null && !HospitalScene.Occupied(safePad.position))
                player.Teleport(safePad);
            graceUntil = Time.unscaledTime + (activeRole == AIRole.Orderly ? 12.0f : PostRecoveryGraceSeconds);
            ui?.Caption(activeRole == AIRole.Orderly
                ? "[Người điều dưỡng đẩy xe qua hành lang. Minh lùi về trạm an toàn — hồ sơ được giữ nguyên.]"
                : "[Lối đi bị chặn. Minh lùi về điểm an toàn — không mất vật chứng hay sổ trực.]", 4.4f, true);
        }

        private int PickFarthestPatrolIndex(Vector3 fromPos)
        {
            int count = GetPatrolPointCount();
            int best = 0;
            float maxDist = -1f;
            for (int i = 0; i < count; i++)
            {
                float d = Vector3.Distance(fromPos, GetPatrolPoint(i));
                if (d > maxDist) { maxDist = d; best = i; }
            }
            return best;
        }

        public void TransitionTo(OrderlyState next, float duration)
        {
            // Hard guard: Orderly NEVER enters Investigate, Chase, or Search.
            if (activeRole == AIRole.Orderly && next is (OrderlyState.Investigate or OrderlyState.Chase or OrderlyState.Search))
                next = OrderlyState.Patrol;
            currentState = next;
            stateTimer = duration;
            waitingAtStop = false;
            InvalidatePath();
            var actor = activeActor;
            if (actor == null || !actor.gameObject.activeInHierarchy) return;
            if (next is OrderlyState.Patrol or OrderlyState.Investigate or OrderlyState.Chase or OrderlyState.Return)
                actor.PlayAction("GK_StalkWalk", 0.20f);
            else if (next == OrderlyState.Suspicious)
                actor.PlayAction("GK_InspectReach", 0.20f, restart: true);
            else
                actor.PlayAction("GK_IdleStill", 0.20f);
        }

        private bool MoveAlongNavMesh(CharacterActor actor, Vector3 destination, float speed, float dt)
        {
            Vector3 pos = actor.transform.position;
            if ((destination - pos).sqrMagnitude <= 0.14f * 0.14f)
                return true;

            if (Time.unscaledTime >= repathAt &&
                (cornerIndex >= corners.Length || (destination - pathDestination).sqrMagnitude >= 0.55f * 0.55f))
            {
                repathAt = Time.unscaledTime + 0.30f;
                pathDestination = destination;
                PathQueries++;
                // Reject both invalid and partial paths; NEVER fall back to raw teleport/wall-clipping!
                if (!NavMesh.SamplePosition(pos, out var start, 0.45f, NavMesh.AllAreas) ||
                    !NavMesh.SamplePosition(destination, out var end, 1.15f, NavMesh.AllAreas) ||
                    !NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, navPath) ||
                    navPath.status != NavMeshPathStatus.PathComplete)
                {
                    InvalidPaths++;
                    InvalidatePath();
                    return StandStill(actor);
                }
                corners = navPath.corners;
                cornerIndex = 1;
            }
            while (cornerIndex < corners.Length && (corners[cornerIndex] - pos).sqrMagnitude <= 0.12f * 0.12f)
                cornerIndex++;
            if (cornerIndex >= corners.Length) return StandStill(actor);

            Vector3 target = corners[cornerIndex];
            Vector3 step = Vector3.MoveTowards(pos, target, speed * dt);
            Vector3 delta = step - pos;
            const float radius = 0.30f;
            bool blockedBySafeZone = IsInSafeZone(step, scene);
            bool blockedByMorgueBoundary = activeRole == AIRole.Hunter &&
                activeFloorKey is ("CH04-B1" or "CH05-B1") &&
                (step.x < 4.55f || (activeFloorKey == "CH05-B1" && step.z > 44.65f));
            if (blockedBySafeZone || blockedByMorgueBoundary ||
                NavMesh.Raycast(pos, step, out _, NavMesh.AllAreas) ||
                (delta.sqrMagnitude > 0.00001f && Physics.CapsuleCast(pos + Vector3.up * 0.48f, pos + Vector3.up * 1.39f,
                    radius, delta.normalized, delta.magnitude, HospitalScene.SolidMask, QueryTriggerInteraction.Ignore)))
            {
                BlockedSteps++;
                movementBlockedFor += dt;
                InvalidatePath();
                if (movementBlockedFor >= 0.75f && currentState == OrderlyState.Patrol)
                {
                    patrolIndex = (patrolIndex + 1) % GetPatrolPointCount();
                    movementBlockedFor = 0f;
                }
                return StandStill(actor);
            }
            movementBlockedFor = 0f;
            if (actor.ActiveActionName != "GK_StalkWalk")
                actor.PlayAction("GK_StalkWalk", 0.18f);
            FaceTarget(actor, target, dt * 260f);
            actor.transform.position = step;

            keyJingleTimer += dt * (speed / PatrolSpeed);
            if (keyJingleTimer >= 1.45f)
            {
                keyJingleTimer = 0f;
                scene?.atmosphere?.From(activeRole == AIRole.Orderly ? "trolleyRoll" : "keyJingle",
                    actor.transform.position + Vector3.up * 0.95f, 0.55f);
            }
            return (destination - step).sqrMagnitude <= 0.14f * 0.14f;
        }

        private void InvalidatePath()
        {
            corners = Array.Empty<Vector3>();
            cornerIndex = 0;
        }

        private static bool StandStill(CharacterActor actor)
        {
            if (actor.ActiveActionName != "GK_IdleStill")
                actor.PlayAction("GK_IdleStill", 0.2f);
            return false;
        }

        private static void FaceTarget(CharacterActor actor, Vector3 target, float maxDegDelta)
        {
            Vector3 dir = target - actor.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                actor.transform.rotation = Quaternion.RotateTowards(actor.transform.rotation, Quaternion.LookRotation(dir), maxDegDelta);
        }

        private int GetPatrolPointCount() => GetPatrolPointCount(activeRole, activeFloorKey);

        private int GetPatrolPointCount(AIRole role, string floorKey)
        {
            if (role == AIRole.Orderly) return ActiveOrderlyRoute.Length;
            if (floorKey == "CH04-B1") return ServiceRound.Length;
            if (floorKey == "CH05-B1") return Ch05ServiceRound.Length;
            if (scene?.Ch03PatrolWaypoints != null && scene.Ch03PatrolWaypoints.Length > 0)
                return scene.Ch03PatrolWaypoints.Length;
            return Ch03FallbackRound.Length;
        }

        private Vector3 GetPatrolPoint(int idx) => GetPatrolPoint(activeRole, activeFloorKey, idx);

        private Vector3 GetPatrolPoint(AIRole role, string floorKey, int idx)
        {
            int safeIdx = Mathf.Abs(idx);
            if (role == AIRole.Orderly)
            {
                var route = ActiveOrderlyRoute;
                return route[safeIdx % route.Length].Position;
            }
            if (floorKey == "CH04-B1")
                return ServiceRound[safeIdx % ServiceRound.Length];
            if (floorKey == "CH05-B1")
                return Ch05ServiceRound[safeIdx % Ch05ServiceRound.Length];
            if (scene?.Ch03PatrolWaypoints != null && scene.Ch03PatrolWaypoints.Length > 0)
            {
                var t = scene.Ch03PatrolWaypoints[safeIdx % scene.Ch03PatrolWaypoints.Length];
                if (t != null) return t.position;
            }
            return Ch03FallbackRound[safeIdx % Ch03FallbackRound.Length];
        }
    }
}
