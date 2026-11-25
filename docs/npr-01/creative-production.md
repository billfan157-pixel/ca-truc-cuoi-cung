# Dàn dựng và kế hoạch art/audio

Dự thảo cho NPR-01-05/06/11. Dùng [Core 04](../04_Level_Chapter_Design.docx) để định beat, [Core 05](../05_Art_Audio_Bible.docx) để định palette, kit và mixer, `07/Dialogue` và `07/Evidence Specs` để xuất copy/cue. Tái sử dụng [register và pipeline nhân vật M2-C](../m2/m2c-character-audit-and-pipeline.md), [bằng chứng M2-C.1](../m2/evidence/m2c1/README.md), [mẫu Lan/CH03 và hướng art M3.0](../m3/m30-foundation-and-art-lock.md), [bộ ảnh campus](../campus/an-sinh-full-campus-expansion-report.md) và [pipeline âm thanh CH01–02](../m2/audio-research-and-pipeline.md).

## Nhân vật và trình bày

| Beat | Hành động và nhịp | Điều người chơi có thể hiểu | Fallback |
| --- | --- | --- | --- |
| CH04 B06, `CH01-D16b` / `CH04-D06` | Giữ Lan nói từ góc nhìn của cô sau H05/PT02. Nếu cần một nhịp ấm trước hồi đáp cuối, thử khoảng lặng hoặc một cử chỉ chờ Minh đọc hết. Phiên thử chỉ sau khi xác định phạm vi M3.2. | Cô ở lại với bệnh nhân và Minh từng lắng nghe cô. Chi tiết 07:40 và hồ sơ năm 2012 vẫn ở các beat sau. | Không bắt tương tác, dùng transcript đúng điều kiện và làm mới sau load. |
| CH06 LOC08/R, E01-A/B + PROP-HD03 | Cùng khung đọc để người chơi tự lật mặt, đối chiếu ghi chú về khoa Nội–Nhi với chữ ký. Bố trí vệt sáng đọc được chữ ở góc mặc định. E06a đã đặt lời tranh luận 88/12 ở băng 2003. | Thấy lý lẽ Vĩnh từng dùng và dấu hành chính của một lựa chọn. Chưa biết Minh đã thấy gì khoảng 03:25. | Inspect lớn, đọc hai mặt bằng phím, nhãn phân biệt băng và phiếu lệnh. Đường truy hồi CH06 giữ theo Core 04. |
| CH07 B03, 03:09–03:12 | Giữ khung nhìn của Minh bên ngoài kính, Lan kêu cứu và cung cấp điều cô biết. Hướng khung nhìn ra lối Khu B để người chơi hiểu lựa chọn. | Tách lời cầu cứu, điều Minh tin lúc ấy và sáu người đã được đưa ra. | Caption tiếng đập kính, transcript và dấu hiệu phía sau kính. |
| CH07 B03, ~03:25 → 04:15 | Ba khung theo thứ tự bảng liều trên kính, tay Minh chạm chìa rồi rụt, cắt tới bàn giao chìa. Báo chuyển giờ bằng thẻ thời gian. Dùng hình và âm thanh trong giới hạn [errata TASK027](../m2/design-lock-errata.md). | Việc rụt tay thuộc lựa chọn của Minh. Chìa được bàn giao sau chuyển thời gian. Thời điểm tiếp cận của cứu hộ và khả năng cứu còn chờ thẩm định. | Khi giảm chuyển động dùng frame tĩnh và dissolve dịu. Skip mở cùng transcript E08 và giữ nonexport. |
| CH07 B03, 07:40 | Cận tay Vĩnh di từ sổ chìa sang phiếu; để khoảng lặng sau lời Minh muốn báo điều đã thấy. Vĩnh chuyển giọng sang nhấn sáu người được cứu. Trả điểm nhìn về bút và tay Minh. | Vĩnh dùng một nguyên tắc đã tự biện hộ cho mình và chủ động chuyển nó thành ép buộc. Minh tự ký dưới sức ép. | Caption/VO, hỗ trợ skip toàn đoạn, người chơi hiểu vẫn có lựa chọn ở thời điểm ký và can thiệp diễn ra sau đó. |
| CH08 B03, AN08 | Lan chỉ chỗ trống trước câu “Anh tự ghi đi, Minh”, tay rời khỏi bút. Giữ khoảng chờ để người chơi hiểu đối chiếu xong nhưng lời khai còn thuộc Minh. | Cử chỉ hồi đáp lời hẹn sửa sổ của E10a. Lan có hành trình sửa sai riêng, Minh chịu trách nhiệm về phần mình. | Cử chỉ chỉ minh họa, giữ câu hiện có và text khi skip. Không phát trong branch khác với điều kiện Core 04. |

Trong animatic ưu tiên lựa chọn của Vĩnh, việc Lan bảo toàn dấu vết bệnh nhân, và sự gắn bó thể hiện qua làm việc chung. Các hành vi mới có thể đổi nghĩa canon phải qua review trước khi khóa shot. Dùng thử đọc mù theo [ma trận QA](qa-impact.md) trước khi quyết định thu thêm thoại.

## Phát triển visual và nội dung môi trường

| Đầu ra | Tận dụng | Tiêu chí duyệt bản mẫu |
| --- | --- | --- |
| Bảng R/B/M cho cùng một điểm nhìn | Hai khung campus R/B và mẫu ánh sáng Core 05, thêm trọng tâm bảng liều, tay/chìa, cửa kính ở M | Giữ mốc hình học. Người xem phân biệt được chứng cứ R, sự sạch sẽ có tính phòng vệ của B và góc nhìn ký ức hữu hạn của M khi tắt màu hoặc motion. |
| Sheet CHAR002 và CHAR005 | Turntable, LOD, wireframe và shot mẫu M2-C.1 | Ba cự ly hành lang/trung/cận. Lan đọc được ánh mắt và động tác chỉ bút; Gatekeeper đọc được dáng chặn cửa, đạo cụ chìa và chuyển trọng tâm trong low light. Đối chiếu nhận diện bằng hành vi ở chế độ mute. |
| Prop và chữ hiện trường | Bản lưu LOC08, thư E10a, E03, lớp giấy hai mặt, bản sao/ảnh đối chiếu của Bảy | Mỗi phiếu lắp được vào góc đọc an toàn, ghi nguồn, mốc, lớp, giới hạn tiết lộ và hỗ trợ chữ lớn. Dùng fixture hiện có và chỉ tạo biến thể khi source/beat cần. |
| Bảng thời kỳ và license | TASK023, vật liệu V/I/F/U trong Core 03/05, [ThirdPartyNotices](../../Prototype/ThirdPartyNotices.md) | Mỗi ảnh/cue có nguồn, quyền dùng reference hoặc phân phối, phiên bản, attribution, mục đích và asset ID. Hạng mục thiếu bằng chứng giữ U hoặc chờ. |

Environmental text ở CH02/03/05 ưu tiên tác vụ nhân viên, lớp sử dụng của không gian và đời sống bệnh nhân. Đề xuất giới hạn mỗi điểm đọc vào một hành động hoặc một suy luận xác định, với người sở hữu, giới hạn tiết lộ và text thay thế ghi cùng ID. CH02 kiểm lại prop hiện có. CH03/04 chờ xác định phạm vi M3.2. CH05 trở đi chỉ lên kế hoạch tài liệu.

## Cue sheet và handoff audio

| Cue hoặc thoại | Trigger/layer | Chỉ dẫn và thay thế | Kiểm |
| --- | --- | --- | --- |
| `CH07-D11/D12` và đập kính | B03 M, 03:09–03:12 | Ưu tiên tiếng Lan và hướng kính, ambience duck theo Core 05. Caption nêu kính theo hướng tương đối, không lấn subtitle thoại. | Nghe mono, mute, thử giữ câu đủ đọc khi skip. |
| `CH07-D15` | B03 M, ~03:25 → 04:15 | Hơi thở, tiếng chìa rất gần, còi nền trung tính, thẻ chuyển giờ. Không đưa khẩu lệnh cứu hỏa, số người, vị trí xâm nhập hoặc mô phỏng ứng cứu trước TASK027. | Tắt SFX còn đủ hiểu tay rụt và mốc giao chìa, reduced motion tránh nháy/strobe. |
| `CH07-D16–D20` | B03 M, 07:40 | Vĩnh nói thấp và chính xác, chỉ thay nhịp ở điểm chọn hướng nói. Hạ âm nền ở chữ “sáu người” và tiếng đặt bút. | Nghe riêng diễn xuất trước mix. Bản E08 text phỏng theo cảnh duyệt duy nhất. |
| `CH08-D03/D16`, `POST-D08` | CH08 B03 / END04 B08 riêng | Lan đối thoại trước khi Minh chọn. Giữ motif ngân đúng năm nốt ở hậu đề với caption tương ứng. | Mute và skip đúng branch. Kiểm không rò Cơ sở 02 qua PA, ambient bark hoặc metadata hiển thị. |
| PA, ambience và barks | Theo `07/Events` và lớp tại cảnh đã xác định | Dùng lại PAStatic/cue đã có. Ghi ID, context, bộ lọc, ưu tiên, cách lặp, điều kiện im lặng, caption và thời lượng cho mỗi cue mới. Thử bản demo có bề mặt text trước khi duyệt thu. | Kiểm mỗi câu từ master, dấu tiếng Việt, mức đọc qua máy và nghe tay của audio lead, tránh chỉ dựa ASR. |

Mỗi lượt handoff gồm shot/cue ID, source revision, speaker, trigger/guard, asset master, file tích hợp, biến thể/giới hạn lặp, license, text/mute/skip/reload, và bằng chứng trên build. Thu demo để chọn giọng trước final. Duyệt cảnh và nhãn nguồn trước khi render/thu lại.

## Thẩm định biểu đạt và nội dung

Phối hợp theo [dossier M2](../m2/specialist-review-dossiers.md). TASK026 xem không gian, thiết bị và trang phục. TASK027 xem mặt cắt, khói và ứng cứu 2005. TASK028 xem hồ sơ hư cấu, provenance, danh tính, sự cưỡng ép, phẩm giá người bệnh, thí nghiệm và miêu tả trẻ em. Chuẩn bị các clip ngắn với beat/TC và các câu hỏi cụ thể. Ghi góp ý theo cảnh, mức độ, ràng buộc sửa, người ký và bản đã xem. Bản ghi nội dung dự kiến nêu hỏa hoạn, mất người thân và cưỡng ép y tế. Nhãn phân loại, thị trường, cách trình bày cảnh và bản cảnh báo cuối chờ phê duyệt.
