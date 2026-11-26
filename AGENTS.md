# Vận hành dự án Ca trực cuối cùng

Áp dụng cho toàn workspace. Bắt đầu ở thư mục chứa `AGENTS.md` và `docs/`. “Ca trực cuối cùng” là game FPS kinh dị điều tra độc lập trong bệnh viện An Sinh hư cấu. Vòng chơi chính đi từ quan sát và đối chiếu vật chứng đến quyết định về hồ sơ, rồi thấy hậu quả trong không gian. Thiết kế giữ nhân tính của bệnh nhân, quyền để ngỏ và khả năng sửa sai của người chơi.

## Chọn skill

| Đầu việc chính | Skill |
| --- | --- |
| Yêu cầu build chung, chọn task, M0–M5, engine spike, đồng bộ tài liệu hoặc chốt milestone | [ctcc-production](.agents/skills/ctcc-production/SKILL.md) |
| Controller, tương tác, evidence, F101, puzzle/gate, R/B/M, save, AI, journal và ending logic | [ctcc-gameplay](.agents/skills/ctcc-gameplay/SKILL.md) |
| Bản đồ, mặt cắt, LOC, greybox, beat, checkpoint, route và encounter | [ctcc-level](.agents/skills/ctcc-level/SKILL.md) |
| Canon, timeline, reveal, thoại, prop text, vignette và nội dung ending | [ctcc-narrative](.agents/skills/ctcc-narrative/SKILL.md) |
| Reference, vật liệu, mesh, model, ánh sáng, audio, VO, caption và quyền sử dụng | [ctcc-art-audio](.agents/skills/ctcc-art-audio/SKILL.md) |
| Test, regression, playtest, nghiệm thu hoặc xác minh một claim trên build | [ctcc-qa](.agents/skills/ctcc-qa/SKILL.md) |

Đọc skill sở hữu đầu việc. Đọc thêm skill liên quan khi thay đổi chạm sang hợp đồng khác, chẳng hạn sửa cue hé lộ phải tra narrative, gameplay và các ca QA tương ứng. Mở phần cần thiết trong [hướng dẫn vận hành](docs/agent/README.md) khi cần chọn nguồn, xử lý mâu thuẫn hoặc lập bằng chứng bàn giao.

## Nguồn và cách làm

- Dùng [Core v3.6.2 (Documentation Consolidation)](docs/agent/README.md#bảy-nguồn-sở-hữu) làm bộ nguồn chính thức duy nhất (Single Source of Truth) tại `docs/01…07` kèm `docs/dialogue_silent_exploration_runtime.json`. Core 01 sở hữu mục tiêu gameplay và UX, Core 02 sở hữu canon và WR, Core 03 sở hữu không gian vật lý, Core 04 sở hữu luồng chương và walkthrough CH01 B01–B08 duy nhất, Core 05 sở hữu art/audio, Core 06 sở hữu contract kỹ thuật, Core 07 sở hữu dữ liệu và trạng thái sản xuất. Tra câu thoại ở `07/Dialogue` (10+ cột kèm `Presentation Mode`), quy chuẩn xưng hô/bark ở `07/Voice Bible & Barks`, văn bản vật chứng ở `07/Evidence Specs` và bối cảnh phát ở Core 04.
- Tra `07/Decisions` và `07/Changelog` theo ID. Áp dụng `DEC-V31`, `DEC-V32`, `DEC-V33`, `DEC-V331`, `DEC-NPR01`, `DEC-VO-01`, `DEC-V34-01–03`, `DEC-V35-01`, `DEC-V36-CH01-01` và `DEC-V361-CH01-01` tại các điểm đã sửa. Xem [cách áp dụng quyết định](docs/agent/README.md#giải-quyết-chênh-lệch-giữa-nguồn) và [ghi chú phát hành](docs/RELEASE-NOTES.md). Xác minh tiến độ theo build và ca QA tương ứng.
- Với CH01 B01–B08, **Core 04 (`docs/04_Level_Chapter_Design.docx` — CH01 / Walkthrough B01–B08)** là nguồn walkthrough duy nhất (bản walkthrough rời cũ đã chuyển vào `docs/archive/ch01-history/`). Áp dụng hộp thư ngoài cổng `T01`/`T02`, `Pocket Note` CH01 chỉ đọc lại, `DEC-VO-01` (0 VO độc thoại khám phá tự do của Minh, 82/143 dòng VO, 28 `INNER_SUB_SILENT`, 12 `CUT_TO_JOURNAL`) và phản hồi im lặng tại `LOC01` sau `T09`. Giữ 143 stable ID, schema save v7 và các gate đã khóa theo Core v3.6.2.
- Khi bắt đầu việc sản xuất, đọc `07/Roadmap` và `07/Backlog` tại thời điểm làm. Có thể dùng `python docs/agent/project_docs.py status` và `python docs/agent/project_docs.py task TASK029`. Khi chưa có project engine, lập đầu ra theo task tiền sản xuất hoặc spike có phạm vi đúng với Roadmap.
- Dẫn theo chuỗi task → mục nguồn và ID liên quan → thay đổi → ca QA → kết quả và bằng chứng theo build. Với việc sửa nhỏ, dùng các mắt xích cần thiết. Giữ các ID đã được dùng trong scene, dữ liệu và save.
- Khi phát hiện skill hoặc tài liệu liên quan đến task có chỉ dẫn mâu thuẫn hoặc đã lỗi thời, dừng thực hiện task và báo người dùng. Nêu vị trí nguồn, nội dung bị lệch, căn cứ đối chiếu, ảnh hưởng đến task và phương án xử lý. Chỉ tiếp tục sau khi người dùng quyết định nguồn áp dụng hoặc cách xử lý.
- Đưa thay đổi thiết kế đang đề xuất vào luồng `07/Decisions`. Sau khi chốt, đồng bộ các nguồn bị tác động và ghi `07/Changelog`.
- Tự chạy các kiểm tra có thể thực hiện trong môi trường hiện tại. Trạng thái hoàn thành bám điều kiện Pass của từng task. Ghi riêng kết quả rà tài liệu, mô phỏng, chạy build, playtest và review chuyên gia theo bằng chứng thực tế.
