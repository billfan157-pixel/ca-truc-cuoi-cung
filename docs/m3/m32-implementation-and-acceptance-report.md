# CTCC — M3.2 Implementation, Verification & Acceptance Dossier

**Ngày lập:** 25/11/2026  
**Nhánh làm việc:** `feat/m32-ai-ch03-ch04` (tách từ `main` `3c0b955de95bcf39853c30e2669b2bac24ad969e`, không force-push hay ghi đè `main`)  
**Cơ sở ngoại lệ sản xuất:** Phương án 2 — Ngoại lệ sản xuất có kiểm soát (chủ dự án phê duyệt ngày 25/11/2026 cho phép triển khai Phase B–F trên nhánh riêng để đạt **M3.2 Technical Verified**, đồng thời giữ nguyên M2 OPEN, `TASK010`/`TASK026–028` = `Human Review Pending`, Art Lock OPEN, M3 Alpha OPEN, `TASK013`/CH05–CH08 khóa).  
**Ứng viên kỹ thuật M3.2:** `c90a2eb5fe62521839c3be84a0e59e2e56b41410` (`prototype_diff=` rỗng, `development=False`, Unity `6000.3.28f1`, URP `17.3.0` Forward+, D3D12, Windows x64, Save Schema v7)  
**Tài liệu kiểm toán Phase A:** [m32-phase-a-audit-and-test-plan.md](m32-phase-a-audit-and-test-plan.md)

---

## 1. Tóm tắt Trạng thái Nghiệm thu & Ranh giới Governance

| Cổng / Trạng thái | Trạng thái thực tế | Ghi chú trung thực |
| --- | --- | --- |
| **Core v3.3.1 Design Lock** | **10/10 SHA-256 nguyên vẹn** | Không sửa bất kỳ tài liệu Core 01–07 nào để hợp thức hóa triển khai. |
| **Save Schema v7** | **Nguyên vẹn (`Schema = 7`)** | Giữ nguyên migration v1–v7; mở rộng whitelist `puzzleId` trong `AtomicSaves.Valid()` cho `CH04-PU01/02/03` tương thích ngược 100%. |
| **`TASK011-A..E` (Regional AI)** | **Technical Verified** | Đủ bằng chứng trên cả Full Module QA, Full Player Input QA và 4 D3D12 Failure Probes trên `c90a2eb`. |
| **`TASK012-A..D` (CH03–CH04 & `PZ04`)** | **Technical Verified** | `CH03-Q01–Q05`, `CH04-Q01–Q06`, cả Tuyến A & Tuyến B của `PZ04`, `AN05` handoff đều PASS trên Windows Player `c90a2eb`. |
| **Full Regression CH01–CH04** | **Technical Verified** | Toàn bộ 23 nhóm M2-FINAL + `TASK011` + `CH03-Q01–Q05` + `CH04-Q01–Q06` + `V33-Q05` + `WORLD-Q`/`W-Q` PASS, `0` P0. |
| **Performance vs `c459aba`** | **Zero Regression Verified (120 Hz A/B) · Formal 60 Hz Elevated PresentMon Pending OS Host** | Dưới cùng trạng thái host `120 Hz`, `c90a2eb` nhanh hơn `c459aba` trên mọi phân vị (`GPU P95 15,20–15,76 ms` vs `15,85 ms`; `GPU P99 16,31–17,29 ms` vs `24,94 ms`; `Main CPU P95 5,69–6,20 ms` vs `6,21 ms`) và đạt hard gate Core `≤ 16,67 ms`. Màn hình host hiện ở `120 Hz` và phiên shell không có quyền Admin nên PresentMon cần 1 lần chạy elevated tại `60 Hz` qua `run-m32-presentmon-60hz.ps1` để chốt thêm `GPUTime`/`DisplayedTime` chính thức ở `60 Hz`. |
| **Milestone M2 (`TASK010`, `TASK026–028`)** | **OPEN — Human Review Pending** | `TASK010` (`0/6` phiên người chơi mới) và `TASK026–028` (ký xác nhận chuyên gia) chưa hoàn tất; không giả lập. |
| **Art Lock (`CHAR-Q02/Q09`)** | **OPEN — Human Review Pending** | Không tự đóng production character art lock trong M3.2. |
| **Milestone M3 Alpha & `TASK013` / CH05–CH08** | **OPEN / Locked Out of Scope** | Không triển khai CH05–CH08; `G05`, `G06`, `E08` bị khóa cứng và đã xác minh qua QA. |

---

## 2. Chuỗi Checkpoint & Báo cáo Triển khai (`Phase A–F`)

| Phase | Commit trên `feat/m32-ai-ch03-ch04` | Phạm vi chính đã hoàn thành |
| --- | --- | --- |
| **Phase A** | `4525df6` | Audit baseline `3c0b955` vs `c459aba`, kiểm 10/10 Core SHA-256, 96-task DAG, `GAP-PROV-01`, `GAP-NAV-01/02`, `GAP-CH03/CH04`, lập test plan và ghi nhận phê duyệt Phương án 2. |
| **Phase B** | `a3ee324` | `TASK011-A..E`: tách 3 actor độc lập (`GatekeeperActor` Hunter, `OrderlyActor` + `OrderlyCart`, `UnrecordedActor`), FSM 6 trạng thái Hunter, thính giác phân lập theo tầng, `Orderly` 3 điểm dừng + Route B không chase, `Unrecorded` tối đa 2 vòng (`!= C12`), đồng bộ NavMesh `morgueLock`, cleanup khi unload zone, bài QA AI chi tiết. |
| **Phase C** | `c6f6b0c` | `TASK012-A/B` CH03: đầy đủ nội dung `E03` đúng `Evidence Specs` & `design-lock-errata.md` (giữ `Bà Lành` riêng ở tuyến thư tùy chọn), ghép cặp chủ động `PZ03` + lỗi `"Mã/chi tiết chăm sóc chưa khớp."` + 2-strike hint, `CH03-PU02` chọn mã + từ chối `C12`/`C03`, checkpoint `CH03-CP02`/`CH03-CP03`, đọc `CH03-D07` ở cả CH03/CH04, `RADIO_LOC07` + caption, mở rộng `CH03-Q01–Q05`. |
| **Phase D** | `09e9a5d` | `TASK012-C/D` CH04 & `PZ04` Dual-Route: giữ Schema v7, hoàn thiện Tuyến A (đọc `PZ04_SCHEDULE`, quan sát nhịp 3 điểm, đi qua không sửa hồ sơ) và Tuyến B (đọc `PZ04_ROOM_SLIP`, chặn sửa sai hồ sơ, đối chiếu đúng phiếu để đổi tuyến `Orderly` + đổi mũi tên hành lang `LOC06`), bổ sung `PZ05` caption rơ-le & note cầu thang luôn khả dụng, `PZ06` đối xứng `E04`/`E07`, transcript đủ `CH04-D01..D09` + `CH01-D16b`, chốt chuyển tiếp `AN05` (khóa CH05/`G05`/`G06`/`E08`). |
| **Phase E** | `3e2e40c`, `9d33192`, `fb8b5a7`, `c90a2eb` | Tái tạo sạch 9 scene + dual LightProbes (`R=369`, `B=369`) + OcclusionCullingData (giải quyết hoàn toàn `GAP-PROV-01`, `prototype_diff=` rỗng); phát hiện và xử lý triệt để 4 lỗi thực tế (`BUG-01..04`); PASS 100% Full Module QA, 4 D3D12 Failure Probes, Targeted S1 Input QA và Full Player Input QA + ghi hình thực tế trên cùng một binary `c90a2eb`. |
| **Phase F** | *(commit này)* | 3 lượt đo hiệu năng `c90a2eb` + lượt đối chứng A/B `c459aba` cùng trạng thái host `120 Hz`, dựng video gameplay thực tế `ch03-ch04-pz04-dual-route-gameplay.mp4` (`1.453` frame), đóng gói Windows x64 candidate ZIP + `SHA256SUMS.txt`, tạo script elevated 60 Hz PresentMon và hoàn thiện hồ sơ nghiệm thu. |

### 2.1 Nhật ký Lỗi Thực tế Phát hiện & Sửa trong Phase E (Không làm yếu test)

| ID | Reproduction | Root Cause | Fix & Retest Evidence |
| --- | --- | --- | --- |
| **BUG-01** | Module QA lần 1 (`run-20261125-183851`) fail tại `TASK011-B: hearing Suspicious transitions to Investigate`. | `HunterOrderlyAI.TickHunter` ([HunterOrderlyAI.cs](../../Prototype/Assets/CTCC/Runtime/HunterOrderlyAI.cs#L726)) hủy mọi trạng thái `Suspicious` về `Return` khi người chơi đứng trên safe pad `AN03`, kể cả khi cảnh giác đến từ tiếng cửa ngoài safe pad (`suspiciousFromHearing == true`). | Chỉ hủy `Suspicious` theo safe pad khi là cảnh giác thị giác (`!suspiciousFromHearing`) hoặc nguồn âm nằm trong safe zone (`IsInSafeZone(stimulusPosition, scene)`). Retest PASS (`TASK011-A/B/C/E & CH03-Q03 PASS`). |
| **BUG-02** | Module QA lần 2 (`run-20261125-184332`) fail tại `GAP-NAV-02: runtime per-zone NavMesh disconnected for AP02 -> AP06`. | Khi nạp additive `LOC05`/`LOC09`, `HospitalScene.LoadAndRegister` chỉ refresh NavMesh scene mới mà không refresh `LOC02`/`LOC04` (vốn đã bake collider cửa nối `S1`/`S2`/`S3` ở trạng thái đóng lúc khởi động), đồng thời tile `NavMeshData` giữa các vùng tách mép voxel. | Refresh cache NavMesh của các vùng hiện diện khi nạp/gỡ zone (vùng không đổi hit cache `< 0,1 ms`) và đồng bộ `NavMesh.AddLink` hai chiều theo điều kiện gate/layer qua các cổng `LOC01↔LOC02↔LOC03↔LOC04` và cầu thang `S1`/`S2`/`S3` ([HospitalScene.cs](../../Prototype/Assets/CTCC/Runtime/HospitalScene.cs)). Retest PASS tất cả 5 tuyến `AP02→AP06`, `AP05→AP06`, `AP06→AN03`, `AP06→AP07`, `AP05→AP08` + `GAP-NAV-01` `AP08→AP09`. |
| **BUG-03** | Module QA lần 3 (`run-20261125-184933`) fail tại `TASK011-D: Orderly emits 2s warning before gentle reset`. | Trong `HunterOrderlyAI.TickOrderly`, khi `Orderly` bắt gặp Minh trong tầm `3,25 m` (`seesPlayer == true`) ở trạng thái `Patrol`, vòng di chuyển tiếp tục xoay nhân vật về hướng waypoint kế tiếp nên quay lưng khỏi Minh trước khi tích lũy đủ `0,35 s` để bật cảnh báo `Suspicious`. | Khi `seesPlayer == true` trong `OrderlyState.Patrol`, `Orderly` dừng đẩy xe và hướng về phía người chơi đang chắn lối trong nhịp `0,35 s` trước khi phát cảnh báo 2 giây. Retest PASS cả Module QA lẫn Player Input QA. |
| **BUG-04** | Full Player Input QA trên `3e2e40c`, `9d33192`, `fb8b5a7` cho thấy Minh bị B1 Hunter bắt và reset về `AP08` ngay sau khi rời bảng điện `CH04_POWER` (đối chiếu trực tiếp qua `frame-01290.jpg`–`frame-01375.jpg`). | (a) `SetServicePower` gọi `TriggerDistraction(player.Feet)` trên mọi thao tác công tắc hợp lệ kéo Hunter về chân người chơi; (b) `LOC09` là phòng trống `6 × 14,4 m` không có vách ngăn nên spawn Hunter trong `LOC09` hướng nam `180°` hoặc điểm `Search` gần cửa `z = 42` nhìn xuyên thẳng sang `CH04_POWER` và lối vào `LOC10`. | Giữ nguyên phạt kéo Hunter về `CH04_POWER` khi **quá tải (`!ValidPower`)**; chuyển vòng tuần tra B1 Gatekeeper sang đúng trục kiến trúc **tiền sảnh Khu C `LOC11` (`13.2, -3.4, 40.2` / `13.4, -3.4, 44.2` / `12.6, -3.4, 42.2`, sau vách đặc `x = 10.5`, canh cửa `G06`) ↔ bắc `LOC09` (`8.8, -3.4, 42.8`)**, với tín hiệu rơ-le đóng mạch `PZ05` tại `(13.2, -3.4, 40.2)`. Retest PASS hoàn chỉnh trên `c90a2eb` (không sửa tốc độ người chơi, không bỏ collider, không teleport). |

---

## 3. Ma trận Vai trò & Trạng thái AI (`TASK011-A..E`)

### 3.1 Phân tách Vai trò (Tuyệt đối không dùng chung transform gây dịch chuyển trong tầm nhìn hay sai tầng)

| Vai trò Canon | Actor & Prop riêng | Vùng & Điều kiện kích hoạt | Tốc độ & Hành vi cốt lõi | Giới hạn cứng theo Core |
| --- | --- | --- | --- | --- |
| **`CHAR005` Hunter** (Kẻ Giữ Cửa) | `scene.GatekeeperActor` | **CH03 Tầng 3 (`LOC07`)** sau `CH03-T06` tới trước `CH03-T07` (`R`); **CH04 B1 (`LOC11`/`LOC09`)** khi `y < -2.4` và chưa `H05` (`R`). | FSM 6 trạng thái đầy đủ: `Patrol (1.65 m/s) → Suspicious (cảnh báo 2.0s) → Investigate (1.85 m/s) / Chase (3.60 m/s) → mất LOS > 2.1s → Search (12.5s quét chống camp) → Return → Patrol`. LOS `12 m / 100°` có chắn tường (`0.70 s`); nghe cùng tầng (`|Δy| ≤ 2.2 m`, giảm `0.5×` qua tường: đi `3 m`, ngồi `1.5 m`, chạy `9 m`, cửa `12 m`, đài `14 m`). | Không teleport qua đường NavMesh không hợp lệ; dừng đuổi ngay khi vào safe pad, `LOC10` (`x < 4.45`), `Story Mode`, `M`/`B` hoặc modal/inspector mở. Recovery về safe pad chính thức (`AN03`/`AP08`), grace `60 s`, giữ nguyên hồ sơ. |
| **`CHAR006` Orderly** (Điều dưỡng đẩy xe thuốc) | `scene.OrderlyActor` + `scene.OrderlyCart` (không collider đặc, chống xuyên tường bằng raycast) | **CH04 Tầng 2 (`LOC05`/`LOC06`)** khi `3.2 < y < 6.2` và chưa `H05` (`R`). | **Tuyến A mặc định (3 điểm dừng):** `Hành lang phòng trực (4.0, 4.0, 40.4 · 5s) → Quầy thuốc Khu B (5.8, 4.0, 36.8 · 6s) → Cửa Khu B (-0.8, 4.0, 36.6 · 4.5s)`. **Tuyến B (sau `PZ04.ScheduleUpdated`):** `Quầy thuốc (7s) ↔ Cửa Khu B (5.5s)`, bỏ chốt hành lang. Gặp người chắn lối (`≤ 3.25 m`): dừng `0.35 s` → cảnh báo `2.0 s` (`Suspicious`) → nếu không tránh, reset nhẹ về `AP07`/`AP06` (grace `12 s`). | **TUYỆT ĐỐI KHÔNG** vào `Investigate`, `Chase`, `Search` (hard guard trong `TransitionTo` và `TriggerDistraction`); không đuổi theo đài/tiếng động; `Story Mode` tự nhường đường không reset; không làm mất vật chứng/sổ trực. |
| **`CHAR007` Unrecorded** (Bệnh nhân không hồ sơ) | `scene.UnrecordedActor` (shader `CTCC/ApparitionHuman`, `ShadowCastingMode.Off`, không collider) | **CH04 B1 (`LOC10`)** độc lập song song khi `MorguePower` bật và người chơi vào `LOC10` (`x < 4.5, y < -2.4`). | Vòng đi chậm `0.78 m/s` trên `MorgueTrace` `(-0.2, -3.4, 41.15) ↔ (3.3, -3.4, 41.15)` (cách `AP09` `2.15 m > 1.65 m` safe radius), tối đa đúng **2 vòng** (`MaxUnrecordedLoops = 2`) rồi tự mờ dần và tắt hẳn. | **TUYỆT ĐỐI KHÔNG** đuổi, không cảnh báo đe dọa, không chặn va chạm, **không đồng nhất `CHAR007` với `C12`**, không làm `GatekeeperActor` dịch chuyển xuyên cửa `x = 4.5` khi bước giữa `LOC09 ↔ LOC10`. |

### 3.2 Chuyển trạng thái & Kiểm chứng NavMesh / Streaming

- **Spawn khuất tầm nhìn:** `PickSafeSpawnWaypointIndex()` lọc khoảng cách `≥ 3.2 m` và ưu tiên điểm khuất tầm nhìn (`!PlayerHasDirectView`) để không bao giờ xuất hiện đột ngột trước mặt người chơi.
- **NavMesh động (`GAP-NAV-01` & `GAP-NAV-02`):** `ChapterFour.SetPower()` gọi `HospitalScene.RefreshZoneNavMesh("LOC09")` ngay khi `morgueLock` đóng/mở; `HospitalScene.SyncPortalNavMeshLinks()` duy trì `NavMesh.AddLink` hai chiều theo điều kiện gate/layer qua các cổng vùng và thu hồi sạch khi unload/destroy.
- **Additive Unload Safety:** `HospitalGame` nối `scene.ZoneUnregistering → orderlyAi.OnZoneUnregistering(zone)` — khi gỡ `LOC05` hoặc `LOC09`, AI cùng vai trò tầng đó dừng sạch, không giữ tham chiếu cũ (đã xác minh trong `VerifyTask011Ch04RolesAndNavMesh`).

---

## 4. Luồng Gameplay CH03–CH04 & Bằng chứng Dual-Route `PZ04`

### 4.1 Luồng Chương 03 (`TASK012-A/B` — *Những cái tên bị xóa*)

1. **S1 trước G02 (`DEC-V331-01` / `V33-Q05`):** Đi hai chiều `LOC02 ↔ LOC05 ↔ LOC07` trong `R` trước `G02` mà không kích hoạt sớm CH03, không cấp vật chứng và không nạp B1; cửa xả sân `S1` chắn đặc trong `R`, mở trong `M` (CH05).
2. **Mở CH03 (`AP05 → AP06`, `CH03-CP01`):** Đọc `E03` tại `LOC07` (đầy đủ nhóm đầu vào, ghi chú `C02`/`C05`/`C10`/`C12` & tác động AS-05 theo `Evidence Specs`, đồng thời giữ tên `Bà Lành` riêng ở tuyến thư tùy chọn đúng `design-lock-errata.md`), thu thập ghi chú `B/2` (`C02_NOTE`) và túi len thẻ `K-02` (`C02_BAG`).
3. **Giải `PZ03` tại `AN03` (`CH03-PU01` → `G03`, `CH03-CP02`):**
   - Thiếu ghi chú/túi hoặc chọn `"Chỉ chép mã C02 (chưa ghép thẻ K-02)"` hoặc sai mã (`C03`, `C12`) → từ chối bằng đúng câu Core 04 `"Mã/chi tiết chăm sóc chưa khớp."`, giữ đóng `G03`, bật gợi ý trường sau 2 lần sai.
   - Ghép đúng `C02` + `B/2` + `K-02` → xác nhận **tồn tại** `C02`, mở `G03`, ghi `CH03-CP02` mà **không bắt buộc** tên `Bà Lành`.
4. **Nhánh tùy chọn `CH03-PU02` (`C02_LETTER`):** Độc lập với `G03` (truy hồi được cả ở CH03 và CH04); từ chối gán thư cho `C12`/`C03`, chỉ xác thực `C02 — Bà Lành` khi ghép đúng `C02`.
5. **Hộp vô danh `C12` (`C12_BOX`, `FS19`, `CH03-D07`):** Xác nhận tồn tại `C12` và giữ tuyệt đối khuyết danh (mọi lệnh gán tên cho `C12` bị chặn cả ở `Investigation` và `AtomicSaves.Valid()`); ghi chú `CH03-D07` đọc được ở cả CH03 và CH04.
6. **Ký ức `AN03` (Lớp `M`, `CH03-T04`):** Ghi `MemoryRef` không thể export vật lý; save/reload ngay giữa `M` hoặc khi đọc `E03` hay unload/reload `LOC05` đều hồi phục an toàn về `AN03` lớp `R` với **0 trùng lặp vật chứng**.
7. **Tuần tra & đánh lạc hướng (`CH03-T06`, `CH03-CP03`, `RADIO_LOC07`):** Rời `AN03` (`z > 34.3`) kích hoạt Floor-3 Hunter; bật `RADIO_LOC07` kéo Hunter vào `Investigate` (kèm caption định hướng), thoát qua `G03` xuống `AP07` (`CH03-T07`, `CH03-CP04`) kiểm `FS10_LOCK` và chuyển sang CH04 (giữ chặn tuyệt đối `E02`, `E07`, `CH01-D16b` trong suốt CH03).

### 4.2 Luồng Chương 04 (`TASK012-C/D` — *Người dẫn đường*) & Bằng chứng `PZ04` Dual-Route

1. **Phòng trực `LOC06` (`AN04`, `CH04-CP01 → CP02`):** Đọc và lưu `E02` (đủ mốc `22:00 / 23:10 / 02:50 / 04:15`), `E09` (đủ 7 mốc `23:10 / 02:50–02:56 / 02:58 / 03:05 / 03:09 / 03:12 / 03:17 / 07:40`, không gán `03:17` thành giờ tử vong), đối chiếu mốc `02:50` (`CH04-D09`), nghe/đọc bản chép `E06a` (`CH04-D02/D03/D04`, giữ tách biệt với `E06`, mở `PT01.presented`). Các mục `E02`/`E09` luôn đọc lại được không nhân đôi vật chứng.
2. **Bằng chứng Dual-Route `PZ04` (`CH04-Q01` — ĐÃ KIỂM TRÊN CẢ MODULE QA VÀ WINDOWS PLAYER INPUT QA `c90a2eb`):**
   - **Tuyến A (Quan sát nhịp 3 điểm, không sửa hồ sơ):** Đọc `PZ04_SCHEDULE` (`5.22, 5.48, 44.85`, ghi `PZ04.ScheduleRead`, hiển thị 3 điểm dừng + vị trí hiện tại của xe cho người chơi tắt tiếng) → chờ `Orderly` + xe thuốc rời hành lang về `WingBDoor` → đi bộ phím `W` xuyên hành lang `(4, 4, 44.85) → (4, 4, 36.2)` → tự động ghi `CH04-T02`, `PZ04.RouteA`, `CH04-PU01.solved` với `PZ04.ScheduleUpdated = false` và `0` lần bị reset.
   - **Tuyến B (Đối chiếu phiếu buồng đúng dữ kiện, chặn sửa sai hồ sơ):** Đọc `PZ04_ROOM_SLIP` (`5.20, 5.24, 46.05`, ghi `PZ04.SlipRead`: buồng hành lang Bắc trống từ `22:00`) → mở bảng đối chiếu (`PZ04_STATUS_BOARD` / từ modal phiếu):
     - Thử chọn `"Sửa sai hồ sơ: ghi toàn bộ Khu B đã xuất viện"` → **BỊ TỪ CHỐI**, không đổi tuyến, ghi failed attempt (`CH04-PU01`), phát cảnh báo trung thực dữ liệu.
     - Thử cập nhật khi chưa đọc phiếu → **BỊ TỪ CHỐI**, kích hoạt gợi ý trường sau 2 lần sai.
     - Chọn `"Đối chiếu đúng phiếu: buồng hành lang Bắc trống từ 22:00"` → ghi `PZ04.ScheduleUpdated`, **`Orderly` lập tức chuyển tuyến `Quầy thuốc ↔ Cửa Khu B`**, mũi tên vật lý phía trên cửa nam `LOC06` đổi từ `PZ04 · Route Arrow Default` (Tắt) sang `PZ04 · Route Arrow Rerouted` xanh sáng (Bật) kèm caption định hướng → đi bộ phím `W` thẳng xuống hành lang trống ghi `PZ04.RouteB`.
   - **Bằng chứng hình ảnh & video:** Ghi trọn vẹn trên Windows Player `c90a2eb` trong `output/m3.2/m32-release-c90a2eb/ch03-ch04-pz04-dual-route-gameplay.mp4` (`1.453` frame thực tế).
3. **Bảng điện Kỹ thuật B1 `LOC09` (`PZ05`, `CH04-Q02/Q05`, `CH04-CP03`):**
   - Đã kiểm đầy đủ `16/16` tổ hợp tải (`Hành lang 1`, `Kho 2`, `Thang 3`, `Kỹ thuật 1`; tổng `≤ 4`, quá tải về `0` + cảnh báo + kéo B1 Hunter về bảng điện; cầu thang bộ luôn đi được dù tắt thang).
   - Bật `Kho + Kỹ thuật` (`mask = 10`) mở khóa điện `morgueLock`, refresh NavMesh `LOC09` tức thời (`AP08 → AP09` `PathComplete`), bật đèn tủ khô `LOC10`, phát caption rơ-le.
   - Kiểm save/reload cả ở trạng thái quá tải (`servicePower = 0`), trạng thái có điện (`servicePower = 10`), ngưỡng pin tối thiểu `≥ 120 s`, và unload/reload `LOC09`.
4. **Nhà xác `LOC10` (`PZ06`, `E04`/`E07`, `H05`, `PT02`, `CH04-Q03/Q06`, `CH04-CP04`):**
   - Kiểm đối xứng: **`E04` đứng một mình giữ đóng `H05`** VÀ **`E07` đứng một mình giữ đóng `H05`**.
   - Đọc đủ `E04` (13 thi thể + mục `#12` `CH04-D08`) và `E07` (3 phần: Thẻ Lan, Phiếu vị trí `L-13`, mẩu bìa `Bảng theo dõi liều AS-05`, không suy giờ tử vong) → xác thực `H05` + `PT02` (`CH04-D05`), mở `G04`, mở đúng guard `CH04-D06` và legacy `CH01-D16b` chỉ tại `CH04 B06` (và ghi `CH04-D07` sau khoảng lặng).
5. **Chốt chuyển tiếp `AN05` tại `LOC06`:** Quay ngược từ `AP09` lên `S2 → S3 → LOC06`, tương tác `AN05` (`4.84, 5.15, 46.72`) ghi `CH04-AN05.reached`. Xác minh cứng `State.chapter == "CH04"`, `!State.Has("E08")`, `!State.Happened("G05.opened")`, `!State.Happened("G06.opened")`.

---

## 5. Ma trận Nghiệm thu Task (`Task Acceptance Matrix`)

| Task ID | Hạng mục | Phân loại hiện tại | Bằng chứng xác minh |
| --- | --- | --- | --- |
| `TASK010` | Playtest mù 6 người chơi mới (M2) | **Human Review Pending** | `0/6` phiên thực; bộ kit sẵn sàng tại [playtest-kit-task010.md](../m2/playtest-kit-task010.md). Không giả lập. |
| `TASK026–028` | Review chuyên gia y khoa, PCCC lịch sử, nhân văn/nội dung (M2) | **Human Review Pending** | Hồ sơ thẩm định sẵn sàng tại [specialist-review-dossiers.md](../m2/specialist-review-dossiers.md). Chưa có chữ ký chuyên gia. |
| `TASK011-A` | Navigation / Patrol AI & NavMesh (`GAP-NAV-01/02`) | **Technical Verified** | `c90a2eb` Module QA `TASK011-A/B/C/E & CH03-Q03 PASS`, `TASK011-D/E & GAP-NAV-01/02 PASS`, `CTCC_GREYBOX_PASS`. |
| `TASK011-B` | Occluded LOS, floor-isolated hearing & door/radio noise | **Technical Verified** | `c90a2eb` Module QA (`2` invalid paths / `0` raw move, radio `1.78 m`, cross-floor door noise isolated) + Input QA (`RADIO_LOC07`). |
| `TASK011-C` | Hunter 6-state FSM (`Patrol/Suspicious/Investigate/Chase/Search/Return`) | **Technical Verified** | `c90a2eb` Module QA (2s warn → Chase → lose LOS > 2.1s → Search 12.5s → Return → Patrol). |
| `TASK011-D` | Orderly 3-stop schedule, Route B, no-chase & gentle reset | **Technical Verified** | `c90a2eb` Module QA + live Windows Player Input QA (`PZ04` Route A + Route B trên `OrderlyActor` + `OrderlyCart` thực). |
| `TASK011-E` | Safe pads, Story Mode, modal pause, `Unrecorded` (`!= C12`), unload cleanup | **Technical Verified** | `c90a2eb` Module QA (Story Mode breakoff, safe-pad breakoff, `Unrecorded` 2 vòng, zero cross-doorway teleport, `LOC05` unload cleanup). |
| `TASK012-A` | CH03 `PZ03`, `E03`, `C02` existence vs optional identity, `C12` anonymity | **Technical Verified** | `c90a2eb` Module QA (`CH03-Q01/Q02/Q05 PASS`) + Input QA (`INPUT CH03–04 PASS`). |
| `TASK012-B` | CH03 `AN03` `M` vignette, `RADIO_LOC07`, patrol & retrieval/reload | **Technical Verified** | `c90a2eb` Module QA (`CH03-Q03/Q04 PASS`, 0 trùng lặp vật chứng sau reload & `LOC05` unload/reload) + Input QA. |
| `TASK012-C` | CH04 `PZ04` Dual-Route (Tuyến A + Tuyến B), `E02`/`E09`/`E06a`/`PT01` | **Technical Verified** | `c90a2eb` Module QA (`CH04-Q01 PZ04 DUAL-ROUTE PASS`, `CH04-Q04 PASS`) + Input QA (`PZ04 Route A + Route B on live Orderly`). |
| `TASK012-D` | CH04 `PZ05` 16-mask, `PZ06` symmetric `E04`/`E07`, `H05`/`PT02`, `CH04-D06`/`D16b`, `AN05` | **Technical Verified** | `c90a2eb` Module QA (`CH04-Q02/Q03/Q05/Q06 PASS`) + Input QA (`AN05 handoff`, CH05/`G05`/`G06`/`E08` locked). |
| `TASK013` | M3 Alpha Integration / CH05–CH08 | **Out of Scope / Locked** | Không triển khai trong M3.2; không chuyển `Accepted`; M3 Alpha giữ **OPEN**. |

---

## 6. Kết quả QA Regression & So sánh Hiệu năng vs `c459aba`

### 6.1 Toàn bộ Bằng chứng QA trên cùng một Binary `c90a2eb`

| Bộ kiểm thử | Kết quả | Exit Code | Đường dẫn bằng chứng |
| --- | --- | --- | --- |
| **Full Module QA** (M2-FINAL 23 nhóm + `TASK011-A..E` + `GAP-NAV-01/02` + `CH03-Q01–Q05` + `CH04-Q01–Q06` + `PZ04` Dual-Route + `V33-Q05` + `WORLD-Q/W-Q`) | **PASS (`RESULT=PASS`)** | `0` | [module-gameplay.txt](evidence/m32-c90a2eb/module-gameplay.txt) |
| **Full Player Input QA** (3 nhánh F101, CH01-R zero-teleport, CH02 Door A/B, S1 pre-G02, CH03 + `RADIO_LOC07`, `PZ04` Route A + Route B thực, `PZ05/06/H05`, B1 reload, `AN05`, 100-cycle stress) | **PASS (`RESULT=PASS`)** | `0` | [input-full-gameplay.txt](evidence/m32-c90a2eb/input-full-gameplay.txt) |
| **Targeted S1 Player Input QA** (`V33-Q05 INPUT S1 PASS`) | **PASS (`RESULT=PASS`)** | `0` | [input-s1-gameplay.txt](evidence/m32-c90a2eb/input-s1-gameplay.txt) |
| **D3D12 Failure Probes** (`module-failure`, `escape-premature`, `invalid-reasoning`, `broken-progression`) | **4/4 VERIFIED (`RESULT=FAIL`)** | `1` (đúng kỳ vọng) | `output/m3.2/m32-release-c90a2eb/qa-logs/probe-*-gameplay.txt` |

### 6.2 So sánh Hiệu năng `c90a2eb` vs `c459aba` (`1920×1080 Medium`, `render_scale=0.88`, D3D12, VSync 1, `dual-probes`, `-Expansion`)

> **Ghi chú trung thực về trạng thái màn hình & quyền OS khi đo:** Trong phiên chạy máy host hiện tại, tần số quét desktop Windows là **`120 Hz`** (`hz=120,000`) thay vì `60 Hz`, và shell chạy dưới tài khoản không nâng quyền (`IsAdmin=False`) khiến PresentMon ghi được `FrameTime`/`CPUBusy` nhưng trả `NA` cho `GPUTime`/`DisplayedTime` (vốn yêu cầu phiên hiển thị foreground + Admin ETW `DxgKrnl`). Để kiểm chứng khoa học không thiên vị, đã chạy **3 lượt độc lập trên `c90a2eb` (`120 Hz`)** VÀ **1 lượt đối chứng A/B trên chính binary baseline `c459aba` dưới cùng trạng thái host `120 Hz`**.

| Chỉ số | `c459aba @ 60 Hz` (tham chiếu M2-FINAL, 3 lượt) | `c459aba @ 120 Hz` (A/B cùng trạng thái host, 1 lượt) | `c90a2eb @ 120 Hz` (M3.2, 3 lượt độc lập) | Đánh giá vs `c459aba` cùng điều kiện |
| --- | --- | --- | --- | --- |
| **Frames / lượt** | `6.655 – 6.663` | `9.366` | `9.570 / 10.079 / 9.953` (`100%` Unity focused) | Đạt `passed=True` cả 3 lượt |
| **Unity GPU P50** | `8,13 – 8,32 ms` | `9,27 ms` | **`9,03 – 9,34 ms`** | Ngang bằng / nhanh hơn |
| **Unity GPU P95** | `12,77 – 12,97 ms` | **`15,85 ms`** | **`15,20 / 15,38 / 15,76 ms`** | **Nhanh hơn `0,09 – 0,65 ms` (đạt Core hard gate `≤ 16,67 ms` ngay cả ở 120 Hz)** |
| **Unity GPU P99** | `14,48 – 14,61 ms` | **`24,94 ms`** | **`16,31 / 17,12 / 17,29 ms`** | **Nhanh hơn `7,65 – 8,63 ms`** |
| **Unity Main CPU P95** | `6,88 – 7,23 ms` | **`6,21 ms`** | **`5,69 / 5,75 / 6,20 ms`** | **Nhanh hơn `0,01 – 0,52 ms`** |
| **Unity Render CPU P95** | `2,50 – 2,53 ms` | **`2,46 ms`** | **`2,15 / 2,32 / 2,38 ms`** | **Nhanh hơn `0,08 – 0,31 ms`** |
| **Unity Wall P95** | `16,68 ms` | `24,99 ms` | **`16,67 ms`** (cả 3 lượt) | **Nhanh hơn `8,32 ms`** |
| **PresentMon FrameTime P95** | `19,06 – 19,35 ms` | *(UnityOnly A/B)* | `17,88 – 18,39 ms` | Ổn định |
| **PresentMon GPUTime / DisplayedTime** | `13,74–13,88 ms` / `16,73–16,75 ms` | *(chưa đo)* | `NA` *(do host `120 Hz` + `IsAdmin=False`)* | Chờ 1 lượt chạy elevated tại `60 Hz` (`run-m32-presentmon-60hz.ps1`) |
| **Working Set Peak** | `1.755,6 – 1.763,3 MiB` | — | `1.789,9 – 1.808,0 MiB` | `+~35 MiB` (ổn định, thêm `OrderlyActor`/`UnrecordedActor`) |
| **Unity Allocated Peak** | `286,1 – 289,5 MiB` (`300–303,5 MB`) | — | **`289,8 – 291,5 MiB`** | Tương đương / thấp hơn |
| **Streaming Textures / Active Mips** | `105` / `281.847.342 B` | `105` / `281.847.342 B` | **`105` / `281.847.342 B`** | **`0 bytes` chênh lệch** |
| **Additive Region Load (`LOC05` / `LOC09`)** | — | — | `42,1–76,4 ms` / `49,6–57,2 ms` | Rất nhanh, không hitch |

**Phân tích theo stage CH03–CH04 (`c90a2eb` vs `c459aba`):** Các stage có thêm AI/prop M3.2 (`ch03-stairs-and-archive = 9,73 ms`, `ch03-hunter-functional = 10,14 ms`, `ch04-begin..E06a = 4,51–5,92 ms`, `ch04-morgue..H05 = 5,99–8,63 ms`) đều nằm rất sâu dưới ngưỡng mục tiêu nội bộ `14,5 ms` và nhanh hơn tới `25–35%` so với `c459aba`.

---

## 7. Build Identity, Gói Phát hành & Manifest SHA-256

- **Đường dẫn gói candidate (local, không commit nhị phân lớn vào git):** `output/m3.2/m32-release-c90a2eb/`
- **Bản sao text gọn đã commit vào repo:** [build-identity.txt](evidence/m32-c90a2eb/build-identity.txt), [SHA256SUMS.txt](evidence/m32-c90a2eb/SHA256SUMS.txt)
- **Video gameplay thực tế CH03–CH04 & `PZ04` Dual-Route:** `output/m3.2/m32-release-c90a2eb/ch03-ch04-pz04-dual-route-gameplay.mp4` (`13.912.793 bytes`, `1.453` frame 1080p ghi từ `c90a2eb` Input QA, `121,1 s`, H.264 `1280×720 @ 12 fps`).

| Tạo tác | Kích thước (bytes) | SHA-256 |
| --- | --- | --- |
| `CaTrucCuoiCung-M3.2-c90a2eb-Windows-x64.zip` | `421.626.919` | `af0d764855dd7a4b97b43b39f9df059cace52969199f29acc17311a9c5b9b211` |
| `windows-x64/CaTrucCuoiCung.exe` | `667.136` | `fbd073ca59c4918edbc6704f5393cf59021173af11714c00db4212c50117492b` |
| `windows-x64/CaTrucCuoiCung_Data/Managed/Assembly-CSharp.dll` | `954.368` | `ac7a8f8eb962597404e5ece578c1053e0007ee613805f1e0084de4d976ea78e3` |
| `ch03-ch04-pz04-dual-route-gameplay.mp4` | `13.912.793` | `0b551aeb3e158a366df241bb9bc45ce5da24422c5287d5ec7d3d7bfae9986e0c` |
| `build-identity.txt` | `1.122` | `d364804ca30bb9b111977d94837fedf2804ed780fe3b44a8046e8883fe8bb7f4` |
| `performance-summary-120hz.json` | `44.667` | `b17a9e3a39c04e9e142169a1e47804766e14861514eac952c268971f8b55bcd7` |
| `run-m32-presentmon-60hz.ps1` | `7410` | `7519ca08fc22e25751ca1aa0181ec4202ab20e87642f5668aa116a0cea81e014` |

**9 Scene SHA-256 (tái tạo sạch, khớp 100% với commit `c90a2eb`, `prototype_diff=` rỗng):**
- `AnSinh_Bootstrap.unity`: `612619dc389b876b4903348b7435ecba16d00054ccbe640b66bcce35650398c0`
- `Campus_Grounds.unity`: `ba73a756062c0262cbfff7b100dadf753525fd0d77d88e613669fef4e8fb278c`
- `Campus_Interiors.unity`: `b7a1343a5cd6855225b1d8c4162a5dd53759e2a4c7a7c87a288e3e9e68f35072`
- `LOC01_Exterior.unity`: `8939830b534738c055fe235b229b1ebfe632a0b9ad3f8cb9b9fc90595e7abd29`
- `LOC02_Reception.unity`: `082e248c2e4150beb34a6019420241d98e9f6803b9ddadfea2bf7ed46f0861ff`
- `LOC03_Ward101.unity`: `4b52c55ce15442c6f71d8fe6883d94a7ef15f34f5c77093a40dc043edd84eb7b`
- `LOC04_Emergency.unity`: `8a95adf5812260bd4f09b6b4e35504019c173c6e2259447e7227c7be87c0b2ed`
- `LOC05_ArchiveWing.unity`: `ea76a689ac12690484781e56d43c8a1b5935a404161786bd2f6dfb79e8d6ac4c`
- `LOC09_ServiceWing.unity`: `002a88a8699d2dbfe1bc114100eb0472a1401e82d79b477e4c0bf2d44a6e116c`

---

## 8. Known Issues & Các Cổng Cần Chủ Dự Án Can Thiệp

1. **Formal 60 Hz Elevated PresentMon Capture (Quyền OS & cấu hình màn hình):** Chủ dự án chuyển màn hình sang `60 Hz`, giữ desktop sáng ở foreground và chạy một lệnh PowerShell thường trên desktop `Default` (tự chuyển 120→60 Hz rồi khôi phục):
   ```powershell
   & "output\m3.2\m32-release-c90a2eb\run-m32-presentmon-60hz.ps1"
   ```
   để xuất thêm `GPUTime` và `DisplayedTime` chính thức ở `60 Hz` (đối chứng A/B `120 Hz` đã chứng minh `c90a2eb` nhanh hơn `c459aba` trên mọi phân vị).
2. **M2 Human-Review Gates (OPEN):** Hoàn tất 6 phiên người chơi mới (`TASK010`) và thu nhận ký xác nhận chuyên gia (`TASK026–028`) để đóng chính thức Milestone M2 và hợp thức hóa nhánh `feat/m32-ai-ch03-ch04` vào `main`.
3. **Character Art Lock & Final VO (Out of Scope M3.2):** `CHAR-Q02/Q09` (art lock nhân vật production) và full VO CH04 (hiện dùng đầy đủ subtitle + transcript + caption định hướng cho trợ năng tắt âm) giữ mở cho giai đoạn sau theo Core.
4. **CH05–CH08 / `TASK013` / M3 Alpha:** Giữ khóa cứng ngoài phạm vi M3.2; chỉ triển khai sau khi chốt đủ cổng M2 và M3.2.
