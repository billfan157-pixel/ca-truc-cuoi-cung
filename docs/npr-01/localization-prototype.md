# Bản thử localization NPR-01

Thiết kế cho một bản dữ liệu offline, phục vụ duyệt trước khi làm runtime. Master là vi-VN. Bản mock so sánh một bản gốc, một bản kéo dài giả lập và một bản thiếu khóa. Locale giả lập chỉ cho đo giao diện. [Core 06 dự thảo](core-draft/06_Technical_Design_Document_NPR-01-Draft.docx) và [kế hoạch kỹ thuật](technical-plan.md) có ranh giới tích hợp.

## Dữ liệu và xuất bản

1. Export các hàng cần thử từ `07/Dialogue`, `07/Evidence Specs` và nguồn UI. Giữ cột `id, surface, source_revision, locale, text, context, guard, speaker, voice_id`. Tách rule metadata và translation record. Dialogue dùng ID đã tồn tại. Evidence dùng ID cùng trường đọc bắt buộc. Định ID UI có kiểm trùng trên scratch fixture trước khi đề xuất namespace.
2. Tạo tài liệu thuật ngữ vi-VN, giữ dấu, chữ viết hoa và cách Lan xưng hô. Các mốc 03:17, 07:40, S-12, AS-05B và mã biểu mẫu là dữ liệu bảo toàn. Tách format ngày giờ UI khỏi time token có ảnh hưởng suy luận. Dùng chuỗi đầy đủ với placeholder có nghĩa, tránh ghép nửa câu.
3. Sinh bundle cho mỗi locale với version nguồn và danh sách thiếu bản dịch. Nếu khóa đích thiếu hoặc bản target lỗi placeholder, quay về vi-VN với log cho QA. Nếu vi-VN thiếu khóa, báo lỗi ở bước build mock. Guard vẫn do nguồn gameplay đánh giá. Hình đạo cụ gốc giữ vi-VN và ngày; bản đọc lớn có thể dịch theo kế hoạch.
4. Subtitle/VO và caption chung speaker/guard. Tách timecode, caption hướng và cue cần thiết khỏi stem. Text và cue có thể chuyển locale độc lập; tệp VO ngoài vi-VN là tùy chọn chờ ngân sách. Giữ font có glyph tiếng Việt và kiểm quyền embed, line break, ký tự kết hợp, dấu, rút gọn và hỗ trợ phím.

| Fixture | Bề mặt | Kiểm đạt trước khi tích hợp |
| --- | --- | --- |
| `CH07-D19`, `CH07-D23/D24`, `CH07-D27/D28` | Subtitle, transcript, PZ10 130% | Dài thêm 30–40% vẫn đọc hết, không đẩy nút. Strip/normalize không đổi nghĩa hoặc mất dấu. Các cặp chứng cứ và ID vẫn khớp. |
| `E01-A`, `E01-B (AS-05B)` | Inspect vật thể, bản chữ lớn, readback | Giữ chữ trên prop, mã và thứ tự hai mặt. Bản đọc lớn đủ trường và nguồn. |
| `CH08-D08/D20/D22` | Coda, nhãn manifest, transcript | Giữ guard của từng ending, không lộ B08 khi đổi locale hoặc load lại. Thiếu VO có bản đọc. |
| `CH07-D15`, `POST-D08` | Mute, reduced motion, hướng và chuyển giờ | Không phụ thuộc âm thanh hoặc chuyển động. Caption giữ ~03:25, 04:15 và motif ở đúng nhánh. |
| Bàn phím và font | 100/130%, nhãn dài, chọn locale | Focus đi đúng thứ tự. Lỗi khóa vắng báo trong log, người chơi đọc vi-VN. Thay locale không đổi state, schema hoặc preview commit. |

Bàn giao CSV/JSON thử, ảnh lỗi và ảnh đạt ở hai cỡ chữ, diff ID, danh sách chuỗi thiếu, danh sách font/quyền và số giờ thử. Tech/QA nhận prototype trước khi lập estimate dịch thuật và quyết định phát hành ngôn ngữ.
