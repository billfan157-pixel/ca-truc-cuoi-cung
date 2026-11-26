# Nhật ký thay đổi Core v3.0 → v3.1 → v3.2 → v3.3 → v3.3.1 → v3.3.2 (NPR-01 Locked)

## Core v3.3.1 → v3.3.2 — NPR-01 Locked · Final Design Lock (26/11/2026)

| Quyết định | Thay đổi |
|---|---|
| DEC-NPR01-01 | Chốt phân tách manifest bản sao ngoài của Bảy với gói Minh nộp và lời tự khai ở CH08; cập nhật câu `CH08-D20` làm rõ hệ quả `END03` (*"Tôi đã nộp bộ hồ sơ, cả bản lưu có chữ ký của mình... Phần của tôi, tôi vẫn chưa kể"*). |
| DEC-NPR01-02 | Chốt 3 tầng thông tin (`CAN02/08` nội bộ vs `E10a` công khai vs căn cứ từng chương) và khóa chuỗi lưu giữ **Phương án A** (`LOC08`/`LOC10`/phần ngoài Bảy); Phương án B giữ ở phạm vi câu hỏi thẩm định `TASK028`; giữ wording trung tính cho `EVT026` trong hồ sơ `TASK027`. |
| DEC-NPR01-03 | Khóa hợp nhất 6 câu thoại nâng cấp của NPR-01 trên `Core 02`, `Core 04` và `Core 07`: `CH07-D19` (*"Hồi năm 2003"*), `PZ10 R1` (`CH07-D23/D24`), `PZ10 R3` (`CH07-D27/D28`) và `CH08-D20`; đồng bộ Walkthrough `CH07 B05/B06`, `07/Events`, `07/PZ10 Matrix` và `07/Puzzle specs`. Giữ nguyên 5 cặp ID và 5 bit Save v7. |
| DEC-NPR01-04 | Đồng bộ `Core 01` với `07/Backlog` thực tế (**96 task: `23 S`, `73 M`; 25 hoàn thành**); khóa nguyên tắc **Vietnamese-first (`vi-VN` master + stable ID)** và quy tắc truy vết hash nguồn sinh/build cho M2 candidate `c459aba`, 9 scene Git và nhánh kỹ thuật `M3.2` trong `Core 06`. |
| DEC-NPR01-05 | Khóa 6 beat ưu tiên hình–âm (`AN05`, `03:25`, `07:40`, `PZ10`, `AN08`, `END04 B08`), mô-típ *"viết thêm một dòng"* tại `AN08`, neutral siren theo `TASK027`, quy tắc không mã hóa đạo đức bằng ngoại hình và gỡ toàn bộ trang bìa *"Bản nội bộ chờ duyệt"* để đóng băng một nguồn chân lý duy nhất. |
| NPR01-DOC01 | Phát hành gói `Core_v3.3.2_NPR01_Locked.zip` (11 tệp đồng bộ, 0 lỗi kiểm toán, 115/115 thoại `Core 04 ↔ Core 07` khớp 100%, 10/10 mã `SHA-256` khớp tuyệt đối). |

## Core v3.3 → v3.3.1 — Final Clean Design Lock (24/11/2026 — bảo lưu truy vết)

| Quyết định | Thay đổi |
|---|---|
| DEC-V331-01..02 & V331-DOC01 | Vá 6 điểm logic/biên tập (`E01-B` 2005 bỏ cụm tháo dỡ 2012, tách trục thang `S1` vs cửa xả sân, `LOC01` phản hồi chủ quan diegetic sau `T09`, nhịp chuyển `~03:25 → 04:15`, độ mở đời thường `C12`/`C05`, quản trị `Core 01`) và dọn 7 điểm đồng bộ chéo (`V31-Q05 ↔ V33-Q03`, bỏ `"giải phẫu E04"`, sửa header `Core 02`, bỏ `"van bị cắt/dây cột"`). |

## Core v3.0 → v3.3 (24/11/2026 — bảo lưu truy vết)

| Quyết định | Thay đổi |
|---|---|
| DEC-V33-01..07 | Nhịp tích tụ tâm lý & tổ hợp suy luận `~03:25`; ngày lưu lạnh `14/11/2005` trên `PROP-XK12`; auto-acquire `PROP-XK12` qua `B02/AN08`; giám sát 7 năm & lý do Bảy trì hoãn; luật cộng hưởng & tách `CHAR007` khỏi `C12`; nâng chiều sâu Vĩnh & 12 bệnh nhân. |
| DEC-V32-01..05 | Tâm lý `03:09–03:12` & `~03:25`, thêm `CH04-D09`, dọn tàn dư CH03–06, tách UI chất vấn `PZ10`, sửa bảng `Core 04` & xóa dòng `NaN` ở `07/Dialogue`. |
| DEC-V31-01..08 | Khóa tổng 13 (`12 cháy + 1 thế thân`), tuyến `LOC10 ↔ 09 ↔ 11 ↔ 12`, phạm vi ký ức AS-05, biên độ Lan, chìa C12 duy nhất, manifest `END03/04` và hậu đề độc quyền `END04`. |

Tra `Core 07 → Changelog` để xem trạng thái theo quyết định và `Core 07 → QA` để xem các ca build chưa chạy.
