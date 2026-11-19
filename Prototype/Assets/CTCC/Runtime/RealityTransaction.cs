using System;
using System.IO;
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

        public RealityTransaction(HospitalScene scene,AtomicSaves saves)
        {
            this.scene=scene;
            this.saves=saves;
            Current=Investigation.New();
        }

        public CommitResult Commit(Investigation proposed,Vector3 feet,string requiredPad=null,bool warp=false,
            bool geometryChanges=false)
        {
            using var _=CommitMarker.Auto();
            if (requiredPad!=null && !OnPad(feet,requiredPad))
                return CommitResult.PadRequired;
            Transform checkpoint=scene.Pad(SafeCheckpoints.Pad(proposed.checkpoint));
            Vector3 landing=warp ? checkpoint.position : feet;
            if ((geometryChanges || warp) && HospitalScene.Occupied(landing))
                return CommitResult.Obstructed;
            proposed.sequence=Current.sequence+1;
            if(!warp || proposed.checkpoint!="CH01-CP00")
                proposed.torchSeconds=ReadCharge?.Invoke()??proposed.torchSeconds;
            Present(proposed);
            if ((geometryChanges || warp) &&
                (HospitalScene.Occupied(landing) || !scene.TryRefreshNavigation(landing)))
            {
                Revert(landing);
                return CommitResult.Obstructed;
            }
            try { saves.Write(SaveSlot.Auto,proposed); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Revert(landing);
                Debug.LogWarning("CTCC snapshot write failed: "+e.GetType().Name);
                return CommitResult.SaveUnavailable;
            }
            Current=proposed;
            Committed?.Invoke(Current);
            return CommitResult.Applied;
        }

        public SaveRead Inspect(SaveSlot slot) => saves.Read(slot,out _);

        public SaveRead Restore(SaveSlot slot,out Transform landing)
        {
            landing=null;
            SaveRead result=saves.Read(slot,out var proposed);
            if (proposed==null)
                return result;
            Transform checkpoint=scene.Pad(SafeCheckpoints.Pad(proposed.checkpoint));
            Present(proposed);
            if (HospitalScene.Occupied(checkpoint.position) || !scene.TryRefreshNavigation(checkpoint.position))
            {
                Revert(scene.Pad(SafeCheckpoints.Pad(Current.checkpoint)).position);
                return SaveRead.Damaged;
            }
            Current=proposed;
            landing=checkpoint;
            Committed?.Invoke(Current);
            return result;
        }

        public bool KeepManual()
        {
            try
            {
                var snapshot=Current.Copy();
                snapshot.torchSeconds=ReadCharge?.Invoke()??snapshot.torchSeconds;
                saves.Write(SaveSlot.Manual,snapshot);
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning("CTCC manual save failed: "+e.GetType().Name);
                return false;
            }
        }

        public bool OnPad(Vector3 feet,string id)
        {
            Vector3 offset=feet-scene.Pad(id).position;
            return Mathf.Abs(offset.y)<.22f && new Vector2(offset.x,offset.z).sqrMagnitude<.58f*.58f;
        }

        public void Present(Investigation snapshot)
        {
            scene.Present(snapshot.layer==Reality.B,(int)snapshot.f101.current,
                snapshot.Happened("CH01-T02"),snapshot.Happened("CH01-T05"),
                snapshot.Has("E12"),GateService.G02(snapshot));
            if(scene.seamBlocker!=null) scene.seamBlocker.SetActive(snapshot.Happened("CH01-T09"));
        }

        private void Revert(Vector3 point)
        {
            Present(Current);
            if (HospitalScene.Occupied(point)) point=scene.Pad(SafeCheckpoints.Pad(Current.checkpoint)).position;
            if (!HospitalScene.Occupied(point))
                scene.TryRefreshNavigation(point);
        }
    }
}
