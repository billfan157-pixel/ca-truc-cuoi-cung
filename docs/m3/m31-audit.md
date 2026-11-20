# M3.1 — Independent repository audit (baseline `31e06c2`)

This records the starting findings and proposals. The current graph and implementation scope are in the [M3.1 world record](m31-world.md).

Scope: everything claimed by M3.0 ([m30-foundation-and-art-lock.md](m30-foundation-and-art-lock.md)) re-read against the owning sources (Core 02/03/04/06, `07/Chapter gates`, `Events`, `Patients`, `Puzzle specs`, `Dialogue`, `QA`) and against the code at `31e06c2`.
Method: static review of `Prototype/Assets/CTCC/{Runtime,Editor}`, `tools/`, `ArtSource/`, plus a re-run of the module QA suite on the locked M3.0 build (`output/m31/baseline-qa-medium`, `RESULT=PASS`, exit 0).
A green M3.0 QA does **not** imply the defects below are absent: the suite asserts the M3.0 behaviour, not the Core contracts.

## 0. Baseline facts

| Item | Result |
| --- | --- |
| Working tree | clean at `31e06c2` (`main`) |
| Toolchain present | Unity 6000.3.28f1, Blender 4.5 (`C:\Program Files\Blender Foundation\Blender 4.5`), Python 3.14 (+numpy, Pillow; no matplotlib), Git; no ffmpeg on PATH |
| Baseline module QA (`tools/verify_windows.ps1`, Medium) | PASS (exit 0) — every `TASKxxx`/`CHAR-Q`/`M3-ARCH` line green |
| Interactive desktop | **locked** (`GetForegroundWindow()==0`) at audit time → `-PlayerInput` suite and PresentMon benchmarks cannot be run from this session (external blocker, see §5) |
| Free disk | 9.6 GB (limits parallel Unity Library copies / many builds) |
| Production DB | `Roadmap`: M2 "Đang làm" 0/16, M3 "Chưa làm" 0/33 → stale versus M3.0 delivery; no row records M3.0/M3.1 |

## 1. Findings

Severity: **P0** = blocks design contract or can corrupt state, **P1** = visible contract/UX defect, **P2** = quality/maintainability.

### 1.1 World topology & architecture

| ID | Sev | Finding | Evidence | Contract violated |
| --- | --- | --- | --- | --- |
| A-01 | P0 | LOC05/06/07 are one flat slab at `y=0`, `z∈[25,40]`, 2.9 m beyond LOC04 with no physical link. CH03 starts by **teleport** (`BeginCh03` → `AP06`). | `HospitalAuthoring.cs:645-676`, `HospitalGame.Pages.cs:562-571`, `Casework.cs:301-312` | Core 03 §3 (floor levels B1 −3.40 / G 0 / L2 +4.00 / L3 +8.00 / roof +11.70); `07/Locations` (LOC05 floor 2, LOC06 floor 2, LOC07 floor 3); Core 04 topology (stair axis 02–05–07, tech stair 04–09–10/11) |
| A-02 | P0 | No stair, basement or upper-floor geometry exists anywhere; LOC09–12 and LOC08 have no geometry at all. | `HospitalAuthoring.cs:23-30` (`ZoneSpecs` lists LOC01–05 only) | Core 04 LD01/LD02/LD04 cannot even be evaluated |
| A-03 | P1 | Level data (`layout.json.levels`) lists six datum levels but only `TRET` is used by code; no machine-checked masterplan, so the doc claims in `layout.md` are not verifiable. | `docs/prototype/layout.json:39-46` | TASK022 acceptance is "draft" only |
| A-04 | P1 | Streaming is "load LOC01-04 at boot, LOC05 once, never unload". `UnloadZoneAsync` has no caller; there is no adjacency graph, preloading, hysteresis or lifetime accounting. | `HospitalScene.cs:186-250`, `HospitalGame.Pages.cs:562-565` | Core 04 §01 "streaming theo tầng"; M3.0 text calls it "regional streaming" |
| A-05 | P2 | `HospitalScene` rebinds a single global slot per door/prop/actor when a zone registers; a second zone with the same role silently overwrites the first. Works for one wing; will not for LOC05–12. | `HospitalScene.cs:287-334` | scalability |

### 1.2 Runtime data contracts

| ID | Sev | Finding | Evidence | Contract violated |
| --- | --- | --- | --- | --- |
| C-01 | P0 | `Distortion()` is a **hospital-global** count of every affirmed form (F101 + `forms[]`). Spec: `D_zone = min(4, #unique FALSE affirmed forms in that zone)`; "không tính D toàn bệnh viện bằng cộng lặp"; affirmed-but-true ≠ distortion. | `Protocol.cs:197-211`, `HospitalGame.Pages.cs:547` | Core 01 §03, Core 04 CH01 table (`D_zone101`), Core 06 `FormService` |
| C-02 | P0 | Unregistered form IDs: `ApplyForm` accepts any string; `RefutationAvailable` special-cases `"F303"`, an ID that exists **nowhere** in canon (only F101 is defined; grep over Core 01–06 + all sheets). | `Protocol.cs:187-192, 229-254` | Core 06 Form record (id, zone, observed[]) |
| C-03 | P1 | Evidence provenance mismatch: `EvidenceRegistry` pins E03 and the C02/C12 items to `LOC07`, `FS10.LockReceipt` to `LOC05`, while the UI says "chưa lấy tại trạm điều dưỡng LOC05" and M3.0 acceptance says "LOC05 Nurse Station (E03)". Core 04 B02/B03 put E03 at LOC07, FS10 at LOC06 (B07). | `Protocol.cs:298-304`, `HospitalGame.Pages.cs:621`, `m30…md:90` | Core 04 CH03 B01–B07; `07/Patients` |
| C-04 | P1 | `FS10.LockReceipt` is registered as **exportable evidence**. FS10 is a foreshadowing *clue* ("nhìn … chưa kết luận"), CH03 must not grant E02. | `Protocol.cs:270,303`, `HospitalGame.cs:584-590` | Core 04 CH03 B07 + design-decision note |
| C-05 | P1 | Save validator whitelists `CH01–CH03` only, `SafeCheckpoints` has no CH04 pads, `PrepareBeginCh03` is the last chapter transition → CH04 cannot be saved/restored. Validator also never checks `forms[]` ids/zones, `puzzles[]`, nor forged `C12.verified` events. | `AtomicSaves.cs:149-169`, `Protocol.cs:384-407` | Core 06 Save |
| C-06 | P1 | CH03 trigger IDs drifted from `07/Events`: code sets T01,T02,T03,T04(on C12 box); never T05, T06, T07 or FS19; invents `CH03-M01/M02`. `HospitalGame.cs:840` reads `CH03-T06` which is never set (dead branch). | `Casework.cs:310-440`, `HospitalGame.cs:838-843` | `07/Events` CH03-B01…B07 |
| C-07 | P2 | QA IDs are mislabelled: Core 04 defines `CH03-Q01..Q05` as {existence-only, wrong match/C12 name, patrol LOS, save during E03/vignette, FS08-10}; M3.0 reports a different mapping (e.g. "Q05 = 2-strike hint"). | `HospitalGame.QA.cs:998-1104`, Core 04 CH03 QA table | `07/QA` traceability |

### 1.3 AI

| ID | Sev | Finding | Evidence |
| --- | --- | --- | --- |
| AI-01 | P0 | **Unsafe direct movement**: when `NavMesh.SamplePosition`/`CalculatePath` fails the step target silently falls back to the raw destination and `Vector3.MoveTowards` drives the agent straight through walls (and ignores Y, so stairs are impossible). | `HunterOrderlyAI.cs:311-323` |
| AI-02 | P1 | `CalculatePath` runs **every frame**, `cornerIndex` is reset to 1 each call (no path following, no cache, no repath throttle, no stuck detection). | `HunterOrderlyAI.cs:312-320` |
| AI-03 | P1 | One class and one actor (`GatekeeperActor`) serve "Hunter/Orderly"; no Unrecorded profile; speeds/senses/warnings are `const`. CH04 needs CHAR005 full stealth + CHAR006 schedule route + Unrecorded. | `HunterOrderlyAI.cs:28-40, 81` |
| AI-04 | P1 | Safe-zone test is a hard-coded pad list with a flat 2-D radius; ignores Y so a player on a stair above/below a pad counts as "safe". Chase capture teleports to hard-coded `AP06`. | `HunterOrderlyAI.cs:110-124, 258-266` |
| AI-05 | P2 | AI state is not part of the save contract (acceptable) but nothing re-seeds it safely after cross-zone restore ("không spawn sát player"). | `SyncState` |

### 1.4 CH03 experience

| ID | Sev | Finding | Evidence |
| --- | --- | --- | --- |
| N-01 | P0 | **PZ03 hands over the answer**: the anchor UI literally offers "Chọn mã C02 (Khớp mẫu len & túi vải)" next to a decoy "Thử mã khác (C03)". No inference from E03/note/bag is required. | `HospitalGame.Pages.cs:603-616` |
| N-02 | P0 | **Reality.M is a flash, not a performance**: `Ch03InspectKnittingBag` enters M, plays D03, and immediately calls `SwitchMemoryVignetteAn03` again (back to R) in the same callback; no vignette timing, animation, ambience, caption or player agency. | `HospitalGame.Pages.cs:635-645` |
| N-03 | P1 | Premature completion text: `Ch03EndOfChapter` says "HOÀN TẤT CHƯƠNG 03 / KIẾN TRÚC M3 ĐÃ XÁC MINH" and "Đã khôi phục hai vị trí" (C12 not required by G03); gate hint tells the player they need "C02, C12" while `GateService.G03` needs only E03 + C02. `EndOfVisit` still says "KẾT THÚC VERTICAL SLICE M2" while offering CH03. | `HospitalGame.Pages.cs:544,708-716`, `HospitalGame.cs:593-597` |
| N-04 | P1 | C12: B05 requires the player to *read* the C12 row (FS19, T05, journal "chưa xác định"); code only asks for a button on the box; no register row UI, no journal entry. | Core 04 B05, `HospitalGame.Pages.cs:671-689` |
| N-05 | P1 | Radio distraction, hunter patrol and G03 exit live in the flat LOC06 corridor; Core 04 B06 places the patrol in LOC07 with a safe room and meeting-room loop. | `HospitalAuthoring.Zones.cs:387-411` |
| N-06 | P2 | No CH04 content exists (`Chapter gates` CH04 "Chưa test"/"Chưa triển khai"). | `07/Events` CH04-B01…B06 |

### 1.5 Art / pipeline

| ID | Sev | Finding |
| --- | --- | --- |
| R-01 | P1 | All M3.0 Blender props (`build_m3_ch03.py`) are scripted assemblies of boxes/cylinders with bevels. They are valid modular blockouts with PBR slots, **not** finished artistic craftsmanship; the acceptance wording must not claim otherwise. |
| R-02 | P1 | No stair, handrail, stair-core, morgue, electrical panel or medication-cart modules exist; materials lack stair-specific wear (nosing, terrazzo polish) and no Blender render of any LOC05–07 interior exists. |
| R-03 | P2 | Candidate B Lan stays "provisional baseline pending human art review" (unchanged, correct). |

### 1.6 Validation / release

| ID | Sev | Finding |
| --- | --- | --- |
| V-01 | P1 | M3.0 verification used a dirty/previous output (`output/m30-main-31e06c2/build`); a reproducible build from a clean committed checkout is not demonstrated. |
| V-02 | P1 | `probe-module-failure` still dies with `0xC0000005` in `D3D12Core.dll 1.618.1.0+0xa1f5` on `-force-d3d12` (clean `exit=1` on D3D11). Root cause isolated, not fixed. |
| V-03 | P1 | Controlled 1080p/60 Hz PresentMon re-benchmark and the 3rd consecutive `-PlayerInput` run were never completed (desktop lock). |
| V-04 | P2 | Evidence is published only as numbers; no commit-linked Blender renders of CH03 interiors, no CH04 footage. |

## 2. Proposed dispositions at baseline

| Finding | M3.1 action |
| --- | --- |
| A-01..A-03 | Machine-readable masterplan (`docs/m3/masterplan/masterplan.json`) + validator (LD01–LD08 style graph checks, stair geometry, vertical alignment, clearances) + drawings; Unity full-hospital greybox with cross-floor NavMesh BFS; playable Block N rebuilt from the masterplan at +4/+8 and B1. |
| A-04/A-05 | `RegionStreamer`: adjacency graph from the masterplan, 1-hop load / 2-hop preload / hysteresis unload, lifetime accounting, persistent-state rehydrate. |
| C-01..C-07 | `FormRegistry` + per-zone distortion, evidence provenance table fixed to Core 04, FS10 as clue, save schema v3 with CH04 + stricter validator, trigger IDs aligned to `07/Events`, QA IDs renamed to Core 04. |
| AI-01..AI-05 | `AgentProfile` data (Hunter / Orderly / Unrecorded), `SafeNavigator` (path cache, throttled repath, stuck recovery, never raw-move), level-aware safe zones. |
| N-01..N-06 | Inference-based PZ03, authored M vignette sequence, honest completion states, CH04 B01–B06 representative implementation honouring E04/E07, H05, PT02 and the D16b guard. |
| R-01/R-02 | Blender stair/handrail/morgue/panel/cart modules with wear + Cycles renders; wording corrected: scripted modelling ≠ art sign-off. |
| V-01..V-04 | Clean-checkout build, benchmark attempt with raw data, D3D12 teardown investigation, evidence under `docs/m3/evidence/m31/`. |

## 3. Open design questions (escalated, not decided here)

1. **E03 acquisition site.** Core 04 (B02/B03) = LOC07; M3.0 acceptance prose = LOC05. M3.1 follows Core 04 (reversible, data-only) — please confirm via `07/Decisions`.
2. **No canonical form besides F101.** Core 01–06 define only F101. M3.1 removes `F303` and requires registry entries; a *new* patient/room form (needed for the "multiple forms" scale test) is created only as a **QA fixture**, never shipped, until a DEC is approved.
3. **S2 as a full-height core (B1→L3).** Core 04 lists the tech stair as 04–09–10/11 and the main stair as 02–05–07. M3.1 realises 05→07 by continuing a core of the technical stair on the north wing; if TASK026 prefers 05→07 on the *main* stair this is a one-file masterplan change.
