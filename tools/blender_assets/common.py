"""Shared Blender 4.5 production utilities for Ca truc cuoi cung (M0-R).

Provides:
- Metric scene setup (1 BU = 1 m)
- Weighted-normal & beveled architectural/prop modeling primitives (bmesh, swept pipes, lathes, sculpted cloth)
- Real-world-scale box/cylindrical UV projection (zero texture stretching)
- Automatic LOD0 / LOD1 / LOD2 generation (Unity LODGroup naming convention) + simplified Collision collection
- Editable .blend source export (ArtSource/Blender/<Name>.blend)
- Game-ready Unity FBX export (Prototype/Assets/CTCC/Art/Meshes/<Name>.fbx)
- Cycles studio verification rendering with actual PBR maps
"""

import math
import os
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
BLEND_DIR = ROOT / "ArtSource" / "Blender"
FBX_DIR = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Meshes"
TEX_DIR = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Textures"
RENDER_DIR = ROOT / "output" / "m2-evidence" / "blender-renders"

# Material preview definitions matching Unity PBR library names
PREVIEW_PALETTE = {
    "PlasterR": ((0.54, 0.56, 0.50), 0.0, 0.84, "PlasterR"),
    "PlasterB": ((0.76, 0.77, 0.69), 0.0, 0.80, "PlasterB"),
    "DadoR": ((0.24, 0.35, 0.31), 0.0, 0.64, "DadoR"),
    "DadoB": ((0.28, 0.46, 0.39), 0.0, 0.54, "DadoB"),
    "WoodDoor": ((0.25, 0.16, 0.09), 0.0, 0.58, "WoodDoorR"),
    "Wood": ((0.28, 0.19, 0.11), 0.0, 0.62, "WoodDoorR"),
    "MetalPaint": ((0.36, 0.47, 0.41), 0.18, 0.52, "PaintedMetalB"),
    "MetalOld": ((0.24, 0.30, 0.26), 0.22, 0.64, "PaintedMetalR"),
    "CabinetPaint": ((0.42, 0.52, 0.46), 0.18, 0.50, "PaintedMetalB"),
    "CabinetOld": ((0.27, 0.34, 0.29), 0.20, 0.62, "PaintedMetalR"),
    "Frame": ((0.22, 0.31, 0.27), 0.25, 0.55, "PaintedMetalR"),
    "Steel": ((0.68, 0.71, 0.70), 0.92, 0.30, "Steel"),
    "Brass": ((0.64, 0.52, 0.24), 0.88, 0.36, "Steel"),
    "Rubber": ((0.08, 0.09, 0.085), 0.0, 0.68, None),
    "Cotton": ((0.75, 0.77, 0.70), 0.0, 0.90, "Cotton"),
    "CottonOld": ((0.55, 0.55, 0.46), 0.0, 0.92, "CottonOld"),
    "Textile": ((0.35, 0.52, 0.44), 0.0, 0.90, "Cotton"),
    "TextileOld": ((0.27, 0.36, 0.32), 0.0, 0.92, "CottonOld"),
    "Mattress": ((0.29, 0.44, 0.39), 0.0, 0.56, "MattressB"),
    "MattressOld": ((0.24, 0.31, 0.26), 0.0, 0.70, "MattressR"),
    "Curtain": ((0.64, 0.74, 0.68), 0.0, 0.88, "Cotton"),
    "Ceramic": ((0.78, 0.80, 0.75), 0.0, 0.34, "Ceramic"),
    "EnamelBlue": ((0.13, 0.24, 0.38), 0.05, 0.32, "Ceramic"),
    "BaseTrim": ((0.26, 0.29, 0.27), 0.0, 0.62, "FloorR"),
    "Paper": ((0.78, 0.73, 0.58), 0.0, 0.88, None),
    "Glass": ((0.58, 0.72, 0.69), 0.0, 0.14, None),
    "FrostedGlass": ((0.68, 0.75, 0.72), 0.0, 0.48, None),
    "DeadTube": ((0.54, 0.58, 0.56), 0.0, 0.26, None),
    "LiveTube": ((0.88, 0.96, 0.90), 0.0, 0.22, None),
    "RedPaint": ((0.56, 0.12, 0.10), 0.15, 0.42, None),
    "Black": ((0.06, 0.07, 0.07), 0.05, 0.55, None),
    "SlipperYellow": ((0.72, 0.67, 0.46), 0.0, 0.65, None),
    "ReedMat": ((0.64, 0.57, 0.37), 0.0, 0.82, "CottonOld"),
    "Bark": ((0.22, 0.18, 0.14), 0.0, 0.88, "Bark"),
    "TropicalLeaf": ((0.20, 0.34, 0.18), 0.0, 0.52, "TropicalLeaf"),
    "TropicalShrub": ((0.18, 0.35, 0.17), 0.0, 0.42, "TropicalShrub"),
    "KraftPaper": ((0.64, 0.53, 0.36), 0.0, 0.90, "KraftPaper"),
    "GreenOxygen": ((0.16, 0.38, 0.26), 0.20, 0.44, "PaintedMetalB"),
    "SalineFluid": ((0.75, 0.84, 0.86), 0.0, 0.08, None),
    "PlasticIvory": ((0.74, 0.71, 0.60), 0.0, 0.46, None),
    # M2 Exterior, ER & Character Materials
    "WetAsphalt": ((0.18, 0.19, 0.20), 0.0, 0.40, "WetAsphalt"),
    "ExteriorFacade": ((0.64, 0.59, 0.44), 0.0, 0.82, "ExteriorFacade"),
    "WeatheredBrick": ((0.42, 0.23, 0.16), 0.0, 0.84, "WeatheredBrick"),
    "TerrazzoBench": ((0.48, 0.50, 0.48), 0.0, 0.42, "TerrazzoBench"),
    "MotorbikePaint": ((0.30, 0.09, 0.10), 0.35, 0.28, None),
    "CrimsonGlow": ((0.85, 0.18, 0.14), 0.0, 0.25, None),
    "MonitorScreen": ((0.08, 0.20, 0.16), 0.0, 0.20, None),
    "WarmGlow": ((0.96, 0.78, 0.46), 0.0, 0.25, None),
    "NurseBlouse": ((0.84, 0.82, 0.74), 0.0, 0.88, "NurseBlouse"),
    "NurseTrim": ((0.26, 0.44, 0.42), 0.0, 0.84, None),
    "LanSkin": ((0.76, 0.59, 0.47), 0.0, 0.66, "LanSkin"),
    "LanLip": ((0.58, 0.33, 0.30), 0.0, 0.52, None),
    "LanHair": ((0.065, 0.052, 0.044), 0.0, 0.48, "LanHair"),
    "LanEyeWhite": ((0.69, 0.65, 0.61), 0.0, 0.24, None),
    "LanIris": ((0.065, 0.042, 0.029), 0.0, 0.21, None),
    "LanBadgeMat": ((0.84, 0.81, 0.72), 0.0, 0.32, None),
    "DarkTrouser": ((0.11, 0.13, 0.15), 0.0, 0.86, "DarkTrouser"),
    "CharredBlouse": ((0.18, 0.18, 0.16), 0.0, 0.92, "CharredBlouse"),
    "ObscuredFace": ((0.075, 0.078, 0.076), 0.0, 0.90, "ObscuredFace"),
    "AnCotton": ((0.64, 0.68, 0.66), 0.0, 0.88, "AnCotton"),
    "AnSkin": ((0.72, 0.62, 0.54), 0.0, 0.74, None),
}


def fresh_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    return scene


def get_material(key):
    mat = bpy.data.materials.get(key)
    if mat is not None:
        return mat
    mat = bpy.data.materials.new(key)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    col, met, rough, tex_prefix = PREVIEW_PALETTE.get(key, ((0.5, 0.5, 0.5), 0.0, 0.6, None))
    bsdf.inputs["Base Color"].default_value = (*col, 1.0)
    bsdf.inputs["Metallic"].default_value = met
    bsdf.inputs["Roughness"].default_value = rough
    if key in ("Glass", "FrostedGlass", "SalineFluid"):
        bsdf.inputs["Alpha"].default_value = 0.28 if key != "FrostedGlass" else 0.55
        bsdf.inputs["IOR"].default_value = 1.50
    elif key == "LiveTube":
        bsdf.inputs["Emission Color"].default_value = (0.76, 0.96, 0.84, 1.0)
        bsdf.inputs["Emission Strength"].default_value = 3.0

    # Link real PBR maps for Blender viewport & Cycles verification renders!
    if tex_prefix and (TEX_DIR / f"{tex_prefix}_Color.png").exists():
        tex_col = nodes.new("ShaderNodeTexImage")
        tex_col.image = bpy.data.images.load(str(TEX_DIR / f"{tex_prefix}_Color.png"), check_existing=True)
        # Tint mix if preview color differs from raw map
        mix = nodes.new("ShaderNodeMix")
        mix.data_type = "RGBA"
        mix.blend_type = "MULTIPLY"
        mix.inputs["Factor"].default_value = 0.22 if tex_prefix in ("Cotton", "CottonOld", "PaintedMetalB", "PaintedMetalR") else 0.0
        mix.inputs["B"].default_value = (*col, 1.0)
        links.new(tex_col.outputs["Color"], mix.inputs["A"])
        links.new(mix.outputs["Result"], bsdf.inputs["Base Color"])
        if key in ("TropicalLeaf", "TropicalShrub"):
            links.new(tex_col.outputs["Alpha"], bsdf.inputs["Alpha"])

        norm_path = TEX_DIR / f"{tex_prefix}_Normal.png"
        if norm_path.exists():
            tex_n = nodes.new("ShaderNodeTexImage")
            tex_n.image = bpy.data.images.load(str(norm_path), check_existing=True)
            tex_n.image.colorspace_settings.name = "Non-Color"
            nmap = nodes.new("ShaderNodeNormalMap")
            nmap.inputs["Strength"].default_value = 0.85
            links.new(tex_n.outputs["Color"], nmap.inputs["Color"])
            links.new(nmap.outputs["Normal"], bsdf.inputs["Normal"])
    return mat


def assign_mat(obj, key):
    mat = get_material(key)
    if len(obj.data.materials) == 0:
        obj.data.materials.append(mat)
    else:
        obj.data.materials[0] = mat
    return obj


def set_smooth_by_angle(obj, angle_deg=38.0):
    """Smooth shading on polygons with crisp normal breaks on sharp edges (Blender 4.5 compatible)."""
    mesh = obj.data
    if not isinstance(mesh, bpy.types.Mesh):
        return
    for p in mesh.polygons:
        p.use_smooth = True
    # Mark sharp edges by dihedral angle using bmesh
    bm = bmesh.new()
    bm.from_mesh(mesh)
    rad = math.radians(angle_deg)
    for e in bm.edges:
        if len(e.link_faces) == 2:
            if e.calc_face_angle(0.0) > rad:
                e.smooth = False
            else:
                e.smooth = True
        else:
            e.smooth = False
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()


def add_box_uv(obj, scale=1.0):
    """Metric-scaled tri-planar box UV projection so tileable PBR textures never stretch."""
    mesh = obj.data
    if not isinstance(mesh, bpy.types.Mesh) or len(mesh.polygons) == 0:
        return
    if not mesh.uv_layers:
        mesh.uv_layers.new(name="UVMap")
    uv_layer = mesh.uv_layers.active.data
    mat_w = obj.matrix_world
    inv_scale = 1.0 / max(scale, 0.01)
    for poly in mesh.polygons:
        n = (mat_w.to_3x3() @ poly.normal).normalized()
        ax, ay, az = abs(n.x), abs(n.y), abs(n.z)
        for li in poly.loop_indices:
            v = mat_w @ mesh.vertices[mesh.loops[li].vertex_index].co
            if az >= ax and az >= ay:
                uv_layer[li].uv = (v.x * inv_scale, v.y * inv_scale)
            elif ax >= ay:
                uv_layer[li].uv = (v.y * inv_scale, v.z * inv_scale)
            else:
                uv_layer[li].uv = (v.x * inv_scale, v.z * inv_scale)


def finalize_mesh(obj, name, key, smooth_angle=38.0, uv_scale=1.0, apply_mods=True):
    obj.name = name
    assign_mat(obj, key)
    if apply_mods and obj.type == "MESH" and len(obj.modifiers) > 0:
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        for mod in list(obj.modifiers):
            try:
                bpy.ops.object.modifier_apply(modifier=mod.name)
            except Exception:
                pass
    if obj.type == "CURVE":
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.convert(target="MESH")
        obj = bpy.context.active_object
        obj.name = name
        assign_mat(obj, key)
    set_smooth_by_angle(obj, smooth_angle)
    add_box_uv(obj, uv_scale)
    return obj


def make_box(name, loc, size, key, bevel=0.005, segments=2, rot=(0, 0, 0), uv_scale=1.0):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel > 0.0:
        max_b = min(size) * 0.42
        b = obj.modifiers.new("Bevel", "BEVEL")
        b.width = min(bevel, max_b)
        b.segments = segments
        b.limit_method = "ANGLE"
    return finalize_mesh(obj, name, key, smooth_angle=42.0, uv_scale=uv_scale)


def make_cylinder(name, start, end, radius, key, segments=18, r_end=None, cap=True, bevel=0.0, uv_scale=1.0):
    p0, p1 = Vector(start), Vector(end)
    delta = p1 - p0
    length = delta.length
    if length < 1e-5:
        length = 1e-4
    r2 = radius if r_end is None else r_end
    bpy.ops.mesh.primitive_cone_add(
        vertices=segments,
        radius1=radius,
        radius2=r2,
        depth=length,
        end_fill_type="NGON" if cap else "NOTHING",
        location=(p0 + p1) * 0.5,
    )
    obj = bpy.context.object
    obj.rotation_euler = delta.to_track_quat("Z", "Y").to_euler()
    if bevel > 0 and cap:
        b = obj.modifiers.new("Bevel", "BEVEL")
        b.width = min(bevel, radius * 0.35, length * 0.3)
        b.segments = 2
        b.limit_method = "ANGLE"
    return finalize_mesh(obj, name, key, smooth_angle=42.0, uv_scale=uv_scale)


def make_pipe(name, points, radius, key, bevel_res=4, cyclic=False, uv_scale=0.8):
    curve = bpy.data.curves.new(name + "_crv", "CURVE")
    curve.dimensions = "3D"
    curve.bevel_depth = radius
    curve.bevel_resolution = bevel_res
    curve.use_fill_caps = not cyclic
    spline = curve.splines.new("POLY")
    spline.points.add(len(points) - 1)
    for pt, co in zip(spline.points, points):
        pt.co = (co[0], co[1], co[2], 1.0)
    spline.use_cyclic_u = cyclic
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    return finalize_mesh(obj, name, key, smooth_angle=48.0, uv_scale=uv_scale)


def make_rounded_u(y, bottom, top, half_w, radius=0.075, arc_steps=7, plane="XZ"):
    pts = []
    if plane == "XZ":
        pts.append((-half_w, y, bottom))
        pts.append((-half_w, y, top - radius))
        for i in range(1, arc_steps + 1):
            a = math.pi - (math.pi * 0.5) * i / arc_steps
            pts.append((-half_w + radius + radius * math.cos(a), y, top - radius + radius * math.sin(a)))
        pts.append((half_w - radius, y, top))
        for i in range(1, arc_steps + 1):
            a = (math.pi * 0.5) * (1.0 - i / arc_steps)
            pts.append((half_w - radius + radius * math.cos(a), y, top - radius + radius * math.sin(a)))
        pts.append((half_w, y, bottom))
    return pts


def make_lathe(name, profile_rz, loc, key, segments=28, rot=(0, 0, 0), uv_scale=0.6):
    """Revolution mesh from list of (radius, z) points in local space."""
    bm = bmesh.new()
    rings = []
    for r, z in profile_rz:
        if r <= 1e-5:
            v = bm.verts.new((0.0, 0.0, z))
            rings.append([v])
        else:
            ring = []
            for i in range(segments):
                a = math.tau * i / segments
                ring.append(bm.verts.new((math.cos(a) * r, math.sin(a) * r, z)))
            rings.append(ring)
    for k in range(len(rings) - 1):
        r0, r1 = rings[k], rings[k + 1]
        if len(r0) == 1 and len(r1) > 1:
            for i in range(segments):
                bm.faces.new((r0[0], r1[(i + 1) % segments], r1[i]))
        elif len(r1) == 1 and len(r0) > 1:
            for i in range(segments):
                bm.faces.new((r0[i], r0[(i + 1) % segments], r1[0]))
        elif len(r0) > 1 and len(r1) > 1:
            for i in range(segments):
                ni = (i + 1) % segments
                bm.faces.new((r0[i], r0[ni], r1[ni], r1[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new(name + "_mesh")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    obj.location = loc
    obj.rotation_euler = rot
    bpy.context.collection.objects.link(obj)
    return finalize_mesh(obj, name, key, smooth_angle=44.0, uv_scale=uv_scale)


def make_sphere(name, loc, dims, key, segs=22, rings=12, rot=(0, 0, 0), uv_scale=0.6):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segs, ring_count=rings, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finalize_mesh(obj, name, key, smooth_angle=60.0, uv_scale=uv_scale)


def make_cloth_grid(name, width, length, nx, ny, z_fn, loc, key, thickness=0.006, uv_tile=(3.0, 4.0)):
    verts = []
    faces = []
    for j in range(ny + 1):
        for i in range(nx + 1):
            u, v = i / float(nx), j / float(ny)
            x, y, z = z_fn(u, v, width, length)
            verts.append((x + loc[0], y + loc[1], z + loc[2]))
            if i and j:
                idx = j * (nx + 1) + i
                faces.append((idx - nx - 2, idx - nx - 1, idx, idx - 1))
    mesh = bpy.data.meshes.new(name + "_mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    uv = mesh.uv_layers.new(name="UVMap")
    for face in mesh.polygons:
        face.use_smooth = True
        for li in face.loop_indices:
            vi = mesh.loops[li].vertex_index
            j, i = divmod(vi, nx + 1)
            uv.data[li].uv = (uv_tile[0] * i / nx, uv_tile[1] * j / ny)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    if thickness > 0:
        sol = obj.modifiers.new("Solidify", "SOLIDIFY")
        sol.thickness = thickness
    assign_mat(obj, key)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    for mod in list(obj.modifiers):
        bpy.ops.object.modifier_apply(modifier=mod.name)
    set_smooth_by_angle(obj, 65.0)
    return obj


def join_objects(objs, name):
    """Join a list of mesh objects into one clean object while preserving multi-material slots."""
    valid = [o for o in objs if o is not None and o.type == "MESH"]
    if not valid:
        return None
    bpy.ops.object.select_all(action="DESELECT")
    for o in valid:
        o.select_set(True)
    bpy.context.view_layer.objects.active = valid[0]
    if len(valid) > 1:
        bpy.ops.object.join()
    joined = bpy.context.active_object
    joined.name = name
    # Reset origin to world (0,0,0) so asset pivots match architectural grid exactly!
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return joined


def make_lod_copy(src_obj, name, ratio):
    dup = src_obj.copy()
    dup.data = src_obj.data.copy()
    dup.name = name
    bpy.context.collection.objects.link(dup)
    bpy.ops.object.select_all(action="DESELECT")
    dup.select_set(True)
    bpy.context.view_layer.objects.active = dup
    dec = dup.modifiers.new("Decimate", "DECIMATE")
    dec.ratio = ratio
    dec.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=dec.name)
    return dup


def make_collision_box(name, bounds_min, bounds_max):
    bmin = Vector(bounds_min)
    bmax = Vector(bounds_max)
    center = (bmin + bmax) * 0.5
    size = bmax - bmin
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=center)
    col = bpy.context.object
    col.name = f"COL_{name}"
    col.dimensions = (max(0.02, size.x), max(0.02, size.y), max(0.02, size.z))
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return col


def compute_bounds(objs):
    mins = Vector((1e9, 1e9, 1e9))
    maxs = Vector((-1e9, -1e9, -1e9))
    for o in objs:
        for corner in o.bound_box:
            w = o.matrix_world @ Vector(corner)
            mins.x, mins.y, mins.z = min(mins.x, w.x), min(mins.y, w.y), min(mins.z, w.z)
            maxs.x, maxs.y, maxs.z = max(maxs.x, w.x), max(maxs.y, w.y), max(maxs.z, w.z)
    return mins, maxs


def render_preview(name, objs, cam_dir=(1.45, -1.65, 0.92), resolution=(640, 480)):
    """Render a clean Cycles studio preview image to validate silhouette, shading and materials."""
    RENDER_DIR.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 24
    scene.cycles.use_denoising = False
    scene.render.resolution_x = resolution[0]
    scene.render.resolution_y = resolution[1]
    scene.render.image_settings.file_format = "PNG"
    scene.view_settings.view_transform = "AgX"

    world = bpy.data.worlds.new("StudioWorld")
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    bg.inputs["Color"].default_value = (0.045, 0.058, 0.056, 1.0)
    bg.inputs["Strength"].default_value = 0.65
    scene.world = world

    bmin, bmax = compute_bounds(objs)
    center = (bmin + bmax) * 0.5
    diag = max(0.25, (bmax - bmin).length)

    # Studio floor catcher (placed cleanly below camera if viewing ceiling fixture from underneath)
    floor_z = min(bmin.z - 0.002, (center + Vector(cam_dir).normalized() * (diag * 1.42)).z - diag * 0.6)
    bpy.ops.mesh.primitive_plane_add(size=diag * 6.0, location=(center.x, center.y, floor_z))
    floor = bpy.context.object
    fmat = bpy.data.materials.new("_PreviewFloor")
    fmat.use_nodes = True
    fmat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.06, 0.075, 0.072, 1.0)
    fmat.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.72
    floor.data.materials.append(fmat)

    # Key, fill, rim area lights
    def area_light(lname, offset, power, color, size):
        ldata = bpy.data.lights.new(lname, type="AREA")
        ldata.energy = power
        ldata.color = color
        ldata.size = size
        lobj = bpy.data.objects.new(lname, ldata)
        lobj.location = center + Vector(offset) * diag
        direction = center - lobj.location
        lobj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
        bpy.context.collection.objects.link(lobj)
        return lobj

    lights = [
        area_light("_Key", (1.2, -1.1, 1.4), diag * diag * 160.0, (1.0, 0.96, 0.90), diag * 0.9),
        area_light("_Fill", (-1.4, -0.8, 0.7), diag * diag * 55.0, (0.68, 0.84, 0.95), diag * 1.1),
        area_light("_Rim", (-0.6, 1.5, 1.1), diag * diag * 95.0, (0.72, 0.92, 0.86), diag * 0.7),
    ]

    cam_data = bpy.data.cameras.new("_Cam")
    cam_data.lens = 50.0
    cam_obj = bpy.data.objects.new("_Cam", cam_data)
    dvec = Vector(cam_dir).normalized()
    cam_obj.location = center + dvec * (diag * 1.42)
    cam_obj.rotation_euler = (center - cam_obj.location).to_track_quat("-Z", "Y").to_euler()
    bpy.context.collection.objects.link(cam_obj)
    scene.camera = cam_obj

    out_path = RENDER_DIR / f"{name}.png"
    scene.render.filepath = str(out_path)
    bpy.ops.render.render(write_still=True)

    # Clean up temporary studio helpers before saving .blend / FBX!
    for tmp in [floor, cam_obj, *lights]:
        bpy.data.objects.remove(tmp, do_unlink=True)
    bpy.data.worlds.remove(world, do_unlink=True)
    return out_path


def export_asset(name, build_fn, make_lods=True, render=True, cam_dir=(1.45, -1.65, 0.88), separate_parts=False):
    """Build an asset, optionally generate LOD1/LOD2 + Collision mesh, render preview,
    save editable .blend to ArtSource/Blender, and export game-ready FBX to Unity Assets."""
    BLEND_DIR.mkdir(parents=True, exist_ok=True)
    FBX_DIR.mkdir(parents=True, exist_ok=True)

    fresh_scene()
    built = build_fn()
    # built can be a list of objects or dict of named parts (when separate_parts=True, e.g. Fan Rotor vs Mount)
    export_objs = []
    preview_objs = []

    if separate_parts and isinstance(built, dict):
        for part_name, objs in built.items():
            joined = join_objects(objs, part_name)
            if joined:
                export_objs.append(joined)
                preview_objs.append(joined)
    else:
        objs = built if isinstance(built, (list, tuple)) else [built]
        lod0 = join_objects(objs, f"{name}_LOD0" if make_lods else name)
        export_objs.append(lod0)
        preview_objs.append(lod0)
        tri_count = sum(len(p.vertices) - 2 for p in lod0.data.polygons)
        if make_lods and tri_count > 450:
            lod1 = make_lod_copy(lod0, f"{name}_LOD1", 0.48)
            lod2 = make_lod_copy(lod0, f"{name}_LOD2", 0.22)
            export_objs.extend([lod1, lod2])

    bmin, bmax = compute_bounds(preview_objs)
    col_mesh = make_collision_box(name, bmin, bmax)

    # Render Cycles visual validation preview on LOD0 only
    if render:
        for o in export_objs:
            o.hide_render = o not in preview_objs
        col_mesh.hide_render = True
        render_preview(name, preview_objs, cam_dir=cam_dir)
        for o in export_objs:
            o.hide_render = False
        col_mesh.hide_render = False

    # Organize editable .blend into clean collections
    col_coll = bpy.data.collections.new("Collision")
    bpy.context.scene.collection.children.link(col_coll)
    bpy.context.scene.collection.objects.unlink(col_mesh)
    col_coll.objects.link(col_mesh)

    # Save editable .blend source
    blend_path = BLEND_DIR / f"{name}.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))

    # Export clean Unity FBX (only render/LOD meshes, collision kept in .blend & Unity box proxies)
    bpy.ops.object.select_all(action="DESELECT")
    for o in export_objs:
        o.select_set(True)
    fbx_path = FBX_DIR / f"{name}.fbx"
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
    lod0_tris = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in preview_objs)
    dims = bmax - bmin
    print(f"[BLENDER ASSET] {name}: LOD0={lod0_tris} tris | size=({dims.x:.2f}x{dims.y:.2f}x{dims.z:.2f}m) -> {fbx_path.name}")
    return lod0_tris


def export_character(name, arm_obj, mesh_objs, render=True, cam_dir=(0.35, -1.85, 0.22), closeup_cam=None):
    """Save editable rigged .blend and export Unity-ready skinned FBX with Armature, Shape Keys & Actions intact.
    CRITICAL (R49): use_mesh_modifiers=False preserves all facial Shape Keys (BlendShapes) on export!"""
    BLEND_DIR.mkdir(parents=True, exist_ok=True)
    FBX_DIR.mkdir(parents=True, exist_ok=True)

    if render:
        render_preview(name, mesh_objs, cam_dir=cam_dir, resolution=(720, 900))
        if closeup_cam is not None:
            # Render close-up face/bust inspection portrait
            RENDER_DIR.mkdir(parents=True, exist_ok=True)
            scene = bpy.context.scene
            scene.render.engine = "CYCLES"
            scene.cycles.device = "CPU"
            scene.cycles.samples = 32
            scene.cycles.use_denoising = False
            scene.render.resolution_x = 640
            scene.render.resolution_y = 640
            scene.view_settings.view_transform = "AgX"
            world = bpy.data.worlds.new("PortraitWorld")
            world.use_nodes = True
            bg = world.node_tree.nodes.get("Background")
            bg.inputs["Color"].default_value = (0.045, 0.058, 0.056, 1.0)
            bg.inputs["Strength"].default_value = 0.65
            scene.world = world
            target_pt = Vector(closeup_cam["target"])
            cam_pt = Vector(closeup_cam["pos"])
            def _al(lname, pos, energy, color, sz):
                ld = bpy.data.lights.new(lname, type="AREA")
                ld.energy = energy
                ld.color = color
                ld.size = sz
                lo = bpy.data.objects.new(lname, ld)
                lo.location = pos
                lo.rotation_euler = (target_pt - lo.location).to_track_quat("-Z", "Y").to_euler()
                bpy.context.collection.objects.link(lo)
                return lo
            lights = [
                _al("_PKey", target_pt + Vector((0.65, -0.75, 0.45)), 55.0, (1.0, 0.95, 0.88), 0.7),
                _al("_PFill", target_pt + Vector((-0.75, -0.55, 0.15)), 20.0, (0.70, 0.85, 0.95), 0.8),
                _al("_PRim", target_pt + Vector((-0.25, 0.70, 0.35)), 38.0, (0.78, 0.94, 0.88), 0.5),
            ]
            cd = bpy.data.cameras.new("_PCam")
            cd.lens = 72.0
            co = bpy.data.objects.new("_PCam", cd)
            co.location = cam_pt
            co.rotation_euler = (target_pt - cam_pt).to_track_quat("-Z", "Y").to_euler()
            bpy.context.collection.objects.link(co)
            scene.camera = co
            scene.render.filepath = str(RENDER_DIR / f"{name}_Portrait.png")
            bpy.ops.render.render(write_still=True)
            for tmp in [co, *lights]:
                bpy.data.objects.remove(tmp, do_unlink=True)
            bpy.data.worlds.remove(world, do_unlink=True)

    blend_path = BLEND_DIR / f"{name}.blend"
    for source in bpy.data.images:
        if source.source == "FILE" and Path(source.filepath).is_absolute():
            source.filepath = "//" + Path(os.path.relpath(source.filepath, BLEND_DIR)).as_posix()
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path), relative_remap=False)

    bpy.ops.object.select_all(action="DESELECT")
    arm_obj.select_set(True)
    for m in mesh_objs:
        m.select_set(True)
    bpy.context.view_layer.objects.active = arm_obj

    fbx_path = FBX_DIR / f"{name}.fbx"
    bpy.ops.export_scene.fbx(
        filepath=str(fbx_path),
        use_selection=True,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        object_types={"ARMATURE", "MESH"},
        mesh_smooth_type="FACE",
        use_mesh_modifiers=False,
        use_tspace=True,
        primary_bone_axis="Y",
        secondary_bone_axis="X",
        armature_nodetype="NULL",
        use_armature_deform_only=True,
        add_leaf_bones=False,
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=True,
        bake_anim_force_startend_keying=True,
        bake_anim_step=1.0,
        embed_textures=False,
    )
    tris = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in mesh_objs)
    sk_count = sum(len(o.data.shape_keys.key_blocks) - 1 for o in mesh_objs if o.data.shape_keys)
    act_count = len(bpy.data.actions)
    print(f"[BLENDER CHARACTER] {name}: {tris} tris | {len(arm_obj.data.bones)} bones | {sk_count} shape keys | {act_count} actions -> {fbx_path.name}")
    return tris
