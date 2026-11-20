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

    /// <summary>
    /// Generalized M3 Regional Hunter / Orderly AI Controller (TASK011-A..E, Core 06 §02.04, CH03-Q04).
    /// Implements the 6-state patrol/perception/distraction/chase/search/return state machine with:
    /// - Vision: <= 12.0 m range, 100 deg FOV, SolidMask raycast occlusion, 0.70 s sight accumulation
    /// - Pre-chase warning: 2.0 s warning window with SFX-KEY + CH03-D05 before entering Chase
    /// - Hearing: Walk 3.0 m, Crouch 1.5 m, Run 9.0 m, Distraction (LOC07 Radio) 14.0 m (0.5x when wall-occluded)
    /// - Safe-Zone & Reading Non-Intrusion: Never enters AP06/AN03 safe pads, pauses perception during modal reading,
    ///   physical inspection, R/B/M transitions, and Memory Vignettes (Reality.M).
    /// </summary>
    public sealed class HunterOrderlyAI : MonoBehaviour
    {
        public const float SightRange = 12.0f;
        public const float SightFovDegrees = 100.0f;
        public const float SightAccumulationSeconds = 0.70f;
        public const float PreChaseWarningSeconds = 2.00f;

        public const float NoiseWalkRadius = 3.0f;
        public const float NoiseCrouchRadius = 1.5f;
        public const float NoiseRunRadius = 9.0f;
        public const float NoiseDistractionRadius = 14.0f;

        public const float PatrolSpeed = 1.65f;
        public const float InvestigateSpeed = 1.78f;
        public const float ChaseSpeed = 3.15f;

        private HospitalScene scene;
        private VoiceDirector voice;
        private FirstPerson player;
        private HospitalUI ui;
        private PhysicalInspector inspector;

        private OrderlyState currentState = OrderlyState.Patrol;
        private int patrolIndex;
        private float sightTimer;
        private float stateTimer;
        private float keyJingleTimer;
        private Vector3 stimulusPosition;
        private bool activeInChapter;
        private bool spokeCh03Warning;
        private readonly NavMeshPath navPath = new();
        private Vector3[] corners = Array.Empty<Vector3>();
        private Vector3 pathDestination;
        private float repathAt;
        private float movementBlockedFor;
        private int cornerIndex;
        private enum Role { None, Hunter, Orderly, Unrecorded }
        private Role role;
        private float graceUntil;
        private bool waitingAtStop;
        private static readonly Vector3[] WardRound = { new(0, 4, 36.9f), new(2.3f, 4, 34.8f), new(4f, 4, 41.3f) };
        private static readonly Vector3[] ServiceRound = { new(7.3f, -3.4f, 37f), new(8.9f, -3.4f, 41.7f), new(6.6f, -3.4f, 43.8f) };
        private static readonly Vector3[] MorgueTrace = { new(2.7f, -3.4f, 38f), new(2.7f, -3.4f, 40.6f) };
        private float Pace => role == Role.Orderly ? 1.21f : role == Role.Unrecorded ? .78f : PatrolSpeed;
        private bool CanChase => role == Role.Hunter;
        private string SafeRecovery => role == Role.Orderly ? "AP07" : player.Feet.y < 0 ? "AP08" : "AN03";
        public int PathQueries { get; private set; }
        public int InvalidPaths { get; private set; }
        public int BlockedSteps { get; private set; }

        private static readonly string[] SafePads = { "AP02", "AP04", "AP05", "AP06", "AN03", "AP07", "AP08", "AP09" };

        public OrderlyState CurrentState => currentState;
        public bool IsActive => activeInChapter;
        public Vector3 StimulusPosition => stimulusPosition;
        public float SightAccumulated => sightTimer;

        public void Initialize(HospitalScene world, VoiceDirector voiceDirector, FirstPerson fps,
            HospitalUI hospitalUi, PhysicalInspector physicalInspector)
        {
            scene = world;
            voice = voiceDirector;
            player = fps;
            ui = hospitalUi;
            inspector = physicalInspector;
        }

        public void SyncState(Investigation state)
        {
            if (scene == null) return;
            var actor = scene.GatekeeperActor;
            if (actor == null)
            {
                activeInChapter = false;
                InvalidatePath();
                return;
            }

            Role next = Role.None;
            if (state.layer == Reality.R && state.chapter == "CH03" && state.Has(EvidenceRegistry.PatientRegister) &&
                !state.Happened("CH03-T07") && scene.HasZone("LOC05"))
                next = Role.Hunter;
            else if (state.layer == Reality.R && state.chapter == "CH04" && !GateService.H05(state) && player != null)
            {
                if (player.Feet.y > 3.4f && scene.HasZone("LOC05")) next = Role.Orderly;
                else if (player.Feet.y < -2.5f && scene.HasZone("LOC09"))
                    next = player.Feet.x < 4.45f ? Role.Unrecorded : Role.Hunter;
            }
            if (next == Role.None)
            {
                if (activeInChapter)
                {
                    activeInChapter = false;
                    role = Role.None;
                    currentState = OrderlyState.Patrol;
                    sightTimer = 0f;
                    actor.gameObject.SetActive(false);
                }
                return;
            }

            if (!activeInChapter || role != next || !actor.gameObject.activeSelf)
            {
                role = next;
                activeInChapter = true;
                currentState = OrderlyState.Patrol;
                patrolIndex = 0;
                sightTimer = 0f;
                stateTimer = 0f;
                InvalidatePath();
                actor.gameObject.SetActive(true);
                actor.ReducedMotion = state.settings.reduceMotion;
                actor.LookTarget = player != null && player.Eye != null ? player.Eye.transform : null;
                for (int index = 0; index < GetPatrolPointCount(); index++)
                {
                    if (Vector3.Distance(player.Feet, GetPatrolPoint(index)) < 3.0f) continue;
                    patrolIndex = index;
                    break;
                }
                Vector3 startPt = GetPatrolPoint(patrolIndex);
                if (Vector3.Distance(startPt, player.Feet) < 3.0f)
                {
                    activeInChapter = false;
                    actor.gameObject.SetActive(false);
                    return;
                }
                actor.transform.SetPositionAndRotation(startPt, Quaternion.Euler(0f, 180f, 0f));
                actor.PlayAction("GK_StalkWalk", 0.15f, restart: true);
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
            if (!activeInChapter || scene?.GatekeeperActor == null || role == Role.Unrecorded) return;
            Vector3 agentPos = scene.GatekeeperActor.transform.position;
            float dist = Vector3.Distance(agentPos, worldPos);
            bool occluded = Physics.Linecast(agentPos + Vector3.up * 1.5f, worldPos + Vector3.up * 0.8f, HospitalScene.SolidMask);
            float effectiveRadius = occluded ? radius * 0.65f : radius;
            if (dist <= effectiveRadius && currentState != OrderlyState.Chase)
            {
                stimulusPosition = worldPos;
                TransitionTo(OrderlyState.Investigate, 6.5f);
            }
        }

        public bool CheckLineOfSight(Vector3 agentPos, Vector3 agentForward, Vector3 targetPos)
        {
            Vector3 toTarget = targetPos - agentPos;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;
            if (Vector3.Distance(agentPos, targetPos) > SightRange || dist < 0.05f) return false;
            float angle = Vector3.Angle(agentForward, toTarget / dist);
            if (angle > SightFovDegrees * 0.5f) return false;
            Vector3 eyeA = agentPos + Vector3.up * 1.62f;
            Vector3 eyeB = targetPos + Vector3.up * 1.45f;
            return !Physics.Linecast(eyeA, eyeB, HospitalScene.SolidMask, QueryTriggerInteraction.Ignore);
        }

        public void Tick(Investigation state, float dt, bool playerRunning, bool playerCrouching, bool playerMoving,
            Func<string, int, string> lineLookup)
        {
            if (!activeInChapter || scene?.GatekeeperActor == null || player == null) return;
            var actor = scene.GatekeeperActor;
            if (!actor.gameObject.activeInHierarchy) return;

            // Pause perception & movement while player is reading documents, solving modals, or in Reality.M vignette!
            if ((ui != null && ui.IsModal) || (inspector != null && inspector.IsInspecting) || state.layer == Reality.M)
            {
                if (actor.ActiveActionName != "GK_IdleStill")
                    actor.PlayAction("GK_IdleStill", 0.20f);
                return;
            }

            Vector3 agentPos = actor.transform.position;
            Vector3 agentFwd = actor.transform.forward;
            Vector3 playerFeet = player.Feet;
            bool playerSafe = IsInSafeZone(playerFeet, scene) || Time.unscaledTime < graceUntil;

            // Safe-zone non-intrusion rule (TASK011-D / CH03-Q04): break off immediately if player reaches safe pad!
            if (playerSafe && (currentState == OrderlyState.Suspicious || currentState == OrderlyState.Chase))
            {
                sightTimer = 0f;
                TransitionTo(OrderlyState.Return, 4.0f);
            }

            // 1. Vision perception
            bool seesPlayer = role != Role.Unrecorded && !playerSafe && CheckLineOfSight(agentPos, agentFwd, playerFeet);
            if (seesPlayer)
            {
                sightTimer += dt;
                stimulusPosition = playerFeet;
                if (currentState == OrderlyState.Patrol || currentState == OrderlyState.Investigate ||
                    currentState == OrderlyState.Search || currentState == OrderlyState.Return)
                {
                    if (sightTimer >= SightAccumulationSeconds)
                    {
                        TransitionTo(OrderlyState.Suspicious, PreChaseWarningSeconds);
                        scene.atmosphere?.From(role == Role.Orderly ? "trolleyRoll" : "keyJingle", agentPos + Vector3.up * 1.0f);
                        if (role == Role.Orderly) ui?.Caption("[Xe thuốc dừng lại. Lùi về phía phòng trực.]", 3f, false);
                        if (state.chapter == "CH03" && !spokeCh03Warning && lineLookup != null)
                        {
                            spokeCh03Warning = true;
                            voice?.Speak("CH03-D05", "Kẻ Giữ Cửa", lineLookup("CH03-D05", 1),
                                scene.GatekeeperVoiceAnchor, actor,
                                directionalPrefix: "[Tiếng chùm chìa khóa · Hành lang ca trực]");
                        }
                    }
                }
            }
            else
            {
                sightTimer = Mathf.Max(0f, sightTimer - dt * 1.2f);
            }

            // 2. Hearing perception (when not already chasing or suspicious)
            if (CanChase && !playerSafe && playerMoving && currentState != OrderlyState.Chase && currentState != OrderlyState.Suspicious)
            {
                float radius = playerRunning ? NoiseRunRadius : playerCrouching ? NoiseCrouchRadius : NoiseWalkRadius;
                bool occluded = Physics.Linecast(agentPos + Vector3.up * 1.4f, playerFeet + Vector3.up * 1.0f, HospitalScene.SolidMask);
                if (occluded) radius *= 0.5f;
                if (Vector3.Distance(agentPos, playerFeet) <= radius)
                {
                    stimulusPosition = playerFeet;
                    TransitionTo(OrderlyState.Investigate, 5.0f);
                }
            }

            // 3. State machine execution
            stateTimer = Mathf.Max(0f, stateTimer - dt);
            switch (currentState)
            {
                case OrderlyState.Patrol:
                    Vector3 wp = GetPatrolPoint(patrolIndex);
                    if (MoveAlongNavMesh(actor, wp, Pace, dt))
                    {
                        if (role == Role.Orderly && !waitingAtStop)
                        {
                            waitingAtStop = true;
                            stateTimer = 4f + patrolIndex;
                        }
                        else if (role != Role.Orderly || stateTimer <= 0f)
                        {
                            waitingAtStop = false;
                            patrolIndex = (patrolIndex + 1) % GetPatrolPointCount();
                        }
                        else StandStill(actor);
                    }
                    break;

                case OrderlyState.Suspicious:
                    // 2.0s pre-chase warning window (Core 06 §02.04)
                    FaceTarget(actor, stimulusPosition, dt * 240f);
                    if (actor.ActiveActionName != "GK_InspectReach")
                        actor.PlayAction("GK_InspectReach", 0.20f);
                    if (stateTimer <= 0f)
                    {
                        if (role == Role.Orderly && seesPlayer && Vector3.Distance(agentPos, playerFeet) < 3.4f && !state.settings.storyMode)
                        {
                            ResetEncounter();
                            TransitionTo(OrderlyState.Return, 5f);
                        }
                        else if (CanChase && seesPlayer && !state.settings.storyMode)
                            TransitionTo(OrderlyState.Chase, 7.0f);
                        else
                            TransitionTo(OrderlyState.Investigate, 4.5f);
                    }
                    break;

                case OrderlyState.Investigate:
                    if (MoveAlongNavMesh(actor, stimulusPosition, InvestigateSpeed, dt) || stateTimer <= 0f)
                        TransitionTo(OrderlyState.Search, 3.2f);
                    break;

                case OrderlyState.Chase:
                    if (!seesPlayer && stateTimer <= 0f)
                    {
                        TransitionTo(OrderlyState.Search, 3.5f);
                        break;
                    }
                    if (seesPlayer) stateTimer = 3.0f;
                    MoveAlongNavMesh(actor, stimulusPosition, state.settings.storyMode ? InvestigateSpeed : ChaseSpeed, dt);
                    if (seesPlayer && Vector3.Distance(actor.transform.position, playerFeet) <= 1.35f)
                    {
                        // Non-lethal interception: reset player cleanly to CH03 Nurse Station safe pad (AP06) without data loss!
                        scene.atmosphere?.From("keyJingle", actor.transform.position + Vector3.up);
                        ResetEncounter();
                        TransitionTo(OrderlyState.Return, 4.0f);
                    }
                    break;

                case OrderlyState.Search:
                    if (actor.ActiveActionName != "GK_IdleStill")
                        actor.PlayAction("GK_IdleStill", 0.25f);
                    actor.transform.Rotate(0f, Mathf.Sin(Time.unscaledTime * 2.2f) * 35f * dt, 0f);
                    if (stateTimer <= 0f)
                        TransitionTo(OrderlyState.Return, 6.0f);
                    break;

                case OrderlyState.Return:
                    Vector3 home = GetPatrolPoint(patrolIndex);
                    if (MoveAlongNavMesh(actor, home, InvestigateSpeed, dt) || stateTimer <= 0f)
                        TransitionTo(OrderlyState.Patrol, 0f);
                    break;
            }
        }

        private void ResetEncounter()
        {
            if (scene.TryPad(SafeRecovery, out var safePad) && safePad != null)
                player.Teleport(safePad);
            graceUntil = Time.unscaledTime + 60f;
            ui?.Caption("[Lối đi bị chặn. Minh lùi về điểm an toàn.]", 4.2f, true);
        }

        public void TransitionTo(OrderlyState next, float duration)
        {
            currentState = next;
            stateTimer = duration;
            waitingAtStop = false;
            InvalidatePath();
            var actor = scene?.GatekeeperActor;
            if (actor == null || !actor.gameObject.activeInHierarchy) return;
            if (next == OrderlyState.Patrol || next == OrderlyState.Investigate ||
                next == OrderlyState.Chase || next == OrderlyState.Return)
                actor.PlayAction("GK_StalkWalk", 0.20f);
            else if (next == OrderlyState.Suspicious)
                actor.PlayAction("GK_InspectReach", 0.20f, restart: true);
            else
                actor.PlayAction("GK_IdleStill", 0.20f);
        }

        private bool MoveAlongNavMesh(CharacterActor actor, Vector3 destination, float speed, float dt)
        {
            Vector3 pos = actor.transform.position;
            if ((destination - pos).sqrMagnitude <= 0.13f * 0.13f)
                return true;

            if (Time.unscaledTime >= repathAt &&
                (cornerIndex >= corners.Length || (destination - pathDestination).sqrMagnitude >= 0.6f * 0.6f))
            {
                repathAt = Time.unscaledTime + 0.35f;
                pathDestination = destination;
                PathQueries++;
                if (!NavMesh.SamplePosition(pos, out var start, 0.45f, NavMesh.AllAreas) ||
                    !NavMesh.SamplePosition(destination, out var end, 1.20f, NavMesh.AllAreas) ||
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
            // Respect moved doors and safe volumes even while the baked path is cached.
            Vector3 delta = step - pos;
            const float radius = 0.30f;
            if (IsInSafeZone(step, scene) ||
                NavMesh.Raycast(pos, step, out _, NavMesh.AllAreas) ||
                Physics.CapsuleCast(pos + Vector3.up * 0.48f, pos + Vector3.up * 1.39f,
                    radius, delta.normalized, delta.magnitude, HospitalScene.SolidMask, QueryTriggerInteraction.Ignore))
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
                if (role != Role.Unrecorded)
                    scene?.atmosphere?.From(role == Role.Orderly ? "trolleyRoll" : "keyJingle",
                        actor.transform.position + Vector3.up * .95f, .55f);
            }
            return (destination - step).sqrMagnitude <= 0.13f * 0.13f;
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

        private int GetPatrolPointCount() =>
            CustomRound?.Length ?? Mathf.Max(1, scene.Ch03PatrolWaypoints.Length);

        private Vector3[] CustomRound => role == Role.Orderly ? WardRound :
            role == Role.Unrecorded ? MorgueTrace : player != null && player.Feet.y < -2.5f ? ServiceRound : null;

        private Vector3 GetPatrolPoint(int idx)
        {
            var round = CustomRound;
            if (round != null) return round[idx % round.Length];
            if (scene?.Ch03PatrolWaypoints != null && scene.Ch03PatrolWaypoints.Length > 0)
            {
                var t = scene.Ch03PatrolWaypoints[Mathf.Abs(idx) % scene.Ch03PatrolWaypoints.Length];
                if (t != null) return t.position;
            }
            // Default LOC06 Duty Corridor patrol route between Archive Door (z=22.4) and Duty Exit (z=29.2)
            return (idx % 2 == 0) ? new Vector3(-3.2f, 0f, 22.4f) : new Vector3(-3.2f, 0f, 29.2f);
        }
    }
}
