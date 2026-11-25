# Errata áp dụng cho M2-FINAL

## EVT026 · Trình tự ứng cứu trong hồ sơ TASK027

**Trạng thái:** Người dùng đã duyệt cách diễn giải cho hồ sơ thẩm định TASK027. Giờ lực lượng cứu hỏa thực tế vào sân chờ chuyên gia thẩm định.

| Nguồn | Chi tiết cần đối chiếu |
| --- | --- |
| Core 02 §2, `EVT026` | Minh trở lên sân khi hiện trường đang chờ cứu hỏa. |
| Core 07 `Timeline!D27`, `EVT026` | Minh trở lên sân giữa lúc cứu hỏa đổ vào. |
| Core 07 `Dialogue!E104`, `CH07-D15` | Minh rụt tay, rút lên sân giữa tiếng còi. Sau chuyển thời gian, anh giao chìa lúc 04:15. |
| `E02` và Core 07 `Backlog!89`, `TASK027` | E02 xác lập mốc giao chìa. TASK027 cần thẩm định công tác ứng cứu cùng mặt cắt C và cơ chế khói. |

**Cách diễn đạt đã duyệt.** Khoảng 03:25, Minh rụt tay khỏi chùm chìa khóa, rời cửa Khu C và trở lên sân giữa tiếng còi cứu hỏa. Sau đoạn chuyển thời gian, lúc 04:15, Minh giao lại chùm chìa khóa tại bàn chỉ huy, khớp E02.

Xác minh với chuyên gia khoảng thời gian lực lượng có thể vào sân và mức độ chắc chắn của kết luận. Ghi nguồn, giả định và giới hạn trong [hồ sơ TASK027](specialist-review-dossiers.md#2-task027--historical-fire-safety-review). Phạm vi áp dụng là hồ sơ thẩm định. Bảy nguồn đóng băng, mốc 07:40, hành động và trách nhiệm của Minh, stable ID và Save Schema v7 tiếp tục theo bản Design Lock. Sau kết quả TASK027, trình phương án đồng bộ chính thức nếu cần. Errata này đã gỡ điểm chặn đối với runtime CH01–CH04.

## E03 · Bề mặt hiển thị danh tính C02

**Trạng thái:** Người dùng đã duyệt cách hiển thị trong Phase B ngày 25/11/2026.

Core 07 `Evidence Specs!G5` có phụ lục gấp kèm ghi C02 / Bà Lành. Core 04 CH03 B03–B04, `PZ03` và Core 07 `CH03-PU02` dùng lượt đọc sổ để xác nhận tồn tại, rồi dùng thư độc lập trong nhánh tùy chọn để xác thực danh tính.

Lượt đọc E03 bắt buộc trong runtime chỉ hiển thị mã và dấu vết. Giữ danh mục đầy đủ ở nguồn sản xuất. Người chơi gặp tên Bà Lành khi đi tuyến thư tùy chọn, còn Journal và vignette dùng trạng thái đã xác thực. G03 vẫn đọc sự tồn tại C02. C12 giữ danh tính chưa xác định.

## S1 · Trục cầu thang nội bộ trước G02

**Trạng thái:** Người dùng đã duyệt triển khai ngày 25/11/2026.

Theo Core 03 §8, `DEC-V331-01` và `V33-Q05`, trục S1 nối LOC02, LOC05 và LOC07 thông trong R. Core 07 `Chapter gates!C4` tiếp tục áp dụng cho thời điểm bắt đầu điều tra CH03. S1 cho phép di chuyển vật lý trong R trước G02. S3, B1, kích hoạt CH03, vật chứng và các sự kiện giữ điều kiện hiện hành. Cửa xả trực tiếp từ chiếu nghỉ ra sân là hạng mục riêng của `V33-Q05`.

### Implementation Clarifications · Cửa xả sân S1

**Phê duyệt M2-FINAL ngày 25/11/2026.** Tâm cửa ở mặt nam K-A, phía tây LOC02, tại `(-2.05, 0, -7.86)` theo trục Unity X/Y/Z. `docs/prototype/layout.json` khai báo khẩu độ `1.36 × 2.24 m` và chân vế thang tại `x=-2.90`. Bậc đầu lùi về phía tây để có chiếu nghỉ phẳng nối trực tiếp cửa với chân S1. Tâm trục thang, đầu vế tầng 2, hành lang lên LOC07 và đường vòng qua lễ tân W-Q02 dùng mốc hiện có. Nền cửa nối đến mép sân LOC01 tại `z=-8`.

Khung thép, ngưỡng phẳng và vách bao thuộc tile LOC02 để NavMesh thu được hình học lối ra. Trong R, cánh cửa mở về phía sân và đồ thanh lý cùng rào lưới chặn khẩu độ bằng collider. CH05 ở lớp M mở thông cánh cửa, thu hồi vật cản và cho phép tiếp cận qua chân S1. Hợp đồng này chỉ có phần trình bày hình học và tải vùng. Cửa khép trong B và khi kiểm bằng trạng thái M của CH03. G02 tiếp tục quyết định tiến trình, S3/B1, vật chứng và sự kiện dùng điều kiện hiện hành. Các ID canon và Save Schema v7 giữ nguyên.

`V33-Q05` kiểm đường thẳng qua ngưỡng bằng capsule và NavMesh trong R/M, thử tiến bằng controller từ chiếu nghỉ trong R và đi hai chiều qua LOC05/LOC07 trước G02. `W-Q02` kiểm đường qua lễ tân độc lập B1, còn `WORLD-Q04` kiểm phân vùng theo cao độ. Module QA lưu hai góc ảnh từ chiếu nghỉ và sân khi chạy trên Player.
