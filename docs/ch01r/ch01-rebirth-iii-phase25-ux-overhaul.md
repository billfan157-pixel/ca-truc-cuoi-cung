# CH01 REBIRTH III — PHASE 2.5: Immersive Interaction & Diegetic UX Overhaul

- **Branch:** `feat/ch01-rebirth-iii-phase25-diegetic-ux` (branched from `main` at `0b5b0a9`, **not merged into `main`**)
- **Baseline & Canon:** Core v3.6.2 Single Source of Truth (`docs/01..07` + `docs/dialogue_silent_exploration_runtime.json` untouched, `CORE_DOCS_DIRTY_COUNT=0`), `DEC-VO-01`, `DEC-V36-CH01-01`, `DEC-V361-CH01-01`, D1–D7
- **Build Artifact:** `output/ch01-rebirth-iii-p25/windows/CaTrucCuoiCung.exe` (`1,080,839,752 bytes`, Unity `6000.3.28f1`, URP 17.3 Forward+, D3D12)
- **Assembly SHA-256:** `45c348ae93d6ac89f5f656833883146814ada9101dea6b4430e2b28069962c50`

---

## 1. Executive Summary & Audit Findings

Phase 2.5 eliminates the core UX debt identified after Phase 1 (`21b8add`) and Phase 2 (`0b5b0a9`) — **Menu-first UX disguised as 3D** — and converts *Ca Trực Cuối Cùng* to a **World-first, Physical-first, Diegetic-first, Minimal HUD, Player-driven Investigation** architecture while keeping 100% of backend contracts and epistemic safeguards intact.

### Key Root Causes Found in Pre-Phase 2.5 Audit
1. **Center-screen HUD Clutter:** `HospitalUI` rendered a `760×54` text prompt near center screen plus permanent controls/objective strips, spoiling world objects from afar and covering 3D props.
2. **Oblique-Angle Document Blur (4-Layer Root Cause):**
   - `PC_RPAsset.asset` (Medium default) ran `m_RenderScale = 0.88`, downsampling crisp text strokes before screen presentation.
   - `PhysicalInspector` enabled macro **Bokeh Depth of Field** (`focusDistance = 0.42m`, `aperture = 3.2`, `focalLength = 52mm`), whose thin ~2.5 cm circle-of-confusion plane blurred the top and bottom edges of any paper tilted 20°–35° toward or away from the camera.
   - `HospitalAuthoring.Art.cs` imported document textures as `Bilinear`, `anisoLevel = 8`, `mipMapBias = 0`, `Compressed` (BC1/DXT1), softening slanted glyphs at oblique grazing angles.
   - Player flashlight stayed active during close-up 3D inspection, washing out matte archival paper contrast.
3. **Menu-Wrapped 3D Interactions:**
   - Envelope inspection used tabbed `ActionChoice` dialogs (`Giấy nhắn` / `Thư Lan` / `Mẩu báo` / `Chìa C` / `SMS`) and coupled Minh's personal phone to the envelope flow.
   - Backpack used a Notebook-style multi-tab layout with long description paragraphs and embedded Pocket Note.
   - Room 101 `F101` / `AN02` still opened a 2D `ui.Show` sidebar beside the 3D wall clipboard.
4. **LOC01 Institutional Gate Scale Defect:** Outer gate collider was `1.80 m` tall against a `1.62 m` player eye height, making the hospital entrance feel waist-high/residential; `Sign_NightGate` clipped into the gateway opening.
5. **Player-Facing Text & Stationery Leaks:**
   - `tools/bake_ch01r_documents.py` baked artist/production notes onto the back of Lan's 2005 letter, raw control keybind hints onto the phone screen texture, and a date inconsistency (`19/11` newspaper referencing future framing).
   - `Ch01FieldObjects.cs` passed raw internal location IDs (`LOC01`, `LOC02`, `LOC03`, `HALL`, `STAFF`) directly into `ObservationSpec.ProvenanceLabel`, and `DeductionFramework.cs` exposed `PZ01` in CH02 implication text.
   - Runtime UI labels still referenced `"Pocket Note"` / `"WCAG"` / `"Vertical Slice"` in places.

---

## 2. Requirements A–I Implementation Architecture

### A. Universal Interaction System
- **Primary Files:** `Prototype/Assets/CTCC/Runtime/HospitalGame.cs`, `HospitalGame.World.cs`, `FirstPerson.cs`, `HospitalGame.InputQA.cs`
- **Design:**
  - Added **Left Mouse Button (`LMB`)** as the primary world interaction input alongside **`E` / `K`** accessibility parity.
  - Unified click vs. hold-drag disambiguation (`0.16 s` / `10 px` threshold) so single-clicking toggles/inspects while holding + dragging manipulates physical doors, drawers, curtains, and the mailbox lid smoothly without fighting look controls.
  - Explicit `InputContext` state machine (`WorldExploration`, `WorldDrag`, `PhysicalInspect`, `DiegeticEnvelope`, `DiegeticPhone`, `DiegeticAn02`, `Inventory`, `PocketNote`, `ModalMenu`) with cursor-lock transition guards (`waitForLmbRelease`, `RefreshControls()` immediate `InputBlocked` sync across all 4 3D subsystems) preventing click-through bleed across modal boundaries.
  - Preserved all reach, line-of-sight, and `RealityTransaction` guards.

### B. Minimal Contextual HUD
- **Primary Files:** `Prototype/Assets/CTCC/Runtime/HospitalUI.MinimalHud.cs`, `HospitalUI.cs`, `HospitalGame.cs`
- **Design:**
  - Retired the `760×54` center prompt banner and permanent HUD control clutter during free exploration.
  - Replaced with a **LOS-occluded, world-projected diegetic marker** (`Dot` / `Ring` / `?` / `↔` / `Lock`) anchored cleanly to the targeted physical prop or hotspot, revealing short bottom-edge feedback only when contextually needed (e.g., locked gate, wrong key).
  - Never pierces walls or reveals investigation clues/spoilers from afar.

### C. Physical Inspector 2.0 & Oblique-Angle Zero-Blur Pipeline
- **Primary Files:** `Prototype/Assets/CTCC/Runtime/InspectionSharpnessGuard.cs`, `PhysicalInspector.cs`, `HospitalAuthoring.Art.cs`, `Prototype/Assets/CTCC/Art/InspectPaperRaking.shader`
- **Design:**
  - Default 3D inspection runs **standalone with zero sidebar** (`ui.ShowStandaloneOverlay`), letting players directly rotate (drag / `WASD`), flip (`F` / RMB), zoom (scroll), tilt for raking light (`R`), and click reusable 3D projected hotspots (`ProjectedHotspot3D`) directly on the object surface.
  - **Oblique-Angle Blur Elimination (`InspectionSharpnessGuard` — refcounted across Inspector, Envelope, Phone, Backpack3D, and AN02):**
    1. Temporarily promotes URP `renderScale` to native **`1.0×`** during 3D inspection and restores the exact user tier scale (`0.88×` Medium / `0.76×` Low) on release.
    2. Replaces shallow macro Bokeh DoF with **far-only Gaussian DoF (`gaussianStart ≥ 1.25 m`, `gaussianEnd = 4.2 m`)** (and completely disables DoF under Reduced Motion), guaranteeing the entire held 3D prop (`0.22–0.55 m`) stays 100% outside the blur curve at every oblique tilt angle (0°–45°).
    3. Suppresses the player flashlight (`player.TorchSuppressed = true`) and switches to calibrated inspection key + rim + raking lighting.
    4. Upgraded all document textures (`Doc_*`, `F101*`, `ObsCard*`, `Calendar2005*`, `Sign_*`) in `HospitalAuthoring.Art.cs` to **`FilterMode.Trilinear`**, **`anisoLevel = 16`**, **`mipMapBias = -0.5`**, **`CompressedHQ` (BC7)**, and **`NPOTScale.None`**.
  - **Accessibility:** Large Print 130% (`[L]`) remains available on demand without cluttering the default physical view.
  - **Visual Proof:** Verified razor-sharp at 0° (`p25-inspector-0deg-sharp.png`), 32° oblique tilt (`p25-inspector-32deg-oblique-sharp.png`), and raking-light embossed `03g17` (`p25-inspector-raking-sharp.png`).

### D. Inventory & Backpack 2.0 + Decoupled Pocket Note
- **Primary Files:** `Prototype/Assets/CTCC/Runtime/HospitalUI.Backpack2.cs`, `BackpackStage3D.cs`, `HospitalGame.Backpack.cs`, `BackpackFramework.cs`
- **Design:**
  - Replaced the Notebook-layout inventory and page-flip copy with a commercial horror minimalist dark split view:
    - **Left 55%:** Interactive 360° rotatable, zoomable 3D turntable (`BackpackStage3D`) displaying actual Blender meshes (`CTCC_PROP_GateKey`, `CTCC_PROP_WingCKey`, `CTCC_PROP_MinhPhone`, `CTCC_PROP_EnvelopeInteractive`, physical sheets) under `InspectionSharpnessGuard`.
    - **Right 42%:** Compact dark visual tile grid (`Đồ vật` | `Giấy tờ` | `Sổ tay [N]` | `Đóng`) with clean Vietnamese typographic emblem pills (zero tofu glyphs) and direct `Kiểm tra` / `Cầm trên tay` actions.
  - Completely decoupled **`Sổ tay nhỏ` (`[N]`)** from Inventory 2.0 presentation and removed `"Pocket Note"` UI strings.
  - Zero artificial capacity, weight, or grid-Tetris constraints added.

### E. Physical Envelope 3D & Late-20th-Century Vietnamese Stationery Overhaul
- **Primary Files:** `ArtSource/Blender/CTCC_PROP_EnvelopeInteractive.blend`, `Prototype/Assets/CTCC/Art/Meshes/CTCC_PROP_EnvelopeInteractive.fbx`, `Prototype/Assets/CTCC/Runtime/DiegeticEnvelope3D.cs`, `HospitalGame.Envelope3D.cs`, `tools/bake_ch01r_documents.py`
- **Design:**
  - Eliminated tab-based and `ActionChoice` envelope flows. Players directly rotate the 3D envelope, click Chú Bảy's external tucked note (`EnvCard_BayNote`), click/open the 142° hinged top flap (`EnvelopeFlap`), and click Lan's 2005 folded letter (`EnvCard_LanLetter`), the 19/11/2005 newspaper (`EnvCard_Newspaper`), or the Wing-C key (`EnvSlot_WingCKey`) as they slide out above the open flap.
  - **Authentic Late-20th-Century Vietnamese Letter & Envelope Art Direction** (guided by the user's archival Vietnamese letter & envelope photo references):
    - `tools/bake_ch01r_documents.py` uses **`Palatino Linotype Italic` (`palai.ttf`)** with full 10/10 Vietnamese stacked diacritics, per-word baseline/tilt/ink-density micro-jitter (`draw_vietnamese_handwriting`), aged ivory paper grain, subtle quarter-fold crease shadows, circular An Sinh archival ink postmarks (`draw_circular_postmark`), faded blue sender/recipient lines, and a muted corner floral sprig (`draw_vintage_envelope_sprig`).
    - Regenerated and visually verified `Doc_LanLetter2005.png`, `Doc_LanLetter2005_Back.png` (subtle mirrored bleed-through + brief pencil note only), `Doc_BayEnvelopeFront.png`, and `Doc_BayNote.png`.
  - **E10a Epistemic Contract & Fault Probe:** Picking up the mailbox package (`0/4`) and opening the 3D flap (`0/4`) NEVER auto-read any inner document. Guarded by the new real negative fault-injection probe **`v36-p25-envelope-autoread`**. Zero phone coupling.

### F. Diegetic 2012 Mobile Phone 3D
- **Primary Files:** `ArtSource/Blender/CTCC_PROP_MinhPhone.blend`, `Prototype/Assets/CTCC/Art/Meshes/CTCC_PROP_MinhPhone.fbx`, `Prototype/Assets/CTCC/Runtime/DiegeticPhone3D.cs`, `HospitalGame.Phone.cs`
- **Design:**
  - Minh's personal 2012 bar phone is owned from frame 0 (`AP00`) with zero presentation dependency on the envelope.
  - 8-part Blender mesh with an on-demand **`512×640` 16-bit-depth `RenderTexture`** backlit monochrome-teal LCD (0.0 ms idle GPU cost, rendered only on state change, URP Lit + `_EmissionMap` safe against standalone shader stripping, pushed `1.2 mm` clear of bezel for zero z-fighting) and **7 clickable 3D hardware keypad buttons** (`Up`, `Down`, `OK`, `SoftL`, `Call`, `End`, `Back`).
  - Opening the Inbox stays strictly passive (`0` SMS flags auto-committed). Pressing `OK` / `Down` / `Call` directly on the 3D keypad commits `PostponementSms`, `SmsAppointmentViewed`, and `PhoneCalledBay` individually.

### G. Diegetic Direct-Surface F101 & AN02
- **Primary Files:** `Prototype/Assets/CTCC/Runtime/DiegeticAn02Surface.cs`, `HospitalGame.An02Clipboard.cs`
- **Design:**
  - Replaced the default 2D sidebar with direct 3D paper-surface interaction mounted on Room 101's West wall (shifted `+0.16 m` into the unobstructed wall bay beside the doorway with physical oak backing + metal spring clip while hiding the duplicate static wall F101 renderer during focus):
    - **Live F101 Sheet (`1024×1360` 16-bit-depth RT):** Direct click on `"0"` row draws a blue ballpoint highlight; equal-step neutral checkboxes (`A` / `B` / `C`, zero moral color bias); 2-step `N. H. Minh` ink signature + red shift-stamp confirmation directly on the paper; top-right corner lift (`[F]`) transitions to 2-sided 3D `PhysicalInspector`.
    - **Pinned 3D Margin / Branch-C Slip (`768×1000` 16-bit-depth RT):** Free 4-digit `HH:MM` pencil cells for `EX01` (clickable digits or direct `0–9` keyboard input, zero preset distractors), AS-05B receiver-box mark, read-only `SpatialFieldVerified` Bed-4 provenance display, and Branch C source/claim checkboxes.
  - Preserves `Esc` draft retention, revision rights, `Task02HardGateEnabled => false`, opt-in Large Print (`[L]`), and CH02+ Journal separation.

### H. Environment Scale (`LOC01` Institutional Hospital Gate)
- **Primary Files:** `ArtSource/Blender/CTCC_PROP_ExteriorGate.blend`, `CTCC_ENV_CourtyardFenceBay.blend`, `CTCC_PROP_GateMailbox.blend`, `tools/blender_assets/build_ch01_rebirth3_props.py`, `build_m2_architecture.py`, `Prototype/Assets/CTCC/Editor/HospitalAuthoring.Zones.cs`
- **Measurements (Verified by `P25-H` QA + `p25-loc01-gate-scale.png`):**
  - Outer gate leaf frame raised to **`2.48 m`** (`2.62 m` decorative spear finials), gate collider **`2.52 m`** (up from `1.80 m`).
  - Masonry gate pillars raised to **`2.88 m`** with a **`3.08 m`** reinforced overhead concrete lintel beam carrying the centered `Sign_HospitalGate` enamel plaque (`center.y ≥ 2.95 m`).
  - Courtyard perimeter fence bays raised to **`2.48 m`** matching the gate silhouette.
  - `Sign_NightGate` mounted flush on the West pillar (`max.x ≤ 2.815`, zero doorway clipping).
  - East-pillar iron mailbox synced cleanly preserving all D4 reach, clearance, and pivot invariants.
  - Player FPS eye height strictly locked at **`1.62 m`** (never lowered).

### I. Player-Facing Text & Narrative Hygiene
- **Primary Files:** `tools/bake_ch01r_documents.py`, `Prototype/Assets/CTCC/Runtime/Ch01FieldObjects.cs`, `DeductionFramework.cs`, `BackpackFramework.cs`, `HospitalUI.cs`, `HospitalUI.Backpack2.cs`, `HospitalGame.Pages.cs`, `HospitalGame.Phase25QA.cs`
- **Cleanup Executed & Verified:**
  - Stripped artist/material notes from `Doc_LanLetter2005_Back.png`, control keybind hints from `Doc_PhoneScreen.png`, and resolved the 19/11/2005 newspaper date inconsistency.
  - Replaced raw location IDs (`LOC01`, `LOC02`, `LOC03`, `HALL`, `STAFF`) in `Ch01FieldObjects.cs` player-facing `ProvenanceLabel` with natural Vietnamese (`Sân ngoài cổng`, `Quầy tiếp nhận`, `Phòng 101`, `Hành lang tầng trệt`, `Bàn điều dưỡng`) while keeping internal location IDs intact for backend provenance checks.
  - Removed internal token `PZ01` from CH02 `ObservationSpec.Implication` strings in `DeductionFramework.cs`.
  - Automated `P25-I` sweep enforces zero leakage of `{ E10a, OBS101, PZ01, PZ07, LOC01, LOC03, LOC09, LOC11, CH05-Q03, (34.5, 42), Vertical Slice, WCAG, Pocket Note }` across `BackpackCatalog`, `ObservationRegistry`, and `PocketNoteBuilder`.

---

## 3. Additional Technical Fixes Surfaced During Verification

1. **Standalone URP Shader Stripping:** `Shader.Find("Universal Render Pipeline/Unlit")` returned `null` in Windows standalone players. Switched `DiegeticPhone3D.cs` and `DiegeticAn02Surface.cs` to `Universal Render Pipeline/Lit` + `_EmissionMap` / `_EmissionColor` + `EnableKeyword("_EMISSION")`.
2. **URP 17 RenderGraph Depth Buffer Requirement:** Upgraded all 3 offscreen camera `RenderTexture` targets (`f101Rt`, `slipRt`, `lcdRt`) from `depth: 0` to `depth: 16`, reducing RenderGraph depth warnings in `player.log` from 15+ to **`0`**.
3. **Blender FBX `-90°` X Child Axis Orientation:** Mounted `DiegeticEnvelope3D`'s procedural front print & inner document quads directly under `envelopeInst.transform` in upright Unity space so Blender Z-up → Unity Y-up sub-mesh rotations never twist paper quads sideways.
4. **Immediate `InputBlocked` Sync:** Added `an02Surface.InputBlocked = busy || ui.ModalInputBlocked` in `HospitalGame.RefreshControls()` and `RenderAn02DirectSurface3D()` to eliminate a 1-frame stale lock after `SaveCheckpoint`.
5. **CharacterQA Clip-Duration Race:** Updated `HospitalGame.CharacterQA.cs` `VerifyForcedFaceLods` pause/resume test from `CH01-D03` (`1.07 s`) to verified lip-sync clip `CH01-D03c` (`6.92 s`) so 30-frame lip-sync allocation measurement + pause/resume never races past clip completion under heavy load (leaving `char-track` and `char-interruption` probes unchanged).
6. **Concurrent High-Tier Graphics Files Note:** The workspace also contained High-tier URP camera/renderer files (`HospitalGame.Graphics.cs`, `PC_High_Renderer.asset`, `PC_High_RPAsset.asset` referenced by `HospitalGame.cs:235` and `HospitalGame.Pages.cs:235`, Medium tier unchanged). These required runtime dependencies are preserved cleanly in the feature branch so fresh checkouts compile cleanly; the research note `docs/graphics/graphics-upgrade-research-v1.md` is left untouched outside Phase 2.5 commits.

---

## 4. Verification Matrix & Evidence (Build `45c348ae…`)

### Scene & Assembly Integrity
| Artifact | SHA-256 |
| --- | --- |
| `Assembly-CSharp.dll` | `45c348ae93d6ac89f5f656833883146814ada9101dea6b4430e2b28069962c50` |
| `AnSinh_Bootstrap.unity` | `3c1b312ce0e9e199e37e4cc10ed1365a083fbc8ecad95fdcfcd7b3900780e368` |
| `Campus_Grounds.unity` | `c3cc46c7355a5a45d40eec8ea5c62d7b4d84bb527d7ff06703140fa9d1d81ef4` |
| `Campus_Interiors.unity` | `e5e689113a4e0d6bdc94b80d3789edad88ffdc1d288dd5d1766a8c17ee11449e` |
| `LOC01_Exterior.unity` | `8c7d27b56155ee56d89a94b6124066cc70cce88165e46420796b86ccc8a0a222` |
| `LOC02_Reception.unity` | `0da065dc6f305794383f732293218174d11296afab026053892abe68e51e288a` |
| `LOC03_Ward101.unity` | `2bdafba54d2742379c7fc6dbe515d0479a88e910457c280ce3540d182807a841` |
| `LOC04_Emergency.unity` | `b341010b78f1406981bf98b282ac4155dc6589453343256b031112d388705f79` |
| `LOC05_ArchiveWing.unity` | `a3068eab3d48e5d4c2816615c3ea095eda3e3b7491e82f3b19698adb07f0dde1` |
| `LOC09_ServiceWing.unity` | `dfa1cd1c028470c7036120c6469ac0e287362d1d43929ad3775161a2de9809e7` |

### Verified Automated & Visual Gates (On Exact Final Build `45c348ae…`)
| Check | Status | Evidence |
| --- | --- | --- |
| Full Windows Module QA (37 suites + Phase 2.5 A–I + 100-cycle 5-system stress) | **PASS** (`RESULT=PASS`, exit `0`) | `output/ch01-rebirth-iii-p25/qa-module/gameplay.txt` |
| URP 17 RenderGraph Depth Warnings | **PASS** (`0` warnings) | `output/ch01-rebirth-iii-p25/qa-module/player.log` |
| Fault Probe `v36-t02-wrong-key` | **PASS** (exit `1` verified) | `output/ch01-rebirth-iii-p25/probe-v36-t02-wrong-key/gameplay.txt` |
| Fault Probe `v36-vo-leak` | **PASS** (exit `1` verified) | `output/ch01-rebirth-iii-p25/probe-v36-vo-leak/gameplay.txt` |
| Fault Probe `v36-ex01-autosolve` | **PASS** (exit `1` verified) | `output/ch01-rebirth-iii-p25/probe-v36-ex01-autosolve/gameplay.txt` |
| Fault Probe `v36-f101-unverified-link` | **PASS** (exit `1` verified) | `output/ch01-rebirth-iii-p25/probe-v36-f101-unverified-link/gameplay.txt` |
| Fault Probe `v36-p25-envelope-autoread` | **PASS** (exit `1` verified) | `output/ch01-rebirth-iii-p25/probe-v36-p25-envelope-autoread/gameplay.txt` |
| Performance Benchmark (`1920×1080`, `Medium`, `dual-probes`, `additive=True`, `loaded_zones=5`) | **PASS** (`BENCH_EXIT=0`, `passed=True`, `frames=2313`) | `output/ch01-rebirth-iii-p25/bench-final/result.txt` |
| Core Docs Structure & Integrity (`project_docs.py check`) | **PASS** (`CORE_DOCS_DIRTY_COUNT=0`) | 7 Core files, 28 sheets, 100-task DAG verified |
| Visual Inspection of 9/9 1080p Phase 2.5 Captures | **PASS** (inspected via `read_file`) | `p25-loc01-gate-scale.png`, `p25-minimal-hud.png`, `p25-phone-3d-lcd.png`, `p25-envelope-3d-open.png`, `p25-inventory2-3d.png`, `p25-inspector-0deg-sharp.png`, `p25-inspector-32deg-oblique-sharp.png`, `p25-inspector-raking-sharp.png`, `p25-an02-3d-surface.png` |

### Remaining Desktop / Human Sign-Off Gates (NOT Verified in Sandboxed Agent Shell)
- **Real Interactive Desktop `-PlayerInput` QA:** Requires running on the user's visible Windows desktop (`WinSta0\Default`) so OS keyboard/mouse `SendInput` events reach a foreground focused window without violating `Application.isFocused`.
  ```powershell
  Remove-Item -Recurse -Force output\ch01-rebirth-iii-p25\qa-input -ErrorAction SilentlyContinue; powershell -ExecutionPolicy Bypass -File tools\verify_windows.ps1 -Executable output\ch01-rebirth-iii-p25\windows\CaTrucCuoiCung.exe -Evidence output\ch01-rebirth-iii-p25\qa-input -PlayerInput -TimeoutSeconds 900
  ```
- **Human Playtest & Feel Review:** Subjective feel of LMB drag curves, 3D phone/envelope ergonomics, oblique paper readability in motion, LOC01 gate silhouette pacing, and PresentMon GPU/CPU profiling remain human sign-off gates before merging into `main`.
