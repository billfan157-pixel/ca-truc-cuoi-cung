---
name: ctcc-narrative
description: >-
  Giữ canon và thiết kế nội dung Ca trực cuối cùng. Dùng cho kịch bản, thoại tiếng Việt, prop text, ký ức, timeline, Lan/Minh/Bảy, twist PT, setup FS, reveal, patient C01–C12, provenance và nội dung bốn ending.
---

# Canon và kiểm soát tiết lộ

## Nguồn cho task

- Tra `CAN01–09`, `WR01–08`, nhân vật và `END01–04` từ nguồn canon Core v3.6.2 (`DEC-V31–V361`, `DEC-VO-01`). [Core 02](../../../docs/02_Narrative_Bible.docx)
- Đối chiếu ngữ cảnh vật lý, thời kỳ và nguồn vật chứng, rồi tra beat, cách gặp manh mối, điều kiện và fallback của cảnh. [Core 03](../../../docs/03_World_Architecture_Bible.docx), [Core 04](../../../docs/04_Level_Chapter_Design.docx)
- Đọc `07/Timeline`, `Reveal order`, `Plot twists`, `Foreshadowing`, `Payoff`, `Patients`, `Voice Bible & Barks` và `Canon checks` theo phạm vi. `07/Dialogue` (cùng `Runtime Display Copy (Silent Mode)` và [dialogue_silent_exploration_runtime.json](../../../docs/dialogue_silent_exploration_runtime.json)) là văn bản chính cho UI/VO/subtitle (82/143 dòng thu VO; 28 `INNER_SUB_SILENT`; 12 `CUT_TO_JOURNAL`). Kiểm DEC/LOCK bị tác động và `Narrative QA`, `QA` cùng ID.

## Soạn và tích hợp

1. Tách sự thật, điều nhân vật biết/tin, điều người chơi đã xác thực và điều được phép tiết lộ tại beat. Gắn mỗi manh mối với nguồn, layer, LOC, ID FS/E/EVT/PT, hành động người chơi, payoff và fallback nếu bỏ qua/mute/load.
2. Xây cảnh quanh hành động suy luận hoặc quyết định hiện tại. Giữ cách xưng hô và giới hạn góc nhìn nhân vật từ Core 02. Vignette làm rõ đời sống bệnh nhân; C01 là định danh bắt buộc ở tutorial, các tên phụ tùy chọn, C12 giữ chưa rõ tên.
3. Mở đúng dòng `Dialogue` theo ID và đọc Chapter/Context để đăng ký cue. Kiểm xuyên qua menu tương tác, VO, subtitle, preload, transcript, replay, skip, backtrack và load. Nếu sửa copy/condition, cập nhật các bề mặt dùng cùng ID khi nằm trong phạm vi, ghi tương tác gameplay và QA bị ảnh hưởng.

## Chuỗi tiết lộ trọng yếu

Dùng `CH01 / Walkthrough B01–B08` trong [Core 04](../../../docs/04_Level_Chapter_Design.docx) cho dàn cảnh CH01 và `Core 07/Dialogue` + `Voice Bible & Barks` cho câu thoại/xưng hô. B01 nhận gói ở hộp thư ngoài cổng, E10a chống ẩm, SMS hẹn/hoãn; CH01 `Pocket Note` chỉ đọc lại tùy chọn. Ba nhánh F101 chỉ nhắc điều Minh đã ký và nguồn đã kiểm. EX01 ghi nhận mốc trùng bằng thao tác chọn đúng `03:17`, giữ cách hiểu 03:17 theo CAN05. Echo B08 thuộc lớp trình bày, không xác thực thêm người hoặc nguồn vật lý.

- CH01 gieo `E10a` và tri thức công khai: cháy khu lưu bệnh tạm thời ngày 18/11/2005, 13 tử vong (`DEC-V34-01`), Lan đã mất, Bảy gửi SMS/nhắn rồi hoãn, Minh tự vào và nhớ mình từng từ chối mở cửa. `D16` hỏi ký ức chung. `CH05-D08` là câu nghi vấn mở dựa trên `LOC09` + `E10a` (`DEC-V35-01`).
- CH04 B05 dùng `E04/E07` xác thực `H05` và trình bày `PT02`. CH04 B06 mở `CH01-D16b` theo `Chapter=CH04, Beat=B06, H05, PT02_present` (tách ý khỏi `CH04-D06` theo `DEC-V34-02`). Giữ guard cho `CH04-D06`, `D16b` chỉ trình bày thoại và giữ `G04`, `E08` độc lập.
- CH05 giữ sáu người khu B được cứu. CH06 tách mốc mất liên lạc 03:17 được dùng làm giờ hoàn tất giả và 07:40 là giờ ký phiếu nội bộ; `CH06-D15` khớp cơ chế ký tên không xác nhận số 0 (`DEC-V35-01`). `E04` là hồ sơ tiếp nhận và đối chiếu số. `E06` là giấy ghi nhận lệnh, `E06a` là băng họp, `E10b` là danh mục nguồn của Bảy.
- CH07 tái hiện trọn lời cầu cứu và lựa chọn 03:09–03:12 qua ô kính, nhịp tích tụ tâm lý `~03:25` (cứu hỏa mới tiếp cận ngoài sân chưa phá cửa B1) và chuyển thời gian tới mốc giao chìa `04:15` (`DEC-V331-02`/`DEC-V34-01`), hoàn tất `E08` sau `M`. Tách `stageDirection` khỏi `runtimeCopy` cho `CH07-D15` và `POST-D08` (`DEC-V35-01`). Khóa 6 câu thoại nâng cấp `DEC-NPR01-03` và chỉnh khẩu ngữ `DEC-V34-02`. Kẻ Giữ Cửa là hiện thân của báo cáo và tự biện hộ của Minh.
- CH08 phân loại 13 thi thể (12 cháy gồm `C01–C11` và Lan + 1 vô danh chết trước cháy) và tách riêng `C12` chuyển sống lúc 02:50 giữ khuyết danh (`CAN02/CAN08`). Chuỗi lưu giữ dùng Phương án A (`E01-A/B` tại `LOC08` do Vĩnh giấu làm bảo hiểm ngược theo `DEC-V34-01`, `E04/E07/PROP-XK12` gắn `LOC10`). Theo `DEC-V34-01`, rút `E01-B` & `PROP-XK12` khỏi bộ giấy Bảy nộp độc lập mọi ending (Bảy chỉ nộp bản sao 2005–06 + `E10b`), phân tách rõ hệ quả `END01/02` vs `END03/04`; chỉ hậu đề `END04` xác nhận S-12 còn sống tại Cơ sở 02 năm 2013. Tưởng niệm thể hiện số người và tên đã xác minh, để ngỏ kết quả pháp lý và sự tha thứ theo Core 02.

Bàn giao ID thoại/prop/cảnh và guard, kiến thức được phép, setup/payoff và fallback. Chọn FQA, LOG hoặc V22 đúng phạm vi. Ví dụ sửa D16b thì đối chiếu `LOCK-LOG10`, `LOCK-V22-02`, `V22-Q03` cùng các trạng thái trước/sau H05/PT02.
