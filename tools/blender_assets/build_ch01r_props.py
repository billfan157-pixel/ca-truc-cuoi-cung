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
    # Varnished timber writing top
    objs.append(C.make_box("WritingDeck", (0, -0.01, 0.875), (w + 0.03, d + 0.04, 0.034), "WoodDoor", bevel=0.006))

    # Upper wall-mounted chart pigeonhole organizer (Z = 0.90..1.44, depth 0.22 m against back +Y!)
    hy = d * 0.5 - 0.11
    objs.append(C.make_box("HutchBack", (0, d * 0.5 - 0.012, 1.18), (w, 0.022, 0.56), "WoodDoor", bevel=0.004))
    objs.append(C.make_box("HutchTop", (0, hy, 1.46), (w + 0.02, 0.23, 0.026), "WoodDoor", bevel=0.004))
    objs.append(C.make_box("HutchMid", (0, hy, 1.17), (w - 0.02, 0.21, 0.020), "WoodDoor", bevel=0.003))
    for kx in (-0.56, -0.18, 0.18, 0.56):
        objs.append(C.make_box(f"HutchDiv_{kx:.2f}", (kx, hy, 1.18), (0.022, 0.22, 0.54), "WoodDoor", bevel=0.003))
    # Stacked vertical kraft patient folders & brass slot label plates inside pigeonholes
    for i in range(4):
        objs.append(C.make_box(f"FolderSlotL_{i}", (-0.49 + i * 0.072, hy - 0.01, 1.03), (0.042, 0.19, 0.24),
                               "KraftPaper" if i % 2 == 0 else "Paper", bevel=0.004, rot=(0, math.radians((i - 1.5) * 3.0), 0)))
    for i in range(3):
        objs.append(C.make_box(f"FolderSlotR_{i}", (0.28 + i * 0.08, hy - 0.01, 1.31), (0.052, 0.19, 0.23),
                               "EnamelBlue" if i == 1 else "KraftPaper", bevel=0.004))
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


def build_bed4_blanket_trace():
    """Physical trace cluster for OBS101A inside Bed 4 alcove (Layer B):
    folded/displaced thin cotton hospital blanket corner + small pair of child-sized yellow honeycomb slippers
    + stray enamel cup rim mark (zero C01 code, zero patient name — pure physical use trace!)."""
    objs = []

    # Sculpted hastily folded/tucked hospital cotton blanket resting near Bed 4 wall edge
    def fold_fn(u, v, bw, bl):
        x = (u - 0.5) * bw
        y = (v - 0.5) * bl
        ex = math.sin(u * math.pi)
        ey = math.sin(v * math.pi)
        loft = 0.055 * (ex * ey) ** 0.55
        crease = 0.012 * math.sin(u * 18.0 + v * 7.0) * ex * ey
        return (x, y, loft + crease)

    objs.append(C.make_cloth_grid("FoldedBlanketBase", 0.48, 0.36, 22, 18, fold_fn, (-0.12, 0.04, 0.008), "TextileOld", thickness=0.022, uv_tile=(1.6, 1.4)))
    objs.append(C.make_cloth_grid("FoldedBlanketTop", 0.42, 0.31, 18, 16, fold_fn, (-0.10, 0.05, 0.052), "CottonOld", thickness=0.016, uv_tile=(1.5, 1.3)))

    # Small child-sized yellow honeycomb rubber slippers (scaled ~0.76x, placed slightly askew beside blanket)
    for side, pos, yaw in (("L", (0.22, -0.06, 0), 0.28), ("R", (0.34, 0.02, 0), -0.34)):
        ca, sa = math.cos(yaw), math.sin(yaw)

        def pt(lx, ly, lz):
            return (pos[0] + lx * ca - ly * sa, pos[1] + lx * sa + ly * ca, pos[2] + lz)

        objs.append(C.make_box(f"SmallSole_{side}", pt(0, 0, 0.009), (0.068, 0.172, 0.016), "SlipperYellow", bevel=0.006, segments=3, rot=(0, 0, yaw)))
        objs.append(C.make_box(f"SmallVamp_{side}", pt(0, 0.018, 0.027), (0.066, 0.066, 0.032), "SlipperYellow", bevel=0.012, segments=3, rot=(0, 0, yaw)))
        objs.append(C.make_box(f"SmallCav_{side}", pt(0, 0.018, 0.021), (0.048, 0.070, 0.018), "Rubber", bevel=0.004, rot=(0, 0, yaw)))
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
