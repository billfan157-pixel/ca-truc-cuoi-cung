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

        private IEnumerator VerifyPlayerInput()
        {
            Directory.CreateDirectory(Argument("-ctcc-input-qa"));
            Require(Argument("-ctcc-save") != null && transaction.Inspect(SaveSlot.Auto) == SaveRead.Empty &&
                transaction.Inspect(SaveSlot.Manual) == SaveRead.Empty, "Input QA requires isolated, empty save slots");
            yield return Until(() => Application.isFocused, "Input QA needs a focused Windows player");
            qaKeys = InputSystem.AddDevice<Keyboard>();
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
            player.Teleport(scene.Pad("AP02"));
            yield return Until(() => State.layer == Reality.B && ui.IsModal, "reception R→B and transcript");
            yield return Choose("Xem bảng kẹp");
            yield return UseFrom("BOARD", scene.Pad("AP02").position);
            yield return Until(() => State.Happened("CH01-T05"), "board granted");
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
            if (branch == FormStatus.Refuted)
            {
                yield return UseFrom("OBS101A", new Vector3(10.85f, 0.02f, 5.25f));
                Require(State.Observed("OBS101A"), "B floor observation");
                yield return OpenFormInput();
                Require(!ButtonMatching("C    Đối chiếu").interactable, "refutation disabled with only one observation");
                yield return Tap(Key.Escape);
                yield return UseFrom("OBS101B", new Vector3(11.37f, 0.02f, 5.10f));
                Require(State.Observed("OBS101B") && inspector.IsInspecting, "B paper observation and inspector");
                yield return Choose("Mở bản chữ lớn");
                Require(!inspector.IsInspecting && ui.IsModal, "large print fallback");
                yield return Tap(Key.Escape);
            }

            yield return OpenFormInput();
            yield return Choose(branch == FormStatus.Affirmed ? "A    Xác nhận" : branch == FormStatus.Unverified ? "B    Chưa" : "C    Đối chiếu");
            if (branch == FormStatus.Refuted)
            {
                yield return Choose("Dấu sử dụng");
                Require(!ButtonMatching("Kiểm tra nội dung ký").interactable, "one selected observation cannot advance");
                yield return Choose("Thẻ giường 4");
                yield return Choose("Kiểm tra nội dung ký");
            }
            yield return Choose("Xác nhận lựa chọn");
            yield return Until(() => !busy && GateService.G01(State), $"F101 {branch} confirmation");
            Require(State.f101.firstDecision == branch && State.f101.revision == 1 &&
                FormService.Distortion(State) == (branch == FormStatus.Affirmed ? 1 : 0), $"F101 {branch} state");
            player.Teleport(scene.Pad("AP02"));
            yield return Until(() => !busy && State.layer == Reality.R, "reception B→R");
            player.Teleport(new Vector3(4, 0.02f, -20.30f), Quaternion.identity);
            yield return Until(() => !busy && State.Happened("CH01-T09"), "exterior seam");
            player.Teleport(scene.Pad("AP02"));
            yield return Until(() => !busy && State.chapter == "CH02", "CH02 handoff");

            if (branch == FormStatus.Affirmed) yield return InputPhysicalCase();
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
            Require(!ui.IsModal && State.chapter == "CH02" && State.f101.firstDecision == branch &&
                transaction.OnPad(player.Feet, SafeCheckpoints.Pad(State.checkpoint)), "manual load restores state and checkpoint");
            if (branch == FormStatus.Refuted)
            {
                Require(State.settings.alternateBindings && State.settings.textScale > 1.25f && AudioListener.volume == 0,
                    "accessibility survives manual load");
                yield return Tap(Key.J);
                yield return Until(() => ui.IsModal, "remapped J opens journal");
                yield return Tap(Key.Escape);
            }
            verification.Add($"INPUT PASS {branch}: Unity Input System, focus/interaction and UI, F101 branch, CH02 handoff, manual save/load.");
            yield return Tap(Key.Escape);
            yield return Choose("Về màn hình đầu");
        }

        private IEnumerator InputPhysicalCase()
        {
            yield return UseFrom("LIST", new Vector3(6.69f, 0.02f, 3.35f));
            Require(inspector.IsInspecting, "R list opens physical paper");
            yield return Tap(Key.Escape);
            yield return UseFrom("SOCKETS", new Vector3(11.53f, 0.02f, 3.14f));
            yield return UseFrom("WHEELS", new Vector3(11.63f, 0.02f, 4.88f));
            yield return UseFrom("PZ01", new Vector3(12.60f, 0.02f, 3.63f));
            yield return Choose("Có một chỗ kê");
            Require(State.Has("E12") && (scene.cabinet.localPosition - scene.cabinetOpen).sqrMagnitude > 0.001f,
                "PZ01 opens cabinet with animation");
            yield return Until(() => (scene.cabinet.localPosition - scene.cabinetOpen).sqrMagnitude < 0.0004f,
                "cabinet settles at open pose");
            foreach (string id in new[] { "DRAW1", "DRAW2", "DRAW3", "WRIST", "TRANSFER" })
            {
                yield return UseFrom(id, new Vector3(15.0f, 0.02f, 3.64f));
                Require(inspector.IsInspecting, id + " inspect");
                yield return Choose("Thu thập");
            }
            player.Teleport(scene.Pad("AP04"));
            yield return Tap(Key.Q);
            yield return Choose("Ghép tranh và kiểm hồ sơ");
            yield return Choose("Ghép tranh theo đường mép");
            yield return Choose("Đối chiếu An");
            Require(GateService.G02(State), "G02 opens through physical evidence and UI");
            yield return UseFrom("EXIT", scene.Pad("AP05").position);
            Require(State.Happened("M1-complete"), "exit reached and recorded");
            yield return Choose("Ở lại khám phá");
            verification.Add("INPUT PASS E11/E12/G02: 3 clues, animated cabinet, 3 fragments, wristband, transfer, assembly, identify and exit.");
        }

        private IEnumerator OpenFormInput()
        {
            player.Teleport(scene.Pad("AP04"));
            yield return Until(() => State.Happened("CH01-T06"), "room checkpoint");
            yield return Tap(Key.Tab);
            yield return Choose("Xem phiếu 101");
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

        private IEnumerator Choose(string label)
        {
            yield return Until(() => ui.IsModal, "modal for " + label);
            var button = ButtonMatching(label);
            Require(button.interactable, "enabled action " + label);
            for (int i = 0; i < 12 && EventSystem.current.currentSelectedGameObject != button.gameObject; i++)
                yield return Tap(Key.Tab);
            Require(EventSystem.current.currentSelectedGameObject == button.gameObject, "keyboard focused " + label);
            yield return Tap(Key.Enter);
        }

        private IEnumerator Tap(Key key) => Hold(key, 0f);

        private IEnumerator Hold(Key key, float duration)
        {
            qaKeys.MakeCurrent();
            InputSystem.QueueStateEvent(qaKeys, new KeyboardState(key));
            double deadline = Time.realtimeSinceStartupAsDouble + duration;
            yield return Until(() => qaKeys[key].isPressed, "queued keyboard input " + key);
            // Let the normal Update handlers observe the pressed state before releasing it.
            do { yield return null; } while (Time.realtimeSinceStartupAsDouble < deadline);
            InputSystem.QueueStateEvent(qaKeys, new KeyboardState());
            yield return Until(() => !qaKeys[key].isPressed, "keyboard release " + key);
        }

        private static IEnumerator Until(Func<bool> ready, string step)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 6;
            while (!ready() && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Require(ready(), step);
        }

        private IEnumerator InspectLifetimeStress()
        {
            var document = Image("BedList");
            Require(document != null, "stress document available");
            long before = Profiler.GetTotalAllocatedMemoryLong();
            for (int i = 0; i < 100; i++)
            {
                inspector.Open(null, document, (float)document.width / document.height, ActiveVolume());
                yield return null;
                inspector.Close();
                yield return null;
            }
            int meshes = Resources.FindObjectsOfTypeAll<Mesh>().Count(m => m.name == "InspectPaperSheet");
            int materials = Resources.FindObjectsOfTypeAll<Material>().Count(m => m.name == "InspectPaperMaterial");
            long after = Profiler.GetTotalAllocatedMemoryLong();
            Require(meshes == 0 && materials == 0, $"Inspector retains {meshes} meshes and {materials} materials");
            verification.Add($"INPUT STRESS PASS: 100 paper open/close cycles; {meshes} live paper meshes, {materials} live paper materials, Unity allocated bytes {before}→{after}.");
        }
    }
}
