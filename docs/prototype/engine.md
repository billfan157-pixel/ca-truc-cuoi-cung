# Chọn engine và render pipeline cho M1

19/11/2026 · TASK029 · Core 06 §3.1 · DEC-V2-04

**Chọn Unity 6.3 LTS 6000.3.28f1 và URP 17.3 cho prototype Windows x64.** Trên máy phát triển, URP có dư địa tốt hơn cho ánh sáng phòng 101, tài nguyên lớp R/B và các lần cập nhật NavMesh. Git chạy cục bộ ở root workspace. Project Unity nằm trong `Prototype`. [Vòng hỗ trợ Unity 6](https://unity.com/releases/unity-6/support) · [So sánh pipeline Unity 6.3](https://docs.unity.com/en-us/engine/6000.3/manual/render-pipelines/choose-a-render-pipeline/feature-comparison).

## Số đo cùng cảnh thử

Máy phát triển chạy Windows 11 x64, Core Ultra 7 155H, Intel Arc tích hợp, RAM 15.868 MiB, Direct3D 12. Hai template chính thức dùng chung cảnh thử 8 × 6 m, góc nhìn, lịch xuất hiện vách, model OBJ và đường camera. Cường độ dùng đơn vị của từng pipeline. Render vào texture 1920 × 1080, khởi động trước 150 khung hình, đo 420 khung hình. Mẫu thời gian render chờ GPU trả một pixel để xác nhận mỗi khung hình đã được vẽ. Chỉ số bộ nhớ là phần Unity cấp phát trong Editor. Giữ số đo cửa sổ bản chơi ở báo cáo T07 riêng.

| Chỉ số | URP | HDRP |
| --- | ---: | ---: |
| P95 render ngoài màn hình, gồm đọc lại một pixel | 14,08 ms | 36,40 ms |
| P95 vòng khung hình Editor | 17,51 ms | 41,91 ms |
| Draw Calls Count P95 | 65 | 96 |
| Bộ nhớ Unity cấp phát | 217,91 MiB | 409,18 MiB |
| Một lần tạo NavMesh cảnh thử | 6,80 ms | 25,05 ms |
| Lượt phát hiện điểm đáp bị chắn | 30/30 | 30/30 |
| Đọc snapshot và backup sau replace | Pass | Pass |

Nhiễu của máy và shader warm-up có thể làm đổi số tuyệt đối. NavMesh dùng cùng API ở cả hai dự án. Hai số đo NavMesh đơn lẻ phản ánh lượt chạy cụ thể. Cảnh URP được đóng gói và chạy độc lập trên Windows, khoảng 97,5 MiB. Lượt chạy cửa sổ ẩn kiểm va chạm và ghi/đọc snapshot thành công. Hiệu năng render sử dụng hai lượt đo ngoài màn hình ở bảng trên. [URP](evidence/urp-benchmark.txt) · [HDRP](evidence/hdrp-benchmark.txt) · [Trích log bản chơi thử](evidence/urp-player.txt).

## Ma trận công cụ

| Phương án | Kỹ năng và toolchain | Geometry, material, navigation và save | Windows, license và bằng chứng |
| --- | --- | --- | --- |
| Unity URP | C# và Editor sẵn sàng trong workspace. Blender 4.5 xuất FBX theo mét. | Lit PBR, Reflection Probe và Volume chỉnh màu. Scene thử nhập OBJ, đổi collider cục bộ, rebuild NavMesh và lưu snapshot nguyên tử. | Unity Personal đang được máy nhận. Có build Windows Mono đã chạy. Điều khoản và hạn mức theo trang Unity. |
| Unity HDRP | Cùng C#, FBX và scene tools. | Có thêm các tính năng ánh sáng thể tích/phản xạ. Cảnh đo đã nạp vật liệu HDRP và dùng cùng logic va chạm, NavMesh, save. | Cùng điều khoản Unity. Kết quả đo ở Editor. Build HDRP độc lập chưa được nghiệm thu. |
| Godot | GDScript hoặc C#, Godot editor và Forward+. Đánh giá theo công cụ được công bố. | Có PBR, navigation và file API. Cần tích hợp riêng logic R/B và cấu hình bake. | MIT. Không có số chạy tại máy này. [Renderers](https://docs.godotengine.org/en/stable/tutorials/rendering/renderers.html) · [License](https://godotengine.org/license/). |
| Unreal Engine | C++/Blueprint, trình nhập FBX và profiler riêng. Đánh giá theo tài liệu. | Có Lumen, NavMesh và SaveGame. Cần dựng cảnh và đo lại GPU với cấu hình cụ thể. | Điều khoản phát hành game quy định tại Epic. Không có số chạy tại máy này. [EULA](https://www.unrealengine.com/en-US/license) · [Lumen](https://dev.epicgames.com/documentation/en-us/unreal-engine/lumen-global-illumination-and-reflections-in-unreal-engine). |

Hai engine tham chiếu phục vụ ma trận. Phạm vi benchmark thực nghiệm là Unity URP và HDRP theo yêu cầu prototype. Giấy phép Unity hiện có được phát hiện qua cài đặt cục bộ. [Unity Editor Software Terms](https://unity.com/legal/editor-terms-of-service/software).

## Đường găng và cấu hình

1. TASK001 → TASK022 / TASK023 / TASK029. Chốt mặt cắt hư cấu, register nguồn và engine trước khi nhận bản chạy tích hợp.
2. TASK002-A/B/C và TASK024. Kiểm LOC01–04, AP01–04, bốn vị trí giường, cáng và đường về sổ trực.
3. TASK003 → TASK004-A/B/C → TASK005-A/B/C → TASK006-A/B/C. Dùng cùng state trong tương tác, đối chiếu, đổi lớp và lưu.
4. TASK025-A/B. Chạy cả ba quyết định, sửa phiếu, E11 thiếu phiếu chuyển, E12, đổi lớp, rollback, tải lại và lỗi ghi file.
5. Dùng phòng 101 và hành lang làm vùng mẫu. Đo riêng render, kiểm tư liệu chưa xác minh và giữ điều kiện playtest/review ở Roadmap M2.

Đích sơ bộ là PC Windows x64 1080p. 60 fps tương ứng 16,67 ms mỗi khung hình và tiếp tục là mục tiêu đo trên bản chơi. URP dùng PBR linear, đèn chính có bóng mềm, vùng phản xạ cục bộ, hiệu ứng mưa theo vùng, tấm tán xạ cửa sổ kiểm độ sâu và ba mesh đạo cụ gộp theo vật liệu. Bản dựng ghi thông số hiện có theo phép đo thực tế.
