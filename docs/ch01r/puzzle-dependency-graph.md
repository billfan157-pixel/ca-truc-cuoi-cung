# CH01-R Puzzle Dependency Graph & Investigative Architecture

> **Scope:** *Ca trực cuối cùng* — **CH01 (*Người trở về*)** reconstruction (`CH01-R`) through **CH02 (*Phòng bệnh trống*)** handoff.
> Preserves all canonical gates (`G01–G04`), `LOCK-V22-01..03`, `DEC-CH02`, `LOCK-LOG06`, and `CH01 evidence.Count == 1` (`E10a` only).

---

> Current design: [CH01 walkthrough B01–B08](walkthrough-gameplay-B01-B08-investigation-v1.md), DEC-CH01R-04.

## 1. Narrative Arc, Nonlinear Exploration Loop & F101 Deduction Flow

```mermaid
flowchart TD
    B01["B01 · LOC01 Exterior (R)\nInspect E10a Envelope & SMS"] --> GATE["Unlock GATE-CONG\n(CH01-T02)"]
    GATE --> B02["B02 · Courtyard Approach (R)\nLan Silhouette (DREAD_B02)"]
    B02 --> HUB1["B03 · Giở sổ trực tại quầy\nR → B tại chỗ (CH01-T03/T04, mốc lưu AP02)"]
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
    C & D & B & STAFF -.->|"Cất giấy rồi chọn nguồn để đối chiếu"| JOURNAL["Tab/J → Ba lô → Pocket Note → Nhật ký\nHiện trường · Tài liệu · Suy luận · Dòng thời gian · Quyết định"]
    B -.->|"Reveal reverse-side impression\n(tilt / large print)"| AS05TIME["AS-05B: 03g17"]
    AS05TIME & RECDRAW & STAFF -.->|"Clock and shift context; choose matching time"| EX01["Optional CH01-EX01\nUnlocks CH01-D19 after signing in B"]
    R101 --> F101["B05 · Nhấc phiếu F101 khỏi bảng kẹp\nSoi, đánh dấu và đặt lại để ký"]
    A & B -.-> F101

    F101 -->|"Path A · Affirmed\n(Certify 0)"| OUT_A["Reality.B: Solid Partition (deniedBay)\nF101_SignedA · Sterile Fan · PA01\nDistortion(LOC03) = 1"]
    F101 -->|"Path B · Unverified\n(Withhold Confirmation)"| OUT_B["Reality.B: Lingering Curtain (pendingBay)\nF101_SignedB · Slowed Fan · PA02\nDistortion(LOC03) = 0"]
    F101 -->|"Path C · Mark two fields and link the scene"| MARKS["Mark the printed zero and blank receiving signature\nLink the card mark to wheel marks, blanket and slippers"]
    C -.->|"Optional comparison"| MARKS
    MARKS --> BUILD["HypothesisService validates marks, link and provenance"]
    BUILD -->|"Sign: Chưa đủ căn cứ xác nhận số 0"| OUT_C["Reality.B: Parted Curtain + refutedBay\nDamp Wall · Sharp Wheels · CH01-PR01 Corner\nDimmed Tube · F101_SignedC · PA03"]

    %% All 3 decisions open G01 and advance CH01 canonically
    OUT_A & OUT_B & OUT_C --> G01["CH01-T07 · G01 opens on durable signature\nPen, stamp, quiet, then audible room/PA changes"]
    G01 -.-> SLIP["Corridor slip reflects the signed report in B"]
    G01 --> HUB2["B06 · Bàn giao phiếu tại sổ trực\nB → R tại chỗ (CH01-T08)\nLan vắng mặt"]
    HUB2 --> SEAM["B07 · Outer Gate Seam Loop Once\n(CH01-T09)"]
    SEAM --> HUB3["B08 · Giở sổ để sang CH02\n(CH01-T10 · CP04 · lớp R)"]
    HUB3 --> CH02["CH02 · Independent Reality.R Crime Scene\nLIST + SOCKETS + WHEELS → PZ01 (E12) → E11 → G02"]
```

---

## 2. Authoritative F101 Epistemic Validation Matrix (`HypothesisService.EvaluateF101`)

| Evidence / Claim Combination | Decision | Authoritative Outcome | Rationale |
| --- | --- | --- | --- |
| Any observed state (including 0 clues) | `Affirmed` | **`Valid`** | Player may always certify the printed zero; sets `Distortion(LOC03) = 1` and conceals Bed 4 in Layer B. |
| Any observed state (including 0 clues) | `Unverified` | **`Valid`** | Responsible epistemic restraint — never penalized and never blocks `G01` or CH01 completion. |
| `F101_ZERO` + `OBS101B_SIGN`, with `OBS101B_SIGN → OBS101A_TRACES` and `F101_CLAIM_UNRELIABLE_ZERO` | `Refuted` | **`Valid`** | The observed scene and incomplete card support withholding certification of the reported zero. The Bed 2 transfer is optional. |
| Core pair and supported claim from an existing save | `Refuted` | **`Valid`** | Legacy revisions retain their original evidence and claim. |
| Both source documents and the scene, with an incorrect mark or missing link | `Refuted` | **`InsufficientEvidence`** | The player's notes do not yet support signing the discrepancy. |
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

Save schema v7 loads v1–v6 and keeps F101 drafts/history, authored item locations and backpack contents distinct from observations, notes and evidence. T08 restores B paper to its fixture and retains its memory reference. Each new annotated `Refuted` revision records a separate copy of the signed marks, link and claim. The `ObservationRecord` stages for CH01 and CH02 are:

1. **`Discovered` (`0`)**: Registered but not yet inspected (insufficient for formal refutation).
2. **`Examined` (`1`)**: Directly inspected in-world or via `PhysicalInspector` / Large Print in valid layer & location.
3. **`Interpreted` (`2`)**: Reverse side inspected (`Space`/`R` flip) OR key detail callout (`1–3`) / Large-Print full transcript reviewed.
4. **`Corroborated` (`3`)**: Successfully cited inside an accepted `Refuted` F101 deduction submission.

`CH01-EX01` requires the clock, one source of shift context, and `RAKING_INDENT` specifically on `OBS101B`. Ordinary card inspection, flipping the card, and opening Large Print leave the optional note locked. The player can reveal the impression by tilting the card or selecting **Đọc vết hằn mặt sau** in Large Print. The journal then accepts the matching time. Saves that already contain `CH01-EX01` retain their completed note.

**Canon Preservation Guardrails:**
- `Reality.B` observations NEVER enter `state.evidence` (`CH01 state.evidence.Count == 1`, strictly `"E10a"`).
- `OBS101B` never mentions Bé An, `C01`, `Khu C`, `E11`, or `E12` (`DEC-CH02` / `LOCK-LOG06`).
- `CH01-D16b` and `CH04-D06` remain hard-locked behind `CH04 B06 + H05.verified + PT02.presented` (`LOCK-V22-02`).
- `Reality.R` geometry, `LIST`/`SOCKETS`/`WHEELS`, `PZ01`/`E12`, and `E11` remain completely unaltered regardless of F101's `Affirmed` / `Unverified` / `Refuted` decision.
