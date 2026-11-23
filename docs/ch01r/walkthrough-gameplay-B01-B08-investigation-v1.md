# CH01 — NGƯỜI TRỞ VỀ: Walkthrough gameplay B01–B08 (Investigation-First Proposal v1.1)

> **TRẠNG THÁI: ĐỀ XUẤT THIẾT KẾ ĐỂ REVIEW, CHƯA TRIỂN KHAI / CHƯA PHÊ DUYỆT CANON.**
> Ngày: 23/11/2026. Phạm vi: CH01 B01–B08 của *Ca trực cuối cùng*, trên CH01-R hiện hành.
> Tài liệu này là một walkthrough **được chỉ đạo bởi hành động của người chơi**, không phải chuỗi cutscene thuần tuyến tính. **Ngoại lệ có chủ đích tại B03:** Lan nhận biết Minh qua Scripted Event trong thế giới, rồi người chơi chủ động kích hoạt một Cinematic Dialogue Scene ngắn, liền mạch với FPS; sau đó trở lại khám phá tự do. Cách kể mới chỉ là phương án dàn dựng; thoại chính thức vẫn do Core 07/Dialogue sở hữu. Các trigger mới gắn tiền tố **P-SC / P-INT / P-NOTE** chỉ tồn tại trong đề xuất, KHÔNG phải event ID đã đăng ký.
> Mục đích: tăng chiều sâu điều tra và nỗi sợ trong khi KHÔNG thay đổi canon, các gate và dữ liệu bằng chứng hiện hành trước khi được duyệt.

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

- **Tuyến bắt buộc:** khoảng 21–27 phút ở người chơi mới biết đọc cơ bản; 16–20 phút khi chơi lại. 
- **Tuyến khám phá sâu:** thêm 6–10 phút ở STAFF, ngăn kéo, hai hồ sơ đối chiếu, 03:17 và ghi chú cá nhân. Khoảng 27–37 phút không được biến thành yêu cầu bắt buộc của vertical slice.
- **Nấc cảm xúc:** B01 tò mò có lỗi → B02 có người đang chờ → B03 thời gian trở nên không đáng tin → B04 hồ sơ và hiện trường bất đồng → B05 trách nhiệm của chữ ký → B06 hậu quả và vắng mặt → B07 không thể bỏ đi → B08 quyết tâm kiểm lại thế giới thật.
- **Scare mix dự kiến:** 2 điểm nhấn vừa/lớn, 1 scare chính có thể tránh nếu không nhìn đúng thời điểm, 4–6 micro-dread có ngữ cảnh. Nhịp mạnh phải có 1–2 gợi báo, tối thiểu 45 giây không bị xếp chồng, sau đó khoảng lặng 20–35 giây. Không phát scare lúc người chơi đang đọc tài liệu, mở sổ, nghe thoại hoặc thao tác menu; “xê dịch sau tờ giấy” chỉ kích hoạt khi trở về 3D và khi camera có vùng nhìn an toàn. Cần duyệt mức âm, chuyển động và phản xạ bất ngờ với TASK028.

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

## B01 — Bức thư không còn lời hẹn (LOC01, R, 3–4 phút)

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

**Output:** E10a acquired + SMSRead; CH01-T02; Q1 mở, AP/rollback cổng hợp lệ. **Fallback:** đọc theo thứ tự ngược nhau không thất lạc E10a; bỏ qua thông báo không khóa cổng; mute vẫn có ánh sáng cổng/đồ vật.

## B02 — Người đứng sau ô kính (LOC01 sân → LOC02, R, 2–3 phút)

**Mục tiêu kịch:** Từ “đây là nơi bỏ hoang” sang “có ai đó đang chờ mình”. Nỗi sợ nằm ở mong muốn gặp người quen và đồng thời biết điều đó không thể đơn giản đúng.

**Walkthrough:**
1. Qua cổng là sân ướt, rãnh nước CH01_GUTTER, mái hiên, cửa kính quầy tiếp nhận xa phía trước. Đường đi tự nhiên được dẫn bằng vùng đèn ấm, **không dùng waypoint lơ lửng**. Người chơi có thể dừng nghe nước chảy hoặc nhìn sân trống.
2. Khi người chơi tới trục nhìn hợp lệ, bóng người sau kính xuất hiện 1–2 giây hoặc trở nên rõ hơn; đây nối vào **CH01-D02** và DREAD_B02_GLASS_SHADOW hiện hành. Nếu quay đi, bóng không lập tức dịch chuyển trước camera; nếu tiến sát mới thấy quầy còn người như chờ.
3. Một sự kiện nhỏ **P-SC02 “Ô kính chậm một nhịp” (đề xuất)**: nếu người chơi đứng quan sát đủ lâu, bóng bên kính quay đầu sau chuyển động thực của vật thể gây phản xạ; luôn giữ mơ hồ, không phô “ma mặt quỷ”. Có phiên bản reduceMotion: ánh sáng quầy biến đổi và tiếng giày xa thay cho animation.
4. Tiếp cận cửa/sảnh qua lối công cộng; lối khác không cho tắt qua LOC04. Nếu đứng ngoài >60 giây, ánh đèn quầy sáng hơn rất nhẹ; không spam “Hãy vào trong”.

**Mạch điều tra:** Q2 chưa thể giải; hành động chính là **chọn kiểm chứng người mình nhìn thấy**. Người chơi có thể nghi rằng đó chỉ là nhân viên bảo vệ, nhưng không bị HUD ép nhận diện.

**Scare medium có điều kiện:** Khi người chơi tự nhìn xuyên qua kính lần thứ hai, một chiếc bóng thấp thoáng qua **phía sau**, kèm hai bước chân (không bật ngay sát tai); khi xoay lại chỉ là khoảng sân trống. Giữ khả năng lờ nó đi; không ảnh hưởng gate. Thứ tự với DREAD_B02 phải được điều phối, **không phát hai cue trong cùng 45 giây**. Đây là phương án dàn cảnh, chưa có trong build.

**Output:** tới điểm AN01/LOC02; được tiếp cận sổ trực. **Handoff sang B03:** bóng người ngoài kính là setup nhìn-thấy; chỉ sau CH01-T03/R→B Lan mới nhận diện Minh bằng Scripted Event. Tuyệt đối không tự cắt sang cinematic ngay khi bóng người xuất hiện. **Fallback:** nếu người chơi sprint qua sân không nhìn kính, câu CH01-D02 và hiện diện Lan vẫn được setup tại quầy; không yêu cầu nhìn scare để tiến.

## B03 — Ca trực ở một ngày khác (LOC02, AN01, R→B, khoảng 5–6 phút; **Hybrid Cinematic Encounter**)

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

**Output:** CH01-T03/T04 (theo contract hiện hành), AP02, cue Lan/biên bản thoại đúng guard, người chơi tự kích `CH01-T05` bằng BOARD; Q2 mở, Q4 tùy chọn. **Fallback:** bỏ ngăn kéo/điện thoại vẫn hiểu mâu thuẫn thời gian; mất cinematic do skip/load vẫn có transcript/tóm tắt và BOARD; nếu người chơi bỏ đi trước khi bắt chuyện, hệ thống không tự nhảy qua nhiệm vụ.

**Trạng thái:** toàn bộ thay đổi dàn dựng hybrid và các `P-INT03_*` là **đề xuất cần triển khai + QA**; baseline hiện có Lan encounter in-world tự động, không phải cinematic.

## B04 — Phòng có giường thứ tư (HALL ↔ STAFF ↔ LOC03 B, 6–8 phút; trục điều tra chính)

**Mục tiêu kịch:** Biến một căn phòng thành **hiện trường có thể kiểm chứng**, nơi một con số hành chính xung đột với dấu sử dụng vật lý. Người chơi tự chọn lộ trình; hệ thống không chấm điều tra theo thứ tự.

**Topology:** Sau BOARD, ít nhất ba đường khám phá **hợp lệ và hội tụ**:
- Lối A: Quầy → hành lang → Phòng 101 → thấy bất nhất → quay STAFF tìm quy chế.
- Lối B: Quầy → DRAWER-DESK/đồng hồ → STAFF → hành lang → Phòng 101.
- Lối C: Quầy → hành lang, kiểm OBS101C/D trước → dấu giường 4 → STAFF bổ trợ.
Mọi đường dẫn tới F101; không khóa C theo một lối, không cần CH01-EX01 để ký.

**Walkthrough bắt buộc tối thiểu:**
1. Khi vào HALL, Minh thấy lối đi sạch quá mức so với R, mùi ẩm cũ dưới lớp thuốc sát trùng và F101 treo tại GATE-101. Bấm bảng kẹp trước khi khảo sát được phép: phiếu ghi số 0; sổ có câu hỏi “Số này dựa trên đâu?” Không cho một câu “giải đố bằng hai dấu” hiện ra như chỉ dẫn.
2. Đẩy hé DOOR-101 bằng giữ/kéo hoặc nhấn E. **Không tự mở hết**: khe cửa cung cấp âm quạt và góc nhìn thấp của rèm. Khi qua ngưỡng, commit CH01-T06 / checkpoint AP04.
3. Cầm/quan sát **OBS101A**: vết bánh giường thành vùng sử dụng, chăn gấp vội, dép lệch. Phần suy luận tối đa: “Một vị trí đã có sử dụng”; không tự kết luận “người bệnh bị chuyển đi lúc 03:17”. Nếu người chơi đã đọc F101 trước, UI cho thấy câu hỏi quan hệ nhưng không tick đáp án.
4. Kéo **CURTAIN-101** bằng tay để tìm **OBS101B** trên giá kẹp đầu giường 4. Đọc mặt trước và **mặt sau** của AS-05B: trường ký bàn giao thiếu; vết hằn có thể soi nghiêng 25°–40°, phím T hoặc bản Chữ lớn. Mặt sau chỉ có dấu **03g17 · CHƯA CHỐT SỔ**; không hiện tên An/C01/Khu C. Để rõ đạo đức của thao tác, bút tích nhòe được mô tả là “không đọc đủ” nếu chưa soi, chứ không thay bằng phán đoán.
5. Đọc **OBS101C** (giường 2 chuyển đúng quy cách, hai chữ ký/đích nội B) để biết một chuyển giao hoàn tất thường để lại dấu gì. Tài liệu này **hỗ trợ hiểu**, không bắt buộc để Refuted. Đọc **OBS101D** (giường 3, thuốc/xuất viện) để cho thấy giấy có thể khớp hiện trường nhưng không liên hệ giường 4. Nếu người chơi đưa D làm căn cứ, F101 trả “nguồn này nói về giường khác”.
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

**Output:** ít nhất đường cho OBS101A, OBS101B và khả năng nộp F101; hồ sơ đối chiếu C/D/STAFF tùy chọn; Q3 hình thành; CH01-T06. **Fallback:** nếu dùng E không kéo analog, tương tác vẫn tương đương; người chơi có thể quay lại các nguồn chưa đọc; cửa luôn có lối mở; mất nguồn âm không làm mất một dấu bắt buộc; save ở ngưỡng phục hồi toàn bộ trạng thái ngăn/rèm/giấy.

## B05 — Chữ ký làm biến dạng căn phòng (LOC03 B, 5–7 phút; choice climax)

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

**Output:** CH01-T07, G01 true, first/current/revision và variant B theo F101, không thay R. **Fallback:** thiếu A/B không khóa đường A hoặc B; lỗi save không chiếu hậu quả trước commit; có thể phục hồi phiếu & quan sát qua load.

## B06 — Người đã nhận phiếu biến mất (LOC03 → HALL → LOC02, B→R, 3–4 phút)

**Mục tiêu kịch:** Cho người chơi trải nghiệm hậu quả của điều mình **đã ký**, sau đó nhận ra người giao việc không còn ở đó. **Đối trọng với cinematic ấm áp ở B03:** B06 giữ camera FPS tự do, không dựng một cutscene Lan biến mất; chính việc người chơi tìm Lan ở đúng góc quầy trước đây tạo khoảng trống đáng sợ. Biến tuyến backtrack thành một lần **đọc lại môi trường**.

**Walkthrough:**
1. Rời phòng 101. HALL phản chiếu F101 trong chi tiết đã tồn tại: slip/hồ sơ hành lang theo lựa chọn, lời PA méo/sai nhịp **CH01-PA04**, quạt chậm (HE04). Người chơi có thể quay vào buồng để kiểm xem hậu quả có khớp quyết định hay không.
2. Một đối chiếu nhỏ **P-INT06 “Dấu ký vs căn phòng” (đề xuất, không bắt buộc):** tại cuối hành lang, trang Sổ trực đặt song song bản F101_SignedX với ghi nhận OBS101A/B cũ. Người chơi nhận ra **tài liệu ký đã đổi, quan sát cũ không bị xóa khỏi nhật ký**. Đây là một payoff điều tra: sự thật quan sát không thể bị viết lại chỉ bằng giấy.
3. Nếu hoàn tất EX01, CH01-D19 phát tại khoảng nghỉ phù hợp, không chen vào thoại/hành động; nếu không, người chơi vẫn nghe nhiễu PA theo nhịp. Hiện tượng DREAD_B06_PA_INTERFERENCE và DREAD_B06_CLOCK_TICK có ngân sách; không làm clock trở thành câu trả lời.
4. Về AN01, người chơi nhìn lại cùng quầy đã từng được dàn dựng ở B03: bút, ghế, sổ và ánh đèn còn đó nhưng **không có Lan**. **Tự tay giở sổ để bàn giao**, commit CH01-T08. B→R swap tại chỗ với preflight/rollback. Khi ngẩng lên, Lan không còn ở quầy; đồ vật lại lão hóa, ánh đèn trả về 2012; cuốn sổ vẫn ở đó. Không dùng Lan biến mất trong một cú dựng hình jump khi đang nhìn mặt bà — tránh biến Lan thành trò hù dọa rẻ.
5. Câu hỏi nhật ký được cập nhật: “Lan đã ở đây trong trải nghiệm vừa rồi; nhưng sảnh R không có ai. Mình có thể kiểm lại Phòng 101 khi đang ở R hay không?” Điều này dẫn lý do sang CH02, không phải mệnh lệnh đi nhặt E11.

**Đạo diễn sợ:** Cú sợ là **sự thiếu vắng** sau khi đã quen với Lan. Âm quạt ngừng và đèn bàn tắt chậm thay cho tiếng hú. Người chơi được đứng tại quầy 10–15 giây hoàn toàn yên, có thể xem lại lời trong transcript.

**Output:** CH01-T08 và R phục hồi, Lan vắng mặt. **Fallback:** nếu người chơi quay lại khi chưa bàn giao, được sửa F101 ở B theo contract; khi hoàn tất T08 không làm A/B/C phá OBS của R. Checkpoint trước/sau swap, không mất firstDecision.

## B07 — Cánh cổng dẫn về cùng một nơi (LOC02 → LOC01 → LOC02, R, 2–3 phút)

**Mục tiêu kịch:** Người chơi thử giải pháp hợp lý nhất của con người: rời khỏi bệnh viện. Seam buộc họ phải đối mặt điều chưa giải quyết nhưng không biến game thành escape-room vô nghĩa.

**Walkthrough:**
1. Sau T08, mục tiêu mới là **“Thử rời khỏi bệnh viện”**. Minh quay qua sân đã đi ở B01. Các landmark xe, biển cổng, đèn mái hiên và rãnh nước **không đổi vị trí**; người chơi nhận ra “đây là nơi mình vừa vào”.
2. Khi tới SEAM, tương tác vật lý kéo cổng. Trong 0,5–1 giây, mưa ngoài đường tưởng như sáng lên; âm xe máy/mưa bị hút nhỏ, cánh cổng rung như có lực tác dụng từ **phía không thấy**. Transition theo kịch bản CH01-T09/HE06 đặt người chơi lại trong không gian tương đương lối về, không tạo vô hạn vòng lặp.
3. **P-SC07 “Tiếng gõ trả lời” (đề xuất, scare mạnh thứ hai nhưng ngắn):** sau khi người chơi tự kéo cổng quá một ngưỡng, âm **một cú đập ngang khung cổng từ phía ngoài** đi cùng bóng phản xạ lướt trên kính/kim loại, không cho khuôn mặt/cánh tay danh tính. Ngay sau cú giật, mưa tắt thành nghẹt trong 1–2 giây; người chơi có thể lập tức nhìn lại cánh cổng đứng yên. Chỉ chạy **một lần**, sau khi đã cách P-SC05 tối thiểu 60 giây hoặc hạ thành tiếng kim loại nhỏ. Có caption có hướng và phiên bản reduced jumpscare.
4. Minh nói CH01-D14 đúng nơi đã có. Nếu người chơi thử cổng lần hai, chỉ có phản hồi **“Không tìm được đường ra. Trở lại sổ trực.”** Không kéo họ qua 3–4 vòng loop gây mệt hoặc gây chóng mặt.
5. Người chơi có thể quay lại ngắm thông báo tháo dỡ: ký ức về ca trực đang nằm trong một tòa nhà đã đóng. Bất nhất thời gian không biến thành “giấc mơ là giả”; vẫn chưa thể phân loại nguyên nhân.

**Ý nghĩa:** Cổng đầu B01 là lựa chọn **đi vào**; cổng B07 cho thấy **chưa thể đi ra bằng cách phủi bỏ**. Đây là đối xứng hành động, không là mini-game mới.

**Output:** CH01-T09, seamConsumed=true, objective trở về sổ. **Fallback:** seam không lặp vô hạn, không gây mắc trong collider ở cửa; người chơi luôn có đường tới AN01 sau load; disable jumpscare không đổi T09.

## B08 — Tôi sẽ không ký vào điều mình chưa biết (LOC02, R, 2–3 phút)

**Mục tiêu kịch:** CH01 kết bằng một **cam kết phương pháp**, không phải một lời khẳng định vụ án. Người chơi chuyển sang CH02 vì muốn kiểm nghiệm hiện trường R độc lập với thứ họ vừa thấy ở B.

**Walkthrough:**
1. Trở về sổ tại AN01. Trang Timeline chỉ hiển thị những sự kiện đã thực sự ghi: 18/11/2005 vụ cháy/13 người chết theo công khai; 2012 lời hẹn Bảy bị hoãn; cuộc gặp Lan trong trải nghiệm B; Phòng 101 có dấu giường 4/thẻ thiếu xác nhận nếu người chơi đã xem; kết luận F101 **của chính người chơi** và mức độ chắc chắn.
2. Trang **“Điều chưa được chứng minh”** luôn có các câu chưa trả lời: “Ai ở giường 4?”, “03:17 đánh dấu điều gì?”, “Vì sao Lan hiện diện?”, “Tại sao mình nhớ một cánh cửa từng không mở?”. Câu này được hiển thị như **câu hỏi**, không phải objective bắt người chơi đi giải ngay trong CH01.
3. **P-INT08 “Xác định bước điều tra kế tiếp” (đề xuất, tối đa một lựa chọn UI):** chọn “Kiểm tra lại Phòng 101 trong hiện thực 2012” để tạo động cơ; **không thêm cổng** vì CH01-T10 hiện đã có thao tác giở sổ và hợp đồng transition. Nếu bỏ qua interaction phụ, việc giở sổ vẫn commit T10 như hiện hành.
4. Khi giở sổ, CH01-T10; card **CHƯƠNG 02 — PHÒNG BỆNH TRỐNG**, CP04, R. Đường CH02 sử dụng **LIST + SOCKETS + WHEELS → PZ01/E12 → E11 → G02**, không được phát sẵn đáp án/hình C01.
5. Kết cảnh bằng một nhịp đơn giản: tiếng giấy lật ở quầy R, không sử dụng scare nữa. Người chơi chủ động bước sang một không gian quen mà giờ mang ý nghĩa khác.

**Payoff tầng:** Ở B05, Minh chọn cách xử lý hồ sơ. Ở B08, người chơi chọn **tiếp tục kiểm chứng dù lựa chọn vừa rồi vẫn có thể sai**. Phần thưởng là có một phương pháp đáng tin, không phải lời giải do game đọc hộ.

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

## 4. Pacing & ngân sách điều tra

| Beat | Thời lượng chính | Kiểu thử thách | Thay đổi hiểu biết | Khoảng nghỉ sau căng thẳng |
| --- | ---: | --- | --- | --- |
| B01 | 3–4' | đọc nguồn/ý chí mở cổng | Bảy hẹn–hoãn; 13 tử vong công khai | 15–25 s sau thư |
| B02 | 2–3' | tiếp cận bóng người | Có bất nhất về sự hiện diện | 20 s trước bước vào sảnh |
| B03 | 5–6' | Lan nhận diện Minh trong gameplay → tự bắt chuyện → cinematic ngắn → tự lấy BOARD; sau đó đối chiếu thời gian | Lan/2005 vs 2012; giờ chưa rõ | 10–15 s sau cinematic |
| B04 | 6–8' | phi tuyến, mở/kéo/so hai nguồn | OBS101A/B khiến số 0 đáng nghi | 45–60 s giữa scare |
| B05 | 5–7' | suy luận và ký A/B/C | Trách nhiệm chữ ký; B biến dạng | 20–35 s sau scare cao |
| B06 | 3–4' | quay lại kiểm hậu quả | R khác B; Lan vắng | 10–15 s quầy trống |
| B07 | 2–3' | thử thoát, một seam | Không thể bỏ cuộc điều tra bằng cách bước ra | 20 s trước sổ |
| B08 | 2–3' | tổng hợp và chọn tiếp tục | Chấp nhận chưa biết, sẽ đối chiếu R | Không scare |

Thời gian chỉ là **target thiết kế**; không phải kết quả playtest. Khi benchmark với CH01-R hiện tại, phải đo TTFF (time-to-first-factual-clue), thời gian tới hiểu F101, tỷ lệ chọn Unverified vì tự tin hay vì bối rối, số người khám phá STAFF và số người tự chủ động quay lại phòng.

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

**Các câu hỏi phỏng vấn playtest đề nghị:** “Vì sao bạn vào bệnh viện dù Bảy báo hoãn?”, “Bạn tin mình thực sự biết gì về giường 4?”, “Bạn chọn A/B/C vì lý do gì?”, “03:17 có nghĩa gì theo bạn và bạn dựa vào nguồn nào?”, “Cảnh đáng sợ nhất có khiến bạn quan sát/khiếu nghi nhiều hơn không?”, “Bạn có biết điều gì vẫn chưa được chứng minh?” Một câu trả lời “người chết ở 03:17 / An là người ở giường 4” ngay sau CH01 là **dấu hiệu spoiler design**, không phải chiến thắng.

## 6. Backlog triển khai gợi ý & giới hạn thay đổi

**P0 — Story/UX, không cần tạo event mới:** thay objective/hint theo câu hỏi; journal ghi “biết/suy/không biết” mà không auto đáp án; staging lại các beat trên NPC/props hiện hữu; ánh sáng quay lại phòng; làm rõ ba hành động ký A/B/C; đảm bảo có thể xem lại nguồn và phản hồi đúng thiếu căn cứ.

**P1 — Hybrid encounter B03 & hệ tương tác/ngữ cảnh:** refactor LanDirector/Hub để CH01-T03 chỉ khởi động Lan nhận diện in-world, còn E/K tại LAN_TALK mới bắt đầu Cinematic Dialogue Scene; bổ sung Timeline/Cinemachine shot, input/camera restore, skip/transcript và once guards (chưa triển khai); thêm đối chiếu ký với observation ở B06 theo dữ liệu đã lưu; optional lựa chọn bước kế ở B08 không thêm gate; tinh chỉnh khoảng lặng và priority giữa các DREAD đã có. Giữ nguyên observation IDs và schema trừ khi migrations & test đầy đủ.

**P2 — Scare authored:** thử P-SC05 trước ở greybox có spectator + reduced jumpscare, sau đó P-SC07, chỉ thêm P-SC02/P-SC04 nếu playtest chứng tỏ tăng tò mò chứ không gây mệt. Không dựng đủ bốn scare chỉ vì có tên trong tài liệu. Ưu tiên art/audio hiện có; không dựng “quái vật mới” ở CH01.

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
