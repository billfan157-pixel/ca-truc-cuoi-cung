"""CH01-R Investigative Props, Interactive Drawers, Nursing Alcove & Tactile Architecture (Blender 4.5 LTS).

Creates editable .blend sources in ArtSource/Blender/, game-ready FBX meshes in
Prototype/Assets/CTCC/Art/Meshes/, and Cycles studio verification renders in docs/ch01r/blender/.
"""

from __future__ import annotations

import math
import sys
from pathlib import Path

import bmesh
import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))
import common as C

# Direct studio renders into docs/ch01r/blender for commit-linked visual evidence
C.RENDER_DIR = C.ROOT / "docs" / "ch01r" / "blender"
C.RENDER_DIR.mkdir(parents=True, exist_ok=True)


def make_uv_plane(name: str, loc, size_xy, mat_key: str, rot=(0, 0, 0)):
    """Create a crisp 0..1 planar UV-mapped quad mesh (for readable 3D hospital forms & envelopes)."""
    w, h = size_xy
    bm = bmesh.new()
    v0 = bm.verts.new((-w * 0.5, -h * 0.5, 0.0))
    v1 = bm.verts.new((w * 0.5, -h * 0.5, 0.0))
    v2 = bm.verts.new((w * 0.5, h * 0.5, 0.0))
    v3 = bm.verts.new((-w * 0.5, h * 0.5, 0.0))
    face = bm.faces.new((v0, v1, v2, v3))
    uv_layer = bm.loops.layers.uv.new("UVMap")
    uvs = ((0.0, 0.0), (1.0, 0.0), (1.0, 1.0), (0.0, 1.0))
    for loop, uv in zip(face.loops, uvs):
        loop[uv_layer].uv = uv
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new(name + "_mesh")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = loc
    obj.rotation_euler = rot
    # Assign material with direct texture preview if available in TEX_DIR
    mat = bpy.data.materials.get(mat_key)
    if mat is None:
        mat = bpy.data.materials.new(mat_key)
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Roughness"].default_value = 0.86
        tex_file = C.TEX_DIR / f"{mat_key}.png"
        if tex_file.exists():
            tn = mat.node_tree.nodes.new("ShaderNodeTexImage")
            tn.image = bpy.data.images.load(str(tex_file), check_existing=True)
            mat.node_tree.links.new(tn.outputs["Color"], bsdf.inputs["Base Color"])
    obj.data.materials.append(mat)
    return obj


def build_bed4_chart_holder():
    """Grounded rolling/standing steel bedside chart stanchion & aluminum pocket for Bed 4 (OBS101B).
    Completely replaces the floating Bed-4 quad! Height 1.56 m, base Ø0.34 m."""
    objs = []
    # Weighted round painted-steel base + 3 rubber floor glides
    objs.append(C.make_lathe("StandBase", [
        (0.0, 0.012), (0.168, 0.012), (0.172, 0.022), (0.155, 0.048), (0.042, 0.068), (0.022, 0.095), (0.0, 0.095)
    ], (0, 0, 0), "MetalPaint", segments=26))
    for k in range(3):
        a = k * math.tau / 3.0
        gx, gy = math.cos(a) * 0.145, math.sin(a) * 0.145
        objs.append(C.make_cylinder(f"Glide_{k}", (gx, gy, 0.0), (gx, gy, 0.014), 0.016, "Rubber", segments=12))
    # Telescoping brushed stainless pole + knurled height-lock collar
    objs.append(C.make_cylinder("LowerPole", (0, 0, 0.08), (0, 0, 0.92), 0.015, "MetalPaint", segments=16))
    objs.append(C.make_cylinder("LockCollar", (0, 0, 0.89), (0, 0, 0.95), 0.022, "Steel", segments=18, bevel=0.003))
    objs.append(C.make_cylinder("UpperPole", (0, 0, 0.94), (0, 0, 1.54), 0.011, "Steel", segments=16))
    objs.append(C.make_cylinder("TopCap", (0, 0, 1.535), (0, 0, 1.565), 0.016, "PlasticIvory", segments=14))
    # Horizontal cross-bar & hanging aluminum bedside chart board facing -Y (angled slightly upward)
    objs.append(C.make_cylinder("CrossBar", (-0.115, -0.018, 1.49), (0.115, -0.018, 1.49), 0.007, "Steel", segments=12))
    for hx in (-0.075, 0.075):
        objs.append(C.make_pipe(f"Hook_{hx:.2f}", [(hx, -0.018, 1.505), (hx, -0.045, 1.505), (hx, -0.045, 1.455)], 0.0045, "Steel", bevel_res=3))
    objs.append(C.make_box("AluminumPlate", (0, -0.044, 1.335), (0.235, 0.008, 0.305), "Steel", bevel=0.003))
    objs.append(C.make_box("BottomLip", (0, -0.056, 1.188), (0.235, 0.026, 0.012), "Steel", bevel=0.002))
    objs.append(C.make_box("SpringClip", (0, -0.054, 1.462), (0.088, 0.014, 0.026), "Steel", bevel=0.002))
    # UV-mapped readable Bed-4 handover card resting cleanly inside aluminum pocket!
    objs.append(make_uv_plane("ObsCardSheet", (0, -0.050, 1.332), (0.188, 0.250), "ObsCard", rot=(math.radians(90), 0, 0)))
    return objs


def build_nursing_filing_desk():
    """Compact 1990s Vietnamese hospital nursing alcove chart desk & wall pigeonhole rack (1.16 x 0.34 x 1.46 m).
    Designed specifically to hug the STAFF corridor east wall without impeding 1.7m walking clearance!"""
    objs = []
    w, d = 1.16, 0.34
    # 4 Painted steel angle legs with rubber tips
    for sx in (-w * 0.46, w * 0.46):
        for sy in (-d * 0.40, d * 0.40):
            objs.append(C.make_cylinder(f"NFoot_{sx:.2f}_{sy:.2f}", (sx, sy, 0.0), (sx, sy, 0.026), 0.015, "Rubber", segments=12))
            objs.append(C.make_box(f"NLeg_{sx:.2f}_{sy:.2f}", (sx, sy, 0.44), (0.032, 0.032, 0.84), "CabinetPaint", bevel=0.003))
    # Lower storage cross-shelf & steel desk apron housing (leaves open cavity on right for sliding drawer!)
    objs.append(C.make_box("LowerShelf", (0, 0, 0.24), (w - 0.04, d - 0.04, 0.022), "CabinetPaint", bevel=0.003))
    objs.append(C.make_box("DeskBackApron", (0, d * 0.46, 0.77), (w - 0.02, 0.022, 0.19), "CabinetPaint", bevel=0.003))
    objs.append(C.make_box("DeskSideL", (-w * 0.48, 0, 0.77), (0.024, d, 0.19), "CabinetPaint", bevel=0.003))
    objs.append(C.make_box("DeskSideR", (w * 0.48, 0, 0.77), (0.024, d, 0.19), "CabinetPaint", bevel=0.003))
    objs.append(C.make_box("DeskDivider", (-0.02, 0, 0.77), (0.020, d - 0.02, 0.18), "CabinetPaint", bevel=0.002))
    objs.append(C.make_box("DeskLeftShelf", (-0.29, 0.01, 0.68), (0.52, d - 0.04, 0.020), "CabinetPaint", bevel=0.002))
    # Varnished timber writing top (lengthwise grain)
    objs.append(C.make_box("WritingDeck", (0, -0.01, 0.875), (w + 0.03, d + 0.04, 0.034), "WoodDoor", bevel=0.006, uv_rot90=True))

    # Upper wall-mounted chart pigeonhole organizer (Z = 0.90..1.44, depth 0.22 m against back +Y!)
    hy = d * 0.5 - 0.11
    objs.append(C.make_box("HutchBack", (0, d * 0.5 - 0.012, 1.18), (w, 0.022, 0.56), "WoodDoor", bevel=0.004))
    objs.append(C.make_box("HutchTop", (0, hy, 1.46), (w + 0.02, 0.23, 0.026), "WoodDoor", bevel=0.004, uv_rot90=True))
    objs.append(C.make_box("HutchMid", (0, hy, 1.17), (w - 0.02, 0.21, 0.020), "WoodDoor", bevel=0.003, uv_rot90=True))
    for kx in (-0.56, -0.18, 0.18, 0.56):
        objs.append(C.make_box(f"HutchDiv_{kx:.2f}", (kx, hy, 1.18), (0.022, 0.22, 0.54), "WoodDoor", bevel=0.003))
    # Brass pigeonhole index label frames along mid shelf lip
    for lx in (-0.37, 0.0, 0.37):
        objs.append(C.make_box(f"HutchTag_{lx:.2f}", (lx, hy - 0.108, 1.17), (0.095, 0.006, 0.018), "Brass", bevel=0.002))
    # Varied lived-in kraft/manila binders, leaning charts & a flat ward log stack inside pigeonholes
    left_binders = [
        (-0.505, 1.035, 0.036, 0.245, -3.5, "KraftPaper"),
        (-0.452, 1.028, 0.040, 0.232, -1.0, "Paper"),
        (-0.398, 1.042, 0.044, 0.258, 2.5, "EnamelBlue"),
        (-0.336, 1.022, 0.034, 0.218, 8.5, "KraftPaper"),
    ]
    for i, (fx, fz, fw, fh, fl, fmat) in enumerate(left_binders):
        objs.append(C.make_box(f"FolderSlotL_{i}", (fx, hy - 0.012, fz), (fw, 0.195, fh),
                               fmat, bevel=0.004, rot=(0, math.radians(fl), 0)))
    # Horizontal flat paper stack + enamel tray in center-upper cubby
    for s_idx in range(3):
        objs.append(C.make_box(f"FlatStack_{s_idx}", (-0.01 + s_idx * 0.006, hy - 0.015, 1.202 + s_idx * 0.017),
                               (0.27, 0.19, 0.014), "Paper" if s_idx == 2 else "KraftPaper",
                               bevel=0.002, rot=(0, 0, (s_idx - 1) * 0.06)))
    right_binders = [
        (0.255, 1.315, 0.046, 0.248, -2.0, "KraftPaper"),
        (0.318, 1.305, 0.052, 0.232, 0.5, "EnamelBlue"),
        (0.388, 1.312, 0.042, 0.242, 4.0, "Paper"),
        (0.452, 1.298, 0.038, 0.215, 11.0, "KraftPaper"),
    ]
    for i, (fx, fz, fw, fh, fl, fmat) in enumerate(right_binders):
        objs.append(C.make_box(f"FolderSlotR_{i}", (fx, hy - 0.012, fz), (fw, 0.195, fh),
                               fmat, bevel=0.004, rot=(0, math.radians(fl), 0)))
    # Compact gooseneck hospital reading sconce clipped to upper hutch right edge (anchors STAFF task lighting!)
    objs.append(C.make_box("DeskLampClamp", (0.50, hy - 0.08, 1.46), (0.048, 0.056, 0.042), "MetalOld", bevel=0.004))
    objs.append(C.make_pipe("DeskLampArm", [(0.50, hy - 0.08, 1.48), (0.50, hy - 0.12, 1.60), (0.42, hy - 0.22, 1.62)], 0.007, "Steel", bevel_res=3))
    objs.append(C.make_lathe("DeskLampHood", [(0.012, 0.04), (0.030, 0.025), (0.072, -0.045), (0.068, -0.048), (0.026, 0.02)],
                             (0.40, hy - 0.23, 1.59), "MetalPaint", segments=20, rot=(math.radians(24), math.radians(-18), 0)))
    # Open shift binder resting cleanly on left writing deck (well clear of hutch divider at X=-0.18) + pen cup
    objs.append(C.make_box("ShiftBinderCover", (-0.36, -0.04, 0.902), (0.30, 0.22, 0.018), "EnamelBlue", bevel=0.003, rot=(0, 0, 0.05)))
    objs.append(make_uv_plane("ShiftGuidePage", (-0.36, -0.04, 0.914), (0.19, 0.25), "Doc_ShiftGuide", rot=(0, 0, math.radians(86))))
    objs.append(C.make_cylinder("PenCup", (0.44, 0.04, 0.892), (0.44, 0.04, 0.985), 0.036, "Ceramic", segments=18))
    objs.append(C.make_cylinder("PenBlue", (0.43, 0.04, 0.94), (0.46, 0.02, 1.07), 0.005, "EnamelBlue", segments=10))
    objs.append(C.make_cylinder("PenRed", (0.45, 0.05, 0.94), (0.42, 0.07, 1.06), 0.005, "RedPaint", segments=10))
    # Framed cork/painted notice pinboard directly above left wall (mounted at Z=1.62..2.18!)
    objs.append(C.make_box("NoticeFrame", (0, d * 0.5 - 0.018, 1.88), (0.64, 0.028, 0.58), "WoodDoor", bevel=0.005))
    objs.append(make_uv_plane("NoticePaper", (0, d * 0.5 - 0.034, 1.88), (0.40, 0.52), "Doc_WardOverflow", rot=(math.radians(90), 0, 0)))
    return objs


def build_interactive_drawer():
    """Modular enameled steel pull-out drawer (0.44 wide x 0.30 deep x 0.14 tall) with curved chrome pull,
    metal side slides, and folded document sheet resting inside! Front faces -Y."""
    objs = []
    w, d, h = 0.44, 0.30, 0.14
    # Drawer front face plate + recessed chrome bar pull
    objs.append(C.make_box("DrawerFace", (0, -d * 0.5, 0), (w, 0.022, h), "CabinetPaint", bevel=0.005))
    objs.append(C.make_pipe("DrawerChromePull", [
        (-0.08, -d * 0.5 - 0.010, 0.004), (-0.07, -d * 0.5 - 0.038, 0.004),
        (0.07, -d * 0.5 - 0.038, 0.004), (0.08, -d * 0.5 - 0.010, 0.004),
    ], 0.007, "Steel", bevel_res=4))
    # Label holder frame on drawer face
    objs.append(C.make_box("LabelFrame", (0, -d * 0.5 - 0.013, 0.044), (0.092, 0.006, 0.028), "Brass", bevel=0.002))
    # Steel box carcass (sides, back, bottom)
    objs.append(C.make_box("DrawerBottom", (0, 0.005, -h * 0.44), (w - 0.03, d - 0.02, 0.012), "MetalOld", bevel=0.002))
    objs.append(C.make_box("DrawerBack", (0, d * 0.5 - 0.01, -0.008), (w - 0.03, 0.016, h - 0.024), "MetalOld", bevel=0.002))
    for s in (-1, 1):
        objs.append(C.make_box(f"DrawerSide_{s}", (s * (w * 0.5 - 0.020), 0.005, -0.008), (0.016, d - 0.02, h - 0.024), "MetalOld", bevel=0.002))
        objs.append(C.make_box(f"DrawerRail_{s}", (s * (w * 0.5 - 0.008), 0.01, -0.018), (0.010, d - 0.03, 0.018), "Steel", bevel=0.001))
    # Interior kraft folder & folded paper slip visible as soon as player pulls the drawer!
    objs.append(C.make_box("InnerFolder", (-0.01, 0.00, -h * 0.34), (0.31, 0.22, 0.010), "KraftPaper", bevel=0.002, rot=(0, 0, 0.05)))
    objs.append(C.make_box("InnerSheet", (0.01, -0.01, -h * 0.27), (0.26, 0.19, 0.005), "Paper", bevel=0.001, rot=(0, 0, -0.04)))
    return objs


def _make_contoured_slipper(prefix: str, pos, yaw: float, scale: float = 0.76, mirror: float = 1.0):
    """Build an authentic contoured Vietnamese honeycomb rubber slipper (dép tổ ong):
    curved anatomical sole + hollow arched open-toe molded strap with honeycomb vent dots (zero boxy Lego shape!)."""
    parts = []
    ca, sa = math.cos(yaw), math.sin(yaw)

    def xform(lx, ly, lz):
        sx = lx * mirror * scale
        sy = ly * scale
        sz = lz * scale
        return (pos[0] + sx * ca - sy * sa, pos[1] + sx * sa + sy * ca, pos[2] + sz)

    # 1. Anatomical foot-outline sole extruded in bmesh (heel narrower, forefoot wider, slight toe-spring!)
    outline = [
        (-0.028, -0.108), (-0.038, -0.076), (-0.035, -0.022), (-0.046, 0.038),
        (-0.044, 0.088), (-0.022, 0.115), (0.014, 0.116), (0.040, 0.095),
        (0.045, 0.042), (0.033, -0.024), (0.036, -0.078), (0.024, -0.109),
        (0.000, -0.114),
    ]
    bm = bmesh.new()
    bot_v = []
    top_v = []
    for ox, oy in outline:
        toe_lift = max(0.0, (oy - 0.065) * 0.11)
        bot_v.append(bm.verts.new(xform(ox, oy, toe_lift)))
        top_v.append(bm.verts.new(xform(ox * 0.96, oy * 0.98, 0.016 + toe_lift)))
    bm.faces.new(list(reversed(bot_v)))
    bm.faces.new(top_v)
    n = len(outline)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((bot_v[i], bot_v[j], top_v[j], top_v[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4])
    mesh = bpy.data.meshes.new(f"{prefix}Sole_mesh")
    bm.to_mesh(mesh)
    bm.free()
    sole = bpy.data.objects.new(f"{prefix}Sole", mesh)
    bpy.context.collection.objects.link(sole)
    b = sole.modifiers.new("Bevel", "BEVEL")
    b.width = 0.0032 * scale
    b.segments = 2
    b.limit_method = "ANGLE"
    parts.append(C.finalize_mesh(sole, f"{prefix}Sole", "SlipperYellow", smooth_angle=48.0, uv_scale=0.35))

    # 2. Hollow arched molded upper vamp strap (open front & open back — real curved bridge, NOT a solid block!)
    bm2 = bmesh.new()
    y_stations = (-0.022, -0.002, 0.022, 0.046, 0.066)
    rings_out = []
    rings_in = []
    arch_steps = 11
    for yi, ly in enumerate(y_stations):
        # Strap narrows slightly toward toe and drops gently in height toward front opening
        width_scale = 0.94 + 0.10 * math.sin((yi / 4.0) * math.pi)
        arch_h = (0.046 - yi * 0.0022)
        half_w = 0.041 * width_scale
        ro, ri = [], []
        for k in range(arch_steps):
            t = k / float(arch_steps - 1)  # 0..1 across arch
            ang = t * math.pi
            cx = -math.cos(ang) * half_w
            cz = 0.013 + math.sin(ang) * arch_h
            # Wall thickness ~5.5mm
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
    # Cap front & back arch edges
    for k in range(arch_steps - 1):
        bm2.faces.new((rings_out[0][k + 1], rings_out[0][k], rings_in[0][k], rings_in[0][k + 1]))
        bm2.faces.new((rings_out[-1][k], rings_out[-1][k + 1], rings_in[-1][k + 1], rings_in[-1][k]))
    bmesh.ops.recalc_face_normals(bm2, faces=bm2.faces)
    mesh2 = bpy.data.meshes.new(f"{prefix}Vamp_mesh")
    bm2.to_mesh(mesh2)
    bm2.free()
    vamp = bpy.data.objects.new(f"{prefix}Vamp", mesh2)
    bpy.context.collection.objects.link(vamp)
    parts.append(C.finalize_mesh(vamp, f"{prefix}Vamp", "SlipperYellow", smooth_angle=52.0, uv_scale=0.35))

    # 3. Signature recessed honeycomb ventilation dots along top crown of strap
    for hy in (-0.004, 0.020, 0.044):
        for hx in (-0.016, 0.0, 0.016):
            r_frac = abs(hx) / 0.043
            hz = 0.013 + math.sqrt(max(0.05, 1.0 - r_frac * r_frac)) * 0.044
            p0 = xform(hx, hy, hz - 0.002)
            p1 = xform(hx, hy, hz + 0.0018)
            parts.append(C.make_cylinder(f"{prefix}Dot_{hy:.3f}_{hx:.3f}", p0, p1, 0.0038 * scale, "Rubber", segments=8))
    return parts


def build_bed4_blanket_trace():
    """Physical trace cluster for OBS101A inside Bed 4 alcove (Layer B):
    layered hastily folded/creased hospital cotton blanket + authentic contoured child-sized honeycomb rubber slippers
    (zero C01 code, zero patient name — pure physical use trace!)."""
    objs = []

    # Sculpted hastily folded & softly rumpled cotton hospital blanket resting near Bed 4 floor edge
    # Distinct lower folded sage blanket + upper folded cream sheet lip + corner fold drape (NOT a smooth soap bar!)
    def lower_fold_fn(u, v, bw, bl):
        x = (u - 0.5) * bw + 0.014 * math.sin(v * 7.5 + 0.4)
        y = (v - 0.5) * bl + 0.010 * math.cos(u * 6.0)
        # Flat folded rectangle with rounded rolled spine at u=0 and soft sagging open edges at u=1
        edge_x = min(1.0, u / 0.14) * min(1.0, (1.0 - u) / 0.18)
        edge_y = min(1.0, v / 0.15) * min(1.0, (1.0 - v) / 0.15)
        loft = 0.036 * (edge_x ** 0.45) * (edge_y ** 0.45)
        spine_roll = 0.016 * math.exp(-((u - 0.10) / 0.09) ** 2) * edge_y
        creases = 0.011 * math.sin(u * 14.0 + v * 9.0) * edge_x * edge_y
        creases += 0.007 * math.cos(u * 23.0 - v * 12.0) * edge_x * edge_y
        return (x, y, max(0.0, loft + spine_roll + creases))

    def upper_fold_fn(u, v, bw, bl):
        x = (u - 0.5) * bw + 0.012 * math.cos(v * 9.0)
        y = (v - 0.5) * bl + 0.012 * math.sin(u * 8.0)
        edge_x = min(1.0, u / 0.14) * min(1.0, (1.0 - u) / 0.16)
        edge_y = min(1.0, v / 0.14) * min(1.0, (1.0 - v) / 0.14)
        # Asymmetric tucked corner flap dropping softly onto lower blanket
        corner_drop = -0.022 * max(0.0, (u - 0.65) / 0.35) * max(0.0, (v - 0.55) / 0.45)
        loft = 0.024 * (edge_x ** 0.42) * (edge_y ** 0.42)
        folds = 0.010 * math.sin(u * 19.0 - v * 11.0) * edge_x * edge_y
        return (x, y, loft + folds + corner_drop)

    b1 = C.make_cloth_grid("FoldedBlanketBase", 0.52, 0.38, 26, 20, lower_fold_fn, (-0.12, 0.04, 0.004), "TextileOld", thickness=0.018, uv_tile=(2.0, 1.6))
    b2 = C.make_cloth_grid("FoldedBlanketTop", 0.44, 0.32, 22, 18, upper_fold_fn, (-0.09, 0.06, 0.044), "CottonOld", thickness=0.014, uv_tile=(1.8, 1.4))
    b2.rotation_euler.z = 0.11
    bpy.ops.object.select_all(action="DESELECT")
    b2.select_set(True)
    bpy.context.view_layer.objects.active = b2
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    objs.extend([b1, b2])

    # Small child-sized contoured honeycomb rubber slippers (~17.5 cm, slightly askew beside folded blanket)
    # Keep "Small*" prefix so build_ch01_rebirth.py binds Bed4RubberTrace -> OBS101A_SLIPPERS cleanly!
    objs.extend(_make_contoured_slipper("SmallL_", (0.23, -0.05, 0.0), yaw=0.26, scale=0.76, mirror=1.0))
    objs.extend(_make_contoured_slipper("SmallR_", (0.35, 0.03, 0.0), yaw=-0.32, scale=0.76, mirror=-1.0))
    return objs


def build_staff_cross_door():
    """Compact 0.94 x 2.18 m Vietnamese hospital hardwood-and-frosted-glass staff service door leaf
    connecting STAFF Nursing Alcove <-> HALL Main Corridor. Pivot at hinge X=0, Y=0, Z=0."""
    objs = []
    w, h, t = 0.94, 2.18, 0.046
    stile = 0.11
    inner_w = w - stile * 2
    objs.append(C.make_box("StaffHingeStile", (stile * 0.5, 0, h * 0.5), (stile, t, h), "WoodDoor", bevel=0.004))
    objs.append(C.make_box("StaffLatchStile", (w - stile * 0.5, 0, h * 0.5), (stile, t, h), "WoodDoor", bevel=0.004))
    objs.append(C.make_box("StaffBotRail", (w * 0.5, 0, 0.14), (inner_w + 0.01, t, 0.28), "WoodDoor", bevel=0.004))
    objs.append(C.make_box("StaffMidRail", (w * 0.5, 0, 1.06), (inner_w + 0.01, t, 0.15), "WoodDoor", bevel=0.004))
    objs.append(C.make_box("StaffTopRail", (w * 0.5, 0, h - 0.08), (inner_w + 0.01, t, 0.16), "WoodDoor", bevel=0.004))
    objs.append(C.make_box("StaffLowPanel", (w * 0.5, 0, 0.63), (inner_w - 0.02, t * 0.55, 0.68), "WoodDoor", bevel=0.006))
    objs.append(C.make_box("StaffGlass", (w * 0.5, 0, 1.58), (inner_w - 0.02, 0.010, 0.86), "FrostedGlass", bevel=0.0))
    objs.append(C.make_box("StaffMuntin", (w * 0.5, 0, 1.58), (inner_w, 0.028, 0.026), "WoodDoor", bevel=0.002))
    # Stainless push-plate & curved lever handle on both sides
    hx, hz = w - 0.085, 1.02
    for s in (-1, 1):
        ey = s * (t * 0.5 + 0.004)
        objs.append(C.make_box(f"StaffKick_{s}", (w * 0.5, ey, 0.15), (w - 0.05, 0.004, 0.26), "Steel", bevel=0.001))
        objs.append(C.make_box(f"StaffPlate_{s}", (hx, ey, hz - 0.02), (0.044, 0.007, 0.19), "Steel", bevel=0.002))
        objs.append(C.make_pipe(f"StaffLever_{s}", [(hx, ey, hz), (hx, ey + s * 0.052, hz), (hx - 0.12, ey + s * 0.052, hz)], 0.009, "Steel", bevel_res=3))
    return objs


def build_clipboard_f101_v2():
    """Refined UV-mapped masonite hospital clipboard (0.265 x 0.365 m) with planar 0..1 F101 front page,
    layered under-sheets, brass rivets, stamped spring clip, and chained hospital pen."""
    objs = []
    objs.append(C.make_box("Board", (0, 0.006, 0), (0.265, 0.011, 0.365), "WoodDoor", bevel=0.004))
    objs.append(C.make_box("PaperUnder", (0.003, -0.002, -0.008), (0.224, 0.004, 0.312), "Paper", bevel=0.001, rot=(0, 0.014, 0)))
    objs.append(make_uv_plane("PaperTop", (0, -0.0055, -0.006), (0.222, 0.308), "F101", rot=(math.radians(90), 0, 0)))
    objs.append(C.make_box("ClipPlate", (0, -0.012, 0.155), (0.105, 0.011, 0.036), "Steel", bevel=0.003))
    objs.append(C.make_pipe("ClipWire", [(-0.042, -0.017, 0.145), (-0.042, -0.025, 0.175),
                                         (0.042, -0.025, 0.175), (0.042, -0.017, 0.145)], 0.004, "Steel", bevel_res=3))
    for rx in (-0.036, 0.036):
        objs.append(C.make_cylinder(f"ClipRivet_{rx:.2f}", (rx, 0.012, 0.155), (rx, -0.019, 0.155), 0.005, "Brass", segments=10))
    objs.append(C.make_cylinder("BoardPen", (0.105, -0.013, 0.01), (0.118, -0.013, 0.145), 0.005, "EnamelBlue", segments=10))
    return objs


def build_bay_envelope_v2():
    """Refined Bảy kraft envelope (E10a) with readable planar UV-mapped front address face,
    red string-button tie, peeking folded letter, and distinct steel + aged brass keys!"""
    objs = []
    objs.append(C.make_box("EnvelopeBody", (0, 0, 0.008), (0.26, 0.176, 0.014), "KraftPaper", bevel=0.004))
    # Readable front address & stamp face sitting cleanly on top (+Z) of envelope
    objs.append(make_uv_plane("EnvelopeFrontFace", (0, 0, 0.0156), (0.252, 0.168), "Doc_BayEnvelopeFront", rot=(0, 0, 0)))
    objs.append(C.make_cylinder("ButtonWasherA", (-0.088, 0.052, 0.016), (-0.088, 0.052, 0.020), 0.0085, "RedPaint", segments=14))
    objs.append(C.make_cylinder("ButtonWasherB", (-0.088, 0.006, 0.016), (-0.088, 0.006, 0.020), 0.0085, "RedPaint", segments=14))
    objs.append(C.make_pipe("TieString", [(-0.088, 0.052, 0.019), (-0.082, 0.029, 0.019), (-0.088, 0.006, 0.019), (-0.094, 0.029, 0.019)], 0.0014, "CottonOld", bevel_res=3, cyclic=True))
    objs.append(C.make_box("LetterPeek", (0.046, 0.004, 0.011), (0.205, 0.142, 0.004), "Paper", bevel=0.001, rot=(0, 0, -0.045)))
    # Two distinct period keys on split steel ring tucked cleanly near bottom-right corner (never covering recipient name!)
    kx0, ky0 = 0.082, -0.056
    objs.append(C.make_pipe("KeyRing", [(kx0 + 0.015 * math.cos(k * math.tau / 16), ky0 + 0.015 * math.sin(k * math.tau / 16), 0.020)
                                        for k in range(16)], 0.002, "Steel", bevel_res=3, cyclic=True))
    for k_idx, (k_mat, ang) in enumerate((("Steel", -0.25), ("Brass", 0.35))):
        ca, sa = math.cos(ang), math.sin(ang)
        z_off = k_idx * 0.0038
        objs.append(C.make_cylinder(f"KeyBow_{k_idx}", (kx0 - ca * 0.019, ky0 - sa * 0.019, 0.0175 + z_off),
                                    (kx0 - ca * 0.019, ky0 - sa * 0.019, 0.0210 + z_off), 0.013, k_mat, segments=14))
        objs.append(C.make_box(f"KeyBlade_{k_idx}", (kx0 - ca * 0.052, ky0 - sa * 0.052, 0.0192 + z_off),
                               (0.048, 0.0075, 0.003), k_mat, bevel=0.001, rot=(0, 0, ang)))
        objs.append(C.make_box(f"KeyBit_{k_idx}", (kx0 - ca * 0.068 - sa * 0.006, ky0 - sa * 0.068 + ca * 0.006, 0.0192 + z_off),
                               (0.014, 0.011, 0.003), k_mat, bevel=0.001, rot=(0, 0, ang)))
    return objs


def _triangulate_ngons(objs):
    """Triangulate any n-gon faces (>4 verts) so Unity FBX computes clean tangent spaces with zero warnings."""
    for o in objs:
        if o.type != "MESH" or not o.data:
            continue
        if any(len(p.vertices) > 4 for p in o.data.polygons):
            bm = bmesh.new()
            bm.from_mesh(o.data)
            ngons = [f for f in bm.faces if len(f.verts) > 4]
            if ngons:
                bmesh.ops.triangulate(bm, faces=ngons)
                bm.to_mesh(o.data)
                o.data.update()
            bm.free()
    return objs


def run_all():
    C.export_asset("CTCC_PROP_Bed4ChartHolder", lambda: _triangulate_ngons(build_bed4_chart_holder()), make_lods=False, render=True, cam_dir=(0.95, -2.6, 0.75))
    C.export_asset("CTCC_PROP_NursingFilingDesk", lambda: _triangulate_ngons(build_nursing_filing_desk()), make_lods=True, render=True, cam_dir=(1.4, -2.8, 1.15))
    C.export_asset("CTCC_PROP_InteractiveDrawer", lambda: _triangulate_ngons(build_interactive_drawer()), make_lods=False, render=True, cam_dir=(0.8, -1.5, 1.15))
    C.export_asset("CTCC_PROP_Bed4BlanketTrace", lambda: _triangulate_ngons(build_bed4_blanket_trace()), make_lods=False, render=True, cam_dir=(0.8, -1.4, 1.35))
    C.export_asset("CTCC_ENV_StaffCrossDoor", lambda: _triangulate_ngons(build_staff_cross_door()), make_lods=False, render=True, cam_dir=(1.1, -2.9, 0.55))
    C.export_asset("CTCC_PROP_ClipboardF101", lambda: _triangulate_ngons(build_clipboard_f101_v2()), make_lods=False, render=True, cam_dir=(0.45, -1.9, 0.35))
    C.export_asset("CTCC_PROP_BayEnvelope", lambda: _triangulate_ngons(build_bay_envelope_v2()), make_lods=False, render=True, cam_dir=(0.25, -0.95, 1.65))
    print("CH01-R Blender 4.5 asset suite exported and rendered cleanly.")


if __name__ == "__main__":
    run_all()
