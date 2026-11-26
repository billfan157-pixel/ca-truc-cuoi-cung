# Mỹ thuật Chương 01 — Người trở về

## Phạm vi

Đợt nâng cấp dựa trên Campus-v1 và hướng dẫn mỹ thuật của Core 05 v3.3.1. Các điểm chính là cổng phụ và sân trước LOC01, quầy tiếp nhận LOC02, hành lang STAFF, Phòng 101 và sân sáng LOC03. Dàn dựng B03 theo [walkthrough B01–B08 (lưu trữ)](../archive/ch01-history/walkthrough-gameplay-B01-B08-investigation-v1.md) và DEC-CH01R-04 (mốc trước Core v3.6.2). Lan ghi sổ phía sau quầy, ngẩng lên khi Minh đến gần và tiếp tục cảnh thoại khi người chơi chủ động tương tác.

## Hạng mục đã đưa vào bản dựng

| Hạng mục | Thay đổi tại điểm người chơi quan sát |
| --- | --- |
| Kiến trúc và đạo cụ | Ống máng có đai bắt, hai ô chớp thay thế giữa các nhịp cũ, ghế trực có gác chân, kệ chia hồ sơ, sổ mở và bộ chậu men cùng vật dụng chăm sóc. Ghế và sổ đặt sau quầy, bộ chậu và chai nằm trên tủ đầu giường. |
| Vật liệu | Vệt thấm theo ống và chân tường, vết mài theo đường bánh xe, vecni mòn quanh khe trao giấy và men sứt quanh lỗ bắt vít. Kính R tích vệt lau ở cạnh, kính B sạch ở vùng trao đổi. Khay inox xước phản xạ dịu hơn. Mặt giấy có thớ vi mô, mép cắt và đèn soi xiên riêng cho vết hằn. |
| Ánh sáng | R dùng ánh hắt từ sân và cửa sổ để đọc bậc, lanh-tô, giường và cửa kho. B dùng ánh T8 đều, đèn bàn ấm cục bộ và ít lóa trên kính quầy. URP bake riêng GI của R và B với các điểm probe dày hơn ở mái đón và chỗ ngồi của Lan. Các Reflection Probe theo vùng tiếp tục phản ánh lớp hiện tại. |
| Mưa và chuyển động | Vệt mưa ngắn, giọt từ mái có tiếng rơi tại chỗ, màng nước cục bộ và gợn trên các vùng trũng. Lá và rèm chuyển động nhẹ theo không gian, giảm trong chế độ giảm chuyển động. |
| Lan | Hiệu chỉnh mức phản xạ mắt, độ mềm da, viền tóc và độ nhám áo blouse. Ba clip ngồi mô tả viết sổ, lắng nghe và chỉ về BOARD. Bút theo thao tác và nằm trên sổ khi Lan ngẩng lên. Máy quay ở phía Minh, giữ lịch và đồng hồ làm hậu cảnh. |

Sáu mẫu mesh và hướng dẫn tái tạo nằm trong [bộ dựng Blender](../../tools/blender_assets/build_ch01_art.py). [Bộ dựng PBR](../../tools/bake_ch01_art.py) tạo texture decal 512 và bản đồ 1K dùng cho asphalt và giấy. [Hồ sơ nguồn](../../Prototype/ThirdPartyNotices.md) ghi nguồn tự tạo và những vật liệu gốc dùng lại. Cảnh Lan dùng cùng rig, giọng và kho khẩu hình đã tích hợp.

## Phạm vi nguồn và tiêu chí kiểm

| Ca | Bằng chứng cần xem |
| --- | --- |
| TASK007-A · CH01-WT-Q01/Q02 | Lan ở đúng phía quầy, bắt đầu thoại khi người chơi tương tác, khôi phục FPS khi skip, transcript đủ, BOARD và đồng hồ cần thao tác riêng. |
| TASK007-B/C · CH01-Q04–Q14 | Chạy ba nhánh F101, cầm và soi giấy hai mặt, giữ đường đọc chữ lớn và phụ đề, quay về R và qua CH02. |
| CHAR-Q01/Q04–Q08 | Hợp đồng clip, khẩu hình, nhận mặt và LOD qua cảnh quay đã dựng. |
| AR01–03 · WORLD-Q05 · TASK026–028 | So sánh ảnh cùng góc, kiểm độ đọc dấu vết và nguồn quyền. Nghiệm thu niên đại và công năng cần review chuyên môn. |
| Core 06 §04 · T07 · WORLD-Q06 | Đo Medium 1920 × 1080 trên iGPU, kiểm chi phí năm thành phần môi trường và thời gian khung hình của tuyến. |

## Kiểm chứng trên Windows

- Bản Windows x64 Release dùng Unity 6000.3.28f1, URP 17.3, dung lượng 983.595.332 byte. [Định danh Assembly và scene](../../output/ch01-art-production/windows/build-identity.txt).
- Tạo cảnh additive, bake thành công 399 Light Probe ở mỗi lớp R/B, tính occlusion cho bootstrap và tám vùng. Sau bake, tinh chỉnh ghế, tư thế, khung máy và vật liệu khay tại LOC02, rồi dựng lại player và kiểm trên đúng bản bàn giao. [Log bake](../../output/ch01-art-production/gen-final/generate-scenes.log) · [Tinh chỉnh LOC02](../../output/ch01-art-production/gen-final/seat-iteration.log).
- Module QA trên Intel Arc, D3D12, Medium 1920 × 1080 đạt PASS. Log ghi nhận ba nhánh F101, 30 lượt đổi lớp được chấp nhận, 30 lượt bị chặn, 10 lượt nạp lại, chế độ đọc lớn, tắt âm và giảm chuyển động. Ca nhân vật kiểm ba LOD và 150 lượt cập nhật khẩu hình trong lúc phát thoại. Cảnh B03 được chụp trong game ở tư thế viết sổ và ở máy quay đoạn thoại. [Module QA](../../output/ch01-art-production/module-tested/gameplay.txt) · [Lan B03](../../output/ch01-art-production/ch01-lan-b03.jpg).
- Input QA đạt PASS cho ba nhánh F101 và hai lối thoát CH02. Tuyến Bác bỏ đi bằng bàn phím và chuột từ AP00 qua B01–B08 đến CH02. Cửa Phòng 101 giữ đúng độ hé khi tải hoặc dỡ vùng, sau đó mở được từ hành lang STAFF. Bài thử 100 lượt soi hai mặt và soi xiên kết thúc với 0 mesh và 0 material giấy còn sống. [Input QA](../../output/ch01-art-production/input-tested/gameplay.txt) · [Cảnh Lan từ tuyến chơi](../../output/ch01-art-production/input-tested/walkthrough-lan-cinematic.png).
- Ảnh chụp khi cầm E10a, soi xiên AS-05B và ghi F101 cho thấy vật liệu ở khoảng cách thao tác. [E10a](../../output/ch01-art-production/input-tested/walkthrough-e10a-letter.png) · [AS-05B](../../output/ch01-art-production/module-tested/ch01r-raking-obscard-back.png) · [F101](../../output/ch01-art-production/input-tested/f101-signed-100.png).
- Tỷ lệ điểm ảnh có luma hiển thị dưới 5% giảm từ khoảng 70% xuống 45% ở góc Phòng 101 lớp R và từ 70% xuống 62% ở cổng phụ. Chỉ số lấy từ ảnh cùng góc đã quy về 625 × 352, dùng để so mức mất chi tiết trong vùng tối. Qua kính quầy B có thể thấy vách gỗ, kệ hồ sơ và vùng bàn dưới đèn. [Đối chiếu trước và sau](../../output/ch01-art-production/ch01-truoc-sau.jpg) · [Số đo ảnh](../../output/ch01-art-production/luma-comparison.json).

## Hiệu năng Intel Arc iGPU

Lượt đối chứng dùng Windows x64 Release, Core Ultra 7 155H và Intel Arc iGPU, D3D12, Medium 1920 × 1080, render scale 0,88 và VSync tắt. Mỗi hiệu ứng được đo cùng góc camera theo thứ tự bật, tắt, tắt, bật. Mỗi đoạn có 0,9 giây ổn định và 2,4 giây lấy mẫu. Bảng dùng 3.537 khung có focus từ bản mỹ thuật trước bản sửa trạng thái cửa khi tải vùng. [Số đo từng khung](../../output/ch01-art-production/benchmark-weather/frames.csv) · [Thống kê GPU, CPU và thời gian khung](../../output/ch01-art-production/benchmark-weather/stage-summary.json).

| Hiệu ứng | GPU trung vị bật / tắt (ms) | GPU P95 bật / tắt (ms) |
| --- | ---: | ---: |
| Mưa sân trước | 13,03 / 13,09 | 13,53 / 13,54 |
| Giọt từ mái đón | 13,04 / 13,04 | 13,47 / 13,62 |
| Gợn nước ở sân | 13,13 / 13,05 | 13,67 / 13,69 |
| Tán lá, gồm chi phí vẽ | 11,86 / 11,28 | 12,37 / 11,92 |
| Chuyển động rèm ở góc Phòng 101 | 13,48 / 13,25 | 15,50 / 13,82 |

Chênh lệch nhỏ ở mưa, giọt mái và gợn nước nằm trong dao động lấy mẫu. Hàng rèm dùng chế độ giảm chuyển động tại góc Phòng 101. P95 ở các góc cố định dùng để đánh giá mỗi thành phần môi trường.

Benchmark trên bản bàn giao đi hết tuyến CH01 đến CH02. Trong đoạn CH01, Unity xác nhận focus ở 128/2.559 khung đo được. Cổng 60 fps toàn tuyến còn mở. [Kết quả chạy tuyến](../../output/ch01-art-production/route-tested/result.txt) · [Số khung có focus](../../output/ch01-art-production/route-tested/stage-summary.json).

## Duyệt mỹ thuật

Playtest người mới theo TASK010 và review niên đại, tạo hình, công năng theo TASK026–028 vẫn còn mở. Cảnh Lan đã qua các kiểm tra kỹ thuật về tư thế, khung máy, khẩu hình và chuyển trạng thái sau khi bỏ qua thoại. Biểu cảm trong cả cuộc thoại cần được đánh giá khi duyệt mỹ thuật.
