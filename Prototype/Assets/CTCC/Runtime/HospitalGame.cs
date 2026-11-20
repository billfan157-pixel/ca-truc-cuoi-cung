using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CTCC
{
    public sealed partial class HospitalGame : MonoBehaviour
    {
        [Serializable] private sealed class DialogueEntry { public string id, chapter, speaker, context, copy; }
        [Serializable] private sealed class DialogueSheet
        {
            public DialogueEntry[] entries;
            public DialogueCondition[] conditions;
        }
        private readonly Dictionary<string, DialogueEntry> dialogue = new();
        private readonly Dictionary<string, DialogueCondition> revealConditions = new();
        private HospitalScene scene;
        private FirstPerson player;
        private HospitalUI ui;
        private PhysicalInspector inspector;
        private VoiceDirector voice;
        private LanDirector lanDirector;
        private GatekeeperCameo gatekeeperCameo;
        private HunterOrderlyAI orderlyAi;
        private float retryEmergencySaveAt;
        private float retryAutomaticCheckpointAt;
        private float dialogueSaveAt;
        private AnApparitionDirector anDirector;
        private RealityTransaction transaction;
        private Casework casework;
        private AtomicSaves saves;
        private bool live, busy, savingDialogue, firstEscape, bootReady;
        private string qaFolder;

        // Scripted Horror Events (once-per-visit runtime guards) & Progressive Hints (CH01-T11)
        private bool heardLobbySilhouette;
        private bool playedHe01Trolley;
        private bool playedHe04CorridorCut;
        private string lastObjectiveKey = "";
        private float objectiveStagnantSeconds;
        private int hintTierShown;

        private Investigation State => casework.State;
        private VisitSettings Options => State.settings;
        private string UseKey => Options.alternateBindings ? "K" : "E";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartGame()
        {
            var world = FindFirstObjectByType<HospitalScene>();
            if (world != null && world.GetComponent<HospitalGame>() == null)
                world.gameObject.AddComponent<HospitalGame>();
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
            string requestedGi = Argument("-ctcc-gi");
            if (!string.IsNullOrEmpty(requestedGi))
                scene.giMode = requestedGi.ToLowerInvariant();

            var lineSheet = Resources.Load<TextAsset>("CTCC/Dialogue");
            var sheet = JsonUtility.FromJson<DialogueSheet>(lineSheet.text);
            foreach (var entry in sheet.entries)
                dialogue.Add(entry.id, entry);
            AddDialogueAlias("CH01-D11", "CH01-D10", 2);
            AddDialogueAlias("CH01-D12", "CH01-D10", 3);
            AddDialogueAlias("CH01-D14", "CH01-D13", 2);
            foreach (var condition in sheet.conditions)
                revealConditions.Add(condition.id, condition);

            qaFolder = Argument("-ctcc-qa");
            if (Argument("-ctcc-benchmark") != null) benchmarkAwakeQpc = PerformanceCounter();
        }

        private void Start() => StartCoroutine(BootAndRun());

        private IEnumerator BootAndRun()
        {
            // Load all additive zone scenes (LOC01..LOC04) and wait until every ZoneRegion is live
            yield return scene.EnsureAllZonesLoadedAsync();
            Debug.Log($"CTCC_BOOT_ZONES loaded={scene.LoadedZoneCount} additive={scene.IsAdditiveMode} gi={scene.giMode}");

            var rig = new GameObject("FPS character", typeof(CharacterController), typeof(FirstPerson));
            player = rig.GetComponent<FirstPerson>();
            player.Initialize(Camera.main, scene.atmosphere);
            player.Teleport(scene.Pad("AP00"));

            inspector = gameObject.AddComponent<PhysicalInspector>();
            inspector.Initialize(player.Eye);

            ui = gameObject.AddComponent<HospitalUI>();
            ui.Initialize();
            ui.OpenChanged += RefreshControls;

            voice = gameObject.AddComponent<VoiceDirector>();
            voice.Initialize(scene.atmosphere?.voiceClips, scene.atmosphere?.lanMotif, scene.atmosphere, ui, MayDeliver);

            lanDirector = gameObject.AddComponent<LanDirector>();
            lanDirector.Initialize(scene, voice, player);

            gatekeeperCameo = gameObject.AddComponent<GatekeeperCameo>();
            gatekeeperCameo.Initialize(scene, voice, player, ui);

            anDirector = gameObject.AddComponent<AnApparitionDirector>();
            anDirector.Initialize(scene, voice, player);

            orderlyAi = gameObject.AddComponent<HunterOrderlyAI>();
            orderlyAi.Initialize(scene, voice, player, ui, inspector);

            var saveDirectory = qaFolder == null ? Argument("-ctcc-save") ?? Path.Combine(Application.persistentDataPath, "Patient101")
                : Path.Combine(Path.GetFullPath(qaFolder), "isolated-save");
            saves = new AtomicSaves(saveDirectory);
            transaction = new RealityTransaction(scene, saves);
            transaction.ReadCharge = () => player.BatterySeconds;
            voice.Recorded += entry =>
            {
                transaction.RecordDialogue(entry);
                dialogueSaveAt = Time.unscaledTime + 2f;
            };
            casework = new Casework(transaction);
            transaction.Committed += ApplyCommitted;
            scene.ZoneUnregistering += zone =>
            {
                if (zone.lanActor != null)
                {
                    voice.StopFor(zone.lanActor);
                    lanDirector.StopEncounter(false);
                }
                if (zone.gatekeeperActor != null) gatekeeperCameo.StopAndHide();
                if (zone.anActor != null) anDirector.HideImmediately();
            };
            scene.ZoneReloaded += () => ApplyCommitted(State);

            // Pre-warm BOTH R and B regional NavMesh caches behind startup so the very first R->B swap never hitches!
            scene.smoothPresentation = false;
            scene.WarmupNavigationAndShaders();
            transaction.Present(State);
            scene.smoothPresentation = qaFolder == null;
            ApplyCommitted(State);
            bootReady = true;

            string benchmark = Argument("-ctcc-benchmark");
            if (benchmark != null)
            {
                benchmarkReadyQpc = PerformanceCounter();
                StartCoroutine(BenchmarkVisit(Path.GetFullPath(benchmark)));
                yield break;
            }
            if (qaFolder != null)
            {
                StartCoroutine(AutomatedVisit());
                yield break;
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
            voice?.Apply(current.settings);
            inspector?.SetReducedMotion(current.settings.reduceMotion);
            scene.interactables?.SetReducedMotion(current.settings.reduceMotion);
            if (scene.motion != null)
            {
                scene.motion.reducedMotion = current.settings.reduceMotion;
                if (current.layer == Reality.R || !GateService.G01(current))
                    scene.motion.fanSpeedScale = 1f;
            }
            if (scene.realGrade != null && scene.realGrade.sharedProfile.TryGet<ColorAdjustments>(out var real))
                real.postExposure.value = 0.42f + current.settings.visibility * 0.58f;
            if (scene.reportGrade != null && scene.reportGrade.sharedProfile.TryGet<ColorAdjustments>(out var report))
                report.postExposure.value = -0.02f + current.settings.visibility * 0.45f;

            lanDirector?.SyncState(current);
            gatekeeperCameo?.SyncState(current);
            if (current.chapter == "CH01" || current.layer == Reality.B)
                anDirector?.HideImmediately();
            orderlyAi?.SyncState(current);

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
            if (!bootReady || !live || busy || ui.IsModal || qaFolder != null)
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
            bool use = keys[Options.alternateBindings ? Key.K : Key.E].wasPressedThisFrame;
            bool anchorPressed = keys.qKey.wasPressedThisFrame;

            // CH01 B02: Spotting Lan's warm backlit silhouette behind reception glass pulls Minh voluntarily inside!
            if (!heardLobbySilhouette && State.chapter == "CH01" && State.Happened("CH01-T02") && !State.Happened("CH01-T03") &&
                feet.z > -10.5f && feet.z < -6.2f)
            {
                heardLobbySilhouette = true;
                voice?.Speak("CH01-D02", "Minh", Line("CH01-D02"), directionalPrefix: "[Bóng người sau ô kính quầy lễ tân]");
            }

            // HE01: Frosted ER glass trolley shadow gliding silently behind emergency doors on first corridor walk in B
            if (!playedHe01Trolley && State.chapter == "CH01" && State.layer == Reality.B && State.Happened("CH01-T05") &&
                !GateService.G01(State) && feet.z > 1.8f && feet.x < 5.5f)
            {
                playedHe01Trolley = true;
                StartCoroutine(PlayHe01TrolleyShadow());
            }

            // HE04: After handling F101 in B, walking back down corridor slows ceiling fans & cuts PA04 mid-sentence!
            if (!playedHe04CorridorCut && State.chapter == "CH01" && State.layer == Reality.B && GateService.G01(State) &&
                !State.Happened("CH01-T08") && feet.x < 5.2f && feet.z < 3.6f && feet.z > -0.8f)
            {
                playedHe04CorridorCut = true;
                if (scene.motion != null) scene.motion.fanSpeedScale = 0.22f;
                scene.atmosphere?.From("paStatic", scene.PaSpeakerAnchor != null ? scene.PaSpeakerAnchor.transform.position : new Vector3(5.5f, 2.6f, 3.35f));
                voice?.Speak("CH01-PA04", "Loa hành lang", Line("CH01-PA04"), scene.PaSpeakerAnchor,
                    directionalPrefix: "[Loa hành lang · rè và ngắt đột ngột]");
            }

            // CH01 B07: Outer gate loop once (HE06)
            if (State.chapter == "CH01" && State.Happened("CH01-T08") && !State.Happened("CH01-T09") &&
                feet.z < -20.26f && feet.z > -21.3f && Mathf.Abs(feet.x - 4) < 1.18f)
            {
                TransitionAsync(() => casework.OutsideAttemptAsync(player.Feet), true, () =>
                {
                    scene.atmosphere?.From("distantThunder", new Vector3(4f, 4f, -18f));
                    voice?.Speak("CH01-D14", "Minh", Line("CH01-D14"));
                });
                return;
            }

            // CH01 Hub Reception trigger (B03 entry, B06 collapse, B08 CH02 handoff)
            if (State.chapter == "CH01" && transaction.OnPad(feet, "AP02") && NeedsHub())
            {
                Hub();
                return;
            }

            // CH01 Room 101 threshold checkpoint
            if (State.chapter == "CH01" && State.Happened("CH01-T05") && !State.Happened("CH01-T06") &&
                feet.x > 5.82f && feet.z > 2.01f && feet.z < 7.99f &&
                !use && !anchorPressed && Time.unscaledTime >= retryAutomaticCheckpointAt)
            {
                retryAutomaticCheckpointAt = Time.unscaledTime + 2f;
                SaveCheckpoint(() => casework.EnterRoomAsync(feet));
                return;
            }

            // CH02 B02: Slipper cue + gentle Bé An glimpse near Room 101 doorway
            if (State.chapter == "CH02" && State.layer == Reality.R && !State.Happened("CH02-T02") &&
               feet.z > 1.25f && feet.z < 7.9f && feet.x > 2.55f && feet.x < 6f &&
               !use && !anchorPressed && Time.unscaledTime >= retryAutomaticCheckpointAt)
            {
                retryAutomaticCheckpointAt = Time.unscaledTime + 2f;
                SaveCheckpoint(() => casework.RecordAsync("CH02-T02", feet), () =>
                {
                    var slipperPosition = new Vector3(10.3f, 0.1f, 4.74f);
                    scene.atmosphere?.From("slippers", slipperPosition);
                    string direction = scene.atmosphere?.DirectionTo(slipperPosition) ?? "phía phòng 101";
                    anDirector?.PlaySlipperGlimpse(Options.reduceMotion);
                    voice?.Speak("CH02-D02", "Minh", Line("CH02-D02"),
                        directionalPrefix: $"[Tiếng dép tổ ong · {direction} · phòng 101]");
                });
                return;
            }

            // CH02 B06: Gatekeeper scripted cameo (strictly after G02, once-only, non-lethal, escapable via Door A or Door B!)
            if (gatekeeperCameo != null)
            {
                if (gatekeeperCameo.ShouldTrigger(State, feet))
                {
                    gatekeeperCameo.BeginCameo(State, Line);
                }
                else if (!use && gatekeeperCameo.CheckEscapedToEmergency(feet) && Time.unscaledTime >= retryEmergencySaveAt)
                {
                    StartCoroutine(SaveEmergency(false));
                    return;
                }
            }

            // CH01-T11: Progressive gentle inactivity hints (60s / 120s / 180s, toggleable in Settings)
            UpdateProgressiveHints();

            // CH03 Regional Patrol/Investigate/Chase/Search/Return AI (TASK011-A..E)
            if (State.chapter == "CH03" && orderlyAi != null && orderlyAi.IsActive)
            {
                orderlyAi.Tick(State, Time.deltaTime, player.IsRunning, player.IsCrouching, player.IsMoving, Line);
            }

            if (transaction.OnPad(feet, "AP04") && State.chapter == "CH02" && anchorPressed)
            {
                Anchor();
                return;
            }
            if (transaction.OnPad(feet, "AN03") && State.chapter == "CH03" && anchorPressed)
            {
                Ch03ArchiveAnchor();
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
            else if (!busy && !savingDialogue && !ui.IsModal &&
                (gatekeeperCameo == null || !gatekeeperCameo.IsActive) &&
                transaction.HasPendingDialogue && Time.unscaledTime >= dialogueSaveAt)
                StartCoroutine(SaveDialogue());
        }

        private IEnumerator SaveDialogue()
        {
            savingDialogue = true;
            var task = transaction.SaveDialogueAsync();
            while (!task.IsCompleted) yield return null;
            savingDialogue = false;
            if (task.GetAwaiter().GetResult() != CommitResult.Applied) dialogueSaveAt = Time.unscaledTime + 10f;
        }

        private IEnumerator PlayHe01TrolleyShadow()
        {
            var prop = scene.TrolleyShadowProp;
            var trolleyPosition = new Vector3(4.0f, 0.9f, 16.4f);
            scene.atmosphere?.From("trolleyRoll", trolleyPosition);
            string direction = scene.atmosphere?.DirectionTo(trolleyPosition) ?? "khu Cấp cứu";
            ui.Caption($"[Tiếng xe đẩy cáng lăn sau cửa kính mờ Cấp cứu · {direction}]", 4.5f);
            if (prop == null) yield break;

            prop.SetActive(true);
            Vector3 start = new(1.2f, 0f, 17.2f);
            Vector3 end = new(7.2f, 0f, 17.2f);
            float dur = Options.reduceMotion ? 0.5f : 3.2f;
            for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
            {
                prop.transform.position = Vector3.Lerp(start, end, Mathf.Clamp01(t / dur));
                yield return null;
            }
            prop.SetActive(false);
        }

        private void UpdateProgressiveHints()
        {
            if (!Options.hintsEnabled || State.Happened("M1-complete")) return;
            objectiveStagnantSeconds += Time.unscaledDeltaTime;
            int tier = objectiveStagnantSeconds >= 180f ? 3 : objectiveStagnantSeconds >= 120f ? 2 : objectiveStagnantSeconds >= 60f ? 1 : 0;
            if (tier <= hintTierShown) return;
            hintTierShown = tier;
            string hint = BuildProgressiveHint(tier);
            if (!string.IsNullOrEmpty(hint))
                ui.Toast("GỢI Ý NHẸ · SỔ TRỰC", hint, 6.5f);
        }

        private string BuildProgressiveHint(int tier)
        {
            if (State.chapter == "CH01")
            {
                if (!State.Happened("E10a.SMSRead")) return "Kiểm tra bì thư của Bảy đặt trên túi vải cạnh cổng phụ.";
                if (!State.Happened("CH01-T02")) return "Dùng chìa khóa trong bì thư để mở cổng sắt phụ phía trước.";
                if (!State.Happened("CH01-T03")) return "Đi qua sân mưa vào sảnh lễ tân và đứng tại vạch đồng trước bàn trực.";
                if (!State.Happened("CH01-T05")) return "Lấy phiếu bàn giao phòng 101 đặt ngay trên mặt quầy lễ tân.";
                if (!GateService.G01(State))
                    return tier == 1 ? "Đi dọc hành lang vào Phòng 101 bên phải để kiểm tra bảng kẹp F101."
                        : "Quan sát dấu bánh xe & thẻ giường 4 gần rèm nếu muốn đối chiếu bất nhất trước khi ký phiếu F101 trên vạch đồng.";
                if (!State.Happened("CH01-T08")) return "Mang phiếu đã xử lý quay về bàn trực lễ tân (vạch đồng AP02).";
                if (!State.Happened("CH01-T09")) return "Quay ra cổng sắt ngoài sân để thử rời khỏi bệnh viện.";
                return "Trở lại sổ trực ở bàn lễ tân để ghi nhận bất thường và mở bước điều tra hiện trường.";
            }
            if (!State.Has("E12"))
                return tier == 1 ? "Trong Phòng 101 (lớp R), kiểm tra bản kê 3 giường, 4 ổ cắm trên tường và dấu bánh xe trên sàn."
                    : "Sau khi ghi đủ 3 dấu vết vật lý, lại gần tủ sắt kính chắn cửa kho để đối chiếu chỗ kê giường thứ 4.";
            if (!EvidenceRegistry.BundleComplete(State))
                return "Vào kho giường phía sau tủ sắt: thu thập 3 mảnh tranh sáp màu, vòng tay C01 và phiếu chuyển 101/4, rồi mở điểm neo phòng 101 (phím Q).";
            if (!GateService.G02(State))
                return "Đứng trên vạch đồng trước cửa phòng 101 (phím Q) → Ghép tranh và đối chiếu hồ sơ C01.";
            return "Cửa Cấp cứu ở cuối hành lang chính (hoặc hành lang nhân viên bên trái) đã mở. Tới sổ giao ca Cấp cứu.";
        }

        private bool NeedsHub() => !State.Happened("CH01-T03") && State.Happened("CH01-T02") ||
            GateService.G01(State) && !State.Happened("CH01-T08") ||
            State.Happened("CH01-T09") && !State.Happened("CH01-T10");

        private bool Available(SceneTarget target) => target.id switch
        {
            "GATE-CONG" => !State.Happened("CH01-T02"),
            "SEAM" => State.Happened("CH01-T08"),
            "BOARD" => State.layer == Reality.B && State.chapter == "CH01" && !State.Happened("CH01-T05"),
            "BATTERY" => !State.Happened("CH01-BATTERY") && State.Happened("CH01-T02"),
            "F101" or "AN02" => casework.CanRevise,
            "LIST" or "SOCKETS" or "WHEELS" or "PZ01" => State.chapter == "CH02" && State.layer == Reality.R,
            "EXIT" or "CH03_DOOR" => GateService.G02(State),
            "DOOR-101" => State.Happened("CH01-T05") || State.layer == Reality.R,
            "CURTAIN-101" => scene.interactables != null && scene.interactables.curtainCloth != null && scene.interactables.curtainCloth.gameObject.activeInHierarchy,
            "E03" or "C02_NOTE" or "AN03" or "C02_BAG" or "C02_LETTER" or "C12_BOX" or "FS10_LOCK" or "RADIO_LOC07" or "G03" => State.chapter == "CH03",
            "CH03_EXIT" => State.chapter == "CH03" && GateService.G03(State),
            _ => true
        };

        private void Use(string id)
        {
            switch (id)
            {
                case "GATE-CONG":
                    if (!State.Happened("E10a.SMSRead")) Letter();
                    else SaveCheckpoint(() => casework.OpenGateAsync(player.Feet), () =>
                    {
                        scene.atmosphere?.From("doorCreak", new Vector3(4, 1.2f, -20f));
                        scene.atmosphere?.From("distantThunder", new Vector3(4, 6f, -15f), 0.7f);
                        ui.Caption("Cổng phụ đã mở. [Tiếng mưa nặng hạt trên sân xi măng]", 4.2f);
                    });
                    break;
                case "BATTERY":
                    if (Report(casework.TakeSpareBattery(player.Feet)))
                    {
                        player.RestoreBattery(360);
                        scene.atmosphere?.From("doorLatch", player.Feet + Vector3.up);
                        ui.Toast("PIN ĐÈN PIN DỰ PHÒNG", "Đã thay đôi pin khô đặt trên quầy lễ tân. Pin đèn hồi đầy 100%.", 4.5f);
                    }
                    break;
                case "STAIRWELL":
                    ui.Caption("Cầu thang lên Khu B đã bị rào chắn và khóa xích trước ngày tháo dỡ.", 4.5f, true);
                    break;
                case "DOOR-101":
                    if (scene.interactables != null && scene.roomDoor?.pivot != null)
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
                        TransitionAsync(() => casework.OutsideAttemptAsync(player.Feet), true, () =>
                            voice?.Speak("CH01-D14", "Minh", Line("CH01-D14")));
                    else ui.Caption("Không tìm được đường ra. Trở lại sổ trực.", 5, true);
                    break;
                case "E10a": Letter(); break;
                case "BOARD":
                    SaveCheckpoint(() => casework.BoardAsync(player.Feet), () =>
                    {
                        scene.atmosphere?.From("paper", player.Feet + Vector3.up);
                        lanDirector?.PlayHandoverGesture();
                        voice?.Speak("CH01-D06", "Minh", Line("CH01-D06"));
                        ui.Toast("PHIẾU BÀN GIAO 101", "Đã nhận bảng kẹp từ Lan. Cửa phòng 101 cuối hành lang bên phải đã mở.", 5f);
                    });
                    break;
                case "AN01":
                    if (NeedsHub() && State.chapter == "CH01") Hub();
                    else Journal();
                    break;
                case "AN02": Anchor(); break;
                case "F101": Form(); break;
                case "OBS101A":
                    DiegeticObservation(id, "Dấu sử dụng giường thứ tư",
                        "Có vết bánh cao su cạnh rèm và dấu đồ dùng đã bị dời đi. Ổ cắm số 4 vẫn gắn trên tường.", () =>
                        {
                            scene.atmosphere?.From("curtainSlide", new Vector3(11.4f, 1.8f, 4.3f), 0.55f);
                        });
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
                    ui.Caption(GateService.G02(State)
                        ? "Cửa cấp cứu đã mở."
                        : State.chapter == "CH01"
                            ? "Khu Cấp cứu tạm khóa trong ca này. Quay lại xử lý phiếu Phòng 101."
                            : "Cấp cứu tạm đóng. Hãy đối chiếu hồ sơ phòng 101.", 5, true);
                    break;
                case "EXIT":
                    StartCoroutine(SaveEmergency(true));
                    break;
                case "CH03_DOOR":
                    StartCoroutine(EnterCh03ArchiveWingAsync());
                    break;
                case "E03":
                    SaveCheckpoint(() => casework.TakeCh03EvidenceAsync(EvidenceRegistry.PatientRegister, "LOC07"), () =>
                    {
                        voice?.Speak("CH03-D01", "Minh", Line("CH03-D01"));
                        ui.Toast("ĐÃ THU THẬP · SỔ ĐĂNG KÝ E03", "Sổ đăng ký bệnh nhân ghi 12 mã C01–C12, nhưng hai vị trí C02 và C12 đã bị cạo xóa.", 5.5f);
                    });
                    break;
                case "C02_NOTE":
                    SaveCheckpoint(() => casework.TakeCh03EvidenceAsync(EvidenceRegistry.C02CareNote, "LOC07"), () =>
                    {
                        ui.Toast("MẪU LEN ĐỎ · TRẠM ĐIỀU DƯỠNG", "Mảnh giấy kẹp sợi len đỏ ghi: 'Giữ túi len ở kệ số 2 cho cụ bà phòng trong — đan nốt khăn trước khi xuất viện.'", 5.5f);
                    });
                    break;
                case "AN03":
                    Ch03ArchiveAnchor();
                    break;
                case "C02_BAG":
                    Ch03InspectKnittingBag();
                    break;
                case "C02_LETTER":
                    Ch03InspectOptionalIdentityLetter();
                    break;
                case "C12_BOX":
                    Ch03InspectAnonymousBoxC12();
                    break;
                case "FS10_LOCK":
                    SaveCheckpoint(() => casework.TakeCh03EvidenceAsync(EvidenceRegistry.BayLockReceipt, "LOC05"), () =>
                    {
                        ui.Toast("DẤU NIÊM PHONG · FS10", "Ổ khóa tủ lưu trữ mang vết kìm cắt cũ và nhãn niêm phong nội bộ đêm 18/11/2005.", 5.0f);
                    });
                    break;
                case "RADIO_LOC07":
                    TriggerLoc07RadioDistraction();
                    break;
                case "G03":
                    ui.Caption(GateService.G03(State)
                        ? "Cửa khu lưu trữ (G03) đã mở."
                        : "Cửa khu lưu trữ đang khóa. Hãy khôi phục hai vị trí bị xóa (C02, C12) trong sổ tên tại AN03.", 5f, true);
                    break;
                case "CH03_EXIT":
                    if (Report(casework.CompleteCh03(scene.Pad("AP07").position)))
                        Ch03EndOfChapter();
                    break;
            }
        }

        private IEnumerator SaveEmergency(bool finish)
        {
            while (savingDialogue) yield return null;
            busy = true;
            RefreshControls();
            bool firstEscape = !State.Happened("CH02-T06");
            var save = casework.SaveEmergencyAsync(player.Feet, finish, gatekeeperCameo.IsActive);
            while (!save.IsCompleted) yield return null;
            busy = false;
            RefreshControls();
            if (!Report(save.GetAwaiter().GetResult()))
            {
                retryEmergencySaveAt = Time.unscaledTime + 5f;
                yield break;
            }
            if (firstEscape)
                ui.Caption("[Tiếng chùm chìa khóa xa dần sau cánh cửa kính mờ Cấp cứu]", 4.5f, true);
            if (finish) EndOfVisit();
        }

        private void Hub()
        {
            if (!transaction.OnPad(player.Feet, "AP02"))
            {
                ui.Caption("Đứng sát vạch đồng trước bàn trực.", 4, true);
                return;
            }
            bool enteringB = !State.Happened("CH01-T03");
            bool collapsingR = GateService.G01(State) && !State.Happened("CH01-T08");
            bool startingCh02 = State.Happened("CH01-T09") && !State.Happened("CH01-T10");

            if (collapsingR)
                scene.atmosphere?.From("breakerCut", new Vector3(1.5f, 2.2f, -3.2f));

            TransitionAsync(() => casework.AtHubAsync(player.Feet), true, () =>
            {
                if (enteringB)
                {
                    lanDirector?.StartEncounter(Line);
                    LanTranscript();
                }
                else if (collapsingR)
                {
                    lanDirector?.StopEncounter(false);
                    voice?.Speak("CH01-D13", "Minh", Line("CH01-D13"));
                }
                else if (startingCh02)
                {
                    ui.ShowChapterCard("CHƯƠNG 02  ·  LỚP R / B", "PHÒNG BỆNH TRỐNG", 4.6f);
                    scene.atmosphere?.From("phoneBuzz", scene.PhoneAnchor != null ? scene.PhoneAnchor.transform.position : new Vector3(1.8f, 1.2f, -3.2f));
                    voice?.Speak("CH02-D01", "Minh", Line("CH02-D01"));
                }
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
                    TransitionAsync(() => casework.SwitchIn101Async(player.Feet), true);
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
            TransitionAsync(() => Task.FromResult(apply()), warp, after);
        }

        private void TransitionAsync(Func<Task<CommitResult>> applyAsync, bool warp, Action after = null)
        {
            if (busy) return;
            StartCoroutine(WhileTransitionAsync(applyAsync, warp, after));
        }

        private IEnumerator WhileTransitionAsync(Func<Task<CommitResult>> applyAsync, bool warp, Action after)
        {
            while (savingDialogue) yield return null;
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

            // Atomic CommitAsync runs durable NTFS Flush on background thread during peak dark fade (ZERO main-thread spike!)
            scene.smoothPresentation = false;
            var task = applyAsync();
            while (!task.IsCompleted) yield return null;
            var outcome = task.IsFaulted ? CommitResult.SaveUnavailable : task.GetAwaiter().GetResult();
            if (task.IsFaulted) Debug.LogException(task.Exception);
            scene.smoothPresentation = qaFolder == null;

            bool switchedLayer = State.layer != beforeLayer;
            if (outcome == CommitResult.Applied)
            {
                if (warp) player.Teleport(scene.Pad(SafeCheckpoints.Pad(State.checkpoint)));
                string cueName = switchedLayer ? (State.layer == Reality.B ? "switchToB" : "switchToR") : "switch";
                scene.atmosphere?.From(cueName, player.Feet + Vector3.up);
            }

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

        private bool MayDeliver(string id, string beat)
        {
            if (string.IsNullOrEmpty(id) || !dialogue.ContainsKey(id)) return false;
            return !revealConditions.TryGetValue(id, out var condition) || condition.Allows(State, beat);
        }

        private void RestoreJournal(Investigation state) =>
            voice.RestoreTranscript(state.transcript, id => dialogue.ContainsKey(id) &&
                (!revealConditions.TryGetValue(id, out var condition) || condition.HasPrerequisites(state)));

        private string Line(string id, int ordinal = 1) => LineAtBeat(id, ordinal, null);

        private void AddDialogueAlias(string id, string groupId, int ordinal)
        {
            if (dialogue.ContainsKey(id) || !dialogue.TryGetValue(groupId, out var group) || string.IsNullOrEmpty(group.copy)) return;
            var quotes = System.Text.RegularExpressions.Regex.Matches(group.copy, "“([^”]+)”");
            if (ordinal <= quotes.Count)
                dialogue.Add(id, new DialogueEntry { id = id, chapter = group.chapter, speaker = "Minh", copy = quotes[ordinal - 1].Groups[1].Value });
        }

        private string LineAtBeat(string id, int ordinal, string beat)
        {
            if (!MayDeliver(id, beat)) return "";
            var entry = dialogue[id];
            if (string.IsNullOrWhiteSpace(entry.copy)) return "";
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
                : State.chapter == "CH03"
                ? State.Happened("CH03-T06") ? "Đã hoàn tất điều tra Khu lưu trữ CH03."
                : GateService.G03(State) ? "Qua cửa thoát khu lưu trữ G03 tới hành lang chuyển tiếp AP07."
                : !State.Has("E03") ? "Kiểm tra sổ đăng ký bệnh nhân 12 mã (E03) tại trạm điều dưỡng LOC05."
                : "Đối chiếu kệ hồ sơ LOC07, khôi phục vị trí C02 & C12 tại điểm neo AN03."
                : State.Happened("M1-complete") ? "Hồ sơ 101 đã lưu."
                : GateService.G02(State) ? "Đến sổ giao ca ở cấp cứu."
                : State.layer == Reality.B ? "Trở về hiện trường R tại điểm neo 101."
                : !State.Has("E12") ? "Đối chiếu bản kê, ổ cắm và dấu bánh phòng 101."
                : !EvidenceRegistry.BundleComplete(State) ? "Tìm và ghép các phần hồ sơ trong kho 101."
                : "Về điểm neo 101 để xác thực hồ sơ.";

            if (task != lastObjectiveKey)
            {
                lastObjectiveKey = task;
                objectiveStagnantSeconds = 0f;
                hintTierShown = 0;
            }
            string layerLabel = State.layer == Reality.M ? "M  ·  LỚP KÝ ỨC"
                : State.layer == Reality.B ? "B  ·  BẢN BÁO CÁO 2005" : "R  ·  HIỆN TRƯỜNG 2012";
            ui.Hud(layerLabel, task);
        }
    }
}
