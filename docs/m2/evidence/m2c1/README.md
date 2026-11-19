# Bằng chứng ứng viên M2-C.1

[Hồ sơ theo CHAR-Q01–09](../../m2c1-acceptance.md) · [Baseline Windows](baseline-build.txt) · [Ứng viên Windows](candidate-build.txt)

## Hình và diễn xuất

| Nhân vật | Blender trung tính | Blender đèn cứng | Windows trong cảnh |
| --- | --- | --- | --- |
| Lan | [Gốc](blender-lan-baseline-neutral.jpg) · [Ứng viên](blender-lan-candidate-neutral.jpg) | [Gốc](blender-lan-baseline-hard-key.jpg) · [Ứng viên](blender-lan-candidate-hard-key.jpg) | [Gốc B](unity-lan-b-baseline.jpg) · [Ứng viên B](unity-lan-b-candidate.jpg) · [Trao F101](unity-lan-handover.jpg) |
| Bé An | [Gốc](blender-anapparition-baseline-neutral.jpg) · [Ứng viên](blender-anapparition-candidate-neutral.jpg) | [Gốc](blender-anapparition-baseline-hard-key.jpg) · [Ứng viên](blender-anapparition-candidate-hard-key.jpg) | [Gốc R](unity-an-r-baseline.jpg) · [Ứng viên R](unity-an-r-candidate.jpg) |
| Kẻ Giữ Cửa | [Gốc](blender-gatekeeper-baseline-neutral.jpg) · [Ứng viên](blender-gatekeeper-candidate-neutral.jpg) | [Gốc](blender-gatekeeper-baseline-hard-key.jpg) · [Ứng viên](blender-gatekeeper-candidate-hard-key.jpg) | [Gốc R](unity-gatekeeper-r-baseline.jpg) · [Ứng viên R](unity-gatekeeper-r-candidate.jpg) |

Ảnh chân dung Blender dùng cùng camera và setup đèn theo từng người. Ảnh Windows dùng camera QA cùng cảnh. Ảnh ép LOD dùng cùng khoảng cách và giây speech cố định để thấy mặt, răng, mắt và thẻ ở cự ly gần. [LOD0](unity-lan-lod0.jpg) · [LOD1](unity-lan-lod1.jpg) · [LOD2](unity-lan-lod2.jpg)

| Nhân vật | Turntable Blender | Lưới và biểu cảm |
| --- | --- | --- |
| Lan | [Video](blender-lan-turntable.mp4) | [Wireframe](blender-lan-wireframe.jpg) · [Biểu cảm](blender-lan-expressions.jpg) |
| Bé An | [Video](blender-anapparition-turntable.mp4) | [Wireframe](blender-anapparition-wireframe.jpg) · [Biểu cảm](blender-anapparition-expressions.jpg) |
| Kẻ Giữ Cửa | [Video](blender-gatekeeper-turntable.mp4) | [Wireframe](blender-gatekeeper-wireframe.jpg) · [Biểu cảm](blender-gatekeeper-expressions.jpg) |

Video 19 giây lấy 570 khung render từ ứng viên Windows. Các đoạn được dựng theo camera QA và ghép WAV tạm theo cue. Clip bước chân phát ở một phần ba tốc độ nguồn cho mục đích quan sát. [Video có âm thanh](unity-staged-performance.mp4) · [Cue âm thanh](unity-staged-cues.json) · [Lan bước ra quầy](unity-lan-stepout-third-speed.mp4) · [Kẻ Giữ Cửa di chuyển](unity-gatekeeper-walk-third-speed.mp4)

## Kiểm thử

- Windows Release D3D12 theo preset. [Low](module-low.txt) · [Medium](module-medium.txt) · [High](module-high.txt)
- Điều khiển thật. [Baseline](baseline-input.txt) · [Ứng viên lần đầu](input-first-attempt.txt) · [Chạy lại](input-retry.txt)
- Bốn probe cố ý làm hỏng phải thoát mã 1 với thông báo đúng. [Thiếu mắt](fault-mesh.txt) · [Sai timestamp](fault-track.txt) · [Bỏ dừng thoại](fault-interruption.txt) · [Kết thúc cameo sớm](fault-escape.txt)
- Đối chiếu cấu trúc của nguồn và FBX. [Lan](candidate-Lan-blend.json) · [Bé An](candidate-AnApparition-blend.json) · [Kẻ Giữ Cửa](candidate-Gatekeeper-blend.json) · [WAV và track](voice-wave-validation.json)
- Xuất lại đủ ba FBX bằng Blender ở factory startup. Kết quả kiểm khi nhập trở lại khớp FBX Unity trên cấu trúc, trọng số, shape và hoạt ảnh. [Log đối chiếu](portable-export-check.txt)
- Kiểm năm shader đã nhập. Có danh sách pass và kết quả trình biên dịch. Mã tương thích SRP Batcher ở phiên batch là `Not initialized`. Hai lần mở Frame Debugger dừng ở bước khởi động Unity Editor nên chi phí GPU theo pass còn mở. [Shader import](shader-import.txt)
- Bước chân trong Blender. Sai lệch dọc trục tiến dựa trên xương ngón chân gần sàn. [Lan M2-C](baseline-Lan-blend.json) · [Lan M2-C.1](lan-stride-candidate.json) · [Kẻ Giữ Cửa M2-C](baseline-Gatekeeper-blend.json) · [Kẻ Giữ Cửa M2-C.1](gatekeeper-stride-candidate.json)

## Đo và nhận dạng tệp

- 12 lượt tuyến chính hợp lệ, ba lượt mỗi build và chế độ. Ba lượt stress riêng trên ứng viên. Các lượt bị mất focus và số mẫu hợp lệ có trong dữ liệu. [Số liệu theo lượt](performance.json) · [Unity và PresentMon trace](windows-frame-traces.zip)
- Dấu vân tay của gói Windows và bộ bằng chứng. [Gói Windows](windows-package-sha256.txt) · [Bộ bằng chứng](SHA256SUMS.txt)
