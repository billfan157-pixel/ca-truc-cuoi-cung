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
        public bool disableCameraAssist;
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
        public const int Schema = 7;
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
        public int servicePower;
        public VisitSettings settings = new();
        public bool publicFireKnown = true;
        public bool publicLanDeathKnown = true;
        public int publicDeathCount = 13;
        public int torchSeconds = 360;
        public List<string> events = new();
        public List<string> evidence = new();
        public List<string> observed = new();
        public List<ObservationRecord> observationRecords = new();
        public List<FormRevisionEntry> f101History = new();
        public DeductionSubmission f101Argument = new();
        public F101Annotations f101Annotations = new();
        public List<JournalNote> journalNotes = new();
        public TimelineConnection exception0317Link;
        public List<ScenePropPose> sceneProps = new();
        public List<BackpackEntry> backpackItems = new();
        public List<string> quickSlots = new();
        public string equippedInstanceId = "";
        public int e10aLastTab;
        public List<string> dreadEventsSeen = new();
        public int dreadSeed = 20051118;
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

        internal ScenePropPose FindPropPose(string id, Reality atLayer) =>
            sceneProps.Find(p => p.id == id && p.layer == atLayer);

        public ObservationRecord FindObservation(string id)
        {
            if (observationRecords == null || string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < observationRecords.Count; i++)
                if (observationRecords[i] != null && string.Equals(observationRecords[i].id, id, StringComparison.Ordinal))
                    return observationRecords[i];
            return null;
        }

        public ObservationRecord RecordObservation(string id, Reality provenanceLayer, string location, ObservationStage stage)
        {
            observationRecords ??= new();
            Observe(id);
            var existing = FindObservation(id);
            if (existing != null)
            {
                if (stage > existing.stage) existing.stage = stage;
                return existing;
            }
            var rec = new ObservationRecord
            {
                id = id,
                layer = provenanceLayer,
                location = location ?? "",
                chapter = chapter ?? "CH01",
                stage = stage
            };
            observationRecords.Add(rec);
            return rec;
        }

        public bool MarkObservationDetail(string id, string detailId, bool flippedBack = false)
        {
            var rec = FindObservation(id);
            if (rec == null) return false;
            bool changed = false;
            if (flippedBack && !rec.flippedBack)
            {
                rec.flippedBack = true;
                changed = true;
            }
            if (!string.IsNullOrEmpty(detailId) && rec.MarkDetail(detailId))
                changed = true;
            if ((rec.flippedBack || (rec.inspectedDetails != null && rec.inspectedDetails.Count > 0)) &&
                rec.stage < ObservationStage.Interpreted)
            {
                rec.stage = ObservationStage.Interpreted;
                changed = true;
            }
            return changed;
        }

        public bool HasDreadEvent(string id) =>
            dreadEventsSeen != null && !string.IsNullOrEmpty(id) && dreadEventsSeen.Contains(id);

        public bool MarkDreadEvent(string id)
        {
            dreadEventsSeen ??= new();
            if (string.IsNullOrEmpty(id) || dreadEventsSeen.Contains(id)) return false;
            dreadEventsSeen.Add(id);
            return true;
        }

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
            InventoryService.EnsureDefaultTools(state);
            return state;
        }
    }

    public static class FormService
    {
        internal readonly struct Definition
        {
            public readonly string Zone;
            public readonly bool FalseClaim;
            public readonly Func<Investigation, bool> CanRefute;
            public Definition(string zone, bool falseClaim, Func<Investigation, bool> canRefute)
            {
                Zone = zone;
                FalseClaim = falseClaim;
                CanRefute = canRefute;
            }
        }

        private static readonly Dictionary<string, Definition> Registry = new(StringComparer.OrdinalIgnoreCase)
        {
            ["F101"] = new("LOC03", true, RefutationAvailable)
        };

        public static bool RefutationAvailable(Investigation state) =>
            HypothesisService.HasValidCoreObservations(state);

        public static bool RefutationAvailable(Investigation state, string formId) =>
            formId != null && Registry.TryGetValue(formId, out var spec) && spec.CanRefute(state);

        public static int Distortion(Investigation state, string zone = "LOC03") => Distortion(state, zone, Registry);

        internal static int Distortion(Investigation state, string zone, IReadOnlyDictionary<string, Definition> definitions)
        {
            bool Counted(string id, FormStatus status) => status == FormStatus.Affirmed &&
                definitions.TryGetValue(id, out var spec) && spec.Zone == zone && spec.FalseClaim;
            int count = Counted("F101", state.f101.current) ? 1 : 0;
            if (state.forms != null)
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "F101" };
                for (int i = 0; i < state.forms.Count; i++)
                {
                    var f = state.forms[i];
                    if (f != null && !string.IsNullOrEmpty(f.formId) && Counted(f.formId, f.current) && seen.Add(f.formId))
                        count++;
                }
            }
            return Mathf.Min(4, count);
        }

        public static bool Apply(Investigation candidate, FormStatus decision) =>
            ApplyWithReasoning(candidate, decision, null, out _);

        public static bool ApplyWithReasoning(
            Investigation candidate,
            FormStatus decision,
            DeductionSubmission submission,
            out HypothesisEvaluation evaluation)
        {
            evaluation = HypothesisService.EvaluateF101(candidate, decision, submission);
            if (!evaluation.Accepted)
                return false;

            var record = candidate.f101;
            candidate.f101History ??= new();

            if (record.current == decision)
            {
                // Duplicate identical submission is a safe no-op (never increments revision twice).
                return true;
            }

            if (record.revision == 0)
                record.firstDecision = decision;
            record.current = decision;
            record.revision++;
            candidate.Remember("FS02");

            var normalizedIds = new List<string>();
            string claimId = "";
            if (decision == FormStatus.Refuted && submission != null)
            {
                var seen = new HashSet<string>(StringComparer.Ordinal);
                if (submission.selectedEvidenceIds != null)
                {
                    for (int i = 0; i < submission.selectedEvidenceIds.Count; i++)
                    {
                        string id = submission.selectedEvidenceIds[i];
                        if (!string.IsNullOrEmpty(id) && seen.Add(id))
                        {
                            normalizedIds.Add(id);
                            var obsRec = candidate.FindObservation(id);
                            if (obsRec != null && obsRec.stage < ObservationStage.Corroborated)
                                obsRec.stage = ObservationStage.Corroborated;
                        }
                    }
                }
                claimId = submission.claimId ?? "";
                candidate.f101Argument = new DeductionSubmission
                {
                    formId = "F101",
                    selectedEvidenceIds = new List<string>(normalizedIds),
                    claimId = claimId,
                    annotationVersion = submission.annotationVersion,
                    annotations = submission.annotationVersion == F101Annotations.Version ? submission.annotations.Clone() : null
                };
                PuzzleService.RecordAttempt(candidate, HypothesisService.F101PuzzleId, true);
            }
            else
            {
                candidate.f101Argument = new DeductionSubmission { formId = "F101", claimId = "" };
            }

            candidate.f101History.Add(new FormRevisionEntry
            {
                revision = record.revision,
                decision = decision,
                claimId = claimId,
                evidenceIds = normalizedIds,
                chapter = candidate.chapter ?? "CH01",
                layer = candidate.layer,
                annotationVersion = candidate.f101Argument.annotationVersion,
                annotations = candidate.f101Argument.annotations?.Clone()
            });
            return true;
        }

        public static bool ApplyForm(Investigation candidate, string formId, FormStatus decision)
        {
            if (string.IsNullOrEmpty(formId) || !Registry.ContainsKey(formId)) return false;
            if (string.Equals(formId, "F101", StringComparison.OrdinalIgnoreCase))
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

        internal static bool ValidExtras(Investigation state)
        {
            if (state.forms == null) return true;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "F101" };
            return state.forms.TrueForAll(record => record != null && !string.IsNullOrEmpty(record.formId) &&
                Registry.ContainsKey(record.formId) && seen.Add(record.formId) && record.revision > 0 &&
                Enum.IsDefined(typeof(FormStatus), record.current) && record.current != FormStatus.Pending &&
                Enum.IsDefined(typeof(FormStatus), record.firstDecision) && record.firstDecision != FormStatus.Pending &&
                (record.current != FormStatus.Refuted || RefutationAvailable(state, record.formId)));
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
        public const string DutyOrders = "E02";
        public const string DutyLog = "E09";
        public const string MeetingTape = "E06a";
        public const string MorgueRegister = "E04";
        public const string LanRecovery = "E07";
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
            [DutyOrders] = new(Reality.R, "CH04", "LOC06"),
            [DutyLog] = new(Reality.R, "CH04", "LOC06"),
            [MeetingTape] = new(Reality.R, "CH04", "LOC06"),
            [MorgueRegister] = new(Reality.R, "CH04", "LOC10"),
            [LanRecovery] = new(Reality.R, "CH04", "LOC10"),
            [NonExportableTraceE08] = new(Reality.B, "CH04", "LOC08", false),
        };

        public static bool Grant(Investigation state, string id, Reality provenanceLayer, string provenanceLocation)
        {
            if (state == null || string.IsNullOrEmpty(id) || !Specs.TryGetValue(id, out var spec) || state.Has(id))
                return false;
            // Non-exportable B/M traces (such as E08) cannot be granted into durable physical R evidence inventory!
            if (!spec.PhysicalExportable || state.layer != spec.Layer || provenanceLayer != spec.Layer ||
                (state.chapter != spec.Chapter && !(state.chapter == "CH04" && spec.Chapter == "CH03")) ||
                provenanceLocation != spec.Location)
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
        public static bool G02(Investigation state) => state.chapter is ("CH02" or "CH03" or "CH04") && H01Tutorial(state);
        // Core 07 Chapter gates: G03 opens when E03 is collected and C02 existence is confirmed (name is optional per CH03-Q02!)
        public static bool G03(Investigation state) =>
            state.chapter is ("CH03" or "CH04") && state.Has(EvidenceRegistry.PatientRegister) && state.PatientConfirmed("C02");

        public static bool H05(Investigation state) => state.Has(EvidenceRegistry.MorgueRegister) &&
            state.Has(EvidenceRegistry.LanRecovery) && state.Happened("H05.verified");
        public static bool G04(Investigation state) => state.chapter == "CH04" && H05(state) &&
            state.Happened("PT01.presented") && state.Happened("PT02.presented");
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
                "CH04-CP01" or "CH04-CP02" => "AP07",
                "CH04-CP03" => "AP08",
                "CH04-CP04" => "AP09",
                _ => null
            };
        }
    }
}
