"""Generate side-by-side BEFORE (M1) vs AFTER (M0-R) comparison boards and hero close-up showcase sheet."""

import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageStat

ROOT = Path(__file__).resolve().parents[1]
FONT_BOLD = ROOT / "Prototype/Assets/CTCC/Art/Resources/CTCC/BeVietnamPro-SemiBold.ttf"
FONT_REG = ROOT / "Prototype/Assets/CTCC/Art/Resources/CTCC/BeVietnamPro-Regular.ttf"


def make_pair(before_path, after_path, title, out_path):
    before = Image.open(before_path).convert("RGB")
    after = Image.open(after_path).convert("RGB")
    assert min(ImageStat.Stat(after).stddev) > 7, f"{after_path} looks blank or flat"

    w, h = 840, 472
    b_res = before.resize((w, h), Image.Resampling.LANCZOS)
    a_res = after.resize((w, h), Image.Resampling.LANCZOS)

    board = Image.new("RGB", (w * 2 + 42, h + 110), "#111917")
    dr = ImageDraw.Draw(board)
    f_title = ImageFont.truetype(str(FONT_BOLD), 26)
    f_sub = ImageFont.truetype(str(FONT_REG), 20)

    dr.text((16, 14), title, font=f_title, fill="#e6ece1")
    board.paste(b_res, (14, 56))
    board.paste(a_res, (w + 28, 56))
    dr.text((18, h + 68), "BEFORE · M1 Baseline (primitives + flat light)", font=f_sub, fill="#9eb0a5")
    dr.text((w + 32, h + 68), "AFTER · M0-R Blender Assets + PBR + DBuffer Decals + Real Shadows", font=f_sub, fill="#cce0ae")
    out_path.parent.mkdir(parents=True, exist_ok=True)
    board.save(out_path, quality=93)
    print("Saved", out_path)


def make_showcase(after_dir, out_path):
    items = [
        ("room101-R.png", "Room 101 · R (2012 Abandoned)"),
        ("room101-B.png", "Room 101 · B (2005 Report)"),
        ("corridor-R.png", "Corridor · Door 101 Transom & Timber Leaf"),
        ("corridor-long-R.png", "Corridor · Pilasters, Handrails, PA & Depth"),
        ("closeup-bed-iv.png", "Hero Props · WardBed, IV Stand, Thermos, Slippers"),
        ("closeup-window-rain.png", "Architecture · Steel Window, Iron Grille, Rain & Trees"),
        ("closeup-cabinet-wheels.png", "Investigation · Bed 4 Wheel Trails & Glass Medical Cabinet"),
        ("closeup-inspect-3d.png", "Gameplay · 3D Physical Evidence Inspection (Bảy's Envelope)"),
    ]
    cols, rows = 2, 4
    tw, th = 720, 405
    board = Image.new("RGB", (cols * (tw + 20) + 20, rows * (th + 46) + 70), "#111917")
    dr = ImageDraw.Draw(board)
    f_title = ImageFont.truetype(str(FONT_BOLD), 28)
    f_cap = ImageFont.truetype(str(FONT_REG), 19)
    dr.text((20, 18), "CA TRỰC CUỐI CÙNG · M0-R Visual & Gameplay Reconstruction Showcase", font=f_title, fill="#e8eee2")
    for idx, (fname, cap) in enumerate(items):
        p = after_dir / fname
        if not p.exists():
            continue
        im = Image.open(p).convert("RGB").resize((tw, th), Image.Resampling.LANCZOS)
        x = 20 + (idx % cols) * (tw + 20)
        y = 66 + (idx // cols) * (th + 46)
        board.paste(im, (x, y))
        dr.text((x + 4, y + th + 10), cap, font=f_cap, fill="#c9d9bd")
    board.save(out_path, quality=92)
    print("Saved", out_path)


def main():
    after_dir = Path(sys.argv[1]).resolve()
    out_dir = Path(sys.argv[2]).resolve()
    before_dir = ROOT / "output" / "qa-reference-v2"
    make_pair(before_dir / "room101-R.png", after_dir / "room101-R.png",
              "Room 101 · R (2012 Monsoon Abandoned) — Same Camera Coordinate", out_dir / "before-after-room101-R.jpg")
    make_pair(before_dir / "room101-B.png", after_dir / "room101-B.png",
              "Room 101 · B (2005 Operational Report) — Same Camera Coordinate", out_dir / "before-after-room101-B.jpg")
    make_pair(before_dir / "corridor-R.png", after_dir / "corridor-R.png",
              "Corridor Approach to Room 101 — Same Camera Coordinate", out_dir / "before-after-corridor-R.jpg")
    make_showcase(after_dir, out_dir / "m0r-showcase-board.jpg")


if __name__ == "__main__":
    main()
