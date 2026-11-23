#!/usr/bin/env python3
"""
Generate 2D architectural drawings for An Sinh Hospital Full Campus Expansion
synchronized 1:1 with Unity authoring coordinates (Core 03 Campus-v0 / DEC-WORLD-01).

Outputs in docs/campus/:
  - campus-master-plan.png   (100x110m Site Master Plan, 6 blocks, 4 gates, 4 streams)
  - campus-floors-B1-3F.png  (4-panel floor plans: B1 -3.4m, Grade 0m, 2F +4m, 3F +8m)
  - campus-sections.png      (Architectural Sections A-A' W-E and B-B' S-N with exact datums)
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent.parent
OUT_DIR = ROOT / "docs" / "campus"
OUT_DIR.mkdir(parents=True, exist_ok=True)

FONT_CANDIDATES = [
    ROOT / "Prototype" / "Assets" / "CTCC" / "Resources" / "CTCC" / "BeVietnamPro-SemiBold.ttf",
    Path(r"C:\Windows\Fonts\segoeuib.ttf"),
    Path(r"C:\Windows\Fonts\segoeui.ttf"),
    Path(r"C:\Windows\Fonts\arial.ttf"),
]
FONT_PATH = next((str(p) for p in FONT_CANDIDATES if p.exists()), None)


def font(size: int):
    if FONT_PATH:
        return ImageFont.truetype(FONT_PATH, size)
    return ImageFont.load_default()


# Architectural blueprint palette (clean technical off-white paper with muted structural inks)
BG = (246, 245, 240)
PANEL_BG = (252, 252, 249)
GRID = (224, 226, 222)
GRID_MAJOR = (195, 200, 194)
INK = (26, 34, 40)
MUTED = (96, 106, 112)
ROAD = (214, 218, 222)
PLAZA = (232, 232, 224)
LAWN = (220, 234, 214)
LIGHT_COURT = (206, 232, 238)
SUNKEN = (212, 206, 198)

C_KA = (214, 226, 212)       # K-A 1987 Main Core (sage plaster)
C_KA_LOCK = (182, 210, 188)  # Locked gameplay rooms inside K-A
C_KB = (210, 224, 238)       # K-B Outpatient (cool clinic blue)
C_KC = (208, 230, 222)       # K-C Inpatient 3F (teal-sage)
C_KD = (242, 212, 210)       # K-D Emergency (soft crimson tint)
C_KE = (232, 226, 206)       # K-E Admin (warm ochre)
C_KF = (224, 218, 214)       # K-F Logistics (industrial grey-brown)
C_KHUC = (226, 204, 204)     # B1 Khu C (sealed)
C_LINK = (226, 230, 234)     # Covered walkways

STR_PUBLIC = (42, 112, 186)
STR_AMB = (196, 46, 42)
STR_INPAT = (34, 134, 116)
STR_SERVICE = (158, 108, 38)


def draw_dashed_line(draw: ImageDraw.ImageDraw, pts, fill, width=3, dash=12, gap=8):
    for i in range(len(pts) - 1):
        x0, y0 = pts[i]
        x1, y1 = pts[i + 1]
        dx, dy = x1 - x0, y1 - y0
        dist = (dx * dx + dy * dy) ** 0.5
        if dist < 1:
            continue
        ux, uy = dx / dist, dy / dist
        pos = 0.0
        while pos < dist:
            end = min(pos + dash, dist)
            draw.line(
                [(x0 + ux * pos, y0 + uy * pos), (x0 + ux * end, y0 + uy * end)],
                fill=fill,
                width=width,
            )
            pos += dash + gap


def draw_arrow(draw: ImageDraw.ImageDraw, pts, fill, width=4, dashed=False):
    if dashed:
        draw_dashed_line(draw, pts, fill, width=width)
    else:
        draw.line(pts, fill=fill, width=width)
    if len(pts) >= 2:
        x0, y0 = pts[-2]
        x1, y1 = pts[-1]
        dx, dy = x1 - x0, y1 - y0
        d = (dx * dx + dy * dy) ** 0.5
        if d > 1:
            ux, uy = dx / d, dy / d
            px, py = -uy, ux
            sz = 11
            tri = [
                (x1, y1),
                (x1 - ux * sz + px * sz * 0.55, y1 - uy * sz + py * sz * 0.55),
                (x1 - ux * sz - px * sz * 0.55, y1 - uy * sz - py * sz * 0.55),
            ]
            draw.polygon(tri, fill=fill)


def centered_text(draw, box, text, font, fill=INK, line_gap=4):
    x0, y0, x1, y1 = box
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    lines = text.split("\n")
    sizes = [draw.textbbox((0, 0), ln, font=font) for ln in lines]
    heights = [b[3] - b[1] for b in sizes]
    total_h = sum(heights) + line_gap * (len(lines) - 1)
    y = cy - total_h / 2
    for ln, b, h in zip(lines, sizes, heights):
        w = b[2] - b[0]
        draw.text((cx - w / 2, y - b[1]), ln, font=font, fill=fill)
        y += h + line_gap


def draw_master_plan():
    W, H = 2600, 1900
    img = Image.new("RGB", (W, H), BG)
    draw = ImageDraw.Draw(img)

    # Header bar
    draw.rectangle([0, 0, W, 118], fill=(30, 42, 50))
    draw.text((48, 22), "BỆNH VIỆN ĐA KHOA AN SINH — MẶT BẰNG TỔNG THỂ QUẦN THỂ (MASTER SITE PLAN)", font=font(34), fill=(244, 246, 244))
    draw.text((48, 70), "Ca Trực Cuối Cùng · Core 03 Campus-v0 / DEC-WORLD-01 · Quy mô 100 × 110 m (1,1 ha) · Hệ tọa độ đồng bộ 1:1 với Unity World (X Đông, Z Bắc)", font=font(20), fill=(186, 206, 212))

    # Map viewport (left) and architectural schedule/legend (right)
    vp = (70, 155, 1790, 1830)
    draw.rectangle(vp, fill=PANEL_BG, outline=INK, width=3)

    # World bounds X in [-48..64] (112m), Z in [-42..86] (128m)
    # Note: +Z is NORTH (UP on plan), +X is EAST (RIGHT on plan)
    wx0, wx1 = -48.0, 64.0
    wz0, wz1 = -42.0, 86.0
    scale = min((vp[2] - vp[0] - 80) / (wx1 - wx0), (vp[3] - vp[1] - 80) / (wz1 - wz0))
    ox = vp[0] + (vp[2] - vp[0] - (wx1 - wx0) * scale) / 2
    oy = vp[1] + (vp[3] - vp[1] - (wz1 - wz0) * scale) / 2

    def pt(x, z):
        return (ox + (x - wx0) * scale, oy + (wz1 - z) * scale)

    def rect(x0, z0, x1, z1, fill=None, outline=INK, width=2):
        p0 = pt(x0, z1)
        p1 = pt(x1, z0)
        draw.rectangle([p0[0], p0[1], p1[0], p1[1]], fill=fill, outline=outline, width=width)
        return (p0[0], p0[1], p1[0], p1[1])

    # Lawn inside 100x110m perimeter wall
    rect(-42, -30, 58, 80, fill=LAWN, outline=None, width=0)

    # 10m coordinate grid
    for gx in range(-40, 61, 10):
        p0, p1 = pt(gx, -40), pt(gx, 84)
        col = GRID_MAJOR if gx == 0 else GRID
        draw.line([p0, p1], fill=col, width=2 if gx == 0 else 1)
        draw.text((p0[0] - 14, vp[3] - 32), f"{gx:+d}", font=font(14), fill=MUTED)
    for gz in range(-40, 81, 10):
        p0, p1 = pt(-46, gz), pt(62, gz)
        col = GRID_MAJOR if gz == 0 else GRID
        draw.line([p0, p1], fill=col, width=2 if gz == 0 else 1)
        draw.text((vp[0] + 10, p0[1] - 8), f"{gz:+d}", font=font(14), fill=MUTED)

    # Roads & paved aprons
    rect(-46, -39.4, 62, -30.2, fill=ROAD, outline=MUTED, width=1)  # South Avenue
    rect(-5, -30.2, 13, -20.0, fill=ROAD, outline=MUTED, width=1)   # Night gate approach
    rect(-34.5, -30.2, -25.5, -8.0, fill=ROAD, outline=MUTED, width=1)  # Main gate drive
    rect(-27.2, -29.0, -5.8, -8.0, fill=PLAZA, outline=MUTED, width=1)  # Public forecourt
    rect(-22.1, -8.0, -14.9, 40.0, fill=PLAZA, outline=MUTED, width=1)  # West courtyard walk
    rect(-33.0, 27.0, -27.0, 42.0, fill=PLAZA, outline=MUTED, width=1)  # Path to K-E
    rect(47.9, -30.2, 57.1, 36.2, fill=ROAD, outline=MUTED, width=1)    # Ambulance driveway
    rect(39.2, 13.8, 52.8, 31.2, fill=ROAD, outline=MUTED, width=1)     # Ambulance apron
    rect(18.0, -5.0, 39.0, 13.0, fill=PLAZA, outline=MUTED, width=1)    # East terrace
    rect(9.8, 54.8, 18.2, 80.2, fill=ROAD, outline=MUTED, width=1)      # North service road
    rect(-21.0, 53.4, 15.0, 58.6, fill=ROAD, outline=MUTED, width=1)    # Cross lane to morgue
    rect(-22.6, 38.5, -17.4, 56.5, fill=ROAD, outline=MUTED, width=1)   # Morgue west branch
    rect(12.5, 72.8, 54.5, 78.2, fill=ROAD, outline=MUTED, width=1)     # North-east service loop
    rect(49.1, 39.0, 55.9, 77.0, fill=ROAD, outline=MUTED, width=1)     # East connector

    # Sunken Courts
    rb_rear = rect(37.2, 35.0, 49.0, 45.0, fill=SUNKEN, outline=INK, width=2)
    centered_text(draw, rb_rear, "Sân sau hạ cốt\n-1.90m", font=font(14))
    rect(34.7, 35.0, 37.2, 45.0, fill=(196, 178, 170), outline=(140, 48, 42), width=2)  # E05b pit
    rect(-19.5, 37.7, -1.5, 43.1, fill=SUNKEN, outline=STR_SERVICE, width=2)
    p_morg = pt(-20.2, 44.8)
    draw.text((p_morg[0], p_morg[1]), "Ram nhà xác (-3.4m)", font=font(12), fill=STR_SERVICE)

    # Covered Walkways
    rect(-22.0, 3.0, -15.0, 7.0, fill=C_LINK, outline=INK, width=2)
    rect(10.0, 46.0, 22.0, 50.0, fill=C_LINK, outline=INK, width=2)
    p_lnk = pt(11.0, 52.8)
    draw.text((p_lnk[0], p_lnk[1]), "Cầu nối 1F/2F K-A↔K-C", font=font(12), fill=INK)

    # Parking Sheds
    r_park = rect(-40.5, -22.5, -34.5, -10.5, fill=(216, 222, 228), outline=INK, width=1)
    centered_text(draw, r_park, "Nhà xe\nBN", font=font(13))

    # 6 ARCHITECTURAL BLOCKS
    # Block K-A (1987 Main 3F + B1)
    rect(-15.0, -8.0, 18.0, 40.0, fill=C_KA, outline=INK, width=3)
    rect(-6.0, 40.0, 10.0, 50.0, fill=C_KA, outline=INK, width=3)
    # Locked CH01-02 interior footprint overlay inside K-A
    r_loc01 = rect(-5.0, -20.0, 13.0, -8.0, fill=(224, 232, 224), outline=(44, 96, 62), width=3)
    centered_text(draw, r_loc01, "LOC01 · SÂN ĐÊM\n(CH01 · AP01)", font=font(16), fill=(24, 68, 42))
    r_loc02 = rect(-1.0, -8.0, 9.0, 0.0, fill=C_KA_LOCK, outline=(44, 96, 62), width=2)
    centered_text(draw, r_loc02, "LOC02\nLễ tân", font=font(14), fill=(24, 68, 42))
    rect(2.5, 0.0, 5.5, 16.0, fill=C_KA_LOCK, outline=(44, 96, 62), width=1)  # HALL
    rect(-1.0, 0.0, 1.1, 16.0, fill=C_KA_LOCK, outline=(44, 96, 62), width=1)  # STAFF
    r_101 = rect(5.7, 2.0, 13.7, 8.0, fill=(164, 204, 174), outline=(28, 84, 48), width=2)
    centered_text(draw, r_101, "LOC03\nP.101", font=font(14), fill=(20, 62, 34))
    rect(13.9, 2.0, 17.1, 5.6, fill=C_KA_LOCK, outline=(44, 96, 62), width=1)  # STORE
    r_loc04 = rect(-1.0, 16.0, 9.0, 22.0, fill=(232, 202, 198), outline=(148, 42, 38), width=2)
    centered_text(draw, r_loc04, "LOC04\nCấp cứu", font=font(14), fill=(116, 28, 24))
    # Inner Light Court (Sân sáng)
    r_lc = rect(5.6, 8.1, 14.2, 14.1, fill=LIGHT_COURT, outline=(36, 108, 128), width=2)
    centered_text(draw, r_lc, "SÂN SÁNG\n(thông 3F)", font=font(13), fill=(24, 78, 96))
    # Label K-A upper mass
    centered_text(draw, rect(-14.5, 22.5, 17.5, 39.5, fill=None, outline=None, width=0),
                  "KHỐI K-A · KHỐI CHÍNH (1987)\n3 Tầng (+11.7m) + Tầng hầm B1 (-3.4m)\nS1/S2/S3 · LOC05-08 · LOC09-12", font=font(16), fill=INK)

    # Block K-B (Outpatient 2F, 1998-2002)
    r_kb = rect(-38.0, -8.0, -22.0, 28.0, fill=C_KB, outline=INK, width=3)
    centered_text(draw, r_kb, "KHỐI K-B\nKHÁM NGOẠI TRÚ\n2 Tầng (+8.0m)\n1998–2002\nNhà thuốc · CLS", font=font(17))
    rect(-34.2, -11.3, -25.8, -8.0, fill=C_LINK, outline=INK, width=2)  # Entry portico

    # Block K-D (Emergency 1F + Ambulance Porte-Cochère, 1995-2000)
    r_kd = rect(18.0, 14.0, 43.0, 32.0, fill=C_KD, outline=(148, 38, 34), width=3)
    centered_text(draw, r_kd, "KHỐI K-D · CẤP CỨU\n1 Tầng (+4.6m) · 1995–2000\nLưu bệnh · X-Quang · Tiểu phẫu", font=font(16), fill=(112, 24, 20))
    r_can = rect(43.0, 16.0, 49.5, 29.0, fill=(248, 226, 224), outline=(176, 36, 32), width=3)
    centered_text(draw, r_can, "Mái đón\nxe cấp\ncứu", font=font(13), fill=(140, 24, 20))

    # Block K-C (Inpatient 3F, 2001-2004, 56 beds)
    r_kc = rect(22.0, 46.0, 48.0, 68.0, fill=C_KC, outline=INK, width=3)
    centered_text(draw, r_kc, "KHỐI K-C · ĐIỀU TRỊ NỘI TRÚ\n3 Tầng (+12.1m) · 2001–2004\nBan công Nam · 14P (~56 giường*)", font=font(15))

    # Block K-E (Admin 2F, 1990s)
    r_ke = rect(-40.0, 41.0, -26.0, 63.0, fill=C_KE, outline=INK, width=3)
    centered_text(draw, r_ke, "KHỐI K-E\nHÀNH CHÍNH\n2 Tầng (+8.0m)\n1990s", font=font(16))

    # Block K-F (Logistics + 17.5m Water Tower)
    r_kf = rect(-14.0, 59.0, 10.0, 73.0, fill=C_KF, outline=INK, width=3)
    centered_text(draw, r_kf, "KHỐI K-F · HẬU CẦN & KỸ THUẬT\n1 Tầng (+4.1m) · Bếp · Giặt · Nồi hơi · Kho", font=font(15))
    # Water tower circle
    wt = pt(-18.5, 66.5)
    draw.ellipse([wt[0] - 24, wt[1] - 24, wt[0] + 24, wt[1] + 24], fill=(208, 214, 220), outline=INK, width=3)
    draw.text((wt[0] - 44, wt[1] + 28), "Đài nước\n+17.5m", font=font(13), fill=INK)

    # Perimeter Wall (100x110m thick dark border)
    def wall_seg(x0, z0, x1, z1):
        draw.line([pt(x0, z0), pt(x1, z1)], fill=INK, width=5)

    wall_seg(-42, -30, -35, -30)
    wall_seg(-25, -30, -5, -30)
    wall_seg(-5, -30, -5, -20)
    wall_seg(13, -30, 13, -20)
    wall_seg(13, -30, 48, -30)
    wall_seg(-42, -30, -42, 80)
    wall_seg(58, -30, 58, 80)
    wall_seg(-42, 80, 10, 80)
    wall_seg(18, 80, 58, 80)

    # 4 Differentiated Gates
    def gate_badge(x, z, label, col):
        px, py = pt(x, z)
        draw.rounded_rectangle([px - 88, py - 20, px + 88, py + 20], radius=6, fill=col, outline=(255, 255, 255), width=2)
        centered_text(draw, (px - 88, py - 20, px + 88, py + 20), label, font=font(14), fill=(255, 255, 255))

    gate_badge(-30, -30, "G1 · CỔNG CHÍNH", STR_PUBLIC)
    gate_badge(4, -22.2, "G2 · CỔNG ĐÊM (AP00)", (44, 104, 66))
    gate_badge(53, -30, "G3 · CỔNG CẤP CỨU", STR_AMB)
    gate_badge(14, 80, "G4 · CỔNG HẬU CẦN", STR_SERVICE)

    # 4 Segregated Circulation Stream Arrows
    draw_arrow(draw, [pt(-30, -37), pt(-30, -14), pt(-18, -14), pt(-18, 5), pt(-15, 5)], STR_PUBLIC, width=5)
    draw_arrow(draw, [pt(52.5, -37), pt(52.5, 22.5), pt(46.5, 22.5)], STR_AMB, width=5)
    draw_arrow(draw, [pt(4, 22), pt(4, 48), pt(22, 48)], STR_INPAT, width=4, dashed=True)
    draw_arrow(draw, [pt(14, 84), pt(14, 56), pt(-20, 56), pt(-20, 40.5), pt(-10, 40.5)], STR_SERVICE, width=4, dashed=True)
    draw_arrow(draw, [pt(14, 75.5), pt(52.5, 75.5), pt(52.5, 40), pt(45, 40)], STR_SERVICE, width=4, dashed=True)

    # North Arrow & Scale Bar inside viewport
    nx, ny = vp[2] - 65, vp[1] + 65
    draw.polygon([(nx, ny - 34), (nx - 16, ny + 18), (nx, ny + 6), (nx + 16, ny + 18)], fill=INK)
    draw.text((nx - 9, ny - 58), "N", font=font(22), fill=INK)

    sbx, sby = vp[0] + 65, vp[3] - 66
    for idx, meters in enumerate([0, 10, 20]):
        x = sbx + meters * scale
        if idx < 2:
            draw.rectangle([x, sby, x + 10 * scale, sby + 10], fill=INK if idx % 2 == 0 else PANEL_BG, outline=INK, width=2)
        draw.text((x - 8, sby + 14), f"{meters}m", font=font(14), fill=INK)

    # Right Sidebar: Program Schedule, Area/Bed Breakdown & Legend
    sx0, sy0, sx1, sy1 = 1820, 155, 2550, 1830
    draw.rectangle([sx0, sy0, sx1, sy1], fill=PANEL_BG, outline=INK, width=3)
    y = sy0 + 26

    draw.text((sx0 + 26, y), "BẢNG QUY MÔ KIẾN TRÚC & GIAI ĐOẠN", font=font(24), fill=INK)
    y += 42
    draw.line([(sx0 + 24, y), (sx1 - 24, y)], fill=INK, width=2)
    y += 18

    rows = [
        ("K-A · Khối Chính (1987)", "3F + B1 · ~2.120 m² · 8P / 32G* · Lõi CH01–08", C_KA),
        ("K-B · Khám Ngoại Trú (2002)", "2F · ~1.152 m² · Khám, CLS, XN, Nhà thuốc", C_KB),
        ("K-C · Nội Trú 3F (2004)", "3F · ~1.716 m² · 14P / 56G* · Ban công Nam", C_KC),
        ("K-D · Cấp Cứu (1995–2000)", "1F · ~450 m² + Mái đón 85 m² · Lưu cấp cứu", C_KD),
        ("K-E · Hành Chính (1990s)", "2F · ~616 m² · Giám đốc, Tài vụ, Lưu trữ", C_KE),
        ("K-F · Hậu Cần & Đài Nước", "1F · ~336 m² + Đài +17.5m · Bếp, Giặt, Nồi hơi", C_KF),
    ]
    for title, desc, col in rows:
        draw.rectangle([sx0 + 26, y + 4, sx0 + 54, y + 32], fill=col, outline=INK, width=2)
        draw.text((sx0 + 68, y), title, font=font(18), fill=INK)
        draw.text((sx0 + 68, y + 24), desc, font=font(15), fill=MUTED)
        y += 58

    y += 10
    draw.rounded_rectangle([sx0 + 24, y, sx1 - 24, y + 132], radius=8, fill=(238, 242, 238), outline=(72, 98, 80), width=2)
    draw.text((sx0 + 40, y + 14), "TỔNG HỢP QUY MÔ (PLANNING SEED — KHÔNG PHẢI CANON)", font=font(16), fill=(26, 68, 42))
    draw.text((sx0 + 40, y + 42), "• Khu đất: 100 × 110 m = 11.000 m² (1,1 ha) · MĐXD ~29%", font=font(16), fill=INK)
    draw.text((sx0 + 40, y + 68), "• Tổng sàn xây dựng (GFA): ~6.390 m² (gồm B1 700 m²)", font=font(16), fill=INK)
    draw.text((sx0 + 40, y + 94), "• *Giường thường quy quy hoạch: 88 giường (22P×4G) + 12G Khu C", font=font(15), fill=INK)
    y += 156

    draw.text((sx0 + 26, y), "4 LUỒNG GIAO THÔNG TÁCH BIỆT (CORE 03 §3.1)", font=font(22), fill=INK)
    y += 38
    draw.line([(sx0 + 24, y), (sx1 - 24, y)], fill=INK, width=2)
    y += 18
    streams = [
        ("Luồng 1 — Ngoại trú / Khách", "Cổng chính (-30,-30) → K-B → Cầu nối K-A", STR_PUBLIC),
        ("Luồng 2 — Cấp cứu (Ưu tiên)", "Cổng cấp cứu (53,-30) → Đường riêng → Mái đón K-D", STR_AMB),
        ("Luồng 3 — Nội trú / Phẫu thuật", "Liên thông có mái che K-A ↔ K-C (1F & 2F), tách B1", STR_INPAT),
        ("Luồng 4 — Hậu cần & Nhà xác", "Cổng Bắc (14,80) → K-F / Ram nhà xác Tây & Sân -1.9m", STR_SERVICE),
    ]
    for st, sd, col in streams:
        draw.line([(sx0 + 26, y + 14), (sx0 + 66, y + 14)], fill=col, width=6)
        draw.text((sx0 + 78, y), st, font=font(17), fill=INK)
        draw.text((sx0 + 78, y + 23), sd, font=font(15), fill=MUTED)
        y += 56

    y += 12
    draw.text((sx0 + 26, y), "NIÊN ĐẠI XÂY DỰNG (1987 → 2005 → 2012)", font=font(22), fill=INK)
    y += 38
    draw.line([(sx0 + 24, y), (sx1 - 24, y)], fill=INK, width=2)
    y += 16
    chrono = [
        ("1987", "Khối K-A (3F + B1), vữa vôi vàng-xanh, ô gạch bông gió cầu thang."),
        ("1990s", "Khối K-E Hành chính 2F và Khối K-F Hậu cần + Đài nước 17,5m."),
        ("1995–2002", "Khối K-D Cấp cứu + mái đón xe cứu thương; Khối K-B Ngoại trú 2F."),
        ("2001–2004", "Khối K-C Nội trú 3F; 2004 bịt gạch cửa sau B1 Khu C (E05b)."),
        ("2005 (Layer B)", "Ca trực vận hành sạch, đèn huỳnh quang T8, xe cứu thương đậu mái đón."),
        ("2012 (Layer R)", "Đóng cửa 7 năm, thấm nước, rêu chân tường, mưa đêm, thanh lý dở dang."),
    ]
    for yr, txt in chrono:
        draw.text((sx0 + 26, y), f"{yr}:", font=font(16), fill=(30, 74, 96))
        draw.text((sx0 + 148, y), txt, font=font(15), fill=INK)
        y += 38

    y += 14
    draw.text((sx0 + 26, y), "PHÂN VÙNG STREAMING UNITY (8 SCENES)", font=font(20), fill=INK)
    y += 32
    draw.text((sx0 + 26, y), "Boot (5): LOC01..04 + Campus_Grounds (CAMPUS_EXT)", font=font(15), fill=INK)
    draw.text((sx0 + 26, y + 24), "Stream (3): LOC05 (2F/3F) · LOC09 (B1) · Campus_Interiors", font=font(15), fill=INK)
    draw.text((sx0 + 26, y + 48), "Bảo toàn 100% tọa độ LOC01–12, P.101, AP00–09, G01–G08.", font=font(15), fill=(26, 94, 52))

    out = OUT_DIR / "campus-master-plan.png"
    img.save(out)
    print(f"Saved {out}")


def draw_floor_plans():
    W, H = 2600, 1900
    img = Image.new("RGB", (W, H), BG)
    draw = ImageDraw.Draw(img)

    draw.rectangle([0, 0, W, 110], fill=(30, 42, 50))
    draw.text((48, 20), "BỆNH VIỆN AN SINH — MẶT BẰNG CÁC TẦNG (B1 · TẦNG TRỆT · TẦNG 2 · TẦNG 3)", font=font(32), fill=(244, 246, 244))
    draw.text((48, 66), "Đồng bộ tuyệt đối với HospitalAuthoring / Expansion / Campus · Giữ nguyên khóa tọa độ LOC01–LOC12, S1/S2/S3 và chứng cứ E05b", font=font(19), fill=(186, 206, 212))

    panels = [
        ("TẦNG HẦM B1 · CỐT -3.40m (LOC09–LOC12 + HỐ QUAN SÁT E05b)", (48, 138, 1280, 988), "B1"),
        ("TẦNG TRỆT · CỐT ±0.00m (LOC01–LOC04 + K-B / K-D / K-C / K-E / K-F)", (1320, 138, 2552, 988), "1F"),
        ("TẦNG 2 · CỐT +4.00m (S1 GALLERY · LOC05 KHU B · LOC06 · K-B/K-C/K-E 2F)", (48, 1020, 1280, 1866), "2F"),
        ("TẦNG 3 · CỐT +8.00m (S1 ARCHIVE · LOC07 KHO · LOC08 GIÁM ĐỐC · K-C 3F)", (1320, 1020, 2552, 1866), "3F"),
    ]

    for title, (px0, py0, px1, py1), floor in panels:
        draw.rectangle([px0, py0, px1, py1], fill=PANEL_BG, outline=INK, width=3)
        draw.rectangle([px0, py0, px1, py0 + 42], fill=(48, 62, 72))
        draw.text((px0 + 18, py0 + 10), title, font=font(18), fill=(244, 246, 244))

        if floor == "B1":
            wx0, wx1, wz0, wz1 = -24.0, 56.0, 18.0, 56.0
        elif floor == "1F":
            wx0, wx1, wz0, wz1 = -44.0, 58.0, -32.0, 76.0
        elif floor == "2F":
            wx0, wx1, wz0, wz1 = -44.0, 54.0, -14.0, 72.0
        else:
            wx0, wx1, wz0, wz1 = -24.0, 54.0, -14.0, 72.0

        pad_x, pad_y = 32, 24
        usable_w = (px1 - px0) - pad_x * 2
        usable_h = (py1 - (py0 + 42)) - pad_y * 2
        sc = min(usable_w / (wx1 - wx0), usable_h / (wz1 - wz0))
        ox = px0 + pad_x + (usable_w - (wx1 - wx0) * sc) / 2
        oy = (py0 + 42) + pad_y + (usable_h - (wz1 - wz0) * sc) / 2

        def pt(x, z):
            return (ox + (x - wx0) * sc, oy + (wz1 - z) * sc)

        def rbox(x0, z0, x1, z1, fill, label="", outline=INK, width=2, fsz=14, lfill=INK):
            p0 = pt(x0, z1)
            p1 = pt(x1, z0)
            b = (p0[0], p0[1], p1[0], p1[1])
            draw.rectangle(b, fill=fill, outline=outline, width=width)
            if label:
                centered_text(draw, b, label, font=font(fsz), fill=lfill)
            return b

        # Grid every 10m
        for gx in range(int(wx0 // 10) * 10, int(wx1) + 1, 10):
            if wx0 <= gx <= wx1:
                draw.line([pt(gx, wz0), pt(gx, wz1)], fill=GRID, width=1)
        for gz in range(int(wz0 // 10) * 10, int(wz1) + 1, 10):
            if wz0 <= gz <= wz1:
                draw.line([pt(wx0, gz), pt(wx1, gz)], fill=GRID, width=1)

        if floor == "B1":
            # S2 Stair from LOC04
            rbox(5.65, 22.0, 8.35, 31.1, (206, 214, 220), "S2\n↓", fsz=12)
            # LOC09 Technical
            rbox(4.5, 31.1, 10.5, 45.5, (216, 224, 214), "LOC09\nKỹ thuật\nAP08", fsz=12)
            # LOC10 Morgue + Discreet West Door & Sunken Ramp
            rbox(-1.5, 36.0, 4.5, 42.0, (212, 220, 228), "LOC10\nNhà xác\nAP09", fsz=11)
            rbox(-19.5, 37.7, -1.5, 43.1, SUNKEN, "Ram dốc dịch vụ nhà xác\n(cửa Tây khóa z=40.95 · W-Q04)", outline=STR_SERVICE, fsz=12, lfill=STR_SERVICE)
            # LOC11 Vestibule C
            rbox(10.5, 39.0, 14.5, 45.0, (226, 220, 208), "L11", fsz=11)
            # LOC12 Khu C (sealed)
            rbox(14.5, 35.0, 34.5, 49.0, C_KHUC, "LOC12 · KHU C (KHÓA G06)\n3 Khoang × 4 Giường = 12 Giường\nTường Đông bịt gạch 2004 (x=34.5)", outline=(146, 40, 36), width=3, fsz=14, lfill=(110, 24, 20))
            # Accessible Exterior Observation Gallery for E05b (W-Q03!)
            rbox(11.3, 45.0, 13.7, 51.2, (230, 238, 218), "", outline=(44, 112, 62), width=2)
            rbox(11.3, 49.0, 37.2, 51.2, (230, 238, 218), "", outline=(44, 112, 62), width=2)
            p_gal = pt(11.3, 53.6)
            draw.text((p_gal[0], p_gal[1]), "Hành lang bao kỹ thuật Bắc (mở từ LOC11 · xem E05b trước G06)", font=font(13), fill=(28, 82, 44))
            rbox(34.7, 35.0, 37.2, 51.2, (236, 222, 212), "", outline=(168, 52, 40), width=2)
            p_pit = pt(34.8, 33.8)
            draw.text((p_pit[0] - 8, p_pit[1]), "Hố E05b (-3.4m)", font=font(12), fill=(132, 32, 24))
            # Brick infill highlight at x=34.5, z=42
            p_seal = pt(34.5, 42.0)
            draw.rectangle([p_seal[0] - 5, p_seal[1] - 18, p_seal[0] + 5, p_seal[1] + 18], fill=(182, 42, 32))
            draw.text((p_seal[0] - 135, p_seal[1] - 60), "E05b Bịt gạch 2004", font=font(12), fill=(160, 28, 22))
            # Rear Sunken Court -1.90m + East Ramp
            rbox(37.2, 35.0, 49.0, 45.0, SUNKEN, "SÂN SAU HẠ CỐT\n-1.90m\n(bậc vỡ nối hố E05b)", fsz=12)
            rbox(49.0, 37.7, 54.0, 42.3, ROAD, "Ram\n0m", fsz=11)
            # Route arrow AP08 -> LOC11 -> E05b
            draw_arrow(draw, [pt(7.5, 42.0), pt(12.5, 42.0), pt(12.5, 50.1), pt(35.8, 50.1), pt(35.8, 42.0)], (36, 128, 62), width=3, dashed=True)

        elif floor == "1F":
            rbox(-38, -8, -22, 28, C_KB, "K-B Ngoại trú\n(1F)", fsz=14)
            rbox(-22, 3, -15, 7, C_LINK, "Nối", fsz=11)
            rbox(-15, -8, 18, 40, C_KA, "", width=2)
            rbox(-6, 40, 10, 50, C_KA, "", width=2)
            rbox(-5, -20, 13, -8, (224, 232, 224), "LOC01 Sân (AP01)", outline=(40, 98, 58), fsz=13)
            rbox(-1, -8, 9, 0, C_KA_LOCK, "LOC02\nLễ tân", outline=(40, 98, 58), fsz=12)
            rbox(-1, 0, 1.1, 16, C_KA_LOCK, "", outline=(40, 98, 58), width=1)
            rbox(2.5, 0, 5.5, 16, C_KA_LOCK, "", outline=(40, 98, 58), width=1)
            rbox(5.7, 2, 13.7, 8, (164, 204, 174), "P.101", outline=(26, 84, 46), fsz=12)
            rbox(13.9, 2, 17.1, 5.6, C_KA_LOCK, "", outline=(40, 98, 58), width=1)
            rbox(5.6, 8.1, 14.2, 14.1, LIGHT_COURT, "Sân sáng", outline=(36, 108, 128), fsz=11)
            rbox(-1, 16, 9, 22, (232, 202, 198), "LOC04", outline=(146, 40, 36), fsz=12)
            rbox(18, 14, 43, 32, C_KD, "K-D Cấp cứu\n(1F)", outline=(146, 40, 36), fsz=14)
            rbox(43, 16, 49.5, 29, (248, 226, 224), "Mái\nđón", outline=(168, 36, 32), fsz=11)
            rbox(22, 46, 48, 68, C_KC, "K-C Nội trú (1F)", fsz=14)
            rbox(10, 46, 22, 50, C_LINK, "Nối", fsz=11)
            rbox(-40, 41, -26, 63, C_KE, "K-E\nH.Chính", fsz=13)
            rbox(-14, 59, 10, 73, C_KF, "K-F Hậu cần", fsz=13)
            wt = pt(-18.5, 66.5)
            draw.ellipse([wt[0] - 11, wt[1] - 11, wt[0] + 11, wt[1] + 11], fill=(208, 214, 220), outline=INK, width=2)

        elif floor == "2F":
            rbox(-38, -8, -22, 28, C_KB, "K-B Tầng 2\nXét nghiệm · CLS", fsz=14)
            rbox(-15, -8, 18, 40, C_KA, "", width=2)
            rbox(-6, 40, 10, 50, C_KA, "", width=2)
            # S1 West Gallery & Bridges
            rbox(-13.7, -5.35, -11.0, 38.2, (192, 218, 202), "", outline=(32, 92, 54))
            draw.text((pt(-10.2, 18.0)[0], pt(-10.2, 18.0)[1]), "S1 Hành lang Tây\n(thoát ra sân · W-Q02)", font=font(11), fill=(26, 86, 48))
            rbox(-11.0, 35.9, -3.5, 38.2, (192, 218, 202), "", outline=(32, 92, 54))
            rbox(-11.0, -5.35, -1.0, -2.65, (206, 218, 220), "", outline=(32, 92, 54))
            draw.text((pt(-10.6, -9.8)[0], pt(-10.6, -9.8)[1]), "Thang S1 xuống LOC02", font=font(11), fill=(26, 86, 48))
            # S3 Emergency stair
            rbox(2.65, 22.0, 5.35, 32.1, (232, 208, 204), "S3", outline=(146, 40, 36), fsz=11)
            # LOC05 Khu B & LOC06 Duty Corridor
            rbox(-3.5, 32.1, 7.5, 38.2, (164, 204, 174), "LOC05 · KHU B (AP06)", outline=(28, 84, 48), width=2, fsz=11)
            rbox(2.6, 38.2, 5.4, 47.1, (186, 212, 192), "L06", outline=(28, 84, 48), width=2, fsz=10)
            draw.text((pt(-5.6, 46.8)[0], pt(-5.6, 46.8)[1]), "LOC06 · AP07 · G03", font=font(11), fill=(26, 80, 44))
            # Inner Light Court Void
            rbox(5.6, 8.1, 14.2, 14.1, LIGHT_COURT, "Thông tầng\nSân sáng", outline=(36, 108, 128), fsz=11)
            # Bridge K-A to K-C & K-C 2F & K-E 2F
            rbox(10, 46, 22, 50, C_LINK, "Cầu nối 2F", fsz=11)
            rbox(22, 46, 48, 68, C_KC, "K-C Tầng 2\nNội trú (20 giường)\nBan công Nam", fsz=14)
            rbox(-40, 41, -26, 63, C_KE, "K-E Tầng 2\nHành chính", fsz=13)
            # W-Q02 escape line
            draw_arrow(draw, [pt(2.0, 36.8), pt(-12.3, 36.8), pt(-12.3, -4.0), pt(-1.5, -4.0)], (28, 118, 62), width=3, dashed=True)

        elif floor == "3F":
            rbox(-15, -8, 18, 12, C_KA, "K-A Tầng 3 · Bệnh phòng Nam", fsz=14)
            rbox(-17, 12, 10.5, 40, C_KA, "", width=2)
            # S1 Archive Landing & Bridge
            rbox(-16.2, 33.25, -13.5, 38.15, (192, 218, 202), "S1", outline=(32, 92, 54), fsz=11)
            rbox(-13.5, 35.45, -3.5, 38.15, (192, 218, 202), "Cầu S1", outline=(32, 92, 54), fsz=10)
            # LOC07 Archive & LOC08 Director
            rbox(-3.5, 29.5, 3.5, 38.5, (164, 204, 174), "LOC07\nKho\nAN03", outline=(28, 84, 48), width=2, fsz=11)
            rbox(3.5, 29.5, 8.5, 35.5, (232, 220, 194), "L08\nGĐ\nG05", outline=(140, 96, 32), width=2, fsz=10)
            # Inner Light Court open sky
            rbox(5.6, 8.1, 14.2, 14.1, LIGHT_COURT, "Thông tầng\nSân sáng", outline=(36, 108, 128), fsz=11)
            # K-C 3F
            rbox(22, 46, 48, 68, C_KC, "K-C Tầng 3\nNội trú (20 giường)\nMái tôn chống nóng + Bồn nước", fsz=14)

    out = OUT_DIR / "campus-floors-B1-3F.png"
    img.save(out)
    print(f"Saved {out}")


def draw_sections():
    W, H = 2600, 1550
    img = Image.new("RGB", (W, H), BG)
    draw = ImageDraw.Draw(img)

    draw.rectangle([0, 0, W, 110], fill=(30, 42, 50))
    draw.text((48, 20), "BỆNH VIỆN AN SINH — MẶT CẮT KIẾN TRÚC TỔNG THỂ (ARCHITECTURAL SECTIONS)", font=font(32), fill=(244, 246, 244))
    draw.text((48, 66), "Kiểm soát cao độ chuẩn: B1 (-3.40m) · Sân sau (-1.90m) · Trệt (±0.00m) · Tầng 2 (+4.00m) · Tầng 3 (+8.00m) · Mái (+11.70m) · Đài nước (+17.50m)", font=font(19), fill=(186, 206, 212))

    datums = [
        (-3.4, "-3.40m (B1)"),
        (-1.9, "-1.90m (Sân sau)"),
        (0.0, "±0.00m (Trệt)"),
        (4.0, "+4.00m (Tầng 2)"),
        (8.0, "+8.00m (Tầng 3)"),
        (11.7, "+11.70m (Mái K-A)"),
        (17.5, "+17.50m (Đài nước)"),
    ]

    # Section 1: West-East through Z ≈ 38..42m (K-E -> Morgue Ramp -> LOC10/09/11/12 + K-A 3F -> E05b Pit -> Rear Court -> East Road)
    def render_section(title, box, x_min, x_max, y_min, y_max, draw_fn):
        px0, py0, px1, py1 = box
        draw.rectangle([px0, py0, px1, py1], fill=PANEL_BG, outline=INK, width=3)
        draw.rectangle([px0, py0, px1, py0 + 42], fill=(48, 62, 72))
        draw.text((px0 + 18, py0 + 10), title, font=font(19), fill=(244, 246, 244))

        left_m, right_m, top_m, bot_m = 165, 50, 34, 46
        uw = (px1 - px0) - left_m - right_m
        uh = (py1 - (py0 + 42)) - top_m - bot_m
        sx = uw / (x_max - x_min)
        sy = uh / (y_max - y_min)

        def pt(x, y):
            return (px0 + left_m + (x - x_min) * sx, (py0 + 42) + top_m + (y_max - y) * sy)

        # Datum lines
        for dy, dlabel in datums:
            if y_min <= dy <= y_max:
                p0, p1 = pt(x_min, dy), pt(x_max, dy)
                col = (120, 134, 144) if abs(dy) < 0.01 else GRID_MAJOR
                draw_dashed_line(draw, [p0, p1], col, width=2 if abs(dy) < 0.01 else 1, dash=10, gap=6)
                draw.text((px0 + 12, p0[1] - 9), dlabel, font=font(14), fill=INK if abs(dy) < 0.01 else MUTED)

        # Earth cutaway fill below grade y=0
        draw_fn(pt, sx, sy)

        # X axis labels
        for gx in range(int(x_min // 10) * 10, int(x_max) + 1, 10):
            if x_min <= gx <= x_max:
                p = pt(gx, y_min)
                draw.text((p[0] - 14, py1 - 28), f"{gx:+d}m", font=font(14), fill=MUTED)

    def sec_we(pt, sx, sy):
        def sbox(x0, y0, x1, y1, fill, label="", outline=INK, width=2, fsz=14, lfill=INK):
            p0, p1 = pt(x0, y1), pt(x1, y0)
            b = (p0[0], p0[1], p1[0], p1[1])
            draw.rectangle(b, fill=fill, outline=outline, width=width)
            if label:
                centered_text(draw, b, label, font=font(fsz), fill=lfill)

        # Earth strata below 0m
        sbox(-44, -5.0, 58, 0.0, (226, 220, 212), "", outline=None, width=0)
        # Excavated West Morgue Sunken Ramp (-19.5..-1.5)
        draw.polygon([pt(-20, 0), pt(-10, -3.4), pt(-1.5, -3.4), pt(-1.5, 0)], fill=PANEL_BG, outline=STR_SERVICE)
        draw.line([pt(-20, 0), pt(-10, -3.4), pt(-1.5, -3.4)], fill=STR_SERVICE, width=4)
        draw.text((pt(-14.5, -0.5)[0], pt(-14.5, -0.5)[1]), "Ram nhà xác (-3.4m)", font=font(12), fill=STR_SERVICE)
        # Excavated E05b pit & Rear Sunken Court (34.5..52.5)
        draw.polygon([pt(34.5, 0), pt(34.5, -3.4), pt(37.2, -3.4), pt(37.2, -1.9), pt(49.0, -1.9), pt(52.5, 0)], fill=PANEL_BG, outline=INK)
        draw.line([pt(34.5, -3.4), pt(37.2, -3.4), pt(37.2, -1.9), pt(49.0, -1.9), pt(52.5, 0)], fill=INK, width=3)

        # K-E Admin 2F (-40..-26)
        sbox(-40, 0, -26, 4.0, C_KE, "K-E 1F", fsz=14)
        sbox(-40, 4.0, -26, 8.0, C_KE, "K-E 2F · Hành chính", fsz=14)
        draw.polygon([pt(-40, 8.0), pt(-33, 9.6), pt(-26, 8.0)], fill=(184, 178, 170), outline=INK)

        # B1 Rooms (-1.5..34.5, y=-3.4..-0.3)
        sbox(-1.5, -3.4, 4.5, -0.3, (212, 220, 228), "LOC10\nNhà xác", fsz=13)
        sbox(4.5, -3.4, 10.5, -0.3, (216, 224, 214), "LOC09\nKỹ thuật", fsz=13)
        sbox(10.5, -3.4, 14.5, -0.3, (226, 220, 208), "L11", fsz=12)
        sbox(14.5, -3.4, 34.5, -0.3, C_KHUC, "LOC12 · KHU C (B1 -3.40m · 12 Giường · Khóa G06)", outline=(146, 40, 36), width=3, fsz=14, lfill=(110, 24, 20))
        # E05b 2004 brick infill wall at x=34.5
        p_b = pt(34.5, -1.8)
        draw.line([pt(34.5, -3.4), pt(34.5, -0.8)], fill=(186, 36, 28), width=7)
        draw.text((p_b[0] + 8, p_b[1] - 46), "E05b Bịt gạch\n2004 + Hố xem", font=font(13), fill=(156, 26, 20))
        draw.text((pt(40.0, -1.1)[0], pt(40.0, -1.1)[1]), "Sân sau\n-1.90m", font=font(13), fill=INK)

        # K-A Upper Stack over B1
        sbox(-8.0, 0.0, 18.0, 4.0, C_KA, "K-A Tầng Trệt (Cốt ±0.00m)", fsz=14)
        sbox(-15.0, 4.0, -11.0, 8.0, (192, 218, 202), "S1", fsz=12)
        sbox(-11.0, 4.0, 18.0, 8.0, C_KA, "K-A Tầng 2 (+4.00m) · LOC05 Khu B / LOC06", fsz=14)
        sbox(-17.0, 8.0, -13.5, 11.7, (192, 218, 202), "S1", fsz=12)
        sbox(-13.5, 8.0, 3.5, 11.7, (164, 204, 174), "LOC07 · Kho (+8.00m)", fsz=13)
        sbox(3.5, 8.0, 10.5, 11.7, (232, 220, 194), "LOC08", fsz=13)
        draw.polygon([pt(-15, 11.7), pt(-2, 13.6), pt(10.5, 11.7)], fill=(184, 178, 170), outline=INK)
        # Background K-C 3F silhouette behind sunken court (x=22..48 dashed)
        p0, p1 = pt(22, 12.1), pt(48, 0)
        draw_dashed_line(draw, [(p0[0], p1[1]), (p0[0], p0[1]), (p1[0], p0[1]), (p1[0], p1[1])], (62, 124, 112), width=2)
        draw.text((pt(26, 10.6)[0], pt(26, 10.6)[1]), "Khối K-C 3F phía sau (Z=46..68)", font=font(14), fill=(48, 102, 92))

    def sec_sn(pt, sx, sy):
        def sbox(z0, y0, z1, y1, fill, label="", outline=INK, width=2, fsz=14, lfill=INK):
            p0, p1 = pt(z0, y1), pt(z1, y0)
            b = (p0[0], p0[1], p1[0], p1[1])
            draw.rectangle(b, fill=fill, outline=outline, width=width)
            if label:
                centered_text(draw, b, label, font=font(fsz), fill=lfill)

        sbox(-34, -5.0, 82, 0.0, (226, 220, 212), "", outline=None, width=0)
        # Ground road & LOC01 courtyard
        draw.line([pt(-34, 0), pt(82, 0)], fill=INK, width=3)
        draw.text((pt(-27.5, 2.7)[0], pt(-27.5, 2.7)[1]), "AP00\nXe Minh", font=font(13), fill=INK)
        draw.text((pt(-18.5, 2.7)[0], pt(-18.5, 2.7)[1]), "LOC01 Sân đêm\n(AP01)", font=font(13), fill=(26, 84, 46))

        # K-A Front (z=-8..8.1)
        sbox(-8, 0, 0, 3.6, C_KA_LOCK, "LOC02\nLễ tân", fsz=13)
        sbox(0, 0, 8.1, 3.6, (164, 204, 174), "LOC03 · P.101\n(3 cửa sổ Bắc)", fsz=13)
        sbox(-8, 4.0, 8.1, 8.0, C_KA, "K-A Tầng 2 Nam", fsz=13)
        sbox(-8, 8.0, 8.1, 11.7, C_KA, "K-A Tầng 3 Nam", fsz=13)
        draw.polygon([pt(-8, 11.7), pt(0, 13.6), pt(8.1, 11.7)], fill=(184, 178, 170), outline=INK)

        # Open 3-Story Inner Light Court (Sân sáng z=8.1..14.1)
        sbox(8.1, 0, 14.1, 11.7, LIGHT_COURT, "SÂN\nSÁNG\n(thông\ntrời)", outline=(36, 108, 128), fsz=13, lfill=(22, 74, 92))

        # K-A North Core (z=14.1..50)
        sbox(14.1, 0, 22.0, 3.6, (232, 202, 198), "LOC04\nCấp cứu", outline=(146, 40, 36), fsz=13)
        sbox(22.0, 0, 32.1, 4.0, (232, 214, 210), "S3↑ / S2↓", fsz=12)
        sbox(32.1, 0, 50.0, 4.0, C_KA, "K-A Bắc 1F", fsz=13)
        sbox(14.1, 4.0, 32.1, 8.0, C_KA, "K-A 2F Giữa", fsz=13)
        sbox(32.1, 4.0, 38.2, 7.6, (164, 204, 174), "LOC05", fsz=12)
        sbox(38.2, 4.0, 50.0, 7.6, (186, 212, 192), "LOC06 · G03", fsz=12)
        sbox(14.1, 8.0, 29.5, 11.7, C_KA, "K-A 3F Giữa", fsz=13)
        sbox(29.5, 8.0, 38.5, 11.3, (164, 204, 174), "LOC07\nKho", fsz=12)
        draw.polygon([pt(14.1, 11.7), pt(27.0, 13.6), pt(40.0, 11.7)], fill=(184, 178, 170), outline=INK)

        # B1 underneath (z=31.1..45.5)
        sbox(31.1, -3.4, 45.5, -0.3, (216, 224, 214), "B1 · LOC09 Kỹ thuật (-3.40m)", fsz=13)
        draw.line([pt(22.5, 0), pt(31.1, -3.4)], fill=(120, 40, 36), width=4)
        draw.line([pt(22.0, 0), pt(32.1, 4.0)], fill=(36, 112, 66), width=4)

        # K-F Logistics (z=59..73) & Water Tower Silhouette (+17.5m)
        sbox(59, 0, 73, 4.1, C_KF, "K-F Hậu cần", fsz=13)
        draw.polygon([pt(59, 4.1), pt(66, 5.8), pt(73, 4.1)], fill=(184, 178, 170), outline=INK)
        # Water tower silhouette (projected at z=66.5)
        draw.rectangle([pt(64.5, 13.0)[0], pt(64.5, 13.0)[1], pt(68.5, 0)[0], pt(68.5, 0)[1]], outline=MUTED, width=2)
        draw.rectangle([pt(62.5, 17.5)[0], pt(62.5, 17.5)[1], pt(70.5, 13.0)[0], pt(70.5, 13.0)[1]], fill=(208, 214, 220), outline=INK, width=2)
        draw.text((pt(71.5, 16.5)[0], pt(71.5, 16.5)[1]), "Đài nước\n+17.5m", font=font(13), fill=INK)

    render_section(
        "MẶT CẮT A–A' (TÂY → ĐÔNG qua Z ≈ 38–42m): K-E · Ram Nhà xác · B1 (LOC10/09/11/12) · K-A 3F · Hố E05b & Sân sau -1.90m",
        (48, 136, 2552, 816),
        -44.0, 58.0, -5.0, 18.5,
        sec_we,
    )
    render_section(
        "MẶT CẮT B–B' (NAM → BẮC qua X ≈ 4–10m): Cổng AP00 · Sân LOC01 · K-A 3F · Phòng 101 · Sân sáng · LOC04 · B1/2F/3F · K-F & Đài nước",
        (48, 846, 2552, 1520),
        -34.0, 82.0, -5.0, 18.5,
        sec_sn,
    )

    out = OUT_DIR / "campus-sections.png"
    img.save(out)
    print(f"Saved {out}")


if __name__ == "__main__":
    draw_master_plan()
    draw_floor_plans()
    draw_sections()
