"""LOC01 Exterior & Entrance Hero Asset Kit production library (Blender 4.5 LTS).

Extends LOC02/LOC03/HALL_STAFF's Cycles PBR multi-pass baker (`loc02/hero_lib.py`)
with authentic 1987-2012 Vietnamese district general hospital exterior architectural
and landscape procedural PBR materials (Reality B 2005 operational <-> Reality R 2012
monsoon-weathered, closed, slated for demolition).

Key anti-artifact rules baked into every shader:
  - ZERO oversized Dalmatian cow-print terrazzo on beams/slabs/soffits/copings.
  - Real fine washed-stone cement plaster (`ext_concrete_washed`) with drip staining & moss.
  - Real running-bond 1987 Vietnamese red-clay brick masonry (`ext_brick_1987`) w/ damp moss base.
  - Authentic 1987 ochre limewash (`ext_ochre_stucco`) w/ vertical tropical rain streaks.
  - Calibrated night-readable dark materials (e.g. wet maroon motorbike lacquer, wet wrought iron,
    wet soil & tropical bark) that hold crisp PBR specular highlights without black crush.

Outputs:
  - Editable .blend sources  -> ArtSource/Blender/LOC01/<Asset>.blend
  - High-res print sheets    -> ArtSource/LOC01/prints/<sheet>.png
  - Game-ready FBX (+LOD0-2) -> Prototype/Assets/CTCC/Art/Meshes/<Asset>.fbx
  - Cycles PBR maps (B & R)  -> Prototype/Assets/CTCC/Art/Textures/<Atlas>{B,R}_{Color,Mask}.png + _Normal.png
  - B/R studio renders       -> output/loc01-hero/blender-renders/<Asset>_BR.png
"""
from __future__ import annotations

import sys
from pathlib import Path

import bpy

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))
sys.path.insert(0, str(HERE.parent / "loc02"))
sys.path.insert(0, str(HERE.parent / "loc03"))
sys.path.insert(0, str(HERE.parent / "hall_staff"))

import common as C  # noqa: E402
import hero_lib as H  # noqa: E402
import hall_staff_lib as HS  # noqa: E402

ROOT = C.ROOT
SRC_DIR = ROOT / "ArtSource" / "Blender" / "LOC01"
RENDER_DIR = ROOT / "output" / "loc01-hero" / "blender-renders"
PRINT_DIR = ROOT / "ArtSource" / "LOC01" / "prints"
SRC_DIR.mkdir(parents=True, exist_ok=True)
RENDER_DIR.mkdir(parents=True, exist_ok=True)
PRINT_DIR.mkdir(parents=True, exist_ok=True)

H.SRC_DIR = SRC_DIR
H.RENDER_DIR = RENDER_DIR
H.PRINT_DIR = PRINT_DIR


# --------------------------------------------------------------------------------------
# Calibrated 1987-2012 Vietnamese Hospital Exterior & Landscape Procedural PBR Shaders
# --------------------------------------------------------------------------------------

def _streak_coord(b, P, sx=7.5, sy=7.5, sz=0.65):
    """Stretch object coordinates vertically to form organic monsoon rain & algae runoff streaks."""
    return b.vmath("MULTIPLY", P, (sx, sy, sz))


def _k_ext_ochre_stucco(b, A, P, gv, rnd, edge, cav):
    """1987 Vietnamese institutional ochre-cream exterior limewash (vôi vàng bệnh viện 1987).
    B (2005): warm maintained ochre-ivory plaster w/ subtle trowel variation.
    R (2012): aged olive-ochre w/ vertical tropical monsoon streaks, damp moss in crevices & faint mortar chips."""
    plaster_fine = b.noise(P, 42.0, 6, 0.65)
    plaster_broad = b.noise(P, 3.2, 4, 0.58)
    streaks = b.noise(_streak_coord(b, P, 4.8, 4.8, 0.48), 2.4, 5, 0.65)
    streak_mask = b.mapr(streaks, 0.48, 0.78)
    moss = b.mapr(b.noise(P, 6.5, 4, 0.7), 0.54, 0.78)

    col_b = b.mix(H.srgb(0.68, 0.63, 0.45), H.srgb(0.79, 0.74, 0.56), b.mapr(plaster_broad, 0.25, 0.78))
    col_r = b.mix(H.srgb(0.45, 0.43, 0.31), H.srgb(0.58, 0.55, 0.40), b.mapr(plaster_broad, 0.22, 0.78))
    # Vertical tropical rain darkening + dark-olive algae streaks stronger in R
    col_r = b.mix(col_r, H.srgb(0.22, 0.25, 0.19), b.math("MULTIPLY", streak_mask, 0.62))
    col_r = b.mix(col_r, H.srgb(0.18, 0.23, 0.16), b.math("MULTIPLY", moss, b.math("ADD", 0.20, b.math("MULTIPLY", cav, 0.75))))
    # Subtle grey cement patch wear on exposed outer corners in R
    col_r = b.mix(col_r, H.srgb(0.44, 0.45, 0.42), b.math("MULTIPLY", edge, 0.38))

    base = b.mix(col_b, col_r, A)
    base = b.mix(base, H.srgb(0.15, 0.16, 0.13), b.math("MULTIPLY", cav, b.mixf(0.28, 0.55, A)))
    rough_b = b.mixf(0.78, 0.88, b.mapr(plaster_fine, 0.2, 0.8))
    rough_r = b.mixf(0.76, 0.54, b.math("MULTIPLY", streak_mask, 0.75))  # wet streaks slightly glossier in rain!
    rough = b.mixf(rough_b, rough_r, A)
    height = b.math("ADD", b.math("MULTIPLY", plaster_fine, 0.65), b.math("MULTIPLY", plaster_broad, 0.35))
    return base, rough, 0.0, height, 0.32


def _k_ext_concrete_washed(b, A, P, gv, rnd, edge, cav):
    """Authentic 1987 Vietnamese cast-in-place RC beam / sunshade ô văng / coping / soffit concrete.
    Fine mineral washed-stone cement plaster (đá rửa mịn & bê tông trần) — ZERO giant Dalmatian blotches!"""
    micro = b.noise(P, 85.0, 5, 0.65)
    fine_agg = b.voronoi(P, 72.0, "F1", 0.90, "Distance")
    broad = b.noise(P, 2.8, 4, 0.55)
    streaks = b.mapr(b.noise(_streak_coord(b, P, 5.2, 5.2, 0.55), 2.8, 4, 0.62), 0.46, 0.78)
    algae = b.mapr(b.noise(P, 8.0, 4, 0.68), 0.56, 0.80)

    col_b = b.mix(H.srgb(0.56, 0.57, 0.53), H.srgb(0.67, 0.68, 0.63), b.mapr(broad, 0.25, 0.78))
    col_b = b.mix(col_b, H.srgb(0.46, 0.47, 0.44), b.math("MULTIPLY", b.mapr(fine_agg, 0.15, 0.65), 0.22))

    col_r = b.mix(H.srgb(0.36, 0.38, 0.35), H.srgb(0.48, 0.49, 0.45), b.mapr(broad, 0.22, 0.78))
    col_r = b.mix(col_r, H.srgb(0.20, 0.23, 0.19), b.math("MULTIPLY", streaks, 0.58))
    col_r = b.mix(col_r, H.srgb(0.16, 0.22, 0.15), b.math("MULTIPLY", algae, b.math("ADD", 0.22, b.math("MULTIPLY", cav, 0.70))))
    col_r = b.mix(col_r, H.srgb(0.54, 0.55, 0.51), b.math("MULTIPLY", edge, 0.34))

    base = b.mix(col_b, col_r, A)
    base = b.mix(base, H.srgb(0.14, 0.15, 0.14), b.math("MULTIPLY", cav, b.mixf(0.28, 0.52, A)))
    rough = b.mixf(0.76, b.mixf(0.66, 0.46, streaks), A)
    height = b.math("ADD", b.math("MULTIPLY", micro, 0.62), b.math("MULTIPLY", fine_agg, 0.38))
    return base, rough, 0.0, height, 0.34


def _k_ext_concrete_deck(b, A, P, gv, rnd, edge, cav):
    """Sheltered dry-center / rain-blown-edge brushed hospital portico concrete deck.
    Calibrated deeper neutral cement tone so overhead warm canopy spot illuminates it naturally
    without blowing out to a bright white slab against dark wet courtyard asphalt."""
    micro = b.noise(P, 95.0, 5, 0.66)
    broad = b.noise(P, 2.2, 4, 0.56)
    scuff = b.mapr(b.noise(P, 5.5, 4, 0.64), 0.42, 0.78)
    py = b.sep(P)[1]
    # Rain-blown outer southern border (y < -2.35) gets darker & slightly glossier in R!
    edge_wet = b.mapr(py, -2.25, -2.92)

    col_b = b.mix(H.srgb(0.40, 0.41, 0.38), H.srgb(0.50, 0.51, 0.47), b.mapr(broad, 0.25, 0.78))
    col_r = b.mix(H.srgb(0.24, 0.255, 0.24), H.srgb(0.33, 0.345, 0.32), b.mapr(broad, 0.22, 0.78))
    col_r = b.mix(col_r, H.srgb(0.16, 0.175, 0.165), b.math("MULTIPLY", scuff, 0.42))
    col_r = b.mix(col_r, H.srgb(0.11, 0.13, 0.135), b.math("MULTIPLY", edge_wet, 0.68))

    base = b.mix(col_b, col_r, A)
    base = b.mix(base, H.srgb(0.10, 0.11, 0.105), b.math("MULTIPLY", cav, b.mixf(0.24, 0.48, A)))
    rough = b.mixf(0.78, b.mixf(0.76, 0.36, edge_wet), A)
    height = b.math("ADD", b.math("MULTIPLY", micro, 0.68), b.math("MULTIPLY", broad, 0.32))
    return base, rough, 0.0, height, 0.32


def _k_ext_brick_1987(b, A, P, gv, rnd, edge, cav):
    """Authentic 1987 Vietnamese fired red-clay masonry brick (gạch đất nung) for plinths,
    perimeter wall bases, guardhouse dado & raised planters. Procedural running-bond courses
    in XY/ZY with sandy lime mortar, varied kiln tones, and rising tropical damp/moss in R."""
    px, py, pz = b.sep(P)
    # Combine X+Y so vertical faces along both X and Y walls show horizontal brick courses!
    u_wall = b.math("ADD", px, py)
    brick_vec = b.node("ShaderNodeCombineXYZ")
    b.feed(brick_vec, "X", u_wall)
    b.feed(brick_vec, "Y", pz)
    b.feed(brick_vec, "Z", 0.0)

    brk = b.node("ShaderNodeTexBrick", offset=0.5, offset_frequency=2, squash=1.0)
    b.feed(brk, "Vector", brick_vec.outputs["Vector"])
    b.feed(brk, "Scale", 4.6)
    b.feed(brk, "Mortar Size", 0.028)
    b.feed(brk, "Mortar Smooth", 0.18)
    b.feed(brk, "Bias", -0.08)
    b.feed(brk, "Brick Width", 0.96)
    b.feed(brk, "Row Height", 0.34)
    b.feed(brk, "Color1", (*H.srgb(0.46, 0.19, 0.13)[:3], 1.0))
    b.feed(brk, "Color2", (*H.srgb(0.31, 0.13, 0.095)[:3], 1.0))
    b.feed(brk, "Mortar", (*H.srgb(0.50, 0.48, 0.42)[:3], 1.0))

    brick_col = brk.outputs["Color"]
    brick_fac = brk.outputs["Fac"]  # 0=mortar, 1=brick

    grit = b.noise(P, 55.0, 5, 0.68)
    moss_n = b.mapr(b.noise(P, 6.0, 4, 0.68), 0.50, 0.78)
    # Stronger capillary damp & moss near bottom (pz < 0.38m)
    low_damp = b.mapr(pz, 0.45, 0.04)

    col_b = b.mix(brick_col, H.srgb(0.24, 0.12, 0.09), b.math("MULTIPLY", grit, 0.18))
    col_r = b.mix(brick_col, H.srgb(0.18, 0.10, 0.08), b.math("ADD", 0.22, b.math("MULTIPLY", low_damp, 0.42)))
    col_r = b.mix(col_r, H.srgb(0.14, 0.21, 0.12), b.math("MULTIPLY", moss_n, b.math("ADD", 0.18, b.math("MULTIPLY", low_damp, 0.65))))
    # Slightly darken mortar lines in R (damp moss-stained mortar)
    mortar_mask = b.math("SUBTRACT", 1.0, brick_fac)
    col_r = b.mix(col_r, H.srgb(0.22, 0.24, 0.19), b.math("MULTIPLY", mortar_mask, 0.55))

    base = b.mix(col_b, col_r, A)
    base = b.mix(base, H.srgb(0.10, 0.08, 0.07), b.math("MULTIPLY", cav, 0.42))
    rough = b.mixf(0.84, b.mixf(0.76, 0.52, low_damp), A)
    height = b.math("ADD", b.math("MULTIPLY", brick_fac, 0.72), b.math("MULTIPLY", grit, 0.28))
    return base, rough, 0.0, height, 0.52


def _k_ext_dado_green(b, A, P, gv, rnd, edge, cav):
    """Heritage Vietnamese hospital exterior forest-sage oil enamel for lower dado, canopy fascia & trim."""
    streaks = b.mapr(b.noise(_streak_coord(b, P, 5.5, 5.5, 0.55), 3.0, 4, 0.62), 0.48, 0.78)
    base, rough, metal, h0, s0 = H._k_paint(
        b, A, P, gv, rnd, edge, cav,
        col_b=H.srgb(0.145, 0.305, 0.235),
        col_r=H.srgb(0.115, 0.225, 0.175),
        rough_b=0.38, rough_r=0.52,
        primer=H.srgb(0.45, 0.46, 0.42),
        rust=H.srgb(0.24, 0.15, 0.09),
        metal_sheet=False,
    )
    base = b.mix(base, H.srgb(0.08, 0.14, 0.11), b.math("MULTIPLY", streaks, b.math("MULTIPLY", A, 0.45)))
    rough = b.mixf(rough, 0.34, b.math("MULTIPLY", streaks, b.math("MULTIPLY", A, 0.65)))
    return base, rough, metal, h0, s0


def _k_ext_iron_wrought(b, A, P, gv, rnd, edge, cav):
    """Forged dark hospital green-charcoal wrought iron for gates, spear fences, lantern cages & grilles.
    Calibrated so rain-wet edges & rust seams gleam authentically under sodium/moonlight without pitch-black crush!"""
    rust_n = b.mapr(b.noise(P, 12.0, 4, 0.65), 0.52, 0.78)
    base, rough, metal, h0, s0 = H._k_paint(
        b, A, P, gv, rnd, edge, cav,
        col_b=H.srgb(0.16, 0.25, 0.21),
        col_r=H.srgb(0.14, 0.20, 0.175),
        rough_b=0.36, rough_r=0.46,
        primer=H.srgb(0.32, 0.34, 0.33),
        rust=H.srgb(0.36, 0.16, 0.08),
        metal_sheet=True,
    )
    # Extra rust blooms inside weld cavities & rain joints in R
    rust_zone = b.math("MULTIPLY", b.math("ADD", b.math("MULTIPLY", cav, 0.65), b.math("MULTIPLY", edge, 0.50)), rust_n)
    base = b.mix(base, H.srgb(0.34, 0.15, 0.075), b.math("MULTIPLY", rust_zone, b.math("MULTIPLY", A, 0.78)))
    metal = b.mixf(0.48, 0.38, A)
    return base, rough, metal, h0, max(s0, 0.28)


def _k_ext_mailbox_green(b, A, P, gv, rnd, edge, cav):
    """Weathered institutional green enameled sheet-steel for GATE-CONG iron mailbox (E10a)."""
    return H._k_paint(
        b, A, P, gv, rnd, edge, cav,
        col_b=H.srgb(0.22, 0.42, 0.31),
        col_r=H.srgb(0.18, 0.32, 0.24),
        rough_b=0.34, rough_r=0.48,
        primer=H.srgb(0.48, 0.50, 0.46),
        rust=H.srgb(0.36, 0.16, 0.075),
        metal_sheet=True,
    )


def _k_ext_galv_zinc(b, A, P, gv, rnd, edge, cav):
    """Weathered galvanized zinc steel for corrugated roofs, gutters, downspouts & utility brackets."""
    spangle = b.voronoi(P, 64.0, "F1", 0.95, "Color")
    sp_luma = b.math("MULTIPLY", b.sep(spangle)[0], 0.16)
    rust_s = b.mapr(b.noise(_streak_coord(b, P, 4.5, 4.5, 0.7), 3.2, 4, 0.66), 0.54, 0.80)

    col_b = b.mix(H.srgb(0.56, 0.59, 0.58), H.srgb(0.68, 0.71, 0.70), sp_luma)
    col_r = b.mix(H.srgb(0.38, 0.41, 0.40), H.srgb(0.49, 0.51, 0.49), sp_luma)
    col_r = b.mix(col_r, H.srgb(0.34, 0.18, 0.10), b.math("MULTIPLY", rust_s, 0.68))
    col_r = b.mix(col_r, H.srgb(0.28, 0.14, 0.08), b.math("MULTIPLY", cav, 0.55))

    base = b.mix(col_b, col_r, A)
    metal = b.mixf(0.76, b.mixf(0.54, 0.16, rust_s), A)
    rough = b.mixf(0.34, b.mixf(0.46, 0.72, rust_s), A)
    height = b.math("ADD", b.math("MULTIPLY", b.noise(P, 95.0, 4), 0.5), b.math("MULTIPLY", rust_s, 0.5))
    return base, rough, metal, height, 0.28


def _k_ext_terrazzo_fine(b, A, P, gv, rnd, edge, cav):
    """Authentic Vietnamese FINE polished stone-chip terrazzo (ghế đá mài bệnh viện).
    Fine mineral aggregate chips (scale=65-110), soft warm-grey/ivory/slate contrast — NEVER cow-print!"""
    c1 = b.voronoi(P, 78.0, "F1", 0.92, "Color")
    c2 = b.voronoi(P, 135.0, "F1", 0.90, "Distance")
    chip_sel = b.sep(c1)[0]
    chip_mask = b.mapr(c2, 0.25, 0.52)

    matrix_b = H.srgb(0.64, 0.65, 0.61)
    matrix_r = H.srgb(0.46, 0.48, 0.45)
    chip_col = b.mix(H.srgb(0.30, 0.32, 0.31), H.srgb(0.78, 0.77, 0.71), b.mapr(chip_sel, 0.2, 0.8))

    col_b = b.mix(matrix_b, chip_col, b.math("MULTIPLY", chip_mask, 0.42))
    col_r = b.mix(matrix_r, chip_col, b.math("MULTIPLY", chip_mask, 0.42))
    moss = b.mapr(b.noise(P, 8.5, 4, 0.65), 0.54, 0.80)
    col_r = b.mix(col_r, H.srgb(0.18, 0.24, 0.16), b.math("MULTIPLY", moss, b.math("ADD", 0.16, b.math("MULTIPLY", cav, 0.72))))

    base = b.mix(col_b, col_r, A)
    base = b.mix(base, H.srgb(0.16, 0.17, 0.15), b.math("MULTIPLY", cav, 0.38))
    # Wet polished terrazzo seats catch rain highlights beautifully!
    rough = b.mixf(0.32, 0.26, A)
    height = b.math("MULTIPLY", b.noise(P, 110.0, 3), 0.35)
    return base, rough, 0.0, height, 0.14


def _k_ext_ironwood_wet(b, A, P, gv, rnd, edge, cav):
    """Dark weathered Vietnamese ironwood (gỗ lim) for monumental hospital entrance doors & guard desk."""
    base, rough, metal, h0, s0 = H._k_wood(b, A, P, gv, rnd, edge, cav, light=False)
    # Rain-wet gloss on exterior doors in R
    rough = b.mixf(rough, b.mixf(0.38, 0.54, cav), A)
    return base, rough, metal, h0, s0


def _k_ext_moto_maroon(b, A, P, gv, rnd, edge, cav):
    """Glossy wet Vietnamese Super-Dream-era deep plum-eggplant / wine-burgundy metallic fairing lacquer.
    Matches the rich dark plum-wine hue of the user's reference photo with crisp specular clearcoat!"""
    micro = b.noise(P, 160.0, 2, 0.5)
    dust = b.mapr(b.noise(P, 12.0, 3, 0.6), 0.52, 0.78)
    base = b.mix(H.srgb(0.28, 0.085, 0.195), H.srgb(0.23, 0.072, 0.165), A)
    pz = b.sep(P)[2]
    splash = b.math("MULTIPLY", b.mapr(pz, 0.30, 0.08), dust)
    base = b.mix(base, H.srgb(0.22, 0.20, 0.18), b.math("MULTIPLY", splash, 0.35))
    base = b.mix(base, H.srgb(0.11, 0.032, 0.078), b.math("MULTIPLY", cav, 0.28))
    return base, b.mixf(0.14, 0.20, splash), 0.44, b.math("MULTIPLY", micro, 0.04), 0.04


def _k_ext_moto_ivory(b, A, P, gv, rnd, edge, cav):
    """Crisp glossy pearl-white / cool-ivory ABS leg shield matching classic Vietnamese Dream yếm xe."""
    pz = b.sep(P)[2]
    dust = b.mapr(b.noise(P, 14.0, 3, 0.6), 0.54, 0.80)
    splash = b.math("MULTIPLY", b.mapr(pz, 0.28, 0.08), dust)
    base = b.mix(H.srgb(0.84, 0.85, 0.83), H.srgb(0.78, 0.79, 0.76), A)
    base = b.mix(base, H.srgb(0.38, 0.36, 0.32), b.math("MULTIPLY", splash, 0.42))
    base = b.mix(base, H.srgb(0.45, 0.46, 0.44), b.math("MULTIPLY", cav, 0.26))
    return base, b.mixf(0.18, 0.28, splash), 0.05, b.math("MULTIPLY", b.noise(P, 120.0, 2), 0.05), 0.05


def _k_ext_moto_chrome(b, A, P, gv, rnd, edge, cav):
    """Bright gleaming polished chrome for 17-inch wheel rims, wire spokes, front basket rack, grab-bar, shocks & muffler heat shield.
    Calibrated metallic 0.76 so it stays bright and unmistakable under dark night-time skies & reflection probes!"""
    base, rough, _, h0, _ = H._k_steel(b, A, P, gv, rnd, edge, cav, tone=H.srgb(0.96, 0.97, 0.98))
    return base, b.mixf(0.14, 0.20, A), 0.76, h0, 0.04


def _k_ext_moto_alu(b, A, P, gv, rnd, edge, cav):
    """Bright satin cast-silver aluminum for engine crankcase, front/rear brake drum hubs, fork lowers & swingarm."""
    base, rough, _, h0, _ = H._k_steel(b, A, P, gv, rnd, edge, cav, tone=H.srgb(0.86, 0.88, 0.90))
    return base, b.mixf(0.28, 0.36, A), 0.50, h0, 0.08


def _k_ext_moto_exhaust_black(b, A, P, gv, rnd, edge, cav):
    """Satin heat-resistant matte-black enamel for iconic Dream tapered megaphone exhaust body & black mudguard."""
    grain = b.noise(P, 140.0, 3, 0.6)
    base = b.mix(H.srgb(0.085, 0.090, 0.095), H.srgb(0.105, 0.110, 0.115), A)
    base = b.mix(base, H.srgb(0.20, 0.20, 0.21), b.math("MULTIPLY", edge, 0.28))
    return base, b.mixf(0.36, 0.42, A), 0.20, grain, 0.08


def _k_ext_moto_leather(b, A, P, gv, rnd, edge, cav):
    """Smooth fine-grain black motorbike vinyl dual saddle & rubber handgrips (realistic subtle pebble, never tin-foil crinkled!)."""
    grain = b.noise(P, 260.0, 3, 0.55)
    base = b.mix(H.srgb(0.10, 0.105, 0.11), H.srgb(0.12, 0.125, 0.13), A)
    base = b.mix(base, H.srgb(0.19, 0.195, 0.20), b.math("MULTIPLY", edge, 0.32))
    return base, b.mixf(0.42, 0.36, A), 0.0, grain, 0.045


def _k_ext_moto_tire(b, A, P, gv, rnd, edge, cav):
    """Wet vulcanized motorbike rubber tire with subtle tread bump & road grit."""
    tread = b.wave(P, 48.0, "BANDS", "Z", profile="SAW")
    base = b.mix(H.srgb(0.10, 0.105, 0.105), H.srgb(0.125, 0.13, 0.125), A)
    return base, b.mixf(0.58, 0.44, A), 0.0, tread, 0.26


def _k_ext_wet_soil(b, A, P, gv, rnd, edge, cav):
    """Organic tropical wet courtyard planter soil, dark mulch & fallen leaf litter (replaces flat bark!)."""
    clump = b.voronoi(P, 38.0, "F1", 0.92, "Distance")
    grit = b.noise(P, 85.0, 5, 0.72)
    leaf_fleck = b.mapr(b.voronoi(P, 16.0, "F1", 0.88, "Color"), 0.58, 0.85)
    moss = b.mapr(b.noise(P, 5.5, 4, 0.68), 0.55, 0.82)

    col_b = b.mix(H.srgb(0.16, 0.12, 0.085), H.srgb(0.24, 0.18, 0.125), grit)
    col_r = b.mix(H.srgb(0.095, 0.08, 0.062), H.srgb(0.16, 0.13, 0.095), grit)
    col_r = b.mix(col_r, H.srgb(0.12, 0.18, 0.095), b.math("MULTIPLY", moss, 0.55))
    col_r = b.mix(col_r, H.srgb(0.24, 0.19, 0.11), b.math("MULTIPLY", leaf_fleck, 0.35))

    base = b.mix(col_b, col_r, A)
    rough = b.mixf(0.86, 0.56, A)  # wet rain-soaked soil catches cool moonlight glint!
    height = b.math("ADD", b.math("MULTIPLY", clump, 0.55), b.math("MULTIPLY", grit, 0.45))
    return base, rough, 0.0, height, 0.55


def _k_ext_cast_iron(b, A, P, gv, rnd, edge, cav):
    """Heavy wet cast-iron monsoon storm drain grate & bollard cap."""
    pitting = b.noise(P, 75.0, 4, 0.68)
    rust = b.mapr(b.noise(P, 14.0, 4, 0.65), 0.50, 0.78)
    base = b.mix(H.srgb(0.26, 0.28, 0.29), H.srgb(0.20, 0.22, 0.23), A)
    base = b.mix(base, H.srgb(0.34, 0.17, 0.09), b.math("MULTIPLY", rust, b.mixf(0.22, 0.62, A)))
    base = b.mix(base, H.srgb(0.44, 0.46, 0.47), b.math("MULTIPLY", edge, 0.42))
    return base, b.mixf(0.46, 0.36, A), 0.65, pitting, 0.34


def _k_ext_ceramic_brown(b, A, P, gv, rnd, edge, cav):
    """Glossy brown-glazed porcelain electrical pole insulator."""
    base = b.mix(H.srgb(0.32, 0.16, 0.10), H.srgb(0.26, 0.14, 0.09), A)
    return base, 0.16, 0.04, b.math("MULTIPLY", b.noise(P, 90.0, 2), 0.05), 0.05


def _k_ext_bark_tropical(b, A, P, gv, rnd, edge, cav):
    """Deeply furrowed damp Vietnamese bàng/xà cừ tree bark with moss creeping up lower trunk."""
    pz = b.sep(P)[2]
    furrow = b.wave(_streak_coord(b, P, 14.0, 14.0, 1.4), 6.0, "BANDS", "X", profile="SIN")
    grit = b.noise(P, 48.0, 5, 0.7)
    moss = b.math("MULTIPLY", b.mapr(pz, 1.6, 0.15), b.mapr(b.noise(P, 6.0, 4, 0.65), 0.45, 0.75))

    base = b.mix(H.srgb(0.20, 0.16, 0.125), H.srgb(0.14, 0.12, 0.095), A)
    base = b.mix(base, H.srgb(0.28, 0.24, 0.19), b.math("MULTIPLY", furrow, 0.28))
    base = b.mix(base, H.srgb(0.14, 0.23, 0.12), b.math("MULTIPLY", moss, b.mixf(0.35, 0.78, A)))
    rough = b.mixf(0.84, 0.62, A)
    height = b.math("ADD", b.math("MULTIPLY", furrow, 0.65), b.math("MULTIPLY", grit, 0.35))
    return base, rough, 0.0, height, 0.55


def _k_ext_amber_lens(b, A, P, gv, rnd, edge, cav):
    """Motorbike faceted amber turn signal lens."""
    return H.srgb(0.84, 0.44, 0.06), 0.16, 0.05, b.math("MULTIPLY", b.wave(P, 120.0), 0.15), 0.12


def _k_ext_red_lens(b, A, P, gv, rnd, edge, cav):
    """Motorbike faceted ruby-red taillight lens."""
    return H.srgb(0.68, 0.06, 0.05), 0.16, 0.05, b.math("MULTIPLY", b.wave(P, 120.0), 0.15), 0.12


def register_loc01_kinds():
    HS.register_hall_staff_kinds()
    H.KINDS.update({
        "ext_ochre_stucco":     _k_ext_ochre_stucco,
        "ext_concrete_washed":  _k_ext_concrete_washed,
        "ext_concrete_deck":    _k_ext_concrete_deck,
        "ext_brick_1987":       _k_ext_brick_1987,
        "ext_dado_green":       _k_ext_dado_green,
        "ext_iron_wrought":     _k_ext_iron_wrought,
        "ext_mailbox_green":    _k_ext_mailbox_green,
        "ext_galv_zinc":        _k_ext_galv_zinc,
        "ext_terrazzo_fine":    _k_ext_terrazzo_fine,
        "ext_ironwood_wet":     _k_ext_ironwood_wet,
        "ext_moto_maroon":      _k_ext_moto_maroon,
        "ext_moto_ivory":       _k_ext_moto_ivory,
        "ext_moto_chrome":      _k_ext_moto_chrome,
        "ext_moto_alu":         _k_ext_moto_alu,
        "ext_moto_exhaust":     _k_ext_moto_exhaust_black,
        "ext_moto_leather":     _k_ext_moto_leather,
        "ext_moto_tire":        _k_ext_moto_tire,
        "ext_wet_soil":         _k_ext_wet_soil,
        "ext_cast_iron":        _k_ext_cast_iron,
        "ext_ceramic_brown":    _k_ext_ceramic_brown,
        "ext_bark_tropical":    _k_ext_bark_tropical,
        "ext_amber_lens":       _k_ext_amber_lens,
        "ext_red_lens":         _k_ext_red_lens,
    })


register_loc01_kinds()

_PASSTHROUGH_MATS = (
    "Glass", "FrostedGlass", "DeadTube", "LiveTube", "WarmGlow",
    "CrimsonGlow", "WindowDarkR", "WindowLitB", "TropicalLeaf", "TropicalShrub",
    "Sign_CampusMap",
)

_PREV_GET_MAT = H.get_mat


def _loc01_get_mat(kind: str):
    if kind in _PASSTHROUGH_MATS:
        return C.get_material(kind)
    return _PREV_GET_MAT(kind)


H.get_mat = _loc01_get_mat


def export_loc01_asset(
    asset_name: str,
    build_fn,
    atlas_specs: dict,
    make_lods: bool = True,
    separate_parts: bool = False,
    cam_dir=(1.45, -1.65, 0.72),
    render: bool = True,
    ao_samples: int = 10,
):
    H.SRC_DIR = SRC_DIR
    H.RENDER_DIR = RENDER_DIR
    H.PRINT_DIR = PRINT_DIR
    return H.export_hero_asset(
        asset_name,
        build_fn,
        atlas_specs,
        make_lods=make_lods,
        separate_parts=separate_parts,
        cam_dir=cam_dir,
        render=render,
        ao_samples=ao_samples,
        passthrough_kinds=_PASSTHROUGH_MATS,
    )
