# Phiếu Kiểm duyệt Chuyên gia & Cổng Con người (`TASK026`–`TASK028`)

- **Phạm vi:** Chương 01 (`CH01 — Người trở về`, sau 8 hạng mục nâng cấp chất lượng) & Vertical Slice M2 (`LOC01–LOC04`)
- **Trạng thái tổng thể:** **MỞ (0/3 cổng chuyên gia đã ký duyệt bởi con người)**
- **Nguyên tắc trung thực (`AGENTS.md` / `ctcc-qa`):** Tuyệt đối không giả lập hay tự ký thay chuyên gia. Tài liệu này chuẩn hóa đúng các hạng mục cần duyệt thực tế trên bản build Windows x64 (`CaTrucCuoiCung.exe`) và bộ ảnh/tài liệu đi kèm.

---

## 1. `TASK026` — Kiến trúc Bệnh viện, Không gian & Tính chân thực Thị giác (1987–2005–2012)

| Hạng mục cần thẩm định | Hiện vật kiểm tra trên build | Trạng thái | Ghi chú chuyên gia |
| --- | --- | --- | --- |
| Tỷ lệ hành lang, cửa Phòng 101 thông cáng `0,72 m`, biển chỉ dẫn men sứ, quầy lễ tân kính khung gỗ | Scene `LOC01–LOC04`, `corridor-long-B.png`, `room101-B.png` | **MỞ** | |
| Pass Weathering & Decal hữu cơ lớp R/B: vệt ố vàng quanh máng đèn tuýp T8, vết kéo cáng trên nền gạch bông, thấm nước mưa chân tường terrazzo | `corridor-long-B.png`, `corridor-long-R.png`, `room101-B.png`, `room101-R.png` | **MỞ** | |
| Ánh sáng thể tích dưới đèn hành lang/mái hiên, hạt bụi lơ lửng và sương thấp ngoài sân cổng phụ `LOC01` | `loc01-courtyard-R.png`, `loc01-exterior-R.png`, `corridor-long-B.png` | **MỞ** | |
| Staging bán diện nhân vật Lan tại `AtCounter`, ánh sáng hắt ngược ấm từ đèn bàn, khử hiệu ứng uncanny valley ở khoảng cách hội thoại | `loc02-lan-B.png`, `m2c-lan-closeup-B.png`, phiên chạy trực tiếp tại `AP02` | **MỞ** | |

- **Người duyệt (`TASK026`):** *(Chờ chuyên gia kiến trúc / mỹ thuật)* · **Kết quả:** **PENDING**

---

## 2. `TASK027` — Lịch sử PCCC, Giữ nhịp Canon & Diễn xuất Thoại Tiếng Việt

| Hạng mục cần thẩm định | Hiện vật kiểm tra trên build | Trạng thái | Ghi chú chuyên gia |
| --- | --- | --- | --- |
| Độ thuyết phục & tôn trọng nạn nhân vụ cháy 18/11/2005 (13 nạn nhân, `CAN02`/`CAN03`, mốc `03:17` chỉ là mất liên lạc `CAN05`, không suy diễn nguyên nhân cực đoan) | `E10a` (`Doc_BayLetter.png`), `CH01-D19`, `OBS_CLOCK_0317` | **MỞ** | |
| Nhịp sáu câu mở rộng của cuộc gặp Lan, độ rõ của VO và chuyển ý sang đồng hồ đứng kim sau lời Minh về đám tang | `Dialogue.json`, phiên gặp Lan tại Quầy tiếp nhận (`AP02`) | **MỞ** | |
| Giọng điệu nội tâm tự vấn của Minh trong phản hồi `F101` (`DeductionFramework.cs` / `OpenF101Notes`) — tự nhiên, không mang giọng kiểm toán máy móc | `F101` bước đối chiếu & thử chọn kết luận sai | **MỞ** | |
| Khóa canon tiết lộ (`V22-Q03`): `CH01-D16b` và `CH04-D06` chỉ xuất hiện khi đủ điều kiện CH04. Thẻ `OBS101B` và vết hằn `03g17 · CHƯA CHỐT SỔ` giữ mã C01, tên Bé An và Khu C cho các cảnh tiết lộ sau (`DEC-CH02`, `LOCK-LOG06`). | Thẻ AS-05B trong soi 3D và bản chữ lớn, `Dialogue.json` | **MỞ** | |

- **Người duyệt (`TASK027`):** *(Chờ chuyên gia PCCC / biên kịch thoại tiếng Việt)* · **Kết quả:** **PENDING**

---

## 3. `TASK028` — Nghiệp vụ Y tế, Hồ sơ Bệnh án & Cân bằng Mix Âm thanh Cuối

| Hạng mục cần thẩm định | Hiện vật kiểm tra trên build | Trạng thái | Ghi chú chuyên gia |
| --- | --- | --- | --- |
| Tính hợp lý nghiệp vụ của hệ thống mẫu biểu 2005: `F101` (`AS-05`), thẻ đầu giường `AS-05B` (`ObsCard`), phiếu chuyển `OBS101C`, phiếu thuốc `OBS101D`, quy chế ca đêm `OBS_SHIFT_LOG`, sổ phân ca `OBS_RECEPTION_DRAWER` | Soi 3D 2 mặt + bản chữ lớn tất cả tài liệu CH01 | **MỞ** | |
| Tương tác công bằng về nhận thức: `Unverified` (Để ngỏ) luôn hợp lệ và tôn trọng nguyên tắc cẩn trọng y tế; nhánh giải mã ẩn `03:17` (`CH01-EX01`) và vết hằn bút bi nghiêng đón sáng bổ trợ điều tra nhưng không khóa tiến trình | Chơi trực tiếp cả 3 nhánh F101 + đối chiếu `Ghi chú ngoại lệ · Mốc 03:17` | **MỞ** | |
| Cân bằng mix âm thanh không gian, độ tiết chế của `DeferredMicroShift` (rèm tự khép, đèn tuýp tắt 0,6s, ghế kéo `ChairScrape.wav`, tiếng gõ sau vách `PartitionKnock.wav` ở nhánh `Affirmed`) & mức an toàn trợ năng (`reduceMotion`, `monoAudio`, chú thích có hướng) | Chơi bằng tai nghe stereo + kiểm tra cài đặt `reduceMotion` & `monoAudio` | **MỞ** | |

- **Người duyệt (`TASK028`):** *(Chờ chuyên gia y tế / đạo diễn âm thanh)* · **Kết quả:** **PENDING**
