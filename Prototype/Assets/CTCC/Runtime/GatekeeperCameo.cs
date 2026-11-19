using System;
using System.Collections;
using UnityEngine;

namespace CTCC
{
    /// <summary>
    /// Choreographs CHAR005 Kẻ Giữ Cửa (The Gatekeeper) strictly inside approved CH02 B06 scope (LOC03 corridor -> LOC04).
    /// HARD CANON RULES (Core 02 / Core 04 / CH02-T06 / CH02-Q05):
    /// - NEVER active in CH01 or before G02 is unlocked in CH02.
    /// - 2.0 s spatial warning (SFX-KEY + directional caption + calm crimson light tint).
    /// - Stalk speed 1.78 m/s (slower than Minh's 2.20 m/s walk — escapable walking WITHOUT sprint via BOTH Door A and Door B!).
    /// - Strictly non-lethal; Story Mode reduces cameo to distant silhouette + warning without closing distance.
    /// - Plays ONCE ONLY; records CH02-T06 on reaching LOC04 (z > 16.05) and persists across reloads.
    /// </summary>
    public sealed class GatekeeperCameo : MonoBehaviour
    {
        private HospitalScene scene;
        private VoiceDirector voice;
        private FirstPerson player;
        private HospitalUI ui;

        private bool active;
        private bool resolved;
        private Coroutine routine;
        private float keyStepTimer;
        private float pulsePhase;

        public bool IsActive => active;
        public bool WasResolved => resolved;

        public void Initialize(HospitalScene world, VoiceDirector voiceDirector, FirstPerson fps, HospitalUI hud)
        {
            scene = world;
            voice = voiceDirector;
            player = fps;
            ui = hud;
        }

        public void SyncState(Investigation state)
        {
            resolved = state.Happened("CH02-T06");
            bool allowed = state.chapter == "CH02" && state.layer == Reality.R && GateService.G02(state) && !resolved;
            if (!allowed)
            {
                StopAndHide();
            }
        }

        public bool ShouldTrigger(Investigation state, Vector3 playerFeet)
        {
            if (active || resolved || scene == null || scene.GatekeeperActor == null) return false;
            if (state.chapter != "CH02" || state.layer != Reality.R || !GateService.G02(state) || state.Happened("CH02-T06"))
                return false;
            // Trigger as Minh exits Room 101 into main or staff corridor heading for Emergency (LOC04)
            return playerFeet.x < 5.45f && playerFeet.z > 1.0f && playerFeet.z < 14.5f;
        }

        public void BeginCameo(Investigation state, Func<string, int, string> lineLookup)
        {
            if (active || resolved || scene == null || scene.GatekeeperActor == null) return;
            active = true;
            routine = StartCoroutine(RunCameo(state, lineLookup));
        }

        public bool CheckEscapedToEmergency(Vector3 playerFeet)
        {
            // Escaped cleanly through EITHER Main Corridor Door A (x~4, z>16.05) OR Side Staff Door B (x~0.05, z>16.05)
            return active && playerFeet.z >= 16.05f;
        }

        public void CompleteEscape(Action onPersisted = null)
        {
            if (!active) return;
            resolved = true;
            StopAndHide();
            ui?.Caption("[Tiếng chùm chìa khóa xa dần sau cánh cửa kính mờ Cấp cứu]", 4.5f, true);
            onPersisted?.Invoke();
        }

        public void StopAndHide()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
            active = false;
            if (scene?.GatekeeperActor != null)
                scene.GatekeeperActor.gameObject.SetActive(false);
            ResetEmergencyLights();
        }

        private IEnumerator RunCameo(Investigation state, Func<string, int, string> lineLookup)
        {
            var gk = scene.GatekeeperActor;
            var waypoints = scene.GatekeeperWaypoints;
            bool reduced = state.settings.reduceMotion;
            bool story = state.settings.storyMode;

            // Phase 1: 2.0 s spatial audio & crimson corridor telegraph (ZERO abrupt jumpscare!)
            Vector3 spawnPos = waypoints != null && waypoints.Length > 0 ? waypoints[0].position : new Vector3(4.0f, 0f, -1.4f);
            scene.atmosphere?.From("keyJingle", spawnPos + Vector3.up * 1.1f);
            ui?.Caption("[Tiếng chùm chìa khóa kim loại lạch cạch · phía sau hành lang]", 4.0f, true);

            for (float t = 0f; t < 2.0f; t += Time.unscaledDeltaTime)
            {
                PulseEmergencyLights(t, reduced, 1.35f);
                yield return null;
            }

            // Phase 2: Gatekeeper steps out of south corridor shadow & speaks canonical CH02-D05
            gk.gameObject.SetActive(true);
            gk.ReducedMotion = reduced;
            gk.LookTarget = player != null && player.Eye != null ? player.Eye.transform : null;
            gk.transform.position = spawnPos;
            gk.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            gk.PlayAction(story ? "GK_IdleStill" : "GK_StalkWalk", 0.25f, restart: true);

            string lineText = lineLookup("CH02-D05", 1);
            if (string.IsNullOrWhiteSpace(lineText))
                lineText = "Phòng này không có bệnh nhân. Yêu cầu rời khỏi khu vực.";
            voice?.Speak("CH02-D05", "Người giữ cửa", lineText, scene.GatekeeperVoiceAnchor, gk,
                "[Giọng khàn trầm · phía sau hành lang]");

            if (story)
            {
                // Story/Accessible Mode: distant silhouette + voice warning only; holds position so player walks calmly to LOC04
                while (active)
                {
                    PulseEmergencyLights(Time.unscaledTime, true, 1.15f);
                    yield return null;
                }
                yield break;
            }

            // Phase 3: Measured steady stalk along corridor waypoints at 1.78 m/s (Minh walks 2.20 m/s without sprinting!)
            int wp = 1;
            const float stalkSpeed = 1.78f;
            float repeatCooldown = 7.5f;
            while (active)
            {
                float dt = Mathf.Min(Time.deltaTime, 0.045f);
                pulsePhase += dt;
                PulseEmergencyLights(pulsePhase, reduced, 1.45f);

                Vector3 target = (waypoints != null && wp < waypoints.Length)
                    ? waypoints[wp].position
                    : new Vector3(4.0f, 0f, 14.2f);
                Vector3 toWp = target - gk.transform.position;
                toWp.y = 0f;

                float distToPlayer = player != null ? Vector3.Distance(gk.transform.position, player.Feet) : 10f;
                // Strictly non-lethal safety buffer: pauses respectfully at 1.75 m so player can ALWAYS walk past or into side staff corridor!
                if (distToPlayer < 1.75f)
                {
                    gk.PlayAction("GK_InspectReach", 0.25f);
                    repeatCooldown -= dt;
                    if (repeatCooldown <= 0f)
                    {
                        repeatCooldown = 7.5f;
                        voice?.Speak("CH02-D05", "Người giữ cửa", lineText, scene.GatekeeperVoiceAnchor, gk,
                            "[Gần phía sau · Tiếng chìa khóa]");
                    }
                }
                else if (toWp.sqrMagnitude > 0.06f)
                {
                    gk.PlayAction("GK_StalkWalk", 0.25f);
                    Vector3 dir = toWp.normalized;
                    gk.transform.position += dir * (stalkSpeed * dt);
                    gk.transform.rotation = Quaternion.Slerp(gk.transform.rotation, Quaternion.LookRotation(dir), dt * 4.5f);

                    keyStepTimer += dt;
                    if (keyStepTimer >= 0.92f)
                    {
                        keyStepTimer = 0f;
                        scene.atmosphere?.From("keyJingle", gk.transform.position + Vector3.up * 0.95f);
                    }
                }
                else if (waypoints != null && wp + 1 < waypoints.Length)
                {
                    wp++;
                }
                else
                {
                    // Halts at the ER threshold glass — watches calmly without entering LOC04
                    gk.PlayAction("GK_IdleStill", 0.35f);
                }
                yield return null;
            }
        }

        private void PulseEmergencyLights(float t, bool reduced, float peak)
        {
            if (scene?.EmergencyRedLights == null) return;
            float intensity = reduced ? 1.15f : (0.85f + (Mathf.Sin(t * 2.6f) * 0.5f + 0.5f) * (peak - 0.85f));
            foreach (var l in scene.EmergencyRedLights)
                if (l != null) l.intensity = intensity;
        }

        private void ResetEmergencyLights()
        {
            if (scene?.EmergencyRedLights == null) return;
            foreach (var l in scene.EmergencyRedLights)
                if (l != null) l.intensity = 1.05f;
        }
    }
}
