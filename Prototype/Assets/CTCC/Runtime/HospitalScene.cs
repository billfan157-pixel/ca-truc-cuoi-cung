using System;
using System.Collections.Generic;
using Unity.Profiling;
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
        public Material[] realMaterials;
        public Material[] reportMaterials;
    }

    [Serializable]
    public sealed class InspectImage
    {
        public string id;
        public Texture2D image;
    }

    [Serializable]
    public sealed class InspectModel
    {
        public string id;
        public GameObject template;
    }

    [Serializable]
    public sealed class DoorPose
    {
        public Transform pivot;
        public float openAngle;
        public bool isOpen;

        public void Set(bool open)
        {
            isOpen = open;
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
        public const float Radius = 0.35f;
        public const float Height = 1.75f;

        public MarkerPose[] pads;
        public InspectImage[] inspectImages = Array.Empty<InspectImage>();
        public InspectModel[] inspectModels = Array.Empty<InspectModel>();
        public DoorPose outerGate, roomDoor, emergencyDoorA, emergencyDoorB;
        public GameObject seamBlocker;
        public GameObject realProps, reportProps, pendingBay, deniedBay;
        public GameObject statusA, statusB, statusC, paperAfter, storeClues;
        public GameObject clockHandsR, clockHandsB;
        public Transform cabinet;
        public Vector3 cabinetClosed;
        public Vector3 cabinetOpen;
        public List<LayerFinish> finishes = new();
        public Volume realGrade, reportGrade;
        public ReflectionProbe localReflection, corridorReflection;
        public EnvironmentMotion motion;
        public EnvironmentalAudio atmosphere;
        public InteractableRig interactables;
        public bool referenceArt;
        [SerializeField] private Bounds navigationBounds = new(new Vector3(5, 1, 0), new Vector3(42, 8, 59));
        [NonSerialized] public bool smoothPresentation;
        private bool themeReady;
        private bool showedReport;
        private NavMeshDataInstance navigation;
        private NavigationSnapshot activeNavigation;
        private readonly List<NavigationSnapshot> navigationCache = new();
        private readonly List<NavMeshBuildSource> navigationSources = new();
        private readonly List<NavMeshBuildMarkup> navigationMarkups = new();
        private static readonly ProfilerMarker CollectNavigationMarker = new("CTCC.Navigation.Collect");
        private static readonly ProfilerMarker BuildNavigationMarker = new("CTCC.Navigation.Build");
        private static readonly ProfilerMarker ProbeMarker = new("CTCC.Reflection.Schedule");
        private const int NavigationCacheCapacity = 8;
        private ReflectionProbe renderingProbe;
        private int reflectionRequest;
        private int reflectionVersion;
        private int renderingVersion;
        private int nextProbe;

        private sealed class NavigationSnapshot
        {
            public NavMeshBuildSource[] Sources;
            public Vector3 Origin;
            public Quaternion Rotation;
            public NavMeshData Data;
        }

        public Transform Pad(string id)
        {
            foreach (var pad in pads)
                if (pad.id == id)
                    return pad.point;
            throw new InvalidOperationException("Missing safe pad " + id);
        }

        public GameObject InspectTemplate(string id)
        {
            if (inspectModels == null) return null;
            foreach (var entry in inspectModels)
                if (entry.id == id)
                    return entry.template;
            return null;
        }

        public void Present(bool layerB, int form, bool entered, bool acceptedBoard, bool closetOpen, bool secondGate)
        {
            if (!themeReady || showedReport != layerB)
            {
                foreach (var part in finishes)
                {
                    if (part.renderer == null) continue;
                    if (part.realMaterials != null && part.realMaterials.Length > 0)
                        part.renderer.sharedMaterials = layerB ? part.reportMaterials : part.realMaterials;
                    else if (part.real != null && part.report != null)
                        part.renderer.sharedMaterial = layerB ? part.report : part.real;
                }
                Toggle(realProps, !layerB);
                Toggle(reportProps, layerB);
                Toggle(clockHandsR, !layerB);
                Toggle(clockHandsB, layerB);

                if (realGrade != null) realGrade.weight = layerB ? 0f : 1f;
                if (reportGrade != null) reportGrade.weight = layerB ? 1f : 0f;

                // Trilight Gradient ambient lighting — alive cool sky, tinted equator, grounded dark warm floor bounce!
                RenderSettings.ambientMode = AmbientMode.Trilight;
                if (layerB)
                {
                    RenderSettings.ambientSkyColor = new Color(0.32f, 0.38f, 0.33f);
                    RenderSettings.ambientEquatorColor = new Color(0.22f, 0.27f, 0.23f);
                    RenderSettings.ambientGroundColor = new Color(0.11f, 0.12f, 0.10f);
                    RenderSettings.fogColor = new Color(0.19f, 0.27f, 0.23f);
                    RenderSettings.fogDensity = 0.011f;
                }
                else
                {
                    RenderSettings.ambientSkyColor = new Color(0.13f, 0.19f, 0.23f);
                    RenderSettings.ambientEquatorColor = new Color(0.08f, 0.11f, 0.13f);
                    RenderSettings.ambientGroundColor = new Color(0.045f, 0.042f, 0.036f);
                    RenderSettings.fogColor = new Color(0.065f, 0.10f, 0.125f);
                    RenderSettings.fogDensity = 0.019f;
                }

                if (Application.isPlaying) reflectionVersion++;
                if (motion != null) motion.reportLayer = layerB;
                if (atmosphere != null) atmosphere.ReportLayer = layerB;
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
                Vector3 targetPos = accessible ? cabinetOpen : cabinetClosed;
                if (smoothPresentation && interactables != null && Application.isPlaying)
                    interactables.MoveCabinet(targetPos, false, atmosphere);
                else
                {
                    cabinet.localPosition = targetPos;
                    interactables?.ConfigureCabinet(cabinet, targetPos);
                }
            }

            outerGate.Set(entered);
            bool wantRoomOpen = acceptedBoard || !layerB;
            if (smoothPresentation && interactables != null && Application.isPlaying && roomDoor.pivot != null)
            {
                roomDoor.isOpen = wantRoomOpen;
                interactables.MoveDoor(roomDoor.pivot, wantRoomOpen ? roomDoor.openAngle : 0f, false, atmosphere);
            }
            else
            {
                roomDoor.Set(wantRoomOpen);
                interactables?.ConfigureDoor(roomDoor.pivot, wantRoomOpen ? roomDoor.openAngle : 0f);
            }
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
            return Physics.CheckCapsule(point + Vector3.up * (Radius + 0.04f),
                point + Vector3.up * (Height - Radius), Radius, SolidMask, QueryTriggerInteraction.Ignore);
        }

        public NavMeshData BuildNavigation()
        {
            CollectNavigation();
            return BakeNavigation();
        }

        private void CollectNavigation()
        {
            using var marker = CollectNavigationMarker.Auto();
            // The scene-owned root keeps an additively loaded zone out of this zone's source set.
            NavMeshBuilder.CollectSources(transform, SolidMask, NavMeshCollectGeometry.PhysicsColliders, 0,
                navigationMarkups, navigationSources);
        }

        private NavMeshData BakeNavigation()
        {
            using var marker = BuildNavigationMarker.Auto();
            var build = NavMesh.GetSettingsByIndex(0);
            build.agentRadius = Radius;
            build.agentHeight = Height;
            build.agentClimb = 0.25f;
            build.overrideVoxelSize = true;
            build.voxelSize = 0.14f;
            return NavMeshBuilder.BuildNavMeshData(build, navigationSources, navigationBounds, transform.position, transform.rotation);
        }

        public bool TryRefreshNavigation(Vector3 landing)
        {
            CollectNavigation();
            var proposed = navigationCache.Find(MatchesNavigation);
            if (proposed == activeNavigation && navigation.valid)
                return NavMesh.SamplePosition(landing, out _, 0.36f, NavMesh.AllAreas);
            bool missed = proposed == null;
            if (missed)
            {
                var data = BakeNavigation();
                if (data == null) return false;
                proposed = new NavigationSnapshot { Sources = navigationSources.ToArray(), Data = data,
                    Origin = transform.position, Rotation = transform.rotation };
            }

            if (navigation.valid) navigation.Remove();
            var candidate = NavMesh.AddNavMeshData(proposed.Data);
            if (!candidate.valid || !NavMesh.SamplePosition(landing, out _, 0.36f, NavMesh.AllAreas))
            {
                if (candidate.valid) candidate.Remove();
                if (missed) Destroy(proposed.Data);
                if (activeNavigation != null) navigation = NavMesh.AddNavMeshData(activeNavigation.Data);
                return false;
            }
            navigationCache.Remove(proposed);
            navigationCache.Add(proposed);
            if (navigationCache.Count > NavigationCacheCapacity)
            {
                Destroy(navigationCache[0].Data);
                navigationCache.RemoveAt(0);
            }
            activeNavigation = proposed;
            navigation = candidate;
            return true;
        }

        private bool MatchesNavigation(NavigationSnapshot snapshot)
        {
            if (snapshot.Sources.Length != navigationSources.Count || !snapshot.Origin.Equals(transform.position) ||
                !snapshot.Rotation.Equals(transform.rotation)) return false;
            for (int i = 0; i < snapshot.Sources.Length; i++)
            {
                var a = snapshot.Sources[i];
                var b = navigationSources[i];
                if (a.shape != b.shape || a.area != b.area || a.sourceObject != b.sourceObject ||
                    a.component != b.component || !a.size.Equals(b.size) || !a.transform.Equals(b.transform)) return false;
            }
            return true;
        }

        private void LateUpdate()
        {
            if (renderingProbe != null && renderingProbe.isActiveAndEnabled && !renderingProbe.IsFinishedRendering(reflectionRequest)) return;
            renderingProbe = null;
            if (renderingVersion != reflectionVersion)
            {
                renderingVersion = reflectionVersion;
                nextProbe = 0;
            }
            while (nextProbe < 2)
            {
                var probe = nextProbe++ == 0 ? localReflection : corridorReflection;
                if (probe == null || !probe.isActiveAndEnabled) continue;
                using var marker = ProbeMarker.Auto();
                reflectionRequest = probe.RenderProbe();
                if (reflectionRequest >= 0) renderingProbe = probe;
                break;
            }
        }

        private void OnDestroy()
        {
            if (navigation.valid) navigation.Remove();
            foreach (var snapshot in navigationCache) Destroy(snapshot.Data);
        }
    }
}
