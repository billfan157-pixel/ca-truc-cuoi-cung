"""Compose M3.0 commit-specific visual comparison sheets and encode synchronized H.264+AAC MP4 gameplay video via Blender 4.5 VSE."""

import argparse
import json
import subprocess
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
FONT_PATH = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Resources" / "CTCC" / "BeVietnamPro-SemiBold.ttf"
BLENDER_EXE = Path(r"C:\Program Files\Blender Foundation\Blender 4.5\blender.exe")


def load_font(size):
    try:
        return ImageFont.truetype(str(FONT_PATH), size)
    except Exception:
        return ImageFont.load_default()


def make_grid(panels, cols, cell_w, cell_h, title, subtitle, out_path):
    rows = (len(panels) + cols - 1) // cols
    pad = 18
    header_h = 84
    label_h = 34
    w = pad + cols * (cell_w + pad)
    h = header_h + rows * (cell_h + label_h + pad) + pad
    canvas = Image.new("RGB", (w, h), (14, 20, 19))
    draw = ImageDraw.Draw(canvas)
    f_title = load_font(26)
    f_sub = load_font(15)
    f_lbl = load_font(16)

    draw.rectangle([0, 0, w, header_h - 4], fill=(19, 28, 26))
    draw.rectangle([0, header_h - 4, w, header_h - 2], fill=(138, 166, 126))
    draw.text((pad, 14), title, fill=(232, 232, 214), font=f_title)
    draw.text((pad, 52), subtitle, fill=(166, 184, 170), font=f_sub)

    for idx, (label, path) in enumerate(panels):
        r, c = divmod(idx, cols)
        x = pad + c * (cell_w + pad)
        y = header_h + pad // 2 + r * (cell_h + label_h + pad)
        draw.rectangle([x - 1, y - 1, x + cell_w + 1, y + cell_h + label_h + 1], outline=(58, 76, 68))
        if path and Path(path).is_file():
            im = Image.open(path).convert("RGB")
            src_ratio = im.width / float(im.height)
            dst_ratio = cell_w / float(cell_h)
            if src_ratio > dst_ratio:
                nh = cell_h
                nw = int(round(nh * src_ratio))
                im = im.resize((nw, nh), Image.Resampling.LANCZOS)
                left = (nw - cell_w) // 2
                im = im.crop((left, 0, left + cell_w, cell_h))
            else:
                nw = cell_w
                nh = int(round(nw / src_ratio))
                im = im.resize((nw, nh), Image.Resampling.LANCZOS)
                top = (nh - cell_h) // 2
                im = im.crop((0, top, cell_w, top + cell_h))
            canvas.paste(im, (x, y))
        else:
            draw.rectangle([x, y, x + cell_w, y + cell_h], fill=(28, 34, 34))
            draw.text((x + 16, y + cell_h // 2), f"Missing: {path}", fill=(200, 120, 100), font=f_lbl)
        draw.rectangle([x, y + cell_h, x + cell_w, y + cell_h + label_h], fill=(22, 32, 30))
        draw.text((x + 12, y + cell_h + 7), label, fill=(224, 228, 212), font=f_lbl)

    out_path.parent.mkdir(parents=True, exist_ok=True)
    if out_path.suffix.lower() in (".jpg", ".jpeg"):
        canvas.save(out_path, quality=91, optimize=True)
    else:
        canvas.save(out_path, optimize=True)
    print(f"[M30-BOARD] Saved {out_path} ({w}x{h})")


def _mix_audio_cues(frames_dir: Path, n_frames: int, fps: int, out_wav: Path) -> int:
    import array
    import wave

    sr = 48000
    total_samples = int(round((n_frames / float(fps)) * sr))
    mix = [0.0] * (total_samples * 2)

    cue_path = frames_dir / "audio_cues.json"
    cues = json.loads(cue_path.read_text(encoding="utf-8")).get("cues", []) if cue_path.is_file() else []
    applied = 0
    for cue in cues:
        asset_abs = (ROOT / cue["asset"]).resolve()
        if not asset_abs.is_file():
            continue
        frame_start = int(cue.get("frame", 0))
        gain = float(cue.get("gain", 1.0))
        start_sample = int(round((frame_start / float(fps)) * sr))
        with wave.open(str(asset_abs), "rb") as wf:
            ch = wf.getnchannels()
            sw = wf.getsampwidth()
            in_sr = wf.getframerate()
            raw = wf.readframes(wf.getnframes())
        if sw != 2:
            continue
        samples = array.array("h")
        samples.frombytes(raw)
        n_in = len(samples) // ch
        ratio = in_sr / float(sr)
        out_len = int(n_in / ratio)
        for i in range(out_len):
            dst = start_sample + i
            if dst < 0 or dst >= total_samples:
                continue
            src_f = i * ratio
            s0 = int(src_f)
            s1 = min(s0 + 1, n_in - 1)
            frac = src_f - s0
            if ch == 1:
                v = (samples[s0] * (1.0 - frac) + samples[s1] * frac) / 32768.0 * gain
                mix[dst * 2] += v
                mix[dst * 2 + 1] += v
            else:
                vl = (samples[s0 * ch] * (1.0 - frac) + samples[s1 * ch] * frac) / 32768.0 * gain
                vr = (samples[s0 * ch + 1] * (1.0 - frac) + samples[s1 * ch + 1] * frac) / 32768.0 * gain
                mix[dst * 2] += vl
                mix[dst * 2 + 1] += vr
        applied += 1

    pcm = array.array("h", [max(-32767, min(32767, int(round(v * 32767.0)))) for v in mix])
    with wave.open(str(out_wav), "wb") as wf:
        wf.setnchannels(2)
        wf.setsampwidth(2)
        wf.setframerate(sr)
        wf.writeframes(pcm.tobytes())
    return applied


def encode_mp4_with_audio(frames_dir: Path, out_mp4: Path, fps: int = 30):
    frames = sorted(frames_dir.glob("frame_*.jpg"))
    if not frames:
        raise FileNotFoundError(f"No recorded frame_*.jpg found in {frames_dir}")

    mix_wav = frames_dir / "_walkthrough_mix.wav"
    n_cues = _mix_audio_cues(frames_dir, len(frames), fps, mix_wav)
    out_mp4.parent.mkdir(parents=True, exist_ok=True)

    ffmpeg_exe = subprocess.check_output(
        ["uv", "run", "--python", "3.11", "--with", "imageio-ffmpeg",
         "python", "-c", "import imageio_ffmpeg; print(imageio_ffmpeg.get_ffmpeg_exe())"],
        text=True,
    ).strip().splitlines()[-1]

    cmd = [
        ffmpeg_exe, "-y",
        "-framerate", str(fps),
        "-i", str(frames_dir / "frame_%04d.jpg"),
        "-i", str(mix_wav),
        "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "20", "-preset", "medium",
        "-c:a", "aac", "-b:a", "192k",
        "-shortest", "-movflags", "+faststart",
        str(out_mp4),
    ]
    subprocess.run(cmd, check=True)
    mix_wav.unlink(missing_ok=True)
    print(f"[M30-VIDEO] Encoded synchronized H.264+AAC MP4 -> {out_mp4} ({len(frames)} frames @ {fps}fps, {n_cues} audio cues)")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--qa-dir", type=Path, required=True)
    ap.add_argument("--baseline-dir", type=Path, default=ROOT / "docs" / "m2" / "evidence" / "m2c1")
    ap.add_argument("--out-dir", type=Path, default=ROOT / "docs" / "m3" / "evidence")
    ap.add_argument("--frames-dir", type=Path, default=None)
    args = ap.parse_args()

    qa = args.qa_dir
    base = args.baseline_dir

    # 1. In-Game Unity 1080p Before (M2-C.1 c0531dc) vs After (M3.0 Candidate B) + LOD0..2 Silhouette Integrity
    lan_panels = [
        ("M2-C.1 (c0531dc) · LOC02 B Reception", base / "unity-lan-b-candidate.jpg"),
        ("M3.0 Candidate B · LOC02 B Close-up (Sculpt + Hair Cards + SSS)", qa / "m2c-lan-closeup-B.png"),
        ("M2-C.1 (c0531dc) · F101 Handover", base / "unity-lan-handover.jpg"),
        ("M3.0 Candidate B · F101 Handover (Two-Bone Arm IK + Printed F101)", qa / "m2c-lan-handover-B.png"),
        ("M3.0 Candidate B · Neutral 5500K Studio (Collar + Flat ID Badge)", qa / "m2c-lan-neutral-studio.png"),
        ("M3.0 Candidate B · Forced LOD2 Speech (17.4k tris · Protected Collar)", qa / "m2c1-lan-lod2-speech.png"),
    ]
    make_grid(
        lan_panels, 2, 820, 461,
        "CA TRỰC CUỐI CÙNG — M3.0 IN-ENGINE LAN COMPARISON & LOD SILHOUETTE AUDIT",
        "Unity 6.3 URP Forward+ 1080p Medium (D3D12) · Commit c0531dc vs M3.0 Candidate B",
        args.out_dir / "m30-lan-ingame-before-after.jpg",
    )

    # 2. All 3 Upgraded Characters + Representative CH03 (LOC05 Nurse, LOC07 Archive R, LOC07 Memory M)
    gallery_panels = [
        ("M3.0 · LOC02 Reception — CHAR002 Trần Ngọc Lan (F101 Handover IK)", qa / "m2c-lan-handover-B.png"),
        ("M3.0 · LOC03 Room 101 — CHAR007 Bé An (Depth Prepass + Queues)", qa / "m2c-an-vignette-R.png"),
        ("M3.0 · LOC03/04 — CHAR005 Gatekeeper (Teeth/Tongue + 0.009 m/s Slip)", qa / "m2c-gatekeeper-closeup-R.png"),
        ("M3.0 CH03 · LOC05 Nurse Station & E03 12-Code Register (R)", qa / "ch03-loc05-nurse-R.png"),
        ("M3.0 CH03 · LOC07 Archive Racks, C02 Knitting Bag & C12 Box (R)", qa / "ch03-loc07-archive-R.png"),
        ("M3.0 CH03 · LOC07 Reality.M Memory Vignette (Warm Amber Grade)", qa / "ch03-loc07-memory-M.png"),
    ]
    make_grid(
        gallery_panels, 3, 620, 349,
        "CA TRỰC CUỐI CÙNG — M3.0 DIGITAL HUMANS & REPRESENTATIVE CH03 ARCHIVE WING",
        "6 Additive Scenes (Regional Stream LOC05_ArchiveWing) · Blender 4.5 Assets · R / B / M Reality",
        args.out_dir / "m30-characters-and-ch03-gallery.jpg",
    )

    # 3. Synchronized H.264+AAC MP4 Gameplay Performance Video
    if args.frames_dir and args.frames_dir.is_dir():
        encode_mp4_with_audio(args.frames_dir, args.out_dir / "m30-character-performance.mp4", fps=30)


if __name__ == "__main__":
    main()
