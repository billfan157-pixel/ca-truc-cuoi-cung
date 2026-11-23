# CH01-R2 · Đề xuất nâng cấp gameplay Chương 1

> **LƯU TRỮ 23/11/2026.** Bản đề xuất CH01-R2 trước khi đồng bộ thiết kế. Dùng [walkthrough B01–B08](../../ch01r/walkthrough-gameplay-B01-B08-investigation-v1.md) và DEC-CH01R-04 cho triển khai và nghiệm thu CH01. Các ý tưởng dưới đây được giữ để tra cứu lịch sử.
>
> Ngày 23/11/2026. Phạm vi: Chương 1 — Người trở về (B01–B08). Nền tham chiếu: `walkthrough-gameplay-B01-B08-investigation-v1.md`, `acceptance.md`, `puzzle-dependency-graph.md`, và proposal ba lô `docs/gameplay/backpack-physical-investigation-proposal.md`.
>
> Tài liệu này **không** đăng ký event/evidence ID mới vào runtime. Mọi mã `MD-*`, `DREAD_*`, `OBS101E` là **đề xuất**, phải qua `07` (Decisions/Changelog) trước khi vào code.
>
> **Ràng buộc bất di bất dịch:** CH01 giữ đúng **một** evidence (`E10a`); ba nhánh F101 đều mở `G01`; không có Hunter/chase; tổng thời lượng **không tăng** quá 30 phút; `firstDecision` bất biến; R độc lập 100%.

---

## 1. Chẩn đoán — 6 lỗ hổng cần vá

Đối chiếu với 9 game tham chiếu (Alien: Isolation, Obra Dinn, Golden Idol, RE7, Amnesia: The Bunker, Outlast, P.T., MADiSON/Visage) và 6 nguồn phê bình/thiết kế, Chương 1 có sáu vấn đề xếp theo mức thiệt hại:

| # | Lỗ hổng | Bằng chứng | Thiệt hại |
|---|---|---|---|
| P1 | **55–60% chương trước quyết định thật** | B01–B04 = 11,5–16,5 phút; F101 ở B05 | Người chơi bỏ trước phút 10 |
| P2 | **Suy luận là tùy chọn** | A và B không cần bằng chứng; chỉ C cần | 2/3 người chơi không suy luận bước nào |
| P3 | **Khám phá chỉ trả bằng text** | STAFF/giường 2/3 = đọc thêm 4 nguồn | Không có phần thưởng không gian |
| P4 | **Căng thẳng phẳng, không leo thang** | Không có gì đếm ngược; dread có ngân sách nhưng tuyến tính | Bất an, không hồi hộp |
| P5 | **Hệ quả dừng ở một phòng** | Pocket Note không phản ánh chữ ký; CH02 không thấy dấu | Quyết định không "theo" người chơi |
| P6 | **Ký xong không ai bảo đảm nhìn lại** | Hậu quả đổi hình học nhưng không có tín hiệu kéo mắt | Khoảnh khắc mạnh nhất bị bỏ lỡ |

Nguyên tắc xuyên suốt: **không thêm nội dung, chỉ tái phân bổ và thêm phản hồi.** Mục tiêu là 20,5–30 phút, không phải 35.

---

## 2. Sáu nâng cấp cốt lõi

### N1 — Đồng hồ ca trực (giải P1, P4)

**Hiện trạng:** đồng hồ lớp B dừng 03:17, là vật trang trí + nguồn EX01.

**Đề xuất:** đồng hồ **chạy** từ 02:40 → 03:17, nhưng **không chạy theo giây thực**. Nó nhích theo **tiến độ hiểu biết**:

| Sự kiện | Kim nhích |
|---|---|
| Bước vào HALL lần đầu | +04 phút |
| Mở cửa 101 (`CH01-T06`) | +03 |
| Mỗi `OBS101x` chuyển Discovered→Examined | +02 |
| Mỗi nguồn STAFF/giường 2/3 đọc xong | +01 |
| Mỗi lần MD-01/MD-02 kích | +01 |
| Ký F101 khi đồng hồ < 03:10 | **nhảy thẳng 03:17** |

**Vì sao hoạt động:**
- Người khám phá nhiều → kim tiến từ tốn → **thấy nhiều hơn**, không bị phạt. Đây là *phần thưởng* cho chiều sâu, không phải hình phạt.
- Người đi thẳng → kim đứng → ký sớm → **cú nhảy 03:17 ngay lúc bút chạm giấy**. Một cú giật thời gian, không cần jumpscare.
- Không có fail state. Không có "hết giờ thua". Đúng mô hình *Outer Wilds*: đồng hồ tạo ý nghĩa cho hành động, không tạo áp lực thắng/thua.
- Diegetic: đồng hồ đã tồn tại trong scene, không cần UI mới.

**Cấm:** không hiển thị đếm ngược, không đổi màu, không âm báo động. Nghiên cứu chỉ rõ: đồng hồ "nhấp nháy đỏ" phá immersion; đồng hồ **mờ** mới đáng sợ.

---

### N2 — Im lặng leo thang (giải P4)

**Đề xuất:** âm lượng ambience lớp B **tỷ lệ nghịch** với kim đồng hồ.

| Kim | Còn lại |
|---|---|
| 02:40 | Đủ: quạt, mưa xa, bút, giấy, hành lang |
| 03:00 | Mất mưa, mất quạt. Còn bút + bước chân Minh |
| 03:10 | Chỉ bước chân Minh + một tiếng tick đồng hồ |
| 03:17 | **Câm 1,2 giây** → một tiếng duy nhất: móc rèm |

**Căn cứ:** kỹ thuật *drop-out* — rút dần lớp âm thanh tạo căng thẳng mạnh hơn thêm nhạc; nhưng im lặng quá dài bị người chơi hiểu là **lỗi audio**, nên **luôn giữ ít nhất một lớp sống** (bước chân Minh), và **luôn kết thúc bằng một âm thanh đơn lẻ rõ ràng**.

**Căn cứ thứ hai:** audio director của RE7 mô tả đúng cơ chế này — dựng "âm thanh đời thường" để người chơi quen, rồi **xoá sạch** để phá cảm giác an toàn; hoặc **cô lập một âm thanh duy nhất** (tiếng đồng hồ) để người chơi nhận ra mọi thứ khác đã biến mất.

**Chi phí:** một tham số mix duy nhất, lái bởi biến đếm đồng hồ. Không cần hệ thống mới.

---

### N3 — Xe đẩy tiến gần (giải P4, P6)

**Hiện trạng:** `DREAD_B04_TROLLEY_STOP` — tiếng xe đẩy xa rồi khựng, chạy một lần.

**Đề xuất:** biến nó thành **thang tiến gần**, không phải cue một lần.

Mỗi lần người chơi hoàn tất một quan sát ở B, tiếng xe đẩy phát lại **gần hơn một đoạn** dọc HALL → 101:

```
[HALL xa] → [HALL giữa] → [ngoài cửa 101] → [ngay sau vách hốc 4]
```

Không bao giờ nhìn thấy. Không bao giờ lao vào camera. Đến lúc người chơi ký, nó đã ở **sau vách** — và tiếng gõ sau vách của P-SC05 trở thành **hệ quả logic**, không phải jumpscare ngẫu nhiên.

**Vì sao hoạt động:** đây là *menace gauge* phiên bản không có quái vật. Alien: Isolation dùng khoảng cách của Xenomorph; CH01 dùng **khoảng cách của âm thanh**. Người chơi không thấy gì, nhưng **cảm giác có thứ đang tiến**.

**Ràng buộc:** nếu người chơi đứng yên không quan sát, xe không tiến. Chấp nhận — người đi thẳng nhận escalation từ N1 (cú nhảy đồng hồ).

---

### N4 — Ba micro-decision trước B05 (giải P1, P2)

Mục tiêu: **time-to-first-decision ≤ 8 phút**.

#### MD-01 · B01 · Đèn xe (≈60 giây)

Đèn pha xe máy bật/tắt bằng `E`, không cần inventory.

| Lựa chọn | Thấy rõ | Bỏ lỡ |
|---|---|---|
| Bật đèn | Biển thông báo tháo dỡ, vệt lốp | Bảng tên cũ (bị loá) |
| Không bật | Bảng tên cũ, ô kính sảnh | Biển tháo dỡ |

Hệ quả: một dòng Pocket Note khác nhau; B08 đèn sân phản chiếu lựa chọn. **Không** ảnh hưởng gate, **không** thêm item.

#### MD-02 · B03 · Mở ngăn kéo khi Lan đang nhìn (≈90 giây)

| Lựa chọn | Phản hồi | Flag |
|---|---|---|
| Mở khi cô đang viết | Cô ngẩng lên, **một câu duy nhất**: "Anh cần gì cứ nói." Rồi viết tiếp | `LAN_SAW_DRAWER` |
| Đợi cô cúi xuống | Không ai thấy | `LAN_DIDNT_SEE` |

Hệ quả: nếu `LAN_SAW_DRAWER`, ở **B06** trang sổ bàn giao có **một dòng chữ của Lan** ghi việc đó. Người chơi mở sổ và đọc thấy **chính mình trong ghi chép của người đã biến mất**. Đây là khoảnh khắc mạnh nhất mà đề xuất này tạo ra, và nó gần như miễn phí.

**Cấm:** Lan không nhắc lại, không trách móc, không đổi thái độ. Một câu, một lần.

#### MD-03 · B04 · Cất thẻ AS-05B hay để lại (đã có, cần signpost)

Logic ba kịch bản đã tồn tại. Thiếu **tín hiệu rằng đây là quyết định**:
- Cất thẻ → ô chữ ký trống **rời khỏi tầm nhìn**; Pocket Note ghi "đã cất".
- Để lại → nó vẫn ở giá kẹp khi quay lại; B08 **không** có echo.

Chỉ cần thêm một dòng phản hồi khi cất, không thêm hệ thống.

#### MD-04 · B02 · Tiến sát ô kính (≈30 giây)

Tiến sát và nhìn = `OBS_GLASS` (không phải evidence). Mở một dòng thoại mới ở B03:
> Lan: "Anh đứng ngoài đó lâu chưa?"

Đây là **payoff chéo beat**: thế giới nhớ việc người chơi đã làm ở beat trước. Rẻ nhất trong bốn cái.

---

### N5 — Tủ STAFF kẹt: khám phá phải mở được thứ gì đó (giải P3)

**Hiện trạng:** STAFF chỉ có text.

**Đề xuất:** cửa tủ hồ sơ điều dưỡng **kẹt chốt dưới**.

| Cách | Điều kiện | Kết quả |
|---|---|---|
| Cạy mạnh (giữ `E` 1,5s) | Luôn được | Mở, nhưng **tiếng kim loại lớn** → dread +1 bậc; nếu xe đẩy đang gần, nó **khựng lại** (phản hồi tức thì) |
| Mở đúng cách | Đã đọc `OBS_CORRIDOR_NOTICE` | Mở **êm**, không tăng dread |

Bên trong: **nửa carbon của phiếu AS-05** (`OBS101E`, đề xuất) — chứng minh phiếu là **bộ đôi**, nên ô chữ ký trống nghĩa là **quy trình dở dang**, không phải mất giấy.

**Đây là spatial payoff của hiểu biết** — thứ đang thiếu hoàn toàn. Người chơi *hiểu quy chế* → *mở được tủ êm* → *nhận thêm chứng cứ củng cố*.

**Cân bằng:** `OBS101E` **không** thay thế A hoặc B. C vẫn cần đúng `OBS101A` + `OBS101B` + mark `F101_ZERO` + `OBS101B_SIGN` + link `OBS101A_TRACES` + claim `UNRELIABLE_ZERO`. Nó chỉ thêm **một dòng xác nhận**, không mở khóa gì.

---

### N6 — Hậu ký: ba tín hiệu kéo mắt về phía sau (giải P6)

Không ép camera. Ba lớp, chạy **sau khi inspector đóng**:

1. **Ánh sáng** — một bóng đèn ở hốc 4 **tắt** (A) / **chập** (B) / **rèm tách, sáng hành lang hắt vào** (C). Vùng tối **mới** xuất hiện trong ngoại vi thị giác.
2. **Âm thanh có hướng** — tiếng gõ phát **từ phía sau**, không từ phía trước. Phản xạ quay đầu, không phải mệnh lệnh.
3. **Đồng hồ nhảy** — nếu ký sớm, kim nhảy 03:17 đúng lúc bút chạm giấy, kèm một tiếng "cạch". Đồng hồ nằm gần hốc 4, nên cú nhảy **kéo mắt về đúng hướng**.

Ba lớp này **không vi phạm** quy tắc "không scare khi đang mở ba lô / soi 3D / menu", vì chúng chỉ chạy sau khi phiếu đã commit và inspector đã đóng.

---

## 3. Mang hệ quả sang CH02 (giải P5)

Ba thứ đi theo, **tất cả dùng dữ liệu đã có**:

| Vật mang | Nội dung | Xuất hiện ở |
|---|---|---|
| **Pocket Note** | A: "Đã xác nhận: 0." · B: "Chưa xác minh." · C: "Số 0 chưa đủ căn cứ." | Mở ba lô bất kỳ lúc nào ở CH02 |
| **Dòng sổ bàn giao** (nếu `LAN_SAW_DRAWER`) | Chữ của Lan ghi việc Minh mở ngăn kéo | B06, và bản photocopy trong hồ sơ CH02 |
| **Trạng thái phòng 101** | `deniedBay` / `pendingBay` / `refutedBay` | Đã có, giữ nguyên |

**Căn cứ:** Telltale mất uy tín vì hứa "she will remember that" rồi không nhớ. Nhưng cùng nghiên cứu chỉ ra **chiếc hoodie của Clementine** là ví dụ đúng: một vật nhỏ, nhìn thấy mỗi cảnh, nhắc lại lựa chọn **mà không cần hệ thống ký ức nào**. Pocket Note là chiếc hoodie đó — rẻ, bền, không bao giờ nói dối.

---

## 4. Làm C bớt "mù" (giải P2)

**Bài học Golden Idol:** khi người chơi điền sai mà **không biết sai ở đâu**, họ ức chế; studio phải thêm chỉ báo *"sai từ hai ô trở xuống"*. Nhưng cũng chính studio đó cảnh báo: chỉ báo quá rõ **giảm độ khó và độ dài**.

**Đề xuất:** khi C bị từ chối, Minh nói **đúng một** thiếu sót, không bao giờ cả hai:

| Thiếu | Câu |
|---|---|
| Chưa xem `OBS101A` | "Mình mới có tờ giấy. Chưa nhìn thấy gì ở chỗ đó." |
| Chưa xem `OBS101B` | "Mình thấy dấu ở đó. Nhưng chưa có gì nói chỗ đó đã bàn giao." |

**Cấm:** toast đỏ, âm "sai", đếm số lần thử, khoá nút. A và B **luôn bấm được ngay**.

---

## 5. Thời lượng — tái phân bổ, không bành trướng

| Beat | Hiện tại | Sau nâng cấp | Ghi chú |
|---|---|---|---|
| B01 | 1,5–2,5 | 2–3 | +MD-01 |
| B02 | 1–1,5 | 0,75–1 | Gộp 5 cách nhìn → 3 |
| B03 | 3–4,5 | 3,5–5 | +MD-02 |
| B04 | 6–8 | 6–8 | Không đổi (đã dày nhất) |
| B05 | 5–7 | 5–7 | Không đổi |
| B06 | 2–3 | 2–3 | +dòng sổ nếu MD-02 |
| B07 | 1–1,5 | 1–1,5 | Không đổi |
| B08 | 1–2 | 1–1,5 | Cắt phần đi bộ thừa |
| **Tổng** | **20,5–30** | **20,5–30** | **Không tăng** |

---

## 6. Lộ trình theo chi phí

| GĐ | Nội dung | Công sức | File chạm |
|---|---|---|---|
| **0 — Miễn phí** | Pocket Note theo chữ ký · câu từ chối C cụ thể · 1 conditional line Lan · tắt đèn hậu ký | 1–2 ngày | `HospitalGame.Pages.cs`, `Casework.cs`, text table |
| **1 — Rẻ** | Xe đẩy tiến gần (4 emitter) · drop-out mix · kim đồng hồ chạy | 3–5 ngày | `AmbientDreadDirector.cs`, audio mixer |
| **2 — Trung bình** | MD-01 đèn xe · tủ STAFF kẹt · `OBS101E` · signpost MD-03 · MD-04 | 1–2 tuần | `InteractableRig.cs`, `DeductionFramework.cs`, `HospitalScene.cs` |
| **3 — Đắt** | Ba lô 3D `BP-00…BP-07` | Theo proposal riêng | `InventoryService`, schema v7 |

**Khuyến nghị thứ tự:** GĐ 0 → GĐ 1 → **chạy `TASK010`** → mới quyết định GĐ 2. Lý do: GĐ 0–1 vá được P1/P4/P5/P6 với chi phí gần bằng không, và **đủ để playtest có ý nghĩa**. GĐ 2 thêm nội dung; không nên thêm trước khi biết người chơi thật phản ứng thế nào.

---

## 7. Tiêu chí nghiệm thu (đo được)

| Chỉ số | Ngưỡng đạt | Cách đo |
|---|---|---|
| Time-to-first-decision | ≤ 8 phút | Bấm giờ từ spawn tới lần đầu chạm một vật có hai lựa chọn |
| Tỷ lệ chọn C | ≥ 3/6 | Log `firstDecision` |
| Quay lại nhìn phòng sau khi ký | 6/6 | Đếm số người xoay camera >90° trong 20s sau commit |
| Trả lời được "vì sao mày lấy món đó ra?" | 6/6 | Hỏi ngay sau khi dùng item |
| Thời lượng trung vị | 22–28 phút | Log session |
| Không ai nghĩ mình bị bấm giờ | 0/6 | Hỏi: "Mày có nghĩ game đang đếm ngược không?" |
| Không ai nghĩ audio bị lỗi | 0/6 | Hỏi: "Có lúc nào mày nghĩ game mất tiếng không?" |

Hai chỉ số cuối là **chỉ số âm** — quan trọng ngang các chỉ số dương. Nếu N1 hoặc N2 bị hiểu sai, phải tắt chúng, không phải giải thích thêm.

---

## 8. Rủi ro của chính đề xuất

1. **Đồng hồ bị hiểu là time limit.** Giảm thiểu: không UI, không đếm ngược, không âm báo. Nếu test vẫn hiểu sai → bỏ N1, giữ N2.
2. **Drop-out bị hiểu là lỗi audio.** Giảm thiểu: luôn giữ tiếng bước chân; khoảng câm tối đa 1,2s; luôn kết bằng một âm đơn.
3. **`OBS101E` làm C dễ hơn.** Giảm thiểu: không nới điều kiện C; chỉ thêm một dòng xác nhận.
4. **MD-02 làm Lan "biết tuốt".** Giảm thiểu: một câu, một lần, không nhắc lại, không đổi thái độ.
5. **Phình schema save.** Mọi flag mới (`LAN_SAW_DRAWER`, `MD01_LIGHT`, `SHIFT_CLOCK`, `TROLLEY_STAGE`) phải vào schema + migration, có test round-trip.
6. **Xung đột với proposal ba lô.** Nếu `BP-00` dựng trước, `MD-03` phải viết lại theo `InventoryService`. Ưu tiên GĐ 0–1 để tránh đụng.

---

## 9. Quyết định cần chốt

| ID | Câu hỏi | Đề xuất |
|---|---|---|
| `D-CH01R2-01` | Đồng hồ tiến theo quan sát hay theo thời gian thực? | **Theo quan sát.** Thời gian thực tạo áp lực giả và phạt người khám phá. |
| `D-CH01R2-02` | `OBS101E` có được tính vào điều kiện C? | **Không.** Chỉ xác nhận, không mở khóa. |
| `D-CH01R2-03` | A/B có nên có giá về tiến trình? | **Không.** Giá nằm ở Pocket Note và hình học phòng, không ở gate. |
| `D-CH01R2-04` | Dựng `BP-00` trước hay chạy `TASK010` trước? | **`TASK010` trước**, trên build hiện tại, ghi rõ giới hạn Journal. |

---

## 10. Liên kết nguồn

- Walkthrough gốc: `docs/ch01r/walkthrough-gameplay-B01-B08-investigation-v1.md`
- Nghiệm thu CH01-R: `docs/ch01r/acceptance.md`
- Đồ thị puzzle/gate: `docs/ch01r/puzzle-dependency-graph.md`
- Proposal ba lô: `docs/gameplay/backpack-physical-investigation-proposal.md`
- Nghiên cứu đối chiếu: Alien: Isolation (menace gauge, nucl.ai 2016); Obra Dinn (rule of three); Golden Idol (chỉ báo ≤2 ô sai, "closed puzzle"); RE7 (audio director Wataru Hachisako về drop-out và cô lập âm thanh); Amnesia: Bunker (generator clock, semi-open); Outer Wilds/Majora (đồng hồ diegetic, không hiển thị); Telltale (hoodie vs "will remember that").
