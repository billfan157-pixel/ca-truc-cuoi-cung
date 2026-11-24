using System;
using System.Collections.Generic;
using UnityEngine;

namespace CTCC
{
    public sealed partial class HospitalGame
    {
        private void Ch04DutyRoom()
        {
            var choices = new List<ActionChoice>
            {
                new("Lấy bản lệnh giao ca · E02", () => ReadDutyItem(EvidenceRegistry.DutyOrders,
                    "Sổ bàn giao chìa khóa ca đêm 17–18/11/2005.\n22:00 · Minh nhận chìa C-01.\n23:10 · Khóa thường lệ sau khi Lan đã vào.\n02:50 · Mở kiểm kê vật tư theo lệnh giám đốc, rồi khóa lại. Người giữ chìa không đổi.\n04:15 · Trả chìa tại bàn chỉ huy ngoài sân."),
                    !State.Has(EvidenceRegistry.DutyOrders)),
                new("Chép sổ ca đêm · E09", () => ReadDutyItem(EvidenceRegistry.DutyLog,
                    "Sổ ca ghi lần mở cửa kiểm kê 02:50 và các lần nhận, mất liên lạc sau đó. Các mốc này chưa xác định giờ tử vong."),
                    !State.Has(EvidenceRegistry.DutyLog)),
                new("Đọc bản chép băng họp", Ch04Meeting),
                new("Xem lịch đi xe thuốc", () =>
                    ui.Show("BẢNG GIAO CA", "Tuyến xe thuốc", "Hành lang → quầy thuốc → cửa khu B. Khi xe dừng ở quầy, lối giữa thông. Lối cầu thang phía sảnh luôn quay lại được.",
                        new[] { new ActionChoice("Đặt lịch xuống", Ch04DutyRoom) }, Ch04DutyRoom))
            };
            if (CanCompareInventoryTime())
                choices.Add(new ActionChoice("Đối chiếu mốc 02:50", Ch04CompareInventoryTime));
            choices.Add(new("Rời phòng trực", CloseModal));
            ui.Show("PHÒNG TRỰC · TẦNG 2", "Lịch trực và tủ hồ sơ",
                State.Happened("PT01.presented") ? "Xuống cầu thang Cấp cứu, theo biển Kỹ thuật B1." : Line("CH04-D01"),
                choices.ToArray(), CloseModal);
        }

        private void ReadDutyItem(string id, string copy) =>
            ui.Show("PHÒNG TRỰC", id, copy,
                new[] { new ActionChoice("Lưu vào sổ trực", () =>
                    SaveCheckpoint(() => ChapterFour.Take(transaction, id, "LOC06"), Ch04DutyRoom)),
                    new ActionChoice("Đặt lại", Ch04DutyRoom) }, Ch04DutyRoom);

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
            ui.Show("BĂNG HỌP · BẢN LAN GIỮ", "Bản chép E06a",
                $"Vĩnh · {Line("CH04-D02")}\n\nLan · {Line("CH04-D03")}\n\nLan · {Line("CH04-D04")}",
                new[]
                {
                    new ActionChoice("Ghi nhận lời Lan", () => SaveCheckpoint(async () =>
                    {
                        var take = await ChapterFour.Take(transaction, EvidenceRegistry.MeetingTape, "LOC06");
                        return take == CommitResult.Applied ? await ChapterFour.PresentMeeting(transaction) : take;
                    }, () =>
                    {
                        foreach (string id in new[] { "CH04-D02", "CH04-D03", "CH04-D04" })
                            voice?.RecordTranscriptOnly(id, dialogue[id].speaker, Line(id));
                        Ch04DutyRoom();
                    }), State.Has(EvidenceRegistry.DutyOrders) && State.Has(EvidenceRegistry.DutyLog)),
                    new ActionChoice("Đặt băng xuống", Ch04DutyRoom)
                }, Ch04DutyRoom);
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
            ui.Show("B1 · KỸ THUẬT", "Phân bổ tải",
                $"Đồng hồ tải {ChapterFour.TotalLoad(mask)}/4\nTủ khô và khóa nhà xác dùng hai nhánh Kho và Kỹ thuật. Cầu thang vẫn dùng được khi mất điện.",
                choices.ToArray(), CloseModal);
        }

        private void SetServicePower(int mask) => SaveCheckpoint(() => ChapterFour.SetPower(transaction, mask), () =>
        {
            if (!ChapterFour.ValidPower(mask))
                ui.Caption("Quá tải. Rơ-le ngắt, toàn bộ nhánh đã về Tắt.", 4f, true);
            scene.atmosphere?.From("doorLatch", player.Feet);
            orderlyAi?.TriggerDistraction(player.Feet);
            Ch04PowerBoard();
        });

        private void Ch04Morgue()
        {
            bool e04 = State.Has(EvidenceRegistry.MorgueRegister);
            bool e07 = State.Has(EvidenceRegistry.LanRecovery);
            if (!ChapterFour.MorguePower(State.servicePower) && !GateService.H05(State))
            {
                ui.Caption("Tủ khô đóng. Kiểm tra hai nhánh tải tại phòng Kỹ thuật.", 5f, true);
                return;
            }
            var choices = new List<ActionChoice>();
            choices.Add(new("Đọc hồ sơ tiếp nhận · E04", Ch04ReadMorgueRegister));
            if (!e07) choices.Add(new("Đọc thẻ và phiếu thu hồi · E07", () =>
                ReadMorgueItem(EvidenceRegistry.LanRecovery,
                    "Thẻ của Lan đi kèm phiếu L-13. Ghi chú hiện trường xác nhận vị trí tìm thấy ở bên trong Khu C.")));
            if (!GateService.H05(State))
                choices.Add(new("Đối chiếu phiếu L-13", () => ui.Show("ĐỐI CHIẾU · H05", "Lan được tìm thấy trong khu C",
                    Line("CH04-D05"), new[] { new ActionChoice("Ghi nhận đối chiếu", () =>
                        SaveCheckpoint(() => ChapterFour.MatchRecovery(transaction), Ch04Morgue)) }, Ch04Morgue), e04 && e07));
            if (GateService.G04(State))
                choices.Add(new("Nghe Lan · Bên tủ hồ sơ", () => SaveCheckpoint(
                    () => casework.RecordAsync("CH04-T06", player.Feet), Ch04LanPage)));
            choices.Add(new("Rời nhà xác", CloseModal));
            ui.Show("B1 · NHÀ XÁC", "Tủ hồ sơ tiếp nhận",
                $"{(e04 ? "E04 · Đã chép hồ sơ tiếp nhận" : "E04 · Bản sao kèm danh mục của Bảy")}\n" +
                (e07 ? "E07 · Đã chép thẻ và phiếu thu hồi" : "E07 · Thẻ và phiếu vị trí trong ngăn hồ sơ"),
                choices.ToArray(), CloseModal);
        }

        private void Ch04ReadMorgueRegister()
        {
            ReadMorgueItem(EvidenceRegistry.MorgueRegister,
                "Biên bản tiếp nhận sơ bộ · 18/11/2005. Thu hồi 13 thi thể trong Khu C. " +
                "Các mục #01–11 đã được đối chiếu từng phần. Phiếu L-13 dẫn đến ghi nhận vị trí thu hồi. " +
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
                "Nam khoảng 48–55 tuổi, có sẹo mổ cũ. Phụ lục vật kèm ghi nhãn ngăn lạnh N-04. Chưa xác định danh tính.\n\nMinh · " + line,
                new[] { new ActionChoice("Quay lại biên bản", Ch04ReadMorgueRegister) }, Ch04ReadMorgueRegister);
        }

        private void ReadMorgueItem(string id, string copy, ActionChoice? detail = null)
        {
            var choices = new List<ActionChoice>();
            if (!State.Has(id))
                choices.Add(new ActionChoice("Chép tài liệu", () =>
                    SaveCheckpoint(() => ChapterFour.Take(transaction, id, "LOC10"), Ch04Morgue)));
            if (detail.HasValue) choices.Add(detail.Value);
            choices.Add(new ActionChoice("Đặt lại", Ch04Morgue));
            ui.Show("NHÀ XÁC", id, copy, choices.ToArray(), Ch04Morgue);
        }

        private void Ch04LanPage()
        {
            const string beat = "B06";
            if (!GateService.G04(State) || !State.Happened("CH04-T06")) return;
            voice?.RecordTranscriptOnly("CH04-D06", "Lan", LineAtBeat("CH04-D06", 1, beat), beat);
            ui.Show("NHÀ XÁC · SAU ĐỐI CHIẾU", "Lan", LineAtBeat("CH04-D06", 1, beat),
                new[]
                {
                    new ActionChoice("Hỏi Lan về những phút cuối", () =>
                    {
                        string line = LineAtBeat("CH01-D16b", 1, beat);
                        voice?.RecordTranscriptOnly("CH01-D16b", "Lan", line, beat);
                        ui.Show("NHÀ XÁC · SAU ĐỐI CHIẾU", "Lan", line,
                            new[] { new ActionChoice("Quay về cầu thang", CloseModal) }, CloseModal);
                    }),
                    new ActionChoice("Quay về cầu thang", CloseModal)
                }, CloseModal);
        }
    }
}
