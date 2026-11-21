"""CH01 Weathering Decals & Courtyard Mist Generator (Upgrade #7 — Devotion-grade dampness).

Generates 1024x1024 PBR URP DBuffer decal textures (Color RGBA + Tangent Normal) and
a soft procedural mist puff texture for LOC01 exterior courtyard haze:
- TubeYellowStain (+ _Normal): Heat-aged yellow-ochre nicotine/humidity halo around T8 ceiling fixtures
- StretcherDragTrack (+ _Normal): Scuffed hospital stretcher/gurney rubber drag marks down terrazzo corridors
- TerrazzoRainSeep (+ _Normal): Capillary rainwater seepage & dark humid tide creeping up terrazzo wall bases
- CourtyardMistTex: Swirling soft alpha mist sprite for exterior side-gate low fog
"""

from __future__ import annotations

import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Textures"
SZ = 1024


def fbm(size: int, octaves: tuple[int, ...], seed: int, persistence: float = 0.54) -> np.ndarray:
    r = np.random.default_rng(seed)
    acc = np.zeros((size, size), dtype=np.float32)
    amp = 1.0
    norm = 0.0
    for freq in octaves:
        grid = r.random((freq, freq), dtype=np.float32)
        up = np.asarray(
            Image.fromarray(grid, mode="F").resize((size, size), Image.Resampling.BICUBIC),
            dtype=np.float32,
        )
        acc += up * amp
        norm += amp
        amp *= persistence
    return np.clip(acc / max(norm, 1e-6), 0.0, 1.0)


def fbm_aniso(size: int, fx_list: tuple[int, ...], fy_list: tuple[int, ...], seed: int) -> np.ndarray:
    r = np.random.default_rng(seed)
    acc = np.zeros((size, size), dtype=np.float32)
    amp = 1.0
    norm = 0.0
    for fx, fy in zip(fx_list, fy_list):
        grid = r.random((fy, fx), dtype=np.float32)
        up = np.asarray(
            Image.fromarray(grid, mode="F").resize((size, size), Image.Resampling.BICUBIC),
            dtype=np.float32,
        )
        acc += up * amp
        norm += amp
        amp *= 0.54
    return np.clip(acc / max(norm, 1e-6), 0.0, 1.0)


def normal_from_height(height: np.ndarray, strength: float = 4.0) -> np.ndarray:
    h = height.astype(np.float32)
    l = np.roll(h, 1, axis=1)
    r = np.roll(h, -1, axis=1)
    u = np.roll(h, 1, axis=0)
    d = np.roll(h, -1, axis=0)
    ul = np.roll(u, 1, axis=1)
    ur = np.roll(u, -1, axis=1)
    dl = np.roll(d, 1, axis=1)
    dr = np.roll(d, -1, axis=1)
    dx = ((ul + 2.0 * l + dl) - (ur + 2.0 * r + dr)) * (strength * 0.25)
    dy = ((dl + 2.0 * d + dr) - (ul + 2.0 * u + ur)) * (strength * 0.25)
    v = np.stack((dx, dy, np.ones_like(dx)), axis=-1)
    v /= np.maximum(1e-5, np.linalg.norm(v, axis=-1, keepdims=True))
    return v * 0.5 + 0.5


def save_arr(name: str, arr: np.ndarray) -> None:
    u8 = np.uint8(np.clip(arr, 0.0, 1.0) * 255.0 + 0.5)
    mode = "RGBA" if arr.ndim == 3 and arr.shape[-1] == 4 else "RGB"
    Image.fromarray(u8, mode=mode).save(DEST / f"{name}.png", optimize=False)


def build_tube_yellow_stain() -> None:
    """Warm yellow-brown heat/humidity halo + ballast dust speckles around ceiling fluorescent housings."""
    y, x = np.mgrid[0:SZ, 0:SZ].astype(np.float32) / (SZ - 1)
    n_macro = fbm(SZ, (6, 12, 24), seed=9101)
    n_fine = fbm(SZ, (32, 64, 128), seed=9102)
    # Elongated oval halo matching 1.2m T8 fixture footprint
    dx = (x - 0.5) * 2.15
    dy = (y - 0.5) * 3.10
    dist = np.sqrt(dx * dx + dy * dy)
    # Stronger yellow-brown scorch right outside ballast ends (x ~ 0.26, 0.74)
    end_hot = (
        np.exp(-((x - 0.25) ** 2 * 22.0 + (y - 0.5) ** 2 * 36.0))
        + np.exp(-((x - 0.75) ** 2 * 22.0 + (y - 0.5) ** 2 * 36.0))
    )
    halo = np.clip((1.0 - dist * 0.92) * (0.62 + 0.58 * n_macro), 0.0, 1.0)
    alpha = np.clip((halo * 0.68 + end_hot * 0.42) * (0.72 + 0.28 * n_fine), 0.0, 0.86)
    edge = (np.maximum(0.0, np.sin(x * math.pi)) ** 0.5) * (np.maximum(0.0, np.sin(y * math.pi)) ** 0.5)
    alpha *= edge

    r = 0.44 - end_hot * 0.14 + (n_fine - 0.5) * 0.08
    g = 0.36 - end_hot * 0.16 + (n_fine - 0.5) * 0.07
    b = 0.19 - end_hot * 0.09 + (n_fine - 0.5) * 0.05
    save_arr("TubeYellowStain", np.stack((r, g, b, alpha), axis=-1))
    save_arr("TubeYellowStain_Normal", normal_from_height(alpha * 0.28 + n_fine * 0.22, strength=2.6))


def build_stretcher_drag_track() -> None:
    """Longitudinal parallel rubber wheel scuffs, bumper scrapes & damp grit along hospital corridors."""
    n_fine = fbm(SZ, (24, 64, 128), seed=9201)
    n_streak = fbm_aniso(SZ, (32, 64), (4, 8), seed=9202)
    img = Image.new("RGBA", (SZ, SZ), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # Parallel stretcher wheel bands (left track around x=310, right track around x=714)
    rng = np.random.default_rng(9203)
    for base_x in (290, 322, 702, 734):
        pts = []
        for s in range(64):
            ky = s / 63.0
            px = base_x + math.sin(ky * math.pi * 2.4 + base_x * 0.01) * 16.0 + rng.uniform(-3.0, 3.0)
            py = 36 + ky * (SZ - 72)
            pts.append((px, py))
        d.line(pts, fill=(30, 27, 22, 138), width=11, joint="curve")
        d.line([(p[0] + 4, p[1]) for p in pts], fill=(42, 39, 32, 82), width=22, joint="curve")

    # Occasional diagonal caster skid & dark floor grime smear in center lane
    for _ in range(18):
        sx = int(rng.uniform(240, 780))
        sy = int(rng.uniform(90, SZ - 180))
        ex = sx + int(rng.uniform(-45, 45))
        ey = sy + int(rng.uniform(60, 150))
        d.line([(sx, sy), (ex, ey)], fill=(24, 22, 18, int(rng.uniform(55, 115))), width=int(rng.uniform(4, 10)))

    img = img.filter(ImageFilter.GaussianBlur(1.8))
    arr = np.asarray(img, dtype=np.float32) / 255.0
    y, x = np.mgrid[0:SZ, 0:SZ].astype(np.float32) / (SZ - 1)
    fade = (np.maximum(0.0, np.sin(y * math.pi)) ** 0.75) * (np.maximum(0.0, np.sin(x * math.pi)) ** 0.45)
    arr[..., 3] = np.clip(arr[..., 3] * (0.58 + 0.65 * n_streak) * fade, 0.0, 0.84)
    arr[..., :3] = np.clip(arr[..., :3] + (n_fine[..., None] - 0.5) * 0.06, 0.0, 1.0)
    save_arr("StretcherDragTrack", arr)
    save_arr("StretcherDragTrack_Normal", normal_from_height(1.0 - arr[..., 3] * 0.45 + n_fine * 0.14, strength=3.2))


def build_terrazzo_rain_seep() -> None:
    """Capillary monsoon rainwater seepage & mossy-ochre humid tide creeping up terrazzo skirting."""
    y, x = np.mgrid[0:SZ, 0:SZ].astype(np.float32) / (SZ - 1)
    n_macro = fbm(SZ, (5, 10, 22), seed=9301)
    n_fine = fbm(SZ, (32, 64, 128), seed=9302)
    n_vert = fbm_aniso(SZ, (24, 48), (6, 12), seed=9303)

    # Bottom (y=1) is wettest right against terrazzo floor skirting, wicking irregularly up to y~0.28
    wick_top = 0.42 + (n_macro - 0.5) * 0.34 + (n_vert - 0.5) * 0.18
    rise = np.clip((y - (1.0 - wick_top)) / np.maximum(0.18, wick_top), 0.0, 1.0) ** 1.15
    # Damp mineral salt rim at the top edge of capillary rise
    rim = np.exp(-((y - (1.0 - wick_top + 0.05)) * 18.0) ** 2) * (0.45 + 0.55 * n_fine)
    edge_x = np.maximum(0.0, np.sin(x * math.pi)) ** 0.38
    alpha = np.clip((rise * 0.86 + rim * 0.32) * edge_x * (0.72 + 0.38 * n_fine), 0.0, 0.90)

    # Dark mossy damp core + warm ochre water stain + chalky efflorescence line
    r = 0.13 + (1.0 - rise) * 0.14 + rim * 0.42 + (n_fine - 0.5) * 0.06
    g = 0.17 + (1.0 - rise) * 0.13 + rim * 0.40 + (n_fine - 0.5) * 0.06
    b = 0.13 + (1.0 - rise) * 0.08 + rim * 0.34 + (n_fine - 0.5) * 0.05
    save_arr("TerrazzoRainSeep", np.stack((r, g, b, alpha), axis=-1))
    save_arr("TerrazzoRainSeep_Normal", normal_from_height(rise * 0.25 + rim * 0.40 + n_fine * 0.22, strength=4.2))


def build_courtyard_mist() -> None:
    """Soft swirling 512x512 alpha mist puff texture for LOC01 exterior low ground fog."""
    msz = 512
    y, x = np.mgrid[0:msz, 0:msz].astype(np.float32) / (msz - 1) * 2.0 - 1.0
    dist = np.sqrt(x * x + y * y)
    n1 = fbm(msz, (4, 8, 16, 32), seed=9401)
    radial = np.clip(1.0 - dist, 0.0, 1.0) ** 1.85
    alpha = np.clip(radial * (0.42 + 0.72 * n1), 0.0, 0.88)
    col = np.stack(
        (
            np.full((msz, msz), 0.68, dtype=np.float32),
            np.full((msz, msz), 0.79, dtype=np.float32),
            np.full((msz, msz), 0.84, dtype=np.float32),
            alpha,
        ),
        axis=-1,
    )
    save_arr("CourtyardMistTex", col)


def main() -> None:
    DEST.mkdir(parents=True, exist_ok=True)
    build_tube_yellow_stain()
    build_stretcher_drag_track()
    build_terrazzo_rain_seep()
    build_courtyard_mist()
    print(f"CH01 weathering decals + courtyard mist generated cleanly in {DEST}")


if __name__ == "__main__":
    main()
