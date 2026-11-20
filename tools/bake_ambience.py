"""M0-R Rich Spatial Ambience & Physical Interaction Foley Synthesizer.

All audio clips are 100% original procedural synthesis (clean project ownership, zero external copyrighted audio).
Legacy renders go to a staging directory. Use master_hospital_audio.py for the current mix.
Generates:
- Layered environmental loops: Rain, Roof (corrugated awning), Rumble (distant monsoon thunder/wind),
  Drips (water dripping into basin/wet terrazzo), Fan (ceiling fan blade throb + motor hum),
  Tube (50Hz/100Hz Vietnamese mains fluorescent ballast hum + occasional starter buzz),
  CorridorWind (eerie low draft down hallway in R)
- Material-varied footsteps: Step (dry terrazzo), StepWet (damp puddle splash near window), StepCorridor (harder tiled click)
- Physical interactions: DoorCreak, DoorLatch, CurtainSlide, CabinetRoll, Paper, Stamp, Slipper,
  Switch (R<->B reality shift), SwitchToB (fluorescent power surge), SwitchToR (abrupt power cut + breath)
"""

import sys
import wave
from pathlib import Path

import numpy as np

RATE = 32000
RNG = np.random.default_rng(704)
ROOT = Path(__file__).resolve().parents[1]
DEST = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "output" / "audio-tools" / "legacy-ambience"


def noise(seconds, low, high):
    count = round(seconds * RATE)
    freq = np.fft.rfftfreq(count, 1.0 / RATE)
    band = np.minimum(1.0, (freq / max(low, 1.0)) ** 2) / (1.0 + (freq / high) ** 4)
    phase = RNG.uniform(0, 2 * np.pi, len(freq))
    signal = np.fft.irfft(np.exp(1j * phase) * band, n=count)
    return signal / max(0.001, np.std(signal))


def time_arr(seconds):
    return np.linspace(0, seconds, round(RATE * seconds), endpoint=False)


def save(name, signal):
    # Ensure click-free start/end fade on short clips
    fade = min(int(RATE * 0.008), len(signal) // 8)
    s = np.array(signal, dtype=np.float64)
    if fade > 2:
        env = np.linspace(0.0, 1.0, fade)
        s[:fade] *= env
        s[-fade:] *= env[::-1]
    with wave.open(str(DEST / (name + ".wav")), "wb") as w:
        w.setparams((1, 2, RATE, len(s), "NONE", "not compressed"))
        w.writeframes(np.asarray(np.clip(s, -0.94, 0.94) * 32767, dtype="<i2").tobytes())


def water_drop(pitch=520):
    t = time_arr(0.28)
    amp = np.exp(-22 * t) * np.clip(t / 0.005, 0, 1)
    # Classic rising bubble pitch chirp
    chirp = np.sin(2 * np.pi * (pitch * t + 38 * np.sqrt(t))) * 0.28
    splash = noise(0.28, 900, 6400) * 0.045 * np.exp(-32 * t)
    return amp * (chirp + splash)


if __name__ == "__main__":
    DEST.mkdir(parents=True, exist_ok=True)

    # --- 20s Seamless Environmental Loops ---
    t = time_arr(20.0)
    phase = 2 * np.pi * t
    swell = 0.78 + 0.11 * np.sin(phase / 20.0) + 0.06 * np.sin(3 * phase / 20.0)
    save("Rain", (noise(20, 190, 5200) * 0.082 + noise(20, 1200, 11000) * 0.018) * swell)
    save("Roof", (noise(20, 90, 2100) * 0.032 + noise(20, 420, 4200) * 0.018) * (0.82 + 0.18 * np.sin(phase / 5.0)))
    # Deep low monsoon thunder rumble + structural draft
    thunder_env = 0.42 + 0.58 * np.exp(-((t - 8.5) / 2.2) ** 2) + 0.35 * np.exp(-((t - 16.2) / 1.6) ** 2)
    save("Rumble", (noise(20, 28, 190) * 0.042 + noise(20, 75, 580) * 0.012) * thunder_env)

    # Water dripping irregularly into wet terrazzo / enamel basin
    drip = np.zeros(len(t))
    for sec in (0.92, 2.84, 5.41, 7.12, 8.95, 11.78, 13.15, 13.68, 16.44, 18.20):
        pulse = water_drop(460 + RNG.uniform(-110, 190))
        idx = int(sec * RATE)
        if idx + len(pulse) < len(drip):
            drip[idx : idx + len(pulse)] += pulse * RNG.uniform(0.45, 1.0)
    save("Drips", drip)

    # Vietnamese 3-blade ceiling fan air throb + 50Hz AC motor hum (Vietnam mains = 50Hz!)
    save("Fan", noise(20, 32, 390) * 0.048 * (0.60 + 0.38 * np.sin(phase * 5.2) ** 2)
         + 0.015 * np.sin(phase * 50) + 0.005 * np.sin(phase * 100))

    # Fluorescent T8 magnetic ballast 100Hz buzz + intermittent faint starter crackle
    crackle = np.zeros(len(t))
    for sec in (3.2, 9.8, 14.5):
        ci = int(sec * RATE)
        cl = int(0.12 * RATE)
        crackle[ci : ci + cl] = noise(0.12, 1600, 8200) * 0.025 * np.sin(np.linspace(0, np.pi, cl))
    save("Tube", 0.015 * np.sin(phase * 100) + 0.005 * np.sin(phase * 200) + 0.002 * np.sin(phase * 300)
         + noise(20, 110, 1800) * 0.009 + crackle)

    # --- Short Prop & Physical Interaction SFX ---
    # Paper / document inspect rustle
    tp = time_arr(0.36)
    save("Paper", noise(0.36, 920, 6800) * 0.13 * np.sin(np.pi * tp / 0.36) ** 1.8)

    # Heavy Vietnamese rubber ink stamp thud on F101
    ts = time_arr(0.35)
    save("Stamp", np.sin(2 * np.pi * ts * 104) * 0.32 * np.exp(-ts * 26) * np.clip(ts / 0.004, 0, 1)
         + noise(0.35, 180, 2200) * 0.21 * np.exp(-ts * 40) * np.clip(ts / 0.004, 0, 1))

    # Child's honeycomb rubber slipper shuffle on terrazzo (SFX-AN)
    ta = time_arr(0.32)
    env_a = np.sin(np.pi * ta / 0.32) ** 2.4
    save("Slipper", (noise(0.32, 320, 2600) * 0.16 + noise(0.32, 1200, 4200) * 0.05) * env_a)

    # Footsteps: Dry terrazzo, Wet puddle splash, Harder corridor step
    tf = time_arr(0.24)
    heel = np.exp(-tf * 28) * np.clip(tf / 0.005, 0, 1)
    save("Step", (noise(0.24, 65, 520) * 0.20 + np.sin(2 * np.pi * tf * 95) * 0.12) * heel)
    save("StepWet", (noise(0.24, 65, 520) * 0.14 + noise(0.24, 1400, 7500) * 0.22 * np.exp(-tf * 14)) * np.clip(tf / 0.004, 0, 1))
    save("StepCorridor", (noise(0.24, 120, 1600) * 0.21 + np.sin(2 * np.pi * tf * 145) * 0.10) * heel)

    # Heavy wooden hospital ward door hinge creak + metallic latch click
    td = time_arr(0.75)
    creak_freq = 285 + 95 * np.sin(2 * np.pi * td * 2.4) + 35 * np.sin(2 * np.pi * td * 7.8)
    creak_phase = np.cumsum(creak_freq) * (2 * np.pi / RATE)
    creak = (np.sin(creak_phase) * 0.11 + np.sin(creak_phase * 2.02) * 0.06 + np.sin(creak_phase * 3.1) * 0.03)
    creak *= np.sin(np.pi * td / 0.75) ** 1.4
    wood_thud = noise(0.75, 80, 750) * 0.08 * np.exp(-td * 12)
    save("DoorCreak", creak + wood_thud)

    tl = time_arr(0.22)
    latch = (noise(0.22, 1200, 7800) * 0.22 * np.exp(-tl * 55)
             + np.sin(2 * np.pi * tl * 1850) * 0.08 * np.exp(-tl * 48)) * np.clip(tl / 0.002, 0, 1)
    save("DoorLatch", latch)

    # Privacy curtain rings sliding along galvanized ceiling track + fabric swoosh
    tc = time_arr(0.82)
    rings = noise(0.82, 2400, 9200) * 0.09 * (0.5 + 0.5 * np.sin(2 * np.pi * tc * 14.0) ** 2)
    cloth_swish = noise(0.82, 350, 2400) * 0.14
    save("CurtainSlide", (rings + cloth_swish) * np.sin(np.pi * tc / 0.82) ** 1.3)

    # Heavy rolling steel medical cabinet scraping/rumbling aside on terrazzo (E12 reveal!)
    tr = time_arr(1.35)
    caster_rumble = noise(1.35, 45, 360) * 0.24
    metal_rattle = noise(1.35, 950, 4400) * 0.10 * (0.5 + 0.5 * np.sin(2 * np.pi * tr * 9.5) ** 2)
    squeak = np.sin(2 * np.pi * (420 * tr + 15 * np.sin(2 * np.pi * tr * 3))) * 0.045
    save("CabinetRoll", (caster_rumble + metal_rattle + squeak) * np.sin(np.pi * tr / 1.35) ** 0.9)

    # Reality shift (Switch default + dedicated SwitchToB fluorescent surge & SwitchToR power cut)
    tw = time_arr(0.90)
    env_w = np.sin(np.pi * tw / 0.90) ** 1.6
    save("Switch", noise(0.90, 42, 420) * 0.18 * env_w + np.sin(2 * np.pi * tw * 58) * 0.09 * env_w)

    # R -> B: Relay click + rising 100Hz fluorescent tube ignition hum
    tb = time_arr(0.95)
    click = noise(0.95, 1200, 6000) * 0.25 * np.exp(-tb * 70)
    surge = (0.09 * np.sin(2 * np.pi * tb * 100) + 0.04 * np.sin(2 * np.pi * tb * 200)) * np.clip(tb / 0.35, 0, 1) * np.exp(-np.maximum(0, tb - 0.55) * 3.5)
    save("SwitchToB", click + surge + noise(0.95, 60, 480) * 0.11 * np.sin(np.pi * tb / 0.95) ** 1.5)

    # B -> R: Breaker clunk + descending sub-bass power-off sigh
    tbr = time_arr(0.95)
    drop_f = np.linspace(110, 36, len(tbr))
    drop_ph = np.cumsum(drop_f) * (2 * np.pi / RATE)
    save("SwitchToR", noise(0.95, 900, 4500) * 0.22 * np.exp(-tbr * 65)
         + np.sin(drop_ph) * 0.14 * np.exp(-tbr * 2.8)
         + noise(0.95, 45, 340) * 0.15 * np.sin(np.pi * tbr / 0.95) ** 1.6)

    print(f"Wrote {len(list(DEST.glob('*.wav')))} M0-R spatial ambience & foley WAV clips to {DEST}")
