"""Pass 2 High-Craft Character PBR Maps (Lan Face UV Atlas, Crisp Poplin Blouse, Dark Trousers,
Directional Scorched Gatekeeper Overcoat, Obscured Visage, Soft Pediatric Cotton).
"""

import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parent))
import bake_surfaces as BS

DEST = BS.DEST


def build_character_pbr_pass2():
    sz = 1024

    # --- 1. Crisp Fine-Weave Poplin Cotton (replaces oversized Fabric085 waffle on NurseBlouse!) ---
    warp = BS.fbm_aniso(sz, [256, 512], [64, 128], seed=8101)
    weft = BS.fbm_aniso(sz, [64, 128], [256, 512], seed=8102)
    weave = warp * 0.5 + weft * 0.5
    crease = BS.fbm(sz, [4, 8, 16], seed=8103)
    nb_col = np.full((sz, sz, 3), (0.85, 0.83, 0.76), dtype=np.float32) + BS.center(crease)[..., None] * 0.045 + BS.center(weave)[..., None] * 0.022
    nb_norm = BS.normal_from_height(weave * 0.16 + crease * 0.25, 1.35)
    nb_sm = np.clip(0.11 + BS.center(weave) * 0.03, 0.06, 0.17)
    nb_ao = np.clip(0.95 - (1.0 - crease) * 0.08, 0.82, 1.0)
    BS.map_set("NurseBlouse", nb_col, nb_norm, nb_sm, nb_ao)

    # --- 2. DarkTrouser (Institutional Charcoal-Navy Tailored Twill) ---
    twill = BS.fbm_aniso(sz, [128, 256], [128, 256], seed=8110)
    dt_col = np.full((sz, sz, 3), (0.105, 0.125, 0.155), dtype=np.float32) + BS.center(crease)[..., None] * 0.025 + BS.center(twill)[..., None] * 0.015
    dt_norm = BS.normal_from_height(twill * 0.18 + crease * 0.22, 1.4)
    BS.map_set("DarkTrouser", dt_col, dt_norm, np.full((sz, sz), 0.12, np.float32), np.full((sz, sz), 0.90, np.float32))

    # --- 3. CharredBlouse (Gatekeeper: realistic vertical smoke-charred hospital overcoat, NO polka dots!) ---
    yy, xx = np.mgrid[:sz, :sz].astype(np.float32) / sz
    v_smoke = BS.fbm_aniso(sz, [16, 32, 64], [4, 8, 16], seed=8120)
    ash_fine = BS.fbm(sz, [32, 64, 128], seed=8121)
    # Dark ash & heavy soot wash with weathered fabric grain
    soot_blend = np.clip(0.45 + (v_smoke - 0.5) * 0.95 + (ash_fine - 0.5) * 0.35, 0.0, 1.0)
    base_cloth = np.full((sz, sz, 3), (0.27, 0.26, 0.23), dtype=np.float32) + BS.center(weave)[..., None] * 0.03
    deep_char = np.full((sz, sz, 3), (0.048, 0.046, 0.045), dtype=np.float32) + BS.center(ash_fine)[..., None] * 0.02
    singe = np.clip(soot_blend * (1.0 - soot_blend) * 3.5, 0.0, 1.0)
    cb_col = base_cloth * (1.0 - soot_blend[..., None]) + deep_char * soot_blend[..., None]
    cb_col += singe[..., None] * np.array([0.075, 0.042, 0.022], dtype=np.float32)
    cb_norm = BS.normal_from_height(weave * 0.20 + crease * 0.30 + ash_fine * 0.14, 1.8)
    cb_sm = np.clip(0.08 - soot_blend * 0.04, 0.03, 0.12)
    BS.map_set("CharredBlouse", cb_col, cb_norm, cb_sm, np.clip(0.88 - soot_blend * 0.25, 0.45, 1.0))

    # --- 4. ObscuredFace (Gatekeeper shadowed dark featureless visage) ---
    ob_n = BS.fbm_aniso(sz, [24, 48], [6, 12], seed=8125)
    ob_col = np.full((sz, sz, 3), (0.075, 0.078, 0.076), dtype=np.float32) + BS.center(ob_n)[..., None] * 0.028
    BS.map_set("ObscuredFace", ob_col, BS.normal_from_height(ob_n, 1.5), np.full((sz, sz), 0.10, np.float32), np.full((sz, sz), 0.72, np.float32))

    # --- 5. AnCotton (Soft smooth pediatric cotton stripes — no giant waffle bump!) ---
    stripe = ((np.mgrid[:sz, :sz][1] // 24) % 2).astype(np.float32)
    ac_col = np.full((sz, sz, 3), (0.76, 0.80, 0.79), dtype=np.float32) * (1.0 - stripe[..., None] * 0.14)
    ac_col += stripe[..., None] * np.array([0.06, 0.11, 0.15], dtype=np.float32) + BS.center(weave)[..., None] * 0.02
    BS.map_set("AnCotton", ac_col, BS.normal_from_height(weave * 0.15 + crease * 0.20, 1.1), np.full((sz, sz), 0.10, np.float32), np.full((sz, sz), 0.92, np.float32))

    # --- 6. LanHair (Natural matte-satin dark Vietnamese black-brown hair — NOT shiny plastic!) ---
    strands = BS.fbm_aniso(sz, [128, 256, 512], [6, 12, 24], seed=8130)
    lh_col = np.full((sz, sz, 3), (0.048, 0.038, 0.032), dtype=np.float32) + strands[..., None] * 0.026
    BS.map_set("LanHair", lh_col, BS.normal_from_height(strands, 2.2), np.clip(0.28 + strands * 0.10, 0.20, 0.42), np.clip(0.75 + strands * 0.22, 0.55, 1.0))

    # --- 7. LanFace UV Portrait Texture (cylindrical UV: u=0.5 front -Y, v=0..1 chin->crown) ---
    # Matches exact UV projection in build_characters.py:
    # u = 0.5 + atan2(x, -y) / (2*pi)   -> front center is u = 0.50 (x = 512)
    # v = 0.5 + (z - head_z) / (2.2*sz) -> vertical center is v = 0.50 (y_img = 512 in PIL where y=0 is top!)
    pores = BS.fbm(sz, [64, 128, 256], seed=8140)
    var = BS.fbm(sz, [4, 8, 16], seed=8141)
    # Base warm Vietnamese skin
    face = np.full((sz, sz, 3), (0.78, 0.60, 0.48), dtype=np.float32)
    face[..., 0] += BS.center(var) * 0.032 + BS.center(pores) * 0.012
    face[..., 1] += BS.center(var) * 0.022 + BS.center(pores) * 0.010
    face[..., 2] += BS.center(var) * 0.018 + BS.center(pores) * 0.008

    # PIL overlay for delicate anatomical skin tones, soft brows, lash shadows, cheeks & natural lips!
    # In PIL coordinates: x_px = u * sz, y_px = (1.0 - v) * sz!
    yy_f, xx_f = np.mgrid[:sz, :sz].astype(np.float32) / sz
    u_g = xx_f
    v_g = 1.0 - yy_f  # v=0.5 is center, v=0.545 is eyes, v=0.615 is brows, v=0.285 is lips!

    def gauss(u0, v0, su, sv):
        return np.exp(-(((u_g - u0) / su) ** 2 + ((v_g - v0) / sv) ** 2))

    # Subtle warm rose cheek blush & nose-tip warmth
    blush = gauss(0.425, 0.445, 0.045, 0.048) + gauss(0.575, 0.445, 0.045, 0.048) + 0.55 * gauss(0.500, 0.430, 0.022, 0.025)
    face = face * (1.0 - blush[..., None] * 0.16) + np.array([0.82, 0.46, 0.40]) * (blush[..., None] * 0.16)

    # Gentle orbital / under-brow & nose-side soft ambient shadow (gives natural depth!)
    orb_sh = gauss(0.446, 0.552, 0.032, 0.026) + gauss(0.554, 0.552, 0.032, 0.026)
    under_chin = np.clip((0.20 - v_g) * 6.0, 0.0, 1.0)
    face *= (1.0 - orb_sh[..., None] * 0.14 - under_chin[..., None] * 0.12)

    # Soft eyelid crease & natural dark lash line
    for sgn in (-1.0, 1.0):
        eu = 0.50 + sgn * 0.056
        # Delicate arched upper eyelid lash shadow
        du = (u_g - eu) / 0.028
        arch_v = 0.548 + 0.008 * np.cos(np.clip(du, -1.5, 1.5) * 1.1)
        lash = np.exp(-((du) ** 2 + ((v_g - arch_v) / 0.0055) ** 2))
        crease_v = arch_v + 0.015
        lid_crease = np.exp(-((du / 1.1) ** 2 + ((v_g - crease_v) / 0.004) ** 2))
        face = face * (1.0 - lash[..., None] * 0.72) + np.array([0.11, 0.08, 0.07]) * (lash[..., None] * 0.72)
        face = face * (1.0 - lid_crease[..., None] * 0.22) + np.array([0.46, 0.32, 0.26]) * (lid_crease[..., None] * 0.22)

        # Natural soft tapered Vietnamese eyebrows painted directly onto browbone!
        bu = (u_g - (0.50 + sgn * 0.058)) / 0.038
        brow_v = 0.608 + 0.009 * np.cos(np.clip(bu - sgn * 0.12, -1.4, 1.4) * 1.2) - sgn * bu * 0.004
        brow_taper = np.clip(1.0 - np.abs(bu) ** 2.2, 0.0, 1.0)
        brow_mask = np.exp(-((v_g - brow_v) / (0.0075 * (0.35 + 0.65 * brow_taper))) ** 2) * brow_taper * (0.78 + 0.22 * pores)
        face = face * (1.0 - brow_mask[..., None] * 0.82) + np.array([0.13, 0.095, 0.078]) * (brow_mask[..., None] * 0.82)

    # Subtle nostril shading
    nostrils = gauss(0.486, 0.402, 0.0085, 0.006) + gauss(0.514, 0.402, 0.0085, 0.006)
    face = face * (1.0 - nostrils[..., None] * 0.42) + np.array([0.36, 0.24, 0.20]) * (nostrils[..., None] * 0.42)

    # Soft natural contoured Cupid's-bow lips (upper lip + fuller lower lip + gentle lip parting crease!)
    lu = (u_g - 0.50) / 0.042
    cupid = 0.308 + 0.004 * np.cos(np.clip(lu * 2.4, -math.pi, math.pi)) - 0.006 * (lu ** 2)
    lip_upper = np.exp(-((lu) ** 2 * 1.15 + ((v_g - cupid) / 0.0095) ** 2)) * np.clip(1.0 - np.abs(lu) ** 2.5, 0, 1)
    lip_lower = np.exp(-((lu * 1.08) ** 2 + ((v_g - (0.289 - 0.003 * lu ** 2)) / 0.0115) ** 2)) * np.clip(1.0 - np.abs(lu) ** 2.5, 0, 1)
    lip_mask = np.clip(lip_upper + lip_lower, 0.0, 1.0)
    lip_seam = np.exp(-((lu * 1.05) ** 2 + ((v_g - (0.299 - 0.003 * lu ** 2)) / 0.0026) ** 2))
    lip_col = np.array([0.64, 0.37, 0.34], dtype=np.float32)
    face = face * (1.0 - lip_mask[..., None] * 0.78) + lip_col * (lip_mask[..., None] * 0.78)
    face = face * (1.0 - lip_seam[..., None] * 0.45) + np.array([0.32, 0.17, 0.16]) * (lip_seam[..., None] * 0.45)

    # Soft hairline shading around upper forehead and back of head
    hairline = np.clip((v_g - 0.74) * 7.5, 0.0, 1.0)
    face = face * (1.0 - hairline[..., None] * 0.65) + np.array([0.14, 0.11, 0.09]) * (hairline[..., None] * 0.65)

    f_norm = BS.normal_from_height(pores * 0.18 + lip_mask * 0.25 - lip_seam * 0.18, 1.2)
    f_sm = np.clip(0.28 + lip_mask * 0.14 + BS.center(var) * 0.04, 0.18, 0.48)
    BS.map_set("LanSkin", face, f_norm, f_sm, np.full((sz, sz), 0.94, np.float32))
    print("Pass 2 character PBR maps (LanSkin portrait UV, crisp NurseBlouse, DarkTrouser, CharredBlouse, ObscuredFace, AnCotton) written!")


if __name__ == "__main__":
    build_character_pbr_pass2()
