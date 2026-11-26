"""M2 High-Fidelity Vietnamese Hospital Architecture Kit (1987-2005) built in Blender 4.5 LTS.

Covers LOC01 Exterior, LOC02 Reception, and LOC04 Emergency Department:
- CTCC_ENV_HospitalFacade: 3-story 1987 Đổi Mới hospital facade w/ concrete brise-soleil (lam chắn nắng),
  breeze-block stairwell grille (gạch thông gió), recessed upper steel windows, cornice, gutters & entrance portico canopy
- CTCC_ENV_Gatehouse: Brick & limewash security guard booth (phòng bảo vệ) beside side gate w/ sliding window & awning
- CTCC_ENV_CourtyardFenceBay: 4.0m brick-base + plastered pier + iron-spear perimeter fence wall module
- CTCC_ENV_ReceptionPartition: 1990s-2000s Vietnamese glazed timber reception screen w/ speaking arch & document slot
- CTCC_ENV_EmergencySwingDoors: 1.40m double swinging hospital ER doors w/ stainless kick plates, porthole glass & crash bars
- CTCC_ENV_StairwellBarrier: Concrete stairwell opening w/ rising terrazzo steps to Khu B (+4.0m) & timber barricade
- CTCC_ENV_DrainGrateAndCurb: Cast-iron monsoon storm drain grate & chamfered courtyard curb channel
"""

import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import common as C


def build_hospital_facade():
    """Authentic 3-story 1987 Vietnamese public hospital front elevation (width 18 m, height 11.7 m, base Y=0).
    Ground floor open at X in [-1.0, 9.0] -> centered at local X=0 spanning [-9.0, 9.0] facing -Y (South toward LOC01)."""
    objs = []
    # Main upper facade mass (floors 2 & 3: Z = 3.60 m to 11.70 m, spanning X = -9.0 to +9.0, depth 0.55 m behind Y=0)
    objs.append(C.make_box("UpperFacadeWall", (0.0, 0.28, 7.65), (18.2, 0.45, 8.10), "ExteriorFacade", bevel=0.02, uv_scale=2.5))
    # Flanking ground-floor exterior side wings left/right of reception bay (X = -9..-5.1 and +5.1..+9.0)
    for sx in (-7.05, 7.05):
        objs.append(C.make_box(f"GroundWing_{sx:.1f}", (sx, 0.28, 1.80), (4.10, 0.45, 3.60), "ExteriorFacade", bevel=0.02, uv_scale=2.5))
        objs.append(C.make_box(f"GroundWingPlinth_{sx:.1f}", (sx, 0.24, 0.45), (4.16, 0.50, 0.90), "WeatheredBrick", bevel=0.012, uv_scale=1.2))

    # Horizontal reinforced-concrete floor slab bands (tầng 2 @ 3.72m, tầng 3 @ 7.72m, roof parapet @ 11.55m)
    for z_slab, h_slab, proj in ((3.72, 0.34, 0.32), (7.72, 0.34, 0.32), (11.55, 0.48, 0.42)):
        objs.append(C.make_box(f"FloorSlabBand_{z_slab:.1f}", (0.0, -proj * 0.4, z_slab), (18.6, 0.55 + proj, h_slab), "BaseTrim", bevel=0.015, uv_scale=2.0))

    # Vertical structural concrete pilasters across facade (5 bays x 4m)
    for px in (-8.8, -4.8, -1.95, 1.95, 4.8, 8.8):
        objs.append(C.make_box(f"FacadePilaster_{px:.1f}", (px, -0.02, 7.65), (0.36, 0.28, 8.10), "BaseTrim", bevel=0.015, uv_scale=2.0))

    # Signature 1980s-90s Vietnamese concrete sun-shading brise-soleil fins (lam chắn nắng) on floors 2 & 3!
    for floor_z in (4.55, 8.55):
        for bay_x in (-6.8, -3.35, 3.35, 6.8):
            # Recessed dark steel-framed upper ward window
            objs.append(C.make_box(f"WinRecess_{floor_z:.0f}_{bay_x:.1f}", (bay_x, 0.04, floor_z + 1.05), (2.45, 0.14, 1.75), "Frame", bevel=0.008))
            objs.append(C.make_box(f"WinGlass_{floor_z:.0f}_{bay_x:.1f}", (bay_x, 0.01, floor_z + 1.05), (2.30, 0.03, 1.60), "Glass", bevel=0.002))
            # Horizontal mullions + vertical casement dividers
            objs.append(C.make_box(f"WinMidRail_{floor_z:.0f}_{bay_x:.1f}", (bay_x, -0.02, floor_z + 1.48), (2.36, 0.05, 0.05), "Frame", bevel=0.004))
            for mx in (-0.75, 0.0, 0.75):
                objs.append(C.make_box(f"WinStile_{floor_z:.0f}_{bay_x:.1f}_{mx:.1f}", (bay_x + mx, -0.02, floor_z + 1.05), (0.045, 0.05, 1.68), "Frame", bevel=0.004))
            # Projecting concrete rain hood + 2 horizontal brise-soleil blades
            objs.append(C.make_box(f"RainHood_{floor_z:.0f}_{bay_x:.1f}", (bay_x, -0.26, floor_z + 2.02), (2.68, 0.58, 0.10), "BaseTrim", bevel=0.010, uv_scale=1.5))
            objs.append(C.make_box(f"Sill_{floor_z:.0f}_{bay_x:.1f}", (bay_x, -0.14, floor_z + 0.15), (2.60, 0.34, 0.09), "BaseTrim", bevel=0.008, uv_scale=1.5))
            for fz in (floor_z + 1.65, floor_z + 1.25):
                objs.append(C.make_box(f"BriseFin_{floor_z:.0f}_{bay_x:.1f}_{fz:.1f}", (bay_x, -0.34, fz), (2.56, 0.24, 0.055), "ExteriorFacade", bevel=0.006, rot=(math.radians(14), 0, 0)))

    # Center stairwell perforated concrete breeze-block screen (gạch thông gió — iconic 1987 Vietnamese hospital motif!)
    for row in range(11):
        for col in range(7):
            bx = -1.35 + col * 0.45
            bz = 4.35 + row * 0.58
            objs.append(C.make_box(f"BreezeH_{row}_{col}", (bx, -0.14, bz), (0.43, 0.18, 0.07), "BaseTrim", bevel=0.005))
            objs.append(C.make_box(f"BreezeV_{row}_{col}", (bx, -0.14, bz + 0.29), (0.07, 0.18, 0.56), "BaseTrim", bevel=0.005))
    # Dark interior backing behind stairwell breeze blocks
    objs.append(C.make_box("StairwellVoid", (0.0, 0.10, 7.65), (3.40, 0.08, 7.20), "Black", bevel=0.0))

    # Molded cantilevered entrance portico canopy over reception doors (center X=0, projects -3.2m into LOC01 courtyard)
    objs.append(C.make_box("CanopySlab", (0.0, -1.55, 3.05), (5.20, 3.35, 0.26), "BaseTrim", bevel=0.018, uv_scale=1.8))
    objs.append(C.make_box("CanopyFascia", (0.0, -3.22, 3.16), (5.30, 0.12, 0.46), "DadoR", bevel=0.012, uv_scale=1.5))
    # Twin fluted portico columns w/ brick bases & concrete capitals (X = -2.18, +2.18)
    for cx in (-2.18, 2.18):
        objs.append(C.make_box(f"PorticoBase_{cx:.1f}", (cx, -2.55, 0.35), (0.42, 0.42, 0.70), "WeatheredBrick", bevel=0.012, uv_scale=1.0))
        objs.append(C.make_box(f"PorticoCol_{cx:.1f}", (cx, -2.55, 1.82), (0.32, 0.32, 2.25), "ExteriorFacade", bevel=0.016, uv_scale=1.5))
        objs.append(C.make_box(f"PorticoCap_{cx:.1f}", (cx, -2.55, 2.92), (0.44, 0.44, 0.16), "BaseTrim", bevel=0.012))
    # Two weathered galvanized rainwater downspouts along left & right facade edges
    for dx in (-8.55, 8.55):
        objs.append(C.make_cylinder(f"Downspout_{dx:.1f}", (dx, -0.18, 0.15), (dx, -0.18, 11.45), 0.055, "MetalOld", segments=12))
        for cz in (1.5, 4.5, 7.5, 10.5):
            objs.append(C.make_box(f"PipeClamp_{dx:.1f}_{cz:.0f}", (dx, -0.12, cz), (0.16, 0.14, 0.05), "Frame", bevel=0.004))

    # Rooftop water tank silhouette on right parapet (quintessential Vietnamese 1990s-2000s roof detail!)
    objs.append(C.make_cylinder("RoofTank", (5.8, 1.2, 12.6), (7.6, 1.2, 12.6), 0.62, "Steel", segments=18, bevel=0.02))
    for lx in (6.1, 7.3):
        for ly in (0.9, 1.5):
            objs.append(C.make_cylinder(f"TankLeg_{lx:.1f}_{ly:.1f}", (lx, ly, 11.7), (lx, ly, 12.15), 0.035, "MetalOld", segments=8))
    return objs


def build_gatehouse():
    """Brick & weathered limewash Vietnamese hospital security guardhouse (nhà bảo vệ: 2.6 x 2.3 x 2.85 m)
    beside LOC01 side gate (placed at x=8.55, z=-18.55). Both SOUTH (-Y, facing exterior approach) AND
    WEST (-X, facing the entry gate!) have brick plinths, concrete sills, sliding guard windows & caged porch lamp!"""
    objs = []
    # Concrete base pad
    objs.append(C.make_box("GH_Pad", (0, 0, 0.06), (2.85, 2.55, 0.12), "BaseTrim", bevel=0.012, uv_scale=1.5))
    # Back (+Y) and East (+X) solid masonry walls w/ brick lower plinth so every angle looks grounded
    objs.append(C.make_box("GH_BackLower", (0, 1.05, 0.60), (2.50, 0.18, 1.06), "WeatheredBrick", bevel=0.010, uv_scale=1.2))
    objs.append(C.make_box("GH_BackUpper", (0, 1.05, 1.88), (2.50, 0.18, 1.52), "ExteriorFacade", bevel=0.012, uv_scale=2.0))
    objs.append(C.make_box("GH_RightLower", (1.16, 0, 0.60), (0.18, 2.28, 1.06), "WeatheredBrick", bevel=0.010, uv_scale=1.2))
    objs.append(C.make_box("GH_RightUpper", (1.16, 0, 1.88), (0.18, 2.28, 1.52), "ExteriorFacade", bevel=0.012, uv_scale=2.0))
    # Gate-facing WEST (-X) and Approach-facing SOUTH (-Y) counter walls with sliding security windows!
    objs.append(C.make_box("GH_LeftLower", (-1.16, 0, 0.60), (0.18, 2.28, 1.06), "WeatheredBrick", bevel=0.010, uv_scale=1.2))
    objs.append(C.make_box("GH_LeftUpper", (-1.16, 0, 2.40), (0.18, 2.28, 0.48), "ExteriorFacade", bevel=0.010, uv_scale=1.8))
    objs.append(C.make_box("GH_FrontLower", (0, -1.05, 0.60), (2.50, 0.18, 1.06), "WeatheredBrick", bevel=0.010, uv_scale=1.2))
    objs.append(C.make_box("GH_FrontUpper", (0, -1.05, 2.40), (2.50, 0.18, 0.48), "ExteriorFacade", bevel=0.010, uv_scale=1.8))
    # Corner masonry piers
    for cx in (-1.16, 1.16):
        for cy in (-1.05, 1.05):
            objs.append(C.make_box(f"GH_Corner_{cx:.1f}_{cy:.1f}", (cx, cy, 1.38), (0.24, 0.24, 2.54), "ExteriorFacade", bevel=0.012, uv_scale=2.0))

    # Front (-Y) & Gate-facing West (-X) sliding steel window frames + dusty glass panes + outer concrete sills
    objs.append(C.make_box("GH_FrontSill", (0, -1.14, 1.12), (2.30, 0.34, 0.07), "BaseTrim", bevel=0.008))
    objs.append(C.make_box("GH_LeftSill", (-1.24, 0, 1.12), (0.34, 2.08, 0.07), "BaseTrim", bevel=0.008))
    objs.append(C.make_box("GH_FrontGlassL", (-0.52, -1.05, 1.65), (1.02, 0.02, 1.00), "FrostedGlass", bevel=0.002))
    objs.append(C.make_box("GH_FrontGlassR", (0.56, -1.02, 1.65), (0.94, 0.02, 1.00), "Glass", bevel=0.002))
    objs.append(C.make_box("GH_LeftGlassS", (-1.16, -0.48, 1.65), (0.02, 0.94, 1.00), "Glass", bevel=0.002))
    objs.append(C.make_box("GH_LeftGlassN", (-1.13, 0.48, 1.65), (0.02, 0.94, 1.00), "FrostedGlass", bevel=0.002))
    # Steel window muntins & top/bottom steel casement bars
    for wx in (-1.02, 0.0, 1.02):
        objs.append(C.make_box(f"GH_FBar_{wx:.1f}", (wx, -1.05, 1.65), (0.04, 0.05, 1.04), "Frame", bevel=0.004))
    for wy in (-0.95, 0.0, 0.95):
        objs.append(C.make_box(f"GH_LBar_{wy:.1f}", (-1.16, wy, 1.65), (0.05, 0.04, 1.04), "Frame", bevel=0.004))
    objs.append(C.make_box("GH_LMidRail", (-1.16, 0, 1.92), (0.05, 1.94, 0.036), "Frame", bevel=0.003))
    objs.append(C.make_box("GH_FMidRail", (0, -1.05, 1.92), (2.06, 0.05, 0.036), "Frame", bevel=0.003))

    # Sloped overhanging monsoon concrete awning roof + weathered metal fascia
    objs.append(C.make_box("GH_Roof", (0, -0.06, 2.74), (3.32, 3.08, 0.16), "BaseTrim", bevel=0.016, rot=(math.radians(5), 0, 0), uv_scale=1.6))
    objs.append(C.make_box("GH_RoofFasciaS", (0, -1.58, 2.62), (3.36, 0.08, 0.22), "MetalOld", bevel=0.006))
    objs.append(C.make_box("GH_RoofFasciaW", (-1.66, -0.06, 2.72), (0.08, 3.10, 0.20), "MetalOld", bevel=0.006, rot=(math.radians(5), 0, 0)))

    # Interior storytelling: guard desk, ledger & dark interior backdrop visible through both windows!
    objs.append(C.make_box("GH_DeskTop", (-0.25, -0.30, 0.78), (1.35, 0.65, 0.05), "Wood", bevel=0.005, uv_rot90=True))
    for dx in (-0.88, 0.38):
        for dy in (-0.58, -0.04):
            objs.append(C.make_box(f"GH_Leg_{dx:.1f}_{dy:.1f}", (dx, dy, 0.42), (0.05, 0.05, 0.70), "Wood", bevel=0.004))
    objs.append(C.make_box("GH_Ledger", (-0.42, -0.36, 0.82), (0.34, 0.24, 0.03), "Paper", bevel=0.002, rot=(0, 0, 0.18)))
    # Exterior caged bulkhead porch lamp mounted on GATE-FACING (-X) front-west corner pier!
    objs.append(C.make_cylinder("GH_LampBase", (-1.28, -0.82, 2.26), (-1.34, -0.82, 2.26), 0.082, "MetalOld", segments=14))
    objs.append(C.make_sphere("GH_LampGlobe", (-1.36, -0.82, 2.26), (0.095, 0.115, 0.115), "WarmGlow", segs=14, rings=8))
    return objs


def build_courtyard_fence_bay():
    """Modular 4.0m Vietnamese hospital perimeter fence bay: brick plinth (0.90m) + iron spear railings (1.15m) + masonry pier."""
    objs = []
    # Masonry pier at X = -2.0 m
    objs.append(C.make_box("FencePier", (-2.0, 0, 1.12), (0.36, 0.36, 2.24), "ExteriorFacade", bevel=0.015, uv_scale=1.5))
    objs.append(C.make_box("FencePierCap", (-2.0, 0, 2.28), (0.44, 0.44, 0.10), "BaseTrim", bevel=0.010))
    # Brick lower wall spanning [-1.84, +2.0]
    objs.append(C.make_box("FenceBrickWall", (0.08, 0, 0.45), (3.86, 0.24, 0.90), "WeatheredBrick", bevel=0.010, uv_scale=1.2))
    objs.append(C.make_box("FenceCoping", (0.08, 0, 0.93), (3.90, 0.30, 0.075), "BaseTrim", bevel=0.008))
    # Horizontal iron fence rails + 24 vertical iron spear pickets
    for rz in (1.12, 1.88):
        objs.append(C.make_box(f"IronRail_{rz:.2f}", (0.08, 0, rz), (3.84, 0.038, 0.038), "MetalOld", bevel=0.004))
    for i in range(23):
        px = -1.70 + i * 0.162
        objs.append(C.make_cylinder(f"Picket_{i}", (px, 0, 0.96), (px, 0, 2.02), 0.011, "MetalOld", segments=8))
        objs.append(C.make_cylinder(f"SpearTip_{i}", (px, 0, 2.02), (px, 0, 2.13), 0.018, "MetalOld", segments=6, r_end=0.002))
    return objs


def build_reception_partition():
    """Authentic 1990s-2000s Vietnamese hospital reception counter glazed timber partition (length 2.55 m, height 1.15 m above counter).
    Horizontal rails align wood grain lengthwise (uv_rot90=True) to eliminate vertical zebra striping!"""
    objs = []
    # Base wooden sill rail & top header with lengthwise wood grain (uv_rot90=True)
    objs.append(C.make_box("RP_Sill", (0, 0, 0.03), (0.10, 2.56, 0.06), "WoodDoor", bevel=0.005, uv_scale=0.85, uv_rot90=True))
    objs.append(C.make_box("RP_Header", (0, 0, 1.12), (0.11, 2.58, 0.08), "WoodDoor", bevel=0.006, uv_scale=0.85, uv_rot90=True))
    # Subtle cornice cap lip along top header for authentic Vietnamese joinery profile
    objs.append(C.make_box("RP_HeaderCap", (0, 0, 1.17), (0.135, 2.62, 0.026), "WoodDoor", bevel=0.004, uv_scale=0.85, uv_rot90=True))
    # 4 Vertical timber mullions creating 3 bays (vertical grain)
    for my in (-1.24, -0.42, 0.42, 1.24):
        objs.append(C.make_box(f"RP_Mullion_{my:.2f}", (0, my, 0.58), (0.085, 0.07, 1.08), "WoodDoor", bevel=0.005, uv_scale=0.85))
    # Side bay glass panes (full height) + horizontal mid transom rail (lengthwise grain)
    for gy in (-0.83, 0.83):
        objs.append(C.make_box(f"RP_Glass_{gy:.2f}", (0, gy, 0.58), (0.016, 0.76, 1.00), "Glass", bevel=0.001))
        objs.append(C.make_box(f"RP_MidBar_{gy:.2f}", (0, gy, 0.84), (0.048, 0.78, 0.036), "WoodDoor", bevel=0.003, uv_scale=0.85, uv_rot90=True))
    # Center bay glass starts above Z=0.26m leaving realistic document pass-through slit + stainless tray!
    objs.append(C.make_box("RP_CenterGlass", (0, 0, 0.69), (0.016, 0.76, 0.78), "Glass", bevel=0.001))
    objs.append(C.make_box("RP_PassBar", (0, 0, 0.28), (0.048, 0.78, 0.034), "WoodDoor", bevel=0.003, uv_scale=0.85, uv_rot90=True))
    objs.append(C.make_box("RP_SteelTray", (0, 0, 0.065), (0.28, 0.62, 0.014), "Steel", bevel=0.004))
    # Overhead steel mounting posts connecting header to reception ceiling
    for py in (-1.18, 1.18):
        objs.append(C.make_cylinder(f"RP_CeilingRod_{py:.2f}", (0, py, 1.16), (0, py, 2.52), 0.018, "Steel", segments=10))
    return objs


def build_emergency_swing_door_leaf():
    """Single heavy swinging Vietnamese Emergency Department door leaf (width 0.70m x height 2.18m, hinge at X=0).
    Paired left/right across 1.40m GATE-CAPCUU openings with stainless kick plate, chamfered observation window,
    black rubber impact bumper & horizontal stainless push crash bar."""
    objs = []
    w, h, t = 0.692, 2.16, 0.052
    # Main enameled hospital steel leaf body (stiles + rails around recessed porthole window)
    objs.append(C.make_box("ER_LowerPanel", (w * 0.5, 0, 0.55), (w, t, 1.10), "MetalPaint", bevel=0.005))
    objs.append(C.make_box("ER_TopRail", (w * 0.5, 0, 2.02), (w, t, 0.28), "MetalPaint", bevel=0.005))
    objs.append(C.make_box("ER_HingeStile", (0.09, 0, 1.49), (0.18, t, 0.80), "MetalPaint", bevel=0.005))
    objs.append(C.make_box("ER_MeetStile", (w - 0.09, 0, 1.49), (0.18, t, 0.80), "MetalPaint", bevel=0.005))
    # Brushed stainless steel heavy stretcher kick plate (bottom 0.88m both sides)
    objs.append(C.make_box("ER_KickPlate", (w * 0.5, 0, 0.45), (w - 0.03, t + 0.008, 0.86), "Steel", bevel=0.003))
    # Rubber edge bumper along meeting edge
    objs.append(C.make_box("ER_EdgeBumper", (w - 0.004, 0, 1.08), (0.018, t + 0.004, 2.14), "Rubber", bevel=0.004))
    # Chamfered black rubber glazing gasket + frosted observation safety glass
    objs.append(C.make_box("ER_WinFrame", (w * 0.5, 0, 1.49), (w - 0.32, t + 0.012, 0.82), "Rubber", bevel=0.006))
    objs.append(C.make_box("ER_WinGlass", (w * 0.5, 0, 1.49), (w - 0.38, 0.016, 0.74), "FrostedGlass", bevel=0.002))
    # Tubular stainless steel crash push bar on both faces
    for sy in (-0.058, 0.058):
        objs.append(C.make_pipe(f"ER_CrashBar_{sy:.2f}", [
            (0.10, sy * 0.45, 1.02), (0.10, sy, 1.02), (w - 0.10, sy, 1.02), (w - 0.10, sy * 0.45, 1.02)
        ], 0.016, "Steel", bevel_res=4))
    # Heavy double-action brass/steel spring hinges
    for hz in (0.34, 1.12, 1.88):
        objs.append(C.make_cylinder(f"ER_Hinge_{hz:.2f}", (0.004, 0, hz - 0.06), (0.004, 0, hz + 0.06), 0.017, "Steel", segments=10))
    return objs


def build_stairwell_barrier():
    """Freestanding wooden/steel hospital barricade sawhorse (width 1.85m, height 1.06m) blocking Khu B stairwell
    in LOC02/LOC04, with angled legs, cross rails, and hazard diagonal backing board."""
    objs = []
    # Twin A-frame tubular legs at X = -0.80, +0.80
    for sx in (-0.80, 0.80):
        objs.append(C.make_cylinder(f"LegF_{sx:.1f}", (sx, -0.32, 0.0), (sx, -0.04, 1.06), 0.022, "MetalOld", segments=10))
        objs.append(C.make_cylinder(f"LegB_{sx:.1f}", (sx, 0.32, 0.0), (sx, 0.04, 1.06), 0.022, "MetalOld", segments=10))
        objs.append(C.make_cylinder(f"LegTie_{sx:.1f}", (sx, -0.22, 0.38), (sx, 0.22, 0.38), 0.015, "MetalOld", segments=8))
    # Twin horizontal wooden barrier planks
    objs.append(C.make_box("UpperPlank", (0, 0, 0.88), (1.88, 0.045, 0.24), "Wood", bevel=0.005))
    objs.append(C.make_box("LowerPlank", (0, 0, 0.48), (1.82, 0.038, 0.14), "Wood", bevel=0.005))
    # Red painted corner warning end-caps
    for sx in (-0.84, 0.84):
        objs.append(C.make_box(f"WarnCap_{sx:.1f}", (sx, 0, 0.88), (0.20, 0.049, 0.242), "RedPaint", bevel=0.004))
    return objs


def build_drain_grate_and_curb():
    """Cast-iron monsoon storm drain grate (0.65 x 0.42 m) nestled in recessed concrete surround for LOC01 courtyard."""
    objs = []
    objs.append(C.make_box("DrainFrame", (0, 0, 0.015), (0.72, 0.48, 0.03), "BaseTrim", bevel=0.006))
    objs.append(C.make_box("DrainPit", (0, 0, 0.018), (0.62, 0.38, 0.022), "Black", bevel=0.0))
    # Slotted cast-iron grate bars
    objs.append(C.make_box("GrateRimL", (-0.30, 0, 0.028), (0.04, 0.38, 0.016), "MetalOld", bevel=0.003))
    objs.append(C.make_box("GrateRimR", (0.30, 0, 0.028), (0.04, 0.38, 0.016), "MetalOld", bevel=0.003))
    for i in range(11):
        gx = -0.25 + i * 0.05
        objs.append(C.make_box(f"GrateBar_{i}", (gx, 0, 0.028), (0.022, 0.36, 0.016), "MetalOld", bevel=0.003))
    return objs


def run_all():
    C.export_asset("CTCC_ENV_HospitalFacade", build_hospital_facade, make_lods=True, cam_dir=(0.9, -2.1, 0.55))
    C.export_asset("CTCC_ENV_Gatehouse", build_gatehouse, make_lods=True, cam_dir=(1.55, -1.65, 0.72))
    C.export_asset("CTCC_ENV_CourtyardFenceBay", build_courtyard_fence_bay, make_lods=True, cam_dir=(1.1, -1.9, 0.65))
    C.export_asset("CTCC_ENV_ReceptionPartition", build_reception_partition, make_lods=True, cam_dir=(1.6, -1.3, 0.75))
    C.export_asset("CTCC_ENV_EmergencySwingDoorLeaf", build_emergency_swing_door_leaf, make_lods=True, cam_dir=(1.1, -1.7, 0.55))
    C.export_asset("CTCC_ENV_StairwellBarrier", build_stairwell_barrier, make_lods=True, cam_dir=(1.1, -1.7, 0.72))
    C.export_asset("CTCC_ENV_DrainGrateAndCurb", build_drain_grate_and_curb, make_lods=False, cam_dir=(1.1, -1.4, 1.25))


if __name__ == "__main__":
    run_all()
