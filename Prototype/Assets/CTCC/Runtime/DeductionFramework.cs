using System;
using System.Collections.Generic;

namespace CTCC
{
    public enum ObservationStage
    {
        Discovered = 0,
        Examined = 1,
        Interpreted = 2,
        Corroborated = 3
    }

    public enum ObservationCategory
    {
        Fact = 0,
        Comparison = 1,
        Context = 2,
        Claim = 3
    }

    public enum HypothesisOutcome
    {
        Valid = 0,
        InsufficientEvidence = 1,
        IrrelevantEvidence = 2,
        UnsupportedConclusion = 3,
        ContradictsDecision = 4,
        UnknownEvidenceId = 5,
        InvalidProvenance = 6,
        UnexaminedEvidence = 7
    }

    [Serializable]
    public sealed class ObservationRecord
    {
        public string id;
        public Reality layer;
        public string location;
        public string chapter;
        public ObservationStage stage;
        public bool flippedBack;
        public List<string> inspectedDetails = new();

        public bool HasDetail(string detailId) =>
            inspectedDetails != null && inspectedDetails.Contains(detailId);

        public bool MarkDetail(string detailId)
        {
            inspectedDetails ??= new();
            if (string.IsNullOrEmpty(detailId) || inspectedDetails.Contains(detailId))
                return false;
            inspectedDetails.Add(detailId);
            return true;
        }
    }

    [Serializable]
    public struct EvidenceAnnotation
    {
        public string observationId;
        public string hotspotId;

        public EvidenceAnnotation(string observationId, string hotspotId)
        {
            this.observationId = observationId;
            this.hotspotId = hotspotId;
        }
    }

    /// <summary>
    /// Core v3.6.2 (GAP-R3-06 / Plan 4.5) 4-state backend provenance for F101 Bed-4 link verification.
    /// Prevents CH01 UI/save/annotationVersion==0 bypass while preserving legacy committed saves and CH02 revisions.
    /// </summary>
    public enum F101LinkProvenance
    {
        Unverified = 0,
        SpatialFieldVerified = 1,
        LegacyUi = 2,
        LegacyCommittedSave = 3
    }

    [Serializable]
    public sealed class F101Annotations
    {
        public const int Version = 1;
        public const string Table = "F101_TABLE";
        public const string Zero = "F101_ZERO";
        public const string CardStatus = "OBS101B_STATUS";
        public const string Receiver = "OBS101B_SIGN";
        public const string TraceGroup = "OBS101A_TRACES";
        public const string SpatialLinkEvent = "CH01-F101-LINK-SPATIAL";
        public const string LegacyUiLinkEvent = "CH02-F101-LINK-UI";

        public List<EvidenceAnnotation> marks = new();
        public EvidenceAnnotation linkFrom;
        public EvidenceAnnotation linkTo;

        public bool HasLink => !string.IsNullOrEmpty(linkFrom.observationId);
        public string MarkedHotspot(string id) => marks?.Find(mark => mark.observationId == id).hotspotId;

        public static F101LinkProvenance ResolveLinkProvenance(Investigation state, DeductionSubmission submission = null)
        {
            if (state == null) return F101LinkProvenance.Unverified;
            bool hasLink = submission?.annotations?.HasLink == true || state.f101Annotations?.HasLink == true;
            if (!hasLink) return F101LinkProvenance.Unverified;
            if (state.Happened(SpatialLinkEvent)) return F101LinkProvenance.SpatialFieldVerified;
            if (state.Happened(LegacyUiLinkEvent)) return F101LinkProvenance.LegacyUi;
            if (state.f101 != null && state.f101.revision > 0 &&
                state.f101History != null && state.f101History.Exists(h => h != null && h.decision == FormStatus.Refuted))
                return F101LinkProvenance.LegacyCommittedSave;
            return F101LinkProvenance.Unverified;
        }

        public static bool IsHotspot(string id, string hotspot) => id switch
        {
            ObservationRegistry.F101Sheet => hotspot is Table or Zero,
            ObservationRegistry.Bed4Card => hotspot is CardStatus or Receiver,
            _ => false
        };

        public F101Annotations Clone() => new()
        {
            marks = new List<EvidenceAnnotation>(marks),
            linkFrom = linkFrom,
            linkTo = linkTo
        };

        internal bool ValidFor(Investigation state)
        {
            if (marks == null || marks.Count > 2) return false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var mark in marks)
                if (!ids.Add(mark.observationId) || !IsHotspot(mark.observationId, mark.hotspotId) ||
                    !HypothesisService.HasExaminedInLayerB(state, mark.observationId))
                    return false;

            if (!HasLink)
                return string.IsNullOrEmpty(linkFrom.hotspotId) && string.IsNullOrEmpty(linkTo.observationId) &&
                       string.IsNullOrEmpty(linkTo.hotspotId);
            return linkFrom.observationId == ObservationRegistry.Bed4Card &&
                   IsHotspot(linkFrom.observationId, linkFrom.hotspotId) &&
                   linkFrom.hotspotId == MarkedHotspot(linkFrom.observationId) &&
                   linkTo.observationId == ObservationRegistry.Bed4Traces && linkTo.hotspotId == TraceGroup &&
                   HypothesisService.HasExaminedInLayerB(state, linkTo.observationId);
        }

        internal bool SupportsRefutation(Investigation state) =>
            ValidFor(state) && MarkedHotspot(ObservationRegistry.F101Sheet) == Zero &&
            HasLink && linkFrom.hotspotId == Receiver;
    }

    [Serializable]
    public sealed class DeductionSubmission
    {
        public string formId = "F101";
        public List<string> selectedEvidenceIds = new();
        public string claimId = "";
        public int annotationVersion;
        public F101Annotations annotations;

        public DeductionSubmission Clone()
        {
            var copy = new DeductionSubmission
            {
                formId = formId ?? "F101",
                claimId = claimId ?? "",
                selectedEvidenceIds = new List<string>(),
                annotationVersion = annotationVersion,
                annotations = annotationVersion == F101Annotations.Version ? annotations?.Clone() : null
            };
            if (selectedEvidenceIds != null)
            {
                for (int i = 0; i < selectedEvidenceIds.Count; i++)
                    copy.selectedEvidenceIds.Add(selectedEvidenceIds[i]);
            }
            return copy;
        }
    }

    [Serializable]
    public sealed class FormRevisionEntry
    {
        public int revision;
        public FormStatus decision;
        public string claimId = "";
        public List<string> evidenceIds = new();
        public string chapter = "CH01";
        public Reality layer = Reality.B;
        public int annotationVersion;
        public F101Annotations annotations;
        public bool consequenceObserved;
    }

    public sealed class HypothesisEvaluation
    {
        public bool Accepted;
        public HypothesisOutcome Outcome;
        public string Title = "";
        public string Message = "";
        public string ProgressiveHint = "";

        public static HypothesisEvaluation Pass(string title, string message) => new()
        {
            Accepted = true,
            Outcome = HypothesisOutcome.Valid,
            Title = title,
            Message = message
        };

        public static HypothesisEvaluation Reject(HypothesisOutcome outcome, string title, string message, string hint = "") => new()
        {
            Accepted = false,
            Outcome = outcome,
            Title = title,
            Message = message,
            ProgressiveHint = hint
        };
    }

    public sealed class ObservationSpec
    {
        public readonly string Id;
        public readonly string ShortCode;
        public readonly string Title;
        public readonly string FactSummary;
        public readonly string InterpretationNote;
        public readonly string ProvenanceLabel;
        public readonly ObservationCategory Category;
        public readonly Reality[] AllowedLayers;
        public readonly string[] AllowedLocations;
        public readonly string[] AllowedChapters;
        public readonly bool SelectableForF101;
        public readonly bool IsDocument;
        public readonly bool HasBackSide;
        public readonly string FrontTexture;
        public readonly string BackTexture;
        public readonly string[] DetailLabels;

        public ObservationSpec(
            string id,
            string shortCode,
            string title,
            string factSummary,
            string interpretationNote,
            string provenanceLabel,
            ObservationCategory category,
            Reality[] allowedLayers,
            string[] allowedLocations,
            string[] allowedChapters,
            bool selectableForF101,
            bool isDocument = false,
            bool hasBackSide = false,
            string frontTexture = null,
            string backTexture = null,
            string[] detailLabels = null)
        {
            Id = id;
            ShortCode = shortCode;
            Title = title;
            FactSummary = factSummary;
            InterpretationNote = interpretationNote;
            ProvenanceLabel = provenanceLabel;
            Category = category;
            AllowedLayers = allowedLayers;
            AllowedLocations = allowedLocations;
            AllowedChapters = allowedChapters;
            SelectableForF101 = selectableForF101;
            IsDocument = isDocument;
            HasBackSide = hasBackSide;
            FrontTexture = frontTexture;
            BackTexture = backTexture;
            DetailLabels = detailLabels ?? Array.Empty<string>();
        }

        public bool AllowsLayer(Reality layer) =>
            AllowedLayers != null && Array.IndexOf(AllowedLayers, layer) >= 0;

        public bool AllowsLocation(string location) =>
            !string.IsNullOrEmpty(location) && AllowedLocations != null &&
            Array.Exists(AllowedLocations, l => string.Equals(l, location, StringComparison.OrdinalIgnoreCase));

        public bool AllowsChapter(string chapter) =>
            !string.IsNullOrEmpty(chapter) && AllowedChapters != null &&
            Array.Exists(AllowedChapters, c => string.Equals(c, chapter, StringComparison.OrdinalIgnoreCase));
    }

    public sealed class DeductionClaimSpec
    {
        public readonly string Id;
        public readonly string Label;
        public readonly HypothesisOutcome FailureOutcome;
        public readonly string FailureDiagnostic;

        public DeductionClaimSpec(
            string id,
            string label,
            HypothesisOutcome failureOutcome = HypothesisOutcome.Valid,
            string failureDiagnostic = "")
        {
            Id = id;
            Label = label;
            FailureOutcome = failureOutcome;
            FailureDiagnostic = failureDiagnostic;
        }
    }

    public static class ObservationRegistry
    {
        public const string F101Sheet = "OBS101_F101";
        public const string Bed4Traces = "OBS101A";
        public const string Bed4Card = "OBS101B";
        public const string Bed2Transfer = "OBS101C";
        public const string Bed3Meds = "OBS101D";
        public const string ShiftGuide = "OBS_SHIFT_LOG";
        public const string WardOverflowNotice = "OBS_CORRIDOR_NOTICE";
        public const string ReceptionDrawerLog = "OBS_RECEPTION_DRAWER";
        public const string StoppedClock0317 = "OBS_CLOCK_0317";

        // CH02 Reality.R physical observations
        public const string Ch02List = "LIST";
        public const string Ch02Sockets = "SOCKETS";
        public const string Ch02Wheels = "WHEELS";
        public const string Ch05PostedMap = "OBS_CH05_POSTED_MAP";
        public const string Ch05AsBuiltBlueprint = "OBS_CH05_ASBUILT_E05";
        public const string Ch05ValveSabotage = "OBS_LOC09_VALVE_SABOTAGE";
        public const string Ch05SealedWall = "OBS_LOC11_E05B_WALL";

        private static readonly ObservationSpec[] OrderedSpecs =
        {
            new(
                F101Sheet, "F101", "Phiếu bàn giao ca đêm F101",
                "Bảng in liệt kê giường 01, 02, 03 đều đã chốt hồ sơ; ô tổng hợp ghi tay số 0 bệnh nhân chưa bàn giao.",
                "Tuyên bố cần kiểm chứng: phiếu không hề có dòng giường 04; mặt sau có ghi chú bút chì nhắc kiểm hốc giường tạm cuối phòng.",
                "Ca đêm 18/11/2005 · Quầy tiếp nhận / Phòng 101",
                ObservationCategory.Claim,
                new[] { Reality.B },
                new[] { "LOC02", "LOC03" },
                new[] { "CH01", "CH02" },
                selectableForF101: true,
                isDocument: true,
                hasBackSide: true,
                frontTexture: "F101",
                backTexture: "F101_Back",
                detailLabels: new[]
                {
                    "Bảng kê 3 giường (01–03): không có dòng nào cho giường số 4.",
                    "Ô tổng hợp ghi tay '0 (Không còn tồn đọng)' kèm yêu cầu đối chiếu thực tế.",
                    "Mặt sau: nét bút chì nhắc đối chiếu lại hốc sau rèm trước khi xác nhận số 0."
                }),
            new(
                Bed4Traces, "OBS101A", "Dấu sử dụng tại vị trí giường 4",
                "Sau rèm cuối phòng 101 có tấm chăn bông gấp vội và đôi dép cỡ nhỏ để lệch bên mép sàn.",
                "Những dấu vết này cho thấy chỗ kê giường từng được sử dụng. Chưa rõ ai đã dùng hoặc rời đi lúc nào.",
                "Ca đêm 18/11/2005 · Hốc giường 4, Phòng 101",
                ObservationCategory.Fact,
                new[] { Reality.B },
                new[] { "LOC03" },
                new[] { "CH01", "CH02" },
                selectableForF101: true,
                isDocument: false,
                hasBackSide: false,
                detailLabels: new[]
                {
                    "Tấm chăn bông gấp vội đặt lệch ở góc khuất sau rèm.",
                    "Đôi dép cỡ nhỏ để lệch hướng bên mép sàn gần chân giường tạm."
                }),
            new(
                Bed4Card, "OBS101B", "Thẻ bàn giao giường số 4 (AS-05B)",
                "Thẻ ghi ngày 17/11/2005, 'Khám sốt tạm', khoanh tròn 'CHƯA BÀN GIAO', trống ô chữ ký người nhận và nhòe dấu mực xanh.",
                "Thẻ vẫn giữ tình trạng dở dang từ đêm 17–18/11. Ô ký nhận còn trống nên chưa rõ sau đó ca trực đã làm gì.",
                "Ca đêm 18/11/2005 · Giá treo đầu giường 4, Phòng 101",
                ObservationCategory.Fact,
                new[] { Reality.B },
                new[] { "LOC03" },
                new[] { "CH01", "CH02" },
                selectableForF101: true,
                isDocument: true,
                hasBackSide: true,
                frontTexture: "ObsCard",
                backTexture: "ObsCard_Back",
                detailLabels: new[]
                {
                    "Mục tình trạng khoanh tròn 'CHƯA BÀN GIAO' bằng bút bi xanh.",
                    "Ô người giao chỉ có nét ký tắt nhòe mực. Ô người nhận để trống hoàn toàn.",
                    "Mặt sau: ghi chú bút chì nhòe nước — nghiêng mặt giấy đón sáng lộ vết hằn bút bi bị gạch xóa."
                }),
            new(
                Bed2Transfer, "OBS101C", "Phiếu chuyển giường 02 (Đã hoàn tất)",
                "Giường 02 chuyển khoa Nội B lúc 19g20 ngày 17/11/2005: đủ mã hồ sơ, nơi tiếp nhận, chữ ký cả hai phía giao–nhận và dấu đỏ xác nhận.",
                "Phiếu này cho thấy nơi đến và người nhận được ghi thế nào khi một ca chuyển giường đã hoàn tất.",
                "Ca đêm 18/11/2005 · Đuôi giường 02, Phòng 101",
                ObservationCategory.Comparison,
                new[] { Reality.B },
                new[] { "LOC03" },
                new[] { "CH01", "CH02" },
                selectableForF101: true,
                isDocument: true,
                hasBackSide: true,
                frontTexture: "Doc_Bed2Transfer",
                backTexture: "Doc_Bed2Transfer_Back",
                detailLabels: new[]
                {
                    "Ghi rõ điểm đến 'Khoa Nội B — Tầng 2' và giờ bàn giao 19g20.",
                    "Đủ chữ ký người giao lẫn người nhận kèm dấu đỏ đã đối chiếu."
                }),
            new(
                Bed3Meds, "OBS101D", "Phiếu phát thuốc giường 03 (Đã xuất viện)",
                "Lịch cấp phát thuốc của giường 03 đã khóa lúc 10g15 ngày 17/11/2005 khi bệnh nhân xuất viện.",
                "Giờ khóa phiếu và tình trạng xuất viện có thể đối chiếu với dòng giường 03 trên F101.",
                "Ca đêm 18/11/2005 · Ngăn tủ đầu giường 03, Phòng 101",
                ObservationCategory.Comparison,
                new[] { Reality.B },
                new[] { "LOC03" },
                new[] { "CH01", "CH02" },
                selectableForF101: true,
                isDocument: true,
                hasBackSide: true,
                frontTexture: "Doc_Bed3Meds",
                backTexture: "Doc_Bed3Meds_Back",
                detailLabels: new[]
                {
                    "Các cữ thuốc ngày 15–17/11 đều đã ký phát đầy đủ.",
                    "Dấu khóa phiếu xuất viện lúc 10g15 ngày 17/11."
                }),
            new(
                ShiftGuide, "QCBG", "Quy chế bàn giao ca đêm",
                "Quy định nội bộ tại bàn điều dưỡng: chỉ ký xác nhận '0 tồn đọng' khi mọi giường đã sử dụng — kể cả giường kê tạm — đều có đủ hai chữ ký bàn giao; nếu thiếu phải để ngỏ hoặc ghi nhận bất nhất.",
                "Bối cảnh nghiệp vụ: củng cố chuẩn mực kiểm tra trước khi ký F101, không ép buộc kết luận vượt quá chứng cứ.",
                "Ca đêm 18/11/2005 · Bàn điều dưỡng, Lối nhân viên",
                ObservationCategory.Context,
                new[] { Reality.B },
                new[] { "STAFF", "LOC03" },
                new[] { "CH01", "CH02" },
                selectableForF101: true,
                isDocument: true,
                hasBackSide: true,
                frontTexture: "Doc_ShiftGuide",
                backTexture: "Doc_ShiftGuide_Back",
                detailLabels: new[]
                {
                    "Mục 2: Giường kê tạm cuối phòng bắt buộc đối chiếu thẻ đầu giường riêng.",
                    "Mục 3: Không được xác nhận số 0 nếu phiếu bàn giao còn thiếu chữ ký nhận."
                }),
            new(
                WardOverflowNotice, "TB-101", "Thông báo kê thêm giường tạm phòng 101",
                "Thông báo tháng 11/2005 dán ở lối nhân viên cho phép kê tạm 01 giường vị trí số 4 cuối phòng 101 khi quá tải, dùng thẻ rời AS-05B vì mẫu F101 in sẵn chỉ có 3 dòng.",
                "Bối cảnh không gian: giải thích lý do giường 4 không xuất hiện sẵn trong bảng in của F101 mà không suy đoán ác ý.",
                "Ca đêm 18/11/2005 · Bảng tin lối nhân viên",
                ObservationCategory.Context,
                new[] { Reality.B },
                new[] { "STAFF", "HALL", "LOC03" },
                new[] { "CH01", "CH02" },
                selectableForF101: true,
                isDocument: true,
                hasBackSide: false,
                frontTexture: "Doc_WardOverflow",
                detailLabels: new[]
                {
                    "Cho phép bố trí giường tạm số 4 sau rèm vải cuối phòng 101.",
                    "Mẫu F101 in sẵn 3 giường nên giường tạm phải kẹp thẻ rời AS-05B."
                }),
            new(
                ReceptionDrawerLog, "ST-1105", "Sổ phân ca trực 17–18/11/2005",
                "Sổ trực trong ngăn kéo quầy tiếp nhận ghi đêm 17/11 đông bệnh nhân, điều dưỡng Lan vừa trực quầy vừa chạy buồng nên nhiều phiếu kẹp tạm chưa kịp đóng tập.",
                "Bối cảnh con người: giải thích áp lực ca trực và vì sao hồ sơ có thể bị bỏ dở mà không vội quy kết cố ý làm giả.",
                "Ngăn kéo quầy tiếp nhận",
                ObservationCategory.Context,
                new[] { Reality.B, Reality.R },
                new[] { "LOC02" },
                new[] { "CH01", "CH02" },
                selectableForF101: true,
                isDocument: true,
                hasBackSide: false,
                frontTexture: "Doc_DutyRoster",
                detailLabels: new[]
                {
                    "Ghi chú ca đêm: thiếu người trực phụ, nhiều thẻ giường tạm chưa chốt cuối ca."
                }),
            new(
                StoppedClock0317, "ĐH-0317", "Đồng hồ quầy tiếp nhận dừng ở 03:17",
                "Chiếc đồng hồ nhựa tròn sau quầy tiếp nhận đứng kim đúng 03:17.",
                "Chi tiết hiện trường gợi mốc thời gian bất thường của đêm cháy, không phải bằng chứng trực tiếp cho số bệnh nhân trên F101.",
                "Quầy tiếp nhận",
                ObservationCategory.Context,
                new[] { Reality.B, Reality.R },
                new[] { "LOC02" },
                new[] { "CH01", "CH02" },
                selectableForF101: true,
                isDocument: false,
                hasBackSide: false,
                detailLabels: new[]
                {
                    "Kim đồng hồ đứng yên ở 03:17 dù tầng trệt vẫn sáng đèn."
                }),
            // CH02 Reality.R observations (compatible with M1-P/M2 PZ01)
            new(
                Ch02List, "LIST", "Bản kê 3 giường phòng 101 (Hiện thực R)",
                "Bản kê vật lý cháy xém cạnh giường 3 vẫn chỉ ghi 3 giường cố định trong phòng 101.",
                "Đối chiếu thực tế phòng 101: mâu thuẫn với 4 cụm ổ cắm thực tế trên tường.",
                "Lớp R · Phòng 101",
                ObservationCategory.Fact,
                new[] { Reality.R },
                new[] { "LOC03" },
                new[] { "CH02", "CH03", "CH04", "CH05" },
                selectableForF101: false),
            new(
                Ch02Sockets, "SOCKETS", "Bốn cụm ổ cắm và đầu gọi y tá (Hiện thực R)",
                "Tường phòng 101 thực tế có 4 cụm ổ cắm, nhiều hơn bản kê 3 giường đúng một vị trí ở góc bị tủ kho che.",
                "Đối chiếu thực tế phòng 101: vị trí giường thứ tư từng được bố trí thực sự.",
                "Lớp R · Cuối phòng 101",
                ObservationCategory.Fact,
                new[] { Reality.R },
                new[] { "LOC03" },
                new[] { "CH02", "CH03", "CH04", "CH05" },
                selectableForF101: false),
            new(
                Ch02Wheels, "WHEELS", "Vết bánh đẩy ngang sàn tới tủ kho (Hiện thực R)",
                "Nền gạch lớp R còn vệt bánh nặng dẫn thẳng tới chiếc tủ kho bị đẩy chắn vào hốc cuối phòng.",
                "Đối chiếu thực tế phòng 101: sau chiếc tủ kho có khoảng trống từng kê giường 4.",
                "Lớp R · Góc tủ kho, Phòng 101",
                ObservationCategory.Fact,
                new[] { Reality.R },
                new[] { "LOC03" },
                new[] { "CH02", "CH03", "CH04", "CH05" },
                selectableForF101: false),
            new(
                Ch05PostedMap, "SD-2005", "Sơ đồ thoát hiểm niêm yết 2005",
                "Bản sơ đồ niêm yết trên bảng trực năm 2005 vẫn vẽ một cửa thoát hiểm phía Đông mở từ Khu C ra hào kỹ thuật ngoài trời.",
                "Cần chồng lên bản hoàn công cải tạo tháng 06/2004 theo hai mốc cầu thang S2 và trục cột để kiểm mâu thuẫn.",
                "Tủ bản vẽ tầng hầm B1",
                ObservationCategory.Claim,
                new[] { Reality.R },
                new[] { "LOC09" },
                new[] { "CH05" },
                selectableForF101: false,
                isDocument: true,
                hasBackSide: false,
                frontTexture: "Doc_Ch05_PostedMap2005",
                detailLabels: new[]
                {
                    "Vẽ mũi tên thoát hiểm xuyên tường Đông khu bán hầm.",
                    "Đóng dấu lưu hành bảng trực Khu C năm 2005 nhưng không phản ánh cải tạo 06/2004."
                }),
            new(
                Ch05AsBuiltBlueprint, "HC-2004", "Bản vẽ hoàn công cải tạo B1 · 06/2004",
                "Bản hoàn công tháng 06/2004 có chữ ký phê duyệt của Giám đốc Phạm Đức Vĩnh: xây gạch đặc 220 mm, trát vữa hai mặt bít vĩnh viễn cửa thoát hiểm phía Đông và tháo bỏ tay nắm, khung cửa theo yêu cầu cô lập của đơn vị tài trợ Chương trình B1.",
                "Chỉ riêng bản vẽ chưa đủ kết luận tình trạng hiện trường năm 2012 — cần quan sát thực địa bức tường ngoài phía Đông.",
                "Tủ bản vẽ tầng hầm B1",
                ObservationCategory.Fact,
                new[] { Reality.R },
                new[] { "LOC09" },
                new[] { "CH05" },
                selectableForF101: false,
                isDocument: true,
                hasBackSide: false,
                frontTexture: "Doc_Ch05_AsBuilt2004_E05",
                detailLabels: new[]
                {
                    "Ghi chú thi công: tường gạch đặc 220 mm trát kín hai mặt tại lối thoát Đông.",
                    "Phê duyệt: Giám đốc Phạm Đức Vĩnh · Tháng 06/2004 · Yêu cầu cô lập Chương trình B1."
                }),
            new(
                Ch05ValveSabotage, "VAN-B1", "Van bồn chứa dung môi & tấm chắn gió",
                "Ổ khóa đồng thau rỉ sẫm trên van xả bồn dung môi kỹ thuật mang vết cắt/cạy cơ học đã cũ ám muội; ngay phía trên, một chốt thép rỉ chèn mở tấm chắn gió hướng sang Khu C.",
                "Gợi nghi vấn có cơ sở về nguyên nhân vụ cháy khi đối chiếu với mùi dung môi lạ ghi trong sổ ca đêm — nhưng tự thân dấu vết không đủ kết luận thủ phạm, nguồn phát lửa hay thời điểm can thiệp.",
                "Khoang kỹ thuật tầng hầm B1",
                ObservationCategory.Context,
                new[] { Reality.R },
                new[] { "LOC09" },
                new[] { "CH05" },
                selectableForF101: false,
                isDocument: false,
                detailLabels: new[]
                {
                    "Vết cắt cơ học ám muội lâu năm trên ổ khóa van bồn dung môi.",
                    "Tấm chắn gió bị chèn mở về phía thông gió Khu C."
                }),
            new(
                Ch05SealedWall, "TB-2004", "Mảng tường gạch xây bít cửa Đông 2004",
                "Quan sát thực địa tại hố quan sát phía Đông: khối gạch đặc 220 mm và lớp vữa xi măng năm 2004 bít kín hoàn toàn ô cửa cũ, không còn khung hay tay nắm, bậc cấp bê tông nứt vỡ dẫn cụt vào tường.",
                "Xác nhận trực tiếp tại hiện trường 2012 rằng lối thoát phía Đông ghi trên sơ đồ 2005 đã bị bít vĩnh viễn từ trước vụ cháy.",
                "Hào quan sát phía Đông tầng hầm B1",
                ObservationCategory.Fact,
                new[] { Reality.R },
                new[] { "LOC11" },
                new[] { "CH05" },
                selectableForF101: false,
                isDocument: false,
                detailLabels: new[]
                {
                    "Gạch đặc xây bít khít ô cửa cũ ngoài hào kỹ thuật phía Đông.",
                    "Không có lối mở hay khe bí mật xuyên tường vào Khu C."
                })
        };

        private static readonly Dictionary<string, ObservationSpec> ById;
        private static readonly List<ObservationSpec> Catalog;

        static ObservationRegistry()
        {
            ById = new Dictionary<string, ObservationSpec>(StringComparer.Ordinal);
            Catalog = new List<ObservationSpec>(OrderedSpecs);
            foreach (var prop in Ch01FieldObjects.All) Catalog.Add(prop.Observation);
            foreach (var spec in Catalog) ById.Add(spec.Id, spec);
        }

        public static IReadOnlyList<ObservationSpec> All => Catalog;

        public static bool Registered(string id) =>
            !string.IsNullOrEmpty(id) && ById.ContainsKey(id);

        public static ObservationSpec Get(string id) =>
            !string.IsNullOrEmpty(id) && ById.TryGetValue(id, out var spec) ? spec : null;

        public static bool ValidProvenance(string id, Reality layer, string location, string chapter)
        {
            var spec = Get(id);
            return spec != null && spec.AllowsLayer(layer) && spec.AllowsLocation(location) && spec.AllowsChapter(chapter);
        }

        public static string StageLabel(ObservationStage stage) => stage switch
        {
            ObservationStage.Discovered => "Đã phát hiện",
            ObservationStage.Examined => "Đã kiểm tra",
            ObservationStage.Interpreted => "Đã phân tích",
            ObservationStage.Corroborated => "Đã đối chiếu",
            _ => "Chưa rõ"
        };

        public static string CategoryLabel(ObservationCategory cat) => cat switch
        {
            ObservationCategory.Fact => "Dữ kiện thực tế",
            ObservationCategory.Comparison => "Tài liệu đối chiếu",
            ObservationCategory.Context => "Bối cảnh",
            ObservationCategory.Claim => "Tuyên bố cần kiểm",
            _ => "Ghi nhận"
        };
    }

    public static class HypothesisService
    {
        public const string F101PuzzleId = "CH01-F101";
        public const string ClaimUnreliableZero = "F101_CLAIM_UNRELIABLE_ZERO";
        public const string ClaimPatientPresent = "F101_CLAIM_PATIENT_PRESENT";
        public const string ClaimFalsification = "F101_CLAIM_FALSIFICATION";
        public const string ClaimClericalOk = "F101_CLAIM_CLERICAL_OK";
        public const string ClaimBlankSignature = "F101_CLAIM_BLANK_SIGNATURE";

        private static readonly DeductionClaimSpec[] F101ClaimsList =
        {
            new(
                ClaimBlankSignature,
                "Thẻ giường tạm còn trống ô chữ ký người nhận.",
                HypothesisOutcome.UnsupportedConclusion,
                "Ô ký nhận đúng là còn trống… nhưng nếu chỉ chép lại chi tiết đó thì mình vẫn chưa nói rõ nó ảnh hưởng gì đến con số tổng hợp trên phiếu."),
            new(
                ClaimPatientPresent,
                "Lúc chốt ca, người bệnh ở giường tạm vẫn còn trong phòng 101.",
                HypothesisOutcome.UnsupportedConclusion,
                "Những dấu vết này cho thấy giường từng có người nằm. Mình vẫn chưa biết họ ở đâu lúc chốt ca."),
            new(
                ClaimClericalOk,
                "Giường tạm đã bàn giao xong, chỉ còn thiếu chữ ký.",
                HypothesisOutcome.ContradictsDecision,
                "Nếu mình tự trấn an rằng ca bàn giao đã hoàn tất chỉ thiếu mỗi chữ ký, thì khác gì mình lại nhắm mắt ký bừa như hồi đó — đâu có căn cứ nào cho thấy họ đã chuyển đi xong."),
            new(
                ClaimUnreliableZero,
                "Đối chiếu với dấu vết ở giường 4 và ô ký nhận còn trống, chưa đủ căn cứ xác nhận số 0."),
            new(
                ClaimFalsification,
                "Người lập phiếu cố ý bỏ qua giường tạm để chốt ca.",
                HypothesisOutcome.UnsupportedConclusion,
                "Hồ sơ bỏ dở. Mình chưa có căn cứ để nói người lập phiếu đã biết gì đêm đó.")
        };

        private static readonly Dictionary<string, DeductionClaimSpec> ClaimsById;

        static HypothesisService()
        {
            ClaimsById = new Dictionary<string, DeductionClaimSpec>(StringComparer.Ordinal);
            for (int i = 0; i < F101ClaimsList.Length; i++)
                ClaimsById[F101ClaimsList[i].Id] = F101ClaimsList[i];
        }

        public static IReadOnlyList<DeductionClaimSpec> F101Claims => F101ClaimsList;

        public static DeductionClaimSpec GetClaim(string claimId) =>
            !string.IsNullOrEmpty(claimId) && ClaimsById.TryGetValue(claimId, out var spec) ? spec : null;

        public static bool KnownClaim(string claimId) =>
            !string.IsNullOrEmpty(claimId) && ClaimsById.ContainsKey(claimId);

        public static bool IsIrrelevantForF101(string obsId) =>
            obsId == ObservationRegistry.Bed3Meds ||
            obsId == ObservationRegistry.StoppedClock0317 ||
            obsId == ObservationRegistry.ReceptionDrawerLog;

        public static bool IsSupportingContextForF101(string obsId) =>
            obsId == ObservationRegistry.F101Sheet ||
            obsId == ObservationRegistry.Bed2Transfer ||
            obsId == ObservationRegistry.ShiftGuide ||
            obsId == ObservationRegistry.WardOverflowNotice;

        public static bool HasValidCoreObservations(Investigation state)
        {
            if (state == null) return false;
            return HasExaminedInLayerB(state, ObservationRegistry.Bed4Traces) &&
                   HasExaminedInLayerB(state, ObservationRegistry.Bed4Card);
        }

        public static bool HasExaminedInLayerB(Investigation state, string id)
        {
            if (state == null || !state.Observed(id)) return false;
            var rec = state.FindObservation(id);
            if (rec == null) return true; // Legacy fallback before records exist
            return rec.layer == Reality.B && rec.stage >= ObservationStage.Examined &&
                   ObservationRegistry.ValidProvenance(id, rec.layer, rec.location, rec.chapter);
        }

        /// <summary>
        /// Harness-only fault-injection hook for automated probe <c>v36-f101-unverified-link</c> (Phase 2).
        /// Never enabled in gameplay code; toggled exclusively inside try/finally by Phase 2 QA.
        /// </summary>
        internal static bool QaInjectBypassLinkProvenanceForTesting;

        public static HypothesisEvaluation EvaluateF101LiveCommit(
            Investigation state,
            FormStatus decision,
            DeductionSubmission submission) => EvaluateF101Core(state, decision, submission, liveCommit: true);

        public static HypothesisEvaluation EvaluateF101(
            Investigation state,
            FormStatus decision,
            DeductionSubmission submission) => EvaluateF101Core(state, decision, submission, liveCommit: false);

        private static HypothesisEvaluation EvaluateF101Core(
            Investigation state,
            FormStatus decision,
            DeductionSubmission submission,
            bool liveCommit)
        {
            if (state == null || !Enum.IsDefined(typeof(FormStatus), decision) || decision == FormStatus.Pending)
            {
                return HypothesisEvaluation.Reject(
                    HypothesisOutcome.ContradictsDecision,
                    "Mình chưa đặt bút chọn phương án nào",
                    "Trên phiếu F101 vẫn phải chọn rõ một trong ba cách xử lý trước khi đóng dấu.");
            }

            // Affirmed and Unverified are ALWAYS allowed without blocking CH01 progression.
            if (decision == FormStatus.Affirmed)
            {
                return HypothesisEvaluation.Pass(
                    "Đã xác nhận số 0 theo phiếu",
                    "Minh ký nhận báo cáo 0 bệnh nhân chưa bàn giao theo bảng F101.");
            }

            if (decision == FormStatus.Unverified)
            {
                return HypothesisEvaluation.Pass(
                    "Đã để ngỏ — chưa chốt số 0",
                    "Minh giữ thái độ thận trọng, từ chối xác nhận số 0 khi chưa đủ căn cứ khép hồ sơ.");
            }

            // Refuted requires genuine authoritative reasoning: valid evidence IDs, Layer B provenance,
            // both complementary core observations (OBS101A + OBS101B), no irrelevant padding, and a defensible conclusion.
            if (submission == null || !string.Equals(submission.formId, "F101", StringComparison.OrdinalIgnoreCase))
            {
                return HypothesisEvaluation.Reject(
                    HypothesisOutcome.InsufficientEvidence,
                    "Mình chưa gắn căn cứ nào vào phiếu",
                    "Nếu gạch bác con số 0 mà không ghi rõ mình đã nhìn thấy gì, tờ phiếu này cũng vô nghĩa như hồi đó.");
            }

            var selected = submission.selectedEvidenceIds ?? new List<string>();
            if (selected.Count == 0)
            {
                return HypothesisEvaluation.Reject(
                    HypothesisOutcome.InsufficientEvidence,
                    "Mình đang phản bác bằng tay không",
                    "Mình phải đối chiếu giữa dòng chữ trên hồ sơ với những dấu vết thật mắt mình vừa thấy trong phòng.",
                    "Tìm hai thứ khớp nhau: một dấu vết cho thấy chỗ giường 4 từng có người nằm, và một tờ giấy ghi nhận bàn giao còn bỏ dở.");
            }

            var unique = new HashSet<string>(StringComparer.Ordinal);
            bool hasA = false;
            bool hasB = false;

            for (int i = 0; i < selected.Count; i++)
            {
                string id = selected[i];
                if (string.IsNullOrEmpty(id) || !ObservationRegistry.Registered(id))
                {
                    return HypothesisEvaluation.Reject(
                        HypothesisOutcome.UnknownEvidenceId,
                        "Ghi nhận này không có trong hồ sơ",
                        $"Mình không tìm thấy mục '{id ?? ""}' nào trong sổ ghi chép ca trực này.");
                }
                if (!unique.Add(id))
                    continue;

                var spec = ObservationRegistry.Get(id);
                if (!spec.SelectableForF101)
                {
                    return HypothesisEvaluation.Reject(
                        HypothesisOutcome.InvalidProvenance,
                        "Mình đang trộn lẫn hai thời điểm",
                        $"'{spec.Title}' thuộc một hiện trường khác — không thể lấy nó để bác bỏ tờ phiếu của đêm 2005 này.");
                }

                if (!state.Observed(id))
                {
                    return HypothesisEvaluation.Reject(
                        HypothesisOutcome.InvalidProvenance,
                        "Mình chưa tận mắt kiểm tra chỗ đó",
                        $"Mình không thể viện dẫn '{spec.Title}' khi chính mắt mình còn chưa kiểm tra ở phòng bệnh.");
                }

                var rec = state.FindObservation(id);
                if (rec != null)
                {
                    if (rec.layer != Reality.B || !ObservationRegistry.ValidProvenance(id, rec.layer, rec.location, rec.chapter))
                    {
                        return HypothesisEvaluation.Reject(
                            HypothesisOutcome.InvalidProvenance,
                            "Nguồn đối chiếu không thuộc ca trực này",
                            $"'{spec.Title}' không xuất phát từ ca trực năm 2005 nên không đứng vững trước phiếu F101.");
                    }
                    if (rec.stage < ObservationStage.Examined)
                    {
                        return HypothesisEvaluation.Reject(
                            HypothesisOutcome.UnexaminedEvidence,
                            "Mình mới chỉ lướt qua thôi",
                            $"Phải cầm lên xem kỹ '{spec.Title}' đã — ký bác một tờ phiếu không thể dựa vào cái nhìn thoáng qua.");
                    }
                }

                if (IsIrrelevantForF101(id))
                {
                    string reason = id switch
                    {
                        ObservationRegistry.Bed3Meds => "Phiếu thuốc giường 03 chỉ xác nhận ca xuất viện đã khớp với bản kê. Mình đang bám nhầm tờ giấy rồi.",
                        ObservationRegistry.StoppedClock0317 => "Chiếc đồng hồ dừng ở 03:17 khiến mình lạnh sống lưng, nhưng tự nó không chứng minh giường nào bị sót trên F101 — phải ghi vào nhánh ngoại lệ riêng.",
                        _ => "Sổ phân ca chỉ nói đêm đó thiếu người chạy buồng, không ghi giường nào còn dở bàn giao."
                    };
                    return HypothesisEvaluation.Reject(
                        HypothesisOutcome.IrrelevantEvidence,
                        "Chi tiết này không trả lời con số 0 trên F101",
                        reason);
                }

                if (id == ObservationRegistry.Bed4Traces) hasA = true;
                else if (id == ObservationRegistry.Bed4Card) hasB = true;
                else if (!IsSupportingContextForF101(id))
                {
                    return HypothesisEvaluation.Reject(
                        HypothesisOutcome.IrrelevantEvidence,
                        "Mình đang kéo chi tiết ngoài lề vào phiếu",
                        $"'{spec.Title}' chưa đụng tới chỗ mâu thuẫn thật sự của con số 0 trên F101.");
                }
            }

            if (!hasA || !hasB)
            {
                string missingMsg = !hasA && !hasB
                    ? "Mấy tờ quy chế chỉ cho mình thấy cách ghi sổ đúng. Mình vẫn cần dấu vết thật ở chỗ giường 4 và tờ thẻ bàn giao đi kèm."
                    : !hasA
                        ? "Chỉ mỗi tấm thẻ trống chữ ký thì ai cũng có thể bảo là giấy thừa bỏ quên. Có gì sau tấm rèm chứng minh chỗ giường đó từng được dùng không?"
                        : "Tấm chăn gấp vội với đôi dép nhỏ cho thấy góc phòng từng được dùng… nhưng trên giấy tờ ca trực có phiếu nào ghi nhận tình trạng bàn giao ở đó không?";
                return HypothesisEvaluation.Reject(
                    HypothesisOutcome.InsufficientEvidence,
                    "Vẫn còn hụt một nửa câu chuyện",
                    missingMsg,
                    !hasA
                        ? "Kéo rèm góc cuối phòng 101 xem dưới nền còn dấu vết thật nào không."
                        : "Kiểm lại giá treo đầu giường 4 xem có thẻ theo dõi nào còn trống chữ ký người nhận không.");
            }

            var claim = GetClaim(submission.claimId);
            if (claim == null)
            {
                return HypothesisEvaluation.Reject(
                    HypothesisOutcome.UnsupportedConclusion,
                    "Mình chưa chốt được lời ghi chú cho phiếu",
                    "Mình phải viết rõ điều giấy tờ và phòng bệnh thực sự chứng minh được về con số 0 — không nhiều hơn, không ít hơn.");
            }

            if (claim.FailureOutcome != HypothesisOutcome.Valid)
            {
                return HypothesisEvaluation.Reject(
                    claim.FailureOutcome,
                    claim.FailureOutcome == HypothesisOutcome.ContradictsDecision
                        ? "Mình đang tự biện hộ cho con số 0"
                        : "Mình đang suy diễn vượt quá giấy tờ",
                    claim.FailureDiagnostic,
                    "Bỏ nỗi sợ sang một bên — nhìn lại đúng những gì trên giấy và dưới nền gạch chứng minh được thôi.");
            }

            bool strictCh01Commit = liveCommit && string.Equals(state.chapter, "CH01", StringComparison.Ordinal) &&
                                    !QaInjectBypassLinkProvenanceForTesting;

            if (strictCh01Commit || submission.annotationVersion != 0)
            {
                if (submission.annotationVersion != F101Annotations.Version ||
                    submission.annotations?.SupportsRefutation(state) != true ||
                    !unique.Contains(ObservationRegistry.F101Sheet))
                {
                    string detailMessage = !unique.Contains(ObservationRegistry.F101Sheet)
                        ? "Cần kẹp cả tờ phiếu F101 vào nhóm nguồn phản bác để chỉ rõ con số đang đối chiếu."
                        : submission.annotations == null || string.IsNullOrEmpty(submission.annotations.MarkedHotspot(ObservationRegistry.F101Sheet))
                            ? "Chưa đánh dấu dòng số 0 trên tờ F101 tại bảng kẹp AN02."
                            : !submission.annotations.HasLink
                                ? "Chưa đối chứng trực tiếp thẻ AS-05B đã đánh dấu với dấu sử dụng tại hốc giường 4."
                                : "Mình đã có nguồn, nhưng chưa chỉ rõ đúng dòng số 0 trên F101 và ô chữ ký người nhận trên thẻ AS-05B.";
                    return HypothesisEvaluation.Reject(
                        HypothesisOutcome.InsufficientEvidence,
                        "Mình chưa nối đúng chỗ vênh giữa giấy và phòng",
                        detailMessage);
                }

                if (strictCh01Commit)
                {
                    var provenance = F101Annotations.ResolveLinkProvenance(state, submission);
                    if (provenance != F101LinkProvenance.SpatialFieldVerified)
                    {
                        return HypothesisEvaluation.Reject(
                            HypothesisOutcome.InvalidProvenance,
                            "Chưa đối chứng trực tiếp tại hốc giường 4",
                            "Liên kết giấy tờ chưa được xác thực tại hiện trường. Hãy mang thẻ AS-05B đã đánh dấu ô người nhận tới sau rèm giường 4 để đối chiếu với dấu dép và chăn.");
                    }
                }
            }

            return HypothesisEvaluation.Pass(
                "Đủ căn cứ giữ lại con số này",
                "Dấu vết sau rèm giường 4 cộng với ô ký nhận bỏ trống trên thẻ AS-05B đã quá rõ — con số 0 trên phiếu F101 không thể ký xác nhận được.");
        }

        public static bool ValidPersistedF101(Investigation state)
        {
            if (state == null || state.f101 == null) return false;
            if (state.f101Annotations?.ValidFor(state) != true) return false;
            if (state.f101.revision == 0)
            {
                return (state.f101History == null || state.f101History.Count == 0) &&
                       (state.f101Argument == null || string.IsNullOrEmpty(state.f101Argument.claimId));
            }
            if (state.f101History == null || state.f101History.Count != state.f101.revision)
                return false;
            var latest = state.f101History[state.f101History.Count - 1];
            if (state.f101History[0]?.decision != state.f101.firstDecision || latest?.decision != state.f101.current)
                return false;
            for (int i = 0; i < state.f101History.Count; i++)
            {
                var h = state.f101History[i];
                if (h == null || h.revision != i + 1 || !Enum.IsDefined(typeof(FormStatus), h.decision) ||
                    h.decision == FormStatus.Pending)
                    return false;
                if (h.evidenceIds != null && h.evidenceIds.Exists(id => !ObservationRegistry.Registered(id)))
                    return false;
                if (!string.IsNullOrEmpty(h.claimId) && !KnownClaim(h.claimId))
                    return false;
                if (h.annotationVersion != 0 &&
                    (h.decision != FormStatus.Refuted || !EvaluateF101(state, h.decision, new DeductionSubmission
                    {
                        claimId = h.claimId,
                        selectedEvidenceIds = h.evidenceIds,
                        annotationVersion = h.annotationVersion,
                        annotations = h.annotations
                    }).Accepted))
                    return false;
            }
            if (state.f101.current == FormStatus.Refuted)
            {
                var eval = EvaluateF101(state, FormStatus.Refuted, state.f101Argument);
                if (!eval.Accepted || state.f101Argument.annotationVersion != latest.annotationVersion) return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Optional hidden-layer corroboration bonus (Upgrade #5 — "Ghi chú ngoại lệ · Mốc 03:17").
    /// Connects the stopped 03:17 wall clock (OBS_CLOCK_0317), shift-context paperwork (OBS_RECEPTION_DRAWER /
    /// OBS_SHIFT_LOG / OBS_CORRIDOR_NOTICE), and Bed-4's 03g17 timestamp on AS-05B (OBS101B) to unlock
    /// Minh's chilling pre-collapse realization (CH01-D19) without altering CAN05 or blocking G01.
    /// </summary>
    public static class CorroborationService
    {
        public const string Exception0317Event = "CH01-EX01";
        public const string Exception0317Dialogue = "CH01-D19";
        public const string RakingDetailId = "RAKING_INDENT";
        public const string TargetTimecode = "03:17";

        /// <summary>
        /// Harness-only fault-injection hook for automated probe <c>v36-ex01-autosolve</c> (Phase 2).
        /// Never enabled in gameplay code; toggled exclusively inside try/finally by Phase 2 QA.
        /// </summary>
        internal static bool QaInjectEx01AutoSolveForTesting;

        public static bool IsShiftContext(string id) =>
            id is ObservationRegistry.ReceptionDrawerLog or ObservationRegistry.ShiftGuide or ObservationRegistry.WardOverflowNotice;

        public static bool HasClock(Investigation state) =>
            HypothesisService.HasExaminedInLayerB(state, ObservationRegistry.StoppedClock0317);

        public static bool HasCardTimestamp(Investigation state) =>
            state != null && HypothesisService.HasExaminedInLayerB(state, ObservationRegistry.Bed4Card) &&
            state.FindObservation(ObservationRegistry.Bed4Card)?.HasDetail(RakingDetailId) == true;

        public static bool TryResolveShiftContext(Investigation state, out string contextId, string preferredContextId = null)
        {
            contextId = null;
            if (state?.observationRecords == null) return false;
            if (!string.IsNullOrEmpty(preferredContextId) && IsShiftContext(preferredContextId) &&
                state.observationRecords.Exists(r => r.id == preferredContextId && r.stage >= ObservationStage.Examined))
            {
                contextId = preferredContextId;
                return true;
            }
            string[] order = { ObservationRegistry.ShiftGuide, ObservationRegistry.WardOverflowNotice, ObservationRegistry.ReceptionDrawerLog };
            for (int i = 0; i < order.Length; i++)
            {
                if (state.observationRecords.Exists(r => r.id == order[i] && r.stage >= ObservationStage.Examined))
                {
                    contextId = order[i];
                    return true;
                }
            }
            return false;
        }

        public static bool CanCorroborate0317(Investigation state) =>
            state != null && state.chapter == "CH01" && state.layer == Reality.B &&
            !state.Happened("CH01-T08") && HasClock(state) && HasCardTimestamp(state) &&
            TryResolveShiftContext(state, out _);

        public static string NormalizeTimecode(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            string trimmed = raw.Trim().Replace('g', ':').Replace('G', ':').Replace('h', ':').Replace('H', ':');
            string[] parts = trimmed.Split(':');
            if (parts.Length != 2) return "";
            if (!int.TryParse(parts[0], out int hh) || !int.TryParse(parts[1], out int mm)) return "";
            if (hh < 0 || hh > 23 || mm < 0 || mm > 59) return "";
            return $"{hh:00}:{mm:00}";
        }

        public static bool IsValidEx01Submission(Investigation state, string rawTimecode, out TimelineConnection resolvedLink,
            string preferredContextId = null)
        {
            resolvedLink = default;
            if (!CanCorroborate0317(state)) return false;
            if (!string.Equals(NormalizeTimecode(rawTimecode), TargetTimecode, StringComparison.Ordinal)) return false;
            if (!TryResolveShiftContext(state, out string ctx, preferredContextId)) return false;
            resolvedLink = new TimelineConnection
            {
                left = ObservationRegistry.StoppedClock0317,
                right = ObservationRegistry.Bed4Card,
                context = ctx
            };
            return true;
        }

        internal static bool Supported0317(Investigation state, TimelineConnection link) =>
            ((link.left == ObservationRegistry.StoppedClock0317 && link.right == ObservationRegistry.Bed4Card) ||
             (link.right == ObservationRegistry.StoppedClock0317 && link.left == ObservationRegistry.Bed4Card)) &&
            HasClock(state) && HasCardTimestamp(state) && IsShiftContext(link.context) && JournalNote.HasSource(state, link.context);

        public static bool IsCorroborated(Investigation state) =>
            state != null && state.Happened(Exception0317Event);

        public static bool HasRakingIndent(Investigation state)
        {
            if (state == null) return false;
            var card = state.FindObservation(ObservationRegistry.Bed4Card);
            if (card != null && card.HasDetail(RakingDetailId)) return true;
            var f101 = state.FindObservation(ObservationRegistry.F101Sheet);
            return f101 != null && f101.HasDetail(RakingDetailId);
        }

    }
}
