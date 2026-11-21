"""Author a readable calendar leaf and short, original prop audio for Chapter 1."""
import math
import random
import wave
from array import array
from pathlib import Path

from PIL import Image, ImageDraw
import bake_surfaces as surfaces

DIRECTORY = Path(__file__).resolve().parents[1] / "Prototype/Assets/CTCC/Art/Resources/CTCC"
RATE = 24000


def calendar():
    leaf = Image.new("RGB", (512, 680), "#ded4b7")
    page = ImageDraw.Draw(leaf)
    page.rectangle((16, 16, 496, 664), outline="#49574b", width=5)
    page.rectangle((16, 16, 496, 150), fill="#78342a")
    page.text((256, 84), "THÁNG 11 · 2005", font=surfaces.typeface(44, True), fill="#f7eed8", anchor="mm")
    page.text((256, 283), "17", font=surfaces.typeface(164, True), fill="#263830", anchor="mm")
    page.text((256, 406), "THỨ NĂM  ·  CA ĐÊM", font=surfaces.typeface(29, True), fill="#524938", anchor="mm")
    for y in (477, 531, 585, 637):
        page.line((47, y, 466, y), fill="#a0987d", width=2)
    page.text((65, 487), "Rửa cốc sau khi chốt ca.", font=surfaces.typeface(23), fill="#395775")
    page.text((60, 542), "Treo lại chìa tủ ở móc cũ.", font=surfaces.typeface(23), fill="#395775")
    page.line([(63, 512), (162, 514), (282, 511)], fill="#365570", width=2)
    surfaces.add_paper_grain(leaf, seed=3117, foxing=.43).save(DIRECTORY / "Calendar2005_Previous.png")


def write_clip(name, duration, sample):
    rng = random.Random(1317)
    pcm = array("h", (round(max(-.95, min(.95, sample(i / RATE, rng))) * 32767)
                      for i in range(round(duration * RATE))))
    with wave.open(str(DIRECTORY / (name + ".wav")), "wb") as wav:
        wav.setparams((1, 2, RATE, 0, "NONE", ""))
        wav.writeframes(pcm.tobytes())


def tone(freq, t):
    return math.sin(math.tau * freq * t)


def strike(t, at, damp, freqs, noise, rng):
    t -= at
    if t < 0:
        return 0
    return math.exp(-t * damp) * (sum(gain * tone(freq, t) for freq, gain in freqs)
                                  + noise * rng.uniform(-1, 1) * math.exp(-45 * t))


def phone(t, rng):
    envelope = min(1, t * 38, (2.4 - t) * 10)
    answer = .07 * (tone(392, t) + .4 * tone(447, t)) * math.exp(-2.5 * abs(t - .5))
    static = .14 * rng.uniform(-1, 1) * (.65 + .35 * tone(13, t))
    return envelope * (answer + static + .028 * tone(100, t))


DIRECTORY.mkdir(parents=True, exist_ok=True)
calendar()
write_clip("CH01_Handset_Line", 2.4, phone)
write_clip("CH01_Plastic_Cradle", .26, lambda t, rng:
    strike(t, .009, 75, [(698, .23), (1183, .15)], .2, rng)
    + strike(t, .084, 97, [(572, .19)], .22, rng))
write_clip("CH01_Enamel_Pedal", .64, lambda t, rng:
    strike(t, .008, 34, [(333, .13), (910, .11)], .21, rng)
    + strike(t, .105, 17, [(472, .23), (821, .08), (1614, .043)], .13, rng)
    + strike(t, .228, 34, [(570, .09)], .11, rng))
write_clip("CH01_Ceramic_Contact", .38, lambda t, rng:
    strike(t, .006, 49, [(902, .26), (1860, .10), (2210, .065)], .12, rng))
