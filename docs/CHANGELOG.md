# Core v3.6.2 — Documentation Consolidation · 26/11/2026

- Tích hợp đầy đủ walkthrough CH01 B01–B08 vào Core 04 tại các mục hiện có; xóa tệp walkthrough riêng khỏi gói, không tạo đặc tả song song.
- Đồng bộ tham chiếu, metadata v3.6.2 của 7 Core và JSON; thêm một hàng lịch sử trong 07/Changelog, không thêm Decisions hay QA.
- Bảo toàn hợp đồng v3.6.1, canon, thoại, logic và các điểm chưa nghiệm thu; gói còn 12 tệp, 11 checksum.

---
## Lịch sử trước v3.6.2 (không ghi đè hợp đồng hiện hành)

# Core v3.6.1 — Consistency & Gameplay Contract Patch · 26/11/2026

- DEC-V361-CH01-01: sửa T02, guard D08 và recovery H08; giữ 4 part E10a, không thêm chứng cứ. WR05 prototype chống sequence break; OBS101A dép/chăn.
- Đồng bộ phụ lục Core 04, Core 03, bìa Core 02, Q12/Q14/Q01/Q07/Q08, loader và V30-03; giữ 25 phút ngân sách. 52 ô/12 sheet; 76 Decisions, QA 150, Backlog 100. Không thay lời thoại; chỉ sửa guard D08.
- Task 02 chờ duyệt, CH02 bridge sau prototype. Không gán nghiệm thu mới.

---
## Lịch sử phiên bản cũ

# Core v3.6 — CH01 Design Revision · 26/11/2026

- DEC-V36-CH01-01: đưa Unified Walkthrough B01–B08 vào Core; hộp thư ngoài cổng, SMS hẹn/hoãn, provenance E10a; F101/EX01 3D và chuỗi nhiệm vụ điều tra; Pocket Note CH01 chỉ đọc lại.
- DEC-V36-CH01-02: gate hồ sơ thường chờ quyết định, mặc định A/B không đòi OBS.
- 07: 8 QA mới V36-CH01-Q01..Q08; 100 backlog giữ ID, sửa phạm vi TASK007-A/B/C, TASK009-C, TASK010, TASK031-B. 143 dialogue lines giữ nguyên. Giữ checksum mới, chart và formulas workbook.
- Giới hạn: thiết kế cập nhật, chưa Unity/QA runtime/playtest; xem CANON-AUDIT và Core 04 — CH01 / Walkthrough B01–B08 (đường dẫn cập nhật ở v3.6.2).

---

# Nhật ký thay đổi Core v3.0 → v3.5 (Pre-Production Lock)

## Core v3.4 → v3.5 — Pre-Production Lock · Vá 5 điểm kiểm toán cuối (26/11/2026)

| Quyết định | Thay đổi |
|---|---|
| DEC-V35-01 | Khép sạch 5 hạng mục kiểm toán v3.4: (1) sửa `CH06-D15` thành *"Đêm nay mình không xác nhận số 0. Năm đó thì có."* khớp cơ chế ký F101 ở cả 3 nhánh A/B/C; (2) sửa `CH05-D08` thành câu nghi vấn mở *"Khóa van bị bẻ, cửa gió chèn hở. Chập điện thật sao?"* dựa trực tiếp trên LOC09 + `E10a` (không cần thêm guard `E09`; ghi chú `Core 04` CH05 B06 nêu rõ đối chứng `E09` nếu đã thu thập); (3) tách hoàn toàn `stageDirection` và `timecodeCard` khỏi `runtimeCopy` cho `CH07-D15` (rút caption xuống 16 từ / 68 ký tự) và vá liên đới `POST-D08` (gỡ mã nội bộ `MUS-LAN` khỏi phụ đề người chơi), đồng bộ cột L mới trên `07/Dialogue` & JSON; (4) giữ mặc định `INNER_SUB_SILENT` `2,8–4,5s` (`1.0x`) và bổ sung `ThoughtReadDurationScale` (`1.0x / 1.35x` tự giãn khi bật Large Print / `1.6x` đọc chậm tối đa `7,2s`) + `routeToTranscriptHistory: true`; (5) đánh dấu rõ trạng thái kế thừa trong `07/Decisions` (`DEC-V33-03` Partially Superseded by `DEC-V34-01`, `DEC-NPR01-02` Clarified by `DEC-V34-01`, `DEC-NPR01-04` Updated by `DEC-V34-03`, `DEC-VO-01` Enhanced) và dọn nốt 3 cụm sót *"chụp mã"* ở `Core 02`, `EVT023`, `LOG03`. |

## Core v3.3.3 → v3.4 — Pre-Production Lock · Tiếp thu đánh giá độc lập (26/11/2026)

| Quyết định | Thay đổi |
|---|---|
| DEC-V34-01 | Vá 4 điểm canon/logic: rút `E01-B` & `PROP-XK12` khỏi bộ giấy Bảy nộp độc lập mọi ending; chốt lý do Vĩnh giấu `E01-A/B` tại ngăn khóa phụ `LOC08` làm bảo hiểm ngược; chốt Public Cover Story báo 2005 (`E10a`); kịch hóa nấc quyết định đêm cháy 02:50/03:05 của Vĩnh và ghi rõ `~03:25` cứu hỏa mới tiếp cận ngoài cổng/sân (chưa phá cửa B1). |
| DEC-V34-02 | Vá 6 điểm thoại & dữ liệu: bỏ `K-A` khỏi `CH05-D07`, thống nhất `"anh"` ở `CH06-D18`, đồng bộ `CH04-D02`, tách trùng `CH01-D16b`, gọt khẩu ngữ `CH06-D05` & `CH07-D11/D12/D19/D24`; hạ dấu `…` trên `INNER_SUB_SILENT` từ 20/28 xuống 2/28 và sửa `CH06-D11/D13`; làm sạch cấp cha `CH01-D10/D13` và bổ sung 12/12 `pocketNoteText` (JSON + cột K `07/Dialogue`). |
| DEC-V34-03 | Nâng chuẩn tương phản `SilentThoughtSubtitle` lên WCAG AA `≥ 4,5:1` (`#F4F1E8` + scrim tối mềm `alpha 0.56` + toggle `HighContrastThoughtBox`); bổ sung sheet `Voice Bible & Barks` (8 cặp đại từ + 14 câu PA/AI bark đồng bộ vào JSON `ambientBarks`); thêm 4 task Backlog `TASK031-A..D` (100 task: `26 S, 74 M`); chèn cached values cho `07/Dashboard`; đổi nhãn sang **Pre-Production Lock** và dừng cập nhật bản mới cho đến khi hoàn tất thử nghiệm Unity & `TASK010`. |

## Core v3.3.2 → v3.3.3 — Silent Exploration & VO Lock (26/11/2026 — bảo lưu truy vết)

| Quyết định | Thay đổi |
|---|---|
| DEC-VO-01 | Áp dụng chính sách Khám phá Im lặng (82 core VO lines, 28 `INNER_SUB_SILENT`, 12 `CUT_TO_JOURNAL`, 21 `UI_NON_VO`) và xuất `dialogue_silent_exploration_runtime.json`. |

## Core v3.3.1 → v3.3.2 — NPR-01 Locked (26/11/2026 — bảo lưu truy vết)

| Quyết định | Thay đổi |
|---|---|
| DEC-NPR01-01..05 | Khóa 6 câu thoại nâng cấp, phân tầng 3 lớp thông tin, chuỗi lưu Phương án A, kiến trúc Vietnamese-first và gỡ trang bìa chờ duyệt. |

## Core v3.0 → v3.3.1 Final Clean (24/11/2026 — bảo lưu truy vết)

| Quyết định | Thay đổi |
|---|---|
| DEC-V31..V331 | Khóa tổng 13 (`12 cháy + 1 thế thân`), tuyến `LOC10↔09↔11↔12`, tâm lý `~03:25 → 04:15 → 07:40`, `PROP-XK12` lưu lạnh `14/11/2005` & auto-acquire, tách UI `PZ10`, tách trục thang `S1`, phản hồi chủ quan `LOC01`. |
