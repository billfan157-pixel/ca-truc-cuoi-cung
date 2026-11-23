# Gameplay Redesign — BA LÔ ĐIỀU TRA & TƯƠNG TÁC VẬT THỂ 3D (Proposal V1.1)

> **STATUS: DESIGN PROPOSAL / REVIEW REQUIRED — NOT IMPLEMENTED IN UNITY.**
>
> Ngày 23/11/2026. Scope: gameplay toàn game, vertical slice CH01–CH02 trước; phụ thuộc Walkthrough CH01 v1.2 đang ở nhánh `design/ch01-b01-b08-investigation-walkthrough`. Source of truth vẫn là Core 01–07, Gameplay/Narrative/Level skills và DEC/LOCK đã được duyệt. Bản này **không** thêm event ID/công năng runtime bằng cách ghi ra tài liệu; mọi `BP-*` là mã **đề xuất**.
>
> **Thiết kế lõi:** thay **Sổ trực/Journal toàn màn hình** như trung tâm gameplay bằng **Ba lô điều tra có vật phẩm thật**. Người chơi nhìn/chạm/cầm/soi 3D/nhặt/cất/lấy ra/cầm sử dụng/đặt lên mặt bàn/đối chiếu hai nguồn và sau đó ra quyết định. Giữ logic evidence, provenance, F101, timeline, patient identity, endings và save/load; không biến ba lô thành một hệ crafting hoặc kho lưu đồ vô nghĩa.

## 1. Nghiên cứu đối chiếu — học cơ chế, không sao chép nguyên xi

| Nguồn đối chứng | Điều nghiên cứu xác nhận | Bài học áp dụng cho CTCC | Cần tránh |
| --- | --- | --- | --- |
| **Amnesia: The Bunker** — Frictional Games / Steam | Thế giới có tương tác vật lý, đồ/công cụ hỗ trợ giải quyết tình huống; Frictional bổ sung Physics Interaction Mode, no-input-holds, caption, reduced motion trong accessibility patch. | Dụng cụ/vật thể cần có cảm giác cầm/đặt, cửa/ngăn kéo là một phần của lựa chọn; thao tác nhấn E và kéo analog tương đương về hiệu quả. | Không sao chép inventory căng thẳng khi đang đọc hồ sơ; CH01 không Hunter và không phạt vì đang mở ba lô. |
| **SIGNALIS** — hướng dẫn item Repair Logic Module, community manual | Menu item có **USE / COMBINE / INSPECT**, trong INSPECT có thể xoay đồ. Slot hạn chế là cơ chế survival horror. | Dùng verbs nhất quán cho đồ vật và phối hợp được kiểm soát bởi luật, inspector đầy đủ front/back/hotspots. | Không mượn giới hạn 6 slot để buộc bỏ bằng chứng; tránh "thử combine mọi cặp" không có ý nghĩa. |
| **The Mortuary Assistant** — manual cộng đồng + store | Điều tra dấu trên cơ thể dẫn tới ghi chép hồ sơ và các bước nghiệp vụ chuẩn bị vật dụng. Các thao tác phục vụ một quy trình có kết quả rõ. | Mỗi vật thể có câu hỏi nghiệp vụ: "Dùng để kiểm điều gì?"; khám phá chi tiết bằng tương tác nhỏ, không phải nhấn E để auto biết. | Không gamify đau khổ của bệnh nhân hay thêm minigame y tế phi thực tế. |
| **MADiSON / MADiSON VR** — phát biểu của nhà phát triển trên PlayStation Blog | Máy ảnh là công cụ dùng xuyên game, vật thể tương tác cầm tay được nhấn mạnh ở bản VR. | Tái sử dụng đèn pin, kính lúp/đèn hắt sáng, hồ sơ và vị trí vật thể như công cụ điều tra; có payoff khi soi lại. | Không thêm máy ảnh thần kỳ chỉ vì game khác có; không làm ảnh chụp thay thế provenance vật lý. |
| **Crow Country** — SFB Games / Steam | Khám phá khu vực bỏ hoang và giải câu đố đưa người chơi đến vật phẩm/địa điểm tiếp theo; có Exploration Mode. | Đồ nhặt từ một nơi có thể đem dùng tại vị trí khác; phản hồi với người thích khám phá không chiến đấu. | Không ép backtrack chỉ để đặt đồ vào ổ khóa hoặc làm chậm tuyến chính. |
| **SOMA** — Frictional Games design pillars & planning essay | Puzzle nên mọc từ câu chuyện, đồ vật có vị trí hợp lý, người chơi phải tự dẫn cuộc khám phá; “planning” làm cho việc mở ngăn kéo có mục tiêu. | Sử dụng vật phẩm vì có một giả thuyết muốn kiểm chứng, không vì objective số hóa; giữ quyền suy luận và phương án để ngỏ. | Không thay suy luận thành danh sách checklist tự động. |

**Nguồn đã tra (đọc trực tiếp ngày soạn):**
- [Amnesia: The Bunker Steam](https://store.steampowered.com/app/1944430/Amnesia_The_Bunker/) · [Frictional accessibility patch](https://frictionalgames.com/2024-01-amnesia-the-bunker-accessibility-patch/)
- [SIGNALIS item use / combine / inspect](https://signalis.wiki.gg/wiki/LSTR_Features:_Repair_Logic_Module) · [SIGNALIS Steam](https://store.steampowered.com/app/1262350/SIGNALIS/)
- [The Mortuary Assistant Steam](https://store.steampowered.com/app/1295920/The_Mortuary_Assistant/) · [community mechanics guide](https://steamcommunity.com/sharedfiles/filedetails/?id=2839414660)
- [MADiSON VR developer interview](https://blog.playstation.com/2023/08/22/first-person-horror-madison-is-coming-to-ps-vr2-gameplay-revealed/) · [Crow Country Steam](https://store.steampowered.com/app/1996010/Crow_Country/)
- [SOMA five design pillars](https://frictionalgames.com/2013-12-the-five-foundational-design-pillars-of-soma/) · [Frictional: planning and gameplay](https://frictionalgames.com/2017-05-planning-the-core-reason-why-gameplay-feels-good/)

**Độ tin cậy:** các trang nhà phát triển/Steam hỗ trợ đặc điểm game; hướng dẫn SIGNALIS/Mortuary là mô tả cộng đồng, không dùng chúng để khẳng định universal best practice. Tất cả lựa chọn cho CTCC bên dưới là **đề xuất thiết kế**, chưa có chứng cứ cho thấy playtest sẽ thích hơn.

> **Trạng thái 23/11/2026:** phần ba ngăn, schema v7, kẹp AN02, đối chiếu B→R và echo B08 đã có trong ứng viên CH01. Phần dưới ghi khảo sát và đề xuất ban đầu. Dùng [walkthrough B01–B08](../ch01r/walkthrough-gameplay-B01-B08-investigation-v1.md) và DEC-CH01R-04 khi triển khai hoặc nghiệm thu.

## 2. Kiểm toán code trước đợt triển khai

| File/hệ thống hiện có | Đã làm được | Điểm cần chuyển đổi |
| --- | --- | --- |
| `HospitalGame.Journal.cs`, `HospitalUI.Journal.cs` | Sổ trực 5 tab, hồ sơ đã xem, kéo tài liệu vào hai khung, viết note, chọn nguồn đối chiếu | Chưa phải kho vật phẩm vật lý. Tách model `JournalNote`/timeline/decision và biến nó thành **tệp giấy/dossier mở từ ba lô hoặc bàn làm việc**, không còn màn hình điều hướng gameplay chính. |
| `PhysicalInspector.cs` | Stage 3D gắn camera, quay/zoom, mesh giấy hai mặt, tilt/raking, detail, annotation; prefab 3D và fallback chữ lớn đã có | Dùng lại làm **InspectMode** chung cho vật phẩm được cầm từ túi hoặc tại chỗ, không tạo inspector thứ hai. |
| `HospitalGame.Pages.cs` + `Casework.cs` | `InspectRegisteredObservation`, `OpenObservationInspector`, `Take3D`, logic `SaveCheckpoint`; `HypothesisService` có kiểm provenance | Thay đường “xem hồ sơ → tự nằm trong tab tài liệu” bằng “xem / lấy / cất / đặt / sử dụng” có bước commit rõ, giữ quan sát là nguồn được xác thực. |
| `HospitalGame.cs` | Tab/J hiện mở `Journal()`; `Use(id)` điều phối đối tượng, scene có rig cửa/ngăn kéo/rèm | Tab/J mở **BackpackView**; tương tác target khi đang cầm vật dùng qua `WorldUseResolver`. Đảm bảo không làm vỡ cinematic Lan, save và refresh control. |
| `Protocol.cs` (schema v6), `AtomicSaves.cs` | State atomically saved, `evidence`, `observed`, `observationRecords`, notes, scene pose, F101 revisions | Thêm inventory **nếu được duyệt** ở schema v7, migration v1–6; không trộn item sở hữu với `state.evidence` hoặc tự cấp chứng cứ khi mở ba lô. |
| `DeductionFramework.cs` | `ObservationRecord` có `Discovered/Examined/Interpreted/Corroborated`; F101 C đòi A/B + marks/link/claim | Giữ authoritative; thao tác trực tiếp 3D tạo quan sát và link, không bypass `HypothesisService`. |

**Phát hiện quan trọng:** `Investigation.New()` hiện **seed `E10a` trong `state.evidence`**. Sự hiện diện trong evidence **không** đồng nghĩa đã cầm phong bì trong gameplay. Khi di trú phải gắn `BackpackEntry` E10a với **sự kiện nhặt/đọc** hoặc bước tương thích đã có (ví dụ `E10a.SMSRead`), không tạo `E10a` lần thứ hai và không phá `CH01 evidence.Count == 1`.

## 3. Fantasy trải nghiệm & vòng chơi mới

**Từ:** đi → nhấn E → toast/inspector → thêm bản sao vào Sổ → so UI → ký.

**Sang:** **chú ý → đến gần → dùng tay mở/hé/soi → nhặt hoặc ghi nhận tại chỗ → quyết định cất → mở ba lô → rút đồ → xoay/lật/soi 3D → dùng trên thế giới hay đặt cạnh nguồn khác → lập luận → chọn quyết định → thấy hậu quả vật lý**.

Không phải mọi prop cần nhặt: vết bánh trên nền, ổ cắm, cánh cửa, đồng hồ và chiếc giường là **hiện trường cố định**. Người chơi phải *quay lại nó* để kiểm; tự động cất mọi thứ vào túi sẽ xóa lý do khám phá không gian. Không phải mọi item phải dùng hai lần; mỗi item có chủ đích và ít nhất một cách trả lời “vì sao tôi mang nó?”.

### 3.1. Giao diện ba lô — diegetic nhưng nhanh

- Nhấn **Tab** (hoặc J qua alternate binding đang có) để Minh đưa chiếc **ba lô vải cũ** lên; camera FPS không teleport. Một vùng 3D/2.5D chiếu túi mở, có **ba ngăn hiển thị**: (1) **Dụng cụ & đồ vật**, (2) **Giấy tờ & vật chứng** (gắn nhãn rõ loại nguồn R/B/M, bản gốc/bản nhớ), (3) **Pocket Note** ghim bên trong nắp. **Backend vẫn giữ bốn loại dữ liệu vật lý/observation/evidence/note với provenance riêng**; giảm số nhóm UI không đồng nghĩa trộn dữ liệu. Nội dung theo **những thứ thật sự được giữ**, không phải toàn bộ quan sát.
- **Bấm một món:** ưu tiên mở trực tiếp inspector 3D bằng một thao tác; `Cầm ra`, `Soi 3D`, `Đặt lên bàn` (khi ở cạnh bàn), `Dùng với…`, `Xếp lại` và một số `Ghép` đã được tác giả thiết kế. Không có `Dùng` nếu không có target/công năng hợp lệ. `Esc` trả đồ và camera đúng trạng thái; shortcut tay cầm/quick-slot không cần mở ba lô liên tục.
- **Túi tài liệu** có bì thư, thẻ, phiếu/bản sao đã cất; mỗi món có 3D hai mặt và transcript chữ lớn. **Pocket Note** cho thấy 1–2 câu tay Minh viết về nghi vấn/dự định; khi cần, người chơi có thể mở tập ghi chú đầy đủ để xem observation/provenance/giả thuyết/mốc giờ và F101 history. Tập này vẫn là **vật thể phụ**, không còn là UI chính và không tự giải đố. Không xóa dữ liệu `JournalNote` đã có. Người chơi có thể lưu ghi chú về dấu sàn mà không “nhét dấu sàn vào ba lô”.
- **Bàn đối chiếu vật lý:** ở quầy, bàn STAFF và mặt bàn thích hợp trong phòng 101, đặt tối đa hai tờ tài liệu (hoặc tài liệu và vật thể nhỏ) lên hai vị trí 3D; rút giấy ra, lật/soi/di chuyển, hướng về dấu thật ngoài sàn. Nếu không thể dùng kéo-thả, có `Chọn nguồn trái/phải`, phím số và bản chữ lớn cùng trạng thái. **Không bắt buộc bàn** cho ký A/B hoặc hoàn thành CH01.
- **Không có giới hạn slot làm mất chứng cứ bắt buộc.** Giới hạn số món `đang cầm`/ô quick-slot để giữ sự hiện diện vật lý (ví dụ 1 món trên tay và tối đa 4 shortcut) nhưng giấy và vật chứng gốc tự được sắp theo ngăn. Nếu về sau thử giới hạn sức chứa cho vật dụng không trọng yếu, cần QA no-softlock trước.
- **Đang đọc không có Hunter ở CH01**. Ở các chương nguy hiểm hơn, có thể chọn freeze thế giới khi xem bản chữ lớn/đối chiếu bắt buộc, hoặc safe volume/hint rõ; không dùng inventory-unpaused của game khác để trừng phạt người đọc chậm.

### 3.2. Bốn “động từ” chơi bắt buộc

1. **Nhặt/Cất:** vật portable có nguồn, lớp và vị trí rõ; pickup không đồng nghĩa confirm giả thuyết.
2. **Soi/Lật:** xoay vật 3D, nghiêng ánh sáng, lật mặt, mở nắp/túi/con dấu, tìm hotspot; ai không thao tác chính xác được vẫn có action list.
3. **Lấy ra/Dùng:** equip một món vào tay, đem tới target trong scene, nhấn Use; target xác thực `itemId + targetId + layer + beat + provenance` trước khi chạy animation.
4. **Đặt/Đối chiếu:** đặt đồ lên mặt bàn/vị trí gốc, so với vật khác hoặc dấu vết thế giới, lưu **quan sát/suy luận** vào tập ghi chú, không tự đánh dấu đúng/sai.

## 4. Ownership + provenance contract (phải giữ canon)

**Không đồng nhất bốn loại dữ liệu:**
- **World object:** instance đang ở môi trường, có transform/layer/physical state; có thể cố định hoặc portable.
- **BackpackItem:** *vật Minh đang thực sự giữ* (instance đã nhặt), có thể cầm/đặt/dùng/soi; có thể là bản sao giấy có thể sờ trong B.
- **ObservationRecord:** điều Minh đã xem ở vị trí/lớp cụ thể; có `stage` và nguồn, không phải đồ vật có thể bỏ vào túi.
- **EvidenceRegistry / state.evidence:** vật chứng hợp lệ cho gate/export/endings; không được tăng bằng cách cất bản sao, note, ảnh hoặc một vật của B.

| Loại | Ở thế giới | Trong ba lô | Qua R/B/M | Export? |
| --- | --- | --- | --- | --- |
| **PersistentPhysical (R)** — E10a, mảnh tranh/vòng tay/phiếu chuyển CH02 sau khi lấy hợp lệ | Nhặt từ R bằng transaction | Có 3D gốc / prefab | Mang theo khi state cho phép | Chỉ theo `EvidenceRegistry`, không suy diễn |
| **LayerBoundPaper (B)** — AS-05B, OBS101C/D, quy chế, bản F101 chưa ký | Có thể nhấc xem, cất tạm để so trong B nếu đã được author cho phép | Ngăn “giấy trong ca trực” đánh dấu **B / chưa phải bản gốc 2012** | Khi B→R: **trả nguyên bản về fixture B**, slot tạm được chuyển thành **memory reference/transcript** phi vật chất trong tập giấy; không có tờ giấy ma rơi ra sân R | **Không**; CH01 evidence vẫn 1 |
| **AnchoredWorld** — F101 trên bảng ký, đồng hồ 03:17, ổ cắm, rèm, vết bánh | Chỉ soi/chạm, không nhét vào túi | **Chỉ note/ảnh mô tả nguồn**, không vật lý | Không di chuyển với player | Không phải vật chứng gốc |
| **MemoryOnly (M)** — E08 và những quan sát ký ức | Cảnh M | Chỉ reference có provenance M | Không xuất đồ vật M | **Nonexport** tuyệt đối |
| **EquippableTool** — đèn pin, chìa đã được author, kẹp giấy/đèn soi nếu có | Nhặt nếu đủ luật cảnh | Có một hand item, quick slot | Theo luật thực thể và nguồn | Không thành chứng cứ chỉ vì là tool |

**Quy tắc B→R không được là một cú lừa dữ liệu:** người chơi luôn thấy nhãn “tờ giấy thuộc cảnh giao ca B”; khi cảnh sụp, bản B **không tồn tại vật lý ở R** và ba lô vẫn mở lại được **ghi nhớ nguồn đã kiểm**, không rơi mất dấu observation. Nếu item đang cầm lúc swap, giao dịch phải preflight trả về vị trí B hoặc cất bản nhớ an toàn; lỗi giao dịch rollback toàn bộ hand/inventory/fixture. **Không biến bản nhớ thành đồ dùng với cổng/kho R.**

**Các quy tắc không thương lượng:**
1. CH01 `state.evidence.Count == 1` với `E10a`; không đưa `E11/E12/C01` ra khỏi CH02. Bản giấy B không xuất vật chứng.
2. `F101` ba nhánh A/B/C đều mở `G01`. C vẫn cần `OBS101A + OBS101B`, đánh dấu đúng, link đúng và claim đúng qua `HypothesisService`; A và B luôn có thể ký không cần A/B; giữ first/current/revisions. **Việc cất giấy không tự phát hiện chi tiết** chưa được đọc.
3. `R` của CH02 không bị đổi theo trạng thái A/B/C của lớp B; `LIST/SOCKETS/WHEELS`, PZ01/E12, E11 không thay thứ tự.
4. `03:17` chỉ là mốc mất liên lạc/foreshadow; không suy ra giờ tử vong. `CH01-D16b`/`CH04-D06` vẫn bị khóa.
5. E08 chỉ hoàn tất sau M theo CH07, không phải tờ giấy cất ba lô hay vật chứng export.

## 5. Thiết kế dữ liệu và API dự kiến cho Unity (chưa phải code đã chạy)

```text
ItemDefinition (static registry)
  id, label, kind, inventoryCategory, inspectPrefab/sideTextures,
  canStow, canEquip, canPlace, canCombine, allowedLayers,
  allowedChapters, provenanceRule, exportableEvidenceId?,
  worldUseActions[] { actionId, targetId, layer, guard, result }

BackpackEntry (save v7, instance)
  instanceId (stable, unique), itemDefId, status (InBag/Equipped/Placed/LayerBound),
  originLayer, originLoc, acquiredChapter, sourceWorldObjectId,
  inspectionFlags[], quantity, itemRevision

HandState (save v7)
  equippedInstanceId?, placementTargetId?, pendingUseActionId?

ObservationRecord (existing, authoritative)
  id, stage, provenance layer/LOC/chapter, inspectedDetails

Investigation (existing state)
  backpackItems[], equippedInstanceId?  // only after schema migration
  evidence[], observed[], observationRecords[], f101*, journalNotes[], ...
```

**Không lưu Transform động thô của mọi vật rơi tự do.** Vật được đặt chỉ tại các **Anchor** đã author với `anchorId` (quầy/STAFF/bàn 101). Khi scene reload, reconstruct transform từ anchor + slot. Nếu muốn cho drop tự do mọi nơi phải có schema, collider/rollback và navmesh riêng — chưa nên làm trong vertical slice.

**Phân quyền các service (không để UI tự sửa state):**
- `BackpackCatalog`: duyệt ID/item capability, nguồn và rule layer; không chấp nhận string từ UI không đăng ký.
- `InventoryService.TryAcquire/Equip/Stow/Place/Use/Combine`: kiểm khả năng, item owner, target, trạng thái, vị trí, layer, gate; tạo candidate state.
- `WorldUseResolver`: map `item + world target` sang một hành động có tác dụng; ví dụ E10a/chìa → GATE-CONG, tờ tài liệu B → clip desk, F101 anchored → form action. Từ chối item sai có phản hồi diegetic (không để “Use failed” vô nghĩa).
- `PhysicalInspector`: vẫn là presenter cho paper/model 3D, phát callback inspected detail; **không tự cấp item**.
- `HypothesisService/FormService/Casework`: authoritative cho argument/F101/evidence; `BackpackView` chỉ request action, không trực tiếp `evidence.Add`.
- `RealityTransaction/AtomicSaves`: commit action & save atomically, xử lý preflight/rollback, reconcile LayerBound khi R/B/M swap; idempotent once.

**Thứ tự hành động mẫu:** raycast world object → inspect/choose pickup → registry validate → tạo `candidate = State.Copy()` → gắn BackpackEntry (và sự kiện canon nếu đúng) → durable save thành công → world object ẩn/đổi pose + update UI; save fail → không nuốt đồ. Khi equip/use: validate target và item current, plan transition → durable commit → animation/response; retry cùng instance không tạo vật trùng. Khi xem lại 3D trong ba lô, stage ghi nhận **inspection** nếu có hotspot mới, không cấp lại nguồn.

### 5.1. Schema v6 → v7, tương thích bản lưu

- **Migration phải đọc v1–6** rồi tạo item instance deterministic từ các event/evidence có thật. `E10a` đã có trong `state.evidence` ngay từ `Investigation.New()`: chỉ sinh `BackpackEntry` cầm phong bì nếu có **dấu thật đã nhặt/đọc**, không nhân đôi E10a. Với save trước khi thấy phong bì, vẫn thấy đồ tại LOC01.
- Vật chứng R đã lấy (ví dụ `E11.drawing.1/2/3`, `E11.wristband`, `E11.transferNote`) reconstruct từ `evidence` và `chapter/events` tương ứng. Không tự cấp phần chưa lấy.
- B `OBS101B/OBS101C/OBS_SHIFT_LOG` từng `Examined` → dựng **reference đã đọc**; không tự giả định đã “nhặt bản gốc”. `OBS101A` và hiện trường cố định mãi là observation-only.
- Persist `itemInstanceId` stable, unique once, no duplicate on reload or repeated pickup; bound paper reconciles on layer transaction. F101 `firstDecision`, `revision`, marks/links, transcript không đổi.
- Invalid/malformed entries bị chặn hoặc fallback backup valid, có thông báo; không âm thầm xóa `evidence`. Migration v7 phải được kiểm bằng đối chiếu snapshot trước/sau và theo các save legacy v1–6.
- Không bỏ `journalNotes`; chuyển chúng thành chữ viết trong tập ghi chú của ba lô mà giữ nguyên source id, loại note và giới hạn hiện hành.

## 6. Tương tác 3D chi tiết & quy tắc cho puzzle

**World prompt theo ngữ cảnh, không chồng nút:**
- Nhìn giấy/đồ ở gần: `E xem` → `Giữ/cất` (nếu được stow) hoặc `Đặt lại`; lấy vào túi chỉ khi người chơi xác nhận. Ngoài phạm vi tầm tay không có thu thập từ xa.
- Mở túi: `Tab` → chọn món → `Soi 3D` hoặc `Cầm ra`; nhấn phải/Space lật giấy, WASD/chuột xoay, cuộn zoom, T nghiêng đón sáng. Trợ năng giống inspector hiện tại.
- Đang cầm vật: hiện target highlight **chỉ khi item hợp lệ**, `E dùng` trên world target. Nếu target liên quan suy luận, hỏi người chơi muốn **so sánh** hay **gắn kết luận**, không auto hoàn thành. `Esc` cất lại an toàn, `Q` đổi quick item nếu có.
- `Đặt lên bàn`: anchor có ít nhất hai slot; item vẫn thuộc cùng bản gốc, không nhân bản mesh hay evidence. Khi rời room/scene load, item ở slot đúng; nếu player không thu lại **critical item**, game hỗ trợ chọn “Tìm đồ đã đặt” trong túi, tuyến tới bàn còn mở.
- `Ghép`: chỉ cho authored pairs, ví dụ CH02 ba mảnh tranh E11 có thể thử ghép trên bàn/kẹp thành phần mềm ghép tranh, vẫn dùng ID drawing fragments và gate E11 của Core. Không thêm crafting tùy ý hay bùa chú mới.

**Ví dụ CH01 F101 có thao tác thật (vẫn ba nhánh):**
- **B04:** kéo rèm → đứng vào góc nhìn để thấy vết bánh OBS101A → nhấc thẻ AS-05B trên giá đầu giường → lật/soi vùng chữ ký thiếu → tạm cất tài liệu B vào túi; optional lấy phiếu chuyển giường 2 để so.
- **B05:** người chơi đem thẻ ra đặt lên bàn so với F101 (bản F101 vẫn **anchored** bảng kẹp AN02), chỉ vào số 0 và ô chữ ký, đối chiếu lại vùng dấu bánh trong phòng; ghi lập luận trong tập ghi chú hoặc bằng interaction “Liên hệ dấu hiện trường”; chọn A/B/C qua **bảng kẹp trong thế giới**. `HypothesisService` vẫn nhận IDs thật; không phải kéo hai card giả để được pass.
- **B06:** khi B→R, các giấy B tạm cất trở thành **bản nhớ tham khảo**, người chơi không thể lôi AS-05B “bản gốc 2005” ra dùng ở LOC03 R; thông tin đã xem vẫn có. Điều này củng cố sự bất nhất, không trừng phạt việc đã thu thập.
- **B08:** người chơi mở túi nhìn E10a và tập ghi chú; quay lại Phòng 101 R ở CH02 để tìm **bằng chứng vật lý độc lập**.

### 6.1. Chống giải đố “đặt đồ đúng chỗ”

Mỗi puzzle phải trả lời tối thiểu 3 câu: (1) **Tại sao Minh muốn thử thao tác này?** (2) **Phản hồi nào làm thay đổi cách hiểu hiện trường?** (3) **Nếu thất bại hoặc không nhận ra, có dấu hiệu nào để thử lại?**. Nếu “dùng chìa với khóa” chỉ để kéo dài 20 giây và không có ý nghĩa, đưa nó vào animation tự nhiên không cần minigame. `ItemUse` không tạo ra một cơ chế tăng số lần bấm không có chiều sâu.

## 7. Mapping CH01 B01–B08 sang gameplay mới

| Beat | Đồ vật / hành động thật | Học được gì? | Scare / payoff | Không được phá |
| --- | --- | --- | --- | --- |
| **B01** | Cầm **E10a**, mở phong bì 3D, đọc SMS/mẩu báo, cất vào túi, lấy chìa được ủy quyền và dùng tại cổng (hoặc fallback tương tác cổng theo event legacy). | Bảy hoãn, Minh tự đến, 13 tử vong công khai. | Một hành động vật lý lặp chủ đề trách nhiệm: tự mở cổng. | E10a vốn seeded trong evidence; không add 2 lần. |
| **B02** | Tới ô kính, chạm/nhìn thay góc để kiểm bóng; ba lô **không** chen popup. | Người trong quầy không khớp ngữ cảnh nơi bỏ hoang. | Dread từ điều nhìn thấy, không auto pickup “Lan”. | R→B vẫn ở AN01; không auto cinematic. |
| **B03** | Giở sổ AN01 chuyển lớp; Lan nhận biết Minh → E/K bắt đầu cinematic; tự cầm **BOARD/order** và cất vào ngăn hồ sơ. | Lan nói ca 2005, Minh sống trong 2012. | Ấm áp để B06 vắng mặt đau hơn. | CH01-T03/T05, guard thoại, không auto item OBS. |
| **B04** | Mở DOOR-101, kéo CURTAIN-101, quan sát **OBS101A** ở sàn; nhấc/soi/lật/cất tạm **AS-05B**; có thể tìm C/D/STAFF đưa ra bàn so. | Dấu vật lý không khớp số 0; chữ ký bàn giao thiếu. | Dấu bánh và tiếng cáng trở nên có nghĩa. | A/B không cần thu đủ thẻ; C vẫn requires A+B đúng. |
| **B05** | Lấy thẻ ra, đặt cạnh F101, so lại dấu sàn; cầm bút/đánh dấu trực tiếp trên F101 anchored, ký A/B/C. | Chỉ một giấy ký có thể biến lớp B, không sửa lịch sử quan sát. | Scare phản hồi F101 và overprint *presentation-only*. | G01 A/B/C; D; first/current/revision; no physical B evidence. |
| **B06** | Thu thẻ khỏi bàn hoặc để trên bàn, về AN01 bàn giao; B giấy không còn vật lý khi swap B→R, nhưng có reference. | Lan vắng; source hiện trường R độc lập. | Không phải mất đồ thật: nhìn được “ký ức giấy” mà không thể dùng nó làm chứng cứ xuất. | CH01-T08, rollback, no R geometry changes. |
| **B07** | Dùng tay thử cổng; ba lô không giải seam bằng một vật phẩm tình cờ. | Không thể rời câu hỏi bằng một thao tác cơ học. | Uncanny Walk sau seam. | T09 once, accessibility, path tới AN01. |
| **B08** | Mở túi kiểm **E10a + note của Minh**, chọn đóng túi và giở sổ AN01 để sang CH02. | Mục tiêu quay lại 101 R do nghi vấn có thể kiểm vật lý. | Tiếng giấy/đồ vật và khoảng lặng thay màn tổng kết UI. | CH01-T10; CH02 giữ LIST/SOCKETS/WHEELS/E11/E12. |

## 8. Rollout sang các chương sau

- **CH02:** lấy và soi vật chứng thật `E11.drawing.1/2/3`, vòng tay, transfer note, `E12`; ghép tranh ở mặt bàn vật lý; vẫn đạt `G02` chỉ sau khi đúng bundle và xác thực C01 theo hợp đồng. Nếu mất kết nối, không làm rơi vật chứng quan trọng xuống geometry.
- **CH03–CH04:** hồ sơ, cassette, thẻ và vật cá nhân dùng `BackpackEntry` đúng origin; một cassette có thể được đặt vào máy phát **đúng lúc** thay vì phát VO tự động qua UI, nhưng thoại/guard H05/PT02 giữ nguyên.
- **CH05–CH06:** tool/use vật lý nối nguồn `E01–07/E09/E10`, bằng chứng nguồn xuất vẫn do EvidenceRegistry/canon quyết định, không cho tờ bản chụp B thành bản chính.
- **CH07–CH08:** ký ức `M` vào ngăn **ký ức tham khảo phi vật chất**, E08 nonexport; gói hồ sơ xuất tạo từ chứng cứ R có provenance, xác nhận lần cuối và bốn ending giữ nguyên.

Không rollout đồng thời cả tám chương. Bắt đầu vertical slice **B01 E10a + B04 AS-05B + B05 F101 + B06 R/B**, sau đó mới CH02 E11/E12 để chứng minh portable/anchored/LayerBound/PhysicalExportable hoạt động đúng.

## 9. Tech implementation slices / dependency

| Stage | Task đề xuất | Definition of done / QA |
| --- | --- | --- |
| **BP-00** | Design review & trace Core 01/02/04/06/07 + DEC | Quyết định thuộc source of truth được duyệt; provenance & item classes; không lệch gate. |
| **BP-01** | Data registry và `InventoryService`; schema v7 + migrate v1–6 | Unity domain tests acquired/equipped/placed/duplicate/missing; save fail rollback; E10a unchanged evidence count. |
| **BP-02** | `BackpackView` 3D/2.5D, bốn ngăn, item select, quick slots | Mouse/keyboard/full controller fallback; Escape restore camera/input; no doc lost. |
| **BP-03** | Reuse `PhysicalInspector` cho paper & prefabs; supports take out + stow | Flip/tilt/zoom/large print, stress 100 cycles no meshes/materials leak; avoids dup observation. |
| **BP-04** | `WorldUseResolver` + item placement anchors + 2-doc comparison table | 3D camera, F101 marks/links valid; inspect source then use at world target; invalid target feedback. |
| **BP-05** | LayerBound B→R transaction & prefab presentation | 30 swaps / fail preflight / load during held item; no B original exported; R independent. |
| **BP-06** | CH01/CH02 authoring and migration of UI | Route B01–B08, C01 CH02, all A/B/C, optional EX01; evidence count, G01/G02, lore guards. |
| **BP-07** | Accessibility/playtest/performance | TASK010 6 new players + expert TASK026–028 **pending**; 1080p Medium profiling, logging; compare old-vs-new completion time, manual actions, player understanding and frustration. |

**Critical path:** BP-00 → BP-01 → BP-02/BP-03 → BP-04 → BP-05 → BP-06 → BP-07. Nếu muốn prototype không chỉnh save ngay: làm **nonpersistent mock** của một giấy + một item trên scene sandbox, không gắn gate; chỉ khi UX đạt mới triển khai BP-01/v7 thật. Không đánh tráo mock là build hoàn chỉnh.

## 10. QA adversarial & trải nghiệm (đều CHƯA CHẠY)

1. B01 lấy E10a 2 lần / load trước nhặt → 1 instance, evidence vẫn 1; bấm cổng đúng sau sự kiện canon; có key in-hand hoặc fallback.
2. TAB/J bag mở rồi Escape, inspector, rotate, flip/rake, large print, alt E/K và input remap → không kẹt camera/cursor; không “nhìn vật từ xa” để auto lấy.
3. Tại CH01 ký **Affirmed ngay** không A/B → G01 mở; bag không tạo “Refuted” tự động khi stow thẻ.
4. Ký **Unverified ngay** → G01 mở, D=0, vẫn có đường cứu nhận định sau này.
5. Ký **Refuted thiếu A/B**, chỉ xem thẻ hoặc chỉ xem sàn, link sai, claim vượt chứng cứ → bị từ chối đúng; không tăng D/không nuốt đồ.
6. Refuted hợp lệ qua cầm giấy + soi mặt sau + đặt lên bàn + đánh dấu trong thế giới + so OBS101A → valid; `firstDecision` và `revision` giữ khi sửa A/B/C.
7. Cất AS-05B trong B, cầm trên tay khi T08, đặt ở bàn STAFF hoặc phòng 101 rồi swap → không mang bản giấy B vào R; reference vẫn xem được, không dùng với vật trong R; load/cancel swap nhất quán.
8. `OBS101C/D`, `OBS_SHIFT_LOG`, EX01/03:17 optional không tự mở bằng bag; `OBS101B` không lộ An/C01/Khu C.
9. CH02 `E11` từng mảnh thiếu 1, wristband/transferNote và `E12` → G02 chưa mở; đủ bundle mới mở; không có duplicate hoặc item rơi ngoài bản đồ.
10. Đặt item lên bàn → rời phòng/load/backtrack/revisit → item vẫn đúng anchor, không nhân đôi, có cách lấy lại dù prop bị cản.
11. CH03–06 dùng item ở layer/target sai → không gây tác dụng side effect, không phát thoại chưa unlock. E08 và vật thuộc M không export.
12. UAT không dùng chuột chính xác: keyboard-only, reducedMotion, monoAudio, caption có hướng, large print; chứng cứ không lệ thuộc model 3D cực nhỏ.
13. Chế độ mute, giảm jumpscare, load khi scare once đang queue → item interactions/gates vẫn bình thường.
14. Save lỗi, backup restore, migrated v1–6, invalid instanceIds, equipped missing item, scene unload, swap rollback và 30 lần stress transitions → không mất chứng cứ/trạng thái F101.
15. So hiệu quả với CH01-R hiện tại: **tỷ lệ người chơi tự lấy món từ túi để dùng**, số lần nhìn/đọc theo chủ đích, câu hỏi điều tra, việc thiếu UI auto answer; hỏi “Vì sao mày vừa lấy món đó ra?”; nếu không trả lời được thì item use chỉ là click padding.

## 11. Tác động tài liệu & giới hạn giao PR

- **Nơi chủ quản cần đồng bộ khi approved:** Core 01 §2 loop/UX; Core 04 CH01 B01–B08 và CH02 item route; Core 06 schema/event/save contracts; Core 07 `Events`, `Puzzle specs`, `Assets`, `QA`, `Roadmap`, `Decisions`, `Changelog`; Core 02/05 nếu có text/asset mới. Đây là một quyết định gameplay lớn, **không tự cập nhật Core mà không chốt**.
- Source baseline đọc trực tiếp từ GitHub: `AGENTS.md`, `docs/ch01r/puzzle-dependency-graph.md`, `docs/ch01r/acceptance.md`, `HospitalGame.Journal.cs`, `HospitalUI.Journal.cs`, `PhysicalInspector.cs`, `HospitalGame.Pages.cs`, `HospitalGame.cs`, `Casework.cs`, `DeductionFramework.cs`, `Protocol.cs`.
- Không tuyên bố Unity code/build đã được cập nhật, test đã PASS, schema v7 hiện có hoặc task review đã đóng. **Bản này là đặc tả để duyệt và thực thi theo thứ tự, không phải gameplay vận hành.**

## 12. Thẩm định Walkthrough Backpack & Exploration-First v2.0 do người dùng cung cấp (23/11/2026)

**Kết quả:** có nhiều ý tưởng sáng về tương tác thật, ba ngăn UI, cầm chìa, phản ứng âm thanh với cử chỉ và bất thường trên vật phẩm. Không nên lấy nguyên bản vì có những chỗ trái luật F101/canon hiện tại.

| Cơ chế v2.0 bên ngoài | Quyết định cho bản này | Contract triển khai |
| --- | --- | --- |
| Ba ngăn ba lô thay bốn nhóm | **Nhận** | Ba vùng hiển thị: Dụng cụ & đồ vật / Giấy tờ & vật chứng / Pocket Note. Metadata sở hữu/provenance/evidence vẫn tách ở backend; Pocket Note 1–2 dòng là lời nhắc, không xóa tập note đầy đủ. |
| Rút hai chìa từ E10a | **Nhận có điều kiện** | `HospitalGame.Pages.cs::Letter()` đã ghi *chìa cổng phụ và chìa khu C đã thay khóa*. Cho người chơi rút chìa và dùng chìa **cổng phụ** trên `GATE-CONG`, nhưng T02 vẫn cần sự kiện đã đọc E10a/SMS; item thứ hai giữ cho chương sau. Không tạo E10a thứ hai (đã seeded). |
| Thử chìa khu C ở cửa LOC04 ngay B04, nghe khóa không vừa | **Không áp dụng CH01** | CH01 không nên đưa người chơi tới cửa khu C hoặc tuyên bố ổ khóa đã thay **một lần nữa** trái với copy chìa khóa ở E10a; giữ bí ẩn cho CH02+. |
| Cầm cả F101 từ quầy và cất vào túi | **Không đồng nhất BOARD và F101** | Runtime `BOARD` nhận ở AN01/T05, còn `F101` được ký trên bảng kẹp vật lý AN02 Phòng 101. Giữ F101 anchored; vật cầm/đặt chỉ hỗ trợ so sánh và đánh dấu, không dịch vị trí ký. |
| Kẹp AS-05B lên F101 đủ để ký C | **Lấy cử chỉ, không auto-pass** | Kẹp giấy là thế giới 3D **biểu đạt một liên kết nguồn**; vẫn cần OBS101A đã Examined + OBS101B, mark số 0 và ô người nhận thiếu, link tới traces, claim “chưa đủ căn cứ xác nhận số 0” qua `HypothesisService`. Nhánh A/B không cần thẻ. |
| Bốn công tắc và bóc băng | **Chưa duyệt** | Không suy ra bốn công tắc từ bốn **ổ cắm** CH02; cần Core 03/04 và kiểm nghiệm chuyên gia nếu làm. Dấu sàn/rèm/giường là spatial deduction ưu tiên. |
| Lan không có bóng phản chiếu, chuyển B03 100% FPS | **Giữ mơ hồ và giữ hybrid** | Lan phát hiện Minh qua scripted event, player chủ động bắt chuyện, hội thoại điện ảnh ngắn, lại FPS. Có thể thử sai lệch phản chiếu **bị ánh sáng che**, không xác nhận ma; không auto-start 6 giây. |
| Hai âm scare và “tắt về 0 dB 2,5 giây” | **Chỉnh kỹ thuật** | Dùng ducking theo cue và âm có hướng, không biến “0 dBFS” thành im lặng hoặc cố định mọi khoảng nghỉ, trợ năng caption/mono/reduceMotion. |
| F101 và AS-05B vẫn là giấy **thật** trong ba lô ở R | **Không áp dụng vật lý xuyên lớp** | B→R trả giấy B về anchor, giữ **memory reference**. Thử `P-BAG08_ECHO`: hình bóng giấy thoáng trên vải túi hoặc hình lưu thị giác chậm biến mất khi mở lại; presentation-only, không equip/use/export, không evidence mới. Nếu muốn giấy B vật lý tồn tại ở R, cần một DEC/canon change riêng, không lén thay luật. |
| Tính pacing “18–22” | **Không nhận như kết quả test** | Cộng bảng nguồn: min **21**, max **26,5** phút, không phải 18–22. Bản CTCC hiện giữ giả thuyết mainline **20,5–30** và chỉ sửa từ TASK010 thực tế. |

**Ưu tiên mẫu triển khai sau review:** (1) E10a → rút chìa cổng → equip/use tại GATE-CONG và save/load idempotent. (2) OBS101B portable B → kẹp vào ô đối chiếu gần F101 anchored → đánh dấu/link/suy luận đúng qua solver → tại R chỉ còn reference + echo thị giác tùy chọn. **Chưa chạy build/test và chưa đăng ký P-BAG08_ECHO.**

**Playtest bổ sung:** “Bạn lấy chìa ra nhằm kiểm chứng hay mở điều gì?”, “Tại B05, thao tác kẹp thẻ khiến bạn hiểu dấu sàn hơn hay chỉ thử mò?”, “Khi chỗ giấy trong túi trống đi ở B08, bạn nghĩ điều gì xảy ra?”. Không giả định người chơi phải sợ từ một hiệu ứng đồ vật.
