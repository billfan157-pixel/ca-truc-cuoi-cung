using System;
using System.Threading.Tasks;
using UnityEngine;

namespace CTCC
{
    public sealed class Casework
    {
        private readonly RealityTransaction reality;
        public Investigation State => reality.Current;
        public Casework(RealityTransaction reality) { this.reality=reality; }

        public CommitResult Begin(Vector3 feet,VisitSettings options)
        {
            var proposed=Investigation.New();
            proposed.settings=options;
            return reality.Commit(proposed,feet,warp:true,geometryChanges:true);
        }

        public CommitResult ReadLetter(Vector3 feet) =>
            Record("E10a.SMSRead",feet);

        public CommitResult OpenGate(Vector3 feet)
        {
            if (!State.Happened("E10a.SMSRead")) return CommitResult.PadRequired;
            return Record("CH01-T02",feet,"CH01-S1",true);
        }

        public CommitResult AtHub(Vector3 feet, out bool warp)
        {
            warp=false;
            if (State.chapter=="CH02") return CommitResult.Applied;
            var next=State.Copy();
            if (State.Happened("CH01-T02") && !State.Happened("CH01-T03"))
            {
                next.layer=Reality.B;
                next.checkpoint="CH01-CP01";
                next.Remember("CH01-T03");
                next.Remember("CH01-T04");
                warp=true;
                return reality.Commit(next,feet,"AP02",true,true);
            }
            if (GateService.G01(State) && !State.Happened("CH01-T08"))
            {
                next.layer=Reality.R;
                next.checkpoint="CH01-CP03";
                next.Remember("CH01-T08");
                warp=true;
                return reality.Commit(next,feet,"AP02",true,true);
            }
            if (State.Happened("CH01-T09") && !State.Happened("CH01-T10"))
            {
                next.layer=Reality.R;
                next.chapter="CH02";
                next.checkpoint="CH01-CP04";
                next.Remember("CH01-T10");
                warp=true;
                return reality.Commit(next,feet,"AP02",true,true);
            }
            return CommitResult.Applied;
        }

        public CommitResult Board(Vector3 feet) =>
            State.layer == Reality.B && State.chapter=="CH01" && State.Happened("CH01-T03")
                ? Record("CH01-T05",feet,"CH01-CP01",true) : CommitResult.PadRequired;

        public CommitResult EnterRoom(Vector3 feet) =>
            State.chapter=="CH01" && State.Happened("CH01-T05")
                ? Record("CH01-T06",feet,"CH01-CP02") : CommitResult.Applied;

        public CommitResult OutsideAttempt(Vector3 feet)
        {
            if (State.chapter!="CH01" || !State.Happened("CH01-T08") || State.Happened("CH01-T09"))
                return CommitResult.PadRequired;
            var next=State.Copy();
            next.checkpoint="CH01-S7";
            next.Remember("CH01-T09");
            return reality.Commit(next,feet,warp:true,geometryChanges:true);
        }

        public CommitResult SwitchIn101(Vector3 feet)
        {
            if (State.chapter!="CH02")
                return CommitResult.PadRequired;
            var next=State.Copy();
            next.layer=State.layer==Reality.R ? Reality.B : Reality.R;
            next.checkpoint=GateService.G02(State) ? "CH02-CP03" : "CH02-CP02";
            return reality.Commit(next,feet,"AP04",true,true);
        }

        public bool CanRevise => State.Happened("CH01-T05") &&
            (State.chapter=="CH02" || State.layer==Reality.B && !State.Happened("CH01-T08"));

        public CommitResult Observe(string id,Vector3 feet)
        {
            if ((id=="OBS101A" || id=="OBS101B") && State.layer!=Reality.B ||
                (id=="LIST" || id=="SOCKETS" || id=="WHEELS") && (State.layer!=Reality.R || State.chapter!="CH02"))
                return CommitResult.PadRequired;
            var next=State.Copy();
            return next.Observe(id) ? reality.Commit(next,feet) : CommitResult.Applied;
        }

        public CommitResult Submit(FormStatus form,bool pairedObservations,Vector3 feet)
        {
            if (!CanRevise || form==FormStatus.Refuted && !pairedObservations)
                return CommitResult.PadRequired;
            if (!reality.OnPad(feet,"AP04")) return CommitResult.PadRequired;
            var next=State.Copy();
            if (!FormService.Apply(next,form))
                return CommitResult.PadRequired;
            if (next.f101.revision==State.f101.revision)
                return CommitResult.Applied;
            next.Remember("CH01-T07");
            if (next.chapter=="CH01")
                next.checkpoint="CH01-CP02";
            return reality.Commit(next,feet,"AP04",false,true);
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
            if (State.layer!=Reality.R || State.chapter!="CH02" || MissingPhysicalObservations()!=null)
                return CommitResult.PadRequired;
            if (State.Has("E12")) return CommitResult.Applied;
            var next=State.Copy();
            EvidenceRegistry.Grant(next,"E12",Reality.R,"LOC03");
            next.checkpoint="CH02-CP02";
            next.Remember("CH02-T03");
            return reality.Commit(next,feet,geometryChanges:true);
        }

        public CommitResult Take(string id,Vector3 feet)
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
            if (!State.Has("E12") || !EvidenceRegistry.BundleComplete(State) || State.layer!=Reality.R)
                return CommitResult.PadRequired;
            if (GateService.G02(State)) return CommitResult.Applied;
            var next=State.Copy();
            next.Remember("C01.verified");
            next.Remember("CH02-T05");
            next.Remember("G02.opened");
            next.checkpoint="CH02-CP03";
            return reality.Commit(next,feet,"AP04",false,true);
        }

        public CommitResult Complete(Vector3 feet) =>
            GateService.G02(State) ? Record("M1-complete",feet,"CH02-CP04") : CommitResult.PadRequired;

        public CommitResult UpdateSettings(VisitSettings options,Vector3 feet)
        {
            var proposed=State.Copy();
            proposed.settings=options;
            return reality.Commit(proposed,feet);
        }

        public CommitResult Record(string id,Vector3 feet,string checkpoint=null,bool geometryChanges=false)
        {
            if (State.Happened(id)) return CommitResult.Applied;
            var proposed=State.Copy();
            proposed.Remember(id);
            if (checkpoint!=null) proposed.checkpoint=checkpoint;
            return reality.Commit(proposed,feet,geometryChanges:geometryChanges);
        }
    }
}
