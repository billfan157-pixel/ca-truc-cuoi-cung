# Ma trận tác động gameplay và kỹ thuật NPR-01

Thay đổi hiện có nằm trong DOCX, XLSX và tài liệu bổ trợ. Cột triển khai tương lai giúp ước lượng công việc sau duyệt. Thiết kế kiểm chứng hiện hành vẫn là chuẩn cho candidate M2 và M3.2.

| Phạm vi | Đích Core và tài liệu | Tác động nếu đưa vào game | Kiểm trước khi tích hợp |
| --- | --- | --- | --- |
| Hai manifest và lời kết | Core 01 §6; 02 §3.4; 04 CH08 B05/B07; 06 snapshot; 07 Ending/Dialogue; [hồ sơ canon](canon-proposals.md). | Đổi label preview, transcript, subtitle/VO của END03 theo nguồn R. Dùng lại nhánh hiện có và payload hiện có. Ảnh E01-B nếu duyệt ảnh hưởng asset read/large print. | LOG11, CH08-Q01/Q03/Q04/Q07, V31-Q05/V33-Q03. Giữ 3×4, fallback XK12, E08 nonexport, replay và snapshot. |
| Tầng công bố và provenance | Core 02 CAN02/04/08, 03 LOC08/10, 04 CH06/08, 07 Evidence/Provenance/Reveal. | Có thể thêm nhãn đọc trên UI theo trạng thái sẵn có. Giữ nguồn và đường lấy lại. Chọn prop hiện có nếu cần minh họa. | G04/G06 giữ điều kiện. H05 chỉ xác thực vị trí thu hồi. Kiểm lại E01-A/B ở chế độ chữ lớn và luồng hồi phục theo checkpoint. TASK028 trước thêm nội dung nghiệp vụ. |
| EVT026/AN05 | Core 02 Timeline, 03 mặt cắt/Khu B, 04 B03 và CH05, 05 shot/cue, 07 EVT026/D15/E08. | Tương lai có thể thay mô tả/caption trung tính theo phạm vi duyệt. AN05 giữ waypoint/cameo của CH05, 6 người và route độc lập. | Kiểm route W-Q02 trong cấu hình liên quan, CH07-Q02/Q03 và V32-Q01; TASK027. |
| Vĩnh và Minh–Lan | Core 02 CHAR, 04/05 beat/visual, 07 Dialogue/Assets. | Tùy chọn cử chỉ hoặc prop-only. Chỉ đổi VO/inspect khi chốt cut. Phạm vi M3.2 cần xem riêng nếu chọn CH04; tránh chạm track và guard hiện hành. | H05/PT02, `CH04-D06`, `CH01-D16b`, mute/reload. 28 CH01 và candidate giữ theo lock. |
| PZ10 và 2003 | Core 02 và 04, 05 cue, 06 lock hợp đồng, 07 Dialogue/PZ10. | Chỉ ánh xạ copy qua bốn ID R1/R3 và D19 hiện có. Localization kiểm tràn chữ, transcript sync. Option UI không sửa validator. | R1 E03+E01; R2 E05+E05b; R3 E02+E09; R4 E02+E08; R5 E01+E04. 5 bit, hint/reload, G07 và Save v7. |
| Dashboard, Ledger và governance | Core 01 §3, Core 07, [Ledger](production-ledger.md). | Dùng số để lập kế hoạch, công thức chỉ đếm các trạng thái nêu trong nhãn. Static snapshot và build logs ghi ngày. | So raw XML, phép đếm độc lập, phép thử ref tính lại. Core 07 Roadmap/Backlog quyết điều kiện nghiệm thu. |
| Art, locale, tái lập và ngân sách | Core 03 mốc hình học, 05 art/audio, 06 tech, Core 07 Assets/QA; [kế hoạch technical](technical-plan.md). | Prototype text offline theo ID, lưu stable content và cache. Dữ liệu streaming/build sizing thu bằng build độc lập. Chỉ tích hợp sau duyệt. | T06/T07/T11, WORLD-Q04/Q06, AR/AU. So Core và runtime theo diff. |

## Ảnh hưởng tới các nhánh và build

| Phạm vi | Hiện trạng | Điều kiện trước khi đưa thay đổi vào đây |
| --- | --- | --- |
| M2 `c459aba` | Ứng viên có định danh và artifact riêng. E10a, 28 dòng CH01, F101, route và các cổng nằm trong phạm vi kiểm hiện có. | Giữ ứng viên và đường nghiệm thu riêng. Bất kỳ build mới nào có runtime khác phải có ID, hash và QA riêng trước khi dùng cho TASK010. |
| M3.2 `feat/m32-ai-ch03-ch04` | Worktree riêng tại `9a6378b`. Phase F ghi ứng viên `c90a2eb` đã kiểm kỹ thuật TASK011/012. CH04 có guard H05/PT02 và hai tuyến PZ04 đã chạy. | Chỉ cân nhắc nhịp CH04 sau khi owner chốt phạm vi. Test lại guard, streaming và route trên một build của nhánh được giao. |
| CH05–CH08 | Phạm vi tài liệu trong NPR-01. | Dùng quyền triển khai theo Roadmap, đủ quyết định/QA áp dụng. Giữ topology, ID, thời điểm reveal, G01–G08 và Save v7. |

Các phần phụ thuộc chéo về narrative xem [ma trận narrative](narrative-impact-matrix.md). Cách kiểm mỗi ca xem [QA impact](qa-impact.md).
