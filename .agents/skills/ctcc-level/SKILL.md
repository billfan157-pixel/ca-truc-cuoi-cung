---
name: ctcc-level
description: >-
  Thiết kế và triển khai không gian chơi Ca trực cuối cùng. Dùng cho mặt cắt TASK022, bệnh viện An Sinh, LOC01–12, topology, greybox, layer layout, safe pad/checkpoint, beat CH01–08, route hồi phục và dàn dựng scare/chase.
---

# Level và không gian An Sinh

## Nguồn cho task

- Dùng §2–4, §6 và Hình 1 cho hình học vật lý, cao độ seed, tuyến công cộng/kỹ thuật và continuity. [Core 03](../../../docs/03_World_Architecture_Bible.docx)
- Dùng §1 cho adjacency, luồng cửa, chốt chương, metrics chơi và điểm dàn cảnh. Mở heading chương và hình kết nối CH01 theo phạm vi cần làm. [Core 04](../../../docs/04_Level_Chapter_Design.docx)
- Với CH01, đọc trực tiếp `CH01 / Walkthrough B01–B08` trong [Core 04](../../../docs/04_Level_Chapter_Design.docx) (nguồn walkthrough duy nhất từ v3.6.2) cho hộp thư ngoài cổng `B01`, cổng phụ, điểm nhìn kính, `STAFF`, `DOOR-101`, `AN01/AN02`, seam `WR05` sau `T09` và handoff.
- Tra `07/Locations`, `Chapter gates`, `Events`, `QA` và `Assets` theo LOC, CH, beat và asset ID. Ví dụ `python docs/agent/project_docs.py doc 04 --section "CH02 /"`.

## Dựng theo tuyến

1. Gắn cho beat mục tiêu người chơi, hành động, thông tin mới, phản hồi, trigger, điều kiện, điểm save và fallback. Định danh cảnh theo CH/beat cùng ID trong Events; chốt chuyển chương đọc state đã commit.
2. Vẽ tuyến tiến, tuyến quay lại lấy evidence và vùng an toàn theo từng layer. Dùng một bệnh viện liên thông: R là gốc vật lý, B đổi mô-đun cục bộ quanh AN, M là cảnh giới hạn. Giữ landmark, cột và trục nhìn chung để người chơi nhận ra cùng một nơi. Dùng Core 03 cho seed vật lý nếu số đo trong phần cũ của Core 04 khác nhau.
3. Dựng greybox trước asset nặng. Gắn LOC, cửa, pad, CP, vật chứng, navigation và clearance. Sau khi có engine, kiểm capsule/xe đẩy, cửa và hốc giường; đi được từ mỗi CP tới mục tiêu và safe zone với gate/layer tương ứng. Dùng kiểm đồ thị cho kết nối và đi thực tế trong engine cho collider, tầm nhìn, navmesh, thời gian đường chạy.
4. Dàn nhịp từ cue báo trước đến căng thẳng và khoảng lặng. CH01 giữ T02 sau SMS/chìa, STAFF tùy chọn, lối hồi từ 101 về AN01 và seam cổng một lượt ở AP01. T03/T08/T10 cần thao tác tại sổ trực vật lý. CH02 đi được hai lối cameo LOC04 rồi hội tụ an toàn. Cảnh đọc có safe volume. Cue quan trọng phải đọc được khi mute/giảm chuyển động. Dùng `ctcc-gameplay` khi đổi gate/pad transaction và `ctcc-narrative` khi đổi trình tự thông tin.

## Điểm cần giữ khi sửa layout

- LOC03 có chỗ cho bốn vị trí vật lý, dấu bánh và hốc giường 4. Trong B, trạng thái F101 chi phối che/mở cục bộ. Đặt dấu E12 ở phòng 101 và giữ lối tới các phần E11 ở R qua mọi nhánh F101. Giữ LOC04 khóa đúng beat ở CH01 và thử hồi về sổ trực.
- Trục chính cho khu B ra sân độc lập với B1. Lối bắt buộc dùng cầu thang, thang máy là shortcut tùy chọn. B1 dùng tuyến kỹ thuật LOC04/09; cửa chính khu C nối LOC11→12, cửa sau cũ đã bịt, cửa dịch vụ LOC10 phục vụ nhà xác.
- Dấu vữa E05b nhìn từ hành lang ngoài LOC11 trước G06; phía boss/AN07 cần G06. Dựa vào hai mốc E05/E05b và đường vòng R sẵn có, kể cả mất điện hoặc D cao. Ở ký ức CH07, bố cục giữ Lan trong C và Minh ngoài cửa có ô kính.

Bàn giao sơ đồ hoặc ảnh top-down theo stage, cao độ/kích thước đã đo hoặc ghi seed, tuyến và gate, bảng pad/checkpoint và evidence theo layer. Đối chiếu LD01–08 từ Core 04, W-Q01–07 từ Core 03 và QA theo chương. Ghi dấu vết chạy route thực tế khi có build.
