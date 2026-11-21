"""Legacy procedural M2 sound sources and canonical slice dialogue lookup.

Render the current pack with master_hospital_audio.py. This entry point can
regenerate the legacy SFX and motif into an explicitly chosen staging folder.
"""

import argparse
import json
import re
import wave
from pathlib import Path

import numpy as np

RATE = 32000
RNG = np.random.default_rng(2026)
ROOT = Path(__file__).resolve().parents[1]
AUDIO_DIR = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Audio"
DIALOGUE_JSON = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Resources" / "CTCC" / "Dialogue.json"


def noise(seconds, low, high):
    count = round(seconds * RATE)
    freq = np.fft.rfftfreq(count, 1.0 / RATE)
    band = np.minimum(1.0, (freq / max(low, 1.0)) ** 2) / (1.0 + (freq / high) ** 4)
    phase = RNG.uniform(0, 2 * np.pi, len(freq))
    signal = np.fft.irfft(np.exp(1j * phase) * band, n=count)
    return signal / max(0.001, np.std(signal))


def time_arr(seconds):
    return np.linspace(0, seconds, round(RATE * seconds), endpoint=False)


def save_wav(path, signal, rate=RATE):
    s = np.asarray(signal, dtype=np.float64)
    peak = np.max(np.abs(s)) if len(s) else 0.0
    if peak > 0.88:
        s = s * (0.88 / peak)
    fade = min(int(rate * 0.008), len(s) // 8)
    if fade > 2:
        env = np.linspace(0.0, 1.0, fade)
        s[:fade] *= env
        s[-fade:] *= env[::-1]
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as w:
        w.setparams((1, 2, rate, len(s), "NONE", "not compressed"))
        w.writeframes(np.asarray(np.clip(s, -0.94, 0.94) * 32767, dtype="<i2").tobytes())


def bandpass_fft(sig, low_hz, high_hz, sr=RATE):
    spec = np.fft.rfft(sig)
    freqs = np.fft.rfftfreq(len(sig), 1.0 / sr)
    hp = (freqs / max(low_hz, 1.0)) ** 2 / (1.0 + (freqs / max(low_hz, 1.0)) ** 2)
    lp = 1.0 / (1.0 + (freqs / max(high_hz, 10.0)) ** 4)
    return np.fft.irfft(spec * hp * lp, n=len(sig))


def simple_reverb(sig, delays_ms=(38, 67, 113, 179), decays=(0.24, 0.18, 0.12, 0.08), wet=0.28):
    pad = int(RATE * 0.45)
    out = np.zeros(len(sig) + pad, dtype=np.float64)
    out[:len(sig)] = sig
    rev = np.zeros_like(out)
    for d_ms, dec in zip(delays_ms, decays):
        d = int(RATE * d_ms / 1000.0)
        if d < len(out):
            rev[d:d + len(sig)] += sig * dec
    rev = bandpass_fft(rev, 180, 4200)
    return out * (1.0 - wet * 0.4) + rev * wet


def build_lan_motif():
    """Original 5-note felt-piano / celesta motif (Core 05 MUS-LAN: full 5 notes for CH01 B03).
    Notes: D4 (293.66), F4 (349.23), A4 (440.00), G4 (392.00), E4 (329.63) + soft resonant D4 resolution."""
    dur = 8.4
    out = np.zeros(round(dur * RATE), dtype=np.float64)
    notes = [
        (0.25, 293.66, 2.2, 0.36),  # D4
        (1.45, 349.23, 2.2, 0.34),  # F4
        (2.65, 440.00, 2.6, 0.40),  # A4
        (4.05, 392.00, 2.4, 0.34),  # G4
        (5.20, 329.63, 3.0, 0.38),  # E4 (5th note — full CH01 motif!)
    ]
    for onset, freq, length, amp in notes:
        t = time_arr(length)
        # Warm felt upright piano partials with slight natural string detuning
        tone = (
            1.00 * np.sin(2 * np.pi * freq * t) * np.exp(-t * 1.15)
            + 0.42 * np.sin(2 * np.pi * (freq * 2.0012) * t) * np.exp(-t * 2.0)
            + 0.16 * np.sin(2 * np.pi * (freq * 3.0038) * t) * np.exp(-t * 3.2)
            + 0.06 * np.sin(2 * np.pi * (freq * 4.008) * t) * np.exp(-t * 4.5)
            + 0.18 * np.sin(2 * np.pi * (freq * 0.5005) * t) * np.exp(-t * 0.9)  # warm sub-octave resonance
        )
        hammer = noise(length, 260, 1400) * 0.032 * np.exp(-t * 45.0)
        attack = np.clip(t / 0.018, 0.0, 1.0) ** 0.7
        sig = (tone + hammer) * attack * amp
        idx = int(onset * RATE)
        end = min(len(out), idx + len(sig))
        out[idx:end] += sig[:end - idx]
    # Soft lush warm hall decay
    out = simple_reverb(out, delays_ms=(62, 118, 194, 310), decays=(0.30, 0.22, 0.16, 0.10), wet=0.45)
    out = bandpass_fft(out, 65, 5800) * 0.65
    save_wav(AUDIO_DIR / "MUS_LanMotif.wav", out)


def build_m2_sfx():
    """Generate new canonical M2 spatial horror & world SFX clips."""
    # 1. KeyJingle (SFX-KEY — strictly CH02+ Gatekeeper ONLY!)
    tk = time_arr(0.85)
    keys = np.zeros_like(tk)
    for onset, base_f in ((0.02, 3420), (0.09, 4180), (0.16, 2890), (0.28, 3760), (0.41, 4520)):
        idx = int(onset * RATE)
        lt = time_arr(0.32)
        clink = (
            0.22 * np.sin(2 * np.pi * base_f * lt) * np.exp(-lt * 22.0)
            + 0.15 * np.sin(2 * np.pi * (base_f * 1.41) * lt) * np.exp(-lt * 28.0)
            + 0.10 * np.sin(2 * np.pi * (base_f * 2.13) * lt) * np.exp(-lt * 34.0)
            + noise(0.32, 2200, 9600) * 0.08 * np.exp(-lt * 40.0)
        ) * np.clip(lt / 0.002, 0, 1)
        end = min(len(keys), idx + len(clink))
        keys[idx:end] += clink[:end - idx]
    save_wav(AUDIO_DIR / "KeyJingle.wav", simple_reverb(keys, delays_ms=(45, 92, 165), decays=(0.25, 0.18, 0.11), wet=0.35) * 0.72)

    # 2. TrolleyRoll (HE01 — wheeled stretcher passing frosted emergency glass in CH01 B04!)
    tr = time_arr(2.40)
    env_tr = np.sin(np.pi * tr / 2.40) ** 1.2
    rumble = noise(2.40, 55, 380) * 0.18
    rattle = noise(2.40, 1100, 5200) * 0.075 * (0.45 + 0.55 * np.sin(2 * np.pi * tr * 6.5) ** 2)
    squeak = np.sin(2 * np.pi * (920 * tr + 14 * np.sin(2 * np.pi * tr * 2.8))) * 0.032 * (0.5 + 0.5 * np.sin(2 * np.pi * tr * 2.8))
    save_wav(AUDIO_DIR / "TrolleyRoll.wav", simple_reverb((rumble + rattle + squeak) * env_tr, wet=0.30))

    # 3. PhoneBuzz (CH01 B01 Bảy's SMS arrival buzz + faint 2012 feature-phone two-tone ping)
    tp = time_arr(1.35)
    buzz_env = ((tp > 0.05) & (tp < 0.34)).astype(float) + ((tp > 0.46) & (tp < 0.75)).astype(float)
    buzz = (0.16 * np.sin(2 * np.pi * 135 * tp) + 0.08 * np.sin(2 * np.pi * 270 * tp) + noise(1.35, 180, 900) * 0.05) * buzz_env
    ping_t = np.maximum(0.0, tp - 0.82)
    ping = (0.16 * np.sin(2 * np.pi * 1318 * ping_t) * np.exp(-ping_t * 14) * (tp >= 0.82)
            + 0.14 * np.sin(2 * np.pi * 1760 * np.maximum(0, tp - 0.96)) * np.exp(-np.maximum(0, tp - 0.96) * 12) * (tp >= 0.96))
    save_wav(AUDIO_DIR / "PhoneBuzz.wav", buzz + ping)

    # 4. PAStatic (corridor speaker click & brief carrier hum)
    tpa = time_arr(0.45)
    pa_click = noise(0.45, 900, 6200) * 0.22 * np.exp(-tpa * 45) + noise(0.45, 380, 3200) * 0.04 * np.sin(np.pi * tpa / 0.45)
    save_wav(AUDIO_DIR / "PAStatic.wav", pa_click)

    # 5. BreakerCut (HE05 power drop during B->R collapse)
    tbc = time_arr(1.25)
    clunk = (noise(1.25, 120, 2400) * 0.28 * np.exp(-tbc * 35) + np.sin(2 * np.pi * 78 * tbc) * 0.25 * np.exp(-tbc * 18))
    pf = np.linspace(100, 28, len(tbc))
    hum_die = np.sin(np.cumsum(pf) * 2 * np.pi / RATE) * 0.16 * np.exp(-tbc * 2.2)
    save_wav(AUDIO_DIR / "BreakerCut.wav", clunk + hum_die)

    # 6. EmergencyHum (20s LOC04 crimson lightbox transformer hum + deep ER ventilation room tone)
    te = time_arr(20.0)
    ph = 2 * np.pi * te
    er_loop = (0.018 * np.sin(ph * 50) + 0.012 * np.sin(ph * 100) + 0.004 * np.sin(ph * 150)
               + noise(20.0, 48, 340) * 0.026 * (0.85 + 0.15 * np.sin(ph / 10.0)))
    save_wav(AUDIO_DIR / "EmergencyHum.wav", er_loop)

    # 7. DistantThunder (LOC01 monsoon atmospheric swell)
    tth = time_arr(5.2)
    env_th = np.clip(tth / 0.35, 0, 1) * np.exp(-np.maximum(0, tth - 0.5) * 0.72) * (0.75 + 0.25 * np.sin(2 * np.pi * tth * 1.4))
    save_wav(AUDIO_DIR / "DistantThunder.wav", (noise(5.2, 24, 180) * 0.36 + noise(5.2, 80, 460) * 0.10) * env_th)


def extract_clean_lines():
    """Load canonical CH01 & CH02 spoken lines from Dialogue.json with strict LOCK-V22-02 guard."""
    raw = json.loads(DIALOGUE_JSON.read_text(encoding="utf-8"))
    entries = {e["id"]: e for e in raw["entries"]}
    def quote_n(entry_id, n=1):
        txt = entries[entry_id]["copy"]
        matches = re.findall(r"“([^”]+)”", txt)
        if matches and n <= len(matches):
            return matches[n - 1].strip()
        return txt.strip()

    # Exact mapping: (clip_id, role, vietnamese_text)
    lines = [
        ("CH01-D01", "minh", quote_n("CH01-D01")),
        ("CH01-D02", "minh", quote_n("CH01-D02")),
        ("CH01-D03", "lan", quote_n("CH01-D03")),
        ("CH01-D03b", "minh", quote_n("CH01-D03b")),
        ("CH01-D03c", "lan", quote_n("CH01-D03c")),
        ("CH01-D04", "minh", quote_n("CH01-D04")),
        ("CH01-D04c", "lan", quote_n("CH01-D04c")),
        ("CH01-D05", "lan", quote_n("CH01-D05")),
        ("CH01-D05b", "minh", quote_n("CH01-D05b")),
        ("CH01-D05c", "lan", quote_n("CH01-D05c")),
        ("CH01-D06", "minh", quote_n("CH01-D06")),
        ("CH01-D07", "lan", quote_n("CH01-D07")),
        ("CH01-D07b", "minh", quote_n("CH01-D07b")),
        ("CH01-D08", "lan", quote_n("CH01-D08")),
        ("CH01-D09", "lan", quote_n("CH01-D09")),
        ("CH01-D10", "minh", quote_n("CH01-D10", 1)),
        ("CH01-D11", "minh", quote_n("CH01-D10", 2)),
        ("CH01-D12", "minh", quote_n("CH01-D10", 3)),
        ("CH01-D13", "minh", quote_n("CH01-D13", 1)),
        ("CH01-D14", "minh", quote_n("CH01-D13", 2)),
        ("CH01-D16", "lan", quote_n("CH01-D16")),
        ("CH01-D19", "minh", quote_n("CH01-D19")),
        ("CH01-PA01", "pa", quote_n("CH01-PA01")),
        ("CH01-PA02", "pa", quote_n("CH01-PA02")),
        ("CH01-PA03", "pa", quote_n("CH01-PA03")),
        ("CH01-PA04", "pa_cut", quote_n("CH01-PA04")),
        ("CH02-D01", "minh", quote_n("CH02-D01")),
        ("CH02-D02", "minh", quote_n("CH02-D02")),
        ("CH02-D03", "an", quote_n("CH02-D03")),
        ("CH02-D04", "minh", quote_n("CH02-D04")),
        ("CH02-D05", "gatekeeper", quote_n("CH02-D05")),
        ("CH03-D01", "minh", quote_n("CH03-D01")),
        ("CH03-D02", "minh", quote_n("CH03-D02")),
        ("CH03-D03", "ba_lanh", quote_n("CH03-D03")),
        ("CH03-D04", "minh", quote_n("CH03-D04")),
        ("CH03-D05", "gatekeeper", quote_n("CH03-D05")),
    ]
    if {"CH01-D16b", "CH04-D06"}.intersection(line_id for line_id, _, _ in lines):
        raise RuntimeError("LOCK-V22-02: CH04 reveal lines cannot enter this VO pack")
    return lines


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    destination = parser.parse_args().output.resolve()
    if destination == AUDIO_DIR.resolve():
        parser.error("Use a staging folder for legacy SFX. The game uses master_hospital_audio.py.")
    AUDIO_DIR = destination
    build_lan_motif()
    build_m2_sfx()
    print(f"Wrote legacy procedural SFX and motif to {AUDIO_DIR}")
