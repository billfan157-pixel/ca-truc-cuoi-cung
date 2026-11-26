# Bệnh Viện Đa Khoa An Sinh — Báo Cáo Mở Rộng Quần Thể Kiến Trúc & Tích Hợp Unity

**Dự án:** Ca Trực Cuối Cùng (*The Last Night Shift*)  
**Nhánh triển khai:** `feat/an-sinh-full-campus-expansion` (từ `HEAD ac0c270`)  
**Engine:** Unity `6000.3.28f1` / URP `17.3` (Forward+) · Blender `4.5.10 LTS`  
**Tài liệu nguồn đối chiếu:** Core 01–07 (đặc biệt Core 03 `Campus-v0`, Core 04 `CH01`), `DEC-WORLD-01`, [walkthrough B01–B08 (lưu trữ)](../archive/ch01-history/walkthrough-gameplay-B01-B08-investigation-v1.md), `docs/prototype/layout.json`, `docs/prototype/campus-layout-concept.json`.

---

## 1. Tóm tắt triển khai & Quy mô lựa chọn

Môi trường bệnh viện An Sinh đã được xây dựng lại trực tiếp trong hệ thống multi-scene production của Unity (`AnSinh_Bootstrap.unity` + **8 scene additive**) thành một quần thể bệnh viện đa khoa tuyến huyện/khu vực Việt Nam giai đoạn **1987 → 2005 → 2012** có tổ chức công năng, phân luồng giao thông, niên đại vật liệu và cấu trúc tầng bậc nhất quán:

- **Phương án quy mô lựa chọn:** Quần thể **6 khối nhà chức năng (`K-A` .. `K-F`)** trên khu đất **100 × 110 m (`11.000 m²` / 1,1 ha)**, mật độ xây dựng ~29%, tổng diện tích sàn xây dựng (GFA) **~6.390 m²** (bao gồm tầng hầm B1 ~700 m²).
- **Quy hoạch giường bệnh (Planning Seed — không phải canon thoại/hồ sơ):** **88 giường thường quy** (22 buồng tiêu chuẩn × 4 giường thuộc K-A Tầng 2 và K-C 3 tầng), tách biệt với giường lưu cấp cứu (K-D), giường 4 tạm thời ở Phòng 101 (`LOC03`) và 12 vị trí đặc biệt ở Khu C tầng hầm (`LOC12`) theo đúng `DEC-WORLD-01`.
- **Bảo toàn 100% lõi gameplay & canon:** Toàn bộ tọa độ, cao độ, cửa, vật chứng và quan hệ đã khóa trong `layout.json` và `HospitalAuthoring.Expansion.cs` (`LOC01–LOC12`, `Room 101`, `AP00–AP09`, `AN01–AN08`, `E01–E12`, `F101`, `G01–G08`, `S1/S2/S3`, tuyến `CH01–CH08`, tính độc lập của tuyến `Khu B → S1 → sân` với B1 và niêm phong `Khu C`) đều được giữ nguyên tuyệt đối.

---

## 2. Bản vẽ kiến trúc 2D đồng bộ 1:1 với tọa độ Unity

Toàn bộ 3 bản vẽ được sinh trực tiếp từ hệ tọa độ Unity đang chạy (`tools/draw_campus_plans.py`):

- **Mặt bằng tổng thể 100 × 110 m (6 khối, 4 cổng, 4 luồng):** [campus-master-plan.png](campus-master-plan.png)
- **Mặt bằng 4 tầng (B1 `-3.40m`, Trệt `±0.00m`, Tầng 2 `+4.00m`, Tầng 3 `+8.00m`):** [campus-floors-B1-3F.png](campus-floors-B1-3F.png)
- **Mặt cắt Tây–Đông (A–A') & Nam–Bắc (B–B') với đủ mốc cao độ:** [campus-sections.png](campus-sections.png)
- **Dữ liệu đồng bộ:** [campus-layout-concept.json](../prototype/campus-layout-concept.json) (đã nâng cấp lên `Campus-v1`; [layout.json](../prototype/layout.json) giữ nguyên 100% không sửa đổi).

---

## 3. Bảng quy mô kiến trúc, niên đại & phân vùng scene

| Khối | Chức năng chính | Niên đại | Phạm vi tọa độ Unity `(X, Z)` | Cao độ / Số tầng | GFA ước tính | Giường quy hoạch* | Luồng tiếp cận | Scene Unity sở hữu |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| **K-A** | Khối chính (Lễ tân `LOC02`, P.101 `LOC03`, Cấp cứu lõi `LOC04`, Khu B `LOC05–06`, Kho/GĐ `LOC07–08`, B1 `LOC09–12`, `Sân sáng`) | **1987** (vữa vôi vàng-xanh, ô gạch bông gió cầu thang, mái tôn chống nóng) | `X [-15..+18]`, `Z [-8..+40]` + cánh Bắc `X [-6..+10]`, `Z [40..50]` | `B1 (-3.4m)` + `3F (+11.7m)` | ~2.120 m² | 32 giường (8P×4G) + 12G Khu C (ngoại lệ) | Luồng 1 (từ K-B), Cổng đêm `G2`, Luồng 3 nội trú | Lõi: `LOC01–05`, `LOC09`; Vỏ 3F & mái: `Campus_Grounds`; Hậu cảnh: `Campus_Interiors` |
| **K-B** | Khám ngoại trú, đăng ký khám, CLS/xét nghiệm, nhà thuốc + Nhà xe bệnh nhân | **1998–2002** (cải tạo bê tông-kính khung thép) | `X [-38..-22]`, `Z [-8..+28]` | `2F (+8.0m)` + mái tôn | ~1.152 m² | 0 (ngoại trú) | Luồng 1 từ **Cổng chính G1** `(-30,-30)` → cầu nối `K-B ↔ K-A` | `Campus_Grounds` + `Campus_Interiors` |
| **K-C** | Điều trị nội trú 3 tầng, ban công phơi/hành lang hướng Nam | **2001–2004** (mở rộng công suất trước 2005) | `X [+22..+48]`, `Z [+46..+68]` | `3F (+12.1m)` + mái tôn + bồn nước | ~1.716 m² | 56 giường (14P×4G) | Luồng 3 qua **Cầu nối 2 tầng `K-A ↔ K-C`** (`Z=48`) | `Campus_Grounds` |
| **K-D** | Khoa Cấp cứu mở rộng, phòng lưu bệnh, X-Quang & Mái đón xe cấp cứu (*Porte-cochère*) | **1995–2000** (bổ sung băng đỏ & mái đón xe) | `X [+18..+43]`, `Z [+14..+32]` + Mái đón `X [43..49.5]` | `1F (+4.6m)` | ~535 m² (gồm mái đón 85 m²) | Vị trí lưu cấp cứu (không tính giường thường quy) | Luồng 2 riêng từ **Cổng cấp cứu G3** `(53,-30)` | `Campus_Grounds` + `Campus_Interiors` |
| **K-E** | Hành chính, tài vụ, lưu trữ hồ sơ hành chính | **1990s** | `X [-40..-26]`, `Z [+41..+63]` | `2F (+8.0m)` | ~616 m² | 0 | Lối nội bộ phía Tây tách khỏi cấp cứu | `Campus_Grounds` |
| **K-F** | Hậu cần (bếp, giặt, nồi hơi, kho) & **Đài nước bê tông +17,5 m** | **1987–1990s** | `X [-14..+10]`, `Z [+59..+73]`; Đài nước `(-18.5, 66.5)` | `1F (+4.1m)` + ống khói `+11.6m` + đài nước `+17.5m` | ~336 m² | 0 | Luồng 4 từ **Cổng hậu cần Bắc G4** `(14,80)` | `Campus_Grounds` |
| **Hạ cốt** | **Sân sau hạ cốt `-1.90m`** + **Hố quan sát `E05b` `-3.40m`** + **Ram dốc nhà xác Tây `-3.40m`** | 1987 + **2004** (bịt gạch cửa sau Khu C `E05b`) | Sân sau `X [37.2..49], Z [35..45]`; Hố `E05b` `X [34.7..37.2]`; Ram Tây `X [-19.5..-1.5], Z [37.7..43.1]` | `-1.90m` / `-3.40m` | ~225 m² ngoài trời | — | Luồng 4 hậu cần & hành lang bao kỹ thuật từ `LOC11` | `LOC09_ServiceWing` (Hố `E05b` có NavMesh) + `Campus_Interiors` (Sân `-1.9m` & ram) |

*\*Ghi chú: Tổng 88 giường thường quy chỉ là thông số tỷ lệ kiến trúc tham khảo theo `DEC-WORLD-01`, không đưa thành con số canon trong thoại hay tài liệu trong game.*

---

## 4. Bộ asset 3D Modular (Blender 4.5.10 LTS), PBR & Nguồn quyền

Toàn bộ kiến trúc chính và bối cảnh quần thể được dựng bằng asset 3D thật (có sẵn `LOD0 / LOD1 / LOD2`, UV chuẩn mét, collider chính xác và cặp vật liệu R/B), không dừng ở khối hộp greybox:

### 4.1. 14 Module 3D mới (`tools/blender_assets/build_campus_kit.py` → `ArtSource/Blender/` & `Prototype/Assets/CTCC/Art/Meshes/`)

| Asset FBX | Kích thước (m) | LOD0 Tris | Vai trò & chi tiết kiến trúc |
| --- | --- | ---: | --- |
| `CTCC_ENV_MainFacadeBay_1987.fbx` | `4.12 × 0.87 × 4.00` | 2.952 | Mô-đun mặt đứng khối K-A 1987: khung cột-dầm, cửa sổ thép song chắn, ô văng mưa bê tông, hộp điều hòa cũ |
| `CTCC_ENV_StairwellBreezeBay_1987.fbx` | `4.04 × 0.51 × 4.00` | 7.356 | Mảng gạch bông gió thông gió cầu thang 1987 đặc trưng bệnh viện Việt Nam (đặt trục giữa sảnh K-A, hành lang S1 & tháp thang K-C) |
| `CTCC_ENV_ClinicFacadeBay_2002.fbx` | `4.10 × 0.76 × 4.00` | 2.224 | Mô-đun cải tạo 1998–2002 cho K-B, K-D, K-E: ô cửa sổ rộng hơn, chỉ ngang bê tông, bậu cửa sổ gọn |
| `CTCC_ENV_WardBalconyBay_2004.fbx` | `4.03 × 1.59 × 4.00` | 2.196 | Mô-đun ban công nội trú 2001–2004 cho K-C hướng Nam: lan can thép, cửa ra ban công, đèn ống trần ban công R/B |
| `CTCC_ENV_CorrugatedRoofModule.fbx` | `8.06 × 8.74 × 1.87` | 1.644 | Mái tôn chống nóng gợn sóng (flat-shaded sắc nét), hệ vì kèo thép, máng xối & úp nóc (tách rỉ 2012 / sạch 2005) |
| `CTCC_ENV_CoveredWalkwayBay.fbx` | `4.08 × 4.73 × 3.70` | 2.728 | Hành lang cầu nối có mái che (`K-B ↔ K-A` & cầu 2 tầng `K-A ↔ K-C`): cột gạch/vữa, lan can ống thép, đèn T8 |
| `CTCC_ENV_AmbulanceCanopy.fbx` | `13.38 × 6.79 × 6.26` | 4.840 | Mái đón xe cấp cứu K-D: cột trụ sơn vằn vàng-đen cảnh báo, băng đỏ fascia, trần đèn cứu thương |
| `CTCC_ENV_MainHospitalArchGate.fbx` | `10.30 × 0.92 × 5.93` | 3.628 | Cổng vòm chính `Cổng G1`: trụ gạch-bê tông, chữ thập đỏ, 2 đèn cầu, cửa sắt hai cánh & cổng đi bộ |
| `CTCC_ENV_WaterTowerAndRoofKit.fbx` | `5.40 × 5.48 × 17.50` | 1.016 | Đài nước bê tông cốt thép cao 17,5 m (6 chân cột lục giác, dầm giằng, bồn trụ, thang sắt, đèn cảnh báo đỏ) |
| `CTCC_ENV_RooftopTankCluster.fbx` | `7.81 × 4.45 × 5.40` | 1.320 | Cụm tum cầu thang mái + bồn nước inox + ống thông hơi & kim thu lôi trên mái K-A, K-C |
| `CTCC_ENV_UtilityPoleAndLamp.fbx` | `1.75 × 2.62 × 8.50` | 884 | Cột điện bê tông ly tâm + cần đèn cao áp natri chiếu sáng đường nội bộ & mặt tiền |
| `CTCC_ENV_ParkingShedBay.fbx` | `6.24 × 4.83 × 2.79` | 744 | Nhà xe mái tôn một dốc cho bệnh nhân ngoại trú tại sân K-B |
| `CTCC_PROP_VintageAmbulance.fbx` | `2.00 × 5.16 × 2.21` | 3.224 | Xe cứu thương hộp kiểu thập niên 1990–2000 nguyên tác: đậu ngay ngắn trong mái đón ở B (2005), bỏ xéo ngoài sân ở R (2012) |
| `CTCC_ENV_B1SealedDoorE05b.fbx` | `2.42 × 0.50 × 2.47` | 1.512 | Chứng cứ kiến trúc `E05b`: khung cửa 1987 còn nguyên bản lề thép & lanh-tô, lòng cửa bị xây bịt kín bằng gạch đỏ năm 2004 |
| **Tổng cộng** | — | **36.268** | Toàn bộ đi kèm LOD0/LOD1/LOD2 tự động và source `.blend` trong `ArtSource/Blender/` |

### 4.2. Bộ vật liệu PBR & Biển báo tiếng Việt (`tools/bake_campus_surfaces.py`)

- **9 bộ vật liệu PBR mới (27 ảnh PNG `_Color / _Normal / _Mask`):** `ExteriorFacadeB`, `CampusStuccoR/B`, `CorrugatedRoofR/B`, `ConcretePavingR/B`, `BrickInfill2004` + cặp vật liệu kính cửa sổ `WindowDarkR` (phản xạ đêm tối 2012) ↔ `WindowLitB` (ánh sáng huỳnh quang xanh-trắng 2005).
- **15 biển báo tiếng Việt hư cấu cho Bệnh viện An Sinh:** `Sign_MainArch`, `Sign_KB_Outpatient`, `Sign_KC_Inpatient`, `Sign_KD_Emergency` (hộp đèn đỏ phát sáng), `Sign_KE_Admin`, `Sign_KF_Service`, `Sign_NightGate`, `Sign_AmbulanceGate`, `Sign_MorgueService`, `Sign_CampusMap` (bảng sơ đồ chỉ dẫn 6 khối đặt tại sân LOC01), `Sign_Pharmacy`, `Sign_XRay`, `Sign_Ward102/201/202`.
- **Kiểm nguồn & bản quyền:** 100% asset 3D, biển báo và texture campus mới là sản phẩm nguyên tác sinh bằng quy trình Blender/Python riêng cho dự án hư cấu An Sinh (kết hợp thư viện bề mặt CC0 ambientCG đã được ghi nhận trong `ThirdPartyNotices.md`); không sao chép bệnh viện thật ngoài đời và không dùng tài sản thiếu quyền.

---

## 5. Tích hợp Unity, Refactor phân vùng 3D XYZ & R/B Environment Art

- **Hệ thống 8 scene additive production (`HospitalAuthoring.cs`, `HospitalScene.cs`, `ZoneRegion.cs`, `RegionStreamer.cs`):**
  - **5 scene boot luôn tải sẵn:** `LOC01_Exterior`, `LOC02_Reception`, `LOC03_Ward101`, `LOC04_Emergency`, và `Campus_Grounds` (`CAMPUS_EXT`, chứa toàn bộ vỏ kiến trúc 6 khối, đường nội bộ, cổng, mái, cây xanh, cột đèn, `participatesInNavMesh = false` để không tăng chi phí cập nhật NavMesh khi chuyển lớp R↔B).
  - **3 scene stream theo ngữ cảnh 3D XYZ:** `LOC05_ArchiveWing` (Tầng 2–3 sau G02), `LOC09_ServiceWing` (B1 sau G02/CH04, gồm cả hành lang bao kỹ thuật & hố quan sát `E05b`), và `Campus_Interiors` (`CAMPUS_WINGS`, nội thất nền sau kính/sân hạ cốt với hysteresis 60 giây tránh thrashing trong CH01).
- **Refactor `ZoneAt(float z)` → `ZoneAt(Vector3 worldPos)` & `ResolveZoneAt(Vector3)`:** Phân biệt chính xác nhiều cánh nhà cùng trục Z (`K-B` ở `X=-30`, `K-A` ở `X=4`, `K-D` ở `X=+30`) và chồng tầng theo trục Y (`B1 < -0.5m`, cột không khí sân `LOC01`, tầng trệt gồm trần `y=3.71m`, tầng 2–3 `> 3.88m`), đồng thời giữ tương thích 100% với toàn bộ `zoneId`, safe pad và save schema `v1–v7`.
- **Khắc phục triệt để lỗi đồ họa gốc & nâng cấp LOC01–LOC03:**
  - Gỡ bỏ `CTCC_ENV_HospitalFacade` cũ vốn bị đặt lơ lửng tại `y = +3.60m`, thay bằng khối K-A 3 tầng tiếp đất thật + dời biển `AN SINH` lên mép trước mái đón tránh bị khuất.
  - Gỡ tường sân Bắc phẳng ở `z = 15.2` để mở thông tầng **Sân Sáng (`Sân sáng` 3 tầng)** ngay ngoài 3 cửa sổ Phòng 101 (`LOC03`).
  - Hiệu chỉnh `farClipPlane` (`68m → 190m`), đồng bộ mật độ sương mù `RenderSettings.fogDensity` giữa Editor và Runtime (`0.0082` ở R, `0.0056` ở B), bóng đổ `38m / 2 cascades`, và bake **369 Dual LightProbes (R & B)** cùng **Multi-Scene Occlusion Culling (9 scene)**.
- **Duality R (2012) ↔ B (2005):** `1.466` cặp vật liệu/bề mặt đồng bộ (`LayerFinish`) giữ nguyên hình khối, landmark và collider giữa hai thực tại; chuyển từ mưa đêm, vữa rêu thấm nước, kính tối, mái tôn rỉ, xe cứu thương bỏ xéo (2012) sang ca trực vận hành sạch, kính sáng đèn huỳnh quang, quạt quay và xe cấp cứu đậu ngay ngắn trong mái đón (2005).

---

## 6. Bằng chứng thị giác Trước / Sau & Tổng thể quần thể

### Đối chiếu Trước (`HEAD ac0c270`) vs Sau (Campus Expansion)

| Góc nhìn CH01 | Bảng so sánh 1080p |
| --- | --- |
| **AP00 — Xe máy Minh & Cổng phụ đêm (R 2012)** | [before-after-ap00-exterior.jpg](evidence/before-after-ap00-exterior.jpg) |
| **AP01 — Sân bệnh viện nhìn về khối chính K-A 3F (R 2012)** | [before-after-ap01-courtyard.jpg](evidence/before-after-ap01-courtyard.jpg) |
| **AP04 — Phòng 101 nhìn qua cửa sổ ra Sân Sáng 3 tầng (R 2012)** | [before-after-room101-lightcourt.jpg](evidence/before-after-room101-lightcourt.jpg) |
| **Tổng hợp 6 góc quần thể (Tây-Nam R/B, Đông-Bắc B, Cổng chính, Cấp cứu K-D, B1 E05b)** | [campus-showcase-6panel.jpg](evidence/campus-showcase-6panel.jpg) |

### Bộ ảnh 1920×1080 chi tiết từng khu (R 2012 & B 2005)

- **CH01 — Cổng phụ, Sân, Góc chéo Đông-Bắc & Sảnh Lan:** [campus-ap00-R.jpg](evidence/campus-ap00-R.jpg) · [campus-ap01-R.jpg](evidence/campus-ap01-R.jpg) · [campus-ap01-B.jpg](evidence/campus-ap01-B.jpg) · [campus-courtyard-diag-R.jpg](evidence/campus-courtyard-diag-R.jpg) · [campus-ap02-reception-B.jpg](evidence/campus-ap02-reception-B.jpg)
- **Room 101 → Sân Sáng nội bộ:** [campus-ap04-room101-window-R.jpg](evidence/campus-ap04-room101-window-R.jpg) · [campus-ap04-room101-window-B.jpg](evidence/campus-ap04-room101-window-B.jpg)
- **Các cánh nhà mới (Cổng chính + K-B Ngoại trú, K-D Cấp cứu + Xe cứu thương):** [campus-kb-maingate-R.jpg](evidence/campus-kb-maingate-R.jpg) · [campus-kb-maingate-B.jpg](evidence/campus-kb-maingate-B.jpg) · [campus-kd-ambulance-R.jpg](evidence/campus-kd-ambulance-R.jpg) · [campus-kd-ambulance-B.jpg](evidence/campus-kd-ambulance-B.jpg)
- **Tầng 2 (`S1` Khu B) & Tầng hầm B1 (Hố quan sát `E05b` cửa bịt gạch 2004):** [campus-upper-s1-loc05-R.jpg](evidence/campus-upper-s1-loc05-R.jpg) · [campus-b1-e05b-R.jpg](evidence/campus-b1-e05b-R.jpg)
- **Toàn cảnh trên không (Tây-Nam & Đông-Bắc, R & B):** [campus-aerial-SW-R.jpg](evidence/campus-aerial-SW-R.jpg) · [campus-aerial-SW-B.jpg](evidence/campus-aerial-SW-B.jpg) · [campus-aerial-NE-R.jpg](evidence/campus-aerial-NE-R.jpg) · [campus-aerial-NE-B.jpg](evidence/campus-aerial-NE-B.jpg)

---

## 7. Kết quả kiểm thử thực tế & Hiệu năng (tách rõ loại bằng chứng)

### 7.1. Định danh bản build nghiệm thu

- **Build cuối cùng (`tools/build_m2.ps1`):** [campus-build-identity.txt](evidence/campus-build-identity.txt)
  - Unity `6000.3.28f1` Windows x64 Non-Development (`968.836.148 bytes`).
  - `assembly_sha256 = 476620f1bd8572bd84f7ca74886619767b8b500d4a7db1ee1852b6bfa3ab8892` (dùng cho **Module QA** cuối và **Performance Benchmark** cuối).
  - Toàn bộ 9 scene `.unity` có SHA-256 giống hệt nhau giữa bản chạy **Player Input QA** (`assembly_sha256 = f1acd186...`, chạy ngay trước chỉnh sửa duy nhất tại `HospitalGame.Benchmark.cs` cho đường chạy benchmark) và bản **Module QA + Benchmark** cuối.

### 7.2. Kết quả từng lớp kiểm thử

| Lớp kiểm thử | Trạng thái | Bằng chứng thực tế |
| --- | --- | --- |
| **1. Unity Editor Compile, Scene Generate, NavMesh Blockout, Dual GI & Occlusion** | **PASS** | `CTCC_GREYBOX_PASS`, `CTCC_ROUTE_PASS` (`R`, 3 nhánh `F101`, `LOC04`, `CH01-R staff loop`, `CH03–04`, `W-Q02`), `CTCC_WQ03_PASS`, `CTCC_GI_PROBES_BAKE okR=True (369 probes) okB=True (369 probes)`, `CTCC_OCCLUSION_BAKE success=True (9 scenes)`, `CTCC_SCENE additive-8regions 1466 paired finishes, 1125 static objects`. |
| **2. Automated Runtime Module QA (`-ctcc-qa`)** | **PASS** | [campus-qa-module.txt](evidence/campus-qa-module.txt) — `RESULT=PASS`: `TASK025-A/B`, `T02/T03/T06/T08`, `M2-ADDITIVE`, `V22-Q03`, `CHAR-Q01/Q04–08`, `AU01`, `CH02-Q05`, `CH01-R ADVERSARIAL`, `CH01 WALKTHROUGH & PROPOSAL V1.1`, `CH03-Q01–Q05`, `CH04-Q02–Q06`, `WORLD-Q03/Q04 & W-Q02/W-Q03`. |
| **3. Real Player Input QA (`-ctcc-input-qa`, Windows keyboard+mouse)** | **PASS** | [campus-qa-input.txt](evidence/campus-qa-input.txt) — `RESULT=PASS`: cả 3 nhánh F101 (`Affirmed` + Door A, `Unverified` + Door B + đi bộ vật lý CH03–CH04 qua 3 nhịp thang, `Refuted` không dịch chuyển tức thời suốt CH01 + journal/annotation + manual save/load + stress test 100 vòng, `222→222 RectTransforms`, `0` mesh/material rò rỉ). |
| **4. Performance Benchmark (`-ctcc-benchmark`, 1920×1080 `Medium`, `-ctcc-expansion true`)** | **PASS** | [campus-benchmark-result.txt](evidence/campus-benchmark-result.txt) — `passed=True`, `10.767` frames trên **Intel Core Ultra 7 155H / Intel Arc iGPU** (D3D12, `render_scale=0.88`, `dual-probes`, `644 LODGroups`). Chi tiết ở §7.3. |
| **5. Playtest người chơi thật & Đánh giá chuyên gia độc lập (`TASK010`, `TASK026–028`)** | **CHƯA CHẠY** | Chưa thực hiện thử nghiệm người chơi thật ngoài môi trường tự động và chưa qua đánh giá chuyên gia kiến trúc/y khoa độc lập; không báo PASS cho hạng mục này. |

### 7.3. Chỉ số hiệu năng thực đo (CH01 → CH04, 1920×1080 Medium, Intel Arc iGPU)

| Chỉ số | Giá trị thực đo |
| --- | --- |
| **Tổng số frame đo (`frames.csv`)** | `10.767` frames |
| **Thời gian khởi động đến sẵn sàng (`boot_to_ready`)** | `2,33 s` |
| **Wall Frame Time — Trung vị (P50)** | **`9,79 ms` (`102,2 FPS`)** |
| **Wall Frame Time — P95 / P99** | **`17,06 ms` (`58,6 FPS`)** / `19,43 ms` (`51,5 FPS`) |
| **CPU Main Thread / Render Thread / GPU (P50)** | `4,04 ms` / `1,66 ms` / `9,51 ms` |
| **CH01 Ngoài trời (`gate-R` / `approach-R` / `approach-return-R`) P50** | `12,65 ms` (`79,1 FPS`) / `12,06 ms` (`82,9 FPS`) / `8,86 ms` (`112,9 FPS`) |
| **CH01 Sảnh Lan (`lan-reception-B`) / Phòng 101 (`room-entry-B`) P50** | `13,19 ms` (`75,8 FPS`) / `16,04 ms` (`62,4 FPS`) |
| **CH03 Tầng 2–3 (`ch03-stairs-and-archive`) / CH04 B1 (`ch04-morgue`) P50** | `8,80 ms` (`113,6 FPS`) / `6,95 ms` (`143,8 FPS`) |
| **Draw Calls / SetPass (Trung vị)** | `1.002` draws / `53` SetPass |
| **Triangles (Trung vị / P95)** | `265.060` tris / `849.266` tris |
| **Chuyển lớp R↔B có lưu nguyên tử (`swap-R-B` / `swap-B-R`)** | `70,71 ms` / `66,93 ms` |
| **Streaming additive (`load-LOC05` / `load-LOC09`)** | `82,16 ms` / `99,20 ms` |
| **Bộ nhớ Unity Allocated / System RAM (Trung vị / Đỉnh)** | `291,0 MiB` (max `298,7 MiB`) / `1.787,9 MiB` (max `1.813,6 MiB`) |

### 7.4. Đối chiếu tiêu chí Core 03 `W-Q01–W-Q07` & `WORLD-Q01–Q06`

| Mã QA | Yêu cầu cốt lõi | Kết quả xác minh & căn cứ |
| --- | --- | --- |
| **W-Q01** | Tách 4 luồng giao thông (ngoại trú, cấp cứu, nội trú, hậu cần/nhà xác) & kiểm soát cổng | **PASS (Kiến trúc 2D/3D + Visual):** 4 cổng riêng (`G1–G4`), đường xe cấp cứu riêng sang K-D, cầu nối có mái `K-B↔K-A` và `K-A↔K-C`, cổng Bắc hậu cần + ram nhà xác Tây. |
| **W-Q02** | Tuyến thoát Khu B (`LOC05`) qua `S1` về sảnh/sân độc lập hoàn toàn với B1 (`LOC09`) | **PASS (Editor + Runtime QA):** Đã xác minh bằng cả `CTCC_ROUTE_PASS W-Q02` (Editor) và runtime `VerifyCampusWorldQA()` khi dỡ hoàn toàn `LOC09_ServiceWing` khỏi bộ nhớ & NavMesh. |
| **W-Q03 / WORLD-Q03** | Cửa sau Khu C bịt gạch 2004 (`E05b`) quan sát được từ ngoài trong R trước `G06`, không tạo lối tắt xuyên vào `LOC12` | **PASS (Editor + Runtime QA + Visual):** `CTCC_WQ03_PASS` và `VerifyCampusWorldQA()` xác nhận đường NavMesh `AP08 → hố E05b (35.8, -3.4, 42.0)` thông hoàn toàn trong khi tường gạch `x=34.5` chặn cứng cả collider (`Physics.Linecast=True`) lẫn NavMesh (`breach=False`); ảnh [campus-b1-e05b-R.jpg](evidence/campus-b1-e05b-R.jpg). |
| **W-Q04** | Nhà xác (`LOC10`) có cửa dịch vụ Tây kín đáo tách luồng sảnh công cộng & Khu C | **PASS (3D Authoring + Bản vẽ):** Cửa dịch vụ khóa đặt tại `(-1.42, -2.25, 40.95)` (tránh tủ khô tại `z=39`), nối ra ram dốc Tây `-3.40m` dẫn tới nhánh phụ cổng Bắc; không nối thông công cộng hay `LOC12`. |
| **W-Q05** | 3D khớp 2D mặt bằng/mặt cắt, cao độ chuẩn (`-3.4`, `-1.9`, `0`, `+4`, `+8`, `+11.7`, `+17.5m`) | **PASS (Automated + Visual):** 3 bản vẽ 2D sinh trực tiếp từ hệ tọa độ Unity; `VerifyScalableM3AndCh03` xác nhận cao độ chuẩn `AP06 (+4.0m)`, `AN03 (+8.0m)`, `AP08 (-3.4m)`. |
| **W-Q06** | Nhất quán landmark & collider giữa R (2012) và B (2005); biến đổi chỉ nằm trong phạm vi canon | **PASS (Runtime QA + Visual):** 1.466 cặp `LayerFinish` dùng chung cấu trúc/collider; ảnh R/B tại `AP01`, `Room 101`, `K-B`, `K-D`, và trên không khớp nhau tuyệt đối. |
| **W-Q07** | Quy mô lớn nhận ra ngay từ các góc CH01 mà không làm dài thời lượng hay lạc đường CH01 | **PASS (Input QA + Visual):** `INPUT CH01-R FULL ROUTE PASS` giữ nguyên 100% tuyến CH01 B01–B08 không dịch chuyển tức thời; từ `AP00`, `AP01`, sân và `Room 101` thấy rõ K-A 3 tầng, Sân Sáng 3 tầng và cánh K-B/K-D. |
| **WORLD-Q01 / Q02** | Không để lộ khoảng trống chân trời / khối hộp lơ lửng; nhận diện bệnh viện Việt Nam 1987–2005–2012 | **PASS (Visual 1080p):** Đã xóa lỗi facade lơ lửng `y=+3.6m`, thêm 6 khối nhà thật + đường phố Nam + hàng cây chân trời + biển báo tiếng Việt chuẩn thời kỳ. |
| **WORLD-Q04** | Refactor phân vùng 3D XYZ hỗ trợ nhiều cánh cùng Z & nhiều tầng, không hỏng save/checkpoint | **PASS (Runtime QA):** `VerifyCampusWorldQA()` xác minh phân tách cùng Z giữa `K-B`/`K-A`/`K-D`, phân tách theo tầng `B1/Trệt/2F/3F`, tải/dỡ `Campus_Interiors` giữ nguyên toàn bộ 6 gameplay zones & pads, cùng legacy save schema `v1–v6`. |
| **WORLD-Q05 / Q06** | Hiệu năng ổn định & không phá hồi quy các chương/cổng hiện tại | **PASS (Benchmark + Module QA + Input QA):** `102,2 FPS` P50 / `58,6 FPS` P95 ở 1080p Medium trên iGPU; 100% suite CH01–CH04 PASS. |
