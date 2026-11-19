# Ca Trực Cuối Cùng — M2 Specialist Review Dossiers (`TASK026`, `TASK027`, `TASK028`)

- **Status:** **OPEN — AWAITING HUMAN SPECIALIST SIGN-OFF**
- **Scope:** Condensed CH01 & early CH02 across `LOC01–04` (branch `codex/m2-a-production-readiness`, Unity 6.3 LTS and Blender 4.5 assets).
- **Evidence Package Linked for Reviewers:**
  - Research and art direction in [research-and-art-direction.md](research-and-art-direction.md).
  - Revision, playable build and captures in [the M2-A acceptance record](acceptance.md).

> [!IMPORTANT]
> Per `AGENTS.md` and M2 Definition of Done, **AI self-review does NOT substitute for human domain specialists.** Each dossier below packages exact architectural metrics, historical assumptions (`V/I/F/U`), and visual assets so external reviewers can inspect and sign off directly.

---

## 1. `TASK026` — Vietnamese Hospital Architecture, Medical Equipment & Attire Review

- **Target Specialist:** Vietnamese healthcare architect / retired hospital nurse or clinician familiar with 1987–2012 provincial/district general hospitals (*bệnh viện đa khoa tuyến tỉnh/huyện*).
- **Items for Verification:**
  1. **Exterior & Reception (`LOC01`–`LOC02`):**
     - 3-story 1987 reinforced-concrete facade with horizontal *ô văng* sun-shading louvers, *gạch bông gió* breeze-block stairwell tower, rooftop concrete water tank, brick gatehouse (*nhà bảo vệ*), terrazzo courtyard benches, glazed timber-and-steel reception partition (*quầy tiếp đón có ô kính giao dịch*), and 2005 wall calendar.
     - *Open Uncertainty (`U-01`):* Confirm proportion & typography of fictional *Bệnh viện Đa khoa An Sinh · Thành lập 1987* gate plaque and *Tiếp đón · Trực đêm* sign match 1990s–2005 provincial hospital style without resembling any specific real hospital (`AR03`).
  2. **Ward 101 & Store Room (`LOC03`):**
     - `3.60 m` floor-to-ceiling height, `1.15 m` teal-green oil-painted dado wainscot with limewash upper walls, terrazzo cove skirting, 3 rolled-steel casement windows with concrete rain hoods & vertical security grilles, surface-mounted conduit & double rocker outlets numbered `1–4`, stainless IV stands, vacuum thermos flask + blue-rimmed enamel mug, rolled sedge mats (*chiếu cói*), yellow honeycomb slippers (*dép tổ ong*), glass-and-louver steel medical cabinet, and dismantled 4th bed frame in adjacent linen/bed store.
  3. **Emergency Department (`LOC04`):**
     - `1.40 m` double swing doors with frosted top panes & stainless push bars, illuminated red *CẤP CỨU* light boxes, hydraulic transport stretcher, vital-signs ECG monitor cart, green medical oxygen cylinder cart (`O₂`), stainless instrument trolley, wall gas manifold (`O₂ / VAC / AIR`).
     - *Open Uncertainty (`U-02`):* Confirm 2005-era vital-signs monitor & wall gas manifold colorway/placing fits a provincial Vietnamese hospital ER (2005 vs 2012 state).
  4. **Nurse Attire (`CHAR002` Trần Ngọc Lan):**
     - Knee-length off-white poplin *áo blouse* with standing collar & muted teal piping trim per MoH *Quy chế bệnh viện* `1895/1997/QĐ-BYT`, dark trousers, flat closed shoes, neatly pinned low bun, scratched staff ID badge.
     - *Open Uncertainty (`U-03`):* Confirm lapel/standing collar & badge placement match ~2005 Vietnamese nursing convention cleanly.
- **Reviewer Sign-Off:** `PENDING HUMAN REVIEW` | Name/Credentials: `________________` | Date: `________` | Verdict: `[ ] Pass  [ ] Pass w/ Fixes  [ ] Revise`

---

## 2. `TASK027` — Fire Safety, Egress & Post-Incident Plausibility Review

- **Target Specialist:** Fire safety engineer / PCCC specialist.
- **Items for Verification:**
  1. **Clear Egress Widths (`Core 03` / `layout.json` verified by Unity NavMesh & stretcher proxy):**
     - Main Corridor width: `3.00 m` clear (`x ∈ [2.5, 5.5]`).
     - Side Staff Corridor width: `2.10 m` clear (`x ∈ [-1.0, 1.1]`).
     - Room 101 doorway (`GATE-101`): `1.20 m` clear — passes `0.72 m × 1.98 m` stretcher proxy cleanly with `0` collision.
     - Store Room doorway (`GATE-STORE`): `1.22 m` clear (initially blocked in R by rolling medical cabinet until `PZ01`).
     - Emergency dual entrances (`GATE-CAPCUU` Door A & `GATE-CAPCUU-B` Door B): `1.40 m` clear each — dual escape routes verified for `CH02-B06`.
  2. **Environmental Storytelling & Signage Restraint:**
     - Corridor wall-mounted red fire extinguisher (`CTCC_PROP_FireExtinguisher`), closed stairwell barrier (`Sign_KhuBClosed`), red *CẤP CỨU* light boxes, and scorched-blouse motif on the psychological manifestation (`CHAR005` Gatekeeper) avoid sensationalized active flames or implausible building collapse in an intact reinforced-concrete 1987 structure.
     - *Open Uncertainty (`U-04`):* Confirm that the 7-year post-fire deterioration in Wing A/Room 101 (monsoon seepage, efflorescence, rust, peeling limewash, subtle smoke discoloration near vents) is physically consistent with a localized 2005 ward/wing fire followed by abandonment through 2012.
- **Reviewer Sign-Off:** `PENDING HUMAN REVIEW` | Name/Credentials: `________________` | Date: `________` | Verdict: `[ ] Pass  [ ] Pass w/ Fixes  [ ] Revise`

---

## 3. `TASK028` — Medical Provenance, Paperwork Realism & Ethical Representation Review

- **Target Specialist:** Medical records / clinical ethics advisor & Vietnamese narrative/legal reviewer.
- **Items for Verification:**
  1. **Fictionality & Non-Defamation (`AR03`):**
     - *Bệnh viện Đa khoa An Sinh*, address, staff IDs, forms (`F101`, `Phiếu chuyển 101/4`, `Bản kê phòng bệnh`, `Thẻ bàn giao giường 4`), and demolition notice are 100% original fictional creations; zero real hospital logos, photographs, or patient names appear anywhere in the build or art source.
  2. **Clinical Paperwork Plausibility:**
     - `F101` handover form (*Sổ bàn giao · 18/11/2005*), `OBS101B` bed card (*Giường 4 · CHƯA BÀN GIAO*), `LIST` 3-bed inventory vs 4-bed physical room, and `TRANSFER` internal note (*17/11/2005 · An · Giường 4 phòng 101, khám sốt tạm → Trở lại C01*) reflect plausible administrative discrepancy without fabricating real MoH legal serial numbers.
  3. **Dignity of Patients & Trauma-Informed Horror (`Core 01 / Core 02`):**
     - Pediatric patient **C01 Bé An** is depicted with tenderness and restraint (translucent soft memory apparition asking *"Chú có thấy cửa nhà con không?"*, yellow honeycomb slippers, child's crayon drawing) — zero grotesque horror imagery, zero exploitation of child suffering.
     - **Trần Ngọc Lan (`CHAR002`)** retains full humanity and warmth — no demonic transformation or jumpscare vanish; she is simply absent after the 2005 facade collapses.
     - Player retains full agency (3 F101 routes including *Chưa đủ thông tin* + revision ability), upfront content warning on Main Menu, Reduced Motion, Directional Captions, and Story Mode safety toggle.
     - *Open Uncertainty (`U-05`):* Confirm Vietnamese phrasing of `F101`, `TRANSFER`, and content warning meets ethical and clinical-tone standards for public release.
- **Reviewer Sign-Off:** `PENDING HUMAN REVIEW` | Name/Credentials: `________________` | Date: `________` | Verdict: `[ ] Pass  [ ] Pass w/ Fixes  [ ] Revise`
