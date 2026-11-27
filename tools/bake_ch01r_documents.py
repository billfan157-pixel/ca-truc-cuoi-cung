"""CH01-R High-Craft Vietnamese Hospital Paperwork & Diegetic Document Generator (1997-2005).

Grounded in Vietnam Ministry of Health hospital record conventions (QĐ 1895/1997/QĐ-BYT &
QĐ 4069/2001/QĐ-BYT) while strictly preserving fictional Bệnh viện Đa khoa An Sinh branding (AR03)
and SIL OFL 1.1 typography (Be Vietnam Pro) + original procedural ballpoint handwriting/stamps.

Generates front/back 1024x1360 document textures, signed F101 board variants, contextual
comparison documents (Bed 2 completed transfer, Bed 3 routine medication log), nursing alcove
records, readable Bảy envelope/letter faces, and subtle CH01-PR01 corner foreshadowing.
"""

from __future__ import annotations

import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[1]
FONTS_DIR = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Resources" / "CTCC"
DEST = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Textures"

W, H = 1024, 1360
INK_PRINT = (30, 44, 38)
INK_SUB = (58, 68, 60)
INK_RULE = (112, 114, 96)
INK_RULE_FAINT = (148, 144, 122)
INK_PEN = (26, 56, 118)       # Classic Vietnamese blue ballpoint (Thiên Long)
INK_PENCIL = (68, 70, 64)     # Graphite pencil margin notes
INK_STAMP_RED = (146, 44, 38) # Institutional red rubber stamp
INK_STAMP_PUR = (86, 52, 118) # Secondary clerk stamp


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    name = "BeVietnamPro-SemiBold.ttf" if bold else "BeVietnamPro-Regular.ttf"
    return ImageFont.truetype(str(FONTS_DIR / name), size)


def fbm_2d(w: int, h: int, octaves: tuple[int, ...], seed: int, persistence: float = 0.54) -> np.ndarray:
    r = np.random.default_rng(seed)
    acc = np.zeros((h, w), dtype=np.float32)
    amp = 1.0
    norm = 0.0
    for freq in octaves:
        gh = max(2, int(round(freq * h / max(w, h))))
        gw = max(2, int(round(freq * w / max(w, h))))
        grid = r.random((gh, gw), dtype=np.float32)
        up = np.asarray(
            Image.fromarray(grid, mode="F").resize((w, h), Image.Resampling.BICUBIC),
            dtype=np.float32,
        )
        acc += up * amp
        norm += amp
        amp *= persistence
    return np.clip(acc / max(norm, 1e-6), 0.0, 1.0)


def make_paper_base(
    w: int = W,
    h: int = H,
    base_rgb: tuple[int, int, int] = (224, 214, 186),
    seed: int = 7001,
    age: float = 0.48,
    punch_holes: bool = True,
    fold_y: float = 0.50,
    damp_corner: bool = False,
) -> Image.Image:
    """Create tactile Vietnamese wood-pulp hospital record paper with fibers, foxing, crease & punch holes."""
    macro = fbm_2d(w, h, (4, 8, 16, 32), seed=seed)
    fine = fbm_2d(w, h, (64, 128, 256), seed=seed + 17)
    fibers = fbm_2d(w, h, (16, 48, 160), seed=seed + 31)

    arr = np.empty((h, w, 3), dtype=np.float32)
    arr[..., 0] = base_rgb[0] / 255.0
    arr[..., 1] = base_rgb[1] / 255.0
    arr[..., 2] = base_rgb[2] / 255.0

    # Cellulose fiber grain + subtle blotchiness
    arr *= 0.93 + 0.09 * macro[..., None] + 0.05 * (fine[..., None] - 0.5)
    arr -= np.clip(fibers - 0.78, 0.0, 1.0)[..., None] * 0.08

    # Edge foxing / yellowing
    ex = np.linspace(-1.0, 1.0, w, dtype=np.float32)[None, :]
    ey = np.linspace(-1.0, 1.0, h, dtype=np.float32)[:, None]
    edge = np.clip((ex**2 * 0.55 + ey**2 * 0.55 - 0.25) * 1.15, 0.0, 1.0)
    fox = np.array([0.52, 0.42, 0.26], dtype=np.float32)
    arr = arr * (1.0 - edge[..., None] * age * 0.32) + fox * (edge[..., None] * age * 0.32)

    # Optional monsoon corner damp stain (bottom-right)
    if damp_corner:
        dist = np.sqrt(((ex - 0.78) * 1.2) ** 2 + ((ey - 0.82) * 1.2) ** 2)
        ring = np.clip(1.0 - dist * 1.65, 0.0, 1.0)
        rim = np.exp(-((dist - 0.52) * 18.0) ** 2) * (0.6 + 0.4 * macro)
        stain = np.array([0.46, 0.39, 0.28], dtype=np.float32)
        arr = arr * (1.0 - ring[..., None] * 0.22) + stain * (ring[..., None] * 0.22)
        arr -= rim[..., None] * 0.10

    # Realistic horizontal fold crease
    if 0.1 < fold_y < 0.9:
        yy = np.linspace(0.0, 1.0, h, dtype=np.float32)[:, None]
        crease_dark = np.exp(-((yy - fold_y) * 145.0) ** 2)
        crease_hi = np.exp(-((yy - (fold_y + 0.0045)) * 180.0) ** 2)
        arr -= crease_dark[..., None] * 0.065
        arr += crease_hi[..., None] * 0.025

    img = Image.fromarray(np.uint8(np.clip(arr, 0.0, 1.0) * 255.0), mode="RGB")

    # Left margin two-hole hospital chart binder punches
    if punch_holes:
        d = ImageDraw.Draw(img)
        for py in (int(h * 0.34), int(h * 0.66)):
            px = 42
            d.ellipse((px - 17, py - 17, px + 17, py + 17), fill=(150, 132, 102))
            d.ellipse((px - 12, py - 12, px + 12, py + 12), fill=(58, 54, 46))
            d.arc((px - 14, py - 14, px + 14, py + 14), 200, 350, fill=(104, 86, 62), width=2)
    return img


def handwriting_font(size: int) -> ImageFont.FreeTypeFont:
    """Authentic calligraphic Vietnamese-capable italic font (100% stacked diacritic coverage)."""
    for candidate in (
        r"C:\Windows\Fonts\palai.ttf",
        r"C:\Windows\Fonts\SitkaVF-Italic.ttf",
        r"C:\Windows\Fonts\cambriai.ttf",
        r"C:\Windows\Fonts\timesi.ttf",
    ):
        if Path(candidate).exists():
            return ImageFont.truetype(candidate, size)
    return font(size, bold=False)


def draw_vietnamese_handwriting(
    img: Image.Image,
    pos: tuple[int, int],
    text: str,
    size: int = 34,
    color: tuple[int, int, int] = (34, 52, 92),
    slant: float = -0.14,
    base_angle: float = -0.45,
    alpha: int = 224,
    seed: int = 7700,
    centered: bool = False,
) -> None:
    """Render authentic late-20th-century Vietnamese fountain/ballpoint pen handwriting
    with word-level organic baseline/angle variation, paper fiber ink absorption, and gentle ink bleed."""
    if not text or not text.strip():
        return
    f = handwriting_font(size)
    rng = np.random.default_rng(seed)
    words = text.split(" ")

    # Measure total width if centered
    probe = ImageDraw.Draw(Image.new("L", (4, 4), 0))
    space_w = max(8, int(probe.textlength(" ", font=f) * 1.08))
    word_widths = [int(math.ceil(probe.textlength(w, font=f))) for w in words]
    total_w = sum(word_widths) + space_w * max(0, len(words) - 1)

    cur_x = pos[0] - total_w // 2 if centered else pos[0]
    base_y = pos[1]
    pad = int(size * 0.65)

    for idx, word in enumerate(words):
        ww = word_widths[idx]
        if ww <= 0:
            cur_x += space_w
            continue
        cw = ww + pad * 2
        ch = int(size * 2.5)
        tmp = Image.new("RGBA", (cw, ch), (0, 0, 0, 0))
        dt = ImageDraw.Draw(tmp)
        # Subtle ink saturation variation per word (ballpoint / fountain pen pressure)
        word_alpha = int(np.clip(alpha + rng.integers(-18, 14), 130, 245))
        dt.text((pad, int(size * 0.45)), word, font=f, fill=(*color, word_alpha))
        w_slant = slant + float(rng.uniform(-0.03, 0.03))
        sheared = tmp.transform(
            tmp.size,
            Image.Transform.AFFINE,
            (1.0, w_slant, -w_slant * size * 0.85, 0.0, 1.0, 0.0),
            resample=Image.Resampling.BICUBIC,
        )
        ang = base_angle + float(rng.uniform(-0.75, 0.75))
        if abs(ang) > 0.05:
            sheared = sheared.rotate(ang, resample=Image.Resampling.BICUBIC, expand=False)
        # Paper-grain modulation on ink alpha so ink sits inside paper fibers
        s_arr = np.asarray(sheared, dtype=np.float32)
        grain = fbm_2d(cw, ch, (8, 22), seed=seed + idx * 31)
        s_arr[..., 3] *= np.clip(0.72 + 0.36 * grain, 0.45, 1.0)
        sheared = Image.fromarray(np.uint8(np.clip(s_arr, 0, 255)), mode="RGBA")
        sheared = sheared.filter(ImageFilter.GaussianBlur(0.28))

        dy = int(round(rng.uniform(-1.8, 1.8) + math.sin(idx * 0.55) * 1.1))
        img.paste(sheared, (cur_x - pad, base_y + dy - int(size * 0.45)), sheared)
        cur_x += ww + space_w + int(rng.integers(-1, 3))


def draw_circular_postmark(
    img: Image.Image,
    center: tuple[int, int],
    radius: int,
    top_label: str,
    date_label: str,
    color: tuple[int, int, int] = (78, 74, 68),
    seed: int = 7810,
) -> None:
    """Authentic faded late-20th-century Vietnamese circular archival/postal ink stamp."""
    sz = radius * 2 + 24
    st = Image.new("RGBA", (sz, sz), (0, 0, 0, 0))
    ds = ImageDraw.Draw(st)
    c = (*color, 178)
    cx = cy = sz // 2
    ds.ellipse((cx - radius, cy - radius, cx + radius, cy + radius), outline=c, width=3)
    ds.ellipse((cx - radius + 12, cy - radius + 12, cx + radius - 12, cy + radius - 12), outline=c, width=1)
    ds.line((cx - radius + 16, cy - 16, cx + radius - 16, cy - 16), fill=c, width=2)
    ds.line((cx - radius + 16, cy + 18, cx + radius - 16, cy + 18), fill=c, width=2)
    ds.text((cx, cy - 36), top_label, font=font(14, True), fill=c, anchor="mm")
    ds.text((cx, cy + 1), date_label, font=font(18, True), fill=c, anchor="mm")
    ds.text((cx, cy + 36), "VIỆT NAM", font=font(13, True), fill=c, anchor="mm")
    arr = np.asarray(st, dtype=np.float32)
    noise = fbm_2d(sz, sz, (10, 28, 64), seed=seed)
    arr[..., 3] *= np.clip((noise - 0.18) * 1.75, 0.18, 0.92)
    st = Image.fromarray(np.uint8(np.clip(arr, 0, 255)), mode="RGBA")
    rot = st.rotate(-14.0, resample=Image.Resampling.BICUBIC, expand=True)
    img.paste(rot, (center[0] - rot.width // 2, center[1] - rot.height // 2), rot)


def draw_vintage_envelope_sprig(img: Image.Image, ox: int, oy: int, seed: int = 7820) -> None:
    """Subtle faded vintage late-20th-century Vietnamese printed envelope botanical sprig (bottom-left)."""
    sz = 340
    sp = Image.new("RGBA", (sz, sz), (0, 0, 0, 0))
    ds = ImageDraw.Draw(sp)
    stem_c = (78, 94, 72, 165)
    leaf_c = (72, 96, 70, 150)
    petal_dark = (146, 52, 46, 175)
    petal_mid = (168, 68, 58, 165)
    center_c = (94, 42, 34, 185)
    # Delicate curving stems
    ds.line([(125, 295), (145, 230), (168, 155), (195, 108)], fill=stem_c, width=4, joint="curve")
    ds.line([(145, 230), (112, 188), (94, 152)], fill=stem_c, width=3, joint="curve")
    ds.line([(162, 185), (218, 148), (244, 118)], fill=stem_c, width=3, joint="curve")
    # Small muted leaves
    for lx, ly, rx, ry in [(118, 248, 22, 11), (186, 226, 24, 11), (96, 206, 20, 10), (206, 194, 22, 11), (154, 260, 18, 9)]:
        ds.ellipse((lx - rx, ly - ry, lx + rx, ly + ry), fill=leaf_c)
    # 3 muted vintage crimson-terracotta blossoms (echoing classic late-20th-century Vietnamese floral envelopes)
    for bx, by, br in [(142, 148, 30), (228, 112, 24), (108, 198, 20)]:
        for k in range(5):
            ang = k * (2.0 * math.pi / 5.0) - 0.35
            px = bx + int(math.cos(ang) * br * 0.68)
            py = by + int(math.sin(ang) * br * 0.68)
            pr = int(br * 0.56)
            ds.ellipse((px - pr, py - pr, px + pr, py + pr), fill=petal_dark if k % 2 == 0 else petal_mid)
        ds.ellipse((bx - br // 3, by - br // 3, bx + br // 3, by + br // 3), fill=center_c)
    # Soft vintage litho fade + paper grain wear
    arr = np.asarray(sp, dtype=np.float32)
    wear = fbm_2d(sz, sz, (10, 32), seed=seed)
    arr[..., 3] *= np.clip(0.38 + 0.58 * wear, 0.15, 0.82)
    sp = Image.fromarray(np.uint8(np.clip(arr, 0, 255)), mode="RGBA").filter(ImageFilter.GaussianBlur(0.8))
    img.paste(sp, (ox, oy), sp)


def draw_handwritten(
    img: Image.Image,
    pos: tuple[int, int],
    text: str,
    size: int = 28,
    color: tuple[int, int, int] = INK_PEN,
    slant: float = -0.18,
    angle_deg: float = -0.8,
    alpha: int = 232,
) -> None:
    """Render natural slanted ballpoint/pencil Vietnamese handwritten text onto the document."""
    f = font(size, bold=False)
    tmp = Image.new("RGBA", (W, int(size * 2.4)), (0, 0, 0, 0))
    dt = ImageDraw.Draw(tmp)
    dt.text((24, int(size * 0.45)), text, font=f, fill=(*color, alpha))
    # Shear slightly to mimic cursive hand slant
    sheared = tmp.transform(
        tmp.size,
        Image.Transform.AFFINE,
        (1.0, slant, -slant * size, 0.0, 1.0, 0.0),
        resample=Image.Resampling.BICUBIC,
    )
    if abs(angle_deg) > 0.05:
        sheared = sheared.rotate(angle_deg, resample=Image.Resampling.BICUBIC, expand=False)
    # Slight ink pressure softness
    sheared = sheared.filter(ImageFilter.GaussianBlur(0.35))
    img.paste(sheared, (pos[0] - 24, pos[1] - int(size * 0.45)), sheared)


def draw_checkmark(draw: ImageDraw.ImageDraw, x: int, y: int, size: int = 16, color: tuple[int, int, int] = INK_PEN) -> None:
    """Draw a hand-written blue ballpoint checkmark (since BeVietnamPro omits U+2713)."""
    pts = [
        (x, y + int(size * 0.52)),
        (x + int(size * 0.36), y + int(size * 0.88)),
        (x + size, y + int(size * 0.08)),
    ]
    draw.line(pts, fill=color, width=max(2, size // 5), joint="curve")


def draw_signature_flourish(
    draw: ImageDraw.ImageDraw,
    x: int,
    y: int,
    seed: int,
    color: tuple[int, int, int] = INK_PEN,
    scale: float = 1.0,
) -> None:
    """Draw a smooth, natural Vietnamese cursive ink signature stroke."""
    r = np.random.default_rng(seed)
    anchors = []
    cur_x, cur_y = float(x), float(y)
    for k in range(8):
        t = k / 7.0
        cur_x += (16.5 + r.uniform(-3.0, 5.0)) * scale
        wave = math.sin(t * math.pi * 3.0 + seed * 0.7) * 11.0 * scale + r.uniform(-2.5, 2.5) * scale
        if k in (2, 5):
            wave -= 12.0 * scale
        anchors.append((cur_x, cur_y + wave))
    # Smooth quadratic/Catmull-like oversampling between anchor points
    pts = []
    for i in range(len(anchors) - 1):
        p0 = anchors[max(0, i - 1)]
        p1 = anchors[i]
        p2 = anchors[i + 1]
        p3 = anchors[min(len(anchors) - 1, i + 2)]
        for s in range(6):
            u = s / 6.0
            u2, u3 = u * u, u * u * u
            px = 0.5 * ((2 * p1[0]) + (-p0[0] + p2[0]) * u + (2 * p0[0] - 5 * p1[0] + 4 * p2[0] - p3[0]) * u2 + (-p0[0] + 3 * p1[0] - 3 * p2[0] + p3[0]) * u3)
            py = 0.5 * ((2 * p1[1]) + (-p0[1] + p2[1]) * u + (2 * p0[1] - 5 * p1[1] + 4 * p2[1] - p3[1]) * u2 + (-p0[1] + 3 * p1[1] - 3 * p2[1] + p3[1]) * u3)
            pts.append((px, py))
    pts.append(anchors[-1])
    draw.line(pts, fill=color, width=max(2, int(round(2.8 * scale))), joint="curve")
    draw.line(
        [(x + 12 * scale, y + 18 * scale), (cur_x - 6 * scale, y + 12 * scale)],
        fill=color,
        width=max(2, int(round(2.1 * scale))),
    )


def draw_rubber_stamp(
    img: Image.Image,
    center: tuple[int, int],
    top_line: str,
    main_line: str,
    sub_line: str = "",
    color: tuple[int, int, int] = INK_STAMP_RED,
    angle_deg: float = -6.5,
    seed: int = 8001,
) -> None:
    """Apply a realistic slightly-tilted Vietnamese hospital rectangular rubber stamp with dry-ink texture."""
    sw, sh = 350, 142
    stamp = Image.new("RGBA", (sw, sh), (0, 0, 0, 0))
    d = ImageDraw.Draw(stamp)
    c = (*color, 212)
    d.rectangle((6, 6, sw - 6, sh - 6), outline=c, width=5)
    d.rectangle((14, 14, sw - 14, sh - 14), outline=c, width=2)
    d.text((sw // 2, 36), top_line, font=font(18, True), fill=c, anchor="mm")
    d.text((sw // 2, 76 if sub_line else 82), main_line, font=font(27, True), fill=c, anchor="mm")
    if sub_line:
        d.text((sw // 2, 111), sub_line, font=font(17, False), fill=c, anchor="mm")

    # Dry-ink micro dropouts so stamp looks hand-pressed on paper
    arr = np.asarray(stamp, dtype=np.float32)
    noise = fbm_2d(sw, sh, (16, 48, 128), seed=seed)
    keep = np.clip((noise - 0.22) * 1.9, 0.28, 1.0)
    arr[..., 3] *= keep
    stamp = Image.fromarray(np.uint8(np.clip(arr, 0, 255)), mode="RGBA")
    rot = stamp.rotate(angle_deg, resample=Image.Resampling.BICUBIC, expand=True)
    img.paste(rot, (center[0] - rot.width // 2, center[1] - rot.height // 2), rot)


def draw_ink_smudge(img: Image.Image, center: tuple[int, int], radius: int = 44, color: tuple[int, int, int] = INK_PEN, seed: int = 8100) -> None:
    """Subtle smeared thumb/ballpoint ink blotch (Core 04 OBS101B: dấu tay mực nhòe)."""
    sz = radius * 3
    yy, xx = np.mgrid[0:sz, 0:sz].astype(np.float32)
    cx, cy = sz * 0.5, sz * 0.5
    # Slight directional shear like a thumb brushing rightward across damp ballpoint ink
    dx = ((xx - cx) * 0.85 + (yy - cy) * 0.18) / max(radius * 1.15, 1)
    dy = ((yy - cy) * 0.95 - (xx - cx) * 0.12) / max(radius * 0.58, 1)
    dist = np.sqrt(dx * dx + dy * dy)
    n = fbm_2d(sz, sz, (5, 14, 32), seed=seed)
    fine = fbm_2d(sz, sz, (24, 52), seed=seed + 19)
    # Very gentle asymmetric thumb-grain modulation (no synthetic bullseye rings)
    grain = 0.78 + 0.22 * np.sin((yy * 0.34 + xx * 0.08 + n * 7.5))
    alpha = np.clip((1.0 - dist * 0.95) * (0.35 + 0.65 * n) * (0.65 + 0.35 * fine) * grain, 0.0, 1.0) * 86.0
    arr = np.zeros((sz, sz, 4), dtype=np.uint8)
    arr[..., 0], arr[..., 1], arr[..., 2] = color
    arr[..., 3] = np.uint8(np.clip(alpha, 0, 255))
    blot = Image.fromarray(arr, mode="RGBA").filter(ImageFilter.GaussianBlur(1.4))
    img.paste(blot, (center[0] - sz // 2, center[1] - sz // 2), blot)


def draw_hospital_header(
    d: ImageDraw.ImageDraw,
    doc_code: str,
    save_ref: str,
    title: str,
    subtitle: str,
) -> int:
    """Standard An Sinh 1997-2005 administrative header returning Y coordinate below header."""
    # Outer thin border
    d.rectangle((84, 42, W - 46, H - 46), outline=INK_RULE, width=2)
    # Left hospital identity
    d.text((108, 66), "BỆNH VIỆN ĐA KHOA AN SINH", font=font(22, True), fill=INK_PRINT)
    d.text((108, 96), "KHOA NỘI TRÚ  ·  TẦNG TRỆT", font=font(18, False), fill=INK_SUB)
    d.line((108, 126, 428, 126), fill=INK_RULE, width=2)
    # Right form metadata
    d.text((W - 68, 66), f"Mẫu nội bộ: {doc_code}", font=font(19, True), fill=INK_SUB, anchor="ra")
    d.text((W - 68, 96), f"Số lưu: {save_ref}", font=font(18, False), fill=INK_SUB, anchor="ra")
    # Document title
    d.text((536, 178), title, font=font(42, True), fill=INK_PRINT, anchor="mm")
    d.text((536, 224), subtitle, font=font(22, False), fill=INK_SUB, anchor="mm")
    d.line((108, 252, W - 68, 252), fill=INK_PRINT, width=3)
    d.line((108, 259, W - 68, 259), fill=INK_RULE, width=1)
    return 282


def make_back_sheet(front_img: Image.Image, seed: int, title_note: str, lines: list[str], extra_fn=None) -> Image.Image:
    """Create reverse page with realistic faint mirrored front bleed-through & penciled/ballpoint notes."""
    back = make_paper_base(seed=seed, age=0.52, punch_holes=False, fold_y=0.50, damp_corner=False)
    # Mirror front at subtle 7% bleed-through so paper feels genuinely thin & physical
    mirrored = front_img.transpose(Image.Transpose.FLIP_LEFT_RIGHT).filter(ImageFilter.GaussianBlur(1.4))
    b_arr = np.asarray(back, dtype=np.float32)
    m_arr = np.asarray(mirrored, dtype=np.float32)
    # Dark ink bleeds slightly onto reverse face
    ink_bleed = np.clip((215.0 - m_arr) / 215.0, 0.0, 1.0)
    b_arr = np.clip(b_arr - ink_bleed * 16.0, 0.0, 255.0)
    back = Image.fromarray(np.uint8(b_arr), mode="RGB")

    # Right-side binder punches (mirror of left-margin punches!)
    d = ImageDraw.Draw(back)
    for py in (int(H * 0.34), int(H * 0.66)):
        px = W - 42
        d.ellipse((px - 17, py - 17, px + 17, py + 17), fill=(150, 132, 102))
        d.ellipse((px - 12, py - 12, px + 12, py + 12), fill=(58, 54, 46))

    d.text((88, 76), "MẶT SAU  ·  GHI CHÚ LƯU TRỮ", font=font(20, True), fill=(128, 124, 106))
    d.line((88, 108, W - 96, 108), fill=(165, 158, 134), width=1)

    if title_note:
        draw_handwritten(back, (108, 185), title_note, size=29, color=INK_PENCIL, slant=-0.16, angle_deg=-0.7)
    for idx, ln in enumerate(lines):
        draw_handwritten(back, (112, 265 + idx * 66), ln, size=26, color=INK_PENCIL, slant=-0.19, angle_deg=-0.4 + (idx % 3) * 0.25)

    if extra_fn:
        extra_fn(back, d)
    return back


def build_f101_suite() -> None:
    """Build F101 front, back, and signed A/B/C wall-clipboard overlay variants (clean diegetic — no AS-05 leak!)."""
    img = make_paper_base(seed=7101, age=0.40, punch_holes=True, fold_y=0.50)
    d = ImageDraw.Draw(img)
    y = draw_hospital_header(d, "BC-101", "101-181105", "PHIẾU BÀN GIAO CA ĐÊM", "Ngày 18/11/2005   ·   Phòng bệnh 101   ·   Mã phiếu: F101")

    # Metadata block
    d.text((110, y), "Ca bàn giao:", font=font(23, True), fill=INK_PRINT)
    draw_handwritten(img, (280, y - 2), "Đêm 17–18/11/2005 (Nội trú tầng trệt)", size=24, color=INK_PEN)
    d.text((110, y + 46), "Người trực nhận kiểm:", font=font(23, True), fill=INK_PRINT)
    draw_handwritten(img, (382, y + 44), "N. H. Minh", size=26, color=INK_PEN)
    d.text((610, y + 46), "Bàn trực:", font=font(23, True), fill=INK_PRINT)
    draw_handwritten(img, (728, y + 44), "ĐD. T. N. Lan", size=24, color=INK_PEN)

    # Structured bed handover table (Beds 1, 2, 3 ONLY — Bed 4 conspicuously absent!)
    ty = y + 112
    cols = (110, 215, 455, 725, W - 70)
    row_h = 64
    d.rectangle((cols[0], ty, cols[-1], ty + row_h * 4), outline=INK_PRINT, width=2)
    # Header fill tint
    d.rectangle((cols[0] + 2, ty + 2, cols[-1] - 2, ty + row_h), fill=(208, 200, 174))
    for cx in cols[1:-1]:
        d.line((cx, ty, cx, ty + row_h * 4), fill=INK_RULE, width=2)
    for r_idx in range(1, 4):
        d.line((cols[0], ty + r_idx * row_h, cols[-1], ty + r_idx * row_h), fill=INK_RULE, width=1)

    headers = ("Giường", "Trạng thái hồ sơ", "Ghi chú bàn giao", "Đối chiếu")
    for idx, h_txt in enumerate(headers):
        cx = (cols[idx] + cols[idx + 1]) // 2
        d.text((cx, ty + 32), h_txt, font=font(20, True), fill=INK_PRINT, anchor="mm")

    rows = (
        ("01", "Trống (đã xuất viện)", "Đã dọn giường ngày 16/11", "Đã khớp"),
        ("02", "Đã chuyển Nội B", "Phiếu 19g20 · đủ 2 chữ ký", "Đã khớp"),
        ("03", "Đã xuất viện 10g15", "Đã khóa phiếu thuốc 17/11", "Đã khớp"),
    )
    for r_i, (bed, st, note, chk) in enumerate(rows, start=1):
        ry = ty + r_i * row_h + 18
        d.text(((cols[0] + cols[1]) // 2, ry + 12), bed, font=font(23, True), fill=INK_PRINT, anchor="mm")
        draw_handwritten(img, (cols[1] + 12, ry), st, size=20, color=INK_PEN, angle_deg=-0.4)
        draw_handwritten(img, (cols[2] + 12, ry), note, size=18, color=INK_PEN, angle_deg=0.3)
        draw_checkmark(d, cols[3] + 22, ry + 2, size=18, color=INK_PEN)
        draw_handwritten(img, (cols[3] + 46, ry), chk, size=20, color=INK_PEN, angle_deg=-0.8)

    # Highlighted summary box (THE CENTRAL CLAIM BEING TESTED!)
    sy = ty + row_h * 4 + 34
    d.rectangle((110, sy, W - 70, sy + 132), outline=INK_PRINT, width=3)
    d.rectangle((116, sy + 6, W - 76, sy + 126), outline=INK_RULE, width=1)
    d.text((136, sy + 24), "TỔNG HỢP THEO BẢNG BÀN GIAO PHÒNG 101:", font=font(22, True), fill=INK_PRINT)
    d.text((136, sy + 68), "Số bệnh nhân chưa bàn giao:", font=font(25, False), fill=INK_SUB)
    draw_handwritten(img, (515, sy + 62), "0  (Không còn tồn đọng)", size=32, color=INK_PEN, angle_deg=-0.6)

    # Verification instruction & signature section
    vy = sy + 162
    d.text((110, vy), "Yêu cầu chuyên môn: Kiểm tra thực tế buồng bệnh và đối chiếu thẻ giường trước khi ký.", font=font(20, False), fill=INK_SUB)
    d.line((110, vy + 46, W - 70, vy + 46), fill=INK_RULE_FAINT, width=1)

    d.text((130, vy + 72), "XÁC NHẬN CỦA NGƯỜI TRỰC CA", font=font(21, True), fill=INK_PRINT)
    d.text((130, vy + 106), "(Ghi rõ tình trạng & ký tên)", font=font(18, False), fill=INK_SUB)
    d.line((130, vy + 225, 470, vy + 225), fill=INK_RULE, width=2)
    d.text((300, vy + 242), "N. H. Minh", font=font(22, True), fill=INK_SUB, anchor="mm")

    draw_rubber_stamp(img, (752, vy + 165), "BỆNH VIỆN AN SINH", "BÀN TRỰC NỘI TRÚ", "CA ĐÊM · 18/11/2005", INK_STAMP_RED, -5.5, seed=7102)
    d.text((110, H - 82), "Bản lưu tại bàn trực · Quy chế bàn giao buồng bệnh · Không tự hủy phiếu đã phát hành", font=font(17, False), fill=INK_SUB)

    img.save(DEST / "F101.png")

    # Back side of F101 (subtle procedural clue rewarding flip inspection!)
    back = make_back_sheet(
        img,
        seed=7103,
        title_note="Ghi chú biên lề (bút chì):",
        lines=[
            "— Kiểm đủ 3 giường cố định theo bản kê phòng 101.",
            "— Mục giường tạm cuối phòng chưa thấy phiếu chốt ở quầy.",
        ],
    )
    back.save(DEST / "F101_Back.png")

    # Dynamic signed wall-board variants (A: Affirmed, B: Unverified, C: Refuted)
    for suffix, label, stamp_main, stamp_col, note in (
        ("F101_SignedA", "Đã xác nhận: 0 tồn đọng", "ĐÃ BÀN GIAO · 0", INK_STAMP_RED, "Ký nhận theo bảng — không tồn đọng."),
        ("F101_SignedB", "Để ngỏ: Chờ kiểm tra lại", "CHỜ KIỂM TRA", INK_STAMP_PUR, "Chưa đủ căn cứ để chốt số 0."),
        ("F101_SignedC", "Bất nhất: Không xác nhận 0", "CÓ SAI LỆCH", INK_STAMP_RED, "Đối chiếu: dấu giường 4 & thẻ chưa khép."),
    ):
        v = img.copy()
        dv = ImageDraw.Draw(v)
        draw_handwritten(v, (132, vy + 144), label, size=22, color=INK_PEN, angle_deg=-0.5)
        draw_signature_flourish(dv, 185, vy + 195, seed=7110 + len(suffix), color=INK_PEN, scale=1.05)
        draw_handwritten(v, (132, vy + 266), note, size=18, color=INK_PENCIL, angle_deg=0.2)
        draw_rubber_stamp(v, (752, vy + 38), "KẾT QUẢ ĐỐI CHIẾU", stamp_main, "N. H. MINH", stamp_col, 4.5, seed=7120 + len(suffix))
        v.save(DEST / f"{suffix}.png")


def build_obs101b_bed4_card() -> None:
    """Incomplete Bed-4 handover card (OBS101B) — NO patient name, NO C01, NO Area C!"""
    img = make_paper_base(seed=7201, base_rgb=(220, 208, 176), age=0.56, punch_holes=True, fold_y=0.48, damp_corner=True)
    d = ImageDraw.Draw(img)
    y = draw_hospital_header(d, "AS-05B", "101/04-1105", "THẺ BÀN GIAO GIƯỜNG", "Thẻ treo đầu giường / hốc kê tạm   ·   Khoa Nội trú")

    # Form grid (authentic blank fields — ZERO bracketed developer stage directions!)
    fields = [
        ("Ngày lập thẻ:", "17/11/2005 (Chuyển sang ca đêm 18/11/2005)"),
        ("Phòng / Vị trí giường:", "Phòng 101   —   Giường số 4 (vị trí kê tạm cuối phòng)"),
        ("Lý do lưu tạm:", "Khám sốt tạm, theo dõi ngắn trong tối 17/11"),
        ("Tên / Mã bệnh nhân:", ""),
    ]
    cy = y + 8
    for idx, (lbl, val) in enumerate(fields):
        d.text((112, cy), lbl, font=font(22, True), fill=INK_PRINT)
        d.line((112, cy + 56, W - 72, cy + 56), fill=INK_RULE_FAINT, width=1)
        if val:
            draw_handwritten(img, (375, cy - 2), val, size=23, color=INK_PEN, angle_deg=-0.5 + idx * 0.2)
        else:
            # Subtle dotted line on genuinely unfilled patient field
            for dx in range(382, W - 92, 18):
                d.ellipse((dx, cy + 28, dx + 3, cy + 31), fill=INK_RULE)
        cy += 78

    # Status checkboxes — CHƯA BÀN GIAO circled in blue ballpoint!
    by = cy + 20
    d.rectangle((112, by, W - 72, by + 162), outline=INK_PRINT, width=2)
    d.text((134, by + 20), "TÌNH TRẠNG BÀN GIAO CA:", font=font(22, True), fill=INK_PRINT)
    d.text((150, by + 72), "[   ] Đã bàn giao xong", font=font(24, False), fill=INK_SUB)
    d.text((515, by + 72), "[   ] CHƯA BÀN GIAO", font=font(26, True), fill=INK_PRINT)
    draw_checkmark(d, 524, by + 73, size=21, color=INK_PEN)
    # Hand-drawn blue pen oval encircling CHƯA BÀN GIAO
    d.arc((494, by + 54, 888, by + 122), 0, 355, fill=INK_PEN, width=4)
    d.arc((498, by + 58, 884, by + 118), 15, 340, fill=INK_PEN, width=2)
    draw_handwritten(img, (150, by + 118), "Ghi chú trực: Kiểm tra lại vị trí chuyển trước khi chốt sổ.", size=22, color=INK_PEN, angle_deg=-0.4)

    # Signature comparison block: Sender faint rushed initials + thumb smudge; Receiver genuinely EMPTY!
    sy = by + 194
    d.rectangle((112, sy, 520, sy + 215), outline=INK_RULE, width=2)
    d.rectangle((536, sy, W - 72, sy + 215), outline=INK_RULE, width=2)
    d.text((316, sy + 26), "NGƯỜI GIAO CA", font=font(20, True), fill=INK_PRINT, anchor="mm")
    d.text((744, sy + 26), "NGƯỜI NHẬN BÀN GIAO", font=font(20, True), fill=INK_PRINT, anchor="mm")
    draw_handwritten(img, (218, sy + 96), "T. N. L.", size=28, color=INK_PEN, alpha=165)
    # Dotted blank signature guide line on receiver box (physically unsigned!)
    for dx in range(586, W - 122, 16):
        d.ellipse((dx, sy + 148, dx + 3, sy + 151), fill=INK_RULE)
    # Ink thumb smudge across lower receiver/sender divider (Core 04 OBS101B: dấu tay mực nhòe)
    draw_ink_smudge(img, (515, sy + 162), radius=52, color=INK_PEN, seed=7205)

    draw_rubber_stamp(img, (756, sy + 286), "BỆNH VIỆN AN SINH", "CHỜ XÁC NHẬN", "CHƯA KHÉP THẺ", INK_STAMP_RED, -8.0, seed=7202)
    d.text((112, H - 82), "Thẻ lưu nội bộ · Chỉ dùng đối chiếu tại buồng bệnh, không thay hồ sơ ra viện", font=font(17, False), fill=INK_SUB)
    img.save(DEST / "ObsCard.png")

    # Back of OBS101B: Clean physical pencil note with genuinely smudged water-washed second line (ZERO stage directions!)
    def draw_water_smudged_line(b_img: Image.Image, _: ImageDraw.ImageDraw) -> None:
        draw_handwritten(b_img, (112, 331), "— Đã chuyển sang khu...", size=25, color=(112, 106, 92), slant=-0.18, angle_deg=-0.3, alpha=110)
        draw_ink_smudge(b_img, (430, 345), radius=76, color=(118, 110, 88), seed=7209)
        draw_ink_smudge(b_img, (585, 355), radius=68, color=(102, 94, 76), seed=7213)

    back = make_back_sheet(
        img,
        seed=7203,
        title_note="17/11 — Giường 4 (tạm):",
        lines=[
            "— Kê tạm tối 17/11 cho ca khám sốt, chờ ký nhận ca sau.",
        ],
        extra_fn=draw_water_smudged_line,
    )
    back.save(DEST / "ObsCard_Back.png")


def build_bed2_completed_transfer() -> None:
    """Comparison Document 1 (OBS101C): Completed Bed-2 transfer record — authentic standard baseline."""
    img = make_paper_base(seed=7301, base_rgb=(226, 219, 194), age=0.35, punch_holes=True, fold_y=0.50)
    d = ImageDraw.Draw(img)
    y = draw_hospital_header(d, "AS-05B", "101/02-1105", "PHIẾU CHUYỂN GIƯỜNG", "Hồ sơ chuyển khoa nội trú   ·   Đã hoàn tất bàn giao")

    fields = [
        ("Ngày lập phiếu:", "17/11/2005   ·   Giờ chuyển: 19g20"),
        ("Phòng / Giường cũ:", "Phòng 101   —   Giường số 2"),
        ("Bệnh nhân (viết tắt):", "N. V. T.   (Khoa Nội tổng hợp — theo dõi huyết áp)"),
        ("Chuyển đến:", "Khu Nội B   —   Tầng 2, Phòng B-04"),
        ("Tình trạng bàn giao:", "ĐÃ BÀN GIAO ĐẦY ĐỦ (đồ dùng & sổ thuốc đã chuyển)"),
    ]
    cy = y + 10
    for lbl, val in fields:
        d.text((112, cy), lbl, font=font(22, True), fill=INK_PRINT)
        d.line((112, cy + 56, W - 72, cy + 56), fill=INK_RULE_FAINT, width=1)
        draw_handwritten(img, (370, cy - 2), val, size=22, color=INK_PEN, angle_deg=-0.3)
        cy += 78

    # Both signatures cleanly filled & stamped!
    sy = cy + 28
    d.rectangle((112, sy, 520, sy + 230), outline=INK_PRINT, width=2)
    d.rectangle((536, sy, W - 72, sy + 230), outline=INK_PRINT, width=2)
    d.text((316, sy + 28), "NGƯỜI GIAO (PHÒNG 101)", font=font(19, True), fill=INK_PRINT, anchor="mm")
    d.text((744, sy + 28), "NGƯỜI NHẬN (KHU B)", font=font(19, True), fill=INK_PRINT, anchor="mm")
    draw_signature_flourish(d, 200, sy + 115, seed=7310, color=INK_PEN, scale=1.05)
    draw_signature_flourish(d, 635, sy + 118, seed=7311, color=INK_PEN, scale=1.02)
    draw_handwritten(img, (210, sy + 178), "ĐD. T. N. Lan · 19g20", size=21, color=INK_PEN)
    draw_handwritten(img, (630, sy + 178), "ĐD. P. T. H. · 19g25", size=21, color=INK_PEN)

    draw_rubber_stamp(img, (750, sy + 300), "BỆNH VIỆN AN SINH", "ĐÃ BÀN GIAO", "HOÀN TẤT 17/11/2005", INK_STAMP_RED, 5.2, seed=7302)
    d.text((112, H - 82), "Mẫu chuẩn bàn giao đầy đủ: bắt buộc đủ chữ ký cả hai đầu giao – nhận và giờ tiếp nhận.", font=font(17, False), fill=INK_SUB)
    img.save(DEST / "Doc_Bed2Transfer.png")

    back = make_back_sheet(
        img,
        seed=7303,
        title_note="Ghi nhận đối chiếu sổ bàn giao (giường 2):",
        lines=[
            "— Đã đối chiếu sổ trực tầng 2 lúc 19g30 ngày 17/11.",
            "— Giường 2 đã thay ga sạch, đưa vào trạng thái sẵn sàng.",
            "— Thủ tục khép đủ chữ ký hai bên — không còn tồn đọng.",
        ],
    )
    back.save(DEST / "Doc_Bed2Transfer_Back.png")


def build_bed3_medication_log() -> None:
    """Comparison Document 2 (OBS101D): Routine Bed-3 medication & temperature chart (discharged)."""
    img = make_paper_base(seed=7401, base_rgb=(224, 217, 192), age=0.42, punch_holes=True, fold_y=0.52)
    d = ImageDraw.Draw(img)
    y = draw_hospital_header(d, "AS-09", "101/03-1105", "PHIẾU CÔNG KHAI THUỐC", "Theo dõi điều trị & nhiệt độ   ·   Phòng 101 — Giường 03")

    d.text((112, y), "Bệnh nhân:", font=font(22, True), fill=INK_PRINT)
    draw_handwritten(img, (265, y - 2), "L. T. M. (Viêm phế quản nhẹ)", size=23, color=INK_PEN)
    d.text((640, y), "Vào phòng:", font=font(22, True), fill=INK_PRINT)
    draw_handwritten(img, (780, y - 2), "15/11/2005", size=23, color=INK_PEN)

    ty = y + 66
    cols = (112, 240, 560, 745, W - 70)
    row_h = 62
    d.rectangle((cols[0], ty, cols[-1], ty + row_h * 5), outline=INK_PRINT, width=2)
    d.rectangle((cols[0] + 2, ty + 2, cols[-1] - 2, ty + row_h), fill=(210, 204, 180))
    for cx in cols[1:-1]:
        d.line((cx, ty, cx, ty + row_h * 5), fill=INK_RULE, width=1)
    for r_i in range(1, 5):
        d.line((cols[0], ty + r_i * row_h, cols[-1], ty + r_i * row_h), fill=INK_RULE, width=1)

    for idx, h_txt in enumerate(("Giờ / Ngày", "Thuốc & y lệnh", "Nhiệt độ", "Điều dưỡng")):
        d.text(((cols[idx] + cols[idx + 1]) // 2, ty + 31), h_txt, font=font(19, True), fill=INK_PRINT, anchor="mm")

    med_rows = (
        ("16/11 · 08g", "Hạ sốt + dịch truyền NaCl", "37,6 °C", "T. N. Lan"),
        ("16/11 · 20g", "Kháng sinh đường uống", "37,1 °C", "T. N. Lan"),
        ("17/11 · 08g", "Kiểm tra phổi ổn định", "36,8 °C", "P. T. H."),
        ("17/11 · 10g15", "Ký giấy xuất viện", "Ổn định", "BS. trực"),
    )
    for r_i, (tm, med, tmp, nur) in enumerate(med_rows, start=1):
        ry = ty + r_i * row_h + 18
        draw_handwritten(img, (cols[0] + 10, ry), tm, size=19, color=INK_PEN)
        draw_handwritten(img, (cols[1] + 12, ry), med, size=20, color=INK_PEN)
        draw_handwritten(img, (cols[2] + 28, ry), tmp, size=20, color=INK_PEN)
        draw_handwritten(img, (cols[3] + 16, ry), nur, size=20, color=INK_PEN)

    fy = ty + row_h * 5 + 36
    d.text((112, fy), "Trạng thái hồ sơ giường 03:", font=font(22, True), fill=INK_PRINT)
    draw_handwritten(img, (440, fy - 2), "Đã xuất viện sáng 17/11 — khóa hồ sơ thường lệ.", size=23, color=INK_PEN)
    draw_rubber_stamp(img, (750, fy + 160), "BỆNH VIỆN AN SINH", "ĐÃ KHÓA HỒ SƠ", "XUẤT VIỆN 17/11", INK_STAMP_PUR, -4.0, seed=7402)
    d.text((112, H - 82), "Phiếu công khai thuốc lưu hồ sơ điều trị nội trú · Bệnh viện Đa khoa An Sinh", font=font(17, False), fill=INK_SUB)
    img.save(DEST / "Doc_Bed3Meds.png")

    back = make_back_sheet(
        img,
        seed=7403,
        title_note="Ghi chú dọn buồng giường 03:",
        lines=[
            "— Bệnh nhân xuất viện lúc 10g15 ngày 17/11/2005.",
            "— Đã thu hồi chăn, ga và nộp lại tủ đồ vải tầng trệt.",
            "— Tủ đầu giường trống, không còn vật dụng cá nhân.",
        ],
    )
    back.save(DEST / "Doc_Bed3Meds_Back.png")


def build_nursing_alcove_and_reception_docs() -> None:
    """Optional contextual exploration documents in Staff Nursing Alcove & Reception Drawer."""
    # 1. OBS_SHIFT_LOG: Handover verification guideline + Lan's handwritten sticky note on back
    img = make_paper_base(seed=7501, base_rgb=(221, 214, 189), age=0.45, punch_holes=True, fold_y=0.50)
    d = ImageDraw.Draw(img)
    y = draw_hospital_header(d, "QC-1997", "NOI-TRU-04", "QUY CHẾ BÀN GIAO CA", "Trích hướng dẫn bàn giao buồng bệnh   ·   Khoa Nội trú")

    rules = [
        "1. Trước khi ký xác nhận số 0 (không còn tồn đọng), người trực phải",
        "   đối chiếu đồng thời thực địa buồng bệnh và thẻ theo dõi từng giường.",
        "",
        "2. Phiếu chuyển hoàn tất bắt buộc có đủ hai chữ ký (giao & nhận) và",
        "   dấu 'ĐÃ BÀN GIAO'. Thiếu một bên chữ ký được xếp loại 'CHƯA KHÉP'.",
        "",
        "3. Khi phát hiện thẻ chưa khép nhưng chưa xác minh rõ bệnh nhân đi đâu,",
        "   không được tự suy đoán nguyên nhân; ghi nhận 'CHƯA ĐỦ CĂN CỨ' hoặc",
        "   'CÓ SAI LỆCH — CẦN ĐỐI CHIẾU' và giữ lại dấu vết gốc.",
        "",
        "4. Không gạch xóa chữ ký cũ trên phiếu đã lưu; mọi điều chỉnh phải",
        "   ghi rõ lần sửa và giữ bản chụp xác nhận ban đầu.",
    ]
    for idx, line in enumerate(rules):
        d.text((112, y + 18 + idx * 44), line, font=font(22, idx in (0, 3, 6, 10)), fill=INK_PRINT)

    draw_rubber_stamp(img, (746, H - 220), "PHÒNG KẾ HOẠCH TỔNG HỢP", "LƯU HÀNH NỘI BỘ", "AN SINH · 1997–2005", INK_STAMP_RED, -3.5, seed=7502)
    img.save(DEST / "Doc_ShiftGuide.png")

    back = make_back_sheet(
        img,
        seed=7503,
        title_note="Ghi chú bàn trực (bút chì):",
        lines=[
            "Phòng 101 cuối ca nhớ kiểm lại đủ các thẻ đầu giường",
            "trước khi chốt bản tổng hợp bàn giao.",
            "                                                      — T. N. Lan",
        ],
    )
    back.save(DEST / "Doc_ShiftGuide_Back.png")

    # 2. OBS_CORRIDOR_NOTICE: Ward 101 temporary 4th bed overflow memo (DEC-V361-CH01-01: no wall-socket #4 leak in B!)
    ov = make_paper_base(seed=7511, base_rgb=(225, 216, 186), age=0.50, punch_holes=False, fold_y=0.0)
    do = ImageDraw.Draw(ov)
    yo = draw_hospital_header(do, "TB-101", "NV-11/05", "THÔNG BÁO BỐ TRÍ GIƯỜNG", "Khoa Nội trú tầng trệt   ·   Áp dụng đợt trực tháng 11/2005")
    body_ov = [
        "• Phòng 101 bố trí cố định 03 giường điều trị (vị trí 1, 2, 3).",
        "",
        "• Vị trí số 4 (hốc cuối phòng sau thanh rèm ngăn)",
        "  chỉ dùng kê giường tạm cho ca khám sốt ngắn khi phòng trên đông.",
        "",
        "• Khi thu gọn giường tạm vào kho 101, điều dưỡng phải tháo thẻ treo",
        "  và đối chiếu ký nhận đầy đủ tại bàn trực tầng trệt.",
    ]
    for idx, ln in enumerate(body_ov):
        do.text((112, yo + 36 + idx * 54), ln, font=font(24, False), fill=INK_PRINT)
    draw_handwritten(ov, (124, yo + 470), "Ghi chú thêm: Tối 17/11 có kê tạm giường 4 — sáng 18/11 đã đẩy vào kho?", size=24, color=INK_PEN, angle_deg=-0.6)
    draw_rubber_stamp(ov, (748, H - 230), "KHOA NỘI TRÚ", "BẢNG THÔNG BÁO", "TẦNG TRỆT", INK_STAMP_RED, 3.8, seed=7512)
    ov.save(DEST / "Doc_WardOverflow.png")

    # 3. OBS_RECEPTION_DRAWER: November 2005 Night Duty Roster (canon BS.TT N. H. Minh, real red strike, ZERO stage text!)
    dr = make_paper_base(seed=7521, base_rgb=(222, 213, 185), age=0.55, punch_holes=True, fold_y=0.50)
    ddr = ImageDraw.Draw(dr)
    ydr = draw_hospital_header(ddr, "PC-1105", "LT-2005", "LỊCH PHÂN CÔNG CA ĐÊM", "Tháng 11 năm 2005   ·   Tầng trệt & Khu Nội trú")
    rows_dr = (
        ("15/11/2005", "ĐD. T. N. Lan", "BS.TT N. H. Minh", "Bình thường"),
        ("16/11/2005", "ĐD. P. T. H.", "BS.TT N. H. Minh", "Bình thường"),
        ("17/11/2005", "ĐD. T. N. Lan", "BS.TT N. H. Minh", "Tăng cường ca sốt"),
        ("18/11/2005", "ĐD. T. N. Lan", "BS.TT N. H. Minh", "Tạm đình chỉ"),
    )
    cols = (110, 275, 485, 705, W - 70)
    ty = ydr + 20
    rh = 68
    ddr.rectangle((cols[0], ty, cols[-1], ty + rh * 5), outline=INK_PRINT, width=2)
    ddr.rectangle((cols[0] + 2, ty + 2, cols[-1] - 2, ty + rh), fill=(208, 200, 174))
    for cx in cols[1:-1]:
        ddr.line((cx, ty, cx, ty + rh * 5), fill=INK_RULE, width=1)
    for r_i in range(1, 5):
        ddr.line((cols[0], ty + r_i * rh, cols[-1], ty + r_i * rh), fill=INK_RULE, width=1)
    for idx, h_txt in enumerate(("Ngày trực", "Điều dưỡng trực", "Hành chính / Khóa", "Ghi chú")):
        ddr.text(((cols[idx] + cols[idx + 1]) // 2, ty + 34), h_txt, font=font(19, True), fill=INK_PRINT, anchor="mm")
    for r_i, (dt, lan, minh, note) in enumerate(rows_dr, start=1):
        ry = ty + r_i * rh + 20
        draw_handwritten(dr, (cols[0] + 10, ry), dt, size=20, color=INK_PRINT)
        draw_handwritten(dr, (cols[1] + 14, ry), lan, size=20, color=INK_PEN)
        draw_handwritten(dr, (cols[2] + 14, ry), minh, size=20, color=INK_PEN)
        draw_handwritten(dr, (cols[3] + 10, ry), note, size=18, color=INK_STAMP_RED if r_i == 4 else INK_SUB)
    # Red strike line across 18/11/2005 row
    ddr.line((cols[0] + 8, ty + 4 * rh + 34, cols[-1] - 8, ty + 4 * rh + 38), fill=INK_STAMP_RED, width=3)
    draw_handwritten(dr, (118, ty + rh * 5 + 48), "Ghi chú cũ: Mọi ca sau 18/11/2005 đã tạm đình chỉ hoạt động.", size=23, color=INK_PENCIL)
    dr.save(DEST / "Doc_DutyRoster.png")


def build_bay_envelope_and_letter() -> None:
    """Authentic late-20th-century Vietnamese envelope & archival paper design (inspired by genuine 1970s–2000s Vietnamese stationery)."""
    # 1. Vintage Vietnamese Envelope Front (1024 x 660 landscape — faded warm beige-cream paper, thin blue address lines,
    #    circular ink postmark, vintage corner floral print, and natural Vietnamese blue-indigo ink handwriting!)
    ew, eh = 1024, 660
    env = make_paper_base(ew, eh, base_rgb=(206, 188, 164), seed=7601, age=0.66, punch_holes=False, fold_y=0.0, damp_corner=True)
    # Soft worn dark patina along perimeter & fold edge (matches genuine aged Vietnamese envelope edges)
    e_arr = np.asarray(env, dtype=np.float32) / 255.0
    yy_e, xx_e = np.mgrid[0:eh, 0:ew].astype(np.float32)
    edge_d = np.minimum(np.minimum(xx_e / 34.0, (ew - xx_e) / 34.0), np.minimum(yy_e / 28.0, (eh - yy_e) / 36.0))
    edge_wear = np.clip(1.0 - edge_d, 0.0, 1.0)
    e_arr -= edge_wear[..., None] * np.array([0.16, 0.15, 0.14], dtype=np.float32)
    # Subtle warm rosy-beige age blush toward upper half
    blush = np.exp(-(((xx_e / ew - 0.36) ** 2) / 0.18 + ((yy_e / eh - 0.30) ** 2) / 0.22))
    e_arr += blush[..., None] * np.array([0.045, 0.012, 0.008], dtype=np.float32)
    env = Image.fromarray(np.uint8(np.clip(e_arr, 0.0, 1.0) * 255.0), mode="RGB")

    # Subtle vintage printed floral motif at bottom-left
    draw_vintage_envelope_sprig(env, 32, 275, seed=7605)

    de = ImageDraw.Draw(env)
    guide_blue = (102, 134, 158)
    ink_indigo = (32, 48, 86)

    # Top-left: 3 classic thin blue printed sender lines + handwritten sender
    for ly in (156, 204, 252):
        de.line((68, ly, 392, ly), fill=guide_blue, width=2)
    draw_vietnamese_handwriting(env, (74, 114), "Ông Bảy — Bảo vệ", size=30, color=ink_indigo, slant=-0.16, base_angle=-0.4, alpha=216, seed=7610)
    draw_vietnamese_handwriting(env, (78, 162), "Bệnh viện An Sinh", size=28, color=ink_indigo, slant=-0.15, base_angle=-0.2, alpha=210, seed=7611)
    draw_vietnamese_handwriting(env, (80, 210), "Chốt cổng phụ", size=27, color=ink_indigo, slant=-0.15, base_angle=0.1, alpha=206, seed=7612)

    # Top-right: vintage archive/hospital stamp seal + circular ink postmark (matches reference layout!)
    de.rectangle((866, 38, 976, 178), fill=(192, 186, 162), outline=(126, 120, 102), width=2)
    de.rectangle((876, 48, 966, 168), fill=(72, 102, 74), outline=(188, 182, 154), width=2)
    de.text((921, 70), "AN SINH", font=font(15, True), fill=(224, 218, 188), anchor="mm")
    de.text((921, 108), "LƯU KHO", font=font(16, True), fill=(232, 224, 194), anchor="mm")
    de.text((921, 146), "2012", font=font(16, True), fill=(220, 214, 182), anchor="mm")
    draw_circular_postmark(env, (568, 226), 80, "AN SINH · CỔNG PHỤ", "17 · 11 · 12", color=(88, 84, 76), seed=7602)
    draw_circular_postmark(env, (866, 146), 76, "GIAO TẬN TAY", "17 · 11 · 12", color=(74, 70, 64), seed=7603)

    # Bottom-right: 3 classic thin blue printed recipient lines + handwritten address
    for ly in (428, 488, 548):
        de.line((454, ly, 946, ly), fill=guide_blue, width=2)
    draw_vietnamese_handwriting(env, (458, 382), "Kính gửi:  Anh Nguyễn Hoàng Minh", size=34, color=(26, 44, 82), slant=-0.17, base_angle=-0.45, alpha=224, seed=7615)
    draw_vietnamese_handwriting(env, (466, 442), "Đ/c:  Cổng phụ Bệnh viện An Sinh", size=30, color=(32, 50, 88), slant=-0.15, base_angle=-0.2, alpha=216, seed=7616)
    draw_vietnamese_handwriting(env, (520, 502), "Đêm 17-11-2012  (Chống ẩm)", size=28, color=(38, 54, 86), slant=-0.14, base_angle=0.1, alpha=208, seed=7617)
    env.save(DEST / "Doc_BayEnvelopeFront.png")

    # 2. Legacy combined sheet (cleaned to 100% canon dates — no 2012 time-travel paradox or bracketed stage notes!)
    lt = make_paper_base(seed=7611, base_rgb=(218, 206, 174), age=0.68, punch_holes=False, fold_y=0.52, damp_corner=True)
    dl = ImageDraw.Draw(lt)
    dl.rectangle((54, 48, W - 54, 620), outline=(136, 122, 92), width=2)
    dl.text((84, 72), "THƯ GỬI NGUYỄN HOÀNG MINH", font=font(20, True), fill=INK_SUB)
    lan_lines = [
        "Anh Minh,",
        "",
        "Em cần nói với anh một chuyện ở bệnh viện.",
        "Anh có thể đến gặp em sau ca trực không?",
        "Em không muốn để chuyện này qua thêm một ngày nữa.",
        "",
        "                                                              — Lan",
    ]
    for i, ln in enumerate(lan_lines):
        draw_handwritten(lt, (104, 148 + i * 58), ln, size=29, color=(38, 62, 108), slant=-0.20, angle_deg=-0.6, alpha=210)

    dl.rectangle((54, 668, W - 54, H - 56), outline=INK_PRINT, width=3)
    dl.text((84, 696), "GIẤY NHẮN · ĐÊM 17/11/2012", font=font(22, True), fill=INK_PRINT)
    draw_handwritten(lt, (96, 756), "Nếu chưa thấy tôi, vào quầy trực chờ. — Bảy", size=28, color=(26, 28, 30), slant=-0.10, angle_deg=0.4)
    draw_handwritten(lt, (96, 816), "Chìa cổng phụ buộc ngoài bì; chìa cũ khu C đã thay khóa.", size=24, color=(38, 40, 42), slant=-0.10)
    dl.line((84, 892, W - 84, 892), fill=INK_RULE, width=2)
    dl.text((84, 916), "TIN BÁO 19/11/2005:", font=font(21, True), fill=INK_SUB)
    dl.text((96, 962), "Vụ cháy rạng sáng 18/11/2005 tại Bệnh viện từ thiện An Sinh", font=font(22, False), fill=INK_SUB)
    dl.text((96, 1002), "xuất phát từ chập điện khu kỹ thuật lan sang khu lưu bệnh tạm thời,", font=font(22, False), fill=INK_SUB)
    dl.text((96, 1042), "làm 13 người tử vong, trong đó có một nữ điều dưỡng trực đêm.", font=font(22, False), fill=INK_SUB)
    draw_ink_smudge(lt, (780, 1185), radius=76, color=(94, 82, 62), seed=7619)
    lt.save(DEST / "Doc_BayLetter.png")

    back = make_back_sheet(
        lt,
        seed=7613,
        title_note="Ghi chú kiểm kê (Bảy):",
        lines=[
            "• Chìa đồng sáng: cổng phụ chốt gác đêm.",
            "• Chìa sắt cũ: cửa sau khu C (ổ khóa đã thay từ 2006).",
        ],
    )
    back.save(DEST / "Doc_BayLetter_Back.png")


def build_ch01_rebirth3_e10a_parts() -> None:
    """Core v3.6.2 (DEC-V36-CH01-01) separated E10a physical artifacts + clean 2012 phone LCD fallback.
    100% diegetic — ZERO artist/material descriptions, ZERO PC keybinds, ZERO 2006/2012 newspaper time-travel paradoxes!"""
    # 1. External Appointment Note outside envelope (Doc_BayNote.png — concise canon Core 07 Evidence Specs!14)
    nw, nh = 1024, 760
    note = make_paper_base(nw, nh, base_rgb=(228, 220, 198), seed=7631, age=0.40, punch_holes=False, fold_y=0.48, damp_corner=True)
    dn = ImageDraw.Draw(note)
    for gy in range(132, nh - 70, 62):
        dn.line((56, gy, nw - 56, gy), fill=(192, 188, 174), width=1)
    draw_vietnamese_handwriting(note, (nw // 2, 72), "Cổng phụ An Sinh, đêm 17-11-2012", size=28, color=(52, 54, 58), slant=-0.12, base_angle=-0.3, alpha=208, seed=7630, centered=True)
    draw_vietnamese_handwriting(note, (96, 152), "Minh à,", size=38, color=(28, 34, 48), slant=-0.14, base_angle=-0.4, alpha=224, seed=7631)
    draw_vietnamese_handwriting(note, (96, 232), "Nếu chưa thấy tôi, vào quầy trực chờ.", size=40, color=(22, 30, 46), slant=-0.14, base_angle=-0.2, alpha=232, seed=7632)
    draw_vietnamese_handwriting(note, (96, 326), "Chìa cổng phụ tôi buộc sẵn bên ngoài bì chống ẩm.", size=31, color=(34, 42, 56), slant=-0.12, base_angle=0.1, alpha=216, seed=7633)
    draw_vietnamese_handwriting(note, (96, 396), "Thư cũ của cô Lan và giấy tờ liên quan nằm bên trong.", size=31, color=(34, 42, 56), slant=-0.12, base_angle=-0.2, alpha=216, seed=7634)
    draw_vietnamese_handwriting(note, (710, 526), "Chú Bảy", size=40, color=(26, 34, 52), slant=-0.16, base_angle=-0.8, alpha=226, seed=7635, centered=True)
    draw_signature_flourish(dn, 665, 586, seed=7632, color=(34, 44, 64), scale=0.86)
    note.save(DEST / "Doc_BayNote.png")

    # 2. Authentic Late-20th-Century Vietnamese Handwritten Letter (Doc_LanLetter2005.png + _Back.png)
    #    Modeled closely on authentic Vietnamese personal letter composition: warm aged cream paper, faint subtle lines,
    #    quarter-fold creases, centered date & salutation, flowing faded blue-indigo pen handwriting, 2-line centered closing.
    #    100% verbatim Core 07 Evidence Specs!14 text — ZERO spoilers, ZERO printed boxes, ZERO artist notes!
    lan = make_paper_base(seed=7641, base_rgb=(224, 214, 192), age=0.66, punch_holes=False, fold_y=0.50, damp_corner=True)
    # Add gentle vertical center fold crease (classic folded-in-quarters letter tucked inside an envelope)
    l_arr = np.asarray(lan, dtype=np.float32) / 255.0
    xx_l = np.linspace(0.0, 1.0, W, dtype=np.float32)[None, :]
    v_dark = np.exp(-((xx_l - 0.50) * 135.0) ** 2)
    v_hi = np.exp(-((xx_l - 0.504) * 175.0) ** 2)
    l_arr -= v_dark[..., None] * 0.048
    l_arr += v_hi[..., None] * 0.018
    lan = Image.fromarray(np.uint8(np.clip(l_arr, 0.0, 1.0) * 255.0), mode="RGB")

    dl = ImageDraw.Draw(lan)
    # Very faint vintage notebook horizontal ruling lines (subtle, never overpowering the handwriting)
    for gy in range(214, H - 150, 68):
        dl.line((76, gy, W - 76, gy), fill=(196, 190, 172), width=1)

    ink_lan = (34, 54, 96)
    # Centered traditional Vietnamese date header & salutation (inspired by reference letter)
    draw_vietnamese_handwriting(lan, (W // 2, 118), "An Sinh, ngày 17 tháng 11 năm 2005", size=34, color=ink_lan, slant=-0.16, base_angle=-0.4, alpha=214, seed=7640, centered=True)
    # Subtle handwritten pen underline under day/month digits
    dl.line((454, 162, 488, 161), fill=(*ink_lan,), width=2)
    dl.line((586, 161, 616, 160), fill=(*ink_lan,), width=2)
    draw_vietnamese_handwriting(lan, (W // 2, 194), "Anh Minh,", size=40, color=(28, 48, 92), slant=-0.17, base_angle=-0.5, alpha=224, seed=7641, centered=True)

    # Natural indented paragraphs in flowing blue-indigo handwriting (100% Core 07 canon!)
    lan_paragraphs = [
        (168, 304, "Em cần nói với anh một chuyện ở bệnh viện."),
        (118, 392, "Anh có thể đến gặp em sau ca trực không?"),
        (118, 480, "Em không muốn để chuyện này qua thêm một ngày nữa."),
    ]
    for idx, (lx, ly, line_txt) in enumerate(lan_paragraphs):
        draw_vietnamese_handwriting(
            lan,
            (lx, ly),
            line_txt,
            size=38,
            color=ink_lan,
            slant=-0.17,
            base_angle=-0.35 + (idx % 2) * 0.25,
            alpha=218,
            seed=7642 + idx * 17,
            centered=False,
        )

    # Classic 2-line centered-right closing signature ("Em / Lan") matching late-20th-century Vietnamese letter style
    draw_vietnamese_handwriting(lan, (648, 690), "Em", size=38, color=ink_lan, slant=-0.18, base_angle=-0.6, alpha=214, seed=7650, centered=True)
    draw_vietnamese_handwriting(lan, (654, 764), "Lan", size=50, color=(26, 46, 90), slant=-0.19, base_angle=-1.0, alpha=226, seed=7651, centered=True)
    draw_ink_smudge(lan, (790, 920), radius=38, color=(54, 72, 108), seed=7644)
    lan.save(DEST / "Doc_LanLetter2005.png")

    # Back of Lan's letter: subtle ink bleed-through + ONLY Bảy's brief pencil note at bottom (ZERO material/fold/artist notes!)
    lan_back = make_paper_base(seed=7645, base_rgb=(222, 212, 190), age=0.66, punch_holes=False, fold_y=0.50, damp_corner=True)
    mirrored_lan = lan.transpose(Image.Transpose.FLIP_LEFT_RIGHT).filter(ImageFilter.GaussianBlur(1.5))
    lb_arr = np.asarray(lan_back, dtype=np.float32)
    ml_arr = np.asarray(mirrored_lan, dtype=np.float32)
    lb_arr = np.clip(lb_arr - np.clip((210.0 - ml_arr) / 210.0, 0.0, 1.0) * 14.0, 0.0, 255.0)
    lan_back = Image.fromarray(np.uint8(lb_arr), mode="RGB")
    draw_vietnamese_handwriting(lan_back, (132, 990), "Tìm thấy kẹp sau ngăn kéo tủ lưu trữ khi dọn kho.", size=28, color=INK_PENCIL, slant=-0.14, base_angle=-0.5, alpha=184, seed=7646)
    draw_vietnamese_handwriting(lan_back, (670, 1054), "16-11-2012 — Bảy", size=27, color=INK_PENCIL, slant=-0.14, base_angle=-0.3, alpha=176, seed=7647, centered=True)
    lan_back.save(DEST / "Doc_LanLetter2005_Back.png")

    # 3. Torn 19/11/2005 Newspaper Clipping (Doc_Newspaper2005.png — strictly 19/11/2005 facts per Core 07, ZERO 2006/demolition/wrong-name paradox!)
    pw, ph = 1024, 1180
    news = make_paper_base(pw, ph, base_rgb=(196, 188, 166), seed=7651, age=0.80, punch_holes=False, fold_y=0.52, damp_corner=True)
    dnw = ImageDraw.Draw(news)
    dnw.rectangle((40, 40, pw - 40, ph - 40), outline=(72, 68, 58), width=3)
    dnw.text((70, 62), "BÁO ĐỊA PHƯƠNG  ·  SỐ RA NGÀY 19/11/2005  ·  TIN THỜI SỰ", font=font(20, True), fill=(62, 58, 50))
    dnw.line((64, 102, pw - 64, 102), fill=(48, 46, 40), width=4)
    dnw.line((64, 112, pw - 64, 112), fill=(82, 78, 68), width=1)
    dnw.text((pw // 2, 176), "HỎA HOẠN RẠNG SÁNG TẠI", font=font(40, True), fill=(32, 30, 26), anchor="mm")
    dnw.text((pw // 2, 232), "BỆNH VIỆN TỪ THIỆN AN SINH:", font=font(42, True), fill=(32, 30, 26), anchor="mm")
    dnw.text((pw // 2, 290), "13 NGƯỜI TỬ VONG", font=font(46, True), fill=(116, 36, 30), anchor="mm")
    dnw.line((90, 336, pw - 90, 336), fill=(92, 88, 76), width=2)

    col_lines = [
        "Rạng sáng ngày 18/11/2005, một vụ hỏa hoạn nghiêm trọng bùng phát",
        "tại Bệnh viện từ thiện An Sinh trong cơn mưa lớn. Ngọn lửa lan nhanh",
        "từ khu kỹ thuật sang khu lưu bệnh tạm thời thuộc diện bảo trợ nhân",
        "đạo, khiến công tác tiếp cận gặp nhiều trở ngại.",
        "",
        "Theo thông cáo sơ bộ của cơ quan chức năng trong sáng cùng ngày,",
        "vụ cháy đã cướp đi sinh mạng của 13 người, trong đó có 01 nữ điều",
        "dưỡng đang trực ca đêm. Nguyên nhân ban đầu được xác định là sự cố",
        "chập điện hệ thống dây dẫn khu kỹ thuật.",
        "",
        "Cơ sở đã tạm đình chỉ tiếp nhận bệnh nhân để cơ quan công an phong",
        "tỏa, khám nghiệm hiện trường và tiếp tục xác minh danh tính đầy đủ",
        "các nạn nhân theo quy định.",
    ]
    y_cur = 374
    for ln in col_lines:
        if ln:
            dnw.text((76, y_cur), ln, font=font(24, False), fill=(44, 42, 36))
        y_cur += 40
    dnw.line((76, y_cur + 16, pw - 76, y_cur + 16), fill=(112, 108, 94), width=2)
    dnw.text((76, y_cur + 38), "THÔNG TIN TIẾP THEO (TRANG 4):", font=font(20, True), fill=(58, 54, 46))
    # Authentic faint water-smeared newsprint lines instead of bracketed "[Mép báo rách...]" text!
    dnw.text((76, y_cur + 76), "Danh sách sơ bộ các nạn nhân và thân nhân liên hệ bàn tiếp...", font=font(21, False), fill=(76, 72, 62))
    dnw.text((76, y_cur + 112), "1. Nữ điều dưỡng trực đêm khoa Nội trú · 2. Bệnh nhân lưu...", font=font(20, False), fill=(118, 110, 96))
    # Heavy authentic damp/torn crease obscuring lower-right column
    draw_ink_smudge(news, (640, y_cur + 108), radius=105, color=(138, 128, 106), seed=7655)
    draw_ink_smudge(news, (790, y_cur + 122), radius=118, color=(98, 88, 70), seed=7659)
    news.save(DEST / "Doc_Newspaper2005.png")

    # 4. Wing-C Key Manila Tag (Doc_KeyCTag.png — 1024 x 620)
    tw, th = 1024, 620
    tag = make_paper_base(tw, th, base_rgb=(210, 192, 148), seed=7661, age=0.62, punch_holes=False, fold_y=0.0, damp_corner=False)
    dt = ImageDraw.Draw(tag)
    dt.rectangle((34, 34, tw - 34, th - 34), outline=(98, 84, 58), width=4)
    dt.ellipse((78, th // 2 - 36, 150, th // 2 + 36), fill=(130, 102, 66), outline=(74, 58, 38), width=5)
    dt.ellipse((96, th // 2 - 18, 132, th // 2 + 18), fill=(42, 38, 32))
    dt.text((220, 130), "THẺ CHÌA KHÓA KHO THANH LÝ", font=font(26, True), fill=INK_SUB)
    dt.line((220, 174, tw - 80, 174), fill=(116, 98, 68), width=2)
    draw_handwritten(tag, (226, 224), "KHU C  —  CỬA SAU CŨ", size=46, color=INK_PRINT, slant=-0.10)
    draw_handwritten(tag, (226, 328), "ĐÃ THAY KHÓA (2006)", size=44, color=INK_STAMP_RED, slant=-0.12, angle_deg=-1.2)
    draw_handwritten(tag, (226, 434), "Thu hồi từ kho lưu trữ · không dùng cho cổng phụ. — Bảy", size=25, color=INK_SUB, slant=-0.08)
    tag.save(DEST / "Doc_KeyCTag.png")

    # 5. Minh's 2012 Mobile Phone LCD Screen (Doc_PhoneScreen.png — 512 x 680 — authentic 2012 softkeys, ZERO PC keybinds!)
    sw, sh = 512, 680
    scr = Image.new("RGB", (sw, sh), (18, 34, 36))
    arr = np.asarray(scr, dtype=np.float32)
    yy = np.linspace(0.0, 1.0, sh, dtype=np.float32)[:, None]
    arr[..., 0] += 18.0 + 16.0 * (1.0 - yy)
    arr[..., 1] += 48.0 + 28.0 * (1.0 - yy)
    arr[..., 2] += 42.0 + 22.0 * (1.0 - yy)
    scr = Image.fromarray(np.uint8(np.clip(arr, 0, 255)), mode="RGB")
    ds = ImageDraw.Draw(scr)
    ds.rectangle((14, 14, sw - 14, sh - 14), outline=(140, 188, 176), width=3)
    ds.rectangle((14, 14, sw - 14, 72), fill=(26, 56, 56))
    ds.text((34, 30), "VN-MOBI", font=font(22, True), fill=(182, 232, 214))
    ds.text((sw - 34, 30), "22:50", font=font(24, True), fill=(224, 244, 232), anchor="ra")
    ds.text((sw // 2, 126), "HỘP THƯ ĐẾN (2)", font=font(24, True), fill=(216, 240, 224), anchor="mm")
    ds.line((32, 162, sw - 32, 162), fill=(104, 152, 140), width=2)
    ds.rounded_rectangle((28, 190, sw - 28, 390), 10, fill=(24, 52, 52), outline=(136, 192, 174), width=2)
    ds.text((48, 208), "Chú Bảy · 22:48", font=font(22, True), fill=(244, 236, 178))
    ds.text((48, 252), "Tôi đang ở viện.", font=font(26, True), fill=(238, 246, 238))
    ds.text((48, 292), "Hoãn nhé, cậu đừng", font=font(26, True), fill=(238, 246, 238))
    ds.text((48, 332), "vào trong.", font=font(26, True), fill=(238, 246, 238))
    ds.rounded_rectangle((28, 414, sw - 28, 560), 10, fill=(20, 44, 44), outline=(92, 134, 124), width=2)
    ds.text((48, 432), "Chú Bảy · 16/11 19:15", font=font(20, True), fill=(176, 212, 198))
    ds.text((48, 468), "Minh à, dọn kho thanh lý", font=font(20, False), fill=(204, 224, 214))
    ds.text((48, 500), "tôi tìm thấy lá thư cũ...", font=font(20, False), fill=(204, 224, 214))
    ds.rectangle((14, sh - 68, sw - 14, sh - 14), fill=(22, 48, 48))
    ds.text((38, sh - 52), "Tùy chọn", font=font(22, True), fill=(192, 236, 218))
    ds.text((sw // 2, sh - 52), "Đọc", font=font(22, True), fill=(244, 238, 186), anchor="ma")
    ds.text((sw - 38, sh - 52), "Trở về", font=font(22, True), fill=(192, 236, 218), anchor="ra")
    scr.save(DEST / "Doc_PhoneScreen.png")


def build_signs_and_pr01() -> None:
    """Enamel plaque for the Nursing Alcove + subtle non-exportable CH01-PR01 crayon corner."""
    # Nursing Alcove Plaque
    w, h = 1024, 390
    s = make_paper_base(w, h, base_rgb=(228, 220, 192), seed=7701, age=0.45, punch_holes=False, fold_y=0.0)
    ds = ImageDraw.Draw(s)
    ink = (36, 58, 50)
    ds.rounded_rectangle((14, 14, w - 14, h - 14), 6, outline=ink, width=8)
    ds.rounded_rectangle((28, 28, w - 28, h - 28), 4, outline=ink, width=2)
    ds.text((w // 2, 165), "HỒ SƠ TRỰC", font=font(112, True), fill=ink, anchor="mm")
    ds.text((w // 2, 298), "KHU HÀNH CHÍNH  ·  NỘI TRÚ TẦNG TRỆT", font=font(34, True), fill=ink, anchor="mm")
    for sx in (44, w - 44):
        for sy in (44, h - 44):
            ds.ellipse((sx - 11, sy - 11, sx + 11, sy + 11), fill=(109, 71, 45))
            ds.ellipse((sx - 7, sy - 7, sx + 7, sy + 7), fill=(61, 58, 51))
    s.save(DEST / "Sign_NursingAlcove.png")

    # CH01-PR01: Subtle torn crayon paper corner revealed in B ONLY after Refuted (NO name, NO C01, NO door!)
    pw, ph = 512, 512
    pr = Image.new("RGBA", (pw, ph), (0, 0, 0, 0))
    # Torn triangular/irregular corner sheet
    poly = [(48, 62), (436, 92), (398, 424), (112, 448), (64, 240)]
    mask = Image.new("L", (pw, ph), 0)
    ImageDraw.Draw(mask).polygon(poly, fill=255)
    paper = make_paper_base(pw, ph, base_rgb=(212, 198, 164), seed=7710, age=0.65, punch_holes=False, fold_y=0.0)
    dp = ImageDraw.Draw(paper)
    # Just a fragment of red crayon roof edge & warm yellow sun ray — pure visual continuity for CH02!
    dp.line([(85, 290), (225, 162), (390, 238)], fill=(156, 72, 56), width=14)
    dp.line([(92, 298), (226, 174), (382, 244)], fill=(176, 88, 68), width=6)
    dp.arc((275, 108, 345, 178), 20, 260, fill=(168, 134, 62), width=8)
    pr.paste(paper, (0, 0), mask)
    pr.save(DEST / "CH01_PR01_Corner.png")


def normal_from_height_doc(height: np.ndarray, strength: float = 6.5) -> Image.Image:
    """Sobel tangent-space OpenGL normal map (Y-up for Unity) from a normalized 0..1 height field."""
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
    v = np.stack((dx, dy, np.ones_like(dx)), axis=-1)
    v /= np.maximum(1e-5, np.linalg.norm(v, axis=-1, keepdims=True))
    return Image.fromarray(np.uint8(np.clip(v * 0.5 + 0.5, 0.0, 1.0) * 255.0), mode="RGB")


def build_raking_indentation_maps() -> None:
    """Generate tangent-space ballpoint indentation normal maps & reveal masks for raking-light 3D inspection.

    When viewed straight-on in PhysicalInspector, these impressions stay barely perceptible on paper fibers.
    Tilting the reverse page 25-40 deg into keyLight catches raking specular highlights along the pen grooves:
    - ObsCard_Back (AS-05B): impressed shift note '03g17 · CHƯA CHỐT SỔ'.
    - F101_Back: erased ballpoint line 'GIƯỜNG 04 — CHƯA CHỐT [GẠCH BỎ] → 0'
    """
    specs = (
        (
            "ObsCard_Back",
            7811,
            (
                (140, 680, "03g17 · CHƯA CHỐT SỔ", 42, -0.16, -1.2),
                (148, 764, "CHƯA CÓ NGƯỜI NHẬN — ĐỂ LẠI THẺ", 31, -0.14, -0.6),
                (152, 838, "GIƯỜNG 04 · PHÒNG 101", 27, -0.12, 0.4),
            ),
            # Strike-through scratch lines over the first row to simulate frantic erasure
            ((135, 704, 835, 696), (140, 718, 820, 710)),
        ),
        (
            "F101_Back",
            7821,
            (
                (128, 660, "GIƯỜNG 04 — CHƯA CHỐT SỔ", 40, -0.15, -1.0),
                (136, 744, "GẠCH BỎ MỤC TẠM → GHI TRÒN: 0", 33, -0.16, -0.5),
                (142, 822, "03g17 · CHƯA CHỐT BÀN GIAO", 28, -0.12, 0.3),
            ),
            ((120, 684, 820, 678), (128, 766, 840, 756)),
        ),
    )

    for prefix, seed, lines, scratches in specs:
        fiber = fbm_2d(W, H, (24, 64, 160), seed=seed) * 0.08
        indent_img = Image.new("L", (W, H), 0)
        d = ImageDraw.Draw(indent_img)

        for x, y, txt, sz, slant, ang in lines:
            f = font(sz, bold=True)
            tmp = Image.new("L", (W, int(sz * 2.4)), 0)
            dt = ImageDraw.Draw(tmp)
            dt.text((24, int(sz * 0.45)), txt, font=f, fill=240)
            sheared = tmp.transform(
                tmp.size,
                Image.Transform.AFFINE,
                (1.0, slant, -slant * sz, 0.0, 1.0, 0.0),
                resample=Image.Resampling.BICUBIC,
            )
            if abs(ang) > 0.05:
                sheared = sheared.rotate(ang, resample=Image.Resampling.BICUBIC, expand=False)
            indent_img.paste(sheared, (x - 24, y - int(sz * 0.45)), sheared)

        for x0, y0, x1, y1 in scratches:
            d.line([(x0, y0), (x1, y1)], fill=165, width=4)

        # Soft bevel profile around ballpoint pressure grooves so raking light catches crisp ridge + trough
        groove_sharp = np.asarray(indent_img.filter(ImageFilter.GaussianBlur(1.1)), dtype=np.float32) / 255.0
        groove_wide = np.asarray(indent_img.filter(ImageFilter.GaussianBlur(3.4)), dtype=np.float32) / 255.0
        ridge = np.clip(groove_wide - groove_sharp * 0.55, 0.0, 1.0)
        height = np.clip(0.55 + fiber - groove_sharp * 0.42 + ridge * 0.24, 0.0, 1.0)

        normal_img = normal_from_height_doc(height, strength=9.5)
        normal_img.save(DEST / f"{prefix}_Indent_Normal.png")

        mask_arr = np.clip(groove_sharp * 1.15 + ridge * 0.45, 0.0, 1.0)
        Image.fromarray(np.uint8(mask_arr * 255.0), mode="L").save(DEST / f"{prefix}_Indent_Mask.png")


def main() -> None:
    DEST.mkdir(parents=True, exist_ok=True)
    build_f101_suite()
    build_obs101b_bed4_card()
    build_bed2_completed_transfer()
    build_bed3_medication_log()
    build_nursing_alcove_and_reception_docs()
    build_bay_envelope_and_letter()
    build_ch01_rebirth3_e10a_parts()
    build_signs_and_pr01()
    build_raking_indentation_maps()
    print(f"CH01-R documents + raking indentation maps generated cleanly in {DEST}")


if __name__ == "__main__":
    main()
