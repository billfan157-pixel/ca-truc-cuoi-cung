using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CTCC
{
    public sealed partial class HospitalGame
    {
        private HandItemPresenter handPresenter;
        private BackpackStage3D backpackStage;
        private BackpackCompartment backpackCompartment = BackpackCompartment.Tools;
        private string backpackSelection;
        private bool showingBag08EchoThisOpen;
        private const string PlacedLocateKey = "__PLACED_LOCATE__";

        private void OpenBackpack(BackpackCompartment? compartment = null)
        {
            bool openingFresh = !ui.IsModal;
            if (openingFresh)
            {
                scene.atmosphere?.From("bagZip", player.Eye.transform.position + player.Eye.transform.forward * 0.3f, 0.55f);
                showingBag08EchoThisOpen = false;
            }
            if (compartment.HasValue)
                backpackCompartment = compartment.Value;
            else if (openingFresh)
                backpackCompartment = DefaultBackpackCompartment();

            DrawBackpack(openingFresh);
        }

        private BackpackCompartment DefaultBackpackCompartment()
        {
            if (State.chapter == "CH01" && !State.Happened("CH01-T02"))
                return BackpackCompartment.Tools;
            if (State.chapter == "CH01" && State.Happened("CH01-T08"))
                return BackpackCompartment.PocketNote;
            return BackpackCompartment.Papers;
        }

        private bool ShouldTriggerBag08Echo() =>
            State != null &&
            State.chapter == "CH01" &&
            State.layer == Reality.R &&
            State.Happened("CH01-T08") &&
            !State.HasDreadEvent(DreadCatalog.Bag08Echo) &&
            HypothesisService.HasExaminedInLayerB(State, ObservationRegistry.Bed4Card);

        private void RefreshQuickSlotHud(Investigation current)
        {
            if (ui == null || current == null) return;
            var equipped = InventoryService.GetEquipped(current);
            var list = new List<(string label, bool equipped)>();
            if (current.quickSlots != null)
            {
                for (int i = 0; i < current.quickSlots.Count && list.Count < InventoryService.MaxQuickSlots; i++)
                {
                    string id = InventoryService.GetQuickSlotItem(current, i);
                    if (string.IsNullOrEmpty(id)) continue;
                    var def = BackpackCatalog.Get(id);
                    if (def == null) continue;
                    bool isEq = equipped != null && equipped.itemDefId == id;
                    list.Add((def.ShortLabel, isEq));
                }
            }
            ui.SetQuickSlots(list);
        }

        private void ActivateQuickSlot(int slotIndex)
        {
            if (State == null || busy) return;
            string id = InventoryService.GetQuickSlotItem(State, slotIndex);
            if (string.IsNullOrEmpty(id)) return;
            var equipped = InventoryService.GetEquipped(State);
            bool alreadyEquipped = equipped != null && equipped.itemDefId == id;
            string nextId = alreadyEquipped ? "" : id;
            var def = BackpackCatalog.Get(id);
            SaveCheckpoint(() => casework.EquipItemAsync(nextId), () =>
            {
                string sfx = nextId is (BackpackCatalog.GateKey or BackpackCatalog.WingCKey) ? "keyJingle"
                    : string.IsNullOrEmpty(nextId) ? "bagZip" : "paper";
                scene.atmosphere?.From(sfx, player.Eye.transform.position + player.Eye.transform.forward * 0.3f, 0.42f);
                if (Options.captions && def != null)
                    ui.Caption(alreadyEquipped ? $"Đã cất {def.ShortLabel} về ba lô." : $"Đang cầm {def.ShortLabel} trên tay.", 2.2f);
            });
        }

        private void CycleQuickSlot()
        {
            if (State?.quickSlots == null || State.quickSlots.Count == 0 || busy) return;
            var validIds = new List<string>();
            for (int i = 0; i < State.quickSlots.Count; i++)
            {
                string id = InventoryService.GetQuickSlotItem(State, i);
                if (!string.IsNullOrEmpty(id)) validIds.Add(id);
            }
            if (validIds.Count == 0) return;

            var equipped = InventoryService.GetEquipped(State);
            int curIdx = equipped != null ? validIds.IndexOf(equipped.itemDefId) : -1;
            string nextId = curIdx + 1 < validIds.Count ? validIds[curIdx + 1] : "";
            SaveCheckpoint(() => casework.EquipItemAsync(nextId), () =>
            {
                string sfx = nextId is (BackpackCatalog.GateKey or BackpackCatalog.WingCKey) ? "keyJingle"
                    : string.IsNullOrEmpty(nextId) ? "bagZip" : "paper";
                scene.atmosphere?.From(sfx, player.Eye.transform.position + player.Eye.transform.forward * 0.3f, 0.40f);
            });
        }

        private int QuickSlotNumber(string itemDefId)
        {
            if (State?.quickSlots == null || string.IsNullOrEmpty(itemDefId)) return -1;
            int idx = State.quickSlots.IndexOf(itemDefId);
            return idx >= 0 ? idx + 1 : -1;
        }

        private void AddQuickSlotAction(List<ActionChoice> actions, BackpackItemDef def, BackpackEntry entry)
        {
            if (def == null || !def.CanEquip || entry == null || entry.status == BackpackItemStatus.MemoryRef)
                return;
            if (def.Kind == ItemKind.LayerBoundPaper && State.layer != Reality.B)
                return;
            int slotNum = QuickSlotNumber(def.Id);
            string label = slotNum > 0 ? $"Gỡ khỏi ô nhanh [{slotNum}]" : "Ghim vào ô nhanh (1–4)";
            actions.Add(new ActionChoice(label, () =>
                SaveCheckpoint(() => casework.ToggleQuickSlotAsync(def.Id), () => DrawBackpack())));
        }

        private void BuildPlacedLocateDetail(List<BackpackEntry> placed, out string title, out string body, List<ActionChoice> actions)
        {
            title = $"Tìm đồ đã đặt ({placed.Count})";
            var sb = new StringBuilder();
            sb.AppendLine("Danh sách vật phẩm hoặc giấy tờ Minh đang đặt trên bàn hoặc kẹp trong hiện trường:");
            sb.AppendLine();
            for (int i = 0; i < placed.Count; i++)
            {
                var p = placed[i];
                var def = BackpackCatalog.Get(p.itemDefId);
                string anchorLabel = WorldAnchors.Label(p.placedAnchorId);
                sb.Append("• ").Append(def?.Title ?? p.itemDefId)
                  .Append("\n   Vị trí: ").AppendLine(anchorLabel);
                string anchorId = p.placedAnchorId;
                string shortLabel = def?.ShortLabel ?? p.itemDefId;
                actions.Add(new ActionChoice($"Thu lại {shortLabel} về ba lô", () =>
                    SaveCheckpoint(() => casework.RetrieveItemFromAnchorAsync(anchorId), () =>
                    {
                        scene.atmosphere?.From("paper", player.Eye.transform.position + player.Eye.transform.forward * 0.3f, 0.48f);
                        handPresenter?.SyncState(State, scene, hideViewModel: true);
                        ui.Caption($"Đã thu {shortLabel} từ {anchorLabel} về lại ba lô.", 3.4f, aboveModal: true);
                        if (InventoryService.GetAllPlaced(State).Count == 0) backpackSelection = null;
                        DrawBackpack();
                    })));
            }
            sb.AppendLine();
            sb.Append("Không vật phẩm điều tra nào có thể bị thất lạc vĩnh viễn: có thể thu lại từ xa ngay tại đây hoặc đến trực tiếp bàn/kẹp tương ứng.");
            body = sb.ToString();
        }

        private void DrawBackpack(bool animate = false)
        {
            InventoryService.EnsureDefaultTools(State);
            var entries = new List<NotebookEntry>();
            var placedItems = InventoryService.GetAllPlaced(State);
            string emptyCopy;
            string title;
            string body;
            Texture image = null;
            var actions = new List<ActionChoice>();
            bool triggerEcho = false;

            if (backpackCompartment == BackpackCompartment.Tools)
            {
                emptyCopy = "Chưa có dụng cụ nào khác.";
                foreach (var item in State.backpackItems)
                {
                    var def = BackpackCatalog.Get(item.itemDefId);
                    if (def == null || def.Compartment != BackpackCompartment.Tools) continue;
                    string badge = item.status switch
                    {
                        BackpackItemStatus.Equipped => " [Đang cầm]",
                        BackpackItemStatus.Placed => " [Đang đặt]",
                        _ => ""
                    };
                    if (def.Id == BackpackCatalog.Flashlight)
                        badge = player != null && player.IsTorchOn ? " [Đang bật]" : " [Đang tắt]";
                    int qs = QuickSlotNumber(def.Id);
                    if (qs > 0) badge += $" · #{qs}";
                    string id = item.itemDefId;
                    entries.Add(new NotebookEntry(def.ShortLabel + badge, () =>
                    {
                        backpackSelection = id;
                        DrawBackpack();
                    }));
                }

                var selectedEntry = InventoryService.FindByDef(State, backpackSelection);
                var selectedDef = selectedEntry != null ? BackpackCatalog.Get(selectedEntry.itemDefId) : null;
                if (selectedDef == null || selectedDef.Compartment != BackpackCompartment.Tools)
                {
                    selectedDef = BackpackCatalog.Get(BackpackCatalog.Flashlight);
                    selectedEntry = InventoryService.FindByDef(State, BackpackCatalog.Flashlight);
                    backpackSelection = BackpackCatalog.Flashlight;
                }

                title = selectedDef.Title;
                var sb = new StringBuilder(selectedDef.ProvenanceLabel).Append("\n\n").Append(selectedDef.Description);
                if (selectedDef.Id == BackpackCatalog.Flashlight)
                {
                    sb.Append("\n\nPin còn lại: ").Append(Mathf.RoundToInt(Mathf.Clamp01(player.BatterySeconds / 360f) * 100f)).Append("%.");
                    actions.Add(new ActionChoice(player.IsTorchOn ? "Tắt đèn pin" : "Bật đèn pin", () =>
                    {
                        player.SetTorchEnabled(!player.IsTorchOn);
                        scene.atmosphere?.From("doorLatch", player.Eye.transform.position, 0.45f);
                        DrawBackpack();
                    }));
                }
                else if (selectedDef.Id == BackpackCatalog.MinhPhone)
                {
                    sb.Append(State.Happened(E10aParts.PostponementSms)
                        ? "\n\n✓ Đã đọc tin nhắn báo hoãn (22:48) của chú Bảy."
                        : "\n\n• Có 2 tin nhắn từ chú Bảy chưa mở đọc (16/11 và 22:48).");
                    if (State.Happened(E10aParts.PhoneCalledBay))
                        sb.Append("\n✓ Đã thử gọi chú Bảy (không bắt máy).");
                    actions.Add(new ActionChoice("Mở điện thoại (SMS & Gọi chú Bảy)", () =>
                    {
                        backpackStage?.Close();
                        OpenPhone(0, returnToBackpack: true);
                    }));
                    actions.Add(new ActionChoice("Xem SMS hẹn hôm 16/11", () =>
                    {
                        backpackStage?.Close();
                        OpenPhone(1, returnToBackpack: true);
                    }));
                }
                else
                {
                    if (selectedEntry != null && selectedEntry.status == BackpackItemStatus.Placed)
                    {
                        sb.Append("\n\nĐang đặt tại: ").Append(WorldAnchors.Label(selectedEntry.placedAnchorId)).Append('.');
                        string anchorId = selectedEntry.placedAnchorId;
                        actions.Add(new ActionChoice("Thu lại về ba lô", () =>
                            SaveCheckpoint(() => casework.RetrieveItemFromAnchorAsync(anchorId), () =>
                            {
                                scene.atmosphere?.From("paper", player.Feet + Vector3.up, 0.48f);
                                handPresenter?.SyncState(State, scene, hideViewModel: true);
                                DrawBackpack();
                            })));
                    }
                    bool equipped = selectedEntry != null && selectedEntry.status == BackpackItemStatus.Equipped;
                    if (selectedDef.CanEquip)
                    {
                        actions.Add(new ActionChoice(equipped ? "Cất lại vào ba lô" : "Cầm ra tay", () =>
                        {
                            string nextEquip = equipped ? "" : selectedDef.Id;
                            SaveCheckpoint(() => casework.EquipItemAsync(nextEquip), () =>
                            {
                                handPresenter?.SyncState(State, scene, hideViewModel: false);
                                if (!string.IsNullOrEmpty(nextEquip)) CloseModal();
                                else DrawBackpack();
                            });
                        }));
                        AddQuickSlotAction(actions, selectedDef, selectedEntry);
                    }
                    var tmpl = (!string.IsNullOrEmpty(selectedDef.InspectModelId) ? scene.InspectTemplate(selectedDef.InspectModelId) : null)
                               ?? handPresenter?.GetInspectTemplate(selectedDef.Id);
                    if (tmpl != null && inspector != null && qaFolder == null)
                    {
                        actions.Add(new ActionChoice("Soi 3D", () => InspectBackpackTool3D(selectedDef, tmpl)));
                    }
                }
                body = sb.ToString();
            }
            else if (backpackCompartment == BackpackCompartment.Papers)
            {
                if (!showingBag08EchoThisOpen && ShouldTriggerBag08Echo())
                {
                    showingBag08EchoThisOpen = true;
                    triggerEcho = true;
                    SaveCheckpoint(() => casework.RecordDreadEventAsync(DreadCatalog.Bag08Echo));
                }

                emptyCopy = "Chưa cất tờ giấy hoặc vật chứng nào trong ba lô.";
                foreach (var item in State.backpackItems)
                {
                    var def = BackpackCatalog.Get(item.itemDefId);
                    if (def == null || def.Compartment != BackpackCompartment.Papers) continue;
                    string tag = def.Kind == ItemKind.MemoryOnly
                        ? " [Ký ức lớp M · Không xuất]"
                        : item.status switch
                        {
                            BackpackItemStatus.MemoryRef => " [Ký ức · Nguồn B]",
                            BackpackItemStatus.Placed => " [Đang đặt]",
                            BackpackItemStatus.Equipped => " [Đang cầm]",
                            _ => def.Kind == ItemKind.PersistentPhysical ? " [Vật chứng R]" : " [Giấy lớp B]"
                        };
                    int qs = QuickSlotNumber(def.Id);
                    if (qs > 0) tag += $" · #{qs}";
                    string id = item.itemDefId;
                    entries.Add(new NotebookEntry(def.ShortLabel + tag, () =>
                    {
                        backpackSelection = id;
                        DrawBackpack();
                    }));
                }

                if (placedItems.Count > 0)
                {
                    entries.Add(new NotebookEntry($"Tìm đồ đã đặt ({placedItems.Count})", () =>
                    {
                        backpackSelection = PlacedLocateKey;
                        DrawBackpack();
                    }));
                }

                if (backpackSelection == PlacedLocateKey && placedItems.Count > 0)
                {
                    BuildPlacedLocateDetail(placedItems, out title, out body, actions);
                }
                else
                {
                    var selectedEntry = InventoryService.FindByDef(State, backpackSelection);
                    var selectedDef = selectedEntry != null ? BackpackCatalog.Get(selectedEntry.itemDefId) : null;
                    if (selectedDef == null || selectedDef.Compartment != BackpackCompartment.Papers)
                    {
                        selectedEntry = State.backpackItems.Find(e => BackpackCatalog.Get(e.itemDefId)?.Compartment == BackpackCompartment.Papers);
                        selectedDef = selectedEntry != null ? BackpackCatalog.Get(selectedEntry.itemDefId) : null;
                        backpackSelection = selectedDef?.Id;
                    }

                    if (selectedDef == null)
                    {
                        title = "Ngăn giấy tờ & vật chứng";
                        body = "Những tài liệu Minh tự nhặt hoặc tạm cất sẽ nằm ở đây.\n\n" +
                               "• Vật chứng thực thể lớp R được lưu bền vững (Chương 1 chỉ có E10a).\n" +
                               "• Giấy tờ nhặt trong lớp B chỉ tồn tại vật lý trong ca trực 2005; khi trở về hiện thực R sẽ chuyển thành ghi nhớ nguồn đã kiểm.";
                    }
                    else
                    {
                        title = selectedDef.Title;
                        image = Image(selectedDef.FrontTexture);
                        bool isMemoryOnly = selectedDef.Kind == ItemKind.MemoryOnly;
                        bool isMemory = isMemoryOnly || selectedEntry.status == BackpackItemStatus.MemoryRef;
                        var sb = new StringBuilder();
                        sb.Append(isMemoryOnly
                            ? selectedDef.ProvenanceLabel
                            : isMemory
                                ? $"Ký ức đã kiểm · {selectedEntry.originLoc} / Lớp B / 17-11-2005"
                                : selectedDef.ProvenanceLabel);
                        sb.Append("\n\n").Append(selectedDef.Description);

                        if (isMemoryOnly)
                        {
                            sb.Append("\n\nBản chất lớp M: đây là vết ký ức phi vật chất — không có bản giấy để cầm ra tay, đặt lên bàn hay xuất vào gói vật chứng.");
                        }
                        else if (isMemory)
                        {
                            sb.Append("\n\nTờ giấy này chỉ tồn tại trong lớp B (2005). Trong hiện thực năm 2012 không có bản vật lý để cầm hay xuất làm vật chứng — chỉ còn ghi nhớ về những gì Minh đã tận mắt đọc.");
                        }
                        else if (selectedEntry.status == BackpackItemStatus.Placed)
                        {
                            sb.Append("\n\nĐang đặt tại: ").Append(WorldAnchors.Label(selectedEntry.placedAnchorId)).Append('.');
                        }

                        if (selectedDef.Id == BackpackCatalog.E10aEnvelope)
                        {
                            int partsRead = E10aParts.ReadCount(State);
                            sb.Append($"\n\nTiến độ đọc tài liệu trong gói: {partsRead}/4 mục")
                              .Append(State.Happened(E10aParts.SignedAppointment) ? "\n✓ Giấy nhắn ngoài bì (Bảy)" : "\n• Chưa đọc giấy nhắn ngoài bì")
                              .Append(State.Happened(E10aParts.Letter) ? "\n✓ Thư cũ của Lan (2005)" : "\n• Chưa đọc thư cũ của Lan")
                              .Append(State.Happened(E10aParts.PublicReportSummary) ? "\n✓ Mẩu báo vụ cháy 18/11/2005" : "\n• Chưa đọc mẩu báo 18/11/2005")
                              .Append(State.Happened(E10aParts.PostponementSms) ? "\n✓ Tin nhắn SMS hoãn 22:48 (trên điện thoại)" : "\n• Tin nhắn SMS hoãn 22:48 (xem trên điện thoại [P])");
                            actions.Add(new ActionChoice("Mở gói tài liệu E10a", () =>
                            {
                                backpackStage?.Close();
                                Letter(null, returnToBackpack: true);
                            }));
                        }
                        else if (!isMemoryOnly && selectedDef.LinkedObservationId != null && ObservationRegistry.Get(selectedDef.LinkedObservationId) is ObservationSpec obsSpec)
                        {
                            var rec = State.FindObservation(obsSpec.Id);
                            if (rec != null)
                                sb.Append("\n\nTrạng thái kiểm tra: ").Append(ObservationRegistry.StageLabel(rec.stage)).Append('.');

                            if (isMemory)
                            {
                                actions.Add(new ActionChoice("Xem lại ghi nhớ đã đọc", () =>
                                {
                                    backpackStage?.Close();
                                    OpenObservationInspector(obsSpec, () => OpenBackpack(BackpackCompartment.Papers));
                                }));
                            }
                            else
                            {
                                actions.Add(new ActionChoice("Soi 3D hai mặt", () =>
                                {
                                    backpackStage?.Close();
                                    OpenObservationInspector(obsSpec, () => OpenBackpack(BackpackCompartment.Papers));
                                }));
                                if (selectedDef.CanEquip && State.layer == Reality.B)
                                {
                                    bool eq = selectedEntry.status == BackpackItemStatus.Equipped;
                                    actions.Add(new ActionChoice(eq ? "Cất lại vào ngăn giấy" : "Cầm ra tay", () =>
                                        SaveCheckpoint(() => casework.EquipItemAsync(eq ? "" : selectedDef.Id), () =>
                                        {
                                            handPresenter?.SyncState(State, scene, hideViewModel: false);
                                            if (!eq) CloseModal();
                                            else DrawBackpack();
                                        })));
                                    AddQuickSlotAction(actions, selectedDef, selectedEntry);
                                }
                                if (selectedEntry.status == BackpackItemStatus.Placed)
                                {
                                    string placedAnchor = selectedEntry.placedAnchorId;
                                    actions.Add(new ActionChoice($"Thu lại từ {WorldAnchors.Label(placedAnchor)}", () =>
                                        SaveCheckpoint(() => casework.RetrieveItemFromAnchorAsync(placedAnchor), () =>
                                        {
                                            scene.atmosphere?.From("paper", player.Feet + Vector3.up, 0.55f);
                                            handPresenter?.SyncState(State, scene, hideViewModel: true);
                                            DrawBackpack();
                                        })));
                                }
                                else if (selectedDef.CanPlace && State.layer == Reality.B)
                                {
                                    bool nearAn02 = transaction.AtRequiredPlace(player.Feet, RealityTransaction.WardClipboard) ||
                                                    AmbientDreadDirector.ClassifyZone(player.Feet) == "LOC03";
                                    if (nearAn02)
                                    {
                                        actions.Add(new ActionChoice("Đặt làm nguồn đối chiếu cạnh F101", () =>
                                            SaveCheckpoint(() => casework.PlaceItemAtAnchorAsync(selectedDef.Id, WorldAnchors.An02CompareClip), () =>
                                            {
                                                scene.atmosphere?.From("clipSnap", new Vector3(5.74f, 1.36f, 5.79f), 0.72f);
                                                handPresenter?.SyncState(State, scene, hideViewModel: true);
                                                ui.Caption($"Đã kẹp {selectedDef.ShortLabel} cạnh phiếu F101 để tự đối chiếu.", 3.6f, aboveModal: true);
                                                DrawBackpack();
                                            })));
                                    }
                                }
                            }
                        }
                        else if (!isMemoryOnly)
                        {
                            // CH02/CH03/CH04 Persistent Physical papers (drawings, transfer note, registers, letters)
                            if (selectedDef.CanCombine && InventoryService.CanAssembleCh02Drawing(State))
                            {
                                actions.Add(new ActionChoice("Ghép 3 mảnh tranh thành bản E11", () =>
                                    SaveEvidence(EvidenceRegistry.Drawing, () =>
                                    {
                                        scene.atmosphere?.From("paper", player.Feet + Vector3.up, 0.60f);
                                        backpackSelection = BackpackCatalog.Ch02DrawFull;
                                        ui.Toast("ĐÃ GHÉP TRANH CỦA AN (E11)", "Ba mảnh tranh khớp mép thành hình cánh cửa có ổ khóa. Quay về bảng kẹp 101 để đối chiếu hồ sơ.", 5.2f);
                                        DrawBackpack();
                                    })));
                            }
                            if (image != null && inspector != null && qaFolder == null)
                            {
                                actions.Add(new ActionChoice("Soi 3D", () => InspectBackpackPaper3D(selectedDef, image)));
                            }
                            if (selectedEntry.status == BackpackItemStatus.Placed)
                            {
                                string placedAnchor = selectedEntry.placedAnchorId;
                                actions.Add(new ActionChoice("Thu lại về ba lô", () =>
                                    SaveCheckpoint(() => casework.RetrieveItemFromAnchorAsync(placedAnchor), () =>
                                    {
                                        scene.atmosphere?.From("paper", player.Feet + Vector3.up, 0.50f);
                                        handPresenter?.SyncState(State, scene, hideViewModel: true);
                                        DrawBackpack();
                                    })));
                            }
                            if (selectedDef.CanEquip)
                            {
                                bool eq = selectedEntry.status == BackpackItemStatus.Equipped;
                                actions.Add(new ActionChoice(eq ? "Cất lại vào ngăn giấy" : "Cầm ra tay", () =>
                                    SaveCheckpoint(() => casework.EquipItemAsync(eq ? "" : selectedDef.Id), () =>
                                    {
                                        handPresenter?.SyncState(State, scene, hideViewModel: false);
                                        if (!eq) CloseModal();
                                        else DrawBackpack();
                                    })));
                                AddQuickSlotAction(actions, selectedDef, selectedEntry);
                            }
                        }
                        body = sb.ToString();
                    }
                }

                if (IsJournalAllowedInBackpack)
                {
                    actions.Add(new ActionChoice("Mở sổ ghi chép đầy đủ", () =>
                    {
                        backpackStage?.Close();
                        journalReturn = () => OpenBackpack(BackpackCompartment.Papers);
                        DrawJournal(JournalTab.Documents, pageTurn: !Options.reduceMotion);
                    }));
                }
            }
            else
            {
                emptyCopy = "";
                entries.Add(new NotebookEntry("Ghi chú ngắn (Pocket Note)", () => { backpackSelection = "POCKET_MAIN"; DrawBackpack(); }));
                if (placedItems.Count > 0)
                {
                    entries.Add(new NotebookEntry($"Tìm đồ đã đặt ({placedItems.Count})", () =>
                    {
                        backpackSelection = PlacedLocateKey;
                        DrawBackpack();
                    }));
                }
                if (IsJournalAllowedInBackpack)
                {
                    entries.Add(new NotebookEntry("Mở sổ trực đầy đủ (5 mục)", () =>
                    {
                        backpackStage?.Close();
                        journalReturn = () => OpenBackpack(BackpackCompartment.PocketNote);
                        DrawJournal(JournalTab.Field, pageTurn: !Options.reduceMotion);
                    }));
                }
                if (voice != null && voice.Transcript.Count > 0)
                    entries.Add(new NotebookEntry("Bản ghi hội thoại", () => { backpackStage?.Close(); TranscriptPage(); }));

                if (backpackSelection == PlacedLocateKey && placedItems.Count > 0)
                {
                    BuildPlacedLocateDetail(placedItems, out title, out body, actions);
                }
                else
                {
                    title = "Pocket Note · Ghi nhớ của Minh";
                    body = BuildPocketNoteWithCutToJournalEntries(State);
                    if (voice != null && voice.Transcript.Count > 0)
                    {
                        actions.Add(new ActionChoice("Xem bản ghi hội thoại & ghi nhớ", () =>
                        {
                            backpackStage?.Close();
                            TranscriptPage();
                        }));
                    }
                    if (IsJournalAllowedInBackpack)
                    {
                        actions.Add(new ActionChoice("Mở sổ ghi chép & đối chiếu", () =>
                        {
                            backpackStage?.Close();
                            journalReturn = () => OpenBackpack(BackpackCompartment.PocketNote);
                            DrawJournal(JournalTab.Field, pageTurn: !Options.reduceMotion);
                        }));
                        if (State.f101.revision > 0)
                            actions.Add(new ActionChoice("Xem lịch sử quyết định F101", () =>
                            {
                                backpackStage?.Close();
                                journalReturn = () => OpenBackpack(BackpackCompartment.PocketNote);
                                DrawJournal(JournalTab.Decisions, pageTurn: !Options.reduceMotion);
                            }));
                    }
                }
            }

            bool show3DStage = qaFolder == null && backpackStage != null;
            if (show3DStage)
                backpackStage.Open(State, backpackCompartment, backpackSelection, triggerEcho);

            ui.ShowBackpack(
                (int)backpackCompartment,
                idx =>
                {
                    backpackCompartment = (BackpackCompartment)idx;
                    backpackSelection = null;
                    DrawBackpack(animate: !Options.reduceMotion);
                },
                CloseModal,
                Options.alternateBindings,
                animate && !Options.reduceMotion,
                entries,
                emptyCopy,
                title,
                body,
                image,
                actions.ToArray(),
                showBag08Echo: triggerEcho,
                onEchoDissolve: null,
                show3DStageOnLeft: show3DStage,
                onCloseBag: () => backpackStage?.Close());
        }

        private void InspectBackpackTool3D(BackpackItemDef def, GameObject template)
        {
            backpackStage?.Close();
            scene.atmosphere?.From("keyJingle", player.Eye.transform.position + player.Eye.transform.forward * 0.35f, 0.36f);
            inspector.Open(template, null, 1f, ActiveVolume(), observationId: def.Id);
            void BackToBag()
            {
                inspector.Close();
                OpenBackpack(BackpackCompartment.Tools);
            }
            var choices = new List<ActionChoice>();
            if (def.CanEquip)
                choices.Add(new ActionChoice("Cầm trên tay", () =>
                    SaveCheckpoint(() => casework.EquipItemAsync(def.Id), () =>
                    {
                        inspector.Close();
                        CloseModal();
                        handPresenter?.SyncState(State, scene, hideViewModel: false);
                    })));
            choices.Add(new ActionChoice("Trả lại ba lô", BackToBag));
            ui.ShowInspectSidebar(def.ProvenanceLabel, def.Title, def.Description, choices.ToArray(), BackToBag, () => inspector.Close());
        }

        private void InspectBackpackPaper3D(BackpackItemDef def, Texture frontTex)
        {
            backpackStage?.Close();
            scene.atmosphere?.From("paper", player.Eye.transform.position + player.Eye.transform.forward * 0.35f, 0.45f);
            float aspect = frontTex != null ? (float)frontTex.width / Mathf.Max(1, frontTex.height) : 0.75f;
            inspector.Open(null, frontTex, aspect, ActiveVolume(), observationId: def.Id);
            void BackToBag()
            {
                inspector.Close();
                OpenBackpack(BackpackCompartment.Papers);
            }
            var choices = new List<ActionChoice>();
            if (def.CanCombine && InventoryService.CanAssembleCh02Drawing(State))
            {
                choices.Add(new ActionChoice("Ghép 3 mảnh tranh (E11)", () =>
                    SaveEvidence(EvidenceRegistry.Drawing, () =>
                    {
                        inspector.Close();
                        backpackSelection = BackpackCatalog.Ch02DrawFull;
                        OpenBackpack(BackpackCompartment.Papers);
                    })));
            }
            if (def.CanEquip)
                choices.Add(new ActionChoice("Cầm trên tay", () =>
                    SaveCheckpoint(() => casework.EquipItemAsync(def.Id), () =>
                    {
                        inspector.Close();
                        CloseModal();
                        handPresenter?.SyncState(State, scene, hideViewModel: false);
                    })));
            choices.Add(new ActionChoice("Trả lại ba lô", BackToBag));
            ui.ShowInspectSidebar(def.ProvenanceLabel, def.Title, def.Description, choices.ToArray(), BackToBag, () => inspector.Close());
        }

        private static string SurfaceSectionTitle(string surfaceTargetId) => surfaceTargetId switch
        {
            "COMPARE_CLIP_AN02" => "KẸP ĐỐI CHIẾU CẠNH F101 · AN02",
            "COMPARE_DESK_STAFF" => "BÀN ĐỐI CHIẾU ĐIỀU DƯỠNG · STAFF",
            "COMPARE_DESK_RECEPTION" => "MẶT QUẦY TIẾP NHẬN · ĐỐI CHIẾU",
            "COMPARE_DESK_101" => "BÀN ĐỐI CHIẾU & GHÉP HỒ SƠ · PHÒNG 101",
            _ => "BÀN ĐỐI CHIẾU VẬT LÝ"
        };

        private static Vector3 SurfaceSoundOrigin(string surfaceTargetId) => surfaceTargetId switch
        {
            "COMPARE_CLIP_AN02" => new Vector3(5.74f, 1.36f, 5.79f),
            "COMPARE_DESK_STAFF" => new Vector3(0.88f, 0.96f, 3.70f),
            "COMPARE_DESK_RECEPTION" => new Vector3(1.86f, 1.15f, -1.96f),
            "COMPARE_DESK_101" => new Vector3(7.08f, 0.82f, 2.26f),
            _ => Vector3.zero
        };

        private void InteractComparisonSurface(string surfaceTargetId)
        {
            string[] slots = WorldAnchors.SlotsForSurface(surfaceTargetId);
            if (slots.Length == 0 || !WorldAnchors.AllowedInLayer(slots[0], State.layer))
                return;

            Vector3 sfxOrigin = SurfaceSoundOrigin(surfaceTargetId);
            string placeSfx = surfaceTargetId == "COMPARE_CLIP_AN02" ? "clipSnap" : "paper";

            // Fast physical path: holding a valid placeable item immediately places it onto the first free slot!
            var equipped = InventoryService.GetEquipped(State);
            if (equipped != null)
            {
                var res = WorldUseResolver.Resolve(State, equipped.itemDefId, surfaceTargetId);
                if (res.Outcome == WorldUseOutcome.PlacePaperOnAnchor)
                {
                    string targetSlot = slots[0];
                    for (int i = 0; i < slots.Length; i++)
                    {
                        if (InventoryService.GetPlacedAt(State, slots[i]) == null)
                        {
                            targetSlot = slots[i];
                            break;
                        }
                    }
                    SaveCheckpoint(() => casework.PlaceItemAtAnchorAsync(equipped.itemDefId, targetSlot), () =>
                    {
                        handPresenter?.TriggerUseAnimation();
                        scene.atmosphere?.From(placeSfx, sfxOrigin, 0.78f);
                        var def = BackpackCatalog.Get(equipped.itemDefId);
                        ui.Caption($"Đã đặt {def?.ShortLabel ?? "tài liệu"} lên {WorldAnchors.Label(targetSlot)}.", 3.5f);
                        handPresenter?.SyncState(State, scene, hideViewModel: false);
                    });
                    return;
                }
                if (res.Outcome == WorldUseOutcome.NotApplicableFeedback)
                {
                    ui.Caption(res.Feedback, 3.2f, true);
                    return;
                }
            }

            // Inspect / arrange / compare modal for 1-slot clip or 2-slot physical work surfaces
            var placedEntries = new BackpackEntry[slots.Length];
            var placedObs = new List<ObservationSpec>();
            int occupiedCount = 0;
            var body = new StringBuilder();
            for (int i = 0; i < slots.Length; i++)
            {
                placedEntries[i] = InventoryService.GetPlacedAt(State, slots[i]);
                if (placedEntries[i] != null)
                {
                    occupiedCount++;
                    var def = BackpackCatalog.Get(placedEntries[i].itemDefId);
                    body.Append(slots.Length == 1 ? "• Đang kẹp: " : $"• Ô {i + 1}: ")
                        .AppendLine(def?.Title ?? placedEntries[i].itemDefId);
                    if (def?.LinkedObservationId != null && ObservationRegistry.Get(def.LinkedObservationId) is ObservationSpec os)
                        placedObs.Add(os);
                }
                else
                {
                    body.AppendLine(slots.Length == 1 ? "• Kẹp đang trống." : $"• Ô {i + 1}: Trống.");
                }
            }
            body.AppendLine();
            body.Append(surfaceTargetId == "COMPARE_DESK_101" && State.chapter == "CH02" && State.layer == Reality.R
                ? "Có thể trải các mảnh tranh, phiếu chuyển và vòng tay lên mặt bàn để đối chiếu hoặc ghép 3 mảnh tranh tại đây."
                : "Đặt hai tài liệu cạnh nhau trên mặt bàn giúp tự đối chiếu mốc giờ, quy chế và chữ ký. Việc đặt giấy không tự động đánh dấu hay hoàn tất suy luận.");

            var choices = new List<ActionChoice>();

            // Authored CH02 physical table assembly of the 3 drawing fragments into E11.drawing!
            if (surfaceTargetId == "COMPARE_DESK_101" && InventoryService.CanAssembleCh02Drawing(State))
            {
                choices.Add(new ActionChoice("Ghép 3 mảnh tranh trên mặt bàn (E11)", () =>
                    SaveEvidence(EvidenceRegistry.Drawing, () =>
                    {
                        scene.atmosphere?.From("paper", sfxOrigin, 0.72f);
                        string freeTableSlot = InventoryService.GetPlacedAt(State, WorldAnchors.Ward101TableLeft) == null
                            ? WorldAnchors.Ward101TableLeft
                            : InventoryService.GetPlacedAt(State, WorldAnchors.Ward101TableRight) == null
                                ? WorldAnchors.Ward101TableRight
                                : WorldAnchors.Ward101TableLeft;
                        SaveCheckpoint(() => casework.PlaceItemAtAnchorAsync(BackpackCatalog.Ch02DrawFull, freeTableSlot), () =>
                        {
                            handPresenter?.SyncState(State, scene, hideViewModel: false);
                            ui.Toast("ĐÃ GHÉP TRANH CỦA AN (E11)", "Ba mảnh tranh đã ghép khít trên bàn Phòng 101. Quay lại bảng kẹp để đối chiếu hồ sơ C01.", 5.5f);
                            CloseModal();
                        });
                    })));
            }

            // Side-by-side 2-document comparison shortcut
            if (surfaceTargetId == "COMPARE_CLIP_AN02" && placedObs.Count == 1)
            {
                var obs = placedObs[0];
                choices.Add(new ActionChoice("Soi song song với phiếu F101", () =>
                {
                    comparison[0] = new PaperPlacement(ObservationRegistry.F101Sheet);
                    comparison[1] = new PaperPlacement(obs.Id);
                    comparingDocuments = true;
                    journalReturn = CloseModal;
                    DrawJournal(JournalTab.Documents, pageTurn: !Options.reduceMotion);
                }));
            }
            else if (placedObs.Count >= 2)
            {
                var leftObs = placedObs[0];
                var rightObs = placedObs[1];
                choices.Add(new ActionChoice($"Soi song song: {leftObs.ShortCode} ↔ {rightObs.ShortCode}", () =>
                {
                    comparison[0] = new PaperPlacement(leftObs.Id);
                    comparison[1] = new PaperPlacement(rightObs.Id);
                    comparingDocuments = true;
                    journalReturn = CloseModal;
                    DrawJournal(JournalTab.Documents, pageTurn: !Options.reduceMotion);
                }));
            }

            // Inspect & Retrieve actions for each occupied slot
            for (int i = 0; i < slots.Length; i++)
            {
                var placed = placedEntries[i];
                if (placed == null) continue;
                var def = BackpackCatalog.Get(placed.itemDefId);
                string anchorId = slots[i];
                string slotPrefix = slots.Length == 1 ? "" : $"[Ô {i + 1}] ";
                if (def?.LinkedObservationId != null && ObservationRegistry.Get(def.LinkedObservationId) is ObservationSpec obs)
                {
                    choices.Add(new ActionChoice($"{slotPrefix}Soi & đánh dấu {def.ShortLabel}", () =>
                        OpenObservationInspector(obs, () => InteractComparisonSurface(surfaceTargetId))));
                }
                else if (def != null && !string.IsNullOrEmpty(def.FrontTexture) && Image(def.FrontTexture) is Texture tex && inspector != null && qaFolder == null)
                {
                    choices.Add(new ActionChoice($"{slotPrefix}Soi 3D {def.ShortLabel}", () => InspectBackpackPaper3D(def, tex)));
                }
                choices.Add(new ActionChoice($"{slotPrefix}Rút {def?.ShortLabel ?? "tài liệu"} về ba lô", () =>
                    SaveCheckpoint(() => casework.RetrieveItemFromAnchorAsync(anchorId), () =>
                    {
                        scene.atmosphere?.From("paper", sfxOrigin, 0.58f);
                        handPresenter?.SyncState(State, scene, hideViewModel: false);
                        InteractComparisonSurface(surfaceTargetId);
                    })));
            }

            // Place eligible items from Backpack into the first free slot (full keyboard/accessibility parity!)
            string freeSlot = null;
            for (int i = 0; i < slots.Length; i++)
                if (placedEntries[i] == null) { freeSlot = slots[i]; break; }

            if (freeSlot != null)
            {
                foreach (var entry in State.backpackItems)
                {
                    if (entry.status == BackpackItemStatus.MemoryRef || entry.status == BackpackItemStatus.Placed)
                        continue;
                    var def = BackpackCatalog.Get(entry.itemDefId);
                    if (def == null || !def.CanPlace) continue;
                    if (def.Kind == ItemKind.LayerBoundPaper && State.layer != Reality.B) continue;
                    string itemId = def.Id;
                    string slotTarget = freeSlot;
                    string verb = surfaceTargetId == "COMPARE_CLIP_AN02" ? "Kẹp" : "Đặt";
                    choices.Add(new ActionChoice($"{verb} {def.ShortLabel} vào đây", () =>
                        SaveCheckpoint(() => casework.PlaceItemAtAnchorAsync(itemId, slotTarget), () =>
                        {
                            scene.atmosphere?.From(placeSfx, sfxOrigin, 0.76f);
                            handPresenter?.SyncState(State, scene, hideViewModel: false);
                            ui.Caption($"Đã đặt {def.ShortLabel} lên {WorldAnchors.Label(slotTarget)}.", 3.4f, aboveModal: slots.Length > 1);
                            if (slots.Length == 1) CloseModal();
                            else InteractComparisonSurface(surfaceTargetId);
                        })));
                }
            }

            if (occupiedCount == 0 && choices.Count == 0)
            {
                ui.Caption(surfaceTargetId == "COMPARE_CLIP_AN02"
                    ? "Kẹp đối chiếu cạnh F101 đang trống. Có thể cất tạm thẻ AS-05B hoặc phiếu khác trong lớp B rồi kẹp vào đây."
                    : "Mặt bàn đối chiếu đang trống. Nhặt hoặc cất tạm giấy tờ/vật chứng vào ba lô rồi đặt lên đây để tự đối chiếu.", 4.2f, true);
                return;
            }

            choices.Add(new ActionChoice("Đóng", CloseModal));
            string heading = occupiedCount == 0 ? "Đặt tài liệu đối chiếu"
                : slots.Length == 1 ? (BackpackCatalog.Get(placedEntries[0].itemDefId)?.Title ?? "Tài liệu đang kẹp")
                : $"Mặt bàn đối chiếu ({occupiedCount}/2 ô)";
            ui.Show(SurfaceSectionTitle(surfaceTargetId), heading, body.ToString().TrimEnd(), choices.ToArray(), CloseModal);
        }

        private bool TryUseHeldTapeOnAn04()
        {
            if (State == null || State.chapter != "CH04") return false;
            var eq = InventoryService.GetEquipped(State);
            if (eq == null || eq.itemDefId != BackpackCatalog.Ch04Tape) return false;
            var res = WorldUseResolver.Resolve(State, eq.itemDefId, "AN04");
            if (res.Outcome == WorldUseOutcome.NotApplicableFeedback)
            {
                ui.Caption(res.Feedback, 4.2f, true);
                return true;
            }
            if (res.Outcome == WorldUseOutcome.PlayTapeAtDutyDesk)
            {
                handPresenter?.TriggerUseAnimation();
                scene.atmosphere?.From("latchClick", player.Eye.transform.position + player.Eye.transform.forward * 0.45f, 0.72f);
                Ch04Meeting();
                return true;
            }
            return false;
        }
    }
}
