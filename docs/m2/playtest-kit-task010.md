# Ca Trực Cuối Cùng — Human Playtest Kit (`TASK010`)

- **Milestone / Gate:** M2 Vertical Slice (`TASK010`)
- **Status:** **Human Review Pending — OPEN (0/6 human sessions completed)**
- **Canonical Design Standard:** `Core v3.6.2 (Documentation Consolidation)` (`DEC-VO-01`, `DEC-V36-CH01-01`, `DEC-V361-CH01-01`). **Lưu ý:** candidate `c459aba` bên dưới được đóng gói từ `Core v3.3.1 Final Clean` (trước `DEC-VO-01` & `DEC-V36-CH01-01`); theo `07/Backlog` `TASK010`, cần bridge runtime CH01 v3.6 trước khi nghiệm thu playtest chính thức trên chuẩn mới.
- **Approved Technical Candidate for Sessions (pre-v3.6 baseline):** `m2-final-cand-c459aba` (`revision=c459aba86192591b4adf5d267438e5fb4b77f704`, `development=False`, Unity `6000.3.28f1`, built against `Core v3.3.1 Final Clean`)
  - `exe_sha256`: `fbd073ca59c4918edbc6704f5393cf59021173af11714c00db4212c50117492b`
  - `assembly_sha256`: `373d7c91bfcc071a04969b664f6a2fd03c553ee66e39fb65f6fcb8b58b149cbc`
  - **Release Package:** [output/m2-final/m2-final-release-c459aba/CaTrucCuoiCung-M2-FINAL-c459aba-Windows-x64.zip](../../output/m2-final/m2-final-release-c459aba/CaTrucCuoiCung-M2-FINAL-c459aba-Windows-x64.zip) + [SHA256SUMS.txt](../../output/m2-final/m2-final-release-c459aba/SHA256SUMS.txt)
  - **Moderator Launcher (6 isolated sessions `PT-01`..`PT-06`):** [start-task010-session.ps1](../../output/m2-final/m2-final-release-c459aba/start-task010-session.ps1)
    ```powershell
    & '.\start-task010-session.ps1' -ParticipantId PT-01
    ```
    *(Verifies binary SHA-256 against `build-identity.txt`, isolates `-ctcc-save` and `-ctcc-playtest-log` per participant, and launches `1920x1080` Medium D3D12 cleanly.)*
- **Target Sample:** ≥ 6 new players unfamiliar with the codebase or Core Design System
- **Scope:** use a named M2 candidate covering shortened CH01, PZ01/PZ02 to verified C01, and the scripted cameo. The full CH01 walkthrough has its own timing target.
- **Pass thresholds (Core 01 and Core 07/TASK010):**
  1. Score three outcomes independently. At least **5/6** verify C01 within **8 minutes of starting PZ01** without moderator guidance. At least **5/6** explain how an incorrect F101 affirmation changes the room. At least **5/6** distinguish the patient guide from the threat using their behavior. Record in-game hints separately.
  2. **Epistemic Fairness:** Leaving F101 **`Unverified` is NEVER scored as failure** — it is a canonical, responsible decision when a player withholds certification without sufficient grounds. For players who attempt `Refuted`, observe whether they distinguish Bed-4 relevant evidence (`OBS101A`, `OBS101B`) from the Bed-3 routine medication log (`OBS101D`) and avoid overclaiming patient presence or deliberate falsification.
  3. **0 critical softlocks** across all sessions (including recovery after rejected F101 refutation arguments, optional `STAFF` exploration loop, and interactive drawers/doors).
  4. **Average completion time** falls within **20–30 minutes** (for shortened CH01 through C01 and scripted cameo).
  5. Accessibility & comfort: zero unmitigated motion-sickness dropouts; directional captions, Large-Print document view, single-press click/toggle alternatives (vs optional hold-and-drag), and Reduced Motion / Story Mode verified when used.

> [!IMPORTANT]
> Per project truthfulness rules, **no synthetic, AI-simulated, or fabricated playtest sessions are recorded here.** Record the build revision, start/end markers, and exact slice before the first session.

---

## 1. Pre-Session Protocol & Content Note

1. **Hardware Setup:** Reference target or equivalent Windows 11 PC at `1920x1080`, `Medium` preset, headphones or stereo speakers enabled, mouse + keyboard.
   - **Optional Blind-Playtest Telemetry Flag (Upgrade #4):** Launch the executable with `-ctcc-playtest-log "<session-dir>\PT-01-telemetry.json"` so that upon completing CH01 (`CH01-T10`), the build automatically writes a non-intrusive JSON summary (`durationSeconds`, `firstF101Decision`, `currentF101Decision`, `f101Revisions`, `f101FailedAttempts`, `visitedStaffAlcove`, `observedCount`, `corroborated0317`, `rakingIndentRevealed`, `dreadEventsCount`, accessibility toggles) to measure blind `Refuted` solve rate and optional discovery rates objectively.
2. **Content Notice (Read aloud / shown on Main Menu):**
   - Fictional Vietnamese hospital setting (*Bệnh viện An Sinh*, 1987–2012).
   - Themes include grief, memory of a past hospital fire, and psychological tension (no extreme gore, no lethal CH01 chase).
   - Inform participant they may enable **Giảm chuyển động (Reduced Motion)**, **Chế độ Cốt truyện (Story Mode)**, **Chú thích âm thanh (Directional Captions)**, or **Tăng cỡ chữ (130%)** at any time, or pause/stop without penalty.
3. **Neutral Briefing (Zero Spoilers):**
   - *"Bạn sẽ chơi bản Vertical Slice dài khoảng 20–30 phút của game kinh dị điều tra Ca Trực Cuối Cùng. Hãy chơi theo cách tự nhiên nhất. Nếu bí hoặc cảm thấy khó hiểu ở đâu, hãy nói thành tiếng suy nghĩ của bạn; điều phối viên sẽ quan sát. Bạn có thể dùng gợi ý trong game."*
   - **Never explain R vs B, Lan, F101 routes, or Bed 4 beforehand.**

---

## 2. Live Observation Rubric (Moderator Checklist)

| Beat | Space | Key Signals to Observe | Pass / Friction Notes |
| --- | --- | --- | --- |
| **CH01-B01/B02** | `LOC01` Exterior | Does the player find the envelope by the motorbike, read Bảy's letter and SMS, unlock the gate and spot Lan's silhouette through the reception glass? | |
| **CH01-B03** | `LOC02` Reception | Does the player find the physical duty book and understand the in-place change to B? Note their reaction to Lan, whether they speak with her, and how easily they take the clipboard. | |
| **CH01-B04/B05** | `STAFF`, `HALL`, `LOC03` Room 101 (B) | Record which route the player takes first, whether they manipulate doors, drawers or the curtain, and which details they check. Observe paper flipping, marking and reading in 3D or large print. The optional impression reads `03g17 · CHƯA CHỐT SỔ`. Note use of the notebook to compare, write or remove a link, the initial F101 decision, and recognition of its consequence. | |
| **CH01-B06/B07/B08** | `LOC02` & `LOC01` | Does the player return the form at the duty book, notice the PA cut and Lan's absence, then find their way back from the gate seam to the CH02 handoff? | |
| **CH02-B01–B04** | `LOC03` Room 101 & Store (R) | Start the C01 clock on entry to the PZ01 investigation. Does player hear slipper cue + glimpse Bé An? Do they connect `LIST` (3 beds) vs `SOCKETS` (4 outlets) vs `WHEELS` → roll steel cabinet (`PZ01`/`E12`) → discover Dismantled Bed 4 & `E11`? | |
| **CH02-B05** | `LOC03` Wall clipboard | Does the player use the clipboard, assemble the three drawing pieces, verify `C01` (`An · 101/4 → C01`) and notice that they can revise `F101`? | |
| **CH02-B06/B07** | `LOC03` → `LOC04` ER | Reaction to Gatekeeper (`CHAR005`) 2s `SFX-KEY` warning + voice `CH02-D05`; did they escape via Door A (main) or Door B (staff corridor) without panic-softlock? Reaction to ER bay & slice ending. | |

---

## 3. Post-Session Comprehension Interview (Scoring the 5/6 Gate)

Ask these 5 open-ended questions immediately after the participant finishes (before any debrief):

1. **Q1 (Contradiction Mechanic — Core Pass Criterion):** *"Theo bạn, sự khác nhau giữa không gian sáng đèn (lớp B năm 2005) và hiện trường hoang phế (lớp R năm 2012) có ý nghĩa gì? Lớp B có phải là sự thật lịch sử không?"*
   - **Pass criterion:** Player articulates that B reflects the *reported / paperwork version* (2005 facade) whereas physical proof lies in R (2012), and that the report hid/omitted Bed 4.
2. **Q2 (F101 Agency & Revision):** *"Khi ký phiếu F101, bạn có cảm thấy mình bị ép chọn một đáp án duy nhất không? Bạn có biết mình có thể để ngỏ hoặc sửa phiếu sau không?"*
   - Ask what would change in Room 101 after certifying zero and why. Score the explanation independently of the participant’s chosen route.
3. **Q3 (Patient and Threat):** *“Nhân vật nào dẫn đường, nhân vật nào đe dọa anh? Hành động nào khiến anh nhận ra?”* Score this outcome separately.
4. **Q4 (Emotional & Horror Tone):** *"Khoảnh khắc nào khiến bạn thấy ấn tượng hoặc bất an nhất (gặp Lan, hành lang tối, kho giường 4, hay Kẻ Giữ Cửa)? Có đoạn nào gây khó chịu thị giác hoặc quá tối không?"*
5. **Q5 (Navigation & Clarity):** *"Có lúc nào bạn không biết phải đi đâu, hoặc bỏ sót manh mối vì quá tối hay khó nhìn không? Gợi ý nhẹ (nếu xuất hiện) có hữu ích không?"*
6. **Q6 (Canon & Spoiler Check — `V22-Q03`):** *"Sau khi chơi xong, bạn nghĩ điều gì đã xảy ra với Lan và với bệnh viện?"* *(Confirm player did NOT see any premature CH04 reveal / spoiler.)*

---

## 4. Session Tally Sheet (Unfilled — Awaiting Human Runs)

| Session ID | Date | Playtime (min) | Optional `STAFF` Loop? | First F101 Route (Note: `Unverified` = Valid) | Refutation Attempts / Reasoning Quality | Hints Triggered | Revised F101? | Escape Door (A/B) | Perceived Tension (1–5) | Understood Contradiction (Y/N) | Softlocks | Comfort / Accessibility Issues |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| PT-01 | *Pending* | — | — | — | — | — | — | — | — | — | — | — |
| PT-02 | *Pending* | — | — | — | — | — | — | — | — | — | — | — |
| PT-03 | *Pending* | — | — | — | — | — | — | — | — | — | — | — |
| PT-04 | *Pending* | — | — | — | — | — | — | — | — | — | — | — |
| PT-05 | *Pending* | — | — | — | — | — | — | — | — | — | — | — |
| PT-06 | *Pending* | — | — | — | — | — | — | — | — | — | — | — |

### Core 01 outcome scores

| Session | PZ01 start | C01 verified | Elapsed | Moderator help? | C01 within 8 min unaided? | Explains F101 consequence? | Distinguishes guide from threat? |
| --- | --- | --- | --- | --- | --- | --- | --- |
| PT-01 | — | — | — | — | — | — | — |
| PT-02 | — | — | — | — | — | — | — |
| PT-03 | — | — | — | — | — | — | — |
| PT-04 | — | — | — | — | — | — | — |
| PT-05 | — | — | — | — | — | — | — |
| PT-06 | — | — | — | — | — | — | — |
| Total of 6 | | | | | … / 6 | … / 6 | … / 6 |

Count in-game hints in the session tally. Moderator help before C01 verification disqualifies that session from the unaided C01 threshold. The CH01 telemetry file ends at T10, so use the observation sheet to time C01 and the cameo.

**Current Tally:** `0 / 6` human sessions completed. **Gate `TASK010` remains OPEN.**
