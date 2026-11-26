# THE LAST NIGHT SHIFT · Core v3.3.2 (NPR-01 Locked — Final Production Lock)

**Bản chốt đóng băng chính thức ngày 26/11/2026.** Gói `Core v3.3.2` hợp nhất trọn vẹn các nâng cấp của `NPR-01` (`DEC-NPR01-01–05`) vào nền `Core v3.3.1 Final Clean`, gỡ bỏ toàn bộ trang bìa *"Bản nội bộ chờ duyệt"* và các tham chiếu nháp rời ngoài gói, tái lập **một nguồn chân lý duy nhất (Single Source of Truth)** với 10/10 mã `SHA-256` khớp tuyệt đối.

## 5 nhóm nội dung đã chốt khóa trong Core v3.3.2 (`DEC-NPR01-01–05`)

1. **Khóa 6 câu thoại nâng cấp & đồng bộ `PZ10 R1/R3` (`DEC-NPR01-03`):**
   - Sửa lỗi niên đại ở `CH07-D19` của Vĩnh: *"Ba năm trước"* → ***"Hồi năm 2003"***.
   - Nâng sắc bén 2 vòng chất vấn `PZ10`: `R1` (`CH07-D23/D24` — lập luận *"bán hầm là kho"* vs 12 mã `E03`) và `R3` (`CH07-D27/D28` — sổ chìa `E02` + mốc yêu cầu mở `03:09` trên `E09`), đồng bộ trọn vẹn trên `Core 02`, `Core 04` (kể cả Walkthrough `CH07 B05/B06`), `07/Dialogue`, `07/Events`, `07/PZ10 Matrix` và `07/Puzzle specs`.
   - Khóa `CH08-D20` (`END03`) phân tách rạch ròi giữa gói hồ sơ Minh chủ động nộp có chữ ký với việc anh chưa ký lời tự khai phần mình thấy.
2. **Khóa phân tầng thông tin & chuỗi lưu giữ Phương án A (`DEC-NPR01-01`, `DEC-NPR01-02`):**
   - Giữ manifest bản sao ngoài của Bảy độc lập ở cả 4 ending; giữ Phương án A làm chuẩn lưu giữ (`E01-A/B` ở `LOC08`, `E04/E07/PROP-XK12` gắn `LOC10`, phần ngoài Bảy tách riêng); Phương án B chỉ mang đi thẩm định `TASK028`.
   - Giữ mô tả trung tính cho `EVT026` trong hồ sơ `TASK027`.
3. **Khóa quản trị sản xuất & Vietnamese-first (`DEC-NPR01-04`):**
   - Đồng bộ `Core 01` với số đếm thực tế của `07/Backlog`: **96 task (`23 S`, `73 M`; 25 hoàn thành, 12 đang làm, 4 chờ duyệt, 55 chưa làm)**.
   - Khóa kiến trúc **Vietnamese-first (`vi-VN` master + stable ID tách chuỗi hiển thị khỏi validator/save)** và quy định gắn hash nguồn sinh + build khi tái lập 9 scene Git khác bản bake trên M2 candidate `c459aba`.
4. **Khóa ưu tiên dàn dựng & đóng băng thiết kế (`DEC-NPR01-05`):**
   - Chốt 6 beat ưu tiên hình–âm (`AN05`, `03:25`, `07:40`, `PZ10`, `AN08`, `END04 B08`), mô-típ *"viết thêm một dòng"* tại `AN08`, neutral siren theo `TASK027`, và nguyên tắc không mã hóa phẩm chất đạo đức bằng ngoại hình.
   - Gỡ toàn bộ trạng thái *"Bản nội bộ chờ duyệt"* khỏi `Core 01–06`.
5. **Bảo toàn 100% tương thích kỹ thuật:**
   - Giữ nguyên 28 dòng thoại `CH01`, 3 nhánh `F101`, toàn bộ ID `LOC/AN/E/H/G/PZ/EVT/END`, 5 cặp ID & 5 bit `PZ10`, guard `CH01-D16b` tại `CH04 B06`, `143 core lines` (`0` dòng `NaN`), `Save Schema v7` và toàn bộ kiểm toán `Core v3.3.1 Final Clean`.

## Trạng thái kiểm toán (`NPR01-DOC01`) & Hành động tiếp theo

- **Kiểm toán tự động:** **100% Pass** trên toàn bộ 11 tệp (0 lỗi cấu trúc, 0 trang bìa chờ duyệt, 0 tàn dư, 143/143 dòng `Dialogue` sạch `NaN`, 28/28 dòng `CH01` nguyên vẹn, 115/115 thoại `Core 04` khớp `07/Dialogue`, 10/10 mã `SHA-256` khớp).
- **Trọng tâm ngay lúc này:** Dừng chỉnh sửa tài liệu thiết kế; giải phóng dung lượng ổ C (dọn cache `Library` Nhóm A + bản giải nén dở Nhóm C), chạy đủ 6 phiên playtest mù `TASK010` trên M2 candidate `c459aba`, và gom phản hồi thực tế cùng `TASK026–028` trước khi khóa Art Lock và thu VO chính thức.
