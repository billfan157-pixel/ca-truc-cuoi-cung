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
        private const string SceneFile = Root + "/Scenes/AnSinh.unity";
        private const string GeometryFile = Root + "/Generated/TileGeometry.asset";

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
        private readonly Dictionary<string, Material> materials = new();
        private readonly List<Light> livingTubes = new();
        private readonly List<MeshFilter> movingCloth = new();
        private readonly List<Transform> movingFans = new();
        private readonly List<GameObject> staticArchitecture = new();
        private readonly Scene scene;
        private readonly HospitalScene world;
        private readonly Transform permanent, real, report;
        private Mesh geometry;

        private HospitalAuthoring(bool art)
        {
            this.art = art;
            string path = Path.GetFullPath("../docs/prototype/layout.json");
            layout = JsonUtility.FromJson<Layout>(File.ReadAllText(path));
            Directory.CreateDirectory(Root + "/Scenes");
            Directory.CreateDirectory(Root + "/Generated");
            Directory.CreateDirectory(Root + "/Generated/Materials");
            AssetDatabase.Refresh();
            if (AssetDatabase.LoadAssetAtPath<Mesh>(GeometryFile) != null)
                AssetDatabase.DeleteAsset(GeometryFile);
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            world = new GameObject("An Sinh · CTCC M0-R").AddComponent<HospitalScene>();
            world.referenceArt = art;
            world.interactables = world.gameObject.AddComponent<InteractableRig>();
            permanent = Group("Physical continuity", world.transform);
            real = Group("R · 2012", world.transform);
            report = Group("B · Report in 2005 dress", world.transform);
            world.realProps = real.gameObject;
            world.reportProps = report.gameObject;
            world.pads = layout.pads.Select(p =>
            {
                var point = Group(p.id, permanent, new Vector3(p.x, 0.015f, p.z));
                point.rotation = Quaternion.Euler(0, p.yaw, 0);
                return new MarkerPose { id = p.id, point = point };
            }).ToArray();
            PrepareMaterials();
        }

        [MenuItem("Ca Trực Cuối Cùng/1 — Dựng và kiểm mặt bằng")]
        public static void Greybox() => new HospitalAuthoring(false).Generate(true);

        [MenuItem("Ca Trực Cuối Cùng/2 — Dựng cảnh Phòng 101 (M0-R)")]
        public static void ReferenceScene() => new HospitalAuthoring(true).Generate(false);

        public static void PrepareAndTest()
        {
            ReferenceScene();
            EditorApplication.isPlaying = true;
        }

        public static void TestScene()
        {
            EditorSceneManager.OpenScene(SceneFile);
            EditorApplication.isPlaying = true;
        }

        public static void PrepareAndBuild()
        {
            ReferenceScene();
            Windows();
        }

        [MenuItem("Ca Trực Cuối Cùng/3 — Build Windows")]
        public static void Windows()
        {
            if (!File.Exists(SceneFile))
                throw new InvalidOperationException("Build the CTCC scene first.");
            ConfigureEnvironment();
            string destination = Argument("-ctcc-build") ?? "../output/CTCC-M0R/CaTrucCuoiCung.exe";
            destination = Path.GetFullPath(destination);
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            var options = new BuildPlayerOptions
            {
                scenes = new[] { SceneFile },
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

        private static void ConfigureEnvironment()
        {
            PlayerSettings.companyName = "An Sinh Project";
            PlayerSettings.productName = "Ca trực cuối cùng";
            PlayerSettings.bundleVersion = "0.2.0";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.enableFrameTimingStats = true;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D12, GraphicsDeviceType.Direct3D11 });
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(SceneFile, true) };
            AssetDatabase.SaveAssets();
        }

        private void Generate(bool verify)
        {
            BuildShell();
            BuildFurnishing();
            MarkInteractions();
            LightAndStage();
            if (art)
            {
                ApplyStaticFlags();
                GroupFixedSurfaces();
            }
            world.Present(false, 0, false, false, false, false);
            if (verify)
                VerifyBlockout();
            EditorSceneManager.SaveScene(scene, SceneFile);
            ConfigureEnvironment();
            Debug.Log($"CTCC_SCENE {(art ? "m0r-reference" : "greybox")} {world.finishes.Count} paired finishes, {staticArchitecture.Count} static objects");
        }

        private void ApplyStaticFlags()
        {
            var flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic |
                        StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic;
            foreach (var go in staticArchitecture)
            {
                if (go == null) continue;
                GameObjectUtility.SetStaticEditorFlags(go, flags);
                foreach (Transform child in go.GetComponentsInChildren<Transform>(true))
                    GameObjectUtility.SetStaticEditorFlags(child.gameObject, flags);
            }
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
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent == null ? permanent : parent, false);
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
            world.finishes.Add(new LayerFinish { renderer = item.GetComponent<Renderer>(), real = Mat(r), report = Mat(b) });
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
                // Terrazzo cove skirting (len đá mài cao 9.5 cm)
                Vector3 p = alongX ? new Vector3(middle, 0.048f, axis) : new Vector3(axis, 0.048f, middle);
                Vector3 size = alongX ? new Vector3(to - from, 0.096f, 0.224f) : new Vector3(0.224f, 0.096f, to - from);
                Box("Terrazzo cove skirting", p, size, Mat("BaseTrim"), tile: 0.8f, isStatic: true);

                // Crisp painted wainscot dado trim stripe at 1.15 m (kẻ chỉ chân tường bệnh viện VN!)
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
            Box("Approach at grade", new Vector3(4, -0.1f, -22), new Vector3(9, 0.2f, 4), Mat("Exterior"), solid: true, tile: 2f, isStatic: true);
            Wall(false, -5, -20, -8, 0, 0.85f);
            Wall(false, 13, -20, -8, 0, 0.85f);
            Wall(true, -20, -5, 2.8f, 0, 1.72f);
            Wall(true, -20, 5.2f, 13, 0, 1.72f);

            var gate = Opening("GATE-CONG");
            world.outerGate = HingedExteriorGate("GATE-CONG", gate, -101);
            world.seamBlocker = Box("Exhausted gate seam only", new Vector3(4, 1.1f, -20.50f),
                new Vector3(2.44f, 2.20f, 0.07f), Mat("Glass"), solid: true);
            world.seamBlocker.GetComponent<Renderer>().enabled = false;
            world.seamBlocker.SetActive(false);

            Wall(false, -1.1f, -8, 0);
            Wall(false, 9.1f, -8, 0);
            WallOpening(true, -8, -1, 9, 4, 1.8f, 2.42f);
            Wall(true, 0, 1.1f, 2.5f);
            Wall(true, 0, 5.5f, 9);
            Wall(false, -1.1f, 0, 16);
            Wall(false, 1.2f, 0, 16);
            Wall(false, 2.4f, 0, 16);

            // Room 101 doorway: leave transom clear up to 2.84 m in reference art so real louvered glass transom shines through!
            WallOpening(false, 5.60f, 0, 16, 4.75f, 1.2f, art ? 2.84f : 2.22f);
            var room = Opening("GATE-101");
            world.roomDoor = HingedWardDoor("GATE-101", room, -105);
            if (art)
            {
                var frame = PlaceBlenderModel("CTCC_ENV_WardDoorFrame", permanent, new Vector3(room.x, 0, room.z), -90f, pairRB: true);
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
                var sFrame = PlaceBlenderModel("CTCC_ENV_WardDoorFrame", permanent, new Vector3(storeOp.x, 0, storeOp.z), -90f, pairRB: true);
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
            world.emergencyDoorA = Hinged("GATE-CAPCUU", Opening("GATE-CAPCUU"), 2.18f, Mat("MetalPaint"), -94, true);
            world.emergencyDoorB = Hinged("GATE-CAPCUU-B", Opening("GATE-CAPCUU-B"), 2.18f, Mat("MetalPaint"), -94, true);
        }

        private DoorPose HingedExteriorGate(string name, Aperture a, float angle)
        {
            if (!art) return Hinged(name, a, 1.80f, Mat("MetalPaint"), angle, false);
            var offset = new Vector3(-a.width / 2f, 0, 0);
            Transform pivot = Group(name + " · hinge", permanent, new Vector3(a.x, 0, a.z) + offset);
            // Solid collision proxy kept exact for navigation/blockout
            var proxy = Box("Gate collider", new Vector3(a.width / 2f, 0.90f, 0), new Vector3(a.width - 0.025f, 1.80f, 0.08f), Mat("MetalOld"), pivot, solid: true);
            proxy.GetComponent<Renderer>().enabled = false;
            PlaceBlenderModel("CTCC_PROP_ExteriorGate", pivot, Vector3.zero, 0f, pairRB: true);
            // Masonry gate pillars on either side
            foreach (int s in new[] { -1, 1 })
            {
                Box("Gate pillar", new Vector3(a.x + s * (a.width * 0.5f + 0.16f), 1.05f, a.z), new Vector3(0.32f, 2.10f, 0.34f), Mat("PlasterR"), solid: true, tile: 1.5f, isStatic: true);
                Box("Gate pillar cap", new Vector3(a.x + s * (a.width * 0.5f + 0.16f), 2.14f, a.z), new Vector3(0.38f, 0.09f, 0.40f), Mat("BaseTrim"), isStatic: true);
            }
            return new DoorPose { pivot = pivot, openAngle = angle };
        }

        private DoorPose HingedWardDoor(string name, Aperture a, float angle)
        {
            if (!art) return Hinged(name, a, 2.20f, Mat("DoorWood"), angle, false);
            var offset = a.alongX ? new Vector3(-a.width / 2f, 0, 0) : new Vector3(0, 0, -a.width / 2f);
            Transform pivot = Group(name + " · hinge", permanent, new Vector3(a.x, 0, a.z) + offset);
            Transform leaf = Group("Leaf", pivot);
            if (!a.alongX) leaf.localRotation = Quaternion.Euler(0, -90, 0);

            // Exact solid collision proxy matching greybox metrics (1.20 m clear when open!)
            var proxy = Box("Ward door collider", new Vector3(a.width / 2f, 1.10f, 0), new Vector3(a.width - 0.025f, 2.20f, 0.055f), Mat("WoodDoorR"), leaf, solid: true);
            proxy.GetComponent<Renderer>().enabled = false;
            PlaceBlenderModel("CTCC_ENV_WardDoorLeaf", leaf, Vector3.zero, 0f, pairRB: true);

            // Physical handle trigger so player can swing Room 101's timber door open/shut directly!
            CapsuleTarget("DOOR-101", "Cửa phòng 101", new Vector3(a.width - 0.16f, 1.04f, 0f), new Vector3(0.36f, 0.48f, 0.34f), leaf);
            return new DoorPose { pivot = pivot, openAngle = angle };
        }

        private DoorPose Hinged(string name, Aperture a, float height, Material finish, float angle, bool glass)
        {
            var offset = a.alongX ? new Vector3(-a.width / 2, 0, 0) : new Vector3(0, 0, -a.width / 2);
            Transform pivot = Group(name + " · hinge", permanent, new Vector3(a.x, 0, a.z) + offset);
            Transform leaf = Group("Leaf", pivot);
            if (!a.alongX) leaf.localRotation = Quaternion.Euler(0, -90, 0);
            if (!art || !glass)
            {
                Box("Door leaf", new Vector3(a.width / 2, height / 2, 0), new Vector3(a.width - 0.025f, height, 0.065f),
                    finish, leaf, true);
            }
            else
            {
                Box("Kick plate", new Vector3(a.width / 2, 0.40f, 0), new Vector3(a.width - 0.02f, 0.80f, 0.065f), finish, leaf, true);
                Box("Safety glass", new Vector3(a.width / 2, 1.53f, 0), new Vector3(a.width - 0.05f, 1.23f, 0.022f), Mat("FrostedGlass"), leaf, true);
                Box("Tubular left stile", new Vector3(0.045f, 1.08f, 0), new Vector3(0.064f, 2.16f, 0.078f), finish, leaf);
                Box("Tubular right stile", new Vector3(a.width - 0.045f, 1.08f, 0), new Vector3(0.064f, 2.16f, 0.078f), finish, leaf);
                Box("Tubular top rail", new Vector3(a.width / 2, 2.13f, 0), new Vector3(a.width, 0.075f, 0.075f), finish, leaf);
                Box("Push bar", new Vector3(a.width / 2, 1.02f, -0.065f), new Vector3(a.width - 0.24f, 0.045f, 0.035f), Mat("Steel"), leaf);
            }
            return new DoorPose { pivot = pivot, openAngle = angle };
        }

        private void Window(float left, float right, float z, string blenderModel)
        {
            Wall(true, z, left, right, 0, 1.09f);
            Wall(true, z, left, right, 2.94f, 3.6f);
            float midX = (left + right) * 0.5f;
            float w = right - left;

            // Invisible solid collision barrier so capsule never clips outside windows
            var barrier = Box("Window barrier", new Vector3(midX, 2.01f, z + 0.03f), new Vector3(w, 1.85f, 0.035f),
                Mat("Glass"), solid: true);
            if (!art)
            {
                Box("Sill", new Vector3(midX, 1.10f, z), new Vector3(w, 0.065f, 0.4f), Mat("BaseTrim"), solid: true);
                return;
            }
            barrier.GetComponent<Renderer>().enabled = false;

            // Authentic Blender rolled-steel casement window + terrazzo sill + rain hood + louvers + iron security grille!
            var win = PlaceBlenderModel(blenderModel, permanent, new Vector3(midX, 0f, z), 0f, pairRB: true);
            staticArchitecture.Add(win);

            // Animated monsoon raindrop-streaked window glass in R, swapping to clean glass in B!
            var pane = Flat("Window rain glass", "RainGlassR", new Vector3(midX, 1.76f, z + 0.01f), Vector3.back, new Vector2(w - 0.08f, 1.30f), permanent);
            world.finishes.Add(new LayerFinish { renderer = pane.GetComponent<Renderer>(), real = Mat("RainGlassR"), report = Mat("Glass") });
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
            world.Present(false, 0, false, false, false, false);
            Debug.Log("CTCC_GREYBOX_PASS AP00–05, R & 3 B branches, LOC04 via either door, 0.72 m stretcher at 1.2 m doorway");
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
