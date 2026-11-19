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
            world = new GameObject("An Sinh · CTCC M1").AddComponent<HospitalScene>();
            world.referenceArt = art;
            permanent = Group("Physical continuity", world.transform);
            real = Group("R · 2012", world.transform);
            report = Group("B · Report in 2005 dress", world.transform);
            world.realProps = real.gameObject;
            world.reportProps = report.gameObject;
            world.pads = layout.pads.Select(p =>
            {
                var point = Group(p.id, permanent, new Vector3(p.x, .015f, p.z));
                point.rotation = Quaternion.Euler(0, p.yaw, 0);
                return new MarkerPose { id = p.id, point = point };
            }).ToArray();
            PrepareMaterials();
        }

        [MenuItem("Ca Trực Cuối Cùng/1 — Dựng và kiểm mặt bằng")]
        public static void Greybox() => new HospitalAuthoring(false).Generate(true);

        [MenuItem("Ca Trực Cuối Cùng/2 — Dựng cảnh Phòng 101")]
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
            string destination = Argument("-ctcc-build") ?? "../output/CTCC-M1/CaTrucCuoiCung.exe";
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
            return i >= 0 && i+1 < args.Length ? args[i+1] : null;
        }

        private static void ConfigureEnvironment()
        {
            PlayerSettings.companyName = "An Sinh Project";
            PlayerSettings.productName = "Ca trực cuối cùng";
            PlayerSettings.bundleVersion = "0.1.0";
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
            if(art) GroupFixedSurfaces();
            world.Present(false, 0, false, false, false, false);
            if (verify)
                VerifyBlockout();
            EditorSceneManager.SaveScene(scene, SceneFile);
            ConfigureEnvironment();
            Debug.Log($"CTCC_SCENE {(art ? "reference" : "greybox")} {world.finishes.Count} paired finishes");
        }

        private static Transform Group(string name, Transform parent, Vector3 position = default)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.localPosition = position;
            return child;
        }

        private GameObject Box(string name, Vector3 p, Vector3 size, Material material,
            Transform parent = null, bool solid = false, float tile = 0)
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
                Vector3 p = Vector3.Scale(vertices[i],size) + position;
                coordinates[i] = Mathf.Abs(normals[i].y) > .90f
                    ? new Vector2(p.x, p.z) / metres
                    : new Vector2(Mathf.Abs(normals[i].x) > .90f ? p.z : p.x, p.y) / metres;
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

        private GameObject Surface(string name, Vector3 p, Vector3 size, string r, string b, bool solid = false, float tile = 0)
        {
            var item = Box(name,p,size,Mat(r),solid:solid,tile:tile);
            world.finishes.Add(new LayerFinish { renderer=item.GetComponent<Renderer>(), real=Mat(r), report=Mat(b) });
            return item;
        }

        private void Floor(string id, bool covered = true)
        {
            var a = layout.rooms.Single(room => room.id == id);
            Surface(id+" · floor",new Vector3(a.x + a.w/2,-.10f,a.z + a.d/2),
                new Vector3(a.w,.20f,a.d),"FloorR","FloorB",true,1.6f);
            if (covered)
                Surface(id+" · ceiling",new Vector3(a.x+a.w/2,3.71f,a.z+a.d/2),
                    new Vector3(a.w,.22f,a.d),"PlasterR","PlasterB",true,2.15f);
        }

        private void Wall(bool alongX, float axis, float from, float to, float bottom = 0, float top = 3.6f)
        {
            if (to - from < .02f || top-bottom < .01f) return;
            const float thickness = .20f;
            void Segment(string name, float low, float high, string r, string b, bool solid)
            {
                var middle = (from+to)*.5f;
                var point = alongX ? new Vector3(middle,(low+high)*.5f,axis) : new Vector3(axis,(low+high)*.5f,middle);
                var scale = alongX ? new Vector3(to-from,high-low,thickness) : new Vector3(thickness,high-low,to-from);
                Surface(name,point,scale,r,b,solid,2.3f);
            }

            float split = Mathf.Clamp(1.15f,bottom,top);
            if (split > bottom)
                Segment("Wainscot",bottom,split,"DadoR","DadoB",true);
            if (top > split)
                Segment("Paint over lime plaster",split,top,"PlasterR","PlasterB",true);
            if (art && bottom < .01f)
            {
                float middle = (from+to)*.5f;
                Vector3 p = alongX ? new Vector3(middle,.043f,axis) : new Vector3(axis,.043f,middle);
                Vector3 size = alongX ? new Vector3(to-from,.086f,.219f) : new Vector3(.219f,.086f,to-from);
                Box("Terrazzo cove skirting",p,size,Mat("BaseTrim"),tile:.8f);
            }
        }

        private void WallOpening(bool alongX, float axis, float from, float to, float opening, float width, float clear)
        {
            float half = width*.5f;
            Wall(alongX,axis,from,opening-half);
            Wall(alongX,axis,opening+half,to);
            Wall(alongX,axis,opening-half,opening+half,clear,layout.wallHeight);
        }

        private Aperture Opening(string id) => layout.openings.Single(e => e.id == id);

        private void BuildShell()
        {
            foreach (var a in layout.rooms)
                Floor(a.id, a.id != "LOC01");
            Box("Approach at grade",new Vector3(4,-.1f,-22),new Vector3(9,.2f,4),Mat("Exterior"),solid:true,tile:2f);
            Wall(false,-5,-20,-8,0,.85f);
            Wall(false,13,-20,-8,0,.85f);
            Wall(true,-20,-5,2.8f,0,1.72f);
            Wall(true,-20,5.2f,13,0,1.72f);
            var gate = Opening("GATE-CONG");
            world.outerGate = Hinged("GATE-CONG",gate,1.80f,Mat("MetalPaint"),-101,false);
            world.seamBlocker=Box("Exhausted gate seam only",new Vector3(4,1.1f,-20.50f),
                new Vector3(2.44f,2.20f,.07f),Mat("Glass"),solid:true);
            world.seamBlocker.GetComponent<Renderer>().enabled=false;
            world.seamBlocker.SetActive(false);

            Wall(false,-1.1f,-8,0);
            Wall(false,9.1f,-8,0);
            WallOpening(true,-8,-1,9,4,1.8f,2.42f);
            Wall(true,0,1.1f,2.5f);
            Wall(true,0,5.5f,9);
            Wall(false,-1.1f,0,16);
            Wall(false,1.2f,0,16);
            Wall(false,2.4f,0,16);
            WallOpening(false,5.60f,0,16,4.75f,1.2f,2.22f);
            var room = Opening("GATE-101");
            world.roomDoor = Hinged("GATE-101",room,2.20f,Mat("DoorWood"),-105,false);
            if (art) DoorFrame(room,2.22f,Mat("MetalPaint"));

            Surface("Continuous 101 threshold",new Vector3(5.60f,-.1f,4.75f),new Vector3(.22f,.20f,1.2f),
                "FloorR","FloorB",true,1.6f);
            Surface("Continuous store threshold",new Vector3(13.80f,-.1f,3.62f),new Vector3(.22f,.20f,1.22f),
                "FloorR","FloorB",true,1.6f);
            Wall(true,1.90f,5.7f,13.9f);
            Wall(true,8.10f,5.7f,6.28f);
            Window(6.28f,8.42f,8.10f);
            Wall(true,8.10f,8.42f,9.23f);
            Window(9.23f,11.32f,8.10f);
            Wall(true,8.10f,11.32f,11.75f);
            Window(11.75f,13.03f,8.10f);
            Wall(true,8.10f,13.03f,13.9f);
            WallOpening(false,13.80f,2,8,3.62f,1.22f,2.20f);
            Wall(false,17.2f,2,5.6f);
            Wall(true,1.90f,13.9f,17.1f);
            Wall(true,5.70f,13.9f,17.1f);
            if (art) DoorFrame(Opening("GATE-STORE"),2.20f,Mat("DoorWood"));

            Wall(false,-1.1f,16,22);
            Wall(false,9.1f,16,22);
            Wall(true,22.1f,-1,9);
            Wall(true,16,-1,-.65f);
            Wall(true,16,.75f,3.3f);
            Wall(true,16,4.7f,9);
            Wall(true,16,-.65f,.75f,2.20f);
            Wall(true,16,3.3f,4.7f,2.20f);
            world.emergencyDoorA = Hinged("GATE-CAPCUU",Opening("GATE-CAPCUU"),2.18f,Mat("MetalPaint"),-94,true);
            world.emergencyDoorB = Hinged("GATE-CAPCUU-B",Opening("GATE-CAPCUU-B"),2.18f,Mat("MetalPaint"),-94,true);
        }

        private DoorPose Hinged(string name, Aperture a, float height, Material finish, float angle, bool glass)
        {
            var offset = a.alongX ? new Vector3(-a.width/2,0,0) : new Vector3(0,0,-a.width/2);
            Transform pivot = Group(name+" · hinge",permanent,new Vector3(a.x,0,a.z)+offset);
            Transform leaf = Group("Leaf",pivot);
            if (!a.alongX) leaf.localRotation = Quaternion.Euler(0,-90,0);
            if (!art || !glass)
            {
                Box("Door leaf",new Vector3(a.width/2,height/2,0),new Vector3(a.width-.025f,height,.065f),
                    finish,leaf,true);
            }
            else
            {
                Box("Kick plate",new Vector3(a.width/2,.40f,0),new Vector3(a.width-.02f,.80f,.065f),finish,leaf,true);
                Box("Safety glass",new Vector3(a.width/2,1.53f,0),new Vector3(a.width-.05f,1.23f,.022f),
                    Mat("Glass"),leaf,true);
                Box("Tubular left stile",new Vector3(.045f,1.08f,0),new Vector3(.064f,2.16f,.078f),finish,leaf);
                Box("Tubular right stile",new Vector3(a.width-.045f,1.08f,0),new Vector3(.064f,2.16f,.078f),finish,leaf);
                Box("Tubular top rail",new Vector3(a.width/2,2.13f,0),new Vector3(a.width,.075f,.075f),finish,leaf);
            }
            if (art)
            {
                for (int side = -1; side <= 1; side += 2)
                    Box("Lever handle",new Vector3(a.width-.15f,1.015f,side*.060f),new Vector3(.155f,.032f,.044f),Mat("Steel"),leaf);
                for (int i=0;i<3;i++)
                    Box("Hinge knuckle",new Vector3(.02f,.28f+i*.68f,.04f),new Vector3(.039f,.094f,.037f),Mat("Steel"),leaf);
            }
            return new DoorPose { pivot = pivot, openAngle = angle };
        }

        private void DoorFrame(Aperture a, float height, Material finish)
        {
            Transform frame = Group("Lipped metal door frame",permanent,new Vector3(a.x,0,a.z));
            if (!a.alongX) frame.localRotation = Quaternion.Euler(0,-90,0);
            for (int side = -1; side <= 1; side += 2)
                Box("Jamb",new Vector3(side*(a.width/2+.015f),height/2,0),new Vector3(.055f,height,.24f),finish,frame);
            Box("Header",new Vector3(0,height+.022f,0),new Vector3(a.width+.1f,.064f,.24f),finish,frame);
            Box("Threshold bar",new Vector3(0,.011f,0),new Vector3(a.width,.022f,.086f),Mat("Steel"),frame);
        }

        private void Window(float left, float right, float z)
        {
            Wall(true,z,left,right,0,1.09f);
            Wall(true,z,left,right,2.94f,3.6f);
            Box("Sill",new Vector3((left+right)/2,1.10f,z),new Vector3(right-left,.065f,.4f),
                Mat("BaseTrim"),solid:true);
            Box("Window barrier",new Vector3((left+right)/2,2.01f,z+.03f),new Vector3(right-left,1.85f,.035f),
                Mat("Glass"),solid:true);
            if (!art) return;
            int panels = right-left > 1.5f ? 4 : 2;
            for (int i=0;i<=panels;i++)
                Box("Window · rolled steel mullion", new Vector3(Mathf.Lerp(left+.022f,right-.022f,i/(float)panels),2.01f,z-.04f),
                    new Vector3(.046f,1.84f,.086f),Mat("MetalPaint"));
            foreach (float y in new[] {1.13f,1.69f,2.44f,2.91f})
                Box("Window · horizontal rail",new Vector3((left+right)/2,y,z-.04f),
                    new Vector3(right-left,.046f,.094f),Mat("MetalPaint"));
            for (int i=0;i<6;i++)
                Box("Fixed high transom blade",new Vector3((left+right)/2,2.48f+i*.074f,z-.02f),
                    new Vector3(right-left,.024f,.102f),Mat("Frame"));
        }

        private void VerifyBlockout()
        {
            Transform ap04 = world.Pad("AP04");
            if (Mathf.Abs(layout.wallHeight-3.6f) > .001f || layout.beds.Length != 4)
                throw new InvalidOperationException("Core 03 dimensions differ");
            world.Present(false, 4, true, true, true, true);
            VerifyNav("R / all relevant doors",new[] { "AP00","AP01","AP02","AP03","AP04","AP05" });
            world.emergencyDoorA.Set(false);
            Physics.SyncTransforms();
            VerifyNav("LOC04 / staff route",new[] { "AP02","AP05" });
            world.emergencyDoorA.Set(true);
            world.emergencyDoorB.Set(false);
            Physics.SyncTransforms();
            VerifyNav("LOC04 / main route",new[] { "AP02","AP05" });
            for (int route=2;route<=4;route++)
            {
                world.Present(true,route,true,true,false,false);
                if (HospitalScene.Occupied(ap04.position))
                    throw new InvalidOperationException("AP04 collision after F101 branch " + route);
                VerifyNav("F101 route " + route,new[] { "AP01","AP02","AP03","AP04" });
            }
            world.Present(false,0,true,true,false,false);
            Vector3 through = new Vector3(5.6f,.30f,4.75f);
            bool stretcherClear = !Physics.CheckBox(through,new Vector3(.99f,.25f,.36f),Quaternion.identity,
                HospitalScene.SolidMask,QueryTriggerInteraction.Ignore);
            if (!stretcherClear)
                throw new InvalidOperationException("0.72 m stretcher proxy collided at the 101 doorway.");
            world.Present(false,0,false,false,false,false);
            Debug.Log("CTCC_GREYBOX_PASS AP00–05, R & 3 B branches, LOC04 via either door, 0.72 m stretcher at 1.2 m doorway");
        }

        private void VerifyNav(string label,string[] pads)
        {
            var nav = world.BuildNavigation();
            if (nav == null) throw new InvalidOperationException("No NavMesh: " + label);
            var handle = NavMesh.AddNavMeshData(nav);
            try
            {
                var spots = new Vector3[pads.Length];
                for (int i=0;i<pads.Length;i++)
                {
                    Vector3 point = world.Pad(pads[i]).position;
                    if (HospitalScene.Occupied(point) || !NavMesh.SamplePosition(point,out var nearest,.36f,NavMesh.AllAreas))
                        throw new InvalidOperationException("Invalid pad " + pads[i] + " / " + label);
                    spots[i]=nearest.position;
                }
                foreach (var from in spots)
                    foreach (var to in spots)
                    {
                        var path = new NavMeshPath();
                        if (!NavMesh.CalculatePath(from,to,NavMesh.AllAreas,path) || path.status != NavMeshPathStatus.PathComplete)
                            throw new InvalidOperationException("Disconnected pads " + from + " → " + to + " / " + label);
                    }
                Debug.Log("CTCC_ROUTE_PASS " + label + " / " + string.Join(", ",pads));
            }
            finally
            {
                handle.Remove();
                UnityEngine.Object.DestroyImmediate(nav);
            }
        }
    }
}
