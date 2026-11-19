using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace CTCC
{
    [Serializable]
    public sealed class MarkerPose
    {
        public string id;
        public Transform point;
    }

    [Serializable]
    public sealed class LayerFinish
    {
        public Renderer renderer;
        public Material real;
        public Material report;
    }

    [Serializable]
    public sealed class InspectImage
    {
        public string id;
        public Texture2D image;
    }

    [Serializable]
    public sealed class DoorPose
    {
        public Transform pivot;
        public float openAngle;
        public void Set(bool open)
        {
            if (pivot != null)
                pivot.localRotation = Quaternion.Euler(0, open ? openAngle : 0, 0);
        }
    }

    public sealed class HospitalScene : MonoBehaviour
    {
        public const int SolidLayer = 9;
        public const int SolidMask = 1 << SolidLayer;
        public const int InteractLayer = 8;
        public const int InteractMask = 1 << InteractLayer;
        public const float Radius = .35f;
        public const float Height = 1.75f;

        public MarkerPose[] pads;
        public InspectImage[] inspectImages = Array.Empty<InspectImage>();
        public DoorPose outerGate, roomDoor, emergencyDoorA, emergencyDoorB;
        public GameObject seamBlocker;
        public GameObject realProps, reportProps, pendingBay, deniedBay;
        public GameObject statusA, statusB, statusC, paperAfter, storeClues;
        public Transform cabinet;
        public Vector3 cabinetClosed;
        public Vector3 cabinetOpen;
        public List<LayerFinish> finishes = new();
        public Volume realGrade, reportGrade;
        public ReflectionProbe localReflection;
        public EnvironmentMotion motion;
        public EnvironmentalAudio atmosphere;
        public bool referenceArt;
        private bool themeReady;
        private bool showedReport;
        private NavMeshDataInstance navigation;
        private NavMeshData navData;

        public Transform Pad(string id)
        {
            foreach (var pad in pads)
                if (pad.id == id)
                    return pad.point;
            throw new InvalidOperationException("Missing safe pad " + id);
        }

        public void Present(bool layerB, int form, bool entered, bool acceptedBoard, bool closetOpen, bool secondGate)
        {
            if (!themeReady || showedReport != layerB)
            {
                foreach (var part in finishes)
                    part.renderer.sharedMaterial = layerB ? part.report : part.real;
                Toggle(realProps, !layerB);
                Toggle(reportProps, layerB);
                if (realGrade != null) realGrade.weight = layerB ? 0 : 1;
                if (reportGrade != null) reportGrade.weight = layerB ? 1 : 0;
                RenderSettings.fogColor = layerB ? new Color(.24f,.37f,.32f) : new Color(.11f,.17f,.21f);
                RenderSettings.ambientLight = layerB ? new Color(.64f,.71f,.62f) : new Color(.33f,.41f,.43f);
                if (localReflection != null && Application.isPlaying)
                    localReflection.RenderProbe();
                if(motion != null) motion.reportLayer=layerB;
                if(atmosphere != null) atmosphere.ReportLayer=layerB;
                showedReport = layerB;
                themeReady = true;
            }
            Toggle(pendingBay, layerB && (form < 2 || form == 3));
            Toggle(deniedBay, layerB && form == 2);
            Toggle(statusA, layerB && form == 2);
            Toggle(statusB, layerB && form == 3);
            Toggle(statusC, layerB && form == 4);
            Toggle(paperAfter, layerB && form >= 2);
            Toggle(storeClues, !layerB && closetOpen);
            if (cabinet != null)
            {
                bool accessible = !layerB && closetOpen;
                cabinet.localPosition = accessible ? cabinetOpen : cabinetClosed;
            }
            outerGate.Set(entered);
            roomDoor.Set(acceptedBoard || !layerB);
            emergencyDoorA.Set(secondGate);
            emergencyDoorB.Set(secondGate);
            Physics.SyncTransforms();
        }

        private static void Toggle(GameObject item, bool active)
        {
            if (item != null && item.activeSelf != active)
                item.SetActive(active);
        }

        public static bool Occupied(Vector3 point)
        {
            return Physics.CheckCapsule(point + Vector3.up * (Radius + .04f),
                point + Vector3.up * (Height - Radius), Radius, SolidMask, QueryTriggerInteraction.Ignore);
        }

        public NavMeshData BuildNavigation()
        {
            var bounds = new Bounds(new Vector3(5,1,0), new Vector3(42,8,59));
            var sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(bounds, SolidMask, NavMeshCollectGeometry.PhysicsColliders, 0,
                new List<NavMeshBuildMarkup>(), sources);
            var build = NavMesh.GetSettingsByIndex(0);
            build.agentRadius = Radius;
            build.agentHeight = Height;
            build.agentClimb = .25f;
            build.overrideVoxelSize = true;
            build.voxelSize = .14f;
            return NavMeshBuilder.BuildNavMeshData(build, sources, bounds, Vector3.zero, Quaternion.identity);
        }

        public bool TryRefreshNavigation(Vector3 landing)
        {
            var proposed = BuildNavigation();
            if (proposed == null) return false;
            if (navigation.valid) navigation.Remove();
            var candidate = NavMesh.AddNavMeshData(proposed);
            if (!candidate.valid || !NavMesh.SamplePosition(landing, out _, .36f, NavMesh.AllAreas))
            {
                if (candidate.valid) candidate.Remove();
                Destroy(proposed);
                if (navData != null) navigation = NavMesh.AddNavMeshData(navData);
                return false;
            }
            if (navData != null) Destroy(navData);
            navData = proposed;
            navigation = candidate;
            return true;
        }

        private void OnDestroy()
        {
            if (navigation.valid) navigation.Remove();
            if (navData != null) Destroy(navData);
        }
    }
}
