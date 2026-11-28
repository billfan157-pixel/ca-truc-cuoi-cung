"""Generate labeled Before/After comparison sheets, R/B contrast boards & H.264 MP4 videos for HALL & STAFF."""
from __future__ import annotations

import subprocess
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]
BEFORE_SHOTS = ROOT / "output" / "hall-staff-hero" / "before" / "shots-High"
AFTER_SHOTS = ROOT / "output" / "hall-staff-hero" / "final" / "shots-High"
CMP_DIR = ROOT / "output" / "hall-staff-hero" / "comparisons"
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
    d.text((16, 10), text, fill=(236, 240, 232), font=load_font(21, bold=True))
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


def make_summary_board(pairs, title: str, out_path: Path, left_tag: str = "BEFORE", right_tag: str = "AFTER"):
    """Compose a 2x2 grid of labeled pairs into one master 1920px-wide comparison board."""
    tiles = []
    for lp, rp, cap in pairs:
        if not lp.exists() or not rp.exists():
            continue
        a = Image.open(lp).convert("RGB").resize((472, 266), Image.Resampling.LANCZOS)
        b = Image.open(rp).convert("RGB").resize((472, 266), Image.Resampling.LANCZOS)
        a = label_strip(a, f"{left_tag} · {cap}", bg=(48, 26, 24))
        b = label_strip(b, f"{right_tag} · {cap}", bg=(18, 46, 36))
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
        b_dir = BEFORE_SHOTS / f"frames_{layer}"
        a_dir = AFTER_SHOTS / f"frames_{layer}"
        b_mp4 = VID_DIR / f"hall_staff_before_{layer}.mp4"
        a_mp4 = VID_DIR / f"hall_staff_after_{layer}.mp4"
        s_mp4 = VID_DIR / f"hall_staff_before_after_sidebyside_{layer}.mp4"
        for src_dir, dst in ((b_dir, b_mp4), (a_dir, a_mp4)):
            if (src_dir / "f00000.png").exists():
                subprocess.run([
                    str(FFMPEG), "-y", "-framerate", "30",
                    "-i", str(src_dir / "f%05d.png"),
                    "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "18", "-preset", "medium",
                    str(dst)
                ], check=True)
        if b_mp4.exists() and a_mp4.exists():
            vf = (
                "[0:v]scale=960:540,drawbox=x=0:y=0:w=960:h=36:color=0x2a1816@0.82:t=fill,"
                "drawtext=text='BEFORE (Baseline)':fontcolor=white:fontsize=20:x=16:y=9[L];"
                "[1:v]scale=960:540,drawbox=x=0:y=0:w=960:h=36:color=0x142c24@0.82:t=fill,"
                "drawtext=text='AFTER (HALL & STAFF Hero Overhaul)':fontcolor=white:fontsize=20:x=16:y=9[R];"
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
        ("HS-01-hall-from-loc02",    "LOC02 -> HALL Portal"),
        ("HS-02-hall-south-wide",    "HALL Main Corridor Wide"),
        ("HS-03-hall-door101-pa",    "HALL Room 101 & PA Speaker"),
        ("HS-04-hall-mid-crossdoor", "HALL Cross-Door & PCCC"),
        ("HS-05-hall-extinguisher",  "PCCC Fire Station Close-up"),
        ("HS-06-hall-north-doorA",   "HALL North & ER Door A"),
        ("HS-07-hall-look-south",    "HALL Looking South -> LOC02"),
        ("HS-08-hall-ceiling-arch",  "HALL Ceiling Girders & MEP"),
        ("HS-09-staff-from-loc02",   "LOC02 -> STAFF Corridor"),
        ("HS-10-staff-workstation",  "STAFF Nursing Workstation"),
        ("HS-11-staff-desk-close",   "Nursing Desk & Corkboard"),
        ("HS-12-staff-chair-drawer", "Chair, Drawer & LOC02 View"),
        ("HS-13-staff-cross-pocket", "STAFF Cross-Door & Switch"),
        ("HS-14-staff-north-doorB",  "STAFF North, Cabinet & Door B"),
        ("HS-15-staff-look-south",   "STAFF Looking South -> LOC02"),
    ]
    for shot, title in key_shots:
        for layer, yr in (("B", "2005 Layer B"), ("R", "2012 Layer R")):
            side_by_side(
                BEFORE_SHOTS / f"{shot}-{layer}.png",
                AFTER_SHOTS / f"{shot}-{layer}.png",
                f"BEFORE · {title} ({yr})",
                f"AFTER (Hero) · {title} ({yr})",
                CMP_DIR / f"cmp_{shot}_{layer}.png",
            )
        side_by_side(
            AFTER_SHOTS / f"{shot}-B.png",
            AFTER_SHOTS / f"{shot}-R.png",
            f"LAYER B (2005 Operational) · {title}",
            f"LAYER R (2012 Abandoned) · {title}",
            CMP_DIR / f"rb_{shot}.png",
        )

    for shot, title in [
        ("HS-16-torch-hall-R-R",       "Flashlight R · HALL Wide"),
        ("HS-17-torch-staff-desk-R-R", "Flashlight R · Nursing Desk"),
        ("HS-18-torch-staff-north-R-R","Flashlight R · STAFF North & Door B"),
    ]:
        side_by_side(
            BEFORE_SHOTS / f"{shot}.png",
            AFTER_SHOTS / f"{shot}.png",
            f"BEFORE · {title}",
            f"AFTER (Hero) · {title}",
            CMP_DIR / f"cmp_{shot}.png",
        )

    # Master 4-panel overview summary boards (Layer B, Layer R, and R/B Contrast)
    make_summary_board(
        [
            (BEFORE_SHOTS / "HS-02-hall-south-wide-B.png",   AFTER_SHOTS / "HS-02-hall-south-wide-B.png",   "HALL Corridor Wide"),
            (BEFORE_SHOTS / "HS-06-hall-north-doorA-B.png",  AFTER_SHOTS / "HS-06-hall-north-doorA-B.png",  "HALL North & ER Door A"),
            (BEFORE_SHOTS / "HS-10-staff-workstation-B.png", AFTER_SHOTS / "HS-10-staff-workstation-B.png", "STAFF Nursing Desk"),
            (BEFORE_SHOTS / "HS-14-staff-north-doorB-B.png", AFTER_SHOTS / "HS-14-staff-north-doorB-B.png", "STAFF North & ER Door B"),
        ],
        "HALL & STAFF ARCHITECTURAL OVERHAUL · LAYER B (2005 OPERATIONAL)",
        CMP_DIR / "SUMMARY_BEFORE_AFTER_B.png",
    )
    make_summary_board(
        [
            (BEFORE_SHOTS / "HS-02-hall-south-wide-R.png",       AFTER_SHOTS / "HS-02-hall-south-wide-R.png",       "HALL Corridor Wide R"),
            (BEFORE_SHOTS / "HS-16-torch-hall-R-R.png",          AFTER_SHOTS / "HS-16-torch-hall-R-R.png",          "HALL Flashlight R"),
            (BEFORE_SHOTS / "HS-17-torch-staff-desk-R-R.png",    AFTER_SHOTS / "HS-17-torch-staff-desk-R-R.png",    "STAFF Desk Flashlight R"),
            (BEFORE_SHOTS / "HS-18-torch-staff-north-R-R.png",   AFTER_SHOTS / "HS-18-torch-staff-north-R-R.png",   "STAFF Door B Flashlight R"),
        ],
        "HALL & STAFF ARCHITECTURAL OVERHAUL · LAYER R (2012 ABANDONED)",
        CMP_DIR / "SUMMARY_BEFORE_AFTER_R.png",
    )
    make_summary_board(
        [
            (AFTER_SHOTS / "HS-02-hall-south-wide-B.png",   AFTER_SHOTS / "HS-02-hall-south-wide-R.png",   "HALL Main Corridor"),
            (AFTER_SHOTS / "HS-04-hall-mid-crossdoor-B.png",AFTER_SHOTS / "HS-04-hall-mid-crossdoor-R.png","HALL PCCC & Cross-Door"),
            (AFTER_SHOTS / "HS-10-staff-workstation-B.png", AFTER_SHOTS / "HS-10-staff-workstation-R.png", "STAFF Workstation"),
            (AFTER_SHOTS / "HS-14-staff-north-doorB-B.png", AFTER_SHOTS / "HS-14-staff-north-doorB-R.png", "STAFF North & Door B"),
        ],
        "HALL & STAFF HERO OVERHAUL · LAYER B (2005) VS LAYER R (2012) CONTRAST",
        CMP_DIR / "SUMMARY_RB_CONTRAST.png",
        left_tag="LAYER B (2005)",
        right_tag="LAYER R (2012)",
    )
    if "--boards-only" not in __import__("sys").argv:
        encode_videos()
    print(f"Generated HALL & STAFF comparisons & videos in {CMP_DIR}")


if __name__ == "__main__":
    main()
