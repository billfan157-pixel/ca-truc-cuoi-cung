"""Generate labeled Before/After comparison boards and synchronized side-by-side MP4 videos for LOC02 Reception Overhaul."""
import argparse
import shutil
import subprocess
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
FFMPEG_CANDIDATES = [
    Path(r"C:\Users\phanb\.vscode\extensions\kilocode.kilo-code-7.8.32-win32-x64\bin\ffmpeg.exe"),
    Path(r"C:\Users\phanb\.codeium\ws-browser\ffmpeg-1011\ffmpeg-win64.exe"),
]


def find_ffmpeg() -> str:
    for p in FFMPEG_CANDIDATES:
        if p.exists():
            return str(p)
    found = shutil.which("ffmpeg")
    if found:
        return found
    raise FileNotFoundError("ffmpeg executable not found")


def load_font(size: int, bold: bool = False):
    candidates = [
        r"C:\Windows\Fonts\segoeuib.ttf" if bold else r"C:\Windows\Fonts\segoeui.ttf",
        r"C:\Windows\Fonts\arialbd.ttf" if bold else r"C:\Windows\Fonts\arial.ttf",
    ]
    for c in candidates:
        if Path(c).exists():
            return ImageFont.truetype(c, size)
    return ImageFont.load_default()


def make_side_by_side(before_path: Path, after_path: Path, out_path: Path, title: str, left_lbl: str, right_lbl: str):
    ib = Image.open(before_path).convert("RGB")
    ia = Image.open(after_path).convert("RGB")
    w, h = 1280, 720
    ib = ib.resize((w, h), Image.Resampling.LANCZOS)
    ia = ia.resize((w, h), Image.Resampling.LANCZOS)

    hdr_h = 56
    gap = 12
    canvas = Image.new("RGB", (w * 2 + gap, h + hdr_h), (18, 22, 22))
    canvas.paste(ib, (0, hdr_h))
    canvas.paste(ia, (w + gap, hdr_h))

    draw = ImageDraw.Draw(canvas)
    f_title = load_font(22, bold=True)
    f_sub = load_font(18, bold=True)

    # Header bar
    draw.rectangle([(0, 0), (w * 2 + gap, hdr_h)], fill=(20, 26, 25))
    draw.text((22, 14), f"LOC02 HERO OVERHAUL  |  {title}", fill=(235, 232, 220), font=f_title)

    # Badges over top-left of each half
    for x0, lbl, bg in ((16, left_lbl, (90, 42, 38)), (w + gap + 16, right_lbl, (26, 94, 68))):
        tw = draw.textlength(lbl, font=f_sub)
        draw.rounded_rectangle([(x0, hdr_h + 14), (x0 + tw + 26, hdr_h + 48)], radius=6, fill=bg)
        draw.text((x0 + 13, hdr_h + 19), lbl, fill=(248, 246, 240), font=f_sub)

    out_path.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(out_path, "PNG", optimize=True)


def make_contact_grid(pairs, out_path: Path, title: str):
    """Build a 2-column (Before | After) multi-row vertical hero showcase board."""
    tw, th = 960, 540
    gap = 10
    hdr = 64
    row_hdr = 34
    rows = len(pairs)
    W = tw * 2 + gap
    H = hdr + rows * (th + row_hdr + gap)
    canvas = Image.new("RGB", (W, H), (16, 20, 20))
    draw = ImageDraw.Draw(canvas)
    f_big = load_font(26, bold=True)
    f_row = load_font(19, bold=True)
    draw.text((22, 16), title, fill=(240, 236, 224), font=f_big)

    y = hdr
    for label, bp, ap in pairs:
        draw.rectangle([(0, y), (W, y + row_hdr)], fill=(26, 33, 31))
        draw.text((16, y + 6), f"BEFORE  —  {label}", fill=(224, 168, 152), font=f_row)
        draw.text((tw + gap + 16, y + 6), f"AFTER (Blender 4.5 + URP)  —  {label}", fill=(148, 228, 186), font=f_row)
        y += row_hdr
        ib = Image.open(bp).convert("RGB").resize((tw, th), Image.Resampling.LANCZOS)
        ia = Image.open(ap).convert("RGB").resize((tw, th), Image.Resampling.LANCZOS)
        canvas.paste(ib, (0, y))
        canvas.paste(ia, (tw + gap, y))
        y += th + gap

    out_path.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(out_path, "PNG", optimize=True)


def encode_video(frames_dir: Path, out_mp4: Path, fps: int = 30):
    ff = find_ffmpeg()
    out_mp4.parent.mkdir(parents=True, exist_ok=True)
    cmd = [
        ff, "-y",
        "-framerate", str(fps),
        "-i", str(frames_dir / "f%05d.png"),
        "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "18", "-preset", "medium",
        "-movflags", "+faststart",
        str(out_mp4),
    ]
    subprocess.run(cmd, check=True)


def encode_sidebyside_video(before_mp4: Path, after_mp4: Path, out_mp4: Path):
    ff = find_ffmpeg()
    out_mp4.parent.mkdir(parents=True, exist_ok=True)
    # Scale each to 960x540 (total 1920x540) and hstack side by side!
    vf = (
        "[0:v]scale=960:540:flags=lanczos[l];"
        "[1:v]scale=960:540:flags=lanczos[r];"
        "[l][r]hstack=inputs=2[v]"
    )
    cmd = [
        ff, "-y",
        "-i", str(before_mp4),
        "-i", str(after_mp4),
        "-filter_complex", vf,
        "-map", "[v]",
        "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "18", "-preset", "medium",
        "-movflags", "+faststart",
        str(out_mp4),
    ]
    subprocess.run(cmd, check=True)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--before-shots", type=Path, required=True)
    ap.add_argument("--after-shots", type=Path, required=True)
    ap.add_argument("--out-dir", type=Path, required=True)
    ap.add_argument("--before-video", type=Path, default=None)
    ap.add_argument("--after-video", type=Path, default=None)
    ap.add_argument("--fps", type=int, default=30)
    args = ap.parse_args()

    key_shots = [
        ("L02-01-arrival-B", "Arrival Wide — Layer B (2005 Operational)"),
        ("L02-01-arrival-R", "Arrival Wide — Layer R (2012 Abandoned)"),
        ("L02-02-hero-wide-B", "Hero Reception Counter & Partition — Layer B (2005)"),
        ("L02-02-hero-wide-R", "Hero Reception Counter & Partition — Layer R (2012)"),
        ("L02-03-counter-front-B", "Counter Front & Pass-Through Window — Layer B (2005)"),
        ("L02-05-ledger-close-R", "Close-Up AN01 Duty Ledger & Phone — Layer R (2012)"),
        ("L02-05-ledger-close-B", "Close-Up AN01 Duty Ledger & Phone — Layer B (2005)"),
        ("L02-09-behind-desk-B", "Lan's Workstation, Chair & Kneehole — Layer B (2005)"),
        ("L02-10-file-rack-B", "Wall Archive Rack, S1 Door & Switchboard — Layer B (2005)"),
        ("L02-11-wall-clock-B", "Wall Clock (03:17) Through Glazing — Layer B (2005)"),
        ("L02-11-wall-clock-R", "Wall Clock (11:52) Through Glazing — Layer R (2012)"),
        ("L02-12-waiting-bench-B", "Lobby Ceiling Beams, Fan & Architecture — Layer B (2005)"),
        ("L02-13-ceiling-fan-B", "Ceiling Fan Orientation Fix & Beams — Layer B (2005)"),
        ("L02-15-torch-counter-R", "Flashlight Raking Detail — Layer R (2012)"),
    ]

    for stem, title in key_shots:
        bp = args.before_shots / f"{stem}.png"
        ap_img = args.after_shots / f"{stem}.png"
        if bp.exists() and ap_img.exists():
            make_side_by_side(bp, ap_img, args.out_dir / f"cmp_{stem}.png", title, "BEFORE (Baseline)", "AFTER (Hero Overhaul)")
            print(f"[CMP] cmp_{stem}.png")

    # R vs B Hero Split Showcase
    for stem, title in [
        ("L02-01-arrival", "Arrival Wide — 2005 Layer B vs 2012 Layer R"),
        ("L02-02-hero-wide", "Hero Reception Counter — 2005 Layer B vs 2012 Layer R"),
        ("L02-05-ledger-close", "Close-Up AN01 Ledger & Phone — 2005 Layer B vs 2012 Layer R"),
        ("L02-11-wall-clock", "Wall Clock Canon Contrast — 03:17 (B) vs 11:52 (R)"),
    ]:
        pb = args.after_shots / f"{stem}-B.png"
        pr = args.after_shots / f"{stem}-R.png"
        if pb.exists() and pr.exists():
            make_side_by_side(pb, pr, args.out_dir / f"rb_{stem}.png", title, "LAYER B · 2005 (Operational)", "LAYER R · 2012 (Abandoned)")
            print(f"[RB]  rb_{stem}.png")

    # Master 4-row contact sheets for quick overview
    grid_b = [
        ("Hero Reception Wide (B 2005)", args.before_shots / "L02-02-hero-wide-B.png", args.after_shots / "L02-02-hero-wide-B.png"),
        ("Lan's Workstation & Stool (B 2005)", args.before_shots / "L02-09-behind-desk-B.png", args.after_shots / "L02-09-behind-desk-B.png"),
        ("Close-Up AN01 Ledger & Phone (B 2005)", args.before_shots / "L02-05-ledger-close-B.png", args.after_shots / "L02-05-ledger-close-B.png"),
        ("Arrival, S1 Door & Ceiling Beams (B 2005)", args.before_shots / "L02-01-arrival-B.png", args.after_shots / "L02-01-arrival-B.png"),
    ]
    if all(bp.exists() and ap_i.exists() for _, bp, ap_i in grid_b):
        make_contact_grid(grid_b, args.out_dir / "SUMMARY_BEFORE_AFTER_B.png", "LOC02 RECEPTION HERO OVERHAUL — BEFORE vs AFTER (LAYER B · 2005)")
        print("[GRID] SUMMARY_BEFORE_AFTER_B.png")

    grid_r = [
        ("Hero Reception Wide (R 2012)", args.before_shots / "L02-02-hero-wide-R.png", args.after_shots / "L02-02-hero-wide-R.png"),
        ("Close-Up AN01 Ledger & Lamp (R 2012)", args.before_shots / "L02-05-ledger-close-R.png", args.after_shots / "L02-05-ledger-close-R.png"),
        ("Arrival, S1 Door & Moonlight (R 2012)", args.before_shots / "L02-01-arrival-R.png", args.after_shots / "L02-01-arrival-R.png"),
        ("Flashlight Detail on Counter (R 2012)", args.before_shots / "L02-15-torch-counter-R.png", args.after_shots / "L02-15-torch-counter-R.png"),
    ]
    if all(bp.exists() and ap_i.exists() for _, bp, ap_i in grid_r):
        make_contact_grid(grid_r, args.out_dir / "SUMMARY_BEFORE_AFTER_R.png", "LOC02 RECEPTION HERO OVERHAUL — BEFORE vs AFTER (LAYER R · 2012)")
        print("[GRID] SUMMARY_BEFORE_AFTER_R.png")

    if args.before_video and args.after_video:
        vid_dir = args.out_dir / "videos"
        for layer in ("B", "R"):
            bf = args.before_video / f"frames_{layer}"
            af = args.after_video / f"frames_{layer}"
            if bf.exists() and af.exists():
                b_mp4 = vid_dir / f"loc02_before_{layer}.mp4"
                a_mp4 = vid_dir / f"loc02_after_{layer}.mp4"
                s_mp4 = vid_dir / f"loc02_before_after_sidebyside_{layer}.mp4"
                encode_video(bf, b_mp4, args.fps)
                encode_video(af, a_mp4, args.fps)
                encode_sidebyside_video(b_mp4, a_mp4, s_mp4)
                print(f"[VIDEO] {b_mp4.name}, {a_mp4.name}, {s_mp4.name}")


if __name__ == "__main__":
    main()
