using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.Profiling;
using UnityEngine;

namespace CTCC
{
    public enum CommitResult { Applied, PadRequired, Obstructed, SaveUnavailable }

    public sealed class RealityTransaction
    {
        private static readonly ProfilerMarker CommitMarker = new("CTCC.RealityTransaction");
        private readonly HospitalScene scene;
        private readonly AtomicSaves saves;
        public Investigation Current { get; private set; }
        public event Action<Investigation> Committed;
        public Func<int> ReadCharge { get; set; }
        private bool savingAsync;
        private readonly List<TranscriptEntry> newDialogue = new();

        public void RecordDialogue(TranscriptEntry entry) => newDialogue.Add(entry);
        public bool HasPendingDialogue => newDialogue.Count != 0;

        private int IncludePendingDialogue(Investigation proposed)
        {
            proposed.transcript.AddRange(newDialogue);
            return newDialogue.Count;
        }

        public RealityTransaction(HospitalScene scene, AtomicSaves saves)
        {
            this.scene = scene;
            this.saves = saves;
            Current = Investigation.New();
        }

        public CommitResult Commit(Investigation proposed, Vector3 feet, string requiredPad = null, bool warp = false,
            bool geometryChanges = false, bool newVisit = false)
        {
            using var _ = CommitMarker.Auto();
            if (savingAsync) return CommitResult.SaveUnavailable;
            if (requiredPad != null && !OnPad(feet, requiredPad))
                return CommitResult.PadRequired;
            if (!scene.TryPad(SafeCheckpoints.Pad(proposed.checkpoint), out var checkpoint))
                return CommitResult.Obstructed;
            Vector3 landing = warp ? checkpoint.position : feet;
            if ((geometryChanges || warp) && HospitalScene.Occupied(landing))
                return CommitResult.Obstructed;
            proposed.sequence = Current.sequence + 1;
            if (!warp || proposed.checkpoint != "CH01-CP00")
                proposed.torchSeconds = ReadCharge?.Invoke() ?? proposed.torchSeconds;
            Present(proposed);
            if ((geometryChanges || warp) &&
                (HospitalScene.Occupied(landing) || !scene.TryRefreshNavigation(landing)))
            {
                Revert(landing);
                return CommitResult.Obstructed;
            }
            int included = newVisit ? newDialogue.Count : IncludePendingDialogue(proposed);
            try { saves.Write(SaveSlot.Auto, proposed); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Revert(landing);
                Debug.LogWarning($"CTCC snapshot write failed: {e.GetType().Name} (0x{e.HResult:X8}) {e.Message}");
                return CommitResult.SaveUnavailable;
            }
            Current = proposed;
            newDialogue.RemoveRange(0, included);
            Committed?.Invoke(Current);
            return CommitResult.Applied;
        }

        /// <summary>
        /// Stages presentation and validates regional navigation before writing
        /// the snapshot on a worker. Publishes committed state after the write succeeds.
        /// </summary>
        public async Task<CommitResult> CommitAsync(Investigation proposed, Vector3 feet, string requiredPad = null,
            bool warp = false, bool geometryChanges = false, Action afterFlush = null)
        {
            if (savingAsync) return CommitResult.SaveUnavailable;
            if (requiredPad != null && !OnPad(feet, requiredPad))
                return CommitResult.PadRequired;

            if (!scene.TryPad(SafeCheckpoints.Pad(proposed.checkpoint), out var checkpoint))
                return CommitResult.Obstructed;
            Vector3 landing = warp ? checkpoint.position : feet;
            if ((geometryChanges || warp) && HospitalScene.Occupied(landing))
                return CommitResult.Obstructed;

            savingAsync = true;
            try
            {
                using (CommitMarker.Auto())
                {
                    proposed.sequence = Current.sequence + 1;
                    if (!warp || proposed.checkpoint != "CH01-CP00")
                        proposed.torchSeconds = ReadCharge?.Invoke() ?? proposed.torchSeconds;
                    Present(proposed);
                }
                if ((geometryChanges || warp) &&
                    (HospitalScene.Occupied(landing) || !await scene.TryRefreshNavigationAsync(landing)))
                {
                    Revert(landing);
                    return CommitResult.Obstructed;
                }

                int included = IncludePendingDialogue(proposed);
                try { await Task.Run(() => saves.Write(SaveSlot.Auto, proposed, afterFlush)); }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                    Revert(landing);
                    Debug.LogWarning($"CTCC async snapshot write failed: {e.GetType().Name} (0x{e.HResult:X8}) {e.Message}");
                    return CommitResult.SaveUnavailable;
                }

                Current = proposed;
                newDialogue.RemoveRange(0, included);
                Committed?.Invoke(Current);
                return CommitResult.Applied;
            }
            finally
            {
                savingAsync = false;
            }
        }

        // Evidence pickups change the snapshot only. Geometry and the committed state stay live until durable publication.
        internal async Task<CommitResult> CommitEvidenceAsync(Investigation proposed, Action afterFlush = null,
            bool publishGameplay = true)
        {
            if (savingAsync) return CommitResult.SaveUnavailable;
            if (!scene.TryPad(SafeCheckpoints.Pad(proposed.checkpoint), out _))
                return CommitResult.Obstructed;
            savingAsync = true;
            try
            {
                proposed.sequence = Current.sequence + 1;
                proposed.torchSeconds = ReadCharge?.Invoke() ?? proposed.torchSeconds;
                int included = IncludePendingDialogue(proposed);
                try { await Task.Run(() => saves.Write(SaveSlot.Auto, proposed, afterFlush)); }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                    Debug.LogWarning($"CTCC evidence save failed: {e.GetType().Name} (0x{e.HResult:X8}) {e.Message}");
                    return CommitResult.SaveUnavailable;
                }
                Current = proposed;
                newDialogue.RemoveRange(0, included);
                if (publishGameplay) Committed?.Invoke(Current);
                return CommitResult.Applied;
            }
            finally { savingAsync = false; }
        }

        public Task<CommitResult> SaveDialogueAsync() =>
            HasPendingDialogue ? CommitEvidenceAsync(Current.Copy(), publishGameplay: false) : Task.FromResult(CommitResult.Applied);

        public SaveRead Inspect(SaveSlot slot) => saves.Read(slot, out _);

        public SaveRead Restore(SaveSlot slot, out Transform landing)
        {
            landing = null;
            if (savingAsync) return SaveRead.Empty;
            SaveRead result = saves.Read(slot, out var proposed);
            if (proposed == null)
                return result;
            // Core 04 / CH03-Q01: Reloading a save taken during a memory vignette (M) restores safely at the anchor pad in R.
            if (proposed.layer == Reality.M)
                proposed.layer = Reality.R;
            if (!scene.TryPad(SafeCheckpoints.Pad(proposed.checkpoint), out var checkpoint))
                return SaveRead.Damaged;
            Present(proposed);
            if (HospitalScene.Occupied(checkpoint.position) || !scene.TryRefreshNavigation(checkpoint.position))
            {
                Revert(checkpoint.position);
                return SaveRead.Damaged;
            }
            Current = proposed;
            newDialogue.Clear();
            landing = checkpoint;
            Committed?.Invoke(Current);
            return result;
        }

        public bool KeepManual()
        {
            if (savingAsync) return false;
            try
            {
                var snapshot = Current.Copy();
                snapshot.torchSeconds = ReadCharge?.Invoke() ?? snapshot.torchSeconds;
                IncludePendingDialogue(snapshot);
                saves.Write(SaveSlot.Manual, snapshot);
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"CTCC manual save failed: {e.GetType().Name} (0x{e.HResult:X8}) {e.Message}");
                return false;
            }
        }

        public bool OnPad(Vector3 feet, string id)
        {
            if (!scene.TryPad(id, out var pad)) return false;
            Vector3 offset = feet - pad.position;
            return Mathf.Abs(offset.y) < .22f && new Vector2(offset.x, offset.z).sqrMagnitude < .58f * .58f;
        }

        public void Present(Investigation snapshot)
        {
            scene.Present(snapshot.layer == Reality.B, (int)snapshot.f101.current,
                snapshot.Happened("CH01-T02"), snapshot.Happened("CH01-T05"),
                snapshot.Has("E12"), GateService.G02(snapshot),
                snapshot.layer == Reality.M, GateService.G03(snapshot), snapshot.chapter == "CH04");
            scene.PresentCheckpointProps(snapshot.Happened("CH01-T09"), snapshot.Happened("CH01-BATTERY"));
        }

        private void Revert(Vector3 point)
        {
            bool smooth = scene.smoothPresentation;
            scene.smoothPresentation = false;
            try { Present(Current); }
            finally { scene.smoothPresentation = smooth; }
            if (HospitalScene.Occupied(point) && scene.TryPad(SafeCheckpoints.Pad(Current.checkpoint), out var fallback))
                point = fallback.position;
            if (!HospitalScene.Occupied(point))
                scene.TryRefreshNavigation(point);
        }
    }
}
