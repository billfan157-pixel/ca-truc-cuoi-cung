"""LOC02 hero-asset production library (Blender 4.5).

Full High-Fidelity Semi-Realistic PBR Workflow:
  1. Sculpted/bevelled/swept/lathed geometry in real metric coordinates.
  2. Procedural PBR materials with AGE parameter:
       AGE = 0 -> Reality B (2005 operational hospital night shift)
       AGE = 1 -> Reality R (2012 abandoned, damp, worn, dusty)
  3. Clean multi-island UV atlas (preserve PRINT UVs for readable documents/dials/labels,
     pack BakeUV cleanly with generous padding).
  4. Real Cycles CPU multi-pass PBR baking -> Unity URP textures:
       <Atlas>_B_Color.png, <Atlas>_R_Color.png,
       <Atlas>_Normal.png,
       <Atlas>_B_Mask.png (R=Metallic, G=Cycles AO, B=0, A=Smoothness),
       <Atlas>_R_Mask.png (R=Metallic, G=Cycles AO, B=0, A=Smoothness)
  5. Save editable .blend to ArtSource/Blender/LOC02/<Asset>.blend
     AND export game-ready FBX (with optional LOD1/LOD2) to Prototype/Assets/CTCC/Art/Meshes/<Asset>.fbx.
  6. Render side-by-side B (2005) & R (2012) Cycles verification previews!

Axis convention (matched with PlaceBlenderModel yawDeg=0 which rotates 180 deg Y in Unity):
    Blender +X = Unity +X (east, visitors' side)
    Blender +Y = Unity +Z (north, toward corridor)
    Blender +Z = Unity +Y (up)
"""
import hashlib
import math
import os
import sys
from pathlib import Path

import bmesh
import bpy
import numpy as np
from mathutils import Euler, Matrix, Vector

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))
import common as C  # noqa: E402

ROOT = C.ROOT
SRC_DIR = ROOT / "ArtSource" / "Blender" / "LOC02"
FBX_DIR = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Meshes"
TEX_DIR = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Textures"
RENDER_DIR = ROOT / "output" / "loc02-hero" / "blender-renders"
PRINT_DIR = ROOT / "ArtSource" / "LOC02" / "prints"

SRC_DIR.mkdir(parents=True, exist_ok=True)
FBX_DIR.mkdir(parents=True, exist_ok=True)
TEX_DIR.mkdir(parents=True, exist_ok=True)
RENDER_DIR.mkdir(parents=True, exist_ok=True)


def _seed(name: str) -> float:
    return int(hashlib.md5(name.encode("utf-8")).hexdigest()[:8], 16) / 0xFFFFFFFF


def _axis_vec(grain: str):
    return {"X": (1.0, 0.0, 0.0), "Y": (0.0, 1.0, 0.0), "Z": (0.0, 0.0, 1.0), "N": (0.0, 0.0, 0.0)}[grain]


def set_kind(obj, kind: str, grain: str = "N"):
    obj.data.materials.clear()
    obj.data.materials.append(get_mat(kind))
    me = obj.data
    if "grain" in me.color_attributes:
        me.color_attributes.remove(me.color_attributes["grain"])
    attr = me.color_attributes.new(name="grain", type="FLOAT_COLOR", domain="POINT")
    gx, gy, gz = _axis_vec(grain)
    n = len(me.vertices)
    attr.data.foreach_set("color", [gx, gy, gz, _seed(obj.name)] * n)
    obj["kind"] = kind
    return obj


def set_planar_print_uv(obj, axis: str = "Z", uv_rect=(0.0, 0.0, 1.0, 1.0), flip_u=False, flip_v=False, rot90=False):
    """Assign a dedicated 'PRINT' UV map projecting bounding box onto uv_rect=(u0,v0,u1,v1)."""
    bpy.context.view_layer.update()
    me = obj.data
    if "PRINT" not in me.uv_layers:
        me.uv_layers.new(name="PRINT")
    uv_layer = me.uv_layers["PRINT"].data
    mw = obj.matrix_world
    coords = [mw @ v.co for v in me.vertices]
    if not coords:
        return obj
    if axis == "Z":
        us = [p.x for p in coords]
        vs = [p.y for p in coords]
    elif axis == "X":
        us = [p.y for p in coords]
        vs = [p.z for p in coords]
    else:  # Y
        us = [p.x for p in coords]
        vs = [p.z for p in coords]
    umin, umax = min(us), max(us)
    vmin, vmax = min(vs), max(vs)
    du = max(umax - umin, 1e-6)
    dv = max(vmax - vmin, 1e-6)
    ru0, rv0, ru1, rv1 = uv_rect
    for poly in me.polygons:
        for li in poly.loop_indices:
            p = mw @ me.vertices[me.loops[li].vertex_index].co
            if axis == "Z":
                u, v = (p.x - umin) / du, (p.y - vmin) / dv
            elif axis == "X":
                u, v = (p.y - umin) / du, (p.z - vmin) / dv
            else:
                u, v = (p.x - umin) / du, (p.z - vmin) / dv
            if rot90:
                u, v = v, 1.0 - u
            if flip_u:
                u = 1.0 - u
            if flip_v:
                v = 1.0 - v
            uv_layer[li].uv = (ru0 + u * (ru1 - ru0), rv0 + v * (rv1 - rv0))
    return obj


def set_cylindrical_print_uv(obj, z0: float, z1: float, angle_offset: float = 0.0):
    """Cylindrical wrap UV onto 'PRINT' layer (e.g. battery label around Z cylinder)."""
    me = obj.data
    if "PRINT" not in me.uv_layers:
        me.uv_layers.new(name="PRINT")
    uv_layer = me.uv_layers["PRINT"].data
    mw = obj.matrix_world
    dz = max(z1 - z0, 1e-6)
    # Use object center in XY
    pts = [mw @ v.co for v in me.vertices]
    cx = sum(p.x for p in pts) / max(len(pts), 1)
    cy = sum(p.y for p in pts) / max(len(pts), 1)
    for poly in me.polygons:
        for li in poly.loop_indices:
            p = mw @ me.vertices[me.loops[li].vertex_index].co
            ang = math.atan2(p.y - cy, p.x - cx) + angle_offset
            u = (ang / math.tau) % 1.0
            v = max(0.0, min(1.0, (p.z - z0) / dz))
            uv_layer[li].uv = (u, v)
    return obj


# --------------------------------------------------------------------------------------
# Geometry primitives
# --------------------------------------------------------------------------------------

def box(name, loc, size, kind, bevel=0.004, seg=2, rot=(0, 0, 0), grain="N"):
    obj = C.make_box(name, loc, size, "Black", bevel=bevel, segments=seg, rot=rot)
    return set_kind(obj, kind, grain)


def cyl(name, p0, p1, r, kind, seg=24, r2=None, bevel=0.0, cap=True, grain="N"):
    obj = C.make_cylinder(name, p0, p1, r, "Black", segments=seg, r_end=r2, cap=cap, bevel=bevel)
    return set_kind(obj, kind, grain)


def pipe(name, pts, r, kind, res=6, cyclic=False, grain="N"):
    obj = C.make_pipe(name, pts, r, "Black", bevel_res=res, cyclic=cyclic)
    return set_kind(obj, kind, grain)


def lathe(name, profile, loc, kind, seg=40, rot=(0, 0, 0), grain="N"):
    obj = C.make_lathe(name, profile, loc, "Black", segments=seg, rot=rot)
    return set_kind(obj, kind, grain)


def sphere(name, loc, dims, kind, seg=28, rings=16, rot=(0, 0, 0)):
    obj = C.make_sphere(name, loc, dims, "Black", segs=seg, rings=rings, rot=rot)
    return set_kind(obj, kind)


def sweep(name, profile, path, kind, closed=True, caps=True, up=(0, 0, 1), grain="N", smooth=38.0):
    path = [Vector(p) for p in path]
    up = Vector(up)
    bm = bmesh.new()
    rings = []
    n = len(path)
    count = len(profile)
    for i, p in enumerate(path):
        if i == 0:
            t = (path[1] - p).normalized()
        elif i == n - 1:
            t = (p - path[i - 1]).normalized()
        else:
            t = ((path[i + 1] - p).normalized() + (p - path[i - 1]).normalized()).normalized()
        side = t.cross(up)
        if side.length < 1e-6:
            side = t.cross(Vector((1, 0, 0)))
        side.normalize()
        vup = side.cross(t).normalized()
        scale = 1.0
        if 0 < i < n - 1:
            a = (p - path[i - 1]).normalized()
            b = (path[i + 1] - p).normalized()
            half = math.acos(max(-1.0, min(1.0, a.dot(b)))) / 2.0
            scale = 1.0 / max(math.cos(half), 0.35)
        rings.append([bm.verts.new(p + side * (u * scale) + vup * v) for (u, v) in profile])
    for i in range(n - 1):
        for j in range(count if closed else count - 1):
            a, b = rings[i][j], rings[i][(j + 1) % count]
            c, d = rings[i + 1][(j + 1) % count], rings[i + 1][j]
            bm.faces.new((a, b, c, d))
    if caps and closed:
        bm.faces.new(rings[0][::-1])
        bm.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new(name + "_mesh")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj = C.finalize_mesh(obj, name, "Black", smooth_angle=smooth)
    return set_kind(obj, kind, grain)


def extrude_outline(name, outline_xy, z0, z1, kind, bevel=0.004, grain="N"):
    bm = bmesh.new()
    bottom = [bm.verts.new((x, y, z0)) for (x, y) in outline_xy]
    top = [bm.verts.new((x, y, z1)) for (x, y) in outline_xy]
    k = len(outline_xy)
    bm.faces.new(bottom[::-1])
    bm.faces.new(top)
    for i in range(k):
        j = (i + 1) % k
        bm.faces.new((bottom[i], bottom[j], top[j], top[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new(name + "_mesh")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    if bevel > 0:
        b = obj.modifiers.new("Bevel", "BEVEL")
        b.width = bevel
        b.segments = 2
        b.limit_method = "ANGLE"
    obj = C.finalize_mesh(obj, name, "Black", smooth_angle=42.0)
    return set_kind(obj, kind, grain)


def sculpted_grid(name, nx, ny, pos_fn, kind, thickness=0.004, grain="N", print_uv=False, uv_rect=(0.0, 0.0, 1.0, 1.0)):
    """Build a smoothly curved parametric surface (e.g. arched book pages, snake plant leaves, contoured seat)."""
    verts, faces = [], []
    for j in range(ny + 1):
        for i in range(nx + 1):
            u, v = i / float(nx), j / float(ny)
            verts.append(pos_fn(u, v))
            if i and j:
                k = j * (nx + 1) + i
                faces.append((k - nx - 2, k - nx - 1, k, k - 1))
    mesh = bpy.data.meshes.new(name + "_mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    uv = mesh.uv_layers.new(name="UVMap")
    puv = mesh.uv_layers.new(name="PRINT") if print_uv else None
    ru0, rv0, ru1, rv1 = uv_rect
    for poly in mesh.polygons:
        poly.use_smooth = True
        for li in poly.loop_indices:
            vi = mesh.loops[li].vertex_index
            j, i = divmod(vi, nx + 1)
            uu, vv = i / float(nx), j / float(ny)
            uv.data[li].uv = (uu, vv)
            if puv is not None:
                puv.data[li].uv = (ru0 + uu * (ru1 - ru0), rv0 + vv * (rv1 - rv0))
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    if thickness > 0:
        sol = obj.modifiers.new("Solidify", "SOLIDIFY")
        sol.thickness = thickness
        sol.offset = -1.0
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=sol.name)
    C.set_smooth_by_angle(obj, 55.0)
    return set_kind(obj, kind, grain)


def rounded_rect(w, d, r, steps=5):
    pts = []
    hw, hd = w / 2 - r, d / 2 - r
    for cx, cy, a0 in ((hw, hd, 0), (-hw, hd, 90), (-hw, -hd, 180), (hw, -hd, 270)):
        for s in range(steps + 1):
            a = math.radians(a0 + 90 * s / steps)
            pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return pts


def join(objs, name):
    return C.join_objects([o for o in objs if o is not None], name)


def tri_count(obj):
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)


# --------------------------------------------------------------------------------------
# Procedural PBR Shader Graph (AGE=0 for B 2005, AGE=1 for R 2012)
# --------------------------------------------------------------------------------------

def _S(node, name, outputs=False):
    sockets = node.outputs if outputs else node.inputs
    for s in sockets:
        if s.name == name and s.enabled:
            return s
    for s in sockets:
        if s.name == name:
            return s
    raise KeyError(f"{node.bl_idname}.{name}")


class NB:
    def __init__(self, nt):
        self.nt = nt
        self.nodes = nt.nodes
        self.col = 0

    def node(self, bl, **props):
        n = self.nodes.new(bl)
        self.col += 1
        n.location = (-1600 + (self.col % 14) * 190, -((self.col // 14) * 260))
        for k, v in props.items():
            setattr(n, k, v)
        return n

    def feed(self, node, name, val):
        s = _S(node, name)
        if isinstance(val, bpy.types.NodeSocket):
            self.nt.links.new(val, s)
        elif val is not None:
            try:
                s.default_value = val
            except TypeError:
                s.default_value = (val, val, val) if len(s.default_value) == 3 else (val, val, val, 1.0)

    def value(self, v, label=None):
        n = self.node("ShaderNodeValue")
        n.outputs[0].default_value = v
        if label:
            n.label = label
            n.name = label
        return n.outputs[0]

    def math(self, op, a, b=None, c=None, clamp=False):
        n = self.node("ShaderNodeMath", operation=op, use_clamp=clamp)
        for i, v in enumerate((a, b, c)):
            if v is None:
                continue
            if isinstance(v, bpy.types.NodeSocket):
                self.nt.links.new(v, n.inputs[i])
            else:
                n.inputs[i].default_value = v
        return n.outputs[0]

    def vmath(self, op, a, b=None, c=None, out="Vector"):
        n = self.node("ShaderNodeVectorMath", operation=op)
        for i, v in enumerate((a, b, c)):
            if v is None:
                continue
            if isinstance(v, bpy.types.NodeSocket):
                self.nt.links.new(v, n.inputs[i])
            else:
                n.inputs[i].default_value = v if hasattr(v, "__len__") else (v, v, v)
        return _S(n, out, outputs=True)

    def mix(self, a, b, fac, blend="MIX"):
        n = self.node("ShaderNodeMix", data_type="RGBA", blend_type=blend)
        self.feed(n, "Factor", fac)
        for nm, v in (("A", a), ("B", b)):
            s = [x for x in n.inputs if x.name == nm and x.enabled][0]
            if isinstance(v, bpy.types.NodeSocket):
                self.nt.links.new(v, s)
            else:
                s.default_value = (*v[:3], 1.0) if hasattr(v, "__len__") else (v, v, v, 1.0)
        return [o for o in n.outputs if o.name == "Result" and o.enabled][0]

    def mixf(self, a, b, fac):
        return self.math("ADD", self.math("MULTIPLY", a, self.math("SUBTRACT", 1.0, fac)),
                         self.math("MULTIPLY", b, fac))

    def mapr(self, v, a0, a1, b0=0.0, b1=1.0, clamp=True):
        n = self.node("ShaderNodeMapRange", clamp=clamp)
        for nm, val in (("Value", v), ("From Min", a0), ("From Max", a1), ("To Min", b0), ("To Max", b1)):
            self.feed(n, nm, val)
        return n.outputs[0]

    def ramp(self, fac, stops):
        n = self.node("ShaderNodeValToRGB")
        cr = n.color_ramp
        while len(cr.elements) > len(stops):
            cr.elements.remove(cr.elements[-1])
        while len(cr.elements) < len(stops):
            cr.elements.new(0.5)
        for el, (pos, col) in zip(cr.elements, stops):
            el.position = pos
            el.color = (*col[:3], 1.0)
        self.nt.links.new(fac, n.inputs["Fac"])
        return n.outputs["Color"]

    def noise(self, vec, scale, detail=2.0, rough=0.5, distortion=0.0):
        n = self.node("ShaderNodeTexNoise", noise_dimensions="3D")
        self.feed(n, "Vector", vec)
        self.feed(n, "Scale", scale)
        self.feed(n, "Detail", detail)
        self.feed(n, "Roughness", rough)
        self.feed(n, "Distortion", distortion)
        return n.outputs["Fac"]

    def voronoi(self, vec, scale, feature="F1", rand=1.0, out="Distance"):
        n = self.node("ShaderNodeTexVoronoi", voronoi_dimensions="3D", feature=feature)
        self.feed(n, "Vector", vec)
        self.feed(n, "Scale", scale)
        self.feed(n, "Randomness", rand)
        return _S(n, out, outputs=True)

    def wave(self, vec, scale, kind="BANDS", direction="X", distortion=0.0, detail=2.0, profile="SIN"):
        n = self.node("ShaderNodeTexWave", wave_type=kind, bands_direction=direction, rings_direction="X",
                      wave_profile=profile)
        self.feed(n, "Vector", vec)
        self.feed(n, "Scale", scale)
        self.feed(n, "Distortion", distortion)
        self.feed(n, "Detail", detail)
        return n.outputs["Fac"]

    def sep(self, vec):
        n = self.node("ShaderNodeSeparateXYZ")
        self.nt.links.new(vec, n.inputs["Vector"])
        return n.outputs["X"], n.outputs["Y"], n.outputs["Z"]

    def comb(self, x, y, z):
        n = self.node("ShaderNodeCombineXYZ")
        for nm, v in (("X", x), ("Y", y), ("Z", z)):
            self.feed(n, nm, v)
        return n.outputs["Vector"]


def _common_inputs(b):
    tc = b.node("ShaderNodeTexCoord")
    P = tc.outputs["Object"]
    attr = b.node("ShaderNodeAttribute", attribute_name="grain", attribute_type="GEOMETRY")
    grain_vec = attr.outputs["Color"]
    rnd = attr.outputs["Alpha"]
    geo = b.node("ShaderNodeNewGeometry")
    bev = b.node("ShaderNodeBevel", samples=4)
    b.feed(bev, "Radius", 0.008)
    dotn = b.vmath("DOT_PRODUCT", geo.outputs["Normal"], bev.outputs["Normal"], out="Value")
    edge = b.mapr(b.math("SUBTRACT", 1.0, dotn), 0.0015, 0.045, 0.0, 1.0)
    ao = b.node("ShaderNodeAmbientOcclusion", samples=4, inside=False, only_local=False)
    b.feed(ao, "Distance", 0.065)
    cav = b.math("SUBTRACT", 1.0, ao.outputs["AO"])
    return P, grain_vec, rnd, edge, cav


def _stretch(b, P, grain_vec, rnd, stretch=0.045, spread=7.3):
    s = b.vmath("SUBTRACT", (1, 1, 1), b.vmath("SCALE", grain_vec, 1.0 - stretch, out="Vector"))
    q = b.vmath("MULTIPLY", P, s)
    off = b.vmath("SCALE", b.comb(1.0, 0.7, 0.4), b.math("MULTIPLY", rnd, spread), out="Vector")
    return b.vmath("ADD", q, off)


def _rough_noise(b, P, scale=9.0, amp=0.10):
    return b.math("MULTIPLY", b.math("SUBTRACT", b.noise(P, scale, 4, 0.55), 0.5), amp)


def srgb(r, g, b):
    """Convert sRGB [0..1] color to Cycles Linear RGB so baked albedos match Unity URP Linear palette exactly."""
    def f(c):
        return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
    return (f(r), f(g), f(b))


def _k_paint(b, A, P, gv, rnd, edge, cav, col_b, col_r, rough_b, rough_r, primer=None,
             rust=None, metal_sheet=False):
    primer = primer or srgb(0.42, 0.36, 0.27)
    rust = rust or srgb(0.32, 0.14, 0.07)
    z = b.sep(P)[2]
    blotch = b.math("SUBTRACT", b.noise(P, 2.8, 3, 0.6), 0.5)
    base = b.mix(col_b, col_r, A)
    base = b.mix(base, b.vmath("SCALE", base, 0.70, out="Vector"), b.math("MULTIPLY", b.mapr(blotch, -0.25, 0.25), 0.55))
    low = b.mapr(z, 0.42, 0.02)
    grime_amt = b.math("ADD", b.math("MULTIPLY", low, b.math("MULTIPLY", A, 0.65)),
                       b.math("MULTIPLY", cav, b.math("ADD", 0.22, b.math("MULTIPLY", A, 0.42))))
    base = b.mix(base, srgb(0.06, 0.07, 0.06), b.math("MULTIPLY", grime_amt, 0.78, clamp=True))
    chip = b.mapr(b.noise(P, 36.0, 5, 0.6), 0.44, 0.60)
    chip_mask = b.math("MULTIPLY", b.math("MULTIPLY", edge, chip),
                       b.math("ADD", 0.16, b.math("MULTIPLY", A, 0.72)), clamp=True)
    flakes = b.mapr(b.noise(P, 13.0, 4, 0.6), 0.74, 0.82)
    flake_mask = b.math("MULTIPLY", b.math("MULTIPLY", flakes, A), b.mapr(cav, 0.0, 0.6, 0.15, 0.85))
    under = b.mix(primer, rust, b.mapr(b.noise(P, 20.0, 3), 0.35, 0.65)) if metal_sheet else primer
    color = b.mix(base, under, b.math("MAXIMUM", chip_mask, flake_mask, clamp=True))
    rough = b.mixf(rough_b, rough_r, A)
    rough = b.math("ADD", rough, _rough_noise(b, P, 10.0, 0.14))
    rough = b.mixf(rough, 0.84, b.math("MAXIMUM", chip_mask, flake_mask))
    peel = b.noise(P, 320.0, 2, 0.5)
    height = b.math("SUBTRACT", b.math("MULTIPLY", peel, 0.22),
                    b.math("MULTIPLY", b.math("MAXIMUM", chip_mask, flake_mask), 0.85))
    return color, rough, 0.0, height, 0.28


def _k_wood(b, A, P, gv, rnd, edge, cav, light=False):
    Q = _stretch(b, P, gv, rnd, stretch=0.05)
    rings = b.wave(Q, 2.2, "RINGS", distortion=5.5, detail=3.0)
    rings = b.math("ADD", rings, b.math("MULTIPLY", b.noise(Q, 4.5, 3), 0.30))
    if light:
        # Warm varnished Vietnamese teak / mahogany trim (calibrated for URP ACES Filmic!)
        stops = [(0.0, srgb(0.34, 0.200, 0.105)), (0.5, srgb(0.47, 0.285, 0.155)), (1.0, srgb(0.28, 0.162, 0.084))]
    else:
        # Rich varnished Vietnamese ironwood (gỗ lim / căm xe) readable under both tungsten B & moonlight R!
        stops = [(0.0, srgb(0.235, 0.125, 0.064)), (0.5, srgb(0.355, 0.198, 0.104)), (1.0, srgb(0.188, 0.098, 0.050))]
    ramp = b.ramp(b.math("MAXIMUM", b.math("MINIMUM", rings, 1.0), 0.0), stops)
    faded = b.mix(ramp, srgb(0.30, 0.245, 0.195), 0.34)
    base = b.mix(ramp, faded, A)
    fine = b.noise(Q, 150.0, 2, 0.5)
    base = b.mix(base, b.vmath("SCALE", base, 0.76, out="Vector"), b.math("MULTIPLY", b.mapr(fine, 0.35, 0.65), 0.40))
    raw = srgb(0.50, 0.36, 0.22)
    wear = b.math("MULTIPLY", b.math("MULTIPLY", edge, b.mapr(b.noise(P, 28.0, 4), 0.38, 0.62)),
                  b.math("ADD", 0.22, b.math("MULTIPLY", A, 0.58)), clamp=True)
    base = b.mix(base, raw, wear)
    stain = b.mapr(b.noise(P, 2.3, 4, 0.62, 0.5), 0.54, 0.72)
    base = b.mix(base, b.vmath("SCALE", base, 0.52, out="Vector"), b.math("MULTIPLY", stain, b.math("MULTIPLY", A, 0.72)))
    base = b.mix(base, srgb(0.08, 0.055, 0.038), b.math("MULTIPLY", cav, b.math("ADD", 0.18, b.math("MULTIPLY", A, 0.36))))
    rough = b.mixf(0.28, 0.58, A)
    rough = b.mixf(rough, 0.74, wear)
    rough = b.math("ADD", rough, _rough_noise(b, P, 7.0, 0.14))
    height = b.math("SUBTRACT", b.math("MULTIPLY", b.noise(Q, 220.0, 1, 0.5), 0.42), b.math("MULTIPLY", wear, 0.45))
    return base, rough, 0.0, height, 0.26


def _k_terrazzo(b, A, P, gv, rnd, edge, cav, dark=False):
    cell = b.voronoi(P, 88.0, "F1", 1.0, "Distance")
    cid = b.voronoi(P, 88.0, "F1", 1.0, "Color")
    chip = b.mapr(cell, 0.42, 0.29)
    if dark:
        stops = [(0.0, srgb(0.14, 0.15, 0.14)), (0.35, srgb(0.32, 0.34, 0.31)), (0.70, srgb(0.54, 0.53, 0.48)), (1.0, srgb(0.22, 0.26, 0.23))]
        cement = b.mix(srgb(0.26, 0.28, 0.26), srgb(0.18, 0.195, 0.185), A)
    else:
        # Authentic 1987 Vietnamese honed warm-grey / cream terrazzo counter slab (đá mài granito)
        stops = [(0.0, srgb(0.24, 0.25, 0.23)), (0.30, srgb(0.58, 0.55, 0.48)), (0.62, srgb(0.76, 0.74, 0.67)),
                 (0.82, srgb(0.38, 0.44, 0.37)), (1.0, srgb(0.54, 0.44, 0.38))]
        cement = b.mix(srgb(0.68, 0.66, 0.60), srgb(0.52, 0.50, 0.44), A)
    chip_col = b.ramp(b.math("MAXIMUM", b.math("MINIMUM", b.vmath("DOT_PRODUCT", cid, (0.5, 0.33, 0.17), out="Value"), 1.0), 0.0), stops)
    cement = b.mix(cement, b.vmath("SCALE", cement, 0.80, out="Vector"), b.mapr(b.noise(P, 7.0, 4), 0.35, 0.65))
    base = b.mix(cement, chip_col, chip)
    stain = b.mapr(b.noise(P, 2.8, 4, 0.6, 0.4), 0.52, 0.72)
    base = b.mix(base, b.vmath("SCALE", base, 0.52, out="Vector"), b.math("MULTIPLY", stain, b.math("MULTIPLY", A, 0.82)))
    base = b.mix(base, srgb(0.08, 0.075, 0.065), b.math("MULTIPLY", cav, b.math("ADD", 0.24, b.math("MULTIPLY", A, 0.42))))
    cracks = b.mapr(b.voronoi(P, 4.5, "DISTANCE_TO_EDGE", 1.0, "Distance"), 0.011, 0.0)
    cracks = b.math("MULTIPLY", cracks, b.math("MULTIPLY", b.mapr(b.noise(P, 2.8, 2), 0.52, 0.70), A))
    base = b.mix(base, srgb(0.06, 0.06, 0.055), b.math("MULTIPLY", cracks, 0.88))
    rough = b.mixf(0.28, 0.52, A)
    rough = b.math("ADD", rough, _rough_noise(b, P, 6.5, 0.16))
    height = b.math("SUBTRACT", b.math("MULTIPLY", chip, 0.14), b.math("MULTIPLY", cracks, 0.90))
    return base, rough, 0.0, height, 0.26


def _k_steel(b, A, P, gv, rnd, edge, cav, tone=None):
    tone = tone or srgb(0.76, 0.78, 0.77)
    Q = _stretch(b, P, gv, rnd, stretch=0.025)
    streak = b.noise(Q, 160.0, 2, 0.55)
    aged_tone = (tone[0] * 0.62, tone[1] * 0.63, tone[2] * 0.60)
    base = b.mix(tone, aged_tone, b.math("MULTIPLY", A, 0.60))
    base = b.mix(base, b.vmath("SCALE", base, 0.80, out="Vector"), b.math("MULTIPLY", b.mapr(streak, 0.4, 0.65), 0.42))
    rust_mask = b.math("MULTIPLY", b.math("MAXIMUM", b.math("MULTIPLY", cav, 0.85), b.math("MULTIPLY", edge, 0.55)),
                       b.math("MULTIPLY", b.mapr(b.noise(P, 18.0, 4), 0.46, 0.66), A), clamp=True)
    base = b.mix(base, srgb(0.38, 0.18, 0.09), b.math("MULTIPLY", rust_mask, 0.86))
    rough = b.mixf(0.28, 0.50, A)
    rough = b.math("ADD", rough, b.math("MULTIPLY", b.math("SUBTRACT", streak, 0.5), 0.20))
    rough = b.mixf(rough, 0.84, rust_mask)
    metal = b.mixf(0.80, 0.25, rust_mask)
    return base, rough, metal, b.math("MULTIPLY", streak, 0.22), 0.12


def _k_brass(b, A, P, gv, rnd, edge, cav):
    base = b.mix(srgb(0.74, 0.59, 0.28), srgb(0.42, 0.32, 0.16), b.math("MULTIPLY", A, 0.78))
    patina = b.math("MULTIPLY", b.mapr(cav, 0.0, 0.8), b.math("MULTIPLY", b.mapr(b.noise(P, 20.0, 4), 0.42, 0.65), A))
    base = b.mix(base, srgb(0.22, 0.36, 0.28), b.math("MULTIPLY", patina, 0.62))
    wear = b.math("MULTIPLY", edge, b.math("ADD", 0.28, b.math("MULTIPLY", A, 0.22)), clamp=True)
    base = b.mix(base, srgb(0.84, 0.70, 0.38), b.math("MULTIPLY", wear, 0.60))
    rough = b.math("ADD", b.mixf(0.28, 0.48, A), _rough_noise(b, P, 28.0, 0.14))
    return base, rough, 0.82, b.math("MULTIPLY", b.noise(P, 200.0, 2), 0.18), 0.10


def _k_plastic(b, A, P, gv, rnd, edge, cav, col_b, col_r, rough_b=0.34, rough_r=0.52):
    base = b.mix(col_b, col_r, A)
    base = b.mix(base, b.vmath("SCALE", base, 0.80, out="Vector"), b.math("MULTIPLY", b.mapr(b.noise(P, 4.5, 3), 0.35, 0.65), 0.42))
    base = b.mix(base, srgb(0.08, 0.075, 0.065), b.math("MULTIPLY", cav, b.math("ADD", 0.28, b.math("MULTIPLY", A, 0.42))))
    rough = b.math("ADD", b.mixf(rough_b, rough_r, A), _rough_noise(b, P, 35.0, 0.11))
    return base, rough, 0.0, b.math("MULTIPLY", b.noise(P, 420.0, 1), 0.14), 0.12


def _k_rubber(b, A, P, gv, rnd, edge, cav):
    base = b.mix(srgb(0.09, 0.095, 0.092), srgb(0.13, 0.13, 0.12), A)
    rough = b.math("ADD", 0.72, _rough_noise(b, P, 55.0, 0.12))
    return base, rough, 0.0, b.math("MULTIPLY", b.noise(P, 280.0, 2), 0.25), 0.22


def _k_vinyl(b, A, P, gv, rnd, edge, cav, col_b=None, col_r=None):
    col_b = col_b or srgb(0.14, 0.28, 0.21)
    col_r = col_r or srgb(0.13, 0.20, 0.16)
    grain = b.voronoi(P, 360.0, "F1", 1.0, "Distance")
    base = b.mix(col_b, col_r, A)
    base = b.mix(base, b.vmath("SCALE", base, 0.68, out="Vector"), b.math("MULTIPLY", b.mapr(grain, 0.2, 0.6), 0.32))
    wear = b.math("MULTIPLY", edge, b.math("ADD", 0.15, b.math("MULTIPLY", A, 0.75)), clamp=True)
    base = b.mix(base, srgb(0.28, 0.32, 0.26), b.math("MULTIPLY", wear, 0.48))
    rough = b.mixf(0.42, 0.58, A)
    rough = b.math("ADD", rough, _rough_noise(b, P, 14.0, 0.12))
    return base, rough, 0.0, b.math("MULTIPLY", grain, 0.7), 0.26


def _k_cloth(b, A, P, gv, rnd, edge, cav, col_b, col_r):
    w1 = b.wave(P, 240.0, "BANDS", "X", profile="SIN")
    w2 = b.wave(P, 240.0, "BANDS", "Y", profile="SIN")
    weave = b.math("ADD", w1, w2)
    base = b.mix(col_b, col_r, A)
    base = b.mix(base, b.vmath("SCALE", base, 0.74, out="Vector"), b.math("MULTIPLY", b.mapr(weave, 0.5, 1.5), 0.30))
    worn = b.math("MULTIPLY", edge, b.math("ADD", 0.25, b.math("MULTIPLY", A, 0.65)), clamp=True)
    base = b.mix(base, srgb(0.42, 0.36, 0.30), b.math("MULTIPLY", worn, 0.52))
    base = b.mix(base, srgb(0.06, 0.055, 0.05), b.math("MULTIPLY", cav, 0.45))
    return base, 0.86, 0.0, b.math("MULTIPLY", weave, 0.35), 0.22


def _k_paper(b, A, P, gv, rnd, edge, cav, col_b=None, col_r=None):
    col_b = col_b or srgb(0.84, 0.80, 0.69)
    col_r = col_r or srgb(0.64, 0.56, 0.42)
    base = b.mix(col_b, col_r, A)
    fibre = b.noise(P, 260.0, 3, 0.55)
    base = b.mix(base, b.vmath("SCALE", base, 0.86, out="Vector"), b.math("MULTIPLY", b.mapr(fibre, 0.3, 0.7), 0.35))
    stain = b.mapr(b.noise(P, 5.0, 4, 0.6, 0.4), 0.54, 0.72)
    base = b.mix(base, b.vmath("SCALE", base, 0.55, out="Vector"), b.math("MULTIPLY", stain, b.math("MULTIPLY", A, 0.78)))
    base = b.mix(base, srgb(0.10, 0.09, 0.075), b.math("MULTIPLY", cav, 0.30))
    return base, b.math("ADD", 0.84, _rough_noise(b, P, 100.0, 0.08)), 0.0, b.math("MULTIPLY", fibre, 0.45), 0.08


def _k_ceramic(b, A, P, gv, rnd, edge, cav, col_b=None, col_r=None):
    col_b = col_b or srgb(0.86, 0.85, 0.79)
    col_r = col_r or srgb(0.68, 0.66, 0.58)
    base = b.mix(col_b, col_r, A)
    base = b.mix(base, srgb(0.10, 0.095, 0.085), b.math("MULTIPLY", cav, b.math("ADD", 0.22, b.math("MULTIPLY", A, 0.35))))
    rough = b.math("ADD", b.mixf(0.14, 0.36, A), _rough_noise(b, P, 28.0, 0.09))
    return base, rough, 0.0, b.math("MULTIPLY", b.noise(P, 180.0, 2), 0.10), 0.08


def _k_snake_leaf(b, A, P, gv, rnd, edge, cav):
    bands = b.wave(P, 18.0, "BANDS", "Z", distortion=4.5, detail=3.0)
    col_b = b.mix(srgb(0.12, 0.28, 0.13), srgb(0.36, 0.48, 0.22), b.mapr(bands, 0.32, 0.68))
    col_r = b.mix(srgb(0.15, 0.22, 0.12), srgb(0.34, 0.33, 0.17), b.mapr(bands, 0.32, 0.68))
    base = b.mix(col_b, col_r, A)
    rough = b.mixf(0.32, 0.56, A)
    return base, rough, 0.0, b.math("MULTIPLY", bands, 0.25), 0.15


def _k_enamel_lamp(b, A, P, gv, rnd, edge, cav):
    return _k_paint(b, A, P, gv, rnd, edge, cav, srgb(0.12, 0.34, 0.23), srgb(0.10, 0.23, 0.16), 0.18, 0.44, metal_sheet=True)


def _k_thermos_red(b, A, P, gv, rnd, edge, cav):
    return _k_paint(b, A, P, gv, rnd, edge, cav, srgb(0.58, 0.14, 0.12), srgb(0.40, 0.12, 0.10), 0.22, 0.48, metal_sheet=True)


def _k_soil(b, A, P, gv, rnd, edge, cav):
    n = b.noise(P, 42.0, 5, 0.65)
    base = b.mix(srgb(0.16, 0.12, 0.085), srgb(0.24, 0.19, 0.14), b.mapr(n, 0.3, 0.7))
    return base, 0.92, 0.0, n, 0.55


def _k_plaster(b, A, P, gv, rnd, edge, cav):
    return _k_paint(b, A, P, gv, rnd, edge, cav, srgb(0.70, 0.72, 0.64), srgb(0.48, 0.51, 0.46), 0.78, 0.90, primer=srgb(0.48, 0.46, 0.41))


def _k_perf_steel(b, A, P, gv, rnd, edge, cav):
    cell = b.voronoi(b.vmath("SCALE", P, 1.0, out="Vector"), 36.0, "F1", 0.0, "Distance")
    hole = b.mapr(cell, 0.24, 0.28, 1.0, 0.0)
    # Authentic Vietnamese hospital brushed satin silver-steel waiting bench (reads clearly in dim lobbies!)
    base, rough, _, h0, s0 = _k_steel(b, A, P, gv, rnd, edge, cav, tone=srgb(0.84, 0.86, 0.85))
    base = b.mix(base, srgb(0.08, 0.09, 0.088), b.math("MULTIPLY", hole, 0.92))
    rough = b.mixf(b.mixf(0.36, 0.56, A), 0.90, hole)
    metal = b.mixf(0.46, 0.08, hole)
    height = b.math("SUBTRACT", h0, b.math("MULTIPLY", hole, 1.0))
    return base, rough, metal, height, 0.55


KINDS = {
    # Authentic 1987 Vietnamese hospital bottle-teal oil-painted dado (matches DadoB / DadoR!)
    "paint_green":   lambda b, A, *a: _k_paint(b, A, *a, srgb(0.170, 0.345, 0.275), srgb(0.120, 0.232, 0.192), 0.34, 0.58),
    "paint_cream":   lambda b, A, *a: _k_paint(b, A, *a, srgb(0.74, 0.74, 0.65), srgb(0.52, 0.52, 0.45), 0.56, 0.78),
    "paint_grey":    lambda b, A, *a: _k_paint(b, A, *a, srgb(0.40, 0.46, 0.43), srgb(0.26, 0.30, 0.28), 0.34, 0.58, metal_sheet=True),
    "paint_olive":   lambda b, A, *a: _k_paint(b, A, *a, srgb(0.24, 0.31, 0.24), srgb(0.17, 0.22, 0.18), 0.34, 0.58, metal_sheet=True),
    "enamel_lamp":   _k_enamel_lamp,
    "enamel_white":  lambda b, A, *a: _k_ceramic(b, A, *a, srgb(0.86, 0.85, 0.80), srgb(0.68, 0.67, 0.60)),
    "enamel_blue":   lambda b, A, *a: _k_ceramic(b, A, *a, srgb(0.14, 0.28, 0.50), srgb(0.12, 0.22, 0.38)),
    "thermos_red":   _k_thermos_red,
    "wood":          lambda b, A, *a: _k_wood(b, A, *a, light=False),
    "wood_light":    lambda b, A, *a: _k_wood(b, A, *a, light=True),
    "terrazzo":      lambda b, A, *a: _k_terrazzo(b, A, *a, dark=False),
    "terrazzo_dark": lambda b, A, *a: _k_terrazzo(b, A, *a, dark=True),
    "steel":         _k_steel,
    "steel_dark":    lambda b, A, *a: _k_steel(b, A, *a, tone=srgb(0.32, 0.34, 0.34)),
    "perf_steel":    _k_perf_steel,
    "brass":         _k_brass,
    "rubber":        _k_rubber,
    "plastic_ivory": lambda b, A, *a: _k_plastic(b, A, *a, srgb(0.76, 0.72, 0.59), srgb(0.58, 0.51, 0.36)),
    "plastic_black": lambda b, A, *a: _k_plastic(b, A, *a, srgb(0.10, 0.105, 0.105), srgb(0.14, 0.14, 0.13), 0.30, 0.48),
    "plastic_red":   lambda b, A, *a: _k_plastic(b, A, *a, srgb(0.62, 0.15, 0.13), srgb(0.44, 0.13, 0.11), 0.28, 0.48),
    "vinyl_green":   _k_vinyl,
    "cloth_maroon":  lambda b, A, *a: _k_cloth(b, A, *a, srgb(0.32, 0.10, 0.095), srgb(0.24, 0.12, 0.105)),
    "cloth_navy":    lambda b, A, *a: _k_cloth(b, A, *a, srgb(0.13, 0.18, 0.28), srgb(0.13, 0.16, 0.22)),
    "cloth_olive":   lambda b, A, *a: _k_cloth(b, A, *a, srgb(0.22, 0.29, 0.21), srgb(0.18, 0.22, 0.17)),
    "paper":         _k_paper,
    "kraft":         lambda b, A, *a: _k_paper(b, A, *a, srgb(0.58, 0.44, 0.27), srgb(0.42, 0.31, 0.19)),
    "ceramic":       _k_ceramic,
    "ceramic_pot":   lambda b, A, *a: _k_ceramic(b, A, *a, srgb(0.28, 0.40, 0.35), srgb(0.20, 0.28, 0.25)),
    "snake_leaf":    _k_snake_leaf,
    "soil":          _k_soil,
    "plaster":       _k_plaster,
    "tea_liquid":    lambda b, A, *a: (srgb(0.22, 0.18, 0.06), 0.08, 0.0, 0.0, 0.0),
    "bulb_warm":     lambda b, A, *a: (srgb(1.0, 0.90, 0.68), 0.12, 0.0, 0.0, 0.0),
}


def get_mat(kind: str):
    if kind == "Glass":
        return C.get_material("Glass")
    if kind == "FrostedGlass":
        return C.get_material("FrostedGlass")
    if kind == "DeadTube":
        return C.get_material("DeadTube")
    if kind == "LiveTube":
        return C.get_material("LiveTube")
    if kind == "WarmGlow":
        return C.get_material("WarmGlow")

    name = f"H_{kind}"
    mat = bpy.data.materials.get(name)
    if mat is not None:
        return mat
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.name = "BSDF"
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    b = NB(nt)

    if kind.startswith("print:"):
        return _print_material(mat, nt, bsdf, b, kind.split(":", 1)[1])

    A = b.value(0.0, "AGE")
    P, gv, rnd, edge, cav = _common_inputs(b)
    color, rough, metal, height, strength = KINDS[kind](b, A, P, gv, rnd, edge, cav)
    if isinstance(color, bpy.types.NodeSocket):
        nt.links.new(color, bsdf.inputs["Base Color"])
    else:
        bsdf.inputs["Base Color"].default_value = (*color[:3], 1.0)
    if isinstance(rough, bpy.types.NodeSocket):
        nt.links.new(b.math("MAXIMUM", b.math("MINIMUM", rough, 1.0), 0.04), bsdf.inputs["Roughness"])
    else:
        bsdf.inputs["Roughness"].default_value = rough
    if isinstance(metal, bpy.types.NodeSocket):
        nt.links.new(metal, bsdf.inputs["Metallic"])
    else:
        bsdf.inputs["Metallic"].default_value = metal
    if kind == "bulb_warm":
        bsdf.inputs["Emission Color"].default_value = (1.0, 0.84, 0.52, 1.0)
        bsdf.inputs["Emission Strength"].default_value = 4.0
    if isinstance(height, bpy.types.NodeSocket) and strength > 0.0:
        bump = b.node("ShaderNodeBump")
        bump.inputs["Strength"].default_value = strength
        bump.inputs["Distance"].default_value = 0.008
        nt.links.new(height, bump.inputs["Height"])
        nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    return mat


def _print_material(mat, nt, bsdf, b, sheet: str):
    A = b.value(0.0, "AGE")
    P, gv, rnd, edge, cav = _common_inputs(b)
    path = PRINT_DIR / f"{sheet}.png"
    tex = b.node("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(str(path), check_existing=True)
    tex.image.colorspace_settings.name = "sRGB"
    uv = b.node("ShaderNodeUVMap", uv_map="PRINT")
    nt.links.new(uv.outputs["UV"], tex.inputs["Vector"])
    # Slightly darken & yellow printed surfaces in R (AGE=1)
    aged = b.mix(tex.outputs["Color"], (0.68, 0.62, 0.48), b.math("MULTIPLY", A, 0.36), blend="MULTIPLY")
    # Cavity shadow darkening
    aged = b.mix(aged, (0.06, 0.055, 0.045), b.math("MULTIPLY", cav, 0.25))
    nt.links.new(aged, bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.78 if "battery" not in sheet else 0.32
    fibre = b.noise(P, 260.0, 2, 0.5)
    bump = b.node("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.05
    bump.inputs["Distance"].default_value = 0.004
    nt.links.new(fibre, bump.inputs["Height"])
    nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    return mat


def set_age(age: float):
    for m in bpy.data.materials:
        if m.use_nodes and "AGE" in m.node_tree.nodes:
            m.node_tree.nodes["AGE"].outputs[0].default_value = float(age)


# --------------------------------------------------------------------------------------
# UV Atlas Unwrapping + Multi-Pass Cycles PBR Baker -> Unity URP Maps
# --------------------------------------------------------------------------------------

def _configure_cycles_bake(samples=8):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = samples
    scene.cycles.use_adaptive_sampling = False
    scene.cycles.use_denoising = False
    scene.cycles.max_bounces = 2
    scene.cycles.diffuse_bounces = 1
    scene.cycles.glossy_bounces = 0
    scene.cycles.transmission_bounces = 0
    scene.cycles.volume_bounces = 0
    scene.cycles.transparent_max_bounces = 2
    scene.render.bake.margin = 10
    scene.render.bake.margin_type = "EXTEND"
    scene.render.bake.use_clear = True
    scene.render.bake.use_pass_direct = False
    scene.render.bake.use_pass_indirect = False
    scene.render.bake.use_pass_color = True


def _ensure_bake_uv(objs, angle_limit_deg=55.0, island_margin=0.005):
    """Create a clean non-overlapping UVMap across all provided objects (preserving PRINT UV!)."""
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objs:
        me = obj.data
        if "UVMap" not in me.uv_layers:
            me.uv_layers.new(name="UVMap")
        me.uv_layers.active = me.uv_layers["UVMap"]
        me.uv_layers["UVMap"].active_render = True
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(
        angle_limit=math.radians(angle_limit_deg),
        island_margin=island_margin,
        area_weight=0.0,
        correct_aspect=True,
        scale_to_bounds=False,
    )
    bpy.ops.uv.pack_islands(margin=island_margin, rotate=True)
    bpy.ops.object.mode_set(mode="OBJECT")
    for obj in objs:
        obj.data.uv_layers.active = obj.data.uv_layers["UVMap"]


def _attach_bake_image(mats, img):
    created = []
    for m in mats:
        if not m.use_nodes:
            continue
        nt = m.node_tree
        node = nt.nodes.new("ShaderNodeTexImage")
        node.name = "__BAKE_TARGET__"
        node.image = img
        for n in nt.nodes:
            n.select = False
        node.select = True
        nt.nodes.active = node
        created.append((nt, node))
    return created


def _detach_bake_image(created):
    for nt, node in created:
        try:
            nt.nodes.remove(node)
        except Exception:
            pass


def _image_to_np(img) -> np.ndarray:
    w, h = img.size
    arr = np.empty(w * h * 4, dtype=np.float32)
    img.pixels.foreach_get(arr)
    arr = arr.reshape((h, w, 4))
    return np.flipud(arr)  # Blender bottom-up -> PIL top-down


def _linear_to_srgb(x: np.ndarray) -> np.ndarray:
    x = np.clip(x, 0.0, 1.0)
    return np.where(x <= 0.0031308, x * 12.92, 1.055 * np.power(x, 1.0 / 2.4) - 0.055)


def _save_np_png(arr_topdown: np.ndarray, path: Path, is_srgb=False):
    """Save HxWx(3 or 4) float32 [0..1] top-down array to 8-bit PNG using Blender native image I/O."""
    h, w = arr_topdown.shape[:2]
    if arr_topdown.shape[2] == 3:
        rgba = np.concatenate([arr_topdown, np.ones((h, w, 1), dtype=np.float32)], axis=-1)
        alpha = False
    else:
        rgba = arr_topdown
        alpha = True
    # Flip back to Blender bottom-up layout
    flipped = np.flipud(np.clip(rgba, 0.0, 1.0)).astype(np.float32)
    out_img = bpy.data.images.new("__save_tmp", width=w, height=h, alpha=alpha, float_buffer=False)
    out_img.colorspace_settings.name = "sRGB" if is_srgb else "Non-Color"
    out_img.pixels.foreach_set(flipped.ravel())
    out_img.filepath_raw = str(path)
    out_img.file_format = "PNG"
    # Save via Image.save() — note Blender writes sRGB directly when scene view is Standard or raw Non-Color!
    scene = bpy.context.scene
    prev_vt = scene.view_settings.view_transform
    prev_look = scene.view_settings.look
    prev_cm = scene.render.image_settings.color_mode
    prev_cd = scene.render.image_settings.color_depth
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    scene.render.image_settings.color_mode = "RGBA" if alpha else "RGB"
    scene.render.image_settings.color_depth = "8"
    out_img.save_render(str(path), scene=scene) if is_srgb else out_img.save()
    scene.view_settings.view_transform = prev_vt
    scene.view_settings.look = prev_look
    scene.render.image_settings.color_mode = prev_cm
    scene.render.image_settings.color_depth = prev_cd
    bpy.data.images.remove(out_img)


def _save_rgb_png(arr: np.ndarray, path: Path, srgb=True):
    # Blender's bake output is linear; out_img.save() writes float pixels directly to sRGB PNG when colorspace='sRGB'
    # Wait: using explicit linear->sRGB + Non-Color save() guarantees 100% exact bit values with zero view-transform drift!
    rgb = _linear_to_srgb(arr[..., :3]) if srgb else np.clip(arr[..., :3], 0.0, 1.0)
    h, w = rgb.shape[:2]
    rgba = np.concatenate([rgb, np.ones((h, w, 1), dtype=np.float32)], axis=-1)
    flipped = np.flipud(np.clip(rgba, 0.0, 1.0)).astype(np.float32)
    out_img = bpy.data.images.new("__save_rgb", width=w, height=h, alpha=False, float_buffer=False)
    out_img.colorspace_settings.name = "Non-Color"
    out_img.pixels.foreach_set(flipped.ravel())
    out_img.filepath_raw = str(path)
    out_img.file_format = "PNG"
    out_img.save()
    bpy.data.images.remove(out_img)


def _save_rgba_png(r, g, b, a, path: Path):
    rgba = np.stack([r, g, b, a], axis=-1)
    h, w = rgba.shape[:2]
    flipped = np.flipud(np.clip(rgba, 0.0, 1.0)).astype(np.float32)
    out_img = bpy.data.images.new("__save_rgba", width=w, height=h, alpha=True, float_buffer=False)
    out_img.colorspace_settings.name = "Non-Color"
    out_img.pixels.foreach_set(flipped.ravel())
    out_img.filepath_raw = str(path)
    out_img.file_format = "PNG"
    out_img.save()
    bpy.data.images.remove(out_img)


def _bake_pass(objs, mats, bake_type: str, res: int, colorspace="Non-Color", samples=6, use_emit_metallic=False):
    img = bpy.data.images.new(f"__bake_{bake_type}", width=res, height=res, alpha=False, float_buffer=True)
    img.colorspace_settings.name = colorspace
    saved_links = []

    if use_emit_metallic:
        # Temporarily route Metallic socket -> Emission Output so Cycles bakes exact per-pixel Metallic!
        for m in mats:
            nt = m.node_tree
            out = next((n for n in nt.nodes if n.type == "OUTPUT_MATERIAL"), None)
            bsdf = nt.nodes.get("BSDF")
            if not out or not bsdf:
                continue
            orig_surf = list(out.inputs["Surface"].links)
            em = nt.nodes.new("ShaderNodeEmission")
            em.name = "__TEMP_EMIT__"
            msock = bsdf.inputs["Metallic"]
            if msock.is_linked:
                src = msock.links[0].from_socket
                nt.links.new(src, em.inputs["Color"])
            else:
                v = msock.default_value
                em.inputs["Color"].default_value = (v, v, v, 1.0)
            saved_links.append((nt, out, bsdf, em, [l.from_socket for l in orig_surf]))
            nt.links.new(em.outputs["Emission"], out.inputs["Surface"])

    created = _attach_bake_image(mats, img)
    _configure_cycles_bake(samples=samples)
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]

    if bake_type == "DIFFUSE":
        bpy.ops.object.bake(type="DIFFUSE", pass_filter={"COLOR"})
    elif bake_type == "NORMAL":
        bpy.ops.object.bake(type="NORMAL", normal_space="TANGENT", normal_r="POS_X", normal_g="POS_Y", normal_b="POS_Z")
    elif bake_type == "AO":
        bpy.ops.object.bake(type="AO")
    elif bake_type == "ROUGHNESS":
        bpy.ops.object.bake(type="ROUGHNESS")
    elif bake_type == "EMIT":
        bpy.ops.object.bake(type="EMIT")

    data = _image_to_np(img)
    _detach_bake_image(created)
    bpy.data.images.remove(img)

    for nt, out, bsdf, em, srcs in saved_links:
        nt.nodes.remove(em)
        for s in srcs:
            nt.links.new(s, out.inputs["Surface"])
    return data


def bake_atlas_maps(objs, atlas_name: str, res: int = 1024, bake_r: bool = True, ao_samples: int = 18):
    """Bake Unity URP PBR maps (B & optionally R) for a group of objects sharing one UV atlas."""
    mats = []
    for o in objs:
        for m in o.data.materials:
            if m and m not in mats and m.name.startswith("H_"):
                mats.append(m)
    if not mats:
        return

    print(f"  [BAKE] {atlas_name} ({res}x{res}, {len(objs)} meshes, {len(mats)} proc mats)...", flush=True)

    # Shared geometry-level passes: Tangent Normal + Raytraced AO
    set_age(0.0)
    normal_arr = _bake_pass(objs, mats, "NORMAL", res, colorspace="Non-Color", samples=2)
    _save_rgb_png(normal_arr, TEX_DIR / f"{atlas_name}_Normal.png", srgb=False)

    ao_arr = _bake_pass(objs, mats, "AO", res, colorspace="Non-Color", samples=min(ao_samples, 10))
    ao_ch = np.clip(ao_arr[..., 0], 0.0, 1.0)
    # Gentle contrast lift so AO enriches cavities without crushing darks
    ao_ch = np.clip(np.power(ao_ch, 0.85), 0.16, 1.0)

    # Layer B (2005 operational, AGE = 0.0)
    set_age(0.0)
    col_b = _bake_pass(objs, mats, "DIFFUSE", res, colorspace="sRGB", samples=2)
    rough_b = _bake_pass(objs, mats, "ROUGHNESS", res, colorspace="Non-Color", samples=1)[..., 0]
    metal_b = _bake_pass(objs, mats, "EMIT", res, colorspace="Non-Color", samples=1, use_emit_metallic=True)[..., 0]
    smooth_b = np.clip(1.0 - rough_b, 0.04, 0.96)
    _save_rgb_png(col_b, TEX_DIR / f"{atlas_name}B_Color.png", srgb=True)
    _save_rgba_png(metal_b, ao_ch, np.zeros_like(ao_ch), smooth_b, TEX_DIR / f"{atlas_name}B_Mask.png")

    if bake_r:
        # Layer R (2012 abandoned, AGE = 1.0)
        set_age(1.0)
        col_r = _bake_pass(objs, mats, "DIFFUSE", res, colorspace="sRGB", samples=2)
        rough_r = _bake_pass(objs, mats, "ROUGHNESS", res, colorspace="Non-Color", samples=1)[..., 0]
        smooth_r = np.clip(1.0 - rough_r, 0.04, 0.90)
        metal_r = metal_b * 0.85
        _save_rgb_png(col_r, TEX_DIR / f"{atlas_name}R_Color.png", srgb=True)
        _save_rgba_png(metal_r, ao_ch, np.zeros_like(ao_ch), smooth_r, TEX_DIR / f"{atlas_name}R_Mask.png")
    else:
        # Single-state asset: also mirror Color/Mask without B/R suffix for convenience
        _save_rgb_png(col_b, TEX_DIR / f"{atlas_name}_Color.png", srgb=True)
        _save_rgba_png(metal_b, ao_ch, np.zeros_like(ao_ch), smooth_b, TEX_DIR / f"{atlas_name}_Mask.png")


def make_baked_preview_mat(mat_key: str, atlas_name: str, variant: str = "B"):
    """Create a Blender PBR material pointing directly to the baked Unity maps (for .blend + Cycles preview)."""
    m = bpy.data.materials.get(mat_key)
    if m is None:
        m = bpy.data.materials.new(mat_key)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])

    suffix = variant if variant in ("B", "R") else ""
    col_path = TEX_DIR / f"{atlas_name}{suffix}_Color.png"
    mask_path = TEX_DIR / f"{atlas_name}{suffix}_Mask.png"
    norm_path = TEX_DIR / f"{atlas_name}_Normal.png"

    if col_path.exists():
        tc = nt.nodes.new("ShaderNodeTexImage")
        tc.image = bpy.data.images.load(str(col_path), check_existing=False)
        tc.image.colorspace_settings.name = "sRGB"
        nt.links.new(tc.outputs["Color"], bsdf.inputs["Base Color"])
    if mask_path.exists():
        tm = nt.nodes.new("ShaderNodeTexImage")
        tm.image = bpy.data.images.load(str(mask_path), check_existing=False)
        tm.image.colorspace_settings.name = "Non-Color"
        sep = nt.nodes.new("ShaderNodeSeparateColor")
        nt.links.new(tm.outputs["Color"], sep.inputs["Color"])
        nt.links.new(sep.outputs["Red"], bsdf.inputs["Metallic"])
        inv = nt.nodes.new("ShaderNodeMath")
        inv.operation = "SUBTRACT"
        inv.inputs[0].default_value = 1.0
        nt.links.new(tm.outputs["Alpha"], inv.inputs[1])
        nt.links.new(inv.outputs["Value"], bsdf.inputs["Roughness"])
    if norm_path.exists():
        tn = nt.nodes.new("ShaderNodeTexImage")
        tn.image = bpy.data.images.load(str(norm_path), check_existing=False)
        tn.image.colorspace_settings.name = "Non-Color"
        nm = nt.nodes.new("ShaderNodeNormalMap")
        nt.links.new(tn.outputs["Color"], nm.inputs["Color"])
        nt.links.new(nm.outputs["Normal"], bsdf.inputs["Normal"])
    return m


def _render_single_view(objs, out_path: Path, cam_dir=(1.45, -1.55, 0.85), resolution=(800, 600), samples=28, warm_key=True):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = samples
    scene.cycles.use_denoising = True
    scene.cycles.max_bounces = 4
    scene.cycles.diffuse_bounces = 2
    scene.cycles.glossy_bounces = 2
    scene.render.resolution_x = resolution[0]
    scene.render.resolution_y = resolution[1]
    scene.render.image_settings.file_format = "PNG"
    scene.view_settings.view_transform = "AgX"

    world = bpy.data.worlds.new("_HeroStudio")
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    bg.inputs["Color"].default_value = (0.055, 0.068, 0.064, 1.0) if warm_key else (0.032, 0.044, 0.050, 1.0)
    bg.inputs["Strength"].default_value = 0.75 if warm_key else 0.55
    scene.world = world

    bmin, bmax = C.compute_bounds(objs)
    center = (bmin + bmax) * 0.5
    diag = max(0.20, (bmax - bmin).length)

    floor_z = min(bmin.z - 0.002, (center + Vector(cam_dir).normalized() * (diag * 1.35)).z - diag * 0.6)
    bpy.ops.mesh.primitive_plane_add(size=diag * 6.0, location=(center.x, center.y, floor_z))
    floor = bpy.context.object
    fmat = bpy.data.materials.new("_PreviewFloor")
    fmat.use_nodes = True
    fmat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.05, 0.062, 0.058, 1.0)
    fmat.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.72
    floor.data.materials.append(fmat)

    def area_light(lname, offset, power, color, size):
        ldata = bpy.data.lights.new(lname, type="AREA")
        ldata.energy = power
        ldata.color = color
        ldata.size = size
        lobj = bpy.data.objects.new(lname, ldata)
        lobj.location = center + Vector(offset) * diag
        lobj.rotation_euler = (center - lobj.location).to_track_quat("-Z", "Y").to_euler()
        bpy.context.collection.objects.link(lobj)
        return lobj

    key_col = (1.0, 0.96, 0.88) if warm_key else (0.76, 0.86, 0.96)
    fill_col = (0.72, 0.86, 0.92) if warm_key else (0.55, 0.70, 0.82)
    lights = [
        area_light("_Key", (1.25, -1.15, 1.35), diag * diag * (190.0 if warm_key else 145.0), key_col, diag * 0.85),
        area_light("_Fill", (-1.35, -0.85, 0.65), diag * diag * 65.0, fill_col, diag * 1.05),
        area_light("_Rim", (-0.65, 1.45, 1.15), diag * diag * 105.0, (0.80, 0.94, 0.88), diag * 0.65),
    ]

    cam_data = bpy.data.cameras.new("_Cam")
    cam_data.lens = 52.0
    cam_obj = bpy.data.objects.new("_Cam", cam_data)
    dvec = Vector(cam_dir).normalized()
    cam_obj.location = center + dvec * (diag * 1.32)
    cam_obj.rotation_euler = (center - cam_obj.location).to_track_quat("-Z", "Y").to_euler()
    bpy.context.collection.objects.link(cam_obj)
    scene.camera = cam_obj

    scene.render.filepath = str(out_path)
    bpy.ops.render.render(write_still=True)

    for tmp in [floor, cam_obj, *lights]:
        bpy.data.objects.remove(tmp, do_unlink=True)
    bpy.data.worlds.remove(world, do_unlink=True)


def render_rb_comparison(name: str, objs, slot_atlas_map: dict, cam_dir=(1.45, -1.55, 0.85)):
    """Render side-by-side B (2005) and R (2012) Cycles previews using the actual baked PBR maps."""
    p_b = RENDER_DIR / f"{name}_B.png"
    p_r = RENDER_DIR / f"{name}_R.png"
    # Render B
    _render_single_view(objs, p_b, cam_dir=cam_dir, resolution=(720, 540), samples=24, warm_key=True)
    # Swap baked materials to R variant for R render
    orig_mats = {}
    for o in objs:
        orig_mats[o] = list(o.data.materials)
        for i, m in enumerate(o.data.materials):
            if m and m.name in slot_atlas_map:
                atlas = slot_atlas_map[m.name]
                if (TEX_DIR / f"{atlas}R_Color.png").exists():
                    o.data.materials[i] = make_baked_preview_mat(f"{atlas}R_Prev", atlas, "R")
    _render_single_view(objs, p_r, cam_dir=cam_dir, resolution=(720, 540), samples=24, warm_key=False)
    # Restore original materials IN-PLACE (NEVER call .clear(), which zeroes polygon material_index to 0!)
    for o, ms in orig_mats.items():
        for i, m in enumerate(ms):
            o.data.materials[i] = m

    # Compose side-by-side comparison sheet using Blender native images + numpy
    ib = bpy.data.images.load(str(p_b), check_existing=False)
    ir = bpy.data.images.load(str(p_r), check_existing=False)
    ab = _image_to_np(ib)[..., :3]
    ar = _image_to_np(ir)[..., :3]
    bpy.data.images.remove(ib)
    bpy.data.images.remove(ir)
    gap = np.full((ab.shape[0], 8, 3), 0.07, dtype=np.float32)
    combo = np.concatenate([ab, gap, ar], axis=1)
    _save_rgb_png(combo, RENDER_DIR / f"{name}_BR.png", srgb=False)
    p_b.unlink(missing_ok=True)
    p_r.unlink(missing_ok=True)
    return RENDER_DIR / f"{name}_BR.png"


def export_hero_asset(
    asset_name: str,
    build_fn,
    atlas_specs: dict,
    make_lods: bool = True,
    separate_parts: bool = False,
    cam_dir=(1.45, -1.55, 0.85),
    render: bool = True,
    ao_samples: int = 18,
    passthrough_kinds=("Glass", "FrostedGlass", "DeadTube", "LiveTube", "WarmGlow"),
):
    """Master production entry point for a LOC02 hero asset.

    `build_fn()` returns either:
      - `list[obj]` (single mesh asset)
      - `dict[part_name, list[obj]]` (when `separate_parts=True` or multi-atlas grouping!)

    `atlas_specs`: dict mapping `atlas_key -> {"res": 2048, "bake_r": True, "parts": [...] or None}`
      - If `parts` is None, all non-passthrough objects go into that single atlas.
      - The exported FBX material slot name for each baked atlas is `<atlas_key>B` (so Unity's
        PlaceBlenderModel / ReportKey / AgedKey automatically maps `<atlas_key>B` <-> `<atlas_key>R`!).
    """
    C.fresh_scene()
    built = build_fn()

    # Collect objects grouped by part_name
    if isinstance(built, dict):
        part_dict = {k: [o for o in v if o is not None] for k, v in built.items()}
    else:
        part_dict = {asset_name: [o for o in built if o is not None]}

    all_objs = [o for lst in part_dict.values() for o in lst]

    # Separate passthrough (glass/emissive) objects from bakeable objects
    def is_passthrough(o):
        return o.get("kind") in passthrough_kinds or (len(o.data.materials) > 0 and o.data.materials[0].name in passthrough_kinds)

    # CRITICAL PERFORMANCE: Join sub-meshes per (part_name, bakeable/passthrough) BEFORE Cycles baking!
    # Baking 1 joined mesh runs 1 Cycles bake instead of 90 separate per-object Cycles bakes (30x faster!).
    # Preserve 'PRINT' UV & 'grain' color attribute across every object before joining.
    has_any_print = any("PRINT" in o.data.uv_layers for o in all_objs)
    for o in all_objs:
        if has_any_print and "PRINT" not in o.data.uv_layers:
            o.data.uv_layers.new(name="PRINT")
        if "UVMap" not in o.data.uv_layers:
            o.data.uv_layers.new(name="UVMap")

    joined_parts = {}
    for pname, objs in part_dict.items():
        bake_list = [o for o in objs if not is_passthrough(o)]
        pass_list = [o for o in objs if is_passthrough(o)]
        out_list = []
        if bake_list:
            jb = join(bake_list, f"{pname}_Bake")
            if jb:
                jb["__bakeable__"] = True
                out_list.append(jb)
        if pass_list:
            jp = join(pass_list, f"{pname}_Pass")
            if jp:
                jp["__bakeable__"] = False
                out_list.append(jp)
        joined_parts[pname] = out_list

    part_dict = joined_parts
    all_objs = [o for lst in part_dict.values() for o in lst]

    # Determine which joined meshes belong to each atlas
    obj_to_atlas = {}
    for atlas_key, spec in atlas_specs.items():
        target_parts = spec.get("parts")
        atlas_objs = []
        for pname, objs in part_dict.items():
            if target_parts is None or pname in target_parts:
                for o in objs:
                    if o.get("__bakeable__", False):
                        atlas_objs.append(o)
                        obj_to_atlas[o] = atlas_key
        if not atlas_objs:
            continue
        _ensure_bake_uv(atlas_objs, angle_limit_deg=spec.get("angle", 52.0), island_margin=spec.get("margin", 0.005))
        bake_atlas_maps(atlas_objs, atlas_key, res=spec.get("res", 1024), bake_r=spec.get("bake_r", True), ao_samples=ao_samples)

    # Assign final single baked material per atlas (named `<atlas_key>B` or `<atlas_key>` matching Unity!)
    slot_atlas_map = {}
    for atlas_key, spec in atlas_specs.items():
        bake_r = spec.get("bake_r", True)
        mat_name = f"{atlas_key}B" if bake_r else atlas_key
        baked_mat = make_baked_preview_mat(mat_name, atlas_key, "B" if bake_r else "")
        slot_atlas_map[mat_name] = atlas_key
        for o, ak in obj_to_atlas.items():
            if ak == atlas_key:
                o.data.materials.clear()
                o.data.materials.append(baked_mat)
                # Drop helper PRINT UV layer before FBX export so Unity reads baked UVMap cleanly as TEXCOORD0
                if "PRINT" in o.data.uv_layers:
                    o.data.uv_layers.remove(o.data.uv_layers["PRINT"])

    # Join parts according to separate_parts
    export_objs = []
    preview_objs = []
    if separate_parts:
        for pname, objs in part_dict.items():
            if not objs:
                continue
            j = join(objs, pname)
            if j:
                export_objs.append(j)
                preview_objs.append(j)
    else:
        lod0 = join(all_objs, f"{asset_name}_LOD0" if make_lods else asset_name)
        export_objs.append(lod0)
        preview_objs.append(lod0)
        tc = tri_count(lod0)
        if make_lods and tc > 650:
            lod1 = C.make_lod_copy(lod0, f"{asset_name}_LOD1", 0.48)
            lod2 = C.make_lod_copy(lod0, f"{asset_name}_LOD2", 0.22)
            export_objs.extend([lod1, lod2])

    bmin, bmax = C.compute_bounds(preview_objs)
    col_mesh = C.make_collision_box(asset_name, bmin, bmax)

    if render:
        for o in export_objs:
            o.hide_render = o not in preview_objs
        col_mesh.hide_render = True
        render_rb_comparison(asset_name, preview_objs, slot_atlas_map, cam_dir=cam_dir)
        for o in export_objs:
            o.hide_render = False
        col_mesh.hide_render = False

    # Organize editable .blend
    col_coll = bpy.data.collections.new("Collision")
    bpy.context.scene.collection.children.link(col_coll)
    bpy.context.scene.collection.objects.unlink(col_mesh)
    col_coll.objects.link(col_mesh)

    blend_path = SRC_DIR / f"{asset_name}.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))

    # Triangulate any n-gons (>4 verts) on export meshes so Blender exports 100% clean MikkTSpace tangents!
    for o in export_objs:
        if o.type != "MESH":
            continue
        bm = bmesh.new()
        bm.from_mesh(o.data)
        ngons = [f for f in bm.faces if len(f.verts) > 4]
        if ngons:
            bmesh.ops.triangulate(bm, faces=ngons)
            bm.to_mesh(o.data)
            o.data.update()
        bm.free()

    # Export Unity FBX
    bpy.ops.object.select_all(action="DESELECT")
    for o in export_objs:
        o.select_set(True)
    fbx_path = FBX_DIR / f"{asset_name}.fbx"
    bpy.ops.export_scene.fbx(
        filepath=str(fbx_path),
        use_selection=True,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        object_types={"MESH"},
        mesh_smooth_type="FACE",
        use_mesh_modifiers=True,
        use_tspace=True,
        add_leaf_bones=False,
        bake_anim=False,
        embed_textures=False,
    )
    lod0_tris = sum(tri_count(o) for o in preview_objs)
    dims = bmax - bmin
    print(f"[HERO ASSET] {asset_name}: LOD0={lod0_tris} tris | size=({dims.x:.2f}x{dims.y:.2f}x{dims.z:.2f}m) -> {fbx_path.name}")
    return lod0_tris
