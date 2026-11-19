# Ca trực cuối cùng

Prototype Unity khám phá phòng 101 của bệnh viện An Sinh. Bản thử đi từ cổng phụ qua lễ tân, hành lang và phòng bệnh đến kho giường và cấp cứu. F101 có ba cách xử lý. Người chơi có thể quay lại sửa phiếu, đổi giữa R và B tại điểm neo, đối chiếu E11/E12 và tải lại ca đã lưu.

**Bản M1 cho Windows x64** · Bản dựng `M1-0.1.0-1dc1a29558bb` đã qua kiểm thử kỹ thuật tích hợp. M1 đang nghiệm thu hình ảnh và hiệu năng. Gói đã kiểm thử nằm trong `output/CaTrucCuoiCung-M1-Windows.zip` tại máy phát triển. Bên dưới có cách mở project Unity và build Windows từ mã nguồn.

## Điều khiển

| Phím | Thao tác |
| --- | --- |
| WASD và chuột | Di chuyển và nhìn |
| E | Tương tác với vật đang nhìn |
| Tab | Mở sổ trực. Trong trang đọc, Tab chuyển nút và Enter chọn |
| Q tại vạch đồng phòng 101 | Mở điểm neo AN02 trong CH02 |
| F | Đèn pin |
| Shift và Ctrl | Chạy và cúi |
| Esc | Khép trang đọc hoặc mở menu |

Trong Cài đặt có cách phím mũi tên, K tương tác, J mở sổ và L bật đèn pin, cùng tùy chọn tăng chữ và giảm chuyển động. Bản tự lưu ghi sau các mốc chính. Lưu tay dùng ở bàn trực trong lễ tân.

## Nguồn và cách dựng

Mở thư mục `Prototype` với Unity 6.3 LTS 6000.3.28f1. Scene chính nằm tại `Assets/CTCC/Scenes/AnSinh.unity`. Menu **Ca Trực Cuối Cùng** có lệnh dựng mặt bằng, dựng lại cảnh chi tiết và build Windows.

- [Nghiên cứu An Sinh](docs/prototype/research.md), [mặt cắt và mặt bằng](docs/prototype/layout.md) và [bảng vật liệu](docs/prototype/loc03-reference.png).
- [Chọn Unity và URP](docs/prototype/engine.md) và [Bằng chứng nghiệm thu](docs/prototype/verification.md).
- [Nguồn tài nguyên của build](Prototype/ThirdPartyNotices.md) và [Production Database v2.2](docs/07_Production_Database.xlsx).
