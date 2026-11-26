"""Bake compact original surface maps for the CH01 environment art pass."""
from pathlib import Path

import numpy as np
from PIL import Image

MAPS = Path(__file__).resolve().parents[1] / "Prototype/Assets/CTCC/Art/Textures"
RNG = np.random.default_rng(0x101)


def noise(size, cells):
    src = Image.fromarray(RNG.integers(0, 256, (cells, cells), dtype=np.uint8))
    return np.asarray(src.resize((size, size), Image.Resampling.BICUBIC), dtype=np.float32) / 255.0


def write(name, pixels):
    Image.fromarray(np.uint8(np.clip(pixels, 0, 1) * 255 + 0.5)).save(MAPS / f"{name}.png", optimize=True)


def decal(name, pigment, alpha, detail):
    grain = (0.72 + detail * 0.36)[..., None]
    color = np.array(pigment, dtype=np.float32) / 255.0 * grain
    write(name, np.concatenate((color, np.clip(alpha, 0, 1)[..., None]), axis=2))


def main():
    size = 512
    y, x = np.mgrid[:size, :size] / (size - 1)
    n = noise(size, 22)
    fine = noise(size, 155)
    broad = noise(size, 7)
    edge = np.clip(np.minimum(x, 1 - x) * 18, 0, 1) * np.clip(np.minimum(y, 1 - y) * 21, 0, 1)
    vertical = np.asarray(Image.fromarray(np.uint8(noise(size, 18) * 255)).resize(
        (size, size // 9), Image.Resampling.BICUBIC).resize(
        (size, size), Image.Resampling.BICUBIC), dtype=np.float32) / 255.0
    # Capillary tide line with spaced vertical chalk inclusions.
    tide = 0.25 + 0.25 * broad + 0.14 * vertical
    capillary = np.clip((tide - y) * 4.6, 0, 1) * (0.43 + n * 0.55) * edge
    decal("CH01_Capillary", (100, 114, 95), capillary * 0.69, fine)

    # Water exits the metal collar at the top and branches across old limewash.
    center = 0.52 + 0.087 * np.sin(y * 8.5) + 0.045 * n
    streaks = np.exp(-((x - center) / (0.045 + y * 0.21)) ** 2)
    streaks *= np.clip(vertical * 2.5 - 0.50, 0.12, 1) * y ** 0.45 * edge
    decal("CH01_LimeRunoff", (100, 106, 91), streaks * 0.68, fine)

    # Enamel wears around fasteners and sheet edges. Keep the central printing intact.
    margins = np.minimum(np.minimum(x * 1.2, (1 - x) * 1.18), np.minimum(y * 0.86, (1 - y) * 0.86))
    chips = (1 - np.clip(margins * 14, 0, 1)) * np.clip((fine * 0.53 + n * 0.47 - 0.47) * 3.6, 0, 1)
    screws = np.maximum.reduce([np.exp(-((x - cx) ** 2 * 1600 + (y - cy) ** 2 * 285))
                               for cx, cy in [(0.035, 0.14), (0.96, 0.84)]])
    decal("CH01_EnamelChips", (107, 76, 52), np.maximum(chips * 0.82, screws * 0.58), n)

    # Two shopping-cart-width tracks are distributed and broken, so they read as use.
    center = 0.5 + 0.10 * (y - 0.25) ** 2
    wheel = sum(np.exp(-((x - center + offset) / 0.040) ** 2) for offset in (-0.23, 0.23))
    wheel *= edge * np.clip(n * 1.45 - 0.17, 0, 1)
    decal("CH01_WheelWear", (114, 109, 94), wheel * 0.52, fine)

    wear = np.exp(-((x - 0.48) * 3.4) ** 2 - ((y - 0.49) * 2.8) ** 2) * edge
    decal("CH01_VarnishWear", (155, 117, 75), wear * (0.32 + 0.47 * n) * 0.58, fine)

    # Preserve the original packed material's metal and AO. Continuous rain sheen broken by fine aggregate
    # (no low-frequency cow-spot patches — distinct puddles are driven by DampFloor decals & RainRipples!).
    source = Image.open(MAPS / "WetAsphalt_Mask.png").convert("RGBA").resize((1024, 1024), Image.Resampling.LANCZOS)
    mask = np.asarray(source, dtype=np.float32).copy() / 255
    broad_wet = noise(1024, 10) * 0.42 + noise(1024, 28) * 0.36 + noise(1024, 96) * 0.22
    micro_grit = noise(1024, 240)
    mask[..., 3] = np.clip(0.42 + 0.24 * np.clip((broad_wet - 0.30) * 1.45, 0, 1) + (micro_grit - 0.5) * 0.12, 0.30, 0.72)
    write("CH01_AsphaltHumidity_Mask", mask)

    # Long, overlapping cellulose fibres and a gentle cross-grain at macro distance.
    fiber_src = Image.fromarray(RNG.integers(0, 256, (230, 890), dtype=np.uint8))
    fiber = np.asarray(fiber_src.resize((1024, 1024), Image.Resampling.BICUBIC), dtype=np.float32) / 255.0
    grain = fiber * 0.68 + noise(1024, 410) * 0.32
    dy, dx = np.gradient(grain)
    normal = np.stack((-dx * .37, dy * .37, np.ones_like(dx)), axis=2)
    normal /= np.linalg.norm(normal, axis=2, keepdims=True)
    out = MAPS.parent / "Resources/CTCC/PaperFiber_Normal.png"
    Image.fromarray(np.uint8((normal * 0.5 + 0.5) * 255 + 0.5)).save(out, optimize=True)


if __name__ == "__main__":
    main()
