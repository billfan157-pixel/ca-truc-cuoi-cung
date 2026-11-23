using System.Collections;
using UnityEngine;

namespace CTCC
{
    public sealed partial class HospitalGame
    {
        private float dragStartPose;
        private float observeWardAgainAt;
        private float wardVisibleSeconds;
        private bool b07GateAttemptRunning;
        private const string WheelTrace = "BED4_WHEELS", BlanketTrace = "BED4_BLANKET", SlipperTrace = "BED4_SLIPPERS";

        private bool CanSpatialLinkBed4() =>
            State.layer == Reality.B &&
            casework.CanRevise &&
            State.f101Annotations.MarkedHotspot(ObservationRegistry.Bed4Card) == F101Annotations.Receiver &&
            HypothesisService.HasExaminedInLayerB(State, ObservationRegistry.Bed4Traces) &&
            !State.f101Annotations.HasLink;

        private string WorldPrompt(SceneTarget target)
        {
            if (target == null) return null;

            if (target.id == "GATE-CONG")
            {
                var eq = InventoryService.GetEquipped(State);
                if (eq?.itemDefId == BackpackCatalog.GateKey)
                    return "★ [" + UseKey + "]  Mở khóa cổng phụ bằng chìa đồng";
                if (eq?.itemDefId == BackpackCatalog.WingCKey)
                    return "[" + UseKey + "]  Thử chìa khu C trên ổ khóa cổng phụ";
                if (State.Happened("E10a.SMSRead") && InventoryService.HasPhysical(State, BackpackCatalog.GateKey))
                    return "[" + UseKey + "]  Mở khóa cổng phụ (dùng chìa cổng phụ)";
                return "[" + UseKey + "]  Kiểm tra ổ khóa cổng phụ";
            }

            if (target.id == "SEAM")
            {
                if (State.chapter == "CH01" && State.Happened("CH01-T08") && !State.Happened("CH01-T09"))
                    return "[" + UseKey + "]  Kéo mở cánh cổng phụ   ·   Giữ chuột trái để kéo";
                var eq = InventoryService.GetEquipped(State);
                if (eq != null && eq.itemDefId is (BackpackCatalog.GateKey or BackpackCatalog.WingCKey))
                    return "[" + UseKey + "]  Thử mở cổng bằng " + (BackpackCatalog.Get(eq.itemDefId)?.ShortLabel ?? "chìa khóa");
                return "[" + UseKey + "]  Kiểm tra cánh cổng phụ";
            }

            if (target.id is ("COMPARE_CLIP_AN02" or "COMPARE_DESK_STAFF" or "COMPARE_DESK_RECEPTION" or "COMPARE_DESK_101"))
            {
                var eq = InventoryService.GetEquipped(State);
                var eqDef = eq != null ? BackpackCatalog.Get(eq.itemDefId) : null;
                if (eqDef != null && WorldUseResolver.Resolve(State, eqDef.Id, target.id).Outcome == WorldUseOutcome.PlacePaperOnAnchor)
                {
                    string verb = target.id == "COMPARE_CLIP_AN02" ? "Kẹp" : "Đặt";
                    return $"★ [{UseKey}]  {verb} {eqDef.ShortLabel} lên {target.title.ToLowerInvariant()}";
                }
                string[] slots = WorldAnchors.SlotsForSurface(target.id);
                int count = 0;
                string firstLabel = null;
                for (int i = 0; i < slots.Length; i++)
                {
                    var p = InventoryService.GetPlacedAt(State, slots[i]);
                    if (p != null)
                    {
                        count++;
                        firstLabel ??= BackpackCatalog.Get(p.itemDefId)?.ShortLabel;
                    }
                }
                if (target.id == "COMPARE_DESK_101" && InventoryService.CanAssembleCh02Drawing(State))
                    return $"★ [{UseKey}]  Bàn Phòng 101 · Ghép 3 mảnh tranh (E11)";
                if (target.id == "COMPARE_CLIP_AN02")
                {
                    return count > 0
                        ? $"[{UseKey}]  Xem / rút {firstLabel ?? "tài liệu"} cạnh F101"
                        : $"[{UseKey}]  Kẹp đối chiếu cạnh F101";
                }
                return count > 0
                    ? $"[{UseKey}]  {target.title} ({count}/{slots.Length} đang đặt)"
                    : $"[{UseKey}]  {target.title}";
            }

            if (target.id == "AN04" && State.chapter == "CH04")
            {
                var eq = InventoryService.GetEquipped(State);
                if (eq?.itemDefId == BackpackCatalog.Ch04Tape)
                    return $"★ [{UseKey}]  Đặt băng họp E06a vào máy phát tại bàn trực";
            }

            string label = target.id switch
            {
                "AN01" when State.chapter == "CH01" && NeedsHub() =>
                    GateService.G01(State) && !State.Happened("CH01-T08") ? "Bàn giao phiếu tại sổ trực" : "Giở sổ trên quầy",
                "AN01" => "Mở sổ trực",
                "BOARD" => State.Happened("CH01-T04") ? "Xem & nhận bảng kẹp Phòng 101" : "Xem bảng kẹp trên quầy",
                "F101" when State.chapter == "CH02" => "Bảng kẹp 101   ·   Q Đổi lớp",
                "F101" => "Nhấc phiếu trên bảng kẹp",
                "LAN_TALK" => !State.Happened("CH01-T04") ? "Nói chuyện với Lan" : "Hỏi thêm Lan",
                "LAN_EMPTY_DESK" => "Nhìn quầy trực trống",
                "OBS_CLOCK_0317" => "Xem kim đồng hồ",
                "OBS101B" => "Cầm thẻ đầu giường",
                "OBS101C" => "Xem tờ kẹp ở giường 02",
                "OBS101D" => "Lấy phiếu trong ngăn",
                Ch01FieldObjects.BinPedal when scene.PedalBin?.HeldOpen == true => "Nhả bàn đạp",
                _ => target.title
            };

            if (target.id is ("OBS101A" or "OBS101A_BLANKET" or "OBS101A_SLIPPERS") && CanSpatialLinkBed4())
                return "[" + UseKey + "]  " + label + "   ·   [Q] Liên hệ AS-05B với dấu hiện trường";

            if (IsDraggablePhysicalRig(target.id) && scene.interactables != null)
            {
                bool open = scene.interactables.IntendedOpenness(target.id) > 0.5f;
                if (target.id == "DRAWER-NURSE" && !open)
                    return "[" + UseKey + "]  Kéo mạnh ngăn tủ   ·   [Q] Mở êm chốt   ·   Giữ chuột trái để kéo";
                string action = target.id == "CHAIR-STAFF" ? (open ? "Đẩy ghế vào" : "Kéo ghế ra") :
                    (open ? "Khép " : "Mở ") + label.ToLowerInvariant();
                return "[" + UseKey + "]  " + action + (target.id == "CURTAIN-101"
                    ? "   ·   Q Hé rèm   ·   Kéo chuột" : "   ·   Giữ chuột trái để kéo");
            }
            return "[" + UseKey + "]  " + label;
        }

        private void ToggleWorldProp(string id, bool quietNurseDrawer = false)
        {
            var rig = scene.interactables;
            if (rig == null) return;
            float before = rig.IntendedOpenness(id);
            if (id is "DOOR-101" or "DOOR-STAFF" or "CHAIR-STAFF")
            {
                // Keep the leaf from ending over Minh if he is on the jamb or in its swing.
                float current = rig.GetDragOpenness01(id);
                float threshold = id == "CHAIR-STAFF" ? 0.15f : 0.35f;
                rig.RestorePose(id, before > threshold ? 0f : 1f);
                bool blocked = HospitalScene.Occupied(player.Feet) || id == "CHAIR-STAFF" && !rig.StaffChairClear();
                rig.RestorePose(id, current);
                if (blocked)
                {
                    ui.Caption(id == "CHAIR-STAFF" ? "Chân ghế đang vướng." : "Lùi một bước để nhường chỗ cho cánh cửa.", 2.5f, true);
                    return;
                }
                if (id == "CHAIR-STAFF") rig.ToggleDrawer(id, scene.atmosphere);
                else rig.ToggleDoor(id, scene.atmosphere);
            }
            else if (id == "CURTAIN-101") rig.ToggleCurtain(scene.atmosphere);
            else rig.ToggleDrawer(id, scene.atmosphere, quiet: quietNurseDrawer);

            float after = rig.IntendedOpenness(id);
            SaveWorldPose(id, before);

            if (Options.captions && id == "CHAIR-STAFF")
                ui.Caption("[Chân ghế cọ nhẹ trên nền gạch]", 2f);
            else if (id == "DRAWER-NURSE" && after > 0.5f && before <= 0.5f)
            {
                if (quietNurseDrawer)
                    ui.Caption("[Chốt ngăn tủ bật nhẹ, trượt ra êm]", 2.2f);
                else
                    dreadDirector?.NotifyStaffDrawerForcePull();
            }
            else if (id == "DRAWER-DESK" && after > 0.5f && before <= 0.5f)
            {
                TryTriggerLanDrawerLook();
            }
        }

        private void TryTriggerLanDrawerLook()
        {
            if (State == null || State.chapter != "CH01" || State.layer != Reality.B ||
                !State.Happened("CH01-T03") || GateService.G01(State) || State.Happened("CH01-T08"))
                return;
            if (State.HasDreadEvent(DreadCatalog.B03LanDrawerLook)) return;
            if (lanDirector == null || lanDirector.IsCinematicActive || lanDirector.IsEncounterRunning) return;

            lanDirector.ReactToReceptionDrawer();
            SaveCheckpoint(() => casework.RecordDreadEventAsync(DreadCatalog.B03LanDrawerLook));
        }

        private void CancelWorldDrag()
        {
            var rig = scene.interactables;
            if (rig == null || !rig.IsDragging) return;
            string id = rig.DraggingId;
            rig.EndDrag(null);
            rig.RestorePose(id, dragStartPose);
            player.LookSuppressed = false;
        }

        private void SaveWorldPose(string id, float previous)
        {
            var rig = scene.interactables;
            float destination = rig.IntendedOpenness(id);
            SetDoorState(destination);
            if (Mathf.Abs(destination - previous) < 0.004f) return;
            SaveCheckpoint(() => casework.StorePropPoseAsync(id, destination), onFailure: () =>
            {
                rig.RestorePose(id, previous);
                SetDoorState(previous);
            });

            void SetDoorState(float pose)
            {
                if (id == "DOOR-101" && scene.roomDoor != null) scene.roomDoor.isOpen = pose > 0.15f;
                if (id == "DOOR-STAFF" && scene.staffCrossDoor != null) scene.staffCrossDoor.isOpen = pose > 0.15f;
            }
        }

        private void SpeakAtReception()
        {
            if (lanDirector == null || lanDirector.IsCinematicActive || busy || ui.IsModal || inspector.IsInspecting)
                return;

            // Pha 3: Player-initiated active cinematic dialogue scene before CH01-T04!
            if (!State.Happened("CH01-T04"))
            {
                if (lanDirector.StartCinematicEncounter(
                    LanEncounterLine,
                    skipOpeningGreeting: State.Happened("CH01-LAN-NOTICED"),
                    reducedMotion: Options.reduceMotion,
                    onComplete: () =>
                    {
                        SaveCheckpoint(() => casework.CompleteLanCinematicAsync(), () =>
                        {
                            dreadDirector?.NotifyMajorBeatQuietWindow(8f);
                            RefreshControls();
                        });
                    }))
                {
                    RefreshControls();
                }
                return;
            }

            string next = NextLanReflection();
            if (next != null) lanDirector.PlayOptionalLine(next, "Lan", Line(next), 0.25f, 0.5f, 0.22f);
        }

        private static readonly string[] LanReflections = { "CH01-D08", "CH01-D09", "CH01-D16" };

        private string NextLanReflection()
        {
            if (lanDirector == null || lanDirector.IsEncounterRunning || lanDirector.IsCinematicActive || voice == null || voice.IsSpeaking)
                return null;
            var transcript = voice.Transcript;
            foreach (string id in LanReflections)
            {
                int i = 0;
                while (i < transcript.Count && transcript[i].LineId != id) i++;
                if (i == transcript.Count) return id;
            }
            return null;
        }

        private void TriggerSpatialBed4Link()
        {
            if (!CanSpatialLinkBed4()) return;
            var from = new EvidenceAnnotation(ObservationRegistry.Bed4Card, F101Annotations.Receiver);
            var to = new EvidenceAnnotation(ObservationRegistry.Bed4Traces, F101Annotations.TraceGroup);
            SaveCheckpoint(() => casework.SetF101LinkAsync(from, to), () =>
            {
                scene.atmosphere?.From("paper", player.Feet + Vector3.up, 0.52f);
                ui.Toast("ĐÃ LIÊN HỆ NGUỒN TẠI HIỆN TRƯỜNG",
                    "Ô chữ ký trống trên thẻ AS-05B ↔ dấu sử dụng quanh hốc Giường 4. Có thể quay lại bảng kẹp F101 để lập luận.", 5.8f);
            });
        }

        private void TryUnlockOuterGate()
        {
            var equipped = InventoryService.GetEquipped(State);
            string eqId = equipped?.itemDefId ?? "";
            var res = WorldUseResolver.Resolve(State, eqId, "GATE-CONG");

            switch (res.Outcome)
            {
                case WorldUseOutcome.UnlockOuterGate:
                    SaveCheckpoint(() => casework.OpenGateAsync(player.Feet), () =>
                    {
                        handPresenter?.TriggerUseAnimation();
                        scene.atmosphere?.From("keyTurn", new Vector3(4f, 1.24f, -20.0f), 0.85f);
                        scene.atmosphere?.From("doorCreak", new Vector3(4f, 1.2f, -20.0f), 0.78f);
                        scene.atmosphere?.From("distantThunder", new Vector3(4f, 6f, -15f), 0.65f);
                        ui.Caption("Ổ khóa bật chốt, cánh cổng sắt nặng nề mở ra. [Tiếng mưa nặng hạt trên sân xi măng]", 4.2f);
                        // Schedule restrained demolition-sign wind knock a few seconds after entering courtyard
                        b01SignKnockAt = Time.unscaledTime + 3.4f;
                    });
                    break;

                case WorldUseOutcome.WrongKeyForOuterGate:
                    scene.atmosphere?.From("doorLatch", new Vector3(4f, 1.24f, -20.0f), 0.72f);
                    ui.Caption(res.Feedback, 4.4f, true);
                    break;

                case WorldUseOutcome.NeedSmsBeforeGate:
                    scene.atmosphere?.From("doorLatch", new Vector3(4f, 1.24f, -20.0f), 0.62f);
                    ui.Caption(res.Feedback, 4.2f, true);
                    break;

                default:
                    scene.atmosphere?.From("doorLatch", new Vector3(4f, 1.24f, -20.0f), 0.60f);
                    ui.Caption("Ổ khóa cổng phụ đang cài chặt. Kiểm tra bì thư của Bảy đặt cạnh xe.", 3.6f, true);
                    break;
            }
        }

        private void ExecuteB07GateAttempt()
        {
            if (b07GateAttemptRunning || busy) return;
            var equipped = InventoryService.GetEquipped(State);
            if (State.Happened("CH01-T09"))
            {
                if (equipped != null && equipped.itemDefId is (BackpackCatalog.GateKey or BackpackCatalog.WingCKey))
                {
                    var res = WorldUseResolver.Resolve(State, equipped.itemDefId, "SEAM");
                    scene.atmosphere?.From("doorLatch", new Vector3(4f, 1.24f, -20.0f), 0.72f);
                    ui.Caption(res.Feedback, 4.2f, true);
                }
                else
                {
                    scene.atmosphere?.From("doorLatch", new Vector3(4f, 1.24f, -20.0f), 0.60f);
                    ui.Caption("Cánh cổng sắt đã khóa chặt phía ngoài. Không tìm được đường ra — trở lại sổ trực.", 4.6f, true);
                }
                return;
            }

            StartCoroutine(RunB07PeakGateSeam());
        }

        private IEnumerator RunB07PeakGateSeam()
        {
            b07GateAttemptRunning = true;
            try
            {
                // Peak Horror #2: Metal slam outside gate + brief rain/ambience dip + bar shadow (safe under Reduced Motion)
                dreadDirector?.PlayPeakGateSlam(Options.reduceMotion);
                if (scene.outerGate?.pivot != null)
                    scene.interactables?.MoveDoor("GATE-CONG", scene.outerGate.pivot, 0f, Options.reduceMotion, null);
                yield return new WaitForSecondsRealtime(Options.reduceMotion ? 0.14f : 0.46f);
                TransitionAsync(() => casework.OutsideAttemptAsync(player.Feet), true, () =>
                {
                    voice?.Speak("CH01-D14", "Minh", Line("CH01-D14"));
                });
            }
            finally
            {
                b07GateAttemptRunning = false;
            }
        }

        private void WorldF101(Transform source)
        {
            if (!transaction.AtRequiredPlace(player.Feet, RealityTransaction.WardClipboard))
            {
                ui.Caption("Lại gần mặt bảng kẹp trong phòng.", 2.7f, true);
                return;
            }
            InspectRegisteredObservation(ObservationRegistry.F101Sheet, "LOC03", source: source, afterRead: Form);
        }

        private void ExamineFieldObject(SceneTarget target)
        {
            var item = target.fieldObject;
            var spec = item.Observation;
            string location = spec.AllowedLocations[0];
            if (spec.Id == Ch01FieldObjects.BinPedal && scene.PedalBin != null)
            {
                if (scene.PedalBin.HeldOpen) SetPedalBin(false);
                else SaveCheckpoint(() => casework.ObserveAsync(spec.Id, location), () => SetPedalBin(true));
                return;
            }
            if (spec.IsDocument)
            {
                InspectRegisteredObservation(spec.Id, location, source: target.transform);
                return;
            }

            SaveCheckpoint(() => casework.ObserveAsync(spec.Id, location), () =>
            {
                if (item.Cue != null) scene.atmosphere?.From(item.Cue, target.transform.position, .38f);
                if (target.inspectModel != null && inspector != null)
                {
                    bool handset = spec.Id == Ch01FieldObjects.Handset;
                    string contact = handset ? "handsetCradle" : spec.Id == Ch01FieldObjects.Thermos ? "ceramicContact" : "drawerSlide";
                    var position = target.Contact;
                    scene.atmosphere?.From(contact, position, .65f);
                    inspector.Open(target.inspectModel, null, 1f, ActiveVolume(), observationId: spec.Id,
                        source: position, liftModel: true, pickupSeconds: handset ? .29f : .24f);
                    if (handset)
                    {
                        scene.atmosphere?.ListenToHandset();
                        ui.Caption("[Tín hiệu ngắt quãng, rồi nhiễu trong ống nghe]", 2.6f, aboveModal: true);
                    }
                    void PutDown() => StartCoroutine(ReturnHeldProp(position, contact));
                    ui.ShowInspectSidebar(PlaceName(location), spec.Title, spec.FactSummary,
                        new[] { new ActionChoice("Đặt lại chỗ cũ", PutDown) }, PutDown, () =>
                        {
                            scene.atmosphere?.PutHandsetDown();
                            inspector.Close();
                        });
                }
                else
                {
                    string summary = spec.FactSummary;
                    // B01 Flashlight glare / oblique light response on exterior enamel plaque & demolition notice (always readable!)
                    if (spec.Id == "CH01_GATE_PLAQUE")
                    {
                        summary += player.IsTorchOn
                            ? " Dưới ánh đèn pin chiếu thẳng, lớp men ướt loá nhẹ nhưng vẫn rõ từng chữ «BỆNH VIỆN AN SINH»."
                            : " Dưới ánh đèn đường ngoài cổng, mặt men cũ vẫn đọc rõ tên viện.";
                    }
                    else if (spec.Id == "CH01_DEMOLITION")
                    {
                        summary += player.IsTorchOn
                            ? " Ánh đèn rọi nghiêng làm nổi rõ dòng ngày bàn giao 18/11/2012 trên mặt giấy mới."
                            : " Mép giấy ẩm mưa khẽ phập phồng theo từng đợt gió ngoài đường.";
                    }
                    ui.Toast(spec.Title, summary, 5.2f);
                }
            });
        }

        private IEnumerator ReturnHeldProp(Vector3 position, string contact)
        {
            if (busy || !inspector.IsInspecting) yield break;
            busy = true;
            ui.ModalInputBlocked = true;
            scene.atmosphere?.PutHandsetDown();
            yield return inspector.PutBack();
            scene.atmosphere?.From(contact, position, .72f);
            ui.ModalInputBlocked = false;
            busy = false;
            CloseModal();
        }

        private void SetPedalBin(bool open)
        {
            var bin = scene.PedalBin;
            if (bin == null || bin.HeldOpen == open) return;
            bin.SetOpen(open);
            scene.atmosphere?.From("enamelPedal", bin.transform.position, open ? .9f : .7f);
            if (open) ui.Caption("[Lò xo kéo nắp men]", 1.8f);
        }

        private static string TraceDescription(string detail) => detail switch
        {
            WheelTrace => "Hai vệt bánh đứt quãng nằm ngang nền gạch.",
            BlanketTrace => "Một góc chăn gập vào trong, còn lộ nếp gấp lệch.",
            SlipperTrace => "Hai chiếc dép nhỏ nằm khác hướng bên mép sàn.",
            _ => ""
        };

        private void ObserveBed4Trace(string detail, string label) =>
            SaveCheckpoint(() => casework.ObserveAsync(ObservationRegistry.Bed4Traces, "LOC03", detail: detail),
                () => ui.Toast(label, TraceDescription(detail), 4.6f));

        private bool TryRecordWardChange()
        {
            int revision = State.f101.revision;
            if (State.layer != Reality.B || revision == 0 || State.f101History[revision - 1].consequenceObserved ||
                Time.unscaledTime < observeWardAgainAt ||
                !scene.CanSeeWardChange(State.f101.current, player.Eye.transform))
            {
                wardVisibleSeconds = 0;
                return false;
            }
            wardVisibleSeconds += Mathf.Min(Time.unscaledDeltaTime, .06f);
            if (wardVisibleSeconds < .21f) return false;
            wardVisibleSeconds = 0;
            observeWardAgainAt = Time.unscaledTime + 2f;
            SaveCheckpoint(() => casework.ObserveF101ConsequenceAsync(revision), () =>
                ui.Caption(State.f101.current switch
                {
                    FormStatus.Affirmed => "Vách che đã khép, hàng giường trước mặt thẳng tắp.",
                    FormStatus.Unverified => "Rèm vẫn còn đó. Phía sau còn một khoảng trống.",
                    _ => "Từ đây thấy rõ hơn vết bánh trên nền."
                }, 4f));
            return true;
        }
    }
}
