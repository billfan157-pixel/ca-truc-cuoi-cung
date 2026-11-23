# Ca trực cuối cùng

**Ca trực cuối cùng** có ứng viên Windows x64 cho tuyến điều tra CH01 B01–B08 trên nền M3.1. [Walkthrough CH01](docs/ch01r/walkthrough-gameplay-B01-B08-investigation-v1.md) và [kết quả kiểm kỹ thuật](docs/ch01r/acceptance.md#kiểm-kỹ-thuật-tuyến-b01b08). Tuyến CH01–02 đi từ cổng phụ qua phòng 101 đến Cấp cứu, với ba cách xử lý F101 và đối chiếu E11/E12. CH03 nối khu B và kho tầng 3 bằng cầu thang. Tuyến thử CH04 đi từ phòng trực đến nhà xác, với sổ ca, bảng tải và đối chiếu E04/E07. [Mặt cắt và căn cứ của M3.1](docs/m3/m31-world.md)

Project dùng Unity 6.3 LTS, URP 17.3 và nguồn Blender 4.5. [Audit M3.1](docs/m3/m31-audit.md) ghi các vấn đề tại mốc M3.0. [Hồ sơ M2-A](docs/m2/acceptance.md) và [hồ sơ M1-P](docs/prototype/m1p-acceptance.md) lưu kết quả các mốc trước.

## Điều khiển

| Phím | Thao tác |
| --- | --- |
| WASD và chuột | Di chuyển và nhìn |
| E | Cầm bì thư hoặc giấy, giở sổ trên quầy, mở cửa và thao tác với vật thể đang nhìn |
| Giữ chuột trái khi nhìn tay nắm | Kéo cửa, ngăn, rèm hoặc ghế. Thả để giữ vị trí, Esc để hủy |
| Kéo chuột / WASD & lăn chuột / Q-Z (khi soi 3D) | Xoay và thu phóng vật chứng / hồ sơ trong 3D Physical Inspector |
| Space / M (khi cầm giấy) | Lật mặt giấy. Bấm phải vào dòng trên F101 hoặc AS-05B để đánh dấu, hoặc chọn vùng bằng phím số rồi nhấn M |
| Tab hoặc J theo cấu hình | Mở ba lô. Chọn Dụng cụ, Giấy tờ & vật chứng hoặc Pocket Note. Mở nhật ký điều tra từ Pocket Note để xem ghi chú, liên kết và lịch sử |
| Q khi nhìn bảng kẹp phòng 101 | Đổi lớp tại chỗ trong CH02. Dùng E để mở hồ sơ |
| F | Đèn pin (bóng đổ thật, cảnh báo pin yếu) |
| Shift và Ctrl | Chạy và cúi |
| Esc | Khép trang đọc / thoát soi 3D hoặc mở menu |

Trong Cài đặt có mức đồ họa Low/Medium/High, phím mũi tên, K tương tác, J mở ba lô, L bật đèn pin, cỡ chữ 130%, giảm chuyển động, tăng sáng, âm thanh mono, phụ đề âm thanh, độ nhạy chuột và chỉnh âm lượng tổng/âm nền. Bản tự lưu ghi sau các mốc chính. Lưu tay dùng ở bàn trực trong lễ tân.

## Nguồn và cách dựng

Mở thư mục `Prototype` với Unity 6.3 LTS `6000.3.28f1`. Trên Windows, khi đường dẫn repo có dấu, dùng junction ASCII `C:\CTCC-Unity-Workspace` trỏ đến repo cho trình dựng Unity. `AnSinh_Bootstrap.unity` tải bốn scene nền LOC01–04 rồi tải vùng tầng trên hoặc tầng hầm khi đi tới đầu mối. Nguồn chỉnh sửa nằm tại `ArtSource/Blender/*.blend`, cùng công cụ xuất FBX trong `tools/blender_assets/`.

```powershell
./tools/build_m2.ps1 -SkipGenerate -Output output/m2a/repro-windows
./tools/verify_windows.ps1 -Executable output/m2a/repro-windows/CaTrucCuoiCung.exe -Evidence output/m2a/repro-module
./tools/verify_windows.ps1 -Executable output/m2a/repro-windows/CaTrucCuoiCung.exe -Evidence output/m2a/repro-input -PlayerInput
```

`-SkipGenerate` dựng lại từ scene và dữ liệu chiếu sáng đã commit. Bỏ tham số khi cần dựng lại chúng từ mã authoring.

- [Nghiên cứu An Sinh](docs/prototype/research.md), [mặt cắt và mặt bằng](docs/prototype/layout.md) và [bảng vật liệu](docs/prototype/loc03-reference.png).
- [Chọn Unity và URP](docs/prototype/engine.md), [Bằng chứng nghiệm thu & so sánh BEFORE/AFTER](docs/prototype/verification.md) và [Bảng showcase 8 góc M0-R](docs/prototype/evidence/m0r-showcase-board.jpg).
- [Nguồn tài nguyên của build](Prototype/ThirdPartyNotices.md) và [Production Database v2.2](docs/07_Production_Database.xlsx).
