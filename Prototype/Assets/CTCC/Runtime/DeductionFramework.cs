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

    [Serializable]
    public sealed class F101Annotations
    {
        public const int Version = 1;
        public const string Table = "F101_TABLE";
        public const string Zero = "F101_ZERO";
        public const string CardStatus = "OBS101B_STATUS";
        public const string Receiver = "OBS101B_SIGN";
        public const string TraceGroup = "OBS101A_TRACES";

        public List<EvidenceAnnotation> marks = new();
        public EvidenceAnnotation linkFrom;
        public EvidenceAnnotation linkTo;

        public bool HasLink => !string.IsNullOrEmpty(linkFrom.observationId);
        public string MarkedHotspot(string id) => marks?.Find(mark => mark.observationId == id).hotspotId;

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

        private static readonly ObservationSpec[] OrderedSpecs =
        {
            new(
                F101Sheet, "F101", "Phiếu bàn giao ca đêm F101",
                "Bảng in liệt kê giường 01, 02, 03 đều đã chốt hồ sơ; ô tổng hợp ghi tay số 0 bệnh nhân chưa bàn giao.",
                "Tuyên bố cần kiểm chứng: phiếu không hề có dòng giường 04; mặt sau có ghi chú bút chì nhắc kiểm hốc giường tạm cuối phòng.",
                "Lớp B · Quầy tiếp nhận / Phòng 101",
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
                "Sau rèm cuối phòng 101 có vết kéo bánh giường in ngang nền, tấm chăn bông gấp vội và đôi dép nhỏ để lệch.",
                "Những dấu vết này cho thấy chỗ kê giường từng được sử dụng. Chưa rõ ai đã dùng hoặc rời đi lúc nào.",
                "Lớp B · Hốc giường 4, Phòng 101",
                ObservationCategory.Fact,
                new[] { Reality.B },
                new[] { "LOC03" },
                new[] { "CH01", "CH02" },
                selectableForF101: true,
                isDocument: false,
                hasBackSide: false,
                detailLabels: new[]
                {
                    "Vết bánh giường kéo ngang nền gạch về phía góc tường.",
                    "Tấm chăn bông gấp vội và đôi dép cỡ nhỏ để lệch bên mép sàn."
                }),
            new(
                Bed4Card, "OBS101B", "Thẻ bàn giao giường số 4 (AS-05B)",
                "Thẻ ghi ngày 17/11/2005, 'Khám sốt tạm', khoanh tròn 'CHƯA BÀN GIAO', ghi lề 'Kiểm cuối ca: 03g17', trống ô chữ ký người nhận và nhòe dấu mực xanh.",
                "Thẻ vẫn giữ tình trạng dở dang từ đêm 17–18/11 với mốc giờ cuối 03g17. Ô ký nhận còn trống nên chưa rõ sau đó ca trực đã làm gì.",
                "Lớp B · Giá treo đầu giường 4, Phòng 101",
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
                    "Ô người giao chỉ ký tắt nhòe mực kèm mốc 'Kiểm cuối ca: 03g17'; ô người nhận để trống hoàn toàn.",
                    "Mặt sau: ghi chú bút chì nhòe nước — nghiêng mặt giấy đón sáng lộ vết hằn bút bi bị gạch xóa."
                }),
            new(
                Bed2Transfer, "OBS101C", "Phiếu chuyển giường 02 (Đã hoàn tất)",
                "Giường 02 chuyển khoa Nội B lúc 19g20 ngày 17/11/2005: đủ mã hồ sơ, nơi tiếp nhận, chữ ký cả hai phía giao–nhận và dấu đỏ xác nhận.",
                "Phiếu này cho thấy nơi đến và người nhận được ghi thế nào khi một ca chuyển giường đã hoàn tất.",
                "Lớp B · Đuôi giường 02, Phòng 101",
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
                "Lớp B · Ngăn tủ đầu giường 03, Phòng 101",
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
                ShiftGuide, "AS-05", "Quy chế bàn giao ca đêm AS-05",
                "Quy định nội bộ tại bàn điều dưỡng: chỉ ký xác nhận '0 tồn đọng' khi mọi giường đã sử dụng — kể cả giường kê tạm — đều có đủ hai chữ ký bàn giao; nếu thiếu phải để ngỏ hoặc ghi nhận bất nhất.",
                "Bối cảnh nghiệp vụ: củng cố chuẩn mực kiểm tra trước khi ký F101, không ép buộc kết luận vượt quá chứng cứ.",
                "Lớp B · Góc bàn điều dưỡng, Lối nhân viên",
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
                "Lớp B · Bảng tin lối nhân viên",
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
                "Lớp B/R · Ngăn kéo quầy tiếp nhận",
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
                StoppedClock0317, "FS01", "Đồng hồ quầy tiếp nhận dừng ở 03:17",
                "Chiếc đồng hồ nhựa tròn sau quầy tiếp nhận đứng kim đúng 03:17 — trùng thời điểm vụ cháy bệnh viện An Sinh đêm 18/11/2005 mà Minh đã biết.",
                "Chi tiết hiện trường ký ức (FS01): gợi mốc thời gian bất thường của đêm cháy, không phải bằng chứng trực tiếp cho số bệnh nhân trên F101.",
                "Lớp B/R · Quầy tiếp nhận",
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
                "Đối chiếu vật lý cho PZ01: mâu thuẫn với 4 cụm ổ cắm thực tế trên tường.",
                "Lớp R · Phòng 101",
                ObservationCategory.Fact,
                new[] { Reality.R },
                new[] { "LOC03" },
                new[] { "CH02", "CH03", "CH04" },
                selectableForF101: false),
            new(
                Ch02Sockets, "SOCKETS", "Bốn cụm ổ cắm và đầu gọi y tá (Hiện thực R)",
                "Tường phòng 101 thực tế có 4 cụm ổ cắm, nhiều hơn bản kê 3 giường đúng một vị trí ở góc bị tủ kho che.",
                "Đối chiếu vật lý cho PZ01: vị trí giường thứ tư từng được bố trí thực sự.",
                "Lớp R · Cuối phòng 101",
                ObservationCategory.Fact,
                new[] { Reality.R },
                new[] { "LOC03" },
                new[] { "CH02", "CH03", "CH04" },
                selectableForF101: false),
            new(
                Ch02Wheels, "WHEELS", "Vết bánh đẩy ngang sàn tới tủ kho (Hiện thực R)",
                "Nền gạch lớp R còn vệt bánh nặng dẫn thẳng tới chiếc tủ kho bị đẩy chắn vào hốc cuối phòng.",
                "Đối chiếu vật lý cho PZ01: sau chiếc tủ kho có khoảng trống từng kê giường 4.",
                "Lớp R · Góc tủ kho, Phòng 101",
                ObservationCategory.Fact,
                new[] { Reality.R },
                new[] { "LOC03" },
                new[] { "CH02", "CH03", "CH04" },
                selectableForF101: false)
        };

        private static readonly Dictionary<string, ObservationSpec> ById;

        static ObservationRegistry()
        {
            ById = new Dictionary<string, ObservationSpec>(StringComparer.Ordinal);
            for (int i = 0; i < OrderedSpecs.Length; i++)
                ById[OrderedSpecs[i].Id] = OrderedSpecs[i];
        }

        public static IReadOnlyList<ObservationSpec> All => OrderedSpecs;

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
                "Mình đang đoán vì sợ — chăn gấp vội và đôi dép chỉ nói giường này từng có người nằm, chứ không chứng minh được họ đang ở đâu vào lúc chốt ca. Trên giấy chỉ mới chứng minh được con số 0 này có vấn đề."),
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
                "Mình muốn tin có ai đó đã cố tình xóa dấu… nhưng giấy tờ chỉ cho thấy hồ sơ bỏ dở, chưa có gì chứng minh người lập phiếu đã biết gì đêm đó. Trên giấy chỉ mới chứng minh được con số 0 này có vấn đề.")
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

        public static HypothesisEvaluation EvaluateF101(
            Investigation state,
            FormStatus decision,
            DeductionSubmission submission)
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
                        : "Vết bánh giường với đôi dép cho thấy góc phòng từng được dùng… nhưng trên giấy tờ ca trực có phiếu nào ghi nhận tình trạng bàn giao ở đó không?";
                return HypothesisEvaluation.Reject(
                    HypothesisOutcome.InsufficientEvidence,
                    "Vẫn còn hụt một nửa câu chuyện",
                    missingMsg,
                    !hasA
                        ? "Kéo rèm góc cuối phòng 101 xem dưới nền còn dấu vết thật nào không."
                        : "Kiểm lại giá treo đầu giường 4 xem có thẻ theo dõi nào còn trống chữ ký người nhận không.");
            }

            if (submission.annotationVersion != 0 &&
                (submission.annotationVersion != F101Annotations.Version ||
                 submission.annotations?.SupportsRefutation(state) != true ||
                 !unique.Contains(ObservationRegistry.F101Sheet)))
            {
                return HypothesisEvaluation.Reject(
                    HypothesisOutcome.InsufficientEvidence,
                    "Mình chưa nối đúng chỗ vênh giữa giấy và phòng",
                    "Trên F101 phải khoanh đúng con số 0, trên thẻ AS-05B phải đánh dấu ô ký nhận trống rồi nối thẳng sang dấu vết giường 4.");
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

        public static bool HasShiftContext(Investigation state) =>
            state != null && (state.Observed(ObservationRegistry.ReceptionDrawerLog) ||
                              state.Observed(ObservationRegistry.ShiftGuide) ||
                              state.Observed(ObservationRegistry.WardOverflowNotice));

        public static bool HasClock(Investigation state) =>
            state != null && state.Observed(ObservationRegistry.StoppedClock0317);

        public static bool HasCardTimestamp(Investigation state) =>
            state != null && HypothesisService.HasExaminedInLayerB(state, ObservationRegistry.Bed4Card);

        public static bool HasAnyPiece(Investigation state) =>
            HasClock(state) || HasShiftContext(state);

        public static bool CanCorroborate0317(Investigation state) =>
            state != null && state.chapter == "CH01" && state.layer == Reality.B &&
            !state.Happened("CH01-T08") && HasClock(state) && HasCardTimestamp(state) && HasShiftContext(state);

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

        public static string BuildStatusSummary(Investigation state)
        {
            if (state == null) return "";
            if (IsCorroborated(state))
                return "Đã ghi chú ngoại lệ 03:17: mốc giờ ghi dở cuối ca trên thẻ AS-05B (03g17) trùng khít với chiếc đồng hồ chết sau quầy và sổ trực đêm 17–18/11/2005.";
            if (CanCorroborate0317(state))
                return "Cả đồng hồ quầy (03:17), sổ/quy chế trực và mốc 'Kiểm cuối ca: 03g17' trên thẻ AS-05B đều đã nằm trong sổ. Có thể đối chiếu nhánh ngoại lệ 03:17.";
            if (HasClock(state) && !HasCardTimestamp(state))
                return "Kim đồng hồ quầy dừng ở 03:17. Mình cần xem trên các phiếu hoặc thẻ ở phòng 101 có mốc giờ nào trùng với khoảnh khắc đó không.";
            if (HasClock(state) && !HasShiftContext(state))
                return "Thẻ AS-05B có ghi lề 03g17 trùng với đồng hồ chết ngoài quầy — nhưng mình cần kiểm tra thêm sổ phân ca hoặc quy chế/thông báo ở lối nhân viên để xác nhận ca trực đêm 17–18/11.";
            return "Chưa đủ móc nối giữa đồng hồ 03:17, sổ trực và thẻ giường 4 AS-05B.";
        }
    }
}
