# Kiểm toán canon và tính liên tục Core v3.3.1 (Final Clean — Design Lock)

Kiểm đối chiếu ngày 24/11/2026 — kế thừa trọn bộ kiểm toán v3.0 → v3.3 (`DEC-V31-01–08`, `DEC-V32-01–05`, `DEC-V33-01–07`), khép sạch 6 điểm bản vá `v3.3.1` (`DEC-V331-01–02`) và dọn dứt điểm 7 điểm đồng bộ chéo cuối cùng ở bản **Core v3.3.1 (Final Clean)**. Mốc và ID là đường tra trực tiếp tới 7 tệp nguồn. Các kết luận mang nhãn “canon nội bộ” thuộc nhóm tác giả; tri thức người chơi và nhân vật đi theo `07/Canon Matrix` và `Reveal order`.

## Phần A — Kế thừa các vòng kiểm toán trước (bảo toàn 100%)

- **v3.0 → v3.1 (`DEC-V31-01–08`):** Khóa tổng 13 (`12 cháy + 1 thế thân`), tuyến `LOC10 ↔ 09 ↔ 11 ↔ 12`, phạm vi ký ức AS-05, biên độ Lan, chìa C12 duy nhất, manifest `END03/04` và hậu đề độc quyền `END04`.
- **v3.1 → v3.2 (`DEC-V32-01–05`):** Bỏ cửa kẹt nhiệt/người của Vĩnh ở `~03:25`, sửa `CH07-D11` đúng biên độ Lan, thêm `CH04-D09` giải thích mốc `02:50`, dọn tàn dư CH03–06, tách UI chất vấn `PZ10` khỏi `Hypothesis Board`, sửa 3 bảng lệch CH06–08 ở `Core 04` và xóa hàng trống `NaN` ở `07/Dialogue`.
- **v3.2 → v3.3 (`DEC-V33-01–07`):** Hoàn thiện bước tích tụ tâm lý & tổ hợp suy luận `~03:25`, bổ sung ngày lưu lạnh `14/11/2005` trên `PROP-XK12`, tự động thu `PROP-XK12` vào manifest xuất qua `B02/AN08`, khóa manifest phần ngoài của Bảy, dọn 5 điểm tàn dư cũ, khóa luật cộng hưởng & phân tách 3 tầng thực thể (`C12 ≠ CHAR007`), nâng cấp chiều sâu Giám đốc Vĩnh & 12 bệnh nhân `C01–C12`.

## Phần B — Bản vá v3.3 → v3.3.1 (`DEC-V331-01–02`)

| Mục audit v3.3 | Hạng | Vấn đề tồn tại trong v3.3 | Khắc phục chốt trong Core v3.3.1 | Kiểm |
|---|---|---|---|---|
| 01. `E01-B` (2005) biết trước ngày phá dỡ 2012 | P1 | `E01-B` ký `18/11/2005` ghi *(thời điểm hoàn tất bàn giao tháo dỡ cơ sở cũ)* trong khi An Sinh năm 2006 mới đóng cửa | Bỏ cụm tháo dỡ khỏi văn bản ký năm 2005 `E01-B`, chỉ giữ **chu kỳ theo dõi ngoại trú bảy năm (`18/11/2012`)**; việc Hội đồng thúc đẩy lịch bàn giao trùng hạn sau 2006 được ghi ở `E10b` (2012) và `CAN07`/`EVT023` | V331-DOC01 |
| 02. Tuyến cầu thang `S1` nguy cơ thành blocker | P1 | `Core 03 §8` & `Core 04` viết gọn khiến dễ hiểu nhầm toàn bộ `S1` bị chặn ở `R`, xung đột tuyến `LOC02–05–07` | Tách rõ 2 phần của `S1`: **trục thang trong nhà (`LOC02 ↔ LOC05 ↔ LOC07`) luôn thông trong lớp `R`**; chỉ riêng **cửa thoát xả trực tiếp từ chiếu nghỉ tầng trệt `S1` ra sân ngoài trời** mới bị đồ thanh lý và rào lưới chắn ở `R` (mở thông trong `M` CH05) | V33-Q05 |
| 03. Cơ chế khóa lại của cổng phụ `LOC01` sau `T09` | P1 | Ghi *"khóa chốt công trình / chốt xích ngoài"* dù Minh đã mở bằng chìa `E10a` và `CAN07` không ai canh cổng | Giữ rào tôn/dây xích vật lý cho **3 cổng Minh không có chìa (`K-B`, `K-D`, `K-F`)**; riêng **cổng phụ `LOC01`** sau lượt `T09` không tự khóa chốt ngoài mà dùng **phản hồi chủ quan diegetic nhất quán của chính Minh** (nhịp ù trầm `AS-05` + lời tự thoại chưa thể bỏ đi khi sự thật còn dang dở, lùi nhẹ về sân kèm gợi ý Journal) | V33-Q05 |
| 04. Khoảng trống 50 phút giữa `~03:25` và `04:15` | P2 | `CH07-D15`, `EVT026`, `E08` viết liền *"rụt tay quay lưng chạy lên sân giao chìa lúc 04:15"* dễ gây hiểu nhầm diễn ra trong vài giây | Tách rõ thành 2 nhịp có **chuyển cảnh thời gian**: `~03:25` Minh đóng băng rụt tay, quay lưng rút lên sân giữa lúc cứu hỏa đổ vào → chuyển thời gian gần 50 phút bàng hoàng ngoài sân → `04:15` nộp lại chùm chìa tại bàn chỉ huy (`E02`) | V33-Q01 |
| 05. `C12` và đôi dép chỉ xanh / 5 nốt nhạc | P2 | Hồ sơ `C05`/`C12` ở `Core 02 §4.3` & `07/Patients` khẳng định cứng C12 bắt dư âm cảm xúc, vênh độ mở ở `E03` | Giữ nguyên **độ mở đời thường** trong Phần 1 (ở chung khu hầm có thể quan sát/nghe kể, nghe Lan hát ru Bé An), chỉ để gợi mở; cơ chế nhạy vật neo giữ ở mức canon nội bộ cho phần sau | V33-Q06 |
| 06. Thông tin quản trị cũ ở `Core 01` | P2 | `Core 01 §3` vẫn ghi *"engine chưa được cung cấp / chưa có prototype"* vênh với `Core 06` & `DEC-V2-04` | Cập nhật `Core 01 §3` tách rõ phần **đã chốt ở M0–M2** (`Unity 6.3 LTS`, `URP 17.3`, `Windows x64`, prototype `M1`/`M2`) và phần **thực sự còn mở** (nền tảng phụ, ngân sách, nhân sự, full VO, lịch phát hành, playtest thời lượng) | V331-DOC01 |

## Phần C — 7 điểm dọn đồng bộ chéo cuối cùng (Final Clean)

1. **Khớp hợp đồng QA `V31-Q05 ↔ V33-Q03`:** Cập nhật `Core 06 §5.1`, `Core 02 §3.3`, `07/Plot twists (PT06)`, `07/QA (V31-Q05)` và `07/Narrative QA (V31-Q05)` — thống nhất không auto-mark `inspectedProps.PROP_XK12` và không cấp vật chứng trái luật (`E08`/`Chìa C12`), còn liên gốc `PROP-XK12` có sẵn trong túi Bảy tại `AN08` được tự động thu nguồn `R` vào manifest xuất `END03/04` khi hoàn tất `B02`/fallback `AN08`.
2. **Xóa sạch 6 chỗ sót cụm `"giải phẫu E04"`:** Đổi toàn bộ trên `Core 02 (FS23)`, `Core 04` (CH04 nghiệm thu, `CH04-D08`, `V30-Q04`), `Core 06 (C12AnomalyNoted)`, `07/Dialogue (CH04-D08 Context)` và `07/Plot twists (PT02)` thành **mô tả thể trạng trên biên bản tiếp nhận sơ bộ `E04`** (giữ nguyên `CHAR-Q02` về giải phẫu model nhân vật 3D).
3. **Sửa lỗi đánh máy header `Core 02`:** Sửa `NARRATIVE BIBLE v3.3.1.1` → `v3.3.1` và nâng 2 tiêu đề cột bảng 4.1 / 4.3 sang `v3.3.1`.
4. **Xóa sạch 3 chỗ sót `"van bị cắt / dây cột cửa gió"`:** Đồng bộ `Core 04` (Asset CH05), `Core 02 (EVT006)` và `07/Timeline (EVT006)` về đúng mô tả `DEC-V32-03` (*dấu can thiệp cơ khí cũ đã gỉ/ám muội trên cụm khóa van & chốt cửa gió chèn hở*).
5. **Đồng bộ nhịp chuyển `~03:25 → 04:15` ở 2 bảng còn sót:** Cập nhật `07/Events (CH07-B03)` và `07/Canon Matrix (~03:25 / EVT026)`.
6. **Đồng bộ `07/Narrative QA (V33-Q05)` với `07/QA (V33-Q05)`:** Nêu rõ trục thang nội bộ `S1` thông trong `R` và cổng `LOC01` dùng phản hồi chủ quan diegetic.
7. **Cập nhật 2 đoạn quản trị sản xuất cũ ở `Core 05 §1.4` và `Core 06 §2.4`:** Ghi rõ thực trạng bản mẫu `M1`/`M2-C.1` và đánh dấu `Engine spike` là bối cảnh lịch sử tiền `M1` trước `TASK029`.

**Bằng chứng tài liệu (`V331-DOC01`).** Quét tự động toàn bộ 11 tệp gói **Core v3.3.1 (Final Clean)**: **0 lỗi cấu trúc**, **0 mâu thuẫn hợp đồng QA**, **0 tàn dư trái canon**, **0 lỗi đánh máy phiên bản**, **0 dòng trống `NaN`** trong `07/Dialogue` (143 dòng có ID; 28/28 dòng `CH01` nguyên vẹn 100%; 115/115 dòng thoại trong `Core 04` khớp tuyệt đối `07/Dialogue`), công thức `Dashboard` sống và 10/10 mã `SHA-256` hợp lệ.
