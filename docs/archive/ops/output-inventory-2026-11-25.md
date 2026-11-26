# Kiểm kê đầu ra dự án — 25/11/2026

Đây là ảnh chụp để quyết định dọn dẹp, không phải lệnh dọn. Đã rà trong `C:\Ca trực cuối cùng`, tập trung vào `output/`; không xóa hoặc di chuyển đầu ra. Công việc này ngoài các task sản xuất trong Backlog và không thay đổi thiết kế game.

## Kết quả chính

- `output/`: **63,870 GiB**, 191.624 tệp, 39 thư mục con cùng các tệp ở gốc; số liệu là tổng kích thước logic của tệp.
- Ổ C còn khoảng **12,5 GiB** lúc 21:05 ngày 25/11/2026, có thể thay đổi trong khi máy hoạt động. Số GiB đo theo 1 GiB = 1.073.741.824 byte.
- `.gitignore` bỏ qua toàn bộ `output/`; tại `HEAD ad02220`, Git không theo dõi tệp nào ở đó. Điều này không chứng minh các tệp không có bản sao ở nơi khác. Không mặc nhiên coi `git push` là sao lưu chúng.
- Đã kiểm 52 vị trí có `CaTrucCuoiCung.exe` trong `output/`; đây là số bản player tìm thấy, không phải số phiên bản hoàn toàn khác nhau. Những thư mục có cùng dung lượng hoặc nội dung tương tự chưa chắc là bản sao giống hệt.
- Bốn cache `Library` trong các checkout M2 cũ chiếm **12,048 GiB**. Năm player CH01 trung gian chiếm **3,361 GiB** và là các build khác nhau. Một thư mục giải nén M3.2 chưa hoàn chỉnh có **0,280 GiB** trùng SHA-256 với những tệp tương ứng trong ZIP. Ba nhóm không chồng nhau, tổng tối đa theo kích thước logic **15,688 GiB**; điều kiện và hệ quả ở dưới.

## Những thứ cần bảo vệ trước

1. **M2 đang cần playtest người thật.** [Bộ hướng dẫn TASK010](../../m2/playtest-kit-task010.md) ghi 0/6 phiên; Roadmap/Backlog cũng còn M2 mở. Giữ `output/m2-final/m2-final-release-c459aba/`, ZIP, `SHA256SUMS.txt`, `start-task010-session.ps1`, QA và dữ liệu phiên nếu phát sinh. Script cần bản build được giải nén hoặc `-BuildDir` phù hợp. Không suy luận đã nghiệm thu từ việc có ZIP.
2. **Nguồn build M2 chứa thay đổi chưa commit.** `output/m2-final/checkpoint-repo/` có repository riêng, dùng chung Git cho bốn worktree nằm bên dưới `output/m2-final`. Đã thấy lần lượt 1, 232, 7, 236 và 0 tệp thay đổi trong checkpoint và bốn worktree. Xóa nguyên checkpoint có thể làm mất dữ liệu chưa commit và metadata Git của các worktree. [Rà soát M3.2](../m3-history/m32/m32-phase-a-audit-and-test-plan.md) giải thích provenance của 9 scene. Trong lượt kiểm kê này, cả **9/9 scene** trong `v33-host-20261125-103747/player-source` khớp SHA-256 với build identity M2; giữ những nguồn này và Git liên quan.
3. **Giữ bản M3.2, video và bằng chứng hiện hành.** Thư mục `output/m3.2/` có Windows candidate `c90a2eb`, ZIP, video, ảnh và dữ liệu hiệu năng; xem [báo cáo M3.2](../../m3/m32-implementation-and-acceptance-report.md). Các gói M2 và M3.2 có SHA-256 khớp manifest lưu cạnh gói; `zipfile.testzip()` trả kết quả tốt cho cả hai. Đây là kiểm toàn vẹn tệp ZIP, không phải lượt chạy lại game.
4. **Giữ save, log, ảnh, video và dữ liệu nguồn cho tới khi xác định giá trị.** Ví dụ `output/ch01-world-first/playtest-local/save/auto.json` và `.bak` thực sự tồn tại. Một số thư mục `isolated-save` là dữ liệu QA; tên thư mục không đủ để kết luận toàn bộ đều vô giá trị. `output/material-staging/source/` còn được `tools/bake_surfaces.py` sử dụng.

## Phương án dọn để lựa chọn

Chưa thực hiện. Các số bên dưới là tổng kích thước logic trước khi lưu lại tài liệu cần thiết; dung lượng ổ đĩa thực thu hồi có thể khác.

| Nhóm | Đường dẫn cụ thể | Dung lượng | Điều kiện và hệ quả |
| --- | --- | ---: | --- |
| A — cache Unity trong checkout cũ | `output/m2-final/host-verify-20261125-005837-20f3c1/generation-source/Prototype/Library/` | 2,491 GiB | Cache Unity thường có thể tạo lại khi mở project, nhưng phải import lại, mất thời gian/dung lượng tạm và không bảo đảm tái dựng build cũ y hệt. Giữ toàn bộ `Assets`, scene, `.git`, log và phần còn lại của checkout. Kiểm tra Unity đã đóng trước khi thao tác. |
| A | `output/m2-final/host-verify-20261125-005837-20f3c1/player-source/Prototype/Library/` | 3,468 GiB | Cùng điều kiện như trên; không xóa cả `player-source`. |
| A | `output/m2-final/v33-host-20261125-103747/player-source/Prototype/Library/` | 3,524 GiB | Cùng điều kiện; **đặc biệt giữ** các scene đã xác minh trong `Assets` và metadata Git của worktree. |
| A | `output/m2-final/checkpoint-repo/Prototype/Library/` | 2,565 GiB | Cùng điều kiện; **không xóa** `checkpoint-repo/.git`. |
| B — build CH01 trung gian | `output/ch01-world-first/candidate-1/` đến `candidate-5/` | 3,361 GiB | Năm assembly khác nhau: đây là các phiên bản cũ, không phải 5 bản sao đã chứng minh trùng. Chỉ xét dọn nếu không cần mở chính xác từng bản; trước đó giữ `build-identity.txt`, log và bằng chứng có liên quan. Giữ `windows/`, QA và `playtest-local/`. Không tìm thấy tham chiếu trực tiếp tới 5 đường dẫn này trong phạm vi tìm bằng văn bản ở `docs`, `tools` và README, nhưng không loại trừ cách dùng ngoài phạm vi đó. |
| C — bản giải nén M3.2 thiếu | `output/m3.2/m32-release-c90a2eb/CaTrucCuoiCung-M3.2-c90a2eb-Windows-x64/` | 0,280 GiB | Có 32 tệp, tất cả khớp SHA-256 với tệp cùng tên trong ZIP đã kiểm; thư mục này thiếu `CaTrucCuoiCung.exe`, còn ZIP có EXE. Có thể xét bỏ thư mục giải nén dở sau khi xác nhận không còn lượt giải nén/chạy dùng nó; giữ ZIP, player candidate, video, log và các tệp khác trong `m32-release-c90a2eb/`. |

Tổng A = **12,048 GiB**; A + B + C = **15,688 GiB**. Nếu chỉ muốn ít rủi ro và chấp nhận Unity import lại, A là nhóm nên xem trước. B giải phóng thêm nhưng đánh đổi khả năng mở đúng bản trung gian. C ít dung lượng, song trùng lặp đã được kiểm SHA-256. Không nên coi tổng này là không gian chắc chắn thu hồi cho đến khi thao tác thực tế.

Ngoài `output/`, `Prototype/Library/` của project chính khoảng **3,775 GiB**. Nó không nằm trong tổng trên; dọn nó sẽ khiến project chính phải import lại, nên để riêng khỏi lựa chọn A.

## Danh mục toàn bộ thư mục cấp một trong `output/`

Bảng này mô tả nội dung chủ yếu. Một thư mục có thể đồng thời chứa build, QA và save; lời khuyên không cho phép xóa cả thư mục khi bên trong còn nội dung cần giữ.

| Đường dẫn | GiB | Số tệp | Loại đầu ra | Đề xuất sơ bộ |
| --- | ---: | ---: | --- | --- |
| `output/m2-final/` | 22.866 | 154,268 | M2, worktree, build, QA và phục hồi | Giữ gói M2 và nguồn/bằng chứng; chỉ xét từng cache `Library`. |
| `output/ch01-rebirth-ii-world/` | 5.296 | 6,447 | CH01: bản chơi, capture, ảnh và QA | Giữ `windows`, media và bằng chứng được dẫn; các candidate xét riêng. |
| `output/ch01-rebirth-ii/` | 5.038 | 2,038 | CH01: baseline, candidate, profile và QA | Giữ bản phát hành và dấu vết; xét riêng các bản chạy thử cũ. |
| `output/ch01-world-first/` | 5.035 | 1,715 | CH01: candidate, bản chơi, QA và save | Giữ `windows`, QA và `playtest-local`; 5 candidate có thể xét bỏ có điều kiện. |
| `output/m31/` | 3.955 | 3,357 | M3.1: nhiều Windows player, ZIP và QA | Giữ ZIP, bản baseline đang được dẫn và log; rà từng bản khác trước khi dọn. |
| `output/ch01-review/` | 2.949 | 1,136 | CH01: nhiều bản Windows và lượt QA | Xét từng build cũ; giữ log, ảnh và kết quả review. |
| `output/m3.2/` | 2.276 | 4,601 | M3.2: candidate, ZIP, QA, đo hiệu năng và video | Giữ candidate, ZIP, QA và video; xét riêng bản giải nén thiếu file. |
| `output/task03-f101/` | 2.265 | 2,455 | F101: build trước/sau, QA và ghi hình | Cân nhắc các bản Windows cũ sau khi giữ bằng chứng so sánh. |
| `output/f101-annotations/` | 1.515 | 726 | F101: hai build và QA | Giữ thông tin cuối và bằng chứng; bản cũ xét riêng. |
| `output/task04-05/` | 1.453 | 549 | Task 04–05: build và QA | Giữ dấu vết cuối; bản build cũ xét riêng. |
| `output/campus/` | 1.336 | 590 | Campus: build, ảnh và các lượt QA | Giữ bằng chứng phục vụ thiết kế và review; xem lại bản build cũ. |
| `output/ch01-walkthrough/` | 1.273 | 808 | Walkthrough CH01: bản chơi, QA và ảnh | Giữ bằng chứng walkthrough; đánh giá nhu cầu chạy lại build. |
| `output/ch01r/` | 1.159 | 1,161 | CH01-R: Windows, ZIP, benchmark và QA | Giữ ZIP và bằng chứng; đánh giá nhu cầu giữ bản giải nén. |
| `output/m30/` | 1.005 | 1,054 | M3.0: bản chơi, QA, ảnh, tệp Blender | Giữ bằng chứng và kiểm nguồn Blender trước khi xét build cũ. |
| `output/audit-head/` | 0.903 | 213 | Player cho lượt audit một revision | Xét sau khi giữ định danh và kết quả audit. |
| `output/lan-encounter/` | 0.896 | 2,196 | Lan: player, ảnh trước/sau, công cụ và QA | Giữ media, script và QA; bản Windows cũ xét riêng. |
| `output/ch01-audit-head/` | 0.859 | 297 | Player và ảnh cho audit CH01 | Xét sau khi giữ định danh và kết quả audit. |
| `output/ch01-upgrades/` | 0.843 | 394 | CH01: player, ảnh và QA | Giữ bằng chứng; xét nhu cầu chạy lại player cũ. |
| `output/m30-main-31e06c2/` | 0.777 | 335 | M3.0: build cũ và QA | Giữ log và định danh; xét riêng build sau khi kiểm nhu cầu đối chiếu. |
| `output/f101-deduction/` | 0.718 | 265 | F101: player và QA | Giữ kết quả; xét nhu cầu chạy lại player cũ. |
| `output/github-push-20261123/` | 0.570 | 2,029 | Dữ liệu staging Git: blobs, objects và manifest | Giữ tạm; chưa xác minh bản sao và khả năng khôi phục. |
| `output/audio-tools/` | 0.300 | 4,092 | Công cụ, môi trường Python, âm thanh thô và model | Giữ tạm; xem từng phần và khả năng tải/tạo lại. |
| `output/material-staging/` | 0.204 | 75 | Nguồn texture, audio, mesh và ZIP tham khảo | Giữ provenance; công cụ bake vẫn chỉ vào `source`. |
| `output/m0r-evidence/` | 0.112 | 426 | M0-R: video, ảnh, log và QA | Giữ bằng chứng; dung lượng nhỏ. |
| `output/audit-20261124/` | 0.101 | 79 | Ảnh, log và dữ liệu audit | Giữ bằng chứng; dung lượng nhỏ. |
| `output/audit-head-module/` | 0.101 | 79 | Ảnh, log và dữ liệu audit | Giữ bằng chứng; dung lượng nhỏ. |
| `output/m2-evidence/` | 0.014 | 28 | Ảnh render Blender | Giữ bằng chứng; dung lượng nhỏ. |
| `output/` (các tệp tại gốc) | 0.012 | 16 | ZIP capture M1P, Git bundle Core, log và script | Giữ bundle/capture; xem từng log riêng nếu cần. |
| `output/qa-final-windows/` | 0.008 | 20 | Ảnh, log, PresentMon, số đo và save QA | Giữ bằng chứng và công cụ được tài liệu tham chiếu. |
| `output/qa-final-editor/` | 0.006 | 15 | Ảnh và dữ liệu QA Editor | Giữ bằng chứng; dung lượng nhỏ. |
| `output/qa-reference-v2/` | 0.006 | 15 | Ảnh và dữ liệu QA tham chiếu | Giữ bằng chứng; dung lượng nhỏ. |
| `output/world-architecture-20261123/` | 0.004 | 51 | Backup, tài liệu kiến trúc, hình và script | Giữ nguồn và lịch sử; dung lượng nhỏ. |
| `output/office-stage/` | 0.004 | 35 | Tài liệu gốc/đã sửa, proof và script | Giữ lịch sử tài liệu; dung lượng nhỏ. |
| `output/core-v3.1/` | 0.003 | 14 | Lưu trữ Core v3.0/v3.1 và hình | Giữ lịch sử; dung lượng nhỏ. |
| `output/core-v30-publish/` | 0.003 | 13 | Tài liệu Core v3.0, patch và công cụ phát hành | Giữ lịch sử; dung lượng nhỏ. |
| `output/qa-reference/` | 0.003 | 14 | Ảnh và dữ liệu QA tham chiếu | Giữ bằng chứng; dung lượng nhỏ. |
| `output/core-v30-inspect/` | 0.001 | 7 | Bản Office Core v3.0 | Giữ lịch sử; dung lượng nhỏ. |
| `output/m30-main-verification/` | 0.000 | 1 | Log build thử | Giữ log; dung lượng rất nhỏ. |
| `output/qa-editor/` | 0.000 | 7 | Dữ liệu QA Editor | Giữ bằng chứng; dung lượng rất nhỏ. |
| `output/play-smoke-save/` | 0.000 | 3 | Save thử smoke | Xem như dữ liệu lưu; không dọn chung với cache. |

## Liên kết và giới hạn kiểm kê

- Quét liên kết Markdown có chứa `output/` trong `README.md` và `docs/**/*.md` thấy **ít nhất 55 liên kết hiện không có đích tại máy này**: 22 liên kết đến `output/m2a/` trong [M2 acceptance](../../m2/acceptance.md), 33 liên kết đến `output/m2c/` trong [M2-C acceptance](../../m2/m2c-acceptance.md). Đây là số liên kết, không nhất thiết là số tệp khác nhau. Không xác định được nguyên nhân từ lượt rà này. Các đường dẫn viết dạng mã thường và tài liệu Office chưa được kiểm đầy đủ.
- Hai junction `output/m31/tracker/node_modules` và `output/ch01-world-first/source-update/node_modules` trỏ đến thư viện cài đặt bên ngoài workspace. Phép đếm **không đi theo** hai junction. Nếu sau này dọn, cần xử lý chính junction đúng cách, không duyệt xóa đệ quy đích bên ngoài.
- `C:\CTCC-Unity-Workspace` là junction trỏ về chính `C:\Ca trực cuối cùng`, không phải một bản output thứ hai để cộng thêm. Các worktree ở vị trí khác, thư mục người dùng, GitHub Releases và mọi bản sao lưu ngoài workspace **chưa được kiểm kê**.
- Kiểm kê dựa trên tên, cấu trúc, kích thước, một số tham chiếu và kiểm định danh có chọn lọc; không chạy lại từng build hay xác nhận tính khôi phục của từng tệp. Không có lỗi truy cập khi quét. Không suy từ kết quả này rằng mọi tệp không được tài liệu nhắc tới đều có thể xóa.
