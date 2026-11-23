# Mặt cắt và mặt bằng nháp · TASK022

[Mặt cắt](section.png) · [Mặt bằng tầng trệt](floor-plan.png) · [Dữ liệu kích thước](layout.json)

Core 03 Campus-v0 dùng [bản tọa độ khuôn viên](campus-layout-concept.json) để đặt seed lõi này vào sáu khối. Sảnh khám chính và mái xe cấp cứu thuộc các cánh riêng. Bảng tuyến dưới đây mô tả bản TASK022 của lõi. Dùng Core 03 §3.2–3.5 cho công năng và phương án nối ở quy mô khuôn viên. Ghi nhận kết quả trên build Campus theo TASK030-B–F.

Hệ trục dùng mét, X sang đông, Z sang bắc, Y lên cao. Phòng 101 rộng 8 m theo X và sâu 6 m theo Z. Cửa vào mở từ hành lang lòng 3 m, cửa kho ở đầu đối diện về phía giường 4. Giữ AP04 phía trong phòng, cách mặt cửa 0,92 m. Giường có đầu áp tường. Vị trí 4 hiện dưới dạng ổ cắm, dấu chân bánh và khoảng kê trống. B thêm mô-đun che hốc cục bộ theo F101. R dùng tủ di động trước lối kho cố định. CH02 nhận E12 qua dấu vật lý, kho trả đủ vòng C01, tranh và phiếu chuyển.

| Tuyến | Liên kết gốc | Nghiệm thu |
| --- | --- | --- |
| Công cộng và Khu B | Sân trước 0,00 → LOC02 → cầu thang chính → Khu B +4,00. Chiều trở về sân đi cùng trục. | Đồ thị kiểm Khu B ↔ sân sau khi loại cả LOC09 và B1. Vị trí tiếp cận của lối chính đã có trong mặt cắt; tầng trên nằm ngoài vùng chơi M1. |
| Cấp cứu và vận chuyển | Sân → mái đón một bên → LOC04. Tầng trệt có đường nối nội bộ từ sảnh qua hành lang và lối phụ. | Cửa cấp cứu hai nhánh 1,4 m, phòng 101 1,2 m. Cả hai nhánh cấp cứu giữ khóa trước G02 trong phạm vi M1. |
| Nhân viên và kỹ thuật | LOC04 → cầu thang kỹ thuật → LOC09 ở −3,40 → LOC11 → cửa chính có ô kính → LOC12. | Khi đứng LOC11 có thể đối chiếu dấu cửa sau từ phía hành lang. Sơ đồ thể hiện tuyến, cảnh B1 chưa mở ở M1. |
| Dịch vụ | Đường xe riêng phía sân sau −1,90 dẫn tới lối kỹ thuật; cửa dịch vụ của LOC10 có đệm độc lập. | LOC10 không nối qua khu C. Cao độ sân sau −1,90 là giả định dựng section; chênh 1,50 m với B1 thể hiện bằng ram/cầu thang ngắn ở nhánh cũ. |
| Cửa cũ | LOC12 → cửa đã bị bịt năm 2004 → đoạn dẫn ngắn tới sân sau. Cửa chính tiếp cận từ LOC11. | Trên bản vẽ chỉ cửa cũ có vệt khóa vữa. Sơ đồ tuyến chơi không có cạnh xuyên qua. |

Core 03 §2–3 và DEC-V2-01 xác định cao độ lõi và ranh giới khu C. Các tuyến kỹ thuật và sạch/bẩn là thiết kế chức năng hư cấu. Bản vẽ phục vụ duyệt blockout. Đối chiếu thực địa và chuyên gia TASK026–028 vẫn thuộc cổng trước art lock.

## Chi tiết hóa kiến trúc M0-R (Phòng 101 & Hành lang liền kề)

Giữ nguyên 100% tọa độ phòng, cửa, giường và pad trong `layout.json` (và toàn bộ ngưỡng kiểm `VerifyBlockout`: lối cáng 0,72 m qua cửa 1,20 m, capsule 0,35 m, khoảng giữa hai dãy giường 1,88 m, `AP04` tại `(6,52; 4,93)`). Bổ sung chiều sâu cấu tạo kiến trúc công trình bệnh viện Việt Nam 1987–2005:

- **Kết cấu nhịp BTCT & phân khu cải tạo:** Hành lang trục chính rộng 3,00 m chia 4 nhịp cột BTCT vát cạnh (tại `z = 1,00 / 5,25 / 9,50 / 13,75 m`) nối dầm ngang cao 360 mm và dầm dọc đỡ trần phòng 101 (`x = 9,77 m`). Lắp ốp bảo vệ góc inox tại cột dễ va quệt xe cáng và tay vịn gỗ + thanh chống va đập dọc tường tây hành lang ở cao độ 0,82 m.
- **Lớp hoàn thiện tường hai tầng:** Len chân tường đá mài bo cong cao 95 mm, dải sơn dầu xanh bệnh viện cao 1,15 m có đường chỉ phân cách 12 mm, phần trên sơn vôi/nước nhạt. Ở R (2012), hiện tượng thấm mao dẫn, muối hóa bong sơn và rêu mốc tập trung dọc chân tường bắc/đông và dưới bậu cửa sổ theo quy luật vật lý [R41].
- **Hệ cửa đi & cửa sổ thông gió tự nhiên [R39/R40]:**
  - `GATE-101` (1,20 × 2,22 m): khuôn gỗ ghép nẹp chỉ, cánh gỗ pano dưới dày + tấm inox đá chân, ô kính mờ quan sát phía trên, tay gạt inox, khóa, 3 bản lề trụ, tay co thủy lực và ô thoáng chớp kính phía trên khuôn cửa (tới cao độ 2,86 m). Cánh cửa xoay mượt theo bản lề và cho phép người chơi đóng/mở trực tiếp trong R/B.
  - 3 ô cửa sổ tường bắc phòng 101 (`z = 8,10 m`, diện tích lấy sáng 10,19 m² = 21,2% diện tích sàn 48 m² theo số đo prototype và đối chiếu R39): tường dày 200 mm tạo hốc cửa sâu có bậu đá mài nhô 35 mm, khung thép góc chia cánh, 6 lá chớp kính thông gió phía trên và lưới hoa sắt bảo vệ hàn hình học đổ bóng thực lên sàn và giường. Campus-v0 đặt sân sáng phía ngoài. Tính phù hợp thực tế chờ TASK026.
  - `GATE-STORE` (1,22 × 2,20 m): khuôn gỗ sâu, bậc ngưỡng mòn, biển `KHO GIƯỜNG` gắn vít nổi, bị tủ sắt kính 2 cánh di động (`1,36 × 0,48 × 2,06 m`) chắn phía trước trong R trước khi giải `E12` và lăn sang bên mượt mà sau khi đối chiếu `PZ01`.
- **Hệ kỹ thuật nổi & giao thông nội phòng:** Ống ghen điện chạy nổi có kẹp đỡ và hộp nối chia nhánh tới từng đèn T8 đôi, hộp công tắc/ổ cắm đôi có số `1–4` đầu giường (ổ số 4 giữ nguyên dấu vết tại hốc trống), loa phóng thanh hành lang gắn tường gần `GATE-101`, bảng kẹp `F101` gắn tường trong cửa, bình chữa cháy treo hành lang, ray rèm treo trần có móc trượt và rèm vải có thể kéo mở vật lý quanh hốc giường 4.
- **Đồng nhất hình học R/B:** Toàn bộ khung tường, cột, dầm, khuôn cửa, cửa sổ, quạt trần, máng đèn, ổ cắm và bao va chạm giường 1–3 giữ cùng vị trí giữa R (2012) và B (2005). B chỉ đổi vật liệu sạch hơn, ánh sáng đèn T8 vận hành, bật rèm `pendingBay` hoặc vách áp hồ sơ `deniedBay` theo trạng thái `F101`, và ẩn các lớp xuống cấp/vật chứng riêng của hiện trường 2012.
