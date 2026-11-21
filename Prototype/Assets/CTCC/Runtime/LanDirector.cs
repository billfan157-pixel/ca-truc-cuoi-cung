using System;
using System.Collections;
using UnityEngine;

namespace CTCC
{
    /// <summary>Stages Lan's CH01 appearance from the glass to the reception desk.</summary>
    public sealed class LanDirector : MonoBehaviour
    {
        private enum ReceptionPose { Absent, BehindGlass, AtCounter }

        private HospitalScene scene;
        private VoiceDirector voice;
        private FirstPerson player;
        private CharacterActor stagedActor;
        private ReceptionPose pose;
        private Coroutine encounterRoutine;
        private Coroutine glassPass;
        private bool glassPassPlayed;

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

        public void StartEncounter(Func<string, int, string> lineLookup)
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

        private IEnumerator PlayEncounterSequence(CharacterActor lan, Func<string, int, string> lineLookup)
        {
            // Let the room settle after AN01, then let the familiarity of D03 lead the scene.
            lan.LookTarget = MinhEye;
            lan.SetExpression(0.32f, 0.12f, 0f);
            var beats = new (string id, bool isLan, float pause, float warm, float concern, float sorrow)[]
            {
                ("CH01-D03", true, 0.52f, 0.49f, 0.12f, 0f),
                ("CH01-D04", false, 0.66f, 0.27f, 0.25f, 0.04f),
                ("CH01-D05", true, 0.95f, 0.34f, 0.22f, 0.05f),
                ("CH01-D07", true, 0.24f, 0.38f, 0.19f, 0.04f),
            };

            foreach (var beat in beats)
            {
                lan.SetExpression(beat.warm, beat.concern, beat.sorrow);
                lan.PlayAction(beat.id is "CH01-D04" or "CH01-D05" ? "Lan_Listen" : "Lan_IdleDesk", 0.32f);
                for (float pause = beat.pause; pause > 0f;)
                {
                    yield return null;
                    if (!voice.IsPaused) pause -= Time.unscaledDeltaTime;
                }

                bool finished = false;
                bool completed = false;
                voice.Speak(beat.id, beat.isLan ? "Lan" : "Minh", lineLookup(beat.id, 1),
                    beat.isLan ? scene.LanVoiceAnchor : null, beat.isLan ? lan : null,
                    onComplete: heard => { completed = heard; finished = true; });

                if (beat.id == "CH01-D04") voice.SetLanMotif(true, 0.13f);

                // At "phòng 101", look toward F101. Return to Minh for "như hồi đó".
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
            lan.SetExpression(0.25f, 0.15f, 0.04f);
            lan.PlayAction("Lan_IdleDesk", 0.32f);
            encounterRoutine = null;
        }
    }
}
