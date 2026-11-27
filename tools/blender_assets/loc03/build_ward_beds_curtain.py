"""LOC03 Ward 101 Hero Beds, Cloth-Simulated Privacy Curtain & Bed 4 Traces (Blender 4.5 LTS).

Produces (with real Blender 4.5 Cloth Physics Simulation + Cycles PBR baking):
  1. CTCC_PROP_WardBed         -> LOC03_WardBed (2048² B/R, 1 draw call!) — Jade striped cloth-simmed blanket + readable clinical chart
  2. CTCC_LOC03_WardBedLived   -> LOC03_WardBedLived (2048² B/R, 1 draw call!) — Bed 2 variant w/ turned-back blue blanket + aligned OBS101C frame
  3. CTCC_ENV_CurtainAssembly  -> LOC03_Curtain (1024² B/R, separate CurtainTrack + readable CurtainCloth!) — Real cloth-simmed pleated drapery + upper mesh band
  4. CTCC_PROP_Bed4TracesWorking -> LOC03_Bed4Trace (1024² B-only, separate Bed4CottonTrace + Bed4RubberTrace + Bed4ContactTrace!)
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

import common as C  # noqa: E402
import hero_lib as H  # noqa: E402
import ward_lib as W  # noqa: E402


def _caster_wheel(objs, prefix, x, y, wheel_r=0.046, z_top=0.19):
    objs.append(H.cyl(f"{prefix}_Stem", (x, y, wheel_r * 2 + 0.012), (x, y, z_top), 0.016, "satin_inox", seg=12))
    objs.append(H.cyl(f"{prefix}_Bearing", (x, y, wheel_r * 2 + 0.008), (x, y, wheel_r * 2 + 0.026), 0.024, "satin_inox", seg=14))
    objs.append(H.cyl(f"{prefix}_Tire", (x - 0.017, y, wheel_r), (x + 0.017, y, wheel_r), wheel_r, "rubber", seg=20, bevel=0.005))
    objs.append(H.cyl(f"{prefix}_Hub", (x - 0.020, y, wheel_r), (x + 0.020, y, wheel_r), wheel_r * 0.56, "satin_inox", seg=16))
    objs.append(H.cyl(f"{prefix}_Axle", (x - 0.026, y, wheel_r), (x + 0.026, y, wheel_r), 0.008, "satin_inox", seg=10))
    for side in (-1, 1):
        objs.append(H.box(f"{prefix}_Yoke_{side}", (x + side * 0.023, y, wheel_r + 0.028), (0.007, 0.044, 0.066), "satin_inox", bevel=0.002))
    objs.append(H.box(f"{prefix}_Brake", (x, y - 0.044, wheel_r + 0.042), (0.026, 0.032, 0.008), "plastic_red", bevel=0.002, rot=(math.radians(18), 0, 0)))


def _build_ward_bed_common(lived_bed2: bool = False):
    """Authentic 1987 Vietnamese tubular steel hospital bed (exact 0.88 x 2.06 m footprint)
    with real Blender Cloth-Simulated blanket drape, indented cotton pillow & fitted sheet."""
    objs = []

    # 1. Tubular enameled steel bedframe, legs, gussets, casters, slats & elevation crank
    for x in (-0.385, 0.385):
        objs.append(H.box(f"SideFrame_{x:.2f}", (x, 0, 0.545), (0.036, 1.92, 0.048), "bed_enamel_green", bevel=0.005, grain="Y"))
        for y in (-0.875, 0.875):
            _caster_wheel(objs, f"BedCaster_{x:.2f}_{y:.2f}", x, y, wheel_r=0.046, z_top=0.19)
            objs.append(H.cyl(f"Leg_{x:.2f}_{y:.2f}", (x, y, 0.175), (x, y, 0.565), 0.021, "bed_enamel_green", seg=14))
            objs.append(H.cyl(f"LegCollar_{x:.2f}_{y:.2f}", (x, y, 0.515), (x, y, 0.575), 0.027, "bed_enamel_green", seg=14, bevel=0.003))
        objs.append(H.cyl(f"LowerRail_{x:.2f}", (x, -0.875, 0.31), (x, 0.875, 0.31), 0.013, "bed_enamel_green", seg=12))
    # Cross undercarriage tie bars + elevation leadscrew
    for cy in (-0.52, 0.52):
        objs.append(H.cyl(f"UnderCross_{cy:.2f}", (-0.385, cy, 0.31), (0.385, cy, 0.31), 0.012, "bed_enamel_green", seg=10))
    objs.append(H.cyl("LeadScrew", (0.14, -0.96, 0.43), (0.14, 0.38, 0.43), 0.011, "steel_dark", seg=10))

    # Head (+Y, 1.12m) & Foot (-Y, 0.91m) bent U-tube arches with vertical spindles & riveted enamel nameplate
    for idx, y in enumerate((-0.96, 0.96)):
        top_z = 1.12 if y > 0 else 0.91
        objs.append(H.pipe(f"EndArch_{idx}", C.make_rounded_u(y, 0.22, top_z, 0.385, radius=0.088), 0.021, "bed_enamel_green", res=5))
        objs.append(H.cyl(f"EndCrossLow_{idx}", (-0.385, y, 0.43), (0.385, y, 0.43), 0.016, "bed_enamel_green", seg=12))
        objs.append(H.cyl(f"EndCrossMid_{idx}", (-0.385, y, 0.62), (0.385, y, 0.62), 0.015, "bed_enamel_green", seg=12))
        for sx in (-0.28, -0.14, 0.0, 0.14, 0.28):
            objs.append(H.cyl(f"Spindle_{idx}_{sx:.2f}", (sx, y, 0.62), (sx, y, top_z - 0.014), 0.0095, "bed_enamel_green", seg=10))
        objs.append(H.box(f"EndPanel_{idx}", (0, y, 0.72), (0.64, 0.018, 0.16), "bed_enamel_green", bevel=0.006))
        for rx in (-0.26, 0.26):
            objs.append(H.cyl(f"PanelRivet_{idx}_{rx:.2f}", (rx, y - 0.012, 0.72), (rx, y + 0.012, 0.72), 0.006, "satin_inox", seg=10))
        # Corner rubber wall bumpers + IV pole corner sockets
        for sx in (-0.415, 0.415):
            objs.append(H.cyl(f"WallBumper_{idx}_{sx:.2f}", (sx, y, 0.535), (sx, y, 0.562), 0.032, "rubber", seg=14))
        objs.append(H.cyl(f"IVSocket_{idx}", (-0.39, y, 0.56), (-0.39, y, 0.65), 0.016, "satin_inox", seg=12))

    # Bed number enameled badge mounted neatly on footboard right side
    bed_plate_uv = (0.52, 0.69, 0.98, 0.83) if lived_bed2 else (0.52, 0.84, 0.98, 0.98)
    plate = H.box("BedNumberPlate", (0.19, -0.972, 0.73), (0.13, 0.005, 0.052), "print:med_labels", bevel=0.001)
    H.set_planar_print_uv(plate, axis="Y", uv_rect=bed_plate_uv, flip_u=False)
    objs.append(plate)

    # Folding manual elevation crank handle at foot (-Y)
    objs.append(H.cyl("CrankShaft", (0.16, -0.96, 0.43), (0.16, -1.02, 0.43), 0.010, "satin_inox", seg=12))
    objs.append(H.pipe("CrankArm", [(0.16, -1.015, 0.43), (0.16, -1.025, 0.31), (0.16, -1.072, 0.31)], 0.008, "satin_inox", res=3))
    objs.append(H.cyl("CrankGrip", (0.16, -1.032, 0.31), (0.16, -1.096, 0.31), 0.012, "rubber", seg=12))

    # Bed-foot aluminum patient medical chart pocket:
    # - Beds 1 & 3: left-offset at X=-0.12 with crisp printed clinical monitoring sheet (`bed_chart_01`)
    # - Bed 2 (`lived_bed2=True`): centered at X=0.0, Z=0.785 with empty brushed-steel clip frame
    #   so Unity's interactive `OBS101C_Bed2Chart` (`Doc_Bed2Transfer`) rests FLUSH inside it with ZERO overlap!
    cx = 0.0 if lived_bed2 else -0.13
    cz = 0.785 if lived_bed2 else 0.745
    objs.append(H.box("ChartBoardBack", (cx, -0.986, cz), (0.24, 0.008, 0.31), "satin_inox", bevel=0.003))
    objs.append(H.box("ChartBoardLip", (cx, -0.996, cz - 0.150), (0.24, 0.022, 0.010), "satin_inox", bevel=0.002))
    objs.append(H.box("ChartClip", (cx, -0.998, cz + 0.136), (0.088, 0.014, 0.026), "satin_inox", bevel=0.002))
    for hx in (cx - 0.072, cx + 0.072):
        objs.append(H.pipe(f"ChartHook_{hx:.2f}", [(hx, -0.986, cz + 0.145), (hx, -0.986, 0.928), (hx, -0.942, 0.928)], 0.0045, "satin_inox", res=3))
    if not lived_bed2:
        paper = H.box("ChartPaper", (cx, -0.993, cz - 0.005), (0.206, 0.004, 0.272), "print:bed_chart_01", bevel=0.001)
        H.set_planar_print_uv(paper, axis="Y", uv_rect=(0.0, 0.0, 1.0, 1.0))
        objs.append(paper)

    # Welded deck slats
    for i in range(10):
        objs.append(H.box(f"DeckSlat_{i}", (0, -0.82 + i * 0.182, 0.562), (0.74, 0.070, 0.016), "bed_enamel_green", bevel=0.003))

    # Folding satin-inox side protection rail (-X) with vertical pickets & hinge brackets
    rx = -0.415
    objs.append(H.pipe("SideRailLoop", [
        (rx, 0.56, 0.56), (rx, 0.53, 0.83), (rx, 0.47, 0.88),
        (rx, -0.46, 0.88), (rx, -0.53, 0.83), (rx, -0.56, 0.56)
    ], 0.0135, "satin_inox", res=4))
    for sy in (-0.28, 0.0, 0.28):
        objs.append(H.cyl(f"SideRailBar_{sy:.2f}", (rx, sy, 0.565), (rx, sy, 0.875), 0.0085, "satin_inox", seg=10))
    for sy in (-0.56, 0.56):
        objs.append(H.box(f"RailBracket_{sy:.2f}", (rx + 0.016, sy, 0.56), (0.036, 0.044, 0.046), "steel_dark", bevel=0.003))

    # 2. Mattress + Fitted Cotton Sheet + Naturally Indented Pillow
    mattress = H.box("MattressBase", (0, 0.0, 0.625), (0.79, 1.84, 0.116), "mattress_ticking", bevel=0.028, seg=4)
    objs.append(mattress)

    def sheet_pos(u, v):
        x = (u - 0.5) * 0.806
        y = (v - 0.5) * 1.856
        ex = max(0.0, 1.0 - (abs(x) / 0.403) ** 6)
        ey = max(0.0, 1.0 - (abs(y) / 0.928) ** 8)
        crown = 0.028 * (ex * ey) ** 0.55
        sag = -0.015 * math.exp(-((x / 0.24) ** 2 + ((y - 0.06) / 0.52) ** 2))
        crease = 0.003 * math.sin(u * 17.0 + v * 11.0 + (1.4 if lived_bed2 else 0.0)) * ex * ey
        return (x, y, 0.678 + crown + sag + crease)

    sheet = H.sculpted_grid("FittedSheet", 20, 30, sheet_pos, "cotton_sheet", thickness=0.008)
    objs.append(sheet)
    objs.append(H.box("SheetTuckBand", (0, 0, 0.618), (0.806, 1.856, 0.046), "cotton_sheet", bevel=0.014, seg=3))

    # Plump cotton hospital pillow with realistic head indentation (angled slightly differently on Bed 2!)
    pil_yaw = -0.14 if lived_bed2 else 0.06
    pil_cx = -0.035 if lived_bed2 else 0.015
    pil_cy = 0.63 if lived_bed2 else 0.65
    ca, sa = math.cos(pil_yaw), math.sin(pil_yaw)

    def pillow_pos(u, v):
        lx = (u - 0.5) * 0.57
        ly = (v - 0.5) * 0.37
        ex = math.sin(u * math.pi)
        ey = math.sin(v * math.pi)
        loft = 0.098 * (ex * ey) ** 0.58
        indent = -0.030 * math.exp(-(((lx + 0.01) / 0.14) ** 2 + (ly / 0.10) ** 2))
        crease = 0.005 * math.sin(u * 13.0 + v * 9.0) * ex * ey
        wx = pil_cx + lx * ca - ly * sa
        wy = pil_cy + lx * sa + ly * ca
        return (wx, wy, 0.695 + loft + indent + crease)

    pillow = H.sculpted_grid("HospitalPillow", 20, 14, pillow_pos, "cotton_sheet", thickness=0.032)
    objs.append(pillow)

    # 3. REAL BLENDER 4.5 CLOTH PHYSICS SIMULATION FOR THE HOSPITAL BLANKET (ZERO PIN PINCHING!)
    # High-friction mattress collider holds blanket smoothly in place while gravity drapes edges organically!
    col_mattress = H.box("__ColMattress", (0.0, 0.0, 0.625), (0.818, 1.855, 0.168), "cotton_sheet", bevel=0.040, seg=3)
    col_rail = H.box("__ColRail", (-0.425, 0.0, 0.72), (0.048, 1.14, 0.32), "satin_inox", bevel=0.012, seg=2)
    col_foot = H.box("__ColFoot", (0.0, -0.96, 0.72), (0.82, 0.06, 0.42), "bed_enamel_green", bevel=0.015, seg=2)
    colliders = [col_mattress, col_rail, col_foot]

    blanket_kind = "blanket_blue" if lived_bed2 else "blanket_jade"
    if lived_bed2:
        # Bed 2 lived-in variant: slightly diagonal soft blanket with gentle organic ripples (NO pinched vertices!)
        byaw = -0.075
        bca, bsa = math.cos(byaw), math.sin(byaw)

        def blanket_init(u, v):
            lx = (u - 0.5) * 1.06
            ly = (v - 0.5) * 1.24
            # Soft organic body mound & gentle upper cuff without sharp corners
            mound = 0.024 * math.sin(u * math.pi) * math.sin(v * math.pi)
            cuff = 0.022 * math.exp(-((v - 0.93) / 0.08) ** 2) * (0.65 + 0.35 * math.sin(u * 4.5))
            ripple = 0.008 * math.sin(u * 6.5 + v * 4.0) + 0.005 * math.cos(u * 9.5 - v * 5.0)
            wx = 0.03 + lx * bca - ly * bsa
            wy = -0.22 + lx * bsa + ly * bca
            wz = 0.742 + mound + cuff + ripple
            return (wx, wy, wz)
    else:
        # Beds 1 & 3 neat hospital shift blanket: crisp rolled upper cuff + silky natural side & foot drape!
        def blanket_init(u, v):
            lx = (u - 0.5) * 1.08
            ly = (v - 0.5) * 1.28
            cuff = 0.026 * math.exp(-((v - 0.94) / 0.07) ** 2)
            ripple = 0.007 * math.sin(u * 6.0 + v * 3.8 + 0.5) + 0.004 * math.cos(u * 9.0 - v * 5.0)
            return (0.02 + lx, -0.21 + ly, 0.742 + cuff + ripple)

    blanket = W.create_cloth_grid("DrapedBlanket", 32, 36, blanket_init, blanket_kind, pin_fn=None)
    W.simulate_cloth_drape(
        blanket,
        colliders,
        frames=22,
        mass=0.58,
        tension=24.0,
        bending=1.25,
        air_damping=2.2,
        collision_dist=0.012,
        self_collide=False,
        solidify_thickness=0.011,
        cleanup_colliders=True,
    )
    objs.append(blanket)
    return objs


def build_ward_bed():
    return _build_ward_bed_common(lived_bed2=False)


def build_ward_bed_lived():
    return _build_ward_bed_common(lived_bed2=True)


def build_curtain_assembly():
    """Hospital ceiling privacy curtain track + ceiling hangers + chrome glider hooks +
    REAL Blender Cloth-Simulated pleated privacy curtain (2.38 m wide x 1.92 m tall).
    Preserves EXACT part names 'CurtainTrack' & 'CurtainCloth' and exact origin (X=0, Z=0)!"""
    track_objs = []
    w = 2.38
    ch = 1.92

    # Extruded anodized aluminum I-track rail + end stoppers
    track_objs.append(H.box("TrackRailMain", (w * 0.5, 0, 0.078), (w + 0.10, 0.024, 0.026), "satin_inox", bevel=0.003))
    track_objs.append(H.box("TrackRailLip", (w * 0.5, 0, 0.062), (w + 0.10, 0.034, 0.008), "satin_inox", bevel=0.002))
    for ex in (-0.05, w + 0.05):
        track_objs.append(H.box(f"TrackEndCap_{ex:.2f}", (ex, 0, 0.074), (0.016, 0.036, 0.032), "plastic_black", bevel=0.002))

    # 3 Ceiling suspension rods + bolted ceiling rosettes (rising 0.86m to ceiling)
    for hx in (0.06, w * 0.5, w - 0.06):
        track_objs.append(H.cyl(f"TrackHanger_{hx:.2f}", (hx, 0, 0.088), (hx, 0, 0.86), 0.0075, "satin_inox", seg=10))
        track_objs.append(H.cyl(f"TrackCollar_{hx:.2f}", (hx, 0, 0.085), (hx, 0, 0.125), 0.014, "satin_inox", seg=12))
        track_objs.append(H.cyl(f"CeilingFlange_{hx:.2f}", (hx, 0, 0.845), (hx, 0, 0.865), 0.032, "satin_inox", seg=16))

    # 15 chrome glider carriers & S-hooks spaced organically along track
    n_hooks = 15
    hook_xs = [0.025 + (w - 0.05) * k / float(n_hooks - 1) for k in range(n_hooks)]
    for k, gx in enumerate(hook_xs):
        track_objs.append(H.box(f"Glider_{k}", (gx, 0, 0.058), (0.014, 0.016, 0.014), "plastic_ivory", bevel=0.002))
        track_objs.append(H.pipe(f"Hook_{k}", [(gx, 0, 0.054), (gx, 0.010, 0.028), (gx, 0, 0.002)], 0.003, "satin_inox", res=3))

    # REAL BLENDER CLOTH SIMULATION FOR PRIVACY CURTAIN DRAPERY!
    # Start slightly pleated with natural non-uniform wavelength, pin the top ring nodes (v==0),
    # and run gravity + subtle asymmetric air current so the hem hangs organically with natural folds!
    nx, ny = 42, 18

    def curtain_init(u, v):
        x = u * w
        # Organic varied pleat spacing (not robotic uniform sine!)
        phase = u * 11.5 * math.pi + 0.55 * math.sin(u * 5.0)
        amp = 0.044 + 0.014 * math.sin(u * 7.3 + 0.8)
        y = math.sin(phase) * amp * (0.72 + 0.45 * v) + 0.012 * math.cos(u * 21.0 - v * 2.5) * v
        z = -v * ch
        return (x, y, z)

    # Pin the top edge vertices (grommet header tape hanging on track hooks)
    pin_fn = lambda u, v: (v < 0.03)
    cloth = W.create_cloth_grid("CurtainFabric", nx, ny, curtain_init, "curtain_ward", pin_fn=pin_fn)
    W.simulate_cloth_drape(
        cloth,
        collider_objs=[],
        frames=22,
        mass=0.32,
        tension=22.0,
        bending=0.35,
        air_damping=1.8,
        solidify_thickness=0.0,  # Two-sided URP material -> keeps vertex count light for EnvironmentMotion sway!
        wind_strength=2.2,
        wind_dir=(0.18, 0.95, -0.10),
    )
    return {"CurtainTrack": track_objs, "CurtainCloth": [cloth]}


def _make_contoured_slipper_hero(prefix: str, loc, yaw: float, scale: float = 1.0, mirror: float = 1.0):
    """High-detail Vietnamese ivory-yellow honeycomb EVA rubber slipper (dép tổ ong)."""
    parts = []
    ca, sa = math.cos(yaw), math.sin(yaw)

    def xform(lx, ly, lz):
        sx = lx * mirror * scale
        sy = ly * scale
        sz = lz * scale
        return (loc[0] + sx * ca - sy * sa, loc[1] + sx * sa + sy * ca, loc[2] + sz)

    # 1. Contoured anatomical sole
    outline = [
        (-0.033, -0.118), (-0.038, -0.072), (-0.032, -0.018),
        (-0.043,  0.042), (-0.041,  0.095), (-0.018,  0.122),
        ( 0.016,  0.123), ( 0.039,  0.098), ( 0.044,  0.045),
        ( 0.036, -0.018), ( 0.036, -0.075), ( 0.029, -0.118),
        ( 0.000, -0.126),
    ]
    bm = bmesh.new()
    bot_v, top_v = [], []
    for lx, ly in outline:
        lift = 0.007 if ly > 0.088 else (0.003 if ly < -0.10 else 0.0)
        bot_v.append(bm.verts.new(xform(lx, ly, lift)))
        top_v.append(bm.verts.new(xform(lx * 0.95, ly * 0.98, 0.016 + lift)))
    n = len(outline)
    bm.faces.new(bot_v[::-1])
    bm.faces.new(top_v)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((bot_v[i], bot_v[j], top_v[j], top_v[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new(f"{prefix}Sole_mesh")
    bm.to_mesh(mesh)
    bm.free()
    sole = bpy.data.objects.new(f"{prefix}Sole", mesh)
    bpy.context.collection.objects.link(sole)
    b = sole.modifiers.new("Bevel", "BEVEL")
    b.width = 0.0035 * scale
    b.segments = 2
    b.limit_method = "ANGLE"
    sole = C.finalize_mesh(sole, f"{prefix}Sole", "Black", smooth_angle=52.0)
    H.set_kind(sole, "slipper_honey")
    parts.append(sole)

    # 2. Hollow arched molded upper vamp strap
    bm2 = bmesh.new()
    y_stations = (-0.022, -0.002, 0.022, 0.046, 0.066)
    rings_out, rings_in = [], []
    arch_steps = 11
    for yi, ly in enumerate(y_stations):
        width_scale = 0.94 + 0.10 * math.sin((yi / 4.0) * math.pi)
        arch_h = 0.046 - yi * 0.0022
        half_w = 0.041 * width_scale
        ro, ri = [], []
        for k in range(arch_steps):
            ang = (k / float(arch_steps - 1)) * math.pi
            cx = -math.cos(ang) * half_w
            cz = 0.013 + math.sin(ang) * arch_h
            ix = -math.cos(ang) * (half_w - 0.0065)
            iz = 0.013 + max(0.0, math.sin(ang) * (arch_h - 0.0065))
            ro.append(bm2.verts.new(xform(cx, ly, cz)))
            ri.append(bm2.verts.new(xform(ix, ly, iz)))
        rings_out.append(ro)
        rings_in.append(ri)
    for yi in range(len(y_stations) - 1):
        for k in range(arch_steps - 1):
            bm2.faces.new((rings_out[yi][k], rings_out[yi][k + 1], rings_out[yi + 1][k + 1], rings_out[yi + 1][k]))
            bm2.faces.new((rings_in[yi + 1][k], rings_in[yi + 1][k + 1], rings_in[yi][k + 1], rings_in[yi][k]))
    for k in range(arch_steps - 1):
        bm2.faces.new((rings_out[0][k + 1], rings_out[0][k], rings_in[0][k], rings_in[0][k + 1]))
        bm2.faces.new((rings_out[-1][k], rings_out[-1][k + 1], rings_in[-1][k + 1], rings_in[-1][k]))
    bmesh.ops.recalc_face_normals(bm2, faces=bm2.faces)
    mesh2 = bpy.data.meshes.new(f"{prefix}Vamp_mesh")
    bm2.to_mesh(mesh2)
    bm2.free()
    vamp = bpy.data.objects.new(f"{prefix}Vamp", mesh2)
    bpy.context.collection.objects.link(vamp)
    vamp = C.finalize_mesh(vamp, f"{prefix}Vamp", "Black", smooth_angle=52.0)
    H.set_kind(vamp, "slipper_honey")
    parts.append(vamp)

    # 3. Signature recessed honeycomb ventilation dots along strap arch
    for hy in (-0.006, 0.018, 0.042):
        for hx in (-0.018, 0.0, 0.018):
            r_frac = abs(hx) / 0.043
            hz = 0.013 + math.sqrt(max(0.05, 1.0 - r_frac * r_frac)) * 0.044
            parts.append(H.cyl(f"{prefix}Dot_{hy:.3f}_{hx:.3f}",
                               xform(hx, hy, hz - 0.002), xform(hx, hy, hz + 0.0018),
                               0.0038 * scale, "rubber", seg=8))
    return parts


def build_bed4_traces_working():
    """Physical trace cluster for OBS101A inside Bed 4 alcove (Layer B):
    MUST preserve exact child names: 'Bed4CottonTrace', 'Bed4RubberTrace', 'Bed4ContactTrace'!
    Uses real Blender Cloth Simulation on the hastily folded pediatric blanket & draw-sheet."""
    # Temporary floor + soft fold cushion collider so cloth drapes with genuine creases onto floor
    col_floor = H.box("__TraceFloor", (0, 0, -0.02), (1.4, 1.4, 0.04), "cotton_sheet", bevel=0.0)
    col_core = H.box("__TraceCore", (-0.12, 0.04, 0.022), (0.42, 0.30, 0.042), "blanket_bed4", bevel=0.018, seg=3)

    # Lower hastily folded sage hospital blanket simulated over soft folded core
    def lower_init(u, v):
        x = -0.12 + (u - 0.5) * 0.56
        y = 0.04 + (v - 0.5) * 0.42
        ripple = 0.008 * math.sin(u * 12.0 + v * 7.0)
        return (x, y, 0.052 + ripple)

    b1 = W.create_cloth_grid("FoldedBlanketBase", 22, 18, lower_init, "blanket_bed4")
    W.simulate_cloth_drape(
        b1, [col_floor, col_core], frames=20, mass=0.45, tension=14.0, bending=0.45,
        collision_dist=0.006, solidify_thickness=0.014, cleanup_colliders=False,
    )

    # Upper tucked cloth flap draped softly over lower blanket (warm toned cotton, soft rounded edges!)
    def upper_init(u, v):
        lx = (u - 0.5) * 0.38
        ly = (v - 0.5) * 0.28
        ca, sa = math.cos(0.18), math.sin(0.18)
        wx = -0.10 + lx * ca - ly * sa
        wy = 0.05 + lx * sa + ly * ca
        dome = 0.016 * math.sin(u * math.pi) * math.sin(v * math.pi)
        return (wx, wy, 0.080 + dome + 0.005 * math.cos(u * 8.0 - v * 7.0))

    b2 = W.create_cloth_grid("FoldedBlanketTop", 22, 18, upper_init, "towel_terry")
    W.simulate_cloth_drape(
        b2, [col_floor, col_core, b1], frames=24, mass=0.42, tension=18.0, bending=0.75,
        collision_dist=0.006, solidify_thickness=0.010, cleanup_colliders=False,
    )

    # Clean up temporary floor/core colliders
    bpy.data.objects.remove(col_floor, do_unlink=True)
    bpy.data.objects.remove(col_core, do_unlink=True)

    # Child-sized contoured honeycomb slippers beside folded blanket
    slippers = []
    slippers.extend(_make_contoured_slipper_hero("SmallL_", (0.23, -0.05, 0.0), yaw=0.26, scale=0.76, mirror=1.0))
    slippers.extend(_make_contoured_slipper_hero("SmallR_", (0.35, 0.03, 0.0), yaw=-0.32, scale=0.76, mirror=-1.0))

    # Subtle physical crumpled cotton tissue / corner pad anchor for Bed4ContactTrace (keeps non-empty part!)
    pad = H.box("Bed4FloorCrumple", (-0.11, 0.04, 0.004), (0.38, 0.26, 0.008), "blanket_bed4", bevel=0.003)

    return {
        "Bed4CottonTrace": [b1, b2],
        "Bed4RubberTrace": slippers,
        "Bed4ContactTrace": [pad],
    }


def run_all():
    W.export_ward_asset(
        "CTCC_PROP_WardBed",
        build_ward_bed,
        {"LOC03_WardBed": {"res": 2048, "bake_r": True}},
        make_lods=True,
        cam_dir=(1.45, -1.55, 0.92),
        ao_samples=10,
    )
    W.export_ward_asset(
        "CTCC_LOC03_WardBedLived",
        build_ward_bed_lived,
        {"LOC03_WardBedLived": {"res": 2048, "bake_r": True}},
        make_lods=True,
        cam_dir=(1.45, -1.55, 0.92),
        ao_samples=10,
    )
    W.export_ward_asset(
        "CTCC_ENV_CurtainAssembly",
        build_curtain_assembly,
        {"LOC03_Curtain": {"res": 1024, "bake_r": True}},
        make_lods=False,
        separate_parts=True,
        cam_dir=(1.05, -2.05, 0.30),
        ao_samples=10,
    )
    W.export_ward_asset(
        "CTCC_PROP_Bed4TracesWorking",
        build_bed4_traces_working,
        {"LOC03_Bed4Trace": {"res": 1024, "bake_r": False}},
        make_lods=False,
        separate_parts=True,
        cam_dir=(0.95, -1.35, 0.95),
        ao_samples=10,
    )


if __name__ == "__main__":
    run_all()
