# Ca trực cuối cùng

Vertical slice Windows x64 của **Ca trực cuối cùng** đi từ cổng phụ bệnh viện An Sinh qua lễ tân, phòng 101 và kho giường đến Cấp cứu. Người chơi có thể chọn một trong ba cách xử lý F101, quay lại sửa phiếu, đổi giữa R và B tại điểm neo, đối chiếu E11/E12 và tải lại ca đã lưu.

Ứng viên **M2-A** dùng Unity 6.3 LTS, URP 17.3 và nguồn Blender 4.5. [Hồ sơ M2-A](docs/m2/acceptance.md) ghi revision, bằng chứng, điều kiện nghiệm thu còn mở và đường găng M3. [Hồ sơ M1-P](docs/prototype/m1p-acceptance.md) giữ kết quả nền trước vertical slice.

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

Mở thư mục `Prototype` với Unity 6.3 LTS `6000.3.28f1`. Trên Windows, khi đường dẫn repo có dấu, dùng junction ASCII `C:\CTCC-Unity-Workspace` trỏ đến repo cho trình dựng Unity. `AnSinh_Bootstrap.unity` tải bốn scene LOC01–04. Nguồn chỉnh sửa được nằm tại `ArtSource/Blender/*.blend`, cùng công cụ xuất FBX trong `tools/blender_assets/`.

```powershell
./tools/build_m2.ps1 -SkipGenerate -Output output/m2a/repro-windows
./tools/verify_windows.ps1 -Executable output/m2a/repro-windows/CaTrucCuoiCung.exe -Evidence output/m2a/repro-module
./tools/verify_windows.ps1 -Executable output/m2a/repro-windows/CaTrucCuoiCung.exe -Evidence output/m2a/repro-input -PlayerInput
```

`-SkipGenerate` dựng lại từ scene và dữ liệu chiếu sáng đã commit. Bỏ tham số khi cần dựng lại chúng từ mã authoring.

- [Nghiên cứu An Sinh](docs/prototype/research.md), [mặt cắt và mặt bằng](docs/prototype/layout.md) và [bảng vật liệu](docs/prototype/loc03-reference.png).
- [Chọn Unity và URP](docs/prototype/engine.md), [Bằng chứng nghiệm thu & so sánh BEFORE/AFTER](docs/prototype/verification.md) và [Bảng showcase 8 góc M0-R](docs/prototype/evidence/m0r-showcase-board.jpg).
- [Nguồn tài nguyên của build](Prototype/ThirdPartyNotices.md) và [Production Database v2.2](docs/07_Production_Database.xlsx).
