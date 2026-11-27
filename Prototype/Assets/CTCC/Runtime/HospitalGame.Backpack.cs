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
        private bool pocketNoteOpen;
        private const string PlacedLocateKey = "__PLACED_LOCATE__";

        internal bool IsPocketNoteOpen => pocketNoteOpen && ui != null && ui.IsModal;

        /// <summary>
        /// Opens Inventory 2.0 (Đồ vật | Giấy tờ) with interactive 360-degree 3D preview.
        /// Note: BackpackCompartment.PocketNote routes cleanly to the decoupled Sổ tay nhỏ presentation!
        /// </summary>
        private void OpenBackpack(BackpackCompartment? compartment = null)
        {
            if (compartment == BackpackCompartment.PocketNote)
            {
                OpenPocketNote();
                return;
            }
            pocketNoteOpen = false;
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

            if (backpackCompartment == BackpackCompartment.PocketNote)
                backpackCompartment = BackpackCompartment.Papers;

            DrawBackpack(openingFresh);
        }

        /// <summary>
        /// Phase 2.5 decoupled Sổ tay nhỏ (Pocket Note) — completely separated from Inventory 2.0 presentation.
        /// Read-only concise recall in CH01 (0 Journal tabs); gateway to full 5-tab Journal in CH02+.
        /// </summary>
        internal void OpenPocketNote(bool showPlacedRecovery = false)
        {
            if (State == null || busy) return;
            backpackStage?.Close();
            inspector?.Close();
            pocketNoteOpen = true;

            var placed = InventoryService.GetAllPlaced(State);
            var actions = new List<ActionChoice>();
            string title;
            string body;

            if (showPlacedRecovery && placed.Count > 0)
            {
                BuildPlacedLocateDetail(placed, out title, out body, actions);
                actions.Add(new ActionChoice("Quay lại ghi nhớ", () => OpenPocketNote(false)));
            }
            else
            {
                title = "Sổ tay nhỏ của Minh";
                body = BuildPocketNoteWithCutToJournalEntries(State);
                if (placed.Count > 0)
                    actions.Add(new ActionChoice($"Đồ đang đặt ngoài hiện trường ({placed.Count})", () => OpenPocketNote(true)));
                if (voice != null && voice.Transcript.Count > 0)
                    actions.Add(new ActionChoice("Xem lại lời thoại đã nghe", () => { pocketNoteOpen = false; TranscriptPage(); }));
                if (IsJournalAllowedInBackpack)
                {
                    actions.Add(new ActionChoice("Mở sổ trực đầy đủ", () =>
                    {
                        pocketNoteOpen = false;
                        journalReturn = () => OpenPocketNote(false);
                        DrawJournal(JournalTab.Field, pageTurn: !Options.reduceMotion);
                    }));
                    if (State.f101.revision > 0)
                        actions.Add(new ActionChoice("Lịch sử phiếu F101", () =>
                        {
                            pocketNoteOpen = false;
                            journalReturn = () => OpenPocketNote(false);
                            DrawJournal(JournalTab.Decisions, pageTurn: !Options.reduceMotion);
                        }));
                }
                actions.Add(new ActionChoice("Mở ba lô [Tab]", () => { pocketNoteOpen = false; OpenBackpack(); }));
            }
            actions.Add(new ActionChoice("Đóng sổ tay", ClosePocketNote));

            ui.Show("GHI NHỚ CÁ NHÂN", title, body, actions.ToArray(), ClosePocketNote);
        }

        private void ClosePocketNote()
        {
            pocketNoteOpen = false;
            CloseModal();
        }

        private BackpackCompartment DefaultBackpackCompartment()
        {
            if (State.chapter == "CH01" && !State.Happened("CH01-T02"))
                return BackpackCompartment.Tools;
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
                    ui.Caption(alreadyEquipped ? $"Đã cất {def.ShortLabel}." : $"Đang cầm {def.ShortLabel}.", 2.0f);
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
            string label = slotNum > 0 ? $"Gỡ ô nhanh [{slotNum}]" : "Ghim ô nhanh";
            actions.Add(new ActionChoice(label, () =>
                SaveCheckpoint(() => casework.ToggleQuickSlotAsync(def.Id), () => DrawBackpack())));
        }

        private void BuildPlacedLocateDetail(List<BackpackEntry> placed, out string title, out string body, List<ActionChoice> actions)
        {
            title = $"Đồ đang đặt ngoài hiện trường ({placed.Count})";
            var sb = new StringBuilder();
            for (int i = 0; i < placed.Count; i++)
            {
                var p = placed[i];
                var def = BackpackCatalog.Get(p.itemDefId);
                string anchorLabel = WorldAnchors.Label(p.placedAnchorId);
                sb.Append("• ").Append(def?.Title ?? p.itemDefId)
                  .Append(" — ").AppendLine(anchorLabel);
                string anchorId = p.placedAnchorId;
                string shortLabel = def?.ShortLabel ?? p.itemDefId;
                actions.Add(new ActionChoice($"Thu lại {shortLabel}", () =>
                    SaveCheckpoint(() => casework.RetrieveItemFromAnchorAsync(anchorId), () =>
                    {
                        scene.atmosphere?.From("paper", player.Eye.transform.position + player.Eye.transform.forward * 0.3f, 0.48f);
                        handPresenter?.SyncState(State, scene, hideViewModel: true);
                        ui.Caption($"Đã thu {shortLabel} về ba lô.", 2.8f, aboveModal: true);
                        if (pocketNoteOpen) OpenPocketNote(InventoryService.GetAllPlaced(State).Count > 0);
                        else DrawBackpack();
                    })));
            }
            body = sb.ToString().TrimEnd();
        }

        private void DrawBackpack(bool animate = false)
        {
            InventoryService.EnsureDefaultTools(State);
            bool triggerEcho = false;
            int filterIdx = backpackCompartment == BackpackCompartment.Papers ? 1 : 0;

            if (filterIdx == 1 && !showingBag08EchoThisOpen && ShouldTriggerBag08Echo())
            {
                showingBag08EchoThisOpen = true;
                triggerEcho = true;
                SaveCheckpoint(() => casework.RecordDreadEventAsync(DreadCatalog.Bag08Echo));
            }

            // Collect items for active filter (0 = Tools, 1 = Papers)
            var matching = new List<(BackpackEntry entry, BackpackItemDef def)>();
            foreach (var item in State.backpackItems)
            {
                var def = BackpackCatalog.Get(item.itemDefId);
                if (def == null) continue;
                bool wantPapers = filterIdx == 1;
                if ((def.Compartment == BackpackCompartment.Papers) != wantPapers) continue;
                matching.Add((item, def));
            }

            // Ensure valid selection
            BackpackEntry selectedEntry = null;
            BackpackItemDef selectedDef = null;
            for (int i = 0; i < matching.Count; i++)
            {
                if (matching[i].def.Id == backpackSelection)
                {
                    selectedEntry = matching[i].entry;
                    selectedDef = matching[i].def;
                    break;
                }
            }
            if (selectedDef == null && matching.Count > 0)
            {
                selectedEntry = matching[0].entry;
                selectedDef = matching[0].def;
                backpackSelection = selectedDef.Id;
            }

            // Build visual tiles
            var tiles = new List<InventoryTileData>(matching.Count);
            for (int i = 0; i < matching.Count; i++)
            {
                var (entry, def) = matching[i];
                string badge = "";
                if (def.Id == BackpackCatalog.Flashlight)
                    badge = player != null && player.IsTorchOn ? "Bật" : "Tắt";
                else if (def.Kind == ItemKind.MemoryOnly || entry.status == BackpackItemStatus.MemoryRef)
                    badge = "Ký ức";
                else if (entry.status == BackpackItemStatus.Equipped)
                    badge = "Cầm";
                else if (entry.status == BackpackItemStatus.Placed)
                    badge = "Đặt";
                int qs = QuickSlotNumber(def.Id);
                if (qs > 0) badge = string.IsNullOrEmpty(badge) ? $"#{qs}" : $"{badge} · #{qs}";

                Texture thumb = !string.IsNullOrEmpty(def.FrontTexture) ? Image(def.FrontTexture) : null;
                string id = def.Id;
                tiles.Add(new InventoryTileData(id, def.ShortLabel, badge, thumb, id == backpackSelection, () =>
                {
                    backpackSelection = id;
                    DrawBackpack();
                }));
            }

            string emptyCopy = filterIdx == 0 ? "Không có vật dụng nào." : "Chưa có giấy tờ nào trong ba lô.";
            string selTitle = selectedDef?.Title ?? (filterIdx == 0 ? "Đồ vật" : "Giấy tờ");
            string selBrief = "";
            var actions = new List<ActionChoice>();

            if (selectedDef != null && selectedEntry != null)
            {
                bool isMemoryOnly = selectedDef.Kind == ItemKind.MemoryOnly;
                bool isMemory = isMemoryOnly || selectedEntry.status == BackpackItemStatus.MemoryRef;

                if (selectedDef.Id == BackpackCatalog.Flashlight)
                {
                    int pct = Mathf.RoundToInt(Mathf.Clamp01(player.BatterySeconds / 360f) * 100f);
                    selBrief = $"Pin còn khoảng {pct}%.";
                    actions.Add(new ActionChoice(player.IsTorchOn ? "Tắt đèn pin" : "Bật đèn pin", () =>
                    {
                        player.SetTorchEnabled(!player.IsTorchOn);
                        scene.atmosphere?.From("doorLatch", player.Eye.transform.position, 0.45f);
                        DrawBackpack();
                    }));
                }
                else if (selectedDef.Id == BackpackCatalog.MinhPhone)
                {
                    selBrief = State.Happened(E10aParts.PostponementSms)
                        ? "Điện thoại cá nhân của Minh."
                        : "Có tin nhắn chưa đọc.";
                    actions.Add(new ActionChoice("Mở điện thoại", () =>
                    {
                        backpackStage?.Close();
                        OpenPhone(0, returnToBackpack: true);
                    }));
                }
                else if (selectedDef.Id == BackpackCatalog.E10aEnvelope)
                {
                    selBrief = "Phong bì chống ẩm chú Bảy gửi lại ngoài hộp thư trụ cổng.";
                    actions.Add(new ActionChoice("Cầm & mở phong bì", () =>
                    {
                        backpackStage?.Close();
                        OpenPhysicalEnvelope3D(returnToBackpack: true);
                    }));
                }
                else
                {
                    selBrief = isMemory
                        ? "Chỉ còn lại trong trí nhớ sau khi trở về năm 2012."
                        : selectedEntry.status == BackpackItemStatus.Placed
                            ? $"Đang đặt tại {WorldAnchors.Label(selectedEntry.placedAnchorId)}."
                            : selectedDef.ProvenanceLabel;

                    // 1. Inspect action (full-screen 0-sidebar Physical Inspector 2.0!)
                    if (!isMemoryOnly && selectedDef.LinkedObservationId != null &&
                        ObservationRegistry.Get(selectedDef.LinkedObservationId) is ObservationSpec obsSpec)
                    {
                        actions.Add(new ActionChoice("Kiểm tra", () =>
                        {
                            backpackStage?.Close();
                            OpenObservationInspector(obsSpec, () => OpenBackpack(BackpackCompartment.Papers));
                        }));
                    }
                    else if (!isMemoryOnly)
                    {
                        var tmpl = ResolveInventory3DTemplate(selectedDef);
                        Texture paperTex = !string.IsNullOrEmpty(selectedDef.FrontTexture) ? Image(selectedDef.FrontTexture) : null;
                        if (tmpl != null && inspector != null && qaFolder == null)
                            actions.Add(new ActionChoice("Kiểm tra", () => InspectBackpackTool3D(selectedDef, tmpl)));
                        else if (paperTex != null && inspector != null && qaFolder == null)
                            actions.Add(new ActionChoice("Kiểm tra", () => InspectBackpackPaper3D(selectedDef, paperTex)));
                    }

                    // 2. Combine CH02 drawing if ready
                    if (selectedDef.CanCombine && InventoryService.CanAssembleCh02Drawing(State))
                    {
                        actions.Add(new ActionChoice("Ghép 3 mảnh tranh", () =>
                            SaveEvidence(EvidenceRegistry.Drawing, () =>
                            {
                                scene.atmosphere?.From("paper", player.Feet + Vector3.up, 0.60f);
                                backpackSelection = BackpackCatalog.Ch02DrawFull;
                                ui.Caption("Ba mảnh tranh khớp mép thành hình cánh cửa có ổ khóa.", 4.6f, aboveModal: true);
                                DrawBackpack();
                            })));
                    }

                    // 3. Retrieve if placed
                    if (selectedEntry.status == BackpackItemStatus.Placed)
                    {
                        string placedAnchor = selectedEntry.placedAnchorId;
                        actions.Add(new ActionChoice("Thu về ba lô", () =>
                            SaveCheckpoint(() => casework.RetrieveItemFromAnchorAsync(placedAnchor), () =>
                            {
                                scene.atmosphere?.From("paper", player.Feet + Vector3.up, 0.50f);
                                handPresenter?.SyncState(State, scene, hideViewModel: true);
                                DrawBackpack();
                            })));
                    }

                    // 4. Equip / Stow + Quick Slot
                    bool canHoldNow = selectedDef.CanEquip && !isMemory &&
                                      (selectedDef.Kind != ItemKind.LayerBoundPaper || State.layer == Reality.B);
                    if (canHoldNow)
                    {
                        bool eq = selectedEntry.status == BackpackItemStatus.Equipped;
                        actions.Add(new ActionChoice(eq ? "Cất khỏi tay" : "Cầm ra tay", () =>
                            SaveCheckpoint(() => casework.EquipItemAsync(eq ? "" : selectedDef.Id), () =>
                            {
                                handPresenter?.SyncState(State, scene, hideViewModel: false);
                                if (!eq) CloseModal();
                                else DrawBackpack();
                            })));
                        AddQuickSlotAction(actions, selectedDef, selectedEntry);
                    }

                    // 5. Place beside F101 if near clipboard in Layer B
                    if (!isMemory && selectedEntry.status != BackpackItemStatus.Placed && selectedDef.CanPlace && State.layer == Reality.B)
                    {
                        bool nearAn02 = transaction.AtRequiredPlace(player.Feet, RealityTransaction.WardClipboard) ||
                                        AmbientDreadDirector.ClassifyZone(player.Feet) == "LOC03";
                        if (nearAn02)
                        {
                            actions.Add(new ActionChoice("Kẹp cạnh phiếu F101", () =>
                                SaveCheckpoint(() => casework.PlaceItemAtAnchorAsync(selectedDef.Id, WorldAnchors.An02CompareClip), () =>
                                {
                                    scene.atmosphere?.From("clipSnap", new Vector3(5.74f, 1.36f, 5.79f), 0.72f);
                                    handPresenter?.SyncState(State, scene, hideViewModel: true);
                                    ui.Caption($"Đã kẹp {selectedDef.ShortLabel} cạnh phiếu F101.", 3.0f, aboveModal: true);
                                    DrawBackpack();
                                })));
                        }
                    }
                }
            }

            bool show3DStage = backpackStage != null && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
            if (show3DStage)
                backpackStage.Open(State, backpackCompartment, backpackSelection, triggerEcho, ActiveVolume());

            Action openJournal = IsJournalAllowedInBackpack
                ? () =>
                {
                    backpackStage?.Close();
                    journalReturn = () => OpenBackpack(backpackCompartment);
                    DrawJournal(JournalTab.Documents, pageTurn: !Options.reduceMotion);
                }
                : null;

            ui.ShowInventory2(
                filterIdx,
                idx =>
                {
                    backpackCompartment = idx == 1 ? BackpackCompartment.Papers : BackpackCompartment.Tools;
                    backpackSelection = null;
                    DrawBackpack();
                },
                tiles,
                emptyCopy,
                selTitle,
                selBrief,
                actions.ToArray(),
                openPocketNote: () => OpenPocketNote(false),
                openFullJournalIfAllowed: openJournal,
                close: CloseModal,
                onCloseBag: () => backpackStage?.Close(),
                show3DStageOnLeft: show3DStage);
        }

        private GameObject ResolveInventory3DTemplate(BackpackItemDef def)
        {
            if (def == null) return null;
            if (def.Id == BackpackCatalog.GateKey)
                return scene.InspectTemplate("GATE_KEY") ?? handPresenter?.GetInspectTemplate(def.Id);
            if (def.Id == BackpackCatalog.WingCKey)
                return scene.InspectTemplate("WINGC_KEY") ?? handPresenter?.GetInspectTemplate(def.Id);
            if (!string.IsNullOrEmpty(def.InspectModelId))
            {
                var t = scene.InspectTemplate(def.InspectModelId);
                if (t != null) return t;
            }
            return handPresenter?.GetInspectTemplate(def.Id);
        }

        private void InspectBackpackTool3D(BackpackItemDef def, GameObject template)
        {
            backpackStage?.Close();
            scene.atmosphere?.From("keyJingle", player.Eye.transform.position + player.Eye.transform.forward * 0.35f, 0.36f);
            Texture tagTex = def.Id == BackpackCatalog.WingCKey ? Image("Doc_KeyCTag") : null;
            inspector.Open(template, null, 1f, ActiveVolume(), observationId: def.Id, standaloneCenter: true);
            void BackToBag()
            {
                inspector.Close();
                OpenBackpack(BackpackCompartment.Tools);
            }
            var chips = new List<ActionChoice>();
            if (tagTex != null)
            {
                chips.Add(new ActionChoice("Đọc thẻ giấy", () =>
                {
                    inspector.Open(null, tagTex, (float)tagTex.width / Mathf.Max(1, tagTex.height),
                        ActiveVolume(), observationId: def.Id + "_TAG", standaloneCenter: true);
                }));
            }
            if (def.CanEquip)
            {
                chips.Add(new ActionChoice("Cầm ra tay [G]", () =>
                    SaveCheckpoint(() => casework.EquipItemAsync(def.Id), () =>
                    {
                        inspector.Close();
                        CloseModal();
                        handPresenter?.SyncState(State, scene, hideViewModel: false);
                    })));
            }
            chips.Add(new ActionChoice("Đóng [Esc]", BackToBag));
            ui.ShowStandaloneOverlay("Kéo xoay 360° · Cuộn thu phóng · Esc quay lại ba lô", BackToBag,
                onClose: () => inspector.Close(), microChips: chips.ToArray(),
                onSecondaryKey: def.CanEquip ? () => SaveCheckpoint(() => casework.EquipItemAsync(def.Id), () =>
                {
                    inspector.Close();
                    CloseModal();
                    handPresenter?.SyncState(State, scene, hideViewModel: false);
                }) : null);
        }

        private void InspectBackpackPaper3D(BackpackItemDef def, Texture frontTex)
        {
            backpackStage?.Close();
            scene.atmosphere?.From("paper", player.Eye.transform.position + player.Eye.transform.forward * 0.35f, 0.45f);
            float aspect = frontTex != null ? (float)frontTex.width / Mathf.Max(1, frontTex.height) : 0.75f;
            Texture backTex = !string.IsNullOrEmpty(def.BackTexture) ? Image(def.BackTexture) : null;
            inspector.Open(null, frontTex, aspect, ActiveVolume(), backTexture: backTex, observationId: def.Id, standaloneCenter: true);
            void BackToBag()
            {
                inspector.Close();
                OpenBackpack(BackpackCompartment.Papers);
            }
            void ShowLarge()
            {
                inspector.Close();
                ui.Show(def.Title, def.Title, def.Description, new[]
                {
                    new ActionChoice("Quay lại xem 3D", () => InspectBackpackPaper3D(def, frontTex)),
                    new ActionChoice("Quay lại ba lô", BackToBag)
                }, BackToBag, frontTex, largePrint: true);
            }
            var chips = new List<ActionChoice>
            {
                new("Chữ lớn [L]", ShowLarge),
                new("Đóng [Esc]", BackToBag)
            };
            ui.ShowStandaloneOverlay(
                backTex != null ? "Kéo xoay · Cuộn thu phóng · Chuột phải / Space lật · L chữ lớn · Esc đóng"
                                : "Kéo xoay · Cuộn thu phóng · L chữ lớn · Esc đóng",
                BackToBag, onClose: () => inspector.Close(), microChips: chips.ToArray(), onLargePrintKey: ShowLarge);
        }

        private static string SurfaceHeading(string surfaceId) => surfaceId switch
        {
            WorldAnchors.SurfaceAn02Clip => "Kẹp đối chiếu cạnh F101",
            WorldAnchors.SurfaceStaffDesk => "Bàn điều dưỡng",
            WorldAnchors.SurfaceReceptionDesk => "Quầy tiếp nhận",
            WorldAnchors.SurfaceWard101Table => "Bàn Phòng 101",
            _ => "Mặt bàn đối chiếu"
        };

        private static string SurfaceShortNote(string surfaceId) => surfaceId switch
        {
            WorldAnchors.SurfaceAn02Clip => "Đặt tờ giấy kẹp ngay bên cạnh phiếu F101 để tự đối chiếu.",
            WorldAnchors.SurfaceStaffDesk => "Đặt tài liệu cạnh nhau trên mặt bàn để đối chiếu.",
            WorldAnchors.SurfaceReceptionDesk => "Đặt tài liệu lên mặt quầy tiếp nhận.",
            WorldAnchors.SurfaceWard101Table => "Đặt tài liệu hoặc mảnh tranh lên mặt bàn Phòng 101.",
            _ => "Đặt tài liệu để đối chiếu."
        };

        private void InteractComparisonSurface(string surfaceId)
        {
            if (State == null || busy) return;
            string[] slots = WorldAnchors.SlotsForSurface(surfaceId);
            if (slots.Length == 0) return;

            var equipped = InventoryService.GetEquipped(State);
            if (equipped != null && WorldUseResolver.Resolve(State, equipped.itemDefId, surfaceId).Outcome == WorldUseOutcome.PlacePaperOnAnchor)
            {
                string targetSlot = null;
                for (int i = 0; i < slots.Length; i++)
                {
                    if (InventoryService.GetPlacedAt(State, slots[i]) == null) { targetSlot = slots[i]; break; }
                }
                targetSlot ??= slots[0];
                var def = BackpackCatalog.Get(equipped.itemDefId);
                string sfx = surfaceId == WorldAnchors.SurfaceAn02Clip ? "clipSnap" : "paper";
                SaveCheckpoint(() => casework.PlaceItemAtAnchorAsync(equipped.itemDefId, targetSlot), () =>
                {
                    scene.atmosphere?.From(sfx, player.Feet + Vector3.up, 0.72f);
                    handPresenter?.SyncState(State, scene, hideViewModel: false);
                    ui.Caption($"Đã đặt {def?.ShortLabel ?? "tài liệu"} lên {SurfaceHeading(surfaceId).ToLowerInvariant()}.", 3.2f);
                });
                return;
            }

            if (surfaceId == WorldAnchors.SurfaceWard101Table && InventoryService.CanAssembleCh02Drawing(State))
            {
                SaveEvidence(EvidenceRegistry.Drawing, () =>
                {
                    scene.atmosphere?.From("paper", player.Feet + Vector3.up, 0.65f);
                    ui.Caption("Ba mảnh tranh khớp mép trên mặt bàn thành hình cánh cửa có ổ khóa.", 4.6f);
                });
                return;
            }

            var choices = new List<ActionChoice>();
            var sb = new StringBuilder();
            sb.AppendLine(SurfaceShortNote(surfaceId));
            sb.AppendLine();

            int occupiedCount = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                string slotId = slots[i];
                var placed = InventoryService.GetPlacedAt(State, slotId);
                if (placed == null) continue;
                occupiedCount++;
                var def = BackpackCatalog.Get(placed.itemDefId);
                string title = def?.Title ?? placed.itemDefId;
                sb.Append("• ").AppendLine(title);

                if (def != null && !string.IsNullOrEmpty(def.LinkedObservationId) &&
                    ObservationRegistry.Get(def.LinkedObservationId) is ObservationSpec obsSpec)
                {
                    choices.Add(new ActionChoice($"Xem {def.ShortLabel}", () =>
                        OpenObservationInspector(obsSpec, () => InteractComparisonSurface(surfaceId))));
                }
                else if (def != null && !string.IsNullOrEmpty(def.FrontTexture))
                {
                    var tex = Image(def.FrontTexture);
                    if (tex != null)
                        choices.Add(new ActionChoice($"Xem {def.ShortLabel}", () => InspectBackpackPaper3D(def, tex)));
                }

                choices.Add(new ActionChoice($"Rút {def?.ShortLabel ?? "tài liệu"} về ba lô", () =>
                    SaveCheckpoint(() => casework.RetrieveItemFromAnchorAsync(slotId), () =>
                    {
                        scene.atmosphere?.From("paper", player.Feet + Vector3.up, 0.52f);
                        handPresenter?.SyncState(State, scene, hideViewModel: false);
                        CloseModal();
                    })));
            }

            if (occupiedCount == 0)
                sb.AppendLine("Hiện đang trống.");

            var placeables = new List<BackpackItemDef>();
            foreach (var item in State.backpackItems)
            {
                if (item.status == BackpackItemStatus.Placed || item.status == BackpackItemStatus.MemoryRef) continue;
                var def = BackpackCatalog.Get(item.itemDefId);
                if (def == null) continue;
                if (WorldUseResolver.Resolve(State, def.Id, surfaceId).Outcome == WorldUseOutcome.PlacePaperOnAnchor)
                    placeables.Add(def);
            }

            string freeSlot = null;
            for (int i = 0; i < slots.Length; i++)
                if (InventoryService.GetPlacedAt(State, slots[i]) == null) { freeSlot = slots[i]; break; }

            if (freeSlot != null && placeables.Count > 0)
            {
                string verb = surfaceId == WorldAnchors.SurfaceAn02Clip ? "Kẹp" : "Đặt";
                for (int i = 0; i < placeables.Count && i < 4; i++)
                {
                    var def = placeables[i];
                    string sfx = surfaceId == WorldAnchors.SurfaceAn02Clip ? "clipSnap" : "paper";
                    choices.Add(new ActionChoice($"{verb} {def.ShortLabel}", () =>
                        SaveCheckpoint(() => casework.PlaceItemAtAnchorAsync(def.Id, freeSlot), () =>
                        {
                            scene.atmosphere?.From(sfx, player.Feet + Vector3.up, 0.72f);
                            handPresenter?.SyncState(State, scene, hideViewModel: false);
                            CloseModal();
                        })));
                }
            }

            if (occupiedCount >= 2 && IsJournalAllowedInBackpack)
            {
                choices.Add(new ActionChoice("Soi song song hai tài liệu", () =>
                {
                    journalReturn = CloseModal;
                    DrawJournal(JournalTab.Documents, pageTurn: !Options.reduceMotion);
                }));
            }

            choices.Add(new ActionChoice("Đóng", CloseModal));
            ui.Show(SurfaceHeading(surfaceId).ToUpperInvariant(), SurfaceHeading(surfaceId), sb.ToString().TrimEnd(), choices.ToArray(), CloseModal);
        }

        /// <summary>
        /// Core v3.6.2 / Phase 3 H08 Recovery at LOC08 Reality R:
        /// Lets Minh place and inspect the carried E10a envelope and personal phone directly on the
        /// Administrative Room desk to read any missing parts among the 4 independent sources.
        /// Strictly preserves independence of all 4 parts — NEVER auto-reads or auto-completes!
        /// </summary>
        private void InteractLoc08RecoveryDesk()
        {
            if (State == null || busy) return;
            if (State.chapter is not ("CH05" or "CH06") || State.layer != Reality.R || !GateService.G05(State))
            {
                ui.Caption("Bàn đối chiếu tài liệu trong Phòng Hành chính hiện chưa sẵn sàng.", 3.6f, true);
                return;
            }

            InventoryService.EnsureDefaultTools(State);
            bool hasEnv = InventoryService.HasPhysical(State, BackpackCatalog.E10aEnvelope);
            bool hasPhone = InventoryService.HasPhysical(State, BackpackCatalog.MinhPhone);
            bool envPlaced = InventoryService.GetPlacedAt(State, WorldAnchors.Loc08DeskEnvelope)?.itemDefId == BackpackCatalog.E10aEnvelope;
            bool phonePlaced = InventoryService.GetPlacedAt(State, WorldAnchors.Loc08DeskPhone)?.itemDefId == BackpackCatalog.MinhPhone;

            bool rLetter = State.Happened(E10aParts.Letter);
            bool rAppt = State.Happened(E10aParts.SignedAppointment);
            bool rPostpone = State.Happened(E10aParts.PostponementSms);
            bool rNews = State.Happened(E10aParts.PublicReportSummary);
            int readCount = E10aParts.ReadCount(State);

            var sb = new StringBuilder();
            sb.AppendLine("Mặt bàn Phòng Hành chính đủ sáng để trải lại phong bì chú Bảy gửi và điện thoại cá nhân, rà những nguồn chưa đọc trước khi kiểm tra báo cáo.");
            sb.AppendLine();
            sb.AppendLine($"• Trên mặt bàn: {(envPlaced ? "Đã đặt phong bì" : "Chưa đặt phong bì")}  ·  {(phonePlaced ? "Đã đặt điện thoại" : "Chưa đặt điện thoại")}");
            sb.AppendLine($"• Nguồn đã đọc ({readCount}/4 — từng phần độc lập, chỉ ghi nhận khi tự mở đọc):");
            sb.AppendLine($"   {(rLetter ? "✓" : "○")} Thư tay năm 2005 của Lan (trong phong bì)");
            sb.AppendLine($"   {(rAppt ? "✓" : "○")} Giấy hẹn & tin nhắn hẹn ngày 16/11 (giấy ngoài phong bì + tin nhắn cũ)");
            sb.AppendLine($"   {(rPostpone ? "✓" : "○")} Tin nhắn hoãn ca lúc 22:48 (trong điện thoại)");
            sb.AppendLine($"   {(rNews ? "✓" : "○")} Mẩu báo công khai ngày 19/11/2005 (trong phong bì)");
            if (readCount == 4)
            {
                sb.AppendLine();
                sb.Append("Tất cả bốn nguồn tài liệu ban đầu đã được kiểm chứng đầy đủ.");
            }

            var choices = new List<ActionChoice>();
            if (hasEnv)
            {
                if (!envPlaced)
                {
                    choices.Add(new ActionChoice("Đặt phong bì lên mặt bàn", () =>
                        SaveCheckpoint(() => casework.PlaceItemAtAnchorAsync(BackpackCatalog.E10aEnvelope, WorldAnchors.Loc08DeskEnvelope), () =>
                        {
                            scene.atmosphere?.From("paper", player.Feet + Vector3.up, 0.56f);
                            handPresenter?.SyncState(State, scene, hideViewModel: true);
                            InteractLoc08RecoveryDesk();
                        })));
                }
                else
                {
                    choices.Add(new ActionChoice("Thu phong bì về ba lô", () =>
                        SaveCheckpoint(() => casework.RetrieveItemFromAnchorAsync(WorldAnchors.Loc08DeskEnvelope), () =>
                        {
                            scene.atmosphere?.From("paper", player.Feet + Vector3.up, 0.48f);
                            handPresenter?.SyncState(State, scene, hideViewModel: true);
                            InteractLoc08RecoveryDesk();
                        })));
                }
                choices.Add(new ActionChoice("Mở kiểm tra phong bì 3D", () =>
                {
                    CloseModal();
                    OpenPhysicalEnvelope3D(returnToBackpack: false);
                }));
            }

            if (hasPhone)
            {
                if (!phonePlaced)
                {
                    choices.Add(new ActionChoice("Đặt điện thoại lên mặt bàn", () =>
                        SaveCheckpoint(() => casework.PlaceItemAtAnchorAsync(BackpackCatalog.MinhPhone, WorldAnchors.Loc08DeskPhone), () =>
                        {
                            scene.atmosphere?.From("doorLatch", player.Feet + Vector3.up, 0.42f);
                            handPresenter?.SyncState(State, scene, hideViewModel: true);
                            InteractLoc08RecoveryDesk();
                        })));
                }
                else
                {
                    choices.Add(new ActionChoice("Thu điện thoại về túi", () =>
                        SaveCheckpoint(() => casework.RetrieveItemFromAnchorAsync(WorldAnchors.Loc08DeskPhone), () =>
                        {
                            scene.atmosphere?.From("bagZip", player.Feet + Vector3.up, 0.42f);
                            handPresenter?.SyncState(State, scene, hideViewModel: true);
                            InteractLoc08RecoveryDesk();
                        })));
                }
                choices.Add(new ActionChoice("Mở kiểm tra điện thoại", () =>
                {
                    CloseModal();
                    OpenPhone(0, returnToBackpack: false);
                }));
            }

            choices.Add(new ActionChoice("Đóng", CloseModal));
            ui.Show("BÀN ĐỐI CHIẾU · PHÒNG HÀNH CHÍNH", "Kiểm lại phong bì & điện thoại", sb.ToString().TrimEnd(), choices.ToArray(), CloseModal);
        }

        private bool TryUseHeldTapeOnAn04()
        {
            if (State == null || busy || State.chapter != "CH04") return false;
            var equipped = InventoryService.GetEquipped(State);
            if (equipped == null || equipped.itemDefId != BackpackCatalog.Ch04Tape) return false;
            var res = WorldUseResolver.Resolve(State, BackpackCatalog.Ch04Tape, "AN04");
            if (res.Outcome == WorldUseOutcome.NotApplicableFeedback)
            {
                scene.atmosphere?.From("doorLatch", player.Feet + Vector3.up, 0.50f);
                ui.Caption(res.Feedback, 4.4f, true);
                return true;
            }
            if (res.Outcome == WorldUseOutcome.PlayTapeAtDutyDesk)
            {
                handPresenter?.TriggerUseAnimation();
                scene.atmosphere?.From("tapeClick", player.Eye.transform.position + player.Eye.transform.forward * 0.4f, 0.78f);
                Ch04DutyRoom();
                return true;
            }
            return false;
        }
    }
}
