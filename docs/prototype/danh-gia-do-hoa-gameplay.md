# Đánh giá đồ họa và gameplay — M1 vertical slice

**Ngày:** 19/11/2026  
**Bản dựng đối chiếu:** `M1-0.1.0-1dc1a29558bb` (Windows x64, Unity 6.3 LTS, URP 17.3)  
**Phạm vi:** kiến trúc, đồ vật, vật liệu, ánh sáng, cảnh quan, âm thanh, gameplay và UX của vùng mẫu Phòng 101 + hành lang.  
**Mục tiêu đối chiếu:** game kinh dị đồ họa 3A realistic.

> **Lưu ý về phạm vi:** M0 là giai đoạn nghiên cứu thiết kế và spike engine, không có bản dựng runtime. Vì vậy phần hình ảnh được đánh giá trên bản M1 — vertical slice duy nhất có thể chạy — rồi đối chiếu với các artefact thiết kế M0 (`layout.md`, `research.md`, `engine.md`, `verification.md`). Các kết luận về gameplay cũng lấy M1 làm mẫu đại diện cho những gì đã thực sự vào build.

### Mục lục

0. [Tóm tắt điều hành](#0-tóm-tắt-điều-hành)
1. [Phạm vi, bằng chứng và giới hạn](#1-phạm-vi-bằng-chứng-và-giới-hạn)
2. [Kiến trúc](#2-kiến-trúc)
3. [Đồ vật](#3-đồ-vật)
4. [Vật liệu và texture](#4-vật-liệu-và-texture)
5. [Ánh sáng và hậu kỳ](#5-ánh-sáng-và-hậu-kỳ)
6. [Cảnh quan và thời tiết](#6-cảnh-quan-và-thời-tiết)
7. [Gameplay và UX](#7-gameplay-và-ux)
8. [Âm thanh](#8-âm-thanh)
9. [Đối chiếu với kinh dị 3A](#9-đối-chiếu-với-kinh-dị-3a)
10. [Lộ trình nâng cấp](#10-lộ-trình-nâng-cấp)
11. [Ngân sách hiệu năng](#11-ngân-sách-hiệu-năng)
12. [Rủi ro và quyết định cần chốt](#12-rủi-ro-và-quyết-định-cần-chốt)
- [Phụ lục A — Bảng bằng chứng](#phụ-lục-a--bảng-bằng-chứng)
- [Phụ lục B — Thay đổi thông số đề xuất (P0)](#phụ-lục-b--thay-đổi-thông-số-đề-xuất-p0)

---

## 0. Tóm tắt điều hành

### Kết luận ngắn

M1 là một **vertical slice kỹ thuật tốt**, nhưng là một **hình ảnh chưa đạt mức kinh dị 3A**. Hệ thống trạng thái, vật chứng, cổng tiến trình và lưu nguyên tử được làm chắc hơn phần trình bày rất nhiều. Khoảng cách mỹ thuật không nằm ở việc thiếu một shader đắt tiền, mà ở ba tầng nền:

1. **Hình học thô.** Chỉ có 3 mô hình FBX (`WardBed`, `BedsideCabinet`, `TransportTrolley`); gần như toàn bộ phòng, cửa, cửa sổ, trần, đồ đạc còn lại là cube/quad/cylinder nguyên thủy. Không có bevel, jambs, reveal, trim profile, khe cửa, ốp, trần thả, đường ống, công tắc, ổ cắm.
2. **Vật liệu phẳng.** 9 bộ texture 2048² nhưng albedo ít biến thiên, normal của sàn/kim loại gần như phẳng, mask gần như đồng nhất. Pipeline còn ghi đè độ nhám tác giả bằng `_Smoothness = 0.93` và dùng nhầm kênh occlusion, nên mọi bề mặt đều "sạch và nhựa" như nhau.
3. **Ánh sáng không có GI và contact shadow.** 18/20 đèn không đổ bóng; không có lightmap, không có APV; ambient là một màu phẳng; SSAO chỉ 1 sample, radius 0.3 m. Đồ vật vì thế trông như dán lên nền chứ không nằm trong không gian.

Nếu chỉ thêm bloom, grain hay LUT lên bản hiện tại, game sẽ trông giống **"prototype bị filter"**, không giống 3A. Thứ tự đúng là: sửa pipeline vật liệu → thêm contact shadow/GI → thay hình học nguyên thủy bằng kit modular + hero prop → mới đến grading và volumetrics.

### Bảng điểm

Thang 1–10, trong đó 10 = mức 3A hoàn chỉnh (Resident Evil 2/7, Silent Hill 2 Remake, Alan Wake 2).

| # | Hạng mục | Trọng số | Điểm hiện tại | Mức 3A | Khoảng cách chính |
|---|---|---:|---:|---:|---|
| 1 | Kiến trúc | 15% | **5.5** | 9.0 | Hình khối đúng tỉ lệ nhưng thiếu cấu tạo, chi tiết 2–5 cm, lớp cải tạo |
| 2 | Đồ vật | 15% | **3.5** | 9.0 | 3 FBX, còn lại là hộp; thiếu hero prop và vật dụng kể chuyện |
| 3 | Vật liệu & texture | 20% | **3.0** | 9.0 | Albedo/normal/mask phẳng, smoothness bị ghi đè, decal vô hình |
| 4 | Ánh sáng | 15% | **3.0** | 9.5 | Không GI, 18/20 đèn không bóng, ambient phẳng, SSAO yếu |
| 5 | Hậu kỳ | 5% | **3.5** | 8.5 | Thiếu grain, DOF, LUT riêng, lens, exposure control |
| 6 | Cảnh quan & thời tiết | 10% | **4.0** | 8.5 | Sân vườn là hộp, cây card, mưa không tương tác bề mặt |
| 7 | Âm thanh | 5% | **4.5** | 9.0 | 8 source, 1 reverb zone, không occlusion/ducking/VO |
| 8 | Gameplay | 10% | **4.5** | 9.0 | Vòng lặp điều tra tốt nhưng thiếu lõi kinh dị: AI, truy đuổi, trốn |
| 9 | UX & accessibility | 3% | **5.5** | 8.5 | uGUI, không gamepad, không sensitivity/gamma/volume riêng |
| 10 | Hiệu năng | 2% | **4.5** | 8.5 | 21.62 ms/frame, 593 draw call cho một lát cắt nhỏ |

**Điểm có trọng số: 3.9/10 theo thang 3A.**  
**Quy đổi theo thang prototype/vertical slice: 6.8/10.**  

Hai con số này không mâu thuẫn: với tư cách một prototype để chứng minh pipeline và gameplay, M1 khá tốt; với tư cách một sản phẩm kinh dị 3A, nó còn thiếu gần như toàn bộ tầng sản xuất mỹ thuật.

### Năm nguyên nhân gốc

| # | Nguyên nhân | Bằng chứng |
|---|---|---|
| 1 | Hình học nguyên thủy chiếm gần như toàn bộ cảnh | Chỉ 3 FBX trong `Art/Meshes`; `HospitalAuthoring.cs:182` dùng `CreatePrimitive(Cube)`; `HospitalAuthoring.Art.cs:443–490` chỉ thêm lintel, pier, handrail, conduit |
| 2 | Pipeline vật liệu ghi đè tham số tác giả | `HospitalAuthoring.Art.cs:134` đặt `_Smoothness = .93`; `:132` dùng chung mask cho occlusion; `:130` bump scale .75 trên normal gần phẳng |
| 3 | Không có GI, contact shadow hay shadow từ đèn trong | 18/20 đèn có `m_Type: 0` (None); scene không có lightmap index; ambient `m_AmbientMode: 3` (flat); SSAO 1 sample |
| 4 | Texture 2048² nhưng thông tin bề mặt thấp | Albedo PlasterB/Steel SD 1.5–2.4; normal FloorR/Steel SD 0.35–2.5; mask gần như solid |
| 5 | Vòng lặp kinh dị chưa tồn tại | Không có AI, chase, hiding, damage, inventory, pickup; tương tác chỉ là raycast 2.55 m |

---

## 1. Phạm vi, bằng chứng và giới hạn

### 1.1 Bằng chứng đã dùng

| Nguồn | Dùng để |
|---|---|
| `docs/prototype/verification.md` | Trạng thái M0/M1, thông số hiệu năng, SHA-256, phạm vi nghiệm thu |
| `docs/prototype/engine.md` | So sánh URP/HDRP, DEC-V2-04 |
| `docs/prototype/layout.md`, `research.md` | Tỉ lệ, công năng, vật liệu, bối cảnh |
| `Prototype/Assets/CTCC/Editor/HospitalAuthoring.cs` | Dựng kiến trúc, kích thước, opening |
| `Prototype/Assets/CTCC/Editor/HospitalAuthoring.Art.cs` | Vật liệu, texture, prop, decal, cây |
| `Prototype/Assets/CTCC/Editor/HospitalAuthoring.Lighting.cs` | Đèn, post FX, probe, mưa, audio |
| `Prototype/Assets/CTCC/Runtime/FirstPerson.cs` | Điều khiển, đèn pin, stamina, tương tác |
| `Prototype/Assets/CTCC/Runtime/HospitalUI.cs` | UI, font, accessibility |
| `Prototype/Assets/CTCC/Scenes/AnSinh.unity` | Thống kê scene: 665 GameObject, 531 MeshRenderer, 20 Light, 8 AudioSource, 1 ReflectionProbe |
| `Prototype/Assets/Settings/PC_RPAsset.asset`, `PC_Renderer.asset` | Forward+, shadow, SSAO, render scale |
| `output/qa-final-windows/room101-R.png`, `room101-B.png`, `corridor-R.png`, `comparison.jpg` | Đối chiếu hình ảnh R/B |
| Phân tích pixel trực tiếp trên `Art/Textures` | SD albedo/normal/mask, alpha decal |

### 1.2 Giới hạn

- Không có GPU capture (Nsight/RenderDoc) trong môi trường này; số hiệu năng lấy từ `verification.md`, có ghi rõ phép đo gồm render-to-texture và đọc 1 pixel.
- Không đánh giá trên màn hình HDR hiệu chuẩn; nhận xét màu dựa trên ảnh PNG/JPG xuất từ build.
- Không có playtest người mới trong phạm vi này; nhận xét gameplay dựa trên code, trang thái Production Database và log đã công bố.
- M1 đang ở trạng thái "đang nghiệm thu hình ảnh và hiệu năng" (`verification.md:9`), nên các tuyên bố "đã đạt" về hình ảnh chưa nên coi là đóng.
- Trạng thái Production Database tại thời điểm đánh giá: M0 4/4 hoàn thành, M1 21/21 "Đang nghiệm thu", M2 chưa bắt đầu.

---

## 2. Kiến trúc

### 2.1 Điểm đúng

- **Tỉ lệ và công năng chuẩn.** Phòng 101 là 8 × 6 m, trần 3.60 m, cửa 1.20 m, hành lang 3.00 m (`verification.md:16`). Đây là tỉ lệ bệnh viện thật, không phải "phòng game" phóng đại.
- **Mặt bằng có logic.** Tuyến cổng phụ → quầy trực → phòng 101 → kho giường → cấp cứu rõ ràng; các mốc AP00–05 và proxy cáng 0.72 m đã được kiểm (`verification.md:18`).
- **Hai lớp hiện thực R/B dùng chung hình học.** Đây là quyết định production tốt: tiết kiệm asset, tăng tính đọc của câu chuyện.
- **Collider và điểm neo tách khỏi hình ảnh.** Proxy cáng, capsule target, interaction marker cho thấy tư duy gameplay-first đúng.

### 2.2 Điểm yếu

| Hạng mục | Hiện tại | Hệ quả hình ảnh |
|---|---|---|
| Tường | Hộp phẳng từ `CreatePrimitive(Cube)`; opening chia thành 3 hộp (`HospitalAuthoring.cs:272–278`) | Không có chiều dày reveal, không có jambs, không có vết vá, không có gờ |
| Cửa | Một tấm hộp vật liệu `DoorWood` | Không frame, bản lề, tay nắm, closer, ngưỡng, khe sáng; đọc như "cửa prototype" |
| Cửa sổ | Lỗ mở + plane/card | Không frame profile, sill, glazing bar, seal, tay nắm; nhìn ra ngoài thấy card cây |
| Trần | Slab phẳng | Không trần thả 600 × 600, không grid tee, không đèn huỳnh quang đúng cụm, không vết ố loang |
| Sàn | Hộp + texture | Không khe gạch, không cove, không vết xước theo hướng đi, không nước đọng cục bộ |
| Dịch vụ | 1 conduit + saddle lặp 3 lần (`Art.cs:454–457`) | Thiếu ổ cắm, công tắc, junction box, ống nước, sprinkler, smoke detector, loa, cable tray |
| Trim | `BaseTrim` là hộp; handrail là hộp | Không profile, không bóng đổ, không mối nối, không bong tróc |
| Lặp | 4 bay hành lang giống hệt qua `foreach` (`Art.cs:445–450`) | Mắt đọc được pattern trong 2 giây; mất cảm giác "công trình thật đã sống" |
| Tĩnh | 665/665 GameObject có `m_StaticEditorFlags: 0` | Không static batching, không lightmap static, không occlusion |

### 2.3 Chuẩn 3A cần đạt

Một hành lang bệnh viện 3A không cần "nhiều đa giác" ở mọi bề mặt, nhưng cần **chi tiết đúng chỗ mắt người chơi đi qua**:

- **Tường:** bevel 2–4 cm ở mọi cạnh lộ; reveal cửa 10–15 cm; skirting profile 8–12 cm; corner guard kim loại; 2–3 lớp sơn vá lệch màu; vết nứt tóc; ố vàng quanh ổ điện.
- **Cửa:** frame + jamb + stop + threshold + 3 bản lề + tay nắm + closer + khe sáng dưới cửa; 3–6k triangle/cửa; collision riêng.
- **Cửa sổ:** frame nhôm/gỗ đúng profile, sill đá, gioăng, chấn song nếu có, tay nắm, rèm treo trên rail thật; 2–4k triangle/cửa.
- **Trần:** hệ trần thả với tee 24 mm, tile 600 × 600, 1–2 tile thiếu/ố, khoảng tối phía trên, đèn tube gắn vào grid.
- **Dịch vụ:** conduit theo tuyến thật, junction box, ổ cắm 2 loại cao độ, công tắc, bảng điện, sprinkler, smoke detector, loa treo tường, cable tray trên trần.
- **Bẩn có tác giả:** vệt nước chảy từ cửa sổ xuống theo trọng lực, gỉ dưới ốc, sơn bong theo cạnh va chạm, vết giày theo tuyến đi, vết cáng trên sàn.

### 2.4 Việc cần làm

| Ưu tiên | Việc | Chi phí | Tác động |
|---|---|---|---|
| P0 | Gắn static flags, lightmap static, occlusion cho toàn bộ kiến trúc | 0.5 ngày | Batching + GI nền |
| P0 | Thêm bevel 2–3 cm bằng modifier cho kit tường/cửa/trần | 1 ngày | Bắt sáng cạnh, hết "hộp giấy" |
| P1 | Làm kit modular: 6 module tường, 2 góc, 3 cửa, 2 cửa sổ, 1 trần | 3–4 tuần | Nâng cấp lớn nhất về kiến trúc |
| P1 | Thêm 1 lớp "cải tạo": vá tường, ổ điện mới, conduit chạy nổi, sơn khác màu | 1 tuần | Kể chuyện lịch sử công trình |
| P2 | Trần thả + dịch vụ + cable tray + đèn tube đúng cụm | 1–2 tuần | Chiều sâu thị giác khi ngước nhìn |

---

## 3. Đồ vật

### 3.1 Hiện trạng

- **Chỉ 3 FBX:** `CTCC_PROP_WardBed.fbx` (468 KB), `CTCC_PROP_BedsideCabinet.fbx` (80 KB), `CTCC_PROP_TransportTrolley.fbx` (209 KB).
- Mọi thứ còn lại là hình hộp: ghế băng (`Art.cs:479–480`), túi vải (`:563`), folder (`:567`), quầy, tủ, IV stand, monitor, quạt, giá treo, bảng hiệu.
- Rèm là mesh procedural + animation (`EnvironmentMotion.cs`), không có fabric shading, không translucency, không độ dày.
- Cây là 11 nhánh × 38 cụm, mỗi cụm 4 triangle, hai tông màu (`Art.cs:492–525`) — đọc rõ là card billboard khi nhìn gần.
- Không có hero prop nào đủ chi tiết để camera dừng lại: không monitor CRT còn sáng, không máy truyền dịch có nhãn, không tủ thuốc mở, không cáng gấp, không xe đẩy thuốc, không bảng bệnh án có chữ.

### 3.2 Hệ quả

Đồ vật hiện tại **đúng chức năng nhưng không đúng vật liệu**. Người chơi nhận ra "đây là cái ghế" nhưng không tin "đây là cái ghế đã nằm trong bệnh viện 7 năm". 3A horror sống bằng **vật dụng kể chuyện**: chiếc cốc còn vệt nước, tờ báo ngày cuối, đôi dép lệch nhau, dây truyền dịch còn treo, vết máu đã khô. Hiện tại những tín hiệu đó chủ yếu do **biển hiệu và decal** gánh, không do vật thể.

### 3.3 Chuẩn 3A

- **Mật độ:** 40–60 prop/phòng, trong đó 12–20 hero prop được phép cận cảnh.
- **Mỗi hero prop:** 3–8k triangle, high-poly bake sang low, weighted normals, bevel, 2–3 bộ UV (albedo/normal/ORM), decal riêng.
- **Mỗi prop phụ:** 300–1500 triangle, dùng trim sheet hoặc atlas chung.
- **Vật liệu đúng:** kim loại sơn bong lộ gỉ, gỗ sờn cạnh, vải nhăn theo trọng lực, nhựa ố vàng, cao su nứt.
- **Tương tác vật lý nhẹ:** rèm lay, giấy bay, đèn rung, dây truyền dịch đung đưa — không cần physics đắt, chỉ cần animation có trọng lượng.

### 3.4 Việc cần làm

| Ưu tiên | Việc | Chi phí |
|---|---|---|
| P0 | Thêm 10–15 prop cận cảnh: IV stand, monitor, bình oxy, tủ đầu giường có nội thất, dép, ca nước, điện thoại, quạt, đồng hồ, bảng bệnh án | 1–2 tuần |
| P1 | 40 prop/phòng theo kit; 12 hero prop có high-low bake | 4–6 tuần |
| P1 | Rèm có translucency + weighted normal; cây SpeedTree/billboard đúng khoảng cách | 1 tuần |
| P2 | Prop biến thể theo R/B: cùng collider, khác mesh/material/decal | 2 tuần |

---

## 4. Vật liệu và texture

Đây là **điểm yếu kỹ thuật nặng nhất** và cũng là chỗ sửa nhanh nhất.

### 4.1 Số liệu đo được

9 bộ texture, mỗi bộ 3 map 2048 × 2048. Số liệu đo trên file PNG nguồn, trước nén import:

| Bộ | Albedo — SD toàn cục | Albedo — SD high-pass | Normal — SD | Mask — kênh đáng chú ý |
|---|---:|---:|---:|---|
| PlasterR | 6.9 | 3.4 | 19.0 | G = 251.8 ± 1.34 (AO gần như trắng) |
| PlasterB | 1.5 | 1.4 | — | G gần như đồng nhất |
| FloorR | 23.6 | 15.5 | 2.5 | A = 116.5 ± 10.8 (smoothness) |
| DadoR / DadoB | 6.9 | 3.4 | 19.0 | gần như solid |
| Steel | 2.4 | 1.9 | 0.35 | R = 224, G = 239, A = 139.7 ± 2.15 |
| Cotton | 7.1 | 6.8 | 24.5 | G = 239, A = 40.3 ± 3.38 |
| Ceramic | 15.0 | 13.1 | — | gần như solid |

Không phải mọi map đều phẳng. FloorR và Ceramic có biến thiên albedo lớn (SD 23.6 / 15.0), high-pass vẫn còn 15.5 / 13.1 — tức có chi tiết tần số trung bình, không hoàn toàn phẳng. PlasterB và Steel gần như solid (1.5 / 2.4). Normal thì ngược lại: FloorR và Steel gần như phẳng (2.5 / 0.35) — đúng vào bề mặt cần phản xạ — trong khi PlasterR, DadoR và Cotton có chi tiết (19–25) nhưng bị pipeline và ánh sáng đều làm mất tương phản.

Toàn bộ 116 file texture nguồn: **108.4 MB**, trong đó **58 MB là normal map**. Ngân sách này không hẳn lãng phí, nhưng độ chi tiết không tương xứng: sàn/kim loại thiếu microdetail, còn tường có normal lại bị làm phẳng khi render.

Decal:

| Decal | Kích thước | Alpha trung bình | Alpha SD | Vấn đề |
|---|---:|---:|---:|---|
| `WaterRun` | 1024² | 14.0 | 24.2 | Là blob noise, không có hướng chảy, không có cạnh khô |
| `PaintLoss` | 1024² | 7.6 | 24.3 | Không có hình dạng bong sơn theo cạnh |
| `RustRun` | 1024² | 5.8 | 25.4 | Không có gốc gỉ và vệt chảy xuống |
| `SootSwatch` | 1024² | 28.5 | 58.9 | Không có hình khói/áp lực |
| `DampFloor` | 1024² | 5.7 | 14.4 | Gần như không thấy khi blend |
| `WheelMarks` | 768² | 6.6 | 27.5 | Dùng làm "report trace" phi vật lý |

Alpha trung bình 5.7–28.5/255, nhưng SD 14–59 cho thấy phân bố **thưa**: phần lớn pixel trong suốt, chỉ vài đốm sáng. Khi blend ở cường độ thấp, decal gần như **không nhìn thấy** trong build. Chúng còn là quad mesh đặt sát bề mặt (`HospitalAuthoring.Art.cs:482–489`), không phải decal projector, nên không bám theo hình học, dễ z-fighting và không có normal blending.

### 4.2 Lỗi pipeline

**(a) `_Smoothness` ghi đè tham số tác giả.**  

`HospitalAuthoring.Art.cs:117–137`:

```csharp
mat.SetFloat("_Smoothness", smooth);          // dòng 123: giá trị tác giả
...
mat.SetTexture("_MetallicGlossMap", Tex(maps + "_Mask"));
mat.SetTexture("_OcclusionMap", Tex(maps + "_Mask"));
mat.SetFloat("_OcclusionStrength", .78f);
mat.SetFloat("_Smoothness", .93f);             // dòng 134: ghi đè
```

Trong URP 17, khi có `_METALLICSPECGLOSSMAP`, shader nhân alpha của mask với `_Smoothness` (`LitInput.hlsl:146: specGloss.a *= _Smoothness;`). Vì vậy:

- Mọi giá trị `smooth:` ở `Art.cs:46–65` (0.23–0.66) **bị bỏ qua**.
- Độ nhám thực tế = `A/255 × 0.93`: Plaster ≈ 0.255, Floor ≈ 0.425, Steel ≈ 0.51.
- Không có sự phân biệt giữa tường sơn, sàn gạch, vải, kim loại — tất cả do alpha gần như đồng nhất trong từng bộ quyết định.

**(b) Occlusion dùng sai kênh.**  

`_OcclusionMap` trỏ vào cùng mask, lấy kênh G. Với Plaster G ≈ 251.8/255 = 0.987, `_OcclusionStrength = .78` gần như **không tạo ra occlusion nào**. Đáng lẽ G phải là AO bake (0–255 có dao động), hoặc không dùng occlusion map.

**(c) Normal map không đồng đều, nhưng map quan trọng lại phẳng.**  

FloorR và Steel — hai bề mặt chiếm phần lớn khung hình và cần phản xạ — có SD chỉ 2.5 và 0.35, gần như không có gradient. PlasterR, DadoR và Cotton có SD 19–25, tức có chi tiết hình học, nhưng `_BumpScale = .75` cùng ánh sáng đều và SSAO yếu khiến chi tiết này gần như không đọc được. Kết quả: sàn và kim loại trượt sáng như nhựa, còn tường có normal cũng không tạo được micro-shading.

**(d) Texture tiling lặp.**  

Cùng một bộ 2K trải với tile 2.3 m trên tường dài hàng chục mét. Không có macro variation, không có detail map, không có vertex color. Ở góc nhìn 1–2 m, mắt đọc được ô lặp.

**(e) Decal không phải decal.**  

Quad `Flat()` không nhận normal của bề mặt, không có fade theo góc nhìn, không có screen-space sorting. Với alpha thấp, chúng vừa vô hình vừa có nguy cơ nhấp nháy.

### 4.3 Hệ quả hình ảnh

- **B mất tương phản vật liệu:** tường, sàn, tủ, giường đều có cùng kiểu highlight mềm; cảnh trông "overexposed và dẻo".
- **R thiếu chiều sâu:** màu teal đồng nhất, không có điểm nhấn kim loại/gỗ/vải; sương mờ làm phẳng thêm.
- **Không có contact dirt:** không có bụi ở chân tường, không có gỉ ở bản lề, không có vệt tay ở tay nắm.
- **Sai vật lý thị giác:** sàn bệnh viện ướt nhưng không phản xạ đúng; kim loại không có anisotropic highlight; vải không có sheen.

### 4.4 Chuẩn 3A

| Thành phần | Mức 3A |
|---|---|
| Texel density | 512–1024 px/m cho tường; 1024–2048 px/m cho hero prop |
| Albedo | Có biến thiên màu thật, không chỉ noise; 3 lớp bẩn (đáy, cạnh, va chạm) |
| Normal | Bake từ high-poly hoặc scan; SD đủ lớn để bắt sáng; detail normal thứ hai |
| Roughness | Dải 0.15–0.9, có breakup theo vật liệu và vị trí |
| AO | Bake theo hình học, không dùng mask phẳng; kênh G có dao động |
| Decal | Projector 2K–4K, atlas 8–12 loại, có normal/roughness; đặt theo vật lý |
| Variation | 2–3 biến thể mỗi vật liệu; vertex color cho bẩn cục bộ |

### 4.5 Việc cần làm

| Ưu tiên | Việc | Chi phí | Tác động |
|---|---|---|---|
| **P0** | Bỏ `_Smoothness = .93`; trả lại `smooth` tác giả hoặc đặt 1.0 và bake đúng alpha | 2 giờ | Sửa ngay "nhựa hóa" |
| **P0** | Bỏ `_OcclusionMap` hoặc bake lại G đúng; đặt `_OcclusionStrength` theo dữ liệu | 2 giờ | Hết AO giả |
| **P0** | Tăng `_BumpScale` lên 1.0 sau khi có normal thật; thêm detail normal | 1 ngày | Microdetail |
| **P0** | Thay 6 decal noise bằng decal authored (nước chảy, gỉ, bong sơn, giày, cáng, ố) | 3–5 ngày | Bẩn có lý do |
| P1 | Author lại 9 bộ texture: albedo có variation, normal bake, mask đúng kênh | 2–3 tuần | Nâng cấp lớn nhất về vật liệu |
| P1 | Thêm macro variation + vertex color + detail map | 1 tuần | Hết lặp |
| P2 | Chuyển sang quy trình high-poly → low-poly bake cho toàn bộ hero prop | 4–6 tuần | Chất lượng 3A thật |

---

## 5. Ánh sáng và hậu kỳ

### 5.1 Hiện trạng

| Thành phần | Giá trị | Đánh giá |
|---|---|---|
| Tổng đèn | 20 | Đủ số lượng cho một phòng + hành lang |
| Đèn có bóng | 2/20 (`m_Shadows: Soft`) | Chỉ moon và một beam; 18 đèn còn lại `None` |
| Bóng đổ của đèn trong | `LightShadows.None` tại `HospitalAuthoring.Lighting.cs:133` | Đây là **lựa chọn code**, không phải giới hạn pipeline |
| Pipeline | Forward+ (`PC_Renderer.asset:56`) | Hỗ trợ nhiều đèn; additional shadow atlas 2048 |
| Lightmap | Không có `m_LightmapIndex` trong scene | Không có GI bake |
| APV | Tắt/không dùng | Không có probe volume |
| Ambient | `m_AmbientMode: 3` (Flat), sky 0.33/0.41/0.43 | Ánh sáng nền phẳng, không đổi theo không gian |
| SSAO | Intensity 0.4, Radius 0.3, Samples 1, NormalSamples 1 | Gần như không có contact shadow |
| Reflection | 1 probe 128 px, realtime qua script, box 9.2 × 3.6 × 6.12 | Độ phân giải thấp, chỉ phòng 101 |
| Fog | Exponential², density 0.024 | Sương đều, không có height fog/volumetric |
| Light shaft | Mesh giả (`beam`) | Không tương tác với camera/không khí |
| Post FX | ACES, Color Adjustments, Bloom 0.24/1.09/0.56, Vignette | Thiếu grain, DOF, LUT riêng, lens |
| Render scale | 0.88, MSAA 1x | Giảm chất lượng nền, đặc biệt cạnh |

### 5.2 Vì sao cảnh trông phẳng

1. **Không có contact shadow.** Vật thể không có bóng tiếp xúc với sàn/tường. Mắt người đọc độ cao và trọng lượng chủ yếu bằng contact shadow; thiếu nó, mọi thứ "lơ lửng".
2. **Không có GI.** Ánh sáng chỉ đến trực tiếp từ đèn + ambient phẳng. Không có bounce màu từ sàn lên tường, không có vùng tối tự nhiên ở góc phòng.
3. **Bóng đổ quá ít.** Một cảnh có 20 đèn nhưng chỉ 2 đổ bóng thì mọi bề mặt đều nhận sáng đều, làm mất khối.
4. **SSAO quá yếu.** 1 sample, radius 0.3 m, intensity 0.4: chỉ đủ làm tối nhẹ ở khe, không đủ tạo cảm giác tiếp xúc.
5. **Reflection probe 128 px.** Sàn ướt cần reflection sắc nét hơn; 128 px làm phản xạ thành vệt mờ, không đọc được hình.
6. **Hậu kỳ "an toàn".** Bloom nhẹ, vignette nhẹ, không grain/DOF/LUT riêng. Kết quả là ảnh sạch nhưng vô hồn.

Đối chiếu ảnh chụp: ở `room101-B.png`, vùng cửa sổ và tường phía trên gần như trắng, mất chi tiết; bóng đồ vật mềm và không có contact shadow; sàn bóng đều như nhựa. Ở `room101-R.png`, toàn ảnh ngả teal, sương làm mất tương phản xa/gần; tán cây ngoài cửa sổ là các mảng phẳng; không có vệt nước đọng đúng vật lý. `corridor-R.png` cho thấy nhịp lặp của 4 bay, đèn tube không tạo pool sáng rõ và khung cửa không đổ bóng.

### 5.3 Chuẩn 3A

- **GI bake** cho toàn bộ kiến trúc tĩnh, cộng APV cho prop động.
- **4–8 đèn đổ bóng** trong vùng chơi, shadow 512–1024 mỗi đèn, soft shadow đúng.
- **Contact shadow** từ SSAO chất lượng cao hoặc ray-traced shadow.
- **Volumetric fog** có noise, height fog, light shaft thật.
- **Reflection:** probe 256–512 cho phòng, planar reflection cho sàn ướt.
- **Post:** LUT riêng theo R/B, film grain, DOF nhẹ, bloom đúng ngưỡng, exposure lock, chromatic aberration rất nhẹ, lens dirt.
- **Ánh sáng kể chuyện:** đèn tube nhấp nháy có nhịp, đèn exit xanh lá, đèn phòng cấp cứu đỏ, bóng người đi qua cửa.

### 5.4 Việc cần làm

| Ưu tiên | Việc | Thông số đề xuất | Chi phí |
|---|---|---|---|
| **P0** | Bật bóng cho 6–8 đèn chính | `LightShadows.Soft`, 512–1024 | 0.5 ngày |
| **P0** | Tăng SSAO | Samples 1 → 4, NormalSamples 1 → 2, Radius 0.3 → 0.55, Intensity 0.4 → 0.85 | 1 giờ |
| **P0** | Bake lightmap/APV cho kiến trúc | Mixed lighting, 2–3 bounces | 2–3 ngày |
| **P0** | Thêm contact shadow decal dưới 20 prop | AO decal 256–512 | 1 ngày |
| P1 | Nâng reflection probe 128 → 256/512; thêm planar reflection cho sàn | 1–2 ngày | |
| P1 | Volumetric fog + light shaft thật | Renderer feature/custom | 1 tuần |
| P1 | LUT riêng R/B + grain + DOF + lens | 2–3 ngày | |
| P2 | SSGI/RTGI, đèn nhấp nháy có animation, đèn khẩn cấp | 2–4 tuần | |

**Lưu ý kỹ thuật:** `PC_RPAsset.asset` đã bật `m_AdditionalLightShadowsSupported: 1` và atlas 2048. Với 6 đèn đổ bóng, mỗi đèn còn ~256–512 px — đủ cho phòng nhỏ. Nếu cần nét hơn, nâng atlas lên 4096 hoặc dùng một đèn key có bóng + bake cho phần còn lại.

---

## 6. Cảnh quan và thời tiết

### 6.1 Hiện trạng

- Sân sau là vài hộp đất, đường đi, canopy (`Art.cs:461–466`).
- 9 cây procedural, mỗi cây 11 nhánh × 38 cụm, mỗi cụm 4 triangle, hai tông màu (`Art.cs:467–469`, `492–525`). Nhìn từ cửa sổ, tán lá đọc như **các mảng card phẳng**.
- Mưa là particle + audio loop; không có ripple, splash, wetness mask hay nước chảy trên kính.
- Cửa sổ không có droplet shader, không có hơi nước, không có phản xạ cảnh ngoài.
- Không có skybox động/thời gian; fog đều một màu.
- Cảnh ngoài chỉ chiếm khoảng 10% góc nhìn, nhưng lại là **khung hình hero** khi người chơi nhìn ra cửa sổ phòng 101 — nơi đáng đầu tư nhất.

### 6.2 Chuẩn 3A

- Cây: SpeedTree hoặc mesh 3 tầng LOD, tán có translucency, wind vertex animation.
- Mưa: hạt + splash particle + ripple decal trên vũng + wetness mask tăng dần + âm thanh theo mái/tôn.
- Cửa sổ: droplet shader, vệt nước chảy, hơi nước, phản xạ cảnh ngoài.
- Sân: vật liệu ướt, vũng có planar reflection, cỏ dại, rác, vết bánh xe, cột điện, dây.
- Không khí: volumetric mist ở nền, ánh sáng exit xanh, đèn sân vàng.

### 6.3 Việc cần làm

| Ưu tiên | Việc | Chi phí |
|---|---|---|
| P0 | Thay cây card bằng 2–3 mesh cây có LOD; thêm wind | 2–3 ngày |
| P0 | Thêm ripple/splash decal + wetness mask cho sân | 2 ngày |
| P1 | Droplet shader cho cửa sổ + hơi nước | 1 tuần |
| P1 | Vũng nước có planar reflection + rác/cỏ dại | 1 tuần |
| P2 | Skybox/time of day, ánh sáng ngoài có nhịp | 2 tuần |

---

## 7. Gameplay và UX

### 7.1 Điểm mạnh

- **Máy trạng thái chắc.** `Protocol.cs`, `Casework.cs`, `RealityTransaction.cs`, `AtomicSaves.cs` cho thấy tư duy hệ thống tốt: evidence, gate, revision, rollback, backup.
- **Đã kiểm các ca khó:** 30 lần từ chối đổi tại ngưỡng, 30 lần đổi tại neo, rollback khi collider chèn, 10 lần reload, lỗi I/O sau flush (`verification.md:26–28`).
- **Hai lớp hiện thực R/B** là cơ chế độc đáo, có tiềm năng kinh dị tâm lý mạnh nếu được khai thác bằng hình ảnh và âm thanh, không chỉ bằng text.
- **Accessibility cơ bản có thật:** Tab/Enter/Esc, text scale 130%, alternate bindings, tắt âm, giảm chuyển động (`verification.md:29`).

### 7.2 Điểm yếu

| Hạng mục | Hiện tại | Thiếu gì để thành kinh dị 3A |
|---|---|---|
| Vòng lặp lõi | Đi, tương tác, đọc, chọn đáp án | AI hunter, truy đuổi, trốn, damage, sợ hãi, tài nguyên |
| Tương tác | Raycast 2.55 m, một phím, tức thời (`FirstPerson.cs:143`) | Hold-to-interact, xoay vật phẩm, animation tay, haptic |
| Đèn pin | 360 giây, không pickup (`FirstPerson.cs:19`, `:126`) | Pin nhặt được, nhấp nháy, tiếng rè, đổi màu theo pin |
| Di chuyển | 2.2/4.2/1.2 m/s, tức thời (`:110`) | Gia tốc, quán tính, lean, headbob, tiếng bước theo vật liệu |
| Stamina | 7 giây, hồi 7 giây (`:16`, `:103`) | Nhịp thở, giảm tốc, ảnh hưởng khi sợ |
| Kẻ địch | Không có | AI state machine, hearing, sight, chase, search, patrol |
| Trốn | Không có | Hiding spot, peek, hold breath, heartbeat |
| Inventory | Không có | Grid/túi, combine, examine, pin/medical |
| Câu đố | Chỉ form/evidence | Puzzle vật lý, khóa số, dây điện, tủ thuốc |
| Save | Atomic, tốt | Save room, giới hạn save, tension |
| UI | uGUI, 2 font, không TMP | Diegetic UI, subtitle speaker, gamepad, settings đầy đủ |

### 7.3 UX và accessibility

- Chuột: `delta × .079` cố định (`FirstPerson.cs:87`), **không có sensitivity setting**.
- Không có gamepad; không remap đầy đủ, chỉ có bộ phím thay thế.
- Không có gamma/brightness calibration — điều bắt buộc với game kinh dị tối.
- Chỉ có master volume 72%/tắt; không có slider riêng cho VO/Music/SFX.
- Không có subtitle theo người nói, không có size/background option.
- Không có photosensitivity warning/option; nếu P1 thêm đèn nhấp nháy thì bắt buộc phải bổ sung.
- Font Be Vietnam Pro ổn về tiếng Việt, nhưng uGUI `Text` không bằng TMP về crispness và layout.

### 7.4 Ưu tiên

| Ưu tiên | Việc | Chi phí |
|---|---|---|
| **P0** | Thêm sensitivity, gamma calibration, 3 volume slider, subtitles size/background | 2–3 ngày |
| **P0** | Interaction hold 0.3–0.6 s + highlight + cursor đổi | 2 ngày |
| P1 | Một prototype kinh dị: 1 AI, 1 chase, 1 hiding spot, 1 resource | 3–4 tuần |
| P1 | Gamepad + full remap + photosensitivity | 1–2 tuần |
| P2 | Inventory, puzzle, save room, AI director | 2–3 tháng |

---

## 8. Âm thanh

### 8.1 Hiện trạng

- **8 AudioSource**, 7 loop + 1 prop cue (`HospitalAuthoring.Lighting.cs:290–302`): Rain, Roof, Rumble, Drips, Fan ×2, Tube.
- 5 one-shot: Paper, Stamp, Slipper, Step, Switch.
- **1 AudioReverbZone** "Short tiled room reflections" (`:303–305`).
- Không có mixer group, snapshot, ducking, occlusion, low-pass theo tường.
- Tiếng bước dùng chung một clip, không phân biệt gạch/sàn ướt/vải.
- Không có VO; thoại bằng text.
- Không có ambience layer theo R/B khác nhau (chỉ đổi hình ảnh).

### 8.2 Hệ quả

Tai người chơi nhận ra ngay "prototype": tiếng mưa đều, không có tiếng nước chảy vào ống, không có tiếng đèn tube rè, không có tiếng bước vọng hành lang, không có tiếng cửa gỗ nặng. Trong kinh dị, **âm thanh chiếm 50% nỗi sợ**; hiện tại nó mới ở mức "có tiếng nền".

### 8.3 Chuẩn 3A

- FMOD/Wwise, 40–80 source hoạt động đồng thời, bus theo không gian.
- Convolution reverb theo phòng, occlusion theo tường/cửa, low-pass khi qua góc.
- Footstep 6–8 bộ theo vật liệu, có random pitch/volume, có tiếng vải.
- Ambience 3 lớp: ngoài trời, công trình, tâm lý (drone).
- VO đầy đủ, subtitle theo speaker, ducking music/SFX.
- Dynamic mix: khi chase, giảm ambience, tăng heartbeat/breath.

### 8.4 Việc cần làm

| Ưu tiên | Việc | Chi phí |
|---|---|---|
| P0 | Thêm 2 reverb zone (hành lang, kho), low-pass occlusion đơn giản | 2 ngày |
| P0 | 3 bộ footstep (gạch khô, gạch ướt, vải) + random | 2 ngày |
| P0 | 10–15 one-shot có địa điểm: cửa, tủ, giường, đèn, nước | 3 ngày |
| P1 | FMOD/Wwise + mixer snapshot + ducking | 2–3 tuần |
| P2 | VO, foley, dynamic mix, AI hearing | 1–2 tháng |

---

## 9. Đối chiếu với kinh dị 3A

| Tiêu chí | M1 hiện tại | 3A tham chiếu (RE2/RE7/SH2/AW2) | Khoảng cách |
|---|---|---|---|
| Mật độ hình học | Chủ yếu cube/quad; 3 FBX | 500k–2M triangle/khung hình, hero prop 3–8k | Rất lớn |
| Vật liệu | 9 bộ 2K, chi tiết không đều, smoothness lỗi | 4K hero, 2K mid, ORM đúng, decal projector | Rất lớn |
| Ánh sáng | Ambient phẳng + 2 đèn bóng | GI bake + SSGI/RT + volumetrics | Rất lớn |
| Bóng đổ | 18/20 đèn không bóng | 4–8 đèn bóng + contact shadow | Lớn |
| Hậu kỳ | Bloom/vignette/ACES | LUT riêng, grain, DOF, lens, exposure | Lớn |
| Cảnh quan | Hộp + cây card | SpeedTree, mưa tương tác, wetness | Lớn |
| Âm thanh | 8 source, 1 reverb | FMOD/Wwise, 40–80 source, occlusion | Rất lớn |
| Animation | Không có tay/nhân vật | Mocap, facial, IK | Đặc biệt lớn |
| AI/kinh dị | Không có | Patrol/chase/hide/director | Đặc biệt lớn |
| Hiệu năng | 21.62 ms/frame, 0.88 scale | 16.67 ms có upscaling | Trung bình |

**Kết luận đối chiếu:** M1 đang ở mức **"AA prototype"**, không phải **"3A realistic"**. Điều này không có nghĩa là dự án thất bại: vertical slice chứng minh được pipeline, điều khiển, trạng thái và nghệ thuật bố cục. Nhưng nếu mục tiêu là 3A, cần một **chương trình sản xuất mỹ thuật 6–9 tháng**, không phải một đợt tinh chỉnh.

---

## 10. Lộ trình nâng cấp

### P0 — "Cứu hình ảnh" (1–2 tuần, 1 dev + 1 artist)

Mục tiêu: nâng điểm cảm nhận từ 3.9 lên **5.2–5.5**, giảm frame time xuống **17–18 ms**, không đổi canon.

| # | Việc | File/thông số | Kết quả |
|---|---|---|---|
| 1 | Sửa smoothness | `Art.cs:134` bỏ `.93`; dùng `smooth` hoặc 1.0 | Hết nhựa hóa |
| 2 | Sửa occlusion | `Art.cs:132` bỏ map hoặc bake G | Hết AO giả |
| 3 | BumpScale 1.0 + detail normal | `Art.cs:130` | Microdetail |
| 4 | SSAO 4 sample, radius 0.55, intensity 0.85 | `PC_Renderer.asset` | Contact shadow |
| 5 | Bật bóng 6 đèn | `Lighting.cs:133` | Khối và chiều sâu |
| 6 | Static flags + lightmap/APV | scene/renderer | Batching + GI |
| 7 | Decal authored + projector | `Art.cs:482–489` | Bẩn có lý do |
| 8 | Reflection probe 256/512 | `Lighting.cs:102` | Sàn ướt đọc được |
| 9 | LUT + grain + DOF nhẹ | `Lighting.cs:172–200` | Điện ảnh |
| 10 | 10 hero prop | `Art.cs` | Điểm neo thị giác |
| 11 | 3 bộ footstep + 2 reverb | `FirstPerson.cs`, `EnvironmentalAudio.cs` | Tai nghe tốt hơn |
| 12 | Batching/LOD/static | scene | Giảm draw call |

**Gate P0:** 3 ảnh chụp (1 m / 3 m / 10 m) qua đánh giá mù với reference; frame P95 ≤ 18 ms ở 1080p; không regression save/evidence.

### P1 — "Vertical slice đáng tin" (8–10 tuần, 2–3 artist + 1 dev)

Mục tiêu: **6.8–7.2/10**, đủ để gọi là "AA+".

- Kit kiến trúc modular: 6 tường, 2 góc, 3 cửa, 2 cửa sổ, trần thả, dịch vụ.
- 40 prop/phòng, 12 hero; high-low bake; 3 biến thể màu.
- Author lại 9 bộ texture; decal atlas 4K; vertex color bẩn.
- Lighting redesign: GI bake + APV + 6 đèn bóng + volumetric + planar reflection.
- Audio middleware, 30 source, occlusion, ducking.
- Controller polish: accel, lean, headbob, footsteps theo vật liệu.
- Settings đầy đủ: sensitivity, gamma, 3 volume, gamepad, photosensitivity.
- Một prototype kinh dị: AI, chase, hiding, resource.

**Gate P1:** 60 fps ở 1080p trên GPU tầm trung; 3 playtest người mới; art review TASK026–028; không còn "prototype look" trong ảnh chụp.

### P2 — "3A realistic" (4–6 tháng, team 8–12)

Mục tiêu: **8.0–8.5/10**.

- Toàn bộ hero asset high-low bake/scan; photogrammetry cho vật liệu chủ đạo.
- HDRP evaluation hoặc URP high-end path; SSGI/RTGI; volumetric fog; nước mưa tương tác.
- AI director, animation mocap, facial, VO đầy đủ.
- 10–12 giờ nội dung; console parity; localization.
- Pipeline QA: art lock, performance lock, regression tự động.

**Gate P2:** so sánh mù với 3A cùng thể loại; 30/60 fps ổn định trên min-spec; đánh giá người chơi mục tiêu.

---

## 11. Ngân sách hiệu năng

### 11.1 Hiện tại

| Phép đo | Giá trị | Ghi chú |
|---|---|---|
| P95 render-to-texture + đọc 1 pixel | 17.63 ms | Gồm sync readback, không phải GPU time thuần |
| P95 frame | 21.62 ms | Vượt ngân sách 16.67 ms |
| Draw call đỉnh | 593 | 531 MeshRenderer, 58 material |
| Unity allocated | 106.41 MiB | |
| Render scale | 0.88 | Giảm chất lượng nền |
| MSAA | 1x | Cạnh răng cưa |
| Shadow distance | 32 m | Quá xa cho phòng 8 m |
| Additional shadow atlas | 2048 | Chia cho nhiều đèn |

**Cảnh báo phương pháp:** 17.63 ms có readback đồng bộ, có thể cao hơn GPU time thật. Nhưng 21.62 ms frame vẫn vượt 16.67 ms, nên kết luận "chưa đạt 60 fps" là đúng. Cần đo lại bằng Nsight/RenderDoc trước khi tối ưu.

### 11.2 Ngân sách đề xuất

| Hạng mục | 1080p60 | 1440p60 | 4K30 |
|---|---:|---:|---:|
| GPU frame | ≤ 13.0 ms | ≤ 13.0 ms | ≤ 28.0 ms |
| CPU frame | ≤ 3.0 ms | ≤ 3.0 ms | ≤ 5.0 ms |
| Draw call | ≤ 1200 | ≤ 1200 | ≤ 1500 |
| Triangle/khung hình | ≤ 2.5 M | ≤ 3.5 M | ≤ 6 M |
| Shadow caster | ≤ 150 | ≤ 150 | ≤ 200 |
| Đèn/khung hình | 12–16 Forward+ | 12–16 | 16–20 |
| Texture stream | 1.5–2 GB | 2–3 GB | 3–4 GB |
| Post FX | ≤ 2.5 ms | ≤ 2.5 ms | ≤ 4 ms |

**Chiến lược:** 3A realistic ở 60 fps gần như bắt buộc dùng upscaling (DLSS/FSR/XeSS) và dynamic resolution. Nếu mục tiêu là 30 fps console, ngân sách thoải mái hơn nhiều; nên chốt trước khi art lock.

### 11.3 Bài học từ engine

`engine.md` ghi P95 1080p: URP 14.08 ms, HDRP 36.40 ms trên Intel Arc tích hợp. HDRP cho hình ảnh tốt hơn nhưng **đắt gấp 2.6 lần** trên cùng cảnh. Với mục tiêu 3A realistic, HDRP là lựa chọn hợp lý cho PC cao cấp, nhưng cần GPU rời và upscaling. Không nên chuyển HDRP trước khi tối ưu URP, vì sẽ che lấp lỗi pipeline hiện tại.

---

## 12. Rủi ro và quyết định cần chốt

### 12.1 Xung đột canon

GDD hiện tại ghi mục tiêu **"semi-realistic tối ưu asset"**, không phải photoreal. Core 05 quy định 1K–2K texture, modular kit, R/B/M variants, art lock sau TASK026–028. Yêu cầu "3A realistic" là **thay đổi art direction**, cần một DEC mới, không nên âm thầm đổi canon.

### 12.2 Ba lựa chọn

| Phương án | Nội dung | Chi phí | Rủi ro |
|---|---|---|---|
| **A. Nâng canon lên 3A realistic** | Đổi GDD, ngân sách, pipeline, thời gian | Cao | Scope creep, trễ milestone |
| **B. Dual target** | M1/M2 giữ semi-realistic; nhánh "High Fidelity" cho PC/next-gen | Trung bình | Hai pipeline asset, phức tạp QA |
| **C. Chuyển HDRP** | Đổi engine render cho toàn dự án | Cao | Hiệu năng, thời gian đào tạo, rủi ro kỹ thuật |

### 12.3 Khuyến nghị

**Chọn B cho M1/M2, đánh giá A tại gate M2.** Lý do:

- M1 đang trong nghiệm thu; đổi canon ngay sẽ làm mất mốc so sánh.
- P0 có thể đạt 5.2–5.5 mà không đổi canon, đủ để qua art lock.
- P1 xây pipeline modular — nền tảng cho cả semi-realistic lẫn 3A.
- Tại gate M2, có dữ liệu hiệu năng và chi phí thực tế để quyết định A/C.

### 12.4 Rủi ro chính

| Rủi ro | Xác suất | Tác động | Giảm thiểu |
|---|---|---|---|
| Scope creep khi làm 3A | Cao | Trễ 3–6 tháng | Gate rõ, art lock, budget theo tuần |
| Thiếu artist có kinh nghiệm PBR/3A | Cao | Chất lượng không đều | Thuê ngoài hero asset, kit chuẩn hóa |
| Hiệu năng tụt khi thêm GI/volumetric | Cao | Không đạt 60 fps | Đo sớm, upscaling, LOD, budget |
| Mất save/evidence do refactor | Trung bình | Regression | Giữ test TASK005/006, atomic save |
| Bản quyền photogrammetry/asset | Trung bình | Pháp lý | ThirdPartyNotices, license audit |
| Ánh sáng tối quá mức | Trung bình | Người chơi bỏ | Gamma calibration, playtest |

### 12.5 Mẫu DEC

```
DEC-V2-XX: Mục tiêu mỹ thuật M2
- Bối cảnh: GDD hiện tại semi-realistic; yêu cầu 3A realistic.
- Quyết định: giữ semi-realistic cho M1/M2; mở nhánh High Fidelity.
- Hệ quả: +2 tuần pipeline, +1 artist; không đổi milestone M2.
- Gate: so sánh mù 3 ảnh; 60 fps 1080p; art review TASK026–028.
- Ngày hiệu lực: sau khi M1 đóng.
```

---

## Phụ lục A — Bảng bằng chứng

| File:dòng | Nội dung |
|---|---|
| `HospitalAuthoring.Art.cs:117–137` | Hàm `Lit`, gán map, smoothness, occlusion |
| `HospitalAuthoring.Art.cs:46–65` | Palette vật liệu và `smooth` tác giả |
| `HospitalAuthoring.Art.cs:97` | Danh sách decal noise |
| `HospitalAuthoring.Art.cs:443–490` | Chi tiết kiến trúc và decal |
| `HospitalAuthoring.Art.cs:492–525` | Cây procedural |
| `HospitalAuthoring.Art.cs:527–553` | Merge trim thủ công |
| `HospitalAuthoring.Art.cs:555–569` | Prop detail |
| `HospitalAuthoring.Lighting.cs:16` | Shadow distance 32 |
| `HospitalAuthoring.Lighting.cs:47–50` | Moon shadow |
| `HospitalAuthoring.Lighting.cs:97–108` | Reflection probe 128 |
| `HospitalAuthoring.Lighting.cs:126–133` | `Lamp` đặt `LightShadows.None` |
| `HospitalAuthoring.Lighting.cs:172–200` | Grade, bloom, vignette |
| `HospitalAuthoring.Lighting.cs:290–305` | Audio loop, one-shot, reverb |
| `HospitalAuthoring.cs:182` | `CreatePrimitive(Cube)` |
| `HospitalAuthoring.cs:272–278` | `WallOpening` chia hộp |
| `FirstPerson.cs:16` | Stamina 7 |
| `FirstPerson.cs:19`, `:126` | Pin 360 giây, không pickup |
| `FirstPerson.cs:44–48` | Đèn pin 48°, 13 m, 4.8 |
| `FirstPerson.cs:87` | Mouse delta × .079 |
| `FirstPerson.cs:110` | Tốc độ 1.2/2.2/4.2 |
| `FirstPerson.cs:143` | Tương tác 2.55 m |
| `HospitalUI.cs:47–48` | Hai font Be Vietnam Pro |
| `HospitalUI.cs:100–102` | Text scale 0.85–1.35 |
| `PC_Renderer.asset:56` | Forward+ |
| `PC_Renderer.asset:71–84` | SSAO 0.4/0.3/1 |
| `PC_RPAsset.asset:46–57` | Shadow 2048, distance 32 |
| `PC_RPAsset.asset:49–53` | Additional shadow hỗ trợ, tier 256/512/1024 |
| `AnSinh.unity` | 665 GameObject, 531 MeshRenderer, 20 Light, 8 AudioSource, 1 ReflectionProbe |
| `AnSinh.unity:23–27` | Ambient flat, intensity 1 |
| `AnSinh.unity` | 665/665 `m_StaticEditorFlags: 0`; 0 LODGroup; 0 LightProbeGroup |
| `verification.md:9` | M1 đang nghiệm thu hình ảnh/hiệu năng |
| `verification.md:47` | 17.63 ms / 21.62 ms / 593 DC / 106.41 MiB |
| `verification.md:51` | Ngân sách 60 fps 16.67 ms, đã vượt |
| `LitInput.hlsl:146` | `specGloss.a *= _Smoothness` |

## Phụ lục B — Thay đổi thông số đề xuất (P0)

```text
# Vật liệu
HospitalAuthoring.Art.cs:134   _Smoothness = .93f        -> 1.0f (hoặc bỏ, dùng smooth)
HospitalAuthoring.Art.cs:132   _OcclusionMap = mask      -> bỏ, hoặc bake lại G
HospitalAuthoring.Art.cs:133   _OcclusionStrength = .78  -> 0.0 nếu giữ map cũ
HospitalAuthoring.Art.cs:130   _BumpScale = .75f         -> 1.0f + detail normal

# SSAO (PC_Renderer.asset)
Intensity        0.4  -> 0.85
Radius           0.3  -> 0.55
Samples          1    -> 4
NormalSamples    1    -> 2
Downsample       0    -> 0 (giữ full res)

# Ánh sáng
HospitalAuthoring.Lighting.cs:133  LightShadows.None -> Soft cho 6 đèn chính
HospitalAuthoring.Lighting.cs:16   shadowDistance 32 -> 24
PC_RPAsset.asset:64               shadowNormalBias 0.5 -> 0.25

# Reflection
HospitalAuthoring.Lighting.cs:102  resolution 128 -> 256/512

# Hậu kỳ
Bloom intensity 0.24 -> 0.35; threshold 1.09 -> 0.9
Thêm Film Grain 0.12–0.2; DOF nhẹ; LUT riêng R/B

# Điều khiển
FirstPerson.cs:87  delta * .079 -> delta * sensitivity (0.03–0.12, mặc định .079)
FirstPerson.cs:47  range 13 -> 18; bật shadow cho đèn pin
FirstPerson.cs:110 thêm gia tốc 8–12 m/s², giảm tốc 10–14 m/s²

# Scene
Bật StaticEditorFlags cho kiến trúc; bake lightmap/APV; LOD cho prop lớn
```

---

## Kết luận

M1 đã chứng minh được **dự án có thể chạy**: điều khiển, tương tác, trạng thái, lưu, hai lớp hiện thực và bố cục đều hoạt động. Nhưng để đạt "kinh dị 3A realistic", cần nhìn thẳng vào ba khoảng cách lớn nhất: **hình học nguyên thủy**, **vật liệu không được khai thác đúng**, và **ánh sáng không có GI/contact shadow**. Ba thứ này không thể vá bằng hậu kỳ; phải sửa ở pipeline sản xuất.

Việc đúng tiếp theo không phải là thêm hiệu ứng, mà là:

1. **Sửa lỗi vật liệu P0** (2 ngày) để ngừng làm hỏng mọi bề mặt.
2. **Bật bóng + SSAO + bake GI** (1 tuần) để cảnh có khối.
3. **Thay 10 prop cận cảnh + 1 bộ decal authored** (2 tuần) để có điểm neo thị giác.
4. **Đo lại hiệu năng bằng GPU capture** trước khi hứa 60 fps.

Sau P0, nếu hình ảnh vẫn chưa đủ, đó là lúc cần kit modular và hero asset — không phải lúc thêm bloom.
