# Canon Authority Index NPR-01

Đối chiếu 25/11/2026 theo Core v3.3.1 Final Clean tại `3c0b955`. Chủ dự án đã duyệt hướng NPR-01. Những mục có câu thay thế hoặc diễn xuất mới trong bộ Core dự thảo tiếp tục chờ duyệt tại [Decision Register](decision-register.md).

Liên kết trong bản đồ mở tài liệu NPR-01 tại đường dẫn chuẩn trên nhánh review. [Bộ Core đã khóa](https://github.com/billfan157-pixel/ca-truc-cuoi-cung/tree/3c0b955de95bcf39853c30e2669b2bac24ad969e/docs) là căn cứ để xác minh dữ kiện hiện hành.

## Bản đồ quyền sở hữu

| Nguồn | Quyết định tại nguồn | Bề mặt đối chiếu |
| --- | --- | --- |
| [Core 01](../01_Game_Design_Document.docx) | Mục tiêu gameplay và UX, phạm vi sản phẩm, quyền để ngỏ, chuẩn playtest và cổng sản xuất cấp thiết kế. | Core 06 kỹ thuật, Core 07 Roadmap/Backlog/QA. |
| [Core 02](../02_Narrative_Bible.docx) | Sự kiện thật, tri thức nhân vật, CAN01–09, WR01–08, kết thúc và giới hạn kết luận. | Core 07 Timeline, Canon Matrix, Reveal order, Ending Matrix và Mystery Matrix. |
| [Core 03](../03_World_Architecture_Bible.docx) | Kiến trúc vật lý, mặt cắt, access, luồng, tính thời kỳ và provenance theo địa điểm. | Core 04 ghép vào beat, Core 07 Locations và Assets. |
| [Core 04](../04_Level_Chapter_Design.docx) | Dàn cảnh, trigger, checkpoint, route và thứ tự người chơi gặp thông tin. | Core 07 Events, Chapter gates, Puzzle specs; Core 06 giao dịch. |
| [Core 05](../05_Art_Audio_Bible.docx) | Dáng, ánh sáng, vật liệu, ký hiệu, audio, giọng đọc, asset-ready và nguồn quyền. | Core 07 Assets, Research, Dialogue; quy tắc QA AR/AU. |
| [Core 06](../06_Technical_Design_Document.docx) | Module, schema, validator, gate condition, save, transactions, performance. | Core 07 Puzzle specs, Chapter gates, QA; code/build cho bằng chứng triển khai. |
| [Core 07](../07_Production_Database.xlsx) | Master câu thoại, full inspect copy, ID, Backlog và hợp đồng nghiệm thu. Quyết định thiết kế và lịch sử theo Decisions/Changelog. | Dẫn về nguồn sở hữu trước khi đồng bộ đoạn trích; kết quả kỹ thuật nằm ở log và [Live Production Ledger](production-ledger.md). |

Nếu văn bản trích trong Core 04 khác `07/Dialogue`, dùng đúng beat/context và tìm DEC có thẩm quyền trước khi cho phát. Khi thấy chỉ dẫn mâu thuẫn hoặc lỗi thời, áp dụng quy tắc ở [AGENTS.md](../../AGENTS.md). Đọc log kỹ thuật theo revision và phạm vi, sau đó dùng người có thẩm quyền xác nhận cổng tương ứng.

## Sự kiện, lớp thông tin và ranh giới tiết lộ

| Mã nguồn | Dữ kiện đã khóa | Suy luận được phép trên tuyến | Đề xuất, câu hỏi chờ hoặc chủ ý để ngỏ |
| --- | --- | --- | --- |
| CAN01, EVT002/003, E03, E05/E05b, E06a, PROP-HD03 | An Sinh thành lập 1987. Thỏa thuận 2003 tài trợ 88 giường công khai đổi lấy 12 vị trí Khu C ngoài sổ chính. Vĩnh duyệt bịt cửa đông 06/2004. | CH03 biết Khu C từng có người. CH04 gặp băng họp 08/2003. CH05 nối hồ sơ hoàn công với dấu vữa. CH06 gặp hợp đồng 07/2003. | Cue và câu đọc thêm về Vĩnh chờ duyệt. |
| CAN02/08, EVT013/022, E10a, E01-A, E04 | Canon nội bộ 13 thi thể thu hồi gồm 11 bệnh nhân và Lan chết do cháy, cộng N-04 vô danh chết trước. E10a là tin sơ bộ 13 tử vong, gồm một nữ điều dưỡng, nghi chập điện. E01-A khai 0 bệnh nhân tồn đọng B1 và mô tả B1 là kho. E04 ghi tiếp nhận sơ bộ 13 thi thể thu hồi trong Khu C. | CH01 biết tin báo; CH04 biết tiếp nhận và nơi thu hồi Lan theo H05; CH06 hỏi vì sao danh mục điều trị bỏ Khu C; CH08 phân loại. | Hướng A+B đã duyệt cho thiết kế nội bộ. Hồ sơ công bố ngoài mẩu báo, danh tính hoặc phân loại công khai cụ thể của 12 trường hợp còn lại và nghiệp vụ báo cáo chờ TASK028/owner. |
| CAN03/05, EVT014/026, E02/E07/E08/E09 | 03:09–03:12 Minh giữ chìa và đi cứu 6 người; 03:17 mất liên lạc. ~03:25 trở lại kính và rụt tay; 04:15 giao chìa; 07:40 ký; can thiệp diễn ra sau ký. | CH04 thấy ghi nhận trên sổ; CH05 đi theo ký ức sơ tán; CH07 thấy chuỗi M và đối chất PZ10. E08 chỉ là góc nhìn Minh trong bệnh viện. | Giờ tử vong từng người và giờ can thiệp chưa xác lập. Thời điểm tiếp cận, khói và diễn biến ứng cứu chờ TASK027. Giữ câu trung tính đã duyệt cho dossier. |
| CAN04/07, EVT015/023, E10b, Provenance E01-A/B | Vĩnh giữ bản lưu nội bộ hai mặt ký 07:40. Bảy giữ bản sao riêng 2005–06, đơn bị trả năm 2006, thấy và đối chiếu bản lưu LOC08 khi kiểm kê 11/2012. Hạn theo dõi đến 18/11/2012 ghi trên E01-B; việc thúc đẩy lịch tháo dỡ nằm ở E10b. | CH06 thấy bản lưu và danh mục. Bảy tập hợp bản sao tách địa điểm. Lan chỉ biết giấy mình đã nhờ giữ. | Phạm vi thu hồi và chuỗi lưu 2006–2012 là đề xuất tại [hồ sơ canon](canon-proposals.md), chờ TASK028 và owner. |
| CHAR001/002/003, CAN03/04, LOG05/10 | Minh nhớ giữ và từ chối mở; phần thấy khoảng 03:25 và ý nghĩa việc ký bị mờ. Lan thỏa hiệp rồi dùng E03 sửa sai. Vĩnh biết lệnh sai và vụ thanh lý nhưng không chứng kiến ~03:25 qua kính. | CH04 B06: `CH01-D16b` và `CH04-D06` dùng H05/PT02 đúng guard; 07:40 theo Minh; CH08 Lan phản hồi điều Minh kể. | Cut thoại và 3 nhịp tùy chọn mới để review. Lan không biết kết quả gói Bảy, C12 ở đâu hay việc xảy ra 2012 do tự chứng kiến. |
| CAN06, WR01–06/08 | R là nguồn vật lý. B là phản xạ phòng vệ nhận thức. M là ký ức cố định, hữu hạn. Dư âm của người mất khu trú tại An Sinh. C12 là người sống, dư ảnh B/M nếu dùng chỉ thụ động. | B/M gợi hướng nhưng vật chứng đủ nguồn R quyết định export; E08 nonexport. 03:17 dùng theo ngữ cảnh mất liên lạc hoặc số giờ trên phiếu. | Vẫn cần thử độ rõ của từng lớp trong low light, mute và khi skip. |
| CAN07/09, WR07, DEC-V33-03, LOG11 | Bảy nộp phần ngoài ở cả bốn END. END01 giữ phiếu 0; END02 giữ hồ sơ; END03 nộp gói đủ nguồn; END04 thêm tự khai và biên nhận độc lập. | CH06 phân nguồn; CH08 B05 xem trước; B07 coda; B08 chỉ END04. Thấy lời mời giải trình ở END03 theo diễn biến của nhánh. | Mức đọc được của ảnh E01-B chờ review. Khả năng được mời ở END01/02 và phán quyết pháp lý để ngỏ. |
| CAN08/09, PT06/PT07, EVT027, PROP-XK12 | C12 giao sống 02:50 với mã S-12. Điểm đến ghi Cơ sở 02. N-04 nhập lạnh 14/11, ngoài vụ cháy, được đối chiếu nhãn #12. | CH03 seed, CH04 lệch thể trạng, CH08 xác nhận 2005, B08 END04 xác nhận S-12 còn sống tại Cơ sở 02 năm 2013. | Danh tính C12, danh tính người vô danh, thời điểm chọn N-04 và năm Cơ sở 02 bắt đầu hoạt động chưa xác lập. |

Các đề xuất người xem có thể nhầm thành thông tin canon được ghi riêng ở [hồ sơ canon](canon-proposals.md). Dùng các ID trên để dẫn chiếu thay cho chép lại cùng một đoạn ở nhiều tài liệu.
