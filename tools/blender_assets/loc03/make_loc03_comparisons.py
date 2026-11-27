"""Generate labeled Before/After comparison sheets, R/B contrast boards & H.264 MP4 videos for LOC03 Room 101."""
from __future__ import annotations

import subprocess
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]
BEFORE_SHOTS = ROOT / "output" / "loc03-hero" / "before" / "shots-High"
AFTER_SHOTS = ROOT / "output" / "loc03-hero" / "final" / "shots-High"
CMP_DIR = ROOT / "output" / "loc03-hero" / "comparisons"
VID_DIR = CMP_DIR / "videos"
CMP_DIR.mkdir(parents=True, exist_ok=True)
VID_DIR.mkdir(parents=True, exist_ok=True)

FFMPEG = Path(r"C:\Users\phanb\.vscode\extensions\kilocode.kilo-code-7.8.32-win32-x64\bin\ffmpeg.exe")


def load_font(size: int, bold: bool = True):
    for p in (
        ["C:/Windows/Fonts/arialbd.ttf", "C:/Windows/Fonts/tahomabd.ttf"]
        if bold
        else ["C:/Windows/Fonts/arial.ttf", "C:/Windows/Fonts/tahoma.ttf"]
    ):
        if Path(p).exists():
            return ImageFont.truetype(p, size)
    return ImageFont.load_default()


def label_strip(img: Image.Image, text: str, bg=(18, 26, 23)) -> Image.Image:
    w, h = img.size
    bar = 44
    out = Image.new("RGB", (w, h + bar), bg)
    out.paste(img, (0, bar))
    d = ImageDraw.Draw(out)
    d.text((16, 10), text, fill=(236, 240, 232), font=load_font(22, bold=True))
    return out


def side_by_side(left_path: Path, right_path: Path, left_lbl: str, right_lbl: str, out_path: Path, scale_w: int = 960):
    if not left_path.exists() or not right_path.exists():
        return
    a = Image.open(left_path).convert("RGB")
    b = Image.open(right_path).convert("RGB")
    scale_h = int(round(scale_w * a.height / a.width))
    a = label_strip(a.resize((scale_w, scale_h), Image.Resampling.LANCZOS), left_lbl, bg=(42, 26, 24))
    b = label_strip(b.resize((scale_w, scale_h), Image.Resampling.LANCZOS), right_lbl, bg=(20, 42, 34))
    gap = 8
    combo = Image.new("RGB", (a.width + b.width + gap, max(a.height, b.height)), (12, 16, 15))
    combo.paste(a, (0, 0))
    combo.paste(b, (a.width + gap, 0))
    combo.save(out_path)


def make_summary_board(pairs, title: str, out_path: Path):
    """Compose a 2x2 grid of Before|After pairs into one master 1920px-wide comparison board."""
    tiles = []
    for lp, rp, cap in pairs:
        if not lp.exists() or not rp.exists():
            continue
        a = Image.open(lp).convert("RGB").resize((472, 266), Image.Resampling.LANCZOS)
        b = Image.open(rp).convert("RGB").resize((472, 266), Image.Resampling.LANCZOS)
        a = label_strip(a, f"BEFORE · {cap}", bg=(48, 26, 24))
        b = label_strip(b, f"AFTER (LOC03 HERO) · {cap}", bg=(18, 46, 36))
        pair = Image.new("RGB", (a.width + b.width + 6, a.height), (12, 16, 15))
        pair.paste(a, (0, 0))
        pair.paste(b, (a.width + 6, 0))
        tiles.append(pair)
    if not tiles:
        return
    cols = 2
    rows = (len(tiles) + 1) // 2
    tw, th = tiles[0].size
    hdr = 56
    pad = 12
    board = Image.new("RGB", (cols * tw + (cols + 1) * pad, hdr + rows * th + (rows + 1) * pad), (12, 16, 15))
    d = ImageDraw.Draw(board)
    d.text((20, 14), title, fill=(238, 242, 234), font=load_font(28, bold=True))
    for i, t in enumerate(tiles):
        r, c = divmod(i, cols)
        board.paste(t, (pad + c * (tw + pad), hdr + pad + r * (th + pad)))
    board.save(out_path)


def encode_videos():
    if not FFMPEG.exists():
        print(f"WARN: ffmpeg not found at {FFMPEG}")
        return
    for layer in ("B", "R"):
        b_dir = ROOT / "output" / "loc03-hero" / "before" / "video-High" / f"frames_{layer}"
        a_dir = ROOT / "output" / "loc03-hero" / "final" / "shots-High" / f"frames_{layer}"
        b_mp4 = VID_DIR / f"loc03_before_{layer}.mp4"
        a_mp4 = VID_DIR / f"loc03_after_{layer}.mp4"
        s_mp4 = VID_DIR / f"loc03_before_after_sidebyside_{layer}.mp4"
        for src_dir, dst in ((b_dir, b_mp4), (a_dir, a_mp4)):
            if (src_dir / "f00000.png").exists():
                subprocess.run([
                    str(FFMPEG), "-y", "-framerate", "30",
                    "-i", str(src_dir / "f%05d.png"),
                    "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "18", "-preset", "medium",
                    str(dst)
                ], check=True)
        if b_mp4.exists() and a_mp4.exists():
            # Synchronized 1920x540 crisp side-by-side Before | After video with clean top banner labels
            vf = (
                "[0:v]scale=960:540,drawbox=x=0:y=0:w=960:h=36:color=0x2a1816@0.82:t=fill,"
                "drawtext=text='BEFORE (Baseline)':fontcolor=white:fontsize=20:x=16:y=9[L];"
                "[1:v]scale=960:540,drawbox=x=0:y=0:w=960:h=36:color=0x142c24@0.82:t=fill,"
                "drawtext=text='AFTER (LOC03 Hero Overhaul)':fontcolor=white:fontsize=20:x=16:y=9[R];"
                "[L][R]hstack=inputs=2[v]"
            )
            subprocess.run([
                str(FFMPEG), "-y", "-i", str(b_mp4), "-i", str(a_mp4),
                "-filter_complex", vf, "-map", "[v]",
                "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "18", "-preset", "medium",
                str(s_mp4)
            ], check=True)


def main():
    key_shots = [
        ("L03-02-hero-wide",      "Hero Wide"),
        ("L03-04-bed1-close",     "Bed 1 & Care Tray"),
        ("L03-05-bed2-thermos",   "Bed 2, Thermos & OBS101C"),
        ("L03-06-bed3-drawer",    "Bed 3 & Interactive Drawer"),
        ("L03-07-bed4-alcove",    "Bed 4 Alcove"),
        ("L03-09-cabinet-store",  "Medical Cabinet & Store Door"),
        ("L03-10-f101-wall",      "Ward Door & F101 Clipboard"),
        ("L03-11-windows-north",  "North Steel Windows"),
        ("L03-12-east-basin",     "East Wall Handwashing Basin"),
        ("L03-13-ceiling-fans",   "Ceiling Beams, Fans & T8"),
    ]
    for shot, title in key_shots:
        for layer, yr in (("B", "2005 Layer B"), ("R", "2012 Layer R")):
            side_by_side(
                BEFORE_SHOTS / f"{shot}-{layer}.png",
                AFTER_SHOTS / f"{shot}-{layer}.png",
                f"BEFORE · {title} ({yr})",
                f"AFTER (LOC03 Hero) · {title} ({yr})",
                CMP_DIR / f"cmp_{shot}_{layer}.png",
            )
        # R vs B contrast split of our Final LOC03 Hero
        side_by_side(
            AFTER_SHOTS / f"{shot}-B.png",
            AFTER_SHOTS / f"{shot}-R.png",
            f"LAYER B (2005 Operational) · {title}",
            f"LAYER R (2012 Abandoned) · {title}",
            CMP_DIR / f"rb_{shot}.png",
        )

    # R-only flashlight & Store Room comparisons
    for shot, title in [
        ("L03-14-store-interior-R", "Store Room (Kho giuong · Bed 4 Payoff)"),
        ("L03-15-torch-beds-R-R",   "Flashlight R · Ward Wide"),
        ("L03-16-torch-bed4-R-R",   "Flashlight R · Bed 4 & Socket 4"),
    ]:
        side_by_side(
            BEFORE_SHOTS / f"{shot}.png",
            AFTER_SHOTS / f"{shot}.png",
            f"BEFORE · {title}",
            f"AFTER (LOC03 Hero) · {title}",
            CMP_DIR / f"cmp_{shot}.png",
        )

    # Master 4-panel summary boards
    make_summary_board(
        [
            (BEFORE_SHOTS / "L03-02-hero-wide-B.png",    AFTER_SHOTS / "L03-02-hero-wide-B.png",    "Room 101 Wide"),
            (BEFORE_SHOTS / "L03-05-bed2-thermos-B.png", AFTER_SHOTS / "L03-05-bed2-thermos-B.png", "Bed 2 & OBS101C"),
            (BEFORE_SHOTS / "L03-12-east-basin-B.png",   AFTER_SHOTS / "L03-12-east-basin-B.png",   "East Wall Basin"),
            (BEFORE_SHOTS / "L03-13-ceiling-fans-B.png", AFTER_SHOTS / "L03-13-ceiling-fans-B.png", "Ceiling & Fans"),
        ],
        "LOC03 — ROOM 101 VISUAL OVERHAUL · LAYER B (2005 OPERATIONAL)",
        CMP_DIR / "SUMMARY_BEFORE_AFTER_B.png",
    )
    make_summary_board(
        [
            (BEFORE_SHOTS / "L03-02-hero-wide-R.png",      AFTER_SHOTS / "L03-02-hero-wide-R.png",      "Room 101 Wide R"),
            (BEFORE_SHOTS / "L03-07-bed4-alcove-R.png",    AFTER_SHOTS / "L03-07-bed4-alcove-R.png",    "Bed 4 & Socket 4"),
            (BEFORE_SHOTS / "L03-14-store-interior-R.png", AFTER_SHOTS / "L03-14-store-interior-R.png", "Store Room Payoff"),
            (BEFORE_SHOTS / "L03-15-torch-beds-R-R.png",   AFTER_SHOTS / "L03-15-torch-beds-R-R.png",   "Flashlight R"),
        ],
        "LOC03 — ROOM 101 VISUAL OVERHAUL · LAYER R (2012 ABANDONED)",
        CMP_DIR / "SUMMARY_BEFORE_AFTER_R.png",
    )
    encode_videos()
    print(f"Generated LOC03 comparisons & videos in {CMP_DIR}")


if __name__ == "__main__":
    main()
