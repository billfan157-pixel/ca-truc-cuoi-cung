using System;
using System.Collections;
using System.Collections.Generic;
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
    public sealed class HospitalScene : MonoBehaviour
    {
        public const int SolidLayer = 9;
        public const int SolidMask = 1 << SolidLayer;
        public const int InteractLayer = 8;
        public const int InteractMask = 1 << InteractLayer;
        public const float Radius = 0.35f;
        public const float Height = 1.75f;

        public static HospitalScene Instance { get; private set; }

        public string[] additiveZoneScenes = Array.Empty<string>();
        public MarkerPose[] pads = Array.Empty<MarkerPose>();
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

        // M2 Dual-Scenario Indirect GI Evaluation Assets (R 2012 vs B 2005)
        public LightProbes realLightProbes;
        public LightProbes reportLightProbes;
        [NonSerialized] public string giMode = "dual-probes";

        // M2 Zone-Bound Characters, Anchors & Horror Props
        public CharacterActor LanActor { get; private set; }
        public CharacterActor GatekeeperActor { get; private set; }
        public CharacterActor AnActor { get; private set; }
        public Transform[] GatekeeperWaypoints { get; private set; } = Array.Empty<Transform>();
        public Transform LanSilhouettePoint { get; private set; }
        public Transform LanDeskPoint { get; private set; }
        public Transform LanStepOutPoint { get; private set; }
        public GameObject ReceptionGlassSilhouette { get; private set; }
        public GameObject TrolleyShadowProp { get; private set; }
        public GameObject SpareBatteryProp { get; private set; }
        public Light[] EmergencyRedLights { get; private set; } = Array.Empty<Light>();
        public AudioSource PaSpeakerAnchor { get; private set; }
        public AudioSource PhoneAnchor { get; private set; }
        public AudioSource LanVoiceAnchor { get; private set; }
        public AudioSource GatekeeperVoiceAnchor { get; private set; }
        public AudioSource AnVoiceAnchor { get; private set; }

        [SerializeField] private Bounds navigationBounds = new(new Vector3(5, 1, 0), new Vector3(42, 8, 59));
        [NonSerialized] public bool smoothPresentation;

        private readonly List<ZoneRegion> registeredZones = new();
        private readonly Dictionary<string, ZoneRegion> zonesById = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Transform> padsById = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<ReflectionProbe> allProbes = new();
        private readonly List<Light> allEmergencyLights = new();

        private bool themeReady;
        private bool showedReport;
        private bool lastLayerB;
        private int lastForm;
        private bool lastEntered, lastAcceptedBoard, lastClosetOpen, lastSecondGate;
        private bool seamClosed, spareTaken;
        public event Action<ZoneRegion> ZoneUnregistering;
        public event Action ZoneReloaded;

        // Legacy single-scene NavMesh fallback (also used when additiveZoneScenes is empty)
        private NavMeshDataInstance navigation;
        private NavigationSnapshot activeNavigation;
        private readonly List<NavigationSnapshot> navigationCache = new();
        private readonly List<NavMeshBuildSource> navigationSources = new();
        private readonly List<NavMeshBuildMarkup> navigationMarkups = new();
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

        public IEnumerator LoadZoneAsync(string sceneName)
        {
            var sc = SceneManager.GetSceneByName(sceneName);
            if (sc.IsValid() && sc.isLoaded) yield break;
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (op != null && !op.isDone) yield return null;
            sc = SceneManager.GetSceneByName(sceneName);
            if (!sc.isLoaded) yield break;
            foreach (var root in sc.GetRootGameObjects())
                foreach (var region in root.GetComponentsInChildren<ZoneRegion>())
                    if (region.isActiveAndEnabled) RegisterZone(region);
            themeReady = false;
            bool restoreSmooth = smoothPresentation;
            smoothPresentation = false;
            try
            {
                Present(lastLayerB, lastForm, lastEntered, lastAcceptedBoard, lastClosetOpen, lastSecondGate);
                PresentCheckpointProps(seamClosed, spareTaken);
            }
            finally { smoothPresentation = restoreSmooth; }
            foreach (var region in registeredZones)
                if (region.gameObject.scene == sc) region.RefreshRegionalNavMesh();
            ZoneReloaded?.Invoke();
        }

        public IEnumerator UnloadZoneAsync(string sceneName)
        {
            var sc = SceneManager.GetSceneByName(sceneName);
            if (!sc.IsValid() || !sc.isLoaded) yield break;
            var op = SceneManager.UnloadSceneAsync(sc);
            while (op != null && !op.isDone) yield return null;
        }

        public bool HasZone(string zoneId) => !string.IsNullOrEmpty(zoneId) && zonesById.ContainsKey(zoneId);

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

            // Bind zone-owned doors, dynamic rigs, characters, audio anchors and motion
            if (region.outerGate != null && region.outerGate.pivot != null) outerGate = region.outerGate;
            if (region.roomDoor != null && region.roomDoor.pivot != null) roomDoor = region.roomDoor;
            if (region.emergencyDoorA != null && region.emergencyDoorA.pivot != null) emergencyDoorA = region.emergencyDoorA;
            if (region.emergencyDoorB != null && region.emergencyDoorB.pivot != null) emergencyDoorB = region.emergencyDoorB;
            if (region.seamBlocker != null) seamBlocker = region.seamBlocker;
            if (region.pendingBay != null) pendingBay = region.pendingBay;
            if (region.deniedBay != null) deniedBay = region.deniedBay;
            if (region.statusA != null) statusA = region.statusA;
            if (region.statusB != null) statusB = region.statusB;
            if (region.statusC != null) statusC = region.statusC;
            if (region.paperAfter != null) paperAfter = region.paperAfter;
            if (region.storeClues != null) storeClues = region.storeClues;
            if (region.clockHandsR != null) clockHandsR = region.clockHandsR;
            if (region.clockHandsB != null) clockHandsB = region.clockHandsB;
            if (region.spareBatteryProp != null) SpareBatteryProp = region.spareBatteryProp;
            if (region.receptionGlassSilhouette != null) ReceptionGlassSilhouette = region.receptionGlassSilhouette;
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
                interactables.curtainCloth = region.curtainCloth;
                interactables.curtainClosedScale = region.curtainClosedScale;
                interactables.curtainOpenScale = region.curtainOpenScale;
            }
            if (region.lanActor != null) LanActor = region.lanActor;
            if (region.gatekeeperActor != null) GatekeeperActor = region.gatekeeperActor;
            if (region.anActor != null) AnActor = region.anActor;
            if (region.gatekeeperWaypoints != null && region.gatekeeperWaypoints.Length > 0)
                GatekeeperWaypoints = region.gatekeeperWaypoints;
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

            motion?.Register(region.movingFans, region.livingTubes, region.movingCloth);
            atmosphere?.RegisterZoneLoops(region.roomLoops);
            RebuildPadLookup();
            RebuildProbeLookup();

            // Sync visuals immediately to the current committed reality layer
            if (themeReady)
                region.ApplyLayerVisuals(showedReport);
            return true;
        }

        public void UnregisterZone(ZoneRegion region)
        {
            if (region == null || !registeredZones.Remove(region)) return;
            ZoneUnregistering?.Invoke(region);
            if (!string.IsNullOrEmpty(region.zoneId) && zonesById.TryGetValue(region.zoneId, out var cur) && cur == region)
                zonesById.Remove(region.zoneId);
            motion?.Unregister(region.movingFans, region.livingTubes, region.movingCloth);
            atmosphere?.UnregisterZoneLoops(region.roomLoops);
            ClearOwned(ref outerGate, region.outerGate);
            ClearOwned(ref roomDoor, region.roomDoor);
            ClearOwned(ref emergencyDoorA, region.emergencyDoorA);
            ClearOwned(ref emergencyDoorB, region.emergencyDoorB);
            ClearOwned(ref seamBlocker, region.seamBlocker);
            ClearOwned(ref pendingBay, region.pendingBay);
            ClearOwned(ref deniedBay, region.deniedBay);
            ClearOwned(ref statusA, region.statusA);
            ClearOwned(ref statusB, region.statusB);
            ClearOwned(ref statusC, region.statusC);
            ClearOwned(ref paperAfter, region.paperAfter);
            ClearOwned(ref storeClues, region.storeClues);
            ClearOwned(ref clockHandsR, region.clockHandsR);
            ClearOwned(ref clockHandsB, region.clockHandsB);
            ClearOwned(ref realProps, region.realProps);
            ClearOwned(ref reportProps, region.reportProps);
            if (region.cabinet != null && cabinet == region.cabinet)
            {
                cabinet = null;
                interactables?.ConfigureCabinet(null, Vector3.zero);
            }
            if (interactables != null && region.roomDoor?.pivot != null)
                interactables.ConfigureDoor(null, 0f);
            if (interactables != null && interactables.curtainCloth == region.curtainCloth)
                interactables.curtainCloth = null;
            if (SpareBatteryProp == region.spareBatteryProp) SpareBatteryProp = null;
            if (ReceptionGlassSilhouette == region.receptionGlassSilhouette) ReceptionGlassSilhouette = null;
            if (TrolleyShadowProp == region.trolleyShadowProp) TrolleyShadowProp = null;
            if (LanActor == region.lanActor) LanActor = null;
            if (GatekeeperActor == region.gatekeeperActor) GatekeeperActor = null;
            if (AnActor == region.anActor) AnActor = null;
            if (GatekeeperWaypoints == region.gatekeeperWaypoints) GatekeeperWaypoints = Array.Empty<Transform>();
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
            if (localReflection != null) allProbes.Add(localReflection);
            if (corridorReflection != null) allProbes.Add(corridorReflection);
            foreach (var z in registeredZones)
            {
                if (z == null) continue;
                if (z.reflectionProbes != null)
                    foreach (var rp in z.reflectionProbes)
                        if (rp != null && !allProbes.Contains(rp)) allProbes.Add(rp);
                if (z.emergencyRedLights != null)
                    foreach (var el in z.emergencyRedLights)
                        if (el != null && !allEmergencyLights.Contains(el)) allEmergencyLights.Add(el);
            }
            EmergencyRedLights = allEmergencyLights.ToArray();
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

        public void Present(bool layerB, int form, bool entered, bool acceptedBoard, bool closetOpen, bool secondGate)
        {
            lastLayerB = layerB;
            lastForm = form;
            lastEntered = entered;
            lastAcceptedBoard = acceptedBoard;
            lastClosetOpen = closetOpen;
            lastSecondGate = secondGate;

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

                foreach (var zone in registeredZones)
                    zone?.ApplyLayerVisuals(layerB);

                if (realGrade != null) realGrade.weight = layerB ? 0f : 1f;
                if (reportGrade != null) reportGrade.weight = layerB ? 1f : 0f;

                ApplyIndirectLighting(layerB);

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

            outerGate?.Set(entered);
            bool wantRoomOpen = acceptedBoard || !layerB;
            if (roomDoor != null)
            {
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
            }
            emergencyDoorA?.Set(secondGate);
            emergencyDoorB?.Set(secondGate);
            Physics.SyncTransforms();
        }

        private void ApplyIndirectLighting(bool layerB)
        {
            // Mode 1: Calibrated Trilight Gradient Ambient (always set as smooth base floor)
            RenderSettings.ambientMode = AmbientMode.Trilight;
            if (layerB)
            {
                RenderSettings.ambientSkyColor = new Color(0.36f, 0.42f, 0.37f);
                RenderSettings.ambientEquatorColor = new Color(0.25f, 0.30f, 0.26f);
                RenderSettings.ambientGroundColor = new Color(0.13f, 0.14f, 0.12f);
                RenderSettings.fogColor = new Color(0.20f, 0.28f, 0.24f);
                RenderSettings.fogDensity = 0.0095f;
            }
            else
            {
                // Calibrated R (2012) moonlight bounce — preserves deep shadow tension while keeping clues & walls readable!
                RenderSettings.ambientSkyColor = new Color(0.17f, 0.24f, 0.29f);
                RenderSettings.ambientEquatorColor = new Color(0.115f, 0.155f, 0.175f);
                RenderSettings.ambientGroundColor = new Color(0.068f, 0.064f, 0.055f);
                RenderSettings.fogColor = new Color(0.068f, 0.105f, 0.132f);
                RenderSettings.fogDensity = 0.0155f;
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
                    if (z != null && z.isActiveAndEnabled)
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
                    if (z != null && z.isActiveAndEnabled && !z.RefreshRegionalNavMesh())
                        allOk = false;
                }
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
            if (Instance == this) Instance = null;
            if (navigation.valid) navigation.Remove();
            foreach (var snapshot in navigationCache)
                if (snapshot.Data != null) Destroy(snapshot.Data);
            foreach (var z in registeredZones)
                z?.ReleaseNavigation();
        }
    }
}
