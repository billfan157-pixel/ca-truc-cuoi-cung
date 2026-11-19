"""Compose labeled QA contact sheet boards from rendered PNG panels (system Python + Pillow)."""

import json
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
FONT_PATH = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Resources" / "CTCC" / "BeVietnamPro-SemiBold.ttf"


def make_board(spec: dict):
    panels = spec["panels"]
    cols = int(spec["cols"])
    cw = int(spec["cw"])
    ch = int(spec["ch"])
    title = spec["title"]
    subtitle = spec["subtitle"]
    out_path = Path(spec["out_path"])

    rows = (len(panels) + cols - 1) // cols
    pad = 14
    hdr = 74
    lbl = 30
    w = pad + cols * (cw + pad)
    h = hdr + rows * (ch + lbl + pad) + pad
    im = Image.new("RGB", (w, h), (18, 22, 22))
    draw = ImageDraw.Draw(im)
    try:
        ft = ImageFont.truetype(str(FONT_PATH), 24)
        fs = ImageFont.truetype(str(FONT_PATH), 14)
        fl = ImageFont.truetype(str(FONT_PATH), 15)
    except Exception:
        ft = fs = fl = ImageFont.load_default()
    draw.rectangle([0, 0, w, hdr - 3], fill=(24, 30, 30))
    draw.text((pad, 12), title, fill=(235, 236, 226), font=ft)
    draw.text((pad, 44), subtitle, fill=(168, 182, 176), font=fs)

    for idx, item in enumerate(panels):
        label, path = item[0], item[1]
        r, c = divmod(idx, cols)
        x = pad + c * (cw + pad)
        y = hdr + pad // 2 + r * (ch + lbl + pad)
        draw.rectangle([x - 1, y - 1, x + cw + 1, y + ch + lbl + 1], outline=(58, 68, 66))
        if path and Path(path).exists():
            src = Image.open(path).convert("RGB")
            sr = src.width / float(src.height)
            dr = cw / float(ch)
            if sr > dr:
                nh = ch
                nw = int(round(nh * sr))
                src = src.resize((nw, nh), Image.Resampling.LANCZOS)
                l0 = (nw - cw) // 2
                src = src.crop((l0, 0, l0 + cw, ch))
            else:
                nw = cw
                nh = int(round(nw / sr))
                src = src.resize((nw, nh), Image.Resampling.LANCZOS)
                t0 = (nh - ch) // 2
                src = src.crop((0, t0, cw, t0 + ch))
            im.paste(src, (x, y))
        draw.rectangle([x, y + ch, x + cw, y + ch + lbl], fill=(26, 32, 32))
        draw.text((x + 10, y + ch + 5), label, fill=(224, 228, 220), font=fl)

    out_path.parent.mkdir(parents=True, exist_ok=True)
    im.save(out_path, quality=92, optimize=True)
    print(f"[QA BOARD] Saved {out_path}")


if __name__ == "__main__":
    spec_path = Path(sys.argv[1])
    data = json.loads(spec_path.read_text(encoding="utf-8"))
    for s in data if isinstance(data, list) else [data]:
        make_board(s)
