# HỆ THỐNG ĐIỀU TRA — PHYSICAL EVIDENCE & REASONING
## Đặc tả gameplay dùng xuyên game, triển khai mẫu tại CH01 / F101

**Ngày thiết kế:** 23/11/2026  
**Phạm vi:** khám phá 3D, ghi nhận nguồn, so giấy–hiện trường, đánh dấu, liên kết, lập luận, ký quyết định và nhớ lại từ ba lô.  
**Trạng thái:** ĐỀ XUẤT KỸ THUẬT/UX ĐỂ REVIEW VÀ PROTOTYPE. Không phải Unity runtime đã triển khai hoặc được playtest.  
**Nguồn dự án:** Core 01/02/03/04/06/07 là tài liệu có thẩm quyền; trong đợt này đã đọc trực tiếp các nguồn text trên \`main\`: \`AGENTS.md\` (theo repo), \`docs/ch01r/puzzle-dependency-graph.md\`, \`docs/ch01r/acceptance.md\`, Walkthrough B01–B08, đặc tả ba lô, \`DeductionFramework.cs\`, \`HospitalGame.Pages.cs\`, \`HospitalGame.Journal.cs\`, \`PhysicalInspector.cs\`, \`Casework.cs\`, \`Protocol.cs\`. Các DOCX/XLSX Core chứa quy định bổ sung cần duyệt trước triển khai; không tự coi ý tưởng mới là canon.

---

## 1. MỤC ĐÍCH: NGƯỜI CHƠI PHÁT HIỆN, KHÔNG PHẢI UI TỰ GIẢI

### Tình huống người chơi phải hiểu bằng chính trải nghiệm

Một tờ F101 tại phòng 101 ghi **0** ở chỗ người chưa bàn giao. Người chơi kéo rèm, thấy vệt bánh, chăn, dép ở hốc giường 4 (OBS101A) và lật thẻ AS-05B có ô người nhận bàn giao còn trống (OBS101B). Hai nguồn có thể liên quan; người chơi tự nhận ra rằng **không đủ căn cứ để xác nhận số 0**.

Chưa có cơ sở cho rằng ai đang nằm trong buồng, ai đó cố tình giả mạo giấy tờ, hoặc 03:17 là giờ tử vong. Đây là **một câu hỏi nghiệp vụ hẹp**, không phải lời giải vụ cháy. Người chơi muốn ký nhánh C phải diễn đạt đúng phạm vi biết được; A và B vẫn là lựa chọn hợp lệ, không soft-lock.

### Bốn nguyên tắc vận hành

1. **Physical first:** sự bất thường xuất hiện trong không gian/giấy thật. UI chỉ hỗ trợ *ghi nhớ và diễn đạt*, không phát hiện hộ.
2. **Cognitive action before validation:** người chơi tự chọn *vùng nào đáng nghi*, *nguồn nào liên quan*, *mức kết luận nào đủ*; hệ thống không khen đúng ngay mỗi bước.
3. **Không ép khó ở thao tác tay:** không cần xoay đủ 37 độ, rê mép giấy tới 1 pixel hoặc vẽ chữ ký thật bằng chuột. Khó nằm ở cách nghĩ.
4. **Diegetic nhưng không lề mề:** một bảng kẹp F101, một thẻ có thể cầm, một nguồn hiện trường cố định và một tờ ghi chú nháp là đủ. Không dựng corkboard/crafting/phòng thí nghiệm mới cho CH01.

### Kết quả tối thiểu khi playtest

Người chơi phải có khả năng tự nói: **“Phiếu báo 0, nhưng hốc giường có dấu sử dụng và thẻ bàn giao chưa ký. Vì thế tôi không thể xác nhận 0; tôi chưa biết ai ở đó.”** Không đọc câu này từ một màn "Đáp án đúng" trước khi ký.

---

## 2. NGHIÊN CỨU CƠ CHẾ: LẤY NGUYÊN TẮC, KHÔNG SAO CHÉP UI

| Nguồn đã đối chiếu | Điều được chứng thực qua nguồn | Quyết định cho CTCC |
| --- | --- | --- |
| *Return of the Obra Dinn*: devlog Lucas Pope (2017), phỏng vấn Game Developer | Dùng suy luận từ quan sát, xác nhận theo nhóm để tránh dò đáp án từng bước; nhà phát triển cân đối giữa xác nhận quá nhanh và không có phản hồi | Không hiện dấu ✓ khi đánh dấu đúng ô; phản hồi từng thiếu sót chỉ ở giai đoạn review, không tiết lộ nguồn nào là “đáp án” trước khi người chơi khám phá |
| *The Case of the Golden Idol*: phỏng vấn nhà thiết kế Andrejs Klavins trên Game Developer (2022, 2023) | “Thought path” giúp thiết kế một chuỗi những điều người chơi cần hiểu; thử nghiệm cho thấy feedback toàn câu quá ít gây bế tắc, feedback từng slot gây dễ thử mò | Thiết kế cầu thang nhận thức ba bước: số 0 đáng nghi → thẻ thiếu ký → dấu hiện trường hỗ trợ; cho feedback về *loại lỗ hổng*, không phát đáp án |
| *Shadows of Doubt*: phát biểu dev ColePowered, mô tả store | Thu thập, lưu lại và nối nguồn thành mạng câu hỏi; việc ghi dấu thông tin để trở lại sau là cấu phần trải nghiệm | Ghi \`WorldObservation\` vào Pocket Note khi đã tự xem, cho gọi lại nguồn khi đối chiếu với giấy; không cho dấu vết trong sàn thành item cầm tay |
| *Scene Investigators*: mô tả phát hành | Dựa trên suy luận từ hiện trường có thể không đủ chứng cứ để đưa ra kết luận chắc chắn | Nhánh Unverified là sự thận trọng nghề nghiệp hợp lệ, không bị coi là “thua” |
| *Paradise Killer*: phỏng vấn Kaizen/PC Gamer, official Steam | Khám phá phi tuyến, cho phép trình bày kết luận với mức độ chắc chắn khác nhau; nền sự kiện cố định, cách player hiểu có thể khác | Giữ A/B/C và first/current/revision, tuyệt đối không thay đổi R/quá khứ bằng một suy đoán |
| *The Painscreek Killings*: store | Khám phá kỹ, không phụ thuộc marker/hint tuyến tính | Không vẽ mũi tên trên sàn chỉ chỗ giường 4; cho landmark, âm, ánh sáng, hint theo nhu cầu thay thế |

**Nguồn tham khảo kiểm chứng trực tiếp:**
- https://dukope.com/devlogs/obra-dinn/tig-30/
- https://www.gamedeveloper.com/design/case-of-the-golden-idol
- https://www.gamedeveloper.com/business/-the-case-of-the-golden-idol-i-used-frequent-testing-to-improve-its-mystery-solving
- https://colepowered.com/shadows-of-doubt-devblog-2-finding-the-game/
- https://store.steampowered.com/app/986130/Shadows_of_Doubt/
- https://store.steampowered.com/app/1159830/Scene_Investigators/
- https://www.pcgamer.com/how-a-new-indie-studio-created-one-of-the-best-detective-games-ever/
- https://store.steampowered.com/app/624270/The_Painscreek_Killings/

Các game khác nhau về nền tảng và mức độ phức tạp. **Thiết kế CTCC bên dưới là tổng hợp và suy luận sản phẩm riêng**, chưa có nghiên cứu nào chứng minh chính xác hiệu quả của cơ chế F101 trên người chơi CTCC.

---

## 3. VÒNG CHƠI “PHÁT HIỆN → GIỮ NGUỒN → ĐỐI CHIẾU → LẬP LUẬN”

\`\`\`text
[ĐI LẠI & NHẬN THẤY]
    ↓ player tự tiếp cận vật có lý do
[KIỂM HIỆN TRƯỜNG HOẶC SOI TÀI LIỆU 3D]
    ↓ Examined/Interpreted theo hành động có thật
[GHI NGUỒN]
    ↓ portable: cất giấy thật B tạm thời; fixed: nguồn tham khảo OBS, không cất dấu sàn
[ĐƯA NGUỒN VỀ BẢNG KẸP]
    ↓ mark hai vùng trên giấy, gọi lại OBS101A đã kiểm qua Pocket Note
[CHỌN QUAN HỆ]
    ↓ “ô nhận chưa ký” đối chiếu “vết sử dụng vị trí giường 4”
[PHÁT BIỂU GIỚI HẠN]
    ↓ chọn A/B/C; C cần xét đủ nguồn và mark/link/claim
[CHỮ KÝ & HẬU QUẢ]
    ↓ bút trên giấy → lớp B thay đổi, R/tri thức gốc giữ nguyên
\`\`\`

**Không được đánh đồng:** (1) item đang giữ, (2) observation đã thực sự xem, (3) giả thuyết Minh viết, (4) official evidence đủ xuất. Ba lô chứa đồ và các nguồn ghi nhớ của Minh; nó **không phải “máy kiểm chứng”**.

### 3.1. Bốn động từ trực tiếp

| Động từ | Tương tác của người chơi | Phản hồi trong thế giới | Trạng thái |
| --- | --- | --- | --- |
| **Quan sát** | Tiến sát/dịch góc/đèn, E kiểm dấu sàn; cầm/lật/soi giấy 3D | Một chi tiết rõ thêm; âm vải/giấy/đèn có nguồn | \`ObservationRecord.stage = Examined/Interpreted\` khi đủ điều kiện |
| **Đánh dấu** | Mở giấy trên kẹp hoặc 3D, chuyển sang bút và khoanh/gạch một vùng có ý nghĩa | Nét mực bi hiện trực tiếp, có Undo; không có pháo hoa ✓ | \`F101Annotations.marks\` một mark cho mỗi giấy có quyền đánh dấu |
| **Đối chiếu** | Đặt AS-05B cạnh F101; “nhắc lại dấu hiện trường” qua nguồn đã xem hoặc quay tới hốc 4 | Tờ nháp của Minh ghi hai nguồn song song, mỗi nguồn có provenance, không ép dây đỏ ảo | \`F101Annotations.linkFrom/linkTo\` chỉ sau lựa chọn chủ động |
| **Kết luận** | Ghi mức nhận định trên F101 rồi tự ký | Bút/giấy, ngừng nhịp, hiệu ứng B A/B/C khác nhau | \`HypothesisService.EvaluateF101\` + commit quyết định qua service |

Không đặt “kết hợp vật phẩm A+B” thành một động từ phổ quát. Vật phẩm chỉ được dùng đúng cơ chế khả dụng; không cho combine mọi cặp giống crafting game.

---

## 4. LUỒNG MẪU TOÀN BỘ CH01 / F101 TRONG GÓC NHÌN NGƯỜI CHƠI

### Cảnh 01 — Tờ F101 đầu tiên đặt ra câu hỏi

Tại cửa Phòng 101/B, người chơi thấy mẫu F101 trên kẹp AN02, trường **“Chưa bàn giao: 0”**. Nếu mở 3D, có thể xoay/lật giấy, nhìn dòng này. Không xuất hiện prompt “Hãy tìm hai manh mối”, không khóa cửa chờ người chơi mở UI.

Nếu player muốn **Affirmed** hoặc **Unverified**, có thể quay lại mẫu và ký mà không cần mở rèm. Nếu muốn bác số 0, tự tìm một lý do đáng tin trong phòng.

**Tín hiệu không lời:** ở phía trong là rèm hốc giường thứ tư, âm ray cọ nhẹ và bố cục ba giường đã được sử dụng. Bảng F101 nhìn thấy ngay trước khi đi sâu để con số 0 có ý nghĩa từ đầu.

### Cảnh 02 — Dấu vết hốc giường 4 (OBS101A)

Người chơi tự mở cửa và kéo rèm; nhìn vệt bánh giường, chăn gấp vội, dép nhựa nhỏ. Khi tới đủ gần, một prompt ngắn **“Kiểm tra dấu sử dụng”** xuất hiện; E cho một mô tả xác thực của dấu đã quan sát. Nếu dùng đèn xiên, vệt mòn rõ hơn, nhưng không yêu cầu chính xác góc chiếu mới ghi Examined.

Khi nguồn đã **Examined ở LOC03/B**, Pocket Note sinh một **mục tham chiếu**: “Dấu sử dụng tại hốc 4 · đã kiểm tại Phòng 101/B”. Đây là *dòng ghi nhớ*, không là đồ vật, không chụp màn hình tự động tạo evidence chính thức. Player có thể xem lại nguồn/mô tả sau, không cần chạy bộ về đúng vết nếu muốn nối một liên hệ ở bảng kẹp.

**Khoảnh khắc phải do player tự phát hiện:** ở giường 1–3, bố trí có quy luật; hốc 4 có dấu của một vị trí tương tự từng được dùng nhưng không có giường đúng trạng thái hiện tại. Chỉ là *dấu sử dụng trong quá khứ*, không tự nói “vẫn có bệnh nhân ở đây”.

### Cảnh 03 — AS-05B (OBS101B), giấy không giống hiện trường

Người chơi nhấc thẻ AS-05B từ giá đầu giường 4, cầm và lật 3D. Ở mặt trước có mục tình trạng và **ô người nhận bàn giao trống**. Khi người chơi focus chi tiết này, mô tả chỉ nhắc nó đang trống, không phán xét nhân viên. Nếu tò mò, lật mặt sau/đón sáng để thấy vết hằn 03:17 cho EX01 tùy chọn; nguồn thời gian không thành bằng chứng trực tiếp cho F101.

Player có thể cất thẻ vật lý tạm vào ngăn **Giấy tờ & vật chứng (B)** hoặc mang nó tới bảng kẹp F101. Lần kiểm tra giấy lưu \`OBS101B\` stage và danh sách chi tiết đã xem. **Cất giấy không tự coi như đã đánh dấu ô người nhận.**

### Cảnh 04 — Bảng kẹp F101 biến thành bàn đối chiếu thực tế

Người chơi đứng trước AN02; chọn **“Đối chiếu tại đây”**. Không chuyển sang phòng giải đố trắng tinh hoặc sơ đồ đỏ đầy màn hình. F101 nằm ở giữa; kẹp phụ bên cạnh có thể đặt một tài liệu; dưới mép kẹp là một **tờ nháp nhỏ của Minh** gồm các mục:

- **Điều giấy đang khẳng định:** ô trống để người chơi đánh dấu vùng trên chính F101.
- **Điểm bất thường của nguồn giấy:** ô trống để đánh dấu vùng trong AS-05B.
- **Điều hiện trường cho thấy:** ô để gọi lại một observation đã thực sự Examined.
- **Mức nhận định có thể bảo vệ:** người chơi tự chọn câu kết luận khi ký.

Tờ nháp là **công cụ tổ chức lập luận**, không phải vật chứng mới trong hồ sơ bệnh viện và không đổi canon. Có thể bỏ việc đối chiếu và ký A/B ngay.

**Camera và hands:** cận bàn ở góc vừa đủ, cho thấy kẹp chính và kẹp phụ; player có thể ngẩng lên nhìn hốc 4, rút/đặt giấy, lật 3D; không khóa người chơi trong một “modal ảo” đến khi giải xong. Vật không được tự duplicate khi vừa hiển thị trên tay vừa hiển thị tại kẹp.

### Cảnh 05 — Người chơi tự khoanh hai điểm trên giấy

**Mark 1:** người chơi chọn bút, bấm/khoanh trên trường **F101_ZERO**. Nét bút sẫm hiện trên giấy. Nếu người chơi khoanh “bảng kê giường 01–03” thay vì số 0, vẫn giữ nét mực đã chọn nhưng **không hiện đánh dấu đúng/sai**. Có thể tẩy/Undo và chọn lại. Nếu dùng bàn phím, người chơi chọn danh sách “Bảng kê” / “Tổng hợp: 0” và nhấn M.

**Mark 2:** lấy AS-05B và đặt ở kẹp phụ, nhìn trước/mặt sau, dùng bút khoanh vùng **OBS101B_SIGN**. Nếu người chơi khoanh “Tình trạng: Chưa bàn giao” thay vì ô nhận còn trống thì cũng có phản hồi mực thật, nhưng chưa tạo cơ sở hợp lệ để C.

Hai nét này đã có hỗ trợ metadata/annotation ở runtime hiện tại; UI mới tập trung chuyển chúng thành cử chỉ mực chân thực, không tạo một hệ vẽ tự do không thể kiểm soát. Phần so sánh chỉ bắt đầu từ một **câu hỏi:** tại sao một nguồn ghi 0 nhưng một thẻ liên quan vẫn chưa có chữ ký nhận?

### Cảnh 06 — Liên kết thẻ giấy với dấu vết trong không gian

Đây là bước quan trọng nhất, cần thiết kế khác một nút “Nối manh mối” mơ hồ.

**Cách A — tại bảng kẹp, không ép backtrack:**
1. Người chơi đã khoanh ô người nhận thiếu trên thẻ và đã Examined OBS101A tại hiện trường.
2. Trên tờ nháp hiện dòng **“Điều gì ở phòng 101 khiến ô nhận này đáng nghi?”** (câu hỏi, không phải đáp án).
3. Mở Pocket Note, chọn trong những nguồn đã thực sự kiểm: ví dụ “Dấu sử dụng hốc 4”, “Phiếu thuốc giường 3”, “Đồng hồ 03:17” nếu đã xem. Người chơi **tự chọn** “Dấu sử dụng hốc 4” để ghi vào tờ nháp.
4. Hai ghi nhận nằm cạnh nhau dưới hai tiêu đề **“Trên thẻ”** và **“Tại hiện trường”**. Nét nối bằng bút của Minh xuất hiện như một ghi chú “so nguồn”, không phải sợi dây phép thuật xuất hiện trong không khí.

**Cách B — đối chiếu trực tiếp trong căn phòng:**
1. Người chơi có thể cầm hoặc lưu tham chiếu thẻ AS-05B, quay về hốc 4 trong cùng phòng.
2. Nhìn đúng vùng \`OBS101A_TRACES\` đã kiểm và chọn hành động ngữ cảnh **“Đối chiếu với ô nhận trên AS-05B”**. Nếu không muốn raycast chính xác, chọn từ một danh sách nguồn hiện trường trong bán kính phù hợp.
3. Minh ghi cùng mối quan hệ vào tờ nháp. Quay lại F101, hai nguồn vẫn hiện trên nháp, không bắt làm lại.

**Hai cách là cùng một hành động logic**: \`linkFrom = OBS101B_SIGN\` trên OBS101B, \`linkTo = OBS101A_TRACES\` trên OBS101A. Không cho việc đặt thẻ cạnh F101 tự tạo link, và không cho một source chỉ Discovered hoặc thuộc R/M đi vào link CH01-B.

**Bảo vệ cảm giác tự tìm hiểu:** nếu người chơi chọn đồng hồ, một dòng ghi nguồn vẫn có thể nằm trong note tự do nhưng nó không hỗ trợ việc khẳng định số 0 chưa chắc chắn; không auto chuyển lựa chọn sang vết bánh. Player có thể Undo mối liên hệ, mở lại nguồn và thử lại mà không mất bản gốc hoặc bị trừ “điểm điều tra”.

### Cảnh 07 — Tự định lượng sức nặng của lập luận

Trên F101, sau khi xem giấy và hiện trường, người chơi chọn một trong các mức ý:

- **“Xác nhận 0 người chưa bàn giao”** → A, luôn hợp lệ dù ít nguồn; \`Distortion(LOC03)=1\`.
- **“Chưa đủ thông tin để chốt số 0”** → B, luôn hợp lệ; \`Distortion(LOC03)=0\`.
- **“Số 0 trên phiếu chưa đủ căn cứ xác nhận”** → thử C, validator yêu cầu đủ cặp \`OBS101A/B\`, mark đúng hai vùng, link đúng và giới hạn phát biểu; \`Distortion(LOC03)=0\`.

Ở nhánh C, có thể đưa ra **phát biểu mạnh hơn** trong trang nháp để người chơi thấy ranh giới giữa quan sát và suy luận, chẳng hạn “Có bệnh nhân còn trong phòng”, “Họ cố ý giả mạo hồ sơ”. Chúng **không phải đáp án hợp lệ**: nhà phát triển không cho phép ký C bằng cáo buộc vượt nguồn; phản hồi đưa người chơi trở lại câu hỏi *mình đã chứng minh được điều gì*, không sửa giả thuyết hộ họ.

Chữ ký F101 và hiệu ứng lớp B chỉ xảy ra **sau khi service đã kiểm tra và commit**. Người chơi có thể xem lại toàn bộ nguồn hoặc rút thẻ ra/cất lại; nếu ký A/B xong và trước T08 đổi ý, có thể sửa và ghi revision như cũ. CH01 không tặng một mã “đúng 100%” như điểm thưởng.

---

## 5. UI / INPUT CONTRACT: CẦM, XOAY, KHOANH, NỐI, GỠ

| Ngữ cảnh | Chuột | Bàn phím / gamepad / trợ năng | Điều phải thấy |
| --- | --- | --- | --- |
| Tiếp cận dấu sàn | Tự đi, chiếu sáng, E kiểm tại chỗ | E hoặc action “Kiểm dấu”; bản chữ lớn | Dấu vật lý rõ, một nguồn đã kiểm được ghi lại |
| Soi AS-05B | Kéo xoay, cuộn zoom, Space/R lật; T raking | 1–3 focus chi tiết, toggle large print | Mặt/chi tiết đúng nguồn, không tự đặt giả thuyết |
| Đưa giấy lên bảng | Chọn từ tay/túi, E đặt vào slot cạnh F101 | Chọn “Đặt nguồn cạnh F101” ở phạm vi tương tác | Giấy dịch có khối lượng/âm kẹp, xuất hiện đúng một instance |
| Đánh dấu | Click phải lên vùng giấy (chỉ khi mode bút), Undo qua E/Back | Chọn field từ list + M; không cần tọa độ nhỏ | Nét mực lưu, không hiện đáp án ✓ |
| Nối nguồn | Chọn nút “So với hiện trường” rồi chọn một nguồn đã kiểm hoặc quay lại chỉ tận nơi | Danh sách các source đã kiểm tại Pocket Note, chọn nguồn và xác nhận | Tờ nháp ghi nguồn cụ thể/provenance, có cách Undo |
| Chọn phát biểu | Chọn câu thể hiện mức chắc chắn trên phiếu | 3 nhánh A/B/C cùng truy cập được | Bản nháp vẫn xem được trước khi ký |
| Ký | Nhấn/giữ ngắn tại ô ký | Nhấn E/confirm, không trace chữ viết | Bút → commit → khoảng dừng → phòng B phản ứng |
| Rút giấy/đóng | Nhấn Esc/Back, cất hoặc đặt lại | Esc/Back, không item loss | Camera và input FPS khôi phục, anchor/paper state nhất quán |

**Không đổi keybind gốc một cách vội vã:** \`PhysicalInspector\` hiện dùng chuột phải để mark ở mặt trước, M ở chi tiết focus, Space/R lật, T nghiêng đón sáng. Prototype nên giữ những nút này, chỉ làm rõ mode bút và feedback; binding tổng thể cần kiểm với tùy chọn \`alternateBindings\` và accessibility. Trạng thái con trỏ khi đang trong UI không được đồng thời xoay người chơi.

### 5.1. Thiết kế hình ảnh của tờ nháp

Tờ nháp trông như giấy thường Minh đặt vào kẹp, không phải một template bí ẩn do bệnh viện chuẩn bị sẵn. Mực viết tay làm rõ:

\`\`\`text
PHIẾU ĐỐI CHIẾU NHÁP — MINH
Trên F101:       [vùng đã đánh dấu] — con số 0
Trên AS-05B:     [vùng đã đánh dấu] — ô người nhận còn trống
Tại hiện trường: [ghi nhận nguồn đã kiểm] — dấu sử dụng hốc 4
Vấn đề:          Hai nguồn chưa đủ để xác nhận số 0
                  (người chơi tự chọn mức kết luận)
\`\`\`

**Không tự điền “Vấn đề” trước khi player chọn claim.** Trong source nội bộ, mỗi dòng chứa id/layer/loc/revision thay vì một bản copy giấy tự bịa. Nếu chọn OBS101D hay 03:17 làm nguồn so, tờ nháp vẫn thể hiện người chơi đã chọn nó nhưng service không coi đây là lập luận đủ cho Refuted. Với large print, tab/field tương ứng đọc một danh sách nguồn rõ và có nhãn B/R.

---

## 6. PHẢN HỒI THÔNG MINH: KHÔNG PHÁT “✓ ĐÚNG” MỖI LẦN BẤM

Tách **phản hồi thao tác** và **phản hồi lập luận**:

1. **Tức thời, cơ học:** bút để lại nét mực; giấy đặt xuống kẹp; ghi chú mang nguồn và layer; Undo xóa đúng nét. Phản hồi này nói “thao tác thành công”, **không** nói “lập luận đúng”.
2. **Tại bước review trước khi ký C:** người chơi có thể chủ động bấm **“Đọc lại căn cứ”**. Nếu thiếu, chỉ thấy câu hỏi về loại khoảng trống (chưa có hiện trường, chưa có giấy, chưa đánh dấu, chưa liên hệ, nhận định vượt chứng cứ) theo trạng thái hiện có, không chỉ đích từng pixel.
3. **Khi submit C không hợp lệ:** dùng \`HypothesisOutcome\` hiện có (\`InsufficientEvidence / IrrelevantEvidence / UnsupportedConclusion / InvalidProvenance / UnexaminedEvidence\`) trả lời bằng suy nghĩ chuyên môn của Minh. Không khóa cửa; chỉnh/tẩy rồi thử lại.
4. **Gợi ý nhiều tầng có thể tắt:** sau nhiều lần submit thiếu căn cứ hoặc khi player chủ động hỏi, tầng 1 chỉ nhắc câu hỏi cần đối chiếu; tầng 2 nói nên tìm một nguồn giấy bàn giao và một dấu vật lý; tầng 3 mới chỉ khu vực rèm/giá kẹp. Không liên tục phát gợi ý khi người chơi đang tự thử.
5. **Không phạt thử sai:** không tịch thu AS-05B, không làm mất mực đã chọn khi UI đóng, không reset nguồn đã Examined; chỉ có hậu quả khi một chữ ký hợp lệ được commit.

### Ma trận phản hồi mẫu

| Hành động/ý định | Vì sao không chứng minh C | Phản hồi gợi suy nghĩ |
| --- | --- | --- |
| Khoanh F101 số 0 nhưng chưa xem phòng | Chưa có nguồn vật lý phản biện | “Mình mới chỉ thấy điều tờ phiếu ghi.” |
| Có AS-05B nhưng chưa kiểm dấu sàn | Giấy có thể còn sót lại | “Tờ thẻ chưa đủ. Nó ứng với dấu nào trong phòng?” |
| Có OBS101A nhưng chưa soi ô nhận | Chưa biết trạng thái bàn giao trên giấy | “Dấu sử dụng có thật; giấy tờ nói gì về nó?” |
| Mark đúng nhưng link sang giường 3 hoặc đồng hồ | Không hỗ trợ tình trạng hốc 4 | “Hai ghi nhận này chưa trả lời cùng một câu hỏi.” |
| Có A/B nhưng chọn claim cố ý làm giả | Cấp độ khẳng định vượt chứng cứ | “Không có cơ sở để nói người ta cố tình làm việc này.” |
| Có A/B đúng + mark/link/claim đúng | Đủ để ghi nhận **không thể chứng nhận số 0** | Chữ ký hợp lệ; hậu quả thế giới thay đổi; **không trao câu trả lời toàn vụ** |
| Bỏ tất cả nguồn và chọn Unverified | Không đủ cơ sở nhưng người chơi biết giới hạn | Hợp lệ, không làm bị kẹt, không xem là thất bại |

**Nguồn thoại:** các câu trong bảng là *copy hướng dẫn UX đề xuất*, không phát dưới ID thoại chính thức khi chưa duyệt kho \`07/Dialogue\`. Nếu chưa có VO, ưu tiên caption ngắn hoặc chữ viết trên tờ nháp. Không để một lời gợi ý “nghiệp vụ” vô ý tiết lộ C01, Khu C hoặc giờ tử vong.

---

## 7. HỢP ĐỒNG DỮ LIỆU / TÁI SỬ DỤNG CODE HIỆN CÓ

### 7.1. Những phần đã có và nên tận dụng

- \`PhysicalInspector\`: giấy 3D hai mặt, xoay/zoom/flip/raking, \`InspectAnnotationField\`, thao tác \`MarkAtPointer\` và \`SetMarkedField\`. Có thể giữ hệ field rectangles theo template F101/AS-05B, vẽ mực mới dựa trên ID; đừng xây physics pen tự do ngay.
- \`DeductionFramework.F101Annotations\`: hai \`marks\` theo nguồn + \`linkFrom/linkTo\`; contract \`SupportsRefutation()\` đòi \`F101_ZERO\` và \`OBS101B_SIGN → OBS101A_TRACES\`.
- \`ObservationRegistry/ObservationRecord\`: \`Discovered/Examined/Interpreted/Corroborated\` và provenance \`B / LOC03 / CH01\`. Chỉ cấp tag nguồn sau khi Examined, **không** sau khi nhìn hình trong cinematic.
- \`HypothesisService.EvaluateF101\`: kết luận có căn cứ, các loại lỗi chuyên môn, A/B hợp lệ không cần nguồn. \`FormService\` và \`RealityTransaction\` commit signed decision/history, rồi thế giới B phản ứng.
- \`HospitalGame.Pages/Journal\`: có action \`ToggleF101MarkAsync\`, \`ToggleF101TraceLinkAsync\`, lựa chọn nguồn và UI so tài liệu hiện hành. Chuyển phần nhập sang bảng kẹp 3D + Pocket Note; không bãi bỏ service đang kiểm chứng chính thức.
- \`Protocol.Schema = 6\`: sau nghiên cứu chưa có reason để tự ý thay schema chỉ vì thay visual của field/link; tận dụng cấu trúc hiện có cho prototype, triển khai inventory ownership/schema theo tài liệu Ba lô riêng sau review.

### 7.2. Những service/scene component được đề xuất thêm

| Component đề xuất | Trách nhiệm | Không được làm |
| --- | --- | --- |
| \`InvestigationFocus\` | Raycast và nhận biết nguồn tương tác, state camera/hand | Tự cấp chứng cứ theo góc nhìn đơn thuần |
| \`EvidenceWorkbench\` | Đặt F101 anchored, slot so AS-05B, paper positioning, stage/exit | Tự thêm marks/link hoặc tự sinh một F101 bản sao |
| \`AnnotationPresenter\` | Nét bút theo đúng hotspot đã đăng ký, undo, trạng thái mark | Tự quyết định mark đúng hoặc cấp G01 |
| \`WorldObservationReference\` | Biểu diễn một OBS cố định đã Examined từ Pocket Note, kèm layer/LOC | Biến dấu sàn thành vật phẩm gốc/evidence được export |
| \`RelationshipPresenter\` | Hai cách chủ động tạo link (bàn kẹp hoặc chỉ tận nơi), visual trên nháp | Chọn đúng nguồn giùm người chơi, auto-link khi đặt giấy gần |
| \`ReasoningDraftPresenter\` | Bốn dòng nháp, các source được chọn và câu kết luận | Làm thay việc suy luận, sửa lại source nếu player chọn “sai” |
| \`EvidenceReasoningController\` | Gom giao dịch UI -> Casework, validate nguồn/retry, đóng/mở/khi save | Ghi thẳng \`state.evidence\`, bypass \`EvaluateF101\`, thay đổi geometry R |

Tên class chỉ là đề xuất; khi triển khai có thể gom service thay vì tạo quá nhiều MonoBehaviour. Giao dịch và provenance vẫn là authoritative, scene chỉ trình bày.

### 7.3. Mapping hành động mới lên state cũ

\`\`\`text
PLAYER ACTION                               AUTHORITATIVE DATA
Examine traces at bed 4 in B           →     OBS101A Examined, B/LOC03/CH01
Inspect blank receiver on AS-05B       →     OBS101B Examined, B/LOC03/CH01
Circle zero on F101                     →     marks[F101]    = F101_ZERO
Circle blank receiver on AS-05B         →     marks[OBS101B] = OBS101B_SIGN
Choose trace source to relate to card   →     linkFrom       = (OBS101B, OBS101B_SIGN)
                                            linkTo          = (OBS101A, OBS101A_TRACES)
Select narrow claim                     →     claimId         = F101_CLAIM_UNRELIABLE_ZERO
Attempt Refuted                         →     selectedEvidenceIds = [F101, OBS101A, OBS101B]
                                            annotations.Version = 1
                                            EvaluateF101(...) then commit
Sign Affirmed / Unverified              →     EvaluateF101 always valid without sources
\`\`\`

**Lưu ý quan trọng khi triển khai:** trong code \`EvaluateF101\` hiện có đường tương thích bản lưu cũ khi \`annotationVersion=0\`; **không được để thao tác UI mới submit phiên bản 0 rồi bỏ qua marks/link**. Form submit mới từ workbench phải phát \`F101Annotations.Version\`, có \`annotations\` thật; chỉ bản lưu cũ đã migration mới được phép dùng đường legacy theo hợp đồng đã duyệt. Trước khi sửa \`DeductionFramework\`, viết regression test cho cả save v1–6, không phá những chữ ký C hợp lệ trước đó.

**Lưu trữ UI không được âm thầm “thật hóa” source B:** một paper prop có thể vào túi tạm, nhưng ghi nhận chính thức vẫn là \`ObservationRecord\`, không tăng \`evidence\`. Khi B→R, source reference còn nhưng giấy B vật lý không equip/use/export. Chữ ký F101 và bản nháp của Minh có thể được xem lại trong Pocket Note, không tự phát sinh bản giấy F101 gốc trong ba lô.

### 7.4. Tính lặp lại và đường nâng cấp chương

Bộ 4 động từ có thể dùng ở:
- **CH02:** so \`LIST\` (bảng ghi) với \`SOCKETS/WHEELS\` (nguồn R cố định) để mở PZ01/E12, rồi xem đồ thật của E11; không dùng lại mẫu F101 hay giật trước danh tính C01.
- **CH03–CH06:** một tờ giấy, băng/cassette hoặc một vật cá nhân đặt cạnh nguồn khác; quan hệ nghiệp vụ khác nhau có thể dùng các loại “xác nhận, mâu thuẫn, liên hệ thời gian” được author.
- **CH07 M:** cho phép player ghi nhận và đối chiếu ký ức, nhưng **E08 nonexport**, không biến ký ức thành item physical.
- **CH08:** xây lập luận kết luận và gói xuất từ nguồn có provenance, không để hình ảnh bản B giả làm vật chứng R.

Không ép mỗi chapter vào 4 ô câu hỏi giống nhau. Reuse ở **hệ nguồn + cử chỉ + authoritative validation**, còn cách trình bày, mức khó và claim do narrative/gameplay author cho mỗi trường hợp.

---

## 8. VÙNG AN TOÀN, SAVE/LOAD VÀ ACCESSIBILITY

**Không mất vật:** mọi “đặt lên kẹp / lấy khỏi túi / bỏ lại bàn” đều có anchor stable. Nếu UI đóng, reload hoặc scene unload, một paper vẫn có một instance; ngăn túi/slot không tạo copy. Không trừ durability/bút/mực trong puzzle này.

**Input focus:** khi mở Workbench, gameplay camera và modal phải có luật sở hữu input duy nhất. Không nhận E đồng thời trên quầy và trên thẻ. Khi đóng/skip/reducedMotion, trả FPS yaw/pitch/FOV/cursor/hand đúng trạng thái.

**Save/Load:** mark, link và claim draft lưu qua cấu trúc có sẵn hoặc giao dịch tương ứng; first/current/revision của F101 giữ nguyên. Save giữa hai mark sẽ giữ đúng một mark; save khi thẻ đặt kẹp phải lưu anchor; save sau ký A/B/C phải dựng lại đúng variant B. Save fail rollback trước khi đổi geometry. Không để signed A làm mất OBS101A đã thấy hoặc chặn quay lại sửa.

**Provenance:** B-only item không thể mang thành bản gốc qua T08; R của CH02 vẫn có \`LIST/SOCKETS/WHEELS\`, \`PZ01/E12\` và \`E11\` độc lập. CH01 chỉ có E10a trong evidence, không đưa C01/Khu C/CH04-D06/CH01-D16b/E08 vào sớm.

**Khả năng tiếp cận:** thao tác chuột chính xác có danh sách field/hotspot thay thế; long press không bắt buộc; dừng camera lắc khi reduceMotion; bản chữ lớn phản ánh cả nét mực trên F101/AS-05B và liên hệ đã ghi; caption theo hướng cho các cue âm, không cần nghe 03:17 để hoàn thành C. Không có jumpscare khi mở workbench hoặc inspector.

---

## 9. TRIỂN KHAI THEO LÁT CẮT & ĐIỀU KIỆN NGHIỆM THU

| Giai đoạn | Dựng những gì | Khi nào được coi là xong |
| --- | --- | --- |
| **IR-00 · Logic audit** | Đối chiếu Core 01/02/04/06/07; source & claim F101, quyền kết luận, save v6 legacy; khoanh path \`annotationVersion=0\` | Chủ sở hữu gameplay/narrative xác nhận không đổi canon; unit test baseline |
| **IR-01 · Greybox 101** | Vị trí F101, AS-05B, rèm/hốc 4, một kẹp phụ và giấy nháp | FPS có thể qua lại giữa dấu sàn và kẹp; không tạo path/collider kẹt |
| **IR-02 · Interaction** | Tương tác 3D cầm/đặt, mark cả hai giấy, undo, source reference | Keyboard-only + mouse + fallback truy cập được; không tự auto-credit link |
| **IR-03 · Logic bridge** | Link from/to, claim selection, mapping \`DeductionSubmission\` version1, EvaluateF101 | A/B không có nguồn pass; C đúng pass; C thiếu nguồn/mark/link/claim fail đúng |
| **IR-04 · Presentation** | Nét bút, giấy sột soạt, bản nháp, ánh đèn và world reaction F101 | Không “✓ đúng” khi mark, không kích scare trong inspector, 3 branch khác nhau |
| **IR-05 · Cross-layer QA** | Save/reload, B→R giấy tạm, F101 revision, CH02 independent | 30 swap/load/cancel không mất giấy, không tăng evidence CH01, R không méo |
| **IR-06 · Blind test** | Ít nhất TASK010 6 người mới + review TASK026–028, telemetry hành động | Có ghi video/sự kiện thời gian và câu hỏi sau chơi; điều chỉnh dựa trên hiểu biết chứ không theo lời khen |

### 9.1. Bộ test tối thiểu

1. Đi vào 101, **ký A ngay** → pass và G01; không cần kéo rèm.
2. Đi vào 101, **ký B ngay** → pass và G01; không có D=1.
3. Đọc thẻ AS-05B nhưng không xem dấu sàn, mark đúng hết → C không pass; feedback thiếu hiện trường.
4. Xem dấu sàn, không soi thẻ → C không pass; feedback thiếu hồ sơ.
5. Xem cả A+B, nhưng mark F101_TABLE thay F101_ZERO → không pass; không xóa nguồn hay bắt đọc lại.
6. Xem cả A+B, mark OBS101B_STATUS thay OBS101B_SIGN → không pass.
7. Mark đúng cả hai, **link sai** sang OBS101D hoặc đồng hồ → không pass; không tự đổi source.
8. Link đúng nhưng chọn claim “Có bệnh nhân còn ở phòng” → không pass vì suy diễn vượt nguồn.
9. Toàn bộ A+B + mark + link + claim đúng → C pass; Corroborated/first/current/revision và G01 đúng.
10. Đang draft C, đóng UI, lấy thẻ về túi, reload, đặt lên kẹp lại → giữ mark/link, không nhân item.
11. Ký A rồi quay lại C hợp lệ, rồi B → history/revision tăng đúng, geometry B theo current, first bất biến.
12. Cầm thẻ AS-05B khi T08 B→R → vật B mất quyền equip nhưng nguồn đã đọc còn; không có chứng cứ B trong \`state.evidence\`.
13. Trong CH02-R, kiểm \`LIST/SOCKETS/WHEELS\` độc lập dù CH01 ký A/B/C, và không tự có E11/E12.
14. Mute/reducedMotion/mono/alternate keys/large print → đủ dữ kiện và mọi branch, không yêu cầu pixel-perfect.
15. Save fail khi ký/đổi lớp → không half signed, không biến dạng world khi commit chưa hợp lệ, không mất paper anchor.
16. Người chơi bỏ qua thẻ và đi tiếp A/B → vẫn hiểu bản thân đã ký một phiếu, hậu quả B vẫn là nội dung có thể quan sát.

### 9.2. Đo trải nghiệm — mục tiêu, không giả vờ đã kiểm chứng

Nhóm 6 người chơi mới TASK010 được chia theo route tự nhiên, không mớm câu. Thu:
- \`time_first_F101_seen\`, \`time_OBS101A_examined\`, \`time_OBS101B_receiver_seen\`, \`time_first_mark\`, \`time_first_link\`, \`time_decision\`.
- \`wrong_mark_type\`, \`wrong_link_source\`, \`reasoning_attempts\`, \`time_world_vs_ui\`, \`paper_placed_count\`, \`abandon_reasoning_to_choose_B\`.
- Hỏi mở: “F101 ghi điều gì?”, “Vì sao ô ký trống có/không có liên quan tới dấu sàn?”, “Nếu chọn C, câu nào là kết luận mạnh quá mức?”, “Bạn có đang thử bấm đúng nút hay thật sự suy luận?”.

**Không đặt đích cố định 4/6 hay giảm X% click như tiêu chuẩn phổ quát.** Quan sát hành động và lời giải thích cùng lúc: nếu người chơi đoán đúng C nhưng không thể giải thích, thiết kế chưa thực sự dạy cách điều tra. Nếu người chơi hiểu đúng nhưng khó đặt giấy/đánh dấu, cần sửa UX chứ không làm dễ logic.

---

## 10. QUYẾT ĐỊNH PHẢI REVIEW TRƯỚC KHI CÓ CODE

- Bàn so phụ ở AN02 có không gian phù hợp với Core 03/04 và asset?
- Có cần vật thể tờ nháp thật ở bảng kẹp, hay trình bày như một tờ gấp thuộc Pocket Note để tránh thêm prop?
- UI từ ba lô ra F101 có thật sự trong 1–2 thao tác không? Có cần quick action “So với F101” khi đang cầm AS-05B?
- Có thể giữ serialization của marks/link và F101 original trong Schema v6 trước khi inventory schema v7 đã được duyệt?
- Nên hiển thị những claim sai như các mức lập luận hoặc chỉ hiển thị khi player muốn bác bỏ C? Cần blind playtest để tránh biến việc chọn C thành multiple-choice quiz.
- Copy mọi lời Minh trong phần hướng dẫn phải so \`07/Dialogue\`, không đưa vào VO chưa duyệt; bản chữ lớn phải cùng nội dung.
- Nếu mới làm được **một lát cắt**, ưu tiên **F101 + AS-05B + dấu sàn/OBS101A + mark + link + claim + quyết định** trước; không mở hàng loạt giấy tình tiết ngoài nhiệm vụ này.

**Tuyên bố phạm vi:** đây là thiết kế tái sử dụng để tham khảo và thực thi; dữ liệu và implementation actual vẫn thuộc Core 06/07 cùng các file Runtime. Không tự coi các tên IR/Workbench mới là registry canonical hoặc tính năng đã được ship.
