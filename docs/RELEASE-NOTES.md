# THE LAST NIGHT SHIFT · Core v3.3.1 (Final Clean — Design Lock)

**Bản chốt đóng băng tài liệu hoàn chỉnh ngày 24/11/2026.** Gói `Core v3.3.1 (Final Clean)` hoàn tất toàn bộ 6 điểm vá `DEC-V331-01–02` cùng 7 điểm dọn đồng bộ chéo cuối cùng, giữ nguyên 100% kiến trúc, ID, gate, puzzle, 28 dòng thoại `CH01`, `Save Schema v7` và canon cốt lõi.

## Tóm tắt hoàn thiện của Core v3.3.1 (Final Clean)

### 6 điểm vá logic/biên tập (`DEC-V331-01–02`)
1. **Logic thời gian `E01-B` (2005):** Bỏ cụm tháo dỡ 2012 khỏi tờ phụ lục ký năm 2005, chỉ giữ kỳ hạn theo dõi ngoại trú 7 năm (`18/11/2012`); việc thúc đẩy lịch bàn giao trùng hạn sau 2006 nằm tại `E10b` (2012) & `CAN07`.
2. **Tách hai nhánh cầu thang `S1`:** Trục thang trong nhà `S1` (`LOC02 ↔ LOC05 ↔ LOC07`) luôn thông trong lớp `R` 2012; chỉ riêng cửa xả trực tiếp từ chiếu nghỉ trệt `S1` ra sân bị chắn bằng đồ thanh lý/rào lưới ở `R` (mở thông trong `M` CH05).
3. **Cổng phụ `LOC01` sau `T09`:** Bỏ cơ chế tự khóa chốt ngoài vô lý; giữ rào tôn/xích vật lý cho 3 cổng `K-B`, `K-D`, `K-F` và dùng phản hồi chủ quan diegetic nhất quán của Minh ở `LOC01`.
4. **Nhịp chuyển `~03:25 → 04:15`:** Tách rõ khoảnh khắc rụt tay rút lên sân (`~03:25`) với cảnh chuyển thời gian nộp lại chìa tại bàn chỉ huy (`04:15`, khớp `E02`).
5. **Độ mở đời thường `C12`/`C05`:** Giữ nguyên tinh thần `E03` (*"Có thể cậu đã nghe ai kể"*) cho chi tiết đôi dép chỉ xanh và 5 nốt nhạc trong Phần 1.
6. **Quản trị sản xuất `Core 01 §3`:** Tách rõ phần đã chốt (`Unity 6.3 LTS`, `URP 17.3`, `Windows x64`, `M1`/`M2`) với phần còn mở.

### 7 điểm dọn đồng bộ chéo cuối cùng (`Final Clean`)
1. Khớp hợp đồng QA **`V31-Q05 ↔ V33-Q03`** trên `Core 02`, `Core 06 §5.1`, `07/Plot twists`, `07/QA` và `07/Narrative QA` về auto-acquire `PROP-XK12` (nguồn `R` trong túi Bảy) vào manifest xuất.
2. Xóa sạch 6 vị trí sót cụm **`"giải phẫu E04"`**, chuẩn hóa thành mô tả thể trạng trên biên bản tiếp nhận sơ bộ `E04`.
3. Sửa lỗi đánh máy header **`v3.3.1.1`** trên `Core 02` và đồng bộ nhãn bảng 4.1 / 4.3 sang `v3.3.1`.
4. Xóa sạch 3 vị trí sót **`"van bị cắt / dây cột cửa gió"`** (`Core 02`, `Core 04`, `07/Timeline`) khớp `DEC-V32-03`.
5. Đồng bộ nhịp chuyển **`~03:25 → 04:15`** tại `07/Events (CH07-B03)` và `07/Canon Matrix (~03:25)`.
6. Đồng bộ dòng **`V33-Q05`** trên `07/Narrative QA` khớp `07/QA`.
7. Cập nhật 2 đoạn quản trị sản xuất cũ ở **`Core 05 §1.4`** và **`Core 06 §2.4`** khớp thực trạng `M1`/`M2-C.1` và `TASK029`.

## Trạng thái đóng băng (`V331-DOC01`)

- **Kiểm toán tự động:** **100% Pass** trên toàn bộ 11 tệp (0 lỗi cấu trúc, 0 mâu thuẫn QA, 0 tàn dư, 0 typo phiên bản, 143/143 dòng `Dialogue` sạch `NaN`, 28/28 dòng `CH01` nguyên vẹn, 115/115 thoại `Core 04` khớp `07/Dialogue`, 10/10 `SHA-256` khớp).
- **Bước tiếp theo:** Chính thức đóng băng tài liệu; chuyển toàn lực sang tích hợp build, chạy bộ kiểm thử `V30`–`V33`, playtest mù 6 người (`TASK010`) và review chuyên gia (`TASK026–028`).
