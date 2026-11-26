# Ca trực cuối cùng (*The Last Night Shift*)

Game FPS kinh dị tâm lý điều tra độc lập lấy bối cảnh **Bệnh viện Đa khoa An Sinh** hư cấu (1987–2005–2012). Vòng chơi chính đi từ quan sát hiện trường, đối chiếu vật chứng hai mặt đến ký quyết định hồ sơ, rồi đối diện hậu quả ngay trong không gian giữa các lớp thực tại **R (2012)**, **B (2005)** và **M (ký ức)**.

- **Nguồn chân lý duy nhất (Single Source of Truth):** **[Core v3.6.2 — Documentation Consolidation](docs/RELEASE-NOTES.md)** tại `docs/01…07` kèm [dialogue_silent_exploration_runtime.json](docs/dialogue_silent_exploration_runtime.json) (11/11 mã SHA-256 khóa tại [SHA256SUMS.txt](docs/SHA256SUMS.txt)).
- **Bản đồ tài liệu toàn workspace:** [docs/INDEX.md](docs/INDEX.md) · **Vận hành & skill:** [AGENTS.md](AGENTS.md) · [docs/agent/README.md](docs/agent/README.md).

## Trạng thái sản xuất hiện tại

- **Chuẩn thiết kế:** **Core v3.6.2** (`100` task trong [Production Database](docs/07_Production_Database.xlsx) / 28 sheet / 150 QA; M0/M1 hoàn thành, M2 đang mở). Áp dụng **Khám phá Im lặng & Chữ nội tâm không VO (`DEC-VO-01`, 82/143 dòng VO)** và hợp nhất **toàn bộ walkthrough CH01 B01–B08 duy nhất vào [Core 04](docs/04_Level_Chapter_Design.docx)**.
- **Lưu ý tích hợp runtime:** Gói `Core v3.6.2` là bản hợp nhất tài liệu thiết kế (`DEC-V36-CH01-01` / `DEC-V361-CH01-01`); các hạng mục bridge runtime mới ở CH01 (`TASK031-A/B`, `V36-CH01-Q01..Q08`) chưa nghiệm thu trên build Unity.
- **M2 Vertical Slice (`c459aba`):** Ứng viên kỹ thuật nền trước v3.6 ([Hồ sơ M2-FINAL](docs/m2/m2-final-candidate-report.md)); các cổng con người gồm playtest 6 người chơi mới ([TASK010 Playtest Kit](docs/m2/playtest-kit-task010.md)) và thẩm định chuyên gia ([TASK026–028 Dossiers](docs/m2/specialist-review-dossiers.md)) đang mở.
- **Quần thể & Mở rộng kỹ thuật M3:** Đã tích hợp quần thể 6 khối An Sinh ([Campus-v1](docs/campus/an-sinh-full-campus-expansion-report.md)), kiểm chứng kỹ thuật AI & CH03–CH04 ([M3.2 `c90a2eb`](docs/m3/m32-implementation-and-acceptance-report.md)), và triển khai kỹ thuật CH05 + đợt mỹ thuật CH01 ([M3.3 Phase A](docs/m3/m33-phase-a-audit-and-test-plan.md), [CH01 Art](docs/ch01r/art-production.md)).

## Điều khiển

| Phím | Thao tác |
| --- | --- |
| `WASD` và chuột | Di chuyển và nhìn |
| `E` | Cầm bì thư/tài liệu, mở hộp thư, giở sổ trực, mở cửa và thao tác với vật thể đang nhìn |
| Giữ chuột trái khi nhìn tay nắm | Kéo cửa, ngăn, rèm hoặc ghế. Thả để giữ vị trí, `Esc` để hủy |
| Kéo chuột / `WASD` & lăn chuột / `Q`–`Z` (khi soi 3D) | Xoay và thu phóng vật chứng / hồ sơ trong 3D Physical Inspector |
| `Space` / `M` (khi cầm giấy) | Lật mặt giấy (`T` để soi xiên mặt sau). Bấm phải vào dòng trên F101 hoặc AS-05B để đánh dấu, hoặc chọn vùng bằng phím số rồi nhấn `M` |
| `Tab` hoặc `J` theo cấu hình | Mở ba lô (Pocket Note dùng để đọc lại/theo dõi tùy chọn; CH01 không bắt buộc Journal gameplay) |
| `Q` khi nhìn bảng kẹp phòng 101 | Đổi lớp tại chỗ trong CH02. Dùng `E` để mở hồ sơ |
| `F` | Đèn pin (bóng đổ thật, cảnh báo pin yếu) |
| `Shift` và `Ctrl` | Chạy và cúi |
| `Esc` | Khép trang đọc / thoát soi 3D hoặc mở menu |

Trong Cài đặt có mức đồ họa Low/Medium/High, phím mũi tên, `K` tương tác, `J` mở ba lô, `L` bật đèn pin, cỡ chữ 130%, giảm chuyển động, tăng sáng, âm thanh mono, phụ đề âm thanh định hướng, hộp chữ nội tâm tương phản cao (`HighContrastThoughtBox`), tốc độ đọc chữ nội tâm (`ThoughtReadDurationScale`), chế độ Cốt truyện (Story Mode), độ nhạy chuột và âm lượng tổng/âm nền. Bản tự lưu ghi sau các mốc chính theo Save Schema v7; lưu tay dùng ở bàn trực trong lễ tân.

## Stack, Tra cứu Core & Cách dựng

Project dùng **Unity 6.3 LTS `6000.3.28f1`**, **URP 17.3 Forward+**, **Windows x64 (D3D12)** và nguồn **Blender 4.5.10 LTS**. Mở thư mục `Prototype` bằng Unity; trên Windows khi đường dẫn repo có dấu tiếng Việt, dùng junction ASCII `C:\CTCC-Unity-Workspace` trỏ tới repo cho trình dựng Unity. `AnSinh_Bootstrap.unity` tải 5 scene nền (`LOC01–04` + `Campus_Grounds`) và stream theo ngữ cảnh 3D XYZ cho tầng trên, B1 và `Campus_Interiors`. Nguồn chỉnh sửa nằm tại `ArtSource/Blender/*.blend`, cùng bộ công cụ xuất/bake trong `tools/`.

```powershell
# Kiểm tra toàn vẹn bộ nguồn Core v3.6.2, đồng bộ JSON runtime và trạng thái Backlog/Roadmap
python docs/agent/project_docs.py check
python docs/agent/project_docs.py hashes
python docs/agent/project_docs.py status
python tools/sync_production_data.py

# Dựng lại bản Windows x64 và chạy bộ kiểm chứng tự động
./tools/build_m2.ps1 -SkipGenerate -Output output/repro/windows
./tools/verify_windows.ps1 -Executable output/repro/windows/CaTrucCuoiCung.exe -Evidence output/repro/module
./tools/verify_windows.ps1 -Executable output/repro/windows/CaTrucCuoiCung.exe -Evidence output/repro/input -PlayerInput
```

`-SkipGenerate` dựng lại từ các scene và dữ liệu chiếu sáng đã commit; bỏ tham số khi cần tái sinh scene từ mã authoring.

## Liên kết tra cứu nhanh

- **Thiết kế chuẩn:** [Core v3.6.2 Release Notes](docs/RELEASE-NOTES.md) · [Canon Audit](docs/CANON-AUDIT.md) · [Core 04 Level & CH01 Walkthrough](docs/04_Level_Chapter_Design.docx) · [Production Database](docs/07_Production_Database.xlsx)
- **Kiểm chứng & Cổng đang mở:** [Hồ sơ M2-FINAL (`c459aba`)](docs/m2/m2-final-candidate-report.md) · [Hồ sơ M3.2 (`c90a2eb`)](docs/m3/m32-implementation-and-acceptance-report.md) · [Playtest Kit TASK010](docs/m2/playtest-kit-task010.md) · [Specialist Dossiers TASK026–028](docs/m2/specialist-review-dossiers.md)
- **Nghiên cứu, Kiến trúc & Bản quyền:** [Quần thể An Sinh Campus-v1](docs/campus/an-sinh-full-campus-expansion-report.md) · [Chọn Unity & URP](docs/prototype/engine.md) · [Nguồn tài nguyên & Giấy phép](Prototype/ThirdPartyNotices.md) · [Danh mục tài liệu đầy đủ](docs/INDEX.md)
