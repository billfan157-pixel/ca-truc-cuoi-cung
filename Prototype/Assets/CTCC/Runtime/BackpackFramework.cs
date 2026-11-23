using System;
using System.Collections.Generic;
using System.Text;

namespace CTCC
{
    public enum ItemKind
    {
        EquippableTool = 0,
        PersistentPhysical = 1,
        LayerBoundPaper = 2,
        MemoryOnly = 3
    }

    public enum BackpackCompartment
    {
        Tools = 0,
        Papers = 1,
        PocketNote = 2
    }

    public enum BackpackItemStatus
    {
        InBag = 0,
        Equipped = 1,
        Placed = 2,
        MemoryRef = 3
    }

    [Serializable]
    public sealed class BackpackEntry
    {
        public string instanceId;
        public string itemDefId;
        public BackpackItemStatus status;
        public Reality originLayer;
        public string originLoc;
        public string acquiredChapter;
        public string placedAnchorId;
        public List<string> viewedPages = new();
        public int itemRevision;

        public bool HasViewed(string page) =>
            viewedPages != null && !string.IsNullOrEmpty(page) && viewedPages.Contains(page);

        public bool MarkViewed(string page)
        {
            viewedPages ??= new();
            if (string.IsNullOrEmpty(page) || viewedPages.Contains(page)) return false;
            viewedPages.Add(page);
            itemRevision++;
            return true;
        }
    }

    public sealed class BackpackItemDef
    {
        public readonly string Id;
        public readonly string Title;
        public readonly string ShortLabel;
        public readonly string Description;
        public readonly string ProvenanceLabel;
        public readonly ItemKind Kind;
        public readonly BackpackCompartment Compartment;
        public readonly bool CanEquip;
        public readonly bool CanPlace;
        public readonly string LinkedObservationId;
        public readonly string LinkedEvidenceId;
        public readonly string FrontTexture;
        public readonly string BackTexture;

        public BackpackItemDef(
            string id,
            string title,
            string shortLabel,
            string description,
            string provenanceLabel,
            ItemKind kind,
            BackpackCompartment compartment,
            bool canEquip,
            bool canPlace,
            string linkedObservationId = null,
            string linkedEvidenceId = null,
            string frontTexture = null,
            string backTexture = null)
        {
            Id = id;
            Title = title;
            ShortLabel = shortLabel;
            Description = description;
            ProvenanceLabel = provenanceLabel;
            Kind = kind;
            Compartment = compartment;
            CanEquip = canEquip;
            CanPlace = canPlace;
            LinkedObservationId = linkedObservationId;
            LinkedEvidenceId = linkedEvidenceId;
            FrontTexture = frontTexture;
            BackTexture = backTexture;
        }
    }

    public static class WorldAnchors
    {
        public const string An02CompareClip = "AN02_CLIP";
        public const string StaffDeskCompare = "STAFF_DESK_COMPARE";

        public static bool Known(string anchorId) =>
            anchorId is An02CompareClip or StaffDeskCompare;

        public static string Label(string anchorId) => anchorId switch
        {
            An02CompareClip => "Kẹp đối chiếu cạnh F101 (Phòng 101)",
            StaffDeskCompare => "Bàn điều dưỡng (Hốc STAFF)",
            _ => ""
        };
    }

    public static class BackpackCatalog
    {
        public const string Flashlight = "FLASHLIGHT";
        public const string GateKey = "KEY_GATE";
        public const string WingCKey = "KEY_WING_C";
        public const string E10aEnvelope = "E10A_ENVELOPE";
        public const string BoardAssignment = "BOARD_ASSIGNMENT";
        public const string PaperObs101B = "PAPER_OBS101B";
        public const string PaperObs101C = "PAPER_OBS101C";
        public const string PaperObs101D = "PAPER_OBS101D";
        public const string PaperShiftLog = "PAPER_SHIFT_LOG";

        private static readonly BackpackItemDef[] Ordered =
        {
            new(
                Flashlight,
                "Đèn pin cầm tay",
                "Đèn pin",
                "Đèn pin cầm tay của Minh. Có thể bật/tắt bằng phím F (hoặc L) và điều chỉnh góc soi lên bảng tên, dấu sàn hay mặt giấy.",
                "Đồ mang theo · Lớp R",
                ItemKind.EquippableTool,
                BackpackCompartment.Tools,
                canEquip: false,
                canPlace: false),
            new(
                GateKey,
                "Chìa cổng phụ An Sinh",
                "Chìa cổng phụ",
                "Chìa sắt đã sờn mép rút từ bì thư của Bảy. Khớp với ổ khóa cổng phụ phía trước chốt bảo vệ.",
                "Từ bì thư E10a · Lớp R · LOC01",
                ItemKind.EquippableTool,
                BackpackCompartment.Tools,
                canEquip: true,
                canPlace: false),
            new(
                WingCKey,
                "Chìa khu C (đã thay khóa)",
                "Chìa khu C",
                "Chìa đồng cũ có thẻ giấy nhỏ ghi ký hiệu khu C. Bảy chú thích ổ khóa khu C đã thay từ trước khi thanh lý — hiện chưa rõ dùng vào đâu trong đêm nay.",
                "Từ bì thư E10a · Lớp R · LOC01",
                ItemKind.EquippableTool,
                BackpackCompartment.Tools,
                canEquip: true,
                canPlace: false),
            new(
                E10aEnvelope,
                "Bì thư của Bảy (E10a)",
                "Bì thư E10a",
                "Phong bì giấy kraft chứa tờ hẹn lúc 23 giờ của Bảy, tin nhắn báo hoãn, thư cũ của Lan và mẩu báo vụ cháy rạng sáng 18/11/2005 (13 người tử vong).",
                "Vật chứng lớp R · LOC01 · 17/11/2012",
                ItemKind.PersistentPhysical,
                BackpackCompartment.Papers,
                canEquip: false,
                canPlace: false,
                linkedEvidenceId: "E10a",
                frontTexture: "Doc_BayEnvelopeFront",
                backTexture: "Doc_BayLetter"),
            new(
                BoardAssignment,
                "Bảng kẹp bàn giao ca (BOARD)",
                "Bảng kẹp ca trực",
                "Tờ nhiệm vụ bàn giao nhận từ quầy tiếp nhận trong ca trực 17/11/2005, yêu cầu kiểm tra phòng 101 trước khi ký phiếu F101 tại bảng kẹp trong buồng.",
                "Giấy tạm lớp B · LOC02 · 17/11/2005",
                ItemKind.LayerBoundPaper,
                BackpackCompartment.Papers,
                canEquip: false,
                canPlace: false,
                linkedObservationId: ObservationRegistry.F101Sheet,
                frontTexture: "F101",
                backTexture: "F101_Back"),
            new(
                PaperObs101B,
                "Thẻ bàn giao giường 4 (AS-05B)",
                "Thẻ AS-05B",
                "Thẻ giường tạm ngày 17/11/2005 khoanh tròn 'CHƯA BÀN GIAO', trống ô chữ ký người nhận. Chỉ tồn tại vật lý trong lớp B; có thể kẹp cạnh F101 để đối chiếu.",
                "Giấy tạm lớp B · LOC03 · 17/11/2005",
                ItemKind.LayerBoundPaper,
                BackpackCompartment.Papers,
                canEquip: true,
                canPlace: true,
                linkedObservationId: ObservationRegistry.Bed4Card,
                frontTexture: "ObsCard",
                backTexture: "ObsCard_Back"),
            new(
                PaperObs101C,
                "Phiếu chuyển giường 02",
                "Phiếu giường 02",
                "Phiếu chuyển khoa Nội B lúc 19g20 ngày 17/11/2005 có đủ hai chữ ký giao–nhận và dấu đỏ. Dùng để đối chiếu quy trình hoàn tất.",
                "Giấy tạm lớp B · LOC03 · 17/11/2005",
                ItemKind.LayerBoundPaper,
                BackpackCompartment.Papers,
                canEquip: true,
                canPlace: true,
                linkedObservationId: ObservationRegistry.Bed2Transfer,
                frontTexture: "Doc_Bed2Transfer",
                backTexture: "Doc_Bed2Transfer_Back"),
            new(
                PaperObs101D,
                "Phiếu phát thuốc giường 03",
                "Phiếu thuốc giường 03",
                "Lịch thuốc giường 03 đã khóa lúc 10g15 ngày 17/11/2005 khi bệnh nhân xuất viện. Nói về giường 03, không liên quan trực tiếp đến vị trí thứ tư.",
                "Giấy tạm lớp B · LOC03 · 17/11/2005",
                ItemKind.LayerBoundPaper,
                BackpackCompartment.Papers,
                canEquip: true,
                canPlace: true,
                linkedObservationId: ObservationRegistry.Bed3Meds,
                frontTexture: "Doc_Bed3Meds",
                backTexture: "Doc_Bed3Meds_Back"),
            new(
                PaperShiftLog,
                "Quy chế bàn giao ca đêm AS-05",
                "Quy chế AS-05",
                "Trích quy định bàn giao từ ngăn kéo bàn điều dưỡng STAFF. Nhắc kiểm đủ hai chữ ký bàn giao kể cả giường kê tạm trước khi chốt số 0.",
                "Giấy tạm lớp B · STAFF · 17/11/2005",
                ItemKind.LayerBoundPaper,
                BackpackCompartment.Papers,
                canEquip: true,
                canPlace: true,
                linkedObservationId: ObservationRegistry.ShiftGuide,
                frontTexture: "Doc_ShiftGuide",
                backTexture: "Doc_ShiftGuide_Back")
        };

        private static readonly Dictionary<string, BackpackItemDef> ById;
        private static readonly Dictionary<string, BackpackItemDef> ByObservation;

        static BackpackCatalog()
        {
            ById = new Dictionary<string, BackpackItemDef>(StringComparer.Ordinal);
            ByObservation = new Dictionary<string, BackpackItemDef>(StringComparer.Ordinal);
            for (int i = 0; i < Ordered.Length; i++)
            {
                var def = Ordered[i];
                ById[def.Id] = def;
                if (!string.IsNullOrEmpty(def.LinkedObservationId) && def.Id != BoardAssignment)
                    ByObservation[def.LinkedObservationId] = def;
            }
        }

        public static IReadOnlyList<BackpackItemDef> All => Ordered;
        public static bool Registered(string id) => !string.IsNullOrEmpty(id) && ById.ContainsKey(id);
        public static BackpackItemDef Get(string id) => !string.IsNullOrEmpty(id) && ById.TryGetValue(id, out var d) ? d : null;
        public static BackpackItemDef ForObservation(string obsId) =>
            !string.IsNullOrEmpty(obsId) && ByObservation.TryGetValue(obsId, out var d) ? d : null;
    }

    public static class InventoryService
    {
        public static void EnsureDefaultTools(Investigation state)
        {
            if (state == null) return;
            state.backpackItems ??= new();
            if (FindByDef(state, BackpackCatalog.Flashlight) == null)
            {
                state.backpackItems.Add(new BackpackEntry
                {
                    instanceId = "ITEM_" + BackpackCatalog.Flashlight,
                    itemDefId = BackpackCatalog.Flashlight,
                    status = BackpackItemStatus.InBag,
                    originLayer = Reality.R,
                    originLoc = "LOC01",
                    acquiredChapter = "CH01",
                    placedAnchorId = ""
                });
            }
        }

        public static BackpackEntry FindByDef(Investigation state, string itemDefId)
        {
            if (state?.backpackItems == null || string.IsNullOrEmpty(itemDefId)) return null;
            for (int i = 0; i < state.backpackItems.Count; i++)
            {
                var e = state.backpackItems[i];
                if (e != null && string.Equals(e.itemDefId, itemDefId, StringComparison.Ordinal))
                    return e;
            }
            return null;
        }

        public static BackpackEntry FindByInstance(Investigation state, string instanceId)
        {
            if (state?.backpackItems == null || string.IsNullOrEmpty(instanceId)) return null;
            for (int i = 0; i < state.backpackItems.Count; i++)
            {
                var e = state.backpackItems[i];
                if (e != null && string.Equals(e.instanceId, instanceId, StringComparison.Ordinal))
                    return e;
            }
            return null;
        }

        public static BackpackEntry GetEquipped(Investigation state) =>
            string.IsNullOrEmpty(state?.equippedInstanceId) ? null : FindByInstance(state, state.equippedInstanceId);

        public static BackpackEntry GetPlacedAt(Investigation state, string anchorId)
        {
            if (state?.backpackItems == null || string.IsNullOrEmpty(anchorId)) return null;
            for (int i = 0; i < state.backpackItems.Count; i++)
            {
                var e = state.backpackItems[i];
                if (e != null && e.status == BackpackItemStatus.Placed &&
                    string.Equals(e.placedAnchorId, anchorId, StringComparison.Ordinal))
                    return e;
            }
            return null;
        }

        public static bool HasPhysical(Investigation state, string itemDefId)
        {
            var e = FindByDef(state, itemDefId);
            return e != null && e.status != BackpackItemStatus.MemoryRef;
        }

        public static bool TryAcquire(Investigation candidate, string itemDefId, Reality layer, string location, out BackpackEntry entry)
        {
            entry = null;
            var def = BackpackCatalog.Get(itemDefId);
            if (candidate == null || def == null) return false;
            candidate.backpackItems ??= new();

            // LayerBound papers cannot be physically acquired in Layer R!
            if (def.Kind == ItemKind.LayerBoundPaper && layer != Reality.B)
                return false;

            var existing = FindByDef(candidate, itemDefId);
            if (existing != null)
            {
                if (existing.status == BackpackItemStatus.MemoryRef && def.Kind == ItemKind.LayerBoundPaper && layer == Reality.B)
                {
                    existing.status = BackpackItemStatus.InBag;
                    existing.placedAnchorId = "";
                    existing.itemRevision++;
                }
                entry = existing;
                return true;
            }

            entry = new BackpackEntry
            {
                instanceId = "ITEM_" + itemDefId,
                itemDefId = itemDefId,
                status = BackpackItemStatus.InBag,
                originLayer = layer,
                originLoc = location ?? "LOC01",
                acquiredChapter = candidate.chapter ?? "CH01",
                placedAnchorId = "",
                itemRevision = 1
            };
            candidate.backpackItems.Add(entry);
            return true;
        }

        public static bool TryEquip(Investigation candidate, string itemDefId)
        {
            if (candidate == null) return false;
            if (string.IsNullOrEmpty(itemDefId))
            {
                UnequipAll(candidate);
                return true;
            }
            var def = BackpackCatalog.Get(itemDefId);
            var entry = FindByDef(candidate, itemDefId);
            if (def == null || entry == null || !def.CanEquip) return false;
            if (entry.status == BackpackItemStatus.MemoryRef) return false;
            if (def.Kind == ItemKind.LayerBoundPaper && candidate.layer != Reality.B) return false;

            UnequipAll(candidate);
            entry.status = BackpackItemStatus.Equipped;
            entry.placedAnchorId = "";
            entry.itemRevision++;
            candidate.equippedInstanceId = entry.instanceId;
            return true;
        }

        public static void UnequipAll(Investigation candidate)
        {
            if (candidate?.backpackItems == null) return;
            for (int i = 0; i < candidate.backpackItems.Count; i++)
            {
                var e = candidate.backpackItems[i];
                if (e != null && e.status == BackpackItemStatus.Equipped)
                {
                    e.status = BackpackItemStatus.InBag;
                    e.placedAnchorId = "";
                }
            }
            candidate.equippedInstanceId = "";
        }

        public static bool TryPlaceAtAnchor(Investigation candidate, string itemDefId, string anchorId)
        {
            if (candidate == null || !WorldAnchors.Known(anchorId)) return false;
            var def = BackpackCatalog.Get(itemDefId);
            var entry = FindByDef(candidate, itemDefId);
            if (def == null || entry == null || !def.CanPlace) return false;
            if (entry.status == BackpackItemStatus.MemoryRef) return false;
            if (def.Kind == ItemKind.LayerBoundPaper && candidate.layer != Reality.B) return false;

            // Return any previous occupant of the same anchor cleanly back to bag
            var prev = GetPlacedAt(candidate, anchorId);
            if (prev != null && prev != entry)
            {
                prev.status = BackpackItemStatus.InBag;
                prev.placedAnchorId = "";
                prev.itemRevision++;
            }

            if (candidate.equippedInstanceId == entry.instanceId)
                candidate.equippedInstanceId = "";
            entry.status = BackpackItemStatus.Placed;
            entry.placedAnchorId = anchorId;
            entry.itemRevision++;
            return true;
        }

        public static bool TryRetrieveFromAnchor(Investigation candidate, string anchorId, bool equip = false)
        {
            var entry = GetPlacedAt(candidate, anchorId);
            if (entry == null) return false;
            entry.status = BackpackItemStatus.InBag;
            entry.placedAnchorId = "";
            entry.itemRevision++;
            if (equip) TryEquip(candidate, entry.itemDefId);
            return true;
        }

        public static bool TryReturnToWorldFixture(Investigation candidate, string itemDefId)
        {
            var def = BackpackCatalog.Get(itemDefId);
            var entry = FindByDef(candidate, itemDefId);
            if (def == null || entry == null || def.Kind != ItemKind.LayerBoundPaper) return false;
            if (candidate.equippedInstanceId == entry.instanceId)
                candidate.equippedInstanceId = "";
            candidate.backpackItems.Remove(entry);
            return true;
        }

        /// <summary>
        /// Walkthrough B06 atomic B->R reconciliation:
        /// Every LayerBoundPaper (in bag, held in hand, or placed on an AN02/STAFF anchor) is returned
        /// to Layer B and preserved strictly as a non-physical MemoryRef (cannot be equipped/placed/exported in R).
        /// </summary>
        public static void ReconcileLayerBoundToMemory(Investigation candidate)
        {
            if (candidate?.backpackItems == null) return;
            for (int i = 0; i < candidate.backpackItems.Count; i++)
            {
                var entry = candidate.backpackItems[i];
                if (entry == null) continue;
                var def = BackpackCatalog.Get(entry.itemDefId);
                if (def == null || def.Kind != ItemKind.LayerBoundPaper) continue;

                if (candidate.equippedInstanceId == entry.instanceId)
                    candidate.equippedInstanceId = "";
                entry.status = BackpackItemStatus.MemoryRef;
                entry.placedAnchorId = "";
                entry.itemRevision++;
            }

            // If player examined AS-05B in B without stowing it, preserve a read-only memory reference so
            // B08's conditional memory echo and provenance review remain accurate without inventing physical paper.
            if (candidate.layer == Reality.R && candidate.Happened("CH01-T08"))
            {
                EnsureMemoryRefIfExamined(candidate, ObservationRegistry.Bed4Card, BackpackCatalog.PaperObs101B, "LOC03");
                EnsureMemoryRefIfExamined(candidate, ObservationRegistry.Bed2Transfer, BackpackCatalog.PaperObs101C, "LOC03");
                EnsureMemoryRefIfExamined(candidate, ObservationRegistry.Bed3Meds, BackpackCatalog.PaperObs101D, "LOC03");
                EnsureMemoryRefIfExamined(candidate, ObservationRegistry.ShiftGuide, BackpackCatalog.PaperShiftLog, "STAFF");
            }
        }

        private static void EnsureMemoryRefIfExamined(Investigation candidate, string obsId, string itemDefId, string loc)
        {
            if (!HypothesisService.HasExaminedInLayerB(candidate, obsId)) return;
            if (FindByDef(candidate, itemDefId) != null) return;
            candidate.backpackItems.Add(new BackpackEntry
            {
                instanceId = "ITEM_" + itemDefId,
                itemDefId = itemDefId,
                status = BackpackItemStatus.MemoryRef,
                originLayer = Reality.B,
                originLoc = loc,
                acquiredChapter = "CH01",
                placedAnchorId = "",
                itemRevision = 1
            });
        }

        public static void MigrateFromLegacy(Investigation state)
        {
            if (state == null) return;
            EnsureDefaultTools(state);
            if (state.Happened("E10a.SMSRead"))
            {
                if (TryAcquire(state, BackpackCatalog.E10aEnvelope, Reality.R, "LOC01", out var env))
                {
                    env.MarkViewed("FLAP");
                    env.MarkViewed("BAY_SMS");
                    env.MarkViewed("KEYS_EXTRACTED");
                }
                TryAcquire(state, BackpackCatalog.GateKey, Reality.R, "LOC01", out _);
                TryAcquire(state, BackpackCatalog.WingCKey, Reality.R, "LOC01", out _);
            }
            if (state.Happened("CH01-T05"))
            {
                var board = FindByDef(state, BackpackCatalog.BoardAssignment);
                if (board == null)
                {
                    state.backpackItems.Add(new BackpackEntry
                    {
                        instanceId = "ITEM_" + BackpackCatalog.BoardAssignment,
                        itemDefId = BackpackCatalog.BoardAssignment,
                        status = state.layer == Reality.B && !state.Happened("CH01-T08")
                            ? BackpackItemStatus.InBag : BackpackItemStatus.MemoryRef,
                        originLayer = Reality.B,
                        originLoc = "LOC02",
                        acquiredChapter = "CH01",
                        placedAnchorId = "",
                        itemRevision = 1
                    });
                }
            }
            if (state.layer != Reality.B)
                ReconcileLayerBoundToMemory(state);
        }

        public static bool ValidState(Investigation state)
        {
            if (state == null || state.backpackItems == null) return false;
            var seenInstances = new HashSet<string>(StringComparer.Ordinal);
            var seenDefs = new HashSet<string>(StringComparer.Ordinal);
            var usedAnchors = new HashSet<string>(StringComparer.Ordinal);
            string equippedSeen = "";

            for (int i = 0; i < state.backpackItems.Count; i++)
            {
                var e = state.backpackItems[i];
                if (e == null || string.IsNullOrEmpty(e.instanceId) || string.IsNullOrEmpty(e.itemDefId))
                    return false;
                var def = BackpackCatalog.Get(e.itemDefId);
                if (def == null || !seenInstances.Add(e.instanceId) || !seenDefs.Add(e.itemDefId))
                    return false;
                if (!Enum.IsDefined(typeof(BackpackItemStatus), e.status) || !Enum.IsDefined(typeof(Reality), e.originLayer))
                    return false;

                // LayerBound papers MUST NEVER be physically held, stowed, or placed in Layer R!
                if (def.Kind == ItemKind.LayerBoundPaper && state.layer != Reality.B && e.status != BackpackItemStatus.MemoryRef)
                    return false;

                if (e.status == BackpackItemStatus.Placed)
                {
                    if (!def.CanPlace || !WorldAnchors.Known(e.placedAnchorId) || !usedAnchors.Add(e.placedAnchorId))
                        return false;
                }
                else if (!string.IsNullOrEmpty(e.placedAnchorId))
                {
                    return false;
                }

                if (e.status == BackpackItemStatus.Equipped)
                {
                    if (!def.CanEquip || !string.IsNullOrEmpty(equippedSeen))
                        return false;
                    equippedSeen = e.instanceId;
                }
            }

            string declaredEquipped = state.equippedInstanceId ?? "";
            return string.Equals(declaredEquipped, equippedSeen, StringComparison.Ordinal);
        }
    }

    public enum WorldUseOutcome
    {
        NoMatch = 0,
        UnlockOuterGate = 1,
        WrongKeyForOuterGate = 2,
        NeedSmsBeforeGate = 3,
        PlacePaperOnAnchor = 4,
        GateRefusedAfterCollapse = 5,
        NotApplicableFeedback = 6
    }

    public readonly struct WorldUseResolution
    {
        public readonly WorldUseOutcome Outcome;
        public readonly string Feedback;
        public readonly string SuggestedItemDefId;

        public WorldUseResolution(WorldUseOutcome outcome, string feedback = "", string suggested = null)
        {
            Outcome = outcome;
            Feedback = feedback ?? "";
            SuggestedItemDefId = suggested;
        }
    }

    public static class WorldUseResolver
    {
        public static WorldUseResolution Resolve(Investigation state, string equippedItemDefId, string targetId)
        {
            if (state == null || string.IsNullOrEmpty(targetId))
                return new WorldUseResolution(WorldUseOutcome.NoMatch);

            if (targetId == "GATE-CONG" && !state.Happened("CH01-T02"))
            {
                if (equippedItemDefId == BackpackCatalog.WingCKey)
                {
                    bool hasGateKey = InventoryService.HasPhysical(state, BackpackCatalog.GateKey);
                    return new WorldUseResolution(
                        WorldUseOutcome.WrongKeyForOuterGate,
                        hasGateKey
                            ? "Chìa khu C đã thay khóa — không khớp ổ cổng phụ. Trong ba lô vẫn còn chiếc chìa cổng phụ."
                            : "Chìa khu C đã thay khóa — không khớp ổ cổng phụ này.",
                        hasGateKey ? BackpackCatalog.GateKey : null);
                }

                if (equippedItemDefId == BackpackCatalog.GateKey ||
                    (string.IsNullOrEmpty(equippedItemDefId) && InventoryService.HasPhysical(state, BackpackCatalog.GateKey)))
                {
                    if (!state.Happened("E10a.SMSRead"))
                        return new WorldUseResolution(
                            WorldUseOutcome.NeedSmsBeforeGate,
                            "Chìa khớp ổ, nhưng mình vẫn chưa đọc hết tin nhắn của chú Bảy trong bì thư.");
                    return new WorldUseResolution(WorldUseOutcome.UnlockOuterGate);
                }
            }

            if (targetId == "SEAM" && state.Happened("CH01-T08"))
            {
                if (equippedItemDefId is BackpackCatalog.GateKey or BackpackCatalog.WingCKey)
                {
                    return new WorldUseResolution(
                        WorldUseOutcome.GateRefusedAfterCollapse,
                        equippedItemDefId == BackpackCatalog.WingCKey
                            ? "Chìa khu C không liên quan tới cánh cổng này."
                            : "Ổ khóa không còn là vấn đề. Chiếc chìa cổng phụ không đổi được tình thế này.");
                }
            }

            if (targetId == "COMPARE_CLIP_AN02" && state.layer == Reality.B)
            {
                if (!string.IsNullOrEmpty(equippedItemDefId))
                {
                    var def = BackpackCatalog.Get(equippedItemDefId);
                    if (def != null && def.CanPlace && def.Kind == ItemKind.LayerBoundPaper)
                        return new WorldUseResolution(WorldUseOutcome.PlacePaperOnAnchor);
                    return new WorldUseResolution(
                        WorldUseOutcome.NotApplicableFeedback,
                        "Kẹp cạnh phiếu F101 chỉ dùng để ghim tài liệu ca trực đối chiếu.");
                }
            }

            return new WorldUseResolution(WorldUseOutcome.NoMatch);
        }
    }

    /// <summary>
    /// Walkthrough I.4 & B08 epistemic Pocket Note builder.
    /// Strictly reflects ONLY what Minh has actually witnessed/committed; never leaks unseen facts,
    /// never affirms Bed 4's occupant/identity, never claims 03:17 is time of death, and never grades morality.
    /// </summary>
    public static class PocketNoteBuilder
    {
        public static string BuildConciseNote(Investigation state)
        {
            if (state == null) return "";
            if (state.chapter != "CH01")
            {
                if (state.chapter == "CH02")
                {
                    if (!state.Has("E12"))
                        return "Cần tự đối chiếu hiện trường Phòng 101 năm 2012 — bản kê, ổ cắm và dấu bánh trên nền.";
                    if (!GateService.G02(state))
                        return "Kho giường phía sau tủ sắt đã lộ ra. Cần đối chiếu đủ các mảnh giấy và hồ sơ ở hiện trường R.";
                    return "Cửa khu Cấp cứu đã mở. Tới sổ giao ca Cấp cứu.";
                }
                return "Theo dõi các ghi chép thực tế đã kiểm chứng trong sổ hồ sơ.";
            }

            if (!state.Happened("E10a.SMSRead"))
                return "Chú Bảy để lại bì thư gần xe máy. Cần mở xem tin nhắn và giấy hẹn trước khi quyết định.";

            if (!state.Happened("CH01-T02"))
                return "Chú Bảy báo hoãn, không muốn mình vào. Nhưng trong bệnh viện bỏ hoang kia vẫn có ánh đèn.";

            if (!state.Happened("CH01-T03"))
                return "Sau ô kính quầy tiếp nhận như có người. Cần đi vào tận bàn trực để tự kiểm tra.";

            if (!state.Happened("CH01-T04"))
                return "Quầy trực đang sáng như ca đêm 2005 — và Lan đang ở ngay trước mặt. Cần nói chuyện với cô ấy.";

            if (!state.Happened("CH01-T05"))
                return "Lan nói như ca trực đêm 17/11/2005 vẫn đang diễn ra. Trên mặt quầy có bảng kẹp phòng 101.";

            if (!GateService.G01(state))
            {
                bool hasA = HypothesisService.HasExaminedInLayerB(state, ObservationRegistry.Bed4Traces);
                bool hasB = HypothesisService.HasExaminedInLayerB(state, ObservationRegistry.Bed4Card);
                if (hasA && hasB)
                    return "Dấu sử dụng ở hốc giường 4 khớp với thẻ AS-05B chưa có chữ ký nhận. Số 0 trên F101 có đủ cơ sở để ký không?";
                if (hasA)
                    return "Phía sau rèm có dấu vết một vị trí giường thứ tư từng được dùng. Giấy tờ bàn giao của vị trí này ở đâu?";
                if (hasB)
                    return "Thẻ AS-05B của giường 4 còn trống ô ký nhận. Hiện trường sau rèm có dấu vết gì khớp với tấm thẻ này không?";
                return "Lan nhờ kiểm tra phòng 101 trước khi ký con số 0 trên phiếu bàn giao F101.";
            }

            if (!state.Happened("CH01-T08"))
            {
                string decisionLine = DecisionReflectionLine(state.f101.current);
                return decisionLine + "\nChữ ký vừa làm không gian phòng 101 đổi khác. Cần quay lại quầy tiếp nhận kiểm tra.";
            }

            if (!state.Happened("CH01-T09"))
                return DecisionReflectionLine(state.f101.current) + "\nLan không còn ở quầy, lớp 2005 đã tắt. Thử ra cổng phụ xem sao.";

            // B08: After outer gate refuses exit
            var sb = new StringBuilder();
            sb.Append(DecisionReflectionLine(state.f101.current));
            sb.Append("\nCổng phụ không cho rời đi. Chỉ còn cách tự kiểm chứng hiện trường Phòng 101 năm 2012 qua sổ trực.");
            return sb.ToString();
        }

        public static string DecisionReflectionLine(FormStatus current) => current switch
        {
            FormStatus.Affirmed => "Mình đã ký xác nhận số 0.",
            FormStatus.Unverified => "Mình đã để số 0 ở trạng thái chưa xác minh.",
            FormStatus.Refuted => "Mình đã ghi nhận: số 0 chưa đủ căn cứ xác nhận.",
            _ => "Phiếu F101 vẫn chưa ký."
        };

        public static string BuildEpistemicSummary(Investigation state)
        {
            if (state == null) return "";
            var sb = new StringBuilder();
            sb.AppendLine(BuildConciseNote(state));

            if (state.f101 != null && state.f101.revision > 0)
            {
                sb.AppendLine();
                sb.Append("Quyết định F101 hiện tại: ");
                sb.AppendLine(DecisionReflectionLine(state.f101.current));
                if (state.f101.revision > 1)
                {
                    sb.Append("Lần ký đầu tiên vẫn được lưu trong lịch sử: ");
                    sb.AppendLine(DecisionReflectionLine(state.f101.firstDecision));
                }
            }

            bool hasA = HypothesisService.HasExaminedInLayerB(state, ObservationRegistry.Bed4Traces);
            bool hasB = HypothesisService.HasExaminedInLayerB(state, ObservationRegistry.Bed4Card);
            if (hasA || hasB || CorroborationService.IsCorroborated(state))
            {
                sb.AppendLine();
                sb.AppendLine("Những gì chính mắt đã kiểm chứng:");
                if (hasA) sb.AppendLine("• Sau rèm phòng 101 (lớp B) có dấu sử dụng ở vị trí giường thứ tư.");
                if (hasB) sb.AppendLine("• Thẻ bàn giao AS-05B (lớp B) còn trống ô chữ ký người nhận.");
                if (CorroborationService.IsCorroborated(state))
                    sb.AppendLine("• Mốc 03:17 lặp lại giữa đồng hồ quầy lớp B và vết hằn sau thẻ AS-05B — chưa rõ nguyên nhân.");
                if (state.Happened("CH01-T08") && hasB)
                    sb.AppendLine("• Tờ thẻ cầm ở ca 2005 không còn bản vật lý trong năm 2012, chỉ còn ghi nhớ nguồn.");
            }
            return sb.ToString().TrimEnd();
        }
    }
}
