# M2-FINAL · Phase B Runtime Synchronization

**Revision được đối chiếu** `c0b1bbd75805c5e947a3cb5895a00d2e1a0c198e` cùng thay đổi trong working tree. Nguồn là Core v3.3.1 Final Clean. Kết quả dưới đây gồm kiểm tĩnh, biên dịch C# và smoke test logic ngoài Player.

| Hạng mục | Expected | Actual và bằng chứng |
| --- | --- | --- |
| Dialogue CH01–CH04 | Đồng bộ `07/Dialogue` theo cột Chapter, giữ 28 dòng CH01 và guard `CH01-D16b`/`CH04-D06`. | `Dialogue.json` có 53/53 dòng khớp, thêm đúng bốn ID. 28/28 dòng CH01 và năm bản ghi guard khớp baseline. Smoke test kiểm đủ các trường hợp sai chapter, sai beat, thiếu từng cờ và đủ H05/PT02. |
| `CH02-D08` · E11 / FS21 | Phát khi người chơi đọc dòng gạch xóa trên phiếu R trong kho 101. E11 vẫn cần đủ tranh, vòng và phiếu. | `HospitalGame.cs`, `HospitalGame.Pages.cs`, `BackpackFramework.cs`, `Transfer.png` và nguồn bake đồng bộ phiếu. Thao tác đọc có bản chữ lớn và transcript; nút thu thập dùng đường lưu vật chứng cũ. G02 thiếu phiếu bị từ chối trong smoke test. |
| `CH03-D07` · E03 / C12 | Sau bước xác nhận tồn tại C02, ghi chú C12 giữ độ mở. Lượt đọc bắt buộc chỉ có mã và dấu vết. Hộp R chỉ còn vết gỉ. | `HospitalGame.Pages.cs` giữ tên Bà Lành ở tuyến thư. Ghi chú C12 mở từ hộp khi G03 đủ điều kiện. Blender/FBX và bề mặt đọc khớp hộp rỗng. Smoke test xác nhận G03 phụ thuộc sự tồn tại C02 và C12 vẫn chưa xác thực tên. Xem [errata E03](design-lock-errata.md). |
| `CH04-D08` / `CH04-D09` | Đọc mục #12 E04 tại LOC10 và đối chiếu 02:50 từ E02/E09 tại LOC06. Nguồn giữ giới hạn tri thức. | `HospitalGame.ChapterFour.cs` tách thao tác đọc #12 và đối chiếu 02:50. E04 hiển thị 13 thi thể thu hồi, #12 chưa rõ danh tính. Tuyến đọc 02:50 cần cả E02/E09. Bốn cue mới có nội dung đọc và transcript. |
| `DEC-V331-01` · LOC01 sau T09 | Seam một lần. Sau đó cổng giữ mở, Minh nghe ù trầm, tự nhắc và lùi nhẹ về sân, Journal chỉ đường. | `RealityTransaction.cs` chỉ trình bày cổng khép trong lúc chờ lần thử T09. `HospitalGame.World.cs`, `HospitalGame.cs`, `FirstPerson.cs` dùng phản hồi và bước lùi va chạm qua controller. Có assertion mới trong `HospitalGame.QA.cs`. E/K, đi bộ qua ngưỡng, tải lại và Reduced Motion cần Player để xác minh. |
| `DEC-V331-01` · S1 | Core 03 §8 và `V33-Q05` ghi trục nội bộ thông ở R, cửa xả trực tiếp ra sân bị chắn. | Theo quyết định được duyệt, `RegionStreamer.cs` tải LOC05 khi đến gần S1 trong R trước G02 và giữ vùng khi người chơi ở trên thang hoặc tầng trên. `HospitalScene.cs` mở connector LOC02 khi LOC05 đã đăng ký. S3/B1 và các điều kiện CH03 tiếp tục đi qua logic hiện có. `HospitalGame.QA.cs` thêm ca V33-Q05 cho R/B, tải vùng trước khi mở lối, đường NavMesh và giữ gate khi tới AP06. Cửa xả trực tiếp ra sân chưa có vị trí xác nhận để kiểm trên Player. |

## Kiểm chứng

- Kiểm nguồn 10/10 SHA-256, 7 tài liệu Core, 27 sheet, đồ thị 96 task. Save Schema khai báo v7, tối thiểu v1. Smoke test chưa chạy migration dữ liệu lưu thực tế. [Đối chiếu SHA-256](evidence/phase-ab/core-sha256-verification.txt).
- Biên dịch `Assembly-CSharp` theo response file Windows Player của Unity 6000.3.28f1 bằng Roslyn đi kèm. Thành công. SHA-256 assembly biên dịch `5051bdb272bbb8997c69623082b3b113b0eddfb138d9bfb5d56eed3a0e218104`. [Log biên dịch](evidence/phase-ab/roslyn-compile.txt).
- Smoke test 11/11 qua G01, G02, G03, danh tính C02/C12, điều kiện thoại CH04 và hợp đồng schema. [Logic smoke test](evidence/phase-ab/logic-smoke.txt).
- Lệnh build thử `./tools/build_m2.ps1 -SkipGenerate -Output 'output/m2-final/phase-b-verification'` dừng trước khi đóng gói Player. Editor báo timeout khởi tạo Licensing rồi crash trong `UnityEditor.BuildReportRestService` khi tạo socket cục bộ. Lượt dựng lại sau thay đổi S1 lặp lại timeout Licensing và lỗi `wakeup_pipes_init`. Log gốc `output/m2-final/phase-b-verification/build.log` và log dựng lại `output/m2-final/s1-verification/build.log`. Ca NavMesh V33-Q05 chưa chạy.

**Implemented — Player Verification Pending** · Đồng bộ thoại, bề mặt vật chứng và phản hồi LOC01 theo các mục trên. Trục nội bộ S1 trước G02 đã triển khai. Vị trí và va chạm cửa xả trực tiếp ra sân cần xác minh riêng.
**Technical Verified** · Kiểm tĩnh, biên dịch C# và smoke test logic. Build Windows x64, QA trên Player, NavMesh/streaming, D3D12 và benchmark đang chờ.
**Human Review Pending** · TASK010 và TASK026–028.
**Accepted** · M2 tiếp tục mở. Người dùng đã duyệt checkpoint triển khai. Trạng thái nghiệm thu Phase B và V33-Q05 đang chờ Player.
