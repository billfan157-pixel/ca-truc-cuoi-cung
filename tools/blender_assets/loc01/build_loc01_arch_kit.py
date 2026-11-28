"""LOC01 Architectural Kit A — Block K-A Facade, Entry Portico Canopy, Gate Arch & Pillars,
Wrought-Iron Side Gate, Hero Mailbox, Perimeter Fence Bay & Security Guardhouse.

All 9 assets are modeled procedurally in Blender 4.5 LTS with true architectural relief,
Cycles multi-pass PBR baking (B 2005 operational <-> R 2012 monsoon-weathered), LOD0-2 FBX,
and editable .blend sources saved to ArtSource/Blender/LOC01/.

Coordinate convention (matching PlaceBlenderModel yawDeg=0):
  Blender +X = Unity +X (East)
  Blender +Y = Unity +Z (North, into hospital)
  Blender -Y = Unity -Z (South, facing approaching player at AP00)
  Blender +Z = Unity +Y (Up)
"""
from __future__ import annotations

import math
import sys
from pathlib import Path

import bmesh
import bpy

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))
sys.path.insert(0, str(HERE.parent / "loc02"))

import hero_lib as H  # noqa: E402
import loc01_lib as L  # noqa: E402


def _add_scroll_ring(objs, prefix, cx, cy, cz, rx, rz, wire_r=0.007, kind="ext_iron_wrought", segs=18):
    """Forged wrought-iron decorative ring / oval scroll between vertical pickets."""
    pts = [
        (cx + math.cos(k * math.tau / segs) * rx, cy, cz + math.sin(k * math.tau / segs) * rz)
        for k in range(segs)
    ]
    objs.append(H.pipe(prefix, pts, wire_r, kind, res=3, cyclic=True))


# --------------------------------------------------------------------------------------
# 1. Block K-A 1987 4x4m Modular Facade Bay (CTCC_ENV_MainFacadeBay_1987)
# --------------------------------------------------------------------------------------

def build_main_facade_bay_1987():
    """Authentic 1987 Vietnamese hospital 4.0 x 4.0 m concrete-frame facade bay.
    Replaces Dalmatian cow-print concrete bands with fine washed-stone cement plaster,
    deep cantilevered ô văng sunshades, angled brise-soleil louvers, recessed iron hoa sắt
    grilles, sloped concrete drip sills & R/B paired WindowDarkR glazing."""
    objs = []
    W, H_BAY = 4.00, 4.00
    # Outer wall envelope framing window opening x in [-1.22..1.22], z in [0.96..2.84]
    # Bottom stucco spandrel (0.32..0.96) + top stucco header (2.84..3.66) + left/right piers
    objs.append(H.box("StuccoBot", (0.0, 0.08, 0.64), (3.52, 0.24, 0.64), "ext_ochre_stucco", bevel=0.006))
    objs.append(H.box("StuccoTop", (0.0, 0.08, 3.25), (3.52, 0.24, 0.82), "ext_ochre_stucco", bevel=0.006))
    for sx in (-1.52, 1.52):
        objs.append(H.box(f"StuccoPier_{sx:+.2f}", (sx, 0.08, 1.90), (0.60, 0.24, 1.88), "ext_ochre_stucco", bevel=0.006))

    # 1987 RC Structural Frame (fine washed concrete pilasters, slab beam, drip sill band)
    for sx in (-1.88, 1.88):
        objs.append(H.box(f"RCPilaster_{sx:+.2f}", (sx, 0.04, H_BAY * 0.5), (0.26, 0.34, H_BAY), "ext_concrete_washed", bevel=0.010))
        # Subtle recessed vertical architectural shadow flute down pilaster face
        objs.append(H.box(f"PilasterTrim_{sx:+.2f}", (sx, -0.132, H_BAY * 0.5), (0.10, 0.018, H_BAY - 0.58), "ext_concrete_washed", bevel=0.004))
    objs.append(H.box("RCBeamTop", (0.0, 0.03, 3.84), (W, 0.36, 0.32), "ext_concrete_washed", bevel=0.010))
    objs.append(H.box("RCDripLipTop", (0.0, -0.155, 3.685), (W, 0.055, 0.036), "ext_concrete_washed", bevel=0.005))
    objs.append(H.box("RCPlinthBot", (0.0, 0.04, 0.16), (W, 0.32, 0.32), "ext_concrete_washed", bevel=0.008))
    # Horizontal subtle plaster molding under window sill
    objs.append(H.box("DadoBand", (0.0, -0.05, 0.88), (3.50, 0.035, 0.065), "ext_concrete_washed", bevel=0.004))

    # Deep Cantilevered Concrete Rain Hood (ô văng) + Side Cheeks + 3 Brise-Soleil Louvers
    objs.append(H.box("OvangRoof", (0.0, -0.24, 2.93), (2.68, 0.48, 0.10), "ext_concrete_washed", bevel=0.009))
    objs.append(H.box("OvangDrip", (0.0, -0.47, 2.885), (2.70, 0.04, 0.04), "ext_concrete_washed", bevel=0.004))
    for cx in (-1.30, 1.30):
        objs.append(H.box(f"OvangCheek_{cx:+.2f}", (cx, -0.22, 2.52), (0.08, 0.42, 0.82), "ext_concrete_washed", bevel=0.007))
    # 3 angled brise-soleil concrete/iron sunshade blades (lam chắn nắng)
    for idx, lz in enumerate((2.24, 2.47, 2.70)):
        objs.append(H.box(f"SunLouver_{idx}", (0.0, -0.28, lz), (2.52, 0.16, 0.036), "ext_concrete_washed",
                          bevel=0.004, rot=(math.radians(16), 0, 0)))

    # Sloped Concrete Window Sill projecting -Y with underside drip groove
    objs.append(H.box("WinSill", (0.0, -0.08, 0.94), (2.60, 0.30, 0.085), "ext_concrete_washed", bevel=0.008))

    # Recessed 1987 Green-Painted Steel Window Frame, Transom, Mullions & Iron Hoa Sắt Grille
    wy = 0.05
    objs.append(H.box("WinFrameTop", (0.0, wy, 2.82), (2.44, 0.07, 0.05), "ext_iron_wrought", bevel=0.003))
    objs.append(H.box("WinFrameBot", (0.0, wy, 0.985), (2.44, 0.07, 0.045), "ext_iron_wrought", bevel=0.003))
    for sx in (-1.20, -0.40, 0.40, 1.20):
        objs.append(H.box(f"WinMullion_{sx:+.2f}", (sx, wy, 1.90), (0.042, 0.068, 1.84), "ext_iron_wrought", bevel=0.003))
    objs.append(H.box("WinTransom", (0.0, wy, 2.26), (2.42, 0.065, 0.046), "ext_iron_wrought", bevel=0.003))
    # Secondary thin pane muntins
    for sx in (-0.80, 0.0, 0.80):
        objs.append(H.box(f"WinMuntinV_{sx:+.2f}", (sx, wy, 1.62), (0.020, 0.032, 1.24), "ext_iron_wrought", bevel=0.002))
    for hz in (1.36, 1.78, 2.54):
        objs.append(H.box(f"WinMuntinH_{hz:.2f}", (0.0, wy, hz), (2.38, 0.028, 0.020), "ext_iron_wrought", bevel=0.002))
    # Security burglar grille bars (hoa sắt) in front of lower glazing
    for gx in (-1.00, -0.60, -0.20, 0.20, 0.60, 1.00):
        objs.append(H.cyl(f"GrilleV_{gx:+.2f}", (gx, wy - 0.04, 1.00), (gx, wy - 0.04, 2.24), 0.0065, "ext_iron_wrought", seg=8))
    for gz in (1.28, 1.62, 1.96):
        objs.append(H.cyl(f"GrilleH_{gz:.2f}", (-1.18, wy - 0.04, gz), (1.18, wy - 0.04, gz), 0.007, "ext_iron_wrought", seg=8))

    # Recessed Glazing Backing Plane (Passthrough WindowDarkR -> pairs with WindowLitB in Unity!)
    objs.append(H.box("WindowGlass", (0.0, wy + 0.055, 1.90), (2.38, 0.014, 1.80), "WindowDarkR", bevel=0.0))
    return objs


# --------------------------------------------------------------------------------------
# 2. Block K-A 1987 Stairwell Breeze-Block Bay (CTCC_ENV_StairwellBreezeBay_1987)
# --------------------------------------------------------------------------------------

def build_stairwell_breeze_bay_1987():
    """Iconic 1987 Vietnamese hospital stairwell perforated concrete gạch thông gió screen
    (4.0 x 4.0 m) with deep molded geometric blocks, concrete frame & R/B backing glazing."""
    objs = []
    W, H_BAY = 4.00, 4.00
    # Flanking structural stairwell piers & floor beams (fine washed concrete!)
    for sx in (-1.78, 1.78):
        objs.append(H.box(f"StairPier_{sx:+.2f}", (sx, 0.04, H_BAY * 0.5), (0.44, 0.36, H_BAY), "ext_concrete_washed", bevel=0.010))
        objs.append(H.box(f"StairStuccoStrip_{sx:+.2f}", (sx, -0.145, H_BAY * 0.5), (0.20, 0.016, H_BAY - 0.64), "ext_ochre_stucco", bevel=0.004))
    objs.append(H.box("StairBeamTop", (0.0, 0.03, 3.84), (W, 0.38, 0.32), "ext_concrete_washed", bevel=0.010))
    objs.append(H.box("StairBeamBot", (0.0, 0.03, 0.16), (W, 0.38, 0.32), "ext_concrete_washed", bevel=0.010))
    objs.append(H.box("StairDripLip", (0.0, -0.165, 3.685), (W, 0.05, 0.036), "ext_concrete_washed", bevel=0.004))

    # Deep 3D Perforated Concrete Breeze-Block Grid (7 cols x 8 rows across x in [-1.54..+1.54], z in [0.34..3.66])
    cols, rows = 7, 8
    x0, x1 = -1.54, 1.54
    z0, z1 = 0.34, 3.66
    cw = (x1 - x0) / cols
    ch = (z1 - z0) / rows
    depth = 0.20
    # Outer vertical & horizontal grid ribs
    for c in range(cols + 1):
        px = x0 + c * cw
        objs.append(H.box(f"BreezeV_{c}", (px, -0.02, (z0 + z1) * 0.5), (0.046, depth, z1 - z0 + 0.02), "ext_concrete_washed", bevel=0.004))
    for r in range(rows + 1):
        pz = z0 + r * ch
        objs.append(H.box(f"BreezeH_{r}", (0.0, -0.02, pz), (x1 - x0 + 0.02, depth, 0.046), "ext_concrete_washed", bevel=0.004))
    # Authentic Vietnamese 1987 molded diagonal diamond-flower inserts inside each block
    for r in range(rows):
        cz = z0 + (r + 0.5) * ch
        for c in range(cols):
            cx = x0 + (c + 0.5) * cw
            # 4 sloped petals forming a hollow diamond ventilation aperture
            d_off = cw * 0.22
            for dx, dz, ang in ((-d_off, d_off, 45), (d_off, d_off, -45), (-d_off, -d_off, -45), (d_off, -d_off, 45)):
                objs.append(H.box(f"BreezeDiag_{r}_{c}_{ang}_{dx:+.1f}", (cx + dx, -0.01, cz + dz),
                                  (cw * 0.52, depth - 0.03, 0.036), "ext_concrete_washed",
                                  bevel=0.003, rot=(0, math.radians(ang), 0)))

    # Translucent interior stairwell backing glass (paired R/B glow!)
    objs.append(H.box("StairGlassBack", (0.0, 0.14, (z0 + z1) * 0.5), (3.12, 0.016, 3.34), "WindowDarkR", bevel=0.0))
    return objs


# --------------------------------------------------------------------------------------
# 3. Block K-A Ground-Floor Reception Exterior Cladding & Portal (CTCC_LOC01_KAGroundFacade)
# --------------------------------------------------------------------------------------

def build_ka_ground_facade():
    """Hero 1987 exterior ground-floor cladding for LOC02 south wall (x in [-5.0..+5.0], z in [0..4.08])
    placed at (4.0, 0, -8.06) so world x in [-1.0..+9.0]!
    - Replaces flat indoor mint plaster with ochre limewash, red-clay brick plinth & RC pilasters.
    - Extends west spandrel apron (-6.65..-5.0, z=3.42..4.08) to SEAL the floating S1 stairwell gap!
    - Preserves exact ENTRY opening (x in [-0.90..+0.90], z in [0..2.42]) 100% unobstructed,
      framing it with a molded concrete portal, steel frosted transom grille, and twin glazed
      ironwood entrance doors pinned cleanly open flush against flanking doorstops!"""
    objs = []
    # Main ochre stucco wall panels flanking the ENTRY opening (x in [-5.0..-0.90] & [+0.90..+5.0])
    # Plus header above ENTRY (x in [-0.90..+0.90], z in [2.42..3.66])
    for side, x0, x1 in (("W", -5.00, -0.90), ("E", 0.90, 5.00)):
        cx, w = (x0 + x1) * 0.5, x1 - x0
        # Weathered red-clay brick plinth (z=0..0.72) + beveled washed-concrete water-table ledge
        objs.append(H.box(f"BrickPlinth_{side}", (cx, -0.02, 0.36), (w, 0.10, 0.72), "ext_brick_1987", bevel=0.006))
        objs.append(H.box(f"PlinthCap_{side}", (cx, -0.045, 0.75), (w + 0.02, 0.15, 0.075), "ext_concrete_washed", bevel=0.008))
        # Lower exterior heritage dado accent band (z=0.78..1.24) + aged ochre limewash above (z=1.24..3.64)
        objs.append(H.box(f"ExtDado_{side}", (cx, -0.015, 1.01), (w, 0.07, 0.46), "ext_dado_green", bevel=0.005))
        objs.append(H.box(f"ExtDadoTrim_{side}", (cx, -0.032, 1.255), (w, 0.095, 0.045), "ext_concrete_washed", bevel=0.005))
        objs.append(H.box(f"ExtStucco_{side}", (cx, -0.01, 2.46), (w, 0.065, 2.38), "ext_ochre_stucco", bevel=0.006))

    # Header panel above ENTRY doorway (z=3.02..3.64, leaving z=2.42..3.02 for frosted transom grille!)
    objs.append(H.box("EntryHeaderStucco", (0.0, -0.01, 3.34), (1.84, 0.07, 0.62), "ext_ochre_stucco", bevel=0.006))

    # Continuous Floor-2 Horizontal RC Spandrel Beam (z=3.62..4.08) spanning x in [-6.65..+5.05]
    # Completely SEALS the dark gap above LOC02 and above the S1 stairwell door (-6.65..-5.0)!
    objs.append(H.box("GroundSpandrelBeam", (-0.80, -0.06, 3.85), (11.72, 0.32, 0.46), "ext_concrete_washed", bevel=0.010))
    objs.append(H.box("GroundSpandrelDrip", (-0.80, -0.225, 3.635), (11.74, 0.05, 0.04), "ext_concrete_washed", bevel=0.004))
    # Seal back soffit over S1 stairwell head so zero sky leaks from any angle
    objs.append(H.box("S1HeadSeal", (-5.82, 0.16, 3.82), (1.68, 0.65, 0.46), "ext_concrete_washed", bevel=0.008))

    # 4 Vertical 1987 Structural RC Pilasters tying directly into K-A Floor 2 columns!
    for px in (-4.88, -2.12, 2.12, 4.88):
        objs.append(H.box(f"GroundPilaster_{px:+.2f}", (px, -0.06, 1.84), (0.30, 0.22, 3.68), "ext_concrete_washed", bevel=0.010))
        objs.append(H.box(f"GroundPilBase_{px:+.2f}", (px, -0.075, 0.36), (0.36, 0.25, 0.72), "ext_brick_1987", bevel=0.008))

    # Flanking Exterior Decorative Hospital Windows / Ventilation Louvers (blind recessed bays at x=±3.72)
    # Leaves generous clear wall pier at x in [-2.85..-2.25] for the An Sinh 1987 enamel crest plaque!
    for fx in (-3.72, 3.72):
        # Concrete hood + sill framing recessed exterior louver/window bay
        objs.append(H.box(f"FlankHood_{fx:+.2f}", (fx, -0.18, 2.74), (1.56, 0.34, 0.085), "ext_concrete_washed", bevel=0.008))
        objs.append(H.box(f"FlankSill_{fx:+.2f}", (fx, -0.09, 1.38), (1.54, 0.20, 0.075), "ext_concrete_washed", bevel=0.007))
        for cx in (fx - 0.74, fx + 0.74):
            objs.append(H.box(f"FlankCheek_{cx:+.2f}", (cx, -0.15, 2.44), (0.07, 0.28, 0.56), "ext_concrete_washed", bevel=0.006))
        # Steel outer frame + iron security bars + dark frosted exterior glass panel
        objs.append(H.box(f"FlankFrameT_{fx:+.2f}", (fx, -0.055, 2.68), (1.42, 0.06, 0.045), "ext_iron_wrought", bevel=0.003))
        objs.append(H.box(f"FlankFrameB_{fx:+.2f}", (fx, -0.055, 1.44), (1.42, 0.06, 0.045), "ext_iron_wrought", bevel=0.003))
        for mx in (-0.68, -0.23, 0.23, 0.68):
            objs.append(H.box(f"FlankMul_{fx:+.2f}_{mx:+.2f}", (fx + mx, -0.055, 2.06), (0.036, 0.058, 1.24), "ext_iron_wrought", bevel=0.002))
        for gz in (1.72, 2.06, 2.38):
            objs.append(H.cyl(f"FlankBar_{fx:+.2f}_{gz:.2f}", (fx - 0.68, -0.088, gz), (fx + 0.68, -0.088, gz), 0.0065, "ext_iron_wrought", seg=8))
        objs.append(H.box(f"FlankGlass_{fx:+.2f}", (fx, -0.025, 2.06), (1.36, 0.014, 1.20), "WindowDarkR", bevel=0.0))

    # Monumental Entrance Portal Surround around ENTRY (x in [-0.90..+0.90], z in [0..2.42])
    for sx in (-0.98, 0.98):
        objs.append(H.box(f"PortalJamb_{sx:+.2f}", (sx, -0.08, 1.54), (0.16, 0.26, 3.08), "ext_concrete_washed", bevel=0.010))
        objs.append(H.box(f"PortalTimberJamb_{sx:+.2f}", (sx * 0.925, 0.02, 1.21), (0.06, 0.24, 2.42), "ext_ironwood_wet", bevel=0.004))
    objs.append(H.box("PortalLintel", (0.0, -0.08, 3.06), (2.16, 0.28, 0.14), "ext_concrete_washed", bevel=0.010))
    objs.append(H.box("PortalTransomBar", (0.0, -0.04, 2.44), (1.92, 0.22, 0.075), "ext_ironwood_wet", bevel=0.005))
    # Overhead Steel-Mullioned Frosted Transom Grille above door (z=2.48..2.98)
    for tx in (-0.60, -0.20, 0.20, 0.60):
        objs.append(H.box(f"TransomBarV_{tx:+.2f}", (tx, -0.04, 2.73), (0.028, 0.055, 0.50), "ext_iron_wrought", bevel=0.002))
    objs.append(H.box("TransomBarH", (0.0, -0.04, 2.73), (1.80, 0.050, 0.028), "ext_iron_wrought", bevel=0.002))
    objs.append(H.box("TransomGlass", (0.0, -0.02, 2.73), (1.78, 0.012, 0.48), "FrostedGlass", bevel=0.0))

    # Twin Monumental Glazed Ironwood Entrance Doors PINNED WIDE OPEN against exterior flanking walls!
    # Left leaf hinges at (-0.91, -0.10), swings 172° outward flush along wall (x in [-1.81..-0.93], y ~ -0.15) -> ZERO doorway obstruction!
    # Right leaf hinges at (+0.91, -0.10), swings 172° outward flush along wall (x in [+0.93..+1.81], y ~ -0.15) -> ZERO doorway obstruction!
    for side, sgn in (("L", -1.0), ("R", 1.0)):
        cx = sgn * 1.38
        dy = -0.155
        # Solid hardwood outer stiles, bottom kick panel, mid rail, top rail + glazed upper sash
        objs.append(H.box(f"DoorStileIn_{side}", (cx - sgn * 0.41, dy, 1.20), (0.09, 0.048, 2.38), "ext_ironwood_wet", bevel=0.004))
        objs.append(H.box(f"DoorStileOut_{side}", (cx + sgn * 0.41, dy, 1.20), (0.09, 0.048, 2.38), "ext_ironwood_wet", bevel=0.004))
        objs.append(H.box(f"DoorKickPanel_{side}", (cx, dy, 0.46), (0.76, 0.036, 0.86), "ext_ironwood_wet", bevel=0.004))
        objs.append(H.box(f"DoorKickInset_{side}", (cx, dy - 0.018, 0.46), (0.64, 0.012, 0.68), "ext_ironwood_wet", bevel=0.004))
        objs.append(H.box(f"DoorKickPlate_{side}", (cx, dy - 0.026, 0.14), (0.78, 0.006, 0.24), "steel", bevel=0.002))
        objs.append(H.box(f"DoorMidRail_{side}", (cx, dy, 0.94), (0.76, 0.048, 0.11), "ext_ironwood_wet", bevel=0.004))
        objs.append(H.box(f"DoorTopRail_{side}", (cx, dy, 2.33), (0.76, 0.048, 0.11), "ext_ironwood_wet", bevel=0.004))
        # Muntins dividing upper glass pane (z=1.00..2.28)
        objs.append(H.box(f"DoorMuntinV_{side}", (cx, dy, 1.64), (0.028, 0.036, 1.28), "ext_ironwood_wet", bevel=0.002))
        for mz in (1.42, 1.86):
            objs.append(H.box(f"DoorMuntinH_{side}_{mz:.2f}", (cx, dy, mz), (0.74, 0.034, 0.026), "ext_ironwood_wet", bevel=0.002))
        objs.append(H.box(f"DoorGlass_{side}", (cx, dy, 1.64), (0.73, 0.008, 1.26), "Glass", bevel=0.0))
        # Vertical brushed brass pull handle & heavy hinges + floor rubber doorstop
        objs.append(H.pipe(f"DoorHandle_{side}", [
            (cx + sgn * 0.34, dy - 0.025, 1.26),
            (cx + sgn * 0.34, dy - 0.075, 1.24),
            (cx + sgn * 0.34, dy - 0.075, 0.94),
            (cx + sgn * 0.34, dy - 0.025, 0.92),
        ], 0.011, "brass", res=4))
        for hz in (0.35, 1.20, 2.08):
            objs.append(H.cyl(f"DoorHinge_{side}_{hz:.2f}", (sgn * 0.92, -0.11, hz - 0.05),
                              (sgn * 0.92, -0.11, hz + 0.05), 0.014, "brass", seg=10))
        objs.append(H.cyl(f"DoorStop_{side}", (cx + sgn * 0.30, -0.22, 0.0), (cx + sgn * 0.30, -0.22, 0.06), 0.026, "rubber", seg=12))

    # Authentic An Sinh 1987 Enamel Crest Plaque mounted cleanly on clear west flank wall pier (x=-2.58, zero overlap!)
    crest_plate = H.box("CanopyCrestBacking", (-2.58, -0.058, 1.84), (0.64, 0.024, 0.34), "ext_iron_wrought", bevel=0.004)
    objs.append(crest_plate)
    # Single clean front-facing quad so Smart UV fills the dedicated print atlas without wasting half on a hidden back face!
    crest_face = H.sculpted_grid(
        "CanopyCrestPrint", 2, 2,
        lambda u, v: (-2.58 + (u - 0.5) * 0.60, -0.074, 1.84 + (v - 0.5) * 0.30),
        "print:canopy_crest", thickness=0.0, print_uv=True,
    )

    # Worn granite/terrazzo entrance threshold sill flush at z=0.012
    objs.append(H.box("EntryThreshold", (0.0, 0.0, 0.010), (1.96, 0.34, 0.020), "ext_terrazzo_fine", bevel=0.006))
    return {"Shell": objs, "Prints": [crest_face]}


# --------------------------------------------------------------------------------------
# 4. Hero Entry Portico Canopy & Integrated Rain Drainage (CTCC_LOC01_EntryCanopy)
# --------------------------------------------------------------------------------------

def build_entry_canopy():
    """Sculpted 1987 Vietnamese hospital cantilevered Entry Portico Canopy.
    Replaces ALL legacy Unity primitive cubes (`Courtyard entry canopy`, `fascia`, `drip lip`,
    `columns`, `plinths`, `caps`, `BulkheadLamp`, `CH01CanopyDrain`)!
    Placed at (4.0, 0, -8.08) yaw 0 -> projects South to y = -3.02 (world z = -11.10)."""
    objs = []
    # 1. Sheltered Dry Concrete Portico Deck (x in [-2.42..+2.42], y in [-2.94..0.0], height 2.4cm w/ beveled ramp edges!)
    objs.append(H.box("PorticoDeckCore", (0.0, -1.44, 0.012), (4.68, 2.86, 0.024), "ext_concrete_deck", bevel=0.008))
    # Beveled wheelchair/stretcher transition curb edge along South, West & East edges
    objs.append(H.box("PorticoRampS", (0.0, -2.93, 0.009), (4.80, 0.14, 0.018), "ext_concrete_deck", bevel=0.007))
    for sx in (-2.38, 2.38):
        objs.append(H.box(f"PorticoRampSide_{sx:+.2f}", (sx, -1.44, 0.009), (0.14, 2.94, 0.018), "ext_concrete_deck", bevel=0.007))
    # Subtle inlaid brass/concrete expansion joint grooves across dry deck
    for gx in (-1.10, 1.10):
        objs.append(H.box(f"DeckJoint_{gx:+.1f}", (gx, -1.44, 0.0245), (0.016, 2.78, 0.004), "ext_iron_wrought", bevel=0.001))

    # 2. Two Sculpted Structural Portico Columns at x = ±2.12, y = -2.42 (exact match to world x=1.88 & 6.12, z=-10.50!)
    for side, cx in (("L", -2.12), ("R", 2.12)):
        cy = -2.42
        # Concrete base shoe + weathered red-clay brick pedestal plinth + beveled concrete water-table cap
        objs.append(H.box(f"ColFoot_{side}", (cx, cy, 0.055), (0.46, 0.46, 0.11), "ext_concrete_washed", bevel=0.008))
        objs.append(H.box(f"ColBrickPlinth_{side}", (cx, cy, 0.40), (0.40, 0.40, 0.62), "ext_brick_1987", bevel=0.006))
        objs.append(H.box(f"ColPlinthCap_{side}", (cx, cy, 0.74), (0.44, 0.44, 0.075), "ext_concrete_washed", bevel=0.009))
        # Chamfered ochre-stucco structural shaft with subtle recessed corner reveals
        objs.append(H.box(f"ColShaft_{side}", (cx, cy, 1.70), (0.29, 0.29, 1.88), "ext_ochre_stucco", bevel=0.012))
        for rx in (-0.07, 0.07):
            objs.append(H.box(f"ColFluteS_{side}_{rx:+.2f}", (cx + rx, cy - 0.146, 1.70), (0.025, 0.012, 1.64), "ext_concrete_washed", bevel=0.003))
        # Stepped 1987 modernist concrete capital + neck ring supporting slab
        objs.append(H.box(f"ColCapNeck_{side}", (cx, cy, 2.66), (0.35, 0.35, 0.065), "ext_concrete_washed", bevel=0.007))
        objs.append(H.box(f"ColCapTop_{side}", (cx, cy, 2.75), (0.43, 0.43, 0.11), "ext_concrete_washed", bevel=0.009))

    # 3. Sculpted Cantilevered Concrete Portico Slab (z = 2.78..3.18) with Coffered Soffit Beams!
    # Main upper deck slab (recessed slightly so underside beams protrude cleanly!)
    objs.append(H.box("CanopyMainSlab", (0.0, -1.49, 2.99), (4.68, 2.98, 0.22), "ext_concrete_washed", bevel=0.010))
    # Exposed coffered RC underside structural beams (perimeter ring + 2 inner cross ribs — ZERO flat spotted box!)
    objs.append(H.box("SoffitBeamS", (0.0, -2.88, 2.83), (4.68, 0.22, 0.14), "ext_concrete_washed", bevel=0.008))
    objs.append(H.box("SoffitBeamN", (0.0, -0.10, 2.83), (4.68, 0.22, 0.14), "ext_concrete_washed", bevel=0.008))
    for bx in (-2.12, -0.74, 0.74, 2.12):
        objs.append(H.box(f"SoffitRib_{bx:+.2f}", (bx, -1.49, 2.83), (0.20, 2.72, 0.12), "ext_concrete_washed", bevel=0.007))
    objs.append(H.box("SoffitMidBeam", (0.0, -1.49, 2.84), (4.24, 0.16, 0.10), "ext_concrete_washed", bevel=0.006))

    # Heritage Vietnamese Hospital Forest-Green Enamel Fascia Band wrapping Front (-Y) + West + East
    objs.append(H.box("FasciaFront", (0.0, -2.995, 2.98), (4.78, 0.065, 0.30), "ext_dado_green", bevel=0.006))
    objs.append(H.box("FasciaRevealFront", (0.0, -3.028, 2.98), (4.62, 0.018, 0.045), "ext_concrete_washed", bevel=0.003))
    for sx in (-2.385, 2.385):
        objs.append(H.box(f"FasciaSide_{sx:+.2f}", (sx, -1.50, 2.98), (0.065, 3.04, 0.30), "ext_dado_green", bevel=0.006))
    # Overhanging Molded Concrete Cornice Coping + Rain Drip Lip on top (z=3.14..3.22)
    objs.append(H.box("CanopyCornice", (0.0, -1.51, 3.165), (4.92, 3.12, 0.075), "ext_concrete_washed", bevel=0.009))
    objs.append(H.box("CanopyTopFlashing", (0.0, -1.51, 3.21), (4.94, 3.14, 0.022), "ext_galv_zinc", bevel=0.003))

    # Steel Structural Sign-Support Stanchions on top of canopy slab behind existing AN SINH sign (y = -2.92, z=3.22..3.88)
    for sx in (-1.05, 0.0, 1.05):
        objs.append(H.box(f"SignPost_{sx:+.2f}", (sx, -2.90, 3.55), (0.048, 0.048, 0.68), "ext_iron_wrought", bevel=0.003))
        objs.append(H.cyl(f"SignBrace_{sx:+.2f}", (sx, -2.88, 3.74), (sx, -2.35, 3.22), 0.018, "ext_iron_wrought", seg=8))

    # 4. Integrated Galvanized Rainwater Gutter, Collector Hopper & Downspout hugging East Column (+X = +2.12, y = -2.42)
    # Rear gutter along East parapet lip + collector hopper box + vertical galvanized downpipe + 3 wall collars + splash block!
    objs.append(H.box("GutterEast", (2.46, -1.52, 3.06), (0.11, 3.06, 0.09), "ext_galv_zinc", bevel=0.005))
    objs.append(H.box("RainHopper", (2.34, -2.62, 2.94), (0.20, 0.20, 0.22), "ext_galv_zinc", bevel=0.006))
    objs.append(H.pipe("DownspoutPipe", [
        (2.34, -2.62, 2.86),
        (2.26, -2.58, 2.66),
        (2.22, -2.58, 0.34),
        (2.34, -2.68, 0.14),
    ], 0.040, "ext_galv_zinc", res=5))
    for cz in (0.65, 1.48, 2.34):
        objs.append(H.cyl(f"PipeCollar_{cz:.2f}", (2.22, -2.58, cz), (2.22, -2.58, cz + 0.032), 0.048, "ext_iron_wrought", seg=12))
        objs.append(H.box(f"PipeStrap_{cz:.2f}", (2.17, -2.50, cz + 0.016), (0.10, 0.16, 0.032), "ext_iron_wrought", bevel=0.003))
    # Concrete rain splash block under downspout shoe
    objs.append(H.box("RainSplashBlock", (2.42, -2.76, 0.035), (0.34, 0.44, 0.07), "ext_concrete_washed", bevel=0.008))

    # 5. Sculpted Caged Institutional Portico Soffit Bulkhead Lamp (centered above entrance!)
    lx, ly, lz = 0.0, -0.68, 2.86
    objs.append(H.cyl("PorticoLampBase", (lx, ly, lz + 0.03), (lx, ly, lz - 0.02), 0.135, "ext_iron_wrought", seg=20))
    objs.append(H.sphere("PorticoLampGlobe", (lx, ly, lz - 0.055), (0.21, 0.28, 0.14), "WarmGlow", seg=20, rings=12))
    # Protective forged steel guard cage ribs over warm glass globe
    for ang_deg in (0, 60, 120):
        a = math.radians(ang_deg)
        pts = [
            (lx + math.cos(t) * 0.118 * math.cos(a),
             ly + math.cos(t) * 0.152 * math.sin(a),
             lz - 0.015 - math.sin(t) * 0.088)
            for t in [k * math.pi / 12.0 for k in range(13)]
        ]
        objs.append(H.pipe(f"PorticoCageRib_{ang_deg}", pts, 0.005, "ext_iron_wrought", res=3))
    # Conduit run along soffit feeding portico lamp from north wall
    objs.append(H.pipe("PorticoLampConduit", [(0.0, -0.02, 2.87), (0.0, -0.68, 2.87)], 0.011, "ext_galv_zinc", res=4))
    return objs


# --------------------------------------------------------------------------------------
# 5. Hero Side Gate Arch, Masonry Pillars & Forged Lanterns (CTCC_LOC01_GateArchAndPillars)
# --------------------------------------------------------------------------------------

def build_gate_arch_and_pillars():
    """Hero 1987 Vietnamese hospital masonry side-gate pillars, overhead transom architrave
    and twin forged-iron colonial/institutional pier lanterns!
    Placed at (4.0, 0, -20.0) yaw 0.
    LOCKED CLEARANCES:
      - Inner gate opening EXACTLY x in [-1.20..+1.20] (world 2.80..5.20, width 2.40m).
      - Pillar centers at x = ±1.43 (world 2.57 & 5.43), south face locked at y = -0.22
        (world z = -20.22) so MAILBOX (5.36, 1.18, -20.29) & NightGate plaque sit 100% flush!"""
    objs = []
    for side, px in (("W", -1.43), ("E", 1.43)):
        # Stepped concrete footing + red-clay weathered brick plinth (z=0..0.52) + beveled cap
        objs.append(H.box(f"GateFoot_{side}", (px, 0.0, 0.05), (0.56, 0.52, 0.10), "ext_concrete_washed", bevel=0.008))
        objs.append(H.box(f"GateBrickBase_{side}", (px, 0.0, 0.29), (0.50, 0.46, 0.40), "ext_brick_1987", bevel=0.006))
        objs.append(H.box(f"GateBaseCap_{side}", (px, 0.0, 0.515), (0.54, 0.50, 0.065), "ext_concrete_washed", bevel=0.008))

        # Main Ochre-Stucco Masonry Pier Shaft (0.46 x 0.44m -> exact y in [-0.22..+0.22], inner edge ±1.20!)
        objs.append(H.box(f"GatePier_{side}", (px, 0.0, 1.70), (0.46, 0.44, 2.32), "ext_ochre_stucco", bevel=0.010))
        # Subtle recessed vertical frame borders on East/West/North faces (keep South face flush at -0.22 for Mailbox/Plaque!)
        objs.append(H.box(f"PierAccentN_{side}", (px, 0.224, 1.72), (0.28, 0.016, 1.88), "ext_concrete_washed", bevel=0.004))
        objs.append(H.box(f"PierAccentOut_{side}", (px + (0.232 if side == "E" else -0.232), 0.0, 1.72),
                          (0.016, 0.26, 1.88), "ext_concrete_washed", bevel=0.004))
        # Subtle corner quoins / horizontal band near top of shaft
        objs.append(H.box(f"PierNeckBand_{side}", (px, 0.0, 2.74), (0.49, 0.47, 0.065), "ext_concrete_washed", bevel=0.006))
        # Stepped 1987 concrete pier capital (z=2.84..2.98)
        objs.append(H.box(f"PierCap_{side}", (px, 0.0, 2.91), (0.55, 0.53, 0.12), "ext_concrete_washed", bevel=0.010))

        # Sculpted Forged-Iron Heritage Hospital Gate Pillar Lantern atop coping (z=3.44..3.98!)
        # Replaces the terrible tiny floating yellow Unity cubes!
        lz = 3.43
        objs.append(H.box(f"LanternPlinth_{side}", (px, 0.0, lz + 0.03), (0.30, 0.30, 0.06), "ext_concrete_washed", bevel=0.006))
        objs.append(H.lathe(f"LanternPedestal_{side}", [
            (0.0, 0.0), (0.095, 0.0), (0.088, 0.03), (0.052, 0.06),
            (0.042, 0.11), (0.092, 0.14), (0.105, 0.16), (0.0, 0.16)
        ], (px, 0.0, lz + 0.06), "ext_iron_wrought", seg=20))
        # Glowing ribbed frosted lamp glass cylinder inside forged cage (WarmGlow passthrough!)
        objs.append(H.cyl(f"LanternGlobe_{side}", (px, 0.0, lz + 0.22), (px, 0.0, lz + 0.45), 0.072, "WarmGlow", seg=16, r2=0.086))
        # 4 Forged vertical lantern cage corner ribs + bottom/top rings
        for k in range(4):
            ang = math.radians(45 + k * 90)
            r0, r1 = 0.095, 0.116
            objs.append(H.cyl(f"LanternRib_{side}_{k}",
                              (px + math.cos(ang) * r0, math.sin(ang) * r0, lz + 0.21),
                              (px + math.cos(ang) * r1, math.sin(ang) * r1, lz + 0.46),
                              0.009, "ext_iron_wrought", seg=8))
        # Peaked tiered weather hood & cast finial spire
        objs.append(H.lathe(f"LanternHood_{side}", [
            (0.145, 0.0), (0.138, 0.025), (0.075, 0.095), (0.026, 0.125),
            (0.014, 0.165), (0.022, 0.195), (0.004, 0.245), (0.0, 0.245)
        ], (px, 0.0, lz + 0.455), "ext_iron_wrought", seg=8))

    # Overhead Concrete Hospital Gate Architrave / Lintel Beam (z=2.94..3.36, x in [-1.72..+1.72])
    # South face at y = -0.22 so Sign_HospitalGate at (4.0, 3.08, -20.235) mounts seamlessly!
    objs.append(H.box("GateLintelCore", (0.0, 0.0, 3.14), (3.44, 0.44, 0.38), "ext_ochre_stucco", bevel=0.010))
    objs.append(H.box("GateLintelSoffit", (0.0, 0.0, 2.945), (3.48, 0.48, 0.055), "ext_concrete_washed", bevel=0.006))
    # Subtle stepped cornice trim & rain-drip coping stone atop lintel (z=3.34..3.44)
    objs.append(H.box("GateLintelBand", (0.0, 0.0, 3.34), (3.54, 0.50, 0.055), "ext_dado_green", bevel=0.006))
    objs.append(H.box("GateLintelCoping", (0.0, 0.0, 3.40), (3.64, 0.56, 0.072), "ext_concrete_washed", bevel=0.009))
    # Subtle molded recessed plaque surround bars on South lintel face framing Sign_HospitalGate (w=1.68, h=0.30)
    objs.append(H.box("GateSignMouldT", (0.0, -0.224, 3.255), (1.76, 0.022, 0.028), "ext_concrete_washed", bevel=0.004))
    objs.append(H.box("GateSignMouldB", (0.0, -0.224, 2.975), (1.76, 0.022, 0.028), "ext_concrete_washed", bevel=0.004))
    return objs


# --------------------------------------------------------------------------------------
# 6. Hero Wrought-Iron Hospital Side Gate Leaf (CTCC_PROP_ExteriorGate)
# --------------------------------------------------------------------------------------

def build_exterior_gate_hero():
    """Cycles-baked sculpted Vietnamese 1987 wrought-iron hospital side gate leaf.
    Preserves EXACT pivot (hinge at X=0, Z=0), width 2.36m, frame height 2.48m, finials 2.62m!"""
    objs = []
    W, H_GATE = 2.36, 2.48
    # Heavy outer tubular rectangular steel stiles + 4 structural rails
    objs.append(H.box("GateHingeStile", (0.030, 0.0, H_GATE * 0.5), (0.060, 0.058, H_GATE), "ext_iron_wrought", bevel=0.004))
    objs.append(H.box("GateLatchStile", (W - 0.030, 0.0, H_GATE * 0.5), (0.060, 0.058, H_GATE), "ext_iron_wrought", bevel=0.004))
    for rz in (0.14, 0.88, 1.66, 1.94, H_GATE - 0.04):
        objs.append(H.box(f"GateRail_{rz:.2f}", (W * 0.5, 0.0, rz), (W - 0.04, 0.050, 0.046), "ext_iron_wrought", bevel=0.004))

    # Heavy forged barrel pintle hinges at X=0
    for hz in (0.42, 1.38, 2.22):
        objs.append(H.cyl(f"PintleBarrel_{hz:.2f}", (-0.016, 0.0, hz - 0.055), (-0.016, 0.0, hz + 0.055), 0.018, "ext_iron_wrought", seg=12))
        objs.append(H.box(f"PintleStrap_{hz:.2f}", (0.08, 0.0, hz), (0.16, 0.068, 0.044), "ext_iron_wrought", bevel=0.003))

    # Lower riveted steel kick panel (z=0.16..0.86) with 3 embossed recessed frame compartments & rivets
    objs.append(H.box("GateKickPlate", (W * 0.5, 0.0, 0.51), (W - 0.10, 0.018, 0.70), "ext_iron_wrought", bevel=0.002))
    for k in range(3):
        cx = 0.08 + (k + 0.5) * ((W - 0.16) / 3.0)
        objs.append(H.box(f"KickFrame_{k}", (cx, 0.0, 0.51), (0.64, 0.030, 0.54), "ext_iron_wrought", bevel=0.004))
        objs.append(H.box(f"KickInset_{k}", (cx, 0.0, 0.51), (0.56, 0.036, 0.44), "ext_dado_green", bevel=0.003))
    # Rivet studs along bottom & top kick seams
    for rx_i in range(14):
        rx = 0.12 + rx_i * ((W - 0.24) / 13.0)
        for rz in (0.185, 0.835):
            objs.append(H.cyl(f"Rivet_{rx_i}_{rz:.2f}", (rx, -0.022, rz), (rx, 0.022, rz), 0.006, "steel_dark", seg=8))

    # 15 Forged Vertical Wrought-Iron Pickets with cast collars & pointed spear finials (reaching 2.62m)
    for k in range(1, 16):
        px = k * (W / 16.0)
        objs.append(H.cyl(f"Picket_{k}", (px, 0.0, 0.88), (px, 0.0, H_GATE + 0.045), 0.0105, "ext_iron_wrought", seg=10))
        # Decorative collar rings below spear tip & mid-rail
        objs.append(H.cyl(f"CollarTop_{k}", (px, 0.0, H_GATE + 0.025), (px, 0.0, H_GATE + 0.052), 0.016, "ext_iron_wrought", seg=10))
        objs.append(H.cyl(f"FinialBase_{k}", (px, 0.0, H_GATE + 0.052), (px, 0.0, H_GATE + 0.140), 0.017, "ext_iron_wrought", seg=4, r2=0.0015))
        # Forged scroll ring between z=1.66..1.94 in each bay!
        if k < 16:
            cx_ring = px - (W / 32.0)
            _add_scroll_ring(objs, f"ScrollRing_{k}", cx_ring, 0.0, 1.80, 0.056, 0.108, wire_r=0.0065, kind="ext_iron_wrought", segs=14)
    _add_scroll_ring(objs, "ScrollRing_16", W - W / 32.0, 0.0, 1.80, 0.056, 0.108, wire_r=0.0065, kind="ext_iron_wrought", segs=14)

    # Heavy Welded Mortise Lockbox, Padlock Hasp, Brass Keyhole Escutcheon & Curved Lever Handles (z=1.06)
    objs.append(H.box("GateLockBox", (W - 0.145, 0.0, 1.06), (0.20, 0.068, 0.22), "ext_iron_wrought", bevel=0.004))
    objs.append(H.box("LockEscutcheonS", (W - 0.15, -0.036, 1.03), (0.045, 0.008, 0.11), "brass", bevel=0.002))
    objs.append(H.box("LockEscutcheonN", (W - 0.15, 0.036, 1.03), (0.045, 0.008, 0.11), "brass", bevel=0.002))
    # Forged curved door handles both sides
    for sy in (-1.0, 1.0):
        objs.append(H.pipe(f"GateHandle_{sy:+.0f}", [
            (W - 0.15, sy * 0.034, 1.09),
            (W - 0.15, sy * 0.075, 1.09),
            (W - 0.24, sy * 0.072, 1.08),
        ], 0.011, "steel", res=4))
    # Heavy sliding ground drop-bolt cane rod on latch stile
    objs.append(H.cyl("DropBoltRod", (W - 0.075, -0.034, 0.04), (W - 0.075, -0.034, 0.72), 0.009, "steel_dark", seg=10))
    objs.append(H.pipe("DropBoltHandle", [(W - 0.075, -0.034, 0.72), (W - 0.135, -0.034, 0.72)], 0.009, "steel_dark", res=3))
    for gz in (0.22, 0.56):
        objs.append(H.box(f"DropBoltGuide_{gz:.2f}", (W - 0.075, -0.032, gz), (0.038, 0.034, 0.028), "ext_iron_wrought", bevel=0.002))
    return objs


# --------------------------------------------------------------------------------------
# 7. Hero Weathered Iron Hospital Mailbox (CTCC_PROP_GateMailbox)
#    MUST PRESERVE separate_parts: MailboxBody, MailboxLid, MailboxPackageVisual!
# --------------------------------------------------------------------------------------

def build_gate_mailbox_hero():
    """Cycles-baked hero Vietnamese iron hospital mailbox (E10a / B01 hero close-up!).
    PRESERVES EXACT separate_parts names & dimensions (0.26 x 0.34 x 0.14 m, back y=+0.07,
    front door y=-0.07, lid right-hand hinge at x=+0.125) so MailboxLidPivot & atomic pickup work 100%!"""
    W, D, H_BOX = 0.26, 0.14, 0.34
    t = 0.010

    # ---- Part 1: MailboxBody (static wall-mounted housing) ----
    body = [
        # Back plate + flanged mounting ears w/ 4 mounting bolts
        H.box("MbBack", (0.0, D * 0.5 - t * 0.5, 0.0), (W, t, H_BOX), "ext_mailbox_green", bevel=0.002),
        H.box("MbMountBarT", (0.0, D * 0.5 - 0.004, H_BOX * 0.5 + 0.014), (W - 0.04, 0.008, 0.028), "ext_iron_wrought", bevel=0.002),
        H.box("MbMountBarB", (0.0, D * 0.5 - 0.004, -H_BOX * 0.5 - 0.014), (W - 0.04, 0.008, 0.028), "ext_iron_wrought", bevel=0.002),
        # Left, right, bottom, top interior shell
        H.box("MbSideL", (-W * 0.5 + t * 0.5, 0.0, 0.0), (t, D, H_BOX), "ext_mailbox_green", bevel=0.003),
        H.box("MbSideR", (W * 0.5 - t * 0.5, 0.0, 0.0), (t, D, H_BOX), "ext_mailbox_green", bevel=0.003),
        H.box("MbBot", (0.0, 0.0, -H_BOX * 0.5 + t * 0.5), (W, D, t), "ext_mailbox_green", bevel=0.003),
        H.box("MbTopInner", (0.0, 0.0, H_BOX * 0.5 - t * 0.5), (W, D, t), "ext_mailbox_green", bevel=0.002),
        # Sloped overhanging weatherproof rain hood with rolled drip lip
        H.box("MbRainHood", (0.0, -0.008, H_BOX * 0.5 + 0.014), (W + 0.036, D + 0.052, 0.014),
              "ext_mailbox_green", bevel=0.004, rot=(math.radians(-10.5), 0, 0)),
        H.box("MbHoodLip", (0.0, -D * 0.5 - 0.032, H_BOX * 0.5 - 0.004), (W + 0.036, 0.010, 0.024),
              "ext_mailbox_green", bevel=0.003),
        # Fixed barrel hinge knuckles on right side of body (x=+0.128)
        H.cyl("MbHingeBodyT", (W * 0.5 - 0.001, -D * 0.5, 0.095), (W * 0.5 - 0.001, -D * 0.5, 0.125), 0.0065, "steel_dark", seg=10),
        H.cyl("MbHingeBodyB", (W * 0.5 - 0.001, -D * 0.5, -0.125), (W * 0.5 - 0.001, -D * 0.5, -0.095), 0.0065, "steel_dark", seg=10),
    ]
    for bx in (-0.085, 0.085):
        for bz in (H_BOX * 0.5 + 0.014, -H_BOX * 0.5 - 0.014):
            body.append(H.cyl(f"MbBolt_{bx:+.2f}_{bz:+.2f}", (bx, D * 0.5 + 0.002, bz), (bx, D * 0.5 - 0.014, bz), 0.007, "steel_dark", seg=6))

    # ---- Part 2: MailboxLid (rotates cleanly around right edge x=+0.126 in Unity!) ----
    lid_y = -D * 0.5 - 0.005
    lid_panel = H.box("MbDoorPanel", (0.0, lid_y, -0.008), (W - 0.012, 0.012, H_BOX - 0.026), "ext_mailbox_green", bevel=0.003)
    # Crisp printed front stencil overlay ("HỘP THƯ · BỆNH VIỆN ĐA KHOA AN SINH")
    lid_print = H.box("MbDoorPrint", (0.0, lid_y - 0.0072, -0.008), (W - 0.022, 0.0025, H_BOX - 0.036), "print:mailbox_stencil", bevel=0.0)
    H.set_planar_print_uv(lid_print, axis="Y", flip_u=False, flip_v=False)
    lid = [
        lid_panel,
        lid_print,
        # Embossed upper mail-slot rain flap & recessed letter slot
        H.box("MbSlotFlap", (0.0, lid_y - 0.013, 0.092), (0.175, 0.014, 0.030),
              "ext_mailbox_green", bevel=0.003, rot=(math.radians(-14), 0, 0)),
        H.box("MbSlotRecess", (0.0, lid_y - 0.008, 0.082), (0.160, 0.008, 0.014), "ext_iron_wrought", bevel=0.001),
        # Detailed brass cylinder cam-lock + keyhole plate + pull tab on left edge (x=-0.092)
        H.box("MbLockPlate", (-0.092, lid_y - 0.011, -0.018), (0.030, 0.008, 0.046), "brass", bevel=0.002),
        H.cyl("MbLockCyl", (-0.092, lid_y - 0.011, -0.018), (-0.092, lid_y - 0.022, -0.018), 0.010, "brass", seg=14),
        H.box("MbKeySlot", (-0.092, lid_y - 0.023, -0.018), (0.0035, 0.004, 0.012), "ext_iron_wrought", bevel=0.0),
        H.box("MbPullTab", (-0.092, lid_y - 0.018, -0.056), (0.018, 0.018, 0.024), "steel", bevel=0.003),
        # Lid-side hinge pin matching body right edge
        H.cyl("MbHingeLidT", (W * 0.5 - 0.002, lid_y + 0.004, 0.068), (W * 0.5 - 0.002, lid_y + 0.004, 0.096), 0.006, "steel", seg=10),
        H.cyl("MbHingeLidB", (W * 0.5 - 0.002, lid_y + 0.004, -0.096), (W * 0.5 - 0.002, lid_y + 0.004, -0.068), 0.006, "steel", seg=10),
    ]

    # ---- Part 3: MailboxPackageVisual (Bảy's kraft envelope + exposed gate key inside!) ----
    pkg = [
        # Standing weathered kraft envelope leaning slightly inside mailbox
        H.box("PkgEnvelope", (-0.008, 0.012, -0.025), (0.168, 0.018, 0.228),
              "kraft", bevel=0.004, rot=(math.radians(8), math.radians(-4), math.radians(4))),
        # Red string-tie washers & folded white paper note tucked into envelope mouth
        H.cyl("PkgWasherA", (-0.008, 0.002, 0.025), (-0.008, -0.004, 0.025), 0.009, "pccc_red", seg=12),
        H.cyl("PkgWasherB", (-0.008, 0.004, -0.025), (-0.008, -0.002, -0.025), 0.009, "pccc_red", seg=12),
        H.box("PkgInnerPaper", (0.012, 0.005, 0.008), (0.140, 0.006, 0.195),
              "paper", bevel=0.001, rot=(math.radians(8), math.radians(2), math.radians(-3))),
        # Exposed steel Gate Key on split ring hooked cleanly over front of envelope (per Core v3.6.2!)
        H.pipe("PkgKeyRing", [
            (0.048 + math.cos(k * math.tau / 14) * 0.014, -0.014, 0.032 + math.sin(k * math.tau / 14) * 0.014)
            for k in range(14)
        ], 0.002, "steel", res=3, cyclic=True),
        H.cyl("PkgKeyBow", (0.048, -0.017, 0.008), (0.048, -0.011, 0.008), 0.013, "brass", seg=14),
        H.box("PkgKeyBlade", (0.048, -0.014, -0.030), (0.008, 0.004, 0.054), "brass", bevel=0.001),
        H.box("PkgKeyBit", (0.056, -0.014, -0.048), (0.012, 0.004, 0.015), "brass", bevel=0.001),
    ]
    return {"MailboxBody": body, "MailboxLid": lid, "MailboxPackageVisual": pkg}


# --------------------------------------------------------------------------------------
# 8. Hero Courtyard Perimeter Fence Bay (CTCC_ENV_CourtyardFenceBay)
# --------------------------------------------------------------------------------------

def build_courtyard_fence_bay_hero():
    """Cycles-baked 1987 Vietnamese hospital perimeter fence module (4.00 m long, pier at x=-1.82,
    height 2.28m) replacing cow-print terrazzo coping & flat railings with weathered red-clay brick,
    drip-grooved washed concrete coping, molded masonry pier & forged scrolled wrought-iron spears."""
    objs = []
    # 1. Weathered red-clay brick plinth wall (x in [-1.64..+2.00], z=0..0.90)
    objs.append(H.box("FenceBrickWall", (0.18, 0.0, 0.45), (3.64, 0.25, 0.90), "ext_brick_1987", bevel=0.006))
    objs.append(H.box("FenceBaseCurb", (0.18, 0.0, 0.06), (3.64, 0.30, 0.12), "ext_concrete_washed", bevel=0.007))
    # Chamfered washed-concrete coping cap with rain drip groove (ZERO cow-print terrazzo!)
    objs.append(H.box("FenceCoping", (0.18, 0.0, 0.945), (3.68, 0.33, 0.095), "ext_concrete_washed", bevel=0.009))

    # 2. Structural Masonry Fence Pier at left edge (x=-1.82, 0.38 x 0.38 x 2.28m)
    px = -1.82
    objs.append(H.box("FencePierFoot", (px, 0.0, 0.06), (0.46, 0.46, 0.12), "ext_concrete_washed", bevel=0.007))
    objs.append(H.box("FencePierBrick", (px, 0.0, 0.48), (0.40, 0.40, 0.76), "ext_brick_1987", bevel=0.006))
    objs.append(H.box("FencePierLedge", (px, 0.0, 0.90), (0.44, 0.44, 0.07), "ext_concrete_washed", bevel=0.008))
    objs.append(H.box("FencePierShaft", (px, 0.0, 1.56), (0.36, 0.36, 1.28), "ext_ochre_stucco", bevel=0.009))
    for sy in (-0.184, 0.184):
        objs.append(H.box(f"FencePierRecess_{sy:+.2f}", (px, sy, 1.56), (0.20, 0.014, 0.96), "ext_concrete_washed", bevel=0.003))
    objs.append(H.box("FencePierCapBot", (px, 0.0, 2.22), (0.44, 0.44, 0.075), "ext_concrete_washed", bevel=0.008))
    objs.append(H.box("FencePierCapTop", (px, 0.0, 2.28), (0.34, 0.34, 0.055), "ext_concrete_washed", bevel=0.010))

    # 3. Forged Wrought-Iron Spear Railing Section (x in [-1.64..+2.00], z=0.99..2.18)
    for rz in (1.10, 1.82, 2.02):
        objs.append(H.box(f"FenceRail_{rz:.2f}", (0.18, 0.0, rz), (3.64, 0.040, 0.036), "ext_iron_wrought", bevel=0.003))
    # Central iron sub-post for structural stiffness
    objs.append(H.box("FenceMidPost", (0.18, 0.0, 1.55), (0.044, 0.044, 1.12), "ext_iron_wrought", bevel=0.003))
    n_pickets = 18
    x_start, x_end = -1.52, 1.88
    step = (x_end - x_start) / (n_pickets - 1)
    for i in range(n_pickets):
        x = x_start + i * step
        objs.append(H.cyl(f"FPicket_{i}", (x, 0.0, 0.99), (x, 0.0, 2.10), 0.009, "ext_iron_wrought", seg=8))
        objs.append(H.cyl(f"FCollar_{i}", (x, 0.0, 2.06), (x, 0.0, 2.09), 0.014, "ext_iron_wrought", seg=8))
        objs.append(H.cyl(f"FSpear_{i}", (x, 0.0, 2.09), (x, 0.0, 2.20), 0.015, "ext_iron_wrought", seg=4, r2=0.0015))
        if i < n_pickets - 1:
            _add_scroll_ring(objs, f"FScroll_{i}", x + step * 0.5, 0.0, 1.92, step * 0.36, 0.072, wire_r=0.0055, kind="ext_iron_wrought", segs=12)
    return objs


# --------------------------------------------------------------------------------------
# 9. Hero 1987 Security Guardhouse w/ Rich Visible Interior (CTCC_ENV_Gatehouse)
# --------------------------------------------------------------------------------------

def build_gatehouse_hero():
    """Cycles-baked 1987 Vietnamese hospital Security Guardhouse (bốt bảo vệ, 2.84 x 2.54 m).
    Placed at (8.55, 0, -18.55) yaw 0:
      - West face (-X, x=-1.40) faces GATE-CONG & courtyard (primary visible window in EX-06 & EX-07!).
      - South face (-Y, y=-1.24) faces outer street & approach (visible through fence in EX-01 & EX-02!).
      - East (+X) & North (+Y) interior walls host rich guardroom storytelling visible right through the glass!"""
    objs = []
    W, D = 2.80, 2.48
    hw, hd = W * 0.5, D * 0.5  # 1.40, 1.24
    H_WALL = 2.54
    wt = 0.16

    # 1. Grounded Foundation Plinth & Interior Tiled Floor
    objs.append(H.box("GHBasePlinth", (0.0, 0.0, 0.07), (W + 0.16, D + 0.16, 0.14), "ext_concrete_washed", bevel=0.008))
    objs.append(H.box("GHFloorTile", (0.0, 0.0, 0.145), (W - 0.18, D - 0.18, 0.02), "ext_terrazzo_fine", bevel=0.002))

    # 2. Corner Structural Masonry Piers (4 corners)
    for sx in (-1, 1):
        for sy in (-1, 1):
            px, py = sx * (hw - 0.14), sy * (hd - 0.14)
            objs.append(H.box(f"GHPierBrick_{sx}_{sy}", (px, py, 0.60), (0.28, 0.28, 0.92), "ext_brick_1987", bevel=0.006))
            objs.append(H.box(f"GHPierTop_{sx}_{sy}", (px, py, 1.80), (0.27, 0.27, 1.48), "ext_ochre_stucco", bevel=0.008))

    # 3. Solid Back Walls — East (+X) & North (+Y) with red-brick exterior dado & ochre plaster
    # East wall (+X): solid masonry w/ service door outline
    objs.append(H.box("GHEastBrick", (hw - wt * 0.5, 0.0, 0.60), (wt, D - 0.24, 0.92), "ext_brick_1987", bevel=0.005))
    objs.append(H.box("GHEastUpper", (hw - wt * 0.5, 0.0, 1.80), (wt, D - 0.24, 1.48), "ext_ochre_stucco", bevel=0.005))
    # North wall (+Y): solid masonry
    objs.append(H.box("GHNorthBrick", (0.0, hd - wt * 0.5, 0.60), (W - 0.24, wt, 0.92), "ext_brick_1987", bevel=0.005))
    objs.append(H.box("GHNorthUpper", (0.0, hd - wt * 0.5, 1.80), (W - 0.24, wt, 1.48), "ext_ochre_stucco", bevel=0.005))
    # Interior sage-painted dado wainscoting on East & North interior walls
    objs.append(H.box("GHIntDadoE", (hw - wt - 0.01, 0.0, 0.64), (0.02, D - 0.34, 0.98), "ext_dado_green", bevel=0.002))
    objs.append(H.box("GHIntDadoN", (0.0, hd - wt - 0.01, 0.64), (W - 0.34, 0.02, 0.98), "ext_dado_green", bevel=0.002))

    # 4. Windowed Front Walls — West (-X, facing gate!) & South (-Y, facing street!)
    # Lower brick knee-walls (z=0.14..1.06) + sloped concrete drip sills + upper lintels (z=2.14..2.54)
    # West (-X)
    objs.append(H.box("GHWestKnee", (-hw + wt * 0.5, 0.0, 0.60), (wt, D - 0.52, 0.92), "ext_brick_1987", bevel=0.005))
    objs.append(H.box("GHWestSill", (-hw - 0.02, 0.0, 1.08), (0.28, D - 0.44, 0.075), "ext_concrete_washed", bevel=0.008))
    objs.append(H.box("GHWestLintel", (-hw + wt * 0.5, 0.0, 2.34), (wt + 0.02, D - 0.44, 0.40), "ext_ochre_stucco", bevel=0.006))
    # South (-Y)
    objs.append(H.box("GHSouthKnee", (0.0, -hd + wt * 0.5, 0.60), (W - 0.52, wt, 0.92), "ext_brick_1987", bevel=0.005))
    objs.append(H.box("GHSouthSill", (0.0, -hd - 0.02, 1.08), (W - 0.44, 0.28, 0.075), "ext_concrete_washed", bevel=0.008))
    objs.append(H.box("GHSouthLintel", (0.0, -hd + wt * 0.5, 2.34), (W - 0.44, wt + 0.02, 0.40), "ext_ochre_stucco", bevel=0.006))

    # Authentic Recessed Steel Sliding Window Frames, Security Burglar Bars & Glass (West & South!)
    # West Window (-X): y in [-0.96..+0.96], z in [1.12..2.14]
    wx = -hw + 0.07
    objs.append(H.box("GHW_FrameT", (wx, 0.0, 2.12), (0.06, 1.94, 0.042), "ext_iron_wrought", bevel=0.002))
    objs.append(H.box("GHW_FrameB", (wx, 0.0, 1.135), (0.06, 1.94, 0.036), "ext_iron_wrought", bevel=0.002))
    for wy_p in (-0.95, 0.0, 0.95):
        objs.append(H.box(f"GHW_FrameV_{wy_p:+.2f}", (wx, wy_p, 1.63), (0.056, 0.040, 1.00), "ext_iron_wrought", bevel=0.002))
    # Sliding pass-through sash frame slightly ajar on south half + thin security bars
    objs.append(H.box("GHW_SashMidH", (wx - 0.01, 0.0, 1.84), (0.036, 1.90, 0.030), "ext_iron_wrought", bevel=0.002))
    for bz in (1.36, 1.60):
        objs.append(H.cyl(f"GHW_Bar_{bz:.2f}", (wx - 0.036, -0.94, bz), (wx - 0.036, 0.94, bz), 0.006, "ext_iron_wrought", seg=8))
    objs.append(H.box("GHW_GlassN", (wx + 0.006, 0.46, 1.63), (0.008, 0.92, 0.96), "Glass", bevel=0.0))
    objs.append(H.box("GHW_GlassS", (wx - 0.010, -0.58, 1.63), (0.008, 0.72, 0.96), "Glass", bevel=0.0))

    # South Window (-Y): x in [-1.12..+1.12], z in [1.12..2.14]
    sy_w = -hd + 0.07
    objs.append(H.box("GHS_FrameT", (0.0, sy_w, 2.12), (2.26, 0.06, 0.042), "ext_iron_wrought", bevel=0.002))
    objs.append(H.box("GHS_FrameB", (0.0, sy_w, 1.135), (2.26, 0.06, 0.036), "ext_iron_wrought", bevel=0.002))
    for sx_p in (-1.11, -0.37, 0.37, 1.11):
        objs.append(H.box(f"GHS_FrameV_{sx_p:+.2f}", (sx_p, sy_w, 1.63), (0.040, 0.056, 1.00), "ext_iron_wrought", bevel=0.002))
    objs.append(H.box("GHS_Transom", (0.0, sy_w, 1.84), (2.22, 0.040, 0.030), "ext_iron_wrought", bevel=0.002))
    for bz in (1.36, 1.60):
        objs.append(H.cyl(f"GHS_Bar_{bz:.2f}", (-1.10, sy_w - 0.036, bz), (1.10, sy_w - 0.036, bz), 0.006, "ext_iron_wrought", seg=8))
    objs.append(H.box("GHS_Glass", (0.0, sy_w + 0.006, 1.63), (2.20, 0.008, 0.96), "Glass", bevel=0.0))

    # 5. Sculpted Overhanging Concrete Eave + Pitched Corrugated Zinc Roof + Rain Gutter (NO cow-print!)
    objs.append(H.box("GHConcreteEave", (-0.08, -0.06, 2.58), (W + 0.78, D + 0.74, 0.14), "ext_concrete_washed", bevel=0.009))
    objs.append(H.box("GHFasciaTrim", (-0.08, -0.06, 2.66), (W + 0.84, D + 0.80, 0.10), "ext_dado_green", bevel=0.006))
    # Gently pitched corrugated galvanized tin tropical heat-shield roof (mái tôn chống nóng) on steel purlin trusses
    roof_w, roof_d = W + 0.92, D + 0.88
    for sx_p in (-1.38, 0.0, 1.38):
        # Front (south) taller steel roof struts + rear (north) lower struts + purlin beam
        objs.append(H.box(f"GHStrutS_{sx_p:+.2f}", (sx_p, -1.25, 2.80), (0.048, 0.048, 0.24), "ext_iron_wrought", bevel=0.003))
        objs.append(H.box(f"GHStrutN_{sx_p:+.2f}", (sx_p, 1.15, 2.71), (0.048, 0.048, 0.08), "ext_iron_wrought", bevel=0.003))
    objs.append(H.box("GHPurlinS", (-0.08, -1.25, 2.91), (roof_w - 0.16, 0.055, 0.045), "ext_iron_wrought", bevel=0.003))
    objs.append(H.box("GHPurlinM", (-0.08, -0.06, 2.82), (roof_w - 0.16, 0.055, 0.045), "ext_iron_wrought", bevel=0.003))
    objs.append(H.box("GHPurlinN", (-0.08, 1.15, 2.73), (roof_w - 0.16, 0.055, 0.045), "ext_iron_wrought", bevel=0.003))
    objs.append(H.box("GHRoofDeck", (-0.08, -0.06, 2.85), (roof_w, roof_d, 0.048),
                      "ext_galv_zinc", bevel=0.004, rot=(math.radians(-4.5), 0, 0)))
    # Corrugated roof standing ribs running north-south
    for ri in range(13):
        rx = -0.08 - roof_w * 0.46 + ri * (roof_w * 0.92 / 12.0)
        objs.append(H.box(f"GHRib_{ri}", (rx, -0.06, 2.885), (0.038, roof_d - 0.04, 0.032),
                          "ext_galv_zinc", bevel=0.003, rot=(math.radians(-4.5), 0, 0)))
    # Galvanized gutter along north lower roof edge + downspout
    objs.append(H.box("GHGutterN", (-0.08, hd + 0.36, 2.68), (roof_w, 0.09, 0.08), "ext_galv_zinc", bevel=0.004))
    objs.append(H.pipe("GHDownspout", [(hw + 0.34, hd + 0.34, 2.66), (hw + 0.08, hd + 0.08, 2.42),
                                       (hw + 0.08, hd + 0.08, 0.20), (hw + 0.22, hd + 0.22, 0.08)], 0.032, "ext_galv_zinc", res=4))

    # 6. Exterior Caged Bulkhead Porch Lamp on West Upper Pier (-X, facing gate!)
    lx, ly, lz = -hw - 0.025, -0.82, 2.26
    objs.append(H.cyl("GHPorchBase", (lx + 0.02, ly, lz), (lx - 0.02, ly, lz), 0.105, "ext_iron_wrought", seg=18))
    objs.append(H.sphere("GHPorchGlobe", (lx - 0.052, ly, lz), (0.11, 0.16, 0.20), "WarmGlow", seg=16, rings=10))
    for ang_deg in (0, 60, 120):
        a = math.radians(ang_deg)
        pts = [
            (lx - 0.015 - math.sin(t) * 0.072,
             ly + math.cos(t) * 0.088 * math.cos(a),
             lz + math.cos(t) * 0.110 * math.sin(a))
            for t in [k * math.pi / 10.0 for k in range(11)]
        ]
        objs.append(H.pipe(f"GHPorchCage_{ang_deg}", pts, 0.004, "ext_iron_wrought", res=3))
    objs.append(H.pipe("GHPorchConduit", [(lx + 0.01, ly, lz + 0.10), (lx + 0.01, ly, 2.54)], 0.009, "ext_galv_zinc", res=3))

    # 7. RICH VISIBLE INTERIOR STORYTELLING (seen right through West & South windows in EX-06 & EX-07!)
    # Guard's ironwood duty desk against East/South interior corner (top at z=0.88)
    objs.append(H.box("GHDeskTop", (0.58, -0.42, 0.88), (1.16, 0.72, 0.042), "ext_ironwood_wet", bevel=0.005))
    objs.append(H.box("GHDeskApron", (0.58, -0.42, 0.78), (1.06, 0.62, 0.14), "ext_ironwood_wet", bevel=0.004))
    objs.append(H.box("GHDeskPedestal", (0.94, -0.42, 0.50), (0.36, 0.62, 0.68), "ext_ironwood_wet", bevel=0.004))
    for lx_p in (0.06, 0.96):
        for ly_p in (-0.72, -0.12):
            objs.append(H.box(f"GHDeskLeg_{lx_p:.1f}_{ly_p:.1f}", (lx_p, ly_p, 0.51), (0.048, 0.048, 0.72), "ext_ironwood_wet", bevel=0.003))
    # Wooden round guard stool beside desk
    objs.append(H.cyl("GHStoolSeat", (0.12, 0.22, 0.56), (0.12, 0.22, 0.60), 0.18, "ext_ironwood_wet", seg=18))
    for k in range(4):
        a = math.radians(45 + k * 90)
        objs.append(H.cyl(f"GHStoolLeg_{k}", (0.12 + math.cos(a) * 0.11, 0.22 + math.sin(a) * 0.11, 0.56),
                          (0.12 + math.cos(a) * 0.15, 0.22 + math.sin(a) * 0.15, 0.15), 0.016, "ext_ironwood_wet", seg=8))

    # Open Duty Ledger ("SỔ TRỰC BẢO VỆ") flat on desk facing West window!
    ledger_base = H.box("GHLedgerCover", (0.42, -0.38, 0.908), (0.38, 0.28, 0.014), "ext_ironwood_wet", bevel=0.002, rot=(0, 0, math.radians(-14)))
    ledger_page = H.box("GHLedgerPages", (0.42, -0.38, 0.917), (0.36, 0.26, 0.004), "print:guardhouse_interior", bevel=0.0, rot=(0, 0, math.radians(-14)))
    H.set_planar_print_uv(ledger_page, axis="Z", uv_rect=(0.0, 0.5, 0.5, 1.0))  # TL quadrant!
    objs.append(ledger_base)

    # Vintage Green-Enamel Banker / Guard Desk Lamp glowing warmly on desk!
    objs.append(H.cyl("GHLampBase", (0.86, -0.58, 0.902), (0.86, -0.58, 0.935), 0.075, "brass", seg=16))
    objs.append(H.pipe("GHLampNeck", [(0.86, -0.58, 0.93), (0.90, -0.60, 1.18), (0.76, -0.52, 1.28), (0.72, -0.48, 1.24)], 0.009, "brass", res=4))
    objs.append(H.lathe("GHLampShade", [(0.02, 0.0), (0.045, -0.02), (0.115, -0.095), (0.122, -0.102)],
                        (0.72, -0.48, 1.25), "enamel_lamp", seg=20, rot=(math.radians(16), math.radians(-12), 0)))
    objs.append(H.sphere("GHLampBulb", (0.71, -0.47, 1.19), (0.055, 0.055, 0.068), "WarmGlow", seg=12, rings=8))

    # Vietnamese Red Tin Thermos Flask & White Enamel Mug on desk corner
    objs.append(H.lathe("GHThermos", [(0, 0), (0.062, 0), (0.064, 0.27), (0.038, 0.33), (0.036, 0.36), (0, 0.36)],
                        (0.88, -0.22, 0.902), "thermos_red", seg=18))
    objs.append(H.cyl("GHMug", (0.72, -0.16, 0.902), (0.72, -0.16, 0.992), 0.042, "enamel_white", seg=16))

    # Back East Wall (+X interior, x = hw - wt - 0.02 = +1.22, facing West window camera EX-07!):
    ewx = hw - wt - 0.025
    # 1) Framed "NỘI QUY BẢO VỆ CỔNG ĐÊM" wall notice (y=-0.38, z=1.58)
    objs.append(H.box("GHNoticeFrame", (ewx, -0.38, 1.58), (0.026, 0.62, 0.72), "ext_ironwood_wet", bevel=0.004))
    notice_face = H.box("GHNoticePrint", (ewx - 0.015, -0.38, 1.58), (0.004, 0.56, 0.66), "print:guardhouse_interior", bevel=0.0)
    H.set_planar_print_uv(notice_face, axis="X", uv_rect=(0.5, 0.5, 1.0, 1.0), flip_u=True)  # TR quadrant!

    # 2) Mounted Key Cabinet ("TỦ CHÌA KHÓA BẢO VỆ") with real 3D hanging keys (y=+0.48, z=1.56)
    objs.append(H.box("GHKeyCabBox", (ewx - 0.04, 0.48, 1.56), (0.09, 0.54, 0.64), "ext_iron_wrought", bevel=0.005))
    key_hdr = H.box("GHKeyCabPrint", (ewx - 0.088, 0.48, 1.56), (0.004, 0.48, 0.56), "print:guardhouse_interior", bevel=0.0)
    H.set_planar_print_uv(key_hdr, axis="X", uv_rect=(0.5, 0.0, 1.0, 0.5), flip_u=True)  # BR quadrant!
    for ki in range(6):
        r, c = divmod(ki, 2)
        ky = 0.36 + c * 0.22
        kz = 1.62 - r * 0.14
        objs.append(H.cyl(f"GHKeyHook_{ki}", (ewx - 0.088, ky, kz), (ewx - 0.112, ky, kz), 0.004, "brass", seg=8))
        objs.append(H.box(f"GHKeyTag_{ki}", (ewx - 0.105, ky, kz - 0.036), (0.005, 0.022, 0.048), "brass", bevel=0.001))

    # 3) Back North Wall (+Y interior, y = hd - wt - 0.025 = +1.055): Hanging Nov 2005 Shift Calendar & Electrical Breaker Box!
    nwy = hd - wt - 0.025
    objs.append(H.box("GHCalBack", (-0.36, nwy, 1.62), (0.48, 0.016, 0.62), "ext_ironwood_wet", bevel=0.003))
    cal_face = H.box("GHCalPrint", (-0.36, nwy - 0.010, 1.62), (0.44, 0.004, 0.58), "print:guardhouse_interior", bevel=0.0)
    H.set_planar_print_uv(cal_face, axis="Y", uv_rect=(0.0, 0.0, 0.5, 0.5))  # BL quadrant!
    # Electrical breaker cabinet + vertical metal conduit on North wall
    objs.append(H.box("GHBreakerBox", (0.48, nwy - 0.05, 1.54), (0.34, 0.11, 0.44), "breaker_grey", bevel=0.006))
    objs.append(H.cyl("GHBreakerConduit", (0.48, nwy - 0.03, 1.76), (0.48, nwy - 0.03, 2.50), 0.012, "ext_galv_zinc", seg=8))
    return {"Shell": objs, "Prints": [ledger_page, notice_face, key_hdr, cal_face]}


def run_all():
    assets = [
        ("CTCC_ENV_MainFacadeBay_1987",      build_main_facade_bay_1987,     {"LOC01_FacadeBay":      {"res": 1024, "bake_r": True}}, True,  False, (1.1, -2.2, 0.45)),
        ("CTCC_ENV_StairwellBreezeBay_1987", build_stairwell_breeze_bay_1987,{"LOC01_BreezeBay":      {"res": 1024, "bake_r": True}}, True,  False, (1.0, -2.2, 0.45)),
        ("CTCC_LOC01_KAGroundFacade",        build_ka_ground_facade,         {
            "LOC01_KAGroundFacade": {"res": 2048, "bake_r": True, "parts": ["Shell"]},
            "LOC01_KAGroundPrints": {"res": 1024, "bake_r": True, "parts": ["Prints"]},
        }, True,  False, (1.2, -2.6, 0.55)),
        ("CTCC_LOC01_EntryCanopy",           build_entry_canopy,             {"LOC01_EntryCanopy":    {"res": 2048, "bake_r": True}}, True,  False, (1.5, -2.0, 0.85)),
        ("CTCC_LOC01_GateArchAndPillars",    build_gate_arch_and_pillars,    {"LOC01_GateArch":       {"res": 2048, "bake_r": True}}, True,  False, (1.3, -2.2, 0.65)),
        ("CTCC_PROP_ExteriorGate",           build_exterior_gate_hero,       {"LOC01_ExteriorGate":   {"res": 1024, "bake_r": True}}, True,  False, (1.2, -2.1, 0.45)),
        ("CTCC_PROP_GateMailbox",            build_gate_mailbox_hero,        {
            "LOC01_Mailbox":    {"res": 1024, "bake_r": True, "parts": ["MailboxBody", "MailboxPackageVisual"]},
            "LOC01_MailboxLid": {"res": 1024, "bake_r": True, "parts": ["MailboxLid"]},
        }, False, True,  (1.0, -1.8, 0.45)),
        ("CTCC_ENV_CourtyardFenceBay",       build_courtyard_fence_bay_hero, {"LOC01_FenceBay":       {"res": 1024, "bake_r": True}}, True,  False, (1.2, -2.2, 0.55)),
        ("CTCC_ENV_Gatehouse",               build_gatehouse_hero,           {
            "LOC01_Gatehouse":       {"res": 2048, "bake_r": True, "parts": ["Shell"]},
            "LOC01_GatehousePrints": {"res": 1024, "bake_r": True, "parts": ["Prints"]},
        }, True,  False, (-1.8, -1.6, 0.72)),
    ]
    only = set()
    if "--only" in sys.argv:
        idx = sys.argv.index("--only")
        only = set(sys.argv[idx + 1:])
    for name, fn, specs, lods, sep, cam in assets:
        if only and name not in only:
            continue
        print(f"=== BUILDING {name} ===", flush=True)
        L.export_loc01_asset(name, fn, specs, make_lods=lods, separate_parts=sep, cam_dir=cam, render=True, ao_samples=10)
    print("LOC01_ARCH_KIT_DONE", flush=True)


if __name__ == "__main__":
    run_all()
