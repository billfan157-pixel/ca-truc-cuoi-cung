"""Author spatially layered, loopable ambience and short prop cues from synthesis."""

import sys
import wave
from pathlib import Path

import numpy as np

RATE = 32000
RNG = np.random.default_rng(704)
ROOT = Path(__file__).resolve().parents[1]
DEST = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "output" / "material-staging" / "audio"


def noise(seconds, low, high):
    count = round(seconds * RATE)
    time = np.fft.rfftfreq(count, 1 / RATE)
    band = np.minimum(1, (time/max(low,1))**2) / (1 + (time/high)**4)
    phase = RNG.uniform(0, 2*np.pi, len(time))
    signal = np.fft.irfft(np.exp(1j*phase)*band, n=count)
    return signal / max(.001,np.std(signal))


def time(seconds):
    return np.linspace(0, seconds, round(RATE*seconds), endpoint=False)


def save(name, signal):
    with wave.open(str(DEST / (name+".wav")), "wb") as w:
        w.setparams((1,2,RATE,len(signal),"NONE","not compressed"))
        w.writeframes(np.asarray(np.clip(signal, -.95, .95) * 32767, dtype="<i2").tobytes())


def drop(pitch=490):
    t = time(.27)
    amp = np.exp(-24*t) * np.clip(t/.006,0,1)
    return amp * (np.sin(2*np.pi*(pitch*t+24*np.sqrt(t))) * .27 + noise(.27,600,5200)*.041)


if __name__ == "__main__":
    DEST.mkdir(parents=True,exist_ok=True)
    t = time(20)
    swell = .78 + .10 * np.sin(2*np.pi*t/20) + .06*np.sin(2*np.pi*3*t/20)
    save("Rain", (noise(20,180,4800)*.078 + noise(20,880,10500)*.014) * swell)
    save("Roof", (noise(20,80,1900)*.026 + noise(20,350,3500)*.015) * (.83+.17*np.sin(2*np.pi*t/5)))
    save("Rumble", noise(20,31,230)*.037 + noise(20,81,700)*.012)
    drip = np.zeros(len(t))
    for second in (1.18,3.14,7.03,8.68,13.05,13.55,17.18):
        pulse = drop(470+RNG.uniform(-120,160))
        index = int(second*RATE)
        drip[index:index+len(pulse)] += pulse * RNG.uniform(.40,1)
    save("Drips",drip)
    phase = 2*np.pi*t
    save("Fan", noise(20,32,420)*.046*(.62+.36*np.sin(phase*5.4)**2) +
         .016*np.sin(phase*50)+.004*np.sin(phase*125))
    save("Tube", .013*np.sin(phase*100)+.004*np.sin(phase*200)+noise(20,90,1700)*.010)
    t=time(.35)
    save("Paper",noise(.35,830,6300)*.12*np.sin(np.pi*t/.35)**2)
    save("Stamp",np.sin(2*np.pi*t*106)*.29*np.exp(-t*28)*np.clip(t/.004,0,1) +
         noise(.35,180,2100)*.19*np.exp(-t*43)*np.clip(t/.004,0,1))
    t=time(.26)
    save("Slipper",noise(.26,280,2300)*.15*np.sin(np.pi*t/.26)**3)
    save("Step",noise(.26,60,400)*.19*np.sin(np.pi*t/.26)**3)
    t=time(.85)
    save("Switch",noise(.85,43,395)*.16*np.sin(np.pi*t/.85)**2 + np.sin(2*np.pi*t*60)*.08*np.sin(np.pi*t/.85)**2)
