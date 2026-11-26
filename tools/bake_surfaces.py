"""M0-R High-Craft PBR Surface, Authored Decal, Foliage & Period Sign Generator.

Produces physically calibrated 2048x2048 PBR sets (Color, NormalGL, Mask[R=Metallic, G=AO, B=0, A=Smoothness]),
authored PBR Decal Projector maps (Color RGBA, NormalGL, Mask), botanical tropical foliage/bark maps,
and authentic aged Vietnamese hospital signs/documents (1987-2012, fictional An Sinh branding per AR03).
"""

import math
import sys
from io import BytesIO
from pathlib import Path
from zipfile import ZipFile

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[1]
RAW = ROOT / "output" / "material-staging" / "source"
DEST = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Textures"
SIZE = 2048


def rng(seed):
    return np.random.default_rng(seed)


def fbm(size, octaves, seed, persistence=0.52):
    """Tileable multi-octave value noise via bicubic periodic upsampling."""
    r = rng(seed)
    acc = np.zeros((size, size), dtype=np.float32)
    amp = 1.0
    norm = 0.0
    for freq in octaves:
        grid = r.random((freq, freq), dtype=np.float32)
        # Pad periodically for seamless tiling
        pad = 2
        tiled = np.pad(grid, pad, mode="wrap")
        up_full = (freq + 2 * pad) * max(1, size // freq)
        im = Image.fromarray(tiled, mode="F").resize((up_full, up_full), Image.Resampling.BICUBIC)
        arr = np.asarray(im, dtype=np.float32)
        scale = up_full / (freq + 2 * pad)
        crop0 = int(round(pad * scale))
        crop1 = crop0 + int(round(freq * scale))
        cropped = arr[crop0:crop1, crop0:crop1]
        if cropped.shape != (size, size):
            cropped = np.asarray(
                Image.fromarray(cropped, mode="F").resize((size, size), Image.Resampling.BICUBIC),
                dtype=np.float32,
            )
        acc += cropped * amp
        norm += amp
        amp *= persistence
    acc /= max(norm, 1e-6)
    return np.clip(acc, 0.0, 1.0)


def fbm_aniso(size, fx_list, fy_list, seed, persistence=0.55):
    """Tileable anisotropic noise (streaks, wood grain, brushed metal)."""
    r = rng(seed)
    acc = np.zeros((size, size), dtype=np.float32)
    amp = 1.0
    norm = 0.0
    for fx, fy in zip(fx_list, fy_list):
        grid = r.random((fy, fx), dtype=np.float32)
        tiled = np.pad(grid, 2, mode="wrap")
        w = (fx + 4) * max(1, size // fx)
        h = (fy + 4) * max(1, size // fy)
        im = Image.fromarray(tiled, mode="F").resize((min(w, size * 2), min(h, size * 2)), Image.Resampling.BICUBIC)
        arr = np.asarray(im, dtype=np.float32)
        sx = arr.shape[1] / (fx + 4)
        sy = arr.shape[0] / (fy + 4)
        x0, y0 = int(round(2 * sx)), int(round(2 * sy))
        x1, y1 = x0 + int(round(fx * sx)), y0 + int(round(fy * sy))
        crop = arr[y0:y1, x0:x1]
        if crop.shape != (size, size):
            crop = np.asarray(Image.fromarray(crop, mode="F").resize((size, size), Image.Resampling.BICUBIC), dtype=np.float32)
        acc += crop * amp
        norm += amp
        amp *= persistence
    return np.clip(acc / max(norm, 1e-6), 0.0, 1.0)


def source(asset, channel, size=SIZE):
    with ZipFile(RAW / (asset + ".zip")) as package:
        name = next(n for n in package.namelist() if n.endswith("_" + channel + ".jpg"))
        image = Image.open(BytesIO(package.read(name))).convert("RGB")
        if image.size != (size, size):
            image = image.resize((size, size), Image.Resampling.BICUBIC)
        return np.asarray(image, dtype=np.float32) / 255.0


def save(name, pixels):
    arr = np.uint8(np.clip(pixels, 0.0, 1.0) * 255.0 + 0.5)
    Image.fromarray(arr).save(DEST / (name + ".png"), optimize=False)


def rgb(*channels):
    return np.stack(channels, axis=-1)


def luma(image):
    return image @ np.array([0.299, 0.587, 0.114], dtype=np.float32)


def center(values):
    return values - np.median(values)


def normal_from_height(height, strength=4.0):
    """Sobel-filtered tangent-space OpenGL normal map (Y-up for Unity)."""
    h = height.astype(np.float32)
    l = np.roll(h, 1, axis=1)
    r = np.roll(h, -1, axis=1)
    u = np.roll(h, 1, axis=0)
    d = np.roll(h, -1, axis=0)
    ul = np.roll(u, 1, axis=1)
    ur = np.roll(u, -1, axis=1)
    dl = np.roll(d, 1, axis=1)
    dr = np.roll(d, -1, axis=1)
    dx = ((ul + 2.0 * l + dl) - (ur + 2.0 * r + dr)) * (strength * 0.25)
    dy = ((dl + 2.0 * d + dr) - (ul + 2.0 * u + ur)) * (strength * 0.25)
    v = rgb(dx, dy, np.ones_like(dx))
    v /= np.linalg.norm(v, axis=-1, keepdims=True)
    return v * 0.5 + 0.5


def blend_normals(n1, n2, weight=0.5):
    """Reoriented-style tangent normal blending."""
    a = n1 * 2.0 - 1.0
    b = n2 * 2.0 - 1.0
    b[..., :2] *= weight
    out = rgb(a[..., 0] + b[..., 0], a[..., 1] + b[..., 1], a[..., 2] * b[..., 2])
    out /= np.maximum(1e-5, np.linalg.norm(out, axis=-1, keepdims=True))
    return out * 0.5 + 0.5


def ao_from_height(height, radius=7, strength=0.65):
    """Approximate micro-cavity AO from height vs local blur."""
    im = Image.fromarray(np.uint8(np.clip(height, 0, 1) * 255), mode="L")
    blurred = np.asarray(im.filter(ImageFilter.GaussianBlur(radius)), dtype=np.float32) / 255.0
    cavity = np.clip((blurred - height) * 3.2, 0.0, 1.0)
    return np.clip(1.0 - cavity * strength, 0.22, 1.0)


def map_set(name, color, normal, smoothness, occlusion=1.0, metal=0.0):
    save(name + "_Color", color)
    save(name + "_Normal", normal)
    shape = color.shape[:2]
    ao = np.broadcast_to(np.asarray(occlusion, dtype=np.float32), shape)
    metallic = np.broadcast_to(np.asarray(metal, dtype=np.float32), shape)
    smooth = np.broadcast_to(np.asarray(smoothness, dtype=np.float32), shape)
    save(name + "_Mask", np.stack((metallic, ao, np.zeros(shape, dtype=np.float32), smooth), axis=-1))


def build_walls():
    """Authentic 1987-2012 Vietnamese hospital limewash plaster & semi-gloss oil-paint dado.
    Zero blotchy polka dots: uses real CC0 plaster trowel texture + multi-scale weathering,
    hairline cracks, roller lap variation, subtle moisture staining and exposed mortar patches.
    """
    plaster_col = luma(source("PaintedPlaster017", "Color"))
    plaster_disp = luma(source("PaintedPlaster017", "Displacement"))
    plaster_norm = source("PaintedPlaster017", "NormalGL")
    plaster_rough = luma(source("PaintedPlaster017", "Roughness"))

    macro = fbm(SIZE, [4, 8, 16, 32], seed=1011)
    mid = fbm(SIZE, [12, 24, 48, 96], seed=1012)
    fine = fbm(SIZE, [64, 128, 256], seed=1013)
    vertical_wash = fbm_aniso(SIZE, [16, 32, 64], [4, 8, 16], seed=1014)
    roller = fbm_aniso(SIZE, [32, 64], [8, 16], seed=1015)

    # Sparse, natural hairline vertical/diagonal shrinkage fractures (never repeating contour loops!)
    crack_img = Image.new("L", (SIZE, SIZE), 0)
    dc = ImageDraw.Draw(crack_img)
    r_crk = rng(1019)
    for _ in range(10):
        cx = float(r_crk.uniform(96, SIZE - 96))
        cy = float(r_crk.uniform(96, SIZE - 96))
        ang = float(r_crk.uniform(0.35, math.pi - 0.35))
        steps = int(r_crk.integers(12, 28))
        pts = [(cx, cy)]
        for _s in range(steps):
            ang += float(r_crk.uniform(-0.32, 0.32))
            step_l = float(r_crk.uniform(6.0, 14.0))
            cx = (cx + math.cos(ang) * step_l) % SIZE
            cy = (cy + math.sin(ang) * step_l) % SIZE
            if abs(cx - pts[-1][0]) < 56 and abs(cy - pts[-1][1]) < 56:
                pts.append((cx, cy))
            else:
                break
        if len(pts) > 2:
            dc.line(pts, fill=int(r_crk.integers(70, 140)), width=1)
    cracks = np.asarray(crack_img.filter(ImageFilter.GaussianBlur(0.8)), dtype=np.float32) / 255.0

    # Localized limewash flaking (large organic islands, NOT polka dots)
    flake_base = macro * 0.65 + mid * 0.35
    flake_mask = np.clip((flake_base - 0.67) * 9.5, 0.0, 1.0)
    # Deeper mortar exposure inside flake centers
    mortar_mask = np.clip((flake_base - 0.74) * 12.0, 0.0, 1.0)
    # Raised blistered paint edge around flake boundary
    flake_rim = np.clip(flake_mask * (1.0 - flake_mask) * 4.0, 0.0, 1.0)

    # Subtle mildew clouding only in low-value macro damp zones (no harsh spots)
    mildew = np.clip((1.0 - macro * 0.7 - vertical_wash * 0.3 - 0.42) * 3.8, 0.0, 1.0) * (0.45 + 0.55 * fine)

    trowel_detail = center(plaster_col) * 0.22 + center(plaster_disp) * 0.10
    trowel_ao = ao_from_height(plaster_disp, radius=8, strength=0.36)

    # --- PlasterB (2005 operational: maintained warm ivory-sage institutional limewash, clean smooth finish) ---
    pb_col = np.full((SIZE, SIZE, 3), (0.77, 0.77, 0.69), dtype=np.float32)
    pb_col += trowel_detail[..., None] * 0.52 + center(roller)[..., None] * 0.028 + center(macro)[..., None] * 0.026
    pb_col -= cracks[..., None] * 0.022
    pb_norm = blend_normals(plaster_norm, normal_from_height(plaster_disp * 0.75 + roller * 0.14 - cracks * 0.08, 1.8), 0.32)
    pb_smooth = np.clip(0.15 + (1.0 - plaster_rough) * 0.09 + center(roller) * 0.045, 0.08, 0.27)
    pb_ao = np.clip(trowel_ao * 0.96 + 0.04 - cracks * 0.06, 0.72, 1.0)
    map_set("PlasterB", pb_col, pb_norm, pb_smooth, pb_ao)

    # --- PlasterR (2012 abandoned: weathered cool damp limewash, yellow-ochre old coat & grey mortar) ---
    pr_col = np.full((SIZE, SIZE, 3), (0.50, 0.53, 0.48), dtype=np.float32)
    pr_col += trowel_detail[..., None] * 0.95 + center(macro)[..., None] * 0.09 + center(vertical_wash)[..., None] * 0.07
    # Old yellow-cream 1987 undercoat revealed where outer coat flakes
    undercoat = np.array([0.52, 0.49, 0.37], dtype=np.float32) + center(fine)[..., None] * 0.08
    # Raw grey cement plaster where deeply spalled
    raw_mortar = np.array([0.34, 0.35, 0.33], dtype=np.float32) + center(fine)[..., None] * 0.12
    pr_col = pr_col * (1.0 - flake_mask[..., None]) + undercoat * flake_mask[..., None]
    pr_col = pr_col * (1.0 - mortar_mask[..., None]) + raw_mortar * mortar_mask[..., None]
    # Dark olive-slate tropical dampness clouding
    pr_col = pr_col * (1.0 - mildew[..., None] * 0.38) + np.array([0.16, 0.21, 0.18]) * (mildew[..., None] * 0.38)
    pr_col -= cracks[..., None] * 0.14
    pr_col += flake_rim[..., None] * 0.05  # chalky efflorescence rim
    pr_height = plaster_disp - flake_mask * 0.28 - mortar_mask * 0.22 - cracks * 0.35 + flake_rim * 0.08
    pr_norm = blend_normals(plaster_norm, normal_from_height(pr_height, 5.5), 0.72)
    pr_smooth = np.clip(0.14 + (1.0 - plaster_rough) * 0.09 - flake_mask * 0.06 - mortar_mask * 0.04 + mildew * 0.07, 0.05, 0.28)
    pr_ao = np.clip(ao_from_height(pr_height, radius=10, strength=0.72) - cracks * 0.22 - mortar_mask * 0.12, 0.35, 1.0)
    map_set("PlasterR", pr_col, pr_norm, pr_smooth, pr_ao)

    # --- DadoB (2005 operational: wipeable Vietnamese hospital oil-paint wainscot green #3E6659) ---
    db_col = np.full((SIZE, SIZE, 3), (0.28, 0.46, 0.39), dtype=np.float32)
    db_col += trowel_detail[..., None] * 0.35 + center(roller)[..., None] * 0.04
    # Subtle horizontal brush/wipe marks
    wipe = fbm_aniso(SIZE, [8, 16], [48, 96], seed=1017)
    db_smooth = np.clip(0.42 + (1.0 - plaster_rough) * 0.12 + center(wipe) * 0.08, 0.28, 0.58)
    db_norm = blend_normals(plaster_norm, normal_from_height(plaster_disp + wipe * 0.08, 1.8), 0.32)
    db_ao = np.clip(trowel_ao * 0.94 + 0.06, 0.72, 1.0)
    map_set("DadoB", db_col, db_norm, db_smooth, db_ao)

    # --- DadoR (2012 abandoned: scuffed faded oil paint, chalky bloom, chipped streaks) ---
    dr_col = np.full((SIZE, SIZE, 3), (0.21, 0.31, 0.28), dtype=np.float32)
    dr_col += trowel_detail[..., None] * 0.60 + center(macro)[..., None] * 0.08 + center(vertical_wash)[..., None] * 0.06
    scuff = np.clip((fbm_aniso(SIZE, [12, 24], [32, 64], seed=1018) - 0.62) * 3.8, 0.0, 1.0)
    dr_col = dr_col * (1.0 - flake_mask[..., None] * 0.85) + raw_mortar * (flake_mask[..., None] * 0.85)
    dr_col = dr_col * (1.0 - mildew[..., None] * 0.42) + np.array([0.10, 0.14, 0.12]) * (mildew[..., None] * 0.42)
    dr_col += scuff[..., None] * 0.055  # bed/stretcher rub marks
    dr_height = plaster_disp - flake_mask * 0.30 - cracks * 0.25
    dr_norm = blend_normals(plaster_norm, normal_from_height(dr_height, 4.8), 0.68)
    # Oil paint still retains pockets of dull sheen where intact, matte where chalked/flaked
    dr_smooth = np.clip(0.32 + (1.0 - plaster_rough) * 0.14 - flake_mask * 0.22 - scuff * 0.12 + mildew * 0.06, 0.08, 0.50)
    dr_ao = np.clip(ao_from_height(dr_height, radius=10, strength=0.68) - cracks * 0.18, 0.38, 1.0)
    map_set("DadoR", dr_col, dr_norm, dr_smooth, dr_ao)


def build_floor_tiles():
    """Authentic Vietnamese 20x20 cm pressed cement tiles (gạch bông) with fine smooth pigment cement paste
    (zero coarse terrazzo boulder overlay!), subtle hand-laid per-tile micro-tilt & bevels, crisp recessed grout,
    classic octagonal/star-corner motif, and distinct B (2005 waxed satin) vs R (2012 gritty aged) surface feel."""
    plaster_disp = luma(source("PaintedPlaster017", "Displacement"))
    plaster_rough = luma(source("PaintedPlaster017", "Roughness"))

    tiles_per_side = 8  # 1.6 m tile scale => exact 0.20 m per cement tile!
    s = SIZE // tiles_per_side
    yy, xx = np.mgrid[:SIZE, :SIZE]
    tx, ty = xx // s, yy // s
    u = (xx % s) / float(s)
    v = (yy % s) / float(s)

    # Per-tile deterministic slight tonal, gloss & hand-laid lippage/tilt variation (breaks sterile tube reflections!)
    r = rng(2020)
    tile_tone = r.uniform(-0.024, 0.024, (tiles_per_side, tiles_per_side)).astype(np.float32)[ty, tx]
    tile_gloss = r.uniform(-0.055, 0.055, (tiles_per_side, tiles_per_side)).astype(np.float32)[ty, tx]
    tile_tilt_x = r.uniform(-0.036, 0.036, (tiles_per_side, tiles_per_side)).astype(np.float32)[ty, tx]
    tile_tilt_y = r.uniform(-0.036, 0.036, (tiles_per_side, tiles_per_side)).astype(np.float32)[ty, tx]
    tile_lip = (u - 0.5) * tile_tilt_x + (v - 0.5) * tile_tilt_y
    checker = ((tx + ty) % 2).astype(np.float32)

    # Distance from tile edge (0 at grout center)
    edge = np.minimum(np.minimum(u, 1.0 - u), np.minimum(v, 1.0 - v))
    grout = np.clip(1.0 - edge / 0.015, 0.0, 1.0)
    bevel = np.clip(1.0 - edge / 0.040, 0.0, 1.0)

    # Classic Vietnamese hospital gạch bông geometric motif (symmetric, crisp, understated):
    du = np.abs(u - 0.5)
    dv = np.abs(v - 0.5)
    manhattan = du + dv
    cheb = np.maximum(du, dv)
    radial = np.sqrt(du * du + dv * dv)

    # Outer frame border line
    border_frame = ((cheb > 0.405) & (cheb < 0.442)).astype(np.float32)
    # Rotated diamond frame
    diamond = ((manhattan > 0.365) & (manhattan < 0.405)).astype(np.float32)
    # Corner quarter-petal accents (connect across 4 tiles into flowers!)
    corner_r = np.sqrt(np.minimum(u, 1.0 - u) ** 2 + np.minimum(v, 1.0 - v) ** 2)
    corner_arc = ((corner_r > 0.145) & (corner_r < 0.185)).astype(np.float32)
    corner_dot = (corner_r < 0.075).astype(np.float32)
    # Central subtle geometric rosette
    rosette = ((radial > 0.105) & (radial < 0.142)).astype(np.float32)
    cross_arm = (((du < 0.024) & (dv < 0.115)) | ((dv < 0.024) & (du < 0.115))).astype(np.float32)

    motif_dark = np.clip(border_frame * 0.70 + diamond * 0.86 + corner_dot * 0.72 + cross_arm * 0.55, 0.0, 1.0)
    motif_mid = np.clip(corner_arc * 0.60 + rosette * 0.52, 0.0, 1.0)

    # Soften motif edges slightly like real hydraulically pressed pigment paste
    motif_dark = np.asarray(Image.fromarray(np.uint8(motif_dark * 255)).filter(ImageFilter.GaussianBlur(0.85)), dtype=np.float32) / 255.0
    motif_mid = np.asarray(Image.fromarray(np.uint8(motif_mid * 255)).filter(ImageFilter.GaussianBlur(0.85)), dtype=np.float32) / 255.0

    wear = fbm(SIZE, [6, 12, 24, 48], seed=2021)
    grit = fbm(SIZE, [32, 64, 128, 256], seed=2022)
    cement_micro = fbm(SIZE, [96, 192, 384], seed=2024)
    scratches = fbm_aniso(SIZE, [64, 128], [16, 32], seed=2023)
    chip_mask = np.clip((wear * 0.55 + grit * 0.45 - 0.73) * 8.0, 0.0, 1.0) * (bevel > 0.25)

    # Fine pressed mineral cement paste (clean & tactile, zero chunky terrazzo boulders!)
    paste_grain = center(cement_micro) * 0.036 + center(plaster_disp) * 0.028 + center(grit) * 0.022

    for name, base_rgb, dark_rgb, mid_rgb, age in (
        ("FloorB", (0.67, 0.66, 0.58), (0.24, 0.35, 0.30), (0.45, 0.51, 0.44), 0.18),
        ("FloorR", (0.49, 0.50, 0.45), (0.20, 0.27, 0.24), (0.35, 0.38, 0.34), 1.00),
    ):
        col = np.full((SIZE, SIZE, 3), base_rgb, dtype=np.float32)
        col += paste_grain[..., None] + tile_tone[..., None] + (checker - 0.5)[..., None] * 0.018
        fade = 1.0 - age * np.clip(wear - 0.32, 0.0, 1.0) * 0.46
        m1 = motif_dark * fade
        m2 = motif_mid * fade
        col = col * (1.0 - m2[..., None] * 0.54) + np.array(mid_rgb) * (m2[..., None] * 0.54)
        col = col * (1.0 - m1[..., None] * 0.74) + np.array(dark_rgb) * (m1[..., None] * 0.74)
        # Dust & trapped dirt in grout + subtle chalky grime & chipped corners in R
        grout_col = np.array([0.19, 0.20, 0.17]) if age > 0.5 else np.array([0.36, 0.36, 0.32])
        col = col * (1.0 - grout[..., None] * 0.84) + grout_col * (grout[..., None] * 0.84)
        if age > 0.5:
            dust_film = np.clip((wear * 0.6 + grit * 0.4 - 0.38) * 1.9, 0.0, 1.0)
            col = col * (1.0 - dust_film[..., None] * 0.24) + np.array([0.32, 0.33, 0.29]) * (dust_film[..., None] * 0.24)
            col -= chip_mask[..., None] * 0.14
        else:
            col += center(wear)[..., None] * 0.025

        # Real physical height: slight per-tile hand-laid tilt + beveled edge + recessed grout + fine mineral relief
        tile_h = 0.82 + tile_lip * (1.0 - grout) - bevel * 0.17 - grout * 0.46 - chip_mask * age * 0.24 + center(cement_micro) * 0.035 + center(plaster_disp) * 0.03
        tile_n = normal_from_height(tile_h, 6.4)

        # Physically accurate smoothness: grout is matte (0.06), B has waxed satin hospital patina, R is dusty/scuffed
        base_sm = 0.44 if age < 0.5 else 0.27
        sm = base_sm + tile_gloss + center(wear) * 0.14 + center(1.0 - plaster_rough) * 0.06 + center(scratches) * 0.05
        sm = sm * (1.0 - grout) + 0.06 * grout - chip_mask * age * 0.14
        sm = np.clip(sm, 0.05, 0.62 if age < 0.5 else 0.48)

        # Crisp recessed grout & chip crevice AO
        ao = np.clip(1.0 - grout * 0.54 - bevel * 0.11 - chip_mask * age * 0.22 - (1.0 - ao_from_height(plaster_disp, 5, 0.22)) * 0.28, 0.34, 1.0)
        map_set(name, col, tile_n, sm, ao)


def build_wood_and_metals():
    """Dedicated PBR textures for Vietnamese varnished timber doors, painted hospital iron/steel,
    brushed stainless steel, vinyl mattress, cotton linen, ceramic, and exterior concrete."""
    # --- WoodDoorB / WoodDoorR (Vietnamese hardwood xoan đào / chò, physically calibrated so grain reads under dim lighting!) ---
    yy, xx = np.mgrid[:SIZE, :SIZE].astype(np.float32) / SIZE
    warp = fbm(SIZE, [4, 8, 16], seed=3001) * 0.18
    rings = np.sin((xx * 24.0 + warp * 13.0) * math.tau) * 0.5 + 0.5
    fibers = fbm_aniso(SIZE, [96, 192], [8, 16], seed=3002)
    pores = np.clip((fbm_aniso(SIZE, [128, 256], [16, 32], seed=3003) - 0.74) * 4.8, 0.0, 1.0)
    wear = fbm(SIZE, [6, 12, 24], seed=3004)
    grain_h = rings * 0.35 + fibers * 0.45 - pores * 0.32

    for name, dark_c, light_c, sm_base, age in (
        ("WoodDoorB", (0.28, 0.175, 0.105), (0.48, 0.32, 0.19), 0.50, 0.15),
        ("WoodDoorR", (0.22, 0.150, 0.098), (0.38, 0.265, 0.17), 0.32, 1.00),
    ):
        t = np.clip(rings * 0.55 + fibers * 0.45, 0.0, 1.0)
        col = np.array(dark_c) * (1.0 - t[..., None]) + np.array(light_c) * t[..., None]
        col -= pores[..., None] * 0.05
        if age > 0.5:
            # Sun/humidity faded dust & worn varnish patches along edges/rails
            faded = np.clip((wear - 0.50) * 2.8, 0.0, 1.0)
            col = col * (1.0 - faded[..., None] * 0.34) + np.array([0.31, 0.27, 0.21]) * (faded[..., None] * 0.34)
        norm = normal_from_height(grain_h, 3.4)
        sm = np.clip(sm_base + center(rings) * 0.06 - pores * 0.16 - age * np.clip(wear - 0.42, 0, 1) * 0.16, 0.10, 0.64)
        ao = np.clip(0.95 - pores * 0.22 - (1.0 - rings) * 0.07, 0.58, 1.0)
        map_set(name, col, norm, sm, ao)

    # --- PaintedMetalB / PaintedMetalR (bed frames, window frames, steel cabinets) ---
    metal_col = luma(source("Metal032", "Color"))
    metal_disp = luma(source("Metal032", "Displacement"))
    metal_norm = source("Metal032", "NormalGL")
    metal_rough = luma(source("Metal032", "Roughness"))
    plaster_n = source("PaintedPlaster017", "NormalGL")
    m_wear = fbm(SIZE, [6, 12, 24, 48], seed=3010)
    m_fine = fbm(SIZE, [48, 96, 192], seed=3011)
    rust_mask = np.clip((m_wear * 0.68 + m_fine * 0.32 - 0.64) * 7.5, 0.0, 1.0)
    chip_metal = np.clip((m_wear * 0.5 + m_fine * 0.5 - 0.72) * 9.0, 0.0, 1.0) * (1.0 - rust_mask * 0.6)

    # Clean institutional enameled hospital green-sage metal (B)
    pm_b = np.full((SIZE, SIZE, 3), (0.36, 0.47, 0.41), dtype=np.float32)
    pm_b += center(metal_col)[..., None] * 0.12 + center(m_fine)[..., None] * 0.04
    pm_b_n = blend_normals(metal_norm, plaster_n, 0.25)
    pm_b_sm = np.clip(0.48 + center(1.0 - metal_rough) * 0.16 + center(m_wear) * 0.08, 0.26, 0.66)
    pm_b_ao = np.clip(ao_from_height(metal_disp, 6, 0.30), 0.74, 1.0)
    map_set("PaintedMetalB", pm_b, pm_b_n, pm_b_sm, pm_b_ao, metal=0.18)

    # Aged 2012 painted hospital steel with localized flaky rust & chipped edges (R)
    pm_r = np.full((SIZE, SIZE, 3), (0.24, 0.30, 0.26), dtype=np.float32)
    pm_r += center(metal_col)[..., None] * 0.16 + center(m_wear)[..., None] * 0.08
    rust_col = np.array([0.33, 0.14, 0.06], dtype=np.float32) + center(m_fine)[..., None] * 0.10
    bare_col = np.array([0.42, 0.44, 0.43], dtype=np.float32)
    pm_r = pm_r * (1.0 - chip_metal[..., None]) + bare_col * chip_metal[..., None]
    pm_r = pm_r * (1.0 - rust_mask[..., None]) + rust_col * rust_mask[..., None]
    pm_r_h = metal_disp * 0.5 + m_fine * rust_mask * 0.55 - chip_metal * 0.2
    pm_r_n = blend_normals(metal_norm, normal_from_height(pm_r_h, 4.8), 0.65)
    pm_r_sm = np.clip(0.36 + center(1.0 - metal_rough) * 0.14 - rust_mask * 0.28 + chip_metal * 0.24, 0.06, 0.68)
    pm_r_met = np.clip(0.18 + chip_metal * 0.65 - rust_mask * 0.16, 0.0, 0.88)
    pm_r_ao = np.clip(ao_from_height(pm_r_h, 8, 0.52) - rust_mask * 0.15, 0.42, 1.0)
    map_set("PaintedMetalR", pm_r, pm_r_n, pm_r_sm, pm_r_ao, metal=pm_r_met)

    # --- Brushed Stainless Steel (IV pole, door kickplate, handles, trolley frame) ---
    brush = fbm_aniso(SIZE, [24, 48], [128, 256], seed=3020)
    st_col = np.full((SIZE, SIZE, 3), (0.64, 0.67, 0.66), dtype=np.float32)
    st_col += center(metal_col)[..., None] * 0.24 + center(brush)[..., None] * 0.10
    st_n = blend_normals(metal_norm, normal_from_height(metal_disp * 0.5 + brush * 0.5, 3.8), 0.72)
    st_sm = np.clip(0.66 + center(1.0 - metal_rough) * 0.22 + center(brush) * 0.10, 0.35, 0.88)
    st_ao = np.clip(ao_from_height(metal_disp, 5, 0.35), 0.68, 1.0)
    map_set("Steel", st_col, st_n, st_sm, st_ao, metal=0.92)

    # --- CottonB / CottonR (woven hospital sheets, curtains, blankets) ---
    fab_col = luma(source("Fabric085", "Color"))
    fab_disp = luma(source("Fabric085", "Displacement"))
    fab_norm = source("Fabric085", "NormalGL")
    fab_ao = luma(source("Fabric085", "AmbientOcclusion"))
    fab_rough = luma(source("Fabric085", "Roughness"))
    crease = fbm(SIZE, [4, 8, 16, 32], seed=3030)
    stain = np.clip((fbm(SIZE, [4, 8, 16], seed=3031) - 0.58) * 2.8, 0.0, 1.0)

    cb_col = np.full((SIZE, SIZE, 3), (0.76, 0.78, 0.71), dtype=np.float32) + center(fab_col)[..., None] * 0.38
    cb_norm = blend_normals(fab_norm, normal_from_height(crease, 2.0), 0.35)
    cb_sm = np.clip(0.09 + center(1.0 - fab_rough) * 0.06, 0.04, 0.18)
    map_set("Cotton", cb_col, cb_norm, cb_sm, np.clip(fab_ao, 0.45, 1.0))

    cr_col = np.full((SIZE, SIZE, 3), (0.55, 0.55, 0.46), dtype=np.float32) + center(fab_col)[..., None] * 0.44
    cr_col = cr_col * (1.0 - stain[..., None] * 0.35) + np.array([0.34, 0.31, 0.22]) * (stain[..., None] * 0.35)
    cr_norm = blend_normals(fab_norm, normal_from_height(crease, 3.2), 0.48)
    cr_sm = np.clip(0.07 + center(1.0 - fab_rough) * 0.05, 0.03, 0.15)
    map_set("CottonOld", cr_col, cr_norm, cr_sm, np.clip(fab_ao * 0.90 - stain * 0.12, 0.35, 1.0))

    # --- Vinyl Mattress (hospital green-blue wipeable leatherette cover) ---
    pebble = fbm(SIZE, [96, 192, 384], seed=3040)
    fold = fbm_aniso(SIZE, [6, 12], [18, 36], seed=3041)
    vh = pebble * 0.45 + fold * 0.55
    vn = normal_from_height(vh, 3.4)
    v_ao = ao_from_height(vh, 6, 0.38)
    vb_col = np.full((SIZE, SIZE, 3), (0.29, 0.44, 0.39), dtype=np.float32) + center(pebble)[..., None] * 0.06 + center(fold)[..., None] * 0.05
    map_set("MattressB", vb_col, vn, np.clip(0.42 + center(pebble) * 0.12, 0.24, 0.58), v_ao)
    vr_col = np.full((SIZE, SIZE, 3), (0.24, 0.31, 0.26), dtype=np.float32) + center(pebble)[..., None] * 0.08 + center(fold)[..., None] * 0.09
    vr_col = vr_col * (1.0 - stain[..., None] * 0.32) + np.array([0.19, 0.17, 0.12]) * (stain[..., None] * 0.32)
    map_set("MattressR", vr_col, vn, np.clip(0.28 + center(pebble) * 0.10 - stain * 0.12, 0.10, 0.46), np.clip(v_ao - stain * 0.10, 0.4, 1.0))

    # --- Ceramic / Terrazzo Counter & Enamelware ---
    cer_col = luma(source("Tiles040", "Color"))
    cer_disp = luma(source("Tiles040", "Displacement"))
    cer_norm = source("Tiles040", "NormalGL")
    cer_rough = luma(source("Tiles040", "Roughness"))
    c_col = np.full((SIZE, SIZE, 3), (0.74, 0.76, 0.71), dtype=np.float32) + center(cer_col)[..., None] * 0.35
    c_sm = np.clip(0.64 + center(1.0 - cer_rough) * 0.22, 0.35, 0.85)
    c_ao = ao_from_height(cer_disp, 6, 0.45)
    map_set("Ceramic", c_col, cer_norm, c_sm, c_ao)

    # --- Exterior Weathered Concrete & Bark ---
    bark_h = fbm_aniso(SIZE, [48, 96, 192], [6, 12, 24], seed=3050)
    bark_moss = np.clip((fbm(SIZE, [8, 16, 32], seed=3051) - 0.56) * 3.2, 0.0, 1.0)
    bark_col = np.array([0.17, 0.14, 0.11]) * (1.0 - bark_h[..., None]) + np.array([0.30, 0.26, 0.20]) * bark_h[..., None]
    bark_col = bark_col * (1.0 - bark_moss[..., None] * 0.55) + np.array([0.13, 0.20, 0.11]) * (bark_moss[..., None] * 0.55)
    map_set("Bark", bark_col, normal_from_height(bark_h, 7.5), np.clip(0.14 + bark_moss * 0.08, 0.06, 0.28), ao_from_height(bark_h, 8, 0.65))


def build_tropical_leaf_atlas():
    """Paint a high-craft botanical tropical courtyard tree twig & multi-leaf cluster texture (1024x1024 RGBA + Normal + Mask)
    inspired by Vietnamese hospital courtyard shade trees (bàng / xà cừ)."""
    sz = 1024
    col_img = Image.new("RGBA", (sz, sz), (24, 42, 24, 0))
    h_img = Image.new("L", (sz, sz), 0)
    d_col = ImageDraw.Draw(col_img)
    d_h = ImageDraw.Draw(h_img)

    # Central woody twig curving gently upward
    twig_pts = [(512, 990), (506, 760), (518, 520), (508, 280), (514, 95)]
    for i in range(len(twig_pts) - 1):
        w = 18 - i * 3
        d_col.line([twig_pts[i], twig_pts[i + 1]], fill=(54, 44, 31, 255), width=w)
        d_h.line([twig_pts[i], twig_pts[i + 1]], fill=180, width=w)

    # Draw 18 overlapping veined broad tropical leaves radiating naturally along the branch
    r = rng(4040)
    leaves = [
        (510, 820, -1.05, 195, 92), (514, 800, 1.02, 190, 88),
        (508, 690, -0.92, 225, 104), (516, 665, 0.94, 220, 100),
        (512, 560, -1.12, 235, 110), (518, 540, 1.08, 230, 108),
        (514, 440, -0.82, 215, 98), (512, 420, 0.85, 210, 96),
        (510, 325, -0.68, 195, 92), (514, 310, 0.70, 190, 90),
        (510, 225, -0.46, 175, 84), (514, 215, 0.48, 172, 82),
        (512, 145, -0.18, 165, 78), (514, 140, 0.18, 162, 76),
        (512, 115, 0.0, 155, 74),
        # Depth filler inner leaves
        (465, 590, -0.55, 165, 78), (560, 480, 0.52, 160, 76), (480, 360, -0.35, 155, 72),
    ]
    for cx, cy, ang, length, width in leaves:
        shade = float(r.uniform(0.84, 1.16))
        base_c = (
            int(np.clip(36 * shade, 18, 70)),
            int(np.clip(64 * shade, 32, 105)),
            int(np.clip(34 * shade, 18, 65)),
            255,
        )
        vein_c = (
            int(np.clip(58 * shade, 30, 96)),
            int(np.clip(88 * shade, 48, 130)),
            int(np.clip(45 * shade, 25, 80)),
            255,
        )
        # Stem + leaf polygon
        ca, sa = math.cos(ang), math.sin(ang)
        pts = []
        steps = 20
        for k in range(steps):
            t = k / float(steps) * math.tau
            # Obovate tropical leaf (wider toward tip like cây bàng!)
            u_l = (1.0 - math.cos(t)) * 0.5  # 0 at stem, 1 at tip
            side = math.sin(t)
            profile = math.sin(u_l * math.pi) ** 0.72 * (0.72 + 0.42 * u_l)
            lx = side * width * 0.52 * profile
            ly = -u_l * length
            px = cx + lx * ca - ly * sa
            py = cy + lx * sa + ly * ca
            pts.append((px, py))
        # Petiole
        d_col.line([(cx, cy), (cx + sa * 18, cy - ca * 18)], fill=(58, 52, 34, 255), width=5)
        d_col.polygon(pts, fill=base_c)
        d_h.polygon(pts, fill=int(np.clip(145 * shade, 110, 210)))
        # Central midrib vein + lateral veins
        tip = (cx + sa * length * 0.92, cy - ca * length * 0.92)
        d_col.line([(cx, cy), tip], fill=vein_c, width=4)
        d_h.line([(cx, cy), tip], fill=235, width=4)
        for v_idx in range(1, 7):
            frac = v_idx / 7.2
            mx = cx + sa * length * frac
            my = cy - ca * length * frac
            span = width * 0.40 * math.sin(frac * math.pi)
            for sgn in (-1, 1):
                ex = mx + (sgn * span) * ca + (length * 0.08) * sa
                ey = my + (sgn * span) * sa - (length * 0.08) * ca
                d_col.line([(mx, my), (ex, ey)], fill=vein_c, width=2)
                d_h.line([(mx, my), (ex, ey)], fill=215, width=2)

    col_arr = np.asarray(col_img, dtype=np.float32) / 255.0
    # Add organic chlorophyll mottling inside leaf alpha
    mottle = fbm(sz, [8, 16, 32], seed=4041)
    alpha = col_arr[..., 3:4]
    col_arr[..., :3] = np.clip(col_arr[..., :3] * (0.82 + 0.38 * mottle[..., None]), 0.0, 1.0)
    col_arr[..., 3:4] = alpha
    save("TropicalLeaf_Color", col_arr)

    h_arr = np.asarray(h_img.filter(ImageFilter.GaussianBlur(1.2)), dtype=np.float32) / 255.0
    save("TropicalLeaf_Normal", normal_from_height(h_arr, 4.5))
    # Wet monsoon rain sheen on leaves
    sm = np.clip(0.42 + mottle * 0.18, 0.25, 0.65)
    ao = np.clip(0.65 + h_arr * 0.35, 0.45, 1.0)
    save("TropicalLeaf_Mask", np.stack((np.zeros((sz, sz), dtype=np.float32), ao, np.zeros((sz, sz), dtype=np.float32), sm), axis=-1))


def build_authored_decals():
    """Rich, physically grounded 1024x1024 PBR Decal Projector textures (Color RGBA + Normal + Mask).
    Designed for URP DecalProjector so every stain conforms to geometry with real roughness & normal relief."""
    sz = 1024
    y, x = np.mgrid[0:sz, 0:sz].astype(np.float32) / (sz - 1)
    sin_x = np.maximum(0.0, np.sin(x * math.pi))
    sin_y = np.maximum(0.0, np.sin(y * math.pi))
    edge_fade = (sin_x ** 0.7) * (sin_y ** 0.7)
    n_macro = fbm(sz, [6, 12, 24], seed=5001)
    n_fine = fbm(sz, [32, 64, 128], seed=5002)
    n_streaks = fbm_aniso(SIZE // 2, [24, 48, 96], [4, 8, 16], seed=5003)

    # 1. WaterRun: Gravity-driven rainwater runoff & tropical algae streaks under window sills
    # Top edge (y near 0) is concentrated under sill, breaking into vertical gravity fingers downward
    finger_x = np.exp(-((x - 0.5) * 2.15) ** 2) * (0.55 + 0.65 * n_streaks)
    drip_len = 0.35 + 0.58 * n_streaks
    grav = np.clip(1.0 - y / np.maximum(0.25, drip_len), 0.0, 1.0) ** 0.75 * np.clip(y * 12.0, 0.0, 1.0)
    water_alpha = np.clip(finger_x * grav * (0.65 + 0.55 * n_macro) * 1.45, 0.0, 0.88) * (sin_x ** 0.45)
    # Dark olive-brown algae core + chalky mineral tide edge
    tide = np.clip(water_alpha * (1.0 - water_alpha) * 3.5, 0.0, 1.0)
    w_col = rgb(
        0.13 + n_fine * 0.08 + tide * 0.18,
        0.17 + n_fine * 0.07 + tide * 0.17,
        0.12 + n_fine * 0.05 + tide * 0.14,
        water_alpha,
    )
    save("WaterRun", w_col)
    save("WaterRun_Normal", normal_from_height(water_alpha * 0.4 + n_fine * 0.25, 3.0))
    save("WaterRun_Mask", rgb(np.zeros((sz, sz)), 1.0 - water_alpha * 0.4, np.zeros((sz, sz)), 0.35 + water_alpha * 0.42))

    # 2. PaintLoss: Blistered peeling Vietnamese limewash/oil paint showing yellow undercoat & grey mortar
    blob = np.exp(-((x - 0.5) ** 2 * 6.5 + (y - 0.5) ** 2 * 7.5)) * edge_fade
    peel_field = blob * (0.45 + 0.85 * n_macro + 0.25 * n_fine)
    outer_peel = np.clip((peel_field - 0.30) * 6.5, 0.0, 1.0)
    inner_mortar = np.clip((peel_field - 0.46) * 8.5, 0.0, 1.0)
    curl_rim = np.clip(outer_peel * (1.0 - outer_peel) * 4.2, 0.0, 1.0)
    p_r = 0.49 * (1.0 - inner_mortar) + 0.31 * inner_mortar + center(n_fine) * 0.10 + curl_rim * 0.14
    p_g = 0.45 * (1.0 - inner_mortar) + 0.32 * inner_mortar + center(n_fine) * 0.09 + curl_rim * 0.13
    p_b = 0.35 * (1.0 - inner_mortar) + 0.30 * inner_mortar + center(n_fine) * 0.08 + curl_rim * 0.11
    save("PaintLoss", rgb(p_r, p_g, p_b, np.clip(outer_peel * 0.92, 0.0, 0.92)))
    peel_h = 0.7 - outer_peel * 0.35 - inner_mortar * 0.25 + curl_rim * 0.22 + n_fine * 0.12
    save("PaintLoss_Normal", normal_from_height(peel_h, 7.2))
    save("PaintLoss_Mask", rgb(np.zeros((sz, sz)), 1.0 - outer_peel * 0.38, np.zeros((sz, sz)), np.full((sz, sz), 0.10)))

    # 3. DampFloor: Glistening monsoon water puddle & dark damp halo on terrazzo floor
    puddle_field = np.exp(-((x - 0.5) ** 2 * 5.2 + (y - 0.5) ** 2 * 6.0)) * (0.55 + 0.65 * n_macro) * edge_fade
    damp_halo = np.clip((puddle_field - 0.16) * 2.8, 0.0, 0.78)
    wet_core = np.clip((puddle_field - 0.36) * 5.0, 0.0, 0.92)
    d_col = rgb(
        0.09 + n_fine * 0.04,
        0.13 + n_fine * 0.04,
        0.13 + n_fine * 0.05,
        np.clip(damp_halo * 0.65 + wet_core * 0.22, 0.0, 0.76),
    )
    save("DampFloor", d_col)
    save("DampFloor_Normal", normal_from_height(n_fine * 0.06 * (1.0 - wet_core), 1.5))
    # High smoothness in wet core so Reflection Probe shines right off the puddle!
    save("DampFloor_Mask", rgb(np.zeros((sz, sz)), 1.0 - damp_halo * 0.25, np.zeros((sz, sz)), np.clip(0.55 + wet_core * 0.39, 0.45, 0.95)))

    # 4. RustRun: Concentrated iron-oxide rust weep dripping beneath window bolts/hinges/screws
    rust_x = np.exp(-((x - 0.5) * 6.0) ** 2) * (0.55 + 0.65 * n_streaks)
    rust_y = np.clip(1.0 - y * 1.15, 0.0, 1.0) ** 1.1 * np.clip(y * 18.0, 0.0, 1.0)
    r_alpha = np.clip(rust_x * rust_y * (0.6 + 0.6 * n_fine) * 2.0, 0.0, 0.90)
    save("RustRun", rgb(0.36 + n_fine * 0.14, 0.14 + n_fine * 0.09, 0.05 + n_fine * 0.05, r_alpha))
    save("RustRun_Normal", normal_from_height(r_alpha * 0.35 + n_fine * 0.2, 3.8))
    save("RustRun_Mask", rgb(np.zeros((sz, sz)), 1.0 - r_alpha * 0.35, np.zeros((sz, sz)), np.full((sz, sz), 0.14)))

    # 5. WallBaseGrime: Horizontal capillary rising damp, mildew & efflorescence along floor skirting
    rise = np.clip(1.0 - (1.0 - y) * 1.35 + (n_macro - 0.5) * 0.48, 0.0, 1.0) ** 1.4
    salt_band = np.clip(np.exp(-((1.0 - y - 0.48 - (n_macro - 0.5) * 0.22) * 11.0) ** 2) * (0.4 + 0.8 * n_fine), 0.0, 1.0)
    g_alpha = np.clip((rise * 0.82 + salt_band * 0.42) * (sin_x ** 0.25), 0.0, 0.88)
    gr_r = 0.12 * (1.0 - salt_band) + 0.58 * salt_band + n_fine * 0.05
    gr_g = 0.16 * (1.0 - salt_band) + 0.58 * salt_band + n_fine * 0.05
    gr_b = 0.13 * (1.0 - salt_band) + 0.53 * salt_band + n_fine * 0.04
    save("WallBaseGrime", rgb(gr_r, gr_g, gr_b, g_alpha))
    save("WallBaseGrime_Normal", normal_from_height(salt_band * 0.35 + n_fine * 0.2, 3.5))

    # 6. SootSwatch (material reference swatch kept clean for Core 03/05)
    soot_a = np.clip((puddle_field - 0.18) * 2.2, 0.0, 0.85)
    save("SootSwatch", rgb(0.09 + n_fine * 0.05, 0.09 + n_fine * 0.04, 0.08 + n_fine * 0.04, soot_a))

    # 7. WheelMarks: Crisp, unmistakable 4-caster indentation & rubber scuff drag trails toward Store cabinet (E12/OBS101A)
    w_sz = 1024
    wheel = Image.new("RGBA", (w_sz, w_sz), (0, 0, 0, 0))
    d = ImageDraw.Draw(wheel)
    # Four bed caster resting indentations & swivel arc scuffs forming an exact bed rectangle
    corners = [(220, 200), (760, 200), (220, 790), (760, 790)]
    for cx, cy in corners:
        # Dark accumulated dust halo around rubber wheel footprint
        d.ellipse((cx - 56, cy - 56, cx + 56, cy + 56), fill=(32, 30, 25, 110))
        # Swivel arc marks from hospital rubber casters
        d.arc((cx - 64, cy - 78, cx + 64, cy + 78), 10, 350, fill=(28, 25, 21, 215), width=15)
        d.arc((cx - 42, cy - 54, cx + 42, cy + 54), 30, 290, fill=(24, 21, 18, 175), width=9)
        d.ellipse((cx - 22, cy - 22, cx + 22, cy + 22), fill=(22, 19, 16, 205))
    # Parallel double rubber drag tracks leading out from the bay toward the storage doorway!
    for sy in (215, 775):
        for step in range(95):
            k = step / 94.0
            px = int(760 + k * 245)
            py = int(sy - k * (130 if sy > 500 else -45) + math.sin(k * 9.0) * 6.0)
            a = int((195 - k * 95) * (0.78 + 0.22 * math.sin(step * 1.7)))
            d.ellipse((px - 14, py - 9, px + 14, py + 9), fill=(26, 23, 19, max(45, a)))
    # Secondary diagonal skid from left casters moving out
    for sy in (210, 785):
        for step in range(70):
            k = step / 69.0
            px = int(220 + k * 540)
            py = int(sy + math.sin(k * 5.0) * 14.0)
            a = int(145 * (1.0 - k * 0.45))
            d.ellipse((px - 11, py - 7, px + 11, py + 7), fill=(30, 27, 22, max(40, a)))
    wheel = wheel.filter(ImageFilter.GaussianBlur(2.0))
    wheel.save(DEST / "WheelMarks.png")

    # 8. ContactAO: Rich soft grounding ambient occlusion decal placed beneath beds, cabinets & furniture
    csz = 512
    cy_g, cx_g = np.mgrid[0:csz, 0:csz].astype(np.float32) / (csz - 1) * 2.0 - 1.0
    box_d = np.maximum(np.abs(cx_g) ** 3.2, np.abs(cy_g) ** 3.2)
    soft_ao = np.clip(1.0 - box_d, 0.0, 1.0) ** 1.35
    core_ao = np.exp(-(cx_g * cx_g * 2.6 + cy_g * cy_g * 1.4))
    ao_a = np.clip((soft_ao * 0.62 + core_ao * 0.42) * 0.72, 0.0, 0.78)
    save("BedContact", rgb(np.full((csz, csz), 0.05), np.full((csz, csz), 0.06), np.full((csz, csz), 0.05), ao_a))


def typeface(size, strong=False):
    return ImageFont.truetype(str(RAW / ("BeVietnamPro-SemiBold.ttf" if strong else "BeVietnamPro-Regular.ttf")), size)


def add_paper_grain(img, seed, foxing=0.35):
    w, h = img.size
    arr = np.asarray(img.convert("RGB"), dtype=np.float32) / 255.0
    grain = fbm(max(w, h), [8, 16, 32, 64], seed=seed)[:h, :w]
    edge_x = np.linspace(-1, 1, w)[None, :]
    edge_y = np.linspace(-1, 1, h)[:, None]
    vig = np.clip((edge_x ** 2 + edge_y ** 2) * 0.55, 0.0, 1.0)
    arr = arr * (0.94 + 0.12 * grain[..., None])
    # Warm aged foxing near borders
    arr = arr * (1.0 - vig[..., None] * foxing * 0.25) + np.array([0.46, 0.38, 0.24]) * (vig[..., None] * foxing * 0.25)
    # Subtle horizontal fold crease line across middle
    fold_y = np.exp(-((np.linspace(0, 1, h)[:, None] - 0.50) * 95.0) ** 2)
    arr -= fold_y[..., None] * 0.06
    return Image.fromarray(np.uint8(np.clip(arr, 0, 1) * 255))


def build_signs_and_documents():
    """High-craft aged Vietnamese enameled plaques & institutional forms (1987-2005, fictional An Sinh)."""
    def sign(name, big, small, palette=("e7dfc2", "233a33"), size=(1024, 390), seed=6001, age=0.45):
        paper, ink = ("#" + v for v in palette)
        width, height = size
        img = Image.new("RGB", size, paper)
        d = ImageDraw.Draw(img)
        # Double enamel border
        d.rounded_rectangle((14, 14, width - 14, height - 14), 6, outline=ink, width=8)
        d.rounded_rectangle((28, 28, width - 28, height - 28), 4, outline=ink, width=2)
        title_font, suffix_font = typeface(146, True), typeface(36, True)
        while d.textlength(big, font=title_font) > width - 135:
            title_font = typeface(title_font.size - 4, True)
        d.text((width / 2, height * (0.45 if small else 0.50)), big, font=title_font, fill=ink, anchor="mm")
        if small:
            d.text((width / 2, height * 0.78), small, font=suffix_font, fill=ink, anchor="mm")
        # Corner mounting screw holes with subtle rust drip halos
        for sx in (44, width - 44):
            for sy in (44, height - 44):
                d.ellipse((sx - 11, sy - 11, sx + 11, sy + 11), fill="#6d472d")
                d.ellipse((sx - 7, sy - 7, sx + 7, sy + 7), fill="#3d3a33")
                d.line((sx - 5, sy - 2, sx + 5, sy + 2), fill="#1b1a18", width=2)
        img = add_paper_grain(img, seed=seed, foxing=age)
        img.save(DEST / (name + ".png"))

    sign("Room101", "101", "PHÒNG BỆNH", ("e4dcbf", "273d34"), seed=6001, age=0.55)
    sign("Welcome", "AN SINH", "BỆNH VIỆN ĐA KHOA · 1987", ("2a4a3f", "eae8d2"), seed=6002, age=0.50)
    sign("Emergency", "CẤP CỨU", "LỐI CÁNG · TIẾP NHẬN", ("6a2e28", "f3e7ce"), seed=6003, age=0.45)
    sign("Station", "BÀN TRỰC", "KHOA NỘI TRÚ · TẦNG TRỆT", ("e6dec3", "263c35"), seed=6004, age=0.40)
    sign("FileA", "ĐÃ BÀN GIAO", "PHÒNG 101 · 0 TỒN ĐỌNG", ("dfe2ce", "2c4842"), seed=6005, age=0.25)
    sign("FileB", "CHỜ KIỂM TRA", "PHÒNG 101 · CHƯA KHÉP SỔ", ("e5e2ca", "4a4427"), seed=6006, age=0.25)
    sign("FileC", "CÓ SAI LỆCH", "PHÒNG 101 · CẦN ĐỐI CHIẾU", ("e7d8cc", "662d26"), seed=6007, age=0.25)
    sign("Demolition", "KHU VỰC ĐÓNG CỬA", "DỰ KIẾN THÁO DỠ 18/11/2012", ("c1b48a", "423121"), seed=6008, age=0.75)
    sign("Staff", "LỐI NHÂN VIÊN", "CẤP CỨU / KỸ THUẬT", ("c9c6aa", "2e3a32"), seed=6009, age=0.55)
    sign("Store", "KHO GIƯỜNG", "VẬT TƯ NỘI TRÚ", ("c5c0a2", "353b2e"), seed=6010, age=0.65)
    for k in range(1, 5):
        sign("Socket" + str(k), str(k), "", ("d4cdb2", "2b2e24"), (240, 212), seed=6010 + k, age=0.55)

    # Fictional hospital ward regulations board (AR03 clean)
    rules = Image.new("RGB", (768, 1024), "#dfd6ba")
    dr = ImageDraw.Draw(rules)
    dr.rectangle((24, 24, 744, 1000), outline="#2c443b", width=6)
    dr.rectangle((36, 36, 732, 988), outline="#556b5e", width=2)
    dr.text((384, 96), "BỆNH VIỆN AN SINH", font=typeface(30, True), fill="#2a443b", anchor="mm")
    dr.text((384, 162), "NỘI QUY BUỒNG BỆNH", font=typeface(44, True), fill="#21352d", anchor="mm")
    dr.line((80, 210, 688, 210), fill="#3c5449", width=3)
    items = [
        "1. Giữ trật tự, vệ sinh chung trong phòng bệnh.",
        "2. Người nhà chăm sóc không mang bếp nấu vào buồng.",
        "3. Giờ thăm bệnh: 11g00–13g00 và 16g30–20g00.",
        "4. Tắt đèn chính sau 21g30; giữ lối đi cho xe cáng.",
        "5. Chuyển giường hoặc chuyển khu phải có phiếu trực.",
        "6. Khi cần hỗ trợ, báo điều dưỡng tại bàn trực.",
    ]
    for idx, line in enumerate(items):
        dr.text((76, 260 + idx * 104), line, font=typeface(24), fill="#333c34")
    dr.text((545, 920), "BAN GIÁM ĐỐC", font=typeface(24, True), fill="#3a4c42", anchor="mm")
    add_paper_grain(rules, seed=6020, foxing=0.60).save(DEST / "WardRules.png")

    def form(name, heading, left, lines, stamp_text=None, seed=6030, struck_lines=(),
             footer="Bản lưu tại bàn trực · Mẫu nội bộ AS-05"):
        width, height = (770, 1024)
        img = Image.new("RGB", (width, height), "#d8c9a6")
        d = ImageDraw.Draw(img)
        d.rectangle((22, 22, width - 22, height - 22), outline="#7d775b", width=3)
        d.rectangle((30, 30, width - 30, height - 30), outline="#a49c7b", width=1)
        d.text((48, 48), "BỆNH VIỆN AN SINH  ·  KHOA NỘI TRÚ", font=typeface(25, True), fill="#3b4b3d")
        d.text((48, 114), heading, font=typeface(44, True), fill="#26382f")
        d.text((48, 200), left, font=typeface(25), fill="#433e34")
        for i, line in enumerate(lines):
            y_pos = 292 + i * 78
            d.line((46, y_pos - 18, width - 48, y_pos - 18), fill="#9a9271", width=2)
            d.text((48, y_pos), line, font=typeface(27, i == 1), fill="#3c3426")
        note_font = typeface(23)
        for index, line in enumerate(struck_lines):
            y = 652 + 36 * index
            d.text((48, y), line, font=note_font, fill="#3c3426")
            end = 48 + d.textlength(line, font=note_font)
            d.line((47, y + 16, end + 5, y + 12), fill="#745443", width=2)
        d.line((46, 845, width - 48, 845), fill="#847958", width=2)
        d.text((48, 882), footer, font=typeface(22), fill="#5d523d")
        if stamp_text:
            # Red faded Vietnamese institutional rectangular rubber stamp
            st_box = (465, 675, 712, 790)
            d.rectangle(st_box, outline="#8d362d", width=4)
            d.rectangle((473, 683, 704, 782), outline="#8d362d", width=1)
            d.text((588, 718), "BỆNH VIỆN AN SINH", font=typeface(18, True), fill="#8d362d", anchor="mm")
            d.text((588, 754), stamp_text, font=typeface(23, True), fill="#8d362d", anchor="mm")
        add_paper_grain(img, seed=seed, foxing=0.65).save(DEST / (name + ".png"))

    form("F101", "PHIẾU BÀN GIAO", "18/11/2005   •   Phòng 101   •   F101",
         ("Số chưa bàn giao theo bản kê", "0  (Không)", "Đối chiếu thực tế trước khi ký.", "Người trực ca:  N. H. Minh"),
         stamp_text="BÀN TRỰC NỘI TRÚ", seed=6031)
    form("ObsCard", "THẺ BÀN GIAO", "18/11/2005   •   Phòng 101",
         ("Giường số 4", "CHƯA BÀN GIAO", "Kiểm tra lại vị trí chuyển.", "Ghi chú: chưa có chữ ký nhận."),
         stamp_text="CHỜ XÁC NHẬN", seed=6032)
    form("BedList", "BẢN KÊ GIƯỜNG", "Nội trú   •   Phòng 101   •   Tầng trệt",
         ("Vị trí 1    •    Vị trí 2    •    Vị trí 3", "Tổng theo bản kê: 03 giường", "Đính kèm sổ kiểm kê 2005."),
         stamp_text="ĐÃ KIỂM KÊ", seed=6033)
    form("Transfer", "PHIẾU CHUYỂN", "17/11/2005   •   Ghi tay, chưa nhập sổ",
         ("Bệnh nhi: An (mã C01)", "Khám sốt tạm tại giường 4 — Phòng 101",
          "Chuyển lại Khu C trong ngày", "Theo lệnh phòng Giám đốc"),
         seed=6034,
         struck_lines=("Sốt 39,8°C sau đợt truyền AS-05 lần 4.",
                       "Bé khóc hoảng loạn vì không nhớ tên mẹ,",
                       "ôm chặt bức tranh mới chịu ngủ.",
                       "Không thể để bé dưới hầm thêm một đêm nào nữa."),
         footer="Phiếu ghi tay · Lan")

    # An's crayon/colored-pencil drawing on aged drawing paper (richer handmade crayon stroke look!)
    drawing = Image.new("RGB", (700, 500), "#d2c39f")
    d = ImageDraw.Draw(drawing)
    # House walls & roof in warm brick-red crayon
    d.line(((88, 298), (92, 197), (307, 82), (550, 203), (546, 376), (93, 384), (88, 298)), fill="#9b4a3a", width=11)
    d.line(((94, 202), (308, 90), (543, 206)), fill="#b25946", width=5)
    # Heavy blue door with padlock (foreshadowing Area C locked door!)
    d.line(((193, 367), (199, 217), (404, 222), (407, 365)), fill="#3b4d62", width=12)
    d.line(((207, 357), (198, 218), (402, 219), (401, 363)), fill="#4f647c", width=5)
    d.arc((263, 280, 312, 336), 178, 357, fill="#51422e", width=8)
    d.rectangle((266, 310, 309, 342), outline="#51422e", width=7)
    # Small window
    d.line(((474, 286), (468, 233), (521, 239), (518, 290), (474, 286)), fill="#5e6d45", width=6)
    d.line(((495, 236), (493, 288)), fill="#5e6d45", width=3)
    d.line(((470, 261), (520, 264)), fill="#5e6d45", width=3)
    # Sun
    d.ellipse((569, 48, 623, 108), outline="#a4833d", width=7)
    for k in range(8):
        angle = k * math.tau / 8
        d.line(((596 + 34 * math.cos(angle), 77 + 34 * math.sin(angle)),
                (596 + 47 * math.cos(angle), 77 + 47 * math.sin(angle))), fill="#a4833d", width=4)
    add_paper_grain(drawing, seed=6040, foxing=0.70).save(DEST / "Drawing.png")

    # Bed-foot aluminum patient chart paper
    chart = Image.new("RGB", (512, 680), "#ddd2b4")
    dc = ImageDraw.Draw(chart)
    dc.rectangle((18, 18, 494, 662), outline="#5c6252", width=3)
    dc.text((256, 56), "PHIẾU THEO DÕI ĐIỀU TRỊ", font=typeface(24, True), fill="#2e3f36", anchor="mm")
    dc.text((40, 104), "Phòng: 101   ·   Khoa: Nội trú", font=typeface(20), fill="#3d433a")
    for row in range(9):
        yy = 165 + row * 50
        dc.line((36, yy, 476, yy), fill="#958d72", width=2)
    for col in (150, 290, 390):
        dc.line((col, 165, col, 615), fill="#958d72", width=2)
    add_paper_grain(chart, seed=6041, foxing=0.55).save(DEST / "ChartCard.png")


if __name__ == "__main__":
    DEST.mkdir(parents=True, exist_ok=True)
    build_walls()
    build_floor_tiles()
    build_wood_and_metals()
    build_tropical_leaf_atlas()
    build_authored_decals()
    build_signs_and_documents()
    print(f"Wrote {len(list(DEST.glob('*.png')))} M0-R PBR surfaces, decals, foliage & signs to {DEST}")
