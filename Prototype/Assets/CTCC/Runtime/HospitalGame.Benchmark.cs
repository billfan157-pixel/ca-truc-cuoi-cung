using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Unity.Profiling;
using Unity.Profiling.Memory;
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
        private long benchmarkAwakeQpc, benchmarkReadyQpc;
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
            string profilerTrace = Argument("-ctcc-profiler-trace");
            if (profilerTrace != null)
            {
                Profiler.logFile = profilerTrace;
                Profiler.enableBinaryLog = true;
                Profiler.enabled = true;
            }
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
            bool weatherProfile = Argument("-ctcc-art-profile") == "weather";
            using var route = weatherProfile ? BenchmarkCh01Weather() : BenchmarkRoute();
            try
            {
                if (Screen.width != 1920 || Screen.height != 1080 || player.Eye.targetTexture != null)
                    throw new InvalidOperationException($"Expected onscreen 1920x1080; got {Screen.width}x{Screen.height}.");
                benchmarkTorch = player.Eye.GetComponentInChildren<Light>(true);
                ApplyBenchmarkAblation(Argument("-ctcc-ablation") ?? "none");
            }
            catch (Exception ex) { error = ex; }

            while (error == null)
            {
                try
                {
                    if (!route.MoveNext())
                    {
                        passed = weatherProfile ? samples.Count >= 180 : State.Happened("M1-complete") && GateService.G02(State) &&
                            (Argument("-ctcc-expansion") != "true" || (GateService.G04(State) && GateService.G05(State)));
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

            if (profilerTrace != null)
            {
                Profiler.enabled = false;
                Profiler.enableBinaryLog = false;
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
                $"qpc_hz={Stopwatch.Frequency}", $"awake_qpc={benchmarkAwakeQpc}", $"ready_qpc={benchmarkReadyQpc}",
                $"draw_counter={draws.Valid}", $"system_memory_counter={native.Valid}",
                $"frames={samples.Count}", $"development={Debug.isDebugBuild}"
            });
            if (error != null) Debug.LogException(error);
            string memorySnapshot = Argument("-ctcc-memory-snapshot");
            if (memorySnapshot != null && passed)
            {
                bool? captured = null;
                MemoryProfiler.TakeSnapshot(memorySnapshot, (_, success) => captured = success,
                    CaptureFlags.ManagedObjects | CaptureFlags.NativeObjects | CaptureFlags.NativeAllocations);
                float deadline = Time.realtimeSinceStartup + 40f;
                while (!captured.HasValue && Time.realtimeSinceStartup < deadline) yield return null;
                File.WriteAllText(Path.Combine(folder, "memory-capture.txt"), $"success={captured == true}\n");
            }
            yield return PrepareForQuit();
            Application.Quit(passed ? 0 : 1);
        }

        private IEnumerator<IDisposable> BenchmarkCh01Weather()
        {
            var courtyard = new Vector3(4.94f, .015f, -14.24f);
            var ward = new Vector3(9.50f, .015f, 5.85f);
            var rain = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
            var surfaces = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            var exteriorRain = rain.Single(p => p.name == "Exterior rain").gameObject;
            var eave = rain.Single(p => p.name == "Canopy eave runoff").gameObject;
            var puddles = surfaces.Where(p => p.name == "Rain rings on shallow water" && p.transform.position.z < 0)
                .Select(p => p.gameObject).ToArray();
            var foliage = surfaces.Where(r => r.transform.position.z < 0 &&
                r.sharedMaterials.Any(m => m != null && m.shader.name == "CTCC/TropicalFoliage")).ToArray();
            Require(puddles.Length == 4 && foliage.Length > 0 && scene.motion != null,
                "CH01 weather benchmark requires the authored courtyard and ward effects");

            var studies = new (string name, Vector3 feet, Vector3 toward, Action<bool> show)[]
            {
                ("rain", courtyard, new Vector3(5.35f, .93f, -8.96f), exteriorRain.SetActive),
                ("runoff", courtyard, new Vector3(5.35f, .93f, -8.96f), eave.SetActive),
                ("ripples", courtyard, new Vector3(5.35f, .93f, -8.96f),
                    visible => { foreach (var puddle in puddles) puddle.SetActive(visible); }),
                ("foliage-draw", new Vector3(6.78f, .015f, -17.24f), new Vector3(8.75f, 1.5f, -14.1f),
                    visible => { foreach (var leaf in foliage) leaf.enabled = visible; }),
                ("curtain-motion", ward, new Vector3(10.87f, 1.30f, 4.35f),
                    visible => scene.motion.reducedMotion = !visible),
            };

            var camera = player.Eye.transform;
            foreach (var study in studies)
            {
                player.Teleport(study.feet, Quaternion.identity);
                camera.LookAt(study.toward);
                try
                {
                    // ABBA sampling keeps startup, temperature drift and cubemap jobs out of the measured windows.
                    foreach (bool visible in new[] { true, false, false, true })
                    {
                        study.show(visible);
                        benchmarkStage = "weather-warmup";
                        double until = Time.realtimeSinceStartupAsDouble + .9;
                        while (Time.realtimeSinceStartupAsDouble < until) yield return null;
                        benchmarkStage = "weather-" + study.name + (visible ? "-on" : "-off");
                        until = Time.realtimeSinceStartupAsDouble + 2.4;
                        while (Time.realtimeSinceStartupAsDouble < until) yield return null;
                    }
                }
                finally
                {
                    study.show(true);
                }
            }
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
                    scene?.SetShadowsAblated(true);
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
            yield return new("inspect-letter", () =>
            {
                inspector.Open(scene.InspectTemplate("E10a"), null, 1f, ActiveVolume());
                return CommitResult.Applied;
            }, 1.1f);
            yield return new("read-letter", () => { inspector.Close(); return casework.ReadLetterAsync(player.Feet); });
            yield return new("open-gate", () => casework.OpenGateAsync(player.Feet));
            yield return new("gate-R", 4, -17.9f);
            yield return new("approach-R", 4, -3.3f);
            yield return new("swap-R-B", async () => { var r = await casework.AtHubAsync(player.Feet); player.Teleport(scene.Pad("AP02")); return r; });
            yield return new("lan-reception-B", () =>
            {
                player.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                lanDirector.StartEncounter(LanEncounterLine);
                return CommitResult.Applied;
            }, 5.2f);
            yield return new("board", () => { lanDirector.StopEncounter(true); voice.StopCurrent(); return casework.BoardAsync(player.Feet); });
            yield return new("hall-B", 4, 4.75f);
            yield return new("open-door-101", () =>
            {
                if (scene.roomDoor != null && !scene.roomDoor.isOpen)
                {
                    scene.interactables?.ToggleDoor("DOOR-101", scene.atmosphere, instant: true);
                    scene.roomDoor.isOpen = true;
                }
                return CommitResult.Applied;
            });
            yield return new("room-entry-B", 6.52f, 4.88f);
            yield return new("enter-room", () => casework.EnterRoomAsync(player.Feet));
            yield return new("submit-F101-B", () => casework.SubmitF101Async(FormStatus.Unverified, null, player.Feet));
            yield return new("room-exit-B", 4, 4.75f);
            yield return new("hall-return-B", 4, -3.29f);
            yield return new("swap-B-R", async () => { var r = await casework.AtHubAsync(player.Feet); player.Teleport(scene.Pad("AP02")); return r; });
            yield return new("approach-return-R", 4, -19.20f);
            yield return new("seam", async () => { var r = await casework.OutsideAttemptAsync(player.Feet); player.Teleport(scene.Pad("AP01")); return r; });
            yield return new("reception-return-R", 4, -3.30f);
            yield return new("handoff-and-load", async () => {
                var r = await casework.AtHubAsync(player.Feet);
                Require(r == CommitResult.Applied && transaction.Restore(SaveSlot.Auto, out _) == SaveRead.Valid, "CP04 reload");
                player.Teleport(scene.Pad("AP02"));
                benchmarkTorch.enabled = true;
                return r;
            });
            yield return new("hall-R-torch", 4, 4.75f);
            yield return new("open-door-101-R", () =>
            {
                if (scene.roomDoor != null && !scene.roomDoor.isOpen)
                {
                    scene.interactables?.ToggleDoor("DOOR-101", scene.atmosphere, instant: true);
                    scene.roomDoor.isOpen = true;
                }
                return CommitResult.Applied;
            });
            yield return new("room-entry-R-torch", 6.52f, 4.91f);
            foreach (string id in new[] { "LIST", "SOCKETS", "WHEELS" }) yield return new("observe-" + id, () => casework.ObserveAsync(id));
            yield return new("room-cross-R-torch", 12.60f, 4.87f);
            yield return new("cabinet-approach", 12.61f, 3.62f);
            yield return new("resolve-E12", () => casework.SolvePz01Async(player.Feet));
            yield return new("store-R-torch", 15f, 3.64f);
            foreach (string id in EvidenceRegistry.DrawingFragments.Concat(new[] { EvidenceRegistry.Drawing, EvidenceRegistry.Wristband, EvidenceRegistry.Transfer }))
                yield return new BenchmarkStep("take-" + id, () => casework.TakeAsync(id));
            yield return new("store-return", 12.61f, 3.62f);
            yield return new("cabinet-return", 12.60f, 4.87f);
            yield return new("room-return", 6.52f, 4.93f);
            yield return new("verify-C01", () => casework.VerifyAnAsync(player.Feet));
            yield return new("an-vignette-R", () =>
            {
                player.transform.rotation = Quaternion.Euler(0f, 105f, 0f);
                anDirector.PlayPostVerifyVignette(Line("CH02-D03"), Options.reduceMotion);
                return CommitResult.Applied;
            }, 3.2f);
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
                    double until = Time.realtimeSinceStartupAsDouble + step.SettleSeconds;
                    while (Time.realtimeSinceStartupAsDouble < until) yield return null;
                }
                else
                {
                    foreach (var unused in BenchmarkWalk(step.Name, step.Destination)) yield return null;
                }
            }
            if (Argument("-ctcc-expansion") == "true")
                foreach (var unused in BenchmarkExpansion()) yield return null;
            if (Argument("-ctcc-character-stress") == "true")
                foreach (var unused in BenchmarkCharacterStress()) yield return null;
        }

        private IEnumerable<object> BenchmarkExpansion()
        {
            foreach (var frame in BenchmarkLoadRegion("LOC05", RegionStreamer.Upper)) yield return frame;
            foreach (var frame in BenchmarkCommit("ch03-handoff", casework.BeginCh03Async(player.Feet))) yield return frame;
            foreach (var point in new[]
            {
                new Vector3(4, 0, 33.68f), new(-2.65f, 0, 34.35f), new(-14.7f, 0, 34.35f),
                new(-14.7f, 0, 36.8f), new(-.60f, 0, 36.8f), new(-.60f, 0, 32.20f)
            })
                foreach (var unused in BenchmarkWalk("ch03-stairs-and-archive", point)) yield return null;
            Require(transaction.OnPad(player.Feet, "AN03"), "Benchmark reached AN03 on the third floor");
            foreach (var id in new[] { EvidenceRegistry.PatientRegister, EvidenceRegistry.C02CareNote,
                EvidenceRegistry.C02YarnBag, EvidenceRegistry.C12UnnamedBox })
                foreach (var frame in BenchmarkCommit("ch03-evidence-" + id, casework.TakeCh03EvidenceAsync(id, "LOC07")))
                    yield return frame;
            foreach (var frame in BenchmarkCommit("ch03-pz03", casework.AttemptCh03Pu01Async("C02", true, player.Feet)))
                yield return frame;
            foreach (var frame in BenchmarkCommit("ch03-enter-M", casework.SwitchMemoryVignetteAn03Async(player.Feet)))
                yield return frame;
            player.Teleport(scene.Pad("AN03"));
            foreach (var frame in BenchmarkCommit("ch03-return-R", casework.SwitchMemoryVignetteAn03Async(player.Feet)))
                yield return frame;
            foreach (var frame in BenchmarkCommit("ch03-t06-patrol", casework.RecordAsync("CH03-T06", player.Feet, "CH03-CP03")))
                yield return frame;
            benchmarkStage = "ch03-hunter-functional";
            orderlyAi.SyncState(State);
            var deadline = Time.realtimeSinceStartupAsDouble + 3;
            while (Time.realtimeSinceStartupAsDouble < deadline)
            {
                orderlyAi.Tick(State, Time.unscaledDeltaTime, false, false, false, Line);
                yield return null;
            }
            foreach (var point in new[]
            {
                new Vector3(-.60f, 0, 36.8f), new(-14.7f, 0, 36.8f), new(-14.7f, 0, 34.35f),
                new(-2.60f, 0, 34.35f), new(4f, 0, 35.0f), new(4f, 0, 45.7f)
            })
                foreach (var unused in BenchmarkWalk("ch03-return-to-duty", point)) yield return null;
            foreach (var frame in BenchmarkCommit("ch03-duty-checkpoint", casework.CompleteCh03Async(player.Feet))) yield return frame;
            foreach (var frame in BenchmarkCommit("ch04-begin", casework.BeginCh04Async())) yield return frame;
            foreach (var id in new[] { EvidenceRegistry.DutyOrders, EvidenceRegistry.DutyLog, EvidenceRegistry.MeetingTape })
                foreach (var frame in BenchmarkCommit("ch04-evidence-" + id, ChapterFour.Take(transaction, id, "LOC06")))
                    yield return frame;
            foreach (var frame in BenchmarkCommit("ch04-meeting", ChapterFour.PresentMeeting(transaction))) yield return frame;
            foreach (var point in new[] { new Vector3(4, 0, 33.5f), new(4, 0, 20.97f), new(7, 0, 21.18f) })
                foreach (var unused in BenchmarkWalk("ch04-to-technical-stair", point)) yield return null;
            foreach (var frame in BenchmarkLoadRegion("LOC09", RegionStreamer.Service)) yield return frame;
            foreach (var unused in BenchmarkWalk("ch04-technical-stair", new Vector3(7, 0, 32.55f))) yield return null;
            Require(Mathf.Abs(player.Feet.y + 3.4f) < .27f, "Benchmark reached B1");
            foreach (var frame in BenchmarkCommit("ch04-power", ChapterFour.SetPower(transaction, 10))) yield return frame;
            foreach (var point in new[] { new Vector3(7, 0, 39), new(1.5f, 0, 39) })
                foreach (var unused in BenchmarkWalk("ch04-morgue", point)) yield return null;
            foreach (var id in new[] { EvidenceRegistry.MorgueRegister, EvidenceRegistry.LanRecovery })
                foreach (var frame in BenchmarkCommit("ch04-evidence-" + id, ChapterFour.Take(transaction, id, "LOC10")))
                    yield return frame;
            foreach (var frame in BenchmarkCommit("ch04-H05-PT02", ChapterFour.MatchRecovery(transaction))) yield return frame;
            foreach (var frame in BenchmarkCommit("ch04-t06-lan", casework.RecordAsync("CH04-T06", player.Feet))) yield return frame;
            Require(transaction.Restore(SaveSlot.Auto, out var restored) == SaveRead.Valid && restored == scene.Pad("AP09"),
                "Benchmark restored B1 checkpoint");

            // CH05 Benchmark Stages (M3.3 Phase G: AN05 with live M NPCs, LOC09 PZ07 blueprints, B1 E05b gallery, LOC08 G05)
            foreach (var point in new[]
            {
                new Vector3(7f, 0, 39f), new(7f, 0, 32.4f), new(7f, 0, 21.17f),
                new(4f, 0, 20.97f), new(4f, 0, 33.0f), new(0.85f, 0, 33.0f)
            })
                foreach (var unused in BenchmarkWalk("ch05-approach-an05", point)) yield return null;

            foreach (var frame in BenchmarkCommit("ch05-cp01-entry", ChapterFive.CommitEntryCheckpointAsync(transaction, player.Feet))) yield return frame;
            foreach (var frame in BenchmarkCommit("ch05-enter-M", ChapterFive.EnterEvacuationMemoryAsync(transaction, player.Feet))) yield return frame;
            player.Teleport(scene.Pad("AP10"));
            player.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            benchmarkStage = "ch05-an05-memory-npcs";
            var an05Until = Time.realtimeSinceStartupAsDouble + 3.5;
            while (Time.realtimeSinceStartupAsDouble < an05Until) yield return null;
            foreach (var frame in BenchmarkCommit("ch05-evac-pair1", ChapterFive.CompleteEvacPairAsync(transaction, 1))) yield return frame;
            foreach (var point in new[] { new Vector3(0.85f, 0, 36.9f), new(-12.2f, 0, 37.0f), new(-12.2f, 0, 22.6f) })
                foreach (var unused in BenchmarkWalk("ch05-an05-memory-npcs", point)) yield return null;
            foreach (var frame in BenchmarkCommit("ch05-evac-pair2", ChapterFive.CompleteEvacPairAsync(transaction, 2))) yield return frame;
            foreach (var point in new[] { new Vector3(-12.2f, 0, -4.0f), new(-2.0f, 0, -4.0f) })
                foreach (var unused in BenchmarkWalk("ch05-an05-memory-npcs", point)) yield return null;
            var s1Out = scene.s1DischargeDoor.pivot.parent.position + Vector3.back * 1.15f;
            foreach (var unused in BenchmarkWalk("ch05-an05-memory-npcs", s1Out)) yield return null;
            foreach (var frame in BenchmarkCommit("ch05-evac-pair3", ChapterFive.CompleteEvacPairAsync(transaction, 3))) yield return frame;
            foreach (var frame in BenchmarkCommit("ch05-return-R", ChapterFive.FinishEvacuationReturnAsync(transaction, player.Feet))) yield return frame;
            player.Teleport(scene.Pad("AP10"));

            foreach (var point in new[]
            {
                new Vector3(4f, 0, 33.0f), new(4f, 0, 20.97f), new(7f, 0, 21.17f)
            })
                foreach (var unused in BenchmarkWalk("ch05-loc09-pz07", point)) yield return null;
            if (!scene.RegionReady("LOC09", RegionStreamer.Service))
                foreach (var frame in BenchmarkLoadRegion("LOC09", RegionStreamer.Service)) yield return frame;
            foreach (var point in new[] { new Vector3(7f, 0, 32.55f), new(7.4f, 0, 43.2f) })
                foreach (var unused in BenchmarkWalk("ch05-loc09-pz07", point)) yield return null;
            foreach (var frame in BenchmarkCommit("ch05-acquire-E05", ChapterFive.AcquireBlueprintsAtLoc09Async(transaction))) yield return frame;
            foreach (var frame in BenchmarkCommit("ch05-pz07-align", ChapterFive.AttemptPz07OverlayAsync(transaction, true, true))) yield return frame;
            benchmarkStage = "ch05-loc09-pz07";
            player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            var pz07Until = Time.realtimeSinceStartupAsDouble + 2.5;
            while (Time.realtimeSinceStartupAsDouble < pz07Until) yield return null;

            foreach (var point in new[]
            {
                new Vector3(9.4f, 0, 42.0f), new(12.5f, 0, 42.0f), new(12.5f, 0, 50.4f),
                new(35.8f, 0, 50.4f), new(35.8f, 0, 42.0f)
            })
                foreach (var unused in BenchmarkWalk("ch05-b1-e05b-gallery", point)) yield return null;
            foreach (var frame in BenchmarkCommit("ch05-observe-E05b", ChapterFive.ObserveSealedWallE05bAsync(transaction))) yield return frame;
            foreach (var frame in BenchmarkCommit("ch05-verify-H03-G05", ChapterFive.VerifyH03AndOpenG05Async(transaction))) yield return frame;
            benchmarkStage = "ch05-b1-e05b-gallery";
            player.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            var e05bUntil = Time.realtimeSinceStartupAsDouble + 2.0;
            while (Time.realtimeSinceStartupAsDouble < e05bUntil) yield return null;

            foreach (var point in new[]
            {
                new Vector3(35.8f, 0, 50.4f), new(12.5f, 0, 50.4f), new(12.5f, 0, 42.0f),
                new(7f, 0, 42.0f), new(7f, 0, 32.4f), new(7f, 0, 21.17f),
                new(4f, 0, 20.97f), new(4f, 0, 34.35f), new(-2.65f, 0, 34.35f),
                new(-14.7f, 0, 34.35f), new(-14.7f, 0, 36.8f), new(2.4f, 0, 36.8f),
                new(2.4f, 0, 32.3f), new(5.2f, 0, 32.3f)
            })
                foreach (var unused in BenchmarkWalk("ch05-loc08-g05-handoff", point)) yield return null;
            foreach (var frame in BenchmarkCommit("ch05-cp03-handoff", ChapterFive.CommitCh05HandoffAsync(transaction, player.Feet))) yield return frame;
            Require(GateService.G05(State) && State.checkpoint == ChapterFive.CheckpointHandoff &&
                !State.Has("E08") && !State.Happened("G06.opened"),
                "Benchmark completed CH05 cleanly through G05 and CP03 without violating CH06 boundary");
        }

        private IEnumerable<object> BenchmarkCommit(string name, Task<CommitResult> commit)
        {
            benchmarkStage = name;
            long began = Stopwatch.GetTimestamp();
            while (!commit.IsCompleted) yield return null;
            Apply(commit.GetAwaiter().GetResult(), name);
            benchmarkActions.Add((name, PerformanceCounter(), (Stopwatch.GetTimestamp() - began) * 1000d / Stopwatch.Frequency));
            yield return null;
        }

        private IEnumerable<object> BenchmarkLoadRegion(string zone, string sceneName)
        {
            benchmarkStage = "load-" + zone;
            long began = Stopwatch.GetTimestamp();
            StartCoroutine(scene.LoadZoneAsync(sceneName));
            double deadline = Time.realtimeSinceStartupAsDouble + 8d;
            while (!scene.RegionReady(zone, sceneName) && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Require(scene.RegionReady(zone, sceneName), "Region did not load for benchmark: " + zone);
            benchmarkActions.Add((benchmarkStage, PerformanceCounter(), (Stopwatch.GetTimestamp() - began) * 1000d / Stopwatch.Frequency));
        }

        private IEnumerable<object> BenchmarkCharacterStress()
        {
            Vector3 cameraFeet = new(4f, 0.02f, -16f);
            player.Teleport(cameraFeet, Quaternion.identity);
            benchmarkTorch.enabled = false;
            var templates = new[] { scene.LanActor, scene.AnActor, scene.GatekeeperActor };
            var stageActions = new[] { "Lan_TalkGesture", "An_IdleLook", "GK_IdleStill" };
            var lines = new[] { "CH01-D05", "CH02-D03", "CH02-D05" };
            var actors = new List<CharacterActor>(9);
            var faces = new List<SkinnedMeshRenderer>(9);
            try
            {
                foreach (int total in new[] { 1, 3, 6, 9 })
                {
                    for (int i = actors.Count; i < total; i++)
                    {
                        Vector3 position = cameraFeet + new Vector3((i % 3 - 1) * 1.45f, 0f, 4.3f + (i / 3) * 1.7f);
                        var actor = Instantiate(templates[i % 3], position, Quaternion.Euler(0f, 180f, 0f));
                        actors.Add(actor);
                        actor.gameObject.SetActive(true);
                        actor.LookTarget = player.Eye.transform;
                        if (actor.isApparition) actor.SetApparitionAlpha(0.8f);
                        actor.PlayAction(stageActions[i % 3], 0.25f, restart: true);
                        var group = actor.GetComponentInChildren<LODGroup>();
                        group.ForceLOD(0);
                        faces.Add(group.GetLODs()[0].renderers.OfType<SkinnedMeshRenderer>()
                            .Single(renderer => renderer.name.Contains("_Body_")));
                    }
                    benchmarkStage = $"stress-visible-{total}";
                    double start = Time.realtimeSinceStartupAsDouble;
                    for (double elapsed = 0; elapsed < 6.0; elapsed = Time.realtimeSinceStartupAsDouble - start)
                    {
                        for (int i = 0; i < actors.Count; i++)
                            actors[i].ForceSpeechTime(lines[i % 3], (float)((elapsed + i * 0.07) % 1.3));
                        yield return null;
                    }
                    Require(faces.All(face => face.isVisible), $"Stress stage {total}: one or more bodies were culled");
                }
                benchmarkStage = "stress-offscreen-9";
                player.Teleport(cameraFeet, Quaternion.Euler(0f, 180f, 0f));
                double until = Time.realtimeSinceStartupAsDouble + 6.0;
                while (Time.realtimeSinceStartupAsDouble < until) yield return null;
                // Renderer.isVisible can still include shadows, so only the camera direction is controlled here.
            }
            finally
            {
                foreach (var actor in actors)
                    if (actor != null) Destroy(actor.gameObject);
            }
        }

        private readonly struct BenchmarkStep
        {
            public readonly string Name;
            public readonly Vector3 Destination;
            public readonly Func<CommitResult> Action;
            public readonly Func<Task<CommitResult>> AsyncAction;
            public readonly float SettleSeconds;
            public BenchmarkStep(string name, float x, float z) { Name = name; Destination = new Vector3(x, 0, z); Action = null; AsyncAction = null; SettleSeconds = 0f; }
            public BenchmarkStep(string name, Func<CommitResult> action, float settleSeconds = 0.4f) { Name = name; Destination = default; Action = action; AsyncAction = null; SettleSeconds = settleSeconds; }
            public BenchmarkStep(string name, Func<Task<CommitResult>> action) { Name = name; Destination = default; Action = null; AsyncAction = action; SettleSeconds = 0.4f; }
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
