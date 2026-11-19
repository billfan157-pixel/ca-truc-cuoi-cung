"""Vietnamese Period Storytelling Props & 3D Physical Evidence Models (1987-2012) built in Blender 4.5.

Includes:
- Tin vacuum thermos flask (phích nước vỏ sắt) + enameled mug + washcloth
- Enameled washbasin (chậu men) + rolled woven sedge mat (chiếu cói)
- Iconic Vietnamese honeycomb rubber slippers (dép tổ ong)
- 1990s Hospital quartz wall clock with real 3D housing & hands
- Wooden clipboard F101 with metal spring clip, paper stack & chained pen
- Physical 3D evidence items: C01 curled wristband, 3 curled torn drawing fragments,
  hanging handover tag (OBS101B), Bảy's kraft envelope + 2 keys, patient canvas bag
- Ward accessories: PA intercom speaker, fire extinguisher, pedal waste bin, exterior wrought-iron gate
"""

import math
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import common as C


def build_thermos_and_mug():
    """Iconic Vietnamese 2L tin-cased vacuum thermos (phích nước vỏ sắt Ø14x39 cm, non-branded per AR03)
    + white enamel mug with classic dark-blue rim (ca men) + folded washcloth."""
    objs = []
    # Vacuum thermos at (-0.07, 0.02, 0)
    tx, ty = -0.07, 0.02
    objs.append(C.make_lathe("ThermosBody", [
        (0.0, 0.0), (0.068, 0.0), (0.072, 0.015), (0.070, 0.024),
        (0.070, 0.295), (0.072, 0.305), (0.050, 0.345), (0.036, 0.355), (0.036, 0.370)
    ], (tx, ty, 0), "MetalPaint", segments=24))
    # Aluminum shoulder & base trim bands
    objs.append(C.make_cylinder("ThermosBaseRing", (tx, ty, 0.0), (tx, ty, 0.022), 0.0725, "Steel", segments=24))
    objs.append(C.make_lathe("ThermosShoulder", [(0.0715, 0.298), (0.052, 0.345), (0.037, 0.355), (0.037, 0.372)], (tx, ty, 0), "Steel", segments=24))
    # Overturned plastic/enamel cup lid & cork stopper hint
    objs.append(C.make_lathe("ThermosCap", [(0.044, 0.342), (0.041, 0.398), (0.036, 0.404), (0.0, 0.404)], (tx, ty, 0), "RedPaint", segments=22))
    # Curved wire side handle
    objs.append(C.make_pipe("ThermosHandle", [(tx - 0.068, ty, 0.275), (tx - 0.118, ty, 0.255),
                                              (tx - 0.120, ty, 0.120), (tx - 0.068, ty, 0.095)], 0.007, "PlasticIvory", bevel_res=4))

    # White enamel mug with dark blue lip (ca men viền xanh) at (0.095, -0.02, 0)
    mx, my = 0.095, -0.02
    objs.append(C.make_lathe("EnamelMug", [
        (0.0, 0.0), (0.042, 0.0), (0.044, 0.008), (0.044, 0.088),
        (0.048, 0.094), (0.044, 0.096), (0.039, 0.088), (0.039, 0.008), (0.0, 0.008)
    ], (mx, my, 0), "Ceramic", segments=24))
    objs.append(C.make_lathe("MugBlueRim", [(0.0435, 0.089), (0.049, 0.094), (0.0435, 0.097), (0.039, 0.090)], (mx, my, 0), "EnamelBlue", segments=24))
    objs.append(C.make_pipe("MugHandle", [(mx + 0.042, my, 0.076), (mx + 0.074, my, 0.068),
                                          (mx + 0.072, my, 0.026), (mx + 0.042, my, 0.020)], 0.0055, "Ceramic", bevel_res=3))

    # Folded cotton face towel beside mug
    objs.append(C.make_box("FoldedTowelA", (0.07, 0.085, 0.013), (0.17, 0.12, 0.024), "Cotton", bevel=0.010, segments=3, rot=(0, 0, 0.12)))
    objs.append(C.make_box("FoldedTowelB", (0.072, 0.082, 0.035), (0.16, 0.115, 0.022), "Textile", bevel=0.010, segments=3, rot=(0, 0, 0.06)))
    return objs


def build_washbasin_and_mat():
    """Under-bed Vietnamese hospital attendant staples: enameled washbasin (chậu men) & rolled woven sedge mat (chiếu cói)."""
    objs = []
    # Enameled washbasin with rolled rim
    objs.append(C.make_lathe("Washbasin", [
        (0.0, 0.0), (0.135, 0.0), (0.155, 0.022), (0.195, 0.112), (0.214, 0.122),
        (0.218, 0.115), (0.192, 0.108), (0.150, 0.020), (0.130, 0.006), (0.0, 0.006)
    ], (0, 0, 0), "Ceramic", segments=28))
    objs.append(C.make_lathe("BasinBlueRim", [(0.205, 0.118), (0.219, 0.122), (0.219, 0.113), (0.205, 0.111)], (0, 0, 0), "EnamelBlue", segments=28))

    # Rolled woven sedge mat (chiếu cói) lying beside basin (0.95 m long, Ø11 cm)
    objs.append(C.make_cylinder("ReedMatRoll", (-0.32, 0.34, 0.056), (0.62, 0.30, 0.056), 0.055, "ReedMat", segments=22, bevel=0.006))
    # Red fabric cloth trim bindings on mat ends & twine cord in middle
    for mx in (-0.31, 0.61):
        objs.append(C.make_cylinder(f"MatBinding_{mx:.2f}", (mx, 0.32, 0.056), (mx + 0.024, 0.319, 0.056), 0.0565, "RedPaint", segments=22))
    objs.append(C.make_cylinder("MatTwine", (0.14, 0.321, 0.056), (0.155, 0.320, 0.056), 0.0575, "KraftPaper", segments=20))
    return objs


def build_honeycomb_slippers():
    """Pair of iconic Vietnamese ivory-yellow honeycomb rubber slippers (dép tổ ong), placed slightly askew.
    Direct visual anchor for patient humanity & SFX-AN slipper cues!"""
    objs = []
    for side, pos, yaw in (("L", (-0.075, -0.02, 0), 0.16), ("R", (0.085, 0.04, 0), -0.24)):
        ca, sa = math.cos(yaw), math.sin(yaw)

        def pt(lx, ly, lz):
            return (pos[0] + lx * ca - ly * sa, pos[1] + lx * sa + ly * ca, pos[2] + lz)

        # Contoured rubber sole + continuous arched molded vamp strap with classic honeycomb holes!
        sole = C.make_box(f"Sole_{side}", pt(0, 0, 0.011), (0.090, 0.228, 0.020), "SlipperYellow", bevel=0.008, segments=3, rot=(0, 0, yaw))
        objs.append(sole)
        # Sleek continuous molded arch vamp
        vamp = C.make_box(f"VampArch_{side}", pt(0, 0.024, 0.035), (0.088, 0.088, 0.042), "SlipperYellow", bevel=0.016, segments=4, rot=(0, 0, yaw))
        objs.append(vamp)
        # Foot opening tunnel dark shadow inside vamp
        objs.append(C.make_box(f"VampCavity_{side}", pt(0, 0.024, 0.027), (0.064, 0.092, 0.024), "Black", bevel=0.006, rot=(0, 0, yaw)))
        # Crisp honeycomb recessed ventilation holes on top of vamp
        for hx in (-0.022, 0.0, 0.022):
            for hy in (0.002, 0.024, 0.046):
                p0 = pt(hx, hy, 0.046)
                p1 = pt(hx, hy, 0.058)
                objs.append(C.make_cylinder(f"Hole_{side}_{hx:.3f}_{hy:.3f}", p0, p1, 0.0052, "Black", segments=10))
    return objs


def build_wall_clock():
    """Round 1990s Vietnamese hospital quartz wall clock (Ø40 cm) with stepped bakelite bezel,
    recessed dial plate, real 3D hands (export separate R/B hand sets or base housing + hands),
    and convex glass crystal! Back at Y=0, protrudes toward -Y."""
    body = []
    # Stepped round bakelite bezel & rear drum
    body.append(C.make_lathe("ClockCase", [
        (0.0, 0.0), (0.192, 0.0), (0.205, -0.012), (0.205, -0.048),
        (0.194, -0.056), (0.180, -0.056), (0.175, -0.032), (0.0, -0.032)
    ], (0, 0, 0), "Frame", segments=36, rot=(math.radians(90), 0, 0)))
    # Recessed dial face (flat disc where Unity maps ClockR / ClockB dial texture or stays crisp)
    body.append(C.make_cylinder("ClockDial", (0, -0.028, 0), (0, -0.034, 0), 0.174, "Paper", segments=36))
    # 12 Raised brass/dark hour tick markers around perimeter
    for h in range(12):
        a = math.tau * h / 12.0
        tx, tz = math.sin(a) * 0.148, math.cos(a) * 0.148
        body.append(C.make_box(f"HourTick_{h}", (tx, -0.036, tz), (0.008 if h % 3 == 0 else 0.004, 0.004, 0.022 if h % 3 == 0 else 0.014),
                               "Black", bevel=0.001, rot=(0, -a, 0)))
    # Center spindle cap + subtle protective glass crystal ring
    body.append(C.make_cylinder("ClockPin", (0, -0.034, 0), (0, -0.049, 0), 0.009, "Brass", segments=14))
    body.append(C.make_cylinder("ClockGlass", (0, -0.050, 0), (0, -0.053, 0), 0.182, "Glass", segments=32))

    # Real 3D Hands for R (11:52) and B (03:17)
    def hands_for(hour, minute, tag):
        h_objs = []
        ha = math.radians(hour * 30.0 + minute * 0.5)
        ma = math.radians(minute * 6.0)
        h_objs.append(C.make_box(f"HourHand_{tag}", (math.sin(ha) * 0.048, -0.041, math.cos(ha) * 0.048),
                                 (0.011, 0.003, 0.105), "Black", bevel=0.001, rot=(0, -ha, 0)))
        h_objs.append(C.make_box(f"MinHand_{tag}", (math.sin(ma) * 0.068, -0.045, math.cos(ma) * 0.068),
                                 (0.007, 0.003, 0.145), "Black", bevel=0.001, rot=(0, -ma, 0)))
        h_objs.append(C.make_box(f"SecHand_{tag}", (math.sin(ma + 1.2) * 0.055, -0.047, math.cos(ma + 1.2) * 0.055),
                                 (0.003, 0.002, 0.145), "RedPaint", bevel=0.0, rot=(0, -(ma + 1.2), 0)))
        return h_objs

    return {"ClockBody": body, "HandsR": hands_for(11, 52, "R"), "HandsB": hands_for(3, 17, "B")}


def build_clipboard_f101():
    """Detailed masonite/wood hospital clipboard (0.26 x 0.36 m) with spring metal clip, layered paper stack & chained pen.
    Back at Y=0, front paper faces -Y (or centered for 3D inspector)."""
    objs = []
    # Beveled dark wood/masonite backing board
    objs.append(C.make_box("Board", (0, 0.005, 0), (0.265, 0.010, 0.365), "WoodDoor", bevel=0.004))
    # Layered paper sheets slightly offset at bottom corner for natural realism
    objs.append(C.make_box("PaperUnder", (0.003, -0.003, -0.008), (0.225, 0.005, 0.315), "Paper", bevel=0.001, rot=(0, 0.015, 0)))
    objs.append(C.make_box("PaperTop", (0, -0.007, -0.006), (0.224, 0.003, 0.312), "Paper", bevel=0.001))
    # Spring-loaded stamped steel clipboard clamp at top
    objs.append(C.make_box("ClipPlate", (0, -0.013, 0.155), (0.105, 0.012, 0.036), "Steel", bevel=0.003))
    objs.append(C.make_pipe("ClipWire", [(-0.042, -0.018, 0.145), (-0.042, -0.026, 0.175),
                                         (0.042, -0.026, 0.175), (0.042, -0.018, 0.145)], 0.004, "Steel", bevel_res=3))
    for rx in (-0.036, 0.036):
        objs.append(C.make_cylinder(f"ClipRivet_{rx:.2f}", (rx, 0.012, 0.155), (rx, -0.020, 0.155), 0.005, "Brass", segments=10))
    # Attached hospital ballpoint pen tucked on right clip edge
    objs.append(C.make_cylinder("BoardPen", (0.105, -0.014, 0.01), (0.118, -0.014, 0.145), 0.005, "EnamelBlue", segments=10))
    return objs


def build_wristband_c01():
    """Detailed curled Vietnamese pediatric/patient ID vinyl wristband (C01) with plastic snap button & paper insert!"""
    objs = []
    # Curled oval vinyl band loop
    pts = []
    steps = 28
    for i in range(steps):
        a = math.tau * i / steps
        x = math.cos(a) * 0.038 + (0.004 if i > steps * 0.7 else 0)
        y = math.sin(a) * 0.026
        z = 0.012 + 0.003 * math.sin(a * 2.0)
        pts.append((x, y, z))
    # Build flat ribbon band via bmesh extrusion along loop
    bm = bmesh.new()
    top_v, bot_v = [], []
    for i, (px, py, pz) in enumerate(pts):
        w_half = 0.011 if abs(px) < 0.022 and py > 0 else 0.0065
        top_v.append(bm.verts.new((px, py, pz + w_half)))
        bot_v.append(bm.verts.new((px, py, pz - w_half)))
    for i in range(steps):
        ni = (i + 1) % steps
        bm.faces.new((bot_v[i], bot_v[ni], top_v[ni], top_v[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new("WristbandRibbon_mesh")
    bm.to_mesh(mesh)
    bm.free()
    band = bpy.data.objects.new("WristbandRibbon", mesh)
    bpy.context.collection.objects.link(band)
    sol = band.modifiers.new("Thick", "SOLIDIFY")
    sol.thickness = 0.0022
    objs.append(C.finalize_mesh(band, "WristbandRibbon", "PlasticIvory", smooth_angle=60.0, uv_scale=0.15))
    # Paper ID slip nestled inside wider front window
    objs.append(C.make_box("IDSlip", (0, 0.0265, 0.012), (0.044, 0.0025, 0.015), "Paper", bevel=0.001))
    # Plastic tamper-proof snap rivet at rear clasp
    objs.append(C.make_cylinder("SnapClasp", (0, -0.023, 0.012), (0, -0.031, 0.012), 0.0055, "Steel", segments=12))
    return objs


def build_bay_envelope():
    """Bảy's weathered kraft envelope (E10a) with string-button tie, folded paper peeking out, & two distinct brass/steel keys."""
    objs = []
    # Thick weathered kraft envelope lying flat
    objs.append(C.make_box("EnvelopeBody", (0, 0, 0.009), (0.26, 0.175, 0.014), "KraftPaper", bevel=0.004))
    # Slightly raised triangular top flap + twin string-button washers
    objs.append(C.make_box("EnvelopeFlap", (0, 0.052, 0.017), (0.245, 0.068, 0.004), "KraftPaper", bevel=0.002, rot=(math.radians(-3), 0, 0)))
    objs.append(C.make_cylinder("ButtonWasherA", (0, 0.028, 0.018), (0, 0.028, 0.022), 0.009, "RedPaint", segments=14))
    objs.append(C.make_cylinder("ButtonWasherB", (0, -0.018, 0.016), (0, -0.018, 0.020), 0.009, "RedPaint", segments=14))
    # Folded cream letter slightly protruding from right edge
    objs.append(C.make_box("LetterPeek", (0.045, 0.005, 0.013), (0.21, 0.145, 0.005), "Paper", bevel=0.001, rot=(0, 0, -0.05)))
    # Two distinct period keys on split steel ring resting on envelope (Gate key = steel, Area C key = dark brass!)
    objs.append(C.make_pipe("KeyRing", [(0.08 + 0.016 * math.cos(k * math.tau / 16), -0.04 + 0.016 * math.sin(k * math.tau / 16), 0.021)
                                        for k in range(16)], 0.002, "Steel", bevel_res=3, cyclic=True))
    for k_idx, (k_mat, ang) in enumerate((("Steel", -0.35), ("Brass", 0.25))):
        ca, sa = math.cos(ang), math.sin(ang)
        kx0, ky0 = 0.075, -0.042
        objs.append(C.make_cylinder(f"KeyBow_{k_idx}", (kx0 - ca * 0.02, ky0 - sa * 0.02, 0.019),
                                    (kx0 - ca * 0.02, ky0 - sa * 0.02, 0.023), 0.014, k_mat, segments=14))
        objs.append(C.make_box(f"KeyBlade_{k_idx}", (kx0 - ca * 0.056, ky0 - sa * 0.056, 0.021),
                               (0.052, 0.008, 0.003), k_mat, bevel=0.001, rot=(0, 0, ang)))
        objs.append(C.make_box(f"KeyBit_{k_idx}", (kx0 - ca * 0.074 - sa * 0.006, ky0 - sa * 0.074 + ca * 0.006, 0.021),
                               (0.016, 0.012, 0.003), k_mat, bevel=0.001, rot=(0, 0, ang)))
    return objs


def build_patient_bag():
    """Soft woven hospital patient duffel/tote bag (túi đồ vải lưu tại kho 101) with sagging folds, straps & brass zipper."""
    objs = []
    # Sculpted soft slumped fabric bag body
    def bag_fn(u, v, bw, bl):
        x = (u - 0.5) * bw
        y = (v - 0.5) * bl
        ex = math.sin(u * math.pi)
        ey = math.sin(v * math.pi)
        dome = 0.165 * (ex ** 0.55) * (ey ** 0.55)
        slump = -0.025 * math.exp(-((x / 0.09) ** 2 + (y / 0.06) ** 2))
        wrinkle = 0.008 * math.sin(u * 19.0 + v * 13.0) * ex * ey
        return (x, y, dome + slump + wrinkle)

    objs.append(C.make_cloth_grid("BagBody", 0.36, 0.23, 24, 18, bag_fn, (0, 0, 0.018), "TextileOld", thickness=0.028, uv_tile=(2.0, 2.0)))
    objs.append(C.make_box("BagBase", (0, 0, 0.014), (0.33, 0.20, 0.022), "TextileOld", bevel=0.010, segments=3))
    # Brass top zipper hugging the slumped center ridge of the bag
    objs.append(C.make_pipe("BagZipper", [(-0.125, 0, 0.145), (-0.05, 0, 0.165), (0.0, 0, 0.162), (0.05, 0, 0.165), (0.125, 0, 0.145)], 0.0045, "Brass", bevel_res=3))
    # Twin stitched woven webbing handles drooping to sides
    for sy in (-0.065, 0.065):
        objs.append(C.make_pipe(f"BagStrap_{sy:.2f}", [(-0.085, sy, 0.12), (-0.075, sy * 1.35, 0.205),
                                                       (0.075, sy * 1.35, 0.205), (0.085, sy, 0.12)], 0.008, "CottonOld", bevel_res=3))
    return objs


def build_pa_speaker():
    """Vintage Vietnamese hospital corridor wall-mounted PA intercom speaker box (loa hành lang — grounds CH01-PA01..03!).
    Back at Y=0, grille faces -Y tilted slightly downward."""
    objs = []
    # Slanted wooden/metal trapezoid speaker cabinet
    objs.append(C.make_box("SpeakerBox", (0, -0.065, 0), (0.28, 0.13, 0.21), "WoodDoor", bevel=0.007, rot=(math.radians(8), 0, 0)))
    objs.append(C.make_box("SpeakerBaffle", (0, -0.126, -0.005), (0.245, 0.012, 0.175), "Black", bevel=0.002, rot=(math.radians(8), 0, 0)))
    # Horizontal enameled acoustic louver slats across speaker grille
    for k in range(7):
        sz = -0.072 + k * 0.024
        objs.append(C.make_box(f"SpeakerSlat_{k}", (0, -0.133 - sz * 0.14, sz), (0.235, 0.010, 0.009), "PlasticIvory",
                               bevel=0.002, rot=(math.radians(-18), 0, 0)))
    objs.append(C.make_cylinder("SpeakerWire", (0.14, -0.04, 0.04), (0.14, -0.04, 0.45), 0.005, "Black", segments=8))
    return objs


def build_fire_extinguisher():
    """Corridor wall-mounted red steel fire extinguisher (bình chữa cháy CO2/bột) with valve, pressure gauge, hose & bracket."""
    objs = []
    # Red steel pressure tank
    objs.append(C.make_lathe("Tank", [
        (0.0, 0.0), (0.072, 0.0), (0.078, 0.02), (0.078, 0.39),
        (0.065, 0.44), (0.024, 0.475), (0.024, 0.50), (0.0, 0.50)
    ], (0, 0, 0), "RedPaint", segments=24))
    # Instruction white paper wrap label
    objs.append(C.make_cylinder("TankLabel", (0, 0, 0.14), (0, 0, 0.32), 0.0792, "Paper", segments=24, cap=False))
    # Brass valve neck, black squeeze lever & pressure gauge
    objs.append(C.make_cylinder("ValveBody", (0, 0, 0.495), (0, 0, 0.545), 0.018, "Brass", segments=14))
    objs.append(C.make_pipe("CarryHandle", [(0, 0, 0.53), (-0.095, 0, 0.515)], 0.007, "Black", bevel_res=3))
    objs.append(C.make_pipe("TriggerLever", [(0, 0, 0.545), (-0.092, 0, 0.585)], 0.007, "Black", bevel_res=3))
    objs.append(C.make_cylinder("Gauge", (0, -0.016, 0.525), (0, -0.032, 0.525), 0.015, "Steel", segments=14))
    # Curved black discharge hose & flare horn
    objs.append(C.make_pipe("Hose", [(0.02, 0, 0.525), (0.098, -0.02, 0.49), (0.105, -0.03, 0.25), (0.095, -0.02, 0.11)],
                            0.0085, "Rubber", bevel_res=4))
    objs.append(C.make_cylinder("NozzleHorn", (0.095, -0.02, 0.12), (0.092, -0.02, 0.03), 0.014, "Black", segments=14, r_end=0.026))
    # Wall steel mounting bracket strap
    objs.append(C.make_box("WallBracket", (0, 0.088, 0.26), (0.055, 0.018, 0.34), "Steel", bevel=0.002))
    return objs


def build_pedal_bin():
    """Round stainless/painted Vietnamese hospital foot-pedal medical waste bin (thùng rác đạp chân Ø28x38 cm)."""
    objs = []
    objs.append(C.make_lathe("BinBody", [
        (0.0, 0.0), (0.135, 0.0), (0.142, 0.015), (0.142, 0.35), (0.148, 0.355), (0.136, 0.355)
    ], (0, 0, 0), "Steel", segments=26))
    objs.append(C.make_lathe("BinLid", [
        (0.149, 0.354), (0.146, 0.376), (0.115, 0.392), (0.0, 0.396)
    ], (0, 0, 0), "MetalPaint", segments=26))
    # Foot pedal bar sticking out front (-Y)
    objs.append(C.make_box("FootPedal", (0, -0.165, 0.028), (0.072, 0.058, 0.014), "Rubber", bevel=0.003))
    objs.append(C.make_box("PedalArm", (0, -0.14, 0.028), (0.024, 0.04, 0.010), "Steel", bevel=0.001))
    objs.append(C.make_box("RearHinge", (0, 0.148, 0.34), (0.048, 0.022, 0.052), "Black", bevel=0.002))
    return objs


def build_exterior_gate():
    """Detailed wrought-iron Vietnamese hospital side gate leaf (2.36 m wide x 1.82 m tall, pivot at hinge X=0, Z=0)."""
    objs = []
    w, h = 2.36, 1.82
    # Heavy outer tubular rectangular frame
    objs.append(C.make_box("GateHingeStile", (0.025, 0, h * 0.5), (0.050, 0.050, h), "Frame", bevel=0.004))
    objs.append(C.make_box("GateLatchStile", (w - 0.025, 0, h * 0.5), (0.050, 0.050, h), "Frame", bevel=0.004))
    for rz in (0.16, 0.88, h - 0.04):
        objs.append(C.make_box(f"GateRail_{rz:.2f}", (w * 0.5, 0, rz), (w, 0.044, 0.044), "Frame", bevel=0.004))
    # Lower kick sheet-metal panel (0.16..0.68 m)
    objs.append(C.make_box("GateSheet", (w * 0.5, 0, 0.42), (w - 0.09, 0.012, 0.50), "MetalOld", bevel=0.002))
    # 15 Vertical wrought-iron picket bars with pointed finial tips!
    for k in range(1, 16):
        px = k * (w / 16.0)
        objs.append(C.make_cylinder(f"Picket_{k}", (px, 0, 0.66), (px, 0, h + 0.05), 0.009, "Frame", segments=10))
        objs.append(C.make_cylinder(f"Finial_{k}", (px, 0, h + 0.05), (px, 0, h + 0.12), 0.011, "Frame", segments=4, r_end=0.001))
    # Heavy padlock hasp & latch bar
    objs.append(C.make_box("GateLockPlate", (w - 0.12, 0, 1.02), (0.16, 0.056, 0.12), "MetalOld", bevel=0.003))
    objs.append(C.make_cylinder("GateHandle", (w - 0.14, -0.055, 1.02), (w - 0.14, 0.055, 1.02), 0.011, "Steel", segments=12))
    return objs


def build_bulkhead_lamp():
    """Exterior caged oval cast-metal hospital courtyard bulkhead wall lamp (đèn bão ốp tường)."""
    objs = []
    objs.append(C.make_lathe("BulkheadBack", [(0.0, 0.0), (0.115, 0.0), (0.115, -0.025), (0.095, -0.032)],
                             (0, 0, 0), "Frame", segments=24, rot=(math.radians(90), 0, 0)))
    objs.append(C.make_sphere("BulkheadDome", (0, -0.055, 0), (0.175, 0.11, 0.235), "LiveTube", segs=20, rings=12))
    # Protective metal wire cage ribs over glass dome
    for ang_deg in (0, 60, 120):
        a = math.radians(ang_deg)
        pts = [(math.cos(t) * 0.092 * math.cos(a), -0.028 - math.sin(t) * 0.072, math.cos(t) * 0.122 * math.sin(a))
               for t in [k * math.pi / 12.0 for k in range(13)]]
        objs.append(C.make_pipe(f"CageRib_{ang_deg}", pts, 0.0045, "Frame", bevel_res=3))
    return objs


def run_all():
    C.export_asset("CTCC_PROP_ThermosAndMug", build_thermos_and_mug, make_lods=True, render=True, cam_dir=(1.2, -1.7, 0.9))
    C.export_asset("CTCC_PROP_WashbasinAndMat", build_washbasin_and_mat, make_lods=True, render=True, cam_dir=(1.3, -1.6, 1.1))
    C.export_asset("CTCC_PROP_HoneycombSlippers", build_honeycomb_slippers, make_lods=False, render=True, cam_dir=(0.9, -1.6, 1.4))
    C.export_asset("CTCC_PROP_WallClock", build_wall_clock, make_lods=False, render=True, cam_dir=(0.4, -2.0, 0.2), separate_parts=True)
    C.export_asset("CTCC_PROP_ClipboardF101", build_clipboard_f101, make_lods=False, render=True, cam_dir=(0.6, -2.0, 0.4))
    C.export_asset("CTCC_PROP_WristbandC01", build_wristband_c01, make_lods=False, render=True, cam_dir=(1.0, -1.5, 1.1))
    C.export_asset("CTCC_PROP_BayEnvelope", build_bay_envelope, make_lods=False, render=True, cam_dir=(0.6, -1.2, 1.6))
    C.export_asset("CTCC_PROP_PatientBag", build_patient_bag, make_lods=True, render=True, cam_dir=(1.2, -1.6, 0.9))
    C.export_asset("CTCC_PROP_PASpeaker", build_pa_speaker, make_lods=False, render=True, cam_dir=(1.0, -1.9, 0.3))
    C.export_asset("CTCC_PROP_FireExtinguisher", build_fire_extinguisher, make_lods=True, render=True, cam_dir=(1.2, -1.8, 0.5))
    C.export_asset("CTCC_PROP_PedalBin", build_pedal_bin, make_lods=True, render=True, cam_dir=(1.2, -1.7, 0.9))
    C.export_asset("CTCC_PROP_ExteriorGate", build_exterior_gate, make_lods=True, render=True, cam_dir=(1.1, -2.1, 0.4))
    C.export_asset("CTCC_PROP_BulkheadLamp", build_bulkhead_lamp, make_lods=False, render=False)


if __name__ == "__main__":
    run_all()
