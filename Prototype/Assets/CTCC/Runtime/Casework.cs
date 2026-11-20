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

        public Task<CommitResult> ReadLetterAsync(Vector3 feet) => RecordAsync("E10a.SMSRead", feet);

        public CommitResult OpenGate(Vector3 feet)
        {
            if (!State.Happened("E10a.SMSRead")) return CommitResult.PadRequired;
            return Record("CH01-T02", feet, "CH01-S1", true);
        }

        public Task<CommitResult> OpenGateAsync(Vector3 feet) => State.Happened("E10a.SMSRead")
            ? RecordAsync("CH01-T02", feet, "CH01-S1", true) : Task.FromResult(CommitResult.PadRequired);

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
            MayTakeBoard
                ? Record("CH01-T05", feet, "CH01-CP01", true) : CommitResult.PadRequired;

        private bool MayTakeBoard => State.layer == Reality.B && State.chapter == "CH01" && State.Happened("CH01-T03");

        public Task<CommitResult> BoardAsync(Vector3 feet) => MayTakeBoard
            ? RecordAsync("CH01-T05", feet, "CH01-CP01", true) : Task.FromResult(CommitResult.PadRequired);

        public CommitResult EnterRoom(Vector3 feet) =>
            MayEnterRoom
                ? Record("CH01-T06", feet, "CH01-CP02") : CommitResult.Applied;

        private bool MayEnterRoom => State.chapter == "CH01" && State.Happened("CH01-T05");

        public Task<CommitResult> EnterRoomAsync(Vector3 feet) => MayEnterRoom
            ? RecordAsync("CH01-T06", feet, "CH01-CP02") : Task.FromResult(CommitResult.Applied);

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
            var result = PrepareObservation(id, out var next);
            return next == null ? result : reality.Commit(next, feet);
        }

        public Task<CommitResult> ObserveAsync(string id)
        {
            var result = PrepareObservation(id, out var next);
            return next == null ? Task.FromResult(result) : reality.CommitEvidenceAsync(next);
        }

        private CommitResult PrepareObservation(string id, out Investigation next)
        {
            next = null;
            if ((id == "OBS101A" || id == "OBS101B") && State.layer != Reality.B ||
                (id == "LIST" || id == "SOCKETS" || id == "WHEELS") && (State.layer != Reality.R || State.chapter != "CH02"))
                return CommitResult.PadRequired;
            if (State.Observed(id)) return CommitResult.Applied;
            next = State.Copy();
            next.Observe(id);
            return CommitResult.Applied;
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
            var result = PreparePz01(out var next);
            return next == null ? result : reality.Commit(next, feet, geometryChanges: true);
        }

        public Task<CommitResult> SolvePz01Async(Vector3 feet)
        {
            var result = PreparePz01(out var next);
            return next == null ? Task.FromResult(result) : reality.CommitAsync(next, feet, geometryChanges: true);
        }

        private CommitResult PreparePz01(out Investigation next)
        {
            next = null;
            if (State.layer != Reality.R || State.chapter != "CH02" || MissingPhysicalObservations() != null)
                return CommitResult.PadRequired;
            if (State.Has("E12")) return CommitResult.Applied;
            next = State.Copy();
            EvidenceRegistry.Grant(next, "E12", Reality.R, "LOC03");
            next.checkpoint = "CH02-CP02";
            next.Remember("CH02-T03");
            return CommitResult.Applied;
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
            var result = PrepareVerifyAn(out var next);
            return next == null ? result : reality.Commit(next, feet, "AP04", false, true);
        }

        public Task<CommitResult> VerifyAnAsync(Vector3 feet)
        {
            var result = PrepareVerifyAn(out var next);
            return next == null ? Task.FromResult(result) : reality.CommitAsync(next, feet, "AP04", false, true);
        }

        private CommitResult PrepareVerifyAn(out Investigation next)
        {
            next = null;
            if (!State.Has("E12") || !EvidenceRegistry.BundleComplete(State) || State.layer != Reality.R)
                return CommitResult.PadRequired;
            if (GateService.G02(State)) return CommitResult.Applied;
            next = State.Copy();
            next.VerifyPatientIdentity("C01", "Bé An");
            next.Remember("CH02-T05");
            next.Remember("G02.opened");
            next.checkpoint = "CH02-CP03";
            return CommitResult.Applied;
        }

        // ------------------------------------------------------------------
        // M3.0 REPRESENTATIVE CH03 ('NHỮNG CÁI TÊN BỊ XÓA') CASEWORK
        // ------------------------------------------------------------------

        public CommitResult BeginCh03(Vector3 feet)
        {
            if (!PrepareBeginCh03(out var next)) return CommitResult.PadRequired;
            return reality.Commit(next, feet, warp: true, geometryChanges: true);
        }

        public Task<CommitResult> BeginCh03Async(Vector3 feet)
        {
            if (!PrepareBeginCh03(out var next)) return Task.FromResult(CommitResult.PadRequired);
            return reality.CommitAsync(next, feet, warp: true, geometryChanges: true);
        }

        private bool PrepareBeginCh03(out Investigation next)
        {
            next = null;
            if (!GateService.G02(State)) return false;
            if (State.chapter == "CH03") return false;
            next = State.Copy();
            next.chapter = "CH03";
            next.layer = Reality.R;
            next.checkpoint = "CH03-CP01";
            next.Remember("CH03-T01");
            return true;
        }

        public CommitResult TakeCh03Evidence(string id, string location, Vector3 feet)
        {
            var result = PrepareCh03Evidence(id, location, out var next);
            return next == null ? result : reality.Commit(next, feet);
        }

        public Task<CommitResult> TakeCh03EvidenceAsync(string id, string location)
        {
            var result = PrepareCh03Evidence(id, location, out var next);
            return next == null ? Task.FromResult(result) : reality.CommitEvidenceAsync(next);
        }

        private CommitResult PrepareCh03Evidence(string id, string location, out Investigation next)
        {
            next = null;
            if (State.chapter != "CH03" || State.layer != Reality.R) return CommitResult.PadRequired;
            if (State.Has(id)) return CommitResult.Applied;
            var proposal = State.Copy();
            if (!EvidenceRegistry.Grant(proposal, id, Reality.R, location))
                return CommitResult.PadRequired;
            if (id == EvidenceRegistry.PatientRegister)
                proposal.Remember("CH03-T02");
            else if (id == EvidenceRegistry.C12UnnamedBox)
            {
                // Canon CH03-Q03: Confirm C12 existence without ever fabricating a name.
                proposal.ConfirmPatientExistence("C12");
                proposal.Remember("CH03-T04");
            }
            next = proposal;
            return CommitResult.Applied;
        }

        public CommitResult AttemptCh03Pu01(string selectedCode, bool pairedNoteAndBag, Vector3 feet)
        {
            var result = PrepareCh03Pu01(selectedCode, pairedNoteAndBag, out var next, out bool solved);
            return next == null ? result : reality.Commit(next, feet, "AN03", false, solved);
        }

        public Task<CommitResult> AttemptCh03Pu01Async(string selectedCode, bool pairedNoteAndBag, Vector3 feet)
        {
            var result = PrepareCh03Pu01(selectedCode, pairedNoteAndBag, out var next, out bool solved);
            if (next == null) return Task.FromResult(result);
            return solved
                ? reality.CommitAsync(next, feet, "AN03", false, true)
                : reality.CommitEvidenceAsync(next);
        }

        private CommitResult PrepareCh03Pu01(string selectedCode, bool pairedNoteAndBag, out Investigation next, out bool solved)
        {
            next = null;
            solved = false;
            if (State.chapter != "CH03" || State.layer != Reality.R ||
                !State.Has(EvidenceRegistry.PatientRegister) ||
                !State.Has(EvidenceRegistry.C02CareNote) ||
                !State.Has(EvidenceRegistry.C02YarnBag))
                return CommitResult.PadRequired;
            if (GateService.G03(State)) return CommitResult.Applied;

            next = State.Copy();
            bool correct = string.Equals(selectedCode, "C02", StringComparison.OrdinalIgnoreCase) && pairedNoteAndBag;
            PuzzleService.RecordAttempt(next, "CH03-PU01", correct);
            if (correct)
            {
                solved = true;
                next.ConfirmPatientExistence("C02");
                next.Remember("CH03-T03");
                next.Remember("G03.opened");
                next.checkpoint = "CH03-CP03";
            }
            return CommitResult.Applied;
        }

        public CommitResult SolveCh03Pu02OptionalIdentity(Vector3 feet)
        {
            var result = PrepareCh03Pu02(out var next);
            return next == null ? result : reality.Commit(next, feet);
        }

        public Task<CommitResult> SolveCh03Pu02OptionalIdentityAsync()
        {
            var result = PrepareCh03Pu02(out var next);
            return next == null ? Task.FromResult(result) : reality.CommitEvidenceAsync(next);
        }

        private CommitResult PrepareCh03Pu02(out Investigation next)
        {
            next = null;
            if (State.chapter != "CH03" || !State.Has(EvidenceRegistry.C02DaughterLetter) || !State.Has(EvidenceRegistry.C02YarnBag))
                return CommitResult.PadRequired;
            if (State.PatientIdentityVerified("C02")) return CommitResult.Applied;
            next = State.Copy();
            PuzzleService.RecordAttempt(next, "CH03-PU02", true);
            next.VerifyPatientIdentity("C02", "Bà Lành");
            return CommitResult.Applied;
        }

        public CommitResult SwitchMemoryVignetteAn03(Vector3 feet)
        {
            if (!PrepareMemoryVignetteAn03(out var next)) return CommitResult.PadRequired;
            return reality.Commit(next, feet, "AN03", true, true);
        }

        public Task<CommitResult> SwitchMemoryVignetteAn03Async(Vector3 feet)
        {
            if (!PrepareMemoryVignetteAn03(out var next)) return Task.FromResult(CommitResult.PadRequired);
            return reality.CommitAsync(next, feet, "AN03", true, true);
        }

        private bool PrepareMemoryVignetteAn03(out Investigation next)
        {
            next = null;
            if (State.chapter != "CH03" || !State.Has(EvidenceRegistry.C02YarnBag)) return false;
            next = State.Copy();
            if (State.layer == Reality.R)
            {
                next.layer = Reality.M;
                next.checkpoint = GateService.G03(State) ? "CH03-CP03" : "CH03-CP02";
                next.Remember("CH03-M01");
            }
            else if (State.layer == Reality.M)
            {
                next.layer = Reality.R;
                next.checkpoint = GateService.G03(State) ? "CH03-CP03" : "CH03-CP02";
                next.Remember("CH03-M02");
            }
            else return false;
            return true;
        }

        public CommitResult CompleteCh03(Vector3 feet)
        {
            if (!GateService.G03(State) || State.layer != Reality.R || !reality.OnPad(feet, "AP07"))
                return CommitResult.PadRequired;
            return Record("M3-CH03-complete", feet, "CH03-CP04");
        }

        public Task<CommitResult> CompleteCh03Async(Vector3 feet)
        {
            if (!GateService.G03(State) || State.layer != Reality.R || !reality.OnPad(feet, "AP07"))
                return Task.FromResult(CommitResult.PadRequired);
            return RecordAsync("M3-CH03-complete", feet, "CH03-CP04");
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
            var proposed = PrepareRecord(id, checkpoint);
            return proposed == null ? CommitResult.Applied : reality.Commit(proposed, feet, geometryChanges: geometryChanges);
        }

        public Task<CommitResult> RecordAsync(string id, Vector3 feet, string checkpoint = null, bool geometryChanges = false)
        {
            var proposed = PrepareRecord(id, checkpoint);
            if (proposed == null) return Task.FromResult(CommitResult.Applied);
            return geometryChanges
                ? reality.CommitAsync(proposed, feet, geometryChanges: true)
                : reality.CommitEvidenceAsync(proposed);
        }

        private Investigation PrepareRecord(string id, string checkpoint)
        {
            if (State.Happened(id)) return null;
            var proposed = State.Copy();
            proposed.Remember(id);
            if (checkpoint != null) proposed.checkpoint = checkpoint;
            return proposed;
        }
    }
}
