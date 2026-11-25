# CTCC — M2-FINAL Integrated QA Candidate Report (`c459aba`)

- **Canonical Standard:** `Core v3.3.1 Final Clean — Design Lock` (`10/10` file khớp [docs/SHA256SUMS.txt](../SHA256SUMS.txt), `core_sha256sums_sha256=b7b57f9f22be56e3371232e2cc0983705717af07d87c8e4d72171aa553bef34c`; `python docs/agent/project_docs.py check` đạt: 7 file Core, 27 sheet, DAG 96 task không cycle/missing).
- **Candidate Build Directory:** `output/m2-final/m2-final-cand-c459aba/windows-x64/`
- **Release Deliverable Package:** `output/m2-final/m2-final-release-c459aba/`
- **Engine & Configuration:** Unity `6000.3.28f1` (URP 17.3, Forward+, D3D12, `development=False`, Medium `render_scale=0.88`, `dual-probes`, `VSync 1`, `1920×1080`, `60 Hz`).
- **M2 Closure Recommendation:** **KHÔNG ĐỀ XUẤT ĐÓNG M2.** Ứng viên kỹ thuật `c459aba` đã **Technical Verified** đầy đủ cho phạm vi tích hợp CH01–CH04 (Phase A–F), nhưng cổng đóng M2 theo Core 07 bắt buộc hoàn tất **`TASK010`** (6 người chơi mới) và 3 biên bản chuyên gia **`TASK026`–`TASK028`** (**Human Review Pending**).

---

## 1. Build Identity, Commit Chain & Reproducibility

### 1.1. Danh tính ứng viên (`build-identity.txt`)
- **Git Branch / Clean Commit:** `checkpoint/m2-phase-b-v33` @ `c459aba86192591b4adf5d267438e5fb4b77f704`
- **`CaTrucCuoiCung.exe` SHA-256:** `fbd073ca59c4918edbc6704f5393cf59021173af11714c00db4212c50117492b`
- **`Assembly-CSharp.dll` SHA-256:** `373d7c91bfcc071a04969b664f6a2fd03c553ee66e39fb65f6fcb8b58b149cbc`
- **Core v3.3.1 Manifest SHA-256 (`docs/SHA256SUMS.txt`):** `b7b57f9f22be56e3371232e2cc0983705717af07d87c8e4d72171aa553bef34c`
- **9/9 Baked Scenes SHA-256 (giữ nguyên 100% từ bake v3.3.1 chuẩn):**
  - `AnSinh_Bootstrap.unity`: `ad40c0ef7be5ec25e1ce0469c2943bb7448fcaba01c3c99bab99d5ad000a1a64`
  - `Campus_Grounds.unity`: `1780e4c41298cc84223853af919b002727a4976ace593b7ce51f7a71e6cac1ff`
  - `Campus_Interiors.unity`: `9ac129831a16b65fed99fc349f482f817880ed1edfc4afb5cf25f7c0a0e5a4fb`
  - `LOC01_Exterior.unity`: `aff520e85f486ab6a71cc3182294109d73e5d81969308c6dbe5dd9c34cf9389b`
  - `LOC02_Reception.unity`: `7109e4762e26aa24daaca5530d04648143e71680f1951481f5e51311f2c8b698`
  - `LOC03_Ward101.unity`: `35b11a8f92ae25e0e669d746a638c45bf6838937a796abe4e9f480f10ce16232`
  - `LOC04_Emergency.unity`: `3628e3121654efbabc617559c5f912ad149ef22bcd34346f1d47200674672d98`
  - `LOC05_ArchiveWing.unity`: `fd82e1e3c6928cbb6d1e612a0d5d2a39ea3946867b902380be521359e7734a8d`
  - `LOC09_ServiceWing.unity`: `aa167b75a84dc0c6327a747bcb391b4207219b0c06493d10bee110cee47dd75d`

### 1.2. Chuỗi commit & tệp thay đổi trên `checkpoint/m2-phase-b-v33`
| Commit | Phạm vi | Tệp sửa đổi | Mục đích |
| --- | --- | --- | --- |
| `3712d89` | Phase A/B baseline | Sync Core v3.3.1 + Campus-v1 + CH03–CH04 | Khôi phục build sạch trên Unity 6000.3.28f1 |
| `fb2e96d` | Phase B GPU optimization | `HospitalScene.Performance.cs` (+`.meta`), `HospitalScene.cs`, `HospitalGame.Benchmark.cs`, `PC_RPAsset.asset`, `HospitalAuthoring.Lighting.cs` | Culling đèn/bóng `CAMPUS_EXT` và tán `LOC01` khi ở trong nhà; khôi phục ngân sách shadow Medium 1-cascade 24 m; hạ GPU P95 từ 18,89–19,37 ms xuống 13,14–14,01 ms |
| `c7f158b` | Phase C/D/E QA suite | `FirstPerson.cs` (+1 dòng QA helper), `HospitalGame.QA.cs` (`VerifyM2FinalPhaseCDERegression`) | Bổ sung ca tự động cho `CH01-Q05/Q09/Q10/Q11/Q12/Q13/Q14`, `CH02-Q02/Q03/Q05/Q06`, `V30-Q01`, và `TASK009-C` bật/tắt 2 chiều không kéo chuột trên cả 7 prop kéo được |
| `c459aba` | Phase D bugfix (`TASK009`) | `HospitalScene.cs`, `HospitalAuthoring.ChapterOne.cs` | **Sửa lỗi thực tế do ca `TASK009-C` phát hiện:** `CHAIR-STAFF` ở vị trí nghỉ `x=0.95` lấn 6,35 cm vào tường đông hành lang nhân viên (`x=1.10`), khiến nút `[E]`/`[K]` đẩy ghế vào lại luôn báo `"Chân ghế đang vướng."`. Dịch `closedPos.x` sang `0.87f` (cách tường 1,65 cm, giữ nguyên vị trí kéo ra `(0.81, 0, 2.66)` và hành lang thoát Cửa B) |

### 1.3. Khả năng tái lập build (Reproducibility)
1. Checkout `c459aba86192591b4adf5d267438e5fb4b77f704` với junction ASCII `C:\CTCC-Unity-Workspace -> C:\Ca trực cuối cùng` và Unity `6000.3.28f1`.
2. Dựng lại toàn bộ 9 scene + Windows x64 release: `& tools\build_m2.ps1 -Output <out>\windows-x64` (hoặc `-SkipGenerate` khi giữ nguyên 9 scene bake sạch).
3. Kiểm chứng tự động:
   - Module QA + 4 D3D12 failure probes: `& tools\verify_windows.ps1 -Executable <exe> -Evidence <dir>\module -GraphicsApi D3D12 -Quality Medium -TimeoutSeconds 600` và `-ProbeFailure module-failure|escape-premature|invalid-reasoning|broken-progression`.
   - Foreground Player Input QA (S1 + Full 19/19): `& tools\verify_windows.ps1 -PlayerInput -S1Only` và `& output\m2-final\run-full-input-qa.ps1`.
   - Benchmark 3 lượt độc lập 60 Hz (D3D12, Medium, VSync 1): `& output\m2-final\measure-phase-b-windows.ps1` + `python tools\summarize_m2.py`.

---

## 2. Phân tách trạng thái theo Task (Implemented / Technical Verified / Human Review Pending / Accepted)

| Task / Cổng | Implemented | Technical Verified (`c459aba`) | Human Review Pending | Accepted | Ghi chú bằng chứng |
| --- | :---: | :---: | :---: | :---: | --- |
| **Phase A — Baseline & Core v3.3.1 Lock** | Có | Có | Không | **Accepted** | `10/10` SHA-256 khớp `docs/SHA256SUMS.txt`, DAG 96 task pass |
| **Phase B — Build Recovery & Layer R GPU Budget** | Có | Có | Không | **Accepted** | Đã đóng trên `fb2e96d` và giữ nguyên trên `c459aba` (GPU P95 `12,77–12,97 ms` Unity / `13,74–13,88 ms` PresentMon) |
| **`TASK025` (M1 Transaction, F101 Revisions, Save v7)** | Có | Có | Không | **Accepted (M1 Tech)** | `TASK025-A` (A/B/C), `TASK025-B/T02/T03`, `T08` + `T08 deferred`, migration schema 1–7 pass 100% |
| **`TASK007` (CH01 B01–B08 theo `DEC-CH01R-04`)** | Có | **Có** | Cần playtest người thật (`TASK010`) | Chưa đóng M2 | `CH01-Q01–Q14`, `CH01-WT-Q01/Q02`, `CH01R-Q01/Q02`, `TASK03/04–05`, `INPUT CH01-R FULL ROUTE PASS` |
| **`TASK008` (CH02 PZ01/PZ02, E11/E12, C01, G02, Cameo)** | Có | **Có** | Cần playtest người thật (`TASK010`) | Chưa đóng M2 | `CH02-Q01–Q06`, `LOG06-Q01`, `V30-Q01`, Door A + Door B + Story Mode + mute/reduced motion pass |
| **`TASK009` (LOC01–04 Kit, Temp VO, Captions, Accessibility)** | Có | **Có** | Cần duyệt art/VO/license (`CHAR-Q02/Q09`, `TASK026`) | Chưa đóng M2 | `AU01`, `T06`, `TASK009-C` (7/7 prop toggle 2 chiều sau sửa `CHAIR-STAFF`), 130% Large-Print, Mute, Reduced Motion, `[E]/[K]` |
| **`TASK030` / Expansion CH03–CH04 + Campus-v1 + S1 + XYZ** | Có | **Có (phạm vi CH03–CH04)** | Review `TASK026–028` & mở rộng M3 | Chưa đóng M3 | `CH03-Q01–Q05`, `CH04-Q02–Q06`, `INPUT CH03–04 PASS`, `V33-Q05` (S1/K-B/K-D/K-F/LOC01), `WORLD-Q03/Q04`, `W-Q02/W-Q03` |
| **`TASK010` (Playtest 6 người chơi mới)** | Bộ kit sẵn sàng | Đã chuẩn bị binary + script + rubric | **Đang chờ (0/6 phiên)** | **Chưa** | **Chặn cổng M2.** Không giả lập người chơi; xem [playtest-kit-task010.md](playtest-kit-task010.md) |
| **`TASK026` (Chuyên gia Y tế / Kiến trúc BV)** | Hồ sơ sẵn sàng | Chưa có chữ ký chuyên gia | **Đang chờ ký duyệt** | **Chưa** | **Chặn cổng M2 / Art Lock.** Xem [specialist-review-dossiers.md](specialist-review-dossiers.md) |
| **`TASK027` (Chuyên gia PCCC lịch sử 2005)** | Hồ sơ sẵn sàng | Chỉ xác minh route kỹ thuật (`V33-Q05`, `W-Q02/03`) | **Đang chờ ký duyệt** | **Chưa** | **Chặn cổng M2 / Art Lock.** Cần thẩm định 3 mục (mặt cắt C, khói, ứng cứu) |
| **`TASK028` (Chuyên gia Y tế về Provenance & Đạo đức)** | Hồ sơ sẵn sàng | Đã kiểm tự động `LOG06-Q01`, `V22-Q03` | **Đang chờ ký duyệt** | **Chưa** | **Chặn cổng M2 / Art Lock.** |

---

## 3. Kết quả kiểm chứng kỹ thuật & Ma trận phủ QA (`c459aba`)

### 3.1. Tổng hợp 4 bộ chạy tự động trên `c459aba`
1. **Full Module QA (D3D12, Medium):** **`30/30` PASS (`RESULT=PASS`)**, D3D12 thoát bình thường (`exit_code=0`, `[Physics::Module] Cleanup current backend.` → `Input System ... Shutdown.`, 0 exception ngoài 4 ca fault-injection chủ đích). Log: [evidence/m2-final-candidate/module-gameplay.txt](evidence/m2-final-candidate/module-gameplay.txt).
2. **D3D12 Failure Probes:** **`4/4` PASS** (`module-failure`, `escape-premature`, `invalid-reasoning`, `broken-progression` đều trả `exit_code=1` và `RESULT=FAIL` khớp chuỗi lỗi kỳ vọng). Log: [evidence/m2-final-candidate/probes.txt](evidence/m2-final-candidate/probes.txt).
3. **Targeted S1 Input QA (Foreground D3D12):** **`3/3` PASS (`RESULT=PASS`)** (`TASK03 INPUT PASS`, `WARD AFTERMATH PASS Unverified`, `V33-Q05 INPUT S1 PASS`). Log: [evidence/m2-final-candidate/player-input-s1-gameplay.txt](evidence/m2-final-candidate/player-input-s1-gameplay.txt).
4. **Full Player Input QA (Foreground D3D12, `436,8 s`, `timeout=False`, `exit_code=0`):** **`19/19` PASS (`RESULT=PASS`)**, gồm tuyến CH01-R không teleport từ AP00 đến CH02, cả 3 nhánh F101 + Door A/Door B + CH03–04 + Journal 100/130% + 100 chu kỳ Retention (`222→222` RectTransforms) + 100 chu kỳ soi giấy 2 mặt + đèn xiên (`0` mesh rò rỉ, `0` material rò rỉ, `315,17→315,32 MB` managed). Log: [evidence/m2-final-candidate/player-input-full-harness.txt](evidence/m2-final-candidate/player-input-full-harness.txt).

### 3.2. Ma trận phủ QA (Expected / Actual / Status)

#### A. Phạm vi M1/M2 CH01–CH02 & Tích hợp (`TASK007`, `TASK008`, `TASK009`, `TASK025`)
| ID QA | Expected (Core 07) | Actual trên `c459aba` | Trạng thái |
| --- | --- | --- | --- |
| `CH01-Q01` | A không inspect → G01; D=1; FS02; G02 false | `TASK025-A PASS Affirmed` + `INPUT PASS Affirmed` + `WARD AFTERMATH PASS Affirmed` | Technical Verified |
| `CH01-Q02` | B từ đầu không OBS → D=0, collapse & loop vẫn xảy ra | `TASK025-A PASS Unverified` + `INPUT PASS Unverified` + `broken-progression` probe | Technical Verified |
| `CH01-Q03` | Ký C sai bị chặn; đủ phản chứng → D=0, G01 mở, không cấp E11/E12 ở CH01 | `REBIRTH II F101 PASS` + `TASK025-A PASS Refuted` + `invalid-reasoning` probe | Technical Verified |
| `CH01-Q04` | A→B→C, submit lặp → D=0, first=A, không cộng revision trùng | `TASK03 F101 PASS` + `TASK03 INPUT PASS` + `TASK025-A PASS Affirmed` (r3, D=0) | Technical Verified |
| `CH01-Q05` | Đứng trong hốc Giường 4 lúc đổi vách → chờ pad, không clipping, AP04 clear | `TASK007 / CH01-Q04–Q14 PASS` (`PadRequired` tại `(11.30,0.02,4.20)`, `AP04` clear) | Technical Verified |
| `CH01-Q06` | Skip Lan, quay đi khi PA → Board & nội dung thiết yếu vẫn có | `INPUT CH01-R FULL ROUTE PASS` (Lan cinematic+skip) + `M2A-DIALOGUE PASS` + `CH01-R ADVERSARIAL PASS` | Technical Verified |
| `CH01-Q07` | Thoát / lỗi ghi giữa T07/T08/T09; load → snapshot hoàn chỉnh, không double event | `T08 PASS` + `T08 deferred PASS` + `REBIRTH II SAVE FAILURE PASS` + `WT B->R swap fails cleanly` | Technical Verified |
| `CH01-Q08` | Thử cổng trước/sau collapse nhiều lần → loop đúng 1 lần, AP01 clear, không kẹt | `WT B07 SEAM fires once and refuses repeat` + `AP01` clear + `KEY_GATE` giữ nguyên | Technical Verified |
| `CH01-Q09` | Cạn pin (`0 s`), mute (`0`), reduced motion → vẫn đến CP04, đọc đủ thông tin, sàn pin `120 s` khi load | `TASK007 / CH01-Q04–Q14 PASS` (`torch=0`, `AudioListener.volume=0`, đến `CH01-CP04`, reload sàn `>=120 s`) | Technical Verified |
| `CH01-Q10` | Đổi binding, UI chỉ dùng bàn phím → prompt `[K]` đúng; chọn/confirm/thoát được | `T06 keyboard subtest PASS` + `TASK007` (`UseKey=="K"`, `[K]` trên `GATE-CONG` & `SEAM`) | Technical Verified |
| `CH01-Q11` | Đi thẳng LOC04 / cầu thang trong CH01 → gate đóng hợp lý, không sequence break | `TASK007 / CH01-Q04–Q14 PASS` (`emergencyDoorA/B` đóng, `G01/G02=false`) + `V33-Q05 S1 pre-G02 PASS` | Technical Verified |
| `CH01-Q12` | Bỏ inspect E10a / bỏ FS04 → E10a sẵn, fallback về sau giữ nguyên | `TASK007 / CH01-Q04–Q14 PASS` (`State.Has("E10a") && !State.Happened("FS04")`) | Technical Verified |
| `CH01-Q13` | Load CP04 vào CH02 → R, G01 true, G02 false, giữ phiếu và OBS | `TASK007 / CH01-Q04–Q14 PASS` + `TASK025-A PASS` (3 nhánh) | Technical Verified |
| `CH01-Q14` | Cả 3 route CH01: không lộ AS-05 / Hội đồng, không D16b | `TASK007 / CH01-Q04–Q14 PASS` (quét `CH01-D01..D16`) + `V22-Q03 runtime PASS` | Technical Verified |
| `CH01-WT-Q01/Q02` | Walkthrough B01–B08 (module + input không teleport + 50-cycle Backpack + 4 bề mặt đặt giấy) | `CH01 WALKTHROUGH & PROPOSAL V1.1 100% PASS` + `INPUT CH01-R FULL ROUTE PASS` | Technical Verified |
| `CH01R-Q01/Q02` | CH01-R Adversarial + probes + stress 100 chu kỳ soi 2 mặt | `CH01-R ADVERSARIAL PASS` + `INPUT STRESS PASS` (`0` mesh/`0` mat) + probes `exit 1` | Technical Verified |
| `CH02-Q01` | Cả 3 route A/B/C → G02 đạt, F101 sửa được, không tạo E11 từ M | `TASK025-A PASS` (A/B/C) + `INPUT PASS` (A/B/C) | Technical Verified |
| `CH02-Q02` | Thiếu từng mảnh E11 chặn BundleComplete/G02; nhặt lặp không nhân bản | `TASK008 / CH02-Q01–Q06 & V30-Q01 PASS` (kiểm từng mảnh `drawing`, `wristband`, `transferNote` + duplicate) | Technical Verified |
| `CH02-Q03` | Bỏ Bé An, mute, reduced motion → PZ01/02 giải bằng dấu/nhãn; chase có cảnh báo chữ | `TASK008 / CH02-Q01–Q06 & V30-Q01 PASS` (`VerifyAn` không vignette, `CH02-D05` vào transcript khi mute) | Technical Verified |
| `CH02-Q04` | Đứng ở hốc khi đổi lớp; load CP02/03 → safe pad/rollback, không nhân đôi vật chứng | `TASK025-B / T02 / T03 PASS` (30 swap + 30 từ chối + 10 reload) + `TASK008` duplicate check | Technical Verified |
| `CH02-Q05` | Thử cả 2 lối Door A/Door B đi bộ, chế độ Story, không chết, T06 không lặp | `CH02-Q05 PASS` + `INPUT PASS Door A` + `INPUT PASS Door B` + `TASK008` Story Mode hold | Technical Verified |
| `CH02-Q06` | Không spoil liên kết 12 mã với hồ sơ tử vong hay mức trách nhiệm Minh | `TASK008 / CH02-Q01–Q06 & V30-Q01 PASS` (`!H02`, `!H05`, chưa có `E04`/`E07`) | Technical Verified |
| `LOG06-Q01` | Thiếu phiếu E11 chưa complete; E12 không tự xác định danh tính | `TASK008` + `CH01-R ADVERSARIAL PASS` (`LOG06-Q01 card content`) | Technical Verified |
| `TASK009-C` | Large-Print 130%, Mute, Reduced Motion, Keyboard-only, bật/tắt 1 lần thay giữ-kéo | `TASK009-C ACCESSIBILITY & TOGGLE PASS` (7/7 prop kéo được 2 chiều gồm `CHAIR-STAFF` sau sửa + `SEAM` `[K]`) + `AU01 spatial PASS` + `T06` | Technical Verified |
| `TASK025-A/B`, `M1P-*` | Layer transaction, 30 swaps/denials, atomic save fault/recovery, 100-cycle inspect | `TASK025-A/B`, `T08`, `T08 deferred`, `INPUT STRESS PASS` | Technical Verified |
| `CHAR-Q01/03/04/05/06/07/08` | Giải phẫu/LOD/stride/lip-sync/0-byte GC/shader/interruption | `CHAR-Q01/Q04/Q07/Q08 PASS` (`Lan=0.008`, `GK=0.009 m/s`) + `CHAR-Q05/Q06/Q08 PASS` (`0 B` GC) | Technical Verified |
| `CHAR-Q02`, `CHAR-Q09` | Duyệt thẩm mỹ R/B và kiểm hồ sơ bản quyền nguồn ngoài | Cần người duyệt hình ảnh & license trước Art Lock | **Human Review Pending** |

#### B. Phạm vi CH03–CH04, Campus-v1 & Core v3.3.1 (In-Scope)
| ID QA | Expected (Core 07) | Actual trên `c459aba` | Trạng thái |
| --- | --- | --- | --- |
| `CH03-Q01/02/03/04/05` | C02/C12 bất biến, LOS/patrol vật cản, E03 atomic save, M restore, không spoil | `CH03-Q03 local module PASS` + `CH03-Q01/Q02/Q04/Q05 ... PASS` + `INPUT CH03–04 PASS` | Technical Verified |
| `CH04-Q01` | Hai lối PZ04 vật lý trong không gian đầy đủ | Chưa triển khai đầy đủ level PZ04 (phạm vi M3 Alpha) | **Chưa test (M3 scope)** |
| `CH04-Q02/03/04/05/06` | 16 tổ hợp công tắc, H05/PT02, E06a≠E06, B1 save, D16b guard | `CH04-Q02–Q06 module PASS` + `INPUT CH03–04 PASS` + `V22-Q03 runtime PASS` | Technical Verified |
| `W-Q02`, `W-Q03`, `WORLD-Q03`, `WORLD-Q04` | Tuyến S1 độc lập B1, hố quan sát E05b trước G06 (tường 2004 kín), 3D XYZ multi-wing/multi-floor & `Campus_Interiors` streaming | `WORLD-Q03/Q04 & W-Q02/W-Q03 PASS` | Technical Verified |
| `WORLD-Q02`, `WORLD-Q06` | Tuyến LOC01–04 đủ gate/seam & đo hiệu năng Windows chuẩn đối chứng baseline | `INPUT CH01-R FULL ROUTE PASS` + 3x PresentMon 60 Hz (`12,77–12,97 ms` Unity GPU P95) | Technical Verified |
| `W-Q01/04/05/06/07`, `WORLD-Q01/05` | Thẩm định mặt bằng, luồng công năng/bẩn, tính lịch sử props & hồ sơ quyền | Kỹ thuật đã dựng đúng Campus-v1; cần xác nhận chuyên gia `TASK026–028` | **Human Review Pending** |
| `V22-Q03`, `LOG10-Q01` (phạm vi CH01/04) | Chặn D16b ở CH01/02, chỉ mở CH04 B06 khi đủ `H05 && PT02` | `V22-Q03 partial PASS` + `V22-Q03 runtime PASS` (phần CH07/E08 thuộc M3) | Technical Verified (phần CH01–04) |
| `V22-Q04`, `V31/V32/V33/V331-DOC01` | Toàn vẹn gói Core v3.3.1, 143 dòng Dialogue, DAG 96 task, 10/10 SHA-256 | `project_docs.py check` + `10/10` SHA-256 khớp tuyệt đối | Technical Verified (Doc Lock) |
| `V30-Q01`, `V30-Q06` | Không spoil AS-05/Hội đồng ở CH01; `CH02-D08` có AS-05 đợt 4; hồi quy M1/M2 100% | `TASK007` + `TASK008 / V30-Q01 PASS` + Save v7 regression pass | Technical Verified |
| `V33-Q05` (phạm vi Campus/CH01–04) | S1 thông R trước G02, cửa xả S1 bị rào ở R và thông ở M CH05, rào K-B/K-D/K-F ở R, cổng LOC01 sau T09 giữ mở vật lý + phản hồi diegetic (`DEC-V331-01`) | `V33-Q05 R outer gate seals PASS` + `S1 discharge PASS` + `S1 streaming PASS` + `S1 pre-G02 PASS` + `V33-Q05 INPUT S1 PASS` + `WT B07 post-T09` | Technical Verified (phần đã triển khai; phần CH05–07/CHAR007 đầy đủ thuộc M3) |
| `V31-Q08` | Tuyến B1/Campus không nối trực tiếp `LOC10 → C` | `W-Q03` xác minh vách kín `LOC12` tại `E05b`; thẩm định chuyên gia chờ `TASK027` | Technical Verified (kỹ thuật) / **Human Review Pending** |

#### C. Phạm vi CH05–CH08 / Endings (KHÔNG thực hiện trong M2-FINAL — KHÔNG đánh dấu Pass)
- `CH05-Q01–Q05`, `CH06-Q01–Q06`, `CH07-Q01–Q06`, `CH08-Q01–Q07`, `LOG01–05`, `LOG07–09`, `LOG11`, `V22-Q01/Q02`, `V30-Q02–Q05`, `V31-Q01–Q07/Q09–Q12`, `V32-Q01–Q05`, `V33-Q01–Q04/Q06`: **Ngoài phạm vi triển khai M2 (CH05–CH08 / M3–M5)** — giữ nguyên trạng thái `Chưa test`.

---

## 4. Tóm tắt hiệu năng (3 lượt độc lập 60 Hz trên `c459aba`)

Cấu hình tham chiếu: Intel Core Ultra 7 155H, Intel Arc Graphics (Driver D3D12 level 12.2), RAM `15.868 MiB`, Windows 11, `1920×1080`, `Medium` (`render_scale=0.88`), `dual-probes`, `VSync 1`, `60 Hz`, tuyến mở rộng CH01–CH04 (`7` zone hoạt động). Nguồn số liệu: [evidence/m2-final-candidate/performance.json](evidence/m2-final-candidate/performance.json).

| Lượt độc lập | Khung foreground | Launch → Ready | Unity Wall P50 / P95 / P99 | Unity Main P95 | Unity GPU P50 / P95 / P99 / Max | PresentMon DisplayedTime P50 / P95 / P99 | PresentMon GPUTime P50 / P95 / P99 | PresentMon FrameTime P95 | Working Set đỉnh |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| `benchmark-1` | `6.663 / 6.663` (`100%`) | `5,04 s` | `16,67 / 16,68 / 16,68 ms` | `6,88 ms` | `8,13 / 12,97 / 14,52 / 16,29 ms` | `16,66 / 16,75 / 16,83 ms` | `8,76 / 13,88 / 15,83 ms` | `19,06 ms` | `1.755,9 MiB` |
| `benchmark-2` | `6.659 / 6.659` (`100%`) | `4,78 s` | `16,67 / 16,68 / 16,68 ms` | `7,21 ms` | `8,32 / 12,77 / 14,48 / 17,04 ms` | `16,66 / 16,74 / 16,81 ms` | `8,97 / 13,74 / 15,66 ms` | `19,35 ms` | `1.763,3 MiB` |
| `benchmark-3` | `6.655 / 6.655` (`100%`) | `4,75 s` | `16,67 / 16,68 / 16,68 ms` | `7,23 ms` | `8,22 / 12,80 / 14,61 / 17,09 ms` | `16,66 / 16,73 / 16,80 ms` | `8,90 / 13,80 / 15,74 ms` | `19,26 ms` | `1.755,6 MiB` |

- **Đánh giá cổng 16,67 ms:** **ĐẠT (Technical Verified)** trên cả 3 lượt độc lập: Unity GPU P95 `12,77–12,97 ms` (lượt 1 đạt `0/6.640` khung vượt 16,67 ms), PresentMon GPUTime P95 `13,74–13,88 ms` (kể cả **P99** cũng dưới ngưỡng: `15,66–15,83 ms`), Unity Wall P95/P99 `16,68 ms`, PresentMon DisplayedTime P95 `16,73–16,75 ms`.

---

## 5. Lỗi còn mở & Giới hạn đã biết (Known Issues)

1. **`TASK010` — Chặn cổng M2 (Human Review Pending):** `0/6` phiên chơi thử người mới đã thực hiện. Cần chạy đủ 6 người mới không biết mã nguồn/tài liệu trên gói `c459aba` bằng `start-task010-session.ps1` và đạt độc lập 3 ngưỡng 5/6 theo Core 01/07.
2. **`TASK026`, `TASK027`, `TASK028` — Chặn cổng M2 / Art Lock (Human Review Pending):** Cần biên bản ký duyệt của chuyên gia kiến trúc/y tế, chuyên gia PCCC lịch sử 2005 và chuyên gia nghiệp vụ hồ sơ/đạo đức.
3. **Độ phủ Temp VO (`TASK009-B`):** Đóng gói 36 tệp WAV (`CH01`: 23/28 — 5 ID còn lại là mục tiêu UI `CH01-OBJ01–06`, SMS `CH01-D15`, caption tùy chọn `CH01-D17/18`; `CH02`: 5/8 có thoại `D01..D05`, 3 dòng `D06..D08` hiện phụ đề + bản ghi; `CH03`: 5/7; `CH04`: 0/10 nằm ngoài phạm vi VO tạm LOC01–04).
4. **`CH04-Q01` (`PZ04` dual-route level puzzle):** Chưa dựng level đầy đủ cho `PZ04`; giữ trạng thái `Chưa test` cho giai đoạn M3 Alpha.
5. **PresentMon `FrameTime` P95 dưới VSync 1 (`19,06–19,35 ms`):** Phản ánh jitter hàng đợi flip D3D12 chuẩn dưới VSync 1 (tương đương baseline M2C1 `19,54–20,13 ms`), trong khi `DisplayedTime` P95 là `16,73–16,75 ms` và `GPUTime` P95 là `13,74–13,88 ms`.
6. **Cảnh báo biên dịch Editor `CS0618`:** `Lightmapping.giWorkflowMode` ở `HospitalAuthoring.Lighting.cs:253` (chỉ thuộc mã Editor Unity 6.3, không đi vào Player release).
7. **Duyệt nghệ thuật & bản quyền (`CHAR-Q02`, `CHAR-Q09`):** Còn mở bước thẩm định thẩm mỹ nhân vật R/B và kiểm kê điều khoản nguồn trước Art Lock.
