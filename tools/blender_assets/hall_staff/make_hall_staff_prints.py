"""Generate crisp high-resolution fictional Vietnamese 1987-2005 hospital print sheets
for HALL & STAFF Cycles PBR baking (100% fictional An Sinh Hospital, AR03 compliant).

Sheets written to ArtSource/HALL_STAFF/prints/:
  1. pccc_extinguisher_label.png  - Vietnamese CO2/powder fire extinguisher label + pressure dial
  2. pccc_inspect_tag.png         - Yellowed hanging PCCC 2005 inspection tag
  3. hall_floor_map.png           - Ground-floor An Sinh directory & evacuation map (Core 03 canon!)
  4. hall_blade_sign.png          - Hanging corridor double-sided wayfinding blade sign
  5. breaker_schedule.png         - Electrical distribution panel stencil & circuit schedule
  6. staff_binder_spines.png      - Nursing archive binder spines (SỔ GIAO BAN, BỆNH ÁN 101...)
  7. staff_desk_log.png           - Open nursing shift logbook & ward summary desk sheet
  8. staff_cork_backdrop.png      - Aged cork notice board w/ faded ghost outline & small memo slips
  9. staff_bed_board.png          - Khu A ground-floor ward bed occupancy & shift status board
  10. staff_med_labels.png        - Clinical supply jar, sterile drum & medicine cabinet shelf labels
"""
from __future__ import annotations

from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "ArtSource" / "HALL_STAFF" / "prints"
OUT.mkdir(parents=True, exist_ok=True)


def load_font(size: int, bold: bool = False, serif: bool = False):
    if serif:
        candidates = (
            ["C:/Windows/Fonts/timesbd.ttf", "C:/Windows/Fonts/georgiab.ttf"]
            if bold
            else ["C:/Windows/Fonts/times.ttf", "C:/Windows/Fonts/georgia.ttf"]
        )
    else:
        candidates = (
            ["C:/Windows/Fonts/arialbd.ttf", "C:/Windows/Fonts/tahomabd.ttf", "C:/Windows/Fonts/seguisb.ttf"]
            if bold
            else ["C:/Windows/Fonts/arial.ttf", "C:/Windows/Fonts/tahoma.ttf", "C:/Windows/Fonts/segoeui.ttf"]
        )
    for p in candidates:
        if Path(p).exists():
            return ImageFont.truetype(p, size)
    return ImageFont.load_default()


def make_pccc_extinguisher_label():
    """Cylindrical wrap label for Vietnamese hospital PCCC MFZ4 fire extinguisher."""
    W, H = 1024, 512
    img = Image.new("RGB", (W, H), (236, 232, 220))
    d = ImageDraw.Draw(img)

    # Top & bottom red PCCC border bands
    d.rectangle([0, 0, W, 86], fill=(168, 28, 24))
    d.rectangle([0, H - 46, W, H], fill=(168, 28, 24))
    d.rectangle([16, 16, W - 16, H - 16], outline=(42, 42, 40), width=4)

    f_hdr = load_font(42, bold=True)
    f_sub = load_font(26, bold=True)
    f_txt = load_font(22, bold=False)
    f_sm = load_font(19, bold=True)

    d.text((48, 22), "BÌNH CHỮA CHÁY BỘT ABC · MFZ4", fill=(246, 242, 232), font=load_font(38, bold=True))
    d.text((W - 254, 30), "PCCC · AN SINH", fill=(246, 226, 150), font=f_sub)

    # Left column: 4 numbered operating steps with graphic badges
    d.text((48, 108), "HƯỚNG DẪN SỬ DỤNG KHẨN CẤP:", fill=(164, 28, 24), font=f_sub)
    steps = [
        ("1", "Kéo rút chốt hãm chì bảo vệ trên cụm van."),
        ("2", "Hướng loa phun thẳng vào gốc ngọn lửa (1.5 - 2m)."),
        ("3", "Bóp mạnh tay cò van xả bột chữa cháy."),
        ("4", "Quét đều sang hai bên cho đến khi lửa tắt hẳn."),
    ]
    y = 152
    for num, line in steps:
        d.ellipse([48, y, 82, y + 34], fill=(168, 28, 24))
        d.text((58, y + 3), num, fill=(248, 245, 238), font=f_sub)
        d.text((96, y + 5), line, fill=(32, 34, 32), font=f_txt)
        y += 50

    # Right column: technical specs & fire class badges (A, B, C)
    rx = 650
    d.rectangle([rx, 108, W - 36, 420], fill=(244, 240, 228), outline=(72, 74, 70), width=3)
    d.text((rx + 18, 120), "THÔNG SỐ KỸ THUẬT", fill=(30, 34, 32), font=f_sub)
    d.line([(rx + 14, 154), (W - 50, 154)], fill=(120, 122, 118), width=2)
    specs = [
        "Trọng lượng bột:  4.0 kg ± 2%",
        "Áp suất làm việc: 1.2 MPa",
        "Tầm phun xa:      3.0 - 4.0 m",
        "Thời gian phun:   ≥ 13 giây",
        "Vị trí đặt:       Hành lang Khu A",
    ]
    sy = 166
    for sp in specs:
        d.text((rx + 18, sy), sp, fill=(40, 44, 42), font=f_txt)
        sy += 34

    # Class A / B / C square badges
    for i, (cls, col) in enumerate([("A", (44, 118, 66)), ("B", (176, 36, 30)), ("C", (38, 86, 154))]):
        bx = rx + 26 + i * 102
        d.rectangle([bx, 344, bx + 78, 404], fill=col, outline=(28, 30, 28), width=2)
        d.text((bx + 26, 354), cls, fill=(248, 248, 244), font=load_font(36, bold=True))

    d.text((48, H - 38), "KIỂM TRA ĐỊNH KỲ 06 THÁNG / LẦN · BẢO QUẢN NƠI KHÔ RÁO", fill=(245, 240, 228), font=f_sm)
    img.save(OUT / "pccc_extinguisher_label.png")


def make_pccc_inspect_tag():
    """Hanging yellowed inspection card (Tem kiểm định PCCC 2005)."""
    W, H = 512, 768
    img = Image.new("RGB", (W, H), (226, 214, 178))
    d = ImageDraw.Draw(img)
    d.rectangle([18, 18, W - 18, H - 18], outline=(62, 52, 40), width=5)
    d.ellipse([W // 2 - 20, 34, W // 2 + 20, 74], fill=(120, 96, 66), outline=(58, 44, 30), width=4)

    f_hdr = load_font(32, bold=True)
    f_md = load_font(24, bold=True)
    f_sm = load_font(21, bold=False)

    d.text((52, 92), "PHIẾU KIỂM ĐỊNH PCCC", fill=(154, 32, 26), font=f_hdr)
    d.text((76, 134), "BỆNH VIỆN ĐA KHOA AN SINH", fill=(38, 42, 38), font=f_md)
    d.line([(40, 174), (W - 40, 174)], fill=(70, 62, 50), width=3)

    d.text((46, 192), "Mã thiết bị:  PCCC-KA-02", fill=(36, 38, 36), font=f_md)
    d.text((46, 228), "Vị trí:       Hành lang P.101", fill=(36, 38, 36), font=f_md)

    # Inspection date table
    ty = 280
    d.rectangle([40, ty, W - 40, ty + 46], fill=(198, 186, 152), outline=(66, 58, 46), width=2)
    d.text((56, ty + 10), "NGÀY KT", fill=(34, 34, 30), font=f_md)
    d.text((210, ty + 10), "TÌNH TRẠNG", fill=(34, 34, 30), font=f_md)
    d.text((380, ty + 10), "KÝ NHẬN", fill=(34, 34, 30), font=f_md)
    rows = [
        ("14/05/2004", "Đạt áp suất", "Bảy"),
        ("12/11/2004", "Đạt · Nạp đủ", "Bảy"),
        ("16/05/2005", "Đạt áp suất", "Bảy"),
        ("10/11/2005", "Đạt · Sẵn sàng", "T. Bảy"),
        ("___/___/2006", "(Chưa kiểm)", ""),
    ]
    ty += 46
    for dt, st, sig in rows:
        d.rectangle([40, ty, W - 40, ty + 58], outline=(82, 74, 60), width=2)
        d.line([(196, ty), (196, ty + 58)], fill=(82, 74, 60), width=2)
        d.line([(366, ty), (366, ty + 58)], fill=(82, 74, 60), width=2)
        d.text((52, ty + 16), dt, fill=(38, 44, 72) if "Chưa" not in st else (110, 102, 88), font=f_sm)
        d.text((208, ty + 16), st, fill=(36, 62, 46) if "Đạt" in st else (110, 102, 88), font=f_sm)
        d.text((382, ty + 16), sig, fill=(36, 48, 108), font=f_md)
        ty += 58

    # Red round inspection stamp
    d.ellipse([290, 620, 448, 726], outline=(168, 42, 36), width=4)
    d.text((314, 648), "ĐÃ KIỂM", fill=(168, 42, 36), font=f_md)
    d.text((322, 680), "11/2005", fill=(168, 42, 36), font=f_sm)
    img.save(OUT / "pccc_inspect_tag.png")


def make_hall_floor_map():
    """Authentic 1987-2005 An Sinh Hospital Ground-Floor Directory & Fire Evacuation Diagram
    matching exact Core 03 topology (Sảnh tiếp nhận, Hành lang chính, Lối nhân viên, Phòng 101, Cấp cứu)."""
    W, H = 1024, 768
    img = Image.new("RGB", (W, H), (236, 232, 218))
    d = ImageDraw.Draw(img)

    # Header bar (deep Vietnamese institutional hospital green)
    d.rectangle([0, 0, W, 112], fill=(24, 78, 60))
    d.rectangle([14, 14, W - 14, H - 14], outline=(24, 78, 60), width=5)

    f_title = load_font(30, bold=True)
    f_sub = load_font(21, bold=True)
    f_room = load_font(20, bold=True)
    f_sm = load_font(16, bold=False)

    d.text((38, 22), "SƠ ĐỒ TẦNG TRỆT · KHU A — BỆNH VIỆN ĐA KHOA AN SINH", fill=(245, 244, 236), font=f_title)
    d.text((38, 68), "CHỈ DẪN KHOA PHÒNG & LỐI THOÁT HIỂM KHẨN CẤP (CẬP NHẬT 2005)", fill=(208, 232, 218), font=f_sub)

    # Architectural floor plan box (left 65% of canvas)
    mx0, my0, mx1, my1 = 38, 138, 678, 696
    d.rectangle([mx0, my0, mx1, my1], fill=(246, 244, 235), outline=(42, 54, 48), width=4)

    # Top (North): KHOA CẤP CỨU (LOC04)
    d.rectangle([62, 162, 654, 268], fill=(244, 216, 212), outline=(140, 38, 34), width=3)
    d.text((220, 198), "KHOA CẤP CỨU (LỐI A & LỐI B)", fill=(140, 28, 24), font=f_sub)

    # Left vertical corridor: LỐI NHÂN VIÊN & BÀN ĐIỀU DƯỠNG (STAFF)
    d.rectangle([62, 268, 174, 558], fill=(214, 230, 222), outline=(36, 64, 52), width=3)
    d.text((104, 318), "LỐI", fill=(28, 56, 46), font=load_font(18, bold=True))
    d.text((104, 342), "NHÂN", fill=(28, 56, 46), font=load_font(18, bold=True))
    d.text((104, 366), "VIÊN", fill=(28, 56, 46), font=load_font(18, bold=True))
    # Nursing desk marker
    d.rectangle([144, 434, 172, 512], fill=(154, 114, 72), outline=(60, 44, 28), width=2)
    d.text((68, 466), "Đ.Dưỡng", fill=(64, 46, 28), font=load_font(15, bold=True))

    # Middle pier + Staff cross-door opening
    d.rectangle([174, 268, 218, 388], fill=(182, 186, 178), outline=(48, 54, 50), width=2)
    d.rectangle([174, 432, 218, 558], fill=(182, 186, 178), outline=(48, 54, 50), width=2)
    d.rectangle([174, 388, 218, 432], fill=(238, 234, 214), outline=(48, 54, 50), width=2)

    # Main Corridor (HALL)
    d.rectangle([218, 268, 344, 558], fill=(232, 238, 228), outline=(36, 64, 52), width=3)
    d.text((226, 362), "HÀNH", fill=(28, 56, 46), font=load_font(18, bold=True))
    d.text((226, 388), "LANG", fill=(28, 56, 46), font=load_font(18, bold=True))
    d.text((226, 414), "CHÍNH", fill=(28, 56, 46), font=load_font(18, bold=True))

    # Green dashed evacuation arrows positioned cleanly clear of room labels!
    for y_arr in range(536, 290, -52):
        d.line([(312, y_arr), (312, y_arr - 22)], fill=(26, 134, 76), width=5)
        d.polygon([(312, y_arr - 30), (303, y_arr - 16), (321, y_arr - 16)], fill=(26, 134, 76))
    for y_arr in range(536, 290, -56):
        d.line([(84, y_arr), (84, y_arr - 22)], fill=(26, 134, 76), width=4)
        d.polygon([(84, y_arr - 30), (76, y_arr - 18), (92, y_arr - 18)], fill=(26, 134, 76))

    # Inner Light Court (SÂN SÁNG) north of Room 101
    d.rectangle([344, 268, 654, 404], fill=(212, 232, 206), outline=(48, 86, 56), width=3)
    d.text((422, 326), "SÂN SÁNG NỘI BỘ", fill=(36, 82, 46), font=f_room)

    # Room 101 (PHÒNG 101) & Storage Wing (KHO)
    d.rectangle([344, 404, 580, 558], fill=(242, 236, 214), outline=(48, 58, 52), width=3)
    d.text((384, 456), "PHÒNG BỆNH 101", fill=(32, 46, 40), font=f_room)
    d.text((402, 488), "(Nội tổng hợp)", fill=(64, 76, 68), font=f_sm)
    d.rectangle([580, 404, 654, 558], fill=(214, 210, 198), outline=(56, 60, 56), width=3)
    d.text((594, 468), "KHO", fill=(54, 54, 50), font=f_room)

    # South: SẢNH TIẾP NHẬN (LOC02)
    d.rectangle([62, 558, 654, 674], fill=(224, 234, 224), outline=(36, 64, 52), width=3)
    d.text((220, 604), "SẢNH TIẾP NHẬN & QUẦY TRỰC", fill=(24, 58, 46), font=f_sub)
    d.rectangle([86, 584, 188, 650], fill=(154, 114, 72), outline=(60, 44, 28), width=2)
    d.text((104, 608), "QUẦY", fill=(248, 244, 234), font=f_sm)

    # "YOU ARE HERE" red dot on East wall of north HALL
    d.ellipse([330, 336, 358, 364], fill=(196, 34, 28), outline=(255, 250, 240), width=3)
    d.text((228, 294), "BẠN Ở ĐÂY", fill=(182, 26, 22), font=load_font(16, bold=True))

    # Fire extinguisher icon on West wall of HALL
    d.rectangle([220, 466, 238, 492], fill=(186, 30, 26))

    # Right legend column (wider so zero right-edge touch!)
    lx = 698
    d.rectangle([lx, 138, W - 30, 696], fill=(242, 239, 228), outline=(54, 68, 60), width=3)
    d.text((lx + 20, 158), "CHÚ GIẢI KÝ HIỆU", fill=(24, 72, 56), font=f_sub)
    d.line([(lx + 16, 194), (W - 46, 194)], fill=(96, 112, 102), width=2)

    legends = [
        ((196, 34, 28), "circle", "Vị trí hiện tại (Hành lang)"),
        ((26, 134, 76), "arrow", "Hướng thoát hiểm ưu tiên"),
        ((186, 30, 26), "box", "Bình chữa cháy PCCC"),
        ((140, 38, 34), "box", "Cửa Cấp Cứu (Lối A & B)"),
        ((154, 114, 72), "box", "Quầy trực / Bàn điều dưỡng"),
    ]
    ly = 220
    for col, shape, lbl in legends:
        if shape == "circle":
            d.ellipse([lx + 26, ly + 2, lx + 50, ly + 26], fill=col)
        elif shape == "arrow":
            d.line([(lx + 24, ly + 14), (lx + 52, ly + 14)], fill=col, width=5)
        else:
            d.rectangle([lx + 26, ly + 4, lx + 50, ly + 24], fill=col)
        d.text((lx + 62, ly + 2), lbl, fill=(36, 44, 40), font=f_sm)
        ly += 52

    d.line([(lx + 18, 500), (W - 54, 500)], fill=(96, 112, 102), width=2)
    d.text((lx + 22, 518), "QUY ĐỊNH LƯU THÔNG:", fill=(154, 34, 28), font=f_room)
    rules = [
        "• Giữ thông thoáng lối cáng",
        "  rộng tối thiểu 1.7m - 3.0m.",
        "• Không chắn cửa Cấp Cứu",
        "  và cửa thông nhân viên.",
        "• Gọi số nội bộ 100 khi có",
        "  sự cố điện hoặc hỏa hoạn.",
    ]
    ry = 552
    for rl in rules:
        d.text((lx + 22, ry), rl, fill=(42, 48, 44), font=f_sm)
        ry += 24

    d.text((44, H - 56), "BAN QUẢN LÝ CƠ SỞ VẬT CHẤT · BỆNH VIỆN ĐA KHOA AN SINH · MÃ BẢNG: AS-SD-T1", fill=(48, 64, 56), font=f_sm)
    img.save(OUT / "hall_floor_map.png")


def make_hall_blade_sign():
    """Hanging double-sided enameled corridor wayfinding sign."""
    W, H = 1024, 384
    img = Image.new("RGB", (W, H), (22, 82, 62))
    d = ImageDraw.Draw(img)
    d.rectangle([14, 14, W - 14, H - 14], outline=(238, 240, 230), width=6)
    d.line([(40, H // 2), (W - 40, H // 2)], fill=(190, 218, 204), width=3)

    f_big = load_font(54, bold=True)
    f_sub = load_font(34, bold=True)

    # Top row: CẤP CỨU ↑
    d.rectangle([40, 38, 128, 160], fill=(178, 34, 28))
    d.text((64, 62), "↑", fill=(250, 248, 242), font=load_font(72, bold=True))
    d.text((158, 52), "KHOA CẤP CỨU", fill=(252, 248, 238), font=f_big)
    d.text((158, 118), "EMERGENCY · LỐI A", fill=(216, 236, 224), font=f_sub)

    # Bottom row: PHÒNG 101 · NỘI TRÚ | LỐI NHÂN VIÊN
    d.text((48, 218), "← LỐI NHÂN VIÊN", fill=(238, 242, 232), font=load_font(40, bold=True))
    d.text((545, 218), "PHÒNG 101 →", fill=(246, 228, 152), font=load_font(42, bold=True))
    d.text((48, 286), "BÀN ĐIỀU DƯỠNG · LỐI B", fill=(198, 222, 210), font=load_font(26, bold=True))
    d.text((545, 286), "KHOA NỘI TỔNG HỢP", fill=(214, 232, 220), font=load_font(26, bold=True))

    img.save(OUT / "hall_blade_sign.png")

    # Opposite (+Y / North-facing) side seen when walking SOUTH from Emergency toward Reception!
    img_s = Image.new("RGB", (W, H), (22, 82, 62))
    ds = ImageDraw.Draw(img_s)
    ds.rectangle([14, 14, W - 14, H - 14], outline=(238, 240, 230), width=6)
    ds.line([(40, H // 2), (W - 40, H // 2)], fill=(190, 218, 204), width=3)

    # Top row: SẢNH TIẾP NHẬN ↑ (straight ahead south!)
    ds.rectangle([40, 38, 128, 160], fill=(38, 118, 86))
    ds.text((64, 62), "↑", fill=(250, 248, 242), font=load_font(72, bold=True))
    ds.text((158, 52), "SẢNH TIẾP NHẬN", fill=(252, 248, 238), font=f_big)
    ds.text((158, 118), "QUẦY TRỰC · LỐI RA CỔNG", fill=(216, 236, 224), font=f_sub)

    # Bottom row (when facing SOUTH: East / Room 101 is LEFT, West / Staff is RIGHT!)
    ds.text((48, 218), "← PHÒNG 101", fill=(246, 228, 152), font=load_font(42, bold=True))
    ds.text((518, 218), "LỐI NHÂN VIÊN →", fill=(238, 242, 232), font=load_font(40, bold=True))
    ds.text((48, 286), "KHOA NỘI TỔNG HỢP", fill=(214, 232, 220), font=load_font(26, bold=True))
    ds.text((518, 286), "BÀN ĐIỀU DƯỠNG · LỐI B", fill=(198, 222, 210), font=load_font(26, bold=True))

    img_s.save(OUT / "hall_blade_sign_south.png")


def make_breaker_schedule():
    """Electrical distribution cabinet door stencil (TỦ ĐIỆN TẦNG TRỆT · KHU A)."""
    W, H = 512, 768
    img = Image.new("RGB", (W, H), (118, 128, 122))
    d = ImageDraw.Draw(img)
    d.rectangle([20, 20, W - 20, H - 20], outline=(62, 70, 66), width=4)

    # Classic yellow/black Vietnamese high-voltage danger triangle
    tri = [(W // 2, 72), (W // 2 - 96, 236), (W // 2 + 96, 236)]
    d.polygon(tri, fill=(228, 186, 36), outline=(28, 28, 26))
    d.line([tri[0], tri[1], tri[2], tri[0]], fill=(28, 28, 26), width=8)
    # Red lightning bolt inside triangle
    bolt = [(264, 116), (232, 174), (258, 174), (244, 222), (288, 160), (262, 160)]
    d.polygon(bolt, fill=(184, 30, 24))

    f_hdr = load_font(34, bold=True)
    f_md = load_font(24, bold=True)
    f_sm = load_font(20, bold=False)

    d.rectangle([42, 262, W - 42, 318], fill=(176, 34, 28))
    d.text((78, 272), "NGUY HIỂM · ĐIỆN ÁP 220V", fill=(248, 244, 234), font=load_font(28, bold=True))

    d.text((64, 346), "TỦ ĐIỆN TẦNG TRỆT", fill=(28, 34, 32), font=f_hdr)
    d.text((112, 392), "KHU A · MẠCH CHIẾU SÁNG", fill=(36, 44, 40), font=f_md)

    # Pasted paper circuit legend card on lower door
    d.rectangle([48, 448, W - 48, 704], fill=(226, 220, 202), outline=(56, 62, 58), width=3)
    d.text((68, 464), "SƠ ĐỒ ÁT-TÔ-MÁT (CB):", fill=(32, 42, 38), font=f_md)
    circuits = [
        "CB1:  Sảnh tiếp nhận & Quầy trực",
        "CB2:  Hành lang chính (Đèn T8)",
        "CB3:  Lối nhân viên & Bàn ĐD",
        "CB4:  Phòng bệnh 101 (Đèn & Quạt)",
        "CB5:  Đèn báo Cấp Cứu (Dự phòng)",
        "CB6:  Hệ thống loa hành lang PA",
    ]
    cy = 504
    for c in circuits:
        d.text((66, cy), c, fill=(40, 46, 42), font=f_sm)
        cy += 31

    img.save(OUT / "breaker_schedule.png")


def make_staff_binder_spines():
    """Vertical archival spine labels for nursing workstation hutch binders (UV sliced in 4 columns)."""
    W, H = 1024, 1024
    img = Image.new("RGB", (W, H), (220, 212, 192))
    d = ImageDraw.Draw(img)

    cols = [
        ((38, 66, 98),  (234, 228, 210), "SỔ GIAO BAN", "CA ĐÊM · 2005", "AS-01"),
        ((122, 92, 58), (240, 232, 210), "BỆNH ÁN 101", "KHOA NỘI",      "P.101"),
        ((46, 86, 66),  (236, 234, 216), "Y LỆNH THUỐC", "TẦNG TRỆT",    "AS-04"),
        ((134, 46, 42), (242, 234, 214), "CHUYỂN KHOA", "LƯU HÀNH CHÍNH", "AS-09"),
    ]
    cw = W // 4
    f_big = load_font(36, bold=True)
    f_sm = load_font(24, bold=True)
    for i, (bg, paper, t1, t2, code) in enumerate(cols):
        x0, x1 = i * cw, (i + 1) * cw
        d.rectangle([x0, 0, x1, H], fill=bg)
        d.rectangle([x0 + 18, 120, x1 - 18, 430], fill=paper, outline=(46, 42, 36), width=4)
        # Rotate text vertically? Horizontal compact title reads cleaner at FPS distance!
        d.text((x0 + 30, 154), t1, fill=(28, 34, 32), font=load_font(28, bold=True))
        d.line([(x0 + 28, 210), (x1 - 28, 210)], fill=(140, 44, 38), width=3)
        d.text((x0 + 32, 232), t2, fill=(52, 56, 52), font=f_sm)
        d.text((x0 + 52, 310), "AN SINH", fill=(42, 68, 56), font=f_sm)
        # Brass-rimmed finger pull hole at bottom + index box
        d.rectangle([x0 + 38, 620, x1 - 38, 720], fill=paper, outline=(48, 44, 38), width=3)
        d.text((x0 + 68, 648), code, fill=(32, 34, 32), font=f_big)
        d.ellipse([x0 + cw // 2 - 26, 810, x0 + cw // 2 + 26, 862], fill=(28, 26, 24), outline=(188, 162, 98), width=6)

    img.save(OUT / "staff_binder_spines.png")


def make_staff_desk_log():
    """Open two-page Vietnamese hospital nursing shift logbook on the STAFF desk."""
    W, H = 1024, 768
    img = Image.new("RGB", (W, H), (236, 228, 206))
    d = ImageDraw.Draw(img)

    # Center gutter shadow
    for dx in range(-22, 23):
        shade = int(236 - 68 * (1.0 - abs(dx) / 23.0))
        d.line([(W // 2 + dx, 0), (W // 2 + dx, H)], fill=(shade, shade - 8, shade - 22), width=1)

    f_hdr = load_font(26, bold=True)
    f_md = load_font(20, bold=True)
    f_sm = load_font(19, bold=False)

    # Left page: Shift handover summary
    d.rectangle([28, 28, W // 2 - 26, H - 28], outline=(62, 72, 66), width=3)
    d.text((52, 46), "SỔ GIAO BAN ĐIỀU DƯỠNG", fill=(28, 48, 40), font=f_hdr)
    d.text((52, 82), "KHU A · TẦNG TRỆT (17/11/2005)", fill=(144, 38, 32), font=f_md)
    for r in range(12):
        y = 132 + r * 48
        d.line([(44, y + 34), (W // 2 - 42, y + 34)], fill=(168, 174, 166), width=2)
    notes_l = [
        "19:00  Nhận ca trực từ ĐD. Hương.",
        "19:30  Phát thuốc tối P.101 (G.01-G.03).",
        "20:15  Kiểm tra đường truyền G.01: ổn định.",
        "21:00  Đo mạch nhiệt độ theo bảng đầu giường.",
        "22:30  Bổ sung nước sôi & gạc tủ hành lang.",
        "23:10  Cửa thông nhân viên chốt đúng quy chế.",
        "00:05  Kiểm tra hồ sơ kê tạm đầu hành lang.",
        "01:30  Trực quầy tiếp nhận báo chuyển phiếu.",
    ]
    for i, line in enumerate(notes_l):
        d.text((50, 136 + i * 48), line, fill=(38, 52, 108), font=f_sm)

    # Right page: Ward room status table
    rx = W // 2 + 28
    d.rectangle([rx, 28, W - 28, H - 28], outline=(62, 72, 66), width=3)
    d.text((rx + 22, 46), "THEO DÕI BUỒNG BỆNH", fill=(28, 48, 40), font=f_hdr)
    d.text((rx + 22, 82), "CA ĐÊM · ĐD. TRẦN THỊ LAN", fill=(48, 62, 56), font=f_md)
    for r in range(12):
        y = 132 + r * 48
        d.line([(rx + 16, y + 34), (W - 44, y + 34)], fill=(168, 174, 166), width=2)
    notes_r = [
        "P.101 - G.01:  Nội trú · Truyền dịch",
        "P.101 - G.02:  Đã hoàn tất chuyển khoa",
        "P.101 - G.03:  Uống thuốc định kỳ",
        "P.101 - Phiếu: Chờ đối chiếu hồ sơ F101",
        "Hành lang:     Đèn T8 số 1, 2 hoạt động",
        "Cửa Cấp Cứu:   Lối A, Lối B thông thoáng",
        "Ghi chú:       Giữ nguyên ngăn hồ sơ trực",
    ]
    for i, line in enumerate(notes_r):
        d.text((rx + 20, 136 + i * 48), line, fill=(40, 52, 98), font=f_sm)

    # Red round nurse station stamp
    d.ellipse([W - 208, H - 172, W - 64, H - 64], outline=(168, 44, 38), width=4)
    d.text((W - 188, H - 138), "TRỰC ĐÊM", fill=(168, 44, 38), font=f_md)
    d.text((W - 180, H - 108), "AN SINH", fill=(168, 44, 38), font=f_sm)
    img.save(OUT / "staff_desk_log.png")


def make_staff_cork_backdrop():
    """Cork notice board texture behind Doc_WardOverflow: warm cork texture with small pinned
    secondary memos on left/right margins and a pale ghost rectangle in center for R authenticity."""
    W, H = 768, 768
    img = Image.new("RGB", (W, H), (144, 108, 68))
    d = ImageDraw.Draw(img)

    # Subtle cork speckles
    import random
    rng = random.Random(1987)
    for _ in range(9000):
        x = rng.randint(0, W - 1)
        y = rng.randint(0, H - 1)
        delta = rng.randint(-26, 22)
        c = (max(0, min(255, 144 + delta)), max(0, min(255, 108 + delta)), max(0, min(255, 68 + delta)))
        d.point((x, y), fill=c)

    # Slightly lighter sun-protected ghost footprint in center (where Doc_WardOverflow hangs in B!)
    d.rectangle([168, 102, 600, 666], fill=(162, 126, 84), outline=(126, 92, 56), width=2)
    # Torn corner paper stub left pinned at top-right of center footprint
    d.polygon([(568, 108), (598, 108), (598, 146), (576, 132)], fill=(218, 208, 182))

    # Small permanent phone-extension slip pinned at upper-left margin
    d.rectangle([28, 56, 152, 242], fill=(224, 218, 196), outline=(92, 86, 74), width=2)
    f_sm = load_font(16, bold=True)
    f_xs = load_font(14, bold=False)
    d.text((38, 72), "SỐ NỘI BỘ", fill=(154, 36, 30), font=f_sm)
    for i, s in enumerate(["Quầy:  101", "Cấp cứu: 105", "Trực BS: 108", "Kho:   112", "Bảo vệ: 100"]):
        d.text((38, 104 + i * 24), s, fill=(38, 42, 40), font=f_xs)
    d.ellipse([82, 60, 98, 76], fill=(186, 42, 34))

    # Small emergency shift reminder card on right margin
    d.rectangle([618, 470, 742, 660], fill=(216, 226, 208), outline=(74, 88, 76), width=2)
    d.text((628, 486), "LỊCH TRỰC", fill=(28, 68, 52), font=f_sm)
    for i, s in enumerate(["T2: Hương", "T4: Mai", "T6: Lan", "T7: Thảo", "CN: Lan"]):
        d.text((628, 518 + i * 24), s, fill=(36, 44, 40), font=f_xs)
    d.ellipse([672, 474, 688, 490], fill=(42, 92, 154))

    img.save(OUT / "staff_cork_backdrop.png")


def make_staff_bed_board():
    """Enameled Vietnamese Ward Bed Occupancy & Shift Status Board for STAFF west wall.
    Calibrated matte hospital enamel tone so table grid stays 100% legible under Layer-B T8 lighting!"""
    W, H = 1024, 768
    img = Image.new("RGB", (W, H), (192, 198, 186))
    d = ImageDraw.Draw(img)

    d.rectangle([0, 0, W, 104], fill=(22, 74, 56))
    d.rectangle([12, 12, W - 12, H - 12], outline=(28, 66, 52), width=5)

    f_hdr = load_font(33, bold=True)
    f_sub = load_font(20, bold=True)
    f_tbl = load_font(19, bold=True)

    d.text((40, 18), "BẢNG THEO DÕI GIƯỜNG BỆNH · KHU A — TẦNG TRỆT", fill=(232, 234, 224), font=f_hdr)
    d.text((40, 64), "KHOA NỘI TỔNG HỢP · CẬP NHẬT CA TRỰC ĐÊM 18/11/2005", fill=(188, 216, 200), font=f_sub)

    # Table grid (generous right column so zero clipping, 100% CH01 canon-pure — zero CH02 store spoilers!)
    tx0, ty0, tx1, ty1 = 36, 128, W - 36, 650
    cols_x = [tx0, 182, 318, 504, 706, tx1]
    headers = ["BUỒNG", "CHỈ TIÊU", "HIỆN DIỆN", "Y TÁ TRỰC", "GHI CHÚ CA"]
    d.rectangle([tx0, ty0, tx1, ty0 + 54], fill=(164, 182, 170), outline=(30, 48, 40), width=3)
    for i, h in enumerate(headers):
        d.text((cols_x[i] + 12, ty0 + 16), h, fill=(18, 38, 30), font=f_sub)
        if i > 0:
            d.line([(cols_x[i], ty0), (cols_x[i], ty1)], fill=(44, 60, 52), width=3)

    rows = [
        ("PHÒNG 101",  "03 giường", "02 · 01 chuyển", "ĐD. T. T. Lan", "Đối chiếu phiếu F101"),
        ("PHÒNG 102",  "04 giường", "04 / 04",        "ĐD. N. Hương", "Ổn định · Đủ hồ sơ"),
        ("PHÒNG 103",  "04 giường", "03 / 04",        "ĐD. P. Mai",   "01 xuất viện sáng"),
        ("PHÒNG 104",  "04 giường", "02 / 04",        "ĐD. L. Thảo",  "Truyền dịch định kỳ"),
        ("TRẠM TRỰC",  "Nội bộ",    "Trực hành lang", "Điều dưỡng",    "Bàn giao sổ đúng giờ"),
        ("CẤP CỨU",    "02 cáng",   "Trực 24/24",     "Tổ trực đêm",   "Giữ thông lối A & B"),
    ]
    ry = ty0 + 54
    row_h = (ty1 - ry) // len(rows)
    for idx, rdata in enumerate(rows):
        bg = (204, 209, 198) if idx % 2 == 0 else (188, 195, 184)
        d.rectangle([tx0, ry, tx1, ry + row_h], fill=bg, outline=(56, 70, 62), width=2)
        for ci, val in enumerate(rdata):
            col = (142, 24, 20) if "F101" in val else (22, 34, 48)
            d.text((cols_x[ci] + 12, ry + 24), val, fill=col, font=f_tbl)
            if ci > 0:
                d.line([(cols_x[ci], ry), (cols_x[ci], ry + row_h)], fill=(56, 70, 62), width=2)
        ry += row_h

    d.text((40, 676), "LƯU Ý: Mọi thay đổi số giường kê tạm phải lập phiếu bổ sung có chữ ký điều dưỡng trực.", fill=(28, 42, 36), font=f_sub)
    d.text((40, 712), "BỆNH VIỆN ĐA KHOA AN SINH · KHU A", fill=(52, 66, 58), font=load_font(19, bold=True))
    img.save(OUT / "staff_bed_board.png")


def make_staff_med_labels():
    """Label atlas for North STAFF wall-mounted medical cabinet bottles & supply boxes."""
    W, H = 512, 512
    img = Image.new("RGB", (W, H), (234, 229, 214))
    d = ImageDraw.Draw(img)
    labels = [
        ((0, 0, 256, 256),     (168, 36, 30), "CỒN Y TẾ 70°",    "SÁT KHUẨN NGOÀI DA"),
        ((256, 0, 512, 256),   (32, 92, 146), "NaCl 0.9%",       "DUNG DỊCH RỬA VẾT THƯƠNG"),
        ((0, 256, 256, 512),   (34, 106, 68), "BÔNG GẠC VÔ TRÙNG", "HẤP TIỆT TRÙNG · KHU A"),
        ((256, 256, 512, 512), (146, 98, 32), "POVIDONE 10%",    "DÙNG TẠI TRẠM ĐIỀU DƯỠNG"),
    ]
    f_hdr = load_font(22, bold=True)
    f_sm = load_font(15, bold=True)
    for (x0, y0, x1, y1), col, t1, t2 in labels:
        d.rectangle([x0 + 10, y0 + 10, x1 - 10, y1 - 10], fill=(242, 238, 224), outline=col, width=5)
        d.rectangle([x0 + 10, y0 + 10, x1 - 10, y0 + 68], fill=col)
        d.text((x0 + 22, y0 + 24), t1, fill=(248, 246, 238), font=f_hdr)
        d.text((x0 + 22, y0 + 106), t2, fill=(36, 42, 38), font=f_sm)
        d.text((x0 + 22, y0 + 168), "BV ĐA KHOA AN SINH", fill=(72, 80, 76), font=f_sm)
    img.save(OUT / "staff_med_labels.png")


def run_all():
    make_pccc_extinguisher_label()
    make_pccc_inspect_tag()
    make_hall_floor_map()
    make_hall_blade_sign()
    make_breaker_schedule()
    make_staff_binder_spines()
    make_staff_desk_log()
    make_staff_cork_backdrop()
    make_staff_bed_board()
    make_staff_med_labels()
    print(f"Generated 10 HALL & STAFF high-res Vietnamese print sheets -> {OUT}")


if __name__ == "__main__":
    run_all()
