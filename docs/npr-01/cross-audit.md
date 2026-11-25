# Kiểm toán chéo bộ Core NPR-01

Đối chiếu ngày 25/11/2026. Phạm vi gồm tài liệu, tính toàn vẹn, công thức, xuất Word, liên kết và bằng chứng build sẵn có. Nguồn khóa tại GitHub `main` `3c0b955`. [Kiểm toán nội dung ban đầu](audit.md) giữ kết quả của vòng NPR-01 trước khi chủ dự án duyệt hướng.

| Kiểm tra | Căn cứ và kết quả |
| --- | --- |
| Baseline | 10/10 tệp từ commit `3c0b955` khớp bản kê phát hành. Trên NPR-01, bảy tệp tại `docs/01…07` có cùng nội dung byte với bản review `4f4eaf7`. Diff ngoài bảy tệp Office gồm hồ sơ review và liên kết hướng dẫn. |
| Core 01–06 | So OOXML với baseline. Chỉ có trang hướng dẫn, numbering kỹ thuật và 11 vị trí văn bản đã khai báo. Giữ bảng, hình, section sau và quan hệ gốc. Word xuất 118 trang gồm 6 trang hướng dẫn mới, mỗi file tăng đúng một trang. Rà trang mới/trang đổi và so với bản nguồn. |
| Core 07 và DAG | 27 sheet, 96 Backlog. 143 Dialogue, 28 CH01, 18 Evidence Specs, 27 Timeline. Giữ Roadmap, Backlog, Chapter gates, Events, Puzzle specs, QA, Narrative QA và Evidence Specs. Hai bảng Decisions, Changelog có 11 dòng NPR-01 đối ứng và bao phủ đúng đến dòng 75/59. |
| Dashboard | Tất cả 38 công thức giữ nguyên và có cache sau khi tính. Dashboard hiển thị 96 task, 25 hoàn thành, 0 DEC/TQA đúng phạm vi, 143 dòng thoại. Excel kiểm tính lại bằng một task và một quyết định giả lập trong phiên đọc, kết quả tăng lên 97 và 1. Phiên kiểm đóng mà không lưu dữ liệu thử. |
| Câu thoại và PZ10 | So sáu câu E108/E112/E113/E116/E117/E132 với bản Core 04 dự thảo. R1/R3 còn khớp Core 02 và PZ10 Matrix. Năm cặp, ID, Dialogue Context và các bảng điều kiện không đổi. Giới hạn kết luận xem [ma trận narrative](narrative-impact-matrix.md#vết-chứng-minh-pz10). |
| Canon và thời điểm tiết lộ | Ba tầng công bố, hai nguồn export, Bảy ở cả bốn END; E08 nonexport; C12 và N-04 chưa định danh, B08 chỉ END04. Phương án ảnh và provenance B được đánh dấu đề xuất ở [hồ sơ canon](canon-proposals.md). Phạm vi EVT026 là dossier TASK027. |
| M2 và M3.2 | Hash cục bộ EXE/DLL/ZIP M2 khớp `c459aba`. ZIP ứng viên M3.2 khớp manifest `c90a2eb`. Bằng chứng Phase F trỏ đúng binary; revision dossier và build được ghi riêng. [Ledger](production-ledger.md) |
| Mức nghiệm thu | QA game dùng hồ sơ sẵn có. Ca mới về thoại, cue, visual, UX locale và các nhịp tùy chọn cần build tương ứng sau duyệt. M2, art lock và M3 Alpha còn mở. TASK010 0/6 theo hồ sơ gần nhất; TASK026–028 chờ biên bản. |

## Cách kiểm lại

1. Chạy `python docs/agent/project_docs.py check` ở root để đọc trực tiếp Core dự thảo. Đối chiếu các tệp tại `3c0b955` với bản kê `docs/SHA256SUMS.txt` của lần phát hành. Kiểm `git diff 3c0b955..HEAD` theo phạm vi. Đối chiếu Word, workbook và QA với [Changelog](changelog.md).
2. Mở workbook dự thảo bằng Excel hoặc trình xem hỗ trợ cache. Đọc Dashboard B9/B10/B12 và F13, Dialogue và PZ10 Matrix. So bảng Decisions/Changelog và đưa bất kỳ thử thay đổi formula input nào vào phiên không lưu.
3. Dùng báo cáo M2/M3.2 theo revision. 9 scene trên GitHub main khác bản bake M2. Đây là chênh lệch provenance của candidate M2. Phase F ghi M3.2 có bake sạch riêng. Mỗi lượt benchmark giữ counter và tần số riêng.
4. Chuyển góp ý sang [Decision Register](decision-register.md) cùng dòng cần xử lý, ca ở [QA impact](qa-impact.md) và [ma trận gameplay](gameplay-technical-impact-matrix.md). Các bước cần owner hoặc chuyên gia ghi dấu vết ở đó.

Bộ Word được render bằng Microsoft Word cục bộ vì host thiếu LibreOffice cho renderer mặc định. Workbook được tính lại khi soạn, ghép thay đổi vào đúng phần của gói Office để bảo toàn bảng, đồ thị và metadata, rồi thử tính lại bằng Excel. Tài liệu này xác nhận gói review; cổng nghiệm thu theo nguồn chính thức.
