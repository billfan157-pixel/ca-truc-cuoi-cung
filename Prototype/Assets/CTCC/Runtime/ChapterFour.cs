using System.Threading.Tasks;

namespace CTCC
{
    internal static class ChapterFour
    {
        internal const int Hall = 1, Archive = 2, Lift = 4, Service = 8;
        private static readonly int[] Loads = { 1, 2, 3, 1 };

        internal static int TotalLoad(int mask)
        {
            int load = 0;
            for (int i = 0; i < Loads.Length; i++)
                if ((mask & (1 << i)) != 0) load += Loads[i];
            return load;
        }

        internal static bool ValidPower(int mask) => (mask & ~15) == 0 && TotalLoad(mask) <= 4;
        internal static bool MorguePower(int mask) => (mask & (Archive | Service)) == (Archive | Service);

        internal static Task<CommitResult> ReadOrderlySchedule(RealityTransaction transaction)
        {
            var state = transaction.Current;
            if (state.chapter != "CH04" || state.layer != Reality.R)
                return Task.FromResult(CommitResult.PadRequired);
            if (state.Happened("PZ04.ScheduleRead")) return Task.FromResult(CommitResult.Applied);
            var next = state.Copy();
            next.Remember("PZ04.ScheduleRead");
            return transaction.CommitEvidenceAsync(next);
        }

        internal static Task<CommitResult> ReadRoomStatusSlip(RealityTransaction transaction)
        {
            var state = transaction.Current;
            if (state.chapter != "CH04" || state.layer != Reality.R)
                return Task.FromResult(CommitResult.PadRequired);
            if (state.Happened("PZ04.SlipRead")) return Task.FromResult(CommitResult.Applied);
            var next = state.Copy();
            next.Remember("PZ04.SlipRead");
            return transaction.CommitEvidenceAsync(next);
        }

        /// <summary>
        /// PZ04 Route B: Factual room-status cross-check (requires reading PZ04_ROOM_SLIP).
        /// Strictly rejects record falsification (validCrossCheck == false) or missing slip,
        /// recording failed attempts for progressive 2-strike hint without ever altering records falsely.
        /// </summary>
        internal static Task<CommitResult> AttemptUpdateOrderlySchedule(RealityTransaction transaction, bool validCrossCheck)
        {
            var state = transaction.Current;
            if (state.chapter != "CH04" || state.layer != Reality.R)
                return Task.FromResult(CommitResult.PadRequired);
            if (state.Happened("PZ04.ScheduleUpdated"))
                return Task.FromResult(CommitResult.Applied);
            var next = state.Copy();
            bool accepted = validCrossCheck && state.Happened("PZ04.SlipRead");
            PuzzleService.RecordAttempt(next, "CH04-PU01", accepted);
            if (accepted)
                next.Remember("PZ04.ScheduleUpdated");
            return transaction.CommitEvidenceAsync(next);
        }

        /// <summary>
        /// Commits CH04-T02 when player cleanly clears the LOC06 -> LOC05 ward corridor via either
        /// Route A (timing/observation past the 3-stop Orderly) or Route B (factual schedule update).
        /// </summary>
        internal static Task<CommitResult> CompletePz04Passage(RealityTransaction transaction)
        {
            var state = transaction.Current;
            if (state.chapter != "CH04" || state.layer != Reality.R)
                return Task.FromResult(CommitResult.PadRequired);
            bool routeB = state.Happened("PZ04.ScheduleUpdated");
            string routeEvent = routeB ? "PZ04.RouteB" : "PZ04.RouteA";
            if (state.Happened("CH04-T02") && state.Happened(routeEvent))
                return Task.FromResult(CommitResult.Applied);
            var next = state.Copy();
            next.Remember("CH04-T02");
            next.Remember(routeEvent);
            PuzzleService.RecordAttempt(next, "CH04-PU01", true);
            return transaction.CommitEvidenceAsync(next);
        }

        internal static async Task<CommitResult> SetPower(RealityTransaction transaction, int mask)
        {
            var state = transaction.Current;
            if (state.chapter != "CH04" || state.layer != Reality.R || !state.Happened("PT01.presented") || (mask & ~15) != 0)
                return CommitResult.PadRequired;
            var next = state.Copy();
            bool valid = ValidPower(mask);
            next.servicePower = valid ? mask : 0;
            if (!valid)
                PuzzleService.RecordAttempt(next, "CH04-PU02", false);
            if (MorguePower(next.servicePower))
            {
                next.Remember("CH04-T04");
                PuzzleService.RecordAttempt(next, "CH04-PU02", true);
                if (!GateService.H05(next)) next.checkpoint = "CH04-CP03";
            }
            bool lockChanged = MorguePower(next.servicePower) != MorguePower(state.servicePower);
            var result = await transaction.CommitEvidenceAsync(next);
            if (result == CommitResult.Applied && lockChanged)
                HospitalScene.Instance?.RefreshZoneNavMesh("LOC09");
            return result;
        }

        internal static Task<CommitResult> Take(RealityTransaction transaction, string id, string location)
        {
            var next = transaction.Current.Copy();
            if (next.Has(id)) return Task.FromResult(CommitResult.Applied);
            if (location == "LOC10" && !MorguePower(next.servicePower) ||
                !EvidenceRegistry.Grant(next, id, Reality.R, location))
                return Task.FromResult(CommitResult.PadRequired);
            InventoryService.SyncFromEvidence(next, id, location);
            return transaction.CommitEvidenceAsync(next);
        }

        internal static Task<CommitResult> PresentMeeting(RealityTransaction transaction)
        {
            var next = transaction.Current.Copy();
            if (next.chapter != "CH04" || !next.Has(EvidenceRegistry.MeetingTape) ||
                !next.Has(EvidenceRegistry.DutyOrders) || !next.Has(EvidenceRegistry.DutyLog))
                return Task.FromResult(CommitResult.PadRequired);
            next.Remember("PT01.presented");
            next.Remember("CH04-T03");
            if (!next.Happened("CH04-T04")) next.checkpoint = "CH04-CP02";
            return transaction.CommitEvidenceAsync(next);
        }

        internal static Task<CommitResult> MatchRecovery(RealityTransaction transaction)
        {
            var next = transaction.Current.Copy();
            if (next.chapter != "CH04" || !next.Happened("PT01.presented") ||
                !next.Has(EvidenceRegistry.MorgueRegister) || !next.Has(EvidenceRegistry.LanRecovery))
                return Task.FromResult(CommitResult.PadRequired);
            next.Remember("CH04-T05");
            next.Remember("H05.verified");
            next.Remember("PT02.presented");
            PuzzleService.RecordAttempt(next, "CH04-PU03", true);
            next.checkpoint = "CH04-CP04";
            return transaction.CommitEvidenceAsync(next);
        }

        internal static Task<CommitResult> ReachAn05Handoff(RealityTransaction transaction)
        {
            var state = transaction.Current;
            if (state.chapter != "CH04" || state.layer != Reality.R ||
                !GateService.G04(state) || !state.Happened("CH04-T06"))
                return Task.FromResult(CommitResult.PadRequired);
            if (state.Happened("CH04-AN05.reached"))
                return Task.FromResult(CommitResult.Applied);
            var next = state.Copy();
            next.Remember("CH04-AN05.reached");
            return transaction.CommitEvidenceAsync(next);
        }
    }
}
