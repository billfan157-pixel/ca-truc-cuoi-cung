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

        internal static async Task<CommitResult> SetPower(RealityTransaction transaction, int mask)
        {
            var state = transaction.Current;
            if (state.chapter != "CH04" || state.layer != Reality.R || !state.Happened("PT01.presented") || (mask & ~15) != 0)
                return CommitResult.PadRequired;
            var next = state.Copy();
            next.servicePower = ValidPower(mask) ? mask : 0;
            if (MorguePower(next.servicePower))
            {
                next.Remember("CH04-T04");
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
            next.checkpoint = "CH04-CP04";
            return transaction.CommitEvidenceAsync(next);
        }
    }
}
