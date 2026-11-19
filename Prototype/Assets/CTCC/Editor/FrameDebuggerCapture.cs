using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CTCC.Editor
{
    public static class FrameDebuggerCapture
    {
        private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

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
