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
        public readonly string CategoryTag;
        public readonly bool IsSupportedForRefutation;
        public readonly HypothesisOutcome FailureOutcome;
        public readonly string FailureDiagnostic;

        public DeductionClaimSpec(
            string id,
            string label,
            string categoryTag,
            bool isSupported,
            HypothesisOutcome failureOutcome,
            string failureDiagnostic)
        {
            Id = id;
            Label = label;
            CategoryTag = categoryTag;
            IsSupportedForRefutation = isSupported;
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
                "Dữ kiện thực địa: vị trí số 4 vừa có người dùng gần đây; tự dấu vết này chưa đủ để khẳng định hiện vẫn còn người trong phòng hay danh tính của họ.",
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
                "Dữ kiện hồ sơ: phiếu bàn giao của giường 4 chưa được khép; thiếu chữ ký tự nó chưa chứng minh cố ý làm giả hay ca tử vong.",
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
                "Tài liệu đối chiếu: cho thấy một ca chuyển giường hợp lệ tại An Sinh phải có đủ 2 chữ ký và điểm đến — tương phản rõ với thẻ bỏ dở ở giường 4.",
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
                "Lịch cấp phát thuốc thường quy của giường 03 đã khóa xong từ 10g15 ngày 17/11/2005 khi bệnh nhân xuất viện, khớp hoàn toàn với dòng 03 trên F101.",
                "Tài liệu đối chiếu không liên quan sai lệch: hồ sơ giường 03 khép đủ và không bác bỏ hay chứng minh điều gì về vị trí giường 4.",
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
                    "Dấu khóa phiếu xuất viện 10g15 ngày 17/11 — hoàn toàn khớp F101."
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

        private static readonly DeductionClaimSpec[] F101ClaimsList =
        {
            new(
                ClaimUnreliableZero,
                "Con số 0 trên F101 chưa đủ tin cậy để xác nhận: có dấu vết dùng giường 4 và thẻ bàn giao bỏ dở.",
                "Kết luận có căn cứ",
                isSupported: true,
                failureOutcome: HypothesisOutcome.Valid,
                failureDiagnostic: ""),
            new(
                ClaimPatientPresent,
                "Chắc chắn hiện tại vẫn còn một bệnh nhân đang nằm lại hoặc ẩn trong phòng 101.",
                "Giả thuyết vượt chứng cứ",
                isSupported: false,
                failureOutcome: HypothesisOutcome.UnsupportedConclusion,
                failureDiagnostic: "Dấu giường 4 và thẻ thiếu chữ ký chỉ chứng tỏ vị trí đó từng có người dùng và chưa khép bàn giao — không đủ để khẳng định hiện vẫn còn người ở trong phòng."),
            new(
                ClaimFalsification,
                "Điều dưỡng trực đã cố tình làm giả phiếu F101 để che giấu một ca tử vong tại giường 4.",
                "Suy đoán quy kết",
                isSupported: false,
                failureOutcome: HypothesisOutcome.UnsupportedConclusion,
                failureDiagnostic: "Thiếu chữ ký nhận hay thiếu dòng giường tạm trên mẫu in sẵn không đủ chứng minh cố ý làm giả hồ sơ hay một ca tử vong. Hãy chọn kết luận bám sát chứng cứ."),
            new(
                ClaimClericalOk,
                "Đây chỉ là sơ sót hành chính nhỏ; vẫn có thể tin con số 0 trên F101 là đúng.",
                "Mâu thuẫn lựa chọn",
                isSupported: false,
                failureOutcome: HypothesisOutcome.ContradictsDecision,
                failureDiagnostic: "Kết luận này mâu thuẫn với mục 'Ghi nhận bất nhất'. Nếu muốn xác nhận số 0 hoặc giữ nguyên trạng chờ kiểm tra, hãy chọn đúng quyết định tương ứng.")
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
                    "Chưa lập luận chứng",
                    "Để ghi nhận bất nhất cho F101, hãy chọn các chứng cứ đối chiếu và một kết luận có căn cứ.");
            }

            var selected = submission.selectedEvidenceIds ?? new List<string>();
            if (selected.Count == 0)
            {
                return HypothesisEvaluation.Reject(
                    HypothesisOutcome.InsufficientEvidence,
                    "Chưa chọn bằng chứng",
                    "Cần chọn ít nhất hai nguồn bổ sung cho nhau: dấu sử dụng thực tế tại giường 4 và thẻ bàn giao giường 4 bỏ dở.",
                    "Gợi ý: Kiểm tra góc sau rèm cuối phòng 101 rồi đối chiếu dấu thực địa với thẻ treo đầu giường 4.");
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
                    string reason = id == ObservationRegistry.Bed3Meds
                        ? "Phiếu thuốc giường 03 đã khóa đúng ngày xuất viện và hoàn toàn khớp với F101 — tài liệu này không chứng minh sai lệch ở giường 4."
                        : id == ObservationRegistry.StoppedClock0317
                            ? "Đồng hồ dừng ở 03:17 gợi nhớ mốc đêm cháy nhưng không phải căn cứ bác bỏ số giường tồn đọng trên F101."
                            : "Sổ phân ca trực ở quầy cho thấy bối cảnh thiếu người chứ không trực tiếp bác bỏ con số 0 của phòng 101.";
                    return HypothesisEvaluation.Reject(
                        HypothesisOutcome.IrrelevantEvidence,
                        "Chứng cứ không phục vụ trực tiếp cho lập luận",
                        reason,
                        "Gợi ý: Loại bỏ hồ sơ khớp sẵn của giường 03 hoặc chi tiết ngoài buồng; chỉ giữ chứng cứ đối chiếu trực tiếp với giường 4.");
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
                    ? "Các tài liệu đối chiếu hay quy chế chỉ làm rõ bối cảnh. Cần chọn đủ cả hai nguồn trực tiếp tại giường 4: dấu sử dụng thực tế và thẻ bàn giao bỏ dở."
                    : !hasA
                        ? "Một thẻ giường thiếu chữ ký có thể chỉ là giấy sót lại nếu không có dấu vết thực tế cho thấy vị trí giường 4 vừa được dùng."
                        : "Dấu chăn dép và vết bánh giường tự nó chưa cho biết tình trạng bàn giao — cần ghép thêm thẻ bàn giao giường 4 (AS-05B) còn bỏ dở.";
                return HypothesisEvaluation.Reject(
                    HypothesisOutcome.InsufficientEvidence,
                    "Chưa đủ hai nguồn đối chiếu bổ sung",
                    missingMsg,
                    "Gợi ý: Ghép (1) Dấu sử dụng tại vị trí giường 4 với (2) Thẻ bàn giao giường số 4 chưa ký nhận.");
            }

            var claim = GetClaim(submission.claimId);
            if (claim == null)
            {
                return HypothesisEvaluation.Reject(
                    HypothesisOutcome.UnsupportedConclusion,
                    "Chưa chọn kết luận đối chiếu",
                    "Hãy chọn nhận định bám sát chứng cứ để hoàn tất lập luận ghi nhận bất nhất.");
            }

            if (!claim.IsSupportedForRefutation)
            {
                return HypothesisEvaluation.Reject(
                    claim.FailureOutcome,
                    claim.FailureOutcome == HypothesisOutcome.ContradictsDecision
                        ? "Kết luận mâu thuẫn với quyết định"
                        : "Kết luận vượt quá phạm vi chứng cứ",
                    claim.FailureDiagnostic,
                    "Gợi ý: Với chứng cứ hiện có, chỉ có thể kết luận con số 0 trên F101 chưa đủ tin cậy để xác nhận.");
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
