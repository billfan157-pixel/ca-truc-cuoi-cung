# CH01 Rebirth II · Phiếu F101 và sổ điều tra

Mốc đối chiếu `e542476fd47ba053a9fb286ba2a54a3b0a79a327`. Phạm vi là bảng kẹp phòng 101 và năm trang sổ trực. Unity 6000.3.28f1, URP 17.3 và uGUI. Các luồng lấy vật chứng, checkpoint và nội dung cảnh sử dụng những điểm vào hiện có.

Nguồn thiết kế gồm Core 01 §2, Core 04 CH01 B05–B06, Core 06 §02–04, `07/Backlog TASK007-B`, `07/Chapter gates CH01–CH02` và `DEC-CH02`. Hai quan sát lớp B phục vụ F101 và G01. Vật chứng vật lý lớp R phục vụ tuyến E11/E12 và G02.

## Luồng chơi

- Tương tác với bảng kẹp trong phòng 101 để nhấc F101. Kéo hoặc dùng WASD để xoay, Space để lật, T để soi nghiêng, lăn chuột hoặc Q/Z để thu phóng. Bấm phải trên vùng giấy hoặc chọn chi tiết bằng phím số rồi nhấn M. Có bản chữ lớn. Đặt giấy lên bảng kẹp để điền dòng ghi nhận.
- Kẹp từng nguồn đã quan sát vào phiếu bằng thao tác kéo hoặc chọn ở mép giấy. Sổ cũng có thao tác kẹp nguồn từ trang hiện trường, tài liệu hoặc ghi chú. Chọn hai đầu nối và chọn lời ghi phù hợp. Đọc lại, kéo bút qua dòng ký hoặc dùng Enter, rồi đóng dấu. Esc trước lúc đóng dấu đưa về phần ghi.
- Sổ có năm bookmark. Trong Tài liệu, kẹp trái/phải hoặc kéo bản sao vào một trong hai ô. Chọn ô bằng 1/2, dùng WASD để dịch và Q/Z để phóng, hoặc dùng chuột. Hai ô có bản chữ lớn riêng, nút lật, soi 3D, rút và đổi tờ. Bấm phải vào vùng in để đánh dấu hoặc ghim ghi chú. Quay lại từ soi 3D sẽ về đúng hai tài liệu đang kẹp.
- Trong Suy luận, ghi văn bản của Minh và chọn tối đa hai nguồn đã đọc. Có thể mở lại nguồn, tháo đầu nối và đổi trạng thái giữa giả thuyết, câu hỏi còn mở và mâu thuẫn. Chỉ nút kẹp trên ghi chú chuyển những nguồn được chọn vào phiếu.

## Điểm nối mã nguồn

| File trong `Prototype/Assets/CTCC/Runtime` | Giao diện và trách nhiệm |
| --- | --- |
| `HospitalGame.Pages.cs` | `OpenObservationInspector`, vùng chữ trên hai giấy, bản chữ lớn, âm thanh/PA sau ký. `HospitalGame.World.cs` vẫn mở giấy qua `WorldF101`. |
| `HospitalGame.F101.cs`, `F101Draft.cs` | Điều phối bảng kẹp. `F101Draft.Submission` lấy danh sách ID do người chơi kẹp, claim họ chọn và những nét/liên kết có đầu mút đã kẹp. Draft khởi tạo rỗng khi bắt đầu ca hoặc tải save. |
| `Casework.cs`, `DeductionFramework.cs` | `SetF101LinkAsync(first, second)` ghi hai đầu mút qua `F101Annotations.ValidFor`. `EvaluateF101` kiểm lập luận khi đọc lại và khi gửi qua `Casework.SubmitF101DetailedAsync`. |
| `HospitalGame.Journal.cs`, `InvestigationJournal.cs` | Tra quan sát đã kiểm tra, dữ kiện, ghi chú, đối chiếu và lịch sử quyết định. `PaperPlacement` giữ tờ, mặt, cỡ phóng, vị trí và cuộn bản chữ lớn trong phiên. |
| `HospitalUI.cs`, `HospitalUI.Journal.cs` | Modal và điều hướng của game. Sổ giữ khung uGUI, hai vùng cuộn và ô viết, rồi gán lại dữ liệu cho hàng và nhãn đã tạo. |
| `HospitalUI.Papers.cs`, `PaperPointer.cs` | Hai viewport cắt theo khung. Pan và zoom sửa transform của riêng tờ đang thao tác. Vùng giấy chuẩn hóa theo UV, chữ lớn có cùng vùng đánh dấu. |
| `HospitalUI.F101.cs` | Trang biểu mẫu, nét ký chờ, animation bút và dấu. Bút/dấu đã ghi và âm thanh ký chỉ chạy sau khi transaction trả thành công và có revision mới. |
| `HospitalGame.InputQA.cs` | Ca Windows dùng đầu vào bàn phím Unity, raycast/drag của uGUI, lưu bị khóa, stress và ảnh đối chiếu. |

Màn hình được tạo lúc chạy. Tương tác cầm giấy dùng cùng bảng kẹp, renderer, texture và inspector trong các scene hiện có.

### Vòng đời chữ ký

`Draft → Review → Seal → SubmitF101DetailedAsync → animation → phản ứng phòng`.

- Khi chọn đọc lại, `EvaluateF101` trả `HypothesisEvaluation`. Lỗi hiển thị ở phần ghi trên phiếu để người chơi sửa từng nguồn, nét đánh dấu, liên kết hoặc nhận định.
- Kéo bút hoặc Enter chỉ chuẩn bị nét ký. Đóng dấu khóa tương tác trong lúc đợi giao dịch. Lỗi ghi giữ phần xem lại và revision đã commit. Retry đi qua cùng đường ghi.
- `Affirmed` và `Unverified` dùng hai dòng ghi trên phiếu, kể cả khi chưa kẹp căn cứ. Luồng `Refuted` của bảng kẹp gửi `annotationVersion=1`, đúng cặp OBS101A/B, hàng số 0 và đầu nối hợp lệ giữa chi tiết trên thẻ với hiện trường. Version 0 vẫn được giải mã theo hợp đồng lưu cũ.
- `firstDecision`, lịch sử revision, hệ số D và G01 vẫn do `FormService`/giao dịch hiện có quyết định. Cùng trạng thái đã ký đi qua nhánh idempotent. Cảnh trình bày bản đã ký theo state được publish.

## Save v6

`Investigation.Schema` giữ giá trị 6. `journalNotes[].kind/text/source/related`, `f101Annotations`, `f101Argument`, `f101History` và ID nguồn giữ cách đọc hiện có. Trần 32 ghi chú, 280 ký tự mỗi ghi chú. `paperMark` là trường tùy chọn trên note kiểu Annotation, gồm `placed`, `back`, `x`, `y`. Khi `placed=true`, tọa độ UV từ trái dưới trong [0,1], mặt sau cần tài liệu có hai mặt. Ghi chú v6 cũ thiếu trường này được đọc như ghi lề thường. Save cũ hơn vẫn đi qua migration của `AtomicSaves`.

Bản nháp quyết định, claim chưa ký và các tờ đang kẹp thuộc phiên chơi. Các nét đánh dấu F101, liên kết F101 hợp lệ và ghi chú đã lưu được ghi nguyên tử. Ghim vùng trên bản sao là dữ liệu của sổ. Nó không thêm vật chứng vào danh mục export.

## Bằng chứng nghiệm thu

Build Windows x64 `CH01-RII-9beee2c9870f`, mã nguồn `7f2c44d4a5f554c0319ff6756c340f81461baf66`. Mã SHA-256 của `Assembly-CSharp.dll` là `9beee2c9870f82a47ef89ba0ba98c50dd19fb7a7c1208624670513ec8ceea884`. Build được dựng từ scene có sẵn, cấu hình phát hành. Manifest và build nằm ở `output/ch01-rebirth-ii/windows/`.


### Tuyến đối chiếu và hiệu năng

Tuyến Refuted đối chiếu dùng cùng điểm đứng tại bảng kẹp, cùng các nguồn đã kiểm tra và hai nét đánh dấu đã có từ trước. Cả hai lượt ký được một lần và mở G01. Số bề mặt tương tác từ lúc nhấc F101 đến khi mở G01 giảm **4 → 2**. Mốc gốc đi qua giấy trên tay, danh sách cách xử lý, bảng đối chiếu lập luận và bảng xác nhận. Bản mới đi qua giấy trên tay rồi bảng kẹp, nơi nhập căn cứ, đọc lại, ký và đóng dấu.

[Ảnh F101 trước](evidence/rebirth-ii/f101-before.png) · [Ảnh F101 sau](evidence/rebirth-ii/f101-after.png) · [Tuyến thao tác trước](evidence/rebirth-ii/modal-before.txt) · [Tuyến thao tác sau](evidence/rebirth-ii/modal-after.txt)

Đo bằng hai bản Development có cùng bộ đo, lấy mã từ mốc gốc và commit giao, trên Intel Core Ultra 7 155H / Intel Arc, D3D11, Medium, VSync bật, 1920×1080, chữ 130%. Chạy trên cùng bộ quan sát CH01 và 12 ghi chú, camera cố định. Sau lượt làm nóng, mỗi bản thu 70 lần lật trang, 240 khung rê qua 30 lượt kéo giấy và 30 lần thả giấy. P95 theo thứ hạng gần nhất. Cột layout dùng marker `UI Layout / Layout`, CPU dùng Unity Frame Timing.

| Chỉ số | Trước | Sau |
| --- | ---: | ---: |
| Lật trang đến lúc khung đầu được render, P95 | 18,71 ms | 9,71 ms |
| GC cấp phát ở khung lật trang, P95 | 712,8 KiB | 11,6 KiB |
| Thời gian layout ở khung lật trang, P95 | 0,620 ms | 0,054 ms |
| Số lượt marker layout ở khung lật trang, trung vị | 2 | 1 |
| CPU luồng chính ở mẫu lật trang, P95 | 14,84 ms | 9,87 ms |
| Callback `UIEvents.WillRenderCanvases` ở mẫu lật trang, P95 | 0,187 ms | 2,476 ms |
| Xử lý thả giấy, P95 | 8,91 ms | 1,30 ms |
| Khung vượt 33,3 ms khi rê hoặc thả | 0/270 | 0/270 |

[Dữ liệu trước](evidence/rebirth-ii/profile-before.csv) · [Dữ liệu sau](evidence/rebirth-ii/profile-after.csv) · [Số liệu tổng hợp](evidence/rebirth-ii/profile-summary.json)

### Kiểm thử Windows

Bản phát hành chạy qua hai bộ kiểm thử trên D3D11, Medium và 1920×1080. Bộ đầu vào điều khiển nhân vật với Unity Input System và gửi sự kiện raycast/kéo thả qua uGUI. Bộ hệ thống kiểm trạng thái, điều kiện cổng, trình bày và giao dịch. [Kết quả đầu vào](evidence/rebirth-ii/qa-input.txt) · [Kết quả hệ thống](evidence/rebirth-ii/qa-module.txt)

| Ca chạy | Kết quả |
| --- | --- |
| Ba nhánh F101, CH01 → CH02 | PASS. Ba nhánh đi qua bảng kẹp thật. Ở ca Refuted, ba nguồn được kẹp bằng ba thao tác của người chơi. Một nguồn chuyển từ ghi chú, một nguồn kéo lên phiếu và một nguồn chọn trên mép bảng kẹp. Mỗi lượt ghi đúng quyết định đầu và mở G01. |
| Thiếu căn cứ, nguồn ngoài lề, đánh dấu hoặc nối sai, kết luận vượt chứng cứ | PASS. Lời phản hồi xuất hiện trên phiếu, G01 còn đóng trước lần ký hợp lệ. |
| Hủy nét ký, bấm dấu liên tiếp, ký lại cùng trạng thái, lưu bị khóa | PASS. Hủy giữ revision 0. Bấm lặp chỉ có một revision. Ký lại cùng quyết định giữ revision và không phát bút/dấu. Lỗi lưu giữ bản chờ để thử lại. |
| Save/load, lịch sử, R/B và migration | PASS. Reload giữ F101 đã ký, ghi chú và tọa độ ghim. Bộ hệ thống thử đổi lớp, rollback sau flush, phục hồi backup và migration v1–v5. Bộ đo đã đọc save v6 thật từ mốc gốc ở CH02 có ghi chú OBS101B và F101 revision 1. [Đọc save gốc](evidence/rebirth-ii/journal-v6.txt) |
| Sổ và bảng kẹp ở chữ 100% và 130% | PASS. Hai tờ độc lập, phóng/dịch/lật, ghi vùng, đổi/rút, mở inspector rồi trở về, viết nhận định, mở nguồn, đổi trạng thái, tháo liên kết. Đã rà ảnh trên build giao ở cả hai cỡ chữ. |
| Stress vòng đời UI | PASS. 100 lượt mở/đóng sổ và 100 lượt mở/đóng bảng kẹp giữ 191 RectTransform sau làm nóng. 100 lượt cầm/lật/soi giấy rồi đặt xuống kết thúc với 0 mesh giấy và 0 material giấy còn sống. |
| Hồi quy CH02–CH04 | PASS cho các tuyến chạy trong hai log. E11/E12 từ lớp R và G02, hai lối thoát CH02, lên tầng và xuống B1, G03/C12, R/M, cấp nguồn, H05 và điều kiện thoại CH04. |

[Đối chiếu ở mốc gốc](evidence/rebirth-ii/journal-before.png) · [Đối chiếu 100%](evidence/rebirth-ii/journal-after-100.png) · [Đối chiếu 130%](evidence/rebirth-ii/journal-after-130.png) · [Ghi chú và liên kết](evidence/rebirth-ii/journal-reasoning.png) · [F101 130%](evidence/rebirth-ii/f101-draft-130.png) · [Dấu sau ghi thành công](evidence/rebirth-ii/f101-signed.png)

Có thể chạy lại bộ kiểm thử bằng `tools/verify_windows.ps1` với `-Executable <bản Windows>`, `-GraphicsApi D3D11`, thư mục `-Evidence` mới cho mỗi lần, và thêm `-PlayerInput` cho bộ đầu vào.

### Giới hạn nghiệm thu

Mức dễ hiểu của suy luận và cảm giác cầm, ký giấy vẫn cần playtest người mới. Phép đo hiệu năng mô tả các thao tác trên sổ ở cấu hình và camera đã ghi. Một ghi chú có tối đa hai đầu nguồn. Bản nháp và bố trí tài liệu giữ theo phiên, còn ghi chú, nét đánh dấu và bản đã ký đi theo save.
