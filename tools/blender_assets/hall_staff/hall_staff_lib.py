"""HALL & STAFF Hero Architectural & Nursing Kit production library (Blender 4.5 LTS).

Extends LOC02/LOC03's battle-tested Cycles PBR multi-pass baker (`loc02/hero_lib.py` &
`loc03/ward_lib.py`) with calibrated 1987-2012 Vietnamese district general hospital
materials for corridors, MEP systems, emergency doors, safety fixtures, and nursing
workstations (Reality B 2005 operational <-> Reality R 2012 abandoned).

Outputs:
  - Editable .blend sources  -> ArtSource/Blender/HALL_STAFF/<Asset>.blend
  - High-res print sheets    -> ArtSource/HALL_STAFF/prints/<sheet>.png
  - Game-ready FBX (+LOD0-2) -> Prototype/Assets/CTCC/Art/Meshes/<Asset>.fbx
  - Cycles PBR maps (B & R)  -> Prototype/Assets/CTCC/Art/Textures/<Atlas>{B,R}_{Color,Mask}.png + _Normal.png
  - B/R studio renders       -> output/hall-staff-hero/blender-renders/<Asset>_BR.png
"""
from __future__ import annotations

import sys
from pathlib import Path

import bpy

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))
sys.path.insert(0, str(HERE.parent / "loc02"))
sys.path.insert(0, str(HERE.parent / "loc03"))

import common as C  # noqa: E402
import hero_lib as H  # noqa: E402
import ward_lib as W  # noqa: E402

ROOT = C.ROOT
SRC_DIR = ROOT / "ArtSource" / "Blender" / "HALL_STAFF"
RENDER_DIR = ROOT / "output" / "hall-staff-hero" / "blender-renders"
PRINT_DIR = ROOT / "ArtSource" / "HALL_STAFF" / "prints"
SRC_DIR.mkdir(parents=True, exist_ok=True)
RENDER_DIR.mkdir(parents=True, exist_ok=True)
PRINT_DIR.mkdir(parents=True, exist_ok=True)

H.SRC_DIR = SRC_DIR
H.RENDER_DIR = RENDER_DIR
H.PRINT_DIR = PRINT_DIR


# --------------------------------------------------------------------------------------
# Calibrated 1987-2012 Vietnamese Hospital Corridor & Nursing Procedural PBR Shaders
# --------------------------------------------------------------------------------------

def _k_pccc_red(b, A, P, gv, rnd, edge, cav):
    """Vietnamese PCCC safety-red enameled steel cylinder & fire alarm bell.
    B: clean satin safety red; R: dusty darkened red with subtle chipped edges &
    brighter protected band near mounting strap (matches CH01_EXTINGUISHER observation!)."""
    return H._k_paint(
        b, A, P, gv, rnd, edge, cav,
        col_b=H.srgb(0.64, 0.10, 0.085),
        col_r=H.srgb(0.42, 0.095, 0.085),
        rough_b=0.24, rough_r=0.52,
        primer=H.srgb(0.56, 0.56, 0.54),
        rust=H.srgb(0.28, 0.13, 0.07),
        metal_sheet=True,
    )


def _k_er_door_sage(b, A, P, gv, rnd, edge, cav):
    """1987 Vietnamese hospital emergency swing-door institutional sage-green enamel.
    Calibrated luminous hospital sage in B so doors read crisply at north corridor ends;
    weathered muted sage in R under crimson ER lightbox (STRICTLY NO red paint splats!)."""
    return H._k_paint(
        b, A, P, gv, rnd, edge, cav,
        col_b=H.srgb(0.46, 0.62, 0.54),
        col_r=H.srgb(0.32, 0.44, 0.38),
        rough_b=0.34, rough_r=0.56,
        primer=H.srgb(0.54, 0.56, 0.52),
        rust=H.srgb(0.24, 0.16, 0.10),
        metal_sheet=True,
    )


def _k_er_kick_inox(b, A, P, gv, rnd, edge, cav):
    """Bright brushed satin stainless kickplate & crashbar finish for ER doors (reads cleanly under low north corridor bounce)."""
    base, rough, _, h0, _ = H._k_steel(b, A, P, gv, rnd, edge, cav, tone=H.srgb(0.86, 0.88, 0.87))
    return base, b.mixf(0.30, 0.50, A), b.mixf(0.46, 0.34, A), h0, 0.10


def _k_anodized_alu(b, A, P, gv, rnd, edge, cav):
    """Weathered extruded anodized aluminum for hanging lightboxes, signage frames & vent grilles."""
    base, rough, _, h0, _ = H._k_steel(b, A, P, gv, rnd, edge, cav, tone=H.srgb(0.76, 0.78, 0.76))
    return base, b.mixf(0.32, 0.54, A), b.mixf(0.58, 0.42, A), h0, 0.12


def _k_breaker_grey(b, A, P, gv, rnd, edge, cav):
    """1987 industrial grey-sage hammer-tone enameled steel electrical distribution panel."""
    return H._k_paint(
        b, A, P, gv, rnd, edge, cav,
        col_b=H.srgb(0.42, 0.47, 0.44),
        col_r=H.srgb(0.28, 0.32, 0.30),
        rough_b=0.38, rough_r=0.62,
        primer=H.srgb(0.46, 0.47, 0.44),
        rust=H.srgb(0.26, 0.15, 0.09),
        metal_sheet=True,
    )


def _k_pa_enamel_cream(b, A, P, gv, rnd, edge, cav):
    """Vintage warm-ivory cream enameled steel louver grille & electrical switch casing."""
    return H._k_paint(
        b, A, P, gv, rnd, edge, cav,
        col_b=H.srgb(0.80, 0.77, 0.66),
        col_r=H.srgb(0.58, 0.54, 0.44),
        rough_b=0.34, rough_r=0.60,
        primer=H.srgb(0.48, 0.46, 0.42),
        metal_sheet=True,
    )


def _k_cork_board(b, A, P, gv, rnd, edge, cav):
    """Granular warm cork / baize institutional notice pinboard surface."""
    g1 = b.voronoi(P, 95.0, "F1", 0.85, "Distance")
    g2 = b.noise(P, 180.0, 4, 0.65)
    col_b = b.mix(H.srgb(0.48, 0.35, 0.21), H.srgb(0.62, 0.46, 0.29), b.mapr(g1, 0.2, 0.8))
    col_r = b.mix(H.srgb(0.32, 0.24, 0.16), H.srgb(0.44, 0.33, 0.22), b.mapr(g1, 0.2, 0.8))
    base = b.mix(col_b, col_r, A)
    base = b.mix(base, H.srgb(0.14, 0.11, 0.08), b.math("MULTIPLY", cav, 0.38))
    return base, b.mixf(0.82, 0.92, A), 0.0, b.math("ADD", b.math("MULTIPLY", g1, 0.55), b.math("MULTIPLY", g2, 0.45)), 0.34


def _k_acoustic_cloth(b, A, P, gv, rnd, edge, cav):
    """Dark woven vintage loudspeaker acoustic grille cloth behind PA slats."""
    w1 = b.wave(P, 240.0, "BANDS", "X", profile="SIN")
    w2 = b.wave(P, 240.0, "BANDS", "Z", profile="SIN")
    weave = b.math("ADD", w1, w2)
    base = b.mix(H.srgb(0.14, 0.13, 0.11), H.srgb(0.19, 0.18, 0.15), A)
    base = b.mix(base, b.vmath("SCALE", base, 0.72, out="Vector"), b.math("MULTIPLY", b.mapr(weave, 0.4, 1.5), 0.4))
    return base, 0.92, 0.0, b.math("MULTIPLY", weave, 0.45), 0.28


def _k_rubber_ribbed(b, A, P, gv, rnd, edge, cav):
    """Heavy black vulcanized corridor stretcher crash bumper & ER door meeting gasket."""
    rib = b.wave(P, 85.0, "BANDS", "Z", profile="SIN")
    base = b.mix(H.srgb(0.085, 0.090, 0.088), H.srgb(0.145, 0.148, 0.140), A)
    scuff = b.mapr(b.noise(P, 14.0, 3, 0.6), 0.55, 0.76)
    base = b.mix(base, H.srgb(0.24, 0.24, 0.22), b.math("MULTIPLY", scuff, b.math("ADD", 0.12, b.math("MULTIPLY", A, 0.42))))
    base = b.mix(base, H.srgb(0.04, 0.04, 0.04), b.math("MULTIPLY", cav, 0.35))
    return base, b.mixf(0.56, 0.78, A), 0.0, b.math("MULTIPLY", rib, 0.45), 0.26


def _k_wood_worn_desk(b, A, P, gv, rnd, edge, cav):
    """Varnished Vietnamese ironwood nursing writing desktop with authentic wrist-wear polish streak
    on the right writing corner (Blender X > +0.08, matching CH01_NURSE_DESK!) and subtle aging in R."""
    base, rough, metal, h0, s0 = H._k_wood(b, A, P, gv, rnd, edge, cav, light=True)
    px = b.sep(P)[0]
    py = b.sep(P)[1]
    # Focused forearm/wrist wear band around X in [+0.14..+0.44], Y in [-0.16..-0.02]
    wx = b.math("MULTIPLY", b.mapr(px, 0.08, 0.22), b.mapr(px, 0.48, 0.36))
    wy = b.math("MULTIPLY", b.mapr(py, -0.18, -0.12), b.mapr(py, 0.02, -0.05))
    wear = b.math("MULTIPLY", wx, wy)
    base = b.mix(base, H.srgb(0.46, 0.31, 0.17), b.math("MULTIPLY", wear, 0.48))
    rough = b.mixf(rough, 0.24, b.math("MULTIPLY", wear, 0.65))
    return base, rough, metal, h0, s0


def _k_fan_blade_teal(b, A, P, gv, rnd, edge, cav):
    """Vintage Vietnamese Điện Cơ wall fan translucent sage-teal plastic blade."""
    base = b.mix(H.srgb(0.22, 0.44, 0.42), H.srgb(0.20, 0.33, 0.31), A)
    dust = b.mapr(b.noise(P, 18.0, 3, 0.6), 0.48, 0.72)
    base = b.mix(base, H.srgb(0.40, 0.40, 0.36), b.math("MULTIPLY", dust, b.math("MULTIPLY", A, 0.55)))
    return base, b.mixf(0.24, 0.58, A), 0.0, b.math("MULTIPLY", b.noise(P, 120.0, 2), 0.08), 0.08


def register_hall_staff_kinds():
    W.register_ward_kinds()
    H.KINDS.update({
        "pccc_red":        _k_pccc_red,
        "er_door_sage":    _k_er_door_sage,
        "er_kick_inox":    _k_er_kick_inox,
        "anodized_alu":    _k_anodized_alu,
        "breaker_grey":    _k_breaker_grey,
        "pa_cream":        _k_pa_enamel_cream,
        "cork_board":      _k_cork_board,
        "acoustic_cloth":  _k_acoustic_cloth,
        "rubber_ribbed":   _k_rubber_ribbed,
        "wood_worn_desk":  _k_wood_worn_desk,
        "fan_blade_teal":  _k_fan_blade_teal,
    })


register_hall_staff_kinds()

_PREV_GET_MAT = H.get_mat


def _hs_get_mat(kind: str):
    if kind == "CrimsonGlow":
        return C.get_material("CrimsonGlow")
    return _PREV_GET_MAT(kind)


H.get_mat = _hs_get_mat


def export_hs_asset(
    asset_name: str,
    build_fn,
    atlas_specs: dict,
    make_lods: bool = True,
    separate_parts: bool = False,
    cam_dir=(1.45, -1.55, 0.72),
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
        passthrough_kinds=(
            "Glass", "FrostedGlass", "DeadTube", "LiveTube", "WarmGlow",
            "CrimsonGlow", "SalineFluid", "ObsCard", "F101",
            "Doc_ShiftGuide", "Doc_WardOverflow", "Sign_EmergencyBox",
        ),
    )
