using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CTCC
{
    /// <summary>
    /// Stages Lan's CH01 arc across B02 (reception glass parallax silhouette + voluntary study reaction)
    /// and B03 (Pha 1 desk pen writing -> Pha 2 free-FPS scripted recognition -> Pha 3 player-initiated
    /// 4-shot active cinematic with safe skip & reduced-motion cuts -> Pha 4 post-talk reflections & drawer look).
    /// </summary>
    public sealed class LanDirector : MonoBehaviour
    {
        private enum ReceptionPose { Absent, BehindGlass, AtCounter }

        private static readonly (string id, float pause, float warm, float concern, float sorrow,
            float yawBias, float pitchBias, bool lookAtBoard, bool microBlink, int shotIndex)[] EncounterBeats =
        {
            ("CH01-D03",  0.48f, 0.46f, 0.12f, 0.00f,  7.5f, 2.0f, false, true,  0),
            ("CH01-D03b", 0.50f, 0.36f, 0.16f, 0.02f, 11.0f, 4.0f, false, false, 0),
            ("CH01-D03c", 0.44f, 0.38f, 0.20f, 0.02f, 14.0f, 5.0f, false, true,  0),
            ("CH01-D04",  0.65f, 0.24f, 0.28f, 0.06f, 16.5f, 6.5f, false, true,  1),
            ("CH01-D04c", 0.72f, 0.11f, 0.30f, 0.08f, 13.0f, 4.2f, false, true,  2),
            ("CH01-D05",  0.76f, 0.26f, 0.20f, 0.06f,  5.0f, 2.0f, false, false, 2),
            ("CH01-D05b", 0.50f, 0.28f, 0.24f, 0.08f, 10.5f, 5.5f, true,  false, 3),
            ("CH01-D05c", 0.44f, 0.35f, 0.20f, 0.05f,  4.0f, 6.0f, true,  true,  3),
            ("CH01-D07",  0.34f, 0.38f, 0.19f, 0.04f,  8.5f, 2.5f, false, true,  3),
            ("CH01-D07b", 0.40f, 0.25f, 0.18f, 0.05f, 12.0f, 4.5f, false, false, 3),
        };

        private static readonly (Vector3 pos, Vector3 lookAt, float fov)[] CinematicShots =
        {
            // Shot 0: Over-the-desk medium two-shot
            (new Vector3(3.92f, 1.56f, -2.28f), new Vector3(2.48f, 1.44f, -3.55f), 54f),
            // Shot 1: Closer semi-profile reaction
            (new Vector3(3.52f, 1.54f, -2.66f), new Vector3(2.54f, 1.48f, -3.66f), 47f),
            // Shot 2: Wider counter angle keeping the stopped 03:17 wall clock in background (no auto-observation!)
            (new Vector3(4.24f, 1.60f, -3.32f), new Vector3(1.52f, 1.66f, -3.45f), 60f),
            // Shot 3: Desk-and-clipboard framing as Lan points toward Room 101 assignment (no auto-pickup!)
            (new Vector3(3.46f, 1.50f, -2.00f), new Vector3(2.34f, 1.29f, -3.22f), 51f),
        };

        private HospitalScene scene;
        private VoiceDirector voice;
        private FirstPerson player;
        private HospitalUI ui;
        private Func<Investigation> stateGetter;
        private Func<bool> suppressionCheck;
        private Action onGlassStudied;
        private Action onRecognizedMinh;
        private Func<string, (string speaker, string text)> defaultLineLookup;

        private CharacterActor stagedActor;
        private ReceptionPose pose;
        private Coroutine encounterRoutine;
        private Coroutine glassPass;
        private Coroutine recognitionRoutine;
        private Coroutine drawerLookRoutine;

        private bool glassPassPlayed;
        private bool glassStudiedFired;
        private bool lookedAtGlassOnce;
        private bool glancedAwayFromGlass;
        private float glassStudyTimer;
        private Vector3 silhouetteBasePos;
        private Light glassSilhouetteLight;
        private float glassSilhouetteBaseIntensity;
        private bool cinematicActive;
        private bool skipRequested;
        private float cinematicStartedAt;
        private FirstPerson.CameraPoseSnapshot savedCameraPose;
        private bool hasSavedCameraPose;

        internal static IEnumerable<string> EncounterLineIds()
        {
            foreach (var beat in EncounterBeats) yield return beat.id;
        }

        public bool IsEncounterRunning => encounterRoutine != null || recognitionRoutine != null;
        public bool IsCinematicActive => cinematicActive;

        private Transform MinhEye => player?.Eye != null ? player.Eye.transform : null;

        public void Initialize(
            HospitalScene world,
            VoiceDirector voiceDirector,
            FirstPerson fps,
            HospitalUI interfaceUi = null,
            Func<Investigation> getState = null,
            Func<bool> isSuppressed = null,
            Action glassStudiedCallback = null,
            Action recognizedCallback = null,
            Func<string, (string speaker, string text)> lineLookup = null)
        {
            scene = world;
            voice = voiceDirector;
            player = fps;
            ui = interfaceUi;
            stateGetter = getState;
            suppressionCheck = isSuppressed;
            onGlassStudied = glassStudiedCallback;
            onRecognizedMinh = recognizedCallback;
            defaultLineLookup = lineLookup;
        }

        public void SyncState(Investigation state)
        {
            if (scene == null || state == null) return;
            var lan = scene.LanActor;
            var glassLight = scene.ReceptionGlassSilhouette;

            var next = ReceptionPose.Absent;
            if (state.chapter == "CH01")
            {
                if (state.layer == Reality.R && state.Happened("CH01-T02") && !state.Happened("CH01-T03"))
                    next = ReceptionPose.BehindGlass;
                // F101 is processed in Room 101. Lan is already gone when Minh sees the desk again in B06.
                if (state.layer == Reality.B && state.Happened("CH01-T03") && !state.Happened("CH01-T08") &&
                    !GateService.G01(state))
                    next = ReceptionPose.AtCounter;
            }

            if (glassLight != null)
            {
                glassLight.SetActive(next == ReceptionPose.BehindGlass);
                if (glassSilhouetteLight == null)
                {
                    glassSilhouetteLight = glassLight.GetComponent<Light>();
                    if (glassSilhouetteLight != null) glassSilhouetteBaseIntensity = glassSilhouetteLight.intensity;
                }
            }
            if (lan == null)
            {
                scene.atmosphere?.SetPenWriting(false, Vector3.zero);
                return;
            }
            lan.ReducedMotion = state.settings.reduceMotion;

            // Update pen-writing ambient loop cleanly whenever state changes
            UpdatePenWritingLoop(next == ReceptionPose.AtCounter && !IsEncounterRunning && !cinematicActive &&
                                 drawerLookRoutine == null && (voice == null || !voice.IsSpeaking));

            // Saving dialogue or collecting evidence can reapply the same state during a performance.
            if (lan == stagedActor && next == pose) return;

            StopEncounter(false);
            StopSecondaryRoutines();
            glassPassPlayed = false;
            glassStudiedFired = state.HasDreadEvent(DreadCatalog.B02GlassStudied);
            lookedAtGlassOnce = false;
            glancedAwayFromGlass = false;
            glassStudyTimer = 0f;
            stagedActor = lan;
            pose = next;
            lan.gameObject.SetActive(pose != ReceptionPose.Absent);
            lan.SetHeldClipboardVisible(false);
            if (pose == ReceptionPose.Absent)
            {
                UpdatePenWritingLoop(false);
                return;
            }

            var mark = pose == ReceptionPose.BehindGlass ? scene.LanSilhouettePoint : scene.LanStepOutPoint;
            if (mark == null) mark = scene.LanDeskPoint;
            if (mark != null)
            {
                lan.transform.SetPositionAndRotation(mark.position, mark.rotation);
                silhouetteBasePos = mark.position;
            }
            lan.LookTarget = pose == ReceptionPose.AtCounter ? MinhEye : null;
            lan.EnableCloseUpAversion = pose == ReceptionPose.AtCounter;
            lan.GazeYawBias = pose == ReceptionPose.AtCounter ? 9.5f : 0f;
            lan.GazePitchBias = pose == ReceptionPose.AtCounter ? 3.5f : 0f;
            lan.SetExpression(0.24f, 0.12f, 0f);
            lan.PlayAction("Lan_IdleDesk", 0.2f);
            UpdatePenWritingLoop(pose == ReceptionPose.AtCounter);
        }

        private void UpdatePenWritingLoop(bool writing)
        {
            if (scene?.atmosphere == null) return;
            Vector3 deskPos = scene.LanStepOutPoint != null ? scene.LanStepOutPoint.position + Vector3.up * 1.1f
                : new Vector3(2.35f, 1.15f, -3.50f);
            scene.atmosphere.SetPenWriting(writing && pose == ReceptionPose.AtCounter, deskPos);
        }

        private void StopSecondaryRoutines()
        {
            if (glassPass != null) { StopCoroutine(glassPass); glassPass = null; }
            if (recognitionRoutine != null) { StopCoroutine(recognitionRoutine); recognitionRoutine = null; }
            if (drawerLookRoutine != null) { StopCoroutine(drawerLookRoutine); drawerLookRoutine = null; }
        }

        private void Update()
        {
            if (cinematicActive)
            {
                var kb = Keyboard.current;
                if (kb != null && Time.unscaledTime - cinematicStartedAt > 0.28f &&
                    (kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame))
                {
                    RequestSkipCinematic();
                }
                return;
            }

            if (stagedActor == null || player == null) return;

            if (pose == ReceptionPose.BehindGlass)
            {
                TickBehindGlassAnomaly();
                return;
            }

            if (pose == ReceptionPose.AtCounter)
            {
                TickAtCounterRecognition();
            }
        }

        private void TickBehindGlassAnomaly()
        {
            if (!player.CanPlay || player.Eye == null) return;
            Vector3 feet = player.Feet;
            Vector3 glassFocus = new Vector3(0.55f, 1.50f, -2.75f);
            Vector3 toGlass = glassFocus - player.Eye.transform.position;
            float dist = toGlass.magnitude;
            float align = dist > 0.05f ? Vector3.Dot(player.Eye.transform.forward, toGlass / dist) : 0f;

            // Subtle flashlight glare / wash response on reception window (never monster face)
            if (glassSilhouetteLight != null)
            {
                bool torchAimed = player.IsTorchOn && align > 0.78f && dist < 12.5f;
                float targetIntensity = torchAimed ? glassSilhouetteBaseIntensity * 0.62f : glassSilhouetteBaseIntensity;
                glassSilhouetteLight.intensity = Mathf.Lerp(glassSilhouetteLight.intensity, targetIntensity, Time.deltaTime * 4.5f);
            }

            // Subtle lateral parallax anomaly while Minh moves sideways in courtyard (off in reduced motion)
            if (!glassPassPlayed && !stagedActor.ReducedMotion && feet.z > -15.5f && feet.z < -4.2f)
            {
                float lagZ = Mathf.Clamp((feet.x - 4.0f) * -0.028f, -0.14f, 0.14f);
                Vector3 desired = silhouetteBasePos + new Vector3(0f, 0f, lagZ);
                stagedActor.transform.position = Vector3.Lerp(stagedActor.transform.position, desired, Time.deltaTime * 1.8f);
            }

            // Voluntary close observation tracking -> DREAD_B02_GLASS_STUDIED
            if (feet.z > -12.2f && feet.z < -4.6f && align > 0.70f)
            {
                lookedAtGlassOnce = true;
                glassStudyTimer += Time.deltaTime;
                if (!glassStudiedFired && glassStudyTimer >= 0.95f)
                {
                    glassStudiedFired = true;
                    onGlassStudied?.Invoke();
                }
            }
            else if (lookedAtGlassOnce && align < 0.44f)
            {
                glancedAwayFromGlass = true;
            }

            // Trigger quiet silhouette shift when glancing back OR entering lobby approach sightline
            if (glassPassPlayed) return;
            bool inApproach = feet.z >= -8.1f && feet.z <= -4.0f && feet.x >= 2.4f && feet.x <= 6.0f;
            bool glancedBack = glancedAwayFromGlass && align > 0.72f && feet.z > -12.5f && feet.z < -4.2f;
            if (!inApproach && !glancedBack) return;

            glassPassPlayed = true;
            scene.atmosphere?.From("distantSteps", new Vector3(0.6f, 0.8f, -2.9f), 0.34f);
            if (!stagedActor.ReducedMotion)
                glassPass = StartCoroutine(CrossBehindGlass(stagedActor));
        }

        private void TickAtCounterRecognition()
        {
            var state = stateGetter?.Invoke();
            if (state == null || state.layer != Reality.B || !state.Happened("CH01-T03"))
                return;

            bool quietDesk = !IsEncounterRunning && !cinematicActive && drawerLookRoutine == null &&
                             (voice == null || !voice.IsSpeaking);
            UpdatePenWritingLoop(quietDesk);

            if (state.Happened("CH01-LAN-NOTICED") || state.Happened("CH01-T04") ||
                recognitionRoutine != null || encounterRoutine != null ||
                !player.CanPlay || (suppressionCheck != null && suppressionCheck()))
                return;

            Vector3 lanPos = stagedActor.transform.position;
            float dist = Vector2.Distance(new Vector2(player.Feet.x, player.Feet.z), new Vector2(lanPos.x, lanPos.z));
            if (dist > 3.65f) return;

            Vector3 toLan = (lanPos + Vector3.up * 1.48f) - player.Eye.transform.position;
            float align = toLan.sqrMagnitude > 0.04f ? Vector3.Dot(player.Eye.transform.forward, toLan.normalized) : 1f;
            if (align < 0.44f) return;

            bool studiedGlass = state.HasDreadEvent(DreadCatalog.B02GlassStudied);
            recognitionRoutine = StartCoroutine(PlayScriptedRecognition(stagedActor, studiedGlass));
        }

        private IEnumerator PlayScriptedRecognition(CharacterActor lan, bool studiedGlass)
        {
            UpdatePenWritingLoop(false);
            lan.LookTarget = MinhEye;
            lan.GazeYawBias = 5.0f;
            lan.GazePitchBias = 1.6f;
            lan.TriggerMicroBlink();
            lan.SetExpression(0.42f, 0.14f, 0.02f);
            lan.PlayAction("Lan_Listen", 0.28f);

            // B02 glass-study consequence: Lan holds eye contact 1 beat longer (~0.9s) before speaking!
            float hold = studiedGlass ? 1.55f : 0.65f;
            for (float t = 0f; t < hold; t += Time.unscaledDeltaTime)
            {
                if (cinematicActive || pose != ReceptionPose.AtCounter) { recognitionRoutine = null; yield break; }
                yield return null;
            }

            onRecognizedMinh?.Invoke();
            if (defaultLineLookup != null)
            {
                var (speaker, text) = defaultLineLookup("CH01-D03");
                if (!string.IsNullOrWhiteSpace(text))
                {
                    bool done = false;
                    voice?.Speak("CH01-D03", speaker, text, scene?.LanVoiceAnchor, lan, onComplete: _ => done = true);
                    while (!done && !cinematicActive && pose == ReceptionPose.AtCounter)
                        yield return null;
                }
            }

            if (!cinematicActive && pose == ReceptionPose.AtCounter && lan != null && lan.gameObject.activeInHierarchy)
            {
                lan.GazeYawBias = 9.5f;
                lan.GazePitchBias = 3.5f;
                lan.SetExpression(0.28f, 0.14f, 0.02f);
                lan.PlayAction("Lan_IdleDesk", 0.35f);
            }
            recognitionRoutine = null;
        }

        private IEnumerator CrossBehindGlass(CharacterActor lan)
        {
            Vector3 start = lan.transform.position;
            Vector3 end = start + lan.transform.forward * 0.82f;
            lan.PlayAction("Lan_StepOut", 0.14f, restart: true);
            const float seconds = 0.68f;
            for (float elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            {
                lan.transform.position = Vector3.Lerp(start, end, elapsed / seconds);
                yield return null;
            }
            lan.transform.position = end;
            lan.PlayAction("Lan_IdleDesk", 0.22f);
            glassPass = null;
        }

        public void ReactToReceptionDrawer()
        {
            if (pose != ReceptionPose.AtCounter || cinematicActive || encounterRoutine != null || stagedActor == null)
                return;
            if (drawerLookRoutine != null) StopCoroutine(drawerLookRoutine);
            drawerLookRoutine = StartCoroutine(PlayDrawerLookReaction(stagedActor));
        }

        private IEnumerator PlayDrawerLookReaction(CharacterActor lan)
        {
            UpdatePenWritingLoop(false);
            lan.TriggerMicroBlink();
            lan.SetExpression(0.14f, 0.38f, 0.06f);
            lan.GazeYawBias = 2.0f;
            lan.GazePitchBias = 8.0f;
            lan.LookTarget = MinhEye;
            lan.PlayAction("Lan_Listen", 0.26f);

            for (float t = 0f; t < 1.75f; t += Time.unscaledDeltaTime)
            {
                if (cinematicActive || pose != ReceptionPose.AtCounter) break;
                yield return null;
            }

            if (!cinematicActive && pose == ReceptionPose.AtCounter && lan != null && lan.gameObject.activeInHierarchy)
            {
                lan.GazeYawBias = 9.5f;
                lan.GazePitchBias = 3.5f;
                lan.SetExpression(0.24f, 0.14f, 0.02f);
                lan.PlayAction("Lan_IdleDesk", 0.38f);
            }
            drawerLookRoutine = null;
        }

        /// <summary>
        /// Pha 3: Player-initiated active cinematic dialogue scene at reception desk.
        /// Takes smooth camera ownership across 4 authored shots, supports safe skip & reduced-motion cuts,
        /// records canonical lines CH01-D03..D07b, and cleanly restores FPS control before calling onComplete.
        /// Never auto-credits OBS_CLOCK_0317 or BOARD pickup.
        /// </summary>
        public bool StartCinematicEncounter(
            Func<string, (string speaker, string text)> lineLookup,
            bool skipOpeningGreeting,
            bool reducedMotion,
            Action onComplete)
        {
            if (cinematicActive || encounterRoutine != null) return false;
            var lan = scene?.LanActor;
            if (lan == null || !lan.gameObject.activeInHierarchy) return false;

            StopSecondaryRoutines();
            voice?.StopCurrent();
            UpdatePenWritingLoop(false);
            skipRequested = false;
            cinematicActive = true;
            cinematicStartedAt = Time.unscaledTime;
            encounterRoutine = StartCoroutine(PlayCinematicSequence(lan, lineLookup, skipOpeningGreeting, reducedMotion, onComplete));
            return true;
        }

        public void RequestSkipCinematic()
        {
            if (!cinematicActive) return;
            skipRequested = true;
            voice?.StopCurrent();
        }

        // Preserved non-cinematic fallback for QA / legacy callers
        public void StartEncounter(Func<string, (string speaker, string text)> lineLookup)
        {
            StopEncounter(false);
            var lan = scene?.LanActor;
            if (lan == null || !lan.gameObject.activeInHierarchy) return;
            UpdatePenWritingLoop(false);
            encounterRoutine = StartCoroutine(PlayEncounterSequence(lan, lineLookup));
        }

        public void AcknowledgeBoard()
        {
            StopEncounter(false);
            var lan = scene?.LanActor;
            if (lan == null || !lan.gameObject.activeInHierarchy) return;

            lan.TriggerMicroBlink();
            lan.SetExpression(0.20f, 0.19f, 0.045f);
            lan.PlayAction("Lan_Listen", 0.32f);
        }

        public void PlayOptionalLine(string lineId, string speaker, string text, float warm, float concern, float sorrow)
        {
            StopEncounter(true);
            UpdatePenWritingLoop(false);
            var lan = scene?.LanActor;
            if (lan != null && lan.gameObject.activeInHierarchy)
            {
                lan.SetExpression(warm, concern, sorrow);
                lan.PlayAction("Lan_TalkGesture", 0.25f);
            }
            voice?.Speak(lineId, speaker, text, scene?.LanVoiceAnchor, lan);
        }

        public void StopEncounter(bool keepMotifQuietly)
        {
            if (encounterRoutine != null)
            {
                StopCoroutine(encounterRoutine);
                encounterRoutine = null;
            }
            if (cinematicActive)
            {
                RestoreCinematicState();
            }
            if (!keepMotifQuietly) voice?.SetLanMotif(false);
            var lan = scene?.LanActor;
            if (lan != null && lan.gameObject.activeInHierarchy)
            {
                lan.LookTarget = pose == ReceptionPose.AtCounter ? MinhEye : null;
                lan.PlayAction("Lan_IdleDesk", 0.35f);
            }
        }

        private void RestoreCinematicState()
        {
            cinematicActive = false;
            skipRequested = false;
            if (hasSavedCameraPose && player != null)
            {
                player.RestoreCameraPose(savedCameraPose);
                hasSavedCameraPose = false;
            }
            else if (player != null)
            {
                player.ExternalCameraControl = false;
            }
            ui?.SetCinematicOverlay(false, false);
        }

        private IEnumerator PlayCinematicSequence(
            CharacterActor lan,
            Func<string, (string speaker, string text)> lineLookup,
            bool skipOpeningGreeting,
            bool reducedMotion,
            Action onComplete)
        {
            try
            {
                if (player != null && player.Eye != null)
                {
                    savedCameraPose = player.CaptureCameraPose();
                    hasSavedCameraPose = true;
                    player.ExternalCameraControl = true;
                }
                ui?.Prompt(null);
                ui?.SetCinematicOverlay(true, reducedMotion);

                lan.LookTarget = MinhEye;
                lan.EnableCloseUpAversion = true;
                lan.GazeYawBias = 9f;
                lan.GazePitchBias = 3f;
                lan.SetExpression(0.32f, 0.12f, 0f);

                // Initial blend from FPS eye to Shot 0
                yield return BlendCameraToShot(0, reducedMotion ? 0f : 0.72f);

                int activeShot = 0;
                foreach (var beat in EncounterBeats)
                {
                    if (skipRequested) break;
                    if (skipOpeningGreeting && beat.id == "CH01-D03")
                    {
                        var (sp, tx) = lineLookup(beat.id);
                        if (!string.IsNullOrWhiteSpace(tx)) voice?.RecordTranscriptOnly(beat.id, sp, tx);
                        continue;
                    }

                    var (speaker, text) = lineLookup(beat.id);
                    if (string.IsNullOrWhiteSpace(text)) continue;

                    if (beat.shotIndex != activeShot)
                    {
                        activeShot = beat.shotIndex;
                        yield return BlendCameraToShot(activeShot, reducedMotion ? 0f : 0.58f);
                        if (skipRequested) break;
                    }

                    lan.SetExpression(beat.warm, beat.concern, beat.sorrow);
                    lan.GazeYawBias = beat.yawBias;
                    lan.GazePitchBias = beat.pitchBias;
                    lan.LookTarget = beat.lookAtBoard && scene.ReceptionBoard != null ? scene.ReceptionBoard.transform : MinhEye;
                    if (beat.microBlink) lan.TriggerMicroBlink();
                    string action = speaker != "Lan" || beat.id is "CH01-D04" or "CH01-D04c" or "CH01-D05b"
                        ? "Lan_Listen"
                        : beat.id == "CH01-D05" && !reducedMotion ? "Lan_TalkGesture" : "Lan_IdleDesk";
                    lan.PlayAction(action, 0.32f);

                    for (float pause = beat.pause; pause > 0f && !skipRequested;)
                    {
                        yield return null;
                        if (!voice.IsPaused) pause -= Time.unscaledDeltaTime;
                    }
                    if (skipRequested) break;

                    bool finished = false;
                    bool isLan = speaker == "Lan";
                    voice.Speak(beat.id, speaker, text,
                        isLan ? scene.LanVoiceAnchor : null, isLan ? lan : null,
                        onComplete: _ => finished = true);

                    if (beat.id == "CH01-D04") voice.SetLanMotif(true, 0.13f);

                    float elapsed = 0f;
                    while (!finished && !skipRequested)
                    {
                        yield return null;
                        if (voice.IsPaused || finished) continue;
                        elapsed += Time.unscaledDeltaTime;
                        if (beat.id == "CH01-D05" && elapsed >= 0.82f && scene.ReceptionBoard != null)
                            lan.LookTarget = scene.ReceptionBoard.transform;
                        if (beat.id == "CH01-D05b" && elapsed >= 0.64f)
                        {
                            lan.LookTarget = MinhEye;
                            lan.GazeYawBias = 5.5f;
                            lan.GazePitchBias = 1.8f;
                            lan.SetExpression(.27f, .18f, .04f);
                        }
                        if (beat.id == "CH01-D07" && elapsed >= 0.76f)
                            lan.LookTarget = MinhEye;
                    }
                }

                // Guarantee all allowed canonical lines CH01-D03..D07b are recorded in transcript even on Skip
                foreach (var beat in EncounterBeats)
                {
                    var (speaker, text) = lineLookup(beat.id);
                    if (!string.IsNullOrWhiteSpace(text))
                        voice?.RecordTranscriptOnly(beat.id, speaker, text);
                }

                // Smoothly return camera to saved FPS pose (instant cut under Reduced Motion or Skip)
                if (player != null && player.Eye != null && hasSavedCameraPose)
                {
                    float retDur = (reducedMotion || skipRequested) ? 0f : 0.62f;
                    if (retDur > 0.01f)
                    {
                        Vector3 fromPos = player.Eye.transform.position;
                        Quaternion fromRot = player.Eye.transform.rotation;
                        float fromFov = player.Eye.fieldOfView;
                        for (float t = 0f; t < retDur; t += Time.unscaledDeltaTime)
                        {
                            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / retDur));
                            player.Eye.transform.SetPositionAndRotation(
                                Vector3.Lerp(fromPos, savedCameraPose.WorldPosition, k),
                                Quaternion.Slerp(fromRot, savedCameraPose.WorldRotation, k));
                            player.Eye.fieldOfView = Mathf.Lerp(fromFov, savedCameraPose.Fov, k);
                            yield return null;
                        }
                    }
                }
            }
            finally
            {
                RestoreCinematicState();
                if (lan != null && lan.gameObject.activeInHierarchy)
                {
                    lan.LookTarget = MinhEye;
                    lan.GazeYawBias = 10f;
                    lan.GazePitchBias = 4f;
                    lan.SetExpression(0.25f, 0.15f, 0.04f);
                    lan.PlayAction("Lan_IdleDesk", 0.32f);
                }
                encounterRoutine = null;
            }

            onComplete?.Invoke();
        }

        private IEnumerator BlendCameraToShot(int shotIndex, float duration)
        {
            if (player == null || player.Eye == null || shotIndex < 0 || shotIndex >= CinematicShots.Length)
                yield break;
            var shot = CinematicShots[shotIndex];
            Quaternion targetRot = Quaternion.LookRotation((shot.lookAt - shot.pos).normalized, Vector3.up);
            if (duration <= 0.01f)
            {
                player.Eye.transform.SetPositionAndRotation(shot.pos, targetRot);
                player.Eye.fieldOfView = shot.fov;
                yield break;
            }
            Vector3 startPos = player.Eye.transform.position;
            Quaternion startRot = player.Eye.transform.rotation;
            float startFov = player.Eye.fieldOfView;
            for (float t = 0f; t < duration && !skipRequested; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
                player.Eye.transform.SetPositionAndRotation(
                    Vector3.Lerp(startPos, shot.pos, k),
                    Quaternion.Slerp(startRot, targetRot, k));
                player.Eye.fieldOfView = Mathf.Lerp(startFov, shot.fov, k);
                yield return null;
            }
            if (!skipRequested)
            {
                player.Eye.transform.SetPositionAndRotation(shot.pos, targetRot);
                player.Eye.fieldOfView = shot.fov;
            }
        }

        private IEnumerator PlayEncounterSequence(CharacterActor lan, Func<string, (string speaker, string text)> lineLookup)
        {
            lan.LookTarget = MinhEye;
            lan.EnableCloseUpAversion = true;
            lan.GazeYawBias = 9f;
            lan.GazePitchBias = 3f;
            lan.SetExpression(0.32f, 0.12f, 0f);

            foreach (var beat in EncounterBeats)
            {
                var (speaker, text) = lineLookup(beat.id);
                if (string.IsNullOrWhiteSpace(text)) continue;

                lan.SetExpression(beat.warm, beat.concern, beat.sorrow);
                lan.GazeYawBias = beat.yawBias;
                lan.GazePitchBias = beat.pitchBias;
                lan.LookTarget = beat.lookAtBoard && scene.ReceptionBoard != null ? scene.ReceptionBoard.transform : MinhEye;
                if (beat.microBlink) lan.TriggerMicroBlink();
                string action = speaker != "Lan" || beat.id is "CH01-D04" or "CH01-D04c" or "CH01-D05b"
                    ? "Lan_Listen"
                    : beat.id == "CH01-D05" && !lan.ReducedMotion ? "Lan_TalkGesture" : "Lan_IdleDesk";
                lan.PlayAction(action, 0.35f);

                for (float pause = beat.pause; pause > 0f;)
                {
                    yield return null;
                    if (!voice.IsPaused) pause -= Time.unscaledDeltaTime;
                }

                bool finished = false;
                bool completed = false;
                bool isLan = speaker == "Lan";
                voice.Speak(beat.id, speaker, text,
                    isLan ? scene.LanVoiceAnchor : null, isLan ? lan : null,
                    onComplete: heard => { completed = heard; finished = true; });

                if (beat.id == "CH01-D04") voice.SetLanMotif(true, 0.13f);

                while (!finished) yield return null;
                if (!completed) break;
            }

            lan.LookTarget = MinhEye;
            lan.GazeYawBias = 10f;
            lan.GazePitchBias = 4f;
            lan.SetExpression(0.25f, 0.15f, 0.04f);
            lan.PlayAction("Lan_IdleDesk", 0.32f);
            encounterRoutine = null;
        }
    }
}
