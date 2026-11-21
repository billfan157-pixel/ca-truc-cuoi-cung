using UnityEngine;

namespace CTCC
{
    internal sealed class Ch01FieldObject
    {
        internal readonly ObservationSpec Observation;
        internal readonly Vector3 Position;
        internal readonly Vector3 Size;
        internal readonly string Zone;
        internal readonly string Cue;
        internal readonly string ViewModel;
        internal readonly string Prompt;

        internal Ch01FieldObject(string id, string title, string place, string zone, Vector3 position, Vector3 size,
            string fact, Reality? layer = null, string cue = null, string image = null, string viewModel = null, string prompt = null)
        {
            Observation = new ObservationSpec(id, title, title, fact, "", place, ObservationCategory.Context,
                layer.HasValue ? new[] { layer.Value } : new[] { Reality.R, Reality.B }, new[] { place },
                new[] { "CH01" }, selectableForF101: false, isDocument: image != null, hasBackSide: false, frontTexture: image);
            Position = position;
            Size = size;
            Zone = zone;
            Cue = cue;
            ViewModel = viewModel;
            Prompt = prompt ?? "Xem " + title.ToLowerInvariant();
        }
    }

    internal static class Ch01FieldObjects
    {
        internal const string StaffSeat = "Ghế gỗ bàn điều dưỡng";
        internal static readonly Ch01FieldObject[] All =
        {
            new("CH01_MOTORBIKE", "Xe máy của Minh", "LOC01", "LOC01", new(1.73f, .83f, -22.16f), new(.32f, .46f, .56f),
                "Vệt nước còn chảy từ yên xuống chắn bùn. Hai vết lốp nối từ đường vào đúng chỗ chống xe."),
            new("CH01_GATE_PLAQUE", "Biển men An Sinh", "LOC01", "LOC01", new(2.58f, 1.50f, -20.23f), new(.58f, .32f, .10f),
                "Mặt men sáng ở những chỗ tay người từng chạm. Mép biển đã sứt, để lộ gỉ quanh con vít."),
            new("CH01_DEMOLITION", "Thông báo tháo dỡ", "LOC01", "LOC01", new(6.76f, 1.22f, -20.22f), new(1.16f, .74f, .18f),
                "Ca trông coi cuối trước khi bàn giao mặt bằng. Tờ thông báo thành văn bản duy nhất còn mới ở cổng.",
                Reality.R, "paper", prompt: "Đọc bảng thông báo"),
            new("CH01_GUTTER", "Rãnh thoát nước", "LOC01", "LOC01", new(6.25f, .075f, -13.80f), new(.55f, .13f, .80f),
                "Tiếng mưa dồn xuống rãnh cạnh lối đi. Nước đọng lại một lúc giữa hai nắp gang.",
                prompt: "Nghe mưa dưới rãnh"),
            new("CH01_CALENDAR", "Lịch bàn trực", "LOC02", "LOC02", new(-.92f, 1.56f, -4.65f), new(.14f, .469f, .37f),
                "Tờ lịch dừng ở tháng 11 năm 2005. Có những nét gạch bút bi ở nửa sau trang.",
                image: "Calendar2005", prompt: "Đọc tờ lịch"),
            new("CH01_TELEPHONE", "Điện thoại bàn", "LOC02", "LOC02", new(1.65f, 1.23f, -3.55f), new(.23f, .18f, .21f),
                "Ống nghe cũ chỉ còn tiếng nhiễu nhẹ. Dây xoắn đã giãn ở đoạn sát tay cầm.",
                cue: "paStatic", prompt: "Nghe điện thoại bàn"),
            new("CH01_WAITING_SEAT", "Ghế chờ sảnh", "LOC02", "LOC02", new(7.01f, .56f, -5.12f), new(.55f, .40f, 1.04f),
                "Ở mép ngồi, gỗ bóng lên vì chạm tay. Dưới gầm có vệt hằn do đế ghế bị kéo nhiều lần."),
            new("CH01_EXTINGUISHER", "Bình chữa cháy hành lang", "HALL", "LOC03", new(2.66f, 1.18f, 5.56f), new(.23f, .50f, .30f),
                "Tem kiểm tra đã ố. Phần kim loại bên dưới đai treo sáng hơn mặt bình."),
            new("CH01_PA_BOX", "Loa hành lang", "HALL", "LOC03", new(5.46f, 2.56f, 3.35f), new(.22f, .31f, .37f),
                "Màng loa rung rất nhẹ giữa những khoảng im. Từ đây không thể thấy phòng phát.",
                Reality.B, "paStatic", prompt: "Nghe sát loa"),
            new("CH01_NURSE_DESK", "Mặt bàn điều dưỡng", "STAFF", "LOC03", new(.84f, .98f, 3.34f), new(.23f, .10f, .35f),
                "Một góc bàn mòn thành dải, vừa tầm người tì cổ tay để ghi. Giấy được xếp dồn bên kia cho khỏi vướng."),
            new("CH01_BASIN", "Chậu và tấm lót", "LOC03", "LOC03", new(7.90f, .15f, 3.11f), new(.42f, .25f, .45f),
                "Tấm lót kê sát chậu. Một nửa đã sẫm nước, nửa kia vẫn lộ đường đan."),
            new("CH01_THERMOS", "Phích nước và cốc men", "LOC03", "LOC03", new(10.48f, 1.01f, 7.10f), new(.37f, .36f, .35f),
                "Cốc có vành men sứt và một vòng cặn trà. Phích cũ để với quai quay ra phía người nằm.",
                viewModel: "CTCC_PROP_ThermosAndMug", prompt: "Cầm phích và cốc lên xem"),
            new("CH01_SLIPPERS", "Dép cạnh giường có bệnh án", "LOC03", "LOC03", new(10.91f, .09f, 6.89f), new(.37f, .17f, .40f),
                "Dép người lớn đặt song song, mũi hướng ra hành lang. Lớp cao su ở gót đã mòn xéo."),
            new("CH01_PEDAL_BIN", "Thùng men chân đạp", "LOC03", "LOC03", new(6.12f, .38f, 2.38f), new(.28f, .49f, .32f),
                "Men sứt dọc mép nắp. Trên bàn đạp có một vệt sáng ở đúng giữa dấu giày.")
        };
    }
}
