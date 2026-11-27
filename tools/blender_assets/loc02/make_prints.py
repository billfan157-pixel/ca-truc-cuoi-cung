"""Generate 100% fictional Vietnamese 1987-2005 An Sinh Hospital archival print textures
for Blender LOC02 hero props (duty ledger, Lan's open logbook, taped partition notices,
vintage landline keypad/extension card, clock dials, duty roster board, retro battery wrap,
archival binder spines).

Zero third-party trademarks or real hospital details (Core 05 / AR03 compliant).
"""
import math
import random
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "ArtSource" / "LOC02" / "prints"
OUT.mkdir(parents=True, exist_ok=True)


def load_font(size: int, bold: bool = False, italic: bool = False, serif: bool = False):
    candidates = []
    if serif:
        if bold and italic:
            candidates += ["timesbi.ttf", "georgiab.ttf"]
        elif bold:
            candidates += ["timesbd.ttf", "georgiab.ttf"]
        elif italic:
            candidates += ["timesi.ttf", "georgiai.ttf"]
        else:
            candidates += ["times.ttf", "georgia.ttf"]
    else:
        if bold and italic:
            candidates += ["arialbi.ttf", "segoeuiz.ttf", "calibriz.ttf"]
        elif bold:
            candidates += ["arialbd.ttf", "segoeuib.ttf", "calibrib.ttf"]
        elif italic:
            candidates += ["ariali.ttf", "segoeuii.ttf", "calibrii.ttf"]
        else:
            candidates += ["arial.ttf", "segoeui.ttf", "calibri.ttf"]
    candidates += ["arial.ttf", "tahoma.ttf", "times.ttf"]
    for name in candidates:
        p = Path("C:/Windows/Fonts") / name
        if p.exists():
            try:
                return ImageFont.truetype(str(p), size)
            except OSError:
                pass
    return ImageFont.load_default()


def add_paper_grain(img: Image.Image, seed: int = 11, strength: float = 1.0, foxing: bool = False):
    rng = random.Random(seed)
    w, h = img.size
    overlay = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    dr = ImageDraw.Draw(overlay)
    # subtle warm paper speckles and aging vignette
    count = int((w * h) / 480 * strength)
    for _ in range(count):
        x = rng.randint(0, w - 1)
        y = rng.randint(0, h - 1)
        r = rng.randint(1, 3)
        c = rng.randint(115, 168)
        a = rng.randint(8, 22)
        dr.ellipse((x - r, y - r, x + r, y + r), fill=(c, int(c * 0.90), int(c * 0.72), a))
    if foxing:
        for _ in range(int(26 * strength)):
            x = rng.randint(0, w)
            y = rng.randint(0, h)
            rx = rng.randint(18, 95)
            ry = rng.randint(14, 72)
            a = rng.randint(14, 34)
            dr.ellipse((x - rx, y - ry, x + rx, y + ry), fill=(116, 86, 48, a))
        overlay = overlay.filter(ImageFilter.GaussianBlur(radius=6))
    return Image.alpha_composite(img.convert("RGBA"), overlay)


def draw_red_stamp(draw_img: Image.Image, cx: int, cy: int, radius: int, top_txt: str, mid_txt: str, bot_txt: str, angle: float = -11.0):
    sz = radius * 3
    stamp = Image.new("RGBA", (sz, sz), (0, 0, 0, 0))
    d = ImageDraw.Draw(stamp)
    c = sz // 2
    ink = (168, 36, 34, 188)
    d.ellipse((c - radius, c - radius, c + radius, c + radius), outline=ink, width=4)
    d.ellipse((c - radius + 7, c - radius + 7, c + radius - 7, c + radius - 7), outline=ink, width=2)
    f_sm = load_font(max(11, int(radius * 0.16)), bold=True)
    f_md = load_font(max(13, int(radius * 0.21)), bold=True)
    d.text((c, c - int(radius * 0.45)), top_txt, fill=ink, font=f_sm, anchor="mm")
    d.text((c, c), mid_txt, fill=ink, font=f_md, anchor="mm")
    d.text((c, c + int(radius * 0.44)), bot_txt, fill=ink, font=f_sm, anchor="mm")
    stamp = stamp.rotate(angle, resample=Image.BICUBIC, expand=False)
    draw_img.alpha_composite(stamp, (cx - c, cy - c))


def make_an01_ledger_spread():
    """2048x1024 open cloth-bound Vietnamese hospital duty ledger spread (AN01)."""
    w, h = 2048, 1024
    img = Image.new("RGBA", (w, h), (229, 220, 194, 255))
    d = ImageDraw.Draw(img)

    # Center gutter shadow
    for dx in range(-48, 49):
        t = 1.0 - abs(dx) / 48.0
        a = int((t ** 2.2) * 115)
        d.line([(w // 2 + dx, 0), (w // 2 + dx, h)], fill=(45, 36, 26, a))

    f_hdr = load_font(30, bold=True, serif=True)
    f_sub = load_font(21, bold=True, serif=True)
    f_col = load_font(18, bold=True)
    f_hand = load_font(21, italic=True, serif=True)
    f_small = load_font(16, serif=True)

    rule_col = (104, 126, 134, 155)
    red_rule = (165, 68, 62, 165)
    ink_blue = (34, 54, 102, 220)
    ink_dark = (36, 40, 48, 215)

    for page_idx, x0 in enumerate((56, w // 2 + 52)):
        pw = w // 2 - 112
        # Top header
        if page_idx == 0:
            d.text((x0 + pw // 2, 48), "BỆNH VIỆN ĐA KHOA AN SINH · KHOA NỘI TRÚ", fill=(42, 46, 44, 230), font=f_sub, anchor="mm")
            d.text((x0 + pw // 2, 86), "SỔ TRỰC ĐÊM & BÀN GIAO CA", fill=(28, 32, 30, 240), font=f_hdr, anchor="mm")
            d.text((x0 + 14, 124), "Tháng 11 / 2005  ·  Quyển số 04", fill=(60, 56, 48, 220), font=f_small)
            d.text((x0 + pw - 18, 124), "Trang 42", fill=(60, 56, 48, 220), font=f_small, anchor="ra")
        else:
            d.text((x0 + pw // 2, 54), "THEO DÕI CA TRỰC NGOÀI GIỜ (19:00 – 07:00)", fill=(42, 46, 44, 230), font=f_sub, anchor="mm")
            d.text((x0 + 14, 98), "Ngày trực: 17 – 18 / 11 / 2005   ·   ĐDV trực: Trần Ngọc Lan", fill=ink_blue, font=f_hand)
            d.text((x0 + pw - 18, 124), "Trang 43", fill=(60, 56, 48, 220), font=f_small, anchor="ra")

        # Table border + column headers
        top_y = 150
        bot_y = h - 56
        d.rectangle((x0, top_y, x0 + pw, bot_y), outline=(72, 78, 76, 210), width=3)
        d.line([(x0, top_y + 42), (x0 + pw, top_y + 42)], fill=red_rule, width=2)

        cols = [0.0, 0.14, 0.27, 0.78, 1.0]
        headers = ["GIỜ", "PHÒNG", "NỘI DUNG BÀN GIAO / GHI CHÉP", "KÝ NHẬN"]
        for i in range(1, len(cols) - 1):
            cx = x0 + int(pw * cols[i])
            d.line([(cx, top_y), (cx, bot_y)], fill=rule_col, width=2)
        for i, htxt in enumerate(headers):
            cx = x0 + int(pw * (cols[i] + cols[i + 1]) * 0.5)
            d.text((cx, top_y + 21), htxt, fill=(44, 48, 46, 225), font=f_col, anchor="mm")

        # Ruled horizontal lines
        row_h = 40
        rows = (bot_y - (top_y + 42)) // row_h
        for r in range(1, rows + 1):
            ry = top_y + 42 + r * row_h
            d.line([(x0 + 2, ry), (x0 + pw - 2, ry)], fill=rule_col, width=1)

        if page_idx == 0:
            entries = [
                ("19:00", "Trệt", "Nhận ca trực đêm Khu A. Đủ chìa khóa, sổ trực.", "Lan"),
                ("19:45", "P.101", "Kiểm kê 03 giường kê cố định (G.01, G.02, G.03).", "Lan"),
                ("20:30", "P.102", "Bệnh nhân ổn định, đã phát thuốc tối.", "Lan"),
                ("21:15", "Sảnh", "Khóa cổng phụ theo quy chế sau 21:00.", "Bảy"),
                ("22:10", "P.101", "Thay dịch truyền G.01, kiểm tra mạch/nhiệt.", "Lan"),
                ("23:05", "P.101", "G.02 hoàn tất phiếu chuyển khoa sáng mai.", "Lan"),
                ("23:40", "Cấp cứu", "Nhận điện báo chuẩn bị hồ sơ kê bổ sung.", "Lan"),
                ("00:25", "Kho", "Mở kho giường 101 kiểm tra khung dự phòng.", "Lan"),
                ("01:10", "P.101", "Ghi nhận lưu ý vị trí kê tạm góc tường Bắc.", "Lan"),
                ("01:55", "Khu B", "Cầu thang S1 khóa chốt ngoài giờ theo lệnh.", "Bảy"),
                ("02:30", "P.101", "Bệnh nhân G.03 ngủ yên, không phát sinh.", "Lan"),
            ]
        else:
            entries = [
                ("02:45", "Quầy", "Kiểm tra lại Phiếu bàn giao P.101 (mẫu F101).", "Lan"),
                ("03:00", "P.101", "Đối chiếu số giường trên bảng trực & sổ kho.", "Lan"),
                ("03:10", "Khu A", "Điện hành lang chập chờn ngắn, đã ổn định.", "Lan"),
                ("03:15", "P.101", "Hồ sơ chuyển 101/4 chờ xác minh sáng.", "Lan"),
                ("03:17", "Quầy", "Ghi chép bàn giao ca — chờ BS kiểm phiếu...", "T.N.Lan"),
                ("", "", "", ""),
                ("", "Lưu ý:", "Không để trống mục xác nhận giường 04.", ""),
                ("", "", "Giữ bản lưu tại tủ khô Phòng 101.", ""),
            ]

        for idx, (tm, rm, txt, sig) in enumerate(entries):
            ry = top_y + 42 + idx * row_h + 20
            col_ink = ink_blue if (page_idx == 1 or idx % 3 != 1) else ink_dark
            if tm:
                d.text((x0 + int(pw * 0.07), ry), tm, fill=col_ink, font=f_hand, anchor="mm")
            if rm:
                d.text((x0 + int(pw * 0.205), ry), rm, fill=col_ink, font=f_hand, anchor="mm")
            if txt:
                d.text((x0 + int(pw * 0.285), ry), txt, fill=col_ink, font=f_hand, anchor="lm")
            if sig:
                d.text((x0 + int(pw * 0.89), ry), sig, fill=col_ink, font=f_hand, anchor="mm")

    draw_red_stamp(img, w // 2 + 760, 745, 74, "BV ĐA KHOA AN SINH", "BAN TRỰC ĐÊM", "KHOA NỘI TRÚ", angle=-14)
    img = add_paper_grain(img, seed=101, strength=1.0, foxing=True)
    img.save(OUT / "an01_ledger_spread.png")


def make_lan_dutybook_spread():
    """1024x512 Lan's active open duty notebook resting right under her pen in B."""
    w, h = 1024, 512
    img = Image.new("RGBA", (w, h), (235, 229, 206, 255))
    d = ImageDraw.Draw(img)
    for dx in range(-28, 29):
        t = 1.0 - abs(dx) / 28.0
        d.line([(w // 2 + dx, 0), (w // 2 + dx, h)], fill=(48, 40, 30, int((t ** 2) * 95)))

    f_hdr = load_font(20, bold=True, serif=True)
    f_txt = load_font(17, italic=True, serif=True)
    ink = (30, 52, 112, 225)
    rule = (128, 152, 170, 145)

    for pi, x0 in enumerate((34, w // 2 + 28)):
        pw = w // 2 - 64
        for y in range(74, h - 32, 28):
            d.line([(x0, y), (x0 + pw, y)], fill=rule, width=1)
        d.line([(x0 + 58, 48), (x0 + 58, h - 32)], fill=(185, 92, 88, 150), width=2)
        if pi == 0:
            d.text((x0 + pw // 2, 46), "GHI CHÉP CA TRỰC · 17/11/2005", fill=(45, 48, 46, 220), font=f_hdr, anchor="mm")
            lines = [
                "19:30  Bàn giao dụng cụ y tế ca đêm",
                "20:15  Phát thuốc P.101 (3 giường)",
                "21:00  Kiểm tra khóa cửa sau sảnh",
                "22:20  Cập nhật nhiệt độ P.101 G.01",
                "23:15  Lưu phiếu chuyển giường 02",
                "00:40  Báo bảo vệ đèn sân chập chờn",
                "01:30  Kiểm tra hồ sơ lưu tủ khô",
                "02:10  Soát lại danh sách bệnh nhân",
            ]
        else:
            d.text((x0 + pw // 2, 46), "PHIẾU BÀN GIAO SÁNG 18/11", fill=(45, 48, 46, 220), font=f_hdr, anchor="mm")
            lines = [
                "02:50  Điền sẵn mẫu F101 — P.101",
                "03:05  Đánh dấu mục cần đối chiếu",
                "03:12  Để phiếu tại khay cửa sổ 1",
                "03:17  Chờ BS trực xác nhận ký...",
            ]
        for i, line in enumerate(lines):
            d.text((x0 + 10, 80 + i * 28), line, fill=ink, font=f_txt)

    img = add_paper_grain(img, seed=202, strength=0.7, foxing=False)
    img.save(OUT / "lan_dutybook_spread.png")


def make_partition_notices():
    """1024x1024 atlas with 2 authentic Vietnamese hospital reception glass notices (top half + bottom half)."""
    w, h = 1024, 1024
    img = Image.new("RGBA", (w, h), (232, 226, 205, 255))
    d = ImageDraw.Draw(img)

    f_title = load_font(44, bold=True)
    f_sub = load_font(28, bold=True)
    f_body = load_font(25)

    # Top half: Notice A (XUẤT TRÌNH SỔ KHÁM BỆNH)
    d.rectangle((28, 28, w - 28, h // 2 - 28), outline=(32, 72, 60, 235), width=6)
    d.rectangle((40, 40, w - 40, h // 2 - 40), outline=(32, 72, 60, 175), width=2)
    d.rectangle((40, 40, w - 40, 128), fill=(32, 76, 62, 235))
    d.text((w // 2, 84), "QUY ĐỊNH TIẾP NHẬN NGOÀI GIỜ", fill=(238, 234, 218, 250), font=f_title, anchor="mm")
    lines_a = [
        "1. Xuất trình Sổ khám bệnh và Thẻ BHYT tại ô cửa số 1.",
        "2. Giữ trật tự, ngồi ghế chờ theo thứ tự gọi tên.",
        "3. Ca cấp cứu đi thẳng hành lang đến Khoa Cấp Cứu.",
        "4. Không tự ý vào khu vực nội trú sau 21 giờ 00.",
    ]
    for i, ln in enumerate(lines_a):
        d.text((78, 176 + i * 62), ln, fill=(32, 36, 34, 235), font=f_body)
    d.text((w - 78, h // 2 - 72), "BAN GIÁM ĐỐC BỆNH VIỆN AN SINH", fill=(142, 36, 34, 220), font=f_sub, anchor="rm")

    # Bottom half: Notice B (GIỜ TRỰC ĐÊM)
    y0 = h // 2
    d.rectangle((28, y0 + 28, w - 28, h - 28), outline=(140, 38, 34, 230), width=6)
    d.text((w // 2, y0 + 96), "THÔNG BÁO GIỜ TRỰC ĐÊM", fill=(148, 34, 30, 240), font=f_title, anchor="mm")
    d.line([(96, y0 + 136), (w - 96, y0 + 136)], fill=(148, 34, 30, 200), width=3)
    d.text((w // 2, y0 + 206), "CA ĐÊM: 19:00 – 07:00 SÁNG HÔM SAU", fill=(30, 34, 34, 240), font=f_sub, anchor="mm")
    d.text((w // 2, y0 + 266), "Phụ trách tiếp nhận: Điều dưỡng trực tầng trệt", fill=(40, 44, 42, 230), font=f_body, anchor="mm")
    d.text((w // 2, y0 + 322), "Cổng phụ đóng lúc 21:00 · Gọi bảo vệ số máy lẻ 100", fill=(40, 44, 42, 230), font=f_body, anchor="mm")
    d.text((w // 2, y0 + 392), "KHU B TẠM DỪNG TIẾP NHẬN BAN ĐÊM", fill=(140, 42, 38, 225), font=f_sub, anchor="mm")

    img = add_paper_grain(img, seed=303, strength=0.9, foxing=True)
    img.save(OUT / "partition_notices.png")


def make_phone_label():
    """512x512 internal telephone extension strip taped to desktop phone."""
    w, h = 512, 512
    img = Image.new("RGBA", (w, h), (232, 226, 202, 255))
    d = ImageDraw.Draw(img)
    f_hdr = load_font(34, bold=True)
    f_row = load_font(28, bold=True)
    d.rectangle((14, 14, w - 14, h - 14), outline=(45, 58, 52, 230), width=5)
    d.rectangle((14, 14, w - 14, 96), fill=(36, 74, 62, 240))
    d.text((w // 2, 55), "MÁY LẺ NỘI BỘ", fill=(242, 238, 222, 255), font=f_hdr, anchor="mm")
    rows = [
        ("BẢO VỆ CỔNG", "100"),
        ("QUẦY TRỰC", "101"),
        ("KHOA CẤP CỨU", "104"),
        ("KHO VẬT TƯ", "108"),
        ("PHÒNG 101", "112"),
        ("TRỰC LÃNH ĐẠO", "115"),
    ]
    for i, (lbl, num) in enumerate(rows):
        y = 126 + i * 60
        if i > 0:
            d.line([(24, y - 16), (w - 24, y - 16)], fill=(140, 145, 138, 160), width=2)
        d.text((34, y + 12), lbl, fill=(32, 36, 34, 240), font=f_row, anchor="lm")
        d.text((w - 34, y + 12), num, fill=(148, 32, 30, 245), font=f_row, anchor="rm")
    img = add_paper_grain(img, seed=404, strength=0.8, foxing=True)
    img.save(OUT / "phone_extension_card.png")


def make_clock_dials():
    """1024x1024 crisp vintage Vietnamese hospital wall clock dials (B clean ivory, R aged damp-spotted)."""
    for tag, bg, ink, fox in (
        ("clock_dial_B", (236, 231, 212, 255), (26, 28, 28, 245), False),
        ("clock_dial_R", (196, 188, 162, 255), (38, 38, 34, 235), True),
    ):
        sz = 1024
        img = Image.new("RGBA", (sz, sz), bg)
        d = ImageDraw.Draw(img)
        c = sz // 2
        # Outer chapter rings
        d.ellipse((c - 440, c - 440, c + 440, c + 440), outline=ink, width=6)
        d.ellipse((c - 420, c - 420, c + 420, c + 420), outline=ink, width=2)
        # 60 minute ticks
        for m in range(60):
            a = math.radians(m * 6.0 - 90.0)
            r0 = 376 if m % 5 == 0 else 396
            r1 = 418
            w_line = 9 if m % 15 == 0 else (6 if m % 5 == 0 else 2)
            d.line(
                [(c + math.cos(a) * r0, c + math.sin(a) * r0),
                 (c + math.cos(a) * r1, c + math.sin(a) * r1)],
                fill=ink, width=w_line,
            )
        # 12 numerals
        f_num = load_font(86, bold=True, serif=True)
        for hr in range(1, 13):
            a = math.radians(hr * 30.0 - 90.0)
            r = 310
            d.text((c + math.cos(a) * r, c + math.sin(a) * r), str(hr), fill=ink, font=f_num, anchor="mm")
        f_brand = load_font(30, bold=True, serif=True)
        f_sub = load_font(22, serif=True)
        d.text((c, c - 135), "AN SINH", fill=(42, 68, 58, 220), font=f_brand, anchor="mm")
        d.text((c, c + 150), "QUARTZ · 1987", fill=(72, 72, 66, 200), font=f_sub, anchor="mm")
        img = add_paper_grain(img, seed=505 if not fox else 506, strength=0.7 if not fox else 1.6, foxing=fox)
        img.save(OUT / f"{tag}.png")


def make_roster_board():
    """1024x1024 wall-mounted Vietnamese hospital shift roster & notices board (BẢNG PHÂN CÔNG TRỰC)."""
    w, h = 1024, 1024
    # Green baize backing
    img = Image.new("RGBA", (w, h), (38, 76, 62, 255))
    d = ImageDraw.Draw(img)
    f_hdr = load_font(38, bold=True)
    f_sub = load_font(24, bold=True)
    f_txt = load_font(19)
    f_sm = load_font(16)

    # Top title banner strip
    d.rectangle((32, 26, w - 32, 114), fill=(24, 52, 42, 255), outline=(212, 198, 152, 220), width=3)
    d.text((w // 2, 70), "BẢNG PHÂN CÔNG TRỰC · KHOA NỘI TRÚ", fill=(238, 232, 204, 250), font=f_hdr, anchor="mm")

    # Left pinned sheet: Shift Roster Nov 2005
    sx0, sy0, sx1, sy1 = 48, 146, 560, 820
    d.rectangle((sx0 + 6, sy0 + 8, sx1 + 6, sy1 + 8), fill=(15, 26, 22, 140))
    d.rectangle((sx0, sy0, sx1, sy1), fill=(234, 228, 206, 255), outline=(140, 134, 118, 255), width=2)
    d.text(((sx0 + sx1) // 2, sy0 + 36), "LỊCH TRỰC THÁNG 11 / 2005", fill=(32, 38, 36, 240), font=f_sub, anchor="mm")
    d.text(((sx0 + sx1) // 2, sy0 + 68), "Ca đêm: 19:00 – 07:00", fill=(140, 38, 34, 230), font=f_txt, anchor="mm")
    rows = [
        ("14/11", "BS. Hải", "ĐDV. Hương", "Bảy"),
        ("15/11", "BS. Minh", "ĐDV. Lan", "Hùng"),
        ("16/11", "BS. Hải", "ĐDV. Mai", "Bảy"),
        ("17/11", "BS. Minh", "ĐDV. Lan", "Bảy"),
        ("18/11", "BS. Tuấn", "ĐDV. Lan", "Bảy"),
        ("19/11", "BS. Hải", "ĐDV. Hương", "Hùng"),
        ("20/11", "BS. Tuấn", "ĐDV. Mai", "Bảy"),
    ]
    ty = sy0 + 104
    d.rectangle((sx0 + 20, ty, sx1 - 20, ty + 480), outline=(65, 70, 68, 220), width=2)
    for i, (dt, bs, ddv, bv) in enumerate(rows):
        ry = ty + 32 + i * 64
        if dt in ("17/11", "18/11"):
            d.rectangle((sx0 + 22, ry - 26, sx1 - 22, ry + 30), fill=(242, 226, 178, 175))
        if i > 0:
            d.line([(sx0 + 20, ry - 28), (sx1 - 20, ry - 28)], fill=(120, 126, 122, 180), width=1)
        d.text((sx0 + 40, ry), dt, fill=(32, 36, 36, 240), font=f_txt)
        d.text((sx0 + 140, ry), bs, fill=(32, 36, 36, 240), font=f_txt)
        d.text((sx0 + 266, ry), ddv, fill=(34, 52, 108, 240), font=f_txt)
        d.text((sx0 + 408, ry), bv, fill=(40, 44, 42, 230), font=f_txt)

    # Right top sheet: Safety / Fire Memo
    rx0, ry0, rx1, ry1 = 602, 162, 976, 540
    d.rectangle((rx0 + 6, ry0 + 8, rx1 + 6, ry1 + 8), fill=(15, 26, 22, 140))
    d.rectangle((rx0, ry0, rx1, ry1), fill=(238, 229, 200, 255), outline=(140, 130, 114, 255), width=2)
    d.text(((rx0 + rx1) // 2, ry0 + 36), "QUY CHẾ BÀN GIAO", fill=(148, 36, 32, 235), font=f_sub, anchor="mm")
    for i, ln in enumerate([
        "- Kiểm đếm đủ hồ sơ nội trú.",
        "- Đối chiếu mẫu F101 trước 07:00.",
        "- Ghi rõ số giường & giờ bàn giao.",
        "- Khoá cửa Khu B lúc 21:00.",
        "- Báo trực lãnh đạo nếu quá tải.",
    ]):
        d.text((rx0 + 22, ry0 + 84 + i * 44), ln, fill=(38, 40, 38, 235), font=f_txt)

    # Right bottom slip: Phone Directory
    bx0, by0, bx1, by1 = 616, 588, 964, 938
    d.rectangle((bx0 + 5, by0 + 7, bx1 + 5, by1 + 7), fill=(15, 26, 22, 140))
    d.rectangle((bx0, by0, bx1, by1), fill=(224, 230, 212, 255), outline=(92, 108, 98, 255), width=2)
    d.text(((bx0 + bx1) // 2, by0 + 36), "SỐ ĐIỆN THOẠI KHẨN", fill=(32, 74, 60, 240), font=f_sub, anchor="mm")
    for i, ln in enumerate([
        "Cấp cứu tầng trệt ..... 104",
        "Bảo vệ cổng phụ ....... 100",
        "Kỹ thuật điện B1 ...... 109",
        "Kho vật tư ............ 108",
        "Phòng Hành chính ...... 115",
    ]):
        d.text((bx0 + 24, by0 + 84 + i * 46), ln, fill=(34, 38, 36, 235), font=f_txt)

    # Red pushpins
    for px, py in ((70, 166), (538, 166), (788, 182), (790, 608)):
        d.ellipse((px - 10, py - 10, px + 10, py + 10), fill=(176, 38, 34, 255), outline=(85, 18, 16, 255), width=2)
        d.ellipse((px - 5, py - 5, px - 1, py - 1), fill=(242, 150, 142, 220))

    draw_red_stamp(img, 452, 748, 52, "AN SINH", "ĐÃ DUYỆT", "11/2005", angle=-9)
    img = add_paper_grain(img, seed=606, strength=0.85, foxing=True)
    img.save(OUT / "roster_board.png")


def make_battery_label():
    """512x512 vintage fictional Vietnamese 1.5V D-cell battery wrap (no real brand)."""
    w, h = 512, 512
    img = Image.new("RGBA", (w, h), (156, 34, 30, 255))
    d = ImageDraw.Draw(img)
    # Navy top/bottom bands & cream/gold accent stripe
    d.rectangle((0, 0, w, 78), fill=(26, 42, 78, 255))
    d.rectangle((0, h - 78, w, h), fill=(26, 42, 78, 255))
    d.rectangle((0, 78, w, 98), fill=(214, 178, 86, 255))
    d.rectangle((0, h - 98, w, h - 78), fill=(214, 178, 86, 255))
    f_big = load_font(56, bold=True)
    f_md = load_font(30, bold=True)
    f_sm = load_font(22, bold=True)
    d.text((w // 2, 175), "PIN ĐẠI", fill=(242, 224, 162, 255), font=f_big, anchor="mm")
    d.text((w // 2, 250), "SAO MAI · R20", fill=(250, 246, 232, 255), font=f_md, anchor="mm")
    d.text((w // 2, 318), "1.5V  ·  DÙNG CHO ĐÈN PIN", fill=(238, 214, 142, 245), font=f_sm, anchor="mm")
    d.text((w // 2, 39), "+  CỰC DƯƠNG  +", fill=(240, 236, 218, 245), font=f_sm, anchor="mm")
    d.text((w // 2, h - 39), "CHỐNG RÒ RỈ · VIỆT NAM", fill=(214, 210, 194, 230), font=f_sm, anchor="mm")
    img = add_paper_grain(img, seed=707, strength=0.6, foxing=False)
    img.save(OUT / "battery_label.png")


def make_binder_spines():
    """1024x1024 sheet of archival folder/binder spine labels."""
    w, h = 1024, 1024
    img = Image.new("RGBA", (w, h), (220, 212, 186, 255))
    d = ImageDraw.Draw(img)
    f_sp = load_font(34, bold=True)
    labels = [
        ("HỒ SƠ BỆNH ÁN · 2005", (36, 74, 62, 240)),
        ("SỔ CHUYỂN VIỆN · KHOA NỘI", (138, 38, 34, 240)),
        ("PHIẾU TRỰC ĐÊM · T11/2005", (34, 52, 92, 240)),
        ("LƯU TRỮ PHÒNG 101 – 104", (52, 48, 42, 240)),
        ("HỒ SƠ BỆNH ÁN · 2004", (36, 74, 62, 240)),
        ("SỔ KHO VẬT TƯ · TẦNG TRỆT", (92, 64, 34, 240)),
        ("BÁO CÁO GIAO BAN ĐÊM", (138, 38, 34, 240)),
        ("QUY CHẾ TRỰC · AN SINH", (34, 52, 92, 240)),
    ]
    strip_h = h // len(labels)
    for i, (txt, col) in enumerate(labels):
        y0 = i * strip_h
        d.rectangle((20, y0 + 12, w - 20, y0 + strip_h - 12), fill=(232, 224, 198, 255), outline=col, width=4)
        d.rectangle((20, y0 + 12, 110, y0 + strip_h - 12), fill=col)
        d.text((65, y0 + strip_h // 2), f"0{i + 1}", fill=(242, 238, 220, 255), font=f_sp, anchor="mm")
        d.text((140, y0 + strip_h // 2), txt, fill=(32, 34, 32, 240), font=f_sp, anchor="lm")
    img = add_paper_grain(img, seed=808, strength=0.8, foxing=True)
    img.save(OUT / "binder_spines.png")


if __name__ == "__main__":
    make_an01_ledger_spread()
    make_lan_dutybook_spread()
    make_partition_notices()
    make_phone_label()
    make_clock_dials()
    make_roster_board()
    make_battery_label()
    make_binder_spines()
    print(f"[PRINTS] Generated 9 archival print sheets in {OUT}")
