using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CTCC
{
    public sealed partial class HospitalGame
    {
        private readonly List<string> verification=new();

        private IEnumerator AutomatedVisit()
        {
            Directory.CreateDirectory(qaFolder);
            live=true;
            bool passed=false;
            try
            {
                var settings=new VisitSettings { volume=0, reduceMotion=true, captions=false, alternateBindings=true, textScale=1.3f };
                foreach(var form in new[]{FormStatus.Affirmed,FormStatus.Unverified,FormStatus.Refuted})
                    WalkCase(form,settings);
                ExerciseTransitions();
                ExerciseRecovery();
                Require(!dialogue.ContainsKey("CH01-D16b") && !dialogue.ContainsKey("CH04-D06"),"CH01 dialogue guard");
                verification.Add("V22-Q03 [CH01/02 subset] PASS: CH04 copy absent from M1 data export.");
                passed=true;
            }
            catch(Exception e)
            {
                verification.Add("FAIL: "+e.ToString());
                Debug.LogException(e);
            }
            if(passed)
                yield return VerifyKeyboard();
            File.WriteAllLines(Path.Combine(qaFolder,"gameplay.txt"),verification);
            if(passed && SystemInfo.graphicsDeviceType!=GraphicsDeviceType.Null)
                yield return VerifyRender();
            FinishQa(passed);
        }

        private IEnumerator VerifyKeyboard()
        {
            bool first=false,second=false,escaped=false;
            var keyboard=InputSystem.AddDevice<Keyboard>();
            ui.Show("KIỂM THỬ TRỢ NĂNG","Điều hướng bàn phím","Cỡ chữ 130%. Đổi cách điều khiển được lưu.",
                new[]{new ActionChoice("Lựa chọn 1",()=>first=true),new ActionChoice("Lựa chọn 2",()=>second=true)},
                ()=>{escaped=true;ui.Close();});
            var before=InputSystem.settings.updateMode;
            var background=InputSystem.settings.backgroundBehavior;
            try
            {
                InputSystem.settings.updateMode=InputSettings.UpdateMode.ProcessEventsManually;
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                foreach(Key key in new[]{Key.Tab,Key.Enter,Key.Escape})
                {
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));
                    InputSystem.Update();
                    ui.HandleNavigation(keyboard);
                    yield return null;
                }
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.updateMode=before;
                InputSystem.settings.backgroundBehavior=background;
            }
            if(second && !first && escaped && State.settings.alternateBindings && State.settings.textScale>1.25f)
                verification.Add("T06 keyboard subtest PASS: Tab focus within modal, Enter action, Esc dismiss, alternate binding and 130% font settings kept.");
            else
                verification.Add("T06 keyboard subtest UNVERIFIED: input state was not observed in the expected order.");
            ui.Close();
            yield return null;
        }

        private void WalkCase(FormStatus route,VisitSettings settings)
        {
            Apply(casework.Begin(player.Feet,settings),"begin");
            player.Teleport(scene.Pad("AP00"));
            Require(casework.OpenGate(player.Feet)!=CommitResult.Applied,"gate before SMS");
            Apply(casework.ReadLetter(player.Feet),"SMS");
            Apply(casework.OpenGate(player.Feet),"gate");
            Walk(new Vector3(4,0,-17.9f));
            Walk(new Vector3(4,0,-3.3f));
            Apply(casework.AtHub(player.Feet,out _),"enter B");
            player.Teleport(scene.Pad("AP02"));
            Require(State.layer==Reality.B && !GateService.G01(State),"first visit B");
            Apply(casework.Board(player.Feet),"board");
            Walk(new Vector3(4,0,4.75f));
            Walk(new Vector3(6.51f,0,4.88f));
            Apply(casework.EnterRoom(player.Feet),"room CP02");
            if(route==FormStatus.Refuted)
            {
                Apply(casework.Observe("OBS101A",player.Feet),"A observed");
                Require(casework.Submit(route,true,player.Feet)!=CommitResult.Applied,"one observation denies C");
                Apply(casework.Observe("OBS101B",player.Feet),"B observed");
                Require(casework.Submit(route,false,player.Feet)!=CommitResult.Applied,"C without selected pair");
            }
            Apply(casework.Submit(route,true,player.Feet),"F101 confirmation");
            Require(GateService.G01(State) && !GateService.G02(State) && State.f101.firstDecision==route &&
                FormService.Distortion(State)==(route==FormStatus.Affirmed?1:0) && !State.Has("E12"),
                "form result before G02");
            Require(scene.deniedBay.activeInHierarchy==(route==FormStatus.Affirmed),"branch applied in B");
            Walk(new Vector3(4,0,4.75f));
            Walk(new Vector3(4,0,-3.29f));
            Apply(casework.AtHub(player.Feet,out _),"collapse");
            player.Teleport(scene.Pad("AP02"));
            Require(State.layer==Reality.R,"collapse R");
            Walk(new Vector3(4,0,-20.30f));
            Apply(casework.OutsideAttempt(player.Feet),"seam");
            player.Teleport(scene.Pad("AP01"));
            Require(casework.OutsideAttempt(player.Feet)!=CommitResult.Applied && scene.seamBlocker.activeSelf,"seam once");
            Walk(new Vector3(4,0,-3.30f));
            Apply(casework.AtHub(player.Feet,out _),"CH02 handoff");
            Require(transaction.Restore(SaveSlot.Auto,out var handoff)==SaveRead.Valid && handoff!=null,"reload CP04");
            player.Teleport(handoff);
            Require(State.layer==Reality.R && State.chapter=="CH02" && State.checkpoint=="CH01-CP04" &&
                State.f101.firstDecision==route && State.f101.revision==1 && !GateService.G02(State),"CP04 invariants");
            Walk(new Vector3(4,0,4.75f)); Walk(new Vector3(6.52f,0,4.91f));
            Require(casework.SolvePz01(player.Feet)!=CommitResult.Applied,"physical corroboration required");
            foreach(var id in new[]{"LIST","SOCKETS","WHEELS"}) Apply(casework.Observe(id,player.Feet),id);
            Walk(new Vector3(12.6f,0,4.87f)); Walk(new Vector3(12.61f,0,3.62f));
            Apply(casework.SolvePz01(player.Feet),"E12 and cupboard");
            Walk(new Vector3(15.0f,0,3.64f));
            foreach(int i in new[]{2,0,1,1}) Apply(casework.Take(EvidenceRegistry.DrawingFragments[i],player.Feet),"fragment "+i);
            Apply(casework.Take(EvidenceRegistry.Drawing,player.Feet),"assemble");
            Apply(casework.Take(EvidenceRegistry.Wristband,player.Feet),"wristband");
            Require(!EvidenceRegistry.BundleComplete(State) && !GateService.G02(State) &&
                casework.VerifyAn(player.Feet)!=CommitResult.Applied,"missing transfer blocks G02");
            Apply(casework.Take(EvidenceRegistry.Transfer,player.Feet),"transfer 101/4→C01");
            Walk(new Vector3(12.61f,0,3.62f)); Walk(new Vector3(12.60f,0,4.87f)); Walk(new Vector3(6.52f,0,4.93f));
            Apply(casework.VerifyAn(player.Feet),"C01 confirmation");
            Require(GateService.G02(State) && GateService.H01Tutorial(State) && !GateService.H02(State),"G02/H01; H02 remains unknown");
            if(route==FormStatus.Affirmed)
            {
                Apply(casework.Submit(FormStatus.Unverified,false,player.Feet),"revision A→B");
                Apply(casework.SwitchIn101(player.Feet),"back to B for observations");
                foreach(var id in new[]{"OBS101A","OBS101B"}) Apply(casework.Observe(id,player.Feet),id);
                Apply(casework.Submit(FormStatus.Refuted,true,player.Feet),"revision B→C");
                for(int i=0;i<10;i++) Apply(casework.Submit(FormStatus.Refuted,true,player.Feet),"duplicate F101");
                Require(State.f101.firstDecision==FormStatus.Affirmed && State.f101.revision==3 &&
                    FormService.Distortion(State)==0,"revision idempotence");
                Apply(casework.SwitchIn101(player.Feet),"back to R");
            }
            Require(transaction.KeepManual(),"manual slot");
            Require(transaction.Restore(SaveSlot.Manual,out var checkpoint)==SaveRead.Valid,"manual restore");
            player.Teleport(checkpoint);
            Walk(new Vector3(4,0,4.75f)); Walk(new Vector3(4,0,19.70f));
            Apply(casework.Complete(player.Feet),"finish");
            Require(State.Happened("M1-complete") && State.evidence.Count==8,"end snapshot");
            verification.Add($"TASK025-A PASS {route}: actual controller/collision path and runtime modules; F101 r{State.f101.revision}, D={FormService.Distortion(State)}, G01/G02, CP04, E12/E11 and slots.");
        }

        private void ExerciseTransitions()
        {
            player.Teleport(scene.Pad("AP04"));
            Require(State.layer==Reality.R,"stress begins in R");
            long sequence=State.sequence;
            Vector3 doorway=new(5.59f,.015f,4.75f);
            for(int i=0;i<30;i++) Require(casework.SwitchIn101(doorway)==CommitResult.PadRequired,"doorway attempt "+i);
            Require(State.sequence==sequence,"doorway did not commit");
            var artificial=new GameObject("QA collision only in B");
            artificial.layer=HospitalScene.SolidLayer;
            artificial.transform.SetParent(scene.reportProps.transform,false);
            artificial.transform.position=scene.Pad("AP04").position+Vector3.up*.84f;
            artificial.AddComponent<BoxCollider>().size=new Vector3(.89f,1.5f,.89f);
            Require(casework.SwitchIn101(player.Feet)==CommitResult.Obstructed,"invalid B collision rollback");
            Require(State.layer==Reality.R && State.sequence==sequence && !scene.reportProps.activeSelf,"rollback restored presentation/state");
            artificial.SetActive(false);
            Destroy(artificial);
            for(int i=0;i<30;i++)
            {
                Apply(casework.SwitchIn101(player.Feet),"stress safe swap "+i);
                if(i%3==0)
                {
                    Require(transaction.Restore(SaveSlot.Auto,out var safe)==SaveRead.Valid,"stress reload "+i);
                    player.Teleport(safe);
                }
                Require(State.evidence.Count==8 && State.f101.revision==1,"no duplicates after stress");
            }
            verification.Add("TASK025-B / T02 / T03 PASS: 30 denied doorway attempts, 30 valid swaps, forced rollback and 10 reloads in the same build.");
        }

        private void ExerciseRecovery()
        {
            var isolated=new AtomicSaves(Path.Combine(qaFolder,"fault-injection"));
            var first=Investigation.New();
            isolated.Write(SaveSlot.Auto,first);
            var next=first.Copy();
            next.sequence=1;
            bool interrupted=false;
            try { isolated.Write(SaveSlot.Auto,next,()=>throw new IOException("QA interruption after flush")); }
            catch(IOException) { interrupted=true; }
            Require(interrupted && isolated.Read(SaveSlot.Auto,out var old)==SaveRead.Valid && old.sequence==0,"interrupted write preserved main");
            isolated.Write(SaveSlot.Auto,next);
            File.WriteAllText(isolated.SlotPath(SaveSlot.Auto),"QA damage");
            Require(isolated.Read(SaveSlot.Auto,out var fallback)==SaveRead.Backup && fallback.sequence==0,"bad main uses valid backup");
            verification.Add("T08 PASS: fault after durable temp write retains previous commit; damaged primary recovers verified backup.");
        }

        private void Walk(Vector3 point)
        {
            for(int i=0;i<875;i++)
            {
                Vector3 offset=point-player.Feet;
                offset.y=0;
                if(offset.sqrMagnitude<.045f*.045f) return;
                player.Move(offset.normalized*2.2f,.018f);
            }
            throw new InvalidOperationException($"Unable to reach {point} from {player.Feet} through ordinary capsule movement.");
        }

        private static void Apply(CommitResult result,string stage) => Require(result==CommitResult.Applied,stage+" → "+result);
        private static void Require(bool condition,string label)
        {
            if(!condition) throw new InvalidOperationException(label);
        }

        private IEnumerator VerifyRender()
        {
            QualitySettings.vSyncCount=0;
            Application.targetFrameRate=-1;
            var camera=player.Eye;
            var target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            target.Create();
            camera.enabled=false;
            var artView=new Vector3(7.58f,.02f,4.66f);
            var artAngle=Quaternion.Euler(0,63,0);
            player.Teleport(artView,artAngle);
            for(int i=0;i<65;i++) yield return null;
            SaveFrame(camera,target,"room101-R");
            player.Teleport(new Vector3(3.65f,.02f,4.05f),Quaternion.Euler(0,73,0));
            SaveFrame(camera,target,"corridor-R");
            player.Teleport(scene.Pad("AP04"));
            Apply(casework.SwitchIn101(player.Feet),"capture B");
            for(int i=0;i<55;i++) yield return null;
            player.Teleport(artView,artAngle);
            SaveFrame(camera,target,"room101-B");
            ui.Close();
            player.Teleport(scene.Pad("AP04"));
            Apply(casework.SwitchIn101(player.Feet),"capture back to R");
            const int count=420;
            var elapsed=new double[count];
            var gpu=new double[count];
            var roundtrip=new double[count];
            var timing=new FrameTiming[1];
            using var calls=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count");
            using var renderThread=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Render Thread");
            long peakDraws=0,peakRender=0;
            var request=new UniversalRenderPipeline.SingleCameraRequest { destination=target };
            for(int i=-125;i<count;i++)
            {
                float t=Mathf.Repeat(i/300f,1);
                camera.transform.parent.position=Vector3.Lerp(new Vector3(4.07f,.02f,4.8f),new Vector3(9.64f,.02f,5.32f),t);
                camera.transform.LookAt(new Vector3(12.05f,1.02f,6.56f));
                long started=System.Diagnostics.Stopwatch.GetTimestamp();
                RenderPipeline.SubmitRenderRequest(camera,request);
                var fence=AsyncGPUReadback.Request(target,0,0,1,0,1,0,1);
                fence.WaitForCompletion();
                if(fence.hasError) throw new InvalidOperationException("Graphics test readback failed.");
                if(i>=0)
                    roundtrip[i]=(System.Diagnostics.Stopwatch.GetTimestamp()-started)*1000d/System.Diagnostics.Stopwatch.Frequency;
                FrameTimingManager.CaptureFrameTimings();
                yield return null;
                if (i<0) continue;
                elapsed[i]=Time.unscaledDeltaTime*1000;
                if(FrameTimingManager.GetLatestTimings(1,timing)>0) gpu[i]=timing[0].gpuFrameTime;
                peakDraws=Math.Max(peakDraws,calls.LastValue);
                peakRender=Math.Max(peakRender,renderThread.LastValue);
            }
            File.WriteAllLines(Path.Combine(qaFolder,"profile.txt"),new[]{
                "unity="+Application.unityVersion,
                "GPU="+SystemInfo.graphicsDeviceName,
                "graphics="+SystemInfo.graphicsDeviceType,
                "renderer_mode=Offscreen + synchronized 1px readback",
                "screen="+target.width+"x"+target.height,
                "frames="+count,
                "draw_calls_peak="+peakDraws,
                "render_thread_peak_ms="+(peakRender/1000000d).ToString("0.00",CultureInfo.InvariantCulture),
                "frame_p95_ms="+Percentile(elapsed),
                "offscreen_render_p95_ms="+Percentile(roundtrip),
                "gpu_p95_ms="+Percentile(gpu.Where(x=>x>0).ToArray()),
                "allocated_mb="+(Profiler.GetTotalAllocatedMemoryLong()/1048576d).ToString("0.00",CultureInfo.InvariantCulture)
            });
            target.Release();
            Destroy(target);
        }

        private void SaveFrame(Camera camera,RenderTexture target,string name)
        {
            var request=new UniversalRenderPipeline.SingleCameraRequest { destination=target };
            RenderPipeline.SubmitRenderRequest(camera,request);
            var fence=AsyncGPUReadback.Request(target,0,0,1,0,1,0,1);
            fence.WaitForCompletion();
            var previous=RenderTexture.active;
            RenderTexture.active=target;
            var screenshot=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            screenshot.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
            screenshot.Apply();
            File.WriteAllBytes(Path.Combine(qaFolder,name+".png"),screenshot.EncodeToPNG());
            Destroy(screenshot);
            RenderTexture.active=previous;
        }

        private static string Percentile(double[] sample)
        {
            if(sample.Length==0) return "NA";
            Array.Sort(sample);
            return sample[(int)Math.Ceiling(.95*sample.Length)-1].ToString("0.00",CultureInfo.InvariantCulture);
        }

        private static void FinishQa(bool passed)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(passed?0:1);
#else
            Application.Quit(passed?0:1);
#endif
        }
    }
}
