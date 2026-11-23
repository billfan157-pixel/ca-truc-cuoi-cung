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

        public CommitResult ReadLetter(Vector3 feet)
        {
            var next = PrepareReadLetter(extractKeys: true);
            return next == null ? CommitResult.Applied : reality.Commit(next, feet);
        }

        public Task<CommitResult> ReadLetterAsync(Vector3 feet, bool extractKeys = true)
        {
            var next = PrepareReadLetter(extractKeys);
            return next == null ? Task.FromResult(CommitResult.Applied) : reality.CommitEvidenceAsync(next);
        }

        private Investigation PrepareReadLetter(bool extractKeys)
        {
            bool smsAlready = State.Happened("E10a.SMSRead");
            bool envAlready = InventoryService.HasPhysical(State, BackpackCatalog.E10aEnvelope);
            bool keysAlready = !extractKeys ||
                (InventoryService.HasPhysical(State, BackpackCatalog.GateKey) && InventoryService.HasPhysical(State, BackpackCatalog.WingCKey));
            if (smsAlready && envAlready && keysAlready) return null;

            var next = State.Copy();
            next.Remember("E10a.SMSRead");
            if (InventoryService.TryAcquire(next, BackpackCatalog.E10aEnvelope, Reality.R, "LOC01", out var env))
            {
                env.MarkViewed("FLAP");
                env.MarkViewed("BAY_SMS");
                if (extractKeys) env.MarkViewed("KEYS_EXTRACTED");
            }
            if (extractKeys)
            {
                InventoryService.TryAcquire(next, BackpackCatalog.GateKey, Reality.R, "LOC01", out _);
                InventoryService.TryAcquire(next, BackpackCatalog.WingCKey, Reality.R, "LOC01", out _);
            }
            return next;
        }

        public Task<CommitResult> InspectE10aLayerAsync(int tabIndex, string pageKey, bool markSmsRead = false, bool extractKeys = false)
        {
            var next = State.Copy();
            bool changed = false;
            int clamped = Mathf.Clamp(tabIndex, 0, 4);
            if (next.e10aLastTab != clamped) { next.e10aLastTab = clamped; changed = true; }

            bool hadEnv = InventoryService.HasPhysical(next, BackpackCatalog.E10aEnvelope);
            if (InventoryService.TryAcquire(next, BackpackCatalog.E10aEnvelope, Reality.R, "LOC01", out var env))
            {
                if (!hadEnv) changed = true;
                if (!string.IsNullOrEmpty(pageKey) && env.MarkViewed(pageKey)) changed = true;
            }
            if (markSmsRead && !next.Happened("E10a.SMSRead"))
            {
                next.Remember("E10a.SMSRead");
                env?.MarkViewed("BAY_SMS");
                changed = true;
            }
            if (extractKeys)
            {
                bool hadGate = InventoryService.HasPhysical(next, BackpackCatalog.GateKey);
                bool hadWing = InventoryService.HasPhysical(next, BackpackCatalog.WingCKey);
                InventoryService.TryAcquire(next, BackpackCatalog.GateKey, Reality.R, "LOC01", out _);
                InventoryService.TryAcquire(next, BackpackCatalog.WingCKey, Reality.R, "LOC01", out _);
                if (env != null && env.MarkViewed("KEYS_EXTRACTED")) changed = true;
                if (!hadGate || !hadWing) changed = true;
            }
            return changed ? reality.CommitEvidenceAsync(next) : Task.FromResult(CommitResult.Applied);
        }

        public Task<CommitResult> AcquireItemAsync(string itemDefId, string location)
        {
            if (string.IsNullOrEmpty(itemDefId)) return Task.FromResult(CommitResult.PadRequired);
            var existing = InventoryService.FindByDef(State, itemDefId);
            if (existing != null && existing.status != BackpackItemStatus.MemoryRef)
                return Task.FromResult(CommitResult.Applied);
            var next = State.Copy();
            if (!InventoryService.TryAcquire(next, itemDefId, State.layer, location, out _))
                return Task.FromResult(CommitResult.PadRequired);
            return reality.CommitEvidenceAsync(next);
        }

        public Task<CommitResult> EquipItemAsync(string itemDefId)
        {
            var currentEq = InventoryService.GetEquipped(State);
            if (string.IsNullOrEmpty(itemDefId) && currentEq == null)
                return Task.FromResult(CommitResult.Applied);
            if (!string.IsNullOrEmpty(itemDefId) && currentEq != null && currentEq.itemDefId == itemDefId)
                return Task.FromResult(CommitResult.Applied);
            var next = State.Copy();
            if (!InventoryService.TryEquip(next, itemDefId))
                return Task.FromResult(CommitResult.PadRequired);
            return reality.CommitEvidenceAsync(next);
        }

        public Task<CommitResult> PlaceItemAtAnchorAsync(string itemDefId, string anchorId)
        {
            var next = State.Copy();
            if (!InventoryService.TryPlaceAtAnchor(next, itemDefId, anchorId))
                return Task.FromResult(CommitResult.PadRequired);
            return reality.CommitEvidenceAsync(next);
        }

        public Task<CommitResult> RetrieveItemFromAnchorAsync(string anchorId, bool equip = false)
        {
            var next = State.Copy();
            if (!InventoryService.TryRetrieveFromAnchor(next, anchorId, equip))
                return Task.FromResult(CommitResult.PadRequired);
            return reality.CommitEvidenceAsync(next);
        }

        public Task<CommitResult> ReturnPaperToFixtureAsync(string itemDefId)
        {
            var next = State.Copy();
            if (!InventoryService.TryReturnToWorldFixture(next, itemDefId))
                return Task.FromResult(CommitResult.Applied);
            return reality.CommitEvidenceAsync(next);
        }

        public Task<CommitResult> ToggleQuickSlotAsync(string itemDefId)
        {
            var next = State.Copy();
            if (!InventoryService.ToggleQuickSlot(next, itemDefId))
                return Task.FromResult(CommitResult.PadRequired);
            return reality.CommitEvidenceAsync(next);
        }

        public Task<CommitResult> RecordLanNoticedAsync()
        {
            if (State.layer != Reality.B || !State.Happened("CH01-T03") || State.Happened("CH01-LAN-NOTICED"))
                return Task.FromResult(CommitResult.Applied);
            var next = State.Copy();
            next.Remember("CH01-LAN-NOTICED");
            return reality.CommitEvidenceAsync(next, publishGameplay: false);
        }

        public Task<CommitResult> CompleteLanCinematicAsync()
        {
            if (State.layer != Reality.B || !State.Happened("CH01-T03"))
                return Task.FromResult(CommitResult.PadRequired);
            if (State.Happened("CH01-T04") && State.Happened("CH01-LAN-NOTICED"))
                return Task.FromResult(CommitResult.Applied);
            var next = State.Copy();
            next.Remember("CH01-LAN-NOTICED");
            next.Remember("CH01-T04");
            return reality.CommitEvidenceAsync(next);
        }

        public CommitResult OpenGate(Vector3 feet)
        {
            if (!State.Happened("E10a.SMSRead")) return CommitResult.PadRequired;
            var next = PrepareOpenGate();
            return next == null ? CommitResult.Applied : reality.Commit(next, feet, geometryChanges: true);
        }

        public Task<CommitResult> OpenGateAsync(Vector3 feet)
        {
            if (!State.Happened("E10a.SMSRead")) return Task.FromResult(CommitResult.PadRequired);
            var next = PrepareOpenGate();
            return next == null ? Task.FromResult(CommitResult.Applied) : reality.CommitAsync(next, feet, geometryChanges: true);
        }

        private Investigation PrepareOpenGate()
        {
            if (State.Happened("CH01-T02")) return null;
            var next = State.Copy();
            next.Remember("CH01-T02");
            next.checkpoint = "CH01-S1";
            // Ensure gate key is cleanly in bag (stowed after unlocking)
            InventoryService.TryAcquire(next, BackpackCatalog.GateKey, Reality.R, "LOC01", out _);
            if (InventoryService.GetEquipped(next)?.itemDefId == BackpackCatalog.GateKey)
                InventoryService.UnequipAll(next);
            return next;
        }

        public CommitResult AtHub(Vector3 feet, out bool warp)
        {
            warp = false;
            if (!PrepareHub(out var next)) return CommitResult.Applied;
            return reality.Commit(next, feet, RealityTransaction.ReceptionDesk, geometryChanges: true);
        }

        public Task<CommitResult> AtHubAsync(Vector3 feet)
        {
            if (!PrepareHub(out var next)) return Task.FromResult(CommitResult.Applied);
            return reality.CommitAsync(next, feet, RealityTransaction.ReceptionDesk, geometryChanges: true);
        }

        private bool PrepareHub(out Investigation next)
        {
            next = null;
            if (State.chapter == "CH02") return false;
            var candidate = State.Copy();
            if (State.Happened("CH01-T02") && !State.Happened("CH01-T03"))
            {
                candidate.layer = Reality.B;
                candidate.checkpoint = "CH01-CP01";
                candidate.Remember("CH01-T03");
                next = candidate;
                return true;
            }
            if (GateService.G01(State) && !State.Happened("CH01-T08"))
            {
                candidate.layer = Reality.R;
                candidate.checkpoint = "CH01-CP03";
                candidate.Remember("CH01-T08");
                InventoryService.ReconcileLayerBoundToMemory(candidate);
                next = candidate;
                return true;
            }
            if (State.Happened("CH01-T09") && !State.Happened("CH01-T10"))
            {
                candidate.layer = Reality.R;
                candidate.chapter = "CH02";
                candidate.checkpoint = "CH01-CP04";
                candidate.Remember("CH01-T10");
                InventoryService.ReconcileLayerBoundToMemory(candidate);
                next = candidate;
                return true;
            }
            return false;
        }

        public CommitResult Board(Vector3 feet)
        {
            if (!MayTakeBoard) return CommitResult.PadRequired;
            return State.Happened("CH01-T05") ? CommitResult.Applied :
                reality.Commit(PrepareBoard(), feet, geometryChanges: true);
        }

        private bool MayTakeBoard => State.layer == Reality.B && State.chapter == "CH01" && State.Happened("CH01-T03");

        public Task<CommitResult> BoardAsync(Vector3 feet)
        {
            if (!MayTakeBoard) return Task.FromResult(CommitResult.PadRequired);
            return State.Happened("CH01-T05") ? Task.FromResult(CommitResult.Applied) :
                reality.CommitAsync(PrepareBoard(), feet, geometryChanges: true);
        }

        private Investigation PrepareBoard()
        {
            var next = State.Copy();
            next.Remember("CH01-T04");
            next.Remember("CH01-LAN-NOTICED");
            next.Remember("CH01-T05");
            next.checkpoint = "CH01-CP01";
            next.RecordObservation(ObservationRegistry.F101Sheet, Reality.B, "LOC02", ObservationStage.Discovered);
            InventoryService.TryAcquire(next, BackpackCatalog.BoardAssignment, Reality.B, "LOC02", out _);
            return next;
        }

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
            return reality.Commit(next, feet, RealityTransaction.WardClipboard, geometryChanges: true);
        }

        public Task<CommitResult> SwitchIn101Async(Vector3 feet)
        {
            if (!PrepareSwitch101(out var next)) return Task.FromResult(CommitResult.PadRequired);
            return reality.CommitAsync(next, feet, RealityTransaction.WardClipboard, geometryChanges: true);
        }

        private bool PrepareSwitch101(out Investigation next)
        {
            next = null;
            if (State.chapter != "CH02") return false;
            next = State.Copy();
            next.layer = State.layer == Reality.R ? Reality.B : Reality.R;
            next.checkpoint = GateService.G02(State) ? "CH02-CP03" : "CH02-CP02";
            if (next.layer != Reality.B)
                InventoryService.ReconcileLayerBoundToMemory(next);
            return true;
        }

        public bool CanRevise => State.Happened("CH01-T05") &&
            (State.chapter == "CH02" || State.layer == Reality.B && !State.Happened("CH01-T08"));

        public CommitResult Observe(string id, Vector3 feet, string location = null, ObservationStage stage = ObservationStage.Examined)
        {
            var result = PrepareObservation(id, location, stage, out var next);
            return next == null ? result : reality.Commit(next, feet);
        }

        public Task<CommitResult> ObserveAsync(string id, string location = null, ObservationStage stage = ObservationStage.Examined,
            string detail = null)
        {
            var result = PrepareObservation(id, location, stage, out var next);
            if (next == null)
                return result == CommitResult.Applied && detail != null
                    ? MarkObservationDetailAsync(id, detail) : Task.FromResult(result);
            if (detail != null) next.MarkObservationDetail(id, detail, false);
            return reality.CommitEvidenceAsync(next);
        }

        public Task<CommitResult> MarkObservationDetailAsync(string id, string detailId, bool flippedBack = false)
        {
            if (string.IsNullOrEmpty(id) || !State.Observed(id))
                return Task.FromResult(CommitResult.PadRequired);
            var existing = State.FindObservation(id);
            if (existing == null)
                return Task.FromResult(CommitResult.Applied);
            if ((!flippedBack || existing.flippedBack) && (string.IsNullOrEmpty(detailId) || existing.HasDetail(detailId)))
                return Task.FromResult(CommitResult.Applied);
            var next = State.Copy();
            if (!next.MarkObservationDetail(id, detailId, flippedBack))
                return Task.FromResult(CommitResult.Applied);
            return reality.CommitEvidenceAsync(next);
        }

        internal Task<CommitResult> WriteJournalNoteAsync(int index, JournalNote note)
        {
            if (index < -1 || index >= State.journalNotes.Count ||
                index == -1 && (note == null || State.journalNotes.Count >= JournalNote.MaxNotes))
                return Task.FromResult(CommitResult.PadRequired);
            if (note != null && !note.ValidFor(State))
                return Task.FromResult(CommitResult.PadRequired);
            var next = State.Copy();
            if (note == null) next.journalNotes.RemoveAt(index);
            else if (index == -1) next.journalNotes.Add(note);
            else next.journalNotes[index] = note;
            return reality.CommitEvidenceAsync(next, publishGameplay: false);
        }

        internal Task<CommitResult> StorePropPoseAsync(string id, float openness)
        {
            if (!ScenePropPose.Known(id) || State.layer == Reality.M || !float.IsFinite(openness))
                return Task.FromResult(CommitResult.PadRequired);
            var next = State.Copy();
            var pose = next.FindPropPose(id, next.layer);
            if (pose == null)
            {
                pose = new ScenePropPose { id = id, layer = next.layer };
                next.sceneProps.Add(pose);
            }
            pose.openness = Mathf.Clamp01(openness);
            return reality.CommitEvidenceAsync(next, publishGameplay: false);
        }

        internal Task<CommitResult> ObserveF101ConsequenceAsync(int revision)
        {
            if (State.layer != Reality.B || revision < 1 || revision != State.f101.revision)
                return Task.FromResult(CommitResult.PadRequired);
            if (State.f101History[revision - 1].consequenceObserved)
                return Task.FromResult(CommitResult.Applied);
            var next = State.Copy();
            next.f101History[revision - 1].consequenceObserved = true;
            return reality.CommitEvidenceAsync(next, publishGameplay: false);
        }

        private CommitResult PrepareObservation(string id, string location, ObservationStage stage, out Investigation next)
        {
            next = null;
            var spec = ObservationRegistry.Get(id);
            if (spec == null) return CommitResult.PadRequired;
            string loc = !string.IsNullOrEmpty(location)
                ? location
                : (spec.AllowedLocations != null && spec.AllowedLocations.Length > 0 ? spec.AllowedLocations[0] : "LOC03");
            if (!ObservationRegistry.ValidProvenance(id, State.layer, loc, State.chapter))
                return CommitResult.PadRequired;
            var existing = State.FindObservation(id);
            if (State.Observed(id) && existing != null && existing.stage >= stage)
                return CommitResult.Applied;
            next = State.Copy();
            next.RecordObservation(id, State.layer, loc, stage);
            if (id == ObservationRegistry.StoppedClock0317)
                next.Remember("FS01");
            return CommitResult.Applied;
        }

        internal Task<CommitResult> ToggleF101MarkAsync(string id, string hotspot)
        {
            if (!CanRevise || !F101Annotations.IsHotspot(id, hotspot) || !HypothesisService.HasExaminedInLayerB(State, id))
                return Task.FromResult(CommitResult.PadRequired);
            var next = State.Copy();
            var notes = next.f101Annotations;
            bool removing = notes.MarkedHotspot(id) == hotspot;
            notes.marks.RemoveAll(mark => mark.observationId == id);
            if (!removing) notes.marks.Add(new EvidenceAnnotation(id, hotspot));
            if (notes.linkFrom.observationId == id)
            {
                notes.linkFrom = default;
                notes.linkTo = default;
            }
            return reality.CommitEvidenceAsync(next, publishGameplay: false);
        }

        internal Task<CommitResult> SetF101LinkAsync(EvidenceAnnotation first, EvidenceAnnotation second)
        {
            if (!CanRevise) return Task.FromResult(CommitResult.PadRequired);
            if (first.observationId == ObservationRegistry.Bed4Traces)
                (first, second) = (second, first);
            var next = State.Copy();
            var notes = next.f101Annotations;
            notes.linkFrom = first;
            notes.linkTo = second;
            if (!notes.ValidFor(next)) return Task.FromResult(CommitResult.PadRequired);
            return reality.CommitEvidenceAsync(next, publishGameplay: false);
        }

        public CommitResult SubmitF101(FormStatus decision, DeductionSubmission submission, Vector3 feet, out HypothesisEvaluation evaluation)
        {
            var status = PrepareSubmitF101(decision, submission, feet, out var next, out evaluation, out bool geometryChanged);
            if (next == null) return status;
            if (!evaluation.Accepted)
            {
                // Persist failed reasoning attempt for progressive hints, while still returning rejection status!
                reality.Commit(next, feet);
                return CommitResult.PadRequired;
            }
            return geometryChanged
                ? reality.Commit(next, feet, RealityTransaction.WardClipboard, geometryChanges: true)
                : reality.Commit(next, feet);
        }

        public async Task<(CommitResult Result, HypothesisEvaluation Evaluation)> SubmitF101DetailedAsync(
            FormStatus decision,
            DeductionSubmission submission,
            Vector3 feet)
        {
            var status = PrepareSubmitF101(decision, submission, feet, out var next, out var evaluation, out bool geometryChanged);
            if (next == null) return (status, evaluation);
            if (!evaluation.Accepted)
            {
                await reality.CommitEvidenceAsync(next);
                return (CommitResult.PadRequired, evaluation);
            }
            var result = geometryChanged
                ? await reality.CommitAsync(next, feet, RealityTransaction.WardClipboard, geometryChanges: true)
                : await reality.CommitEvidenceAsync(next);
            return (result, evaluation);
        }

        public async Task<CommitResult> SubmitF101Async(FormStatus decision, DeductionSubmission submission, Vector3 feet)
        {
            var pair = await SubmitF101DetailedAsync(decision, submission, feet);
            return pair.Result;
        }

        private CommitResult PrepareSubmitF101(
            FormStatus decision,
            DeductionSubmission submission,
            Vector3 feet,
            out Investigation next,
            out HypothesisEvaluation evaluation,
            out bool geometryChanged)
        {
            next = null;
            geometryChanged = false;
            evaluation = HypothesisEvaluation.Reject(HypothesisOutcome.ContradictsDecision, "Chưa thể ký phiếu", "Ký trên bảng kẹp trong phòng 101.");
            if (!CanRevise || !reality.AtRequiredPlace(feet, RealityTransaction.WardClipboard))
                return CommitResult.PadRequired;

            var candidate = State.Copy();
            if (!FormService.ApplyWithReasoning(candidate, decision, submission, out evaluation))
            {
                // Persist failed F101 refutation attempt cleanly so contextual hints escalate after 2 failed tries!
                if (decision == FormStatus.Refuted)
                {
                    PuzzleService.RecordAttempt(candidate, HypothesisService.F101PuzzleId, false);
                    next = candidate;
                    geometryChanged = false;
                }
                return CommitResult.PadRequired;
            }

            if (candidate.f101.revision == State.f101.revision)
                return CommitResult.Applied;

            candidate.Remember("CH01-T07");
            if (candidate.chapter == "CH01")
                candidate.checkpoint = "CH01-CP02";
            next = candidate;
            geometryChanged = true;
            return CommitResult.Applied;
        }

        public Task<CommitResult> RecordDreadEventAsync(string dreadId)
        {
            if (string.IsNullOrEmpty(dreadId) || State.HasDreadEvent(dreadId))
                return Task.FromResult(CommitResult.Applied);
            var next = State.Copy();
            if (!next.MarkDreadEvent(dreadId))
                return Task.FromResult(CommitResult.Applied);
            return reality.CommitEvidenceAsync(next);
        }

        public CommitResult RecordDreadEvent(string dreadId, Vector3 feet)
        {
            if (string.IsNullOrEmpty(dreadId) || State.HasDreadEvent(dreadId))
                return CommitResult.Applied;
            var next = State.Copy();
            if (!next.MarkDreadEvent(dreadId))
                return CommitResult.Applied;
            return reality.Commit(next, feet);
        }

        public CommitResult CrossCheck0317Exception(Vector3 feet, TimelineConnection link)
        {
            var result = PrepareCrossCheck0317(link, out var next);
            return next == null ? result : reality.Commit(next, feet);
        }

        public Task<CommitResult> CrossCheck0317ExceptionAsync(TimelineConnection link)
        {
            var result = PrepareCrossCheck0317(link, out var next);
            return next == null ? Task.FromResult(result) : reality.CommitEvidenceAsync(next);
        }

        private CommitResult PrepareCrossCheck0317(TimelineConnection link, out Investigation next)
        {
            next = null;
            if (CorroborationService.IsCorroborated(State))
                return CommitResult.Applied;
            if (!CorroborationService.CanCorroborate0317(State) || !CorroborationService.Supported0317(State, link))
                return CommitResult.PadRequired;
            next = State.Copy();
            next.Remember(CorroborationService.Exception0317Event);
            next.exception0317Link = link;
            foreach (string ctxId in new[] { link.left, link.right, link.context })
            {
                var rec = next.FindObservation(ctxId);
                if (rec != null && rec.stage < ObservationStage.Corroborated)
                    rec.stage = ObservationStage.Corroborated;
            }
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
            InventoryService.SyncFromEvidence(proposal, id, "LOC03");
            next = proposal;
            return CommitResult.Applied;
        }

        public CommitResult VerifyAn(Vector3 feet)
        {
            var result = PrepareVerifyAn(out var next);
            return next == null ? result : reality.Commit(next, feet, RealityTransaction.WardClipboard, geometryChanges: true);
        }

        public Task<CommitResult> VerifyAnAsync(Vector3 feet)
        {
            var result = PrepareVerifyAn(out var next);
            return next == null ? Task.FromResult(result) :
                reality.CommitAsync(next, feet, RealityTransaction.WardClipboard, geometryChanges: true);
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

        public Task<CommitResult> BeginCh03Async(Vector3 feet)
        {
            if (!PrepareBeginCh03(out var next)) return Task.FromResult(CommitResult.PadRequired);
            return reality.CommitAsync(next, feet, geometryChanges: true);
        }

        private bool PrepareBeginCh03(out Investigation next)
        {
            next = null;
            if (!GateService.G02(State)) return false;
            if (State.chapter != "CH02") return false;
            next = State.Copy();
            next.chapter = "CH03";
            next.layer = Reality.R;
            next.checkpoint = "CH03-CP01";
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
            if (State.chapter is not ("CH03" or "CH04") || State.layer != Reality.R) return CommitResult.PadRequired;
            if (State.Has(id)) return CommitResult.Applied;
            var proposal = State.Copy();
            if (!EvidenceRegistry.Grant(proposal, id, Reality.R, location))
                return CommitResult.PadRequired;
            InventoryService.SyncFromEvidence(proposal, id, location);
            if (id == EvidenceRegistry.PatientRegister)
                proposal.Remember("CH03-T02");
            else if (id == EvidenceRegistry.C12UnnamedBox)
            {
                // Canon CH03-Q03: Confirm C12 existence without ever fabricating a name.
                proposal.ConfirmPatientExistence("C12");
                proposal.Remember("CH03-T05");
                proposal.Remember("FS19");
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
            if (!reality.OnPad(feet, "AN03")) return Task.FromResult(CommitResult.PadRequired);
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
            if (State.chapter is not ("CH03" or "CH04") || State.layer != Reality.R || !State.PatientConfirmed("C02") ||
                !State.Has(EvidenceRegistry.C02DaughterLetter) || !State.Has(EvidenceRegistry.C02YarnBag) ||
                !State.Has(EvidenceRegistry.C02CareNote))
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
            if (State.chapter != "CH03" || !GateService.G03(State)) return false;
            next = State.Copy();
            if (State.layer == Reality.R)
            {
                next.layer = Reality.M;
                next.checkpoint = GateService.G03(State) ? "CH03-CP03" : "CH03-CP02";
                next.Remember("CH03-T04");
                InventoryService.RecordMemoryRef(next, BackpackCatalog.MemVignetteC02, "LOC07");
            }
            else if (State.layer == Reality.M)
            {
                next.layer = Reality.R;
                next.checkpoint = GateService.G03(State) ? "CH03-CP03" : "CH03-CP02";
            }
            else return false;
            return true;
        }

        public CommitResult CompleteCh03(Vector3 feet)
        {
            if (!GateService.G03(State) || State.layer != Reality.R || !reality.OnPad(feet, "AP07"))
                return CommitResult.PadRequired;
            return Record("CH03-T07", feet, "CH03-CP04");
        }

        public Task<CommitResult> CompleteCh03Async(Vector3 feet)
        {
            if (!GateService.G03(State) || State.layer != Reality.R || !reality.OnPad(feet, "AP07"))
                return Task.FromResult(CommitResult.PadRequired);
            return RecordAsync("CH03-T07", feet, "CH03-CP04");
        }

        public Task<CommitResult> BeginCh04Async()
        {
            if (State.chapter != "CH03" || !GateService.G03(State) || !State.Happened("CH03-T07") ||
                !State.Happened("FS19"))
                return Task.FromResult(CommitResult.PadRequired);
            var next = State.Copy();
            next.chapter = "CH04";
            next.checkpoint = "CH04-CP01";
            next.Remember("CH04-T01");
            return reality.CommitEvidenceAsync(next);
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
