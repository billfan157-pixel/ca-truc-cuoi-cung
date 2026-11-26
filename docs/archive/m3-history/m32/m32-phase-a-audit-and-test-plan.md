# CTCC — M3.2 Phase A: Baseline, Gap Audit & Test Plan

- **Milestone / Scope:** M3.2 — Regional AI (`TASK011-A..E`), Physical Stealth & CH03–CH04 Gameplay Completion (`TASK012-A..D`)
- **Audit Date:** 2026-11-25
- **Repository:** `billfan157-pixel/ca-truc-cuoi-cung`
- **Git `HEAD` (`main` = `origin/main`):** `3c0b955de95bcf39853c30e2669b2bac24ad969e` (clean working tree)
- **Verified Technical Candidate:** `c459aba86192591b4adf5d267438e5fb4b77f704` (direct parent of `3c0b955`)
- **Canonical Standard:** `Core v3.3.1 Final Clean — Design Lock` (`10/10` SHA-256 verified)
- **Runtime Changes Made in Phase A:** **0** (audit & test plan only; halted at Production Gate before Phase B)

> [!IMPORTANT]
> **PRODUCTION EXCEPTION RECORDED (2026-11-25) — OPTION 2 APPROVED BY PROJECT OWNER**
> Per Core 07 `Backlog` & `Roadmap`:
> - **M2** remains **OPEN** (`TASK010` = `0/6` human sessions, `TASK026–028` = awaiting human specialist sign-off).
> - **`TASK011-A`** depends directly on **`TASK010`** (`Backlog!33`), and **`TASK012-A`** depends directly on **`TASK010` + `TASK011`** (`Backlog!39`).
> - On 2026-11-25, the project owner approved **Option 2 (Controlled Production Exception)**: execute M3.2 Phases B–F on dedicated branch `feat/m32-ai-ch03-ch04` strictly toward **`M3.2 Technical Verified`**, while keeping **M2 OPEN**, **`TASK010`/`TASK026–028` Human Review Pending**, **Art Lock OPEN**, **`TASK013`/CH05–CH08 strictly locked**, **Save Schema v7 untouched**, and **Core v3.3.1 Design Lock untouched**.

---

## 1. Baseline, Toolchain & Core v3.3.1 Verification

| Check | Expected (Core v3.3.1 / M3.2 Spec) | Actual (Verified 2026-11-25) | Status |
| --- | --- | --- | --- |
| GitHub fetch & `HEAD` | `main` at `3c0b955...`, candidate `c459aba...` | `git fetch --all --tags` OK; `main` = `origin/main` = `3c0b955de95bcf39853c30e2669b2bac24ad969e`; parent = `c459aba86192591b4adf5d267438e5fb4b77f704`; `origin/feat/an-sinh-full-campus-expansion` at `fdff2b8` (older doc branch already reflected in `main`) | **PASS** |
| Diff `c459aba..main` | Docs/evidence only | `18` files changed strictly under `docs/m2/` (`+1402 / -5`); **0** changes to `Prototype/`, `tools/`, `ArtSource/`, or `docs/01..07` | **PASS** |
| Core v3.3.1 Hashes (`docs/SHA256SUMS.txt`) | `10/10` exact SHA-256 match | `SHA256SUMS.txt` SHA-256 `b7b57f9f22be56e3371232e2cc0983705717af07d87c8e4d72171aa553bef34c`; **`10/10` files MATCH** (`01..07`, `CANON-AUDIT.md`, `CHANGELOG.md`, `RELEASE-NOTES.md`) | **PASS** |
| Core 07 Structure & DAG (`project_docs.py check`) | 7 Core docs, 27 sheets, 96-task DAG valid | `Đã kiểm cấu trúc 7 file Core, 27 sheet và DAG của 96 task.` (0 cycles, 0 missing refs) | **PASS** |
| Save Schema | `Schema = 7`, `MinSupportedSchema = 1` | `Prototype/Assets/CTCC/Runtime/Protocol.cs:83-84` (`Schema = 7`, `MinSupportedSchema = 1`) | **PASS** |
| Dialogue Master Sync (CH01–CH04) | `53` lines (`28/8/7/10`) + `CH01-D16b`/`CH04-D06` guards | `Dialogue.json`: `53` entries (`CH01=28, CH02=8, CH03=7, CH04=10`) + 5 condition records including `CH04-D06` & `CH01-D16b` (`B06`, `H05.verified`, `PT02.presented`) | **PASS** |
| Engine & Packages | Unity `6000.3.28f1`, URP `17.3.0`, AI Nav, Input System | `ProjectVersion.txt`: `6000.3.28f1 (8f082d2fe476)` installed at `%USERPROFILE%\Unity\Editors\6000.3.28f1\Editor\Unity.exe`; URP `17.3.0`, `com.unity.ai.navigation` `2.0.16`, `com.unity.inputsystem` `1.20.1`; ASCII junction `C:\CTCC-Unity-Workspace` present | **PASS** |
| Disk Free Space (`C:`) | Sufficient headroom for builds & captures | **`17.58 GB` free** (`457.32 GB` used) — adequate for 1–2 builds, **must monitor/prune old scratch outputs before Phase F** | **WATCH** |

---

## 2. Candidate `c459aba` Identity, 9 Scene Hashes, Provenance & Reproducibility

### 2.1 Binary & Package Verification (No Redundant Rebuild Needed)
All published `c459aba` binaries under `output/m2-final/m2-final-cand-c459aba/` and `output/m2-final/m2-final-release-c459aba/` are intact on disk and match [build-identity.txt](../../../m2/evidence/m2-final-candidate/build-identity.txt) and [SHA256SUMS.txt](../../../m2/evidence/m2-final-candidate/SHA256SUMS.txt) byte-for-byte:

| Artifact | Expected SHA-256 (`build-identity.txt` / `SHA256SUMS.txt`) | Actual Disk SHA-256 | Result |
| --- | --- | --- | --- |
| `CaTrucCuoiCung.exe` (`667,136 B`) | `fbd073ca59c4918edbc6704f5393cf59021173af11714c00db4212c50117492b` | `fbd073ca59c4918edbc6704f5393cf59021173af11714c00db4212c50117492b` | **MATCH** |
| `Assembly-CSharp.dll` (`894,976 B`) | `373d7c91bfcc071a04969b664f6a2fd03c553ee66e39fb65f6fcb8b58b149cbc` | `373d7c91bfcc071a04969b664f6a2fd03c553ee66e39fb65f6fcb8b58b149cbc` | **MATCH** |
| `CaTrucCuoiCung-M2-FINAL-c459aba-Windows-x64.zip` (`427.8 MB`) | `4fc873b6a2e8d3c1ecb65ff087e66e73977cdfefd16e89d7ec08c6a76dc29b3b` | `4fc873b6a2e8d3c1ecb65ff087e66e73977cdfefd16e89d7ec08c6a76dc29b3b` | **MATCH** |

### 2.2 Critical Provenance Finding (`GAP-PROV-01`): 9 Scene Hashes Diverge Between `main` (`3c0b955`) and `c459aba` Binary
A direct SHA-256 comparison revealed a critical provenance discrepancy between git-committed scenes and the `c459aba` candidate build:

| Scene File | `c459aba` `build-identity.txt` & `v33-host-20261125-103747` Bake | Clean `main` (`3c0b955`) / `c459aba` Git Tree (`c9f47c3` scenes) | Status |
| --- | --- | --- | --- |
| `AnSinh_Bootstrap.unity` | `ad40c0ef7be5...` | `82eb0df08f45...` | **DIVERGED** |
| `Campus_Grounds.unity` | `1780e4c41298...` | `3631ab883707...` | **DIVERGED** |
| `Campus_Interiors.unity` | `9ac129831a16...` | `e2e39d5ea635...` | **DIVERGED** |
| `LOC01_Exterior.unity` | `aff520e85f48...` | `a39fea19f65f...` | **DIVERGED** |
| `LOC02_Reception.unity` | `7109e4762e26...` | `df9342a46ad1...` | **DIVERGED** |
| `LOC03_Ward101.unity` | `35b11a8f92ae...` | `1f0ddf8e7ceb...` | **DIVERGED** |
| `LOC04_Emergency.unity` | `3628e3121654...` | `8e6edb0278bc...` | **DIVERGED** |
| `LOC05_ArchiveWing.unity` | `fd82e1e3c692...` | `1b72c952e263...` | **DIVERGED** |
| `LOC09_ServiceWing.unity` | `aa167b75a84d...` | `b94972e4e623...` | **DIVERGED** |

**Root Cause & Reproducibility Assessment:**
1. The last commit on `main` touching `Prototype/Assets/CTCC/Scenes/*.unity` was `c9f47c3` (pre-`46e706c` K-B/K-D/K-F outer gate seals, pre-`78ec669` S1 courtyard discharge, pre-`fb2e96d` lighting authoring, pre-`c459aba` `CHAIR-STAFF` authoring).
2. `c459aba` was compiled inside the worktree `output/m2-final/v33-host-20261125-103747/player-source/` (detached at `c459aba`), reusing the **20 uncommitted generated files** baked at `78ec669` (`9` scenes + `LightProbes_R/B` + `TileGeometry` + `LightingData` + `OcclusionCullingData` + `ReflectionProbe-0..5.exr`), explicitly logged in `prototype_diff` of `build-identity.txt`. All `9/9` scenes in `v33-host-20261125-103747/player-source/` match `build-identity.txt` **100%**.
3. `c459aba` worked cleanly despite reusing the `78ec669` scene bake only because `HospitalScene.cs` also enforced the `CHAIR-STAFF` clamp (`if (closedPos.x > 0.87f)`) at runtime, and `PC_RPAsset.asset` was committed.
4. **Reproducibility Rule:** Running `tools/build_m2.ps1 -SkipGenerate` on a clean checkout of `main`/`c459aba` will **NOT** reproduce `c459aba` — it would silently package stale `c9f47c3` scenes missing the S1 discharge and K-B/K-D/K-F seals! Reproducing from clean git requires full `tools/build_m2.ps1` **without** `-SkipGenerate` (running `HospitalAuthoring.ReferenceScene`). In M3.2, every scene authoring edit must trigger a clean scene regeneration (`ReferenceScene`), and generated scene/probe provenance must be cleanly tracked before Phase F packaging.

### 2.3 Generated-Asset & Audio Provenance
- [Prototype/ThirdPartyNotices.md](../../../../Prototype/ThirdPartyNotices.md) documents all third-party and generated sources: MakeHuman/MPFB2 `hm08` (CC0 1.0), ambientCG PBR micro-surfaces (CC0 1.0), CMU MoCap BVH (`07_04`, `18_08`, `08_04` with NSF EIA-0196217 credit), VieNeu-TTS v3 Turbo (Apache-2.0 offline WAVs), NumPy/SciPy synthesized SFX, Be Vietnam Pro (SIL OFL 1.1), and self-authored Blender 4.5 models (`.blend` in `ArtSource/Blender/`, `.fbx` in `Prototype/Assets/CTCC/Art/Meshes/`).
- Audio inventory verified on disk: `69` WAVs in `Prototype/Assets/CTCC/Art/Audio/` (`33` SFX/ambient/music including `KeyJingle.wav`, `TrolleyRoll.wav`, `PAStatic.wav`, `DoorLatch.wav`, `DoorCreak.wav`, `BreakerCut.wav`, `EmergencyHum.wav` + `36` Temp VO: `CH01=26`, `CH02=5`, `CH03=5`, `CH04=0`) and `12` foley WAVs in `Prototype/Assets/CTCC/Art/Resources/CTCC/`.
- `CHAR-Q02` (R/B visual direction approval) and `CHAR-Q09` (human license/source audit) remain **Human Review Pending**.

### 2.4 Performance Reference Baseline (`c459aba`, 1080p Medium `0.88`, D3D12, VSync 1, 60 Hz)
From [docs/m2/evidence/m2-final-candidate/performance.json](../../../m2/evidence/m2-final-candidate/performance.json) (`3` independent runs, `100%` foreground, Intel Core Ultra 7 155H / Intel Arc):
- **Unity GPU P95:** `12.77–12.97 ms` (P99 `14.48–14.61 ms`, Run 1 max `16.29 ms`, `0` frames > `16.67 ms`)
- **PresentMon GPUTime P95:** `13.74–13.88 ms` (P99 `15.66–15.83 ms`)
- **Unity Main CPU P95:** `6.88–7.23 ms` | **Unity Render CPU P95:** `~2.53 ms`
- **Unity Wall P95:** `16.68 ms` | **PresentMon DisplayedTime P95:** `16.73–16.75 ms` | **FrameTime P95:** `19.06–19.35 ms`
- **Peak Working Set:** `1,755.6–1,763.3 MiB` | **Launch → Ready:** `4.75–5.04 s`
- **M3.2 Targets:** Core hard gate `≤ 16.67 ms` P95; M3.2 internal target **Unity/PresentMon GPU P95 `≤ 14.5 ms`**.

---

## 3. Production Gates & `TASK010` Shortened 20–30 Minute Route Audit

### 3.1 M2 Human Gates (Actual Status)
- **`TASK010`** (`Backlog!32`, MUST, dep `TASK009`): **Human Review Pending (`Chưa làm`, `0/6` sessions)**. Kit & moderator launcher ready at `output/m2-final/m2-final-release-c459aba/start-task010-session.ps1` and [playtest-kit-task010.md](../../../m2/playtest-kit-task010.md). Three independent `5/6` gates per Core 01/07: (1) verify `C01` within `8 min` of starting `PZ01` unaided, (2) explain F101 false-affirmation consequence, (3) distinguish patient guide from threat by behavior.
- **`TASK026`** (`Backlog!88`, MUST, dep `TASK023`): **Human Review Pending (`Chờ duyệt`)** — Healthcare architect / clinician review of `LOC01–04` & Lan attire.
- **`TASK027`** (`Backlog!89`, MUST, dep `TASK022`): **Human Review Pending (`Chờ duyệt`)** — Historical 2005 PCCC specialist review of 3 mandatory items (Wing C section, smoke behavior, emergency response; aligned with `EVT026` erratum).
- **`TASK028`** (`Backlog!90`, MUST, dep `TASK023`): **Human Review Pending (`Chờ duyệt`)** — Clinical provenance, paperwork realism, and ethical representation review.

### 3.2 Audit of `TASK010` Shortened 20–30 Minute Route (`GAP-M2-T010`, P2/Operational)
Checking [playtest-kit-task010.md](../../../m2/playtest-kit-task010.md), Core 07 `Roadmap!4`, [walkthrough-gameplay-B01-B08-investigation-v1.md](../../ch01-history/walkthrough-gameplay-B01-B08-investigation-v1.md), and `c459aba` runtime (`HospitalGame.cs`, `HospitalGame.Pages.cs`):
1. **No dedicated shortened-CH01 mode in `c459aba`:** The binary runs the **full CH01-R B01–B08** walkthrough (designed for `20.5–30 min` main path + `6–10 min` optional STAFF/raking-light exploration) followed by **CH02** (`PZ01`/`PZ02` + cameo, `~8–15 min`). Blind first-time players exploring STAFF or struggling with F101 refutation may exceed `30 min` total (`~28–45 min`).
2. **CH03 exit exposed on ER summary (`HospitalGame.Pages.cs:1150`):** `EndOfVisit()` shows button `"Mở lối lên khu B"`, which launches `CH03`. Moderators running `TASK010` must stop the session at the ER ledger (`EndOfVisit`) so sessions measure the M2 vertical slice only.
3. **Telemetry boundary (`HospitalGame.cs:996, 1023`):** `-ctcc-playtest-log` writes automatically at `CH01-T10` (CH01 end) and does not record `PZ01` start / `C01` verify timestamps. As honestly noted in `playtest-kit-task010.md:94`, moderators must time `PZ01 → C01` manually on the tally sheet.

---

## 4. Task & Gate Acceptance Status Classification

| ID / Gate | Core 07 Dep | Classification | Actual Evidence / Open Blocker |
| --- | --- | --- | --- |
| **Phase A (M3.2 Audit)** | — | **Accepted** | This report; 10/10 Core hashes, git/provenance audit, code/scene gap matrix |
| **M0 (`TASK001/022/023/029`)** | — | **Accepted** | Completed in Core 07 (`4/4`) |
| **M1 (`TASK025` etc.)** | M0 | **Accepted** | Completed in Core 07 (`21/21`); regressed PASS on `c459aba` |
| **`TASK007` (CH01 B01–B08)** | M1 | **Technical Verified** | `CH01-Q01–Q14`, `CH01-WT-Q01/02`, `INPUT CH01-R FULL ROUTE PASS` on `c459aba`; awaiting `TASK010` human playtest |
| **`TASK008` (CH02 PZ01/02 + Cameo)** | M1 | **Technical Verified** | `CH02-Q01–Q06`, `LOG06-Q01`, `V30-Q01`, Door A & Door B InputQA PASS; awaiting `TASK010` |
| **`TASK009` (Kit, Temp VO, Captions)** | `TASK007`, `008` | **Technical Verified** | `AU01`, `T06`, `TASK009-C` (7/7 toggle props incl. `CHAIR-STAFF`) PASS; awaiting `CHAR-Q02/Q09` & `TASK026` |
| **`TASK010` (6-Player Playtest)** | `TASK009` | **Human Review Pending** | `0/6` real sessions; **BLOCKS M2 closure & DAG entry to `TASK011-A` / `TASK012-A`** |
| **`TASK026`, `027`, `028`** | `TASK022/023` | **Human Review Pending** | Dossiers ready; **BLOCKS M2 closure & Art Lock** |
| **`TASK011-A` (Hunter nav + patrol)** | **`TASK010`** | **Implemented (Partial / Gaps)** | `HunterOrderlyAI.cs` uses `NavMesh.CalculatePath` + `CapsuleCast` + `NavMesh.Raycast`, **but** has single shared actor teleporting on role switch (`GAP-AI-01`), stale `y=0` fallback waypoints (`GAP-AI-03`), untested per-zone runtime stitching & dynamic `morgueLock` NavMesh bug (`GAP-NAV-01`) |
| **`TASK011-B` (Hunter sight/hearing)** | `TASK011-A` | **Implemented (Partial / Gaps)** | `12 m` / `100°` FOV / `0.7 s` LOS + occlusion + walk/crouch/run/radio hearing exist, **but** missing `12 m` door-slam noise, missing Hunter `Suspicious` captions after 1st CH03 line & in CH04 B1, cross-floor hearing bleed risk (`Δy=4 m < 4.5 m`), and hearing skips `Suspicious` (`GAP-AI-03`) |
| **`TASK011-C` (Hunter chase/search/return)** | `TASK011-B` | **Implemented (Partial / Gaps)** | State machine exists, **but** `ChaseSpeed=3.15` vs `3.5–4.0` seed, lose-LOS timer `3.0 s` vs `>2 s`, `Search` only `3.2–3.5 s` stationary spin vs `12–18 s` anti-camp sweep, Story Mode still captures if toggled mid-chase, and zero automated QA for state transitions (`GAP-AI-03`, `GAP-AI-06`) |
| **`TASK011-D` (Orderly schedule + reset)** | `TASK011-C` | **Missing / Partial** | `WardRound` stub exists, **but** Orderly falls into `Investigate`/`Search` & reacts to radio like Hunter, static `MedicineCart` does not move, no named 3-stop schedule (`hành lang / quầy thuốc / cửa khu B`), no Route B route mutation/indicator, no Orderly QA (`GAP-AI-02`, `GAP-CH04-01`) |
| **`TASK011-E` (Safe zones & layer sync)** | `TASK011-D` | **Implemented (Partial / Gaps)** | Floor-aware `IsInSafeZone` & modal/inspector/M pause exist, **but** `ZoneUnregistering` never notifies `orderlyAi`, `CH03` never re-syncs on unload, role switch at `x=4.45` pops actor in player sight without LOS/FOV/hysteresis guard, `SafeRecovery` ignores occupancy & floor in CH03 (`GAP-AI-01`, `GAP-AI-05`) |
| **`TASK011` (Full Regional AI Integration)** | `TASK011-A..E` | **Missing (Blocked by `TASK010` + Gaps)** | Requires closing `GAP-AI-01..06` + `GAP-NAV-01..02` and dedicated per-role/per-transition QA |
| **`TASK012-A` (CH03 archive & PZ03)** | **`TASK010`, `011`** | **Implemented (Partial / Gaps)** | `E03`, `PZ03`, `C02` existence vs optional `C02_LETTER`, `C12` anonymous, `AN03` `M`→`R` save/load pass module QA, **but** `E03` inspect copy lacks Lan's AS-05 header/care notes, `PZ03` skips care-note/bag pairing UI (`pairedNoteAndBag` hardcoded `true`), `CH03-PU02` lacks selection/edit flow, and `CH03-CP02` is skipped (`GAP-CH03-01..03`) |
| **`TASK012-B` (CH03 patrol + retrieve route)** | `TASK012-A` | **Implemented (Partial / Gaps)** | Radio distraction & `LOC07` partition exist, **but** CH03 Hunter activates early on `E03` pickup (violating B04 quiet `AN03` & B06 `T06` timing), `CH03-Q03` not tested in Player Input QA, `CH03-D07` locked out when returning in CH04, safe room / 2-route loop needs tightening (`GAP-CH03-03..04`) |
| **`TASK012-C` (CH04 schedule + power PZ04/05)** | `TASK012-B` | **Missing (`PZ04`) / Partial (`PZ05`)** | `PZ05` 16 combinations & stairs-always-open work, **but** **`PZ04` (`CH04-Q01`) dual-route puzzle is MISSING** (`Ch04DutyRoom` only shows static text; `CH04-T02` auto-fires on floor box), `morgueLock` toggle fails to refresh `LOC09` NavMesh, `CH04-Q05` misses overload reload + battery floor + safe spawn checks (`GAP-CH04-01..02`, `GAP-NAV-01`) |
| **`TASK012-D` (CH04 H05 PZ06 + PT02 + AN05)** | `TASK012-C` | **Implemented (Partial / Gaps)** | `E02/E09` (`CH04-D09`), `E06a≠E06` (`PT01`), `E04/E07` (`CH04-D08`, `H05`, `PT02`), `CH04-D06`/`CH01-D16b` guard & `G04` work, **but** **`AN05` handoff is MISSING**, `CH04-D07` is never played/recorded, `CH04-D01/D05` missing from transcript, `CH04-Q03` misses "`E07` alone" & `E07` 2-part/hint checks, `E02/E09` cannot be re-read at desk (`GAP-CH04-03..06`) |
| **`TASK012` (CH03–04 Greybox & Puzzles)** | `TASK012-A..D` | **Missing (Blocked by `TASK010/011` + Gaps)** | Requires closing `GAP-CH03-01..04`, `GAP-CH04-01..06`, and full `CH03-Q01–Q05` + `CH04-Q01–Q06` on Module & Windows Player |
| **`TASK013` (CH05 M + Overlay)** | `TASK012` | **Out of Scope (Not Started)** | Explicitly excluded from M3.2; `G05`, `G06`, `E08`, CH05–CH08 stay strictly locked |

---

## 5. Expected / Actual / Gap Matrix (Against Core v3.3.1)

| Gap ID | Sev | Area / Task | Expected (Core v3.3.1 Source) | Actual (`3c0b955` Code & Scenes) | Concrete Remediation (Phase B–D) |
| --- | :---: | --- | --- | --- | --- |
| `GAP-PROV-01` | **P1** | Provenance / Reproducibility | Clean checkout reproduces verified build; scene hashes match committed source | All 9 `.unity` scenes in `main` are from stale `c9f47c3`; `c459aba` used 20 dirty cached files from `v33-host-20261125-103747` (`prototype_diff`) | Regenerate all 9 scenes cleanly via `HospitalAuthoring.ReferenceScene` after authoring changes; verify 0 stale scene reuse and record exact hashes |
| `GAP-M2-T010` | **P2** | `TASK010` Route (20–30 min) | Core 07 `Roadmap!4`: M2 slice = `20–30 min` shortened CH01 + C01 + cameo; 3× `5/6` independent thresholds | `c459aba` runs full CH01-R B01–B08 + CH02 (`~28–45 min` risk), exposes `"Mở lối lên khu B"` in `EndOfVisit()`, telemetry stops at `CH01-T10` | Keep documented in moderator protocol; do not alter `c459aba` M2 candidate unless owner requests a dedicated slice-mode build |
| `GAP-AI-01` | **P0** | `TASK011-A/E` Role Switching & Actors | Core 02 §4.2, Core 06 §03: Distinct Hunter (`CHAR005`), Orderly (`CHAR006`), Unrecorded (`CHAR007`) roles; zero pop-in in player sight, zero wrong-floor spawns | `HunterOrderlyAI.cs:97, 105-155` & `HospitalGame.cs:475`: single `GatekeeperActor` (owned by `LOC04`) teleports immediately when crossing `x=4.45` (`LOC09↔LOC10` doorway) or floors with only `<3.0 m` check (no LOS/FOV/hysteresis) | Separate logical/visual actor instances or non-colliding role presentations per floor/role (reusing existing character/prop rigs without new production art lock); enforce LOS + FOV + distance + floor hysteresis guard on spawn/role activation |
| `GAP-AI-02` | **P0** | `TASK011-D` Orderly (`CHAR006`) Behavior & Cart | Core 02 §4.2, Core 04 `PZ04`, Core 06 §03: Fixed 3-waypoint schedule (`hành lang`, `quầy thuốc`, `cửa khu B`), `4–8 s` dwell, moving medicine cart, `2 s` warning, gentle reset, Route B route update; **never chases or hunts** | `HunterOrderlyAI.cs:66, 175, 298-306`: `Orderly` reacts to radio distraction & falls into `Investigate`/`Search` when player `>= 3.4 m`; `CTCC_PROP_MedicineCart` is static at `(3.05, 4, 42)` (`HospitalAuthoring.Expansion.cs:115`) | Lock `CHAR006` strictly to scheduled `Patrol → Warning (2s) → GentleReset/Return` (never `Investigate`/`Chase`/`Search`); couple movable `MedicineCart` rig to Orderly; support Route A (default 3-stop loop) and Route B (updated observable route) |
| `GAP-AI-03` | **P1** | `TASK011-B/C` Hunter (`CHAR005`) Senses, Chase, Search & Captions | Core 06 §03: Walk `3 m`, crouch `1.5 m`, run `9 m`, door `12 m`, radio `14 m` (occluded, floor-bounded); `Suspicious` before `Investigate`/`Chase`; `Chase` `3.5–4.0 m/s`, lose LOS `>2 s` → `LastSeen` → `Search` (`12–18 s`, no camp); warning captions on every warning; Story Mode non-capture | `HunterOrderlyAI.cs:28-40, 227-345, 475`: `ChaseSpeed=3.15`, lose-LOS `3.0 s`, `Search` `3.2–3.5 s` stationary spin; no door-slam `12 m` noise; hearing crosses floors (`Δy=4 m < 4.5 m`); no caption after 1st CH03 line or in CH04 B1; Story Mode still captures if toggled mid-chase; fallback waypoints at stale `y=0` | Implement full 6-state Hunter transitions with floor-bounded hearing + door-slam hook, `LastSeen` → `Search` sweep (`12–18 s` with anti-camp cooldown), directional captions on every `Suspicious`/warning/chase event (`AU01`), Story Mode immediate breakoff, and valid multi-floor fallback waypoints |
| `GAP-AI-04` | **P1** | `TASK011-E` / `V33-Q05` `CHAR007 Unrecorded` | Core 02 §4.2, Core 06 §03, `DEC-V33-06`: Harmless apparition (C01–C11 only, **never C12**), **no chase**, **max 2 loops**, distinguishable non-solid apparition presentation | `HunterOrderlyAI.cs:68, 113, 274-289`: uses solid `GatekeeperActor` (`GK_StalkWalk`) looping infinitely in `LOC10`, indistinguishable from threat | Enforce strict `max 2 loops` then fade/disappear; zero chase/warning/capture/solid block; visually distinct apparition presentation (using existing `ApparitionHuman`/fade material & label) explicitly excluded from `C12` |
| `GAP-AI-05` | **P1** | `TASK011-E` Safe Pads, Recovery & Streaming Lifecycle | Core 06 §03, `TASK011-E`: Safe pads/reading pause AI; unload cleanly stops AI; recovery uses valid unoccupied floor-appropriate pad; no spawn near player on load | `HospitalGame.cs:207` `ZoneUnregistering` ignores `orderlyAi`; `CH03` skips `SyncState` in `Update`; `SafeRecovery` hardcodes `"AN03"` for all `y>=0` non-Orderly (even on floor 2) without occupancy check (`HunterOrderlyAI.cs:71, 350`) | Hook `ZoneUnregistering` & `ZoneReloaded` into `orderlyAi`; make recovery pad floor/chapter-aware (`AP06`/`AN03`/`AP07`/`AP08`/`AP09`) with occupancy + NavMesh check & anti-camp separation; re-seed AI safely away from player on load (`CH04-Q05`) |
| `GAP-AI-06` | **P1** | `TASK011` Automated QA Coverage | Prompt §5: Dedicated QA per role and per state transition | `HospitalGame.QA.cs:1091-1127` only tests CH03 Hunter LOS partition, 1 unreachable stimulus, and 1 radio displacement | Add full Module & Player Input QA suites covering Hunter (all 6 states, hearing/occlusion, door/radio, safe breakoff, Story Mode, modal/M pause), Orderly (3 stops, Route A/B, 2 s warn, gentle reset, no-chase), Unrecorded (max 2 loops, no-chase, ≠C12), and role-switch/unload safety |
| `GAP-NAV-01` | **P0** | Dynamic Door / `morgueLock` NavMesh Desync | Core 06 §02/03, Prompt §5: Dynamic obstacles & open/closed doors reflected in NavMesh without teleporting | `ChapterFour.SetPower` calls `CommitEvidenceAsync` (no `TryRefreshNavigation`); `LoadAndRegister` bakes `LOC09` NavMesh *before* `PresentCasework` activates `morgueLock` (`HospitalScene.cs:325`, `ZoneRegion.cs:169`) | Call `PresentCasework` before `RefreshRegionalNavMesh` on zone load, and refresh `LOC09` NavMesh whenever `morgueLock` / `archiveExitDoor` / dynamic doors toggle (`CommitAsync(..., geometryChanges: true)` or targeted regional refresh) |
| `GAP-NAV-02` | **P1** | Runtime Per-Zone NavMesh Stitching | Prompt §5: Check missing NavMesh, partial paths, multi-floor/portal seams at runtime | `HospitalAuthoring.cs:983` validates combined `BuildNavigation()`, while runtime builds per-zone `ZoneRegion.RefreshRegionalNavMesh()` | Add runtime QA validating `NavMesh.CalculatePath` (`PathComplete`) across every active zone portal (`LOC02↔05`, `LOC04↔05`, `LOC04↔09`, `LOC05↔06↔07`, `LOC09↔10↔11`) and partial-path / obstacle rejection |
| `GAP-CH03-01` | **P1** | `TASK012-A` `E03` Inspect Copy | Core 04 CH03 B03, `07/Evidence Specs!R5`, `design-lock-errata.md`: Full 12-code register with Lan's AS-05 / memory-loss notes & care traces (reserving `Bà Lành` name for optional letter) | `HospitalGame.Pages.cs:1173-1182` only shows a 5-line bare table | Expand `E03` inspect / Large-Print copy to match `07/Evidence Specs!R5` + `design-lock-errata.md` (12 codes, 3 groups, C02/C05/C10 care notes, C12 key-note, AS-05 margin note; no premature C02 name) |
| `GAP-CH03-02` | **P1** | `TASK012-A` `PZ03` & Optional `CH03-PU02` | Core 04 `PZ03` / `07/Puzzle specs!R5-R6`: Link code + item + care note; exact wrong message `"Mã/chi tiết chăm sóc chưa khớp"`; 2-strike field hint; optional `CH03-PU02` allows verify/edit without locking `G03` | `HospitalGame.Pages.cs:1210-1221` hardcodes `pairedNoteAndBag=true` and auto-verifies `C02_LETTER` in 1 click without matching | Add explicit item/note-to-code pairing selection in `AN03` `PZ03` with exact feedback + 2-strike hint, and clean optional matching/confirmation UI for `CH03-PU02` while keeping `G03` strictly on `C02` existence |
| `GAP-CH03-03` | **P1** | `TASK012-B` CH03 Beat Pacing & Hunter Timing | Core 04 CH03 B03–B06: `T03` sets `CH03-CP02`; B04 `AN03` is quiet (no AI); Hunter patrol begins at **B06 (`CH03-T06` leaving `AN03`, `CP03`)** | `HunterOrderlyAI.cs:106` activates Hunter immediately on `E03` pickup (B02/B03); `Casework.cs:753` skips `CH03-CP02` straight to `CH03-CP03` | Set `CH03-CP02` at `CH03-T03`, keep `LOC07` calm through `PZ03` & `AN03` vignette, activate CH03 patrol at `CH03-T06` (`CH03-CP03`) on leaving `AN03` post-`G03` (with clean radio/loop bypass) |
| `GAP-CH03-04` | **P1** | `TASK012-B` CH03 Retrieval, `CH03-D07`, & `CH03-Q01–Q05` Player QA | Core 04 CH03 B05–B07, `CH03-Q01–Q05`: Safe room + 2-route loop in `LOC07`; retrieve optional `C02_LETTER` / `C12` note; save during `E03` or `M` without duplication; S1 pre-G02 preserved | `Ch03ReadC12Note` (`HospitalGame.Pages.cs:1307`) blocked in `CH04`; `LOC07` safe alcove/loop signage minimal; `CH03-Q03` (radio/patrol) & `CH03-Q04` (save during `E03`) missing in Player Input QA | Allow re-reading `C12` note (`CH03-D07`) in `CH03` & `CH04` retrieval; clearly mark `LOC07` safe alcove & meeting-room bypass; cover all `CH03-Q01–Q05` on both Module & Windows Player Input QA |
| `GAP-CH04-01` | **P0** | **`TASK012-C` `PZ04` / `CH04-Q01` Dual-Route Puzzle (TOP PRIORITY)** | Core 04 CH04 B02 / `PZ04`, `07/Puzzle specs!R7`, `07/QA!R27`: **Route A** (read 3-waypoint schedule, observe Orderly/cart at `hành lang / quầy thuốc / cửa khu B`, pass when cart is at `quầy thuốc`) **AND Route B** (cross-check valid room-status slip → observably updates cart route with notice + directional arrow); gentle reset; no forced falsification | **MISSING:** Static 2-sentence popup in `Ch04DutyRoom`; static `MedicineCart`; `CH04-T02` fires trivially on walking into `LOC05`; no room-status board, no slip cross-check, no route update, no `CH04-PU01` state, `CH04-Q01 = Chưa test` | Author physical `PZ04` props in `LOC05`/`LOC06` (schedule board, room-status board & valid clinical slip, pharmacy counter stop, moving `MedicineCart` + `CHAR006`, route arrow/notice); gate `CH04-T02` on genuine corridor crossing via Route A OR Route B (without modifying/falsifying records); add full Module + Windows Player Input QA for both routes + reset + accessibility |
| `GAP-CH04-02` | **P1** | `TASK012-C` `PZ05` & `CH04-Q05` B1 Save/Load/Relay | Core 04 `PZ05`, `CH04-Q02/Q05`: 16 combinations, stairs always usable, relay noise draws B1 Hunter with caption, reload on overload or at morgue preserves power state, min battery `≥120 s`, no threat spawn near player | `SetServicePower` lacks directional relay caption; `CH04-Q05` never tests reload after overload (`0`), min battery floor, or threat spawn distance; optional Lift lacks clear diegetic note | Add relay directional caption, lift shortcut note, `CH04-PU01..03` puzzle-hint whitelist compatibility (preserving Save Schema v7!), and full `CH04-Q02`/`CH04-Q05` tests (overload reload, powered reload, battery floor, AI spawn distance) |
| `GAP-CH04-03` | **P1** | `TASK012-D` `E02`/`E09` Copy & Retrieval | Core 04 CH04 B01, `07/Evidence Specs!R4/R13`: Full `E02` & `E09` copy (`23:10..07:40` & `02:50`), `CH04-D09`, always re-readable/retrievable at `LOC06` | `Ch04DutyRoom` disables `E02`/`E09` buttons once taken; `E09` copy abbreviated (omits timeline timestamps) | Keep `E02`/`E09` re-inspectable at `LOC06` without duplicating evidence; align full inspect copy to `07/Evidence Specs!R4/R13` |
| `GAP-CH04-04` | **P1** | `TASK012-D` `PZ06` (`E04`/`E07`) & `CH04-Q03` | Core 04 `PZ06`, `07/Evidence Specs!R11`, `CH04-Q03`: Neither `E04` alone nor `E07` alone unlocks `H05`; `E07` explicitly carries badge + recovery location note; missing-part hint points to dry cabinet | `CH04-Q03` only tests `E04` alone (never `E07` alone); `E07` UI skips badge + recovery note breakdown & missing-part hint | Verify both `E04`-only and `E07`-only keep `H05`/`PT02`/`G04`/`D16b` locked; surface `E07` two-part provenance (`badge` + `recoveryLocationNote` + charred AS-05 slip) and missing-part hint |
| `GAP-CH04-05` | **P1** | `TASK012-D` `CH04-D01/D05/D07` Transcript & `G04 → AN05` Handoff | Core 04 CH04 B06 & Exit: `G04` opens **`AN05`** handoff; `CH04-D06` + optional `CH01-D16b` guarded by `B06 & H05 & PT02`; `CH04-D07` plays after pause; no `G05`/`G06`/`E08`/CH05 unlock | **No `AN05` anchor/pad/interactable exists** (`RefreshObjective` tells player to return to floor 2 where nothing responds); `CH04-D07` is never delivered; `CH04-D01` & `CH04-D05` missing from transcript | Author `AN05` handoff anchor at `LOC06`/`LOC05` (unlocking only when `G04 && CH04-T06`, summarizing CH04 completion while keeping CH05/`G05`/`G06`/`E08` locked); record `CH04-D01`, `CH04-D05`, and `CH04-D07` to transcript/UI cleanly |
| `GAP-CH04-06` | **P2** | `Save Schema v7` Compatibility Guard | Prompt §11: **Do NOT change Save Schema v7** (`Investigation.Schema == 7`, v1–v7 migration) | `AtomicSaves.cs:280` whitelists only `"CH01-F101" | "CH03-PU01" | "CH03-PU02"` in `puzzles` | Keep `Investigation.Schema = 7` untouched; persist `PZ04` route/completion via existing `events` list (`CH04-T02`, `PZ04.RouteA`, `PZ04.RouteB`, `PZ04.ScheduleUpdated`) and extend `puzzles` whitelist cleanly within Schema v7 only if needed without breaking v1–v7 saves |

---

## 6. Phased Execution & Test Plan (Ready Upon Gate Clearance)

### Phase B — `TASK011` Regional AI Completion
- **Architecture:** Evolve `HunterOrderlyAI.cs` with explicit role profiles (`Hunter`, `Orderly`, `Unrecorded`), non-teleporting role/actor presentation (moving `MedicineCart` coupled to `CHAR006`, distinct apparition presentation + 2-loop cap for `CHAR007`, strictly separate from `C12`), floor-bounded hearing + `12 m` door-slam hook, full 6-state Hunter FSM (`Patrol ↔ Suspicious (2s) ↔ Investigate ↔ Chase ↔ Search (12–18s) ↔ Return`), safe-zone/modal/inspector/`M`/Story Mode guards, `ZoneUnregistering`/`ZoneReloaded` lifecycle hooks, and dynamic `morgueLock` NavMesh refresh (`GAP-NAV-01`).
- **Automated Tests:** Dedicated Module QA & Player Input QA cases per role (`TASK011-A..E`), state transitions, LOS/hearing occlusion, partial/invalid path non-teleport, Story Mode, mute captions, and additive unload/reload.

### Phase C — `TASK012-A/B` CH03 Completion
- **Implementation:** Complete `E03` inspect copy (per Core 07 + approved E03 errata), interactive `PZ03` code-item-note linking + 2-strike hint + optional `CH03-PU02` match/edit, `CH03-CP02` at `T03` & patrol activation at `T06`/`CP03` (keeping `AN03` B04 calm), `LOC07` safe alcove & meeting-room bypass clarity, and `CH03-D07` retrieval support.
- **Verification:** Run `CH03-Q01–Q05` + `V33-Q05` (`S1` pre-G02) on both Module QA and Windows Player Input QA (including save/load mid-`E03`, mid-`M`, skip, and unload/reload duplicate checks).

### Phase D — `TASK012-C/D` CH04 Completion
- **Priority 1 (`PZ04` / `CH04-Q01`):** Implement physical 3-stop `LOC05` Orderly + moving `MedicineCart` route (`hành lang → quầy thuốc → cửa khu B`, `4–8 s`/stop), schedule board, room-status board & valid slip cross-check, observable Route B cart reroute with notice + directional arrow, gentle reset to safe station without losing items/journal, and non-falsification guarantee.
- **Priority 2 (`PZ05`, `PZ06`, `G04`, `AN05`):** Fix `morgueLock` NavMesh refresh, add relay directional caption, re-inspectable full `E02`/`E09`, `E07` 2-part provenance & missing-part hint, `CH04-D01/D05/D07` transcript parity, strict `CH04-D06`/`CH01-D16b` guards, and `AN05` handoff anchor (CH05/`G05`/`G06`/`E08` strictly locked).
- **Verification:** Run `CH04-Q01–Q06` (explicitly testing **both Route A and Route B of `PZ04`** on Windows Player Input QA, both `E04`-only and `E07`-only for `CH04-Q03`, and overload + morgue reload for `CH04-Q05`).

### Phase E — Full Regression (M2-FINAL + M3.2 on New Binary)
- Re-run complete M2-FINAL regression (`F101` A/B/C + revision history, atomic save fault injection/retry, schema `v1–v7` migration, `CH01–CH04` gameplay routes, bidirectional `S1` pre-G02, streaming + seal-before-unload, R/B/M & collision, `[E]`/`[K]`, keyboard-only, mute, Large Print 130%, Reduced Motion, Story Mode, `TASK011/012`, `CH03-Q01–Q05`, `CH04-Q01–Q06`, `WORLD-Q`, `W-Q`, `V22-Q03`, `V30-Q01`, `V33-Q05`).
- Zero test weakening (no player speed tweaks, no collider bypass, no teleport in normal-route Input QA).

### Phase F — Windows x64 Candidate, Benchmarks & Deliverables
- Check free disk space (`C:` currently `17.58 GB`); clean temporary/obsolete build caches only if needed without touching `c459aba` release package.
- Clean scene generation (`ReferenceScene`) + Windows x64 non-development build (Unity `6000.3.28f1`, URP `17.3`, D3D12).
- Execution order: Targeted AI/CH03/CH04 QA → Full Module QA → Full Player Input QA → 4 D3D12 failure probes → 3× independent PresentMon 60 Hz 1080p Medium (`0.88`) benchmarks comparing against `c459aba` (`12.77–12.97 ms` Unity GPU P95 / `13.74–13.88 ms` PresentMon GPU P95; internal target `≤ 14.5 ms`, hard gate `≤ 16.67 ms`) → real gameplay video capture + SHA-256 manifest + compact reports under `docs/m3/`.

---

## 7. Blocking Gates Summary (Decision Required Before Phase B)

| Blocker ID | Gate | Status | Effect on M3.2 |
| --- | --- | --- | --- |
| `TASK010` | M2 Playtest (6 fresh players, 3× `5/6` independent thresholds) | **Human Review Pending (`0/6`)** | Direct DAG prerequisite of `TASK011-A` and `TASK012-A` |
| `TASK026` | Healthcare Architect / Clinician Review | **Human Review Pending** | Blocks M2 closure & Art Lock (does not prevent greybox/gameplay prototyping if exception granted) |
| `TASK027` | Historical 2005 PCCC Fire Safety Review | **Human Review Pending** | Blocks M2 closure & Art Lock |
| `TASK028` | Clinical Provenance & Ethics Review | **Human Review Pending** | Blocks M2 closure & Art Lock |

**Requested Project Owner Decision:**
1. **Option A (Strict Sequential Gate):** Keep Phase B–F paused until `TASK010` (6 real human players on `m2-final-release-c459aba`) completes and passes.
2. **Option B (Explicit Production Exception for Technical M3.2):** Approve starting **Phase B → Phase F** now on a dedicated branch strictly for **`M3.2 Technical Verified`** (keeping M2 OPEN, `TASK010` & `TASK026–028` Human Review Pending, Art Lock OPEN, `TASK013` untouched, and Core v3.3.1 Design Lock untouched).
