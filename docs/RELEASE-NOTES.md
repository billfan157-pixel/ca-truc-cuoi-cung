# THE LAST NIGHT SHIFT · Core v3.6.2 — Documentation Consolidation

26/11/2026 · Baseline: Core v3.6.1 · Hợp nhất tài liệu theo yêu cầu người dùng.

## Phạm vi hiện hành

- Core 04 là nguồn walkthrough CH01 duy nhất: tích hợp trực tiếp toàn bộ B01–B08, bảng Task B01/B04, bảng nhánh F101, quy tắc runtime, trợ năng, nghiệm thu và các điểm mở vào đúng mục hiện hữu; không tạo phụ lục walkthrough trùng lặp.
- Bỏ `CH01-WALKTHROUGH.md` khỏi gói phát hành. Các tham chiếu hiện hành trong Core 01–07 và JSON chuyển sang Core 04 / CH01 B01–B08; lịch sử vẫn lưu việc hợp nhất.
- Kế thừa nguyên hợp đồng v3.6.1 (DEC-V361-CH01-01): T02, WR05, OBS101A/B, F101/EX01, D08 guard, H08 recovery và tri thức E10a. Task 02 chưa hard gate; CH02–08 chưa migration. Không thêm ID hoặc quyết định gameplay.
- 7 tài liệu chính + 4 tệp hỗ trợ (RELEASE-NOTES, CHANGELOG, CANON-AUDIT, JSON) + SHA256SUMS: 12 tệp, 11 checksum. Workbook vẫn 28 sheet, 150 QA, 76 Decisions, 100 Backlog, 42 công thức Dashboard; cập nhật metadata/tham chiếu và thêm 1 hàng Changelog. JSON chỉ đổi designRevision và ch01WorldFirst.source.
- Trạng thái: chỉ hợp nhất tài liệu. Unity, parser/save runtime, QA candidate, TASK010 và TASK026–028 chưa nghiệm thu. Không phải Pre-Production Lock mới. Giữ ZIP v3.6.1 nguyên trạng.

---
## Lịch sử trước v3.6.2 (không ghi đè hợp đồng hiện hành)

# THE LAST NIGHT SHIFT · Core v3.6.1 — Consistency & Gameplay Contract Patch

26/11/2026 · DEC-V361-CH01-01 · Baseline v3.6 CH01 Design Revision

## Phạm vi hiện hành

- Vá OBS101A, phụ lục F101/E10a, WR05, bìa/metadata, QA và thời lượng, schema/loader và Canon checks.
- T01 nhận gói tại hộp thư (chìa ngoài bì). T02 mở bằng đúng chìa, không gate E10aComplete/SMSRead. Giữ gói/điện thoại; đọc riêng. CH05-D08 cần đã đọc báo, H08 cần đủ E10a/b; kiểm lại gói tại LOC08 R.
- WR05 sau seam một lần: prototype Minh tự dừng tại ngưỡng; input hướng ra bị giới hạn trước G08, không collider vô hình, relock, ép lùi/camera hoặc Journal. Cần test sequence break, không tuyên bố đã giải runtime.
- Task 02 dẫn dắt tự nhiên, chưa hard gate. CH02–08 chưa migration; TASK010 chỉ nghiệm thu sau bridge. Giữ 143 ID/copy, Save v7 mục tiêu, gate và canon. Không ghi đè v3.6.
- 7/7 Core cập nhật; 28 sheet, 150 QA (không chạy ca mới), 76 Decisions; 42 công thức và chart giữ nguyên; 12 checksum phát hành. Xem Core 04 — CH01 / Walkthrough B01–B08, CANON-AUDIT.md (đường dẫn cập nhật ở v3.6.2).

**Trạng thái:** thiết kế cập nhật, Unity/QA/playtest/thẩm định chưa nghiệm thu; chưa Pre-Production Lock.

---
## Lịch sử phát hành v3.6/v3.5 (không phải hợp đồng hiện hành)

# THE LAST NIGHT SHIFT · Core v3.6 — CH01 Design Revision

**26/11/2026 · Baseline: Core v3.5 Pre-Production Lock · DEC-V36-CH01-01**

Bản cập nhật thiết kế được tách phiên bản v3.6 vì thay hợp đồng tiếp nhận E10a và presentation CH01. Bản v3.5 gốc không bị ghi đè. Đây là revision tài liệu, KHÔNG phải Pre-Production Lock hoặc nghiệm thu Unity mới.

## Nội dung cập nhật
- Ở v3.6, tệp walkthrough riêng là nguồn chi tiết thay walkthrough v1; từ v3.6.2 nội dung đã hợp nhất vào Core 04 B01–B08.
- B01: SMS hẹn trước, Bảy tìm lại thư gốc trong kiểm kê, hộp thư sắt ngoài cổng chứa chìa cổng, giấy nhắn và E10a chống ẩm chứa thư Lan, báo công bố sơ bộ và chìa C cũ. T01 seed fixture; T02 kiểm đủ nguồn E10a, SMSRead, đúng chìa. Không cấp starter kit; không nhầm chìa C với chìa nhà S-12.
- B04: chuỗi nhiệm vụ kiểm phòng, hồ sơ thường, hốc giường, thẻ AS-05B và đối chứng tại hiện trường. Lượt hồ sơ thường đề xuất bắt buộc nhưng mặc định chưa gate (DEC-V36-CH01-02 chờ duyệt). A/B vẫn ký không OBS.
- B05: F101 trên bảng kẹp 3D; giữ đủ mark/link/kết luận và confirm lần hai; EX01 chủ động chọn đúng mốc trên bề mặt vật lý, không auto solve.
- B06–08: thu hồi giấy B, seam một lần không relock/prompt Journal, echo nhận thức tùy chọn, T10/CP04 giữ. Pocket Note CH01 đọc lại tùy chọn; CH02–08 chưa migration.
- 6 DOCX đồng bộ; 07 cập nhật 131 ô trên 17/28 sheet; thêm DEC-V36-CH01-01/02 và 8 ca QA (150 tổng), 100 task backlog giữ ID. JSON thêm metadata/prop copy, giữ token schema cũ và 143 bản ghi nguyên vẹn. 42 công thức Dashboard, chart và 11 media trong Word giữ nguyên.

## Kiểm tra và giới hạn
- Bản gốc 11/11 checksum đúng; kiểm ZIP nội bộ, ID duy nhất các bảng chính, so sánh JSON lines, 12 pocketNoteText, 14 barks, Voice Bible, UI spec và 7 bảng thoại CH02–POST trong Core 04; tất cả kiểm tra nêu trên đạt.
- Không chạy Unity, không audit repo, không xác nhận fixture/asset thực có, không đo hiệu năng, chưa playtest hoặc thẩm định nghiệp vụ; Save v7 là hợp đồng cần regression, không phải xác nhận tương thích runtime.
- Cần quyết định lượt hồ sơ giường 02 bắt buộc, prototype clearance/pivot hộp thư, thao tác EX01, A→B→C và echo B08; TASK010, TASK026–028 và V36-CH01-Q01..Q08 chưa nghiệm thu. T02 thiếu E10a sẽ không mở ở new game; nạp save cũ giữ acquired, đây là quyết định tích hợp để bảo toàn H08/CH05-D08.
- Tài liệu Word được render phục vụ kiểm bố cục; kiểm tự động nội dung/ảnh không thay thế review trình bày từng trang ở mức sản xuất. Không sửa nội dung các chương khác ngoài tham chiếu liên đới CH01.

---
## Lịch sử v3.5 (giữ nguyên nội dung release notes gốc, không áp dụng kết luận nghiệm thu cho v3.6)

# THE LAST NIGHT SHIFT · Core v3.5 (Pre-Production Lock)

**Bản khóa tiền sản xuất hoàn thiện ngày 26/11/2026.** Gói `Core v3.5 (Pre-Production Lock)` tiếp thu và xử lý dứt điểm 5 điểm còn mở sau kiểm toán v3.4 (`DEC-V35-01`), giữ nguyên 100% tương thích kỹ thuật (`143` core ID, `28` dòng legacy `CH01`, `Save Schema v7`, `G01–G08`, `PZ01–PZ10`, `Campus-v0`, `100` task Backlog) và **đóng băng tài liệu** để chuyển toàn bộ sang triển khai Unity (`TASK031-A..D`), playtest mù `TASK010` và thẩm định chuyên gia `TASK026–028`.

## 5 hạng mục vá chính ở Core v3.5 (`DEC-V35-01`)

1. **Sửa `CH06-D15` khớp cơ chế ký F101:**
   - Đổi `runtimeCopy` thành *"Đêm nay mình không xác nhận số 0. Năm đó thì có."* (12 từ, 48 ký tự) trên JSON, `07/Dialogue` và `Core 04` để không phủ nhận thao tác ký tên của nhánh B/C ở CH01.
2. **Sửa `CH05-D08` giữ trọn độ bất định & không cần guard `E09`:**
   - Đổi `runtimeCopy` thành *"Khóa van bị bẻ, cửa gió chèn hở. Chập điện thật sao?"* (12 từ, 52 ký tự) — câu nghi vấn mở chỉ dựa trên hiện trường LOC09 + mẩu báo `E10a` (CH01), miễn nhiễm với thứ tự nhặt `E09`. Cập nhật `Core 04` CH05 B06 ghi rõ đối chứng `E09` *(nếu đã thu thập)*.
3. **Tách sạch chỉ đạo dựng cảnh (`stageDirection`) khỏi phụ đề (`runtimeCopy`):**
   - Thêm cột L `Stage Direction / Cutscene Cue (Non-HUD)` trên `07/Dialogue` và trường `stageDirection` (kèm `timecodeCard`) trong JSON.
   - `CH07-D15`: rút `runtimeCopy` từ 101 từ xuống caption 16 từ `"[Tiếng thở gấp — chùm chìa khóa va khẽ — còi cứu hỏa vọng ngoài sân]"`, chuyển toàn bộ mô tả hành động/chuyển cảnh sang `stageDirection` + `timecodeCard: "04:15 — Bàn chỉ huy ngoài sân"`.
   - `POST-D08` (vá liên đới): rút `runtimeCopy` thành `"[Tiếng ngân chậm rãi, tròn đủ năm nốt giai điệu của Lan]"` (gỡ mã asset `MUS-LAN` khỏi phụ đề người chơi).
4. **Nâng cấp trợ năng thời gian đọc `INNER_SUB_SILENT` & lưu Transcript History:**
   - Giữ thời lượng mặc định `2,8–4,5s` (`1.0x`) để giữ nhịp tĩnh lặng khám phá, đồng thời bổ sung `ThoughtReadDurationScale` (`1.0x` mặc định / `1.35x` tự động khi bật Large Print = tối đa `6,08s` / `1.6x` đọc chậm = tối đa `7,2s`) và `routeToTranscriptHistory: true` vào JSON, `Core 05`, `Core 06` và `TASK031-A/B`.
5. **Chuẩn hóa quản trị nguồn canon trên `07/Decisions` & dọn nốt 3 tàn dư:**
   - Cập nhật trạng thái kế thừa rõ ràng: `DEC-V33-03` → `Partially Superseded by DEC-V34-01`; `DEC-NPR01-02` → `Clarified by DEC-V34-01` (bảo toàn Phương án A & 3 tầng thông tin); `DEC-NPR01-04` → `Updated by DEC-V34-03`; `DEC-VO-01` → `Enhanced by DEC-V34-02..03 & DEC-V35-01`; bổ sung `DEC-V35-01`.
   - Xóa sạch 3 tàn dư *"chụp mã"* / *"chụp/chép mã"* cũ ở `Core 02` (§3.2 CH06), `07/Timeline` (`EVT023`) và `07/Canon checks` (`LOG03`).

## Kết quả kiểm toán (`V35-DOC01`) & Ranh giới nghiệm thu

- **12 tệp đầy đủ** trong `Core_v3.5_PreProd_Lock.zip`; **11/11 mã `SHA-256`** khớp tuyệt đối.
- **0 lỗi cấu trúc**, **0 tàn dư cũ**, **143/143 dòng** khớp giữa `07/Dialogue` và JSON, **115/115 dòng** trên 7 bảng thoại `Core 04` khớp `07/Dialogue`, **12/12 `pocketNoteText`**, **14/14 `ambientBarks`**, **28/28 dòng legacy `CH01`** nguyên vẹn, **142 ca QA**, **100 task Backlog (`26 S, 74 M`)**, **100% hình ảnh `Core 03`** bảo toàn.
- **Ranh giới trung thực:** Đây là mốc **Pre-Production Lock** ở cấp tài liệu & dữ liệu; các cổng build thực tế (`VO-SILENT-Q01` trên Unity, tối ưu phòng 101 `≤ 16,6 ms`, `TASK010` 0/6 playtest mù, `TASK026–028` thẩm định chuyên gia) vẫn giữ nguyên trạng thái chờ thực thi.
