"""M2 High-Fidelity PBR Surface, Exterior Architecture, ER Equipment, Signage & Character Texture Generator.

Extends M0-R surfaces (tools/bake_surfaces.py) with:
- Refined PlasterR / DadoR (eliminating procedural contour squiggles -> realistic tropical monsoon limewash weathering)
- Refined fibrous KraftPaper PBR set (for crisp 3D physical inspection of Bảy's envelope)
- LOC01 Exterior PBR sets: WetAsphalt (rain puddles + gritty aggregate), ExteriorFacade (1987 ochre-cream limewash),
  WeatheredBrick, TerrazzoBench (stone-chip đá mài), TropicalShrub (elephant-ear / fern atlas), MotorbikePaint
- LOC02 & LOC04 Signage / Equipment maps: Sign_EmergencyBox, Sign_KhuBClosed, Sign_ReceptionWindow,
  Sign_HospitalGate, Calendar2005, MonitorECG, LanBadge
- Character PBR maps: NurseBlouse (Lan's 2005 off-white áo blouse), LanSkin, LanHair, CharredBlouse (Gatekeeper),
  AnCotton (Bé An's striped hospital shirt)

All generated textures are 100% original or derived from CC0 1.0 ambientCG base micro-maps in output/material-staging/source.
"""

import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parent))
import bake_surfaces as BS

DEST = BS.DEST
SIZE = BS.SIZE


def build_refined_walls():
    """Replace M0-R contour-loop cracks with authentic tropical Vietnamese limewash weathering,
    organic damp plaster discoloration, delicate branching shrinkage hairlines, and subtle salt bloom."""
    plaster_col = BS.luma(BS.source("PaintedPlaster017", "Color"))
    plaster_disp = BS.luma(BS.source("PaintedPlaster017", "Displacement"))
    plaster_norm = BS.source("PaintedPlaster017", "NormalGL")
    plaster_rough = BS.luma(BS.source("PaintedPlaster017", "Roughness"))

    macro = BS.fbm(SIZE, [3, 6, 12, 24], seed=7101)
    mid = BS.fbm(SIZE, [16, 32, 64, 128], seed=7102)
    fine = BS.fbm(SIZE, [64, 128, 256], seed=7103)
    vertical_wash = BS.fbm_aniso(SIZE, [12, 24, 48], [3, 6, 12], seed=7104)
    roller = BS.fbm_aniso(SIZE, [24, 48], [6, 12], seed=7105)

    # Sparse, natural hairline vertical/diagonal settlement fractures (never continuous loops!)
    crack_img = Image.new("L", (SIZE, SIZE), 0)
    dc = ImageDraw.Draw(crack_img)
    r = BS.rng(7109)
    for _ in range(14):
        cx = float(r.uniform(80, SIZE - 80))
        cy = float(r.uniform(80, SIZE - 80))
        ang = float(r.uniform(0.3, math.pi - 0.3))
        steps = int(r.integers(14, 34))
        pts = [(cx, cy)]
        for _s in range(steps):
            ang += float(r.uniform(-0.35, 0.35))
            step_l = float(r.uniform(6.0, 16.0))
            cx = (cx + math.cos(ang) * step_l) % SIZE
            cy = (cy + math.sin(ang) * step_l) % SIZE
            if abs(cx - pts[-1][0]) < 64 and abs(cy - pts[-1][1]) < 64:
                pts.append((cx, cy))
            else:
                break
        if len(pts) > 2:
            dc.line(pts, fill=int(r.integers(90, 170)), width=int(r.integers(1, 3)))
    cracks = np.asarray(crack_img.filter(ImageFilter.GaussianBlur(0.8)), dtype=np.float32) / 255.0

    # Organic limewash peeling islands (soft blended, authentic aged lime paint)
    flake_base = macro * 0.62 + mid * 0.38
    flake_mask = np.clip((flake_base - 0.68) * 8.0, 0.0, 1.0)
    mortar_mask = np.clip((flake_base - 0.76) * 11.0, 0.0, 1.0)
    flake_rim = np.clip(flake_mask * (1.0 - flake_mask) * 3.8, 0.0, 1.0)

    mildew = np.clip((1.0 - macro * 0.68 - vertical_wash * 0.32 - 0.44) * 3.2, 0.0, 1.0) * (0.50 + 0.50 * fine)
    trowel_detail = BS.center(plaster_col) * 0.24 + BS.center(plaster_disp) * 0.12
    trowel_ao = BS.ao_from_height(plaster_disp, radius=8, strength=0.40)

    # PlasterR: slightly brighter base albedo (0.54, 0.56, 0.50) so indirect lighting reads cleanly without crushing black!
    pr_col = np.full((SIZE, SIZE, 3), (0.54, 0.56, 0.50), dtype=np.float32)
    pr_col += trowel_detail[..., None] * 0.85 + BS.center(macro)[..., None] * 0.08 + BS.center(vertical_wash)[..., None] * 0.06
    undercoat = np.array([0.55, 0.51, 0.39], dtype=np.float32) + BS.center(fine)[..., None] * 0.07
    raw_mortar = np.array([0.37, 0.38, 0.36], dtype=np.float32) + BS.center(fine)[..., None] * 0.10
    pr_col = pr_col * (1.0 - flake_mask[..., None]) + undercoat * flake_mask[..., None]
    pr_col = pr_col * (1.0 - mortar_mask[..., None]) + raw_mortar * mortar_mask[..., None]
    pr_col = pr_col * (1.0 - mildew[..., None] * 0.32) + np.array([0.19, 0.24, 0.20]) * (mildew[..., None] * 0.32)
    pr_col -= cracks[..., None] * 0.11
    pr_col += flake_rim[..., None] * 0.045
    pr_height = plaster_disp - flake_mask * 0.22 - mortar_mask * 0.18 - cracks * 0.25 + flake_rim * 0.06
    pr_norm = BS.blend_normals(plaster_norm, BS.normal_from_height(pr_height, 4.2), 0.60)
    pr_smooth = np.clip(0.14 + (1.0 - plaster_rough) * 0.08 - flake_mask * 0.05 - mortar_mask * 0.04 + mildew * 0.07, 0.05, 0.26)
    pr_ao = np.clip(BS.ao_from_height(pr_height, radius=10, strength=0.62) - cracks * 0.15 - mortar_mask * 0.10, 0.42, 1.0)
    BS.map_set("PlasterR", pr_col, pr_norm, pr_smooth, pr_ao)

    # DadoR: scuffed hospital teal oil-paint dado without squiggle cracks
    dr_col = np.full((SIZE, SIZE, 3), (0.24, 0.35, 0.31), dtype=np.float32)
    dr_col += trowel_detail[..., None] * 0.50 + BS.center(macro)[..., None] * 0.07 + BS.center(vertical_wash)[..., None] * 0.05
    scuff = np.clip((BS.fbm_aniso(SIZE, [12, 24], [32, 64], seed=7106) - 0.62) * 3.6, 0.0, 1.0)
    dr_col = dr_col * (1.0 - flake_mask[..., None] * 0.72) + raw_mortar * (flake_mask[..., None] * 0.72)
    dr_col = dr_col * (1.0 - mildew[..., None] * 0.35) + np.array([0.12, 0.17, 0.14]) * (mildew[..., None] * 0.35)
    dr_col += scuff[..., None] * 0.05
    dr_height = plaster_disp - flake_mask * 0.24 - cracks * 0.16
    dr_norm = BS.blend_normals(plaster_norm, BS.normal_from_height(dr_height, 3.8), 0.56)
    dr_smooth = np.clip(0.34 + (1.0 - plaster_rough) * 0.14 - flake_mask * 0.20 - scuff * 0.10 + mildew * 0.06, 0.10, 0.52)
    dr_ao = np.clip(BS.ao_from_height(dr_height, radius=10, strength=0.58) - cracks * 0.12, 0.45, 1.0)
    BS.map_set("DadoR", dr_col, dr_norm, dr_smooth, dr_ao)

    # Refined KraftPaper (1024x1024) for Bảy's envelope — tactile matte cellulose fibers, zero noisy plastic bumps!
    ks = 1024
    k_macro = BS.fbm(ks, [4, 8, 16], seed=7120)
    k_fib = BS.fbm_aniso(ks, [64, 128, 256], [16, 32, 64], seed=7121)
    k_speck = BS.fbm(ks, [128, 256], seed=7122)
    k_col = np.full((ks, ks, 3), (0.64, 0.53, 0.36), dtype=np.float32)
    k_col += BS.center(k_macro)[..., None] * 0.07 + BS.center(k_fib)[..., None] * 0.045 - (k_speck > 0.74)[..., None] * 0.045
    k_norm = BS.normal_from_height(k_fib * 0.35 + k_macro * 0.15, 1.4)
    k_sm = np.clip(0.08 + BS.center(k_fib) * 0.03, 0.04, 0.14)
    k_ao = np.clip(0.92 + BS.center(k_fib) * 0.08, 0.80, 1.0)
    BS.map_set("KraftPaper", k_col, k_norm, k_sm, k_ao)


def build_exterior_materials():
    """LOC01 Exterior monsoon materials: WetAsphalt, ExteriorFacade, WeatheredBrick, TerrazzoBench, MotorbikePaint."""
    terrazzo_col = BS.luma(BS.source("Terrazzo013", "Color"))
    terrazzo_disp = BS.luma(BS.source("Terrazzo013", "Displacement"))
    terrazzo_norm = BS.source("Terrazzo013", "NormalGL")
    plaster_disp = BS.luma(BS.source("PaintedPlaster017", "Displacement"))
    plaster_norm = BS.source("PaintedPlaster017", "NormalGL")

    # 1. WetAsphalt (2048x2048): Rain-slicked Vietnamese hospital courtyard concrete/asphalt with glossy puddle reflections!
    puddle = BS.fbm(SIZE, [3, 6, 12, 24], seed=7201)
    grit = BS.fbm(SIZE, [48, 96, 192, 384], seed=7202)
    moss = np.clip((BS.fbm(SIZE, [8, 16, 32], seed=7203) - 0.62) * 3.8, 0.0, 1.0)
    wet_mask = np.clip((puddle - 0.48) * 5.2, 0.0, 1.0)
    damp_col = np.full((SIZE, SIZE, 3), (0.20, 0.21, 0.21), dtype=np.float32) + BS.center(grit)[..., None] * 0.10 + BS.center(terrazzo_col)[..., None] * 0.08
    puddle_col = damp_col * 0.55 + np.array([0.02, 0.03, 0.038], dtype=np.float32)
    asph_col = damp_col * (1.0 - wet_mask[..., None]) + puddle_col * wet_mask[..., None]
    asph_col = asph_col * (1.0 - moss[..., None] * 0.40) + np.array([0.11, 0.16, 0.10]) * (moss[..., None] * 0.40)
    # Puddles flatten micro-normal so specular reflections mirror canopy light & sky!
    asph_h = (grit * 0.55 + terrazzo_disp * 0.45) * (1.0 - wet_mask * 0.92)
    asph_norm = BS.normal_from_height(asph_h, 5.0)
    asph_sm = np.clip(0.30 + BS.center(grit) * 0.08 + wet_mask * 0.60 - moss * 0.15, 0.12, 0.94)
    asph_ao = np.clip(BS.ao_from_height(grit, 6, 0.55) * (1.0 - wet_mask * 0.12), 0.35, 1.0)
    BS.map_set("WetAsphalt", asph_col, asph_norm, asph_sm, asph_ao)

    # 2. ExteriorFacade (2048x2048): 1987 Vietnamese provincial hospital ochre-cream weathered masonry limewash
    macro = BS.fbm(SIZE, [4, 8, 16, 32], seed=7210)
    streaks = BS.fbm_aniso(SIZE, [24, 48, 96], [3, 6, 12], seed=7211)
    moss_run = np.clip((streaks * 0.65 + (1.0 - macro) * 0.35 - 0.54) * 3.8, 0.0, 1.0)
    ef_col = np.full((SIZE, SIZE, 3), (0.64, 0.59, 0.44), dtype=np.float32) + BS.center(macro)[..., None] * 0.10 + BS.center(plaster_disp)[..., None] * 0.08
    ef_col = ef_col * (1.0 - moss_run[..., None] * 0.55) + np.array([0.16, 0.19, 0.15]) * (moss_run[..., None] * 0.55)
    ef_norm = BS.blend_normals(plaster_norm, BS.normal_from_height(plaster_disp * 0.6 + streaks * 0.25, 4.2), 0.55)
    ef_sm = np.clip(0.16 + moss_run * 0.12 + BS.center(macro) * 0.06, 0.06, 0.36)
    ef_ao = np.clip(BS.ao_from_height(plaster_disp, 8, 0.55) - moss_run * 0.18, 0.38, 1.0)
    BS.map_set("ExteriorFacade", ef_col, ef_norm, ef_sm, ef_ao)

    # 3. WeatheredBrick (1024x1024): Southern Vietnamese perimeter wall & gatehouse plinth brickwork
    bsz = 1024
    yy, xx = np.mgrid[:bsz, :bsz]
    rows, cols = 16, 8
    rh, cw = bsz // rows, bsz // cols
    row_idx = yy // rh
    x_shift = (xx + (row_idx % 2) * (cw // 2)) % bsz
    col_idx = x_shift // cw
    u = (x_shift % cw) / float(cw)
    v = (yy % rh) / float(rh)
    mortar = ((u < 0.055) | (u > 0.945) | (v < 0.10) | (v > 0.90)).astype(np.float32)
    mortar = np.asarray(Image.fromarray(np.uint8(mortar * 255)).filter(ImageFilter.GaussianBlur(1.1)), dtype=np.float32) / 255.0
    r = BS.rng(7220)
    b_var = r.uniform(-0.07, 0.07, (rows, cols)).astype(np.float32)[row_idx % rows, col_idx % cols]
    b_noise = BS.fbm(bsz, [12, 24, 48, 96], seed=7221)
    b_moss = np.clip((BS.fbm(bsz, [6, 12, 24], seed=7222) - 0.58) * 3.5, 0.0, 1.0)
    brick_rgb = np.full((bsz, bsz, 3), (0.43, 0.23, 0.16), dtype=np.float32) + b_var[..., None] + BS.center(b_noise)[..., None] * 0.10
    mortar_rgb = np.full((bsz, bsz, 3), (0.36, 0.35, 0.31), dtype=np.float32) + BS.center(b_noise)[..., None] * 0.07
    wb_col = brick_rgb * (1.0 - mortar[..., None]) + mortar_rgb * mortar[..., None]
    wb_col = wb_col * (1.0 - b_moss[..., None] * 0.45) + np.array([0.13, 0.18, 0.11]) * (b_moss[..., None] * 0.45)
    wb_h = 0.80 - mortar * 0.48 + BS.center(b_noise) * 0.20
    BS.map_set("WeatheredBrick", wb_col, BS.normal_from_height(wb_h, 6.2), np.clip(0.18 - mortar * 0.08 + b_moss * 0.08, 0.06, 0.34),
               np.clip(1.0 - mortar * 0.45, 0.40, 1.0))

    # 4. TerrazzoBench (1024x1024): Classic Vietnamese speckled stone-chip ghế đá mài
    tsz = 1024
    base_tz = BS.source("Terrazzo013", "Color", size=tsz)
    tz_n = BS.source("Terrazzo013", "NormalGL", size=tsz)
    tz_r = BS.luma(BS.source("Terrazzo013", "Roughness", size=tsz))
    tz_wear = BS.fbm(tsz, [6, 12, 24], seed=7230)
    tz_col = np.clip(base_tz * 0.62 + np.array([0.24, 0.26, 0.24]) + BS.center(tz_wear)[..., None] * 0.08, 0.0, 1.0)
    tz_sm = np.clip(0.56 + BS.center(1.0 - tz_r) * 0.18 + BS.center(tz_wear) * 0.12, 0.28, 0.78)
    BS.map_set("TerrazzoBench", tz_col, tz_n, tz_sm, np.clip(0.88 - tz_wear * 0.14, 0.60, 1.0))

    # 5. TropicalShrub (1024x1024 RGBA): Broad veined elephant-ear (cây ráy) & wet monsoon ferns
    ssz = 1024
    img = Image.new("RGBA", (ssz, ssz), (20, 38, 18, 0))
    h_img = Image.new("L", (ssz, ssz), 0)
    d = ImageDraw.Draw(img)
    dh = ImageDraw.Draw(h_img)
    # 5 lush heart-shaped elephant-ear fronds fanning upward from bottom center
    fronds = [
        (512, 940, -0.58, 540, 290, 0.88),
        (512, 940, 0.56, 530, 285, 0.92),
        (512, 950, -0.26, 640, 320, 1.02),
        (512, 950, 0.24, 620, 310, 0.96),
        (512, 960, 0.0, 710, 345, 1.08),
    ]
    for ox, oy, ang, length, width, shade in fronds:
        ca, sa = math.cos(ang), math.sin(ang)
        # Stalk
        sx1, sy1 = ox + sa * (length * 0.36), oy - ca * (length * 0.36)
        d.line([(ox, oy), (sx1, sy1)], fill=(int(38 * shade), int(62 * shade), int(28 * shade), 255), width=14)
        dh.line([(ox, oy), (sx1, sy1)], fill=180, width=14)
        # Heart-arrow leaf blade
        pts = []
        for k in range(32):
            t = k / 32.0 * math.tau
            u_l = (1.0 - math.cos(t)) * 0.5
            side = math.sin(t)
            # Heart lobe near base, tapering to pointed monsoon drip-tip!
            lobe = (1.0 - u_l) ** 0.55 * (0.28 + 0.85 * math.sin(u_l * math.pi))
            lx = side * width * 0.52 * lobe
            ly = -length * (0.25 + 0.75 * u_l - 0.14 * math.exp(-((u_l - 0.12) * 5.0) ** 2) * abs(side))
            pts.append((ox + lx * ca - ly * sa, oy + lx * sa + ly * ca))
        bc = (int(np.clip(28 * shade, 14, 58)), int(np.clip(58 * shade, 28, 96)), int(np.clip(26 * shade, 14, 52)), 255)
        vc = (int(np.clip(52 * shade, 26, 92)), int(np.clip(88 * shade, 46, 134)), int(np.clip(42 * shade, 22, 74)), 255)
        d.polygon(pts, fill=bc)
        dh.polygon(pts, fill=int(np.clip(150 * shade, 110, 210)))
        tip = (ox + sa * length * 0.95, oy - ca * length * 0.95)
        d.line([(sx1, sy1), tip], fill=vc, width=6)
        dh.line([(sx1, sy1), tip], fill=235, width=6)
        for vi in range(1, 6):
            frac = 0.34 + vi * 0.105
            mx, my = ox + sa * length * frac, oy - ca * length * frac
            span = width * 0.38 * math.sin((frac - 0.25) / 0.75 * math.pi)
            for sgn in (-1, 1):
                ex = mx + (sgn * span) * ca + (length * 0.06) * sa
                ey = my + (sgn * span) * sa - (length * 0.06) * ca
                d.line([(mx, my), (ex, ey)], fill=vc, width=3)
    sh_arr = np.asarray(img, dtype=np.float32) / 255.0
    mottle = BS.fbm(ssz, [8, 16, 32], seed=7240)
    sh_arr[..., :3] = np.clip(sh_arr[..., :3] * (0.82 + 0.34 * mottle[..., None]), 0.0, 1.0)
    BS.save("TropicalShrub_Color", sh_arr)
    h_arr = np.asarray(h_img.filter(ImageFilter.GaussianBlur(1.2)), dtype=np.float32) / 255.0
    BS.save("TropicalShrub_Normal", BS.normal_from_height(h_arr, 4.8))
    BS.save("TropicalShrub_Mask", np.stack((np.zeros((ssz, ssz), np.float32), np.clip(0.68 + h_arr * 0.32, 0.45, 1.0),
                                            np.zeros((ssz, ssz), np.float32), np.clip(0.52 + mottle * 0.20, 0.35, 0.78)), axis=-1))


def build_character_textures():
    """High-craft character PBR maps for CHAR002 Lan, CHAR005 Gatekeeper, and CHAR007 Bé An."""
    sz = 1024
    fab_col = BS.luma(BS.source("Fabric085", "Color", size=sz))
    fab_norm = BS.source("Fabric085", "NormalGL", size=sz)
    fab_ao = BS.luma(BS.source("Fabric085", "AmbientOcclusion", size=sz))
    fab_r = BS.luma(BS.source("Fabric085", "Roughness", size=sz))

    # 1. NurseBlouse (Lan's 2005 Vietnamese white nurse áo blouse — slightly yellowed ivory cotton, fine weave)
    crease = BS.fbm(sz, [4, 8, 16, 32], seed=7301)
    nb_col = np.full((sz, sz, 3), (0.84, 0.82, 0.74), dtype=np.float32) + BS.center(fab_col)[..., None] * 0.22 + BS.center(crease)[..., None] * 0.04
    nb_norm = BS.blend_normals(fab_norm, BS.normal_from_height(crease, 1.8), 0.30)
    nb_sm = np.clip(0.11 + BS.center(1.0 - fab_r) * 0.05, 0.05, 0.20)
    BS.map_set("NurseBlouse", nb_col, nb_norm, nb_sm, np.clip(fab_ao * 0.95 + 0.05, 0.60, 1.0))

    # 2. LanSkin (Natural warm Vietnamese young woman skin with realistic micro-pore & gentle warmth)
    pores = BS.fbm(sz, [64, 128, 256], seed=7310)
    tone_var = BS.fbm(sz, [4, 8, 16], seed=7311)
    skin_col = np.full((sz, sz, 3), (0.76, 0.59, 0.47), dtype=np.float32)
    skin_col[..., 0] += BS.center(tone_var) * 0.045
    skin_col[..., 1] += BS.center(tone_var) * 0.032
    skin_col[..., 2] += BS.center(tone_var) * 0.025
    skin_col += BS.center(pores)[..., None] * 0.015
    skin_norm = BS.normal_from_height(pores * 0.25 + tone_var * 0.08, 1.2)
    skin_sm = np.clip(0.32 + BS.center(tone_var) * 0.06 - pores * 0.04, 0.20, 0.44)
    BS.map_set("LanSkin", skin_col, skin_norm, skin_sm, np.full((sz, sz), 0.94, np.float32))

    # 3. LanHair (Natural dark Vietnamese black-brown hair strands with anisotropic highlight)
    strands = BS.fbm_aniso(sz, [96, 192, 384], [4, 8, 16], seed=7320)
    hair_col = np.full((sz, sz, 3), (0.065, 0.052, 0.044), dtype=np.float32) + strands[..., None] * 0.038
    hair_norm = BS.normal_from_height(strands, 3.6)
    hair_sm = np.clip(0.42 + strands * 0.18, 0.28, 0.64)
    BS.map_set("LanHair", hair_col, hair_norm, hair_sm, np.clip(0.65 + strands * 0.35, 0.50, 1.0))

    # 4. CharredBlouse (Gatekeeper CHAR005 — soot-blackened scorched hospital overcoat fabric)
    soot = BS.fbm(sz, [4, 8, 16, 32], seed=7330)
    ash = BS.fbm(sz, [24, 48, 96], seed=7331)
    char_mask = np.clip((soot * 0.72 + ash * 0.28 - 0.34) * 2.8, 0.0, 1.0)
    base_cloth = np.full((sz, sz, 3), (0.34, 0.34, 0.30), dtype=np.float32) + BS.center(fab_col)[..., None] * 0.26
    char_rgb = np.full((sz, sz, 3), (0.055, 0.052, 0.050), dtype=np.float32) + BS.center(ash)[..., None] * 0.04
    # Subtle burnt umber singe edge between cloth and soot
    singe = np.clip(char_mask * (1.0 - char_mask) * 4.0, 0.0, 1.0)
    cb_col = base_cloth * (1.0 - char_mask[..., None]) + char_rgb * char_mask[..., None]
    cb_col += singe[..., None] * np.array([0.11, 0.06, 0.03], dtype=np.float32)
    cb_norm = BS.blend_normals(fab_norm, BS.normal_from_height(soot * 0.4 + ash * 0.35, 3.4), 0.55)
    cb_sm = np.clip(0.10 - char_mask * 0.05 + BS.center(1.0 - fab_r) * 0.04, 0.03, 0.16)
    BS.map_set("CharredBlouse", cb_col, cb_norm, cb_sm, np.clip(fab_ao * 0.85 - char_mask * 0.22, 0.32, 1.0))

    # 5. AnCotton (Bé An's classic Vietnamese pediatric striped cotton hospital garment)
    yy, xx = np.mgrid[:sz, :sz]
    stripe = ((xx // 16) % 2).astype(np.float32)
    ac_col = np.full((sz, sz, 3), (0.72, 0.76, 0.75), dtype=np.float32) * (1.0 - stripe[..., None] * 0.22)
    ac_col += stripe[..., None] * np.array([0.10, 0.16, 0.20], dtype=np.float32) + BS.center(fab_col)[..., None] * 0.18
    BS.map_set("AnCotton", ac_col, fab_norm, np.full((sz, sz), 0.10, np.float32), np.clip(fab_ao, 0.55, 1.0))


def build_m2_signs_and_screens():
    """Fictional An Sinh M2 signs, ER illuminated lightbox, ECG screen, 2005 calendar, Lan's scratched ID card."""
    # 1. Illuminated Emergency Lightbox (CẤP CỨU)
    eb = Image.new("RGB", (1024, 340), "#a8241d")
    de = ImageDraw.Draw(eb)
    de.rounded_rectangle((14, 14, 1010, 326), 12, outline="#f5ead4", width=10)
    de.text((512, 138), "CẤP CỨU", font=BS.typeface(148, True), fill="#fbf2dc", anchor="mm")
    de.text((512, 260), "KHOA CẤP CỨU  ·  TIẾP NHẬN 24/24", font=BS.typeface(34, True), fill="#f2d6c4", anchor="mm")
    BS.add_paper_grain(eb, seed=7401, foxing=0.18).save(DEST / "Sign_EmergencyBox.png")

    # 2. Stairwell Barrier Sign (KHU B — TẠM PHONG TỎA)
    kb = Image.new("RGB", (1024, 384), "#d6cbb0")
    dk = ImageDraw.Draw(kb)
    dk.rectangle((18, 18, 1006, 366), outline="#5e2a22", width=10)
    dk.rectangle((34, 34, 990, 350), outline="#304138", width=3)
    dk.text((512, 138), "KHU B  ·  TẦNG 2", font=BS.typeface(96, True), fill="#263931", anchor="mm")
    dk.text((512, 264), "LỐI LÊN TẠM PHONG TỎA  ·  CHỜ BÀN GIAO", font=BS.typeface(34, True), fill="#692a22", anchor="mm")
    BS.add_paper_grain(kb, seed=7402, foxing=0.58).save(DEST / "Sign_KhuBClosed.png")

    # 3. Reception Counter Glass Plaque
    rw = Image.new("RGB", (1024, 320), "#24433a")
    drw = ImageDraw.Draw(rw)
    drw.rectangle((14, 14, 1010, 306), outline="#e6e1c8", width=6)
    drw.text((512, 126), "TIẾP NHẬN  ·  CỬA SỐ 1", font=BS.typeface(76, True), fill="#eee9d2", anchor="mm")
    drw.text((512, 232), "XUẤT TRÌNH SỔ KHÁM BỆNH & GIẤY CHUYỂN TUYẾN", font=BS.typeface(28, True), fill="#c7d5c8", anchor="mm")
    BS.add_paper_grain(rw, seed=7403, foxing=0.30).save(DEST / "Sign_ReceptionWindow.png")

    # 4. Large Exterior Hospital Gate Banner (1987 fictional An Sinh, NO real hospital logo)
    hg = Image.new("RGB", (1536, 384), "#1f3c34")
    dhg = ImageDraw.Draw(hg)
    dhg.rectangle((18, 18, 1518, 366), outline="#e5dcc0", width=8)
    dhg.rectangle((32, 32, 1504, 352), outline="#99ad9d", width=2)
    dhg.text((768, 140), "BỆNH VIỆN ĐA KHOA AN SINH", font=BS.typeface(92, True), fill="#efe6ca", anchor="mm")
    dhg.text((768, 266), "THÀNH LẬP 1987   ·   CỔNG PHỤ KHOA NỘI TRÚ", font=BS.typeface(38, True), fill="#cfd9c8", anchor="mm")
    BS.add_paper_grain(hg, seed=7404, foxing=0.55).save(DEST / "Sign_HospitalGate.png")

    # 5. Vintage 2005 Desk/Wall Calendar Page (18/11/2005!)
    cal = Image.new("RGB", (512, 680), "#dfd6be")
    dca = ImageDraw.Draw(cal)
    dca.rectangle((16, 16, 496, 664), outline="#2c443b", width=5)
    dca.rectangle((16, 16, 496, 150), fill="#7b2b24")
    dca.text((256, 84), "THÁNG 11 · 2005", font=BS.typeface(44, True), fill="#f3ebd6", anchor="mm")
    dca.text((256, 345), "18", font=BS.typeface(235, True), fill="#263830", anchor="mm")
    dca.line((56, 515, 456, 515), fill="#6a6552", width=3)
    dca.text((256, 566), "THỨ SÁU  ·  CA ĐÊM", font=BS.typeface(30, True), fill="#524938", anchor="mm")
    dca.text((256, 616), "Bệnh viện An Sinh", font=BS.typeface(22), fill="#4a574d", anchor="mm")
    BS.add_paper_grain(cal, seed=7405, foxing=0.52).save(DEST / "Calendar2005.png")

    # 6. Early-2000s Clinical Patient Monitor Screen (ECG / SpO2 / NIBP)
    ecg = Image.new("RGB", (1024, 768), "#0a1514")
    de2 = ImageDraw.Draw(ecg)
    # Subtle phosphor grid
    for gx in range(0, 1024, 48):
        de2.line((gx, 0, gx, 768), fill="#122724", width=1)
    for gy in range(0, 768, 48):
        de2.line((0, gy, 1024, gy), fill="#122724", width=1)
    de2.rectangle((12, 12, 1012, 756), outline="#265448", width=4)
    de2.text((40, 36), "AN SINH · CẤP CỨU · MONITOR 02", font=BS.typeface(26, True), fill="#68b89a")
    de2.text((780, 36), "03:17", font=BS.typeface(32, True), fill="#9ae0be")
    # Lead II ECG baseline trace (muted flatline / standby pulse)
    pts_ecg = []
    for x in range(36, 700, 6):
        y = 240 + int(math.sin(x * 0.04) * 2.0)
        pts_ecg.append((x, y))
    de2.line(pts_ecg, fill="#4ce292", width=4)
    # Pleth trace
    pts_pleth = [(x, 460 + int(math.sin(x * 0.03) * 2.0)) for x in range(36, 700, 6)]
    de2.line(pts_pleth, fill="#58b8dc", width=3)
    # Numeric readouts on right column
    de2.text((740, 145), "HR (bpm)", font=BS.typeface(24, True), fill="#4ce292")
    de2.text((740, 185), "---", font=BS.typeface(86, True), fill="#4ce292")
    de2.text((740, 360), "SpO2 (%)", font=BS.typeface(24, True), fill="#58b8dc")
    de2.text((740, 400), "--", font=BS.typeface(86, True), fill="#58b8dc")
    de2.text((740, 565), "NIBP (mmHg)", font=BS.typeface(24, True), fill="#e2c874")
    de2.text((740, 608), "--- / ---", font=BS.typeface(46, True), fill="#e2c874")
    de2.text((40, 695), "STANDBY · CHỜ KẾT NỐI CẢM BIẾN", font=BS.typeface(24), fill="#b9a45f")
    ecg.save(DEST / "MonitorECG.png")

    # 7. Lan's Scratched Laminated Hospital ID Badge (Core 02 §2.02: "bảng tên có vết xước")
    bd = Image.new("RGB", (512, 320), "#e4dec9")
    dbd = ImageDraw.Draw(bd)
    dbd.rectangle((8, 8, 504, 312), outline="#27463d", width=6)
    dbd.rectangle((8, 8, 504, 86), fill="#27463d")
    dbd.text((256, 48), "BỆNH VIỆN AN SINH", font=BS.typeface(30, True), fill="#efe9d4", anchor="mm")
    dbd.rectangle((34, 114, 148, 266), fill="#b8b5a6", outline="#44544c", width=3)
    dbd.ellipse((62, 134, 120, 194), fill="#5a5c56")
    dbd.chord((42, 198, 140, 290), 180, 360, fill="#d4d2c4")
    dbd.text((174, 136), "TRẦN NGỌC LAN", font=BS.typeface(34, True), fill="#1f312b")
    dbd.text((174, 188), "Điều dưỡng · Khoa Nội trú", font=BS.typeface(22), fill="#394a42")
    dbd.text((174, 230), "MSNV: AS-0412", font=BS.typeface(20, True), fill="#515a52")
    # Authentic diagonal laminated surface scratches
    dbd.line([(165, 118), (385, 255)], fill="#c8c2ad", width=4)
    dbd.line([(210, 128), (345, 212)], fill="#f2eee0", width=2)
    BS.add_paper_grain(bd, seed=7406, foxing=0.32).save(DEST / "LanBadge.png")


if __name__ == "__main__":
    DEST.mkdir(parents=True, exist_ok=True)
    build_refined_walls()
    build_exterior_materials()
    build_character_textures()
    build_m2_signs_and_screens()
    print(f"M2 PBR surfaces, exterior materials, signage & character maps updated in {DEST}")
