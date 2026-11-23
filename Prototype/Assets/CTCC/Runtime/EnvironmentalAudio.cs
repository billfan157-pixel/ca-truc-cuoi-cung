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
        [NonSerialized] public float mixLevel;
        [NonSerialized] public float obstruction;
    }

    public sealed class EnvironmentalAudio : MonoBehaviour
    {
        public RoomLoop[] loops = Array.Empty<RoomLoop>();
        public AudioSource propCue;
        public AudioClip paper, stamp, slippers, step, stepWet, stepCorridor, switchLayer;
        public AudioClip switchToB, switchToR, doorCreak, doorLatch, curtainSlide, cabinetRoll;
        public AudioClip keyJingle, trolleyRoll, phoneBuzz, paStatic, breakerCut, emergencyHum, distantThunder;
        public AudioClip drawerSlide, paperFlip, clockTick, distantSteps, chairScrape, partitionKnock;
        public VoiceClipEntry[] voiceClips = Array.Empty<VoiceClipEntry>();
        public AudioClip lanMotif;

        public bool ReportLayer { get; set; }
        internal FormStatus F101Report { get; set; }

        private sealed class CueVoice
        {
            public AudioSource source;
            public AudioLowPassFilter filter;
            public float level;
            public float obstruction;
            public bool checkWalls;
            public float reverbSend;
            public int importance;
            public double startedAt;
            public double endsAt;
        }

        private const int CueVoiceCount = 8;
        private readonly List<RoomLoop> activeLoops = new();
        private readonly List<CueVoice> cueVoices = new(CueVoiceCount);
        private bool mono;
        private float footstepPitch;
        private float ambienceScale = 0.85f;
        private float sfxScale = 0.90f;
        private float duckGain = 1.0f;
        private float duckTarget = 1.0f;
        private Transform listener;
        private float nextWallCheck;
        private float signatureQuietUntil;
        private float signatureGain = 1f;
        private AudioClip signaturePen;
        private AudioClip cradleClick, enamelPedal, ceramicContact;
        private AudioClip signKnock, keyTurn, latchClick, gateSlam, bagZip, clipSnap;
        private AudioSource handset;
        private AudioSource penWriteLoop;
        private bool penWriteActive;
        private float penWriteGain;
        private float tensionDipUntil;
        private float tensionDipFloor = 1f;
        private float tensionGain = 1f;
        private float returnWalkRainScale = 1f;
        internal Transform Listener => listener;

        public string DirectionTo(Vector3 position)
        {
            if (listener == null) return "gần đây";
            Vector3 offset = position - listener.position;
            offset.y = 0f;
            if (offset.sqrMagnitude < 0.25f) return "gần đây";
            Vector3 forward = Vector3.Cross(listener.right, Vector3.up);
            float front = Vector3.Dot(forward, offset);
            float side = Vector3.Dot(listener.right, offset);
            if (Mathf.Abs(side) > Mathf.Abs(front)) return side > 0f ? "bên phải" : "bên trái";
            return front >= 0f ? "phía trước" : "phía sau";
        }

        private void Start()
        {
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
                if (loop != null && activeLoops.Remove(loop) && loop.source != null)
                    loop.source.Stop();
        }

        private void AttachLoop(RoomLoop loop)
        {
            if (loop == null || loop.source == null || activeLoops.Contains(loop)) return;
            activeLoops.Add(loop);
            loop.mixLevel = 0f;
            loop.obstruction = 0f;
            loop.source.volume = 0f;
            loop.source.spatialBlend = mono ? 0f : 1f;
            loop.source.reverbZoneMix = mono ? 0f : 0.40f;
            if (loop.exteriorOccluded)
            {
                loop.filter = loop.source.gameObject.GetComponent<AudioLowPassFilter>();
                if (loop.filter == null)
                    loop.filter = loop.source.gameObject.AddComponent<AudioLowPassFilter>();
                loop.currentCutoff = 3200f;
                loop.filter.cutoffFrequency = 3200f;
            }
            if (Application.isPlaying && loop.source.clip != null)
                loop.source.Play();
        }

        public void SetVoiceDucking(bool ducking)
        {
            duckTarget = ducking ? 0.56f : 1.0f;
        }

        internal void LeaveSilenceAfterStamp(float seconds) => signatureQuietUntil = Time.unscaledTime + seconds;

        /// <summary>
        /// Walkthrough III mix discipline: brief tension ambience dip that never cuts master/footsteps to 0 dBFS.
        /// </summary>
        internal void PulseAmbienceDip(float seconds, float floor = 0.18f)
        {
            tensionDipUntil = Mathf.Max(tensionDipUntil, Time.unscaledTime + Mathf.Clamp(seconds, 0.2f, 4.5f));
            tensionDipFloor = Mathf.Clamp(floor, 0.12f, 0.85f);
        }

        internal void SetReturnWalkRainScale(float scale) =>
            returnWalkRainScale = Mathf.Clamp(scale, 0.42f, 1f);

        internal void SetPenWriting(bool active, Vector3 worldPos)
        {
            penWriteActive = active;
            if (active && penWriteLoop == null)
            {
                var clip = Resources.Load<AudioClip>("CTCC/CH01_PenWrite");
                if (clip == null) return;
                var go = new GameObject("Lan Desk Pen Loop");
                go.transform.SetParent(transform, false);
                go.transform.position = worldPos;
                penWriteLoop = go.AddComponent<AudioSource>();
                penWriteLoop.clip = clip;
                penWriteLoop.loop = true;
                penWriteLoop.playOnAwake = false;
                penWriteLoop.dopplerLevel = 0f;
                penWriteLoop.rolloffMode = AudioRolloffMode.Linear;
                penWriteLoop.minDistance = 1.1f;
                penWriteLoop.maxDistance = 8.5f;
                penWriteLoop.volume = 0f;
                if (propCue != null) penWriteLoop.outputAudioMixerGroup = propCue.outputAudioMixerGroup;
                penWriteLoop.Play();
            }
            if (penWriteLoop != null)
            {
                penWriteLoop.transform.position = worldPos;
                if (active && !penWriteLoop.isPlaying) penWriteLoop.Play();
            }
        }

        internal static float DistanceGain(AudioSource source, Transform ear)
        {
            if (ear == null) return 1f;
            float distance = Vector3.Distance(ear.position, source.transform.position);
            return 1f - Mathf.InverseLerp(source.minDistance, source.maxDistance, distance);
        }

        private bool IsObstructed(Vector3 target)
        {
            if (listener == null) return false;
            Vector3 offset = target - listener.position;
            float distance = offset.magnitude;
            return distance > 0.6f && Physics.Raycast(listener.position, offset / distance, distance - 0.35f,
                HospitalScene.SolidMask, QueryTriggerInteraction.Ignore);
        }

        private void Update()
        {
            if (listener == null && Camera.main != null) listener = Camera.main.transform;
            float dt = Time.unscaledDeltaTime;
            duckGain = Mathf.MoveTowards(duckGain, duckTarget, dt * (duckTarget < duckGain ? 4f : 0.95f));
            float signatureTarget = Time.unscaledTime < signatureQuietUntil ? 0.14f : 1f;
            signatureGain = Mathf.MoveTowards(signatureGain, signatureTarget, dt * (signatureTarget < signatureGain ? 9f : 1.8f));
            float tensionTarget = Time.unscaledTime < tensionDipUntil ? tensionDipFloor : 1f;
            tensionGain = Mathf.MoveTowards(tensionGain, tensionTarget, dt * (tensionTarget < tensionGain ? 6.5f : 1.3f));
            bool checkOcclusion = Time.unscaledTime >= nextWallCheck;
            if (checkOcclusion) nextWallCheck = Time.unscaledTime + 0.1f;

            for (int i = activeLoops.Count - 1; i >= 0; i--)
            {
                var loop = activeLoops[i];
                var source = loop?.source;
                if (source == null)
                {
                    activeLoops.RemoveAt(i);
                    continue;
                }
                loop.mixLevel = Mathf.MoveTowards(loop.mixLevel, ReportLayer == loop.report ? 1f : 0f, dt / 0.75f);

                if (loop.exteriorOccluded && loop.filter != null && listener != null)
                {
                    if (checkOcclusion && loop.mixLevel > 0f && DistanceGain(source, listener) > 0f)
                        loop.currentCutoff = IsObstructed(source.transform.position) ? 1350f : 8500f;
                    loop.filter.cutoffFrequency = Mathf.MoveTowards(loop.filter.cutoffFrequency, loop.currentCutoff, dt * 3600f);
                    loop.obstruction = Mathf.MoveTowards(loop.obstruction, loop.currentCutoff < 2000f ? 1f : 0f, dt * 4f);
                }

                float distance = mono ? DistanceGain(source, listener) : 1f;
                float envelope = Mathf.Sin(loop.mixLevel * Mathf.PI * 0.5f);
                if (source.clip != null && source.clip.name == "Fan" && source.transform.position.x > 6f)
                {
                    float pitch = F101Report switch
                    {
                        FormStatus.Affirmed => 1.10f,
                        FormStatus.Unverified => 0.69f,
                        FormStatus.Refuted => 0.85f,
                        _ => 1f
                    };
                    source.pitch = Mathf.MoveTowards(source.pitch, pitch, dt * 0.6f);
                }
                float rainFactor = !loop.report && loop.exteriorOccluded ? returnWalkRainScale : 1f;
                source.volume = loop.level * envelope * ambienceScale * duckGain * signatureGain * tensionGain * rainFactor * distance * (1f - 0.36f * loop.obstruction);
            }

            if (penWriteLoop != null)
            {
                float targetPen = penWriteActive && ReportLayer ? 1f : 0f;
                penWriteGain = Mathf.MoveTowards(penWriteGain, targetPen, dt * (targetPen < penWriteGain ? 5.5f : 1.8f));
                penWriteLoop.spatialBlend = mono ? 0f : 1f;
                float penDist = mono ? DistanceGain(penWriteLoop, listener) : 1f;
                penWriteLoop.volume = 0.42f * penWriteGain * sfxScale * duckGain * penDist;
                if (penWriteGain <= 0.001f && penWriteLoop.isPlaying) penWriteLoop.Stop();
            }

            double now = AudioSettings.dspTime;
            foreach (var cue in cueVoices)
            {
                if (cue.endsAt <= now) continue;
                if (checkOcclusion && cue.checkWalls)
                    cue.obstruction = IsObstructed(cue.source.transform.position) ? 1f : 0f;
                float cutoff = cue.obstruction > 0f ? 2300f : 22000f;
                cue.filter.cutoffFrequency = Mathf.Lerp(cue.filter.cutoffFrequency, cutoff, 1f - Mathf.Exp(-dt * 16f));
                UpdateCueMix(cue);
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
                {
                    loop.source.spatialBlend = mono ? 0f : 1f;
                    loop.source.reverbZoneMix = mono ? 0f : 0.40f;
                }
            foreach (var cue in cueVoices)
                if (cue.endsAt > AudioSettings.dspTime)
                    UpdateCueMix(cue);
        }

        private void UpdateCueMix(CueVoice cue)
        {
            float distance = mono ? DistanceGain(cue.source, listener) : 1f;
            cue.source.spatialBlend = mono ? 0f : 1f;
            cue.source.reverbZoneMix = mono ? 0f : cue.reverbSend;
            float wallGain = Mathf.Lerp(0.64f, 1f, Mathf.InverseLerp(2300f, 22000f, cue.filter.cutoffFrequency));
            cue.source.volume = Mathf.Clamp01(cue.level * sfxScale * distance * wallGain);
        }

        private CueVoice AcquireCue(int importance, double now)
        {
            CueVoice replace = null;
            foreach (var voice in cueVoices)
            {
                if (voice.endsAt <= now) return voice;
                if (voice.importance <= importance && (replace == null || voice.importance < replace.importance ||
                    (voice.importance == replace.importance && voice.startedAt < replace.startedAt)))
                    replace = voice;
            }

            if (cueVoices.Count == CueVoiceCount) return replace;
            var emitter = new GameObject("World cue voice");
            emitter.transform.SetParent(propCue.transform.parent, false);
            var source = emitter.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.dopplerLevel = 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.outputAudioMixerGroup = propCue.outputAudioMixerGroup;
            var filter = emitter.AddComponent<AudioLowPassFilter>();
            var created = new CueVoice { source = source, filter = filter };
            cueVoices.Add(created);
            return created;
        }

        public void From(string cue, Vector3 at, float gainScale = 1f)
        {
            var sound = cue switch
            {
                "paper" => paper,
                "signaturePen" => signaturePen != null ? signaturePen : signaturePen = Resources.Load<AudioClip>("CTCC/F101Pen"),
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
                "handsetCradle" => cradleClick != null ? cradleClick : cradleClick = Resources.Load<AudioClip>("CTCC/CH01_Plastic_Cradle"),
                "enamelPedal" => enamelPedal != null ? enamelPedal : enamelPedal = Resources.Load<AudioClip>("CTCC/CH01_Enamel_Pedal"),
                "ceramicContact" => ceramicContact != null ? ceramicContact : ceramicContact = Resources.Load<AudioClip>("CTCC/CH01_Ceramic_Contact"),
                "paStatic" => paStatic,
                "breakerCut" => breakerCut,
                "emergencyHum" => emergencyHum,
                "distantThunder" => distantThunder,
                "drawerSlide" => drawerSlide != null ? drawerSlide : cabinetRoll,
                "paperFlip" => paperFlip != null ? paperFlip : paper,
                "clockTick" => clockTick != null ? clockTick : doorLatch,
                "distantSteps" => distantSteps != null ? distantSteps : stepCorridor,
                "chairScrape" => chairScrape != null ? chairScrape : cabinetRoll,
                "partitionKnock" => partitionKnock != null ? partitionKnock : doorLatch,
                "signKnock" => signKnock != null ? signKnock : signKnock = Resources.Load<AudioClip>("CTCC/CH01_SignKnock"),
                "keyTurn" => keyTurn != null ? keyTurn : keyTurn = Resources.Load<AudioClip>("CTCC/CH01_KeyTurn"),
                "latchClick" => latchClick != null ? latchClick : latchClick = Resources.Load<AudioClip>("CTCC/CH01_LatchClick"),
                "gateSlam" => gateSlam != null ? gateSlam : gateSlam = Resources.Load<AudioClip>("CTCC/CH01_GateSlam"),
                "bagZip" => bagZip != null ? bagZip : bagZip = Resources.Load<AudioClip>("CTCC/CH01_BagZip"),
                "clipSnap" => clipSnap != null ? clipSnap : clipSnap = Resources.Load<AudioClip>("CTCC/CH01_ClipSnap"),
                _ => null
            };
            if (sound == null || propCue == null) return;
            bool directionCue = cue is "keyJingle" or "slippers" or "trolleyRoll" or "distantSteps" or "chairScrape" or "partitionKnock" or "signKnock" or "gateSlam";
            bool transition = cue is "switch" or "switchToB" or "switchToR";
            double now = AudioSettings.dspTime;
            var voice = AcquireCue(directionCue ? 2 : transition ? 1 : 0, now);
            if (voice == null) return;
            voice.source.Stop();
            voice.source.transform.position = at;
            voice.source.minDistance = propCue.minDistance;
            voice.source.maxDistance = cue switch
            {
                "distantThunder" => 36f,
                "gateSlam" => 26f,
                "trolleyRoll" or "keyJingle" or "distantSteps" => 23f,
                "chairScrape" or "partitionKnock" or "signKnock" => 15f,
                "slippers" or "clockTick" => 11f,
                _ => propCue.maxDistance
            };
            voice.source.priority = directionCue ? 110 : 135;
            voice.reverbSend = transition || cue is "paperFlip" or "bagZip" or "clipSnap" ? 0f : 0.62f;
            voice.importance = directionCue ? 2 : transition ? 1 : 0;
            voice.checkWalls = directionCue || cue is "distantThunder" or "paStatic" or "clockTick";
            voice.obstruction = voice.checkWalls && IsObstructed(at) ? 1f : 0f;
            voice.filter.cutoffFrequency = voice.obstruction > 0f ? 2300f : 22000f;
            float baseVol = cue switch
            {
                "switch" or "switchToB" or "switchToR" => 0.44f,
                "cabinetRoll" => 0.66f,
                "doorCreak" => 0.56f,
                "drawerSlide" => 0.56f,
                "paperFlip" => 0.48f,
                "clockTick" => 0.60f,
                "distantSteps" => 0.50f,
                "chairScrape" => 0.54f,
                "partitionKnock" => 0.66f,
                "signKnock" => 0.56f,
                "keyTurn" => 0.64f,
                "latchClick" => 0.52f,
                "gateSlam" => 0.80f,
                "bagZip" => 0.46f,
                "clipSnap" => 0.56f,
                "curtainSlide" => 0.52f,
                "keyJingle" => 0.72f,
                "trolleyRoll" => 0.62f,
                "breakerCut" => 0.74f,
                "distantThunder" => 0.48f,
                "phoneBuzz" => 0.55f,
                _ => 0.58f
            };
            voice.level = baseVol * gainScale;
            voice.startedAt = now;
            voice.endsAt = now + sound.length + 0.05;
            voice.source.clip = sound;
            UpdateCueMix(voice);
            voice.source.Play();
        }

        internal void ListenToHandset()
        {
            if (handset == null)
            {
                handset = gameObject.AddComponent<AudioSource>();
                handset.playOnAwake = false;
                handset.spatialBlend = 0;
                handset.priority = 98;
                handset.clip = Resources.Load<AudioClip>("CTCC/CH01_Handset_Line");
            }
            handset.volume = .62f * sfxScale;
            handset.PlayDelayed(.15f);
        }

        internal void PutHandsetDown() => handset?.Stop();

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
            source.reverbZoneMix = mono ? 0f : 0.35f;
            source.PlayOneShot(clip, (crouched ? 0.16f : running ? 0.50f : 0.32f) * sfxScale);
        }
    }
}
