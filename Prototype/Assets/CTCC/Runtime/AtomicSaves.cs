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

        public SaveRead Read(SaveSlot slot,out Investigation state)
        {
            string path=SlotPath(slot);
            SaveRead main = ReadFile(path,out state);
            if (main == SaveRead.Valid || main == SaveRead.Unsupported)
                return main;
            SaveRead backup = ReadFile(path+".bak",out state);
            if (backup == SaveRead.Valid) return SaveRead.Backup;
            if (backup == SaveRead.Unsupported) return SaveRead.Unsupported;
            return main == SaveRead.Empty && backup == SaveRead.Empty ? SaveRead.Empty : SaveRead.Damaged;
        }

        public void Write(SaveSlot slot,Investigation state,Action afterFlush=null)
        {
            using var write = WriteMarker.Auto();
            if (!Valid(state))
                throw new InvalidDataException("Snapshot is inconsistent.");
            Directory.CreateDirectory(directory);
            string path=SlotPath(slot);
            using var slotLock = RetrySharing(() => new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None));
            string temp=path+".tmp";
            string payload=JsonUtility.ToJson(state);
            var bytes=Utf8.GetBytes(JsonUtility.ToJson(new Envelope
            {
                schemaVersion=Investigation.Schema,
                payload=payload,
                checksum=Hash(payload)
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

        private static bool IsSharingError(IOException error) => (error.HResult & 0xffff) is 32 or 33;

        private static T RetrySharing<T>(Func<T> operation)
        {
            for (int attempt = 0;; attempt++)
            {
                try { return operation(); }
                catch (IOException e) when (attempt < 2 && IsSharingError(e)) { Thread.Sleep(4 << attempt); }
            }
        }

        private static SaveRead ReadFile(string path,out Investigation state,bool throwSharingError=false)
        {
            state=null;
            if (!File.Exists(path)) return SaveRead.Empty;
            try
            {
                if (new FileInfo(path).Length > 262144)
                    return SaveRead.Damaged;
                var data=JsonUtility.FromJson<Envelope>(File.ReadAllText(path,Utf8));
                if (data==null || string.IsNullOrEmpty(data.payload) || data.checksum!=Hash(data.payload))
                    return SaveRead.Damaged;
                if (data.schemaVersion!=Investigation.Schema)
                    return SaveRead.Unsupported;
                var decoded=JsonUtility.FromJson<Investigation>(data.payload);
                if (!Valid(decoded)) return SaveRead.Damaged;
                state=decoded;
                return SaveRead.Valid;
            }
            catch (IOException e) when (throwSharingError && IsSharingError(e)) { throw; }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException ||
                                     e is ArgumentException || e is DecoderFallbackException)
            {
                return SaveRead.Damaged;
            }
        }

        private static bool Valid(Investigation s)
        {
            if (s==null || s.schemaVersion!=Investigation.Schema || s.sequence<0 || s.settings==null || s.f101==null ||
                s.events==null || s.evidence==null || s.observed==null || (s.chapter!="CH01" && s.chapter!="CH02") ||
                SafeCheckpoints.Pad(s.checkpoint)==null || !Enum.IsDefined(typeof(Reality),s.layer) ||
                !Enum.IsDefined(typeof(FormStatus),s.f101.current) || !Enum.IsDefined(typeof(FormStatus),s.f101.firstDecision))
                return false;
            if (s.publicDeathCount!=13 || !s.publicLanDeathKnown || !s.publicFireKnown ||
                s.f101.revision<0 || (s.f101.revision==0)!=(s.f101.current==FormStatus.Pending) ||
                (s.f101.revision==0)!=(s.f101.firstDecision==FormStatus.Pending) ||
                s.f101.current==FormStatus.Refuted && !FormService.RefutationAvailable(s))
                return false;
            if (new HashSet<string>(s.evidence).Count!=s.evidence.Count || new HashSet<string>(s.events).Count!=s.events.Count ||
                new HashSet<string>(s.observed).Count!=s.observed.Count || !s.Has("E10a") ||
                s.evidence.Exists(e=>!EvidenceRegistry.Registered(e)))
                return false;
            if (s.Has(EvidenceRegistry.Drawing) && !EvidenceRegistry.HaveFragments(s) ||
                s.Happened("C01.verified") && (!EvidenceRegistry.BundleComplete(s) || !s.Has("E12")) ||
                s.chapter=="CH02" && (!GateService.G01(s) || !s.Happened("CH01-T10")) ||
                s.chapter=="CH01" && s.evidence.Count!=1)
                return false;
            return true;
        }

        private static string Hash(string data)
        {
            using var sha=SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Utf8.GetBytes(data)));
        }
    }
}
