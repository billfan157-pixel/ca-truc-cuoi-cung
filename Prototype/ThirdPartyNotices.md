# Nguồn tài nguyên của bản dựng M2 Vertical Slice (kế thừa M0-R / M1-P)

## Bản đồ vật liệu (PBR) và biển hiệu

Các bản đồ micro-surface nền dùng trong bộ vật liệu PBR 2048² và URP DBuffer decal tại LOC01–LOC04 (ngoại cảnh cổng viện, sảnh tiếp nhận, phòng 101, kho lưu, hành lang và phòng Cấp cứu) do ambientCG công bố dưới [CC0 1.0](https://docs.ambientcg.com/license/). Bảng màu, hoa văn gạch bông 20 × 20 cm, hệ vữa vôi–sơn dầu chân tường 1,15 m (đã khử họa tiết lặp cho M2), vết thấm ẩm/rêu mốc nhiệt đới, nhựa đường ướt, mặt tiền vữa cũ, tường gạch rêu, ghế đá mài, tán lá nhiệt đới, vải blouse trắng/quần tối/áo cháy xém, da/tóc/mắt nhân vật, biển hiệu An Sinh (`Sign_HospitalGate`, `Sign_ReceptionWindow`, `Sign_EmergencyBox`, `Sign_KhuBClosed`, `Calendar2005`, `MonitorECG`, `LanBadge`) và toàn bộ giấy tờ điều tra (`F101`, `ObsCard`, `BedList`, `Transfer`, `Drawing`, `WardRules`, `ClockB/R`) do dự án tự tạo (`tools/bake_surfaces.py`, `tools/bake_m2_surfaces.py`, `tools/bake_m2_character_maps.py`, `tools/audit_pbr.py`).

| Asset nguồn | Thành phần trong build |
| --- | --- |
| [PaintedPlaster017](https://ambientcg.com/view?id=PaintedPlaster017) | Độ nhám, vân và mảng màu vữa, mặt tiền ngoại cảnh, sơn dầu chân tường, đồ sắt sơn |
| [Terrazzo013](https://ambientcg.com/view?id=Terrazzo013) | Lõi hạt đá trong gạch bông 20 × 20 cm và ghế đá mài sân vườn tự thiết kế |
| [Fabric085](https://ambientcg.com/view?id=Fabric085) | Bề mặt nệm, gối, chăn cotton, rèm che giường và trang phục y tế M2 |
| [Metal032](https://ambientcg.com/view?id=Metal032) | Bề mặt thép không gỉ, inox, chùm chìa khóa, vỏ tủ y tế và đồng thau |
| [Tiles040](https://ambientcg.com/view?id=Tiles040) | Độ nhám và vân gạch men quầy lễ tân |

## Mô hình 3D và nhân vật gốc (Blender 4.5 LTS)

Toàn bộ tài sản 3D (36 tài sản M1 kế thừa + 18 tài sản kiến trúc/đạo cụ M2 cho LOC01–04 + 3 nhân vật có khung xương, blendshape khuôn mặt và hoạt ảnh: `CTCC_CHR_Lan`, `CTCC_CHR_Gatekeeper`, `CTCC_CHR_AnApparition`) được dựng gốc trong Blender 4.5 LTS (`tools/blender_assets/`), lưu đầy đủ tệp nguồn chỉnh sửa được tại `ArtSource/Blender/*.blend` kèm UV, phân tầng `LOD0/LOD1/LOD2` và collider riêng, xuất FBX sang `Prototype/Assets/CTCC/Art/Meshes/`. Không dùng nhãn hiệu hay nhận diện bệnh viện có thật (tuân thủ `AR03`).

## Âm thanh, Nhạc, Giọng nói tạm (Temp VO) và Shader gốc

- **26 tệp WAV không gian và foley gốc:** 19 tệp từ M1 (`tools/bake_ambience.py`) + 7 tệp M2 (`KeyJingle`, `TrolleyRoll`, `PhoneBuzz`, `PAStatic`, `BreakerCut`, `EmergencyHum`, `DistantThunder`) và bản nhạc chủ đề 5 nốt nguyên bản `MUS_LanMotif.wav` được tổng hợp gốc cho dự án (`tools/bake_m2_audio.py`).
- **24 tệp giọng nói tiếng Việt tạm thời (`TASK009-B Temp VO`):** `CH01-D01`..`CH01-D16`, `CH01-PA01`..`CH01-PA04`, `CH02-D01`..`CH02-D05` (tuyệt đối loại trừ `CH01-D16b` và `CH04-D06` theo `LOCK-V22-02`) được tổng hợp cục bộ trên máy phát triển bằng giọng Windows OneCore Vietnamese (`Microsoft An`) kết hợp hậu kỳ băng thông loa bệnh viện/thì thầm (`tools/bake_m2_audio.py`) phục vụ kiểm thử dựng nhịp, lip-sync và định vị không gian cho M2 Vertical Slice; chỉ dùng tạm thời trong giai đoạn M2 và sẽ thay bằng thu âm diễn viên lồng tiếng có hợp đồng bản quyền ở giai đoạn tiền phát hành (`TASK028`).
- **4 URP HLSL shader gốc:** `RainWindowGlass.shader`, `TropicalFoliage.shader`, `WindowLight.shader`, và `ApparitionSoft.shader`.

## Typeface

[Be Vietnam Pro](https://github.com/google/fonts/tree/main/ofl/bevietnampro), Be Vietnam Pro Project Authors, theo SIL Open Font License 1.1. Văn bản giấy phép ở [OFL.txt](Assets/CTCC/Art/Resources/CTCC/OFL.txt).

## Tư liệu lịch sử

Ảnh lịch sử, tiêu chuẩn `TCVN 4470:1995`, Quy chế trang phục y tế `1895/1997/QĐ-BYT`, bản vẽ JICA và tư liệu kiến trúc/khí hậu trong register `TASK023` (`docs/prototype/research.md`, `R01–R41` và `docs/m2/research-and-art-direction.md`, `R42–R50`) chỉ dùng làm tài liệu tham chiếu nghiên cứu và liên kết tới nguồn tra cứu; không nhúng ảnh bên thứ ba vào build.
