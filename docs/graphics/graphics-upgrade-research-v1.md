# Nghiên cứu nâng cấp đồ họa v1 (đề xuất, chưa chốt DEC)

2026-11-27 · Baseline: nhánh hiện tại sau `0b5b0a9` · Unity `6000.3.28f1` · URP 17.3 Forward+ · D3D12 · Core v3.6.2

> **Trạng thái:** Đề xuất ngoài Backlog. Chưa có DEC, chưa sửa asset, chưa có số đo mới. Mọi con số "baseline" lấy từ hồ sơ có sẵn trong repo. Mọi hiệu quả của phương án mới ghi **U** (chưa đo) cho tới khi có bằng chứng theo build.

## 1. Hiện trạng kiểm kê được từ mã và asset

| Hạng mục | Giá trị hiện có | Nguồn |
| --- | --- | --- |
| Pipeline | URP 17.3 Forward+, HDR bật, MSAA tắt, SRP Batcher bật | `Assets/Settings/PC_RPAsset.asset`, `ProjectSettings/QualitySettings.asset` |
| AA | SMAA Medium gắn trên camera khi dựng scene | `HospitalAuthoring.Lighting.cs:45-46` |
| Renderer | **Một** `PC_Renderer` dùng chung cho cả Low/Medium/High: Decal (Screen, technique 1) + SSAO (Depth Normals, Intensity 0,86, Radius 0,56, Blur High, Falloff 100) | `PC_Renderer.asset`, cả ba `*_RPAsset` trỏ cùng GUID renderer `f288ae1f…` |
| Chênh lệch Low / Medium / High | Chỉ ở render scale (0,76 / 0,88 / 1,0), shadow map (1024/2048/2048 main; 2048/2048/4096 phụ), shadow distance 17/24/24, LOD bias 1,1/1,6/2,0 | ba file `*_RPAsset`, `QualitySettings.asset` |
| Bóng | Một cascade, soft shadow quality 3, main 2048 | `PC_High_RPAsset.asset` |
| Ánh sáng gián tiếp | Hai LightProbe bake riêng R và B (399 điểm mỗi lớp), Reflection Probe realtime cập nhật bằng script, `ambientMode = Trilight` | `HospitalAuthoring.Lighting.cs` |
| Hậu kỳ | Volume theo lớp R/B: filter, vignette 0,14–0,21, grain 0,07–0,13, CA 0,012–0,022, shadow/highlight tone, bloom 0,14–0,21 | `HospitalAuthoring.Lighting.cs:193-197` |
| Khí quyển | Fog ExponentialSquared 0,0082, mưa, giọt mái, gợn nước, volumetric cone giả, bụi | `HospitalAuthoring.Lighting.cs`, `docs/ch01r/art-production.md` |
| Tuỳ chỉnh trong game | Nút xoay Low → Medium → High, lưu `CTCC.GraphicsQuality` | `HospitalGame.Pages.cs:231-235` |

**Kết luận kiểm kê:** Nền kỹ thuật đã gần mức cao nhất mà URP cho phép (probe riêng R/B, decal, SSAO, SMAA, hậu kỳ theo lớp). Phần "nâng cấp" còn lại không phải bật thêm vài hiệu ứng mà là (a) thoát khỏi trần hiệu năng và (b) làm cho mức **High** thực sự khác Medium.

## 2. Ràng buộc đã khóa (không được phá)

1. **Ngân sách khung hình.** Baseline M1 phòng 101 là 17,63 ms ở 1080p Medium, mục tiêu ≤ 16,6 ms (`TASK031-D`, Pass: T07 và WORLD-Q04). Cổng 60 fps toàn tuyến CH01→CH02 vẫn mở (`docs/ch01r/art-production.md`). Mọi tính năng thêm vào **Medium** là rủi ro cho cổng này.
2. **HDRP bị loại.** P95 render ngoài màn hình 36,40 ms so với URP 14,08 ms trên Intel Arc iGPU, bộ nhớ gấp đôi (`docs/prototype/engine.md`). Không đề xuất đổi pipeline.
3. **Art lock chưa mở.** Core 05: sản xuất artwork chờ `TASK026–028` và `CHAR-Q02/Q09`. Đề xuất này chỉ chạm cấu hình render, không đổi model, vật liệu PBR đã kiểm hay nhân vật (nguyên tắc Delta tối thiểu của Core 05).
4. **Đọc được.** Chữ F101, E01, thẻ chứng cứ và `INNER_SUB_SILENT` (≥ 4,5:1) phải đọc được ở mọi font scale. Mọi thay đổi grain, bloom, vignette, CA phải qua lại ca này.
5. **Reduced motion và tăng sáng** phải vẫn hoạt động (không thêm hiệu ứng chuyển động không có công tắc).
6. **Hồ sơ nghiệm thu.** Dùng ca QA có ID và số đo theo build: T07, WORLD-Q04/Q05/Q06, AR01–03, CHAR-Q01/Q04–Q08. Chuyển niên đại/công năng cho review chuyên gia, không tự kết luận.

## 3. Phát hiện chính

**F1. Mức High chưa tận dụng được gì.** Ba RP Asset dùng chung một renderer nên người chơi chọn High chỉ nhận render scale 1,0 và shadow map phụ 4096. Đây là chỗ nâng chất lượng gần như không đụng Medium.

**F2. Hiệu năng là thứ chặn đồ họa đẹp hơn.** Baseline 17,63 ms đã vượt 16,6 ms. Đo ablation sẵn có (`measure_m2.ps1 -Ablation none|ssao|decals|shadows|probes|transparency|geometry`) cho phép biết chi phí từng khối trước khi thêm gì.

**F3. Dải tối và sương dễ bị banding.** Đa số cảnh là low-light với fog và gradient rộng. Ảnh so sánh cũ chỉ đo tỷ lệ pixel luma < 5% (45–62%), chưa đo banding. HDR color buffer đang ở precision mặc định (`m_HDRColorBufferPrecision: 0`, 32-bit) và LUT 32 với LDR grading (`m_ColorGradingMode: 0`).

**F4. Chống răng cưa là SMAA Medium trên camera.** Phù hợp vì không ghost, nhưng không xử lý specular shimmer trên kính, inox, nước mưa. Phương án thay thế (MSAA, STP) đều có chi phí và ràng buộc riêng ở mục 4.

**F5. Phản xạ realtime tốn theo thời gian.** Probe realtime, refresh qua script, time slicing theo mặt. Ở những cảnh tĩnh theo lớp R/B, bake probe riêng từng lớp là cách quen thuộc để đổi chi phí chạy lấy dung lượng. **U**: chưa có số đo chi phí hiện tại, cần ablation `probes`.

## 4. Phương án và rủi ro

Cột "Medium" cho biết phương án có chạm mức chất lượng mục tiêu của cổng 16,6 ms hay không.

| ID | Phương án | Chạm Medium | Lợi ích kỳ vọng | Rủi ro / ghi chú | Số đo cần |
| --- | --- | --- | --- | --- | --- |
| G1 | Tạo `PC_High_Renderer` riêng và trỏ `PC_High_RPAsset` vào đó. Giữ `PC_Renderer` cho Low/Medium | Không | Nền tảng cho G2–G5, không đổi hành vi Medium | Phải đồng bộ lại danh sách Renderer Feature khi sửa. Cần kiểm shader variant stripping (`ShaderVariantStripper.cs`) | Diff bằng chứng Medium trước/sau phải bằng nhau |
| G2 | High: SSAO tăng mẫu, tắt downsample, giữ blur High, radius theo Core 05 | Không | Tiếp xúc tường/giường/bàn rõ hơn | Có thể làm tối thêm vùng đã tối (mất chi tiết hơn). Cần so luma | GPU ms (iGPU và dGPU), luma < 5% cùng góc |
| G3 | High: HDR precision 64-bit, LUT 64, HDR grading, dithering bật | Không | Giảm banding sương và vùng sáng yếu | Tăng băng thông. Phải thử trên iGPU | GPU ms, ảnh gradient sương, ca banding |
| G4 | High: AA — hai nhánh cần thử: SMAA High, hoặc MSAA 4x tắt SMAA | Không | Bớt shimmer trên kính, inox, mép khung | MSAA 4x trong Forward+ tốn băng thông, không sửa aliasing do shader. SMAA High rẻ hơn | GPU ms, ảnh cùng góc |
| G5 | High: 2 cascade, shadow main 4096, `ShadowDistance` đúng quy mô phòng | Không | Bóng đèn pin và cửa sổ sạch hơn | Cảnh trong nhà nhỏ nên lợi ích thấp. Cascade thứ hai chỉ hữu ích ở sân LOC01 | GPU ms ở LOC01, 101 |
| G6 | Thay SMAA bằng STP và hạ render scale (Medium 0,88 → ~0,77) | **Có** | Có thể giảm GPU ms nhờ giảm pixel shading | Cần motion vector, có nguy cơ ghost trên mưa, giấy, chữ nhỏ. Rủi ro lớn với ca đọc chữ | GPU ms, ca đọc chữ ở mọi font scale, ảnh khi quay nhanh |
| G7 | Bake Reflection Probe riêng theo lớp R/B thay vì realtime, giữ realtime ở vùng thật sự cần | **Có** | Giảm chi phí chạy lặp | Phản xạ không còn phản ứng với đổi lớp động nếu thiếu logic đổi probe | Ablation `probes`, ca đổi lớp 30 lượt |
| G8 | Giảm số light thêm có bóng ở phòng 101 và hành lang, gộp bóng cho đèn gần nhau | **Có** | Giảm shadow pass | Thay đổi bố cục ánh sáng, cần duyệt art | Ablation `shadows` |
| G9 | So sánh APV với bake probe R/B hiện tại | **Có** | Ánh sáng gián tiếp liên tục theo không gian | Phải bake lại cả hai lớp, thêm dung lượng, phát sinh rủi ro rò sáng. `measure_m2.ps1` đã có `-GiMode apv` | GPU ms, memory, ảnh |

**Đề xuất thứ tự:** G1 → đo ablation baseline → G2, G3, G4 chỉ trên High → nếu muốn Medium đẹp hơn thì làm G7/G8 để trả ngân sách trước, rồi mới xét G6.

## 5. Không nên làm lúc này

- Đổi sang HDRP hay ray tracing (mục 2.2).
- Nâng texture lên 4K hoặc thay model: bị art lock và có thể phá nguyên tắc Delta tối thiểu.
- Thêm bloom, grain, chromatic aberration mạnh hơn để "trông điện ảnh": đụng ca đọc chữ và reduced motion.
- Bật motion blur: Volume mặc định đã có thành phần này, không nên bật vì đọc chữ và say chuyển động.

## 6. Kế hoạch kiểm chứng

1. **Baseline:** chạy `measure_m2.ps1` ở Medium 1080p với ablation `none`, `ssao`, `decals`, `shadows`, `probes`. Ghi P50/P95/P99, GPU/CPU/VRAM tách bạch như `TASK031-D` yêu cầu.
2. **Hồi quy:** `verify_windows.ps1 -Quality Medium` bản trước và sau, chứng minh Medium không đổi (nếu chỉ làm G1–G5).
3. **High:** cùng góc máy ở LOC01 cổng/sân, LOC02 quầy kính, LOC03 phòng 101 lớp R và B, hành lang STAFF. So ảnh cùng góc và số đo.
4. **Đọc được:** F101, E01, thẻ chứng cứ, `INNER_SUB_SILENT` ở font scale 100% và 130%, bật/tắt tăng sáng và giảm chuyển động.
5. **Ghi hồ sơ:** DEC đề xuất (ví dụ `DEC-GFX-01`) trong `07/Decisions`, cập nhật `07/Changelog` sau khi chốt.

## 6b. Việc cần người dùng quyết định

1. Mục tiêu: nâng chất lượng mức **High** (an toàn với cổng hiệu năng) hay cũng muốn Medium đẹp hơn (phải trả ngân sách bằng G7/G8/G6).
2. Máy chuẩn để chấm High: iGPU Intel Arc như các lượt đo trước, hay có dGPU rời.
3. Có chấp nhận mở DEC mới để ghi nhận thay đổi này không.

## 6c. Đã triển khai (G1–G5 phần High, người dùng chọn "Chỉ mức High", máy chuẩn Intel Arc iGPU)

| Thay đổi | Vị trí |
| --- | --- |
| Renderer riêng cho High, SSAO Samples High và tắt downsample, giữ Source Depth Normals, Blur High | `Assets/Settings/PC_High_Renderer.asset` (+ `.meta`, GUID `7c1e0b5a…`) |
| `PC_High_RPAsset`: trỏ sang `PC_High_Renderer`, HDR buffer 64-bit, LUT 64, 2 cascade, shadow main 4096 | `Assets/Settings/PC_High_RPAsset.asset` |
| Camera tại High: SMAA High và dithering. Low/Medium khôi phục giá trị đã dựng trong scene | `Assets/CTCC/Runtime/HospitalGame.Graphics.cs`; gọi từ `HospitalGame.cs` (sau `player.Initialize`) và nút đổi chất lượng ở `HospitalGame.Pages.cs` |

`PC_Renderer.asset`, `PC_RPAsset.asset`, `PC_Low_RPAsset.asset` và `QualitySettings.asset` không đổi. Giữ LDR color grading để grade R/B đã duyệt không lệch giữa các mức.

**Bằng chứng (build từ worktree sạch `C:\CTCC-Gfx-Worktree`, HEAD `0b5b0a9` + đúng các thay đổi trên):**
- Build Windows x64: `Build Finished, Result: Success`.
- `verify_windows.ps1` module QA: `RESULT=PASS` ở **High** và **Medium** (`output/gfx-high/module-High`, `module-Medium`).
- Ảnh cùng góc `corridor-long-R.png`: High cạnh khung cửa và mép gạch sạch hơn, hình dạng và grade giữ nguyên. Chỉ là đánh giá bằng mắt, chưa đo.

**Chưa có bằng chứng (ghi U):**
- Số đo GPU/CPU/VRAM High và Medium: `measure_m2.ps1` dừng ở "did not acquire foreground focus" (cần cửa sổ game có focus trên desktop tương tác). Cần chạy trên máy có người trước màn hình. Lệnh: `./tools/measure_m2.ps1 -Executable <exe> -PresentMon <PresentMon-2.6.0-x64.exe> -Evidence <thư mục mới> -Quality High` và lặp lại với `Medium`.
- Ca đọc chữ F101/E01/`INNER_SUB_SILENT` ở font scale 130% trên High, banding sương, so luma < 5%.
- Chưa mở DEC (đề xuất `DEC-GFX-01`), chưa ghi `07/Changelog`.

**Blocker ngoài phạm vi:** working tree chính (nhánh `feat/ch01-rebirth-iii-phase25-diegetic-ux`) đang có thay đổi chưa commit của việc khác và **không compile** (`WorldAnchors.SurfaceAn02Clip`, `VerifyCh01Rebirth3Phase25Suite`, `HospitalGame.cs:1307`). Tôi không sửa các lỗi này. Thay đổi đồ họa ở trên đã nằm trong working tree chính và cần được commit cùng hoặc cherry-pick riêng khi nhánh đó ổn.

## 7. Nguồn tham khảo

- Unity URP 17 / Unity 6: Screen Space Ambient Occlusion, SMAA, Spatial-Temporal Post-processing (tra qua tìm kiếm web ngày 2026-11-27, nguồn tài liệu Unity, chưa đối chiếu từng tham số trên 17.3.0 cụ thể → **U**).
- `docs/prototype/engine.md` (so URP/HDRP trên máy dự án), `docs/ch01r/art-production.md` (đo hiệu ứng môi trường), `docs/m2/research-and-art-direction.md`.
