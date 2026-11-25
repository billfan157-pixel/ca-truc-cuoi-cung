using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CTCC
{
    public sealed partial class HospitalGame
    {
        private VisitSettings startOptions = new();

        private void MainMenu()
        {
            live = false;
            voice?.StopCurrent();
            voice?.SetPaused(false);
            lanDirector?.StopEncounter(false);
            gatekeeperCameo?.StopAndHide();
            anDirector?.HideImmediately();

            var choices = new List<ActionChoice>();
            SaveRead automatic = transaction.Inspect(SaveSlot.Auto);
            SaveRead manual = transaction.Inspect(SaveSlot.Manual);
            if (automatic == SaveRead.Valid || automatic == SaveRead.Backup)
                choices.Add(new ActionChoice("Tiếp tục từ tự lưu", () => Load(SaveSlot.Auto)));
            if (manual == SaveRead.Valid || manual == SaveRead.Backup)
                choices.Add(new ActionChoice("Mở bản lưu tay", () => Load(SaveSlot.Manual)));
            choices.Add(new ActionChoice("Ca trực mới", () =>
            {
                if (automatic == SaveRead.Empty) Begin();
                else ui.Show("Bắt đầu lại", "Ca trực mới", "Bắt đầu một lượt mới sẽ thay mốc tự lưu của lượt hiện tại.",
                    new[] { new ActionChoice("Quay lại", MainMenu), new ActionChoice("Bắt đầu", Begin) }, MainMenu);
            }));
            choices.Add(new ActionChoice("Cài đặt & Trợ năng", () => Settings(MainMenu)));
            choices.Add(new ActionChoice("Thoát", () => Application.Quit()));
            string warning = automatic == SaveRead.Unsupported ? "\n\nBản tự lưu được tạo bằng phiên bản khác."
                : automatic == SaveRead.Damaged ? "\n\nKhông đọc được bản tự lưu. Có thể mở bản lưu tay nếu còn." : "";
            ui.Show("BỆNH VIỆN AN SINH  /  17.11.2012", "Ca trực cuối cùng",
                "Phòng 101 · Khu Nội Trú (Vertical Slice M2 · Chương 01 & đầu Chương 02)\n\n" +
                "Minh trở lại An Sinh trong ca trông coi cuối cùng trước ngày tháo dỡ.\n\n" +
                "Lưu ý nội dung: Bối cảnh bệnh viện hư cấu, chủ đề mất mát, hỏa hoạn và tâm lý căng thẳng. Có chế độ Giảm chuyển động, Chế độ Cốt truyện và Chú thích âm thanh trong Cài đặt." + warning,
                choices.ToArray(), null);
        }

        private void Begin()
        {
            staffReactions.Clear();
            CancelStaffReaction();
            heardLobbySilhouette = false;
            playedHe04CorridorCut = false;
            playedCh01Ex01Monologue = false;
            pendingCh01D01AfterLetter = false;
            b01SignKnockAt = 0f;
            ch01StartUnscaledTime = Time.unscaledTime;
            scene.atmosphere?.SetReturnWalkRainScale(1f);
            scene.atmosphere?.SetPenWriting(false, Vector3.zero);
            if (!Report(casework.Begin(player.Feet, startOptions))) return;
            ResetNotebookSession();
            orderlyAi?.ResetForLoad();
            scene.interactables?.ResetSessionProps();
            if (scene.staffCrossDoor != null) scene.staffCrossDoor.isOpen = false;
            dreadDirector?.ResetSeed(State.dreadSeed);
            RestoreJournal(State);
            live = true;
            player.Teleport(scene.Pad("AP00"));
            player.RestoreBattery(State.torchSeconds);
            ui.ShowChapterCard("CHƯƠNG 01  ·  17.11.2012", "NGƯỜI TRỞ VỀ", 4.4f);
            CloseModal();
        }

        private void Load(SaveSlot slot) => StartCoroutine(LoadAsync(slot));

        private IEnumerator LoadAsync(SaveSlot slot)
        {
            if (busy) yield break;
            staffReactions.Clear();
            CancelStaffReaction();
            voice?.StopCurrent();
            gatekeeperCameo?.StopAndHide();
            busy = true;
            ui.ModalInputBlocked = true;
            RefreshControls();
            try
            {
                while (streaming || savingDialogue) yield return null;
                if (scene.IsAdditiveMode && saves.Read(slot, out var preview) != SaveRead.Empty && preview != null)
                {
                    switch (SafeCheckpoints.Pad(preview.checkpoint))
                    {
                        case "AP06": case "AN03": case "AP07":
                            yield return scene.LoadZoneAsync(RegionStreamer.Upper);
                            break;
                        case "AP08": case "AP09":
                            yield return scene.LoadZoneAsync(RegionStreamer.Service);
                            break;
                    }
                }
            }
            finally
            {
                busy = false;
                ui.ModalInputBlocked = false;
                RefreshControls();
            }
            SaveRead result = transaction.Restore(slot, out var safe);
            if (safe == null)
            {
                ui.Show("Bản lưu", "Không thể mở", result == SaveRead.Unsupported
                    ? "Bản lưu thuộc phiên bản khác." : "Không tìm thấy một mốc lưu có thể khôi phục.",
                    new[] { new ActionChoice("Quay lại", live ? Pause : MainMenu) }, live ? Pause : MainMenu);
                yield break;
            }
            lanDirector?.StopEncounter(false);
            anDirector?.HideImmediately();
            scene.atmosphere?.SetPenWriting(false, Vector3.zero);
            scene.atmosphere?.SetReturnWalkRainScale(1f);
            pendingCh01D01AfterLetter = false;
            b01SignKnockAt = 0f;
            playedCh01Ex01Monologue = State != null && State.transcript.Exists(t => t.LineId == "CH01-D19");
            ResetNotebookSession();
            RestoreJournal(State);
            live = true;
            player.Teleport(safe);
            orderlyAi?.ResetForLoad();
            orderlyAi?.SyncState(State);
            player.RestoreBattery(State.torchSeconds);
            CloseModal();
            if (result == SaveRead.Backup)
                ui.Caption("Đã mở bản lưu dự phòng.", 5, true);
        }

        private void Pause()
        {
            voice?.SetPaused(true);
            var manual = transaction.Inspect(SaveSlot.Manual);
            var automatic = transaction.Inspect(SaveSlot.Auto);
            string chapterTitle = State.chapter switch
            {
                "CH01" => "NGƯỜI TRỞ VỀ",
                "CH03" => "NHỮNG CÁI TÊN BỊ XÓA",
                "CH04" => "NGƯỜI DẪN ĐƯỜNG",
                _ => "PHÒNG BỆNH TRỐNG"
            };
            ui.Show(chapterTitle, "Tạm dừng",
                transaction.AtRequiredPlace(player.Feet, RealityTransaction.ReceptionDesk)
                    ? "Có thể lưu tay tại sổ trực." : "Lưu tay khả dụng tại sổ trực ở lễ tân.",
                new[]
                {
                    new ActionChoice("Tiếp tục", CloseModal),
                    new ActionChoice("Lưu tay", () => { if (transaction.KeepManual()) { CloseModal(); ui.Caption("Đã lưu ca trực.", 4, true); } },
                        transaction.AtRequiredPlace(player.Feet, RealityTransaction.ReceptionDesk)),
                    new ActionChoice("Tải điểm lưu tự động", () => Load(SaveSlot.Auto), automatic == SaveRead.Valid || automatic == SaveRead.Backup),
                    new ActionChoice("Mở bản lưu tay", () => Load(SaveSlot.Manual), manual == SaveRead.Valid || manual == SaveRead.Backup),
                    new ActionChoice("Cài đặt & Trợ năng", () => Settings(Pause)),
                    new ActionChoice("Về màn hình đầu", MainMenu)
                }, CloseModal);
        }

        private void Settings(Action back)
        {
            VisitSettings current = live ? Options : startOptions;
            void Change(Action<VisitSettings> edit)
            {
                var next = JsonUtility.FromJson<VisitSettings>(JsonUtility.ToJson(current));
                edit(next);
                if (live)
                {
                    if (!Report(casework.UpdateSettings(next, player.Feet))) return;
                }
                else
                {
                    startOptions = next;
                    ui.Apply(next);
                    player.Apply(next);
                    inspector?.SetReducedMotion(next.reduceMotion);
                    inspector?.SetCameraAssist(!next.disableCameraAssist);
                    scene.atmosphere?.Apply(next);
                    voice?.Apply(next);
                }
                Settings(back);
            }

            float sensVal = current.mouseSensitivity > 0.05f ? current.mouseSensitivity : 1f;
            float ambVal = Mathf.Clamp01(current.ambienceVolume);
            float voVal = current.voiceVolume > 0.001f ? Mathf.Clamp01(current.voiceVolume) : 0.92f;
            float musVal = Mathf.Clamp01(current.musicVolume);
            ui.Show("TRỢ NĂNG & HIỂN THỊ", "Cài đặt",
                "Tab hoặc mũi tên để chuyển nút. Enter để chọn. Esc để quay lại.",
                new[]
                {
                    new ActionChoice(current.reduceMotion ? "Chuyển động · Giảm (An toàn)" : "Chuyển động · Đầy đủ", () => Change(x => x.reduceMotion = !x.reduceMotion)),
                    new ActionChoice(current.disableCameraAssist ? "Hỗ trợ lấy nét · Tắt" : "Hỗ trợ lấy nét · Bật",
                        () => Change(x => x.disableCameraAssist = !x.disableCameraAssist)),
                    new ActionChoice(current.captions ? "Chú thích âm thanh · Bật" : "Chú thích âm thanh · Tắt", () => Change(x => x.captions = !x.captions)),
                    new ActionChoice("Âm lượng tổng · " + Mathf.RoundToInt(current.volume * 100) + "%", () => Change(x => x.volume = x.volume <= 0.01f ? 0.50f : x.volume < 0.70f ? 0.72f : x.volume < 0.95f ? 1f : 0f)),
                    new ActionChoice("Âm nền môi trường · " + Mathf.RoundToInt(ambVal * 100) + "%", () => Change(x => x.ambienceVolume = ambVal >= 0.95f ? 0f : Mathf.Min(1f, ambVal + 0.25f))),
                    new ActionChoice("Giọng thoại · " + Mathf.RoundToInt(voVal * 100) + "%", () => Change(x => x.voiceVolume = voVal >= 0.95f ? 0.25f : Mathf.Min(1f, voVal + 0.25f))),
                    new ActionChoice("Nhạc nền · " + Mathf.RoundToInt(musVal * 100) + "%", () => Change(x => x.musicVolume = musVal >= 0.95f ? 0f : Mathf.Min(1f, musVal + 0.25f))),
                    new ActionChoice(current.monoAudio ? "Âm thanh · Mono" : "Âm thanh · Không gian 3D", () => Change(x => x.monoAudio = !x.monoAudio)),
                    new ActionChoice("Cỡ chữ · " + Mathf.RoundToInt(current.textScale * 100) + "%", () => Change(x => x.textScale = x.textScale >= 1.30f ? 1f : 1.30f)),
                    new ActionChoice("Độ sáng / Gamma · " + (current.visibility <= 0.05f ? "Chuẩn" : current.visibility < 0.7f ? "Sáng vừa" : "Tăng sáng"), () => Change(x => x.visibility = x.visibility <= 0.05f ? 0.5f : x.visibility < 0.7f ? 1f : 0f)),
                    new ActionChoice("Độ nhạy chuột · " + sensVal.ToString("0.0") + "x", () => Change(x => x.mouseSensitivity = sensVal >= 1.7f ? 0.6f : sensVal + 0.4f)),
                    new ActionChoice(current.alternateBindings ? "Phím · Mũi tên / K / J / L" : "Phím · WASD / E / Tab / F", () => Change(x => x.alternateBindings = !x.alternateBindings)),
                    new ActionChoice(current.hintsEnabled ? "Gợi ý điều tra · Bật" : "Gợi ý điều tra · Tắt", () => Change(x => x.hintsEnabled = !x.hintsEnabled)),
                    new ActionChoice(current.storyMode ? "Chế độ Cốt truyện · Bật (An toàn)" : "Chế độ Cốt truyện · Tiêu chuẩn", () => Change(x => x.storyMode = !x.storyMode)),
                    new ActionChoice("Góc nhìn (FOV) · " + Mathf.RoundToInt(current.fov) + "°", () => Change(x => x.fov = x.fov >= 78 ? 62 : x.fov + 8)),
                    new ActionChoice("Chất lượng đồ họa · " + QualitySettings.names[QualitySettings.GetQualityLevel()], () =>
                    {
                        int preset = (QualitySettings.GetQualityLevel() + 1) % QualitySettings.names.Length;
                        QualitySettings.SetQualityLevel(preset, true);
                        PlayerPrefs.SetString("CTCC.GraphicsQuality", QualitySettings.names[preset]);
                        PlayerPrefs.Save();
                        Settings(back);
                    }),
                    new ActionChoice("Quay lại", back)
                }, back);
        }

        private void Letter() => Letter(null, false);

        private void Letter(Transform source, bool returnToBackpack = false)
        {
            int activeTab = Mathf.Clamp(State.e10aLastTab, 0, 4);
            bool largePrintMode = qaFolder != null || inspector == null;
            bool playedOpenSound = false;

            void FinishEnvelope()
            {
                inspector?.Close();
                bool speakIntro = pendingCh01D01AfterLetter;
                pendingCh01D01AfterLetter = false;
                if (returnToBackpack) OpenBackpack(BackpackCompartment.Papers);
                else CloseModal();
                if (speakIntro)
                    voice?.Speak("CH01-D01", "Minh", Line("CH01-D01"));
            }

            string TabPageKey(int tab) => tab switch
            {
                0 => "FLAP",
                1 => "LAN_LETTER",
                2 => "NEWSPAPER",
                3 => "BAY_SMS",
                _ => "KEYS"
            };

            void RecordTab(int tab)
            {
                activeTab = Mathf.Clamp(tab, 0, 4);
                SaveCheckpoint(() => casework.InspectE10aLayerAsync(activeTab, TabPageKey(activeTab), markSmsRead: false),
                    () => RenderLayer(reopen3D: true));
            }

            void ConfirmSms()
            {
                bool firstSms = !State.Happened("E10a.SMSRead");
                SaveCheckpoint(() => casework.InspectE10aLayerAsync(3, "BAY_SMS", markSmsRead: true), () =>
                {
                    voice?.RecordTranscriptOnly("CH01-D15", "Bảy (SMS)", Line("CH01-D15"));
                    if (firstSms) pendingCh01D01AfterLetter = true;
                    ui.Caption("Đã đọc tin nhắn của Bảy và ghi vào sổ trực.", 3.2f, aboveModal: true);
                    RenderLayer(reopen3D: false);
                });
            }

            void ExtractKeys(bool alsoEquipGate)
            {
                SaveCheckpoint(async () =>
                {
                    var r = await casework.InspectE10aLayerAsync(4, "KEYS_EXTRACTED", markSmsRead: false, extractKeys: true);
                    if (r != CommitResult.Applied) return r;
                    if (alsoEquipGate)
                        return await casework.EquipItemAsync(BackpackCatalog.GateKey);
                    return CommitResult.Applied;
                }, () =>
                {
                    scene.atmosphere?.From("keyJingle", player.Eye.transform.position + player.Eye.transform.forward * 0.35f, 0.45f);
                    handPresenter?.SyncState(State, scene, hideViewModel: ui.IsModal);
                    if (alsoEquipGate)
                    {
                        ui.Toast("ĐÃ RÚT HAI CHÌA KHÓA", "Chìa cổng phụ đang cầm trên tay. Chìa khu C cất trong ba lô.", 4.6f);
                        FinishEnvelope();
                    }
                    else
                    {
                        ui.Caption("Đã rút chìa cổng phụ và chìa khu C vào ba lô.", 3.4f, aboveModal: true);
                        RenderLayer(reopen3D: false);
                    }
                });
            }

            string TabSection(int tab) => tab switch
            {
                0 => "E10a · 1/5 · MẶT NGOÀI & GIẤY HẸN",
                1 => "E10a · 2/5 · THƯ CŨ CỦA LAN (HAI MẶT)",
                2 => "E10a · 3/5 · MẨU BÁO 18/11/2005",
                3 => "E10a · 4/5 · TIN NHẮN CỦA BẢY (SMS)",
                _ => "E10a · 5/5 · HAI CHÌA KHÓA TRONG BÌ THƯ"
            };

            string TabTitle(int tab) => tab switch
            {
                0 => "Bì thư & Giấy hẹn của Bảy",
                1 => "Thư cũ của Lan",
                2 => "Mẩu báo vụ cháy An Sinh",
                3 => "Tin nhắn điện thoại từ Bảy",
                _ => "Chìa cổng phụ & Chìa khu C"
            };

            string TabBody(int tab)
            {
                bool smsRead = State.Happened("E10a.SMSRead");
                bool hasKeys = InventoryService.HasPhysical(State, BackpackCatalog.GateKey) &&
                               InventoryService.HasPhysical(State, BackpackCatalog.WingCKey);
                return tab switch
                {
                    0 => "Giấy hẹn có chữ ký Bảy:\n«23 giờ, 17/11/2012 · Chốt cổng phụ. Tôi trực ca cuối.»\n\n" +
                         "Bì thư giấy kraft ẩm mưa đặt trên túi vải cạnh xe máy. Bên trong gồm thư cũ của Lan, mẩu báo cũ, tờ nhắn đi kèm SMS và hai chiếc chìa khóa." +
                         (smsRead ? "\n\n✓ Đã đọc tin nhắn của Bảy." : "\n\n• Chưa xác nhận đọc tin nhắn mới của Bảy (mục 4/5).") +
                         (hasKeys ? "\n✓ Đã rút hai chìa khóa vào ba lô." : "\n• Hai chìa khóa vẫn nằm trong bì thư (mục 5/5)."),
                    1 => "Thư cũ của Lan (nét bút bi xanh, gấp nếp nhiều lần):\n\n" +
                         "«Anh Minh, em cần nói với anh một chuyện ở bệnh viện. Em không muốn để chuyện này qua thêm một ngày nữa.»\n\n" +
                         "Có thể lật mặt sau tờ thư bằng [Space] để kiểm tra dấu gấp và mép giấy ố nước.",
                    2 => "Mẩu báo cũ cắt từ trang tin địa phương (góc dưới bị rách và nhòe nước mưa):\n\n" +
                         "«Vụ cháy rạng sáng 18/11/2005 tại Bệnh viện An Sinh khiến 13 người tử vong.»\n\n" +
                         "Cột danh sách nạn nhân phía dưới đã rách mất, không còn đọc được tên nào.",
                    3 => "Tin nhắn mới từ Bảy (gửi lúc 22:54):\n\n«" + Line("CH01-D15") + "»\n\n" +
                         (smsRead ? "✓ Đã ghi nội dung tin nhắn vào sổ trực." : "Đọc kỹ lời dặn của Bảy trước khi mở cổng phụ vào sân."),
                    _ => "Hai chiếc chìa khóa buộc rời nhau trong góc bì thư:\n\n" +
                         "• Chìa cổng phụ (đồng vàng cũ, răng mòn sáng) — dùng mở cổng sắt phụ phía trước.\n" +
                         "• Chìa khu C (sắt sẫm màu, có thẻ giấy) — ổ khóa cũ đã bị thay từ trước năm 2012.\n\n" +
                         (hasKeys ? "✓ Cả hai chìa đã nằm trong Ba lô → Dụng cụ & đồ vật." : "Rút hai chìa khóa ra ba lô để cầm và mở cổng phụ.")
                };
            }

            void RenderLayer(bool reopen3D = true)
            {
                if (!largePrintMode && inspector != null && qaFolder == null && (reopen3D || !inspector.IsInspecting))
                {
                    if (!playedOpenSound)
                    {
                        scene.atmosphere?.From("paper", player.Feet + Vector3.up);
                        playedOpenSound = true;
                    }
                    else
                    {
                        scene.atmosphere?.From("paperFlip", player.Eye.transform.position + player.Eye.transform.forward * 0.35f, 0.45f);
                    }

                    Vector3? liftOrigin = source != null && !returnToBackpack ? source.position : (Vector3?)null;
                    if (activeTab == 0)
                    {
                        var envModel = scene.InspectTemplate("E10a");
                        var envTex = Image("Doc_BayEnvelopeFront");
                        if (envModel != null)
                            inspector.Open(envModel, null, 1f, ActiveVolume(), observationId: "E10a", source: liftOrigin);
                        else if (envTex != null)
                            inspector.Open(null, envTex, (float)envTex.width / Mathf.Max(1, envTex.height), ActiveVolume(), observationId: "E10a", source: liftOrigin);
                    }
                    else if (activeTab == 1)
                    {
                        var front = Image("Doc_BayLetter");
                        var back = Image("Doc_BayLetter_Back");
                        if (front != null)
                            inspector.Open(null, front, (float)front.width / Mathf.Max(1, front.height), ActiveVolume(),
                                backTexture: back, onFlip: _ => RenderLayer(reopen3D: false), observationId: "E10a_LETTER");
                    }
                    else if (activeTab == 4)
                    {
                        var keyTmpl = handPresenter?.GetInspectTemplate(BackpackCatalog.GateKey) ?? scene.InspectTemplate("E10a");
                        if (keyTmpl != null)
                            inspector.Open(keyTmpl, null, 1f, ActiveVolume(), observationId: "E10a_KEYS");
                    }
                    else
                    {
                        var front = Image("Doc_BayEnvelopeFront");
                        if (front != null)
                            inspector.Open(null, front, (float)front.width / Mathf.Max(1, front.height), ActiveVolume(), observationId: "E10a");
                    }
                }

                bool smsRead = State.Happened("E10a.SMSRead");
                bool hasKeys = InventoryService.HasPhysical(State, BackpackCatalog.GateKey) &&
                               InventoryService.HasPhysical(State, BackpackCatalog.WingCKey);
                var choices = new List<ActionChoice>();

                // Primary contextual actions for the active layer
                if (activeTab == 3 && !smsRead)
                    choices.Add(new ActionChoice("Xác nhận đã đọc tin nhắn Bảy", ConfirmSms));
                if (activeTab == 4)
                {
                    if (!hasKeys)
                    {
                        choices.Add(new ActionChoice("Rút 2 chìa khóa & cầm chìa cổng phụ", () => ExtractKeys(alsoEquipGate: true)));
                        choices.Add(new ActionChoice("Rút 2 chìa khóa cất vào ba lô", () => ExtractKeys(alsoEquipGate: false)));
                    }
                    else
                    {
                        bool eqGate = InventoryService.GetEquipped(State)?.itemDefId == BackpackCatalog.GateKey;
                        choices.Add(new ActionChoice(eqGate ? "Đang cầm chìa cổng phụ" : "Cầm chìa cổng phụ trên tay", () =>
                            SaveCheckpoint(() => casework.EquipItemAsync(BackpackCatalog.GateKey), FinishEnvelope), !eqGate));
                        choices.Add(new ActionChoice("Cầm chìa khu C trên tay", () =>
                            SaveCheckpoint(() => casework.EquipItemAsync(BackpackCatalog.WingCKey), FinishEnvelope)));
                    }
                }
                else if (activeTab == 1 && !largePrintMode && inspector != null && inspector.IsInspecting)
                {
                    choices.Add(new ActionChoice(inspector.ShowingBack ? "[Space] Lật mặt trước thư Lan" : "[Space] Lật mặt sau thư Lan",
                        () => inspector.Flip()));
                }

                // Quick navigation between all 5 layers (any reading order valid!)
                string[] navLabels =
                {
                    "1. Mặt ngoài & Giấy hẹn",
                    "2. Thư cũ của Lan",
                    "3. Mẩu báo 18/11/2005",
                    smsRead ? "4. Tin nhắn Bảy ✓" : "4. Tin nhắn Bảy (SMS)",
                    hasKeys ? "5. Hai chìa khóa ✓" : "5. Rút hai chìa khóa"
                };
                for (int i = 0; i < navLabels.Length; i++)
                {
                    if (i == activeTab) continue;
                    int idx = i;
                    choices.Add(new ActionChoice(navLabels[i], () => RecordTab(idx)));
                }

                if (qaFolder == null && inspector != null)
                {
                    choices.Add(new ActionChoice(largePrintMode ? "Xem bản 3D" : "Mở bản chữ lớn", () =>
                    {
                        largePrintMode = !largePrintMode;
                        if (largePrintMode) inspector.Close();
                        RenderLayer(reopen3D: !largePrintMode);
                    }));
                }
                choices.Add(new ActionChoice(returnToBackpack ? "Quay lại ba lô" : "Gấp bì thư lại", FinishEnvelope));

                if (!largePrintMode && inspector != null && qaFolder == null)
                {
                    string hint = activeTab == 1
                        ? "Space/R: Lật thư Lan · Chuột/WASD: Xoay · Lăn: Thu phóng · Chọn mục bên phải để chuyển lớp"
                        : "Chuột/WASD: Xoay vật phẩm 3D · Lăn: Thu phóng · Chọn mục bên phải để xem các lớp trong bì thư";
                    ui.ShowInspectSidebar(TabSection(activeTab), TabTitle(activeTab), TabBody(activeTab),
                        choices.ToArray(), FinishEnvelope, () => inspector.Close(), hint);
                }
                else
                {
                    Texture split = activeTab == 1 ? Image("Doc_BayLetter") : Image("Doc_BayEnvelopeFront");
                    ui.Show(TabSection(activeTab), TabTitle(activeTab), TabBody(activeTab),
                        choices.ToArray(), FinishEnvelope, split, largePrint: largePrintMode);
                }
            }

            RenderLayer(reopen3D: true);
        }

        private void InspectBoardBeforeAccept(Transform source)
        {
            if (State.Happened("CH01-T05")) return;
            var frontTex = Image("F101");
            var backTex = Image("F101_Back");
            string copy = "Bảng kẹp bàn giao ca trực Phòng 101 đặt sẵn trên mặt quầy lễ tân.\n\n" +
                          "Gồm phiếu tổng hợp bàn giao buồng bệnh và ghi chú yêu cầu kiểm tra thực tế Phòng 101 trước khi ký nộp lại sổ trực.";

            void AcceptBoard()
            {
                inspector?.Close();
                SaveCheckpoint(() => casework.BoardAsync(player.Feet), () =>
                {
                    CloseModal();
                    if (scene.ReceptionBoard != null)
                        scene.atmosphere?.From("paper", scene.ReceptionBoard.transform.position, 0.65f);
                    else
                        scene.atmosphere?.From("paper", player.Feet + Vector3.up);
                    scene.atmosphere?.From("latchClick", new Vector3(5.6f, 1.1f, 4.75f), 0.42f);
                    lanDirector?.AcknowledgeBoard();
                    voice?.Speak("CH01-D06", "Minh", Line("CH01-D06"));
                });
            }

            if (frontTex != null && inspector != null && qaFolder == null)
            {
                scene.atmosphere?.From("paper", source != null ? source.position : player.Feet + Vector3.up, 0.55f);
                inspector.Open(null, frontTex, (float)frontTex.width / Mathf.Max(1, frontTex.height), ActiveVolume(),
                    backTexture: backTex, observationId: "BOARD", source: source != null ? source.position : (Vector3?)null);
                ui.ShowInspectSidebar("QUẦY TIẾP NHẬN  ·  LỚP B", "Bảng kẹp phân công Phòng 101", copy,
                    new[]
                    {
                        new ActionChoice("Nhận bảng kẹp & cất vào ba lô", AcceptBoard),
                        new ActionChoice("Đặt lại mặt quầy", CloseModal)
                    }, CloseModal, () => inspector.Close());
            }
            else
            {
                AcceptBoard();
            }
        }

        private string BuildLanTranscript(bool record)
        {
            var body = new StringBuilder();
            foreach (string id in LanDirector.EncounterLineIds())
            {
                var (speaker, copy) = LanEncounterLine(id);
                if (string.IsNullOrWhiteSpace(copy)) continue;
                if (record) voice?.RecordTranscriptOnly(id, speaker, copy);
                body.Append(speaker).Append("  —  ").Append(copy).Append("\n\n");
            }
            return body.Append("Minh vẫn nhớ lần anh đã từ chối mở cửa. Anh chưa biết chuyện gì xảy ra sau đó.").ToString();
        }

        private void DiegeticObservation(string id, string title, string copy, Action after = null) =>
            DiegeticObservation(id, title, copy, null, after);

        private void DiegeticObservation(string id, string title, string copy, string location, Action after = null) =>
            SaveCheckpoint(() => casework.ObserveAsync(id, location, ObservationStage.Examined), () =>
            {
                scene.atmosphere?.From("paper", player.Feet + player.transform.forward * 0.5f + Vector3.up);
                ui.Toast("ĐÃ GHI QUAN SÁT  ·  " + title.ToUpperInvariant(), copy, 6.2f);
                after?.Invoke();
            });

        private void InspectDocumentObservation(string id, string title, string copy, string imageId) =>
            SaveCheckpoint(() => casework.ObserveAsync(id, "LOC03", ObservationStage.Examined), () => ShowObservedDocument(title, copy, imageId));

        private void InspectRegisteredObservation(string obsId, string location, Action onReturn = null,
            Transform source = null, Action afterRead = null)
        {
            var spec = ObservationRegistry.Get(obsId);
            if (spec == null) return;
            SaveCheckpoint(() => casework.ObserveAsync(obsId, location, ObservationStage.Examined), () =>
                OpenObservationInspector(spec, onReturn, staffDiscovery: location == "STAFF",
                    origin: source != null ? source.position : null, afterRead: afterRead));
        }

        private void OpenObservationInspector(ObservationSpec spec, Action onReturn = null, bool staffDiscovery = false,
            Vector3? origin = null, Action afterRead = null)
        {
            // Presentation-only signed F101 overprint in Layer B after signing (source scan & ObservationRecord untouched!)
            string frontKey = spec.FrontTexture;
            if (spec.Id == ObservationRegistry.F101Sheet && State.layer == Reality.B && State.f101.revision > 0)
            {
                frontKey = State.f101.current switch
                {
                    FormStatus.Affirmed => "F101_SignedA",
                    FormStatus.Unverified => "F101_SignedB",
                    FormStatus.Refuted => "F101_SignedC",
                    _ => spec.FrontTexture
                };
            }
            var frontTex = Image(frontKey) ?? Image(spec.FrontTexture);
            var backTex = spec.HasBackSide ? Image(spec.BackTexture) : null;
            var details = BuildDetailTargetsFor(spec.Id);
            bool physicalView = frontTex != null && inspector != null && qaFolder == null;
            string description = ObservationDescription(spec.Id);
            string closeLabel = onReturn != null ? "Quay lại sổ trực" : "Đặt xuống & tiếp tục";
            // The one-sided notice is read in its entirety in the ordinary viewer.
            bool examined = spec.Id == ObservationRegistry.WardOverflowNotice;
            const string signLabel = "Đặt giấy lên bảng kẹp";
            string stowDefId = BackpackCatalog.ForObservation(spec.Id)?.Id;
            bool canStowLayerB = !string.IsNullOrEmpty(stowDefId) && State.layer == Reality.B && State.chapter is ("CH01" or "CH02");

            void Finish()
            {
                inspector?.Close();
                if (onReturn != null) onReturn();
                else CloseModal();
                if (staffDiscovery && examined) QueueStaffReaction(spec.Id);
            }

            void AddBackpackChoices(List<ActionChoice> list, Action refreshAfter)
            {
                if (!canStowLayerB) return;
                var existing = InventoryService.FindByDef(State, stowDefId);
                bool inPossession = existing != null && existing.status is (BackpackItemStatus.InBag or BackpackItemStatus.Equipped or BackpackItemStatus.Placed);
                if (!inPossession)
                {
                    list.Add(new ActionChoice("Cất tạm vào ba lô (Lớp B)", () =>
                    {
                        string originLoc = spec.AllowedLocations != null && spec.AllowedLocations.Length > 0 ? spec.AllowedLocations[0] : "LOC03";
                        SaveCheckpoint(() => casework.AcquireItemAsync(stowDefId, originLoc), () =>
                        {
                            scene.atmosphere?.From("bagZip", player.Eye.transform.position + player.Eye.transform.forward * 0.3f, 0.50f);
                            handPresenter?.SyncState(State, scene, hideViewModel: true);
                            ui.Caption($"Đã cất tạm {spec.Title} vào ba lô (chỉ tồn tại trong lớp B).", 3.6f, aboveModal: true);
                            refreshAfter();
                        });
                    }));
                }
                else
                {
                    bool eq = existing.status == BackpackItemStatus.Equipped;
                    list.Add(new ActionChoice(eq ? "Cất lại vào ngăn giấy" : "Cầm tờ này ra tay", () =>
                        SaveCheckpoint(() => casework.EquipItemAsync(eq ? "" : stowDefId), () =>
                        {
                            handPresenter?.SyncState(State, scene, hideViewModel: false);
                            if (!eq) Finish();
                            else refreshAfter();
                        })));
                    list.Add(new ActionChoice("Đặt lại chỗ cũ", () =>
                        SaveCheckpoint(() => casework.ReturnPaperToFixtureAsync(stowDefId), () =>
                        {
                            scene.atmosphere?.From("paper", player.Feet + Vector3.up, 0.55f);
                            handPresenter?.SyncState(State, scene, hideViewModel: true);
                            ui.Caption($"Đã đặt {spec.Title} lại vị trí ban đầu.", 3.2f, aboveModal: true);
                            refreshAfter();
                        })));
                }
            }

            void ShowLargePrintPage()
            {
                inspector?.Close();
                examined = true;
                bool isCard = spec.Id == ObservationRegistry.Bed4Card;
                bool readRaking = !isCard || CorroborationService.HasCardTimestamp(State);
                var choices = new List<ActionChoice>();
                if (physicalView) choices.Add(new ActionChoice("Xem lại bản 3D", () => ShowPhysical(null)));
                if (isCard && !readRaking)
                {
                    choices.Add(new ActionChoice("Đọc vết hằn mặt sau", () =>
                        SaveCheckpoint(() => casework.MarkObservationDetailAsync(spec.Id, CorroborationService.RakingDetailId, flippedBack: true),
                            ShowLargePrintPage)));
                }
                if (CanMarkF101Document(spec.Id))
                    foreach (var field in AnnotationFields(spec.Id))
                        choices.Add(new ActionChoice(
                            Check(State.f101Annotations.MarkedHotspot(spec.Id) == field.Id) + AnnotationLabel(field.Id),
                            () => SaveCheckpoint(() => casework.ToggleF101MarkAsync(spec.Id, field.Id), ShowLargePrintPage)));
                AddBackpackChoices(choices, ShowLargePrintPage);
                if (afterRead != null) choices.Add(new ActionChoice(signLabel, afterRead));
                choices.Add(new ActionChoice(closeLabel, Finish));
                ui.Show(spec.ProvenanceLabel, spec.Title, BuildLargePrintTranscript(spec, readRaking),
                    choices.ToArray(), Finish, frontTex, largePrint: true);
            }

            void ShowLargePrint() =>
                SaveCheckpoint(() => casework.MarkObservationDetailAsync(spec.Id, "LARGE_PRINT", flippedBack: spec.HasBackSide),
                    ShowLargePrintPage);

            void ShowPhysical(Vector3? from)
            {
                Texture indentNormal = spec.Id == ObservationRegistry.Bed4Card ? Image("ObsCard_Back_Indent_Normal")
                    : spec.Id == ObservationRegistry.F101Sheet ? Image("F101_Back_Indent_Normal") : null;
                Texture indentMask = spec.Id == ObservationRegistry.Bed4Card ? Image("ObsCard_Back_Indent_Mask")
                    : spec.Id == ObservationRegistry.F101Sheet ? Image("F101_Back_Indent_Mask") : null;

                inspector.Open(
                    null,
                    frontTex,
                    (float)frontTex.width / Mathf.Max(1, frontTex.height),
                    ActiveVolume(),
                    backTexture: backTex,
                    detailTargets: details,
                    onFlip: back =>
                    {
                        ShowSide();
                        if (back)
                            SaveCheckpoint(() => casework.MarkObservationDetailAsync(spec.Id, "BACK", flippedBack: true),
                                () => examined = true);
                    },
                    onDetail: detailId =>
                    {
                        int index = inspector.ActiveDetailIndex;
                        SaveCheckpoint(() => casework.MarkObservationDetailAsync(spec.Id, detailId), () =>
                        {
                            examined = true;
                            if (description != null) ShowSide(ObservationDetailText(spec, details[index], index));
                        });
                    },
                    observationId: spec.Id,
                    backIndentNormal: indentNormal,
                    backIndentMask: indentMask,
                    onRakingReveal: () =>
                    {
                        SaveCheckpoint(() => casework.MarkObservationDetailAsync(spec.Id, CorroborationService.RakingDetailId, flippedBack: true), () =>
                        {
                            examined = true;
                            string rakingCopy = ObservationRakingText(spec.Id);
                            if (!string.IsNullOrEmpty(rakingCopy))
                            {
                                ui.Toast("VẾT HẰN BÚT BI · GÓC ĐÓN SÁNG", rakingCopy, 6.2f);
                                ShowSide(rakingCopy);
                            }
                        });
                    },
                    source: from,
                    annotationFields: CanMarkF101Document(spec.Id) ? AnnotationFields(spec.Id) : null,
                    markedField: State.f101Annotations.MarkedHotspot(spec.Id),
                    onMark: field => SaveCheckpoint(() => casework.ToggleF101MarkAsync(spec.Id, field),
                        () => inspector.SetMarkedField(State.f101Annotations.MarkedHotspot(spec.Id))),
                    liftSheet: afterRead != null ? scene.f101WallRenderer : null);
                ShowSide();
            }

            void ShowSide(string revealedDetail = null)
            {
                bool back = inspector.ShowingBack;
                var choices = new List<ActionChoice>();
                if (backTex != null)
                    choices.Add(new ActionChoice(spec.Id == Ch01FieldObjects.Calendar
                        ? (back ? "[Space] Lật lại tờ 18" : "[Space] Lật sang tờ 17")
                        : (back ? "[Space] Lật về mặt trước" : "[Space] Lật sang mặt sau"), inspector.Flip));
                if (back && inspector.HasRakingIndent)
                    choices.Add(new ActionChoice("[T] Nghiêng đón sáng (soi vết hằn bút bi)", () => inspector.TiltForRakingLight()));
                for (int i = 0; i < details.Length; i++)
                {
                    if (details[i].OnBackSide != back) continue;
                    int idx = i;
                    choices.Add(new ActionChoice($"[{idx + 1}] Soi: {details[idx].Label}", () => inspector.FocusDetail(idx)));
                }
                choices.Add(new ActionChoice("Mở bản chữ lớn", ShowLargePrint));
                AddBackpackChoices(choices, () => ShowSide(revealedDetail));
                if (afterRead != null) choices.Add(new ActionChoice(signLabel, afterRead));
                choices.Add(new ActionChoice(closeLabel, Finish));

                string side = back ? "MẶT SAU" : "MẶT TRƯỚC";
                string hint = (backTex != null ? "Space/R: Lật giấy · " : "") +
                    (back && inspector.HasRakingIndent ? "T: Nghiêng đón sáng (25°–40°) · " : "") +
                    "Chuột/WASD: Xoay · Lăn/Q-Z: Thu phóng · 0: Đặt lại" +
                    (!back && CanMarkF101Document(spec.Id) ? "\nBấm phải trên dòng giấy để đánh dấu, hoặc chọn vùng rồi nhấn M." : "");
                ui.ShowInspectSidebar(spec.ShortCode + "  ·  " + side, spec.Title,
                    revealedDetail ?? description ?? BuildFullObservationBody(spec, State.FindObservation(spec.Id)),
                    choices.ToArray(), Finish, inspector.Close, hint);
            }

            if (physicalView)
            {
                scene.atmosphere?.From("paper", player.Feet + player.transform.forward * 0.5f + Vector3.up);
                ShowPhysical(origin);
            }
            else ShowLargePrint();
        }

        private static string ObservationDescription(string id) => id switch
        {
            ObservationRegistry.F101Sheet => "Một phiếu in sẵn kẹp trên bảng bàn giao.",
            ObservationRegistry.Bed4Card => "Một tấm thẻ giấy treo ở đầu giường.",
            ObservationRegistry.WardOverflowNotice => "Một tờ thông báo dán trên bảng tin nhân viên.",
            ObservationRegistry.ShiftGuide => "Một bản trích quy chế lưu ở bàn điều dưỡng.",
            Ch01FieldObjects.Calendar => "Hai lá lịch vẫn chung một ghim sắt.",
            _ => null
        };

        private static InspectDetailTarget[] BuildDetailTargetsFor(string obsId) => obsId switch
        {
            Ch01FieldObjects.Calendar => new[]
            {
                new InspectDetailTarget("CALENDAR_MARGIN", "Mép tờ 17", new Vector2(0f, -.05f), .275f, true)
            },
            ObservationRegistry.F101Sheet => new[]
            {
                new InspectDetailTarget(F101Annotations.Table, "Bảng kê", new Vector2(0.01f, 0.02f), 0.27f, false),
                new InspectDetailTarget(F101Annotations.Zero, "Ô tổng hợp", new Vector2(0.01f, -0.04f), 0.25f, false),
                new InspectDetailTarget("F101_BACK", "Ghi chú bút chì", new Vector2(0.0f, 0.02f), 0.28f, true)
            },
            ObservationRegistry.Bed4Card => new[]
            {
                new InspectDetailTarget(F101Annotations.CardStatus, "Mục tình trạng", new Vector2(0.02f, -0.02f), 0.26f, false),
                new InspectDetailTarget(F101Annotations.Receiver, "Ô chữ ký", new Vector2(0.01f, -0.08f), 0.25f, false),
                new InspectDetailTarget("OBS101B_BACK", "Ghi chú bút chì", new Vector2(0.0f, 0.02f), 0.28f, true)
            },
            ObservationRegistry.Bed2Transfer => new[]
            {
                new InspectDetailTarget("OBS101C_DEST", "Điểm đến Khoa Nội B & giờ 19g20", new Vector2(0.0f, 0.03f), 0.27f, false),
                new InspectDetailTarget("OBS101C_SIGNS", "Đủ 2 chữ ký giao–nhận & dấu đỏ", new Vector2(0.0f, -0.07f), 0.26f, false),
                new InspectDetailTarget("OBS101C_BACK", "Mặt sau xác nhận nhập buồng", new Vector2(0.0f, 0.02f), 0.28f, true)
            },
            ObservationRegistry.Bed3Meds => new[]
            {
                new InspectDetailTarget("OBS101D_LOG", "Lịch thuốc 15–17/11 đã ký phát", new Vector2(0.0f, 0.01f), 0.27f, false),
                new InspectDetailTarget("OBS101D_CLOSED", "Dấu khóa phiếu xuất viện 10g15", new Vector2(0.0f, -0.07f), 0.26f, false)
            },
            ObservationRegistry.ShiftGuide => new[]
            {
                new InspectDetailTarget("SHIFT_RULE2", "Mục 2", new Vector2(0.0f, 0.02f), 0.27f, false),
                new InspectDetailTarget("SHIFT_RULE3", "Mục 3", new Vector2(0.0f, -0.05f), 0.26f, false),
                new InspectDetailTarget("SHIFT_BACK", "Giấy ghi chú", new Vector2(0.0f, 0.02f), 0.28f, true)
            },
            _ => Array.Empty<InspectDetailTarget>()
        };

        private static string BuildFullObservationBody(ObservationSpec spec, ObservationRecord rec)
        {
            var sb = new StringBuilder();
            sb.Append(spec.FactSummary);
            if (rec != null && rec.flippedBack)
                sb.Append("\n\n✓ Đã kiểm tra mặt sau tài liệu.");
            return sb.ToString();
        }

        private static string BuildLargePrintTranscript(ObservationSpec spec, bool readRaking)
        {
            var sb = new StringBuilder();
            sb.Append("NGUỒN: ").Append(spec.ProvenanceLabel).Append("  ·  ").Append(ObservationRegistry.CategoryLabel(spec.Category)).Append("\n\n");
            string back = ObservationBackText(spec.Id);
            if (ObservationDescription(spec.Id) != null)
            {
                sb.Append("MẶT TRƯỚC\n").Append(spec.FactSummary);
                var targets = BuildDetailTargetsFor(spec.Id);
                for (int i = 0; i < targets.Length; i++)
                    if (!targets[i].OnBackSide) sb.Append("\n\n").Append(ObservationDetailText(spec, targets[i], i));
                if (spec.Id == ObservationRegistry.WardOverflowNotice)
                    sb.Append("\n\nGhi thêm: Tối 17/11 có kê tạm giường 4. Sáng 18/11 đã đẩy vào kho?");
                if (spec.Id == ObservationRegistry.ShiftGuide)
                    sb.Append("\n\nMục 4: Giữ chữ ký cũ và bản chụp xác nhận ban đầu. Ghi rõ mỗi lần sửa.");
                if (back != null) sb.Append("\n\nMẶT SAU\n").Append(back);
                if (readRaking && ObservationRakingText(spec.Id) is string raking)
                    sb.Append("\n\nVẾT HẰN BÚT BI (SOI NGHIÊNG ÁNH SÁNG)\n").Append(raking);
                return sb.ToString();
            }
            sb.Append("NỘI DUNG CHÍNH:\n").Append(spec.FactSummary).Append("\n\n");
            if (!string.IsNullOrEmpty(back)) sb.Append("MẶT SAU\n").Append(back).Append("\n\n");
            sb.Append("PHÂN TÍCH:\n").Append(spec.InterpretationNote);
            if (spec.DetailLabels != null && spec.DetailLabels.Length > 0)
            {
                sb.Append("\n\nCHI TIẾT ĐÁNG CHÚ Ý TRÊN TÀI LIỆU:");
                for (int i = 0; i < spec.DetailLabels.Length; i++)
                    sb.Append("\n• ").Append(spec.DetailLabels[i]);
            }
            return sb.ToString();
        }

        private static string ObservationRakingText(string id) => id switch
        {
            ObservationRegistry.Bed4Card =>
                "Vết hằn bút bi từ tờ giấy phía trên: «03g17 · CHƯA CHỐT SỔ» và «CHƯA CÓ NGƯỜI NHẬN — ĐỂ LẠI THẺ».",
            ObservationRegistry.F101Sheet =>
                "Nghiêng phiếu 25°–40° đón sáng thấy dòng bị ấn đè ở mặt sau: «GIƯỜNG 04 — CHƯA CHỐT SỔ / GẠCH BỎ MỤC TẠM → GHI TRÒN: 0 / 03g17 · LIÊN LẠC GIÁN ĐOẠN».",
            _ => null
        };

        private static string ObservationDetailText(ObservationSpec spec, InspectDetailTarget target, int index)
        {
            if (target.OnBackSide) return ObservationBackText(spec.Id);
            return target.Id switch
            {
                "F101_TABLE" => "01 đã xuất viện, dọn giường ngày 16/11. 02 chuyển Nội B lúc 19g20, đủ hai chữ ký. 03 xuất viện lúc 10g15, khóa phiếu thuốc ngày 17/11.",
                F101Annotations.Zero => "Số bệnh nhân chưa bàn giao: 0 (Không còn tồn đọng).",
                "SHIFT_RULE2" => "Mục 2: Phiếu chuyển hoàn tất cần đủ chữ ký giao và nhận, cùng dấu ĐÃ BÀN GIAO. Thiếu một chữ ký thì phiếu chưa khép.",
                "SHIFT_RULE3" => "Mục 3: Nếu chưa rõ người bệnh đi đâu, ghi nhận chưa đủ căn cứ hoặc có sai lệch cần đối chiếu. Giữ lại dấu vết gốc.",
                _ => spec.DetailLabels[index]
            };
        }

        private static string ObservationBackText(string id) => id switch
        {
            ObservationRegistry.F101Sheet =>
                "Ghi chú bút chì: Đã kiểm đủ ba giường cố định. Chưa thấy phiếu chốt cho giường tạm ở quầy.",
            ObservationRegistry.Bed4Card =>
                "Ghi chú bút chì: Giường 4 kê tạm tối 17/11 cho ca khám sốt. Phần ghi nơi chuyển bị nhòe.",
            ObservationRegistry.Bed2Transfer =>
                "Đã đối chiếu sổ trực tầng 2 lúc 19g30 ngày 17/11. Giường 2 đã thay ga sạch và sẵn sàng. Thủ tục đủ chữ ký hai bên.",
            ObservationRegistry.Bed3Meds =>
                "Bệnh nhân xuất viện lúc 10g15 ngày 17/11/2005. Chăn và ga đã nộp lại tủ đồ vải tầng trệt. Tủ đầu giường trống.",
            ObservationRegistry.ShiftGuide =>
                "Giấy kẹp của Lan gửi Minh: Bảng tổng hợp phòng 101 ghi 0, nhưng sau rèm còn một mục tối 17/11 thiếu chữ ký. Lan nhắc Minh xem cả hai mặt thẻ trước khi ký.",
            Ch01FieldObjects.Calendar =>
                "Thứ Năm, 17/11/2005. Hai dòng bút bi ở lề: Rửa cốc sau khi chốt ca. Treo lại chìa tủ ở móc cũ.",
            _ => null
        };

        private void ShowObservedDocument(string title, string copy, string imageId)
        {
            scene.atmosphere?.From("paper", player.Feet + player.transform.forward * 0.5f + Vector3.up);
            var tex = Image(imageId);
            if (tex != null && inspector != null && qaFolder == null)
            {
                inspector.Open(null, tex, (float)tex.width / tex.height, ActiveVolume());
                ui.ShowInspectSidebar(State.layer == Reality.B ? "QUAN SÁT TỪ B" : "QUAN SÁT TẠI HIỆN TRƯỜNG", title, copy,
                    new[]
                    {
                        new ActionChoice("Đặt xuống & tiếp tục", CloseModal),
                        new ActionChoice("Mở bản chữ lớn", () =>
                        {
                            inspector.Close();
                            ui.Show(State.layer == Reality.B ? "QUAN SÁT TỪ B" : "QUAN SÁT TẠI HIỆN TRƯỜNG", title, copy,
                                new[] { new ActionChoice("Khép và trở lại", CloseModal) }, CloseModal, tex);
                        })
                    }, CloseModal, () => inspector.Close());
            }
            else
            {
                ui.Show(State.layer == Reality.B ? "QUAN SÁT TỪ B" : "QUAN SÁT TẠI HIỆN TRƯỜNG", title, copy,
                    new[] { new ActionChoice("Khép và trở lại", CloseModal) }, CloseModal, tex);
            }
        }

        private void Take3D(string id, string title, string copy, string imageId, string modelId, ActionChoice? readDetail = null)
        {
            var tex = Image(imageId);
            var tmpl = string.IsNullOrEmpty(modelId) ? null : scene.InspectTemplate(modelId);
            scene.atmosphere?.From("paper", player.Feet + player.transform.forward * 0.5f + Vector3.up);

            Action confirmTake = () => SaveEvidence(id, () =>
            {
                CloseModal();
                ui.Toast("ĐÃ THU THẬP VẬT CHỨNG", title + " đã được lưu vào sổ trực.", 4.5f);
            });

            const string heading = "KHO GIƯỜNG 101  /  HIỆN TRƯỜNG R";
            var reader = new List<ActionChoice>
            {
                new(State.Has(id) ? "Đã ghi vào sổ" : "Ghi vật chứng", confirmTake)
            };
            if (readDetail.HasValue) reader.Add(readDetail.Value);
            reader.Add(new ActionChoice("Quay lại", CloseModal));

            if ((tmpl != null || tex != null) && inspector != null && qaFolder == null)
            {
                float aspect = tex != null ? (float)tex.width / tex.height : 1f;
                inspector.Open(tmpl, tex, aspect, ActiveVolume());
                var sidebar = new List<ActionChoice>
                {
                    new(State.Has(id) ? "Đã ghi vào sổ" : "Thu thập & ghi vào sổ", confirmTake)
                };
                if (readDetail.HasValue) sidebar.Add(readDetail.Value);
                sidebar.Add(new ActionChoice("Bản chữ lớn", () =>
                {
                    inspector.Close();
                    ui.Show(heading, title, copy, reader.ToArray(), CloseModal, tex);
                }));
                sidebar.Add(new ActionChoice("Đặt lại", CloseModal));
                ui.ShowInspectSidebar(heading, title, copy, sidebar.ToArray(), CloseModal, () => inspector.Close());
                return;
            }

            ui.Show(heading, title, copy, reader.ToArray(), CloseModal, tex);
        }

        private void Ch02ReadTransferNote()
        {
            if (State.chapter != "CH02" || State.layer != Reality.R || !State.Has("E12")) return;
            inspector?.Close();
            string line = LineAtBeat("CH02-D08", 1, "B04");
            voice?.RecordTranscriptOnly("CH02-D08", "Minh", line, "B04");
            ui.Show("PHIẾU CHUYỂN 101/4", "Dòng ghi chú bị gạch xóa",
                "Sốt 39,8°C sau đợt truyền AS-05 lần 4. Bé khóc hoảng loạn vì không nhớ tên mẹ, ôm chặt bức tranh mới chịu ngủ. " +
                "Không thể để bé dưới hầm thêm một đêm nào nữa.\n\nMinh · " + line,
                new[] { new ActionChoice("Quay lại phiếu chuyển", () => Use("TRANSFER")) }, CloseModal);
        }

        private void SaveEvidence(string id, Action committed) =>
            SaveCheckpoint(() => casework.TakeAsync(id), committed, closeOnFailure: true);

        private void SaveCheckpoint(Func<System.Threading.Tasks.Task<CommitResult>> save, Action committed = null,
            bool closeOnFailure = false, Action onFailure = null)
        {
            if (!busy) StartCoroutine(WaitForCheckpoint(save, committed, closeOnFailure, onFailure));
        }

        private IEnumerator WaitForCheckpoint(Func<System.Threading.Tasks.Task<CommitResult>> save, Action committed,
            bool closeOnFailure, Action onFailure)
        {
            busy = true;
            ui.ModalInputBlocked = true;
            RefreshControls();
            while (savingDialogue) yield return null;
            System.Threading.Tasks.Task<CommitResult> pending;
            try
            {
                pending = save();
                while (!pending.IsCompleted) yield return null;
            }
            finally
            {
                ui.ModalInputBlocked = false;
                busy = false;
                RefreshControls();
            }
            if (pending.IsFaulted)
            {
                Debug.LogException(pending.Exception);
                if (closeOnFailure) CloseModal();
                Report(CommitResult.SaveUnavailable);
                onFailure?.Invoke();
            }
            else
            {
                var result = pending.GetAwaiter().GetResult();
                if (result != CommitResult.Applied && closeOnFailure) CloseModal();
                if (Report(result)) committed?.Invoke();
                else onFailure?.Invoke();
            }
        }

        private bool CanMarkF101Document(string id) => casework.CanRevise &&
            id is (ObservationRegistry.F101Sheet or ObservationRegistry.Bed4Card) &&
            HypothesisService.HasExaminedInLayerB(State, id);

        private static string AnnotationLabel(string id) => id switch
        {
            F101Annotations.Table => "Bảng kê giường 01–03",
            F101Annotations.Zero => "Tổng hợp: 0 (Không còn tồn đọng)",
            F101Annotations.CardStatus => "Tình trạng: Chưa bàn giao",
            F101Annotations.Receiver => "Người nhận bàn giao: ô ký nhận trống",
            _ => ""
        };

        private static InspectAnnotationField[] AnnotationFields(string id) =>
            id == ObservationRegistry.F101Sheet
                ? new[]
                {
                    PaperField(F101Annotations.Table, 110, 394, 844, 256),
                    PaperField(F101Annotations.Zero, 110, 684, 844, 132)
                }
                : new[]
                {
                    PaperField(F101Annotations.CardStatus, 112, 622, 840, 162),
                    PaperField(F101Annotations.Receiver, 536, 816, 416, 215)
                };

        private static InspectAnnotationField PaperField(string id, int x, int y, int width, int height) =>
            new(id, new Rect(x / 1024f, 1f - (y + height) / 1360f, width / 1024f, height / 1360f));

        private IEnumerator PlayF101Signature(FormStatus choice, int revision)
        {
            if (State.layer != Reality.B) yield break;

            f101SignatureRevision = revision;
            ui.Prompt(null);
            bool SameSignature() => live && State.layer == Reality.B &&
                State.f101.revision == revision && State.f101.current == choice;

            try
            {
                yield return new WaitForSecondsRealtime(1.45f);
                while (SameSignature() && (ui.IsModal || busy || voice?.IsSpeaking == true))
                    yield return null;
                if (!SameSignature()) yield break;

                // Keep the room response ahead of PA and corridor dialogue even when Minh walks away immediately.
                if (dreadDirector?.TryPlayF101Reaction() == true)
                    yield return new WaitForSecondsRealtime(2.1f);
                if (!SameSignature() || ui.IsModal || busy || voice?.IsSpeaking == true) yield break;

                string paId = choice == FormStatus.Affirmed ? "CH01-PA01" : choice == FormStatus.Unverified ? "CH01-PA02" : "CH01-PA03";
                Vector3 paPosition = scene.PaSpeakerAnchor != null ? scene.PaSpeakerAnchor.transform.position : new Vector3(5.5f, 2.6f, 3.35f);
                scene.atmosphere?.From("paStatic", paPosition, choice == FormStatus.Affirmed ? 0.20f : 0.46f);
                string direction = scene.atmosphere?.DirectionTo(paPosition) ?? "ngoài cửa phòng 101";
                voice?.Speak(paId, "Loa hành lang", Line(paId), scene.PaSpeakerAnchor,
                    directionalPrefix: $"[Loa hành lang · {direction}]");
            }
            finally
            {
                if (f101SignatureRevision == revision) f101SignatureRevision = 0;
            }
        }

        private void PhysicalContradiction()
        {
            var missing = casework.MissingPhysicalObservations();
            if (State.Has("E12"))
            {
                ui.Caption("Tủ sắt đã lăn sang bên. Kiểm tra hồ sơ trong kho.", 4, true);
                return;
            }
            ui.Show("GIƯỜNG THỨ TƯ  /  E12", "Đặt bản kê cạnh dấu vết",
                missing ?? "Bản kê có ba dòng. Bốn ổ cắm và dấu bánh có vị trí thứ tư. Chọn cách đọc và đẩy tủ kiểm tra kho.",
                new[]
                {
                    new ActionChoice("Có một chỗ kê chưa ghi trong bản kê — Đẩy tủ kho", () =>
                    {
                        CloseModal();
                        bool smooth = scene.smoothPresentation;
                        SaveCheckpoint(async () =>
                        {
                            scene.smoothPresentation = false;
                            try { return await casework.SolvePz01Async(player.Feet); }
                            finally { scene.smoothPresentation = smooth; }
                        }, () =>
                        {
                            if (scene.interactables != null && scene.cabinet != null)
                            {
                                scene.cabinet.localPosition = scene.cabinetClosed;
                                scene.interactables.MoveCabinet(scene.cabinetOpen, !smooth, scene.atmosphere);
                            }
                            ui.Toast("ĐÃ GHI NHẬN E12", "Tủ sắt đang lăn khỏi cửa kho giường. Bên trong còn khung giường tháo rời và tủ khô.", 5.5f);
                        });
                    }, missing == null),
                    new ActionChoice("Quay lại kiểm tra", CloseModal)
                }, CloseModal, Image("BedList"));
        }

        private void Identify()
        {
            string missing = !State.Has("E12") ? "Đối chiếu các dấu vật lý để tiếp cận kho 101."
                : !EvidenceRegistry.HaveFragments(State) ? "Tìm đủ ba mảnh tranh ở tủ khô trong kho."
                : !State.Has(EvidenceRegistry.Drawing) ? "Ghép ba mép giấy trước khi đối chiếu."
                : !State.Has(EvidenceRegistry.Wristband) ? "Còn thiếu vòng cũ có mã C01."
                : !State.Has(EvidenceRegistry.Transfer) ? "Còn thiếu phiếu chuyển từ bàn trực."
                : State.layer != Reality.R ? "Trở về R để xác thực vật chứng."
                : null;
            var choices = new List<ActionChoice>();
            if (EvidenceRegistry.HaveFragments(State) && !State.Has(EvidenceRegistry.Drawing))
                choices.Add(new ActionChoice("Ghép tranh theo đường mép", () => SaveEvidence(EvidenceRegistry.Drawing, Identify), State.layer == Reality.R));
            if (missing == null && !GateService.G02(State))
                choices.Add(new ActionChoice("Đối chiếu An · 101/4 → C01", () =>
                {
                    CloseModal();
                    SaveCheckpoint(() => casework.VerifyAnAsync(player.Feet), () =>
                    {
                        scene.atmosphere?.From("doorCreak", new Vector3(4f, 1.5f, 16f));
                        anDirector?.PlayPostVerifyVignette(Line("CH02-D03"), Options.reduceMotion);
                        ui.Toast("CỬA CẤP CỨU ĐÃ MỞ · G02", "Minh · " + Line("CH02-D04") + " (Có thể đi hành lang chính hoặc hành lang nhân viên.)", 6.5f);
                        voice?.RecordTranscriptOnly("CH02-D04", "Minh", Line("CH02-D04"));
                    });
                }));
            choices.Add(new ActionChoice("Trở lại bảng kẹp", Anchor));
            ui.Show("HỒ SƠ TRONG KHO 101", "Bức tranh của An", missing ??
                "Tranh ghi lại một cánh cửa có khóa. Vòng cũ ghi C01. Phiếu chuyển ghi An, khám sốt tạm ở 101/4 ngày 17/11/2005, rồi chuyển lại Khu C.",
                choices.ToArray(), Anchor, State.Has(EvidenceRegistry.Drawing) ? Image("Drawing") : null);
        }

        private List<ObservationSpec> CollectObservedDocuments()
        {
            var docs = new List<ObservationSpec>();
            var all = ObservationRegistry.All;
            for (int i = 0; i < all.Count; i++)
                if (all[i].IsDocument && JournalNote.HasSource(State, all[i].Id))
                    docs.Add(all[i]);
            return docs;
        }

        private void TranscriptPage()
        {
            var sb = new StringBuilder();
            if (voice == null || voice.Transcript.Count == 0)
                sb.Append("Chưa có lời thoại nào được ghi nhận trong lượt này.");
            else
            {
                foreach (var entry in voice.Transcript)
                    sb.Append(entry.Speaker).Append("  [").Append(entry.LineId).Append("]\n").Append(entry.Text).Append("\n\n");
            }
            ui.Show("BẢN GHI HỘI THOẠI", "Nhật ký thoại", sb.ToString().TrimEnd(),
                new[] { new ActionChoice("Quay lại sổ trực", Journal), new ActionChoice("Đóng", CloseModal) }, Journal);
        }

        private void EndOfVisit()
        {
            var r = State.f101;
            ui.Show("SỔ GIAO CA CẤP CỨU", "Hồ sơ C01 · Phòng 101",
                "Đã xác thực Bé An từ bản lưu 101/4, vòng tay C01 và phiếu chuyển nội bộ ngày 17/11/2005. Vị trí tìm thấy An ở khu sau vẫn để ngỏ cho phần tiếp theo.\n\n" +
                $"• Quyết định F101 hiện tại: {Status(r.current)} (lần đầu: {Status(r.firstDecision)}, số lần sửa: {r.revision})\n" +
                $"• Sai lệch tại khu phòng 101: {FormService.Distortion(State, "LOC03")}\n" +
                $"• Vật chứng thực thể đã lưu: {State.evidence.Count} mục · Sự kiện Cấp cứu: {(State.Happened("CH02-T06") ? "Đã thoát khỏi Kẻ giữ cửa" : "Đã vào khu Cấp cứu")}\n\n" +
                "Phiếu 101, lịch sử sửa phiếu và toàn bộ vật chứng đã được giữ nguyên trong bản lưu.",
                new[]
                {
                    new ActionChoice("Mở lối lên khu B", () =>
                    {
                        CloseModal();
                        StartCoroutine(EnterCh03ArchiveWingAsync());
                    }),
                    new ActionChoice("Ở lại khám phá", CloseModal),
                    new ActionChoice("Về màn hình đầu", MainMenu)
                }, CloseModal);
        }

        private IEnumerator EnterCh03ArchiveWingAsync()
        {
            if (busy || State.chapter != "CH02") yield break;
            CloseModal();
            busy = true;
            RefreshControls();
            while (streaming) yield return null;
            if (scene.IsAdditiveMode) yield return scene.LoadZoneAsync(RegionStreamer.Upper);
            busy = false;
            TransitionAsync(() => casework.BeginCh03Async(player.Feet), false, () =>
                ui.ShowChapterCard("CHƯƠNG 03", "NHỮNG CÁI TÊN BỊ XÓA", 4.6f));
        }

        private const string RegisterRows =
            "C01     101/4     Đã chuyển\n" +
            "····     B/2         Phiếu giữ đồ K-02\n" +
            "C03     C04     C05     C06     C07\n" +
            "C08     C09     C10     C11\n" +
            "C12     C/12       Dấu chìa, tên bị xé khỏi trang";

        private void Ch03ReadRegister()
        {
            ui.Show("SỔ ĐĂNG KÝ E03", "Mục bàn giao đồ cá nhân", RegisterRows,
                new[]
                {
                    new ActionChoice("Chép sổ vào nhật ký", () =>
                        SaveCheckpoint(() => casework.TakeCh03EvidenceAsync(EvidenceRegistry.PatientRegister, "LOC07"), () =>
                        {
                            CloseModal();
                            voice?.Speak("CH03-D01", "Minh", Line("CH03-D01"));
                        })),
                    new ActionChoice("Đặt sổ xuống", CloseModal)
                }, CloseModal);
        }

        private void Ch03ArchiveAnchor()
        {
            bool solved = GateService.G03(State);
            bool registered = State.Has(EvidenceRegistry.PatientRegister);
            bool hasNote = State.Has(EvidenceRegistry.C02CareNote);
            bool hasBag = State.Has(EvidenceRegistry.C02YarnBag);
            var copy = new StringBuilder();
            copy.AppendLine(registered ? RegisterRows : "Sổ E03 đặt trên giá đọc trong kho tầng 3.");
            copy.AppendLine(hasNote ? "\nGhi chú · B/2. Giữ khăn len đan dở trong túi có thẻ K-02." : "\nTìm mảnh ghi chú kẹp sợi len ở bàn đọc.");
            copy.AppendLine(hasBag ? "Túi vải · Thẻ K-02 buộc vào quai. Trong túi còn khăn len đỏ." : "Kiểm tra túi vải ở kệ 2.");
            if (PuzzleService.ShouldShowFieldHint(State, "CH03-PU01"))
                copy.AppendLine("\nĐối chiếu cột phiếu giữ đồ với thẻ buộc vào quai túi, rồi đọc thứ tự mã quanh dòng bị cạo.");
            var choices = new List<ActionChoice>();
            if (!solved && registered && hasNote && hasBag)
            {
                foreach (string code in new[] { "C02", "C03", "C12" })
                    choices.Add(new ActionChoice("Ghi nhận " + code, () =>
                        SaveCheckpoint(() => casework.AttemptCh03Pu01Async(code, true, player.Feet), () =>
                        {
                            if (GateService.G03(State))
                            {
                                voice?.Speak("CH03-D02", "Minh", Line("CH03-D02"));
                                ui.Toast("ĐÃ GHI NHẬN C02", Line("CH03-D06"), 5f);
                            }
                            else ui.Caption("Mã và phiếu giữ đồ chưa khớp.", 4f, true);
                            Ch03ArchiveAnchor();
                        })));
            }
            if (solved && State.chapter == "CH03")
                choices.Add(new ActionChoice("Xem ký ức trên chiếc ghế", StartKnittingMemory));
            choices.Add(new ActionChoice("Đặt sổ xuống", CloseModal));
            ui.Show("KHO HỒ SƠ · TẦNG 3", solved ? "Vị trí người bệnh C02 đã được ghi nhận" : "Đối chiếu một dòng bị xóa",
                copy.ToString(), choices.ToArray(), CloseModal);
        }

        private void StartKnittingMemory()
        {
            CloseModal();
            TransitionAsync(() => casework.SwitchMemoryVignetteAn03Async(player.Feet), true, () =>
            {
                bool named = State.PatientIdentityVerified("C02");
                string caption = named ? Line("CH03-D03") : "[Tiếng kim đan. Chiếc khăn vẫn vắt trên ghế.]";
                ui.Show("KÝ ỨC · KHO HỒ SƠ", named ? "Bà Lành" : "Chiếc ghế cạnh cửa sổ", caption,
                    new[] { new ActionChoice("Trở về kho hồ sơ", EndKnittingMemory) }, EndKnittingMemory);
                if (named) voice?.Speak("CH03-D03", "Bà Lành", caption, directionalPrefix: "[Tiếng kim đan]");
                else scene.atmosphere?.From("curtainSlide", scene.Pad("AN03").position, .4f);
            });
        }

        private void EndKnittingMemory()
        {
            voice?.StopCurrent();
            CloseModal();
            TransitionAsync(() => casework.SwitchMemoryVignetteAn03Async(player.Feet), true);
        }

        private void Ch03InspectKnittingBag()
        {
            ui.Show("KỆ 2", "Túi vải đan dở",
                "Trên quai túi có thẻ K-02. Trong túi còn đôi kim tre và một đoạn khăn len đỏ chưa đan xong.",
                new[]
                {
                    new ActionChoice("Ghi lại thẻ và vật trong túi", () =>
                        SaveCheckpoint(() => casework.TakeCh03EvidenceAsync(EvidenceRegistry.C02YarnBag, "LOC07"), CloseModal)),
                    new ActionChoice("Đặt lại", CloseModal)
                }, CloseModal);
        }
        private void Ch03InspectOptionalIdentityLetter()
        {
            ui.Show("HỒ SƠ BỔ SUNG  /  CH03-PU02 (TÙY CHỌN)", "Phong thư thăm bệnh ở ngăn trên",
                "Phong thư gửi bệnh nhân giường số 2 đề tên: 'Bà Lành'.\n\n" +
                "Xác thực danh tính này là tùy chọn (CH03-PU02), tách biệt với việc xác nhận sự tồn tại của C02.",
                new[]
                {
                    new ActionChoice("Thu thập thư & Ghi nhận danh tính: Bà Lành", () =>
                    {
                        SaveCheckpoint(() => casework.TakeCh03EvidenceAsync(EvidenceRegistry.C02DaughterLetter, "LOC07"), () =>
                        {
                            if (Report(casework.SolveCh03Pu02OptionalIdentity(player.Feet)))
                                ui.Toast("ĐÃ XÁC THỰC DANH TÍNH C02", "C02 — Bà Lành (Sự tồn tại và danh tính được lưu tách biệt).", 5f);
                            CloseModal();
                        });
                    }),
                    new ActionChoice("Đóng", CloseModal)
                }, CloseModal);
        }

        private void Ch03InspectAnonymousBoxC12()
        {
            var choices = new List<ActionChoice>
            {
                new("Ghi nhận sự tồn tại C12 (Giữ nguyên khuyết danh)", () =>
                {
                    SaveCheckpoint(() => casework.TakeCh03EvidenceAsync(EvidenceRegistry.C12UnnamedBox, "LOC07"), () =>
                    {
                        voice?.Speak("CH03-D04", "Minh", Line("CH03-D04"));
                        CloseModal();
                        Ch03CheckG03Unlocked();
                    });
                })
            };
            if (State.chapter == "CH03" && GateService.G03(State))
                choices.Add(new ActionChoice("Đọc ghi chú của Lan trong E03", Ch03ReadC12Note));
            choices.Add(new ActionChoice("Đóng", CloseModal));
            ui.Show("NGĂN KỆ C12  /  HỒ SƠ VÔ DANH", "Hộp đồ cá nhân không tên",
                "Hộp trống. Dưới đáy còn một vết gỉ hình chiếc chìa khóa. Không có thẻ tên hay giấy tờ tùy thân.\n\n" +
                "Sổ E03, dòng C/12: hồ sơ thiếu góc ghi tên.",
                choices.ToArray(), CloseModal);
        }

        private void Ch03ReadC12Note()
        {
            if (State.chapter != "CH03" || State.layer != Reality.R || !GateService.G03(State)) return;
            string line = LineAtBeat("CH03-D07", 1, "B05");
            voice?.RecordTranscriptOnly("CH03-D07", "Minh", line, "B05");
            ui.Show("SỔ RIÊNG CỦA LAN · E03", "Ghi chú cạnh mã C12",
                "Thiếu niên gầy, khoảng mười lăm đến mười sáu tuổi. Góc ghi tên đã bị xé. " +
                "Cậu ấy nói về mũi dép có chỉ xanh trước khi tôi mở lá thư của C05. Có thể cậu đã nghe ai kể. " +
                "Đêm nào cậu cũng nghe tôi hát ru bé An năm nốt nhạc. Cậu xin tự cất chiếc chìa có vết sứt ở răng và dặn tôi ghi ‘tôi giữ chìa’.\n\nMinh · " + line,
                new[] { new ActionChoice("Quay lại hộp C12", Ch03InspectAnonymousBoxC12) }, CloseModal);
        }

        private void TriggerLoc07RadioDistraction()
        {
            Vector3 radioPos = scene.Ch03RadioPoint != null ? scene.Ch03RadioPoint.position : new Vector3(-1.85f, 0.85f, 35.85f);
            scene.atmosphere?.From("paStatic", radioPos);
            orderlyAi?.TriggerDistraction(radioPos, HunterOrderlyAI.NoiseDistractionRadius);
            ui.Toast("ĐÃ BẬT ĐÀI CASSETTE · ĐÁNH LẠC HƯỚNG", "Tiếng băng rè trong phòng họp nhỏ vang sang dãy kệ tầng 3.", 5f);
        }

        private void Ch03CheckG03Unlocked()
        {
            if (GateService.G03(State))
            {
                scene.atmosphere?.From("doorCreak", new Vector3(4f, 1.5f, 37.2f));
                ui.Toast("CỬA KHU LƯU TRỮ ĐÃ MỞ · G03", Line("CH03-D06"), 6f);
            }
        }

        private void Ch03EndOfChapter()
        {
            bool hasReadC12 = State.Happened("FS19");
            ui.Show("PHÒNG TRỰC · TẦNG 2", "Những cái tên bị xóa",
                "Sổ E03 có đủ căn cứ để ghi nhận người bệnh C02.\n" +
                (hasReadC12 ? "C12 · Chưa xác định danh tính.\n" : "Cần đọc dòng C/12 và đối chiếu hộp chìa khóa ở kho tầng 3.\n"),
                new[]
                {
                    new ActionChoice("Vào phòng trực", () => SaveCheckpoint(casework.BeginCh04Async, () =>
                    {
                        CloseModal();
                        ui.ShowChapterCard("CHƯƠNG 04", "NGƯỜI DẪN ĐƯỜNG", 4.6f);
                    }), hasReadC12),
                    new ActionChoice("Quay lại hành lang", CloseModal)
                }, CloseModal);
        }
        private void CloseModal()
        {
            voice?.SetPaused(false);
            firstEscape = true;
            inspector?.Close();
            backpackStage?.Close();
            ui.Close();
        }

        private static string Status(FormStatus status) => status switch
        {
            FormStatus.Affirmed => "Đã xác nhận 0",
            FormStatus.Unverified => "Chờ kiểm tra",
            FormStatus.Refuted => "Có sai lệch",
            _ => "Chưa ghi"
        };
    }
}
