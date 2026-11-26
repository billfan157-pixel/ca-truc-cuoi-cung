# Danh mục & Cấu trúc Tài liệu · Ca trực cuối cùng

**Chuẩn tham chiếu duy nhất (Single Source of Truth):** **Core v3.3.2 (NPR-01 Locked — Final Production Lock)** ngày 26/11/2026 (`10/10` SHA-256 khớp [SHA256SUMS.txt](SHA256SUMS.txt)).

Mọi tài liệu trong dự án được tổ chức thành **4 tầng rõ ràng** dựa trên khuôn mẫu của Core v3.3.2. Khi có xung đột nội dung, luôn áp dụng thứ tự ưu tiên từ Tầng 1 xuống Tầng 4 và tra cứu quyết định trong `07_Production_Database.xlsx → Decisions`.

---

## Tổng quan Thư mục Workspace

| Thư mục | Vai trò | Quy tắc quản trị |
| --- | --- | --- |
| `docs/` | Nguồn thiết kế chuẩn Core v3.3.2, hướng dẫn vận hành, đặc tả sống, hồ sơ nghiệm thu và lưu trữ | Không mở tài liệu dự thảo song song ngoài Core v3.3.2 (`DEC-NPR01-05`). |
| `.agents/skills/` | 6 skill chuyên môn vận hành theo vai trò (`production`, `gameplay`, `level`, `narrative`, `art-audio`, `qa`) | Định tuyến theo [AGENTS.md](../AGENTS.md) và [docs/agent/README.md](agent/README.md). |
| `Prototype/` | Project Unity 6.3 LTS (`6000.3.28f1`, URP 17.3 Forward+) & `ThirdPartyNotices.md` | Không xóa `Library/` của project chính khi chưa cần thiết; `HospitalAuthoring.cs` đọc trực tiếp `docs/prototype/layout.json`. |
| `ArtSource/` | Tệp nguồn chỉnh sửa Blender 4.5 (`.blend`) và texture nhân vật gốc | Nguồn gốc trước khi xuất FBX vào `Prototype/Assets/CTCC/Art/Meshes/`. |
| `tools/` | Bộ script dựng build, QA Windows, bake PBR/audio/character, đồng bộ dữ liệu Core 07 | Không đổi tên script khi chưa kiểm tra phụ thuộc nội bộ (`verify_windows.ps1` & `measure_m2.ps1` gọi `focus_m1p_player.ps1`). |
| `output/` | Đầu ra build, benchmark, gói phát hành cục bộ (được `.gitignore` bỏ qua) | Bảo vệ gói `c459aba` (M2), `c90a2eb` (M3.2), `material-staging/source/`, `audio-tools/` và dữ liệu save. |

---

## Tầng 1 — Core v3.3.2 · Single Source of Truth (Đóng băng · Bất biến)

> Không sửa nội dung thiết kế đã khóa khi chưa có quyết định mới qua luồng `07/Decisions`. Kiểm toàn vẹn bằng `python docs/agent/project_docs.py check` và `python docs/agent/project_docs.py hashes`.

| Tệp | Phạm vi sở hữu |
| --- | --- |
| [01_Game_Design_Document.docx](01_Game_Design_Document.docx) | Vòng chơi, UX, phạm vi sản phẩm, hợp đồng trải nghiệm v3.3.2 (§6) |
| [02_Narrative_Bible.docx](02_Narrative_Bible.docx) | Canon `CAN01–09`, `WR01–08`, timeline 1987–2013, 8 chương, 4 kết thúc |
| [03_World_Architecture_Bible.docx](03_World_Architecture_Bible.docx) | Kiến trúc quần thể An Sinh (`Campus-v0`, `DEC-WORLD-01`), cao độ, liên tục 2005/2012 |
| [04_Level_Chapter_Design.docx](04_Level_Chapter_Design.docx) | Tuyến chơi `CH01–08`, trigger, checkpoint, fallback và phần V3.1–V3.3.2 |
| [05_Art_Audio_Bible.docx](05_Art_Audio_Bible.docx) | Ngôn ngữ hình ảnh R/B/M, kit modular, âm thanh/VO/mixer, tiêu chí giấy phép |
| [06_Technical_Design_Document.docx](06_Technical_Design_Document.docx) | Kiến trúc module, Save Schema v7, giao dịch lớp, snapshot `PZ10` & export |
| [07_Production_Database.xlsx](07_Production_Database.xlsx) | 27 sheet dữ liệu chuẩn: `Dialogue`, `Evidence Specs`, `Events`, `Chapter gates`, `Decisions`, `Roadmap`, `Backlog` (96 task), `QA` |
| [RELEASE-NOTES.md](RELEASE-NOTES.md) | Tóm tắt bản chốt khóa Core v3.3.2 (`DEC-NPR01-01–05`) & hành động ưu tiên |
| [CHANGELOG.md](CHANGELOG.md) | Nhật ký thay đổi từ Core v3.0 → v3.3.2 |
| [CANON-AUDIT.md](CANON-AUDIT.md) | Biên bản kiểm toán canon và tính liên tục `NPR01-DOC01` |
| [SHA256SUMS.txt](SHA256SUMS.txt) | Bản kê SHA-256 khóa 10/10 tệp gói Core v3.3.2 |

---

## Tầng 2 — Tài liệu Vận hành, Đặc tả Sống & Hồ sơ Hiện hành

### Vận hành & Điều phối
- [AGENTS.md](../AGENTS.md) — Quy tắc vận hành workspace và định tuyến 6 skill.
- [agent/README.md](agent/README.md) — Bản đồ nguồn sở hữu, cơ chế giải quyết chênh lệch và cổng M0–M5.
- [agent/project_docs.py](agent/project_docs.py) — Công cụ CLI tra cứu trực tiếp Core 01–07 (`status`, `task`, `doc`, `sheet`, `check`, `hashes`).

### Đặc tả Sống & Dữ liệu Kỹ thuật đang đọc trực tiếp
- [ch01r/walkthrough-gameplay-B01-B08-investigation-v1.md](ch01r/walkthrough-gameplay-B01-B08-investigation-v1.md) — Walkthrough hiện hành CH01 B01–B08 (`DEC-CH01R-04`).
- [prototype/layout.json](prototype/layout.json) — Tọa độ cốt lõi (được `Prototype/Assets/CTCC/Editor/HospitalAuthoring.cs` đọc trực tiếp).
- [prototype/campus-layout-concept.json](prototype/campus-layout-concept.json) — Dữ liệu tọa độ 6 khối `Campus-v1`.
- [m2/design-lock-errata.md](m2/design-lock-errata.md) — Các đính chính triển khai đã duyệt (`EVT026`, hiển thị `E03`, cửa xả sân `S1`).
- [Prototype/ThirdPartyNotices.md](../Prototype/ThirdPartyNotices.md) — Hồ sơ nguồn tài nguyên & giấy phép bên thứ ba của bản dựng.

### Cổng Con người Đang mở (Chặn đóng M2 & Art Lock)
- [m2/playtest-kit-task010.md](m2/playtest-kit-task010.md) — Bộ chạy playtest mù 6 người chơi mới trên `c459aba` (`TASK010`, hiện `0/6`).
- [m2/specialist-review-dossiers.md](m2/specialist-review-dossiers.md) — Hồ sơ thẩm định chuyên gia duy nhất cho `TASK026`, `TASK027` và `TASK028` (hiện `0/3`).

### Báo cáo Kỹ thuật Mốc Hiện hành
- [m2/m2-final-candidate-report.md](m2/m2-final-candidate-report.md) — Hồ sơ ứng viên kỹ thuật M2-FINAL (`c459aba`) & bằng chứng tại `docs/m2/evidence/m2-final-candidate/`.
- [campus/an-sinh-full-campus-expansion-report.md](campus/an-sinh-full-campus-expansion-report.md) — Báo cáo mở rộng quần thể 6 khối Campus-v1 & 3 bản vẽ 2D (`campus-master-plan.png`, `campus-floors-B1-3F.png`, `campus-sections.png`).
- [m3/m32-implementation-and-acceptance-report.md](m3/m32-implementation-and-acceptance-report.md) — Hồ sơ ứng viên kỹ thuật M3.2 (`c90a2eb`, AI `TASK011` & CH03–CH04 `TASK012`).
- [m3/m33-phase-a-audit-and-test-plan.md](m3/m33-phase-a-audit-and-test-plan.md) — Kiểm toán Phase A & kế hoạch kiểm thử M3.3 (CH05 `TASK013-A/B/C`).
- [ch01r/art-production.md](ch01r/art-production.md) — Ghi nhận đợt nâng cấp mỹ thuật CH01 trên working tree hiện tại.

---

## Tầng 3 — Hồ sơ Tham chiếu Nền & Nghiệm thu Mốc cũ (Giữ đường dẫn theo Core v3.3.2)

> Các tệp dưới đây thuộc mốc trước hoặc là tư liệu nghiên cứu nền, **giữ nguyên vị trí vật lý** vì được trích dẫn trực tiếp trong các tệp Word/Excel của bộ Core v3.3.2 đang đóng băng.

| Nhóm | Tài liệu | Vai trò truy vết |
| --- | --- | --- |
| **M0 / M1 Prototype** | [prototype/engine.md](prototype/engine.md), [prototype/research.md](prototype/research.md), [prototype/layout.md](prototype/layout.md), [prototype/verification.md](prototype/verification.md), [prototype/m1p-acceptance.md](prototype/m1p-acceptance.md) | Chốt Unity/URP (`TASK029`/`DEC-V2-04`), register nghiên cứu `R01–R41` (`TASK023`), mặt cắt nháp (`TASK022`), nghiệm thu M0-R & M1-P (`TASK025`). |
| **CH01-R** | [ch01r/acceptance.md](ch01r/acceptance.md), [ch01r/puzzle-dependency-graph.md](ch01r/puzzle-dependency-graph.md), [ch01r/audio-additions.json](ch01r/audio-additions.json) | Hồ sơ tái cấu trúc CH01-R và đồ thị phụ thuộc suy luận F101 (Core 03 & Core 07 trích dẫn). |
| **M2 Nền & Nhân vật** | [m2/research-and-art-direction.md](m2/research-and-art-direction.md), [m2/audio-research-and-pipeline.md](m2/audio-research-and-pipeline.md), [m2/m2c1-acceptance.md](m2/m2c1-acceptance.md), [m2/acceptance.md](m2/acceptance.md), [m2/m2c-acceptance.md](m2/m2c-acceptance.md) | Register `R42–R50`, quy trình âm thanh/giấy phép VieNeu và các mốc M2-A/M2-C/M2-C.1 (Core 05/06/07 trích dẫn). *Lưu ý: `m2/acceptance.md` (M2-A) và `m2/m2c-acceptance.md` (M2-C) là bản ghi lịch sử có liên kết tới thư mục `output/m2a/` và `output/m2c/` không còn lưu trên máy cục bộ.* |
| **M3.1** | [m3/m31-acceptance.md](m3/m31-acceptance.md) | Bàn giao kỹ thuật mốc M3.1 (`05a5bfa`, Core 07 trích dẫn). |

---

## Tầng 4 — Lưu trữ Lịch sử (`docs/archive/`)

> Các tài liệu trung gian, audit mốc cũ hoặc bản đã được hợp nhất/thay thế bởi Tầng 2. Chỉ dùng để tra cứu lịch sử thiết kế và truy vết thay đổi.

| Mục lưu trữ | Nội dung | Đã được thay thế / đóng bởi |
| --- | --- | --- |
| [archive/m1-to-m2/m2-scene-handoff.md](archive/m1-to-m2/m2-scene-handoff.md) | Kế hoạch ranh giới scene additive M1 → M2 | Triển khai đầy đủ từ M2-A đến Campus-v1 |
| [archive/ch01-history/human-review-checklist.md](archive/ch01-history/human-review-checklist.md) | Phiếu kiểm duyệt chuyên gia riêng cho CH01 | Hợp nhất vào [m2/specialist-review-dossiers.md](m2/specialist-review-dossiers.md) |
| [archive/ch01-history/rebirth-ii/rebirth-ii-f101-journal.md](archive/ch01-history/rebirth-ii/rebirth-ii-f101-journal.md) | Báo cáo CH01 Rebirth II Prompt 1 (F101 & sổ trực) | Tích hợp vào [m2/m2-final-candidate-report.md](m2/m2-final-candidate-report.md) |
| [archive/ch01-history/rebirth-ii/rebirth-ii-world.md](archive/ch01-history/rebirth-ii/rebirth-ii-world.md) | Báo cáo CH01 Rebirth II Prompt 2 (thế giới & 14 điểm quan sát) | Tích hợp vào [m2/m2-final-candidate-report.md](m2/m2-final-candidate-report.md) |
| [archive/m2-history/m2c-character-audit-and-pipeline.md](archive/m2-history/m2c-character-audit-and-pipeline.md) | Audit nhân vật M2-A → M2-C | Đã đóng kỹ thuật qua M2-C.1 và M3.0 |
| [archive/m3-history/m30/m30-foundation-and-art-lock.md](archive/m3-history/m30/m30-foundation-and-art-lock.md) | Hồ sơ M3.0 (Character Candidate B & CH03 sơ khởi) | Kế thừa bởi M3.1 và [m3/m32-implementation-and-acceptance-report.md](m3/m32-implementation-and-acceptance-report.md) |
| [archive/m3-history/m31/m31-audit.md](archive/m3-history/m31/m31-audit.md) | Audit độc lập mốc M3.0 (`31e06c2`) | Đã xử lý tại M3.1 và M3.2 |
| [archive/m3-history/m31/m31-world.md](archive/m3-history/m31/m31-world.md) | Mặt cắt và chương trình không gian sơ bộ M3.1 | Nâng cấp bởi [campus/an-sinh-full-campus-expansion-report.md](campus/an-sinh-full-campus-expansion-report.md) |
| [archive/m3-history/m32/m32-phase-a-audit-and-test-plan.md](archive/m3-history/m32/m32-phase-a-audit-and-test-plan.md) | Kế hoạch & kiểm toán Phase A của M3.2 | Đã đóng bởi [m3/m32-implementation-and-acceptance-report.md](m3/m32-implementation-and-acceptance-report.md) |
| [archive/ops/output-inventory-2026-11-25.md](archive/ops/output-inventory-2026-11-25.md) | Ảnh chụp kiểm kê dung lượng `output/` ngày 25/11/2026 | Snapshot vận hành cũ trước đợt dọn ngày 26/11/2026 |
