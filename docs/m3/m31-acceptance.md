# M3.1 · Bàn giao kỹ thuật

Nhánh `codex/m31-hospital-world`. Cổng Alpha M3 và các cổng nghiệm thu M2 tiếp tục mở. [Audit nền M3.0](m31-audit.md) · [Mặt cắt, đồ thị và căn cứ lịch sử](m31-world.md).

| Phạm vi | Thay đổi và bằng chứng |
| --- | --- |
| TASK022, LD01–04 | Trệt, tầng 2 +4 m, tầng 3 +8 m và B1 −3,4 m. Unity tạo và kiểm các cặp đường NavMesh hai chiều giữa AP02, AP05, AP06, AN03, AP07, AP08 và AP09. Phép quét capsule riêng xác nhận lối thẳng qua Cấp cứu đến chân thang. G05 và cửa khu C giữ khóa trên tuyến chơi hiện có. [Log dựng scene](evidence/m31/regression/authoring-ee42125.txt) |
| TASK011 | Hunter dừng khi đường thiếu hoặc không hợp lệ, giữ đường hợp lệ giữa các lần truy vấn và kiểm va chạm trước khi dịch chuyển. Fixture CH03 ghi ba lần từ chối trên ba truy vấn trong 0,8 giây, không đổi vị trí. Radio làm Hunter đi 1,78 m trong ca thử tiếp theo. Safe pad phân biệt cao độ. Orderly có điểm dừng theo lịch và hồi phục sau cảnh báo. Unrecorded dùng vòng ngắn và không truy đuổi. Ba vai trò đang dùng chung mẫu hình thể Kẻ Giữ Cửa. [Kiểm module Windows `ee42125`](evidence/m31/regression/module-ee42125.txt) |
| TASK012-A/B | E03 ở bàn đọc LOC07. PZ03 yêu cầu ghép dòng bị cạo, ghi chú B/2 và thẻ K-02. G03 dùng tồn tại C02. Tên Bà Lành là lựa chọn riêng. C12 luôn khuyết danh. FS10 là dấu nhận lệnh ở LOC06. Đoạn M cho người chơi rời đi chủ động, giữ ghế, khăn len và kim đan trong cảnh. Load giữa M đưa người chơi về AN03 ở R. [Cảnh Windows](evidence/m31/windows/ch03-memory.png) |
| TASK012-C/D | Tuyến thử CH04 từ phòng trực đến kỹ thuật và nhà xác. Bộ tải làm đèn và khóa đổi theo state đã lưu. Module thử 16 tổ hợp, kiểm E04 riêng lẻ giữ H05 đóng, sau đó cấp E07 để đối chiếu và lưu tại AP09. Cả hai câu của Lan giữ điều kiện CH04 B06, H05 và PT02. E06a giữ nguồn cassette. [Kiểm module Windows](evidence/m31/regression/module-ee42125.txt) |
| Lưu và streaming | Schema 3 đọc được fixture v1/v2, phục hồi B1 qua unload/reload và giữ E03 qua R/M. Chỉ F101 có định nghĩa sản xuất. Bộ nhiều phiếu giả lập dùng để kiểm giới hạn vùng và loại phiếu đúng. Vùng tầng trên, tầng hầm tải theo đầu mối và giữ cư trú khi có checkpoint. Bốn scene trệt luôn sẵn trong bản này. |
| TASK023 và asset kit | Năm asset gốc chỉnh sửa được trong Blender, FBX và LOD khi phù hợp. Cycles render cho [kho LOC07](evidence/m31/blender/interiors/LOC07_archive.png) và [nhà xác LOC10](evidence/m31/blender/interiors/LOC10_morgue.png) là cảnh nghiên cứu ánh sáng dùng mesh của game. [So sánh Windows trước và sau](evidence/m31/windows/visibility-comparison.jpg) dùng hai build tại cùng mốc máy quay. [Nguồn vật liệu và giấy phép](../../Prototype/ThirdPartyNotices.md) |

## Thoát sớm trên D3D12

Phép thử `-ProbeFailure module-failure` trên `ee42125` ghi đúng `RESULT=FAIL` trên cả hai API. D3D11 kết thúc với mã 1. D3D12 vẫn thoát với `0xC0000005` trong `D3D12Core.dll 1.618.1.0`, offset `0xa1f5` ở lần ghi sự kiện này. Giữ cửa sổ render để hoàn tất thêm một lượt đọc GPU trước khi thoát chưa loại được lỗi. Bài module D3D12 đầy đủ trên cùng build kết thúc thành công. [Sự kiện Windows](evidence/m31/regression/quit-ee42125-d3d12-event.txt) · [Verdict D3D12](evidence/m31/regression/quit-ee42125-d3d12-verdict.txt) · [Verdict D3D11](evidence/m31/regression/quit-ee42125-d3d11-verdict.txt)

## Cổng còn mở

- TASK010 cần sáu người chơi mới. TASK026–028 cần duyệt kiến trúc bệnh viện, lịch sử và PCCC, cùng provenance lâm sàng. Candidate B của Lan là mốc tạm cho duyệt mỹ thuật.
- Duyệt chất lượng hoàn chỉnh của môi trường, dấu sử dụng theo thời kỳ, diễn xuất ký ức có nhân vật, VO CH04 và hình thể riêng cho Orderly và Unrecorded. Các clip IK và mặt của M3.0 còn trong build.
- Kiểm đầy đủ nhánh đánh lạc hướng, lịch xe thuốc, các mức tốc độ và cầu thang của tác nhân. Cửa G05, tiền sảnh và khu C có greybox theo mặt cắt, với các tuyến và cơ chế của chương sau còn phải nghiệm thu.
