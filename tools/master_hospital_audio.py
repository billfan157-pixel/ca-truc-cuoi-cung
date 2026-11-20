"""Render the CH01–02 sound pack and report its source and signal measurements."""

import argparse
import hashlib
import json
import math
from pathlib import Path
from tempfile import TemporaryDirectory

import numpy as np
import scipy
from scipy import signal
from scipy.io import wavfile

from bake_m2_audio import AUDIO_DIR, extract_clean_lines

RATE = 48000
RNG = np.random.default_rng(2012101)
LOOPS = ("Rain", "Roof", "Rumble", "Drips", "Fan", "Tube", "EmergencyHum")


def time(seconds):
    return np.arange(round(seconds * RATE)) / RATE


def texture(seconds, low, high):
    count = round(seconds * RATE)
    freq = np.fft.rfftfreq(count, 1 / RATE)
    weights = freq**2 / (freq**2 + low**2) / (1 + (freq / high)**4)
    spectrum = np.fft.rfft(RNG.normal(size=count)) * weights
    samples = np.fft.irfft(spectrum, n=count)
    return samples / np.std(samples)


def pulse(seconds, frequency, decay):
    t = time(seconds)
    return np.sin(2 * np.pi * frequency * t) * np.exp(-decay * t) * np.minimum(t / 0.003, 1)


def scatter(bed, event, onsets, gains):
    for onset, gain in zip(onsets, gains, strict=True):
        start = round(onset * RATE)
        indices = (start + np.arange(len(event))) % len(bed)
        bed[indices] += gain * event


def weather_and_rooms():
    length = 30
    t = time(length)
    phase = 2 * np.pi * t / length
    rain = texture(length, 235, 5300) * (0.64 + 0.18 * np.sin(phase) + 0.07 * np.cos(phase * 7))
    rain += texture(length, 4800, 10500) * 0.18
    roof = texture(length, 140, 2200) * (0.64 + 0.22 * np.cos(phase * 3))
    roof_hit = pulse(0.11, 537, 62) + 0.35 * pulse(0.11, 763, 58)
    scatter(roof, roof_hit, RNG.uniform(0, length, 94), RNG.uniform(0.17, 0.55, 94))
    rumble = texture(length, 34, 170) * (0.45 + 0.2 * np.sin(phase * 2) + 0.12 * np.cos(phase * 5))
    rumble += 0.13 * texture(length, 105, 360)
    drop_time = time(0.28)
    drips = texture(length, 140, 660) * 0.008
    for onset in (1.4, 3.6, 5.5, 8.9, 10.2, 13.4, 16.1, 20.8, 21.5, 24.4, 28.7):
        freq = RNG.uniform(470, 835)
        drop = np.sin(2 * np.pi * (freq * drop_time + 330 * drop_time**2)) * np.exp(-23 * drop_time)
        drop += texture(0.28, 1420, 6100) * np.exp(-49 * drop_time) * 0.17
        drop *= np.minimum(drop_time / 0.004, 1)
        scatter(drips, drop, (onset,), (RNG.uniform(0.45, 1.0),))
    fan = texture(length, 65, 510) * (0.64 + 0.26 * np.cos(phase * 148))
    fan += 0.34 * np.sin(2 * np.pi * 50 * t) + 0.16 * np.sin(2 * np.pi * 100 * t)
    tubes = texture(length, 115, 1900) * 0.18
    tubes += (np.sin(2 * np.pi * 100 * t) + 0.3 * np.sin(2 * np.pi * 200 * t)) * (0.8 + 0.13 * np.sin(phase * 4))
    hiss = texture(0.13, 1650, 6200) * np.sin(np.pi * time(0.13) / 0.13)**2
    scatter(tubes, hiss, (6.2, 18.4, 26.1), (0.75, 0.48, 0.58))
    emergency = 0.38 * np.sin(2 * np.pi * 50 * t) + 0.28 * np.sin(2 * np.pi * 100 * t)
    emergency += texture(length, 52, 408) * (0.36 + 0.1 * np.sin(phase * 2))
    return dict(zip(LOOPS, (rain, roof, rumble, drips, fan, tubes, emergency), strict=True))


def foley():
    keys = np.zeros_like(time(1.1))
    for onset, frequency, level in ((0.015, 2760, 0.9), (0.092, 3180, 0.65), (0.18, 2290, 1), (0.34, 3070, 0.65), (0.49, 3710, 0.45)):
        clink = pulse(0.36, frequency, 21) + 0.5 * pulse(0.36, frequency * 1.57, 29)
        clink += 0.21 * pulse(0.36, frequency * 2.13, 41)
        scrape = texture(0.36, 800, 5700) * np.exp(-49 * time(0.36)) * 0.31
        scatter(keys, clink + scrape, (onset,), (level,))
    t = time(2.7)
    travel = np.sin(np.pi * t / 2.7)**1.45
    trolley = texture(2.7, 53, 320) * travel
    trolley += texture(2.7, 410, 2600) * travel * (0.13 + 0.24 * np.sin(2 * np.pi * (3.6 * t + 0.65 * t**2))**8)
    squeak = np.sin(2 * np.pi * (715 * t + 18 * np.sin(2 * np.pi * 1.2 * t)))
    trolley += 0.19 * squeak * travel * (0.5 + 0.5 * np.sin(2 * np.pi * 2.7 * t))**10
    t = time(0.49)
    slipper = 0.76 * texture(0.49, 130, 950) * np.exp(-((t - 0.095) / 0.024)**2)
    slipper += texture(0.49, 480, 3150) * (0.61 * np.exp(-((t - 0.16) / 0.034)**2) + 0.3 * np.exp(-((t - 0.29) / 0.031)**2))
    return {"KeyJingle": keys, "TrolleyRoll": trolley, "Slipper": slipper}


def lan_motif():
    musical = np.zeros_like(time(10.5))
    t = time(4.0)
    attack = 1 - np.exp(-t * 115)
    for onset, freq, weight in ((0.25, 293.66, 0.88), (1.45, 349.23, 0.78), (2.65, 440.00, 0.91),
                                 (4.05, 392.00, 0.70), (5.20, 329.63, 0.82)):
        note = np.sin(2 * np.pi * freq * t) * np.exp(-1.35 * t)
        note += 0.22 * np.sin(2 * np.pi * freq * 1.003 * t) * np.exp(-1.61 * t)
        note += 0.31 * np.sin(2 * np.pi * freq * 2.002 * t) * np.exp(-2.7 * t)
        note += 0.10 * np.sin(2 * np.pi * freq * 3.017 * t) * np.exp(-4.5 * t)
        note += 0.055 * texture(4.0, 450, 2500) * np.exp(-t * 27)
        scatter(musical, note * attack, (onset,), (weight,))
    tail_time = time(1.45)
    room = texture(1.45, 260, 3600) * np.exp(-tail_time * 5.8) * (1 - np.exp(-tail_time * 45))
    room /= np.linalg.norm(room)
    return musical + 0.28 * signal.fftconvolve(musical, room)[:len(musical)]


def read_take(path):
    rate, samples = wavfile.read(path)
    if np.issubdtype(samples.dtype, np.signedinteger):
        samples = samples.astype(float) / -float(np.iinfo(samples.dtype).min)
    elif not np.issubdtype(samples.dtype, np.floating):
        raise ValueError(f"Expected signed PCM or floating point WAV: {path}")
    if samples.ndim > 1:
        samples = np.mean(samples, axis=1)
    if not len(samples) or not np.all(np.isfinite(samples)) or np.max(np.abs(samples)) < 0.002:
        raise ValueError(f"Empty or invalid recording: {path}")
    divisor = math.gcd(rate, RATE)
    return signal.resample_poly(samples, RATE // divisor, rate // divisor)


def voice_fx(samples, role):
    pa = role in ("pa", "pa_cut")
    low, high = (220, 4200) if pa else (85, 10500)
    sos = signal.butter(3, [low, high], btype="bandpass", fs=RATE, output="sos")
    samples = signal.sosfilt(sos, samples)
    peaks = np.flatnonzero(np.abs(samples) > np.max(np.abs(samples)) * 0.025)
    if not len(peaks):
        raise ValueError("Recording has no speech after filtering")
    samples = samples[max(0, peaks[0] - RATE // 25):peaks[-1] + RATE // 15]
    samples *= 0.21 / max(0.008, np.sqrt(np.mean(samples**2)))
    samples = np.tanh(samples * (1.25 if pa else 1.08))
    if pa:
        bounce = round(0.046 * RATE)
        samples[bounce:] += 0.10 * samples[:-bounce]
        samples += texture(len(samples) / RATE, 620, 3400) * 0.003
        if role == "pa_cut":
            click_time = time(0.125)
            click = 0.24 * texture(0.125, 780, 5000) * np.exp(-click_time * 63)
            samples = np.concatenate((samples[:-RATE // 25], click))
    return samples


def db(value):
    return round(20 * np.log10(max(float(value), 1e-9)), 2)


def write_master(path, samples, rms, loop=False):
    samples = samples - np.mean(samples)
    samples *= (10**(rms / 20)) / np.sqrt(np.mean(samples**2))
    ceiling = 10**(-4.0 / 20)
    # Keep transients below the ceiling with a soft knee, then check an oversampled peak.
    samples = ceiling * np.tanh(samples / ceiling)
    if not loop:
        fade = min(round(0.012 * RATE), len(samples) // 5)
        envelope = np.linspace(0, 1, fade)**2
        samples[:fade] *= envelope
        samples[-fade:] *= envelope[::-1]
    true_peak = float(np.max(np.abs(signal.resample_poly(samples, 4, 1))))
    if true_peak > ceiling:
        samples *= ceiling / true_peak
    path.parent.mkdir(parents=True, exist_ok=True)
    wavfile.write(path, RATE, np.rint(samples * 32767).astype(np.int16))
    return {"seconds": round(len(samples) / RATE, 3), "rmsDbfs": db(np.sqrt(np.mean(samples**2))),
            "peakDbfs": db(np.max(np.abs(samples))), "truePeakDbtpApprox": db(min(ceiling, true_peak)),
            "loopSeamDeltaDbfs": db(abs(samples[-1] - samples[0])) if loop else None,
            "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}


def render_pack(staging, args):
    assets = {}
    selected_lines = set(args.lines) if getattr(args, "lines", None) else None
    if not selected_lines:
        for name, samples in weather_and_rooms().items():
            target = -29 if name in ("Rain", "Roof") else -33
            assets[name] = write_master(staging / f"{name}.wav", samples, target, loop=True)
        for name, samples in foley().items():
            assets[name] = write_master(staging / f"{name}.wav", samples, -24)
        assets["MUS_LanMotif"] = write_master(staging / "MUS_LanMotif.wav", lan_motif(), -25)
    cast_record = None
    if args.raw_voice:
        cast_record = json.loads((args.raw_voice / "takes.json").read_text(encoding="utf-8"))
        for line_id, role, _ in extract_clean_lines():
            if selected_lines and line_id not in selected_lines:
                continue
            is_pa = role in ("pa", "pa_cut")
            if cast_record["takes"][line_id]["role"] != role:
                raise ValueError(f"Voice take has the wrong role: {line_id}")
            take = args.raw_voice / f"{line_id}.wav"
            path = staging / "VO" / f"{line_id}.wav"
            assets[line_id] = write_master(path, voice_fx(read_take(take), role), -19 if is_pa else -21)
            assets[line_id]["rawSha256"] = hashlib.sha256(take.read_bytes()).hexdigest()

    return {"sampleRateHz": RATE, "numpy": np.__version__, "scipy": scipy.__version__,
            "voiceTakes": cast_record, "assets": assets}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=AUDIO_DIR)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--raw-voice", type=Path, help="All dialogue WAVs and takes.json from bake_character_takes.py")
    parser.add_argument("--lines", nargs="+", help="Master only the specified dialogue IDs")
    args = parser.parse_args()

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.report.parent.mkdir(parents=True, exist_ok=True)
    with TemporaryDirectory(prefix="ctcc-audio-", dir=args.output.parent) as folder:
        staging = Path(folder)
        report = render_pack(staging, args)
        for rendered in sorted(staging.rglob("*.wav")):
            destination = args.output / rendered.relative_to(staging)
            destination.parent.mkdir(parents=True, exist_ok=True)
            rendered.replace(destination)
    args.report.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
