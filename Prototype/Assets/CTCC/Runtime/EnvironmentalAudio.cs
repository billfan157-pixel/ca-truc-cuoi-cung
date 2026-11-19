using System;
using System.Collections.Generic;
using UnityEngine;

namespace CTCC
{
    [Serializable]
    public sealed class RoomLoop
    {
        public AudioSource source;
        [Range(0, 1)] public float level;
        public bool report;
        public bool exteriorOccluded;
        [NonSerialized] public AudioLowPassFilter filter;
        [NonSerialized] public float currentCutoff;
    }

    public sealed class EnvironmentalAudio : MonoBehaviour
    {
        public RoomLoop[] loops = Array.Empty<RoomLoop>();
        public AudioSource propCue;
        public AudioClip paper, stamp, slippers, step, stepWet, stepCorridor, switchLayer;
        public AudioClip switchToB, switchToR, doorCreak, doorLatch, curtainSlide, cabinetRoll;
        public AudioClip keyJingle, trolleyRoll, phoneBuzz, paStatic, breakerCut, emergencyHum, distantThunder;
        public VoiceClipEntry[] voiceClips = Array.Empty<VoiceClipEntry>();
        public AudioClip lanMotif;

        public bool ReportLayer { get; set; }
        private readonly List<RoomLoop> activeLoops = new();
        private bool mono;
        private float footstepPitch;
        private float ambienceScale = 0.85f;
        private float sfxScale = 0.90f;
        private float duckGain = 1.0f;
        private float duckTarget = 1.0f;
        private Transform listener;
        private bool started;

        private void Start()
        {
            started = true;
            var cam = Camera.main;
            if (cam != null) listener = cam.transform;
            if (loops != null)
            {
                foreach (var loop in loops)
                    AttachLoop(loop);
            }
        }

        public void RegisterZoneLoops(RoomLoop[] extra)
        {
            if (extra == null) return;
            foreach (var loop in extra)
                AttachLoop(loop);
        }

        public void UnregisterZoneLoops(RoomLoop[] extra)
        {
            if (extra == null) return;
            foreach (var loop in extra)
                activeLoops.Remove(loop);
        }

        private void AttachLoop(RoomLoop loop)
        {
            if (loop == null || loop.source == null || activeLoops.Contains(loop)) return;
            activeLoops.Add(loop);
            loop.source.volume = 0f;
            loop.source.spatialBlend = mono ? 0f : 0.96f;
            if (loop.exteriorOccluded)
            {
                loop.filter = loop.source.gameObject.GetComponent<AudioLowPassFilter>();
                if (loop.filter == null)
                    loop.filter = loop.source.gameObject.AddComponent<AudioLowPassFilter>();
                loop.currentCutoff = 3200f;
                loop.filter.cutoffFrequency = 3200f;
            }
            if (started || Application.isPlaying)
                loop.source.Play();
        }

        public void SetVoiceDucking(bool ducking)
        {
            // -5 dB smooth ambience ducking during spoken Vietnamese VO so every word stays crisp!
            duckTarget = ducking ? 0.56f : 1.0f;
        }

        private void Update()
        {
            if (listener == null && Camera.main != null) listener = Camera.main.transform;
            float dt = Time.unscaledDeltaTime;
            float amount = dt * 1.65f;
            duckGain = Mathf.MoveTowards(duckGain, duckTarget, dt * 2.2f);
            bool checkOcclusion = Time.frameCount % 6 == 0 && listener != null;

            for (int i = activeLoops.Count - 1; i >= 0; i--)
            {
                var loop = activeLoops[i];
                var source = loop?.source;
                if (source == null)
                {
                    activeLoops.RemoveAt(i);
                    continue;
                }
                float target = (ReportLayer == loop.report ? loop.level : 0f) * ambienceScale * duckGain;

                if (loop.exteriorOccluded && loop.filter != null && listener != null)
                {
                    if (checkOcclusion)
                    {
                        Vector3 from = listener.position;
                        Vector3 to = source.transform.position;
                        Vector3 dir = to - from;
                        float dist = dir.magnitude;
                        bool blocked = dist > 0.6f && Physics.Raycast(from, dir / dist, Mathf.Min(dist - 0.35f, 18f),
                            HospitalScene.SolidMask, QueryTriggerInteraction.Ignore);
                        loop.currentCutoff = blocked ? 1350f : 6200f;
                    }
                    loop.filter.cutoffFrequency = Mathf.MoveTowards(loop.filter.cutoffFrequency, loop.currentCutoff, dt * 3600f);
                }

                source.volume = Mathf.MoveTowards(source.volume, target, amount);
            }
        }

        public void Apply(VisitSettings options)
        {
            AudioListener.volume = Mathf.Clamp01(options.volume);
            ambienceScale = Mathf.Clamp01(options.ambienceVolume);
            sfxScale = Mathf.Clamp01(options.sfxVolume);
            mono = options.monoAudio;
            foreach (var loop in activeLoops)
                if (loop?.source != null)
                    loop.source.spatialBlend = mono ? 0f : 0.96f;
        }

        public void From(string cue, Vector3 at, float gainScale = 1f)
        {
            var sound = cue switch
            {
                "paper" => paper,
                "stamp" => stamp,
                "slippers" => slippers,
                "switch" => switchLayer,
                "switchToB" => switchToB != null ? switchToB : switchLayer,
                "switchToR" => switchToR != null ? switchToR : switchLayer,
                "doorCreak" => doorCreak,
                "doorLatch" => doorLatch,
                "curtainSlide" => curtainSlide,
                "cabinetRoll" => cabinetRoll,
                "keyJingle" => keyJingle,
                "trolleyRoll" => trolleyRoll,
                "phoneBuzz" => phoneBuzz,
                "paStatic" => paStatic,
                "breakerCut" => breakerCut,
                "emergencyHum" => emergencyHum,
                "distantThunder" => distantThunder,
                _ => null
            };
            if (sound == null || propCue == null) return;
            propCue.transform.position = at;
            propCue.spatialBlend = mono ? 0f : 0.92f;
            float baseVol = cue switch
            {
                "switch" or "switchToB" or "switchToR" => 0.44f,
                "cabinetRoll" => 0.66f,
                "doorCreak" => 0.56f,
                "curtainSlide" => 0.52f,
                "keyJingle" => 0.72f,
                "trolleyRoll" => 0.62f,
                "breakerCut" => 0.74f,
                "distantThunder" => 0.48f,
                "phoneBuzz" => 0.55f,
                _ => 0.58f
            };
            propCue.PlayOneShot(sound, baseVol * sfxScale * gainScale);
        }

        public void Step(AudioSource source, bool running, bool crouched, Vector3 worldPos, bool reportLayer)
        {
            AudioClip clip = step;
            // Exterior wet courtyard (z < -8), damp north window puddle & corridor track in R (2012)
            bool wetZone = !reportLayer && (
                worldPos.z < -8.2f ||
                (worldPos.x > 7.4f && worldPos.x < 11.4f && worldPos.z > 6.3f && worldPos.z < 8.1f) ||
                (worldPos.x > 3.8f && worldPos.x < 5.5f && worldPos.z > 4.6f && worldPos.z < 9.2f)
            );
            bool corridorZone = worldPos.x > 2.3f && worldPos.x < 5.6f && worldPos.z > 0f && worldPos.z < 16f;
            if (wetZone && stepWet != null) clip = stepWet;
            else if (corridorZone && stepCorridor != null) clip = stepCorridor;
            if (clip == null) return;

            footstepPitch = Mathf.Repeat(footstepPitch + 0.618f, 1f);
            source.pitch = 0.90f + footstepPitch * 0.20f;
            source.PlayOneShot(clip, (crouched ? 0.16f : running ? 0.50f : 0.32f) * sfxScale);
        }
    }
}
