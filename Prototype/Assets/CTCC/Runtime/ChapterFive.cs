using System;
using System.Threading.Tasks;
using UnityEngine;

namespace CTCC
{
    /// <summary>
    /// Canonical CH05 state-transition and contract service (Core v3.3.1 — TASK013-A/B/C).
    /// All commits route strictly through the existing single RealityTransaction & Save Schema v7.
    /// Enforces:
    /// - Entry gate G04 + CH04-T06 and pre-memory CP01 commit in R before entering M.
    /// - Sequential 3-pair evacuation (6/6 patients invariant), segment recovery, skip parity, and zero M export.
    /// - Dual architectural proof for H03/G05: PZ07 2-marker overlay (E05) PLUS physical R observation (E05b).
    /// - Optional LOC09 valve observation (DEC-V32-03) strictly non-blocking and capped at corroborated suspicion.
    /// - Hard CH05->CH06 boundary lock (G06, E01, E06, E10b, E08 remain closed).
    /// </summary>
    internal static class ChapterFive
    {
        internal const string PuzzleEvac = "CH05-PU01";
        internal const string PuzzleOverlay = "CH05-PU02";

        internal const string CheckpointEntry = "CH05-CP01";
        internal const string CheckpointAfterMemory = "CH05-CP02";
        internal const string CheckpointService = "CH05-CP02B";
        internal const string CheckpointHandoff = "CH05-CP03";

        internal const string EvEntry = "CH05-T01";
        internal const string EvPair1 = "CH05-T02";
        internal const string EvPair2 = "CH05-T03";
        internal const string EvPair3 = "CH05-T04";
        internal const string EvReturnR = "CH05-T05";
        internal const string EvBlueprintFound = "CH05-T06";
        internal const string EvSealedWall = "CH05-T07";
        internal const string EvHandoff = "CH05-T08";

        internal const string FlagPair1 = "CH05.Pair1";
        internal const string FlagPair2 = "CH05.Pair2";
        internal const string FlagPair3 = "CH05.Pair3";
        internal const string FlagEvacSkipped = "CH05.EvacSkipped";
        internal const string FlagFs14WingCKey = "FS14";
        internal const string FlagFs22BioBroadcast = "FS22";
        internal const string FlagOverlayAligned = "PZ07.Aligned";
        internal const string FlagValveSabotage = "ValveSabotageFound";
        internal const string FlagH03Verified = "H03.verified";
        internal const string FlagG05Opened = "G05.opened";

        internal static bool CanEnter(Investigation state) =>
            state != null && state.layer == Reality.R &&
            state.chapter is ("CH04" or "CH05") &&
            GateService.G04(state) && state.Happened("CH04-T06");

        internal static int RescuedCount(Investigation state)
        {
            if (state == null) return 0;
            int total = 0;
            if (state.Happened(EvPair1)) total += 2;
            if (state.Happened(EvPair2)) total += 2;
            if (state.Happened(EvPair3)) total += 2;
            return total;
        }

        internal static int ActivePairIndex(Investigation state)
        {
            if (state == null || !state.Happened(EvPair1)) return 1;
            if (!state.Happened(EvPair2)) return 2;
            if (!state.Happened(EvPair3)) return 3;
            return 4;
        }

        internal static bool CanVerifyH03(Investigation state) =>
            state != null && state.chapter == "CH05" && state.layer == Reality.R &&
            state.Has(EvidenceRegistry.AsBuilt2004) &&
            state.Happened(FlagOverlayAligned) &&
            state.Has(EvidenceRegistry.SealedWallPhoto);

        /// <summary>
        /// Step 1 of B01: Commit CH05-CP01 durably in Layer R at AN05 (AP10) BEFORE entering Layer M.
        /// Preserves valid CH04 power mask, all prior gates (G01-G04), and all evidence without duplication.
        /// </summary>
        internal static Task<CommitResult> CommitEntryCheckpointAsync(RealityTransaction transaction, Vector3 feet)
        {
            var state = transaction.Current;
            if (!CanEnter(state))
                return Task.FromResult(CommitResult.PadRequired);
            if (state.chapter == "CH05" && state.checkpoint == CheckpointEntry && state.layer == Reality.R)
                return Task.FromResult(CommitResult.Applied);
            if (state.chapter == "CH05" && state.Happened(EvReturnR))
                return Task.FromResult(CommitResult.Applied);

            var next = state.Copy();
            next.Remember("CH04-AN05.reached");
            next.chapter = "CH05";
            next.layer = Reality.R;
            next.checkpoint = CheckpointEntry;
            return transaction.CommitEvidenceAsync(next);
        }

        /// <summary>
        /// Step 2 of B01: Enter AN05 Evacuation Memory (R -> M) at AP10.
        /// Requires CH05-CP01 committed in R first; opens S1 courtyard discharge in M and refreshes NavMesh.
        /// </summary>
        internal static Task<CommitResult> EnterEvacuationMemoryAsync(RealityTransaction transaction, Vector3 feet)
        {
            var state = transaction.Current;
            if (state == null || state.chapter != "CH05" || state.layer != Reality.R ||
                !GateService.G04(state) || !state.Happened("CH04-T06") || state.Happened(EvReturnR))
                return Task.FromResult(CommitResult.PadRequired);

            var next = state.Copy();
            next.checkpoint = CheckpointEntry;
            next.layer = Reality.M;
            next.Remember(EvEntry);
            return transaction.CommitAsync(next, feet, requiredPad: null, warp: true, geometryChanges: true);
        }

        /// <summary>
        /// Commits completion of one sequential evacuation pair (1, 2, or 3 -> 2/4/6 patients saved).
        /// Persisted atomically to Auto save so mid-evacuation reload restores cleanly at the next pair.
        /// </summary>
        internal static Task<CommitResult> CompleteEvacPairAsync(RealityTransaction transaction, int pairIndex)
        {
            var state = transaction.Current;
            if (state == null || state.chapter != "CH05" || state.layer != Reality.M || !state.Happened(EvEntry))
                return Task.FromResult(CommitResult.PadRequired);

            var next = state.Copy();
            switch (pairIndex)
            {
                case 1:
                    if (next.Happened(EvPair1)) return Task.FromResult(CommitResult.Applied);
                    next.Remember(EvPair1);
                    next.Remember(FlagPair1);
                    next.Remember(FlagFs22BioBroadcast);
                    break;
                case 2:
                    if (!next.Happened(EvPair1)) return Task.FromResult(CommitResult.PadRequired);
                    if (next.Happened(EvPair2)) return Task.FromResult(CommitResult.Applied);
                    next.Remember(EvPair2);
                    next.Remember(FlagPair2);
                    break;
                case 3:
                    if (!next.Happened(EvPair2)) return Task.FromResult(CommitResult.PadRequired);
                    if (next.Happened(EvPair3)) return Task.FromResult(CommitResult.Applied);
                    next.Remember(EvPair3);
                    next.Remember(FlagPair3);
                    next.Remember(FlagFs14WingCKey);
                    PuzzleService.RecordAttempt(next, PuzzleEvac, true);
                    break;
                default:
                    return Task.FromResult(CommitResult.PadRequired);
            }
            return transaction.CommitEvidenceAsync(next);
        }

        /// <summary>
        /// Accessible Skip (CH05-Q01): switches to fixed recap while guaranteeing the invariant 6/6 patients saved
        /// and committing the exact same historical flags (T02-T04, FS22, FS14, CH05-PU01.solved).
        /// </summary>
        internal static Task<CommitResult> SkipEvacuationMemoryAsync(RealityTransaction transaction)
        {
            var state = transaction.Current;
            if (state == null || state.chapter != "CH05" || state.layer != Reality.M || !state.Happened(EvEntry))
                return Task.FromResult(CommitResult.PadRequired);

            var next = state.Copy();
            next.Remember(EvPair1);
            next.Remember(FlagPair1);
            next.Remember(FlagFs22BioBroadcast);
            next.Remember(EvPair2);
            next.Remember(FlagPair2);
            next.Remember(EvPair3);
            next.Remember(FlagPair3);
            next.Remember(FlagFs14WingCKey);
            next.Remember(FlagEvacSkipped);
            PuzzleService.RecordAttempt(next, PuzzleEvac, true);
            return transaction.CommitEvidenceAsync(next);
        }

        /// <summary>
        /// Allows stepping out of Layer M cleanly back to AP10 in R between evacuation pairs (CH05-Q02).
        /// Preserves committed pair progress without exporting any M items and restores R S1 salvage blockage.
        /// </summary>
        internal static Task<CommitResult> ExitEvacuationEarlyToRealAsync(RealityTransaction transaction, Vector3 feet)
        {
            var state = transaction.Current;
            if (state == null || state.chapter != "CH05" || state.layer != Reality.M)
                return Task.FromResult(CommitResult.PadRequired);

            var next = state.Copy();
            next.layer = Reality.R;
            next.checkpoint = CheckpointEntry;
            return transaction.CommitAsync(next, feet, requiredPad: null, warp: true, geometryChanges: true);
        }

        /// <summary>
        /// B05 Return (M -> R): commits CH05-T05 and CH05-CP02 at AP10, stores non-exportable MemoryOnly
        /// reference MemEvacKhuB, releases M actors/NavMesh, and restores solid S1 salvage collider in R.
        /// </summary>
        internal static Task<CommitResult> FinishEvacuationReturnAsync(RealityTransaction transaction, Vector3 feet)
        {
            var state = transaction.Current;
            if (state == null || state.chapter != "CH05" || state.layer != Reality.M || !state.Happened(EvPair3))
                return Task.FromResult(CommitResult.PadRequired);

            var next = state.Copy();
            next.layer = Reality.R;
            next.checkpoint = CheckpointAfterMemory;
            next.Remember(EvReturnR);
            InventoryService.RecordMemoryRef(next, BackpackCatalog.MemEvacKhuB, "LOC05");
            return transaction.CommitAsync(next, feet, requiredPad: null, warp: true, geometryChanges: true);
        }

        /// <summary>
        /// B06 (LOC09): Acquires the 2005 Posted Diagram (adversarial observation) and 2004 As-Built Blueprint (E05).
        /// Never unlocks H03 or G05 by itself.
        /// </summary>
        internal static Task<CommitResult> AcquireBlueprintsAtLoc09Async(RealityTransaction transaction)
        {
            var state = transaction.Current;
            if (state == null || state.chapter != "CH05" || state.layer != Reality.R || !state.Happened(EvReturnR))
                return Task.FromResult(CommitResult.PadRequired);

            var next = state.Copy();
            RecordObservation(next, ObservationRegistry.Ch05PostedMap, "LOC09", ObservationStage.Examined);
            RecordObservation(next, ObservationRegistry.Ch05AsBuiltBlueprint, "LOC09", ObservationStage.Examined);
            if (!next.Has(EvidenceRegistry.AsBuilt2004))
            {
                if (!EvidenceRegistry.Grant(next, EvidenceRegistry.AsBuilt2004, Reality.R, "LOC09"))
                    return Task.FromResult(CommitResult.PadRequired);
            }
            InventoryService.TryAcquire(next, BackpackCatalog.Ch05PostedMap, Reality.R, "LOC09", out _);
            InventoryService.SyncFromEvidence(next, EvidenceRegistry.AsBuilt2004, "LOC09");
            next.Remember(EvBlueprintFound);
            if (!GateService.G05(next))
                next.checkpoint = CheckpointService;
            return transaction.CommitEvidenceAsync(next);
        }

        /// <summary>
        /// B06 (PZ07 / CH05-PU02): 2-marker architectural overlay alignment (Stairwell S2 + East Column Grid).
        /// Records failed attempts for progressive hint after 2 failures.
        /// Strictly NEVER opens H03 or G05 without physical E05b verification (CH05-Q03).
        /// </summary>
        internal static Task<CommitResult> AttemptPz07OverlayAsync(RealityTransaction transaction, bool s2Aligned, bool columnAligned)
        {
            var state = transaction.Current;
            if (state == null || state.chapter != "CH05" || state.layer != Reality.R ||
                !state.Has(EvidenceRegistry.AsBuilt2004))
                return Task.FromResult(CommitResult.PadRequired);
            if (state.Happened(FlagOverlayAligned))
                return Task.FromResult(CommitResult.Applied);

            var next = state.Copy();
            bool aligned = s2Aligned && columnAligned;
            PuzzleService.RecordAttempt(next, PuzzleOverlay, aligned);
            if (aligned)
            {
                next.Remember(FlagOverlayAligned);
                RecordObservation(next, ObservationRegistry.Ch05PostedMap, "LOC09", ObservationStage.Interpreted);
                RecordObservation(next, ObservationRegistry.Ch05AsBuiltBlueprint, "LOC09", ObservationStage.Interpreted);
            }
            return transaction.CommitEvidenceAsync(next);
        }

        /// <summary>
        /// Optional B06 LOC09 solvent tank valve & wedged damper inspection (DEC-V32-03).
        /// Non-blocking; unlocks CH05-D08 at beat B06 without claiming culprit/ignition source.
        /// </summary>
        internal static Task<CommitResult> ObserveOptionalValveAsync(RealityTransaction transaction)
        {
            var state = transaction.Current;
            if (state == null || state.chapter != "CH05" || state.layer != Reality.R || !state.Happened(EvReturnR))
                return Task.FromResult(CommitResult.PadRequired);
            if (state.Happened(FlagValveSabotage))
                return Task.FromResult(CommitResult.Applied);

            var next = state.Copy();
            RecordObservation(next, ObservationRegistry.Ch05ValveSabotage, "LOC09", ObservationStage.Examined);
            next.Remember(FlagValveSabotage);
            return transaction.CommitEvidenceAsync(next);
        }

        /// <summary>
        /// B07 (LOC11 exterior sunken observation pit): Physically verifies the 2004 bricked-in east exit in R (E05b).
        /// Grants E05b without opening H03/G05 unless PZ07 overlay is also complete and explicitly corroborated.
        /// </summary>
        internal static Task<CommitResult> ObserveSealedWallE05bAsync(RealityTransaction transaction)
        {
            var state = transaction.Current;
            if (state == null || state.chapter != "CH05" || state.layer != Reality.R || !state.Happened(EvReturnR))
                return Task.FromResult(CommitResult.PadRequired);

            var next = state.Copy();
            RecordObservation(next, ObservationRegistry.Ch05SealedWall, "LOC11", ObservationStage.Examined);
            if (!next.Has(EvidenceRegistry.SealedWallPhoto))
            {
                if (!EvidenceRegistry.Grant(next, EvidenceRegistry.SealedWallPhoto, Reality.R, "LOC11"))
                    return Task.FromResult(CommitResult.PadRequired);
            }
            InventoryService.SyncFromEvidence(next, EvidenceRegistry.SealedWallPhoto, "LOC11");
            next.Remember(EvSealedWall);
            return transaction.CommitEvidenceAsync(next);
        }

        /// <summary>
        /// Corroborates H03 and opens G05 (LOC08 door on Floor 3) ONLY when BOTH E05 (with PZ07.Aligned)
        /// AND physical R E05b are present.
        /// </summary>
        internal static Task<CommitResult> VerifyH03AndOpenG05Async(RealityTransaction transaction)
        {
            var state = transaction.Current;
            if (!CanVerifyH03(state))
                return Task.FromResult(CommitResult.PadRequired);
            if (GateService.G05(state) && state.Happened(FlagG05Opened))
                return Task.FromResult(CommitResult.Applied);

            var next = state.Copy();
            next.Remember(FlagH03Verified);
            next.Remember(FlagG05Opened);
            RecordObservation(next, ObservationRegistry.Ch05AsBuiltBlueprint, "LOC09", ObservationStage.Corroborated);
            RecordObservation(next, ObservationRegistry.Ch05SealedWall, "LOC11", ObservationStage.Corroborated);
            return transaction.CommitEvidenceAsync(next);
        }

        /// <summary>
        /// B08 CH05 -> CH06 Handoff at unlocked LOC08 threshold (AP11): commits CH05-T08 and CH05-CP03
        /// and stops cleanly at the CH05 boundary without entering CH06 or touching G06/E01/E06/E10b/E08.
        /// </summary>
        internal static Task<CommitResult> CommitCh05HandoffAsync(RealityTransaction transaction, Vector3 feet)
        {
            var state = transaction.Current;
            if (state == null || state.chapter != "CH05" || state.layer != Reality.R || !GateService.G05(state))
                return Task.FromResult(CommitResult.PadRequired);
            if (state.Happened(EvHandoff) && state.checkpoint == CheckpointHandoff)
                return Task.FromResult(CommitResult.Applied);

            var next = state.Copy();
            next.Remember(EvHandoff);
            next.checkpoint = CheckpointHandoff;
            return transaction.CommitEvidenceAsync(next);
        }

        private static void RecordObservation(Investigation state, string id, string location, ObservationStage minStage)
        {
            state.Observe(id);
            var rec = state.FindObservation(id);
            if (rec == null)
            {
                state.observationRecords.Add(new ObservationRecord
                {
                    id = id,
                    layer = Reality.R,
                    location = location,
                    chapter = "CH05",
                    stage = minStage
                });
            }
            else if (rec.stage < minStage)
            {
                rec.stage = minStage;
            }
        }
    }
}
