using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CTCC
{
    public sealed partial class HospitalGame
    {
        private HandItemPresenter handPresenter;
        private BackpackCompartment backpackCompartment = BackpackCompartment.Tools;
        private string backpackSelection;
        private bool showingBag08EchoThisOpen;

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

        private void DrawBackpack(bool animate = false)
        {
            InventoryService.EnsureDefaultTools(State);
            var entries = new List<NotebookEntry>();
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
                    string badge = item.status == BackpackItemStatus.Equipped ? " [Đang cầm]" : "";
                    if (def.Id == BackpackCatalog.Flashlight)
                        badge = player != null && player.IsTorchOn ? " [Đang bật]" : " [Đang tắt]";
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
                else
                {
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
                    }
                    var tmpl = handPresenter?.GetInspectTemplate(selectedDef.Id);
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
                    string tag = item.status switch
                    {
                        BackpackItemStatus.MemoryRef => " [Ký ức · Nguồn B]",
                        BackpackItemStatus.Placed => " [Đang kẹp]",
                        BackpackItemStatus.Equipped => " [Đang cầm]",
                        _ => def.Kind == ItemKind.PersistentPhysical ? " [Vật chứng R]" : " [Giấy lớp B]"
                    };
                    string id = item.itemDefId;
                    entries.Add(new NotebookEntry(def.ShortLabel + tag, () =>
                    {
                        backpackSelection = id;
                        DrawBackpack();
                    }));
                }

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
                    bool isMemory = selectedEntry.status == BackpackItemStatus.MemoryRef;
                    var sb = new StringBuilder();
                    sb.Append(isMemory ? $"Ký ức đã kiểm · {selectedEntry.originLoc} / Lớp B / 17-11-2005" : selectedDef.ProvenanceLabel);
                    sb.Append("\n\n").Append(selectedDef.Description);

                    if (isMemory)
                    {
                        sb.Append("\n\nTờ giấy này chỉ tồn tại trong lớp B (2005). Trong hiện thực năm 2012 không có bản vật lý để cầm hay xuất làm vật chứng — chỉ còn ghi nhớ về những gì Minh đã tận mắt đọc.");
                    }
                    else if (selectedEntry.status == BackpackItemStatus.Placed)
                    {
                        sb.Append("\n\nĐang kẹp tại: ").Append(WorldAnchors.Label(selectedEntry.placedAnchorId)).Append('.');
                    }

                    if (selectedDef.Id == BackpackCatalog.E10aEnvelope)
                    {
                        actions.Add(new ActionChoice("Mở bì thư E10a", () => Letter(null, returnToBackpack: true)));
                    }
                    else if (selectedDef.LinkedObservationId != null && ObservationRegistry.Get(selectedDef.LinkedObservationId) is ObservationSpec obsSpec)
                    {
                        var rec = State.FindObservation(obsSpec.Id);
                        if (rec != null)
                            sb.Append("\n\nTrạng thái kiểm tra: ").Append(ObservationRegistry.StageLabel(rec.stage)).Append('.');

                        if (isMemory)
                        {
                            actions.Add(new ActionChoice("Xem lại ghi nhớ đã đọc", () =>
                                OpenObservationInspector(obsSpec, () => OpenBackpack(BackpackCompartment.Papers))));
                        }
                        else
                        {
                            actions.Add(new ActionChoice("Soi 3D hai mặt", () =>
                                OpenObservationInspector(obsSpec, () => OpenBackpack(BackpackCompartment.Papers))));
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
                            }
                            if (selectedDef.CanPlace && State.layer == Reality.B)
                            {
                                bool placedAn02 = selectedEntry.status == BackpackItemStatus.Placed &&
                                                  selectedEntry.placedAnchorId == WorldAnchors.An02CompareClip;
                                bool nearAn02 = transaction.AtRequiredPlace(player.Feet, RealityTransaction.WardClipboard) ||
                                                AmbientDreadDirector.ClassifyZone(player.Feet) == "LOC03";
                                if (placedAn02)
                                {
                                    actions.Add(new ActionChoice("Rút khỏi kẹp cạnh F101", () =>
                                        SaveCheckpoint(() => casework.RetrieveItemFromAnchorAsync(WorldAnchors.An02CompareClip), () =>
                                        {
                                            scene.atmosphere?.From("paper", player.Feet + Vector3.up, 0.55f);
                                            handPresenter?.SyncState(State, scene, hideViewModel: false);
                                            DrawBackpack();
                                        })));
                                }
                                else if (nearAn02)
                                {
                                    actions.Add(new ActionChoice("Đặt làm nguồn đối chiếu cạnh F101", () =>
                                        SaveCheckpoint(() => casework.PlaceItemAtAnchorAsync(selectedDef.Id, WorldAnchors.An02CompareClip), () =>
                                        {
                                            scene.atmosphere?.From("clipSnap", new Vector3(5.74f, 1.36f, 5.84f), 0.72f);
                                            handPresenter?.SyncState(State, scene, hideViewModel: false);
                                            ui.Caption($"Đã kẹp {selectedDef.ShortLabel} cạnh phiếu F101 để tự đối chiếu.", 3.6f, aboveModal: true);
                                            DrawBackpack();
                                        })));
                                }
                            }
                        }
                    }
                    body = sb.ToString();
                }

                actions.Add(new ActionChoice("Mở sổ ghi chép đầy đủ", () =>
                {
                    journalReturn = () => OpenBackpack(BackpackCompartment.Papers);
                    DrawJournal(JournalTab.Documents, pageTurn: !Options.reduceMotion);
                }));
            }
            else
            {
                emptyCopy = "";
                entries.Add(new NotebookEntry("Ghi chú ngắn (Pocket Note)", () => { backpackSelection = "POCKET_MAIN"; DrawBackpack(); }));
                entries.Add(new NotebookEntry("Mở sổ trực đầy đủ (5 mục)", () =>
                {
                    journalReturn = () => OpenBackpack(BackpackCompartment.PocketNote);
                    DrawJournal(JournalTab.Field, pageTurn: !Options.reduceMotion);
                }));
                if (voice != null && voice.Transcript.Count > 0)
                    entries.Add(new NotebookEntry("Bản ghi hội thoại", TranscriptPage));

                title = "Pocket Note · Ghi nhớ của Minh";
                body = PocketNoteBuilder.BuildEpistemicSummary(State);
                actions.Add(new ActionChoice("Mở sổ ghi chép & đối chiếu", () =>
                {
                    journalReturn = () => OpenBackpack(BackpackCompartment.PocketNote);
                    DrawJournal(JournalTab.Field, pageTurn: !Options.reduceMotion);
                }));
                if (State.f101.revision > 0)
                    actions.Add(new ActionChoice("Xem lịch sử quyết định F101", () =>
                    {
                        journalReturn = () => OpenBackpack(BackpackCompartment.PocketNote);
                        DrawJournal(JournalTab.Decisions, pageTurn: !Options.reduceMotion);
                    }));
            }

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
                onEchoDissolve: null);
        }

        private void InspectBackpackTool3D(BackpackItemDef def, GameObject template)
        {
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

        private void InteractComparisonClipAn02()
        {
            if (State.layer != Reality.B) return;
            var equipped = InventoryService.GetEquipped(State);
            if (equipped != null)
            {
                var res = WorldUseResolver.Resolve(State, equipped.itemDefId, "COMPARE_CLIP_AN02");
                if (res.Outcome == WorldUseOutcome.PlacePaperOnAnchor)
                {
                    SaveCheckpoint(() => casework.PlaceItemAtAnchorAsync(equipped.itemDefId, WorldAnchors.An02CompareClip), () =>
                    {
                        handPresenter?.TriggerUseAnimation();
                        scene.atmosphere?.From("clipSnap", new Vector3(5.74f, 1.36f, 5.84f), 0.78f);
                        var def = BackpackCatalog.Get(equipped.itemDefId);
                        ui.Caption($"Đã kẹp {def?.ShortLabel ?? "tài liệu"} cạnh phiếu F101.", 3.5f);
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

            var placed = InventoryService.GetPlacedAt(State, WorldAnchors.An02CompareClip);
            if (placed != null)
            {
                var def = BackpackCatalog.Get(placed.itemDefId);
                var obs = def?.LinkedObservationId != null ? ObservationRegistry.Get(def.LinkedObservationId) : null;
                var choices = new List<ActionChoice>();
                if (obs != null)
                    choices.Add(new ActionChoice("Soi & đánh dấu trực tiếp", () => OpenObservationInspector(obs, CloseModal)));
                choices.Add(new ActionChoice("Soi song song với phiếu F101", () =>
                {
                    if (obs != null)
                    {
                        comparison[0] = new PaperPlacement(ObservationRegistry.F101Sheet);
                        comparison[1] = new PaperPlacement(obs.Id);
                        comparingDocuments = true;
                        journalReturn = CloseModal;
                        DrawJournal(JournalTab.Documents, pageTurn: !Options.reduceMotion);
                    }
                }, obs != null));
                choices.Add(new ActionChoice("Rút tài liệu cất về ba lô", () =>
                    SaveCheckpoint(() => casework.RetrieveItemFromAnchorAsync(WorldAnchors.An02CompareClip), () =>
                    {
                        scene.atmosphere?.From("paper", new Vector3(5.74f, 1.36f, 5.84f), 0.60f);
                        handPresenter?.SyncState(State, scene, hideViewModel: false);
                        CloseModal();
                    })));
                choices.Add(new ActionChoice("Để nguyên trên kẹp", CloseModal));
                ui.Show("KẸP ĐỐI CHIẾU CẠNH F101 · AN02", def?.Title ?? "Tài liệu đang kẹp",
                    "Tài liệu được kẹp ngay bên cạnh phiếu F101 để tự đọc đối chiếu hai bề mặt. Việc kẹp giấy không tự động đánh dấu hay hoàn tất suy luận.",
                    choices.ToArray(), CloseModal);
                return;
            }

            // Empty clip: offer stowed Layer B documents to clip (full keyboard/accessibility parity!)
            var stowable = new List<ActionChoice>();
            foreach (var entry in State.backpackItems)
            {
                var def = BackpackCatalog.Get(entry.itemDefId);
                if (def == null || !def.CanPlace || def.Kind != ItemKind.LayerBoundPaper || entry.status == BackpackItemStatus.MemoryRef)
                    continue;
                string id = def.Id;
                stowable.Add(new ActionChoice($"Kẹp {def.ShortLabel} vào đây", () =>
                    SaveCheckpoint(() => casework.PlaceItemAtAnchorAsync(id, WorldAnchors.An02CompareClip), () =>
                    {
                        scene.atmosphere?.From("clipSnap", new Vector3(5.74f, 1.36f, 5.84f), 0.78f);
                        handPresenter?.SyncState(State, scene, hideViewModel: false);
                        CloseModal();
                        ui.Caption($"Đã kẹp {def.ShortLabel} cạnh phiếu F101.", 3.5f);
                    })));
            }
            if (stowable.Count == 0)
            {
                ui.Caption("Kẹp đối chiếu cạnh F101 đang trống. Có thể cất tạm thẻ AS-05B hoặc phiếu khác trong lớp B rồi kẹp vào đây.", 4.2f, true);
                return;
            }
            stowable.Add(new ActionChoice("Đóng", CloseModal));
            ui.Show("KẸP ĐỐI CHIẾU CẠNH F101 · AN02", "Đặt tài liệu đối chiếu",
                "Chọn một tài liệu lớp B đang cất trong ba lô để kẹp ngay bên cạnh phiếu F101.",
                stowable.ToArray(), CloseModal);
        }
    }
}
