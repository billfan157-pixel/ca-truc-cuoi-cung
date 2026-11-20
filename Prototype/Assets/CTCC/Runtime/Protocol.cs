using System;
using System.Collections.Generic;
using UnityEngine;

namespace CTCC
{
    public enum Reality { R = 0, B = 1, M = 2 }
    public enum FormStatus { Pending = 0, Affirmed = 2, Unverified = 3, Refuted = 4 }

    [Serializable]
    public sealed class FormEntry
    {
        public string formId;
        public FormStatus firstDecision;
        public FormStatus current;
        public int revision;
    }

    [Serializable]
    public sealed class PatientEntry
    {
        public string code;
        public bool existenceConfirmed;
        public bool identityVerified;
        public string displayName;
    }

    [Serializable]
    public sealed class PuzzleAttempt
    {
        public string puzzleId;
        public int failedAttempts;
        public bool solved;
    }

    [Serializable]
    public sealed class VisitSettings
    {
        public bool reduceMotion;
        public bool captions = true;
        public bool monoAudio;
        public bool alternateBindings;
        public float fov = 62;
        public float textScale = 1;
        public float volume = .72f;
        public float visibility = 0;
        public float mouseSensitivity = 1f;
        public float ambienceVolume = .85f;
        public float sfxVolume = .90f;
        public float voiceVolume = .92f;
        public float musicVolume = .65f;
        public bool hintsEnabled = true;
        public bool storyMode;
    }

    [Serializable]
    public sealed class F101Record
    {
        public FormStatus firstDecision;
        public FormStatus current;
        public int revision;
    }

    [Serializable]
    public sealed class DialogueCondition
    {
        public string id;
        public string chapter;
        public string beat;
        public string[] requiredEvents;

        public bool Allows(Investigation state, string currentBeat) =>
            state.chapter == chapter && currentBeat == beat && HasPrerequisites(state);

        public bool HasPrerequisites(Investigation state) =>
            requiredEvents != null && Array.TrueForAll(requiredEvents, state.Happened);
    }

    [Serializable]
    public sealed class Investigation
    {
        public const int Schema = 2;
        public const int MinSupportedSchema = 1;
        public int schemaVersion = Schema;
        public long sequence;
        public string checkpoint = "CH01-CP00";
        public string chapter = "CH01";
        public Reality layer;
        public F101Record f101 = new();
        public List<FormEntry> forms = new();
        public List<PatientEntry> patients = new();
        public List<PuzzleAttempt> puzzles = new();
        public VisitSettings settings = new();
        public bool publicFireKnown = true;
        public bool publicLanDeathKnown = true;
        public int publicDeathCount = 13;
        public int torchSeconds = 360;
        public List<string> events = new();
        public List<string> evidence = new();
        public List<string> observed = new();
        public List<TranscriptEntry> transcript = new();

        public Investigation Copy() => JsonUtility.FromJson<Investigation>(JsonUtility.ToJson(this));
        public bool Happened(string id) => events.Contains(id);
        public void Remember(string id) { if (!events.Contains(id)) events.Add(id); }
        public bool Observed(string id) => observed.Contains(id);
        public bool Observe(string id)
        {
            if (observed.Contains(id)) return false;
            observed.Add(id);
            return true;
        }
        public bool Has(string id) => evidence.Contains(id);

        public PatientEntry FindOrCreatePatient(string code)
        {
            patients ??= new();
            for (int i = 0; i < patients.Count; i++)
                if (string.Equals(patients[i].code, code, StringComparison.OrdinalIgnoreCase))
                    return patients[i];
            var entry = new PatientEntry { code = code.ToUpperInvariant(), displayName = "" };
            patients.Add(entry);
            return entry;
        }

        public bool PatientConfirmed(string code)
        {
            if (Happened(code + ".confirmed") || Happened(code + ".verified")) return true;
            if (patients == null) return false;
            for (int i = 0; i < patients.Count; i++)
                if (string.Equals(patients[i].code, code, StringComparison.OrdinalIgnoreCase) && patients[i].existenceConfirmed)
                    return true;
            return false;
        }

        public bool PatientIdentityVerified(string code)
        {
            // Canon CH03-Q03: C12 must permanently remain anonymous (no verified identity).
            if (string.Equals(code, "C12", StringComparison.OrdinalIgnoreCase)) return false;
            if (Happened(code + ".verified")) return true;
            if (patients == null) return false;
            for (int i = 0; i < patients.Count; i++)
                if (string.Equals(patients[i].code, code, StringComparison.OrdinalIgnoreCase) && patients[i].identityVerified)
                    return true;
            return false;
        }

        public void ConfirmPatientExistence(string code)
        {
            var p = FindOrCreatePatient(code);
            p.existenceConfirmed = true;
            if (string.Equals(code, "C12", StringComparison.OrdinalIgnoreCase))
            {
                p.identityVerified = false;
                p.displayName = "";
            }
            Remember(p.code + ".confirmed");
        }

        public bool VerifyPatientIdentity(string code, string displayName)
        {
            // Canon CH03-Q03: C12 is permanently anonymous — only existence is recorded, never a fabricated name.
            if (string.Equals(code, "C12", StringComparison.OrdinalIgnoreCase))
                return false;
            var p = FindOrCreatePatient(code);
            p.existenceConfirmed = true;
            p.identityVerified = true;
            p.displayName = displayName ?? "";
            Remember(p.code + ".confirmed");
            Remember(p.code + ".verified");
            return true;
        }

        public static Investigation New()
        {
            var state = new Investigation();
            state.evidence.Add("E10a");
            state.Remember("CH01-T01");
            return state;
        }
    }

    public static class FormService
    {
        public static bool RefutationAvailable(Investigation state) => state.Observed("OBS101A") && state.Observed("OBS101B");

        public static bool RefutationAvailable(Investigation state, string formId) => formId switch
        {
            "F101" => RefutationAvailable(state),
            "F303" => state.Has("E03") && state.PatientConfirmed("C02"),
            _ => false
        };

        /// <summary>
        /// Generalized M3 reality distortion index: D_zone = min(4, unique false affirmed forms).
        /// </summary>
        public static int Distortion(Investigation state)
        {
            int count = state.f101.current == FormStatus.Affirmed ? 1 : 0;
            if (state.forms != null)
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "F101" };
                for (int i = 0; i < state.forms.Count; i++)
                {
                    var f = state.forms[i];
                    if (f != null && !string.IsNullOrEmpty(f.formId) && seen.Add(f.formId) && f.current == FormStatus.Affirmed)
                        count++;
                }
            }
            return Mathf.Min(4, count);
        }

        public static bool Apply(Investigation candidate, FormStatus decision)
        {
            if (!Enum.IsDefined(typeof(FormStatus), decision) || decision == FormStatus.Pending ||
                decision == FormStatus.Refuted && !RefutationAvailable(candidate))
                return false;
            var record = candidate.f101;
            if (record.current == decision)
                return true;
            if (record.revision == 0)
                record.firstDecision = decision;
            record.current = decision;
            record.revision++;
            candidate.Remember("FS02");
            return true;
        }

        public static bool ApplyForm(Investigation candidate, string formId, FormStatus decision)
        {
            if (string.IsNullOrEmpty(formId) || string.Equals(formId, "F101", StringComparison.OrdinalIgnoreCase))
                return Apply(candidate, decision);
            if (!Enum.IsDefined(typeof(FormStatus), decision) || decision == FormStatus.Pending ||
                decision == FormStatus.Refuted && !RefutationAvailable(candidate, formId))
                return false;
            candidate.forms ??= new();
            FormEntry record = null;
            for (int i = 0; i < candidate.forms.Count; i++)
                if (string.Equals(candidate.forms[i].formId, formId, StringComparison.OrdinalIgnoreCase))
                {
                    record = candidate.forms[i];
                    break;
                }
            if (record == null)
            {
                record = new FormEntry { formId = formId.ToUpperInvariant() };
                candidate.forms.Add(record);
            }
            if (record.current == decision) return true;
            if (record.revision == 0) record.firstDecision = decision;
            record.current = decision;
            record.revision++;
            return true;
        }
    }

    public static class EvidenceRegistry
    {
        public const string Drawing = "E11.drawing";
        public const string Wristband = "E11.wristband";
        public const string Transfer = "E11.transferNote";
        public static readonly string[] DrawingFragments = { "E11.drawing.1", "E11.drawing.2", "E11.drawing.3" };

        // CH03 Evidence IDs (Core 02 / Core 04 / Core 07)
        public const string PatientRegister = "E03";
        public const string C02CareNote = "E03.C02Note";
        public const string C02YarnBag = "E03.C02YarnBag";
        public const string C02DaughterLetter = "E03.C02Letter";
        public const string C12UnnamedBox = "E03.C12KeyBox";
        public const string BayLockReceipt = "FS10.LockReceipt";
        public const string NonExportableTraceE08 = "E08";

        private sealed class Spec
        {
            public readonly Reality Layer;
            public readonly string Chapter;
            public readonly string Location;
            public readonly bool PhysicalExportable;
            public Spec(Reality layer, string chapter, string location, bool exportable = true)
            {
                Layer = layer;
                Chapter = chapter;
                Location = location;
                PhysicalExportable = exportable;
            }
        }

        private static readonly Dictionary<string, Spec> Specs = new(StringComparer.Ordinal)
        {
            ["E10a"] = new(Reality.R, "CH01", "LOC01", true),
            ["E12"] = new(Reality.R, "CH02", "LOC03", true),
            ["E11.drawing.1"] = new(Reality.R, "CH02", "LOC03", true),
            ["E11.drawing.2"] = new(Reality.R, "CH02", "LOC03", true),
            ["E11.drawing.3"] = new(Reality.R, "CH02", "LOC03", true),
            [Drawing] = new(Reality.R, "CH02", "LOC03", true),
            [Wristband] = new(Reality.R, "CH02", "LOC03", true),
            [Transfer] = new(Reality.R, "CH02", "LOC03", true),
            [PatientRegister] = new(Reality.R, "CH03", "LOC07", true),
            [C02CareNote] = new(Reality.R, "CH03", "LOC07", true),
            [C02YarnBag] = new(Reality.R, "CH03", "LOC07", true),
            [C02DaughterLetter] = new(Reality.R, "CH03", "LOC07", true),
            [C12UnnamedBox] = new(Reality.R, "CH03", "LOC07", true),
            [BayLockReceipt] = new(Reality.R, "CH03", "LOC05", true),
            [NonExportableTraceE08] = new(Reality.B, "CH04", "LOC08", false),
        };

        public static bool Grant(Investigation state, string id, Reality provenanceLayer, string provenanceLocation)
        {
            if (state == null || string.IsNullOrEmpty(id) || !Specs.TryGetValue(id, out var spec) || state.Has(id))
                return false;
            // Non-exportable B/M traces (such as E08) cannot be granted into durable physical R evidence inventory!
            if (!spec.PhysicalExportable || state.layer != spec.Layer || provenanceLayer != spec.Layer ||
                state.chapter != spec.Chapter || provenanceLocation != spec.Location)
                return false;
            if (id == Drawing && !HaveFragments(state))
                return false;
            state.evidence.Add(id);
            return true;
        }

        public static bool IsPhysicalExportable(string id) =>
            !string.IsNullOrEmpty(id) && Specs.TryGetValue(id, out var spec) && spec.PhysicalExportable;

        public static bool HaveFragments(Investigation state) =>
            state.Has(DrawingFragments[0]) && state.Has(DrawingFragments[1]) && state.Has(DrawingFragments[2]);
        public static bool BundleComplete(Investigation state) =>
            state.Has(Drawing) && state.Has(Wristband) && state.Has(Transfer);
        public static bool Registered(string id) => IsPhysicalExportable(id);
    }

    public static class PuzzleService
    {
        private static PuzzleAttempt FindOrCreate(Investigation state, string puzzleId)
        {
            state.puzzles ??= new();
            for (int i = 0; i < state.puzzles.Count; i++)
                if (string.Equals(state.puzzles[i].puzzleId, puzzleId, StringComparison.OrdinalIgnoreCase))
                    return state.puzzles[i];
            var entry = new PuzzleAttempt { puzzleId = puzzleId };
            state.puzzles.Add(entry);
            return entry;
        }

        public static bool RecordAttempt(Investigation state, string puzzleId, bool correct)
        {
            var entry = FindOrCreate(state, puzzleId);
            if (correct)
            {
                entry.solved = true;
                state.Remember(puzzleId + ".solved");
                return true;
            }
            entry.failedAttempts++;
            if (entry.failedAttempts >= 2)
                state.Remember(puzzleId + ".hint");
            return false;
        }

        public static int FailedAttempts(Investigation state, string puzzleId)
        {
            if (state?.puzzles == null) return 0;
            for (int i = 0; i < state.puzzles.Count; i++)
                if (string.Equals(state.puzzles[i].puzzleId, puzzleId, StringComparison.OrdinalIgnoreCase))
                    return state.puzzles[i].failedAttempts;
            return 0;
        }

        public static bool ShouldShowFieldHint(Investigation state, string puzzleId) =>
            FailedAttempts(state, puzzleId) >= 2;
    }

    public static class GateService
    {
        public static bool G01(Investigation state) => state.f101.current != FormStatus.Pending;
        public static bool H01Tutorial(Investigation state) =>
            state.Has("E12") && EvidenceRegistry.BundleComplete(state) && state.PatientIdentityVerified("C01");
        public static bool H02(Investigation state) => state.Has("E01") && state.Has("E04");
        public static bool G02(Investigation state) => (state.chapter == "CH02" || state.chapter == "CH03") && H01Tutorial(state);
        // Core 07 Chapter gates: G03 opens when E03 is collected and C02 existence is confirmed (name is optional per CH03-Q02!)
        public static bool G03(Investigation state) =>
            state.chapter == "CH03" && state.Has(EvidenceRegistry.PatientRegister) && state.PatientConfirmed("C02");
    }

    public static class SafeCheckpoints
    {
        public static string Pad(string checkpoint)
        {
            return checkpoint switch
            {
                "CH01-CP00" => "AP00",
                "CH01-S1" => "AP01",
                "CH01-CP01" => "AP02",
                "CH01-CP02" => "AP04",
                "CH01-CP03" => "AP02",
                "CH01-S7" => "AP01",
                "CH01-CP04" => "AP02",
                "CH02-CP02" => "AP04",
                "CH02-CP03" => "AP04",
                "CH02-CP04" => "AP05",
                "CH03-CP01" => "AP06",
                "CH03-CP02" => "AN03",
                "CH03-CP03" => "AN03",
                "CH03-CP04" => "AP07",
                _ => null
            };
        }
    }
}
