using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CTCC
{
    public sealed partial class HospitalGame : MonoBehaviour
    {
        [Serializable] private sealed class DialogueEntry { public string id, chapter, speaker, context, copy; }
        [Serializable] private sealed class DialogueSheet { public DialogueEntry[] entries; }
        private readonly Dictionary<string, DialogueEntry> dialogue = new();
        private HospitalScene scene;
        private FirstPerson player;
        private HospitalUI ui;
        private PhysicalInspector inspector;
        private RealityTransaction transaction;
        private Casework casework;
        private AtomicSaves saves;
        private bool live, busy, firstEscape;
        private string qaFolder;
        private Investigation State => casework.State;
        private VisitSettings Options => State.settings;
        private string UseKey => Options.alternateBindings ? "K" : "E";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartGame()
        {
            var world = FindFirstObjectByType<HospitalScene>();
            if (world != null) world.gameObject.AddComponent<HospitalGame>();
        }

        private void Awake()
        {
            string requestedQuality = Argument("-ctcc-quality");
            if (requestedQuality != null)
            {
                int preset = Array.IndexOf(QualitySettings.names, requestedQuality);
                if (preset >= 0) QualitySettings.SetQualityLevel(preset, true);
            }
            else if (Argument("-ctcc-qa") == null && Argument("-ctcc-input-qa") == null)
            {
                int preset = Array.IndexOf(QualitySettings.names, PlayerPrefs.GetString("CTCC.GraphicsQuality"));
                if (preset >= 0) QualitySettings.SetQualityLevel(preset, true);
            }
            scene = GetComponent<HospitalScene>();
            var lineSheet = Resources.Load<TextAsset>("CTCC/Dialogue");
            foreach (var entry in JsonUtility.FromJson<DialogueSheet>(lineSheet.text).entries)
                dialogue.Add(entry.id, entry);
            var rig = new GameObject("FPS character", typeof(CharacterController), typeof(FirstPerson));
            player = rig.GetComponent<FirstPerson>();
            player.Initialize(Camera.main, scene.atmosphere);
            player.Teleport(scene.Pad("AP00"));

            inspector = gameObject.AddComponent<PhysicalInspector>();
            inspector.Initialize(player.Eye);

            ui = gameObject.AddComponent<HospitalUI>();
            ui.Initialize();
            ui.OpenChanged += RefreshControls;

            qaFolder = Argument("-ctcc-qa");
            var saveDirectory = qaFolder == null ? Argument("-ctcc-save") ?? Path.Combine(Application.persistentDataPath, "Patient101")
                : Path.Combine(Path.GetFullPath(qaFolder), "isolated-save");
            saves = new AtomicSaves(saveDirectory);
            transaction = new RealityTransaction(scene, saves);
            transaction.ReadCharge = () => player.BatterySeconds;
            casework = new Casework(transaction);
            transaction.Committed += ApplyCommitted;
            scene.smoothPresentation = false;
            transaction.Present(State);
            scene.smoothPresentation = qaFolder == null;
            ApplyCommitted(State);
        }

        private void Start()
        {
            string benchmark = Argument("-ctcc-benchmark");
            if (benchmark != null)
            {
                StartCoroutine(BenchmarkVisit(Path.GetFullPath(benchmark)));
                return;
            }
            if (qaFolder != null)
            {
                StartCoroutine(AutomatedVisit());
                return;
            }
            MainMenu();
            string inputQa = Argument("-ctcc-input-qa");
            if (inputQa != null) StartCoroutine(FinishVerification(VerifyPlayerInput(), Path.GetFullPath(inputQa)));
        }

        private static string Argument(string label)
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, label);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        private void ApplyCommitted(Investigation current)
        {
            player.Apply(current.settings);
            player.ReportLayer = current.layer == Reality.B;
            ui.Apply(current.settings);
            scene.atmosphere?.Apply(current.settings);
            inspector?.SetReducedMotion(current.settings.reduceMotion);
            scene.interactables?.SetReducedMotion(current.settings.reduceMotion);
            if (scene.motion != null) scene.motion.reducedMotion = current.settings.reduceMotion;
            if (scene.realGrade != null && scene.realGrade.sharedProfile.TryGet<ColorAdjustments>(out var real))
                real.postExposure.value = 0.28f + current.settings.visibility * 0.58f;
            if (scene.reportGrade != null && scene.reportGrade.sharedProfile.TryGet<ColorAdjustments>(out var report))
                report.postExposure.value = -0.04f + current.settings.visibility * 0.45f;
            RefreshObjective();
        }

        private void RefreshControls()
        {
            bool play = live && !ui.IsModal && !busy && qaFolder == null;
            player.CanPlay = play;
            if (qaFolder != null) return;
            Cursor.lockState = play ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !play;
        }

        private void Update()
        {
            if (!live || busy || ui.IsModal || qaFolder != null)
                return;
            var keys = Keyboard.current;
            if (keys == null) return;
            if (keys.escapeKey.wasPressedThisFrame)
            {
                if (firstEscape) firstEscape = false;
                else Pause();
                return;
            }
            firstEscape = false;
            if (keys[Options.alternateBindings ? Key.J : Key.Tab].wasPressedThisFrame)
            {
                Journal();
                return;
            }
            Vector3 feet = player.Feet;
            if (State.chapter == "CH01" && State.Happened("CH01-T08") && !State.Happened("CH01-T09") &&
                feet.z < -20.26f && feet.z > -21.3f && Mathf.Abs(feet.x - 4) < 1.18f)
            {
                Transition(() => casework.OutsideAttempt(player.Feet), true, () => ui.Caption(Line("CH01-D13", 2), 4, true));
                return;
            }
            if (State.chapter == "CH01" && transaction.OnPad(feet, "AP02") && NeedsHub())
            {
                Hub();
                return;
            }
            if (State.chapter == "CH01" && State.Happened("CH01-T05") && !State.Happened("CH01-T06") &&
                feet.x > 5.82f && feet.z > 2.01f && feet.z < 7.99f)
                casework.EnterRoom(feet);
            if (State.chapter == "CH02" && State.layer == Reality.R && !State.Happened("CH02-T02") &&
               feet.z > 1.25f && feet.z < 7.9f && feet.x > 2.55f && feet.x < 6f &&
               casework.Record("CH02-T02", feet) == CommitResult.Applied)
            {
                scene.atmosphere?.From("slippers", new Vector3(10.3f, 0.1f, 4.74f));
                ui.Caption("[Tiếng dép tổ ong · bên phải — phía phòng 101]", 4.5f);
            }
            bool use = keys[Options.alternateBindings ? Key.K : Key.E].wasPressedThisFrame;
            if (transaction.OnPad(feet, "AP04") && State.chapter == "CH02" && keys.qKey.wasPressedThisFrame)
            {
                Anchor();
                return;
            }
            var target = player.Focus(Available);
            ui.Prompt(target == null
                ? (transaction.OnPad(feet, "AP04") && State.chapter == "CH02" ? "[Q]   Điểm neo phòng 101" : null)
                : "[" + UseKey + "]   " + target.title);
            if (use && target != null)
                Use(target.id);
            else if (use && transaction.OnPad(feet, "AP04") && casework.CanRevise)
                Anchor();
        }

        private bool NeedsHub() => !State.Happened("CH01-T03") && State.Happened("CH01-T02") ||
            GateService.G01(State) && !State.Happened("CH01-T08") ||
            State.Happened("CH01-T09") && !State.Happened("CH01-T10");

        private bool Available(SceneTarget target) => target.id switch
        {
            "GATE-CONG" => !State.Happened("CH01-T02"),
            "SEAM" => State.Happened("CH01-T08"),
            "BOARD" => State.layer == Reality.B && State.chapter == "CH01" && !State.Happened("CH01-T05"),
            "F101" or "AN02" => casework.CanRevise,
            "LIST" or "SOCKETS" or "WHEELS" or "PZ01" => State.chapter == "CH02" && State.layer == Reality.R,
            "EXIT" => GateService.G02(State),
            "DOOR-101" => State.Happened("CH01-T05") || State.layer == Reality.R,
            "CURTAIN-101" => scene.interactables != null && scene.interactables.curtainCloth != null && scene.interactables.curtainCloth.gameObject.activeInHierarchy,
            _ => true
        };

        private void Use(string id)
        {
            switch (id)
            {
                case "GATE-CONG":
                    if (!State.Happened("E10a.SMSRead")) Letter();
                    else if (Report(casework.OpenGate(player.Feet)))
                    {
                        scene.atmosphere?.From("doorCreak", new Vector3(4, 1.2f, -20f));
                        ui.Caption("Cổng phụ đã mở.");
                    }
                    break;
                case "DOOR-101":
                    if (scene.interactables != null && scene.roomDoor.pivot != null)
                    {
                        scene.roomDoor.isOpen = !scene.roomDoor.isOpen;
                        scene.interactables.MoveDoor(scene.roomDoor.pivot, scene.roomDoor.isOpen ? scene.roomDoor.openAngle : 0f, false, scene.atmosphere);
                    }
                    break;
                case "CURTAIN-101":
                    if (scene.interactables != null)
                    {
                        bool opened = scene.interactables.ToggleCurtain(scene.atmosphere);
                        ui.Caption(opened ? "[Kéo mở rèm che giường 4]" : "[Khép rèm che giường 4]", 2.8f);
                    }
                    break;
                case "SEAM":
                    if (!State.Happened("CH01-T09"))
                        Transition(() => casework.OutsideAttempt(player.Feet), true, () => ui.Caption("Minh · " + Line("CH01-D13", 2), 4, true));
                    else ui.Caption("Không tìm được đường ra. Trở lại sổ trực.", 5, true);
                    break;
                case "E10a": Letter(); break;
                case "BOARD":
                    if (Report(casework.Board(player.Feet)))
                    {
                        scene.atmosphere?.From("paper", player.Feet + Vector3.up);
                        ui.Caption("Đã lấy phiếu bàn giao. Cửa phòng 101 đã mở.", 5, true);
                    }
                    break;
                case "AN01":
                    if (NeedsHub() && State.chapter == "CH01") Hub();
                    else Journal();
                    break;
                case "AN02": Anchor(); break;
                case "F101": Form(); break;
                // Environmental world clues -> non-blocking diegetic toast! Zero unnecessary modal interruption!
                case "OBS101A":
                    DiegeticObservation(id, "Dấu sử dụng giường thứ tư",
                        "Có vết bánh cao su cạnh rèm và dấu đồ dùng đã bị dời đi. Ổ cắm số 4 vẫn gắn trên tường.");
                    break;
                case "OBS101B":
                    InspectDocumentObservation(id, "Thẻ bàn giao giường 4",
                        "18/11/2005 · Phòng 101\n\nGiường 4 · CHƯA BÀN GIAO\nKiểm tra lại.\n\nThẻ không ghi tên hoặc nơi chuyển.", "ObsCard");
                    break;
                case "LIST":
                    InspectDocumentObservation(id, "Bản kê phòng bệnh",
                        "Bản kê cũ có ba dòng, đánh số 1 đến 3. Tổng số ghi trên bản kê là 03.", "BedList");
                    break;
                case "SOCKETS":
                    DiegeticObservation(id, "Bốn ổ cắm đầu giường",
                        "Phòng kê ba giường, nhưng trên tường vẫn còn đủ bốn ổ cắm đánh số 1 đến 4.");
                    break;
                case "WHEELS":
                    DiegeticObservation(id, "Dấu bánh xe tại hốc 4",
                        "Bốn vết bánh tạo thành hình chữ nhật một khung giường. Đường kéo cao su dừng ngay trước tủ sắt kho.");
                    break;
                case "PZ01": PhysicalContradiction(); break;
                case "DRAW1": Take3D(EvidenceRegistry.DrawingFragments[0], "Mảnh tranh 1", "Nét mái ngói vẽ bằng sáp màu dừng ở mép giấy rách.", "DrawingPiece1", null); break;
                case "DRAW2": Take3D(EvidenceRegistry.DrawingFragments[1], "Mảnh tranh 2", "Nét cánh cửa màu xanh và ổ khóa bằng chì sẫm.", "DrawingPiece2", null); break;
                case "DRAW3": Take3D(EvidenceRegistry.DrawingFragments[2], "Mảnh tranh 3", "Ông mặt trời và một nửa ô cửa sổ nhỏ.", "DrawingPiece3", null); break;
                case "WRIST": Take3D(EvidenceRegistry.Wristband, "Vòng tay bệnh nhi C01", "Phiến giấy trong vòng nhựa mờ ghi mã C01. Vòng được giữ trong túi đồ vải ở kho 101.", null, "WRIST"); break;
                case "TRANSFER":
                    Take3D(EvidenceRegistry.Transfer, "Phiếu chuyển 101/4",
                        "17/11/2005 · An\nGiường 4 phòng 101, khám sốt tạm. Trở lại C01.\n\nBản lưu nội bộ từ bàn trực, cất trong tủ khô kho 101.", "Transfer", null);
                    break;
                case "G02":
                    ui.Caption(GateService.G02(State) ? "Cửa cấp cứu đã mở." : "Cấp cứu tạm đóng. Hãy đối chiếu hồ sơ phòng 101.", 5, true);
                    break;
                case "EXIT":
                    if (Report(casework.Complete(player.Feet))) EndOfVisit();
                    break;
            }
        }

        private void Hub()
        {
            if (!transaction.OnPad(player.Feet, "AP02"))
            {
                ui.Caption("Đứng sát vạch đồng trước bàn trực.", 4, true);
                return;
            }
            bool entering = !State.Happened("CH01-T03");
            Transition(() => casework.AtHub(player.Feet, out _), true, () =>
            {
                if (entering) LanTranscript();
                else if (State.chapter == "CH02") ui.Caption("Ca trực chưa kết thúc. Tìm lối ra qua phòng 101.", 5, true);
                else ui.Caption("Minh · " + Line("CH01-D13", 1), 4);
            });
        }

        private void Anchor()
        {
            if (State.chapter == "CH01") { Form(); return; }
            var choices = new List<ActionChoice>
            {
                new(State.layer == Reality.R ? "Bước vào B · Bản báo cáo 2005" : "Trở về R · Hiện trường 2012", () =>
                {
                    ui.Close();
                    Transition(() => casework.SwitchIn101(player.Feet), true);
                }),
                new("Đối chiếu phiếu F101", Form),
                new("Ghép tranh và kiểm hồ sơ", Identify),
                new("Quay lại phòng", CloseModal)
            };
            ui.Show("Phòng 101", "Điểm neo vạch đồng",
                "Vạch đồng trước cửa giữ cùng tọa độ trong cả hai lớp.\n\n" +
                "R giữ hiện trường năm 2012. B trình bày căn phòng theo diện mạo hồ sơ năm 2005.",
                choices.ToArray(), CloseModal);
        }

        private void Transition(Func<CommitResult> apply, bool warp, Action after = null)
        {
            if (busy) return;
            StartCoroutine(WhileTransition(apply, warp, after));
        }

        private IEnumerator WhileTransition(Func<CommitResult> apply, bool warp, Action after)
        {
            busy = true;
            ui.Close();
            RefreshControls();

            Reality beforeLayer = State.layer;
            bool reduced = Options.reduceMotion;
            float baseFov = player.Eye != null ? player.Eye.fieldOfView : Options.fov;
            Volume activeVol = beforeLayer == Reality.B ? scene.reportGrade : scene.realGrade;
            ChromaticAberration ca = null;
            LensDistortion lens = null;
            if (!reduced && activeVol != null && activeVol.sharedProfile != null)
            {
                activeVol.sharedProfile.TryGet(out ca);
                activeVol.sharedProfile.TryGet(out lens);
            }

            float seconds = reduced ? 0.07f : 0.42f;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Clamp01(t / seconds);
                float ease = k * k;
                ui.Fade(ease, beforeLayer == Reality.R ? new Color(0.06f, 0.10f, 0.085f) : new Color(0.018f, 0.028f, 0.032f));
                if (!reduced)
                {
                    if (ca != null) ca.intensity.value = Mathf.Lerp(0.06f, 0.48f, ease);
                    if (lens != null) lens.intensity.value = Mathf.Lerp(0f, -0.22f, ease);
                    if (player.Eye != null) player.Eye.fieldOfView = baseFov + ease * 3.8f;
                }
                yield return null;
            }
            ui.Fade(1f);

            // Transaction commit happens atomically at peak transition (zero mid-frame pop!)
            scene.smoothPresentation = false;
            var outcome = apply();
            scene.smoothPresentation = qaFolder == null;

            bool switchedLayer = State.layer != beforeLayer;
            if (outcome == CommitResult.Applied)
            {
                if (warp) player.Teleport(scene.Pad(SafeCheckpoints.Pad(State.checkpoint)));
                string cueName = switchedLayer ? (State.layer == Reality.B ? "switchToB" : "switchToR") : "switch";
                scene.atmosphere?.From(cueName, player.Feet + Vector3.up);
            }

            // Reset transition distortion on previous volume before fading in new reality
            if (ca != null) ca.intensity.value = 0.06f;
            if (lens != null) lens.intensity.value = 0f;

            Volume nextVol = State.layer == Reality.B ? scene.reportGrade : scene.realGrade;
            ChromaticAberration nextCa = null;
            if (!reduced && nextVol != null && nextVol.sharedProfile != null)
                nextVol.sharedProfile.TryGet(out nextCa);

            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = 1f - Mathf.Clamp01(t / seconds);
                float ease = k * k;
                ui.Fade(ease);
                if (!reduced)
                {
                    if (nextCa != null) nextCa.intensity.value = Mathf.Lerp(0.04f, 0.36f, ease);
                    if (player.Eye != null) player.Eye.fieldOfView = baseFov + ease * 2.4f;
                }
                yield return null;
            }

            if (nextCa != null) nextCa.intensity.value = State.layer == Reality.R ? 0.06f : 0.03f;
            if (player.Eye != null) player.Eye.fieldOfView = Mathf.Clamp(Options.fov, 55f, 82f);
            ui.Fade(0f);
            busy = false;
            RefreshControls();
            if (Report(outcome))
            {
                if (switchedLayer && reduced)
                    ui.Caption(State.layer == Reality.B ? "[Chuyển sang B · Bản báo cáo 2005]" : "[Trở về R · Hiện trường 2012]", 3.5f, true);
                after?.Invoke();
            }
        }

        private bool Report(CommitResult result)
        {
            if (result == CommitResult.Applied) return true;
            string copy = result switch
            {
                CommitResult.PadRequired => "Còn thiếu điều kiện. Kiểm tra vật chứng hoặc trở lại vạch neo.",
                CommitResult.Obstructed => "Vị trí chuyển lớp đang bị chắn. Hồ sơ được giữ trước lần thử.",
                _ => "Chưa ghi được bản lưu. Ca trực vẫn ở mốc trước thao tác."
            };
            ui.Caption(copy, 5, true);
            return false;
        }

        private string Line(string id, int ordinal = 1)
        {
            if (!dialogue.TryGetValue(id, out var entry) || string.IsNullOrWhiteSpace(entry.copy)) return "";
            var quotes = System.Text.RegularExpressions.Regex.Matches(entry.copy, "“([^”]+)”");
            return ordinal <= quotes.Count ? quotes[ordinal - 1].Groups[1].Value : entry.copy;
        }

        private Texture Image(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var entry in scene.inspectImages)
                if (entry.id == id) return entry.image;
            return null;
        }

        private Volume ActiveVolume() => State.layer == Reality.B ? scene.reportGrade : scene.realGrade;

        private void RefreshObjective()
        {
            string task = State.chapter == "CH01"
                ? !State.Happened("E10a.SMSRead") ? "Đọc bì thư và tin nhắn của Bảy."
                : !State.Happened("CH01-T02") ? "Mở cổng phụ."
                : !State.Happened("CH01-T03") ? "Tới bàn trực phía trong sảnh."
                : !State.Happened("CH01-T05") ? "Lấy phiếu bàn giao tại quầy."
                : !GateService.G01(State) ? "Kiểm tra phòng 101 và xử lý phiếu."
                : !State.Happened("CH01-T08") ? "Mang phiếu về sổ trực."
                : !State.Happened("CH01-T09") ? "Thử ra cổng phụ."
                : "Trở lại sổ trực."
                : State.Happened("M1-complete") ? "Hồ sơ 101 đã lưu."
                : GateService.G02(State) ? "Đến sổ giao ca ở cấp cứu."
                : State.layer == Reality.B ? "Trở về hiện trường R tại điểm neo 101."
                : !State.Has("E12") ? "Đối chiếu bản kê, ổ cắm và dấu bánh phòng 101."
                : !EvidenceRegistry.BundleComplete(State) ? "Tìm và ghép các phần hồ sơ trong kho 101."
                : "Về điểm neo 101 để xác thực hồ sơ.";
            ui.Hud(State.layer == Reality.R ? "R  ·  HIỆN TRƯỜNG 2012" : "B  ·  BẢN BÁO CÁO 2005", task);
        }
    }
}
