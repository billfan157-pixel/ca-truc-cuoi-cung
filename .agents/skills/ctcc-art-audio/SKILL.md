---
name: ctcc-art-audio
description: >-
  Chuẩn bị reference và sản xuất art/audio cho Ca trực cuối cùng. Dùng cho TASK023, asset kit CTCC, môi trường và prop bệnh viện, ánh sáng R/B/M, rig, SFX, VO, nhạc, caption, tích hợp asset và kiểm nguồn hoặc license.
---

# Art, audio và quyền sử dụng

## Nguồn cho task

- Dùng §1, §4–6 cho mốc thời gian, vật liệu và mức độ xác thực V/I/F/U. Đọc tiêu chí asset-ready, visual language, ENV/CHR/PROP kits, cue, mixer và naming. [Core 03](../../../docs/03_World_Architecture_Bible.docx), [Core 05](../../../docs/05_Art_Audio_Bible.docx)
- Tra `07/Assets`, `Research`, `Dialogue`, `Events` và QA liên quan theo ID. Đọc beat/layer nơi prop hoặc cue xuất hiện. [Core 04](../../../docs/04_Level_Chapter_Design.docx)

## Thực hiện

1. Xác định task, asset/cue ID, CH/LOC/beat/layer, trường hợp dùng, kích thước/pivot, kiểu giao tạm hoặc final và QA từ nguồn. Với `TASK023`, tạo register có nguồn, thời kỳ, phạm vi quyền và bộ reference 2D LOC03. Ghi `U` cho hạng mục còn thiếu chứng cứ.
2. Dùng bộ kit modular theo Core 05. Ưu tiên kiểm LOC03 và vị trí clue trên greybox. Giữ grid/pivot/landmark R/B chung, dùng M theo vignette; dùng ánh sáng, vật liệu, chữ và âm thanh để phân biệt. Render đủ chữ của F101, E01 và thẻ chứng cứ ở font scale hỗ trợ.
3. Gắn prop, bản đọc và phụ đề với ID vật chứng hoặc thoại. E06 là prop giấy có inspect và TTS tùy chọn, E06a là cassette, E04 là hồ sơ tiếp nhận, E11 có thẻ chuyển 101/4→C01. Gói Bảy có nhãn nguồn và biến thể vật liệu phù hợp nơi bảo quản. Dùng nội dung hư cấu cùng chính tả từ `07/Dialogue` và Core 02/04.
4. Gắn cue với trigger từ Events và lớp phát. Chìa cho CHAR005, xe thuốc cho CHAR006, dép cho An. Mọi cue cần để ra quyết định có caption hướng và dấu hiệu thị giác hoặc văn bản. Bản thu D16b, subtitle và transcript dùng cùng guard CH04 B06 + H05 + PT02; kiểm cả chế độ mute và skip. Nhạc motif Lan dùng biến thể đúng CH/ending từ Core 05.
5. Ghi creator, URL/ngày hoặc nguồn tự tạo, phiên bản giấy phép, phạm vi sử dụng thương mại/phân phối và attribution. Dùng tài liệu có quyền rõ trong build. Giữ ảnh chỉ được dùng làm reference ở đúng phạm vi. Dùng tên, logo, hồ sơ và nhân vật hư cấu của An Sinh.

Với CH01, tra `CH01 / Walkthrough B01–B08` trong [Core 04](../../../docs/04_Level_Chapter_Design.docx), `CH01-WT-FOLEY-01`, `07/Voice Bible & Barks` và [Core 05](../../../docs/05_Art_Audio_Bible.docx). Áp dụng `DEC-VO-01` (82/143 dòng thu VO + 14 `ambientBarks`, 0 VO độc thoại khám phá của Minh; 28 dòng `INNER_SUB_SILENT` chữ nghiêng `#F4F1E8` + scrim mềm WCAG AA `≥ 4,5:1` và `ThoughtReadDurationScale`). Tiếng bút gắn B03, quầy B06 vắng tiếng bút, cue đồng hồ 03:17 chỉ ở B trước T08. Hậu quả F101 sau commit tách ngân sách ambience. Đỉnh B05/B07 theo hành động, giữ đường hiểu khi mute và giảm chuyển động.

Phân biệt kit/VO tạm của M2 với art lock và final asset. Tra `TASK023` và review `TASK026–028` trước art lock. Khi bàn giao, ghi đường dẫn resource, ID, phiên bản/license, thông số scale/pivot/collider/LOD nếu áp dụng, ảnh hoặc clip trong layer cần thiết, và AR/AU/W-Q tương ứng. Cập nhật Status/License của đúng dòng Assets khi có bằng chứng và nằm trong phạm vi.
