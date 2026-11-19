# Hồ sơ nghiệm thu kỹ thuật & nghệ thuật M2-C — High-Fidelity Digital Human Production

**Đối chiếu M2-C.1.** Số liệu và ảnh bên dưới thuộc build M2-C gốc. Hồ sơ M2-C.1 đo lại commit `3845fb5` ở 60 Hz thực, gồm kiểm tra bổ sung cho LOD, diễn xuất và runtime. [Hồ sơ M2-C.1](m2c1-acceptance.md)

Báo cáo tổng hợp quá trình tái dựng ba nhân vật chính của *Ca trực cuối cùng* từ nền mẫu M2-A (`49bc06837cf3ebc72348ca4a5f86575f371a108a`) sang hệ nhân vật Digital Human đạt chuẩn giải phẫu, vật liệu URP 17.3 Forward+, diễn xuất cơ thể/khuôn mặt và giữ trọn bản sắc khuôn mặt người Việt Nam / Đông Nam Á cùng bối cảnh Bệnh viện An Sinh năm 2005–2012.

- **Tài liệu thẩm định pipeline & bản quyền:** [m2c-character-audit-and-pipeline.md](m2c-character-audit-and-pipeline.md)
- **Tuyên bố nguồn gốc & giấy phép bên thứ ba:** [ThirdPartyNotices.md](../../Prototype/ThirdPartyNotices.md)
- **Nhận dạng build Windows x64 Release (Unity `6000.3.28f1`, URP 17.3 Forward+, `development=False`):** [build-identity.txt](../../output/m2c/release/build-identity.txt)
  - `Assembly-CSharp.dll` SHA-256: `fe8ac9f2432e55b518133335050fa1d99c0676a6f016f94c54f7036db085170f`
  - `AnSinh_Bootstrap.unity` SHA-256: `a6bd2d65e769720d7c12cafe3be21c60c09f70c3ab61f6b38f6744b6eeed6832`
  - `LOC02_Reception.unity` SHA-256: `56813cb0ada3bcd89eb640739b1e43fbb263d834ea088d3c55c13518075e6e36`
  - `LOC03_Ward101.unity` SHA-256: `88b89092ceb60351646bd469038dc1eb8dbc697eae1e9be73bb23fc16b47ec2c`
  - `LOC04_Emergency.unity` SHA-256: `d1da0d9f0e9751a3d7044c4eb3ea691f86e7b95a1c4740ceecafe39d796df0b7`

---

## 1. Bảng tổng hợp ba nhân vật Digital Human (Blender 4.5.10 LTS + MPFB 2.0.18 CC0)

Tất cả nhân vật đều dùng nền lưới giải phẫu `hm08` (CC0 1.0) được hiệu chỉnh nhân trắc học **100% Á Đông (`race: asian=1.0, caucasian=0.0, african=0.0`)** kèm các morph đặc trưng gương mặt người Việt Nam (sống mũi thấp mềm, đầu mũi và cánh mũi tự nhiên, gò má tròn ấm, nếp mí trên và khóe mắt Á Đông, tông da vàng ngà ấm, tròng mắt nâu-đen sẫm, chân mày dịu tự nhiên `eyebrow001` cho Lan & Bé An và `eyebrow004` cho Kẻ Giữ Cửa), mô phỏng vải thật hai tầng trong Blender (Cloth Simulation), tách lớp giác mạc đục ngoài của MakeHuman (`strip_outer_cornea_shell`), và bảo toàn 100% tệp nguồn `.blend` chỉnh sửa được.

| Nhân vật | Chiều cao | LOD0 / LOD1 / LOD2 (tris) | Xương | BlendShapes | Hoạt ảnh (CMU MoCap + Authored) | Tệp nguồn `.blend` & FBX |
| --- | ---: | --- | ---: | ---: | --- | --- |
| **CHAR002 · Trần Ngọc Lan** (Điều dưỡng 23 tuổi, 2005) | `1,600 m` | `49.812` / `31.641` / `15.478` | `65` (`mixamo_unity` + `ClipboardSocket_R`) | `29` (khép mi kín, lông mi bám bờ mi, răng/lưỡi, khẩu hình Việt) | `5`: `Lan_IdleDesk`, `Lan_StepOut` (`07_04.bvh`), `Lan_TalkGesture` (`18_08.bvh`), `Lan_Listen`, `Lan_Handover` | [CTCC_CHR_Lan.blend](../../ArtSource/Blender/CTCC_CHR_Lan.blend) · [CTCC_CHR_Lan.fbx](../../Prototype/Assets/CTCC/Art/Meshes/CTCC_CHR_Lan.fbx) |
| **CHAR007 / C01 · Bé An** (Bệnh nhi 7–8 tuổi, ký ức R) | `1,150 m` | `48.090` / `32.331` / `16.345` | `64` (`mixamo_unity`) | `29` (tỉ lệ khuôn mặt trẻ em, mi nhẹ `eyelashes01`) | `2`: `An_IdleLook`, `An_TurnFade` (xoay trục Z thẳng đứng, triệt tiêu hoàn toàn lỗi nghiêng thân M2-A) | [CTCC_CHR_AnApparition.blend](../../ArtSource/Blender/CTCC_CHR_AnApparition.blend) · [CTCC_CHR_AnApparition.fbx](../../Prototype/Assets/CTCC/Art/Meshes/CTCC_CHR_AnApparition.fbx) |
| **CHAR005 · Kẻ Giữ Cửa** (Gatekeeper, cameo R) | `1,940 m` | `35.278` / `25.896` / `11.714` | `65` (`mixamo_unity` + `KeyRing`) | `29` (khẩu hình `CH02-D05`, chân mày nghiêm uất) | `3`: `GK_IdleStill`, `GK_StalkWalk` (`08_04.bvh` + lắc chùm chìa khóa), `GK_InspectReach` | [CTCC_CHR_Gatekeeper.blend](../../ArtSource/Blender/CTCC_CHR_Gatekeeper.blend) · [CTCC_CHR_Gatekeeper.fbx](../../Prototype/Assets/CTCC/Art/Meshes/CTCC_CHR_Gatekeeper.fbx) |

### Những điểm cốt lõi được chỉnh sửa qua các vòng kiểm tra trực quan (Blender Cycles & Unity URP)
1. **Nhân trắc & sắc diện Việt Nam:** Nâng cấp toàn bộ 3 nhân vật sang hình thái Á Đông/Việt Nam đầy đủ (`asian=1.0`, sống mũi thấp mềm, gò má tròn ấm, tròng mắt nâu-đen sẫm, da vàng ngà ấm). Chuyển chân mày Lan và Bé An từ `eyebrow004` (dốc cau vào trong) sang `eyebrow001` dịu hiền để gương mặt trung tính của Lan ấm áp và Bé An ngây thơ đúng nhân vật, giữ nguyên nét uất trầm cho Kẻ Giữ Cửa.
2. **Giải phẫu mắt, lông mi & miệng:** Bóc lớp giác mạc đục ngoài (`high-poly` cornea dome) vốn che khuất mống mắt; bổ sung độ lệch lấy mẫu BVH `+8 mm` vào trong cho đầu lông mi khi truyền shape key để mi mắt khép kín 100% không bị lật mi khi chớp mắt; cân chỉnh độ mở khẩu hình nói tiếng Việt tự nhiên trong [CharacterActor.cs](../../Prototype/Assets/CTCC/Runtime/CharacterActor.cs).
3. **Tóc & trang phục đúng thời kỳ 2005:** Khử sạch viền trắng alpha và watermark góc trên phải từ kết cấu tóc nguồn `ponytail01_diffuse.png`, giảm phản xạ bạc đỉnh đầu trên shader tóc URP; dựng áo blouse trắng ngả vàng theo `1895/1997/QĐ-BYT`, thẻ tên `TRẦN NGỌC LAN` gắn ảnh chân dung kết xuất thật của Lan, giày bệt da kín mũi mượt, kẹp hồ sơ F101 3D gắn trên `ClipboardSocket_R` lật mặt giấy in hướng về Minh khi trao tay; dựng đồ bộ bệnh nhi kẻ sọc + dép tổ ong vàng cho Bé An; dựng áo khoác dài cháy xém màu tro lạnh liền mạch từ eo xuống gối + cẳng tay kín cổ tay cho Kẻ Giữ Cửa.

---

## 2. Hệ Shader URP 17.3 Forward+ & Hệ diễn xuất Runtime

Năm shader HLSL mới tuân thủ tuyệt đối SRP Batcher (`HLSLINCLUDE` `CBUFFER_START(UnityPerMaterial)`) và Forward+ (`_CLUSTER_LIGHT_LOOP`), biên dịch sạch 0 lỗi:
- [DigitalHumanSkin.shader](../../Prototype/Assets/CTCC/Art/DigitalHumanSkin.shader): RGB wrap SSS, terminator hồng cầu, back-scatter tai/cánh mũi, dual-lobe GGX sebum specular, micro-pore normal 512² giảm dần theo khoảng cách.
- [DigitalHumanEye.shader](../../Prototype/Assets/CTCC/Art/DigitalHumanEye.shader): Tự động nhận diện mống mắt hai mắt theo UV MakeHuman, khúc xạ thị sai giác mạc (IOR `1,376`), vòng viền limbal, ánh sáng tụ tiền phòng và điểm sáng ướt giác mạc.
- [DigitalHumanHair.shader](../../Prototype/Assets/CTCC/Art/DigitalHumanHair.shader): Hai pass (lõi cắt alpha đục có chiều sâu + sợi rìa mềm không cắt cứng) kết hợp phản xạ dị hướng Kajiya-Kay hai dải theo `_FlowMap`.
- [DigitalHumanFabric.shader](../../Prototype/Assets/CTCC/Art/DigitalHumanFabric.shader): Phản xạ sợi bông Charlie grazing sheen + wrap cotton diffuse cho áo blouse, quần tối, đồ cotton Bé An và áo cháy xém Kẻ Giữ Cửa.
- [ApparitionHuman.shader](../../Prototype/Assets/CTCC/Art/ApparitionHuman.shader): Depth prepass + giữ nguyên đầy đủ chất liệu da/tóc/mắt/vải thật của Bé An bên dưới độ mờ `_Alpha` và quầng Fresnel ký ức.

Hệ thống diễn xuất thời gian thực ([CharacterActor.cs](../../Prototype/Assets/CTCC/Runtime/CharacterActor.cs), [LanDirector.cs](../../Prototype/Assets/CTCC/Runtime/LanDirector.cs), [VoiceDirector.cs](../../Prototype/Assets/CTCC/Runtime/VoiceDirector.cs)):
- Căn gióng khẩu hình tiếng Việt ngoại tuyến 30 Hz ([generate_vietnamese_lipsync.py](../../tools/character_pipeline/generate_vietnamese_lipsync.py) · [biểu đồ xác minh](../../output/m2c/lipsync-verification.png)) cho 9 câu thoại (`CH01-D03/D05/D06/D07/D08/D09/D16`, `CH02-D03`, `CH02-D05`).
- Đồng bộ 29 BlendShape giữa `LOD0` và `LOD1`, mắt dẫn đầu (eye-lead gaze), vi chuyển động nhãn cầu (micro-saccade), chớp mắt theo chuyển hướng nhìn, trạng thái lắng nghe `Lan_Listen` ở `CH01-D04`, và trao kẹp hồ sơ F101 thật ở `Lan_Handover` rồi ẩn đi khi `CH01-T05` kích hoạt.

---

## 3. Thư mục bằng chứng hình ảnh & video (Blender + Unity)

### So sánh Trước / Sau (M2-A `49bc068` vs M2-C)
- [So sánh Blender (Lan toàn thân, chân dung Lan, Bé An, Kẻ Giữ Cửa)](../../output/m2c/m2c-before-after-blender.jpg)
- [So sánh Unity 6.3 URP trong game 1080p](../../output/m2c/m2c-before-after-unity.jpg)

### Video trình diễn trong Unity (CÓ ÂM THANH ĐỒNG BỘ)
- [m2c-character-performance-with-audio.mp4](../../output/m2c/m2c-character-performance-with-audio.mp4) — 19,00 giây, `1280×720` 30fps H.264 + `48 kHz` AAC (`MUS_LanMotif`, `CH01-D03/D04/D05`, `CH02-D03`, `KeyJingle`, `CH02-D05`, nền mưa `Rain.wav`), ghi trực tiếp từ bản dựng Windows x64 Release (`output/m2c/release`).

### Ảnh chụp Unity 1080p đa cự ly & Neutral Studio (bản Release)
- **Lan (LOC02 B 2005):** [Toàn cảnh quầy](../../output/m2c/release-module/loc02-lan-B.png) · [Cận mặt & thẻ tên](../../output/m2c/release-module/m2c-lan-closeup-B.png) · [Trao kẹp hồ sơ F101](../../output/m2c/release-module/m2c-lan-handover-B.png) · [Trung cảnh toàn thân](../../output/m2c/release-module/m2c-lan-medium-B.png) · [Ánh sáng Neutral Studio](../../output/m2c/release-module/m2c-lan-neutral-studio.png)
- **Bé An (LOC03 R 2012):** [Ký ức Phòng 101 cạnh dép tổ ong](../../output/m2c/release-module/m2c-an-vignette-R.png)
- **Kẻ Giữ Cửa (LOC04 R 2012):** [Cameo hành lang Cấp cứu](../../output/m2c/release-module/loc04-gatekeeper-R.png) · [Cận cảnh ngược sáng R](../../output/m2c/release-module/m2c-gatekeeper-closeup-R.png)

### Bảng kiểm định Blender Cycles (Chân dung, Wireframe, Biểu cảm 29 BlendShape, Turntable & Action MP4)
- **CHAR002 Lan:** [Portrait & Anatomy](../../output/m2c/blender-qa/CTCC_CHR_Lan/CTCC_CHR_Lan_portrait_board.jpg) · [Turnaround & Wireframe](../../output/m2c/blender-qa/CTCC_CHR_Lan/CTCC_CHR_Lan_turnaround_board.jpg) · [Expression & Viseme Sheet](../../output/m2c/blender-qa/CTCC_CHR_Lan/CTCC_CHR_Lan_expression_sheet.jpg) · [360° Turntable MP4](../../output/m2c/blender-qa/CTCC_CHR_Lan/CTCC_CHR_Lan_turntable.mp4) · [Actions MP4](../../output/m2c/blender-qa/CTCC_CHR_Lan/CTCC_CHR_Lan_actions.mp4)
- **CHAR007 Bé An:** [Portrait & Anatomy](../../output/m2c/blender-qa/CTCC_CHR_AnApparition/CTCC_CHR_AnApparition_portrait_board.jpg) · [Turnaround & Wireframe](../../output/m2c/blender-qa/CTCC_CHR_AnApparition/CTCC_CHR_AnApparition_turnaround_board.jpg) · [Expression & Viseme Sheet](../../output/m2c/blender-qa/CTCC_CHR_AnApparition/CTCC_CHR_AnApparition_expression_sheet.jpg) · [360° Turntable MP4](../../output/m2c/blender-qa/CTCC_CHR_AnApparition/CTCC_CHR_AnApparition_turntable.mp4) · [Actions MP4](../../output/m2c/blender-qa/CTCC_CHR_AnApparition/CTCC_CHR_AnApparition_actions.mp4)
- **CHAR005 Kẻ Giữ Cửa:** [Portrait & Anatomy](../../output/m2c/blender-qa/CTCC_CHR_Gatekeeper/CTCC_CHR_Gatekeeper_portrait_board.jpg) · [Turnaround & Wireframe](../../output/m2c/blender-qa/CTCC_CHR_Gatekeeper/CTCC_CHR_Gatekeeper_turnaround_board.jpg) · [Expression & Viseme Sheet](../../output/m2c/blender-qa/CTCC_CHR_Gatekeeper/CTCC_CHR_Gatekeeper_expression_sheet.jpg) · [360° Turntable MP4](../../output/m2c/blender-qa/CTCC_CHR_Gatekeeper/CTCC_CHR_Gatekeeper_turntable.mp4) · [Actions MP4](../../output/m2c/blender-qa/CTCC_CHR_Gatekeeper/CTCC_CHR_Gatekeeper_actions.mp4)

---

## 4. Kết quả kiểm thử tự động trên bản dựng Windows x64 Release

Toàn bộ hợp đồng gameplay, bản lưu, lớp R/B, cổng G01–G02, bảo vệ tiết lộ `LOCK-V22-02` / `V22-Q03`, trợ năng và điều khiển thật được giữ nguyên vẹn không hồi quy:

| Bộ kiểm thử | Kết quả | Chi tiết & Bằng chứng |
| --- | --- | --- |
| **Module QA Windows D3D12** | **PASS (14/14)** | `TASK025-A` (3 nhánh F101), `TASK025-B/T02/T03`, `T08` + `T08 deferred`, `M2-ADDITIVE`, `V22-Q03` (data & runtime guard `CH01-D16b`/`CH04-D06`), `M2A-DIALOGUE`, `M2-CHARACTERS & GI` (`Lan 29 shapes/5 clips`, `Gatekeeper 3 clips`, `An 2 clips`, `Dual LightProbes R=213/B=213`), `CH02-Q05`, `T06 keyboard`. [gameplay.txt](../../output/m2c/release-module/gameplay.txt) |
| **Live PlayerInput & Stress** | **PASS (6/6)** | Toàn bộ tuyến điều khiển bằng Input System thật qua cả cửa thoát A & B, 3 nhánh F101; ca stress 100 lượt đóng/mở giấy còn `0` mesh và `0` material rò rỉ (`251.691.704 → 251.311.057` bytes). [gameplay.txt](../../output/m2c/release-input/gameplay.txt) |
| **Negative Probe (`escape-premature`)** | **PASS (Mã thoát 1 như kỳ vọng)** | Phát hiện chính xác hành vi giấu cameo khi lượt ghi lỗi và trả mã thoát tiến trình `1`. [gameplay.txt](../../output/m2c/release-escape-probe/gameplay.txt) |

---

## 5. Đo hiệu năng trên phần cứng tham chiếu (Intel Core Ultra 7 155H / Intel Arc iGPU)

Đo bằng tuyến benchmark tự động đầy đủ (`LOC01 → LOC04`, `1920×1080`, Medium, render scale `0,88`, `dual-probes`, 4 scene additive, D3D12, `lod_groups=78`, texture streaming bật) kết hợp `PresentMon-2.6.0-x64.exe` (lưu ý: môi trường không nâng quyền Administrator nên PresentMon ghi đầy đủ `FrameTime` toàn phiên nhưng để `DisplayedTime=NA`; số liệu `Unity Wall / Main / GPU` lấy trực tiếp từ counter gốc trong build Release):

| Lượt đo | Khung mẫu | Unity Wall P50 / P95 / P99 | Unity Main P95 | Unity GPU P50 / P95 / P99 | PresentMon FrameTime P50 / P95 / P99 | Draws / Tris (P95) | Working Set đỉnh |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| **M2-A tham chiếu** (`docs/m2/acceptance.md`, VSync 1 @ 60 Hz) | `9.857` | `16,67 / 33,32 / 33,34 ms` | `6,70–6,76 ms` | `— / 16,22–17,82 / — ms` | `16,66 / 33,31 / 33,46 ms` (Display) | — | `1.273–1.281 MiB` |
| **M2-C Release (`VSync 1`, 120 Hz)** ([log](../../output/m2c/release-profile-vsync1/profiling-summary.json)) | `5.632` | **`8,34 / 16,67 / 16,67 ms`** | **`5,28 ms`** | **`8,42 / 13,31 / 14,65 ms`** | `8,68 / 17,32 / 19,48 ms` | `1.382` / `747.459` | `1.542,3 MiB` |
| **M2-C Release (`VSync 0`, mở trần)** ([log](../../output/m2c/release-profile-vsync0/profiling-summary.json)) | `6.273` | **`9,14 / 13,84 / 15,11 ms`** | **`5,09 ms`** | **`8,73 / 13,44 / 14,66 ms`** | `8,88 / 14,08 / 16,43 ms` | `1.372` / `751.969` | `1.562,0 MiB` |

### Chi phí thực đo tại các phân đoạn nhân vật (lượt `VSync 0` mở trần, P95)
- `lan-reception-B` (Lan LOD0 + Skin SSS + Eye Parallax + Hair 2-pass + Fabric Charlie + 29 BlendShapes + Vietnamese Lip-Sync): **`wall 10,75 ms | main 4,91 ms | gpu 9,86 ms | 321 draws | 311.346 tris`** (~93 FPS).
- `an-vignette-R` (Bé An 64 xương + `ApparitionHuman` 7 slot + 29 BlendShapes): **`wall 14,01 ms | main 7,28 ms | gpu 13,48 ms | 793 draws | 776.104 tris`** (~71 FPS).
- `gatekeeper-start` (Kẻ Giữ Cửa 65 xương + áo cháy xém Cloth Sim + chùm chìa khóa): **`wall 14,02 ms | main 5,37 ms | gpu 13,54 ms | 793 draws | 776.169 tris`** (~71 FPS).
- Mức tăng Working Set so với M2-A là `+261 MiB` (`1.281 → 1.542 MiB`), nằm gọn trong ngân sách RAM 16 GB và App VRAM Budget `8.337 MiB` của máy tham chiếu; toàn bộ hệ LOD0–2 và 5 shader mới giữ **GPU P95 dưới ngưỡng 16,67 ms (60 FPS)** xuyên suốt cả tuyến chơi.

---

## 6. Trạng thái nghiệm thu trung thực & Các cổng còn mở (Chưa đóng bằng tự động hóa)

Phân tách rạch ròi giữa kiểm chứng kỹ thuật đã hoàn tất và các hạng mục cần con người thực hiện trực tiếp:
- **Đã hoàn tất và có bằng chứng thực thi:** Nghiên cứu & kiểm định bản quyền (`m2c-character-audit-and-pipeline.md`), tái dựng 3 nhân vật Việt Nam trong Blender 4.5.10 LTS (`.blend` + `.fbx` + PBR 2048² + LOD0–2 + 29 BlendShapes + CMU MoCap), tích hợp 5 shader URP 17.3 Forward+ và lip-sync tiếng Việt trong Unity 6.3 LTS, 3 bộ QA Windows D3D12 (`PASS`), video Unity có âm thanh, bảng so sánh trước/sau, và đo hiệu năng trên phần cứng tham chiếu.
- **Cổng ĐANG MỞ — chưa có xác nhận con người (không tự ý đánh dấu hoàn thành):**
  1. **Thẩm định nghệ thuật / Art Direction bởi con người** đối với thần thái gương mặt, trang phục và sắc độ ánh sáng 3 nhân vật trên màn hình thực.
  2. **`TASK010` — Playtest mù với 6 người chơi mới** (yêu cầu ≥5/6 hiểu cơ chế đối chiếu mâu thuẫn, thời lượng 20–30 phút, không softlock).
  3. **`TASK026–028` — Review chuyên gia** (y tế/kiến trúc, PCCC lịch sử, và nghiệp vụ hồ sơ + thu âm diễn viên lồng tiếng thật thay cho `Temp VO` OneCore hiện hành).
