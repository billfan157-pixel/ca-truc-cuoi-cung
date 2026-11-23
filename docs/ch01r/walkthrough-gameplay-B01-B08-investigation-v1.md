# CH01 — NGƯỜI TRỞ VỀ: Walkthrough gameplay B01–B08 (Investigation-First + Physical Interaction Proposal v1.4)

> **TRẠNG THÁI: ĐỀ XUẤT THIẾT KẾ ĐỂ REVIEW, CHƯA TRIỂN KHAI / CHƯA PHÊ DUYỆT CANON.**
> Ngày: 23/11/2026. Phạm vi: CH01 B01–B08 của *Ca trực cuối cùng*, trên CH01-R hiện hành. **v1.4: đánh giá và tiếp nhận có chọn lọc bản Backpack & Exploration-First v2.0 do người dùng cung cấp; kế thừa v1.3, chưa triển khai Unity/chưa phê duyệt canon.**
> Tài liệu này là một walkthrough **được chỉ đạo bởi hành động của người chơi**, không phải chuỗi cutscene thuần tuyến tính. **Ngoại lệ có chủ đích tại B03:** Lan nhận biết Minh qua Scripted Event trong thế giới, rồi người chơi chủ động kích hoạt một Cinematic Dialogue Scene ngắn, liền mạch với FPS; sau đó trở lại khám phá tự do. Cách kể mới chỉ là phương án dàn dựng; thoại chính thức vẫn do Core 07/Dialogue sở hữu. Các trigger mới gắn tiền tố **P-SC / P-INT / P-NOTE** chỉ tồn tại trong đề xuất, KHÔNG phải event ID đã đăng ký.
> Mục đích: tăng chiều sâu điều tra và nỗi sợ trong khi KHÔNG thay đổi canon, các gate và dữ liệu bằng chứng hiện hành trước khi được duyệt.


> **QUY TẮC ƯU TIÊN v1.4:** các chỗ bên dưới còn mô tả “Sổ trực/Sổ điều tra” như menu/tabs là **baseline v1.2 để đối chiếu**, không còn là UX mặc định. **Tab/J mở BA LÔ ĐIỀU TRA** (đồ vật, giấy tờ, dụng cụ, vật chứng, ghi chú phụ); **AN01/sổ trực ở quầy** vẫn là **đối tượng vật lý** làm mốc R→B, bàn giao B→R, chuyển CH02. So tài liệu từ túi diễn ra trên bàn vật lý/khung inspector 3D. Cụ thể xem [Backpack & Physical Investigation Proposal](../gameplay/backpack-physical-investigation-proposal.md). Phần bổ sung v1.3–v1.4 từng beat và §9–§10 **thay thế hướng dẫn tương tác/UI cũ khi xung đột**, nhưng không thay canon/gate đã chốt.

> **Chốt UX v1.4:** ba lô có **3 ngăn hiển thị** (Dụng cụ & Đồ vật; Giấy tờ & Vật chứng; Pocket Note), nhưng backend vẫn phân biệt `BackpackItem`, `ObservationRecord`, `evidence`, provenance R/B/M. B03 giữ **Scripted Event → người chơi chủ động nói chuyện → Cinematic Dialogue Scene → FPS**. F101 C vẫn cần đủ phép đánh dấu/link/claim, không auto-pass chỉ bằng kẹp một tờ giấy. B08 chỉ thử `P-BAG08_ECHO` là *presentation-only*; không mang giấy B thật sang R. §10 ghi đối chiếu nguồn.

## 0. Định hướng trải nghiệm

**Câu hỏi mở đầu:** Vì sao Minh trở lại một nơi mình từng từ chối mở cửa — và vì sao bệnh viện dường như chỉ công nhận những gì một tờ phiếu đã xác nhận?

**Câu hỏi người chơi phải tự trả lời khi kết thúc CH01:** “Tôi thực sự biết điều gì về Phòng 101, và tôi sẽ ký vào điều gì khi những dữ kiện chưa khớp nhau?”

**Lời hứa của CH01:** không giải mã toàn bộ thảm kịch. Người chơi chỉ có thể xác định (1) vụ cháy 18/11/2005 và 13 người chết thuộc tri thức công khai; (2) Lan đã mất nhưng Minh gặp một hình ảnh/cảnh giao ca không thể giải thích giản đơn; (3) phiếu F101 báo 0 có vấn đề về độ tin cậy nếu đối chiếu với dấu sử dụng giường 4 và thẻ bàn giao thiếu chữ ký; (4) việc xác nhận, để ngỏ hoặc ghi nhận bất nhất tạo những hậu quả cảm giác khác nhau ở lớp B. Không có căn cứ để kết luận bệnh nhân còn sống, ai làm giả giấy tờ, một cái chết cụ thể đã xảy ra tại 03:17 hoặc điều gì thực sự diễn ra ở cửa khu C.

**Vòng chơi:** quan sát một dấu hiệu đời thường → đặt câu hỏi → tìm nguồn kiểm chứng độc lập → thao tác vật lý có mục đích → phân biệt dữ kiện và suy đoán → ký/để ngỏ → chứng kiến thế giới đáp lại → trở lại để đối chiếu thêm. Nỗi sợ nằm trong khoảng cách giữa **điều mắt thấy** và **điều văn bản cho phép ghi nhận**.

### 0.1. Ranh giới bất biến (must keep)

- CH01 chỉ thêm vào kho Evidence chính thức **E10a**, không biến OBS, FS01 hoặc ghi chú thành vật chứng có thể xuất. E10b chỉ tới sau; E11/E12 và định danh C01 thuộc CH02.
- Minh biết ký ức mình đã từ chối mở cửa, nhưng **CH07** mới tái hiện đầy đủ 03:09–03:12. CH01 không đưa người chơi đến cửa khu C, không tái hiện M, không trình bày Kẻ Giữ Cửa với tư cách sự thật đã xác nhận.
- 03:17 ở CH01 là **mốc đồng hồ / vết hằn và manh mối mất liên lạc**; không tự gọi là giờ tử vong, giờ phóng hỏa hay giờ chốt số. Lan đã mất trong mốc 2012; cảnh ở B dùng ngữ cảnh 17/11/2005 theo thoại hiện hành, không xác nhận nguồn gốc siêu nhiên của Lan.
- F101: Affirmed hợp lệ không cần chứng cứ và đặt Distortion(LOC03)=1; Unverified hợp lệ, D=0, không phạt; Refuted phải có OBS101A + OBS101B, đúng vùng đánh dấu, liên kết và kết luận “số 0 chưa đủ đáng tin”, D=0. **Cả ba đều mở G01**, cho phép sửa phiếu và giữ first/current/revisions. Không chấm điểm “đạo đức” trên HUD.
- Lớp **R (2012)** là hiện trường vật lý độc lập với F101, **B** là biến đổi mang tính hồ sơ, **M** chưa được mở. Không đưa vật chứng vật lý mới vào B rồi xuất; không chỉnh geometry R của CH02 theo A/B/C.
- CH01 vẫn phi sát thương: không Hunter, không sanity meter, không đuổi giết, không khóa đọc/safe volume. Sợ không được thay cho khả năng suy luận.
- Giữ LOC01 cổng/sân; LOC02 quầy; HALL và STAFF vòng ngắn; LOC03 Phòng 101; cửa LOC04 khóa đúng nhịp CH01; seam cổng một lần; AN01, AN02, AP02, AP04; CH01-T02–T10 và các voice line hiện có.
- Không phát CH01-D16b hay CH04-D06 tại CH01. Bất cứ thoại mới nào phải qua Dialogue, guard và V22-Q03.

### 0.2. Nhịp mục tiêu và phạm vi

- **Tuyến chính v1.3:** mục tiêu **20,5–30 phút** cho người chơi mới khi cầm/nhặt/cất/lấy ra/soi 3D và sử dụng đồ thật; cộng đúng bảng §4 mới. Đây là giả thuyết pacing chưa playtest, không đặt giới hạn để ép người chơi bỏ khám phá. v1.2 từng đặt 18–24 phút cho route thiên về UI, hiện **không còn là target chính**. 
- **Tuyến khám phá sâu:** thêm khoảng 6–10 phút tại STAFF, hồ sơ phụ, EX01 và việc đặt vật lên bàn đối chiếu; tổng thiết kế **26,5–40 phút**. Tất cả khám phá sâu vẫn tùy chọn, không được ép người chơi ký A/B/C theo một route vật phẩm duy nhất.
- **Nấc cảm xúc:** B01 tò mò có lỗi → B02 có người đang chờ → B03 thời gian trở nên không đáng tin → B04 hồ sơ và hiện trường bất đồng → B05 trách nhiệm của chữ ký → B06 hậu quả và vắng mặt → B07 không thể bỏ đi → B08 quyết tâm kiểm lại thế giới thật.
- **Scare mix dự kiến:** 2 điểm authored có trọng lượng (B05/B07), một silhouette có thể bỏ lỡ nhưng không làm mất thông tin cốt truyện, và 4–6 micro-dread có ngữ cảnh. Không dùng cooldown 45–60 giây cứng cho *mọi* cue nhỏ: hệ nền hiện có global cooldown seed khoảng 16 giây; riêng scare lớn cần hồi nhịp riêng, theo hành động thay vì chỉ timer. Cấm cú giật ở inspector, sổ, thoại, cinematic, menu, save/load hoặc đổi lớp; xê dịch sau tờ giấy chỉ xảy ra khi trở lại 3D an toàn. Không ép xoay camera. Duyệt ngưỡng âm, chuyển động, caption với TASK028.

## 1. Đồ thị câu hỏi và tri thức

| Mã câu hỏi | Người chơi thấy gì | Chứng cứ / hành động dùng để kiểm | Kết luận tối đa được phép | Payoff |
| --- | --- | --- | --- | --- |
| Q1: Ai gọi Minh trở lại? | E10a, SMS Bảy hoãn gặp, bảng tháo dỡ | Nhặt thư, xem hai mặt, đọc bối cảnh 2012, giữ nhật ký nguồn | Bảy từng hẹn; sau đó báo hoãn; lý do thực sự chưa rõ | B01 → B08; E10b ở CH06 |
| Q2: Tại sao có một người trong bệnh viện vắng? | Bóng qua kính, Lan ở quầy, Minh nhớ Lan đã mất | Tiếp cận, nghe thoại CH01-D03/D04/D05/D07 và nhánh tùy chọn, soi lịch/đồng hồ | Có bất nhất trải nghiệm và mốc thời gian; không chốt là ma | B02/B03 → B06 |
| Q3: Tại sao Phòng 101 chỉ có số 0? | Bảng F101, dấu giường 4 và thẻ AS-05B | Khảo sát OBS101A + OBS101B ở B, so với OBS101C/D, hướng dẫn STAFF; đánh dấu hai chi tiết và nối nguồn | “Số 0 chưa đủ căn cứ xác nhận”; không kết luận ai còn trong phòng hay ai cố ý che giấu | B04 → B05 → CH02 |
| Q4: “03:17” là mốc gì? | Đồng hồ dừng trong B; mặt sau AS-05B; bối cảnh sổ trực | Nghiêng giấy/Chữ lớn, đọc nguồn ca, tự khớp mốc trong CH01-EX01 | Hai dấu thời gian trùng nhau; ý nghĩa sau cùng chưa rõ | B03/B04 → CH01-D19 → CH06/07 |
| Q5: Vì sao quyết định khiến không gian thay đổi? | Vách/rèm/vết bánh/loa thay theo F101 | Chọn A/B/C, nhìn hậu quả, nếu muốn sửa, quay lại đối chiếu | Thế giới B phản ứng với việc ký; **không** suy ra hiện trường R đã đổi | B05 → B06 → CH02 |
| Q6: Minh có thể đơn giản bỏ đi không? | Seam cổng, sự lặp lại lối trở vào | Thử rời đi sau khi bàn giao; quay lại sổ | Minh chưa thoát khỏi cuộc đối diện; chưa chứng minh nguyên nhân siêu nhiên | B07 → B08 |

**Nguyên tắc thông tin:** Sổ trực ghi tách bốn trường: “Đã thấy” (khách quan), “Nguồn” (vật/thời/lớp), “Có thể suy ra” (có giới hạn), “Còn chưa biết”. Câu hồi tưởng của Minh là **lời kể có góc nhìn**, không có cùng cấp độ chứng cứ với hồ sơ/hình học. Chỉ ghi điều người chơi đã trực tiếp kiểm tra; không auto-solve. Câu hỏi Q4 là tuyến thưởng tùy chọn, Q3 vẫn giải được khi bỏ qua Q4.

---

# 2. WALKTHROUGH THEO THAO TÁC NGƯỜI CHƠI

## B01 — Bức thư không còn lời hẹn (LOC01, R, 1,5–2 phút tuyến chính)

**Mục tiêu kịch:** Đưa người chơi từ “một ca trông coi/đêm quay lại” sang hành động có lý do cá nhân: kiểm tra vì sao Bảy mời rồi lại hoãn gặp, trong khi Minh tự quyết định đến An Sinh. Mở Q1, gieo Q5 dưới hình thức đồ vật bị quản lý bằng giấy.

**Entry / nhịp camera:** Mưa trên đường sau bệnh viện; xe máy Minh dựng dưới đèn. Không ép góc nhìn cinematic. Hai landmark rõ: cổng sắt phụ và phong bì cạnh xe. Cửa chính không là đường đi thay thế. Thông báo tháo dỡ thuộc **R, 2012**.

**Walkthrough:**
1. Người chơi được tự do đi 4–6 m quanh xe, quan sát vệt lốp mới và nước mưa còn đọng (CH01_MOTORBIKE). Đây **không phải manh mối án mạng** mà xác nhận Minh vừa đến, đặt câu hỏi chủ động “vì sao không quay xe?”; bấm E để soi, có mô tả ngắn thay cho objective rỗng.
2. Nhặt **E10a** cạnh xe, soi mặt ngoài/đọc thư và SMS báo hoãn (giữ văn bản chính thức CH01-D15, không thay lời). Người chơi có thể xem lại nguồn bất cứ lúc nào. Nhật ký mục “Người gửi / thời điểm hẹn / tin hoãn” ghi trạng thái đã đọc; **chỉ E10a** được đăng ký trong evidence.
3. Rẽ 1–3 m nhìn thông báo tháo dỡ CH01_DEMOLITION và biển men CH01_GATE_PLAQUE: bệnh viện đã đóng, cảnh ở hiện tại không thể là một ca bệnh bình thường. Nếu đọc tin báo trong E10a, người chơi biết công khai **13 người chết trong vụ cháy 18/11/2005**, nhưng không biết diễn biến bên trong.
4. Người chơi thử cổng trước khi đọc SMS: tay nắm không mở, lời nhắc “Chìa khóa trong bì thư”. Sau khi đọc đúng phần E10a, mở GATE-CONG — **CH01-T02**. Hành động mở cổng là một quyết định tiến vào chứ không là puzzle chìa khóa tùy tiện.
5. Tùy chọn: nhìn lại xe/cổng; không thêm “có ai đụng vào xe” giả. Câu hỏi trong nhật ký: “Bảy hẹn rồi hoãn. Vậy điều gì khiến mình vẫn phải vào?” Không tự điền đáp án.

**Đạo diễn sợ:** Nhịp quãng mưa tắt một phần khi đọc tin báo; khi hạ thư, tiếng mưa trở lại hơi lệch phương nhưng không phát tiếng người thì thầm. Tại cửa, tấm biển gió giật **chỉ sau khi người chơi đã đọc bảng tháo dỡ** (âm kim loại có hướng, caption); đây là *warning cue*, không jumpscare lớn. Giữ 15–25 giây không scare sau E10a.

**Ý nghĩa tầng:** Người chơi tự mở cổng dù biết lời hẹn bị hoãn. Chủ đề: “Một lựa chọn cũng là một dạng chữ ký”. Về sau khi F101, hành động ký lặp lại hành động tự bước vào.

**Nén nhịp v1.2:** thư E10a, chìa và cổng nằm trên tuyến tự nhiên từ xe. Quan sát xe/biển tháo dỡ vẫn có ý nghĩa nhưng không bị biến thành thủ tục bắt buộc. Người chơi có thể mở cổng nhanh sau khi đọc SMS; hành động tự bước vào vẫn là một quyết định của Minh.

**Balo/3D v1.3 — không bỏ động cơ cốt truyện:** Người chơi **cầm phong bì E10a** ngay cạnh xe, xoay/lật xem thư và SMS như các thành phần của một prop tương tác; tự **cất phong bì vào ngăn Hồ sơ giấy**. Từ ba lô có thể rút lại bì thư để đọc, hoặc cầm chìa được ủy quyền (nếu item con được author) đưa tới cổng. Tuy nhiên `GATE-CONG` vẫn xét sự kiện đọc SMS/nhận chìa canon; không tự mở chỉ vì tồn tại item. Với save legacy `Investigation.New()` đã seed E10a, **không add E10a một lần nữa** khi nhặt vào ba lô. Những dòng tin và giấy đọc được phải có transcript và large print.

### Học từ Backpack v2.0 — B01 (v1.4)

**Động tác có giá trị:** mở mô hình phong bì `E10a` trong inspector 3D, **rút chìa cổng phụ** như item con, rồi cầm chìa ra tra vào ổ `GATE-CONG` bằng thao tác tay hoặc phím E/K tương đương. `Letter()` trong code hiện đã viết **có hai chìa**: chìa cổng phụ và **chìa khu C đã thay khóa**. Chiếc thứ hai có thể xem trong túi như một câu hỏi chưa giải, **không dùng để mở/tiếp cận khu C ngay CH01**. `T02` vẫn phải kiểm đã đọc E10a/SMS và vị trí; không tạo bản evidence E10a thứ hai (đã seed trong `Investigation.New()`).

Đề xuất bản nguồn về **vết hằn “Kiểm tra kỹ trước khi ký” trên bì thư** có tiềm năng foreshadowing, nhưng **chưa thêm vào lời/texture canon**: phải qua Core 02/07 & narrative review. Hiệu ứng cầm/đọc/cất chìa không quá nhiều màn menu, còn cổng vẫn là lựa chọn tự bước vào.

**Output:** E10a acquired + SMSRead; CH01-T02; Q1 mở, AP/rollback cổng hợp lệ. **Fallback:** đọc theo thứ tự ngược nhau không thất lạc E10a; bỏ qua thông báo không khóa cổng; mute vẫn có ánh sáng cổng/đồ vật.

## B02 — Người đứng sau ô kính (LOC01 sân → LOC02, R, 1–1,5 phút tuyến chính)

**Mục tiêu kịch:** Từ “đây là nơi bỏ hoang” sang “có ai đó đang chờ mình”. Nỗi sợ nằm ở mong muốn gặp người quen và đồng thời biết điều đó không thể đơn giản đúng.

**Walkthrough:**
1. Qua cổng là sân ướt, rãnh nước CH01_GUTTER, mái hiên, cửa kính quầy tiếp nhận xa phía trước. Đường đi tự nhiên được dẫn bằng vùng đèn ấm, **không dùng waypoint lơ lửng**. Người chơi có thể dừng nghe nước chảy hoặc nhìn sân trống.
2. Khi người chơi tới trục nhìn hợp lệ, bóng người sau kính xuất hiện 1–2 giây hoặc trở nên rõ hơn; đây nối vào **CH01-D02** và DREAD_B02_GLASS_SHADOW hiện hành. Nếu quay đi, bóng không lập tức dịch chuyển trước camera; nếu tiến sát mới thấy quầy còn người như chờ.
3. Một sự kiện nhỏ **P-SC02 “Ô kính chậm một nhịp” (đề xuất)**: nếu người chơi đứng quan sát đủ lâu, bóng bên kính quay đầu sau chuyển động thực của vật thể gây phản xạ; luôn giữ mơ hồ, không phô “ma mặt quỷ”. Có phiên bản reduceMotion: ánh sáng quầy biến đổi và tiếng giày xa thay cho animation.
4. Tiếp cận cửa/sảnh qua lối công cộng; lối khác không cho tắt qua LOC04. Nếu đứng ngoài >60 giây, ánh đèn quầy sáng hơn rất nhẹ; không spam “Hãy vào trong”.

**Mạch điều tra:** Q2 chưa thể giải; hành động chính là **chọn kiểm chứng người mình nhìn thấy**. Người chơi có thể nghi rằng đó chỉ là nhân viên bảo vệ, nhưng không bị HUD ép nhận diện.

**Scare medium có điều kiện:** Khi người chơi tự nhìn xuyên qua kính lần thứ hai, một chiếc bóng thấp thoáng qua **phía sau**, kèm hai bước chân (không bật ngay sát tai); khi xoay lại chỉ là khoảng sân trống. Giữ khả năng lờ nó đi; không ảnh hưởng gate. Thứ tự với DREAD_B02 phải được điều phối, **không phát hai cue trong cùng 45 giây**. Đây là phương án dàn cảnh, chưa có trong build.

**Nén nhịp v1.2:** bóng người qua kính đã đủ là hook; P-SC02 chỉ là phần thưởng phụ, không buộc đứng chờ hoặc soi kính hai lần để qua. Bước vào sảnh là tiếp cận ngay AN01. Bóng ngoài kính trong R **không** kích hoạt cinematic của B03.

**Balo/3D v1.3:** Giữ người chơi tập trung vào môi trường, không hướng dẫn mở ba lô khi bóng Lan xuất hiện. Không biến bóng người, chiếc kính hay âm thanh thành vật phẩm. Người chơi có thể chạm kính/quan sát từ góc khác, nhưng việc đó chỉ phát một cue và/hoặc ghi nhận **quan sát có nguồn**. Vào AN01 mới gặp Lan trong lớp B, **không được auto cinematic** từ B02.

### Học từ Backpack v2.0 — B02 (v1.4)

Giữ một hành động xác minh *bằng góc nhìn*: lia đèn pin qua kính quầy, tự dịch ngang để nhìn bóng người bị che bởi phản xạ; tiến vào AN01 không cần chờ một sự kiện phụ. Bản nguồn gọi điện toàn khu “đã cắt” và có hộp cầu dao niêm phong — **chưa coi là fact** khi Core 03/asset chưa xác minh. Bất thường ánh đèn và bóng người được giữ mơ hồ; không bấm flash gắt nếu reduceMotion.

**Output:** tới điểm AN01/LOC02; được tiếp cận sổ trực. **Handoff sang B03:** bóng người ngoài kính là setup nhìn-thấy; chỉ sau CH01-T03/R→B Lan mới nhận diện Minh bằng Scripted Event. Tuyệt đối không tự cắt sang cinematic ngay khi bóng người xuất hiện. **Fallback:** nếu người chơi sprint qua sân không nhìn kính, câu CH01-D02 và hiện diện Lan vẫn được setup tại quầy; không yêu cầu nhìn scare để tiến.

## B03 — Ca trực ở một ngày khác (LOC02, AN01, R→B, 3–4 phút tuyến chính; **Hybrid Cinematic Encounter**)

**Mục tiêu kịch:** Vừa dạy sổ trực như **công cụ phán đoán**, vừa tạo một cuộc gặp có trọng lượng cảm xúc. Scripted Event làm Lan có vẻ là một con người đang sống, *chủ động nhận ra Minh*; Cinematic Dialogue Scene bảo vệ nhịp diễn xuất, mâu thuẫn thời gian và cảm giác từng quen biết; gameplay tự do trả lại quyền điều tra cho người chơi. Q2 (Lan) và Q4 (03:17) cùng mở, nhưng không giải đáp hộ.

**Nguyên tắc đạo diễn đã chọn cho bản proposal:** **Scripted Event nhận biết → tiếp cận chủ động → cinematic đối thoại trong cùng scene → quay về FPS và tự lấy BOARD.** Không dùng màn hình đen, không tải scene mới, không auto-teleport người chơi, không khóa camera trước khi người chơi chấp nhận bắt chuyện. Vẫn giữ thứ tự **CH01-T03 (giở sổ trực, R→B) → encounter Lan → CH01-T05 (người chơi tự cầm BOARD)**; không thay gate hay ID canon.

### Walkthrough chi tiết theo thời điểm / hành vi người chơi

| Phase | Điều kiện và thao tác người chơi | Dàn dựng hình/âm/thái độ Lan | Điều tra / state |
| --- | --- | --- | --- |
| **0 · Quầy năm 2012** (tùy tốc độ) | Minh bước vào quầy R; tùy ý xem điện thoại, ghế chờ, lịch, DRAWER-DESK nếu có thể tương tác. **Tự giở sổ tại AN01.** | Ánh đèn bàn và sổ trực dẫn mắt, không HUD cưỡng ép. CH01-T03 kích R→B ngay tại AN01, giữ vị trí/hướng nhìn/landmark và safe pad; sau transition mới khởi động dàn dựng Lan. | Người chơi xác nhận đang vào một *ca trực*; checkpoint AP02. Không đăng ký OBS tùy chọn chỉ vì camera lia qua prop. |
| **1 · Lan nhận ra Minh** (~0:00–0:12 sau T03, *scripted gameplay*) | Minh đi đến gần quầy, nhìn Lan hoặc tiếp cận từ bên cạnh. Có thể dừng, lùi lại, quay đi, xem lịch/đồng hồ. | Lan đang ghi sổ, tay bút khựng lại, ngẩng lên, nhận ra Minh và chào bằng giọng gần gũi. Dùng animation nhìn theo góc có giới hạn, micro-blink và cử chỉ đặt bút thật; không stare khóa cổ hoặc jump-face. **Không khóa điều khiển.** | Chỉ ghi `P-INT03_LAN_NOTICED` (ID **đề xuất**, once), không tự chốt đã hội thoại. Nếu Minh đứng xa, Lan tiếp tục ghi, không phát lại câu chào mỗi lần ra/vào trigger. |
| **2 · Minh chủ động bắt chuyện** (~0:12–0:18, *player-initiated*) | Đến vị trí an toàn trước quầy và nhấn E/K vào `LAN_TALK` hoặc ngữ cảnh trò chuyện tương đương; prompt rõ `Nói chuyện với Lan`. | Cử chỉ Lan dừng ở tư thế hợp logic, camera **blend 0,6–1,0 s** từ FPS sang góc medium từ phía quầy; ưu tiên thấy biểu cảm Lan và phản ứng Minh. Chỉ dùng over-the-shoulder có mô hình Minh nếu rig/continuity thực sự hỗ trợ, không tự lộ body/camera clipping. | Chỉ bắt đầu nếu `CH01-T03` đã commit, lớp B, không busy/swap/read modal, vị trí player hợp lệ. Có thể lùi khỏi trigger trước khi ấn mà không bị khóa story. |
| **3 · Cinematic Dialogue Scene** (~0:18–1:18, 60–90 s tổng khi không skip) | Trong thoại chính, người chơi nghe/đọc; cho phép **Skip** và tùy thiết kế lựa chọn đóng/mở phụ đề, nhưng không kéo người chơi ra khỏi vùng đã khóa vào giữa take. | Dùng góc medium/close-up nhẹ, ánh đèn bàn ấm sau vai Lan, reaction shots Minh, khoảng dừng trước câu thời gian. Lan nhắc về đêm **17/11/2005** như hiện tại; Minh biết cô đã mất. Có thể dùng **insert shot** vào đồng hồ B dừng **03:17** khi phù hợp `CH01-D04c`, nhưng tuyệt đối **không biến nó thành thông tin kết luận về tử vong**. Camera không phô danh tính C01, Khu C hay nguồn hồi tưởng CH04/CH07. Giữ nguyên VO và copy trong `07/Dialogue` (`CH01-D03/D04/D05/D07` + cue đã có theo đúng Context/guard). Không thêm lời mới chưa duyệt. | Thông tin nhận được: Lan đang ở một ngữ cảnh năm 2005 trái với thời điểm Minh quay về; người chơi **không được auto-credit** `OBS_CLOCK_0317`, `CH01-EX01` hoặc tài liệu chưa tự xem. Script/skip chỉ đánh dấu chính những câu thoại đã được cấp quyền phát theo hệ thống hiện hữu. |
| **4 · Trả lại camera, trả lại trách nhiệm** (~1:18–1:30) | Kết cảnh, blend về **đúng góc nhìn FPS khả dụng** ở quầy; Minh có thể tự lấy `BOARD` trên mặt quầy và rời đi. | Lan đặt/nhìn về BOARD hoặc dùng gesture hướng về Phòng 101, không “đưa qua” cho player bằng cutscene rồi âm thầm commit. Sau thoại, không phát jumpscare tức thì; khoảng lặng 10–15 s để người chơi hấp thụ và bắt đầu tìm hiểu. | **CH01-T05 chỉ commit khi người chơi tự tương tác BOARD**, đúng baseline. Nếu không lấy, có nhắc bằng bố cục/nhìn chứ không ép cutscene thứ hai. `LAN_TALK` về sau chỉ phát cue phản hồi bổ sung phù hợp, không replay đoạn cinematic. |
| **5 · Hậu đối thoại / điều tra tự do** (tùy chọn) | Minh đối chiếu lịch 2005, đồng hồ B, `OBS_RECEPTION_DRAWER`, điện thoại; có thể rời quầy rồi quay lại. | Lan tiếp tục một vòng công việc nhẹ, không thành NPC đuổi theo player; âm môi trường trở lại bình thường. Nếu người chơi nhìn lâu, Lan có thể ngừng viết một nhịp; không thêm bằng chứng ma quỷ. | Sổ trực tách **“điều Minh biết về Lan” / “điều đã chứng kiến trong B” / “điều chưa thể kết luận”**. Nhánh 03:17 tiếp tục tùy chọn; không tác động G01. |

### Kịch bản quay và tâm lý

- **Shot A — Establishing medium:** vào cinematic từ gameplay không cắt đen, giữ các mốc quầy/kính/sổ. Lan là trung tâm, không phải gương mặt hù dọa.
- **Shot B — Phản ứng con người:** giữ một khoảnh khắc nhìn nhau đủ dài để thấy Minh nhận ra người quen. Tránh close-up liên tục, camera rung, zoom ma quái hoặc môi trường đỏ loè.
- **Shot C — Lệch thời gian:** khi thoại nhắc thời điểm 2005, dùng cắt cảnh thận trọng sang lịch hoặc đồng hồ, nhưng chỉ như *chi tiết để người chơi nhớ về sau*; không nhấn hướng dẫn giải đố, không hiện toast `03:17 confirmed`.
- **Shot D — F101 như giao phó:** Lan hướng mắt đến BOARD, Minh nhìn về phòng 101. **Cú cắt/điểm trả quyền điều khiển là lựa chọn nghề nghiệp**: lời nói của Lan tạo động lực, nhưng người chơi tự quyết định có cầm hồ sơ hay không.
- **Ám ảnh trì hoãn:** Lan phải đủ ấm và tự nhiên để ở B06, một **quầy trống không cinematic** trở nên đáng sợ. Không làm Lan bật ra mặt quỷ ở B03, không xác nhận cô là ma, không cố tạo shock lúc camera trở lại.

### Contract tương tác, đồng bộ thoại và phục hồi

1. **Trigger tách đôi:** `P-INT03_LAN_NOTICED` (đề xuất, once khi Lan phát hiện) **không** tự kích cinematic. `P-INT03_DIALOGUE_START` (đề xuất, tương tác E/K) mới vào cinematic. Bất cứ lúc nào trước START, player tự do đi lại; sự kiện nhận biết không lặp khi ra/vào trigger. Không tự cộng CH01-T05.
2. **Thời điểm bắt đầu:** sau R→B commit an toàn tại AN01. Nếu người chơi chưa tới quầy, Lan idle/nhìn theo có giới hạn; tuyệt đối không kéo camera từ xa. Cảnh được kích hoạt một lần, ngăn bấm nhanh nhiều lần, không bật khi inspector/sổ/swap/voice khác đang mở.
3. **Camera:** Unity Timeline + Cinemachine là lựa chọn kỹ thuật đề xuất; vẫn dùng scene/actor/ánh sáng hiện tại (`LanDirector`, `CharacterActor`, `VoiceDirector`). Trước cinematic chụp yaw/pitch/FOV, vị trí hợp lệ, input focus; không làm dịch player capsule qua vật chắn; blend về FPS và phục hồi camera/input trong cả normal end, skip, lỗi và load. Với `reduceMotion`: dùng góc cố định hoặc dissolve/nhảy thẳng tới khung an toàn, không cinematic camera sweep; bật phụ đề đầy đủ.
4. **Tiến trình và skip:** `P-INT03_CINEMATIC_COMPLETED` / `SKIPPED` là **cờ đề xuất, chưa có runtime**. Skip luôn trả FPS, cho xem transcript/tóm tắt đúng những đoạn cốt lõi được phép biết; **không** tự hoàn tất quan sát 03:17/EX01 hay nhặt BOARD. Khi quay lại không replay toàn cảnh. Nếu save/load khi đã commit lời thoại, không phát trùng VO hoặc mark T05; nếu load giữa take, resume từ checkpoint nhất quán (hoặc trở lại trước START) chứ không kẹt cinematic.
5. **Không khóa mềm:** phòng sảnh vẫn đi được, `LAN_TALK`/BOARD trong tầm tương tác E/K/kích thước chữ lớn; player bỏ đi trước START vẫn có thể quay lại hội thoại. Nếu người chơi sprint qua hoặc không nhìn Lan, vẫn có prompt gặp Lan/nhận BOARD khi trở lại; không bắt phải xem animation phát hiện. Cảnh giữa thoại không có scare background; các `DREAD_*` được chặn/hoãn hoặc bỏ như luật hiện hành, không phát bù một cú giật bất ngờ ngay khi kết thúc.
6. **Tri thức & canon:** không dùng cinematic như chứng cứ xuất, chỉ E10a trong CH01; `CH01-D16b`/`CH04-D06` tuyệt đối không phát; không đổi ngữ nghĩa `CH01-D04c`; lời thoại chính xác lấy từ `07/Dialogue`. Hãy rà xem `LanDirector.StartEncounter()` hiện được gọi tự động từ `HospitalGame.Hub()` sau T03: **triển khai hybrid buộc phải refactor đường tự phát thoại đó** thành `Lan notices → E/K conversation`; đây là thay đổi **chưa làm trong code**.
7. **Dấu ấn cho B06:** nhớ staging tương phản — lúc gặp, quầy có đèn ấm/bút/lời nói; lúc về, sổ vẫn còn mà không có Lan. Không lặp Timeline, không dùng hiệu ứng biến Lan trước camera để hù dọa.

### Tinh chỉnh hybrid theo phản biện (v1.2 — **không bỏ cinematic**)

- **Giữ Scripted Event → chủ động trò chuyện → Cinematic Dialogue Scene → FPS**, vì đây là lựa chọn đã thống nhất. Đề nghị thay 100% cinematic bằng free-look FPS là một biến thể đáng thử, **không phải thay đổi mặc định**. Giảm số insert shot máy móc, giữ scene ngắn khoảng 60–90 giây; hình ảnh đồng hồ 03:17 có thể nằm ở hậu cảnh, không ghi `OBS_CLOCK_0317` từ góc máy.
- **Sửa hành vi chờ phím E/K:** sau khi Lan ngẩng lên, cô giữ eye contact một nhịp, chỉnh sổ, kéo ghế hoặc nhẹ nhàng đẩy **BOARD ra mép quầy như cử chỉ mời làm việc**. Nếu Minh lùi lại, Lan chuyển sang idle hợp lý rồi trở lại khi Minh đến; không khởi động cinematic tự động sau một timer 6 giây và không tự commit `CH01-T05`. Để trò chuyện, Minh vẫn **chủ động** nhấn E/K.
- **Cinematic là diễn xuất có dụng ý, không showreel camera:** ưu tiên khuôn mặt Lan, phản ứng Minh và một dấu thời gian tinh tế; tránh cắt bốn góc máy cứng nhắc. Từ quầy trở lại FPS ngay sau take hoặc Skip; người chơi tự cầm BOARD. Không khóa nhân vật ở bên kia quầy hoặc phát jumpscare khi vừa thoát cinematic.
- **Đừng biến góc máy thành vật chứng:** người chơi mới có thể nhận ra sự lệch thời gian từ thoại; nhưng muốn EX01 vẫn phải tự kiểm đồng hồ B, thẻ AS-05B và nguồn ca. Không auto ghi sự kiện chỉ vì cinematic có một shot.
- **Acceptance thêm:** test trường hợp Lan đã thấy Minh nhưng Minh bỏ đi, quay lại, spam E/K, Skip, load trước/sau/giữa thoại; cảnh phát tối đa một lần, các dòng canonical guard đúng; không mất BOARD, không auto-credit EX01; B06 không replay cinematic. Đo tỷ lệ người chơi tự nhận ra năm 2005, không mặc định 4/6 là chuẩn chất lượng tuyệt đối.

**Balo/3D v1.3 — sau hybrid cinematic:** `CH01-T03` vẫn qua **cuốn sổ trực vật lý trên quầy**, nhưng **Tab/J không còn mở nó như một menu**. Khi Lan kết thúc cảnh, Minh **tự nhấc BOARD** và cất vào ngăn Hồ sơ giấy của ba lô; `T05` chỉ commit sau cầm đúng món. Minh có thể rút BOARD để xem yêu cầu Phòng 101 trong FPS. `LAN_TALK` và Timeline không tự thu đồ, không làm nhiệm vụ thay người chơi. Lịch/đồng hồ/quầy là world observations, không “nhặt đồng hồ cho vào ba lô”.

### Học từ Backpack v2.0 — B03 (v1.4; **giữ Hybrid Cinematic Encounter**)

Nguồn độc lập gợi ý tốt về **bố cục thoại ở trong chính không gian**: lịch năm 2005/đồng hồ B 03:17/tiếng bút Lan. Chuyển chúng thành *staging lead-in* và background của **cinematic ngắn 60–90 giây**, thay vì bỏ cinematic hoàn toàn. Lan phát hiện Minh bằng scripted event, ngẩng lên và nhích BOARD ra vị trí nhìn thấy; Minh **chủ động nhấn E/K** mới vào thoại chính; không bắt đầu sau 6 giây im lặng. Sau đó trả FPS, người chơi tự nhấc **BOARD** để commit T05. **BOARD tại quầy không phải phiếu F101 anchored tại AN02**, và player không lấy F101 + bút ký từ quầy.

Một phản chiếu Lan bị kính lóa hoặc trễ một nhịp có thể thử như **P-SC03 giả định**, nhưng **không cho thấy rõ Lan hoàn toàn không có bóng**, vì như vậy dễ giải thích siêu nhiên thay người chơi. Góc máy không auto-credit `OBS_CLOCK_0317`/EX01.

**Output:** CH01-T03/T04 (theo contract hiện hành), AP02, cue Lan/biên bản thoại đúng guard, người chơi tự kích `CH01-T05` bằng BOARD; Q2 mở, Q4 tùy chọn. **Fallback:** bỏ ngăn kéo/điện thoại vẫn hiểu mâu thuẫn thời gian; mất cinematic do skip/load vẫn có transcript/tóm tắt và BOARD; nếu người chơi bỏ đi trước khi bắt chuyện, hệ thống không tự nhảy qua nhiệm vụ.

**Trạng thái:** toàn bộ thay đổi dàn dựng hybrid và các `P-INT03_*` là **đề xuất cần triển khai + QA**; baseline hiện có Lan encounter in-world tự động, không phải cinematic.

## B04 — Phòng có giường thứ tư (HALL ↔ STAFF ↔ LOC03 B, 5–6 phút tuyến chính; trục điều tra 3D)

**Mục tiêu kịch:** Biến một căn phòng thành **hiện trường có thể kiểm chứng**, nơi một con số hành chính xung đột với dấu sử dụng vật lý. Người chơi tự chọn lộ trình; hệ thống không chấm điều tra theo thứ tự.

**Topology:** Sau BOARD, ít nhất ba đường khám phá **hợp lệ và hội tụ**:
- Lối A: Quầy → hành lang → Phòng 101 → thấy bất nhất → quay STAFF tìm quy chế.
- Lối B: Quầy → DRAWER-DESK/đồng hồ → STAFF → hành lang → Phòng 101.
- Lối C: Quầy → hành lang, kiểm OBS101C/D trước → dấu giường 4 → STAFF bổ trợ.
Mọi đường dẫn tới F101; không khóa C theo một lối, không cần CH01-EX01 để ký.

**Walkthrough khám phá theo tình huống (giữ thứ tự linh hoạt; chỉ nhánh Refuted mới cần đủ OBS101A/B):**
1. Khi vào HALL, Minh thấy lối đi sạch quá mức so với R, mùi ẩm cũ dưới lớp thuốc sát trùng và F101 treo tại GATE-101. Bấm bảng kẹp trước khi khảo sát được phép: phiếu ghi số 0; sổ có câu hỏi “Số này dựa trên đâu?” Không cho một câu “giải đố bằng hai dấu” hiện ra như chỉ dẫn.
2. Đẩy hé DOOR-101 bằng giữ/kéo hoặc nhấn E. **Không tự mở hết**: khe cửa cung cấp âm quạt và góc nhìn thấp của rèm. Khi qua ngưỡng, commit CH01-T06 / checkpoint AP04.
3. Cầm/quan sát **OBS101A**: vết bánh giường thành vùng sử dụng, chăn gấp vội, dép lệch. Phần suy luận tối đa: “Một vị trí đã có sử dụng”; không tự kết luận “người bệnh bị chuyển đi lúc 03:17”. Nếu người chơi đã đọc F101 trước, UI cho thấy câu hỏi quan hệ nhưng không tick đáp án.
4. Kéo **CURTAIN-101** bằng tay để tìm **OBS101B** trên giá kẹp đầu giường 4. Đọc mặt trước và **mặt sau** của AS-05B: trường ký bàn giao thiếu; vết hằn có thể soi nghiêng 25°–40°, phím T hoặc bản Chữ lớn. Mặt sau chỉ có dấu **03g17 · CHƯA CHỐT SỔ**; không hiện tên An/C01/Khu C. Để rõ đạo đức của thao tác, bút tích nhòe được mô tả là “không đọc đủ” nếu chưa soi, chứ không thay bằng phán đoán.
5. **Tùy chọn**, đọc **OBS101C** (giường 2 chuyển đúng quy cách, hai chữ ký/đích nội B) để biết một chuyển giao hoàn tất thường để lại dấu gì. Tài liệu này **hỗ trợ hiểu**, không bắt buộc để Refuted. Cũng có thể đọc **OBS101D** (giường 3, thuốc/xuất viện) để cho thấy giấy có thể khớp hiện trường nhưng không liên hệ giường 4. Nếu người chơi đưa D làm căn cứ, F101 trả “nguồn này nói về giường khác”.
6. Tùy chọn nhưng khuyến khích: rời Phòng 101, vòng qua **STAFF**, mở DRAWER-NURSE đọc **OBS_SHIFT_LOG** (quy chế AS-05) và **OBS_CORRIDOR_NOTICE** (kê giường tạm). Phòng STAFF là nơi giải thích “vì sao chuyện thiếu chữ ký có ý nghĩa nghiệp vụ” chứ không cung cấp mã khóa. Người chơi quay về phòng để xem lại dấu bánh đã thấy với một giả thuyết rõ hơn.
7. Trong Sổ trực, người chơi có thể kẹp F101 bên OBS101B rồi thay bằng OBS101C. Chọn quan sát và ghi câu “Phiếu chốt 0 nhưng chỗ thứ tư có dấu sử dụng; thẻ bàn giao chưa có xác nhận”, **với nhãn giả thuyết**, không tự chốt sự kiện. Gỡ một giấy khỏi bàn so sánh không xóa tiến độ Examination.

**Hệ thống sai lầm có ý nghĩa:**
- Chỉ nhìn chăn: chưa thể kết luận giấy sai — chăn có thể chưa dọn.
- Chỉ nhìn thẻ: chưa thể kết luận có giường đang sử dụng — có thể thẻ cũ.
- Dùng phiếu giường 3: chứng cứ không liên quan giường 4.
- Dùng đồng hồ 03:17 để cáo buộc làm giả: thời gian trùng chưa chứng minh tác giả hay chủ ý.
- Gắn “người bệnh còn đây”: vượt chứng cứ; sổ lưu nó như **giả thuyết bị treo** nhưng không ký Refuted theo đó.
Những từ chối này phải là tự vấn có lời giải thích ngắn, không phải cảnh báo màu đỏ “SAI”.

**Điều tra phụ CH01-EX01 (không chặn G01):** Người chơi soi mặt sau AS-05B nghiêng sáng để thấy vết hằn, đọc đồng hồ B 03:17, rồi kiểm một nguồn ca (OBS_SHIFT_LOG hoặc OBS_RECEPTION_DRAWER). Tự chọn mốc trùng trong trang Timeline. Đúng thì lưu câu hỏi “Tại sao các dấu này cùng chỉ 03:17?” và mở khả năng nghe CH01-D19 **sau** ký phiếu; sai thì không phạt, không xóa ghi chú. Đây là **foreshadowing**, không là xác nhận giờ chết.

**Scare & hiện tượng theo hành động:**
- **Có sẵn:** DREAD_B04_TROLLEY_STOP chỉ sau BOARD/đi vào hành lang: tiếng xe đẩy chạy, bóng lướt ở cuối HALL rồi dừng đột ngột; có thể quay lại kiểm hành lang rỗng. Không cho xe đẩy chắn đường.
- **Có sẵn:** DREAD_B04_CURTAIN_BREATH; gió làm rung rèm khi người chơi đang cố nhìn dấu giường 4.
- **P-SC04 “Không ai cầm đầu cáng” (đề xuất, mức vừa):** sau khi đã quan sát OBS101A và quay ra hành lang, âm một bánh xe nảy mạnh phía sau cửa vừa khép; qua khe cửa thấy bóng xe trên nền nhưng **không có vật thể xâm nhập**. Không hiện mặt người. Đây là cách nhấn vào vết bánh giường và sự thiếu xác nhận, không phải vật thể tấn công.
- **DeferredMicroShift hiện có:** sau khi người chơi hạ một giấy, rèm lặng lẽ khép thêm hoặc đèn tuýp ngắt 0,6 giây; chỉ dùng khi không đe dọa khả năng đọc, vùng đi lại hay suy luận. Mỗi cue ghi once flag và không biến thành evidence.

### Spatial Deduction — căn phòng phải *được điều tra bằng cơ thể* (v1.2)

Hai micro-task **đề xuất** cho phép người chơi tự kiểm không gian thay vì chỉ kéo thẻ vào UI; cả hai đều làm rõ `OBS101A`, **không tạo evidence chính thức mới** và không lấy mất giải đố CH02:

1. **P-INT04_SPATIAL_ALIGN:** đi từ giường 1–3 đến hốc số 4, so cách bố trí đầu giường, khoảng trống, chân giường/ổ điện và dấu bánh dưới rèm. Bố trí cho phép người chơi *nhìn ra một vị trí giường đã từng được dùng* từ khoảng cách và dấu mòn, nhưng không phát badge “4 bệnh nhân” hoặc tự suy ra tên bệnh nhân. Không chụp trực tiếp toàn bộ logic `SOCKETS` của CH02-R sang đây.
2. **P-INT04_FLOOR_RAKE:** chủ động kéo rèm, dịch bước hoặc xoay đèn pin để thấy một đoạn vệt mòn trong lớp B ở đúng hướng hốc giường; tự quay sang các giường hiện hữu để so vị trí. Vệt này chỉ là chi tiết **thuộc `OBS101A`**; không gắn `WHEELS` / `LIST` / `E12` / `E11` vốn thuộc R của CH02. Bản chữ lớn và phụ đề cho người không thể nhìn raking light phải cho thông tin tương đương.
3. **Bốn công tắc bị dán băng trong bản kiểm toán: _chỉ được cân nhắc sau review_**. Repository hiện xác nhận **4 ổ cắm đầu giường** trong kiến trúc, không tự suy ra có 4 công tắc riêng và đèn sau rèm. Nếu muốn dùng, phải đối chiếu Core 03/04, mô hình điện và specialist TASK026/028; không biến thao tác điện thành vật chứng xác minh độc lập.
4. Phản hồi vật lý tức thời: sau khi tự tìm vệt bánh, tiếng xe cáng xa có thể bất chợt dừng ở hướng phù hợp. Đó là dấu gợi câu hỏi về *việc sử dụng vị trí này*, không xác nhận người đẩy là ai. Ở mức logic, Refuted vẫn phải do `OBS101A + OBS101B + marks/link/claim` thông qua HypothesisService. A/B không cần micro-task này để qua G01.

**Kiểm chống spoiler:** chơi lại CH02 sau đủ mọi micro-task, `LIST/SOCKETS/WHEELS → PZ01/E12 → E11` vẫn là một phát hiện độc lập trong R. Mute, alternate E, large print, save tại AP04 và các góc nhìn đều phải tương đương.

**Balo/3D v1.3 — chuyển suy luận từ UI ra thế giới:** Dùng raycast và `PhysicalInspector` hiện có cho 3D:
1. Hé `DOOR-101`, kéo `CURTAIN-101`, bước tới dấu bánh/giường 4 để **xem hiện trường OBS101A tại chỗ**. Vết bánh/ổ cắm/dấu sàn **không vào ba lô**.
2. Nhấc thẻ **AS-05B (OBS101B)** khỏi kẹp, soi mặt trước/mặt sau, nghiêng đón sáng, chỉ vào ô chữ ký còn trống; tùy chọn **cất tạm giấy B vào ngăn “Giấy trong ca trực”** (đánh dấu nguồn B, không phải evidence).
3. Có thể lấy OBS101C/D ở vị trí nguồn rồi đặt **hai tờ** cạnh nhau trên bàn STAFF hoặc vị trí có mặt phẳng hợp lệ, lật giấy để đối chiếu. Không cho phép cất một bản gốc của B rồi đem thành giấy thật trong R.
4. Tập ghi chú **nằm trong ba lô** ghi đúng `ObservationRecord` đã khám phá, không tự giải F101; người chơi vẫn cần soi dấu vật lý và nguồn AS-05B độc lập để có quyền chọn C. Nếu bỏ các tài liệu phụ, A/B và route CH01 vẫn hợp lệ.

**Fallback:** Nếu người chơi cất AS-05B vào túi và bỏ quên hoặc đặt trên bàn, có đường lấy lại/tìm vị trí đã đặt trước khi cần soi; trường hợp giảm chuyển động/bàn phím có action list tương đương. Không làm mất props/chứng cứ vì bag đầy.

### Học từ Backpack v2.0 — B04 (v1.4)

**Giữ chất xúc giác:** người chơi tự kéo rèm, đổi vị trí để thấy vệt bánh/vị trí giường, nhấc `AS-05B`, lật thẻ, nghiêng đón sáng và cất tạm trong ngăn Giấy tờ B. Đặt thẻ lên bàn đối chiếu là chọn kiểm nguồn, không phải một bước bắt buộc của route A/B. Đừng ép rê rèm đến đúng mức 60% rồi phải giật lần hai; E/K và kéo analog cần đồng đẳng.

**Không nhân bản canon mới:** bốn **công tắc** có băng dính chưa được xác nhận (hệ thống đã có bốn **ổ cắm CH02-R**, không giống công tắc B); “dép tổ ong trẻ con” chỉ nên giữ đúng mô tả asset hiện hành là **dép nhựa nhỏ**, không gán bệnh nhân cụ thể; vệt bánh dẫn tới hốc 4 không mặc nhiên chứng minh hành lang khu C. **Không thử chìa khu C/LOC04 trong CH01**. Nếu muốn thêm công tắc, phải review Core 03/04 và chuyên gia trước. Bản nền `OBS101A/B` và tuyến CH02 `LIST/SOCKETS/WHEELS` vẫn tách.

**Output:** ít nhất đường cho OBS101A, OBS101B và khả năng nộp F101; hồ sơ đối chiếu C/D/STAFF tùy chọn; Q3 hình thành; CH01-T06. **Fallback:** nếu dùng E không kéo analog, tương tác vẫn tương đương; người chơi có thể quay lại các nguồn chưa đọc; cửa luôn có lối mở; mất nguồn âm không làm mất một dấu bắt buộc; save ở ngưỡng phục hồi toàn bộ trạng thái ngăn/rèm/giấy.

## B05 — Chữ ký làm biến dạng căn phòng (LOC03 B, 4–5 phút tuyến chính; choice climax)

**Mục tiêu kịch:** Chuyển từ “tìm một đáp án đúng” sang “chịu trách nhiệm về mức độ biết chắc”. Đây là đỉnh cảm xúc của CH01 và là nút gameplay ảnh hưởng lớp B. Ba lựa chọn **đều hợp lệ**, nhưng kể ba nỗi sợ khác nhau.

**Walkthrough chính xác:**
1. Người chơi nhấc F101 khỏi bảng kẹp, xem mặt trước/sau, có thể đánh dấu vùng **số 0** và ô **chữ ký bàn giao trống**, nhìn lại OBS101A/OBS101B bằng inspector hoặc bàn so sánh. Không bắt mọi người phải đánh dấu để chọn A/B; chỉ C cần lập luận được xác minh.
2. Trang lập luận cho phép chọn nguồn (OBS101A và OBS101B), nối “vết sử dụng tại chỗ thứ 4 ↔ bàn giao chưa xác nhận”, chọn **“Chưa đủ căn cứ xác nhận số 0”**. Nếu thiếu một trong hai nguồn hoặc gán sai vùng, phản hồi giải thích chính xác cái thiếu. Các kết luận “Có người còn trong phòng”, “Có người cố ý làm giả” **không được chấp nhận**, dù người chơi thích chúng hơn.
3. Người chơi chủ động ký chọn:
   - **A — Affirmed / Xác nhận 0:** không cần chứng cứ. Mực khô, con dấu rơi gọn; tường vách **deniedBay** phủ hốc 4, quạt đều lạnh, F101_SignedA và D(LOC03)=1. **Nỗi sợ của lựa chọn dễ dàng:** khi vừa nhìn vách mới, hai tiếng gõ nén sau tường (DREAD_F101_AFFIRMED_SUFFOCATION). Không khẳng định có người phía sau.
   - **B — Unverified / Để ngỏ:** không cần chứng cứ và **không bị phạt**. F101_SignedB, rèm nửa mở, nhịp quạt chậm, khoảng im kéo dài (DREAD_F101_UNVERIFIED_STILLNESS). **Nỗi sợ của sự chưa biết:** thế giới không ép người chơi tin một giả thuyết hấp dẫn.
   - **C — Refuted / Ghi nhận bất nhất:** cần cặp OBS101A/B và câu lập luận hợp lệ; F101_SignedC, rèm tách, vết bánh rõ hơn, tường ẩm và CH01_PR01_Corner / đèn tuýp chập nhẹ (DREAD_F101_REFUTED_CRACK). **Nỗi sợ của việc chống lại giấy:** thực tại B nứt ở chỗ từng bị giấy phủ lên.
4. Sau khi ký, **G01 mở với cả A/B/C**. Người chơi chưa bị ép rời đi; có thể xem lại hốc giường 4, kiểm vật chứng, sửa phiếu và thấy cập nhật mới. Lần ký đầu bất biến firstDecision, ký sửa tăng revision; bấm trùng cùng quyết định không nhân trạng thái.
5. Có cơ hội nghe **CH01-D19** sau khi ký nếu và chỉ nếu CH01-EX01 hoàn tất, trong đoạn về hành lang trước bàn giao. Đừng cho D19 đọc thay kết luận thủ phạm.

**Đỉnh sợ được thiết kế (P-SC05 “Bóng người ở đầu giường” — mới, một lần):** Chỉ sau khi người chơi đã tiếp cận thẻ AS-05B rồi hạ giấy, biết rằng vẫn đang ở 101, không mở modal và không nghe thoại. Camera trở lại vị trí tự do; nếu nhìn qua rèm, trong khoảng **0,8–1,3 giây** có bóng đen đứng thấp hoặc hình dạng như người tựa đầu giường 4; một tiếng thở nghẹn và kim loại va cạnh giường đồng bộ, bóng biến mất khi đèn tuýp phục hồi. **Không đưa mặt ma, không lao vào camera, không xác nhận bệnh nhân/nhân dạng.** Thời điểm phụ thuộc thao tác **đọc thẻ rồi quay lại kiểm chỗ trống**, không phải random. Nếu người chơi không nhìn đúng hướng, chỉ phát âm môi trường và giữ không gian có thể kiểm tra; không teleport camera ép xem bóng. Nếu đang phát DREAD_SHIFT_TUBE_BLACKOUT hoặc cắt điện khác, director gộp chúng thành một cue duy nhất; không chơi chồng. Với reduceMotion/photosensitivity: không flash nhanh, dùng suy giảm ánh sáng êm + silhouette tĩnh/âm được caption, có toggle giảm jumpscare. Scare này **không** làm người chơi mất quyền xem chính xác dấu vết.

**Scare hậu ký:** Đừng phủ thêm ồn sau P-SC05. DREAD_F101_* phải luôn xảy ra đúng **sau phản hồi bút/dấu**, có khoảng ngắt để nhận ra là **phản hồi của lựa chọn**. Với A có tiếng vách; B có im lặng và quạt; C có nét nứt và bánh xe — không trao thưởng “chân lý” cho C bằng jump lớn vượt A/B.

### Scare dẫn bằng vật lý và hiện tượng “văn bản chống lại ký ức” (v1.2)

**Audio-led framing của `P-SC05_BED4_SHAPE` (đề xuất):** khi hạ tờ AS-05B và camera đã trở lại gameplay an toàn, giảm từ từ tiếng quạt và hum đèn trong khoảng **0,8–1,5 giây**, rồi cho một tiếng móc rèm/khung kim loại vang **có hướng từ hốc 4** để người chơi có lý do tự quay nhìn; silhouette xuất hiện chừng 0,8–1,2 giây. **Không xoay camera cưỡng bức, không bảo đảm “90% người chơi nhìn thấy”.** Tiếng nền vẫn đủ làm mốc không gian; giảm mức đỉnh âm theo review, đồng thời caption có hướng. `0 dBFS` nghĩa là mức tham chiếu cực đại của tín hiệu số chứ **không phải im lặng**; không thực hiện nguyên văn yêu cầu “ngắt về 0 dB” của báo cáo. Người không nhìn vẫn tiếp tục điều tra; F101 hậu ký là payoff **không thể bỏ lỡ** trong cả ba nhánh. Tránh xếp chồng với deferred tube blackout, giao thoại, save/load và cinematic.

**P-META05_OFFICIAL_OVERPRINT (đề xuất, hiển thị B-only, không sửa provenance):** khi người chơi đã ký và chủ động mở trang **bản sao biểu mẫu của cơ sở trong lớp B**, một lớp phủ diễn xuất như mực trắng/nhòe xuất hiện trên vùng **bản trình bày của hồ sơ**:
- **Affirmed:** số 0 trông quá rõ, lấn phần chú thích mô tả hốc giường; người chơi nhìn thấy văn bản hành chính đang cố làm phần đó “không tồn tại”.
- **Unverified:** chỗ chưa xác nhận giữ nét mực lửng, không biến trạng thái giữ lại thành một hình phạt.
- **Refuted:** nét gạch xác nhận bất nhất hiện lên, không biến thành lời thú nhận làm giả.

**Quan trọng:** đó chỉ là lớp rendering/presentation, có thể tắt và xem bản gốc ngay. **Không thay đổi `ObservationRecord`, dữ liệu `OBS101A/B`, provenance, first/current F101, transcript, bản scan nguồn, save hoặc nội dung ghi tay “Đã thấy” của Minh.** Không phát âm giật khi đang đọc; chỉ là uncanny visual chậm, có bản không chuyển động và large print nguyên vẹn. Không buộc mở nhật ký để qua G01.

**Đối lập cảm xúc:** sau khi ký, bổ sung **một ghi chú nội tâm ngắn** (đề xuất nội dung, không phải thoại canon, cần 07/Dialogue phê duyệt) để người chơi cảm nhận Minh đang nhớ một cánh cửa từng không mở. Không đưa hình ảnh cánh cửa khu C hay mô tả hành động CH07; không phán xét hoặc chấm đạo đức lựa chọn A. Sổ trực giữ hai cột rõ ràng: **đã thấy** và **cảm giác/điều day dứt**.

**Balo/3D v1.3 — F101 phải là một quyết định bằng tay:** F101 vẫn nằm trên **bảng kẹp tại AN02**; Minh có thể đưa giấy từ ba lô ra cạnh phiếu, cầm bút/đánh dấu hotspot `F101_ZERO` và `OBS101B_SIGN`, nối dấu sàn `OBS101A_TRACES` bằng hành động đối chiếu có xác thực. Người chơi tự ký trên bảng kẹp; `HypothesisService` kiểm lại **đúng nguồn đã Examined trong B** và claim, không chấp nhận kéo item ID tùy tiện. **A/B luôn ký được** không cần nhặt bất kỳ thẻ nào. Ba nhánh giữ F101 first/current/revision, G01 và hậu quả hiện thực B như cũ. Hiệu ứng hồ sơ bị mực trắng xóa ở nhánh A chỉ tác động **lớp hiển thị của bản sao**, không xóa object/item/note/evidence.

**Output:** CH01-T07, G01 true, first/current/revision và variant B theo F101, không thay R. **Fallback:** thiếu A/B không khóa đường A hoặc B; lỗi save không chiếu hậu quả trước commit; có thể phục hồi phiếu & quan sát qua load.

## B06 — Người đã nhận phiếu biến mất (LOC03 → HALL → LOC02, B→R, 1,5–2,5 phút tuyến chính)

**Mục tiêu kịch:** Cho người chơi trải nghiệm hậu quả của điều mình **đã ký**, sau đó nhận ra người giao việc không còn ở đó. **Đối trọng với cinematic ấm áp ở B03:** B06 giữ camera FPS tự do, không dựng một cutscene Lan biến mất; chính việc người chơi tìm Lan ở đúng góc quầy trước đây tạo khoảng trống đáng sợ. Biến tuyến backtrack thành một lần **đọc lại môi trường**.

**Walkthrough:**
1. Rời phòng 101. HALL phản chiếu F101 trong chi tiết đã tồn tại: slip/hồ sơ hành lang theo lựa chọn, lời PA méo/sai nhịp **CH01-PA04**, quạt chậm (HE04). Người chơi có thể quay vào buồng để kiểm xem hậu quả có khớp quyết định hay không.
2. Một đối chiếu nhỏ **P-INT06 “Dấu ký vs căn phòng” (đề xuất, không bắt buộc):** tại cuối hành lang, trang Sổ trực đặt song song bản F101_SignedX với ghi nhận OBS101A/B cũ. Người chơi nhận ra **tài liệu ký đã đổi, quan sát cũ không bị xóa khỏi nhật ký**. Đây là một payoff điều tra: sự thật quan sát không thể bị viết lại chỉ bằng giấy.
3. Nếu hoàn tất EX01, CH01-D19 phát tại khoảng nghỉ phù hợp, không chen vào thoại/hành động; nếu không, người chơi vẫn nghe nhiễu PA theo nhịp. Hiện tượng DREAD_B06_PA_INTERFERENCE và DREAD_B06_CLOCK_TICK có ngân sách; không làm clock trở thành câu trả lời.
4. Về AN01, người chơi nhìn lại cùng quầy đã từng được dàn dựng ở B03: bút, ghế, sổ và ánh đèn còn đó nhưng **không có Lan**. **Tự tay giở sổ để bàn giao**, commit CH01-T08. B→R swap tại chỗ với preflight/rollback. Khi ngẩng lên, Lan không còn ở quầy; đồ vật lại lão hóa, ánh đèn trả về 2012; cuốn sổ vẫn ở đó. Không dùng Lan biến mất trong một cú dựng hình jump khi đang nhìn mặt bà — tránh biến Lan thành trò hù dọa rẻ.
5. Câu hỏi nhật ký được cập nhật: “Lan đã ở đây trong trải nghiệm vừa rồi; nhưng sảnh R không có ai. Mình có thể kiểm lại Phòng 101 khi đang ở R hay không?” Điều này dẫn lý do sang CH02, không phải mệnh lệnh đi nhặt E11.

**Đạo diễn sợ:** Cú sợ là **sự thiếu vắng** sau khi đã quen với Lan. Âm quạt ngừng và đèn bàn tắt chậm thay cho tiếng hú. Người chơi được đứng tại quầy 10–15 giây hoàn toàn yên, có thể xem lại lời trong transcript.

### Payoff đối xứng B03/B06 và thiết kế âm ba lớp (v1.2)

B03: quầy có Lan, đèn bàn ấm, bút đang viết; B06: **đúng quầy đó**, cuốn sổ vẫn ở đây nhưng giọng Lan/tiếng bút đã mất. B06 **không có cinematic mới**; người chơi tự lia camera tìm cô. Hệ âm ba lớp diễn giải điều đã mất: **R** là mưa/sân bỏ hoang; **B** là quạt, hum đèn, giọng người, tiếng giấy; **R sau bàn giao T08** trở lại âm ngoài trời nhưng thiếu tiếng công việc quen của Lan. Caption/mono truyền sự khác biệt, không giấu manh mối bắt buộc trong stereo.

**Nén nhịp:** quay về AN01 là hành động duy nhất bắt buộc; đối chiếu bản F101 với ghi chú trong sổ **tùy chọn**. Không buộc mở cùng tài liệu lần thứ hai chỉ để cho T08 chạy. Giữ một khoảng **10–15 giây không jumpscare** sau khi Lan vắng, vì cảm giác mất mát cần thời gian tự hình thành.

**Balo/3D v1.3 — khoảnh khắc tách “vật lý” và “ký ức”:** Nếu Minh đang cầm một tờ giấy thuộc B hoặc đã cất AS-05B trong túi, khi `T08` chuyển B→R hệ thống phải **trả B-original về fixture/anchor cũ** và giữ một **reference nhớ lại đã đọc** trong tập ghi chú của ba lô (không phải item cầm dùng ở R). Reconcile ở thời điểm preflight/commit nguyên tử; rollback khi thất bại, không tự xóa ObservationRecord. Sau chuyển lớp, Minh có thể kiểm tra ba lô và nhận ra không có “bản giấy thật từ 2005” trong tay, làm nỗi bất an B06 hữu hình. **Không bắt người chơi xem túi để mới được sang B07**.

**Output:** CH01-T08 và R phục hồi, Lan vắng mặt. **Fallback:** nếu người chơi quay lại khi chưa bàn giao, được sửa F101 ở B theo contract; khi hoàn tất T08 không làm A/B/C phá OBS của R. Checkpoint trước/sau swap, không mất firstDecision.

## B07 — Cánh cổng dẫn về cùng một nơi (LOC02 → LOC01 → LOC02, R, 1–1,5 phút tuyến chính)

**Mục tiêu kịch:** Người chơi thử giải pháp hợp lý nhất của con người: rời khỏi bệnh viện. Seam buộc họ phải đối mặt điều chưa giải quyết nhưng không biến game thành escape-room vô nghĩa.

**Walkthrough:**
1. Sau T08, mục tiêu mới là **“Thử rời khỏi bệnh viện”**. Minh quay qua sân đã đi ở B01. Các landmark xe, biển cổng, đèn mái hiên và rãnh nước **không đổi vị trí**; người chơi nhận ra “đây là nơi mình vừa vào”.
2. Khi tới SEAM, tương tác vật lý kéo cổng. Trong 0,5–1 giây, mưa ngoài đường tưởng như sáng lên; âm xe máy/mưa bị hút nhỏ, cánh cổng rung như có lực tác dụng từ **phía không thấy**. Transition theo kịch bản CH01-T09/HE06 đặt người chơi lại trong không gian tương đương lối về, không tạo vô hạn vòng lặp.
3. **P-SC07 “Tiếng gõ trả lời” (đề xuất, scare mạnh thứ hai nhưng ngắn):** sau khi người chơi tự kéo cổng quá một ngưỡng, âm **một cú đập ngang khung cổng từ phía ngoài** đi cùng bóng phản xạ lướt trên kính/kim loại, không cho khuôn mặt/cánh tay danh tính. Ngay sau cú giật, mưa tắt thành nghẹt trong 1–2 giây; người chơi có thể lập tức nhìn lại cánh cổng đứng yên. Chỉ chạy **một lần**, sau khi đã cách P-SC05 tối thiểu 60 giây hoặc hạ thành tiếng kim loại nhỏ. Có caption có hướng và phiên bản reduced jumpscare.
4. Minh nói CH01-D14 đúng nơi đã có. Nếu người chơi thử cổng lần hai, chỉ có phản hồi **“Không tìm được đường ra. Trở lại sổ trực.”** Không kéo họ qua 3–4 vòng loop gây mệt hoặc gây chóng mặt.
5. Người chơi có thể quay lại ngắm thông báo tháo dỡ: ký ức về ca trực đang nằm trong một tòa nhà đã đóng. Bất nhất thời gian không biến thành “giấc mơ là giả”; vẫn chưa thể phân loại nguyên nhân.

**Ý nghĩa:** Cổng đầu B01 là lựa chọn **đi vào**; cổng B07 cho thấy **chưa thể đi ra bằng cách phủi bỏ**. Đây là đối xứng hành động, không là mini-game mới.

### The Uncanny Walk — khoảng nghẹt thở B07→B08 (v1.2)

Sau T09, không cắt màn hình hoặc xuất thêm vòng lặp: để Minh **tự đi từ cổng về quầy** trên tuyến thực. Tiếng mưa/xe ở lớp R được duck nhẹ, nghe như không gian đang thu nhỏ trong một khoảng **15–30 giây tùy tốc độ di chuyển**, rồi âm trở lại khi gần AN01. Đèn quầy trở thành điểm nhìn, một vệt sáng yếu ở hướng Phòng 101 tạo câu hỏi chưa giải; **không tắt toàn bộ đèn sân nếu phá landmark/đường đi hoặc trợ năng**. Đường đi ngắn đủ để người chơi thấy mình đã chọn quay lại, không dài chỉ để tăng thời lượng. Nếu P-SC07 không được kích hoạt ở chế độ giảm jumpscare, loop và Uncanny Walk vẫn diễn ra hoàn chỉnh.

**Balo/3D v1.3:** Người chơi **tự kéo/thử cổng bằng tay**. Không giải `SEAM` bằng chìa/bùa/đồ bất ngờ trong túi; đây là biến cố không gian đã có `T09`. Sau một lần loop, ba lô vẫn giữ đồ R theo đúng thứ tự; giấy B không tự trở thành physical. `Uncanny Walk` được giữ, không sinh các vật phẩm rơi trên sân chỉ để kéo dài thời gian.

**Output:** CH01-T09, seamConsumed=true, objective trở về sổ. **Fallback:** seam không lặp vô hạn, không gây mắc trong collider ở cửa; người chơi luôn có đường tới AN01 sau load; disable jumpscare không đổi T09.

## B08 — Điều còn ở phía bên kia cánh cửa (LOC02, R, 1–1,5 phút tuyến chính)

**Mục tiêu kịch:** CH01 kết bằng **một vết thương riêng của Minh chưa khép lại**, đồng thời mở một việc điều tra rất cụ thể: kiểm tra xem hiện trường R có còn những dấu nào không lệ thuộc phiếu F101. Không đọc như tuyên ngôn phương pháp hay bài thi đạo đức. Bất kể A/B/C, người chơi vẫn muốn biết những gì mình vừa thấy có thể bị phủ nhận hay không.

**Walkthrough:**
1. Trở về sổ tại AN01. Trang Timeline chỉ hiển thị những sự kiện đã thực sự ghi: 18/11/2005 vụ cháy/13 người chết theo công khai; 2012 lời hẹn Bảy bị hoãn; cuộc gặp Lan trong trải nghiệm B; Phòng 101 có dấu giường 4/thẻ thiếu xác nhận nếu người chơi đã xem; kết luận F101 **của chính người chơi** và mức độ chắc chắn.
2. Trang **“Điều chưa được chứng minh”** luôn có các câu chưa trả lời: “Ai ở giường 4?”, “03:17 đánh dấu điều gì?”, “Vì sao Lan hiện diện?”, “Tại sao mình nhớ một cánh cửa từng không mở?”. Câu này được hiển thị như **câu hỏi**, không phải objective bắt người chơi đi giải ngay trong CH01.
3. **P-INT08 “Xác định bước điều tra kế tiếp” (đề xuất, tối đa một lựa chọn UI):** chọn “Kiểm tra lại Phòng 101 trong hiện thực 2012” để tạo động cơ; **không thêm cổng** vì CH01-T10 hiện đã có thao tác giở sổ và hợp đồng transition. Nếu bỏ qua interaction phụ, việc giở sổ vẫn commit T10 như hiện hành.
4. Khi giở sổ, CH01-T10; card **CHƯƠNG 02 — PHÒNG BỆNH TRỐNG**, CP04, R. Đường CH02 sử dụng **LIST + SOCKETS + WHEELS → PZ01/E12 → E11 → G02**, không được phát sẵn đáp án/hình C01.
5. Kết cảnh bằng một nhịp đơn giản: tiếng giấy lật ở quầy R, không sử dụng scare nữa. Người chơi chủ động bước sang một không gian quen mà giờ mang ý nghĩa khác.

**Payoff tầng:** Ở B05, Minh ký một bản báo cáo; ở B08, anh tự hỏi vì sao một cánh cửa trong ký ức vẫn không chịu mở. Người chơi đi sang CH02 **vì bất an và vì muốn kiểm chứng hiện trường thật**, không phải vì game chấm phương pháp đúng. Câu nội tâm ngắn phải được duyệt Dialogue, giữ câu hỏi về cánh cửa ở mức chưa hồi tưởng (không tiết lộ CH07).

### Ghi chú tay: nỗi sợ không phải một trường dữ liệu (v1.2)

Giữ nguyên các trang Sổ trực phản ánh **sự kiện / nguồn / suy luận có thể chứng minh / điều chưa biết**. Ở lề trang, thêm **một dòng “Minh tự ghi”** như thể chữ viết chưa thật đều (P-NOTE08, đề xuất copy cần review) để tách cảm xúc khỏi hồ sơ:
- Dòng **dữ kiện** luôn rõ ràng: có thư Bảy, đã thấy/không thấy OBS101A/B tùy save, đã ký A/B/C và chưa biết thêm.
- Dòng **nội tâm** chỉ gợi *một lần Minh từng không mở cửa*; không trích lời cầu cứu, không hiện ô kính/cửa khu C, không nói ai đứng ngoài hay còn sống.
- Không dùng câu tuyên ngôn “tôi sẽ không ký...” nếu người chơi thực tế đã chọn **Affirmed**. A không bị tước khả năng tiếp tục điều tra; B và C không bị gắn danh hiệu “đúng đắn”. Người chơi có thể bỏ dòng viết tay mà vẫn khởi CH02.

**Kết bằng hành động:** Minh tự giở sổ tại AN01. Cảnh kết ngắn; người chơi muốn trở lại Phòng 101 năm 2012 do **một nghi vấn vẫn nhói**, không do cinematic phát đáp án.

**Balo/3D v1.3 — chuẩn bị CH02 bằng một hành động đồ vật:** Người chơi mở ngăn giấy của ba lô thấy **E10a thật** và tập ghi chú về sự khác biệt ở 101; *không* thấy vật chứng R của CH02 đã chưa lấy. Có thể rút bì thư ra xem lại, ghi câu hỏi cảm xúc rồi cất; cuối cùng tự giở **sổ trực vật lý tại AN01** để `T10` chuyển CH02. Không bắt thêm một menu suy luận năm tab; Timeline/Decisions là các trang phụ trong tập ghi chú, và `T10` không được khóa sau khi mở ba lô.

**Output:** CH01-T10/CP04, CH02 bắt đầu, CH01 evidence count vẫn 1; FIRST/Current F101 và sự kiện dread lưu bền vững. **Fallback:** người chơi bỏ hết optional vẫn được tóm tắt tri thức tối thiểu, không nhận những chi tiết chưa từng quan sát; transcript từ skip ghi theo dữ liệu đã được trình bày, không sinh thoại bị lock.

---

## 3. Thiết kế horror theo nguyên nhân — không phải ngẫu nhiên

| Cue | Beat, độ mạnh | Trigger theo HÀNH ĐỘNG | Người chơi sợ vì điều gì? | Trạng thái |
| --- | --- | --- | --- | --- |
| DREAD_B02_GLASS_SHADOW | B02, thấp | Đứng trên trục nhìn kính sau T02 trước T03 | Có người chờ bên trong nơi bị bỏ hoang | Đã có |
| P-SC02_GLASS_DELAY | B02, thấp–vừa | Quan sát kính lần 2, khoảng lặng đủ, không hội thoại | Hình ảnh không hoàn toàn khớp với chuyển động | **Mới, đề xuất** |
| DREAD_B04_TROLLEY_STOP | B04, vừa | BOARD + đi vào HALL | Một cuộc vận chuyển bị cắt ngang | Đã có |
| DREAD_B04_CURTAIN_BREATH | B04, thấp | Đứng gần rèm giường 4 | Phòng trống có nhịp thở của chính căn phòng | Đã có |
| P-SC04_STRETCHER_ABSENCE | B04, vừa | Xem dấu bánh rồi quay lại HALL | Có âm xe cáng nhưng không có người đẩy | **Mới, đề xuất** |
| DREAD_SHIFT_* | B04/B05, thấp–vừa | Hạ giấy đã soi; không có modal | Hiện trường xê dịch khi mình đang chú ý hồ sơ | Đã có |
| P-SC05_BED4_SHAPE | B05, **cao** | Hạ AS-05B, nhìn qua rèm, đủ an toàn | Có “hình người” nhưng không có bằng chứng về ai | **Mới, đề xuất** |
| DREAD_F101_A/B/C | B05, vừa | Sau ký F101 và commit; branch-specific | Hồ sơ làm căn phòng thay đổi | Đã có |
| DREAD_B06_PA_INTERFERENCE / CLOCK_TICK | B06, thấp | Backtrack sau G01 | Bệnh viện “trả lời” bằng hành chính và thời gian | Đã có |
| DREAD_B07_GATE_RAIN_HUSH | B07, thấp | Thử lối ra sau T08 | Không gian quen mất âm thanh bình thường | Đã có |
| P-SC07_GATE_KNOCK | B07, **cao nhưng rất ngắn** | Tự kéo cổng SEAM lần đầu | Thế giới chặn chính hành vi muốn thoát | **Mới, đề xuất** |
| B08 silence | B08, không scare | Khi trở lại sổ sau T09 | Người chơi tự quyết định tiếp tục điều tra | Dàn dựng, không event mới |

**Quy tắc chống phá trải nghiệm:**
- Không âm lớn thình lình trong VO, menu, đọc giấy, modal, save, swap preflight hoặc ngay khi tải checkpoint. Không auto-rotate camera / rút quyền điều khiển.
- Director nắm cooldown tổng và ưu tiên cue theo mục tiêu: cue dạy điều tra/giúp thấy chi tiết > scare nền. Event mới cần registry + save once + test repeated load. Các cue mạnh được ưu tiên nhưng không “xếp hàng để phát bù” sau một scene có thoại dài.
- Nếu người chơi quay lưng, sự kiện không được tự chuyển thành bàn tay lao vào mặt; dùng cue âm/vật lý đủ hiểu và có thể bỏ lỡ. **Missable scare, not missable evidence**.
- Điều chỉnh độ tương phản, mức đỉnh SFX, tần số flicker, caption không gian, chế độ mono/reduceMotion/giảm jumpscare; âm thanh là thưởng cảm giác, không là kênh thông tin duy nhất.
- Dùng *sự biến dạng có nghĩa*: vách xuất hiện sau Affirmed = xóa khỏi biên bản; rèm còn đó sau Unverified = chưa chốt; vết bánh rõ sau Refuted = một bất nhất được công nhận. Không áp dụng random biến dạng lên props CH02 R.

## 4. Pacing & ngân sách điều tra — v1.3 với thao tác vật lý

**Không lấy target 18–24 phút của v1.2 làm chuẩn cứng**: vòng chơi mới cần thời gian cầm, cất, lấy, soi 3D và đặt đồ lên bàn; nếu ép về 18 phút, người chơi sẽ bị lùa qua hành động quan trọng. Target chính **20,5–30 phút**, khám phá sâu thêm 6–10 → **26,5–40 phút**; chỉ là **giả thuyết thiết kế** chờ TASK010.

| Beat | Thời lượng mainline (phút) | Tương tác vật lý có ý nghĩa | Có thể bỏ qua |
| --- | ---: | --- | --- |
| B01 | **1,5–2,5** | nhặt/soi/cất E10a; dùng chìa/cổng | prop xe/biển |
| B02 | **1–1,5** | kiểm bóng qua kính; tới AN01 | scare phụ |
| B03 | **3–4,5** | T03, Lan nhận diện→cinematic, tự nhặt/cất BOARD | đọc lịch/tài liệu bên quầy |
| B04 | **6–8** | mở/kéo/soi dấu, nhấc AS-05B, lật, raking, cất | C/D/STAFF/EX01/đặt hai giấy |
| B05 | **5–7** | lấy giấy ra/đối chiếu, đánh dấu F101, ký A/B/C | overprint/so hồ sơ phụ |
| B06 | **2–3** | về quầy, B→R, giấy B thành reference | mở túi xem note |
| B07 | **1–1,5** | kéo cổng, một seam, đi về | scare phụ |
| B08 | **1–2** | xem lại E10a/note hoặc giở sổ để T10 | tự ghi nhật ký dài |

**Tính lại:** Min = 1,5+1+3+6+5+2+1+1 = **20,5**; Max = 2,5+1,5+4,5+8+7+3+1,5+2 = **30**. **Những bước “cất và lấy ra” nếu không có mục đích điều tra có thể được bỏ qua**, ví dụ A/B không cần rút AS-05B từ ba lô; đổi qua lại menu không được trở thành time sink.

**Telemetry cần thu:** `time_to_first_pickup`, `time_to_first_inspect_3D`, `bag_open_count`, `item_equipped_use_attempts`, `successful_world_item_uses`, `documents_reexamined`, `world_observations`, `F101_reasoning_confidence`, `time_in_modal` vs `time_in_world`, những lỗi bế tắc do item không ở vị trí dự kiến. **Không dùng số lần bấm làm chỉ số chất lượng**; hỏi người chơi hành động lấy món đó ra giúp họ kiểm điều gì.

## 5. Acceptance walkthrough & trạng thái QA cần có

**Các đường bắt buộc để kiểm:**
0. **Hybrid B03 (mới):** sau T03 vào B, Lan phát hiện Minh mà FPS vẫn tự do; tiến/lùi/quay đi không tự bật cutscene; E/K mới mở cinematic. Đảm bảo thoại có guard, camera blend, diễn xuất/lip-sync, phụ đề, không bấm trùng, skip, giảm chuyển động, bấm BOARD *sau* cảnh mới commit T05. Tải lại trước/sau/lúc đang thoại, không nhân đôi VO hay T05, không kẹt input/camera; quay lại B06 chỉ là quầy trống, không replay cinematic; kiểm CH01-EX01 không auto-unlock từ insert-shot đồng hồ.
1. Từ AP00: đọc E10a → mở GATE-CONG → tới AN01 → lấy BOARD → 101 → chọn **A ngay** mà không có OBS101A/B → G01 mở → T08/T09/T10. Không mất CH02, D=1 ở 101 B, R độc lập.
2. Cùng tuyến chọn **B ngay** → G01 mở, không phạt, không gọi là fail; kể cả không đọc STAFF/C/D/EX01; D=0.
3. Chọn **C hợp lệ**: đánh dấu đúng số 0 và ô ký chưa xác nhận, OBS101A/B đã xem, link + claim UNRELIABLE_ZERO → ký C → hậu quả đúng B và G01 mở.
4. Thử ký C sai: chỉ nhìn chăn, chỉ xem thẻ, đưa Bed3/đồng hồ làm căn cứ, kết luận người bệnh còn đây hoặc cáo buộc làm giả → không commit C; giải thích *tại sao*; có thể chuyển sang B để tiến.
5. Tuyến vòng STAFF trước 101 và 101 trước STAFF đều tới cùng quyết định; tương tác analog, E/K, Chữ lớn, lật giấy, nghiêng hoặc phím T đồng đẳng.
6. EX01 hoàn tất sau khi soi vết hằn + clock + context: D19 chỉ xuất hiện **sau ký**, trước T08. EX01 không đầy đủ: D19 không tự phát và không chặn G01.
7. Lặp A→B→C và C→A, load sau mỗi lần ký, firstDecision bất biến/currentDecision đúng, once flags không replay scare, bộ ba CH02 R LIST/SOCKETS/WHEELS còn nguyên.
8. Tắt âm / reduceMotion / mono / giảm jumpscare: mọi dấu bắt buộc, câu hỏi, F101 và seam vẫn hiểu được. Không phát visual bất ngờ khi người chơi đang đọc; não trạng người chơi không bị coi là thước đo kiểm chứng.
9. Tại bất kỳ checkpoint AP02/AP04, fail save/swap preflight phải rollback. Không mắc collider rèm/cửa/STAFF/SEAM; không khóa mềm cửa LOC04, đường về AN01 và CH02.
10. Guard narrative: CH01 không hé Bé An/C01/Khu C trong OBS101B, không có E11/E12, E10b, M hay thoại CH04-D06/CH01-D16b. Tổng evidence CH01 vẫn 1.
11. Test cue: ghi event once, ưu tiên hiệu ứng ký F101 **bất kể budget nền đã dùng hết 7**, không double-fire khi mở/đóng inspector nhanh; các scare P-SC mới chỉ được tích hợp sau khi có registration & QA.
12. TASK010 playtest mù 6 người mới và TASK026–028 review người thật **chưa được coi là pass** chỉ vì tài liệu hoàn thành.
13. **Pacing v1.2:** cộng dải min/max chính xác 18–24 phút; timer đo theo `T02/T03/T05/T06/T07/T08/T09/T10`. Phân biệt tuyến A/B nhanh với C cần suy luận; nếu 6 người mới không hiểu câu hỏi F101 hoặc cảm xúc Lan dù đi rất nhanh, tính là thất bại thiết kế chứ không là thành công về thời lượng.
14. **B03 hybrid:** Lan nhìn/đáp trước E/K nhưng không auto cinematic; sau lời chào vẫn có việc tự nhiên để làm, không freeze “đợi nút”; bỏ đi quay lại/spam E/K/skip/load không nhân thoại hoặc commit T05. Giữ cinematic theo quyết định thiết kế đã chọn. Camera không cho auto-OBS_CLOCK_0317/EX01.
15. **B04 Spatial Deduction:** thử cả nhìn so từ giường 1–3 và soi vệt sàn sau rèm; thiếu một nguồn chính thì Refuted vẫn không hợp lệ; A/B vẫn đi tiếp; trong CH02-R, `LIST/SOCKETS/WHEELS/PZ01/E12/E11` chưa hề bị ghi trước. Không thêm 4 công tắc khi Core03/04 chưa duyệt. Mute/large-print/không soi đèn phải có đường thay thế.
16. **B05 cue âm và meta journal:** directional cue không ép camera, không trùm lên thoại/đọc; trong A/B/C `ObservationRecord` và chữ viết Minh không mất; xem bản gốc phục hồi ngay không phụ thuộc save. Reload A→B→C, giảm jumpscare, caption mono, reducedMotion và chọn xem/không xem journal đều giữ G01 và first/current history.
17. **B06–B08 cảm xúc:** B06 không replay cinematic và không jumpscare tức thì; B07 chỉ seam một lần với path nhìn rõ ngay khi giảm sáng; B08 lời ghi tay chưa duyệt phải bị chặn cho đến khi đưa vào Dialogue với guard. Không có lời buộc tội, CH07 hồi tưởng, C01/Bé An lộ sớm.
18. **Behavioral & emotional playtest (đề nghị, không coi là KPI chắc chắn):** tự nhìn đồng hồ/lịch trong B03; tự bước tới nói chuyện; do dự trước rèm hoặc ký (thời gian ước lượng, không coi 3–5 giây là chuẩn phổ quát); tự quay tìm Lan tại B06; mức độ nêu đúng điều F101 chứng minh; trả lời “cảnh nào làm bạn bất an nhất và vì sao?” và “bạn muốn quay lại phòng 101 ở CH02 để kiểm cái gì?”. **Không áp đặt chỉ tiêu 4/6** từ báo cáo trước khi có dữ liệu nền; hỏi lại sau 10–15 phút để xem hình ảnh nào còn đọng lại.

**Các câu hỏi phỏng vấn playtest đề nghị:** “Vì sao bạn vào bệnh viện dù Bảy báo hoãn?”, “Bạn tin mình thực sự biết gì về giường 4?”, “Bạn chọn A/B/C vì lý do gì?”, “03:17 có nghĩa gì theo bạn và bạn dựa vào nguồn nào?”, “Cảnh đáng sợ nhất có khiến bạn quan sát/khiếu nghi nhiều hơn không?”, “Bạn có biết điều gì vẫn chưa được chứng minh?” Một câu trả lời “người chết ở 03:17 / An là người ở giường 4” ngay sau CH01 là **dấu hiệu spoiler design**, không phải chiến thắng.

## 6. Backlog triển khai gợi ý & giới hạn thay đổi

**P0 — Story/UX, không cần tạo event mới:** thay objective/hint theo câu hỏi; journal ghi “biết/suy/không biết” mà không auto đáp án; staging lại các beat trên NPC/props hiện hữu; ánh sáng quay lại phòng; làm rõ ba hành động ký A/B/C; đảm bảo có thể xem lại nguồn và phản hồi đúng thiếu căn cứ.

**P1 — Hybrid encounter B03, Spatial Deduction, biểu hiện hồ sơ (baseline v1.2); sau quyết định mới phải ưu tiên các stage BP-01..BP-07 trong tài liệu Ba lô:** refactor LanDirector/Hub để CH01-T03 chỉ khởi động Lan nhận diện in-world, còn E/K tại LAN_TALK mới bắt đầu Cinematic Dialogue Scene; bổ sung Timeline/Cinemachine shot, input/camera restore, skip/transcript và once guards (chưa triển khai). Làm prototype `P-INT04_SPATIAL_ALIGN/FLOOR_RAKE` theo `OBS101A` (không động CH02 R), P-META05 chỉ làm lớp rendering và dòng note Minh chỉ khi đã review Dialogue; thêm đối chiếu ký với observation ở B06 theo dữ liệu đã lưu; optional lựa chọn bước kế ở B08 không thêm gate; tinh chỉnh khoảng lặng và priority giữa các DREAD đã có. Giữ nguyên observation IDs và schema trừ khi migrations & test đầy đủ.

**P2 — Audio-led horror authored:** trước tiên greybox `P-SC05` có audio-led framing + bản giảm jumpscare; thêm `P-SC07` và khoảng đi bộ B07–B08 có điều kiện; chỉ thêm `P-SC02/P-SC04` khi playtest chứng minh tăng tò mò chứ không gây mệt. Dùng âm thanh có hướng, ducking an toàn và caption, không hạ toàn bộ mix xuống một giá trị dB mơ hồ; không cần quái vật mới.

**P3 — Content polish:** chỉ sửa Dialogue/prop text sau khi được duyệt trong 07/Decisions, kiểm 07/Dialogue, timeline/reveal, core narrative và test V22-Q03. Không thay trực tiếp Core 02, 04, 07 hoặc build trong PR đề xuất này.

### Impact / traceability

- **Sources đã đối chiếu để soạn đề xuất:** AGENTS.md; .agents/skills/ctcc-level/SKILL.md; ctcc-gameplay/SKILL.md; ctcc-narrative/SKILL.md; docs/agent/README.md; docs/ch01r/puzzle-dependency-graph.md; docs/ch01r/acceptance.md; docs/ch01r/human-review-checklist.md; Prototype/Assets/CTCC/Runtime/HospitalGame.cs; AmbientDreadDirector.cs; Ch01FieldObjects.cs.
- **Sources cần reviewer đối chiếu/đồng bộ trước khi chốt:** Core 01 §2; Core 02 CAN/WR/Reveal; Core 03 LOC/continuity; Core 04 CH01 B01–B08 / CH01-T02–T10; Core 05 audio; Core 06 interaction and save; Core 07 sheets Dialogue, Events, Chapter gates, Decisions, QA, Changelog.
- **Không tuyên bố:** “đã test Windows”, “đã cập nhật engine”, “đã thêm jumpscare”, “đã chốt canon”, “đã qua TASK010 hoặc expert reviews”.
- **PR chỉ chứa tài liệu đề xuất;** việc triển khai phải mở task và gắn QA/build evidence riêng. **Đặc biệt B03 hybrid:** runtime hiện tự bắt đầu encounter từ Hub sau T03, nên camera, Timeline/Cinemachine và tương tác E/K chủ động là yêu cầu mới cần code, không được mô tả là đã có. Quyết định về cameo Lan, mức jumpscare, flow F101 phải qua review narrative/gameplay và review trợ năng trước khi merge vào canon.

---

## 7. Tóm tắt đường cong điều tra để đối chiếu khi review

**B01:** Tôi tự bước vào vì một lời hẹn bị hoãn.  
**B02:** Có người ở nơi đáng ra phải trống.  
**B03:** Người ấy sống trong một mốc thời gian khác.  
**B04:** Một căn phòng có nhiều dấu vết hơn con số hồ sơ ghi nhận.  
**B05:** Tôi phải ký bằng mức độ chắc chắn mà mình thực sự có.  
**B06:** Hồ sơ thay đổi, nhưng ký ức về điều đã thấy không biến mất.  
**B07:** Tôi không thể rời khỏi việc mình chưa đối mặt.  
**B08:** Tôi sẽ đối chiếu lại thế giới thật — và chưa vội gọi suy đoán là sự thật.
---

## 8. Kiểm toán thiết kế độc lập v1.1 → quyết định v1.2

**Đầu vào:** báo cáo HTML “Kiểm Toán Thiết Kế Proposal v1.1 — Nâng Tầm Chương 1 Ca Trực Cuối Cùng Lên Chuẩn Thế Giới” do người dùng cung cấp ngày 23/11/2026. Phân loại dưới đây là **đánh giá thiết kế đối chiếu repo/contract**, không phải kết quả thực nghiệm. Điểm **8.1/10**, “top 5%”, mốc “9.2+”, so sánh trực tiếp với các game nổi tiếng và dự đoán “90% người chơi nhìn” là diễn giải của tác giả báo cáo, **không có dữ liệu playtest chứng minh**. Ghi rõ ý nào có ích, ý nào sửa và ý nào không dùng nguyên bản.

| Kiểm toán nêu | Kết luận | Thay đổi v1.2 / lý do |
| --- | --- | --- |
| “Bẫy kiểm toán viên lý trí” làm khô cảm xúc | **Chấp nhận phần lớn** | B05 giữ tính nghiêm cẩn nhưng thêm ghi chú nội tâm; B08 từ “cam kết phương pháp” sang ký ức cánh cửa chưa mở. Không biến cảm xúc thành chứng cứ hoặc kết án. |
| “Ma sát hybrid B03”: Lan ngẩng lên rồi đứng đợi E/K và cinematic quá máy móc | **Chấp nhận vấn đề, không chấp nhận bỏ hybrid** | Lan tiếp tục hành động tự nhiên, đẩy BOARD ra như cử chỉ, tự thoại khi player chủ động E/K; cinematic 60–90 s có shot tối thiểu. Free-look FPS 100% chỉ là phương án A/B test sau này. |
| Beat budget lệch số học, quá nhiều thời gian đi bộ | **Chấp nhận hoàn toàn lỗi số học; chỉnh mục tiêu** | Từ 28–38 phút *theo bảng v1.1* về **18–24 phút tuyến chính mục tiêu**, 24–34 phút khám phá; §4 cộng rõ ràng. Không khẳng định 16–20 hoặc 18–22 là “chuẩn” khi chưa có dữ liệu. |
| Scare “quá lịch sự”, dễ bị lướt qua | **Chấp nhận có điều kiện** | Audio-led framing ở B05, soundscape R/B, Uncanny Walk B07–B08; vẫn không cướp camera, không chạy jumpscare khi đọc/cinematic/save, không đo hiệu quả bằng số lần hù hoặc dB đỉnh. |
| Investigation quá thiên về chữ/UI | **Chấp nhận mạnh** | B04 thêm quan sát tương quan hình học và raking-floor dưới `OBS101A`, dùng camera/đi bộ/kéo rèm, duy trì provenance và giới hạn tiết lộ CH02. |
| Thay cinematic bằng FPS ±35°, sau 6 s tự đẩy BOARD | **Chỉ lấy phần actor agency** | Không áp góc xoay ±35°/không xóa cinematic; Lan có cử chỉ chủ động nhưng E/K vẫn cần để bắt thoại, người chơi tự cầm BOARD. |
| Bốn công tắc, một công tắc bị dán | **Chưa chấp nhận** | Repo có 4 **ổ cắm**; cần review kiến trúc điện và Core03/04 trước khi thêm công tắc. Spatial deduction triển khai trước bằng dấu sàn, rèm, so vị trí giường; không đụng điện. |
| “Acoustic vacuum về 0 dB”, hướng nhìn 90% | **Chỉnh kỹ thuật** | Ducking âm nền ngắn, cue đúng vị trí, tuân âm lượng an toàn và caption; không đồng nhất 0 dBFS với im lặng, không khẳng định tỷ lệ nhìn. |
| Sổ Trực tự xóa chữ trên phiếu sau A | **Chấp nhận cách kể, bác bỏ sửa dữ liệu gốc** | B05 dùng **overlay mực trắng trên bản sao trình bày trong B**, bảo toàn nguyên `ObservationRecord`, provenance, nhật ký viết tay, bản scan và lưu. |
| Ghi chú viết tay, câu kết chạm ký ức cánh cửa | **Chấp nhận có guard** | B05/B08 phân biệt **dữ kiện và nỗi day dứt**, chưa viết câu thoại canon khi chưa được duyệt. Không bộc lộ CH07 hoặc chấm người chọn A là xấu. |
| R/B audio ba lớp và Uncanny Walk | **Chấp nhận có điều chỉnh** | B03 ấm và sống, B06 R trống thiếu tiếng bút, B07→B08 nhịp đi bộ căng; không tắt hết đèn làm mất định hướng/trợ năng. |
| Playtest 4/6 nhìn đồng hồ, ngập ngừng 3–5 s | **Chấp nhận tín hiệu hành vi, không nhận ngưỡng như chân lý** | Ghi telemetry quan sát và câu hỏi động lực CH02, phỏng vấn không mớm câu. Chỉ điều chỉnh threshold sau playtest mù và căn cứ mẫu. |

### Tác động đến hệ thống hiện tại, không được bỏ qua

- **`LanDirector`, `HospitalGame.Hub`, `VoiceDirector`:** code hiện tự `StartEncounter` sau T03, còn hybrid v1.2 đòi nhận diện rồi E/K mới vào Timeline. **Mới là thiết kế**, cần refactor và chạy VO/skip/load QA trước khi gọi là xong.
- **`ObservationRegistry` / `HypothesisService`:** spatial task gợi mở quan sát, không sửa luật F101 hoặc các `E11/E12` CH02. R/B có cùng landmark nhưng B không được âm thầm trao chứng cứ R; QA `LOG06-Q01`.
- **`AmbientDreadDirector`, `EnvironmentalAudio`:** cue mới cần ID registry, persistent once, mix/caption và trạng thái giảm jumpscare. Overprint nhật ký là lớp presentational độc lập với model dữ liệu; không sửa schema save một cách tùy tiện.
- **`07/Dialogue` và CH04/CH07 guard:** các ghi chú nội tâm/câu kết mới là *đề xuất copy*, không được đưa vào bản thoại đang chạy trước khi kiểm narrative, V22-Q03, CH01-D16b/CH04-D06, `LOCK-V22-02`.
- **TASK010/026/027/028:** playtest mới 0/6 và specialist review còn mở theo tài liệu nghiệm thu hiện hành; không suy diễn “world-class” từ một bản phân tích HTML hay từ việc cập nhật Markdown.

**Nguyên tắc xét thành công:** người chơi vừa có thể tự nói *vì sao số 0 trên F101 thiếu căn cứ*, vừa mang về **một hình ảnh/âm thanh riêng khiến họ bất an** và có động lực tự quay lại hiện trường R ở CH02. Nếu chỉ nhớ jumpscare hoặc chỉ nhớ bảng suy luận, thiết kế chưa cân bằng.

---

## 9. Gameplay v1.3 — Ba lô điều tra 3D thay UI Sổ Trực chính

**Nơi sở hữu đặc tả:** [`docs/gameplay/backpack-physical-investigation-proposal.md`](../gameplay/backpack-physical-investigation-proposal.md). Bản này ưu tiên hợp đồng system-wide: world physical item → backpack possession → 3D inspector → world item use / placement → observation/deduction → authoritative F101 / canon gate. Mỗi beat đã có hướng tương tác mới ở phía trên; không còn “đi mở menu Sổ trực” như vòng chơi chính.

**Giữ lại một sự phân biệt quyết định:** `AN01` vẫn là một **cuốn sổ vật lý ở quầy** để Minh giở và kích `T03/T08/T10`; **ba lô** mới thay **UI Sổ Trực toàn màn hình**. Tập ghi chú phụ trong ba lô lưu `JournalNote`, timeline, observations và decisions; các đồ có thể mang theo mới là `BackpackItem`. Cánh cửa/vết bánh/ổ cắm/đồng hồ là **WorldObservation**, không phải thứ để nhét vào túi.

**Trạng thái:** Proposal v1.3, **chưa sửa code**. Các thao tác item-use, save schema v7, bag UI, B→R item reconciliation và spatial placement là yêu cầu triển khai mới. Đường code hiện `Journal()` qua Tab/J, `PhysicalInspector`, `Casework` và `Protocol.Schema = 6` phải được review từng stage; không tự ghi PASS.

**Canon QA:** `E10a` là 1 evidence seeded; `OBS101A/B` vẫn là cặp suy luận C; A/B mở G01 khi chưa nhặt gì; giấy B không export vật lý R; `LIST/SOCKETS/WHEELS/PZ01/E12/E11` chỉ được CH02 xác thực; `CH01-D16b/CH04-D06` không lộ sớm; `E08` nonexport; end states giữ nguyên. Save/load, rollback, keyboard/alternate, large print và giảm chuyển động là cổng nghiệm thu bắt buộc.
