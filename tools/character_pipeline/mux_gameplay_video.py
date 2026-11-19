"""Mux Unity 30fps rendered character showcase frames with frame-synced Vietnamese VO, MUS_LanMotif & SFX into MP4 with audio."""

import json
import subprocess
import sys
import wave
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
FFMPEG = ROOT / "output" / "m2a" / "diagnostic-tools" / "imageio_ffmpeg" / "binaries" / "ffmpeg-win-x86_64-v9.0.2.exe"


def load_wav_mono_float(path: Path, target_sr=48000) -> np.ndarray:
    with wave.open(str(path), "rb") as w:
        sr = w.getframerate()
        ch = w.getnchannels()
        sw = w.getsampwidth()
        raw = w.readframes(w.getnframes())
    if sw == 2:
        data = np.frombuffer(raw, dtype=np.int16).astype(np.float32) / 32768.0
    elif sw == 4:
        data = np.frombuffer(raw, dtype=np.int32).astype(np.float32) / 2147483648.0
    else:
        data = (np.frombuffer(raw, dtype=np.uint8).astype(np.float32) - 128.0) / 128.0
    if ch > 1:
        data = data.reshape(-1, ch).mean(axis=1)
    if sr != target_sr and len(data) > 1:
        n_out = int(round(len(data) * target_sr / float(sr)))
        x_old = np.linspace(0.0, 1.0, len(data), dtype=np.float32)
        x_new = np.linspace(0.0, 1.0, n_out, dtype=np.float32)
        data = np.interp(x_new, x_old, data).astype(np.float32)
    return data


def mux_video(frames_dir: Path, out_mp4: Path):
    import re
    cues_path = frames_dir / "audio_cues.json"
    raw = cues_path.read_text(encoding="utf-8")
    # Sanitize locale-formatted floats (e.g., 1,833 -> 1.833) or unquoted F2 format tokens cleanly
    raw = re.sub(r'"time":(\d+),(\d+)', r'"time":\1.\2', raw)
    raw = re.sub(r'"gain":F2', r'"gain":null', raw)
    spec = json.loads(raw)
    fps = float(spec.get("fps", 30))
    n_frames = int(spec.get("frames", len(list(frames_dir.glob("frame_*.jpg")))))
    sr = 48000
    total_samples = int(round((n_frames / fps) * sr))
    mix = np.zeros(total_samples, dtype=np.float32)

    # Subtle ambient hospital rain bed (-24 dB)
    rain_path = ROOT / "Prototype/Assets/CTCC/Art/Audio/Rain.wav"
    if rain_path.exists():
        rain = load_wav_mono_float(rain_path, sr)
        if len(rain) > 0:
            reps = (total_samples // len(rain)) + 2
            mix += np.tile(rain, reps)[:total_samples] * 0.08

    for cue in spec.get("cues", []):
        wav_path = ROOT / cue["asset"]
        if not wav_path.exists():
            print(f"[WARN] Missing cue audio: {wav_path}")
            continue
        default_gain = 0.34 if "MUS_" in wav_path.name else (0.85 if "KeyJingle" in wav_path.name else 1.0)
        gain = float(cue["gain"]) if cue.get("gain") is not None else default_gain
        t_sec = float(cue["frame"]) / fps if "frame" in cue else float(cue["time"])
        clip = load_wav_mono_float(wav_path, sr) * gain
        start_idx = int(round(t_sec * sr))
        end_idx = min(total_samples, start_idx + len(clip))
        if end_idx > start_idx:
            mix[start_idx:end_idx] += clip[: end_idx - start_idx]
        print(f"[AUDIO] Placed {wav_path.name} @ {t_sec:.2f}s (gain={gain:.2f})")

    # Gentle soft limiter so peak never clips
    peak = float(np.max(np.abs(mix))) if len(mix) else 0.0
    if peak > 0.92:
        mix = mix * (0.92 / peak)
    pcm16 = np.clip(mix * 32767.0, -32768, 32767).astype(np.int16)
    mix_wav = frames_dir / "mixed_audio.wav"
    with wave.open(str(mix_wav), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(sr)
        w.writeframes(pcm16.tobytes())

    out_mp4.parent.mkdir(parents=True, exist_ok=True)
    cmd = [
        str(FFMPEG), "-y",
        "-framerate", str(int(fps)),
        "-i", str(frames_dir / "frame_%04d.jpg"),
        "-i", str(mix_wav),
        "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "20", "-preset", "medium",
        "-c:a", "aac", "-b:a", "192k", "-ar", str(sr),
        "-shortest",
        str(out_mp4),
    ]
    subprocess.run(cmd, check=True)
    print(f"[VIDEO+AUDIO] Muxed {out_mp4} ({n_frames} frames @ {fps:.0f}fps + 48kHz AAC audio)")


if __name__ == "__main__":
    mux_video(Path(sys.argv[1]), Path(sys.argv[2]))
