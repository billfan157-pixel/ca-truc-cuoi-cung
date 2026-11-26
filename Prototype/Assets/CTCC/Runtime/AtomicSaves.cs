using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Unity.Profiling;
using UnityEngine;

namespace CTCC
{
    public enum SaveSlot { Auto, Manual }
    public enum SaveRead { Empty, Valid, Backup, Unsupported, Damaged }

    public sealed class AtomicSaves
    {
        [Serializable]
        private sealed class Envelope
        {
            public int schemaVersion;
            public string payload;
            public string checksum;
        }

        private readonly string directory;
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
        private static readonly ProfilerMarker WriteMarker = new("CTCC.Save.AtomicWrite");
        private static readonly ProfilerMarker FlushMarker = new("CTCC.Save.DurableFlush");
        private static readonly ProfilerMarker PublishMarker = new("CTCC.Save.Publish");

        public AtomicSaves(string directory) { this.directory = directory; }

        public string SlotPath(SaveSlot slot) => Path.Combine(directory, slot == SaveSlot.Auto ? "auto.json" : "manual.json");

        public SaveRead Read(SaveSlot slot, out Investigation state)
        {
            string path = SlotPath(slot);
            SaveRead main = ReadFile(path, out state);
            if (main == SaveRead.Valid || main == SaveRead.Unsupported)
                return main;
            SaveRead backup = ReadFile(path + ".bak", out state);
            if (backup == SaveRead.Valid) return SaveRead.Backup;
            if (backup == SaveRead.Unsupported) return SaveRead.Unsupported;
            return main == SaveRead.Empty && backup == SaveRead.Empty ? SaveRead.Empty : SaveRead.Damaged;
        }

        public void Write(SaveSlot slot, Investigation state, Action afterFlush = null)
        {
            using var write = WriteMarker.Auto();
            if (!Valid(state))
                throw new InvalidDataException("Snapshot is inconsistent.");
            Directory.CreateDirectory(directory);
            string path = SlotPath(slot);
            using var slotLock = RetrySharing(() => new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None));
            string temp = path + ".tmp";
            string payload = JsonUtility.ToJson(state);
            var bytes = Utf8.GetBytes(JsonUtility.ToJson(new Envelope
            {
                schemaVersion = Investigation.Schema,
                payload = payload,
                checksum = Hash(payload)
            }));
            try
            {
                using (var stream = RetrySharing(() => new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None)))
                {
                    using var flush = FlushMarker.Auto();
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                if (RetrySharing(() => ReadFile(temp, out _, true)) != SaveRead.Valid)
                    throw new IOException("Temporary save did not validate.");
                afterFlush?.Invoke();
                using var publish = PublishMarker.Auto();
                if (File.Exists(path))
                {
                    bool mainValid = RetrySharing(() => ReadFile(path, out _, true)) == SaveRead.Valid;
                    string backup = path + (mainValid ? ".bak" : ".replaced");
                    RetrySharing(() => { File.Replace(temp, path, backup); return true; });
                }
                else RetrySharing(() => { File.Move(temp, path); return true; });
            }
            finally
            {
                // The same slot lock covers any leftover temp from an interrupted write.
                try { File.Delete(temp); }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                { Debug.LogWarning($"CTCC temporary save could not be removed (0x{e.HResult:X8})."); }
            }
        }

        private static bool IsSharingError(IOException error) => (error.HResult & 0xffff) is 32 or 33 or 1175 or 1176 or 1177;

        private static T RetrySharing<T>(Func<T> operation)
        {
            for (int attempt = 0; ; attempt++)
            {
                try { return operation(); }
                catch (IOException e) when (attempt < 4 && IsSharingError(e)) { Thread.Sleep(5 << attempt); }
            }
        }

        private static SaveRead ReadFile(string path, out Investigation state, bool throwSharingError = false)
        {
            state = null;
            if (!File.Exists(path)) return SaveRead.Empty;
            try
            {
                if (new FileInfo(path).Length > 262144)
                    return SaveRead.Damaged;
                var data = JsonUtility.FromJson<Envelope>(File.ReadAllText(path, Utf8));
                if (data == null || string.IsNullOrEmpty(data.payload) || data.checksum != Hash(data.payload))
                    return SaveRead.Damaged;
                if (data.schemaVersion < Investigation.MinSupportedSchema || data.schemaVersion > Investigation.Schema)
                    return SaveRead.Unsupported;
                var decoded = JsonUtility.FromJson<Investigation>(data.payload);
                if (decoded == null) return SaveRead.Damaged;
                if (data.schemaVersion == 1 || decoded.schemaVersion == 1)
                    MigrateV1ToV2(decoded);
                else if (data.schemaVersion < Investigation.Schema || decoded.schemaVersion < Investigation.Schema)
                    UpgradeLegacy(decoded);
                // Run idempotent Rebirth III Save v7 normalizer for ALL schema versions (1..7) before structural validation.
                InventoryService.ReconcileSaveV7ForRebirthIII(decoded);
                if (!Valid(decoded)) return SaveRead.Damaged;
                decoded.transcript ??= new();
                decoded.forms ??= new();
                decoded.patients ??= new();
                decoded.puzzles ??= new();
                decoded.observationRecords ??= new();
                decoded.f101History ??= new();
                decoded.f101Argument ??= new();
                decoded.backpackItems ??= new();
                decoded.quickSlots ??= new();
                decoded.equippedInstanceId ??= "";
                decoded.dreadEventsSeen ??= new();
                state = decoded;
                return SaveRead.Valid;
            }
            catch (IOException e) when (throwSharingError && IsSharingError(e)) { throw; }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException ||
                                     e is ArgumentException || e is DecoderFallbackException)
            {
                return SaveRead.Damaged;
            }
        }

        public static void MigrateV1ToV2(Investigation s)
        {
            if (s == null) return;
            s.forms ??= new();
            s.patients ??= new();
            s.puzzles ??= new();
            if (s.Happened("C01.verified"))
                s.VerifyPatientIdentity("C01", "Bé An");
            UpgradeLegacy(s);
        }

        private static void UpgradeLegacy(Investigation s)
        {
            int fromVersion = s.schemaVersion;
            s.schemaVersion = Investigation.Schema;
            s.f101Annotations ??= new();
            s.journalNotes ??= new();
            s.sceneProps ??= new();
            s.backpackItems ??= new();
            s.quickSlots ??= new();
            s.equippedInstanceId ??= "";
            if (fromVersion < 4)
            {
                if (fromVersion <= 2)
                {
                    s.forms?.Clear(); // Earlier releases only expose F101, stored separately.
                    if (s.evidence?.Remove(EvidenceRegistry.BayLockReceipt) == true)
                        s.Remember("FS10");
                    if (s.evidence?.Contains(EvidenceRegistry.C12UnnamedBox) == true)
                    {
                        s.Remember("FS19");
                        s.Remember("CH03-T05");
                    }
                }
                // Schema v1/v2/v3 -> v4 migration: synthesize ObservationRecords, F101 revision history & argument cleanly.
                s.observationRecords ??= new();
                s.f101History ??= new();
                s.f101Argument ??= new();
                s.dreadEventsSeen ??= new();
                if (s.dreadSeed == 0) s.dreadSeed = 20051118;

                if (s.observed != null)
                {
                    for (int i = 0; i < s.observed.Count; i++)
                    {
                        string obsId = s.observed[i];
                        if (s.FindObservation(obsId) != null) continue;
                        var spec = ObservationRegistry.Get(obsId);
                        if (spec == null) continue;
                        var defaultLayer = spec.AllowedLayers != null && spec.AllowedLayers.Length > 0 ? spec.AllowedLayers[0] : Reality.B;
                        var defaultLoc = spec.AllowedLocations != null && spec.AllowedLocations.Length > 0 ? spec.AllowedLocations[0] : "LOC03";
                        var defaultChap = spec.AllowedChapters != null && spec.AllowedChapters.Length > 0 ? spec.AllowedChapters[0] : "CH01";
                        s.observationRecords.Add(new ObservationRecord
                        {
                            id = obsId,
                            layer = defaultLayer,
                            location = defaultLoc,
                            chapter = defaultChap,
                            stage = ObservationStage.Examined
                        });
                    }
                }

                if (s.f101 != null && s.f101.revision > 0 && s.f101History.Count == 0)
                {
                    if (s.f101.current == FormStatus.Refuted && FormService.RefutationAvailable(s))
                    {
                        s.f101Argument = new DeductionSubmission
                        {
                            formId = "F101",
                            selectedEvidenceIds = new List<string> { ObservationRegistry.Bed4Traces, ObservationRegistry.Bed4Card },
                            claimId = HypothesisService.ClaimUnreliableZero
                        };
                        var recA = s.FindObservation(ObservationRegistry.Bed4Traces);
                        var recB = s.FindObservation(ObservationRegistry.Bed4Card);
                        if (recA != null) recA.stage = ObservationStage.Corroborated;
                        if (recB != null) recB.stage = ObservationStage.Corroborated;
                    }
                    for (int r = 1; r <= s.f101.revision; r++)
                    {
                        FormStatus stepDecision = r == 1 ? s.f101.firstDecision : (r == s.f101.revision ? s.f101.current : FormStatus.Unverified);
                        bool stepRefuted = stepDecision == FormStatus.Refuted;
                        s.f101History.Add(new FormRevisionEntry
                        {
                            revision = r,
                            decision = stepDecision,
                            claimId = stepRefuted ? HypothesisService.ClaimUnreliableZero : "",
                            evidenceIds = stepRefuted
                                ? new List<string> { ObservationRegistry.Bed4Traces, ObservationRegistry.Bed4Card }
                                : new List<string>(),
                            chapter = r == 1 ? "CH01" : (s.chapter ?? "CH01"),
                            layer = r == 1 ? Reality.B : s.layer
                        });
                    }
                }
            }

            if (fromVersion < 7)
                InventoryService.MigrateFromLegacy(s);
        }

        private static bool Valid(Investigation s)
        {
            if (s == null || s.schemaVersion != Investigation.Schema || s.sequence < 0 || s.settings == null || s.f101 == null ||
                s.events == null || s.evidence == null || s.observed == null ||
                s.chapter is not ("CH01" or "CH02" or "CH03" or "CH04" or "CH05") ||
                SafeCheckpoints.Pad(s.checkpoint) == null || !Enum.IsDefined(typeof(Reality), s.layer) ||
                !Enum.IsDefined(typeof(FormStatus), s.f101.current) || !Enum.IsDefined(typeof(FormStatus), s.f101.firstDecision))
                return false;
            if (s.publicDeathCount != 13 || !s.publicLanDeathKnown || !s.publicFireKnown ||
                s.f101.revision < 0 || (s.f101.revision == 0) != (s.f101.current == FormStatus.Pending) ||
                (s.f101.revision == 0) != (s.f101.firstDecision == FormStatus.Pending) ||
                s.f101.current == FormStatus.Refuted && !FormService.RefutationAvailable(s))
                return false;
            if (new HashSet<string>(s.evidence).Count != s.evidence.Count || new HashSet<string>(s.events).Count != s.events.Count ||
                new HashSet<string>(s.observed).Count != s.observed.Count ||
                s.evidence.Exists(e => !EvidenceRegistry.Registered(e)))
                return false;
            // Core v3.6.2 (DEC-V36-CH01-01 / D3 Rev 1.1): CH01 allows 0 evidence before mailbox pickup and 1 ("E10a") once acquired (or legacy partial-inspect).
            if (s.chapter == "CH01")
            {
                if (!s.Has("E10a"))
                {
                    if (s.evidence.Count != 0 || s.Happened("CH01-T02") || s.Happened(E10aParts.MailboxPickedUp))
                        return false;
                }
                else if (s.evidence.Count != 1)
                {
                    return false;
                }
            }
            else if (!s.Has("E10a"))
            {
                return false;
            }
            if (s.Has(EvidenceRegistry.Drawing) && !EvidenceRegistry.HaveFragments(s) ||
                s.Happened("C01.verified") && (!EvidenceRegistry.BundleComplete(s) || !s.Has("E12")) ||
                s.chapter != "CH01" && (!GateService.G01(s) || !s.Happened("CH01-T10")) ||
                s.chapter is ("CH03" or "CH04" or "CH05") && !GateService.G02(s) ||
                s.chapter == "CH04" && (!GateService.G03(s) ||
                    !s.checkpoint.StartsWith("CH04-", StringComparison.Ordinal) || s.layer != Reality.R) ||
                s.chapter == "CH05" && (!GateService.G03(s) || !GateService.G04(s) || !s.Happened("CH04-T06") ||
                    !s.checkpoint.StartsWith("CH05-", StringComparison.Ordinal) || s.layer == Reality.B))
                return false;
            if (s.Happened("C12.verified") || !FormService.ValidExtras(s) ||
                !ChapterFour.ValidPower(s.servicePower) ||
                s.chapter is not ("CH04" or "CH05") && s.servicePower != 0 ||
                s.Happened("H05.verified") && !GateService.H05(s) ||
                s.Happened("PT02.presented") && !GateService.H05(s) ||
                s.Happened("CH04-T06") && !GateService.G04(s) ||
                s.Happened("H03.verified") && !GateService.H03(s) ||
                s.Happened("G05.opened") && !GateService.G05(s) ||
                s.Happened("G06.opened") || s.Has("E08") || s.Has("E01") || s.Has("E06") || s.Has("E10b"))
                return false;
            var puzzles = new HashSet<string>(StringComparer.Ordinal);
            if (s.puzzles != null && s.puzzles.Exists(p => p == null ||
                p.puzzleId is not ("CH01-F101" or "CH03-PU01" or "CH03-PU02" or "CH04-PU01" or "CH04-PU02" or "CH04-PU03" or "CH05-PU01" or "CH05-PU02") || !puzzles.Add(p.puzzleId) ||
                p.failedAttempts < 0 || p.solved != s.Happened(p.puzzleId + ".solved")))
                return false;
            if (s.observed.Exists(id => !ObservationRegistry.Registered(id)))
                return false;
            if (s.observationRecords != null)
            {
                var seenObs = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < s.observationRecords.Count; i++)
                {
                    var rec = s.observationRecords[i];
                    if (rec == null || !ObservationRegistry.Registered(rec.id) || !seenObs.Add(rec.id) ||
                        !s.Observed(rec.id) || !Enum.IsDefined(typeof(ObservationStage), rec.stage) ||
                        !ObservationRegistry.ValidProvenance(rec.id, rec.layer, rec.location, rec.chapter))
                        return false;
                }
            }
            if (!HypothesisService.ValidPersistedF101(s))
                return false;
            if (s.journalNotes == null || s.journalNotes.Count > JournalNote.MaxNotes ||
                s.journalNotes.Exists(n => n == null || !n.ValidFor(s)) || !ScenePropPose.ValidList(s.sceneProps))
                return false;
            if (!InventoryService.ValidState(s) || s.e10aLastTab < 0 || s.e10aLastTab > 4)
                return false;
            if (s.exception0317Link.HasValue && (!CorroborationService.IsCorroborated(s) ||
                !CorroborationService.Supported0317(s, s.exception0317Link)))
                return false;
            if (s.dreadEventsSeen != null)
            {
                var seenDread = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < s.dreadEventsSeen.Count; i++)
                    if (!DreadCatalog.IsRegistered(s.dreadEventsSeen[i]) || !seenDread.Add(s.dreadEventsSeen[i]))
                        return false;
            }
            // Canon CH03-Q03 invariant: C12 can never have a verified identity or fabricated name.
            if (s.patients != null)
            {
                foreach (var p in s.patients)
                {
                    if (p == null || string.IsNullOrEmpty(p.code)) return false;
                    if (string.Equals(p.code, "C12", StringComparison.OrdinalIgnoreCase) &&
                        (p.identityVerified || !string.IsNullOrEmpty(p.displayName)))
                        return false;
                }
            }
            if (s.transcript != null)
            {
                var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in s.transcript)
                    if (string.IsNullOrEmpty(entry.LineId) || string.IsNullOrWhiteSpace(entry.Text) || !ids.Add(entry.LineId))
                        return false;
            }
            return true;
        }

        private static string Hash(string data)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Utf8.GetBytes(data)));
        }
    }
}
