using System.Collections;
using UnityEngine;

namespace CTCC
{
    public sealed partial class HospitalGame
    {
        private float dragStartPose;
        private float observeWardAgainAt;
        private float wardVisibleSeconds;
        private const string WheelTrace = "BED4_WHEELS", BlanketTrace = "BED4_BLANKET", SlipperTrace = "BED4_SLIPPERS";

        private string WorldPrompt(SceneTarget target)
        {
            if (target == null) return null;
            string label = target.id switch
            {
                "AN01" when State.chapter == "CH01" && NeedsHub() =>
                    GateService.G01(State) && !State.Happened("CH01-T08") ? "Bàn giao phiếu tại sổ trực" : "Giở sổ trên quầy",
                "AN01" => "Mở sổ trực",
                "BOARD" => "Nhận bảng kẹp",
                "F101" when State.chapter == "CH02" => "Bảng kẹp 101   ·   Q Đổi lớp",
                "F101" => "Nhấc phiếu trên bảng kẹp",
                "LAN_TALK" => "Nói với Lan",
                "OBS_CLOCK_0317" => "Xem kim đồng hồ",
                "OBS101B" => "Cầm thẻ đầu giường",
                "OBS101C" => "Xem tờ kẹp ở giường 02",
                "OBS101D" => "Lấy phiếu trong ngăn",
                Ch01FieldObjects.BinPedal when scene.PedalBin?.HeldOpen == true => "Nhả bàn đạp",
                _ => target.title
            };
            if (IsDraggablePhysicalRig(target.id) && scene.interactables != null)
            {
                bool open = scene.interactables.IntendedOpenness(target.id) > 0.5f;
                string action = target.id == "CHAIR-STAFF" ? (open ? "Đẩy ghế vào" : "Kéo ghế ra") :
                    (open ? "Khép " : "Mở ") + label.ToLowerInvariant();
                return "[" + UseKey + "]  " + action + (target.id == "CURTAIN-101"
                    ? "   ·   Q Hé rèm   ·   Kéo chuột" : "   ·   Giữ chuột trái để kéo");
            }
            return "[" + UseKey + "]  " + label;
        }

        private void ToggleWorldProp(string id)
        {
            var rig = scene.interactables;
            if (rig == null) return;
            float before = rig.IntendedOpenness(id);
            if (id is "DOOR-101" or "DOOR-STAFF" or "CHAIR-STAFF")
            {
                // Keep the leaf from ending over Minh if he is on the jamb or in its swing.
                float current = rig.GetDragOpenness01(id);
                rig.RestorePose(id, before > 0.15f ? 0f : 1f);
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
            else rig.ToggleDrawer(id, scene.atmosphere);
            SaveWorldPose(id, before);
            if (Options.captions && id == "CHAIR-STAFF") ui.Caption("[Chân ghế cọ nhẹ trên nền gạch]", 2f);
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
            string next = NextLanReflection();
            if (next != null) lanDirector.PlayOptionalLine(next, "Lan", Line(next), 0.25f, 0.5f, 0.22f);
        }

        private static readonly string[] LanReflections = { "CH01-D08", "CH01-D09", "CH01-D16" };

        private string NextLanReflection()
        {
            if (lanDirector == null || lanDirector.IsEncounterRunning || voice == null || voice.IsSpeaking) return null;
            var transcript = voice.Transcript;
            foreach (string id in LanReflections)
            {
                int i = 0;
                while (i < transcript.Count && transcript[i].LineId != id) i++;
                if (i == transcript.Count) return id;
            }
            return null;
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
                else ui.Toast(spec.Title, spec.FactSummary, 4.6f);
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
