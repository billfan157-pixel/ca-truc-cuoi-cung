using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CTCC
{
    /// <summary>
    /// Core v3.6.2 + Phase 2.5 Diegetic Direct-Surface 3D AN02 Clipboard Controller (Requirement G):
    /// - Default mode uses DiegeticAn02Surface directly on Room 101's physical wall clipboard CTCC_PROP_ClipboardF101
    ///   with ZERO 2D sidebar: player clicks straight on the 3D F101 paper (circle "0", neutral equal-step A/B/C boxes,
    ///   2-step cursive "N. H. Minh" ink signature + red stamp box, top-right corner lift for 2-sided 3D inspect) and the
    ///   pinned 3D margin slip beside it (free manual 4-digit HH:MM EX01 entry + Branch C sources & claim).
    /// - Preserves 100% of backend invariants: neutral equal-step A/B/C, read-only SpatialFieldVerified Bed-4 link provenance,
    ///   zero preset distractors on EX01, Esc draft preservation, Task02HardGateEnabled => false, and opt-in Large Print 130%.
    /// </summary>
    public sealed partial class HospitalGame
    {
        private enum An02View { Clipboard, RefuteBuilder, Ex01Timecode, Preview, Confirm }

        private DiegeticAn02Surface an02Surface;
        private bool an02Active;
        private bool an02LargePrint;
        private An02View an02View;
        private FirstPerson.CameraPoseSnapshot an02SavedPose;
        private Vector3 an02CamTargetPos;
        private Quaternion an02CamTargetRot;
        private readonly int[] ex01Digits = { -1, -1, -1, -1 }; // H1, H2, M1, M2 (-1 = neutral '-')
        private int ex01DigitCursor;
        private string ex01Feedback;
        private bool an02SlipShowsBranchC;

        /// <summary>
        /// Core v3.6.2 D1: Task 02 (Bed 2 transfer comparison OBS101C) stays non-blocking by default
        /// while DEC-V36-CH01-02 remains pending approval.
        /// </summary>
        internal bool Task02HardGateEnabled => false;
        internal bool IsAn02Clipboard3DActive => an02Active;
        internal bool IsAn02DirectSurfaceActive => an02Active && an02Surface != null && an02Surface.IsActive;

        private void SetWallF101Visible(bool visible)
        {
            if (scene?.f101WallRenderer != null)
                scene.f101WallRenderer.enabled = visible;
        }

        private void ResetAn02ClipboardState()
        {
            an02Surface?.Close();
            SetWallF101Visible(true);
            if (an02Active && player != null)
                player.RestoreCameraPose(an02SavedPose);
            an02Active = false;
            an02LargePrint = false;
            an02SlipShowsBranchC = false;
            an02View = An02View.Clipboard;
            ex01Digits[0] = ex01Digits[1] = ex01Digits[2] = ex01Digits[3] = -1;
            ex01DigitCursor = 0;
            ex01Feedback = null;
        }

        private void OpenAn02Clipboard3D()
        {
            if (State == null || !casework.CanRevise) { CloseModal(); return; }
            inspector?.Close();

            // Record F101 sheet as Examined cleanly before marking/drafting (so zero-line hotspot is immediately markable).
            if (!HypothesisService.HasExaminedInLayerB(State, ObservationRegistry.F101Sheet))
            {
                SaveCheckpoint(() => casework.ObserveAsync(ObservationRegistry.F101Sheet, "LOC03", ObservationStage.Examined),
                    EnterAn02Clipboard3DImmediate);
                return;
            }
            EnterAn02Clipboard3DImmediate();
        }

        private void EnterAn02Clipboard3DImmediate()
        {
            if (State == null || !casework.CanRevise) return;
            if (!an02Active && player != null && player.Eye != null)
            {
                an02SavedPose = player.CaptureCameraPose();
                an02Active = true;
                player.ExternalCameraControl = true;
                player.CanPlay = false;

                Vector3 clipCenter = scene.f101WallRenderer != null
                    ? scene.f101WallRenderer.bounds.center
                    : new Vector3(5.736f, 1.365f, 5.50f);
                // Crisp straight-on framing centered cleanly over the unobstructed F101 + pinned 3D margin slip pair!
                an02CamTargetPos = clipCenter + new Vector3(0.49f, 0.006f, 0.28f);
                Vector3 lookTarget = clipCenter + new Vector3(0.0f, -0.004f, 0.28f);
                an02CamTargetRot = Quaternion.LookRotation((lookTarget - an02CamTargetPos).normalized, Vector3.up);

                if (Options.reduceMotion || qaFolder != null)
                    player.Eye.transform.SetPositionAndRotation(an02CamTargetPos, an02CamTargetRot);
            }

            if (an02View is (An02View.Preview or An02View.Confirm))
                an02View = An02View.Clipboard;
            an02SlipShowsBranchC = reportDraft.Decision == FormStatus.Refuted;
            formFeedback = null;
            ex01Feedback = null;
            RenderAn02Clipboard3D();
        }

        private void ExitAn02Clipboard3D(bool clearDraft)
        {
            an02Surface?.Close();
            SetWallF101Visible(true);
            an02View = An02View.Clipboard;
            an02SlipShowsBranchC = false;
            formFeedback = null;
            ex01Feedback = null;
            if (clearDraft)
            {
                reportDraft = new F101Draft();
                an02LargePrint = false;
            }
            if (an02Active && player != null)
            {
                player.RestoreCameraPose(an02SavedPose);
                an02Active = false;
            }
            CloseModal();
        }

        internal void TickAn02Clipboard3D()
        {
            if (!an02Active || player?.Eye == null) return;
            if (!ui.IsModal)
            {
                an02Surface?.Close();
                player.RestoreCameraPose(an02SavedPose);
                an02Active = false;
                return;
            }

            // Smoothly ease camera onto the 3D wall clipboard unless reduceMotion is enabled
            float t = Options.reduceMotion ? 1f : 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime);
            player.Eye.transform.position = Vector3.Lerp(player.Eye.transform.position, an02CamTargetPos, t);
            player.Eye.transform.rotation = Quaternion.Slerp(player.Eye.transform.rotation, an02CamTargetRot, t);

            if (an02Surface != null)
            {
                an02Surface.InputBlocked = busy || ui.ModalInputBlocked;
                if (an02Surface.IsActive && ui.IsStandaloneOverlay)
                {
                    an02Surface.ProjectHotspots(projectedHotspotsBuf);
                    ui.Update3DHotspotMarkers(projectedHotspotsBuf);
                }
            }

            // Direct keyboard input: 0-9 / arrows / Backspace for EX01 free HH:MM (both on 3D slip & Large Print) + [F] to lift 2-sided F101
            if (!busy && !ui.ModalInputBlocked && Keyboard.current is Keyboard k)
            {
                bool allowDigits = (!an02LargePrint && !an02SlipShowsBranchC) || an02View == An02View.Ex01Timecode;
                if (allowDigits) HandleEx01KeyboardDigits(k);

                if (!an02LargePrint && k.fKey.wasPressedThisFrame)
                    LiftF101SheetToInspector();
            }
        }

        private void HandleEx01KeyboardDigits(Keyboard k)
        {
            if (CorroborationService.IsCorroborated(State)) return;
            int typed = ReadTypedDigit(k);
            if (typed >= 0)
            {
                int max = Ex01MaxDigitForSlot(ex01DigitCursor);
                ex01Digits[ex01DigitCursor] = Mathf.Clamp(typed, 0, max);
                if (ex01DigitCursor == 0 && ex01Digits[0] == 2 && ex01Digits[1] > 3)
                    ex01Digits[1] = 3;
                ex01DigitCursor = (ex01DigitCursor + 1) % 4;
                ex01Feedback = null;
                RenderAn02Clipboard3D();
                return;
            }
            if (k.backspaceKey.wasPressedThisFrame)
            {
                ex01Digits[ex01DigitCursor] = -1;
                ex01DigitCursor = (ex01DigitCursor + 3) % 4;
                ex01Feedback = null;
                RenderAn02Clipboard3D();
                return;
            }
            if (k.leftArrowKey.wasPressedThisFrame)
            {
                ex01DigitCursor = (ex01DigitCursor + 3) % 4;
                RenderAn02Clipboard3D();
            }
            else if (k.rightArrowKey.wasPressedThisFrame)
            {
                ex01DigitCursor = (ex01DigitCursor + 1) % 4;
                RenderAn02Clipboard3D();
            }
        }

        private static int ReadTypedDigit(Keyboard k)
        {
            if (k.digit0Key.wasPressedThisFrame || k.numpad0Key.wasPressedThisFrame) return 0;
            if (k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame) return 1;
            if (k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame) return 2;
            if (k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame) return 3;
            if (k.digit4Key.wasPressedThisFrame || k.numpad4Key.wasPressedThisFrame) return 4;
            if (k.digit5Key.wasPressedThisFrame || k.numpad5Key.wasPressedThisFrame) return 5;
            if (k.digit6Key.wasPressedThisFrame || k.numpad6Key.wasPressedThisFrame) return 6;
            if (k.digit7Key.wasPressedThisFrame || k.numpad7Key.wasPressedThisFrame) return 7;
            if (k.digit8Key.wasPressedThisFrame || k.numpad8Key.wasPressedThisFrame) return 8;
            if (k.digit9Key.wasPressedThisFrame || k.numpad9Key.wasPressedThisFrame) return 9;
            return -1;
        }

        private int Ex01MaxDigitForSlot(int slot) => slot switch
        {
            0 => 2,
            1 => ex01Digits[0] == 2 ? 3 : 9,
            2 => 5,
            _ => 9
        };

        private string FormatEx01Digits()
        {
            char D(int idx) => ex01Digits[idx] >= 0 ? (char)('0' + ex01Digits[idx]) : '-';
            return $"{D(0)}{D(1)}:{D(2)}{D(3)}";
        }

        private void StepEx01Digit(int slot, int delta)
        {
            int max = Ex01MaxDigitForSlot(slot);
            int cur = ex01Digits[slot];
            if (cur < 0) cur = delta >= 0 ? 0 : max;
            else cur = (cur + delta + (max + 1)) % (max + 1);
            ex01Digits[slot] = cur;
            if (slot == 0 && ex01Digits[0] == 2 && ex01Digits[1] > 3)
                ex01Digits[1] = 3;
            ex01DigitCursor = slot;
            ex01Feedback = null;
            RenderAn02Clipboard3D();
        }

        private void LiftF101SheetToInspector()
        {
            an02Surface?.Close();
            SetWallF101Visible(true);
            var spec = ObservationRegistry.Get(ObservationRegistry.F101Sheet);
            if (spec != null)
                OpenObservationInspector(spec, onReturn: EnterAn02Clipboard3DImmediate, returnLabel: "Quay lại bảng kẹp");
        }

        private void RenderAn02Clipboard3D()
        {
            if (State == null || !casework.CanRevise) { ExitAn02Clipboard3D(false); return; }

            // Phase 2.5 Default Mode: 100% Diegetic Direct-Surface 3D F101 + Pinned Slip (ZERO sidebar!)
            bool useDirectSurface = !an02LargePrint && an02Surface != null &&
                                    SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
            if (useDirectSurface)
            {
                RenderAn02DirectSurface3D();
                return;
            }

            // Opt-in Accessible Large Print 130% (and headless/QA safe fallback) sharing exact same state!
            an02Surface?.Close();
            SetWallF101Visible(true);
            if (an02View == An02View.Ex01Timecode) { RenderAn02Ex01MarginLarge(); return; }
            if (an02View == An02View.RefuteBuilder) { RenderAn02RefuteBuilderLarge(); return; }
            if (an02View is (An02View.Preview or An02View.Confirm)) { RenderAn02PreviewAndConfirmLarge(); return; }
            RenderAn02MainLarge();
        }

        private void RenderAn02DirectSurface3D()
        {
            Vector3 clipCenter = scene.f101WallRenderer != null
                ? scene.f101WallRenderer.bounds.center
                : new Vector3(5.736f, 1.365f, 5.50f);

            bool openedNow = !an02Surface.IsActive;
            if (openedNow)
                an02Surface.Open(clipCenter, Image("F101"), ActiveVolume(), Options.reduceMotion, HandleAn02SurfaceZoneClick);
            SetWallF101Visible(false);

            var surfaceState = BuildCurrentAn02SurfaceState();
            an02Surface.InputBlocked = busy || ui.ModalInputBlocked;
            an02Surface.Redraw(surfaceState);

            var chips = new[]
            {
                new ActionChoice("Soi 2 mặt [F]", LiftF101SheetToInspector),
                new ActionChoice("Chữ lớn [L]", ToggleAn02LargePrint),
                new ActionChoice("Rời bảng [Esc]", () => ExitAn02Clipboard3D(clearDraft: false))
            };
            string whisper = an02View == An02View.Confirm
                ? "Bước 2/2: Bấm ô đỏ trên phiếu để đóng dấu ca trực · Esc rời bảng kẹp"
                : an02View == An02View.Preview
                    ? "Bước 1/2: Bấm dòng chữ ký trên phiếu để ký N. H. Minh · Esc rời bảng kẹp"
                    : "Bấm trực tiếp lên phiếu F101 và giấy ghi chú · Gõ 0–9 cho mốc giờ · F soi 2 mặt · L chữ lớn · Esc rời bảng";

            if (openedNow || !ui.IsStandaloneOverlay)
            {
                ui.ShowStandaloneOverlay(
                    whisper,
                    () => ExitAn02Clipboard3D(clearDraft: false),
                    onClose: () => { an02Surface?.Close(); SetWallF101Visible(true); },
                    microChips: chips,
                    onLargePrintKey: ToggleAn02LargePrint,
                    backdropAlpha: 0.04f);
            }
            else
            {
                ui.SetStandaloneBottomWhisper(whisper);
            }
        }

        private void ToggleAn02LargePrint()
        {
            an02LargePrint = !an02LargePrint;
            RenderAn02Clipboard3D();
        }

        private An02SurfaceState BuildCurrentAn02SurfaceState()
        {
            bool zeroMarked = State.f101Annotations.MarkedHotspot(ObservationRegistry.F101Sheet) == F101Annotations.Zero;
            bool hasObsA = HypothesisService.HasExaminedInLayerB(State, ObservationRegistry.Bed4Traces);
            bool hasObsB = HypothesisService.HasExaminedInLayerB(State, ObservationRegistry.Bed4Card);
            bool cardMarked = State.f101Annotations.MarkedHotspot(ObservationRegistry.Bed4Card) == F101Annotations.Receiver;
            bool spatialLinked = State.f101Annotations.HasLink &&
                                 F101Annotations.ResolveLinkProvenance(State) == F101LinkProvenance.SpatialFieldVerified;
            bool hasClock = CorroborationService.HasClock(State);
            bool hasCardTime = CorroborationService.HasCardTimestamp(State);
            bool hasShift = CorroborationService.TryResolveShiftContext(State, out _);
            bool ex01Solved = CorroborationService.IsCorroborated(State);

            var sources = new List<(string id, string label, bool selected)>();
            foreach (var rec in State.observationRecords)
            {
                if (rec.stage < ObservationStage.Examined) continue;
                var spec = ObservationRegistry.Get(rec.id);
                if (spec == null || !spec.SelectableForF101) continue;
                sources.Add((spec.Id, ShortSourceName(spec), reportDraft.Includes(spec.Id)));
            }
            var claims = new List<(string id, string label, bool selected)>();
            foreach (var c in HypothesisService.F101Claims)
                claims.Add((c.Id, c.Label, reportDraft.ClaimId == c.Id));

            int signStep = an02View == An02View.Confirm ? 2 : an02View == An02View.Preview ? 1 : 0;
            string fb = an02SlipShowsBranchC ? formFeedback : (!string.IsNullOrEmpty(ex01Feedback) ? ex01Feedback : formFeedback);

            return new An02SurfaceState(
                reportDraft.Decision,
                State.f101.current,
                State.f101.revision,
                zeroMarked,
                hasObsA,
                hasObsB,
                cardMarked,
                spatialLinked,
                hasClock,
                hasCardTime,
                hasShift,
                ex01Solved,
                ex01Digits,
                ex01DigitCursor,
                an02SlipShowsBranchC,
                signStep,
                fb,
                sources,
                claims);
        }

        private void HandleAn02SurfaceZoneClick(string zoneId)
        {
            if (State == null || busy || string.IsNullOrEmpty(zoneId)) return;

            if (zoneId == "LIFT_F101") { LiftF101SheetToInspector(); return; }
            if (zoneId == "SLIP_TAB_NOTES") { an02SlipShowsBranchC = false; RenderAn02Clipboard3D(); return; }
            if (zoneId == "SLIP_TAB_C") { an02SlipShowsBranchC = true; RenderAn02Clipboard3D(); return; }

            if (zoneId == "MARK_ZERO")
            {
                SaveCheckpoint(() => casework.ToggleF101MarkAsync(ObservationRegistry.F101Sheet, F101Annotations.Zero), RenderAn02Clipboard3D);
                return;
            }
            if (zoneId == "MARK_CARD")
            {
                SaveCheckpoint(() => casework.ToggleF101MarkAsync(ObservationRegistry.Bed4Card, F101Annotations.Receiver), RenderAn02Clipboard3D);
                return;
            }
            if (zoneId == "OPT_A")
            {
                reportDraft.Decision = FormStatus.Affirmed;
                an02View = An02View.Clipboard;
                formFeedback = null;
                RenderAn02Clipboard3D();
                return;
            }
            if (zoneId == "OPT_B")
            {
                reportDraft.Decision = FormStatus.Unverified;
                an02View = An02View.Clipboard;
                formFeedback = null;
                RenderAn02Clipboard3D();
                return;
            }
            if (zoneId == "OPT_C")
            {
                reportDraft.Decision = FormStatus.Refuted;
                an02View = An02View.Clipboard;
                an02SlipShowsBranchC = true;
                formFeedback = null;
                RenderAn02Clipboard3D();
                return;
            }
            if (zoneId == "SIGN_PREVIEW")
            {
                ValidateAndPreviewAn02();
                return;
            }
            if (zoneId == "SIGN_NAME")
            {
                an02View = An02View.Confirm;
                formFeedback = null;
                RenderAn02Clipboard3D();
                return;
            }
            if (zoneId == "SIGN_STAMP")
            {
                var submission = reportDraft.Submission(State.f101Annotations);
                StartCoroutine(SignAn02F101(reportDraft.Decision, submission));
                return;
            }
            if (zoneId == "SIGN_CANCEL")
            {
                an02View = An02View.Clipboard;
                formFeedback = null;
                RenderAn02Clipboard3D();
                return;
            }
            if (zoneId.StartsWith("EX01_D", StringComparison.Ordinal) && zoneId.Length == 7 &&
                int.TryParse(zoneId.Substring(6, 1), out int slot) && slot >= 0 && slot < 4)
            {
                StepEx01Digit(slot, +1);
                return;
            }
            if (zoneId == "EX01_RESET")
            {
                ex01Digits[0] = ex01Digits[1] = ex01Digits[2] = ex01Digits[3] = -1;
                ex01DigitCursor = 0;
                ex01Feedback = null;
                RenderAn02Clipboard3D();
                return;
            }
            if (zoneId == "EX01_SUBMIT")
            {
                SubmitAn02Ex01Timecode();
                return;
            }
            if (zoneId.StartsWith("SRC_", StringComparison.Ordinal))
            {
                string obsId = zoneId.Substring(4);
                reportDraft.Toggle(obsId);
                if (an02View is (An02View.Preview or An02View.Confirm)) an02View = An02View.Clipboard;
                formFeedback = null;
                RenderAn02Clipboard3D();
                return;
            }
            if (zoneId.StartsWith("CLM_", StringComparison.Ordinal))
            {
                reportDraft.ClaimId = zoneId.Substring(4);
                if (an02View is (An02View.Preview or An02View.Confirm)) an02View = An02View.Clipboard;
                formFeedback = null;
                RenderAn02Clipboard3D();
            }
        }

        // ==================== OPT-IN ACCESSIBLE LARGE PRINT 130% VIEWS ====================

        private void RenderAn02MainLarge()
        {
            bool zeroMarked = State.f101Annotations.MarkedHotspot(ObservationRegistry.F101Sheet) == F101Annotations.Zero;
            bool hasObsA = HypothesisService.HasExaminedInLayerB(State, ObservationRegistry.Bed4Traces);
            bool hasObsB = HypothesisService.HasExaminedInLayerB(State, ObservationRegistry.Bed4Card);
            bool hasObsC = HypothesisService.HasExaminedInLayerB(State, ObservationRegistry.Bed2Transfer);
            bool cardMarked = State.f101Annotations.MarkedHotspot(ObservationRegistry.Bed4Card) == F101Annotations.Receiver;
            bool spatialLinked = State.f101Annotations.HasLink &&
                                 F101Annotations.ResolveLinkProvenance(State) == F101LinkProvenance.SpatialFieldVerified;
            bool ex01Solved = CorroborationService.IsCorroborated(State);

            string revLine = State.f101.revision == 0
                ? "Trạng thái: Phiếu chưa ký"
                : $"Bản hiện hành: Lần {State.f101.revision} · {Status(State.f101.current)} (Lần đầu: {Status(State.f101.firstDecision)})";

            var sb = new StringBuilder();
            sb.AppendLine("Bệnh viện An Sinh · Phòng 101 · Ca đêm 18/11/2005");
            sb.AppendLine(revLine);
            sb.AppendLine();
            sb.AppendLine("THEO BẢNG BÀN GIAO: Bệnh nhân chưa bàn giao: 0" + (zeroMarked ? "   [✓ ĐÃ ĐÁNH DẤU SỐ 0]" : ""));
            sb.AppendLine("• Giường 01–03 đã ghi chốt trên bảng in; không có dòng giường 04.");
            if (hasObsC)
                sb.AppendLine("• Phiếu chuyển Giường 02: chuyển Nội B có đủ 2 chữ ký giao–nhận.");
            sb.AppendLine();
            sb.AppendLine("GHI NHẬN TẠI CHỖ:");
            if (!hasObsA && !hasObsB)
                sb.AppendLine("• Chưa có quan sát tại hốc giường 4 (A và B không yêu cầu quan sát).");
            else
            {
                if (hasObsA) sb.AppendLine("• Dấu chăn gấp vội & đôi dép nhỏ sau rèm giường 4.");
                if (hasObsB) sb.AppendLine("• Thẻ AS-05B giường 4 còn trống ô ký nhận" + (cardMarked ? " [✓ ĐÃ ĐÁNH DẤU Ô NHẬN]." : "."));
                sb.AppendLine(spatialLinked
                    ? "• Đối chứng hiện trường: ✓ Đã liên hệ trực tiếp thẻ AS-05B với dấu tại hốc giường 4."
                    : "• Đối chứng hiện trường: Chưa đối chứng trực tiếp tại hốc giường 4.");
            }
            if (ex01Solved)
                sb.AppendLine("• Ghi chú mốc giờ ngoại lệ: ✓ Đã ghi nhận mốc trùng 03:17.");
            if (!string.IsNullOrEmpty(formFeedback))
                sb.AppendLine().AppendLine("⚠ " + formFeedback);

            var choices = new List<ActionChoice>
            {
                new(Check(zeroMarked) + "Đánh dấu dòng số 0 trên phiếu F101", () =>
                    SaveCheckpoint(() => casework.ToggleF101MarkAsync(ObservationRegistry.F101Sheet, F101Annotations.Zero), RenderAn02Clipboard3D)),
            };
            if (hasObsB)
            {
                choices.Add(new ActionChoice(Check(cardMarked) + "Đánh dấu ô ký nhận trống trên thẻ AS-05B", () =>
                    SaveCheckpoint(() => casework.ToggleF101MarkAsync(ObservationRegistry.Bed4Card, F101Annotations.Receiver), RenderAn02Clipboard3D)));
            }

            choices.Add(new ActionChoice(Check(reportDraft.Decision == FormStatus.Affirmed) + "A · Xác nhận theo bản kê: 0 bệnh nhân chưa bàn giao", () =>
            {
                reportDraft.Decision = FormStatus.Affirmed;
                formFeedback = null;
                RenderAn02Clipboard3D();
            }));
            choices.Add(new ActionChoice(Check(reportDraft.Decision == FormStatus.Unverified) + "B · Chưa đủ thông tin để xác nhận. Giữ phiếu ở trạng thái chờ", () =>
            {
                reportDraft.Decision = FormStatus.Unverified;
                formFeedback = null;
                RenderAn02Clipboard3D();
            }));
            choices.Add(new ActionChoice(Check(reportDraft.Decision == FormStatus.Refuted) + "C · Ghi nhận số 0 chưa đủ căn cứ xác nhận (chọn nguồn & kết luận)", () =>
            {
                reportDraft.Decision = FormStatus.Refuted;
                formFeedback = null;
                an02View = An02View.RefuteBuilder;
                RenderAn02Clipboard3D();
            }));

            choices.Add(new ActionChoice("Đọc lại trên phiếu (xác nhận trước khi ký)", ValidateAndPreviewAn02,
                reportDraft.Decision != FormStatus.Pending));
            choices.Add(new ActionChoice(ex01Solved
                ? "Ghi chú mốc giờ ngoại lệ (Đã ghi 03:17)"
                : "Ghi chú mốc giờ ngoại lệ · Tự nhập HH:MM", () =>
            {
                an02View = An02View.Ex01Timecode;
                ex01Feedback = null;
                RenderAn02Clipboard3D();
            }));
            choices.Add(new ActionChoice("Nhấc tờ F101 soi 3D hai mặt", LiftF101SheetToInspector));
            if (qaFolder == null && an02Surface != null)
                choices.Add(new ActionChoice("Quay lại mặt giấy 3D trực tiếp", ToggleAn02LargePrint));
            choices.Add(new ActionChoice("Rời bảng kẹp (giữ bản nháp)", () => ExitAn02Clipboard3D(clearDraft: false)));

            ui.Show("BẢNG KẸP PHÒNG 101  ·  CHỮ LỚN", "Phiếu bàn giao ca đêm F101", sb.ToString().TrimEnd(),
                choices.ToArray(), () => ExitAn02Clipboard3D(clearDraft: false), Image("F101"), largePrint: true);
        }

        private void RenderAn02RefuteBuilderLarge()
        {
            var sb = new StringBuilder();
            bool zeroMarked = State.f101Annotations.MarkedHotspot(ObservationRegistry.F101Sheet) == F101Annotations.Zero;
            bool cardMarked = State.f101Annotations.MarkedHotspot(ObservationRegistry.Bed4Card) == F101Annotations.Receiver;
            bool spatialLinked = State.f101Annotations.HasLink &&
                                 F101Annotations.ResolveLinkProvenance(State) == F101LinkProvenance.SpatialFieldVerified;

            sb.AppendLine("LẬP LUẬN NHÁNH C · SỐ 0 CHƯA ĐỦ CĂN CỨ XÁC NHẬN");
            sb.AppendLine($"• Đánh dấu dòng số 0 trên F101: {(zeroMarked ? "✓ Đã đánh dấu" : "Chưa đánh dấu")}");
            sb.AppendLine($"• Đánh dấu ô ký nhận trên AS-05B: {(cardMarked ? "✓ Đã đánh dấu" : "Chưa đánh dấu")}");
            sb.AppendLine($"• Đối chứng trực tiếp tại hốc giường 4: {(spatialLinked ? "✓ Đã xác thực tại hiện trường" : "Chưa đối chứng tại hốc giường 4 (cầm thẻ AS-05B đã đánh dấu ra sau rèm)")}");
            sb.AppendLine($"• Số nguồn đang kẹp vào phiếu: {reportDraft.SourceCount}");
            if (!string.IsNullOrEmpty(formFeedback))
                sb.AppendLine().AppendLine("⚠ " + formFeedback);

            var choices = new List<ActionChoice>();
            foreach (var rec in State.observationRecords)
            {
                if (rec.stage < ObservationStage.Examined) continue;
                var spec = ObservationRegistry.Get(rec.id);
                if (spec == null || !spec.SelectableForF101) continue;
                string obsId = spec.Id;
                bool attached = reportDraft.Includes(obsId);
                choices.Add(new ActionChoice(Check(attached) + "Kẹp nguồn: " + ShortSourceName(spec), () =>
                {
                    reportDraft.Toggle(obsId);
                    formFeedback = null;
                    RenderAn02Clipboard3D();
                }));
            }
            foreach (var claim in HypothesisService.F101Claims)
            {
                string cid = claim.Id;
                choices.Add(new ActionChoice((reportDraft.ClaimId == cid ? "●  " : "○  ") + claim.Label, () =>
                {
                    reportDraft.ClaimId = cid;
                    formFeedback = null;
                    RenderAn02Clipboard3D();
                }));
            }
            choices.Add(new ActionChoice("Đọc lại trên phiếu (kiểm tra căn cứ)", ValidateAndPreviewAn02));
            choices.Add(new ActionChoice("Quay lại mặt phiếu", () =>
            {
                an02View = An02View.Clipboard;
                formFeedback = null;
                RenderAn02Clipboard3D();
            }));

            ui.Show("BẢNG KẸP PHÒNG 101  ·  NHÁNH C", "Chọn nguồn đối chiếu & kết luận", sb.ToString().TrimEnd(),
                choices.ToArray(), () => { an02View = An02View.Clipboard; RenderAn02Clipboard3D(); }, Image("F101"), largePrint: true);
        }

        private void RenderAn02Ex01MarginLarge()
        {
            bool hasClock = CorroborationService.HasClock(State);
            bool hasCardTime = CorroborationService.HasCardTimestamp(State);
            bool hasShift = CorroborationService.TryResolveShiftContext(State, out string ctxId);
            bool solved = CorroborationService.IsCorroborated(State);
            string formatted = FormatEx01Digits();

            var sb = new StringBuilder();
            sb.AppendLine("GHI CHÚ MỐC GIỜ NGOẠI LỆ (TÙY CHỌN)");
            sb.AppendLine("Ghi mốc giờ lặp lại giữa hiện trường và giấy tờ ca trực — không sửa nguyên văn thẻ và không suy đoán thời điểm tử vong.");
            sb.AppendLine();
            sb.AppendLine($"• Đồng hồ quầy tiếp nhận: {(hasClock ? "✓ Đã quan sát" : "Chưa kiểm")}");
            sb.AppendLine($"• Vết hằn mặt sau thẻ AS-05B (soi nghiêng / chữ lớn): {(hasCardTime ? "✓ Đã đọc vết hằn" : "Chưa soi mặt sau")}");
            sb.AppendLine($"• Tài liệu bối cảnh ca trực: {(hasShift ? "✓ " + (ObservationRegistry.Get(ctxId)?.Title ?? ctxId) : "Chưa kiểm")}");
            sb.AppendLine();

            var choices = new List<ActionChoice>();
            if (solved)
            {
                sb.AppendLine("✓ ĐÃ GHI NHẬN MỐC TRÙNG: 03:17");
                sb.AppendLine("Đồng hồ quầy và vết hằn mặt sau thẻ AS-05B trùng cùng một mốc giờ — chưa xác định sự kiện, thời điểm tử vong hay số phận người bệnh.");
            }
            else
            {
                sb.AppendLine($"MỐC GIỜ ĐANG NHẬP (HH:MM):   [ {formatted} ]   (ô đang chọn: #{ex01DigitCursor + 1})");
                sb.AppendLine("Tự điều chỉnh từng chữ số bên dưới hoặc gõ trực tiếp phím số 0–9 trên bàn phím.");
                if (!string.IsNullOrEmpty(ex01Feedback))
                    sb.AppendLine().AppendLine("⚠ " + ex01Feedback);

                string[] slotLabels = { "Giờ (chục)", "Giờ (đơn vị)", "Phút (chục)", "Phút (đơn vị)" };
                for (int i = 0; i < 4; i++)
                {
                    int slot = i;
                    string val = ex01Digits[slot] >= 0 ? ex01Digits[slot].ToString() : "-";
                    string curMark = ex01DigitCursor == slot ? "▸ " : "  ";
                    choices.Add(new ActionChoice($"{curMark}{slotLabels[slot]}: [{val}]  (+1)", () => StepEx01Digit(slot, +1)));
                }
                choices.Add(new ActionChoice("Đặt lại (--:--)", () =>
                {
                    ex01Digits[0] = ex01Digits[1] = ex01Digits[2] = ex01Digits[3] = -1;
                    ex01DigitCursor = 0;
                    ex01Feedback = null;
                    RenderAn02Clipboard3D();
                }));
                choices.Add(new ActionChoice($"Ghi nhận mốc giờ [{formatted}]", SubmitAn02Ex01Timecode));
            }
            choices.Add(new ActionChoice("Quay lại mặt phiếu F101", () =>
            {
                an02View = An02View.Clipboard;
                ex01Feedback = null;
                RenderAn02Clipboard3D();
            }));

            ui.Show("BẢNG KẸP PHÒNG 101  ·  MỐC GIỜ", "Ghi chú mốc giờ (Tự nhập HH:MM)", sb.ToString().TrimEnd(),
                choices.ToArray(), () => { an02View = An02View.Clipboard; RenderAn02Clipboard3D(); }, Image("F101"), largePrint: true);
        }

        private void SubmitAn02Ex01Timecode()
        {
            if (CorroborationService.IsCorroborated(State)) return;
            if (!CorroborationService.CanCorroborate0317(State))
            {
                ex01Feedback = !CorroborationService.HasClock(State)
                    ? "Cần kiểm trực tiếp chiếc đồng hồ treo sau quầy tiếp nhận trước."
                    : !CorroborationService.HasCardTimestamp(State)
                        ? "Cần soi nghiêng (hoặc mở bản chữ lớn) mặt sau thẻ AS-05B để đọc vết hằn giờ trước."
                        : "Cần xem thêm một tài liệu bối cảnh ca trực (sổ ngăn kéo quầy hoặc quy chế/thông báo phòng nhân viên) trước khi ghi mốc ngoại lệ.";
                RenderAn02Clipboard3D();
                return;
            }
            string raw = FormatEx01Digits();
            if (string.IsNullOrEmpty(CorroborationService.NormalizeTimecode(raw)))
            {
                ex01Feedback = "Cần nhập đủ 4 chữ số giờ và phút hợp lệ (00:00–23:59) trước khi ghi nhận.";
                RenderAn02Clipboard3D();
                return;
            }
            SaveCheckpoint(() => casework.SubmitEx01TimecodeAsync(raw), () =>
            {
                if (CorroborationService.IsCorroborated(State))
                {
                    scene.atmosphere?.From("signaturePen", scene.f101WallRenderer != null ? scene.f101WallRenderer.bounds.center : player.Feet + Vector3.up, 0.72f);
                    ex01Feedback = null;
                    ui.Toast("ĐÃ GHI CHÚ MỐC GIỜ · 03:17",
                        "Mốc 03:17 trùng giữa đồng hồ quầy và vết hằn mặt sau thẻ AS-05B (chưa xác định sự kiện hay giờ tử vong).", 5.5f);
                }
                else
                {
                    ex01Feedback = $"Mốc [{raw}] chưa khớp với giờ trên đồng hồ quầy và vết hằn mặt sau thẻ AS-05B.";
                }
                RenderAn02Clipboard3D();
            });
        }

        private void ValidateAndPreviewAn02()
        {
            var submission = reportDraft.Submission(State.f101Annotations);
            var eval = HypothesisService.EvaluateF101LiveCommit(State, reportDraft.Decision, submission);
            if (!eval.Accepted)
            {
                formFeedback = eval.Message;
                if (!string.IsNullOrEmpty(eval.ProgressiveHint))
                    formFeedback += "\nGợi ý: " + eval.ProgressiveHint;
                if (reportDraft.Decision == FormStatus.Refuted)
                    an02SlipShowsBranchC = true;
                RenderAn02Clipboard3D();
                return;
            }
            formFeedback = null;
            an02View = An02View.Preview;
            RenderAn02Clipboard3D();
        }

        private void RenderAn02PreviewAndConfirmLarge()
        {
            bool confirmStep = an02View == An02View.Confirm;
            var submission = reportDraft.Submission(State.f101Annotations);
            var sb = new StringBuilder();
            sb.AppendLine(confirmStep ? "XÁC NHẬN LẦN HAI · ĐÓNG DẤU BÀN GIAO" : "XEM TRƯỚC NỘI DUNG SẼ KÝ TRÊN F101");
            sb.AppendLine();
            sb.AppendLine(FormNotation(reportDraft.Decision));
            if (reportDraft.Decision == FormStatus.Refuted && submission != null)
            {
                sb.AppendLine();
                sb.AppendLine("Kết luận: " + (HypothesisService.GetClaim(submission.claimId)?.Label ?? ""));
                sb.AppendLine("Nguồn kèm theo:");
                foreach (string id in submission.selectedEvidenceIds)
                    if (ObservationRegistry.Get(id) is ObservationSpec spec)
                        sb.Append("• ").AppendLine(ShortSourceName(spec));
            }
            sb.AppendLine();
            sb.AppendLine(confirmStep
                ? "Chữ ký đã đặt nét: N. H. Minh\nBạn sẽ ký nội dung trên. Có thể kiểm tra và sửa phiếu sau."
                : "Bạn sẽ ký nội dung trên. Có thể kiểm tra và sửa phiếu sau.");
            if (!string.IsNullOrEmpty(formFeedback))
                sb.AppendLine().AppendLine("⚠ " + formFeedback);

            var choices = new List<ActionChoice>();
            if (!confirmStep)
            {
                choices.Add(new ActionChoice("Ký tên: N. H. Minh", () =>
                {
                    var paperPos = scene.f101WallRenderer != null ? scene.f101WallRenderer.bounds.center : player.Feet + Vector3.up;
                    scene.atmosphere?.From("signaturePen", paperPos, 0.78f);
                    an02View = An02View.Confirm;
                    formFeedback = null;
                    RenderAn02Clipboard3D();
                }));
                choices.Add(new ActionChoice("Sửa lại phần ghi", () =>
                {
                    an02View = reportDraft.Decision == FormStatus.Refuted ? An02View.RefuteBuilder : An02View.Clipboard;
                    formFeedback = null;
                    RenderAn02Clipboard3D();
                }));
            }
            else
            {
                choices.Add(new ActionChoice("Xác nhận & đóng dấu ca trực", () =>
                    StartCoroutine(SignAn02F101(reportDraft.Decision, submission))));
                choices.Add(new ActionChoice("Quay lại chỉnh sửa (chưa đóng dấu)", () =>
                {
                    an02View = An02View.Clipboard;
                    formFeedback = null;
                    RenderAn02Clipboard3D();
                }));
            }

            ui.Show(confirmStep ? "BẢNG KẸP PHÒNG 101  ·  ĐÓNG DẤU" : "BẢNG KẸP PHÒNG 101  ·  XEM TRƯỚC",
                confirmStep ? "Xác nhận chữ ký N. H. Minh" : "Đọc lại trước khi ký", sb.ToString().TrimEnd(),
                choices.ToArray(), () => { an02View = An02View.Clipboard; RenderAn02Clipboard3D(); }, Image("F101"), largePrint: true);
        }

        private IEnumerator SignAn02F101(FormStatus choice, DeductionSubmission submission)
        {
            if (busy || an02View != An02View.Confirm) yield break;
            if (!casework.CanRevise || !transaction.AtRequiredPlace(player.Feet, RealityTransaction.WardClipboard))
            {
                formFeedback = "Mình cần đứng ở vùng an toàn cạnh bảng kẹp trong phòng để ký.";
                RenderAn02Clipboard3D();
                yield break;
            }

            int previousRevision = State.f101.revision;
            Vector3 paperPos = scene.f101WallRenderer != null ? scene.f101WallRenderer.bounds.center : player.Feet + Vector3.up;
            bool committed = false;
            busy = true;
            ui.ModalInputBlocked = true;
            RefreshControls();
            try
            {
                while (savingDialogue || streaming) yield return null;
                var task = casework.SubmitF101DetailedAsync(choice, submission, player.Feet);
                while (!task.IsCompleted) yield return null;
                if (task.IsFaulted) Debug.LogException(task.Exception);
                var result = task.IsFaulted || task.IsCanceled ? CommitResult.SaveUnavailable : task.Result.Result;
                if (result != CommitResult.Applied)
                {
                    formFeedback = result switch
                    {
                        CommitResult.PadRequired => task.Result.Evaluation.Message,
                        CommitResult.Obstructed => "Đang đứng trong vùng biến đổi hốc giường 4. Hãy bước về gần cửa rồi ký lại.",
                        _ => "Chưa ghi được bản lưu. Bản nháp và chữ ký vẫn được giữ nguyên."
                    };
                }
                else
                {
                    committed = true;
                    if (State.f101.revision != previousRevision)
                    {
                        dreadDirector?.NotifyF101Signing();
                        voice?.StopCurrent();
                        scene.atmosphere?.From("stamp", paperPos);
                        scene.atmosphere?.LeaveSilenceAfterStamp(1.25f);
                        ui.Caption("[Dấu cao su chạm giấy · N. H. Minh]", 1.3f, aboveModal: true);
                        yield return new WaitForSecondsRealtime(Options.reduceMotion ? 0.10f : 0.45f);
                        StartCoroutine(PlayF101Signature(State.f101.current, State.f101.revision));
                    }
                }
            }
            finally
            {
                ui.ModalInputBlocked = false;
                busy = false;
                RefreshControls();
            }

            if (committed)
                ExitAn02Clipboard3D(clearDraft: true);
            else
                RenderAn02Clipboard3D();
        }
    }
}
