using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CTCC
{
    [Serializable]
    public sealed class VoiceClipEntry
    {
        public string id;
        public AudioClip clip;
    }

    [Serializable]
    public struct TranscriptEntry
    {
        [SerializeField] private string lineId;
        [SerializeField] private string speaker;
        [SerializeField] private string text;
        public string LineId => lineId;
        public string Speaker => speaker;
        public string Text => text;

        public TranscriptEntry(string lineId, string speaker, string text)
        {
            this.lineId = lineId;
            this.speaker = speaker;
            this.text = text;
        }
    }

    /// <summary>
    /// Plays canonical Vietnamese Temp VO clips (TASK009-B), original 5-note Lan motif (MUS-LAN),
    /// ducks ambient audio by -5 dB during speech, drives CharacterActor lip-sync, logs every spoken/skipped line
    /// to the journal, subject to the same reveal policy as dialogue text.
    /// </summary>
    public sealed class VoiceDirector : MonoBehaviour
    {
        private readonly Dictionary<string, AudioClip> clipMap = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<TranscriptEntry> transcript = new();
        private readonly HashSet<string> recordedIds = new(StringComparer.OrdinalIgnoreCase);

        private AudioSource fallbackVoice;
        private AudioSource musicSource;
        private AudioSource activeVoice;
        private CharacterActor activeSpeaker;
        private EnvironmentalAudio ambience;
        private HospitalUI ui;
        private Coroutine activeRoutine;
        private Action<bool> speechEnded;
        private Func<string, string, bool> mayDeliver;
        public event Action<TranscriptEntry> Recorded;

        private float voiceVolume = 0.92f;
        private float musicVolume = 0.65f;
        private bool monoAudio;
        private float musicTarget;

        public IReadOnlyList<TranscriptEntry> Transcript => transcript;
        public bool IsSpeaking => activeRoutine != null;

        public void Initialize(VoiceClipEntry[] voiceEntries, AudioClip lanMotif, EnvironmentalAudio envAudio,
            HospitalUI hud, Func<string, string, bool> mayDeliver)
        {
            ambience = envAudio;
            ui = hud;
            this.mayDeliver = mayDeliver;
            clipMap.Clear();
            if (voiceEntries != null)
            {
                foreach (var entry in voiceEntries)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.id) || entry.clip == null) continue;
                    clipMap[entry.id] = entry.clip;
                }
            }

            fallbackVoice = gameObject.AddComponent<AudioSource>();
            fallbackVoice.playOnAwake = false;
            fallbackVoice.spatialBlend = 0f;

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.clip = lanMotif;
            musicSource.volume = 0f;
        }

        public void Apply(VisitSettings settings)
        {
            voiceVolume = Mathf.Clamp01(settings.voiceVolume);
            musicVolume = Mathf.Clamp01(settings.musicVolume >= 0f ? settings.musicVolume : 0.65f);
            monoAudio = settings.monoAudio;
            if (activeVoice != null)
                activeVoice.volume = voiceVolume;
        }

        public bool HasClip(string lineId, string beat = null) => mayDeliver(lineId, beat) && clipMap.ContainsKey(lineId);

        public void RecordTranscriptOnly(string lineId, string speaker, string text, string beat = null)
        {
            if (!mayDeliver(lineId, beat) || string.IsNullOrWhiteSpace(text)) return;
            if (!recordedIds.Add(lineId)) return;
            var entry = new TranscriptEntry(lineId, speaker, text);
            transcript.Add(entry);
            Recorded?.Invoke(entry);
        }

        public void RestoreTranscript(IEnumerable<TranscriptEntry> entries, Func<string, bool> mayRead)
        {
            transcript.Clear();
            recordedIds.Clear();
            foreach (var entry in entries)
                if (mayRead(entry.LineId) && recordedIds.Add(entry.LineId))
                    transcript.Add(entry);
        }

        public void SetLanMotif(bool active, float level = 0.24f)
        {
            musicTarget = active ? Mathf.Clamp01(level) : 0f;
            if (active && musicSource != null && musicSource.clip != null && !musicSource.isPlaying)
                musicSource.Play();
        }

        public void StopCurrent()
        {
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }
            FinishSpeech(false);
        }

        public void StopFor(CharacterActor actor)
        {
            if (actor != null && activeSpeaker == actor) StopCurrent();
        }

        private void FinishSpeech(bool completed)
        {
            if (activeVoice != null)
                activeVoice.Stop();
            if (activeSpeaker != null)
            {
                activeSpeaker.EndSpeech();
                activeSpeaker = null;
            }
            activeVoice = null;
            ambience?.SetVoiceDucking(false);
            activeRoutine = null;
            var callback = speechEnded;
            speechEnded = null;
            callback?.Invoke(completed);
        }

        public void Speak(string lineId, string speakerLabel, string text, AudioSource spatialAnchor = null,
            CharacterActor speakerActor = null, string directionalPrefix = null, Action<bool> onComplete = null,
            bool essential = true, string beat = null)
        {
            if (!mayDeliver(lineId, beat) || string.IsNullOrWhiteSpace(text))
            {
                onComplete?.Invoke(false);
                return;
            }

            StopCurrent();
            RecordTranscriptOnly(lineId, speakerLabel, text, beat);
            activeSpeaker = speakerActor;

            string caption = string.IsNullOrEmpty(directionalPrefix)
                ? $"{speakerLabel} · {text}"
                : $"{directionalPrefix} {speakerLabel} · {text}";

            AudioClip clip = clipMap.TryGetValue(lineId, out var found) ? found : null;
            float duration = clip != null ? Mathf.Clamp(clip.length + 0.45f, 2.6f, 12.0f) : Mathf.Clamp(2.2f + text.Length * 0.045f, 3.2f, 7.5f);
            ui?.Caption(caption, duration, essential);

            if (clip != null)
            {
                activeVoice = spatialAnchor != null ? spatialAnchor : fallbackVoice;
                activeVoice.clip = clip;
                activeVoice.spatialBlend = (spatialAnchor != null && !monoAudio) ? 0.78f : 0f;
                activeVoice.volume = voiceVolume;
                activeVoice.Play();
                if (speakerActor != null)
                {
                    speakerActor.BeginSpeech(lineId, activeVoice);
                }
                ambience?.SetVoiceDucking(true);
            }

            speechEnded = onComplete;
            activeRoutine = StartCoroutine(TrackSpeech(duration));
        }

        private IEnumerator TrackSpeech(float duration)
        {
            float until = Time.unscaledTime + duration;
            while (Time.unscaledTime < until)
            {
                if (activeVoice != null && !activeVoice.isPlaying && Time.unscaledTime > until - 0.5f)
                    break;
                yield return null;
            }
            FinishSpeech(true);
        }

        private void Update()
        {
            if (musicSource == null) return;
            float goal = musicTarget * musicVolume;
            musicSource.volume = Mathf.MoveTowards(musicSource.volume, goal, Time.unscaledDeltaTime * 0.25f);
            if (goal <= 0.001f && musicSource.volume <= 0.001f && musicSource.isPlaying)
                musicSource.Stop();
        }
    }
}
