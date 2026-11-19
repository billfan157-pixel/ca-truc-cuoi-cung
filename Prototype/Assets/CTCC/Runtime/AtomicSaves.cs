using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
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
            if (!Valid(state))
                throw new InvalidDataException("Snapshot is inconsistent.");
            Directory.CreateDirectory(directory);
            string path=SlotPath(slot);
            using var slotLock=new FileStream(path+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
            string temp=path+".tmp";
            string payload=JsonUtility.ToJson(state);
            var bytes=Utf8.GetBytes(JsonUtility.ToJson(new Envelope
            {
                schemaVersion=Investigation.Schema,
                payload=payload,
                checksum=Hash(payload)
            }));
            using (var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough))
            {
                stream.Write(bytes,0,bytes.Length);
                stream.Flush(true);
            }
            if (ReadFile(temp,out _) != SaveRead.Valid)
                throw new IOException("Temporary save did not validate.");
            afterFlush?.Invoke();
            if (File.Exists(path))
            {
                string backup=ReadFile(path,out _) == SaveRead.Valid ? path+".bak" : path+".replaced";
                File.Replace(temp,path,backup);
            }
            else
                File.Move(temp,path);
        }

        private static SaveRead ReadFile(string path,out Investigation state)
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
