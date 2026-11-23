---
name: ctcc-narrative
description: >-
  Giữ canon và thiết kế nội dung Ca trực cuối cùng. Dùng cho kịch bản, thoại tiếng Việt, prop text, ký ức, timeline, Lan/Minh/Bảy, twist PT, setup FS, reveal, patient C01–C12, provenance và nội dung bốn ending.
---

# Canon và kiểm soát tiết lộ

## Nguồn cho task

- Tra CAN01–07, WR01–08, nhân vật và END01–04 từ nguồn canon. [Core 02](../../../docs/02_Narrative_Bible.docx)
- Đối chiếu ngữ cảnh vật lý, thời kỳ và nguồn vật chứng, rồi tra beat, cách gặp manh mối, điều kiện và fallback của cảnh. [Core 03](../../../docs/03_World_Architecture_Bible.docx), [Core 04](../../../docs/04_Level_Chapter_Design.docx)
- Đọc `07/Timeline`, `Reveal order`, `Plot twists`, `Foreshadowing`, `Payoff`, `Patients` và `Canon checks` theo phạm vi. `07/Dialogue` là văn bản chính cho UI/VO/subtitle. Kiểm DEC/LOCK bị tác động và `Narrative QA`, `QA` cùng ID.

## Soạn và tích hợp

1. Tách sự thật, điều nhân vật biết/tin, điều người chơi đã xác thực và điều được phép tiết lộ tại beat. Gắn mỗi manh mối với nguồn, layer, LOC, ID FS/E/EVT/PT, hành động người chơi, payoff và fallback nếu bỏ qua/mute/load.
2. Xây cảnh quanh hành động suy luận hoặc quyết định hiện tại. Giữ cách xưng hô và giới hạn góc nhìn nhân vật từ Core 02. Vignette làm rõ đời sống bệnh nhân; C01 là định danh bắt buộc ở tutorial, các tên phụ tùy chọn, C12 giữ chưa rõ tên.
3. Mở đúng dòng `Dialogue` theo ID và đọc Chapter/Context để đăng ký cue. Kiểm xuyên qua menu tương tác, VO, subtitle, preload, transcript, replay, skip, backtrack và load. Nếu sửa copy/condition, cập nhật các bề mặt dùng cùng ID khi nằm trong phạm vi, ghi tương tác gameplay và QA bị ảnh hưởng.

## Chuỗi tiết lộ trọng yếu

Dùng [walkthrough B01–B08](../../../docs/ch01r/walkthrough-gameplay-B01-B08-investigation-v1.md) cho dàn cảnh CH01 và Core 07/Dialogue cho câu thoại. Ba nhánh Pocket Note chỉ nhắc điều Minh đã ký và nguồn đã kiểm. EX01 ghi nhận mốc trùng, giữ cách hiểu 03:17 theo CAN05. Echo B08 thuộc lớp trình bày, không xác thực thêm người hoặc nguồn vật lý.

- CH01 gieo E10a và tri thức công khai: 13 tử vong, Lan đã mất, Bảy gửi thư/hẹn rồi báo hoãn, Minh tự vào và nhớ mình từng từ chối mở cửa. D16 hỏi ký ức chung. Bằng chứng và hồi tưởng về địa điểm thu hồi Lan nằm ở các beat sau.
- CH04 B05 dùng E04/E07 xác thực H05 và trình bày PT02. CH04 B06 mở `CH01-D16b` theo Chapter=CH04, Beat=B06, H05 và PT02_present. ID là mã cũ để truy vết. Giữ guard cho cả dòng `CH04-D06` theo Context của nó. D16b chỉ trình bày thoại và giữ G04, E08 độc lập.
- CH05 giữ sáu người khu B được cứu. CH06 tách mốc mất liên lạc 03:17 được dùng làm giờ hoàn tất giả và 07:40 là giờ ký phiếu nội bộ. E04 là hồ sơ tiếp nhận và đối chiếu số. E06 là giấy ghi nhận lệnh, E06a là băng họp, E10b là danh mục nguồn của Bảy.
- CH07 tái hiện trọn lời cầu cứu và lựa chọn 03:09–03:12 qua ô kính, hoàn tất E08 sau M. Giữ giờ tử vong chưa xác định và quá khứ cố định. Kẻ Giữ Cửa là hiện thân của báo cáo và tự biện hộ của Minh.
- CH08 ghi nhận đủ 12 bệnh nhân và Lan. Coda Bảy nộp phần ngoài độc lập ở cả bốn ending. Khác biệt nằm ở quyết định ra đi/giữ 0, gói chứng cứ và lời khai của Minh. Tưởng niệm thể hiện số người và tên đã xác minh, để ngỏ kết quả pháp lý và sự tha thứ theo Core 02.

Bàn giao ID thoại/prop/cảnh và guard, kiến thức được phép, setup/payoff và fallback. Chọn FQA, LOG hoặc V22 đúng phạm vi. Ví dụ sửa D16b thì đối chiếu `LOCK-LOG10`, `LOCK-V22-02`, `V22-Q03` cùng các trạng thái trước/sau H05/PT02.
