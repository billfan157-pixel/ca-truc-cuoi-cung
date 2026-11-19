"""Modular Vietnamese Hospital Architecture Kit (1987-2005) built in Blender 4.5.

All coordinates in metres, pivots aligned to architectural grid & Unity layout.json.
"""

import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import common as C


def build_ward_door_frame():
    """1.20 x 2.22 m molded timber/steel doorway casing + overhead louvered transom (2.22-2.86 m) + steel threshold.
    Centered at X=0, Y=0 (wall thickness 0.20 m), Z=0 at floor."""
    objs = []
    w, h, wall_t = 1.20, 2.22, 0.24
    jamb_w = 0.068
    transom_h = 0.62

    # Outer molded casing jambs + deep wall reveal lining
    for s in (-1, 1):
        jx = s * (w * 0.5 + jamb_w * 0.5)
        objs.append(C.make_box(f"JambCore_{s}", (jx, 0, (h + transom_h) * 0.5), (jamb_w, wall_t, h + transom_h), "WoodDoor", bevel=0.005))
        # Door stop strip inside jamb
        objs.append(C.make_box(f"DoorStop_{s}", (s * (w * 0.5 + 0.009), -0.025, h * 0.5), (0.018, 0.028, h), "WoodDoor", bevel=0.003))
        # Stepped architrave molding on both sides of wall
        for side_y in (-1, 1):
            objs.append(C.make_box(f"Architrave_{s}_{side_y}", (s * (w * 0.5 + 0.052), side_y * (wall_t * 0.5 + 0.011), (h + transom_h + 0.07) * 0.5),
                                   (0.088, 0.022, h + transom_h + 0.07), "WoodDoor", bevel=0.006))
        # Stainless bottom kick-guard on jambs
        objs.append(C.make_box(f"JambGuard_{s}", (s * (w * 0.5 + 0.036), 0, 0.14), (0.076, wall_t + 0.008, 0.28), "Steel", bevel=0.003))

    # Transom cross-rail (header at 2.22 m) & top head rail at 2.84 m
    objs.append(C.make_box("HeaderBeam", (0, 0, h + 0.035), (w + jamb_w * 2, wall_t, 0.070), "WoodDoor", bevel=0.005))
    objs.append(C.make_box("TopHead", (0, 0, h + transom_h + 0.032), (w + jamb_w * 2 + 0.04, wall_t + 0.028, 0.064), "WoodDoor", bevel=0.006))
    for side_y in (-1, 1):
        objs.append(C.make_box(f"HeaderTrim_{side_y}", (0, side_y * (wall_t * 0.5 + 0.011), h + transom_h + 0.052),
                               (w + 0.19, 0.022, 0.085), "WoodDoor", bevel=0.006))

    # Overhead louvered ventilation transom (ô thoáng chớp kính - iconic 1987 VN hospital!)
    for sx in (-0.38, 0.0, 0.38):
        objs.append(C.make_box(f"TransomMullion_{sx:.2f}", (sx, 0, h + 0.07 + (transom_h - 0.07) * 0.5),
                               (0.032, 0.075, transom_h - 0.07), "WoodDoor", bevel=0.003))
    for k in range(5):
        lz = h + 0.13 + k * 0.095
        objs.append(C.make_box(f"TransomBlade_{k}", (0, 0, lz), (w - 0.03, 0.085, 0.012), "FrostedGlass",
                               bevel=0.002, rot=(math.radians(32), 0, 0)))

    # Brushed steel beveled threshold plate with ribbed grip strips
    objs.append(C.make_box("ThresholdBase", (0, 0, 0.009), (w + 0.02, 0.145, 0.018), "Steel", bevel=0.006))
    for k in range(-2, 3):
        objs.append(C.make_box(f"ThresholdRib_{k}", (0, k * 0.024, 0.0185), (w - 0.02, 0.006, 0.003), "Steel", bevel=0.001))

    # 3 Fixed frame hinge knuckles on left jamb (matching leaf hinge at X = -w/2)
    for k, hz in enumerate((0.28, 1.08, 1.88)):
        objs.append(C.make_cylinder(f"FrameHinge_{k}", (-w * 0.5 + 0.008, -0.048, hz - 0.055),
                                    (-w * 0.5 + 0.008, -0.048, hz + 0.055), 0.011, "Steel", segments=14))
    return objs


def build_ward_door_leaf():
    """1.18 x 2.20 m paneled Vietnamese hospital timber ward door leaf.
    Pivot at hinge edge X=0, Y=0, Z=0; leaf extends along +X to 1.18 m!"""
    objs = []
    w, h, t = 1.18, 2.20, 0.048
    stile = 0.125

    # Vertical stiles
    objs.append(C.make_box("HingeStile", (stile * 0.5, 0, h * 0.5), (stile, t, h), "WoodDoor", bevel=0.004))
    objs.append(C.make_box("LatchStile", (w - stile * 0.5, 0, h * 0.5), (stile, t, h), "WoodDoor", bevel=0.004))
    # Horizontal rails (bottom, lock mid-rail, top rail)
    inner_w = w - stile * 2
    objs.append(C.make_box("BottomRail", (w * 0.5, 0, 0.15), (inner_w + 0.01, t, 0.30), "WoodDoor", bevel=0.004))
    objs.append(C.make_box("MidRail", (w * 0.5, 0, 1.08), (inner_w + 0.01, t, 0.16), "WoodDoor", bevel=0.004))
    objs.append(C.make_box("TopRail", (w * 0.5, 0, h - 0.09), (inner_w + 0.01, t, 0.18), "WoodDoor", bevel=0.004))

    # Lower recessed timber pano panels (twin recessed panels with beveled molding)
    objs.append(C.make_box("MidMullionLow", (w * 0.5, 0, 0.65), (0.085, t * 0.92, 0.72), "WoodDoor", bevel=0.004))
    for px in (stile + inner_w * 0.25, stile + inner_w * 0.75):
        pw = inner_w * 0.44
        objs.append(C.make_box(f"LowerPanel_{px:.2f}", (px, 0, 0.65), (pw, t * 0.45, 0.66), "WoodDoor", bevel=0.003))
        objs.append(C.make_box(f"LowerPanelRaise_{px:.2f}", (px, 0, 0.65), (pw - 0.05, t * 0.72, 0.56), "WoodDoor", bevel=0.010))

    # Upper recessed frosted observation glass pane + wooden glazing beads & divider muntin
    objs.append(C.make_box("ObsGlass", (w * 0.5, 0, 1.59), (inner_w - 0.02, 0.010, 0.86), "FrostedGlass", bevel=0.0))
    objs.append(C.make_box("GlassMuntinH", (w * 0.5, 0, 1.59), (inner_w, 0.032, 0.030), "WoodDoor", bevel=0.003))
    objs.append(C.make_box("GlassMuntinV", (w * 0.5, 0, 1.59), (0.030, 0.032, 0.88), "WoodDoor", bevel=0.003))
    for s in (-1, 1):
        objs.append(C.make_box(f"BeadTop_{s}", (w * 0.5, s * 0.014, 2.01), (inner_w, 0.016, 0.020), "WoodDoor", bevel=0.002))
        objs.append(C.make_box(f"BeadBot_{s}", (w * 0.5, s * 0.014, 1.17), (inner_w, 0.016, 0.020), "WoodDoor", bevel=0.002))

    # Brushed stainless steel stretcher kick-plate at bottom (both faces)
    for s in (-1, 1):
        objs.append(C.make_box(f"KickPlate_{s}", (w * 0.5, s * (t * 0.5 + 0.002), 0.165), (w - 0.06, 0.004, 0.29), "Steel", bevel=0.001))

    # Mortise lock escutcheon, keyhole, and curved stainless hospital lever handles (both sides!)
    hx = w - 0.095
    hz = 1.02
    objs.append(C.make_box("LatchFaceplate", (w + 0.001, 0, hz), (0.004, 0.024, 0.18), "Steel", bevel=0.001))
    objs.append(C.make_box("LatchBolt", (w + 0.008, 0, hz + 0.025), (0.014, 0.014, 0.024), "Brass", bevel=0.002))
    for s in (-1, 1):
        ey = s * (t * 0.5 + 0.004)
        objs.append(C.make_box(f"Escutcheon_{s}", (hx, ey, hz - 0.025), (0.048, 0.008, 0.215), "Steel", bevel=0.003))
        objs.append(C.make_cylinder(f"KeyCylinder_{s}", (hx, ey, hz - 0.085), (hx, ey + s * 0.006, hz - 0.085), 0.012, "Brass", segments=14))
        # Curved return hospital safety lever (anti-snag U-handle!)
        pts = [
            (hx, ey, hz),
            (hx, ey + s * 0.056, hz),
            (hx - 0.132, ey + s * 0.056, hz),
            (hx - 0.132, ey + s * 0.014, hz),
        ]
        objs.append(C.make_pipe(f"LeverHandle_{s}", pts, 0.010, "Steel", bevel_res=4))

    # Overhead hydraulic door closer box + articulated scissor arm
    objs.append(C.make_box("CloserBody", (0.28, -t * 0.5 - 0.028, h - 0.085), (0.21, 0.046, 0.054), "Steel", bevel=0.005))
    objs.append(C.make_pipe("CloserArm", [(0.22, -t * 0.5 - 0.03, h - 0.055), (0.34, -t * 0.5 - 0.16, h + 0.01),
                                          (0.42, -t * 0.5 - 0.02, h + 0.03)], 0.008, "Steel", bevel_res=3))

    # 3 Hinge leaf plates
    for k, hz_p in enumerate((0.28, 1.08, 1.88)):
        objs.append(C.make_box(f"HingeLeaf_{k}", (0.025, -t * 0.5 - 0.002, hz_p), (0.048, 0.005, 0.10), "Steel", bevel=0.001))
    return objs


def build_steel_window(width, name_tag):
    """Authentic 1987 Vietnamese rolled-steel hospital casement window with:
    - Deep concrete/terrazzo interior sill + exterior drip hood (ô văng)
    - Rolled steel casement frames & mullions
    - 6 angled high transom louver glass blades (ô chớp kính)
    - Welded Vietnamese geometric iron security grille (hoa sắt bảo vệ)
    - Espagnolette window latches
    Pivot at center X=0, wall center Y=0, sill base Z=1.09 m (matching HospitalAuthoring Window()!)."""
    objs = []
    sill_z = 1.09
    top_z = 2.94
    h = top_z - sill_z  # 1.85 m
    transom_z = 2.40
    wall_t = 0.24

    # Beveled terrazzo interior window sill (protruding inward -Y by 4.5 cm, outward +Y by 3.5 cm)
    objs.append(C.make_box("TerrazzoSill", (0, -0.008, sill_z + 0.028), (width + 0.08, wall_t + 0.09, 0.056), "BaseTrim", bevel=0.007))
    # Exterior concrete rain hood (ô văng) above window
    objs.append(C.make_box("ExteriorRainHood", (0, 0.24, top_z + 0.04), (width + 0.22, 0.42, 0.085), "PlasterR", bevel=0.010, rot=(math.radians(-6), 0, 0)))

    # Outer rolled-steel perimeter frame recessed slightly into masonry wall
    frame_y = 0.015
    frame_d = 0.072
    frame_w = 0.044
    objs.append(C.make_box("FrameBottom", (0, frame_y, sill_z + 0.068), (width, frame_d, frame_w), "Frame", bevel=0.004))
    objs.append(C.make_box("FrameTop", (0, frame_y, top_z - 0.022), (width, frame_d, frame_w), "Frame", bevel=0.004))
    objs.append(C.make_box("TransomBar", (0, frame_y, transom_z), (width, frame_d + 0.012, 0.052), "Frame", bevel=0.004))
    for s in (-1, 1):
        objs.append(C.make_box(f"FrameSide_{s}", (s * (width * 0.5 - frame_w * 0.5), frame_y, (sill_z + top_z) * 0.5),
                               (frame_w, frame_d, h - 0.04), "Frame", bevel=0.004))

    # Casement bays (3 bays for wide > 1.8m, 2 bays for narrow)
    bays = 3 if width > 1.8 else 2
    bay_w = (width - frame_w * 2) / bays
    for i in range(1, bays):
        mx = -width * 0.5 + frame_w + i * bay_w
        objs.append(C.make_box(f"Mullion_{i}", (mx, frame_y, (sill_z + 0.06 + top_z) * 0.5), (0.040, frame_d, h - 0.08), "Frame", bevel=0.003))

    # Inner steel sash frames + horizontal glazing bars + casement latches
    lower_mid_z = (sill_z + 0.08 + transom_z) * 0.5
    lower_h = transom_z - (sill_z + 0.09)
    for b in range(bays):
        bx = -width * 0.5 + frame_w + (b + 0.5) * bay_w
        sw = bay_w - 0.026
        # Inner sash rectangle
        for sx in (-1, 1):
            objs.append(C.make_box(f"SashV_{b}_{sx}", (bx + sx * (sw * 0.5 - 0.012), frame_y - 0.012, lower_mid_z),
                                   (0.024, 0.036, lower_h - 0.02), "MetalPaint", bevel=0.003))
        for sz in (sill_z + 0.10, transom_z - 0.035, sill_z + 0.09 + lower_h * 0.45):
            objs.append(C.make_box(f"SashH_{b}_{sz:.2f}", (bx, frame_y - 0.012, sz), (sw, 0.034, 0.024), "MetalPaint", bevel=0.002))
        # Casement locking latch handle
        objs.append(C.make_box(f"LatchBase_{b}", (bx + sw * 0.45, frame_y - 0.034, lower_mid_z - 0.08), (0.018, 0.020, 0.065), "Steel", bevel=0.002))
        objs.append(C.make_pipe(f"LatchHandle_{b}", [(bx + sw * 0.45, frame_y - 0.044, lower_mid_z - 0.08),
                                                     (bx + sw * 0.45, frame_y - 0.072, lower_mid_z - 0.08),
                                                     (bx + sw * 0.45, frame_y - 0.072, lower_mid_z - 0.16)], 0.006, "Steel", bevel_res=3))

    # High ventilation louver glass blades (6 tilted blades above transom_z!)
    for k in range(6):
        lz = transom_z + 0.055 + k * 0.078
        objs.append(C.make_box(f"LouverBlade_{k}", (0, frame_y, lz), (width - 0.09, 0.086, 0.010), "FrostedGlass",
                               bevel=0.001, rot=(math.radians(34), 0, 0)))
        # Side aluminum louver clips
        for b in range(bays):
            bx = -width * 0.5 + frame_w + (b + 0.5) * bay_w
            for sx in (-1, 1):
                objs.append(C.make_box(f"LouverClip_{k}_{b}_{sx}", (bx + sx * (bay_w * 0.5 - 0.016), frame_y, lz),
                                       (0.014, 0.068, 0.020), "Steel", bevel=0.001, rot=(math.radians(34), 0, 0)))

    # Authentic Vietnamese hospital welded iron security grille (hoa sắt) — casts dramatic shadows!
    grille_y = frame_y - 0.062
    bar_r = 0.0075
    # Horizontal grille bars
    for gz in (sill_z + 0.24, sill_z + 0.68, sill_z + 1.12, transom_z - 0.04):
        objs.append(C.make_cylinder(f"GrilleH_{gz:.2f}", (-width * 0.5 + 0.02, grille_y, gz),
                                    (width * 0.5 - 0.02, grille_y, gz), bar_r, "Frame", segments=10))
    # Vertical grille bars every ~0.16 m
    n_bars = max(6, int(round((width - 0.12) / 0.165)))
    for k in range(n_bars + 1):
        gx = -width * 0.5 + 0.06 + (width - 0.12) * k / n_bars
        objs.append(C.make_cylinder(f"GrilleV_{k}", (gx, grille_y, sill_z + 0.06),
                                    (gx, grille_y, transom_z - 0.02), bar_r, "Frame", segments=10))
    # Decorative mid-band welded diamonds (classic 1980s Vietnamese ironwork!)
    for k in range(n_bars):
        x0 = -width * 0.5 + 0.06 + (width - 0.12) * k / n_bars
        x1 = -width * 0.5 + 0.06 + (width - 0.12) * (k + 1) / n_bars
        xm = (x0 + x1) * 0.5
        zm = sill_z + 0.90
        dh = 0.11
        dw = (x1 - x0) * 0.44
        pts = [(xm, grille_y, zm + dh), (xm + dw, grille_y, zm), (xm, grille_y, zm - dh), (xm - dw, grille_y, zm)]
        objs.append(C.make_pipe(f"GrilleDiamond_{k}", pts, 0.0055, "Frame", bevel_res=3, cyclic=True))

    return objs


def build_column_pilaster():
    """Chamfered reinforced-concrete hospital corridor pilaster (0.28 x 0.38 x 3.60 m)
    with terrazzo cove base plinth, stainless steel corner guards (0-1.35 m), and painted wainscot split."""
    objs = []
    w, d, h = 0.28, 0.38, 3.60
    # Terrazzo cove base plinth
    objs.append(C.make_box("PilasterPlinth", (0, 0, 0.05), (w + 0.024, d + 0.024, 0.10), "BaseTrim", bevel=0.009))
    # Lower painted oil-dado shaft (0.10 to 1.15 m)
    objs.append(C.make_box("PilasterDado", (0, 0, 0.625), (w, d, 1.05), "DadoR", bevel=0.018, segments=3))
    # Crisp painted trim band at 1.15 m
    objs.append(C.make_box("PilasterTrimBand", (0, 0, 1.155), (w + 0.008, d + 0.008, 0.022), "Frame", bevel=0.003))
    # Upper limewash plaster shaft (1.16 to 3.60 m)
    objs.append(C.make_box("PilasterUpper", (0, 0, 2.38), (w, d, 2.44), "PlasterR", bevel=0.018, segments=3))
    # Stainless steel L-section corner protection guards on corridor-facing corners (+X side!)
    for sy in (-1, 1):
        cx = w * 0.5 - 0.006
        cy = sy * (d * 0.5 - 0.006)
        objs.append(C.make_box(f"CornerGuardA_{sy}", (cx + 0.006, cy - sy * 0.022, 0.72), (0.005, 0.055, 1.24), "Steel", bevel=0.001))
        objs.append(C.make_box(f"CornerGuardB_{sy}", (cx - 0.022, cy + sy * 0.006, 0.72), (0.055, 0.005, 1.24), "Steel", bevel=0.001))
    return objs


def build_handrail_bay():
    """3.70 m hospital corridor wooden handrail & lower rubber/steel stretcher crash rail with brackets.
    Centered along Y (-1.85..+1.85), wall mounted at X=0 extending to +X."""
    objs = []
    length = 3.70
    # Rounded hardwood top handrail at Z=0.88 m
    objs.append(C.make_cylinder("WoodGrip", (0.072, -length * 0.5, 0.88), (0.072, length * 0.5, 0.88), 0.022, "Wood", segments=16))
    # Curved end returns into wall
    for s in (-1, 1):
        pts = [(0.072, s * length * 0.5, 0.88), (0.072, s * (length * 0.5 + 0.035), 0.88), (0.0, s * (length * 0.5 + 0.035), 0.88)]
        objs.append(C.make_pipe(f"RailEnd_{s}", pts, 0.021, "Wood", bevel_res=4))
    # Stretcher crash bumper strip at Z=0.72 m
    objs.append(C.make_box("CrashStrip", (0.032, 0, 0.72), (0.034, length + 0.06, 0.11), "WoodDoor", bevel=0.007))
    objs.append(C.make_box("RubberBumper", (0.050, 0, 0.72), (0.010, length + 0.04, 0.052), "Rubber", bevel=0.003))
    # 4 Cast metal wall brackets supporting handrail
    for k in range(4):
        by = -1.45 + k * 0.966
        objs.append(C.make_box(f"WallFlange_{k}", (0.006, by, 0.83), (0.012, 0.045, 0.075), "Steel", bevel=0.002))
        objs.append(C.make_pipe(f"BracketArm_{k}", [(0.01, by, 0.83), (0.072, by, 0.83), (0.072, by, 0.865)], 0.008, "Steel", bevel_res=3))
    return objs


def build_fluorescent_fixture():
    """Authentic 1.24 m Vietnamese dual T8 enameled steel fluorescent batten fixture (máng đèn 1m2).
    Origin at ceiling mount center (0,0,0), hangs downward (-Z). Tube length along Y!"""
    objs = []
    # Trapezoidal sheet-metal ballast trough housing
    objs.append(C.make_box("BallastBase", (0, 0, -0.024), (0.145, 1.25, 0.048), "Ceramic", bevel=0.006))
    # Flared white reflector wings
    for s in (-1, 1):
        objs.append(C.make_box(f"ReflectorWing_{s}", (s * 0.095, 0, -0.054), (0.078, 1.25, 0.007), "Ceramic",
                               bevel=0.002, rot=(0, s * math.radians(24), 0)))
    # End caps + rotary G13 tombstone lamp holders
    for sy in (-1, 1):
        objs.append(C.make_box(f"EndCap_{sy}", (0, sy * 0.625, -0.044), (0.195, 0.014, 0.072), "Ceramic", bevel=0.003))
        for sx in (-0.048, 0.048):
            objs.append(C.make_box(f"Tombstone_{sy}_{sx:.2f}", (sx, sy * 0.605, -0.072), (0.026, 0.022, 0.048), "PlasticIvory", bevel=0.003))
    # Cylindrical fluorescent starter can (tắc te) sticking out side
    objs.append(C.make_cylinder("StarterCan", (0.074, 0.42, -0.035), (0.096, 0.42, -0.035), 0.010, "PlasticIvory", segments=14))
    # Twin T8 glass tubes (Ø26 mm, 1.19 m long) with aluminum end collars
    for i, sx in enumerate((-0.048, 0.048)):
        objs.append(C.make_cylinder(f"T8Tube_{i}", (sx, -0.585, -0.082), (sx, 0.585, -0.082), 0.0135, "DeadTube", segments=18))
        for sy in (-1, 1):
            objs.append(C.make_cylinder(f"TubeCollar_{i}_{sy}", (sx, sy * 0.585, -0.082), (sx, sy * 0.600, -0.082), 0.0142, "Steel", segments=16))
    return objs


def build_ceiling_fan():
    """Authentic 1400 mm Vietnamese 3-blade enameled steel ceiling fan (quạt trần Điện Cơ / Vinawind).
    Separated into 'StaticMount' and 'Rotor' so Unity rotates only the blades/motor bell!
    Origin at ceiling hook (0,0,0), drops down to Z = -0.44 m."""
    static_objs = []
    # Top ceiling canopy bell
    static_objs.append(C.make_lathe("TopCanopy", [(0.012, 0.0), (0.072, -0.005), (0.068, -0.028), (0.022, -0.085), (0.013, -0.095)],
                                    (0, 0, 0), "Ceramic", segments=24))
    # Downrod steel tube
    static_objs.append(C.make_cylinder("Downrod", (0, 0, -0.02), (0, 0, -0.34), 0.011, "Frame", segments=16))
    # Lower canopy cup above motor
    static_objs.append(C.make_lathe("LowerCanopy", [(0.013, -0.25), (0.026, -0.27), (0.056, -0.32), (0.054, -0.335)],
                                    (0, 0, 0), "Ceramic", segments=24))

    rotor_objs = []
    mz = -0.38
    # Ribbed ventilated motor housing bell (lathed silhouette!)
    rotor_objs.append(C.make_lathe("MotorBell", [
        (0.015, mz + 0.048), (0.075, mz + 0.045), (0.118, mz + 0.026),
        (0.124, mz + 0.004), (0.122, mz - 0.016), (0.108, mz - 0.032),
        (0.062, mz - 0.040), (0.0, mz - 0.042)
    ], (0, 0, 0), "MetalPaint", segments=28))
    # Decorative chrome center trim ring around motor equator
    rotor_objs.append(C.make_lathe("MotorTrimRing", [(0.123, mz + 0.005), (0.128, mz + 0.002), (0.128, mz - 0.005), (0.123, mz - 0.008)],
                                   (0, 0, 0), "Steel", segments=28))
    # 3 stamped sheet-metal blades (total span 1.40 m => 0.70 m radius!) with bolted root brackets & pitch
    for i in range(3):
        ang = math.radians(i * 120.0)
        ca, sa = math.cos(ang), math.sin(ang)
        # Blade mounting arm
        ax0, ay0 = ca * 0.11, sa * 0.11
        ax1, ay1 = ca * 0.25, sa * 0.25
        arm = C.make_box(f"BladeArm_{i}", ((ax0 + ax1) * 0.5, (ay0 + ay1) * 0.5, mz - 0.018),
                         (0.15, 0.052, 0.010), "MetalOld", bevel=0.002, rot=(math.radians(8), 0, ang))
        rotor_objs.append(arm)
        # Gently tapered pitched steel blade (0.21..0.69 m)
        bx, by = ca * 0.455, sa * 0.455
        blade = C.make_box(f"Blade_{i}", (bx, by, mz - 0.024), (0.50, 0.135, 0.006), "Ceramic",
                           bevel=0.002, rot=(math.radians(9), math.radians(1.5), ang))
        rotor_objs.append(blade)
        # 2 Bolts holding blade to arm
        for b_off in (-0.016, 0.016):
            rx = ca * 0.235 - sa * b_off
            ry = sa * 0.235 + ca * b_off
            rotor_objs.append(C.make_cylinder(f"BladeBolt_{i}_{b_off:.3f}", (rx, ry, mz - 0.032), (rx, ry, mz - 0.010), 0.005, "Steel", segments=10))

    return {"FanMount": static_objs, "FanRotor": rotor_objs}


def build_wall_outlet():
    """Period Vietnamese surface-mounted hospital bed-head electrical outlet & rocker switch box + conduit stub.
    Wall back at Y=0, protrudes toward -Y (room interior)."""
    objs = []
    # Surface bakelite/plastic mounting box
    objs.append(C.make_box("OutletBackBox", (0, -0.018, 0), (0.145, 0.036, 0.095), "PlasticIvory", bevel=0.005))
    # Beveled front cover plate
    objs.append(C.make_box("OutletFaceplate", (0, -0.038, 0), (0.152, 0.010, 0.102), "PlasticIvory", bevel=0.004))
    # Twin recessed 2-prong sockets (left & middle)
    for sx in (-0.042, 0.004):
        objs.append(C.make_cylinder(f"SocketWell_{sx:.3f}", (sx, -0.041, 0), (sx, -0.045, 0), 0.017, "Black", segments=16))
        for pin in (-0.008, 0.008):
            objs.append(C.make_cylinder(f"PinHole_{sx:.3f}_{pin:.3f}", (sx + pin, -0.043, 0), (sx + pin, -0.047, 0), 0.0032, "Black", segments=10))
    # Classic vintage red/ivory rocker switch on right side
    objs.append(C.make_box("SwitchBezel", (0.048, -0.044, 0), (0.024, 0.008, 0.042), "Black", bevel=0.002))
    objs.append(C.make_box("SwitchRocker", (0.048, -0.050, 0.002), (0.018, 0.014, 0.032), "RedPaint", bevel=0.002, rot=(math.radians(14), 0, 0)))
    # Top galvanized conduit riser stub + saddle clamp
    objs.append(C.make_cylinder("ConduitStub", (0, -0.016, 0.045), (0, -0.016, 0.42), 0.011, "Frame", segments=12))
    objs.append(C.make_box("ConduitClamp", (0, -0.018, 0.24), (0.038, 0.026, 0.018), "Steel", bevel=0.002))
    # Brass faceplate corner screws
    for sx in (-0.064, 0.064):
        for sz in (-0.038, 0.038):
            objs.append(C.make_cylinder(f"Screw_{sx:.2f}_{sz:.2f}", (sx, -0.041, sz), (sx, -0.046, sz), 0.003, "Brass", segments=10))
    return objs


def build_conduit_junction_run():
    """4.0 m modular surface electrical conduit run with twin parallel pipes, rectangular junction box,
    saddle clamps, and vertical switch drop — replaces featureless corridor box."""
    objs = []
    length = 4.0
    # Main upper and lower galvanized conduit pipes along Y
    objs.append(C.make_cylinder("ConduitMain", (0.018, -length * 0.5, 0.022), (0.018, length * 0.5, 0.022), 0.011, "Frame", segments=12))
    objs.append(C.make_cylinder("ConduitSub", (0.018, -length * 0.5, -0.022), (0.018, length * 0.5, -0.022), 0.009, "Frame", segments=12))
    # Cast junction box in center with lid bolts
    objs.append(C.make_box("JunctionBox", (0.024, 0, 0), (0.048, 0.115, 0.095), "MetalOld", bevel=0.005))
    objs.append(C.make_box("JunctionLid", (0.050, 0, 0), (0.008, 0.122, 0.102), "MetalPaint", bevel=0.003))
    # 5 saddle clamps along the 4 m bay
    for k in range(-2, 3):
        if k == 0:
            continue
        cy = k * 0.85
        objs.append(C.make_box(f"Saddle_{k}", (0.016, cy, 0), (0.034, 0.020, 0.072), "Steel", bevel=0.002))
    return objs


def build_curtain_assembly():
    """Hospital ceiling privacy curtain track + suspension hangers + sliding ring hooks + sculpted pleated curtain
    (2.38 m wide x 1.92 m tall, origin at X=0 start of track, Z=0 top of curtain).
    Includes classic upper mesh ventilation header band & soft gravity pleats."""
    track_objs = []
    w = 2.38
    # Ceiling anodized aluminum I-track rail
    track_objs.append(C.make_box("TrackRail", (w * 0.5, 0, 0.075), (w + 0.12, 0.028, 0.022), "Steel", bevel=0.003))
    # Ceiling suspension rods rising 0.85 m up to ceiling
    for hx in (0.05, w * 0.5, w - 0.05):
        track_objs.append(C.make_cylinder(f"TrackHanger_{hx:.2f}", (hx, 0, 0.085), (hx, 0, 0.86), 0.008, "Steel", segments=10))
        track_objs.append(C.make_cylinder(f"CeilingFlange_{hx:.2f}", (hx, 0, 0.845), (hx, 0, 0.865), 0.028, "Steel", segments=14))
    # 14 sliding metal curtain hooks & eyelet grommets
    for k in range(14):
        gx = 0.03 + (w - 0.06) * k / 13.0
        track_objs.append(C.make_pipe(f"Hook_{k}", [(gx, 0, 0.065), (gx, 0.012, 0.038), (gx, 0, 0.008)], 0.0035, "Steel", bevel_res=3))

    # Sculpted pleated hospital fabric curtain with deep organic folds (export separately for cloth motion!)
    def curtain_fn(u, v, cw, ch):
        # u along width [0..1], v downward [0..1]
        x = u * cw
        # Layered harmonic pleats (primary wide folds + secondary natural drapery)
        pleat = math.sin(u * 13.0 * math.pi + 0.25 * v) * (0.048 + 0.024 * v)
        secondary = math.sin(u * 27.0 * math.pi - 0.8 * v) * 0.012 * v
        y = pleat + secondary
        z = -v * ch - 0.012 * math.sin(u * 19.0) * v
        return (x, y, z)

    cloth = C.make_cloth_grid("CurtainFabric", w, 1.92, 48, 18, curtain_fn, (0, 0, 0), "Curtain", thickness=0.005, uv_tile=(3.5, 4.0))
    return {"CurtainTrack": track_objs, "CurtainCloth": [cloth]}


def build_enamel_sign_plate():
    """Reusable beveled sheet-metal room plaque backing (1.0 x 0.38 m unit template scaled per sign) with 4 screw heads."""
    objs = []
    objs.append(C.make_box("EnamelBacking", (0, 0.006, 0), (1.0, 0.012, 0.38), "MetalOld", bevel=0.004))
    for sx in (-0.455, 0.455):
        for sz in (-0.145, 0.145):
            objs.append(C.make_cylinder(f"MountScrew_{sx:.2f}_{sz:.2f}", (sx, -0.001, sz), (sx, -0.006, sz), 0.009, "Brass", segments=10))
    return objs


def run_all():
    C.export_asset("CTCC_ENV_WardDoorFrame", build_ward_door_frame, make_lods=True, render=True, cam_dir=(1.2, -2.0, 0.5))
    C.export_asset("CTCC_ENV_WardDoorLeaf", build_ward_door_leaf, make_lods=True, render=True, cam_dir=(1.1, -2.1, 0.4))
    C.export_asset("CTCC_ENV_SteelWindow_Wide", lambda: build_steel_window(2.14, "Wide"), make_lods=True, render=True, cam_dir=(0.9, -2.2, 0.3))
    C.export_asset("CTCC_ENV_SteelWindow_Mid", lambda: build_steel_window(2.09, "Mid"), make_lods=True, render=False)
    C.export_asset("CTCC_ENV_SteelWindow_Narrow", lambda: build_steel_window(1.28, "Narrow"), make_lods=True, render=True, cam_dir=(0.9, -2.2, 0.3))
    C.export_asset("CTCC_ENV_ColumnPilaster", build_column_pilaster, make_lods=True, render=True, cam_dir=(1.5, -1.6, 0.4))
    C.export_asset("CTCC_ENV_HandrailBay", build_handrail_bay, make_lods=True, render=True, cam_dir=(1.8, -1.4, 0.7))
    C.export_asset("CTCC_ENV_FluorescentFixture", build_fluorescent_fixture, make_lods=True, render=True, cam_dir=(1.2, -1.5, -0.8))
    C.export_asset("CTCC_ENV_CeilingFan", build_ceiling_fan, make_lods=False, render=True, cam_dir=(1.4, -1.5, -0.7), separate_parts=True)
    C.export_asset("CTCC_ENV_WallOutlet", build_wall_outlet, make_lods=False, render=True, cam_dir=(0.8, -1.9, 0.5))
    C.export_asset("CTCC_ENV_ConduitRun", build_conduit_junction_run, make_lods=True, render=False)
    C.export_asset("CTCC_ENV_CurtainAssembly", build_curtain_assembly, make_lods=False, render=True, cam_dir=(1.0, -2.1, 0.3), separate_parts=True)
    C.export_asset("CTCC_ENV_EnamelSignPlate", build_enamel_sign_plate, make_lods=False, render=False)


if __name__ == "__main__":
    run_all()
