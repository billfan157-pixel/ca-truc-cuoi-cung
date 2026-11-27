"""Generate crisp high-resolution fictional Vietnamese 1987-2005 hospital ward print sheets
for LOC03 (Room 101 & Store Room) Cycles PBR baking.

All identifiers, forms, and medical labels are 100% fictional An Sinh Hospital assets (zero real brands per AR03
and zero canon conflict with Core v3.6.2 / DEC-V36-CH01-01 / DEC-V361-CH01-01).
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "ArtSource" / "LOC03" / "prints"
OUT.mkdir(parents=True, exist_ok=True)


def load_font(size: int, bold: bool = False, serif: bool = False):
    candidates = []
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


def make_bed_chart(filename: str, bed_no: str, temp_pts, pulse_pts, note_lines):
    """Clinical bedside inpatient monitoring chart (BỆNH ÁN NỘI TRÚ · P.101) clipped to Beds 1 & 3."""
    W, H = 768, 1024
    img = Image.new("RGB", (W, H), (233, 226, 206))
    d = ImageDraw.Draw(img)

    # Aged paper margin & subtle horizontal grain
    for y in range(0, H, 6):
        c = 231 if (y // 6) % 2 == 0 else 234
        d.line([(0, y), (W, y)], fill=(c, c - 7, c - 26), width=1)
    d.rectangle([28, 28, W - 28, H - 28], outline=(46, 56, 50), width=4)
    d.rectangle([38, 38, W - 38, H - 38], outline=(76, 88, 80), width=2)

    f_sm = load_font(22, bold=False)
    f_smb = load_font(23, bold=True)
    f_md = load_font(30, bold=True)
    f_lg = load_font(40, bold=True)
    f_mono = load_font(20, bold=False)

    d.text((58, 56), "SỞ Y TẾ ĐỊA PHƯƠNG", fill=(42, 54, 48), font=f_sm)
    d.text((58, 84), "BỆNH VIỆN ĐA KHOA AN SINH", fill=(28, 42, 36), font=f_smb)
    d.text((W - 245, 56), "MẪU: AS-TD-05", fill=(55, 64, 58), font=f_mono)
    d.text((W - 245, 84), f"PHÒNG 101 · G.{bed_no}", fill=(128, 34, 30), font=f_smb)

    d.line([(56, 124), (W - 56, 124)], fill=(45, 56, 50), width=3)
    d.text((88, 140), "PHIẾU THEO DÕI ĐIỀU DƯỠNG", fill=(26, 38, 34), font=load_font(36, bold=True))
    d.text((156, 188), f"KHOA NỘI TỔNG HỢP · GIƯỜNG {bed_no}", fill=(42, 68, 56), font=load_font(26, bold=True))

    # Info rows
    y0 = 236
    rows = [
        ("Buồng bệnh:  Phòng 101 (Khu A)", f"Giường số:  {bed_no}"),
        ("Chế độ chăm sóc:  Cấp III · Nội trú", "Đợt theo dõi:  11/2005"),
        ("Y tá phụ trách:  ĐD. Trần Thị Lan", "Bác sĩ trực:  BS. Khoa Nội"),
    ]
    for left, right in rows:
        d.rectangle([56, y0, W - 56, y0 + 40], outline=(82, 92, 85), width=2)
        d.text((68, y0 + 8), left, fill=(36, 44, 40), font=f_sm)
        d.text((442, y0 + 8), right, fill=(36, 44, 40), font=f_sm)
        y0 += 40

    # Vital signs temperature & pulse grid
    gx0, gy0, gx1, gy1 = 68, 384, W - 68, 670
    d.rectangle([gx0, gy0, gx1, gy1], fill=(241, 236, 220), outline=(44, 58, 52), width=3)
    d.text((gx0, gy0 - 28), "BIỂU ĐỒ MẠCH (đỏ) — NHIỆT ĐỘ (xanh) · THEO CA TRỰC", fill=(34, 48, 42), font=f_mono)

    cols = 10
    rows_g = 8
    cw = (gx1 - gx0) / cols
    ch = (gy1 - gy0) / rows_g
    for c in range(1, cols):
        x = int(gx0 + c * cw)
        d.line([(x, gy0), (x, gy1)], fill=(170, 180, 172), width=1 if c % 2 else 2)
    for r in range(1, rows_g):
        y = int(gy0 + r * ch)
        d.line([(gx0, y), (gx1, y)], fill=(170, 180, 172), width=1 if r % 2 else 2)

    # Plot curves (blue ink temp, red ink pulse)
    t_xy = [(int(gx0 + (i + 0.5) * cw), int(gy1 - p * (gy1 - gy0))) for i, p in enumerate(temp_pts)]
    p_xy = [(int(gx0 + (i + 0.5) * cw), int(gy1 - p * (gy1 - gy0))) for i, p in enumerate(pulse_pts)]
    for i in range(len(t_xy) - 1):
        d.line([t_xy[i], t_xy[i + 1]], fill=(38, 72, 138), width=4)
    for i in range(len(p_xy) - 1):
        d.line([p_xy[i], p_xy[i + 1]], fill=(158, 42, 38), width=4)
    for pt in t_xy:
        d.ellipse([pt[0] - 5, pt[1] - 5, pt[0] + 5, pt[1] + 5], fill=(38, 72, 138))
    for pt in p_xy:
        d.ellipse([pt[0] - 5, pt[1] - 5, pt[0] + 5, pt[1] + 5], fill=(158, 42, 38))

    # Nursing shift notes table
    ny = 700
    d.text((68, ny), "GHI CHÉP DIỄN BIẾN & Y LỆNH:", fill=(32, 46, 40), font=f_smb)
    ny += 34
    for idx in range(5):
        d.line([(68, ny + 38), (W - 68, ny + 38)], fill=(128, 138, 130), width=2)
        if idx < len(note_lines):
            d.text((76, ny + 8), note_lines[idx], fill=(42, 56, 98), font=f_sm)
        ny += 44

    # Stamp mark at bottom right
    d.ellipse([W - 210, H - 155, W - 75, H - 55], outline=(168, 48, 44), width=3)
    d.text((W - 190, H - 122), "KHOA NỘI", fill=(168, 48, 44), font=f_smb)
    d.text((W - 186, H - 96), "AN SINH", fill=(168, 48, 44), font=f_mono)

    img.save(OUT / filename)


def make_saline_label():
    """500ml NaCl 0.9% IV bottle wrap label."""
    W, H = 512, 320
    img = Image.new("RGB", (W, H), (236, 232, 218))
    d = ImageDraw.Draw(img)
    d.rectangle([10, 10, W - 10, H - 10], outline=(32, 86, 132), width=6)
    d.rectangle([10, 10, W - 10, 66], fill=(32, 92, 138))
    f_hdr = load_font(28, bold=True)
    f_big = load_font(56, bold=True)
    f_sm = load_font(20, bold=True)
    f_xs = load_font(17, bold=False)

    d.text((44, 22), "DUNG DỊCH TRUYỀN TĨNH MẠCH", fill=(240, 244, 240), font=f_hdr)
    d.text((54, 86), "NaCl 0,9%", fill=(26, 68, 112), font=f_big)
    d.text((48, 158), "NATRI CLORID ĐẲNG TRƯƠNG", fill=(36, 48, 46), font=load_font(19, bold=True))
    d.line([(36, 196), (W - 36, 196)], fill=(70, 96, 118), width=2)
    d.text((40, 210), "Thể tích: 500 ml  ·  Vô khuẩn", fill=(42, 50, 48), font=f_sm)
    d.text((40, 242), "Khoa Dược · BV Đa Khoa An Sinh", fill=(54, 62, 58), font=f_xs)
    d.text((40, 270), "Lô SX: AS-0511  ·  Chỉ dùng trong bệnh viện", fill=(132, 42, 38), font=f_xs)
    # Volume graduation tick column on right
    for i, ml in enumerate((500, 400, 300, 200, 100)):
        y = 86 + i * 38
        d.line([(W - 70, y), (W - 34, y)], fill=(36, 68, 104), width=3)
        d.text((W - 118, y - 10), str(ml), fill=(36, 68, 104), font=f_xs)
    img.save(OUT / "saline_label.png")


def make_med_labels():
    """Shared print atlas for medicine bottles, cabinet asset tags, bed plates & binder spines.
    Quadrants:
      Top-Left  (0.0..0.5, 0.5..1.0 in UV -> PIL y 0..512, x 0..512):   CỒN 70° & BETADINE bottle labels
      Top-Right (0.5..1.0, 0.5..1.0 in UV -> PIL y 0..512, x 512..1024): Asset tags (TS-AS-101) & Bed plates (G.01..G.03)
      Bot-Left  (0.0..0.5, 0.0..0.5 in UV -> PIL y 512..1024, x 0..512): Archival cabinet binder spines
      Bot-Right (0.5..1.0, 0.0..0.5 in UV -> PIL y 512..1024, x 512..1024): Chalk inventory tag G.04 · LƯU KHO 2012
    """
    W, H = 1024, 1024
    img = Image.new("RGB", (W, H), (228, 222, 205))
    d = ImageDraw.Draw(img)

    f_lg = load_font(44, bold=True)
    f_md = load_font(32, bold=True)
    f_sm = load_font(24, bold=True)
    f_xs = load_font(20, bold=False)

    # Top-Left: two stacked antiseptic bottle labels
    # Upper half (PIL y=16..240): CỒN 70°
    d.rectangle([16, 16, 496, 240], fill=(240, 236, 224), outline=(160, 40, 36), width=6)
    d.rectangle([16, 16, 496, 72], fill=(162, 42, 38))
    d.text((92, 26), "SÁT KHUẨN NGOÀI DA", fill=(246, 242, 234), font=f_md)
    d.text((130, 88), "CỒN 70°", fill=(154, 34, 32), font=load_font(56, bold=True))
    d.text((88, 162), "ETHANOL Y TẾ · 250 ml", fill=(42, 48, 44), font=f_sm)
    d.text((104, 198), "KHOA DƯỢC · BV AN SINH", fill=(68, 72, 66), font=f_xs)

    # Lower half of top-left (PIL y=264..496): BETADINE / POVIDONE-IODINE
    d.rectangle([16, 264, 496, 496], fill=(238, 226, 194), outline=(124, 68, 24), width=6)
    d.rectangle([16, 264, 496, 322], fill=(138, 76, 22))
    d.text((64, 276), "DUNG DỊCH SÁT TRÙNG", fill=(248, 242, 226), font=f_md)
    d.text((70, 340), "POVIDON-IOD", fill=(116, 52, 16), font=load_font(50, bold=True))
    d.text((108, 412), "NỒNG ĐỘ 10% · 125 ml", fill=(48, 42, 36), font=f_sm)
    d.text((104, 448), "CHỈ DÙNG NGOÀI · P.101", fill=(138, 38, 32), font=f_xs)

    # Top-Right: Bed plates G.01, G.02, G.03 + Metal Asset Tag TS-AS-101
    # PIL y=16..160: G.01 | y=176..320: G.02 | y=336..496: TS-AS-101 asset tag
    for idx, (y0, y1, title, sub, bg) in enumerate([
        (16, 158, "GIƯỜNG 01", "PHÒNG 101 · KHOA NỘI", (28, 72, 58)),
        (174, 316, "GIƯỜNG 02", "PHÒNG 101 · KHOA NỘI", (28, 72, 58)),
        (332, 496, "TS-AS-101", "TÀI SẢN BV AN SINH · 1987", (196, 192, 178)),
    ]):
        d.rectangle([528, y0, 1008, y1], fill=bg, outline=(228, 224, 210) if idx < 2 else (44, 52, 48), width=5)
        fg = (240, 238, 226) if idx < 2 else (28, 36, 32)
        d.text((616, y0 + 22), title, fill=fg, font=load_font(54, bold=True))
        d.text((602, y0 + 96), sub, fill=fg, font=f_sm)

    # Bottom-Left (PIL y=528..1008, x=16..496): 4 archival binder spines
    spines = [
        ("BỆNH ÁN NỘI TRÚ · P.101", (64, 86, 74), (235, 232, 220)),
        ("SỔ GIAO BAN CA ĐÊM 2005", (112, 84, 56), (242, 236, 220)),
        ("HỒ SƠ VẬT TƯ · KHU A", (52, 74, 96), (236, 238, 240)),
        ("KIỂM KÊ THANH LÝ 2012", (128, 106, 78), (34, 32, 28)),
    ]
    for i, (txt, bg, fg) in enumerate(spines):
        y0 = 528 + i * 118
        d.rectangle([16, y0, 496, y0 + 104], fill=bg, outline=(36, 34, 30), width=4)
        d.rectangle([42, y0 + 18, 470, y0 + 86], fill=(232, 224, 204) if i != 3 else (220, 212, 188), outline=(44, 42, 38), width=2)
        d.text((62, y0 + 36), txt, fill=(28, 32, 30), font=f_sm)

    # Bottom-Right (PIL y=528..1008, x=528..1008): Store Room Bed 4 Chalk/Kraft Inventory Tag!
    d.rectangle([528, 528, 1008, 1008], fill=(198, 168, 122), outline=(74, 54, 34), width=8)
    d.rectangle([546, 546, 990, 990], outline=(98, 72, 46), width=3)
    # Tie grommet hole at top center
    d.ellipse([746, 558, 790, 602], fill=(56, 44, 34), outline=(146, 118, 74), width=5)
    d.text((572, 622), "PHIẾU KIỂM KÊ KHO", fill=(48, 34, 24), font=load_font(34, bold=True))
    d.line([(566, 670), (968, 670)], fill=(72, 52, 34), width=3)
    d.text((578, 692), "GIƯỜNG SỐ 04", fill=(138, 32, 28), font=load_font(48, bold=True))
    d.text((570, 766), "NGUỒN: PHÒNG 101 · KHU A", fill=(46, 36, 28), font=load_font(22, bold=True))
    d.text((570, 808), "TRẠNG THÁI: THÁO RỜI / LƯU KHO", fill=(46, 36, 28), font=load_font(21, bold=True))
    d.text((570, 850), "NIÊM PHONG: ĐỢT DỪNG 2012", fill=(46, 36, 28), font=load_font(21, bold=True))
    d.rectangle([572, 900, 962, 966], outline=(148, 36, 32), width=4)
    d.text((602, 920), "LƯU KHO · KHÔNG SỬ DỤNG", fill=(148, 36, 32), font=load_font(22, bold=True))

    img.save(OUT / "med_labels.png")


def make_thermos_band():
    """Non-branded classic Vietnamese vintage crimson & cream tin vacuum thermos body graphic."""
    W, H = 512, 512
    img = Image.new("RGB", (W, H), (146, 36, 34))
    d = ImageDraw.Draw(img)
    # Horizontal vintage cream/gold bands top and bottom
    for y0, y1, col in [
        (0, 36, (198, 178, 134)),
        (36, 48, (96, 26, 24)),
        (464, 476, (96, 26, 24)),
        (476, 512, (198, 178, 134)),
        (168, 178, (212, 184, 126)),
        (334, 344, (212, 184, 126)),
    ]:
        d.rectangle([0, y0, W, y1], fill=col)
    # Classic geometric mid-band diamond/floral motif (generic 1980s VN household vacuum flask aesthetic)
    d.rectangle([0, 178, W, 334], fill=(168, 44, 38))
    for i in range(8):
        cx = int((i + 0.5) * W / 8)
        cy = 256
        d.polygon([(cx, cy - 42), (cx + 26, cy), (cx, cy + 42), (cx - 26, cy)],
                  fill=(220, 194, 138), outline=(108, 28, 26))
        d.ellipse([cx - 10, cy - 10, cx + 10, cy + 10], fill=(142, 38, 34))
    img.save(OUT / "thermos_band.png")


def main():
    make_bed_chart(
        "bed_chart_01.png",
        "01",
        temp_pts=[0.42, 0.48, 0.56, 0.52, 0.46, 0.44, 0.49, 0.45, 0.41, 0.40],
        pulse_pts=[0.55, 0.62, 0.68, 0.60, 0.54, 0.52, 0.58, 0.53, 0.50, 0.49],
        note_lines=[
            "20g00: BN tỉnh, tiếp xúc tốt, đã truyền 1 chai NaCl 0,9%.",
            "22g30: Đã phát thuốc tối theo y lệnh. Nghỉ yên tại giường.",
            "01g15: Kiểm tra mạch/nhiệt ổn định. Lưu ý giữ ấm ban đêm.",
        ],
    )
    make_bed_chart(
        "bed_chart_03.png",
        "03",
        temp_pts=[0.38, 0.41, 0.43, 0.40, 0.39, 0.42, 0.44, 0.41, 0.39, 0.38],
        pulse_pts=[0.48, 0.50, 0.53, 0.51, 0.47, 0.49, 0.52, 0.48, 0.46, 0.47],
        note_lines=[
            "19g30: Kiểm tra thuốc uống đặt ngăn tủ đầu giường 03.",
            "21g45: BN ngủ sớm, người nhà đã ra khỏi buồng đúng giờ.",
            "02g00: Tình trạng ổn định. Hẹn kiểm tra sáng hôm sau.",
        ],
    )
    make_saline_label()
    make_med_labels()
    make_thermos_band()
    print(f"Generated LOC03 archival prints in {OUT}")


if __name__ == "__main__":
    main()
