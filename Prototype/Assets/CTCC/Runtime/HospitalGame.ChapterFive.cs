using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CTCC
{
    public sealed partial class HospitalGame
    {
        private bool ch05Pair2CalledColleague;
        private int pz07MarkerS2Offset = -1;     // -1 = lệch Bắc, 0 = khớp trục cầu thang S2, +1 = lệch Nam
        private int pz07MarkerColumnOffset = 1;  // -1 = lệch Tây, 0 = khớp trục cột Đông X=34.5, +1 = lệch Đông
        private string pz07Feedback = "";

        private void Ch05An05DirectionNote()
        {
            if (!GateService.G04(State) || !State.Happened("CH04-T06"))
            {
                ui.Caption("Cần hoàn tất đối chiếu H05 (E04 + E07) và nói chuyện với Lan tại nhà xác B1 trước.", 4.8f, true);
                return;
            }
            SaveCheckpoint(() => State.chapter == "CH04"
                ? ChapterFour.ReachAn05Handoff(transaction)
                : System.Threading.Tasks.Task.FromResult(CommitResult.Applied), () =>
            {
                ui.Show("GHI CHÚ BÀN GIAO · PHÒNG TRỰC KHU B", "Điểm neo AN05 nằm tại xe bệnh nhân sảnh Khu B",
                    "Điểm neo ký ức AN05 của Chương 05 là chiếc xe bệnh nhân cũ đặt ngay sảnh Khu B (LOC05, tầng 2 — đi qua cửa nam phòng trực sang phía tây).\n\n" +
                    "• Cổng G04 đã mở (H05 + PT01 + PT02 đã xác thực).\n" +
                    "• Sổ trực E02, sổ ca đêm E09 và băng họp E06a vẫn lưu nguyên tại phòng trực và có thể đối chiếu lại trong Chương 05.",
                    new[] { new ActionChoice("Ra sảnh Khu B tới xe bệnh nhân AN05", CloseModal) }, CloseModal);
            });
        }

        private void Ch05An05Anchor()
        {
            if (!GateService.G04(State) || !State.Happened("CH04-T06"))
            {
                ui.Caption("Cần hoàn tất đối chiếu H05 (E04 + E07) và gặp Lan ở nhà xác B1 trước khi mở điểm neo AN05.", 5f, true);
                return;
            }

            if (State.chapter == "CH05" && State.Happened(ChapterFive.EvReturnR))
            {
                string nextHint = !State.Happened(ChapterFive.FlagOverlayAligned)
                    ? "Bước tiếp theo: xuống khu kỹ thuật B1 (LOC09) mở tủ bản vẽ, đối chiếu sơ đồ niêm yết 2005 với bản hoàn công tháng 06/2004 (E05 / PZ07)."
                    : !State.Has(EvidenceRegistry.SealedWallPhoto)
                        ? "Hai bản vẽ mâu thuẫn tại cửa thoát hiểm phía Đông (34.5, 42). Hãy theo hành lang kỹ thuật ngoài LOC11 kiểm chứng trực tiếp bức tường gạch năm 2004 (E05b)."
                        : !GateService.G05(State)
                            ? "Đã đủ cả bản vẽ E05 và hiện trường E05b — mở tủ bản vẽ LOC09 hoặc bức tường LOC11 để chốt H03 mở khóa LOC08 (G05)."
                            : "Cửa Phòng Hành chính (LOC08 · G05) trên tầng 3 đã mở. Tiến tới điểm bàn giao CH05-CP03.";
                ui.Show("XE BỆNH NHÂN KHU B · AN05 (LỚP R)", "Ký ức sơ tán đã ghi nhận (6/6 bệnh nhân)",
                    "Minh đã trở về hiện trường năm 2012 tại xe bệnh nhân AN05 (mốc CH05-CP02).\n" +
                    "• Kết quả lịch sử bất biến: 6/6 bệnh nhân Khu B được đưa ra sân qua cầu thang S1.\n" +
                    "• Không có đạo cụ hay vật chứng nào được mang từ Lớp M sang Lớp R.\n\n" + nextHint,
                    new[] { new ActionChoice("Tiếp tục điều tra hiện trường R", CloseModal) }, CloseModal);
                return;
            }

            SaveCheckpoint(() => ChapterFive.CommitEntryCheckpointAsync(transaction, player.Feet), () =>
            {
                int rescued = ChapterFive.RescuedCount(State);
                int nextPair = ChapterFive.ActivePairIndex(State);
                string resumeNote = rescued > 0
                    ? $"\n• Tiến độ đang giữ: đã đưa {rescued}/6 bệnh nhân qua lối sơ tán (tiếp tục từ Nhóm {Mathf.Min(nextPair, 3)})."
                    : "\n• Đã chốt mốc an toàn CH05-CP01 tại hiện trường R trước khi bước vào ký ức.";
                ui.Show("XE BỆNH NHÂN KHU B · AN05 (CHƯƠNG 05)", "Chạm vào xe bệnh nhân đêm 18/11/2005",
                    "Chiếc xe đẩy bệnh nhân dừng chéo giữa sảnh Khu B. Tay nắm lạnh ẩm, bánh cao su vẫn còn vệt tro mờ từ đêm cháy.\n" +
                    resumeNote + "\n" +
                    "• Lớp M chỉ tái hiện ký ức sơ tán 3 nhóm (6 bệnh nhân) qua cầu thang S1 — không thay đổi lịch sử, không timer tử vong, không xuất vật chứng ra lớp R.",
                    new[]
                    {
                        new ActionChoice(rescued > 0 ? $"Tiếp tục ký ức sơ tán (Nhóm {Mathf.Min(nextPair, 3)} · Lớp M)" : "Bước vào ký ức sơ tán Khu B (Lớp M)", Ch05EnterMemoryFromAn05),
                        new ActionChoice("Ở lại hiện trường R", CloseModal)
                    }, CloseModal);
            });
        }

        private void Ch05EnterMemoryFromAn05()
        {
            CloseModal();
            ch05Pair2CalledColleague = false;
            bool firstTime = !State.Happened(ChapterFive.EvEntry);
            TransitionAsync(() => ChapterFive.EnterEvacuationMemoryAsync(transaction, player.Feet), warp: true, after: () =>
            {
                orderlyAi?.SyncState(State);
                if (firstTime)
                    ui.ShowChapterCard("CHƯƠNG 05  ·  LỚP M (KÝ ỨC)", "ĐÊM SƠ TÁN · 18/11/2005", 4.6f);
                scene.atmosphere?.From("paStatic", player.Feet + Vector3.up * 1.4f, 0.55f);
                ui.Caption("[Tiếng mưa tắt hẳn — chuông báo cháy vang đều dọc hành lang Khu B · Ánh sáng chuyển vàng ấm · LỚP M]", 4.8f, true);
                if (!State.Happened(ChapterFive.EvPair1))
                    voice?.Speak("CH05-D01", "Đồng nghiệp", Line("CH05-D01"),
                        directionalPrefix: "[Chuông báo cháy · Lớp M]");
            });
        }

        private void Ch05MemoryControlModal()
        {
            if (State.chapter != "CH05" || State.layer != Reality.M)
            {
                Ch05An05Anchor();
                return;
            }
            int rescued = ChapterFive.RescuedCount(State);
            int pair = ChapterFive.ActivePairIndex(State);
            string status = pair switch
            {
                1 => "Nhóm 1/3 (0/6 bệnh nhân): tiến lại cửa hành lang Tây Khu B để mở lối, hướng dẫn bệnh nhân và phối hợp đồng nghiệp đẩy xe.",
                2 => "Nhóm 2/3 (2/6 bệnh nhân): đi dọc hành lang Tây sang S1, gọi đồng nghiệp tại điểm hẹn A để tránh vật cản và đẩy xe qua cửa.",
                3 => "Nhóm 3/3 (4/6 bệnh nhân): đưa hai bệnh nhân cuối xuống cầu thang S1 ra cửa xả sân ngoài (đang mở trong ký ức M) và chốt bảng 6/6.",
                _ => "Đã đưa đủ 6/6 bệnh nhân Khu B tới điểm tập kết sân ngoài S1."
            };
            var choices = new List<ActionChoice>();
            if (pair >= 4)
                choices.Add(new ActionChoice("Gặp Giám đốc Vĩnh & trở về hiện trường R (B05)", Ch05ShowVinhPraiseAndReturn));
            else
            {
                choices.Add(new ActionChoice("Tiếp tục dẫn sơ tán trong hành lang M", CloseModal));
                choices.Add(new ActionChoice("Tái hiện cố định phần còn lại (Skip · luôn cứu đủ 6/6)", Ch05SkipEvacuation));
            }
            choices.Add(new ActionChoice("Thoát tạm về AN05 ở lớp R (giữ tiến độ từng nhóm)", Ch05ExitMemoryEarly));
            ui.Show("KÝ ỨC SƠ TÁN KHU B · LỚP M (AN05)", $"Đã đưa ra ngoài: {rescued}/6 bệnh nhân",
                status + "\n\n(Không có đồng hồ tử vong hay lựa chọn bỏ mặc bệnh nhân. Tái hiện cố định giữ nguyên kết quả 6/6 và toàn bộ lời thoại canon.)",
                choices.ToArray(), CloseModal);
        }

        private void Ch05MemoryPair1()
        {
            if (State.chapter != "CH05" || State.layer != Reality.M) return;
            if (State.Happened(ChapterFive.EvPair1))
            {
                ui.Caption("Nhóm 1 (2/6 bệnh nhân) đã qua lối Khu B. Đi tiếp dọc hành lang Tây S1 tới điểm Nhóm 2.", 4.2f, true);
                return;
            }

            void ExecutePair1()
            {
                CloseModal();
                SaveCheckpoint(() => ChapterFive.CompleteEvacPairAsync(transaction, 1), () =>
                {
                    scene.atmosphere?.From("doorCreak", player.Feet + Vector3.up, 0.65f);
                    scene.atmosphere?.From("trolleyRoll", player.Feet + Vector3.up * 0.5f, 0.72f);
                    scene.atmosphere?.From("paStatic", player.Feet + Vector3.up * 2.2f, 0.58f);
                    // Record canonical Pair 1 lines + Vĩnh's PA warning (explicitly framed as a broadcast in M, NOT objective proof of a bio-leak!)
                    voice?.RecordTranscriptOnly("CH05-D01", "Đồng nghiệp", Line("CH05-D01"));
                    voice?.RecordTranscriptOnly("CH05-D02", "Bệnh nhân", Line("CH05-D02"));
                    voice?.RecordTranscriptOnly("CH05-D03", "Minh", Line("CH05-D03"));
                    voice?.Speak("CH05-D07", "Loa hành lang · Giọng Vĩnh (M)", Line("CH05-D07"),
                        directionalPrefix: "[Loa hành lang Khu B · Cảnh báo trong ký ức M]");
                    ui.Toast("NHÓM 1 HOÀN TẤT · 2/6 BỆNH NHÂN",
                        "Minh đã mở lối và phối hợp đồng nghiệp đưa 2 bệnh nhân đầu sang hành lang S1. Tiếp tục tới điểm Nhóm 2 phía nam hành lang Tây.", 6.2f);
                });
            }

            void ResetPair1Segment()
            {
                scene.atmosphere?.From("doorLatch", player.Feet + Vector3.up, 0.65f);
                ui.Caption("[Cánh cửa Khu B chưa mở chốt — xe bệnh nhân khựng lại. Đặt lại đúng phân đoạn Nhóm 1, không mất tiến độ.]", 4.5f, true, aboveModal: true);
                Ch05MemoryPair1();
            }

            ui.Show("KÝ ỨC SƠ TÁN · NHÓM 1/3 (B02 · LỚP M)", "Mở lối Khu B & hướng dẫn hai bệnh nhân đầu",
                $"Đồng nghiệp: “{Line("CH05-D01")}”\n" +
                $"Bệnh nhân: “{Line("CH05-D02")}”\n" +
                $"Minh: “{Line("CH05-D03")}”\n\n" +
                "• Thao tác nhấn đơn [E]/[K], không cần kéo giữ chuột.\n" +
                "• Loa hành lang phía trên sắp phát thông báo của Giám đốc Vĩnh (chỉ là lời cảnh báo trong ký ức M, không phải bằng chứng khách quan xác nhận có dịch).",
                new[]
                {
                    new ActionChoice("Mở chốt cửa Khu B, đỡ bệnh nhân & phối hợp đẩy xe (2/6)", ExecutePair1),
                    new ActionChoice("Thử đẩy xe khi chưa mở chốt cửa (Kiểm thử đặt lại phân đoạn)", ResetPair1Segment),
                    new ActionChoice("Tái hiện cố định phần còn lại (Skip · 6/6)", Ch05SkipEvacuation),
                    new ActionChoice("Thoát tạm về AN05 ở lớp R", Ch05ExitMemoryEarly),
                    new ActionChoice("Đóng", CloseModal)
                }, CloseModal);
        }

        private void Ch05MemoryPair2()
        {
            if (State.chapter != "CH05" || State.layer != Reality.M) return;
            if (!State.Happened(ChapterFive.EvPair1))
            {
                ui.Caption("Hãy mở lối và đưa Nhóm 1 (2 bệnh nhân đầu) ra khỏi cửa Khu B trước.", 4.2f, true);
                return;
            }
            if (State.Happened(ChapterFive.EvPair2))
            {
                ui.Caption("Nhóm 2 (4/6 bệnh nhân) đã qua vật cản an toàn. Đi xuống cầu thang S1 tới cửa xả sân ngoài cho Nhóm 3.", 4.4f, true);
                return;
            }

            void CallColleaguePointA()
            {
                ch05Pair2CalledColleague = true;
                scene.atmosphere?.From("doorCreak", player.Feet + Vector3.up, 0.58f);
                ui.Caption("[Điểm hẹn A: Đồng nghiệp chạy tới giữ cánh cửa thông hành lang S1 — lối tránh xe vải đã rộng mở.]", 4.2f, true, aboveModal: true);
                Ch05MemoryPair2();
            }

            void PushWheelchairPair2()
            {
                if (!ch05Pair2CalledColleague)
                {
                    scene.atmosphere?.From("chairScrape", player.Feet + Vector3.up * 0.6f, 0.72f);
                    ui.Caption("[Xe đẩy vướng xe vải chắn hành lang vì chưa có người giữ cửa — đặt lại đúng phân đoạn Nhóm 2. Hãy gọi đồng nghiệp tại điểm hẹn A trước.]", 4.8f, true, aboveModal: true);
                    Ch05MemoryPair2();
                    return;
                }
                CloseModal();
                SaveCheckpoint(() => ChapterFive.CompleteEvacPairAsync(transaction, 2), () =>
                {
                    ch05Pair2CalledColleague = false;
                    scene.atmosphere?.From("trolleyRoll", player.Feet + Vector3.up * 0.5f, 0.74f);
                    ui.Caption("[Bệnh nhân Nhóm 2: “Cảm ơn chú Minh... người nhà tôi đang đứng chờ ngoài sân.”]", 4.6f, true);
                    ui.Toast("NHÓM 2 HOÀN TẤT · 4/6 BỆNH NHÂN",
                        "Đã đưa thêm 2 bệnh nhân qua vật cản hành lang S1 an toàn. Tiếp tục đi hết hành lang Tây và xuống cầu thang S1 ra cửa xả sân ngoài cho Nhóm 3.", 6.4f);
                });
            }

            string obstacleState = ch05Pair2CalledColleague
                ? "✓ Đã gọi đồng nghiệp tại điểm hẹn A giữ cửa chắn khói. Đường đẩy xe lăn qua vật cản đã thông."
                : "• Phía trước có xe vải chắn nửa hành lang và dải khói dẫn hướng (an toàn, không nhấp nháy). Cần gọi đồng nghiệp tại điểm hẹn A giữ cửa trước khi đẩy xe qua.";

            ui.Show("KÝ ỨC SƠ TÁN · NHÓM 2/3 (B03 · LỚP M)", "Vượt vật cản hành lang S1 & phối hợp điểm hẹn A",
                obstacleState + "\n\n" +
                "• Thao tác nhấn đơn [E]/[K] — không cần kéo giữ, không timer tử vong.\n" +
                "• Nếu thao tác vướng vật cản, ca trực chỉ đặt lại đúng phân đoạn Nhóm 2 và giữ nguyên Nhóm 1 (2/6).",
                new[]
                {
                    new ActionChoice(ch05Pair2CalledColleague ? "✓ Đồng nghiệp đang giữ cửa tại điểm hẹn A" : "1. Gọi đồng nghiệp tại điểm hẹn A giữ cửa", CallColleaguePointA, !ch05Pair2CalledColleague),
                    new ActionChoice("2. Dẫn Nhóm 2 qua vật cản & đẩy xe lăn qua cửa (4/6)", PushWheelchairPair2),
                    new ActionChoice("Tái hiện cố định phần còn lại (Skip · 6/6)", Ch05SkipEvacuation),
                    new ActionChoice("Thoát tạm về AN05 ở lớp R", Ch05ExitMemoryEarly),
                    new ActionChoice("Đóng", CloseModal)
                }, CloseModal);
        }

        private void Ch05MemoryPair3Assembly()
        {
            if (State.chapter != "CH05" || State.layer != Reality.M) return;
            if (!State.Happened(ChapterFive.EvPair2))
            {
                ui.Caption("Cần đưa Nhóm 1 và Nhóm 2 dọc hành lang Khu B — S1 trước khi chốt danh sách 6/6 ở sân ngoài.", 4.5f, true);
                return;
            }
            if (State.Happened(ChapterFive.EvPair3))
            {
                Ch05ShowVinhPraiseAndReturn();
                return;
            }

            void CompletePair3()
            {
                CloseModal();
                SaveCheckpoint(() => ChapterFive.CompleteEvacPairAsync(transaction, 3), () =>
                {
                    scene.atmosphere?.From("trolleyRoll", player.Feet + Vector3.up * 0.5f, 0.65f);
                    scene.atmosphere?.From("keyJingle", player.Feet + Vector3.up * 0.9f, 0.36f);
                    Ch05ShowVinhPraiseAndReturn();
                });
            }

            ui.Show("ĐIỂM TẬP KẾT SÂN NGOÀI S1 · NHÓM 3/3 (B04 · LỚP M)", "Đưa hai bệnh nhân cuối ra sân & kiểm đếm 6/6",
                "Cửa xả sân ngoài của cầu thang đầu hồi S1 đang mở thông thẳng ra sân trước — hoàn toàn độc lập với tầng bán hầm B1.\n\n" +
                "• Khi xoay tay nắm mở lối cho hai bệnh nhân cuối, trên tay Minh vẫn nắm chùm chìa khóa Khu C (FS14).\n" +
                "• Bảng điểm danh Khu B chờ xác nhận đủ 6/6 bệnh nhân tại điểm tập kết.",
                new[]
                {
                    new ActionChoice("Đưa Nhóm 3 ra điểm tập kết & xác nhận bảng 6/6", CompletePair3),
                    new ActionChoice("Thoát tạm về AN05 ở lớp R", Ch05ExitMemoryEarly),
                    new ActionChoice("Đóng", CloseModal)
                }, CloseModal);
        }

        private void Ch05SkipEvacuation()
        {
            if (State.chapter != "CH05" || State.layer != Reality.M) return;
            CloseModal();
            SaveCheckpoint(() => ChapterFive.SkipEvacuationMemoryAsync(transaction), () =>
            {
                voice?.RecordTranscriptOnly("CH05-D01", "Đồng nghiệp", Line("CH05-D01"));
                voice?.RecordTranscriptOnly("CH05-D02", "Bệnh nhân", Line("CH05-D02"));
                voice?.RecordTranscriptOnly("CH05-D03", "Minh", Line("CH05-D03"));
                voice?.RecordTranscriptOnly("CH05-D07", "Loa hành lang · Giọng Vĩnh (M)", Line("CH05-D07"));
                Ch05ShowVinhPraiseAndReturn();
            });
        }

        private void Ch05ShowVinhPraiseAndReturn()
        {
            if (State.chapter != "CH05" || State.layer != Reality.M || !State.Happened(ChapterFive.EvPair3)) return;
            string vinhLine = LineAtBeat("CH05-D04", 1, "B05");
            voice?.Speak("CH05-D04", "Giám đốc Vĩnh", vinhLine, directionalPrefix: "[Điểm tập kết sân ngoài · Lớp M]", beat: "B05");

            string skipBanner = State.Happened(ChapterFive.FlagEvacSkipped)
                ? "TÁI HIỆN CỐ ĐỊNH · KẾT QUẢ BẤT BIẾN (6/6)\n"
                : "ĐIỂM TẬP KẾT SÂN NGOÀI S1 · KẾT QUẢ BẤT BIẾN (6/6)\n";

            ui.Show("KÝ ỨC SƠ TÁN KHU B · B05 (LỚP M)", "6/6 bệnh nhân Khu B đã ra tới sân",
                skipBanner +
                "• Toàn bộ 6 bệnh nhân thuộc Khu B đã qua cầu thang S1 an toàn; tuyến sơ tán độc lập hoàn toàn với B1.\n" +
                $"• Loa hành lang (ký ức M): lời cảnh báo rò rỉ sinh học của Giám đốc Vĩnh đã được ghi vào nhật ký thoại — đó chỉ là phát ngôn trong M, chưa phải chứng cứ khách quan.\n" +
                $"• Giám đốc Vĩnh đặt tay lên vai Minh: “{vinhLine}”\n\n" +
                "Minh cúi nhìn chùm chìa khóa Khu C trong lòng bàn tay (FS14). Đưa đủ sáu người ở Khu B ra ngoài là việc anh đã làm thật — nhưng điều đó không thay thế hay xóa đi câu hỏi còn bỏ ngỏ phía sau cánh cửa Khu C.\n\n" +
                "(Ký ức dừng lại tại đây trước các mốc sau của đêm 18/11 và không xuất bất kỳ vật chứng nào từ Lớp M sang Lớp R.)",
                new[]
                {
                    new ActionChoice("Trở về xe bệnh nhân AN05 tại hiện trường 2012 (Lớp R · CP02)", Ch05FinishReturnToReal)
                }, Ch05FinishReturnToReal);
        }

        private void Ch05FinishReturnToReal()
        {
            voice?.StopCurrent();
            CloseModal();
            TransitionAsync(() => ChapterFive.FinishEvacuationReturnAsync(transaction, player.Feet), warp: true, after: () =>
            {
                orderlyAi?.SyncState(State);
                ui.Toast("TRỞ VỀ HIỆN TRƯỜNG 2012 · CH05-CP02",
                    "Tiếng chuông báo cháy tắt, mưa đêm trở lại. Cửa xả S1 khôi phục tấm lưới sắt cũ. Hãy xuống khu kỹ thuật B1 (LOC09) tìm tủ bản vẽ hoàn công.", 6.8f);
            });
        }

        private void Ch05ExitMemoryEarly()
        {
            voice?.StopCurrent();
            CloseModal();
            TransitionAsync(() => ChapterFive.ExitEvacuationEarlyToRealAsync(transaction, player.Feet), warp: true, after: () =>
            {
                orderlyAi?.SyncState(State);
                ui.Caption($"Đã tạm thoát về AN05 ở lớp R ({ChapterFive.RescuedCount(State)}/6 đã lưu). Chạm lại xe bệnh nhân để tiếp tục.", 4.8f, true);
            });
        }

        // ==================== PHASE D: TASK013-B — PZ07 ARCHITECTURAL INVESTIGATION ====================

        private void Ch05BlueprintsLoc09()
        {
            if (State.chapter != "CH05" || State.layer != Reality.R)
            {
                ui.Caption("Tủ bản vẽ kỹ thuật B1 thuộc mạch điều tra kiến trúc của Chương 05 (hiện trường R).", 4.2f, true);
                return;
            }
            if (!State.Happened(ChapterFive.EvReturnR))
            {
                ui.Caption("Cần hoàn tất ký ức sơ tán tại xe bệnh nhân AN05 (Khu B, tầng 2) trước khi đối chiếu bản vẽ B1.", 4.6f, true);
                return;
            }

            bool firstAcquire = !State.Has(EvidenceRegistry.AsBuilt2004);
            SaveCheckpoint(() => ChapterFive.AcquireBlueprintsAtLoc09Async(transaction), () =>
            {
                if (firstAcquire)
                    voice?.Speak("CH05-D05", "Minh", Line("CH05-D05"));
                ShowCh05BlueprintCabinetModal();
            });
        }

        private void ShowCh05BlueprintCabinetModal()
        {
            bool aligned = State.Happened(ChapterFive.FlagOverlayAligned);
            bool hasE05b = State.Has(EvidenceRegistry.SealedWallPhoto);
            bool g05 = GateService.G05(State);

            var body = new StringBuilder();
            body.AppendLine("Trên bàn kỹ thuật LOC09 có hai bản vẽ khác niên đại của cùng bán hầm B1:");
            body.AppendLine("1. Sơ đồ thoát hiểm niêm yết bảng trực năm 2005 (nguồn đối nghịch): vẫn vẽ cửa thoát hiểm phía Đông Khu C tại mốc (34.5, 42) mở ra sân trũng.");
            body.AppendLine("2. Bản vẽ hoàn công cải tạo B1 tháng 06/2004 — E05 (ký duyệt Giám đốc Phạm Đức Vĩnh): xây gạch đặc 220 mm, trát vữa hai mặt bít vĩnh viễn cửa Đông, tháo khung và tay nắm theo yêu cầu cô lập của đơn vị tài trợ Chương trình B1.");
            body.AppendLine();
            body.AppendLine($"• Trạng thái căn chồng PZ07: {(aligned ? "✓ ĐÃ CĂN KHỚP HAI MỐC KIẾN TRÚC" : "Chưa căn khớp (cần chồng theo mốc cầu thang S2 & trục cột Đông)")}");
            body.AppendLine($"• Quan sát hiện trường thực địa E05b (LOC11): {(hasE05b ? "✓ ĐÃ XÁC NHẬN TƯỜNG XÂY BÍT NĂM 2004" : "Chưa kiểm chứng tại hiện trường R")}");
            if (aligned && !hasE05b)
                body.AppendLine("\nLƯU Ý (CH05-Q03): Chỉ căn chồng hai bản vẽ trên giấy CHƯA ĐỦ xác minh H03 hay mở cửa G05. Hãy qua cửa Đông LOC09 → tiền sảnh LOC11 → cửa Bắc ra hành lang kỹ thuật ngoài để soi trực tiếp tường gạch E05b.");
            else if (g05)
                body.AppendLine("\n✓ H03 đã xác minh đủ hai nguồn (E05 + E05b). Cửa Phòng Hành chính LOC08 (G05) trên tầng 3 đã mở.");

            var choices = new List<ActionChoice>
            {
                new("Xem Sơ đồ niêm yết 2005 (3D / Chữ lớn)", () =>
                    InspectCh05Document("Doc_Ch05_PostedMap2005", "SƠ ĐỒ NIÊM YẾT 2005 (ĐỐI NGHỊCH)",
                        "Bản sơ đồ lưu hành năm 2005 vẫn vẽ cửa thoát hiểm phía Đông Khu C tại (34.5, 42) dẫn ra sân trũng -1,90 m.\nHai mốc chuẩn: Mốc 1 (Trục cầu thang S2) và Mốc 2 (Trục cột Đông X=34,5).",
                        ShowCh05BlueprintCabinetModal)),
                new("Xem Bản vẽ hoàn công 06/2004 · E05 (3D / Chữ lớn)", () =>
                    InspectCh05Document("Doc_Ch05_AsBuilt2004_E05", "BẢN HOÀN CÔNG 06/2004 · E05",
                        "Hồ sơ hoàn công ngày 14/06/2004 do Giám đốc Phạm Đức Vĩnh phê duyệt:\n• Xây gạch đặc 220 mm, trát vữa hai mặt bít vĩnh viễn lối thoát Đông (34.5, 42), tháo bỏ tay nắm và khung.\n• Lý do ghi trong khung tên: Theo yêu cầu của đơn vị tài trợ Chương trình B1 — đảm bảo cô lập tuyệt đối, ngăn đối tượng điều trị rời khỏi buồng thực nghiệm trong trạng thái mất định hướng.",
                        ShowCh05BlueprintCabinetModal)),
                new(aligned ? "Xem lại bàn căn chồng bản vẽ (PZ07 ✓)" : "Căn chồng hai bản vẽ theo 2 mốc kiến trúc (PZ07)", OpenCh05Pz07Overlay)
            };
            if (ChapterFive.CanVerifyH03(State) && !g05)
                choices.Add(new ActionChoice("★ Xác minh H03 (E05 + E05b) & mở khóa LOC08 (G05)", Ch05ConfirmH03AndG05));
            choices.Add(new ActionChoice("Rời tủ bản vẽ", CloseModal));

            ui.Show("KHU KỸ THUẬT B1 · LOC09 (PZ07 / E05)", "Tủ bản vẽ hoàn công & Sơ đồ 2005",
                body.ToString().TrimEnd(), choices.ToArray(), CloseModal, Image("Doc_Ch05_AsBuilt2004_E05"));
        }

        private void InspectCh05Document(string imageId, string title, string copy, Action onBack)
        {
            var tex = Image(imageId);
            scene.atmosphere?.From("paper", player.Feet + Vector3.up, 0.55f);
            if (tex != null && inspector != null && qaFolder == null)
            {
                inspector.Open(null, tex, (float)tex.width / Mathf.Max(1, tex.height), ActiveVolume(), observationId: imageId);
                ui.ShowInspectSidebar("BẢN VẼ KỸ THUẬT B1 · LỚP R", title, copy,
                    new[]
                    {
                        new ActionChoice("Mở bản chữ lớn", () =>
                        {
                            inspector.Close();
                            ui.Show("BẢN VẼ KỸ THUẬT B1 · CHỮ LỚN", title, copy,
                                new[] { new ActionChoice("Quay lại tủ bản vẽ", onBack) }, onBack, tex, largePrint: true);
                        }),
                        new ActionChoice("Quay lại tủ bản vẽ", () => { inspector.Close(); onBack(); })
                    }, () => { inspector.Close(); onBack(); }, () => inspector.Close());
                return;
            }
            ui.Show("BẢN VẼ KỸ THUẬT B1 · LỚP R", title, copy,
                new[] { new ActionChoice("Quay lại tủ bản vẽ", onBack) }, onBack, tex, largePrint: true);
        }

        private void OpenCh05Pz07Overlay()
        {
            if (State.Happened(ChapterFive.FlagOverlayAligned))
            {
                pz07MarkerS2Offset = 0;
                pz07MarkerColumnOffset = 0;
            }
            DrawPz07OverlayModal();
        }

        private void DrawPz07OverlayModal()
        {
            bool aligned = State.Happened(ChapterFive.FlagOverlayAligned);
            bool s2Ok = pz07MarkerS2Offset == 0;
            bool colOk = pz07MarkerColumnOffset == 0;
            string s2Label = pz07MarkerS2Offset < 0 ? "Lệch Bắc (+1,8 m)" : pz07MarkerS2Offset > 0 ? "Lệch Nam (-1,8 m)" : "✓ KHỚP TRỤC CẦU THANG S2 (0,0 m)";
            string colLabel = pz07MarkerColumnOffset < 0 ? "Lệch Tây (-1,5 m)" : pz07MarkerColumnOffset > 0 ? "Lệch Đông (+1,5 m)" : "✓ KHỚP TRỤC CỘT ĐÔNG X=34,5 (0,0 m)";

            var sb = new StringBuilder();
            sb.AppendLine("Đặt Sơ đồ niêm yết 2005 chồng lên Bản vẽ hoàn công tháng 06/2004 (E05) bằng hai mốc kiến trúc cố định (hỗ trợ hoàn toàn bàn phím / nhấn chọn, không cần kéo giữ):");
            sb.AppendLine($"• Mốc 1 (Trục cầu thang S2): {s2Label}");
            sb.AppendLine($"• Mốc 2 (Hệ cột trục Đông X=34,5): {colLabel}");
            if (!string.IsNullOrEmpty(pz07Feedback))
                sb.AppendLine("\n" + pz07Feedback);
            if (!aligned && PuzzleService.ShouldShowFieldHint(State, ChapterFive.PuzzleOverlay))
                sb.AppendLine("\nGợi ý PZ07: Khớp đồng thời Mốc 1 về đúng Trục cầu thang S2 (0,0 m) và Mốc 2 về đúng Trục cột Đông X=34,5 (0,0 m), sau đó bấm Kiểm tra chồng bản vẽ.");
            if (aligned)
            {
                sb.AppendLine("\n✓ KẾT QUẢ CHỒNG BẢN VẼ (TỌA ĐỘ 34.5, 42):");
                sb.AppendLine("Sơ đồ niêm yết năm 2005 vẫn vẽ lối thoát hiểm phía Đông mở ra sân trũng — nhưng Bản hoàn công 06/2004 (E05) ghi rõ lối này đã bị xây gạch đặc 220 mm bít vĩnh viễn theo lệnh Giám đốc Vĩnh.");
                sb.AppendLine(State.Has(EvidenceRegistry.SealedWallPhoto)
                    ? "\n✓ Đã đối chiếu đủ cả bản vẽ E05 và tường bít hiện trường E05b!"
                    : "\n• H03 và G05 VẪN CHƯA MỞ: Hãy đi qua cửa Đông LOC09 → tiền sảnh LOC11 → cửa Bắc ra hành lang ngoài kiểm chứng trực tiếp bức tường tại (34.5, 42) trong hiện trường R (E05b).");
            }

            var choices = new List<ActionChoice>();
            if (!aligned)
            {
                choices.Add(new ActionChoice("Điều chỉnh Mốc 1 · Trục cầu thang S2 (Snap từng nấc)", () =>
                {
                    pz07MarkerS2Offset = pz07MarkerS2Offset < 0 ? 0 : pz07MarkerS2Offset == 0 ? 1 : -1;
                    pz07Feedback = "";
                    scene.atmosphere?.From("paperFlip", player.Feet + Vector3.up, 0.45f);
                    DrawPz07OverlayModal();
                }));
                choices.Add(new ActionChoice("Điều chỉnh Mốc 2 · Trục cột Đông X=34,5 (Snap từng nấc)", () =>
                {
                    pz07MarkerColumnOffset = pz07MarkerColumnOffset > 0 ? 0 : pz07MarkerColumnOffset == 0 ? -1 : 1;
                    pz07Feedback = "";
                    scene.atmosphere?.From("paperFlip", player.Feet + Vector3.up, 0.45f);
                    DrawPz07OverlayModal();
                }));
                choices.Add(new ActionChoice("Kiểm tra chồng bản vẽ (PZ07)", () =>
                {
                    SaveCheckpoint(() => ChapterFive.AttemptPz07OverlayAsync(transaction, s2Ok, colOk), () =>
                    {
                        if (State.Happened(ChapterFive.FlagOverlayAligned))
                        {
                            pz07Feedback = "Hai mốc kiến trúc đã ăn khớp tuyệt đối. Điểm sai lệch hiện rõ tại cửa thoát hiểm Đông (34.5, 42).";
                            scene.atmosphere?.From("clipSnap", player.Feet + Vector3.up, 0.65f);
                            ui.Toast("PZ07 ĐÃ CĂN KHỚP · CẦN KIỂM CHỨNG E05b",
                                "Bản vẽ 2004 cho thấy cửa Đông (34.5, 42) bị xây bít — cần ra hố quan sát ngoài LOC11 kiểm chứng hiện trường R trước khi chốt H03/G05.", 6.5f);
                        }
                        else
                        {
                            string err = !s2Ok && !colOk
                                ? "Cả Mốc 1 (Trục thang S2) và Mốc 2 (Trục cột Đông X=34,5) đều đang lệch khỏi lưới bản hoàn công."
                                : !s2Ok
                                    ? "Mốc 1 (Trục cầu thang S2) chưa trùng khớp với bản hoàn công."
                                    : "Mốc 2 (Hệ cột trục Đông X=34,5) chưa trùng khớp với bản hoàn công.";
                            pz07Feedback = "Chưa khớp mốc: " + err;
                            scene.atmosphere?.From("doorLatch", player.Feet + Vector3.up, 0.50f);
                        }
                        DrawPz07OverlayModal();
                    });
                }));
            }
            else if (ChapterFive.CanVerifyH03(State) && !GateService.G05(State))
            {
                choices.Add(new ActionChoice("★ Xác minh H03 (E05 + E05b) & mở khóa LOC08 (G05)", Ch05ConfirmH03AndG05));
            }
            choices.Add(new ActionChoice("Quay lại tủ bản vẽ", ShowCh05BlueprintCabinetModal));
            choices.Add(new ActionChoice("Đóng", CloseModal));

            ui.Show("ĐỐI CHIẾU KIẾN TRÚC · PZ07 (CH05-PU02)", "Chồng Sơ đồ 2005 lên Bản hoàn công 2004 (E05)",
                sb.ToString().TrimEnd(), choices.ToArray(), ShowCh05BlueprintCabinetModal,
                Image(aligned ? "Doc_Ch05_AsBuilt2004_E05" : "Doc_Ch05_PostedMap2005"));
        }

        private void Ch05OptionalValveLoc09()
        {
            if (State.chapter != "CH05" || State.layer != Reality.R)
            {
                ui.Caption("Cụm van bồn chứa dung môi kỹ thuật thuộc khoang LOC09.", 3.6f, true);
                return;
            }
            if (!State.Happened(ChapterFive.EvReturnR))
            {
                ui.Caption("Kiểm tra xe bệnh nhân AN05 ở Khu B trước khi điều tra hiện trường kỹ thuật B1.", 4.2f, true);
                return;
            }

            SaveCheckpoint(() => ChapterFive.ObserveOptionalValveAsync(transaction), () =>
            {
                string line = LineAtBeat("CH05-D08", 1, "B06");
                voice?.Speak("CH05-D08", "Minh", line, beat: "B06");
                bool hasE09 = State.Has(EvidenceRegistry.DutyLog);
                string e09Note = hasE09
                    ? "• Đối chiếu Sổ trực ca đêm E09 (02:58 báo cháy LOC09, ghi nhận mùi dung môi lạ): dấu vết cơ học củng cố nghi vấn vụ cháy có thể không phải sự cố ngẫu nhiên."
                    : "• Có thể quay lại Phòng trực tầng 2 (LOC06) đối chiếu thêm mục 02:58 ghi mùi dung môi lạ trong Sổ trực E09.";

                ui.Show("QUAN SÁT TÙY CHỌN · LOC09 (DEC-V32-03)", "Van bồn chứa dung môi AS-05 & tấm chắn gió",
                    "Trên ổ khóa đồng thau rỉ sẫm của van xả bồn dung môi có vết cắt/cạy cơ học đã cũ và ám muội lâu năm. Ngay phía trên, một chốt kim loại gỉ đang chèn mở tấm chắn gió hướng về ống thông khí sang Khu C.\n\n" +
                    e09Note + "\n" +
                    "• Giới hạn chứng cứ: Chỉ từ dấu vết này KHÔNG đủ kết luận ai đã can thiệp, nguồn phát lửa là gì hay thời điểm chính xác.\n\n" +
                    $"Minh · “{line}”",
                    new[] { new ActionChoice("Ghi nhận nghi vấn & quay lại khoang B1", CloseModal) }, CloseModal);
            });
        }

        private void Ch05SealedWallE05b()
        {
            if (State.chapter != "CH05" || State.layer != Reality.R)
            {
                ui.Caption("Mảng tường gạch xây kín phía ngoài vách Đông Khu C — hoàn toàn đặc, không có lối mở.", 4.2f, true);
                return;
            }
            if (!State.Happened(ChapterFive.EvReturnR))
            {
                ui.Caption("Mảng gạch xây bít cũ trên tường Đông Khu C. Cần kiểm tra điểm neo AN05 ở Khu B trước.", 4.4f, true);
                return;
            }

            SaveCheckpoint(() => ChapterFive.ObserveSealedWallE05bAsync(transaction), () =>
            {
                string line = LineAtBeat("CH05-D06", 1, "B07");
                voice?.Speak("CH05-D06", "Minh", line, beat: "B07");
                bool aligned = State.Happened(ChapterFive.FlagOverlayAligned);
                bool g05 = GateService.G05(State);
                var choices = new List<ActionChoice>();
                if (ChapterFive.CanVerifyH03(State) && !g05)
                    choices.Add(new ActionChoice("★ Xác minh H03 (E05 + E05b) & mở khóa LOC08 (G05)", Ch05ConfirmH03AndG05));
                choices.Add(new ActionChoice("Quay lại hành lang kỹ thuật", CloseModal));

                string verdict = g05
                    ? "✓ Đã xác minh H03 và mở khóa cửa Phòng Hành chính LOC08 (G05) trên tầng 3."
                    : aligned
                        ? "✓ Đã có đủ cả Bản hoàn công 06/2004 (E05 · đã căn PZ07) lẫn quan sát hiện trường tường bít 2012 (E05b). Có thể xác minh H03 để mở khóa LOC08 (G05)."
                        : "• Đã ghi nhận vật chứng hiện trường E05b — nhưng cần quay lại tủ bản vẽ kỹ thuật LOC09 căn chồng Sơ đồ 2005 và Bản hoàn công 06/2004 (PZ07 / E05) trước khi xác minh H03 và mở G05.";

                ui.Show("HIỆN TRƯỜNG THỰC ĐỊA · LOC11 (E05b · B07)", "Cửa thoát hiểm Đông bị xây bít từ năm 2004",
                    "Ô cửa thoát hiểm phía Đông Khu C tại mốc (34.5, 42) bị xây kín hoàn toàn bằng gạch đặc 220 mm và lớp vữa xi măng đã phong hóa; khung sắt và tay nắm bị tháo bỏ từ trước vụ cháy. Bậc bê tông cũ dẫn cụt vào tường đặc — không có khe hay lối bí mật nào xuyên qua.\n\n" +
                    $"Minh · “{line}”\n\n" + verdict,
                    choices.ToArray(), CloseModal);
            });
        }

        private void Ch05ConfirmH03AndG05()
        {
            if (!ChapterFive.CanVerifyH03(State))
            {
                ui.Caption("Cần đủ cả căn chồng bản vẽ PZ07 (E05) tại LOC09 lẫn quan sát thực địa tường bít E05b tại LOC11 để xác minh H03.", 5f, true, aboveModal: true);
                return;
            }
            CloseModal();
            SaveCheckpoint(() => ChapterFive.VerifyH03AndOpenG05Async(transaction), () =>
            {
                scene.atmosphere?.From("doorCreak", player.Feet + Vector3.up, 0.72f);
                orderlyAi?.SyncState(State);
                ui.Toast("H03 ĐÃ XÁC MINH · CỬA LOC08 MỞ (G05)",
                    "Lối thoát hiểm phía Đông Khu C đã bị xây bít từ 06/2004 theo bản hoàn công E05 và tường thực địa E05b. Quay lên cầu thang S2 → tầng 3 (LOC07) vào Phòng Hành chính LOC08.", 7.2f);
            });
        }

        // ==================== PHASE E: TASK013-C — LOC08 THRESHOLD & CH05 -> CH06 HANDOFF ====================

        private void Ch05Loc08GatePrompt()
        {
            if (GateService.G05(State))
            {
                ui.Caption("Cửa Phòng Hành chính / Giám đốc (LOC08 · G05) đã mở.", 3.5f, true);
                return;
            }
            ui.Caption("Cửa Phòng Hành chính (LOC08 · Khóa G05) đang khóa. Hoàn tất đối chiếu E05 (PZ07) tại LOC09 và xác nhận tường bít E05b tại LOC11.", 5.2f, true);
        }

        private void Ch05HandoffLoc08()
        {
            if (State.chapter != "CH05" || !GateService.G05(State))
            {
                Ch05Loc08GatePrompt();
                return;
            }
            SaveCheckpoint(() => ChapterFive.CommitCh05HandoffAsync(transaction, player.Feet), () =>
            {
                string valveStatus = State.Happened(ChapterFive.FlagValveSabotage)
                    ? "Đã ghi nhận quan sát tùy chọn van bồn AS-05 / tấm chắn gió tại LOC09 (nghi vấn đối chiếu E09, chưa kết luận thủ phạm)."
                    : "Quan sát tùy chọn van LOC09: chưa mở (không bắt buộc cho G05).";
                ui.Show("PHÒNG HÀNH CHÍNH · LOC08 (CH05-CP03)", "Bàn giao Chương 05 → Kiểm tra báo cáo",
                    "Đã chốt mốc CH05-CP03 tại ngưỡng cửa Phòng Hành chính (LOC08 · AP11).\n\n" +
                    "• Ký ức sơ tán AN05 (Lớp M): 6/6 bệnh nhân Khu B qua cầu thang S1 độc lập với B1 (bất biến, không xuất vật chứng M).\n" +
                    "• Điều tra kiến trúc PZ07 & E05b: đã xác minh H03 từ Bản hoàn công 06/2004 (E05) và tường xây bít thực địa (E05b) → Mở cổng G05.\n" +
                    $"• {valveStatus}\n\n" +
                    "Phạm vi kỹ thuật M3.3 dừng đúng tại ranh giới Chương 05 → Chương 06 (“Kiểm tra báo cáo”).\n" +
                    "Chương 06–08, câu đố PZ08, cổng G06 và các vật chứng E01 / E06 / E10b / E08 tiếp tục giữ khóa theo thiết kế.",
                    new[]
                    {
                        new ActionChoice("Ở lại kiểm tra tầng 3", CloseModal),
                        new ActionChoice("Về màn hình đầu", MainMenu)
                    }, CloseModal);
            });
        }
    }
}
