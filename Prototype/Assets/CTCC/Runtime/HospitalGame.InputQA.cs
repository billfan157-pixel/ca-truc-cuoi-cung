using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Profiling;
using UnityEngine.UI;

namespace CTCC
{
    public sealed partial class HospitalGame
    {
        private Keyboard qaKeys;
        private bool captureExpansion;

        private IEnumerator VerifyPlayerInput()
        {
            Directory.CreateDirectory(Argument("-ctcc-input-qa"));
            Require(Argument("-ctcc-save") != null && transaction.Inspect(SaveSlot.Auto) == SaveRead.Empty &&
                transaction.Inspect(SaveSlot.Manual) == SaveRead.Empty, "Input QA requires isolated, empty save slots");
            yield return Until(() => Application.isFocused, "Input QA needs a focused Windows player");
            var physicalKeyboards = InputSystem.devices.OfType<Keyboard>().Where(d => d.enabled).ToArray();
            foreach (var kb in physicalKeyboards) InputSystem.DisableDevice(kb);
            qaKeys = InputSystem.AddDevice<Keyboard>();
            qaKeys.MakeCurrent();
            var background = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            try
            {
                yield return null;
                yield return null;
                foreach (var choice in new[] { FormStatus.Affirmed, FormStatus.Unverified, FormStatus.Refuted })
                    yield return InputVisit(choice);
                yield return InspectLifetimeStress();
            }
            finally
            {
                InputSystem.RemoveDevice(qaKeys);
                foreach (var kb in physicalKeyboards)
                {
                    if (kb.added) InputSystem.EnableDevice(kb);
                }
                InputSystem.settings.backgroundBehavior = background;
            }
        }

        private IEnumerator InputVisit(FormStatus branch)
        {
            yield return Choose("Ca trực mới");
            if (branch != FormStatus.Affirmed) yield return Choose("Bắt đầu");
            yield return Until(() => inspector.IsInspecting, branch + " letter uses physical inspector");
            if (branch == FormStatus.Affirmed)
            {
                var holder = player.Eye.transform.Find("Physical Inspector Stage/Inspect Holder");
                Quaternion before = holder.localRotation;
                float zoom = holder.parent.localPosition.z;
                yield return Hold(Key.A, 0.18f);
                yield return Hold(Key.Q, 0.18f);
                Require(Quaternion.Angle(before, holder.localRotation) > 9 && Mathf.Abs(zoom - holder.parent.localPosition.z) > 0.017f,
                    "physical inspector keyboard rotation and zoom");
            }
            yield return Choose("Ghi tin nhắn");
            yield return Until(() => !busy && State.Happened("E10a.SMSRead"), "letter checkpoint");
            Require(!inspector.IsInspecting && State.Happened("E10a.SMSRead"), "letter closes and enters journal");
            yield return UseFrom("GATE-CONG", scene.Pad("AP00").position);
            yield return Until(() => State.Happened("CH01-T02"), "gate unlocked");
            if (branch == FormStatus.Affirmed)
            {
                player.Teleport(scene.Pad("AP00"));
                yield return Until(() => Application.isFocused, "movement check requires a focused window");
                float z = player.Feet.z;
                yield return Hold(Key.W, 0.4f);
                Require(player.Feet.z > z + 0.35f, "W moves the real capsule");
            }
            if (branch == FormStatus.Refuted)
            {
                // FULL UNINTERRUPTED KEYBOARD+MOUSE CH01->CH02 ROUTE (ZERO TELEPORTATION INSIDE CH01!)
                string record = Argument("-ctcc-record");
                captureExpansion = !string.IsNullOrEmpty(record);
                if (captureExpansion) StartCoroutine(CaptureExpansionFrames(Path.Combine(record, "ch01r-full")));
                try
                {
                    yield return WalkWithInput(new Vector3(4f, 0f, -18.0f));
                    yield return WalkWithInput(new Vector3(4f, 0f, -10.5f));
                    yield return WalkWithInput(new Vector3(4f, 0f, -3.30f));
                    yield return Until(() => !busy && State.layer == Reality.B && State.Happened("CH01-T03"), "live reception R→B without modal hijack");
                    // Optional live Lan interaction via LAN_TALK -> verify transcript & close cleanly!
                    yield return WalkWithInput(new Vector3(3.35f, 0f, -3.40f));
                    yield return AimAndUseHere("LAN_TALK");
                    yield return Until(() => ui.IsModal, "Lan optional dialogue opens");
                    yield return Choose("Xem bảng kẹp");
                    yield return WalkWithInput(new Vector3(2.95f, 0f, -2.75f));
                    yield return AimAndUseHere("BOARD");
                    yield return Until(() => !busy && State.Happened("CH01-T05"), "board granted");
                    yield return Until(() => Quaternion.Angle(scene.roomDoor.pivot.localRotation, Quaternion.Euler(0, scene.roomDoor.openAngle, 0)) < 1,
                        "room door opens");

                    // Walk optional compact nonlinear exploration loop: Reception -> STAFF Nursing Alcove -> open DOOR-STAFF -> HALL -> Room 101!
                    yield return WalkWithInput(new Vector3(0.05f, 0f, -1.0f));
                    yield return WalkWithInput(new Vector3(-0.12f, 0f, 3.65f));
                    yield return AimAndUseHere("OBS_CORRIDOR_NOTICE");
                    yield return Until(() => !busy && inspector.IsInspecting, "STAFF overflow notice inspected");
                    Require(StaffTranscriptCount("CH01-D17") == 0 && ActivePageContains("Một tờ thông báo") &&
                            !ActivePageContains("Dữ kiện quan sát"), "notice opens neutrally and without a reaction");
                    yield return Tap(Key.Escape);
                    yield return Until(() => StaffTranscriptCount("CH01-D17") == 1 && staffReactionCaption != null,
                        "notice reacts after ordinary view and close");
                    ScreenCapture.CaptureScreenshot(Path.Combine(Argument("-ctcc-input-qa"), "task04-05-notice-reaction.png"));
                    yield return WalkWithInput(new Vector3(-0.15f, 0f, 3.92f));
                    yield return AimAndUseHere("DRAWER-NURSE");
                    yield return Until(() => scene.interactables.IsDrawerOpen("DRAWER-NURSE"), "STAFF nurse drawer slides open");
                    yield return new WaitForSecondsRealtime(0.45f);
                    yield return AimAndUseHere("OBS_SHIFT_LOG");
                    yield return Until(() => !busy && inspector.IsInspecting, "AS-05 shift guide inspected");
                    yield return Tap(Key.Digit3);
                    Require(!inspector.ShowingBack && inspector.ActiveDetailIndex == -1 && !ActivePageContains("[3] Soi"),
                        "AS-05 does not reveal a back detail from the front");
                    yield return Tap(Key.Escape);
                    Require(staffReactions.Count == 0 && StaffTranscriptCount("CH01-D18") == 0,
                        "AS-05 opening alone does not earn the reaction");
                    yield return AimAndUseHere("OBS_SHIFT_LOG");
                    yield return Until(() => !busy && inspector.IsInspecting, "AS-05 reopened for detail inspection");
                    yield return Tap(Key.Space); // Test live keyboard flip!
                    yield return Until(() => inspector.ShowingBack, "AS-05 flipped to back via Space");
                    yield return Tap(Key.Digit3);
                    yield return Until(() => !busy && ActivePageContains("Giấy kẹp của Lan"), "AS-05 side-specific detail readout");
                    Require(inspector.ActiveDetailIndex == 2 && StaffTranscriptCount("CH01-D18") == 0,
                        "AS-05 reaction stays quiet while reading the discovered detail");
                    yield return new WaitForSecondsRealtime(0.35f);
                    ScreenCapture.CaptureScreenshot(Path.Combine(Argument("-ctcc-input-qa"), "task04-05-shift-back.png"));
                    yield return Tap(Key.Escape);
                    yield return Until(() => StaffTranscriptCount("CH01-D18") == 1 && staffReactionCaption != null,
                        "AS-05 reacts after inspection and close");
                    yield return WalkWithInput(new Vector3(0.15f, 0f, 7.50f));
                    yield return WalkWithInput(new Vector3(1.55f, 0f, 7.50f));
                    yield return AimAndUseHere("DOOR-STAFF");
                    yield return Until(() => scene.staffCrossDoor.isOpen &&
                        Quaternion.Angle(scene.staffCrossDoor.pivot.localRotation, Quaternion.Euler(0, scene.staffCrossDoor.openAngle, 0)) < 2f,
                        "STAFF cross-door swings fully open via keyboard use");
                    yield return WalkWithInput(new Vector3(4.00f, 0f, 7.50f));
                    yield return WalkWithInput(new Vector3(4.00f, 0f, 4.75f));
                    yield return WalkWithInput(new Vector3(6.52f, 0f, 4.93f));
                    yield return Until(() => !busy && State.Happened("CH01-T06"), "Room 101 threshold checkpoint via keyboard walk");

                    // Investigate Bed 2 comparison doc, Bed 3 drawer + medication log, Bed 4 traces, and Bed 4 card!
                    yield return WalkWithInput(new Vector3(10.15f, 0f, 5.10f));
                    yield return AimAndUseHere("OBS101C");
                    yield return Until(() => !busy && inspector.IsInspecting, "OBS101C Bed-2 transfer inspected");
                    yield return Tap(Key.Escape);

                    yield return WalkWithInput(new Vector3(8.64f, 0f, 3.65f));
                    yield return AimAndUseHere("DRAWER-BED3");
                    yield return Until(() => scene.interactables.IsDrawerOpen("DRAWER-BED3"), "Bed-3 drawer slides open");
                    yield return new WaitForSecondsRealtime(0.45f);
                    yield return AimAndUseHere("OBS101D");
                    yield return Until(() => !busy && inspector.IsInspecting, "OBS101D Bed-3 meds inspected");
                    yield return Tap(Key.Escape);

                    yield return WalkWithInput(new Vector3(12.05f, 0f, 5.05f));
                    yield return AimAndUseHere("CURTAIN-101");
                    yield return Until(() => scene.interactables.curtainPulled, "Bed 4 curtain opened");
                    yield return new WaitForSecondsRealtime(0.55f);
                    yield return WalkWithInput(new Vector3(11.35f, 0f, 5.25f));
                    yield return AimAndUseHere("OBS101B");
                    yield return Until(() => !busy && inspector.IsInspecting, "OBS101B card inspected");
                    yield return Hold(Key.A, 2.1f);
                    yield return Tap(Key.Digit3);
                    Require(!inspector.ShowingBack && inspector.ActiveDetailIndex == -1 && !ActivePageContains("[3] Soi") &&
                            !State.FindObservation(ObservationRegistry.Bed4Card).flippedBack &&
                            ActivePageContains("Một tấm thẻ giấy"), "unseen Bed-4 back remains hidden after tilt and shortcut");
                    ScreenCapture.CaptureScreenshot(Path.Combine(Argument("-ctcc-input-qa"), "task04-05-card-front.png"));
                    yield return Choose("Mở bản chữ lớn");
                    yield return Until(() => !busy && !inspector.IsInspecting && ui.IsModal, "Bed-4 Large Print ready after save");
                    Require(ActivePageContains("MẶT SAU") &&
                            ActivePageContains("Phần ghi nơi chuyển bị nhòe") &&
                            State.FindObservation(ObservationRegistry.Bed4Card).HasDetail("LARGE_PRINT") &&
                            State.FindObservation(ObservationRegistry.Bed4Card).flippedBack,
                        "Bed-4 Large Print grants both sides without explicit flip");
                    ScreenCapture.CaptureScreenshot(Path.Combine(Argument("-ctcc-input-qa"), "task04-05-card-large-print.png"));
                    yield return Tap(Key.Escape);

                    yield return WalkWithInput(new Vector3(10.75f, 0f, 4.35f));
                    yield return AimAndUseHere("OBS101A");
                    yield return Until(() => !busy && State.Observed("OBS101A"), "OBS101A Bed-4 trace observed");

                    // Open Evidence Journal (Tab) & test Side-by-Side Document Comparison!
                    yield return Tap(Key.Tab);
                    yield return Until(() => ui.IsModal, "Journal opened via Tab");
                    yield return Choose("Quan sát & Nguồn chứng cứ");
                    Require(ActivePageContains("Phần ghi nơi chuyển bị nhòe") && ActivePageContains("Nhận định"),
                        "journal keeps interpretation and discovered back evidence");
                    yield return Choose("Quay lại Sổ điều tra");
                    yield return Choose("So sánh hai tài liệu");
                    yield return Choose("Đổi tài liệu phải");
                    yield return Choose("Quay lại");
                    yield return Choose("Khép sổ");

                    // Recover from both evidence and conclusion feedback in the real F101 UI.
                    yield return WalkWithInput(new Vector3(6.52f, 0f, 4.93f));
                    yield return AimAndUseHere("F101");
                    yield return Choose("Soi phiếu F101");
                    Require(ActivePageContains("Một phiếu in sẵn") && !ActivePageContains("[3] Soi"), "F101 opens neutral");
                    yield return Choose("Lật sang mặt sau");
                    yield return Tap(Key.Digit3);
                    yield return Until(() => !busy && ActivePageContains("Chưa thấy phiếu chốt"), "F101 back detail unlocks after explicit flip");
                    yield return Tap(Key.Escape);
                    Require(ui.IsModal && !inspector.IsInspecting, "return to F101 decision closes inspector");
                    yield return Choose("C    Lập luận");
                    yield return Choose("Dấu sử dụng tại vị trí giường 4");
                    yield return Choose("Thẻ bàn giao giường số 4");
                    yield return Choose("Phiếu phát thuốc giường 03");
                    yield return Choose("Chọn nhận định");
                    const string supported = "Số 0 bỏ sót hồ sơ bàn giao còn để ngỏ";
                    var supportedButton = ButtonMatching(supported);
                    Require(EventSystem.current.currentSelectedGameObject != supportedButton.gameObject &&
                        supportedButton.interactable &&
                        supportedButton.GetComponentInChildren<Text>().text.StartsWith("○") &&
                        !ButtonMatching("Kiểm tra lập luận").interactable,
                        "Supported conclusion starts unselected and is not initially highlighted");
                    yield return Choose(supported);
                    yield return Choose("Kiểm tra lập luận");
                    yield return Until(() => !busy && ui.GetComponentsInChildren<Text>().Any(t => t.text.Contains("Nó chỉ giúp kiểm tra dòng đó")),
                        "Irrelevant evidence returns to the evidence list with a document-specific reason");
                    Require(State.f101.current == FormStatus.Pending, "Rejected evidence leaves F101 pending");
                    yield return Choose("[✓]  Phiếu phát thuốc giường 03");
                    yield return Choose("Chọn nhận định");

                    const string accusation = "Người lập phiếu cố ý bỏ qua giường tạm";
                    yield return Choose(accusation);
                    yield return Choose("Kiểm tra lập luận");
                    yield return Until(() => !busy && ui.GetComponentsInChildren<Text>().Any(t => t.text.Contains("người lập phiếu đã biết gì")),
                        "Overclaim feedback explains the missing link about intent");
                    Require(!GateService.G01(State) && State.f101History.Count == 0 &&
                        EventSystem.current.currentSelectedGameObject == ButtonMatching(accusation).gameObject &&
                        !ui.GetComponentsInChildren<Text>().Single(t => t.name == "Readable copy").text.Contains(supported),
                        "Rejected claim keeps its focus and the feedback does not state the supported conclusion");

                    // Change only the conclusion. The selected pair must survive both retries.
                    yield return Choose(supported);
                    yield return Choose("Kiểm tra lập luận");
                    yield return Choose("Xác nhận và đóng dấu phiếu");
                    yield return Until(() => !busy && GateService.G01(State), "F101 Refuted committed via real UI");
                    Require(State.f101.firstDecision == FormStatus.Refuted && State.f101.revision == 1 &&
                        scene.refutedBay != null && scene.refutedBay.activeInHierarchy, "Refuted visual & state verified");
                    if (captureExpansion) yield return new WaitForSecondsRealtime(0.75f);

                    // Walk back out of Room 101 to Reception (B->R collapse), outer gate seam, and Reception (CH02 handoff) — ZERO TELEPORTS!
                    yield return WalkWithInput(new Vector3(4.00f, 0f, 4.75f));
                    yield return WalkWithInput(new Vector3(4.00f, 0f, -3.30f));
                    yield return Until(() => !busy && State.layer == Reality.R && State.Happened("CH01-T08"), "reception B→R collapse via walk");
                    yield return WalkWithInput(new Vector3(4.00f, 0f, -10.5f));
                    yield return WalkWithInput(new Vector3(4.00f, 0f, -20.30f));
                    yield return Until(() => !busy && State.Happened("CH01-T09"), "exterior seam via walk");
                    yield return WalkWithInput(new Vector3(4.00f, 0f, -10.5f));
                    yield return WalkWithInput(new Vector3(4.00f, 0f, -3.30f));
                    yield return Until(() => !busy && State.chapter == "CH02", "CH02 handoff via pure keyboard+mouse CH01 route");
                    verification.Add("INPUT CH01-R FULL ROUTE PASS: zero teleportation from AP00 through STAFF loop, drawers, Room 101 docs, dual-sided inspector, Journal comparison, F101 deduction rejection & valid refutation, collapse, seam, and CH02 handoff.");
                }
                finally { captureExpansion = false; }
            }
            else
            {
                player.Teleport(scene.Pad("AP02"));
                yield return Until(() => !busy && State.layer == Reality.B && State.Happened("CH01-T03"), "reception R→B");
                yield return UseFrom("BOARD", scene.Pad("AP02").position);
                yield return Until(() => !busy && State.Happened("CH01-T05"), "board granted");
                yield return Until(() => Quaternion.Angle(scene.roomDoor.pivot.localRotation, Quaternion.Euler(0, scene.roomDoor.openAngle, 0)) < 1,
                    "room door opens");

                if (branch == FormStatus.Affirmed)
                {
                    yield return UseFrom("DOOR-101", scene.Pad("AP04").position);
                    yield return Until(() => !scene.roomDoor.isOpen && Quaternion.Angle(scene.roomDoor.pivot.localRotation, Quaternion.identity) < 1,
                        "door closes through use input");
                    yield return UseFrom("DOOR-101", scene.Pad("AP04").position);
                    yield return Until(() => scene.roomDoor.isOpen && Quaternion.Angle(scene.roomDoor.pivot.localRotation, Quaternion.Euler(0, scene.roomDoor.openAngle, 0)) < 1,
                        "door reopens through use input");
                }
                if (branch == FormStatus.Unverified)
                {
                    var cloth = scene.interactables.curtainCloth;
                    var scale = cloth.localScale;
                    yield return UseFrom("CURTAIN-101", new Vector3(11.68f, 0.02f, 4.92f));
                    yield return Until(() => scene.interactables.curtainPulled && (cloth.localScale - scale).sqrMagnitude > 0.05f,
                        "curtain opens through use input");
                    yield return UseFrom("CURTAIN-101", new Vector3(11.68f, 0.02f, 4.92f));
                    yield return Until(() => !scene.interactables.curtainPulled && (cloth.localScale - scale).sqrMagnitude < 0.001f,
                        "curtain closes through use input");
                }

                yield return OpenFormInput();
                yield return Choose(branch == FormStatus.Affirmed ? "A    Xác nhận" : "B    Để ngỏ");
                yield return Choose("Xác nhận và đóng dấu phiếu");
                yield return Until(() => !busy && GateService.G01(State), $"F101 {branch} confirmation");
                Require(State.f101.firstDecision == branch && State.f101.revision == 1 &&
                    FormService.Distortion(State) == (branch == FormStatus.Affirmed ? 1 : 0), $"F101 {branch} state");
                if (branch == FormStatus.Unverified)
                {
                    var audio = scene.atmosphere;
                    bool SignatureCuePlaying() => audio.GetComponentsInChildren<AudioSource>().Any(source =>
                        source.isPlaying && (source.clip == audio.stamp || source.clip == audio.switchLayer));
                    yield return Until(() => !voice.IsSpeaking && !SignatureCuePlaying(), "first signature cue finished");
                    long sequence = State.sequence;
                    int historyCount = State.f101History.Count;
                    yield return OpenFormInput();
                    yield return Choose("B    Để ngỏ");
                    yield return Choose("Xác nhận và đóng dấu phiếu");
                    yield return Until(() => !busy && !ui.IsModal, "duplicate F101 submission closes");
                    Require(!voice.IsSpeaking && !SignatureCuePlaying() && State.sequence == sequence &&
                        State.f101.revision == 1 && State.f101History.Count == historyCount,
                        "Unchanged F101 UI submission issues no PA, stamp, transition cue, save or revision");
                    verification.Add("TASK03 INPUT PASS: unchanged F101 submission through the keyboard UI is silent and preserves the revision.");
                }
                player.Teleport(scene.Pad("AP02"));
                yield return Until(() => !busy && State.layer == Reality.R, "reception B→R");
                player.Teleport(new Vector3(4, 0.02f, -20.30f), Quaternion.identity);
                yield return Until(() => !busy && State.Happened("CH01-T09"), "exterior seam");
                player.Teleport(scene.Pad("AP02"));
                yield return Until(() => !busy && State.chapter == "CH02", "CH02 handoff");
            }

            if (branch != FormStatus.Refuted) yield return InputPhysicalCase(branch == FormStatus.Unverified);
            player.Teleport(scene.Pad("AP02"));
            yield return Tap(Key.Escape);
            if (branch == FormStatus.Refuted)
            {
                yield return Choose("Cài đặt & Trợ năng");
                foreach (string label in new[] { "Chuyển động", "Chú thích âm thanh", "Âm lượng tổng", "Âm lượng tổng", "Âm nền môi trường", "Âm nền môi trường",
                    "Âm thanh ·", "Cỡ chữ", "Độ sáng", "Phím ·" }) yield return Choose(label);
                Require(State.settings.reduceMotion && !State.settings.captions && State.settings.volume == 0 &&
                    State.settings.ambienceVolume == 0 && State.settings.monoAudio && State.settings.textScale > 1.25f &&
                    State.settings.visibility > 0 && State.settings.alternateBindings && AudioListener.volume == 0,
                    "accessibility settings changed through actual controls");
                yield return Tap(Key.Escape);
            }
            yield return Choose("Lưu tay");
            yield return Tap(Key.Escape);
            yield return Choose("Mở bản lưu tay");
            yield return Until(() => !ui.IsModal && !busy, "manual reload finished");
            Require(!ui.IsModal && State.chapter == "CH02" && State.f101.firstDecision == branch &&
                transaction.OnPad(player.Feet, SafeCheckpoints.Pad(State.checkpoint)), "manual load restores state and checkpoint");
            Require(voice.Transcript.Any(entry => entry.LineId == "CH01-D07"), "Skipped Lan transcript survives actual manual load");
            Require(StaffTranscriptCount("CH01-D17") == (branch == FormStatus.Refuted ? 1 : 0) &&
                    StaffTranscriptCount("CH01-D18") == (branch == FormStatus.Refuted ? 1 : 0),
                "STAFF reactions survive reload once and remain absent on routes that skip STAFF");
            if (branch == FormStatus.Refuted)
            {
                Require(State.f101.current == FormStatus.Refuted && State.f101History.Count == 1 &&
                    State.f101History[0].claimId == HypothesisService.ClaimUnreliableZero &&
                    State.f101Argument.claimId == HypothesisService.ClaimUnreliableZero &&
                    State.f101Argument.selectedEvidenceIds.Count == 2 &&
                    State.f101Argument.selectedEvidenceIds.Contains(ObservationRegistry.Bed4Traces) &&
                    State.f101Argument.selectedEvidenceIds.Contains(ObservationRegistry.Bed4Card) &&
                    State.FindObservation(ObservationRegistry.Bed4Card).HasDetail("LARGE_PRINT") &&
                    State.FindObservation(ObservationRegistry.ShiftGuide).HasDetail("SHIFT_BACK") &&
                    State.FindObservation(ObservationRegistry.F101Sheet).HasDetail("F101_BACK"),
                    "Supported F101 argument and its single committed revision survive CH02 manual load");
                Require(State.settings.alternateBindings && State.settings.textScale > 1.25f && AudioListener.volume == 0,
                    "accessibility survives manual load");
                yield return Tap(Key.J);
                yield return Until(() => ui.IsModal, "remapped J opens journal");
                yield return Tap(Key.Escape);
            }
            verification.Add($"INPUT PASS {branch}: Unity Input System, focus/interaction and UI, F101 branch, CH02 handoff, manual save/load.");
            if (branch == FormStatus.Unverified && !Environment.GetCommandLineArgs().Contains("-ctcc-ch01-ch02-only"))
            {
                string record = Argument("-ctcc-record");
                captureExpansion = !string.IsNullOrEmpty(record);
                if (captureExpansion) StartCoroutine(CaptureExpansionFrames(Path.Combine(record, "ch03-04")));
                try { yield return InputHospitalExpansion(); }
                finally { captureExpansion = false; }
            }
            yield return Tap(Key.Escape);
            yield return Choose("Về màn hình đầu");
        }

        private IEnumerator InputHospitalExpansion()
        {
            yield return UseFrom("EXIT", scene.Pad("AP05").position);
            yield return Choose("Mở lối lên khu B");
            yield return Until(() => !busy && State.chapter == "CH03", "CH03 handoff");
            Require(player.Feet.y < .3f, "CH03 did not warp to the second floor");
            yield return WalkWithInput(new Vector3(4, 4, 33.73f));
            yield return WalkWithInput(new Vector3(-2.7f, 4, 34.35f));
            yield return WalkWithInput(new Vector3(-14.7f, 8, 34.35f));
            yield return WalkWithInput(new Vector3(-14.7f, 8, 36.8f));
            yield return WalkWithInput(new Vector3(-2.4f, 8, 36.8f));
            var an03 = scene.Pad("AN03");
            yield return UseFrom("E03", new Vector3(-.68f, 8.015f, 31.9f));
            yield return Choose("Chép sổ");
            yield return UseFrom("C02_NOTE", an03.position);
            yield return Until(() => !busy && State.Has(EvidenceRegistry.C02CareNote), "care note");
            yield return UseFrom("C02_BAG", new Vector3(-1.55f, 8.015f, 31.12f));
            yield return Choose("Ghi lại thẻ");
            yield return UseFrom("AN03", an03.position);
            yield return Choose("Ghi nhận C12");
            yield return Until(() => !busy && !GateService.G03(State), "wrong PZ03 input rejected");
            yield return Choose("Ghi nhận C02");
            yield return Choose("Xem ký ức");
            yield return Until(() => State.layer == Reality.M, "M via AN03");
            yield return Choose("Trở về kho");
            yield return Until(() => !busy && State.layer == Reality.R && !ui.IsModal, "Back to R");
            yield return UseFrom("C12_BOX", new Vector3(-1.55f, 8.015f, 33.45f));
            yield return Choose("Ghi nhận sự tồn tại C12");
            yield return Until(() => !busy && State.Happened("FS19"), "C12 row saved");
            Require(!State.PatientIdentityVerified("C02") && !State.PatientIdentityVerified("C12"), "No name required");
            yield return WalkWithInput(new Vector3(-.8f, 8, 36.8f));
            yield return WalkWithInput(new Vector3(-14.7f, 8, 36.8f));
            yield return WalkWithInput(new Vector3(-14.7f, 8, 34.35f));
            yield return WalkWithInput(new Vector3(-2.65f, 4, 34.35f));
            yield return WalkWithInput(new Vector3(4, 4, 35.0f));
            yield return WalkWithInput(new Vector3(4, 4, 45.7f));
            yield return UseFrom("CH03_EXIT", scene.Pad("AP07").position);
            yield return Choose("Vào phòng trực");
            yield return Until(() => !busy && State.chapter == "CH04", "CH04 handoff");
            yield return UseFrom("AN04", scene.Pad("AP07").position);
            yield return Choose("Lấy bản lệnh giao ca");
            yield return Choose("Lưu vào sổ trực");
            yield return Choose("Chép sổ ca đêm");
            yield return Choose("Lưu vào sổ trực");
            yield return Choose("Đọc bản chép băng");
            yield return Choose("Ghi nhận lời Lan");
            yield return Choose("Rời phòng trực");
            yield return WalkWithInput(new Vector3(4, 4, 33.6f));
            yield return WalkWithInput(new Vector3(4, 0, 20.97f));
            yield return WalkWithInput(new Vector3(7, 0, 21.17f));
            yield return WalkWithInput(new Vector3(7, -3.4f, 32.4f));
            yield return UseFrom("CH04_POWER", new Vector3(8.67f, -3.385f, 34.5f));
            yield return Choose("Kho · 2");
            yield return Choose("Kỹ thuật · 1");
            yield return Choose("Rời bảng điện");
            yield return WalkWithInput(new Vector3(7, -3.4f, 38.98f));
            yield return WalkWithInput(new Vector3(1.5f, -3.4f, 39f));
            yield return UseFrom("E04_E07", scene.Pad("AP09").position);
            yield return Choose("Đọc hồ sơ tiếp nhận");
            yield return Choose("Chép tài liệu");
            yield return Choose("Đọc thẻ và phiếu");
            yield return Choose("Chép tài liệu");
            yield return Choose("Đối chiếu phiếu L-13");
            yield return Choose("Ghi nhận đối chiếu");
            yield return Choose("Nghe Lan");
            yield return Choose("Hỏi Lan");
            yield return Choose("Quay về cầu thang");
            yield return Tap(Key.Escape);
            yield return Choose("Tải điểm lưu tự động");
            yield return Until(() => !busy && !ui.IsModal && transaction.OnPad(player.Feet, "AP09"), "B1 reload with actual menu");
            Require(GateService.G04(State) && State.Happened("CH04-T06") && !State.Has("E08"), "B1 autosave state");
            verification.Add("INPUT CH03–04 PASS: W capsule on three flights, PZ03, C12, R/M, duty papers, circuit controls, H05, dialogue guard and B1 autosave menu.");
        }

        private IEnumerator WalkWithInput(Vector3 destination)
        {
            float deadline = Time.unscaledTime + 16f;
            Key forward = State.settings.alternateBindings ? Key.UpArrow : Key.W;
            InputSystem.QueueStateEvent(qaKeys, new KeyboardState(forward));
            Vector3 offset;
            do
            {
                offset = destination - player.Feet;
                offset.y = 0;
                if (offset.sqrMagnitude < .20f * .20f) break;
                player.transform.rotation = Quaternion.LookRotation(offset);
                qaKeys.MakeCurrent();
                yield return null;
            } while (Time.unscaledTime < deadline);
            InputSystem.QueueStateEvent(qaKeys, new KeyboardState());
            yield return Until(() => !qaKeys[forward].isPressed, "walk release");
            string blocker = "none";
            if (offset.sqrMagnitude >= .20f * .20f &&
                Physics.CapsuleCast(player.Feet + Vector3.up * .46f, player.Feet + Vector3.up * 1.40f,
                    HospitalScene.Radius, offset.normalized, out var hit, .50f,
                    HospitalScene.SolidMask, QueryTriggerInteraction.Ignore))
                blocker = hit.collider.name;
            Require(offset.sqrMagnitude < .20f * .20f && Mathf.Abs(player.Feet.y - destination.y) < .32f,
                $"Input route to {destination} ended at {player.Feet}; blocker={blocker}, controls={player.CanPlay}, modal={ui.IsModal}");
        }

        private IEnumerator CaptureExpansionFrames(string directory)
        {
            Directory.CreateDirectory(directory);
            var frameEnd = new WaitForEndOfFrame();
            float nextFrame = Time.unscaledTime;
            int index = 0;
            while (captureExpansion)
            {
                yield return frameEnd;
                if (Time.unscaledTime < nextFrame || !captureExpansion) continue;
                nextFrame += 1f / 12f;
                if (nextFrame < Time.unscaledTime - .24f) nextFrame = Time.unscaledTime;
                var frame = ScreenCapture.CaptureScreenshotAsTexture();
                if (frame == null) continue;
                try { File.WriteAllBytes(Path.Combine(directory, $"frame-{index++:00000}.jpg"), frame.EncodeToJPG(78)); }
                finally { Destroy(frame); }
            }
        }

        private IEnumerator InputPhysicalCase(bool sideDoor)
        {
            yield return UseFrom("LIST", new Vector3(6.69f, 0.02f, 3.35f));
            yield return Until(() => !busy && inspector.IsInspecting, "room list checkpoint");
            Require(inspector.IsInspecting, "R list opens physical paper");
            yield return Tap(Key.Escape);
            yield return UseFrom("SOCKETS", new Vector3(11.53f, 0.02f, 3.14f));
            yield return Until(() => !busy && State.Observed("SOCKETS"), "sockets checkpoint");
            yield return UseFrom("WHEELS", new Vector3(11.63f, 0.02f, 4.88f));
            yield return Until(() => !busy && State.Observed("WHEELS"), "wheel marks checkpoint");
            yield return UseFrom("PZ01", new Vector3(12.60f, 0.02f, 3.63f));
            yield return Choose("Có một chỗ kê");
            yield return Until(() => !busy && State.Has("E12"), "E12 checkpoint");
            Require(State.Has("E12") && (scene.cabinet.localPosition - scene.cabinetOpen).sqrMagnitude > 0.001f,
                "PZ01 opens cabinet with animation");
            yield return Until(() => (scene.cabinet.localPosition - scene.cabinetOpen).sqrMagnitude < 0.0004f,
                "cabinet settles at open pose");
            foreach (string id in new[] { "DRAW1", "DRAW2", "DRAW3", "WRIST", "TRANSFER" })
            {
                yield return UseFrom(id, new Vector3(15.0f, 0.02f, 3.64f));
                Require(inspector.IsInspecting, id + " inspect");
                yield return Choose("Thu thập");
                yield return Until(() => !busy && !ui.IsModal, id + " durable pickup completed");
            }
            player.Teleport(scene.Pad("AP04"));
            yield return Tap(Key.Q);
            yield return Choose("Ghép tranh và kiểm hồ sơ");
            yield return Choose("Ghép tranh theo đường mép");
            yield return Until(() => !busy && State.Has(EvidenceRegistry.Drawing), "assembled drawing was saved");
            yield return Choose("Đối chiếu An");
            yield return Until(() => !busy && !savingDialogue && GateService.G02(State), "C01 checkpoint");
            Require(GateService.G02(State), "G02 opens through physical evidence and UI");
            float x = sideDoor ? 0.05f : 4f;
            player.Teleport(new Vector3(x, 0.02f, 10.4f), Quaternion.identity);
            yield return Until(() => gatekeeperCameo.IsActive, "Gatekeeper begins from the corridor trigger");
            yield return Hold(Key.W, 3.8f);
            yield return Until(() => !busy && !savingDialogue && State.Happened("CH02-T06") && !gatekeeperCameo.IsActive,
                sideDoor ? "walked through Door B" : "walked through Door A");
            yield return UseFrom("EXIT", scene.Pad("AP05").position);
            yield return Until(() => !busy && State.Happened("M1-complete") && ui.IsModal, "exit reached and recorded");
            yield return Choose("Ở lại khám phá");
            verification.Add($"INPUT PASS E11/E12/G02 & CH02-Q05 Door {(sideDoor ? "B" : "A")}: physical clues, cabinet, assembly, identification, walking escape and ER ledger.");
        }

        private IEnumerator OpenFormInput()
        {
            player.Teleport(scene.Pad("AP04"));
            yield return Until(() => State.Happened("CH01-T06"), "room checkpoint");
            yield return Tap(Key.Tab);
            yield return Choose("Mở phiếu F101");
        }

        private IEnumerator AimAndUseHere(string id)
        {
            var target = FindObjectsByType<SceneTarget>(FindObjectsSortMode.None).Single(t => t.id == id);
            yield return Until(() => !busy && !savingDialogue, "ready to aim at " + id);
            Vector3 look = target.transform.position - player.Eye.transform.position;
            Vector3 planar = new(look.x, 0f, look.z);
            if (planar.sqrMagnitude > 0.0001f)
                player.transform.rotation = Quaternion.LookRotation(planar);
            player.Eye.transform.LookAt(target.transform.position);
            Require(player.Focus(Available) == target,
                $"No non-teleport sightline to {id} from walked position {player.Feet} (target at {target.transform.position})");
            player.CanPlay = false;
            yield return Tap(State.settings.alternateBindings ? Key.K : Key.E);
            RefreshControls();
        }

        private IEnumerator UseFrom(string id, Vector3 origin)
        {
            var target = FindObjectsByType<SceneTarget>(FindObjectsSortMode.None).Single(t => t.id == id);
            bool found = false;
            foreach (var offset in new[] { Vector3.zero, Vector3.right * 0.35f, Vector3.left * 0.35f, Vector3.forward * 0.35f, Vector3.back * 0.35f,
                Vector3.right * 0.7f, Vector3.left * 0.7f, Vector3.forward * 0.7f, Vector3.back * 0.7f })
            {
                var point = origin + offset;
                if (HospitalScene.Occupied(point)) continue;
                Vector3 look = target.transform.position - (point + Vector3.up * 1.62f);
                look.y = 0;
                if (look.sqrMagnitude < 0.001f) continue;
                player.Teleport(point, Quaternion.LookRotation(look));
                player.Eye.transform.LookAt(target.transform.position);
                if (player.Focus(Available) != target) continue;
                yield return Until(() => !busy, "ready for " + id + " interaction");
                // Keep the fixture aim while the real game Update reads focus and the Use action.
                player.CanPlay = false;
                yield return Tap(State.settings.alternateBindings ? Key.K : Key.E);
                RefreshControls();
                found = true;
                break;
            }
            Require(found, $"No interactable sightline to {id} from {origin} at {target.transform.position}");
        }

        private Button ButtonMatching(string label)
        {
            return ui.GetComponentsInChildren<Button>().Single(b => b.GetComponentInChildren<Text>().text.Contains(label));
        }

        private bool ActivePageContains(string copy) =>
            ui.GetComponentsInChildren<Text>().Any(label => label.text.Contains(copy, StringComparison.Ordinal));

        private int StaffTranscriptCount(string id) => voice.Transcript.Count(entry => entry.LineId == id);

        private IEnumerator Choose(string label)
        {
            yield return Until(() => ui.IsModal && !busy && !savingDialogue, "modal for " + label);
            var button = ButtonMatching(label);
            Require(button.interactable, "enabled action " + label);
            for (int i = 0; i < 24 && EventSystem.current.currentSelectedGameObject != button.gameObject; i++)
                yield return Tap(Key.Tab);
            Require(EventSystem.current.currentSelectedGameObject == button.gameObject, "keyboard focused " + label);
            yield return Tap(Key.Enter);
        }

        private IEnumerator Tap(Key key)
        {
            yield return Until(() => !busy && !savingDialogue, "ready for tap " + key);
            yield return Hold(key, 0f);
        }

        private IEnumerator Hold(Key key, float duration)
        {
            qaKeys.MakeCurrent();
            InputSystem.QueueStateEvent(qaKeys, new KeyboardState(key));
            yield return Until(() =>
            {
                qaKeys.MakeCurrent();
                return qaKeys[key].isPressed;
            }, "queued keyboard input " + key);
            // Let the normal Update handlers observe the pressed state across simulated time before releasing it.
            float elapsed = 0f;
            do
            {
                qaKeys.MakeCurrent();
                yield return null;
                elapsed += Mathf.Min(Time.deltaTime, 0.045f);
            } while (elapsed < duration);
            qaKeys.MakeCurrent();
            InputSystem.QueueStateEvent(qaKeys, new KeyboardState());
            yield return Until(() =>
            {
                qaKeys.MakeCurrent();
                return !qaKeys[key].isPressed;
            }, "keyboard release " + key);
        }

        private static IEnumerator Until(Func<bool> ready, string step)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 6;
            while (!ready() && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Require(ready(), step);
        }

        private IEnumerator InspectLifetimeStress()
        {
            var document = Image("ObsCard");
            var backDoc = Image("ObsCard_Back");
            Require(document != null && backDoc != null, "dual-sided stress documents available");
            long before = Profiler.GetTotalAllocatedMemoryLong();
            for (int i = 0; i < 100; i++)
            {
                inspector.Open(null, document, (float)document.width / document.height, ActiveVolume(), backDoc);
                inspector.Flip();
                yield return null;
                inspector.Close();
                yield return null;
            }
            int meshes = Resources.FindObjectsOfTypeAll<Mesh>().Count(m => m.name is "InspectPaperFront" or "InspectPaperBack");
            int materials = Resources.FindObjectsOfTypeAll<Material>().Count(m => m.name is "InspectPaperFrontMat" or "InspectPaperBackMat");
            long after = Profiler.GetTotalAllocatedMemoryLong();
            Require(meshes == 0 && materials == 0, $"Inspector retains {meshes} meshes and {materials} materials");
            verification.Add($"INPUT STRESS PASS: 100 dual-sided flip open/close cycles; {meshes} live paper meshes, {materials} live paper materials, Unity allocated bytes {before}→{after}.");
        }
    }
}
