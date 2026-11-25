# Sổ trạng thái sản xuất NPR-01

**Snapshot 25/11/2026.** Mỗi trạng thái gắn một phạm vi. “Đã triển khai” cần commit/asset. “Đã kiểm kỹ thuật” cần build, cấu hình, ca và kết quả. “Chờ người thật” cần phiên chơi hoặc biên bản có thẩm quyền. “Đã nghiệm thu” dùng điều kiện của task và quyết định đã ghi. Khi có thay đổi, production bổ sung một dòng kèm ngày, revision/build, bằng chứng, người xác nhận và cổng chịu tác động.

## Trạng thái tại nguồn và build

| Phạm vi | Tình trạng tại snapshot | Bằng chứng và việc còn mở |
| --- | --- | --- |
| GitHub `main` | `3c0b955de95bcf39853c30e2669b2bac24ad969e`, commit cha là `c459aba86192591b4adf5d267438e5fb4b77f704`. | Danh tính xác minh qua `git ls-remote`. Chênh lệch giữa hai commit ở `docs/m2/`. |
| Core 07 khóa | Backlog 25/96 Hoàn thành, 12 Đang làm, 4 Chờ duyệt, 55 Chưa làm. M0 4/4, M1 21/21, M2 0/19, M3 0/34, M4 0/17, M5 0/1. | Đếm cột H theo ID và milestone, khớp `project_docs.py status`. [Core 07](../07_Production_Database.xlsx) |
| QA khóa | 138 dòng. 101 Chưa test, 21 Pass, 6 Đang làm, 5 Đạt, 4 Đạt rà tài liệu, 1 Chờ duyệt. Narrative QA 33 dòng Chưa test. | Giữ nguyên nhãn gốc. Bằng chứng từ candidate nằm trong report và log theo case. [Core 07](../07_Production_Database.xlsx) |
| M1 | Đã hoàn thành theo workbook. M2 candidate có hồi quy cho F101, swap, save v1–v7. | Nguồn nghiệm thu [báo cáo candidate](../m2/m2-final-candidate-report.md). |
| M2-FINAL `c459aba` | Đã kiểm kỹ thuật CH01–CH04 trong phạm vi báo cáo. Module 30/30, input S1 3/3, full input 19/19 và 4/4 probe lỗi đúng kỳ vọng. | SHA-256 EXE, Assembly-CSharp.dll và ZIP cục bộ khớp. 9 scene trong Git khác bản bake. Benchmark tách theo phép đo. M2 tiếp tục mở. [Báo cáo và log](../m2/m2-final-candidate-report.md) |
| TASK007–009, TASK030 | Có các phần đã triển khai/được đo trên candidate. `TASK007`–`009` trong Backlog vẫn Đang làm. `TASK030-A` Chờ duyệt, B/C Chưa làm trong workbook. | Dùng ma trận candidate cho từng route kỹ thuật. Art, chuyên gia và điều kiện tích hợp/đóng cổng theo Backlog. [Báo cáo candidate](../m2/m2-final-candidate-report.md) |
| TASK010 | Chờ người chơi thật, bằng chứng gần nhất 0/6. Kit có sẵn; tuyến 20–30 phút rút gọn cần được định danh. | Báo cáo Phase A M3.2 ghi binary hiện chạy CH01-R đầy đủ và có nút vào CH03 tại ER. Chờ chủ dự án chốt cách chứng minh tuyến rút gọn trước khi dùng phiên để nghiệm thu. Có thể đo baseline riêng nếu được giao. [Playtest kit](../m2/playtest-kit-task010.md), [walkthrough](../ch01r/walkthrough-gameplay-B01-B08-investigation-v1.md) |
| TASK026–028 | Chờ người thật, hồ sơ đã soạn. M2 và art lock còn mở. | TASK026 kiến trúc/trang phục; TASK027 mặt cắt, khói và ứng cứu, EVT026 còn chi tiết chờ; TASK028 provenance, giấy tờ và phẩm giá. [Dossiers](../m2/specialist-review-dossiers.md), [errata](../m2/design-lock-errata.md) |
| M3.2 / TASK011–012 | Nhánh cục bộ `feat/m32-ai-ch03-ch04` ở `9a6378b`; ứng viên kỹ thuật vẫn là `c90a2eb5fe62521839c3be84a0e59e2e56b41410`. Dossier ở `85925dc` và cập nhật ở `9a6378b` ghi hai nhóm task đã kiểm kỹ thuật. | Phase A `4525df6` ghi ngoại lệ Option 2. Phase F có Module, Full Input, S1 và probe lỗi. Benchmark 120 Hz có đối chứng; 60 Hz PresentMon chính thức chờ ở snapshot. GitHub chưa trả về ref cùng tên. |
| TASK013–016 / CH05–08 | Dự thảo NPR-01 ở phạm vi tài liệu. | Phase A M3.2 giữ phần runtime CH05–08 ngoài phạm vi ngoại lệ. QA cho các chương và ending được liệt kê để lập kế hoạch. |

### TASK010 và báo cáo human review

Khóa revision, tuyến bắt đầu/kết thúc và marker trước khi mời người thử để nhận đúng phạm vi CH01 rút gọn → C01 → cameo. Ghi sáu người mới theo ID ẩn danh, ba ngưỡng 5/6 độc lập, thời gian PZ01 → C01 tối đa 8 phút không hướng dẫn, F101 ký sai và nhận ra hướng dẫn/threat bằng hành vi. Ghi riêng playtime toàn slice và dấu kết thúc ER. Telemetry hiện có dừng ở T10, điều phối viên bấm mốc C01/cameo riêng theo kit. Điểm Unverified vẫn hợp lệ. Chỉ ghi Pass vào Core 07 khi có đủ kết quả, phạm vi và biên bản. Biên bản chuyên gia ghi người duyệt, phạm vi, giới hạn, góp ý, thay đổi và revision kiểm lại.

## Các quyết định production còn mở

| Câu hỏi | Căn cứ cần có để duyệt | Người chuẩn bị / thời điểm quyết định |
| --- | --- | --- |
| Phạm vi bản đầu và thời lượng | Chi phí theo nhóm nội dung, tốc độ làm một cảnh đủ game-ready và dữ liệu slice 20–30 phút. Ước lượng hai phạm vi theo Core 01 §3: quy mô đủ chương và lộ trình nhỏ hơn nếu solo. | Production/owner, sau khi có playtest và số đo mẫu. |
| Nhân sự, chi phí và lịch | Bảng năng lực/giờ thực có của từng vai trò, chi phí asset/thu âm/QA/chuyên gia, tải milestone song song và phần dự phòng. Ghi đơn vị, khoảng ước lượng và giả định cho từng dòng. | Production/owner trước cam kết lịch và đặt hàng final. |
| Phần cứng tối thiểu và nền tảng phụ | Bằng chứng nhiều lớp máy, ngưỡng QA và chi phí build/phân phối. Windows x64, Unity/URP đã chốt tại M0. | Tech/production, xem [kế hoạch đo](technical-plan.md). |
| Ngôn ngữ, mức VO và phân loại | Chi phí dịch, kiểm chữ trên prop, VO/sub/caption, bề mặt bị ảnh hưởng, dossier thẩm định và thị trường dự kiến. | Art/audio, localization, production và owner trước art lock/phát hành. |

Dùng `07/Roadmap` và `07/Backlog` để quyết định thứ tự, rồi attach bằng chứng vào đúng build. Phạm vi Core 01 §3, 07/Dashboard và ledger được làm rõ ở [NPR-01-08/09](proposed-diffs.md#npr-01-08--ngày-số-và-phạm-vi-chỉ-số).

## Bản ghi theo task và revision

| Ngày · build hoặc commit | Task | Implemented | Technical Verified | Human Review Pending | Accepted | Bằng chứng |
| --- | --- | --- | --- | --- | --- | --- |
| 25/11 · Core 07 v3.3.1 và c459aba | TASK025 | Có | Có trong phạm vi M1, hồi quy trên candidate | Theo điều kiện M1 đã ghi | M1 theo Workbook | Module/F101 và Save v1–v7 trong báo cáo M2 |
| 25/11 · c459aba | TASK007 | CH01 B01–B08 | Module/Input theo ma trận candidate | TASK010 | Chưa đóng M2 | CH01-Q và walkthrough hiện hành |
| 25/11 · c459aba | TASK008 | CH02, C01, G02 và cameo | Module/Input Door A/B | TASK010 | Chưa đóng M2 | CH02-Q theo ma trận candidate |
| 25/11 · c459aba | TASK009 | Greybox, temp VO, caption/UI | Stimulus và giao diện theo TASK009-C | Art/VO/legal sourcing và TASK026–028 trong phạm vi tương ứng | Chưa đóng M2 | AU01, T06, 7/7 thao tác toggle theo log |
| 25/11 · c459aba | TASK010 | Kit và candidate; tuyến rút gọn cần xác nhận | Có binary kỹ thuật, kết quả tuyến nghiệm thu chờ | 0/6 | Chưa | Kit, mốc timeline và rubric 3 × 5/6 |
| 25/11 · c459aba | TASK026 | Dossier kiến trúc/thiết bị/trang phục | Chỉ log route và asset liên quan | Chờ chuyên gia | Chưa | Dossier M2, CHAR-Q |
| 25/11 · c459aba | TASK027 | Dossier mặt cắt, khói, ứng cứu | Các kiểm tra route kỹ thuật theo phạm vi báo cáo | Chờ chuyên gia | Chưa | Errata, E02, V33-Q05, W-Q02/03 |
| 25/11 · c459aba | TASK028 | Dossier hồ sơ, provenance và phẩm giá | Các kiểm tra gameplay/guard theo phạm vi báo cáo | Chờ chuyên gia | Chưa | LOG06-Q01, V22-Q03; phụ lục canon NPR-01 để xin ý kiến |
| 25/11 · c459aba | TASK030 A–C | Các phần campus/CH03–04 theo report | S1, tuyến và một số WORLD-Q | Review theo từng task | Chưa nghiệm thu toàn bộ | Candidate report, QA index campus |
| 25/11 · c90a2eb (M3.2) | TASK011-A–E | AI theo vùng, hearing/nav, Orderly và safe/streaming | Technical Verified theo Phase F | Cổng M2 vẫn mở theo ngoại lệ | Chưa nghiệm thu chính thức | Module/Full Input trên binary c90a2eb, chuỗi commit B–E |
| 25/11 · c90a2eb (M3.2) | TASK012-A–D | CH03–04, PZ04 hai tuyến, PZ05/PZ06, AN05 handoff | Technical Verified theo Phase F | Cổng M2 vẫn mở, 60 Hz PresentMon còn chờ | Chưa nghiệm thu chính thức | CH03-Q01–Q05, CH04-Q01–Q06, full input và probe lỗi |


Bằng chứng M3.2 tại nhánh cục bộ: `9a6378b:docs/m3/m32-implementation-and-acceptance-report.md`, phần §5 và §6, cùng `docs/m3/evidence/m32-c90a2eb/`. Tách revision báo cáo và binary. Phạm vi 120 Hz và 60 Hz xem [kế hoạch đo](technical-plan.md).

Một hàng có thể chuyển trạng thái kỹ thuật cho phạm vi con trong khi cổng task chính vẫn chờ. Dòng sau phải ghi người xác nhận, log/phép đo, phạm vi, revision và liên quan Roadmap. Nếu log chưa có, giữ trạng thái đang chờ. Các mã QA và ca draft xem [QA impact](qa-impact.md).
