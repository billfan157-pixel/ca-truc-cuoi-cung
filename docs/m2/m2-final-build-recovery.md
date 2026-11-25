# M2-FINAL · Checkpoint Phase A/B và khôi phục Unity

**Trạng thái:** Phase B **Accepted / Closed** trên `fb2e96d` (ngày 25/11/2026); đã chuyển sang M2-FINAL Phase C–F (`TASK007/008/009` + Integrated QA Candidate + gói `TASK010`). Cổng đóng M2 (`TASK010` và `TASK026–028`) vẫn giữ mở.

## Checkpoint

| Mục | Kết quả |
| --- | --- |
| Commit | `9c2bf959b39dfb8def0cc8f2b3738df514b87d93` trên `checkpoint/m2-final-phase-ab` |
| Parent | `c0b1bbd75805c5e947a3cb5895a00d2e1a0c198e` |
| Phạm vi | 24 tệp gồm runtime, dữ liệu, prop, QA, hồ sơ Phase A/B và log kiểm soát. Hướng dẫn QA khớp blob đã duyệt `3958333494d6d71667c45c98b3e6cf8a285827e0`. |
| Checkout | `output/m2-final/checkpoint-repo`. Git status sạch. Checkout gốc bị từ chối tạo `.git/index.lock`. |
| Git bundle | `output/m2-final/m2-final-phase-ab.bundle`. Đã chạy `git bundle verify`. SHA-256 `2cf72c5ba6ed9460d242ea8d3c1521953a33b3e9b4b54ecc8a09203d85170f7d`. Bundle cần parent nêu trên. |
| Kiểm chứng | [Biên dịch Roslyn](evidence/phase-ab/roslyn-compile.txt), [11/11 logic smoke test](evidence/phase-ab/logic-smoke.txt), [10/10 SHA-256](evidence/phase-ab/core-sha256-verification.txt), [báo cáo triển khai S1](m2-final-phase-b-sync.md). |

Đã rà diff và so parent với checkpoint. Core v3.3.1, scene YAML, ProjectSettings, package manifest/lock và mã hợp đồng Save Schema v7 khớp parent. Unity `6000.3.28f1` revision `8f082d2fe476`, URP `17.3.0`, có Windows Standalone Support. Chưa có build identity của Windows Player cho commit này.

## Bằng chứng lỗi môi trường

| Quan sát | Bằng chứng | Kết luận có thể xác nhận |
| --- | --- | --- |
| Licensing IPC | Các lượt dựng từ project ghi `Connection … refused` và timeout khởi tạo. `unity license status` trả `LICENSING_CLIENT_UNAVAILABLE`, mã 6. | Phiên chạy không truy cập được Licensing Client. Trạng thái entitlement hiện tại chưa đọc được. |
| Kho tài khoản | `unity auth status` trả `LOCAL_STORE_UNWRITABLE`, mã 6. | Phiên chạy không mở được kho tài khoản cục bộ. Riêng lỗi này chưa xác định tài khoản đã đăng nhập hay chưa. |
| Editor crash | Stack ghi `wakeup_pipes_init` qua socket của `UnityEditor.BuildReportRestService`. Unity 6000.3.28f1 lặp lại stack trên project trống tạo bằng `-createProject`. | Lỗi Editor tái hiện ngoài project CTCC. Báo cáo phân biệt lỗi khởi tạo socket với lỗi truy cập Licensing IPC. Quan hệ nhân quả giữa hai lỗi chưa được xác lập. |
| WMI | `WindowsSystemInformation.get_BIOSIdentifier` báo `UnauthorizedAccessException`. | Cùng phiên chạy, truy vấn WMI bị từ chối. Chưa có bằng chứng đây là nguyên nhân trực tiếp của crash. |

Bản sao log nằm ở `output/m2-final/recovery-diagnostics`. Đã thu `Editor.log`, hai log build CTCC, log project trống, `Unity.Licensing.Client.log`, `upm.log` và `Hub-cli-log.json`. `Hub-info-log.json` không có ở vị trí chuẩn, còn hai thư mục crash của các lượt trước không truy cập được. Lượt project trống tạo được `crash.dmp` (689.511 byte) và bản log trong `empty-project-crashes`. Log lịch sử và dump lưu riêng với checkpoint.

## Thao tác người vận hành

1. Mở PowerShell Windows thông thường bằng tài khoản dùng để chạy Unity. Chạy `unity auth status` và `unity license status`.
2. Nếu CLI yêu cầu đăng nhập, chạy `unity auth login` và hoàn tất trong trình duyệt. Khi không có license active, dùng `unity license activate` cho entitlement đã được cấp. Nếu dùng Personal và chấp nhận điều khoản, dùng `unity license activate --personal --accept-eula`. Có thể thực hiện qua Unity Hub → Settings → Licenses nếu Hub có sẵn. [Unity CLI](https://docs.unity.com/en-us/unity-cli/unity-cli-security) · [Unity Hub](https://docs.unity.com/en-us/hub/manage-license).
3. Khi trạng thái license đọc được, mở project checkpoint bằng Editor 6000.3.28f1. Có thể dùng lệnh sau trong cùng phiên Windows.

```powershell
& "$env:USERPROFILE\Unity\Editors\6000.3.28f1\Editor\Unity.exe" -projectPath "C:\CTCC-Unity-Workspace\output\m2-final\checkpoint-repo\Prototype"
```

Nếu Editor vẫn crash trong phiên Windows thông thường, đối chiếu log mới với log project trống và dump đã thu. Chẩn đoán hiện tại chưa đủ căn cứ để thay phiên bản Editor hoặc kết luận license đã hết hạn. [Unity 6.3 log files](https://docs.unity.com/en-us/engine/6000.3/manual/get-started/project-configuration/log-files).

## Cổng tiếp tục

- Khi Editor mở và import ổn định, xác minh vị trí cửa xả sân S1 rồi tái sinh scene chịu ảnh hưởng theo topology đã duyệt. Kiểm diff scene trước khi tạo revision Windows x64.
- Dựng Player từ revision xác định, ghi build identity và chạy Module QA, Player Input QA, LOC01 sau T09, E03, C12, save/load và V33-Q05. Kiểm thực tế LOC02 ↔ LOC05 ↔ LOC07 trong R trước G02, rồi quay lại tuyến CH02 và kiểm nội dung/tín hiệu CH03 vẫn theo điều kiện tiến trình.
- Phase C `TASK007/008` chờ kết quả tích hợp Phase B trên Player.

## Kiểm tra lại sau khi Windows bật license

- Người dùng xác nhận Unity Personal active và Editor `6000.3.28f1` PID `18508` mở checkout `9c2bf95`. Log được sao lưu tại `output/m2-final/recovery-diagnostics/Editor-live-9c2bf95.log`. Có chín lượt Asset Pipeline Refresh trong bản sao đã phân tích. Tra các mẫu lỗi C#, exception, import, shader, NavMesh, Licensing và crash trong log Editor cùng năm log worker cho kết quả không thấy lỗi khớp mẫu. Đây là kiểm log. Bộ đếm lỗi đỏ và toàn bộ 239 warnings trên Console trực tiếp chưa được xác minh.
- Nhóm warnings tìm thấy gồm shader Terrain/Lit URP bị loại subshader và hướng dẫn liên quan đến renderer/fallback. `CS0618` xuất hiện hai lần cho một vị trí ở `HospitalAuthoring.Lighting.cs:253`, API `Lightmapping.giWorkflowMode`. Warnings shader được ghi lặp ở cả Editor và worker. Cần đối chiếu shader trên Windows D3D12 Player trước khi kết luận ảnh hưởng.
- `UnityLockfile` hiện diện lúc đầu rồi biến mất. Cuối Editor.log ghi PID 18508 bắt đầu shutdown, ngắt Licensing IPC thành công và dừng Package Manager. Hai tệp tự ghi lúc import, `Mobile_RPAsset.asset` và `ShaderGraphSettings.asset`, đã được sao lưu ở `output/m2-final/checkpoint-player-verification/editor-import-backup` trước khi phục hồi checkout sạch để duy trì checkpoint.
- Yêu cầu tool thực thi ngoài sandbox bị chính sách phiên tự động từ chối. Batch scene generation chạy từ checkout sạch `9c2bf95` sau khi lock biến mất. Unity vẫn lỗi Licensing IPC và crash ở `UnityEditor.BuildReportRestService` qua `wakeup_pipes_init`. Lệnh thoát thất bại, không có marker `CTCC_SCENE`. Log `output/m2-final/checkpoint-player-verification/scene-generation/generate-scenes.log`. File lock xuất hiện lại sau lượt crash. Lượt chạy tiếp nên dùng checkout mới để tránh lock còn sót.
- Đã chuẩn bị `output/m2-final/verify-phase-b-on-windows.ps1` cho phiên PowerShell Windows đã kích hoạt license. Runner tạo hai worktree từ cùng checkpoint, giữ kết quả tái sinh scene để so diff và dựng Windows Player riêng từ scene commit. Sau build, runner gọi Module QA, Player Input QA và ba lượt PresentMon D3D12 1080p Medium. Đã kiểm cú pháp PowerShell. Ca đi bộ và kiểm cửa xả sân S1 cần thực hiện trên Player, sau đó đọc kết quả V33-Q05 theo đúng phạm vi đã chạy. Phase B vẫn Implemented — Player Verification Pending.

```powershell
& 'C:\CTCC-Unity-Workspace\output\m2-final\verify-phase-b-on-windows.ps1'
```

## Runner trên Windows đã kích hoạt license

- Lượt `host-verify-20261125-005837-20f3c1`, revision `9c2bf959b39dfb8def0cc8f2b3738df514b87d93`, chạy scene generation trên Windows và Unity thoát mã 0. Có marker `CTCC_SCENE`, `CTCC_GREYBOX_PASS`, `W-Q02` và `W-Q03`. Bake GI đạt 369 probe mỗi lớp R/B và occlusion đạt trên 9 scene. Tổng hợp marker ở `scene-generation-summary.txt` trong thư mục lượt chạy, log đầy đủ ở `scene-generation/generate-scenes.log`.
- Runner ban đầu đứng ở lệnh xuất binary diff sau khi Unity đã thoát. Tại thời điểm kiểm tra, `scene-generation.patch` vẫn 0 byte, Windows trả sharing violation khi mở độc quyền và chưa có `player-source`. Scene tái sinh vẫn có trong `generation-source`. Đã đổi runner cho lượt sau sang lưu danh sách tệp thay đổi và chuẩn bị `output/m2-final/resume-phase-b-on-windows.ps1` để dùng lại phần generation đã đạt.
- Windows Player, Module QA, Player Input QA, đi bộ S1 trước G02 và benchmark cần bằng chứng riêng trên cùng build. Kết quả editor scene generation chỉ chứng minh các ca greybox đã ghi trong log.

## Windows Player và lượt chạy QA tiếp theo

| Hạng mục | Tiêu chí | Bằng chứng hiện có |
| --- | --- | --- |
| Windows x64 | Player đúng revision, Unity ghi build thành công và thoát mã 0, có hash assembly và scene | `windows-x64/build.log` ghi `CTCC_BUILD` và mã thoát 0. `qa-recovery-20261125-020545/build-identity.txt` ghi revision `9c2bf95`, Core manifest, hash EXE, assembly, scene nguồn và dữ liệu level trong Player. Các cảnh tái sinh được lưu trong checkout riêng. |
| D3D12 và Module QA | Player hoàn thành suite, ghi `RESULT=PASS` và thoát mã 0 | Lượt `qa-recovery-20261125-020545` khởi tạo D3D12 trên Intel Arc, tạo 34 ảnh và thư mục kiểm migration schema 1–6. Log ghi tải LOC05, LOC09 và Campus Interiors. Thiếu `gameplay.txt` và bằng chứng thoát bình thường. |
| V33-Q05, Player Input QA và benchmark | Kết quả trên cùng Player, gồm ca S1 trước G02, ba lượt benchmark độc lập | Chưa có verdict Module QA, marker V33-Q05, kết quả Player Input QA hay capture benchmark. Ca đi bộ S1 và kiểm cửa xả sân còn chờ. |

- Tại lúc ghi identity, `git status --porcelain` liệt kê bảy tệp cấu hình Unity. Kiểm tra lại bằng `git diff --name-status HEAD -- Prototype` và `git diff --ignore-space-at-eol --exit-code HEAD -- Prototype` đều trả kết quả rỗng, mã 0. Giữ nguyên identity gốc và ghi nhận cả hai quan sát.
- Hai mục `QA interruption while saving CH02-T06` và `Sharing violation ... auto.json.lock` khớp các ca cố ý tiêm lỗi trong `HospitalGame.QA.cs`, kể cả giữ lock để kiểm rollback B→R. Chưa có đủ bằng chứng để xác định verdict của ca hoặc nguyên nhân kỹ thuật dừng Player.
- Duyệt tự động chặn yêu cầu tiếp tục phiên thực thi vì Unity có thể gửi dữ liệu telemetry/crash chưa xác định tới dịch vụ Visual Studio bên ngoài. Sau thông báo chặn đã kiểm tra bằng chứng có sẵn. Không có log xác nhận đã gửi dữ liệu. Phase B vẫn `Implemented — Player Verification Pending`.

## Xác minh sau khi được phép chạy lại

Lượt `qa-authorized-20261125-020832` dùng Windows x64 từ `9c2bf959b39dfb8def0cc8f2b3738df514b87d93`, Unity `6000.3.28f1`, Core `3.3.1 Final Clean`, D3D12 trên Intel Arc. Sau lần xuất diff scene bị kẹt, QA dùng Player dựng từ scene đã commit. Lượt tái sinh bằng Editor được lưu riêng. Hash EXE, assembly, scene nguồn và dữ liệu level trong `build-identity.txt`.

| Hạng mục | Expected | Actual |
| --- | --- | --- |
| Module QA và D3D12 normal exit | `RESULT=PASS` và mã thoát `0` | Đạt. Có kết quả theo ID cho ba nhánh TASK025-A, stress TASK025-B, guard thoại, streaming, E05b và kiểm hồi quy CH03–CH04. `module/gameplay.txt`. |
| Player Input QA | `RESULT=PASS` và mã thoát `0` trên cùng binary | Đạt. Ba nhánh F101, tuyến CH01-R qua controller, save/load, CH02 door A/B, CH03–CH04 qua cầu thang và stress Inspector/Journal. `player-input/gameplay.txt`. |
| D3D12 failure probe | Ca tiêm lỗi `module-failure` ghi `RESULT=FAIL` và mã thoát `1` đúng chủ đích | Đạt. `probe-module-failure-d3d12/gameplay.txt`. |
| V33-Q05 | LOC01 sau T09, K-B/K-D/K-F, đi bộ LOC02 ↔ LOC05 ↔ LOC07 trong R trước G02, cửa xả sân có vật cản vật lý và C12 giữ ranh giới nhận dạng | Module xác nhận phản hồi LOC01, NavMesh LOC02 đến AP06, stream LOC05 trước khi mở connector, cô lập S3/B1, điều kiện kích hoạt CH03 và C12. Input QA có di chuyển vật lý trên các thang sau G02. Còn thiếu đi bộ hai chiều qua cả LOC05/LOC07 trước G02, xác nhận cửa xả trực tiếp và ba cổng ngoài trên Player. |
| Hiệu năng | 3 lượt độc lập, 1920×1080 Medium, D3D12, VSync 1, màn hình 60 Hz và trace PresentMon | Ba lần PresentMon dừng trước khi mở ETW với `Access denied`. Phiên chạy có token mức Low và nhóm Performance Log Users đã được bật. Ba lượt chẩn đoán bằng Unity hoàn tất ở 120 Hz. Cần trace tại 60 Hz theo cấu hình nghiệm thu. |

### Chẩn đoán hiệu năng 120 Hz

Ba lượt `unity-diagnostic-20261125-021634` dùng cùng binary, Intel Core Ultra 7 155H và Intel Arc, `1920×1080`, Medium render scale `0,88`, D3D12, VSync 1, dual GI, tuyến mở rộng CH01–CH04. Unity báo tất cả frame đo có focus. Mã thoát `0` và `passed=True` xác nhận tuyến benchmark hoàn tất. Bảng dùng counter Unity. Dữ liệu gốc và thống kê đầy đủ ở `unity-timing-summary.json`.

| Lượt | Frame tuyến | Wall P95 (ms) | Main P95 (ms) | GPU P95 (ms) | Wall max (ms) | Working set đỉnh (MiB) |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 9.463 | 16,669 | 6,29 | 17,53 | 466,56 | 1.852,91 |
| 2 | 9.576 | 16,669 | 6,62 | 17,14 | 224,94 | 1.839,61 |
| 3 | 9.824 | 16,669 | 6,04 | 17,30 | 224,94 | 1.818,07 |

`an-vignette-R` và `room-return` chiếm 46,8% mẫu GPU vượt 16,67 ms. Cả ba lượt có Wall max ở `approach-R`. Số liệu Unity ở 120 Hz dùng để khoanh vùng chi phí. Cổng hiệu năng hiển thị 60 Hz còn mở.

Lượt A/B `unity-ablation-shadows-20261125-100526` hoàn tất trên cùng EXE và assembly, chỉ tắt bóng bằng cờ chẩn đoán sẵn có. GPU P95 xuống **15,70 ms**. P95 riêng `an-vignette-R` còn 16,58 ms và `room-return` còn 19,48 ms. Wall max lên 724,80 ms. Bóng góp phần vào chi phí GPU; khựng khung dài và đoạn trở về Phòng 101 vẫn cần đối chiếu bằng capture ở cấu hình chuẩn. Preset Medium khi phát hành giữ nguyên. Xem `unity-shadow-ablation-summary.json` và `benchmark-1/frames.csv`.

Để đo nghiệm thu, giữ desktop mở khóa, chọn 60 Hz và chạy từ phiên PowerShell Windows có quyền mở ETW. Script dừng nếu nhịp xuất hình đo được nằm ngoài dải 60 Hz. Cửa sổ Player cần foreground trong khi đo.

```powershell
$root = 'C:\CTCC-Unity-Workspace\output\m2-final'
$run = Join-Path $root 'host-verify-20261125-005837-20f3c1'
& (Join-Path $root 'measure-phase-b-windows.ps1') -RunDirectory $run -BuildIdentity (Join-Path $run 'qa-authorized-20261125-020832\build-identity.txt')
```

### Ca đi bộ S1

`start-phase-b-s1-walk.ps1` kiểm hash Player và bản sao save từ Player Input QA. Save Schema v7 có checksum hợp lệ, lớp R, CH02, `CH01-CP04` → AP02, chỉ có E10a và chưa có G02 hoặc sự kiện CH03. Mỗi lượt có thư mục save riêng. Chọn **Tiếp tục từ tự lưu** rồi đi từ nhánh S1 phía tây lễ tân qua Khu B và Kho, đi ngược về LOC02. Ghi đoạn vào biên tải vùng, tín hiệu/nhật ký tiến trình và va chạm lối xả nếu xác định được đúng cửa từ chiếu nghỉ trệt. Tiếp tục tuyến CH02 qua LOC03 và LOC04, kiểm rằng vật chứng và sự kiện CH03 vẫn theo gate. `HospitalAuthoring.Expansion.cs` định nghĩa hai nhịp S1 nội bộ. Chưa định danh được đối tượng cửa xả sân riêng trong scene/mã dựng. Rào `STAIRWELL` phía đông lễ tân có vị trí khác với connector S1 phía tây.

```powershell
& 'C:\CTCC-Unity-Workspace\output\m2-final\start-phase-b-s1-walk.ps1'
```

**Technical Verified** · Windows x64, Module QA, Player Input QA, D3D12 normal/failure exit và phần tự động V33-Q05 theo log trên. **Human Review Pending** · Ca đi bộ S1, TASK010 và TASK026–028. **Accepted** · Phase B và M2 còn mở. Phase C chưa bắt đầu.

## Bản sửa V33-Q05 sau lượt Player đầu tiên

Checkpoint cục bộ `46e706c4785eda0315391739b22409264457730d` dựa trên `9c2bf95`. `HospitalAuthoring.Campus.cs` đặt rào tôn có collider, xích và khóa trong khẩu độ cổng K-B, K-D, K-F hiện có ở R. `HospitalGame.QA.cs` thêm kiểm vật cản và chuyển lớp trên ba cổng. `HospitalGame.InputQA.cs` thêm ca điều khiển nhân vật đi LOC02 ↔ LOC05 ↔ LOC07 trong R trước G02, xác nhận CH03 và B1 chưa mở rồi tiếp tục tuyến CH02. Core 01–07, package, phiên bản Unity và Save Schema v7 giữ nguyên.

Runtime và Editor đều biên dịch bằng Roslyn của Unity 6000.3.28f1 với mã `0`. Editor còn cảnh báo CS0618 có sẵn trong `HospitalAuthoring.Lighting.cs`. Log ở `output/m2-final/phase-b-remaining-compile/`. Thử tái sinh scene tại `output/m2-final/phase-b-v33-local-build/` gặp `LICENSING_CLIENT_UNAVAILABLE`, log ghi kết nối IPC bị từ chối và ba lần thử kết nối lại. Lượt thử được dừng khi chưa có `CTCC_SCENE`. Chưa có Player `46e706c`, nên kết quả PASS của `9c2bf95` chỉ áp dụng cho build cũ. PresentMon kiểm lại vẫn ghi `failed to start trace session: access denied`.

Cửa xả sân S1 còn một quyết định bố trí. Core 03 §8 và `DEC-V331-01` yêu cầu cửa riêng từ chiếu nghỉ trệt. Bản mặt bằng Campus tầng trệt và `HospitalAuthoring.Expansion.cs` chỉ xác lập S1 qua lễ tân, còn nhánh đi qua sảnh đang được kiểm bằng W-Q02. Có thể khảo sát đoạn infill mặt nam K-A ngay phía tây lễ tân, quanh `x=-2,05, z=-7,86`, cho cửa riêng. Cần duyệt vị trí trước khi thay mặt đứng hoặc thêm nhánh kết nối. Đây là vị trí đề xuất, chưa phải tọa độ đã chốt.

Tái sinh scene, build và QA trên Windows đã kích hoạt Unity. Dùng `-Measure` nếu phiên có quyền ETW và màn hình 60 Hz. Script tạo worktree mới từ `46e706c` để tránh lock của lượt thất bại.

```powershell
& 'C:\CTCC-Unity-Workspace\output\m2-final\verify-phase-b-v33-on-windows.ps1' -Measure
```

## Checkpoint sau khi phê duyệt cửa xả S1

`78ec6694fa5562dc12215faa7f65af0bf82fbcbc` (`checkpoint/m2-phase-b-v33`) chứa `46e706c` và các thay đổi cửa xả S1 đã duyệt. Tâm khẩu độ ở `(-2.05, 0, -7.86)`. Kiểm kích thước từ layout xác nhận vùng lọt lòng giữa hai thanh đứng `x=-2.66…-1.44`, đầu khung cao `2.17 m`, vế thang đầu sau hiệu chỉnh dốc `26.28°` và nền ngưỡng gặp nền sân tại `z=-8`. [Implementation Clarifications](design-lock-errata.md#implementation-clarifications--cửa-xả-sân-s1).

| Hạng mục | Expected | Actual |
| --- | --- | --- |
| Nguồn authoring và layout | Cửa, khung, nền, rào thanh lý và tầng trệt S1 tái sinh từ nguồn đã chốt | Đã tích hợp vào generator. C# Editor và Player biên dịch bằng Roslyn Unity 6000.3.28f1, mã `0`. Hash và log tách khỏi bằng chứng Player. |
| `V33-Q05` | R bị rào lưới chặn. M CH05 mở và có đường NavMesh trực tiếp. R đi hai chiều LOC02–LOC07 trước G02 | Đã bổ sung kiểm capsule, NavMesh, chuyển R/M, đi bộ bằng input và hai góc ảnh trong QA. Chưa có kết quả Player mới. |
| `W-Q02` / `WORLD-Q04` | Giữ đường qua lễ tân độc lập B1 và phân vùng theo XYZ | Bộ kiểm hiện có được chạy lại bởi runner. Chưa có verdict cho `78ec669`. |
| Build và benchmark | Windows x64, Module QA, Player Input QA, D3D12 failure probe và ba lượt PresentMon 60 Hz cùng revision | Preflight trả `LICENSING_CLIENT_UNAVAILABLE` (mã 6). Phiên này từ chối yêu cầu chạy ngoài sandbox. PresentMon đã trả `Access denied` ở bước mở ETW. Ảnh hình học, log Player và số liệu 60 Hz còn chờ. |

Runner cùng tên ở trên đã khóa revision `78ec669`, tái sinh và build từ cùng worktree, ghi hash EXE và xác nhận marker/capture trước khi benchmark. Chạy trong Windows đã có license Unity hoạt động, desktop 60 Hz và quyền ETW. Các kết quả Player của `9c2bf95` chỉ dùng làm baseline lịch sử. Phase B chưa nghiệm thu và Phase C vẫn chờ.

## Lượt Windows sau khi license hoạt động

`v33-host-20261125-103747` chạy từ phiên Windows trực tiếp. Unity xác nhận entitlement, ghi `CTCC_GREYBOX_PASS`, bake R/B 369 probe mỗi lớp, bake occlusion trên 9 scene và dựng Player `78ec669` với mã `0`. Build identity ghi SHA-256 assembly `8e1ac789c0780aba1afa0ff34990e391b440053fc67e8e53c611b2ae7ac026f9` và hash scene LOC02 `7109e4762e26aa24daaca5530d04648143e71680f1951481f5e51311f2c8b698`.

- **Module QA PASS.** `module/gameplay.txt` có V33-Q05 cho ba cổng ngoài, collider R/capsule và NavMesh trong M CH05, S1 trước G02, W-Q02 và WORLD-Q04. Hai ảnh thật `module/s1-discharge-landing-R.png` và `module/s1-discharge-courtyard-R.png` được lưu, cho thấy khung và rào lưới trong điều kiện ánh sáng R.
- **Player Input QA FAIL.** Sau các kiểm thử F101 đầu tiên, `WalkWithInput` đi thẳng từ AP02 tới `(0.30, 0, -4.00)`, dừng ở `(2.47, 0.04, -4.00)` do `Counter apron`. Ca chưa tới cầu thang. Failure probe và benchmark của lượt này chưa chạy.
- Checkpoint cục bộ `71d2c4a8bc920b41b63bebc04d7781866c741339` sửa đường đi của QA qua đầu nam quầy theo cả hai chiều. Diff từ `78ec669` chỉ có `HospitalGame.InputQA.cs`. Runtime Roslyn biên dịch thành công. So khớp 9/9 hash scene đã bake và xác nhận worktree không còn `UnityLockfile`. Runner hỗ trợ dựng lại bằng cache đó và lưu bộ QA/benchmark mới ở thư mục riêng.

```powershell
& 'C:\CTCC-Unity-Workspace\output\m2-final\verify-phase-b-v33-on-windows.ps1' -ReuseGenerated -Measure
```

## Phân tích Player Input QA và retest `1de08a5`

**Phase B · Player Verification Failed.** Bằng chứng của `78ec669` ở `output/m2-final/v33-host-20261125-103747`. Module QA đã đạt. Player Input QA trả `RESULT=FAIL` trước khi tới S1. Chưa có kết quả failure probe và benchmark của lượt này.

| Hạng mục | Quan sát và nguyên nhân | Thay đổi / kiểm lại |
| --- | --- | --- |
| LOC02 → S1 | `WalkWithInput` hướng controller thẳng tới mỗi waypoint. Scene `LOC02_Reception` có `Counter apron` ở `(1.45, .55, -3.24)` kích thước `(1.25, .98, 3.80)`, vật cản trên solid layer. Cạnh đông `x=2.075` cộng bán kính controller `.35` dự đoán dừng gần `x=2.425`, khớp vị trí quan sát `x=2.47`. Module QA đã tìm được đường NavMesh qua lễ tân. | `71d2c4a` sửa tuyến Input QA vòng đầu nam quầy tại `z=-6.15`, đi dọc phía tây tại `x=-0.10` và về theo lối đó. Đối chiếu kích thước footprint của quầy cho dư `.36 m` theo z và `.325 m` theo x sau bán kính và sai số chấp nhận waypoint. Chưa kiểm controller trên build mới. |
| LOC05 load/unload | Log có một cặp load/unload trước exception và thiếu tọa độ/timestamp cho từng sự kiện. Điểm kẹt `x=2.47` ở ngoài vùng tiếp cận S1 có biên đông `x=2.25`. Trên vế thang, khi vượt biên tây `x=-5.25`, cao độ chân theo layout khoảng `1.16 m`, nằm trong vùng giữ tầng trên `y>.30 m`. Mã cũ có thể yêu cầu unload sau khi đã chờ vùng nền và chỉ cập nhật connector sau khi dỡ xong. | `1de08a5` ưu tiên quyết định LOC05 trước vùng chỉ để hiển thị, giữ hysteresis 20 giây, đóng collider connector trước khi bắt đầu unload và tuần tự hóa load/unload cùng scene. Module QA kiểm giữ vùng ở biên vế thang, hysteresis, khóa trước unload và mở lại sau reload. Player Input QA kiểm LOC05 tại các waypoint lên/xuống. |
| `0x80070020` trên `auto.json.lock` | Đây là lỗi được tiêm trong ca F101 Affirmed: Input QA mở `.lock` với `FileShare.None` trong `using`, bấm đóng dấu và đợi lỗi. Log ghi PASS cho rollback, sau đó PASS nhánh Affirmed. `AtomicSaves` giữ khóa theo slot qua lúc ghi, flush, kiểm checksum và publish; lỗi chia sẻ được thử lại sau 5/10/20/40 ms. `RealityTransaction` ngăn ghi giao dịch chồng bằng `savingAsync` và chỉ công bố snapshot sau khi lưu thành công. | Giữ nguyên giao thức save. Ca mới so bản lưu chính byte-for-byte khi bị giữ khóa, kiểm `.tmp` và đọc lại một revision F101 hợp lệ sau khi handle đã dispose và thao tác được thử lại. |

Checkpoint `1de08a51c6ef20cb275fcdb37656c05e7979c70e` biên dịch runtime bằng Roslyn Unity `6000.3.28f1` với mã `0`. So khớp 9/9 scene bake. Diff so với `78ec669` gồm bốn tệp C# runtime/QA. Runner yêu cầu build identity mới và kiểm đủ marker streaming, đi bộ và save retry trước benchmark. Phase C vẫn chờ kết quả retest.

## Lần kiểm thứ hai và retest thời gian di chuyển `3712d89`

Bằng chứng `output/m2-final/v33-host-20261125-112230`. Windows x64 `1de08a5` có assembly SHA-256 `38114805f4a7ea7f99d50e02f8d741251f0ef608ca9f402634ef6c67de24183b`. Module QA ghi `RESULT=PASS`, gồm `V33-Q05 S1 streaming PASS`. Player Input QA ghi `REBIRTH II SAVE FAILURE PASS` và `F101 SLOT RETRY PASS` trước `RESULT=FAIL`. Hai marker xác nhận snapshot được bảo toàn khi tranh khóa và một quyết định F101 được ghi bền vững sau khi nhả khóa. Benchmark chưa chạy.

| Hạng mục | Đối chiếu | Thay đổi và kiểm lại |
| --- | --- | --- |
| `WalkWithInput` | Đoạn từ `(-11.90, 4, -4)` đến `(-12.30, 4, 37.05)` dài `41.05 m`. Đi bộ `2.2 m/s` cần tối thiểu `18.66 s`, trong khi coroutine cũ dừng sau `16 s` unscaled. Trần lý tưởng `35.20 m` còn thiếu `5.85 m`, khớp khoảng `6 m` trong log. Tăng tốc và clamp `deltaTime` ở `.045 s` có thể làm thời gian thực dài thêm. | Ngân sách theo khoảng cách và `deltaTime` mà controller dùng. Giới hạn thời gian thực độc lập từ 10 đến 90 giây. Thoát sớm nếu không có tiến triển trong 2 giây di chuyển tích lũy. Giữ bán kính đến đích `.20 m` và sai số cao độ `.32 m`. Tốc độ, gia tốc, collision và cách nhận phím gameplay giữ nguyên. |
| Hành lang tầng 2 và tuyến còn lại | Scene bake có `BoxCollider` bật trên `S1 · West gallery`, tâm `(-12.35, 3.90, 16.425)`, kích thước `(2.70, .20, 43.55)`, mặt sàn `y=4`, kéo dài `z=-5.35…38.20`. Điểm dừng `(-12.25, 4.04, 31.05)` nằm trong footprint. Log ghi LOC05 đã tải lại trước khi lỗi và không ghi lần dỡ tiếp theo. Module QA đạt đường NavMesh AP02→AP06. | Thêm waypoint tại `z=9.5` và `z=23`, giữ tuyến đi và quay về qua cùng các cầu nối, cửa LOC05/LOC07 và vế thang. Rà soát chiều dài ngang các đoạn trong tuyến hai chiều. Đoạn dài nhất còn `14.05 m`. Giữ nguyên scene và hình học. |
| Chẩn đoán thất bại | Log cũ chỉ ghi điểm cuối, collider hướng tới, controls và modal. | Log mới ghi điểm đầu/đích/cuối, khoảng cách, lý do dừng, thời gian thực và thời gian di chuyển/giới hạn, delta gốc/hữu hiệu, velocity, grounded, collision flags, sàn, vật cản, zone, trạng thái streaming, phím, focus và các cờ khóa điều khiển. |
| Thứ tự retest | Tốn hai lượt full QA mới tới ca S1. | Runner dựng `3712d89604ba4b456536241408530474e6879c14`, chạy targeted `player-input-s1` trước. Ca đi từ CH01 nhánh Unverified qua luồng bình thường, hoàn tất S1 hai chiều và kiểm cửa xả. Runner tiếp tục Module QA, full Player Input QA, failure probe và benchmark nếu targeted PASS. |

Runtime biên dịch Roslyn với mã `0`. So khớp `9/9` hash scene bake với build `1de08a5`. Diff chỉ có `FirstPerson.cs`, `HospitalGame.InputQA.cs` và `tools/verify_windows.ps1`; không có thay đổi authoring, scene, Core, Save Schema hoặc Unity. Thử chạy mới từ phiên công cụ vẫn trả `LICENSING_CLIENT_UNAVAILABLE`. **Phase B vẫn Player Verification Failed. Phase C chưa mở.**

Dung lượng trống C: lúc rà soát khoảng `0.86 GiB`, dưới dung lượng Player cũ. Đã nén NTFS một phần cache Unity ở `checkpoint-repo/Prototype/Library`, ngoài checkout dùng cho retest. Dung lượng trống sau thao tác khoảng `1.75 GiB`. Source và thư mục bằng chứng được giữ lại.

## Hoàn tất chuỗi QA `3712d89`, dọn ổ C và tối ưu GPU lớp R `fb2e96d`

- **Build `3712d89` (`output/m2-final/v33-host-20261125-125209`):** Đã xác nhận Unity Licensing hoạt động (`Unity Personal Assigned`), quyền ETW độc lập qua `Performance Log Users` (`PresentMon-2.6.0-x64.exe` exit `0`), khớp toàn vẹn EXE/assembly/9 scene bake. Targeted S1 QA (`V33-Q05 INPUT S1 PASS`), Full Module QA (`27/27`), Full Player Input QA (`19/19`, `483.1 s`) và D3D12 failure probe đều đạt `RESULT=PASS`/`FAIL` đúng chủ đích. Tuy nhiên benchmark PresentMon 60 Hz sạch (`presentmon-20261125-140600`, sau khi đóng các tab nặng) vẫn ghi `Unity GPU P95 = 18,49–18,99 ms` và `PresentMon GPUTime P95 = 18,89–19,37 ms` (677/6.538 khung GPU > 16,67 ms ở lượt 1) do đèn `CAMPUS_EXT` bán kính `20–44 m` xuyên tường đè lên cluster Forward+ trong `LOC03` và shadow cascade `2 × 38 m`.
- **Dọn ổ C:** Đã dọn `%TEMP%` cũ hơn 30 phút, cache `npm`/`pip` và thư mục build dở bị treo `v33-host-20261125-114542`, nâng dung lượng trống từ `2,06 GB` lên `20,16 GB` (`19,19 GB` còn trống sau khi build và QA `fb2e96d`).
- **Tối ưu GPU lớp R `fb2e96d43cbc307d173c0c93bc0af3e6195aab2b` (`output/m2-final/v33-host-20261125-143228`):** Thêm `HospitalScene.Performance.cs` (rút gọn bán kính và tắt theo vùng các đèn `CAMPUS_EXT`, shadow caster ngoại thất và hạt sân nam bị tường che khuất khi ở trong nhà; giới hạn `DecalProjector.drawDistance` nội thất xuống `16 m`; điều chỉnh động `shadowDistance` `14–24 m` theo vùng), khôi phục `PC_RPAsset.asset` (`Medium`) về `1` cascade `24 m` như baseline M2C1 và `m_SoftShadowQuality = 1` (4-tap PCF mềm), đồng bộ `HospitalAuthoring.Lighting.cs`. Giữ nguyên Core v3.3.1, Save Schema v7, Unity `6000.3.28f1`, `config.toml`, chính sách bảo mật, preset Medium `renderScale = 0,88`, SSAO, DBuffer decals, dual GI (`R=369/B=369`), thông số đèn pin QA và `9/9` hash scene đã bake.

| Hạng mục (`fb2e96d`, `v33-host-20261125-143228`) | Expected | Actual và bằng chứng |
| --- | --- | --- |
| Build identity & ảnh trực quan | Đúng revision `fb2e96d`, `development=False`, khớp `9/9` scene bake, không suy giảm hình ảnh R/B | Đạt. Assembly SHA-256 `fbc7b787947bb74bf9937be0e64eb5e2d018026d77cd5654c915bd384acaf686`, EXE SHA-256 `fbd073ca59c4918edbc6704f5393cf59021173af11714c00db4212c50117492b`. Đã đối chiếu `room101-R.png`, `corridor-R.png`, `loc01-courtyard-R.png`. |
| Targeted S1 QA (`player-input-s1`) | `V33-Q05 INPUT S1 PASS` và `RESULT=PASS` | Đạt. [player-input-s1-gameplay.txt](evidence/phase-b-player-qa/player-input-s1-gameplay.txt). |
| Full Module QA (`module`) | `RESULT=PASS` đủ 27 ca + 4 marker `V33-Q05`/`WORLD` + 4 ảnh xác nhận | Đạt. [module-gameplay.txt](evidence/phase-b-player-qa/module-gameplay.txt). |
| Full Player Input QA (`player-input-full`) | `exit_code=0`, `timeout=False`, đủ 19 ca gồm `F101 SLOT RETRY`, `V33-Q05 INPUT S1`, 3 nhánh F101, CH01-R full route, CH03–04, stress | Đạt (`438,2 s`, `exit_code=0`). [player-input-full-gameplay.txt](evidence/phase-b-player-qa/player-input-full-gameplay.txt). |
| D3D12 Failure Probe (`probe-module-failure-d3d12`) | Exit `1` và `RESULT=FAIL` đúng chủ đích | Đạt. [probe-module-failure-gameplay.txt](evidence/phase-b-player-qa/probe-module-failure-gameplay.txt). |
| Benchmark PresentMon 60 Hz (`presentmon-20261125-143830`) | 3 lượt độc lập, 100% foreground, 1920×1080 Medium `0,88`, D3D12, VSync 1, 60 Hz, GPU & nhịp hiển thị ≤ 16,67 ms | Đạt (`p95_budget_met: true`). 3/3 lượt: `Unity GPU P95 = 13,23 / 12,47 / 12,66 ms` (`0/6.661` khung GPU > 16,67 ms ở lượt 1), `PresentMon GPUTime P95 = 14,01 / 13,14 / 13,54 ms` (`P99 = 15,39–15,88 ms`), `Unity Wall P95 = 16,68 ms`, `DisplayedTime P95 = 16,74–16,75 ms`, `Working Set Peak = 1.750,8–1.765,6 MiB`. [performance.json](evidence/phase-b-benchmark/performance.json). |

**Technical Verified** · Toàn bộ build `fb2e96d`, Targeted S1 QA, Full Module QA, Full Player Input QA, D3D12 failure probe và 3 lượt PresentMon 60 Hz đạt trên cùng binary. **Accepted (Phase B)** · Người dùng đã chốt chuyển sang Phase C–F ngày 25/11/2026; **Phase B đóng**. **Human Review Pending (M2)** · `TASK010` và `TASK026–028` giữ mở cho cổng M2.
