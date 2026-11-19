"""Assemble M2 visual verification sheets (Blender studio showcase, M1-P vs M2 BEFORE/AFTER,
LOC01-04 vertical slice gallery) and optionally encode H.264 MP4 walkthrough via Blender 4.5 VSE."""

import argparse
import subprocess
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
FONT_PATH = ROOT / "Prototype" / "Assets" / "CTCC" / "Resources" / "CTCC" / "BeVietnamPro-SemiBold.ttf"
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
    f_title = load_font(28)
    f_sub = load_font(16)
    f_lbl = load_font(17)

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
            # Aspect-preserving center fit/crop to fill cleanly
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
    canvas.save(out_path, optimize=True)
    print(f"[BOARD] Saved {out_path} ({w}x{h})")


def encode_mp4_via_blender(frames_dir: Path, out_mp4: Path, fps: int = 30):
    frames = sorted(frames_dir.glob("frame_*.jpg"))
    if not frames:
        print(f"[VIDEO] No frames found in {frames_dir}")
        return
    im0 = Image.open(frames[0])
    w, h = im0.size
    out_mp4.parent.mkdir(parents=True, exist_ok=True)
    script = f"""
import bpy
from pathlib import Path
scene = bpy.context.scene
scene.render.resolution_x = {w}
scene.render.resolution_y = {h}
scene.render.resolution_percentage = 100
scene.render.fps = {fps}
scene.frame_start = 1
scene.frame_end = {len(frames)}
scene.render.image_settings.file_format = 'FFMPEG'
scene.render.ffmpeg.format = 'MPEG4'
scene.render.ffmpeg.codec = 'H264'
scene.render.ffmpeg.constant_rate_factor = 'MEDIUM'
scene.render.ffmpeg.ffmpeg_preset = 'GOOD'
scene.render.filepath = r'{str(out_mp4)}'
scene.sequence_editor_create()
seq = scene.sequence_editor.sequences.new_image(
    name='Walkthrough',
    filepath=r'{str(frames[0])}',
    channel=1,
    frame_start=1,
)
for p in sorted(Path(r'{str(frames_dir)}').glob('frame_*.jpg'))[1:]:
    seq.elements.append(p.name)
bpy.ops.render.render(animation=True)
"""
    tmp_py = frames_dir / "_encode_vse.py"
    tmp_py.write_text(script, encoding="utf-8")
    subprocess.run([str(BLENDER_EXE), "--background", "--python", str(tmp_py)], check=True)
    tmp_py.unlink(missing_ok=True)
    print(f"[VIDEO] Encoded {out_mp4} ({len(frames)} frames @ {fps}fps)")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--qa-dir", type=Path, required=True)
    ap.add_argument("--baseline-dir", type=Path, default=ROOT / "output" / "qa-final-windows")
    ap.add_argument("--out-dir", type=Path, default=ROOT / "docs" / "m2" / "evidence")
    ap.add_argument("--frames-dir", type=Path, default=None)
    args = ap.parse_args()

    renders = ROOT / "output" / "m2-evidence" / "blender-renders"
    # 1. Blender 4.5 Studio Asset Showcase Sheet
    blender_panels = [
        ("LOC01 · 1987 3-Story Hospital Facade", renders / "CTCC_ENV_HospitalFacade.png"),
        ("LOC01 · Brick Security Gatehouse", renders / "CTCC_ENV_Gatehouse.png"),
        ("LOC02 · Glazed Reception Partition", renders / "CTCC_ENV_ReceptionPartition.png"),
        ("LOC03 · Store Dismantled Bed 4", renders / "CTCC_PROP_DismantledBedFrame.png"),
        ("LOC04 · ER Resuscitation Stretcher", renders / "CTCC_PROP_EmergencyStretcher.png"),
        ("LOC04 · Patient ECG Monitor Cart", renders / "CTCC_PROP_PatientMonitorCart.png"),
        ("CHAR002 · Trần Ngọc Lan (Full Rig)", renders / "CTCC_CHR_Lan.png"),
        ("CHAR002 · Lan Facial Portrait (9 Shapes)", renders / "CTCC_CHR_Lan_Portrait.png"),
        ("CHAR005 · Gatekeeper (Scorched Coat + Keys)", renders / "CTCC_CHR_Gatekeeper.png"),
        ("CHAR005 · Gatekeeper Portrait", renders / "CTCC_CHR_Gatekeeper_Portrait.png"),
        ("CHAR007 · Bé An Apparition", renders / "CTCC_CHR_AnApparition.png"),
        ("LOC01 · Commuter Motorbike", renders / "CTCC_PROP_CommuterMotorbike.png"),
    ]
    make_grid(blender_panels, 4, 460, 460,
              "CA TRỰC CUỐI CÙNG — M2 BLENDER 4.5 LTS PRODUCTION SHOWCASE",
              "Real Cycles studio renders from editable .blend sources (ArtSource/Blender) with PBR maps & LODs/Rigs",
              args.out_dir / "m2-blender-showcase.png")

    # 2. M2 Vertical Slice 6-Panel In-Engine 1080p Gallery (LOC01-04 + Characters)
    qa = args.qa_dir
    gallery_panels = [
        ("LOC01 · Exterior Arrival & Monsoon Rain (R 2012)", qa / "loc01-exterior-R.png"),
        ("LOC02 · Reception & Trần Ngọc Lan (B 2005)", qa / "loc02-lan-B.png"),
        ("LOC03 · Room 101 Moonlight & Indirect GI (R 2012)", qa / "room101-R.png"),
        ("LOC03 · Store Room — Dismantled Bed 4 & E11 (R)", qa / "loc03-store-R.png"),
        ("LOC04 · Emergency Department Medical Bay (R)", qa / "loc04-emergency-R.png"),
        ("LOC04 · CH02 B06 Gatekeeper Cameo (Corridor R)", qa / "loc04-gatekeeper-R.png"),
    ]
    make_grid(gallery_panels, 3, 620, 349,
              "CA TRỰC CUỐI CÙNG — M2 HIGH-FIDELITY VERTICAL SLICE (UNITY 6.3 URP · 1080p MEDIUM)",
              "5 Additive Scenes (AnSinh_Bootstrap + LOC01–04) · Dual Baked LightProbes · Blender 4.5 Assets & Rigged Characters",
              args.out_dir / "m2-zones-gallery.png")

    # 3. BEFORE (M1-P) vs AFTER (M2) Side-by-Side Comparison Sheet
    base = args.baseline_dir
    compare_panels = [
        ("BEFORE (M1-P) · Room 101 (R 2012)", base / "room101-R.png"),
        ("AFTER (M2) · Room 101 (R 2012 · Dual GI + Refined Plaster)", qa / "room101-R.png"),
        ("BEFORE (M1-P) · Reception Lobby (R 2012)", base / "reception-R.png"),
        ("AFTER (M2) · Reception Partition, Barrier & Desk (R 2012)", qa / "reception-R.png"),
        ("BEFORE (M1-P) · Corridor (R 2012)", base / "corridor-R.png"),
        ("AFTER (M2) · Corridor (R 2012 · Dual GI + Occlusion)", qa / "corridor-R.png"),
        ("BEFORE (M1-P) · Room 101 Report Facade (B 2005)", base / "room101-B.png"),
        ("AFTER (M2) · Room 101 Report Facade (B 2005 · Dual GI)", qa / "room101-B.png"),
    ]
    make_grid(compare_panels, 2, 900, 506,
              "CA TRỰC CUỐI CÙNG — VISUAL COMPARISON: M1-P BASELINE vs M2 VERTICAL SLICE",
              "Direct 1920x1080 Windows x64 D3D12 captures on Intel Arc iGPU (Medium Preset)",
              args.out_dir / "m2-before-after-compare.png")

    # 4. Optional H.264 MP4 Gameplay Walkthrough Encoding via Blender 4.5 VSE
    if args.frames_dir and args.frames_dir.is_dir():
        encode_mp4_via_blender(args.frames_dir, args.out_dir / "m2-walkthrough.mp4", fps=30)


if __name__ == "__main__":
    main()
