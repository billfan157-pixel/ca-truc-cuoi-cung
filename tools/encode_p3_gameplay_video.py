"""Encode recorded 12fps JPG gameplay frames into an H.264 MP4 via Blender 4.5 VSE.

Usage:
  blender --background --factory-startup --python tools/encode_p3_gameplay_video.py -- \
    --frames-dir output/ch01-rebirth-iii-p3/final-record/ch01r-full \
    --out-mp4 output/ch01-rebirth-iii-p3/ch01-p3-b06-b08-gameplay.mp4 \
    --fps 12 --start-index 0 --end-index -1
"""
import argparse
import os
import sys
import bpy


def parse_args():
    argv = sys.argv
    if "--" in argv:
        argv = argv[argv.index("--") + 1 :]
    else:
        argv = []
    parser = argparse.ArgumentParser()
    parser.add_argument("--frames-dir", required=True)
    parser.add_argument("--out-mp4", required=True)
    parser.add_argument("--fps", type=int, default=12)
    parser.add_argument("--start-index", type=int, default=0)
    parser.add_argument("--end-index", type=int, default=-1)
    parser.add_argument("--width", type=int, default=1920)
    parser.add_argument("--height", type=int, default=1080)
    return parser.parse_args(argv)


def main():
    args = parse_args()
    frames_dir = os.path.abspath(args.frames_dir)
    out_mp4 = os.path.abspath(args.out_mp4)
    os.makedirs(os.path.dirname(out_mp4), exist_ok=True)

    files = sorted(
        f for f in os.listdir(frames_dir)
        if f.lower().endswith((".jpg", ".jpeg", ".png"))
    )
    if not files:
        raise RuntimeError(f"No frames found in {frames_dir}")

    start_idx = max(0, args.start_index)
    end_idx = len(files) if args.end_index < 0 else min(len(files), args.end_index)
    selected = files[start_idx:end_idx]
    if not selected:
        raise RuntimeError(f"Empty slice [{start_idx}:{end_idx}] out of {len(files)} frames")

    scene = bpy.context.scene
    scene.sequence_editor_create()
    se = scene.sequence_editor

    # Blender 4.5 VSE: new_image + append elements for image sequence strip
    strip = se.sequences.new_image(
        name="Gameplay",
        filepath=os.path.join(frames_dir, selected[0]),
        channel=1,
        frame_start=1,
    )
    for fname in selected[1:]:
        strip.elements.append(fname)

    scene.render.resolution_x = args.width
    scene.render.resolution_y = args.height
    scene.render.resolution_percentage = 100
    scene.render.fps = args.fps
    scene.render.fps_base = 1.0
    scene.frame_start = 1
    scene.frame_end = len(selected)

    scene.render.image_settings.file_format = "FFMPEG"
    scene.render.ffmpeg.format = "MPEG4"
    scene.render.ffmpeg.codec = "H264"
    scene.render.ffmpeg.constant_rate_factor = "MEDIUM"
    scene.render.ffmpeg.ffmpeg_preset = "GOOD"
    scene.render.ffmpeg.audio_codec = "NONE"
    scene.render.filepath = out_mp4

    # Color management: raw/standard so captured sRGB pixels are preserved faithfully
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    scene.view_settings.exposure = 0.0
    scene.view_settings.gamma = 1.0

    bpy.ops.render.render(animation=True)
    size_bytes = os.path.getsize(out_mp4) if os.path.exists(out_mp4) else 0
    print(f"[VIDEO] Encoded {len(selected)} frames @ {args.fps}fps -> {out_mp4} ({size_bytes} bytes)")


if __name__ == "__main__":
    main()
