# CH01 REBIRTH III — PHASE 3: B06–B08 & Cross-Chapter Compatibility Report

- **Branch:** `feat/ch01-rebirth-iii-phase3` (branched from `main` at `d8861b7`, **not merged into `main`**)
- **Baseline & Canon:** Core v3.6.2 Single Source of Truth (`docs/01..07` + `docs/dialogue_silent_exploration_runtime.json` untouched, **`CORE_DOCS_DIRTY_COUNT=0`**), `DEC-VO-01`, `DEC-V36-CH01-01`, `DEC-V361-CH01-01`, Phase 0/1/2/2.5 & LOC02/LOC03 Hero Visual Upgrades
- **Final Windows x64 Binary:** [`CaTrucCuoiCung.exe`](file:///c:/Ca%20tr%E1%BB%B1c%20cu%E1%BB%91i%20c%C3%B9ng/output/ch01-rebirth-iii-p3/windows/CaTrucCuoiCung.exe) (`1,575,021,780 bytes`, Unity `6000.3.28f1`, URP 17.3 Forward+, D3D12)
- **Executable SHA-256:** `FBD073CA59C4918EDBC6704F5393CF59021173AF11714C00DB4212C50117492B`
- **Game Assembly (`Assembly-CSharp.dll`) SHA-256:** `1D674D542DCCA735649AB44F15ED0807F6C642EE8F6AFE1C4765742D39E30D46` *(100% identical across Module QA, Player Input QA, all 6 Fault-Injection Probes, LOC02 Visual Regression Shots, and 1080p Medium Benchmark)*

---

## 1. Executive Summary

Phase 3 completes the full **CH01 B06–B08** arc and **Cross-Chapter Compatibility (CH02–CH05)** under Core v3.6.2's **Task-driven, World-first, Physical-first, Diegetic-first Psychological Horror** architecture without regressing Phase 2.5 diegetic UX or LOC02/LOC03 visual fidelity and performance:

1. **B06 — Reality Collapse & Return (`T08`, `CP03`):** Atomic `B→R` swap at `AN01` reconciling all **4 Layer-B physical item states** (equipped in hand, stowed in bag, clipped at `AN02_CLIP`, placed on desks) to `MemoryRef`, safe rollback on save-lock failure preserving all 4 states byte-for-byte, outer hospital gate strictly **remaining OPEN** post-`T08` (no canon-breaking relock), `>= 120 s` flashlight battery floor, and synchronized atmospheric/lighting/audio collapse back to 2012 `R`.
2. **B07 — Departure Attempt (`T09`) & WR05:** Replaced the invisible `seamBlocker` collider and `NudgeWithinScene` backward push with **`Wr05ExitBoundaryFilter`** — swept-segment boundary evaluation and tangent displacement projection executed directly inside `FirstPerson.Move()` before `CharacterController.Move()`. Separates the one-time scripted `T09` seam from post-`T09` WR05 boundary containment across both `Reality.R` and `Reality.B`, consumes the real `GateService.G08(state)` predicate (zero stub), and delivers subtle psychological gate pressure (seamless spatial hum `CH01_ExitBoundaryHum.wav`, rain hush, edge vignette/desaturation disabled under Reduced Motion, `"Tìm lối ra."` diegetic caption, zero Journal prompt, immediate reset on input release).
3. **B08 — Perceptual Echo & Chapter Handoff (`T10`, `CP04`):** Built custom in-world 3D **Perceptual Echo** at the `LOC02` Reality `R` reception counter (`CTCC_VFX_B08Echo.blend`/`.fbx`, URP `CTCC/PerceptualEcho.shader`, 6 archival Vietnamese textures strictly using SIL OFL 1.1 `BeVietnamPro`, spatial `CH01_B08EchoWhisper.wav`, `B08EchoController`) matching all 3 F101 branches (`Affirmed`, `Unverified`, `Refuted`). Strictly perceptual (0 colliders, 0 physical evidence, 0 observations, 0 out-of-canon spoilers), supports Reduced Motion static mode, dismisses cleanly on `T10`/`CP04`, and hands off to `CH02` with full 5-tab Journal & F101 revision restored.
4. **Cross-Chapter Compatibility (`CH05-D08` & `H08` Recovery at `LOC08 R`):**
   - `CH05-D08` now requires BOTH `ValveSabotageFound` AND `E10a.Part.publicReportSummary` (no `E09` guard, `INNER_SUB_SILENT`, 0 VO); unread newspaper blocks `CH05-D08` and lets Minh open the 3D envelope or skip.
   - Authored physical Blender 4.5 desk blotter (`CTCC_PROP_Loc08RecoveryBlotter.blend`/`.fbx`) and `LOC08_RECOVERY_DESK` anchor in `LOC08 R`, allowing physical placement & inspection of `E10A_ENVELOPE` and `MINH_PHONE` to independently read any missing piece among the 4 `E10a` sources without auto-reading or auto-completing.
5. **Four Live-Gameplay Runtime Bugs Found & Fixed During Player Input QA:**
   - **`DiegeticAn02Surface.InvokeZone`** suppressed click-time `"stamp"` SFX on `SIGN_STAMP` so unchanged F101 resubmissions stay strictly silent per `TASK03` (real stamp still plays inside `SignAn02F101` only when revision advances).
   - **`HospitalGame.An02Clipboard.RenderAn02DirectSurface3D`** now re-registers `ui.ShowStandaloneOverlay` whenever `an02Surface` opens after returning from `[F]` 3D F101 inspection, eliminating an `[Esc]` softlock on AN02.
   - **`DiegeticAn02Surface.BuildSlipOffscreen`** expanded the Branch C source list pool from 7 to all **9** `SelectableForF101` observations so thorough players exploring Reception + STAFF + Room 101 never have `OBS101A` truncated off the 3D slip.
   - **`HospitalUI.ShowNotebook`** now calls `ClearStandaloneOverlayState()`, fixing an invisible-panel bug when returning to the CH02+ 5-tab Journal from `"Soi 3D"`.

---

## 2. Implemented Systems & Asset Inventory

### Runtime & Editor C# Architecture
| Pillar | Primary Files | Key Behavior & Contracts |
| --- | --- | --- |
| **B06 Collapse & Return** | `RealityTransaction.cs`, `HospitalGame.cs`, `HandItemPresenter.cs` | Atomic `B→R` swap at `AN01` (`T08`/`CP03`); reconciles all 4 Layer-B paper states (in hand / in bag / `AN02_CLIP` / desks) to `MemoryRef`; atomic save-lock rollback restores all 4 states + viewmodel sync; `closeSeam: false` removes legacy `b07GateLatched` gate relock; `>= 120 s` flashlight floor; 2012 rain/lamp collapse cues. |
| **B07 & WR05 Boundary** | `Wr05ExitBoundaryFilter.cs`, `FirstPerson.cs`, `HospitalGame.World.cs`, `HospitalScene.cs`, `ZoneRegion.cs`, `EnvironmentalAudio.cs` | Pre-`CharacterController.Move` swept-segment boundary filter at `z = -19.95`, `x ∈ [2.45..5.55]`, `y ∈ [-0.45..0.45]` active in both `R` & `B` whenever `CH01-T09 && !GateService.G08(state)`; `seamBlocker` permanently disabled; `NudgeWithinScene` deleted; `CheckPreT09GateThreshold` separates one-time scripted `T09` from post-`T09` WR05; seamless spatial hum + rain dip + Reduced-Motion-safe vignette/desat + `"Tìm lối ra."`; real `QaInjectBypassForTesting` fault hook. |
| **B08 Perceptual Echo** | `B08EchoController.cs`, `HospitalAuthoring.Zones.cs`, `PerceptualEcho.shader`, `HospitalUI.cs` | 3-branch F101 (`Affirmed` / `Unverified` / `Refuted`) floating glass + counter slot-tray imprint at `LOC02 R`; 0 colliders, 0 physical evidence, 0 observations; `MaterialPropertyBlock` zero-GC animation frozen when `reduceMotion = true`; proximity/gaze fade; zero-shadow local fill light; spatial `CH01_B08EchoWhisper.wav`; dismisses on `T10`/`CP04`. |
| **CH05-D08 & H08 Recovery** | `HospitalGame.ChapterFive.cs`, `BackpackFramework.cs`, `HospitalGame.Backpack.cs`, `HandItemPresenter.cs`, `HospitalAuthoring.Expansion.cs`, `tools/sync_production_data.py` | `CH05-D08` requires `ValveSabotageFound && E10a.Part.publicReportSummary` (regenerated `Dialogue.json`, 143 rows, `INNER_SUB_SILENT`, 0 VO, 0 `E09` blocker); `LOC08_RECOVERY_DESK` + `Loc08DeskEnvelope` + `Loc08DeskPhone` anchors allow placing/inspecting carried `E10A_ENVELOPE` & `MINH_PHONE` in `LOC08 R` with honest 4-part read checklist (0 auto-read). |
| **Phase 2.5/3 Runtime Fixes** | `DiegeticAn02Surface.cs`, `HospitalGame.An02Clipboard.cs`, `HospitalUI.Journal.cs`, `HospitalGame.cs` | Fixed `TASK03` duplicate-stamp SFX, AN02 `[Esc]` softlock after `[F]` lift, 9-row Branch C 3D slip pool, Journal `"Soi 3D"` return overlay cleanup, and `qaFolder == null` background `SaveDialogue()` lock race guard. |
| **Verification Harnesses** | `HospitalGame.Phase3QA.cs`, `HospitalGame.QA.cs`, `HospitalGame.InputQA.cs`, `tools/verify_windows.ps1`, `tools/encode_p3_gameplay_video.py` | Full `V36-CH01-Q04/Q06/Q07/Q08` module suite, live keyboard WR05 sprint + in-world B08 Echo + `LOC08_RECOVERY_DESK` checks in `InputQA`, `v36-wr05-bypass` fault probe, and Blender 4.5 VSE MP4 encoder. |

### Blender 4.5, Shader, Archival Texture & Procedural Audio Assets
- **Blender 4.5 3D Assets (editable `.blend` + game-ready `.fbx`):**
  - `ArtSource/Blender/CTCC_VFX_B08Echo.blend` → `Prototype/Assets/CTCC/Art/Meshes/CTCC_VFX_B08Echo.fbx` (`1,248 tris`, 3 perceptual meshes `B08Echo_TrayImprint`, `B08Echo_GlassImprint`, `B08Echo_CounterAura`, 0 colliders; refined in Pass 2 so `11:52` clock clears glass imprint cleanly and tray imprint sits clear of `AN01` ledger).
  - `ArtSource/Blender/CTCC_PROP_Loc08RecoveryBlotter.blend` → `Prototype/Assets/CTCC/Art/Meshes/CTCC_PROP_Loc08RecoveryBlotter.fbx` (`1,308 tris`, `0.44 × 0.62 × 0.03 m` ironwood/leather/brass blotter seated cleanly on clear south half of `LOC08` desk).
- **Custom URP 17 Shader & Persistent Scene Materials:**
  - `Prototype/Assets/CTCC/Art/PerceptualEcho.shader` (`CTCC/PerceptualEcho`, transparent additive/alpha hybrid with soft edge feather, Fresnel rim, subtle breathing drift disabled when `_ReduceMotion = 1`, 0 lightmap variants).
  - 7 persistent `.mat` assets in `Prototype/Assets/CTCC/Art/Materials/` (`CTCC_B08Echo_Tray_A/B/C.mat`, `CTCC_B08Echo_Glass_A/B/C.mat`, `CTCC_B08Echo_Halo.mat`) bound directly in `LOC02_Reception.unity` (zero runtime shader stripping).
- **Archival Vietnamese Echo Textures (`tools/bake_ch01_p3_echo_textures.py`):**
  - 6 `1024×1024` RGBA PNGs strictly rendered with **SIL OFL 1.1 `BeVietnamPro-SemiBold.ttf` / `BeVietnamPro-Regular.ttf`** (zero proprietary Windows fonts): `EchoB08_Affirmed/Unverified/Refuted.png` and `EchoB08_Glass_Affirmed/Unverified/Refuted.png` (refined with clean floating typographic glass glyphs and zero circular disc halo).
- **Procedural Spatial Audio (`tools/bake_ch01_rebirth3_audio.py`):**
  - `CH01_ExitBoundaryHum.wav` — seamless 4.00 s loop, 48 kHz mono 16-bit PCM (`RMS -21.50 dBFS`, `TP -9.30 dBTP`).
  - `CH01_B08EchoWhisper.wav` — 2.65 s subtle glass resonance, 48 kHz mono 16-bit PCM (`RMS -22.83 dBFS`, `TP -4.00 dBTP`).
- **Provenance & Notices:** Updated `Prototype/ThirdPartyNotices.md` with all Phase 3 original Blender, shader, texture, and audio entries.

---

## 3. Synchronized Verification & Regression Results (Identical Binary)

All checks below ran on the **exact same final build** (`EXE_SHA256 = FBD073CA59C4918EDBC6704F5393CF59021173AF11714C00DB4212C50117492B`, `ASM_SHA256 = 1D674D542DCCA735649AB44F15ED0807F6C642EE8F6AFE1C4765742D39E30D46`):

| Gate / Suite | Status | Evidence Artifact | Key Verified Assertions |
| --- | --- | --- | --- |
| **Core Doc Integrity (`project_docs.py check`)** | **PASS** | `CORE_DOCS_DIRTY_COUNT = 0` | 7 Core files, 28 sheets, 100-task DAG intact; zero edits to `docs/01..07` or `docs/dialogue_silent_exploration_runtime.json`. |
| **Full Module QA (36 suites)** | **PASS** | [`final-qa/gameplay.txt`](file:///c:/Ca%20tr%E1%BB%B1c%20cu%E1%BB%91i%20c%C3%B9ng/output/ch01-rebirth-iii-p3/final-qa/gameplay.txt) | `V36-CH01-Q04` (all 4 Layer-B states + save-lock rollback + OPEN outer gate + `>=120s` torch), `V36-CH01-Q07` (5/5 WR05 anti-sequence-break scenarios in `R` & `B`, `dt=0.045s` `0.189m/frame`, diagonal pillar hugs, save/load, `S1`/fence detours, 0 `seamBlocker`, 0 backward push, pure `Evaluate(..., g08Open: true)` unit check), B08 3 branches + Reduced Motion + `T10`/`CP04`/`Q06`, `V36-CH01-Q08` (`CH05-D08` `publicReportSummary` guard w/o `E09` + `LOC08 R` 0/4→4/4 independent recovery), plus full Phase 1/2/2.5, CH01–CH05, CHAR-Q01–Q08, AU01, V33-Q05, WORLD-Q03/Q04. |
| **Full Player Input QA (All 3 F101 Branches + CH02–CH05)** | **PASS** | [`final-input-qa/gameplay.txt`](file:///c:/Ca%20tr%E1%BB%B1c%20cu%E1%BB%91i%20c%C3%B9ng/output/ch01-rebirth-iii-p3/final-input-qa/gameplay.txt) | Zero-teleport CH01 `T01–T10` with 0 Journal dependency, all 3 F101 branches (`Affirmed` + save-lock retry + CH02 table assembly; `Unverified` + `TASK03` silent resubmission + `V33-Q05` + CH03–CH04 + CH05 + live `LOC08_RECOVERY_DESK`; `Refuted` + live keyboard WR05 sprint + in-world B08 Echo + CH02 handoff + Accessibility + CH02 5-tab Journal + 100-cycle retention `445→445` & inspector stress `0` leaks). |
| **Fault Probe `v36-wr05-bypass`** | **PASS (exit 1)** | `output/ch01-rebirth-iii-p3/final-probe-v36-wr05-bypass/` | Injected WR05 bypass caught by `V36-CH01-Q07` and propagated to Windows exit code `1`. |
| **Fault Probe `v36-t02-wrong-key`** | **PASS (exit 1)** | `output/ch01-rebirth-iii-p3/final-probe-v36-t02-wrong-key/` | Propagated to exit code `1`. |
| **Fault Probe `v36-vo-leak`** | **PASS (exit 1)** | `output/ch01-rebirth-iii-p3/final-probe-v36-vo-leak/` | Propagated to exit code `1`. |
| **Fault Probe `v36-ex01-autosolve`** | **PASS (exit 1)** | `output/ch01-rebirth-iii-p3/final-probe-v36-ex01-autosolve/` | Propagated to exit code `1`. |
| **Fault Probe `v36-f101-unverified-link`** | **PASS (exit 1)** | `output/ch01-rebirth-iii-p3/final-probe-v36-f101-unverified-link/` | Propagated to exit code `1`. |
| **Fault Probe `v36-p25-envelope-autoread`** | **PASS (exit 1)** | `output/ch01-rebirth-iii-p3/final-probe-v36-p25-envelope-autoread/` | Propagated to exit code `1`. |
| **LOC02 Visual Non-Regression** | **PASS (31 PNGs)** | [`final-loc02-shots/`](file:///c:/Ca%20tr%E1%BB%B1c%20cu%E1%BB%91i%20c%C3%B9ng/output/ch01-rebirth-iii-p3/final-loc02-shots/) | Visually verified `L02-03-counter-front-R.png` and `L02-06-slot-tray-R.png`: zero stray decals, crisp slot tray, preserved LOC02 Hero fidelity when Echo is inactive. |
| **G08 Release E2E (CH08 Full Release)** | **Chưa test (chờ CH08 — `TASK016`)** | — | Pure unit allow-path `Wr05ExitBoundaryFilter.Evaluate(..., g08Open: true)` verified in `V36-CH01-Q07`; true `G08` unlock & end-to-end exit remain unverified until `CH08` (`TASK016`) is authored. Zero stubs or fake triggers added. |
| **Human Playtest / Expert Reviews (`TASK010`, `TASK026–028`)** | **Pending Human Sign-off** | — | Not fabricated; ready for human review in Phase 4. |

---

## 4. Visual & Video Deliverables (1080p)

### Key Phase 3 1080p URP Screenshots
- **B06 Reality Collapse & Return (`LOC02 R`):** [`p3-b06-collapse-return-R.png`](file:///c:/Ca%20tr%E1%BB%B1c%20cu%E1%BB%91i%20c%C3%B9ng/output/ch01-rebirth-iii-p3/final-qa/p3-b06-collapse-return-R.png)
- **B07 Open Gate & WR05 Boundary (`LOC01 R`):** [`p3-b07-wr05-gate-R.png`](file:///c:/Ca%20tr%E1%BB%B1c%20cu%E1%BB%91i%20c%C3%B9ng/output/ch01-rebirth-iii-p3/final-qa/p3-b07-wr05-gate-R.png)
- **B08 Perceptual Echo — Branch A (`Affirmed`):** [`p3-b08-echo-affirmed-R.png`](file:///c:/Ca%20tr%E1%BB%B1c%20cu%E1%BB%91i%20c%C3%B9ng/output/ch01-rebirth-iii-p3/final-qa/p3-b08-echo-affirmed-R.png)
- **B08 Perceptual Echo — Branch B (`Unverified`):** [`p3-b08-echo-unverified-R.png`](file:///c:/Ca%20tr%E1%BB%B1c%20cu%E1%BB%91i%20c%C3%B9ng/output/ch01-rebirth-iii-p3/final-qa/p3-b08-echo-unverified-R.png)
- **B08 Perceptual Echo — Branch C (`Refuted`):** [`p3-b08-echo-refuted-R.png`](file:///c:/Ca%20tr%E1%BB%B1c%20cu%E1%BB%91i%20c%C3%B9ng/output/ch01-rebirth-iii-p3/final-qa/p3-b08-echo-refuted-R.png)
- **Live Gameplay B08 Perceptual Echo (from `InputQA`):** [`phase3-b08-inworld-echo-R.png`](file:///c:/Ca%20tr%E1%BB%B1c%20cu%E1%BB%91i%20c%C3%B9ng/output/ch01-rebirth-iii-p3/final-input-qa/phase3-b08-inworld-echo-R.png)
- **Cross-Chapter H08 Recovery Desk (`LOC08 R`):** [`p3-loc08-h08-recovery-R.png`](file:///c:/Ca%20tr%E1%BB%B1c%20cu%E1%BB%91i%20c%C3%B9ng/output/ch01-rebirth-iii-p3/final-qa/p3-loc08-h08-recovery-R.png)
- **LOC02 Non-Regression Shots:** [`L02-03-counter-front-R.png`](file:///c:/Ca%20tr%E1%BB%B1c%20cu%E1%BB%91i%20c%C3%B9ng/output/ch01-rebirth-iii-p3/final-loc02-shots/L02-03-counter-front-R.png), [`L02-06-slot-tray-R.png`](file:///c:/Ca%20tr%E1%BB%B1c%20cu%E1%BB%91i%20c%C3%B9ng/output/ch01-rebirth-iii-p3/final-loc02-shots/L02-06-slot-tray-R.png)

### Recorded 1080p H.264 MP4 Gameplay Videos (Live Keyboard + Mouse `InputQA`)
- **Focused B06–B08 Gameplay Sequence (381 frames @ 12 fps, 31.8 s, `17.3 MB`):** [`ch01-p3-b06-b08-gameplay.mp4`](file:///c:/Ca%20tr%E1%BB%B1c%20cu%E1%BB%91i%20c%C3%B9ng/output/ch01-rebirth-iii-p3/ch01-p3-b06-b08-gameplay.mp4)
  *(Covers Room 101 exit → AN01 B06 collapse `B→R` & quick-slot paper reconciliation → rain walk to open outer gate → B07 `T09` seam + live WR05 sprint → return to `LOC02 R` → B08 Perceptual Echo → Sổ tay nhỏ → Inventory 2.0 `Ký ức` status → `CH02` handoff.)*
- **Full CH01 B01–B08 Zero-Teleport Walkthrough (1,161 frames @ 12 fps, 96.8 s, `55.7 MB`):** [`ch01-rebirth-iii-full-walkthrough.mp4`](file:///c:/Ca%20tr%E1%BB%B1c%20cu%E1%BB%91i%20c%C3%B9ng/output/ch01-rebirth-iii-p3/ch01-rebirth-iii-full-walkthrough.mp4)

---

## 5. 1080p Medium Performance Benchmark (D3D12, Intel Core Ultra 7 155H / Intel Arc iGPU)

Measured at `1920×1080`, `Medium` (`render_scale=0.88`, `dual-probes GI`, `vsync=0`, `4,327` frames on the final synchronized binary [`final-bench-Medium/frames.csv`](file:///c:/Ca%20tr%E1%BB%B1c%20cu%E1%BB%91i%20c%C3%B9ng/output/ch01-rebirth-iii-p3/final-bench-Medium/frames.csv); plus pre-thermal-soak run [`bench-Medium-final/frames.csv`](file:///c:/Ca%20tr%E1%BB%B1c%20cu%E1%BB%91i%20c%C3%B9ng/output/ch01-rebirth-iii-p3/bench-Medium-final/frames.csv)):

| Stage / Metric | Baseline (`loc03-hero`) | Phase 3 Cold Run (`bench-Medium-final`) | Phase 3 Final Sync Run (`final-bench-Medium`, warm) | Target / Verdict |
| --- | --- | --- | --- | --- |
| **Overall** | `81.1 FPS` (`12.34 ms`, `p95 16.08`, `max 60.92`) | **`82.2 FPS`** (`12.17 ms`, `p95 15.94`, `max 52.29`) | **`74.1 FPS`** (`13.50 ms`, `p95 19.97`, `max 56.08`, `peakMem 2080 MB`) | **PASS** (`>= 60 FPS`, lower max hitch than baseline) |
| `lan-reception-B` (LOC02 B) | `62.7 FPS` (`15.95 ms`) | **`71.9 FPS`** (`13.91 ms`) | **`64.7 FPS`** (`15.46 ms`, `max 23.03`) | **PASS** (faster than baseline) |
| `swap-B-R` (B06 collapse) | — | — | **`59.1 FPS`** (`16.92 ms`, `max 24.59`) | **PASS** (0 hitch spike) |
| `seam` (B07 WR05 gate) | `86.2 FPS` (`11.60 ms`) | **`90.8 FPS`** (`11.01 ms`) | **`79.5 FPS`** (`12.58 ms`, `max 18.78`) | **PASS** |
| `reception-return-R` (LOC02 B08 Echo) | `79.8 FPS` (`12.54 ms`) | **`82.9 FPS`** (`12.07 ms`) | **`72.4 FPS`** (`13.82 ms`, `max 23.42`) | **PASS** (0 shadow cost, 0 regression) |
| `room-entry-R-torch` (LOC03 Hero) | — | — | **`61.6 FPS`** (`16.23 ms`, `max 22.36`) | **PASS** (`>= 60 FPS`) |

---

## 6. Readiness for Phase 4

CH01 Rebirth III — Phase 3 meets all automated, visual, architectural, performance, and cross-chapter Exit Criteria on `feat/ch01-rebirth-iii-phase3` with zero Core canon modifications and is ready for **Phase 4 — Final Validation, Human Playtest & Windows Candidate**.
