# Nguồn tài nguyên của prototype M0-R / M1

## Bản đồ vật liệu (PBR)

Các bản đồ micro-surface nền dùng trong bộ 17 vật liệu PBR 2048² và 7 URP DBuffer decal tại phòng 101 và hành lang do ambientCG công bố dưới [CC0 1.0](https://docs.ambientcg.com/license/). Bảng màu, hoa văn gạch bông 20 × 20 cm, hệ vữa vôi–sơn dầu chân tường 1,15 m, vết thấm ẩm/rêu mốc nhiệt đới, vết gỉ, dấu bánh xe giường 4, tán lá nhiệt đới, biển hiệu An Sinh và toàn bộ giấy tờ điều tra (`F101`, `ObsCard`, `BedList`, `Transfer`, `Drawing`, `WardRules`, `ClockB/R`) do dự án tự tạo (`tools/bake_surfaces.py`, `tools/audit_pbr.py`).

| Asset nguồn | Thành phần trong build |
| --- | --- |
| [PaintedPlaster017](https://ambientcg.com/view?id=PaintedPlaster017) | Độ nhám, vân và mảng màu vữa, sơn dầu chân tường, đồ sắt sơn |
| [Terrazzo013](https://ambientcg.com/view?id=Terrazzo013) | Lõi hạt đá trong gạch bông 20 × 20 cm tự thiết kế |
| [Fabric085](https://ambientcg.com/view?id=Fabric085) | Bề mặt nệm, gối, chăn cotton và rèm che giường |
| [Metal032](https://ambientcg.com/view?id=Metal032) | Bề mặt thép không gỉ, inox và đồng thau |
| [Tiles040](https://ambientcg.com/view?id=Tiles040) | Độ nhám và vân gạch men quầy trực |

## Mô hình 3D gốc (Blender 4.5 LTS)

Toàn bộ 36 tài sản 3D (kiến trúc mô-đun phòng 101 & hành lang, nội thất bệnh viện, thiết bị y tế, đạo cụ Việt Nam giai đoạn 1987–2012 và cây sân vườn phân nhánh 3D) được tạo gốc bằng Blender 4.5 LTS (`tools/blender_assets/`), lưu nguồn chỉnh sửa được tại `ArtSource/Blender/*.blend` kèm UV, phân tầng `LOD0/LOD1/LOD2` và collider riêng, xuất FBX sang `Prototype/Assets/CTCC/Art/Meshes/`. Không dùng nhãn hiệu hay nhận diện bệnh viện có thật (tuân thủ `AR03`).

## Âm thanh và Shader gốc

- 19 tệp WAV không gian và foley (`Rain`, `Roof`, `Rumble`, `Drips`, `Fan`, `Tube`, `Paper`, `Stamp`, `Slipper`, `Step`, `StepWet`, `StepCorridor`, `DoorCreak`, `DoorLatch`, `CurtainSlide`, `CabinetRoll`, `Switch`, `SwitchToB`, `SwitchToR`) được tổng hợp gốc cho dự án (`tools/bake_ambience.py`).
- 3 URP HLSL shader gốc: `RainWindowGlass.shader`, `TropicalFoliage.shader`, `WindowLight.shader`.

## Typeface

[Be Vietnam Pro](https://github.com/google/fonts/tree/main/ofl/bevietnampro), Be Vietnam Pro Project Authors, theo SIL Open Font License 1.1. Văn bản giấy phép ở [OFL.txt](Assets/CTCC/Art/Resources/CTCC/OFL.txt).

## Tư liệu lịch sử

Ảnh lịch sử, tiêu chuẩn `TCVN 4470:1995` và bản vẽ JICA trong register `TASK023` (`docs/prototype/research.md`, `R01–R41`) chỉ dùng làm tài liệu tham chiếu nghiên cứu và liên kết tới nguồn tra cứu; không nhúng ảnh bên thứ ba vào build.
