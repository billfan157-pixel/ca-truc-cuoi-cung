"""CH01 Walkthrough Foley & Atmospheric Cue Synthesizer (48 kHz mono PCM 16-bit, <= -4 dBTP).

Pure NumPy + Python stdlib mastering matching tools/bake_ch01r_audio.py discipline.
Generates 7 original tactile cues in Prototype/Assets/CTCC/Art/Resources/CTCC/:
- CH01_PenWrite.wav: Seamless soft ballpoint-on-ledger writing loop for Lan at desk (B03; absent in B06)
- CH01_SignKnock.wav: Wind-driven tin demolition sign knock in B01
- CH01_KeyTurn.wav: Rusty padlock key insertion + bolt turn for GATE-CONG in B01
- CH01_LatchClick.wav: Quiet deliberate metal latch lift for DRAWER-NURSE quiet open in B04
- CH01_GateSlam.wav: Heavy iron gate exterior impact + rattle for B07 seam
- CH01_BagZip.wav: Canvas backpack cloth rustle + brass buckle click for Backpack open/close
- CH01_ClipSnap.wav: Spring metal clipboard snap for clipping AS-05B beside F101
"""

from __future__ import annotations

import wave
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Resources" / "CTCC"
RATE = 48000
MAX_PEAK_DBTP = -4.0
RNG = np.random.default_rng(20261123)


def t_arr(sec: float) -> np.ndarray:
    return np.arange(round(sec * RATE), dtype=np.float64) / RATE


def band_noise(sec: float, low: float, high: float) -> np.ndarray:
    n = round(sec * RATE)
    freqs = np.fft.rfftfreq(n, 1.0 / RATE)
    w = (freqs**2 / (freqs**2 + low**2)) / (1.0 + (freqs / high) ** 4)
    spec = np.fft.rfft(RNG.normal(size=n)) * w
    out = np.fft.irfft(spec, n=n)
    return out / max(1e-6, float(np.std(out)))


def fft_highpass(sig: np.ndarray, cutoff_hz: float, order: int = 2) -> np.ndarray:
    n = len(sig)
    freqs = np.fft.rfftfreq(n, 1.0 / RATE)
    w = (freqs / cutoff_hz) ** (2 * order)
    w = np.sqrt(w / (1.0 + w))
    return np.fft.irfft(np.fft.rfft(sig) * w, n=n)


def fft_convolve(a: np.ndarray, b: np.ndarray) -> np.ndarray:
    n = 1
    target = len(a) + len(b) - 1
    while n < target:
        n <<= 1
    out = np.fft.irfft(np.fft.rfft(a, n) * np.fft.rfft(b, n), n=n)
    return out[: len(a)]


def add_room_tail(sig: np.ndarray, decay: float = 8.0, wet: float = 0.22) -> np.ndarray:
    t = t_arr(0.55)
    ir = band_noise(0.55, 220, 3200) * np.exp(-t * decay) * (1.0 - np.exp(-t * 90.0))
    ir /= max(1e-6, float(np.linalg.norm(ir)))
    conv = fft_convolve(sig, ir)
    return sig * (1.0 - wet * 0.35) + conv * wet


def measure_true_peak_db(sig: np.ndarray) -> float:
    n = len(sig)
    spec = np.fft.rfft(sig)
    os_n = n * 4
    os_spec = np.zeros(os_n // 2 + 1, dtype=np.complex128)
    os_spec[: len(spec)] = spec * 4.0
    upsampled = np.fft.irfft(os_spec, n=os_n)
    pk = float(np.max(np.abs(upsampled)))
    return 20.0 * float(np.log10(max(pk, 1e-9)))


def write_master(path: Path, sig: np.ndarray, target_rms_db: float, seamless_loop: bool = False) -> None:
    sig = fft_highpass(sig - float(np.mean(sig)), 40.0, order=2)
    if seamless_loop:
        xf = int(0.12 * RATE)
        fade = np.linspace(0.0, 1.0, xf)
        sig[:xf] = sig[:xf] * fade + sig[-xf:] * (1.0 - fade)
        sig = sig[:-xf]
    else:
        fi = min(int(0.004 * RATE), len(sig) // 8)
        fo = min(int(0.025 * RATE), len(sig) // 4)
        if fi > 1:
            sig[:fi] *= np.linspace(0.0, 1.0, fi)
        if fo > 1:
            sig[-fo:] *= np.linspace(1.0, 0.0, fo)

    rms = float(np.sqrt(np.mean(sig * sig)))
    cur_rms_db = 20.0 * float(np.log10(max(rms, 1e-9)))
    sig = sig * (10.0 ** ((target_rms_db - cur_rms_db) / 20.0))

    tp_db = measure_true_peak_db(sig)
    if tp_db > MAX_PEAK_DBTP:
        sig *= 10.0 ** ((MAX_PEAK_DBTP - tp_db) / 20.0)
        tp_db = measure_true_peak_db(sig)

    sig = np.clip(sig, -0.98, 0.98)
    pcm = np.round(sig * 32767.0).astype("<i2")
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as wf:
        wf.setnchannels(1)
        wf.setsampwidth(2)
        wf.setframerate(RATE)
        wf.writeframes(pcm.tobytes())
    final_rms_db = 20.0 * float(np.log10(max(float(np.sqrt(np.mean((pcm / 32768.0) ** 2))), 1e-9)))
    print(f"Wrote {path.name}: {len(pcm)/RATE:.2f}s | RMS {final_rms_db:.2f} dBFS | TP {tp_db:.2f} dBTP")


def make_pen_write() -> np.ndarray:
    sec = 4.6
    t = t_arr(sec)
    scratch = band_noise(sec, 1400, 5200) * 0.55 + band_noise(sec, 420, 1400) * 0.25
    env = np.zeros_like(t)
    # Natural rhythmic handwriting bursts with pauses between ledger columns
    strokes = [
        (0.18, 0.14), (0.36, 0.11), (0.50, 0.18), (0.76, 0.12), (0.94, 0.15),
        (1.45, 0.16), (1.66, 0.10), (1.80, 0.22), (2.10, 0.13),
        (2.68, 0.18), (2.92, 0.12), (3.10, 0.16), (3.34, 0.14),
        (3.85, 0.15), (4.06, 0.18), (4.30, 0.10),
    ]
    for start, dur in strokes:
        mask = (t >= start) & (t < start + dur)
        u = (t[mask] - start) / max(dur, 1e-6)
        wiggle = 0.65 + 0.35 * np.sin(u * np.pi * (4.0 + 3.0 * (start % 1.0)))
        env[mask] += np.sin(u * np.pi) * wiggle
    # Tiny paper shift at 2.4s
    shift_mask = (t >= 2.35) & (t < 2.55)
    u = (t[shift_mask] - 2.35) / 0.20
    paper_shift = np.zeros_like(t)
    paper_shift[shift_mask] = band_noise(sec, 300, 2200)[shift_mask] * np.sin(u * np.pi) * 0.45
    return add_room_tail(scratch * env + paper_shift, decay=11.0, wet=0.14)


def make_sign_knock() -> np.ndarray:
    sec = 0.85
    t = t_arr(sec)
    # Sheet-metal tin rattle: two short damped modal hits from wind gust
    sig = np.zeros_like(t)
    for t0, amp in [(0.04, 1.0), (0.21, 0.48)]:
        dt = np.maximum(0.0, t - t0)
        active = t >= t0
        body = (
            np.sin(2 * np.pi * 192 * dt) * np.exp(-dt * 12.0) * 0.50
            + np.sin(2 * np.pi * 438 * dt) * np.exp(-dt * 16.0) * 0.38
            + np.sin(2 * np.pi * 912 * dt) * np.exp(-dt * 24.0) * 0.24
            + band_noise(sec, 500, 3400) * np.exp(-dt * 28.0) * 0.30
        )
        sig[active] += body[active] * amp
    return add_room_tail(sig, decay=6.5, wet=0.22)


def make_key_turn() -> np.ndarray:
    sec = 0.95
    t = t_arr(sec)
    sig = np.zeros_like(t)
    # 1) Key slides into keyhole (0.04s)
    m1 = (t >= 0.04) & (t < 0.22)
    u1 = (t[m1] - 0.04) / 0.18
    sig[m1] += (
        band_noise(sec, 1800, 7200)[m1] * np.sin(u1 * np.pi) * 0.42
        + np.sin(2 * np.pi * 2460 * (t[m1] - 0.04)) * np.exp(-(t[m1] - 0.04) * 32.0) * 0.26
    )
    # 2) Tumbler resistance + bolt snap (0.34s and 0.56s)
    for t0, f1, f2, amp in [(0.34, 1180, 2940, 0.72), (0.54, 620, 1740, 1.0)]:
        dt = np.maximum(0.0, t - t0)
        act = t >= t0
        click = (
            np.sin(2 * np.pi * f1 * dt) * np.exp(-dt * 34.0) * 0.46
            + np.sin(2 * np.pi * f2 * dt) * np.exp(-dt * 44.0) * 0.34
            + band_noise(sec, 900, 6000) * np.exp(-dt * 40.0) * 0.36
        )
        sig[act] += click[act] * amp
    return add_room_tail(sig, decay=9.0, wet=0.18)


def make_latch_click() -> np.ndarray:
    sec = 0.42
    t = t_arr(sec)
    sig = np.zeros_like(t)
    for t0, f, amp in [(0.03, 1850, 0.75), (0.14, 1320, 0.90)]:
        dt = np.maximum(0.0, t - t0)
        act = t >= t0
        hit = (
            np.sin(2 * np.pi * f * dt) * np.exp(-dt * 55.0) * 0.48
            + band_noise(sec, 1200, 5800) * np.exp(-dt * 65.0) * 0.35
        )
        sig[act] += hit[act] * amp
    return add_room_tail(sig, decay=13.0, wet=0.12)


def make_gate_slam() -> np.ndarray:
    sec = 1.45
    t = t_arr(sec)
    # Heavy hollow iron gate strike + iron bar resonance + secondary chain rattle
    dt = np.maximum(0.0, t - 0.03)
    act = t >= 0.03
    thud = (
        np.sin(2 * np.pi * 78 * dt) * np.exp(-dt * 8.5) * 0.78
        + np.sin(2 * np.pi * 154 * dt) * np.exp(-dt * 10.0) * 0.56
        + np.sin(2 * np.pi * 336 * dt) * np.exp(-dt * 12.5) * 0.38
        + np.sin(2 * np.pi * 712 * dt) * np.exp(-dt * 17.0) * 0.25
        + band_noise(sec, 140, 2600) * np.exp(-dt * 16.0) * 0.60
    )
    sig = np.zeros_like(t)
    sig[act] = thud[act]
    # Secondary metallic bounce/rattle at 0.26s & 0.44s
    for t0, amp in [(0.24, 0.42), (0.42, 0.22)]:
        dt2 = np.maximum(0.0, t - t0)
        act2 = t >= t0
        r = (
            np.sin(2 * np.pi * 425 * dt2) * np.exp(-dt2 * 22.0) * 0.36
            + band_noise(sec, 650, 4200) * np.exp(-dt2 * 25.0) * 0.34
        )
        sig[act2] += r[act2] * amp
    return add_room_tail(sig, decay=4.6, wet=0.30)


def make_bag_zip() -> np.ndarray:
    sec = 0.55
    t = t_arr(sec)
    # Soft heavy canvas rustle + subtle brass buckle/clasp sound
    cloth = band_noise(sec, 260, 2400) * np.sin(np.clip(t / 0.38, 0.0, 1.0) * np.pi) * 0.65
    dt = np.maximum(0.0, t - 0.20)
    act = t >= 0.20
    clasp = np.zeros_like(t)
    clasp[act] = (
        np.sin(2 * np.pi * 2180 * dt[act]) * np.exp(-dt[act] * 48.0) * 0.34
        + band_noise(sec, 1600, 6400)[act] * np.exp(-dt[act] * 55.0) * 0.22
    )
    return add_room_tail(cloth + clasp, decay=14.0, wet=0.10)


def make_clip_snap() -> np.ndarray:
    sec = 0.36
    t = t_arr(sec)
    dt = np.maximum(0.0, t - 0.03)
    act = t >= 0.03
    sig = np.zeros_like(t)
    sig[act] = (
        np.sin(2 * np.pi * 1640 * dt[act]) * np.exp(-dt[act] * 45.0) * 0.55
        + np.sin(2 * np.pi * 3420 * dt[act]) * np.exp(-dt[act] * 60.0) * 0.38
        + band_noise(sec, 900, 6800)[act] * np.exp(-dt[act] * 50.0) * 0.42
    )
    return add_room_tail(sig, decay=12.0, wet=0.14)


def main() -> None:
    write_master(OUT_DIR / "CH01_PenWrite.wav", make_pen_write(), target_rms_db=-26.0, seamless_loop=True)
    write_master(OUT_DIR / "CH01_SignKnock.wav", make_sign_knock(), target_rms_db=-18.0)
    write_master(OUT_DIR / "CH01_KeyTurn.wav", make_key_turn(), target_rms_db=-17.5)
    write_master(OUT_DIR / "CH01_LatchClick.wav", make_latch_click(), target_rms_db=-21.0)
    write_master(OUT_DIR / "CH01_GateSlam.wav", make_gate_slam(), target_rms_db=-13.5)
    write_master(OUT_DIR / "CH01_BagZip.wav", make_bag_zip(), target_rms_db=-22.5)
    write_master(OUT_DIR / "CH01_ClipSnap.wav", make_clip_snap(), target_rms_db=-18.5)


if __name__ == "__main__":
    main()
