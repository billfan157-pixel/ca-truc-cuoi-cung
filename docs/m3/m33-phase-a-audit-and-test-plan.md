# CTCC — M3.3 Phase A: Baseline, Source Audit, GAP Matrix & Test Plan

- **Ngày kiểm toán:** `2026-11-25`
- **Phạm vi:** `M3.3 — Evacuation Memory & Architectural Investigation` (`TASK013-A`, `TASK013-B`, `TASK013-C` — trọn vẹn `CH05 B01–B08`)
- **Nhánh làm việc:** `feat/m33-ch05-evacuation-pz07` (tách sạch từ `main` tại `ad022200b45a930ebbcd633961ee3667f2cba0a3`)
- **Bộ nguồn chuẩn (Canonical Design Lock):** `Core v3.3.1 Final Clean` tại git revision `3c0b955de95bcf39853c30e2669b2bac24ad969e` (`10/10` SHA-256 khớp tuyệt đối `docs/SHA256SUMS.txt`)
- **Toolchain khóa cứng:** Unity `6000.3.28f1`, URP `17.3.0` Forward+, Windows x64 Direct3D 12, **Save Schema v7** (`Investigation.Schema = 7`, `MinSupportedSchema = 1`)

---

## 1. Phạm vi Ủy quyền Sản xuất Có Kiểm soát (Controlled Production Exception)

Gói M3.3 được thực hiện theo ủy quyền ngoại lệ sản xuất có kiểm soát của chủ dự án dành riêng cho việc phát triển và kiểm chứng kỹ thuật `CH05` (`TASK013-A/B/C`) trong khi các cổng con người (human gates) đang chờ. Ngoại lệ này **không miễn trừ** bất kỳ điều kiện nghiệm thu chính thức nào:

| Hạng mục / Cổng | Trạng thái giữ nguyên trong M3.3 | Ràng buộc bắt buộc |
| --- | --- | --- |
| `TASK010` | `Human Review Pending` (`0/6` external playtesters) | Không giả lập playtest; giữ đúng số phiên thực tế |
| `TASK026`, `TASK027`, `TASK028` | `Human Review Pending` (`0/3` biên bản chuyên gia) | Không tự đóng cổng review chuyên gia |
| `TASK011`, `TASK012` | `Technical Verified` | Chưa chuyển sang `Accepted` khi thiếu human gates |
| `TASK013` (`A/B/C`) | Mục tiêu tối đa: `Technical Verified` | Không tuyên bố `TASK013 Accepted` |
| `M2 Vertical Slice` & `M3 Alpha` | `OPEN` | Không đóng milestone khi cổng con người chưa hoàn tất |
| Art Lock (`CHAR-Q02`, `CHAR-Q09`) | `OPEN` | Tái sử dụng rig/model hiện có, không chốt Art Lock |
| Biên chương sau `CH05` | **KHÓA CỨNG** | Không làm `TASK014–017` (`CH06–08`), không thu `E01/E06/E10b/E08`, không làm `PZ08`, không mở `G06` |

---

## 2. Xác minh Baseline Git, Worktree & Bảo toàn Candidate

### 2.1 Trạng thái Git & Worktree
- `HEAD` gốc trên `main` (`origin/main`): `ad022200b45a930ebbcd633961ee3667f2cba0a3` (working tree sạch `100%` trước khi tách nhánh `feat/m33-ch05-evacuation-pz07`).
- Các worktree hiện hữu trên máy được giữ nguyên vẹn, không can thiệp:
  - `C:/Ca trực cuối cùng` (`feat/m33-ch05-evacuation-pz07` từ `ad02220`) + ASCII junction `C:\CTCC-Unity-Workspace -> C:\Ca trực cuối cùng`
  - `C:/CTCC-NPR-01` (`04464f4` detached)
  - `C:/Users/phanb/.local/share/kilo/worktree-pool/ca-tr-c-cu-i-c-ng-afe3905faf8d/just-sleet` (`c0b1bbd` detached)
- Dung lượng trống ổ `C:`: `~12.5 GB` (đủ cho build candidate M3.3, QA logs/video và đóng gói `.zip`).

### 2.2 Bảo toàn Candidate `c459aba` (M2-FINAL) & `c90a2eb` (M3.2)
Đã kiểm chứng trực tiếp trên đĩa — **không xóa, không ghi đè, không build lại trùng**:

| Candidate | Đường dẫn bảo toàn | `CaTrucCuoiCung.exe` SHA-256 | `Assembly-CSharp.dll` SHA-256 | Trạng thái |
| --- | --- | --- | --- | --- |
| **M2-FINAL (`c459aba`)** | `output/m2-final/m2-final-cand-c459aba/` & `CaTrucCuoiCung-M2-FINAL-c459aba-Windows-x64.zip` | `fbd073ca59c43dfbe64a472ef8752d4e2e373a8a0c588332f768ad3bf130e131` | `373d7c91bfcc50f9f52b19d2038938f0a4bc7cb20a425e6f84b7c0c806a5c54f` | Nguyên vẹn |
| **M3.2 (`c90a2eb`)** | `output/m3.2/m32-cand-c90a2eb/` & `CaTrucCuoiCung-M3.2-c90a2eb-Windows-x64.zip` | `fbd073ca59c43dfbe64a472ef8752d4e2e373a8a0c588332f768ad3bf130e131` | `ac7a8f8eb962e88caaa380bf174568d8de2cf352910b9b120e12751bf938b0ab` | Nguyên vẹn |

---

## 3. Kiểm toán Nguồn: Core v3.3.1 (`3c0b955`) vs Dự thảo Core NPR-01 (`HEAD`)

### 3.1 Xác minh SHA-256 của Bộ Nguồn Khóa `3c0b955`
Đối chiếu toàn bộ `01..07` tại `3c0b955de95bcf39853c30e2669b2bac24ad969e` với `docs/SHA256SUMS.txt`: khớp `10/10` (`07_Production_Database.xlsx` = `adbec7e5a354b74a6a0105174862c6a73fcaadeedb7401fee56e0e4f25f3f7d6`).

### 3.2 Kết quả Diff Chi tiết giữa `3c0b955` và `NPR-01` đối với `CH05`
Đã trích xuất và so sánh từng đoạn văn Word (`01..06`) và từng ô dữ liệu Excel (`07`):
1. **Không có bất kỳ thay đổi nào ở dữ liệu runtime của `CH01–CH05`**:
   - Toàn bộ `8` dòng thoại `CH05-D01..D08`, các dòng `Events` (`CH05-T01..T08`, `EVT010`, `EVT026`), `Chapter gates!6` (`CH05`), `Puzzle specs` (`CH05-PU01`, `CH05-PU02`), `Evidence Specs` (`E05`, `E05b`), `Foreshadowing` (`FS14`, `FS22`), `QA` (`CH05-Q01..Q05`) và `Locations` (`LOC05`, `LOC07..LOC12`) tại `HEAD` **trùng khớp 100%** với `3c0b955`.
   - Sáu câu thoại thay đổi trong dự thảo `NPR-01` chỉ nằm ở `CH07` và `CH08` (`CH07-D19`, `CH07-D23`, `CH07-D24`, `CH07-D27`, `CH07-D28`, `CH08-D20`), kèm sửa mô tả `Provenance Matrix!2` (`E01-A/B`), `PZ10 Matrix!2,4` và `Ending Matrix!4` (`END03`).
2. **Hai ghi chú `NPR-01` liên quan gián tiếp tới `CH05`**:
   - `NPR-01-04` (`EVT026` / `TASK027`): giữ nguyên hợp đồng sơ tán `6` bệnh nhân tại `AN05`, tuyến `S1` độc lập `B1`, và cửa thoát hiểm Đông xây bít năm `2004`; chỉ nhắc việc chờ biên bản an toàn kỹ thuật con người (`TASK027`).
   - `NPR-01-11`: định hướng preproduction art, không thay đổi logic hay thoại `CH05`.
3. **Chuẩn hóa công cụ chọn nguồn & Provenance (`project_docs.py` & `sync_production_data.py`)**:
   - Đã nâng cấp `docs/agent/project_docs.py` bổ sung cờ `--rev <git-rev>` và `--design-lock` (`3c0b955de95bcf39853c30e2669b2bac24ad969e`), cho phép đọc trực tiếp blob từ Git đối tượng khóa mà không phụ thuộc bản dự thảo trên working tree.
   - Đã nâng cấp `tools/sync_production_data.py` mặc định đọc từ `DESIGN_LOCK_REV` (`3c0b955de95bcf39853c30e2669b2bac24ad969e`), xuất `61` dòng thoại `CH01–CH05` (`28 + 8 + 7 + 10 + 8`), loại trừ tuyệt đối mọi câu thoại dự thảo `CH07/CH08`, đồng thời ghi trực tiếp `sourceRevision`, `sourceFile` và `sourceSha256` vào `Prototype/Assets/CTCC/Art/Resources/CTCC/Dialogue.json` và đọc vào `HospitalGame` để QA kiểm chứng runtime.

---

## 4. Đặc tả Chuẩn `CH05` từ Bộ Nguồn Khóa (`3c0b955`)

### 4.1 Luồng Beat, Trigger & Checkpoint (`B01–B08`)
| Beat | Trigger / Checkpoint | Lớp | Hợp đồng Gameplay & Narrative theo Core 04 / Core 07 |
| --- | --- | --- | --- |
| **B01** | `CH05-T01`, `CH05-CP01` | `R → M` | Điều kiện vào: `G04` (`E04 + E07`, `H05.verified`, `PT01.presented`, `PT02.presented`) + `CH04-T06`. Tương tác xe bệnh nhân `AN05` tại Khu B (`LOC05`, Tầng 2): ghi `CH05-CP01` ở `R` **trước** khi chuyển sang `M`, tắt tiếng mưa, bật chuông báo cháy + ánh sáng vàng ấm + nhãn lớp `M`. |
| **B02** | `CH05-T02` (`CH05.Pair1`) | `M` | **Nhóm 1 (2 bệnh nhân):** mở lối hành lang Khu B, hướng dẫn 1 bệnh nhân nắm tay/đi sát Minh + phối hợp đồng nghiệp điều dưỡng đẩy xe lăn bằng `[E]`/`[K]`. Loa hành lang phát `CH05-D07` (`FS22` — lời cảnh báo của Vĩnh trong `M`, **không phải** bằng chứng khách quan xác nhận rò rỉ sinh học). Thoại `CH05-D01`, `CH05-D02`, `CH05-D03`. |
| **B03** | `CH05-T03` (`CH05.Pair2`) | `M` | **Nhóm 2 (2 bệnh nhân):** dẫn qua vật cản hành lang có khói dẫn hướng (không nhấp nháy, không timer tử vong), gọi đồng nghiệp tại điểm hẹn A, mở cửa và đưa xe lăn qua bằng thao tác nhấn đơn `[E]`/`[K]` (không bắt giữ kéo chuột). |
| **B04** | `CH05-T04` (`CH05.Pair3`) | `M` | **Nhóm 3 (2 bệnh nhân):** đưa 2 bệnh nhân cuối theo cầu thang đầu hồi `S1` độc lập với `B1` (`LOC05 → chiếu nghỉ trệt S1 → cửa xả sân S1` mở trong `M` theo `DEC-V331-01`), thoáng thấy chìa Khu C (`FS14`) trên tay Minh khi mở cửa Khu B, kiểm đếm đủ **`6/6`** tại bảng tập kết (`CH05-PU01` solved). |
| **B05** | `CH05-T05`, `CH05-CP02` | `M → R` | Vĩnh khen Minh (`CH05-D04`: *“Cậu làm tốt lắm.”* — tạo nền tương phản cho `CH07`). Minh nhìn chùm chìa khóa Khu C nhưng cảnh cắt **trước** các mốc `03:09–03:12`, `~03:25`, `07:40` và không hàm ý việc cứu 6 người xóa bỏ trách nhiệm khác. Trở về `AN05` ở `R`, ghi `CH05-CP02`, lưu tham chiếu `MemEvacKhuB` (`MemoryOnly`, không xuất vật chứng từ `M`), giải phóng toàn bộ actor/khói/chuông của `M`, khôi phục tấm chắn sắt cửa xả `S1` ở `R`. |
| **B06** | `CH05-T06` | `R` | Tại `LOC09` (Kỹ thuật B1): mở tủ bản vẽ thu **Sơ đồ thoát hiểm niêm yết 2005** (tài liệu đối nghịch vẽ lối thoát phía Đông Khu C) và **`E05` — Bản vẽ hoàn công tháng 06/2004** (ký duyệt bởi Giám đốc Phạm Đức Vĩnh, xây gạch đặc 220 mm bít vĩnh viễn cửa thoát hiểm Đông tại tọa độ thiết kế `(34.5, 42)` theo yêu cầu cô lập của đơn vị tài trợ). Phát `CH05-D05`. Giải `PZ07` (`CH05-PU02`) căn chồng 2 bản vẽ theo **2 mốc kiến trúc** (`Cầu thang S2` & `Hệ cột trục Đông`). Quan sát tùy chọn van bồn AS-05 & tấm chắn gió bị chèn (`DEC-V32-03` → `ValveSabotageFound` + `CH05-D08`, chỉ tạo nghi vấn đối chiếu mùi dung môi `E09`, không kết luận thủ phạm/nguồn lửa/thời điểm). **Chỉ căn overlay chưa mở `H03`/`G05` (`CH05-Q03`).** |
| **B07** | `CH05-T07` | `R` | Theo hành lang kiểm tra bao quanh phía Bắc (`LOC09 → cửa Đông LOC11 → cửa Bắc ra hào kỹ thuật → hố quan sát phía Đông ngoài vách `x = 34.62, z = 42.0``) soi trực tiếp bức tường gạch xây bít năm 2004 tại `R`, ghi nhận **`E05b`** + phát `CH05-D06`. Khi có đủ **`E05` (đã căn `PZ07`) + `E05b`**, xác minh **`H03`** và mở **`G05`** (mở khóa cánh cửa `LOC08` trên Tầng 3). |
| **B08** | `CH05-T08`, `CH05-CP03` | `R` | Rời `B1` qua `S2 → LOC04 → S3/S1 → LOC05 → LOC07` (Tầng 3), vượt lượt tuần tra ngắn của `CHAR005` Hunter bằng đường vòng phòng họp / radio đánh lạc hướng (hoặc `Story Mode`), tới ngưỡng cửa `LOC08` vừa mở khóa `G05`, ghi checkpoint bàn giao **`CH05-CP03`** (*“Kiểm tra báo cáo”*) và dừng sạch tại ranh giới `CH05 → CH06`. |

---

## 5. Đối chiếu Tọa độ Thiết kế vs Geometry Thực tế (Không Đục Thêm Cửa)

Đã kiểm tra trực tiếp `HospitalAuthoring.Zones.cs` và `HospitalAuthoring.Expansion.cs`:

| Không gian / Điểm mốc | Tọa độ thiết kế (Core 03/07) | Geometry runtime thực tế đã dựng | Kết luận kỹ thuật |
| --- | --- | --- | --- |
| `LOC05` (Khu B, Tầng 2) | Khu nội trú tầng trên, nối `S1` & `S3` | `x ∈ [-4.0, 5.5]`, `y = 4.0`, `z ∈ [30.0, 38.0]` + hành lang Tây nối thẳng `S1` (`x = -12.3, y = 4.0, z = -4.0..37.05`) | Đặt xe bệnh nhân `AN05` + pad `AP10` ngay trong sảnh Khu B (`LOC05`, `y = 4.0`), thông suốt 100% tới `S1` mà không đi qua `B1`. |
| Cửa xả `S1` ra sân | Độc lập `B1`, khóa sắt hàn năm 2012, mở đêm cháy 2005 | `s1DischargeDoor` + `s1DischargeSalvage` tại `(-2.05, 0.0, -7.86..-8.18)` nối thẳng ra sân `LOC01` | Đã có sẵn cơ chế `memoryS1Discharge` trong `HospitalScene.Present` và test `V33-Q05`! Chỉ cần dẫn Nhóm 3 xuống chiếu nghỉ `S1` tới ngưỡng xả trong `M`. |
| `LOC09` (Kỹ thuật B1) | Trục kỹ thuật cạnh thang `S2` | `x ∈ [4.5, 10.5]`, `y = -3.4`, `z ∈ [31.0, 45.0]` (`AP08` tại `(7.0, -3.4, 33.4)`) | Đặt tủ bản vẽ `E05` + bàn căn `PZ07` dọc tường Đông/Bắc `LOC09` và cụm van bồn AS-05 tùy chọn (`DEC-V32-03`) thông thoáng, không cản lối đi. |
| `LOC11` & Hố quan sát `E05b` | Tọa độ thiết kế bản vẽ `(34.5, 42)` | **Đã dựng sẵn hoàn chỉnh** tại `BuildB1E05bObservationGallery` (`HospitalAuthoring.Expansion.cs:224-277`): `LOC09 → cửa Đông (10.5, -3.4, 42) → LOC11 → cửa Bắc (12.5, -3.4, 45) → hành lang bao Bắc → hố quan sát Đông (35.05..36.3, -3.4, 42.0)` nhìn thẳng vào `CTCC_ENV_B1SealedDoorE05b` (`E05B_SEALED_DOOR` tại `(34.62, -3.4, 42.0)`), ngăn cách tuyệt đối với `LOC12` bằng tường đặc `x = 34.5` và cửa khóa `G06` tại `x = 14.5`! | **Không tạo thêm cửa, không đổi topology.** Tái sử dụng chính xác `E05B_SEALED_DOOR` và tuyến gallery đã pass `W-Q03`. |
| Ngưỡng `LOC08` (Tầng 3) | Sau `LOC07`, mở bởi `G05` | Tấm chặn `LOC08 · Khóa G05` đã dựng sẵn tại `(3.5, 8.0, 31.4..33.2)` ngăn `LOC07` (`x ≤ 3.5`) với tiền sảnh `LOC08` (`x ∈ [3.5, 9.5], y = 8.0, z ∈ [30.0, 38.0]`) | Gắn tham chiếu `loc08GateBlocker` trên `ZoneRegion` của `LOC05` để mở khi `GateService.G05(state)` đạt và đặt `CH05-CP03` (`AP11`) tại ngưỡng `LOC08` trong khi giữ khóa cứng `G06`. |

---

## 6. Ma trận GAP Kỹ thuật (Phase B–E)

| GAP ID | Module hiện tại | Phát hiện kiểm toán | Giải pháp triển khai trong M3.3 |
| --- | --- | --- | --- |
| `GAP-M33-01` | `HospitalAuthoring.Expansion.cs` (`AN05`) | M3.2 đặt biển đồng tạm `AN05` trong `LOC06` (`4.84, 5.04, 46.72`), trong khi Core 02/04/07 quy định `AN05` là **xe bệnh nhân tại Khu B (`LOC05`)**. | Dựng xe bệnh nhân `AN05` + safe pad `AP10` tại `LOC05` (`y = 4.0`); chuyển điểm cũ ở `LOC06` thành ghi chú bàn giao chỉ hướng sang xe bệnh nhân Khu B và giữ tương thích ngược cho save M3.2 cũ. |
| `GAP-M33-02` | `Protocol.cs` (`GateService`) | `G02`, `G03`, `G04` đang kiểm tra cứng chuỗi chương đến `"CH04"`, sẽ trả `false` khi `state.chapter == "CH05"`; chưa có `H03` và `G05`. | Mở rộng `G02/G03/G04` giữ tính đơn điệu khi `chapter == "CH05"`; bổ sung `H03(state)` (`E05 + E05b + PZ07.Aligned + H03.verified`) và `G05(state)` (`chapter == "CH05" && H03(state)`). |
| `GAP-M33-03` | `Protocol.cs` (`EvidenceRegistry`) | Chưa đăng ký `E05`, `E05b`; `Grant()` mới chỉ cho phép truy hồi chéo `CH03` trong `CH04`. | Đăng ký `E05` (`R, CH05, LOC09, exportable: true`) và `E05b` (`R, CH05, LOC11, exportable: true`); mở rộng quy tắc truy hồi vật chứng `R` của các chương trước tại đúng `LOC` gốc (`E02/E09`, `C02DaughterLetter`). |
| `GAP-M33-04` | `AtomicSaves.cs` (`Valid()`) | Whitelist `chapter` dừng ở `"CH04"`, `servicePower != 0` bị từ chối ngoài `"CH04"`, whitelist `puzzleId` chưa có `CH05-PU01/02`. | Mở rộng `Valid()` hỗ trợ `"CH05"`, bảo toàn `ValidPower(servicePower)` sang `"CH05"`, thêm `CH05-PU01` & `CH05-PU02`, kiểm chặt bất biến `G04 + CH04-T06` khi vào `CH05` và khóa cứng `!Has("E08")`, `!Happened("G06.opened")`. **Giữ nguyên 100% Save Schema v7 (không thêm trường tuần tự hóa mới).** |
| `GAP-M33-05` | `RealityTransaction.cs` (`Restore()`) | `Restore()` hiện ép mọi `Reality.M` về `Reality.R`, làm mất trạng thái đang ở trong `M` nếu người chơi lưu/tải giữa các nhóm sơ tán (`CH05-Q02`). | Cho phép save giữa từng nhóm của `CH05` (`CH05-T01` đã có, chưa `CH05-T05`) khôi phục đúng phân đoạn kế tiếp tại `AP10` trong `M` **hoặc** thoát an toàn về `AP10` ở `R` theo lựa chọn người chơi mà không nhân đôi hay xuất vật chứng `M` (giữ nguyên hành vi `CH03-Q04` cho `CH03`). |
| `GAP-M33-06` | `RegionStreamer.cs` & `HospitalScene.cs` | `lastServiceGate` và điều kiện stream `S2` (`atEmergencyS2`) đang kiểm `state.chapter == "CH04"`, khiến cầu thang `S2` xuống `B1` bị đóng trong `CH05`. | Mở rộng sang `state.chapter is ("CH04" or "CH05")`; pin `LOC05` cho `AP10`/`AP11`; mở `loc08GateBlocker` + làm mới NavMesh vùng `LOC05` khi `G05` mở. |
| `GAP-M33-07` | `EnvironmentalAudio.cs` | Trong `Reality.M`, `ReportLayer == false` nên các loop mưa năm 2012 vẫn phát nếu không tách cờ âm thanh sơ tán `M`. | Bổ sung chế độ `MemoryEvacuationLayer`: tắt êm mưa `R` trong `M` của `CH05`, phát vòng chuông báo cháy cơ học ấm `CH05_FireBell.wav` (tôn trọng `volume=0` mute & ducking) và giải phóng sạch khi về `R`. |
| `GAP-M33-08` | `ChapterFive.cs` & `HospitalGame.ChapterFive.cs` | Chưa có state machine `TASK013-A` cho 3 nhóm × 2 bệnh nhân tại `AN05`. | Xây dựng `ChapterFive` + `EvacuationDirector` điều phối tuần tự Nhóm 1/2/3 trên rig nhân vật modular sẵn có, khói dẫn hướng an toàn (`reduceMotion`), thao tác đơn `[E]`/`[K]`, reset đúng phân đoạn, Skip giữ kết quả `6/6` + transcript, và cắt sạch trước cảnh `03:09–03:12`. |
| `GAP-M33-09` | `HospitalAuthoring.Art.cs` & `PZ07` UI/Inspector | Chưa có texture Sơ đồ niêm yết 2005 & Bản hoàn công 2004 (`E05`), chưa có giao diện căn chồng 2 mốc kiến trúc `PZ07` và prop van `LOC09` (`DEC-V32-03`). | Sinh 2 texture bản vẽ kỹ thuật tiếng Việt chuẩn năm 2004/2005, tích hợp `PhysicalInspector` + bảng căn 2 mốc kiến trúc bằng phím/click (snap, báo lỗi sai mốc, gợi ý sau 2 lần sai, không kéo giữ), prop van cắt khóa + tấm chắn gió tùy chọn (`CH05-D08`), và khóa chặt `H03/G05` cho tới khi kiểm tra `E05b` ngoài hiện trường `R`. |
| `GAP-M33-10` | `HunterOrderlyAI.cs` (`TASK013-C`) | `HunterOrderlyAI` mới kích hoạt ở `CH03` và `CH04`. | Mở rộng `HunterOrderlyAI` cho `CH05` ở `R` (tuần tra B1 ngoài vùng an toàn & lượt né ngắn tại `LOC07` sau khi mở `G05`), đảm bảo spawn khuất tầm nhìn `≥ 3.2 m`, không camp checkpoint, hỗ trợ radio đánh lạc hướng, đường vòng phòng họp và `Story Mode`. |

---

## 7. Trạng thái Đo Chuẩn Hiệu năng `c90a2eb` (Phase A)

1. **Dữ liệu chuẩn 60 Hz 100% Foreground đã khóa của `c90a2eb`**:
   - Lưu tại `docs/m3/evidence/m32-c90a2eb/presentmon-60hz-summary.json` và thư mục gốc `output/m3.2/bench-c90a2eb-60hz-20261125-202119` (`3` lượt độc lập, 1080p Medium, `render_scale = 0.88`, `60.000 Hz`, `VSync = 1`, `100%` foreground):
     - Unity `GPU P50 = 6.26 ms`, **`GPU P95 = 11.49 ms`** (`11.45–11.53 ms`), `Main CPU P95 = 10.67 ms`, `Render CPU P95 = 3.09 ms`, `Wall P95 = 16.95 ms`.
     - PresentMon `GPUTime P95 = 8.43 ms`, `DisplayedTime P95 = 16.91 ms`, `FrameTime P95 = 16.88 ms`.
     - Working Set Peak `= 986.55 MiB`, Unity Allocated Peak `= 465.46 MiB`, Active Mips `= 267.97 MiB`.
2. **Kiểm tra đo trực tiếp từ phiên công cụ hiện tại**:
   - Đã chạy thử `output/m3.2/m32-release-c90a2eb/run-m32-presentmon-60hz.ps1` trực tiếp trên binary `c90a2eb` (không build lại).
   - Môi trường tiến trình con của agent trả về `ForegroundHwnd = 0` và `ChangeDisplaySettingsEx(60 Hz) = -1` (màn hình vật lý đang ở `120 Hz` và Windows chặn tiến trình không giữ cửa sổ tiền cảnh đổi tần số quét — đúng hiện tượng đã ghi nhận ở M3.2).
   - Do tuân thủ nghiêm ngặt quy tắc không can thiệp trái phép ranh giới môi trường Windows, trong Phase G chúng tôi sẽ:
     1. Chạy `3` lượt độc lập bằng `tools/measure_m2.ps1` trên cùng cấu hình máy (`1080p Medium`, `render_scale = 0.88`, `VSync = 1`) để so sánh trực tiếp 1-đối-1 Unity `GPU/Main/Render ms`, PresentMon `GPUTime/FrameTime`, `RAM/VRAM` và các phân đoạn riêng `AN05` (với NPC), `PZ07`, `B1` đối với `output/m3.2/bench-c90a2eb` và `bench-c90a2eb-60hz-20261125-202119`.
     2. Cung cấp sẵn script `run-m33-presentmon-60hz.ps1` đóng gói cùng bản phát hành để chạy xác nhận 60 Hz 100% foreground trực tiếp trên Desktop người dùng nếu cần.

---

## 8. Kế hoạch Kiểm thử & Nghiệm thu Kỹ thuật M3.3 (Test Plan)

### 8.1 Bộ ca kiểm thử chuyên biệt CH05 (`CH05-Q01–Q05`)
- **`CH05-Q01` (Bất biến 6 người được cứu):** Chạy nhanh, chạy chậm (kèm reset từng phân đoạn khi thao tác lỗi), và nhấn `Skip` ở đầu/giữa mỗi nhóm đều kết thúc với đúng `6/6` bệnh nhân được cứu, không có timer tử vong, không có nhánh bỏ mặc bệnh nhân.
- **`CH05-Q02` (Save/load giữa nhóm & thoát `M`):** Lưu/tải sau Nhóm 1, sau Nhóm 2 và thoát về `R` khôi phục đúng phân đoạn tiếp theo, không mất vật chứng `R` đã có (`E10a`, `E12`, `E11`, `E03`, `E02`, `E09`, `E06a`, `E04`, `E07`), không nhân đôi item và không xuất bất kỳ đạo cụ `M` nào sang `R`.
- **`CH05-Q03` (Ràng buộc kép `PZ07` + `E05b` cho `H03/G05`):** Căn khớp hoàn toàn bản vẽ `PZ07` (`E05`) khi chưa ra hố quan sát `LOC11` soi `E05b` (và ngược lại: soi `E05b` khi chưa căn `PZ07`) đều giữ `H03 = false`, `G05 = false`, `LOC08` đóng, kèm chỉ dẫn rõ ràng bước còn thiếu.
- **`CH05-Q04` (Trợ năng & điều khiển thay thế):** Hoàn thành trọn vẹn `AN05` và `PZ07`/`E05b` khi bật đồng thời `volume = 0` (Mute), `reduceMotion = true` (giảm khói/chuyển động), `textScale = 1.30` (Large Print 130%), `storyMode = true`, `alternateBindings = true` (`Mũi tên / K / J / L`) và thuần bàn phím (không kéo giữ chuột).
- **`CH05-Q05` (Biên nhận thức & đạo đức trần thuật):** Kiểm tự động toàn bộ chuỗi thoại, UI, Journal và Pocket Note của `CH05`: không câu nào hàm ý người chơi được chọn “cứu 6 hay cứu 12”, không tiết lộ sớm cảnh `03:09–03:12` / `~03:25` / `07:40`, `CH05-D07` được đóng khung rõ là loa phát của Vĩnh trong `M`, và van `LOC09` (`CH05-D08`) chỉ dừng ở nghi vấn có cơ sở khi đối chiếu `E09`.

### 8.2 Kiểm thử Đối kháng (Adversarial QA)
1. **Lỗi ghi đĩa & Atomic Rollback:** Khóa file `.lock` giả lập lỗi I/O ngay tại 3 thời điểm nhạy cảm: bước vào `AN05` (`CH05-T01`), hoàn tất từng nhóm sơ tán (`Pair1/Pair2/T05`), và xác minh `H03/G05` — đảm bảo rollback nguyên tử 100%, thử lại thành công ngay khi mở khóa.
2. **Tương thích Save cũ (`v1–v7` & M3.2 `CH04`):** Nạp trực tiếp save `v1..v6` và save `CH04` chuẩn M3.2 tại `CH04-CP04` / `CH04-AN05.reached`, đi tiếp vào `AN05` sang `CH05` mượt mà trên cùng Schema v7.
3. **Additive Unload/Reload giữa CH05:** Unload rồi reload `LOC05_ArchiveWing` và `LOC09_ServiceWing` sau khi thu `E05`/`E05b` và mở `G05` — đảm bảo `0` vật chứng trùng, cửa `LOC08` giữ trạng thái mở, cửa xả `S1` ở `R` giữ vật cản sắt, AI dọn sạch không leak tham chiếu.
4. **Ranh giới khóa CH06+:** Xác minh `!State.Has("E01")`, `!State.Has("E06")`, `!State.Has("E10b")`, `!State.Has("E08")`, `!State.Happened("G06.opened")`, tường gạch `x = 34.5` và cửa `G06` tại `x = 14.5` chặn tuyệt đối cả CapsuleCast lẫn NavMesh.

### 8.3 Hồi quy Toàn diện CH01–CH05 & Failure Probes
- Chạy đầy đủ trên bản build Windows x64 non-development chính thức:
  1. **Module QA Suite (`-ctcc-qa`):** Toàn bộ kiểm thử `CH01–CH05`, `F101` 3 nhánh `A/B/C`, `PZ04` Route A + Route B, `PZ05` 16-mask, `PZ06/H05/D16b`, Regional AI `TASK011`, `S1` hai chiều pre-G02 & `M` discharge, Campus World `W-Q02/W-Q03/WORLD-Q04`, Save Schema `v1–v7`.
  2. **Targeted S1 Input QA & Full CH01–CH05 Player Input QA (`-ctcc-input-qa`):** Điều khiển bàn phím/UI thực tế xuyên suốt `CH01 → CH02 → CH03 → CH04 → CH05` đến tận `CH05-CP03` (không teleport gian lận trên normal route, không tắt collider), kèm ghi hình video gameplay thực tế `.mp4`.
  3. **4× D3D12 Negative Failure Probes:** `module-failure`, `escape-premature`, `invalid-reasoning`, `broken-progression` đều phải trả mã thoát `!= 0` đúng hợp đồng.
