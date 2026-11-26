"""Hospital Furniture & Medical Equipment Kit (1987-2005 Vietnam) built in Blender 4.5.

Preserves exact Core 03 & layout.json footprints (WardBed 0.88x2.06m, Trolley 0.72x2.05m, MedicalCabinet 1.36x0.48m).
"""

import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import common as C


def caster_wheel(objs, prefix, x, y, wheel_r=0.045, z_top=0.185):
    """Detailed swivel hospital caster with rubber tire, steel hubcap, fork yoke, swivel stem & foot brake lever."""
    objs.append(C.make_cylinder(f"{prefix}_Stem", (x, y, wheel_r * 2 + 0.012), (x, y, z_top), 0.017, "Steel", segments=14))
    objs.append(C.make_cylinder(f"{prefix}_Bearing", (x, y, wheel_r * 2 + 0.008), (x, y, wheel_r * 2 + 0.028), 0.025, "Steel", segments=16))
    # Rubber wheel tire + recessed steel rim
    objs.append(C.make_cylinder(f"{prefix}_Tire", (x - 0.018, y, wheel_r), (x + 0.018, y, wheel_r), wheel_r, "Rubber", segments=24, bevel=0.005))
    objs.append(C.make_cylinder(f"{prefix}_Hub", (x - 0.021, y, wheel_r), (x + 0.021, y, wheel_r), wheel_r * 0.56, "Steel", segments=18))
    objs.append(C.make_cylinder(f"{prefix}_Axle", (x - 0.028, y, wheel_r), (x + 0.028, y, wheel_r), 0.009, "Steel", segments=12))
    # Twin stamped steel yoke cheeks
    for side in (-1, 1):
        objs.append(C.make_box(f"{prefix}_Yoke_{side}", (x + side * 0.024, y, wheel_r + 0.028), (0.008, 0.046, 0.068), "Steel", bevel=0.002))
    # Foot brake pedal tab
    objs.append(C.make_box(f"{prefix}_Brake", (x, y - 0.045, wheel_r + 0.042), (0.028, 0.034, 0.008), "RedPaint", bevel=0.002, rot=(math.radians(18), 0, 0)))


def build_ward_bed():
    """Hero Vietnamese tubular steel hospital ward bed (exact 0.88 x 2.06 m footprint).
    Features vertical head/foot spindles, brazed collars, crank handle, folding side rail,
    sculpted piped vinyl mattress, tucked wrinkled cotton sheet, indented pillow, draped blanket & foot chart board."""
    objs = []
    # Main longitudinal steel rails & 4 swivel casters
    for x in (-0.385, 0.385):
        objs.append(C.make_box(f"SideFrame_{x:.2f}", (x, 0, 0.545), (0.036, 1.92, 0.048), "MetalPaint", bevel=0.005))
        for y in (-0.875, 0.875):
            caster_wheel(objs, f"BedCaster_{x:.2f}_{y:.2f}", x, y, wheel_r=0.046, z_top=0.19)
            objs.append(C.make_cylinder(f"Leg_{x:.2f}_{y:.2f}", (x, y, 0.175), (x, y, 0.565), 0.021, "MetalPaint", segments=16))
            objs.append(C.make_cylinder(f"LegCollar_{x:.2f}_{y:.2f}", (x, y, 0.515), (x, y, 0.575), 0.027, "MetalPaint", segments=16, bevel=0.003))
        # Lower bracing bar
        objs.append(C.make_cylinder(f"LowerRail_{x:.2f}", (x, -0.875, 0.31), (x, 0.875, 0.31), 0.013, "MetalPaint", segments=12))

    # Head (+Y, higher 1.12m) & Foot (-Y, 0.91m) bent U-tube frames with classic Vietnamese vertical spindles!
    for idx, y in enumerate((-0.96, 0.96)):
        top_z = 1.12 if y > 0 else 0.91
        objs.append(C.make_pipe(f"EndArch_{idx}", C.make_rounded_u(y, 0.22, top_z, 0.385, radius=0.085), 0.021, "MetalPaint", bevel_res=5))
        objs.append(C.make_cylinder(f"EndCrossLow_{idx}", (-0.385, y, 0.43), (0.385, y, 0.43), 0.016, "MetalPaint", segments=14))
        objs.append(C.make_cylinder(f"EndCrossMid_{idx}", (-0.385, y, 0.62), (0.385, y, 0.62), 0.015, "MetalPaint", segments=14))
        # 5 vertical tubular spindles + solid stamped steel central nameplate panel
        for sx in (-0.28, -0.14, 0.0, 0.14, 0.28):
            objs.append(C.make_cylinder(f"Spindle_{idx}_{sx:.2f}", (sx, y, 0.62), (sx, y, top_z - 0.015), 0.0095, "MetalPaint", segments=12))
        objs.append(C.make_box(f"EndPanel_{idx}", (0, y, 0.72), (0.64, 0.018, 0.16), "MetalPaint", bevel=0.006))
        for rx in (-0.24, 0.24):
            objs.append(C.make_cylinder(f"PanelRivet_{idx}_{rx:.2f}", (rx, y - 0.012, 0.72), (rx, y + 0.012, 0.72), 0.007, "Steel", segments=10))
        # Rubber corner wall bumpers at corners
        for sx in (-0.415, 0.415):
            objs.append(C.make_cylinder(f"WallBumper_{idx}_{sx:.2f}", (sx, y, 0.535), (sx, y, 0.560), 0.032, "Rubber", segments=16))

    # Folding manual bed elevation crank handle at foot (-Y)
    objs.append(C.make_cylinder("CrankShaft", (0.14, -0.96, 0.43), (0.14, -1.02, 0.43), 0.010, "Steel", segments=12))
    objs.append(C.make_pipe("CrankArm", [(0.14, -1.015, 0.43), (0.14, -1.025, 0.31), (0.14, -1.07, 0.31)], 0.008, "Steel", bevel_res=3))
    objs.append(C.make_cylinder("CrankGrip", (0.14, -1.03, 0.31), (0.14, -1.095, 0.31), 0.012, "Rubber", segments=12))

    # Bed-foot hanging aluminum patient medical chart clipboard (iconic hospital detail!)
    objs.append(C.make_box("ChartBoardBack", (-0.12, -0.985, 0.74), (0.24, 0.008, 0.31), "Steel", bevel=0.003))
    objs.append(C.make_box("ChartBoardPaper", (-0.12, -0.992, 0.735), (0.205, 0.004, 0.27), "Paper", bevel=0.001))
    objs.append(C.make_box("ChartClip", (-0.12, -0.996, 0.875), (0.085, 0.014, 0.025), "Steel", bevel=0.002))
    for hx in (-0.19, -0.05):
        objs.append(C.make_pipe(f"ChartHook_{hx:.2f}", [(hx, -0.985, 0.885), (hx, -0.985, 0.925), (hx, -0.94, 0.925)], 0.005, "Steel", bevel_res=3))

    # Welded deck slats
    for i in range(10):
        objs.append(C.make_box(f"DeckSlat_{i}", (0, -0.82 + i * 0.182, 0.562), (0.74, 0.072, 0.016), "MetalPaint", bevel=0.003))

    # Folding steel side protection rail (left side -X) with 3 vertical pickets & locking pin
    rx = -0.415
    objs.append(C.make_pipe("SideRailLoop", [(rx, 0.56, 0.56), (rx, 0.53, 0.83), (rx, 0.47, 0.88),
                                             (rx, -0.46, 0.88), (rx, -0.53, 0.83), (rx, -0.56, 0.56)], 0.014, "Steel", bevel_res=4))
    for sy in (-0.28, 0.0, 0.28):
        objs.append(C.make_cylinder(f"SideRailBar_{sy:.2f}", (rx, sy, 0.565), (rx, sy, 0.875), 0.0085, "Steel", segments=12))
    for sy in (-0.56, 0.56):
        objs.append(C.make_box(f"RailBracket_{sy:.2f}", (rx + 0.016, sy, 0.56), (0.036, 0.044, 0.046), "MetalOld", bevel=0.003))

    # Sculpted vinyl-encased hospital foam mattress with subtle patient body depression & piping edge
    def mattress_fn(u, v, mw, ml):
        x = (u - 0.5) * mw
        y = (v - 0.5) * ml
        # Soft pillowed crown with subtle worn center sag & edge bevel
        ex = max(0.0, 1.0 - (abs(x) / (mw * 0.5)) ** 6)
        ey = max(0.0, 1.0 - (abs(y) / (ml * 0.5)) ** 8)
        crown = 0.032 * (ex * ey) ** 0.55
        sag = -0.018 * math.exp(-((x / 0.24) ** 2 + ((y - 0.05) / 0.52) ** 2))
        wrinkle = 0.003 * math.sin(u * 22.0 + v * 9.0) * ex * ey
        return (x, y, crown + sag + wrinkle)

    objs.append(C.make_box("MattressBase", (0, 0.0, 0.625), (0.79, 1.84, 0.115), "Mattress", bevel=0.028, segments=4))
    objs.append(C.make_cloth_grid("MattressTopSheet", 0.80, 1.85, 24, 34, mattress_fn, (0, 0, 0.676), "Cotton", thickness=0.008, uv_tile=(2.5, 4.5)))
    # Fitted sheet tucked perimeter piping
    objs.append(C.make_box("SheetTuckBand", (0, 0, 0.618), (0.804, 1.854, 0.042), "Cotton", bevel=0.012, segments=3))

    # Sculpted plump cotton hospital pillow with realistic head indentation & corner ears
    def pillow_fn(u, v, pw, pl):
        x = (u - 0.5) * pw
        y = (v - 0.5) * pl
        ex = math.sin(u * math.pi)
        ey = math.sin(v * math.pi)
        loft = 0.095 * (ex * ey) ** 0.62
        indent = -0.028 * math.exp(-((x / 0.14) ** 2 + (y / 0.10) ** 2))
        crease = 0.006 * math.sin(u * 14.0 + v * 11.0) * ex * ey
        return (x, y, loft + indent + crease)

    pil = C.make_cloth_grid("PillowTop", 0.56, 0.37, 22, 16, pillow_fn, (0.01, 0.64, 0.708), "Cotton", thickness=0.036, uv_tile=(1.8, 1.4))
    pil.rotation_euler.z = 0.075
    bpy.ops.object.select_all(action="DESELECT")
    pil.select_set(True)
    bpy.context.view_layer.objects.active = pil
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    objs.append(pil)

    # Sculpted folded & naturally draped hospital cotton blanket across lower 2/3 of bed
    # Strictly clears MattressTopSheet crown (max z=0.716) so mattress NEVER pokes through blanket folds!
    def blanket_fn(u, v, bw, bl):
        x = (u - 0.5) * bw + 0.008 * math.sin(v * 15.0 + u * 2.0)
        y = (v - 0.5) * bl + 0.012 * math.sin(u * 9.0 + v * 3.0)
        # Soft side drape starting cleanly past mattress corner (0.398m)
        over_x = max(0.0, abs(x) - 0.398) / 0.095
        droop_x = -0.22 * (min(1.0, over_x) ** 1.55)
        over_foot = max(0.0, (0.12 - v) / 0.12)
        droop_foot = -0.15 * (over_foot ** 1.65)
        # Non-negative organic fabric folds & bunched top cuff so troughs never dip into mattress
        folds = 0.012 * (0.5 + 0.5 * math.sin(u * 29.0 + v * 8.0)) * (0.35 + 0.65 * math.sin(v * 3.1) ** 2)
        folds += 0.006 * (0.5 + 0.5 * math.cos(u * 11.0 - v * 17.0))
        cuff = 0.020 * math.exp(-((v - 0.94) / 0.06) ** 2)
        return (x, y, folds + droop_x + droop_foot + cuff)

    objs.append(C.make_cloth_grid("DrapedBlanket", 0.96, 1.24, 34, 38, blanket_fn, (0, -0.22, 0.728), "Textile", thickness=0.010, uv_tile=(3.0, 4.0)))
    return objs


def build_bedside_cabinet():
    """Authentic Vietnamese enameled steel hospital bedside cabinet (0.49 x 0.43 x 0.78 m).
    Features 3-sided anti-spill gallery top lip, upper pull drawer with chrome handle,
    vented louver lower cabinet door, side stainless towel bar, rubber-tipped legs & interior shelf."""
    objs = []
    w, d, h = 0.49, 0.43, 0.78
    # 4 Tubular legs with black rubber floor ferrules
    for sx in (-0.21, 0.21):
        for sy in (-0.18, 0.18):
            objs.append(C.make_cylinder(f"FootRubber_{sx:.2f}_{sy:.2f}", (sx, sy, 0.0), (sx, sy, 0.032), 0.017, "Rubber", segments=14))
            objs.append(C.make_cylinder(f"LegTube_{sx:.2f}_{sy:.2f}", (sx, sy, 0.030), (sx, sy, 0.115), 0.014, "CabinetPaint", segments=14))

    # Welded steel cabinet carcass (back, left, right, bottom, inner shelf)
    objs.append(C.make_box("BodyBack", (0, d * 0.5 - 0.012, 0.425), (w, 0.024, 0.64), "CabinetPaint", bevel=0.005))
    objs.append(C.make_box("BodyBottom", (0, 0, 0.115), (w, d, 0.024), "CabinetPaint", bevel=0.004))
    objs.append(C.make_box("BodyDivider", (0, 0.005, 0.52), (w - 0.02, d - 0.02, 0.020), "CabinetPaint", bevel=0.003))
    objs.append(C.make_box("InnerShelf", (0, 0.015, 0.31), (w - 0.03, d - 0.04, 0.016), "CabinetPaint", bevel=0.002))
    for s in (-1, 1):
        objs.append(C.make_box(f"BodySide_{s}", (s * (w * 0.5 - 0.012), 0, 0.425), (0.024, d, 0.64), "CabinetPaint", bevel=0.005))

    # Recessed top enamel counter with 3-sided raised anti-spill gallery lip
    objs.append(C.make_box("TopDeck", (0, 0, 0.745), (w + 0.02, d + 0.02, 0.026), "Ceramic", bevel=0.006))
    objs.append(C.make_box("GalleryBack", (0, d * 0.5 + 0.005, 0.772), (w + 0.024, 0.014, 0.034), "CabinetPaint", bevel=0.004))
    for s in (-1, 1):
        objs.append(C.make_box(f"GallerySide_{s}", (s * (w * 0.5 + 0.005), 0.005, 0.772), (0.014, d + 0.016, 0.034), "CabinetPaint", bevel=0.004))

    # Upper pull-out drawer face (slightly cracked open 1.5 cm for realism!) + curved chrome pull
    dy = -d * 0.5 - 0.012
    objs.append(C.make_box("DrawerFront", (0, dy, 0.625), (w - 0.032, 0.024, 0.175), "CabinetPaint", bevel=0.006))
    objs.append(C.make_pipe("DrawerPull", [(-0.085, dy - 0.012, 0.628), (-0.075, dy - 0.040, 0.628),
                                           (0.075, dy - 0.040, 0.628), (0.085, dy - 0.012, 0.628)], 0.0075, "Steel", bevel_res=4))

    # Lower vented steel door with real 3D angled ventilation louvers + chrome latch + enamel inventory tag
    objs.append(C.make_box("LowerDoor", (0, -d * 0.5 - 0.006, 0.315), (w - 0.032, 0.022, 0.375), "CabinetPaint", bevel=0.006))
    for k in range(5):
        lz = 0.20 + k * 0.032
        objs.append(C.make_box(f"DoorLouver_{k}", (0, -d * 0.5 - 0.020, lz), (0.26, 0.014, 0.012), "CabinetPaint",
                               bevel=0.002, rot=(math.radians(-30), 0, 0)))
        objs.append(C.make_box(f"DoorLouverShadow_{k}", (0, -d * 0.5 - 0.016, lz - 0.006), (0.25, 0.006, 0.008), "Black", bevel=0.0))
    objs.append(C.make_pipe("DoorHandle", [(0.16, -d * 0.5 - 0.016, 0.39), (0.16, -d * 0.5 - 0.044, 0.39),
                                           (0.16, -d * 0.5 - 0.044, 0.31), (0.16, -d * 0.5 - 0.016, 0.31)], 0.0065, "Steel", bevel_res=3))
    objs.append(C.make_box("AssetTag", (0, -d * 0.5 - 0.019, 0.465), (0.095, 0.003, 0.032), "Paper", bevel=0.001))

    # Side stainless towel bar on right side (+X)
    objs.append(C.make_pipe("TowelBar", [(w * 0.5, -0.14, 0.65), (w * 0.5 + 0.048, -0.14, 0.65),
                                         (w * 0.5 + 0.048, 0.14, 0.65), (w * 0.5, 0.14, 0.65)], 0.0065, "Steel", bevel_res=3))
    return objs


def build_medical_cabinet():
    """Hero rolling 2-door steel hospital storage/medicine cabinet (tủ sắt kính 2 cánh: 0.48 deep x 1.36 wide x 2.06 tall).
    Oriented with doors facing -X (into Room 101!) matching HospitalAuthoring cupboard at (13.34, 0, 3.62)!
    Features recessed upper framed glass doors revealing interior shelves stocked with archive boxes, linen stacks & bottles,
    lower louvered steel doors, chrome handles, lock plate & heavy-duty rolling casters."""
    objs = []
    # In Blender: X = depth (-0.24..+0.24, doors face -X!), Y = width (-0.68..+0.68, 1.36 m), Z = height (0..2.06 m)
    d, w, h = 0.48, 1.36, 2.06
    base_z = 0.12

    # 4 Heavy-duty casters underneath welded chassis frame
    for sx in (-0.17, 0.17):
        for sy in (-0.56, 0.56):
            caster_wheel(objs, f"CabCaster_{sx:.2f}_{sy:.2f}", sx, sy, wheel_r=0.042, z_top=base_z + 0.01)

    # Welded outer steel shell (back +X, top, bottom, sides, central shelf divider)
    objs.append(C.make_box("CabBack", (d * 0.5 - 0.014, 0, (base_z + h) * 0.5), (0.028, w, h - base_z), "CabinetOld", bevel=0.005))
    objs.append(C.make_box("CabBottom", (0, 0, base_z + 0.018), (d, w, 0.036), "CabinetOld", bevel=0.005))
    objs.append(C.make_box("CabTop", (0, 0, h - 0.022), (d + 0.016, w + 0.016, 0.044), "CabinetOld", bevel=0.007))
    objs.append(C.make_box("CabMidDeck", (0.01, 0, 1.02), (d - 0.03, w - 0.04, 0.026), "CabinetOld", bevel=0.003))
    for sy in (-1, 1):
        objs.append(C.make_box(f"CabSide_{sy}", (0, sy * (w * 0.5 - 0.016), (base_z + h) * 0.5), (d, 0.032, h - base_z), "CabinetOld", bevel=0.006))
    # Interior center mullion
    objs.append(C.make_box("CabMullion", (-d * 0.5 + 0.022, 0, (base_z + h) * 0.5), (0.036, 0.036, h - base_z - 0.04), "CabinetOld", bevel=0.003))

    # Interior upper shelves + authentic stocked hospital supplies visible through glass doors!
    for sz in (1.34, 1.68):
        objs.append(C.make_box(f"UpperShelf_{sz:.2f}", (0.02, 0, sz), (d - 0.08, w - 0.06, 0.018), "Steel", bevel=0.002))

    # Stocked interior items on shelves (adds huge depth & realism through the glass panes!)
    # Folded stacks of hospital sheets on left upper shelves
    for s_idx, sz in enumerate((1.045, 1.36)):
        for stack_k in range(3):
            objs.append(C.make_box(f"LinenFold_{s_idx}_{stack_k}", (0.04, -0.38 + (stack_k % 2) * 0.015, sz + 0.03 + stack_k * 0.052),
                                   (0.30, 0.36, 0.048), "CottonOld" if stack_k % 2 == 0 else "TextileOld", bevel=0.014, segments=3))
    # Stacked bound archive boxes & patient ledger binders on right upper shelf
    for k in range(5):
        objs.append(C.make_box(f"LedgerBinder_{k}", (0.03, 0.16 + k * 0.072, 1.18), (0.28, 0.056, 0.29),
                               "KraftPaper" if k % 2 == 0 else "WoodDoor", bevel=0.005, rot=(math.radians((k - 2) * 2.5), 0, 0)))
    # Glass saline & antiseptic bottles on top shelf
    for k, by in enumerate((-0.44, -0.32, -0.20, 0.22, 0.34, 0.48)):
        bh = 0.17 if k % 2 == 0 else 0.13
        br = 0.036 if k % 2 == 0 else 0.028
        objs.append(C.make_cylinder(f"Bottle_{k}", (0.04, by, 1.69), (0.04, by, 1.69 + bh), br,
                                    "SalineFluid" if k % 3 != 0 else "Wood", segments=14))
        objs.append(C.make_cylinder(f"BottleCap_{k}", (0.04, by, 1.69 + bh), (0.04, by, 1.69 + bh + 0.022), br * 0.45, "PlasticIvory", segments=12))
    # Cardboard supply box on middle right shelf
    objs.append(C.make_box("SupplyCarton", (0.04, 0.36, 1.49), (0.31, 0.42, 0.23), "KraftPaper", bevel=0.005))

    # Twin Front Doors (facing -X): Upper framed glass doors + Lower solid louvered doors!
    fx = -d * 0.5 - 0.012
    door_w = w * 0.5 - 0.028
    for side in (-1, 1):
        dy = side * (door_w * 0.5 + 0.012)
        # Upper glass-frame door stiles & rails (Z = 1.04..2.02)
        u_mid_z = 1.53
        u_h = 0.96
        objs.append(C.make_box(f"UpDoorTop_{side}", (fx, dy, 2.00), (0.026, door_w, 0.055), "CabinetOld", bevel=0.004))
        objs.append(C.make_box(f"UpDoorBot_{side}", (fx, dy, 1.06), (0.026, door_w, 0.055), "CabinetOld", bevel=0.004))
        for es in (-1, 1):
            objs.append(C.make_box(f"UpDoorStile_{side}_{es}", (fx, dy + es * (door_w * 0.5 - 0.028), u_mid_z),
                                   (0.026, 0.056, u_h), "CabinetOld", bevel=0.004))
        # Recessed dusty cabinet glass pane
        objs.append(C.make_box(f"UpDoorGlass_{side}", (fx + 0.004, dy, u_mid_z), (0.006, door_w - 0.09, u_h - 0.09), "Glass", bevel=0.0))

        # Lower solid steel panel door (Z = 0.15..1.00) with stamped inset + 6 real 3D louver slats!
        l_mid_z = 0.58
        l_h = 0.85
        objs.append(C.make_box(f"LowDoor_{side}", (fx, dy, l_mid_z), (0.026, door_w, l_h), "CabinetOld", bevel=0.005))
        for k in range(6):
            lz = 0.26 + k * 0.036
            objs.append(C.make_box(f"CabLouver_{side}_{k}", (fx - 0.016, dy, lz), (0.014, door_w * 0.62, 0.012),
                                   "CabinetOld", bevel=0.002, rot=(0, math.radians(-30), 0)))
            objs.append(C.make_box(f"CabLouverSlot_{side}_{k}", (fx - 0.013, dy, lz - 0.006), (0.005, door_w * 0.60, 0.008), "Black", bevel=0.0))

        # Curved stainless vertical door handles near center seam
        hy = side * 0.068
        for hz0, hz1 in ((1.14, 1.32), (0.78, 0.94)):
            objs.append(C.make_pipe(f"CabHandle_{side}_{hz0:.2f}", [(fx - 0.012, hy, hz0), (fx - 0.044, hy, hz0),
                                                                    (fx - 0.044, hy, hz1), (fx - 0.012, hy, hz1)], 0.007, "Steel", bevel_res=3))

    # Central lock cylinder + stainless lock plate
    objs.append(C.make_cylinder("CabLockUp", (fx - 0.010, 0.032, 1.23), (fx - 0.022, 0.032, 1.23), 0.011, "Brass", segments=14))
    objs.append(C.make_cylinder("CabLockLow", (fx - 0.010, 0.032, 0.86), (fx - 0.022, 0.032, 0.86), 0.011, "Brass", segments=14))
    return objs


def build_iv_stand():
    """Detailed stainless steel 5-wheel rolling hospital IV pole (cây truyền dịch inox, 1.92 m tall)
    with telescoping shaft, locking thumbscrew collar, twin spiral hooks, hanging saline bottle & coiled drip tubing."""
    objs = []
    # 5-star rolling base with small hospital casters
    objs.append(C.make_cylinder("BaseHub", (0, 0, 0.068), (0, 0, 0.135), 0.032, "Steel", segments=18, bevel=0.004))
    for i in range(5):
        ang = math.radians(i * 72.0)
        ca, sa = math.cos(ang), math.sin(ang)
        objs.append(C.make_pipe(f"StarLeg_{i}", [(ca * 0.02, sa * 0.02, 0.105), (ca * 0.14, sa * 0.14, 0.088),
                                                 (ca * 0.26, sa * 0.26, 0.072)], 0.012, "Steel", bevel_res=4))
        cx, cy = ca * 0.26, sa * 0.26
        objs.append(C.make_cylinder(f"IVWheel_{i}", (cx - sa * 0.012, cy + ca * 0.012, 0.026),
                                    (cx + sa * 0.012, cy - ca * 0.012, 0.026), 0.026, "Rubber", segments=16))
        objs.append(C.make_cylinder(f"IVWheelPin_{i}", (cx, cy, 0.035), (cx, cy, 0.072), 0.008, "Steel", segments=10))

    # Lower outer tube (0.12..1.18 m) + knurled black locking collar + upper telescoping rod (1.18..1.90 m)
    objs.append(C.make_cylinder("OuterPole", (0, 0, 0.12), (0, 0, 1.18), 0.014, "Steel", segments=16))
    objs.append(C.make_cylinder("LockCollar", (0, 0, 1.155), (0, 0, 1.215), 0.021, "Black", segments=16, bevel=0.003))
    objs.append(C.make_cylinder("ThumbScrew", (0.018, 0, 1.185), (0.052, 0, 1.185), 0.006, "Steel", segments=10))
    objs.append(C.make_cylinder("ThumbKnob", (0.048, 0, 1.185), (0.062, 0, 1.185), 0.015, "Black", segments=14, bevel=0.003))
    objs.append(C.make_cylinder("InnerPole", (0, 0, 1.18), (0, 0, 1.90), 0.0095, "Steel", segments=16))
    objs.append(C.make_sphere("TopFinial", (0, 0, 1.905), (0.026, 0.026, 0.026), "Steel"))

    # Twin curved stainless IV bottle hooks (ram-horn loops)
    for s in (-1, 1):
        pts = [
            (0, 0, 1.88),
            (s * 0.075, 0, 1.88),
            (s * 0.125, 0, 1.845),
            (s * 0.145, 0, 1.875),
            (s * 0.120, 0, 1.905),
        ]
        objs.append(C.make_pipe(f"IVHook_{s}", pts, 0.0045, "Steel", bevel_res=3))

    # Hanging 500 ml translucent glass/plastic saline bottle on +X hook (iconic hospital silhouette!)
    bx = 0.128
    objs.append(C.make_pipe("BottleStrap", [(bx, 0, 1.855), (bx, 0.038, 1.79), (bx, 0.038, 1.68), (bx, -0.038, 1.68),
                                            (bx, -0.038, 1.79), (bx, 0, 1.855)], 0.003, "PlasticIvory", bevel_res=3))
    objs.append(C.make_lathe("SalineBottle", [
        (0.0, 1.815), (0.036, 1.812), (0.039, 1.795), (0.039, 1.665),
        (0.026, 1.635), (0.015, 1.622), (0.015, 1.595), (0.0, 1.595)
    ], (bx, 0, 0), "SalineFluid", segments=20))
    objs.append(C.make_cylinder("BottleStopper", (bx, 0, 1.585), (bx, 0, 1.608), 0.016, "Rubber", segments=14))
    objs.append(C.make_cylinder("BottleLabel", (bx, 0, 1.69), (bx, 0, 1.765), 0.0402, "Paper", segments=20, cap=False))
    # Clear drip chamber & hanging coiled IV tube!
    objs.append(C.make_cylinder("DripChamber", (bx, 0, 1.51), (bx, 0, 1.582), 0.0085, "SalineFluid", segments=14))
    tube_pts = [
        (bx, 0, 1.51),
        (bx - 0.01, 0.015, 1.38),
        (bx - 0.045, 0.032, 1.24),
        (bx - 0.095, 0.025, 1.12),
        (bx - 0.055, -0.025, 1.05),
        (bx + 0.015, -0.035, 1.15),
        (bx + 0.045, -0.010, 1.28),
        (bx + 0.020, 0.025, 1.16),
    ]
    objs.append(C.make_pipe("IVTubing", tube_pts, 0.0032, "SalineFluid", bevel_res=3))
    objs.append(C.make_box("RollerClamp", (bx - 0.028, 0.024, 1.30), (0.016, 0.014, 0.042), "PlasticIvory", bevel=0.002))
    return objs


def build_transport_trolley():
    """Upgraded hospital transport stretcher trolley (0.72 x 2.05 m exact clearance) with O2 cylinder cradle underneath."""
    objs = []
    for x in (-0.31, 0.31):
        for y in (-0.86, 0.86):
            caster_wheel(objs, f"TrCaster_{x:.2f}_{y:.2f}", x, y, wheel_r=0.056, z_top=0.22)
            objs.append(C.make_cylinder(f"TrPost_{x:.2f}_{y:.2f}", (x, y, 0.21), (x, y, 0.74), 0.021, "Steel", segments=16))
        objs.append(C.make_cylinder(f"TrChassis_{x:.2f}", (x, -0.88, 0.27), (x, 0.88, 0.27), 0.022, "Steel", segments=14))
        objs.append(C.make_cylinder(f"TrBraceA_{x:.2f}", (x, -0.80, 0.28), (x, 0.68, 0.72), 0.011, "Steel", segments=12))
        objs.append(C.make_cylinder(f"TrBraceB_{x:.2f}", (x, 0.80, 0.28), (x, -0.68, 0.72), 0.011, "Steel", segments=12))
    # Under-chassis tray + classic Vietnamese green steel oxygen cylinder!
    objs.append(C.make_box("UnderTray", (0, 0, 0.43), (0.50, 1.04, 0.026), "MetalPaint", bevel=0.005))
    objs.append(C.make_lathe("OxygenBottle", [
        (0.0, 0.0), (0.068, 0.0), (0.072, 0.03), (0.072, 0.56), (0.046, 0.64),
        (0.020, 0.68), (0.020, 0.74), (0.0, 0.74)
    ], (0.0, -0.34, 0.515), "GreenOxygen", segments=20, rot=(math.radians(-90), 0, 0)))
    objs.append(C.make_cylinder("OxygenValve", (0, 0.42, 0.515), (0, 0.46, 0.515), 0.026, "Brass", segments=14))

    # Stretcher deck + rubber corner bumpers + segmented padded leatherette mattress + safety straps
    objs.append(C.make_box("StretcherDeck", (0, 0, 0.76), (0.68, 1.92, 0.056), "MetalPaint", bevel=0.014))
    objs.append(C.make_box("StretcherPad", (0, 0, 0.828), (0.64, 1.87, 0.088), "Mattress", bevel=0.032, segments=4))
    for sy in (-0.36, 0.30):
        objs.append(C.make_box(f"Strap_{sy:.2f}", (0, sy, 0.832), (0.652, 0.052, 0.092), "Rubber", bevel=0.004))
        objs.append(C.make_box(f"StrapBuckle_{sy:.2f}", (0.14, sy, 0.880), (0.055, 0.058, 0.012), "Steel", bevel=0.002))
    for sx in (-0.35, 0.35):
        for sy in (-0.96, 0.96):
            objs.append(C.make_cylinder(f"CornerBumper_{sx:.2f}_{sy:.2f}", (sx, sy, 0.74), (sx, sy, 0.78), 0.038, "Rubber", segments=16))
    for sy in (-0.97, 0.97):
        objs.append(C.make_pipe(f"PushBar_{sy:.2f}", C.make_rounded_u(sy, 0.62, 0.94, 0.315, radius=0.065), 0.017, "Steel", bevel_res=4))
    return objs


def build_waiting_bench():
    """Classic 4-seat Vietnamese hospital corridor steel/perforated waiting bench (2.10 m long)."""
    objs = []
    # Central horizontal spine beam + 2 arched tubular legs
    objs.append(C.make_box("SpineBeam", (0, 0, 0.365), (0.065, 2.02, 0.055), "Black", bevel=0.005))
    for sy in (-0.78, 0.78):
        objs.append(C.make_pipe(f"BenchLeg_{sy:.2f}", [(-0.26, sy, 0.015), (-0.18, sy, 0.35), (0.18, sy, 0.35), (0.26, sy, 0.015)],
                                0.018, "Steel", bevel_res=4))
        for sx in (-0.26, 0.26):
            objs.append(C.make_cylinder(f"LegPad_{sy:.2f}_{sx:.2f}", (sx, sy, 0.0), (sx, sy, 0.022), 0.026, "Rubber", segments=14))
    # 4 contoured curved steel seats & backrests
    for k in range(4):
        cy = -0.765 + k * 0.51
        objs.append(C.make_box(f"SeatPan_{k}", (-0.02, cy, 0.43), (0.42, 0.45, 0.028), "Steel", bevel=0.010, rot=(0, math.radians(-5), 0)))
        objs.append(C.make_box(f"SeatBack_{k}", (0.21, cy, 0.66), (0.028, 0.44, 0.41), "Steel", bevel=0.010, rot=(0, math.radians(10), 0)))
        objs.append(C.make_pipe(f"SeatBrace_{k}", [(-0.08, cy, 0.41), (0.18, cy, 0.42), (0.22, cy, 0.64)], 0.012, "Black", bevel_res=3))
    # Curved end armrests
    for sy in (-1.01, 1.01):
        objs.append(C.make_pipe(f"Armrest_{sy:.2f}", [(-0.18, sy, 0.41), (-0.18, sy, 0.59), (0.18, sy, 0.59), (0.18, sy, 0.41)],
                                0.014, "Steel", bevel_res=4))
    return objs


def build_reception_props():
    """Hero Reception / Duty Station tabletop storytelling props (placed atop reception counter at LOC02):
    - Vintage 2005 push-button landline desk telephone with handset & coiled cord
    - Articulated hospital green enameled desk lamp
    - Open cloth-bound hospital duty ledger (sổ trực) with bookmark ribbon & pen
    - Stacked manila folders & wooden document tray"""
    objs = []
    # Open duty ledger (sổ trực) at (0.33, -0.19, 0.0) relative to counter top
    lx, ly = 0.0, 0.0
    objs.append(C.make_box("LedgerCoverL", (lx - 0.155, ly, 0.008), (0.30, 0.36, 0.012), "WoodDoor", bevel=0.003, rot=(0, math.radians(2), 0)))
    objs.append(C.make_box("LedgerCoverR", (lx + 0.155, ly, 0.008), (0.30, 0.36, 0.012), "WoodDoor", bevel=0.003, rot=(0, math.radians(-2), 0)))
    objs.append(C.make_box("LedgerSpine", (lx, ly, 0.006), (0.036, 0.365, 0.014), "WoodDoor", bevel=0.004))
    objs.append(C.make_box("LedgerPagesL", (lx - 0.145, ly, 0.022), (0.275, 0.34, 0.020), "Paper", bevel=0.005, rot=(0, math.radians(3), 0)))
    objs.append(C.make_box("LedgerPagesR", (lx + 0.145, ly, 0.022), (0.275, 0.34, 0.020), "Paper", bevel=0.005, rot=(0, math.radians(-3), 0)))
    # Ballpoint pen resting diagonally on ledger
    objs.append(C.make_cylinder("DeskPen", (lx + 0.08, ly - 0.07, 0.038), (lx + 0.21, ly + 0.04, 0.038), 0.0045, "EnamelBlue", segments=10))

    # Vintage 2000s ivory push-button landline desk phone at (-0.18, 0.44, 0)
    # Flat-bottomed grounded chassis (z=0.0 solid on ceramic counter!) with sloped keypad face & coiled cord
    px, py = -0.18, 0.44
    yaw_p = 0.14
    cp, sp = math.cos(yaw_p), math.sin(yaw_p)

    def ppt(lx, ly, lz):
        return (px + lx * cp - ly * sp, py + lx * sp + ly * cp, lz)

    # Flat grounded plinth + tapered upper keypad wedge + raised cradle shoulder (zero floating!)
    objs.append(C.make_box("PhonePlinth", ppt(0.0, 0.0, 0.016), (0.182, 0.214, 0.032), "PlasticIvory", bevel=0.006, rot=(0, 0, yaw_p)))
    objs.append(C.make_box("PhoneCradleDeck", ppt(-0.044, 0.0, 0.044), (0.088, 0.202, 0.054), "PlasticIvory", bevel=0.009, rot=(0, 0, yaw_p)))
    objs.append(C.make_box("PhoneKeypadSlope", ppt(0.042, 0.008, 0.038), (0.094, 0.172, 0.038), "PlasticIvory", bevel=0.008, rot=(math.radians(11), 0, yaw_p)))
    # Rubber feet & cradle prongs
    for fy in (-0.064, 0.064):
        objs.append(C.make_box(f"PhoneProng_{fy:.2f}", ppt(-0.044, fy, 0.078), (0.044, 0.014, 0.022), "PlasticIvory", bevel=0.003, rot=(0, 0, yaw_p)))
    # Sloped 4x3 keypad buttons flush on keypad deck
    for row in range(4):
        for col in range(3):
            kx = 0.016 + col * 0.025
            ky = -0.052 + row * 0.026
            kz = 0.049 + row * 0.0048
            objs.append(C.make_box(f"PhoneKey_{row}_{col}", ppt(kx, ky, kz),
                                   (0.017, 0.015, 0.010), "Black", bevel=0.002, rot=(math.radians(11), 0, yaw_p)))
    # Grounded coiled cord loop connecting phone base to handset
    cord_pts = [ppt(-0.095 - 0.022 * math.sin(k * 0.9), -0.095 - k * 0.012, 0.016 + 0.007 * abs(math.cos(k * 1.4))) for k in range(7)]
    cord_pts.append(ppt(-0.068, -0.095, 0.072))
    objs.append(C.make_pipe("PhoneCord", cord_pts, 0.0042, "Black", bevel_res=3))
    # Cradle handset resting solidly in prongs (names start with Handset for build_ch01_rebirth.py!)
    objs.append(C.make_pipe("HandsetArc", [ppt(-0.048, -0.092, 0.094), ppt(-0.048, 0.0, 0.114), ppt(-0.048, 0.092, 0.094)],
                            0.0165, "PlasticIvory", bevel_res=4))
    for ey in (-0.092, 0.092):
        objs.append(C.make_cylinder(f"HandsetCup_{ey:.2f}", ppt(-0.048, ey, 0.070), ppt(-0.048, ey, 0.100), 0.027, "PlasticIvory", segments=16))

    # Vintage green enameled desk lamp at (-0.22, -0.58, 0)
    dx, dy = -0.22, -0.58
    objs.append(C.make_lathe("LampBase", [(0.0, 0.0), (0.082, 0.0), (0.080, 0.020), (0.032, 0.035), (0.016, 0.042)], (dx, dy, 0), "MetalPaint", segments=22))
    objs.append(C.make_pipe("LampNeck", [(dx, dy, 0.038), (dx - 0.04, dy, 0.22), (dx + 0.05, dy + 0.03, 0.37), (dx + 0.11, dy + 0.06, 0.35)],
                            0.007, "Steel", bevel_res=4))
    objs.append(C.make_lathe("LampShade", [(0.016, 0.05), (0.036, 0.03), (0.085, -0.05), (0.082, -0.054), (0.032, 0.025)],
                             (dx + 0.12, dy + 0.07, 0.32), "MetalPaint", segments=22, rot=(math.radians(15), math.radians(-22), 0)))

    # Wooden inbox tray + stacked patient folders at (-0.10, 0.92, 0)
    tx, ty = -0.10, 0.92
    objs.append(C.make_box("TrayBase", (tx, ty, 0.010), (0.34, 0.26, 0.014), "Wood", bevel=0.002))
    for s in (-1, 1):
        objs.append(C.make_box(f"TraySide_{s}", (tx, ty + s * 0.125, 0.042), (0.34, 0.014, 0.068), "Wood", bevel=0.002))
    objs.append(C.make_box("TrayBack", (tx - 0.165, ty, 0.042), (0.014, 0.26, 0.068), "Wood", bevel=0.002))
    for k in range(4):
        objs.append(C.make_box(f"TrayFolder_{k}", (tx + 0.01, ty, 0.025 + k * 0.014), (0.30, 0.22, 0.011),
                               "KraftPaper" if k % 2 == 0 else "Paper", bevel=0.002, rot=(0, 0, (k - 1.5) * 0.04)))
    return objs


def build_dry_shelf():
    """Detailed wooden archive shelving unit for Room 101 Store (Kho giường: 2.10 wide x 0.54 deep x 2.35 tall).
    Preserves lower shelf counter height at Z=0.86 m and upper shelf at Z=1.74 m so E11 evidence props sit cleanly!"""
    objs = []
    w, d, h = 2.12, 0.54, 2.35
    # 4 corner timber uprights + center dividers
    for sx in (-w * 0.5 + 0.03, w * 0.5 - 0.03):
        for sy in (-d * 0.5 + 0.03, d * 0.5 - 0.03):
            objs.append(C.make_box(f"Post_{sx:.2f}_{sy:.2f}", (sx, sy, h * 0.5), (0.058, 0.058, h), "Wood", bevel=0.004))
    # Backboard vertical timber planks (south side -Y)
    for k in range(8):
        px = -w * 0.5 + 0.14 + k * 0.262
        objs.append(C.make_box(f"BackPlank_{k}", (px, -d * 0.5 + 0.012, h * 0.5), (0.252, 0.018, h - 0.08), "WoodDoor", bevel=0.002))
    # Shelves at Z=0.14, 0.48, 0.84 (main E11 table!), 1.32, 1.72 (upper E11 shelf!), 2.28 (top cap)
    for sz, thick in ((0.14, 0.028), (0.48, 0.026), (0.84, 0.042), (1.32, 0.026), (1.72, 0.036), (2.28, 0.034)):
        objs.append(C.make_box(f"Shelf_{sz:.2f}", (0, 0, sz), (w, d, thick), "WoodDoor" if sz in (0.84, 1.72) else "Wood", bevel=0.004))
    # Side solid enclosure panels below 0.84 m
    for s in (-1, 1):
        objs.append(C.make_box(f"SidePanel_{s}", (s * (w * 0.5 - 0.014), 0, 0.48), (0.024, d - 0.04, 0.72), "Wood", bevel=0.003))
    # Archive boxes & bound ledgers on lower & top shelves (leaving Z=0.86 and Z=1.74 clear for E11 props!)
    for k in range(3):
        objs.append(C.make_box(f"LowArchiveBox_{k}", (-0.68 + k * 0.46, 0.02, 0.28), (0.38, 0.42, 0.24), "KraftPaper", bevel=0.005))
    for k in range(6):
        objs.append(C.make_box(f"MidBinder_{k}", (-0.82 + k * 0.08, 0.02, 1.48), (0.062, 0.32, 0.28),
                               "WoodDoor" if k % 2 == 0 else "KraftPaper", bevel=0.004))
    objs.append(C.make_box("TopArchiveBoxA", (-0.52, 0.02, 1.90), (0.44, 0.38, 0.30), "KraftPaper", bevel=0.006))
    objs.append(C.make_box("TopArchiveBoxB", (0.05, 0.02, 1.88), (0.36, 0.38, 0.26), "KraftPaper", bevel=0.006))
    return objs


def run_all():
    C.export_asset("CTCC_PROP_WardBed", build_ward_bed, make_lods=True, render=True, cam_dir=(1.4, -1.6, 0.9))
    C.export_asset("CTCC_PROP_BedsideCabinet", build_bedside_cabinet, make_lods=True, render=True, cam_dir=(1.2, -1.7, 0.8))
    C.export_asset("CTCC_PROP_MedicalCabinet", build_medical_cabinet, make_lods=True, render=True, cam_dir=(-1.8, -1.3, 0.5))
    C.export_asset("CTCC_PROP_IVStand", build_iv_stand, make_lods=True, render=True, cam_dir=(1.4, -1.5, 0.4))
    C.export_asset("CTCC_PROP_TransportTrolley", build_transport_trolley, make_lods=True, render=True, cam_dir=(1.5, -1.5, 0.85))
    C.export_asset("CTCC_PROP_WaitingBench", build_waiting_bench, make_lods=True, render=True, cam_dir=(-1.6, -1.4, 0.7))
    C.export_asset("CTCC_PROP_ReceptionProps", build_reception_props, make_lods=True, render=True, cam_dir=(1.4, -1.5, 1.1))
    C.export_asset("CTCC_PROP_DryShelf", build_dry_shelf, make_lods=True, render=True, cam_dir=(0.8, 2.0, 0.5))


if __name__ == "__main__":
    run_all()
