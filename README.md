# Ca trực cuối cùng

Prototype Unity khám phá phòng 101 của bệnh viện An Sinh. Bản thử đi từ cổng phụ qua lễ tân, hành lang và phòng bệnh đến kho giường và cấp cứu. F101 có ba cách xử lý. Người chơi có thể quay lại sửa phiếu, đổi giữa R và B tại điểm neo, đối chiếu E11/E12 và tải lại ca đã lưu.

**M1-P · Windows x64** dùng Unity 6.3 LTS, URP 17.3 và bộ tài sản Blender 4.5 của M0-R. Kết quả đo 1080p, hồi quy, gói build và điều kiện nghiệm thu nằm trong [hồ sơ M1-P](docs/prototype/m1p-acceptance.md).

## Điều khiển

| Phím | Thao tác |
| --- | --- |
| WASD và chuột | Di chuyển và nhìn |
| E | Tương tác vật lý (mở/đóng cửa gỗ, kéo rèm, lăn tủ, soi vật chứng 3D) |
| Kéo chuột / WASD & lăn chuột / Q-Z (khi soi 3D) | Xoay và thu phóng vật chứng / hồ sơ trong 3D Physical Inspector |
| Tab | Mở sổ trực. Trong trang đọc, Tab hoặc mũi tên chuyển nút và Enter chọn |
| Q tại vạch đồng phòng 101 | Mở điểm neo AN02 trong CH02 |
| F | Đèn pin (bóng đổ thật, cảnh báo pin yếu) |
| Shift và Ctrl | Chạy và cúi |
| Esc | Khép trang đọc / thoát soi 3D hoặc mở menu |

Trong Cài đặt có mức đồ họa Low/Medium/High, phím mũi tên, K tương tác, J mở sổ, L bật đèn pin, cỡ chữ 130%, giảm chuyển động, tăng sáng, âm thanh mono, phụ đề âm thanh, độ nhạy chuột và chỉnh âm lượng tổng/âm nền. Bản tự lưu ghi sau các mốc chính. Lưu tay dùng ở bàn trực trong lễ tân.

## Nguồn và cách dựng

Mở thư mục `Prototype` với Unity 6.3 LTS `6000.3.28f1` (trên Windows nên mở qua đường dẫn ASCII như junction `C:\CTCC-Unity-Workspace\Prototype` khi build Windows). Scene chính nằm tại `Assets/CTCC/Scenes/AnSinh.unity`; nguồn 3D gốc chỉnh sửa được nằm tại `ArtSource/Blender/*.blend` (`tools/blender_assets/`). Menu **Ca Trực Cuối Cùng** có lệnh dựng mặt bằng, dựng lại cảnh chi tiết M0-R và build Windows.

```powershell
./tools/build_m1p.ps1 -Output output/m1p/repro-windows
./tools/verify_windows.ps1 -Executable output/m1p/repro-windows/CaTrucCuoiCung.exe -Evidence output/m1p/repro-input -PlayerInput
```

- [Nghiên cứu An Sinh](docs/prototype/research.md), [mặt cắt và mặt bằng](docs/prototype/layout.md) và [bảng vật liệu](docs/prototype/loc03-reference.png).
- [Chọn Unity và URP](docs/prototype/engine.md), [Bằng chứng nghiệm thu & so sánh BEFORE/AFTER](docs/prototype/verification.md) và [Bảng showcase 8 góc M0-R](docs/prototype/evidence/m0r-showcase-board.jpg).
- [Nguồn tài nguyên của build](Prototype/ThirdPartyNotices.md) và [Production Database v2.2](docs/07_Production_Database.xlsx).
