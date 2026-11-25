using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

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

    /// <summary>
    /// Persistent Bootstrap Coordinator for Ca Trực Cuối Cùng (M2 Additive Architecture).
    /// Orchestrates additive LOC01–LOC04 ZoneRegions, validates unique zone/pad IDs,
    /// manages regional NavMesh caches, dual-scenario indirect GI (Trilight / Dual Baked LightProbes / APV),
    /// staggered reflection probes, and atomic R/B world presentation.
    /// </summary>
    public sealed partial class HospitalScene : MonoBehaviour
    {
        public const int SolidLayer = 9;
        public const int SolidMask = 1 << SolidLayer;
        public const int InteractLayer = 8;
        public const int InteractMask = 1 << InteractLayer;
        public const int SightOccluderLayer = 10;
        public const int SightMask = SolidMask | 1 << SightOccluderLayer;
        public const float Radius = 0.35f;
        public const float Height = 1.75f;

        public static HospitalScene Instance { get; private set; }

        public string[] additiveZoneScenes = Array.Empty<string>();
        public string[] regionalStreamZoneScenes = Array.Empty<string>();
        public MarkerPose[] pads = Array.Empty<MarkerPose>();
        public InspectImage[] inspectImages = Array.Empty<InspectImage>();
        public InspectModel[] inspectModels = Array.Empty<InspectModel>();
        public DoorPose outerGate, roomDoor, staffCrossDoor, emergencyDoorA, emergencyDoorB, archiveExitDoor;
        public DoorPose s1DischargeDoor;
        public GameObject s1DischargeSalvage;
        public GameObject seamBlocker;
        public GameObject realProps, reportProps, memoryProps, pendingBay, deniedBay, refutedBay;
        public GameObject statusA, statusB, statusC, paperAfter, storeClues;
        public Renderer f101WallRenderer;
        public Material f101MatPending, f101MatA, f101MatB, f101MatC;
        private UnityEngine.UI.Text f101CorridorSlip;
        public Light bed4TubeLight;
        private float bed4TubeBaseIntensity;
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
        public Shader rakingPaperShader;
        public ParticleSystem[] atmosphericParticles = Array.Empty<ParticleSystem>();
        public bool referenceArt;

        // M2 Dual-Scenario Indirect GI Evaluation Assets (R 2012 vs B 2005)
        public LightProbes realLightProbes;
        public LightProbes reportLightProbes;
        [NonSerialized] public string giMode = "dual-probes";

        // M2/M3 Zone-Bound Characters, Anchors & Horror Props
        public CharacterActor LanActor { get; private set; }
        public CharacterActor GatekeeperActor { get; private set; }
        public CharacterActor AnActor { get; private set; }
        public CharacterActor OrderlyActor { get; private set; }
        public CharacterActor UnrecordedActor { get; private set; }
        public Transform OrderlyCart { get; private set; }
        public Transform[] GatekeeperWaypoints { get; private set; } = Array.Empty<Transform>();
        public Transform[] Ch03PatrolWaypoints { get; private set; } = Array.Empty<Transform>();
        public Transform Ch03RadioPoint { get; private set; }
        public Transform LanSilhouettePoint { get; private set; }
        public Transform LanDeskPoint { get; private set; }
        public Transform LanStepOutPoint { get; private set; }
        public GameObject ReceptionGlassSilhouette { get; private set; }
        public GameObject ReceptionBoard { get; private set; }
        public GameObject TrolleyShadowProp { get; private set; }
        public GameObject SpareBatteryProp { get; private set; }
        public Light[] EmergencyRedLights { get; private set; } = Array.Empty<Light>();
        public AudioSource PaSpeakerAnchor { get; private set; }
        public AudioSource PhoneAnchor { get; private set; }
        public AudioSource LanVoiceAnchor { get; private set; }
        public AudioSource GatekeeperVoiceAnchor { get; private set; }
        public AudioSource AnVoiceAnchor { get; private set; }

        [SerializeField] private Bounds navigationBounds = new(new Vector3(8, 3.6f, 20), new Vector3(92, 22, 108));
        [NonSerialized] public bool smoothPresentation;

        private readonly List<ZoneRegion> registeredZones = new();
        private readonly HashSet<string> loading = new(StringComparer.Ordinal);
        private readonly HashSet<string> unloading = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ZoneRegion> zonesById = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Transform> padsById = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<ReflectionProbe> allProbes = new();
        private readonly List<Light> allEmergencyLights = new();
        private readonly List<ParticleSystem> allAtmosphericParticles = new();
        private bool reducedMotionAtmosphere;

        private bool themeReady;
        private bool showedReport;
        private bool showedMemory;
        private bool lastLayerB, lastLayerM;
        private int lastForm;
        private bool lastEntered, lastAcceptedBoard, lastClosetOpen, lastSecondGate, lastThirdGate;
        private bool lastServiceGate, lastFifthGate, lastB07GateLatched, lastMemoryS1Discharge;
        private bool seamClosed, spareTaken;
        private Investigation lastCaseworkState;
        public event Action<ZoneRegion> ZoneUnregistering;
        public event Action ZoneReloaded;

        // Legacy single-scene NavMesh fallback (also used when additiveZoneScenes is empty)
        private NavMeshDataInstance navigation;
        private NavigationSnapshot activeNavigation;
        private readonly List<NavigationSnapshot> navigationCache = new();
        private readonly List<NavMeshBuildSource> navigationSources = new();
        private readonly List<NavMeshBuildMarkup> navigationMarkups = new();
        private readonly List<NavMeshLinkInstance> portalNavLinks = new();
        private const int NavigationCacheCapacity = 8;

        private static readonly ProfilerMarker CollectNavigationMarker = new("CTCC.Navigation.Collect");
        private static readonly ProfilerMarker BuildNavigationMarker = new("CTCC.Navigation.Build");
        private static readonly ProfilerMarker ProbeMarker = new("CTCC.Reflection.Schedule");

        private ReflectionProbe renderingProbe;
        private int reflectionRequest;
        private int reflectionVersion;
        private int renderingVersion;
        private int nextProbe;

        public bool IsAdditiveMode => additiveZoneScenes != null && additiveZoneScenes.Length > 0;
        public int LoadedZoneCount => registeredZones.Count;
        public int GameplayZoneCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < registeredZones.Count; i++)
                    if (registeredZones[i] != null && !registeredZones[i].isBackgroundCampus) count++;
                return count;
            }
        }

        public bool HasConfiguredRegionalScene(string sceneName)
        {
            if (regionalStreamZoneScenes == null || string.IsNullOrEmpty(sceneName)) return false;
            for (int i = 0; i < regionalStreamZoneScenes.Length; i++)
                if (string.Equals(regionalStreamZoneScenes[i], sceneName, StringComparison.Ordinal))
                    return true;
            return false;
        }

        public ZoneRegion ResolveZoneAt(Vector3 worldPos)
        {
            ZoneRegion best = null;
            int bestPriority = int.MinValue;
            for (int i = 0; i < registeredZones.Count; i++)
            {
                var z = registeredZones[i];
                if (z == null || !z.isActiveAndEnabled) continue;
                if (!z.ContainsPoint(worldPos)) continue;
                if (best == null || z.zonePriority > bestPriority)
                {
                    best = z;
                    bestPriority = z.zonePriority;
                }
            }
            return best;
        }

        public void PresentCasework(Investigation state)
        {
            lastCaseworkState = state;
            bool prevService = lastServiceGate;
            bool prevFifth = lastFifthGate;
            lastServiceGate = state.chapter is ("CH04" or "CH05");
            lastFifthGate = GateService.G05(state);
            foreach (var region in registeredZones)
            {
                Toggle(region.serviceConnector, !lastServiceGate || !HasZone("LOC09"));
                region.PresentCasework(state);
            }
            if (prevService != lastServiceGate && HasZone("LOC04"))
                RefreshZoneNavMesh("LOC04");
            if (prevFifth != lastFifthGate && HasZone("LOC05"))
                RefreshZoneNavMesh("LOC05");
            if (prevService == lastServiceGate && prevFifth == lastFifthGate && registeredZones.Count > 0)
                SyncPortalNavMeshLinks();
        }

        public bool RefreshZoneNavMesh(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId) || !zonesById.TryGetValue(zoneId, out var region) || region == null)
                return false;
            Physics.SyncTransforms();
            bool ok = !region.participatesInNavMesh || region.RefreshRegionalNavMesh();
            if (ok) SyncPortalNavMeshLinks();
            return ok;
        }

        private void ReleasePortalNavLinks()
        {
            for (int i = 0; i < portalNavLinks.Count; i++)
                if (NavMesh.IsLinkValid(portalNavLinks[i])) NavMesh.RemoveLink(portalNavLinks[i]);
            portalNavLinks.Clear();
        }

        /// <summary>
        /// Maintains lightweight bidirectional NavMeshLinks across loaded additive zone portals
        /// so runtime per-zone regional NavMesh tiles stitch cleanly across S1/S2/S3 and corridors (GAP-NAV-02).
        /// </summary>
        private void SyncPortalNavMeshLinks()
        {
            ReleasePortalNavLinks();
            if (registeredZones.Count == 0) return;

            TryAddPortalLink("LOC01", "LOC02", new Vector3(4.0f, 0f, -8.55f), new Vector3(4.0f, 0f, -7.45f), 1.8f);
            TryAddPortalLink("LOC02", "LOC03", new Vector3(4.0f, 0f, -0.55f), new Vector3(4.0f, 0f, 0.55f), 1.8f);
            TryAddPortalLink("LOC02", "LOC03", new Vector3(0.05f, 0f, -0.55f), new Vector3(0.05f, 0f, 0.55f), 1.2f);
            if (lastSecondGate)
            {
                TryAddPortalLink("LOC03", "LOC04", new Vector3(4.0f, 0f, 15.45f), new Vector3(4.0f, 0f, 16.55f), 1.6f);
                TryAddPortalLink("LOC03", "LOC04", new Vector3(0.05f, 0f, 15.45f), new Vector3(0.05f, 0f, 16.55f), 1.2f);
                TryAddPortalLink("LOC04", "LOC05", new Vector3(4.0f, 0f, 21.35f), new Vector3(4.0f, 0.36f, 22.90f), 2.0f);
            }
            if (!lastLayerB)
                TryAddPortalLink("LOC02", "LOC05", new Vector3(-2.35f, 0f, -4.0f), new Vector3(-3.70f, 0.40f, -4.0f), 2.0f);
            if (lastServiceGate && !lastLayerB)
                TryAddPortalLink("LOC04", "LOC09", new Vector3(7.0f, 0f, 21.35f), new Vector3(7.0f, -0.05f, 22.75f), 2.0f);
        }

        private void TryAddPortalLink(string zoneA, string zoneB, Vector3 pointA, Vector3 pointB, float width)
        {
            if (!HasZone(zoneA) || !HasZone(zoneB)) return;
            if (!NavMesh.SamplePosition(pointA, out var hitA, 0.85f, NavMesh.AllAreas) ||
                !NavMesh.SamplePosition(pointB, out var hitB, 0.85f, NavMesh.AllAreas))
                return;
            var link = NavMesh.AddLink(new NavMeshLinkData
            {
                startPosition = hitA.position,
                endPosition = hitB.position,
                width = width,
                costModifier = -1f,
                bidirectional = true,
                area = 0
            });
            if (NavMesh.IsLinkValid(link)) portalNavLinks.Add(link);
        }

        internal bool ServiceApproachBlocked =>
            zonesById.TryGetValue("LOC04", out var emergency) && emergency.serviceConnector != null &&
            emergency.serviceConnector.activeInHierarchy;

        private sealed class NavigationSnapshot
        {
            public NavMeshBuildSource[] Sources;
            public Vector3 Origin;
            public Quaternion Rotation;
            public NavMeshData Data;
        }

        private void Awake()
        {
            Instance = this;
            if (bed4TubeLight != null) bed4TubeBaseIntensity = bed4TubeLight.intensity;
            SubscribeRenderBudget();
            RebuildPadLookup();
            RebuildProbeLookup();

            // Register any ZoneRegions already present in open scenes (e.g. Editor multi-scene play)
            foreach (var region in FindObjectsByType<ZoneRegion>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (region.isActiveAndEnabled) RegisterZone(region);
        }

        /// <summary>
        /// Ensures all configured additive zone scenes (LOC01..LOC04) are loaded asynchronously
        /// before binding player, directors, saves, and navigation warm-up.
        /// Note: Unity queues LoadScene(Additive) for end-of-frame, so additive boot must await AsyncOperation.isDone.
        /// </summary>
        public IEnumerator EnsureAllZonesLoadedAsync()
        {
            if (!IsAdditiveMode) yield break;
            var pending = new List<AsyncOperation>();
            foreach (string sceneName in additiveZoneScenes)
            {
                if (string.IsNullOrEmpty(sceneName)) continue;
                var sc = SceneManager.GetSceneByName(sceneName);
                if (!sc.IsValid() || !sc.isLoaded)
                {
                    var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                    if (op != null) pending.Add(op);
                }
            }
            for (int i = 0; i < pending.Count; i++)
                while (!pending[i].isDone)
                    yield return null;
            foreach (var region in FindObjectsByType<ZoneRegion>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (region.isActiveAndEnabled) RegisterZone(region);
            RebuildPadLookup();
            RebuildProbeLookup();
        }

        public IEnumerator EnsureRegionalZonesStreamedAsync()
        {
            if (!IsAdditiveMode || regionalStreamZoneScenes == null) yield break;
            foreach (string sceneName in regionalStreamZoneScenes)
            {
                if (string.IsNullOrEmpty(sceneName)) continue;
                yield return LoadZoneAsync(sceneName);
            }
        }

        public IEnumerator LoadZoneAsync(string sceneName)
        {
            while (loading.Contains(sceneName) || unloading.Contains(sceneName)) yield return null;
            var sc = SceneManager.GetSceneByName(sceneName);
            if (sc.IsValid() && sc.isLoaded) yield break;
            loading.Add(sceneName);
            var timer = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                yield return LoadAndRegister(sceneName);
                Debug.Log($"CTCC_REGION_LOAD scene={sceneName} loaded={LoadedZoneCount} ms={timer.Elapsed.TotalMilliseconds:F2}");
            }
            finally { loading.Remove(sceneName); }
        }

        private IEnumerator LoadAndRegister(string sceneName)
        {
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (op != null && !op.isDone) yield return null;
            var sc = SceneManager.GetSceneByName(sceneName);
            if (!sc.isLoaded) yield break;
            foreach (var root in sc.GetRootGameObjects())
                foreach (var region in root.GetComponentsInChildren<ZoneRegion>())
                    if (region.isActiveAndEnabled) RegisterZone(region);
            themeReady = false;
            bool restoreSmooth = smoothPresentation;
            smoothPresentation = false;
            try
            {
                Present(lastLayerB, lastForm, lastEntered, lastAcceptedBoard, lastClosetOpen, lastSecondGate,
                    lastLayerM, lastThirdGate, lastServiceGate, b07GateLatched: lastB07GateLatched,
                    memoryS1Discharge: lastMemoryS1Discharge);
                PresentCheckpointProps(seamClosed, spareTaken);
                if (lastCaseworkState != null) PresentCasework(lastCaseworkState);
                Physics.SyncTransforms();
            }
            finally { smoothPresentation = restoreSmooth; }
            foreach (var region in registeredZones)
                if (region != null && region.isActiveAndEnabled && region.participatesInNavMesh)
                    region.RefreshRegionalNavMesh();
            SyncPortalNavMeshLinks();
            ZoneReloaded?.Invoke();
        }

        public IEnumerator UnloadZoneAsync(string sceneName)
        {
            while (loading.Contains(sceneName) || unloading.Contains(sceneName)) yield return null;
            var sc = SceneManager.GetSceneByName(sceneName);
            if (!sc.IsValid() || !sc.isLoaded) yield break;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            unloading.Add(sceneName);
            try
            {
                // Seal the entrance while the destination scene's colliders still exist.
                UpdateRegionalConnectors();
                Physics.SyncTransforms();
                var op = SceneManager.UnloadSceneAsync(sc);
                while (op != null && !op.isDone) yield return null;
            }
            finally
            {
                unloading.Remove(sceneName);
                Present(lastLayerB, lastForm, lastEntered, lastAcceptedBoard, lastClosetOpen, lastSecondGate,
                    lastLayerM, lastThirdGate, lastServiceGate, b07GateLatched: lastB07GateLatched,
                    memoryS1Discharge: lastMemoryS1Discharge);
                Physics.SyncTransforms();
                foreach (var region in registeredZones)
                    if (region != null && region.isActiveAndEnabled && region.participatesInNavMesh)
                        region.RefreshRegionalNavMesh();
                SyncPortalNavMeshLinks();
            }
            Debug.Log($"CTCC_REGION_UNLOAD scene={sceneName} loaded={LoadedZoneCount} ms={timer.Elapsed.TotalMilliseconds:F2}");
        }

        public bool HasZone(string zoneId) => !string.IsNullOrEmpty(zoneId) && zonesById.ContainsKey(zoneId);
        internal bool RegionReady(string zoneId, string sceneName) =>
            HasZone(zoneId) && !loading.Contains(sceneName) && !unloading.Contains(sceneName);

        /// <summary>
        /// Registers an additive ZoneRegion while enforcing strict M2 Handoff uniqueness
        /// invariants (zero duplicate zoneId, zero duplicate pad IDs).
        /// </summary>
        public bool RegisterZone(ZoneRegion region)
        {
            if (region == null || string.IsNullOrEmpty(region.zoneId)) return false;
            if (registeredZones.Contains(region)) return true;

            if (zonesById.ContainsKey(region.zoneId))
            {
                Debug.LogWarning($"CTCC M2-Handoff rejected duplicate ZoneRegion '{region.zoneId}'.");
                return false;
            }
            if (region.pads != null)
            {
                var localIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var pad in region.pads)
                {
                    if (pad == null || pad.point == null || string.IsNullOrWhiteSpace(pad.id) ||
                        pad.point.gameObject.scene != region.gameObject.scene ||
                        !localIds.Add(pad.id) || padsById.ContainsKey(pad.id))
                    {
                        Debug.LogWarning($"CTCC rejected ZoneRegion '{region.zoneId}' due to invalid or duplicate safe pad '{pad?.id}'.");
                        return false;
                    }
                }
            }

            registeredZones.Add(region);
            zonesById[region.zoneId] = region;
            RegisterPerformanceEntries(region);
            if (Application.isPlaying) RegisterChapterOneFieldObjects(region);

            // Bind zone-owned doors, dynamic rigs, characters, audio anchors and motion
            if (region.outerGate != null && region.outerGate.pivot != null)
            {
                outerGate = region.outerGate;
                ConfigureInteractiveDoor("GATE-CONG", outerGate);
                if (Application.isPlaying)
                {
                    Collider gateSolid = null;
                    foreach (var col in outerGate.pivot.GetComponentsInChildren<Collider>())
                    {
                        if (col.gameObject.layer == SolidLayer && !col.isTrigger) { gateSolid = col; break; }
                    }
                    foreach (var st in region.GetComponentsInChildren<SceneTarget>(true))
                    {
                        if (st.id == "SEAM")
                        {
                            // Keep SEAM target reachable from inside the courtyard even when the iron gate is shut in B07.
                            st.transform.position = new Vector3(4f, 1.22f, -19.88f);
                            if (st.TryGetComponent<BoxCollider>(out var bc))
                                bc.size = new Vector3(1.65f, 1.45f, 0.36f);
                            st.visibilitySurface = gateSolid;
                            st.handhold = st.transform;
                        }
                        else if (st.id == "GATE-CONG" && gateSolid != null)
                        {
                            st.visibilitySurface = gateSolid;
                        }
                    }
                }
            }
            if (Application.isPlaying && region.zoneId == "LOC02")
            {
                bool hasEmptyDesk = false;
                foreach (var st in region.GetComponentsInChildren<SceneTarget>(true))
                    if (st.id == "LAN_EMPTY_DESK") { hasEmptyDesk = true; break; }
                if (!hasEmptyDesk)
                {
                    var emptyDesk = AddTarget(region.transform, "LAN_EMPTY_DESK", "Bàn trực trống của Lan",
                        new Vector3(2.42f, 1.14f, -3.52f), new Vector3(0.68f, 0.62f, 0.56f));
                    emptyDesk.focusPriority = -1;
                }
            }
            if (region.roomDoor != null && region.roomDoor.pivot != null)
            {
                roomDoor = region.roomDoor;
                ConfigureInteractiveDoor("DOOR-101", roomDoor);
            }
            if (region.staffCrossDoor != null && region.staffCrossDoor.pivot != null)
            {
                staffCrossDoor = region.staffCrossDoor;
                ConfigureInteractiveDoor("DOOR-STAFF", staffCrossDoor);
            }
            if (region.emergencyDoorA != null && region.emergencyDoorA.pivot != null) emergencyDoorA = region.emergencyDoorA;
            if (region.emergencyDoorB != null && region.emergencyDoorB.pivot != null) emergencyDoorB = region.emergencyDoorB;
            if (region.archiveExitDoor != null && region.archiveExitDoor.pivot != null) archiveExitDoor = region.archiveExitDoor;
            if (region.s1DischargeDoor?.pivot != null) s1DischargeDoor = region.s1DischargeDoor;
            if (region.s1DischargeSalvage != null) s1DischargeSalvage = region.s1DischargeSalvage;
            if (region.seamBlocker != null) seamBlocker = region.seamBlocker;
            if (region.pendingBay != null) pendingBay = region.pendingBay;
            if (region.deniedBay != null) deniedBay = region.deniedBay;
            if (region.refutedBay != null) refutedBay = region.refutedBay;
            if (region.statusA != null) statusA = region.statusA;
            if (region.statusB != null) statusB = region.statusB;
            if (region.statusC != null) statusC = region.statusC;
            if (region.paperAfter != null) paperAfter = region.paperAfter;
            if (region.storeClues != null) storeClues = region.storeClues;
            if (region.f101WallRenderer != null)
            {
                f101WallRenderer = region.f101WallRenderer;
                f101MatPending = region.f101MatPending;
                f101MatA = region.f101MatA;
                f101MatB = region.f101MatB;
                f101MatC = region.f101MatC;
            }
            if (region.bed4TubeLight != null)
            {
                bed4TubeLight = region.bed4TubeLight;
                bed4TubeBaseIntensity = bed4TubeLight.intensity;
            }
            if (region.drawerBed3 != null)
                interactables?.ConfigureDrawer("DRAWER-BED3", region.drawerBed3, region.drawerBed3ClosedPos, region.drawerBed3OpenOffset);
            if (region.drawerDesk != null)
                interactables?.ConfigureDrawer("DRAWER-DESK", region.drawerDesk, region.drawerDeskClosedPos, region.drawerDeskOpenOffset);
            if (region.drawerNurse != null)
                interactables?.ConfigureDrawer("DRAWER-NURSE", region.drawerNurse, region.drawerNurseClosedPos, region.drawerNurseOpenOffset);
            if (region.clockHandsR != null) clockHandsR = region.clockHandsR;
            if (region.clockHandsB != null) clockHandsB = region.clockHandsB;
            if (region.spareBatteryProp != null) SpareBatteryProp = region.spareBatteryProp;
            if (region.receptionGlassSilhouette != null) ReceptionGlassSilhouette = region.receptionGlassSilhouette;
            if (region.receptionBoard != null) ReceptionBoard = region.receptionBoard;
            if (region.trolleyShadowProp != null) TrolleyShadowProp = region.trolleyShadowProp;
            if (region.cabinet != null)
            {
                cabinet = region.cabinet;
                cabinetClosed = region.cabinetClosed;
                cabinetOpen = region.cabinetOpen;
                interactables?.ConfigureCabinet(cabinet, cabinetClosed);
            }
            if (region.curtainCloth != null && interactables != null)
            {
                if (region.curtainHasPosTrack)
                    interactables.ConfigureCurtain(region.curtainCloth, region.curtainClosedScale, region.curtainOpenScale, region.curtainClosedPos, region.curtainOpenPos, true);
                else
                {
                    interactables.curtainCloth = region.curtainCloth;
                    interactables.curtainClosedScale = region.curtainClosedScale;
                    interactables.curtainOpenScale = region.curtainOpenScale;
                    interactables.curtainHasPosTrack = false;
                }
            }
            if (region.lanActor != null) LanActor = region.lanActor;
            if (region.gatekeeperActor != null) GatekeeperActor = region.gatekeeperActor;
            if (region.anActor != null) AnActor = region.anActor;
            if (region.orderlyActor != null) OrderlyActor = region.orderlyActor;
            if (region.unrecordedActor != null) UnrecordedActor = region.unrecordedActor;
            if (region.orderlyCart != null) OrderlyCart = region.orderlyCart;
            if (region.gatekeeperWaypoints != null && region.gatekeeperWaypoints.Length > 0)
                GatekeeperWaypoints = region.gatekeeperWaypoints;
            if (region.ch03PatrolWaypoints != null && region.ch03PatrolWaypoints.Length > 0)
                Ch03PatrolWaypoints = region.ch03PatrolWaypoints;
            if (region.ch03RadioPoint != null)
                Ch03RadioPoint = region.ch03RadioPoint;
            if (region.lanSilhouettePoint != null) LanSilhouettePoint = region.lanSilhouettePoint;
            if (region.lanDeskPoint != null) LanDeskPoint = region.lanDeskPoint;
            if (region.lanStepOutPoint != null) LanStepOutPoint = region.lanStepOutPoint;
            if (region.paSpeakerAnchor != null) PaSpeakerAnchor = region.paSpeakerAnchor;
            if (region.phoneAnchor != null) PhoneAnchor = region.phoneAnchor;
            if (region.lanVoiceAnchor != null) LanVoiceAnchor = region.lanVoiceAnchor;
            if (region.gatekeeperVoiceAnchor != null) GatekeeperVoiceAnchor = region.gatekeeperVoiceAnchor;
            if (region.anVoiceAnchor != null) AnVoiceAnchor = region.anVoiceAnchor;

            if (region.realProps != null && realProps == null) realProps = region.realProps;
            if (region.reportProps != null && reportProps == null) reportProps = region.reportProps;
            if (region.memoryProps != null && memoryProps == null) memoryProps = region.memoryProps;

            motion?.Register(region.movingFans, region.livingTubes, region.movingCloth);
            atmosphere?.RegisterZoneLoops(region.roomLoops);
            RebuildPadLookup();
            RebuildProbeLookup();

            // Sync visuals immediately to the current committed reality layer
            if (themeReady)
                region.ApplyLayerVisuals(showedReport, showedMemory);
            return true;
        }

        public void UnregisterZone(ZoneRegion region)
        {
            if (region == null || !registeredZones.Remove(region)) return;
            UnregisterPerformanceEntries(region);
            ZoneUnregistering?.Invoke(region);
            if (!string.IsNullOrEmpty(region.zoneId) && zonesById.TryGetValue(region.zoneId, out var cur) && cur == region)
                zonesById.Remove(region.zoneId);
            motion?.Unregister(region.movingFans, region.livingTubes, region.movingCloth);
            atmosphere?.UnregisterZoneLoops(region.roomLoops);
            ClearOwned(ref outerGate, region.outerGate);
            ClearOwned(ref roomDoor, region.roomDoor);
            ClearOwned(ref staffCrossDoor, region.staffCrossDoor);
            ClearOwned(ref emergencyDoorA, region.emergencyDoorA);
            ClearOwned(ref emergencyDoorB, region.emergencyDoorB);
            ClearOwned(ref archiveExitDoor, region.archiveExitDoor);
            ClearOwned(ref s1DischargeDoor, region.s1DischargeDoor);
            ClearOwned(ref s1DischargeSalvage, region.s1DischargeSalvage);
            ClearOwned(ref seamBlocker, region.seamBlocker);
            ClearOwned(ref pendingBay, region.pendingBay);
            ClearOwned(ref deniedBay, region.deniedBay);
            ClearOwned(ref refutedBay, region.refutedBay);
            ClearOwned(ref statusA, region.statusA);
            ClearOwned(ref statusB, region.statusB);
            ClearOwned(ref statusC, region.statusC);
            ClearOwned(ref paperAfter, region.paperAfter);
            ClearOwned(ref storeClues, region.storeClues);
            ClearOwned(ref f101WallRenderer, region.f101WallRenderer);
            ClearOwned(ref bed4TubeLight, region.bed4TubeLight);
            ClearOwned(ref clockHandsR, region.clockHandsR);
            ClearOwned(ref clockHandsB, region.clockHandsB);
            ClearOwned(ref realProps, region.realProps);
            ClearOwned(ref reportProps, region.reportProps);
            ClearOwned(ref memoryProps, region.memoryProps);
            if (region.cabinet != null && cabinet == region.cabinet)
            {
                cabinet = null;
                interactables?.ConfigureCabinet(null, Vector3.zero);
            }
            if (interactables != null && region.roomDoor?.pivot != null)
                interactables.ConfigureDoor(null, 0f);
            if (interactables != null && interactables.curtainCloth == region.curtainCloth)
                interactables.curtainCloth = null;
            if (region.zoneId == "LOC02") receptionLedgerTarget = null;
            if (region.zoneId == "LOC03") clipboardTarget = null;
            if (PedalBin == region.pedalBin) PedalBin = null;
            if (SpareBatteryProp == region.spareBatteryProp) SpareBatteryProp = null;
            if (ReceptionGlassSilhouette == region.receptionGlassSilhouette) ReceptionGlassSilhouette = null;
            if (ReceptionBoard == region.receptionBoard) ReceptionBoard = null;
            if (TrolleyShadowProp == region.trolleyShadowProp) TrolleyShadowProp = null;
            if (LanActor == region.lanActor) LanActor = null;
            if (GatekeeperActor == region.gatekeeperActor) GatekeeperActor = null;
            if (AnActor == region.anActor) AnActor = null;
            if (OrderlyActor == region.orderlyActor) OrderlyActor = null;
            if (UnrecordedActor == region.unrecordedActor) UnrecordedActor = null;
            if (OrderlyCart == region.orderlyCart) OrderlyCart = null;
            if (GatekeeperWaypoints == region.gatekeeperWaypoints) GatekeeperWaypoints = Array.Empty<Transform>();
            if (Ch03PatrolWaypoints == region.ch03PatrolWaypoints) Ch03PatrolWaypoints = Array.Empty<Transform>();
            if (Ch03RadioPoint == region.ch03RadioPoint) Ch03RadioPoint = null;
            if (LanSilhouettePoint == region.lanSilhouettePoint) LanSilhouettePoint = null;
            if (LanDeskPoint == region.lanDeskPoint) LanDeskPoint = null;
            if (LanStepOutPoint == region.lanStepOutPoint) LanStepOutPoint = null;
            if (PaSpeakerAnchor == region.paSpeakerAnchor) PaSpeakerAnchor = null;
            if (PhoneAnchor == region.phoneAnchor) PhoneAnchor = null;
            if (LanVoiceAnchor == region.lanVoiceAnchor) LanVoiceAnchor = null;
            if (GatekeeperVoiceAnchor == region.gatekeeperVoiceAnchor) GatekeeperVoiceAnchor = null;
            if (AnVoiceAnchor == region.anVoiceAnchor) AnVoiceAnchor = null;
            RebuildPadLookup();
            RebuildProbeLookup();
        }

        private static void ClearOwned<T>(ref T binding, T owner) where T : class
        {
            if (ReferenceEquals(binding, owner)) binding = null;
        }

        private void RebuildPadLookup()
        {
            padsById.Clear();
            if (pads != null)
                foreach (var p in pads)
                    if (p != null && !string.IsNullOrEmpty(p.id) && p.point != null)
                        padsById[p.id] = p.point;
            foreach (var z in registeredZones)
                if (z != null && z.pads != null)
                    foreach (var p in z.pads)
                        if (p != null && !string.IsNullOrEmpty(p.id) && p.point != null)
                            padsById[p.id] = p.point;
        }

        private void RebuildProbeLookup()
        {
            allProbes.Clear();
            allEmergencyLights.Clear();
            allAtmosphericParticles.Clear();
            if (localReflection != null) allProbes.Add(localReflection);
            if (corridorReflection != null) allProbes.Add(corridorReflection);
            if (atmosphericParticles != null)
                foreach (var ps in atmosphericParticles)
                    if (ps != null && !allAtmosphericParticles.Contains(ps)) allAtmosphericParticles.Add(ps);
            foreach (var z in registeredZones)
            {
                if (z == null) continue;
                if (z.reflectionProbes != null)
                    foreach (var rp in z.reflectionProbes)
                        if (rp != null && !allProbes.Contains(rp)) allProbes.Add(rp);
                if (z.emergencyRedLights != null)
                    foreach (var el in z.emergencyRedLights)
                        if (el != null && !allEmergencyLights.Contains(el)) allEmergencyLights.Add(el);
                if (z.atmosphericParticles != null)
                    foreach (var ps in z.atmosphericParticles)
                        if (ps != null && !allAtmosphericParticles.Contains(ps)) allAtmosphericParticles.Add(ps);
            }
            EmergencyRedLights = allEmergencyLights.ToArray();
            ApplyAtmosphereSettings(reducedMotionAtmosphere);
        }

        public void ApplyAtmosphereSettings(bool reducedMotion)
        {
            reducedMotionAtmosphere = reducedMotion;
            for (int i = 0; i < allAtmosphericParticles.Count; i++)
            {
                var ps = allAtmosphericParticles[i];
                if (ps == null) continue;
                var main = ps.main;
                main.simulationSpeed = reducedMotion ? 0.14f : 1f;
            }
        }

        private void ConfigureInteractiveDoor(string id, DoorPose door)
        {
            interactables?.ConfigureDoor(id, door.pivot, door.isOpen ? door.openAngle : 0f, door.openAngle);
            if (!Application.isPlaying) return;
            var target = door.pivot.GetComponentInChildren<SceneTarget>();
            if (target == null || target.id != id) return;
            foreach (var collider in door.pivot.GetComponentsInChildren<Collider>())
            {
                if (collider.gameObject.layer != SolidLayer || collider.isTrigger) continue;
                target.visibilitySurface = collider;
                break;
            }
        }

        private void RegisterChapterOneFieldObjects(ZoneRegion region)
        {
            if (region.zoneId is "LOC02" or "LOC03")
                foreach (var point in region.GetComponentsInChildren<SceneTarget>(true))
                {
                    if (point.id == "AN01") receptionLedgerTarget = point;
                    if (point.id == "F101") clipboardTarget = point;
                }
            var models = region.GetComponentsInChildren<Transform>(true);
            foreach (var item in Ch01FieldObjects.All)
            {
                if (item.Zone != region.zoneId) continue;
                if (item.Observation.Id == Ch01FieldObjects.BinInterior && region.pedalBin == null) continue;
                var target = AddTarget(region.transform, item.Observation.Id, item.Prompt, item.Position, item.Size);
                target.fieldObject = item;
                target.focusPriority = -1;
                if (item.Observation.Id == Ch01FieldObjects.BinPedal && region.pedalBin?.pedal != null)
                {
                    PedalBin = region.pedalBin;
                    var pedal = PedalBin.pedal.GetComponentInChildren<Renderer>();
                    target.transform.position = pedal.bounds.center + Vector3.up * .13f;
                    target.GetComponent<BoxCollider>().size = new Vector3(.26f, .33f, .24f);
                    target.handhold = target.transform;
                }
                if (item.ViewModel != null)
                {
                    foreach (var model in models)
                        if (model.name == item.ViewModel)
                        {
                            target.inspectModel = model.gameObject;
                            var bounds = model.GetComponentInChildren<Renderer>()?.bounds;
                            if (bounds.HasValue)
                            {
                                target.transform.position = bounds.Value.center;
                                float allowance = item.Drawer == null ? .10f : .035f;
                                target.GetComponent<BoxCollider>().size = bounds.Value.size + Vector3.one * allowance;
                            }
                            if (item.Drawer != null) target.focusPriority = 0;
                            target.transform.SetParent(model.parent, true);
                            target.handhold = target.transform;
                            break;
                        }
                    if (item.Drawer != null && target.inspectModel == null) target.gameObject.SetActive(false);
                }
            }
            if (region.staffChair == null) return;
            var chair = region.staffChair;
            var closedPos = chair.localPosition;
            if (closedPos.x > 0.87f) closedPos.x = 0.87f;
            var openOffset = new Vector3(0.81f - closedPos.x, 0f, -0.17f);
            chair.localPosition = closedPos;
            var seat = AddTarget(chair, "CHAIR-STAFF", Ch01FieldObjects.StaffSeat, chair.position + new Vector3(0, .52f, 0),
                new Vector3(.46f, .48f, .44f));
            seat.visibilitySurface = chair.GetComponentInChildren<Collider>();
            interactables?.ConfigureDrawer("CHAIR-STAFF", chair, closedPos, openOffset, -17f);
        }

        private SceneTarget receptionLedgerTarget, clipboardTarget;
        internal Ch01PedalBin PedalBin { get; private set; }

        internal bool CanReachWorkSurface(Vector3 feet, string id)
        {
            bool clipboard = id == RealityTransaction.WardClipboard;
            var target = clipboard ? clipboardTarget : receptionLedgerTarget;
            if (target == null)
            {
                var sceneId = clipboard ? "F101" : "AN01";
                foreach (var point in GetComponentsInChildren<SceneTarget>(true))
                    if (point.id == sceneId) { target = point; break; }
                if (clipboard) clipboardTarget = target;
                else receptionLedgerTarget = target;
            }
            if (target == null || !target.isActiveAndEnabled) return false;
            Vector3 pointOnPaper = target.Contact;
            Vector3 hand = feet + Vector3.up * 1.24f;
            Vector3 eye = feet + Vector3.up * 1.58f;
            return (hand - pointOnPaper).sqrMagnitude <= SceneTarget.Reach * SceneTarget.Reach &&
                target.VisibleFrom(eye, pointOnPaper);
        }

        internal bool CanSeeWardChange(FormStatus decision, Transform eye)
        {
            if (decision == FormStatus.Affirmed && deniedBay != null && deniedBay.activeInHierarchy)
            {
                var front = deniedBay.transform.Find("Denied bay front");
                var side = deniedBay.transform.Find("Denied bay return");
                return SeePanel(front) || SeePanel(side);
            }
            if (decision == FormStatus.Unverified && interactables?.curtainCloth != null)
            {
                var fabric = interactables.curtainCloth;
                var renderer = fabric.GetComponent<Renderer>();
                return renderer != null && fabric.gameObject.activeInHierarchy && See(renderer.bounds.center, fabric);
            }
            if (decision != FormStatus.Refuted || refutedBay == null || !refutedBay.activeInHierarchy) return false;
            var track = refutedBay.transform.Find("RefutedBed4SharpWheels");
            return track != null && See(track.position + Vector3.up * .045f, track);

            bool SeePanel(Transform panel)
            {
                if (panel == null || !panel.TryGetComponent<Collider>(out var surface)) return false;
                return See(surface.ClosestPoint(eye.position), panel);
            }

            bool See(Vector3 position, Transform ownSurface)
            {
                Vector3 offset = position - eye.position;
                if (offset.sqrMagnitude > 15f || offset.sqrMagnitude < .025f ||
                    Vector3.Dot(eye.forward, offset.normalized) < .77f)
                    return false;
                return !Physics.Raycast(eye.position, offset.normalized, out var hit, offset.magnitude - .025f,
                           SightMask, QueryTriggerInteraction.Collide) || hit.transform.IsChildOf(ownSurface);
            }
        }

        private static SceneTarget AddTarget(Transform parent, string id, string title, Vector3 position, Vector3 size)
        {
            var target = new GameObject(id) { layer = InteractLayer };
            target.transform.SetParent(parent, false);
            target.transform.position = position;
            var area = target.AddComponent<BoxCollider>();
            area.size = size;
            area.isTrigger = true;
            var component = target.AddComponent<SceneTarget>();
            component.id = id;
            component.title = title;
            return component;
        }

        public Transform Pad(string id)
        {
            if (TryPad(id, out var pad)) return pad;
            throw new InvalidOperationException("Missing safe pad " + id);
        }

        public bool TryPad(string id, out Transform pad)
        {
            pad = null;
            return id != null && padsById.TryGetValue(id, out pad) && pad != null;
        }

        public void PresentCheckpointProps(bool closeSeam, bool takeSpare)
        {
            seamClosed = closeSeam;
            spareTaken = takeSpare;
            Toggle(seamBlocker, closeSeam);
            Toggle(SpareBatteryProp, !takeSpare);
        }

        public GameObject InspectTemplate(string id)
        {
            if (inspectModels == null) return null;
            foreach (var entry in inspectModels)
                if (entry.id == id)
                    return entry.template;
            return null;
        }

        /// <summary>
        /// Pre-warms BOTH R and B NavMesh snapshots + material/PSO variants behind startup fade
        /// so the very first R->B reality switch incurs zero NavMesh bake spike and zero shader compile hitch!
        /// </summary>
        public void WarmupNavigationAndShaders()
        {
            bool savedSmooth = smoothPresentation;
            smoothPresentation = false;
            try
            {
                // Pre-cache B (2005) navigation snapshot
                Present(true, 0, true, false, false, false);
                TryRefreshNavigation(Pad("AP02").position);
                // Pre-cache B with Room 101 open
                Present(true, 0, true, true, false, false);
                TryRefreshNavigation(Pad("AP04").position);
                // Return cleanly to initial R (2012) state and cache R snapshot
                Present(false, 0, false, false, false, false);
                TryRefreshNavigation(Pad("AP00").position);
            }
            finally
            {
                smoothPresentation = savedSmooth;
            }
        }

        public void Present(bool layerB, int form, bool entered, bool acceptedBoard, bool closetOpen, bool secondGate,
            bool layerM = false, bool thirdGate = false, bool serviceGate = false, bool deferF101Visuals = false,
            float? roomDoorOpenness = null, bool b07GateLatched = false, bool memoryS1Discharge = false)
        {
            lastLayerB = layerB;
            lastLayerM = layerM;
            lastForm = form;
            lastEntered = entered;
            lastAcceptedBoard = acceptedBoard;
            lastClosetOpen = closetOpen;
            lastSecondGate = secondGate;
            lastThirdGate = thirdGate;
            lastServiceGate = serviceGate;
            lastB07GateLatched = b07GateLatched;
            lastMemoryS1Discharge = memoryS1Discharge;
            UpdateRegionalConnectors();

            if (!themeReady || showedReport != layerB || showedMemory != layerM)
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

                foreach (var zone in registeredZones)
                    zone?.ApplyLayerVisuals(layerB, layerM);

                if (realGrade != null) realGrade.weight = layerB ? 0f : 1f;
                if (reportGrade != null) reportGrade.weight = layerB ? 1f : 0f;

                ApplyIndirectLighting(layerB, layerM);

                if (Application.isPlaying) reflectionVersion++;
                if (motion != null) motion.reportLayer = layerB;
                if (atmosphere != null)
                {
                    atmosphere.ReportLayer = layerB;
                    atmosphere.MemoryEvacuationLayer = layerM && memoryS1Discharge;
                }
                showedReport = layerB;
                showedMemory = layerM;
                themeReady = true;
            }
            else if (atmosphere != null)
            {
                atmosphere.MemoryEvacuationLayer = layerM && memoryS1Discharge;
            }

            // The denied bay has colliders and must be staged for the navigation preflight.
            Toggle(deniedBay, layerB && form == 2);
            if (!deferF101Visuals)
                PresentF101(layerB, (FormStatus)form);
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

            bool wantGateOpen = entered && !b07GateLatched;
            if (outerGate != null)
            {
                if (smoothPresentation && interactables != null && Application.isPlaying && outerGate.pivot != null)
                {
                    outerGate.isOpen = wantGateOpen;
                    interactables.MoveDoor("GATE-CONG", outerGate.pivot, wantGateOpen ? outerGate.openAngle : 0f, false,
                        wantGateOpen ? atmosphere : null);
                }
                else
                {
                    outerGate.Set(wantGateOpen);
                    if (outerGate.pivot != null)
                        interactables?.ConfigureDoor("GATE-CONG", outerGate.pivot, wantGateOpen ? outerGate.openAngle : 0f, outerGate.openAngle);
                }
            }
            bool wantRoomOpen = acceptedBoard || !layerB;
            if (roomDoor != null)
            {
                float targetRatio = roomDoorOpenness ?? 1f;
                if (smoothPresentation && interactables != null && Application.isPlaying && roomDoor.pivot != null)
                {
                    roomDoor.isOpen = wantRoomOpen && targetRatio > 0.15f;
                    float angle = wantRoomOpen ? roomDoor.openAngle * targetRatio : 0f;
                    interactables.MoveDoor(roomDoor.pivot, angle, false, atmosphere);
                }
                else
                {
                    roomDoor.Set(wantRoomOpen && targetRatio > 0.15f);
                    interactables?.ConfigureDoor("DOOR-101", roomDoor.pivot, wantRoomOpen ? roomDoor.openAngle * targetRatio : 0f, roomDoor.openAngle);
                    if (wantRoomOpen && roomDoorOpenness.HasValue)
                    {
                        roomDoor.isOpen = roomDoorOpenness.Value > 0.15f;
                        interactables?.RestorePose("DOOR-101", roomDoorOpenness.Value);
                    }
                }
            }
            emergencyDoorA?.Set(secondGate);
            emergencyDoorB?.Set(secondGate);
            archiveExitDoor?.Set(thirdGate);
            bool presentDay = !layerB && !layerM;
            s1DischargeDoor?.Set(presentDay || (layerM && memoryS1Discharge));
            Toggle(s1DischargeSalvage, presentDay);
            Physics.SyncTransforms();
        }

        private void UpdateRegionalConnectors()
        {
            bool upperAvailable = HasZone("LOC05") && !unloading.Contains(RegionStreamer.Upper);
            bool serviceAvailable = HasZone("LOC09") && !unloading.Contains(RegionStreamer.Service);
            foreach (var zone in registeredZones)
            {
                bool hasOpenS1 = zone.zoneId == "LOC02" && ((!lastLayerB && !lastLayerM) || lastMemoryS1Discharge);
                foreach (var connector in zone.upperConnectors)
                    Toggle(connector, !(lastSecondGate || hasOpenS1) || !upperAvailable);
                Toggle(zone.serviceConnector, !lastServiceGate || !serviceAvailable);
            }
        }

        internal void PresentF101(bool layerB, FormStatus committed)
        {
            FormStatus form = layerB ? committed : FormStatus.Pending;
            // Refuted has its own parted curtain. The interactive cloth belongs to the pending/unverified bay.
            Toggle(pendingBay, layerB && form is (FormStatus.Pending or FormStatus.Unverified));
            Toggle(refutedBay, form == FormStatus.Refuted);
            Toggle(statusA, form == FormStatus.Affirmed);
            Toggle(statusB, form == FormStatus.Unverified);
            Toggle(statusC, form == FormStatus.Refuted);
            Toggle(paperAfter, form != FormStatus.Pending);

            var paper = form switch
            {
                FormStatus.Affirmed => f101MatA,
                FormStatus.Unverified => f101MatB,
                FormStatus.Refuted => f101MatC,
                _ => f101MatPending
            };
            if (f101WallRenderer != null && paper != null && f101WallRenderer.sharedMaterial != paper)
                f101WallRenderer.sharedMaterial = paper;

            if (motion != null)
                motion.fanSpeedScale = form switch
                {
                    FormStatus.Affirmed => 1.14f,
                    FormStatus.Unverified => 0.56f,
                    FormStatus.Refuted => 0.82f,
                    _ => 1f
                };

            if (atmosphere != null) atmosphere.F101Report = form;
            PresentF101CorridorSlip(form);

            if (bed4TubeLight != null && bed4TubeBaseIntensity > 0f)
            {
                float intensity = bed4TubeBaseIntensity * (form switch
                {
                    FormStatus.Affirmed => 1.08f,
                    FormStatus.Refuted => 0.48f,
                    _ => 1f
                });
                if (motion != null)
                    motion.SetTubeBaseline(bed4TubeLight, intensity);
                else
                    bed4TubeLight.intensity = intensity;
            }
        }

        private void PresentF101CorridorSlip(FormStatus form)
        {
            if (!Application.isPlaying) return;
            if (f101CorridorSlip == null)
            {
                if (form == FormStatus.Pending) return;
                var slip = new GameObject("Phòng 101 · Phiếu ngoài cửa", typeof(RectTransform), typeof(Canvas),
                    typeof(UnityEngine.UI.Image));
                slip.transform.SetParent(transform, false);
                slip.transform.SetPositionAndRotation(new Vector3(5.46f, 1.66f, 3.78f), Quaternion.LookRotation(Vector3.right));
                slip.transform.localScale = Vector3.one * 0.00078f;
                ((RectTransform)slip.transform).sizeDelta = new Vector2(480f, 144f);
                slip.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                var paper = slip.GetComponent<UnityEngine.UI.Image>();
                paper.color = new Color(0.61f, 0.59f, 0.51f);
                paper.raycastTarget = false;

                var label = new GameObject("Dòng bàn giao", typeof(RectTransform));
                label.transform.SetParent(slip.transform, false);
                var rect = (RectTransform)label.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(19f, 9f);
                rect.offsetMax = new Vector2(-19f, -9f);
                f101CorridorSlip = label.AddComponent<UnityEngine.UI.Text>();
                f101CorridorSlip.font = Resources.Load<Font>("CTCC/BeVietnamPro-SemiBold");
                f101CorridorSlip.fontSize = 38;
                f101CorridorSlip.lineSpacing = 0.86f;
                f101CorridorSlip.alignment = TextAnchor.MiddleLeft;
                f101CorridorSlip.raycastTarget = false;
            }

            f101CorridorSlip.transform.parent.gameObject.SetActive(form != FormStatus.Pending);
            f101CorridorSlip.color = form == FormStatus.Refuted ? new Color(0.30f, 0.11f, 0.085f) : new Color(0.105f, 0.14f, 0.14f);
            f101CorridorSlip.text = "PHÒNG 101\n" + (form switch
            {
                FormStatus.Affirmed => "ĐÃ BÀN GIAO · 0",
                FormStatus.Unverified => "CHỜ XÁC NHẬN",
                FormStatus.Refuted => "KIỂM TRA LẠI",
                _ => ""
            });
        }

        private void ApplyIndirectLighting(bool layerB, bool layerM = false)
        {
            // Mode 1: Calibrated Trilight Gradient Ambient (always set as smooth base floor)
            RenderSettings.ambientMode = AmbientMode.Trilight;
            if (layerM)
            {
                // Warm golden-amber Memory (M) vignette ambient
                RenderSettings.ambientSkyColor = new Color(0.44f, 0.35f, 0.23f);
                RenderSettings.ambientEquatorColor = new Color(0.32f, 0.25f, 0.16f);
                RenderSettings.ambientGroundColor = new Color(0.16f, 0.12f, 0.08f);
                RenderSettings.fogColor = new Color(0.28f, 0.21f, 0.14f);
                RenderSettings.fogDensity = 0.0095f;
            }
            else if (layerB)
            {
                RenderSettings.ambientSkyColor = new Color(0.36f, 0.42f, 0.37f);
                RenderSettings.ambientEquatorColor = new Color(0.25f, 0.30f, 0.26f);
                RenderSettings.ambientGroundColor = new Color(0.13f, 0.14f, 0.12f);
                RenderSettings.fogColor = new Color(0.20f, 0.28f, 0.24f);
                RenderSettings.fogDensity = 0.0056f;
            }
            else
            {
                // Calibrated R (2012) moonlight bounce — preserves deep shadow tension while keeping multi-wing campus readable!
                RenderSettings.ambientSkyColor = new Color(0.17f, 0.24f, 0.29f);
                RenderSettings.ambientEquatorColor = new Color(0.115f, 0.155f, 0.175f);
                RenderSettings.ambientGroundColor = new Color(0.068f, 0.064f, 0.055f);
                RenderSettings.fogColor = new Color(0.068f, 0.105f, 0.132f);
                RenderSettings.fogDensity = 0.0082f;
            }

            // Mode 2: Dual-Scenario Pre-Baked LightProbes SH L2 (atomic 0.05 ms swap on R<->B!)
            if (giMode == "dual-probes")
            {
                var probes = layerB ? reportLightProbes : realLightProbes;
                if (probes != null)
                    LightmapSettings.lightProbes = probes;
            }
            else if (giMode == "trilight")
            {
                LightmapSettings.lightProbes = null;
            }
            else if (giMode == "apv")
            {
                // Mode 3: URP 17.3 Adaptive Probe Volumes Lighting Scenario swap
                try
                {
                    if (ProbeReferenceVolume.instance != null && ProbeReferenceVolume.instance.isInitialized)
                        ProbeReferenceVolume.instance.lightingScenario = layerB ? "Scenario_B" : "Scenario_R";
                }
                catch (Exception) { /* APV fallback to Trilight if scenario not baked in this build */ }
            }
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
            navigationSources.Clear();
            if (registeredZones.Count > 0)
            {
                foreach (var z in registeredZones)
                    if (z != null && z.isActiveAndEnabled && z.participatesInNavMesh)
                        z.CollectRegionalSources(navigationSources);
            }
            else
            {
                NavMeshBuilder.CollectSources(transform, SolidMask, NavMeshCollectGeometry.PhysicsColliders, 0,
                    navigationMarkups, navigationSources);
            }
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
            // In additive multi-scene mode, refresh per-zone regional NavMesh caches cleanly!
            // Unchanged zones hit their cache in <0.1 ms; only zones whose colliders changed rebuild!
            if (registeredZones.Count > 0)
            {
                bool allOk = true;
                foreach (var z in registeredZones)
                {
                    if (z != null && z.isActiveAndEnabled && z.participatesInNavMesh && !z.RefreshRegionalNavMesh())
                        allOk = false;
                }
                if (allOk) SyncPortalNavMeshLinks();
                return allOk && NavMesh.SamplePosition(landing, out _, 0.36f, NavMesh.AllAreas);
            }

            // Legacy single-scene fallback
            CollectNavigation();
            var proposed = navigationCache.Find(MatchesNavigation);
            if (proposed == activeNavigation && navigation.valid)
                return NavMesh.SamplePosition(landing, out _, 0.36f, NavMesh.AllAreas);
            bool missed = proposed == null;
            if (missed)
            {
                var data = BakeNavigation();
                if (data == null) return false;
                proposed = new NavigationSnapshot
                {
                    Sources = navigationSources.ToArray(),
                    Data = data,
                    Origin = transform.position,
                    Rotation = transform.rotation
                };
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

        public async Task<bool> TryRefreshNavigationAsync(Vector3 landing)
        {
            if (registeredZones.Count == 0) return TryRefreshNavigation(landing);
            foreach (var zone in registeredZones.ToArray())
                if (zone == null || !zone.isActiveAndEnabled || (zone.participatesInNavMesh && !await zone.RefreshRegionalNavMeshAsync()))
                    return false;
            SyncPortalNavMeshLinks();
            return NavMesh.SamplePosition(landing, out _, 0.36f, NavMesh.AllAreas);
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
            while (nextProbe < allProbes.Count)
            {
                var probe = allProbes[nextProbe++];
                if (probe == null || !probe.isActiveAndEnabled) continue;
                using var marker = ProbeMarker.Auto();
                reflectionRequest = probe.RenderProbe();
                if (reflectionRequest >= 0) renderingProbe = probe;
                break;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeRenderBudget();
            if (Instance == this) Instance = null;
            ReleasePortalNavLinks();
            if (navigation.valid) navigation.Remove();
            foreach (var snapshot in navigationCache)
                if (snapshot.Data != null) Destroy(snapshot.Data);
            foreach (var z in registeredZones)
                z?.ReleaseNavigation();
        }
    }
}
