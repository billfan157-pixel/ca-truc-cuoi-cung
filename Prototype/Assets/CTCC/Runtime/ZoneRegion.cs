using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.AI;

namespace CTCC
{
    /// <summary>
    /// Scene-owned additive zone descriptor (LOC01..LOC04) per M2 Scene Handoff.
    /// Owns its local pads, finishes, R/B props, doors, audio loops, reflection probes,
    /// character actors, and regional NavMeshData cache so zones scale independently.
    /// </summary>
    public sealed class ZoneRegion : MonoBehaviour
    {
        public string zoneId;
        public Bounds navigationBounds = new(Vector3.zero, new Vector3(16, 8, 16));
        public MarkerPose[] pads = Array.Empty<MarkerPose>();
        public List<LayerFinish> finishes = new();

        public GameObject realProps;
        public GameObject reportProps;
        public GameObject memoryProps;
        public GameObject pendingBay;
        public GameObject deniedBay;
        public GameObject statusA;
        public GameObject statusB;
        public GameObject statusC;
        public GameObject paperAfter;
        public GameObject storeClues;
        public GameObject seamBlocker;
        public GameObject clockHandsR;
        public GameObject clockHandsB;
        public GameObject spareBatteryProp;
        public GameObject receptionGlassSilhouette;
        public GameObject trolleyShadowProp;
        public Light[] emergencyRedLights = Array.Empty<Light>();

        public DoorPose outerGate;
        public DoorPose roomDoor;
        public DoorPose emergencyDoorA;
        public DoorPose emergencyDoorB;
        public DoorPose archiveExitDoor;
        public GameObject[] upperConnectors = Array.Empty<GameObject>();
        public GameObject serviceConnector;
        public GameObject morgueLock;
        public Light[] circuitLights = Array.Empty<Light>();
        public GameObject namedMemory;
        public ArchiveMemoryMotion memoryMotion;

        public Transform[] ch03PatrolWaypoints = Array.Empty<Transform>();
        public Transform ch03RadioPoint;

        public Transform cabinet;
        public Vector3 cabinetClosed;
        public Vector3 cabinetOpen;
        public Transform curtainCloth;
        public Vector3 curtainClosedScale;
        public Vector3 curtainOpenScale;

        public ReflectionProbe[] reflectionProbes = Array.Empty<ReflectionProbe>();
        public Transform[] movingFans = Array.Empty<Transform>();
        public Light[] livingTubes = Array.Empty<Light>();
        public MeshFilter[] movingCloth = Array.Empty<MeshFilter>();
        public RoomLoop[] roomLoops = Array.Empty<RoomLoop>();

        public CharacterActor lanActor;
        public CharacterActor gatekeeperActor;
        public CharacterActor anActor;
        public Transform[] gatekeeperWaypoints = Array.Empty<Transform>();
        public Transform lanSilhouettePoint;
        public Transform lanDeskPoint;
        public Transform lanStepOutPoint;

        public AudioSource paSpeakerAnchor;
        public AudioSource phoneAnchor;
        public AudioSource lanVoiceAnchor;
        public AudioSource gatekeeperVoiceAnchor;
        public AudioSource anVoiceAnchor;

        private NavMeshDataInstance localNav;
        private RegionalNavSnapshot activeSnapshot;
        private readonly List<RegionalNavSnapshot> navCache = new();
        private readonly List<NavMeshBuildSource> navSources = new();
        private readonly List<NavMeshBuildMarkup> navMarkups = new();
        private const int RegionalCacheCapacity = 6;

        private static readonly ProfilerMarker CollectMarker = new("CTCC.Navigation.Collect");
        private static readonly ProfilerMarker BakeMarker = new("CTCC.Navigation.Build");

        private sealed class RegionalNavSnapshot
        {
            public NavMeshBuildSource[] Sources;
            public Vector3 Origin;
            public Quaternion Rotation;
            public NavMeshData Data;
        }

        private void OnEnable()
        {
            if (HospitalScene.Instance != null)
                HospitalScene.Instance.RegisterZone(this);
        }

        private void OnDisable()
        {
            if (HospitalScene.Instance != null)
                HospitalScene.Instance.UnregisterZone(this);
            ReleaseNavigation();
        }

        public void ApplyLayerVisuals(bool layerB, bool layerM = false)
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
            Toggle(memoryProps, layerM);
            Toggle(clockHandsR, !layerB);
            Toggle(clockHandsB, layerB);
        }

        public void PresentCasework(Investigation state)
        {
            bool hasMorgue = ChapterFour.MorguePower(state.servicePower);
            Toggle(morgueLock, !hasMorgue && !GateService.H05(state));
            foreach (var light in circuitLights)
                if (light != null) light.enabled = hasMorgue;
            Toggle(namedMemory, state.PatientIdentityVerified("C02"));
            if (memoryMotion != null) memoryMotion.enabled = !state.settings.reduceMotion;
        }

        public void CollectRegionalSources(List<NavMeshBuildSource> destination)
        {
            using var _ = CollectMarker.Auto();
            navSources.Clear();
            NavMeshBuilder.CollectSources(transform, HospitalScene.SolidMask, NavMeshCollectGeometry.PhysicsColliders,
                0, navMarkups, navSources);
            destination.AddRange(navSources);
        }

        public bool RefreshRegionalNavMesh()
        {
            var match = CollectNavigation();
            if (match == activeSnapshot && localNav.valid)
                return true;

            bool created = match == null;
            if (created)
            {
                NavMeshData data;
                using (BakeMarker.Auto())
                {
                    data = NavMeshBuilder.BuildNavMeshData(BuildSettings(), navSources, navigationBounds, transform.position, transform.rotation);
                }
                if (data == null) return false;
                match = Snapshot(data);
            }
            return Activate(match, created);
        }

        public async Task<bool> RefreshRegionalNavMeshAsync()
        {
            var match = CollectNavigation();
            if (match != null)
                return match == activeSnapshot && localNav.valid || Activate(match, false);

            var settings = BuildSettings();
            var data = new NavMeshData(settings.agentTypeID) { position = transform.position, rotation = transform.rotation };
            match = Snapshot(data);
            var sources = new List<NavMeshBuildSource>(match.Sources);
            AsyncOperation operation;
            using (BakeMarker.Auto())
                operation = NavMeshBuilder.UpdateNavMeshDataAsync(data, settings, sources, navigationBounds);
            while (!operation.isDone) await Awaitable.NextFrameAsync();
            if (this == null || !isActiveAndEnabled)
            {
                Destroy(data);
                return false;
            }
            return Activate(match, true);
        }

        private RegionalNavSnapshot CollectNavigation()
        {
            using var _ = CollectMarker.Auto();
            navSources.Clear();
            NavMeshBuilder.CollectSources(transform, HospitalScene.SolidMask, NavMeshCollectGeometry.PhysicsColliders,
                0, navMarkups, navSources);
            return navCache.Find(Matches);
        }

        private static NavMeshBuildSettings BuildSettings()
        {
            var settings = NavMesh.GetSettingsByIndex(0);
            settings.agentRadius = HospitalScene.Radius;
            settings.agentHeight = HospitalScene.Height;
            settings.agentClimb = 0.25f;
            settings.overrideVoxelSize = true;
            settings.voxelSize = 0.14f;
            return settings;
        }

        private RegionalNavSnapshot Snapshot(NavMeshData data) => new()
        {
            Sources = navSources.ToArray(),
            Origin = transform.position,
            Rotation = transform.rotation,
            Data = data
        };

        private bool Activate(RegionalNavSnapshot match, bool created)
        {
            if (localNav.valid) localNav.Remove();
            var instance = NavMesh.AddNavMeshData(match.Data);
            if (!instance.valid)
            {
                if (created) Destroy(match.Data);
                if (activeSnapshot != null) localNav = NavMesh.AddNavMeshData(activeSnapshot.Data);
                return false;
            }

            navCache.Remove(match);
            navCache.Add(match);
            if (navCache.Count > RegionalCacheCapacity)
            {
                if (navCache[0] != activeSnapshot)
                {
                    Destroy(navCache[0].Data);
                    navCache.RemoveAt(0);
                }
            }
            activeSnapshot = match;
            localNav = instance;
            return true;
        }

        public void ReleaseNavigation()
        {
            if (localNav.valid) localNav.Remove();
            foreach (var snap in navCache)
                if (snap.Data != null) Destroy(snap.Data);
            navCache.Clear();
            activeSnapshot = null;
        }

        private bool Matches(RegionalNavSnapshot snap)
        {
            if (snap.Sources.Length != navSources.Count || !snap.Origin.Equals(transform.position) ||
                !snap.Rotation.Equals(transform.rotation))
                return false;
            for (int i = 0; i < snap.Sources.Length; i++)
            {
                var a = snap.Sources[i];
                var b = navSources[i];
                if (a.shape != b.shape || a.area != b.area || a.sourceObject != b.sourceObject ||
                    a.component != b.component || !a.size.Equals(b.size) || !a.transform.Equals(b.transform))
                    return false;
            }
            return true;
        }

        private static void Toggle(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active) go.SetActive(active);
        }
    }
}
