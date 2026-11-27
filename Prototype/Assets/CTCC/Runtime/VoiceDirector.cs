using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CTCC
{
    /// <summary>Core v3.3.3 / DEC-VO-01 dialogue presentation contract.</summary>
    public enum DialoguePresentationMode
    {
        VO_DIALOGUE,
        VO_REACTION_SFX,
        INNER_SUB_SILENT,
        CUT_TO_JOURNAL,
        UI_NON_VO,
    }

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
        private bool voiceInWorld;
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
        private bool paused;

        public IReadOnlyList<TranscriptEntry> Transcript => transcript;
        public bool IsSpeaking => activeRoutine != null;
        internal bool IsPaused => paused;
        internal int NonVoAudioPlayCount { get; private set; }
        /// <summary>
        /// Harness-only fault-injection hook for automated probe <c>v36-vo-leak</c> (4.1).
        /// Never enabled in gameplay code; toggled exclusively inside try/finally by Phase 1 QA.
        /// </summary>
        internal bool QaInjectVoLeakForTesting { get; set; }
        internal IReadOnlyCollection<string> LoadedClipIds => clipMap.Keys;
        internal bool HasLoadedClipRaw(string lineId) => !string.IsNullOrEmpty(lineId) && clipMap.ContainsKey(lineId);

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
            musicSource.loop = false;
            musicSource.spatialBlend = 0f;
            musicSource.reverbZoneMix = 0f;
            musicSource.clip = lanMotif;
            musicSource.volume = 0f;
        }

        public void Apply(VisitSettings settings)
        {
            voiceVolume = Mathf.Clamp01(settings.voiceVolume);
            musicVolume = Mathf.Clamp01(settings.musicVolume >= 0f ? settings.musicVolume : 0.65f);
            monoAudio = settings.monoAudio;
            ApplyVoiceMix();
        }

        public bool HasClip(string lineId, string beat = null) => mayDeliver(lineId, beat) && clipMap.ContainsKey(lineId);

        internal bool TrackMatchesAudio(string lineId, LipSyncTrack track) =>
            track != null && clipMap.TryGetValue(lineId, out var clip) &&
            Mathf.Abs(clip.length - track.duration) <= 1f / track.fps;

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
            {
                musicSource.volume = 0f;
                musicSource.Play();
            }
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

        public void SetPaused(bool pause)
        {
            if (paused == pause) return;
            paused = pause;
            if (activeVoice != null)
            {
                if (paused) activeVoice.Pause();
                else activeVoice.UnPause();
            }
            if (activeSpeaker != null) activeSpeaker.SetSpeechPaused(paused);
            if (musicSource != null)
            {
                if (paused) musicSource.Pause();
                else musicSource.UnPause();
            }
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
            voiceInWorld = false;
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
            SpeakInternal(lineId, speakerLabel, text, DialoguePresentationMode.VO_DIALOGUE,
                spatialAnchor, speakerActor, directionalPrefix, onComplete, essential, beat, true);
        }

        /// <summary>
        /// DEC-VO-01 router. VO_DIALOGUE and VO_REACTION_SFX play an AudioClip plus a subtitle;
        /// INNER_SUB_SILENT never touches the AudioBus and renders an italic silent thought with WCAG AA scrim;
        /// CUT_TO_JOURNAL stays out of the HUD and routes pocketNoteText (12/12 lines) into Pocket Note / Transcript.
        /// </summary>
        public void Present(string lineId, string speakerLabel, string text, DialoguePresentationMode mode,
            AudioSource spatialAnchor = null, CharacterActor speakerActor = null, string directionalPrefix = null,
            Action<bool> onComplete = null, bool essential = true, string beat = null,
            bool showSpeakerPrefix = true, bool italic = false, string pocketNoteText = null)
        {
            if (mode == DialoguePresentationMode.CUT_TO_JOURNAL)
            {
                if (!mayDeliver(lineId, beat))
                {
                    onComplete?.Invoke(false);
                    return;
                }
                // Core v3.6.2 DEC-VO-01: no HUD, no VO — route canonical pocketNoteText into Pocket Note / Transcript.
                string note = !string.IsNullOrWhiteSpace(pocketNoteText)
                    ? pocketNoteText
                    : (!string.IsNullOrWhiteSpace(text) && !text.StartsWith("[ẨN", StringComparison.Ordinal) ? text : "");
                if (!string.IsNullOrWhiteSpace(note))
                    RecordTranscriptOnly(lineId, string.IsNullOrEmpty(speakerLabel) ? "Ghi nhớ" : speakerLabel, note, beat);
                onComplete?.Invoke(true);
                return;
            }

            if (!mayDeliver(lineId, beat) || string.IsNullOrWhiteSpace(text))
            {
                onComplete?.Invoke(false);
                return;
            }

            if (mode == DialoguePresentationMode.UI_NON_VO)
            {
                RecordTranscriptOnly(lineId, speakerLabel, text, beat);
                onComplete?.Invoke(true);
                return;
            }

            if (mode == DialoguePresentationMode.INNER_SUB_SILENT && !QaInjectVoLeakForTesting)
            {
                // Silent thought is text-only and never blocks player control or touches AudioBus.
                RecordTranscriptOnly(lineId, string.IsNullOrEmpty(speakerLabel) ? "Minh (suy nghĩ)" : speakerLabel, text, beat);
                float hold = Mathf.Clamp(2.8f + text.Length * 0.025f, 2.8f, 4.5f);
                ui?.SilentThought(text, hold);
                onComplete?.Invoke(true);
                return;
            }

            bool reaction = mode == DialoguePresentationMode.VO_REACTION_SFX;
            SpeakInternal(lineId, speakerLabel, text, mode, spatialAnchor, speakerActor, directionalPrefix,
                onComplete, essential, beat, reaction ? false : showSpeakerPrefix, reaction || italic);
        }

        private void SpeakInternal(string lineId, string speakerLabel, string text, DialoguePresentationMode mode,
            AudioSource spatialAnchor, CharacterActor speakerActor, string directionalPrefix, Action<bool> onComplete,
            bool essential, string beat, bool showSpeakerPrefix, bool italic = false)
        {
            if (!mayDeliver(lineId, beat) || string.IsNullOrWhiteSpace(text))
            {
                onComplete?.Invoke(false);
                return;
            }

            StopCurrent();
            RecordTranscriptOnly(lineId, speakerLabel, text, beat);
            activeSpeaker = speakerActor;

            string caption = showSpeakerPrefix && !string.IsNullOrEmpty(speakerLabel)
                ? (string.IsNullOrEmpty(directionalPrefix) ? $"{speakerLabel} · {text}" : $"{directionalPrefix} {speakerLabel} · {text}")
                : (string.IsNullOrEmpty(directionalPrefix) ? text : $"{directionalPrefix} {text}");

            AudioClip clip = clipMap.TryGetValue(lineId, out var found) ? found : null;
            // DEC-VO-01: only VO_DIALOGUE / VO_REACTION_SFX may reach the AudioBus.
            // Defensive guard: if any non-VO mode ever reaches SpeakInternal (including v36-vo-leak probe),
            // increment NonVoAudioPlayCount so automated VO-SILENT-Q01 fails immediately, while stripping AudioClip.
            if (mode == DialoguePresentationMode.INNER_SUB_SILENT ||
                mode == DialoguePresentationMode.CUT_TO_JOURNAL ||
                mode == DialoguePresentationMode.UI_NON_VO)
            {
                NonVoAudioPlayCount++;
                clip = null;
            }
            float duration = clip != null ? Mathf.Max(clip.length + 0.45f, 2.6f) : Mathf.Clamp(2.2f + text.Length * 0.045f, 3.2f, 7.5f);
            ui?.CaptionStyled(caption, duration, essential, false,
                mode == DialoguePresentationMode.VO_REACTION_SFX ? SubtitleStyle.Reaction : SubtitleStyle.Standard);

            if (clip != null)
            {
                activeVoice = spatialAnchor != null ? spatialAnchor : fallbackVoice;
                voiceInWorld = spatialAnchor != null;
                activeVoice.clip = clip;
                ApplyVoiceMix();
                activeVoice.Play();
                if (speakerActor != null)
                {
                    speakerActor.BeginSpeech(lineId, activeVoice);
                    speakerActor.SetSpeechPaused(paused);
                }
                if (paused) activeVoice.Pause();
            }

            speechEnded = onComplete;
            activeRoutine = StartCoroutine(TrackSpeech(duration));
        }

        private IEnumerator TrackSpeech(float duration)
        {
            float remaining = duration;
            while (remaining > 0f)
            {
                yield return null;
                if (paused) continue;
                remaining -= Time.unscaledDeltaTime;
                if (activeVoice != null && !activeVoice.isPlaying && remaining < 0.5f)
                    break;
            }
            FinishSpeech(true);
        }

        private bool ApplyVoiceMix()
        {
            if (activeVoice == null) return false;
            activeVoice.spatialBlend = voiceInWorld && !monoAudio ? 1f : 0f;
            activeVoice.reverbZoneMix = voiceInWorld && !monoAudio ? 0.32f : 0f;
            float distance = voiceInWorld ? EnvironmentalAudio.DistanceGain(activeVoice, ambience != null ? ambience.Listener : null) : 1f;
            activeVoice.volume = voiceVolume * (monoAudio ? distance : 1f);
            bool audible = !paused && activeVoice.isPlaying && voiceVolume * distance > 0.025f;
            ambience?.SetVoiceDucking(audible);
            return audible;
        }

        private void Update()
        {
            bool audible = ApplyVoiceMix();
            if (musicSource == null || paused) return;
            float goal = musicTarget * musicVolume * (audible ? 0.71f : 1f);
            musicSource.volume = Mathf.MoveTowards(musicSource.volume, goal, Time.unscaledDeltaTime * 0.25f);
            if (goal <= 0.001f && musicSource.volume <= 0.001f && musicSource.isPlaying)
                musicSource.Stop();
        }
    }
}
