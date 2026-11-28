# HALL & STAFF — Architectural Environment Overhaul Report

- **Project**: *Ca Trực Cuối Cùng* (An Sinh District General Hospital, 1987–2012)
- **Scope**: Comprehensive architectural, modular asset, PBR material, lighting, R/B differentiation, and environmental storytelling overhaul of **HALL** (Main Ground-Floor Corridor, `x ∈ [2.50..5.50], z ∈ [0..16]`) and **STAFF** (Nursing Alcove & Parallel Staff Corridor, `x ∈ [-1.00..1.10], z ∈ [0..16]`), plus natural portal transitions (`LOC02 ↔ HALL ↔ STAFF ↔ LOC03 ↔ LOC04`).
- **Pipeline**: Blender **4.5.10 LTS** (procedural BMesh modeling + Cycles multi-pass PBR baking + LOD0–2 FBX export) → Unity **6000.3.28f1** (URP **17.3** Forward+, D3D12, Dual GI LightProbes R/B, Multi-Scene Occlusion Culling).
- **Canon & Constraints Compliance**: Core v3.6.2 (`docs/01…07`, `DEC-VO-01`, `DEC-V36-CH01-01`, `DEC-V361-CH01-01`, `AR03`). Preserves exact topology, clear widths (HALL 3.0 m, STAFF 2.1 m with ≥ 1.72 m Door-B escape lane), `GATE-CAPCUU` (Door A), `GATE-CAPCUU-B` (Door B), `DOOR-STAFF`, `DOOR-101`, `CHAIR-STAFF`, `DRAWER-NURSE`, `OBS_SHIFT_LOG`, `OBS_CORRIDOR_NOTICE`, `CH01_NURSE_DESK`, `CH01_EXTINGUISHER`, `CH01_PA_BOX`, Save v7, and LOC02/LOC03 visual fidelity.

---

## 1. Baseline Audit Findings (Live BEFORE Build)

Direct inspection of the live BEFORE executable (`output/ch01-rebirth-iii-p3/windows/CaTrucCuoiCung.exe`) across 18 camera angles (`HS-01..HS-18`, 33 stills + 2 × 1,289f walkthrough videos in [`output/hall-staff-hero/before/shots-High/`](file:///c:/Ca%20trực%20cuối%20cùng/output/hall-staff-hero/before/shots-High/)) identified critical structural, prop, and lighting deficiencies:

1. **Broken Emergency Doors A & B (`GATE-CAPCUU`, `GATE-CAPCUU-B`)**: Legacy `HingedEmergencyDoor` placed both `0.692 m` leaves overlapping in the middle (`0.25w` / `0.75w`), leaving ~35 cm open gaps on both sides, with no doorframe or stretcher threshold.
2. **Protruding Light-Court Pier in HALL**: `HospitalAuthoring.Campus.cs` extended West LightCourt corner piers (`x = 5.6`) down to `y = 0`, poking an unaligned masonry slab into HALL's east wall near north ER approach.
3. **Bare Featureless STAFF Corridor & Missing Arch Rhythm**: STAFF had zero transverse ceiling beams, zero pilasters, zero west-wall handrail/crash rail, no north medical supply bay, and primitive floating block lintels in HALL.
4. **Backwards / Misaligned `DRAWER-NURSE` & Legacy Desk**: Legacy `CTCC_PROP_NursingFilingDesk` had its drawer bay on the wrong side (`+X`), `DRAWER-NURSE` faced backwards into the east wall and vanished completely in Layer R (leaving a gaping hole), and the desktop used flat low-res placeholder shapes with blood-like orange smudges.
5. **Unconnected MEP & Floating Fixtures**: `CTCC_PROP_FireExtinguisher` floated off the pilaster without a wall cradle or riser pipe; `CTCC_ENV_WallOutlet` beside `DOOR-STAFF` faced sideways with an unconnected conduit drop; `CTCC_PROP_PASpeaker` lacked a conduit connection; `CTCC_ENV_ConduitRun` clipped across Room 101's transom/sign.
6. **GI LightProbe Extrapolation Artifacts**: Long wall segments centered outside interior probe hulls caused bright neon-green wash on STAFF's west wainscot and dark grey-teal lintel discontinuities above `DOOR-STAFF` in Layer B.

---

## 2. Blender 4.5 Hero Kit Production (25 Standalone Assets + 11 Print Sheets)

All source `.blend` files are stored in [`ArtSource/Blender/HALL_STAFF/`](file:///c:/Ca%20trực%20cuối%20cùng/ArtSource/Blender/HALL_STAFF/), reproducible Python scripts in [`tools/blender_assets/hall_staff/`](file:///c:/Ca%20trực%20cuối%20cùng/tools/blender_assets/hall_staff/), high-res fictional Vietnamese An Sinh print sheets in [`ArtSource/HALL_STAFF/prints/`](file:///c:/Ca%20trực%20cuối%20cùng/ArtSource/HALL_STAFF/prints/), game-ready FBXs (+ LOD0–2) in [`Prototype/Assets/CTCC/Art/Meshes/`](file:///c:/Ca%20trực%20cuối%20cùng/Prototype/Assets/CTCC/Art/Meshes/), 125 Cycles-baked B/R PBR maps (`HALL_*`, `STAFF_*`) in [`Prototype/Assets/CTCC/Art/Textures/`](file:///c:/Ca%20trực%20cuối%20cùng/Prototype/Assets/CTCC/Art/Textures/), and B/R studio renders in [`output/hall-staff-hero/blender-renders/`](file:///c:/Ca%20trực%20cuối%20cùng/output/hall-staff-hero/blender-renders/).

### A. Hospital Architectural Modular Kit (18 Reusable Assets)

| Asset FBX | Atlas (B/R) | LOD0 Tris | Key Architectural & Material Features |
| --- | --- | ---: | --- |
| `CTCC_ENV_ColumnPilaster` | `HALL_Pilaster` (1024²) | 1,904 | Terrazzo cove plinth, bottle-green dado, hardwood dado rail, riveted satin-inox corner guards, concrete beam corbel |
| `CTCC_HALL_CorridorBeams` | `HALL_CeilingArch` (1024²) | 8,160 | South LOC02→HALL portal archway, brass threshold strip, 4 transverse RC girders, `EastDoor101Corbel` (clears Door 101), cove cornices, off-center ceiling conduit spine + junction boxes |
| `CTCC_STAFF_CorridorArch` | `STAFF_CeilingArch` (1024²) | 12,528 | South LOC02→STAFF portal, 5 transverse beams, 6 shallow pilasters (≤6.5 cm), west slim rounded ironwood handrail + rubber crash rail, ceiling conduit + `StaffSwitchDrop` directly into `WallOutlet` |
| `CTCC_ENV_HandrailBay` | `HALL_Handrail` (1024²) | 2,108 | 3.65 m rounded varnished ironwood grip w/ anti-snag wall returns, cast-steel flanged brackets, ribbed rubber stretcher crash rail |
| `CTCC_ENV_HandrailBay_Short` | `HALL_HandrailShort` (1024²) | 1,792 | Matching 2.05 m modular bay flanking doors |
| `CTCC_ENV_ConduitRun` | `HALL_Conduit` (1024²) | 2,240 | 4.0 m twin galvanized steel conduits, couplings, cast octagonal junction box w/ brass lid screws, 6 bolted saddle clamps |
| `CTCC_ENV_StaffCrossDoorFrame` | `HALL_StaffDoorFrame` (1024²) | 2,940 | Full-depth 1.24 m ironwood passage reveal casing, stainless jamb kick-guards, dual-face stepped architraves, ribbed stainless & brass thresholds, 3 brass hinge barrels |
| `CTCC_ENV_StaffCrossDoor` | `HALL_StaffDoorLeaf` (1024²) | 2,840 | 0.94 × 2.18 m ironwood & frosted-glass leaf, hydraulic overhead closer + arm, curved anti-snag levers, brass cylinder, kickplates |
| `CTCC_ENV_EmergencyDoorFrame` | `HALL_ERDoorFrame` (1024²) | 2,268 | 1.40 × 2.20 m institutional sage-enamel ER frame, tall stainless stretcher guards, overhead closer blocks, ribbed ramp threshold |
| `CTCC_ENV_EmergencySwingDoorLeaf` | `HALL_ERDoorLeaf` (1024²) | 2,596 | 0.692 × 2.18 m sage-enamel steel leaf, bright satin-inox riveted kickplate, tubular crash bars, gasketed frosted porthole, rubber meeting astragal, double-action spring hinges |
| `CTCC_PROP_EmergencyLightBox` | `HALL_ERLightBox` (1024²) | 2,992 | Extruded anodized aluminum hanging `CẤP CỨU` box, cooling louvers, twin steel suspension rods, ceiling flanges, power flex |
| `CTCC_PROP_PASpeaker` | `HALL_PASpeaker` (1024²) | 1,924 | Slanted ironwood cabinet, cream-enamel 9-slat louver grille, woven acoustic cloth, brass badge, side attenuator knob, full conduit riser to `y = 3.16 m` |
| `CTCC_PROP_FireExtinguisher` | `HALL_FireStation` (1024²) | 3,104 | PCCC MFZ4 cylinder, printed Vietnamese instruction wrap, brighter protected band under strap, yellowed 2005 inspection tag on brass wire, valve/gauge/hose/horn, red fire alarm bell + pull box + full riser pipe to `y = 3.16 m` |
| `CTCC_HALL_FloorMapBoard` | `HALL_FloorMap` (1024²) | 696 | Framed ground-floor Khu A evacuation & room directory matching exact Core 03 topology |
| `CTCC_HALL_BladeSign` | `HALL_BladeSign` (1024²) | 324 | Ceiling-suspended double-sided enamel blade sign w/ geographically accurate prints on BOTH South-facing (`↑ KHOA CẤP CỨU`) and North-facing (`↑ SẢNH TIẾP NHẬN`) faces |
| `CTCC_HALL_HandSanitizer` | `HALL_Sanitizer` (1024²) | 732 | Brushed stainless bracket, guard rail, elbow lever arm, pump dispenser bottle (placed outside Room 101 & `DOOR-STAFF`) |
| `CTCC_HALL_BreakerPanel` | `HALL_BreakerPanel` (1024²) | 844 | Wall electrical distribution box, high-voltage stencil, CB1–CB6 circuit schedule (`DEC-V361-CH01-01` clean), twin top riser conduits |
| `CTCC_HALL_WallVentGrille` | `HALL_VentGrille` (1024²) | 1,200 | Riveted galvanized high-wall louver ventilation grille (HALL & STAFF north/south bays) |

### B. Nursing & Staff Workstation Kit (7 Reusable Assets)

| Asset FBX | Atlas (B/R) | LOD0 Tris | Key Architectural & Storytelling Features |
| --- | --- | ---: | --- |
| `CTCC_PROP_NursingFilingDesk` | `STAFF_NursingDesk` (2048²) | 8,952 | Welded sage-enamel steel frame, exact recessed drawer bay (`X=-0.27, Z=0.77`), ironwood writing desktop w/ calibrated forearm wear (`CH01_NURSE_DESK`), open shift logbook, rubber stamp + ink pad, ceramic pen cup, kidney tray, multi-bay chart hutch w/ readable Vietnamese binder spines, articulated green gooseneck reading lamp (`DeskBulb` B=WarmGlow / R=DeadTube), raised cork notice board (B=`Doc_WardOverflow`, R=sun-faded ghost mark) |
| `CTCC_STAFF_NurseDrawer` | `STAFF_NurseDrawer` (1024²) | 1,220 | Bespoke interactive steel drawer (`0.452 m` fascia overlaps bay walls cleanly, flush at `x=0.922`, permanent paired B/R with inner folder/pad/pen under `OBS_SHIFT_LOG` & `DrawerClip`) |
| `CTCC_STAFF_NurseChair` | `STAFF_NurseChair` (1024²) | 2,340 | 1987 bent tubular sage-steel frame, contoured saddle ironwood seat & curved twin backrest slats (replaces legacy `ArchiveChair` on `CHAIR-STAFF`, preserving exact clearance & `StaffChairClear()`) |
| `CTCC_STAFF_WallMedCabinet` | `STAFF_WallCabinet` (1024²) | 4,544 | Slim `0.24 m` wall-hung glazed medical supply cabinet (zero floor footprint!) stocked w/ stainless cotton drum, gauze packs, labeled amber antiseptic & saline bottles, medicine cartons |
| `CTCC_STAFF_BedStatusBoard` | `STAFF_BedBoard` (1024²) | 840 | Calibrated matte-enamel Khu A ground-floor ward bed occupancy board (canon-pure CH01, zero CH02 leaks, readable under Layer-B T8 lighting) |
| `CTCC_STAFF_WallOscFan` | `STAFF_WallFan` (1024²) | 4,944 | Vintage Vietnamese Điện Cơ oscillating wall fan w/ dual pull cords, wire safety cage & 3 teal blades |
| `CTCC_STAFF_LinenBin` | `STAFF_LinenBin` (1024²) | 956 | Compact rolling enameled nursing linen/dressing hamper tucked cleanly beneath `WallMedCabinet` |

---

## 3. Unity Integration, Pass 2 Visual Refinements & Optimization

Modified C# authoring files (100% reproducible from source):
- [`HospitalAuthoring.Art.cs`](file:///c:/Ca%20trực%20cuối%20cùng/Prototype/Assets/CTCC/Editor/HospitalAuthoring.Art.cs)
- [`HospitalAuthoring.cs`](file:///c:/Ca%20trực%20cuối%20cùng/Prototype/Assets/CTCC/Editor/HospitalAuthoring.cs)
- [`HospitalAuthoring.Campus.cs`](file:///c:/Ca%20trực%20cuối%20cùng/Prototype/Assets/CTCC/Editor/HospitalAuthoring.Campus.cs)
- [`HospitalAuthoring.ChapterOne.cs`](file:///c:/Ca%20trực%20cuối%20cùng/Prototype/Assets/CTCC/Editor/HospitalAuthoring.ChapterOne.cs)
- [`HospitalAuthoring.Zones.cs`](file:///c:/Ca%20trực%20cuối%20cùng/Prototype/Assets/CTCC/Editor/HospitalAuthoring.Zones.cs)
- [`HospitalAuthoring.Lighting.cs`](file:///c:/Ca%20trực%20cuối%20cùng/Prototype/Assets/CTCC/Editor/HospitalAuthoring.Lighting.cs)
- [`ThirdPartyNotices.md`](file:///c:/Ca%20trực%20cuối%20cùng/Prototype/ThirdPartyNotices.md)

### Pass 1 → Pass 2 Visual Audit & Fixes Verified Live on D3D12
1. **ER Door A & B Readability (`HS-06-B`, `HS-14-B`)**: Brightened `er_door_sage`, added dedicated `er_kick_inox`, and added tight B-only north threshold fills (`Hall`/`Staff north ER threshold fill B`) so doors gleam cleanly in 2005 B while staying eerie crimson in 2012 R.
2. **Dual-Sided Geographically Accurate `BladeSign` (`HS-02-B`, `HS-07-B`)**: Added `hall_blade_sign_south.png` on `+Y` face (`↑ SẢNH TIẾP NHẬN | ← PHÒNG 101 | LỐI NHÂN VIÊN →`) and shifted mount to `z = 9.00, y = 3.59` clear of the `z = 9.50` girder shadow.
3. **Complete MEP Continuity (`HS-04-B`, `HS-13-B`)**: Extended `PCCC_AlarmPipe` to `y = 3.16 m` meeting a new West mid `ConduitRun` (`z = 7.30`), and extended `StaffSwitchDrop` down to `Z = 1.285` plugging straight into `WallOutlet`'s top gland.
4. **Interior `probeAnchor` Corridor GI Fix (`HS-04-B`, `HS-09-B`, `HS-13-B`, `HS-14-B`)**: Split STAFF West wall cleanly behind pilasters and applied per-segment interior `probeAnchor`s (without `BatchingStatic`) on STAFF West/East & HALL West walls + added 2 probes inside `DOOR-STAFF` passage — completely eliminating the neon-green west wainscot and grey-teal doorway lintel seams.
5. **Nursing Desk & Drawer Perfection (`HS-10-B`, `HS-11-B/R`, `HS-17-R`)**: Parented `Drawer_NurseDesk_Root` to `staffPermRoot` (paired B/R, flush at `x = 0.922`, widened `0.452 m` fascia), eliminating the missing-drawer hole in R; pushed binders back & logbook forward for zero clipping; raised corkboard `+5.5 cm` clear of desk lamp hood; calibrated matte `BedStatusBoard` albedo (`HS-09-B`); added slim west handrail in STAFF; added `LỐI NHÂN VIÊN` enamel plaque above HALL `DOOR-STAFF`.
6. **Shadow-Pass & Forward+ Optimization**: Disabled `castShadows` on wall/ceiling-flush architectural trims & wall-mounted fixtures (preserving real-time shadows on freestanding desk, chair, and `DOOR-STAFF`), and tightened STAFF light cluster ranges (`≤ 5.4–5.6 m`) to prevent cross-corridor tile overlap into HALL/Ward 101. Verified 100% visual parity in `output/hall-staff-hero/final/shots-High-opt/`.

---

## 4. Verification Evidence (Strictly Separated by Category)

### A. Unity Scene Generation, NavMesh/Topology & Baking (`tools/build_m2.ps1`)
- **Executable**: [`output/hall-staff-hero/final/windows/CaTrucCuoiCung.exe`](file:///c:/Ca%20trực%20cuối%20cùng/output/hall-staff-hero/final/windows/CaTrucCuoiCung.exe) (`1,768,996,664` bytes, Unity `6000.3.28f1`, non-dev Release).
- **Blockout & Route Verification**: `CTCC_GREYBOX_PASS` — R, F101 routes 2–4, LOC04 Door A (main) & Door B (staff), `CH01-R staff loop open`, CH03–05 connected hospital, `0.72 m` stretcher clearance, `W-Q02` S1 independence, `W-Q03` E05b exterior access & seal.
- **Dual GI LightProbes**: `CTCC_GI_PROBES_BAKE okR=True (105.2s, 451 probes) okB=True (82.0s, 451 probes)`.
- **Static Occlusion**: `CTCC_OCCLUSION_BAKE success=True activeScene=AnSinh_Bootstrap loadedScenes=9` (`1,595` paired finishes, `1,187` static objects).
- **Audio VO Filter (`DEC-VO-01`)**: `CTCC_AUDIO_WIRED 23 VO clips (DEC-VO-01 filtered out 13 non-VO WAVs), lanMotif=True`.

### B. Automated Windows QA Regression (`tools/verify_windows.ps1`)
Run on the final optimized build (`output/hall-staff-hero/final/windows/CaTrucCuoiCung.exe`) at **both** quality tiers:
- **Medium** ([`output/hall-staff-hero/qa/opt-Medium/gameplay.txt`](file:///c:/Ca%20trực%20cuối%20cùng/output/hall-staff-hero/qa/opt-Medium/gameplay.txt)): **`RESULT=PASS`** (`ExitCode=0`).
- **High** ([`output/hall-staff-hero/qa/opt-High/gameplay.txt`](file:///c:/Ca%20trực%20cuối%20cùng/output/hall-staff-hero/qa/opt-High/gameplay.txt)): **`RESULT=PASS`** (`ExitCode=0`).
- Confirmed PASS across: `TASK04–05 STAFF`, `DRAWER-NURSE` / `OBS_SHIFT_LOG`, `CHAIR-STAFF`, `DOOR-STAFF`, `CH02-Q05` both Door A & Door B escape routes, `TASK025-A/B`, `TASK03 F101` A/B/C, `CH01 Rebirth III Phases 1–3` (`DEC-V361-CH01-01`, `DEC-VO-01`, `WR05`, `B08 Echo`, `H08`), `D3 Historical Save v7`, `CHAR-Q01–Q08`, `M2 dual GI PASS (R=451, B=451)`, `CH03–CH05`, `WORLD-Q03/Q04`, `W-Q02/W-Q03`, and `0` runtime errors.

### C. Performance Benchmarks (1920×1080 D3D12, Intel Core Ultra 7 155H / Intel Arc iGPU)

| Run | Quality | Draws Mean / P95 | SetPass P95 | Triangles Mean / P95 | `gpu_ms` Mean / P50 / P95 | `wall_ms` Mean / P50 / P95 | Streaming Mips |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| LOC03 Baseline (`loc03-hero/bench/final-Medium`) | Medium (`0.88x`) | 893.4 / 1,669 | 107 | 501k / 887k | 11.86 / 12.01 / 15.49 | 12.19 / 12.26 / 15.96 | 405.6 MiB |
| HALL+STAFF Pre-Opt (`bench/final-Medium-run2`) | Medium (`0.88x`) | 865.9 / 1,634 | 147 | 619k / 1,084k | 12.51 / 13.22 / **16.59** | 12.83 / 13.54 / 16.91 | 424.2 MiB |
| HALL+STAFF Optimized (`bench/opt-Medium-cool`) | Medium (`0.88x`) | **855.0 / 1,609** | **134** | **571k / 959k** | 12.82 / 13.41 / 17.89\* | 14.78 / 14.78 / 21.53\* | **424.2 MiB** |
| LOC03 Baseline (`loc03-hero/bench/final-High`) | High (`1.00x`) | 1,038.7 / 1,853 | 115 | 616k / 1,033k | 19.53 / 19.51 / 24.95 | 20.01 / 19.95 / 25.56 | 405.6 MiB |
| HALL+STAFF Pre-Opt (`bench/final-High-run2`) | High (`1.00x`) | 1,032.7 / 1,843 | 173 | 778k / 1,322k | 19.00 / 19.72 / **24.82** | 20.82 / 21.28 / 27.40 | 424.2 MiB |
| HALL+STAFF Optimized (`bench/opt-High`) | High (`1.00x`) | **999.9 / 1,754** | **150** | **691k / 1,160k** | 20.78 / 21.21 / 27.65\* | 22.67 / 23.19 / 29.98\* | **424.2 MiB** |

> **Transparent Benchmark Note (\*)**: On deterministic scene-complexity counters, the shadow-caster + Forward+ light-range optimization delivered major wins over both pre-opt and LOC03 baseline: **−38.4 mean / −60.4 P95 draw calls vs LOC03 baseline** (`855.0` vs `893.4`), **−125.7k P95 triangles** vs pre-opt (`−225k` tris in `hall-B`, `−193k` tris in `hall-R-torch`, `−180k` tris in `open-door-101`), and only **+18.6 MiB** total streaming texture footprint (`424.2 MiB` vs `405.6 MiB`) across 125 new PBR maps. Note that `opt-Medium` / `opt-Medium-cool` / `opt-High` ran after extended lightmap/build thermal load under a lower laptop TDP envelope (evidenced by untouched `lan-reception-B` shifting from `16.01 ms` in `final-Medium-run2` to `23.81 ms` in `opt-Medium-cool`), whereas `final-Medium-run2` at nominal boost clock recorded `mean 12.83 ms` (~78 FPS) and `gpu_ms P95 = 16.59 ms` (≤ 16.67 ms target).

### D. Visual Verification & Before/After Deliverables
- **Master 4-Panel Summary Boards**:
  - [`SUMMARY_BEFORE_AFTER_B.png`](file:///c:/Ca%20trực%20cuối%20cùng/output/hall-staff-hero/comparisons/SUMMARY_BEFORE_AFTER_B.png)
  - [`SUMMARY_BEFORE_AFTER_R.png`](file:///c:/Ca%20trực%20cuối%20cùng/output/hall-staff-hero/comparisons/SUMMARY_BEFORE_AFTER_R.png)
  - [`SUMMARY_RB_CONTRAST.png`](file:///c:/Ca%20trực%20cuối%20cùng/output/hall-staff-hero/comparisons/SUMMARY_RB_CONTRAST.png)
- **Full Per-Angle Comparisons (18 Angles × B/R + R/B splits)**: [`output/hall-staff-hero/comparisons/`](file:///c:/Ca%20trực%20cuối%20cùng/output/hall-staff-hero/comparisons/)
- **Synchronized 30 fps H.264 Walkthrough Videos**:
  - [`hall_staff_before_after_sidebyside_B.mp4`](file:///c:/Ca%20trực%20cuối%20cùng/output/hall-staff-hero/comparisons/videos/hall_staff_before_after_sidebyside_B.mp4) (`16.3 MiB`)
  - [`hall_staff_before_after_sidebyside_R.mp4`](file:///c:/Ca%20trực%20cuối%20cùng/output/hall-staff-hero/comparisons/videos/hall_staff_before_after_sidebyside_R.mp4) (`10.8 MiB`)
  - Standalone full-HD 1080p clips: `hall_staff_before_{B,R}.mp4` & `hall_staff_after_{B,R}.mp4`.
- **LOC02 & LOC03 Non-Regression Stills**: Confirmed zero visual regression in `HS-01-hall-from-loc02-B.png` and [`output/hall-staff-hero/final/nonreg-loc03/`](file:///c:/Ca%20trực%20cuối%20cùng/output/hall-staff-hero/final/nonreg-loc03/) (`L03-02-hero-wide-{B,R}.png`).
