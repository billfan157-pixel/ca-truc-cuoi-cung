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
        AnchoredWorld = 3,
        MemoryOnly = 4
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
        public string instanceId = "";
        public string itemDefId = "";
        public BackpackItemStatus status = BackpackItemStatus.InBag;
        public Reality originLayer = Reality.R;
        public string originLoc = "";
        public string acquiredChapter = "CH01";
        public string placedAnchorId = "";
        public List<string> viewedFlags = new();
        public int itemRevision = 1;

        public bool HasViewed(string flag) =>
            viewedFlags != null && !string.IsNullOrEmpty(flag) && viewedFlags.Contains(flag);

        public bool MarkViewed(string flag)
        {
            if (string.IsNullOrEmpty(flag)) return false;
            viewedFlags ??= new();
            if (viewedFlags.Contains(flag)) return false;
            viewedFlags.Add(flag);
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
        public readonly bool CanCombine;
        public readonly string LinkedObservationId;
        public readonly string LinkedEvidenceId;
        public readonly string FrontTexture;
        public readonly string BackTexture;
        public readonly string InspectModelId;

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
            bool canCombine = false,
            string linkedObservationId = null,
            string linkedEvidenceId = null,
            string frontTexture = null,
            string backTexture = null,
            string inspectModelId = null)
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
            CanCombine = canCombine;
            LinkedObservationId = linkedObservationId;
            LinkedEvidenceId = linkedEvidenceId;
            FrontTexture = frontTexture;
            BackTexture = backTexture;
            InspectModelId = inspectModelId;
        }
    }

    public static class WorldAnchors
    {
        public const string An02CompareClip = "AN02_CLIP";
        public const string StaffDeskLeft = "STAFF_DESK_L";
        public const string StaffDeskRight = "STAFF_DESK_R";
        public const string StaffDeskCompare = StaffDeskLeft;
        public const string ReceptionDeskLeft = "RECEPTION_SLOT_L";
        public const string ReceptionDeskRight = "RECEPTION_SLOT_R";
        public const string Ward101TableLeft = "WARD101_TABLE_L";
        public const string Ward101TableRight = "WARD101_TABLE_R";

        public static bool Known(string anchorId) =>
            anchorId is An02CompareClip or StaffDeskLeft or StaffDeskRight
                or ReceptionDeskLeft or ReceptionDeskRight
                or Ward101TableLeft or Ward101TableRight;

        public static string Label(string anchorId) => anchorId switch
        {
            An02CompareClip => "Kẹp đối chiếu cạnh F101 (Phòng 101)",
            StaffDeskLeft => "Bàn điều dưỡng STAFF · Ô trái",
            StaffDeskRight => "Bàn điều dưỡng STAFF · Ô phải",
            ReceptionDeskLeft => "Quầy tiếp nhận · Ô trái",
            ReceptionDeskRight => "Quầy tiếp nhận · Ô phải",
            Ward101TableLeft => "Bàn đối chiếu Phòng 101 · Ô trái",
            Ward101TableRight => "Bàn đối chiếu Phòng 101 · Ô phải",
            _ => ""
        };

        public static string ZoneOf(string anchorId) => anchorId switch
        {
            An02CompareClip or Ward101TableLeft or Ward101TableRight => "LOC03",
            StaffDeskLeft or StaffDeskRight => "STAFF",
            ReceptionDeskLeft or ReceptionDeskRight => "LOC02",
            _ => ""
        };

        public static bool AllowedInLayer(string anchorId, Reality layer) => anchorId switch
        {
            An02CompareClip => layer == Reality.B,
            StaffDeskLeft or StaffDeskRight => layer == Reality.B,
            ReceptionDeskLeft or ReceptionDeskRight => layer is (Reality.R or Reality.B),
            Ward101TableLeft or Ward101TableRight => layer is (Reality.R or Reality.B),
            _ => false
        };

        public static string[] SlotsForSurface(string surfaceTargetId) => surfaceTargetId switch
        {
            "COMPARE_CLIP_AN02" => new[] { An02CompareClip },
            "COMPARE_DESK_STAFF" => new[] { StaffDeskLeft, StaffDeskRight },
            "COMPARE_DESK_RECEPTION" => new[] { ReceptionDeskLeft, ReceptionDeskRight },
            "COMPARE_DESK_101" => new[] { Ward101TableLeft, Ward101TableRight },
            _ => Array.Empty<string>()
        };
    }

    public static class BackpackCatalog
    {
        // CH01 Tools & Papers
        public const string Flashlight = "FLASHLIGHT";
        public const string GateKey = "KEY_GATE";
        public const string WingCKey = "KEY_WING_C";
        public const string E10aEnvelope = "E10A_ENVELOPE";
        public const string BoardAssignment = "BOARD_ASSIGNMENT";
        public const string PaperObs101B = "PAPER_OBS101B";
        public const string PaperObs101C = "PAPER_OBS101C";
        public const string PaperObs101D = "PAPER_OBS101D";
        public const string PaperShiftLog = "PAPER_SHIFT_LOG";

        // CH02 Persistent Physical Evidence (Layer R)
        public const string Ch02Draw1 = "CH02_DRAW1";
        public const string Ch02Draw2 = "CH02_DRAW2";
        public const string Ch02Draw3 = "CH02_DRAW3";
        public const string Ch02DrawFull = "CH02_DRAW_FULL";
        public const string Ch02Wristband = "CH02_WRISTBAND";
        public const string Ch02Transfer = "CH02_TRANSFER";

        // CH03 & CH04 Physical Evidence + MemoryOnly References
        public const string Ch03Register = "CH03_REGISTER";
        public const string Ch03C02Note = "CH03_C02_NOTE";
        public const string Ch03C02Bag = "CH03_C02_BAG";
        public const string Ch03C02Letter = "CH03_C02_LETTER";
        public const string Ch03C12Box = "CH03_C12_BOX";
        public const string Ch04Orders = "CH04_ORDERS";
        public const string Ch04Log = "CH04_LOG";
        public const string Ch04Tape = "CH04_TAPE";
        public const string Ch04Morgue = "CH04_MORGUE";
        public const string Ch04LanRecovery = "CH04_LAN_RECOVERY";
        public const string Ch05PostedMap = "CH05_POSTED_MAP";
        public const string Ch05AsBuiltE05 = "CH05_ASBUILT_E05";
        public const string Ch05SealedWallE05b = "CH05_SEALED_E05B";
        public const string MemVignetteC02 = "MEM_VIGNETTE_C02";
        public const string MemEvacKhuB = "MEM_EVAC_KHUB";
        public const string MemE08Trace = "MEM_E08_TRACE";

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
                "Chìa đồng cũ đã sờn mép rút từ bì thư của Bảy. Khớp với ổ khóa cổng phụ phía trước chốt bảo vệ.",
                "Từ bì thư E10a · Lớp R · LOC01",
                ItemKind.EquippableTool,
                BackpackCompartment.Tools,
                canEquip: true,
                canPlace: false),
            new(
                WingCKey,
                "Chìa khu C (đã thay khóa)",
                "Chìa khu C",
                "Chìa sắt sẫm màu có thẻ giấy nhỏ ghi ký hiệu khu C. Bảy chú thích ổ khóa khu C đã thay từ trước khi thanh lý — hiện chưa rõ dùng vào đâu trong đêm nay.",
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
                "Thẻ giường tạm ngày 17/11/2005 khoanh tròn 'CHƯA BÀN GIAO', trống ô chữ ký người nhận. Chỉ tồn tại vật lý trong lớp B; có thể đặt lên kẹp F101 hoặc bàn đối chiếu.",
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
                backTexture: "Doc_ShiftGuide_Back"),

            // CH02 Persistent Physical Evidence (Kho giường 101 · Layer R)
            new(
                Ch02Draw1,
                "Mảnh tranh 1 (Mái ngói)",
                "Mảnh tranh 1",
                "Nét mái ngói vẽ bằng sáp màu dừng ở mép giấy rách, tìm thấy trong tủ khô kho 101. Có thể ghép với 2 mảnh còn lại trên bàn đối chiếu hoặc trong ba lô.",
                "Vật chứng lớp R · LOC03 (Kho 101)",
                ItemKind.PersistentPhysical,
                BackpackCompartment.Papers,
                canEquip: true,
                canPlace: true,
                canCombine: true,
                linkedEvidenceId: EvidenceRegistry.DrawingFragments[0],
                frontTexture: "DrawingPiece1"),
            new(
                Ch02Draw2,
                "Mảnh tranh 2 (Cửa xanh & ổ khóa)",
                "Mảnh tranh 2",
                "Nét cánh cửa màu xanh và ổ khóa bằng chì sẫm, tìm thấy trong tủ khô kho 101. Khớp mép rách với hai mảnh tranh còn lại.",
                "Vật chứng lớp R · LOC03 (Kho 101)",
                ItemKind.PersistentPhysical,
                BackpackCompartment.Papers,
                canEquip: true,
                canPlace: true,
                canCombine: true,
                linkedEvidenceId: EvidenceRegistry.DrawingFragments[1],
                frontTexture: "DrawingPiece2"),
            new(
                Ch02Draw3,
                "Mảnh tranh 3 (Mặt trời & ô cửa)",
                "Mảnh tranh 3",
                "Ông mặt trời và một nửa ô cửa sổ nhỏ bằng sáp màu, tìm thấy trong tủ khô kho 101.",
                "Vật chứng lớp R · LOC03 (Kho 101)",
                ItemKind.PersistentPhysical,
                BackpackCompartment.Papers,
                canEquip: true,
                canPlace: true,
                canCombine: true,
                linkedEvidenceId: EvidenceRegistry.DrawingFragments[2],
                frontTexture: "DrawingPiece3"),
            new(
                Ch02DrawFull,
                "Bức tranh đã ghép của An (E11)",
                "Tranh ghép E11",
                "Ba mảnh tranh sáp màu đã ghép khít mép: mái ngói, cánh cửa xanh có ổ khóa và ô cửa sổ nhỏ.",
                "Vật chứng lớp R · LOC03 · Đã ghép hoàn chỉnh",
                ItemKind.PersistentPhysical,
                BackpackCompartment.Papers,
                canEquip: true,
                canPlace: true,
                linkedEvidenceId: EvidenceRegistry.Drawing,
                frontTexture: "Drawing"),
            new(
                Ch02Wristband,
                "Vòng tay bệnh nhi C01",
                "Vòng tay C01",
                "Phiến giấy trong vòng nhựa mờ ghi mã C01, giữ trong túi đồ vải ở kho giường 101.",
                "Vật chứng lớp R · LOC03 (Kho 101)",
                ItemKind.PersistentPhysical,
                BackpackCompartment.Tools,
                canEquip: true,
                canPlace: true,
                linkedEvidenceId: EvidenceRegistry.Wristband,
                inspectModelId: "WRIST"),
            new(
                Ch02Transfer,
                "Phiếu chuyển 101/4 → C01",
                "Phiếu chuyển 101/4",
                "17/11/2005 · An (C01) — khám sốt tạm tại giường 4, Phòng 101. Chuyển lại Khu C trong ngày theo lệnh phòng Giám đốc. Phiếu viết tay của Lan, lưu tại kho 101.",
                "Vật chứng lớp R · LOC03 (Kho 101)",
                ItemKind.PersistentPhysical,
                BackpackCompartment.Papers,
                canEquip: true,
                canPlace: true,
                linkedEvidenceId: EvidenceRegistry.Transfer,
                frontTexture: "Transfer"),

            // CH03 & CH04 Physical Evidence + MemoryOnly References
            new(
                Ch03Register,
                "Sổ bàn giao lưu trữ (E03)",
                "Sổ lưu trữ E03",
                "Sổ bàn giao trên bàn đọc kho hồ sơ tầng 3 ghi các mã bệnh nhân còn lưu dấu.",
                "Vật chứng lớp R · LOC07",
                ItemKind.PersistentPhysical,
                BackpackCompartment.Papers,
                canEquip: false,
                canPlace: false,
                linkedEvidenceId: EvidenceRegistry.PatientRegister),
            new(
                Ch03C02Note,
                "Ghi chú kẹp sợi len (K-02)",
                "Ghi chú K-02",
                "«B/2. Giữ khăn đan dở trong túi có thẻ K-02.»",
                "Vật chứng lớp R · LOC07",
                ItemKind.PersistentPhysical,
                BackpackCompartment.Papers,
                canEquip: true,
                canPlace: false,
                linkedEvidenceId: EvidenceRegistry.C02CareNote),
            new(
                Ch03C02Bag,
                "Túi len gắn thẻ K-02 (C02)",
                "Túi len K-02",
                "Túi đồ vải giữ chiếc khăn đan dở gắn thẻ K-02 trong kho lưu trữ tầng 3.",
                "Vật chứng lớp R · LOC07",
                ItemKind.PersistentPhysical,
                BackpackCompartment.Tools,
                canEquip: false,
                canPlace: false,
                linkedEvidenceId: EvidenceRegistry.C02YarnBag),
            new(
                Ch03C02Letter,
                "Thư con gái gửi Bà Lành",
                "Thư con gái C02",
                "Lá thư tay tùy chọn xác thực tên thật của C02 là Bà Lành.",
                "Vật chứng lớp R · LOC07",
                ItemKind.PersistentPhysical,
                BackpackCompartment.Papers,
                canEquip: true,
                canPlace: false,
                linkedEvidenceId: EvidenceRegistry.C02DaughterLetter),
            new(
                Ch03C12Box,
                "Ghi nhận hộp C/12 (vô danh)",
                "Hồ sơ C/12",
                "Xác nhận sự tồn tại của người bệnh C12 theo hộp giữ đồ vô danh — không tự đặt tên.",
                "Ghi nhận hiện trường · LOC07",
                ItemKind.PersistentPhysical,
                BackpackCompartment.Papers,
                canEquip: false,
                canPlace: false,
                linkedEvidenceId: EvidenceRegistry.C12UnnamedBox),
            new(
                Ch04Orders,
                "Bản lệnh giao ca (E02)",
                "Lệnh giao ca E02",
                "Bản lệnh giao ca thu được tại phòng trực tầng 2 (LOC06).",
                "Vật chứng lớp R · LOC06",
                ItemKind.PersistentPhysical,
                BackpackCompartment.Papers,
                canEquip: false,
                canPlace: false,
                linkedEvidenceId: EvidenceRegistry.DutyOrders),
            new(
                Ch04Log,
                "Sổ ca đêm (E09)",
                "Sổ ca đêm E09",
                "Sổ ca đêm gốc tại phòng trực tầng 2 (LOC06).",
                "Vật chứng lớp R · LOC06",
                ItemKind.PersistentPhysical,
                BackpackCompartment.Papers,
                canEquip: false,
                canPlace: false,
                linkedEvidenceId: EvidenceRegistry.DutyLog),
            new(
                Ch04Tape,
                "Cuộn băng cuộc họp (E06a)",
                "Băng họp E06a",
                "Cuộn băng ghi âm cuộc họp phòng trực. Có thể cầm trên tay và đặt vào máy phát tại bàn phòng trực (AN04) khi đã đủ E02 + E09.",
                "Vật chứng lớp R · LOC06",
                ItemKind.PersistentPhysical,
                BackpackCompartment.Tools,
                canEquip: true,
                canPlace: false,
                linkedEvidenceId: EvidenceRegistry.MeetingTape),
            new(
                Ch04Morgue,
                "Hồ sơ tiếp nhận nhà xác (E04)",
                "Hồ sơ E04",
                "Hồ sơ tiếp nhận tìm thấy dưới tầng hầm B1 (LOC10).",
                "Vật chứng lớp R · LOC10",
                ItemKind.PersistentPhysical,
                BackpackCompartment.Papers,
                canEquip: false,
                canPlace: false,
                linkedEvidenceId: EvidenceRegistry.MorgueRegister),
            new(
                Ch04LanRecovery,
                "Thẻ & phiếu thu hồi Lan (E07)",
                "Phiếu E07",
                "Thẻ nhân viên và phiếu thu hồi L-13 tại nhà xác B1 (LOC10).",
                "Vật chứng lớp R · LOC10",
                ItemKind.PersistentPhysical,
                BackpackCompartment.Papers,
                canEquip: false,
                canPlace: false,
                linkedEvidenceId: EvidenceRegistry.LanRecovery),
            new(
                Ch05PostedMap,
                "Sơ đồ thoát hiểm niêm yết 2005 (Đối nghịch)",
                "Sơ đồ 2005",
                "Bản sơ đồ niêm yết trên bảng trực năm 2005 vẽ cửa thoát hiểm phía Đông Khu C mở ra hào ngoài. Nguồn đối nghịch dùng để chồng đối chiếu với bản hoàn công 2004.",
                "Tài liệu đối chiếu · Lớp R · LOC09",
                ItemKind.PersistentPhysical,
                BackpackCompartment.Papers,
                canEquip: false,
                canPlace: false,
                linkedObservationId: ObservationRegistry.Ch05PostedMap,
                frontTexture: "Doc_Ch05_PostedMap2005"),
            new(
                Ch05AsBuiltE05,
                "Bản vẽ hoàn công cải tạo B1 · 06/2004 (E05)",
                "Bản hoàn công E05",
                "Bản vẽ hoàn công cải tạo bán hầm kỹ thuật B1 tháng 06/2004 do Giám đốc Phạm Đức Vĩnh phê duyệt: xây gạch đặc 220 mm bít vĩnh viễn cửa thoát hiểm phía Đông tại tọa độ (34.5, 42) theo yêu cầu cô lập của đơn vị tài trợ.",
                "Vật chứng lớp R · LOC09 · 06/2004",
                ItemKind.PersistentPhysical,
                BackpackCompartment.Papers,
                canEquip: false,
                canPlace: false,
                linkedEvidenceId: EvidenceRegistry.AsBuilt2004,
                frontTexture: "Doc_Ch05_AsBuilt2004_E05"),
            new(
                Ch05SealedWallE05b,
                "Ghi nhận hiện trường cửa bít 2004 (E05b)",
                "Hiện trường E05b",
                "Quan sát trực tiếp ngoài hố kỹ thuật phía Đông (LOC11): mảng gạch xây bít 220 mm, lớp vữa trát kín hai mặt năm 2004 và khung cửa đã tháo bỏ. Lối thoát phía Đông không thể mở từ bên trong.",
                "Vật chứng lớp R · LOC11 · 2012",
                ItemKind.PersistentPhysical,
                BackpackCompartment.Papers,
                canEquip: false,
                canPlace: false,
                linkedEvidenceId: EvidenceRegistry.SealedWallPhoto,
                frontTexture: "Doc_Ch05_AsBuilt2004_E05"),
            new(
                MemVignetteC02,
                "Ký ức kho hồ sơ · C02 (Lớp M)",
                "Ký ức C02 (M)",
                "Ghi nhớ phi vật chất từ lát cắt ký ức lớp M tại AN03. Tuyệt đối không phải vật chứng có thể xuất bản.",
                "Ký ức lớp M · Không thể xuất chứng cứ",
                ItemKind.MemoryOnly,
                BackpackCompartment.Papers,
                canEquip: false,
                canPlace: false),
            new(
                MemEvacKhuB,
                "Ký ức sơ tán Khu B · AN05 (Lớp M)",
                "Ký ức Khu B (M)",
                "Ghi nhớ phi vật chất từ ca sơ tán đêm 18/11/2005 tại Khu B (6 bệnh nhân được đưa ra sân qua cầu thang S1). Không phải vật chứng xuất và không thay đổi lịch sử.",
                "Ký ức lớp M · Không thể xuất chứng cứ",
                ItemKind.MemoryOnly,
                BackpackCompartment.Papers,
                canEquip: false,
                canPlace: false),
            new(
                MemE08Trace,
                "Dấu ký ức E08 (Lớp M)",
                "Ký ức E08 (M)",
                "Vết ký ức phi vật chất thuộc lớp M — không tồn tại vật lý và không nằm trong gói vật chứng xuất.",
                "Ký ức lớp M · Không thể xuất chứng cứ",
                ItemKind.MemoryOnly,
                BackpackCompartment.Papers,
                canEquip: false,
                canPlace: false)
        };

        private static readonly Dictionary<string, BackpackItemDef> ById;
        private static readonly Dictionary<string, BackpackItemDef> ByObservation;
        private static readonly Dictionary<string, BackpackItemDef> ByEvidence;

        static BackpackCatalog()
        {
            ById = new Dictionary<string, BackpackItemDef>(StringComparer.Ordinal);
            ByObservation = new Dictionary<string, BackpackItemDef>(StringComparer.Ordinal);
            ByEvidence = new Dictionary<string, BackpackItemDef>(StringComparer.Ordinal);
            for (int i = 0; i < Ordered.Length; i++)
            {
                var def = Ordered[i];
                ById[def.Id] = def;
                if (!string.IsNullOrEmpty(def.LinkedObservationId) && def.Id != BoardAssignment)
                    ByObservation[def.LinkedObservationId] = def;
                if (!string.IsNullOrEmpty(def.LinkedEvidenceId))
                    ByEvidence[def.LinkedEvidenceId] = def;
            }
        }

        public static IReadOnlyList<BackpackItemDef> All => Ordered;
        public static bool Registered(string id) => !string.IsNullOrEmpty(id) && ById.ContainsKey(id);
        public static BackpackItemDef Get(string id) => !string.IsNullOrEmpty(id) && ById.TryGetValue(id, out var d) ? d : null;
        public static BackpackItemDef ForObservation(string obsId) =>
            !string.IsNullOrEmpty(obsId) && ByObservation.TryGetValue(obsId, out var d) ? d : null;
        public static BackpackItemDef ForEvidence(string evidenceId) =>
            !string.IsNullOrEmpty(evidenceId) && ByEvidence.TryGetValue(evidenceId, out var d) ? d : null;
    }

    public static class InventoryService
    {
        public const int MaxQuickSlots = 4;

        public static void EnsureDefaultTools(Investigation state)
        {
            if (state == null) return;
            state.backpackItems ??= new();
            state.quickSlots ??= new();
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

        public static BackpackEntry GetEquipped(Investigation state)
        {
            if (state == null || string.IsNullOrEmpty(state.equippedInstanceId)) return null;
            var entry = FindByInstance(state, state.equippedInstanceId);
            return entry != null && entry.status == BackpackItemStatus.Equipped ? entry : null;
        }

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

        public static List<BackpackEntry> GetAllPlaced(Investigation state)
        {
            var list = new List<BackpackEntry>();
            if (state?.backpackItems == null) return list;
            for (int i = 0; i < state.backpackItems.Count; i++)
            {
                var e = state.backpackItems[i];
                if (e != null && e.status == BackpackItemStatus.Placed && WorldAnchors.Known(e.placedAnchorId))
                    list.Add(e);
            }
            return list;
        }

        public static bool HasPhysical(Investigation state, string itemDefId)
        {
            var e = FindByDef(state, itemDefId);
            return e != null && e.status != BackpackItemStatus.MemoryRef;
        }

        public static bool CanAssembleCh02Drawing(Investigation state) =>
            state != null && state.chapter == "CH02" && state.layer == Reality.R &&
            state.Has("E12") && EvidenceRegistry.HaveFragments(state) && !state.Has(EvidenceRegistry.Drawing);

        public static bool TryAcquire(Investigation candidate, string itemDefId, Reality originLayer, string originLoc, out BackpackEntry entry)
        {
            entry = null;
            var def = BackpackCatalog.Get(itemDefId);
            if (candidate == null || def == null || def.Kind == ItemKind.AnchoredWorld)
                return false;
            if (def.Kind == ItemKind.MemoryOnly)
                return RecordMemoryRef(candidate, itemDefId, originLoc);
            if (def.Kind == ItemKind.LayerBoundPaper && originLayer != Reality.B)
                return false;

            EnsureDefaultTools(candidate);
            var existing = FindByDef(candidate, itemDefId);
            if (existing != null)
            {
                if (existing.status == BackpackItemStatus.MemoryRef && def.Kind == ItemKind.LayerBoundPaper && originLayer == Reality.B)
                {
                    existing.status = BackpackItemStatus.InBag;
                    existing.placedAnchorId = "";
                    existing.itemRevision++;
                    AutoAssignQuickSlotIfRoom(candidate, itemDefId);
                }
                entry = existing;
                return true;
            }

            entry = new BackpackEntry
            {
                instanceId = "ITEM_" + itemDefId,
                itemDefId = itemDefId,
                status = BackpackItemStatus.InBag,
                originLayer = originLayer,
                originLoc = originLoc ?? "",
                acquiredChapter = candidate.chapter ?? "CH01",
                placedAnchorId = "",
                itemRevision = 1
            };
            candidate.backpackItems.Add(entry);
            AutoAssignQuickSlotIfRoom(candidate, itemDefId);
            return true;
        }

        public static bool RecordMemoryRef(Investigation candidate, string itemDefId, string originLoc)
        {
            var def = BackpackCatalog.Get(itemDefId);
            if (candidate == null || def == null || def.Kind != ItemKind.MemoryOnly)
                return false;
            EnsureDefaultTools(candidate);
            if (FindByDef(candidate, itemDefId) != null) return true;
            candidate.backpackItems.Add(new BackpackEntry
            {
                instanceId = "ITEM_" + itemDefId,
                itemDefId = itemDefId,
                status = BackpackItemStatus.MemoryRef,
                originLayer = Reality.M,
                originLoc = originLoc ?? "",
                acquiredChapter = candidate.chapter ?? "CH03",
                placedAnchorId = "",
                itemRevision = 1
            });
            return true;
        }

        public static void SyncFromEvidence(Investigation candidate, string evidenceId, string originLoc)
        {
            if (candidate == null || string.IsNullOrEmpty(evidenceId)) return;
            var def = BackpackCatalog.ForEvidence(evidenceId);
            if (def == null || def.Id == BackpackCatalog.E10aEnvelope) return;
            TryAcquire(candidate, def.Id, Reality.R, originLoc, out _);
            if (evidenceId == EvidenceRegistry.Drawing)
            {
                // Keep fragments clean if any were placed on the assembly table: return them to bag/tidy state.
                foreach (string fragDef in new[] { BackpackCatalog.Ch02Draw1, BackpackCatalog.Ch02Draw2, BackpackCatalog.Ch02Draw3 })
                {
                    var frag = FindByDef(candidate, fragDef);
                    if (frag != null && frag.status == BackpackItemStatus.Placed)
                    {
                        frag.status = BackpackItemStatus.InBag;
                        frag.placedAnchorId = "";
                        frag.itemRevision++;
                    }
                }
            }
        }

        public static void AutoAssignQuickSlotIfRoom(Investigation candidate, string itemDefId)
        {
            if (candidate == null || string.IsNullOrEmpty(itemDefId)) return;
            var def = BackpackCatalog.Get(itemDefId);
            if (def == null || !def.CanEquip) return;
            candidate.quickSlots ??= new();
            if (candidate.quickSlots.Contains(itemDefId)) return;
            if (candidate.quickSlots.Count < MaxQuickSlots)
                candidate.quickSlots.Add(itemDefId);
        }

        public static bool ToggleQuickSlot(Investigation candidate, string itemDefId)
        {
            if (candidate == null || string.IsNullOrEmpty(itemDefId)) return false;
            var def = BackpackCatalog.Get(itemDefId);
            var entry = FindByDef(candidate, itemDefId);
            if (def == null || !def.CanEquip || entry == null || entry.status == BackpackItemStatus.MemoryRef)
                return false;
            candidate.quickSlots ??= new();
            if (candidate.quickSlots.Remove(itemDefId))
                return true;
            if (candidate.quickSlots.Count >= MaxQuickSlots)
                candidate.quickSlots.RemoveAt(0);
            candidate.quickSlots.Add(itemDefId);
            return true;
        }

        public static void PruneQuickSlots(Investigation candidate)
        {
            if (candidate == null) return;
            candidate.quickSlots ??= new();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = candidate.quickSlots.Count - 1; i >= 0; i--)
            {
                string id = candidate.quickSlots[i];
                var def = BackpackCatalog.Get(id);
                var entry = FindByDef(candidate, id);
                bool valid = def != null && def.CanEquip && entry != null &&
                             entry.status != BackpackItemStatus.MemoryRef &&
                             !(def.Kind == ItemKind.LayerBoundPaper && candidate.layer != Reality.B) &&
                             seen.Add(id);
                if (!valid) candidate.quickSlots.RemoveAt(i);
            }
            while (candidate.quickSlots.Count > MaxQuickSlots)
                candidate.quickSlots.RemoveAt(candidate.quickSlots.Count - 1);
        }

        public static string GetQuickSlotItem(Investigation state, int slotIndex)
        {
            if (state?.quickSlots == null || slotIndex < 0 || slotIndex >= state.quickSlots.Count)
                return null;
            string id = state.quickSlots[slotIndex];
            var entry = FindByDef(state, id);
            var def = BackpackCatalog.Get(id);
            if (entry == null || def == null || !def.CanEquip || entry.status == BackpackItemStatus.MemoryRef)
                return null;
            if (def.Kind == ItemKind.LayerBoundPaper && state.layer != Reality.B)
                return null;
            return id;
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
                    e.itemRevision++;
                }
            }
            candidate.equippedInstanceId = "";
        }

        public static bool TryPlaceAtAnchor(Investigation candidate, string itemDefId, string anchorId)
        {
            if (candidate == null || !WorldAnchors.Known(anchorId)) return false;
            if (!WorldAnchors.AllowedInLayer(anchorId, candidate.layer)) return false;
            var def = BackpackCatalog.Get(itemDefId);
            var entry = FindByDef(candidate, itemDefId);
            if (def == null || entry == null || !def.CanPlace || entry.status == BackpackItemStatus.MemoryRef)
                return false;
            if (def.Kind == ItemKind.LayerBoundPaper && candidate.layer != Reality.B)
                return false;

            // Return any previous item at this anchor cleanly to InBag (no duplication, no loss)
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
            candidate.quickSlots?.Remove(itemDefId);
            candidate.backpackItems.Remove(entry);
            return true;
        }

        /// <summary>
        /// Walkthrough B06 atomic B->R reconciliation:
        /// Every LayerBoundPaper (in bag, held in hand, or placed on ANY AN02/STAFF/RECEPTION/WARD101 anchor)
        /// is returned to Layer B and preserved strictly as a non-physical MemoryRef (cannot be equipped/placed/exported in R).
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

            if (candidate.layer == Reality.R && candidate.Happened("CH01-T08"))
            {
                EnsureMemoryRefIfExamined(candidate, ObservationRegistry.Bed4Card, BackpackCatalog.PaperObs101B, "LOC03");
                EnsureMemoryRefIfExamined(candidate, ObservationRegistry.Bed2Transfer, BackpackCatalog.PaperObs101C, "LOC03");
                EnsureMemoryRefIfExamined(candidate, ObservationRegistry.Bed3Meds, BackpackCatalog.PaperObs101D, "LOC03");
                EnsureMemoryRefIfExamined(candidate, ObservationRegistry.ShiftGuide, BackpackCatalog.PaperShiftLog, "STAFF");
            }
            PruneQuickSlots(candidate);
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
            // Deterministically reconstruct CH02/CH03/CH04 physical BackpackEntries strictly from committed evidence
            if (state.evidence != null)
            {
                for (int i = 0; i < state.evidence.Count; i++)
                {
                    string ev = state.evidence[i];
                    if (ev == "E10a" || ev == "E12") continue;
                    string loc = ev.StartsWith("E11", StringComparison.Ordinal) ? "LOC03"
                        : ev.StartsWith("E03", StringComparison.Ordinal) || ev == "FS19" ? "LOC07"
                        : ev is "E04" or "E07" ? "LOC10"
                        : ev == EvidenceRegistry.AsBuilt2004 ? "LOC09"
                        : ev == EvidenceRegistry.SealedWallPhoto ? "LOC11"
                        : "LOC06";
                    SyncFromEvidence(state, ev, loc);
                }
            }
            if (state.Happened("CH03-T04"))
                RecordMemoryRef(state, BackpackCatalog.MemVignetteC02, "LOC07");
            if (state.Happened("CH05-T05"))
                RecordMemoryRef(state, BackpackCatalog.MemEvacKhuB, "LOC05");
            if (state.layer != Reality.B)
                ReconcileLayerBoundToMemory(state);
            PruneQuickSlots(state);
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
                // MemoryOnly items MUST strictly stay MemoryRef (never physical, never equipped/placed)!
                if (def.Kind == ItemKind.MemoryOnly && e.status != BackpackItemStatus.MemoryRef)
                    return false;

                if (e.status == BackpackItemStatus.Placed)
                {
                    Reality effectiveLayer = def.Kind == ItemKind.PersistentPhysical && state.layer == Reality.M
                        ? Reality.R : state.layer;
                    if (!def.CanPlace || !WorldAnchors.Known(e.placedAnchorId) ||
                        !WorldAnchors.AllowedInLayer(e.placedAnchorId, effectiveLayer) ||
                        !usedAnchors.Add(e.placedAnchorId))
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

            if (state.quickSlots != null)
            {
                if (state.quickSlots.Count > MaxQuickSlots) return false;
                var qsSeen = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < state.quickSlots.Count; i++)
                {
                    string qid = state.quickSlots[i];
                    var qdef = BackpackCatalog.Get(qid);
                    var qentry = FindByDef(state, qid);
                    if (qdef == null || !qdef.CanEquip || qentry == null ||
                        qentry.status == BackpackItemStatus.MemoryRef || !qsSeen.Add(qid))
                        return false;
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
        PlayTapeAtDutyDesk = 6,
        NotApplicableFeedback = 7
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
                            : "Mình chưa thể bỏ đi khi chuyện bên trong còn dang dở.");
                }
            }

            if (targetId is ("COMPARE_CLIP_AN02" or "COMPARE_DESK_STAFF" or "COMPARE_DESK_RECEPTION" or "COMPARE_DESK_101"))
            {
                if (!string.IsNullOrEmpty(equippedItemDefId))
                {
                    var def = BackpackCatalog.Get(equippedItemDefId);
                    bool layerMatches = def != null && def.CanPlace &&
                        (def.Kind == ItemKind.PersistentPhysical || (def.Kind == ItemKind.LayerBoundPaper && state.layer == Reality.B));
                    bool anchorAllowed = WorldAnchors.SlotsForSurface(targetId).Length > 0 &&
                        WorldAnchors.AllowedInLayer(WorldAnchors.SlotsForSurface(targetId)[0], state.layer);
                    if (layerMatches && anchorAllowed)
                        return new WorldUseResolution(WorldUseOutcome.PlacePaperOnAnchor);
                    return new WorldUseResolution(
                        WorldUseOutcome.NotApplicableFeedback,
                        "Mặt bàn đối chiếu dùng để đặt giấy tờ hoặc vật chứng kiểm tra trong lớp hiện tại.");
                }
            }

            if (targetId == "AN04" && state.chapter == "CH04" && equippedItemDefId == BackpackCatalog.Ch04Tape)
            {
                if (!state.Has(EvidenceRegistry.DutyOrders) || !state.Has(EvidenceRegistry.DutyLog))
                    return new WorldUseResolution(
                        WorldUseOutcome.NotApplicableFeedback,
                        "Cần có đủ bản lệnh giao ca (E02) và sổ ca đêm (E09) trước khi mở băng họp trên bàn trực.");
                return new WorldUseResolution(WorldUseOutcome.PlayTapeAtDutyDesk);
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
                        return "Kho giường phía sau tủ sắt đã lộ ra. Có thể đặt các mảnh tranh lên bàn Phòng 101 hoặc mở ba lô để ghép và đối chiếu hồ sơ.";
                    return "Cửa khu Cấp cứu đã mở. Tới sổ giao ca Cấp cứu.";
                }
                if (state.chapter == "CH05")
                {
                    if (!state.Happened("CH05-T05"))
                        return "Xe bệnh nhân tại Khu B gợi lại đêm sơ tán 18/11/2005 — sáu người ở Khu B được đưa ra sân qua cầu thang S1.";
                    if (!state.Happened("PZ07.Aligned"))
                        return "Cần xuống khu kỹ thuật B1 (LOC09) kiểm tra tủ bản vẽ, đối chiếu sơ đồ niêm yết 2005 với bản hoàn công tháng 06/2004 (E05).";
                    if (!state.Has(EvidenceRegistry.SealedWallPhoto))
                        return "Hai bản vẽ mâu thuẫn tại cửa thoát hiểm phía Đông (34.5, 42). Cần ra hành lang ngoài LOC11 kiểm chứng trực tiếp bức tường năm 2012 (E05b).";
                    if (!GateService.G05(state))
                        return "Đã có đủ bản hoàn công 2004 (E05) và dấu xây bít thực địa (E05b). Xác minh H03 để mở lối tới Phòng Hành chính (LOC08).";
                    return "Cửa Phòng Hành chính (LOC08 · G05) trên tầng 3 đã mở. Tiến tới điểm kiểm tra báo cáo.";
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

            var sb = new StringBuilder();
            sb.Append(DecisionReflectionLine(state.f101.current));
            sb.Append("\nMình chưa thể bỏ đi khi chuyện bên trong còn dang dở. Trở lại sổ trực để kiểm chứng hiện trường Phòng 101 năm 2012.");
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

            var placed = InventoryService.GetAllPlaced(state);
            if (placed.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Đồ đang đặt trên bàn/kẹp trong hiện trường:");
                for (int i = 0; i < placed.Count; i++)
                {
                    var def = BackpackCatalog.Get(placed[i].itemDefId);
                    sb.Append("• ").Append(def?.ShortLabel ?? placed[i].itemDefId)
                      .Append(" — ").AppendLine(WorldAnchors.Label(placed[i].placedAnchorId));
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
