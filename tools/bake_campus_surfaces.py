"""Campus Expansion PBR Surface & Authentic 1987-2012 Vietnamese Hospital Signage Generator.

Produces 100% original / CC0-derived PBR texture sets (1024x1024 Color, NormalGL, Mask[Metallic, AO, 0, Smoothness])
differentiating An Sinh Hospital's historical construction eras and R (2012 abandoned) vs B (2005 operational):
- ExteriorFacadeB: 1987 K-A ochre-cream limewash maintained in 2005 (pairs with existing ExteriorFacade R 2012)
- CampusStuccoR / CampusStuccoB: 1995-2004 cream-mint rendered masonry for K-B/K-C/K-D/K-E wings
- CorrugatedRoofR / CorrugatedRoofB: ribbed galvanized/painted corrugated iron roofs (oxidized R vs painted teal-blue B)
- ConcretePavingR / ConcretePavingB: hospital courtyard sidewalk concrete tiles (mossy wet R vs damp clean B)
- BrickInfill2004: mismatched 2004 red-brick + coarse grey cement mortar infill for B1 sealed door E05b
- Campus departmental enamel/backlit signs & 6-block wayfinding map (100% fictional An Sinh Hospital)
"""

import math
import sys
from pathlib import Path
from random import Random

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

sys.path.insert(0, str(Path(__file__).resolve().parent))
import bake_surfaces as BS

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Textures"
FONTS = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Resources" / "CTCC"
SZ = 1024


def build_campus_pbr_sets():
    plaster_disp = BS.luma(BS.source("PaintedPlaster017", "Displacement", size=SZ))
    plaster_norm = BS.source("PaintedPlaster017", "NormalGL", size=SZ)
    terrazzo_col = BS.luma(BS.source("Terrazzo013", "Color", size=SZ))
    terrazzo_disp = BS.luma(BS.source("Terrazzo013", "Displacement", size=SZ))
    metal_disp = BS.luma(BS.source("Metal032", "Displacement", size=SZ))
    metal_norm = BS.source("Metal032", "NormalGL", size=SZ)

    # 1. ExteriorFacadeB (1987 K-A main block ochre-cream maintained in 2005 Reality B)
    macro = BS.fbm(SZ, [4, 8, 16, 32], seed=8101)
    streaks = BS.fbm_aniso(SZ, [24, 48], [4, 8], seed=8102)
    damp_b = np.clip((streaks * 0.55 + (1.0 - macro) * 0.45 - 0.64) * 2.4, 0.0, 1.0)
    efb_col = np.full((SZ, SZ, 3), (0.78, 0.72, 0.54), dtype=np.float32) + BS.center(macro)[..., None] * 0.06 + BS.center(plaster_disp)[..., None] * 0.05
    efb_col = efb_col * (1.0 - damp_b[..., None] * 0.22) + np.array([0.46, 0.45, 0.36]) * (damp_b[..., None] * 0.22)
    efb_norm = BS.blend_normals(plaster_norm, BS.normal_from_height(plaster_disp * 0.45, 3.0), 0.42)
    efb_sm = np.clip(0.18 + damp_b * 0.06 + BS.center(macro) * 0.04, 0.08, 0.30)
    efb_ao = np.clip(BS.ao_from_height(plaster_disp, 6, 0.40), 0.55, 1.0)
    BS.map_set("ExteriorFacadeB", efb_col, efb_norm, efb_sm, efb_ao)

    # 2. CampusStuccoB & CampusStuccoR (1998-2004 additions K-B/K-C/K-D/K-E — cooler pale cream-sage)
    m2 = BS.fbm(SZ, [4, 8, 16, 32], seed=8201)
    s2 = BS.fbm_aniso(SZ, [20, 40, 80], [3, 6, 12], seed=8202)
    # 2005 B: crisp pale cream-mint institutional paint
    sb_col = np.full((SZ, SZ, 3), (0.76, 0.79, 0.72), dtype=np.float32) + BS.center(m2)[..., None] * 0.05 + BS.center(plaster_disp)[..., None] * 0.04
    sb_norm = BS.blend_normals(plaster_norm, BS.normal_from_height(plaster_disp * 0.4, 2.6), 0.38)
    sb_sm = np.clip(0.20 + BS.center(m2) * 0.04, 0.10, 0.32)
    sb_ao = np.clip(BS.ao_from_height(plaster_disp, 6, 0.38), 0.58, 1.0)
    BS.map_set("CampusStuccoB", sb_col, sb_norm, sb_sm, sb_ao)

    # 2012 R: rain-streaked tropical algae + chalking stucco
    algae = np.clip((s2 * 0.65 + (1.0 - m2) * 0.35 - 0.52) * 3.4, 0.0, 1.0)
    peel = np.clip((m2 - 0.69) * 8.0, 0.0, 1.0)
    sr_col = np.full((SZ, SZ, 3), (0.58, 0.61, 0.55), dtype=np.float32) + BS.center(m2)[..., None] * 0.08 + BS.center(plaster_disp)[..., None] * 0.06
    sr_col = sr_col * (1.0 - algae[..., None] * 0.52) + np.array([0.17, 0.22, 0.18]) * (algae[..., None] * 0.52)
    sr_col = sr_col * (1.0 - peel[..., None] * 0.65) + np.array([0.40, 0.41, 0.38]) * (peel[..., None] * 0.65)
    sr_h = plaster_disp * 0.65 - peel * 0.25 + s2 * 0.18
    sr_norm = BS.blend_normals(plaster_norm, BS.normal_from_height(sr_h, 4.0), 0.52)
    sr_sm = np.clip(0.15 + algae * 0.14 - peel * 0.06, 0.05, 0.36)
    sr_ao = np.clip(BS.ao_from_height(sr_h, 8, 0.55) - algae * 0.16, 0.38, 1.0)
    BS.map_set("CampusStuccoR", sr_col, sr_norm, sr_sm, sr_ao)

    # 3. CorrugatedRoofB & CorrugatedRoofR (Vietnamese ribbed steel sheet roofs — tôn sóng)
    yy, xx = np.mgrid[:SZ, :SZ]
    ribs = 0.5 + 0.5 * np.sin((xx / float(SZ)) * 28.0 * math.tau)
    ribs_sharp = np.clip((ribs - 0.15) / 0.70, 0.0, 1.0)
    seam_y = ((yy % 256) < 6).astype(np.float32)
    rust_field = BS.fbm(SZ, [4, 8, 16, 32], seed=8301)
    rust_streak = BS.fbm_aniso(SZ, [24, 48], [4, 8], seed=8302)
    rust_mask = np.clip((rust_field * 0.55 + rust_streak * 0.45 - 0.44) * 3.6, 0.0, 1.0)
    roof_h = ribs_sharp * 0.75 - seam_y * 0.22 + metal_disp * 0.15
    roof_norm = BS.blend_normals(metal_norm, BS.normal_from_height(roof_h, 7.5), 0.78)
    roof_ao = np.clip(0.68 + ribs_sharp * 0.32 - seam_y * 0.24, 0.36, 1.0)

    # B 2005: institutional blue-green painted corrugated iron (tôn xanh bệnh viện)
    crb_col = np.full((SZ, SZ, 3), (0.18, 0.36, 0.34), dtype=np.float32) + BS.center(rust_field)[..., None] * 0.04
    crb_col = crb_col * (0.88 + 0.16 * ribs_sharp[..., None])
    crb_sm = np.clip(0.46 + ribs_sharp * 0.12, 0.24, 0.66)
    BS.map_set("CorrugatedRoofB", crb_col, roof_norm, crb_sm, roof_ao, metal=0.42)

    # R 2012: wet oxidized corrugated iron with rust streaks along valleys and seams
    crr_col = np.full((SZ, SZ, 3), (0.20, 0.29, 0.27), dtype=np.float32) + BS.center(rust_field)[..., None] * 0.06
    rust_rgb = np.array([0.38, 0.19, 0.11], dtype=np.float32) + BS.center(rust_streak)[..., None] * 0.06
    crr_col = crr_col * (1.0 - rust_mask[..., None] * 0.78) + rust_rgb * (rust_mask[..., None] * 0.78)
    crr_col = crr_col * (0.84 + 0.20 * ribs_sharp[..., None])
    crr_sm = np.clip(0.54 - rust_mask * 0.32 + (1.0 - ribs_sharp) * 0.14, 0.12, 0.76)
    crr_metal = np.clip(0.48 - rust_mask * 0.36, 0.06, 0.52)
    BS.map_set("CorrugatedRoofR", crr_col, roof_norm, crr_sm, roof_ao, metal=crr_metal)

    # 4. ConcretePavingB & ConcretePavingR (40x40 cm concrete sidewalk & courtyard pavers — gạch block sân bệnh viện)
    grid_n = 8
    step = SZ // grid_n
    gx = (xx % step) / float(step)
    gy = (yy % step) / float(step)
    grout = ((gx < 0.035) | (gx > 0.965) | (gy < 0.035) | (gy > 0.965)).astype(np.float32)
    grout = np.asarray(Image.fromarray(np.uint8(grout * 255)).filter(ImageFilter.GaussianBlur(1.2)), dtype=np.float32) / 255.0
    rng_p = BS.rng(8401)
    tile_var = rng_p.uniform(-0.045, 0.045, (grid_n, grid_n)).astype(np.float32)[(yy // step) % grid_n, (xx // step) % grid_n]
    p_noise = BS.fbm(SZ, [6, 12, 24, 48], seed=8402)
    p_puddle = BS.fbm(SZ, [3, 6, 12], seed=8403)
    p_moss = np.clip((grout * 0.8 + (p_noise - 0.45) * 0.65 - 0.35) * 2.8, 0.0, 1.0)
    p_wet = np.clip((p_puddle - 0.50) * 4.5, 0.0, 1.0)
    p_h = 0.75 - grout * 0.45 + terrazzo_disp * 0.20
    p_norm = BS.normal_from_height(p_h * (1.0 - p_wet * 0.75), 5.2)
    p_ao = np.clip(0.95 - grout * 0.48, 0.40, 1.0)

    cpb_col = np.full((SZ, SZ, 3), (0.50, 0.51, 0.48), dtype=np.float32) + tile_var[..., None] + BS.center(terrazzo_col)[..., None] * 0.08
    cpb_col = cpb_col * (1.0 - grout[..., None] * 0.38)
    BS.map_set("ConcretePavingB", cpb_col, BS.normal_from_height(p_h, 4.6), np.clip(0.32 + BS.center(p_noise) * 0.08, 0.16, 0.48), p_ao)

    cpr_col = np.full((SZ, SZ, 3), (0.34, 0.36, 0.34), dtype=np.float32) + tile_var[..., None] + BS.center(terrazzo_col)[..., None] * 0.07
    cpr_col = cpr_col * (1.0 - p_moss[..., None] * 0.55) + np.array([0.13, 0.19, 0.12]) * (p_moss[..., None] * 0.55)
    cpr_col = cpr_col * (1.0 - p_wet[..., None] * 0.35)
    cpr_sm = np.clip(0.28 + p_wet * 0.56 - p_moss * 0.12, 0.12, 0.88)
    BS.map_set("ConcretePavingR", cpr_col, p_norm, cpr_sm, p_ao)

    # 5. BrickInfill2004 (distinctive newer 2004 sloppy red-brick + thick grey cement mortar for E05b evidence!)
    rows, cols = 18, 9
    rh, cw = SZ // rows, SZ // cols
    r_idx = yy // rh
    x_sh = (xx + (r_idx % 2) * (cw // 2)) % SZ
    c_idx = x_sh // cw
    u = (x_sh % cw) / float(cw)
    v = (yy % rh) / float(rh)
    # Thicker, sloppier rushed 2004 mortar joints compared to 1987 brickwork!
    mortar_jit = (BS.fbm(SZ, [12, 24, 48], seed=8501) - 0.5) * 0.04
    m_mask = ((u < 0.085 + mortar_jit) | (u > 0.915 + mortar_jit) | (v < 0.14 + mortar_jit) | (v > 0.86 + mortar_jit)).astype(np.float32)
    m_mask = np.asarray(Image.fromarray(np.uint8(m_mask * 255)).filter(ImageFilter.GaussianBlur(1.4)), dtype=np.float32) / 255.0
    smear = np.clip((BS.fbm(SZ, [6, 12, 24], seed=8502) - 0.56) * 3.8, 0.0, 1.0)
    r_b = BS.rng(8503)
    bv = r_b.uniform(-0.08, 0.08, (rows, cols)).astype(np.float32)[r_idx % rows, c_idx % cols]
    bn = BS.fbm(SZ, [16, 32, 64], seed=8504)
    brick_c = np.full((SZ, SZ, 3), (0.52, 0.25, 0.17), dtype=np.float32) + bv[..., None] + BS.center(bn)[..., None] * 0.08
    mortar_c = np.full((SZ, SZ, 3), (0.46, 0.47, 0.44), dtype=np.float32) + BS.center(bn)[..., None] * 0.08
    bi_col = brick_c * (1.0 - m_mask[..., None]) + mortar_c * m_mask[..., None]
    bi_col = bi_col * (1.0 - smear[..., None] * 0.55) + mortar_c * (smear[..., None] * 0.55)
    bi_h = 0.72 - m_mask * 0.35 + smear * 0.22 + BS.center(bn) * 0.18
    BS.map_set("BrickInfill2004", bi_col, BS.normal_from_height(bi_h, 6.8), np.clip(0.16 + smear * 0.05, 0.06, 0.28),
               np.clip(0.96 - m_mask * 0.38, 0.44, 1.0))

    print("[PBR] Generated 9 campus PBR surface sets in", DEST)


def _font(size, bold=True):
    path = FONTS / ("BeVietnamPro-SemiBold.ttf" if bold else "BeVietnamPro-Regular.ttf")
    return ImageFont.truetype(str(path), size)


def _weather_sign(img, seed, strength=1.0):
    rng = Random(seed)
    draw = ImageDraw.Draw(img)
    w, h = img.size
    for _ in range(int(520 * strength)):
        x = rng.randrange(w)
        y = rng.choice((rng.randrange(max(4, h // 9)), rng.randrange(h - max(4, h // 9), h), rng.randrange(h)))
        ln = rng.randrange(3, 18)
        col = rng.choice(("#7e7459", "#5c5340", "#91886d", "#453f33"))
        draw.line((x, y, min(w - 1, x + ln), y), fill=col, width=1)
    # Corner mounting screw rust drips
    for sx in (int(w * 0.045), int(w * 0.955)):
        for sy in (int(h * 0.14), int(h * 0.86)):
            draw.ellipse((sx - 7, sy - 7, sx + 7, sy + 7), fill="#4a4031", outline="#2a241b", width=2)
            draw.line((sx, sy + 7, sx, min(h - 2, sy + int(22 * strength))), fill="#6d4329", width=3)
    return img


def make_banner_sign(name, title, subtitle, bg="#194442", fg="#efede0", accent="#c9a554", size=(1024, 256), seed=9001, weather=0.8):
    img = Image.new("RGB", size, bg)
    draw = ImageDraw.Draw(img)
    w, h = size
    draw.rectangle((10, 10, w - 11, h - 11), outline=accent, width=6)
    draw.rectangle((22, 22, w - 23, h - 23), outline=fg, width=2)
    f_title = _font(int(h * 0.30), bold=True)
    f_sub = _font(int(h * 0.145), bold=False)
    if subtitle:
        draw.text((w // 2, int(h * 0.39)), title, font=f_title, fill=fg, anchor="mm")
        draw.line((int(w * 0.12), int(h * 0.62), int(w * 0.88), int(h * 0.62)), fill=accent, width=3)
        draw.text((w // 2, int(h * 0.78)), subtitle, font=f_sub, fill=fg, anchor="mm")
    else:
        draw.text((w // 2, h // 2), title, font=f_title, fill=fg, anchor="mm")
    if weather > 0:
        _weather_sign(img, seed, strength=weather)
    img.save(DEST / f"{name}.png", optimize=True)


def make_emergency_roof_sign():
    w, h = 1024, 320
    img = Image.new("RGB", (w, h), "#b81d18")
    draw = ImageDraw.Draw(img)
    draw.rectangle((10, 10, w - 11, h - 11), outline="#f4f2ea", width=10)
    # Red cross inside white square on left
    draw.rectangle((48, 52, 224, 228), fill="#f4f2ea")
    draw.rectangle((114, 74, 158, 206), fill="#b81d18")
    draw.rectangle((70, 118, 202, 162), fill="#b81d18")
    draw.text((604, 126), "CẤP CỨU", font=_font(128, True), fill="#ffffff", anchor="mm")
    draw.text((604, 240), "EMERGENCY · TIẾP NHẬN 24/24", font=_font(36, True), fill="#ffe4d6", anchor="mm")
    _weather_sign(img, 9050, strength=0.45)
    img.save(DEST / "Sign_KD_Emergency.png", optimize=True)


def make_campus_wayfinding_map():
    """Original fictional 6-block An Sinh Hospital courtyard directory map board (1024x768)."""
    w, h = 1024, 768
    img = Image.new("RGB", (w, h), "#d9d6c2")
    draw = ImageDraw.Draw(img)
    draw.rectangle((14, 14, w - 15, h - 15), outline="#1f4240", width=10)
    draw.rectangle((14, 14, w - 15, 116), fill="#1c4441")
    draw.text((512, 54), "SƠ ĐỒ TỔNG MẶT BẰNG · BỆNH VIỆN AN SINH", font=_font(36, True), fill="#f2efe1", anchor="mm")
    draw.text((512, 94), "THÀNH LẬP 1987 · QUY HOẠCH KHUÔN VIÊN 6 KHỐI CHỨC NĂNG", font=_font(20, False), fill="#c8d8cf", anchor="mm")

    # Coordinate mapper: X in [-42..58] -> px [60..670], Z in [-30..80] -> py [700..155] (North up)
    def mp(x, z):
        px = 65 + (x + 42.0) / 100.0 * 600.0
        py = 700 - (z + 30.0) / 110.0 * 535.0
        return (int(px), int(py))

    def draw_rect(x0, z0, x1, z1, fill, outline="#213533", width=3):
        p0 = mp(x0, z1)
        p1 = mp(x1, z0)
        draw.rectangle((p0[0], p0[1], p1[0], p1[1]), fill=fill, outline=outline, width=width)

    # Site boundary & roads
    draw_rect(-42, -30, 58, 80, "#c8ccb8", "#2d3f3c", 4)
    draw_rect(-34, -30, -26, 40, "#a7ada3", "#888e84", 1)  # Public road
    draw_rect(49, -30, 57, 45, "#b7a19a", "#8a6d65", 1)    # Ambulance road
    draw_rect(10, 56, 18, 80, "#adaea8", "#7e807a", 1)     # Service road
    draw_rect(-5, -20, 13, -8, "#b7bcb2", "#68706a", 2)    # Night courtyard LOC01

    # 6 Blocks
    draw_rect(-15, -8, 18, 50, "#e3cfa0", "#554524", 4)    # K-A Main 3F
    draw_rect(5.6, 8.1, 14.2, 14.1, "#9eb89e", "#3c5b3e", 2)  # Inner Light Court
    draw_rect(-38, -8, -22, 28, "#b4d0c7", "#2c544b", 4)   # K-B Outpatient 2F
    draw_rect(22, 45, 48, 67, "#b6c8d2", "#2c4657", 4)     # K-C Inpatient 3F
    draw_rect(18, 14, 43, 32, "#dfb5ad", "#772f29", 4)     # K-D Emergency 1F
    draw_rect(43, 16, 49, 29, "#c97468", "#772f29", 3)     # Ambulance canopy
    draw_rect(-40, 41, -26, 63, "#ccc6b4", "#514a3b", 4)   # K-E Admin 2F
    draw_rect(-14, 59, 10, 73, "#b9bab3", "#4c4d48", 4)    # K-F Logistics 1F
    draw_rect(-22, 3, -15, 7, "#8ea49c", "#2c544b", 2)     # Link K-B to K-A
    draw_rect(10, 46, 22, 50, "#8ea49c", "#2c4657", 2)     # Link K-A to K-C

    f_blk = _font(17, True)
    for label, cx, cz in (
        ("NHÀ CHÍNH\n3 TẦNG + B1", 1, 24),
        ("KHÁM BỆNH\n2 TẦNG", -30, 10),
        ("NỘI TRÚ\n3 TẦNG", 35, 56),
        ("CẤP CỨU", 30, 23),
        ("HÀNH\nCHÍNH", -33, 52),
        ("HẬU CẦN", -2, 66),
    ):
        draw.multiline_text(mp(cx, cz), label, font=f_blk, fill="#1a2626", anchor="mm", align="center")

    # You are here dot at Night Gate
    yh = mp(4, -19)
    draw.ellipse((yh[0] - 9, yh[1] - 9, yh[0] + 9, yh[1] + 9), fill="#b9221d", outline="#ffffff", width=3)

    # Legend column on right
    lx = 690
    draw.rectangle((lx - 12, 155, 996, 700), fill="#e6e2d1", outline="#324744", width=3)
    draw.text((lx + 144, 185), "CHÚ DẪN KHU VỰC", font=_font(22, True), fill="#1b3a38", anchor="mm")
    legend = [
        ("#e3cfa0", "Nhà chính (1987)\nTrực đêm, P.101, Khu B, B1"),
        ("#b4d0c7", "Khoa Khám bệnh\nNgoại trú · Cổng chính"),
        ("#dfb5ad", "Khoa Cấp cứu\nMái đón xe · Lối đông"),
        ("#b6c8d2", "Khối Nội trú mới\n3 tầng · Cầu nối K-A"),
        ("#ccc6b4", "Khối Hành chính\nQuản trị · Lưu trữ"),
        ("#b9bab3", "Khu Hậu cần – Kỹ thuật\nCổng bắc · Nhà đại thể"),
        ("#b9221d", "Vị trí hiện tại\nCổng phụ trực đêm"),
    ]
    for i, (col, text) in enumerate(legend):
        y = 224 + i * 66
        draw.rectangle((lx, y + 4, lx + 22, y + 26), fill=col, outline="#263230", width=2)
        draw.multiline_text((lx + 32, y + 16), text, font=_font(15, False), fill="#1f2c2b", anchor="lm", spacing=3)

    _weather_sign(img, 9099, strength=0.75)
    img.save(DEST / "Sign_CampusMap.png", optimize=True)


def build_campus_signs():
    make_banner_sign("Sign_MainArch", "BỆNH VIỆN ĐA KHOA AN SINH", "THÀNH LẬP 1987  ·  VÌ SỨC KHỎE CỘNG ĐỒNG",
                     bg="#1a4340", fg="#f0edd8", accent="#c9a554", size=(1024, 256), seed=9001, weather=0.65)
    make_banner_sign("Sign_KB_Outpatient", "KHOA KHÁM BỆNH", "ĐĂNG KÝ KHÁM  ·  NỘI – NGOẠI – NHI  ·  NHÀ THUỐC",
                     bg="#1d4c49", fg="#eff2ea", accent="#98b8ab", size=(1024, 256), seed=9002, weather=0.6)
    make_banner_sign("Sign_KC_Inpatient", "KHỐI ĐIỀU TRỊ NỘI TRÚ", "TẦNG 1 – 3  ·  BUỒNG BỆNH & TRẠM ĐIỀU DƯỠNG",
                     bg="#204056", fg="#eef1ee", accent="#9cb4c3", size=(1024, 256), seed=9003, weather=0.55)
    make_emergency_roof_sign()
    make_banner_sign("Sign_KE_Admin", "KHỐI HÀNH CHÍNH – QUẢN TRỊ", "TÀI CHÍNH  ·  KẾ HOẠCH TỔNG HỢP  ·  HỘI TRƯỜNG",
                     bg="#2e403c", fg="#eae7d6", accent="#b5aa83", size=(1024, 256), seed=9004, weather=0.7)
    make_banner_sign("Sign_KF_Service", "KHU HẬU CẦN – KỸ THUẬT", "KHO VẬT TƯ  ·  GIẶT LÀ  ·  CẤM NGƯỜI KHÔNG NHIỆM VỤ",
                     bg="#3d3c34", fg="#e6dfc6", accent="#b86c36", size=(1024, 256), seed=9005, weather=0.85)
    make_banner_sign("Sign_NightGate", "CỔNG PHỤ TRỰC ĐÊM", "TIẾP NHẬN NGOÀI GIỜ  ·  18:00 – 06:00",
                     bg="#1e3d3b", fg="#ede9d6", accent="#b89f5c", size=(768, 224), seed=9006, weather=0.8)
    make_banner_sign("Sign_AmbulanceGate", "LỐI XE CẤP CỨU", "KHÔNG ĐỖ XE TRƯỚC CỔNG  ·  ƯU TIÊN 24/24",
                     bg="#9e201b", fg="#ffffff", accent="#f2d56b", size=(768, 224), seed=9007, weather=0.6)
    make_banner_sign("Sign_MorgueService", "NHÀ ĐẠI THỂ", "LỐI DỊCH VỤ RIÊNG  ·  KHU VỰC HẠN CHẾ",
                     bg="#333837", fg="#dad6c5", accent="#8d8874", size=(768, 224), seed=9008, weather=0.9)
    make_banner_sign("Sign_Pharmacy", "NHÀ THUỐC BỆNH VIỆN", "CẤP PHÁT THEO ĐƠN",
                     bg="#1d4d44", fg="#f0f2e8", accent="#9bc1b1", size=(512, 180), seed=9009, weather=0.55)
    make_banner_sign("Sign_XRay", "CHẨN ĐOÁN HÌNH ẢNH", "X-QUANG · SIÊU ÂM",
                     bg="#293d4d", fg="#eff1ec", accent="#c9a554", size=(512, 180), seed=9010, weather=0.6)
    make_banner_sign("Sign_Ward102", "PHÒNG 102", "ĐIỀU TRỊ NỘI TRÚ",
                     bg="#d2d0b6", fg="#1d3332", accent="#2a4745", size=(512, 180), seed=9011, weather=0.65)
    make_banner_sign("Sign_Ward201", "PHÒNG 201 · KHU B", "TẦNG 2",
                     bg="#d2d0b6", fg="#1d3332", accent="#2a4745", size=(512, 180), seed=9012, weather=0.65)
    make_banner_sign("Sign_Ward202", "PHÒNG 202 · KHU B", "TẦNG 2",
                     bg="#d2d0b6", fg="#1d3332", accent="#2a4745", size=(512, 180), seed=9013, weather=0.65)
    make_campus_wayfinding_map()
    print("[SIGNS] Generated 15 authentic An Sinh campus signage & directory textures in", DEST)


if __name__ == "__main__":
    build_campus_pbr_sets()
    build_campus_signs()
