using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace CTCC.Spike
{
    public static class BuildSpike
    {
        public static void BuildWindows()
        {
            string scenePath=CreateScene();
            bool high=ActivePipeline.GetType().Name.Contains("HDRender");
            var report=BuildPipeline.BuildPlayer(new[] { scenePath }, "../output/" + (high ? "HDRP" : "URP") + "/Spike.exe",
                BuildTarget.StandaloneWindows64, BuildOptions.None);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException(report.summary.result.ToString());
            Debug.Log("CTCC_SPIKE_BUILD_BYTES=" + report.summary.totalSize);
        }

        public static void BenchmarkInEditor()
        {
            CreateScene();
            EditorApplication.isPlaying = true;
        }

        private static string CreateScene()
        {
            string pipeline = ActivePipeline.GetType().Name;
            bool high = pipeline.Contains("HDRender");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Directory.CreateDirectory("Assets/Spike/Generated");
            var plaster = Surface("Plaster", new Color(.63f, .68f, .62f), .24f);
            var enamel = Surface("Enamel", new Color(.29f, .43f, .39f), .54f);
            var tile = Surface("Tile", new Color(.63f, .62f, .51f), .48f);
            var linen = Surface("Linen", new Color(.72f, .76f, .68f), .16f);
            var paintedSteel = Surface("Trim", new Color(.22f, .28f, .24f), .62f);
            var obj = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Spike/BedRail.obj");
            if (obj == null)
                throw new InvalidOperationException("OBJ import failed");

            Box("LOC03 floor", new Vector3(6.5f,-.1f,5), new Vector3(8,.2f,6), tile);
            Box("Corridor floor", new Vector3(1,-.1f,5), new Vector3(3,.2f,6), tile);
            Box("LOC03 ceiling", new Vector3(6.5f,3.7f,5), new Vector3(8,.2f,6), plaster, false);
            Box("Outer wall", new Vector3(10.6f,1.8f,5), new Vector3(.2f,3.6f,6), plaster);
            Box("Inner wall", new Vector3(6.5f,1.8f,1.9f), new Vector3(8,3.6f,.2f), plaster);
            Box("Window sill", new Vector3(6.5f,.59f,8), new Vector3(8,1.18f,.2f), plaster);
            Box("Window lintel", new Vector3(6.5f,3.3f,8), new Vector3(8,.6f,.2f), plaster);
            Box("Corridor wall", new Vector3(-.6f,1.8f,5), new Vector3(.2f,3.6f,6), plaster);
            Box("Room split near", new Vector3(2.5f,1.8f,3.1f), new Vector3(.2f,3.6f,2.2f), plaster);
            Box("Room split far", new Vector3(2.5f,1.8f,6.7f), new Vector3(.2f,3.6f,2.6f), plaster);
            Box("Room split header", new Vector3(2.5f,2.9f,4.7f), new Vector3(.2f,1.4f,1.4f), plaster);
            for (int i = 0; i < 9; i++)
                Box("Window mullion", new Vector3(2.63f + i * .97f, 2.09f, 8), new Vector3(.047f,1.82f,.12f), paintedSteel, false);
            foreach (var origin in new[] { new Vector3(4.55f,0,6.95f), new Vector3(7.02f,0,6.95f), new Vector3(5f,0,3.05f) })
            {
                Box("Bed base", origin + Vector3.up * .53f, new Vector3(.87f,.13f,2.02f), enamel);
                Box("Mattress", origin + Vector3.up * .667f, new Vector3(.81f,.14f,1.95f), linen);
                Box("Pillow", origin + new Vector3(0,.78f,.67f), new Vector3(.65f,.12f,.43f), linen, false);
                for (int a = -1; a <= 1; a += 2)
                    for (int b = -1; b <= 1; b += 2)
                        Box("Bed leg", origin + new Vector3(.35f*a,.28f,.80f*b), new Vector3(.042f,.55f,.042f), enamel, false);
                var rail = (GameObject)PrefabUtility.InstantiatePrefab(obj);
                rail.transform.position = origin + new Vector3(0,.73f,.97f);
                rail.transform.localScale = new Vector3(.88f,.41f,.035f);
                foreach (var renderer in rail.GetComponentsInChildren<MeshRenderer>())
                    renderer.sharedMaterial = paintedSteel;
            }
            var partition = Box("B — fourth bay", new Vector3(8.9f,1.6f,3.9f),
                new Vector3(2.8f,3.2f,.1f), enamel);
            partition.SetActive(false);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.20f,.24f,.28f);
            var sun = new GameObject("Daylight through the same windows").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(42, 156, 0);
            sun.color = new Color(.76f,.87f,1);
            sun.intensity = high ? 12000 : 2.9f;
            sun.shadows = LightShadows.Soft;
            for (int i = 0; i < 4; i++)
            {
                var lamp = new GameObject("Tube fixture fill").AddComponent<Light>();
                lamp.transform.position = new Vector3(2.2f + i * 2.18f, 3.25f, 4.8f);
                lamp.type = LightType.Point;
                lamp.range = 5.7f;
                lamp.intensity = high ? 550 : 2.55f;
                lamp.color = new Color(.78f,1,.90f);
            }
            var camera = new GameObject("Same path and 68° vertical FOV").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.21f,.29f,.34f);
            camera.fieldOfView = 68f;
            camera.nearClipPlane = .06f;
            camera.farClipPlane = 40;
            var run = new GameObject("TASK029 profiler").AddComponent<SpikeRun>();
            run.eye = camera;
            run.partition = partition;
            const string scenePath = "Assets/Spike/Generated/Room101.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            PlayerSettings.companyName = "An Sinh Project";
            PlayerSettings.productName = "CTCC TASK029 " + (high ? "HDRP" : "URP");
            PlayerSettings.enableFrameTimingStats = true;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D12 });
            return scenePath;
        }

        private static Material Surface(string name, Color color, float smoothness)
        {
            var path = "Assets/Spike/Generated/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(ActivePipeline.defaultMaterial);
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject Box(string name, Vector3 position, Vector3 size, Material material, bool solid = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetPositionAndRotation(position, Quaternion.identity);
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (solid)
                go.layer = 9;
            else
                UnityEngine.Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            return go;
        }

        private static RenderPipelineAsset ActivePipeline => GraphicsSettings.currentRenderPipeline ??
            GraphicsSettings.defaultRenderPipeline ?? throw new InvalidOperationException("Template has no active render pipeline.");
    }
}
