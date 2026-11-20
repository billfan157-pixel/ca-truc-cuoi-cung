# Ca Trực Cuối Cùng — Human Playtest Kit (`TASK010`)

- **Milestone / Gate:** M2 Vertical Slice (`TASK010`)
- **Status:** **OPEN — AWAITING HUMAN PARTICIPANTS (0/6 completed sessions)**
- **Target Sample:** ≥ 6 new players unfamiliar with the codebase or Core Design System
- **Pass Threshold (`Core 07 / Backlog TASK010` + CH01-R Investigative Reconstruction):**
  1. ≥ **5/6 players** correctly understand the contradiction mechanic (**B is the 2005 report facade, not historical truth; R is the 2012 physical site; F101 can be Affirmed, left Unverified, or Refuted through evidence-based reasoning; E12 & E11 prove Bed 4 / Bé An C01**) without developer hints.
  2. **Epistemic Fairness:** Leaving F101 **`Unverified` is NEVER scored as failure** — it is a canonical, responsible decision when a player withholds certification without sufficient grounds. For players who attempt `Refuted`, observe whether they distinguish Bed-4 relevant evidence (`OBS101A`, `OBS101B`) from the Bed-3 routine medication log (`OBS101D`) and avoid overclaiming patient presence or deliberate falsification.
  3. **0 critical softlocks** across all sessions (including recovery after rejected F101 refutation arguments, optional `STAFF` exploration loop, and interactive drawers/doors).
  4. **Average completion time** falls within **20–30 minutes** (targeting ~25 min for CH01 + early CH02 handoff across LOC01–04).
  5. Accessibility & comfort: zero unmitigated motion-sickness dropouts; directional captions, Large-Print document view, single-press click/toggle alternatives (vs optional hold-and-drag), and Reduced Motion / Story Mode verified when used.

> [!IMPORTANT]
> Per project truthfulness rules, **no synthetic, AI-simulated, or fabricated playtest sessions are recorded here.** This kit is ready to run directly on the packaged M2 Windows build (`CaTrucCuoiCung.exe`).

---

## 1. Pre-Session Protocol & Content Note

1. **Hardware Setup:** Reference target or equivalent Windows 11 PC at `1920x1080`, `Medium` preset, headphones or stereo speakers enabled, mouse + keyboard.
2. **Content Notice (Read aloud / shown on Main Menu):**
   - Fictional Vietnamese hospital setting (*Bệnh viện An Sinh*, 1987–2012).
   - Themes include grief, memory of a past hospital fire, and psychological tension (no extreme gore, no lethal CH01 chase).
   - Inform participant they may enable **Giảm chuyển động (Reduced Motion)**, **Chế độ Cốt truyện (Story Mode)**, **Chú thích âm thanh (Directional Captions)**, or **Tăng cỡ chữ (130%)** at any time, or pause/stop without penalty.
3. **Neutral Briefing (Zero Spoilers):**
   - *"Bạn sẽ chơi bản Vertical Slice dài khoảng 20–30 phút của game kinh dị điều tra Ca Trực Cuối Cùng. Hãy chơi theo cách tự nhiên nhất. Nếu bí hoặc cảm thấy khó hiểu ở đâu, hãy nói thành tiếng suy nghĩ của bạn; điều phối viên sẽ không gợi ý trừ khi bạn thực sự mắc kẹt quá 3 phút."*
   - **Never explain R vs B, Lan, F101 routes, or Bed 4 beforehand.**

---

## 2. Live Observation Rubric (Moderator Checklist)

| Beat | Space | Key Signals to Observe | Pass / Friction Notes |
| --- | --- | --- | --- |
| **CH01-B01/B02** | `LOC01` Exterior | Does player read Bảy's letter (`E10a`), notice motorbike/gatehouse/rain mood, unlock gate, and spot Lan's silhouette through reception glass? | |
| **CH01-B03** | `LOC02` Reception | Emotional reaction to meeting Lan live in-world (unblocked by modal); did player engage optional `LAN_TALK` dialogue or open Reception desk drawer (`DRAWER-DESK` / `OBS_RECEPTION_DRAWER`) or notice stopped `03:17` clock (`FS01`)? Clarity of taking `BOARD` (`F101`). | |
| **CH01-B04/B05** | `STAFF` Alcove, `HALL` & `LOC03` Room 101 (B) | Did player discover the optional `STAFF` Nursing Alcove (`OBS_CORRIDOR_NOTICE`, `DRAWER-NURSE` / `OBS_SHIFT_LOG`) or open `DOOR-STAFF`? In Room 101, in what order did they examine `OBS101C` (Bed-2 transfer), `DRAWER-BED3`/`OBS101D` (Bed-3 meds), `CURTAIN-101`, `OBS101A` (Bed-4 traces), and `OBS101B` (Bed-4 card + back flip)? Did they use the Evidence Journal side-by-side comparison? Which F101 decision (`Affirmed` / `Unverified` / `Refuted`) did they choose first, how many reasoning attempts if `Refuted`, and **why**? Did they notice the immediate Layer-B visual/audio consequence (`deniedBay` / lingering curtain / `refutedBay` + wall clipboard stamp)? | |
| **CH01-B06/B07/B08** | `LOC02` & `LOC01` | Reaction to `HE04` PA cut, `HE05` power drop & Lan's calm absence (no monster jumpscare), `HE06` gate seam loop once, and handoff to CH02. | |
| **CH02-B01–B04** | `LOC03` Room 101 & Store (R) | Does player hear slipper cue + glimpse Bé An? Do they connect `LIST` (3 beds) vs `SOCKETS` (4 outlets) vs `WHEELS` → roll steel cabinet (`PZ01`/`E12`) → discover Dismantled Bed 4 & `E11`? | |
| **CH02-B05** | `LOC03` Anchor `AP04` | Does player assemble the 3 drawing pieces, verify `C01` (`An · 101/4 → C01`), and notice the option to revise `F101`? | |
| **CH02-B06/B07** | `LOC03` → `LOC04` ER | Reaction to Gatekeeper (`CHAR005`) 2s `SFX-KEY` warning + voice `CH02-D05`; did they escape via Door A (main) or Door B (staff corridor) without panic-softlock? Reaction to ER bay & slice ending. | |

---

## 3. Post-Session Comprehension Interview (Scoring the 5/6 Gate)

Ask these 5 open-ended questions immediately after the participant finishes (before any debrief):

1. **Q1 (Contradiction Mechanic — Core Pass Criterion):** *"Theo bạn, sự khác nhau giữa không gian sáng đèn (lớp B năm 2005) và hiện trường hoang phế (lớp R năm 2012) có ý nghĩa gì? Lớp B có phải là sự thật lịch sử không?"*
   - **Pass criterion:** Player articulates that B reflects the *reported / paperwork version* (2005 facade) whereas physical proof lies in R (2012), and that the report hid/omitted Bed 4.
2. **Q2 (F101 Agency & Revision):** *"Khi ký phiếu F101, bạn có cảm thấy mình bị ép chọn một đáp án duy nhất không? Bạn có biết mình có thể để ngỏ hoặc sửa phiếu sau không?"*
3. **Q3 (Emotional & Horror Tone):** *"Khoảnh khắc nào khiến bạn thấy ấn tượng hoặc bất an nhất (gặp Lan, hành lang tối, kho giường 4, hay Kẻ Giữ Cửa)? Có đoạn nào gây khó chịu thị giác hoặc quá tối không?"*
4. **Q4 (Navigation & Clarity):** *"Có lúc nào bạn không biết phải đi đâu, hoặc bỏ sót manh mối vì quá tối hay khó nhìn không? Gợi ý nhẹ (nếu xuất hiện) có hữu ích không?"*
5. **Q5 (Canon & Spoiler Check — `V22-Q03`):** *"Sau khi chơi xong, bạn nghĩ điều gì đã xảy ra với Lan và với bệnh viện?"* *(Confirm player did NOT see any premature CH04 reveal / spoiler.)*

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

**Current Tally:** `0 / 6` human sessions completed. **Gate `TASK010` remains OPEN.**
