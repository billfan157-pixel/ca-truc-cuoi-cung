---
name: ctcc-gameplay
description: >-
  Triển khai và sửa hệ thống gameplay của Ca trực cuối cùng theo Core 01 và 06. Dùng cho FPS controller, tương tác, journal, F101, evidence, puzzle, gate G01–G08, layer R/B/M, save/load, hệ AI hunter/Orderly và ending logic.
---

# Gameplay và hợp đồng trạng thái

## Nguồn cho task

- Đọc GDD §2 và phần v2.2, Technical Design §01–04 và contract v2/v2.2, rồi tới chương bị tác động. [Core 01](../../../docs/01_Game_Design_Document.docx), [Core 06](../../../docs/06_Technical_Design_Document.docx), [Core 04](../../../docs/04_Level_Chapter_Design.docx)
- Với CH01, đọc [walkthrough B01–B08](../../../docs/ch01r/walkthrough-gameplay-B01-B08-investigation-v1.md) và DEC-CH01R-04. Tách inventory, ObservationRecord, EvidenceRegistry và nhật ký. Tab/J mở ba lô, Pocket Note mở nhật ký, còn AN01 là prop thực hiện giao dịch theo beat.
- Tra `07/Chapter gates`, `Events`, `Puzzle specs`, `QA` theo ID. Ví dụ `python docs/agent/project_docs.py sheet "Puzzle specs" CH02-PU02`.
- Trước khi thêm code, tra phiên bản project và task. Với hệ thống player đầu tiên, `TASK003` cần `TASK002` và `TASK029`. Khi cần thay beat, reveal hoặc layout, áp dụng thêm skill sở hữu tương ứng.

## Thực hiện

1. Ghi input, trạng thái đã commit, điều kiện, tác dụng phụ, điểm save và đường hồi phục. Dùng ID ổn định và registry ở lớp dữ liệu. Scene tham chiếu ID. UI đọc state và gửi action qua service. Dùng sơ đồ module và event bus Core 06 để nối journal, gate, layer, audio và objective.
2. Kiểm toàn bộ điều kiện trước khi ghi state. Commit một lần theo ID/revision hoặc khóa once đã lưu. Tính `D_zone` từ số phiếu unique xác nhận sai chưa sửa và áp trần 4. F101 ghi `firstDecision` bất biến cùng trạng thái và revision hiện hành. Nút xác nhận lại cùng nội dung giữ revision.
3. Với R/B/M, preflight AN và safe pad, khóa interaction/AI, swap presentation và collider, xác nhận landing/nav, ghi snapshot bền vững, rồi công bố state, cập nhật audio và resume. Lỗi phải rollback về hình học/state trước swap, giữ evidence và giải thích bước thử lại. Save schema 7 đọc v1–v6, có checksum, backup và hai slot theo Core 06. T08 hòa giải giấy B ở tay, trong túi hoặc trên kẹp/bàn về fixture và ghi nhớ, giữ đồ R. Load khôi phục committed state, once flags, pad và ngưỡng tối thiểu pin/thể lực.
4. Với AI, dùng LOS/âm có occlusion, trạng thái có căn cứ và báo hiệu trước chase. CH01 dùng kinh dị môi trường, CH02 cameo scripted, CH03 patrol hạn chế, CH04–06 theo encounter từng beat, CH07 đối đầu bằng phản bác và CH08 hạ nhịp. Tôn trọng safe/reading, pad và layer transaction. Số tốc độ, LOS, pin và cooldown lấy từ spec như seed để test.
5. Giữ hint theo điều kiện, để người chơi tự xác nhận, hỗ trợ bàn phím và thao tác thay cho drag, tóm tắt sau skip, caption hướng và cue vật thể/chữ. Độ khó tác động encounter, giữ logic bằng chứng.

## Đối chiếu các hợp đồng dễ lệch

- `F101`: A affirmed sai có D=1 ở 101, B unverified có D=0, C refute cần OBS101A/B đã kiểm, dấu dòng 0 và ô người nhận AS-05B, liên kết thẻ với dấu giường 4 và kết luận số 0 chưa đủ căn cứ. C có D=0. G01 mở sau commit hợp lệ. CH02 giữ first/current và có thể sửa. G02 dùng E11 đủ `drawing/wristband/transferNote` từ R và E12 vật lý, gắn xác thực C01 theo luồng PZ01–02.
- `G03` yêu cầu tồn tại C02. Danh tính C02–11 là tùy chọn. `C12` giữ danh tính chưa xác định. Khi chạm H05, tuân điều kiện E04+E07 và thứ tự trình bày PT01/PT02 trước khi ra CH04.
- `G05` dùng H03 với E05+E05b, từ tuyến R đến được trước G06. `G06` dùng bundle E01–07, E09, E10 hoàn chỉnh và luồng H01/H02/PT03 theo CDD. E06 là bản ghi trực trên giấy, E06a là cassette họp riêng, E10 cần a+b.
- `PZ09` dùng mốc E09 và dữ liệu Core 04 để mở M. E08 hoàn tất sau khi xem hoặc skip có transcript đúng cảnh M ở CH07. `G07` yêu cầu PZ09, E08 và 5 bit phản bác PZ10. Chỉ xuất vật chứng vật lý đủ provenance, đánh dấu E08 nonexport.
- `G08` dùng preview + xác nhận cuối, 13 nạn nhân bất biến. Logic END01–04 dựa trên lựa chọn phiếu, export và lời khai. Coda Bảy độc lập ở mọi nhánh. Giữ checkpoint trước lựa chọn để chơi lại.

Chạy ca đúng ID theo `ctcc-qa`. Ưu tiên trường hợp hợp lệ, thiếu bộ phận evidence, submit trùng/sửa, skip/backtrack, load giữa giao dịch và lựa chọn có độ trợ năng khác nhau trong phạm vi thay đổi. Bàn giao state trước/sau, build và kết quả cụ thể.
