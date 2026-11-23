# CA TRỰC CUỐI CÙNG — CHƯƠNG 1: NGƯỜI TRỞ VỀ
**WALKTHROUGH GAMEPLAY B01–B08**

**Thể loại:** Kinh dị tâm lý góc nhìn thứ nhất, khám phá hiện trường và điều tra bằng vật thể 3D.  
**Bối cảnh:** Bệnh viện An Sinh; đêm 17/11/2012 và ca trực 17/11/2005.  
**Thời lượng thiết kế:** 20,5–30 phút cho tuyến chính; thêm khoảng 6–10 phút nếu khám phá sâu. Thời lượng cần xác minh qua playtest.  
**Trạng thái:** Kịch bản thiết kế gameplay hoàn chỉnh để dựng và kiểm thử; các tương tác ba lô và cảnh điện ảnh được mô tả là đích triển khai, chưa phải tính năng đã có trong bản build.

---

## I. TRẢI NGHIỆM XUYÊN SUỐT CHƯƠNG 1

### 1. Người chơi là Minh, không phải một con trỏ đi thu thập giấy tờ

Minh quay lại An Sinh vì lá thư cũ, lời hẹn của Bảy và một chuyện trong quá khứ anh chưa đối diện được. Ngay khi bước vào bệnh viện, anh gặp những dấu hiệu không thể giải thích; một người quen tưởng như đã mất đang tiếp tục ca trực năm 2005. Cô đưa Minh đến một phiếu ghi “0”, trong khi căn phòng có dấu vết cho thấy thực tế không đơn giản như vậy.

Người chơi không có nhiệm vụ “tìm đáp án game giấu”. Người chơi **quan sát một sự việc, chọn công cụ thích hợp, thử kiểm tra điều nghi ngờ, tự đối chiếu các nguồn và quyết định mình có đủ chắc chắn để ký giấy hay không**.

Mỗi tương tác quan trọng phải đạt ít nhất một trong ba tác dụng: mở ra một câu hỏi mới, tăng hoặc giảm độ tin cậy của giả thuyết, hoặc khiến Minh trực tiếp chịu hậu quả của điều mình chọn. Không đặt mini-game kéo đồ, tìm công tắc hay ghép giấy chỉ để kéo dài thời gian.

### 2. Bộ thao tác chính

Ba lô điều tra mở bằng **Tab** (hoặc **J** theo thiết lập phím thay thế). Minh có thể kiểm lại vật đang giữ ở bất cứ khu vực an toàn nào. Giao diện có ba ngăn nhìn thấy:

| Ngăn | Chứa những gì | Tác dụng |
| --- | --- | --- |
| **Dụng cụ & đồ vật** | Đèn pin, chìa khóa, những món nhỏ được phép cầm | Trang bị, lấy ra, soi 3D và dùng trực tiếp với vật thể có mục đích |
| **Giấy tờ & vật chứng** | Phong bì E10a, BOARD, các giấy tờ đã thực sự cất, vật chứng R thu thập hợp lệ; phân biệt giấy tạm lớp B | Lấy ra, lật, nghiêng, soi chi tiết, đặt lên vị trí đối chiếu; luôn nhận diện nguồn/lớp |
| **Pocket Note** | 1–2 dòng Minh tự ghi về việc đang làm hoặc điều khó hiểu | Nhắc câu hỏi, không chấm đúng/sai; có thể mở phần ghi chép nguồn đầy đủ nếu cần |

Nhấn **E** để tương tác với vật trong tầm tay; đối với cửa, ngăn kéo và rèm, có thể **nhấn E** hoặc **giữ và kéo**. Khi soi vật phẩm bằng PhysicalInspector: xoay/thu phóng, lật bằng Space hoặc R, nghiêng đón sáng và tập trung vào chi tiết. Bản chữ lớn, điều khiển bàn phím và lựa chọn thay thế cho kéo chuột phải cung cấp thông tin tương đương.

Một vật ở hiện trường có thể được **xem** mà không được **lấy**. Ví dụ: vết bánh, ổ cắm, đồng hồ và cánh cửa vẫn ở vị trí cũ; Minh chỉ có thể nhớ/ghi nguồn quan sát, không thể nhét cả hiện trường vào ba lô. Tài liệu thuộc lớp B có thể được cầm và cất tạm trong lúc đang ở B; khi quay về hiện thực R, bản giấy ấy không còn là vật thể có thể đem sử dụng, nhưng ghi nhớ về việc đã đọc vẫn còn. Vật chứng chính thức là một hệ riêng: **Chương 1 chỉ có E10a trong danh sách evidence**.

### 3. Không gian và tiến trình

Tuyến chính đi theo đường:

**Xe máy và cổng phụ (LOC01/R) → sân và kính sảnh → quầy AN01 (R→B) → hành lang / STAFF tùy chọn → Phòng 101 (LOC03/B) → bảng ký F101 tại AN02 → quầy AN01 (B→R) → cổng phụ và vòng lặp một lần → AN01 → Chương 2 (R).**

Người chơi có thể quay lại các điểm chưa kiểm, đọc giấy theo thứ tự khác hoặc bỏ khám phá phụ. Riêng các thay đổi hiện thực R/B đều dùng giao dịch và checkpoint an toàn, không đưa Minh vào collider hoặc khóa lối khi quay về.

---

## II. WALKTHROUGH CHI TIẾT

### B01 — BỨC THƯ KHÔNG CÒN LỜI HẸN
**Khu vực:** LOC01, hiện thực R (2012).  
**Thời lượng:** 1,5–2,5 phút tuyến chính.  
**Cảm xúc:** Dè chừng, tò mò, có một lý do cá nhân chưa nói hết.

#### Cảnh mở

Mưa đêm rơi xuống sân xi măng. Xe máy Minh dừng cạnh lối cổng phụ bệnh viện. Đèn đường không đủ soi toàn bộ cổng sắt. Người chơi có thể di chuyển vài bước quanh xe trước khi chú ý đến phong bì Bảy để lại. Không mở đầu bằng cutscene dẫn tay hay camera lia ép vào bì thư: vị trí đặt, ánh đèn và âm giấy bị gió tạt là gợi hướng tự nhiên.

#### Trình tự người chơi

1. **Quan sát nơi mình vừa tới.** Người chơi có thể nhìn vệt lốp mới, chiếc xe, bảng tên cũ và biển thông báo tháo dỡ. Vết lốp là chi tiết bối cảnh, không được biến thành “dấu vết thủ phạm”. Nếu bỏ qua, vẫn mở được cổng.

2. **Tự nhấc E10a.** Nhìn phong bì ở gần xe rồi nhấn E: Minh cầm vật lên trước camera. Người chơi có thể xoay, mở nếp gấp, xem thư cũ của Lan, lời hẹn Bảy, mẩu báo về vụ cháy rạng sáng **18/11/2005 khiến 13 người tử vong**, và tin nhắn Bảy báo hoãn. Nội dung thoại/tin nhắn giữ theo các dòng đã đăng ký, đặc biệt CH01-D15; không thay bằng dòng giải thích mới. Bản chữ lớn có thể mở trực tiếp.

3. **Tìm đúng chìa.** Bên trong phong bì có **chìa cổng phụ** và **chìa khu C đã thay khóa**, đúng thông tin đã có của E10a. Người chơi nhấc chìa cổng phụ ra, cất phong bì vào ngăn giấy và có thể cất chìa còn lại vào ngăn dụng cụ. Chìa khu C chỉ là một món chưa hiểu mục đích trong chương này; không có lối đi bắt người chơi tới Khu C hay thử khóa tại đây.

4. **Quyết định tự mở cổng.** Cầm chìa phụ, tiến đến ổ khóa GATE-CONG, sử dụng chìa và xoay chốt. Nếu chỉ nhấn E trên cổng sau khi đọc đủ thư/tin nhắn, game cung cấp thao tác rút chìa phù hợp thay vì bắt lặp mở ba lô vô ích. Cổng chỉ mở khi giao dịch CH01-T02 hợp lệ; chưa đọc tin nhắn sẽ có phản hồi cần kiểm lại bì thư, không phải “đồ sai”.

5. **Bước vào.** Người chơi chủ động đẩy cánh cổng, mưa nặng hạt phủ khoảng sân phía trong. Camera không tự xoay lại chiếc xe. Quyết định đầu tiên của Minh là **đi vào dù cuộc hẹn bị hoãn**.

#### Từng bước trong cảnh: vật thể, tay cầm và chú ý của Minh

| Khoảnh khắc | Người chơi làm gì | Phản hồi phải nhìn/nghe được | Câu hỏi được đặt ra |
| --- | --- | --- | --- |
| Trước khi đụng E10a | Tự nhìn xe, cửa phụ, thông báo trong vùng tối; có thể thử tay nắm cổng | Cổng chưa nhúc nhích, chìa không tự xuất hiện; phong bì dưới đèn vẫn là điểm nhìn | Bảy đã hoãn; tại sao Minh vẫn có mặt? |
| Chạm phong bì | Bàn tay nâng E10a khỏi bề mặt, không biến mất tức thì vào HUD | Âm giấy ướt, nếp gấp có độ dày; bàn tay giữ yên khi vào inspector | Ai chuẩn bị để Minh đọc chuyện này? |
| Mở từng lớp | Lật mặt, hé phong bì, soi thư Lan và tin Bảy, quay qua mẩu báo | Các mặt giấy dùng texture và transcript đúng nguồn; đọc theo thứ tự bất kỳ, không tự đọc hộ câu trả lời | Lời hẹn và lời báo hoãn có ý nghĩa gì? |
| Nhìn cụm chìa | Rút chìa cổng, cất chìa còn lại; xoay chìa trước ống kính nếu muốn | Đồ vật nhỏ vẫn có hình và âm kim loại, ngăn dụng cụ hiện item thực sự đã nhận | Tại sao cùng phong bì lại có một chìa khác? |
| Gặp ổ khóa | Đưa chìa tới ổ, thao tác xoay, mở chốt, đẩy cổng | Ánh kim loại gỉ, tiếng khóa rời và lực cánh cửa; không có animation điện ảnh lấy mất camera | Chính Minh đã lựa chọn đi tiếp |

**Nhịp thao tác:** khi nhấc E10a lần đầu, giữ vật ở vị trí thuận tay, không lập tức chuyển tới trang văn bản nguyên khối. Bì thư là một vật thể có ít nhất mặt ngoài và khoang chứa; nội dung thư, mẩu báo và chìa phải được tiếp cận qua một hành động có hình. Không buộc player xoay đủ một góc pixel-perfect: vùng lật và nút trợ năng cho kết quả như nhau. Nếu đang xem mẩu báo và đóng túi, mở lại đúng trang/cấu trúc mà người chơi đã xem; không buộc lặp đọc lại SMS để đóng gate.

**Điểm khác biệt giữa “đã nhặt” và “đã hiểu”:** đưa E10a vào túi chỉ làm trạng thái sở hữu thay đổi. CH01-T02 yêu cầu đã **đọc nội dung tin báo hoãn** (E10a.SMSRead), không chỉ chạm prop. Ngược lại, sau khi đọc hợp lệ, không ép xem thêm tấm biển tháo dỡ mới cho cổng mở. Người chơi có thể đọc thư Lan trước, đọc mẩu báo sau, rồi tin Bảy cuối cùng; mọi trình tự hợp lệ.

**Gợi hướng và sai thao tác:** nếu thử mở cổng khi chưa có chìa, âm ổ khóa khô và Minh chú ý về bì thư gần xe; không bật cửa sổ hướng dẫn nhiều dòng. Nếu đã có chìa nhưng chưa đọc SMS, phản hồi từ chối phải nói điều còn thiếu là thông tin, không “đưa sai chìa”. Nếu cầm chìa khu C vào cổng phụ, trò chơi cho phép cất/đổi qua chìa đúng với phản hồi ngắn; không lấy món C khỏi túi hay gắn nhãn nó là đồ vô dụng. Nếu người chơi thả chìa tại anchor được phép, nó vẫn có thể lấy lại, không rơi dưới mặt đất qua collider.

**Bản lưu:** lần đọc E10a, nhận item con, sự kiện SMS và CH01-T02 phải có once ID riêng. Khởi tạo evidence E10a có thể có sẵn trong data; vì thế nhặt và mở phong bì **không thêm lần thứ hai vào state.evidence**. Save ngay sau khi nhặt nhưng trước khi đọc phải cho phép quay lại đúng chỗ, mở tiếp phần SMS, không auto mở cổng; save sau khi mở cổng phải giữ khóa và gate không bị đặt lại.

#### Nhịp kinh dị

Khi người chơi hạ phong bì, âm mưa trở lại hơi khác hướng. Nếu đã nhìn biển tháo dỡ, cánh biển tôn gõ một tiếng ngắn sau khi chốt cổng chuyển động; đó là âm kim loại có nguồn, không phải một jumpscare “ma” đầu chương. Giữ khoảng trống âm thanh để người chơi tiếp tục suy nghĩ về lời hẹn.

#### Thông tin và trạng thái sau beat

Minh biết bệnh viện đã đóng, có 13 người chết theo thông tin công khai, Bảy hẹn rồi hoãn và chính mình vẫn bước vào. E10a là evidence duy nhất của chương, sự kiện đọc tin nhắn đã được lưu; chìa cổng chỉ là item dùng cho tương tác, không sinh thêm evidence. Người chơi tới sân trong, CH01-T02 hoàn tất.

---

### B02 — NGƯỜI ĐỨNG SAU Ô KÍNH
**Khu vực:** Sân LOC01 → sảnh LOC02, R.  
**Thời lượng:** 1–1,5 phút.  
**Cảm xúc:** Tòa nhà bỏ hoang có vẻ vẫn đang chờ một người nào đó.

#### Trình tự người chơi

1. **Băng qua sân theo dấu sáng.** Ánh đèn phía quầy tiếp nhận vừa đủ tạo hướng đi; vẫn nhìn được mép nền, rãnh nước và cửa sảnh. Không cần waypoint nổi hoặc bắt người chơi bật đèn pin để được phép tiến.

2. **Nhìn thấy một người qua kính.** Ở góc nhìn từ sân, bóng dáng người đứng hoặc ngồi gần quầy hiện trong ô kính mờ (dread cue B02). Người chơi được di chuyển sang trái/phải, tiến lại gần, dừng và nhìn từ một góc khác. Không phát câu “đó là Lan”; phải để người chơi tự nghi ngờ.

3. **Thử xác minh.** Nếu player nhìn lại sau khi đổi góc, ánh đèn hắt qua kính và bóng người không còn khớp hoàn toàn với dự đoán. Có thể phát một tiếng bước chân lùi xa hoặc một chuyển sáng nhẹ. Nếu người chơi đi thẳng vào sảnh, sự kiện này được bỏ qua; nó không phải chìa khóa cổng hay cờ điều tra cần thiết.

4. **Bước vào quầy.** Không có khuôn mặt áp vào kính, không teleport người chơi và không chuyển qua cinematic. Kính ở R chỉ là tiền đề cho cuộc gặp thật sự khi Minh chuyển lớp.

#### Nhịp kinh dị

Một micro-dread phụ thuộc góc nhìn là đủ. Không chồng tia chớp mạnh, tiếng gõ kim loại B01 và jumpscare ở cửa sảnh trong thời gian quá sát nhau. Cảnh rùng mình đến từ điều phi lý rất nhỏ: **ánh sáng của một ca trực trong tòa nhà lẽ ra đã bỏ hoang**.

#### Dàn cảnh bổ sung tại sân và ô kính

**Bản đồ chú ý:** lối đi từ cổng tới sảnh đặt người chơi trên một đường có thể nhìn thấy quầy đèn ấm. Khoảng sân không nên bị xây thêm bẫy hành lang hay vật chắn khiến người chơi không thể tiếp cận vì không tìm được một công tắc. Vùng đèn có thể phản chiếu trên nền ướt, nhưng **không phải dấu chỉ đường tuyệt đối**; người chơi đã thấy cổng, xe và thông báo trong B01 nên vẫn nhận ra phương hướng khi quay lại B07.

**Những lần nhìn khác nhau:**

- **Đi thẳng, không ngoái:** bóng thoáng đủ một khoảnh khắc ở mép kính; lời/cue CH01-D02 có thể mở theo guard đã có. Không ném HUD “Bạn đã bỏ lỡ manh mối”.
- **Dừng tại góc rộng:** bóng người ở sâu sau kính, đèn tạo lớp phản xạ khiến không rõ đó là một người sống, một nhân viên bảo vệ hay ánh đèn bị méo.
- **Đi ngang kính sang trái/phải:** trục bóng so với cột/quầy đổi chậm hơn dự kiến. Điều này là một anomaly thị giác, không ghi thành Evidence.
- **Tiến sát và nhìn lại lần hai:** một cú thay đổi góc vai hoặc bóng ở ghế diễn ra đúng lúc player tự rời mắt khỏi một landmark. Khi nhìn trực diện lại, quầy có thể trống hoặc khó nhìn hơn qua phản xạ.
- **Bật/tắt đèn pin:** ánh sáng trên lớp kính thay đổi đúng quy tắc phản xạ; bóng trong sảnh không bị rọi sáng thành một khuôn mặt quái dị.

**Thời gian và tính chủ động:** sự kiện phụ chỉ có thể phát một lần và không được buộc chờ một timeout 60 giây. Sau mỗi sự kiện quan sát mạnh có nhịp yên đủ để chân người chơi, mưa và tiếng cửa lấn lên trở lại. Nếu player chạy qua, game giữ nhịp, không teleport bóng ra trước mặt cho bằng được.

**Tính liên tục với B03:** đây là hình ảnh trục quầy từ phía R. Sau khi giở sổ R→B ở AN01, Lan phải ở vị trí có thể liên hệ với bóng ấy; tuy vậy game không đóng dấu “đã gặp Lan” chỉ vì người chơi nhìn qua kính. LAN_TALK không bị kích bằng trục nhìn B02. Mọi nguồn ghi nhận siêu nhiên ở giai đoạn này chỉ là cảm giác về không gian, không là tài liệu dùng ký F101.

#### Thông tin và trạng thái sau beat

Người chơi hiểu rằng tại quầy có một điều cần kiểm tra, nhưng chưa biết đó là ai và chưa nhận thêm chứng cứ. Đích tiếp theo là cuốn sổ trực vật lý tại AN01.

---

### B03 — CA TRỰC Ở MỘT NGÀY KHÁC
**Khu vực:** Quầy LOC02 / AN01, R → B.  
**Thời lượng:** 3–4,5 phút, trong đó thoại điện ảnh khoảng 60–90 giây.  
**Cảm xúc:** Ấm áp trước, bất thường sau.  
**Dàn dựng:** Scripted Event → người chơi chủ động trò chuyện → Cinematic Dialogue Scene → trở lại FPS.

#### Pha 1 — Giở sổ và chuyển lớp

1. Người chơi bước tới quầy tiếp nhận R. Ánh đèn bàn hướng sự chú ý tới **cuốn sổ trực thật ở AN01**. Đây là một đối tượng của thế giới, **không phải giao diện ba lô**.
2. Minh tự giở sổ; CH01-T03 kích hoạt transaction R→B tại cùng vị trí an toàn. Âm mưa sân chuyển thành tiếng quạt, giấy, đèn và hoạt động của một ca trực. Trục cửa, quầy, các cột và hướng đi giữ nhất quán để người chơi nhận ra cùng bệnh viện; chỉ bề mặt, ánh sáng và hoạt động đổi.
3. Sau khi transition commit, người chơi vẫn nhìn tự do. Lan có mặt sau quầy, đang ghi chép.

#### Pha 2 — Lan nhận biết Minh trong gameplay

4. Khi Minh đi gần, **Lan dừng viết, ngẩng đầu và nhận ra anh**. Chuyển động nhỏ, tự nhiên: dừng bút nửa nhịp, ngước mắt, điều chỉnh tư thế. Lan có thể lên tiếng gọi anh theo thoại chính thức đã có; không đưa một jumpscare vào gương mặt.
5. Người chơi **chưa mất điều khiển**. Có thể nhìn chiếc ghế, cuốn sổ, lịch năm 2005 hoặc đồng hồ treo tường. Nếu quay đi hoặc lùi ra sân, Lan không đứng bất động chờ một nút: cô chỉnh tập giấy, tiếp tục công việc rồi chú ý lại khi Minh quay về. Câu chào nhận diện chỉ phát một lần.
6. Khi Minh tới mép quầy, prompt ngắn **“Nói chuyện với Lan”** xuất hiện. Người chơi chủ động nhấn E/K. Chỉ hành động này mới khởi động cinematic, không tự chạy sau 6 giây đứng im, không tự khóa camera vì nhân vật đi qua vùng trigger.

#### Pha 3 — Cinematic Dialogue Scene

7. Camera blend liền mạch từ FPS sang góc medium tại quầy, không màn đen hoặc tải scene mới. Cảnh giữ mạch nhìn của người chơi: Lan ở sau quầy, Minh ở phía đối diện; không sử dụng over-the-shoulder cho Minh nếu rig cơ thể và góc quay không đáp ứng.
8. Diễn xuất mở bằng cảm giác **hai người từng thân quen**: khoảng lặng khi nhìn nhau, chuyển động tay Lan đặt bút xuống, phản ứng không trọn vẹn của Minh. Trong lời thoại chính thức CH01-D03/D04/D05/D07, Lan nói về ca trực **17/11/2005** như một điều đang diễn ra. Minh, ở năm 2012, nhận ra sự sai lệch.
9. Đằng sau Lan có lịch và chiếc đồng hồ lớp B dừng **03:17**. Camera có thể để các vật này xuất hiện như thành phần bố cục, nhưng không zoom để kết luận thay người chơi; **không tự đánh dấu OBS_CLOCK_0317 hoặc hoàn thành EX01** chỉ nhờ cinematic. Mốc 03:17 không được diễn giải thành giờ tử vong.
10. Lan hướng mắt và tay về **BOARD** trên quầy, giao Minh việc kiểm tra phòng 101. Cảnh không tự “nhặt item” qua cutscene, không mở khóa các manh mối người chơi chưa xem và không phát lời thoại bị khóa của các chương sau.
11. Camera quay lại FPS ở quầy. Phải có Skip, phụ đề, bản tóm tắt phần lời thoại đã được phép biết và phục hồi input/camera sau skip hoặc load. Không phát jumpscare ngay lúc trả quyền điều khiển.

#### Pha 4 — Nhận trách nhiệm và tự khám phá

12. Người chơi tự **nhấc BOARD** khỏi quầy, kiểm tra giấy trong 3D và cất vào ngăn giấy của ba lô. Chỉ lúc này **CH01-T05** commit và việc vào Phòng 101 mới được mở. **BOARD tại quầy là đồ bàn giao nhiệm vụ; F101 là phiếu được kiểm tra, đánh dấu và ký ở Phòng 101**, không phải một món đã ký được Lan trao tại đây.
13. Khám phá tùy chọn tại quầy: kéo **DRAWER-DESK** xem OBS_RECEPTION_DRAWER, soi lịch, quay lên đồng hồ 03:17 trong B để kiểm bằng chính camera của mình. Người chơi có thể dùng đèn/đổi góc và ghi nhớ nguồn; không cần phải hoàn thành tất cả để đi Phòng 101.

#### Nhịp kinh dị và giá trị cảm xúc

Lan **không** làm mặt quỷ, biến mất trong tiếng chói, hay bị hệ thống gọi là một hồn ma. Điều đáng sợ là sự bình thường quá thuyết phục của cô. Có thể thử một phản xạ mặt kính chậm hoặc bị lóa trong nhánh quan sát phụ, nhưng không đưa bằng chứng chắc chắn “Lan không có bóng” vào canon.

Hình ảnh Lan chăm chú ghi sổ, hơi ấm đèn bàn và tiếng bút trong cảnh này phải được giữ nhất quán: chúng là chất liệu khiến **quầy trống B06** có tác động cảm xúc.

#### Thông tin và trạng thái sau beat

Minh đã trải nghiệm ca trực B năm 2005, gặp Lan và nhận BOARD. Q về Lan và thời gian được mở, chưa giải đáp. CH01-T03/T04 và T05 đúng thứ tự, AP02 lưu hợp lệ, các observation phụ chỉ ghi khi người chơi chủ động kiểm.

---

### B04 — PHÒNG CÓ VỊ TRÍ GIƯỜNG THỨ TƯ
**Khu vực:** LOC02 ↔ STAFF ↔ HALL ↔ LOC03, B.  
**Thời lượng:** 6–8 phút tuyến chính; điều tra sâu thêm tùy người chơi.  
**Cảm xúc:** Từ nghi ngờ một con số tới cảm giác có điều từng xảy ra ngay tại nơi mình đứng.

#### Đường đến Phòng 101

1. Sau khi cầm BOARD, người chơi có thể đi thẳng qua HALL hoặc vòng qua hốc hồ sơ điều dưỡng STAFF. Hai đường hội tụ ở Phòng 101; không có nhiệm vụ mở tủ STAFF mới được phép vào phòng. Lối tới Khu C chưa mở theo beat này.
2. Trên đường qua HALL, tiếng xe đẩy xuất hiện ở đoạn xa rồi **khựng lại** (DREAD_B04_TROLLEY_STOP). Không cho xe lao vào camera hoặc chắn lối. Nếu Minh quay tìm, hành lang vắng nhưng sự chú ý của anh giờ hướng về những **dấu bánh xe** có thể kiểm.
3. Tại cửa Phòng 101, F101 đang ghi con số **0**. Người chơi có thể đọc ngay mặt trước, nghiêng tờ phiếu để nhìn ô số và đặt lại trên **bảng kẹp AN02**. Không bắt lập luận ngay; lúc này con số chỉ là một tuyên bố hành chính.

#### Vào phòng và nhìn thấy điều giấy không nói

4. Người chơi **hé DOOR-101** bằng E hoặc giữ-kéo rồi bước qua ngưỡng; CH01-T06 và checkpoint AP04 được ghi. Cửa mở vừa đủ để thấy rèm và quạt, không tự phơi hết hốc giường 4.
5. Minh đi giữa những vị trí giường hiện hữu, tự so khoảng cách đầu giường, mặt tường, chỗ đặt đồ và dấu mòn trên sàn. Phần không gian sau rèm có hình học cho thấy **một vị trí từng được sử dụng**, nhưng chưa chứng minh ai ở đó.
6. Người chơi kéo **CURTAIN-101**. Rèm có sức cản nhẹ và âm vải/kim loại trên thanh ray; một lần nhấn E vẫn mở đạt mức có thể nhìn đầy đủ. Đừng biến mức “60% kéo rèm” thành thử thách kỹ năng làm người chơi mắc kẹt.
7. Sau rèm là **OBS101A**: vệt bánh quanh chỗ kê giường, chăn gấp vội, dép nhựa nhỏ. Người chơi tự nhìn từ nhiều góc, có thể cúi xuống hoặc dùng ánh đèn để thấy chỗ mòn. Khi quan sát tại chỗ, ghi observation có provenance lớp B/LOC03; **không cất dấu sàn hay đôi dép thành evidence xuất** trong Chương 1.
8. Người chơi có thể quay nhìn ba giường còn lại rồi trở lại hốc 4. Đây là **Spatial Deduction**: tự so tương quan không gian, không kéo một biểu tượng “dấu bánh giường” sang biểu tượng “giường 4” trong menu.

#### Thẻ AS-05B — sự bất thường có chữ ký

9. Trên giá kẹp tại vị trí thứ tư, người chơi nhấc **AS-05B (OBS101B)**. PhysicalInspector mở trên chính chiếc thẻ được cầm: mặt trước, mặt sau, ô bàn giao và chữ ký nhận còn thiếu.
10. Người chơi lật thẻ, nghiêng đón sáng hoặc dùng lệnh trợ năng tương đương để khám phá vết hằn **“03g17 · CHƯA CHỐT SỔ”** ở mặt sau. Chi tiết này là câu hỏi về mốc giao ca, **không cho phép kết luận một người chết lúc 03:17**.
11. Người chơi có thể **cất tạm AS-05B vào ngăn giấy thuộc B**, lấy ra kiểm lại khi cần hoặc đặt nó ở một điểm so tài liệu hợp lệ. Hệ thống vẫn giữ nguồn và trạng thái đã Examined. Cầm thẻ rồi quên kiểm ô chữ ký sẽ không tự cho phép ký Refuted.

#### Điều tra phụ có ý nghĩa

12. **STAFF:** mở DRAWER-NURSE để đọc **OBS_SHIFT_LOG / quy chế AS-05**; xem **OBS_CORRIDOR_NOTICE**. Tác dụng là hiểu một chuyển giao hợp lệ cần có dấu nhận và cách kê giường tạm được quản lý. Đây là nền nghiệp vụ, không phải chìa khóa UI bắt buộc.
13. **Giường 2:** đọc **OBS101C**, phiếu chuyển đã hoàn tất có đầy đủ dấu bàn giao. Người chơi nhận ra **một hồ sơ đúng quy trình trông như thế nào**, từ đó biết tại sao thẻ giường 4 gây nghi vấn. Không cần đọc C để được ký Refuted nếu đã đủ hai nguồn chính.
14. **Giường 3:** mở DRAWER-BED3 xem **OBS101D**, phiếu thuốc/việc hoàn tất của giường 3. Nó cho thấy giấy tờ có thể khớp đúng một vị trí mà **không** chứng minh điều gì về vị trí 4. Nếu đem phiếu này làm căn cứ bác bỏ F101, Minh phản hồi nguồn đó nói về giường khác.
15. **Mốc thời gian tùy chọn EX01:** nếu đã tự xem đồng hồ B ở quầy, soi vết hằn mặt sau AS-05B và đọc một nguồn ca trực tại quầy hoặc STAFF, người chơi có thể tự ghi mối liên hệ **03:17** trong Pocket Note mở rộng. Trình tự nguồn có thể bất kỳ. EX01 chỉ mở khả năng nghe CH01-D19 sau khi ký, không thay kết luận F101.

#### Nhịp kinh dị

Khi Minh kiểm dấu bánh hoặc hạ thẻ sau khi đọc ô bàn giao, tiếng xe kim loại/tấm rèm có thể vang **đúng từ nơi thiếu dấu xác nhận**. Một chi tiết rèm khép thêm hay quạt chậm lại được phép xuất hiện sau khi camera về 3D, không trong inspector. Không dùng bóng người làm “phần thưởng chắc chắn” cho người chơi đọc tài liệu; sự yên ắng sau vật chứng có thể đáng sợ hơn.

#### Điều người chơi thực sự biết

Có một vị trí sử dụng khác với cách phiếu mô tả; thẻ bàn giao liên quan chưa có xác nhận hoàn tất. **Chưa chứng minh** có bệnh nhân vẫn còn trong phòng, có ai cố tình làm giả giấy, ai nằm tại đó hoặc vì sao 03:17 xuất hiện. Hai quan sát chính cho phép người chơi chuẩn bị một lập luận giới hạn ở B05.

---

### B05 — CHỮ KÝ TRÊN CON SỐ 0
**Khu vực:** Phòng 101, B, bảng kẹp F101 tại AN02.  
**Thời lượng:** 5–7 phút nếu tự kiểm và lựa chọn; tuyến A/B có thể ngắn hơn.  
**Cảm xúc:** Người chơi hiểu rằng đặt bút cũng là một hành động gây hậu quả.

#### Trở lại bảng kẹp

1. Người chơi đi tới F101 tại **AN02** và nhìn trực tiếp các trường trên tờ phiếu. Giấy hiện hữu ở bảng kẹp; không lấy F101 gốc từ BOARD ở quầy hay đem gốc F101 đi khỏi Phòng 101.
2. Nếu muốn đối chiếu, mở ba lô, **lấy AS-05B ra**, đặt vào **kẹp so tài liệu cạnh F101**, tự lật/soi hai vùng in. Đây là vị trí vật lý để người chơi không phải ghép hai biểu tượng trong một màn hình suy luận giả lập.
3. Người chơi có thể tháo thẻ, đi nhìn lại OBS101A trên sàn rồi quay về. Không reset tiến độ quan sát đã commit vì thẻ đang trong túi hay được đặt ở bàn.

#### Lập luận bằng cử chỉ, không bằng dây nối UI

4. Trên F101, người chơi có thể **đánh dấu con số 0**. Trên thẻ AS-05B, chỉ vào **ô người nhận chưa ký**. Khi đã thực sự Examined OBS101A trong phòng, có thể quay/điểm nhìn về dấu bánh/nhóm dấu tại hốc 4 rồi chọn hành động ngắn **“Liên hệ với dấu hiện trường”**; hỗ trợ bàn phím/chữ lớn làm được cùng thao tác.
5. Sau đó Minh chọn giới hạn của phát biểu: **“Chưa đủ căn cứ xác nhận số 0.”** Không đưa lựa chọn “có bệnh nhân còn sống trong phòng” hoặc “ai đó cố tình làm giả” thành đáp án đúng.
6. Các thao tác vật lý trên chỉ tạo đầu vào cho hệ đánh giá: **OBS101A và OBS101B đã Examined ở B, đúng mark F101_ZERO và OBS101B_SIGN, link tới OBS101A_TRACES, cùng claim giới hạn**. Hệ thống mới được phép xác nhận nhánh Refuted. Kẹp đúng thẻ lên bảng mà chưa xem dấu sàn **không** đủ. Nếu thiếu bước, Minh giải thích điều chưa biết, không hiện tiếng chuông “SAI”.

#### Ba chữ ký đều hợp lệ

**A — Affirmed / Xác nhận 0.** Người chơi có thể ký ngay cả khi chưa khám phá giường 4. Chữ ký được commit, giấy hiển thị bản signed A, **D(LOC03)=1** trong lớp B. Vách che **deniedBay** đóng kín khu vực hốc 4; quạt đều và lạnh, sau vách có hai tiếng gõ nén. Cảnh không khẳng định có người đang bị nhốt; điều đáng sợ là **một tờ giấy có thể khiến vị trí từng nhìn thấy bị phủ kín**.

**B — Unverified / Để ngỏ.** Người chơi có thể ký ngay không cần tài liệu. Giấy giữ trạng thái chưa xác minh, **D(LOC03)=0**. Rèm và khoảng giường lửng lơ; tiếng quạt chậm và một khoảng im kéo dài. Game không trừng phạt Minh vì thừa nhận chưa đủ thông tin.

**C — Refuted / Ghi nhận bất nhất.** Chỉ ký được sau khi lập luận hợp lệ bằng OBS101A/B và marks/link/claim đúng. **D(LOC03)=0**. Rèm tách, hốc giường lộ rõ hơn; mảng tường ẩm, dấu bánh đậm và góc CH01_PR01_Corner được nhìn thấy theo hiện trạng đã định. Không có lời thú nhận, tên bệnh nhân hay đáp án thủ phạm.

7. **Chữ ký gây hậu quả trước mặt người chơi.** Bút/chốt giấy, một khoảng dừng, rồi sự thay đổi của căn phòng. Không che toàn bộ chuyển cảnh bằng một flash khiến player không kịp thấy kết quả do mình gây ra.
8. **G01 mở ở cả A/B/C** ngay sau commit bền vững CH01-T07. Người chơi có thể đứng lại quan sát và sửa quyết định trước khi chuyển B→R. Lần ký đầu lưu firstDecision; mỗi sửa đổi hợp lệ cập nhật current/revision/history; bấm lặp cùng lựa chọn không nhân số lần.
9. Nếu EX01 đã hoàn tất, khi rời Phòng 101 người chơi có thể nghe CH01-D19 đúng điều kiện; không tự kích cho người bỏ qua nhánh phụ.

#### Đỉnh kinh dị chính — “ở phía sau rèm”

Khi người chơi **đã kiểm tra AS-05B, hạ tài liệu và đang tự do nhìn lại phòng**, tiếng quạt/hum đèn hạ nhẹ trong khoảng ngắn; một tiếng móc rèm hoặc khung giường phát đúng hướng hốc 4. Nếu player tự quay nhìn, có thể thoáng thấy một hình người thấp hoặc bóng tựa đầu giường trong chưa đầy hai giây, rồi mất khi ánh sáng ổn định. Không lộ mặt, không lao vào camera, không định danh. Nếu không quay, chỉ nghe âm và vẫn được kiểm tra hiện trường; **không ép camera, không tặng manh mối bắt buộc qua jumpscare**.

Scare này chỉ xuất hiện **một lần** ở thời điểm phù hợp giữa lần kiểm thẻ và quyết định/khoảng hậu ký; không chồng blackout hoặc hiệu ứng phản hồi F101. Nếu người chơi ký rất nhanh, không kéo họ trở lại xem scare. Chế độ giảm jumpscare sử dụng silhouette tĩnh hoặc âm có caption, không nháy sáng mạnh.

#### Điều còn lại sau khi ký

Người chơi vừa chọn tin, để ngỏ hoặc phản đối một con số. Không có lựa chọn nào tự giải câu hỏi người ở giường 4 là ai. Kết quả vật lý ở B khiến Minh muốn đối chiếu thêm với một hiện trường không phụ thuộc tờ phiếu — chính là động lực của Chương 2.

---

### B06 — NGƯỜI GIAO VIỆC KHÔNG CÒN Ở QUẦY
**Khu vực:** LOC03 → HALL → LOC02 / AN01, B → R.  
**Thời lượng:** 2–3 phút.  
**Cảm xúc:** Sự mất mát đáng sợ hơn một gương mặt quỷ.

#### Trình tự người chơi

1. Minh rời Phòng 101, đi lại qua hành lang. Không auto-teleport về quầy. Âm nền ở B đổi nhẹ theo chữ ký; không thêm một cuộc rượt đuổi vì CH01 không có Hunter.
2. Người chơi có thể mở ba lô để xem bản nhớ của AS-05B, BOARD và ghi chú, hoặc quay lại 101 để sửa F101. Đây là một cơ hội **tự kiểm điều mình vừa làm**, không là bước bắt buộc.
3. Minh bước tới quầy từng gặp Lan. **Đúng vị trí chiếc ghế, cây bút và cuốn sổ vẫn ở đó, nhưng Lan không còn**. Không chạy cinematic mới, không cho Lan biến mất trước camera, không tự chiếu cảnh hồi tưởng; để người chơi có thời gian tự nhìn từng góc quầy.
4. Nếu tương tác với chỗ làm việc của Lan, chỉ nhận chi tiết xác nhận cô không còn ở đó, không có lời giải thích siêu nhiên. Tiếng bút từng nghe ở B03 đã biến mất. Sau khoảng yên ngắn, mới được có âm đồng hồ hoặc nhiễu loa nếu cue hợp trạng thái.
5. Người chơi tới **sổ trực vật lý tại AN01**, tự giở trang để bàn giao. Hành động này kích **CH01-T08** và transaction B→R ngay vị trí an toàn; không bắt đem F101 gốc từ phòng 101 về đặt lên sổ. Dữ liệu chữ ký đã tồn tại trong history và giấy signed nằm tại lớp B.
6. Khi về R, đèn/quạt của ca 2005 nhường cho mưa và sự xuống cấp. **Các tờ giấy vật lý chỉ thuộc B**, dù đang nằm trên bàn, cầm trên tay hay cất trong ba lô, phải được trả về nơi B có thể phục hồi; ba lô giữ **ghi nhớ nguồn đã kiểm**, không cho Minh rút bản gốc giấy B ra sử dụng trong R.

#### Đạo diễn âm thanh

Sự thay đổi nằm ở ba chất âm: **R 2012** là mưa và tòa nhà bỏ hoang; **B 2005** có quạt, giấy, bút và con người; **R sau bàn giao** trở lại tiếng sân nhưng thiếu hoàn toàn một giọng nói đã trở nên quen thuộc. Sự im lặng của chiếc ghế là payoff của cuộc gặp B03.

#### Thông tin và trạng thái sau beat

Minh chưa giải thích được vì sao Lan ở B nhưng không còn ở quầy. CH01-T08 hoàn tất, R được phục hồi mà không nhận thay đổi hình học do F101 trong B. Ghi chú và lịch sử quyết định còn nguyên, không có giấy B vật lý lọt sang R.

---

### B07 — CÁNH CỔNG KHÔNG CHO RỜI ĐI
**Khu vực:** LOC02 → LOC01 → LOC02, R.  
**Thời lượng:** 1–1,5 phút trên tuyến trực tiếp.  
**Cảm xúc:** Phản xạ tự nhiên đầu tiên sau trải nghiệm phi lý là bỏ đi.

#### Trình tự người chơi

1. Sau khi về R, mục tiêu ngắn trong Pocket Note hoặc qua tự thoại là **thử rời bệnh viện**. Người chơi có thể chạy về sân theo lối đã tới. Xe máy, rãnh nước, biển tháo dỡ và cổng phụ vẫn ở vị trí quen thuộc; không cần đặt một mê cung mới.
2. Tới cổng, Minh tự **nắm và kéo cánh sắt**. Không cần lục túi tìm chìa khu C hoặc món đồ thần bí; đây không phải puzzle chìa khóa. Khi kéo đủ mức, transaction **CH01-T09** chuyển qua vòng lặp không gian một lần, đặt player ở safe pad hợp lệ để nhận ra mình lại đang ở trong khuôn viên.
3. **Scare chủ đạo thứ hai:** một khoảng hụt âm mưa ngắn, cú đập kim loại bên ngoài cổng và bóng phản chiếu lướt ngang khung. Không đưa khuôn mặt, không cho nhân vật tấn công và không biến cổng thành boss. Âm/camera có phiên bản giảm jumpscare. Nếu người chơi đã gặp scare mạnh ở B05 quá gần, hạ cường độ âm hoặc để khoảng hồi nhịp phù hợp.
4. Khi Minh nhìn lại, cổng không cung cấp lối thoát. Lời thoại CH01-D14 chỉ phát đúng điều kiện. Lần thử thứ hai không tạo thêm vòng loop; thay bằng phản hồi ngắn cho thấy hiện tại đường ra không mở được.

#### Ý nghĩa

Ở B01, Minh **chọn bước vào**; ở B07, Minh **không thể rời đi bằng cách phủi bỏ điều vừa biết**. Đây là đối xứng của hai thao tác tay, không phải một phép thử khóa lần nữa.

#### Thông tin và trạng thái sau beat

CH01-T09 được lưu once; đường từ cổng về AN01 luôn mở. Ba lô và vật chứng R không mất hoặc nhân đôi qua seam.

---

### B08 — ĐIỀU CÒN LẠI SAU KHI TỜ GIẤY BIẾN MẤT
**Khu vực:** LOC01 → LOC02 / AN01, R.  
**Thời lượng:** 1–2 phút.  
**Cảm xúc:** Không thể hiểu tất cả, nhưng không thể ngừng kiểm chứng.

#### Đường đi trở lại

1. Người chơi tự đi từ cổng về sảnh trên tuyến đã quen. Trong quãng ngắn, tiếng mưa và âm đường xa bị hạ nhẹ theo khoảng cách, để lại tiếng bước chân và một khoảng bất an có chủ đích. **Không tắt sạch mọi đèn sân**: mép bậc, lối đi và quầy AN01 vẫn được nhận diện. Không kéo dài đi bộ bắt buộc đúng 30 giây nếu người chơi chạy nhanh.
2. Khi tới sảnh R, người chơi nhìn thấy cuốn sổ và lối dẫn về Phòng 101. Cùng một tòa nhà đã mở ra lớp B và trả lại cảnh R, nhưng câu hỏi về hốc giường 4 vẫn chưa có câu trả lời.

#### Ba lô như một phần ký ức

3. Nếu player từng **thực sự nhấc/soi hoặc cất AS-05B ở B**, khi mở ngăn giấy trong R có thể xuất hiện một **hình bóng thoáng qua của mép thẻ hoặc dấu ô bàn giao trên lớp trình bày của túi**. Khi đưa tay tới, đó không phải vật thật có thể equip: nó chỉ dẫn tới **ghi nhớ đã đọc tại LOC03/B**. Sự kiện thị giác này là **P-BAG08_ECHO**, một hiệu ứng biểu đạt ký ức, không phải item mới, không là chứng cứ vật lý 2012 và không xuất hiện nếu người chơi chưa hề thấy thẻ.
4. Nếu người chơi không mở ba lô, câu chuyện vẫn hoàn chỉnh. Pocket Note có thể giữ một câu hỏi ngắn về thứ đã thấy trong Phòng 101, dựa trên chính những nguồn player đã thực sự kiểm. Không được tự tóm tắt rằng Minh đã gặp bệnh nhân, phát hiện thủ phạm hoặc biết giờ tử vong.
5. Minh có thể lấy **E10a thực ở R** ra đọc lại: một lời hẹn bị hoãn, một mẩu tin cũ và lý do anh đã tự bước vào. Đối chiếu cảm giác với trải nghiệm ở B là suy nghĩ của người chơi, không phải thông báo “đã giải xong vụ án”.

#### Kết chương

6. Người chơi đến **sổ trực vật lý tại AN01**, giở trang để xác nhận tiếp tục kiểm hiện trường. **CH01-T10 / CP04** được commit và thẻ **CHƯƠNG 02 — PHÒNG BỆNH TRỐNG** xuất hiện. Không cần mở menu suy luận nhiều tab hoặc chọn một câu kết đạo đức.
7. Camera và trạng thái chuyển tiếp để người chơi bước vào **Phòng 101 của hiện thực R (2012)**. Chương 2 còn các dấu **LIST + SOCKETS + WHEELS → PZ01/E12 → E11 → G02**, được điều tra độc lập; không tự cấp manh mối hoặc đồ vật CH02 ở đoạn kết CH01.

#### Hình ảnh kết

Một tiếng giấy lật khô tại quầy trống. Không có Lan, không có jumpscare cuối chương. Cánh cửa Phòng 101 ở hướng đi tiếp; người chơi **muốn tự quay lại nhìn hiện trường thật** vì những gì đã tận mắt thấy ở B vẫn không thể khớp với một con số trên giấy.

---

## III. NHỊP KINH DỊ VÀ CHỖ DỪNG CHO NGƯỜI CHƠI

Kinh dị trong chương phải có **nguồn**, **thời điểm**, **hậu quả**. Ưu tiên tạo cảm giác bị nhìn, những âm thanh có chỗ phát cụ thể, và các thay đổi diễn ra sau hành động của Minh. Không nhầm số lượng jumpscare với độ sâu kinh dị.

| Nhịp | Cảnh | Vai trò | Điều kiện bảo vệ |
| --- | --- | --- | --- |
| B01 | Biển gió gõ sau khi tự mở cổng | Cảm giác bệnh viện phản ứng với quyết định bước vào | Cue nhẹ, có nguồn; không chồng vào đọc thư |
| B02 | Bóng sau kính thay đổi theo góc nhìn | Nghi ngờ không gian | Có thể bỏ lỡ; không khóa tiến trình |
| B03 | Lan tự nhận ra Minh và đối thoại bình thường | Căng thẳng tâm lý qua sự quen thuộc sai thời gian | Không jumpscare, không auto thoại cinematic |
| B04 | Xe đẩy dừng, rèm cọ thanh ray | Buộc chú ý dấu sử dụng vật lý | Không chặn cửa, không che quan sát |
| B05 | Tiếng từ hốc 4, silhouette thoáng rồi F101 biến dạng | Đỉnh kinh dị thứ nhất gắn với điều tra và ký | Một lần; ngoài inspector/menu; không ép camera |
| B06 | Quầy trống và tiếng bút biến mất | Hụt hẫng, mất một điểm tựa | Cho khoảng lặng, không phát cú giật ngay |
| B07 | Cổng bị đập, seam một lần | Đỉnh kinh dị thứ hai, không còn lối trốn | Giảm cường độ khi scare gần nhau; không lặp loop |
| B08 | Ngăn giấy B chỉ còn ký ức; bước chân về quầy | Dư âm nhận thức thay vì cú hù cuối | Không auto mở ba lô; không trao vật chứng B vào R |

Không phát một scare lớn khi player đang mở ba lô, soi 3D, nghe thoại, ở cinematic, lúc save/load, lúc chuyển lớp hoặc khi đang thao tác bắt buộc có thể khiến mất đồ. Cue giảm âm phải ở mức an toàn, có phụ đề định hướng và tùy chọn giảm nháy/giật. Khi cue phụ bị bỏ lỡ, **mọi nội dung cần để hiểu F101 vẫn truy cập được bằng quan sát bình thường**.

---

## IV. ĐỒ VẬT, THÔNG TIN VÀ QUYẾT ĐỊNH

| Đối tượng | Vị trí / thời điểm | Player tác động bằng gì | Dữ liệu và giới hạn |
| --- | --- | --- | --- |
| **E10a và hai chìa** | B01, LOC01/R | Cầm/mở 3D, cất, rút chìa phụ, dùng mở cổng | E10a là evidence CH01 duy nhất; hai chìa là item; không cho dùng chìa C ở khu cấm trong CH01 |
| **Cuốn sổ trực AN01** | B03, B06, B08 | Tự giở trang để chuyển tiến trình/lớp | Là prop vật lý của quầy, **không phải UI ba lô** |
| **BOARD** | B03, quầy B | Tự nhấc/cất/xem nội dung nhiệm vụ | T05; không đồng nhất với F101 |
| **F101** | Phòng 101/B, bảng AN02 | Soi, đánh dấu, kẹp tài liệu so cạnh, ký/sửa | Anchored; A/B/C có hậu quả và history đúng luật |
| **OBS101A** | B04, dấu hốc 4/B | Tự nhìn, đổi góc, soi sàn/rèm | Observation tại chỗ; không cất vào ba lô |
| **AS-05B / OBS101B** | B04, giá kẹp B | Cầm/lật/soi/chọn điểm chữ ký/cất tạm | LayerBound B; sang R chỉ còn memory reference |
| **OBS101C/D** | B04, giường 2/3 B | Xem/cất tạm/so đối chiếu | Nguồn phụ, không thay được OBS101A/B |
| **OBS_SHIFT_LOG, OBS_CORRIDOR_NOTICE** | STAFF/HALL B | Mở ngăn, đọc, soi | Giải thích quy trình, không bắt buộc mở G01 |
| **OBS_CLOCK_0317** | B03/B B | Tự kiểm đồng hồ sau cinematic | Foreshadowing; không là giờ chết |
| **Ba lô và Pocket Note** | Xuyên chương | Cất/lấy/soi, nhớ nguồn, kiểm câu hỏi | Không tự sinh evidence; không chấm đạo đức |

### Quy tắc quyết định

- **Affirmed** và **Unverified** không cần đủ OBS101A/B, không chặn G01.
- **Refuted** yêu cầu **quan sát cặp OBS101A/B trong B**, đúng vùng đánh dấu và liên kết, phát biểu “Số 0 chưa đủ đáng tin”; không được tự suy ra có bệnh nhân đang còn ở phòng hoặc người cố ý che giấu.
- Cả ba lựa chọn mở G01. Chữ ký đầu tiên bất biến trong **firstDecision**; chữ ký hiện tại và lịch sử sửa được lưu riêng.
- Biến dạng của F101 chỉ tác động lên **lớp B**; phòng 101 **R của Chương 2 vẫn độc lập**. Tại CH01, tuyệt đối không tiết lộ danh tính C01/Bé An, E11/E12, nguồn hồi tưởng CH07 hoặc điều kiện thoại CH04 chưa tới.
- Việc giữ một tờ giấy trong ba lô không tương đương **đã hiểu nội dung nó**. Việc đã Examined một nguồn không đồng nghĩa đã rút ra kết luận đúng. Lỗi save/swap phải rollback để không nuốt item và không đảo evidence.

---

## V. ĐƯỜNG ĐI HỢP LỆ VÀ KHẢ NĂNG PHỤC HỒI

**Tuyến thẳng:** B01 đọc E10a → mở cổng → B02 tới quầy → B03 chuyển B, nói chuyện Lan, lấy BOARD → B04 vào 101 → B05 ký A hoặc B → B06 về AN01/R → B07 thử cổng → B08 về AN01/CH02. Tuyến này không cần xem STAFF, OBS101C/D, EX01 hoặc lấy AS-05B.

**Tuyến điều tra sâu:** B03 xem đồng hồ B/quầy → B04 STAFF, OBS101C/D, OBS101A/B, vết hằn → B05 lập luận Refuted và ký → B06 có thể kiểm lại notes, rồi chuyển R → B08 xem echo ký ức nếu đúng điều kiện. Tuyến này mở nhiều câu hỏi hơn nhưng không cho đáp án danh tính.

**Tuyến quay lại sửa:** B05 sau khi ký A/B/C, quay kiểm hiện trường và sửa F101 trước T08, rồi mới bàn giao. Lần sửa không nhân thêm chướng ngại, không thay đổi vật lý R và không đánh dấu lựa chọn trước là thất bại.

**Các tình huống dự phòng:** người chơi bỏ qua bóng kính; không trò chuyện ngay với Lan; skip cinematic; bỏ tài liệu B trên bàn; xem thẻ nhưng không hiểu; không đủ nguồn để chọn C; giảm jumpscare; dùng chế độ chữ lớn; load ngay trước/sau swap; quay lại cổng lần hai. Mỗi tình huống đều phải có tiến trình hợp lệ và phương thức tiếp cận tài liệu/đường thoát phù hợp, không cần teleport cheat trong gameplay.

---

## VI. ĐIỀU KIỆN HOÀN THÀNH CHO BẢN GAMEPLAY

Bản walkthrough chỉ được coi là vận hành đúng khi các đường chơi và trạng thái sau được kiểm trong Unity:

1. **Vật phẩm 3D:** lấy E10a và chìa không nhân bản; cầm/soi/lật/cất/lấy ra/đặt AS-05B không làm mất nguồn, với cả chuột và phím thay thế.
2. **Lan:** Scripted Event nhận biết diễn ra sau CH01-T03, cinematic chỉ bắt đầu khi Minh chủ động trò chuyện; skip/save/load không nhân thoại, không tự nhặt BOARD hoặc auto-credit quan sát đồng hồ.
3. **F101:** A và B ký được từ tuyến thẳng; C thiếu OBS101A/B, mark/link/claim sai thì bị từ chối; C đầy đủ thì thành công; mọi nhánh mở G01 và cho sửa trước T08.
4. **R/B và ba lô:** giấy B đang cầm, cất hoặc đặt trên bàn đều không biến thành giấy vật lý R sau T08; ghi chú nguồn vẫn còn; echo ở B08 chỉ là hiển thị có điều kiện, không thể dùng/export.
5. **Cốt truyện và dữ liệu:** CH01 evidence vẫn chỉ có E10a; không làm lộ C01, E11/E12 hoặc thoại/hồi tưởng bị khóa; CH02-R giữ LIST/SOCKETS/WHEELS và các gate độc lập.
6. **Điều hướng, trợ năng và sợ:** vòng lặp cổng chỉ một lần, không kẹt collider/không mất đường về AN01; mute, caption, mono, large print, reduced motion, giảm jumpscare đều dùng được; các scare không cướp quyền kiểm tra vật chứng.
7. **Playtest:** đo hành động người chơi tự thực hiện, hiểu biết đúng về F101, việc dùng đồ có mục đích, cảm giác sau khi Lan biến mất và động lực bước vào Phòng 101 ở hiện thực R. Số phút 20,5–30 chỉ là mục tiêu thiết kế; không coi là kết quả người chơi thật khi chưa đo.

**Kết thúc Chương 1:** Minh không biết đầy đủ sự thật, không có bằng chứng kết tội bất kỳ ai và chưa hiểu bản chất sự hiện diện của Lan. Nhưng anh đã tự tìm ra một điểm mà giấy tờ và hiện trường không khớp nhau, đã đặt chữ ký của mình vào cuộc, và giờ muốn tự kiểm tra nơi đó trong hiện thực năm 2012.