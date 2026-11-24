# M2-FINAL · Phase A Baseline Audit

## Phạm vi và baseline

- Source checkout `c0b1bbd75805c5e947a3cb5895a00d2e1a0c198e`, nhánh `feat/an-sinh-full-campus-expansion`. Runtime và scene ở HEAD. Các bản cập nhật hướng dẫn QA và hồ sơ TASK027 lần lượt có commit `1dd8fb3` và `fdff2b8` trên nhánh GitHub.
- Core v3.3.1 Final Clean — Design Lock. 10/10 SHA-256 khớp bản kê phát hành. Trình kiểm tra xác nhận 7 tệp Core, 27 sheet và đồ thị phụ thuộc 96 task.
- Dependency đọc từ ProjectVersion, manifest và package lock. Unity `6000.3.28f1`, URP `17.3.0`, AI Navigation `2.0.16`, Input System `1.20.1`. Đây là cấu hình khai báo, cần xác minh trên build.
- Build Settings bật bootstrap và 8 scene vùng gồm LOC01–05, LOC09, Campus_Grounds và Campus_Interiors. Rà các điểm nối cửa/điểm tương tác do bộ dựng scene tạo trong `HospitalAuthoring.Art.cs`, `HospitalAuthoring.Expansion.cs`, `HospitalAuthoring.Zones.cs` và các lớp trình bày trong `HospitalScene.cs`, `RegionStreamer.cs`. Khi chạy Player, kiểm collider và đường đi thực từ từng pad, cả hai lớp theo phạm vi.
- Đối chiếu `07/Roadmap`, `Backlog`, `Chapter gates`, `Dialogue`, `Evidence Specs`, `QA` và `Narrative QA`. G01/G02 và G03/G04 có điểm vào runtime tương ứng, cần chạy ca thiếu điều kiện, lưu và tải lại. Log Campus cũ ghi revision `ac0c270` với diff Prototype, nên là bằng chứng lịch sử.

## Sai lệch và quyết định

| ID / mức | Expected và nguồn | Actual ở baseline | Ảnh hưởng và xử lý |
| --- | --- | --- | --- |
| EVT026 / P2 tài liệu | Cách diễn giải được duyệt theo `CH07-D15` và E02 cho `TASK027`. | Core 02 §2 ghi Minh lên sân khi hiện trường **chờ cứu hỏa**. Core 07 `Timeline!D27` ghi **cứu hỏa đổ vào**. | **Đã xử lý cho hồ sơ thẩm định.** Khoảng 03:25 Minh rụt tay khỏi chùm chìa, rời cửa Khu C và trở lên sân giữa tiếng còi cứu hỏa. Sau chuyển thời gian, 04:15 giao chìa tại bàn chỉ huy, khớp E02. Chuyên gia thẩm định giờ lực lượng thực sự vào sân. Xem [errata](design-lock-errata.md#evt026--trình-tự-ứng-cứu-trong-hồ-sơ-task027). EVT026 hết chặn runtime CH01–CH04. |
| E03 / P1 kiểm soát tiết lộ | Core 04 CH03 B03 và PZ03 dùng E03 12 mã để xác nhận tồn tại C02. B04 và `CH03-PU02` dành việc xác thực tên cho bộ nguồn tùy chọn, có thư độc lập. | Core 07 `Evidence Specs!G5` ghi trực tiếp cặp `C02 / Bà Lành` trong danh mục gấp kèm E03. `HospitalGame.Pages.cs` mở sổ theo ký hiệu và dành tên cho tương tác thư. | **Đã duyệt cách hiển thị ngày 25/11/2026.** Hiển thị nguyên văn phụ lục ngay khi mở E03 sẽ cho người chơi biết tên trước tuyến điều tra tùy chọn. E03 ở tuyến bắt buộc hiển thị mã và dấu vết, thư giữ vai trò xác thực tên. Lưu danh mục đủ tên ở nguồn sản xuất. Giữ G03 theo tồn tại C02 và C12 unknown. |
| CH01-T04 / P1 hướng dẫn QA | Core 07 `Events!5`, `DEC-CH01R-04`: Lan nhận biết trước, E/K bắt đầu cinematic, skip giữ transcript, BOARD riêng. | Hướng dẫn QA cũ ghi Lan tự khởi thoại. Điểm vào runtime là `LAN_TALK`. | Hướng dẫn đã sửa theo quyết định người dùng ở `1dd8fb3`. Chạy lại `CH01-WT-Q01/Q02` trên ứng viên mới. |
| CH02-D08 / CH03-D07 / CH04-D08 / CH04-D09 / P1 | Core 07 master có 53 dòng theo cột Chapter CH01–04 (28/8/7/10). Phát ở đúng vật chứng và beat. | `Dialogue.json` có 49 dòng (28/7/6/8), thiếu đúng bốn ID kể trên, 49 dòng còn lại khớp theo chapter, speaker, context và copy. `CH01-D16b` và `CH04-D06` vẫn có điều kiện B06, H05 và PT02 trong dữ liệu. | Dùng `tools/sync_production_data.py` và đồng bộ thông qua thao tác đọc vật chứng. Test guard, transcript, skip và load theo `V31-Q01` và `V32-Q02/Q03`. |
| LOC01 / T09 / P1 | `DEC-V331-01`, Core 04 B07 và `V33-Q05`: seam đúng một lần. Lần sau dùng phản hồi chủ quan của Minh và nhắc Journal. | `HospitalGame.World.cs` báo khóa chặt phía ngoài nếu thử lại tay không. `RealityTransaction.cs` và `HospitalScene.cs` áp tư thế đóng cổng dựa trên T08, kể cả sau T09. | Sửa đường thử lại và hồi phục vị trí. Kiểm cả E/K, kéo, chìa đang cầm hoặc cất, Reduced Motion và save/load. |
| E11 / E04 / C12 / P1 | `Evidence Specs!G16/G6`, Core 04 và `DEC-V32-03`: E11 có dòng gạch xóa ở phiếu chuyển, E04 ghi 13 thi thể thu hồi và mục #12 chưa rõ danh tính, hộp C12 ở R chỉ còn vết gỉ. | `HospitalGame.cs` dùng phiếu chuyển rút gọn. `HospitalGame.ChapterFour.cs` còn ghi 13 người tử vong. `HospitalGame.Pages.cs` mô tả chìa thật trong hộp C12. | Đồng bộ các bề mặt đọc/soi. Giữ nguồn R, điều kiện cấp vật chứng và các ranh giới tiết lộ. `CH02-Q02`, `V32-Q03` và kiểm CH04 tại build mới. |
| S1 và Campus / cần chạy | `DEC-V331-01`, `V33-Q05`, `WORLD-Q01–Q06`: S1 nội bộ LOC02↔05↔07 thông ở R, cửa xả trực tiếp ra sân bị chắn, tuyến E05b trước G06. | Mã dựng có hai đoạn S1 nội bộ. Bốn scene Campus/khu trên/B1 được đăng ký trong Build Settings. Cửa xả sân, va chạm và NavMesh cần kiểm trên Player. | Giữ topology. Kiểm XYZ streaming, qua biên khi load, thứ tự gate và E05b từ phía ngoài. |

## Trạng thái theo task

| Task | Phase A | Cổng còn lại |
| --- | --- | --- |
| TASK007 | Rà walkthrough, ba nhánh F101 và các điểm T07–T10 trong mã, xác định sai lệch LOC01. | Input B01–B08, sửa phiếu, AP01/CP04, rollback, save/load và log cùng build. |
| TASK008 | Đối chiếu PZ01/PZ02, E11/E12, G02, C01 và tuyến cameo. | Chạy CH02-Q01–Q06, cả hai lối thoát và thiếu từng thành phần E11. |
| TASK009 | Xác định phạm vi LOC01–04 và phụ thuộc TASK007/008. | Qua kit R/B, temp VO, caption/transcript, Large-Print, mute, Reduced Motion, keyboard-only và thay thế thao tác giữ-kéo. |
| TASK010 | Chưa làm. `Backlog!32` và Core 01 đặt ba ngưỡng 5/6 độc lập. | Khi chuẩn bị kit, kiểm lại nhãn Q1 về hiểu hai lớp. Ba ngưỡng tính riêng là C01 trong 8 phút không hướng dẫn, hiểu hệ quả F101 và phân biệt người dẫn đường với threat. Gói dùng tuyến M2 rút gọn. |
| TASK011/012 | Là cổng M3. CH03–04 hiện có nằm trong hồi quy ứng viên M2-FINAL. | Giữ AI và CH03–04 hiện có. Nghiệm thu M3 theo dependency riêng. |
| TASK025 | Hai subtask hoàn thành ở M1 theo tracker. | Hồi quy 3 nhánh và revision, 30 lần swap, lỗi ghi và duplicate evidence. |
| TASK026–028 | Chờ chuyên gia. TASK027 đã cập nhật bộ câu hỏi theo quyết định EVT026. | Dùng hồ sơ đúng phạm vi từng cổng, ghi chuyên gia, biên bản, giới hạn và việc cần sửa. |
| TASK030-A/B/C | A chờ duyệt. B/C còn mở theo Backlog. | Bằng chứng Campus theo build mới, thông số top-down, NavMesh, streaming XYZ và rollback. |

**Implemented:** hồ sơ TASK027, errata và báo cáo rà nguồn. **Technical Verified:** SHA-256, cấu trúc Core và đối chiếu tĩnh runtime. **Human Review Pending:** TASK010 và TASK026–028. **Accepted:** M2 còn mở. QA trên Player, benchmark ba lượt, Windows x64 candidate và các ca ngoài CH01–CH04 chưa chạy trong lượt này. Người dùng đã chốt bề mặt hiển thị E03 và cho phép tiếp tục Phase B ngày 25/11/2026.
