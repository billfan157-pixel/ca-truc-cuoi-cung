using System;
using System.Collections.Generic;
using UnityEngine;

namespace CTCC
{
    public enum Reality { R, B }
    public enum FormStatus { Pending = 0, Affirmed = 2, Unverified = 3, Refuted = 4 }

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
    public sealed class Investigation
    {
        public const int Schema = 1;
        public int schemaVersion = Schema;
        public long sequence;
        public string checkpoint = "CH01-CP00";
        public string chapter = "CH01";
        public Reality layer;
        public F101Record f101 = new();
        public VisitSettings settings = new();
        public bool publicFireKnown = true;
        public bool publicLanDeathKnown = true;
        public int publicDeathCount = 13;
        public int torchSeconds = 360;
        public List<string> events = new();
        public List<string> evidence = new();
        public List<string> observed = new();

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
        public static int Distortion(Investigation state) => state.f101.current == FormStatus.Affirmed ? 1 : 0;

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
    }

    public static class EvidenceRegistry
    {
        public const string Drawing = "E11.drawing";
        public const string Wristband = "E11.wristband";
        public const string Transfer = "E11.transferNote";
        public static readonly string[] DrawingFragments = { "E11.drawing.1", "E11.drawing.2", "E11.drawing.3" };
        private static readonly HashSet<string> ValidPhysical = new()
        {
            "E11.drawing.1", "E11.drawing.2", "E11.drawing.3", Drawing, Wristband, Transfer, "E12"
        };

        public static bool Grant(Investigation state,string id,Reality provenanceLayer,string provenanceLocation)
        {
            if (state.layer != Reality.R || provenanceLayer != Reality.R || state.chapter != "CH02" ||
                provenanceLocation != "LOC03" || !ValidPhysical.Contains(id) || state.Has(id))
                return false;
            if (id == Drawing && !HaveFragments(state))
                return false;
            state.evidence.Add(id);
            return true;
        }

        public static bool HaveFragments(Investigation state) =>
            state.Has(DrawingFragments[0]) && state.Has(DrawingFragments[1]) && state.Has(DrawingFragments[2]);
        public static bool BundleComplete(Investigation state) =>
            state.Has(Drawing) && state.Has(Wristband) && state.Has(Transfer);
        public static bool Registered(string id) => id == "E10a" || ValidPhysical.Contains(id);
    }

    public static class GateService
    {
        public static bool G01(Investigation state) => state.f101.current != FormStatus.Pending;
        public static bool H01Tutorial(Investigation state) =>
            state.Has("E12") && EvidenceRegistry.BundleComplete(state) && state.Happened("C01.verified");
        public static bool H02(Investigation state) => state.Has("E01") && state.Has("E04");
        public static bool G02(Investigation state) => state.chapter == "CH02" && H01Tutorial(state);
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
                _ => null
            };
        }
    }
}
