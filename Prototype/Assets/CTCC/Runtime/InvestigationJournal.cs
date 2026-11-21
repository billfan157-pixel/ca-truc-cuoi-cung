using System;
using System.Collections.Generic;

namespace CTCC
{
    public enum JournalNoteKind { Annotation, Hypothesis, Contradiction, Question }

    [Serializable]
    public struct TimelineConnection
    {
        public string left;
        public string right;
        public string context;

        internal bool HasValue => !string.IsNullOrEmpty(left) || !string.IsNullOrEmpty(right) || !string.IsNullOrEmpty(context);
    }

    [Serializable]
    public struct JournalPaperMark
    {
        public bool placed;
        public bool back;
        public float x;
        public float y;

        internal bool ValidFor(JournalNoteKind kind, string source)
        {
            if (!placed) return true;
            var spec = ObservationRegistry.Get(source);
            return kind == JournalNoteKind.Annotation && spec?.IsDocument == true && (!back || spec.HasBackSide) &&
                float.IsFinite(x) && float.IsFinite(y) && x >= 0 && x <= 1 && y >= 0 && y <= 1;
        }
    }

    [Serializable]
    public sealed class JournalNote
    {
        public JournalNoteKind kind;
        public string text;
        public string source;
        public string related;
        public JournalPaperMark paperMark;

        public const int MaxLength = 280;
        public const int MaxNotes = 32;

        internal static bool HasSource(Investigation state, string id) =>
            id == "E10a" ? state.Happened("E10a.SMSRead") :
            !string.IsNullOrEmpty(id) && state.Observed(id) &&
                state.FindObservation(id)?.stage >= ObservationStage.Examined;

        internal bool ValidFor(Investigation state)
        {
            if (!Enum.IsDefined(typeof(JournalNoteKind), kind) || string.IsNullOrWhiteSpace(text) ||
                text.Length > MaxLength || !string.IsNullOrEmpty(source) && !HasSource(state, source) ||
                !string.IsNullOrEmpty(related) && (source == related || !HasSource(state, source) || !HasSource(state, related)))
                return false;
            return paperMark.ValidFor(kind, source) &&
                (kind != JournalNoteKind.Annotation || HasSource(state, source));
        }
    }

    [Serializable]
    public sealed class ScenePropPose
    {
        public string id;
        public Reality layer;
        public float openness;

        internal static bool Known(string id) =>
            id is "DOOR-101" or "DOOR-STAFF" or "CURTAIN-101" or "DRAWER-BED3" or "DRAWER-DESK" or "DRAWER-NURSE" or "CHAIR-STAFF";

        internal static bool ValidList(List<ScenePropPose> poses)
        {
            var seen = new HashSet<(string, Reality)>();
            return poses != null && poses.TrueForAll(p => p != null && Known(p.id) &&
                p.layer is (Reality.R or Reality.B) && float.IsFinite(p.openness) &&
                p.openness >= 0f && p.openness <= 1f && seen.Add((p.id, p.layer)));
        }
    }
}
