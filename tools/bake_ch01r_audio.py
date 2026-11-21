"""CH01-R Tactile Foley & Ambient Dread SFX Synthesizer (48 kHz mono PCM 16-bit, <= -4 dBTP).

Pure NumPy + Python stdlib mastering (zero third-party sample dependencies, zero SciPy requirement).
Deterministic RNG seeding and 4x FFT oversampled true-peak ceiling <= -4 dBTP matching
tools/master_hospital_audio.py discipline.

Generates:
- DrawerSlide.wav: Enameled steel bedside/desk drawer metal rail slide + damped wooden stop
- PaperFlip.wav: Crisp aged hospital paper turn-over rustle for PhysicalInspector front/back flip
- ClockTick.wav: Dry resonant single quartz/mechanical tick from the stopped Reception wall clock
- DistantSteps.wav: Soft, heavily muffled distant hospital footsteps (non-hostile, no key jingle!)
"""

from __future__ import annotations

import hashlib
import json
import wave
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
AUDIO_DIR = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Audio"
RATE = 48000
MAX_PEAK_DBTP = -4.0
RNG = np.random.default_rng(20261121)


def t_arr(sec: float) -> np.ndarray:
    return np.arange(round(sec * RATE), dtype=np.float64) / RATE


def band_noise(sec: float, low: float, high: float) -> np.ndarray:
    n = round(sec * RATE)
    freqs = np.fft.rfftfreq(n, 1.0 / RATE)
    w = (freqs**2 / (freqs**2 + low**2)) / (1.0 + (freqs / high) ** 4)
    spec = np.fft.rfft(RNG.normal(size=n)) * w
    out = np.fft.irfft(spec, n=n)
    return out / max(1e-6, float(np.std(out)))


def fft_lowpass(sig: np.ndarray, cutoff_hz: float, order: int = 3) -> np.ndarray:
    n = len(sig)
    freqs = np.fft.rfftfreq(n, 1.0 / RATE)
    w = 1.0 / np.sqrt(1.0 + (freqs / cutoff_hz) ** (2 * order))
    return np.fft.irfft(np.fft.rfft(sig) * w, n=n)


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


def add_room_tail(sig: np.ndarray, decay: float = 7.5, wet: float = 0.24) -> np.ndarray:
    t = t_arr(0.65)
    ir = band_noise(0.65, 220, 3400) * np.exp(-t * decay) * (1.0 - np.exp(-t * 80.0))
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


def write_master(path: Path, sig: np.ndarray, target_rms_db: float) -> dict:
    sig = fft_highpass(sig - float(np.mean(sig)), 35.0, order=2)
    # Soft 5 ms fade-in and 25 ms fade-out so one-shot clips never click
    fi = min(int(0.005 * RATE), len(sig) // 8)
    fo = min(int(0.025 * RATE), len(sig) // 4)
    if fi > 1:
        sig[:fi] *= np.linspace(0.0, 1.0, fi)
    if fo > 1:
        sig[-fo:] *= np.linspace(1.0, 0.0, fo)

    rms = float(np.sqrt(np.mean(sig * sig)))
    cur_rms_db = 20.0 * float(np.log10(max(rms, 1e-9)))
    gain = 10.0 ** ((target_rms_db - cur_rms_db) / 20.0)
    sig = sig * gain

    tp_db = measure_true_peak_db(sig)
    if tp_db > MAX_PEAK_DBTP:
        sig *= 10.0 ** ((MAX_PEAK_DBTP - tp_db) / 20.0)
        tp_db = measure_true_peak_db(sig)

    # Gentle soft safety clip just in case
    sig = np.clip(sig, -0.98, 0.98)
    pcm = np.round(sig * 32767.0).astype("<i2")
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as wf:
        wf.setnchannels(1)
        wf.setsampwidth(2)
        wf.setframerate(RATE)
        wf.writeframes(pcm.tobytes())

    final_rms = float(np.sqrt(np.mean((pcm / 32768.0) ** 2)))
    final_rms_db = 20.0 * float(np.log10(max(final_rms, 1e-9)))
    return {
        "file": path.name,
        "sample_rate_hz": RATE,
        "channels": 1,
        "duration_sec": round(len(pcm) / RATE, 3),
        "rms_dbfs": round(final_rms_db, 2),
        "true_peak_dbtp": round(tp_db, 2),
        "sha256": hashlib.sha256(path.read_bytes()).hexdigest()[:16],
    }


def make_drawer_slide() -> np.ndarray:
    dur = 0.68
    t = t_arr(dur)
    glide_env = np.clip(t / 0.05, 0.0, 1.0) * np.exp(-np.maximum(0.0, t - 0.36) * 11.0)
    rail_friction = band_noise(dur, 420, 3200) * 0.42 * glide_env
    steel_ring = (
        0.16 * np.sin(2 * np.pi * (860 * t + 12 * np.sin(2 * np.pi * 11 * t)))
        + 0.09 * np.sin(2 * np.pi * 1420 * t)
    ) * glide_env * (0.45 + 0.55 * np.sin(2 * np.pi * 9 * t) ** 2)
    stop_t = np.maximum(0.0, t - 0.48)
    stop_hit = (
        0.65 * np.sin(2 * np.pi * 115 * stop_t) * np.exp(-stop_t * 34.0)
        + 0.35 * band_noise(dur, 90, 620) * np.exp(-stop_t * 42.0)
    ) * (t >= 0.48)
    return add_room_tail(rail_friction + steel_ring + stop_hit, decay=9.0, wet=0.20)


def make_paper_flip() -> np.ndarray:
    dur = 0.38
    t = t_arr(dur)
    env = (np.sin(np.pi * np.clip(t / 0.34, 0.0, 1.0)) ** 1.35) * (0.65 + 0.35 * np.sin(2 * np.pi * 7.5 * t) ** 2)
    crisp = band_noise(dur, 950, 7200) * 0.55 * env
    body = band_noise(dur, 280, 1400) * 0.25 * env
    snap_t = np.maximum(0.0, t - 0.14)
    snap = band_noise(dur, 1600, 8800) * 0.45 * np.exp(-snap_t * 38.0) * (t >= 0.14)
    return add_room_tail(crisp + body + snap, decay=11.0, wet=0.14)


def make_clock_tick() -> np.ndarray:
    dur = 0.42
    t = t_arr(dur)
    imp = (
        0.78 * np.sin(2 * np.pi * 2420 * t) * np.exp(-t * 140.0)
        + 0.46 * np.sin(2 * np.pi * 1180 * t) * np.exp(-t * 95.0)
        + 0.28 * np.sin(2 * np.pi * 410 * t) * np.exp(-t * 42.0)
        + 0.40 * band_noise(dur, 1200, 6400) * np.exp(-t * 170.0)
    ) * np.minimum(t / 0.0012, 1.0)
    return add_room_tail(imp, decay=6.5, wet=0.32)


def make_distant_steps() -> np.ndarray:
    dur = 2.45
    t = t_arr(dur)
    out = np.zeros_like(t)
    for onset, gain, pitch in ((0.18, 0.85, 108.0), (0.94, 0.72, 102.0), (1.74, 0.52, 98.0)):
        lt = np.maximum(0.0, t - onset)
        active = t >= onset
        heel = (
            0.70 * np.sin(2 * np.pi * pitch * lt) * np.exp(-lt * 26.0)
            + 0.45 * band_noise(dur, 75, 420) * np.exp(-lt * 32.0)
        ) * active * np.minimum(lt / 0.008, 1.0)
        sole = band_noise(dur, 160, 780) * 0.26 * np.exp(-np.maximum(0.0, lt - 0.055) * 35.0) * (lt >= 0.055)
        out += (heel + sole) * gain
    out = fft_lowpass(out, 980.0, order=3)
    return add_room_tail(out, decay=5.2, wet=0.38)


def make_chair_scrape() -> np.ndarray:
    """Short muted wooden/steel hospital chair leg dragging across terrazzo outside the ward doorway."""
    dur = 0.88
    t = t_arr(dur)
    env = np.clip(t / 0.06, 0.0, 1.0) * np.exp(-np.maximum(0.0, t - 0.42) * 7.5)
    # Stick-slip resonant wood/metal leg shudder on tile grout
    shudder = 0.55 + 0.45 * np.sin(2 * np.pi * 16.5 * t + 1.4 * np.sin(2 * np.pi * 3.2 * t))
    scrape = (
        band_noise(dur, 280, 1650) * 0.48
        + 0.24 * np.sin(2 * np.pi * (312 * t + 6.5 * np.sin(2 * np.pi * 14 * t)))
        + 0.14 * np.sin(2 * np.pi * 645 * t)
    ) * env * shudder
    settle_t = np.maximum(0.0, t - 0.62)
    settle = (0.45 * np.sin(2 * np.pi * 96 * settle_t) + 0.25 * band_noise(dur, 70, 380)) * np.exp(-settle_t * 32.0) * (t >= 0.62)
    sig = fft_lowpass(scrape + settle, 1850.0, order=2)
    return add_room_tail(sig, decay=5.8, wet=0.34)


def make_partition_knock() -> np.ndarray:
    """Two hollow, heavily muffled thumps from behind the sealed Bed-4 Affirmed partition wall."""
    dur = 1.45
    t = t_arr(dur)
    out = np.zeros_like(t)
    for onset, gain, freq in ((0.14, 0.92, 86.0), (0.64, 0.76, 82.0)):
        lt = np.maximum(0.0, t - onset)
        active = t >= onset
        thud = (
            0.80 * np.sin(2 * np.pi * freq * lt) * np.exp(-lt * 19.0)
            + 0.42 * np.sin(2 * np.pi * (freq * 1.92) * lt) * np.exp(-lt * 29.0)
            + 0.35 * band_noise(dur, 60, 420) * np.exp(-lt * 34.0)
        ) * active * np.minimum(lt / 0.006, 1.0)
        out += thud * gain
    out = fft_lowpass(out, 620.0, order=3)
    return add_room_tail(out, decay=6.0, wet=0.30)


def main() -> None:
    report = {}
    clips = (
        ("DrawerSlide", make_drawer_slide(), -22.0),
        ("PaperFlip", make_paper_flip(), -23.5),
        ("ClockTick", make_clock_tick(), -24.5),
        ("DistantSteps", make_distant_steps(), -27.5),
        ("ChairScrape", make_chair_scrape(), -25.5),
        ("PartitionKnock", make_partition_knock(), -23.0),
    )
    for name, sig, target_rms in clips:
        report[name] = write_master(AUDIO_DIR / f"{name}.wav", sig, target_rms)
    out_meta = ROOT / "docs" / "ch01r" / "audio-additions.json"
    out_meta.parent.mkdir(parents=True, exist_ok=True)
    out_meta.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"CH01-R audio mastered cleanly -> {AUDIO_DIR} (manifest: {out_meta})")


if __name__ == "__main__":
    main()
