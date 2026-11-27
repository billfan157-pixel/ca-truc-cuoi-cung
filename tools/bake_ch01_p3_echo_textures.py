"""CH01 Rebirth III Phase 3 — B08 Perceptual Echo Archival Imprint Texture Baker.

Generates 6 high-resolution (1024x1024 RGBA) Vietnamese archival perceptual afterimage textures
for the three canonical F101 branches (Affirmed / Unverified / Refuted):
  Tray / counter horizontal afterimages:
    - EchoB08_Affirmed.png   (circled '0' + sealed closing line, cold clinical phosphor-slate)
    - EchoB08_Unverified.png (open hesitation brackets + 'CHỜ KIỂM TRA', muted amber-parchment)
    - EchoB08_Refuted.png    (struck '0' + discrepancy cross-stroke + empty receiver signature box, damp sepia-teal)
  Center glazed partition optical glass reflections:
    - EchoB08_Glass_Affirmed.png
    - EchoB08_Glass_Unverified.png
    - EchoB08_Glass_Refuted.png

Strict canon boundary (Core 02 / Core 04 B08):
  - Perceptual echo of Minh's F101 decision ONLY.
  - Zero spoilers: NEVER prints Bé An, C01, Khu C, AS-05, or 03:17 death time.
  - Saved to both Prototype/Assets/CTCC/Art/Resources/CTCC/ (runtime lookup) and
    Prototype/Assets/CTCC/Art/Textures/ (scene material asset binding).
"""

from __future__ import annotations

import math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT_DIRS = [
    ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Resources" / "CTCC",
    ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Textures",
]
SIZE = 1024


FONT_DIR = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Resources" / "CTCC"


def load_font(bold: bool, size: int) -> ImageFont.FreeTypeFont:
    p = FONT_DIR / ("BeVietnamPro-SemiBold.ttf" if bold else "BeVietnamPro-Regular.ttf")
    if not p.exists():
        raise FileNotFoundError(f"Missing SIL OFL font: {p}")
    return ImageFont.truetype(str(p), size)


FONT_SERIF_BOLD = lambda s: load_font(True, s)
FONT_SERIF = lambda s: load_font(False, s)
FONT_MONO = lambda s: load_font(True, s)
FONT_SANS = lambda s: load_font(True, s)


def radial_soft_vignette(w: int, h: int, inner: float = 0.56, outer: float = 0.96) -> np.ndarray:
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    nx = (xx / (w - 1) - 0.5) * 2.0
    ny = (yy / (h - 1) - 0.5) * 2.0
    r = np.sqrt(nx * nx * 0.92 + ny * ny * 1.05)
    t = np.clip((r - inner) / max(1e-4, outer - inner), 0.0, 1.0)
    smooth = t * t * (3.0 - 2.0 * t)
    return 1.0 - smooth


def fine_archival_grain(seed: int, w: int, h: int) -> np.ndarray:
    rng = np.random.default_rng(seed)
    n1 = rng.normal(0.0, 1.0, (h // 4, w // 4)).astype(np.float32)
    img = Image.fromarray(np.clip((n1 * 0.25 + 0.5) * 255.0, 0, 255).astype(np.uint8), mode="L")
    img = img.resize((w, h), Image.Resampling.BICUBIC)
    arr = np.asarray(img, dtype=np.float32) / 255.0
    return 0.78 + 0.44 * arr


def draw_centered(draw: ImageDraw.ImageDraw, text: str, cx: float, cy: float, font, fill) -> None:
    bbox = draw.textbbox((0, 0), text, font=font)
    tw = bbox[2] - bbox[0]
    th = bbox[3] - bbox[1]
    draw.text((cx - tw * 0.5 - bbox[0], cy - th * 0.5 - bbox[1]), text, font=font, fill=fill)


def add_ledger_rules(draw: ImageDraw.ImageDraw, line_col: tuple[int, int, int, int], margin: int = 120) -> None:
    # Subtle hospital admission ledger ruling (clean, restrained, never noisy)
    for y in range(margin + 90, SIZE - margin, 68):
        draw.line([(margin, y), (SIZE - margin, y)], fill=line_col, width=2)
    # Double header rule & left/right margin column lines
    draw.line([(margin, margin + 62), (SIZE - margin, margin + 62)], fill=(line_col[0], line_col[1], line_col[2], min(255, int(line_col[3] * 1.45))), width=3)
    draw.line([(margin, margin + 72), (SIZE - margin, margin + 72)], fill=line_col, width=2)
    draw.line([(margin + 115, margin + 62), (margin + 115, SIZE - margin - 30)], fill=line_col, width=2)
    draw.line([(SIZE - margin - 115, margin + 62), (SIZE - margin - 115, SIZE - margin - 30)], fill=line_col, width=2)


def draw_hand_circle(draw: ImageDraw.ImageDraw, cx: float, cy: float, rx: float, ry: float,
                     color: tuple[int, int, int, int], width: int = 14, wobble_seed: int = 7) -> None:
    rng = np.random.default_rng(wobble_seed)
    pts = []
    steps = 72
    for i in range(steps + 4):
        ang = (i / steps) * 2.0 * math.pi - 0.35
        # Slight hand-drawn ink overlap at top-right closure
        rad_j = 1.0 + 0.032 * math.sin(ang * 3.0 + 0.4) + 0.018 * math.cos(ang * 5.0) + rng.normal(0.0, 0.006)
        x = cx + math.cos(ang) * rx * rad_j
        y = cy + math.sin(ang) * ry * rad_j
        pts.append((x, y))
    draw.line(pts, fill=color, width=width, joint="curve")


def draw_dashed_rect(draw: ImageDraw.ImageDraw, x0: int, y0: int, x1: int, y1: int,
                     color: tuple[int, int, int, int], dash: int = 24, gap: int = 14, width: int = 5) -> None:
    for x in range(x0, x1, dash + gap):
        draw.line([(x, y0), (min(x + dash, x1), y0)], fill=color, width=width)
        draw.line([(x, y1), (min(x + dash, x1), y1)], fill=color, width=width)
    for y in range(y0, y1, dash + gap):
        draw.line([(x0, y), (x0, min(y + dash, y1))], fill=color, width=width)
        draw.line([(x1, y), (x1, min(y + dash, y1))], fill=color, width=width)


def finish_perceptual(img: Image.Image, seed: int, glow_radius: float = 4.0,
                      glow_strength: float = 0.28, inner_v: float = 0.52, outer_v: float = 0.96) -> Image.Image:
    # Subtle optical retinal bloom + archival film grain + soft elliptical alpha fade
    arr = np.asarray(img, dtype=np.float32) / 255.0
    blurred = np.asarray(img.filter(ImageFilter.GaussianBlur(radius=glow_radius)), dtype=np.float32) / 255.0
    rgb = np.clip(arr[..., :3] * 0.92 + blurred[..., :3] * glow_strength, 0.0, 1.0)
    alpha = np.clip(arr[..., 3] + blurred[..., 3] * (glow_strength * 0.65), 0.0, 1.0)
    grain = fine_archival_grain(seed, SIZE, SIZE)
    vig = radial_soft_vignette(SIZE, SIZE, inner=inner_v, outer=outer_v)
    alpha = np.clip(alpha * vig * np.clip(grain, 0.72, 1.08), 0.0, 1.0)
    out = np.zeros_like(arr)
    out[..., :3] = rgb
    out[..., 3] = alpha
    return Image.fromarray(np.round(out * 255.0).astype(np.uint8), mode="RGBA")


def bake_tray_affirmed() -> Image.Image:
    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # Ultra-delicate phosphor mist veil (lets brushed stainless tray & terrazzo show cleanly beneath)
    d.rounded_rectangle([118, 118, SIZE - 118, SIZE - 118], radius=48, fill=(210, 228, 226, 22))
    add_ledger_rules(d, (186, 212, 210, 62))

    ink = (224, 240, 238, 235)
    sub = (188, 216, 214, 178)
    seal = (210, 234, 232, 210)

    draw_centered(d, "PHIẾU BÀN GIAO · PHÒNG 101", SIZE * 0.5, 162, FONT_MONO(30), sub)
    draw_centered(d, "SỐ NGƯỜI HIỆN DIỆN", SIZE * 0.5, 250, FONT_SERIF(34), sub)

    # Large stark '0' circled tightly in cold ink
    draw_centered(d, "0", SIZE * 0.5, 470, FONT_SERIF_BOLD(270), ink)
    draw_hand_circle(d, SIZE * 0.5, 476, 152, 166, seal, width=15, wobble_seed=11)

    # Clean double-ruled sealed closing line
    d.line([(230, 682), (SIZE - 230, 682)], fill=seal, width=6)
    d.line([(265, 696), (SIZE - 265, 696)], fill=sub, width=3)

    # Restrained rectangular filing stamp
    d.rectangle([286, 738, SIZE - 286, 822], outline=seal, width=5)
    draw_centered(d, "ĐÃ KÝ XÁC NHẬN", SIZE * 0.5, 780, FONT_SANS(36), ink)
    draw_centered(d, "N. H. Minh", SIZE * 0.5, 876, FONT_SERIF(34), sub)

    return finish_perceptual(img, seed=101, glow_radius=5.0, glow_strength=0.30)


def bake_tray_unverified() -> Image.Image:
    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # Ultra-delicate pale amber mist veil
    d.rounded_rectangle([118, 118, SIZE - 118, SIZE - 118], radius=48, fill=(230, 216, 186, 22))
    add_ledger_rules(d, (218, 200, 168, 60))

    ink = (242, 228, 198, 232)
    sub = (218, 202, 170, 180)
    mark = (238, 204, 146, 218)

    draw_centered(d, "PHIẾU BÀN GIAO · PHÒNG 101", SIZE * 0.5, 162, FONT_MONO(30), sub)
    draw_centered(d, "TRẠNG THÁI GHI NHẬN", SIZE * 0.5, 250, FONT_SERIF(34), sub)

    # Unclosed open brackets framing a faint '0' and hovering hesitation mark
    bx0, bx1, by0, by1 = 304, SIZE - 304, 336, 612
    arm = 54
    for (x0, y0, x1, y1) in [
        (bx0, by0, bx0 + arm, by0), (bx0, by0, bx0, by0 + arm),
        (bx1 - arm, by0, bx1, by0), (bx1, by0, bx1, by0 + arm),
        (bx0, by1, bx0 + arm, by1), (bx0, by1 - arm, bx0, by1),
        (bx1 - arm, by1, bx1, by1), (bx1, by1 - arm, bx1, by1),
    ]:
        d.line([(x0, y0), (x1, y1)], fill=mark, width=10)

    draw_centered(d, "0", SIZE * 0.45, 472, FONT_SERIF(210), (234, 220, 192, 165))
    draw_centered(d, "?", SIZE * 0.61, 456, FONT_SERIF_BOLD(150), mark)

    # Broken hesitation underline
    for x in range(250, SIZE - 250, 44):
        d.line([(x, 668), (min(x + 26, SIZE - 250), 668)], fill=mark, width=6)

    draw_dashed_rect(d, 266, 722, SIZE - 266, 816, mark, dash=28, gap=14, width=5)
    draw_centered(d, "CHỜ KIỂM TRA", SIZE * 0.5, 769, FONT_SANS(38), ink)
    draw_centered(d, "chưa khép sổ", SIZE * 0.5, 868, FONT_SERIF(32), sub)

    return finish_perceptual(img, seed=202, glow_radius=5.5, glow_strength=0.32)


def bake_tray_refuted() -> Image.Image:
    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # Ultra-delicate damp sepia-teal mist veil
    d.rounded_rectangle([118, 118, SIZE - 118, SIZE - 118], radius=48, fill=(198, 214, 206, 22))
    add_ledger_rules(d, (176, 198, 190, 62))

    ink = (232, 236, 224, 232)
    sub = (192, 212, 204, 182)
    strike = (232, 166, 146, 235)

    draw_centered(d, "PHIẾU BÀN GIAO · PHÒNG 101", SIZE * 0.5, 162, FONT_MONO(30), sub)
    draw_centered(d, "GHI NHẬN SAI LỆCH", SIZE * 0.5, 250, FONT_SERIF(34), sub)

    # Struck-through '0' with deliberate ink cross-stroke
    draw_centered(d, "0", SIZE * 0.5, 454, FONT_SERIF_BOLD(250), (212, 222, 216, 185))
    d.line([(358, 566), (666, 344)], fill=strike, width=16)
    d.line([(366, 356), (658, 554)], fill=(232, 166, 146, 155), width=9)

    # Empty dashed receiver signature box outline (canon echo of AS-05B blank signature!)
    draw_dashed_rect(d, 224, 636, SIZE - 224, 748, ink, dash=22, gap=14, width=5)
    draw_centered(d, "CHỮ KÝ NGƯỜI NHẬN: [ TRỐNG ]", SIZE * 0.5, 692, FONT_MONO(30), ink)

    d.line([(248, 804), (SIZE - 248, 804)], fill=strike, width=5)
    draw_centered(d, "PHIẾU KHÔNG KHỚP", SIZE * 0.5, 852, FONT_SANS(36), strike)

    return finish_perceptual(img, seed=303, glow_radius=5.0, glow_strength=0.30)


def bake_glass_reflection(branch: str) -> Image.Image:
    """Delicate vertical glass-pane afterimage aligned to LOC02 center sliding pane."""
    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    if branch == "Affirmed":
        tint = (196, 224, 226)
        accent = (224, 244, 246)
    elif branch == "Unverified":
        tint = (224, 206, 174)
        accent = (242, 222, 180)
    else:
        tint = (196, 214, 206)
        accent = (232, 176, 158)

    # Faint archival column hairline traces (zero hard disc/circle backing — pure retinal afterimage on glass!)
    for x in (250, SIZE - 250):
        d.line([(x, 220), (x, SIZE - 220)], fill=(tint[0], tint[1], tint[2], 38), width=2)
    for y in range(280, SIZE - 220, 104):
        d.line([(230, y), (SIZE - 230, y)], fill=(tint[0], tint[1], tint[2], 30), width=2)

    if branch == "Affirmed":
        draw_centered(d, "0", SIZE * 0.5, 468, FONT_SERIF_BOLD(290), (accent[0], accent[1], accent[2], 145))
        draw_hand_circle(d, SIZE * 0.5, 474, 165, 178, (accent[0], accent[1], accent[2], 160), width=12, wobble_seed=19)
        d.line([(270, 710), (SIZE - 270, 710)], fill=(accent[0], accent[1], accent[2], 130), width=5)
    elif branch == "Unverified":
        draw_centered(d, "0", SIZE * 0.44, 468, FONT_SERIF(230), (tint[0], tint[1], tint[2], 115))
        draw_centered(d, "?", SIZE * 0.62, 448, FONT_SERIF_BOLD(165), (accent[0], accent[1], accent[2], 155))
        draw_dashed_rect(d, 265, 290, SIZE - 265, 650, (accent[0], accent[1], accent[2], 125), dash=34, gap=22, width=6)
        draw_centered(d, "CHỜ KIỂM TRA", SIZE * 0.5, 740, FONT_MONO(36), (accent[0], accent[1], accent[2], 140))
    else:
        draw_centered(d, "0", SIZE * 0.5, 456, FONT_SERIF_BOLD(265), (tint[0], tint[1], tint[2], 120))
        d.line([(335, 588), (688, 326)], fill=(accent[0], accent[1], accent[2], 175), width=15)
        draw_dashed_rect(d, 260, 686, SIZE - 260, 782, (accent[0], accent[1], accent[2], 140), dash=26, gap=16, width=5)

    seed = {"Affirmed": 401, "Unverified": 402, "Refuted": 403}[branch]
    return finish_perceptual(img, seed=seed, glow_radius=7.5, glow_strength=0.40, inner_v=0.42, outer_v=0.92)


def save_all(name: str, image: Image.Image) -> None:
    for d in OUT_DIRS:
        d.mkdir(parents=True, exist_ok=True)
        path = d / name
        image.save(path, format="PNG")
    print(f"Wrote {name} ({image.size[0]}x{image.size[1]} RGBA) to {len(OUT_DIRS)} directories")


def main() -> None:
    save_all("EchoB08_Affirmed.png", bake_tray_affirmed())
    save_all("EchoB08_Unverified.png", bake_tray_unverified())
    save_all("EchoB08_Refuted.png", bake_tray_refuted())
    for branch in ("Affirmed", "Unverified", "Refuted"):
        save_all(f"EchoB08_Glass_{branch}.png", bake_glass_reflection(branch))


if __name__ == "__main__":
    main()
