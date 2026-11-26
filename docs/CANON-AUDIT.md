# Canon audit — Core v3.6.2 · 26/11/2026

## Kiểm tra hợp nhất tài liệu

- Đối chiếu ZIP baseline v3.6.1 trước sửa. Core 04 nhận toàn bộ 8 beat, 3 bảng đặc tả, phần runtime/trợ năng, nghiệm thu và điểm mở từ walkthrough riêng; giữ các chi tiết cũ không trùng. Bỏ file riêng, cập nhật toàn bộ tham chiếu hiện hành sang Core 04. Không sửa canon/gameplay.
- 6 DOCX mở được; media bảo toàn byte. 6 DOCX đã render để kiểm bố cục; kiểm trực quan tập trung Core 04 CH01, chưa nghiệm thu từng trang ở 100%.
- Workbook: 28 sheets; 150 QA, 76 Decisions, 100 Backlog, 143 Dialogue. Chỉnh 10 ô metadata/tham chiếu hiện hữu và thêm 5 ô ở 1 hàng Changelog trên tổng 5 sheet; bảng Changelog tăng một hàng. Công thức, chart, styles và các sheet dữ liệu khác giữ nguyên.
- JSON: chỉ đổi designRevision và ch01WorldFirst.source. 143 thoại, guards, 12 pocketNoteText, 14 barks, Voice Bible và schemaVersion không đổi.
- Gói còn 12 tệp, 11 SHA-256 hợp lệ; không còn đường dẫn triển khai tới tệp walkthrough đã bỏ. Tất cả điều kiện kiểm thử mở của v3.6.1 được bảo toàn. Không chạy Unity hoặc playtest; không xác nhận tương thích save thực tế.

---
## Lịch sử trước v3.6.2 (không ghi đè hợp đồng hiện hành)

# Canon audit — Core v3.6.1 · 26/11/2026

## Kết quả đối chiếu và vá

| ID audit | Xử lý |
|---|---|
| V36-AUD-01 | OBS101A B chỉ dép/chăn; dấu bánh và ổ cắm dành CH02 R/PZ01. |
| V36-AUD-02 | Core 04 phụ lục thay copy SMS/hộp thư và modal hai cột bằng hotspot bảng kẹp 3D. |
| V36-AUD-03/07 | WR05 đồng bộ; prototype giới hạn input hướng ra tại ngưỡng sau T09, không lùi/camera cưỡng ép, không collider vô hình, không seam lặp. P0 test chống sequence break còn mở. |
| V36-AUD-04 | Bìa/metadata Core 02 và nhãn hiện hành v3.6.1; giữ lịch sử v3.5/v3.6. |
| V36-AUD-05 | Q12/Q14 đồng bộ; 25 phút ngân sách, tuyến sâu 24–34 + tùy chọn 4–6, A/B ngắn chưa đo. |
| V36-AUD-06/08 | schemaVersion 3.5 giữ; designRevision 3.6.1; TASK031-A/B và hợp đồng loader mới, cần test thực. |
| V36-AUD-09 | V30-03 ghi rõ phần điều kiện trigger/presentation đã thay. |
| V36-AUD-10 | Không đổi trạng thái QA cũ; chỉ dùng kết quả gắn build cũ, candidate mới chưa nghiệm thu. |
| T02 | Nhận gói atomic nhưng không auto-read; dùng chìa đúng mở cổng dù chưa kiểm E10a/SMS. D08 guard báo, H08 có bàn kiểm lại gói/điện thoại tại LOC08. |
| Task 02 / CH02 | Mặc định chưa gate; DEC-V36-CH01-02 chờ duyệt; prototype CH01 rồi bridge PZ01/02 riêng. |

## Kiểm tra máy thực hiện

- Baseline v3.6: 12/12 SHA-256 hợp lệ trước sửa. Giữ baseline nguyên trạng.
- 6 DOCX mở được, 11 media giữ nguyên byte. Word đã render kiểm bố cục; chưa nghiệm thu từng trang ở 100%.
- Workbook: 28 sheets, 52 ô thay/thêm trên 12 sheet; 42 công thức, chart Dashboard và styles giữ nguyên. 143 Dialogue, 17 Puzzle, 18 Evidence, 62 Events, 150 QA, 100 Backlog, 76 Decisions; không trùng ID ở các bảng đã kiểm. Hai bảng Decisions/Changelog mở rộng phạm vi một hàng.
- 143 ID, legacyCopy/runtimeCopy không đổi. CH05-D08 chỉ cập nhật context/uiAudioRule và metadata guard tương ứng. Voice Bible, ambientBarks, UI spec và schemaVersion giữ nguyên. JSON hợp lệ; parser Unity chưa kiểm.
- Không thay G01–G08, E/PZ, bốn ending, chronology hoặc nguồn lịch sử; thay điều kiện T02/tri thức/WR05. Không coi Save v7 đã tương thích thực tế.

## Còn mở

B07 cần prototype để xác minh input boundary không gây cảm giác invisible wall; đây là lựa chọn thiết kế mới, không kết quả build. Vị trí hộp thư, thao tác nhận gói, đọc lại LOC08, EX01, echo B08, A→B→C, save/loader cần runtime test. Không khóa Task 02 hoặc migration CH02. TASK010/026–028 chưa nghiệm thu. Đây là design patch, không phải Pre-Production Lock.

---
## Lịch sử v3.6 và v3.5 — không áp dụng hợp đồng cũ thay cho v3.6.1

# Canon audit · Core v3.6 CH01 Design Revision

## Kiểm tra thực hiện

| Kiểm tra | Kết quả |
|---|---|
| Baseline v3.5 SHA-256 | 11/11 đúng |
| JSON lines / legacyCopy / runtimeCopy | 143/143 nguyên vẹn theo baseline; 28 dòng chapter CH01 (D16b thuộc CH04 dù ID prefix CH01) |
| Pocket Note / ambientBarks / voiceBible / UI spec | 12/12; 14/14; không thay nội dung |
| 07/Dialogue và Voice Bible & Barks | Sheet XML byte-identical baseline |
| Core 04 bảng thoại CH02–POST | 7/7 giữ nguyên từng cell text |
| Workbook | 28 sheet; 131 ô đổi/thêm trên 17 sheet; 42 công thức giữ nguyên, chart Dashboard giữ nguyên; 150 QA, 100 backlog, 75 decisions; bảng ID chính không trùng |
| Word | 6 DOCX mở được; media từng file byte-identical (10 Core03, 1 Core04) |
| Hợp đồng CH01 | T01–10, T11, CP01–04, A/B/C, OBS101A/B, EX01, G01/G02 được giữ ID; presentation/điều kiện T01/T02 thay đổi |
| Runtime Unity | Chưa chạy; không gán Pass |

## Quyết định và giới hạn

- E10a giữ 4 part ID; `signedAppointment` giữ tên ID legacy nhưng thể hiện qua giấy nhắn + SMS hẹn. Bảy đã lưu bản sao thư từ trước; bản gốc tìm lại lúc kiểm kê 2012. Chìa C cũ không phải chìa nhà S-12, chỉ inventory/metadata. Mẩu báo là nguyên nhân sơ bộ công bố; không sửa bi kịch 2005, không lộ E10b/AS-05/C01 sớm.
- B01 có động cơ đi vào sau cảnh báo: gọi không được Bảy, thấy sảnh sáng; chỉ gặp Lan khi đã chuyển B. Hộp thư ở ngoài vùng khóa. Vị trí, tầm với, cửa/collider cần kiểm greybox. T02 yêu cầu đủ nguồn E10a để tránh hụt tiền đề CH05-D08/H08, thay việc seed sẵn của v3.5. Không thu lại E10a ở save legacy đã có.
- Task 02 chưa bắt buộc đến khi quyết định DEC-V36-CH01-02 được duyệt. A/B vẫn không cần OBS. EX01 cần thao tác chọn đúng 03:17, không tự solve. Echo B08 chỉ nhận thức, không evidence. CH02–08 còn Journal; migration toàn game nằm ngoài phạm vi.
- Kiểm tra tự động xác nhận các invariants được liệt kê, không tuyên bố zero lỗi toàn dự án. Không chạy build, không chứng minh Save v7 tương thích thực tế, không review chuyên gia. Giữ kết quả build cũ làm lịch sử. QA mới và TASK010/026–028 còn mở.
- Đã render Word; review bố cục giới hạn ở bản xem tổng thể/điểm sửa, chưa nghiệm thu thị giác toàn bộ từng trang ở 100%.

---
## Lịch sử kiểm toán v3.5

# Kiểm toán canon và tính liên tục Core v3.5 (Pre-Production Lock)

Kiểm đối chiếu toàn diện ngày 26/11/2026 — kế thừa trọn bộ kiểm toán `v3.0 → v3.4` và xác minh gói vá 5 điểm kiểm toán cuối **`DEC-V35-01`** trên 6 tài liệu Word (`Core 01–06`), 28 sheet của `07_Production_Database.xlsx` và `dialogue_silent_exploration_runtime.json`.

## Điểm xác minh chính ở Core v3.5 (`V35-DOC01`)

1. **Khớp cơ chế ký F101 (`CH06-D15`):**
   - `runtimeCopy` đổi sang *"Đêm nay mình không xác nhận số 0. Năm đó thì có."* (12 từ / 48 ký tự), đồng bộ 100% giữa JSON, `07/Dialogue` và `Core 04`, phản ánh đúng việc nhánh B/C vẫn ký tên `N. H. Minh` nhưng không xác nhận số 0.
2. **Bảo toàn mức bất định & độc lập thứ tự `E09` (`CH05-D08`):**
   - `runtimeCopy` đổi sang câu nghi vấn mở *"Khóa van bị bẻ, cửa gió chèn hở. Chập điện thật sao?"* (12 từ / 52 ký tự), chỉ dựa trên hiện trường LOC09 và mẩu báo `E10a` (bắt buộc ở CH01) nên không cần tạo thêm guard `E09`. `Core 04` CH05 B06 bổ sung chú thích *"nếu đã thu thập"* khi nhắc đối chứng `E09`.
3. **Tách sạch `stageDirection` khỏi `runtimeCopy` (`CH07-D15` & `POST-D08`):**
   - Bổ sung cột L `Stage Direction / Cutscene Cue (Non-HUD)` trên `07/Dialogue` và trường `stageDirection` (kèm `timecodeCard` trên `CH07-D15`) trong JSON.
   - `CH07-D15` `runtimeCopy` rút từ 101 từ (461 ký tự) xuống còn **16 từ (68 ký tự)**: `"[Tiếng thở gấp — chùm chìa khóa va khẽ — còi cứu hỏa vọng ngoài sân]"`.
   - `POST-D08` `runtimeCopy` rút thành **12 từ (56 ký tự)**: `"[Tiếng ngân chậm rãi, tròn đủ năm nốt giai điệu của Lan]"` (gỡ mã asset `MUS-LAN` khỏi phụ đề).
4. **Trợ năng thời gian đọc `INNER_SUB_SILENT` & Transcript History:**
   - Giữ mặc định `2,8–4,5s` (`1.0x`, tối đa `13,78 CPS` cho dòng dài nhất `CH08-D12` 62 ký tự) để không làm loãng nhịp khám phá; bổ sung `ThoughtReadDurationScale` (`[1.0, 1.35, 1.6]`, tự giãn `1.35x` = `6,08s` / `10,2 CPS` khi bật Large Print, tối đa `7,2s` / `8,61 CPS` ở `1.6x`) và `routeToTranscriptHistory: true` trên JSON, `Core 05`, `Core 06` và `TASK031-A/B`.
5. **Quản trị trạng thái kế thừa `07/Decisions` & dọn sạch tàn dư *"chụp mã"*:**
   - Gắn nhãn chính xác `DEC-V33-03` (`Partially Superseded by DEC-V34-01`), `DEC-NPR01-02` (`Clarified by DEC-V34-01` — giữ nguyên Phương án A & 3 tầng thông tin), `DEC-NPR01-04` (`Updated by DEC-V34-03`) và `DEC-VO-01` (`Enhanced by DEC-V34-02..03 & DEC-V35-01`), kèm bản ghi mới `DEC-V35-01`.
   - Xóa sạch 3 tàn dư *"chụp mã"* / *"chụp/chép mã"* cũ tại `Core 02` (§3.2 CH06), `07/Timeline` (`EVT023`) và `07/Canon checks` (`LOG03`).

**Bằng chứng tự động (`V35-DOC01`):** **0 lỗi cấu trúc**, **0 tàn dư trái canon**, **0 stage direction rò vào `runtimeCopy`**, **143/143 dòng** JSON ↔ `07/Dialogue`, **115/115 dòng** `Core 04` ↔ `07/Dialogue`, **12/12 `pocketNoteText`**, **14/14 `ambientBarks`**, **28/28 legacy `CH01`** nguyên vẹn, **100 task Backlog (`26 S, 74 M`)**, **142 QA cases**, **Dashboard cached values đầy đủ**, **100% ảnh nhúng `Core 03` bảo toàn**, và **11/11 mã `SHA-256`** khớp tuyệt đối. Các ca build thực tế (`VO-SILENT-Q01` Unity, `TASK010` 0/6, `TASK026–028`, tối ưu 101 `≤ 16,6 ms`) giữ nguyên trạng thái chờ nghiệm thu.
