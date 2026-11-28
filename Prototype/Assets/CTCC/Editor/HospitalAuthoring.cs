using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace CTCC.Editor
{
    public sealed partial class HospitalAuthoring
    {
        public const string Root = "Assets/CTCC";
        private const string BootstrapSceneFile = Root + "/Scenes/AnSinh_Bootstrap.unity";
        private const string LegacySceneFile = Root + "/Scenes/AnSinh.unity";
        private const string GeometryFile = Root + "/Generated/TileGeometry.asset";

        private sealed class ZoneSpec
        {
            public string id;
            public string name;
            public Bounds navBounds;
            public Bounds zoneBounds;
            public int priority;
            public bool isBootAdditive;
            public bool isBackgroundCampus;
            public bool participatesInNavMesh;
            public string[] portalNeighbors;
        }

        private static readonly ZoneSpec[] ZoneSpecs =
        {
            new()
            {
                id = "LOC01", name = "LOC01_Exterior",
                navBounds = new Bounds(new Vector3(4f, 1f, -16f), new Vector3(24f, 8f, 20f)),
                zoneBounds = new Bounds(new Vector3(4f, 1.8f, -17f), new Vector3(19f, 5.5f, 19f)),
                priority = 20, isBootAdditive = true, isBackgroundCampus = false, participatesInNavMesh = true,
                portalNeighbors = new[] { "LOC02", "CAMPUS_EXT" }
            },
            new()
            {
                id = "LOC02", name = "LOC02_Reception",
                navBounds = new Bounds(new Vector3(4f, 1f, -4f), new Vector3(14f, 8f, 12f)),
                zoneBounds = new Bounds(new Vector3(4f, 1.8f, -4f), new Vector3(11f, 4.4f, 8.2f)),
                priority = 20, isBootAdditive = true, isBackgroundCampus = false, participatesInNavMesh = true,
                portalNeighbors = new[] { "LOC01", "LOC03", "LOC05" }
            },
            new()
            {
                id = "LOC03", name = "LOC03_Ward101",
                navBounds = new Bounds(new Vector3(8.5f, 1f, 8f), new Vector3(22f, 8f, 20f)),
                zoneBounds = new Bounds(new Vector3(8f, 1.8f, 8f), new Vector3(19.5f, 4.4f, 16.2f)),
                priority = 20, isBootAdditive = true, isBackgroundCampus = false, participatesInNavMesh = true,
                portalNeighbors = new[] { "LOC02", "LOC04", "CAMPUS_EXT" }
            },
            new()
            {
                id = "LOC04", name = "LOC04_Emergency",
                navBounds = new Bounds(new Vector3(4f, 1f, 19f), new Vector3(14f, 8f, 10f)),
                zoneBounds = new Bounds(new Vector3(4f, 1.8f, 19f), new Vector3(11f, 4.4f, 7.6f)),
                priority = 20, isBootAdditive = true, isBackgroundCampus = false, participatesInNavMesh = true,
                portalNeighbors = new[] { "LOC03", "LOC05", "LOC09", "CAMPUS_WINGS" }
            },
            new()
            {
                id = "CAMPUS_EXT", name = "Campus_Grounds",
                navBounds = new Bounds(new Vector3(8f, 6f, 25f), new Vector3(116f, 24f, 130f)),
                zoneBounds = new Bounds(new Vector3(8f, 7f, 25f), new Vector3(120f, 26f, 136f)),
                priority = 1, isBootAdditive = true, isBackgroundCampus = true, participatesInNavMesh = false,
                portalNeighbors = new[] { "LOC01", "LOC03", "CAMPUS_WINGS" }
            },
            new()
            {
                id = "LOC05", name = "LOC05_ArchiveWing",
                navBounds = new Bounds(new Vector3(-4f, 5f, 20f), new Vector3(32f, 16f, 64f)),
                zoneBounds = new Bounds(new Vector3(0.5f, 7.4f, 21f), new Vector3(36f, 8.6f, 60f)),
                priority = 20, isBootAdditive = false, isBackgroundCampus = false, participatesInNavMesh = true,
                portalNeighbors = new[] { "LOC02", "LOC04", "CAMPUS_EXT", "CAMPUS_WINGS" }
            },
            new()
            {
                id = "LOC09", name = "LOC09_ServiceWing",
                navBounds = new Bounds(new Vector3(18f, -2f, 38f), new Vector3(46f, 11f, 36f)),
                zoneBounds = new Bounds(new Vector3(18f, -2.0f, 38f), new Vector3(46f, 4.6f, 34f)),
                priority = 20, isBootAdditive = false, isBackgroundCampus = false, participatesInNavMesh = true,
                portalNeighbors = new[] { "LOC04", "CAMPUS_WINGS" }
            },
            new()
            {
                id = "CAMPUS_WINGS", name = "Campus_Interiors",
                navBounds = new Bounds(new Vector3(8f, 4f, 28f), new Vector3(104f, 18f, 96f)),
                zoneBounds = new Bounds(new Vector3(8f, 4f, 28f), new Vector3(104f, 18f, 96f)),
                priority = 5, isBootAdditive = false, isBackgroundCampus = true, participatesInNavMesh = false,
                portalNeighbors = new[] { "LOC01", "LOC04", "LOC05", "LOC09", "CAMPUS_EXT" }
            },
        };

        private sealed class ZoneSlot
        {
            public string id;
            public string sceneName;
            public string scenePath;
            public Scene scene;
            public ZoneRegion region;
            public Transform permanent, real, report, memory;
            public readonly List<MarkerPose> pads = new();
            public readonly List<ReflectionProbe> probes = new();
            public readonly List<Transform> fans = new();
            public readonly List<Light> tubes = new();
            public readonly List<MeshFilter> cloth = new();
            public readonly List<ParticleSystem> atmosphericParticles = new();
            public readonly List<RoomLoop> loops = new();
            public readonly List<Light> emergencyRedLights = new();
        }

        [Serializable] private sealed class Layout
        {
            public Area[] rooms;
            public Aperture[] openings;
            public Pose[] pads, beds;
            public S1DischargeLayout s1Discharge;
            public float wallHeight, bedWidth, bedLength;
        }
        [Serializable] private sealed class S1DischargeLayout
        {
            public float x, y, z, width, height, firstTreadX;
        }
        [Serializable] private sealed class Area
        {
            public string id;
            public float x, z, w, d;
        }
        [Serializable] private sealed class Aperture
        {
            public string id;
            public float x, z, width;
            public bool alongX;
        }
        [Serializable] private sealed class Pose
        {
            public string id;
            public float x, y, z, yaw;
        }

        private readonly Layout layout;
        private readonly bool art;
        private readonly bool multiScene;
        private readonly Dictionary<string, Material> materials = new();
        private readonly Dictionary<string, ZoneSlot> zones = new();
        private readonly List<Light> livingTubes = new();
        private readonly List<MeshFilter> movingCloth = new();
        private readonly List<Transform> movingFans = new();
        private readonly List<ParticleSystem> atmosphericParticles = new();
        private readonly List<GameObject> staticArchitecture = new();
        private readonly List<GameObject> probeAnchoredStatic = new();
        private readonly Scene bootstrapScene;
        private readonly HospitalScene world;
        private readonly Transform permanent, real, report, memory;
        private Mesh geometry;

        private HospitalAuthoring(bool art, bool multiScene = true)
        {
            this.art = art;
            this.multiScene = art && multiScene;
            string path = Path.GetFullPath("../docs/prototype/layout.json");
            layout = JsonUtility.FromJson<Layout>(File.ReadAllText(path));
            Directory.CreateDirectory(Root + "/Scenes");
            Directory.CreateDirectory(Root + "/Generated");
            Directory.CreateDirectory(Root + "/Generated/Materials");
            AssetDatabase.Refresh();
            if (AssetDatabase.LoadAssetAtPath<Mesh>(GeometryFile) != null)
                AssetDatabase.DeleteAsset(GeometryFile);

            bootstrapScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (this.multiScene)
                EditorSceneManager.SaveScene(bootstrapScene, BootstrapSceneFile);
            world = new GameObject(this.multiScene ? "An Sinh · CTCC M2 Bootstrap" : "An Sinh · CTCC Greybox").AddComponent<HospitalScene>();
            world.referenceArt = art;
            world.interactables = world.gameObject.AddComponent<InteractableRig>();
            permanent = Group("Physical continuity", world.transform);
            real = Group("R · 2012", world.transform);
            report = Group("B · Report in 2005 dress", world.transform);
            memory = Group("M · Memory Vignette", world.transform);
            memory.gameObject.SetActive(false);
            world.realProps = real.gameObject;
            world.reportProps = report.gameObject;
            world.memoryProps = memory.gameObject;

            if (this.multiScene)
            {
                world.additiveZoneScenes = ZoneSpecs.Where(z => z.isBootAdditive).Select(z => z.name).ToArray();
                world.regionalStreamZoneScenes = ZoneSpecs.Where(z => !z.isBootAdditive).Select(z => z.name).ToArray();
                foreach (var spec in ZoneSpecs)
                {
                    var zScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                    string zPath = Root + "/Scenes/" + spec.name + ".unity";
                    EditorSceneManager.SaveScene(zScene, zPath);
                    SceneManager.SetActiveScene(zScene);
                    var zRoot = new GameObject("Zone · " + spec.id);
                    var region = zRoot.AddComponent<ZoneRegion>();
                    region.zoneId = spec.id;
                    region.zoneBounds = spec.zoneBounds;
                    region.zonePriority = spec.priority;
                    region.isBackgroundCampus = spec.isBackgroundCampus;
                    region.participatesInNavMesh = spec.participatesInNavMesh;
                    region.portalNeighbors = spec.portalNeighbors;
                    region.navigationBounds = spec.navBounds;
                    var zPerm = Group("Physical continuity", zRoot.transform);
                    var zReal = Group("R · 2012", zRoot.transform);
                    var zRep = Group("B · Report in 2005 dress", zRoot.transform);
                    var zMem = Group("M · Memory Vignette", zRoot.transform);
                    zMem.gameObject.SetActive(false);
                    region.realProps = zReal.gameObject;
                    region.reportProps = zRep.gameObject;
                    region.memoryProps = zMem.gameObject;
                    zones[spec.id] = new ZoneSlot
                    {
                        id = spec.id,
                        sceneName = spec.name,
                        scenePath = zPath,
                        scene = zScene,
                        region = region,
                        permanent = zPerm,
                        real = zReal,
                        report = zRep,
                        memory = zMem
                    };
                }
                SceneManager.SetActiveScene(bootstrapScene);
            }

            // Route safe checkpoint pads AP00–AP05 + CH03 AP06/AN03/AP07 to their owning ZoneRegions (or Bootstrap if greybox)
            var bootstrapPads = new List<MarkerPose>();
            var allPads = new List<Pose>(layout.pads)
            {
                new Pose { id = "AP06", x = 4.0f, y = 4f, z = 33.5f, yaw = 0f },
                new Pose { id = "AP10", x = 0.85f, y = 4f, z = 33.0f, yaw = -90f },
                new Pose { id = "AN03", x = -0.6f, y = 8f, z = 32.2f, yaw = -90f },
                new Pose { id = "AP11", x = 5.2f, y = 8f, z = 32.3f, yaw = 90f },
                new Pose { id = "AP07", x = 4.0f, y = 4f, z = 45.7f, yaw = 0f },
                new Pose { id = "AP08", x = 7f, y = -3.4f, z = 32.2f, yaw = 0f },
                new Pose { id = "AP09", x = 1.5f, y = -3.4f, z = 39f, yaw = -90f },
            };
            foreach (var p in allPads)
            {
                var pos = new Vector3(p.x, p.y + 0.015f, p.z);
                var slot = this.multiScene ? ZoneAt(pos) : null;
                Transform parent = slot?.permanent ?? permanent;
                var point = Group(p.id, parent, pos);
                point.rotation = Quaternion.Euler(0, art && multiScene && p.id == "AP02" ? -82f : p.yaw, 0);
                var mp = new MarkerPose { id = p.id, point = point };
                if (slot != null) slot.pads.Add(mp);
                else bootstrapPads.Add(mp);
            }
            world.pads = bootstrapPads.ToArray();
            PrepareMaterials();
        }

        [MenuItem("Ca Trực Cuối Cùng/1 — Dựng và kiểm mặt bằng")]
        public static void Greybox() => new HospitalAuthoring(false, multiScene: false).Generate(true);

        [MenuItem("Ca Trực Cuối Cùng/2 — Dựng cảnh M2 Vertical Slice (5 Additive Scenes)")]
        public static void ReferenceScene()
        {
            new HospitalAuthoring(true, multiScene: true).Generate(true);
            UpdateRebirthTwo();
        }

        public static void PrepareAndTest()
        {
            ReferenceScene();
            EditorApplication.isPlaying = true;
        }

        public static void TestScene()
        {
            EditorSceneManager.OpenScene(BootstrapSceneFile);
            EditorApplication.isPlaying = true;
        }

        public static void PrepareAndBuild()
        {
            ReferenceScene();
            Windows();
        }

        [MenuItem("Ca Trực Cuối Cùng/3 — Build Windows (M2 Additive)")]
        public static void Windows()
        {
            if (!File.Exists(BootstrapSceneFile))
                throw new InvalidOperationException("Build the M2 additive scenes first.");
            ConfigureEnvironment(multiScene: true);
            string destination = Argument("-ctcc-build") ?? "../output/m2/windows/CaTrucCuoiCung.exe";
            destination = Path.GetFullPath(destination);
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            var scenePaths = new List<string> { BootstrapSceneFile };
            scenePaths.AddRange(ZoneSpecs.Select(z => Root + "/Scenes/" + z.name + ".unity"));
            var options = new BuildPlayerOptions
            {
                scenes = scenePaths.ToArray(),
                locationPathName = destination,
                target = BuildTarget.StandaloneWindows64,
                options = Array.IndexOf(Environment.GetCommandLineArgs(), "-ctcc-development") >= 0
                    ? BuildOptions.Development : BuildOptions.None
            };
            var result = BuildPipeline.BuildPlayer(options);
            if (result.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("CTCC Windows build " + result.summary.result);
            Debug.Log($"CTCC_BUILD {result.summary.totalSize} bytes → {destination}");
        }

        private static string Argument(string key)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, key);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        private static void ConfigureEnvironment(bool multiScene)
        {
            PlayerSettings.companyName = "An Sinh Project";
            PlayerSettings.productName = "Ca trực cuối cùng";
            PlayerSettings.bundleVersion = "0.3.0-m2";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.enableFrameTimingStats = true;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D12, GraphicsDeviceType.Direct3D11 });
            if (multiScene)
            {
                var buildScenes = new List<EditorBuildSettingsScene> { new(BootstrapSceneFile, true) };
                foreach (var z in ZoneSpecs)
                    buildScenes.Add(new EditorBuildSettingsScene(Root + "/Scenes/" + z.name + ".unity", true));
                EditorBuildSettings.scenes = buildScenes.ToArray();
            }
            else
            {
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(LegacySceneFile, true) };
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 3D XYZ spatial zone classifier supporting multi-wing same-Z disambiguation (K-B/K-A/K-D)
        /// and multi-story elevation stacks (B1 -3.4m, Grade 0m, Floor 2 +4m, Floor 3 +8m).
        /// Preserves legacy LOC01–LOC05 & LOC09 ownership for all core gameplay zones and saves.
        /// </summary>
        private ZoneSlot ZoneAt(Vector3 worldPos)
        {
            if (worldPos.y < -0.50f) return zones["LOC09"];
            // Open-air LOC01 courtyard owns its overhead rain emitter & gate lamps cleanly regardless of elevation.
            if (worldPos.z < -8.01f)
                return worldPos.x >= -5.6f && worldPos.x <= 13.6f && worldPos.z >= -27.0f ? zones["LOC01"] : zones["CAMPUS_EXT"];
            // Cutoff at 3.88m keeps ground-floor ceilings (y=3.71m) in LOC02–LOC04 while routing 2F/3F (>=4.0m) to LOC05.
            if (worldPos.y > 3.88f)
            {
                if (worldPos.x >= -17.5f && worldPos.x <= 18.5f && worldPos.z >= -8.5f && worldPos.z <= 50.5f)
                    return zones["LOC05"];
                return zones["CAMPUS_EXT"];
            }
            if (worldPos.z < -0.01f)
                return worldPos.x >= -1.5f && worldPos.x <= 9.5f ? zones["LOC02"] : zones["CAMPUS_EXT"];
            if (worldPos.z < 15.99f)
                return worldPos.x >= -1.5f && worldPos.x <= 17.6f ? zones["LOC03"] : zones["CAMPUS_EXT"];
            if (worldPos.z < 23.50f)
                return worldPos.x >= -1.5f && worldPos.x <= 9.5f ? zones["LOC04"] : zones["CAMPUS_EXT"];
            return zones["CAMPUS_EXT"];
        }

        private ZoneSlot ZoneAt(float z) => ZoneAt(new Vector3(4f, 0f, z));

        private ZoneSlot ZoneForTransform(Transform t)
        {
            if (!multiScene || t == null) return null;
            var cur = t;
            while (cur != null)
            {
                var reg = cur.GetComponent<ZoneRegion>();
                if (reg != null && zones.TryGetValue(reg.zoneId, out var slot))
                    return slot;
                cur = cur.parent;
            }
            return ZoneAt(t.position);
        }

        private Transform ResolveParent(Transform parent, Vector3 worldPos)
        {
            if (!multiScene) return parent == null ? permanent : parent;
            if (parent == null || parent == permanent) return ZoneAt(worldPos).permanent;
            if (parent == real) return ZoneAt(worldPos).real;
            if (parent == report) return ZoneAt(worldPos).report;
            if (parent == memory) return ZoneAt(worldPos).memory;
            return parent;
        }

        private void AddFinish(LayerFinish finish)
        {
            if (finish == null || finish.renderer == null) return;
            if (multiScene)
            {
                var slot = ZoneForTransform(finish.renderer.transform);
                if (slot != null)
                {
                    slot.region.finishes.Add(finish);
                    return;
                }
            }
            world.finishes.Add(finish);
        }

        private void Generate(bool verify)
        {
            BuildShell();
            BuildFurnishing();
            BuildM2ZoneDressing();
            BuildFullCampus();
            if (art && multiScene) DressChapterOneSurfaces();
            MarkInteractions();
            LightAndStage();
            if (multiScene)
                FinalizeZoneRegions();
            else
                world.atmosphericParticles = atmosphericParticles.ToArray();

            if (art)
            {
                ApplyStaticFlags();
                GroupFixedSurfaces();
            }

            // Register zones into Bootstrap Coordinator in Editor so Present/VerifyBlockout/ProbeBaking work across all 5 scenes!
            if (multiScene)
            {
                foreach (var slot in zones.Values)
                    world.RegisterZone(slot.region);
            }

            world.Present(false, 0, false, false, false, false);
            if (verify)
                VerifyBlockout();

            // Save scenes first so Unity Lightmapper & StaticOcclusionCulling have valid on-disk scene assets!
            SaveAllScenes();

            if (art)
            {
                // Bake Dual-Scenario Indirect LightProbes (R 2012 & B 2005) + Multi-Scene Static Occlusion Culling!
                BakeDualLightProbes();
                BakeMultiSceneOcclusion();
                world.Present(false, 0, false, false, false, false);
                SaveAllScenes();
            }

            ConfigureEnvironment(multiScene);
            int finishCount = multiScene ? zones.Values.Sum(z => z.region.finishes.Count) : world.finishes.Count;
            Debug.Log($"CTCC_SCENE {(multiScene ? $"additive-{zones.Count}regions" : "greybox")} {finishCount} paired finishes, {staticArchitecture.Count} static objects");
        }

        private void FinalizeZoneRegions()
        {
            foreach (var slot in zones.Values)
            {
                slot.region.pads = slot.pads.ToArray();
                slot.region.reflectionProbes = slot.probes.ToArray();
                slot.region.movingFans = slot.fans.ToArray();
                slot.region.livingTubes = slot.tubes.ToArray();
                slot.region.movingCloth = slot.cloth.ToArray();
                slot.region.atmosphericParticles = slot.atmosphericParticles.ToArray();
                slot.region.roomLoops = slot.loops.ToArray();
                slot.region.emergencyRedLights = slot.emergencyRedLights.ToArray();
            }
        }

        private void SaveAllScenes()
        {
            SceneManager.SetActiveScene(bootstrapScene);
            if (multiScene)
            {
                foreach (var slot in zones.Values)
                    EditorSceneManager.SaveScene(slot.scene, slot.scenePath);
                EditorSceneManager.SaveScene(bootstrapScene, BootstrapSceneFile);
            }
            else
            {
                EditorSceneManager.SaveScene(bootstrapScene, LegacySceneFile);
            }
            AssetDatabase.SaveAssets();
        }

        private void BakeMultiSceneOcclusion()
        {
            SceneManager.SetActiveScene(bootstrapScene);
            StaticOcclusionCulling.smallestOccluder = 2.0f;
            StaticOcclusionCulling.smallestHole = 0.25f;
            StaticOcclusionCulling.backfaceThreshold = 100f;
            bool ok = StaticOcclusionCulling.Compute();
            Debug.Log($"CTCC_OCCLUSION_BAKE success={ok} activeScene={bootstrapScene.name} loadedScenes={SceneManager.sceneCount}");
        }

        private void ApplyStaticFlags()
        {
            var flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic |
                        StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic |
                        StaticEditorFlags.ContributeGI;
            foreach (var go in staticArchitecture)
            {
                if (go == null) continue;
                ApplyStaticRecursive(go, flags);
            }
            // Probe-anchored corridor walls keep GI/occlusion/reflection static flags WITHOUT BatchingStatic
            // so Unity evaluates each wall segment's exact interior LightProbe anchor cleanly!
            var anchoredFlags = StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic |
                                StaticEditorFlags.ReflectionProbeStatic | StaticEditorFlags.ContributeGI;
            foreach (var go in probeAnchoredStatic)
            {
                if (go == null) continue;
                ApplyStaticRecursive(go, anchoredFlags);
            }
        }

        private static void ApplyStaticRecursive(GameObject go, StaticEditorFlags flags)
        {
            GameObjectUtility.SetStaticEditorFlags(go, flags);
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                // ReceiveGI.LightProbes lets static walls/props bounce GI into probes while swapping R<->B instantaneously!
                mr.receiveGI = ReceiveGI.LightProbes;
            }
            foreach (Transform child in go.transform)
                ApplyStaticRecursive(child.gameObject, flags);
        }

        private static Transform Group(string name, Transform parent, Vector3 position = default)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.localPosition = position;
            return child;
        }

        private GameObject Box(string name, Vector3 p, Vector3 size, Material material,
            Transform parent = null, bool solid = false, float tile = 0, bool isStatic = false)
        {
            Transform resolved = ResolveParent(parent, p);
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(resolved, false);
            box.transform.localPosition = p;
            box.transform.localScale = size;
            if (solid)
                box.layer = HospitalScene.SolidLayer;
            else
                UnityEngine.Object.DestroyImmediate(box.GetComponent<BoxCollider>());
            var renderer = box.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            if (art && tile > 0)
                TileCube(box.GetComponent<MeshFilter>(), p, size, tile);
            if (isStatic) staticArchitecture.Add(box);
            return box;
        }

        private void TileCube(MeshFilter filter, Vector3 position, Vector3 size, float metres)
        {
            var mesh = UnityEngine.Object.Instantiate(filter.sharedMesh);
            mesh.name = filter.name;
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var coordinates = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 p = Vector3.Scale(vertices[i], size) + position;
                coordinates[i] = Mathf.Abs(normals[i].y) > 0.90f
                    ? new Vector2(p.x, p.z) / metres
                    : new Vector2(Mathf.Abs(normals[i].x) > 0.90f ? p.z : p.x, p.y) / metres;
            }
            mesh.uv = coordinates;
            mesh.RecalculateTangents();
            StoreMesh(mesh);
            filter.sharedMesh = mesh;
        }

        private void StoreMesh(Mesh mesh)
        {
            if (geometry == null)
            {
                geometry = mesh;
                AssetDatabase.CreateAsset(mesh, GeometryFile);
            }
            else
                AssetDatabase.AddObjectToAsset(mesh, GeometryFile);
        }

        private GameObject Surface(string name, Vector3 p, Vector3 size, string r, string b,
            bool solid = false, float tile = 0, bool isStatic = true, Transform parent = null)
        {
            var item = Box(name, p, size, Mat(r), parent: parent, solid: solid, tile: tile, isStatic: isStatic);
            AddFinish(new LayerFinish { renderer = item.GetComponent<Renderer>(), real = Mat(r), report = Mat(b) });
            return item;
        }

        private void Floor(string id, bool covered = true)
        {
            var a = layout.rooms.Single(room => room.id == id);
            Surface(id + " · floor", new Vector3(a.x + a.w / 2, -0.10f, a.z + a.d / 2),
                new Vector3(a.w, 0.20f, a.d), "FloorR", "FloorB", true, 1.6f, isStatic: true);
            if (covered)
                Surface(id + " · ceiling", new Vector3(a.x + a.w / 2, 3.71f, a.z + a.d / 2),
                    new Vector3(a.w, 0.22f, a.d), "PlasterR", "PlasterB", true, 2.2f, isStatic: true);
        }

        private void Wall(bool alongX, float axis, float from, float to, float bottom = 0, float top = 3.6f, float? probeX = null)
        {
            if (to - from < 0.02f || top - bottom < 0.01f) return;
            const float thickness = 0.20f;
            void Segment(string name, float low, float high, string r, string b, bool solid)
            {
                var middle = (from + to) * 0.5f;
                var point = alongX ? new Vector3(middle, (low + high) * 0.5f, axis) : new Vector3(axis, (low + high) * 0.5f, middle);
                var scale = alongX ? new Vector3(to - from, high - low, thickness) : new Vector3(thickness, high - low, to - from);
                bool customAnchor = art && !alongX && probeX.HasValue;
                var go = Surface(name, point, scale, r, b, solid, 2.4f, isStatic: !customAnchor);
                if (customAnchor)
                {
                    var anchor = new GameObject(name + " · probeAnchor").transform;
                    anchor.SetParent(go.transform.parent, false);
                    anchor.position = new Vector3(probeX.Value, (low + high) * 0.5f, middle);
                    go.GetComponent<MeshRenderer>().probeAnchor = anchor;
                    probeAnchoredStatic.Add(go);
                }
            }

            float split = Mathf.Clamp(1.15f, bottom, top);
            if (split > bottom)
                Segment("Wainscot", bottom, split, "DadoR", "DadoB", true);
            if (top > split)
                Segment("Paint over lime plaster", split, top, "PlasterR", "PlasterB", true);

            if (art && bottom < 0.01f)
            {
                float middle = (from + to) * 0.5f;
                Vector3 p = alongX ? new Vector3(middle, 0.048f, axis) : new Vector3(axis, 0.048f, middle);
                Vector3 size = alongX ? new Vector3(to - from, 0.096f, 0.224f) : new Vector3(0.224f, 0.096f, to - from);
                Box("Terrazzo cove skirting", p, size, Mat("BaseTrim"), tile: 0.8f, isStatic: true);

                if (top > 1.18f)
                {
                    Vector3 dp = alongX ? new Vector3(middle, 1.152f, axis) : new Vector3(axis, 1.152f, middle);
                    Vector3 ds = alongX ? new Vector3(to - from, 0.022f, 0.214f) : new Vector3(0.214f, 0.022f, to - from);
                    Box("Dado border stripe", dp, ds, Mat("Frame"), isStatic: true);
                }
            }
        }

        private void WallOpening(bool alongX, float axis, float from, float to, float opening, float width, float clear, float? probeX = null)
        {
            float half = width * 0.5f;
            Wall(alongX, axis, from, opening - half, probeX: probeX);
            Wall(alongX, axis, opening + half, to, probeX: probeX);
            Wall(alongX, axis, opening - half, opening + half, clear, layout.wallHeight, probeX: probeX);
        }

        private Aperture Opening(string id) => layout.openings.Single(e => e.id == id);

        private void BuildShell()
        {
            foreach (var a in layout.rooms)
                Floor(a.id, a.id != "LOC01");
            if (!art)
                Box("Approach at grade", new Vector3(4, -0.1f, -22), new Vector3(9, 0.2f, 4), Mat("Exterior"), solid: true, tile: 2f, isStatic: true);

            // Courtyard perimeter low walls: keep exact solid colliders; in art mode CTCC_ENV_CourtyardFenceBay
            // sits flush at y=0 (0.90m brick plinth + coping + iron spear railings + 2.28m piers, zero double-stacking!).
            if (art)
            {
                Box("West yard brick wall", new Vector3(-5f, 1.125f, -14f), new Vector3(0.24f, 2.25f, 12f), Mat("WeatheredBrick"), solid: true, tile: 1.6f, isStatic: true).GetComponent<Renderer>().enabled = false;
                Box("East yard brick wall", new Vector3(13f, 1.125f, -14f), new Vector3(0.24f, 2.25f, 12f), Mat("WeatheredBrick"), solid: true, tile: 1.6f, isStatic: true).GetComponent<Renderer>().enabled = false;
                Box("Front yard brick wall L", new Vector3(-1.33f, 1.14f, -20f), new Vector3(7.34f, 2.28f, 0.26f), Mat("WeatheredBrick"), solid: true, tile: 1.6f, isStatic: true).GetComponent<Renderer>().enabled = false;
                Box("Front yard brick wall R", new Vector3(9.33f, 1.14f, -20f), new Vector3(7.34f, 2.28f, 0.26f), Mat("WeatheredBrick"), solid: true, tile: 1.6f, isStatic: true).GetComponent<Renderer>().enabled = false;
            }
            else
            {
                Wall(false, -5, -20, -8, 0, 2.25f);
                Wall(false, 13, -20, -8, 0, 2.25f);
                Wall(true, -20, -5, 2.8f, 0, 2.28f);
                Wall(true, -20, 5.2f, 13, 0, 2.28f);
            }

            var gate = Opening("GATE-CONG");
            var outerGate = HingedExteriorGate("GATE-CONG", gate, -101);
            var seam = Box("Exhausted gate seam only", new Vector3(4, 1.28f, -20.50f),
                new Vector3(2.44f, 2.56f, 0.07f), Mat("Glass"), solid: true);
            seam.GetComponent<Renderer>().enabled = false;
            seam.SetActive(false);
            if (multiScene)
            {
                zones["LOC01"].region.outerGate = outerGate;
                zones["LOC01"].region.seamBlocker = seam;
            }
            else
            {
                world.outerGate = outerGate;
                world.seamBlocker = seam;
            }

            WallOpening(false, -1.1f, -8, 0, -4f, 2.7f, 2.42f);
            Wall(false, 9.1f, -8, 0);
            WallOpening(true, -8, -1, 9, 4, 1.8f, 2.42f);
            Wall(true, 0, 1.1f, 2.5f);
            Wall(true, 0, 5.5f, 9);
            // STAFF West wall split cleanly behind pilasters (z=4.90, 8.95) & probe-anchored inside STAFF air (x=-0.55)
            // so LightProbe GI never extrapolates outside the west building shell!
            Wall(false, -1.1f, 0, 4.90f, probeX: -0.55f);
            Wall(false, -1.1f, 4.90f, 8.95f, probeX: -0.55f);
            Wall(false, -1.1f, 8.95f, 16, probeX: -0.55f);
            // CH01-R compact nonlinear loop: Staff Cross-Passage at z = 7.50 connecting STAFF Nursing Alcove <-> HALL Corridor
            // Interior probe anchors (STAFF x=+0.65, HALL x=+3.00) keep doorway lintels 100% GI-seamless with adjacent corridor walls!
            WallOpening(false, 1.2f, 0, 16, 7.50f, 1.04f, 2.22f, probeX: 0.65f);
            WallOpening(false, 2.4f, 0, 16, 7.50f, 1.04f, 2.22f, probeX: 3.00f);
            Wall(true, 6.94f, 1.2f, 2.4f, 0f, 2.22f);
            Wall(true, 8.06f, 1.2f, 2.4f, 0f, 2.22f);
            Surface("Staff cross threshold", new Vector3(1.80f, -0.1f, 7.50f), new Vector3(1.42f, 0.20f, 1.04f),
                "FloorR", "FloorB", true, 1.6f, isStatic: true);
            Surface("Staff cross soffit", new Vector3(1.80f, 2.91f, 7.50f), new Vector3(1.22f, 1.38f, 1.04f),
                "PlasterR", "PlasterB", false, 1.8f, isStatic: true);
            var staffDoor = HingedStaffCrossDoor("DOOR-STAFF", new Vector3(2.36f, 0f, 7.03f), 0.94f, -96f);
            if (multiScene) zones["LOC03"].region.staffCrossDoor = staffDoor;
            else world.staffCrossDoor = staffDoor;
            if (art)
            {
                var scFrame = PlaceBlenderModel("CTCC_ENV_StaffCrossDoorFrame", ResolveParent(null, new Vector3(1.80f, 0f, 7.50f)), new Vector3(1.80f, 0f, 7.50f), 0f, pairRB: true, castShadows: false);
                staticArchitecture.Add(scFrame);
            }

            WallOpening(false, 5.60f, 0, 16, 4.75f, 1.2f, art ? 2.84f : 2.22f);
            var room = Opening("GATE-101");
            var roomDoor = HingedWardDoor("GATE-101", room, -105);
            if (multiScene) zones["LOC03"].region.roomDoor = roomDoor;
            else world.roomDoor = roomDoor;
            if (art)
            {
                var frame = PlaceBlenderModel("CTCC_ENV_WardDoorFrame", ResolveParent(null, new Vector3(room.x, 0, room.z)), new Vector3(room.x, 0, room.z), -90f, pairRB: true);
                staticArchitecture.Add(frame);
            }

            Surface("Continuous 101 threshold", new Vector3(5.60f, -0.1f, 4.75f), new Vector3(0.22f, 0.20f, 1.2f),
                "FloorR", "FloorB", true, 1.6f, isStatic: true);
            Surface("Continuous store threshold", new Vector3(13.80f, -0.1f, 3.62f), new Vector3(0.22f, 0.20f, 1.22f),
                "FloorR", "FloorB", true, 1.6f, isStatic: true);
            Wall(true, 1.90f, 5.7f, 13.9f);
            Wall(true, 8.10f, 5.7f, 6.28f);
            Window(6.28f, 8.42f, 8.10f, "CTCC_ENV_SteelWindow_Wide");
            Wall(true, 8.10f, 8.42f, 9.23f);
            Window(9.23f, 11.32f, 8.10f, "CTCC_ENV_SteelWindow_Mid");
            Wall(true, 8.10f, 11.32f, 11.75f);
            Window(11.75f, 13.03f, 8.10f, "CTCC_ENV_SteelWindow_Narrow");
            Wall(true, 8.10f, 13.03f, 13.9f);

            WallOpening(false, 13.80f, 2, 8, 3.62f, 1.22f, art ? 2.84f : 2.20f);
            Wall(false, 17.2f, 2, 5.6f);
            Wall(true, 1.90f, 13.9f, 17.1f);
            Wall(true, 5.70f, 13.9f, 17.1f);
            if (art)
            {
                var storeOp = Opening("GATE-STORE");
                var sFrame = PlaceBlenderModel("CTCC_ENV_WardDoorFrame", ResolveParent(null, new Vector3(storeOp.x, 0, storeOp.z)), new Vector3(storeOp.x, 0, storeOp.z), -90f, pairRB: true);
                staticArchitecture.Add(sFrame);
            }

            Wall(false, -1.1f, 16, 22);
            Wall(false, 9.1f, 16, 22);
            WallOpening(true, 22.1f, -1, 5.45f, 4f, 2.7f, 2.42f);
            WallOpening(true, 22.1f, 5.45f, 9, 7f, 2.7f, 2.42f);
            Wall(true, 16, -1, -0.65f);
            Wall(true, 16, 0.75f, 3.3f);
            Wall(true, 16, 4.7f, 9);
            Wall(true, 16, -0.65f, 0.75f, 2.20f);
            Wall(true, 16, 3.3f, 4.7f, 2.20f);
            var erA = HingedEmergencyDoor("GATE-CAPCUU", Opening("GATE-CAPCUU"), -94);
            var erB = HingedEmergencyDoor("GATE-CAPCUU-B", Opening("GATE-CAPCUU-B"), -94);
            if (multiScene)
            {
                zones["LOC04"].region.emergencyDoorA = erA;
                zones["LOC04"].region.emergencyDoorB = erB;
            }
            else
            {
                world.emergencyDoorA = erA;
                world.emergencyDoorB = erB;
            }

            BuildHospitalExpansion();
        }

        private DoorPose HingedExteriorGate(string name, Aperture a, float angle)
        {
            // Phase 2.5 + LOC01 Hero Overhaul: Cycles-baked wrought-iron gate leaf (2.36x2.48m / 2.62m finials)
            // framed by sculpted 1987 masonry piers + forged lanterns + overhead architrave (inner clear width LOCKED 2.40m: x=[2.80..5.20]!)
            if (!art) return Hinged(name, a, 2.52f, Mat("MetalPaint"), angle, false);
            var pos = new Vector3(a.x, 0, a.z);
            var parent = ResolveParent(null, pos);
            var offset = new Vector3(-a.width / 2f, 0, 0);
            Transform pivot = Group(name + " · hinge", parent, pos + offset);
            var proxy = Box("Gate collider", new Vector3(a.width / 2f, 1.26f, 0), new Vector3(a.width - 0.025f, 2.52f, 0.08f), Mat("MetalOld"), pivot, solid: true);
            proxy.GetComponent<Renderer>().enabled = false;
            PlaceBlenderModel("CTCC_PROP_ExteriorGate", pivot, Vector3.zero, 0f, pairRB: true, castShadows: true);

            // Hero sculpted masonry gate arch, brick plinths, coping & forged iron pier lanterns (replaces primitive cubes!)
            var gateArch = PlaceBlenderModel("CTCC_LOC01_GateArchAndPillars", parent, pos, 0f, pairRB: true, castShadows: true);
            staticArchitecture.Add(gateArch);
            foreach (int s in new[] { -1, 1 })
            {
                float px = a.x + s * (a.width * 0.5f + 0.23f); // West=2.57, East=5.43 (inner edges locked at 2.80 & 5.20)
                Box($"Gate pillar col {(s < 0 ? "W" : "E")}", new Vector3(px, 1.48f, a.z), new Vector3(0.46f, 2.96f, 0.44f),
                    Mat("PlasterR"), parent, solid: true, isStatic: true).GetComponent<Renderer>().enabled = false;
            }
            Box("Gate lintel beam", new Vector3(a.x, 3.16f, a.z), new Vector3(a.width + 1.04f, 0.44f, 0.44f),
                Mat("PlasterR"), parent, solid: true, isStatic: true).GetComponent<Renderer>().enabled = false;
            return new DoorPose { pivot = pivot, openAngle = angle };
        }

        private DoorPose HingedWardDoor(string name, Aperture a, float angle)
        {
            if (!art) return Hinged(name, a, 2.20f, Mat("DoorWood"), angle, false);
            var pos = new Vector3(a.x, 0, a.z);
            var offset = a.alongX ? new Vector3(-a.width / 2f, 0, 0) : new Vector3(0, 0, -a.width / 2f);
            Transform pivot = Group(name + " · hinge", ResolveParent(null, pos), pos + offset);
            Transform leaf = Group("Leaf", pivot);
            if (!a.alongX) leaf.localRotation = Quaternion.Euler(0, -90, 0);

            var proxy = Box("Ward door collider", new Vector3(a.width / 2f, 1.10f, 0), new Vector3(a.width - 0.025f, 2.20f, 0.055f), Mat("WoodDoorR"), leaf, solid: true);
            proxy.GetComponent<Renderer>().enabled = false;
            PlaceBlenderModel("CTCC_ENV_WardDoorLeaf", leaf, Vector3.zero, 0f, pairRB: true);
            CapsuleTarget("DOOR-101", "Cửa phòng 101", new Vector3(a.width - 0.16f, 1.04f, 0f), new Vector3(0.36f, 0.48f, 0.34f), leaf);
            return new DoorPose { pivot = pivot, openAngle = angle };
        }

        private DoorPose HingedStaffCrossDoor(string name, Vector3 hingeWorldPos, float width, float openAngle)
        {
            var parent = ResolveParent(null, hingeWorldPos);
            Transform pivot = Group(name + " · hinge", parent, hingeWorldPos);
            Transform leaf = Group("Leaf", pivot);
            // Runs along +Z (z=7.03 to 7.97) when closed; swings -96 deg into the x=1.2..2.4 service pocket!
            leaf.localRotation = Quaternion.Euler(0, -90f, 0);
            var proxy = Box("Staff cross door collider", new Vector3(width * 0.5f, 1.09f, 0f), new Vector3(width - 0.02f, 2.18f, 0.055f), Mat("WoodDoorR"), leaf, solid: true);
            if (art)
            {
                proxy.GetComponent<Renderer>().enabled = false;
                PlaceBlenderModel("CTCC_ENV_StaffCrossDoor", leaf, Vector3.zero, 0f, pairRB: true);
            }
            CapsuleTarget("DOOR-STAFF", "Cửa thông lối nhân viên", new Vector3(width - 0.14f, 1.04f, 0f), new Vector3(0.36f, 0.50f, 0.36f), leaf);
            return new DoorPose { pivot = pivot, openAngle = openAngle, isOpen = false };
        }

        private DoorPose HingedEmergencyDoor(string name, Aperture a, float angle)
        {
            if (!art) return Hinged(name, a, 2.18f, Mat("MetalPaint"), angle, true);
            var pos = new Vector3(a.x, 0, a.z);
            var offset = a.alongX ? new Vector3(-a.width / 2f, 0, 0) : new Vector3(0, 0, -a.width / 2f);
            Transform pivot = Group(name + " · hinge", ResolveParent(null, pos), pos + offset);
            Transform leaf = Group("Leaf", pivot);
            if (!a.alongX) leaf.localRotation = Quaternion.Euler(0, -90, 0);

            var proxy = Box("ER door collider", new Vector3(a.width / 2f, 1.09f, 0), new Vector3(a.width - 0.025f, 2.18f, 0.065f), Mat("MetalPaint"), leaf, solid: true);
            proxy.GetComponent<Renderer>().enabled = false;
            // Heavy molded institutional ER doorway frame & stretcher threshold wrapping the 1.40 x 2.20m opening!
            var erFrame = PlaceBlenderModel("CTCC_ENV_EmergencyDoorFrame", ResolveParent(null, pos), pos, a.alongX ? 0f : -90f, pairRB: true, castShadows: false);
            staticArchitecture.Add(erFrame);
            // Paired 0.692m leaves spanning [0.004..0.696] & [1.396..0.704] — meeting flush at center 0.700 with ZERO side gaps!
            PlaceBlenderModel("CTCC_ENV_EmergencySwingDoorLeaf", leaf, new Vector3(0.004f, 0f, 0f), 0f, pairRB: true, castShadows: false);
            PlaceBlenderModel("CTCC_ENV_EmergencySwingDoorLeaf", leaf, new Vector3(a.width - 0.004f, 0f, 0f), 180f, pairRB: true, castShadows: false);
            return new DoorPose { pivot = pivot, openAngle = angle };
        }

        private DoorPose Hinged(string name, Aperture a, float height, Material finish, float angle, bool glass)
        {
            var pos = new Vector3(a.x, 0, a.z);
            var offset = a.alongX ? new Vector3(-a.width / 2, 0, 0) : new Vector3(0, 0, -a.width / 2);
            Transform pivot = Group(name + " · hinge", ResolveParent(null, pos), pos + offset);
            Transform leaf = Group("Leaf", pivot);
            if (!a.alongX) leaf.localRotation = Quaternion.Euler(0, -90, 0);
            Box("Door leaf", new Vector3(a.width / 2, height / 2, 0), new Vector3(a.width - 0.025f, height, 0.065f),
                finish, leaf, true);
            return new DoorPose { pivot = pivot, openAngle = angle };
        }

        private void Window(float left, float right, float z, string blenderModel)
        {
            Wall(true, z, left, right, 0, 1.09f);
            Wall(true, z, left, right, 2.94f, 3.6f);
            float midX = (left + right) * 0.5f;
            float w = right - left;
            var pos = new Vector3(midX, 0f, z);
            var parent = ResolveParent(null, pos);

            var barrier = Box("Window barrier", new Vector3(midX, 2.01f, z + 0.03f), new Vector3(w, 1.85f, 0.035f),
                Mat("Glass"), parent, solid: true);
            if (!art)
            {
                Box("Sill", new Vector3(midX, 1.10f, z), new Vector3(w, 0.065f, 0.4f), Mat("BaseTrim"), parent, solid: true);
                return;
            }
            barrier.GetComponent<Renderer>().enabled = false;
            var win = PlaceBlenderModel(blenderModel, parent, pos, 0f, pairRB: true);
            staticArchitecture.Add(win);

            var pane = Flat("Window rain glass", "RainGlassR", new Vector3(midX, 1.76f, z + 0.01f), Vector3.back, new Vector2(w - 0.08f, 1.30f), parent);
            AddFinish(new LayerFinish { renderer = pane.GetComponent<Renderer>(), real = Mat("RainGlassR"), report = Mat("Glass") });
        }

        private void VerifyBlockout()
        {
            Transform ap04 = world.Pad("AP04");
            if (Mathf.Abs(layout.wallHeight - 3.6f) > 0.001f || layout.beds.Length != 4)
                throw new InvalidOperationException("Core 03 dimensions differ");
            world.Present(false, 4, true, true, true, true);
            VerifyNav("R / all relevant doors", new[] { "AP00", "AP01", "AP02", "AP03", "AP04", "AP05" });
            world.emergencyDoorA.Set(false);
            Physics.SyncTransforms();
            VerifyNav("LOC04 / staff route", new[] { "AP02", "AP05" });
            world.emergencyDoorA.Set(true);
            world.emergencyDoorB.Set(false);
            Physics.SyncTransforms();
            VerifyNav("LOC04 / main route", new[] { "AP02", "AP05" });
            for (int route = 2; route <= 4; route++)
            {
                world.Present(true, route, true, true, false, false);
                if (HospitalScene.Occupied(ap04.position))
                    throw new InvalidOperationException("AP04 collision after F101 branch " + route);
                VerifyNav("F101 route " + route, new[] { "AP01", "AP02", "AP03", "AP04" });
            }
            // Verify CH01-R compact nonlinear exploration loop (STAFF <-> HALL cross-door open)
            if (world.staffCrossDoor != null)
            {
                world.Present(true, 0, true, true, false, false);
                world.staffCrossDoor.Set(true);
                Physics.SyncTransforms();
                VerifyNav("CH01-R staff loop open", new[] { "AP02", "AP04" });
                world.staffCrossDoor.Set(false);
                Physics.SyncTransforms();
            }
            world.Present(false, 0, true, true, false, false);
            Vector3 through = new Vector3(5.6f, 0.30f, 4.75f);
            bool stretcherClear = !Physics.CheckBox(through, new Vector3(0.99f, 0.25f, 0.36f), Quaternion.identity,
                HospitalScene.SolidMask, QueryTriggerInteraction.Ignore);
            if (!stretcherClear)
                throw new InvalidOperationException("0.72 m stretcher proxy collided at the 101 doorway.");
            world.Present(false, 4, true, true, true, true, false, true, true);
            VerifyNav("CH03–05 / connected hospital", new[] { "AP02", "AP05", "AP06", "AP10", "AN03", "AP07", "AP08", "AP09" });
            if (multiScene && zones.TryGetValue("LOC05", out var upperSlot) && upperSlot.region.loc08GateBlocker != null)
            {
                upperSlot.region.loc08GateBlocker.SetActive(false);
                Physics.SyncTransforms();
                try
                {
                    VerifyNav("CH05 / G05 LOC08 unlocked", new[] { "AN03", "AP11" });
                }
                finally
                {
                    upperSlot.region.loc08GateBlocker.SetActive(true);
                    Physics.SyncTransforms();
                }
            }

            // W-Q02: Khu B (AP06) -> S1 -> Reception (AP02) -> Courtyard (AP01) with B1 (LOC09) completely excluded!
            if (multiScene && zones.TryGetValue("LOC09", out var b1Slot))
            {
                b1Slot.region.gameObject.SetActive(false);
                Physics.SyncTransforms();
                try
                {
                    VerifyNav("W-Q02 / Khu B to courtyard without B1", new[] { "AP06", "AP02", "AP01" });
                }
                finally
                {
                    b1Slot.region.gameObject.SetActive(true);
                    Physics.SyncTransforms();
                }
            }

            // W-Q03: Exterior observation of sealed 2004 door E05b (35.8, -3.4, 42.0) reachable from AP08 before G06,
            // while sealed 2004 wall blocks direct pass-through into LOC12 (32.5, -3.4, 42.0)!
            VerifyE05bObservationAndSeal();

            Physics.SyncTransforms();
            var erFeet = world.Pad("AP05").position;
            if (Physics.CapsuleCast(erFeet + Vector3.up * .46f, erFeet + Vector3.up * 1.40f,
                HospitalScene.Radius, Vector3.forward, 2.21f, HospitalScene.SolidMask, QueryTriggerInteraction.Ignore))
                throw new InvalidOperationException("The ER ledger or another fixture blocks the ward stair approach.");
            world.Present(false, 0, false, false, false, false);
            Debug.Log("CTCC_GREYBOX_PASS R, three B branches, LOC04 routes, upper and basement safe pads, 0.72 m stretcher, W-Q02 S1 independence, W-Q03 E05b exterior access & seal");
        }

        private void VerifyE05bObservationAndSeal()
        {
            var nav = world.BuildNavigation();
            if (nav == null) throw new InvalidOperationException("No NavMesh for W-Q03 E05b verification");
            var handle = NavMesh.AddNavMeshData(nav);
            try
            {
                Vector3 ap08 = world.Pad("AP08").position;
                Vector3 e05bExterior = new(35.8f, BasementLevel + 0.04f, 42.0f);
                Vector3 loc12Interior = new(32.5f, BasementLevel + 0.04f, 42.0f);
                if (!NavMesh.SamplePosition(ap08, out var startHit, 0.40f, NavMesh.AllAreas))
                    throw new InvalidOperationException("W-Q03: AP08 missing from NavMesh");
                if (HospitalScene.Occupied(e05bExterior) || !NavMesh.SamplePosition(e05bExterior, out var obsHit, 0.45f, NavMesh.AllAreas))
                    throw new InvalidOperationException("W-Q03: E05b exterior observation point invalid or blocked: " + e05bExterior);
                var toE05b = new NavMeshPath();
                if (!NavMesh.CalculatePath(startHit.position, obsHit.position, NavMesh.AllAreas, toE05b) || toE05b.status != NavMeshPathStatus.PathComplete)
                    throw new InvalidOperationException("W-Q03: E05b exterior observation gallery is not reachable from AP08 before G06");
                // Sealed 2004 east wall MUST block pass-through into LOC12!
                bool wallSolid = Physics.Linecast(e05bExterior + Vector3.up * 1.0f, loc12Interior + Vector3.up * 1.0f,
                    HospitalScene.SolidMask, QueryTriggerInteraction.Ignore);
                if (!wallSolid)
                    throw new InvalidOperationException("W-Q03: Sealed 2004 rear door wall at x=34.5 is missing solid collision!");
                if (NavMesh.SamplePosition(loc12Interior, out var cHit, 0.45f, NavMesh.AllAreas))
                {
                    var breach = new NavMeshPath();
                    if (NavMesh.CalculatePath(obsHit.position, cHit.position, NavMesh.AllAreas, breach) && breach.status == NavMeshPathStatus.PathComplete)
                        throw new InvalidOperationException("W-Q03: NavMesh leaked through sealed 2004 door or G06 into LOC12!");
                }
                Debug.Log("CTCC_WQ03_PASS E05b exterior observation reachable from AP08 before G06; 2004 infill solid; LOC12 sealed");
            }
            finally
            {
                handle.Remove();
                UnityEngine.Object.DestroyImmediate(nav);
            }
        }

        private void VerifyNav(string label, string[] pads)
        {
            var nav = world.BuildNavigation();
            if (nav == null) throw new InvalidOperationException("No NavMesh: " + label);
            var handle = NavMesh.AddNavMeshData(nav);
            try
            {
                if (label.StartsWith("CH03"))
                {
                    Vector3 previous = world.Pad("AP05").position;
                    for (int step = -1; step <= 11; step++)
                    {
                        float z = 22.1f + step * .93f;
                        float y = -3.4f * Mathf.InverseLerp(22.7f, 30.89f, z);
                        Vector3 point = new(7f, y + .04f, z);
                        var stairPath = new NavMeshPath();
                        bool found = NavMesh.SamplePosition(point, out var stairPoint, .38f, NavMesh.AllAreas);
                        if (found) NavMesh.CalculatePath(previous, stairPoint.position, NavMesh.AllAreas, stairPath);
                        Debug.Log($"CTCC_STAIR_S2 point={point} collider={HospitalScene.Occupied(point)} found={found} nav={stairPoint.position} fromPrevious={stairPath.status}");
                        if (found) previous = stairPoint.position;
                    }
                }
                var spots = new Vector3[pads.Length];
                for (int i = 0; i < pads.Length; i++)
                {
                    Vector3 point = world.Pad(pads[i]).position;
                    if (HospitalScene.Occupied(point) || !NavMesh.SamplePosition(point, out var nearest, 0.36f, NavMesh.AllAreas))
                        throw new InvalidOperationException("Invalid pad " + pads[i] + " / " + label);
                    spots[i] = nearest.position;
                }
                foreach (var from in spots)
                    foreach (var to in spots)
                    {
                        var path = new NavMeshPath();
                        if (!NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                            throw new InvalidOperationException("Disconnected pads " + from + " → " + to + " / " + label +
                                " / path ends at " + (path.corners.Length > 0 ? path.corners[path.corners.Length - 1].ToString() : "no corner"));
                    }
                Debug.Log("CTCC_ROUTE_PASS " + label + " / " + string.Join(", ", pads));
            }
            finally
            {
                handle.Remove();
                UnityEngine.Object.DestroyImmediate(nav);
            }
        }
    }
}
