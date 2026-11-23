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
                Require(!MayDeliver("CH01-D16b", "B06") && !MayDeliver("CH04-D06", "B06"), "Earlier chapter dialogue guard");
                verification.Add("V22-Q03 partial PASS: CH04 lines blocked before CH04.");
                passed=true;
            }
            catch(Exception e)
            {
                verification.Add("FAIL: "+e.ToString());
                Debug.LogException(e);
            }
            if (passed) yield return FinishVerification(VerifyModuleStages(), qaFolder);
            else
            {
                yield return PrepareForQuit();
                CompleteVerification(qaFolder, false);
            }
        }

        private IEnumerator VerifyModuleStages()
        {
            yield return VerifyDeferredSaveFailure();
            yield return VerifyM2AdditiveAndCharacters();
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null) yield return VerifySpatialAudio();
            yield return VerifyKeyboard();
            yield return VerifyCh01RAdversarialSuite();
            yield return VerifyCh01WalkthroughSuite();
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null) yield return VerifyRender();
            yield return VerifyScalableM3AndCh03();
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
            yield return PrepareForQuit();
            CompleteVerification(directory, passed);
        }

        private IEnumerator PrepareForQuit()
        {
            float deadline = Time.realtimeSinceStartup + 4f;
            while ((busy || savingDialogue) && Time.realtimeSinceStartup < deadline) yield return null;
            inspector?.Close();
            ui?.Close();
            voice?.StopCurrent();
            lanDirector?.StopEncounter(false);
            gatekeeperCameo?.StopAndHide();
            anDirector?.HideImmediately();
            AudioListener.pause = true;
            // Keep submitting frames during teardown so a failure on the first gameplay frame also drains uploads.
            if (player?.Eye != null) player.Eye.enabled = true;
            float drainStart = Time.realtimeSinceStartup;
            while ((Time.realtimeSinceStartup - drainStart < 1.5f || Texture.streamingTextureLoadingCount > 0 ||
                    Texture.streamingTexturePendingLoadCount > 0) && Time.realtimeSinceStartup - drainStart < 5.5f)
                yield return null;
            if (SystemInfo.supportsAsyncGPUReadback)
            {
                var drain = RenderTexture.GetTemporary(8, 8, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(Texture2D.blackTexture, drain);
                var marker = AsyncGPUReadback.Request(drain);
                while (!marker.done) yield return null;
                RenderTexture.ReleaseTemporary(drain);
            }
            AsyncGPUReadback.WaitAllRequests();
            GL.Flush();
            for (int i = 0; i < 8; i++)
                yield return new WaitForEndOfFrame();
            AsyncGPUReadback.WaitAllRequests();
            GL.Flush();
            if (player?.Eye != null) player.Eye.enabled = false;
            if (Camera.main != null) Camera.main.enabled = false;
            yield return null;
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
            ui.Show("KIỂM THỬ TRỢ NĂNG","Điều hướng bàn phím","Cỡ chữ 130%. Đổi cách điều khiển được lưu.",
                new[]{new ActionChoice("Lựa chọn 1",()=>first=true),new ActionChoice("Lựa chọn 2",()=>second=true)},
                ()=>{escaped=true;ui.Close();});
            var before=InputSystem.settings.updateMode;
            var background=InputSystem.settings.backgroundBehavior;
            InputSystem.settings.updateMode=InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            var keyboard=InputSystem.AddDevice<Keyboard>();
            InputSystem.EnableDevice(keyboard);
            try
            {
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
            scene.interactables?.ResetSessionProps();
            if (scene.staffCrossDoor != null) scene.staffCrossDoor.isOpen = false;
            player.Teleport(scene.Pad("AP00"));
            Require(casework.OpenGate(player.Feet)!=CommitResult.Applied,"gate before SMS");
            Apply(casework.ReadLetter(player.Feet),"SMS");
            Apply(casework.OpenGate(player.Feet),"gate");
            var realWardBefore = ActiveWardColliders();
            Walk(new Vector3(4,0,-17.9f));
            Walk(new Vector3(4,0,-3.3f));
            Apply(casework.AtHub(player.Feet,out _),"enter B");
            player.Teleport(scene.Pad("AP02"));
            Require(State.layer==Reality.B && !GateService.G01(State),"first visit B");
            Apply(casework.Board(player.Feet),"board");
            Walk(new Vector3(4,0,4.75f));
            Walk(new Vector3(6.51f,0,4.88f));
            Apply(casework.EnterRoom(player.Feet),"room CP02");
            RequireF101Presentation(FormStatus.Pending, true);
            var validRefutation = new DeductionSubmission
            {
                formId = "F101",
                selectedEvidenceIds = new List<string> { ObservationRegistry.Bed4Traces, ObservationRegistry.Bed4Card },
                claimId = HypothesisService.ClaimUnreliableZero
            };
            if(route==FormStatus.Refuted)
            {
                Apply(casework.Observe("OBS101A",player.Feet,"LOC03"),"A observed");
                Require(casework.SubmitF101(route,validRefutation,player.Feet,out var eval1)!=CommitResult.Applied && !eval1.Accepted,"one observation denies C");
                Apply(casework.Observe("OBS101B",player.Feet,"LOC03"),"B observed");
                var partialSub = new DeductionSubmission { formId = "F101", selectedEvidenceIds = new List<string> { "OBS101A" }, claimId = HypothesisService.ClaimUnreliableZero };
                Require(casework.SubmitF101(route,partialSub,player.Feet,out var eval2)!=CommitResult.Applied && eval2.Outcome==HypothesisOutcome.InsufficientEvidence,"C without selected pair");
            }
            Apply(casework.SubmitF101(route,route==FormStatus.Refuted?validRefutation:null,player.Feet,out var routeEval),"F101 confirmation");
            Require(routeEval.Accepted,"F101 route evaluation accepted");
            Require(GateService.G01(State) && !GateService.G02(State) && State.f101.firstDecision==route &&
                FormService.Distortion(State)==(route==FormStatus.Affirmed?1:0) && !State.Has("E12") && State.evidence.Count==1,
                "form result before G02");
            RequireF101Presentation(route, true);
            Walk(new Vector3(4,0,4.75f));
            Walk(new Vector3(4,0,-3.29f));
            Apply(casework.AtHub(player.Feet,out _),"collapse");
            player.Teleport(scene.Pad("AP02"));
            Require(State.layer==Reality.R,"collapse R");
            RequireF101Presentation(route, false);
            var realWardAfter = ActiveWardColliders();
            Require(realWardBefore.Count == realWardAfter.Count, "F101 leaves R ward collider count unchanged");
            foreach (var (collider, bounds) in realWardBefore)
                Require(realWardAfter.TryGetValue(collider, out var after) && after == bounds,
                    "F101 leaves R ward collider and bounds unchanged: " + collider.name);
            Walk(new Vector3(4,0,-19.2f));
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
                Apply(casework.SubmitF101(FormStatus.Unverified,null,player.Feet,out _),"revision A→B");
                Apply(casework.SwitchIn101(player.Feet),"back to B for observations");
                foreach(var id in new[]{"OBS101A","OBS101B"}) Apply(casework.Observe(id,player.Feet,"LOC03"),id);
                Apply(casework.SubmitF101(FormStatus.Refuted,validRefutation,player.Feet,out var revEval),"revision B→C");
                Require(revEval.Accepted,"revision B→C accepted");
                for(int i=0;i<10;i++) Apply(casework.SubmitF101(FormStatus.Refuted,validRefutation,player.Feet,out _),"duplicate F101");
                Require(State.f101.firstDecision==FormStatus.Affirmed && State.f101.revision==3 &&
                    State.f101History.Count==3 && FormService.Distortion(State)==0,"revision idempotence");
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
                player.SetTorchEnabled(true);
                for (int i = 0; i < 6; i++) yield return null;
                player.Teleport(new Vector3(3.85f, 0.02f, 8.25f), Quaternion.Euler(0, 4, 0));
                camera.transform.localRotation = Quaternion.Euler(-2, 0, 0);
                SaveFrame(camera, target, "loc04-gatekeeper-R");
                // M2-C Gatekeeper closer corridor detail inspection (~1.9 m)
                player.Teleport(new Vector3(3.92f, 0.02f, 9.48f), Quaternion.Euler(0, 3, 0));
                camera.transform.localRotation = Quaternion.Euler(-4, 0, 0);
                for (int i = 0; i < 6; i++) yield return null;
                SaveFrame(camera, target, "m2c-gatekeeper-closeup-R");
                player.SetTorchEnabled(false);
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
            player.Teleport(new Vector3(3.85f, 0.02f, 1.85f), Quaternion.Euler(0, 16, 0));
            camera.transform.localRotation = Quaternion.Euler(2, 0, 0);
            SaveFrame(camera, target, "corridor-long-B");

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
                lan.SetExpression(0.52f, 0.22f, 0.05f);
                lan.PlayAction("Lan_Handover", 0.01f, restart: true);
                Vector3 handFeet = lanPos + lanFwd * 1.48f - lanRight * 0.36f;
                player.Teleport(new Vector3(handFeet.x, 0.02f, handFeet.z), Quaternion.LookRotation(new Vector3(lanPos.x - handFeet.x, 0f, lanPos.z - handFeet.z)));
                camera.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
                for (int i = 0; i < 12; i++) yield return null;
                lan.SetActionTime(1.15f);
                lan.ForceOpenEyes();
                yield return null;
                Require(lan.heldClipboard != null && lan.heldClipboard.transform.position.y >= 1.00f,
                    $"Lan handover clipboard height too low: y={lan.heldClipboard?.transform.position.y:F3}");
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
                AddStudioLight("Key5500K", lanFwd * 1.45f + lanRight * 0.90f + Vector3.up * 2.05f, new Color(1.0f, 0.98f, 0.96f), 1.08f, 6.0f);
                AddStudioLight("FillCool", lanFwd * 1.25f - lanRight * 1.15f + Vector3.up * 1.55f, new Color(0.90f, 0.95f, 1.0f), 0.56f, 6.0f);
                AddStudioLight("RimBack", -lanFwd * 0.95f - lanRight * 0.55f + Vector3.up * 2.00f, new Color(0.95f, 0.97f, 1.0f), 0.78f, 5.0f);
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
            void LogCue(string assetRel, int startFrame, float gain = 1.0f)
            {
                var inv = System.Globalization.CultureInfo.InvariantCulture;
                cues.Add("{\"asset\":\"" + assetRel + "\",\"frame\":" + startFrame +
                    ",\"time\":" + (startFrame / 30.0f).ToString("F3", inv) +
                    ",\"gain\":" + gain.ToString("F2", inv) + "}");
            }

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

                // 1A: Stepping out from behind reception desk (Frames 0..42, 1.40s @ exact 1.70 m/s stride sync!)
                Vector3 stepDir = (stepPos - deskPos).normalized;
                float stepDist = Vector3.Distance(deskPos, stepPos);
                int stepFrames = Mathf.Clamp(Mathf.RoundToInt((stepDist / 1.70f) * 30f), 30, 55);
                lan.PlayAction("Lan_StepOut", 0.01f, restart: true);
                lan.SetExpression(0.65f, 0.12f, 0f);
                for (int f = 0; f < stepFrames; f++)
                {
                    float t = f / 30f;
                    float s = Smooth(f / (float)(stepFrames - 1));
                    Vector3 curPos = deskPos + stepDir * Mathf.Min(stepDist, 1.70f * t);
                    lan.transform.SetPositionAndRotation(curPos, stepRot);
                    lan.SetActionTime(t);
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
                player.SetTorchEnabled(true);
                gk.transform.SetPositionAndRotation(new Vector3(4.0f, 0f, 12.6f), Quaternion.Euler(0, 180, 0));
                gk.LookTarget = camera.transform;
                gk.PlayAction("GK_StalkWalk", 0.05f, restart: true);
                LogCue("Prototype/Assets/CTCC/Art/Audio/KeyJingle.wav", frameIdx + 4, 0.85f);
                LogCue("Prototype/Assets/CTCC/Art/Audio/VO/CH02-D05.wav", frameIdx + 12, 1.0f);
                LogCue("Prototype/Assets/CTCC/Art/Audio/KeyJingle.wav", frameIdx + 62, 0.90f);
                for (int f = 0; f < 120; f++)
                {
                    float t = f / 30f;
                    float s = Smooth(f / 119f);
                    // Exact 1.78 m/s world translation matched to GK_StalkWalk analytical leg IK!
                    Vector3 gkPos = new(4.0f, 0f, 13.6f - 1.78f * t);
                    gk.transform.position = gkPos;
                    gk.SetActionTime(t);
                    if (f >= 12 && f < 112) gk.ForceSpeechTime("CH02-D05", (f - 12) / 30f);
                    Vector3 camPos = Vector3.Lerp(new Vector3(3.88f, 0.02f, 6.85f), new Vector3(3.92f, 0.02f, 5.45f), s);
                    CapturePose(camPos, gkPos + Vector3.up * 1.56f);
                    yield return null;
                }
                player.SetTorchEnabled(false);
                gk.EndSpeech();
                gk.gameObject.SetActive(false);
            }

            File.WriteAllText(Path.Combine(dir, "audio_cues.json"), "{\"fps\":30,\"frames\":" + frameIdx + ",\"cues\":[" + string.Join(",", cues) + "]}");

            static float Smooth(float t) => t * t * (3f - 2f * t);

            Destroy(shot);
            rt.Release();
            Destroy(rt);
        }

        private IEnumerator VerifyScalableM3AndCh03()
        {
            if (scene.IsAdditiveMode)
            {
                yield return scene.EnsureRegionalZonesStreamedAsync();
                Require(scene.LoadedZoneCount == 6 && scene.HasZone("LOC05") && scene.HasZone("LOC09"),
                    "Upper and service regions registered");
            }
            var ap06 = scene.Pad("AP06");
            var an03 = scene.Pad("AN03");
            var ap07 = scene.Pad("AP07");
            Require(Mathf.Abs(ap06.position.y - 4f) < .04f && Mathf.Abs(an03.position.y - 8f) < .04f &&
                Mathf.Abs(scene.Pad("AP08").position.y + 3.4f) < .04f, "Canonical floor elevations");
            Require(scene.archiveExitDoor != null && scene.Ch03PatrolWaypoints.Length >= 3 && scene.Ch03RadioPoint != null,
                "Archive gate, patrol and radio are present");
            Require(voice.HasClip("CH03-D01") && voice.HasClip("CH03-D03") && voice.HasClip("CH03-D05"), "Archive voice clips");
            VerifyLegacySave(1);
            VerifyLegacySave(2);
            VerifyLegacySave(3);
            VerifyLegacySave(4);
            VerifyLegacySave(5);
            VerifyLegacySave(6);

            var multi = State.Copy();
            multi.f101.current = FormStatus.Affirmed;
            Require(!FormService.ApplyForm(multi, "F303", FormStatus.Affirmed) &&
                FormService.Distortion(multi, "LOC03") == 1 && FormService.Distortion(multi, "LOC07") == 0,
                "Unknown forms rejected and F101 stays local");
            var extraIds = new[] { "QA1", "QA2", "QA3", "QA4", "QA5", "QA6" };
            var definitions = new Dictionary<string, FormService.Definition>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in extraIds)
            {
                definitions[id] = new(id == "QA6" ? "LOC09" : "LOC07", id != "QA5", _ => true);
                multi.forms.Add(new FormEntry { formId = id, current = FormStatus.Affirmed });
            }
            multi.forms.Add(multi.forms[0]);
            Require(FormService.Distortion(multi, "LOC07", definitions) == 4 &&
                FormService.Distortion(multi, "LOC09", definitions) == 1, "Multiple-form scope, uniqueness, truth and cap");
            Require(!EvidenceRegistry.IsPhysicalExportable(EvidenceRegistry.BayLockReceipt) &&
                !EvidenceRegistry.IsPhysicalExportable(EvidenceRegistry.NonExportableTraceE08), "FS10 and E08 excluded from physical evidence");

            if (State.layer != Reality.R) Apply(casework.SwitchIn101(scene.Pad("AP04").position), "Return R before CH03");
            var er = scene.Pad("AP05");
            player.Teleport(er);
            yield return WaitForApplied(casework.BeginCh03Async(player.Feet), "CH03 begins at ER");
            Require(Vector3.Distance(er.position, player.Feet) < .01f && State.chapter == "CH03", "Chapter handoff stays on grade");
            Apply(casework.TakeCh03Evidence(EvidenceRegistry.PatientRegister, "LOC07", an03.position), "Take E03");
            Apply(casework.TakeCh03Evidence(EvidenceRegistry.C02CareNote, "LOC07", an03.position), "Take care note");
            Apply(casework.TakeCh03Evidence(EvidenceRegistry.C02YarnBag, "LOC07", an03.position), "Take yarn bag");
            Apply(casework.AttemptCh03Pu01("C03", true, an03.position), "PZ03 wrong code");
            Apply(casework.AttemptCh03Pu01("C12", true, an03.position), "PZ03 second wrong code");
            Require(PuzzleService.ShouldShowFieldHint(State, "CH03-PU01") && !GateService.G03(State),
                "CH03-Q02 incorrect matching keeps G03 closed");
            yield return VerifyArchiveNavigation(an03);
            Apply(casework.AttemptCh03Pu01("C02", true, an03.position), "PZ03 correct code");
            Require(GateService.G03(State) && !State.PatientIdentityVerified("C02"), "CH03-Q01 gate opens on existence");
            Apply(casework.TakeCh03Evidence(EvidenceRegistry.C12UnnamedBox, "LOC07", an03.position), "Read C12 row and box");
            Require(State.Happened("FS19") && State.PatientConfirmed("C12") &&
                !State.Copy().VerifyPatientIdentity("C12", "Invalid sample"), "CH03-Q02 C12 stays anonymous");
            Apply(casework.TakeCh03Evidence(EvidenceRegistry.C02DaughterLetter, "LOC07", an03.position), "Read optional letter");
            Apply(casework.SolveCh03Pu02OptionalIdentity(an03.position), "Verify optional C02 identity");

            player.Teleport(an03);
            Apply(casework.SwitchMemoryVignetteAn03(an03.position), "Enter M");
            Require(State.layer == Reality.M && !orderlyAi.IsActive, "CH03-Q04 safe memory");
            yield return CaptureHospitalFrame("ch03-memory", an03, 32f);
            Require(transaction.Restore(SaveSlot.Auto, out var restoredFromM) == SaveRead.Valid &&
                State.layer == Reality.R && restoredFromM == an03 && State.Has(EvidenceRegistry.PatientRegister),
                "CH03-Q04 restores at AN03 in R with E03");
            Require(!HunterOrderlyAI.IsInSafeZone(an03.position + Vector3.up * 4f, scene), "Safe zone is floor-aware");
            yield return CaptureHospitalFrame("ch03-archive", an03, 18f);
            yield return CaptureHospitalFrame("ch03-ward", ap06, 35f);

            Apply(casework.Record("FS10", ap07.position), "Inspect FS10 in the duty corridor");
            Require(!State.Has(EvidenceRegistry.DutyOrders) && !State.Has(EvidenceRegistry.LanRecovery) &&
                !MayDeliver("CH01-D16b", "B06"), "CH03-Q05 evidence and reveal boundary");
            player.Teleport(ap07);
            Apply(casework.CompleteCh03(player.Feet), "Finish CH03 at AP07");
            if (scene.IsAdditiveMode)
                yield return scene.UnloadZoneAsync(RegionStreamer.Service);
            yield return WaitForApplied(casework.BeginCh04Async(), "Enter CH04 at AP07");
            if (scene.IsAdditiveMode)
            {
                Require(scene.ServiceApproachBlocked, "Closed service threshold while scene is absent");
                yield return scene.LoadZoneAsync(RegionStreamer.Service);
                Require(!scene.ServiceApproachBlocked, "CH04 state opens the service threshold after preload");
            }
            Require(!MayDeliver("CH04-D06", "B06"), "CH04 Lan closed at entry");
            yield return WaitForApplied(ChapterFour.Take(transaction, EvidenceRegistry.DutyOrders, "LOC06"), "Take E02");
            yield return WaitForApplied(ChapterFour.Take(transaction, EvidenceRegistry.DutyLog, "LOC06"), "Take E09");
            yield return WaitForApplied(ChapterFour.Take(transaction, EvidenceRegistry.MeetingTape, "LOC06"), "Take E06a");
            yield return WaitForApplied(ChapterFour.PresentMeeting(transaction), "CH04-Q04 read transcript");
            Require(State.Happened("PT01.presented") && !State.Has("E06"), "Tape remains distinct from written order");

            bool[] expectedFits = { true, true, true, true, true, true, false, false, true, true, true, true, true, false, false, false };
            for (int mask = 0; mask < 16; mask++)
            {
                Require(ChapterFour.ValidPower(mask) == expectedFits[mask], "CH04-Q02 circuit combination " + mask);
                yield return WaitForApplied(ChapterFour.SetPower(transaction, mask), "Write circuit state");
                Require(State.servicePower == (expectedFits[mask] ? mask : 0), "Save overload trip");
            }
            yield return WaitForApplied(ChapterFour.SetPower(transaction, 10), "Energize archive + service");
            yield return WaitForApplied(ChapterFour.Take(transaction, EvidenceRegistry.MorgueRegister, "LOC10"), "Take E04");
            Require(ChapterFour.MatchRecovery(transaction).GetAwaiter().GetResult() == CommitResult.PadRequired &&
                !GateService.H05(State), "CH04-Q03 E04 alone excludes H05");
            yield return WaitForApplied(ChapterFour.Take(transaction, EvidenceRegistry.LanRecovery, "LOC10"), "Take E07");
            yield return WaitForApplied(ChapterFour.MatchRecovery(transaction), "Verify H05 and present PT02");
            Require(GateService.G04(State) && MayDeliver("CH04-D06", "B06") && MayDeliver("CH01-D16b", "B06") &&
                !MayDeliver("CH01-D16b", "B05") && !State.Has("E08"), "CH04-Q03/Q06 exact beat, evidence and guard");
            yield return scene.UnloadZoneAsync(RegionStreamer.Service);
            Require(!scene.HasZone("LOC09") && !scene.TryPad("AP09", out _), "Service unload removes local pads");
            yield return scene.LoadZoneAsync(RegionStreamer.Service);
            Require(transaction.Restore(SaveSlot.Auto, out var restored) == SaveRead.Valid &&
                restored == scene.Pad("AP09") && State.servicePower == 10 && GateService.G04(State),
                "CH04-Q05 saved circuit and clues restored in reloaded B1");
            yield return CaptureHospitalFrame("ch04-morgue", restored, 0f);
            verification.Add("CH03-Q01/Q02/Q04/Q05 and CH04-Q02–Q06 module PASS. C02/C12, M restore, legacy schemas 1–5, local forms, power and H05/guard, reloaded region save.");
        }

        private IEnumerator VerifyArchiveNavigation(Transform safe)
        {
            player.Teleport(safe);
            ui.Close();
            orderlyAi.SyncState(State);
            var actor = scene.GatekeeperActor;
            Require(orderlyAi.IsActive && actor != null, "Archive hunter active after E03");
            Require(!orderlyAi.CheckLineOfSight(new Vector3(.2f, 8f, 34f), Vector3.right, new Vector3(2.3f, 8f, 34f)),
                "CH03-Q03 occluding shelf partition blocks line of sight");
            var start = scene.Ch03PatrolWaypoints[0].position;
            actor.transform.SetPositionAndRotation(start, Quaternion.identity);
            int blockedBefore = orderlyAi.InvalidPaths;
            int queriesBefore = orderlyAi.PathQueries;
            orderlyAi.TriggerDistraction(start - Vector3.up * 2.35f);
            Require(orderlyAi.CurrentState == OrderlyState.Investigate, "Unreachable stimulus entered investigate");
            double deadline = Time.realtimeSinceStartupAsDouble + .80;
            while (Time.realtimeSinceStartupAsDouble < deadline)
            {
                orderlyAi.Tick(State, Time.unscaledDeltaTime, false, false, false, Line);
                yield return null;
            }
            int rejected = orderlyAi.InvalidPaths - blockedBefore;
            int queries = orderlyAi.PathQueries - queriesBefore;
            Require(Vector3.Distance(start, actor.transform.position) < .015f && rejected >= 1 && queries <= 4,
                $"CH03-Q03 no raw movement after invalid path, rejected={rejected}, queries={queries}");
            orderlyAi.TriggerDistraction(scene.Ch03RadioPoint.position);
            deadline = Time.realtimeSinceStartupAsDouble + 1.8;
            while (Time.realtimeSinceStartupAsDouble < deadline)
            {
                orderlyAi.Tick(State, Time.unscaledDeltaTime, false, false, false, Line);
                yield return null;
            }
            float travelled = Vector3.Distance(start, actor.transform.position);
            Require(travelled > .45f && !HunterOrderlyAI.IsInSafeZone(actor.transform.position, scene),
                $"CH03-Q03 hunter navigates toward the radio and stays outside safe pads, displacement={travelled:F2}");
            verification.Add($"CH03-Q03 local module PASS: partition blocks LOS; {rejected} invalid paths in {queries} queries over 0.8 s, zero raw movement, radio displacement {travelled:F2} m.");
        }

        private void VerifyLegacySave(int schema)
        {
            var isolated = new AtomicSaves(Path.Combine(qaFolder, "legacy-schema-" + schema));
            var seed = State.Copy();
            seed.schemaVersion = schema;
            if (schema == 1) seed.patients.Clear();
            if (schema <= 3)
            {
                seed.observationRecords.Clear();
                seed.f101History.Clear();
                seed.f101Argument = new DeductionSubmission();
                seed.dreadEventsSeen.Clear();
                seed.dreadSeed = 0;
            }
            if (schema <= 6)
            {
                seed.backpackItems.Clear();
                seed.equippedInstanceId = "";
                seed.e10aLastTab = 0;
            }
            string payload = JsonUtility.ToJson(seed);
            payload = payload.Replace(",\"journalNotes\":[]", "").Replace(",\"sceneProps\":[]", "")
                .Replace(",\"backpackItems\":[]", "").Replace(",\"equippedInstanceId\":\"\"", "").Replace(",\"e10aLastTab\":0", "");
            if (schema == 4)
            {
                string draftField = ",\"f101Annotations\":" + JsonUtility.ToJson(seed.f101Annotations);
                Require(payload.Contains(draftField), "V4 fixture has a draft field to strip");
                payload = payload.Replace(draftField, "");
            }
            string checksum;
            using (var sha = System.Security.Cryptography.SHA256.Create())
                checksum = Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload)));
            Directory.CreateDirectory(Path.GetDirectoryName(isolated.SlotPath(SaveSlot.Auto)));
            File.WriteAllText(isolated.SlotPath(SaveSlot.Auto),
                $"{{\"schemaVersion\":{schema},\"payload\":{JsonEscape(payload)},\"checksum\":\"{checksum}\"}}");
            Require(isolated.Read(SaveSlot.Auto, out var upgraded) == SaveRead.Valid &&
                upgraded.schemaVersion == Investigation.Schema && upgraded.PatientIdentityVerified("C01") &&
                upgraded.f101History.Count == upgraded.f101.revision &&
                upgraded.f101.current == seed.f101.current && upgraded.f101.firstDecision == seed.f101.firstDecision &&
                upgraded.f101Annotations.marks.Count == 0 &&
                upgraded.sceneProps.Count == 0 && upgraded.journalNotes.Count == 0 &&
                upgraded.backpackItems.Count >= 1 &&
                upgraded.observationRecords.Count >= upgraded.observed.Count,
                "Upgrade legacy schema " + schema);
        }

        private IEnumerator VerifyCh01RAdversarialSuite()
        {
            string probe = Argument("-ctcc-qa-probe");
            // Preserve current CH02 end-state to restore cleanly after CH01-R adversarial tests
            var savedBefore = State.Copy();
            var isolatedDir = Path.Combine(qaFolder, "ch01r-adversarial");
            Directory.CreateDirectory(isolatedDir);

            // 1) Start clean CH01 in Layer B at Room 101 AP04
            var advSettings = new VisitSettings { volume = 0f, reduceMotion = true, captions = true, hintsEnabled = true };
            Apply(casework.Begin(scene.Pad("AP00").position, advSettings), "CH01-R begin");
            scene.interactables?.ResetSessionProps();
            if (scene.staffCrossDoor != null) scene.staffCrossDoor.isOpen = false;
            player.Teleport(scene.Pad("AP00"));
            Apply(casework.ReadLetter(player.Feet), "CH01-R SMS");
            Apply(casework.OpenGate(player.Feet), "CH01-R gate");
            player.Teleport(scene.Pad("AP02"));
            Apply(casework.AtHub(player.Feet, out _), "CH01-R enter B");
            player.Teleport(scene.Pad("AP02"));
            Apply(casework.Board(player.Feet), "CH01-R take board");
            Apply(casework.Observe(ObservationRegistry.F101Sheet, player.Feet, "LOC02"), "CH01-R observe F101");

            // 2) Non-lethal CH01 guarantee: zero Hunter/Gatekeeper activation
            Require((orderlyAi == null || !orderlyAi.IsActive) && (gatekeeperCameo == null || !gatekeeperCameo.IsActive),
                "CH01-R remains strictly non-lethal");

            // 3) Compact nonlinear loop reachability: LOC02 -> STAFF Nursing Alcove -> open DOOR-STAFF -> HALL -> AP04!
            Require(scene.staffCrossDoor != null && scene.staffCrossDoor.pivot != null, "CH01-R STAFF cross-door exists");
            Walk(new Vector3(0.05f, 0f, -1.2f));
            Walk(new Vector3(0.05f, 0f, 3.65f)); // Inside STAFF Nursing Alcove!
            Require(scene.interactables != null && scene.interactables.ToggleDrawer("DRAWER-NURSE", scene.atmosphere, instant: true), "CH01-R nurse drawer opens");
            yield return VerifyStaffReactionOrder();

            // Capture STAFF Nursing Alcove 1080p still (framing desk, open drawer, overflow notice & cross-door!)
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
            {
                var staffPose = new GameObject("QA_StaffAlcovePose").transform;
                staffPose.SetPositionAndRotation(new Vector3(-0.32f, 0.02f, 1.85f), Quaternion.Euler(0f, 34f, 0f));
                yield return CaptureHospitalFrame("ch01r-staff-alcove-B", staffPose, 0f);
                Destroy(staffPose.gameObject);
            }

            // Open STAFF cross-door and physically walk through into HALL -> Room 101 AP04!
            Require(scene.interactables.ToggleDoor("DOOR-STAFF", scene.atmosphere, instant: true), "CH01-R STAFF cross-door opens");
            scene.staffCrossDoor.isOpen = true;
            Walk(new Vector3(0.15f, 0f, 7.50f));
            Walk(new Vector3(1.80f, 0f, 7.50f));
            Walk(new Vector3(4.00f, 0f, 7.50f));
            Walk(new Vector3(4.00f, 0f, 4.75f));
            Walk(new Vector3(6.52f, 0f, 4.93f));
            Apply(casework.EnterRoom(player.Feet), "CH01-R enter Room 101 via nonlinear loop");

            // 4) Provenance, unknown ID, and layer violation adversarial checks
            Require(casework.Observe("OBS_UNKNOWN_999", player.Feet, "LOC03") != CommitResult.Applied, "Unknown observation ID rejected");
            Require(casework.Observe(ObservationRegistry.Ch02List, player.Feet, "LOC03") != CommitResult.Applied, "CH02 R observation rejected in Layer B");
            Require(casework.Observe(ObservationRegistry.Bed4Traces, player.Feet, "LOC01") != CommitResult.Applied, "Wrong location provenance rejected");

            // 5) Alternate clue discovery order: Comparison docs first (OBS101C, DRAWER-BED3 -> OBS101D), then OBS101B, then OBS101A!
            Apply(casework.Observe(ObservationRegistry.Bed2Transfer, player.Feet, "LOC03"), "OBS101C Bed-2 completed transfer");
            Require(scene.interactables.ToggleDrawer("DRAWER-BED3", scene.atmosphere, instant: true), "DRAWER-BED3 opens");
            Apply(casework.Observe(ObservationRegistry.Bed3Meds, player.Feet, "LOC03"), "OBS101D Bed-3 routine meds");
            Apply(casework.Observe(ObservationRegistry.StoppedClock0317, player.Feet, "LOC02"), "OBS_CLOCK_0317 FS01");
            Require(State.Happened("FS01"), "Stopped clock records FS01");

            // Capture grounded Bed 4 Pending state before refutation
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
            {
                var pPose = new GameObject("QA_Bed4Pose").transform;
                pPose.SetPositionAndRotation(new Vector3(9.45f, 0.02f, 5.45f), Quaternion.Euler(0f, 134f, 0f));
                yield return CaptureHospitalFrame("ch01r-bed4-pending-B", pPose, 0f);
                Destroy(pPose.gameObject);
            }

            // Observe OBS101B first (reverse of legacy A->B order!), then dual-sided flip + detail stage advancement!
            Apply(casework.Observe(ObservationRegistry.Bed4Card, player.Feet, "LOC03"), "OBS101B observed first");
            Require(State.FindObservation(ObservationRegistry.Bed4Card).stage == ObservationStage.Examined, "OBS101B starts Examined");
            yield return WaitForApplied(casework.MarkObservationDetailAsync(ObservationRegistry.Bed4Card, "BACK", flippedBack: true), "OBS101B back flip stage");
            Require(State.FindObservation(ObservationRegistry.Bed4Card).stage == ObservationStage.Interpreted &&
                    State.FindObservation(ObservationRegistry.Bed4Card).flippedBack, "OBS101B advances to Interpreted on flip");

            Require(!CorroborationService.HasCardTimestamp(State) && !CorroborationService.CanCorroborate0317(State) &&
                    casework.CrossCheck0317Exception(player.Feet, Observed0317Link()) != CommitResult.Applied,
                "03:17 remains locked after a normal inspection and flip, despite having the clock and shift context");
            foreach (string id in LanDirector.EncounterLineIds())
                Require(!string.IsNullOrEmpty(Line(id)) && voice.HasClip(id), "Lan encounter has dialogue and an imported clip: " + id);
            string encounter = BuildLanTranscript(record: false);
            Require(encounter.Contains("Lan  —  " + Line("CH01-D04c")) && !MayDeliver("CH01-D04b", null),
                "Lan transcript attributes the clock line to Lan and removes the retired reflection cue");
            var oldTranscript = State.Copy();
            oldTranscript.transcript.Clear();
            oldTranscript.transcript.Add(new TranscriptEntry("CH01-D04b", "Minh", "Câu thoại về bóng phản chiếu"));
            oldTranscript.transcript.Add(new TranscriptEntry("CH01-D04c", "Lan", "Câu thoại cũ về tấm kính"));
            RestoreJournal(oldTranscript);
            Require(StaffTranscriptCount("CH01-D04b") == 0 && StaffTranscriptCount("CH01-D04c") == 1 &&
                    voice.Transcript[voice.Transcript.Count - 1].Text == Line("CH01-D04c"),
                "Loading an older transcript drops the retired cue and displays Lan's updated line");
            RestoreJournal(State);
            var cardSpec = ObservationRegistry.Get(ObservationRegistry.Bed4Card);
            Require(!BuildLargePrintTranscript(cardSpec, readRaking: false).Contains("03g17") &&
                    !cardSpec.InterpretationNote.Contains("03g17"),
                "Card summaries and ordinary Large Print keep the time hidden until the impression is read");
            string card = BuildLargePrintTranscript(cardSpec, readRaking: true);
            Require(card.Contains("03g17") && !card.Contains("C01") && !card.Contains("Bé An") && !card.Contains("Khu C"),
                "LOG06-Q01: AS-05B still shows the time and keeps Chapter 2's reveal locked");
            Require(!MayDeliver("CH01-D19", "B05"), "CH01-D19 strictly locked before CH01-EX01 corroboration & G01");

            // Dual-sided 3D PhysicalInspector flip + Upgrade #6 raking-light ballpoint indentation reveal + interrupted close recovery!
            if (inspector != null && Image("ObsCard") != null && Image("ObsCard_Back") != null)
            {
                inspector.SetReducedMotion(true);
                bool rakingCallbackFired = false;
                inspector.Open(
                    null,
                    Image("ObsCard"),
                    1024f / 1360f,
                    ActiveVolume(),
                    Image("ObsCard_Back"),
                    BuildDetailTargetsFor(ObservationRegistry.Bed4Card),
                    observationId: ObservationRegistry.Bed4Card,
                    backIndentNormal: Image("ObsCard_Back_Indent_Normal"),
                    backIndentMask: Image("ObsCard_Back_Indent_Mask"),
                    onRakingReveal: () => rakingCallbackFired = true);
                Require(inspector.IsInspecting && inspector.HasBackSide && inspector.HasRakingIndent && !inspector.ShowingBack,
                    "Inspector opens dual-sided front with raking indentation maps");
                if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                {
                    yield return null;
                    yield return CaptureHospitalFrame("ch01r-inspector-obscard-front", player.transform, 0f);
                }
                inspector.Flip();
                Require(inspector.ShowingBack, "Inspector flips cleanly to back side");
                if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                {
                    yield return null;
                    yield return CaptureHospitalFrame("ch01r-inspector-obscard-back", player.transform, 0f);
                }
                // Tilt the reverse toward the light to reveal its 03g17 impression.
                inspector.TiltForRakingLight();
                float waitRaking = Time.realtimeSinceStartup + 0.35f;
                while (!inspector.RakingRevealed && Time.realtimeSinceStartup < waitRaking)
                    yield return null;
                Require(inspector.RakingRevealed && rakingCallbackFired && inspector.RakingRevealFactor >= 0.72f,
                    $"Upgrade #6 raking-light reveal failed: revealed={inspector.RakingRevealed} cb={rakingCallbackFired} factor={inspector.RakingRevealFactor:F2}");
                if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                {
                    yield return null;
                    yield return CaptureHospitalFrame("ch01r-raking-obscard-back", player.transform, 0f);
                }
                yield return WaitForApplied(casework.MarkObservationDetailAsync(ObservationRegistry.Bed4Card, CorroborationService.RakingDetailId, flippedBack: true), "Persist RAKING_INDENT");
                Require(CorroborationService.HasRakingIndent(State), "RAKING_INDENT detail persisted to observation record");
                inspector.FocusDetail(0);
                Require(inspector.ActiveDetailIndex == -1 && inspector.ShowingBack, "Front detail cannot flip back");
                inspector.Flip();
                inspector.FocusDetail(0);
                Require(inspector.ActiveDetailIndex == 0 && !inspector.ShowingBack, "Detail focus snaps cleanly to front hotspot");
                inspector.Close(); // Interrupted mid-inspection close!
                Require(!inspector.IsInspecting, "Interrupted inspection closes cleanly");
                dreadDirector?.ResetSeed(State.dreadSeed); // Keep deferred shift isolated for dedicated section-6 test
            }

            // Incomplete evidence test: OBS101B + OBS101C without OBS101A MUST fail with InsufficientEvidence
            player.Teleport(scene.Pad("AP04"));
            var missingA = new DeductionSubmission
            {
                formId = "F101",
                selectedEvidenceIds = new List<string> { ObservationRegistry.Bed4Card, ObservationRegistry.Bed2Transfer },
                claimId = HypothesisService.ClaimUnreliableZero
            };
            Require(casework.SubmitF101(FormStatus.Refuted, missingA, player.Feet, out var evalMissA) != CommitResult.Applied &&
                    evalMissA.Outcome == HypothesisOutcome.InsufficientEvidence &&
                    evalMissA.Message.Contains("từng được dùng"), "Card and comparison document cannot establish that the bed was used");

            // Now observe OBS101A (physical trace) to complete the complementary Bed-4 pair
            Apply(casework.Observe(ObservationRegistry.Bed4Traces, player.Feet, "LOC03"), "OBS101A observed second");

            var withoutComparison = State.Copy();
            withoutComparison.observed.Remove(ObservationRegistry.Bed2Transfer);
            withoutComparison.observationRecords.RemoveAll(record => record.id == ObservationRegistry.Bed2Transfer);
            var cardMark = new EvidenceAnnotation(ObservationRegistry.Bed4Card, F101Annotations.Receiver);
            var annotations = new F101Annotations
            {
                marks = new List<EvidenceAnnotation> { new(ObservationRegistry.F101Sheet, F101Annotations.Zero), cardMark },
                linkFrom = cardMark,
                linkTo = new EvidenceAnnotation(ObservationRegistry.Bed4Traces, F101Annotations.TraceGroup)
            };
            var annotatedRefutation = new DeductionSubmission
            {
                selectedEvidenceIds = new List<string> { ObservationRegistry.F101Sheet, ObservationRegistry.Bed4Card, ObservationRegistry.Bed4Traces },
                claimId = HypothesisService.ClaimUnreliableZero,
                annotationVersion = F101Annotations.Version,
                annotations = annotations
            };
            Require(HypothesisService.EvaluateF101(withoutComparison, FormStatus.Refuted, annotatedRefutation).Accepted,
                "Annotated refutation does not require the Bed-2 comparison");
            annotatedRefutation.annotations.linkTo = new EvidenceAnnotation(ObservationRegistry.Bed4Traces, "unknown");
            Require(!HypothesisService.EvaluateF101(withoutComparison, FormStatus.Refuted, annotatedRefutation).Accepted,
                "Unknown linked field is rejected by the hypothesis service");

            // Test Unexamined stage rejection directly on copied state
            var unexaminedState = State.Copy();
            unexaminedState.FindObservation(ObservationRegistry.Bed4Traces).stage = ObservationStage.Discovered;
            var corePairValid = new DeductionSubmission
            {
                formId = "F101",
                selectedEvidenceIds = new List<string> { ObservationRegistry.Bed4Traces, ObservationRegistry.Bed4Card },
                claimId = HypothesisService.ClaimUnreliableZero
            };
            Require(HypothesisService.EvaluateF101(unexaminedState, FormStatus.Refuted, corePairValid).Outcome == HypothesisOutcome.UnexaminedEvidence,
                "Discovered-only unexamined evidence rejected");

            // Test tampered provenance rejection (OBS101A claiming Reality.R!)
            var tamperedLayerState = State.Copy();
            tamperedLayerState.FindObservation(ObservationRegistry.Bed4Traces).layer = Reality.R;
            Require(HypothesisService.EvaluateF101(tamperedLayerState, FormStatus.Refuted, corePairValid).Outcome == HypothesisOutcome.InvalidProvenance,
                "Tampered Layer R provenance rejected");

            // Test Unknown ID in submission
            var unknownSub = new DeductionSubmission
            {
                formId = "F101",
                selectedEvidenceIds = new List<string> { ObservationRegistry.Bed4Traces, ObservationRegistry.Bed4Card, "FAKE_ID" },
                claimId = HypothesisService.ClaimUnreliableZero
            };
            Require(casework.SubmitF101(FormStatus.Refuted, unknownSub, player.Feet, out var evalUnk) != CommitResult.Applied &&
                    evalUnk.Outcome == HypothesisOutcome.UnknownEvidenceId, "Unknown evidence ID rejected");

            // Test Irrelevant Bed-3 medication log & stopped clock rejection
            var irrelSub = new DeductionSubmission
            {
                formId = "F101",
                selectedEvidenceIds = new List<string> { ObservationRegistry.Bed4Traces, ObservationRegistry.Bed4Card, ObservationRegistry.Bed3Meds },
                claimId = HypothesisService.ClaimUnreliableZero
            };
            Require(casework.SubmitF101(FormStatus.Refuted, irrelSub, player.Feet, out var evalIrr) != CommitResult.Applied &&
                    evalIrr.Outcome == HypothesisOutcome.IrrelevantEvidence, "Irrelevant Bed-3 medication record rejected");

            var observationOnly = corePairValid.Clone();
            observationOnly.claimId = HypothesisService.ClaimBlankSignature;
            Require(casework.SubmitF101(FormStatus.Refuted, observationOnly, player.Feet, out var evalFact) != CommitResult.Applied &&
                    evalFact.Outcome == HypothesisOutcome.UnsupportedConclusion &&
                    evalFact.Message.Contains("đúng là còn trống") && evalFact.Message.Contains("số tổng hợp") &&
                    State.f101.revision == 0 && State.f101History.Count == 0,
                "A true observation alone does not explain the total or leave a committed revision");

            // Distinguish the gaps about presence, intent and a completed handover.
            var overclaimPresent = new DeductionSubmission
            {
                formId = "F101",
                selectedEvidenceIds = new List<string> { ObservationRegistry.Bed4Traces, ObservationRegistry.Bed4Card },
                claimId = HypothesisService.ClaimPatientPresent
            };
            Require(casework.SubmitF101(FormStatus.Refuted, overclaimPresent, player.Feet, out var evalPres) != CommitResult.Applied &&
                    evalPres.Outcome == HypothesisOutcome.UnsupportedConclusion && evalPres.Message.Contains("ở đâu"),
                "Patient-present overclaim requires evidence of whereabouts");

            var overclaimFalsify = new DeductionSubmission
            {
                formId = "F101",
                selectedEvidenceIds = new List<string> { ObservationRegistry.Bed4Traces, ObservationRegistry.Bed4Card },
                claimId = HypothesisService.ClaimFalsification
            };
            var falsifyEval = HypothesisService.EvaluateF101(State, FormStatus.Refuted, overclaimFalsify);
            Require(probe != "invalid-reasoning" && !falsifyEval.Accepted &&
                falsifyEval.Outcome == HypothesisOutcome.UnsupportedConclusion && falsifyEval.Message.Contains("đã biết gì"),
                "CH01-R fault-injection guard: unsupported falsification overclaim must be rejected");

            var contraSub = new DeductionSubmission
            {
                formId = "F101",
                selectedEvidenceIds = new List<string> { ObservationRegistry.Bed4Traces, ObservationRegistry.Bed4Card },
                claimId = HypothesisService.ClaimClericalOk
            };
            Require(casework.SubmitF101(FormStatus.Refuted, contraSub, player.Feet, out var evalContra) != CommitResult.Applied &&
                    evalContra.Outcome == HypothesisOutcome.ContradictsDecision && evalContra.Message.Contains("đã hoàn tất"),
                "Clerical explanation requires evidence that the handover was actually completed");

            // Verify progressive hint unlocked cleanly after >= 2 failed attempts, while Unverified STILL permitted!
            Require(PuzzleService.ShouldShowFieldHint(State, HypothesisService.F101PuzzleId), "F101 progressive hint activates after failed attempts");
            Apply(casework.SubmitF101(FormStatus.Unverified, null, player.Feet, out var unvEval), "Unverified succeeds even after failed refutation");
            Require(probe != "broken-progression" && unvEval.Accepted && GateService.G01(State),
                "CH01-R fault-injection guard: Unverified must open G01 without blocking CH01 progression");

            // Capture all 3 consequential F101 visual outcomes in Layer B!
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
            {
                var bed4ShotPose = new GameObject("QA_F101OutcomePose").transform;
                bed4ShotPose.SetPositionAndRotation(new Vector3(9.45f, 0.02f, 5.45f), Quaternion.Euler(0f, 134f, 0f));
                yield return CaptureHospitalFrame("ch01r-f101-unverified-B", bed4ShotPose, 0f);

                player.Teleport(scene.Pad("AP04"));
                Apply(casework.SubmitF101(FormStatus.Affirmed, null, player.Feet, out _), "Revision B->A");
                Require(scene.deniedBay.activeInHierarchy && !scene.refutedBay.activeInHierarchy, "Affirmed activates solid deniedBay");
                yield return CaptureHospitalFrame("ch01r-f101-affirmed-B", bed4ShotPose, 0f);

                // Valid Refuted with core pair + authentic comparison & context docs (OBS101A + OBS101B + OBS101C + OBS_SHIFT_LOG)
                player.Teleport(scene.Pad("AP04"));
                var richRefutation = new DeductionSubmission
                {
                    formId = "F101",
                    selectedEvidenceIds = new List<string>
                    {
                        ObservationRegistry.Bed4Traces,
                        ObservationRegistry.Bed4Card,
                        ObservationRegistry.Bed2Transfer,
                        ObservationRegistry.ShiftGuide
                    },
                    claimId = HypothesisService.ClaimUnreliableZero
                };
                Apply(casework.SubmitF101(FormStatus.Refuted, richRefutation, player.Feet, out var richEval), "Revision A->C rich evidence");
                Require(richEval.Accepted && scene.refutedBay.activeInHierarchy && !scene.deniedBay.activeInHierarchy &&
                        State.FindObservation(ObservationRegistry.Bed4Traces).stage == ObservationStage.Corroborated &&
                        State.FindObservation(ObservationRegistry.Bed4Card).stage == ObservationStage.Corroborated &&
                        State.f101.firstDecision == FormStatus.Unverified && State.f101.revision == 3 &&
                        State.f101History.Count == 3 && State.evidence.Count == 1,
                        "Refuted rich evidence corroborated, firstDecision immutable, CH01 evidence count stays 1");
                yield return CaptureHospitalFrame("ch01r-f101-refuted-B", bed4ShotPose, 0f);
                Destroy(bed4ShotPose.gameObject);
            }

            yield return VerifyF101Revisions();

            // 6) Persistent one-time AmbientDreadDirector event + Upgrade #2 DeferredMicroShift + modal suppression + save reload survival!
            Require(dreadDirector != null, "AmbientDreadDirector active");
            dreadDirector.ResetSeed(State.dreadSeed);
            dreadDirector.NotifyInspectorOpened(ObservationRegistry.Bed4Card);
            Require(dreadDirector.HasPendingMicroShift, "Upgrade #2 NotifyInspectorOpened queues a DeferredMicroShift");
            ui.Show("QA", "Modal suppression", "Testing dread suppression while modal open.", new[] { new ActionChoice("OK", () => ui.Close()) }, () => ui.Close());
            dreadDirector.NotifyInspectorClosed(ObservationRegistry.Bed4Card);
            Require(dreadDirector.HasPendingMicroShift, "Upgrade #2 NotifyInspectorClosed arms DeferredMicroShift");
            Require(!dreadDirector.TryTriggerDeterministicForQA(DreadCatalog.B06ClockTick), "Dread strictly suppressed while modal is open");
            float modalHold = Time.realtimeSinceStartup + 0.50f;
            while (Time.realtimeSinceStartup < modalHold) yield return null;
            Require(dreadDirector.HasPendingMicroShift && !State.HasDreadEvent(DreadCatalog.ShiftTubeBlackout),
                "DeferredMicroShift remains suppressed while modal is open");
            ui.Close();
            yield return Until(() => !busy && (State.HasDreadEvent(DreadCatalog.ShiftTubeBlackout) ||
                State.HasDreadEvent(DreadCatalog.ShiftCurtainCreep) || State.HasDreadEvent(DreadCatalog.ShiftHallChair)),
                "Upgrade #2 armed DeferredMicroShift executes automatically after modal/inspector closes");
            scene.motion?.ClearTubeBlackout();
            Require(dreadDirector.TryTriggerDeterministicForQA(DreadCatalog.B06ClockTick), "Dread triggers cleanly when unsuppressed");
            yield return Until(() => !busy && State.HasDreadEvent(DreadCatalog.B06ClockTick), "Dread event persisted to state");
            foreach (string id in new[] { DreadCatalog.B02GlassShadow, DreadCatalog.B04TrolleyStop, DreadCatalog.B04TrolleyNear,
                DreadCatalog.B04CurtainBreath, DreadCatalog.B05FanDecelerate, DreadCatalog.B05DistantSteps,
                DreadCatalog.ShiftCurtainCreep, DreadCatalog.ShiftHallChair })
            {
                if (dreadDirector.AmbientBudgetExhausted) break;
                if (State.HasDreadEvent(id)) continue;
                Require(dreadDirector.TryTriggerDeterministicForQA(id), "Fill ambient budget: " + id);
                yield return Until(() => !busy && State.HasDreadEvent(id), "Persist ambient fixture: " + id);
            }
            Require(dreadDirector.AmbientBudgetExhausted, "Long exploration fixture exhausted the ambient budget");
            dreadDirector.NotifyF101Signing();
            int ambientEvents = dreadDirector.EventsFiredThisSession;
            Require(dreadDirector.TryPlayF101Reaction() && dreadDirector.EventsFiredThisSession == ambientEvents,
                "Production F101 reaction plays during signing quiet window even if the ambient budget is exhausted");
            yield return Until(() => !busy && State.HasDreadEvent(DreadCatalog.F101AffirmedSuffocation), "F101AffirmedSuffocation persisted to state");
            Require(transaction.Restore(SaveSlot.Auto, out _) == SaveRead.Valid && State.HasDreadEvent(DreadCatalog.B06ClockTick) &&
                    State.HasDreadEvent(DreadCatalog.F101AffirmedSuffocation) &&
                    (State.HasDreadEvent(DreadCatalog.ShiftTubeBlackout) || State.HasDreadEvent(DreadCatalog.ShiftCurtainCreep) || State.HasDreadEvent(DreadCatalog.ShiftHallChair)),
                "Persisted dread & micro-shift events survive save/reload");
            Require(!dreadDirector.TryTriggerDeterministicForQA(DreadCatalog.B06ClockTick), "One-time dread event never replays after reload");
            Require(!dreadDirector.TryPlayF101Reaction(), "Signed F101 reaction does not replay after reload");

            // 6b) Upgrade #5: Optional 03:17 Corroboration Branch (OBS_CLOCK_0317 + OBS101B + shift context -> CH01-EX01 -> CH01-D19!)
            Require(CorroborationService.CanCorroborate0317(State) && !CorroborationService.IsCorroborated(State),
                "Upgrade #5 03:17 corroboration eligible once clock, AS-05B, and shift context observed");
            long beforeWrongTime = State.sequence;
            Require(casework.CrossCheck0317Exception(player.Feet, new TimelineConnection
                    { left = ObservationRegistry.Bed2Transfer, right = ObservationRegistry.Bed4Card, context = ObservationRegistry.ShiftGuide }) != CommitResult.Applied &&
                    State.sequence == beforeWrongTime && !CorroborationService.IsCorroborated(State),
                "Wrong time leaves CH01-EX01 locked and does not write a save");
            Apply(casework.CrossCheck0317Exception(player.Feet, Observed0317Link()), "Upgrade #5 commit CH01-EX01");
            Require(CorroborationService.IsCorroborated(State) && State.Happened(CorroborationService.Exception0317Event) &&
                    State.FindObservation(ObservationRegistry.StoppedClock0317).stage == ObservationStage.Corroborated &&
                    State.evidence.Count == 1 && voice.HasClip("CH01-D19", "B05") && !string.IsNullOrEmpty(LineAtBeat("CH01-D19", 1, "B05")),
                "Upgrade #5 CH01-EX01 persists cleanly, unlocks CH01-D19 at B05, and keeps CH01 evidence.Count == 1");
            Require(transaction.Restore(SaveSlot.Auto, out _) == SaveRead.Valid && CorroborationService.IsCorroborated(State),
                "Upgrade #5 CH01-EX01 survives save/reload");

            // 6c) Upgrade #8: True analog hold-and-drag on CURTAIN-101 and DOOR-101 without snap-back!
            if (scene.interactables != null)
            {
                scene.interactables.ResetCurtainPose();
                Require(scene.interactables.BeginDrag("CURTAIN-101"), "Analog curtain drag begins");
                for (int i = 0; i < 6; i++) scene.interactables.UpdateDrag(new Vector2(28f, 0f), scene.atmosphere);
                scene.interactables.EndDrag(scene.atmosphere);
                for (int i = 0; i < 6; i++) yield return null;
                float curtainMid = scene.interactables.CurtainOpenness;
                Require(curtainMid > 0.15f && curtainMid < 0.85f, $"Upgrade #8 analog curtain holds intermediate openness without snap-back: {curtainMid:F2}");
                scene.interactables.ResetCurtainPose();

                if (scene.roomDoor?.pivot != null)
                {
                    scene.interactables.MoveDoor("DOOR-101", scene.roomDoor.pivot, 0f, true, scene.atmosphere);
                    Require(scene.interactables.BeginDrag("DOOR-101"), "Analog door drag begins");
                    for (int i = 0; i < 6; i++) scene.interactables.UpdateDrag(new Vector2(22f, 0f), scene.atmosphere);
                    scene.interactables.EndDrag(scene.atmosphere);
                    for (int i = 0; i < 6; i++) yield return null;
                    float doorMid = scene.interactables.GetDragOpenness01("DOOR-101");
                    Require(doorMid > 0.12f && doorMid < 0.85f, $"Upgrade #8 analog Door-101 holds cracked peek angle without snap-back: {doorMid:F2}");
                    scene.interactables.MoveDoor("DOOR-101", scene.roomDoor.pivot, scene.roomDoor.openAngle, true, scene.atmosphere);
                }
            }

            // 7) Reality.R independence: collapse back to R and verify deniedBay/refutedBay are strictly inactive and CH02 clues untouched!
            player.Teleport(scene.Pad("AP02"));
            Apply(casework.AtHub(player.Feet, out _), "CH01-R collapse to R");
            Require(State.layer == Reality.R && !scene.deniedBay.activeInHierarchy && !scene.refutedBay.activeInHierarchy &&
                    !MayDeliver("CH01-D16b", "B06") && !MayDeliver("CH04-D06", "B06"),
                    "Reality.R remains completely independent of F101 and canon reveal guards hold");

            // Restore pre-test CH02 snapshot so VerifyRender + VerifyScalableM3AndCh03 continue seamlessly!
            Apply(transaction.Commit(savedBefore, scene.Pad("AP05").position, warp: false, geometryChanges: true), "Restore post-CH02 snapshot");
            player.Teleport(scene.Pad("AP05"));
            verification.Add("CH01-R ADVERSARIAL PASS: Lan VO and old transcripts, LOG06-Q01 card content, 03:17 inspection and answer validation, D19 guard, F101 reaction at exhausted ambient budget, once-only cues after reload, and independent Reality.R progression.");
        }

        private IEnumerator VerifyCh01WalkthroughSuite()
        {
            var savedBefore = State.Copy();
            var wtSettings = new VisitSettings { volume = 0f, reduceMotion = true, captions = true, hintsEnabled = true };
            Apply(casework.Begin(scene.Pad("AP00").position, wtSettings), "WT begin");
            scene.interactables?.ResetSessionProps();
            player.Teleport(scene.Pad("AP00"));

            // 1) B01: Multi-layer E10a, key extraction, wrong-key rejection, SMS gating, single E10a evidence, wind sign knock
            Require(InventoryService.HasPhysical(State, BackpackCatalog.Flashlight) && State.evidence.Count == 1 && State.Has("E10a"),
                "WT B01 starts with flashlight and single E10a evidence");
            player.SetTorchEnabled(true);
            Require(player.IsTorchOn && player.TorchIntensity >= 32f && player.TorchRange >= 30f,
                $"WT flashlight ON delivers boosted beam intensity={player.TorchIntensity:F1}, range={player.TorchRange:F1}");
            player.SetTorchEnabled(false);
            Require(!player.IsTorchOn && player.TorchIntensity == 0f, "WT flashlight toggles OFF cleanly");
            yield return WaitForApplied(casework.InspectE10aLayerAsync(1, "LAN_LETTER", markSmsRead: false), "WT E10a tab 1");
            Require(State.e10aLastTab == 1 && !State.Happened("E10a.SMSRead"), "WT E10a remembers tab without auto-reading SMS");
            yield return WaitForApplied(casework.InspectE10aLayerAsync(4, "KEYS_EXTRACTED", markSmsRead: false, extractKeys: true), "WT extract 2 keys");
            Require(InventoryService.HasPhysical(State, BackpackCatalog.GateKey) &&
                    InventoryService.HasPhysical(State, BackpackCatalog.WingCKey) &&
                    State.evidence.Count == 1 && State.Has("E10a"), "WT extracting keys keeps CH01 evidence.Count == 1");

            // Missing SMS feedback (NOT wrong key!) even when holding KEY_GATE
            var noSmsRes = WorldUseResolver.Resolve(State, BackpackCatalog.GateKey, "GATE-CONG");
            Require(noSmsRes.Outcome == WorldUseOutcome.NeedSmsBeforeGate, "WT KEY_GATE before SMS reports missing SMS");
            // Wrong key (KEY_WING_C) rejected and retained!
            yield return WaitForApplied(casework.InspectE10aLayerAsync(3, "BAY_SMS", markSmsRead: true), "WT read SMS");
            yield return WaitForApplied(casework.EquipItemAsync(BackpackCatalog.WingCKey), "WT equip KEY_WING_C");
            var wrongKeyRes = WorldUseResolver.Resolve(State, BackpackCatalog.WingCKey, "GATE-CONG");
            Require(wrongKeyRes.Outcome == WorldUseOutcome.WrongKeyForOuterGate &&
                    InventoryService.HasPhysical(State, BackpackCatalog.WingCKey), "WT KEY_WING_C rejected and retained");
            yield return WaitForApplied(casework.EquipItemAsync(BackpackCatalog.GateKey), "WT equip KEY_GATE");
            Require(WorldUseResolver.Resolve(State, BackpackCatalog.GateKey, "GATE-CONG").Outcome == WorldUseOutcome.UnlockOuterGate,
                "WT KEY_GATE resolves to UnlockOuterGate after SMS");
            Apply(casework.OpenGate(player.Feet), "WT open gate");
            Require(dreadDirector.TryTriggerB01SignKnock(), "WT B01 demolition sign wind knock fires once");
            yield return Until(() => !busy && State.HasDreadEvent(DreadCatalog.B01SignKnock), "WT B01 sign knock persisted");
            Require(!dreadDirector.TryTriggerB01SignKnock(), "WT B01 sign knock does not replay");

            // 2) B02: Glass study persistence
            yield return WaitForApplied(casework.RecordDreadEventAsync(DreadCatalog.B02GlassStudied), "WT B02 glass studied");
            Require(State.HasDreadEvent(DreadCatalog.B02GlassStudied), "WT B02 glass studied persisted");

            // 3) B03: 2-phase Lan encounter -> CH01-T03 does NOT auto-set CH01-T04; cinematic + safe skip!
            player.Teleport(scene.Pad("AP02"));
            Apply(casework.AtHub(player.Feet, out _), "WT enter B (Pha 1)");
            Require(State.layer == Reality.B && State.Happened("CH01-T03") && !State.Happened("CH01-T04") &&
                    !State.Happened("CH01-T05") && !State.Observed(ObservationRegistry.StoppedClock0317),
                "WT Pha 1 R->B does NOT auto-commit CH01-T04, BOARD, or OBS_CLOCK_0317");
            yield return WaitForApplied(casework.RecordLanNoticedAsync(), "WT Pha 2 Lan noticed");
            Require(State.Happened("CH01-LAN-NOTICED") && !State.Happened("CH01-T04"), "WT Pha 2 keeps CH01-T04 player-initiated");

            bool cineDone = false;
            Require(lanDirector.StartCinematicEncounter(LanEncounterLine, skipOpeningGreeting: true, reducedMotion: true,
                onComplete: () => cineDone = true), "WT Pha 3 Lan cinematic starts cleanly");
            Require(lanDirector.IsCinematicActive && player.ExternalCameraControl, "WT cinematic owns camera safely");
            yield return null;
            lanDirector.RequestSkipCinematic();
            yield return Until(() => cineDone && !lanDirector.IsCinematicActive && !player.ExternalCameraControl,
                "WT cinematic skip restores camera cleanly");
            yield return WaitForApplied(casework.CompleteLanCinematicAsync(), "WT commit CH01-T04");
            Require(State.Happened("CH01-T04") && !State.Happened("CH01-T05") && !State.Observed(ObservationRegistry.StoppedClock0317) &&
                    StaffTranscriptCount("CH01-D07") > 0 &&
                    !MayDeliver("CH01-D16b", "B03") && !MayDeliver("CH04-D06", "B03"),
                "WT cinematic skip records canonical transcript, keeps CH04 guards locked, and never auto-credits clock/BOARD");

            // B03 Lan drawer look once
            yield return WaitForApplied(casework.RecordDreadEventAsync(DreadCatalog.B03LanDrawerLook), "WT B03 drawer look");
            Require(State.HasDreadEvent(DreadCatalog.B03LanDrawerLook), "WT B03 drawer look persisted");

            // Pha 4 BOARD pickup
            Apply(casework.Board(player.Feet), "WT Pha 4 accept BOARD");
            Require(State.Happened("CH01-T05") && InventoryService.HasPhysical(State, BackpackCatalog.BoardAssignment),
                "WT BOARD stored in Backpack as Layer B item");

            // STAFF DRAWER-NURSE quiet latch vs force-pull reaction check
            Require(scene.interactables.ToggleDrawer("DRAWER-NURSE", scene.atmosphere, instant: true, quiet: true), "WT quiet latch opens DRAWER-NURSE");
            Require(scene.interactables.IsDrawerOpen("DRAWER-NURSE"), "WT quiet drawer opened cleanly");
            Require(!scene.interactables.ToggleDrawer("DRAWER-NURSE", scene.atmosphere, instant: true), "WT close DRAWER-NURSE");
            dreadDirector.ResetSeed(State.dreadSeed);
            Require(dreadDirector.TryTriggerDeterministicForQA(DreadCatalog.B04TrolleyStop), "WT stage-1 trolley active");
            yield return Until(() => !busy && State.HasDreadEvent(DreadCatalog.B04TrolleyStop), "WT stage-1 trolley persisted");
            Require(dreadDirector.NotifyStaffDrawerForcePull(), "WT force pull triggers once-only staff trolley halt");
            yield return Until(() => !busy && State.HasDreadEvent(DreadCatalog.B04StaffTrolleyHalt), "WT staff trolley halt persisted");
            Require(!dreadDirector.NotifyStaffDrawerForcePull(), "WT staff trolley halt does not replay");

            // 4) B04/B05: Stow Layer B paper, clip at AN02, P-SC05 silhouette once, F101 C + B->R reconciliation (3 cases)
            player.Teleport(scene.Pad("AP04"));
            Apply(casework.EnterRoom(player.Feet), "WT enter Room 101");
            Require(!dreadDirector.TryTriggerDeterministicForQA(DreadCatalog.B05CurtainSilhouette),
                "WT P-SC05 blocked before OBS101B examined");
            Apply(casework.Observe(ObservationRegistry.Bed4Card, player.Feet, "LOC03"), "WT examine OBS101B");
            Apply(casework.Observe(ObservationRegistry.Bed4Traces, player.Feet, "LOC03"), "WT examine OBS101A");
            yield return WaitForApplied(casework.AcquireItemAsync(BackpackCatalog.PaperObs101B, "LOC03"), "WT stow AS-05B");
            yield return WaitForApplied(casework.EquipItemAsync(BackpackCatalog.PaperObs101B), "WT equip AS-05B");
            handPresenter?.SyncState(State, scene, hideViewModel: false);
            Require(handPresenter.EquippedItemDefId == BackpackCatalog.PaperObs101B, "WT AS-05B held in 3D hand rig");
            Require(WorldUseResolver.Resolve(State, BackpackCatalog.PaperObs101B, "COMPARE_CLIP_AN02").Outcome == WorldUseOutcome.PlacePaperOnAnchor,
                "WT resolver places AS-05B on AN02 clip");
            yield return WaitForApplied(casework.PlaceItemAtAnchorAsync(BackpackCatalog.PaperObs101B, WorldAnchors.An02CompareClip), "WT clip AS-05B at AN02");
            handPresenter?.SyncState(State, scene, hideViewModel: false);
            Require(handPresenter.PlacedAn02ItemDefId == BackpackCatalog.PaperObs101B, "WT AN02 wall clip visibly holds AS-05B");

            // Peak Horror #1 P-SC05 fires once in Layer B after OBS101B examined
            dreadDirector.ResetSeed(State.dreadSeed);
            Require(dreadDirector.TryTriggerDeterministicForQA(DreadCatalog.B05CurtainSilhouette), "WT P-SC05 fires once after OBS101B");
            yield return Until(() => !busy && State.HasDreadEvent(DreadCatalog.B05CurtainSilhouette), "WT P-SC05 persisted");
            Require(!dreadDirector.TryTriggerDeterministicForQA(DreadCatalog.B05CurtainSilhouette), "WT P-SC05 never replays");

            // Also verify 4 Quick-Slots (auto-assign, toggle, save/load, validation), 2-slot physical desks (STAFF/RECEPTION/WARD101),
            // zone unload/reload persistence, "Tìm đồ đã đặt" remote recovery, and BackpackStage3D 50-cycle zero-teleport stress!
            Require(State.quickSlots != null && State.quickSlots.Contains(BackpackCatalog.GateKey) &&
                    State.quickSlots.Contains(BackpackCatalog.WingCKey),
                "WT QuickSlots auto-assigned KEY_GATE and KEY_WING_C on extraction");
            yield return WaitForApplied(casework.ToggleQuickSlotAsync(BackpackCatalog.WingCKey), "WT unpin WingCKey quick-slot");
            Require(!State.quickSlots.Contains(BackpackCatalog.WingCKey), "WT ToggleQuickSlot unpins item");
            yield return WaitForApplied(casework.ToggleQuickSlotAsync(BackpackCatalog.WingCKey), "WT re-pin WingCKey quick-slot");
            Require(State.quickSlots.Contains(BackpackCatalog.WingCKey) && InventoryService.ValidState(State),
                "WT ToggleQuickSlot re-pins item cleanly");

            // Test 2-slot physical comparison desks (STAFF + RECEPTION + WARD101) & remote recovery
            Apply(casework.Observe(ObservationRegistry.Bed2Transfer, player.Feet, "LOC03"), "WT examine OBS101C");
            Apply(casework.Observe(ObservationRegistry.ShiftGuide, player.Feet, "STAFF"), "WT examine OBS_SHIFT_LOG");
            yield return WaitForApplied(casework.AcquireItemAsync(BackpackCatalog.PaperObs101C, "LOC03"), "WT stow OBS101C in bag");
            yield return WaitForApplied(casework.AcquireItemAsync(BackpackCatalog.PaperShiftLog, "STAFF"), "WT stow AS-05 in bag");
            yield return WaitForApplied(casework.PlaceItemAtAnchorAsync(BackpackCatalog.PaperObs101C, WorldAnchors.StaffDeskLeft), "WT place OBS101C on STAFF slot L");
            yield return WaitForApplied(casework.PlaceItemAtAnchorAsync(BackpackCatalog.PaperShiftLog, WorldAnchors.StaffDeskRight), "WT place AS-05 on STAFF slot R");
            handPresenter?.SyncState(State, scene, hideViewModel: false);
            Require(handPresenter.PlacedItemAt(WorldAnchors.StaffDeskLeft) == BackpackCatalog.PaperObs101C &&
                    handPresenter.PlacedItemAt(WorldAnchors.StaffDeskRight) == BackpackCatalog.PaperShiftLog &&
                    InventoryService.GetAllPlaced(State).Count == 3 &&
                    PocketNoteBuilder.BuildEpistemicSummary(State).Contains("Bàn điều dưỡng STAFF"),
                "WT 2-slot STAFF desk renders both papers simultaneously and lists them in Pocket Note");

            // Zone unload/reload persistence of placed comparison items + remote recovery
            string wardSceneName = scene.f101WallRenderer.gameObject.scene.name;
            yield return scene.UnloadZoneAsync(wardSceneName);
            yield return scene.LoadZoneAsync(wardSceneName);
            handPresenter?.SyncState(State, scene, hideViewModel: false);
            Require(InventoryService.GetPlacedAt(State, WorldAnchors.An02CompareClip)?.itemDefId == BackpackCatalog.PaperObs101B &&
                    handPresenter.PlacedItemAt(WorldAnchors.StaffDeskRight) == BackpackCatalog.PaperShiftLog,
                "WT placed desk & clip items survive LOC03 additive unload/reload");
            yield return WaitForApplied(casework.RetrieveItemFromAnchorAsync(WorldAnchors.StaffDeskRight), "WT remote retrieve from STAFF slot R");
            Require(InventoryService.GetPlacedAt(State, WorldAnchors.StaffDeskRight) == null &&
                    InventoryService.FindByDef(State, BackpackCatalog.PaperShiftLog)?.status == BackpackItemStatus.InBag,
                "WT remote recovery returns placed paper cleanly to Backpack");
            yield return WaitForApplied(casework.PlaceItemAtAnchorAsync(BackpackCatalog.PaperShiftLog, WorldAnchors.ReceptionDeskLeft), "WT place AS-05 on RECEPTION slot L");
            yield return WaitForApplied(casework.PlaceItemAtAnchorAsync(BackpackCatalog.PaperObs101C, WorldAnchors.Ward101TableLeft), "WT place OBS101C on WARD101 table L");

            // BackpackStage3D 50 open/close cycles stress check (zero camera teleport)
            if (backpackStage != null)
            {
                Vector3 camBefore = player.Eye.transform.position;
                for (int cycle = 0; cycle < 50; cycle++)
                {
                    backpackStage.Open(State, (BackpackCompartment)(cycle % 3), BackpackCatalog.GateKey, showEcho: cycle == 0);
                    Require(backpackStage.IsOpen, "WT BackpackStage3D opens cleanly");
                    backpackStage.Close();
                    Require(!backpackStage.IsOpen, "WT BackpackStage3D closes cleanly");
                }
                Require(Vector3.Distance(camBefore, player.Eye.transform.position) < 0.001f,
                    "WT BackpackStage3D 50 cycles preserve player camera position with zero teleport");
            }

            Apply(casework.SubmitF101(FormStatus.Refuted, new DeductionSubmission
            {
                formId = "F101",
                selectedEvidenceIds = new List<string> { ObservationRegistry.Bed4Traces, ObservationRegistry.Bed4Card },
                claimId = HypothesisService.ClaimUnreliableZero
            }, player.Feet, out var wtC), "WT F101 Refuted");
            Require(wtC.Accepted && GateService.G01(State), "WT F101 C accepted");

            // 5) B06: Atomic B->R reconciliation (Placed AS-05B + STAFF/RECEPTION/WARD101 desks + BOARD -> MemoryRef) AND rollback on save fail!
            player.Teleport(scene.Pad("AP02"));
            long beforeFailSwap = State.sequence;
            using (var lockFile = new FileStream(saves.SlotPath(SaveSlot.Auto) + ".lock", FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                Require(casework.AtHub(player.Feet, out _) == CommitResult.SaveUnavailable, "WT B->R swap fails cleanly when save locked");
                Require(State.layer == Reality.B && State.sequence == beforeFailSwap &&
                        InventoryService.GetPlacedAt(State, WorldAnchors.An02CompareClip)?.itemDefId == BackpackCatalog.PaperObs101B,
                    "WT failed B->R swap rolls back without losing physical Layer B paper or AN02 clip state");
            }
            Apply(casework.AtHub(player.Feet, out _), "WT B->R collapse");
            handPresenter?.SyncState(State, scene, hideViewModel: false);
            Require(State.layer == Reality.R && State.Happened("CH01-T08") && State.evidence.Count == 1 &&
                    InventoryService.FindByDef(State, BackpackCatalog.PaperObs101B)?.status == BackpackItemStatus.MemoryRef &&
                    InventoryService.FindByDef(State, BackpackCatalog.PaperObs101C)?.status == BackpackItemStatus.MemoryRef &&
                    InventoryService.FindByDef(State, BackpackCatalog.PaperShiftLog)?.status == BackpackItemStatus.MemoryRef &&
                    InventoryService.FindByDef(State, BackpackCatalog.BoardAssignment)?.status == BackpackItemStatus.MemoryRef &&
                    InventoryService.GetEquipped(State) == null &&
                    InventoryService.GetAllPlaced(State).Count == 0 &&
                    !State.quickSlots.Contains(BackpackCatalog.PaperObs101B) &&
                    !State.quickSlots.Contains(BackpackCatalog.PaperShiftLog) &&
                    string.IsNullOrEmpty(handPresenter.PlacedAn02ItemDefId),
                "WT B->R atomically reconciles ALL Placed/Equipped/InBag Layer B papers across all 4 surfaces and prunes Layer-B quick-slots");

            // CRITICAL BUG FIX VERIFICATION: DREAD_B06_CLOCK_TICK strictly blocked in Layer R!
            Require(!dreadDirector.TryTriggerDeterministicForQA(DreadCatalog.B06ClockTick),
                "WT BUGFIX VERIFIED: DREAD_B06_CLOCK_TICK strictly blocked in Layer R (11:52 clock)");

            // 6) B07: Peak Horror #2 + intentional SEAM once + keys not consumed on refusal
            Require(dreadDirector.PlayPeakGateSlam(reducedMotion: true), "WT B07 Peak Horror #2 fires once");
            yield return Until(() => !busy && State.HasDreadEvent(DreadCatalog.B07GateSlam), "WT B07 gate slam persisted");
            Require(!dreadDirector.PlayPeakGateSlam(reducedMotion: true), "WT B07 gate slam does not replay");
            Apply(casework.OutsideAttempt(player.Feet), "WT B07 SEAM once");
            Require(State.Happened("CH01-T09") && casework.OutsideAttempt(player.Feet) != CommitResult.Applied,
                "WT B07 SEAM fires once and refuses repeat");
            var seamKeyRes = WorldUseResolver.Resolve(State, BackpackCatalog.GateKey, "SEAM");
            Require(seamKeyRes.Outcome == WorldUseOutcome.GateRefusedAfterCollapse &&
                    InventoryService.HasPhysical(State, BackpackCatalog.GateKey),
                "WT B07 using KEY_GATE on SEAM refuses cleanly and retains key");

            // 7) B08: Conditional P_BAG08_ECHO + Pocket Note decision reflection (A/B/C)
            Require(ShouldTriggerBag08Echo(), "WT B08 P_BAG08_ECHO eligible when OBS101B was examined in B");
            string pocketC = PocketNoteBuilder.BuildEpistemicSummary(State);
            Require(pocketC.Contains("chưa đủ căn cứ xác nhận") && !pocketC.Contains("Bé An") && !pocketC.Contains("Khu C"),
                "WT B08 Pocket Note C copy matches Walkthrough and respects canon reveal lock");
            var fastBState = State.Copy();
            fastBState.observationRecords.RemoveAll(r => r.id == ObservationRegistry.Bed4Card);
            fastBState.observed.Remove(ObservationRegistry.Bed4Card);
            fastBState.f101.current = FormStatus.Unverified;
            Require(!HypothesisService.HasExaminedInLayerB(fastBState, ObservationRegistry.Bed4Card),
                "WT fast A/B route without OBS101B never triggers P_BAG08_ECHO");
            Require(PocketNoteBuilder.BuildEpistemicSummary(fastBState).Contains("chưa xác minh"),
                "WT B08 Pocket Note B copy matches Walkthrough");
            fastBState.f101.current = FormStatus.Affirmed;
            Require(PocketNoteBuilder.BuildEpistemicSummary(fastBState).Contains("đã ký xác nhận số 0"),
                "WT B08 Pocket Note A copy matches Walkthrough");

            // 8) Proposal V1.1 CH02/CH03/CH04 Rollout & MemoryOnly & Legacy Migration verification
            var ch02Legacy = new Investigation
            {
                chapter = "CH02",
                layer = Reality.R,
                events = new List<string> { "E10a.SMSRead", "CH01-T02", "CH01-T03", "CH01-T04", "CH01-T05", "CH01-T08", "CH01-T09", "CH01-T10" },
                evidence = new List<string> { "E10a", "E12", EvidenceRegistry.DrawingFragments[0], EvidenceRegistry.DrawingFragments[1], EvidenceRegistry.DrawingFragments[2], EvidenceRegistry.Wristband, EvidenceRegistry.Transfer }
            };
            InventoryService.MigrateFromLegacy(ch02Legacy);
            Require(InventoryService.ValidState(ch02Legacy) &&
                    InventoryService.HasPhysical(ch02Legacy, BackpackCatalog.Ch02Draw1) &&
                    InventoryService.HasPhysical(ch02Legacy, BackpackCatalog.Ch02Wristband) &&
                    InventoryService.HasPhysical(ch02Legacy, BackpackCatalog.Ch02Transfer) &&
                    InventoryService.CanAssembleCh02Drawing(ch02Legacy),
                "WT Proposal CH02 legacy v1-v6 save deterministically reconstructs CH02 Backpack items & combine eligibility");
            Require(InventoryService.TryPlaceAtAnchor(ch02Legacy, BackpackCatalog.Ch02Wristband, WorldAnchors.Ward101TableRight) &&
                    InventoryService.ValidState(ch02Legacy),
                "WT Proposal CH02 persistent wristband can be placed on Room 101 comparison table in R");
            ch02Legacy.layer = Reality.M;
            InventoryService.RecordMemoryRef(ch02Legacy, BackpackCatalog.MemVignetteC02, "LOC07");
            Require(InventoryService.ValidState(ch02Legacy) &&
                    InventoryService.FindByDef(ch02Legacy, BackpackCatalog.MemVignetteC02)?.status == BackpackItemStatus.MemoryRef &&
                    !InventoryService.TryEquip(ch02Legacy, BackpackCatalog.MemVignetteC02) &&
                    !InventoryService.TryPlaceAtAnchor(ch02Legacy, BackpackCatalog.MemVignetteC02, WorldAnchors.Ward101TableLeft),
                "WT Proposal CH03 MemoryOnly stays strictly non-equippable, non-placeable, and valid in Layer M");

            var ch04Probe = new Investigation { chapter = "CH04", layer = Reality.R };
            InventoryService.SyncFromEvidence(ch04Probe, EvidenceRegistry.MeetingTape, "LOC06");
            Require(WorldUseResolver.Resolve(ch04Probe, BackpackCatalog.Ch04Tape, "AN04").Outcome == WorldUseOutcome.NotApplicableFeedback,
                "WT Proposal CH04 tape requires E02 + E09 before AN04 playback");
            ch04Probe.evidence.Add(EvidenceRegistry.DutyOrders);
            ch04Probe.evidence.Add(EvidenceRegistry.DutyLog);
            Require(WorldUseResolver.Resolve(ch04Probe, BackpackCatalog.Ch04Tape, "AN04").Outcome == WorldUseOutcome.PlayTapeAtDutyDesk,
                "WT Proposal CH04 tape resolves to PlayTapeAtDutyDesk on AN04 once E02+E09 owned");

            // Restore pre-test CH02 snapshot cleanly
            Apply(transaction.Commit(savedBefore, scene.Pad("AP05").position, warp: false, geometryChanges: true), "WT restore post-CH02");
            player.Teleport(scene.Pad("AP05"));
            verification.Add("CH01 WALKTHROUGH & PROPOSAL V1.1 100% PASS: 3D BackpackStage3D 50-cycle stress, 4 Quick-Slots, all 4 comparison surfaces (AN02/STAFF/RECEPTION/WARD101) + zone reload + remote recovery, CH02/03/04 rollout + MemoryOnly + E06a tape resolver, and complete B01-B08 reconciliation.");
        }

        private IEnumerator VerifyStaffReactionOrder()
        {
            voice.StopCurrent();
            RestoreJournal(State);
            Vector3 staff = player.Feet;
            Quaternion facing = player.transform.rotation;
            Use("OBS_SHIFT_LOG");
            yield return Until(() => ui.IsModal && !busy, "AS-05 Large Print ready");
            Require(State.FindObservation(ObservationRegistry.ShiftGuide).HasDetail("LARGE_PRINT") &&
                    ActivePageContains("Giấy kẹp của Lan") && !TryPlayStaffReaction(),
                "AS-05 Large Print includes the back; no reaction while reading");
            ButtonMatching("Đặt xuống & tiếp tục").onClick.Invoke();
            Require(staffReactions.Count == 1 && StaffTranscriptCount("CH01-D18") == 0 &&
                    !MayDeliver("CH01-D18", null), "AS-05 queues only when closed");

            lanDirector.StartEncounter(LanEncounterLine);
            Require(lanDirector.IsEncounterRunning && !voice.IsSpeaking && !TryPlayStaffReaction(),
                "STAFF reaction waits through a pause in Lan's encounter");
            lanDirector.StopEncounter(false);
            bool? completed = null;
            voice.Speak("CH01-D05", "Lan", Line("CH01-D05"), onComplete: heard => completed = heard);
            Require(voice.IsSpeaking && !TryPlayStaffReaction() && completed == null, "STAFF reaction cannot interrupt active dialogue");
            yield return Until(() => completed == true, "Lan dialogue finishes naturally");
            Require(TryPlayStaffReaction(), "AS-05 reaction plays after Lan");
            yield return Until(() => !busy && staffReactionCaption != null, "AS-05 caption shown");
            Require(StaffTranscriptCount("CH01-D18") == 1 && StaffTranscriptCount("CH01-D17") == 0, "AS-05 can be discovered first");

            Use("OBS_CORRIDOR_NOTICE");
            yield return Until(() => ui.IsModal && !busy, "notice Large Print ready");
            Require(!TryPlayStaffReaction() && StaffTranscriptCount("CH01-D17") == 0, "notice stays quiet while open");
            ButtonMatching("Đặt xuống & tiếp tục").onClick.Invoke();
            player.Teleport(scene.Pad("AP02"));
            yield return null;
            Require(staffReactions.Count == 0 && StaffTranscriptCount("CH01-D17") == 0, "leaving STAFF cancels queued reaction");

            player.Teleport(staff, facing);
            Use("OBS_CORRIDOR_NOTICE");
            yield return Until(() => ui.IsModal && !busy, "notice reopened");
            ButtonMatching("Đặt xuống & tiếp tục").onClick.Invoke();
            Require(TryPlayStaffReaction(), "notice discovered second without a detail hotspot");
            yield return Until(() => !busy && staffReactionCaption != null, "notice caption shown");
            yield return LoadAsync(SaveSlot.Auto);
            Require(StaffTranscriptCount("CH01-D17") == 1 && StaffTranscriptCount("CH01-D18") == 1 &&
                    State.FindObservation(ObservationRegistry.ShiftGuide).flippedBack,
                "autosave restores both transcripts and AS-05 back");
            player.Teleport(staff, facing);
            Use("OBS_SHIFT_LOG");
            yield return Until(() => ui.IsModal && !busy, "AS-05 reopened after reload");
            ButtonMatching("Đặt xuống & tiếp tục").onClick.Invoke();
            Require(!TryPlayStaffReaction() && staffReactions.Count == 0, "reload and reopening cannot replay AS-05");
            verification.Add("TASK04–05 STAFF PASS: reverse discovery order, Large Print, Lan and reading guards, scene cancellation, autosave and once-only reactions.");
        }

        private Dictionary<Collider, Bounds> ActiveWardColliders()
        {
            var colliders = new Dictionary<Collider, Bounds>();
            foreach (var root in scene.f101WallRenderer.gameObject.scene.GetRootGameObjects())
                foreach (var collider in root.GetComponentsInChildren<Collider>())
                    if (collider.enabled) colliders.Add(collider, collider.bounds);
            return colliders;
        }

        private static TimelineConnection Observed0317Link() => new()
        {
            left = ObservationRegistry.StoppedClock0317,
            right = ObservationRegistry.Bed4Card,
            context = ObservationRegistry.ShiftGuide
        };

        private void RequireF101Presentation(FormStatus decision, bool layerB)
        {
            FormStatus visible = layerB ? decision : FormStatus.Pending;
            var (paper, fan) = visible switch
            {
                FormStatus.Affirmed => (scene.f101MatA, 1.14f),
                FormStatus.Unverified => (scene.f101MatB, 0.56f),
                FormStatus.Refuted => (scene.f101MatC, 0.82f),
                _ => (scene.f101MatPending, 1f)
            };
            Require(scene.f101WallRenderer.sharedMaterial == paper &&
                scene.deniedBay.activeInHierarchy == (visible == FormStatus.Affirmed) &&
                scene.pendingBay.activeInHierarchy == (layerB && visible is (FormStatus.Pending or FormStatus.Unverified)) &&
                scene.refutedBay.activeInHierarchy == (visible == FormStatus.Refuted) &&
                Mathf.Approximately(scene.motion.fanSpeedScale, fan) &&
                scene.atmosphere.F101Report == visible,
                "F101 material, bay and fan baseline agree with " + decision + " in " + State.layer);
            string slipText = visible switch
            {
                FormStatus.Affirmed => "ĐÃ BÀN GIAO · 0",
                FormStatus.Unverified => "CHỜ XÁC NHẬN",
                FormStatus.Refuted => "KIỂM TRA LẠI",
                _ => null
            };
            var slip = Array.Find(scene.GetComponentsInChildren<UnityEngine.UI.Text>(), label => label.name == "Dòng bàn giao");
            Require(slipText == null ? slip == null : slip != null && slip.text.EndsWith(slipText),
                "F101 corridor slip follows the committed report and stays out of R");
        }

        private IEnumerator VerifyF101Revisions()
        {
            player.Teleport(scene.Pad("AP04"));
            if (State.f101.current != FormStatus.Refuted)
            {
                var submission = new DeductionSubmission
                {
                    formId = "F101",
                    selectedEvidenceIds = new() { ObservationRegistry.Bed4Traces, ObservationRegistry.Bed4Card },
                    claimId = HypothesisService.ClaimUnreliableZero
                };
                yield return WaitForApplied(casework.SubmitF101Async(FormStatus.Refuted, submission, player.Feet), "F101 start revision check");
            }
            FormStatus first = State.f101.firstDecision;
            int revision = State.f101.revision;
            Require(!scene.interactables.curtainPulled, "Refuted uses its own curtain without pulling the manual cloth");
            yield return null;
            float refutedIntensity = scene.bed4TubeLight.intensity;

            scene.motion.SlowFans(1.5f);
            yield return WaitForApplied(casework.SubmitF101Async(FormStatus.Unverified, null, player.Feet), "F101 Refuted → Unverified");
            RequireF101Presentation(FormStatus.Unverified, true);
            Require(!scene.interactables.curtainPulled && Mathf.Approximately(scene.motion.EffectiveFanSpeed, 0.22f),
                "Unverified restores the unpulled curtain and leaves the active fan slowdown in progress");
            yield return null;
            float baseIntensity = scene.bed4TubeLight.intensity;
            Require(Mathf.Approximately(refutedIntensity, baseIntensity * 0.48f), "Bed-4 Refuted dimming persists through frame updates");

            Require(scene.interactables.ToggleCurtain(null), "Manual curtain opens in Unverified");
            transaction.Present(State);
            Require(scene.interactables.curtainPulled, "Reapplying Unverified preserves the manual curtain target");
            long beforeFailure = State.sequence;
            var rejectedRevision = State.Copy();
            Require(FormService.Apply(rejectedRevision, FormStatus.Affirmed), "QA prepares valid A revision");
            var flushed = new TaskCompletionSource<bool>();
            var release = new TaskCompletionSource<bool>();
            var failedSign = transaction.CommitAsync(rejectedRevision, player.Feet, "AP04", geometryChanges: true, afterFlush: () =>
            {
                flushed.SetResult(true);
                release.Task.Wait(TimeSpan.FromSeconds(5));
                throw new IOException("QA F101 write interrupted before publication");
            });
            try
            {
                yield return Until(() => flushed.Task.IsCompleted, "F101 staged write reached the flush");
                Require(State.sequence == beforeFailure && scene.f101WallRenderer.sharedMaterial == scene.f101MatB,
                    "F101 paper remains Unverified while the A revision is unpublished");
            }
            finally { release.TrySetResult(true); }
            yield return Until(() => failedSign.IsCompleted, "F101 failed write rolled back");
            Require(failedSign.GetAwaiter().GetResult() == CommitResult.SaveUnavailable && State.sequence == beforeFailure &&
                scene.interactables.curtainPulled, "Failed revision retains the previous snapshot and manual curtain pose");
            RequireF101Presentation(FormStatus.Unverified, true);

            yield return WaitForApplied(casework.SubmitF101Async(FormStatus.Affirmed, null, player.Feet), "F101 Unverified → Affirmed");
            RequireF101Presentation(FormStatus.Affirmed, true);
            yield return Until(() => Mathf.Approximately(scene.motion.EffectiveFanSpeed, 1.14f), "temporary fan slowdown expires to Affirmed");
            Require(Mathf.Approximately(scene.bed4TubeLight.intensity, baseIntensity * 1.08f), "Affirmed tube intensity survives frame updates");
            Require(State.f101.firstDecision == first && State.f101.revision == revision + 2 && State.f101History.Count == revision + 2,
                "Each changed F101 decision preserves the first decision and appends one revision");
            long sequence = State.sequence;
            yield return WaitForApplied(casework.SubmitF101Async(FormStatus.Affirmed, null, player.Feet), "F101 duplicate submission");
            Require(State.sequence == sequence && State.f101.revision == revision + 2, "Unchanged submission writes no save or revision");

            scene.motion.SlowFans(7.5f);
            Require(transaction.Restore(SaveSlot.Auto, out _) == SaveRead.Valid, "Restore signed F101 snapshot in B");
            RequireF101Presentation(FormStatus.Affirmed, true);
            Require(!scene.interactables.curtainPulled && Mathf.Approximately(scene.motion.EffectiveFanSpeed, 1.14f),
                "Save/load restores the default curtain and committed fan speed");
            string ward = scene.f101WallRenderer.gameObject.scene.name;
            var oldPaper = scene.f101WallRenderer;
            yield return scene.UnloadZoneAsync(ward);
            yield return scene.LoadZoneAsync(ward);
            Require(oldPaper == null && scene.f101WallRenderer != null, "Additive reload rebinds the F101 renderer");
            RequireF101Presentation(FormStatus.Affirmed, true);
            yield return null;
            Require(Mathf.Approximately(scene.bed4TubeLight.intensity, baseIntensity * 1.08f) &&
                Mathf.Approximately(scene.motion.EffectiveFanSpeed, 1.14f), "Additive reload restores the tube and fan baselines");
            verification.Add("TASK03 F101 PASS: initial presentations, revisions, manual curtain, motion expiry, save restore, additive rebind, and R collider independence.");
        }

        private IEnumerator WaitForApplied(Task<CommitResult> pending, string label)
        {
            while (!pending.IsCompleted) yield return null;
            Apply(pending.GetAwaiter().GetResult(), label);
        }

        private IEnumerator CaptureHospitalFrame(string name, Transform pose, float yaw)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) yield break;
            var camera = player.Eye;
            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.Create();
            bool previous = camera.enabled;
            camera.enabled = false;
            try
            {
                player.Teleport(pose.position, pose.rotation * Quaternion.Euler(0, yaw, 0));
                camera.transform.localRotation = Quaternion.Euler(6f, 0, 0);
                for (int i = 0; i < 4; i++) yield return null;
                SaveFrame(camera, rt, name);
            }
            finally
            {
                camera.enabled = previous;
                rt.Release();
                Destroy(rt);
            }
        }
        private static string JsonEscape(string s) =>
            "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\"";

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
