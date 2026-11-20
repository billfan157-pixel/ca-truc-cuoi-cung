# Hồ sơ ứng viên Digital Human M2-C.1

Baseline là `3845fb556d0b833de1085dff4159d29f82257e42`. Bản Windows x64 dựng lại bằng Unity 6000.3.28f1 khớp SHA-256 của assembly và năm scene trong hồ sơ M2-C gốc. Ứng viên dùng URP 17.3 Forward+, Blender 4.5.10 LTS và ba nguồn MPFB 2.0.18 đã chỉnh trong dự án. [Baseline](evidence/m2c1/baseline-build.txt) · [Ứng viên](evidence/m2c1/candidate-build.txt)

## Trạng thái CHAR-Q01–09

| ID | Kết quả trên ứng viên | Điều kiện còn mở |
| --- | --- | --- |
| CHAR-Q01 | Blender và FBX có mắt trái/phải, tóc và 29 shape ở các LOD của cả ba nhân vật. Lan và Bé An có răng/lưỡi đơn giản hóa. Kẹp tóc Lan và chìa khóa Kẻ Giữ Cửa có mặt cả ba bậc. Lưới dùng tối đa 4 ảnh hưởng xương/đỉnh; Lan đã phát khẩu hình ở từng bậc ép trong Windows. | Ảnh LOD thấp vẫn thấy biến đổi hình cổ áo và thẻ Lan khi ép cận cảnh. Cần kiểm tra chuyển bậc tự động trong chuyển động trên cả ba nhân vật. Kẻ Giữ Cửa còn thiếu mesh răng/lưỡi riêng. |
| CHAR-Q02 | Lan được hiệu chỉnh bất đối xứng gò má và cằm trên cả ba LOD, với điều chỉnh bề mặt da/tóc trong Blender và giảm bóng dầu, tán xạ đỏ trong Unity. Bộ chân dung dùng cùng camera và đèn trung tính/đèn cứng trước sau. Giữ tỉ lệ Bé An và thân hình Kẻ Giữ Cửa. | Mảng tóc, cổ áo và phản xạ mắt còn dáng tạo hình. Cần duyệt mỹ thuật ở góc nhìn người chơi và duyệt giải phẫu trước art lock. |
| CHAR-Q03 | 9 track khóa theo WAV nguồn, đi theo sample đang phát, đóng hàm mạnh hơn ở âm môi và về trung tính cuối câu. Các nhịp diễn riêng cho CH01-D03/D05/D07 có biến đổi lông mày, gật đầu và bất đối xứng. Windows kiểm thời lượng, text, timestamp, pause/resume và dừng thoại. | Giọng OneCore tạm. Cần đánh nhãn tay mốc âm và đối chiếu theo phương ngữ trước khi đưa MFA vào pipeline. Cần bản thu, video liên tục và duyệt diễn xuất. |
| CHAR-Q04 | Lan đi hết điểm rẽ và kết hướng trên dáng lắng nghe. Kẻ Giữ Cửa giữ tốc độ truy đuổi, chặn bước vượt waypoint và reset nhịp chìa khóa khi bắt đầu. Đo diagnostic trên hai clip Blender cho thấy sai khác bước chân ở tốc độ dịch chuyển hiện hành. | Sai lệch trung vị/P95 theo trục tiến còn khoảng 1,00/2,74 m/s với Lan và 0,73/2,52 m/s với Kẻ Giữ Cửa. Ước lượng dùng xương ngón chân thấp, tốc độ dịch chuyển cố định và không có va chạm, nên cần đo đế giày thực tế trong Unity và xử lý khóa chân. |
| CHAR-Q05 | Giảm skinning ngoài tầm nhìn, đồng bộ blendshape sau Animator, tạm dừng đồ thị khi actor bị tắt và giải phóng đồ thị/vật liệu sở hữu khi hủy actor. Windows kiểm 12 vòng tắt/bật Lan và tháo/nạp lại LOC04. | Cần đo dài hạn memory và khoảng cách tải theo layout M3 thực. |
| CHAR-Q06 | 0 byte managed qua 120 lượt cập nhật đo trực tiếp của Lan với track giữ ở 0,10 giây, mỗi preset Low/Medium/High. Marker `CTCC.Character.Face` và `CTCC.Character.Gaze` có trong đường chạy. | Số 0 áp dụng cho các hàm cập nhật nhân vật được lấy mẫu. Cần trace riêng cho phát thoại thường, chi phí toàn frame và các vòng vào/ra vùng dài hạn. |
| CHAR-Q07 | Kiểm import của cả năm shader và danh sách pass. Cắt alpha tóc trước tính sáng, bỏ đọc normal vi mô ở ngoài khoảng fade và đồng bộ ngưỡng alpha hai pass của Bé An. Fade giữ vật liệu riêng theo actor, dùng chung giữa các LOD của actor. | Hai lần mở Frame Debugger hết giờ lúc Editor khởi động. Cần xác minh SRP Batch, tách thời gian GPU theo pass, đo overdraw và duyệt viền khi Bé An mờ dần. |
| CHAR-Q08 | Ca Windows kiểm tên mesh, xương/socket, clip, blendshape, bind pose, shader, clip thoại và vòng đời diễn xuất. Giữ bộ ca F101 ba nhánh, R/B, G01/G02, bảo vệ thoại CH04 và save/restore. | Cần bổ sung kiểm dài hạn và định lượng điểm tiếp xúc trong chuyển động. |
| CHAR-Q09 | Khóa file đầu vào bằng SHA-256. Xác minh thông báo CC0 của đúng ba asset tóc và liên kết atlas từ tệp vật liệu. Đã sửa điều khoản MetaHuman trong tài liệu. Cả ba FBX xuất từ Blender factory startup và nhập lại khớp hợp đồng cấu trúc của FBX dự án. | Quyền phát hành WAV OneCore và kiểm đủ bộ gói tạo asset từ đầu trên máy thứ hai vẫn mở. |

Chi tiết và dữ liệu máy đọc được ở [thư mục bằng chứng](evidence/m2c1/). Trạng thái M2 chưa đóng. Cổng TASK010 và TASK026–028 cần người chơi mới và chuyên gia tham gia. Lan vẫn là nhân vật để duyệt trước khi mở rộng sản xuất M3.

## Đo bản Windows ở 60 Hz

Intel Core Ultra 7 155H, Intel Arc iGPU driver 32.0.101.6127, RAM 16 GB, D3D12. Cả hai build chạy cùng tuyến LOC01–LOC04 ở 1920×1080 Medium, render scale 0,88 và dual-probes. Mỗi ô là khoảng min–max của ba lượt. Lượt hợp lệ cần đạt ít nhất 99% mẫu có tiêu điểm theo cả Unity và Windows. Bộ đo đã lưu các lượt bị cửa sổ Tìm kiếm nhận tiêu điểm và loại chúng khỏi so sánh.

| Chế độ | Build | GPU P95 / P99 (ms) | Main P95 (ms) | Wall P95 (ms) | DisplayedTime P99 (ms) | Working Set đỉnh (MiB) |
| --- | --- | --- | --- | --- | --- | --- |
| VSync 1 | M2-C | 10,40–11,36 / 12,40–13,06 | 7,03–7,64 | 16,68 | 16,82–16,85 | 1.535,2–1.545,9 |
| VSync 1 | M2-C.1 | 11,55–12,38 / 13,04–14,98 | 6,72–7,49 | 16,68 | 16,87–16,91 | 1.495,2–1.498,0 |
| Mở trần | M2-C | 12,43–12,84 / 13,81–14,11 | 4,31–4,41 | 12,84–13,16 | 16,67 | 1.542,1–1.567,2 |
| Mở trần | M2-C.1 | 12,45–12,60 / 13,62–13,89 | 4,40–4,43 | 12,81–13,07 | 16,67–16,68 | 1.514,6–1.529,0 |

Ứng viên đạt ngân sách GPU P95 16,67 ms trên tuyến này. P95 có VSync cao hơn baseline và số đo mở trần gần nhau. PresentMon thu được `DisplayedTime` thực ở các lượt hợp lệ. Khung dài nhất trong các pha chuyển đã chọn ở ứng viên là 33,34 ms tại lúc đổi B→R. Bộ dữ liệu lưu P50/P95/P99, số mẫu counter hợp lệ, các pha diễn xuất và đỉnh chuyển trạng thái theo từng lượt. [Bảng đầy đủ](evidence/m2c1/performance.json) · [Trace gốc](evidence/m2c1/windows-frame-traces.zip)

Ba lượt stress mở trần chạy thêm một đoạn thử ngoài sân, ép các actor thử về LOD0. Với 1, 3, 6 và 9 nhân vật trước camera, GPU P95 lần lượt là 8,19–8,35, 8,54–8,74, 9,09–9,26 và 9,67–9,86 ms. Quay camera khỏi nhóm 9 nhân vật cho 5,28–5,44 ms. Wall P95 ở mốc 9 nhìn thấy là 11,73–13,93 ms và Working Set đỉnh của toàn lượt stress là 1.550,8–1.561,5 MiB. Kết quả hỗ trợ mức 9 nhân vật cho cảnh thử này. Ngân sách M3 còn phụ thuộc ánh sáng, hình học và đường chạy của map thực tế.

## Cơ sở chỉnh nhân vật và tiếng Việt

- Nghiên cứu nhân trắc mô mềm người Việt độ tuổi 18–25 cung cấp tỉ lệ tham chiếu cho gò má, cằm, môi và mũi. Hiệu chỉnh Lan là lựa chọn cho một nhân vật hư cấu. Đối chiếu mắt dưới đèn cứng để nhận ra ranh sụn mi, tròng mắt và lớp phản chiếu. [Nghiên cứu người Việt 18–25](https://pmc.ncbi.nlm.nih.gov/articles/PMC10282534/)
- Số đo theo tuổi thay đổi theo từng vùng mặt. Hình Bé An giữ đầu, má và hàm trẻ em của mesh gốc, chỉ đổi cách hiện geometry và vật liệu qua LOD. Tập số liệu theo tuổi sau đây dùng để đọc hướng thay đổi chứ chưa cung cấp số đo cho một trẻ Việt 7–8 tuổi cụ thể. [Dữ liệu 3D từ 7 đến 17 tuổi](https://pmc.ncbi.nlm.nih.gov/articles/PMC6386244/)
- Kiểm mắt theo ba cấu phần nhìn thấy là củng mạc, giác mạc và mống mắt. Phép chụp cấu trúc mắt của Bérard và cộng sự cho thấy mức độ cá thể hóa mà mảng texture duy nhất chỉ xấp xỉ được. Kiểm vệt bóng tóc từ cả trước và sau vì độ phản xạ đổi theo hướng thớ và góc nhìn. [Hình học mắt từ phép đo](https://la.disneyresearch.com/publication/high-quality-capture-of-eyes/) · [Tán xạ sợi tóc](https://cseweb.ucsd.edu/~henrik/papers/hair/)
- Phản xạ da có thành phần khác nhau theo vùng mặt. Thêm bump tần số cao ở biên độ nhỏ và giảm bóng da Lan để đọc rõ thể tích dưới đèn cứng. Dùng ảnh ép LOD ở cùng cự ly để phát hiện sự thay dạng của mép may, cổ áo và thẻ tên. [Đo phản xạ bề mặt da](https://dash.harvard.edu/entities/publication/73120378-8d6d-6bd4-e053-0100007fdf3b) · [Neo vải và đường may trong Blender](https://docs.blender.org/manual/en/4.5/physics/cloth/settings/shape.html)
- MFA Việt v3.0.0 dùng GMM-HMM với MFCC và cao độ. Mô hình và từ điển dùng CC BY 4.0. Mô hình chung không công bố phương ngữ riêng, trong khi MFA có từ điển Thành phố Hồ Chí Minh riêng. Kiểm từ điển chung trên 9 câu hiện hành còn 4 lượt âm tiết ngoài từ điển sau chuẩn hóa. Phiên này giữ thuật toán G2P + biên độ theo WAV. [Mô hình MFA](https://mfa-models.readthedocs.io/en/latest/acoustic/Vietnamese/Vietnamese%20MFA%20acoustic%20model%20v3_0_0.html) · [Từ điển Thành phố Hồ Chí Minh](https://mfa-models.readthedocs.io/en/latest/dictionary/Vietnamese/Vietnamese%20%28Ho%20Chi%20Minh%20City%29%20MFA%20dictionary%20v3_0_0.html) · [Độ phủ từ điển](evidence/m2c1/mfa-lexicon.json)

## Dựng lại và kiểm tra

Clone vào thư mục đường dẫn ASCII cho Unity Bee. Unity và gói URP lấy phiên bản từ `Prototype/ProjectSettings/ProjectVersion.txt` và `Prototype/Packages/packages-lock.json`. Máy dựng Windows này dùng Blender 4.5.10 LTS build `6dc0b208d1b5`. Các bản `.blend` trong `ArtSource/Blender/` là nguồn chỉnh sửa, FBX là đầu ra cho Unity.

```powershell
# $blender là đường dẫn blender.exe của bản 4.5.10 LTS
& $blender -b --factory-startup --python-exit-code 1 -P tools/character_pipeline/refine_existing.py -- ArtSource/Blender/CTCC_CHR_Lan.blend output/characters-export --export-only
& $blender -b --factory-startup --python-exit-code 1 -P tools/character_pipeline/inspect_character.py -- output/characters-export/CTCC_CHR_Lan.fbx output/characters-export/lan-fbx.json --verify
./tools/build_m2.ps1 -SkipGenerate -Output output/m2c1/windows
./tools/verify_windows.ps1 -Executable output/m2c1/windows/CaTrucCuoiCung.exe -Evidence output/m2c1/test-medium -Quality Medium
```

Lặp xuất cho `CTCC_CHR_AnApparition` và `CTCC_CHR_Gatekeeper`. Duyệt bản staging trước khi chép FBX vào Unity. Khi chép FBX đã duyệt, chạy entry point Editor `CTCC.Editor.HospitalAuthoring.RefreshCharacterModels` để cập nhật material và LODGroup ở các zone. Lệnh dựng lại rig MPFB trong `build_lan.py`, `build_an.py` và `build_gatekeeper.py` xuất vào thư mục staging trống với `CTCC_CHARACTER_STAGE`. [Khóa nguồn](../../tools/character_pipeline/source-lock.json) · [Giấy phép](../../Prototype/ThirdPartyNotices.md)

## Cập nhật đóng kỹ thuật từ M3.0

Bản dựng kế tiếp M3.0 (`assembly_sha256=f4c6bc91cc13d77d89960f1bc742cc68204eee52dd19cf470732fa04c642ab7f`) đã đóng các hạng mục kỹ thuật còn mở của bảng `CHAR-Q01–09` ở trên (chi tiết và bằng chứng đầy đủ tại [Hồ sơ M3.0](../m3/m30-foundation-and-art-lock.md)):

- **CHAR-Q01 / Q02 / Q05:** Chọn ứng viên B (sculpt sọ mặt 23 tuổi, 26 lớp hair cards, cổ áo gấp theo `1895/1997/QĐ-BYT` và thẻ tên PVC phẳng được bảo vệ qua `LOD0–2`), bổ sung `GK_Teeth/Tongue_LOD0..2`, và xác minh chuyển bậc tự động `LOD0→1→2` trong chuyển động trên cả ba nhân vật.
- **CHAR-Q03 / Q06:** Chuyển toàn bộ thoại slice và CH03 (`CH03-D01..05`) sang **VieNeu v3 Turbo** (`Apache-2.0`) kèm track khẩu hình 30 Hz, xuất video diễn xuất liên tục đồng bộ âm thanh ([m30-character-performance.mp4](../m3/evidence/m30-character-performance.mp4)), và xác minh `0 B` managed allocation qua 150 khung (120 ổn định + 30 khung phát thoại thực theo `AudioSource.timeSamples`).
- **CHAR-Q04:** Sửa lỗi gắn `Animator` lệch cấp gốc FBX (`avatarSetup=0`), áp dụng 2-bone analytical leg IK + độ nén curve `0,05°` đưa sai lệch bước/đế giày thực tế trong Unity `PlayableGraph` xuống **`0,008 / 0,008 m/s`** (Lan) và **`0,009 / 0,009 m/s`** (Kẻ Giữ Cửa), đồng thời bổ sung 2-bone arm IK cho handover F101.
- **CHAR-Q07:** Xác minh cả 5 shader nhân vật đạt `import errors=0 | SRP compatibility code=0` ([shader-audit.txt](../m3/evidence/shader-audit.txt)) và sửa thứ tự pass `ApparitionDepthPrepass` (`SRPDefaultUnlit` → `UniversalForward`) kèm renderQueue `3001..3005` cho Bé An.
- **Cổng còn mở:** `TASK010` (playtest 6 người), duyệt Art Direction Lock, `TASK026–028` (review chuyên gia), và lượt đo lại PresentMon 1080p/60 Hz trên `f4c6bc9` (cần phiên desktop mở khóa, `GetForegroundWindow() != 0`).
