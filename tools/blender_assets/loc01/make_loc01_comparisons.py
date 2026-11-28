"""Generate labeled Before/After comparison sheets, R/B contrast boards & H.264 MP4 walkthrough
videos for the LOC01 Exterior & Entrance Visual Overhaul."""
from __future__ import annotations

import subprocess
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]
BEFORE_SHOTS = ROOT / "output" / "loc01-hero" / "before" / "shots-High"
AFTER_SHOTS = ROOT / "output" / "loc01-hero" / "final" / "shots-High"
CMP_DIR = ROOT / "output" / "loc01-hero" / "comparisons"
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
        b_mp4 = VID_DIR / f"loc01_before_{layer}.mp4"
        a_mp4 = VID_DIR / f"loc01_after_{layer}.mp4"
        s_mp4 = VID_DIR / f"loc01_before_after_sidebyside_{layer}.mp4"
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
                "drawtext=text='AFTER (LOC01 Exterior Hero Overhaul)':fontcolor=white:fontsize=20:x=16:y=9[R];"
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
        ("EX-01-ap00-arrival",         "AP00 Opening Arrival View"),
        ("EX-02-ap00-wide-ka",         "Wide Street & 3F Block K-A"),
        ("EX-03-mailbox-closeup",      "B01 Mailbox & East Pillar"),
        ("EX-04-gate-ironwork",        "GATE-CONG Ironwork & Plaque"),
        ("EX-05-motorbike-hero",       "Minh Commuter Motorbike"),
        ("EX-06-guardhouse-ext",       "1987 Security Guardhouse"),
        ("EX-07-guardhouse-window",    "Guardhouse Interior Detail"),
        ("EX-08-courtyard-ap01",       "AP01 Courtyard & Facade"),
        ("EX-09-canopy-portico",       "Entry Portico Canopy & Drain"),
        ("EX-10-entry-doors-glass",    "Entrance Portal -> LOC02"),
        ("EX-11-west-directory-bench", "Directory Kiosk & Benches"),
        ("EX-12-east-planter-tree",    "East Planter, Tree & Drains"),
        ("EX-13-b07-return-gate",      "B07 Look-Back Toward Gate"),
        ("EX-14-ka-facade-upper",      "K-A Upper Bays & Breeze-Blocks"),
        ("EX-18-overview-3q-elevated", "Elevated 3/4 LOC01 Overview"),
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
            f"LAYER R (2012 Monsoon Night) · {title}",
            CMP_DIR / f"rb_{shot}.png",
        )

    for shot, title in [
        ("EX-15-puddles-curbs-low-R",    "Low Angle Wetness, Puddles & Curbs"),
        ("EX-16-torch-gate-mailbox-R-R", "Flashlight R · Gate & Mailbox"),
        ("EX-17-torch-canopy-doors-R-R", "Flashlight R · Canopy & Entrance Doors"),
    ]:
        side_by_side(
            BEFORE_SHOTS / f"{shot}.png",
            AFTER_SHOTS / f"{shot}.png",
            f"BEFORE · {title}",
            f"AFTER (Hero) · {title}",
            CMP_DIR / f"cmp_{shot}.png",
        )

    make_summary_board(
        [
            (BEFORE_SHOTS / "EX-02-ap00-wide-ka-R.png",      AFTER_SHOTS / "EX-02-ap00-wide-ka-R.png",      "Wide K-A & Gate Arch"),
            (BEFORE_SHOTS / "EX-09-canopy-portico-R.png",    AFTER_SHOTS / "EX-09-canopy-portico-R.png",    "Entry Portico & Doors"),
            (BEFORE_SHOTS / "EX-06-guardhouse-ext-R.png",    AFTER_SHOTS / "EX-06-guardhouse-ext-R.png",    "Security Guardhouse"),
            (BEFORE_SHOTS / "EX-05-motorbike-hero-R.png",    AFTER_SHOTS / "EX-05-motorbike-hero-R.png",    "Minh Motorbike"),
        ],
        "LOC01 EXTERIOR & ENTRANCE OVERHAUL · LAYER R (2012 MONSOON NIGHT)",
        CMP_DIR / "SUMMARY_BEFORE_AFTER_R.png",
    )
    make_summary_board(
        [
            (BEFORE_SHOTS / "EX-01-ap00-arrival-R.png",      AFTER_SHOTS / "EX-01-ap00-arrival-R.png",      "AP00 Opening Arrival"),
            (BEFORE_SHOTS / "EX-03-mailbox-closeup-R.png",   AFTER_SHOTS / "EX-03-mailbox-closeup-R.png",   "B01 Hero Mailbox"),
            (BEFORE_SHOTS / "EX-12-east-planter-tree-R.png", AFTER_SHOTS / "EX-12-east-planter-tree-R.png", "East Planter & Flora"),
            (BEFORE_SHOTS / "EX-13-b07-return-gate-R.png",   AFTER_SHOTS / "EX-13-b07-return-gate-R.png",   "B07 Return Look-Back"),
        ],
        "LOC01 EXTERIOR & ENTRANCE OVERHAUL · HERO DETAILS & B01/B07 SIGHTLINES",
        CMP_DIR / "SUMMARY_BEFORE_AFTER_HERO_R.png",
    )
    make_summary_board(
        [
            (BEFORE_SHOTS / "EX-02-ap00-wide-ka-B.png",      AFTER_SHOTS / "EX-02-ap00-wide-ka-B.png",      "Wide K-A & Gate Arch B"),
            (BEFORE_SHOTS / "EX-08-courtyard-ap01-B.png",    AFTER_SHOTS / "EX-08-courtyard-ap01-B.png",    "AP01 Courtyard B"),
            (BEFORE_SHOTS / "EX-09-canopy-portico-B.png",    AFTER_SHOTS / "EX-09-canopy-portico-B.png",    "Entry Portico B"),
            (BEFORE_SHOTS / "EX-18-overview-3q-elevated-B.png", AFTER_SHOTS / "EX-18-overview-3q-elevated-B.png", "3/4 Overview B"),
        ],
        "LOC01 EXTERIOR & ENTRANCE OVERHAUL · LAYER B (2005 OPERATIONAL)",
        CMP_DIR / "SUMMARY_BEFORE_AFTER_B.png",
    )
    make_summary_board(
        [
            (AFTER_SHOTS / "EX-02-ap00-wide-ka-B.png",       AFTER_SHOTS / "EX-02-ap00-wide-ka-R.png",      "Wide Block K-A"),
            (AFTER_SHOTS / "EX-09-canopy-portico-B.png",     AFTER_SHOTS / "EX-09-canopy-portico-R.png",    "Entry Portico Canopy"),
            (AFTER_SHOTS / "EX-06-guardhouse-ext-B.png",     AFTER_SHOTS / "EX-06-guardhouse-ext-R.png",    "Security Guardhouse"),
            (AFTER_SHOTS / "EX-18-overview-3q-elevated-B.png", AFTER_SHOTS / "EX-18-overview-3q-elevated-R.png", "3/4 LOC01 Overview"),
        ],
        "LOC01 EXTERIOR OVERHAUL · LAYER B (2005) VS LAYER R (2012)",
        CMP_DIR / "SUMMARY_RB_CONTRAST.png",
        left_tag="2005 B",
        right_tag="2012 R",
    )
    encode_videos()
    print(f"LOC01_COMPARISONS_DONE -> {CMP_DIR}")


if __name__ == "__main__":
    main()
