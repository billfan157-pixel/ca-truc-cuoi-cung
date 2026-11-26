# Nguồn tài nguyên của bản dựng M2 Vertical Slice (kế thừa M0-R / M1-P)

## Rà soát nguồn M2-C.1

- Blender 4.5.10 LTS và MPFB 2.0.18 là công cụ tạo nội dung. MPFB dùng GPL-3.0-or-later cho mã công cụ. Dữ liệu hình học, texture và target lõi do MakeHuman công bố theo CC0 1.0. Bản khóa nguồn ghi SHA-256 của các tệp đầu vào có trên máy dựng. [Giấy phép MakeHuman](https://static.makehumancommunity.org/about/license.html) · [Gói tài nguyên chính thức](https://static.makehumancommunity.org/assets/assetpacks.html) · [Bản khóa nguồn](../tools/character_pipeline/source-lock.json)
- `ponytail01`, `short01` và `short04` có thông báo CC0 trong cả tệp lưới và tệp vật liệu. `ponytail01_diffuse.png`, `short01_diffuse.png` và `short04_diffuse.png` là các ảnh diffuse được vật liệu tương ứng tham chiếu. Phần chỉnh alpha ở mép atlas áp dụng trên bản dẫn xuất dùng trong game. Người giữ bản quyền gốc được nêu trong thông báo của asset gồm Data Collection AB, Joel Palmius và Jonas Hauquier.
- Các BVH CMU đã retarget trong build là `07_04`, `18_08` và `08_04`. Bản khóa lưu dấu vân tay của bản chuyển đổi hiện dùng. Ghi nhận nguồn và quỹ NSF ở bảng dưới. [CMU Graphics Lab](https://mocap.cs.cmu.edu/)

## Bản đồ vật liệu (PBR) và biển hiệu

Các bản đồ micro-surface nền dùng trong bộ vật liệu PBR 2048² và URP DBuffer decal tại LOC01–LOC04 (ngoại cảnh cổng viện, sảnh tiếp nhận, phòng 101, kho lưu, hành lang và phòng Cấp cứu) do ambientCG công bố dưới [CC0 1.0](https://docs.ambientcg.com/license/). Bảng màu, hoa văn gạch bông 20 × 20 cm, hệ vữa vôi–sơn dầu chân tường 1,15 m (đã khử họa tiết lặp cho M2), vết thấm ẩm/rêu mốc nhiệt đới, nhựa đường ướt, mặt tiền vữa cũ, tường gạch rêu, ghế đá mài, tán lá nhiệt đới, vải blouse trắng/quần tối/áo cháy xém, da/tóc/mắt nhân vật, biển hiệu An Sinh (`Sign_HospitalGate`, `Sign_ReceptionWindow`, `Sign_EmergencyBox`, `Sign_KhuBClosed`, `Calendar2005`, `MonitorECG`, `LanBadge`) và toàn bộ giấy tờ điều tra (`F101`, `ObsCard`, `BedList`, `Transfer`, `Drawing`, `WardRules`, `ClockB/R`) do dự án tự tạo (`tools/bake_surfaces.py`, `tools/bake_m2_surfaces.py`, `tools/bake_m2_character_maps.py`, `tools/audit_pbr.py`).

| Asset nguồn | Thành phần trong build |
| --- | --- |
| [PaintedPlaster017](https://ambientcg.com/view?id=PaintedPlaster017) | Độ nhám, vân và mảng màu vữa, mặt tiền ngoại cảnh, sơn dầu chân tường, đồ sắt sơn |
| [Terrazzo013](https://ambientcg.com/view?id=Terrazzo013) | Lõi hạt đá trong gạch bông 20 × 20 cm và ghế đá mài sân vườn tự thiết kế |
| [Fabric085](https://ambientcg.com/view?id=Fabric085) | Bề mặt nệm, gối, chăn cotton, rèm che giường và trang phục y tế M2 |
| [Metal032](https://ambientcg.com/view?id=Metal032) | Bề mặt thép không gỉ, inox, chùm chìa khóa, vỏ tủ y tế và đồng thau |
| [Tiles040](https://ambientcg.com/view?id=Tiles040) | Độ nhám và vân gạch men quầy lễ tân |

## Mô hình 3D và nhân vật Digital Human M2-C / M3.0 (Blender 4.5 LTS + MPFB2 CC0)

Toàn bộ tài sản môi trường/đạo cụ (36 tài sản M1 kế thừa + 18 tài sản kiến trúc/đạo cụ M2 cho LOC01–04 + 5 tài sản CH03 cho LOC05–07: `CTCC_ENV_ArchiveShelfRack`, `CTCC_PROP_PatientRegisterE03`, `CTCC_PROP_KnittingBagC02`, `CTCC_PROP_UnnamedBoxC12`, `CTCC_PROP_VintageRadio`) được dựng gốc trong Blender 4.5 LTS (`tools/blender_assets/`). Trong đợt nâng cấp **M2-C / M3.0 (High-Fidelity Digital Human Production & Art Lock)**, ba nhân vật chính (`CTCC_CHR_Lan`, `CTCC_CHR_AnApparition`, `CTCC_CHR_Gatekeeper`) được xây dựng và tinh chỉnh giải phẫu trực tiếp trong Blender 4.5.10 LTS (`tools/character_pipeline/`) từ hệ lưới giải phẫu người **CC0 1.0** của **MakeHuman / MPFB 2.0.18** (`hm08`), bổ sung tinh chỉnh sọ mặt người Việt (BMesh sculpt cho Lan, tỉ lệ trẻ em nam cho Bé An, lưới răng/lưỡi CC0 đủ 3 bậc LOD cho cả ba nhân vật gồm Kẻ Giữ Cửa), tóc nhiều lớp card 3D & búi thấp cài kẹp, áo blouse điều dưỡng mô phỏng vải hai tầng với cổ bẻ viền xanh theo `1895/1997/QĐ-BYT` và thẻ tên cứng bảo toàn silhouette qua `LOD0–2`, khung xương 64–65 xương (`mixamo_unity` + `ClipboardSocket_R` / `KeyRing`) kết hợp analytical two-bone leg IK khóa bước chân (`0,00 m/s` trượt trung vị), 29 BlendShape khuôn mặt/khẩu hình tiếng Việt và 3 tầng `LOD0/LOD1/LOD2`. Toàn bộ tệp `.blend` chỉnh sửa được lưu tại `ArtSource/Blender/*.blend` và xuất FBX sang `Prototype/Assets/CTCC/Art/Meshes/`. Các tài sản MakeHuman không thuộc CC0 (ví dụ tài sản mang giấy phép AGPL3 như `rehmanpolanski_hair_bun_brown`) đã bị loại trừ tuyệt đối. Không dùng nhãn hiệu hay nhận diện bệnh viện có thật (tuân thủ `AR03`).

| Nguồn bên thứ ba (M2-C) | Giấy phép / Điều khoản | Thành phần sử dụng trong build |
| --- | --- | --- |
| [MakeHuman / MPFB 2.0.18 System & Community CC0 Packs](https://github.com/makehumancommunity/mpfb2) | CC0 1.0 Universal (Public Domain) | Lưới nền giải phẫu `hm08`, kết cấu da nguồn `young_lightskinned_female_diffuse3` & `middleage_lightskinned_male_diffuse2` (đã cân chỉnh sắc độ da Việt Nam 2048²), nhãn cầu `eyes/high-poly` (đã tách lớp giác mạc đục ngoài), răng `teeth_base`, lưỡi `tongue01`, chân mày `eyebrow001` (Lan & Bé An) & `eyebrow004` (Kẻ Giữ Cửa), lông mi `eyelashes02` (Lan) & `eyelashes01` (Bé An), tóc `ponytail01` (Lan), `short01` (Bé An) và `short04` (Kẻ Giữ Cửa) |
| [CMU Graphics Lab Motion Capture Database](http://mocap.cs.cmu.edu/) (chuyển đổi BVH bởi Bruce Hahn / `cgspeed`) | Miễn phí sử dụng, sao chép và phân phối (gồm cả thương mại), yêu cầu ghi nhận nguồn | Chuyển động retarget `07_04.bvh` (`Lan_StepOut`), `18_08.bvh` (`Lan_TalkGesture`) và `08_04.bvh` (`GK_StalkWalk`). *The data used in this project was obtained from mocap.cs.cmu.edu. The database was created with funding from NSF EIA-0196217.* |

## Âm thanh, Nhạc, Giọng nói tạm (Temp VO), Lip-Sync và Shader gốc

- **26 tệp không gian/foley và motif năm nốt của Lan** do dự án tổng hợp, với 11 WAV được làm mới bằng NumPy/SciPy BSD-3-Clause. Âm thanh dùng dao động, nhiễu tạo tại chỗ và đáp ứng xung tổng hợp. Mã dựng tại `tools/master_hospital_audio.py`, `tools/bake_ambience.py` và `tools/bake_m2_audio.py`. [Giấy phép NumPy](https://github.com/numpy/numpy/blob/main/LICENSE.txt) · [Giấy phép SciPy](https://github.com/scipy/scipy/blob/main/LICENSE.txt)
- **29 WAV thoại nhân vật và loa hành lang** (24 WAV CH01–02 + 5 WAV CH03 `CH03-D01..D05`) tạo offline từ lời thoại hư cấu của dự án bằng VieNeu-TTS v3 Turbo của Phạm Nguyễn Ngọc Bảo. SDK `3.8.5`, checkpoint release `61b85e3d937fbbacb387714180e8182823512523` và giọng dựng sẵn Thái Sơn, Thục Đoan, Ngọc Trân, Đức Trí và Minh Đức. Mã, checkpoint và preset Apache-2.0. Tác giả xác nhận quyền dùng WAV thương mại và sự đồng ý của người nói hoặc chủ quyền đối với giọng preset đã phát hành. Codec OpenMOSS Nano ONNX revision `ceff0d0749bfb3fa2d61149794ec6feef0d1e1ae` Apache-2.0. [VieNeu-TTS](https://github.com/pnnbao97/VieNeu-TTS) · [Quyền sử dụng](https://huggingface.co/pnnbao-ump/VieNeu-TTS-v3-Turbo/blob/61b85e3d937fbbacb387714180e8182823512523/README.md) · [Codec OpenMOSS](https://huggingface.co/OpenMOSS-Team/MOSS-Audio-Tokenizer-Nano-ONNX)
- Bản chơi phân phối WAV đã hậu kỳ và dữ liệu khẩu hình 30 Hz của dự án (11 track nhân vật gồm `CH03-D03` & `CH03-D05`). Các chương trình dựng, mô hình AI và preset giọng dùng ngoài bản chơi. Nguồn và dấu vân tay WAV tại `docs/m2/evidence/audio/masters.json` và `docs/m3/evidence/ch03-audio-masters.json`. Bộ thoại là ứng viên M2/M3.0. Đánh giá nội dung và diễn xuất thuộc `TASK009-B`/`TASK028`.
- **9 URP 17.3 HLSL shader gốc (SRP Batcher & Forward+):** 4 shader M2-A (`RainWindowGlass.shader`, `TropicalFoliage.shader`, `WindowLight.shader`, `ApparitionSoft.shader`) + 5 shader Digital Human M2-C (`DigitalHumanSkin.shader`, `DigitalHumanEye.shader`, `DigitalHumanHair.shader`, `DigitalHumanFabric.shader`, `ApparitionHuman.shader`).

## Mỹ thuật Chương 01 (2026-11-26)

- Nhóm dự án tự tạo ống thoát mái, cửa chớp thay thế, ghế trực, kệ hồ sơ, sổ làm việc và bộ vật dụng phòng 101. Bản nguồn Blender trong `ArtSource/Blender`, FBX trong `Prototype/Assets/CTCC/Art/Meshes` và trình dựng `tools/blender_assets/build_ch01_art.py`. Đạo cụ dùng vật liệu gốc của An Sinh và các bản đồ CC0 đã ghi ở trên. Sáu mẫu này dùng trong build thương mại của dự án.
- `tools/bake_ch01_art.py` tạo năm lớp vệt cục bộ và bản đồ độ nhám mặt sân. Bản đồ thớ giấy dùng nhiễu sinh tại chỗ. Shader kính quầy và gợn mưa do dự án viết. Tiếng giọt mái dùng lại loop `Drips` đã có của An Sinh, lấy từ cùng nhóm âm thanh tổng hợp.
- Ba clip Lan ghi sổ, lắng nghe và chỉ về BOARD được tạo bằng `HospitalAuthoring.LanReception.cs` từ rig và thư viện chuyển động hiện có. Các nguồn CC0 MakeHuman và ghi nhận CMU ở trên tiếp tục áp dụng. Các clip dùng rig và tên nhân vật của dự án.

## Typeface

[Be Vietnam Pro](https://github.com/google/fonts/tree/main/ofl/bevietnampro), Be Vietnam Pro Project Authors, theo SIL Open Font License 1.1. Văn bản giấy phép ở [OFL.txt](Assets/CTCC/Art/Resources/CTCC/OFL.txt).

## Tư liệu lịch sử

Ảnh lịch sử, tiêu chuẩn `TCVN 4470:1995`, Quy chế trang phục y tế `1895/1997/QĐ-BYT`, bản vẽ JICA và tư liệu kiến trúc/khí hậu trong register `TASK023` (`docs/prototype/research.md`, `R01–R41` và `docs/m2/research-and-art-direction.md`, `R42–R50`) chỉ dùng làm tài liệu tham chiếu nghiên cứu và liên kết tới nguồn tra cứu; không nhúng ảnh bên thứ ba vào build.

## Bộ đạo cụ kỹ thuật M3.1

Bậc terrazzo, xe thuốc, tủ khô và bảng tải là thiết kế hư cấu do dự án dựng bằng Blender 4.5, với mã dựng tại `tools/blender_assets/build_m31_service_kit.py`. Tệp `.blend` và FBX cùng tên đi kèm mã nguồn. Vật liệu dùng thư viện nội bộ và các lớp PBR CC0 đã kê ở trên. Không đưa ảnh hồ sơ, nhãn bệnh viện hoặc bản vẽ nhà sản xuất vào bản chơi. Tham chiếu và các chi tiết cần duyệt được ghi trong [mặt cắt M3.1](../docs/archive/m3-history/m31/m31-world.md).

## Foley walkthrough CH01

CH01-WT-FOLEY-01 gồm PenWrite, SignKnock, KeyTurn, LatchClick, GateSlam, BagZip và ClipSnap. Các WAV mono PCM 48 kHz do dự án tổng hợp bằng [bake_ch01_walkthrough_audio.py](../tools/bake_ch01_walkthrough_audio.py), dùng NumPy và không dùng sample thu sẵn. Tệp runtime mang tiền tố CH01_ trong Assets/CTCC/Art/Resources/CTCC. Xem Core 07/Assets về build ứng viên và duyệt.

## Bộ tài sản điều tra & tương tác CH01-R

- **7 mô hình Blender 4.5 LTS tự dựng** (`tools/blender_assets/build_ch01r_props.py`, nguồn `.blend` trong `ArtSource/Blender/`, FBX trong `Prototype/Assets/CTCC/Art/Meshes/`): `CTCC_PROP_Bed4ChartHolder`, `CTCC_PROP_NursingFilingDesk`, `CTCC_PROP_InteractiveDrawer`, `CTCC_PROP_Bed4BlanketTrace`, `CTCC_ENV_StaffCrossDoor`, bản UV-mapped tinh chỉnh `CTCC_PROP_ClipboardF101` và `CTCC_PROP_BayEnvelope`. Toàn bộ sử dụng hệ vật liệu PBR CC0 đã kê ở trên và mặt giấy nội bộ.
- **Bộ giấy tờ hành chính y tế hư cấu 2005** (`tools/bake_ch01r_documents.py`): `F101` (mặt trước/mặt sau/3 bản đã ký `SignedA/B/C`), `ObsCard` (2 mặt), `Doc_Bed2Transfer` (2 mặt), `Doc_Bed3Meds` (2 mặt), `Doc_ShiftGuide` (2 mặt), `Doc_WardOverflow`, `Doc_DutyRoster`, `Doc_BayEnvelopeFront`, `Doc_BayLetter` (2 mặt), `Sign_NursingAlcove`, và góc giấy sáp màu `CH01_PR01_Corner`. Chỉ dùng phông chữ SIL OFL `Be Vietnam Pro` và nét mực/dấu cao su tổng hợp bằng Python/Pillow/NumPy; không dùng phông bản quyền hay mẫu bệnh viện có thật.
- **4 tệp SFX foley & không khí mới** (`tools/bake_ch01r_audio.py`, dấu vân tay và thông số đo tại `docs/ch01r/audio-additions.json`): `DrawerSlide.wav`, `PaperFlip.wav`, `ClockTick.wav`, `DistantSteps.wav` — tổng hợp thuần NumPy (BSD-3-Clause) 48 kHz mono 16-bit PCM, true peak ≤ −4,0 dBTP, không phụ thuộc sample bên thứ ba.

## Bộ tài sản CH01 Rebirth III — Phase 1 (Core v3.6.2 / DEC-V36-CH01-01 / DEC-VO-01)

- **2 mô hình Blender 4.5 LTS tự dựng** (`tools/blender_assets/build_ch01_rebirth3_props.py`, nguồn `.blend` trong `ArtSource/Blender/`, FBX trong `Prototype/Assets/CTCC/Art/Meshes/`): `CTCC_PROP_GateMailbox` (hộp thư sắt trụ cổng Đông tách sẵn 3 phần `MailboxBody`, `MailboxLid`, `MailboxPackageVisual`, bản lề hướng Đông mở ra ngoài) và `CTCC_PROP_MinhPhone` (điện thoại phím bấm phổ thông 2012 của Minh). Dùng hoàn toàn vật liệu PBR nội bộ và bản đồ CC0 đã kê ở trên.
- **6 kết cấu giấy tờ/màn hình B01 tách riêng** (`tools/bake_ch01r_documents.py`): `Doc_BayNote.png`, `Doc_LanLetter2005.png`, `Doc_LanLetter2005_Back.png`, `Doc_Newspaper2005.png`, `Doc_KeyCTag.png`, `Doc_PhoneScreen.png` (kèm chỉnh đồng bộ giờ SMS `22:48` trên `Doc_BayLetter.png`). Dùng phông SIL OFL `Be Vietnam Pro` và nét mực tổng hợp bằng Python/Pillow/NumPy.
- **5 tệp SFX foley B01 mới** (`tools/bake_ch01_rebirth3_audio.py`, nằm tại `Prototype/Assets/CTCC/Art/Resources/CTCC/`): `CH01_MailboxLatch.wav`, `CH01_MailboxLidCreak.wav`, `CH01_MailboxPackagePickup.wav`, `CH01_PhoneSmsChime.wav`, `CH01_PhoneRingNoAnswer.wav` — tổng hợp thuần NumPy 48 kHz mono 16-bit PCM, true peak ≤ −4,0 dBTP, không phụ thuộc sample bên thứ ba.
- **Bộ lọc VO theo `DEC-VO-01`** (`HospitalAuthoring.Lighting.cs`): bản dựng chỉ nạp vào `voiceClips` các tệp WAV có `needsVO: true` trong `docs/dialogue_silent_exploration_runtime.json`, loại trừ hoàn toàn các tệp WAV cũ của dòng `INNER_SUB_SILENT` / `CUT_TO_JOURNAL` / `UI_NON_VO` khỏi bản chạy.
