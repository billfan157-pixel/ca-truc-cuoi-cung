"""Re-bake decals (clean NaN-free), tear An's drawing fragments, audit PBR metrics & generate contact sheet."""

import sys
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))
import bake_surfaces as B
import bake_inspect as BI


def main():
    B.build_authored_decals()
    BI.draw_clock()
    BI.tear()

    tex = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Textures"
    print("--- M0-R PBR STATS AUDIT ---")
    for base in [
        "PlasterR", "PlasterB", "DadoR", "DadoB", "FloorR", "FloorB",
        "WoodDoorR", "WoodDoorB", "PaintedMetalR", "PaintedMetalB",
        "Steel", "Cotton", "CottonOld", "MattressR", "MattressB", "Ceramic", "Bark",
    ]:
        c = np.asarray(Image.open(tex / f"{base}_Color.png"), dtype=np.float32)
        n = np.asarray(Image.open(tex / f"{base}_Normal.png"), dtype=np.float32)
        m = np.asarray(Image.open(tex / f"{base}_Mask.png"), dtype=np.float32)
        print(
            f"{base:14s} | ColorSD={c.std():5.1f} | NormXY_SD={n[...,:2].std():5.1f} "
            f"| AO={m[...,1].mean():5.1f}+/-{m[...,1].std():4.1f} "
            f"| Smooth={m[...,3].mean()/255:.2f}+/-{m[...,3].std()/255:.2f}"
        )

    print("--- M0-R DECAL ALPHA STATS ---")
    for dname in ["WaterRun", "PaintLoss", "DampFloor", "RustRun", "WallBaseGrime", "WheelMarks", "BedContact"]:
        im = np.asarray(Image.open(tex / f"{dname}.png").convert("RGBA"), dtype=np.float32)
        print(f"{dname:14s} | AlphaMean={im[...,3].mean():5.1f} | AlphaSD={im[...,3].std():5.1f} | Max={im[...,3].max():5.1f}")

    out = ROOT / "output" / "m0r-evidence"
    out.mkdir(parents=True, exist_ok=True)
    samples = [
        "PlasterR_Color", "PlasterB_Color", "DadoR_Color", "FloorR_Color",
        "FloorR_Normal", "WoodDoorR_Color", "PaintedMetalR_Color", "Steel_Color",
        "TropicalLeaf_Color", "WaterRun", "PaintLoss", "DampFloor",
        "WheelMarks", "WallBaseGrime", "F101", "Drawing",
    ]
    sheet = Image.new("RGB", (4 * 320, 4 * 320), (24, 30, 28))
    dr = ImageDraw.Draw(sheet)
    for idx, name in enumerate(samples):
        im = Image.open(tex / f"{name}.png").convert("RGBA")
        bg = Image.new("RGBA", im.size, (44, 50, 46, 255))
        comp = Image.alpha_composite(bg, im).convert("RGB").resize((312, 294), Image.Resampling.LANCZOS)
        x, y = (idx % 4) * 320 + 4, (idx // 4) * 320 + 4
        sheet.paste(comp, (x, y))
        dr.text((x + 4, y + 296), name, fill=(230, 235, 220))
    sheet.save(out / "pbr-contact-sheet.jpg", quality=92)
    print("Saved", out / "pbr-contact-sheet.jpg")


if __name__ == "__main__":
    main()
