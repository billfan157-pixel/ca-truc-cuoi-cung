# Kiểm toán canon và tính liên tục Core v3.3.2 (NPR-01 Locked — Design Lock)

Kiểm đối chiếu ngày 26/11/2026 — kế thừa trọn bộ kiểm toán `v3.0 → v3.3.1 Final Clean` (`DEC-V31–V331`) và **chốt hợp nhất hoàn toàn gói `NPR-01` (`DEC-NPR01-01–05`)** vào một bộ nguồn chân lý duy nhất gồm 11 tệp (gỡ bỏ toàn bộ trang bìa *"Bản nội bộ chờ duyệt"* và các liên kết tới tệp nháp rời ngoài gói). Các kết luận mang nhãn “canon nội bộ” thuộc nhóm tác giả; tri thức người chơi và nhân vật đi theo `07/Canon Matrix` và `Reveal order`.

## Phần A — Kế thừa các vòng kiểm toán nền (bảo toàn 100%)

- **v3.0 → v3.1 (`DEC-V31-01–08`):** Khóa tổng 13 (`12 cháy + 1 thế thân`), tuyến `LOC10 ↔ 09 ↔ 11 ↔ 12`, phạm vi ký ức AS-05, biên độ Lan, chìa C12 duy nhất, manifest `END03/04` và hậu đề độc quyền `END04`.
- **v3.1 → v3.2 (`DEC-V32-01–05`):** Bỏ cửa kẹt nhiệt/người của Vĩnh ở `~03:25`, sửa `CH07-D11` đúng biên độ Lan, thêm `CH04-D09` giải thích mốc `02:50`, tách UI chất vấn `PZ10` khỏi `Hypothesis Board`, sửa bảng lệch `Core 04` và xóa dòng `NaN` ở `07/Dialogue`.
- **v3.2 → v3.3 (`DEC-V33-01–07`):** Hoàn thiện nhịp tích tụ tâm lý & tổ hợp suy luận `~03:25`, bổ sung ngày lưu lạnh `14/11/2005` trên `PROP-XK12`, tự động thu `PROP-XK12` vào manifest xuất qua `B02/AN08`, khóa manifest phần ngoài Bảy, khóa luật cộng hưởng & tách 3 tầng thực thể (`C12 ≠ CHAR007`), nâng cấp chiều sâu Vĩnh & 12 bệnh nhân `C01–C12`.
- **v3.3 → v3.3.1 Final Clean (`DEC-V331-01–02`, `V331-DOC01`):** Bỏ cụm tháo dỡ 2012 khỏi tờ ký 2005 `E01-B`, tách trục thang nội bộ `S1` khỏi cửa xả sân `S1`, thay chốt ngoài `LOC01` bằng phản hồi chủ quan diegetic, thêm nhịp chuyển `~03:25 → 04:15`, giữ độ mở đời thường `C12`/`C05`, khớp hợp đồng QA `V31-Q05 ↔ V33-Q03`, xóa cụm `"giải phẫu E04"` và `"van bị cắt/dây cột"`.

## Phần B — Khóa chính thức gói NPR-01 (`DEC-NPR01-01–05`)

| Quyết định chốt | Trọng tâm | Nội dung khóa trong Core v3.3.2 | Kiểm |
|---|---|---|---|
| **DEC-NPR01-01** | Nguồn & trách nhiệm CH08 (`END03/04`) | Giữ manifest bản sao ngoài của Bảy độc lập ở cả 4 ending; nâng câu `CH08-D20` tách rạch ròi `END03` (Minh nộp bộ hồ sơ có chữ ký nên được mời giải trình nhưng chưa kể phần mình chứng kiến) với `END04`; `END01/02` để ngỏ diễn tiến điều tra. | NPR01-DOC01 |
| **DEC-NPR01-02** | 3 tầng thông tin & Chuỗi lưu Phương án A | Phân tách canon nội bộ (`CAN02/08`), tin công khai (`E10a`) và căn cứ từng chương; **chốt Phương án A** làm chuẩn (`E01-A/B` tại `LOC08`, `E04/E07/PROP-XK12` gắn `LOC10`, phần ngoài Bảy tách riêng); Phương án B chỉ mang đi thẩm định `TASK028`. Giữ mô tả trung tính cho `EVT026` trong hồ sơ `TASK027`. | NPR01-DOC01 |
| **DEC-NPR01-03** | Khóa 6 câu thoại & đồng bộ `PZ10 R1/R3` | Sửa lỗi niên đại `CH07-D19` (*"Ba năm trước"* → ***"Hồi năm 2003"***); nâng sắc bén `PZ10 R1` (`CH07-D23/D24`: bán hầm là kho vs 12 mã `E03`) và `R3` (`CH07-D27/D28`: sổ chìa + sổ trực `03:09`); khóa `CH08-D20`; đồng bộ Walkthrough `CH07 B05/B06`, `07/Events`, `07/PZ10 Matrix` và `07/Puzzle specs`. Giữ nguyên 5 cặp ID & 5 bit Save v7. | NPR01-DOC01 |
| **DEC-NPR01-04** | Quản trị sản xuất & Vietnamese-first | Khớp số đếm thực tế `07/Backlog`: **96 task (`23 S`, `73 M`; 25 hoàn thành)**; khóa kiến trúc **Vietnamese-first (`vi-VN` master + stable ID tách khỏi validator/save)**; ghi nhận M2 candidate `c459aba` (chờ playtest mù `0/6` `TASK010` và `TASK026–028`), tách nhánh kỹ thuật `M3.2` và quy định gắn hash nguồn sinh + build khi tái lập 9 scene Git. | NPR01-DOC01 |
| **DEC-NPR01-05** | Dàn dựng ưu tiên & Đóng băng 1 nguồn duy nhất | Chốt 6 beat ưu tiên hình–âm (`AN05`, `03:25`, `07:40`, `PZ10`, `AN08`, `END04 B08`), mô-típ *"viết thêm một dòng"* hồi đáp tại `AN08`, neutral siren theo `TASK027` và nguyên tắc không mã hóa đạo đức bằng ngoại hình. Gỡ toàn bộ trang bìa *"Bản nội bộ chờ duyệt"* để tránh phân mảnh nguồn. | NPR01-DOC01 |

**Bằng chứng tài liệu (`NPR01-DOC01`).** Quét tự động toàn bộ 11 tệp gói **Core v3.3.2 (NPR-01 Locked)**: **0 lỗi cấu trúc**, **0 trang bìa chờ duyệt**, **0 tàn dư trái canon**, **0 mâu thuẫn QA**, **0 dòng trống `NaN`** trong `07/Dialogue` (143 dòng có ID; 28/28 dòng `CH01` nguyên vẹn 100%; 115/115 thoại trong `Core 04` khớp tuyệt đối `07/Dialogue`), công thức `Dashboard` sống và 10/10 mã `SHA-256` khớp tuyệt đối.
