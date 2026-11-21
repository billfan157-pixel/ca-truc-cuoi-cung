using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CTCC
{
    /// <summary>Stages Lan's CH01 appearance from the glass to the reception desk.</summary>
    public sealed class LanDirector : MonoBehaviour
    {
        private enum ReceptionPose { Absent, BehindGlass, AtCounter }

        private static readonly (string id, float pause, float warm, float concern, float sorrow,
            float yawBias, float pitchBias, bool lookAtBoard, bool microBlink)[] EncounterBeats =
        {
            ("CH01-D03", 0.52f, 0.46f, 0.12f, 0.00f,  7.5f, 2.0f, false, true),
            ("CH01-D03b", 0.55f, 0.36f, 0.16f, 0.02f, 11.0f, 4.0f, false, false),
            ("CH01-D03c", 0.48f, 0.38f, 0.20f, 0.02f, 14.0f, 5.0f, false, true),
            ("CH01-D04", 0.72f, 0.24f, 0.28f, 0.06f, 16.5f, 6.5f, false, true),
            ("CH01-D04c", 0.58f, 0.26f, 0.26f, 0.07f, 19.0f, 7.0f, false, true),
            ("CH01-D05", 0.76f, 0.34f, 0.22f, 0.05f,  6.0f, 4.0f, true,  true),
            ("CH01-D05b", 0.54f, 0.28f, 0.24f, 0.08f, 10.5f, 5.5f, true,  false),
            ("CH01-D05c", 0.46f, 0.35f, 0.20f, 0.05f,  4.0f, 6.0f, true,  true),
            ("CH01-D07", 0.34f, 0.38f, 0.19f, 0.04f,  8.5f, 2.5f, false, true),
            ("CH01-D07b", 0.44f, 0.25f, 0.18f, 0.05f, 12.0f, 4.5f, false, false),
        };

        private HospitalScene scene;
        private VoiceDirector voice;
        private FirstPerson player;
        private CharacterActor stagedActor;
        private ReceptionPose pose;
        private Coroutine encounterRoutine;
        private Coroutine glassPass;
        private bool glassPassPlayed;

        internal static IEnumerable<string> EncounterLineIds()
        {
            foreach (var beat in EncounterBeats) yield return beat.id;
        }

        public bool IsEncounterRunning => encounterRoutine != null;

        private Transform MinhEye => player?.Eye != null ? player.Eye.transform : null;

        public void Initialize(HospitalScene world, VoiceDirector voiceDirector, FirstPerson fps)
        {
            scene = world;
            voice = voiceDirector;
            player = fps;
        }

        public void SyncState(Investigation state)
        {
            if (scene == null) return;
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

            if (glassLight != null) glassLight.SetActive(next == ReceptionPose.BehindGlass);
            if (lan == null) return;
            lan.ReducedMotion = state.settings.reduceMotion;
            // Saving dialogue or collecting evidence can reapply the same state during a performance.
            if (lan == stagedActor && next == pose) return;

            StopEncounter(false);
            if (glassPass != null)
            {
                StopCoroutine(glassPass);
                glassPass = null;
            }
            glassPassPlayed = false;
            stagedActor = lan;
            pose = next;
            lan.gameObject.SetActive(pose != ReceptionPose.Absent);
            lan.SetHeldClipboardVisible(false);
            if (pose == ReceptionPose.Absent) return;

            var mark = pose == ReceptionPose.BehindGlass ? scene.LanSilhouettePoint : scene.LanStepOutPoint;
            if (mark == null) mark = scene.LanDeskPoint;
            if (mark != null) lan.transform.SetPositionAndRotation(mark.position, mark.rotation);
            lan.LookTarget = pose == ReceptionPose.AtCounter ? MinhEye : null;
            lan.EnableCloseUpAversion = pose == ReceptionPose.AtCounter;
            // Semi-profile resting gaze bias eliminates flat frontal uncanny stare while keeping warmth
            lan.GazeYawBias = pose == ReceptionPose.AtCounter ? 9.5f : 0f;
            lan.GazePitchBias = pose == ReceptionPose.AtCounter ? 3.5f : 0f;
            lan.SetExpression(0.24f, 0.12f, 0f);
            lan.PlayAction("Lan_IdleDesk", 0.2f);
        }

        private void Update()
        {
            if (pose != ReceptionPose.BehindGlass || glassPassPlayed || !player.CanPlay || stagedActor == null)
                return;

            // From here the approach sightline crosses the south pane rather than the open end of the counter.
            Vector3 feet = player.Feet;
            if (feet.z < -8.1f || feet.z > -4.0f || feet.x < 2.4f || feet.x > 6.0f)
                return;

            glassPassPlayed = true;
            if (!stagedActor.ReducedMotion)
                glassPass = StartCoroutine(CrossBehindGlass(stagedActor));
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

        public void StartEncounter(Func<string, (string speaker, string text)> lineLookup)
        {
            StopEncounter(false);
            var lan = scene?.LanActor;
            if (lan == null || !lan.gameObject.activeInHierarchy) return;
            encounterRoutine = StartCoroutine(PlayEncounterSequence(lan, lineLookup));
        }

        public void AcknowledgeBoard()
        {
            StopEncounter(false);
            var lan = scene?.LanActor;
            if (lan == null || !lan.gameObject.activeInHierarchy) return;

            // F101 was already on the counter. Lan acknowledges Minh taking it.
            lan.SetExpression(0.25f, 0.22f, 0.06f);
        }

        public void PlayOptionalLine(string lineId, string speaker, string text, float warm, float concern, float sorrow)
        {
            StopEncounter(true);
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
            if (!keepMotifQuietly) voice?.SetLanMotif(false);
            var lan = scene?.LanActor;
            if (lan != null && lan.gameObject.activeInHierarchy)
            {
                lan.LookTarget = pose == ReceptionPose.AtCounter ? MinhEye : null;
                lan.PlayAction("Lan_IdleDesk", 0.35f);
            }
        }

        private IEnumerator PlayEncounterSequence(CharacterActor lan, Func<string, (string speaker, string text)> lineLookup)
        {
            // Lan speaks as if the night shift is still underway, with averted gaze and subtle blinks.
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
                lan.PlayAction(beat.id is "CH01-D04" or "CH01-D05" or "CH01-D05b" ? "Lan_Listen" : "Lan_IdleDesk", 0.34f);

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

                // Subtle mid-line gaze shifts keep Lan feeling grounded & alive without intense frontal staring.
                float elapsed = 0f;
                while (!finished)
                {
                    yield return null;
                    if (voice.IsPaused || finished) continue;
                    elapsed += Time.unscaledDeltaTime;
                    if (beat.id == "CH01-D05" && elapsed >= 0.82f && scene.ReceptionBoard != null)
                        lan.LookTarget = scene.ReceptionBoard.transform;
                    if (beat.id == "CH01-D07" && elapsed >= 0.76f)
                        lan.LookTarget = MinhEye;
                }
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
