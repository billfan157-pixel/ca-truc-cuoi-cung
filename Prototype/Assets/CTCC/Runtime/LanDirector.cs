using System;
using System.Collections;
using UnityEngine;

namespace CTCC
{
    /// <summary>
    /// Choreographs CHAR002 Trần Ngọc Lan canonically across CH01 B01–B06:
    /// - B01–B02 (R, before entering reception): subtle human silhouette visible behind reception glass
    /// - B03 (First R->B entry at AP02): warm voiced 3D encounter with step-out, eye contact, lip-sync & MUS-LAN motif
    /// - B04–B05 (B, before collapse): stands peacefully by reception counter ready to hand over F101 board
    /// - B06 (Collapse back to R): cleanly absent — zero monster distortion, zero jumpscare vanish (Core 02/04 canon).
    /// </summary>
    public sealed class LanDirector : MonoBehaviour
    {
        private HospitalScene scene;
        private VoiceDirector voice;
        private FirstPerson player;
        private Coroutine encounterRoutine;
        private bool steppingOut;

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
            var sil = scene.ReceptionGlassSilhouette;

            bool beforeFirstReception = state.chapter == "CH01" && !state.Happened("CH01-T03");
            bool inOperationalReception = state.chapter == "CH01" && state.layer == Reality.B && state.Happened("CH01-T03") && !state.Happened("CH01-T08");

            // Subtle human silhouette behind reception partition while Minh approaches in R (B01-B02)
            if (sil != null) sil.SetActive(beforeFirstReception);

            if (lan == null) return;
            lan.ReducedMotion = state.settings.reduceMotion;
            lan.LookTarget = player != null && player.Eye != null ? player.Eye.transform : null;

            if (beforeFirstReception)
            {
                lan.gameObject.SetActive(true);
                if (scene.LanSilhouettePoint != null)
                    lan.transform.SetPositionAndRotation(scene.LanSilhouettePoint.position, scene.LanSilhouettePoint.rotation);
                lan.PlayAction("Lan_IdleDesk", 0.2f);
                lan.SetExpression(0.25f, 0.15f, 0f);
            }
            else if (inOperationalReception)
            {
                lan.gameObject.SetActive(true);
                if (!steppingOut)
                {
                    var anchor = state.Happened("CH01-T05") ? scene.LanDeskPoint : (scene.LanStepOutPoint != null ? scene.LanStepOutPoint : scene.LanDeskPoint);
                    if (anchor != null)
                        lan.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
                    lan.PlayAction(state.Happened("CH01-T05") ? "Lan_IdleDesk" : "Lan_TalkGesture", 0.3f);
                }
                lan.SetExpression(0.55f, 0.22f, 0.08f);
            }
            else
            {
                // CH01 B06 collapse & all of CH02: Lan is simply absent (canonical restraint, no vanish jumpscare)
                StopEncounter(false);
                lan.gameObject.SetActive(false);
            }
        }

        public void StartEncounter(Func<string, int, string> lineLookup)
        {
            StopEncounter(false);
            var lan = scene?.LanActor;
            voice?.SetLanMotif(true, 0.22f);
            if (lan != null)
            {
                lan.gameObject.SetActive(true);
                lan.LookTarget = player != null && player.Eye != null ? player.Eye.transform : null;
                encounterRoutine = StartCoroutine(PlayEncounterSequence(lan, lineLookup));
            }
        }

        public void PlayHandoverGesture()
        {
            var lan = scene?.LanActor;
            if (lan == null || !lan.gameObject.activeInHierarchy) return;
            lan.SetExpression(0.45f, 0.28f, 0.10f);
            lan.PlayAction("Lan_Handover", 0.25f, restart: true);
        }

        public void PlayOptionalLine(string lineId, string speaker, string text, float warm, float concern, float sorrow)
        {
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
            steppingOut = false;
            if (!keepMotifQuietly) voice?.SetLanMotif(false);
            var lan = scene?.LanActor;
            if (lan != null && lan.gameObject.activeInHierarchy)
                lan.PlayAction("Lan_IdleDesk", 0.35f);
        }

        private IEnumerator PlayEncounterSequence(CharacterActor lan, Func<string, int, string> lineLookup)
        {
            steppingOut = true;
            Vector3 fromPos = scene.LanDeskPoint != null ? scene.LanDeskPoint.position : lan.transform.position;
            Vector3 toPos = scene.LanStepOutPoint != null ? scene.LanStepOutPoint.position : fromPos;
            Quaternion toRot = scene.LanStepOutPoint != null ? scene.LanStepOutPoint.rotation : lan.transform.rotation;

            lan.transform.position = fromPos;
            lan.SetExpression(0.65f, 0.15f, 0f);
            lan.PlayAction("Lan_StepOut", 0.2f, restart: true);

            float moveDur = lan.ReducedMotion ? 0.15f : 1.45f;
            for (float t = 0f; t < moveDur; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / moveDur);
                lan.transform.position = Vector3.Lerp(fromPos, toPos, k);
                lan.transform.rotation = Quaternion.Slerp(lan.transform.rotation, toRot, k);
                yield return null;
            }
            lan.transform.SetPositionAndRotation(toPos, toRot);
            steppingOut = false;

            // Canonical voiced encounter sequence (CH01-D03 -> D04 -> D05 -> D07)
            var beats = new (string id, string speaker, bool isLan, float warm, float concern, float sorrow)[]
            {
                ("CH01-D03", "Lan", true, 0.72f, 0.12f, 0.0f),
                ("CH01-D04", "Minh", false, 0.40f, 0.25f, 0.0f),
                ("CH01-D05", "Lan", true, 0.55f, 0.30f, 0.05f),
                ("CH01-D07", "Lan", true, 0.28f, 0.45f, 0.20f),
            };

            foreach (var b in beats)
            {
                bool finished = false;
                if (b.isLan)
                {
                    lan.SetExpression(b.warm, b.concern, b.sorrow);
                    lan.PlayAction("Lan_TalkGesture", 0.28f);
                }
                else
                {
                    lan.PlayAction("Lan_IdleDesk", 0.28f);
                }
                voice?.Speak(b.id, b.speaker, lineLookup(b.id, 1),
                    b.isLan ? scene.LanVoiceAnchor : null,
                    b.isLan ? lan : null,
                    onComplete: () => finished = true);
                while (!finished) yield return null;
                yield return new WaitForSecondsRealtime(0.28f);
            }

            lan.PlayAction("Lan_Handover", 0.30f, restart: true);
            encounterRoutine = null;
        }
    }
}
