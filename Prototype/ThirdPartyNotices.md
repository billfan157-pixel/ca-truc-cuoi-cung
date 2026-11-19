# Nguồn tài nguyên của bản dựng M2 Vertical Slice (kế thừa M0-R / M1-P)

## Rà soát nguồn M2-C.1

- Blender 4.5.10 LTS và MPFB 2.0.18 là công cụ tạo nội dung. MPFB dùng GPL-3.0-or-later cho mã công cụ. Dữ liệu hình học, texture và target lõi do MakeHuman công bố theo CC0 1.0. Bản khóa nguồn ghi SHA-256 của các tệp đầu vào có trên máy dựng. [Giấy phép MakeHuman](https://static.makehumancommunity.org/about/license.html) · [Gói tài nguyên chính thức](https://static.makehumancommunity.org/assets/assetpacks.html) · [Bản khóa nguồn](../tools/character_pipeline/source-lock.json)
- `ponytail01`, `short01` và `short04` có thông báo CC0 trong cả tệp lưới và tệp vật liệu. `ponytail01_diffuse.png`, `short01_diffuse.png` và `short04_diffuse.png` là các ảnh diffuse được vật liệu tương ứng tham chiếu. Phần chỉnh alpha ở mép atlas áp dụng trên bản dẫn xuất dùng trong game. Người giữ bản quyền gốc được nêu trong thông báo của asset gồm Data Collection AB, Joel Palmius và Jonas Hauquier.
- Các BVH CMU đã retarget trong build là `07_04`, `18_08` và `08_04`. Bản khóa lưu dấu vân tay của bản chuyển đổi hiện dùng. Ghi nhận nguồn và quỹ NSF ở bảng dưới. [CMU Graphics Lab](https://mocap.cs.cmu.edu/)
- Giọng Windows `Microsoft An` hiện dùng cho kiểm thử nội bộ. Tài liệu Windows xác nhận nguồn giọng hệ thống. Quyền phân phối thương mại WAV OneCore trong game cần được xác nhận hoặc WAV được thay bằng bản thu có quyền phát hành. [Danh sách giọng Windows](https://learn.microsoft.com/en-us/uwp/api/windows.media.speechsynthesis.voiceinformation)

## Bản đồ vật liệu (PBR) và biển hiệu

Các bản đồ micro-surface nền dùng trong bộ vật liệu PBR 2048² và URP DBuffer decal tại LOC01–LOC04 (ngoại cảnh cổng viện, sảnh tiếp nhận, phòng 101, kho lưu, hành lang và phòng Cấp cứu) do ambientCG công bố dưới [CC0 1.0](https://docs.ambientcg.com/license/). Bảng màu, hoa văn gạch bông 20 × 20 cm, hệ vữa vôi–sơn dầu chân tường 1,15 m (đã khử họa tiết lặp cho M2), vết thấm ẩm/rêu mốc nhiệt đới, nhựa đường ướt, mặt tiền vữa cũ, tường gạch rêu, ghế đá mài, tán lá nhiệt đới, vải blouse trắng/quần tối/áo cháy xém, da/tóc/mắt nhân vật, biển hiệu An Sinh (`Sign_HospitalGate`, `Sign_ReceptionWindow`, `Sign_EmergencyBox`, `Sign_KhuBClosed`, `Calendar2005`, `MonitorECG`, `LanBadge`) và toàn bộ giấy tờ điều tra (`F101`, `ObsCard`, `BedList`, `Transfer`, `Drawing`, `WardRules`, `ClockB/R`) do dự án tự tạo (`tools/bake_surfaces.py`, `tools/bake_m2_surfaces.py`, `tools/bake_m2_character_maps.py`, `tools/audit_pbr.py`).

| Asset nguồn | Thành phần trong build |
| --- | --- |
| [PaintedPlaster017](https://ambientcg.com/view?id=PaintedPlaster017) | Độ nhám, vân và mảng màu vữa, mặt tiền ngoại cảnh, sơn dầu chân tường, đồ sắt sơn |
| [Terrazzo013](https://ambientcg.com/view?id=Terrazzo013) | Lõi hạt đá trong gạch bông 20 × 20 cm và ghế đá mài sân vườn tự thiết kế |
| [Fabric085](https://ambientcg.com/view?id=Fabric085) | Bề mặt nệm, gối, chăn cotton, rèm che giường và trang phục y tế M2 |
| [Metal032](https://ambientcg.com/view?id=Metal032) | Bề mặt thép không gỉ, inox, chùm chìa khóa, vỏ tủ y tế và đồng thau |
| [Tiles040](https://ambientcg.com/view?id=Tiles040) | Độ nhám và vân gạch men quầy lễ tân |

## Mô hình 3D và nhân vật Digital Human M2-C (Blender 4.5 LTS + MPFB2 CC0)

Toàn bộ tài sản môi trường/đạo cụ (36 tài sản M1 kế thừa + 18 tài sản kiến trúc/đạo cụ M2 cho LOC01–04) được dựng gốc trong Blender 4.5 LTS (`tools/blender_assets/`). Trong đợt nâng cấp **M2-C (High-Fidelity Digital Human Production)**, ba nhân vật chính (`CTCC_CHR_Lan`, `CTCC_CHR_AnApparition`, `CTCC_CHR_Gatekeeper`) được tái dựng hoàn toàn trong Blender 4.5.10 LTS (`tools/character_pipeline/`) từ hệ lưới giải phẫu người **CC0 1.0** của **MakeHuman / MPFB 2.0.18** (`hm08`), tùy biến hình thái khuôn mặt người Việt Nam/Đông Nam Á (tỉ lệ Á Đông 100%, sống mũi mềm, nếp mí, gò má và tông da Việt Nam), kết hợp mô phỏng vải thật hai tầng (Blender Cloth Simulation), khung xương 64–65 xương (`mixamo_unity` + socket đạo cụ), 29 BlendShape khuôn mặt/khẩu hình tiếng Việt và 3 tầng `LOD0/LOD1/LOD2`. Toàn bộ tệp `.blend` chỉnh sửa được lưu tại `ArtSource/Blender/*.blend` và xuất FBX sang `Prototype/Assets/CTCC/Art/Meshes/`. Các tài sản MakeHuman không thuộc CC0 (ví dụ tài sản mang giấy phép AGPL3 như `rehmanpolanski_hair_bun_brown`) đã bị loại trừ tuyệt đối. Không dùng nhãn hiệu hay nhận diện bệnh viện có thật (tuân thủ `AR03`).

| Nguồn bên thứ ba (M2-C) | Giấy phép / Điều khoản | Thành phần sử dụng trong build |
| --- | --- | --- |
| [MakeHuman / MPFB 2.0.18 System & Community CC0 Packs](https://github.com/makehumancommunity/mpfb2) | CC0 1.0 Universal (Public Domain) | Lưới nền giải phẫu `hm08`, kết cấu da nguồn `young_lightskinned_female_diffuse3` & `middleage_lightskinned_male_diffuse2` (đã cân chỉnh sắc độ da Việt Nam 2048²), nhãn cầu `eyes/high-poly` (đã tách lớp giác mạc đục ngoài), răng `teeth_base`, lưỡi `tongue01`, chân mày `eyebrow001` (Lan & Bé An) & `eyebrow004` (Kẻ Giữ Cửa), lông mi `eyelashes02` (Lan) & `eyelashes01` (Bé An), tóc `ponytail01` (Lan), `short01` (Bé An) và `short04` (Kẻ Giữ Cửa) |
| [CMU Graphics Lab Motion Capture Database](http://mocap.cs.cmu.edu/) (chuyển đổi BVH bởi Bruce Hahn / `cgspeed`) | Miễn phí sử dụng, sao chép và phân phối (gồm cả thương mại), yêu cầu ghi nhận nguồn | Chuyển động retarget `07_04.bvh` (`Lan_StepOut`), `18_08.bvh` (`Lan_TalkGesture`) và `08_04.bvh` (`GK_StalkWalk`). *The data used in this project was obtained from mocap.cs.cmu.edu. The database was created with funding from NSF EIA-0196217.* |

## Âm thanh, Nhạc, Giọng nói tạm (Temp VO), Lip-Sync và Shader gốc

- **26 tệp WAV không gian và foley gốc:** 19 tệp từ M1 (`tools/bake_ambience.py`) + 7 tệp M2 (`KeyJingle`, `TrolleyRoll`, `PhoneBuzz`, `PAStatic`, `BreakerCut`, `EmergencyHum`, `DistantThunder`) và bản nhạc chủ đề 5 nốt nguyên bản `MUS_LanMotif.wav` được tổng hợp gốc cho dự án (`tools/bake_m2_audio.py`).
- **24 tệp giọng nói tiếng Việt tạm thời (`TASK009-B Temp VO`):** `CH01-D01`..`CH01-D16`, `CH01-PA01`..`CH01-PA04`, `CH02-D01`..`CH02-D05` (tuyệt đối loại trừ `CH01-D16b` và `CH04-D06` theo `LOCK-V22-02`) được tổng hợp cục bộ trên máy phát triển bằng giọng Windows OneCore Vietnamese (`Microsoft An`) kết hợp hậu kỳ băng thông loa bệnh viện/thì thầm (`tools/bake_m2_audio.py`) và căn gióng khẩu hình tiếng Việt ngoại tuyến 30 Hz (`tools/character_pipeline/generate_vietnamese_lipsync.py`, 9 tệp JSON tại `Prototype/Assets/CTCC/Art/Resources/CTCC/LipSync/`). Chỉ dùng tạm thời trong giai đoạn M2 và sẽ thay bằng thu âm diễn viên lồng tiếng có hợp đồng bản quyền ở giai đoạn tiền phát hành (`TASK028`).
- **9 URP 17.3 HLSL shader gốc (SRP Batcher & Forward+):** 4 shader M2-A (`RainWindowGlass.shader`, `TropicalFoliage.shader`, `WindowLight.shader`, `ApparitionSoft.shader`) + 5 shader Digital Human M2-C (`DigitalHumanSkin.shader`, `DigitalHumanEye.shader`, `DigitalHumanHair.shader`, `DigitalHumanFabric.shader`, `ApparitionHuman.shader`).

## Typeface

[Be Vietnam Pro](https://github.com/google/fonts/tree/main/ofl/bevietnampro), Be Vietnam Pro Project Authors, theo SIL Open Font License 1.1. Văn bản giấy phép ở [OFL.txt](Assets/CTCC/Art/Resources/CTCC/OFL.txt).

## Tư liệu lịch sử

Ảnh lịch sử, tiêu chuẩn `TCVN 4470:1995`, Quy chế trang phục y tế `1895/1997/QĐ-BYT`, bản vẽ JICA và tư liệu kiến trúc/khí hậu trong register `TASK023` (`docs/prototype/research.md`, `R01–R41` và `docs/m2/research-and-art-direction.md`, `R42–R50`) chỉ dùng làm tài liệu tham chiếu nghiên cứu và liên kết tới nguồn tra cứu; không nhúng ảnh bên thứ ba vào build.
