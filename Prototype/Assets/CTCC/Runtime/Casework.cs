using System;
using System.Threading.Tasks;
using UnityEngine;

namespace CTCC
{
    public sealed class Casework
    {
        private readonly RealityTransaction reality;
        public Investigation State => reality.Current;
        public Casework(RealityTransaction reality) { this.reality = reality; }

        public CommitResult Begin(Vector3 feet, VisitSettings options)
        {
            var proposed = Investigation.New();
            proposed.settings = options;
            return reality.Commit(proposed, feet, warp: true, geometryChanges: true, newVisit: true);
        }

        public CommitResult ReadLetter(Vector3 feet) =>
            Record("E10a.SMSRead", feet);

        public CommitResult OpenGate(Vector3 feet)
        {
            if (!State.Happened("E10a.SMSRead")) return CommitResult.PadRequired;
            return Record("CH01-T02", feet, "CH01-S1", true);
        }

        public CommitResult AtHub(Vector3 feet, out bool warp)
        {
            if (!PrepareHub(out var next, out warp)) return CommitResult.Applied;
            return reality.Commit(next, feet, "AP02", true, true);
        }

        public Task<CommitResult> AtHubAsync(Vector3 feet)
        {
            if (!PrepareHub(out var next, out _)) return Task.FromResult(CommitResult.Applied);
            return reality.CommitAsync(next, feet, "AP02", true, true);
        }

        private bool PrepareHub(out Investigation next, out bool warp)
        {
            warp = false;
            next = null;
            if (State.chapter == "CH02") return false;
            var candidate = State.Copy();
            if (State.Happened("CH01-T02") && !State.Happened("CH01-T03"))
            {
                candidate.layer = Reality.B;
                candidate.checkpoint = "CH01-CP01";
                candidate.Remember("CH01-T03");
                candidate.Remember("CH01-T04");
                warp = true;
                next = candidate;
                return true;
            }
            if (GateService.G01(State) && !State.Happened("CH01-T08"))
            {
                candidate.layer = Reality.R;
                candidate.checkpoint = "CH01-CP03";
                candidate.Remember("CH01-T08");
                warp = true;
                next = candidate;
                return true;
            }
            if (State.Happened("CH01-T09") && !State.Happened("CH01-T10"))
            {
                candidate.layer = Reality.R;
                candidate.chapter = "CH02";
                candidate.checkpoint = "CH01-CP04";
                candidate.Remember("CH01-T10");
                warp = true;
                next = candidate;
                return true;
            }
            return false;
        }

        public CommitResult Board(Vector3 feet) =>
            State.layer == Reality.B && State.chapter == "CH01" && State.Happened("CH01-T03")
                ? Record("CH01-T05", feet, "CH01-CP01", true) : CommitResult.PadRequired;

        public CommitResult EnterRoom(Vector3 feet) =>
            State.chapter == "CH01" && State.Happened("CH01-T05")
                ? Record("CH01-T06", feet, "CH01-CP02") : CommitResult.Applied;

        public CommitResult OutsideAttempt(Vector3 feet)
        {
            if (!PrepareOutsideAttempt(out var next)) return CommitResult.PadRequired;
            return reality.Commit(next, feet, warp: true, geometryChanges: true);
        }

        public Task<CommitResult> OutsideAttemptAsync(Vector3 feet)
        {
            if (!PrepareOutsideAttempt(out var next)) return Task.FromResult(CommitResult.PadRequired);
            return reality.CommitAsync(next, feet, warp: true, geometryChanges: true);
        }

        private bool PrepareOutsideAttempt(out Investigation next)
        {
            next = null;
            if (State.chapter != "CH01" || !State.Happened("CH01-T08") || State.Happened("CH01-T09"))
                return false;
            next = State.Copy();
            next.checkpoint = "CH01-S7";
            next.Remember("CH01-T09");
            return true;
        }

        public CommitResult SwitchIn101(Vector3 feet)
        {
            if (!PrepareSwitch101(out var next)) return CommitResult.PadRequired;
            return reality.Commit(next, feet, "AP04", true, true);
        }

        public Task<CommitResult> SwitchIn101Async(Vector3 feet)
        {
            if (!PrepareSwitch101(out var next)) return Task.FromResult(CommitResult.PadRequired);
            return reality.CommitAsync(next, feet, "AP04", true, true);
        }

        private bool PrepareSwitch101(out Investigation next)
        {
            next = null;
            if (State.chapter != "CH02") return false;
            next = State.Copy();
            next.layer = State.layer == Reality.R ? Reality.B : Reality.R;
            next.checkpoint = GateService.G02(State) ? "CH02-CP03" : "CH02-CP02";
            return true;
        }

        public bool CanRevise => State.Happened("CH01-T05") &&
            (State.chapter == "CH02" || State.layer == Reality.B && !State.Happened("CH01-T08"));

        public CommitResult Observe(string id, Vector3 feet)
        {
            if ((id == "OBS101A" || id == "OBS101B") && State.layer != Reality.B ||
                (id == "LIST" || id == "SOCKETS" || id == "WHEELS") && (State.layer != Reality.R || State.chapter != "CH02"))
                return CommitResult.PadRequired;
            var next = State.Copy();
            return next.Observe(id) ? reality.Commit(next, feet) : CommitResult.Applied;
        }

        public CommitResult Submit(FormStatus form, bool pairedObservations, Vector3 feet)
        {
            var status = PrepareSubmit(form, pairedObservations, feet, out var next);
            return next == null ? status : reality.Commit(next, feet, "AP04", false, true);
        }

        public Task<CommitResult> SubmitAsync(FormStatus form, bool pairedObservations, Vector3 feet)
        {
            var status = PrepareSubmit(form, pairedObservations, feet, out var next);
            return next == null ? Task.FromResult(status) : reality.CommitAsync(next, feet, "AP04", false, true);
        }

        private CommitResult PrepareSubmit(FormStatus form, bool pairedObservations, Vector3 feet, out Investigation next)
        {
            next = null;
            if (!CanRevise || form == FormStatus.Refuted && !pairedObservations)
                return CommitResult.PadRequired;
            if (!reality.OnPad(feet, "AP04")) return CommitResult.PadRequired;
            var candidate = State.Copy();
            if (!FormService.Apply(candidate, form))
                return CommitResult.PadRequired;
            if (candidate.f101.revision == State.f101.revision)
                return CommitResult.Applied;
            candidate.Remember("CH01-T07");
            if (candidate.chapter == "CH01")
                candidate.checkpoint = "CH01-CP02";
            next = candidate;
            return CommitResult.Applied;
        }

        public string MissingPhysicalObservations()
        {
            if (!State.Observed("LIST")) return "Đọc bản kê cạnh giường 3.";
            if (!State.Observed("SOCKETS")) return "Đếm các vị trí ổ cắm ở cuối phòng.";
            if (!State.Observed("WHEELS")) return "Xem dấu bánh trên sàn bên tủ kho.";
            return null;
        }

        public CommitResult SolvePz01(Vector3 feet)
        {
            if (State.layer != Reality.R || State.chapter != "CH02" || MissingPhysicalObservations() != null)
                return CommitResult.PadRequired;
            if (State.Has("E12")) return CommitResult.Applied;
            var next = State.Copy();
            EvidenceRegistry.Grant(next, "E12", Reality.R, "LOC03");
            next.checkpoint = "CH02-CP02";
            next.Remember("CH02-T03");
            return reality.Commit(next, feet, geometryChanges: true);
        }

        public CommitResult Take(string id, Vector3 feet)
        {
            var result = PrepareEvidence(id, out var next);
            return next == null ? result : reality.Commit(next, feet);
        }

        public Task<CommitResult> TakeAsync(string id)
        {
            var result = PrepareEvidence(id, out var next);
            return next == null ? Task.FromResult(result) : reality.CommitEvidenceAsync(next);
        }

        private CommitResult PrepareEvidence(string id, out Investigation next)
        {
            next = null;
            if (!State.Has("E12") || State.layer != Reality.R || State.chapter != "CH02") return CommitResult.PadRequired;
            if (State.Has(id)) return CommitResult.Applied;
            var proposal = State.Copy();
            if (!EvidenceRegistry.Grant(proposal, id, Reality.R, "LOC03")) return CommitResult.PadRequired;
            next = proposal;
            return CommitResult.Applied;
        }

        public CommitResult VerifyAn(Vector3 feet)
        {
            if (!State.Has("E12") || !EvidenceRegistry.BundleComplete(State) || State.layer != Reality.R)
                return CommitResult.PadRequired;
            if (GateService.G02(State)) return CommitResult.Applied;
            var next = State.Copy();
            next.Remember("C01.verified");
            next.Remember("CH02-T05");
            next.Remember("G02.opened");
            next.checkpoint = "CH02-CP03";
            return reality.Commit(next, feet, "AP04", false, true);
        }

        public Task<CommitResult> SaveEmergencyAsync(Vector3 feet, bool finish, bool encounterActive,
            Action afterFlush = null)
        {
            if (!GateService.G02(State) || State.layer != Reality.R || !GatekeeperCameo.InEmergency(feet))
                return Task.FromResult(CommitResult.PadRequired);
            bool escaped = State.Happened("CH02-T06");
            if (!escaped && !encounterActive)
                return Task.FromResult(CommitResult.PadRequired);
            if (escaped && (!finish || State.Happened("M1-complete")))
                return Task.FromResult(CommitResult.Applied);

            var next = State.Copy();
            next.Remember("CH02-T06");
            if (finish)
            {
                next.Remember("M1-complete");
                next.checkpoint = "CH02-CP04";
            }
            return reality.CommitEvidenceAsync(next, afterFlush);
        }

        public CommitResult TakeSpareBattery(Vector3 feet) =>
            Record("CH01-BATTERY", feet);

        public CommitResult Complete(Vector3 feet) =>
            GateService.G02(State) ? Record("M1-complete", feet, "CH02-CP04") : CommitResult.PadRequired;

        public CommitResult UpdateSettings(VisitSettings options, Vector3 feet)
        {
            var proposed = State.Copy();
            proposed.settings = options;
            return reality.Commit(proposed, feet);
        }

        public CommitResult Record(string id, Vector3 feet, string checkpoint = null, bool geometryChanges = false)
        {
            if (State.Happened(id)) return CommitResult.Applied;
            var proposed = State.Copy();
            proposed.Remember(id);
            if (checkpoint != null) proposed.checkpoint = checkpoint;
            return reality.Commit(proposed, feet, geometryChanges: geometryChanges);
        }
    }
}
