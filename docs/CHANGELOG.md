# Nhật ký thay đổi Core v3.0 → v3.1 → v3.2 → v3.3 → v3.3.1 (Final Clean)

## Core v3.3.1 — Final Clean Design Lock (24/11/2026)

| Quyết định | Thay đổi |
|---|---|
| DEC-V331-01 | Vá 3 điểm P1: (1) bỏ cụm tháo dỡ 2012 khỏi tờ ký năm 2005 `E01-B` (giữ kỳ hạn theo dõi 7 năm; việc thúc đẩy tháo dỡ trùng hạn chuyển sang `E10b` 2012 & `CAN07`); (2) tách rõ trục thang trong nhà `S1` (`LOC02 ↔ LOC05 ↔ LOC07` luôn thông ở `R`) khỏi riêng cửa thoát xả ra sân của chiếu nghỉ trệt `S1`; (3) bỏ chi tiết cổng phụ `LOC01` tự khóa chốt xích ngoài sau `T09`, thay bằng phản hồi chủ quan diegetic nhất quán của Minh (giữ rào tôn/dây xích vật lý cho 3 cổng `K-B`, `K-D`, `K-F`). |
| DEC-V331-02 | Vá 3 điểm P2: (4) thêm nhịp chuyển thời gian rõ ràng giữa `~03:25` và mốc giao chìa `04:15` ở `CH07-D15`, `EVT026`, `E08`; (5) giữ độ mở đời thường cho chi tiết đôi dép chỉ xanh và 5 nốt nhạc của `C12` trong Phần 1 (`Core 02 §4.3`, `07/Patients`, khớp `E03`); (6) cập nhật mục quản trị sản xuất cũ ở `Core 01 §3` khớp `Unity 6.3 LTS / URP 17.3 / Windows x64` đã chốt ở M0–M2. |
| V331-DOC01 (Final Clean) | Dọn dứt điểm 7 điểm đồng bộ chéo cuối cùng: khớp hợp đồng QA `V31-Q05 ↔ V33-Q03` (`Core 02/06`, `07/QA`, `Narrative QA`, `Plot twists`), xóa 6 chỗ sót cụm `"giải phẫu E04"`, sửa header `v3.3.1.1` trên `Core 02`, xóa 3 chỗ sót `"van bị cắt/dây cột"`, đồng bộ `03:25→04:15` ở `07/Events` & `Canon Matrix`, cập nhật `07/Narrative QA (V33-Q05)` và cập nhật 2 đoạn quản trị sản xuất cũ ở `Core 05 §1.4` & `Core 06 §2.4`. |

## Core v3.2 → v3.3 — Design Lock (24/11/2026 — bảo lưu truy vết)

| Quyết định | Thay đổi |
|---|---|
| DEC-V33-01..07 | Bổ sung nhịp tích tụ tâm lý & tổ hợp suy luận `~03:25`; thêm ngày lưu lạnh `14/11/2005` trên `PROP-XK12`; tự động thu `PROP-XK12` vào manifest xuất qua `B02/AN08` & khóa manifest phần ngoài Bảy; dọn 5 điểm tàn dư cũ; khóa giám sát 7 năm & lý do Bảy trì hoãn (đơn 2006 bị trả); khóa mô hình cộng hưởng, 3 tầng thực thể (`C12 ≠ CHAR007`) & số phận dư âm sau tháo dỡ; nâng cấp chiều sâu Giám đốc Vĩnh & 12 bệnh nhân `C01–C12`. |

## Core v3.0 → v3.2 (24/11/2026 — bảo lưu truy vết)

| Quyết định | Thay đổi |
|---|---|
| DEC-V32-01..05 | Khóa tâm lý `03:09–03:12` & `~03:25`, thêm `CH04-D09` giải thích mốc `02:50`, dọn tàn dư CH03–06, tách UI chất vấn `PZ10`, sửa 3 bảng lệch CH06–08 ở Core 04 và xóa dòng `NaN` ở `07/Dialogue`. |
| DEC-V31-01..08 | Khóa tổng 13 (`12 cháy + 1 thế thân`), tuyến `LOC10 ↔ 09 ↔ 11 ↔ 12`, phạm vi ký ức AS-05, biên độ Lan, chìa C12 duy nhất, manifest `END03/04` và hậu đề độc quyền `END04`. |

Tra `Core 07 → Changelog` để xem trạng thái theo quyết định và `Core 07 → QA` để xem các ca build chưa chạy.
