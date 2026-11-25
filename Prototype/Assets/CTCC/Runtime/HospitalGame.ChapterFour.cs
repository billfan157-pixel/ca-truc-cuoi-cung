using System;
using System.Collections.Generic;
using UnityEngine;

namespace CTCC
{
    public sealed partial class HospitalGame
    {
        private void Ch04DutyRoom()
        {
            if (State.chapter == "CH04" && voice != null && !State.transcript.Exists(e => e.LineId == "CH04-D01"))
                voice.RecordTranscriptOnly("CH04-D01", "Lan", Line("CH04-D01"));

            bool hasE02 = State.Has(EvidenceRegistry.DutyOrders);
            bool hasE09 = State.Has(EvidenceRegistry.DutyLog);
            var choices = new List<ActionChoice>
            {
                new(hasE02 ? "Đọc lại sổ bàn giao chìa khóa · E02" : "Lấy bản lệnh giao ca · E02", () => ReadDutyItem(EvidenceRegistry.DutyOrders,
                    "SỔ CHÌA CA ĐÊM 17–18/11/2005\n" +
                    "• 22:00 ngày 17/11: người nhận BS.TT Nguyễn Hoàng Minh, chìa C-01.\n" +
                    "• 23:10: Minh ghi khóa thường lệ sau khi Lan đã vào.\n" +
                    "• 02:50: mở kiểm kê vật tư theo lệnh giám đốc. Người giữ chìa không đổi. Đã khóa lại.\n" +
                    "• 04:15: trả chìa tại bàn chỉ huy ngoài sân.\n" +
                    "Quy chế: Khu C khóa ngoài sau 23:00; người bên trong không giữ chìa riêng.")),
                new(hasE09 ? "Đọc lại sổ trực ca đêm · E09" : "Chép sổ ca đêm · E09", () => ReadDutyItem(EvidenceRegistry.DutyLog,
                    "SỔ TRỰC KHỐI K-A · ĐÊM 17–18/11/2005\n" +
                    "• 23:10: Lan trong C, Minh đã khóa cửa thường lệ.\n" +
                    "• 02:50: xe nhà tài trợ đến nhánh K-F, khai giao nhận vật tư LOC10. Đã làm việc theo lệnh tại cửa C. Xe rời 02:56.\n" +
                    "• 02:58: báo cháy LOC09, có mùi dung môi lạ, gọi cứu hỏa.\n" +
                    "• 03:05: lệnh giữ khóa của Vĩnh (E06).\n" +
                    "• 03:09: tín hiệu xin mở cửa C.\n" +
                    "• 03:12: Minh rời B1 hỗ trợ Khu B.\n" +
                    "• 03:17: mất liên lạc C (chưa xác định giờ tử vong).\n" +
                    "• 07:40: phiếu nội bộ ký lưu.")),
                new("Đọc bản chép băng họp · E06a", Ch04Meeting),
                new("Xem lịch đi xe thuốc (PZ04)", Ch04OrderlySchedule)
            };
            if (CanCompareInventoryTime())
                choices.Add(new ActionChoice("Đối chiếu mốc 02:50", Ch04CompareInventoryTime));
            choices.Add(new("Rời phòng trực", CloseModal));
            ui.Show("PHÒNG TRỰC · TẦNG 2 (LOC06)", "Lịch trực và tủ hồ sơ",
                State.Happened("PT01.presented") ? "Xuống cầu thang Cấp cứu, theo biển Kỹ thuật B1. (Quan sát tuyến xe thuốc hoặc đối chiếu phiếu buồng Khu B.)" : Line("CH04-D01"),
                choices.ToArray(), CloseModal);
        }

        private void ReadDutyItem(string id, string copy)
        {
            var choices = new List<ActionChoice>();
            if (!State.Has(id))
                choices.Add(new ActionChoice("Lưu vào sổ trực", () =>
                    SaveCheckpoint(() => ChapterFour.Take(transaction, id, "LOC06"), Ch04DutyRoom)));
            choices.Add(new ActionChoice(State.Has(id) ? "Quay lại phòng trực (đã lưu)" : "Đặt lại", Ch04DutyRoom));
            ui.Show("PHÒNG TRỰC", id, copy, choices.ToArray(), Ch04DutyRoom);
        }

        private void Ch04OrderlySchedule()
        {
            void OpenModal()
            {
                bool rerouted = State.Happened("PZ04.ScheduleUpdated");
                bool hasSlip = State.Happened("PZ04.SlipRead");
                string liveStop = orderlyAi != null && orderlyAi.ActiveRole == AIRole.Orderly
                    ? orderlyAi.CurrentOrderlyStopLabel
                    : (rerouted ? "Quầy thuốc Khu B" : "Hành lang phòng trực");
                string statusText = rerouted
                    ? $"TRẠNG THÁI TUYẾN B (ĐÃ CẬP NHẬT ĐÚNG PHIẾU): Quầy thuốc Khu B (7s) ↔ Cửa Khu B (5.5s). Hành lang phòng trực đã thông suốt.\nVị trí xe hiện tại: {liveStop}."
                    : $"TRẠNG THÁI TUYẾN A (MẶC ĐỊNH · 3 ĐIỂM DỪNG):\n" +
                      "1. Hành lang phòng trực (dừng 5s) — chắn cửa nam phòng trực.\n" +
                      "2. Quầy thuốc Khu B (dừng 6s) — hướng đông, mở trống hành lang.\n" +
                      "3. Cửa Khu B (dừng 4.5s) — hướng tây, mở trống lối cầu thang Cấp cứu.\n" +
                      $"Vị trí xe thuốc hiện tại: {liveStop}.\n\n" +
                      "• Tuyến A (Quan sát nhịp): Chờ xe thuốc chuyển sang Quầy thuốc hoặc Cửa Khu B rồi đi qua hành lang, không cần sửa bất kỳ giấy tờ nào.\n" +
                      "• Tuyến B (Đối chiếu phiếu buồng): Kiểm tra phiếu tình trạng buồng Khu B treo trên tường đông phòng trực để cập nhật đúng tuyến thực tế.";

                var choices = new List<ActionChoice>();
                if (!rerouted)
                    choices.Add(new ActionChoice(hasSlip ? "Đối chiếu phiếu buồng để cập nhật tuyến xe" : "Mở bảng đối chiếu tuyến xe", Ch04WardStatusBoard));
                choices.Add(new ActionChoice("Đặt lịch xuống", CloseModal));
                ui.Show("BẢNG LỊCH XE THUỐC · PZ04", "Lịch đẩy xe thuốc Khu B", statusText, choices.ToArray(), CloseModal);
            }

            if (State.Happened("PZ04.ScheduleRead")) OpenModal();
            else SaveCheckpoint(() => ChapterFour.ReadOrderlySchedule(transaction), OpenModal);
        }

        private void Ch04RoomStatusSlip()
        {
            void OpenModal()
            {
                string copy =
                    "PHIẾU TÌNH TRẠNG BUỒNG KHU B · CA ĐÊM\n" +
                    "• Buồng hành lang Bắc (sát cửa phòng trực): ĐÃ CHUYỂN TRỐNG TỪ 22:00.\n" +
                    "• Điểm phát thuốc thực tế còn hoạt động: Quầy thuốc Khu B (Đông) & Cửa Khu B (Tây).\n" +
                    "• Ghi chú điều dưỡng: Nếu cập nhật đúng bảng tình trạng buồng, xe thuốc bỏ điểm dừng hành lang phòng trực và chỉ đi hai đầu Quầy thuốc ↔ Cửa Khu B.\n\n" +
                    "(Không được sửa sai số liệu buồng đang có bệnh nhân.)";
                var choices = new List<ActionChoice>();
                if (!State.Happened("PZ04.ScheduleUpdated"))
                    choices.Add(new ActionChoice("Mở bảng đối chiếu tuyến xe", Ch04WardStatusBoard));
                choices.Add(new ActionChoice("Đóng phiếu", CloseModal));
                ui.Show("PHIẾU TÌNH TRẠNG BUỒNG · PZ04", "Dữ kiện buồng Khu B", copy, choices.ToArray(), CloseModal);
            }

            if (State.Happened("PZ04.SlipRead")) OpenModal();
            else SaveCheckpoint(() => ChapterFour.ReadRoomStatusSlip(transaction), OpenModal);
        }

        private void Ch04WardStatusBoard()
        {
            bool rerouted = State.Happened("PZ04.ScheduleUpdated");
            bool hasSlip = State.Happened("PZ04.SlipRead");
            string hint = !rerouted && PuzzleService.ShouldShowFieldHint(State, "CH04-PU01")
                ? "\n\nGợi ý: Đọc phiếu tình trạng buồng Khu B treo trên tường đông phòng trực (chỉ buồng hành lang Bắc trống từ 22:00), hoặc bỏ qua bảng để đi theo Tuyến A khi xe rời hành lang."
                : "";
            string copy = rerouted
                ? "Bảng tình trạng buồng đã được đối chiếu đúng dữ kiện.\nMũi tên chỉ tuyến phía trên cửa nam phòng trực đã chuyển xanh: xe thuốc giữ tuyến Quầy thuốc Khu B ↔ Cửa Khu B, bỏ chốt hành lang phòng trực."
                : "Đối chiếu bảng tuyến xe thuốc Khu B theo dữ kiện thực tế.\n" +
                  (hasSlip ? "Đã có phiếu tình trạng buồng: buồng hành lang Bắc trống từ 22:00." : "Chưa đọc phiếu tình trạng buồng trên tường đông phòng trực.") + hint;

            var choices = new List<ActionChoice>();
            if (!rerouted)
            {
                choices.Add(new ActionChoice("Đối chiếu đúng phiếu: buồng hành lang Bắc trống từ 22:00", () =>
                    SaveCheckpoint(() => ChapterFour.AttemptUpdateOrderlySchedule(transaction, true), () =>
                    {
                        if (State.Happened("PZ04.ScheduleUpdated"))
                        {
                            orderlyAi?.SyncState(State);
                            scene.atmosphere?.From("doorLatch", player.Feet + Vector3.up);
                            if (Options.captions)
                                ui.Caption("[Bảng tuyến xe thuốc đổi mũi tên xanh — xe đẩy chuyển sang giữ Quầy thuốc ↔ Cửa Khu B]", 5.5f, true);
                            ui.Toast("ĐÃ CẬP NHẬT TUYẾN XE THUỐC · TUYẾN B", "Xe thuốc bỏ chốt hành lang phòng trực và đi giữa Quầy thuốc ↔ Cửa Khu B.", 5.5f);
                            Ch04WardStatusBoard();
                        }
                        else
                        {
                            ui.Caption("Cần đọc phiếu tình trạng buồng trên tường đông phòng trực trước khi cập nhật bảng.", 4.5f, true);
                            Ch04WardStatusBoard();
                        }
                    })));
                choices.Add(new ActionChoice("Sửa sai hồ sơ: ghi toàn bộ Khu B đã xuất viện", () =>
                    SaveCheckpoint(() => ChapterFour.AttemptUpdateOrderlySchedule(transaction, false), () =>
                    {
                        ui.Caption("Không được sửa sai hồ sơ để mở đường — chỉ đối chiếu đúng dữ kiện thực hoặc chờ nhịp xe (Tuyến A).", 5f, true);
                        Ch04WardStatusBoard();
                    })));
            }
            choices.Add(new ActionChoice(rerouted ? "Rời bảng (lối hành lang đã thông)" : "Bỏ qua bảng (chọn Tuyến A theo nhịp xe)", CloseModal));
            ui.Show("BẢNG TÌNH TRẠNG BUỒNG · PZ04", "Đối chiếu tuyến xe thuốc", copy, choices.ToArray(), CloseModal);
        }

        private bool CanCompareInventoryTime() =>
            State.chapter == "CH04" && State.layer == Reality.R &&
            State.Has(EvidenceRegistry.DutyOrders) && State.Has(EvidenceRegistry.DutyLog);

        private void Ch04CompareInventoryTime()
        {
            if (!CanCompareInventoryTime()) return;
            string line = LineAtBeat("CH04-D09", 1, "B01");
            voice?.RecordTranscriptOnly("CH04-D09", "Minh", line, "B01");
            ui.Show("PHÒNG TRỰC · E02 VÀ E09", "Mốc mở kiểm kê 02:50", line,
                new[] { new ActionChoice("Quay lại sổ trực", Ch04DutyRoom) }, Ch04DutyRoom);
        }

        private void Ch04Meeting()
        {
            bool presented = State.Happened("PT01.presented");
            var choices = new List<ActionChoice>();
            if (!presented)
            {
                choices.Add(new ActionChoice("Ghi nhận lời Lan", () => SaveCheckpoint(async () =>
                {
                    var take = await ChapterFour.Take(transaction, EvidenceRegistry.MeetingTape, "LOC06");
                    return take == CommitResult.Applied ? await ChapterFour.PresentMeeting(transaction) : take;
                }, () =>
                {
                    foreach (string id in new[] { "CH04-D02", "CH04-D03", "CH04-D04" })
                        voice?.RecordTranscriptOnly(id, dialogue[id].speaker, Line(id));
                    Ch04DutyRoom();
                }), State.Has(EvidenceRegistry.DutyOrders) && State.Has(EvidenceRegistry.DutyLog)));
            }
            choices.Add(new ActionChoice(presented ? "Quay lại phòng trực (đã ghi PT01)" : "Đặt băng xuống", Ch04DutyRoom));
            ui.Show("BĂNG HỌP 08/2003 · BẢN LAN GIỮ (E06a)", "Bản chép cassette cá nhân",
                $"[Tiếng kéo ghế, quạt trần quay đều]\n\n" +
                $"• Vĩnh · {Line("CH04-D02")}\n\n" +
                $"• Lan · {Line("CH04-D03")}\n\n" +
                $"• Lan [im lặng 3 giây] · {Line("CH04-D04")}\n\n" +
                "Lưu ý hồ sơ: E06a là băng cassette cá nhân của Lan, độc lập và không thay thế văn bản lệnh giữ khóa E06.",
                choices.ToArray(), Ch04DutyRoom);
        }

        private void Ch04PowerBoard()
        {
            if (!State.Happened("PT01.presented"))
            {
                ui.Caption("Kiểm tra sổ giao ca và băng họp ở phòng trực trước.", 5f, true);
                return;
            }
            int mask = State.servicePower;
            var choices = new List<ActionChoice>();
            foreach (var (bit, label) in new[] { (ChapterFour.Hall, "Hành lang · 1"), (ChapterFour.Archive, "Kho · 2"),
                (ChapterFour.Lift, "Thang · 3"), (ChapterFour.Service, "Kỹ thuật · 1") })
                choices.Add(new($"{label} · {((mask & bit) == 0 ? "Tắt" : "Bật")}", () => SetServicePower(mask ^ bit)));
            choices.Add(new("Ngắt tất cả tải", () => SetServicePower(0)));
            choices.Add(new("Rời bảng điện", CloseModal));
            string liftNote = (mask & ChapterFour.Lift) != 0
                ? "\nNhánh Thang (3/4) đang bật; lối cầu thang bộ vẫn luôn dùng được kể cả khi ngắt thang."
                : "\nCầu thang bộ luôn khả dụng khi mất điện.";
            string hint = !ChapterFour.MorguePower(mask) && PuzzleService.ShouldShowFieldHint(State, "CH04-PU02")
                ? "\nGợi ý: Tắt nhánh Thang (3) để tổng tải không quá 4/4, rồi bật đồng thời Kho (2) + Kỹ thuật (1)."
                : "";
            ui.Show("B1 · KỸ THUẬT (PZ05)", "Phân bổ tải điện",
                $"Đồng hồ tải {ChapterFour.TotalLoad(mask)}/4\nTủ khô và khóa nhà xác dùng đồng thời hai nhánh Kho (2) và Kỹ thuật (1).{liftNote}{hint}",
                choices.ToArray(), CloseModal);
        }

        private void SetServicePower(int mask)
        {
            bool wasMorgue = ChapterFour.MorguePower(State.servicePower);
            SaveCheckpoint(() => ChapterFour.SetPower(transaction, mask), () =>
            {
                if (!ChapterFour.ValidPower(mask))
                {
                    ui.Caption("Quá tải (>4). Rơ-le ngắt, toàn bộ nhánh đã về Tắt.", 4.5f, true);
                    orderlyAi?.TriggerDistraction(player.Feet);
                }
                else if (ChapterFour.MorguePower(State.servicePower) && !wasMorgue)
                {
                    if (Options.captions)
                        ui.Caption("[Tiếng rơ-le đóng mạch phía bắc phòng kỹ thuật — khóa điện nhà xác LOC10 mở]", 4.5f, true);
                    ui.Toast("ĐÃ CẤP ĐIỆN NHÀ XÁC · PZ05", "Khóa điện nhà xác và tủ khô LOC10 đã mở (Kho + Kỹ thuật).", 5f);
                    orderlyAi?.TriggerDistraction(new Vector3(9.1f, -3.4f, 43.6f));
                }
                else if (Options.captions)
                    ui.Caption("[Tiếng rơ-le bảng điện Kỹ thuật B1 kêu tách]", 2.8f);
                scene.atmosphere?.From("doorLatch", player.Feet);
                Ch04PowerBoard();
            });
        }

        private void Ch04Morgue()
        {
            bool e04 = State.Has(EvidenceRegistry.MorgueRegister);
            bool e07 = State.Has(EvidenceRegistry.LanRecovery);
            if (!ChapterFour.MorguePower(State.servicePower) && !GateService.H05(State))
            {
                ui.Caption("Tủ khô đóng. Kiểm tra hai nhánh tải Kho + Kỹ thuật tại phòng Kỹ thuật.", 5f, true);
                return;
            }
            var choices = new List<ActionChoice>
            {
                new(e04 ? "Đọc lại hồ sơ tiếp nhận · E04" : "Đọc hồ sơ tiếp nhận · E04", Ch04ReadMorgueRegister),
                new(e07 ? "Đọc lại thẻ và phiếu thu hồi · E07" : "Đọc thẻ và phiếu thu hồi · E07", () =>
                    ReadMorgueItem(EvidenceRegistry.LanRecovery,
                        "GÓI DI VẬT NIÊM PHONG · #13 (E07)\n" +
                        "1. Thẻ tên Trần Ngọc Lan, điều dưỡng nội trú.\n" +
                        "2. Phiếu vị trí thu hồi L-13 (đối chiếu E04): thu hồi sát mặt trong ô kính cửa chính Khu C.\n" +
                        "3. Mẩu bìa cháy còn đọc được “Bảng theo dõi liều AS-05”. Dòng tay mất phần cuối; không xác định giờ tử vong từ vật này."))
            };
            if (!GateService.H05(State))
                choices.Add(new ActionChoice("Đối chiếu phiếu L-13", () => ui.Show("ĐỐI CHIẾU · H05 (PZ06)", "Lan được tìm thấy trong Khu C",
                    Line("CH04-D05"), new[] { new ActionChoice("Ghi nhận đối chiếu", () =>
                        SaveCheckpoint(() => ChapterFour.MatchRecovery(transaction), () =>
                        {
                            voice?.RecordTranscriptOnly("CH04-D05", "Minh", Line("CH04-D05"));
                            Ch04Morgue();
                        })) }, Ch04Morgue), e04 && e07));
            if (GateService.G04(State))
                choices.Add(new ActionChoice("Nghe Lan · Bên tủ hồ sơ", () => SaveCheckpoint(
                    () => casework.RecordAsync("CH04-T06", player.Feet), Ch04LanPage)));
            choices.Add(new ActionChoice("Rời nhà xác", CloseModal));
            string missingHint = !GateService.H05(State) && (e04 ^ e07)
                ? "\n\nCần đồng thời cả E04 (biên bản tiếp nhận 13 thi thể) và E07 (thẻ tên + phiếu vị trí L-13) mới đủ căn cứ xác thực H05."
                : "";
            ui.Show("B1 · NHÀ XÁC (LOC10)", "Tủ hồ sơ tiếp nhận",
                $"{(e04 ? "E04 · Đã chép hồ sơ tiếp nhận" : "E04 · Bản sao kèm danh mục của Bảy")}\n" +
                (e07 ? "E07 · Đã chép thẻ và phiếu thu hồi" : "E07 · Thẻ và phiếu vị trí L-13 trong ngăn hồ sơ") + missingHint,
                choices.ToArray(), CloseModal);
        }

        private void Ch04ReadMorgueRegister()
        {
            ReadMorgueItem(EvidenceRegistry.MorgueRegister,
                "HỒ SƠ TIẾP NHẬN THI THỂ SƠ BỘ · SỰ CỐ AN SINH 18/11/2005 (E04)\n" +
                "Tổng thu hồi trong Khu C: 13 thi thể. Các mục #01–11 đã được đối chiếu từng phần với vật dụng và thân nhân. " +
                "#13 thu hồi sát mặt trong ô kính cửa chính Khu C, dẫn tới phiếu vị trí L-13 (E07). " +
                "Biên bản chưa xác định giờ hoặc nguyên nhân tử vong từng người.",
                State.Has(EvidenceRegistry.PatientRegister)
                    ? new ActionChoice("Đọc mục thi thể #12", Ch04ReadMorgueEntry12)
                    : null);
        }

        private void Ch04ReadMorgueEntry12()
        {
            if (State.chapter != "CH04" || State.layer != Reality.R ||
                !State.Has(EvidenceRegistry.PatientRegister)) return;

            string line = LineAtBeat("CH04-D08", 1, "B05");
            voice?.RecordTranscriptOnly("CH04-D08", "Minh", line, "B05");
            ui.Show("NHÀ XÁC · E04", "Mục thi thể #12",
                "Nam khoảng 48–55 tuổi, thể trạng trung niên, có sẹo mổ cũ. Phụ lục vật kèm ghi nhãn ngăn lạnh N-04. Chưa xác định danh tính.\n\nMinh · " + line,
                new[] { new ActionChoice("Quay lại biên bản", Ch04ReadMorgueRegister) }, Ch04ReadMorgueRegister);
        }

        private void ReadMorgueItem(string id, string copy, ActionChoice? detail = null)
        {
            var choices = new List<ActionChoice>();
            if (!State.Has(id))
                choices.Add(new ActionChoice("Chép tài liệu", () =>
                    SaveCheckpoint(() => ChapterFour.Take(transaction, id, "LOC10"), Ch04Morgue)));
            if (detail.HasValue) choices.Add(detail.Value);
            choices.Add(new ActionChoice(State.Has(id) ? "Quay lại tủ (đã lưu)" : "Đặt lại", Ch04Morgue));
            ui.Show("NHÀ XÁC", id, copy, choices.ToArray(), Ch04Morgue);
        }

        private void Ch04LanPage()
        {
            const string beat = "B06";
            if (!GateService.G04(State) || !State.Happened("CH04-T06")) return;
            voice?.RecordTranscriptOnly("CH04-D06", "Lan", LineAtBeat("CH04-D06", 1, beat), beat);
            void FinishLanConversation()
            {
                string afterLine = Line("CH04-D07");
                if (!string.IsNullOrEmpty(afterLine))
                    voice?.RecordTranscriptOnly("CH04-D07", "Minh", afterLine);
                CloseModal();
                ui.Toast("G04 ĐÃ MỞ · ĐIỂM NEO AN05", "Quay lại tầng 2 tới điểm neo AN05 trong phòng trực Khu B.", 5.5f);
            }
            ui.Show("NHÀ XÁC · SAU ĐỐI CHIẾU", "Lan", LineAtBeat("CH04-D06", 1, beat),
                new[]
                {
                    new ActionChoice("Hỏi Lan về những phút cuối", () =>
                    {
                        string line = LineAtBeat("CH01-D16b", 1, beat);
                        voice?.RecordTranscriptOnly("CH01-D16b", "Lan", line, beat);
                        ui.Show("NHÀ XÁC · SAU ĐỐI CHIẾU", "Lan", line,
                            new[] { new ActionChoice("Quay về cầu thang", FinishLanConversation) }, FinishLanConversation);
                    }),
                    new ActionChoice("Quay về cầu thang", FinishLanConversation)
                }, FinishLanConversation);
        }

        private void Ch04An05Handoff()
        {
            if (!GateService.G04(State) || !State.Happened("CH04-T06"))
            {
                ui.Caption("Cần hoàn tất đối chiếu H05 (E04 + E07) và gặp Lan ở nhà xác B1 trước khi mở điểm neo AN05.", 5f, true);
                return;
            }
            SaveCheckpoint(() => ChapterFour.ReachAn05Handoff(transaction), () =>
            {
                string routeSummary = State.Happened("PZ04.RouteA") && State.Happened("PZ04.RouteB")
                    ? "Đã kiểm chứng cả Tuyến A (nhịp xe) và Tuyến B (đối chiếu phiếu)"
                    : State.Happened("PZ04.RouteB") ? "Tuyến B (đối chiếu phiếu buồng)"
                    : State.Happened("PZ04.RouteA") ? "Tuyến A (quan sát nhịp 3 điểm)" : "Đang mở";
                ui.Show("ĐIỂM NEO KHU B · AN05 (CH04 → CH05)", "Chuyển tiếp Chương 04 đã sẵn sàng",
                    "• Cổng G04: ĐÃ MỞ (H05 + PT01 + PT02 đã xác thực).\n" +
                    $"• Tuyến xe thuốc PZ04: {routeSummary}.\n" +
                    "• Sổ trực, băng họp E06a, biên bản E04, thẻ/phiếu E07 và nhật ký thoại đã lưu an toàn.\n\n" +
                    "Phạm vi M3.2 dừng tại chốt chuyển tiếp AN05. Chương 05–08, cổng G05/G06 và vật chứng E08 tiếp tục giữ khóa theo thiết kế.",
                    new[] { new ActionChoice("Tiếp tục ở tầng 2", CloseModal) }, CloseModal);
            });
        }
    }
}
