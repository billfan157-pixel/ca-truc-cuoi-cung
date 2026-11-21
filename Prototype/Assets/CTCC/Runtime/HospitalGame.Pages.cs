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
            playedHe01Trolley = false;
            playedHe04CorridorCut = false;
            playedCh01Ex01Monologue = false;
            ch01StartUnscaledTime = Time.unscaledTime;
            if (!Report(casework.Begin(player.Feet, startOptions))) return;
            scene.interactables?.ResetSessionProps();
            if (scene.staffCrossDoor != null) scene.staffCrossDoor.isOpen = false;
            dreadDirector?.ResetSeed(State.dreadSeed);
            RestoreJournal(State);
            live = true;
            player.Teleport(scene.Pad("AP00"));
            player.RestoreBattery(State.torchSeconds);
            ui.ShowChapterCard("CHƯƠNG 01  ·  17.11.2012", "NGƯỜI TRỞ VỀ", 4.4f);
            Letter();
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
            playedCh01Ex01Monologue = State != null && State.transcript.Exists(t => t.LineId == "CH01-D19");
            RestoreJournal(State);
            live = true;
            player.Teleport(safe);
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
                transaction.OnPad(player.Feet, "AP02") ? "Có thể lưu tay tại sổ trực." : "Lưu tay khả dụng tại sổ trực ở lễ tân.",
                new[]
                {
                    new ActionChoice("Tiếp tục", CloseModal),
                    new ActionChoice("Lưu tay", () => { if (transaction.KeepManual()) { CloseModal(); ui.Caption("Đã lưu ca trực.", 4, true); } },
                        transaction.OnPad(player.Feet, "AP02")),
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

        private void Letter()
        {
            string body = "Giấy hẹn có chữ ký Bảy\n23 giờ, 17/11/2012 · Chốt cổng phụ. Tôi trực ca cuối.\n\n" +
                "Tin nhắn mới\n" + Line("CH01-D15") + "\n\n" +
                "Thư cũ của Lan\nAnh Minh, em cần nói với anh một chuyện ở bệnh viện. Em không muốn để chuyện này qua thêm một ngày nữa.\n\n" +
                "Mẩu báo cũ (rách mất góc dưới)\nVụ cháy rạng sáng 18/11/2005 ở An Sinh có 13 người tử vong; cột danh sách nạn nhân phía dưới bị rách và nhòe nước mưa.\n\n" +
                "Trong bì thư có chìa cổng phụ và chìa khu C đã thay khóa.";
            var envModel = scene.InspectTemplate("E10a");
            Action confirmRead = () => SaveCheckpoint(() => casework.ReadLetterAsync(player.Feet), () =>
                {
                    voice?.RecordTranscriptOnly("CH01-D15", "Bảy (SMS)", Line("CH01-D15"));
                    CloseModal();
                    voice?.Speak("CH01-D01", "Minh", Line("CH01-D01"));
                });

            if (envModel != null && inspector != null && qaFolder == null)
            {
                scene.atmosphere?.From("paper", player.Feet + Vector3.up);
                inspector.Open(envModel, null, 1f, ActiveVolume(), observationId: "E10a");
                ui.ShowInspectSidebar("THƯ · TỜ HẸN · CHÌA KHÓA", "Bì thư của Bảy", body,
                    new[]
                    {
                        new ActionChoice("Ghi tin nhắn vào sổ trực", confirmRead),
                        new ActionChoice("Bản chữ lớn", () =>
                        {
                            inspector.Close();
                            ui.Show("THƯ · TỜ HẸN · TIN NHẮN", "Bì thư của Bảy", body,
                                new[]
                                {
                                    new ActionChoice("Ghi tin nhắn vào sổ trực", confirmRead),
                                    new ActionChoice("Đóng", CloseModal)
                                }, CloseModal);
                        })
                    }, CloseModal, () => inspector.Close());
            }
            else
            {
                ui.Show("THƯ · TỜ HẸN · TIN NHẮN", "Bì thư của Bảy", body,
                    new[] { new ActionChoice("Ghi tin nhắn vào sổ trực", confirmRead) }, CloseModal);
            }
        }

        private void LanTranscript()
        {
            // Ensure core encounter lines are always preserved in Transcript even if player skips immediately (CH01-Q06)
            voice?.RecordTranscriptOnly("CH01-D03", "Lan", Line("CH01-D03"));
            voice?.RecordTranscriptOnly("CH01-D04", "Minh", Line("CH01-D04"));
            voice?.RecordTranscriptOnly("CH01-D05", "Lan", Line("CH01-D05"));
            voice?.RecordTranscriptOnly("CH01-D07", "Lan", Line("CH01-D07"));

            Action closeEncounter = () =>
            {
                voice?.StopCurrent();
                lanDirector?.StopEncounter(keepMotifQuietly: true);
                CloseModal();
            };

            string encounterText =
                "Lan  —  " + Line("CH01-D03") +
                "\n\nMinh  —  " + Line("CH01-D03b") +
                "\n\nLan  —  " + Line("CH01-D03c") +
                "\n\nMinh  —  " + Line("CH01-D04") +
                "\n\nLan  —  " + Line("CH01-D04b") +
                "\n\nMinh  —  " + Line("CH01-D04c") +
                "\n\nLan  —  " + Line("CH01-D05") +
                "\n\nMinh  —  " + Line("CH01-D05b") +
                "\n\nLan  —  " + Line("CH01-D05c") +
                "\n\nLan  —  " + Line("CH01-D07") +
                "\n\nMinh  —  " + Line("CH01-D07b") +
                "\n\nMinh vẫn nhớ lần anh đã từ chối mở cửa. Anh chưa biết chuyện gì xảy ra sau đó.";

            ui.ShowInspectSidebar("BÀN TRỰC LỄ TÂN  /  LỚP B · 2005", "Trần Ngọc Lan",
                encounterText,
                new[]
                {
                    new ActionChoice("Xem bảng kẹp", closeEncounter),
                    new ActionChoice("Hỏi về ca trực đêm nay", () =>
                    {
                        lanDirector?.PlayOptionalLine("CH01-D08", "Lan", Line("CH01-D08"), 0.30f, 0.48f, 0.12f);
                        ui.ShowInspectSidebar("BÀN TRỰC LỄ TÂN  /  ĐỐI THOẠI", "Về bệnh viện",
                            "Lan  —  " + Line("CH01-D08") + "\n\nLan  —  " + Line("CH01-D09"),
                            new[]
                            {
                                new ActionChoice("Nghe tiếp", () => lanDirector?.PlayOptionalLine("CH01-D09", "Lan", Line("CH01-D09"), 0.25f, 0.55f, 0.20f)),
                                new ActionChoice("Quay lại", LanTranscript)
                            }, LanTranscript);
                    }),
                    new ActionChoice("Đọc đối thoại về ca cũ", () =>
                    {
                        lanDirector?.PlayOptionalLine("CH01-D16", "Lan", Line("CH01-D16"), 0.18f, 0.55f, 0.45f);
                        ui.ShowInspectSidebar("BẢN GHI CUỘC GẶP", "Về ca cũ",
                            "Lan  —  " + Line("CH01-D16"),
                            new[] { new ActionChoice("Khép sổ", closeEncounter) }, closeEncounter);
                    })
                }, closeEncounter);
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

        private void InspectRegisteredObservation(string obsId, string location, Action onReturn = null)
        {
            var spec = ObservationRegistry.Get(obsId);
            if (spec == null) return;
            SaveCheckpoint(() => casework.ObserveAsync(obsId, location, ObservationStage.Examined), () =>
                OpenObservationInspector(spec, onReturn, staffDiscovery: location == "STAFF"));
        }

        private void OpenObservationInspector(ObservationSpec spec, Action onReturn = null, bool staffDiscovery = false)
        {
            var frontTex = Image(spec.FrontTexture);
            var backTex = spec.HasBackSide ? Image(spec.BackTexture) : null;
            var details = BuildDetailTargetsFor(spec.Id);
            bool physicalView = frontTex != null && inspector != null && qaFolder == null;
            string description = ObservationDescription(spec.Id);
            string closeLabel = onReturn != null ? "Quay lại sổ trực" : "Đặt xuống & tiếp tục";
            // The one-sided notice is read in its entirety in the ordinary viewer.
            bool examined = spec.Id == ObservationRegistry.WardOverflowNotice;

            void Finish()
            {
                inspector?.Close();
                if (onReturn != null) onReturn();
                else CloseModal();
                if (staffDiscovery && examined) QueueStaffReaction(spec.Id);
            }

            void AddAnnotationChoice(List<ActionChoice> choices, bool largePrint)
            {
                if (!CanMarkF101Document(spec.Id)) return;
                choices.Add(new ActionChoice("Đánh dấu trên tài liệu", () =>
                {
                    inspector?.Close();
                    AnnotateF101Document(spec.Id, largePrint, Finish);
                }));
            }

            void ShowLargePrint()
            {
                SaveCheckpoint(() => casework.MarkObservationDetailAsync(spec.Id, "LARGE_PRINT", flippedBack: spec.HasBackSide), () =>
                {
                    inspector?.Close();
                    examined = true;
                    var choices = new List<ActionChoice>();
                    if (physicalView) choices.Add(new ActionChoice("Xem lại bản 3D", ShowPhysical));
                    AddAnnotationChoice(choices, largePrint: true);
                    choices.Add(new ActionChoice(closeLabel, Finish));
                    ui.Show(spec.ProvenanceLabel, spec.Title, BuildLargePrintTranscript(spec),
                        choices.ToArray(), Finish, frontTex);
                });
            }

            void ShowPhysical()
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
                    });
                ShowSide();
            }

            void ShowSide(string revealedDetail = null)
            {
                bool back = inspector.ShowingBack;
                var choices = new List<ActionChoice>();
                if (backTex != null)
                    choices.Add(new ActionChoice(back ? "[Space] Lật về mặt trước" : "[Space] Lật sang mặt sau", inspector.Flip));
                if (back && inspector.HasRakingIndent)
                    choices.Add(new ActionChoice("[T] Nghiêng đón sáng (soi vết hằn bút bi)", () => inspector.TiltForRakingLight()));
                for (int i = 0; i < details.Length; i++)
                {
                    if (details[i].OnBackSide != back) continue;
                    int idx = i;
                    choices.Add(new ActionChoice($"[{idx + 1}] Soi: {details[idx].Label}", () => inspector.FocusDetail(idx)));
                }
                choices.Add(new ActionChoice("Mở bản chữ lớn", ShowLargePrint));
                AddAnnotationChoice(choices, largePrint: false);
                choices.Add(new ActionChoice(closeLabel, Finish));

                string side = back ? "MẶT SAU" : "MẶT TRƯỚC";
                string hint = (backTex != null ? "Space/R: Lật giấy · " : "") +
                    (back && inspector.HasRakingIndent ? "T: Nghiêng đón sáng (25°–40°) · " : "") +
                    "Chuột/WASD: Xoay · Lăn/Q-Z: Thu phóng · 0: Đặt lại";
                ui.ShowInspectSidebar(spec.ShortCode + "  ·  " + side, spec.Title,
                    revealedDetail ?? description ?? BuildFullObservationBody(spec, State.FindObservation(spec.Id)),
                    choices.ToArray(), Finish, inspector.Close, hint);
            }

            if (physicalView)
            {
                scene.atmosphere?.From("paper", player.Feet + player.transform.forward * 0.5f + Vector3.up);
                ShowPhysical();
            }
            else ShowLargePrint();
        }

        private static string ObservationDescription(string id) => id switch
        {
            ObservationRegistry.F101Sheet => "Một phiếu in sẵn kẹp trên bảng bàn giao.",
            ObservationRegistry.Bed4Card => "Một tấm thẻ giấy treo ở đầu giường.",
            ObservationRegistry.WardOverflowNotice => "Một tờ thông báo dán trên bảng tin nhân viên.",
            ObservationRegistry.ShiftGuide => "Một bản trích quy chế lưu ở bàn điều dưỡng.",
            _ => null
        };

        private static InspectDetailTarget[] BuildDetailTargetsFor(string obsId) => obsId switch
        {
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
            sb.Append("Loại: ").Append(ObservationRegistry.CategoryLabel(spec.Category)).Append("\n\n");
            sb.Append("Dữ kiện quan sát:\n").Append(spec.FactSummary).Append("\n\n");
            sb.Append("Nhận định thận trọng:\n").Append(spec.InterpretationNote);
            if (rec != null && rec.flippedBack)
                sb.Append("\n\n✓ Đã kiểm tra mặt sau tài liệu.");
            return sb.ToString();
        }

        private static string BuildLargePrintTranscript(ObservationSpec spec)
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
                if (ObservationRakingText(spec.Id) is string raking)
                    sb.Append("\n\nVẾT HẰN BÚT BI (SOI NGHIÊNG ÁNH SÁNG)\n").Append(raking);
                return sb.ToString();
            }
            sb.Append("NỘI DUNG CHÍNH:\n").Append(spec.FactSummary).Append("\n\n");
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
                "Nghiêng thẻ 25°–40° đón sáng thấy vết hằn bút bi đè từ tờ giấy phía trên: «03g17 · CHUYỂN TẠM — C01?» — không có trong mực in thường.",
            ObservationRegistry.F101Sheet =>
                "Nghiêng phiếu 25°–40° đón sáng thấy dòng bị ấn đè ở mặt sau: «GIƯỜNG 04 — CHƯA CHỐT SỔ / GẠCH BỎ MỤC TẠM → GHI TRÒN: 0 / 03g17 · MẤT LIÊN LẠC TẦNG TRÊN».",
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
            ObservationRegistry.ShiftGuide =>
                "Giấy kẹp của Lan gửi Minh: Bảng tổng hợp phòng 101 ghi 0, nhưng sau rèm còn một mục tối 17/11 thiếu chữ ký. Lan nhắc Minh xem cả hai mặt thẻ trước khi ký.",
            _ => null
        };

        private static void AppendObservationNotes(StringBuilder body, ObservationSpec spec, ObservationRecord record)
        {
            if (record == null || ObservationDescription(spec.Id) == null) return;
            if (record.flippedBack && ObservationBackText(spec.Id) is string back)
                body.Append("\n  Mặt sau: ").Append(back);
            if (record.inspectedDetails != null && record.inspectedDetails.Contains(CorroborationService.RakingDetailId) &&
                ObservationRakingText(spec.Id) is string raking)
                body.Append("\n  Vết hằn bút bi: ").Append(raking);
            if (record.stage >= ObservationStage.Interpreted || spec.Id == ObservationRegistry.WardOverflowNotice)
            {
                string interpretation = spec.Id == ObservationRegistry.F101Sheet
                    ? "Số tổng hợp chỉ dựa trên các dòng có trong mẫu. Cần đối chiếu với những gì ghi nhận tại phòng."
                    : spec.InterpretationNote;
                body.Append("\n  Nhận định: ").Append(interpretation);
            }
        }

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

        private void Take3D(string id, string title, string copy, string imageId, string modelId)
        {
            var tex = Image(imageId);
            var tmpl = string.IsNullOrEmpty(modelId) ? null : scene.InspectTemplate(modelId);
            scene.atmosphere?.From("paper", player.Feet + player.transform.forward * 0.5f + Vector3.up);

            Action confirmTake = () => SaveEvidence(id, () =>
            {
                CloseModal();
                ui.Toast("ĐÃ THU THẬP VẬT CHỨNG", title + " đã được lưu vào sổ trực.", 4.5f);
            });

            if ((tmpl != null || tex != null) && inspector != null && qaFolder == null)
            {
                float aspect = tex != null ? (float)tex.width / tex.height : 1f;
                inspector.Open(tmpl, tex, aspect, ActiveVolume());
                ui.ShowInspectSidebar("KHO GIƯỜNG 101  /  HIỆN TRƯỜNG R", title, copy,
                    new[]
                    {
                        new ActionChoice(State.Has(id) ? "Đã ghi vào sổ" : "Thu thập & ghi vào sổ", confirmTake),
                        new ActionChoice("Bản chữ lớn", () =>
                        {
                            inspector.Close();
                            ui.Show("KHO GIƯỜNG 101  /  HIỆN TRƯỜNG R", title, copy,
                                new[] { new ActionChoice(State.Has(id) ? "Đã ghi vào sổ" : "Ghi vật chứng", confirmTake), new ActionChoice("Quay lại", CloseModal) },
                                CloseModal, tex);
                        }),
                        new ActionChoice("Đặt lại", CloseModal)
                    }, CloseModal, () => inspector.Close());
            }
            else
            {
                ui.Show("KHO GIƯỜNG 101  /  HIỆN TRƯỜNG R", title, copy,
                    new[]
                    {
                        new ActionChoice(State.Has(id) ? "Đã ghi vào sổ" : "Ghi vật chứng", confirmTake),
                        new ActionChoice("Quay lại", CloseModal)
                    }, CloseModal, tex);
            }
        }

        private void SaveEvidence(string id, Action committed) =>
            SaveCheckpoint(() => casework.TakeAsync(id), committed, closeOnFailure: true);

        private void SaveCheckpoint(Func<System.Threading.Tasks.Task<CommitResult>> save, Action committed = null, bool closeOnFailure = false)
        {
            if (!busy) StartCoroutine(WaitForCheckpoint(save, committed, closeOnFailure));
        }

        private IEnumerator WaitForCheckpoint(Func<System.Threading.Tasks.Task<CommitResult>> save, Action committed, bool closeOnFailure)
        {
            while (savingDialogue) yield return null;
            busy = true;
            ui.ModalInputBlocked = true;
            RefreshControls();
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
            }
            else
            {
                var result = pending.GetAwaiter().GetResult();
                if (result != CommitResult.Applied && closeOnFailure) CloseModal();
                if (Report(result)) committed?.Invoke();
            }
        }

        private void Form()
        {
            if (!casework.CanRevise) { ui.Caption("Phiếu có thể đối chiếu lại tại điểm neo phòng 101.", 4, true); return; }
            var record = State.f101;
            string pad = transaction.OnPad(player.Feet, "AP04") ? "" : "\nĐứng trên vạch đồng cạnh cửa để ký.";
            string summary = "Số bệnh nhân chưa bàn giao: 0.\n" +
                (record.revision == 0 ? "Chưa ký." : $"Lần ghi {record.revision} · {Status(record.current)}") + pad;
            ui.Show("SỔ BÀN GIAO  /  18.11.2005  ·  MẪU AS-05", "Phiếu phòng 101 (F101)", summary,
                new ActionChoice[]
                {
                    new("Soi phiếu F101", () => InspectRegisteredObservation(ObservationRegistry.F101Sheet, "LOC03", Form)),
                    new("A    Xác nhận 0 tồn đọng", () => PreviewForm(FormStatus.Affirmed, null)),
                    new("B    Để ngỏ", () => PreviewForm(FormStatus.Unverified, null)),
                    new("C    Ghi nhận bất nhất", OpenF101Notes),
                    new("Đặt phiếu xuống", CloseModal)
                }, CloseModal, Image("F101"));
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

        private void AnnotateF101Document(string id, bool largePrint, Action back, int focus = 0)
        {
            if (!CanMarkF101Document(id)) { back(); return; }
            var spec = ObservationRegistry.Get(id);
            bool report = id == ObservationRegistry.F101Sheet;
            var fields = report ? new[] { F101Annotations.Table, F101Annotations.Zero }
                : new[] { F101Annotations.CardStatus, F101Annotations.Receiver };
            // Field bounds from F101.png and ObsCard.png (1024 x 1360, top-left origin).
            const float paperWidth = 1024f;
            const float paperHeight = 1360f;
            var bounds = report
                ? new[] { new Rect(110, 394, 844, 256), new Rect(110, 684, 844, 132) }
                : new[] { new Rect(112, 622, 840, 162), new Rect(536, 816, 416, 215) };

            var texture = Image(spec.FrontTexture);
            var choices = new List<ActionChoice>();
            var hotspots = new DocumentHotspot[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                int index = i;
                string field = fields[i];
                bool marked = State.f101Annotations.MarkedHotspot(id) == field;
                Action toggle = () => SaveCheckpoint(() => casework.ToggleF101MarkAsync(id, field),
                    () => AnnotateF101Document(id, largePrint, back, index));
                choices.Add(new ActionChoice((marked ? "[✓]  " : "[ ]  ") + AnnotationLabel(field), toggle));
                var b = bounds[i];
                var uv = Rect.MinMaxRect(b.xMin / paperWidth, 1f - b.yMax / paperHeight, b.xMax / paperWidth, 1f - b.yMin / paperHeight);
                hotspots[i] = new DocumentHotspot(uv, marked, toggle);
            }

            if (texture != null)
                choices.Add(new ActionChoice(largePrint ? "Xem mặt giấy" : "Bản chữ lớn",
                    () => AnnotateF101Document(id, !largePrint, back)));
            choices.Add(new ActionChoice("Quay lại", back));
            string body = "Chọn dòng muốn đánh dấu. Chọn lại để bỏ dấu.\n\n" +
                (report
                    ? "01 · Đã xuất viện\n02 · Đã chuyển Nội B lúc 19g20\n03 · Đã xuất viện lúc 10g15\n\nSố bệnh nhân chưa bàn giao: 0 (Không còn tồn đọng)."
                    : "Phòng 101 · Giường 4 · 17/11/2005\nTheo dõi ca khám sốt tạm.\n\nTình trạng: Chưa bàn giao.\nNgười giao: T. N. L., chữ ký nhòe.\nNgười nhận: ô ký nhận để trống.");
            ui.ShowAnnotatedDocument(report ? "F101" : "AS-05B", spec.Title, body, choices.ToArray(), back,
                largePrint ? null : texture, paperWidth / paperHeight, hotspots, largePrint, focus);
        }

        private void OpenF101Notes()
        {
            var notes = State.f101Annotations;
            var choices = new List<ActionChoice>();
            var body = new StringBuilder();
            foreach (var mark in notes.marks)
                body.Append(ObservationRegistry.Get(mark.observationId).ShortCode).Append(" · ")
                    .AppendLine(AnnotationLabel(mark.hotspotId));
            if (notes.marks.Count == 0) body.AppendLine("Chưa đánh dấu dòng nào.");
            if (notes.HasLink)
                body.AppendLine().AppendLine("Nối thẻ với hiện trường giường 4")
                    .Append("Vết bánh giường trên nền, chăn gấp vội và đôi dép nhựa đặt lệch.");
            if (!State.Observed(ObservationRegistry.Bed4Card) && !State.Observed(ObservationRegistry.Bed4Traces))
                body.AppendLine().Append("Có thể đặt phiếu xuống để xem buồng bệnh.");

            foreach (string id in new[] { ObservationRegistry.F101Sheet, ObservationRegistry.Bed4Card })
            {
                if (CanMarkF101Document(id))
                {
                    string label = id == ObservationRegistry.F101Sheet ? "Đánh dấu trên F101" : "Đánh dấu trên thẻ AS-05B";
                    choices.Add(new ActionChoice(label, () => AnnotateF101Document(id, largePrint: false, OpenF101Notes)));
                }
            }
            if (!string.IsNullOrEmpty(notes.MarkedHotspot(ObservationRegistry.Bed4Card)) &&
                HypothesisService.HasExaminedInLayerB(State, ObservationRegistry.Bed4Traces))
            {
                choices.Add(new ActionChoice(notes.HasLink ? "Bỏ liên kết với dấu vết giường 4" : "Nối dấu vết giường 4 với ô đã đánh dấu trên thẻ",
                    () => SaveCheckpoint(casework.ToggleF101TraceLinkAsync, OpenF101Notes)));
            }
            if (State.Observed(ObservationRegistry.Bed2Transfer))
            {
                string other = State.Observed(ObservationRegistry.Bed4Card) ? ObservationRegistry.Bed4Card : ObservationRegistry.F101Sheet;
                choices.Add(new ActionChoice("Đặt cạnh phiếu chuyển giường 02",
                    () => CompareDocumentsPage(other, ObservationRegistry.Bed2Transfer, OpenF101Notes)));
            }

            var submission = new DeductionSubmission
            {
                selectedEvidenceIds = new List<string> { ObservationRegistry.F101Sheet, ObservationRegistry.Bed4Card, ObservationRegistry.Bed4Traces },
                claimId = HypothesisService.ClaimUnreliableZero,
                annotationVersion = F101Annotations.Version,
                annotations = notes.Clone()
            };
            var evaluation = HypothesisService.EvaluateF101(State, FormStatus.Refuted, submission);
            bool ready = evaluation.Accepted;
            if (!ready && !string.IsNullOrEmpty(evaluation.Message))
            {
                body.AppendLine().AppendLine()
                    .Append("Minh (tự vấn) — ").Append(evaluation.Message);
            }
            choices.Add(new ActionChoice("Viết vào phiếu", () => PreviewForm(FormStatus.Refuted, submission), ready));
            choices.Add(new ActionChoice("Quay lại phiếu F101", Form));
            ui.Show("SỔ TRỰC  /  PHÒNG 101", "Đối chiếu F101", body.ToString(), choices.ToArray(), Form);
        }

        private void PreviewForm(FormStatus choice, DeductionSubmission submission)
        {
            var sb = new StringBuilder();
            if (choice == FormStatus.Affirmed)
                sb.Append("Xác nhận ca trực còn 0 bệnh nhân chưa bàn giao theo bản kê in sẵn.");
            else if (choice == FormStatus.Unverified)
                sb.Append("Để ngỏ con số bàn giao — ghi rõ chưa đủ căn cứ để xác nhận số 0, giữ nguyên trạng chờ kiểm tra.");
            else
            {
                sb.Append("Ghi nhận bất nhất\n").Append(HypothesisService.GetClaim(submission?.claimId)?.Label).Append("\n");
                if (submission?.selectedEvidenceIds != null && submission.selectedEvidenceIds.Count > 0)
                {
                    sb.Append("\nChứng cứ đính kèm:");
                    foreach (var id in submission.selectedEvidenceIds)
                    {
                        var spec = ObservationRegistry.Get(id);
                        if (spec != null) sb.Append("\n• ").Append(spec.Title);
                    }
                }
            }
            sb.Append("\n\nNgười ghi · N. H. Minh");
            Action back = choice == FormStatus.Refuted ? OpenF101Notes : Form;

            ui.Show("F101  /  N. H. MINH", "Kiểm tra trước khi ký", sb.ToString(),
                new[]
                {
                    new ActionChoice("Quay lại", back),
                    new ActionChoice("Xác nhận và đóng dấu phiếu", () => StartCoroutine(SignF101(choice, submission)))
                }, back, Image("F101"));
        }

        private IEnumerator SignF101(FormStatus choice, DeductionSubmission submission)
        {
            if (busy) yield break;
            if (!casework.CanRevise || !transaction.OnPad(player.Feet, "AP04"))
            {
                CloseModal();
                ui.Caption("Đứng trên vạch đồng cạnh cửa để ký phiếu.", 4f, true);
                yield break;
            }
            busy = true;
            ui.ModalInputBlocked = true;
            RefreshControls();
            while (savingDialogue || streaming) yield return null;

            // Leave the confirmation in place during the save so the signed sheet is revealed
            // with the stamp, at the same viewpoint used to open the form.
            int previousRevision = State.f101.revision;
            var paper = scene.f101WallRenderer;
            Vector3 paperPosition = paper != null ? paper.bounds.center : player.Feet + Vector3.up;
            if (choice != State.f101.current)
            {
                dreadDirector?.NotifyMajorBeatQuietWindow(10f);
                voice?.StopCurrent();
                scene.atmosphere?.From("signaturePen", paperPosition);
                ui.Caption("[Tiếng ngòi bút trên giấy]", 0.6f, aboveModal: true);
                yield return new WaitForSecondsRealtime(0.52f);
            }
            var task = casework.SubmitF101Async(choice, submission, player.Feet);
            while (!task.IsCompleted) yield return null;
            var outcome = task.IsFaulted ? CommitResult.SaveUnavailable : task.GetAwaiter().GetResult();
            if (task.IsFaulted) Debug.LogException(task.Exception);
            ui.ModalInputBlocked = false;
            busy = false;
            RefreshControls();
            ui.Close();
            if (!Report(outcome)) yield break;
            if (State.f101.revision == previousRevision) yield break;
            StartCoroutine(PlayF101Signature(State.f101.current, State.f101.revision, paperPosition));
        }

        private IEnumerator PlayF101Signature(FormStatus choice, int revision, Vector3 paperPosition)
        {
            scene.atmosphere?.From("stamp", paperPosition);
            scene.atmosphere?.LeaveSilenceAfterStamp(1.25f);
            ui.Caption("[Dấu cao su chạm giấy]", 1.1f);
            if (State.layer != Reality.B) yield break;

            f101PaperRevealUntil = Time.unscaledTime + 1.45f;
            ui.Prompt(null);
            yield return new WaitForSecondsRealtime(1.45f);
            if (!live || ui.IsModal || State.layer != Reality.B || State.f101.revision != revision) yield break;
            string paId = choice == FormStatus.Affirmed ? "CH01-PA01" : choice == FormStatus.Unverified ? "CH01-PA02" : "CH01-PA03";
            Vector3 paPosition = scene.PaSpeakerAnchor != null ? scene.PaSpeakerAnchor.transform.position : new Vector3(5.5f, 2.6f, 3.35f);
            scene.atmosphere?.From("paStatic", paPosition, choice == FormStatus.Affirmed ? 0.20f : 0.46f);
            string direction = scene.atmosphere?.DirectionTo(paPosition) ?? "ngoài cửa phòng 101";
            voice?.Speak(paId, "Loa hành lang", Line(paId), scene.PaSpeakerAnchor,
                directionalPrefix: $"[Loa hành lang · {direction}]");
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
            choices.Add(new ActionChoice("Trở lại điểm neo", Anchor));
            ui.Show("HỒ SƠ TRONG KHO 101", "Bức tranh của An", missing ??
                "Tranh ghi lại một cánh cửa có khóa. Vòng cũ ghi C01. Phiếu chuyển ghi An, khám sốt tạm ở 101/4 ngày 17/11/2005, rồi trở lại C01.",
                choices.ToArray(), Anchor, State.Has(EvidenceRegistry.Drawing) ? Image("Drawing") : null);
        }

        private void Journal()
        {
            var r = State.f101;
            int fragments = 0;
            foreach (var id in EvidenceRegistry.DrawingFragments) if (State.Has(id)) fragments++;
            int obsCount = State.observed != null ? State.observed.Count : 0;
            int docCount = CollectObservedDocuments().Count;

            string status = $"F101 · {Status(r.current)}  (Lần ghi: {r.revision} · Lần đầu: {Status(r.firstDecision)})\n" +
                $"Quan sát & tài liệu đã ghi nhận: {obsCount} mục  ({docCount} tài liệu có thể so sánh)";
            if (CorroborationService.IsCorroborated(State))
                status += "\n✓ Ghi chú ngoại lệ · Mốc 03:17 đã được đối chiếu chéo.";
            if (State.chapter == "CH02")
                status += "\n\nHiện trường R · " + (State.Has("E12") ? "Đã đối chiếu E12" : "Chưa đối chiếu chỗ kê") +
                    "\nTranh " + fragments + "/3 · Vòng " + (State.Has(EvidenceRegistry.Wristband) ? "đã ghi" : "chưa có") +
                    " · Phiếu chuyển " + (State.Has(EvidenceRegistry.Transfer) ? "đã ghi" : "chưa có") +
                    "\nHồ sơ " + (GateService.G02(State) ? "C01 đã xác thực" : "còn mở");
            if (State.chapter is "CH03" or "CH04")
                status += "\n\nC02 · " + (State.PatientIdentityVerified("C02") ? "Bà Lành" :
                    State.PatientConfirmed("C02") ? "Đã ghi nhận người bệnh, còn thiếu danh tính" : "Cần đối chiếu sổ bàn giao") +
                    "\nC12 · " + (State.PatientConfirmed("C12") ? "Chưa xác định danh tính" : "Chưa đọc dòng C/12") +
                    (State.Happened("FS10") ? "\nĐã thấy bản nhận niêm phong trong hành lang phòng trực." : "");
            if (State.chapter == "CH04")
                status += $"\n\nBảng tải B1 · {ChapterFour.TotalLoad(State.servicePower)}/4" +
                    (GateService.H05(State) ? "\nĐã đối chiếu hồ sơ tiếp nhận với thẻ Lan và phiếu thu hồi." : "");

            var choices = new List<ActionChoice>
            {
                new($"Quan sát & Nguồn chứng cứ ({obsCount})", ObservationsPage),
                new($"So sánh hai tài liệu ({docCount})", () =>
                {
                    var docs = CollectObservedDocuments();
                    string left = docs.Count > 0 ? docs[0].Id : ObservationRegistry.F101Sheet;
                    string right = docs.Count > 1 ? docs[1].Id : left;
                    CompareDocumentsPage(left, right, Journal);
                }, docCount >= 2),
                new($"Lịch sử quyết định & lập luận F101 ({r.revision})", F101HistoryPage),
                new("Đọc thư và tin nhắn của Bảy (E10a)", Letter)
            };
            bool has0317Clue = State.Observed(ObservationRegistry.StoppedClock0317) ||
                State.Observed(ObservationRegistry.Bed4Card) ||
                CorroborationService.HasShiftContext(State) ||
                CorroborationService.IsCorroborated(State);
            if (State.chapter is ("CH01" or "CH02") && has0317Clue)
                choices.Add(new ActionChoice(
                    CorroborationService.IsCorroborated(State) ? "✓ Ghi chú ngoại lệ · Mốc 03:17" : "Ghi chú ngoại lệ · Đối chiếu mốc 03:17",
                    () => Exception0317Page(Journal)));
            if (State.Happened("CH01-T04")) choices.Add(new ActionChoice("Bản ghi ở bàn trực với Lan", LanTranscript));
            choices.Add(new ActionChoice("Bản ghi hội thoại (" + (voice != null ? voice.Transcript.Count : 0) + ")", TranscriptPage));
            if (casework.CanRevise) choices.Add(new ActionChoice("Mở phiếu F101", Form));
            if (State.chapter == "CH02") choices.Add(new ActionChoice("Xem hồ sơ trong kho", Identify));
            choices.Add(new ActionChoice("Khép sổ", CloseModal));
            ui.Show("SỔ ĐIỀU TRA & ĐỐI CHIẾU CHỨNG CỨ", State.chapter switch
            {
                "CH01" => "Người trở về",
                "CH03" => "Những cái tên bị xóa",
                "CH04" => "Người dẫn đường",
                _ => "Phòng bệnh trống"
            }, status,
                choices.ToArray(), CloseModal);
        }

        private void Exception0317Page(Action back)
        {
            bool hasClock = CorroborationService.HasClock(State);
            bool hasCard = CorroborationService.HasCardTimestamp(State);
            bool hasShift = CorroborationService.HasShiftContext(State);
            bool hasRaking = CorroborationService.HasRakingIndent(State);
            bool done = CorroborationService.IsCorroborated(State);
            bool canDo = CorroborationService.CanCorroborate0317(State);

            var sb = new StringBuilder();
            sb.AppendLine("NHÁNH GIẢI MÃ TÙY CHỌN  ·  KHÔNG CẦN ĐỂ QUA CỬA CHƯƠNG 1");
            sb.AppendLine();
            sb.AppendLine((hasClock ? "[✓]  " : "[ ]  ") + "Đồng hồ treo sau quầy lễ tân đứng kim đúng 03:17");
            sb.AppendLine((hasCard ? "[✓]  " : "[ ]  ") + "Thẻ đầu giường 4 (AS-05B) có dòng ghi lề «Kiểm cuối ca: 03g17»");
            sb.AppendLine((hasShift ? "[✓]  " : "[ ]  ") + "Sổ phân ca / quy chế bàn giao / thông báo trực tối 17/11–18/11");
            if (hasRaking)
                sb.AppendLine("[✓]  Vết hằn bút bi mặt sau (soi nghiêng ánh sáng) cũng nhắc đúng mốc 03g17");
            sb.AppendLine();
            if (done)
            {
                sb.Append("Đã khóa ghi chú ngoại lệ 03:17. Thời điểm mất liên lạc của ca trực trùng khít giờ dừng kim đồng hồ sảnh — không phải con số ghi vội.");
            }
            else if (canDo)
            {
                sb.Append("Cả ba nguồn đã khớp cùng một mốc 03:17 (thời điểm mất liên lạc tầng trên, chưa kết luận giờ mất). Bạn có thể ghép chúng thành ghi chú ngoại lệ trong sổ trực.");
            }
            else
            {
                sb.Append("Minh (tự vấn) — Mốc 03:17 xuất hiện rời rạc ở sảnh và trong phòng bệnh. Cần gom đủ đồng hồ sảnh, thẻ đầu giường AS-05B và một sổ trực/quy chế mới khớp được.");
            }

            var choices = new List<ActionChoice>();
            if (!done)
            {
                choices.Add(new ActionChoice("Đối chiếu mốc 03:17 vào ghi chú ngoại lệ", () =>
                    SaveCheckpoint(casework.CrossCheck0317ExceptionAsync, () =>
                    {
                        scene.atmosphere?.From("clockTick", new Vector3(1.2f, 2.0f, -5.8f), 0.65f);
                        ui.Toast("GHI CHÚ NGOẠI LỆ · MỐC 03:17",
                            "Đã nối mốc 03:17 giữa đồng hồ sảnh, thẻ AS-05B và sổ trực đêm 17–18/11/2005.", 6.0f);
                        Exception0317Page(back);
                    }), canDo));
            }
            choices.Add(new ActionChoice("Quay lại Sổ điều tra", back));
            ui.Show("GHI CHÚ NGOẠI LỆ  /  MỐC 03:17", "Đối chiếu thời điểm mất liên lạc", sb.ToString(),
                choices.ToArray(), back);
        }

        private List<ObservationSpec> CollectObservedDocuments()
        {
            var docs = new List<ObservationSpec>();
            var all = ObservationRegistry.All;
            for (int i = 0; i < all.Count; i++)
                if (all[i].IsDocument && State.Observed(all[i].Id))
                    docs.Add(all[i]);
            return docs;
        }

        private void ObservationsPage()
        {
            var sb = new StringBuilder();
            var choices = new List<ActionChoice>();
            var all = ObservationRegistry.All;
            int count = 0;
            for (int i = 0; i < all.Count; i++)
            {
                var spec = all[i];
                if (!State.Observed(spec.Id)) continue;
                count++;
                var rec = State.FindObservation(spec.Id);
                string st = rec != null ? ObservationRegistry.StageLabel(rec.stage) : "Đã kiểm tra";
                string loc = rec != null ? $"Lớp {rec.layer} · {rec.location}" : spec.ProvenanceLabel;
                sb.Append("• ").Append(spec.Title).Append("  [").Append(st).Append(" | ").Append(loc).Append("]\n  ")
                  .Append(spec.FactSummary);
                AppendObservationNotes(sb, spec, rec);
                sb.Append("\n\n");
                if (spec.IsDocument)
                {
                    var captured = spec;
                    choices.Add(new ActionChoice($"Soi lại: {captured.Title}", () => OpenObservationInspector(captured, ObservationsPage)));
                }
            }
            if (count == 0)
                sb.Append("Chưa ghi nhận quan sát hiện trường nào. Hãy kiểm tra tài liệu và dấu vết xung quanh.");

            choices.Add(new ActionChoice("Quay lại Sổ điều tra", Journal));
            ui.Show("NGUỒN CHỨNG CỨ & TRẠNG THÁI", $"Quan sát đã ghi nhận ({count})", sb.ToString().TrimEnd(),
                choices.ToArray(), Journal);
        }

        private void CompareDocumentsPage(string leftId, string rightId, Action back)
        {
            var docs = CollectObservedDocuments();
            if (docs.Count < 2)
            {
                ui.Show("SO SÁNH TÀI LIỆU", "Cần ít nhất 2 tài liệu",
                    "Hãy kiểm tra thêm ít nhất một phiếu hoặc thẻ bàn giao trong Phòng 101 hoặc Lối nhân viên để đặt hai tài liệu cạnh nhau.",
                    new[] { new ActionChoice("Quay lại", back) }, back);
                return;
            }
            int li = docs.FindIndex(d => d.Id == leftId);
            if (li < 0) li = 0;
            int ri = docs.FindIndex(d => d.Id == rightId);
            if (ri < 0 || ri == li) ri = (li + 1) % docs.Count;

            var left = docs[li];
            var right = docs[ri];

            string BuildNote(ObservationSpec s)
            {
                var rec = State.FindObservation(s.Id);
                string backInfo = rec != null && rec.flippedBack ? " · Đã xem mặt sau" : "";
                return $"{s.ProvenanceLabel}{backInfo}\n{s.FactSummary}";
            }

            var buttons = new[]
            {
                new ActionChoice($"Đổi tài liệu trái ({left.ShortCode} →)", () =>
                {
                    int next = (li + 1) % docs.Count;
                    if (next == ri) next = (next + 1) % docs.Count;
                    CompareDocumentsPage(docs[next].Id, right.Id, back);
                }),
                new ActionChoice($"Đổi tài liệu phải ({right.ShortCode} →)", () =>
                {
                    int next = (ri + 1) % docs.Count;
                    if (next == li) next = (next + 1) % docs.Count;
                    CompareDocumentsPage(left.Id, docs[next].Id, back);
                }),
                new ActionChoice($"Soi cận cảnh 3D: {left.ShortCode}", () => OpenObservationInspector(left, () => CompareDocumentsPage(left.Id, right.Id, back))),
                new ActionChoice($"Soi cận cảnh 3D: {right.ShortCode}", () => OpenObservationInspector(right, () => CompareDocumentsPage(left.Id, right.Id, back))),
                new ActionChoice("Quay lại", back)
            };

            ui.ShowComparison(
                "BÀN ĐỐI CHIẾU TÀI LIỆU  /  KHÔNG TỰ GỢI Ý ĐÁP ÁN",
                $"{left.ShortCode}   ↔   {right.ShortCode}",
                $"{left.Title}  ({ObservationRegistry.CategoryLabel(left.Category)})",
                Image(left.FrontTexture),
                BuildNote(left),
                $"{right.Title}  ({ObservationRegistry.CategoryLabel(right.Category)})",
                Image(right.FrontTexture),
                BuildNote(right),
                buttons,
                back);
        }

        private void F101HistoryPage()
        {
            var r = State.f101;
            var sb = new StringBuilder();
            sb.Append($"• Lần quyết định đầu tiên: {Status(r.firstDecision)}\n");
            sb.Append($"• Quyết định hiện tại: {Status(r.current)}  (Tổng số lần ký: {r.revision})\n");
            int failed = PuzzleService.FailedAttempts(State, HypothesisService.F101PuzzleId);
            if (failed > 0) sb.Append($"• Số lần lập luận chưa đạt căn cứ: {failed}\n");

            if (State.f101History == null || State.f101History.Count == 0)
            {
                sb.Append("\nChưa có bản ghi ký phiếu F101 nào.");
            }
            else
            {
                sb.Append("\nLỊCH SỬ CHỈNH SỬA PHIẾU:");
                for (int i = 0; i < State.f101History.Count; i++)
                {
                    var h = State.f101History[i];
                    sb.Append($"\n\nLần #{h.revision}  [{h.chapter} · Lớp {h.layer}]  →  {Status(h.decision)}");
                    if (!string.IsNullOrEmpty(h.claimId))
                    {
                        var claim = HypothesisService.GetClaim(h.claimId);
                        if (claim != null) sb.Append("\n  Kết luận: ").Append(claim.Label);
                    }
                    if (h.evidenceIds != null && h.evidenceIds.Count > 0)
                    {
                        sb.Append("\n  Chứng cứ: ");
                        for (int j = 0; j < h.evidenceIds.Count; j++)
                        {
                            var sp = ObservationRegistry.Get(h.evidenceIds[j]);
                            if (j > 0) sb.Append(", ");
                            sb.Append(sp != null ? sp.ShortCode : h.evidenceIds[j]);
                        }
                    }
                }
            }
            ui.Show("HỒ SƠ PHIẾU F101", "Lịch sử quyết định & lập luận", sb.ToString(),
                new[] { new ActionChoice("Quay lại Sổ điều tra", Journal) }, Journal);
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
            "C03     B/3         Bàn giao\n" +
            "…\n" +
            "C11     C/11       Bàn giao\n" +
            "····     C/12       Hộp đồ, thiếu thẻ tên";

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
            ui.Show("NGĂN KỆ C12  /  HỒ SƠ VÔ DANH", "Hộp đồ cá nhân không tên",
                "Ngăn kệ thứ 12 chỉ còn hộp carton buộc dây bện và một chiếc chìa khóa nhà đã mòn, hoàn toàn không có thẻ tên hay giấy tờ tùy thân.\n\n" +
                "Sổ E03, dòng C/12: hộp đã bàn giao, chưa xác định người nhận.",
                new[]
                {
                    new ActionChoice("Ghi nhận sự tồn tại C12 (Giữ nguyên khuyết danh)", () =>
                    {
                        SaveCheckpoint(() => casework.TakeCh03EvidenceAsync(EvidenceRegistry.C12UnnamedBox, "LOC07"), () =>
                        {
                            voice?.Speak("CH03-D04", "Minh", Line("CH03-D04"));
                            CloseModal();
                            Ch03CheckG03Unlocked();
                        });
                    }),
                    new ActionChoice("Đóng", CloseModal)
                }, CloseModal);
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
