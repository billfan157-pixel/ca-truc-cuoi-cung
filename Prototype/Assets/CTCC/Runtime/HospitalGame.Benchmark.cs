using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace CTCC
{
    public sealed partial class HospitalGame
    {
        [DllImport("kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long value);

        private static long PerformanceCounter()
        {
            if (!QueryPerformanceCounter(out long value)) throw new InvalidOperationException("Windows QPC unavailable.");
            return value;
        }

        private string benchmarkStage;
        private Light benchmarkTorch;
        private Func<string, Task<CommitResult>> benchmarkTake;
        private readonly List<(string name, long qpc, double milliseconds)> benchmarkActions = new();

        private IEnumerator BenchmarkVisit(string folder)
        {
            if (Argument("-ctcc-save") == null || transaction.Inspect(SaveSlot.Auto) != SaveRead.Empty)
            {
                Debug.LogError("Benchmark requires -ctcc-save pointing to an empty isolated save directory.");
                Application.Quit(1);
                yield break;
            }
            Directory.CreateDirectory(folder);
            string quality = Argument("-ctcc-quality");
            if (quality != null)
            {
                int index = Array.IndexOf(QualitySettings.names, quality);
                if (index < 0)
                {
                    Debug.LogError("Unknown quality preset " + quality);
                    Application.Quit(1);
                    yield break;
                }
                if (QualitySettings.GetQualityLevel() != index) QualitySettings.SetQualityLevel(index, true);
            }
            QualitySettings.vSyncCount = Argument("-ctcc-vsync") == "1" ? 1 : 0;
            Application.targetFrameRate = -1;
            player.CanPlay = false;
            ui.Close();
            var warmup = Time.realtimeSinceStartupAsDouble;
            while (Time.realtimeSinceStartupAsDouble - warmup < 5) yield return null;

            using var draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            using var passes = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            using var triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            using var native = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
            using var collect = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "CTCC.Navigation.Collect");
            using var bake = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "CTCC.Navigation.Build");
            using var probe = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "CTCC.Reflection.Schedule");
            using var write = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "CTCC.Save.AtomicWrite");
            using var flush = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "CTCC.Save.DurableFlush");
            var samples = new List<BenchmarkFrame>(18000);
            var timing = new FrameTiming[1];
            bool passed = false;
            Exception error = null;
            using var route = BenchmarkRoute();
            try
            {
                if (Screen.width != 1920 || Screen.height != 1080 || player.Eye.targetTexture != null)
                    throw new InvalidOperationException($"Expected onscreen 1920x1080; got {Screen.width}x{Screen.height}.");
                benchmarkTorch = player.Eye.GetComponentInChildren<Light>(true);
                var takeAsync = typeof(Casework).GetMethod("TakeAsync");
                if (takeAsync != null) benchmarkTake = (Func<string, Task<CommitResult>>)takeAsync.CreateDelegate(typeof(Func<string, Task<CommitResult>>), casework);
                ApplyBenchmarkAblation(Argument("-ctcc-ablation") ?? "none");
            }
            catch (Exception ex) { error = ex; }

            while (error == null)
            {
                try
                {
                    if (!route.MoveNext())
                    {
                        passed = State.Happened("M1-complete") && GateService.G02(State);
                        break;
                    }
                }
                catch (Exception ex) { error = ex; break; }
                FrameTimingManager.CaptureFrameTimings();
                bool available = FrameTimingManager.GetLatestTimings(1, timing) > 0;
                samples.Add(new BenchmarkFrame(benchmarkStage, PerformanceCounter(), Time.unscaledDeltaTime * 1000,
                    available ? timing[0].cpuMainThreadFrameTime : 0, available ? timing[0].cpuRenderThreadFrameTime : 0,
                    available ? timing[0].gpuFrameTime : 0, draws.LastValue, passes.LastValue, triangles.LastValue,
                    native.LastValue, Profiler.GetTotalAllocatedMemoryLong(), Counter(collect), Counter(bake), Counter(probe), Counter(write), Counter(flush), Application.isFocused));
                yield return null;
            }

            using (var frames = File.CreateText(Path.Combine(folder, "frames.csv")))
            {
                frames.WriteLine("stage,qpc,wall_ms,main_ms,render_ms,gpu_ms,draws,setpass,triangles,system_bytes,unity_bytes,nav_collect_ns,nav_bake_ns,probe_schedule_ns,save_ns,save_flush_ns,focused");
                foreach (var sample in samples) frames.WriteLine(sample.ToCsv());
            }
            using (var actions = File.CreateText(Path.Combine(folder, "actions.csv")))
            {
                actions.WriteLine("action,qpc,ms");
                foreach (var a in benchmarkActions) actions.WriteLine(FormattableString.Invariant($"{a.name},{a.qpc},{a.milliseconds:F4}"));
            }
            var pipeline = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            File.WriteAllLines(Path.Combine(folder, "result.txt"), new[] {
                $"passed={passed}", $"error={error}", $"unity={Application.unityVersion}", $"cpu={SystemInfo.processorType}",
                $"gpu={SystemInfo.graphicsDeviceName}", $"driver={SystemInfo.graphicsDeviceVersion}", $"ram_mib={SystemInfo.systemMemorySize}",
                $"screen={Screen.width}x{Screen.height}", $"hz={Screen.currentResolution.refreshRateRatio.value:F3}",
                $"quality={QualitySettings.names[QualitySettings.GetQualityLevel()]}", $"render_scale={pipeline.renderScale}",
                $"gi_mode={scene.giMode}", $"additive={scene.IsAdditiveMode}", $"loaded_zones={scene.LoadedZoneCount}",
                $"vsync={QualitySettings.vSyncCount}",
                $"lod_bias={QualitySettings.lodBias}", $"streaming={QualitySettings.streamingMipmapsActive}",
                $"streaming_textures={Texture.streamingTextureCount}", $"active_mips_bytes={Texture.currentTextureMemory}",
                $"lod_groups={FindObjectsByType<LODGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length}",
                $"qpc_hz={Stopwatch.Frequency}", $"draw_counter={draws.Valid}", $"system_memory_counter={native.Valid}",
                $"frames={samples.Count}", $"development={Debug.isDebugBuild}"
            });
            if (error != null) Debug.LogException(error);
            Application.Quit(passed ? 0 : 1);
        }

        private static long Counter(ProfilerRecorder recorder) => recorder.Valid ? recorder.LastValue : 0;

        private void ApplyBenchmarkAblation(string feature)
        {
            switch (feature)
            {
                case "none": return;
                case "ssao":
                case "decals":
                    string name = feature == "ssao" ? "ScreenSpaceAmbientOcclusion" : "DecalRendererFeature";
                    var matches = Resources.FindObjectsOfTypeAll<ScriptableRendererFeature>().Where(x => x.GetType().Name == name).ToArray();
                    Require(matches.Length > 0, "No renderer feature " + name);
                    foreach (var item in matches) item.SetActive(false);
                    return;
                case "shadows":
                    foreach (var light in FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                        light.shadows = LightShadows.None;
                    return;
                case "probes":
                    foreach (var probe in FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                        probe.gameObject.SetActive(false);
                    scene.localReflection = null;
                    scene.corridorReflection = null;
                    return;
                case "transparency":
                    foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    {
                        var materials = renderer.sharedMaterials;
                        if (materials.Length > 0 && materials.All(x => x != null && x.renderQueue >= 2501)) renderer.enabled = false;
                    }
                    return;
                case "geometry": QualitySettings.lodBias = 0.8f; return;
                default: throw new ArgumentException("Unknown benchmark ablation " + feature);
            }
        }

        private IEnumerable BenchmarkWalk(string stage, Vector3 destination)
        {
            benchmarkStage = stage;
            double start = Time.realtimeSinceStartupAsDouble;
            while (true)
            {
                Vector3 direction = destination - player.Feet;
                direction.y = 0;
                float distance = direction.magnitude;
                if (distance < 0.04f) yield break;
                if (Time.realtimeSinceStartupAsDouble - start > 25)
                    throw new InvalidOperationException($"{stage} blocked at {player.Feet} en route to {destination}");
                float dt = Mathf.Clamp(Time.unscaledDeltaTime, 0.001f, 0.045f);
                player.transform.rotation = Quaternion.LookRotation(direction);
                player.Move(direction.normalized * Mathf.Min(3.2f, distance / dt), dt);
                yield return null;
            }
        }

        private IEnumerator<BenchmarkStep> BenchmarkSteps()
        {
            yield return new("begin", () => casework.Begin(player.Feet, new VisitSettings()));
            yield return new("read-letter", () => casework.ReadLetter(player.Feet));
            yield return new("open-gate", () => casework.OpenGate(player.Feet));
            yield return new("gate-R", 4, -17.9f);
            yield return new("approach-R", 4, -3.3f);
            yield return new("swap-R-B", async () => { var r = await casework.AtHubAsync(player.Feet); player.Teleport(scene.Pad("AP02")); return r; });
            yield return new("board", () => casework.Board(player.Feet));
            yield return new("hall-B", 4, 4.75f);
            yield return new("room-entry-B", 6.52f, 4.88f);
            yield return new("enter-room", () => casework.EnterRoom(player.Feet));
            yield return new("submit-F101-B", () => casework.SubmitAsync(FormStatus.Unverified, false, player.Feet));
            yield return new("room-exit-B", 4, 4.75f);
            yield return new("hall-return-B", 4, -3.29f);
            yield return new("swap-B-R", async () => { var r = await casework.AtHubAsync(player.Feet); player.Teleport(scene.Pad("AP02")); return r; });
            yield return new("approach-return-R", 4, -20.30f);
            yield return new("seam", async () => { var r = await casework.OutsideAttemptAsync(player.Feet); player.Teleport(scene.Pad("AP01")); return r; });
            yield return new("reception-return-R", 4, -3.30f);
            yield return new("handoff-and-load", () => {
                var r = casework.AtHub(player.Feet, out _);
                Require(r == CommitResult.Applied && transaction.Restore(SaveSlot.Auto, out _) == SaveRead.Valid, "CP04 reload");
                player.Teleport(scene.Pad("AP02"));
                benchmarkTorch.enabled = true;
                return r;
            });
            yield return new("hall-R-torch", 4, 4.75f);
            yield return new("room-entry-R-torch", 6.52f, 4.91f);
            foreach (string id in new[] { "LIST", "SOCKETS", "WHEELS" }) yield return new("observe-" + id, () => casework.Observe(id, player.Feet));
            yield return new("room-cross-R-torch", 12.60f, 4.87f);
            yield return new("cabinet-approach", 12.61f, 3.62f);
            yield return new("resolve-E12", () => casework.SolvePz01(player.Feet));
            yield return new("store-R-torch", 15f, 3.64f);
            foreach (string id in EvidenceRegistry.DrawingFragments.Concat(new[] { EvidenceRegistry.Drawing, EvidenceRegistry.Wristband, EvidenceRegistry.Transfer }))
                yield return benchmarkTake == null ? new BenchmarkStep("take-" + id, () => casework.Take(id, player.Feet))
                    : new BenchmarkStep("take-" + id, () => benchmarkTake(id));
            yield return new("store-return", 12.61f, 3.62f);
            yield return new("cabinet-return", 12.60f, 4.87f);
            yield return new("room-return", 6.52f, 4.93f);
            yield return new("verify-C01", () => casework.VerifyAn(player.Feet));
            yield return new("gatekeeper-start", () => {
                gatekeeperCameo?.BeginCameo(State, Line);
                return CommitResult.Applied;
            });
            yield return new("hall-junction", 4, 4.75f);
            yield return new("emergency-R-torch", 4, 19.70f);
            yield return new("gatekeeper-resolve", () => casework.SaveEmergencyAsync(player.Feet, false, gatekeeperCameo.IsActive));
            yield return new("finish", () => casework.SaveEmergencyAsync(player.Feet, true, gatekeeperCameo.IsActive));
        }

        private IEnumerator<IDisposable> BenchmarkRoute()
        {
            using var steps = BenchmarkSteps();
            while (steps.MoveNext())
            {
                var step = steps.Current;
                if (step.Action != null || step.AsyncAction != null)
                {
                    benchmarkStage = step.Name;
                    long start = Stopwatch.GetTimestamp();
                    if (step.AsyncAction != null)
                    {
                        var pending = step.AsyncAction();
                        while (!pending.IsCompleted) yield return null;
                        Apply(pending.GetAwaiter().GetResult(), step.Name);
                    }
                    else Apply(step.Action(), step.Name);
                    benchmarkActions.Add((step.Name, PerformanceCounter(), (Stopwatch.GetTimestamp() - start) * 1000d / Stopwatch.Frequency));
                    // Leave enough real frames for frame timing and time-sliced probe work after commits.
                    double until = Time.realtimeSinceStartupAsDouble + 0.40;
                    while (Time.realtimeSinceStartupAsDouble < until) yield return null;
                }
                else
                {
                    foreach (var unused in BenchmarkWalk(step.Name, step.Destination)) yield return null;
                }
            }
        }

        private readonly struct BenchmarkStep
        {
            public readonly string Name;
            public readonly Vector3 Destination;
            public readonly Func<CommitResult> Action;
            public readonly Func<Task<CommitResult>> AsyncAction;
            public BenchmarkStep(string name, float x, float z) { Name = name; Destination = new Vector3(x, 0, z); Action = null; AsyncAction = null; }
            public BenchmarkStep(string name, Func<CommitResult> action) { Name = name; Destination = default; Action = action; AsyncAction = null; }
            public BenchmarkStep(string name, Func<Task<CommitResult>> action) { Name = name; Destination = default; Action = null; AsyncAction = action; }
        }

        private readonly struct BenchmarkFrame
        {
            private readonly string stage;
            private readonly long qpc, draws, passes, triangles, system, unity, navCollect, navBake, probe, save, flush;
            private readonly double wall, main, render, gpu;
            private readonly bool focused;
            public BenchmarkFrame(string stage, long qpc, double wall, double main, double render, double gpu,
                long draws, long passes, long triangles, long system, long unity, long navCollect, long navBake, long probe, long save, long flush, bool focused)
            {
                this.stage = stage; this.qpc = qpc; this.wall = wall; this.main = main; this.render = render; this.gpu = gpu;
                this.draws = draws; this.passes = passes; this.triangles = triangles; this.system = system; this.unity = unity;
                this.navCollect = navCollect; this.navBake = navBake; this.probe = probe; this.save = save; this.flush = flush;
                this.focused = focused;
            }
            public string ToCsv() => FormattableString.Invariant($"{stage},{qpc},{wall:F4},{main:F4},{render:F4},{gpu:F4},{draws},{passes},{triangles},{system},{unity},{navCollect},{navBake},{probe},{save},{flush},{(focused ? 1 : 0)}");
        }
    }
}
