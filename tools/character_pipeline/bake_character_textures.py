"""Generate physically calibrated 2048x2048 Digital Human PBR Textures for M2-C.

Sources & Provenance (all verified CC0-1.0 or 100% original project synthesis):
- Base skin atlases: MakeHuman CC0 System Assets (`young_asian_female`, `young_asian_male`, `middleage_asian_male`)
- Eye atlas: MakeHuman CC0 System Assets (`eyes/materials/brown_eye.png`)
- Teeth & tongue atlases: MakeHuman CC0 System Assets (`teeth_base/teeth.png`, `tongue01/tongue01_diffuse.png`)
- Eyebrows & eyelashes: MakeHuman CC0 System Assets (`eyebrow002`, `eyelashes02`)
- Regional skin anatomical masks: MPFB2 CC0 (`mpfb_face`, `mpfb_lips`, `mpfb_eyelids`, `mpfb_ears`, `mpfb_fingernails`)
- Micro-pore detail normal, cotton poplin/twill/scorched fabric maps, LanBadge: 100% original project synthesis.

Writes both Unity runtime textures (Prototype/Assets/CTCC/Art/Textures/) and editable source masters
(ArtSource/Characters/Textures/).
"""

import math
import os
import shutil
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools"))
import bake_surfaces as BS

TEX_DIR = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Textures"
SRC_TEX_DIR = ROOT / "ArtSource" / "Characters" / "Textures"
FONT_BOLD = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Resources" / "CTCC" / "BeVietnamPro-SemiBold.ttf"
FONT_SEMI = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Resources" / "CTCC" / "BeVietnamPro-Regular.ttf"

BLENDER_EXTENSIONS = Path(os.environ.get(
    "BLENDER_USER_EXTENSIONS",
    Path(os.environ.get("APPDATA", Path.home())) / "Blender Foundation/Blender/4.5/extensions",
))
MPFB_EXT = Path(os.environ.get("CTCC_MPFB_EXTENSION_DATA", BLENDER_EXTENSIONS / "blender_org/mpfb/data"))
MPFB_USER = Path(os.environ.get("CTCC_MPFB_ASSET_DATA", BLENDER_EXTENSIONS / ".user/blender_org/mpfb/data"))


def load_rgb(path: Path, size=None) -> np.ndarray:
    im = Image.open(path).convert("RGB")
    if size is not None and im.size != (size, size):
        im = im.resize((size, size), Image.Resampling.LANCZOS)
    return np.asarray(im, dtype=np.float32) / 255.0


def load_rgba(path: Path, size=None) -> np.ndarray:
    im = Image.open(path).convert("RGBA")
    if size is not None and im.size != (size, size):
        im = im.resize((size, size), Image.Resampling.LANCZOS)
    return np.asarray(im, dtype=np.float32) / 255.0


def load_mask_gray(path: Path, size=2048) -> np.ndarray:
    if not path.exists():
        return np.zeros((size, size), dtype=np.float32)
    im = Image.open(path).convert("L")
    if im.size != (size, size):
        im = im.resize((size, size), Image.Resampling.BILINEAR)
    return np.asarray(im, dtype=np.float32) / 255.0


def save_rgb(name: str, arr: np.ndarray):
    img = Image.fromarray(np.clip(arr * 255.0 + 0.5, 0, 255).astype(np.uint8), "RGB")
    p = TEX_DIR / f"{name}.png"
    img.save(p, optimize=True)
    shutil.copy2(p, SRC_TEX_DIR / p.name)


def save_rgba(name: str, arr: np.ndarray):
    img = Image.fromarray(np.clip(arr * 255.0 + 0.5, 0, 255).astype(np.uint8), "RGBA")
    p = TEX_DIR / f"{name}.png"
    img.save(p, optimize=True)
    shutil.copy2(p, SRC_TEX_DIR / p.name)


def build_skin_set(prefix: str, src_diffuse: Path, tone_mul, tone_bias, lip_tint, scalp_darken,
                   age_detail=0.0, soot_veil=0.0, seed=9100):
    """Build 2048x2048 Digital Human Skin Color, Normal & Mask (R=0, G=AO, B=SSS/Thickness, A=Smoothness)."""
    sz = 2048
    base = load_rgb(src_diffuse, sz)

    # Clean bottom-right unused margin watermark cleanly (outside UV islands, x>1940, y>1650)
    margin_col = np.median(base[1600:1640, 1960:2020], axis=(0, 1))
    base[1650:, 1940:] = margin_col

    # Load MPFB2 CC0 anatomical region masks for hm08 UV layout
    m_face = load_mask_gray(MPFB_EXT / "textures/mpfb_face.jpg", sz)
    m_lips = load_mask_gray(MPFB_EXT / "textures/mpfb_lips.jpg", sz)
    m_lids = load_mask_gray(MPFB_EXT / "textures/mpfb_eyelids.jpg", sz)
    m_ears = load_mask_gray(MPFB_EXT / "textures/mpfb_ears.jpg", sz)
    m_nails = load_mask_gray(MPFB_EXT / "textures/mpfb_fingernails.jpg", sz)
    m_sss_raw = load_mask_gray(MPFB_EXT / "textures/sss.png", sz)
    m_scalp = load_mask_gray(MPFB_EXT / "uv_layers/scalp_solid.png", sz)

    # Calibrate warm Southeast Asian / Vietnamese skin tone smoothly across entire hm08 UV
    # (face, neck, ears, forearms, hands stay 100% color-matched!)
    col = np.clip(base * np.array(tone_mul, dtype=np.float32) + np.array(tone_bias, dtype=np.float32), 0.0, 1.0)

    # Subtle micro-skin variation (melanin & capillary mottling)
    var_macro = BS.center(BS.fbm(sz // 2, [6, 12, 24], seed=seed + 1))
    var_macro = np.asarray(Image.fromarray(((var_macro + 0.5) * 255).clip(0, 255).astype(np.uint8)).resize((sz, sz), Image.Resampling.BILINEAR), dtype=np.float32) / 255.0 - 0.5
    pores = BS.center(BS.fbm(sz // 2, [64, 128, 256], seed=seed + 2))
    pores = np.asarray(Image.fromarray(((pores + 0.5) * 255).clip(0, 255).astype(np.uint8)).resize((sz, sz), Image.Resampling.BILINEAR), dtype=np.float32) / 255.0 - 0.5

    col[..., 0] += var_macro * 0.022 + pores * 0.012
    col[..., 1] += var_macro * 0.015 + pores * 0.010
    col[..., 2] += var_macro * 0.012 + pores * 0.008

    # Natural subtle lip hemoglobin & moisture richness (never painted lipstick — natural 23yo nurse!)
    if lip_tint is not None:
        lt = np.array(lip_tint, dtype=np.float32)
        col = col * (1.0 - m_lips[..., None] * 0.40) + lt * (m_lips[..., None] * 0.40)

    # Subtle orbital eyelid depth & tired night-shift lower-lid tone so eyes sit grounded in sockets
    col *= (1.0 - m_lids[..., None] * 0.12)

    # Darken scalp area under hair to natural Vietnamese dark-brown/black root tone
    if scalp_darken > 0.0:
        # Fallback scalp mask from head UV island back-of-head brown stubble if scalp_solid is absent
        if m_scalp.max() < 0.05:
            # In hm08 UV, head island is on the right half (x > 1280, y in [340..1740])
            # and scalp stubble is darker than face skin
            yy, xx = np.mgrid[:sz, :sz].astype(np.float32) / sz
            lum = base[..., 0] * 0.299 + base[..., 1] * 0.587 + base[..., 2] * 0.114
            m_scalp = ((xx > 0.62) & (yy > 0.16) & (yy < 0.86) & (lum < 0.56) & (m_face < 0.25) & (m_ears < 0.25)).astype(np.float32)
            m_scalp = np.asarray(Image.fromarray((m_scalp * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(6)), dtype=np.float32) / 255.0
        scalp_col = np.array([0.085, 0.065, 0.055], dtype=np.float32)
        col = col * (1.0 - m_scalp[..., None] * scalp_darken) + scalp_col * (m_scalp[..., None] * scalp_darken)

    # Optional soot/ash weathering (for Gatekeeper's obscured hands/face)
    if soot_veil > 0.0:
        soot_noise = BS.fbm(sz // 2, [8, 16, 32, 64], seed=seed + 9)
        soot_noise = np.asarray(Image.fromarray((soot_noise * 255).clip(0, 255).astype(np.uint8)).resize((sz, sz), Image.Resampling.BILINEAR), dtype=np.float32) / 255.0
        soot_m = np.clip(soot_veil * (0.65 + 0.55 * soot_noise), 0.0, 0.96)
        ash = np.array([0.085, 0.082, 0.078], dtype=np.float32)
        col = col * (1.0 - soot_m[..., None]) + ash * soot_m[..., None]

    col = np.clip(col, 0.02, 0.96)

    # --- Normal map: fine skin micro-pores + gentle lip & eyelid micro-relief + optional age lines ---
    # Derive subtle high-frequency luminance detail from photo skin + procedural pores
    gray = base[..., 0] * 0.299 + base[..., 1] * 0.587 + base[..., 2] * 0.114
    blur_g = np.asarray(Image.fromarray((gray * 255).clip(0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(5)), dtype=np.float32) / 255.0
    photo_hf = gray - blur_g
    height = photo_hf * (0.45 + age_detail * 0.65) + pores * (0.28 + 0.20 * m_face) + m_lips * 0.14
    normal = BS.normal_from_height(height, 1.35 + age_detail * 0.6)

    # --- Mask Map (Linear RGBA): R=Metallic(0), G=AO/Cavity, B=SSS/Thickness, A=Smoothness ---
    ao = np.clip(0.95 - m_lids * 0.14 - m_ears * 0.10 - np.clip(-photo_hf * 2.5, 0.0, 0.18), 0.58, 1.0)
    # SSS thickness/curvature: ears, lips, nostrils, fingertips scatter warmly; thick bone areas less
    sss = np.clip(0.42 + m_face * 0.22 + m_ears * 0.36 + m_lips * 0.28 + m_sss_raw * 0.18, 0.18, 1.0)
    if soot_veil > 0.0:
        sss *= (1.0 - soot_veil * 0.78)
    # Per-zone smoothness: lips (0.56), eyelids (0.44), nails (0.64), T-zone/face (0.36..0.44), body (0.30)
    smooth = np.clip(0.30 + m_face * 0.09 + m_lids * 0.11 + m_lips * 0.24 + m_nails * 0.32 + pores * 0.05, 0.16, 0.72)
    if soot_veil > 0.0:
        smooth = np.clip(smooth * (1.0 - soot_veil * 0.62), 0.08, 0.30)

    mask = np.zeros((sz, sz, 4), dtype=np.float32)
    mask[..., 0] = 0.0
    mask[..., 1] = ao
    mask[..., 2] = sss
    mask[..., 3] = smooth

    save_rgb(f"{prefix}_Color", col)
    save_rgb(f"{prefix}_Normal", normal)
    save_rgba(f"{prefix}_Mask", mask)
    print(f"[TEXTURE] {prefix}_Color/Normal/Mask (2048x2048) built from CC0 {src_diffuse.name}")


def build_micro_pore_detail():
    """Seamless 512x512 fine dermal pore & micro-wrinkle detail normal map for close-up portrait realism."""
    sz = 512
    # Multi-scale isotropic cellular + fBm micro-pores
    n1 = BS.fbm(sz, [16, 32, 64, 128], seed=9401)
    n2 = BS.fbm(sz, [48, 96, 192], seed=9402)
    # Sharp tiny dimple pits (real human pores are tiny concave indentations, not lumpy bumps!)
    pits = np.clip((n2 - 0.54) * 5.0, 0.0, 1.0) ** 1.6
    h = n1 * 0.45 - pits * 0.55
    norm = BS.normal_from_height(h, 1.8)
    save_rgb("SkinMicroPore_Normal", norm)
    print("[TEXTURE] SkinMicroPore_Normal (512x512 seamless) built")


def build_eye_and_mouth_textures():
    """Calibrate CC0 MakeHuman eye, teeth, tongue, eyebrow & eyelash textures for authentic Vietnamese realism."""
    # 1. Lan Eye: deepen CC0 brown_eye.png to rich warm Vietnamese espresso-brown iris + crisp sclera
    eye_src = MPFB_USER / "eyes/materials/brown_eye.png"
    eye = load_rgba(eye_src, 1024)
    rgb = eye[..., :3]
    # Detect iris pixels (darker & warmer-red than gray/white sclera)
    lum = rgb[..., 0] * 0.299 + rgb[..., 1] * 0.587 + rgb[..., 2] * 0.114
    iris_mask = np.clip((0.46 - lum) * 3.2, 0.0, 1.0)
    # Deepen iris to authentic dark Vietnamese espresso-black-brown (minimal red haze, rich warm pupil depth!)
    dark_iris = np.stack([
        np.power(np.clip(rgb[..., 0], 0.0, 1.0), 1.62) * 0.44,
        np.power(np.clip(rgb[..., 1], 0.0, 1.0), 1.72) * 0.34,
        np.power(np.clip(rgb[..., 2], 0.0, 1.0), 1.82) * 0.28,
    ], axis=-1)
    # Keep sclera slightly warmer & brighter natural off-white (preserving the subtle red capillary veins!)
    warm_sclera = np.clip(rgb * np.array([1.14, 1.11, 1.06], dtype=np.float32), 0.0, 0.94)
    out_eye = warm_sclera * (1.0 - iris_mask[..., None]) + dark_iris * iris_mask[..., None]
    save_rgb("LanEye_Color", out_eye)

    # Radial iris fiber normal map for subtle concave stroma relief
    iris_h = (rgb[..., 0] - rgb[..., 2]) * iris_mask * 0.8 + lum * iris_mask * 0.4
    save_rgb("LanEye_Normal", BS.normal_from_height(iris_h, 2.4))

    # 2. Teeth & Tongue (direct CC0 MakeHuman calibrated for natural ivory enamel & moist oral mucosa)
    teeth = load_rgba(MPFB_USER / "teeth/teeth_base/teeth.png", 1024)
    teeth[..., :3] = np.clip(teeth[..., :3] * np.array([0.96, 0.94, 0.88], dtype=np.float32), 0.0, 0.95)
    save_rgb("LanTeeth_Color", teeth[..., :3])

    tongue = load_rgb(MPFB_USER / "tongue/tongue01/tongue01_diffuse.png", 512)
    tongue = np.clip(tongue * np.array([0.88, 0.76, 0.76], dtype=np.float32), 0.0, 0.92)
    save_rgb("LanTongue_Color", tongue)

    # 3. Eyebrows & Eyelashes (direct CC0 MakeHuman eyebrow001 soft natural arc & eyelashes02)
    brow = load_rgba(MPFB_USER / "eyebrows/eyebrow001/eyebrow001.png", 1024)
    brow[..., 0] = brow[..., 0] * 0.24 + 0.020
    brow[..., 1] = brow[..., 1] * 0.20 + 0.016
    brow[..., 2] = brow[..., 2] * 0.18 + 0.014
    brow[..., 3] = np.clip(brow[..., 3] * 1.12, 0.0, 1.0)
    save_rgba("LanEyebrow_Color", brow)

    lash = load_rgba(MPFB_USER / "eyelashes/eyelashes02/eyelashes02.png", 1024)
    lash[..., :3] = np.array([0.045, 0.036, 0.032], dtype=np.float32)
    lash[..., 3] = np.clip(lash[..., 3] * 1.10, 0.0, 1.0)
    save_rgba("LanEyelash_Color", lash)
    print("[TEXTURE] LanEye, LanTeeth, LanTongue, LanEyebrow, LanEyelash built from CC0 MakeHuman assets")


def build_garment_textures():
    """2048x2048 period-accurate 1997-2005 Vietnamese hospital garment PBR maps + crisp readable Lan ID badge."""
    sz = 2048
    # 1. NurseBlouse: fine woven cotton poplin (áo blouse trắng ngả vàng per Core 02 §2.02 & MoH 1895/1997)
    warp = BS.fbm_aniso(sz // 2, [256, 512], [64, 128], seed=8201)
    weft = BS.fbm_aniso(sz // 2, [64, 128], [256, 512], seed=8202)
    weave = warp * 0.5 + weft * 0.5
    crease = BS.fbm(sz // 2, [4, 8, 16, 32], seed=8203)
    weave2k = np.asarray(Image.fromarray((weave * 255).clip(0, 255).astype(np.uint8)).resize((sz, sz), Image.Resampling.BILINEAR), dtype=np.float32) / 255.0
    crease2k = np.asarray(Image.fromarray((crease * 255).clip(0, 255).astype(np.uint8)).resize((sz, sz), Image.Resampling.BILINEAR), dtype=np.float32) / 255.0
    # Warm yellowed institutional off-white cotton
    nb_col = np.full((sz, sz, 3), (0.855, 0.832, 0.758), dtype=np.float32)
    nb_col += (crease2k - 0.5)[..., None] * 0.055 + (weave2k - 0.5)[..., None] * 0.025
    nb_norm = BS.normal_from_height(weave2k * 0.22 + crease2k * 0.32, 1.45)
    nb_mask = np.zeros((sz, sz, 4), dtype=np.float32)
    nb_mask[..., 1] = np.clip(0.94 - (1.0 - crease2k) * 0.14, 0.76, 1.0)
    nb_mask[..., 3] = np.clip(0.11 + (weave2k - 0.5) * 0.04, 0.05, 0.18)
    save_rgb("NurseBlouse_Color", nb_col)
    save_rgb("NurseBlouse_Normal", nb_norm)
    save_rgba("NurseBlouse_Mask", nb_mask)

    # 2. DarkTrouser: tailored institutional charcoal-navy cotton twill
    twill = BS.fbm_aniso(sz // 2, [192, 384], [192, 384], seed=8210)
    twill2k = np.asarray(Image.fromarray((twill * 255).clip(0, 255).astype(np.uint8)).resize((sz, sz), Image.Resampling.BILINEAR), dtype=np.float32) / 255.0
    dt_col = np.full((sz, sz, 3), (0.115, 0.135, 0.165), dtype=np.float32) + (crease2k - 0.5)[..., None] * 0.032 + (twill2k - 0.5)[..., None] * 0.018
    dt_norm = BS.normal_from_height(twill2k * 0.22 + crease2k * 0.28, 1.5)
    dt_mask = np.zeros((sz, sz, 4), dtype=np.float32)
    dt_mask[..., 1] = np.clip(0.92 - (1.0 - crease2k) * 0.12, 0.78, 1.0)
    dt_mask[..., 3] = np.clip(0.13 + (twill2k - 0.5) * 0.04, 0.07, 0.20)
    save_rgb("DarkTrouser_Color", dt_col)
    save_rgb("DarkTrouser_Normal", dt_norm)
    save_rgba("DarkTrouser_Mask", dt_mask)

    # 3. ClinicalShoe: soft worn dark matte leather clinical flats
    grain = BS.fbm(1024, [32, 64, 128, 256], seed=8220)
    scuff = BS.fbm(1024, [6, 12, 24], seed=8221)
    sh_col = np.full((1024, 1024, 3), (0.068, 0.065, 0.062), dtype=np.float32) + (scuff - 0.5)[..., None] * 0.035
    sh_norm = BS.normal_from_height(grain * 0.35 + scuff * 0.20, 1.6)
    sh_mask = np.zeros((1024, 1024, 4), dtype=np.float32)
    sh_mask[..., 1] = np.clip(0.90 - scuff * 0.15, 0.70, 1.0)
    sh_mask[..., 3] = np.clip(0.42 + (scuff - 0.5) * 0.20, 0.22, 0.62)
    save_rgb("ClinicalShoe_Color", sh_col)
    save_rgb("ClinicalShoe_Normal", sh_norm)
    save_rgba("ClinicalShoe_Mask", sh_mask)

    # 4. CharredBlouse (Gatekeeper 2048²: directional smoke-charred hospital overcoat with singed edges & matte soot)
    v_smoke = BS.fbm_aniso(sz // 2, [16, 32, 64], [4, 8, 16], seed=8230)
    ash_fine = BS.fbm(sz // 2, [32, 64, 128], seed=8231)
    v_smoke2k = np.asarray(Image.fromarray((v_smoke * 255).clip(0, 255).astype(np.uint8)).resize((sz, sz), Image.Resampling.BILINEAR), dtype=np.float32) / 255.0
    ash2k = np.asarray(Image.fromarray((ash_fine * 255).clip(0, 255).astype(np.uint8)).resize((sz, sz), Image.Resampling.BILINEAR), dtype=np.float32) / 255.0
    soot_blend = np.clip(0.56 + (v_smoke2k - 0.5) * 0.90 + (ash2k - 0.5) * 0.35, 0.0, 1.0)
    # Smoke-damaged institutional off-white cotton turning cool charcoal-ash black (less tan, authentic fire soot!)
    base_cloth = np.full((sz, sz, 3), (0.24, 0.245, 0.235), dtype=np.float32) + (weave2k - 0.5)[..., None] * 0.035
    deep_char = np.full((sz, sz, 3), (0.042, 0.044, 0.046), dtype=np.float32) + (ash2k - 0.5)[..., None] * 0.020
    singe = np.clip(soot_blend * (1.0 - soot_blend) * 3.4, 0.0, 1.0)
    cb_col = base_cloth * (1.0 - soot_blend[..., None]) + deep_char * soot_blend[..., None]
    cb_col += singe[..., None] * np.array([0.045, 0.028, 0.016], dtype=np.float32)
    cb_norm = BS.normal_from_height(weave2k * 0.25 + crease2k * 0.40 + ash2k * 0.18, 2.0)
    cb_mask = np.zeros((sz, sz, 4), dtype=np.float32)
    cb_mask[..., 1] = np.clip(0.88 - soot_blend * 0.28, 0.42, 1.0)
    cb_mask[..., 3] = np.clip(0.09 - soot_blend * 0.05, 0.02, 0.13)
    save_rgb("CharredBlouse_Color", cb_col)
    save_rgb("CharredBlouse_Normal", cb_norm)
    save_rgba("CharredBlouse_Mask", cb_mask)

    # 5. AnCotton (2048² soft pale blue-ivory pediatric hospital cotton stripes — preserved under apparition shader!)
    stripe = ((np.mgrid[:sz, :sz][1] // 40) % 2).astype(np.float32)
    ac_col = np.full((sz, sz, 3), (0.80, 0.84, 0.82), dtype=np.float32) * (1.0 - stripe[..., None] * 0.15)
    ac_col += stripe[..., None] * np.array([0.07, 0.12, 0.16], dtype=np.float32) + (weave2k - 0.5)[..., None] * 0.025
    ac_norm = BS.normal_from_height(weave2k * 0.18 + crease2k * 0.24, 1.2)
    ac_mask = np.zeros((sz, sz, 4), dtype=np.float32)
    ac_mask[..., 1] = np.clip(0.93 - (1.0 - crease2k) * 0.10, 0.80, 1.0)
    ac_mask[..., 3] = np.full((sz, sz), 0.10, dtype=np.float32)
    save_rgb("AnCotton_Color", ac_col)
    save_rgb("AnCotton_Normal", ac_norm)
    save_rgba("AnCotton_Mask", ac_mask)

    # 6. Scratched Laminated An Sinh Nurse ID Badge (1024x640, Core 02 §2.02: "bảng tên có vết xước", fictional AR03)
    bw, bh = 1024, 640
    im = Image.new("RGB", (bw, bh), (235, 229, 212))
    draw = ImageDraw.Draw(im)
    draw.rectangle([0, 0, bw, 136], fill=(42, 92, 82))
    draw.rectangle([20, 20, bw - 20, bh - 20], outline=(62, 108, 96), width=4)
    try:
        f_hdr = ImageFont.truetype(str(FONT_BOLD), 42)
        f_sub = ImageFont.truetype(str(FONT_SEMI), 26)
        f_name = ImageFont.truetype(str(FONT_BOLD), 52)
        f_role = ImageFont.truetype(str(FONT_SEMI), 34)
    except Exception:
        f_hdr = f_sub = f_name = f_role = ImageFont.load_default()
    draw.text((48, 28), "BỆNH VIỆN ĐA KHOA AN SINH", fill=(242, 240, 228), font=f_hdr)
    draw.text((48, 84), "THẺ NHÂN VIÊN Y TẾ · 2005", fill=(196, 224, 212), font=f_sub)
    # Portrait photo on left: embed Lan's real neutral-studio rendered portrait if available!
    draw.rectangle([52, 178, 272, 468], fill=(184, 194, 188), outline=(72, 96, 88), width=4)
    portrait_path = ROOT / "output" / "m2c" / "blender-qa" / "CTCC_CHR_Lan" / "portrait_front.png"
    if portrait_path.exists():
        p_im = Image.open(portrait_path).convert("RGB")
        # Crop cleanly around head & collar for an authentic Vietnamese 3x4 ID photo!
        pw, ph = p_im.size
        crop = p_im.crop((int(pw * 0.14), int(ph * 0.04), int(pw * 0.86), int(ph * 0.96)))
        crop = crop.resize((212, 282), Image.Resampling.LANCZOS)
        # Subtle vintage 2005 blue-tinted ID photo wash
        arr_p = np.asarray(crop, dtype=np.float32) / 255.0
        arr_p = np.clip(arr_p * np.array([0.94, 0.96, 0.99], dtype=np.float32) + 0.02, 0.0, 1.0)
        im.paste(Image.fromarray((arr_p * 255).astype(np.uint8)), (56, 182))
    else:
        draw.ellipse([104, 215, 220, 345], fill=(116, 98, 86))
        draw.pieslice([76, 345, 248, 520], 180, 360, fill=(224, 222, 208))
    draw.text((312, 204), "TRẦN NGỌC LAN", fill=(24, 34, 32), font=f_name)
    draw.text((312, 286), "Điều dưỡng · Khoa Nội", fill=(54, 72, 68), font=f_role)
    draw.text((312, 348), "Mã NV: AS-02-041", fill=(78, 88, 82), font=f_sub)
    draw.text((312, 400), "Phòng trực: Tầng trệt · 101", fill=(78, 88, 82), font=f_sub)
    # Authentic subtle laminate scratch across badge (Core 02 §2.02!)
    draw.line([(240, 170), (760, 465)], fill=(215, 212, 198), width=5)
    draw.line([(244, 175), (756, 470)], fill=(142, 146, 138), width=2)
    draw.line([(610, 190), (880, 310)], fill=(208, 206, 196), width=3)
    p_badge = TEX_DIR / "LanBadge.png"
    im.save(p_badge, optimize=True)
    shutil.copy2(p_badge, SRC_TEX_DIR / p_badge.name)
    print("[TEXTURE] Garment PBR maps (NurseBlouse, DarkTrouser, ClinicalShoe, CharredBlouse, AnCotton, LanBadge) written")


def build_cc0_hair_textures():
    """Convert verified CC0 MakeHuman hair atlases (ponytail01, short01, short04) to realistic Vietnamese hair PBR maps."""
    specs = [
        # (prefix, src_rel, melanin_mul, power, gray_streak)
        ("LanHair", "hair/ponytail01/ponytail01_diffuse.png", (0.28, 0.22, 0.18), 1.48, 0.0),
        ("AnHair", "hair/short01/short01_diffuse.png", (0.42, 0.36, 0.32), 1.20, 0.0),
        ("GatekeeperHair", "hair/short04/short04_diffuse.png", (0.34, 0.33, 0.32), 1.25, 0.22),
    ]
    for prefix, rel, mul, pwr, gray_streak in specs:
        src = MPFB_USER / rel
        if not src.exists():
            raise FileNotFoundError(f"Missing pinned CC0 hair source {src}")
        rgba = load_rgba(src, 2048)
        rgb = rgba[..., :3]
        alpha = rgba[..., 3]
        # CRITICAL: MakeHuman ponytail01/short01 PNGs have feathered white-painted borders (alpha=1 over white!)
        # and a tiny top-right corner watermark. Convert neutral white border pixels cleanly to alpha transparency
        # and lock every feathered hair edge RGB to deep dark hair root pigment so ZERO white halo ever appears!
        rgba[:110, 1650:, 3] = 0.0  # clear unused top-right corner watermark cleanly
        rgb = rgba[..., :3]
        alpha = rgba[..., 3]
        lum = rgb[..., 0] * 0.299 + rgb[..., 1] * 0.587 + rgb[..., 2] * 0.114
        max_c = np.max(rgb, axis=-1)
        min_c = np.min(rgb, axis=-1)
        sat = max_c - min_c
        # Neutral white/light-gray paint around feathered hairline edges -> convert luminance directly into soft alpha!
        white_bg = np.clip((lum - 0.42) * 2.8, 0.0, 1.0) * np.clip(1.0 - sat * 4.5, 0.0, 1.0)
        clean_alpha = np.clip(alpha * (1.0 - white_bg), 0.0, 1.0)
        # Keep dense hair interior opaque while smoothly feathering real individual strands
        clean_alpha = np.where(clean_alpha < 0.04, 0.0, clean_alpha)
        edge_clean = np.clip((clean_alpha - 0.10) / 0.70, 0.0, 1.0)
        dark_root = np.array([0.022, 0.017, 0.014], dtype=np.float32)

        # Preserve strand contrast while pulling color into deep natural Vietnamese dark brown-black
        strand_hi = np.clip((lum - 0.10) * 1.25, 0.0, 1.0) * (1.0 - white_bg)
        out_rgb = np.stack([
            np.power(np.clip(rgb[..., 0], 0.0, 1.0), pwr) * mul[0] + strand_hi * mul[0] * 0.16 + 0.012,
            np.power(np.clip(rgb[..., 1], 0.0, 1.0), pwr + 0.05) * mul[1] + strand_hi * mul[1] * 0.13 + 0.010,
            np.power(np.clip(rgb[..., 2], 0.0, 1.0), pwr + 0.10) * mul[2] + strand_hi * mul[2] * 0.10 + 0.009,
        ], axis=-1)
        if gray_streak > 0.0:
            streak = np.clip((lum - 0.35) * 2.4, 0.0, 1.0) * gray_streak * (1.0 - white_bg)
            out_rgb = out_rgb * (1.0 - streak[..., None]) + np.array([0.28, 0.27, 0.26], dtype=np.float32) * streak[..., None]
        # Replace ALL feathered/edge RGB with rich dark root pigment so mipmaps & alpha blending NEVER show white!
        out_rgb = out_rgb * edge_clean[..., None] + dark_root * (1.0 - edge_clean[..., None])
        out_rgb = np.clip(out_rgb, 0.012, 0.30)
        out_rgba = np.concatenate([out_rgb, clean_alpha[..., None]], axis=-1)
        save_rgba(f"{prefix}_Color", out_rgba)

        # Derive fine strand normal + anisotropic tangent flow from luminance gradients
        norm = BS.normal_from_height(lum * alpha, 2.2)
        save_rgb(f"{prefix}_Normal", norm)

        # Flow map: R=tangentX, G=tangentY (aligned perpendicular to luminance gradient!), B=strand root/ID variation
        gy, gx = np.gradient(lum)
        # Hair strand direction is perpendicular to cross-strand brightness gradient
        tx = -gy
        ty = gx
        mag = np.sqrt(tx * tx + ty * ty) + 1e-4
        tx = np.where(mag > 0.008, tx / mag, 0.0)
        ty = np.where(mag > 0.008, ty / mag, 1.0)
        # Keep consistent hemisphere (+Y bias) so specular bands stay coherent
        flip = np.where(ty < -0.25, -1.0, 1.0)
        tx = tx * flip
        ty = ty * flip
        flow = np.stack([
            np.clip(tx * 0.5 + 0.5, 0.0, 1.0),
            np.clip(ty * 0.5 + 0.5, 0.0, 1.0),
            np.clip(0.35 + lum * 0.55, 0.0, 1.0),
        ], axis=-1).astype(np.float32)
        save_rgb(f"{prefix}_Flow", flow)
        print(f"[TEXTURE] {prefix}_Color/Normal/Flow (2048x2048) built from CC0 {src.name}")


def main():
    TEX_DIR.mkdir(parents=True, exist_ok=True)
    SRC_TEX_DIR.mkdir(parents=True, exist_ok=True)

    # 1. CHAR002 Lan Skin (23yo Vietnamese young woman — authentic warm golden-ivory Southeast Asian complexion)
    build_skin_set(
        prefix="LanSkin",
        src_diffuse=MPFB_USER / "skins/young_asian_female/young_lightskinned_female_diffuse3.png",
        tone_mul=(0.84, 0.77, 0.65),
        tone_bias=(0.012, 0.008, -0.006),
        lip_tint=(0.58, 0.35, 0.31),
        scalp_darken=0.88,
        age_detail=0.0,
        soot_veil=0.0,
        seed=9100,
    )

    # 2. CHAR007 Bé An Skin (7-8yo Vietnamese child — authentic warm Southeast Asian child complexion)
    build_skin_set(
        prefix="AnSkin",
        src_diffuse=MPFB_USER / "skins/young_asian_female/young_lightskinned_female_diffuse3.png",
        tone_mul=(0.83, 0.75, 0.63),
        tone_bias=(0.010, 0.006, -0.006),
        lip_tint=(0.58, 0.36, 0.33),
        scalp_darken=0.90,
        age_detail=-0.25,
        soot_veil=0.0,
        seed=9200,
    )

    # 3. CHAR005 Gatekeeper Skin (shadowed, ash/soot-veiled gaunt adult male visage & hands — no gore!)
    build_skin_set(
        prefix="GatekeeperSkin",
        src_diffuse=MPFB_USER / "skins/middleage_asian_male/middleage_lightskinned_male_diffuse2.png",
        tone_mul=(0.42, 0.41, 0.40),
        tone_bias=(0.00, 0.00, 0.00),
        lip_tint=None,
        scalp_darken=0.92,
        age_detail=0.55,
        soot_veil=0.72,
        seed=9300,
    )

    build_micro_pore_detail()
    build_eye_and_mouth_textures()
    build_cc0_hair_textures()
    build_garment_textures()
    print("ALL M2-C DIGITAL HUMAN PBR TEXTURES COMPLETE.")


if __name__ == "__main__":
    main()
