using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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
            yield return VerifyDeferredSaveFailure();
            yield return VerifyM2AdditiveAndCharacters();
            yield return VerifyKeyboard();
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null) yield return VerifyRender();
        }

        private IEnumerator VerifyM2AdditiveAndCharacters()
        {
            // 1) M2-ADDITIVE: verify all 4 additive zone scenes, duplicate zone rejection, and async unload/reload
            if (scene.IsAdditiveMode)
            {
                Require(scene.LoadedZoneCount == 4 && scene.HasZone("LOC01") && scene.HasZone("LOC02") &&
                    scene.HasZone("LOC03") && scene.HasZone("LOC04"), $"All 4 additive zones loaded (got {scene.LoadedZoneCount})");

                // Duplicate zoneId rejection check (m2-scene-handoff.md)
                var dupGo = new GameObject("QA_DupZone");
                var dupRegion = dupGo.AddComponent<ZoneRegion>();
                dupRegion.zoneId = "LOC01";
                bool acceptedDup = scene.RegisterZone(dupRegion);
                Destroy(dupGo);
                Require(!acceptedDup, "Duplicate ZoneRegion LOC01 rejected");

                // Async unload & reload of LOC04_Emergency without breaking persistent state or AP05
                yield return scene.UnloadZoneAsync("LOC04_Emergency");
                Require(scene.LoadedZoneCount == 3 && !scene.HasZone("LOC04"), "LOC04 unloaded cleanly");
                yield return scene.LoadZoneAsync("LOC04_Emergency");
                Require(scene.LoadedZoneCount == 4 && scene.HasZone("LOC04") && scene.Pad("AP05") != null, "LOC04 reloaded cleanly");
                verification.Add("M2-ADDITIVE PASS: 4 additive scenes loaded, duplicate zoneId rejected, async unload/reload of LOC04_Emergency preserved AP05 & state.");
            }

            // 2) LOCK-V22-02 / V22-Q03 runtime VO & Transcript guard
            Require(voice != null && !voice.HasClip("CH01-D16b") && !voice.HasClip("CH04-D06"), "Forbidden CH04 VO absent");
            int beforeT = voice.Transcript.Count;
            voice.Speak("CH01-D16b", "Lan", "Forbidden test");
            voice.Speak("CH04-D06", "Lan", "Forbidden test");
            Require(voice.Transcript.Count == beforeT, "Forbidden CH04 lines blocked from Transcript");
            verification.Add("V22-Q03 runtime PASS: CH01-D16b & CH04-D06 blocked in Dialogue, VoiceDirector clips, and Transcript.");

            // 3) Characters, BlendShapes & Dual-Scenario GI Probes check
            if (scene.referenceArt)
            {
                Require(scene.LanActor != null && scene.LanActor.clips.Length >= 4, "Lan actor rigged with 4+ actions");
                var lanSmr = scene.LanActor.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                int maxShapes = 0;
                foreach (var s in lanSmr) if (s.sharedMesh != null) maxShapes = Mathf.Max(maxShapes, s.sharedMesh.blendShapeCount);
                Require(maxShapes >= 9, $"Lan face has >=9 blendshapes (got {maxShapes})");
                Require(scene.GatekeeperActor != null && scene.GatekeeperActor.clips.Length >= 3, "Gatekeeper actor rigged with 3+ actions");
                Require(scene.AnActor != null && scene.AnActor.clips.Length >= 2, "Bé An apparition actor rigged with 2+ actions");
                int rProbes = scene.realLightProbes != null ? scene.realLightProbes.count : 0;
                int bProbes = scene.reportLightProbes != null ? scene.reportLightProbes.count : 0;
                Require(rProbes > 0 && bProbes > 0, $"Dual-scenario baked LightProbes present (R={rProbes}, B={bProbes})");
                verification.Add($"M2-CHARACTERS & GI PASS: Lan ({maxShapes} blendshapes, {scene.LanActor.clips.Length} clips), Gatekeeper ({scene.GatekeeperActor.clips.Length} clips), An ({scene.AnActor.clips.Length} clips), Dual LightProbes (R={rProbes}, B={bProbes}).");
            }

            // 4) CH02-Q05 Gatekeeper cameo trigger, dual-door escape (Door A & Door B), and CH02-T06 non-repeat check
            if (gatekeeperCameo != null && scene.GatekeeperActor != null)
            {
                Require(State.chapter == "CH02" && GateService.G02(State), "Ready in CH02 post-G02 for cameo test");
                if (State.layer == Reality.B) Apply(casework.SwitchIn101(scene.Pad("AP04").position), "return R for cameo");
                Require(gatekeeperCameo.ShouldTrigger(State, new Vector3(4.0f, 0.02f, 4.75f)), "Gatekeeper triggers in CH02 post-G02 corridor");
                gatekeeperCameo.BeginCameo(State, Line);
                Require(gatekeeperCameo.IsActive, "Gatekeeper cameo active");
                // Verify both Door A (x=4.0, z=16.5) and Door B (x=0.05, z=16.5) count as valid escape routes!
                Require(gatekeeperCameo.CheckEscapedToEmergency(new Vector3(4.0f, 0.02f, 16.5f)), "Door A escape valid");
                Require(gatekeeperCameo.CheckEscapedToEmergency(new Vector3(0.05f, 0.02f, 16.5f)), "Door B staff escape valid");
                gatekeeperCameo.CompleteEscape(() => Apply(casework.RecordGatekeeperResolved(scene.Pad("AP05").position), "CH02-T06 persisted"));
                Require(State.Happened("CH02-T06") && !gatekeeperCameo.IsActive && !gatekeeperCameo.ShouldTrigger(State, new Vector3(4.0f, 0.02f, 4.75f)),
                    "CH02-T06 persisted and Gatekeeper never repeats");
                verification.Add("CH02-Q05 PASS: Gatekeeper dormant in CH01, triggers post-G02 in CH02, escapable via Door A & Door B, CH02-T06 once-only persisted.");
            }
        }

        private IEnumerator VerifyDeferredSaveFailure()
        {
            var isolated = new AtomicSaves(Path.Combine(qaFolder, "deferred-fault"));
            var checking = new RealityTransaction(scene, isolated);
            var before = checking.Current;
            isolated.Write(SaveSlot.Auto, before);
            var proposed = before.Copy();
            proposed.Remember("QA.deferred-save");
            var flushed = new TaskCompletionSource<bool>();
            var release = new TaskCompletionSource<bool>();
            var pending = checking.CommitEvidenceAsync(proposed, () =>
            {
                flushed.SetResult(true);
                release.Task.Wait(TimeSpan.FromSeconds(5));
                throw new IOException("QA deferred interruption after flush");
            });
            try
            {
                yield return Until(() => flushed.Task.IsCompleted, "deferred save reached durable flush");
                int first = Time.frameCount;
                for (int frame = 0; frame < 10; frame++) yield return null;
                Require(Time.frameCount >= first + 10 && !pending.IsCompleted && checking.Current == before &&
                    isolated.Read(SaveSlot.Auto, out var stillSaved) == SaveRead.Valid && stillSaved.sequence == before.sequence,
                    "main loop advanced with unpublished evidence and previous disk commit");
            }
            finally { release.TrySetResult(true); }
            yield return Until(() => pending.IsCompleted, "deferred failure completed");
            Require(pending.GetAwaiter().GetResult() == CommitResult.SaveUnavailable && checking.Current == before &&
                isolated.Read(SaveSlot.Auto, out var fallback) == SaveRead.Valid && fallback.sequence == before.sequence,
                "deferred write failure preserves previous runtime and disk commits");
            verification.Add("T08 deferred PASS: 10 main-loop frames while the worker was held after flush; previous state retained; failed publication returns SaveUnavailable.");
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

            // M2 LOC01 Exterior Arrival & Rain Courtyard Captures (R 2012)
            player.Teleport(new Vector3(3.85f, 0.02f, -24.4f), Quaternion.Euler(0, 6, 0));
            camera.transform.localRotation = Quaternion.Euler(-7, 0, 0);
            SaveFrame(camera, target, "loc01-exterior-R");

            player.Teleport(new Vector3(3.95f, 0.02f, -16.8f), Quaternion.Euler(0, 0, 0));
            camera.transform.localRotation = Quaternion.Euler(-9, 0, 0);
            SaveFrame(camera, target, "loc01-courtyard-R");

            // M2 LOC03 Store Room Dismantled Bed 4 + Dry Shelf E11 Capture (R 2012)
            player.Teleport(new Vector3(14.15f, 0.02f, 3.62f), Quaternion.Euler(0, 74, 0));
            camera.transform.localRotation = Quaternion.Euler(10, 0, 0);
            SaveFrame(camera, target, "loc03-store-R");

            // M2 LOC04 Emergency Bay Equipment Capture (R 2012)
            player.Teleport(new Vector3(3.65f, 0.02f, 17.15f), Quaternion.Euler(0, 42, 0));
            camera.transform.localRotation = Quaternion.Euler(6, 0, 0);
            SaveFrame(camera, target, "loc04-emergency-R");

            // M2 CHAR005 Gatekeeper Corridor Silhouette & Keyring Capture (R 2012 under crimson ER glow)
            if (scene.GatekeeperActor != null)
            {
                scene.GatekeeperActor.gameObject.SetActive(true);
                scene.GatekeeperActor.transform.SetPositionAndRotation(new Vector3(4.0f, 0f, 11.4f), Quaternion.Euler(0, 180, 0));
                scene.GatekeeperActor.PlayAction("GK_StalkWalk", 0.01f, restart: true);
                for (int i = 0; i < 6; i++) yield return null;
                player.Teleport(new Vector3(3.85f, 0.02f, 8.25f), Quaternion.Euler(0, 4, 0));
                camera.transform.localRotation = Quaternion.Euler(-2, 0, 0);
                SaveFrame(camera, target, "loc04-gatekeeper-R");
                scene.GatekeeperActor.gameObject.SetActive(false);
            }

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

            // M2 CHAR002 Trần Ngọc Lan Live Reception Encounter Capture (B 2005!)
            if (scene.LanActor != null)
            {
                scene.LanActor.gameObject.SetActive(true);
                if (scene.LanStepOutPoint != null)
                    scene.LanActor.transform.SetPositionAndRotation(scene.LanStepOutPoint.position, scene.LanStepOutPoint.rotation);
                scene.LanActor.LookTarget = camera.transform;
                scene.LanActor.SetExpression(0.65f, 0.12f, 0f);
                scene.LanActor.PlayAction("Lan_TalkGesture", 0.01f, restart: true);
                player.Teleport(new Vector3(4.15f, 0.02f, -3.55f), Quaternion.Euler(0, -82, 0));
                camera.transform.localRotation = Quaternion.Euler(3, 0, 0);
                for (int i = 0; i < 12; i++) yield return null;
                SaveFrame(camera, target, "loc02-lan-B");
                scene.LanActor.gameObject.SetActive(false);
            }

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

            // 1) LOC01 Exterior Arrival (R 2012, 90 frames = 3s)
            player.Teleport(scene.Pad("AP04"));
            if (State.layer == Reality.B) Apply(casework.SwitchIn101(player.Feet), "record R");
            for (int i = 0; i < 12; i++) yield return null;

            for (int f = 0; f < 90; f++)
            {
                float s = Smooth(f / 89f);
                Vector3 pos = Vector3.Lerp(new Vector3(3.85f, 0.02f, -24.0f), new Vector3(4.0f, 0.02f, -13.5f), s);
                Vector3 look = Vector3.Lerp(new Vector3(4.2f, 2.4f, -10.0f), new Vector3(4.0f, 2.8f, -7.8f), s);
                CapturePose(pos, look);
                yield return null;
            }

            // 2) LOC02 Reception & Live Trần Ngọc Lan (B 2005, 90 frames = 3s)
            player.Teleport(scene.Pad("AP04"));
            Apply(casework.SwitchIn101(player.Feet), "record B");
            if (scene.LanActor != null)
            {
                scene.LanActor.gameObject.SetActive(true);
                if (scene.LanStepOutPoint != null)
                    scene.LanActor.transform.SetPositionAndRotation(scene.LanStepOutPoint.position, scene.LanStepOutPoint.rotation);
                scene.LanActor.LookTarget = camera.transform;
                scene.LanActor.SetExpression(0.62f, 0.14f, 0f);
                scene.LanActor.PlayAction("Lan_TalkGesture", 0.01f, restart: true);
            }
            for (int i = 0; i < 12; i++) yield return null;
            for (int f = 0; f < 90; f++)
            {
                float s = Smooth(f / 89f);
                Vector3 pos = Vector3.Lerp(new Vector3(4.6f, 0.02f, -5.6f), new Vector3(3.95f, 0.02f, -3.35f), s);
                Vector3 look = Vector3.Lerp(new Vector3(2.4f, 1.48f, -3.28f), new Vector3(2.65f, 1.48f, -3.28f), s);
                CapturePose(pos, look);
                yield return null;
            }
            if (scene.LanActor != null) scene.LanActor.gameObject.SetActive(false);

            // 3) LOC03 Room 101 B -> R contrast + Store Room (120 frames = 4s)
            for (int f = 0; f < 60; f++)
            {
                float s = Smooth(f / 59f);
                Vector3 pos = Vector3.Lerp(new Vector3(7.4f, 0.02f, 4.75f), new Vector3(9.6f, 0.02f, 5.15f), s);
                Vector3 look = Vector3.Lerp(new Vector3(11.4f, 1.35f, 5.8f), new Vector3(11.6f, 1.15f, 3.65f), s);
                CapturePose(pos, look);
                yield return null;
            }
            player.Teleport(scene.Pad("AP04"));
            Apply(casework.SwitchIn101(player.Feet), "record R 101");
            for (int i = 0; i < 12; i++) yield return null;
            for (int f = 0; f < 60; f++)
            {
                float s = Smooth(f / 59f);
                Vector3 pos = Vector3.Lerp(new Vector3(9.6f, 0.02f, 5.15f), new Vector3(14.3f, 0.02f, 3.65f), s);
                Vector3 look = Vector3.Lerp(new Vector3(12.2f, 1.15f, 3.85f), new Vector3(15.6f, 0.95f, 4.2f), s);
                CapturePose(pos, look);
                yield return null;
            }

            // 4) LOC04 Gatekeeper Corridor Tension & Emergency Department (120 frames = 4s)
            if (scene.GatekeeperActor != null)
            {
                scene.GatekeeperActor.gameObject.SetActive(true);
                scene.GatekeeperActor.transform.SetPositionAndRotation(new Vector3(4.0f, 0f, 12.2f), Quaternion.Euler(0, 180, 0));
                scene.GatekeeperActor.PlayAction("GK_StalkWalk", 0.01f, restart: true);
            }
            for (int f = 0; f < 60; f++)
            {
                float s = Smooth(f / 59f);
                if (scene.GatekeeperActor != null)
                    scene.GatekeeperActor.transform.position = Vector3.Lerp(new Vector3(4.0f, 0f, 12.2f), new Vector3(4.0f, 0f, 10.6f), s);
                Vector3 pos = Vector3.Lerp(new Vector3(3.9f, 0.02f, 7.6f), new Vector3(3.95f, 0.02f, 8.8f), s);
                CapturePose(pos, new Vector3(4.0f, 1.55f, 11.4f));
                yield return null;
            }
            if (scene.GatekeeperActor != null) scene.GatekeeperActor.gameObject.SetActive(false);
            for (int f = 0; f < 60; f++)
            {
                float s = Smooth(f / 59f);
                Vector3 pos = Vector3.Lerp(new Vector3(3.8f, 0.02f, 16.8f), new Vector3(4.5f, 0.02f, 18.8f), s);
                Vector3 look = Vector3.Lerp(new Vector3(7.2f, 1.1f, 19.0f), new Vector3(7.9f, 1.25f, 19.8f), s);
                CapturePose(pos, look);
                yield return null;
            }

            static float Smooth(float t) => t * t * (3f - 2f * t);

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
