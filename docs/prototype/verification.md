# Bản thử phòng 101 · Kết quả M0-R (Visual & Gameplay Reconstruction) và M1

19/11/2026 · `M0R-0.2.0-26e052568278` · Unity 6.3 LTS `6000.3.28f1` · URP 17.3 · Blender 4.5.10 LTS · Windows x64

[Điều khiển và dự án Unity](../../README.md) · [Production Database](../07_Production_Database.xlsx) · [Register nghiên cứu V/I/F/U](research.md)

Gói bản dựng M0-R đã kiểm nằm tại `output/CaTrucCuoiCung-M0R-Windows.zip` trên máy phát triển (`196.698.385 bytes`, 178 tệp, `testzip() = None`). Giải nén cả gói và mở `CaTrucCuoiCung.exe`. SHA-256 của ZIP là `b9853de07c4bbcc1d35b32b2e6848ea3a1600ab0b5512182ac149ac43789651a`.

M0-R đã tái dựng toàn diện nền tảng mỹ thuật 3D, vật liệu PBR, ánh sáng/bóng đổ URP, âm thanh không gian và tương tác nhập vai cho Phòng 101 (`LOC03`) và hành lang liền kề (`HALL`) từ 36 tài sản Blender 4.5 có thể chỉnh sửa, trong khi bảo toàn 100% mặt bằng (`layout.json`), canon Core 01–07 v2.2 và toàn bộ hệ thống gameplay M1. M2 chưa khởi động; playtest người mới và review chuyên gia `TASK026–028` giữ nguyên ở các cổng theo Roadmap.

## Tư liệu, mặt cắt, Blender 3D và engine

| Hạng mục / Task | Kết quả | Bằng chứng |
| --- | --- | --- |
| TASK022 | Duyệt mặt cắt và chi tiết kiến trúc bệnh viện công Việt Nam 1987–2005 (`TCVN 4470:1995`). Nền trệt ±0,00 m, B1 −3,40 m, phòng 101 `8 × 6 × 3,60 m`, cửa `1,20 m` có ô kính chớp lấy sáng, hành lang `3,00 m`, tỷ lệ chiếu sáng cửa sổ `21,2% ≥ 20%`. | [Mặt cắt và kiến trúc](layout.md) |
| TASK023 (M0-R) | Bổ sung nghiên cứu `R39–R41` (tiêu chuẩn `TCVN 4470:1995`, phong hóa nhiệt đới ẩm/rêu mốc/sùi muối, kích thước đồ bệnh viện giai đoạn 1987–2012) và bảng thông số sản xuất đo được. Không dùng nhãn hiệu hay bệnh viện thật (`AR03`). | [Register nghiên cứu V/I/F/U](research.md) · [Bảng mẫu 2D](loc03-reference.png) |
| M0-R Blender & PBR Pipeline | Sản xuất **36 tài sản 3D gốc** bằng Blender 4.5 LTS (`ArtSource/Blender/*.blend`, xuất FBX kèm `LOD0/LOD1/LOD2` & collider sang `Prototype/Assets/CTCC/Art/Meshes/`), **17 bộ vật liệu PBR 2048²** đã hiệu chuẩn độ nhám/normal/AO, **7 URP DBuffer Decal** (Albedo+Normal+MAOS), **3 shader HLSL URP** (`RainWindowGlass`, `TropicalFoliage`, `WindowLight`) và **19 tệp WAV** không gian/foley. | [Nguồn và giấy phép](../../Prototype/ThirdPartyNotices.md) |
| TASK029 | Giữ chốt Unity 6.3 LTS + URP 17.3 (`DEC-V2-04`) với 1 shadow cascade 2048, Additional Shadow Atlas 2048, half-res SSAO, DBuffer Decals và dual Reflection Probes 256px. | [Cách đo và DEC-V2-04](engine.md) |
| TASK002 và TASK024 | Greybox sau tái dựng giữ nguyên 100% tuyến `AP00–05`, R & 3 bố cục B, hai cửa `LOC04` và cáng `0,72 m` qua ngưỡng `1,20 m`. | [Trích log greybox](evidence/greybox.txt) |

## Gameplay và tương tác nhập vai (đã chạy trên tệp Windows M0-R)

| Task hoặc ca kiểm | Hiện tượng quan sát trên `M0R-0.2.0-26e052568278` |
| --- | --- |
| TASK003 | Toàn tuyến cổng phụ → quầy trực → Phòng 101 → kho giường → cấp cứu vượt qua đầy đủ cả ba nhánh (`Affirmed`, `Unverified`, `Refuted`). |
| TASK004-A/C · CH02-Q01/Q02, LOG06-Q01 | `E12` ghi nhận đủ 3 dấu vật lý (`LIST`, `SOCKETS`, `WHEELS`); `E11` khóa `G02` nếu thiếu phiếu chuyển và chỉ cấp `C01` khi đủ bộ 3 mảnh tranh + vòng + phiếu chuyển. |
| TASK004-B · CH01-Q01–03 | Nhánh A tăng `D=1`, nhánh B giữ `D=0`, nhánh C bắt buộc cặp `OBS101A/B`. Sửa `A→B→C` bảo toàn `firstDecision=Affirmed`, `revision=3`, `D=0`, ký lặp 10 lần không sinh bản sao. |
| TASK005 · TASK025-B · T02/T03 | Từ chối 30 lần đổi lớp ở ngưỡng cửa; thành công 30 lần tại `AP04`; chèn vật cản B gây rollback sạch về R; 10 lần tải lại không mất dữ liệu. |
| TASK006 · T08 | Lỗi I/O giả lập ngay sau flush vẫn giữ commit trước; file chính hỏng tự phục hồi từ backup hợp lệ. |
| T06 và V22-Q03 | **PASS** đầy đủ trên build Windows: Tab chuyển tiêu điểm trong bảng, Enter xác nhận, Esc đóng, lưu phím thay thế và cỡ chữ 130%. Dữ liệu CH01/CH02 không rò lời thoại CH04 (`CH01-D16b`, `CH04-D06`). |
| M0-R Immersion Upgrades | Tích hợp `PhysicalInspector` soi vật chứng 3D xoay/thu phóng bằng chuột hoặc bàn phím (kèm Bokeh DOF và sidebar không che vật chứng), `InteractableRig` (cửa gỗ đóng/mở mượt có tiếng kẽo kẹt, rèm giường 4 kéo mở vật lý, tủ y tế lăn bánh mượt), toast quan sát môi trường không chặn luồng chơi, `FirstPerson` (trọng lượng bước đi, đèn pin có trễ tay cầm & bóng đổ thật) và `EnvironmentalAudio` (3 Audio Reverb Zones + raycast low-pass cản tiếng mưa ngoài cửa sổ + bước chân theo bề mặt). |

[TASK025 và log kiểm thử](evidence/gameplay.txt) · [Trích log Windows](evidence/windows-player.txt)

## Tái dựng hình ảnh & so sánh BEFORE / AFTER (cùng tọa độ camera)

Toàn bộ ảnh BEFORE (M1 baseline dùng khối primitive) và AFTER (M0-R từ build Windows x64) được chụp tại cùng tọa độ và góc máy (`Room 101 R/B: (7.58, 0.02, 4.66), yaw 63°`; `Corridor R: (3.65, 0.02, 4.05), yaw 73°`). Video walkthrough 320 khung hình (1280 × 720, 30 fps H.264, `5,13 MB`) được ghi trực tiếp từ `CaTrucCuoiCung.exe`: [gameplay-walkthrough.mp4](evidence/gameplay-walkthrough.mp4).

- [So sánh BEFORE/AFTER · Phòng 101 lớp R (2012)](evidence/before-after-room101-R.jpg)
- [So sánh BEFORE/AFTER · Phòng 101 lớp B (2005)](evidence/before-after-room101-B.jpg)
- [So sánh BEFORE/AFTER · Hành lang tiếp cận Phòng 101](evidence/before-after-corridor-R.jpg)
- [Bảng tổng hợp 8 góc M0-R Showcase](evidence/m0r-showcase-board.jpg)
- [Ảnh cùng góc nhìn R/B](evidence/room-101-r-b.jpg) · [Hành lang](evidence/corridor-r.png)

## Hiệu năng và nghiệm thu hiện hành

[Hồ sơ M1-P](m1p-acceptance.md) thay phép đo hiệu năng hiện hành bằng dữ liệu từ Windows player 1080p, camera chính và ETW PresentMon. Số liệu M0-R đọc lại 1 pixel đồng bộ mỗi frame chỉ thuộc thử nghiệm ngoài màn hình trước đây. [Log thử nghiệm lịch sử](evidence/profile.txt).

## Đối chiếu bản đóng gói M0-R

Bản Windows x64 được tạo bằng Unity BuildPipeline từ project URP sạch đã đồng bộ byte-identical `Assets/CTCC`, `Assets/Settings`, `ProjectSettings` từ `Prototype` (tránh lỗi Bee/ILPP với đường dẫn tiếng Việt). [Trích log build Windows thành công](evidence/windows-build.txt).

| Thành phần | SHA-256 |
| --- | --- |
| `AnSinh.unity` (trùng khớp `Prototype` & build project) | `f4b3e5e9f4d1e96cbff3e31fb9d8e8a6973ee8402898ae3b02af040c94eb615c` |
| `Assembly-CSharp.dll` trong gói Windows | `26e052568278a8d427ff3357af8f8d8dd04143c8b17d0a97b129925d8e6e2db8` |
| `CaTrucCuoiCung-M0R-Windows.zip` | `b9853de07c4bbcc1d35b32b2e6848ea3a1600ab0b5512182ac149ac43789651a` |
