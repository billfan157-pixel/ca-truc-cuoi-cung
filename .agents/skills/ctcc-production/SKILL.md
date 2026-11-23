---
name: ctcc-production
description: >-
  Điều phối sản xuất Ca trực cuối cùng theo tài liệu Core 01–07, Backlog và Roadmap. Dùng khi được yêu cầu build game, tiếp tục dự án, chọn task, lập scope, làm M0 hoặc engine spike, xử lý phụ thuộc, đồng bộ tài liệu hay nghiệm thu milestone M0–M5.
---

# Điều phối sản xuất Ca trực cuối cùng

## Bắt đầu

1. Tìm root có `AGENTS.md` và `docs/`. Đọc [hướng dẫn vận hành](../../../docs/agent/README.md) cho bản đồ nguồn và điều kiện M0–M5. Kiểm cây hiện tại để biết project engine, lệnh test/build và bằng chứng nào thực sự tồn tại.
2. Chạy `python docs/agent/project_docs.py status` để đọc Roadmap và các việc MUST đang mở. Chạy `python docs/agent/project_docs.py task <ID>` cho task cần làm. Có thể tra nguồn trực tiếp bằng Word/Excel nếu Python chưa sẵn. Status của dependency lấy từ dòng Backlog, sau đó đối chiếu cổng của milestone.
3. Gắn yêu cầu với ID đã có và chọn một đầu ra đủ nhỏ để nghiệm thu. Với hạng mục cha, nghiệm thu tích hợp chỉ sau khi đủ subtask và bằng chứng. Với việc ngoài Backlog, định nghĩa phạm vi và Pass từ tài liệu, rồi ghi rõ đây là đề xuất khi đang chờ chốt.
4. Định tuyến theo bảng trong `AGENTS.md`. Chuyên môn gameplay, level, narrative hoặc art/audio sở hữu thay đổi tương ứng. Dùng `ctcc-qa` khi cần xác minh ca thử hay nghiệm thu build.

## Vận hành theo cổng

- M0 dùng điều kiện sống tại `07/Roadmap`, `07/Backlog` và `LOCK-V22-03`. Ba hướng khởi động là mặt cắt hư cấu `TASK022`, reference đúng thời/quyền `TASK023` và thử nghiệm lựa chọn engine/target `TASK029`. Xem đầu ra và tiêu chí từng việc trong hướng dẫn vận hành.
- Core 06 giao `TASK029` trước `TASK003`. Trong spike, so lựa chọn theo đúng tiêu chí Core 06 và ghi số đo/giới hạn từng phương án thực sự thử. Ghi kết quả vào `DEC-V2-04` khi được giao chốt lựa chọn.
- M1 nghiệm thu `TASK025` qua module thực, gồm ba nhánh F101, sửa phiếu, R/B, save/load, E11/E12 và stress swap. M2 mở sau M1 pass. M2 dùng slice CH01 rút gọn + C01 với cameo scripted, playtest người mới và các review cần thiết trước art lock. Hunter/Orderly đầy đủ đi theo `TASK011` ở M3.
- Với CH01, đọc [walkthrough B01–B08](../../../docs/ch01r/walkthrough-gameplay-B01-B08-investigation-v1.md) và DEC-CH01R-04. Ghi riêng thiết kế CH01 đầy đủ, build ứng viên và phạm vi M2 rút gọn trong Roadmap. Khi có log, ghi phạm vi qua kiểm theo build trước khi xem xét điều kiện đóng task.
- Khi thiếu tiền đề của hạng mục đang được yêu cầu, xác định đầu việc tiên quyết đang mở. Thực hiện phần tiền đề hoặc việc độc lập nếu nằm trong phạm vi. Ghi blocker bằng ID và điều kiện còn thiếu. Mọi quyết định đổi canon, mục tiêu bản phát hành hoặc ràng buộc đã khóa đi qua DEC và người có thẩm quyền chốt.

## Đồng bộ và bàn giao

- Dùng Core 01 §4, `DEC-V2-03`, `LOCK-V22-01–03` và các DEC theo chương để xác định tài liệu sở hữu. Đọc lại dòng nguồn ở đầu việc và trước khi sửa trạng thái. Lấy fingerprint bằng `python docs/agent/project_docs.py hashes` nếu cần gắn kết quả với phiên bản nguồn hoặc kiểm thay đổi giữa hai lần đọc.
- Khi được giao sửa dữ liệu, dùng ID để tìm dòng, đối chiếu phiên bản hiện tại và sửa đúng ô. Giữ công thức, bảng và lịch sử. Cập nhật DEC/Changelog cho quyết định đã chốt và các mục liên quan trong Word, Events, Dialogue, Chapter gates, Puzzle specs, Assets hoặc QA theo ảnh hưởng. Chạy `python docs/agent/project_docs.py check` trước khi bàn giao thay đổi nguồn.
- Bàn giao milestone/task, điều kiện Pass, đường dẫn đầu ra, ca kiểm và bằng chứng, trạng thái thực tế và blocker còn mở. Tài liệu chốt, prototype hoạt động và playtest đạt là những mốc có bằng chứng riêng.
