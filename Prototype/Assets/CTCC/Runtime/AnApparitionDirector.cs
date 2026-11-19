using System;
using System.Collections;
using UnityEngine;

namespace CTCC
{
    /// <summary>
    /// Choreographs gentle, non-hostile CHAR007/C01 Bé An memory vignettes in CH02 (LOC03):
    /// - CH02-B02 (slipper cue): brief translucent apparition glance near Room 101 threshold, fading peacefully.
    /// - CH02-B05 (after VerifyAn / G02): appears calmly by Bed 4 alcove, speaks CH02-D03 ("Chú có thấy cửa nhà con không?"),
    ///   plays An_TurnFade, and dissolves gently with zero horror distortion or strobe.
    /// </summary>
    public sealed class AnApparitionDirector : MonoBehaviour
    {
        private HospitalScene scene;
        private VoiceDirector voice;
        private FirstPerson player;
        private Coroutine activeRoutine;

        public void Initialize(HospitalScene world, VoiceDirector voiceDirector, FirstPerson fps)
        {
            scene = world;
            voice = voiceDirector;
            player = fps;
            HideImmediately();
        }

        public void HideImmediately()
        {
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }
            if (scene?.AnActor != null)
            {
                scene.AnActor.SetApparitionAlpha(0f);
                scene.AnActor.gameObject.SetActive(false);
            }
        }

        public void PlaySlipperGlimpse(bool reducedMotion)
        {
            var an = scene?.AnActor;
            if (an == null) return;
            HideImmediately();
            activeRoutine = StartCoroutine(RunGlimpse(an, new Vector3(7.4f, 0f, 4.75f), Quaternion.Euler(0f, -92f, 0f), reducedMotion));
        }

        public void PlayPostVerifyVignette(string lineText, bool reducedMotion)
        {
            var an = scene?.AnActor;
            if (an == null) return;
            HideImmediately();
            activeRoutine = StartCoroutine(RunPostVerify(an, lineText, reducedMotion));
        }

        private IEnumerator RunGlimpse(CharacterActor an, Vector3 pos, Quaternion rot, bool reducedMotion)
        {
            an.gameObject.SetActive(true);
            an.ReducedMotion = reducedMotion;
            an.LookTarget = player != null && player.Eye != null ? player.Eye.transform : null;
            an.transform.SetPositionAndRotation(pos, rot);
            an.PlayAction("An_IdleLook", 0.2f, restart: true);

            yield return Fade(an, 0f, 0.56f, reducedMotion ? 0.25f : 0.85f);
            yield return new WaitForSecondsRealtime(2.2f);
            an.PlayAction("An_TurnFade", 0.35f, restart: true);
            yield return Fade(an, 0.56f, 0f, reducedMotion ? 0.3f : 1.15f);
            an.gameObject.SetActive(false);
            activeRoutine = null;
        }

        private IEnumerator RunPostVerify(CharacterActor an, string lineText, bool reducedMotion)
        {
            an.gameObject.SetActive(true);
            an.ReducedMotion = reducedMotion;
            an.LookTarget = player != null && player.Eye != null ? player.Eye.transform : null;
            an.transform.SetPositionAndRotation(new Vector3(10.85f, 0f, 3.65f), Quaternion.Euler(0f, -68f, 0f));
            an.PlayAction("An_IdleLook", 0.2f, restart: true);

            yield return Fade(an, 0f, 0.68f, reducedMotion ? 0.3f : 1.0f);
            bool done = false;
            voice?.Speak("CH02-D03", "Bé An", string.IsNullOrWhiteSpace(lineText) ? "Chú có thấy cửa nhà con không?" : lineText,
                scene.AnVoiceAnchor, an, "[Giọng trẻ nhỏ rất khẽ · góc giường 4]", () => done = true);
            while (!done) yield return null;

            yield return new WaitForSecondsRealtime(0.4f);
            an.PlayAction("An_TurnFade", 0.30f, restart: true);
            yield return Fade(an, 0.68f, 0f, reducedMotion ? 0.35f : 1.5f);
            an.gameObject.SetActive(false);
            activeRoutine = null;
        }

        private static IEnumerator Fade(CharacterActor an, float a, float b, float seconds)
        {
            float dur = Mathf.Max(0.05f, seconds);
            for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
            {
                an.SetApparitionAlpha(Mathf.Lerp(a, b, Mathf.Clamp01(t / dur)));
                yield return null;
            }
            an.SetApparitionAlpha(b);
        }
    }
}
