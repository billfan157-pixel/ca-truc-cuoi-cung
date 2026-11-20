# CH01-R Puzzle Dependency Graph & Investigative Architecture

> **Scope:** *Ca trực cuối cùng* — **CH01 (*Người trở về*)** reconstruction (`CH01-R`) through **CH02 (*Phòng bệnh trống*)** handoff.
> Preserves all canonical gates (`G01–G04`), `LOCK-V22-01..03`, `DEC-CH02`, `LOCK-LOG06`, and `CH01 evidence.Count == 1` (`E10a` only).

---

## 1. Narrative Arc, Nonlinear Exploration Loop & F101 Deduction Flow

```mermaid
flowchart TD
    B01["B01 · LOC01 Exterior (R)\nInspect E10a Envelope & SMS"] --> GATE["Unlock GATE-CONG\n(CH01-T02)"]
    GATE --> B02["B02 · Courtyard Approach (R)\nLan Silhouette (DREAD_B02)"]
    B02 --> HUB1["B03 · AP02 Reception Hub\nAtomic R → B Swap (CH01-T03/T04)"]
    HUB1 --> LAN["Live In-World Lan Encounter\n(Free Look + Optional LAN_TALK)"]
    LAN --> BOARD["Accept Clipboard BOARD\n(CH01-T05 · Unlocks Room 101 & Records OBS101_F101)"]

    %% Compact Optional Nonlinear Exploration Loop
    BOARD --> RECDRAW["LOC02 Optional:\nDRAWER-DESK → OBS_RECEPTION_DRAWER\n& Stopped 03:17 Clock (FS01)"]
    BOARD --> STAFF["STAFF Nursing Alcove (Optional):\nOBS_CORRIDOR_NOTICE\nDRAWER-NURSE → OBS_SHIFT_LOG (AS-05)"]
    STAFF -->|"Hinged DOOR-STAFF\n(Click or Hold-Drag)"| HALL["HALL Main Corridor\n(DREAD_B04 Trolley / DREAD_B05 Steps)"]
    BOARD --> HALL
    HALL --> R101["B04 · LOC03 Room 101 (B)\n(CH01-T06)"]

    %% Flexible Evidence Discovery inside Room 101 (any order!)
    R101 --> C["Comparison Doc · OBS101C\nBed-2 Completed Transfer\n(2 Signatures + Nội B)"]
    R101 --> D["Comparison Doc · OBS101D\nDRAWER-BED3 → Bed-3 Closed Meds\n(Matches F101 — Irrelevant to Bed 4)"]
    R101 --> CURTAIN["CURTAIN-101\n(Click or Hold-Drag)"]
    CURTAIN --> A["Core Fact 1 · OBS101A\nBed-4 Physical Use Traces\n(Wheel Marks, Folded Blanket, Slippers)"]
    R101 --> B["Core Fact 2 · OBS101B\nBed-4 Incomplete Card AS-05B\n(Dual-Sided Flip + Detail Hotspots)"]

    %% Journal Comparison & Authoritative F101 Deduction
    C & D & B & STAFF -.->|"Optional Side-by-Side Comparison\n(Zero Auto-Spoilers)"| JOURNAL["Evidence Journal (Tab)\nProvenance · Stage · Compare · History"]
    R101 --> F101["B05 · F101 Handover Form at AP04\n(Claims: 0 Outstanding Patients)"]
    A & B -.-> F101

    F101 -->|"Path A · Affirmed\n(Certify 0)"| OUT_A["Reality.B: Solid Partition (deniedBay)\nF101_SignedA · Sterile Fan · PA01\nDistortion(LOC03) = 1"]
    F101 -->|"Path B · Unverified\n(Withhold Confirmation)"| OUT_B["Reality.B: Lingering Curtain (pendingBay)\nF101_SignedB · Slowed Fan · PA02\nDistortion(LOC03) = 0"]
    F101 -->|"Path C · Refuted\nRequires Core Pair (OBS101A + OBS101B)\nNo Irrelevant Docs + Valid Claim"| BUILD["Authoritative HypothesisService.EvaluateF101\nRejects Missing/Irrelevant/Overclaims\n(Recoverable · Hint after 2 Failures)"]
    BUILD -->|"Valid:\nF101_CLAIM_UNRELIABLE_ZERO"| OUT_C["Reality.B: Parted Curtain + refutedBay\nDamp Wall · Sharp Wheels · CH01-PR01 Corner\nDimmed Tube · F101_SignedC · PA03"]

    %% All 3 decisions open G01 and advance CH01 canonically
    OUT_A & OUT_B & OUT_C --> G01["Gate G01 Open (CH01-T07)\nF101 Revisions Allowed at AP04"]
    G01 --> HUB2["B06 · AP02 Reception Hub\nAtomic B → R Collapse (CH01-T08)\nLan Calmly Absent"]
    HUB2 --> SEAM["B07 · Outer Gate Seam Loop Once\n(CH01-T09)"]
    SEAM --> HUB3["B08 · AP02 CH02 Handoff\n(CH01-T10 · CP04 · Reality.R Intact)"]
    HUB3 --> CH02["CH02 · Independent Reality.R Crime Scene\nLIST + SOCKETS + WHEELS → PZ01 (E12) → E11 → G02"]
```

---

## 2. Authoritative F101 Epistemic Validation Matrix (`HypothesisService.EvaluateF101`)

| Evidence / Claim Combination | Decision | Authoritative Outcome | Rationale |
| --- | --- | --- | --- |
| Any observed state (including 0 clues) | `Affirmed` | **`Valid`** | Player may always certify the printed zero; sets `Distortion(LOC03) = 1` and conceals Bed 4 in Layer B. |
| Any observed state (including 0 clues) | `Unverified` | **`Valid`** | Responsible epistemic restraint — never penalized and never blocks `G01` or CH01 completion. |
| `OBS101A` + `OBS101B` + `F101_CLAIM_UNRELIABLE_ZERO` | `Refuted` | **`Valid`** | Complementary physical trace + incomplete record establish that reported `0` cannot be reliably certified. |
| `OBS101A` + `OBS101B` + `{OBS101C / OBS_SHIFT_LOG / OBS_CORRIDOR_NOTICE / OBS101_F101}` + `F101_CLAIM_UNRELIABLE_ZERO` | `Refuted` | **`Valid`** | Valid core pair corroborated by relevant comparison standard or administrative overflow context. |
| Missing `OBS101A` OR missing `OBS101B` (even with `OBS101C` or `OBS_SHIFT_LOG`) | `Refuted` | **`InsufficientEvidence`** | Single card alone could be leftover stationery; physical trace alone does not prove incomplete handover. |
| Core pair + `OBS101D` (Bed-3 closed medication log) | `Refuted` | **`IrrelevantEvidence`** | Bed 3's closed medication record matches F101 and does not bear on Bed 4's discrepancy. |
| Core pair + `OBS_CLOCK_0317` or `OBS_RECEPTION_DRAWER` | `Refuted` | **`IrrelevantEvidence`** | Atmospheric/institutional context, not direct grounds to refute Room 101's bed count. |
| Core pair + `F101_CLAIM_PATIENT_PRESENT` | `Refuted` | **`UnsupportedConclusion`** | Past use + blank signature do not prove a patient is currently still in Room 101. |
| Core pair + `F101_CLAIM_FALSIFICATION` | `Refuted` | **`UnsupportedConclusion`** | Missing signatures do not prove deliberate falsification or a concealed death. |
| Core pair + `F101_CLAIM_CLERICAL_OK` | `Refuted` | **`ContradictsDecision`** | Conclusion affirms the zero while submitting `Refuted`. |
| Unregistered ID (`"OBS_UNKNOWN_999"`) | `Refuted` | **`UnknownEvidenceId`** | Gameplay logic never trusts unvalidated UI IDs or booleans. |
| Wrong layer/location provenance or `ObservationStage.Discovered` only | `Refuted` | **`InvalidProvenance` / `UnexaminedEvidence`** | Provenance & examination stage verified authoritatively on `ObservationRecord`. |

---

## 3. Observation Lifecycle & Provenance Contract

Every CH01 & CH02 observation is tracked via `ObservationRecord` inside `Investigation` (Save Schema `v4`, backward-compatible with `v1/v2/v3`):

1. **`Discovered` (`0`)**: Registered but not yet inspected (insufficient for formal refutation).
2. **`Examined` (`1`)**: Directly inspected in-world or via `PhysicalInspector` / Large Print in valid layer & location.
3. **`Interpreted` (`2`)**: Reverse side inspected (`Space`/`R` flip) OR key detail callout (`1–3`) / Large-Print full transcript reviewed.
4. **`Corroborated` (`3`)**: Successfully cited inside an accepted `Refuted` F101 deduction submission.

**Canon Preservation Guardrails:**
- `Reality.B` observations NEVER enter `state.evidence` (`CH01 state.evidence.Count == 1`, strictly `"E10a"`).
- `OBS101B` never mentions Bé An, `C01`, `Khu C`, `E11`, or `E12` (`DEC-CH02` / `LOCK-LOG06`).
- `CH01-D16b` and `CH04-D06` remain hard-locked behind `CH04 B06 + H05.verified + PT02.presented` (`LOCK-V22-02`).
- `Reality.R` geometry, `LIST`/`SOCKETS`/`WHEELS`, `PZ01`/`E12`, and `E11` remain completely unaltered regardless of F101's `Affirmed` / `Unverified` / `Refuted` decision.
