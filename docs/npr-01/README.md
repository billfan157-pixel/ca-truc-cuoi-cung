# NPR-01 · Bản dự thảo rà soát Core

- **Ngày đối chiếu** 25/11/2026
- **Nguồn khóa** GitHub `main` tại `3c0b955de95bcf39853c30e2669b2bac24ad969e`, Core v3.3.1 Final Clean — Design Lock
- **Trạng thái** Chờ chủ dự án và chuyên gia duyệt theo từng mục

NPR-01 dùng [bản review và cuộc kiểm toán trong chat Nghiên cứu đánh giá cốt truyện](chatgpt-conversation://6b052af6-45ac-83ec-b1be-a0388ebc5ee4) để lập đề xuất theo nguồn, ID và ca QA. Các đoạn thay thế bên dưới là bản so sánh để duyệt. [Core hiện hành](../agent/README.md#bảy-nguồn-sở-hữu), [mã băm phát hành](../SHA256SUMS.txt) và [ứng viên M2](../m2/m2-final-candidate-report.md) tiếp tục là mốc đối chiếu.

| Tài liệu | Việc cần review |
| --- | --- |
| [Kiểm toán NPR-01](audit.md) | Kết quả từng nhận định và căn cứ từ Core 01–07 |
| [Chỉ mục canon và quyết định](decisions.md) | Phương án, chủ thể duyệt, điều kiện và đích nhập vào Core 07 |
| [Bản so sánh câu chữ](proposed-diffs.md) | Đoạn hiện hành, bản đề xuất, điểm đồng bộ và việc giữ chờ |
| [Dàn dựng và sản xuất nội dung](creative-production.md) | Vĩnh, Lan, storyboard, environmental storytelling, visual, audio và thẩm định biểu đạt |
| [Sổ trạng thái sản xuất](production-ledger.md) | Backlog đóng băng, bằng chứng M2, nhánh M3.2, điều kiện nghiệm thu và quyết định về phạm vi |
| [Kỹ thuật, bản địa hóa và trợ năng](technical-plan.md) | Ma trận đo, build provenance, locale, accessibility và tiêu chí bàn giao |
| [Tác động QA](qa-impact.md) | Ca hiện hành và bổ sung đề xuất theo từng thay đổi, cùng bằng chứng tại thời điểm bàn giao |

## Trình tự duyệt

1. Ưu tiên phân biệt manifest E01-B, phạm vi công bố và chuỗi lưu E01 trước khi chốt nội dung CH06–CH08.
2. Duyệt khả năng chứng minh của R1/R3 trước khi duyệt thoại. Chọn cách dàn dựng Vĩnh và Lan sau khi xem shot list và giới hạn tri thức từng beat.
3. EVT026 theo [errata đã duyệt cho hồ sơ TASK027](../m2/design-lock-errata.md#evt026--trình-tự-ứng-cứu-trong-hồ-sơ-task027). Chi tiết ứng cứu và đồng bộ Core chờ chuyên gia và chủ dự án.
4. Chủ dự án duyệt từng mục trong [sổ quyết định](decisions.md). Mục được duyệt mới chuyển vào `07/Decisions` theo ID quyết định chính thức, đồng bộ đúng bề mặt đích, ghi `07/Changelog` và cập nhật kết quả QA bằng chứng theo build.

Bất biến của gói duyệt gồm 28 dòng thoại CH01, topology, G01–G08, Save Schema v7, PZ10 năm cặp/năm bit, bốn nhánh END, E08 nonexport, C12 chưa xác định danh tính và hậu đề 2013 chỉ ở END04. CH05–CH08 trong đợt này ở phạm vi tài liệu. TASK010 và TASK026–028 vẫn chờ người thật. Phiên bản Core kế tiếp và lịch phát hành chờ quyết định.
