# Kế hoạch kỹ thuật, bản địa hóa và trợ năng

Dự thảo NPR-01-10. Dùng [Core 06](../06_Technical_Design_Document.docx) và `TASK029 / DEC-V2-04` cho công nghệ và các mốc đã khóa. `TASK020` kiểm ngân sách kỹ thuật và trợ năng trước khi cam kết phần cứng. Windows x64, Unity 6000.3.28f1 và URP 17.3 là cấu hình dự án. Các cam kết phần cứng phụ, ngôn ngữ, min spec và ngân sách tài nguyên cần dữ liệu.

## Đo hiệu năng

| Chỉ số | Tham chiếu `c459aba` | Phạm vi đo đề xuất |
| --- | --- | --- |
| GPU / render | Unity GPU P95 12,77–12,97 ms. PresentMon GPUTime P95 13,74–13,88 ms. Số cũ 17,63 ms của phòng 101 là phép thử khác. | Core 06 V30 yêu cầu P95 render 1080p Medium ≤ 16,6 ms, kể cả giao thoa LOC08 và hậu đề Cơ sở 02. Đo riêng từng cửa sổ/route và ghi định nghĩa render. |
| CPU & frame pacing | Unity Main CPU P95 6,88–7,23 ms. Unity wall P95 16,68 ms, PresentMon DisplayedTime P95 16,73–16,75 ms, FrameTime P95 19,06–19,35 ms. | Marker Main/Render, GPU, wall và presented/displayed frame riêng. Ghi p50/p95/p99, max, spike và số khung ngoài ngưỡng, điều kiện VSync và capture focus. |
| RAM/VRAM | Working set đỉnh 1.755,6–1.763,3 MiB trong các lượt đo candidate. | Windows working set, managed heap, native/graphics memory, RAM hệ thống và bộ nhớ GPU dùng riêng/chia sẻ nếu đo được. Giữ giới hạn của từng counter. Đo idle, chơi liên tục, swap/load và reload. |
| Loading & bộ nhớ dài hạn | Launch → Ready 4,75–5,04 s trên máy đo candidate. | Cold/warm launch, thời gian tải theo khối, swap R/B, quay lại checkpoint, 30 swap và phiên dài. Ghi ngưỡng xác nhận, lỗi và log. |

Mốc trên lấy từ ba lượt 1080p Medium, render scale 0,88, 60 Hz, D3D12/VSync 1 trên Intel Core Ultra 7 155H / Intel Arc. Mốc GPU của tuyến đã thử chưa phủ LOC08, hậu đề hoặc ngân sách frame end-to-end. Kết quả P95 trình bày theo counter, tuyến và cấu hình. [Báo cáo M2](../m2/m2-final-candidate-report.md), [số liệu gốc](../m2/evidence/m2-final-candidate/performance.json), [phép đo prototype](../prototype/verification.md).

### Ma trận tối thiểu cho vòng kế tiếp

- Chọn một cấu hình tham chiếu tương đương máy M2, một máy ứng viên mức tối thiểu và một máy phổ thông có GPU rời. Ghi CPU/GPU/driver/RAM/đĩa/OS và thiết lập nguồn điện. Mọi cấu hình chưa thử là ứng viên.
- Dùng 3 lượt độc lập cho mỗi tuyến và preset, cùng save, route, máy và điều kiện nhiệt/focus. Đo cả cấu hình thử trong Core và lượt không giới hạn riêng cho chẩn đoán. Ghi coverage của trace và thời điểm bắt đầu/kết thúc.
- Tuyến nền gồm Room 101, CH02 cameo, và CH03/04 streaming/nav trên đúng build được giao. Khi có build hợp lệ, thêm LOC08 giao thoa R/B, ~03:25 ở M và hậu đề B08 END04. Ghi thiếu tuyến là chưa có kết quả.
- Sau khi có số đo đại diện, tech đề xuất trần CPU/GPU/VRAM/RAM, thời gian tải, mức dự phòng và các lựa chọn giảm tải. Owner chốt phần cứng tối thiểu và preset sau khi route bắt buộc, trợ năng và stability đạt trên cấu hình liên quan.

## Tái lập build và provenance

Ứng viên M2 đã đóng gói có định danh EXE/DLL/ZIP khớp. So trực tiếp Git blob của 9 scene trên `main` với `build-identity.txt` cho thấy cả 9 khác mã băm của bản bake. Phase A M3.2 ghi các file sinh sẵn dùng tại worktree build, xem `prototype_diff` trong [build identity](../m2/evidence/m2-final-candidate/build-identity.txt). Điều kiện dùng `-SkipGenerate` trong [báo cáo candidate](../m2/m2-final-candidate-report.md) là giữ đúng chín scene đã bake.

1. Giữ nguyên candidate M2, nguồn bake và dữ liệu băm của nó. Lần cần tái lập từ checkout sạch, tech lên lượt build mới chạy authoring đầy đủ và lưu hash scene, probes, geometry, lighting, occlusion, toolchain/package lock và script build. Dùng `-SkipGenerate` khi đã đối chiếu đúng snapshot sinh sẵn của build cần lặp.
2. Ghi commit, mọi diff khi build, phiên bản Unity và package, cấu hình đồ họa, tên gói và mã băm đầu ra. Truy vết authoring → generated assets → Player → log.
3. Chỉ tuyên bố tái lập khi so được kết quả trên nguồn và artifact phù hợp. Nếu binary khác hash, giải thích phạm vi khác, xác minh manifest, scene và hành vi, rồi đặt build ID riêng. Quy tắc lưu artifact và thời hạn lưu là quyết định production.

## Bản địa hóa

| Bề mặt | Đề xuất và tiêu chí |
| --- | --- |
| Master nội dung | Giữ vi-VN là bản nguồn. Lấy ID `Dialogue/Events/Evidence Specs` hiện hành làm khóa ổn định. Đoạn biến thể theo locale dùng cùng ID, beat/context, layer, guard, speaker, giới hạn tri thức và fallback. Locale không tham gia state gameplay, khóa export hoặc schema save. |
| Prop và tài liệu | Tách bản vật thể vi-VN theo thời kỳ khỏi bản đọc lớn có thể dịch khi cần. Dùng cùng bản nguồn cho inspect/large print/transcript. Chữ ký, mã, dấu thời gian và trường puzzle giữ nghĩa, không tái tạo bằng chuỗi dịch tự do. |
| UI, caption, VO | Bảng từ vựng F101, R/B/M, 03:17, AS-05B, Khu B/C, SOURCE/Export và cách Lan xưng hô. Marker, định dạng biến chèn, tiếng Việt có dấu, glyph fallback, tự ngắt dòng và chiều dài text có kiểm. Chỉ phát VO đúng guard; giữ text vi-VN khi thiếu bản dịch đã duyệt. |
| Gói thử dịch | Chọn luồng F101, E01 hai mặt, CH07 PZ10, E08 transcript và preview END03/04. Thử giả lập chuỗi dài và dấu, rồi duyệt một locale thử nếu owner chốt. Kiểm cả chữ vật thể, bản đọc, transcript và subtitle khi đổi locale/reload. |

Sản xuất dịch giữ bảng thuật ngữ, hướng dẫn spoiler theo beat, bản mẫu typography, revision và người soát. Kiểm hiển thị 100/130% và cỡ chữ dài, hỗ trợ phím, rút ngắn có kiểm nghĩa, giao diện không tràn/chồng. Quyết định phát hành locale và ngân sách VO dùng sổ quyết định.

## Trợ năng

Giữ đường đọc đủ nghĩa khi mute/mono, caption hướng cho cue quan trọng, reduced motion, chữ lớn, thao tác phím/toggle, Story Mode và tuyến fallback. Thử tìm đúng nút bằng bàn phím, phóng chữ trên mặt giấy và UI, phân biệt tín hiệu bằng văn bản/hình, skip và load lại cảnh có trạng thái. Các kiểm kỹ thuật dự thảo lấy `T06`, `AU01`, `CH08-Q07`, `V32-Q04` và tiêu chí chính thức trong `TASK020`. Bằng chứng nghiệm thu gồm ảnh/clip, log, bề mặt, preset, locale và revision.
