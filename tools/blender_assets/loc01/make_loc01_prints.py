"""Generate crisp high-resolution fictional Vietnamese 1987-2012 hospital exterior
print sheets for LOC01 Cycles PBR baking (100% fictional An Sinh Hospital, AR03 compliant).

Sheets written to ArtSource/LOC01/prints/:
  1. mailbox_stencil.png      - Weathered green iron mailbox front stencil ("HỘP THƯ · BỆNH VIỆN AN SINH")
  2. guardhouse_interior.png  - 4-panel guardhouse duty ledger, night-gate rules, shift calendar & key rack header
  3. motorbike_details.png    - Fictional Vietnamese license plate ("29-K4 048.12") + underbone speedometer dial
  4. bench_stencil.png        - Vietnamese terrazzo courtyard bench institutional stencil ("BỆNH VIỆN ĐA KHOA AN SINH · 1987")
  5. canopy_crest.png         - Entry portal transom/door signage + An Sinh 1987 institutional crest plaque
  6. pole_warning.png         - Utility pole high-voltage stencil ("CẤM TRÈO · ĐIỆN ÁP CAO · TRỤ 04-AS")
"""
from __future__ import annotations

import math
import random
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "ArtSource" / "LOC01" / "prints"
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


def _text_centered(d: ImageDraw.ImageDraw, cx: float, y: float, text: str, font, fill):
    bbox = d.textbbox((0, 0), text, font=font)
    w = bbox[2] - bbox[0]
    d.text((cx - w * 0.5, y), text, font=font, fill=fill)


def _add_speckle_weather(img: Image.Image, seed: int, count: int = 450, color=(70, 48, 32), radius_max=4):
    rng = random.Random(seed)
    d = ImageDraw.Draw(img, "RGBA")
    w, h = img.size
    for _ in range(count):
        x = rng.randint(0, w - 1)
        y = rng.randint(0, h - 1)
        r = rng.randint(1, radius_max)
        a = rng.randint(18, 65)
        d.ellipse([x - r, y - r, x + r, y + r], fill=(color[0], color[1], color[2], a))


def make_mailbox_stencil():
    """Weathered institutional green enamel front door panel for GATE-CONG mailbox."""
    W, H = 1024, 1024
    img = Image.new("RGB", (W, H), (48, 86, 64))
    d = ImageDraw.Draw(img, "RGBA")

    # Subtle vertical rain streaks down enamel face
    rng = random.Random(101)
    for _ in range(65):
        x = rng.randint(18, W - 18)
        y0 = rng.randint(0, 260)
        y1 = rng.randint(y0 + 180, H)
        w = rng.randint(2, 9)
        d.rectangle([x, y0, x + w, y1], fill=(24, 42, 32, rng.randint(14, 38)))

    # Rust weeping along bottom edge & under mail-slot zone
    for i in range(44):
        rx = rng.randint(0, W)
        rw = rng.randint(12, 48)
        rh = rng.randint(18, 68)
        d.ellipse([rx - rw, H - rh, rx + rw, H + 20], fill=(82, 38, 18, rng.randint(40, 105)))

    # Crisp double stencil border
    d.rectangle([42, 42, W - 42, H - 42], outline=(215, 208, 182, 195), width=6)
    d.rectangle([58, 58, W - 58, H - 58], outline=(215, 208, 182, 115), width=2)

    # Top mail slot guide zone (y ~ 160-260)
    _text_centered(d, W * 0.5, 96, "BỎ THƯ TẠI ĐÂY", load_font(38, bold=True), (222, 216, 192, 210))

    # Main centered stencil typography (below mail slot, clear of lock on left)
    _text_centered(d, W * 0.5, 340, "HỘP THƯ", load_font(124, bold=True), (232, 226, 204, 232))
    d.line([(160, 495), (W - 160, 495)], fill=(216, 208, 182, 185), width=5)

    # Stylized minimal medical cross emblem
    cx, cy = W // 2, 575
    d.rectangle([cx - 16, cy - 48, cx + 16, cy + 48], fill=(176, 46, 38, 215))
    d.rectangle([cx - 48, cy - 16, cx + 48, cy + 16], fill=(176, 46, 38, 215))

    _text_centered(d, W * 0.5, 656, "BỆNH VIỆN ĐA KHOA", load_font(52, bold=True), (224, 218, 194, 220))
    _text_centered(d, W * 0.5, 728, "AN SINH", load_font(78, bold=True), (236, 230, 208, 235))
    _text_centered(d, W * 0.5, 845, "CÔNG VĂN · THƯ TỪ · BÀN GIAO", load_font(32, bold=False), (206, 200, 178, 195))
    _text_centered(d, W * 0.5, 895, "THÀNH LẬP 1987", load_font(28, bold=True), (196, 190, 168, 175))

    _add_speckle_weather(img, 102, count=520, color=(42, 32, 22), radius_max=4)
    img.save(OUT / "mailbox_stencil.png")


def make_guardhouse_interior():
    """4-quadrant 1024x1024 interior detail sheet for visible guardhouse storytelling:
       TL (0..512, 0..512):   Open duty ledger ('SỔ TRỰC BẢO VỆ')
       TR (512..1024, 0..512): Wall notice ('NỘI QUY BẢO VỆ CỔNG ĐÊM')
       BL (0..512, 512..1024): Hanging shift calendar ('THÁNG 11 / 2005')
       BR (512..1024, 512..1024): Key cabinet strip ('TỦ CHÌA KHÓA')
    """
    W, H = 1024, 1024
    img = Image.new("RGB", (W, H), (226, 218, 196))
    d = ImageDraw.Draw(img)

    # ---- TL: Open Duty Ledger ----
    d.rectangle([0, 0, 512, 512], fill=(232, 224, 202))
    d.line([(256, 8), (256, 504)], fill=(140, 128, 108), width=4)
    d.rectangle([16, 16, 496, 496], outline=(110, 102, 86), width=2)
    _text_centered(d, 136, 26, "SỔ TRỰC BẢO VỆ", load_font(24, bold=True), (26, 36, 56))
    _text_centered(d, 376, 26, "CỔNG PHỤ · KHU A", load_font(24, bold=True), (26, 36, 56))
    for r in range(11):
        ry = 76 + r * 36
        d.line([(24, ry), (246, ry)], fill=(168, 182, 196), width=1)
        d.line([(266, ry), (488, ry)], fill=(168, 182, 196), width=1)
    f_log = load_font(14)
    left_rows = [
        "17/11 18:00 Ca đêm: Ông Bảy",
        "17/11 21:15 Xe cấp cứu vào K-D",
        "17/11 22:40 Khóa cổng chính",
        "18/11 00:30 Kiểm tra đèn sân",
        "18/11 02:10 Cắt điện nhánh phụ",
        "18/11 03:17 Ghi nhận P.101",
        "18/11 05:45 Bàn giao ca sáng",
        "--- Kiểm kê đóng cửa 2006 ---",
        "18/11/12 Gửi chìa hộp thư ngoài",
    ]
    for i, line in enumerate(left_rows):
        d.text((26, 84 + i * 36), line, font=f_log, fill=(36, 48, 86))
    right_rows = [
        "Ghi chú bàn giao chìa khóa:",
        "- Cổng phụ: Hộp thư trụ đông",
        "- Kho 101: Tủ trực điều dưỡng",
        "- Khu C: Giữ trong phong bì",
        "- Điện sân: Cầu dao trong bốt",
        "- Mưa lớn, kiểm tra rãnh thoát",
        "- Đề nghị đối chiếu sổ gốc",
        "Ký nhận: Bảy (Trực cổng)",
    ]
    for i, line in enumerate(right_rows):
        d.text((268, 84 + i * 36), line, font=f_log, fill=(44, 42, 64))

    # ---- TR: Wall Notice Board ----
    d.rectangle([512, 0, 1024, 512], fill=(236, 232, 218))
    d.rectangle([530, 18, 1006, 494], outline=(28, 76, 56), width=6)
    d.rectangle([530, 18, 1006, 114], fill=(28, 76, 56))
    _text_centered(d, 768, 32, "NỘI QUY BẢO VỆ CỔNG ĐÊM", load_font(27, bold=True), (242, 238, 224))
    _text_centered(d, 768, 72, "BỆNH VIỆN ĐA KHOA AN SINH · 18:00 - 06:00", load_font(17, bold=True), (218, 228, 212))
    rules = [
        "1. Đóng cổng chính sau 21:00, chỉ mở cổng phụ.",
        "2. Ghi chép đầy đủ người ra vào & giờ trực.",
        "3. Giữ đèn mái đón & đèn sân sáng suốt đêm.",
        "4. Không giao chìa khu kỹ thuật khi chưa ký.",
        "5. Báo trực ban khi có sự cố điện / mưa ngập.",
        "6. Bàn giao sổ trực đầy đủ lúc 06:00 hàng ngày.",
    ]
    for i, r in enumerate(rules):
        d.text((548, 138 + i * 52), r, font=load_font(17), fill=(32, 36, 34))
    _text_centered(d, 768, 452, "BAN GIÁM ĐỐC BỆNH VIỆN AN SINH", load_font(18, bold=True), (146, 36, 32))

    # ---- BL: Wall Calendar Nov 2005 ----
    d.rectangle([0, 512, 512, 1024], fill=(240, 234, 218))
    d.rectangle([20, 530, 492, 636], fill=(158, 34, 30))
    _text_centered(d, 256, 542, "THÁNG 11 · 2005", load_font(42, bold=True), (246, 240, 224))
    _text_centered(d, 256, 594, "LỊCH TRỰC BẢO VỆ AN SINH", load_font(20, bold=True), (242, 218, 178))
    days = ["T2", "T3", "T4", "T5", "T6", "T7", "CN"]
    for i, dn in enumerate(days):
        _text_centered(d, 56 + i * 66, 654, dn, load_font(22, bold=True), (156, 34, 30) if i == 6 else (38, 44, 42))
    # Nov 2005 starts on Tuesday (index 1)
    for day in range(1, 31):
        slot = day
        row, col = divmod(slot, 7)
        cx = 56 + col * 66
        cy = 702 + row * 60
        if day == 18:
            d.ellipse([cx - 24, cy - 8, cx + 24, cy + 36], outline=(176, 28, 24), width=4)
        _text_centered(d, cx, cy, str(day), load_font(26, bold=(day == 18)), (176, 28, 24) if (col == 6 or day == 18) else (32, 36, 36))

    # ---- BR: Key Rack Header & Labels ----
    d.rectangle([512, 512, 1024, 1024], fill=(42, 62, 52))
    d.rectangle([530, 530, 1006, 646], fill=(228, 222, 200), outline=(28, 38, 32), width=4)
    _text_centered(d, 768, 546, "TỦ CHÌA KHÓA BẢO VỆ", load_font(34, bold=True), (24, 56, 42))
    _text_centered(d, 768, 594, "KIỂM ĐẾM KHI GIAO CA", load_font(22, bold=True), (148, 36, 30))
    tags = ["01 CỔNG PHỤ", "02 TIẾP NHẬN", "03 PHÒNG 101", "04 KHO ĐỒ", "05 CẤP CỨU", "06 KHU C"]
    for idx, tag in enumerate(tags):
        r, c = divmod(idx, 2)
        tx = 548 + c * 224
        ty = 682 + r * 102
        d.rectangle([tx, ty, tx + 206, ty + 74], fill=(232, 226, 204), outline=(34, 42, 38), width=3)
        _text_centered(d, tx + 103, ty + 22, tag, load_font(22, bold=True), (28, 34, 32))

    _add_speckle_weather(img, 203, count=360, color=(78, 66, 46), radius_max=3)
    img.save(OUT / "guardhouse_interior.png")


def make_motorbike_details():
    """1024x512 2-panel sheet:
       Left  (0..512):   Fictional Vietnamese 2000s motorbike rear plate ('29-K4 / 048.12')
       Right (512..1024): Early-2000s underbone speedometer dial (brandless fictional 'AS-110')
    """
    W, H = 1024, 512
    img = Image.new("RGB", (W, H), (22, 24, 26))
    d = ImageDraw.Draw(img)

    # ---- Left: License plate ----
    d.rectangle([20, 32, 492, 480], fill=(230, 226, 214), outline=(22, 24, 26), width=14)
    d.rectangle([38, 50, 474, 462], outline=(42, 44, 46), width=4)
    _text_centered(d, 256, 86, "29-K4", load_font(122, bold=True), (24, 26, 28))
    d.line([(86, 246), (426, 246)], fill=(38, 40, 42), width=6)
    _text_centered(d, 256, 276, "048.12", load_font(122, bold=True), (24, 26, 28))
    # Mounting bolt heads
    for bx in (110, 402):
        d.ellipse([bx - 12, 68, bx + 12, 92], fill=(120, 124, 126), outline=(42, 44, 46), width=3)

    # ---- Right: Speedometer instrument cluster ----
    d.rounded_rectangle([532, 34, 1004, 478], radius=48, fill=(24, 28, 32), outline=(112, 118, 122), width=8)
    cx, cy = 768, 278
    R = 164
    d.ellipse([cx - R, cy - R, cx + R, cy + R], fill=(18, 22, 26), outline=(146, 152, 156), width=4)
    # Speed arc & ticks (0..140 km/h from -215 deg to +35 deg)
    f_num = load_font(24, bold=True)
    for km in range(0, 141, 10):
        ang = math.radians(-215.0 + (km / 140.0) * 250.0)
        r0 = R - (24 if km % 20 == 0 else 14)
        r1 = R - 6
        col = (212, 54, 44) if km >= 100 else (228, 232, 224)
        d.line([(cx + math.cos(ang) * r0, cy + math.sin(ang) * r0),
                (cx + math.cos(ang) * r1, cy + math.sin(ang) * r1)], fill=col, width=4 if km % 20 == 0 else 2)
        if km % 20 == 0:
            rn = R - 46
            tx, ty = cx + math.cos(ang) * rn, cy + math.sin(ang) * rn - 12
            _text_centered(d, tx, ty, str(km), f_num, col)

    # Odometer window & indicators
    d.rectangle([cx - 66, cy - 66, cx + 66, cy - 34], fill=(10, 12, 14), outline=(128, 134, 136), width=2)
    _text_centered(d, cx, cy - 62, "048215", load_font(24, bold=True), (232, 232, 222))
    _text_centered(d, cx, cy + 54, "km/h", load_font(20, bold=True), (190, 198, 196))
    _text_centered(d, cx, cy + 84, "AS-110", load_font(18, bold=True), (168, 178, 172))
    # Neutral green lamp + turn arrows
    d.ellipse([cx - 18, 66, cx + 18, 102], fill=(38, 156, 74), outline=(210, 220, 210), width=2)
    _text_centered(d, cx, 72, "N", load_font(22, bold=True), (242, 248, 240))
    # Needle resting at 0
    a0 = math.radians(-215.0)
    d.line([(cx, cy), (cx + math.cos(a0) * (R - 30), cy + math.sin(a0) * (R - 30))], fill=(230, 68, 42), width=6)
    d.ellipse([cx - 14, cy - 14, cx + 14, cy + 14], fill=(170, 174, 178), outline=(32, 34, 36), width=3)

    _add_speckle_weather(img, 304, count=220, color=(70, 62, 50), radius_max=3)
    img.save(OUT / "motorbike_details.png")


def make_bench_stencil():
    """1024x512 Vietnamese terrazzo courtyard bench backrest painted stencil."""
    W, H = 1024, 512
    img = Image.new("RGB", (W, H), (164, 166, 158))
    d = ImageDraw.Draw(img, "RGBA")

    # Fine subtle mineral terrazzo chips across background
    rng = random.Random(405)
    for _ in range(3600):
        x = rng.randint(0, W - 1)
        y = rng.randint(0, H - 1)
        r = rng.randint(1, 5)
        palette = [(118, 122, 118), (192, 190, 178), (94, 98, 96), (144, 140, 126), (178, 180, 172)]
        c = rng.choice(palette)
        d.ellipse([x - r, y - r, x + r, y + r], fill=(c[0], c[1], c[2], 185))

    # Classic Vietnamese hospital bench stenciled blue-teal painted donor lettering
    ink = (28, 72, 86, 218)
    ink_sub = (36, 78, 88, 195)
    d.rectangle([48, 48, W - 48, H - 48], outline=(38, 78, 86, 150), width=5)
    _text_centered(d, W * 0.5, 112, "BỆNH VIỆN ĐA KHOA AN SINH", load_font(62, bold=True), ink)
    d.line([(160, 210), (W - 160, 210)], fill=(38, 78, 86, 165), width=4)
    _text_centered(d, W * 0.5, 246, "THÀNH LẬP 1987 · KHU NỘI TRÚ", load_font(38, bold=True), ink_sub)
    _text_centered(d, W * 0.5, 332, "XIN GIỮ GÌN VỆ SINH VÀ TRẬT TỰ CHUNG", load_font(32, bold=False), ink_sub)

    _add_speckle_weather(img, 406, count=480, color=(58, 66, 52), radius_max=4)
    img.save(OUT / "bench_stencil.png")


def make_canopy_crest():
    """1024x512 Entry portal institutional enamel crest plaque + door glass vinyls."""
    W, H = 1024, 512
    img = Image.new("RGB", (W, H), (30, 68, 52))
    d = ImageDraw.Draw(img, "RGBA")

    d.rectangle([20, 20, W - 20, H - 20], outline=(228, 222, 196, 225), width=8)
    d.rectangle([34, 34, W - 34, H - 34], outline=(228, 222, 196, 130), width=3)

    # Left medical cross shield emblem
    cx, cy = 148, H // 2
    d.ellipse([cx - 78, cy - 78, cx + 78, cy + 78], fill=(232, 226, 204, 235), outline=(180, 174, 150, 220), width=5)
    d.rectangle([cx - 18, cy - 50, cx + 18, cy + 50], fill=(176, 36, 32, 240))
    d.rectangle([cx - 50, cy - 18, cx + 50, cy + 18], fill=(176, 36, 32, 240))

    _text_centered(d, 608, 86, "SỞ Y TẾ ĐỊA PHƯƠNG", load_font(32, bold=True), (212, 218, 198, 215))
    _text_centered(d, 608, 148, "BỆNH VIỆN ĐA KHOA AN SINH", load_font(44, bold=True), (244, 240, 224, 245))
    d.line([(295, 228), (920, 228)], fill=(224, 218, 194, 190), width=4)
    _text_centered(d, 608, 260, "KHU TIẾP NHẬN & NỘI TRÚ · K-A", load_font(36, bold=True), (232, 228, 208, 230))
    _text_centered(d, 608, 336, "THÀNH LẬP 1987 · TRỰC CẤP CỨU 24/24", load_font(28, bold=False), (206, 214, 198, 210))

    _add_speckle_weather(img, 507, count=360, color=(42, 34, 24), radius_max=4)
    img.save(OUT / "canopy_crest.png")


def make_pole_warning():
    """512x512 Vietnamese centrifugal concrete power pole stencil ('CẤM TRÈO · ĐIỆN ÁP CAO')."""
    W, H = 512, 512
    img = Image.new("RGB", (W, H), (128, 130, 122))
    d = ImageDraw.Draw(img, "RGBA")

    # Yellow hazard band
    d.rectangle([28, 48, W - 28, 348], fill=(208, 166, 46, 225), outline=(28, 28, 26, 225), width=8)
    _text_centered(d, W * 0.5, 70, "CẤM TRÈO", load_font(62, bold=True), (168, 28, 24, 240))
    _text_centered(d, W * 0.5, 144, "ĐIỆN ÁP CAO", load_font(48, bold=True), (26, 26, 24, 235))
    # Stylized lightning bolt polygon
    bolt = [(276, 206), (222, 272), (262, 272), (236, 330), (300, 256), (262, 256)]
    d.polygon(bolt, fill=(176, 28, 24, 235))

    # Pole ID stencil below
    _text_centered(d, W * 0.5, 386, "TRỤ 04-AS · 220V", load_font(42, bold=True), (32, 34, 34, 220))
    _add_speckle_weather(img, 608, count=320, color=(64, 62, 52), radius_max=4)
    img.save(OUT / "pole_warning.png")


def make_motorbike_decal():
    """1024x512 100% fictional period Dream-era side graphic stripe decal sheet (AR03 compliant).
       Deep plum-eggplant background matched seamlessly to ext_moto_maroon so zero rectangular edge shows!"""
    W, H = 1024, 512
    bg = (42, 14, 30)
    img = Image.new("RGB", (W, H), bg)
    d = ImageDraw.Draw(img, "RGBA")

    # Top half (y in 0..256): Mid side-cover ('cốp xe') dynamic sweeping gold/ivory/crimson wing graphic
    # Upper thin champagne-gold + white accent swoosh
    d.polygon([(58, 48), (740, 38), (940, 82), (760, 62), (96, 68)], fill=(218, 198, 134, 245))
    d.polygon([(82, 68), (720, 62), (835, 84), (118, 78)], fill=(242, 240, 234, 230))
    # Lower bold tapered crimson + ivory speed-wing graphic
    d.polygon([(95, 132), (650, 124), (964, 224), (775, 232), (245, 174)], fill=(240, 238, 232, 242))
    d.polygon([(130, 148), (620, 142), (904, 214), (760, 220), (285, 178)], fill=(172, 32, 46, 245))
    d.polygon([(168, 162), (590, 158), (826, 208), (735, 212), (315, 184)], fill=(216, 194, 124, 232))
    # Period fictional model emblem 'SUPER 100' (100% original fictional per AR03!)
    d.text((312, 146), "SUPER", font=load_font(44, bold=True), fill=(248, 246, 238, 250))
    d.text((488, 152), "100", font=load_font(38, bold=True), fill=(232, 210, 142, 250))
    d.text((270, 44), "4-STROKE · 100cc", font=load_font(20, bold=True), fill=(38, 18, 30, 230))

    # Bottom half (y in 256..512): Rear tail fairing ('đuôi xe') crisp horizontal crimson + silver stripe
    d.rectangle([0, 256, W, H], fill=bg)
    d.polygon([(20, 356), (986, 344), (1000, 370), (20, 378)], fill=(240, 238, 232, 245))
    d.polygon([(26, 374), (994, 364), (982, 400), (26, 398)], fill=(182, 32, 46, 248))
    d.polygon([(34, 398), (976, 398), (962, 412), (34, 408)], fill=(214, 194, 128, 235))

    img.save(OUT / "motorbike_decal.png")


def run_all():
    make_mailbox_stencil()
    make_guardhouse_interior()
    make_motorbike_details()
    make_motorbike_decal()
    make_bench_stencil()
    make_canopy_crest()
    make_pole_warning()
    print(f"LOC01_PRINTS_DONE -> {OUT}")


if __name__ == "__main__":
    run_all()
