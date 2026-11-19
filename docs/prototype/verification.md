# Bản thử phòng 101 · Kết quả M0 và M1

19/11/2026 · `M1-0.1.0-1dc1a29558bb` · Unity 6.3 LTS `6000.3.28f1` · URP 17.3 · Windows x64

[Điều khiển và dự án Unity](../../README.md) · [Production Database](../07_Production_Database.xlsx)

Gói bản dựng đã kiểm nằm tại `output/CaTrucCuoiCung-M1-Windows.zip` trên máy phát triển. Giải nén cả gói và mở `CaTrucCuoiCung.exe`. Gói chứa tệp chạy, dữ liệu Unity, hướng dẫn điều khiển và giấy phép tài nguyên. SHA-256 của ZIP là `96f1a4b2e59a56a3ec9884f1d0ce66326c956674a373a99c259e5156ef02eb5a`.

M0 hoàn thành. M1 qua kiểm thử kỹ thuật tích hợp và đang nghiệm thu hình ảnh, hiệu năng. M2 chưa khởi động. Phạm vi build đi từ cổng phụ, quầy trực và phòng 101 tới kho giường, rồi kết thúc ở mốc cấp cứu. B dùng diện mạo phòng đang hoạt động năm 2005 để biểu hiện nội dung hồ sơ, còn R là hiện trường năm 2012.

## Tư liệu, mặt cắt và engine

| Task | Kết quả | Bằng chứng |
| --- | --- | --- |
| TASK022 | Duyệt mặt cắt ở cấp bản nháp. Nền trệt ±0,00 m, B1 −3,40 m, sân sau giả định −1,90 m. Khu B có tuyến thoát về sân độc lập với B1. Cửa sau cũ của khu C bịt năm 2004. | [Mặt cắt và sơ đồ](layout.md) |
| TASK023 | Dùng bản vẽ JICA, ảnh có tác giả và tư liệu cùng giai đoạn cho công năng, giao thông cáng, vật liệu và dấu sử dụng. Phòng 101 là thiết kế hư cấu 8 × 6 m, trần 3,60 m, cửa 1,20 m, hành lang 3,00 m. Tư liệu ảnh bệnh viện nhỏ miền Nam trước 2005 còn ghi U. | [Register nghiên cứu V/I/F/U](research.md) · [Bảng mẫu 2D](loc03-reference.png) |
| TASK029 | Chốt Unity 6.3 LTS và URP sau khi đo cùng cảnh với HDRP. P95 render đồng bộ ở 1080p là 14,08 ms với URP và 36,40 ms với HDRP trên Intel Arc tích hợp. | [Cách đo và DEC-V2-04](engine.md) |
| TASK002 và TASK024 | Trình điều khiển đi AP00–05 qua R và ba bố cục B. Greybox kiểm riêng từng cửa cấp cứu, đường hồi và proxy cáng 0,72 m qua ngưỡng phòng 1,20 m. | [Trích log greybox](evidence/greybox.txt) |

## Gameplay đã chạy trên tệp Windows

| Task hoặc ca kiểm | Hiện tượng quan sát |
| --- | --- |
| TASK003 | Lượt chơi cửa sổ đã qua cổng phụ tới tuyến bàn trực. Lượt kiểm tự động dùng cùng trình điều khiển va chạm, các điểm neo và logic tương tác để hoàn tất ba đường. |
| TASK004-A/C · CH02-Q01/Q02, LOG06-Q01 | E12 ghi dấu vị trí kê. E11 yêu cầu tranh, vòng và phiếu chuyển. Thiếu phiếu chuyển thì E11 và G02 vẫn khóa. Đủ đối chiếu mới cấp C01 và mở đường cấp cứu. |
| TASK004-B · CH01-Q01–03 | A xác nhận 0 tăng D và mở G01. B để ngỏ giữ D bằng 0. C đòi cặp OBS101A/B và thao tác chọn cặp. Lịch sử A→B→C giữ lần ký đầu. Ký lặp C 10 lần giữ revision và trạng thái. |
| TASK005 · TASK025-B | Từ chối 30 lần đổi khi đứng ở ngưỡng. Thành công 30 lần đổi tại điểm neo. Collider chèn vào đích B gây rollback, khôi phục lớp R và thứ tự commit. |
| TASK006 · T08 | Tự lưu và lưu tay khôi phục lớp, F101 và vật chứng. Tải lại 10 lần qua chuỗi stress giữ dữ liệu. Lỗi I/O giả lập ngay sau flush giữ snapshot cũ. Hỏng file chính phục hồi từ backup hợp lệ. |
| T06 và V22-Q03 | Kiểm Tab chuyển nút, Enter chọn, Esc đóng bảng, cỡ chữ 130% và lưu cách phím thay thế. Cả ba nhánh chạy khi tắt âm thanh và giảm chuyển động. Dữ liệu CH01/CH02 chặn lời thoại dành cho CH04. |

Tệp chạy kết thúc thành công trong các ca tích hợp trên. Một số ca cấp chương có phạm vi rộng hơn, gồm sửa phiếu trước collapse, thoát tiến trình ở từng beat, cạn pin trong lượt chơi và xác minh cue VO. Production Database ghi từng phần đã thử theo ID. Playtest với người mới và review chuyên gia TASK026–028 nằm ở các cổng đã định trong Roadmap.

[TASK025 và log kiểm thử](evidence/gameplay.txt) · [Trích log Windows](evidence/windows-player.txt)

## Hình ảnh và âm thanh

Phòng 101 và đoạn hành lang qua cửa dùng làm vùng mẫu M1. Giường, tủ và xe cáng được dựng theo mét. Gạch bông, sơn lau, vết thấm, gỉ và đồ dùng giữ liên hệ với trục cửa và vị trí kê giường. R có mưa, ánh tản qua cửa sổ và phản xạ sàn ẩm. B giữ cùng hình học với đèn ống, quạt và rèm chuyển động. Mưa, giọt nước, quạt, tiếng dép và âm tương tác phát theo vị trí. Tùy chọn tắt âm và giảm chuyển động giữ đường đọc vật chứng. Phần mở đầu dùng văn bản thoại.

Đã rà lại độ đọc biển, cửa kho và vị trí vật chứng từ ảnh chụp trong bản Windows. Vùng mẫu còn cần duyệt độ chân thực của ánh sáng, bề mặt và đạo cụ ở khoảng cách chơi trước art lock.

[Ảnh cùng góc nhìn R/B](evidence/room-101-r-b.jpg) · [Hành lang](evidence/corridor-r.png) · [Nguồn tài nguyên và quyền](../../Prototype/ThirdPartyNotices.md)

## Hiệu năng và điều kiện nghiệm thu

| Phép đo | Kết quả |
| --- | --- |
| Unity ProfilerRecorder, bản Windows cuối | 420 khung hình trong tuyến camera LOC03. Đầu ra 1920 × 1080, URP render scale 0,88. P95 render vào texture và đồng bộ đọc một pixel là 17,63 ms. P95 vòng khung hình của lượt đo là 21,62 ms. Đỉnh 593 draw calls và 106,41 MiB Unity cấp phát. GPU frame time riêng chưa có số hợp lệ. |
| Frame Debugger, cảnh tham chiếu trước bản đóng gói | 53 sự kiện tại góc phòng 101, có bóng đèn chính và đèn bổ sung, depth/normal prepass và color LUT. Dùng để rà các pass của cảnh. |
| PresentMon, lượt cửa sổ cấu hình 1280 × 720 | 4.527 lần gọi Present trong 18 giây tại đoạn đầu. P95 khoảng giữa hai lần Present là 5,54 ms và P95 khoảng giữa các lần cập nhật được hiển thị là 33,36 ms trong chế độ Composed Flip. Lượt này chỉ đo đoạn đầu. |

Ngân sách 60 fps là 16,67 ms. Phép đo 1080p có đồng bộ vượt ngân sách. Cần số đo cửa sổ 1080p trọn tuyến và duyệt vùng mẫu để đóng M1. Lượt thao tác cửa sổ 1080p dừng khi nhận Esc.

[Unity ProfilerRecorder](evidence/profile.txt) · [Frame Debugger](evidence/frame-debugger.txt) · [Mẫu PresentMon](evidence/presents-720p.csv) · [Định nghĩa chỉ số PresentMon](https://github.com/GameTechDev/PresentMon/blob/main/README-ConsoleApplication.md)

## Đối chiếu bản đóng gói

Bản Windows được tạo bằng Unity BuildPipeline trong project URP đã dùng để thử engine. Các nguồn và cấu hình được chép từ `Prototype` để tránh lỗi treo ở bước BuildPlayerDataGenerator của lần build ban đầu. SHA-256 scene trùng giữa hai project. [Trích log build Windows thành công](evidence/windows-build.txt).

| Thành phần | SHA-256 |
| --- | --- |
| `AnSinh.unity` | `5ddf0c6d76a7893d2609b9a8e8e005afb4da26bc0774f075da6660d568121f33` |
| `Assembly-CSharp.dll` trong gói Windows | `1dc1a29558bba9f72a335d5feec078969fe62808521fa2f418e11d9d6ae9a42d` |
