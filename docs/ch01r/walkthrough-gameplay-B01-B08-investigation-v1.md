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

### 4. Mạch câu hỏi mà người chơi tự mang theo

Để người chơi không thấy mình đang đi theo một bảng checklist, mỗi câu hỏi phải bắt đầu từ một sự kiện đã được tự chứng kiến. Các câu hỏi này có thể ở **Pocket Note** dưới dạng câu ngắn, còn bảng chi tiết nguồn chỉ xem khi mở phần ghi chép sâu; tuyệt đối không hiện hộ kết luận.

| Mốc nhận thức | Điều Minh được chứng kiến | Điều Minh *có thể* nghi | Điều còn thiếu, dẫn tới hành động sau |
| --- | --- | --- | --- |
| Sau B01 | Bảy hoãn hẹn, thư Lan cũ, bệnh viện sắp tháo dỡ | Có lý do khiến Bảy vẫn để lại tài liệu | Muốn kiểm nơi chính Minh vẫn quyết định vào |
| Sau B02 | Bóng người trong quầy R bỏ hoang | Có người đang ở đây hoặc hình ảnh không đúng | Tới tận quầy và tự kiểm |
| Sau B03 | Lan nói chuyện trong ca 2005, BOARD giao nhiệm vụ | Mình đang nhìn thấy một thời điểm khác | Tới 101 xem con số trên giấy |
| Giữa B04 | Dấu sử dụng ở một vị trí giường thứ tư | Số liệu ghi “0” có thể không phản ánh thực trạng đã xảy ra | Kiểm thẻ bàn giao và những trường nào còn trống |
| Sau B04, có đủ nguồn | Thẻ AS-05B thiếu xác nhận khớp với dấu vật lý đã nhìn | Không đủ cơ sở để chốt số 0 | Ký một mức độ chắc chắn phù hợp, không cáo buộc thêm |
| Sau B05 | Chữ ký đổi hình dạng lớp B | Tờ phiếu đang ảnh hưởng cách nơi này “hiện ra” | Kiểm lại một nơi không lệ thuộc bản giấy |
| Sau B06 | Lan không còn, lớp R trở lại, giấy B không có tính vật lý R | Cuộc gặp và việc ký không thể được xác minh dễ dàng trong 2012 | Muốn rời đi, hoặc phải tiếp tục tìm sự thật |
| Sau B07/B08 | Cổng không cho thoát, câu hỏi phòng 101 vẫn còn | Chỉ còn cách tự đối chiếu hiện trường năm 2012 | Bắt đầu CH02 ở R |

Câu hỏi của mỗi beat được phép bỏ qua trong phần UI, nhưng **không được tự ghi người chơi đã biết một thông tin mà họ chưa quan sát**. Người chọn A/B rất sớm vẫn hiểu rằng mình đã ký và không gian đổi, nhưng không được tự nhận ghi chú mô tả AS-05B như đã đọc. Các quyết định không thay số **13 người chết** của thông tin công khai, không thay quá khứ và không đặt nhãn thiện/ác lên HUD.

### 5. Phản hồi thao tác phải có nguyên nhân

- **Trước mỗi E/K**: vật tương tác phải có dấu hình học, vị trí ánh sáng hoặc hướng nhìn cho người chơi quyết định chạm. Không rải outline sáng lên mọi món từ xa.
- **Khi cầm vật**: nguồn vật, ngăn chứa và trạng thái “đã xem/chưa kiểm chi tiết” phải thống nhất. Một thẻ trong túi có thể được mở lại ở 3D, nhưng không tự cho quyền ký C nếu chưa xem dấu sàn.
- **Khi dùng sai món**: phát phản hồi tại chính target hoặc tự vấn ngắn của Minh, không khóa menu, không tiêu hao món trọng yếu; người chơi biết điều gì cần kiểm thêm.
- **Khi người chơi đọc chậm**: scene không sinh scare mạnh, không để thời gian đọc biến thành thua vì stamina/AI trong CH01.
- **Khi vật không thể mang**: cho phép quan sát và ghi nguồn tại chỗ, có cách quay lại khi cần. Đừng tạo một “mẫu vật ma” trong túi chỉ để tăng số món.
- **Khi không có chứng cứ đủ**: vẫn có đường A/B và tiến trình chính; C có thể được thử lại sau khi người chơi quay hiện trường.

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

#### Bảng dàn dựng encounter: thời điểm, camera và âm thanh

Các mốc dưới đây tính từ lúc CH01-T03 đã commit và sảnh B sẵn sàng; người chơi không bị yêu cầu đạt tốc độ cụ thể. Không ép cinematic bắt đầu chỉ vì hết một bộ đếm.

| Pha | Điều kiện kích hoạt | Diễn xuất và bố cục | Âm thanh | Tiến trình |
| --- | --- | --- | --- | --- |
| 0. Chuyển lớp | Minh tự giở AN01 ở R; preflight safe pad thành công | Hình học quầy giữ nguyên landmark, biểu hiện ánh đèn/quạt/giấy thay lớp; player ở đúng vị trí | Mưa R hạ dần, hum B và tiếng bút lên theo lớp; không tạo “nổ điện” | T03/T04, AP02; chưa gọi thoại Lan trước commit |
| 1. Lan chú ý | Player nằm trong vùng phát hiện hợp lý phía trước quầy sau T03 | Animation từ viết sang khựng bút → ngẩng → nhìn theo giới hạn; tối đa một lần “nhận ra” | Tiếng bút dừng, Lan dùng đúng line được guard | Ghi noticed once, chưa bắt thoại chính, chưa T05 |
| 2. Người chơi định hướng | Player ở quầy, chưa nhấn LAN_TALK | Lan chỉnh ghế/giấy/nhìn lại việc trực; không đứng bất động hàng chục giây | Roomtone B và âm giấy trở lại | Có thể soi lịch, nhìn đồng hồ, đi ra rồi quay vào |
| 3. Bắt chuyện | Player nhấn E/K trên LAN_TALK, đúng B, không inspector hay swap | Camera blend khoảng 0,6–1,0 giây sang shot medium không cắt đen, giữ hướng không gian | Tắt/duck cue dread, đặt VO và subtitles vào nhịp thoại | Đặt dialogue-start once, ngăn double-input |
| 4. Thoại chính | Dialogue start hợp lệ | Shot medium Lan → phản ứng Minh → giữ ánh mắt/đôi tay → rộng nhẹ về BOARD; không cắt liên tiếp chỉ để phô máy | Giọng nói rõ, môi trường nhẹ sau tiếng bút; khoảng lặng ngay sau thông tin năm 2005 | Chỉ ghi những line đã được phép trình bày; không tự ghi observation |
| 5. Trả FPS | Hoàn thành hoặc skip | Camera về yaw/pitch/FOV đã bảo vệ, input/cursor khôi phục, không đẩy capsule xuyên quầy | Âm giấy/đèn trở lại, không xếp hù ngay | Dialogue hoàn tất/skip có transcript; BOARD còn trên mặt quầy |
| 6. Minh nhận BOARD | Player tự nhấn E trên prop | Tay nâng BOARD, người chơi có thể nhìn nhiệm vụ và cất | Tiếng kẹp giấy/giấy sột, Lan không đọc lặp toàn cảnh | T05 commit, cửa 101 được phép tiếp cận |
| 7. Hậu gặp | Player quay về quầy hoặc đi ra HALL | Lan tiếp tục công việc vừa đủ, không bám đuôi Minh; khung cảnh B giữ dấu ấn ấm | Tiếng bút nhẹ trở lại | Có thể mở nhánh DRAWER-DESK/đồng hồ nhưng không bắt buộc |

#### Chỉ đạo diễn xuất điện ảnh

**Shot mở (~6–10 giây):** vào từ FPS, camera mượt đi từ mép quầy tới góc medium nhìn thấy Lan ngồi trước sổ. Nền sau vẫn thấy cửa, lịch và không gian trực. Minh không đột nhiên xuất hiện thành một mô hình 3D trọn người nếu rig hiện hành chưa bảo đảm continuity; có thể dựng over-the-desk thay vì over-the-shoulder.

**Shot nhận ra nhau (~10–20 giây):** Lan nhìn vào vị trí mắt Minh lâu hơn một câu giao việc thông thường, sau đó bớt căng khi nói. Chuyển động tinh tế của ngón tay giữ bút có ý nghĩa hơn một cảnh máy zoom sát mặt. Nếu thoại canonical không mô tả Lan vui, diễn xuất chỉ cho thấy **quen thuộc và một điều chưa nói**, không tự viết thêm động cơ.

**Shot lệch thời gian (~20–55 giây):** khi Lan nhắc ca 17/11/2005, camera không làm công việc giải đố. Đồng hồ B 03:17 nằm trong khung hoặc ở nền qua một chuyển góc hợp lý; player chỉ nhận OBS_CLOCK_0317 khi **đã chủ động tương tác kiểm sau cảnh**. Không dùng tiếng “xác nhận manh mối”, không cắt thẳng từ 03:17 sang hình bệnh nhân chết hoặc tường cháy.

**Shot giao việc (~55–90 giây):** thay vì một close-up F101 mà player chưa tới 101, dùng **BOARD** trên quầy. Lan có thể đưa mắt hoặc đẩy nhẹ bảng kẹp về phía gần Minh; nhân vật chính không bị camera cầm món thay người chơi. Khi cảnh kết, BOARD vẫn ở đúng chỗ có collider tương tác và prompt dễ thấy. Thời lượng là mục tiêu biên tập, không phải timer buộc câu thoại tự chạy nếu người chơi còn ở ngoài vùng.

**Nét mặt và khuôn miệng:** lip-sync dùng chính độ dài VO đã duyệt; cử động tay/bút không loop vô hạn trong shot tĩnh. Tránh ánh mắt tracking 360 độ khiến Lan trông như thiết bị an ninh. Chỉ có một vùng nhìn theo ngắn trong scripted lead-in, khóa hướng khi vào take điện ảnh, rồi trở lại hoạt động bình thường.

#### Luồng người chơi lệch tuyến

- **Đã giở sổ nhưng không tiến đến Lan:** Lan nhận biết khi Minh đi gần chứ không xuyên cửa. Player có thể ghé DRAWER-DESK, xem đồng hồ trước; câu thoại chính vẫn đợi họ chủ động tới.
- **Lan vừa ngẩng lên, player bỏ đi:** bút và ghế về idle có lý do, noticed once không phát lại lời chào. Khi quay về có prompt nói chuyện, không bị mất BOARD hoặc gate.
- **Player tiến quá nhanh qua trigger:** sự kiện phát hiện được xử lý once, nhưng không tự đóng cửa chặn lối để ép nghe. Người chơi vẫn có thể quay lại E/K.
- **Player đứng nhìn Lan rất lâu:** sau một vòng eye-contact, Lan hạ mắt làm việc; nếu cần cue, một ánh nhìn lại từ cô chỉ nhắc tương tác, không lặp cùng line.
- **Player nhìn vật ở sau quầy trong cinematic:** việc hiện hình đồng hồ/lịch trên màn ảnh **không cấp observation**, vì đó không phải hành động kiểm của Minh. Có thể mở inspector từng prop sau khi điều khiển được trả lại.
- **Bấm E/K liên tục hoặc mở ba lô đúng lúc thoại bắt đầu:** chỉ một hệ thống giữ quyền input tại một thời điểm. Nếu modal đang mở, từ chối start và cho thử lại sau khi đóng; không phát hai VoiceDirector sequence.
- **Skip ngay câu đầu hoặc load khi take chưa kết:** điều khiển, cursor, vị trí và camera luôn về trạng thái hợp lệ tại quầy; những thông tin cốt lõi đã được phép phát có đường đọc tóm tắt/transcript. Không phát lại toàn take thành một cảnh thứ hai, không đẩy T05 trước thao tác nhặt.
- **Chọn giảm chuyển động:** dùng góc tĩnh, cut mềm hoặc blend ít hơn thay vì cú camera sweep dài. Đảm bảo phụ đề/VO không bị rút ngắn theo tốc độ transition.

#### Giao dịch trạng thái và khóa tri thức

**Nhận diện Lan và cuộc trò chuyện là hai giao dịch riêng.** Chỉ sau khi RealityTransaction đưa player tới B an toàn mới cho LanActor hoạt động. Hệ gameplay phải chặn gọi StartEncounter tự động trong Hub nếu đường cinematic chủ động chưa sẵn; cờ nhận diện không được thay cờ hội thoại, cờ hội thoại không được tự thay CH01-T05. Việc nhận BOARD commit theo tương tác vật phẩm độc lập; nếu save tại quầy sau thoại nhưng trước khi lấy, load lên vẫn thấy BOARD và cửa Phòng 101 chưa được nhận nhiệm vụ.

Thứ tự trạng thái tối thiểu: **T03/T04 → Lan noticed → LAN_TALK completed/skipped → T05**. Nếu thiết kế cho phép lấy BOARD không xem trọn thoại vì Skip, nó vẫn phải bảo đảm nội dung cốt yếu được truy cập qua transcript mà không tiết lộ các line khóa. CH01-D03/D04/D05/D07 và các line tùy chọn phải tuân Context hiện hành; CH01-D16b/CH04-D06 không thuộc cuộc gặp này.

**Những điều người chơi được biết khi rời quầy:** Lan có vẻ tiếp tục sống trong ca 2005, Minh thấy năm 2012, nhiệm vụ là kiểm một phiếu ở 101. Những điều chưa được khẳng định: Lan là gì, ai ở giường 4, 03:17 nghĩa là gì. Cảm xúc được xây trên ký ức Minh về Lan; tri thức vẫn phải được kiểm tra bằng thế giới.

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
7. Sau rèm là **OBS101A**: vệt bánh quanh chỗ kê giường, chăn gấp vội, dép nhựa nhỏ. Người chơi tự nhìn từ nhiều góc, có thể cúi xuống hoặc dùng ánh đèn để thấy chỗ mòn. Khi người chơi **tự kiểm dấu tại chỗ**, hệ thống mới ghi `OBS101A` ở mức `Examined` với provenance **B / LOC03 / CH01**. Pocket Note lưu một **tham chiếu hiện trường** để Minh có thể gọi lại khi đối chiếu F101; đây không phải ảnh chụp hay vật chứng cầm được. **Không cất dấu sàn hoặc đôi dép thành evidence xuất** trong Chương 1. Người chơi chưa kiểm sẽ không có tham chiếu đủ dùng để liên kết.
8. Người chơi có thể quay nhìn ba giường còn lại rồi trở lại hốc 4. Đây là **Spatial Deduction**: tự so tương quan không gian, không kéo một biểu tượng “dấu bánh giường” sang biểu tượng “giường 4” trong menu.

#### Thẻ AS-05B — sự bất thường có chữ ký

9. Trên giá kẹp tại vị trí thứ tư, người chơi nhấc **AS-05B (OBS101B)**. PhysicalInspector mở trên chính chiếc thẻ được cầm: mặt trước, mặt sau, ô bàn giao và chữ ký nhận còn thiếu.
10. Người chơi lật thẻ, nghiêng đón sáng hoặc dùng lệnh trợ năng tương đương để khám phá vết hằn **“03g17 · CHƯA CHỐT SỔ”** ở mặt sau. Chi tiết này là câu hỏi về mốc giao ca, **không cho phép kết luận một người chết lúc 03:17**.
11. Người chơi có thể **cất tạm AS-05B vào ngăn giấy thuộc B**, lấy ra kiểm lại khi cần hoặc đặt nó ở một điểm so tài liệu hợp lệ. Hệ thống vẫn giữ nguồn và trạng thái đã Examined. Cầm thẻ rồi quên kiểm ô chữ ký sẽ không tự cho phép ký Refuted. Khi người chơi focus/đọc ô nhận còn trống, `OBS101B` trở thành nguồn đã kiểm; **việc cất vào túi không tự đánh dấu ô ký hoặc tạo liên kết**. B04 phải đặt được câu hỏi “vì sao giấy báo không còn tồn đọng nhưng hồ sơ bàn giao còn thiếu người nhận?” mà chưa phát đáp án chính thức.

#### Điều tra phụ có ý nghĩa

12. **STAFF:** mở DRAWER-NURSE để đọc **OBS_SHIFT_LOG / quy chế AS-05**; xem **OBS_CORRIDOR_NOTICE**. Tác dụng là hiểu một chuyển giao hợp lệ cần có dấu nhận và cách kê giường tạm được quản lý. Đây là nền nghiệp vụ, không phải chìa khóa UI bắt buộc.
13. **Giường 2:** đọc **OBS101C**, phiếu chuyển đã hoàn tất có đầy đủ dấu bàn giao. Người chơi nhận ra **một hồ sơ đúng quy trình trông như thế nào**, từ đó biết tại sao thẻ giường 4 gây nghi vấn. Không cần đọc C để được ký Refuted nếu đã đủ hai nguồn chính.
14. **Giường 3:** mở DRAWER-BED3 xem **OBS101D**, phiếu thuốc/việc hoàn tất của giường 3. Nó cho thấy giấy tờ có thể khớp đúng một vị trí mà **không** chứng minh điều gì về vị trí 4. Nếu đem phiếu này làm căn cứ bác bỏ F101, Minh phản hồi nguồn đó nói về giường khác.
15. **Mốc thời gian tùy chọn EX01:** nếu đã tự xem đồng hồ B ở quầy, soi vết hằn mặt sau AS-05B và đọc một nguồn ca trực tại quầy hoặc STAFF, người chơi có thể tự ghi mối liên hệ **03:17** trong Pocket Note mở rộng. Trình tự nguồn có thể bất kỳ. EX01 chỉ mở khả năng nghe CH01-D19 sau khi ký, không thay kết luận F101.

#### Ba lộ trình điều tra hội tụ tại cùng một câu hỏi

Người chơi đi qua các phòng theo ý mình, không có thứ tự thu thập chính thức. Thiết kế phải xử lý ba trường hợp đều hợp lý:

| Lộ trình | Thứ tự người chơi có thể tự chọn | Hiệu ứng lên cách hiểu | Khả năng tiến |
| --- | --- | --- | --- |
| **Hiện trường trước** | Quầy → HALL → mở 101 → vết bánh/chăn/dép → AS-05B → vòng STAFF xem quy chế | Người chơi nhìn thấy điều lạ rồi mới tìm một tiêu chuẩn nghiệp vụ để đặt câu hỏi | Có thể ký A/B bất kỳ lúc nào; C khi đã có đủ cặp quan sát và lập luận |
| **Quy chế trước** | Quầy → DRAWER-DESK/đồng hồ → STAFF xem bảng, quy chế → HALL → Phòng 101 | Player biết dấu bàn giao hoàn tất ra sao rồi tự nhận ra ô nhận thiếu | Không được thưởng “đã giải” trước khi soi hiện trường |
| **So hồ sơ ngang trước** | Quầy → 101 → hồ sơ giường 2/3 → rèm 4 → thẻ 4 → có thể quay STAFF | Người chơi thử giả thuyết “giấy tờ 101 đều bình thường”, rồi gặp nguồn ngoại lệ | Giường 3 không tạo căn cứ sai cho nhánh C; không phải về STAFF nếu không muốn |

Không khóa cửa STAFF dựa trên việc đã xem giường 4, cũng không buộc một món ở STAFF mở ngăn kéo giường 3. Lối phòng STAFF qua DOOR-STAFF và đường hành lang chính phải hội tụ tự nhiên; mọi cửa/giấy sau khi được mở/đặt vẫn có đường quay lại, không làm biến mất khỏi thế giới khi chạm E nếu không thật sự nhặt.

#### Thao tác không gian theo lớp — người chơi tự chứng minh bằng đôi chân

**01. Ngưỡng cửa:** khi nhìn qua khe DOOR-101, ở phía đối diện là rèm chứ không phải toàn bộ hốc giường. Người chơi quyết định mở cửa thêm, bước vào hay nhìn bảng F101. Âm quạt và vùng sáng giường hiện hữu giúp không gian giống buồng bệnh đang làm việc, không phải một “căn phòng puzzle” được sắp đặt bằng HUD.

**02. So tương quan giường:** từ vị trí giường 1–3, hướng nhìn tới tường/đầu giường cho thấy cách vật dụng đặt đúng tầm người. Minh có thể đi từng bước sang khoảng trống sau rèm, quan sát mặt sàn và dấu đặt chân giường. Nếu người chơi nhìn từ góc xấu chưa thấy, âm cọ dép/dấu xước không tự bật thành toast giải thích; chỉ có prompt khám phá nhẹ khi đến đủ gần. Bốn ổ cắm CH02-R không được đưa sang đây như một mini-game bốn công tắc.

**03. Vệt sàn dưới ánh xiên:** vệt bánh/đường kéo là texture/decal gắn mặt sàn lớp B, không biến thành item. Người chơi cúi hoặc xoay đèn pin để thấy rõ những đường mòn ở ngưỡng hốc 4. Nếu thích thao tác ánh sáng, giữ T / điều chỉnh nguồn sáng một nhịp; nếu không thể xoay camera hoặc cần chữ lớn, action “Kiểm dấu dưới ánh sáng” cung cấp mô tả tương đương và ghi stage Examined. Không yêu cầu đúng 25°–40° mới có quyền hiểu dấu; độ nghiêng đó chỉ là hướng dàn dựng cho thẻ giấy.

**04. Rèm có lực cản:** rèm đang khép một phần. Khi người chơi giữ kéo, lực ma sát tạo phản hồi qua tốc độ và âm ray; một cú nhấn E cũng mở tới trạng thái đủ tiếp cận AS-05B. Nếu kéo nửa chừng rồi đi, save/load phục hồi vị trí hợp lý, không tạo collider vô hình chặn đường. Rèm rung nhẹ vì luồng gió có thể xảy ra, nhưng không là vật chứng cho sự tồn tại của bệnh nhân.

**05. Những đồ vật mang đời sống:** chăn gấp vội, dép nhựa nhỏ và dấu khung giường có kích thước/độ mòn thống nhất. Không có dòng “bé An” hiện trên dép và không tự gọi nó là dép của bệnh nhi. Giá kẹp tại vị trí 4 nằm trong tầm thấy sau khi kéo rèm; không được đẩy sâu vào điểm mù buộc người chơi click hunt trên một texture tối.

**06. Thẻ trước và sau:** nhấc AS-05B, thấy mặt trước. Đảo mặt là một hành động có mục đích: ô người nhận bàn giao trống và vết hằn bên kia khiến người chơi nghi quy trình chưa hoàn tất. Nếu người chơi chỉ nhìn mặt trước rồi cất, observation có thể dừng ở Examined; việc hiểu vết hằn phụ EX01 vẫn còn mở. Minh có thể cầm thẻ tới bàn so giấy ở STAFF/101 hoặc cất vào ngăn giấy B; **bản cất tạm không đổi thành evidence**.

#### Những sai lầm phải được phản hồi như một suy nghĩ nghề nghiệp

| Điều người chơi thử suy ra | Vì sao chưa đủ | Phản hồi phù hợp |
| --- | --- | --- |
| Chăn và dép chứng tỏ một người đang trong phòng | Chỉ xác nhận dấu sử dụng; hiện tại phòng trống | “Có người từng dùng chỗ này… nhưng không biết họ đang ở đâu.” |
| Thẻ AS-05B tự chứng minh kê giường thật | Một thẻ có thể còn sót, thiếu đối chiếu hiện trường | “Một tờ thẻ bỏ lại chưa đủ chứng minh tình trạng phòng.” |
| Phiếu giường 2 hoàn tất chứng minh giường 4 cũng hoàn tất | Là hai nguồn riêng, giường 2 là đối chứng quy trình | “Chữ ký ở giường 2 rõ ràng. Còn vị trí này thì sao?” |
| Phiếu thuốc giường 3 được dùng bác số 0 | Nó nói về giường khác và không chỉ dấu trạng thái giường 4 | “Không thể lấy giấy giường 3 để nói về vị trí thứ tư.” |
| Đồng hồ 03:17 là giờ có người chết | Chỉ là dấu thời gian, chưa có chứng cứ tử vong tại đó | “Trùng mốc không đồng nghĩa biết chuyện đã xảy ra.” |
| Thiếu chữ ký = có chủ ý làm giả | Chưa biết lỗi nghiệp vụ, hoàn cảnh hay chủ thể quyết định | “Không thể kết tội ai chỉ bằng một ô còn trống.” |

Các phản hồi là **hướng diễn đạt/diễn xuất chờ duyệt với kho thoại**, không tự phát thành VO mới khi chưa có line được cho phép. Ở gameplay, phải trả một lý do có thể hiểu thay vì toast màu đỏ “Sai”.

#### Đối chiếu giấy thực tế và vòng quay lại STAFF

Khi player mở DRAWER-NURSE, động tác kéo ngăn tủ là thao tác chính, không cần thêm một khóa số. Quy chế **OBS_SHIFT_LOG** có thể được rút ra, lật, cất tạm ở B và đặt cạnh **OBS101C** hoặc thẻ **AS-05B**. Người chơi đọc hai chữ ký/chỗ nhận ở giường 2 rồi nhìn ô trống của giường 4 bằng mắt mình; **bàn so 3D là nơi đặt đồ chứ không là máy xác nhận lời giải**. Khi nhấc lại một giấy, không xóa observation đã lưu; khi đặt cả hai cùng một anchor, không nhân bản bản gốc hay tự mở G01.

**Khả năng quay lại:** nếu người chơi tới 101 trước rồi muốn hiểu quy định, đường về STAFF phải rõ qua HALL, không bắt đi một vòng cảnh khác. Khi quay lại 101, cửa/rèm giữ trạng thái đã mở để cảm giác người chơi thực sự đã can thiệp vào căn phòng. Nếu chưa cất thẻ mà để trên bàn, trạng thái anchored placement phải tìm lại được. Khi mất điện/giảm sáng vì scare, những vật liệu cần đọc phải trở lại đủ sáng.

#### Nhánh thời gian 03:17 — một câu hỏi tùy chọn có ba nguồn

Phát hiện **CH01-EX01** chỉ hình thành nếu player tự thu đủ: (1) **xem đồng hồ dừng ở 03:17 trong lớp B**, (2) **soi nghiêng mặt sau AS-05B để thấy “03g17 · CHƯA CHỐT SỔ”**, (3) **xem ít nhất một nguồn ca** ở quầy hoặc STAFF. Các nguồn có thể được xem theo bất kỳ thứ tự nào; việc cinematic B03 đưa đồng hồ vào khuôn hình **không** thay (1). Một cú lật thẻ thông thường chưa thấy vết hằn **không** thay (2).

Sau khi tự liên hệ các nguồn, người chơi có thể ghi một câu hỏi trong phần ghi chép Pocket Note: “Hai dấu thời gian cùng chỉ 03:17 — vì sao?”. Không xuất hiện banner kết luận tai nạn hoặc người đã chết. EX01 chỉ là tiền đề mở line CH01-D19 khi đã ký; nếu thiếu một nguồn, không mất đường A/B/C hay Chương 2. Nếu người chơi cố chọn một mốc khác, có thể giữ giả thuyết chưa xác nhận để sau này xét lại, không tự sửa số trên các tài liệu.

#### Dread được tạo bởi chính việc soi một nơi cụ thể

- Sau khi thấy OBS101A, tiếng bánh từ hành lang **có thể** dừng đúng nơi vệt kéo trên sàn hướng tới. Player quay ra sẽ gặp hành lang trống; camera không bị khóa nhìn một cỗ cáng đang lao đến.
- Khi nhấc thẻ B, ánh sáng ở nơi giường 4 có thể hạ nhẹ; nếu đang trong inspector, không phát bất kỳ giật âm/đèn hay làm người chơi đánh rơi tài liệu.
- Khi người chơi tự quay nhìn lại hốc 4 sau khi hiểu chữ ký thiếu, **một tiếng móc rèm/ngăn kéo** có thể đặt câu hỏi “ai đang dùng nơi này?”. Đó là setup cho silhouette đơn lẻ ở B05, không thêm bốn jumpscare cho cùng một khám phá.
- DeferredMicroShift chỉ cho phép rèm dịch chút hoặc một bóng đèn lệch một nhịp **sau khi thoát chế độ đọc**, không làm dấu bánh, thẻ hay cửa biến mất khi chưa được ghi nguồn.

**Điều kiện mở bước tiếp:** CH01-T06 hoàn tất khi người chơi vào 101; F101 luôn có đường tương tác A/B ngay cả khi player chỉ nhìn một phần căn phòng. Nếu muốn C, validator sẽ chờ đúng OBS101A+B và lập luận được kiểm chứng ở B05. Người chơi không bị ép đi lại STAFF để mở một khóa tùy tiện.

#### Nhịp kinh dị

Khi Minh kiểm dấu bánh hoặc hạ thẻ sau khi đọc ô bàn giao, tiếng xe kim loại/tấm rèm có thể vang **đúng từ nơi thiếu dấu xác nhận**. Một chi tiết rèm khép thêm hay quạt chậm lại được phép xuất hiện sau khi camera về 3D, không trong inspector. Không dùng bóng người làm “phần thưởng chắc chắn” cho người chơi đọc tài liệu; sự yên ắng sau vật chứng có thể đáng sợ hơn.

#### Điều người chơi thực sự biết

Có một vị trí sử dụng khác với cách phiếu mô tả; thẻ bàn giao liên quan chưa có xác nhận hoàn tất. **Chưa chứng minh** có bệnh nhân vẫn còn trong phòng, có ai cố tình làm giả giấy, ai nằm tại đó hoặc vì sao 03:17 xuất hiện. Hai quan sát chính cho phép người chơi chuẩn bị một lập luận giới hạn ở B05.

---

### B05 — CHỮ KÝ TRÊN CON SỐ 0
**Khu vực:** Phòng 101, B, bảng kẹp F101 tại AN02.  
**Thời lượng:** 5–7 phút nếu tự kiểm và lựa chọn; tuyến A/B có thể ngắn hơn.  
**Cảm xúc:** Người chơi hiểu rằng đặt bút cũng là một hành động gây hậu quả.

#### Bảng kẹp biến thành nơi người chơi tự làm công việc điều tra

1. **Trở lại AN02:** F101 vẫn ở bảng kẹp Phòng 101/B. Người chơi đọc trường **“Chưa bàn giao: 0”** bằng chính mắt, có thể nhấc mép/lật xem rồi đặt trở lại. Không có một màn tutorial nói “Hãy tìm 2 manh mối” và không biến F101 thành vật phẩm di động bị mất khỏi hiện trường. Nếu Minh muốn ký **A** hoặc **B**, có thể chọn ngay mà không phải giải phần so sánh.
2. **Mở nơi đối chiếu:** chọn hành động **“Đối chiếu tại đây”** ở mép kẹp. Camera đến gần vừa đủ thấy giấy F101 ở giữa, kẹp phụ bên cạnh và một **tờ nháp nhỏ của Minh** dưới đáy. Bàn không phải một menu sơ đồ suy luận trừu tượng; người chơi có thể ngẩng nhìn hốc giường, đóng và quay về FPS. Tờ nháp chỉ dùng để tổ chức lập luận, không được đưa vào hệ \`evidence\`.
3. **Đưa thẻ thật tới kẹp:** mở ba lô, chọn AS-05B đã nhặt, **lấy ra**, đặt bên cạnh F101; âm kẹp và hình giấy đổi đúng trạng thái tay/túi/bàn. Nếu không cất thẻ mà còn ở giá, người chơi quay lại lấy nó; việc đặt giấy không tạo bản sao. Kẹp đúng thẻ **không tự xác nhận đúng**, không thêm mark hay link.
4. **Đánh dấu điều F101 khẳng định:** bật chế độ bút, nhìn vùng tổng hợp và khoanh/gạch con số **0 (F101_ZERO)**. Một nét mực thật xuất hiện, với Undo/tẩy. Nếu khoanh bảng kê khác, vẫn thấy mực đã khoanh — thao tác thành công **không đồng nghĩa kết luận đúng**. Bàn phím/chữ lớn cho phép chọn tên vùng giấy thay vì phải nhắm pixel.
5. **Đánh dấu điều AS-05B bỏ ngỏ:** nhìn thẻ, chọn **ô người nhận bàn giao chưa ký (OBS101B_SIGN)** và đánh dấu. Người chơi có thể lật lại hoặc zoom chi tiết. Nếu mark mục tình trạng khác, không tự sửa sang ô ký.
6. **Liên hệ với hiện trường, có hai cách điều khiển cùng một kết quả:**
   - **Ngay tại kẹp:** tờ nháp hỏi **“Điều gì trong phòng khiến ô nhận này đáng nghi?”** Người chơi mở phần nguồn đã kiểm của Pocket Note và tự chọn một ghi nhận: có thể là **dấu sử dụng hốc 4**, hồ sơ giường 3 hoặc đồng hồ nếu các nguồn ấy đã thực sự được xem. Chọn **dấu sử dụng hốc 4** để ghi vào tờ nháp. Chỉ các nguồn ở stage Examined/provenance hợp lệ mới được dùng như quan sát hỗ trợ.
   - **Tại nơi có vết thật:** người chơi cầm/nhớ thẻ AS-05B, quay về vùng \`OBS101A_TRACES\`, nhìn dấu và chọn **“Đối chiếu với ô nhận trên AS-05B”**. Khi quay lại bàn, liên hệ đã được Minh ghi trên nháp. Không buộc quay đi quay lại nếu người chơi đã quan sát dấu trước đó.
   
   Dù chọn cách nào, hành động đều phải được player chủ động xác nhận, rồi lưu \`OBS101B_SIGN → OBS101A_TRACES\`. **Không auto-link chỉ vì đặt hai tờ cạnh nhau.** Nếu chọn sai nguồn, tờ nháp vẫn ghi nhận lựa chọn nhưng không được validator coi là liên hệ chứng minh con số 0.
7. **Tự chọn sức mạnh của lời ghi:** trên tờ nháp, player chọn mức phát biểu. Lời hẹp có thể bảo vệ là **“Số 0 trên F101 chưa đủ căn cứ xác nhận.”** Các suy đoán mạnh hơn như “có bệnh nhân còn trong phòng” hay “đã có người cố tình làm giả” không thể trở thành C hợp lệ chỉ nhờ hai nguồn này.
8. **Xem lại căn cứ rồi ký:** người chơi có thể chọn **“Đọc lại căn cứ”**, xem hai nét mark, nguồn vật lý và mối liên hệ do chính mình tạo. Sau đó đặt bút ký. Hệ thống mới đánh giá tổng thể \`OBS101A+B\` đã Examined ở B, marks \`F101_ZERO / OBS101B_SIGN\`, link \`OBS101B_SIGN → OBS101A_TRACES\` và claim giới hạn; thiếu bất cứ phần nào thì không cho ký **C**, nhưng A/B luôn mở. Nếu lỗi, Minh phản hồi về *loại thiếu sót* (nguồn chưa xem, vùng mark, link, claim vượt chứng cứ), không báo chính xác pixel đúng hoặc cho ✓ từng bước.

**Điều cần người chơi tự hiểu:** tờ giấy đang khẳng định “0”, thẻ đi cùng còn thiếu ký nhận, còn mặt sàn cho thấy chỗ giường thứ tư có dấu sử dụng. Sự kết hợp đủ để **từ chối xác nhận 0**, chứ chưa đủ để xác nhận thủ phạm, nạn nhân còn sống hoặc giờ tử vong. Gameplay phải dẫn đến cảm giác **mình tự chỉ ra một mâu thuẫn có thật**, không phải thử đủ bốn lệnh để được cho qua.

#### Ba chữ ký đều hợp lệ

**A — Affirmed / Xác nhận 0.** Người chơi có thể ký ngay cả khi chưa khám phá giường 4. Chữ ký được commit, giấy hiển thị bản signed A, **D(LOC03)=1** trong lớp B. Vách che **deniedBay** đóng kín khu vực hốc 4; quạt đều và lạnh, sau vách có hai tiếng gõ nén. Cảnh không khẳng định có người đang bị nhốt; điều đáng sợ là **một tờ giấy có thể khiến vị trí từng nhìn thấy bị phủ kín**.

**B — Unverified / Để ngỏ.** Người chơi có thể ký ngay không cần tài liệu. Giấy giữ trạng thái chưa xác minh, **D(LOC03)=0**. Rèm và khoảng giường lửng lơ; tiếng quạt chậm và một khoảng im kéo dài. Game không trừng phạt Minh vì thừa nhận chưa đủ thông tin.

**C — Refuted / Ghi nhận bất nhất.** Chỉ ký được sau khi lập luận hợp lệ bằng OBS101A/B và marks/link/claim đúng. **D(LOC03)=0**. Rèm tách, hốc giường lộ rõ hơn; mảng tường ẩm, dấu bánh đậm và góc CH01_PR01_Corner được nhìn thấy theo hiện trạng đã định. Không có lời thú nhận, tên bệnh nhân hay đáp án thủ phạm.

7. **Chữ ký gây hậu quả trước mặt người chơi.** Bút/chốt giấy, một khoảng dừng, rồi sự thay đổi của căn phòng. Không che toàn bộ chuyển cảnh bằng một flash khiến player không kịp thấy kết quả do mình gây ra.
8. **G01 mở ở cả A/B/C** ngay sau commit bền vững CH01-T07. Người chơi có thể đứng lại quan sát và sửa quyết định trước khi chuyển B→R. Lần ký đầu lưu firstDecision; mỗi sửa đổi hợp lệ cập nhật current/revision/history; bấm lặp cùng lựa chọn không nhân số lần.
9. Nếu EX01 đã hoàn tất, khi rời Phòng 101 người chơi có thể nghe CH01-D19 đúng điều kiện; không tự kích cho người bỏ qua nhánh phụ.

#### Cách các nét mực và tờ nháp phản hồi hành động

**Hai vị trí có trọng lượng:** bảng F101 là nguồn gốc của con số 0, còn AS-05B là nguồn của ô nhận. Hai tờ có thể nằm cạnh nhau nhưng không biến thành một ảnh UI cố định; Minh có thể cầm, lật, rút khỏi kẹp, lấy lại và xem bản chữ lớn. Trên F101, vùng “Bảng kê” và “Tổng hợp 0” là hai vùng khác nhau; trên AS-05B, mục “Tình trạng” và ô “Người nhận” cũng khác nhau. Nét mực chỉ đi theo hotspot được người chơi chọn, **không tự tìm ô đúng**.

**Tờ nháp hiện đúng bốn dòng**: “Trên phiếu” (mark F101), “Trên thẻ” (mark AS-05B), “Tại hiện trường” (một reference đã xem do player chọn), và “Tôi có thể khẳng định” (claim do player chọn). Các dòng chưa được chọn để trống; việc nhìn thấy dấu sàn trong game không tự điền liên hệ. Nét liên kết có thể là một đường bút nhỏ trên **giấy nháp** với tên nguồn, không phải dây đỏ xuất hiện trong không khí. Nếu người chơi chọn nhầm nguồn, gỡ liên kết phải trả lại đúng trạng thái trước đó.

**Không thử độ chính xác chuột:** click phải vào vùng giấy hoặc chọn hotspot qua menu + phím M đều là cùng một thao tác. Undo làm biến mất nét mark tương ứng và cập nhật nháp, không xóa bản giấy gốc hay observation. Đối chiếu giữa một giấy và một dấu cố định không buộc vật lý dấu sàn phải “lấy ra khỏi ba lô”. Nếu người chơi đi lại từ kẹp tới hốc 4, nháp vẫn giữ những gì đã đánh dấu.

**Không giải bằng feedback từng slot:** sau một mark, âm bút hoặc dòng ghi trên giấy chỉ xác nhận thao tác. Trước khi ký C, tùy chọn xem lại căn cứ cho biết các loại nguồn/mối liên hệ còn thiếu. Khi submit không hợp lệ, phản hồi chọn đúng nhóm \`HypothesisOutcome\` và một câu hỏi nghề nghiệp giúp thử tiếp; không hiển thị chấm điểm đạo đức hoặc làm khóa mất lựa chọn B.

#### Ranh giới giữa nhận định hợp lý và suy đoán vượt chứng cứ

**Nhánh A không cần phát hiện gì:** một người chơi mệt, không tin vào hiện tượng, hoặc muốn làm xong việc Lan giao có thể chọn xác nhận số 0 trên F101 ngay khi mở form. Game không ngăn cản chỉ vì “chưa nhặt đủ manh mối”; hậu quả tự sự của một quyết định quá dễ dàng đã thể hiện bằng vách mới.

**Nhánh B không phải câu trả lời yếu:** có thể nói chưa đủ cơ sở dù chưa tìm thấy thẻ. Không trừ điểm hay đặt người chơi vào trạng thái “hết đường điều tra”. Sự bất định vẫn là một trạng thái được tôn trọng.

**Nhánh C là ghi nhận bất nhất, không phải lời kết án:** cả dấu sàn và thẻ thiếu chữ ký đều phải được kiểm. Nếu player chỉ thấy chăn, chỉ cầm thẻ mà chưa lật, hoặc đặt giấy thuốc giường 3 bên F101, hệ thống trả phản hồi về **thiếu nguồn hoặc nguồn không liên quan**. Nếu chọn câu “có người hiện đang bị nhốt trong phòng” hoặc “nhân viên cố tình làm giả”, validator từ chối vì phần đó không được chứng minh. Các câu trả lời của Minh phải tránh kết tội người chơi: chúng mô tả độ chắc chắn của chứng cứ.

**Những trường hợp phải có phản hồi riêng:**
- OBS101A có nhưng OBS101B chưa Examined: không có cơ sở liên hệ một thẻ bàn giao thật.
- OBS101B có nhưng OBS101A chưa Examined: thẻ có thể là giấy lưu lại; chưa chứng minh hiện trường tương ứng.
- Đã xem cả hai nhưng đánh dấu sai trường: báo rõ “dấu cần kiểm không nằm ở đây” và cho người chơi thử lại, giữ item.
- Đánh dấu đúng nhưng không tạo liên hệ tới dấu sàn: nhắc rằng các nguồn còn rời rạc, không auto tạo link vì thẻ đã ở gần bảng.
- Claim FALSIFICATION/PATIENT_PRESENT: chỉ ra kết luận vượt phạm vi nguồn, không tạo “bí mật sai” trong canon.
- Dẫn OBS101D/đồng hồ 03:17 làm căn cứ trực tiếp: trả rằng chúng không chứng minh sai lệch của tổng số giường, dù có ý nghĩa phụ.
- Quay về ký A/B sau khi thử C không thành: ký hợp lệ ngay, không bị khóa trong một nhánh “đã chọn C nên phải giải xong”.

#### Ba diễn biến sân khấu sau chữ ký — mỗi lựa chọn có một nỗi sợ riêng

| Kết luận | Cơ học của căn phòng ngay sau bút ký | Âm thanh và ánh sáng | Dư vị của người chơi |
| --- | --- | --- | --- |
| **A · Xác nhận 0** | Vách deniedBay thay đổi phần hốc 4, báo cáo F101_SignedA hiển thị trên giá; đường đi còn an toàn | Quạt đều đặn như máy, lạnh sáng. Hai tiếng gõ nén sau lớp che chỉ phát sau khi người chơi nhận ra vách | Ký một dòng trên giấy có thể che khuất một nơi mình từng nhìn |
| **B · Để ngỏ** | Rèm pendingBay và khoảng trống không được “giải quyết” bằng hình học | Đèn không thưởng màu sắc thắng; quạt lệch nhịp, yên lặng kéo dài rồi âm môi trường trở về | Không biết cũng là một quyết định có sức nặng |
| **C · Ghi nhận bất nhất** | Rèm tách, refutedBay lộ mảng ẩm, vết bánh và góc CH01_PR01_Corner rõ hơn | Một tiếng kẹp giấy khô, đèn tuýp chập rất nhẹ, tiếng bề mặt vật liệu rạn có vị trí | Thế giới chống lại sự phủ nhận, nhưng vẫn chưa nói ai nằm ở đó |

**Điều phối trình tự:** người chơi phải thấy mực/chốt giấy trước → nghỉ một nhịp → thay đổi hình học/ánh sáng trong tầm nhìn hoặc âm thanh nhận thức được → cho tự quay lại kiểm. Không chồng một cú silhouette cùng âm nứt hoặc tiếng gõ hậu ký; đây là hai loại scare khác nhau. Âm gõ sau vách chỉ hợp nhánh A, không phát trong B/C. Cảnh C không được dựng như “phần thưởng có quái vật đặc biệt” chỉ dành người chọn đúng: cả ba đều là kết cục kể chuyện có trọng lượng.

#### Quyền quay lại và thay đổi quyết định

Trong B khi T08 chưa commit, player có thể trở lại AN02 ký lại. Lần đầu luôn được lưu vào firstDecision; trạng thái hiện tại thay đổi trong currentDecision, revision chỉ tăng khi có quyết định mới thực sự khác, và history giữ toàn bộ nguồn/marks/claim của lần ký. Nếu người chơi ký A rồi đọc AS-05B và chuyển sang C, căn phòng biến từ deniedBay sang refutedBay **mà không phá vị trí đặt đồ hoặc chặn đường ra**. Nếu từ C chuyển sang B, phần đã xem và thẻ không bị xóa; chỉ trạng thái hiện diện B thay đổi.

Khi người chơi mở lại bản sao biểu mẫu sau A/B/C, một hiệu ứng mực trắng hoặc nét mực lửng có thể chồng **lên lớp trình bày trong B** tương ứng chữ ký. Chế độ xem nguồn gốc luôn trả bản scan chính xác; không được sửa ObservationRecord, bản gốc, chữ viết tay của Minh hoặc save để “hù”. Nếu chọn A, hình ảnh mực che chỉ là cảm giác hành chính đang thắng chỗ trống; không hàm ý chứng cứ đã bị xóa khỏi lịch sử.

#### Kỷ luật save, vị trí và giải quyết ngắt giữa chừng

Khi player bấm ký, system kiểm state và điều kiện **trước** khi chạy hiệu ứng hủy hiện trường. Commit bền vững CH01-T07/first/current/revision/G01 rồi mới bật biến dạng B và VO/PA tương ứng. Nếu save lỗi giữa ký, giữ nguyên form và cho thử lại; không được xuất vách che khi state chưa ký. Nếu player skip/cancel animation bút, kết quả đã commit thì vẫn có hậu quả; nếu chưa commit, tuyệt đối không tạo half-signed state.

Có thể đọc lại F101 qua inspector hoặc phần ghi chép nguồn trong ba lô mà không tạo thêm một F101 physical copy trong túi. Với người thao tác bàn phím hoặc trợ năng, các hotspot được duyệt theo vùng tương đương, không yêu cầu dùng chuột chính xác. Sau khi T08, không quay lại B ký mới từ R bằng một menu ba lô; các thay đổi sau này đi qua cơ chế CH02 và điều kiện canon riêng.

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

#### Cách quay lại một nơi đã trở thành quen thuộc

**Cảnh đầu B06:** sau chữ ký F101, người chơi không bị kéo về AN01 bằng một transition. HALL vẫn đi được, tiếng đèn và xe đẩy có thể giống lần đến nhưng đã mất sự chắc chắn. Nếu quay đầu nhìn cửa Phòng 101, sự khác nhau do A/B/C vẫn có mặt để kiểm. Nếu player xem lại thẻ trong túi, dấu đã thấy vẫn rõ; **sự thay đổi của giấy ký không được xóa dấu người chơi đã chứng kiến**.

**Điểm đảo cảm xúc:** trước đó Minh đi về phía có Lan như một nơi an toàn. Bây giờ cùng một trục hành lang kéo anh quay về một người có vẻ cần sự giúp đỡ. Người chơi nhìn thấy quầy từ xa, không có tiếng gọi, không có tiếng bút, nhưng ánh sáng, sổ và ghế còn chỗ quen thuộc. Thiết kế phải cho người chơi ít nhất một cơ hội tự quay camera tìm Lan trước khi một thoại/caption nhận xét vắng mặt xuất hiện.

**Đến quầy theo nhiều cách:**
- Nếu đi thẳng tới AN01, thấy ghế trống ngay nhưng không có camera tự pan xác nhận. Người chơi có thể đứng và nhìn vị trí mắt Lan ở B03; không hù bằng bóng Lan xuất hiện sau vai.
- Nếu ghé DRAWER-DESK hoặc xem đồng hồ B, có thể nhận một bất thường âm thanh rất nhỏ: chiếc bút không còn viết, đồng hồ vẫn giữ vai trò mốc nhưng **không** phát kết luận giờ chết.
- Nếu gọi/tương tác vị trí LAN_TALK khi Lan đã vắng, không mở cinematic hay dialogue cùng lời chào. Chỉ có câu phản hồi ngắn đúng thời điểm, hoặc im lặng có nghĩa.
- Nếu tới quầy trước khi hoàn thành G01, lối T08 chưa hợp lệ; cần hướng player về F101 tại 101, không tự cho chuyển R và làm mất cơ hội ký.
- Nếu quay về sau khi ký nhưng muốn sửa, người chơi có thể bước lại LOC03. Không đổi layer khi chỉ đến gần quầy; **chỉ giở AN01** mới bắt đầu giao dịch bàn giao.

**Hành động bàn giao:** một trang sổ trực vật lý mở trước mặt Minh, trên đó có thể thấy dòng trạng thái công việc nhưng không phải bản F101 gốc. Minh đặt tay lên mép trang, giở và xác nhận bàn giao; camera vẫn FPS với thời điểm chuyển lớp gắn vào động tác. Đây là phản chiếu của lần giở sổ B03: hành động y hệt, người ghi ở quầy đã biến mất. Nếu người chơi đang cầm AS-05B trong tay, không để tờ giấy “bay qua thời gian”: preflight chuyển món đó về anchor B và preserve memory reference trước khi đổi lớp R.

#### Trước và sau T08 — ba kịch bản vật phẩm

| Tình trạng AS-05B trước bàn giao | Lúc AN01 bắt đầu transition | Khi player mở ba lô ở R |
| --- | --- | --- |
| Thẻ còn ở giá kẹp giường 4 | Không cần chuyển owner, chỉ đóng quyền tương tác lớp B | Có bản nhớ nếu đã xem; không có physical thẻ |
| Thẻ đã được cất ở túi | Thu quyền sử dụng item B, trả vật về fixture B khi transaction commit | Ngăn giấy không còn thẻ như món cầm được; nguồn đã đọc vẫn tra được |
| Thẻ đang trong tay hoặc đặt ở bàn so | Khóa interact ngắn, reconcile về đúng anchor B; rollback nếu swap thất bại | Không cho Equip/Use giấy B ở R; báo bằng trạng thái nguồn, không tự xóa ký ức |

Cả ba đường phải giữ nguyên **OBS101A/B đã Examined, EX01 nếu có, first/current/history F101**. Nếu swap thất bại, mọi thứ trở lại B đúng trạng thái trước đó (kể cả thẻ trên bàn); không lấy mất giấy rồi hiển thị lỗi. Đường đi R cho CH02 không đổi theo ký A/B/C và không thừa một vách deniedBay từ lớp B.

#### Điểm kết nhịp trong quầy

Sau khi B→R, giữ vài giây cho âm mưa lấp đầy không gian, tiếng bút không quay lại. Cùng vị trí camera ở quầy, người chơi có thể nhìn từ nơi Lan từng ngồi ra hành lang trống. Đây là lúc game cho phép câu hỏi “mình đã gặp ai?” tồn tại mà không trả lời. Nếu phát DREAD_B06_CLOCK_TICK, chỉ dùng **một nhịp tích tắc khô** sau khoảng yên; không biến nó thành lời giải thích thời gian và không chồng cùng PA04.

**Gợi hướng B07:** khi player quay từ quầy ra cửa sảnh, hướng cổng phụ hiện rõ. Sau một cuộc gặp không thể giải thích, việc muốn thoát là phản ứng hợp lý, không phải nhiệm vụ HUD vô duyên. Pocket Note có thể chỉ ghi “Thử ra ngoài” rồi người chơi tự quyết định chân mình đi đâu.

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

#### Dựng seam bằng một hành động người chơi có thật

**Trước khi thử:** sân R vẫn là sân B01. Xe, bảng phá dỡ, cột đèn, rãnh nước và cổng cũ phải giữ tương quan không gian. Nếu player nhìn lại xe, món trong túi vẫn còn và xe vẫn là xe; **không** tráo thành một con đường khác ngay khi vừa ra cửa sảnh. Giữ người chơi có quyền đi qua một quãng ngắn để tự quyết định “mình muốn ra ngoài”.

**Chạm cổng:** player đưa tay về tay nắm; nút E hoặc giữ-kéo đều làm cánh cổng chuyển động có thể nhận biết. Vượt ngưỡng authored mới kích CH01-T09; không để chỉ click từ xa qua collider phát seam. Nếu người chơi buông khi cổng chưa tới ngưỡng, cánh cổng có thể trở về chốt mà không consume loop; bấm lại vẫn được.

**Cú từ chối:** âm mưa hạ ngắn, tiếng kim loại đập có hướng **từ bên ngoài**. Khung nhìn dịch chuyển theo một transition ngắn/continuity seam chứ không làm toàn cảnh xoay 180 độ khiến người dễ say chuyển động. Player thấy mình vẫn ở trong khuôn viên, đường về quầy nằm ở phía có thể nhận ra. Nếu dùng crossfade mềm khi reducedMotion, trạng thái logic không khác; lời CH01-D14 chỉ một lần theo guard.

**Lần thử thứ hai:** cánh cổng phản hồi đã không thể thoát. Không cho player lặp cùng seam vô hạn; không phát lại cú gõ giật. Nếu người chơi nhấn dùng chìa phụ đang equip, Game không tịch thu item hay mở đường tắt; có thể trả cảm giác chìa không thay được tình thế này. Chìa khu C không là phép mở khóa cổng hoặc làm phát thoại nơi cấm.

**Sự kiện ở biên thế giới:** hiện tượng cổng không phải vật thể thuộc ba lô, cũng không cấp observation mới để giải F101 đã ký. Nó đặt ra một vấn đề khác: bệnh viện đang giữ Minh ở lại, nhưng cơ chế đó chưa được giải thích trong CH01. Không cho NPC/ma xuất hiện để diễn giải cho player.

#### Âm thanh và trạng thái an toàn

- Nếu vừa thấy P-SC05 quá gần, hạ cú đập cổng xuống một nhịp nhỏ và dành chỗ cho cảm giác bị trả về; **nỗi sợ do không thể thoát** phải còn ngay cả khi không có âm lớn.
- Với mono/caption, người chơi biết tiếng đập phát từ phía cổng; không cần khả năng định vị stereo tốt mới hiểu sự việc.
- Khi giảm camera motion, safe pad được chọn trước khi thay trình bày. Không đẩy người chơi vào khe cổng, không rơi khỏi sân, không đặt họ quay vào bức tường tối.
- Khi lưu sau CH01-T09, seamConsumed không bị reset; load tại sân có lối về AN01. Khi lưu trước ngưỡng kéo, cổng còn có thể thử lại một lần theo luật.
- Không che đường về quầy bằng bóng tối tuyệt đối. Đèn bàn trong sảnh có thể là mục tiêu nhìn thấy, nhưng các đường mép sân/bậc cửa vẫn cần độ tương phản đủ dùng.

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

#### Sự kiện trong ba lô được xây từ trí nhớ của chính người chơi

**Nguồn của cảm giác lạ:** hồi B04, player có thể đã chạm thẻ AS-05B bằng tay, lật, thấy chỗ chữ ký và cất vào một ngăn cụ thể. Ở B08, họ quay lại **đúng ngăn ấy**, một thao tác quen thuộc bỗng không nhận được kết quả quen thuộc. Kinh dị đến từ quan hệ giữa vật thật lúc trước và khoảng trống lúc này, không phải vì game phát một ảnh ma được đặt tùy ý trong túi.

**Điều kiện để xuất hiện P-BAG08_ECHO:**
1. T08 đã commit về R, và giấy gốc B đã reconcile thành nguồn tham khảo phi vật chất.
2. Player **đã thật sự cầm/soi hoặc cất** AS-05B trong B; chỉ thấy giá kẹp từ xa hoặc xem cinematic B03 không đủ.
3. Player chủ động mở ngăn giấy hoặc mở lại memory reference, không bị game tự bật túi ở giữa đường.
4. Sự kiện chưa từng chạy; nếu trước đó đã hiện, lần tiếp theo chỉ còn danh sách reference bình thường.

**Choreography rất ngắn:** khi ngăn giấy mở, trên vải túi có một mép giấy/ô trống chữ ký trông giống ký ức thị giác hơn vật hiện hữu. Khi Minh tới gần hoặc chọn xem, nét đó chậm mất đi, chỉ còn mô tả nguồn đã kiểm **LOC03 / B / 17-11-2005**. Không phát tiếng scream, không rung camera mạnh, không cho tờ giấy bay ra khỏi túi. Nếu player chọn “Lấy ra”, hệ thống phải chuyển sang **xem lại nội dung đã ghi nhớ**, không đặt mesh giấy gốc trên tay.

**Nếu chưa xem thẻ B:** túi không invent một kỷ niệm để hù. Player có thể chỉ thấy BOARD, E10a và một dòng ghi chú tùy những gì đã tự xem. B08 vẫn phải có tác động nhờ hành lang vắng, cổng không thoát được và việc sổ trực chuẩn bị mở CH02.

**Nếu đã chọn A/B/C:** hiệu ứng echo không khẳng định nhánh nào “đúng”. A có thể mang nỗi bất an vì một dấu đã bị vách che, B còn mắc ở sự lửng lơ, C chưa biết vì sao giấy và dấu sàn chống nhau. Nếu có một dòng Pocket Note đi cùng, nó phản ánh **trạng thái tri thức của player** chứ không diễn đạt “Minh đã quyết định không bao giờ ký nữa”, vì người chơi A có thể đã ký xác nhận.

#### Chuỗi tâm lý kết chương theo mức độ hiểu của người chơi

| Người chơi đã làm gì? | Minh có thể nhớ về điều gì? | Trò chơi tuyệt đối không được khẳng định |
| --- | --- | --- |
| Chỉ ký A/B, không xem hốc 4 | Đã ký một phiếu và chứng kiến căn phòng thay đổi kỳ lạ; Lan biến mất khi về R | “Bạn đã phát hiện giường 4 là có bệnh nhân” |
| Xem dấu sàn nhưng bỏ thẻ | Có một vị trí từng sử dụng mà chưa có giấy đối chiếu | “Có ai cố tình giả số 0” |
| So cả OBS101A/B và ký C | Dấu sử dụng khớp với thẻ chưa bàn giao, nên số 0 không đủ căn cứ | “Một nạn nhân vẫn còn trong bệnh viện” |
| Hoàn tất EX01 | Nhiều nguồn đều nhắc tới 03:17, nhưng chưa rõ lý do | “03:17 là giờ chết/cháy” |
| Đã soi thẻ, giờ nhìn echo | Nhớ đã cầm giấy ở ca B mà hiện tại không có bản vật lý | “Minh mang được chứng cứ năm 2005 về 2012” |

**Câu hỏi cuối:** người chơi có thể chọn nhìn về Phòng 101 R từ quầy, hoặc mở E10a một lần nữa và thấy mẩu tin cũ. Cả hai con đường cùng dẫn tới ý nghĩ cần kiểm hiện trường vật lý năm 2012; không cần một màn hình “Giải thích Chương 1”. Chỉ có thao tác giở sổ AN01 mới commit T10, cho phép người chơi tự lựa chọn khoảnh khắc chuyển tiếp.

**Dư vị về Minh:** một ký ức liên quan đến **cánh cửa từng không mở** có thể hiện trong một dòng nội tâm ngắn đã được duyệt, nhưng không vẽ cửa Khu C, không phát tiếng cầu cứu CH07, không cho người chơi trông thấy nhân dạng thuộc chương sau. Chi tiết này phải gợi day dứt, không chiếm chỗ của câu hỏi về 101 R.

#### Kết chương

6. Người chơi đến **sổ trực vật lý tại AN01**, giở trang để xác nhận tiếp tục kiểm hiện trường. **CH01-T10 / CP04** được commit và thẻ **CHƯƠNG 02 — PHÒNG BỆNH TRỐNG** xuất hiện. Không cần mở menu suy luận nhiều tab hoặc chọn một câu kết đạo đức.
7. Camera và trạng thái chuyển tiếp để người chơi bước vào **Phòng 101 của hiện thực R (2012)**. Chương 2 còn các dấu **LIST + SOCKETS + WHEELS → PZ01/E12 → E11 → G02**, được điều tra độc lập; không tự cấp manh mối hoặc đồ vật CH02 ở đoạn kết CH01.

#### Hình ảnh kết

Một tiếng giấy lật khô tại quầy trống. Không có Lan, không có jumpscare cuối chương. Cánh cửa Phòng 101 ở hướng đi tiếp; người chơi **muốn tự quay lại nhìn hiện trường thật** vì những gì đã tận mắt thấy ở B vẫn không thể khớp với một con số trên giấy.

---

## III. NHỊP KINH DỊ VÀ CHỖ DỪNG CHO NGƯỜI CHƠI

### Nhịp tổng thể, không đánh đồng sự kiện với jumpscare

| Giai đoạn | Âm nền chủ đạo | Điểm gây bất an | Khoảng nghỉ cần có |
| --- | --- | --- | --- |
| B01–B02 | Mưa sân R, tiếng gió, phản xạ kính | Tự mở cổng rồi thấy có người ở nơi bỏ hoang | Giữa tiếng gõ biển và kính có đủ không gian chỉ nghe chân Minh |
| B03 | Quạt, đèn, giấy bút và giọng Lan ở B | Sự ấm áp trái với mốc 2012/2005 | Cảnh hội thoại không kèm âm hù; sau cinematic ít nhất một nhịp an toàn |
| B04 | Gió rèm, ray kéo, xe đẩy xa, tiếng giấy hai mặt | Dấu vật lý không khớp hồ sơ | Không đánh lạc hướng khi player đang đọc chi tiết nguồn |
| B05 | Quạt hạ, một tiếng móc rèm có hướng, im lặng sau mực ký | Bóng thoáng qua; căn phòng phản ứng với chữ ký | P-SC05 và hậu quả F101 không phát chồng nhau |
| B06 | Thiếu tiếng bút, nền quầy trống rồi mưa R | Mất sự hiện diện của Lan | Giữ khoảng trống nhìn ghế và sổ trước cue đồng hồ |
| B07 | Mưa/sắt/cổng; âm thiếu hụt ngắn | Cổng từ chối cho đi | Nếu scare B05 gần quá, giảm âm gõ và ưu tiên seam |
| B08 | Bước chân và sân có âm xa | Hình giấy thoáng trong túi hoặc chỉ còn ký ức | Không thêm đỉnh hù thứ ba ở ngay thẻ chương |

**Quản lý scare:** các cue do môi trường điều phối chỉ chạy một lần theo state và không lấy “người chơi vừa mở modal” làm lý do phát bù một cú hù. Nếu đã có DREAD_SHIFT_TUBE_BLACKOUT, P-SC05 không được cộng thêm một blackout nữa. Một số micro-dread có thể bị bỏ lỡ hoàn toàn, còn hậu quả ký F101 và seam cổng là các sự kiện cốt lõi nhìn/nghe được qua phản hồi an toàn.

**Ba lớp âm được giữ liên tục:** R trước cuộc gặp là mưa và không gian đã đóng; B có hum đèn/quạt/giấy của ca trực; R sau T08 mang lại mưa nhưng thiếu hoàn toàn tiếng bút/giọng Lan. Đối lập này phải rõ cả khi người chơi dùng mono hoặc nghe caption, không phụ thuộc đúng loại tai nghe.

**Nguồn âm vật lý:** tiếng khóa ở tay, tiếng ray ở rèm, tiếng bánh theo dấu sàn, tiếng bút ở quầy và tiếng gõ ngoài cổng đều có hướng/độ vọng phù hợp. Không dùng âm scream toàn màn hình nếu không có đối tượng mà người chơi có thể liên hệ. Khi không có âm, visual vẫn truyền sự kiện bắt buộc (vách mới, cổng seam, quầy trống).

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

### Bản đồ commit, checkpoint và recovery trong tám beat

| Beat | Đầu vào bắt buộc | Sự kiện commit | Dữ liệu phải còn khi load | Đường phục hồi nếu bị gián đoạn |
| --- | --- | --- | --- | --- |
| B01 | Người chơi tự kiểm E10a/tin nhắn và dùng cổng | E10a.SMSRead; CH01-T02 | E10a đúng một evidence, hai chìa không nhân đôi, cổng đã mở | Nhặt/đọc lại đúng trạng thái, không reset SMS hoặc cấp chìa mới |
| B02 | Vượt sân vào sảnh | Setup CH01-D02 / dread once nếu đủ guard | Thứ tự cue đã xem, vị trí/player inventory | Người không nhìn bóng vẫn tiếp cận AN01 |
| B03 | Giở AN01, T03/T04 xong; trò chuyện rồi tự lấy BOARD | AP02, Lan noticed/dialogue, CH01-T05 | Lan once/VO transcript, BOARD thuộc về player nếu đã nhặt | Skip/load trả camera; chưa T05 thì BOARD vẫn ở quầy |
| B04 | Đã có BOARD/T05, vào 101 | CH01-T06/AP04, observation stages, optional EX01 | Vị trí rèm/ngăn, thẻ nằm đâu, những mặt đã soi | Bất kỳ nguồn nào còn chưa xem đều tìm lại được |
| B05 | Player tới F101 và chọn A/B/C | CH01-T07, F101 first/current/revision, G01 | Hình học B theo chữ ký cuối, lập luận và marks/link, once scare | Save fail không ký nửa chừng; được quay lại sửa trước T08 |
| B06 | G01 có, player giở AN01 | CH01-T08 / B→R | Giấy B không physical trong R, memory reference và decisions còn | Swap fail rollback lớp/item/camera, không mất hồ sơ |
| B07 | Sau T08, player thử cổng SEAM | CH01-T09 once | Seam đã tiêu thụ, vị trí hợp lệ, đường về | Load không lặp vô hạn hoặc kẹt collider |
| B08 | Sau T09, player giở AN01 | CH01-T10 / CP04 | CH02-R độc lập, evidence CH01 đúng 1, echo đã xem/không xem | Không mở túi vẫn chuyển CH02; không tự cho E11/E12 |

**Thứ tự kiểm nghiệm:** thử tuyến A và B trước để chắc có đường ngắn, sau đó thử tuyến C đầy đủ, C thiếu từng nguồn/mark/link/claim, rồi thử quay lại sửa A→C→B. Trộn tiếp các trường hợp mở ba lô lúc đang cầm giấy B, skip cinematic, save khi đổi lớp và load tại cổng. Không coi một đường walkthrough “đi thẳng” là đủ nghiệm thu khi các nhánh khác có thể làm mất vật.

**Đường sau chương:** R của CH02 phải vẫn cho quan sát LIST, SOCKETS, WHEELS độc lập, mở PZ01/E12 và thu E11 theo gate CH02. Việc có/không có P-BAG08_ECHO và lựa chọn A/B/C không được đưa những vật này vào túi sớm. C01, E08 và các thoại/hồi tưởng khóa về sau phải giữ nguyên điều kiện nêu trong Core.

Bản walkthrough chỉ được coi là vận hành đúng khi các đường chơi và trạng thái sau được kiểm trong Unity:

1. **Vật phẩm 3D:** lấy E10a và chìa không nhân bản; cầm/soi/lật/cất/lấy ra/đặt AS-05B không làm mất nguồn, với cả chuột và phím thay thế.
2. **Lan:** Scripted Event nhận biết diễn ra sau CH01-T03, cinematic chỉ bắt đầu khi Minh chủ động trò chuyện; skip/save/load không nhân thoại, không tự nhặt BOARD hoặc auto-credit quan sát đồng hồ.
3. **F101 — điều tra bằng tay và lập luận:** A/B ký được ngay, không yêu cầu nguồn. Với C, người chơi tự soi `OBS101A/B`, khoanh `F101_ZERO` và `OBS101B_SIGN`, **tự chọn nguồn nối `OBS101B_SIGN → OBS101A_TRACES`** ở bảng kẹp hoặc ngay tại dấu sàn, rồi chọn claim giới hạn. Kẹp đúng giấy không auto-pass; link sai, mark sai, nguồn chưa Examined hoặc claim quá mức phải nhận phản hồi khác nhau. Bản nháp/Undo/source reference phục hồi sau save/load, `annotationVersion` mới không được lợi dụng đường legacy version 0; mọi nhánh vẫn mở G01 và cho sửa trước T08.
4. **R/B và ba lô:** giấy B đang cầm, cất hoặc đặt trên bàn đều không biến thành giấy vật lý R sau T08; ghi chú nguồn vẫn còn; echo ở B08 chỉ là hiển thị có điều kiện, không thể dùng/export.
5. **Cốt truyện và dữ liệu:** CH01 evidence vẫn chỉ có E10a; không làm lộ C01, E11/E12 hoặc thoại/hồi tưởng bị khóa; CH02-R giữ LIST/SOCKETS/WHEELS và các gate độc lập.
6. **Điều hướng, trợ năng và sợ:** vòng lặp cổng chỉ một lần, không kẹt collider/không mất đường về AN01; mute, caption, mono, large print, reduced motion, giảm jumpscare đều dùng được; các scare không cướp quyền kiểm tra vật chứng.
7. **Playtest:** đo hành động người chơi tự thực hiện, hiểu biết đúng về F101, việc dùng đồ có mục đích, cảm giác sau khi Lan biến mất và động lực bước vào Phòng 101 ở hiện thực R. Số phút 20,5–30 chỉ là mục tiêu thiết kế; không coi là kết quả người chơi thật khi chưa đo.

**Kết thúc Chương 1:** Minh không biết đầy đủ sự thật, không có bằng chứng kết tội bất kỳ ai và chưa hiểu bản chất sự hiện diện của Lan. Nhưng anh đã tự tìm ra một điểm mà giấy tờ và hiện trường không khớp nhau, đã đặt chữ ký của mình vào cuộc, và giờ muốn tự kiểm tra nơi đó trong hiện thực năm 2012.