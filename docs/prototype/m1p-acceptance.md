# M1-P · Hiệu năng và nghiệm thu prototype

19/11/2026 · Core Design System v2.2 · baseline `33a27e3dd913f5436c31c03feb9efd687a6c8f26` (M0-R) · Windows release `1acc87780d07b6c526849f0a6936b904b113b586` (0.2.1, Assembly SHA-256 `40699ecaf8baaffd00f443880f2d3b1a6a28527daf0328cdcb9232d8736f79cc`). Scene `AnSinh.unity` có SHA-256 `f4b3e5e9f4d1e96cbff3e31fb9d8e8a6973ee8402898ae3b02af040c94eb615c` ở cả hai bản.

## Cấu hình và cách đo

Máy đo Intel Core Ultra 7 155H, Intel Arc iGPU, 15.868 MiB RAM theo Unity, driver 32.0.101.6127, Windows 11 build 26300, Direct3D 12. Cửa sổ 1920 × 1080 trên màn hình báo 60 Hz. Đích thử nghiệm là **Medium ở 1080p/60 Hz**, `vSyncCount=1` khi chơi. Các lượt uncapped tắt VSync để đo dư địa, các lượt paced dùng cấu hình phát hành. Hiện có bằng chứng trên một máy.

Chạy warm-up 5 giây. Sau đó capsule và camera đi cổng phụ → lễ tân → hành lang và phòng 101 B → quay lại cổng/lễ tân R → phòng 101 R với đèn pin → kho → cấp cứu. Tuyến profile chọn F101-B, đi qua E11/E12/G02, autosave, điểm tải CP04 và hiệu ứng sau giao dịch. Các thao tác profile gọi module qua kịch bản có định vị ở mốc chuyển cảnh. Bộ kiểm đầu vào riêng đi qua focus, phím, menu và controls của Windows player.

- `frames.csv` ghi thời gian giữa frame, Unity FrameTimingManager main/render/GPU, draw call, SetPass, tam giác, bộ nhớ và cờ foreground. `actions.csv` ghi thời gian giao dịch theo QPC.
- PresentMon 2.6.0 đọc ETW và cho biết frame được hiển thị. Ghép QPC, PID và swap chain chính với khoảng tuyến đo. `DisplayedTime` và CPU/GPU busy được lưu riêng với timing Unity. Tỷ lệ hiển thị và foreground đi cùng mỗi lượt để nhận ra thay đổi compositor. Các lượt uncapped có thể dùng Independent Flip/tearing, nên cadence 60 Hz lấy từ lượt VSync. Nearest-rank cho P50/P95/P99. FrameTimingManager thu kết quả GPU bất đồng bộ và mẫu GPU bằng 0 bị bỏ khỏi phân vị. [Unity FrameTimingManager](https://docs.unity3d.com/ja/6000.0/ScriptReference/FrameTimingManager.html) · [PresentMon Console](https://github.com/GameTechDev/PresentMon/blob/main/README-ConsoleApplication.md).
- Phép đo dùng cửa sổ và camera thông thường. Chụp ảnh đối chiếu là ca riêng. Các kết quả đọc lại GPU đồng bộ của M0-R cũ được giữ ở phần lịch sử.

## Hiệu năng release 1080p

Các số phân vị theo thứ tự P50 / P95 / P99, đơn vị ms. Cột draw call là số lượt vẽ Unity P95 / đỉnh, gồm các pass và thời điểm probe. Unity MiB là đỉnh `GetTotalAllocatedMemoryLong`. `System Used Memory` là counter Unity cho process. Bộ nhớ texture ghi riêng, vì các đại lượng này có phạm vi khác nhau.

| Lượt | Frame Unity | CPU main | CPU render | GPU | ETW hiển thị | Draw call | Unity / system MiB đỉnh |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Medium uncapped 01 | 6,08 / 10,86 / 12,81 | 2,36 / 3,52 / 4,48 | 1,23 / 1,66 / 2,12 | 5,84 / 10,50 / 12,03 | 5,94 / 11,82 / 16,68 | 1.422 / 2.869 | 130,8 / 1.198,8 |
| Medium uncapped 02 | 6,16 / 10,91 / 12,66 | 2,29 / 3,44 / 4,30 | 1,22 / 1,66 / 2,06 | 5,93 / 10,64 / 12,14 | 6,15 / 10,90 / 12,67 | 1.421 / 2.866 | 130,9 / 1.211,3 |
| Medium VSync 01 | 16,67 / 16,68 / 33,33 | 3,12 / 5,05 / 8,15 | 1,56 / 2,36 / 5,75 | 7,01 / 9,91 / 13,75 | 16,66 / 16,75 / 33,36 | 1.419 / 1.452 | 130,8 / 1.194,3 |
| Medium VSync 02 | 16,67 / 16,68 / 33,33 | 3,30 / 5,22 / 8,63 | 1,65 / 2,38 / 4,24 | 7,04 / 10,58 / 11,76 | 16,66 / 16,74 / 33,31 | 1.416 / 1.455 | 129,1 / 1.188,9 |

Hai lượt uncapped đều giữ foreground. Hai lượt VSync đều ghi nhận 100% frame theo swap chain hiển thị, kiểu Hardware Composed Independent Flip, trong khi ứng dụng báo foreground 0%. Hai lượt VSync ghi P95 hiển thị khoảng 16,74 ms và P99 khoảng 33,3 ms. Lượt VSync giữ foreground ở bản tiền nhiệm có P99 16,82 ms, với một lần ghi đồng bộ khóa frame 583 ms khi lấy E11. Giữ các trạng thái foreground/hiển thị riêng trong kết quả.

Baseline M0-R development chèn công cụ đo có assembly `3e7666a2da3a97ffbf5809ed507d95b5e9e592f74c982fb4b55d7754fd70dad6`. Lượt lặp baseline giữ foreground và hiển thị ổn định, với 6.762 khung ETW, Unity 6,18 / 10,79 / 12,77 ms, CPU main 2,83 / 4,08 / 5,06 ms, CPU render 1,52 / 1,96 / 2,26 ms, GPU 6,11 / 10,58 / 11,84 ms và ETW 6,16 / 10,78 / 12,69 ms. Draw call P95 / đỉnh là 1.435 / 2.914 và Unity / system MiB đỉnh là 130,9 / 1.198,1. Hiệu năng thông thường của release Medium nằm gần lượt baseline tốt nhất. Kết quả thay đổi rõ hơn ở độ trễ giao dịch E11 và bộ nhớ texture.

Lượt **Medium VSync từ ZIP đã giải nén** giữ foreground toàn tuyến. PresentMon ghi 2.778 khung hiển thị, 100% present, với P50 / P95 / P99 **16,66 / 16,73 / 16,82 ms**. Các phân vị Unity là 16,67 / 16,68 / 16,68 ms. CPU main P95 4,37 ms, CPU render P95 2,25 ms, GPU P95 10,54 ms, draw call P95 / đỉnh 1.417 / 1.447 và Unity / system MiB đỉnh 128,7 / 1.198,4. Frame lớn nhất trong các đoạn thu E11 là 16,68 ms. Đổi R→B lần đầu có một frame xấp xỉ 100 ms. Giao dịch của lượt đó mất 61,82 ms.

## So sánh khi tắt từng tính năng M0-R

Mỗi hàng là một lượt riêng trên cùng tuyến. `Tắt` chỉ dùng để chẩn đoán. Các pass có chung depth/normal hoặc shadow nên hiệu ứng không cộng dồn. Số ở bảng là P95 trong Unity.

| Chế độ | GPU ms | Draw call | SetPass | Tam giác |
| --- | ---: | ---: | ---: | ---: |
| Baseline đầy đủ | 10,58 | 1.435 | 82 | 666.883 |
| Tắt shadow | 8,72 | 873 | 72 | 289.011 |
| Tắt SSAO | 10,09 | 1.395 | 78 | 666.864 |
| Tắt DBuffer | 10,35 | 1.422 | 74 | 667.185 |
| Tắt reflection probe | 11,17 | 1.441 | 82 | 666.988 |
| Tắt renderer dùng toàn vật liệu transparent | 10,55 | 1.432 | 74 | 668.709 |
| LOD bias 0,8 | 10,43 | 1.389 | 81 | 577.880 |

Shadow là phần tăng lượt vẽ và tam giác rõ nhất. SSAO và DBuffer mang lại tương phản tiếp xúc và chi tiết bề mặt, với chênh GPU tuyến nhỏ hơn shadow. Probe và transparency có kết quả GPU toàn tuyến nằm sát biến động giữa lượt. Probe vẫn được chia lịch để tránh hai yêu cầu cùng lúc. Hạ LOD giảm lượng tam giác ở P95, còn High giữ bias M0-R. Scene M1 chưa có bake occlusion, nên đánh giá culling hiện tại dựa vào phạm vi camera, LOD và draw/tam giác theo khu vực.

## Thay đổi và kiểm lại chất lượng

- Giữ URP Forward+, SSAO half-res, DBuffer Albedo/Normal/MAOS, RainWindowGlass, cây, cửa và nguồn Blender. Baseline ghi 5 shadow map cạnh tranh atlas 2048 và URP tự hạ độ phân giải. Medium giữ 2048 với tier punctual 512 và soft filter Medium. Low dùng 0,76 render scale, 1024 directional, tầm bóng 17 m và LOD bias 1,1. Medium dùng 0,88 và 1,6. High dùng native 1080p, atlas 4096 và LOD bias 2. Cả ba dùng cùng decal/SSAO. Ảnh và tài liệu chứng cứ giữ mip đầy đủ.
- Các lượt Medium release có 48 LODGroup, 51 texture stream và khoảng 222 MiB texture theo Unity sau tuyến, với budget 224 MiB. M0-R dùng khoảng 257 MiB texture sau tuyến và chưa bật streaming. Culling theo frustum và mức LOD thể hiện trong draw/tam giác thay đổi theo vùng. Scene chưa có dữ liệu bake occlusion. [Unity mipmap streaming](https://docs.unity.com/en-us/engine/6000.0/manual/materials-and-shaders/textures/texture-loading/texture-streaming).
- Low và High cũng hoàn thành toàn tuyến trên bản đã giải nén. Low đo Unity frame P50 / P95 / P99 5,51 / 9,06 / 16,00 ms, GPU P95 8,10 ms và 142,7 MiB texture. High đo 7,43 / 14,25 / 16,26 ms, GPU P95 14,07 ms và 256,7 MiB texture. Hai lượt này tắt VSync. ETW ghi 99,44% frame hiển thị trên Low và 100% trên High.
- Chụp lại M0-R từ Windows build có assembly `26e052568278…` và chụp M1-P release cùng góc. Các cặp Phòng 101 R/B, hành lang, giường/IV, kính mưa và phong bì giữ hình học, điểm nhìn và dấu hiệu vật liệu. B có thay đổi phân bố bóng tại tủ và mặt sàn. Ảnh M0-R chụp lại tối hơn ảnh showcase lịch sử ngay trên cùng vị trí camera. Bảng hiện hành dùng ảnh chụp lại cùng máy, theo đúng dữ liệu render. [Đối chiếu sáu góc](evidence/m1p/visual-comparison.jpg).

## Giao dịch và kiểm thử

Cache giữ tối đa 8 cấu hình NavMesh. Collision preflight, xác nhận pad, rollback và thứ tự `save trước → state commit sau` vẫn dùng Core 06. Ở lượt Medium development có 10 lần thu nguồn và 5 lần dựng sync. P95 dựng là 6,91 ms trên 5 lần miss. Đợt M1-P sắp từng reflection probe vào hàng và gộp yêu cầu đổi lớp chưa lên lịch. P95 CPU của `RenderProbe()` là 0,004 ms trên 4 lần enqueue. Chi phí cubemap nằm trong phép so sánh render bên dưới. Save ghi file tạm, flush xuống đĩa, kiểm checksum rồi thay atomically. Trong lượt development trước khi chuyển E11 sang worker, 22 lần save có P95 23,19 ms và durable flush P95 12,74 ms. Hai pha được đo riêng. Ở ứng viên đầu, lần lấy mảnh tranh E11 đầu tiên giữ main thread 564,78 ms trong giao dịch và ETW ghi khung 583,39 ms. Bản giao chuyển các lần thu E11 và ghép tranh sang worker. UI chỉ xác nhận sau khi lưu thành công. G02, đổi lớp và các checkpoint còn chạy phần commit đồng bộ. Trong hai lượt uncapped cuối, frame lớn nhất trong các đoạn thu E11 là 15,65 và 12,18 ms. Lỗi worker chèn sau flush cho phép 10 frame Unity tiếp tục và giữ cả bản lưu lẫn state trước giao dịch. [Unity JSON trên worker](https://docs.unity3d.com/ja/6000.0/ScriptReference/JsonUtility.ToJson.html).

| Kiểm | Kết quả Windows |
| --- | --- |
| `TASK025-A`, `CH01-Q01–03`, `CH02-Q01/Q02`, `LOG06-Q01` | Ba lựa chọn F101, từ chối C khi thiếu quan sát, sửa A→B→C giữ first decision và 10 lần submit lặp. E12, 3 mảnh tranh, vòng tay và phiếu chuyển, đúng khóa G02. Chạy module cả ba nhánh. [Log release](evidence/m1p/module-release.txt). |
| `TASK025-B`, `T02/T03`, `T08` | 30 lần bị chặn tại ngưỡng, 30 lần swap hợp lệ, chèn collider để kiểm rollback và 10 lần reload. Lỗi chèn sau durable flush giữ commit trước. Khi main hỏng, bộ đọc mở backup hợp lệ. [Log release](evidence/m1p/module-release.txt). |
| Input Windows, `T06` | Input System gửi phím đến game qua Update, focus raycast và UI thông thường, với các vị trí dựng sẵn để tái lập. Kiểm cửa/rèm, tủ chạy đến vị trí mở, soi 3D, chuyển bản chữ lớn, chốt F101 cả ba nhánh, tuyến E11/E12 đến exit, âm lượng tổng/âm nền, mono, caption, tăng sáng, cỡ chữ, giảm chuyển động, phím thay thế và lưu tay/tải lại. [Log release](evidence/m1p/input-release.txt). |
| PhysicalInspector | 100 lần mở/đóng giấy 3D trong Windows release. Sau stress còn 0 `InspectPaperSheet` và 0 `InspectPaperMaterial`. Unity cấp phát trước/sau lần lượt 134.675.278 và 133.623.015 byte. [Log release](evidence/m1p/input-release.txt). |
| Truyền lỗi QA | Chèn lỗi riêng ở module, bàn phím và ảnh. Windows release trả exit code 1 và `RESULT=FAIL` ở mỗi ca. [Log kiểm lỗi](evidence/m1p/failure-probes.txt). |

## Dựng lại và trạng thái cổng

```powershell
$pm = 'C:/tools/PresentMon-2.6.0-x64.exe'
./tools/build_m1p.ps1 -Output output/m1p/repro-windows
./tools/measure_m1p.ps1 -Executable output/m1p/repro-windows/CaTrucCuoiCung.exe -PresentMon $pm -Quality Medium -Evidence output/m1p/repro-medium
./tools/measure_m1p.ps1 -Executable output/m1p/repro-windows/CaTrucCuoiCung.exe -PresentMon $pm -Quality Medium -VSync 1 -Evidence output/m1p/repro-paced
./tools/verify_windows.ps1 -Executable output/m1p/repro-windows/CaTrucCuoiCung.exe -Evidence output/m1p/repro-input -PlayerInput
python tools/summarize_m1p.py output/m1p/repro-medium output/m1p/repro-paced --output output/m1p/repro-summary.json
python tools/bundle_m1p.py output/m1p/repro-windows output/CaTrucCuoiCung-M1P-Windows.zip
```

Để đo M0-R, dùng `build_m1p.ps1 -Baseline 33a27e3dd913f5436c31c03feb9efd687a6c8f26 -Development` với thư mục build riêng, benchmark `-Quality PC`. Script lấy cây `Prototype` từ Git và chèn cùng công cụ đo. Bản release M1-P cài từ ZIP có `BUILD.txt`, nguồn giấy phép và hướng dẫn phím.

Gói kiểm `output/CaTrucCuoiCung-M1P-Windows.zip` có 179 tệp, 196.705.318 byte, SHA-256 `6b42c56e3d35dba234434523e4960a61ac34f9fb2025d02ce6da806185e2df99`. CRC của toàn gói, assembly sau giải nén và lượt chạy Windows từ tệp EXE trong gói đều đã kiểm. Trace CSV gốc nằm trong `output/CaTrucCuoiCung-M1P-Captures.zip`. [Định danh release](evidence/m1p/release-build.txt) · [Định danh baseline](evidence/m1p/baseline-build.txt) · [Tóm tắt trace và phân vùng](evidence/m1p/timing-summary.json).

**Cổng M1 hoàn thành cho prototype Medium 1080p/60 Hz trên máy đo.** Ba nhánh F101, E11/E12, swap, rollback, save/load và đầu vào đã chạy trên Windows. Baseline visual được chụp lại và đối chiếu cùng camera.

| Vấn đề đã rà | Phạm vi còn lại |
| --- | --- |
| Đuôi frame time và I/O | Hai lượt VSync ngoài foreground có P99 33,3 ms. Lượt foreground từ ZIP có một frame R→B khoảng 100 ms. Đồng bộ ở lần dựng NavMesh đầu và ở các checkpoint ngoài E11 được giữ để duy trì rollback. Chưa xác nhận máy cấu hình khác hoặc đĩa tải nặng. |
| Preset High | Mức chất lượng tùy chọn. Medium là đích 60 Hz đã nghiệm thu trên Intel Arc iGPU. |
| Culling, chuyển vùng và duyệt nội dung | Scene hiện dùng frustum/LOD. Bake occlusion, load additive liên vùng và playtest người mới hoặc review `TASK026–028` theo cổng M2. |

[Bàn giao ranh giới scene](m2-scene-handoff.md) ghi điều kiện nối vùng additive trong M2. Các cập nhật sản xuất nằm trong [Production Database](../07_Production_Database.xlsx).
