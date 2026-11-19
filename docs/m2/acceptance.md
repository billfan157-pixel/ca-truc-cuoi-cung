# Hồ sơ ứng viên M2-A

Ứng viên dành cho kiểm thử và duyệt M2 trên Windows x64. Baseline là `ee8f4868328673fde54678e1fa1d35b66680bcfd`. Nhánh `codex/m2-a-production-readiness` giữ phạm vi CH01 rút gọn và đầu CH02 qua LOC01–04.

Unity 6.3 LTS `6000.3.28f1`, URP 17.3, Medium render scale 0,88. Build từ cây Prototype sạch tại `f697b6ac4d1e4d8a004fe2015e3cc7617c9a41c0`. [Nhận dạng build và SHA-256 assembly/scene](../../output/m2a/release/build-identity.txt).

- [Gói Windows x64](../../output/m2a/CaTrucCuoiCung-M2A-Windows.zip) và [SHA-256](../../output/m2a/SHA256SUMS.txt). Giải nén cả gói trước khi mở `CaTrucCuoiCung.exe`.
- [Video liền mạch từ cửa sổ game](../../output/m2a/gameplay/gameplay.mp4) ghi tuyến tự chạy từ đầu tới Cấp cứu trên build release. Tệp ghi hình không kèm âm thanh.
- [So sánh ảnh Unity trước và sau](../../output/m2a/m2a-before-after.jpg) và [bảng render Blender](../../output/m2a/m2a-blender-inspection.jpg). Bảy nguồn Blender hiện có được mở và render trực tiếp. Nguồn Lan chỉnh sửa được và FBX nằm chung revision với scene.
- Các trích đoạn action xuất trực tiếp từ Blender gồm [Lan](../../output/m2a/blender/animation/Lan.mp4), [Bé An](../../output/m2a/blender/animation/AnApparition.mp4) và [Kẻ Giữ Cửa](../../output/m2a/blender/animation/Gatekeeper.mp4). Đây là ảnh động rig tại studio. Hiệu ứng mờ dần của Bé An chạy trong Unity.

## Thay đổi và kiểm chứng

| Hạng mục | Thay đổi | Bằng chứng |
| --- | --- | --- |
| F101, E11/E12 và G02 | Giữ ba nhánh, lịch sử quyết định, khả năng sửa và nguồn vật chứng R. Chuyển ghi durable của các mốc đường chơi sang worker, giữ xác nhận vật chứng sau khi lưu. | TASK025-A, CH01-Q01–Q03, CH02-Q01, LOG06-Q01 |
| B06 và bản lưu | Lối thoát A/B dùng cùng điều kiện vùng Cấp cứu. Ghi T06 và chuyển kết thúc qua snapshot. Nếu lỗi ghi, giữ cameo có thể thử lại và không xác nhận kết thúc. | CH02-Q05, thử lỗi sau flush, thử kết thúc cameo sai thời điểm |
| Bản ghi hội thoại | Hội thoại kết thúc, bị thay hoặc bị chặn đều trả kết quả cho cảnh gọi. Lưu nội dung đã được phép trình bày cùng snapshot. Bản lưu schema 1 đời trước tải với bản ghi rỗng. D16b và CH04-D06 dùng điều kiện Chapter, Beat và event từ bản xuất Core 07. | CH01-Q06, V22-Q03 phần CH01/02 và thử logic điều kiện CH04 |
| Scene và navigation | Gỡ pad, door, actor, audio và nav theo vùng. Từ chối restore khi pad chưa nạp, khôi phục sau khi tải lại. Cache miss của transaction additive dùng dựng NavMesh bất đồng bộ. | T02/T03/T08, bản lưu AP05 qua vòng unload/load LOC04 |
| Hình ảnh | Sửa hướng rig, đường Lan ra khỏi bàn, tư thế nhìn và vách lễ tân. Chỉnh mí, bề mặt nhãn cầu của Lan trong Blender và tăng ánh sáng bù cục bộ dọc tuyến R. Giữ preset, render scale và bake GI đã commit làm nền. | Ảnh Blender và Unity cùng góc, checkpoint ở các cụm hình ảnh LOC01–04 |

[Module Windows](../../output/m2a/release-module/gameplay.txt) và [input Windows](../../output/m2a/release-input/gameplay.txt) đều qua. Ca input lái capsule và dùng UI qua các vị trí xuất phát có kiểm soát. Hai đoạn thoát Cấp cứu dùng phím đi bộ qua cửa A/B. Ca stress soi giấy 100 lượt còn 0 mesh và 0 material tạm. [Ca phá invariant](../../output/m2a/release-escape-probe/gameplay.txt) phát hiện việc giấu cameo khi lượt ghi lỗi và trả mã lỗi 1.

Bản tự lưu schema 1 của baseline `ee8f486` chưa có trường transcript. Mở từ menu chính của ứng viên, game tiếp tục tại Cấp cứu với hồ sơ 101 đã lưu. Nhật ký giữ F101 và E11/E12, hiện bản ghi hội thoại rỗng. [Khung hình sau tải](../../output/m2a/legacy-restore/restored-retry.png) và [Nhật ký sau tải](../../output/m2a/legacy-restore/journal-retry.png).

## Đo trên máy tham chiếu

Ba tiến trình release độc lập, Intel Core Ultra 7 155H, Intel Arc, RAM 16 GB, D3D12, Medium 1920×1080, 60 Hz, VSync và render scale 0,88. Toàn bộ 9.857 mẫu trong tuyến có focus. Tuyến tự chạy đi từ ngoài cổng tới sổ Cấp cứu, có soi thư, Lan, Bé An, Gatekeeper, đèn pin, lưu tải và đổi R/B. Mỗi tiến trình khởi động mới. Bộ đệm của hệ điều hành được giữ theo trạng thái sử dụng thông thường.

| Lượt | Từ launch tới sẵn sàng | Frame Unity P50 / P95 / P99 | Display P50 / P95 / P99 | GPU Unity P95 | Working set đỉnh |
| --- | ---: | ---: | ---: | ---: | ---: |
| [01](../../output/m2a/release-perf-01/) | 5,44 s | 16,67 / 33,33 / 33,34 ms | 16,66 / 33,35 / 33,46 ms | 17,82 ms | 1.279 MiB |
| [02](../../output/m2a/release-perf-02/) | 5,34 s | 16,67 / 33,32 / 33,34 ms | 16,66 / 33,31 / 33,46 ms | 16,80 ms | 1.273 MiB |
| [03](../../output/m2a/release-perf-03/) | 5,59 s | 16,67 / 33,32 / 33,34 ms | 16,66 / 33,31 / 33,46 ms | 16,22 ms | 1.281 MiB |

**Cổng 16,67 ms chưa đạt.** Hai chuyển cảnh R↔B có khung 50–66,66 ms. Nhiều khung trễ lặp lại khi dùng đèn pin tại LOC03 và kho giường. Main thread P95 6,70–6,76 ms, còn GPU tại một số góc đèn pin đạt 19–23,76 ms. Giữ nguyên preset Medium khi đo và khi đóng gói. [Số liệu CPU, GPU, frame hiển thị, bộ nhớ và từng pha](../../output/m2a/timing-summary.json).

Lượt chẩn đoán tắt đổ bóng hoàn tất tuyến với GPU P95 15,24 ms và display P95 16,80 ms. Tiến trình gặp access violation khi thoát sau khi ghi log. Kết quả này khoanh vùng chi phí shadow pass để profile tiếp. Ưu tiên cô lập pass của đèn pin và probe refresh ở LOC03 trước khi thay chất lượng. [Log lượt chẩn đoán](../../output/m2a/diagnostic-shadows/) và [tóm tắt](../../output/m2a/shadows-diagnostic-summary.json).

[Unity Profiler trace](../../output/m2a/unity-profiler/route.raw) và [Memory Profiler snapshot ở Cấp cứu](../../output/m2a/unity-profiler/er.snap) được thu riêng từ development player cùng revision. Lượt chẩn đoán ghi đầy đủ tuyến và lưu bộ nhớ thành công. Dùng ba lượt release ở trên để đối chiếu cổng frame time.

## Phạm vi duyệt

- **Hình ảnh** Dải sân tối vẫn che khuất một phần mặt tiền. Da vùng cổ và cẳng tay của Lan lệch tông trong ánh sáng quầy, còn tóc và chuyển động môi lộ giới hạn khi nhìn gần. Bé An và Gatekeeper còn ở mức mô hình slice. Action `An_TurnFade` nghiêng thân rõ khi xem riêng, cần sửa trục quay và thử lại cùng hiệu ứng mờ dần trong Unity. Dùng [đối chiếu lịch sử](research-and-art-direction.md) để chuẩn bị art review và giữ các giả định U trong hồ sơ chuyên gia.
- **Kiến trúc** Bốn scene đang nạp cùng khi khởi động. Vòng gỡ và tải lại LOC04 kiểm được quyền sở hữu vùng và khôi phục AP05. Chuyển vùng theo yêu cầu, pad đích tự nạp khi resume, nhiều phiếu, schema mới và lớp M vẫn thuộc cổng tích hợp Alpha. Guard CH04 được thử trên dữ liệu điều kiện, còn thoại và scene CH04/07 cần kiểm trực tiếp khi có nội dung.
- **Người chơi** `TASK010` hiện có 0/6 người mới. Cần ít nhất 5/6 hiểu cơ chế mâu thuẫn, trung bình 20–30 phút và không có softlock nghiêm trọng. [Hồ sơ chạy playtest](playtest-kit-task010.md).
- **Chuyên gia** `TASK026–028` chờ review kiến trúc và y tế, PCCC lịch sử, cùng nghiệp vụ hồ sơ và thể hiện bệnh nhân. [Hồ sơ review](specialist-review-dossiers.md).

## Đường găng M3 sau quyết định M2

1. Xử lý khung trễ ở các lần đổi R/B và shadow pass tại phòng 101, rồi đo lại đủ ba lượt. Chạy ca chơi mới và review trên ứng viên có cùng revision. Sửa vấn đề đọc đường, hiểu F101 và diễn xuất trước khi khóa kit hoặc VO.
2. Mở `TASK011-A–E` sau `TASK010`. Tách điều hướng, sight/hearing, chase/search và safe zone khỏi cameo. Dùng vòng đời vùng và pad xuyên scene trước khi gắn AI vào LOC05–12.
3. Mở `TASK012-A–D` sau AI. Kiểm migration từ save CH02, phiếu theo ID, provenance và bản thoại D16b tại đúng CH04 B06 trong scene thật.
4. Nối `TASK013–016` theo Backlog để thêm ký ức M, replay F101, chuỗi phản bác và bốn kết thúc. `TASK017` chạy hồi quy cả tám chương, bốn ending và các checkpoint phục hồi.
