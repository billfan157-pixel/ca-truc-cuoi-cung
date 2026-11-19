using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace CTCC.Spike
{
    public sealed class SpikeRun : MonoBehaviour
    {
        public Camera eye;
        public GameObject partition;

        private IEnumerator Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            Application.runInBackground = true;
            if (!Application.isEditor)
                Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            var args = Environment.GetCommandLineArgs();
            int outputIndex = Array.IndexOf(args, "-ctcc-report");
            if (outputIndex < 0 || outputIndex + 1 == args.Length)
                yield break;

            string output = Path.GetFullPath(args[outputIndex + 1]);
            Directory.CreateDirectory(output);
            var facts = new System.Collections.Generic.List<string>
            {
                "engine=" + Application.unityVersion,
                "run_mode=" + (Application.isEditor ? "Editor" : "Windows Player"),
                "pipeline=" + GraphicsSettings.currentRenderPipeline.GetType().Name,
                "gpu=" + SystemInfo.graphicsDeviceName,
                "api=" + SystemInfo.graphicsDeviceType,
                "graphics_mb=" + SystemInfo.graphicsMemorySize,
                "system_mb=" + SystemInfo.systemMemorySize
            };

            float sceneStartupMs = Time.realtimeSinceStartup * 1000;
            bool offscreen=Application.isEditor || Array.IndexOf(args,"-ctcc-offscreen")>=0;
            var locked = new double[420];
            RenderPipeline.StandardRequest shot = null;
            if (offscreen)
            {
                eye.targetTexture=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGBHalf);
                eye.targetTexture.Create();
                eye.enabled=false;
                shot=new RenderPipeline.StandardRequest{destination=eye.targetTexture};
                if (!RenderPipeline.SupportsRenderRequest(eye,shot))
                    throw new InvalidOperationException("Render pipeline does not support the common offscreen request.");
                facts.Add("timing_mode=Offscreen render with synchronized 1px readback");
            }
            else
                facts.Add("timing_mode=Active player swapchain");
            facts.Add("start_ms=" + F(sceneStartupMs));
            facts.Add("nav_rebuild_ms=" + F(CheckNavigation()));
            facts.Add("atomic_read=" + CheckSnapshot(output));
            int denied = 0;
            for (int i = 0; i < 30; i++)
            {
                partition.SetActive(true);
                Physics.SyncTransforms();
                if (Physics.CheckCapsule(new Vector3(8.9f, .36f, 3.9f),
                        new Vector3(8.9f, 1.39f, 3.9f), .35f, 1 << 9))
                    denied++;
                partition.SetActive(false);
            }
            Physics.SyncTransforms();
            facts.Add("occupied_landing_denials=" + denied);

            const int frames = 420;
            const int warmup = 150;
            var elapsed = new double[frames];
            var gpu = new double[frames];
            var gpuSample = new FrameTiming[1];
            using var main = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");
            using var batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            using var render = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Render Thread");
            var draws = new long[frames];
            var cpuMain = new long[frames];
            var cpuRender = new long[frames];
            for (int i = -warmup; i < frames; i++)
            {
                float t = Mathf.Repeat(i / 420f, 1f);
                eye.transform.position = new Vector3(2.85f + Mathf.Sin(t * Mathf.PI * 2) * .38f,
                    1.62f, Mathf.Lerp(4.27f, 6.95f, t));
                eye.transform.LookAt(new Vector3(6.9f, 1.4f, 4.5f));
                partition.SetActive(i >= frames / 2);
                if(offscreen)
                {
                    long begin=Stopwatch.GetTimestamp();
                    RenderPipeline.SubmitRenderRequest(eye,shot);
                    var fence=AsyncGPUReadback.Request(shot.destination,0,0,1,0,1,0,1);
                    fence.WaitForCompletion();
                    if(fence.hasError) throw new InvalidOperationException("Offscreen render readback failed.");
                    if(i>=0) locked[i]=(Stopwatch.GetTimestamp()-begin)*1000d/Stopwatch.Frequency;
                }
                FrameTimingManager.CaptureFrameTimings();
                yield return null;
                if (i < 0)
                    continue;
                elapsed[i] = Time.unscaledDeltaTime * 1000;
                if (FrameTimingManager.GetLatestTimings(1, gpuSample) > 0)
                    gpu[i] = gpuSample[0].gpuFrameTime;
                draws[i] = batches.LastValue;
                cpuMain[i] = main.LastValue;
                cpuRender[i] = render.LastValue;
            }
            facts.Add("screen=" + eye.pixelWidth + "x" + eye.pixelHeight);
            facts.Add("frames=" + frames);
            facts.Add("frame_p50_ms=" + F(P(elapsed, .50)));
            facts.Add("frame_p95_ms=" + F(P(elapsed, .95)));
            if(offscreen) facts.Add("offscreen_render_p95_ms=" + F(P(locked,.95)));
            facts.Add("gpu_p95_ms=" + F(P(gpu.Where(n => n > 0).ToArray(), .95)));
            facts.Add("main_p95_ms=" + F(P(cpuMain.Select(n => n / 1000000d).ToArray(), .95)));
            facts.Add("render_p95_ms=" + F(P(cpuRender.Select(n => n / 1000000d).ToArray(), .95)));
            facts.Add("draws_p95=" + F(P(draws.Select(n => (double)n).ToArray(), .95)));
            facts.Add("unity_allocated_mb=" + F(Profiler.GetTotalAllocatedMemoryLong() / 1048576d));
            facts.Add("working_set_mb=" + F(Process.GetCurrentProcess().WorkingSet64 / 1048576d));
            File.WriteAllLines(Path.Combine(output, "benchmark.txt"), facts);
            partition.SetActive(false);
            eye.transform.position = new Vector3(3.58f, 1.62f, 4.69f);
            eye.transform.LookAt(new Vector3(8f, 1.2f, 4.85f));
            yield return null;
            if (eye.targetTexture != null)
            {
                RenderPipeline.SubmitRenderRequest(eye,shot);
                var old=RenderTexture.active;
                RenderTexture.active=eye.targetTexture;
                var texture=new Texture2D(eye.pixelWidth,eye.pixelHeight,TextureFormat.RGB24,false);
                texture.ReadPixels(new Rect(0,0,eye.pixelWidth,eye.pixelHeight),0,0);
                texture.Apply();
                File.WriteAllBytes(Path.Combine(output,"view.png"),texture.EncodeToPNG());
                RenderTexture.active=old;
            }
            else
            {
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "view.png"));
                yield return new WaitForSecondsRealtime(.65f);
            }
            Debug.Log(string.Join("\n", facts));
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(denied == 30 ? 0 : 1);
#else
            Application.Quit(denied == 30 ? 0 : 1);
#endif
        }

        private float CheckNavigation()
        {
            var timer = Stopwatch.StartNew();
            var sources = new System.Collections.Generic.List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(new Bounds(new Vector3(5, 1, 4), new Vector3(13, 4, 8)),
                1 << 9, NavMeshCollectGeometry.PhysicsColliders, 0,
                new System.Collections.Generic.List<NavMeshBuildMarkup>(), sources);
            var settings = NavMesh.GetSettingsByIndex(0);
            settings.agentRadius = .35f;
            settings.agentHeight = 1.75f;
            var mesh = NavMeshBuilder.BuildNavMeshData(settings, sources,
                new Bounds(new Vector3(5, 1, 4), new Vector3(13, 4, 8)), Vector3.zero, Quaternion.identity);
            if (mesh == null)
                throw new InvalidOperationException("Missing spike navigation data");
            var instance = NavMesh.AddNavMeshData(mesh);
            if (!NavMesh.SamplePosition(new Vector3(4.8f,0,4.7f), out _, .5f, NavMesh.AllAreas))
                throw new InvalidOperationException("Invalid spike landing");
            instance.Remove();
            Destroy(mesh);
            return (float)timer.Elapsed.TotalMilliseconds;
        }

        private static bool CheckSnapshot(string folder)
        {
            var path = Path.Combine(folder, "spike-snapshot.json");
            File.WriteAllText(path, "{\"layer\":\"R\",\"revision\":0}");
            File.WriteAllText(path + ".tmp", "{\"layer\":\"B\",\"revision\":1}");
            File.Replace(path + ".tmp", path, path + ".bak");
            return File.ReadAllText(path).Contains("\"revision\":1") &&
                   File.ReadAllText(path + ".bak").Contains("\"revision\":0");
        }

        private static string F(double value) => double.IsNaN(value) ? "NA" : value.ToString("0.00", CultureInfo.InvariantCulture);

        private static double P(double[] values, double percentile)
        {
            if (values.Length == 0)
                return double.NaN;
            Array.Sort(values);
            return values[(int)Math.Ceiling(percentile * values.Length) - 1];
        }
    }
}
