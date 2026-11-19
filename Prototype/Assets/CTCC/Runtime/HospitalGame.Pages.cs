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
            heardLobbySilhouette = false;
            playedHe01Trolley = false;
            playedHe04CorridorCut = false;
            if (!Report(casework.Begin(player.Feet, startOptions))) return;
            RestoreJournal(State);
            live = true;
            player.Teleport(scene.Pad("AP00"));
            player.RestoreBattery(State.torchSeconds);
            ui.ShowChapterCard("CHƯƠNG 01  ·  17.11.2012", "NGƯỜI TRỞ VỀ", 4.4f);
            Letter();
        }

        private void Load(SaveSlot slot)
        {
            voice?.StopCurrent();
            SaveRead result = transaction.Restore(slot, out var safe);
            if (safe == null)
            {
                ui.Show("Bản lưu", "Không thể mở", result == SaveRead.Unsupported
                    ? "Bản lưu thuộc phiên bản khác." : "Không tìm thấy một mốc lưu có thể khôi phục.",
                    new[] { new ActionChoice("Quay lại", live ? Pause : MainMenu) }, live ? Pause : MainMenu);
                return;
            }
            lanDirector?.StopEncounter(false);
            gatekeeperCameo?.StopAndHide();
            anDirector?.HideImmediately();
            RestoreJournal(State);
            live = true;
            player.Teleport(safe);
            player.RestoreBattery(State.torchSeconds);
            CloseModal();
            if (result == SaveRead.Backup)
                ui.Caption("Đã mở bản lưu dự phòng.", 5, true);
        }

        private void Pause()
        {
            var manual = transaction.Inspect(SaveSlot.Manual);
            var automatic = transaction.Inspect(SaveSlot.Auto);
            ui.Show(State.chapter == "CH01" ? "NGƯỜI TRỞ VỀ" : "PHÒNG BỆNH TRỐNG", "Tạm dừng",
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
                "Mẩu báo cũ\nVụ cháy 18/11/2005 ở An Sinh có 13 người tử vong. Minh đã đến đám tang Lan.\n\n" +
                "Trong bì thư có chìa cổng phụ và chìa khu C đã thay khóa.";
            var envModel = scene.InspectTemplate("E10a");
            Action confirmRead = () =>
            {
                if (Report(casework.ReadLetter(player.Feet)))
                {
                    CloseModal();
                    voice?.Speak("CH01-D01", "Minh", Line("CH01-D01"));
                }
            };

            if (envModel != null && inspector != null && qaFolder == null)
            {
                scene.atmosphere?.From("paper", player.Feet + Vector3.up);
                inspector.Open(envModel, null, 1f, ActiveVolume());
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

            ui.ShowInspectSidebar("BÀN TRỰC LỄ TÂN  /  LỚP B · 2005", "Trần Ngọc Lan",
                "Lan  —  " + Line("CH01-D03") + "\n\nMinh  —  " + Line("CH01-D04") +
                "\n\nLan  —  " + Line("CH01-D05") + "\n\nLan  —  " + Line("CH01-D07") +
                "\n\nMinh vẫn nhớ lần anh đã từ chối mở cửa. Anh chưa biết chuyện gì xảy ra sau đó.",
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

        private void DiegeticObservation(string id, string title, string copy)
        {
            var outcome = casework.Observe(id, player.Feet);
            if (!Report(outcome)) return;
            scene.atmosphere?.From("paper", player.Feet + player.transform.forward * 0.5f + Vector3.up);
            ui.Toast("ĐÃ GHI QUAN SÁT  ·  " + title.ToUpperInvariant(), copy, 6.2f);
        }

        private void InspectDocumentObservation(string id, string title, string copy, string imageId)
        {
            var outcome = casework.Observe(id, player.Feet);
            if (!Report(outcome)) return;
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

        private void SaveEvidence(string id, Action committed)
        {
            if (!busy) StartCoroutine(WaitForEvidenceSave(id, committed));
        }

        private IEnumerator WaitForEvidenceSave(string id, Action committed)
        {
            busy = true;
            ui.ModalInputBlocked = true;
            RefreshControls();
            System.Threading.Tasks.Task<CommitResult> pending;
            try
            {
                pending = casework.TakeAsync(id);
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
                CloseModal();
                Report(CommitResult.SaveUnavailable);
            }
            else
            {
                var result = pending.GetAwaiter().GetResult();
                if (result != CommitResult.Applied) CloseModal();
                if (Report(result)) committed();
            }
        }

        private void Form()
        {
            if (!casework.CanRevise) { ui.Caption("Phiếu có thể đối chiếu lại tại điểm neo phòng 101.", 4, true); return; }
            var record = State.f101;
            string obs = FormService.RefutationAvailable(State) ? "Hai quan sát đã có trong sổ." : "Cần xem cả dấu sử dụng và thẻ bàn giao nếu muốn phản chứng.";
            string pad = transaction.OnPad(player.Feet, "AP04") ? "" : "\n\nĐứng trên vạch đồng cạnh cửa trước khi xác nhận.";
            ui.Show("SỔ BÀN GIAO  /  18.11.2005", "Phiếu phòng 101", "Số chưa bàn giao theo bảng là 0.\n\n" +
                "Hiện tại · " + Status(record.current) + "\nLần ghi · " + record.revision + "\n\n" + obs + pad,
                new[]
                {
                    new ActionChoice("A    Xác nhận theo bảng", () => PreviewForm(FormStatus.Affirmed, false)),
                    new ActionChoice("B    Chưa đủ thông tin", () => PreviewForm(FormStatus.Unverified, false)),
                    new ActionChoice("C    Đối chiếu dữ liệu bất nhất", () => MatchObservations(false, false), FormService.RefutationAvailable(State)),
                    new ActionChoice("Đóng phiếu", CloseModal)
                }, CloseModal, Image("F101"));
        }

        private void MatchObservations(bool a, bool b)
        {
            ui.Show("PHẢN CHỨNG  /  F101", "Chọn cặp quan sát",
                "Chọn hai quan sát liên hệ tới cùng chỗ kê giường. Các chi tiết cho biết còn mục chưa khép bàn giao.",
                new[]
                {
                    new ActionChoice((a ? "✓    " : "□    ") + "Dấu sử dụng ở vị trí thứ tư", () => MatchObservations(!a, b)),
                    new ActionChoice((b ? "✓    " : "□    ") + "Thẻ giường 4 ghi chưa bàn giao", () => MatchObservations(a, !b)),
                    new ActionChoice("Kiểm tra nội dung ký", () => PreviewForm(FormStatus.Refuted, true), a && b),
                    new ActionChoice("Quay lại phiếu", Form)
                }, Form);
        }

        private void PreviewForm(FormStatus choice, bool paired)
        {
            ui.Show("F101  /  N. H. MINH", "Kiểm tra trước khi ký",
                (choice == FormStatus.Affirmed ? "Xác nhận ca còn 0 bệnh nhân chưa bàn giao theo bản kê."
                    : choice == FormStatus.Unverified ? "Để ngỏ số bàn giao. Ghi rõ chưa đủ căn cứ."
                    : "Ghi nhận dữ liệu bất nhất. Giường 4 còn mục chưa bàn giao.") +
                "\n\nNgười ghi · N. H. Minh\nLựa chọn đầu tiên được lưu trong lịch sử sửa phiếu.",
                new[]
                {
                    new ActionChoice("Quay lại", Form),
                    new ActionChoice("Xác nhận lựa chọn", () =>
                    {
                        bool revised = State.f101.current != choice;
                        ui.Close();
                        TransitionAsync(() => casework.SubmitAsync(choice, paired, player.Feet), false, () =>
                        {
                            if (!revised) return;
                            scene.atmosphere?.From("stamp", player.Feet + Vector3.up);
                            if (State.layer == Reality.B)
                            {
                                string paId = choice == FormStatus.Affirmed ? "CH01-PA01" : choice == FormStatus.Unverified ? "CH01-PA02" : "CH01-PA03";
                                scene.atmosphere?.From("paStatic", scene.PaSpeakerAnchor != null ? scene.PaSpeakerAnchor.transform.position : new Vector3(5.5f, 2.6f, 3.35f), 0.6f);
                                voice?.Speak(paId, "Loa hành lang", Line(paId), scene.PaSpeakerAnchor,
                                    directionalPrefix: "[Loa hành lang · phía ngoài cửa phòng 101]");
                            }
                            else
                            {
                                ui.Caption("Đã lưu phiếu · " + Status(choice), 5, true);
                            }
                        });
                    })
                }, Form, Image("F101"));
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
                        CommitResult outcome;
                        bool smooth = scene.smoothPresentation;
                        scene.smoothPresentation = false;
                        try { outcome = casework.SolvePz01(player.Feet); }
                        finally { scene.smoothPresentation = smooth; }
                        if (Report(outcome))
                        {
                            if (scene.interactables != null && scene.cabinet != null)
                            {
                                scene.cabinet.localPosition = scene.cabinetClosed;
                                scene.interactables.MoveCabinet(scene.cabinetOpen, !smooth, scene.atmosphere);
                            }
                            ui.Toast("ĐÃ GHI NHẬN E12", "Tủ sắt đang lăn khỏi cửa kho giường. Bên trong còn khung giường tháo rời và tủ khô.", 5.5f);
                        }
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
                    if (Report(casework.VerifyAn(player.Feet)))
                    {
                        scene.atmosphere?.From("doorCreak", new Vector3(4f, 1.5f, 16f));
                        anDirector?.PlayPostVerifyVignette(Line("CH02-D03"), Options.reduceMotion);
                        ui.Toast("CỬA CẤP CỨU ĐÃ MỞ · G02", "Minh · " + Line("CH02-D04") + " (Có thể đi hành lang chính hoặc hành lang nhân viên.)", 6.5f);
                        voice?.RecordTranscriptOnly("CH02-D04", "Minh", Line("CH02-D04"));
                    }
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
            string status = "F101 · " + Status(r.current) + "\nLần ghi " + r.revision + " · Lần đầu " + Status(r.firstDecision) +
                "\n\nDấu sử dụng · " + (State.Observed("OBS101A") ? "Đã xem" : "Chưa xem") +
                "    Thẻ 4 · " + (State.Observed("OBS101B") ? "Đã xem" : "Chưa xem");
            if (State.chapter == "CH02")
                status += "\n\nHiện trường · " + (State.Has("E12") ? "Đã đối chiếu E12" : "Chưa đối chiếu chỗ kê") +
                    "\nTranh " + fragments + "/3 · Vòng " + (State.Has(EvidenceRegistry.Wristband) ? "đã ghi" : "chưa có") +
                    " · Phiếu chuyển " + (State.Has(EvidenceRegistry.Transfer) ? "đã ghi" : "chưa có") +
                    "\nHồ sơ " + (GateService.G02(State) ? "C01 đã xác thực" : "còn mở");
            var choices = new List<ActionChoice> { new("Đọc thư và tin nhắn", Letter) };
            if (State.Happened("CH01-T04")) choices.Add(new ActionChoice("Bản ghi ở bàn trực", LanTranscript));
            choices.Add(new ActionChoice("Bản ghi hội thoại (" + (voice != null ? voice.Transcript.Count : 0) + ")", TranscriptPage));
            if (casework.CanRevise) choices.Add(new ActionChoice("Xem phiếu 101", Form));
            if (State.chapter == "CH02") choices.Add(new ActionChoice("Xem hồ sơ trong kho", Identify));
            choices.Add(new ActionChoice("Khép sổ", CloseModal));
            ui.Show("NHẬT KÝ ĐIỀU TRA", State.chapter == "CH01" ? "Người trở về" : "Phòng bệnh trống", status,
                choices.ToArray(), CloseModal);
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
            ui.Show("KẾT THÚC VERTICAL SLICE M2  /  BẢN LƯU ĐÃ GHI", "Hồ sơ C01 · Phòng 101",
                "Đã xác thực Bé An từ bản lưu 101/4, vòng tay C01 và phiếu chuyển nội bộ ngày 17/11/2005. Vị trí tìm thấy An ở khu sau vẫn để ngỏ cho phần tiếp theo.\n\n" +
                $"• Quyết định F101 hiện tại: {Status(r.current)} (lần đầu: {Status(r.firstDecision)}, số lần sửa: {r.revision})\n" +
                $"• Chỉ số sai lệch báo cáo (Distortion): {FormService.Distortion(State)}\n" +
                $"• Vật chứng thực thể đã lưu: {State.evidence.Count} mục · Sự kiện Cấp cứu: {(State.Happened("CH02-T06") ? "Đã thoát khỏi Kẻ giữ cửa" : "Đã vào khu Cấp cứu")}\n\n" +
                "Phiếu 101, lịch sử sửa phiếu và toàn bộ vật chứng đã được giữ nguyên trong bản lưu.",
                new[] { new ActionChoice("Ở lại khám phá", CloseModal), new ActionChoice("Về màn hình đầu", MainMenu) }, CloseModal);
        }

        private void CloseModal()
        {
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
