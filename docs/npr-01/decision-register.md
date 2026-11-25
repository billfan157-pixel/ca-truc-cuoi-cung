# Decision Register NPR-01

Ngày 25/11/2026 chủ dự án duyệt hướng A/B và cho phép xây dựng bản dự thảo. Chấp thuận hướng là căn cứ để đưa phương án vào bộ Core dự thảo. Mỗi câu/shot/canon mới còn một bước phê duyệt theo bảng này. [Ma trận narrative](narrative-impact-matrix.md) và [ma trận gameplay và kỹ thuật](gameplay-technical-impact-matrix.md) định phạm vi kiểm sau khi chốt.

| ID | Hướng được duyệt | Có trong bản dự thảo | Điều kiện duyệt cuối |
| --- | --- | --- | --- |
| **01 · Manifest E01-B** | A và phần B về phạm vi ảnh. Bảy độc lập mọi ending; không bảo đảm END01/02 tránh điều tra. | Hai nhãn nguồn và câu END03 mới. Hai phương án crop/đủ mặt trong hồ sơ canon. | Owner chọn mức ảnh và câu coda sau thử legibility. TASK028 rà phạm vi mô tả và giới hạn kết luận pháp lý. |
| **02 · Phạm vi công bố** | A+B ở cấp canon nội bộ, tách nội bộ/công khai/tri thức người chơi. | Mô hình ba tầng và phương án nhãn trong bề mặt hiện có. | TASK028 nhận xét phạm vi, thuật ngữ và tính hợp lý của nguồn. Owner chốt điểm tiết lộ và quyết định cần hay không cần giải thích thêm. |
| **03 · Chuỗi lưu E01** | Dùng A cho hiện trạng; hoàn thiện B để duyệt. | Ba dòng lưu trong Core 03/07. Phương án hồ sơ hành chính ngoài diện thu hồi có mốc và câu hỏi thẩm định. | TASK028 đánh giá loại hồ sơ/quyền giữ/thời điểm. Owner chốt lựa chọn sau khi có căn cứ hoặc giữ khoảng mở. |
| **04 · EVT026** | Dùng errata trung tính đã duyệt. Các mốc và lựa chọn của Minh được giữ. | Câu dùng cho dossier và cảnh bản mẫu có giới hạn. Dữ liệu timeline/inspect gốc để đối chiếu. | TASK027 đánh giá thời gian tiếp cận, khói và mặt cắt. Owner duyệt đồng bộ chính thức nếu có kết luận. |
| **05 · Vĩnh** | A+B qua E06a, PROP-HD03 và 07:40. | Hai mức thoại/diễn xuất và shot list. CH07-D19 sửa mốc để review theo 08. | Owner chọn cut, câu thoại và giới hạn thêm vào vật chứng sau đọc mù/animatic; narrative kiểm tri thức. |
| **06 · Minh–Lan** | A+B, viết thêm một dòng, 2–3 nhịp tùy chọn. Bảo toàn CH01-R/M2 và runtime M3.2. | Ba phương án ở CH04/06/08, giữ tính chủ động và guard của Lan. | Owner chọn nhịp cần sản xuất, level/gameplay dẫn tác động trước khi mở công việc. Bất cứ tích hợp CH04 nào cũng chờ quyết định phạm vi sau M3.2. |
| **07 · PZ10** | A, câu R1/R3 có chiều sâu với đúng cặp và hợp đồng. | Bốn câu đồng bộ Core 02/04/07, kiểm truy nguyên từng mệnh đề. | Owner duyệt thoại. Narrative/QA thử đọc mù, kiểm từng cặp, luồng hint/skip/load trên build sau triển khai. |
| **08 · Ngày, số, Dashboard** | A, mốc 2003, count theo Backlog, nhãn/cache theo phạm vi. | CH07-D19 và Core 01 §3 đã sửa trong bản sao. Core 07 lưu giá trị đã tính của Dashboard theo ngày. | Owner kiểm câu 2003 tự nhiên trong cảnh và chốt đồng bộ VO/transcript khi có thay đổi runtime. Production duyệt snapshot và loại bộ đếm. |
| **09 · Live Production Ledger** | A, build/task/evidence theo bốn mức. Core 07 sở hữu kế hoạch và hợp đồng. | Sổ line-level và quy tắc thăng trạng thái. Human 0/6 và review chờ. | Production xác nhận phiên TASK010 đúng tuyến, reviewer ký TASK026–028, owner quyết định trạng thái nghiệm thu. |
| **10 · Kỹ thuật và locale** | A+B, Vietnamese-first và localization-ready. | Prototype cụ thể và phạm vi ngân sách CPU/GPU/RAM/VRAM/streaming/loading/build size. | Tech chạy ma trận trước khi đề xuất ngưỡng và cấu hình tối thiểu. Owner duyệt phạm vi ngôn ngữ và VO theo chi phí. |
| **11 · Art, audio và thẩm định** | A, tài liệu tiền sản xuất theo sáu beat/cảnh. | Visual, cinematic, environmental, character và audio có tiêu chí handoff. | Art/audio chọn bản mẫu sau kiểm cue, nguồn và giấy phép. TASK026–028 và owner trước art lock, sau đó xác nhận final. |

## Cách ghi quyết định

Core 07 dự thảo có 11 dòng `NPR-01-01…11` trong `Decisions` và dòng tương ứng ở `Changelog`. Khi một mục được chốt chính thức, ghi ngày, người quyết, nguồn áp dụng, giới hạn kết luận, đích đồng bộ, QA và revision. Ghi tách quyết định câu chữ, canon, staging và budget để các mục cần TASK027/028 có thể tiếp tục chờ. Liên kết đến [lịch sử gói dự thảo](changelog.md) và [phương án nguồn/canon](canon-proposals.md).
