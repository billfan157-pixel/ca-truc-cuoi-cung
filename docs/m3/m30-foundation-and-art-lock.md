# Ca trực cuối cùng — M3.0: Production Foundation & Art Direction Lock

Baseline commit: `c0531dcc8a3ed607e4a4b8cc692ce5dce44e282b` (M2-C.1 + VieNeu v3 Turbo audio foundation).  
Locked M3.0 RC build identity: `Unity 6000.3.28f1` (`URP 17.3.0 Forward+`, `D3D12`), `Blender 4.5.10 LTS` (`6dc0b208d1b5`), `assembly_sha256=f4c6bc91cc13d77d89960f1bc742cc68204eee52dd19cf470732fa04c642ab7f`, `CaTrucCuoiCung.exe` (`692,401,063` bytes).  
Full identity & scene hashes: [build-identity.txt](evidence/build-identity.txt).

> **Explicit Open Human Gates (NOT Declared Closed):**
> - **TASK010 (M2 Playtest):** 6 fresh players (`>= 5/6` comprehension across F101/R-B/G01-G02) — still open (`Chưa làm`).
> - **Art Direction Lock:** Human Art Director review & sign-off on Candidate B Lan + Bé An + Gatekeeper + CH03 Archive Wing — open (`Chờ duyệt`).
> - **TASK026 / TASK027 / TASK028:** Independent medical/architecture, historical fire/PCCC, and clinical provenance specialist reviews — still open (`Chờ duyệt`).
> - **Controlled 1080p/60 Hz PresentMon Re-Benchmark on `f4c6bc9`:** Blocked by Windows desktop lock (`GetForegroundWindow() == 0`) during the unattended run; requires an unlocked interactive desktop session to execute `measure_reference.py` (`bench-main` + `bench-stress`) and 3rd consecutive `-PlayerInput` run (`input-run3`). Baseline M2-C.1 60 Hz PresentMon numbers remain archived in [m2c1-acceptance.md](../m2/m2c1-acceptance.md).

---

## 1. Final M2 Technical Acceptance & Root-Cause Closures

### 1.1 Audio-Related `-PlayerInput` Failures — Reproduced & Resolved
- **Root cause:** After VieNeu v3 Turbo voice clips lengthened `CH01-D03..D09` and `CH02-D03/D05`, the real-time `-PlayerInput` suite raced with `HospitalGame.InputQA` timing (Door B, `W moves capsule`, and 3D physical inspector focus transitions), and `tools/focus_m1p_player.ps1` failed on Windows PowerShell 5.1 due to C# 7 inline `out uint` declarations.
- **Fixes:** Stabilized dialogue/inspector/focus transitions across `HospitalGame.cs`, `HospitalGame.InputQA.cs`, `HospitalGame.Pages.cs`, and `FirstPerson.cs`; converted `tools/focus_m1p_player.ps1` to C# 5 compatible syntax for PowerShell 5.1; passed `-CalibrationClick` fallback in `tools/verify_windows.ps1`.
- **Verification on `f4c6bc9`:** `input-run1` (`rc=0`, `RESULT=PASS`, 100 paper inspector cycles, `0` leaked meshes/materials, Unity allocated bytes `274,907,258 → 274,065,275`) and `input-run2` (`rc=0`, `RESULT=PASS`, `277,755,826 → 277,128,437`). (Previous pre-polish M3.0 build also passed `3/3` consecutive `-PlayerInput` runs while desktop was unlocked; `input-run3` on `f4c6bc9` aborted only on desktop lock `HWND=0`.) [input-run1](evidence/input-run1-gameplay.txt) · [input-run2](evidence/input-run2-gameplay.txt)

### 1.2 Forensic Root Cause of `0xC0000005` (`-1073741819`) Exit Crash
- **Windows Application Event Log (`Event ID 1000`) evidence across M2-audio, M2-C, and M3.0:**
  - Faulting module: bundled DirectX 12 Agility SDK `D3D12\D3D12Core.dll` version `1.618.1.0` (`+0x0000a1f5` / `+0x00264831`).
  - GPU / Driver: `Intel(R) Arc(TM) Graphics (ID=0x7d55)`, Driver `32.0.101.6127`, Unity `6000.3.28f1`.
  - Timing: Occurs **only after** `gameplay.txt` finishes writing and **after** `[Physics::Module] Cleanup` and `Input System module state changed to: Shutdown.`.
- **Controlled A/B Isolation on `probe-module-failure`:**
  - `-force-d3d12` (`kGfxThreadingModeSplitJobs`): `-1073741819` (`0xC0000005` in `D3D12Core.dll +0xa1f5`).
  - `-force-d3d12 -force-gfx-direct` (single-threaded D3D12): `-1073741819` (rules out Unity SplitJobs threading).
  - `-force-d3d11`: **clean `ExitCode=1`** (confirms fault is strictly inside `D3D12Core.dll 1.618.1.0` + Intel Arc `32.0.101.6127` teardown).
- **Mitigation & Verification:** Added `PrepareForQuit()` D3D12 drain in `HospitalGame.QA.cs` (`AudioListener.pause = true`, wait for `Texture.streamingTextureLoadingCount == 0`, 8 `WaitForEndOfFrame()` presents + `GL.Flush()` + `AsyncGPUReadback.WaitAllRequests()`).
  - Result on locked RC `f4c6bc9` under `-force-d3d12`: **11/12 active runs exited with exact expected code (`0` or `1`) and zero crashes** (`qa-medium`, `qa-low`, `qa-high`, `input-run1`, `input-run2`, and 6/7 probes: `keyboard-failure`, `visual-failure`, `escape-premature`, `char-mesh`, `char-track`, `char-interruption`). Only `probe-module-failure` (which exits prior to any full scene presentation under `-force-d3d12`) still triggers `D3D12Core.dll +0xa1f5`, while cleanly returning `1` under `-force-d3d11`.

### 1.3 Shader SRP Batcher, LOD Transitions, Memory & Lifecycle Validation
- **SRP Batcher & Pass Audit ([shader-audit.txt](evidence/shader-audit.txt)):**
  - All 5 Digital Human shaders (`CTCC/DigitalHumanSkin`, `CTCC/DigitalHumanEye`, `CTCC/DigitalHumanHair`, `CTCC/DigitalHumanFabric`, `CTCC/ApparitionHuman`) compile with **`import errors=0`** and **`SRP compatibility code=0`**.
  - **Bé An Apparition Depth Prepass Fix (`ApparitionHuman.shader` + `CharacterActor.cs`):** In URP's transparent `DrawObjectsPass`, `SRPDefaultUnlit` executes before `UniversalForward`. Swapped `ApparitionDepthPrepass` (`ZWrite On, ColorMask 0`) to `SRPDefaultUnlit` and `ApparitionHumanForward` (`ZWrite Off, ZTest LEqual`) to `UniversalForward`, paired with deterministic front-to-back transparent queues (`AnCotton=3001`, `AnHair=3002`, `AnSkin=3003`, `AnEye=3004`, `AnTeeth/AnTongue=3005`), eliminating inner-limb and oral-cavity bleed-through cleanly.
- **Automatic Distance LOD0→1→2 in Motion (`CHAR-Q05/Q06/Q08`):** Verified across all 3 characters (`Lan`, `An`, `Gatekeeper`) along the unobstructed `LOC01_Exterior` axis with `ForceLOD(-1)` and `lodBias = 1.0`, plus 3 forced Lan speech LODs with protected MoH collar & flat ID badge geometry.
- **Gatekeeper Oral Anatomy (`CHAR-Q01`):** Added `GK_Teeth_LOD0..2` and `GK_Tongue_LOD0..2` bound to jaw/head bones with `<= 4` bone influences and `0` unweighted vertices.
- **Zero-Allocation Animation & Speech (`CHAR-Q06`):** `150` measured updates (`120` steady-state + `30` live `AudioSource.timeSamples` lip-sync frames) at **`0` managed bytes allocated**, plus pause/resume, interrupted voice release, and `12` consecutive actor disable/reactivation cycles across `Low`, `Medium`, and `High` presets. [qa-medium](evidence/qa-medium-gameplay.txt) · [qa-low](evidence/qa-low-gameplay.txt) · [qa-high](evidence/qa-high-gameplay.txt)

---

## 2. Hero Character Quality Benchmark (`CHAR002 Trần Ngọc Lan`)

Investigated and benchmarked three production candidates in **Blender 4.5.10 LTS** under identical 5500K neutral studio and hard-key lighting, followed by in-engine Unity 6.3 URP validation:

| Candidate | LOD0 / LOD1 / LOD2 Tris | Key Workflow & Geometry | Stance Slip (Median) | License | Verdict |
| --- | --- | --- | --- | --- | --- |
| **A — M2-C.1 Baseline** (`c0531dc`) | `16,528 / 13,526 / 6,356` | Stock `hm08` targets, single ponytail block, body-extracted vacuum-cling blouse + vertical neck ring, BVH-projected curved badge | `1.00 m/s` | CC0-1.0 | **Rejected** (baseline reference) |
| **B — M3.0 Sculpted & Groomed Production** | `49,812 / 34,055 / 17,444` | Balanced 23yo Vietnamese targets + BMesh craniofacial sculpt (inter-canthal release, supratarsal crease, malar fullness, philtrum columns, V-taper jaw), 26-strip multi-layer hair cards + reshaped low nurse bun, pre/post-sim sternum-bridged blouse, 28-segment folded MoH 1895/1997 notched collar with pale-blue piping, flat rigid laminated ID badge with metal clip (collar & badge protected on LOD0..2), calibrated skin albedo/roughness/SSS | `0.000 m/s` (Blender/FBX), `0.008 m/s` (Unity runtime) | CC0-1.0 + 100% original authoring | **SELECTED** (production pipeline lock candidate) |
| **C — High-Density Subdiv-1 + Displacement** | `156,404` (LOD0 only, `2.99×` skinning cost) | Catmull-Clark Subdiv-1 + 3D dermal displacement | N/A (baking reference) | CC0-1.0 | **Baking source only** (raw runtime skinning exceeds multi-character 60 Hz iGPU budget) |

Visual & metric evidence:
- Blender Cycles 3-candidate studio comparison: [lan-hero-candidates-comparison.jpg](evidence/lan-hero-candidates-comparison.jpg) · [lan-hero-candidates-metrics.json](evidence/lan-hero-candidates-metrics.json)
- Unity 6.3 URP commit-specific Before (`c0531dc`) vs After (`M3.0 Candidate B`) + LOD2 silhouette preservation: [m30-lan-ingame-before-after.jpg](evidence/m30-lan-ingame-before-after.jpg)
- All 3 characters + CH03 gallery: [m30-characters-and-ch03-gallery.jpg](evidence/m30-characters-and-ch03-gallery.jpg)

---

## 3. Natural Character Performance & Locomotion Validation

### 3.1 Critical Runtime Animator Root Fix & Analytical 2-Bone Leg IK
- **P0 runtime bug caught by QA:** Generic FBX rigs exported with `avatarSetup=0` contain `0` `Animator` components on the FBX root, causing `CharacterActor.BuildPlayableGraph()` to attach `Animator` onto the outer holder GameObject one level above the FBX root—silently breaking all `*_Armature/mixamorig:...` curve bindings. Binding `Animator` directly to the FBX root child containing `*_Armature` restored full skeletal playback across all 3 characters.
- **Analytical 2-Bone Leg IK + High-Precision FBX Curves (`animationRotationError: 0.05`):** Replaced raw treadmill walk slip with analytical 2-bone leg IK in `tools/character_pipeline/retarget_cmu.py` and tightened FBX keyframe compression from `0.5°` to `0.05°`. Replaced broken CMU `18_08.bvh` on `Lan_TalkGesture` (which rotated Lan sideways with arms raised overhead once playback activated) with an authored, grounded Vietnamese reception conversation performance.

| Character / Clip | World Speed | M2-C.1 Baseline (Med / P95) | M3.0 Blender `.blend` (Toe / Sole Med) | M3.0 Exported `.fbx` (Toe / Sole Med) | M3.0 Unity `PlayableGraph` Runtime (Med / P95 / Sole) |
| --- | --- | --- | --- | --- | --- |
| `CHAR002 Lan` · `Lan_StepOut` | `1.70 m/s` | `1.000 / 2.740 m/s` | `0.0000 / 0.0001 m/s` | `0.0000 / 0.0002 m/s` | **`0.0081 / 0.0801 / 0.0077 m/s`** (`-99.2%`) |
| `CHAR005 Gatekeeper` · `GK_StalkWalk` | `1.78 m/s` | `0.730 / 2.520 m/s` | `0.0000 / 0.0008 m/s` | `0.0000 / 0.0010 m/s` | **`0.0094 / 0.0471 / 0.0093 m/s`** (`-98.7%`) |

### 3.2 Character–Prop Two-Bone Arm IK & Uninterrupted Audio-Synced Performance
- Implemented analytical two-bone arm IK (`SolveTwoBoneArmIK` with sign-invariant downward elbow pole selection) in `CharacterActor.cs` for `Lan_Handover`, holding the physical 3D F101 clipboard with printed white form, pen, and metal clip oriented face-up toward Minh.
- Synthesized & mastered `CH03-D01..05.wav` using pinned **VieNeu v3 Turbo** (`61b85e3d937fbbacb387714180e8182823512523`, Apache-2.0, `48 kHz` mono PCM mastered to `-19 LUFS`) while preserving all 24 existing `c0531dc` `CH01–02` WAV masters byte-identical ([ch03-audio-masters.json](evidence/ch03-audio-masters.json)). Generated 30 Hz Vietnamese phonetic lip-sync tracks for all 11 character speaking lines including `CH03-D03` (Bà Lành) and `CH03-D05` (Orderly) ([lipsync-verification.png](evidence/lipsync-verification.png)).
- Published uninterrupted 18.53 s (`556` frames @ `30 fps`, `1280×720 H.264` + `48 kHz stereo 192 kbps AAC`, `8` frame-synced audio cues covering Lan reception + F101 handover, Bé An Room 101 apparition, and Gatekeeper corridor stalk): [m30-character-performance.mp4](evidence/m30-character-performance.mp4).

---

## 4. Scalable M3 Architecture & Representative CH03 Proof

### 4.1 Generalized Full-Game Runtime Systems
- **Regional Streaming (`HospitalScene.cs`, `ZoneRegion.cs`):** Boot loads `AnSinh_Bootstrap` + `LOC01..LOC04` (`LoadedZoneCount == 4`, preserving M2 contracts), while `LOC05_ArchiveWing` streams asynchronously on demand via `EnsureRegionalZonesStreamedAsync()` (`LoadedZoneCount == 5`), with `261` dual-scenario GI LightProbes (`R` & `B`) and 6-scene baked Static Occlusion Culling.
- **Generalized R/B/M Reality (`Protocol.cs`, `RealityTransaction.cs`, `ZoneRegion.cs`):** Supports `Reality.R`, `Reality.B`, and `Reality.M` (warm-amber memory vignettes). Per Core 04 / `CH03-Q01`, reloading any save taken inside `Reality.M` safely restores `Reality.R` at the anchor pad (`AN03`).
- **Multi-Form Distortion & Evidence Provenance (`Protocol.cs`, `Casework.cs`):** `FormService.Distortion()` generalizes `D_zone = min(4, unique false affirmed forms)` across `F101..F105+`; `EvidenceRegistry` strictly blocks non-exportable trace `E08` from durable physical inventory (`IsPhysicalExportable == false`).
- **Puzzle 2-Strike Field Hints (`PuzzleService`):** Tracks per-puzzle failures and surfaces gentle in-world guidance after 2 failed attempts (`CH03-PU01` / `PZ03`) without soft-locking.
- **6-State Regional Stalker AI (`HunterOrderlyAI.cs`, `TASK011-A..E`):** Implements `Dormant → Patrol → Investigate → Suspicious (2.0 s pre-chase warning after 0.70 s LOS) → Chase → Return` with `LOC07` vintage radio distraction (`14 m` acoustic radius, `0.65×` occlusion attenuation), safe-pad non-intrusion breakoff (`AP06`/`AN03`/`AP07`), and modal/inspector/memory pause.
- **Versioned Save Schema v1 → v2 (`AtomicSaves.cs`):** Automatically migrates v1 saves to `Investigation.Schema = 2`, initializing `forms`, `patients`, and `puzzles` collections and preserving verified `C01` identity (`Bé An`), while enforcing the `CH03-Q03` invariant rejecting any fabricated identity on `C12`.

### 4.2 Representative CH03 Verification (`CH03-Q01..Q05` — All Automated Checks PASS)
- `LOC05` Nurse Station (`AP06`, `E03` 12-code patient register) → `LOC07` Archive/Meeting loop (`AN03`, `PZ03` / `CH03-PU01` 2-strike hint, `Reality.M` vignette + reload-to-`R` safety, `C02` existence confirmed while optional `CH03-PU02` identity remains separate, `C12` permanently anonymous, vintage radio distraction) → `LOC06` Duty Corridor (`HunterOrderlyAI` patrol/radio/warning/chase/safe-zone breakoff) → `G03` exit door → `AP07` (`CH03-CP04` persisted).
- **Mandatory Blender 4.5 CH03 Production Assets (editable `.blend` in `ArtSource/Blender/` + optimized `.fbx` in `Prototype/Assets/CTCC/Art/Meshes/`):**
  1. `CTCC_ENV_ArchiveShelfRack` (slotted-angle steel archive rack with Vietnamese case-file binders, R/B variants)
  2. `CTCC_PROP_PatientRegisterE03` (hardcover 12-code clinical admissions register + ballpoint pen)
  3. `CTCC_PROP_KnittingBagC02` (woven fabric knitting bag with yarn & needles for Bà Lành / `C02`)
  4. `CTCC_PROP_UnnamedBoxC12` (weathered cardboard personal-effects box for permanently anonymous patient `C12`)
  5. `CTCC_PROP_VintageRadio` (1990s/2000s cassette-radio distraction prop for `LOC07`)
- All 6 character `.blend` & `.fbx` inspections pass `--verify` with `missing_images: []`, `<= 4` weights/vertex, `0` unweighted vertices: [lan-fbx](evidence/lan-fbx-inspect.json) · [an-fbx](evidence/an-fbx-inspect.json) · [gatekeeper-fbx](evidence/gatekeeper-fbx-inspect.json).

### 4.3 Extended Vietnamese Hospital Research Register (`R51–R54`)
- **R51 (`1895/1997/QĐ-BYT`, Part III §5 — Quy chế lưu trữ hồ sơ bệnh án):** Requires ward nursing stations to transfer completed medical records to *Phòng Kế hoạch tổng hợp* within 24 hours of discharge/death; mandates retention periods of **10 years** (general inpatient/outpatient), **15 years** (occupational/traffic accidents), and **20 years** (deceased patients, stored in a **separate locked cabinet**). Directly grounds why 2005 records legally remain archived in `LOC07` during Minh's 2012 night shift and why `FS10` sealed lock hardware exists on the deceased/incident rack.
- **R52 (`TCVN 4470:1995` — Bệnh viện đa khoa — Tiêu chuẩn thiết kế):** Specifies inpatient ward corridor clear widths `>= 2.40 m` for two-way stretcher passage, central nursing station sightlines covering ward doors, and natural transom ventilation—directly matching `LOC05` Nurse Station sightlines and `LOC06` Duty Corridor geometry (`2.4 m+` clear NavMesh).
- **R53 (`1895/1997/QĐ-BYT` Nursing Uniform & Identification):** Grounds Candidate B's white blouse with pale-blue (`#98C8DC`) collar/pocket piping, folded notched lapel collar, and laminated PVC staff badge with alligator clip on the left breast pocket.
- **R54 (Patient Dignity & Unclaimed Personal Effects):** Grounds the strict gameplay/save separation between confirming a patient's **existence** (`C02` via `E03` + knitting bag, `C12` via unnamed personal box) and optional **identity verification** (`CH03-PU02`), ensuring `C12` never accepts fabricated names (`CH03-Q03`).

---

## 5. Verification Matrix Summary (`assembly_sha256=f4c6bc91cc...`)

| Suite / Gate | Runs / Scope | Result | Notes & Evidence |
| --- | --- | --- | --- |
| Automated Module + Character + CH03 QA (`Low`, `Medium`, `High`) | 3 presets (`qa-low`, `qa-medium`, `qa-high`) | **3/3 PASS (`rc=0`)** | Includes `TASK025-A/B`, `T02/T03/T08`, `V22-Q03`, `CHAR-Q01..Q08`, `M2 dual GI`, `CH02-Q05`, `AU01`, `T06`, `M3-ARCH & CH03-Q01..Q05` |
| Live `-PlayerInput` (`Door A`, `Door B`, 3 F101 branches, 100-cycle inspector stress) | 2 focused runs (`input-run1`, `input-run2`) | **2/2 PASS (`rc=0`)** | `0` leaked meshes/materials; 3rd run (`input-run3`) deferred due to Windows screen lock (`HWND=0`) |
| Negative Fault-Injection Probes (`-force-d3d12`) | 7 probes (`module-failure`..`char-interruption`) | **6/7 PASS (`rc=1` propagated)** | `probe-module-failure` wrote expected `RESULT=FAIL`, passed cleanly (`exit=1`) on `-force-d3d11`, and isolated `D3D12Core.dll 1.618.1.0 +0xa1f5` on Intel Arc `-force-d3d12` early exit |
| Digital Human Shader & SRP Batcher Audit | 5 shaders (`Unity 6000.3.28f1`) | **5/5 PASS (`code=0`)** | `import errors=0`, `SRP compatibility code=0` ([shader-audit.txt](evidence/shader-audit.txt)) |
| Blender `.blend` & `.fbx` Structural + Stride Inspection | 6 inspections (`Lan`, `An`, `Gatekeeper`) | **6/6 PASS (`--verify`)** | `0` missing images, `0` unweighted verts, `0.000 m/s` median slip in `.blend`/`.fbx` |
| Controlled 1080p/60 Hz PresentMon (`bench-main` + `bench-stress`) | Pending unlocked desktop | **OPEN (Blocked by `HWND=0`)** | Run `measure_reference.py` when workstation is unlocked; M2-C.1 baseline (`11.55–12.60 ms` GPU P95) in [m2c1-acceptance.md](../m2/m2c1-acceptance.md) |
| Human Gates (`TASK010`, Art Lock, `TASK026–028`) | External playtest & specialists | **OPEN (Explicit Human Gates)** | Never bypassed or auto-closed |
