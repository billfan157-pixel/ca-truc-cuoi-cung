using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
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
                Require(Argument("-ctcc-qa-probe") != "module-failure", "intentional QA module failure");
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
            if (passed) yield return FinishVerification(VerifyModuleStages(), qaFolder);
            else CompleteVerification(qaFolder, false);
        }

        private IEnumerator VerifyModuleStages()
        {
            yield return VerifyKeyboard();
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null) yield return VerifyRender();
        }

        private IEnumerator FinishVerification(IEnumerator checks, string directory)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(checks);
            bool passed = true;
            while (stack.Count > 0)
            {
                object yielded = null;
                try
                {
                    var stage = stack.Peek();
                    if (stage.MoveNext()) yielded = stage.Current;
                    else
                    {
                        (stack.Pop() as IDisposable)?.Dispose();
                        continue;
                    }
                }
                catch (Exception e)
                {
                    verification.Add("FAIL: " + e);
                    Debug.LogException(e);
                    passed = false;
                    break;
                }
                if (yielded is IEnumerator child) stack.Push(child);
                else yield return yielded;
            }
            while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
            CompleteVerification(directory, passed);
        }

        private void CompleteVerification(string directory, bool passed)
        {
            try
            {
                verification.Add(passed ? "RESULT=PASS" : "RESULT=FAIL");
                File.WriteAllLines(Path.Combine(directory, "gameplay.txt"), verification);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                passed = false;
            }
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
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                    InputSystem.Update();
                    yield return null;
                }
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.updateMode=before;
                InputSystem.settings.backgroundBehavior=background;
            }
            Require(Argument("-ctcc-qa-probe") != "keyboard-failure" && second && !first && escaped &&
                State.settings.alternateBindings && State.settings.textScale > 1.25f,
                $"T06 keyboard: first={first} second={second} escaped={escaped} alt={State.settings.alternateBindings} scale={State.settings.textScale:0.00}");
            verification.Add("T06 keyboard subtest PASS: Tab focus within modal, Enter action, Esc dismiss, alternate binding and 130% font settings kept.");
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
            var artView = new Vector3(7.58f, 0.02f, 4.66f);
            var artAngle = Quaternion.Euler(0, 63, 0);
            player.Teleport(artView, artAngle);
            for (int i = 0; i < 65; i++) yield return null;
            SaveFrame(camera, target, "room101-R");
            player.Teleport(new Vector3(3.65f, 0.02f, 4.05f), Quaternion.Euler(0, 73, 0));
            SaveFrame(camera, target, "corridor-R");

            // M0-R Close-up Hero Craftsmanship Showcase Captures in R (2012)
            player.Teleport(new Vector3(9.05f, 0.02f, 5.20f), Quaternion.Euler(0, 28, 0));
            camera.transform.localRotation = Quaternion.Euler(14, 0, 0);
            SaveFrame(camera, target, "closeup-bed-iv");
            player.Teleport(new Vector3(8.95f, 0.02f, 5.85f), Quaternion.Euler(0, 6, 0));
            camera.transform.localRotation = Quaternion.Euler(-4, 0, 0);
            SaveFrame(camera, target, "closeup-window-rain");
            player.Teleport(new Vector3(11.55f, 0.02f, 3.85f), Quaternion.Euler(0, 96, 0));
            camera.transform.localRotation = Quaternion.Euler(10, 0, 0);
            SaveFrame(camera, target, "closeup-cabinet-wheels");
            player.Teleport(new Vector3(3.85f, 0.02f, 1.85f), Quaternion.Euler(0, 16, 0));
            camera.transform.localRotation = Quaternion.Euler(2, 0, 0);
            SaveFrame(camera, target, "corridor-long-R");
            player.Teleport(new Vector3(4.35f, 0.02f, -4.65f), Quaternion.Euler(0, -68, 0));
            camera.transform.localRotation = Quaternion.Euler(8, 0, 0);
            SaveFrame(camera, target, "reception-R");

            // 3D Physical Inspector close-up capture (Bảy's kraft envelope + brass/steel keys in 3D!)
            player.Teleport(artView, artAngle);
            camera.transform.localRotation = Quaternion.identity;
            var envTmpl = scene.InspectTemplate("E10a");
            if (envTmpl != null && inspector != null)
            {
                inspector.Open(envTmpl, null, 1f, ActiveVolume());
                for (int i = 0; i < 6; i++) yield return null;
                SaveFrame(camera, target, "closeup-inspect-3d");
                inspector.Close();
            }

            player.Teleport(scene.Pad("AP04"));
            Apply(casework.SwitchIn101(player.Feet), "capture B");
            for (int i = 0; i < 55; i++) yield return null;
            player.Teleport(artView, artAngle);
            camera.transform.localRotation = Quaternion.identity;
            SaveFrame(camera, target, "room101-B");
            player.Teleport(new Vector3(3.65f, 0.02f, 4.05f), Quaternion.Euler(0, 73, 0));
            camera.transform.localRotation = Quaternion.identity;
            SaveFrame(camera, target, "corridor-B");

            // Optional 30fps Gameplay Walkthrough Video Sequence (-ctcc-record <framesDir>)
            string recordDir = Argument("-ctcc-record");
            if (!string.IsNullOrEmpty(recordDir))
            {
                yield return RecordWalkthrough(camera, Path.GetFullPath(recordDir));
            }

            Require(Argument("-ctcc-qa-probe") != "visual-failure", "intentional QA visual failure");
            target.Release();
            Destroy(target);
        }

        private IEnumerator RecordWalkthrough(Camera camera, string dir)
        {
            Directory.CreateDirectory(dir);
            var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.Create();
            var shot = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            var req = new UniversalRenderPipeline.SingleCameraRequest { destination = rt };
            int frameIdx = 0;

            void CapturePose(Vector3 pos, Vector3 lookAt)
            {
                camera.transform.parent.position = pos;
                camera.transform.localPosition = Vector3.up * 1.62f;
                camera.transform.LookAt(lookAt);
                RenderPipeline.SubmitRenderRequest(camera, req);
                var fence = AsyncGPUReadback.Request(rt, 0, 0, 1, 0, 1, 0, 1);
                fence.WaitForCompletion();
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                shot.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                shot.Apply();
                File.WriteAllBytes(Path.Combine(dir, $"frame_{frameIdx:D4}.jpg"), shot.EncodeToJPG(88));
                RenderTexture.active = prev;
                frameIdx++;
            }

            // Ensure R layer for first 210 frames (7 sec at 30fps): Corridor -> Room 101 Door -> Bed/Window -> Bed 4 & Cabinet
            player.Teleport(scene.Pad("AP04"));
            if (State.layer == Reality.B) Apply(casework.SwitchIn101(player.Feet), "record R");
            for (int i = 0; i < 12; i++) yield return null;

            for (int f = 0; f < 90; f++)
            {
                float t = f / 89f;
                float s = t * t * (3f - 2f * t);
                Vector3 pos = Vector3.Lerp(new Vector3(3.85f, 0.02f, 1.90f), new Vector3(6.85f, 0.02f, 4.75f), s);
                Vector3 look = Vector3.Lerp(new Vector3(5.65f, 1.55f, 4.95f), new Vector3(11.4f, 1.25f, 6.20f), s);
                CapturePose(pos, look);
                yield return null;
            }
            for (int f = 0; f < 110; f++)
            {
                float t = f / 109f;
                float s = t * t * (3f - 2f * t);
                Vector3 pos = Vector3.Lerp(new Vector3(6.85f, 0.02f, 4.75f), new Vector3(9.60f, 0.02f, 5.30f), s);
                Vector3 look = Vector3.Lerp(new Vector3(11.4f, 1.25f, 6.20f), new Vector3(12.8f, 1.05f, 3.60f), s);
                CapturePose(pos, look);
                yield return null;
            }

            // Switch to B (2005 operational reality) for next 120 frames (4 sec at 30fps)
            player.Teleport(scene.Pad("AP04"));
            Apply(casework.SwitchIn101(player.Feet), "record B");
            for (int i = 0; i < 18; i++) yield return null;
            for (int f = 0; f < 120; f++)
            {
                float t = f / 119f;
                float s = t * t * (3f - 2f * t);
                Vector3 pos = Vector3.Lerp(new Vector3(7.58f, 0.02f, 4.66f), new Vector3(9.85f, 0.02f, 5.15f), s);
                Vector3 look = Vector3.Lerp(new Vector3(11.8f, 1.30f, 6.30f), new Vector3(11.5f, 1.15f, 3.65f), s);
                CapturePose(pos, look);
                yield return null;
            }

            Destroy(shot);
            rt.Release();
            Destroy(rt);
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
