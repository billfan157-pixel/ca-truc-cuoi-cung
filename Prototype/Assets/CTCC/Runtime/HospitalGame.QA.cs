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
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null) yield return VerifySpatialAudio();
            yield return VerifyKeyboard();
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null) yield return VerifyRender();
        }

        private IEnumerator VerifySpatialAudio()
        {
            var audio = scene.atmosphere;
            Require(audio != null && audio.slippers != null && audio.trolleyRoll != null, "Directional audio assets wired");
            yield return Until(() => audio.Listener != null, "audio listener ready");

            Vector3 origin = audio.Listener.position;
            Vector3 side = audio.Listener.right;
            Vector3 near = origin + side;
            Vector3 far = origin - 7f * side;
            Require(audio.DirectionTo(near) == "bên phải" && audio.DirectionTo(far) == "bên trái", "Relative captions use the camera's facing");
            var wall = new GameObject("QA audio wall", typeof(BoxCollider));
            wall.layer = HospitalScene.SolidLayer;
            wall.transform.position = Vector3.Lerp(origin, near, 0.5f);
            wall.GetComponent<BoxCollider>().size = Vector3.one * 0.16f;
            Physics.SyncTransforms();

            try
            {
                audio.Apply(new VisitSettings { volume = 0f, sfxVolume = 1f, monoAudio = true });
                audio.From("slippers", near);
                audio.From("trolleyRoll", far);
                AudioSource slipper = null;
                AudioSource trolley = null;
                foreach (var source in audio.GetComponentsInChildren<AudioSource>())
                {
                    if (source.clip == audio.slippers && source.transform.position == near) slipper = source;
                    if (source.clip == audio.trolleyRoll && source.transform.position == far) trolley = source;
                }
                Require(slipper != null && trolley != null && slipper != trolley &&
                    slipper.transform.position == near && trolley.transform.position == far,
                    "Overlapping cue voices keep independent world positions");
                Require(slipper.GetComponent<AudioLowPassFilter>().cutoffFrequency < 3000f,
                    "A solid collider between the listener and the cue muffles the cue");
                Require(slipper.spatialBlend == 0f && trolley.spatialBlend == 0f &&
                    EnvironmentalAudio.DistanceGain(slipper, audio.Listener) > EnvironmentalAudio.DistanceGain(trolley, audio.Listener),
                    "Mono centers both cues and keeps near/far falloff");
                audio.Apply(new VisitSettings { volume = 0f, sfxVolume = 1f, monoAudio = false });
                Require(slipper.spatialBlend == 1f && trolley.spatialBlend == 1f && AudioListener.volume == 0f,
                    "Active world cues switch to 3D while master mute is in effect");
                slipper.Stop();
                trolley.Stop();
                verification.Add("AU01 spatial PASS: relative captions, overlapping anchors, collider low-pass, mono falloff, hot stereo switch and master mute.");
            }
            finally
            {
                wall.SetActive(false);
                Destroy(wall);
                audio.Apply(Options);
            }
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
                Require(!acceptedDup, "Duplicate ZoneRegion LOC01 rejected");
                dupRegion.zoneId = "QA-DUPLICATE-PADS";
                dupRegion.pads = new[] { new MarkerPose { id = "QA-PAD", point = dupGo.transform },
                    new MarkerPose { id = "qa-pad", point = dupGo.transform } };
                Require(!scene.RegisterZone(dupRegion) && !scene.TryPad("QA-PAD", out _), "One zone cannot claim the same pad twice");
                Destroy(dupGo);

                // Async unload & reload of LOC04_Emergency without breaking persistent state or AP05
                Apply(casework.Record("QA.loc04-restore", scene.Pad("AP05").position, "CH02-CP04"),
                    "seed committed ER checkpoint before scene removal");
                var originalDoor = scene.emergencyDoorA;
                long sequence = State.sequence;
                yield return scene.UnloadZoneAsync("LOC04_Emergency");
                Require(scene.LoadedZoneCount == 3 && !scene.HasZone("LOC04") && scene.emergencyDoorA == null &&
                    !scene.TryPad("AP05", out _) && scene.GatekeeperWaypoints.Length == 0, "LOC04 released pad, actor and door bindings");
                Require(transaction.Restore(SaveSlot.Auto, out var missing) == SaveRead.Damaged && missing == null &&
                    State.sequence == sequence, "An unloaded checkpoint preserves committed investigation");
                yield return scene.LoadZoneAsync("LOC04_Emergency");
                Require(scene.LoadedZoneCount == 4 && scene.HasZone("LOC04") && scene.Pad("AP05") != null, "LOC04 reloaded cleanly");
                Require(scene.emergencyDoorA != null && !ReferenceEquals(scene.emergencyDoorA, originalDoor) &&
                    transaction.Restore(SaveSlot.Auto, out var restored) == SaveRead.Valid && restored == scene.Pad("AP05") &&
                    State.sequence == sequence, "Reload rebinds door and restores the same revision at AP05");
                verification.Add("M2-ADDITIVE PASS: duplicate zone/pad rejected; LOC04 released bindings, rejected unloaded restore, then rebound and restored AP05.");
            }

            // 2) LOCK-V22-02 / V22-Q03 runtime VO & transcript guard
            Require(voice != null && !voice.HasClip("CH01-D16b") && !voice.HasClip("CH04-D06"), "Forbidden CH04 VO absent");
            var expectedReactions = new[] { "Vậy là xong…?", "Chưa đủ căn cứ.", "Phiếu này không khớp." };
            for (int i = 0; i < expectedReactions.Length; i++)
            {
                string id = $"CH01-D{10 + i}";
                Require(voice.HasClip(id) && Line(id) == expectedReactions[i], "F101 voiced reaction and text match: " + id);
            }
            Require(voice.HasClip("CH01-D14") && Line("CH01-D14") == Line("CH01-D13", 2), "Outer gate D14 uses its own matching clip and transcript ID");
            int beforeT = voice.Transcript.Count;
            int blocked = 0;
            foreach (string id in new[] { "CH01-D16b", "CH04-D06" })
            {
                voice.Speak(id, "Lan", "Forbidden test", onComplete: heard => { if (!heard) blocked++; }, beat: "B06");
                var future = Investigation.New();
                var condition = revealConditions[id];
                Require(!condition.Allows(future, "B06"), "CH01 must not expose " + id);
                future.chapter = "CH04";
                future.Remember("H05.verified");
                Require(!condition.Allows(future, "B06"), "H05 alone must not expose " + id);
                future.Remember("PT02.presented");
                Require(!condition.Allows(future, "B05") && condition.Allows(future, "B06"), "Exact beat and prerequisites for " + id);
            }
            Require(blocked == 2 && voice.Transcript.Count == beforeT, "Blocked lines settle callbacks without exposing transcript");
            verification.Add("V22-Q03 runtime PASS: text/VO/transcript blocked in slice; CH04 B06 condition admits both guarded IDs only with both prerequisites.");

            int interrupted = 0;
            voice.Speak("CH02-D03", "Bé An", Line("CH02-D03"), onComplete: heard => { if (!heard) interrupted++; });
            voice.Speak("CH02-D04", "Minh", Line("CH02-D04"));
            voice.StopCurrent();
            Require(interrupted == 1 && !voice.IsSpeaking, "Interrupted speech settles once and releases active playback");
            var journalSave = transaction.SaveDialogueAsync();
            while (!journalSave.IsCompleted) yield return null;
            Apply(journalSave.GetAwaiter().GetResult(), "transcript autosave");
            Require(saves.Read(SaveSlot.Auto, out var journal) == SaveRead.Valid &&
                journal.transcript.Exists(entry => entry.LineId == "CH02-D03") &&
                journal.transcript.Exists(entry => entry.LineId == "CH02-D04"), "Spoken and interrupted lines round-trip");
            RestoreJournal(journal);
            Require(voice.Transcript.Count == journal.transcript.Count, "Journal restores delivered IDs");
            verification.Add("M2A-DIALOGUE PASS: interruption settles once, allowed transcript persists through the snapshot and restore.");

            // 3) Digital Human import/production contract and dual-scenario GI.
            if (scene.referenceArt)
            {
                VerifyDigitalHumanContracts();
                int rProbes = scene.realLightProbes != null ? scene.realLightProbes.count : 0;
                int bProbes = scene.reportLightProbes != null ? scene.reportLightProbes.count : 0;
                Require(rProbes > 0 && bProbes > 0, $"Dual-scenario baked LightProbes present (R={rProbes}, B={bProbes})");
                verification.Add($"M2 dual GI PASS: R={rProbes}, B={bProbes}.");
            }

            // 4) CH02-Q05 Gatekeeper cameo trigger, dual-door escape (Door A & Door B), and CH02-T06 non-repeat check
            if (gatekeeperCameo != null && scene.GatekeeperActor != null)
            {
                Require(State.chapter == "CH02" && GateService.G02(State), "Ready in CH02 post-G02 for cameo test");
                if (State.layer == Reality.B) Apply(casework.SwitchIn101(scene.Pad("AP04").position), "return R for cameo");
                var beforeEncounter = State.Copy();
                beforeEncounter.events.Remove("M1-complete");
                beforeEncounter.checkpoint = "CH02-CP03";
                Apply(transaction.Commit(beforeEncounter, scene.Pad("AP04").position), "seed pre-encounter checkpoint");
                Require(gatekeeperCameo.ShouldTrigger(State, new Vector3(4.0f, 0.02f, 4.75f)), "Gatekeeper triggers in CH02 post-G02 corridor");
                gatekeeperCameo.BeginCameo(State, Line);
                Require(gatekeeperCameo.IsActive, "Gatekeeper cameo active");
                // Verify both Door A (x=4.0, z=16.5) and Door B (x=0.05, z=16.5) count as valid escape routes!
                Require(gatekeeperCameo.CheckEscapedToEmergency(new Vector3(4.0f, 0.02f, 16.5f)), "Door A escape valid");
                Require(gatekeeperCameo.CheckEscapedToEmergency(new Vector3(0.05f, 0.02f, 16.5f)), "Door B staff escape valid");
                Require(!gatekeeperCameo.CheckEscapedToEmergency(new Vector3(11f, 0.02f, 16.5f)), "Outside ER is not an escape");
                Vector3 safe = scene.Pad("AP05").position;
                long previous = State.sequence;
                var fault = casework.SaveEmergencyAsync(safe, true, gatekeeperCameo.IsActive,
                    () => throw new IOException("QA interruption while saving CH02-T06"));
                while (!fault.IsCompleted) yield return null;
                if (Argument("-ctcc-qa-probe") == "escape-premature") gatekeeperCameo.StopAndHide();
                Require(fault.GetAwaiter().GetResult() == CommitResult.SaveUnavailable && gatekeeperCameo.IsActive &&
                    !gatekeeperCameo.WasResolved && State.sequence == previous && !State.Happened("CH02-T06") &&
                    !State.Happened("M1-complete") && saves.Read(SaveSlot.Auto, out var beforeEscape) == SaveRead.Valid &&
                    !beforeEscape.Happened("CH02-T06") && !beforeEscape.Happened("M1-complete"),
                    "Failed escape publication retains live encounter and previous snapshot");
                var retry = casework.SaveEmergencyAsync(safe, true, gatekeeperCameo.IsActive);
                while (!retry.IsCompleted) yield return null;
                Apply(retry.GetAwaiter().GetResult(), "CH02-T06 persisted");
                Require(State.Happened("CH02-T06") && !gatekeeperCameo.IsActive && !gatekeeperCameo.ShouldTrigger(State, new Vector3(4.0f, 0.02f, 4.75f)),
                    "CH02-T06 persisted and Gatekeeper never repeats");
                Require(saves.Read(SaveSlot.Auto, out var ended) == SaveRead.Valid && ended.Happened("CH02-T06") &&
                    ended.Happened("M1-complete") && ended.checkpoint == "CH02-CP04" && ended.sequence == previous + 1,
                    "Escape and ending coexist in one durable revision");
                verification.Add("CH02-Q05 PASS: both escape thresholds, outside rejected, failed publication keeps cameo live, retry durably commits T06/CP04 once.");
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
                // M2-C Gatekeeper closer corridor detail inspection (~1.9 m)
                player.Teleport(new Vector3(3.92f, 0.02f, 9.48f), Quaternion.Euler(0, 3, 0));
                camera.transform.localRotation = Quaternion.Euler(-4, 0, 0);
                for (int i = 0; i < 6; i++) yield return null;
                SaveFrame(camera, target, "m2c-gatekeeper-closeup-R");
                scene.GatekeeperActor.gameObject.SetActive(false);
            }

            // M2-C CHAR007 Bé An Room 101 Bed 4 Human-Beneath-Apparition Close-Up Capture (R)
            if (scene.AnActor != null)
            {
                scene.AnActor.gameObject.SetActive(true);
                scene.AnActor.transform.SetPositionAndRotation(new Vector3(10.85f, 0f, 3.65f), Quaternion.Euler(0f, -68f, 0f));
                scene.AnActor.LookTarget = camera.transform;
                scene.AnActor.SetApparitionAlpha(0.84f);
                scene.AnActor.PlayAction("An_IdleLook", 0.01f, restart: true);
                player.Teleport(new Vector3(9.45f, 0.02f, 4.18f), Quaternion.Euler(0f, 110f, 0f));
                camera.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
                for (int i = 0; i < 10; i++) yield return null;
                SaveFrame(camera, target, "m2c-an-vignette-R");
                scene.AnActor.SetApparitionAlpha(0f);
                scene.AnActor.gameObject.SetActive(false);
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

            // M2-C CHAR002 Trần Ngọc Lan Multi-Distance In-Game + Neutral Studio Lighting Validation (B 2005!)
            if (scene.LanActor != null)
            {
                var lan = scene.LanActor;
                lan.gameObject.SetActive(true);
                if (scene.LanStepOutPoint != null)
                    lan.transform.SetPositionAndRotation(scene.LanStepOutPoint.position, scene.LanStepOutPoint.rotation);
                lan.LookTarget = camera.transform;
                lan.SetExpression(0.62f, 0.14f, 0f);
                lan.PlayAction("Lan_TalkGesture", 0.01f, restart: true);
                lan.ForceSpeechTime("CH01-D03", 0.10f);

                // 1) Conversation distance (~1.55 m, canonical LOC02 B lighting)
                player.Teleport(new Vector3(4.15f, 0.02f, -3.55f), Quaternion.Euler(0, -82, 0));
                camera.transform.localRotation = Quaternion.Euler(3, 0, 0);
                for (int i = 0; i < 14; i++) yield return null;
                SaveFrame(camera, target, "loc02-lan-B");

                // 2) Close-up portrait distance (~0.82 m: eyes, SSS skin, pores, lashes, hairline, collar, ID badge!)
                Vector3 lanPos = lan.transform.position;
                Vector3 lanFwd = lan.transform.forward;
                Vector3 lanRight = lan.transform.right;
                Vector3 closeFeet = lanPos + lanFwd * 0.84f + lanRight * 0.14f;
                player.Teleport(new Vector3(closeFeet.x, 0.02f, closeFeet.z), Quaternion.LookRotation(new Vector3(lanPos.x - closeFeet.x, 0f, lanPos.z - closeFeet.z)));
                camera.transform.localRotation = Quaternion.Euler(9.5f, 0f, 0f);
                lan.ForceSpeechTime("CH01-D03", 0.58f);
                for (int i = 0; i < 10; i++) yield return null;
                SaveFrame(camera, target, "m2c-lan-closeup-B");

                // 3) Handover with physical 3D F101 clipboard held on ClipboardSocket_R!
                lan.EndSpeech();
                lan.SetHeldClipboardVisible(true);
                lan.SetExpression(0.45f, 0.24f, 0.08f);
                lan.PlayAction("Lan_Handover", 0.01f, restart: true);
                Vector3 handFeet = lanPos + lanFwd * 1.48f - lanRight * 0.36f;
                player.Teleport(new Vector3(handFeet.x, 0.02f, handFeet.z), Quaternion.LookRotation(new Vector3(lanPos.x - handFeet.x, 0f, lanPos.z - handFeet.z)));
                camera.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
                for (int i = 0; i < 36; i++) yield return null;
                SaveFrame(camera, target, "m2c-lan-handover-B");
                lan.SetHeldClipboardVisible(false);

                // 4) Medium full-body distance (~2.95 m: silhouette, uniform drape, shoes, LOD0->LOD1 readability)
                lan.PlayAction("Lan_IdleDesk", 0.01f, restart: true);
                lan.SetExpression(0.55f, 0.10f, 0f);
                Vector3 medFeet = lanPos + lanFwd * 2.85f + lanRight * 0.45f;
                player.Teleport(new Vector3(medFeet.x, 0.02f, medFeet.z), Quaternion.LookRotation(new Vector3(lanPos.x - medFeet.x, 0f, lanPos.z - medFeet.z)));
                camera.transform.localRotation = Quaternion.Euler(11f, 0f, 0f);
                for (int i = 0; i < 10; i++) yield return null;
                SaveFrame(camera, target, "m2c-lan-medium-B");

                // 5) Unity Neutral 5500K Studio Lighting Validation Rig (verifying skin/eye/hair/fabric shaders neutral!)
                var studioRig = new GameObject("_M2C_NeutralStudioRig");
                Light AddStudioLight(string nm, Vector3 offset, Color col, float inten, float range)
                {
                    var go = new GameObject(nm);
                    go.transform.SetParent(studioRig.transform, false);
                    go.transform.position = lanPos + offset;
                    var l = go.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.color = col;
                    l.intensity = inten;
                    l.range = range;
                    l.shadows = LightShadows.None;
                    return l;
                }
                AddStudioLight("Key5500K", lanFwd * 1.45f + lanRight * 0.90f + Vector3.up * 2.05f, new Color(1.0f, 0.98f, 0.96f), 1.65f, 6.0f);
                AddStudioLight("FillCool", lanFwd * 1.25f - lanRight * 1.15f + Vector3.up * 1.55f, new Color(0.90f, 0.95f, 1.0f), 0.78f, 6.0f);
                AddStudioLight("RimBack", -lanFwd * 0.95f - lanRight * 0.55f + Vector3.up * 2.00f, new Color(0.95f, 0.97f, 1.0f), 1.05f, 5.0f);
                player.Teleport(new Vector3(closeFeet.x, 0.02f, closeFeet.z), Quaternion.LookRotation(new Vector3(lanPos.x - closeFeet.x, 0f, lanPos.z - closeFeet.z)));
                camera.transform.localRotation = Quaternion.Euler(9.5f, 0f, 0f);
                for (int i = 0; i < 8; i++) yield return null;
                SaveFrame(camera, target, "m2c-lan-neutral-studio");
                Destroy(studioRig);

                yield return VerifyForcedFaceLods(lan, camera, target);
                lan.gameObject.SetActive(false);
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

            // M2-C 30fps Character Performance & Gameplay Showcase (570 frames = 19s @ 30fps + audio_cues.json!)
            var cues = new List<string>();
            void LogCue(string assetRel, int startFrame, float gain = 1.0f) =>
                cues.Add($"{{\"asset\":\"{assetRel}\",\"frame\":{startFrame},\"time\":{(startFrame / 30.0f):F3},\"gain\":{gain:F2}}}");

            // Segment 1 (Frames 0..330 = 11.0 s): LOC02 Hero Reception Encounter with CHAR002 Trần Ngọc Lan (B 2005)
            player.Teleport(scene.Pad("AP04"));
            if (State.layer == Reality.R) Apply(casework.SwitchIn101(player.Feet), "record B");
            for (int i = 0; i < 12; i++) yield return null;

            LogCue("Prototype/Assets/CTCC/Art/Audio/MUS_LanMotif.wav", 0, 0.24f);
            if (scene.LanActor != null)
            {
                var lan = scene.LanActor;
                lan.gameObject.SetActive(true);
                lan.LookTarget = camera.transform;
                Vector3 deskPos = scene.LanDeskPoint != null ? scene.LanDeskPoint.position : new Vector3(2.0f, 0f, -3.3f);
                Vector3 stepPos = scene.LanStepOutPoint != null ? scene.LanStepOutPoint.position : new Vector3(2.85f, 0f, -3.3f);
                Quaternion stepRot = scene.LanStepOutPoint != null ? scene.LanStepOutPoint.rotation : Quaternion.Euler(0, 90, 0);

                // 1A: Stepping out from behind reception desk (Frames 0..55, 1.83s)
                lan.PlayAction("Lan_StepOut", 0.05f, restart: true);
                lan.SetExpression(0.65f, 0.12f, 0f);
                for (int f = 0; f < 55; f++)
                {
                    float s = Smooth(f / 54f);
                    lan.transform.SetPositionAndRotation(Vector3.Lerp(deskPos, stepPos, s), stepRot);
                    Vector3 camPos = Vector3.Lerp(new Vector3(4.55f, 0.02f, -4.40f), new Vector3(4.12f, 0.02f, -3.48f), s);
                    CapturePose(camPos, Vector3.Lerp(deskPos + Vector3.up * 1.44f, stepPos + Vector3.up * 1.46f, s));
                    yield return null;
                }

                // 1B: CH01-D03 ("Anh đến trễ rồi.") close-up with synced 30Hz Vietnamese lip-sync (Frames 55..110)
                LogCue("Prototype/Assets/CTCC/Art/Audio/VO/CH01-D03.wav", frameIdx, 1.0f);
                lan.transform.SetPositionAndRotation(stepPos, stepRot);
                lan.PlayAction("Lan_TalkGesture", 0.20f, restart: true);
                lan.SetExpression(0.70f, 0.10f, 0f);
                Vector3 lanFwd = lan.transform.forward;
                Vector3 lanRight = lan.transform.right;
                for (int f = 0; f < 55; f++)
                {
                    float s = Smooth(f / 54f);
                    lan.ForceSpeechTime("CH01-D03", f / 30f);
                    Vector3 camPos = Vector3.Lerp(stepPos + lanFwd * 1.30f + lanRight * 0.22f, stepPos + lanFwd * 0.88f + lanRight * 0.14f, s);
                    camPos.y = 0.02f;
                    CapturePose(camPos, stepPos + Vector3.up * 1.48f);
                    yield return null;
                }

                // 1C: CH01-D04 Minh speaks while Lan listens attentively with Lan_Listen + subtle nod (Frames 110..175)
                LogCue("Prototype/Assets/CTCC/Art/Audio/VO/CH01-D04.wav", frameIdx, 0.92f);
                lan.EndSpeech();
                lan.PlayAction("Lan_Listen", 0.24f, restart: true);
                lan.SetExpression(0.38f, 0.28f, 0.06f);
                for (int f = 0; f < 65; f++)
                {
                    float s = Smooth(f / 64f);
                    Vector3 camPos = Vector3.Lerp(stepPos + lanFwd * 0.88f + lanRight * 0.14f, stepPos + lanFwd * 1.02f - lanRight * 0.12f, s);
                    camPos.y = 0.02f;
                    CapturePose(camPos, stepPos + Vector3.up * 1.46f);
                    yield return null;
                }

                // 1D: CH01-D05 ("Em giữ bảng hồ sơ đây rồi.") speaking with warm gesture & lip-sync (Frames 175..255)
                LogCue("Prototype/Assets/CTCC/Art/Audio/VO/CH01-D05.wav", frameIdx, 1.0f);
                lan.PlayAction("Lan_TalkGesture", 0.22f, restart: true);
                lan.SetExpression(0.56f, 0.24f, 0.06f);
                for (int f = 0; f < 80; f++)
                {
                    float s = Smooth(f / 79f);
                    lan.ForceSpeechTime("CH01-D05", f / 30f);
                    Vector3 camPos = Vector3.Lerp(stepPos + lanFwd * 1.02f - lanRight * 0.12f, stepPos + lanFwd * 1.42f - lanRight * 0.30f, s);
                    camPos.y = 0.02f;
                    CapturePose(camPos, stepPos + Vector3.up * 1.40f);
                    yield return null;
                }

                // 1E: Lan_Handover presenting physical 3D F101 clipboard toward Minh (Frames 255..330)
                lan.EndSpeech();
                lan.SetHeldClipboardVisible(true);
                lan.PlayAction("Lan_Handover", 0.20f, restart: true);
                lan.SetExpression(0.46f, 0.22f, 0.10f);
                for (int f = 0; f < 75; f++)
                {
                    float s = Smooth(f / 74f);
                    Vector3 camPos = Vector3.Lerp(stepPos + lanFwd * 1.42f - lanRight * 0.30f, stepPos + lanFwd * 1.56f + lanRight * 0.28f, s);
                    camPos.y = 0.02f;
                    CapturePose(camPos, stepPos + Vector3.up * 1.24f);
                    yield return null;
                }
                lan.SetHeldClipboardVisible(false);
                lan.gameObject.SetActive(false);
            }

            // Segment 2 (Frames 330..450 = 4.0 s): LOC03 Room 101 Bé An Apparition Vignette (R 2012, CH02-D03!)
            player.Teleport(scene.Pad("AP04"));
            Apply(casework.SwitchIn101(player.Feet), "record R");
            for (int i = 0; i < 12; i++) yield return null;

            if (scene.AnActor != null)
            {
                var an = scene.AnActor;
                an.gameObject.SetActive(true);
                an.transform.SetPositionAndRotation(new Vector3(10.85f, 0f, 3.65f), Quaternion.Euler(0f, -68f, 0f));
                an.LookTarget = camera.transform;
                an.PlayAction("An_IdleLook", 0.05f, restart: true);
                LogCue("Prototype/Assets/CTCC/Art/Audio/VO/CH02-D03.wav", frameIdx + 18, 0.96f);
                for (int f = 0; f < 120; f++)
                {
                    float alpha = f < 20 ? (f / 20f) * 0.84f : (f > 90 ? Mathf.Lerp(0.84f, 0f, (f - 90) / 30f) : 0.84f);
                    an.SetApparitionAlpha(alpha);
                    if (f >= 18 && f < 86) an.ForceSpeechTime("CH02-D03", (f - 18) / 30f);
                    else an.EndSpeech();
                    if (f == 86) an.PlayAction("An_TurnFade", 0.22f, restart: true);
                    float s = Smooth(f / 119f);
                    Vector3 camPos = Vector3.Lerp(new Vector3(9.10f, 0.02f, 4.35f), new Vector3(9.58f, 0.02f, 4.10f), s);
                    CapturePose(camPos, new Vector3(10.85f, 0.96f, 3.65f));
                    yield return null;
                }
                an.EndSpeech();
                an.SetApparitionAlpha(0f);
                an.gameObject.SetActive(false);
            }

            // Segment 3 (Frames 450..570 = 4.0 s): LOC03/LOC04 Gatekeeper Corridor Cameo (R 2012, SFX-KEY + CH02-D05!)
            if (scene.GatekeeperActor != null)
            {
                var gk = scene.GatekeeperActor;
                gk.gameObject.SetActive(true);
                gk.transform.SetPositionAndRotation(new Vector3(4.0f, 0f, 12.6f), Quaternion.Euler(0, 180, 0));
                gk.LookTarget = camera.transform;
                gk.PlayAction("GK_StalkWalk", 0.05f, restart: true);
                LogCue("Prototype/Assets/CTCC/Art/Audio/KeyJingle.wav", frameIdx + 4, 0.85f);
                LogCue("Prototype/Assets/CTCC/Art/Audio/VO/CH02-D05.wav", frameIdx + 12, 1.0f);
                LogCue("Prototype/Assets/CTCC/Art/Audio/KeyJingle.wav", frameIdx + 62, 0.90f);
                for (int f = 0; f < 120; f++)
                {
                    float s = Smooth(f / 119f);
                    Vector3 gkPos = Vector3.Lerp(new Vector3(4.0f, 0f, 12.6f), new Vector3(4.0f, 0f, 9.8f), s);
                    gk.transform.position = gkPos;
                    if (f >= 12 && f < 112) gk.ForceSpeechTime("CH02-D05", (f - 12) / 30f);
                    Vector3 camPos = Vector3.Lerp(new Vector3(3.88f, 0.02f, 7.65f), new Vector3(3.92f, 0.02f, 8.15f), s);
                    CapturePose(camPos, gkPos + Vector3.up * 1.56f);
                    yield return null;
                }
                gk.EndSpeech();
                gk.gameObject.SetActive(false);
            }

            File.WriteAllText(Path.Combine(dir, "audio_cues.json"), "{\"fps\":30,\"frames\":" + frameIdx + ",\"cues\":[" + string.Join(",", cues) + "]}");

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
