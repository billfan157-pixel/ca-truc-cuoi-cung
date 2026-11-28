"""Kit A (Part 1): Reusable Hospital Architectural Modular Kit for HALL & STAFF (Blender 4.5 LTS).

Produces Cycles-baked B (2005 operational) / R (2012 abandoned) PBR assets:
  1. CTCC_ENV_ColumnPilaster     - Upgraded chamfered RC structural pilaster w/ terrazzo plinth,
                                   bottle-green dado, hardwood dado rail, riveted satin-inox
                                   corner guards & concrete beam corbel capital.
  2. CTCC_HALL_CorridorBeams     - Complete 1987 HALL structural RC ceiling girders, perimeter cove
                                   cornices, South LOC02->HALL portal archway, brass floor threshold,
                                   East beam corbels (clear of Door 101!), & ceiling conduit spine.
  3. CTCC_STAFF_CorridorArch     - Complete 1987 STAFF structural RC beams, shallow pilasters (<=6cm,
                                   preserving >=1.72m Door-B escape lane!), South LOC02->STAFF portal,
                                   west-wall slim crash rail, & ceiling+wall conduit spine feeding
                                   both STAFF T8 fixtures & the fixed wall switch at z=6.66.
  4. CTCC_ENV_HandrailBay        - Upgraded 3.65m modular varnished ironwood hospital handrail +
                                   cast-steel bolted wall brackets + ribbed rubber stretcher crash rail.
  5. CTCC_ENV_HandrailBay_Short  - Matching 2.05m modular short handrail bay (for bays flanking doors).
  6. CTCC_ENV_ConduitRun         - Upgraded 4.0m modular wall MEP twin galvanized conduit + octagonal
                                   cast junction box + brass cover screws + saddle clamps.
"""
from __future__ import annotations

import math
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))
sys.path.insert(0, str(HERE.parent / "loc02"))
sys.path.insert(0, str(HERE.parent / "loc03"))

import hero_lib as H  # noqa: E402
import hall_staff_lib as HS  # noqa: E402


# --------------------------------------------------------------------------------------
# 1. CTCC_ENV_ColumnPilaster (Reusable across whole hospital! 0.26 x 0.36 x 3.60m)
#    Origin at bottom center (0, 0, 0). Corridor face is +X (yaw=0 on West wall, yaw=180 on East wall).
# --------------------------------------------------------------------------------------
def build_column_pilaster():
    objs = []
    w, d, h = 0.26, 0.36, 3.60

    # Cast terrazzo cove plinth base (0..0.11m)
    objs.append(H.box("PilPlinthBase", (0.0, 0.0, 0.048), (w + 0.026, d + 0.026, 0.096), "terrazzo_dark", bevel=0.008))
    objs.append(H.box("PilPlinthCap", (0.0, 0.0, 0.104), (w + 0.014, d + 0.014, 0.018), "terrazzo_dark", bevel=0.004))

    # Oil-painted bottle-green hospital dado lower shaft (0.11..1.14m)
    objs.append(H.box("PilDadoShaft", (0.0, 0.0, 0.625), (w, d, 1.03), "dado_pilaster", bevel=0.014, seg=3, grain="Z"))

    # Sculpted dark varnished ironwood dado chair-rail collar (1.14..1.19m)
    objs.append(H.box("PilDadoRailMain", (0.004, 0.0, 1.162), (w + 0.022, d + 0.026, 0.042), "wood", bevel=0.006, grain="Y"))
    objs.append(H.box("PilDadoRailBead", (0.006, 0.0, 1.186), (w + 0.012, d + 0.016, 0.012), "wood_light", bevel=0.003, grain="Y"))

    # Upper smooth-troweled limewash concrete shaft (1.19..3.22m)
    objs.append(H.box("PilUpperShaft", (0.0, 0.0, 2.205), (w, d, 2.03), "concrete_beam", bevel=0.014, seg=3, grain="Z"))

    # Stepped structural reinforced-concrete beam capital / corbel (3.22..3.60m)
    objs.append(H.box("PilCapNeck", (0.006, 0.0, 3.242), (w + 0.026, d + 0.030, 0.044), "paint_cream", bevel=0.006))
    objs.append(H.box("PilCapBlock", (0.012, 0.0, 3.432), (w + 0.044, d + 0.044, 0.336), "concrete_beam", bevel=0.010, seg=2))

    # Brushed satin-inox L-section stretcher corner protection guards on corridor-facing +X corners (0.12..1.36m)
    for sy in (-1, 1):
        cx = w * 0.5 - 0.004
        cy = sy * (d * 0.5 - 0.004)
        objs.append(H.box(f"CornerGuardFront_{sy}", (cx + 0.007, cy - sy * 0.024, 0.74), (0.006, 0.056, 1.24), "satin_inox", bevel=0.0015))
        objs.append(H.box(f"CornerGuardSide_{sy}", (cx - 0.024, cy + sy * 0.007, 0.74), (0.056, 0.006, 1.24), "satin_inox", bevel=0.0015))
        # Dome mounting rivets along each guard wing
        for rz in (0.24, 0.56, 0.90, 1.24):
            objs.append(H.cyl(f"RivetF_{sy}_{rz:.2f}", (cx + 0.009, cy - sy * 0.032, rz), (cx + 0.013, cy - sy * 0.032, rz), 0.0042, "steel_dark", seg=8))
            objs.append(H.cyl(f"RivetS_{sy}_{rz:.2f}", (cx - 0.032, cy + sy * 0.009, rz), (cx - 0.032, cy + sy * 0.013, rz), 0.0042, "steel_dark", seg=8))

    return objs


# --------------------------------------------------------------------------------------
# 2. CTCC_HALL_CorridorBeams (HALL architectural ceiling shell, portals & conduit spine)
#    Origin at HALL center world (4.0, 0.0, 8.0), yaw=0:
#    local X in [-1.50..+1.50] (world x=[2.50..5.50]), local Y in [-8.0..+8.0] (world z=[0..16]), Z in [0..3.60]!
# --------------------------------------------------------------------------------------
def build_hall_corridor_beams():
    objs = []

    # 1. South Portal Archway framing LOC02 -> HALL transition (local Y = -7.90 -> world z = 0.10)
    #    Twin chamfered portal jambs (0.14m protrusion, keeping 2.72m wide open entry!) + deep overhead portal girder
    for sx in (-1, 1):
        px = sx * 1.43
        objs.append(H.box(f"SouthPortalPlinth_{sx}", (px, -7.90, 0.050), (0.16, 0.28, 0.100), "terrazzo_dark", bevel=0.007))
        objs.append(H.box(f"SouthPortalDado_{sx}", (px, -7.90, 0.625), (0.14, 0.25, 1.05), "dado_pilaster", bevel=0.010, seg=2))
        objs.append(H.box(f"SouthPortalRail_{sx}", (px, -7.90, 1.162), (0.16, 0.27, 0.036), "wood", bevel=0.005))
        objs.append(H.box(f"SouthPortalUpper_{sx}", (px, -7.90, 2.18), (0.14, 0.25, 2.00), "concrete_beam", bevel=0.010, seg=2))
        objs.append(H.box(f"SouthPortalGuard_{sx}", (px - sx * 0.068, -7.90, 0.72), (0.008, 0.256, 1.18), "satin_inox", bevel=0.002))
    objs.append(H.box("SouthPortalHeader", (0.0, -7.90, 3.39), (2.98, 0.28, 0.42), "concrete_beam", bevel=0.012, seg=2))
    objs.append(H.box("SouthPortalSoffitTrim", (0.0, -7.90, 3.175), (2.94, 0.30, 0.028), "paint_cream", bevel=0.004))
    # Flush inlaid brass terrazzo transition strip at z=0.00
    objs.append(H.box("SouthBrassThreshold", (0.0, -7.98, 0.003), (2.92, 0.058, 0.006), "brass", bevel=0.0015))

    # 2. North End Structural Header & Corner Frames framing Door A (local Y = +7.90 -> world z = 15.90)
    objs.append(H.box("NorthEndHeader", (0.0, 7.90, 3.44), (2.98, 0.22, 0.32), "concrete_beam", bevel=0.010, seg=2))
    objs.append(H.box("NorthEndSoffit", (0.0, 7.90, 3.275), (2.94, 0.24, 0.024), "paint_cream", bevel=0.004))

    # 3. Four Primary Transverse RC Structural Girders at z = 1.00, 5.25, 9.50, 13.75 (Y = -7.0, -2.75, +1.50, +5.75)
    girder_ys = (-7.00, -2.75, 1.50, 5.75)
    for gi, gy in enumerate(girder_ys):
        objs.append(H.box(f"TransGirder_{gi}", (0.0, gy, 3.42), (2.96, 0.30, 0.36), "concrete_beam", bevel=0.012, seg=2))
        objs.append(H.box(f"TransGirderTrim_{gi}", (0.0, gy, 3.236), (2.92, 0.32, 0.024), "paint_cream", bevel=0.004))
    # East wall concrete corbel bracket under Girder #1 (z=5.25 -> Y=-2.75) clearing Door 101 frame (top 2.88m) & plaque (3.08m)!
    objs.append(H.box("EastDoor101Corbel", (1.42, -2.75, 3.30), (0.16, 0.34, 0.20), "concrete_beam", bevel=0.008))

    # 4. Continuous Longitudinal Perimeter Ceiling Cove Cornices (stepped 8mm clear of girders!)
    bay_spans = [(-7.75, -7.16), (-6.84, -2.91), (-2.59, 1.34), (1.66, 5.59), (5.91, 7.78)]
    for sx, side in ((-1.425, "W"), (1.425, "E")):
        for bi, (y0, y1) in enumerate(bay_spans):
            yc = (y0 + y1) * 0.5
            yl = y1 - y0
            objs.append(H.box(f"Cornice_{side}_{bi}", (sx, yc, 3.515), (0.15, yl, 0.17), "concrete_beam", bevel=0.010, seg=2))
            objs.append(H.box(f"CorniceBead_{side}_{bi}", (sx - (0.072 if sx > 0 else -0.072), yc, 3.435),
                              (0.022, yl, 0.022), "paint_cream", bevel=0.003))

    # 5. Continuous Galvanized Overhead Ceiling Conduit Spine offset neatly to X=+0.26m beside T8 axis
    #    (avoids head-on vertical-stem silhouette directly above CẤP CỨU lightbox!), stepping tightly against each girder soffit!
    cx = 0.26
    spine_pts = [
        (cx, -7.75, 3.572),
        (cx, -7.17, 3.572), (cx, -7.155, 3.224), (cx, -6.845, 3.224), (cx, -6.83, 3.572),
        (cx, -2.92, 3.572), (cx, -2.905, 3.224), (cx, -2.595, 3.224), (cx, -2.58, 3.572),
        (cx,  1.33, 3.572), (cx,  1.345, 3.224), (cx,  1.655, 3.224), (cx,  1.67, 3.572),
        (cx,  5.58, 3.572), (cx,  5.595, 3.224), (cx,  5.905, 3.224), (cx,  5.92, 3.572),
        (cx,  7.78, 3.572),
    ]
    objs.append(H.pipe("HallCeilingConduitSpine", spine_pts, 0.011, "galv_conduit", res=4))
    # Girder soffit saddle clips fastening the stepped conduit cleanly onto each RC girder
    for gy in girder_ys:
        objs.append(H.box(f"GirderPipeClip_{gy:.2f}", (cx, gy, 3.226), (0.044, 0.022, 0.024), "satin_inox", bevel=0.002))

    # Cast octagonal electrical junction boxes + conduit saddle clamps along ceiling bays + short T8 fixture feed taps
    for jy in (-5.00, -3.25, -0.60, 2.60, 4.10, 6.85):
        objs.append(H.cyl(f"CeilJBox_{jy:.2f}", (cx, jy, 3.545), (cx, jy, 3.595), 0.046, "breaker_grey", seg=8, bevel=0.004))
        objs.append(H.cyl(f"CeilJCap_{jy:.2f}", (cx, jy, 3.540), (cx, jy, 3.548), 0.050, "galv_conduit", seg=8))
    for ty in (-3.25, 2.60):
        objs.append(H.pipe(f"TubeFeedTap_{ty:.2f}", [(cx, ty, 3.572), (0.0, ty, 3.572)], 0.009, "galv_conduit", res=3))
    for cy in (-6.0, -4.1, -1.7, 0.4, 3.4, 4.9, 7.2):
        objs.append(H.box(f"CeilClamp_{cy:.1f}", (cx, cy, 3.575), (0.048, 0.018, 0.026), "satin_inox", bevel=0.002))

    # Cross-branch galvanized conduit from ceiling spine into East wall conduit line at Y=-5.0 (z=3.0) & Y=+4.1 (z=12.1)
    for by in (-5.00, 4.10):
        objs.append(H.pipe(f"HallEastTap_{by:.1f}", [
            (cx + 0.03, by, 3.572), (1.32, by, 3.572), (1.36, by, 3.42), (1.47, by, 3.38), (1.47, by, 3.16)
        ], 0.0095, "galv_conduit", res=4))

    return objs


# --------------------------------------------------------------------------------------
# 3. CTCC_STAFF_CorridorArch (STAFF structural beams, shallow pilasters, portal & MEP)
#    Origin at STAFF center world (0.05, 0.0, 8.0), yaw=0:
#    local X in [-1.05..+1.05] (world x=[-1.00..+1.10]), local Y in [-8.0..+8.0] (world z=[0..16]), Z in [0..3.60]!
#    Strictly preserves >=1.95m clear width (and >=1.72m beside Nursing Desk) for Door-B escape!
# --------------------------------------------------------------------------------------
def build_staff_corridor_arch():
    objs = []

    # 1. South Portal Archway framing LOC02 -> STAFF (local Y = -7.90 -> world z = 0.10)
    #    Clears existing 'Staff' enamel plaque at (0, 2.67, 0.29) cleanly!
    for sx in (-1, 1):
        px = sx * 0.995
        objs.append(H.box(f"StaffPortalPlinth_{sx}", (px, -7.90, 0.048), (0.12, 0.24, 0.096), "terrazzo_dark", bevel=0.006))
        objs.append(H.box(f"StaffPortalDado_{sx}", (px, -7.90, 0.625), (0.105, 0.22, 1.05), "dado_pilaster", bevel=0.008, seg=2))
        objs.append(H.box(f"StaffPortalRail_{sx}", (px, -7.90, 1.162), (0.12, 0.24, 0.032), "wood", bevel=0.004))
        objs.append(H.box(f"StaffPortalUpper_{sx}", (px, -7.90, 2.22), (0.105, 0.22, 2.08), "concrete_beam", bevel=0.008, seg=2))
    objs.append(H.box("StaffPortalHeader", (0.0, -7.90, 3.43), (2.08, 0.24, 0.34), "concrete_beam", bevel=0.010, seg=2))
    objs.append(H.box("StaffPortalSoffit", (0.0, -7.90, 3.255), (2.04, 0.26, 0.022), "paint_cream", bevel=0.004))
    objs.append(H.box("StaffBrassThreshold", (0.0, -7.98, 0.003), (2.02, 0.056, 0.006), "brass", bevel=0.0015))

    # 2. Five Transverse Concrete Beams breaking up the 16m STAFF ceiling
    #    Positions: z = 0.60, 4.90, 8.95, 12.85, 15.88 -> Y = -7.40, -3.10, +0.95, +4.85, +7.88
    beam_ys = (-7.40, -3.10, 0.95, 4.85, 7.88)
    for bi, by in enumerate(beam_ys):
        objs.append(H.box(f"StaffBeam_{bi}", (0.0, by, 3.455), (2.08, 0.24, 0.29), "concrete_beam", bevel=0.010, seg=2))
        objs.append(H.box(f"StaffBeamTrim_{bi}", (0.0, by, 3.306), (2.04, 0.26, 0.020), "paint_cream", bevel=0.004))

    # 3. Shallow Structural Wall Pilasters (0.065m deep -> zero walkway obstruction!) under middle 3 beams
    #    Strictly clear of Nursing Desk (z=3.05..4.25), DOOR-STAFF (z=6.98..8.02) & North Med Cabinet (z=10.1..11.1)!
    for pi, py in enumerate((-3.10, 0.95, 4.85)):
        for sx, tag in ((-1.018, "W"), (1.018, "E")):
            objs.append(H.box(f"StaffPilPlinth_{tag}_{pi}", (sx, py, 0.048), (0.080, 0.26, 0.096), "terrazzo_dark", bevel=0.005))
            objs.append(H.box(f"StaffPilDado_{tag}_{pi}", (sx, py, 0.625), (0.065, 0.24, 1.05), "dado_pilaster", bevel=0.008))
            objs.append(H.box(f"StaffPilRail_{tag}_{pi}", (sx, py, 1.162), (0.078, 0.255, 0.030), "wood", bevel=0.004))
            objs.append(H.box(f"StaffPilUpper_{tag}_{pi}", (sx, py, 2.25), (0.065, 0.24, 2.12), "concrete_beam", bevel=0.008))
            objs.append(H.box(f"StaffPilCap_{tag}_{pi}", (sx, py, 3.32), (0.085, 0.27, 0.042), "paint_cream", bevel=0.004))

    # 4. Continuous Ceiling Perimeter Cornice Moldings between beams
    staff_spans = [(-7.26, -3.24), (-2.96, 0.81), (1.09, 4.71), (4.99, 7.74)]
    for sx, tag in ((-0.995, "W"), (0.995, "E")):
        for si, (y0, y1) in enumerate(staff_spans):
            yc = (y0 + y1) * 0.5
            yl = y1 - y0
            objs.append(H.box(f"StaffCornice_{tag}_{si}", (sx, yc, 3.535), (0.11, yl, 0.13), "concrete_beam", bevel=0.008))

    # 5. Slim Institutional Stretcher Crash Bumper + Rounded Ironwood Handrail along STAFF West Wall (protrudes <=5.6cm!)
    for si, (y0, y1) in enumerate([(-7.15, -3.32), (-2.88, 0.72), (1.18, 4.62), (5.08, 7.35)]):
        yc = (y0 + y1) * 0.5
        yl = y1 - y0
        objs.append(H.box(f"StaffCrashWood_{si}", (-1.032, yc, 0.71), (0.032, yl, 0.115), "wood", bevel=0.005, grain="Y"))
        objs.append(H.box(f"StaffCrashRub_{si}", (-1.012, yc, 0.71), (0.012, yl - 0.02, 0.056), "rubber_ribbed", bevel=0.003))
        for sy in (y0 + 0.01, y1 - 0.01):
            objs.append(H.box(f"StaffCrashCap_{si}_{sy:.2f}", (-1.028, sy, 0.71), (0.038, 0.018, 0.118), "satin_inox", bevel=0.002))
        # Slim matching rounded ironwood grip rail at Z=0.89m with wall-return elbows & stainless brackets
        objs.append(H.cyl(f"StaffGripBar_{si}", (-0.996, y0 + 0.05, 0.89), (-0.996, y1 - 0.05, 0.89), 0.017, "wood_light", seg=14, grain="Y"))
        for sy, sgn in ((y0 + 0.05, -1), (y1 - 0.05, 1)):
            objs.append(H.pipe(f"StaffGripRet_{si}_{sgn}", [
                (-0.996, sy, 0.89), (-1.004, sy + sgn * 0.025, 0.89), (-1.046, sy + sgn * 0.032, 0.89)
            ], 0.0165, "wood_light", res=4, grain="Y"))
        for k in range(3):
            by = y0 + 0.25 + k * ((yl - 0.50) * 0.5)
            objs.append(H.cyl(f"StaffRailBrk_{si}_{k}", (-1.048, by, 0.845), (-0.996, by, 0.845), 0.008, "steel_dark", seg=8))
            objs.append(H.cyl(f"StaffRailRose_{si}_{k}", (-1.048, by, 0.845), (-1.038, by, 0.845), 0.022, "satin_inox", seg=12))
            objs.append(H.cyl(f"StaffRailStem_{si}_{k}", (-0.996, by, 0.845), (-0.996, by, 0.875), 0.007, "satin_inox", seg=8))

    # 6. Continuous Galvanized Ceiling Conduit Spine offset to X=+0.22m (clean off-center sightline!)
    #    + Vertical Wall Drop plugging DIRECTLY into WallOutlet top gland at Z=1.285 (zero floating gap!)
    scx = 0.22
    sp_pts = [
        (scx, -7.75, 3.572),
        (scx, -7.53, 3.572), (scx, -7.515, 3.296), (scx, -7.285, 3.296), (scx, -7.27, 3.572),
        (scx, -3.23, 3.572), (scx, -3.215, 3.296), (scx, -2.985, 3.296), (scx, -2.97, 3.572),
        (scx,  0.82, 3.572), (scx,  0.835, 3.296), (scx,  1.065, 3.296), (scx,  1.08, 3.572),
        (scx,  4.72, 3.572), (scx,  4.735, 3.296), (scx,  4.965, 3.296), (scx,  4.98, 3.572),
        (scx,  7.74, 3.572),
    ]
    objs.append(H.pipe("StaffCeilingConduit", sp_pts, 0.0105, "galv_conduit", res=4))
    for by in beam_ys[:4]:
        objs.append(H.box(f"StaffBeamPipeClip_{by:.2f}", (scx, by, 3.298), (0.040, 0.020, 0.022), "satin_inox", bevel=0.002))
    for jy in (-4.15, -1.34, 3.80, 6.45):
        objs.append(H.cyl(f"StaffJBox_{jy:.2f}", (scx, jy, 3.545), (scx, jy, 3.595), 0.044, "breaker_grey", seg=8, bevel=0.004))
    for ty in (-4.15, 3.80):
        objs.append(H.pipe(f"StaffTubeTap_{ty:.2f}", [(scx, ty, 3.572), (0.0, ty, 3.572)], 0.009, "galv_conduit", res=3))

    # Branch conduit from Y=-1.34 (z=6.66) across ceiling & straight down East wall (X=+1.032) right into WallOutlet top gland (Z=1.285)!
    objs.append(H.pipe("StaffSwitchDrop", [
        (scx + 0.03, -1.34, 3.572), (0.93, -1.34, 3.572), (0.95, -1.34, 3.46), (1.032, -1.34, 3.43), (1.032, -1.34, 1.285)
    ], 0.010, "galv_conduit", res=4))
    for cz in (1.54, 2.08, 2.75, 3.30):
        objs.append(H.box(f"StaffDropClamp_{cz:.2f}", (1.032, -1.34, cz), (0.026, 0.036, 0.018), "satin_inox", bevel=0.002))

    # East-wall high conduit line serving North STAFF medicine bay & Door B lightbox
    objs.append(H.pipe("StaffNorthEastConduit", [
        (scx + 0.03, 3.80, 3.572), (0.94, 3.80, 3.572), (1.032, 3.80, 3.42), (1.032, 3.80, 3.12), (1.032, 7.65, 3.12)
    ], 0.0095, "galv_conduit", res=4))

    return objs


# --------------------------------------------------------------------------------------
# 4 & 5. CTCC_ENV_HandrailBay (3.65m) & CTCC_ENV_HandrailBay_Short (2.05m)
#        Wall mounted at X=0, protrudes +X (yaw=0 on West wall, yaw=180 on East wall).
# --------------------------------------------------------------------------------------
def _build_handrail(length: float, n_brackets: int):
    objs = []
    half = length * 0.5

    # Warm varnished ironwood rounded handrail grip at Z=0.89m with curved anti-snag wall returns
    objs.append(H.cyl("RailWoodBar", (0.076, -half, 0.89), (0.076, half, 0.89), 0.022, "wood_light", seg=20, grain="Y"))
    for s in (-1, 1):
        ret_pts = [
            (0.076, s * half, 0.89),
            (0.074, s * (half + 0.026), 0.89),
            (0.056, s * (half + 0.044), 0.89),
            (0.004, s * (half + 0.044), 0.89),
        ]
        objs.append(H.pipe(f"RailReturn_{s}", ret_pts, 0.0215, "wood_light", res=5, grain="Y"))
        # Brass ferrule rose where return meets plaster wall
        objs.append(H.cyl(f"RailWallRose_{s}", (0.0, s * (half + 0.044), 0.89), (0.012, s * (half + 0.044), 0.89), 0.032, "satin_inox", seg=16))

    # Sculpted hospital stretcher crash rail at Z=0.71m: chamfered ironwood backer + recessed ribbed vulcanized rubber insert
    objs.append(H.box("CrashBackWood", (0.024, 0.0, 0.71), (0.036, length + 0.08, 0.122), "wood", bevel=0.006, grain="Y"))
    objs.append(H.box("CrashRubberStrip", (0.045, 0.0, 0.71), (0.014, length + 0.04, 0.064), "rubber_ribbed", bevel=0.003))
    for s in (-1, 1):
        objs.append(H.box(f"CrashEndCap_{s}", (0.028, s * (half + 0.046), 0.71), (0.044, 0.018, 0.126), "satin_inox", bevel=0.003))

    # Heavy cast-steel flanged gooseneck wall brackets with mounting bolts
    for k in range(n_brackets):
        t = k / max(n_brackets - 1, 1)
        by = -(half - 0.24) + t * (length - 0.48)
        # Circular wall flange + 2 mounting screws
        objs.append(H.cyl(f"BrkFlange_{k}", (0.0, by, 0.825), (0.011, by, 0.825), 0.030, "satin_inox", seg=16))
        for sz in (-0.017, 0.017):
            objs.append(H.cyl(f"BrkBolt_{k}_{sz:.3f}", (0.008, by, 0.825 + sz), (0.015, by, 0.825 + sz), 0.004, "steel_dark", seg=8))
        # Curved cast neck + under-rail saddle
        objs.append(H.pipe(f"BrkNeck_{k}", [
            (0.009, by, 0.825), (0.052, by, 0.825), (0.076, by, 0.842), (0.076, by, 0.869)
        ], 0.009, "steel_dark", res=4))
        objs.append(H.box(f"BrkSaddle_{k}", (0.076, by, 0.869), (0.032, 0.046, 0.010), "satin_inox", bevel=0.002))

    return objs


def build_handrail_bay():
    return _build_handrail(3.65, 4)


def build_handrail_bay_short():
    return _build_handrail(2.05, 3)


# --------------------------------------------------------------------------------------
# 6. CTCC_ENV_ConduitRun (Upgraded 4.0m modular surface MEP galvanized conduit & junction box)
#    Wall at X=0, protrudes +X (yaw=0 on West wall, yaw=180 on East wall). Length along Y [-2..+2].
# --------------------------------------------------------------------------------------
def build_conduit_run():
    objs = []
    length = 4.00
    # Twin parallel galvanized steel electrical conduits + coupling sleeves
    objs.append(H.cyl("ConduitPipeMain", (0.020, -length * 0.5, 0.026), (0.020, length * 0.5, 0.026), 0.0115, "galv_conduit", seg=14))
    objs.append(H.cyl("ConduitPipeLow", (0.020, -length * 0.5, -0.026), (0.020, length * 0.5, -0.026), 0.0095, "galv_conduit", seg=14))
    for cy in (-1.28, 1.28):
        objs.append(H.cyl(f"CouplingA_{cy:.2f}", (0.020, cy - 0.022, 0.026), (0.020, cy + 0.022, 0.026), 0.0145, "steel_dark", seg=12))
        objs.append(H.cyl(f"CouplingB_{cy:.2f}", (0.020, cy - 0.020, -0.026), (0.020, cy + 0.020, -0.026), 0.0125, "steel_dark", seg=12))

    # Central cast-iron industrial octagonal/beveled junction box with lid gasket & 4 brass corner screws
    objs.append(H.box("JBoxBody", (0.026, 0.0, 0.0), (0.048, 0.128, 0.106), "breaker_grey", bevel=0.006))
    objs.append(H.box("JBoxLid", (0.052, 0.0, 0.0), (0.009, 0.134, 0.112), "pa_cream", bevel=0.003))
    for sy in (-0.052, 0.052):
        for sz in (-0.042, 0.042):
            objs.append(H.cyl(f"JBoxScrew_{sy:.2f}_{sz:.2f}", (0.055, sy, sz), (0.060, sy, sz), 0.0035, "brass", seg=8))

    # 6 Heavy steel twin-pipe saddle clamps bolted to wall
    for cy in (-1.78, -0.96, -0.34, 0.34, 0.96, 1.78):
        objs.append(H.box(f"SaddleBack_{cy:.2f}", (0.006, cy, 0.0), (0.010, 0.024, 0.098), "steel_dark", bevel=0.002))
        objs.append(H.box(f"SaddleStrap_{cy:.2f}", (0.022, cy, 0.0), (0.026, 0.018, 0.078), "satin_inox", bevel=0.002))
        for sz in (-0.040, 0.040):
            objs.append(H.cyl(f"SaddleBolt_{cy:.2f}_{sz:.2f}", (0.008, cy, sz), (0.016, cy, sz), 0.0032, "brass", seg=8))

    return objs


def run_all():
    HS.export_hs_asset(
        "CTCC_ENV_ColumnPilaster", build_column_pilaster,
        {"HALL_Pilaster": {"res": 1024, "bake_r": True}},
        make_lods=True, cam_dir=(1.55, -1.55, 0.35), ao_samples=10,
    )
    HS.export_hs_asset(
        "CTCC_HALL_CorridorBeams", build_hall_corridor_beams,
        {"HALL_CeilingArch": {"res": 1024, "bake_r": True}},
        make_lods=False, cam_dir=(0.95, -2.15, -0.35), ao_samples=8,
    )
    HS.export_hs_asset(
        "CTCC_STAFF_CorridorArch", build_staff_corridor_arch,
        {"STAFF_CeilingArch": {"res": 1024, "bake_r": True}},
        make_lods=False, cam_dir=(0.95, -2.15, -0.35), ao_samples=8,
    )
    HS.export_hs_asset(
        "CTCC_ENV_HandrailBay", build_handrail_bay,
        {"HALL_Handrail": {"res": 1024, "bake_r": True}},
        make_lods=True, cam_dir=(1.65, -1.45, 0.55), ao_samples=10,
    )
    HS.export_hs_asset(
        "CTCC_ENV_HandrailBay_Short", build_handrail_bay_short,
        {"HALL_HandrailShort": {"res": 1024, "bake_r": True}},
        make_lods=True, render=False, ao_samples=8,
    )
    HS.export_hs_asset(
        "CTCC_ENV_ConduitRun", build_conduit_run,
        {"HALL_Conduit": {"res": 1024, "bake_r": True}},
        make_lods=True, cam_dir=(1.35, -1.65, 0.40), ao_samples=10,
    )


if __name__ == "__main__":
    run_all()
