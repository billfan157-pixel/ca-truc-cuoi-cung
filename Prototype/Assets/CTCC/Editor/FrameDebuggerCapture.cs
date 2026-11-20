using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace CTCC.Editor
{
    public static class FrameDebuggerCapture
    {
        private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static void AuditDigitalHumanShaders()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-ctcc-frame-log");
            if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("Supply -ctcc-frame-log");
            var compatibility = typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode", Static);
            var reason = typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityIssueReason", Static);
            var rows = new List<string> { $"Unity {Application.unityVersion} imported shader and SRP Batcher audit" };

            string[] shaderNames = { "DigitalHumanSkin", "DigitalHumanEye", "DigitalHumanHair", "DigitalHumanFabric", "ApparitionHuman" };
            var warmupRt = new RenderTexture(64, 64, 24);
            warmupRt.Create();
            var camGo = new GameObject("ShaderAuditWarmupCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.targetTexture = warmupRt;
            cam.transform.position = new Vector3(0f, 0f, -3f);
            var tempObjects = new List<GameObject>();
            var tempMaterials = new List<Material>();
            try
            {
                for (int s = 0; s < shaderNames.Length; s++)
                {
                    var sh = AssetDatabase.LoadAssetAtPath<Shader>($"Assets/CTCC/Art/{shaderNames[s]}.shader");
                    if (sh == null) continue;
                    var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    quad.transform.position = new Vector3((s - 2) * 0.4f, 0f, 0f);
                    var mat = new Material(sh);
                    quad.GetComponent<Renderer>().sharedMaterial = mat;
                    tempObjects.Add(quad);
                    tempMaterials.Add(mat);
                }
                cam.Render();
            }
            finally
            {
                foreach (var go in tempObjects) UnityEngine.Object.DestroyImmediate(go);
                foreach (var mat in tempMaterials) UnityEngine.Object.DestroyImmediate(mat);
                cam.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(camGo);
                warmupRt.Release();
                UnityEngine.Object.DestroyImmediate(warmupRt);
            }

            foreach (string name in shaderNames)
            {
                var shader = AssetDatabase.LoadAssetAtPath<Shader>($"Assets/CTCC/Art/{name}.shader");
                if (shader == null) throw new InvalidOperationException($"Missing shader {name}");
                var messages = ShaderUtil.GetShaderMessages(shader);
                if (messages.Any(m => m.severity.ToString() == "Error"))
                    throw new InvalidOperationException($"{name}: {string.Join("; ", messages.Select(m => m.message))}");
                var material = new Material(shader);
                try
                {
                    int? code = compatibility == null ? null : (int)compatibility.Invoke(null, new object[] { shader, 0 });
                    string issue = code.GetValueOrDefault() != 0
                        ? (string)reason?.Invoke(null, new object[] { shader, 0, code.Value }) : "";
                    rows.Add($"{shader.name} | import errors=0 | SRP compatibility code={code?.ToString() ?? "unavailable"} {issue}".TrimEnd());
                    for (int i = 0; i < material.passCount; i++) rows.Add($"  {i}: {material.GetPassName(i)}");
                }
                finally { UnityEngine.Object.DestroyImmediate(material); }
            }
            File.WriteAllLines(Path.GetFullPath(args[index + 1]), rows);

            int animIdx = Array.IndexOf(args, "-ctcc-anim-log");
            if (animIdx >= 0 && animIdx + 1 < args.Length)
            {
                var animRows = new List<string>();
                foreach (var entry in new[] {
                    ("Assets/CTCC/Art/Meshes/CTCC_CHR_Lan.fbx", "Lan_Armature|Lan_StepOut"),
                    ("Assets/CTCC/Art/Meshes/CTCC_CHR_Gatekeeper.fbx", "GK_Armature|GK_StalkWalk") })
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(entry.Item1);
                    var clips = AssetDatabase.LoadAllAssetsAtPath(entry.Item1).OfType<AnimationClip>().ToArray();
                    var clip = clips.FirstOrDefault(c => c.name == entry.Item2);
                    var anims = prefab != null ? prefab.GetComponentsInChildren<Animator>(true) : Array.Empty<Animator>();
                    animRows.Add($"FBX={entry.Item1} animators={anims.Length} names=[{string.Join(",", anims.Select(a => a.name + "(av=" + (a.avatar != null ? a.avatar.name : "null") + ")"))}]");
                    if (clip != null)
                    {
                        var bindings = AnimationUtility.GetCurveBindings(clip);
                        animRows.Add($"  CLIP={clip.name} len={clip.length:F3} legacy={clip.legacy} bindings={bindings.Length}");
                        foreach (var b in bindings.Where(b => b.path.Contains("Leg") || b.path.Contains("Foot") || b.path.Contains("Toe") || b.path.Contains("Hips")).Take(12))
                        {
                            var curve = AnimationUtility.GetEditorCurve(clip, b);
                            animRows.Add($"    path='{b.path}' prop='{b.propertyName}' keys={curve.length} v0={curve.Evaluate(0f):F4} v05={curve.Evaluate(0.5f):F4}");
                        }
                        if (prefab != null)
                        {
                            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                            try
                            {
                                var toe = inst.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "mixamorig:LeftToeBase");
                                var animComp = inst.GetComponentInChildren<Animator>(true) ?? inst.AddComponent<Animator>();
                                animComp.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                                var g = UnityEngine.Playables.PlayableGraph.Create("Diag");
                                g.SetTimeUpdateMode(UnityEngine.Playables.DirectorUpdateMode.Manual);
                                var p = UnityEngine.Animations.AnimationClipPlayable.Create(g, clip);
                                var o = UnityEngine.Animations.AnimationPlayableOutput.Create(g, "A", animComp);
                                o.SetSourcePlayable(p);
                                Vector3 minZ = new Vector3(99, 99, 99), maxZ = new Vector3(-99, -99, -99);
                                for (int f = 0; f <= 30; f++)
                                {
                                    p.SetTime(f / 30.0);
                                    g.Evaluate(0f);
                                    if (toe != null)
                                    {
                                        var lp = inst.transform.InverseTransformPoint(toe.position);
                                        minZ = Vector3.Min(minZ, lp);
                                        maxZ = Vector3.Max(maxZ, lp);
                                    }
                                }
                                g.Destroy();
                                animRows.Add($"  PLAYABLE_GRAPH_EVAL toeFound={toe != null} min=({minZ.x:F3},{minZ.y:F3},{minZ.z:F3}) max=({maxZ.x:F3},{maxZ.y:F3},{maxZ.z:F3})");
                            }
                            finally { UnityEngine.Object.DestroyImmediate(inst); }
                        }
                    }
                }
                File.WriteAllLines(Path.GetFullPath(args[animIdx + 1]), animRows);
            }
        }

        public static void Room101()
        {
            int reportIndex = Array.IndexOf(Environment.GetCommandLineArgs(), "-ctcc-frame-log");
            var arguments = Environment.GetCommandLineArgs();
            string report = reportIndex >= 0 && reportIndex + 1 < arguments.Length
                ? arguments[reportIndex + 1] : "../output/frame-debugger.txt";
            var editor = typeof(EditorWindow).Assembly;
            var debug = editor.GetType("UnityEditorInternal.FrameDebuggerInternal.FrameDebuggerUtility");
            var profiler = editor.GetType("UnityEditorInternal.ProfilerDriver");
            var set = debug?.GetMethod("SetEnabled", Static);
            var read = debug?.GetMethod("GetFrameEvents", Static);
            var name = debug?.GetMethod("GetFrameEventInfoName", Static);
            if (set == null || read == null || name == null)
                throw new MissingMethodException("Frame Debugger endpoint unavailable.");

            EditorSceneManager.OpenScene("Assets/CTCC/Scenes/AnSinh.unity");
            var setDressing = UnityEngine.Object.FindFirstObjectByType<HospitalScene>();
            setDressing.Present(false,2,true,true,false,false);
            var camera = Camera.main;
            camera.transform.SetPositionAndRotation(new Vector3(6.23f,1.64f,4.93f),Quaternion.Euler(0,78,0));
            EditorApplication.ExecuteMenuItem("Window/Analysis/Frame Debugger");
            var game = EditorWindow.GetWindow(editor.GetType("UnityEditor.GameView"));
            game.Focus();
            double opened = EditorApplication.timeSinceStartup;
            bool enabled = false;
            int connection = (int)profiler.GetProperty("connectedProfiler",Static).GetValue(null);

            EditorApplication.update += Tick;
            void Tick()
            {
                game.Repaint();
                double elapsed = EditorApplication.timeSinceStartup-opened;
                if (!enabled)
                {
                    if (elapsed < 2) return;
                    enabled = true;
                    set.Invoke(null,new object[] { true, connection });
                }
                if (camera != null) camera.Render();
                var events = (Array)read.Invoke(null,null);
                if ((events == null || events.Length == 0) && elapsed < 22) return;
                EditorApplication.update -= Tick;
                var lines = new List<string>
                {
                    "Unity "+Application.unityVersion,
                    "Scene: LOC03 / R / 2012, camera x6.23 y1.64 z4.93",
                    "Event count: "+(events?.Length ?? 0)
                };
                if (events != null)
                {
                    for (int i = 0; i < events.Length; i++)
                        lines.Add((i+1)+" "+(string)name.Invoke(null,new object[] { i }));
                }
                File.WriteAllLines(Path.GetFullPath(report),lines);
                set.Invoke(null,new object[] { false, connection });
                EditorApplication.Exit(events != null && events.Length > 0 ? 0 : 1);
            }
        }
    }
}
