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
        internal readonly string Drawer;

        internal Ch01FieldObject(string id, string title, string place, string zone, Vector3 position, Vector3 size,
            string fact, Reality? layer = null, string cue = null, string image = null, string viewModel = null, string prompt = null,
            string backImage = null, string drawer = null)
        {
            Observation = new ObservationSpec(id, title, title, fact, "", FormatPlaceLabel(place), ObservationCategory.Context,
                layer.HasValue ? new[] { layer.Value } : new[] { Reality.R, Reality.B }, new[] { place },
                new[] { "CH01" }, selectableForF101: false, isDocument: image != null, hasBackSide: backImage != null,
                frontTexture: image, backTexture: backImage);
            Position = position;
            Size = size;
            Zone = zone;
            Cue = cue;
            ViewModel = viewModel;
            Prompt = prompt ?? "Xem " + title.ToLowerInvariant();
            Drawer = drawer;
        }

        private static string FormatPlaceLabel(string place) => place switch
        {
            "LOC01" => "Sân ngoài cổng",
            "LOC02" => "Quầy tiếp nhận",
            "LOC03" => "Phòng 101",
            "HALL" => "Hành lang tầng trệt",
            "STAFF" => "Bàn điều dưỡng",
            _ => place
        };
    }

    internal static class Ch01FieldObjects
    {
        internal const string StaffSeat = "Ghế gỗ bàn điều dưỡng";
        internal const string Calendar = "CH01_CALENDAR";
        internal const string Handset = "CH01_TELEPHONE";
        internal const string Thermos = "CH01_THERMOS";
        internal const string BinPedal = "CH01_PEDAL_BIN";
        internal const string BinInterior = "CH01_BIN_INTERIOR";
        internal static readonly Ch01FieldObject[] All =
        {
            new("CH01_MOTORBIKE", "Xe máy của Minh", "LOC01", "LOC01", new(1.73f, .83f, -22.16f), new(.32f, .46f, .56f),
                "Vệt nước còn chảy từ yên xuống chắn bùn. Hai vết lốp nối từ đường vào đúng chỗ chống xe."),
            new("CH01_GATE_PLAQUE", "Biển cổng An Sinh", "LOC01", "LOC01", new(2.57f, 1.56f, -20.27f), new(.46f, .28f, .12f),
                "Mặt men sáng ở những chỗ tay người từng chạm. Mép biển đã sứt, để lộ gỉ quanh con vít."),
            new("CH01_DEMOLITION", "Thông báo tháo dỡ", "LOC01", "LOC01", new(6.76f, 1.22f, -20.22f), new(1.16f, .74f, .18f),
                "Ca trông coi cuối trước khi bàn giao mặt bằng. Tờ thông báo thành văn bản duy nhất còn mới ở cổng.",
                Reality.R, "paper", prompt: "Đọc bảng thông báo"),
            new("CH01_GUTTER", "Rãnh thoát nước", "LOC01", "LOC01", new(6.25f, .075f, -13.80f), new(.55f, .13f, .80f),
                "Tiếng mưa dồn xuống rãnh cạnh lối đi. Nước đọng lại một lúc giữa hai nắp gang.",
                prompt: "Nghe mưa dưới rãnh"),
            new(Calendar, "Lịch bàn trực", "LOC02", "LOC02", new(-.92f, 1.56f, -5.85f), new(.14f, .469f, .37f),
                "Tờ lịch ngày thứ Sáu, 18/11/2005. Mép tờ hôm trước còn kẹp sau lá lịch.",
                image: "Calendar2005", backImage: "Calendar2005_Previous", prompt: "Giở lịch bàn trực"),
            new(Handset, "Điện thoại bàn", "LOC02", "LOC02", new(1.65f, 1.23f, -3.55f), new(.23f, .18f, .21f),
                "Ống nghe cũ chỉ còn tiếng nhiễu nhẹ. Dây xoắn đã giãn ở đoạn sát tay cầm.",
                viewModel: "Handset", prompt: "Nhấc ống nghe"),
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
            new(Thermos, "Phích nước và cốc men", "LOC03", "LOC03", new(10.48f, 1.01f, 7.10f), new(.37f, .36f, .35f),
                "Cốc có vành men sứt và một vòng cặn trà. Phích cũ để với quai quay ra phía người nằm.",
                viewModel: "CTCC_PROP_ThermosAndMug", prompt: "Cầm phích và cốc lên xem"),
            new("CH01_SLIPPERS", "Dép cạnh giường có bệnh án", "LOC03", "LOC03", new(10.91f, .09f, 6.89f), new(.37f, .17f, .40f),
                "Dép người lớn đặt song song, mũi hướng ra hành lang. Lớp cao su ở gót đã mòn xéo."),
            new(BinPedal, "Thùng men chân đạp", "LOC03", "LOC03", new(6.12f, .38f, 2.38f), new(.28f, .49f, .32f),
                "Men sứt dọc mép nắp. Trên bàn đạp có một vệt sáng ở đúng giữa dấu giày.",
                prompt: "Đạp bàn mở nắp"),
            new(BinInterior, "Lòng thùng men", "LOC03", "LOC03", new(6.12f, .35f, 2.38f), new(.22f, .06f, .22f),
                "Lòng thùng trống. Vành trong còn đường gấp của túi lót.", prompt: "Xem lòng thùng"),
            new("CH01_DRAWER_PENCIL", "Bút chì trong ngăn", "LOC02", "LOC02", Vector3.zero, new(.10f, .06f, .12f),
                "Đầu chì vẹt về một bên, có vết gọt ngắn sát vòng kim loại.", viewModel: "DrawerPencil", drawer: "DRAWER-DESK"),
            new("CH01_DRAWER_GAUZE", "Cuộn gạc", "LOC03", "LOC03", Vector3.zero, new(.10f, .07f, .10f),
                "Một đầu gạc được gấp ngược ra ngoài để dễ cầm.", Reality.B, viewModel: "DrawerGauze", drawer: "DRAWER-BED3"),
            new("CH01_DRAWER_CLIP", "Kẹp giấy", "STAFF", "LOC03", Vector3.zero, new(.08f, .07f, .10f),
                "Hai tai kẹp đã ngả màu. Mặt thép còn vết cọ khi lật chồng giấy.", Reality.B,
                viewModel: "DrawerClip", drawer: "DRAWER-NURSE")
        };
    }
}
