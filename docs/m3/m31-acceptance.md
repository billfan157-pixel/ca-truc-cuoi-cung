# M3.1 · Bàn giao kỹ thuật

Nhánh `codex/m31-hospital-world`. Cổng Alpha M3 và các cổng nghiệm thu M2 tiếp tục mở. [Audit nền M3.0](m31-audit.md) · [Mặt cắt, đồ thị và căn cứ lịch sử](m31-world.md).

**Ứng viên Windows `05a5bfa`** được dựng từ checkout sạch bằng Unity 6000.3.28f1. Manifest ghi SHA-256 assembly và bảy scene. Bộ kiểm module và bộ kiểm bàn phím đều kết thúc với `RESULT=PASS` trên D3D12. Bài bàn phím đi ba chặng cầu thang, dùng W/Tab/Enter/E ở những thao tác được ghi và đặt máy quay tại vị trí chuẩn khi thử tương tác đạo cụ. Lượt này bao phủ ba nhánh F101, PZ03, C12, R/M, tải điện, H05, hai điều kiện thoại Lan và tải lại AP09. [Manifest](evidence/m31/windows/build-05a5bfa.txt) · [Module](evidence/m31/regression/module-05a5bfa.txt) · [Bàn phím](evidence/m31/regression/input-05a5bfa.txt)

| Phạm vi | Thay đổi và bằng chứng |
| --- | --- |
| TASK022, một phần LD01–04 | Trệt, tầng 2 +4 m, tầng 3 +8 m và B1 −3,4 m. Unity tạo và kiểm các cặp đường NavMesh hai chiều giữa AP02, AP05, AP06, AN03, AP07, AP08 và AP09. Phép quét capsule riêng xác nhận lối thẳng qua Cấp cứu đến chân thang. G05 và cửa khu C giữ khóa trên tuyến chơi hiện có. [Log dựng scene](evidence/m31/regression/authoring-ee42125.txt) |
| TASK011 | Hunter dừng khi đường thiếu hoặc không hợp lệ, giữ đường hợp lệ giữa các lần truy vấn và kiểm va chạm trước khi dịch chuyển. Fixture CH03 ghi ba lần từ chối trên ba truy vấn trong 0,8 giây, không đổi vị trí. Radio làm Hunter đi 1,78 m trong ca thử tiếp theo. Safe pad phân biệt cao độ. Orderly có điểm dừng theo lịch và hồi phục sau cảnh báo. Unrecorded dùng vòng ngắn và không truy đuổi. Ba vai trò đang dùng chung mẫu hình thể Kẻ Giữ Cửa. [Kiểm module Windows `ee42125`](evidence/m31/regression/module-ee42125.txt) |
| TASK012-A/B | E03 ở bàn đọc LOC07. PZ03 yêu cầu ghép dòng bị cạo, ghi chú B/2 và thẻ K-02. G03 dùng tồn tại C02. Tên Bà Lành là lựa chọn riêng. C12 luôn khuyết danh. FS10 là dấu nhận lệnh ở LOC06. Đoạn M cho người chơi rời đi chủ động, giữ ghế, khăn len và kim đan trong cảnh. Load giữa M đưa người chơi về AN03 ở R. [Cảnh Windows](evidence/m31/windows/ch03-memory.png) |
| TASK012-C/D | Tuyến thử CH04 từ phòng trực đến kỹ thuật và nhà xác. Bộ tải làm đèn và khóa đổi theo state đã lưu. Module thử 16 tổ hợp, kiểm E04 riêng lẻ giữ H05 đóng, sau đó cấp E07 để đối chiếu và lưu tại AP09. Cả hai câu của Lan giữ điều kiện CH04 B06, H05 và PT02. E06a giữ nguồn cassette. [Kiểm module Windows](evidence/m31/regression/module-ee42125.txt) |
| Lưu và streaming | Schema 3 đọc được fixture v1/v2, phục hồi B1 qua unload/reload và giữ E03 qua R/M. Chỉ F101 có định nghĩa sản xuất. Bộ nhiều phiếu giả lập dùng để kiểm giới hạn vùng và loại phiếu đúng. Vùng tầng trên, tầng hầm tải theo đầu mối và giữ cư trú khi có checkpoint. Bốn scene trệt luôn sẵn trong bản này. |
| TASK023 và asset kit | Năm asset gốc chỉnh sửa được trong Blender, FBX và LOD khi phù hợp. Cycles render cho [kho LOC07](evidence/m31/blender/interiors/LOC07_archive.png) và [nhà xác LOC10](evidence/m31/blender/interiors/LOC10_morgue.png) là cảnh nghiên cứu ánh sáng dùng mesh của game. [So sánh Windows trước và sau](evidence/m31/windows/visibility-comparison.jpg) dùng hai build tại cùng mốc máy quay. [Nguồn vật liệu và giấy phép](../../Prototype/ThirdPartyNotices.md) |

Nghiên cứu ánh sáng có nguồn chỉnh sửa trong `ArtSource/Blender/Studies`. Manifest kit ghi 812–2.784 tam giác ở LOD0 và dấu vân tay của nguồn .blend, FBX, render. Production Database có `M31-KIT-01`, kết quả module theo dòng QA và ghi nhận ở Changelog. [Manifest kit](evidence/m31/blender/service-kit.json) · [Video từ bộ kiểm bàn phím CH03–04, 12 fps, hình và phụ đề](evidence/m31/windows/CH03-CH04-05a5bfa.mp4)

## Hiệu năng Windows

Build `05a5bfa`, Intel Core Ultra 7 155H, Intel Arc tích hợp, D3D12, Medium, cửa sổ 1920×1080, màn hình ở 60 Hz, URP render scale 0,88. Mỗi hàng là một lượt được chấp nhận, với tối thiểu 99% thời gian foreground theo Unity và Windows. Lượt stress đầu giảm còn khoảng 58% foreground và nằm trong dữ liệu thô dưới nhãn loại.

| Lượt | Main CPU P95 | Render CPU P95 | Unity GPU P95 | PresentMon FrameTime P95 | Working set đỉnh |
| --- | ---: | ---: | ---: | ---: | ---: |
| Tuyến CH01–04, VSync bật | 5,69 ms | 2,31 ms | 12,80 ms | 18,99 ms | 1.541,5 MiB |
| Tuyến CH01–04 và stress 1/3/6/9 nhân vật, VSync tắt | 4,55 ms | 1,91 ms | 10,97 ms | 12,22 ms | 1.594,1 MiB |

Lượt VSync có DisplayedTime P95 16,74 ms. Đoạn vào phòng 101 ở B có Unity GPU P95 18,40 ms. Ngân sách FrameTime 16,67 ms vẫn còn cửa đo vượt ngưỡng. Ở đoạn chín nhân vật giả lập cùng hiện trong khung và giữ LOD0, Unity đo wall P95 12,25 ms, main CPU 6,26 ms và GPU 10,28 ms. Cảnh giả lập kiểm tải render và khẩu hình.

Tải tới khi vùng sẵn sàng mất 54,25 ms ở tầng trên và 80,76 ms ở B1 trong lượt VSync. Lượt stress ghi 31,66 ms và 40,60 ms. Số này gồm đăng ký scene và chuẩn bị navigation. Các cột thời gian collect/bake NavMesh trong release đọc 0, vì vậy ca QA truy vấn AI ở trên và thời gian tải vùng là bằng chứng hiện có cho navigation. [CSV, cấu hình màn hình, kết quả và identity thô](evidence/m31/performance-raw-05a5bfa.zip)

## Thoát sớm trên D3D12

Phép thử `-ProbeFailure module-failure` trên `ee42125` ghi đúng `RESULT=FAIL` trên cả hai API. D3D11 kết thúc với mã 1. D3D12 vẫn thoát với `0xC0000005` trong `D3D12Core.dll 1.618.1.0`, offset `0xa1f5` ở lần ghi sự kiện này. Giữ cửa sổ render để hoàn tất thêm một lượt đọc GPU trước khi thoát chưa loại được lỗi. Bài module D3D12 đầy đủ trên cùng build kết thúc thành công. [Sự kiện Windows](evidence/m31/regression/quit-ee42125-d3d12-event.txt) · [Verdict D3D12](evidence/m31/regression/quit-ee42125-d3d12-verdict.txt) · [Verdict D3D11](evidence/m31/regression/quit-ee42125-d3d11-verdict.txt)

## Cổng còn mở

- TASK010 cần sáu người chơi mới. TASK026–028 cần duyệt kiến trúc bệnh viện, lịch sử và PCCC, cùng provenance lâm sàng. Candidate B của Lan là mốc tạm cho duyệt mỹ thuật.
- Duyệt chất lượng hoàn chỉnh của môi trường, dấu sử dụng theo thời kỳ, diễn xuất ký ức có nhân vật, VO CH04 và hình thể riêng cho Orderly và Unrecorded. Các clip IK và mặt của M3.0 còn trong build.
- Kiểm đầy đủ nhánh đánh lạc hướng, lịch xe thuốc, các mức tốc độ và cầu thang của tác nhân. Cửa G05, tiền sảnh và khu C có greybox theo mặt cắt, với các tuyến và cơ chế của chương sau còn phải nghiệm thu.
- Tiêu đề menu tạm dừng ở CH03–04 còn dùng tên chương cũ. Prompt tương tác và mục tiêu trong màn chơi dùng state chương hiện tại.

Gói Windows x64 cục bộ `CaTrucCuoiCung-M3.1-05a5bfa-Windows-x64.zip` đã qua kiểm CRC. [SHA-256 của gói](evidence/m31/windows/release-05a5bfa.sha256)
