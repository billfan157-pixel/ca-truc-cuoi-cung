# CH01-R · Nghiệm thu Tái cấu trúc Gameplay Điều tra & Kinh dị Tâm lý

Tài liệu nghiệm thu kỹ thuật và thiết kế cho bản tái cấu trúc **CH01 — Người trở về** (`CH01-R`) trên nền M3.1 (`05a5bfa`) và chuẩn Core Design System v2.2 (`docs/01..07`, `LOCK-V22-01..03`, `DEC-CH02`, `LOCK-LOG06`, `LOCK-LOG10`).

> **Trạng thái cổng con người:** Các cổng duyệt bằng con người và chuyên gia (**`TASK010`** 0/6 người chơi mới, **`TASK026`–`TASK028`** duyệt kiến trúc/y tế/PCCC/ngôn ngữ/âm thanh) tiếp tục ở trạng thái **MỞ**. Mọi kết quả dưới đây phân tách rạch ròi giữa kiểm chứng kỹ thuật/hệ thống trên build Windows x64 và các cổng chưa nghiệm thu bằng con người.

---

## 1. Kiểm toán Baseline (M3.1 `05a5bfa`) & Nguyên tắc Thiết kế

Đánh giá bản build độc lập M3.1 (`output/m31/windows-05a5bfa/CaTrucCuoiCung.exe`, video nền [baseline-walkthrough.mp4](evidence/baseline-walkthrough.mp4), log [baseline-qa-gameplay.txt](evidence/baseline-qa-gameplay.txt)) cho thấy 7 điểm nghẽn cốt lõi ở CH01:

1. **F101 chỉ là checklist 2 điểm + cờ boolean:** `FormService.Apply` nhận `bool matched` tin cậy hoàn toàn từ UI thay vì kiểm chứng bằng chứng và lập luận ở tầng nghiệp vụ; người chơi chỉ cần chạm đủ `OBS101A` và `OBS101B` rồi nhấn nút.
2. **Đường đi tuyến tính cứng:** Hành lang trệt dẫn thẳng một mạch từ quầy tiếp nhận → Phòng 101 → F101 mà không có nhánh rẽ điều tra nội bộ hay lý do quay lại đối chiếu hồ sơ.
3. **PhysicalInspector mới dừng ở mặt trước trang trí:** Không thể lật mặt sau hồ sơ, không có phóng to vùng chi tiết, không gắn trạng thái quan sát (`Discovered → Examined → Interpreted → Corroborated`), và ánh sáng nền từng gây chói trắng giấy.
4. **Hậu quả của F101 trong lớp B còn mờ:** Sự khác biệt không gian/ánh sáng/giấy tờ giữa `Affirmed`, `Unverified` và `Refuted` chưa tạo đủ lực phản hồi về mặt tâm lý và hành chính.
5. **Tương tác vật lý còn ít đầu mối:** Chưa có ngăn kéo kéo trượt có lực cản, rèm treo chưa giữ đúng góc gắn thanh ray sau phiên reset, chưa hỗ trợ giữ-và-kéo tùy chọn song song với nhấn bật/tắt.
6. **Không có bộ điều phối bầu không khí riêng:** Mọi kích hoạt kinh dị đều gắn cứng vào trigger tuyến tính; chưa có `AmbientDreadDirector` tách khỏi tầng phát âm thanh với ngân sách sự kiện, khoảng lặng, seed QA tất định và lưu trữ sự kiện một lần.
7. **Sổ trực chưa phục vụ đối chiếu tài liệu:** Chưa cho đặt hai hồ sơ song song để người chơi tự so khớp dữ kiện mà không tự động bật đáp án.

### Chuyển hóa nguyên tắc tham chiếu (không sao chép cơ chế thô)
- ***Return of the Obra Dinn*:** Phân biệt rạch ròi dữ kiện thực tế với suy đoán vượt chứng cứ; không chấm đúng theo cảm tính hay ép thử-sai ngẫu nhiên; yêu cầu người chơi chọn nguồn đối chiếu và câu kết luận có căn cứ.
- ***Devotion*:** Nỗi sợ từ kiến trúc hành chính/đời thường Việt Nam quen thuộc (mẫu biểu 2005, chữ ký tay nhòe mực bi, dép nhựa nhỏ, thông báo kê giường tạm) và biến dạng không gian tinh tế sau quyết định của chính nhân vật.
- ***SOMA*:** Quyền để ngỏ (`Unverified`) là lựa chọn nghề nghiệp hợp lệ, không bị trừng phạt bằng thất bại hay chặn tiến trình; không để màn chơi đẩy nhân vật vào chứng mất trí giả tạo.
- ***Amnesia: The Bunker*:** Tương tác vật lý có trọng lượng và sức cản (cửa bản lề, ngăn kéo tủ, rèm vải trên thanh ray) kèm lựa chọn bật/tắt trợ năng tương đương.
- ***The Vanishing of Ethan Carter*:** Các quan sát tại hiện trường độc lập thứ tự khám phá; hệ thống cho phép đi lệch tuyến, quay lại đối chiếu và tự chắp nối mạch sự kiện.

---

## 2. Kiến trúc & Những thay đổi chính của CH01-R

### 2.1. Tái cấu trúc F101 thành suy luận có căn cứ & Khung Chứng cứ (`DeductionFramework.cs`)
Xem chi tiết đồ thị phụ thuộc và ma trận thẩm định tại [puzzle-dependency-graph.md](puzzle-dependency-graph.md).

- **Hợp đồng dữ liệu gọn nhẹ (`Prototype/Assets/CTCC/Runtime/DeductionFramework.cs`):**
  - Vòng đời `ObservationStage`: `Discovered → Examined → Interpreted → Corroborated`.
  - Phân loại `ObservationCategory`: `Claim` (F101), `PhysicalFact` (`OBS101A`, `OBS101B`), `ComparisonDoc` (`OBS101C` phiếu chuyển giường 02 đã hoàn tất, `OBS101D` phiếu phát thuốc giường 03 đã xuất viện), `ContextDoc` (`OBS_SHIFT_LOG` quy chế AS-05, `OBS_CORRIDOR_NOTICE` thông báo kê giường tạm), và chi tiết môi trường (`OBS_CLOCK_0317`, `OBS_RECEPTION_DRAWER`).
  - Đăng ký `ObservationRegistry` kiểm soát ID hợp lệ, lớp thực tại gốc (`Reality.B` vs `Reality.R`), địa điểm và tương thích ngược với các chứng cứ CH02 (`LIST`, `SOCKETS`, `WHEELS`, `E11.*`, `E12`).
- **Thẩm định tập trung tại tầng gameplay (`HypothesisService.EvaluateF101` & `FormService.ApplyWithReasoning`):**
  - Loại bỏ đường truyền boolean tin cậy UI.
  - Để ký **`Refuted` (Ghi nhận bất nhất)**, người chơi phải: (1) chọn đúng cặp dữ kiện cốt lõi `OBS101A + OBS101B` đã được kiểm tra kỹ ở lớp `B`, (2) loại trừ tài liệu không liên quan (ví dụ `OBS101D` phiếu phát thuốc giường 03 bị từ chối rõ lý do), và (3) chọn đúng kết luận có căn cứ (`UNRELIABLE_ZERO` — *con số 0 trên F101 chưa đủ tin cậy để xác nhận*).
  - Ba kết luận vượt chứng cứ (`PATIENT_PRESENT`, `FALSIFICATION`, `CLERICAL_OK`) bị từ chối với phản hồi chuyên môn cụ thể, không khóa mềm, có lưu số lần thử lỗi để hỗ trợ gợi ý tiến cấp tùy chọn.
  - Cả ba quyết định **`Affirmed`**, **`Unverified`** và **`Refuted`** đều mở `G01` và cho hoàn thành CH01, có thể chỉnh sửa lại trước khi rời buồng (lưu cả `FirstDecision`, `CurrentDecision`, `f101Argument` và toàn bộ `f101History`).
- **Tương thích ngược bản lưu (`Protocol.cs` Schema v4 & `AtomicSaves.cs`):**
  - Di trú tự động các bản lưu v1, v2 và v3 sang v4 (giữ atomic temp-write + durable flush + backup rollback + kiểm tra tính hợp lệ của `observationRecords`, `f101History`, `f101Argument`, `dreadEventsSeen`).

### 2.2. Điều tra vật lý hai mặt & Sổ Đối chiếu Chứng cứ (`PhysicalInspector.cs`, `HospitalUI.cs`, `HospitalGame.Pages.cs`)
- **Bộ tài liệu bệnh viện Việt Nam 2005 (`tools/bake_ch01r_documents.py`):** 18 texture `1024×1360` dùng font SIL OFL `BeVietnamPro`, mực bi/dấu đỏ/nếp gấp/mực thấm ngược mặt sau: `F101` (trước/sau + 3 bản đã ký `A/B/C`), `ObsCard` (AS-05B trước/sau), `Doc_Bed2Transfer` (trước/sau), `Doc_Bed3Meds` (trước/sau), `Doc_ShiftGuide` (trước/sau), `Doc_WardOverflow`, `Doc_DutyRoster`, `Doc_BayEnvelopeFront`, `Doc_BayLetter` (trước/sau), `Sign_NursingAlcove`, `CH01_PR01_Corner`.
- **Soi cận cảnh 3D hai mặt (`PhysicalInspector.cs`):** Lưới giấy cong hai mặt thật, lật mặt trước/sau bằng `Space`/`R` kèm SFX `PaperFlip.wav`, phím `1–3` hoặc nút UI để phóng to các vùng chi tiết trọng yếu, chế độ Chữ lớn (Large-Print) tương đương hoàn toàn cho trợ năng (không yêu cầu thao tác chuột chính xác để lấy dữ kiện). Kiểm stress 100 chu kỳ mở/lật/đóng để lại `0` mesh rò rỉ và `0` material rò rỉ.
- **Sổ trực & Bàn đối chiếu tài liệu:** Cho phép xem lại nguồn chứng cứ (`Provenance`), đặt hai tài liệu song song không gợi ý đáp án, chuyển thẳng vào soi cận cảnh 3D từng tờ và xem lịch sử quyết định F101.

| Soi 3D mặt trước (`ObsCard`) | Soi 3D mặt sau (`ObsCard`) |
| :---: | :---: |
| ![Soi mặt trước](evidence/ch01r-inspector-obscard-front.png) | ![Soi mặt sau](evidence/ch01r-inspector-obscard-back.png) |

| Bàn đối chiếu tài liệu song song | Lập luận F101 — Bước 1/2 (Chọn bằng chứng) | Lập luận F101 — Bước 2/2 (Chọn kết luận) |
| :---: | :---: | :---: |
| ![Bàn đối chiếu](evidence/ch01r-ui-comparison.png) | ![F101 bước 1](evidence/ch01r-ui-deduction-step1.png) | ![F101 bước 2](evidence/ch01r-ui-deduction-step2.png) |

### 2.3. Khám phá phi tuyến nội bộ (`STAFF` Nursing Alcove) & Tương tác vật lý (`InteractableRig.cs`)
- **Nhánh vòng ngắn `LOC02 ↔ STAFF ↔ HALL ↔ LOC03`:** Bổ sung hốc hồ sơ điều dưỡng (`STAFF`) với biển men `HỒ SƠ TRỰC`, bàn hồ sơ gỗ tối, bảng thông báo kê giường tạm (`Doc_WardOverflow`), ngăn kéo chứa quy chế bàn giao (`Doc_ShiftGuide`) và cửa ngang `DOOR-STAFF` nối trực tiếp ra hành lang (`z = 7.50 m`) không cản đường thoát `Door B` của CH02 (`1.72 m` thông thủy).
- **Tương tác vật lý hai chế độ (`InteractableRig.cs` + `FirstPerson.cs`):** Cửa bản lề (`DOOR-101`, `DOOR-STAFF`), 3 ngăn kéo trượt (`DRAWER-BED3`, `DRAWER-DESK`, `DRAWER-NURSE`) và rèm treo trên thanh ray (`CURTAIN-101`, giữ đúng góc gốc `-90° X` của Blender và tự reset sạch giữa các phiên chơi) hỗ trợ cả nhấn bật/tắt (`E`) và giữ-kéo tùy chọn kèm `DrawerSlide.wav` / âm bản lề / âm vải.

![Hốc hồ sơ trực STAFF trong lớp B](evidence/ch01r-staff-alcove-B.png)

### 2.4. Hậu quả lớp B rõ rệt & Bảo toàn hiện trường lớp R (`HospitalScene.cs`)
- **Trạng thái chờ (`Pending`):** Rèm giường 4 khép hờ, giá kẹp đầu giường 4 đặt đúng hướng quay vào buồng, dấu chăn bông gấp vội và dép nhựa nhỏ trên nền.
- **`Affirmed` (Xác nhận 0 tồn đọng):** Lớp B củng cố báo cáo sai — dựng vách che `deniedBay` xóa phẳng dấu vết giường 4, treo bản `F101_SignedA` lên kẹp tường, ánh sáng lạnh phẳng hành chính.
- **`Unverified` (Để ngỏ):** Giữ nguyên trạng thái lửng lơ có chủ đích không trừng phạt — treo `F101_SignedB`, giữ rèm và khu giường 4 mở ngỏ cho đối chiếu.
- **`Refuted` (Ghi nhận bất nhất):** Làm rạn lớp vỏ báo cáo — treo `F101_SignedC`, kéo tách rèm lộ mảng tường ẩm, vết bánh giường đậm nét, góc vẽ sáp màu `CH01_PR01_Corner` và nhịp nhấp nháy nhẹ của đèn tuýp giường 4.
- **`Reality.R` (Hiện trường 2012):** Hoàn toàn độc lập, không bị F101 thay đổi hình học hay xóa dấu vết (`LIST`, `SOCKETS`, `WHEELS`, `E12`, `E11` nguyên vẹn 100%).

| Phòng 101 · Trước khi ký (`Pending`) | Sau `Affirmed` (Che dấu giường 4) | Sau `Unverified` (Giữ trạng thái mở) | Sau `Refuted` (Rạn lớp B, lộ dấu vết) |
| :---: | :---: | :---: | :---: |
| ![Pending](evidence/ch01r-bed4-pending-B.png) | ![Affirmed](evidence/ch01r-f101-affirmed-B.png) | ![Unverified](evidence/ch01r-f101-unverified-B.png) | ![Refuted](evidence/ch01r-f101-refuted-B.png) |

| Phòng 101 lớp B (2005) | Phòng 101 lớp R (2012 — Độc lập) | Gặp Lan ở Quầy tiếp nhận (M2-C.1 giữ nguyên) |
| :---: | :---: | :---: |
| ![Room 101 B](evidence/room101-B.png) | ![Room 101 R](evidence/room101-R.png) | ![Lan Reception](evidence/loc02-lan-B.png) |

### 2.5. Ambient Dread Director tách biệt (`AmbientDreadDirector.cs`) & Âm thanh bổ sung
- **8 nhịp kinh dị môi trường tiết chế (`DreadCatalog`):** Quạt trần lệch nhịp, tiếng xe đẩy ngắt quãng, tiếng bước chân hành lang xa (`DistantSteps.wav`), nhịp đồng hồ 03:17 (`ClockTick.wav`), gió lùa rung rèm giường 4, nhiễu loa PA và chi tiết hành chính xê dịch.
- **Kỷ luật thiết kế:** Chạy tách khỏi `EnvironmentalAudio`; xét ngữ cảnh (di chuyển, đứng yên, hướng nhìn, tần suất tương tác, lịch sử đi `STAFF`/quay lại, beat và lớp thực tại); có cooldown, giới hạn ngân sách, khoảng lặng bắt buộc, seed QA tất định (`-ctcc-dread-seed`), lưu bền vững sự kiện một lần qua save, và **tự động tắt chặn khi có hội thoại, mở sổ, hoặc soi cận cảnh**. CH01 giữ nguyên tuyệt đối phi sát thương (không Hunter, không sanity meter, không ép trốn).
- **4 tệp âm thanh 48 kHz mono 16-bit (`tools/bake_ch01r_audio.py`, manifest [audio-additions.json](audio-additions.json)):** `DrawerSlide.wav` (−7,88 dBTP), `PaperFlip.wav` (−6,57 dBTP), `ClockTick.wav` (−4,00 dBTP), `DistantSteps.wav` (−6,22 dBTP).

---

## 3. Tài sản Blender 4.5 & Render Kiểm chứng

Tất cả 7 tài sản CH01-R được dựng và tinh chỉnh thực tế trong **Blender 4.5 LTS** (`tools/blender_assets/build_ch01r_props.py`), tam giác hóa sạch n-gon (0 cảnh báo tangent khi import Unity), lưu nguồn chỉnh sửa tại `ArtSource/Blender/*.blend`, xuất FBX tại `Prototype/Assets/CTCC/Art/Meshes/` và render kiểm chứng Cycles tại `docs/ch01r/blender/`:

| Tài sản Blender (.blend / .fbx) | Vai trò trong CH01-R | Render Cycles |
| --- | --- | --- |
| `CTCC_PROP_Bed4ChartHolder` | Giá kẹp đầu giường 4 giữ thẻ `AS-05B` quay vào buồng | [Xem render](blender/CTCC_PROP_Bed4ChartHolder.png) |
| `CTCC_PROP_NursingFilingDesk` | Bàn hồ sơ gỗ tối hốc `STAFF` quay ra lối đi | [Xem render](blender/CTCC_PROP_NursingFilingDesk.png) |
| `CTCC_PROP_InteractiveDrawer` | Ngăn kéo trượt 3 vị trí (`BED3`, `DESK`, `NURSE`) | [Xem render](blender/CTCC_PROP_InteractiveDrawer.png) |
| `CTCC_PROP_Bed4BlanketTrace` | Dấu chăn bông gấp vội & dép nhựa nhỏ tại giường 4 | [Xem render](blender/CTCC_PROP_Bed4BlanketTrace.png) |
| `CTCC_ENV_StaffCrossDoor` | Cửa ngang thông giữa hốc `STAFF` và hành lang `HALL` | [Xem render](blender/CTCC_ENV_StaffCrossDoor.png) |
| `CTCC_PROP_ClipboardF101` | Kẹp phiếu F101 chỉnh UV ánh xạ động theo trạng thái ký | [Xem render](blender/CTCC_PROP_ClipboardF101.png) |
| `CTCC_PROP_BayEnvelope` | Phong bì thư Bảy (`E10a`) tinh chỉnh UV & hình khối | [Xem render](blender/CTCC_PROP_BayEnvelope.png) |

Bản quyền nguồn tài sản & font chữ đã cập nhật tại [Prototype/ThirdPartyNotices.md](../../Prototype/ThirdPartyNotices.md).

---

## 4. Xác minh QA, Adversarial QA, Fault Injection & Input QA

- **Danh tính build Windows x64 Release:** [evidence/build-identity.txt](evidence/build-identity.txt) — Unity `6000.3.28f1`, `development=False`, `Assembly-CSharp.dll` SHA-256 `f6819b62a3bbd010cc9c568f0a3072b4bbaa0d74ffe5a4c83aafb370d4334078`, 7/7 scene additive đã bake GI (`R=261`, `B=261`) và Occlusion (`7 scenes`).
- **Bộ Module & Adversarial QA (D3D12, Medium 1080p) — `RESULT=PASS`:** [evidence/qa-module-gameplay.txt](evidence/qa-module-gameplay.txt)
  - Bảo toàn 100% các hồi quy M1-P, M2, M2-C.1, M3.0, M3.1 (`TASK025-A/B`, `T02/T03/T06/T08`, `M2-ADDITIVE`, `V22-Q03` chặn `CH01-D16b`/`CH04-D06`, `CHAR-Q01/Q04..Q08` với 0 byte managed/frame, `CH02-Q05`, `AU01`, `CH03-Q01..05`, `CH04-Q02..06`, di trú save v1/v2/v3 → v4).
  - **`CH01-R ADVERSARIAL PASS`:** Kiểm 3 kết luận vượt chứng cứ bị chặn, phiếu thuốc giường 03 không liên quan bị từ chối, thiếu/chưa xem/ID lạ/giả mạo lớp thực tại bị chặn, lịch sử sửa F101 (`A↔B↔C`), soi 2 mặt, nhánh `STAFF` + ngăn kéo, lưu sự kiện dread + tắt khi mở modal, và tính độc lập của `Reality.R`.
- **Fault Injection (D3D11) — Đều chặn đúng với Exit Code `1` (`RESULT=FAIL`):**
  - `-ProbeFailure invalid-reasoning`: [evidence/probe-invalid-reasoning.txt](evidence/probe-invalid-reasoning.txt) — chứng minh nếu một lỗi làm lọt kết luận cáo buộc làm giả (`FALSIFICATION`), hệ QA lập tức đánh trượt build với mã thoát `1`.
  - `-ProbeFailure broken-progression`: [evidence/probe-broken-progression.txt](evidence/probe-broken-progression.txt) — chứng minh nếu `Unverified` chặn cổng `G01`, hệ QA lập tức đánh trượt build với mã thoát `1`.
- **Input QA bàn phím + chuột đầy đủ không teleport (D3D12, Medium 1080p) — `RESULT=PASS`:** [evidence/qa-input-gameplay.txt](evidence/qa-input-gameplay.txt) · Video 720p liên tục toàn tuyến: [evidence/ch01r-uninterrupted-gameplay.mp4](evidence/ch01r-uninterrupted-gameplay.mp4)
  - **`INPUT CH01-R FULL ROUTE PASS`:** Đi bộ 100% không teleport từ `AP00` qua cổng, quầy tiếp nhận, hốc `STAFF`, kéo ngăn kéo, đọc tài liệu Phòng 101, lật mặt sau 3D Inspector, đối chiếu song song trong Sổ trực, thử lập luận vượt chứng cứ bị từ chối rồi nộp `Refuted` hợp lệ, sụp đổ về R, vòng lặp cổng phụ và bàn giao sang CH02.
  - Bao phủ cả 3 nhánh `Affirmed` (CH02 Door A), `Unverified` (CH02 Door B), `Refuted` + manual save/load + đổi phím `J`, tuyến CH03–04 3 tầng thang bộ, và **stress 100 chu kỳ soi 2 mặt**: `0` live paper meshes, `0` live paper materials (`Unity allocated bytes 293,715,634 → 293,889,035`, chênh < 0,17 MiB).

---

## 5. Đo đạc Hiệu năng Có kiểm soát (Baseline `05a5bfa` vs CH01-R)

Phần cứng tham chiếu: **Intel Core Ultra 7 155H**, **Intel Arc Graphics** (driver `32.0.101.6127`), **16 GiB RAM**, Windows 11, **Direct3D 12**, cấu hình **Medium**, cửa sổ **1920×1080**, `render_scale=0.88`, `gi_mode=dual-probes`. Dữ liệu thô đầy đủ (`frames.csv`, `actions.csv`, `presents.csv`, `process.csv`, `result.txt`, `identity.txt`) được lưu tại [evidence/performance-raw-ch01r.zip](evidence/performance-raw-ch01r.zip) và bản tổng hợp JSON tại [evidence/benchmark-comparison.json](evidence/benchmark-comparison.json).

### So sánh đối chứng cùng điều kiện (Baseline M3.1 `05a5bfa` vs CH01-R Candidate)

| Chế độ & Bản dựng | Frames (`focused`) | Unity Wall P50 / P95 / P99 | Unity Main CPU P95 | Unity Render CPU P95 | Unity GPU P50 / P95 / P99 | PresentMon FrameTime P50 / P95 | Draw Calls P95 / Max | Working Set đỉnh |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| **VSync=0 · Baseline `05a5bfa`** | 5.260 (100%) | 10,69 / 17,28 / 20,93 ms | 7,86 ms | 2,89 ms | 9,21 / 14,93 / 17,76 ms | 10,07 / 20,56 ms | 1.406 / 2.926 | 1.553,0 MiB |
| **VSync=0 · CH01-R Candidate** | 5.173 (100%) | 11,04 / 17,43 / 20,90 ms | **7,48 ms** | **2,88 ms** | 9,91 / 15,08 / 17,99 ms | 10,38 / 20,80 ms | 1.494 / **1.889** | **1.536,7 MiB** |
| *Chênh lệch (VSync=0)* | −87 | +0,35 / +0,15 / −0,03 ms | **−0,38 ms** | **−0,01 ms** | +0,70 / +0,15 / +0,23 ms | +0,31 / +0,24 ms | +88 / **−1.037** | **−16,3 MiB** |
| **VSync=1 · Baseline `05a5bfa`** | 4.807 (100%) | 8,34 / 16,67 / 25,00 ms | 8,90 ms | 2,91 ms | 9,13 / 15,16 / 17,57 ms | 10,59 / 21,97 ms | 1.411 / 2.611 | 1.555,3 MiB |
| **VSync=1 · CH01-R Candidate** | 4.629 (100%) | 8,34 / 24,99 / 25,00 ms | 9,28 ms | 2,96 ms | 9,80 / 15,81 / 18,10 ms | 11,58 / 22,80 ms | 1.496 / 3.008 | **1.545,8 MiB** |
| *Chênh lệch (VSync=1)* | −178 | *(xem ghi chú DWM bên dưới)* | +0,38 ms | +0,05 ms | +0,67 / +0,65 / +0,53 ms | +0,99 / +0,83 ms | +85 / +397 | **−9,5 MiB** |

- **Khung hình giật > 33,3 ms (VSync=0):** Giảm từ **3 khung** (baseline) xuống **1 khung** (CH01-R); đổi lớp `swap-R-B` đạt **52,42 ms** (nhanh hơn 17,59 ms so với baseline 70,01 ms); `swap-B-R` đạt **58,43 ms** (VSync=1) và **97,97 ms** (VSync=0), nằm dưới ngưỡng lịch sử ~100 ms của `DEC-M1P-01`.
- **Chi phí hình học & bộ nhớ:** Dù thêm toàn bộ hốc `STAFF`, 7 prop Blender mới, 18 texture tài liệu 2 mặt và `AmbientDreadDirector`, đỉnh Working Set **giảm 16,3 MiB** (`1.536,7 MiB`), Unity GPU P95 chỉ tăng **+0,15 ms** (`15,08 ms` trên VSync=0) và tam giác P95 tăng **+0,87%** (`769.015`).
- **Ghi chú minh bạch về điều kiện màn hình:** Trong 4 lượt đo đối chứng tự động này, máy trạm ở trạng thái trình bảo vệ màn hình nhàn rỗi (`input=Screen-saver`), nên mặc dù Unity ghi nhận 100% khung hình `focused` với đầy đủ thời gian GPU D3D12 (`FrameTimingManager`) và PresentMon `FrameTime`/`CPUBusy`, DWM không quét hình trực tiếp ra màn hình (`DisplayedTime = NA`, `PresentMon GPUTime = NA` và `VSync=1` lượng tử theo bước 8,33 ms). Khi người vận hành quay lại màn hình hoạt động, có thể chạy lại lệnh chuẩn có khóa foreground dưới đây để ghi bổ sung `DisplayedTime` trực tiếp so với mốc foreground M3.1 (`DisplayedTime P95 = 16,74 ms` trong [performance-raw-05a5bfa.zip](../m3/evidence/m31/performance-raw-05a5bfa.zip)):
  ```powershell
  powershell -ExecutionPolicy Bypass -File tools\measure_m2.ps1 -Executable output\ch01r\windows\CaTrucCuoiCung.exe -PresentMon output\qa-final-windows\PresentMon-2.6.0-x64.exe -Evidence output\ch01r\bench-fg-vs1 -Quality Medium -VSync 1 -GiMode dual-probes
  ```

---

## 6. Nhịp chơi (Pacing) & Các cổng còn mở

- **Nhịp chơi & Mục tiêu ~25 phút:** Toàn bộ 8 beat chuẩn của CH01 được bảo toàn; tuyến không teleport trong Input QA tự động (không dừng đọc kỹ chữ tài liệu) chạy trong ~67 giây kỹ thuật, trong khi cấu trúc điều tra 6–8 nguồn thông tin 2 mặt + hốc `STAFF` + đối chiếu Sổ trực + diễn xuất Lan trực tiếp tạo độ sâu đọc/suy luận thực tế cho khung chơi 20–25 phút của người chơi mới (cần số liệu đo thực tế từ `TASK010`).
- **Bộ tài liệu playtest mù đã cập nhật cho CH01-R:** [docs/m2/playtest-kit-task010.md](../m2/playtest-kit-task010.md) — bổ sung chỉ số đối chiếu tài liệu 2 mặt, phân biệt `Unverified` không bao giờ chấm trượt, theo dõi `AmbientDreadDirector` và đo thời lượng thực tế (`0/6` người chơi — **MỞ**).
- **Các cổng con người/chuyên gia giữ nguyên trạng thái MỞ:**
  - `TASK010`: Playtest mù 6 người chơi mới (0/6).
  - `TASK026`: Review chuyên gia y tế/kiến trúc bệnh viện An Sinh & tính chân thực hình ảnh.
  - `TASK027`: Review chuyên gia PCCC lịch sử & chất lượng thoại/diễn xuất tiếng Việt.
  - `TASK028`: Review chuyên gia y tế về provenance, phiếu chuyển, ghi nhận nạn nhân & cân bằng âm thanh/mix cuối.
