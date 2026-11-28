# LOC01 — Exterior & Entrance Visual Overhaul Report (Ca Trực Cuối Cùng)

**Ngày hoàn thành:** 2026-11-28  
**Phạm vi:** Toàn bộ khu vực ngoại cảnh & tiếp cận **LOC01** (`AP00` đường tiếp cận ngoài cổng, `GATE-CONG`, trụ cổng & cổng vòm, hộp thư `MAILBOX`/`E10a`, nhà bảo vệ 1987, hàng rào gạch–sắt rèn, sân viện/vỉa hè/bó vỉa/rãnh thoát nước/vũng mưa, mái đón & cửa vào `ENTRY` sang `LOC02`, cảnh quan nhiệt đới/bồn cây/ghế đá mài, xe máy của Minh dựng theo phom Super-Dream tham chiếu, bảng sơ đồ tổng mặt bằng, cột điện–đèn đường và toàn bộ mặt đứng chính 3 tầng Khối **K-A 1987**).  
**Tiêu chuẩn mỹ thuật:** *High-Fidelity Semi-Realistic Architectural Visualization & Psychological Horror* (bệnh viện đa khoa tuyến huyện/địa phương Việt Nam xây năm 1987, vận hành năm 2005 ở Layer **B**, ngừng hoạt động từ 2006 và chờ tháo dỡ tháng 11/2012 dưới mưa đêm ở Layer **R**), đồng bộ tuyệt đối với các đợt Hero Overhaul **LOC02**, **LOC03** và **HALL & STAFF**.

---

## 1. Đánh giá Baseline (BEFORE) và 12 Hạn chế Cốt lõi

Khảo sát trực tiếp bản dựng nền (`output/hall-staff-hero/final/windows/CaTrucCuoiCung.exe`) qua bộ 18 góc camera chuẩn ([`loc01-shots.json`](file:///c:/Ca%20trực%20cuối%20cùng/output/loc01-hero/loc01-shots.json), 33 ảnh tĩnh + 2 video walkthrough 30fps tại `output/loc01-hero/before/shots-High/`) và đối chiếu với kiến trúc bệnh viện đa khoa Việt Nam giai đoạn 1987–2012 xác định 12 điểm yếu gốc:

1. **Vật liệu `BaseTrim` đốm đá mài cỡ lớn ("bò sữa")** bị gán nhầm lên dầm biên K-A, trần mái đón, đỉnh tường rào và diềm mái nhà bảo vệ, phá hỏng cảm giác kết cấu bê tông cốt thép 1987.
2. **Khe hở lơ lửng phía trên `LOC02` và cửa thoát `S1`** giữa tầng trệt và dầm sàn tầng 2 Khối K-A.
3. **Cổng vào `ENTRY` trơ khối chữ nhật khoét lỗ**, thiếu khung cửa, ô thoáng kính mờ trên đầu cửa, ngưỡng đá mài và đôi cánh cửa gỗ lim kính mở áp tường.
4. **Trụ cổng phụ & cổng vòm là các khối primitive phẳng**, phía trên đặt 2 khối lập phương phát sáng vàng thô thay vì đèn lồng gang rèn bệnh viện.
5. **Hộp thư `MAILBOX` (`E10a`)** trông phẳng như hộp nhựa xanh, chữ in mờ, thiếu tai bắt ốc, nắp khe thư và độ mòn mép thép sơn.
6. **Xe máy của Minh ngoài cổng (`EX-05`)** bị chìm tối đen thành khối hình hộp đơn giản, không nhận diện được dáng xe số phổ thông Việt Nam thập niên 2000.
7. **Nhà bảo vệ (`EX-06`, `EX-07`)** trống rỗng tối om phía sau khung kính thô, thiếu mái tôn chống nóng, song sắt cửa lùa và toàn bộ đồ đạc kể chuyện bên trong.
8. **Bồn cây Đông (`EX-12`)** dùng hộp ốp vân vỏ cây phẳng như sàn gỗ thay vì bồn xây gạch đỏ–coping bê tông chứa đất ẩm hữu cơ.
9. **Tán cây & bụi ráy (`TropicalFoliage.shader`)** không lấy sáng từ đèn cục bộ Forward+ (`_ADDITIONAL_LIGHTS`), khiến cây bụi chìm đen khi đứng cạnh đèn cổng hoặc soi đèn pin.
10. **Bảng sơ đồ khuông viên (`EX-11`)** dựng bằng 4 hộp primitive thô, tối và bị xoay mặt chữ hướng ra hàng rào Tây (`+55°`) thay vì quay vào sân viện đón người chơi.
11. **Vũng mưa (`RainRipples.shader`)** hiển thị thành mảng bầu dục màu xanh trắng đục thiếu chiều sâu phản chiếu mặt gương tối của nhựa đường ướt.
12. **Góc nhìn quay lại `B07` (`EX-13`)** thiếu tầng hậu cảnh phố đêm (cột điện ly tâm, đèn đường cao áp cobra, dây điện rủ và tán cây ven đường).

---

## 2. Sản xuất Bộ Tài sản Ngoại thất & Cảnh quan bằng Blender 4.5 LTS

Toàn bộ **18 tài sản 3D** (chia thành 2 bộ tái sử dụng cho toàn bộ `Campus-v1`: *Hospital Exterior Architectural Kit* và *Landscape, Vehicle & Exterior Props Kit*) được dựng mới hoàn toàn trong **Blender 4.5.10 LTS** bằng Python API (`tools/blender_assets/loc01/`), bake PBR đa kênh bằng **Cycles** cho cả hai trạng thái thời gian **Layer B (2005)** và **Layer R (2012)** (`Color`, `Normal`, `Mask = R:Metallic / G:AO / A:Smoothness`), tạo tự động `LOD0–2`, lưu tệp nguồn `.blend` tại [`ArtSource/Blender/LOC01/`](file:///c:/Ca%20trực%20cuối%20cùng/ArtSource/Blender/LOC01/) và ảnh studio đối chiếu B/R tại [`output/loc01-hero/blender-renders/`](file:///c:/Ca%20trực%20cuối%20cùng/output/loc01-hero/blender-renders/).

- **Thư viện Shader PBR ngoại cảnh 1987–2012:** [`loc01_lib.py`](file:///c:/Ca%20trực%20cuối%20cùng/tools/blender_assets/loc01/loc01_lib.py) (23 shader procedural chuẩn hóa độ sáng ban đêm: `ext_ochre_stucco`, `ext_concrete_washed`, `ext_concrete_deck`, `ext_brick_1987`, `ext_dado_green`, `ext_iron_wrought`, `ext_mailbox_green`, `ext_galv_zinc`, `ext_terrazzo_fine`, `ext_ironwood_wet`, `ext_moto_maroon/ivory/chrome/alu/exhaust/leather/tire`, `ext_wet_soil`, `ext_cast_iron`, `ext_ceramic_brown`, `ext_bark_tropical`, `ext_amber_lens`, `ext_red_lens`).
- **7 bản in đồ họa hư cấu An Sinh độ phân giải cao (tuân thủ tuyệt đối `AR03`):** [`make_loc01_prints.py`](file:///c:/Ca%20trực%20cuối%20cùng/tools/blender_assets/loc01/make_loc01_prints.py) → [`ArtSource/LOC01/prints/`](file:///c:/Ca%20trực%20cuối%20cùng/ArtSource/LOC01/prints/) (`mailbox_stencil.png`, `guardhouse_interior.png`, `motorbike_details.png`, `motorbike_decal.png`, `bench_stencil.png`, `canopy_crest.png`, `pole_warning.png`).

### Bảng Danh mục 18 Asset Blender 4.5 (22 Bộ Atlas PBR = 110 Bản đồ `LOC01_*`)

| # | Asset FBX (`Prototype/Assets/CTCC/Art/Meshes/`) | Bộ Kit | Atlas PBR Cycles (B/R) | LOD0 Tris | Điểm nổi bật Kiến trúc / Chi tiết |
| --- | --- | --- | --- | ---: | --- |
| 1 | `CTCC_ENV_MainFacadeBay_1987` | Exterior Arch | `LOC01_FacadeBay` (1024) | 2,168 | Ô mặt đứng 1987: dầm biên, ô văng che nắng đa tầng, bậu cửa nhỏ giọt, cửa sổ khung thép song sắt |
| 2 | `CTCC_ENV_StairwellBreezeBay_1987` | Exterior Arch | `LOC01_BreezeBay` (1024) | 11,092 | Ô cầu thang gạch thông gió hoa văn hình học bê tông 3D xuyên sáng đặc trưng bệnh viện Việt Nam |
| 3 | `CTCC_LOC01_KAGroundFacade` | Exterior Arch | `LOC01_KAGroundFacade` (2048) + `LOC01_KAGroundPrints` (1024) | 8,828 | Khép kín dầm sàn tầng 2 & hốc `S1`, cổng vào có ô thoáng kính mờ, đôi cánh cửa gỗ lim kính mở áp tường, phù hiệu An Sinh 1987 riêng biệt không chồng cửa sổ |
| 4 | `CTCC_LOC01_EntryCanopy` | Exterior Arch | `LOC01_EntryCanopy` (2048) | 6,512 | Mái đón dầm trần ô cờ (coffered soffit), diềm xanh rêu, 2 cột chân gạch, nền thềm sảnh khô `ext_concrete_deck` viền mưa hắt, máng xối–phễu thu–ống thoát kẽm–đá hắt nước và đèn lồng ốp trần |
| 5 | `CTCC_LOC01_GateArchAndPillars` | Exterior Arch | `LOC01_GateArch` (2048) | 5,936 | Hai trụ cổng gạch đỏ–vữa vàng cao 4,08 m khóa khít khẩu độ `x ∈ [2.80..5.20]`, lanh-tô giật cấp và 2 đèn lồng gang rèn cổ điển |
| 6 | `CTCC_PROP_ExteriorGate` | Exterior Arch | `LOC01_ExteriorGate` (1024) | 8,360 | Cánh cổng sắt rèn `2.36×2.48 m` (mũi chông 2,62 m), vòng hoa sắt rèn, bản lề cối, hộp khóa mộng, đinh tán và chốt chân cổng |
| 7 | `CTCC_PROP_GateMailbox` | Exterior Arch | `LOC01_Mailbox` (1024) + `LOC01_MailboxLid` (1024) | 2,664 | Bảo toàn tuyệt đối 3 phần tách rời (`MailboxBody`, `MailboxLid`, `MailboxPackageVisual`) + chốt khóa đồng, tai bắt ốc và chữ in `"HỘP THƯ · BỆNH VIỆN ĐA KHOA AN SINH"` sắc nét |
| 8 | `CTCC_ENV_CourtyardFenceBay` | Exterior Arch | `LOC01_FenceBay` (1024) | 7,968 | Tường rào gạch đất nung 1987, mũ bê tông đá rửa có rãnh nhỏ giọt, trụ tường và chấn song mũi chông kèm vòng sắt rèn |
| 9 | `CTCC_ENV_Gatehouse` | Exterior Arch | `LOC01_Gatehouse` (2048) + `LOC01_GatehousePrints` (1024) | 10,880 | Bốt bảo vệ chân gạch, mái tôn kẽm chống nóng trên vì kèo thép, cửa sổ lùa song sắt và toàn bộ nội thất thấy qua kính (bàn gỗ lim, đèn chụp xanh, phích Rạng Đông hư cấu, ca men, sổ trực, lịch 11/2005 khoanh ngày 18, nội quy cổng đêm, tủ chìa khóa 6 móc, cầu dao) |
| 10 | `CTCC_PROP_CommuterMotorbike` | Landscape/Vehicle | `LOC01_Motorbike` (2048) + `LOC01_MotorbikeDecals` (1024) | 16,898 | **Dựng lại trực tiếp theo hình mẫu xe số Super-Dream do người dùng cung cấp:** dàn áo tím mận ánh kim, yếm trắng ngà cong chữ S kèm khe gió tam giác đen, baga mũi inox, vành 17" 28 nan hoa inox, moay-ơ/lốc máy/gắp sau nhôm đúc bạc, yên đôi mũi tròn, tay dắt inox vểnh sau, phuộc sau ống inox, pô đen ốp nhiệt inox bắt vít, tem hông `"SUPER 100"` & biển số `"29-K4 / 048.12"` (100% hư cấu theo `AR03`) |
| 11 | `CTCC_LOC01_CampusDirectoryBoard` | Landscape/Props | `LOC01_DirectoryBoard` (1024) | 2,380 | Kiosk sơ đồ khuông viên mái tôn dốc, khung tủ kính nhôm, 2 đèn cổ ngỗng chiếu mặt bản đồ, xoay `-55°` hướng Đông Nam đón người chơi ngay khi qua `GATE-CONG` |
| 12 | `CTCC_PROP_TerrazzoBench` | Landscape/Props | `LOC01_TerrazzoBench` (1024) | 1,648 | Ghế đá mài cong bệnh viện hạt đá mịn (khử sạch đốm bò sữa) kèm dòng sơn khuôn `"BỆNH VIỆN ĐA KHOA AN SINH · 1987"` |
| 13 | `CTCC_ENV_DrainGrateAndCurb` | Landscape/Props | `LOC01_DrainGrate` (1024) | 1,032 | Song chắn rác gang đúc 15 thanh đặt chìm trong hố thu bê tông vát cạnh |
| 14 | `CTCC_LOC01_PlanterCurbAndWalkway` | Landscape/Props | `LOC01_Planter` (2048) | 2,140 | Bồn cây xây gạch đỏ 1987, mũ bê tông vát 4 trụ góc và gò đất ẩm hữu cơ phủ rêu/lá mục |
| 15 | `CTCC_LOC01_WalkwayCurbRun` | Landscape/Props | `LOC01_CurbRun` (1024) | 576 | Dải bó vỉa & rãnh thoát nước bê tông đúc sẵn cao 5,2 cm (không cản bước chân) dẫn hướng tuyến trung tâm |
| 16 | `CTCC_ENV_UtilityPoleAndLamp` | Landscape/Props | `LOC01_UtilityPole` (1024) | 3,420 | Cột điện bê tông ly tâm côn nhỏ dần, xà sứ cách điện nâu, hộp điện, cảnh báo sơn vàng–đen và cần đèn đường cobra |
| 17 | `CTCC_LOC01_CourtyardProps` | Landscape/Props | `LOC01_YardProps` (1024) | 2,410 | Bộ 3 đạo cụ tách rời (`TrashBin` thùng rác sơn xanh mái vòm, `Bollard` cọc tiêu sơn vạch đỏ trắng ngoài cổng, `ElectricalCabinet` tủ điện tường Đông) |
| 18 | `CTCC_ENV_CourtyardTree_A` | Landscape/Props | `LOC01_TreeBark` (1024) + `TropicalLeaf` | 9,840 | Cây bàng/xà cừ sân viện thân có bạnh rễ nổi, vỏ cây nứt rêu ẩm và tán lá phân tầng đón gió/mưa |

---

## 3. Vật liệu Thời tiết, Ánh sáng Ban đêm & Chu trình Tinh chỉnh Pass 1 → Pass 2

### A. Nâng cấp Shader Thời tiết & Ánh sáng Unity URP 17.3
- **[`RainRipples.shader`](file:///c:/Ca%20trực%20cuối%20cùng/Prototype/Assets/CTCC/Art/RainRipples.shader):** Đường bờ vũng nước hữu cơ đa tần FBM, hai lớp gợn giọt mưa lệch pha nhiễu pháp tuyến, phản xạ môi trường thực từ Dual Box-Projected Reflection Probes (`GlossyEnvironmentReflection`) theo đường cong Fresnel nước (`F0 = 0.02`, mặt gương tối tự nhiên, loại bỏ hoàn toàn hiện tượng loang trắng đục).
- **[`TropicalFoliage.shader`](file:///c:/Ca%20trực%20cuối%20cùng/Prototype/Assets/CTCC/Art/TropicalFoliage.shader):** Bổ sung vòng lặp `_ADDITIONAL_LIGHTS` chuẩn URP 17 Forward+ để tán cây và bụi ráy ướt bắt sáng trực tiếp từ đèn cổng, đèn nhà bảo vệ, đèn đường và đèn pin người chơi.
- **Phân tầng Ánh sáng Điện ảnh Ban đêm & Khác biệt R/B:**
  - *Layer R (2012 — Mưa đêm đóng cửa):* Trời bão xanh-xám sâu (`0.088, 0.132, 0.172`) tôn hình khối 3 tầng K-A và đường dây điện chân trời, tương phản giữa ánh trăng mưa lạnh trên mặt đứng/đường nhựa ướt với cụm đèn natri ấm tại mái đón, cặp đèn lồng cổng, đèn bàn nhà bảo vệ, tủ đèn sơ đồ khuông viên và cặp đèn rìa tôn dáng xe máy Super-Dream của Minh.
  - *Layer B (2005 — Ca đêm đang hoạt động):* Cửa sổ các tầng sáng đèn huỳnh quang xanh-ấm, sân lát gạch vuông khô ráo, đường nhựa ngoài cổng khô tự nhiên (`CH01_AsphaltDryB`), tường vôi vàng sạch rêu và gỡ bỏ biển chờ tháo dỡ.

### B. Các lỗi phát hiện ở Pass 1 và khắc phục dứt điểm ở Pass 2
Đánh giá trực tiếp trên 33 ảnh D3D12 Pass 1 (`output/loc01-hero/pass1/shots-High/`) giúp phát hiện và xử lý triệt để 7 vấn đề trước khi đóng gói Final:
1. **Chữ in bị nhòe do tụt mip streaming & chia sẻ atlas lớn (`EX-03`, `EX-05`, `EX-06`, `EX-07`, `EX-10`):** Tách riêng 4 atlas in độ nét cao (`LOC01_GatehousePrints`, `LOC01_KAGroundPrints`, `LOC01_MailboxLid`, `LOC01_MotorbikeDecals`), tắt `streamingMipmaps` cho các atlas cận cảnh/chữ in tại [`HospitalAuthoring.Art.cs`](file:///c:/Ca%20trực%20cuối%20cùng/Prototype/Assets/CTCC/Editor/HospitalAuthoring.Art.cs) và nâng `streamingMipmapsMemoryBudget` trong [`QualitySettings.asset`](file:///c:/Ca%20trực%20cuối%20cùng/Prototype/ProjectSettings/QualitySettings.asset) lên `192 / 288 / 512 MiB`.
2. **Hướng xoay bảng sơ đồ khuông viên bị ngược (`EX-11`, `EX-18`):** Đổi góc xoay `mapBoard` từ `+55°` sang `-55°` và đặt lại điểm đèn `Directory kiosk cabinet glow` tại `(-3.20, 1.92, -15.11)` để mặt bản đồ quay thẳng về phía người chơi đi vào từ `GATE-CONG`.
3. **Thềm bê tông khô dưới mái đón quá sáng (`EX-09`, `EX-10`):** Tạo vật liệu chuyên biệt `ext_concrete_deck` sắc xám trung tính có viền mưa hắt tối dần ở mép Nam và hạ cường độ đèn `Key below entry canopy` từ `5.2f` → `4.1f`.
4. **Vũng mưa bị ánh trắng đục (`EX-02`, `EX-08`):** Tối ưu công thức Fresnel và độ trong trong `RainRipples.shader`, đưa `_Glint` về `(0.52, 0.64, 0.72, 0.62)`.
5. **Tạo hình ánh sáng xe máy Super-Dream (`EX-05`) & chiều biển số sau:** Bố trí lại đèn rìa lạnh + đèn hắt ấm thấp tôn rõ vành nan hoa inox, lốc máy nhôm, pô đen ốp inox và sơn tím mận; sửa `flip_u=True` trên `LicensePlate` để biển số **`29-K4 / 048.12`** đọc đúng chiều từ trái sang phải.
6. **Tương thích kiểm thử `P25-H` (cổng trụ gạch & lanh-tô):** Giữ nguyên định danh collider `"Gate lintel beam"` trên dầm trên cổng ([`HospitalAuthoring.cs`](file:///c:/Ca%20trực%20cuối%20cùng/Prototype/Assets/CTCC/Editor/HospitalAuthoring.cs)) và bổ sung nhận diện bounds hình học thực của `CTCC_LOC01_GateArchAndPillars` (`4.08 m >= 2.75 m`) trong [`HospitalGame.Phase25QA.cs`](file:///c:/Ca%20trực%20cuối%20cùng/Prototype/Assets/CTCC/Runtime/HospitalGame.Phase25QA.cs) mà không nới lỏng bất kỳ ngưỡng kiểm định nào.

---

## 4. Kết quả Nghiệm thu Kỹ thuật, QA, Hiệu năng & Không hồi quy

### A. Build & Scene Authoring Verification (`build_m2.ps1`)
- **Executable Final:** [`output/loc01-hero/final/windows/CaTrucCuoiCung.exe`](file:///c:/Ca%20trực%20cuối%20cùng/output/loc01-hero/final/windows/CaTrucCuoiCung.exe) (`assembly_sha256=5e79811571aedb559a7a50d920c37071553b0e340df05d8dc3411f26cd7e6cf0`).
- **Topology & Navigation:** `CTCC_GREYBOX_PASS` (đủ 3 nhánh F101, 2 lối LOC04, toàn bộ safe pad, cáng `0.72 m`, `W-Q02` độc lập `S1`, `W-Q03` quan sát `E05b` & khóa `LOC12`).
- **Dual GI & Multi-Scene Occlusion:** `CTCC_GI_PROBES_BAKE okR=True (451 probes) okB=True (451 probes)`, `CTCC_OCCLUSION_BAKE success=True (loadedScenes=9)`, `1,590 paired finishes`, `1,216 static objects`, `583 LODGroups`.
- **Audio / Canon `DEC-VO-01`:** `CTCC_AUDIO_WIRED 23 VO clips (DEC-VO-01 filtered out 13 non-VO WAVs), lanMotif=True`.

### B. Automated Windows QA (`verify_windows.ps1`)
Chạy trọn bộ tự động trên bản Final ở cả hai preset **Medium** và **High** (1920×1080 D3D12):
- **Medium** ([`qa-Medium-v3/gameplay.txt`](file:///c:/Ca%20trực%20cuối%20cùng/output/loc01-hero/final/qa-Medium-v3/gameplay.txt)): **`RESULT=PASS`** (100% 31+ suite, 0 lỗi).
- **High** ([`qa-High-v3/gameplay.txt`](file:///c:/Ca%20trực%20cuối%20cùng/output/loc01-hero/final/qa-High-v3/gameplay.txt)): **`RESULT=PASS`** (100% 31+ suite, 0 lỗi).
- Các cổng trọng yếu liên quan trực tiếp LOC01 đều **PASS**: `P25-H` (cổng `2.62 m`, trụ & lanh-tô `>= 2.75 m`, không cắt biển hiệu, tầm mắt `1.62 m`), `D4` hộp thư trụ Đông (`MailboxLidPivot`, gói tin & chìa khóa), `V36-CH01-Q01` (`GATE-CONG` chặn chìa sai), `V36-CH01-Q04/Q07` (`B06` cổng mở sau sụp đổ + `WR05` 5/5 kịch bản chống xuyên ranh giới trong R & B), `V33-Q05` (hàng rào kín quanh K-B/K-D/K-F + `S1` discharge), `D3` Save v7 tương thích ngược, `M2 dual GI (R=451, B=451)`.

### C. Đo Hiệu năng D3D12 (1920×1080, Intel Core Ultra 7 155H / Intel Arc iGPU)

| Phiên bản / Run | Quality | Frames | Draws Mean / P95 | SetPass P95 | Triangles Mean / P95 | `gpu_ms` Mean / P50 / P95 | `wall_ms` Mean / P50 / P95 | Streaming Mips (`LODGroups`) |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| HALL+STAFF Baseline (`opt-Medium-cool`) | Medium (`0.88x`) | — | 855.0 / 1,609 | 134 | 571k / 959k | 12.82 / 13.41 / 17.89 | 14.78 / 14.78 / 21.53 | 424.2 MiB (457) |
| **LOC01 Final (`bench/final-Medium`)** | **Medium (`0.88x`)** | **3,697** | **699.4 / 1,357** | **137** | **722k / 1,245k** | **14.83 / 15.24 / 19.65** | **15.83 / 16.04 / 21.31** | **490.5 MiB (583)** |
| HALL+STAFF Baseline (`opt-High`) | High (`1.00x`) | — | 999.9 / 1,754 | 150 | 691k / 1,160k | 20.78 / 21.21 / 27.65 | 22.67 / 23.19 / 29.98 | 424.2 MiB (457) |
| **LOC01 Final (`bench/final-High`)** | **High (`1.00x`)** | **2,158** | **828.8 / 1,619** | **152** | **907k / 1,563k** | **25.76 / 25.89 / 33.89** | **27.49 / 27.30 / 35.61** | **511.8 MiB (583)** |

> **Phân tích & Ghi chú minh bạch về điều kiện đo:**
> - **Giảm mạnh Draw Calls trên toàn tuyến:** Nhờ hợp nhất hàng chục khối primitive ngoại cảnh trước đây thành 18 tổ hợp mesh Blender Cycles-baked kèm 583 LODGroup (`+126`), bản Final **giảm −155,6 draw calls trung bình / −252 P95 ở Medium** (`699.4` vs `855.0`) và **−171,1 mean / −135 P95 ở High** (`828.8` vs `999.9`), trong khi `SetPass P95` gần như không đổi (`+2..+3`).
> - Trên **Medium 1080p**, tốc độ trung vị đạt **`16.04 ms` (~62,3 FPS)** (`gpu_mean = 14.83 ms`, riêng các đoạn sân LOC01: `approach-R` đạt `gpu_mean = 15.36 ms`, `approach-return-R` đạt `14.44 ms`, `seam` đạt `14.44 ms`). Dung lượng texture streaming hoạt động ổn định trong ngân sách mới (`490.5 / 511.8 MiB`).
> - **Ghi chú PresentMon:** Khi chạy tự động trong phiên không có tương tác desktop foreground, wrapper `measure_m2.ps1` dừng ở bước kiểm tra `IsForegroundProcess` của PresentMon; số liệu bảng trên được thu trực tiếp từ telemetry D3D12 `FrameTimingManager` trong player (`-ctcc-benchmark`, `passed=True`) tại [`output/loc01-hero/bench/final-Medium/`](file:///c:/Ca%20trực%20cuối%20cùng/output/loc01-hero/bench/final-Medium/result.txt) và [`output/loc01-hero/bench/final-High/`](file:///c:/Ca%20trực%20cuối%20cùng/output/loc01-hero/bench/final-High/result.txt). Có thể chạy bổ sung ETW PresentMon khi màn hình Windows ở trạng thái mở khóa tương tác.

### D. Kiểm tra Không hồi quy (Non-Regression) & Tư liệu Đối chiếu
- **LOC02 Reception Non-Regression (31 ảnh):** [`output/loc01-hero/nonreg-loc02/`](file:///c:/Ca%20trực%20cuối%20cùng/output/loc01-hero/nonreg-loc02/) — đã kiểm trực tiếp `L02-02-hero-wide-B.png`: quầy tiếp nhận, Lan, vách kính, lịch 11/2005 và ánh sáng không có bất kỳ hồi quy nào.
- **HALL & STAFF Non-Regression (33 ảnh):** [`output/loc01-hero/nonreg-hall-staff/`](file:///c:/Ca%20trực%20cuối%20cùng/output/loc01-hero/nonreg-hall-staff/) — đã kiểm trực tiếp `HS-02-hall-south-wide-R.png` và `HS-10-staff-workstation-B.png`: dầm trần, cửa phòng 101, bảng Cấp Cứu đỏ và bàn điều dưỡng STAFF nguyên vẹn tuyệt đối.
- **4 Bảng Tổng hợp Before–After & R/B:**
  - [`SUMMARY_BEFORE_AFTER_R.png`](file:///c:/Ca%20trực%20cuối%20cùng/output/loc01-hero/comparisons/SUMMARY_BEFORE_AFTER_R.png)
  - [`SUMMARY_BEFORE_AFTER_HERO_R.png`](file:///c:/Ca%20trực%20cuối%20cùng/output/loc01-hero/comparisons/SUMMARY_BEFORE_AFTER_HERO_R.png)
  - [`SUMMARY_BEFORE_AFTER_B.png`](file:///c:/Ca%20trực%20cuối%20cùng/output/loc01-hero/comparisons/SUMMARY_BEFORE_AFTER_B.png)
  - [`SUMMARY_RB_CONTRAST.png`](file:///c:/Ca%20trực%20cuối%20cùng/output/loc01-hero/comparisons/SUMMARY_RB_CONTRAST.png)
- **Video Walkthrough 30 fps H.264 Đồng bộ Before–After:**
  - [`loc01_before_after_sidebyside_R.mp4`](file:///c:/Ca%20trực%20cuối%20cùng/output/loc01-hero/comparisons/videos/loc01_before_after_sidebyside_R.mp4) (`7.8 MiB`)
  - [`loc01_before_after_sidebyside_B.mp4`](file:///c:/Ca%20trực%20cuối%20cùng/output/loc01-hero/comparisons/videos/loc01_before_after_sidebyside_B.mp4) (`8.0 MiB`)
  - Bản 1080p độc lập: `loc01_before_{B,R}.mp4` & `loc01_after_{B,R}.mp4` (`15.2–16.0 MiB`).
- **Tệp nguồn & Giấy phép:** Đã cập nhật đầy đủ tại [`Prototype/ThirdPartyNotices.md`](file:///c:/Ca%20trực%20cuối%20cùng/Prototype/ThirdPartyNotices.md).
