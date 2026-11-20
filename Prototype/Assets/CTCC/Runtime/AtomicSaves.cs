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
                else if (data.schemaVersion == 2 && decoded.schemaVersion == 2)
                    UpgradeLegacy(decoded);
                if (!Valid(decoded)) return SaveRead.Damaged;
                decoded.transcript ??= new();
                decoded.forms ??= new();
                decoded.patients ??= new();
                decoded.puzzles ??= new();
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
            s.schemaVersion = Investigation.Schema;
            s.forms?.Clear(); // Earlier releases only expose F101, stored separately.
            if (s.evidence?.Remove(EvidenceRegistry.BayLockReceipt) == true)
                s.Remember("FS10");
            if (s.evidence?.Contains(EvidenceRegistry.C12UnnamedBox) == true)
            {
                s.Remember("FS19");
                s.Remember("CH03-T05");
            }
        }

        private static bool Valid(Investigation s)
        {
            if (s == null || s.schemaVersion != Investigation.Schema || s.sequence < 0 || s.settings == null || s.f101 == null ||
                s.events == null || s.evidence == null || s.observed == null ||
                s.chapter is not ("CH01" or "CH02" or "CH03" or "CH04") ||
                SafeCheckpoints.Pad(s.checkpoint) == null || !Enum.IsDefined(typeof(Reality), s.layer) ||
                !Enum.IsDefined(typeof(FormStatus), s.f101.current) || !Enum.IsDefined(typeof(FormStatus), s.f101.firstDecision))
                return false;
            if (s.publicDeathCount != 13 || !s.publicLanDeathKnown || !s.publicFireKnown ||
                s.f101.revision < 0 || (s.f101.revision == 0) != (s.f101.current == FormStatus.Pending) ||
                (s.f101.revision == 0) != (s.f101.firstDecision == FormStatus.Pending) ||
                s.f101.current == FormStatus.Refuted && !FormService.RefutationAvailable(s))
                return false;
            if (new HashSet<string>(s.evidence).Count != s.evidence.Count || new HashSet<string>(s.events).Count != s.events.Count ||
                new HashSet<string>(s.observed).Count != s.observed.Count || !s.Has("E10a") ||
                s.evidence.Exists(e => !EvidenceRegistry.Registered(e)))
                return false;
            if (s.Has(EvidenceRegistry.Drawing) && !EvidenceRegistry.HaveFragments(s) ||
                s.Happened("C01.verified") && (!EvidenceRegistry.BundleComplete(s) || !s.Has("E12")) ||
                s.chapter != "CH01" && (!GateService.G01(s) || !s.Happened("CH01-T10")) ||
                s.chapter is ("CH03" or "CH04") && !GateService.G02(s) ||
                s.chapter == "CH04" && (!GateService.G03(s) ||
                    !s.checkpoint.StartsWith("CH04-", StringComparison.Ordinal) || s.layer != Reality.R) ||
                s.chapter == "CH01" && s.evidence.Count != 1)
                return false;
            if (s.Happened("C12.verified") || !FormService.ValidExtras(s) ||
                !ChapterFour.ValidPower(s.servicePower) ||
                s.chapter != "CH04" && s.servicePower != 0 ||
                s.Happened("H05.verified") && !GateService.H05(s) ||
                s.Happened("PT02.presented") && !GateService.H05(s) ||
                s.Happened("CH04-T06") && !GateService.G04(s))
                return false;
            var puzzles = new HashSet<string>(StringComparer.Ordinal);
            if (s.puzzles != null && s.puzzles.Exists(p => p == null ||
                p.puzzleId is not ("CH03-PU01" or "CH03-PU02") || !puzzles.Add(p.puzzleId) ||
                p.failedAttempts < 0 || p.solved != s.Happened(p.puzzleId + ".solved")))
                return false;
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
