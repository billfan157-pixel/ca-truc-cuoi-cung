# Bộ Core dự thảo NPR-01

**Trạng thái ngày 25/11/2026.** Chủ dự án đã duyệt hướng A/B và cho phép soạn bộ Core nâng cấp dự thảo. Câu thoại, phương án canon và phương án sản xuất tiếp tục theo từng điều kiện trong [Decision Register](decision-register.md). Bộ chính thức là [Core v3.3.1 Final Clean tại GitHub main `3c0b955`](https://github.com/billfan157-pixel/ca-truc-cuoi-cung/tree/3c0b955de95bcf39853c30e2669b2bac24ad969e/docs). [Nguồn sở hữu](../agent/README.md#bảy-nguồn-sở-hữu) · [SHA-256 của baseline](../SHA256SUMS.txt)

## Bộ Core nâng cấp

Sáu bản Word gồm trang hướng dẫn NPR-01 và nội dung Core để đối chiếu. Các sửa trong thân tài liệu được ghi ở Changelog. Workbook giữ 27 sheet và có các dòng NPR-01 trong Decisions/Changelog. Bảy tệp nằm ngay tại đường dẫn chuẩn `docs/01…07` trên nhánh NPR-01. Mở qua README dự án hoặc `project_docs.py` sẽ dùng trực tiếp bộ Core trên nhánh này.

| Core | Nội dung chính |
| --- | --- |
| [01 · Game Design](../01_Game_Design_Document.docx) | Quyền lựa chọn, phạm vi M2 và đếm task theo Backlog. |
| [02 · Narrative Bible](../02_Narrative_Bible.docx) | Ba tầng thông tin, hai manifest, Vĩnh–Lan và câu thử PZ10. |
| [03 · World Architecture](../03_World_Architecture_Bible.docx) | Đường lưu LOC08/10, phạm vi hồ sơ ứng cứu và topology. |
| [04 · Level & Chapter](../04_Level_Chapter_Design.docx) | AN05, CH07/08, CH07-D19, R1/R3 và coda END03. |
| [05 · Art & Audio](../05_Art_Audio_Bible.docx) | Khung duyệt visual, cinematic, audio và asset handoff. |
| [06 · Technical Design](../06_Technical_Design_Document.docx) | Hợp đồng gameplay, locale, ngân sách và build provenance. |
| [07 · Production Database](../07_Production_Database.xlsx) | Dialogue, PZ10, Ending và Provenance dự thảo. Dashboard có cache theo ngày, giữ công thức và các nguồn nghiệm thu. |

## Tài liệu dùng khi review

| Tài liệu | Mục đích |
| --- | --- |
| [Canon Authority Index](canon-authority-index.md) | Bản đồ sở hữu, dữ kiện khóa, đề xuất, giới hạn và chờ chuyên gia. |
| [Đề xuất canon](canon-proposals.md) | Ảnh E01-B, phạm vi công bố và chuỗi lưu 2006–2012. |
| [Decision Register và các mục chờ](decision-register.md) | Hướng đã duyệt, quyết định tại đích, người cần xác nhận và căn cứ để chốt. |
| [Changelog](changelog.md) · [So sánh câu chữ](proposed-diffs.md) | Sửa đổi theo ID, vị trí và lịch sử gói. |
| [Narrative Impact Matrix](narrative-impact-matrix.md) | Tác động lên tri thức, reveal và giới hạn chứng cứ PZ10. |
| [Gameplay & Technical Impact Matrix](gameplay-technical-impact-matrix.md) | Quan hệ Core 01–07, ảnh hưởng M2/M3.2, các bất biến và điều kiện tích hợp. |
| [Live Production Ledger](production-ledger.md) | Task, build, bằng chứng và bốn mức trạng thái. |
| [Tài liệu sản xuất nội dung](creative-production.md) | Chỉ mục Visual, Cinematic, Environmental, Character, Audio, phạm vi duyệt. |
| [Kế hoạch kỹ thuật](technical-plan.md) · [Bản thử localization](localization-prototype.md) | Vietnamese-first, externalization, performance và build size. |
| [Kiểm toán chéo](cross-audit.md) · [Tác động QA](qa-impact.md) · [Kiểm toán NPR-01 ban đầu](audit.md) | Đối chiếu tài liệu, workbook và căn cứ build. |

## Thứ tự xem xét

1. Đọc [đề xuất canon](canon-proposals.md) và [Decision Register](decision-register.md). Chốt mức ảnh E01-B, khoảng mở công bố và phạm vi provenance sau ý kiến TASK028.
2. Xem các lựa chọn thoại/diễn xuất và thử đọc PZ10 với đúng cặp chứng cứ. Chọn nhịp Minh–Lan có thể dựng mà vẫn giữ guard, route và chuỗi tiết lộ.
3. EVT026 tiếp tục dùng [errata đã duyệt cho hồ sơ TASK027](../m2/design-lock-errata.md#evt026--trình-tự-ứng-cứu-trong-hồ-sơ-task027). Chờ thẩm định chi tiết ứng cứu trước quyết định đồng bộ chính thức.
4. Khi từng mục được duyệt, ghi xác nhận, đích đồng bộ, QA và build. TASK010 và TASK026–028 theo cổng của Core 07. [Ứng viên M2](../m2/m2-final-candidate-report.md) tiếp tục sở hữu phạm vi bằng chứng của `c459aba`.
