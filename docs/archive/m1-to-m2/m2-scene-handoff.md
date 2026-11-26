# Ranh giới scene cho M2

M1-P giữ nguyên `AnSinh.unity`, mốc AP00–AP05, `CH01/CH02`, layout và nguồn Blender. Thay đổi chuẩn bị cho tải additive nằm ở `HospitalScene`. NavMesh lấy collider dưới root sở hữu vùng, dùng bounds cục bộ có thể serialize và giữ cache 8 cấu hình đã xác thực. Cache so nguồn, shape, area, transform và kích thước theo từng collider. Nguồn đổi thì dựng lại. Khi vùng bị hủy, gỡ `NavMeshDataInstance` và giải phóng dữ liệu đã cache. Cách này đã chạy trong scene M1, với một chủ sở hữu navigation.

Khi mở M2, dùng một scene bootstrap cho vòng đời `HospitalGame`, FPS camera/AudioListener, UI, `RealityTransaction` và hai slot save. Giữ phần hình học liên tục, R/B, rig tương tác, probe, audio zone và anchor của từng vùng dưới root riêng. Dùng ID `LOC`, `AP`, `CP`, gate và evidence từ Core để đăng ký vùng. Giữ vị trí thế giới của các pad hiện có. Một scene additive chỉ đăng ký phần vùng của nó và báo đã sẵn sàng qua bootstrap. Khi chưa có đăng ký này, `HospitalGame.StartGame` hiện vẫn khởi tạo từ `HospitalScene` đầu tiên của M1.

Trình tự nối vùng dự kiến giữ giao dịch Core 06 v2.2.

1. Tải scene vào nền và đăng ký các root/ID. Giữ vùng nguồn cùng collider và bản lưu hiện tại trong thời gian tải.
2. Đồng bộ lớp và gate từ snapshot đã commit. Xác nhận ID đích, capsule clearance, NavMesh thuộc vùng đích và đường nối cửa. Hold tương tác và AI tại điểm tiếp giáp đến khi đích hợp lệ. Đặt dữ liệu NavMesh prebuilt theo cấu hình ổn định. Dùng build nền hoặc obstacle carving cho nguồn thật sự động.
3. Publish qua `RealityTransaction`, sau đó đặt player, cập nhật render/audio và cho phép unload vùng nguồn. Khi preflight hoặc save lỗi, giữ vùng nguồn, snapshot và vị trí hợp lệ.
4. Kiểm ID trùng, load/unload lặp, retry sau mất file, thứ tự scene và mẫu R/B ở ranh giới. Dùng mẫu player qua cả hai cửa LOC04, quay lại AP04, lưu/tải sau G01/G02 và profile tải vùng 1080p. Kiểm culling qua cửa mở và lỗ nhìn trước khi bake nhiều scene cùng nhau.

Scene additive, AI di chuyển qua seam, dữ liệu occlusion đa scene và tải bất đồng bộ thuộc nghiệm thu M2. M1-P mới kiểm vòng đời cache và đường hồi phục trong scene hiện tại.
