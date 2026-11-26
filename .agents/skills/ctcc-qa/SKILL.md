---
name: ctcc-qa
description: >-
  Xác minh và nghiệm thu Ca trực cuối cùng bằng ca thử có ID và bằng chứng. Dùng khi kiểm thử build, hồi quy gate/route/save, kiểm canon LOG/V22, test trợ năng/hiệu năng, playtest, xác minh task hoặc đánh giá cổng M0–M5.
---

# Kiểm thử và nghiệm thu

## Chọn ca theo thay đổi

1. Chốt task, milestone, phạm vi, revision/build, cấu hình chạy và save bắt đầu. Đọc tiêu chí Pass của Backlog/Roadmap và các ca phù hợp trong `07/QA`, `Narrative QA`, `Canon checks`. Dùng Core 01 cho mục tiêu playtest, Core 03/04 cho route, Core 05 cho AR/AU và Core 06 cho T01–08. [Bản đồ nguồn](../../../docs/agent/README.md#bảy-nguồn-sở-hữu)
2. Ưu tiên P0 theo thiết kế: crash, blocker, mất save, sai canon. Test đường đi chuẩn và trường hợp thiếu điều kiện. Bao phủ phụ thuộc phía sau theo rủi ro, với đúng preset, save và layer. Nếu mới có tài liệu, chạy được kiểm tra nguồn và ghi ca game ở trạng thái chưa chạy/kèm điều kiện thiếu.
3. Tách bằng chứng static, test tự động, chạy tay trong build, đo profiler, playtest người mới và review chuyên gia. Chạy qua điểm vào bình thường và đường hồi phục. Dùng tool debug để quan sát state, ghi rõ các bước setup ở ca dùng fixture hoặc nhảy checkpoint.

## Hồi quy trọng điểm

| Thay đổi | Ca cần đối chiếu |
| --- | --- |
| F101, G01/G02, E11/E12 | CH01-Q01–Q05, Q07/Q13, CH02-Q01–Q05, LOG06-Q01. Chạy A/B chưa quan sát và C thiếu từng nguồn, dấu, liên kết hoặc kết luận hợp lệ. Thử sửa, ký lặp, lỗi ghi, load và G02 thiếu phiếu chuyển. |
| Walkthrough CH01 B01–B08 (Core 04 v3.6.2) | `V36-CH01-Q01..Q08` và `CH01-WT-Q01/Q02`. Kiểm `T01` hộp thư, `T02` yêu cầu đúng chìa + đủ nguồn `E10a` (không gate `SMSRead`), load save cũ giữ `E10a` acquired, 0 mandatory Journal CH01, `EX01` chọn đúng 03:17, `WR05` giới hạn hướng ra trước `G08` không collider vô hình, `D08` guard & `H08` recovery tại `LOC08`. Tra nguồn duy nhất `CH01 / Walkthrough B01–B08` trong [Core 04](../../../docs/04_Level_Chapter_Design.docx). |
| Silent Exploration & VO (`DEC-VO-01`) | `VO-SILENT-Q01`, `AU05 (v3.3.3)`, `VAL-V3-01` (`TASK031-A/B`). Kiểm 0 rò VO trên `INNER_SUB_SILENT`/`CUT_TO_JOURNAL`/`UI_NON_VO`, độ tương phản WCAG AA `SilentThoughtSubtitle`, `ThoughtReadDurationScale` và `stageDirection` không rò vào `runtimeCopy`. |
| Layer, collision, checkpoint | T02/T03/T08, LD01–04, các ca swap/load theo chương. Tại TASK025-B có 30 lượt thử swap theo Backlog. |
| E05b, E06, E10 và G06 | CH05-Q03, CH06-Q02/Q05, W-Q03, V22-Q01. Thiếu một phần phải chỉ đường lấy lại. |
| D16b, PT02, E08 | CH01-Q14, CH04-Q03/Q06, LOG10-Q01, V22-Q02/Q03, CH07-Q01–Q04. Test menu, VO, subtitle, transcript và cả reload. |
| Ending, export, danh tính | CH08-Q01–Q07, LOG11-Q01, FQA07/FQA10/FQA12. 3 nhánh F101 × 4 ending ở regression đầy đủ, C12 giữ unknown, coda Bảy ở cả 4. |
| Cue, stealth, trợ năng | CH02-Q03/Q05, CH03-Q03, CH08-Q07, T04/T06, LD05/06, AR/AU thích hợp. Thử mute, bàn phím, low-light và reduced motion. |
| Tài liệu và milestone | V22-Q04, chạy kiểm tra dependency bằng `python docs/agent/project_docs.py check`, đối chiếu trạng thái thật từng đầu việc/cổng và biên bản review. |

M2 cần quan sát 6 người mới trên slice. Ghi giờ giải C01, lần xin hint, hiểu tác động F101, phân biệt patient/threat và thoát cameo. Chấm 5/6 riêng cho C01 trong 8 phút không được điều phối viên hướng dẫn, hiểu hệ quả ký sai F101 và phân biệt bệnh nhân/threat qua hành vi. Ghi build, ID ẩn danh, thời điểm đo và trợ giúp. Với hiệu năng, ghi máy, preset, đường đo, p95 frame time, memory/load time theo Core 06 và mục tiêu đã chốt trong TASK029.

## Ghi kết quả

Dùng cột `QA!ID / Chapter / Steps / Expected / Status / Build / Actual / Issue`. `Build` ghi revision hoặc tên gói đủ nhận dạng. `Actual` ghi quan sát, route/setting và link log/clip, còn `Issue` giữ ID lỗi và bước tái hiện. Một ca Pass cần kết quả dự kiến trên đúng build, và một lỗi đã sửa cần retest trước khi đóng. Giữ “Chưa test” cho trường hợp chưa chạy. Bàn giao rõ giới hạn môi trường, ca còn mở và đầu việc chặn nghiệm thu.
