"""CH01 Rebirth III Phase 1 B01 Foley & Phone Cue Synthesizer (48 kHz mono PCM 16-bit, <= -4 dBTP).

Pure NumPy + Python stdlib synthesis matching tools/bake_ch01_walkthrough_audio.py discipline
(100% original procedural audio, zero third-party sample dependencies).
Generates 5 cues in Prototype/Assets/CTCC/Art/Resources/CTCC/:
- CH01_MailboxLatch.wav: Metallic rusted iron latch click when opening the gate mailbox
- CH01_MailboxLidCreak.wav: Short rusted sheet-metal hinge creak as mailbox lid swings outward
- CH01_MailboxPackagePickup.wav: Crisp brass keys jingle + damp kraft envelope rustle (atomic pickup)
- CH01_PhoneSmsChime.wav: Restrained 2012 Vietnamese bar-phone two-tone SMS alert + brief buzz
- CH01_PhoneRingNoAnswer.wav: Two Vietnamese mobile ringback tones ending in silent no-answer disconnect
"""

from __future__ import annotations

import wave
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Resources" / "CTCC"
RATE = 48000
MAX_PEAK_DBTP = -4.0
RNG = np.random.default_rng(20261126)


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


def add_exterior_tail(sig: np.ndarray, decay: float = 9.0, wet: float = 0.16) -> np.ndarray:
    t = t_arr(0.45)
    ir = band_noise(0.45, 240, 3400) * np.exp(-t * decay) * (1.0 - np.exp(-t * 110.0))
    ir /= max(1e-6, float(np.linalg.norm(ir)))
    conv = fft_convolve(sig, ir)
    return sig * (1.0 - wet * 0.3) + conv * wet


def measure_true_peak_db(sig: np.ndarray) -> float:
    n = len(sig)
    spec = np.fft.rfft(sig)
    os_n = n * 4
    os_spec = np.zeros(os_n // 2 + 1, dtype=np.complex128)
    os_spec[: len(spec)] = spec * 4.0
    upsampled = np.fft.irfft(os_spec, n=os_n)
    pk = float(np.max(np.abs(upsampled)))
    return 20.0 * float(np.log10(max(pk, 1e-9)))


def write_master(path: Path, sig: np.ndarray, target_rms_db: float) -> None:
    sig = fft_highpass(sig - float(np.mean(sig)), 45.0, order=2)
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


def make_mailbox_latch() -> np.ndarray:
    sec = 0.48
    t = t_arr(sec)
    sig = np.zeros_like(t)
    # Thumb pressing rusted sheet-metal clasp + spring pop
    for t0, amp, f1, f2 in [(0.025, 0.85, 680.0, 1840.0), (0.115, 1.0, 490.0, 1420.0)]:
        dt = np.maximum(0.0, t - t0)
        m = t >= t0
        hit = (
            np.sin(2 * np.pi * f1 * dt) * np.exp(-dt * 42.0) * 0.45
            + np.sin(2 * np.pi * f2 * dt) * np.exp(-dt * 58.0) * 0.35
            + np.sin(2 * np.pi * 245.0 * dt) * np.exp(-dt * 24.0) * 0.28
            + band_noise(sec, 900, 5400) * np.exp(-dt * 65.0) * 0.42
        )
        sig[m] += hit[m] * amp
    return add_exterior_tail(sig, decay=10.5, wet=0.14)


def make_mailbox_lid_creak() -> np.ndarray:
    sec = 0.72
    t = t_arr(sec)
    # Stick-slip rusted iron hinge squeak sweeping down slightly as lid swings open
    env = np.sin(np.clip((t - 0.04) / 0.56, 0.0, 1.0) * np.pi) ** 1.3
    env *= (t >= 0.04) & (t <= 0.62)
    grain = 0.62 + 0.38 * np.sin(2 * np.pi * 26.0 * t + 0.9 * np.sin(2 * np.pi * 7.5 * t))
    f_base = 520.0 - 115.0 * (t / sec) + 18.0 * np.sin(2 * np.pi * 11.0 * t)
    phase = 2 * np.pi * np.cumsum(f_base) / RATE
    squeak = (
        0.48 * np.sin(phase)
        + 0.32 * np.sin(2.03 * phase)
        + 0.18 * np.sin(3.11 * phase)
        + 0.22 * band_noise(sec, 700, 3600)
    )
    # Soft sheet-metal stop tap at end of travel
    dt_end = np.maximum(0.0, t - 0.57)
    stop_tap = (t >= 0.57) * (
        np.sin(2 * np.pi * 210.0 * dt_end) * np.exp(-dt_end * 26.0) * 0.42
        + band_noise(sec, 350, 1800) * np.exp(-dt_end * 35.0) * 0.26
    )
    return add_exterior_tail(squeak * env * grain * 0.72 + stop_tap, decay=9.5, wet=0.16)


def make_mailbox_package_pickup() -> np.ndarray:
    sec = 0.88
    t = t_arr(sec)
    # 1) Damp kraft envelope slide out of iron box (0.04 - 0.52s)
    p_env = np.sin(np.clip((t - 0.03) / 0.48, 0.0, 1.0) * np.pi) * ((t >= 0.03) & (t <= 0.51))
    paper = band_noise(sec, 420, 3100) * p_env * 0.55 + band_noise(sec, 1200, 5800) * p_env * 0.25
    # 2) Two metal keys clinking crisply into hand (0.14s, 0.28s, 0.44s)
    keys = np.zeros_like(t)
    for t0, amp, shift in [(0.14, 0.72, 1.0), (0.28, 1.0, 1.12), (0.44, 0.58, 0.94)]:
        dt = np.maximum(0.0, t - t0)
        m = t >= t0
        clink = (
            np.sin(2 * np.pi * (2480.0 * shift) * dt) * np.exp(-dt * 28.0) * 0.42
            + np.sin(2 * np.pi * (3860.0 * shift) * dt) * np.exp(-dt * 34.0) * 0.36
            + np.sin(2 * np.pi * (5410.0 * shift) * dt) * np.exp(-dt * 44.0) * 0.24
            + band_noise(sec, 2200, 7200) * np.exp(-dt * 55.0) * 0.18
        )
        keys[m] += clink[m] * amp
    return add_exterior_tail(paper + keys, decay=11.0, wet=0.14)


def make_phone_sms_chime() -> np.ndarray:
    sec = 0.76
    t = t_arr(sec)
    sig = np.zeros_like(t)
    # Subtle 2012 pocket vibration hum + muted two-note mobile alert (never harsh)
    vib_env = ((t >= 0.02) & (t < 0.24)) * np.sin(np.clip((t - 0.02) / 0.22, 0.0, 1.0) * np.pi)
    sig += (0.24 * np.sin(2 * np.pi * 138.0 * t) + 0.14 * np.sin(2 * np.pi * 276.0 * t)) * vib_env
    for t0, freq in [(0.18, 1174.66), (0.34, 1567.98)]:
        dt = np.maximum(0.0, t - t0)
        m = t >= t0
        tone = (
            np.sin(2 * np.pi * freq * dt) * 0.65
            + np.sin(2 * np.pi * freq * 2.0 * dt) * 0.15
        ) * np.exp(-dt * 11.5) * (1.0 - np.exp(-dt * 180.0))
        sig[m] += tone[m] * 0.55
    return sig


def make_phone_ring_no_answer() -> np.ndarray:
    sec = 3.15
    t = t_arr(sec)
    sig = np.zeros_like(t)
    # Authentic Vietnamese 425 Hz carrier ringback tone (2 bursts: 0.12-0.92s and 1.55-2.35s) + soft line drop click
    for start, dur in [(0.12, 0.78), (1.55, 0.78)]:
        m = (t >= start) & (t < start + dur)
        dt = t[m] - start
        ramp = np.minimum(1.0, dt / 0.015) * np.minimum(1.0, (dur - dt) / 0.025)
        sig[m] += (0.58 * np.sin(2 * np.pi * 425.0 * t[m]) + 0.08 * np.sin(2 * np.pi * 850.0 * t[m])) * ramp
    # Faint cellular line hiss throughout
    sig += band_noise(sec, 350, 2900) * 0.032
    # Soft disconnect beep at 2.68s
    m_end = (t >= 2.68) & (t < 2.86)
    dt_e = t[m_end] - 2.68
    sig[m_end] += 0.32 * np.sin(2 * np.pi * 360.0 * t[m_end]) * np.minimum(1.0, dt_e / 0.01) * np.exp(-dt_e * 8.0)
    return sig


def make_clock_0317_eeg_hum() -> np.ndarray:
    """Core v3.6.2 FS22: Ultra-subtle Layer B 03:17 wall-clock EEG monitor undertone (<= -24 dBFS, restrained)."""
    sec = 2.40
    t = t_arr(sec)
    # Soft 50 Hz mains + harmonic medical CRT hum + two distant slow ECG/EEG pulse blips (no harsh beeps)
    env = np.minimum(1.0, t / 0.18) * np.minimum(1.0, (sec - t) / 0.35)
    hum = (
        0.40 * np.sin(2 * np.pi * 100.0 * t)
        + 0.22 * np.sin(2 * np.pi * 200.0 * t)
        + 0.14 * np.sin(2 * np.pi * 432.0 * t)
        + 0.10 * band_noise(sec, 180.0, 1400.0)
    ) * env * 0.45
    blips = np.zeros_like(t)
    for t0 in (0.48, 1.44):
        dt = np.maximum(0.0, t - t0)
        m = (t >= t0) & (t < t0 + 0.18)
        blips[m] += (
            0.52 * np.sin(2 * np.pi * 880.0 * dt[m])
            + 0.14 * np.sin(2 * np.pi * 1760.0 * dt[m])
        ) * np.exp(-dt[m] * 26.0) * (1.0 - np.exp(-dt[m] * 240.0))
    return hum + blips * 0.42


def main() -> None:
    write_master(OUT_DIR / "CH01_MailboxLatch.wav", make_mailbox_latch(), target_rms_db=-20.0)
    write_master(OUT_DIR / "CH01_MailboxLidCreak.wav", make_mailbox_lid_creak(), target_rms_db=-21.0)
    write_master(OUT_DIR / "CH01_MailboxPackagePickup.wav", make_mailbox_package_pickup(), target_rms_db=-19.5)
    write_master(OUT_DIR / "CH01_PhoneSmsChime.wav", make_phone_sms_chime(), target_rms_db=-22.0)
    write_master(OUT_DIR / "CH01_PhoneRingNoAnswer.wav", make_phone_ring_no_answer(), target_rms_db=-21.5)
    write_master(OUT_DIR / "CH01_Clock0317_EEGHum.wav", make_clock_0317_eeg_hum(), target_rms_db=-24.5)


if __name__ == "__main__":
    main()
