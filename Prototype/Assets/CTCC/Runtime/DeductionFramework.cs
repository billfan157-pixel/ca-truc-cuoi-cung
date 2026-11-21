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
    public sealed class DeductionSubmission
    {
        public string formId = "F101";
        public List<string> selectedEvidenceIds = new();
        public string claimId = "";

        public DeductionSubmission Clone()
        {
            var copy = new DeductionSubmission
            {
                formId = formId ?? "F101",
                claimId = claimId ?? "",
                selectedEvidenceIds = new List<string>()
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
                "Thẻ ghi ngày 17/11/2005, 'Khám sốt tạm', khoanh tròn 'CHƯA BÀN GIAO', trống ô chữ ký người nhận và nhòe dấu mực xanh; không ghi tên hay mã bệnh nhân.",
                "Thẻ vẫn ghi tình trạng lúc lập. Ô ký nhận còn trống nên chưa rõ sau đó ca trực đã làm gì.",
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
                    "Ô người giao chỉ ký tắt nhòe mực; ô người nhận để trống hoàn toàn.",
                    "Mặt sau: ghi chú bút chì hỏi ca trực xác nhận lại giường tạm đã chuyển đi hay chưa."
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
                "Ô ký nhận đúng là còn trống. Lập luận vẫn cần giải thích chi tiết đó có ý nghĩa gì đối với số tổng hợp."),
            new(
                ClaimPatientPresent,
                "Lúc chốt ca, người bệnh ở giường tạm vẫn còn trong phòng 101.",
                HypothesisOutcome.UnsupportedConclusion,
                "Dấu vết có thể còn lại sau khi người bệnh rời đi. Chưa có dữ kiện xác định họ ở đâu vào lúc chốt ca."),
            new(
                ClaimClericalOk,
                "Giường tạm đã bàn giao xong, chỉ còn thiếu chữ ký.",
                HypothesisOutcome.ContradictsDecision,
                "Ô ký nhận bỏ trống cũng có thể là do chưa bàn giao. Cần có căn cứ cho thấy ca bàn giao đã hoàn tất."),
            new(
                ClaimUnreliableZero,
                "Số 0 bỏ sót hồ sơ bàn giao còn để ngỏ của giường tạm."),
            new(
                ClaimFalsification,
                "Người lập phiếu cố ý bỏ qua giường tạm để chốt ca.",
                HypothesisOutcome.UnsupportedConclusion,
                "Mẫu in và chữ ký cho thấy hồ sơ được ghi thế nào. Chưa có dữ kiện về việc người lập phiếu đã biết gì khi chốt ca.")
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
                    "Quyết định không hợp lệ",
                    "Hãy chọn một trong ba phương án hợp lệ trên phiếu F101.");
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
                    "Chưa có lập luận",
                    "Hãy chọn các ghi nhận làm căn cứ cho nhận định về F101.");
            }

            var selected = submission.selectedEvidenceIds ?? new List<string>();
            if (selected.Count == 0)
            {
                return HypothesisEvaluation.Reject(
                    HypothesisOutcome.InsufficientEvidence,
                    "Chưa chọn bằng chứng",
                    "Cần đối chiếu điều ghi trong hồ sơ bàn giao với những gì quan sát được trong phòng.",
                    "Hãy tìm hai nguồn độc lập cho biết chỗ nào từng được dùng và ca trực ghi nhận việc bàn giao ra sao.");
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
                        "Mã chứng cứ không hợp lệ",
                        $"Không nhận diện được mã chứng cứ '{id ?? ""}' trong hồ sơ điều tra.");
                }
                if (!unique.Add(id))
                    continue;

                var spec = ObservationRegistry.Get(id);
                if (!spec.SelectableForF101)
                {
                    return HypothesisEvaluation.Reject(
                        HypothesisOutcome.InvalidProvenance,
                        "Nguồn chứng cứ không thuộc phạm vi F101",
                        $"Ghi nhận '{spec.Title}' thuộc lớp hiện thực hoặc phạm vi điều tra khác, không dùng để lập luận cho F101.");
                }

                if (!state.Observed(id))
                {
                    return HypothesisEvaluation.Reject(
                        HypothesisOutcome.InvalidProvenance,
                        "Chứng cứ chưa được quan sát thực địa",
                        $"Chưa thể đưa '{spec.Title}' vào lập luận khi Minh chưa trực tiếp quan sát ở hiện trường lớp B.");
                }

                var rec = state.FindObservation(id);
                if (rec != null)
                {
                    if (rec.layer != Reality.B || !ObservationRegistry.ValidProvenance(id, rec.layer, rec.location, rec.chapter))
                    {
                        return HypothesisEvaluation.Reject(
                            HypothesisOutcome.InvalidProvenance,
                            "Sai nguồn lớp hiện thực",
                            $"'{spec.Title}' không có nguồn gốc hợp lệ tại lớp B cho đối chiếu F101.");
                    }
                    if (rec.stage < ObservationStage.Examined)
                    {
                        return HypothesisEvaluation.Reject(
                            HypothesisOutcome.UnexaminedEvidence,
                            "Chứng cứ mới chỉ thoáng thấy",
                            $"Hãy kiểm tra kỹ '{spec.Title}' trước khi đưa vào lập luận phản bác chính thức.");
                    }
                }

                if (IsIrrelevantForF101(id))
                {
                    string reason = id switch
                    {
                        ObservationRegistry.Bed3Meds => "Phiếu thuốc xác nhận giờ xuất viện của giường 03, vốn đã khớp với bản kê. Nó chỉ giúp kiểm tra dòng đó.",
                        ObservationRegistry.StoppedClock0317 => "Đồng hồ cho thấy kim dừng ở 03:17. Chi tiết này không cho biết ca bàn giao nào còn để ngỏ.",
                        _ => "Sổ phân ca cho thấy ca trực thiếu người. Nó không ghi tình trạng bàn giao của từng giường."
                    };
                    return HypothesisEvaluation.Reject(
                        HypothesisOutcome.IrrelevantEvidence,
                        "Chi tiết chưa giúp đối chiếu số tổng hợp",
                        reason);
                }

                if (id == ObservationRegistry.Bed4Traces) hasA = true;
                else if (id == ObservationRegistry.Bed4Card) hasB = true;
                else if (!IsSupportingContextForF101(id))
                {
                    return HypothesisEvaluation.Reject(
                        HypothesisOutcome.IrrelevantEvidence,
                        "Chứng cứ ngoài phạm vi",
                        $"'{spec.Title}' không đủ liên hệ trực tiếp để bác bỏ con số 0 trên phiếu F101.");
                }
            }

            if (!hasA || !hasB)
            {
                string missingMsg = !hasA && !hasB
                    ? "Tài liệu đối chiếu giúp hiểu cách ghi sổ. Cần thêm căn cứ về việc sử dụng chỗ kê và tình trạng bàn giao tại đó."
                    : !hasA
                        ? "Một thẻ thiếu chữ ký có thể là giấy còn sót lại. Điều gì cho thấy chỗ kê ghi trên thẻ từng được dùng?"
                        : "Dấu vết cho thấy chỗ kê từng được dùng. Hồ sơ ca trực ghi gì về việc bàn giao tại đó?";
                return HypothesisEvaluation.Reject(
                    HypothesisOutcome.InsufficientEvidence,
                    "Còn thiếu căn cứ đối chiếu",
                    missingMsg,
                    !hasA
                        ? "Ngoài giấy tờ, hãy xem tại chỗ kê còn dấu vết gì."
                        : "Khi đối chiếu các phiếu, hãy xem một ca bàn giao hoàn tất thường ghi gì về người nhận.");
            }

            var claim = GetClaim(submission.claimId);
            if (claim == null)
            {
                return HypothesisEvaluation.Reject(
                    HypothesisOutcome.UnsupportedConclusion,
                    "Chưa chọn kết luận đối chiếu",
                    "Từ những gì đã chọn, hãy ghi nhận điều có thể suy ra về số tổng hợp.");
            }

            if (claim.FailureOutcome != HypothesisOutcome.Valid)
            {
                return HypothesisEvaluation.Reject(
                    claim.FailureOutcome,
                    "Cần xem lại nhận định",
                    claim.FailureDiagnostic,
                    "Hãy thử chỉ ra chi tiết nào dẫn từ điều đã quan sát đến nhận định này.");
            }

            return HypothesisEvaluation.Pass(
                "Lập luận bất nhất được xác lập",
                "Đối chiếu dấu sử dụng tại giường 4 cùng thẻ bàn giao bỏ dở cho thấy con số 0 trên F101 không đủ tin cậy để xác nhận.");
        }

        public static bool ValidPersistedF101(Investigation state)
        {
            if (state == null || state.f101 == null) return false;
            if (state.f101.revision == 0)
            {
                return (state.f101History == null || state.f101History.Count == 0) &&
                       (state.f101Argument == null || string.IsNullOrEmpty(state.f101Argument.claimId));
            }
            if (state.f101History == null || state.f101History.Count != state.f101.revision)
                return false;
            if (state.f101History[0].decision != state.f101.firstDecision ||
                state.f101History[state.f101History.Count - 1].decision != state.f101.current)
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
            }
            if (state.f101.current == FormStatus.Refuted)
            {
                var eval = EvaluateF101(state, FormStatus.Refuted, state.f101Argument);
                if (!eval.Accepted) return false;
            }
            return true;
        }
    }
}
