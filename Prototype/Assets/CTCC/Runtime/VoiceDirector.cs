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

    public readonly struct TranscriptEntry
    {
        public readonly string LineId;
        public readonly string Speaker;
        public readonly string Text;

        public TranscriptEntry(string lineId, string speaker, string text)
        {
            LineId = lineId;
            Speaker = speaker;
            Text = text;
        }
    }

    /// <summary>
    /// Plays canonical Vietnamese Temp VO clips (TASK009-B), original 5-note Lan motif (MUS-LAN),
    /// ducks ambient audio by -5 dB during speech, drives CharacterActor lip-sync, logs every spoken/skipped line
    /// to the persistent Journal Transcript, and hard-blocks LOCK-V22-02 forbidden lines (CH01-D16b, CH04-D06).
    /// </summary>
    public sealed class VoiceDirector : MonoBehaviour
    {
        private static readonly HashSet<string> ForbiddenLines = new(StringComparer.OrdinalIgnoreCase)
        {
            "CH01-D16b", "CH04-D06"
        };

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

        private float voiceVolume = 0.92f;
        private float musicVolume = 0.65f;
        private bool monoAudio;
        private float musicTarget;

        public IReadOnlyList<TranscriptEntry> Transcript => transcript;
        public bool IsSpeaking => activeVoice != null && activeVoice.isPlaying;

        public void Initialize(VoiceClipEntry[] voiceEntries, AudioClip lanMotif, EnvironmentalAudio envAudio, HospitalUI hud)
        {
            ambience = envAudio;
            ui = hud;
            clipMap.Clear();
            if (voiceEntries != null)
            {
                foreach (var entry in voiceEntries)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.id) || entry.clip == null) continue;
                    if (ForbiddenLines.Contains(entry.id)) continue;
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
            voiceVolume = Mathf.Clamp01(settings.voiceVolume > 0.001f ? settings.voiceVolume : 0.92f);
            musicVolume = Mathf.Clamp01(settings.musicVolume >= 0f ? settings.musicVolume : 0.65f);
            monoAudio = settings.monoAudio;
            if (activeVoice != null)
                activeVoice.volume = voiceVolume;
        }

        public bool HasClip(string lineId) => !string.IsNullOrEmpty(lineId) && !ForbiddenLines.Contains(lineId) && clipMap.ContainsKey(lineId);

        public void RecordTranscriptOnly(string lineId, string speaker, string text)
        {
            if (string.IsNullOrEmpty(lineId) || ForbiddenLines.Contains(lineId)) return;
            if (string.IsNullOrWhiteSpace(text)) return;
            if (!recordedIds.Add(lineId)) return;
            transcript.Add(new TranscriptEntry(lineId, speaker, text));
        }

        public void ClearTranscript()
        {
            transcript.Clear();
            recordedIds.Clear();
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
            if (activeVoice != null && activeVoice.isPlaying)
                activeVoice.Stop();
            if (activeSpeaker != null)
            {
                activeSpeaker.voiceSource = null;
                activeSpeaker = null;
            }
            activeVoice = null;
            ambience?.SetVoiceDucking(false);
        }

        public void Speak(string lineId, string speakerLabel, string text, AudioSource spatialAnchor = null,
            CharacterActor speakerActor = null, string directionalPrefix = null, Action onComplete = null, bool essential = true)
        {
            if (string.IsNullOrEmpty(lineId) || ForbiddenLines.Contains(lineId))
            {
                Debug.LogWarning($"CTCC LOCK-V22-02 blocked forbidden line '{lineId}'.");
                return;
            }

            StopCurrent();
            RecordTranscriptOnly(lineId, speakerLabel, text);

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
                    activeSpeaker = speakerActor;
                    speakerActor.voiceSource = activeVoice;
                }
                ambience?.SetVoiceDucking(true);
            }

            activeRoutine = StartCoroutine(TrackSpeech(duration, onComplete));
        }

        private IEnumerator TrackSpeech(float duration, Action onComplete)
        {
            float until = Time.unscaledTime + duration;
            while (Time.unscaledTime < until)
            {
                if (activeVoice != null && !activeVoice.isPlaying && Time.unscaledTime > until - 0.5f)
                    break;
                yield return null;
            }
            if (activeSpeaker != null)
            {
                activeSpeaker.voiceSource = null;
                activeSpeaker = null;
            }
            activeVoice = null;
            ambience?.SetVoiceDucking(false);
            activeRoutine = null;
            onComplete?.Invoke();
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
