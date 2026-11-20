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

        private static readonly (string id, string name, Bounds navBounds)[] ZoneSpecs =
        {
            ("LOC01", "LOC01_Exterior", new Bounds(new Vector3(4f, 1f, -16f), new Vector3(24f, 8f, 20f))),
            ("LOC02", "LOC02_Reception", new Bounds(new Vector3(4f, 1f, -4f), new Vector3(14f, 8f, 12f))),
            ("LOC03", "LOC03_Ward101", new Bounds(new Vector3(8.5f, 1f, 8f), new Vector3(22f, 8f, 20f))),
            ("LOC04", "LOC04_Emergency", new Bounds(new Vector3(4f, 1f, 19f), new Vector3(14f, 8f, 10f))),
            ("LOC05", "LOC05_ArchiveWing", new Bounds(new Vector3(2.0f, 1f, 32.5f), new Vector3(16f, 8f, 18f))),
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
            public readonly List<RoomLoop> loops = new();
            public readonly List<Light> emergencyRedLights = new();
        }

        [Serializable] private sealed class Layout
        {
            public Area[] rooms;
            public Aperture[] openings;
            public Pose[] pads, beds;
            public float wallHeight, bedWidth, bedLength;
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
            public float x, z, yaw;
        }

        private readonly Layout layout;
        private readonly bool art;
        private readonly bool multiScene;
        private readonly Dictionary<string, Material> materials = new();
        private readonly Dictionary<string, ZoneSlot> zones = new();
        private readonly List<Light> livingTubes = new();
        private readonly List<MeshFilter> movingCloth = new();
        private readonly List<Transform> movingFans = new();
        private readonly List<GameObject> staticArchitecture = new();
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
                world.additiveZoneScenes = ZoneSpecs.Take(4).Select(z => z.name).ToArray();
                world.regionalStreamZoneScenes = ZoneSpecs.Skip(4).Select(z => z.name).ToArray();
                foreach (var spec in ZoneSpecs)
                {
                    var zScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                    string zPath = Root + "/Scenes/" + spec.name + ".unity";
                    EditorSceneManager.SaveScene(zScene, zPath);
                    SceneManager.SetActiveScene(zScene);
                    var zRoot = new GameObject("Zone · " + spec.id);
                    var region = zRoot.AddComponent<ZoneRegion>();
                    region.zoneId = spec.id;
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
                new Pose { id = "AP06", x = 4.0f, z = 26.4f, yaw = 0f },
                new Pose { id = "AN03", x = -0.6f, z = 32.2f, yaw = -90f },
                new Pose { id = "AP07", x = 4.0f, z = 38.6f, yaw = 0f },
            };
            foreach (var p in allPads)
            {
                var pos = new Vector3(p.x, 0.015f, p.z);
                Transform parent = this.multiScene ? ZoneAt(p.z).permanent : permanent;
                var point = Group(p.id, parent, pos);
                point.rotation = Quaternion.Euler(0, p.yaw, 0);
                var mp = new MarkerPose { id = p.id, point = point };
                if (this.multiScene) ZoneAt(p.z).pads.Add(mp);
                else bootstrapPads.Add(mp);
            }
            world.pads = bootstrapPads.ToArray();
            PrepareMaterials();
        }

        [MenuItem("Ca Trực Cuối Cùng/1 — Dựng và kiểm mặt bằng")]
        public static void Greybox() => new HospitalAuthoring(false, multiScene: false).Generate(true);

        [MenuItem("Ca Trực Cuối Cùng/2 — Dựng cảnh M2 Vertical Slice (5 Additive Scenes)")]
        public static void ReferenceScene() => new HospitalAuthoring(true, multiScene: true).Generate(true);

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

        private ZoneSlot ZoneAt(float z)
        {
            if (z < -8.01f) return zones["LOC01"];
            if (z < -0.01f) return zones["LOC02"];
            if (z < 15.99f) return zones["LOC03"];
            if (z < 23.50f) return zones["LOC04"];
            return zones["LOC05"];
        }

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
            return ZoneAt(t.position.z);
        }

        private Transform ResolveParent(Transform parent, Vector3 worldPos)
        {
            if (!multiScene) return parent == null ? permanent : parent;
            if (parent == null || parent == permanent) return ZoneAt(worldPos.z).permanent;
            if (parent == real) return ZoneAt(worldPos.z).real;
            if (parent == report) return ZoneAt(worldPos.z).report;
            if (parent == memory) return ZoneAt(worldPos.z).memory;
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
            MarkInteractions();
            LightAndStage();
            if (multiScene)
                FinalizeZoneRegions();

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
            Debug.Log($"CTCC_SCENE {(multiScene ? "m2-additive-5scenes" : "greybox")} {finishCount} paired finishes, {staticArchitecture.Count} static objects");
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
            bool solid = false, float tile = 0, bool isStatic = true)
        {
            var item = Box(name, p, size, Mat(r), solid: solid, tile: tile, isStatic: isStatic);
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

        private void Wall(bool alongX, float axis, float from, float to, float bottom = 0, float top = 3.6f)
        {
            if (to - from < 0.02f || top - bottom < 0.01f) return;
            const float thickness = 0.20f;
            void Segment(string name, float low, float high, string r, string b, bool solid)
            {
                var middle = (from + to) * 0.5f;
                var point = alongX ? new Vector3(middle, (low + high) * 0.5f, axis) : new Vector3(axis, (low + high) * 0.5f, middle);
                var scale = alongX ? new Vector3(to - from, high - low, thickness) : new Vector3(thickness, high - low, to - from);
                Surface(name, point, scale, r, b, solid, 2.4f, isStatic: true);
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

        private void WallOpening(bool alongX, float axis, float from, float to, float opening, float width, float clear)
        {
            float half = width * 0.5f;
            Wall(alongX, axis, from, opening - half);
            Wall(alongX, axis, opening + half, to);
            Wall(alongX, axis, opening - half, opening + half, clear, layout.wallHeight);
        }

        private Aperture Opening(string id) => layout.openings.Single(e => e.id == id);

        private void BuildShell()
        {
            foreach (var a in layout.rooms)
                Floor(a.id, a.id != "LOC01");
            if (!art)
                Box("Approach at grade", new Vector3(4, -0.1f, -22), new Vector3(9, 0.2f, 4), Mat("Exterior"), solid: true, tile: 2f, isStatic: true);

            // Courtyard perimeter low walls using authentic WeatheredBrick in M2 art mode
            if (art)
            {
                Box("West yard brick wall", new Vector3(-5f, 0.425f, -14f), new Vector3(0.22f, 0.85f, 12f), Mat("WeatheredBrick"), solid: true, tile: 1.6f, isStatic: true);
                Box("East yard brick wall", new Vector3(13f, 0.425f, -14f), new Vector3(0.22f, 0.85f, 12f), Mat("WeatheredBrick"), solid: true, tile: 1.6f, isStatic: true);
                Box("Front yard brick wall L", new Vector3(-1.1f, 0.86f, -20f), new Vector3(7.8f, 1.72f, 0.24f), Mat("WeatheredBrick"), solid: true, tile: 1.6f, isStatic: true);
                Box("Front yard brick wall R", new Vector3(9.1f, 0.86f, -20f), new Vector3(7.8f, 1.72f, 0.24f), Mat("WeatheredBrick"), solid: true, tile: 1.6f, isStatic: true);
            }
            else
            {
                Wall(false, -5, -20, -8, 0, 0.85f);
                Wall(false, 13, -20, -8, 0, 0.85f);
                Wall(true, -20, -5, 2.8f, 0, 1.72f);
                Wall(true, -20, 5.2f, 13, 0, 1.72f);
            }

            var gate = Opening("GATE-CONG");
            var outerGate = HingedExteriorGate("GATE-CONG", gate, -101);
            var seam = Box("Exhausted gate seam only", new Vector3(4, 1.1f, -20.50f),
                new Vector3(2.44f, 2.20f, 0.07f), Mat("Glass"), solid: true);
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

            Wall(false, -1.1f, -8, 0);
            Wall(false, 9.1f, -8, 0);
            WallOpening(true, -8, -1, 9, 4, 1.8f, 2.42f);
            Wall(true, 0, 1.1f, 2.5f);
            Wall(true, 0, 5.5f, 9);
            Wall(false, -1.1f, 0, 16);
            Wall(false, 1.2f, 0, 16);
            Wall(false, 2.4f, 0, 16);

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
            Wall(true, 22.1f, -1, 9);
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

            BuildCh03ArchiveShell();
        }

        private void BuildCh03ArchiveShell()
        {
            // CH03 Representative Wing (LOC05 Nurse Station + LOC07 Archive & Meeting Loop + LOC06 Duty Corridor, z in [25..40])
            Surface("LOC05–07 · floor", new Vector3(2.0f, -0.10f, 32.5f),
                new Vector3(11.0f, 0.20f, 15.0f), "FloorR", "FloorB", true, 1.6f, isStatic: true);
            Surface("LOC05–07 · ceiling", new Vector3(2.0f, 3.71f, 32.5f),
                new Vector3(11.0f, 0.22f, 15.0f), "PlasterR", "PlasterB", true, 2.2f, isStatic: true);

            // Outer boundary walls of CH03 Archive Wing
            Wall(true, 25.0f, -3.5f, 7.5f);
            Wall(true, 40.0f, -3.5f, 7.5f);
            Wall(false, -3.5f, 25.0f, 40.0f);
            Wall(false, 7.5f, 25.0f, 40.0f);

            // LOC05 Nurse Station north partition (z = 29.5f): West doorway into LOC07 Archive (-0.5f) & Main doorway into LOC06 Corridor (4.0f)
            WallOpening(true, 29.5f, -3.5f, 2.5f, -0.5f, 1.6f, 2.42f);
            WallOpening(true, 29.5f, 2.5f, 7.5f, 4.0f, 1.8f, 2.42f);

            // Longitudinal spine between LOC07 Archive/Meeting Loop (west) and LOC06 Duty Corridor (east) with north bypass doorway at z = 35.6f
            WallOpening(false, 2.5f, 29.5f, 37.2f, 35.6f, 1.6f, 2.42f);
            Wall(false, 5.8f, 29.5f, 40.0f);

            // G03 Archive Exit threshold at z = 37.2f
            Wall(true, 37.2f, -3.5f, 2.5f);
            WallOpening(true, 37.2f, 2.5f, 5.8f, 4.0f, 1.4f, 2.20f);
            var g03Ap = new Aperture { id = "GATE-G03", x = 4.0f, z = 37.2f, width = 1.4f, alongX = true };
            var g03Door = HingedEmergencyDoor("GATE-G03", g03Ap, -94f);
            if (multiScene)
                zones["LOC05"].region.archiveExitDoor = g03Door;
            else
                world.archiveExitDoor = g03Door;
        }

        private DoorPose HingedExteriorGate(string name, Aperture a, float angle)
        {
            if (!art) return Hinged(name, a, 1.80f, Mat("MetalPaint"), angle, false);
            var pos = new Vector3(a.x, 0, a.z);
            var parent = ResolveParent(null, pos);
            var offset = new Vector3(-a.width / 2f, 0, 0);
            Transform pivot = Group(name + " · hinge", parent, pos + offset);
            var proxy = Box("Gate collider", new Vector3(a.width / 2f, 0.90f, 0), new Vector3(a.width - 0.025f, 1.80f, 0.08f), Mat("MetalOld"), pivot, solid: true);
            proxy.GetComponent<Renderer>().enabled = false;
            PlaceBlenderModel("CTCC_PROP_ExteriorGate", pivot, Vector3.zero, 0f, pairRB: true);
            foreach (int s in new[] { -1, 1 })
            {
                Box("Gate pillar", new Vector3(a.x + s * (a.width * 0.5f + 0.16f), 1.05f, a.z), new Vector3(0.32f, 2.10f, 0.34f), Mat("PlasterR"), parent, solid: true, tile: 1.5f, isStatic: true);
                Box("Gate pillar cap", new Vector3(a.x + s * (a.width * 0.5f + 0.16f), 2.14f, a.z), new Vector3(0.38f, 0.09f, 0.40f), Mat("BaseTrim"), parent, isStatic: true);
            }
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
            // Two side-by-side Blender Emergency Swing Door leaves spanning the 1.40m ER doorway!
            PlaceBlenderModel("CTCC_ENV_EmergencySwingDoorLeaf", leaf, new Vector3(a.width * 0.25f, 0f, 0f), 0f, pairRB: true);
            PlaceBlenderModel("CTCC_ENV_EmergencySwingDoorLeaf", leaf, new Vector3(a.width * 0.75f, 0f, 0f), 180f, pairRB: true);
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
            world.Present(false, 0, true, true, false, false);
            Vector3 through = new Vector3(5.6f, 0.30f, 4.75f);
            bool stretcherClear = !Physics.CheckBox(through, new Vector3(0.99f, 0.25f, 0.36f), Quaternion.identity,
                HospitalScene.SolidMask, QueryTriggerInteraction.Ignore);
            if (!stretcherClear)
                throw new InvalidOperationException("0.72 m stretcher proxy collided at the 101 doorway.");
            world.Present(false, 4, true, true, true, true, false, true);
            VerifyNav("CH03 / LOC05–07 archive loop", new[] { "AP06", "AN03", "AP07" });
            world.Present(false, 0, false, false, false, false);
            Debug.Log("CTCC_GREYBOX_PASS AP00–07, R & 3 B branches, LOC04 via either door, CH03 loop, 0.72 m stretcher at 1.2 m doorway");
        }

        private void VerifyNav(string label, string[] pads)
        {
            var nav = world.BuildNavigation();
            if (nav == null) throw new InvalidOperationException("No NavMesh: " + label);
            var handle = NavMesh.AddNavMeshData(nav);
            try
            {
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
                            throw new InvalidOperationException("Disconnected pads " + from + " → " + to + " / " + label);
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
