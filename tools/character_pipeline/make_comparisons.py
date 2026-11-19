"""Build honest M2-A (49bc068) vs M2-C Before/After Visual Comparison Boards (Blender & Unity)."""

from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
FONT_BOLD = ROOT / "Prototype/Assets/CTCC/Art/Resources/CTCC/BeVietnamPro-SemiBold.ttf"
FONT_REG = ROOT / "Prototype/Assets/CTCC/Art/Resources/CTCC/BeVietnamPro-Regular.ttf"


def fit_crop(path: Path, w: int, h: int, crop_box=None):
    im = Image.open(path).convert("RGB")
    if crop_box is not None:
        iw, ih = im.size
        im = im.crop((int(iw * crop_box[0]), int(ih * crop_box[1]), int(iw * crop_box[2]), int(ih * crop_box[3])))
    sr = im.width / float(im.height)
    dr = w / float(h)
    if sr > dr:
        nh = h
        nw = int(round(nh * sr))
        im = im.resize((nw, nh), Image.Resampling.LANCZOS)
        x0 = (nw - w) // 2
        im = im.crop((x0, 0, x0 + w, h))
    else:
        nw = w
        nh = int(round(nw / sr))
        im = im.resize((nw, nh), Image.Resampling.LANCZOS)
        y0 = (nh - h) // 2
        im = im.crop((0, y0, w, y0 + h))
    return im


def fit_contain(path: Path, w: int, h: int, crop_box=None, bg=(34, 38, 38)):
    im = Image.open(path).convert("RGB")
    if crop_box is not None:
        iw, ih = im.size
        im = im.crop((int(iw * crop_box[0]), int(ih * crop_box[1]), int(iw * crop_box[2]), int(ih * crop_box[3])))
    sr = im.width / float(im.height)
    dr = w / float(h)
    if sr > dr:
        nw = w
        nh = int(round(nw / sr))
    else:
        nh = h
        nw = int(round(nh * sr))
    im = im.resize((nw, nh), Image.Resampling.LANCZOS)
    out = Image.new("RGB", (w, h), bg)
    out.paste(im, ((w - nw) // 2, (h - nh) // 2))
    return out


def build_pair_board(rows_spec, title, subtitle, out_path: Path, cw=640, ch=420, contain=False):
    pad = 16
    hdr = 78
    lbl = 32
    W = pad + 2 * (cw + pad)
    H = hdr + len(rows_spec) * (ch + lbl + pad) + pad
    canvas = Image.new("RGB", (W, H), (16, 20, 20))
    draw = ImageDraw.Draw(canvas)
    try:
        ft = ImageFont.truetype(str(FONT_BOLD), 24)
        fs = ImageFont.truetype(str(FONT_REG), 14)
        fl = ImageFont.truetype(str(FONT_BOLD), 15)
    except Exception:
        ft = fs = fl = ImageFont.load_default()
    draw.rectangle([0, 0, W, hdr - 3], fill=(22, 28, 28))
    draw.text((pad, 12), title, fill=(238, 240, 232), font=ft)
    draw.text((pad, 46), subtitle, fill=(168, 182, 176), font=fs)

    for r_idx, (left_lbl, left_path, left_crop, right_lbl, right_path, right_crop) in enumerate(rows_spec):
        y = hdr + pad // 2 + r_idx * (ch + lbl + pad)
        for c_idx, (l_txt, p_src, cbox, accent) in enumerate([
            (left_lbl, left_path, left_crop, (164, 108, 92)),
            (right_lbl, right_path, right_crop, (86, 168, 142)),
        ]):
            x = pad + c_idx * (cw + pad)
            draw.rectangle([x - 1, y - 1, x + cw + 1, y + ch + lbl + 1], outline=accent, width=2)
            if p_src and Path(p_src).exists():
                img = fit_contain(Path(p_src), cw, ch, cbox) if contain else fit_crop(Path(p_src), cw, ch, cbox)
                canvas.paste(img, (x, y))
            draw.rectangle([x, y + ch, x + cw, y + ch + lbl], fill=(24, 30, 30))
            draw.text((x + 10, y + ch + 6), l_txt, fill=(230, 234, 226), font=fl)

    out_path.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(out_path, quality=92, optimize=True)
    print(f"[COMPARE] Saved {out_path}")


def main():
    m2a = ROOT / "output/m2a"
    m2c_b = ROOT / "output/m2c/blender-qa"
    m2c_u = ROOT / "output/m2c/release-module"

    # 1. Blender Studio Before/After (Exact raw M2-A renders vs M2-C Digital Humans — zero head crop!)
    blender_rows = [
        ("BEFORE (M2-A 49bc068) — Lan Procedural Base", m2a / "blender/final/CTCC_CHR_Lan.png", None,
         "AFTER (M2-C) — Lan Vietnamese hm08 + Hair + Cloth Sim", m2c_b / "CTCC_CHR_Lan/full_34.png", None),
        ("BEFORE (M2-A 49bc068) — Lan Face Portrait", m2a / "blender/final/CTCC_CHR_Lan_Portrait.png", None,
         "AFTER (M2-C) — Lan Vietnamese Portrait (29 BlendShapes)", m2c_b / "CTCC_CHR_Lan/portrait_34.png", None),
        ("BEFORE (M2-A 49bc068) — Bé An 7-Bone Prototype", m2a / "blender/baseline-CTCC_CHR_AnApparition.png", None,
         "AFTER (M2-C) — Bé An 64-Bone Vietnamese Child + Cloth", m2c_b / "CTCC_CHR_AnApparition/full_34.png", None),
        ("BEFORE (M2-A 49bc068) — Gatekeeper Prototype", m2a / "blender/baseline-CTCC_CHR_Gatekeeper.png", None,
         "AFTER (M2-C) — Gatekeeper 1.94m Vietnamese + Scorched Cloth", m2c_b / "CTCC_CHR_Gatekeeper/full_34.png", None),
    ]
    build_pair_board(
        blender_rows,
        "M2-A vs M2-C — BLENDER DIGITAL HUMAN RECONSTRUCTION COMPARISON",
        "Honest side-by-side: procedural M2-A baseline (49bc068) vs M2-C Blender 4.5 Vietnamese MPFB2 hm08 + Cloth Sim + Hair + Rig",
        ROOT / "output/m2c/m2c-before-after-blender.jpg",
        cw=540, ch=560, contain=True,
    )

    # 2. Unity 6.3 URP 17.3 In-Game Before/After
    unity_rows = [
        ("BEFORE (M2-A 49bc068) — LOC02 Lan Reception (B 2005)", m2a / "release-module/loc02-lan-B.png", None,
         "AFTER (M2-C) — LOC02 Lan Digital Human (URP Forward+)", m2c_u / "loc02-lan-B.png", None),
        ("BEFORE (M2-A 49bc068) — LOC02 Lan Crop", m2a / "release-module/loc02-lan-B.png", (0.28, 0.14, 0.68, 0.72),
         "AFTER (M2-C) — LOC02 Lan Close-Up (Skin SSS, Eyes, Badge)", m2c_u / "m2c-lan-closeup-B.png", None),
        ("BEFORE (M2-A 49bc068) — Bé An 7-Bone Prototype", m2a / "blender/baseline-CTCC_CHR_AnApparition.png", None,
         "AFTER (M2-C) — Bé An Unity In-Game Apparition (R)", m2c_u / "m2c-an-vignette-R.png", None),
        ("BEFORE (M2-A 49bc068) — LOC04 Gatekeeper Cameo (R)", m2a / "release-module/loc04-gatekeeper-R.png", None,
         "AFTER (M2-C) — LOC04 Gatekeeper Cameo (R)", m2c_u / "loc04-gatekeeper-R.png", None),
        ("BEFORE (M2-A 49bc068) — Gatekeeper Prototype (Blender)", m2a / "blender/baseline-CTCC_CHR_Gatekeeper.png", None,
         "AFTER (M2-C) — Gatekeeper Studio Close-Up (Scorched Coat & Face)", m2c_b / "CTCC_CHR_Gatekeeper/portrait_34.png", None),
    ]
    build_pair_board(
        unity_rows,
        "M2-A vs M2-C — UNITY 6.3 URP IN-GAME CHARACTER COMPARISON",
        "Actual 1920x1080 D3D12 Windows player captures on Intel Arc iGPU · Before (49bc068) vs After (M2-C)",
        ROOT / "output/m2c/m2c-before-after-unity.jpg",
        cw=680, ch=400, contain=True,
    )


if __name__ == "__main__":
    main()
