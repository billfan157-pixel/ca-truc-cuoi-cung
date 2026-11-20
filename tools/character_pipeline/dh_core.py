"""Shared Blender 4.5 + MPFB 2.0.18 Digital Human Production Core (M2-C).

Provides:
- Anatomically credible CC0 human creation via MPFB2 (`hm08` base topology + metric height calibration)
- CC0 eyes, teeth, tongue, eyebrows, and eyelashes loading & fitting
- Full 28-shape anatomical FACS + Vietnamese phonetic viseme blendshape suite
  with barycentric projection onto eyelashes & eyebrows (zero floating lashes on blink!)
- Scalp-surface-projected multi-layer Bezier Hair Card grooming mapped to 2048^2 Cycles hair atlas
- Tailored body-fitted garment construction + real Blender Cloth Physics simulation (`CLOTH` + `COLLISION`)
- Smooth 4-bone-limited skin weight transfer (`DATA_TRANSFER` + smooth) & hidden skin culling
- Realistic Cycles PBR materials (SSS skin, wet cornea eye, alpha-card hair, woven cloth)
- LOD0 / LOD1 / LOD2 generation + clean Unity 6.3 FBX export preserving Armature, BlendShapes & Actions
"""

import gzip
import math
import os
from pathlib import Path

import addon_utils
import bmesh
import bpy
from mathutils import Euler, Matrix, Quaternion, Vector
from mathutils.bvhtree import BVHTree

addon_utils.enable("bl_ext.blender_org.mpfb", default_set=True)

ROOT = Path(__file__).resolve().parents[2]
BLEND_DIR = ROOT / "ArtSource" / "Blender"
FBX_DIR = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Meshes"
TEX_DIR = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Textures"
EVIDENCE_DIR = ROOT / "output" / "m2c"


def fresh_metric_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    addon_utils.enable("bl_ext.blender_org.mpfb", default_set=True)
    from bl_ext.blender_org.mpfb.services.assetservice import AssetService

    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene.render.fps = 30
    AssetService.update_all_asset_lists()
    return scene


def measure_body_bounds(human_obj):
    bvg = human_obj.vertex_groups.get("body")
    if bvg is not None:
        idx = bvg.index
        pts = [human_obj.matrix_world @ v.co for v in human_obj.data.vertices if any(g.group == idx for g in v.groups)]
    else:
        pts = [human_obj.matrix_world @ v.co for v in human_obj.data.vertices]
    mins = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    maxs = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return mins, maxs


def create_calibrated_human(macro_dict, target_height_m, facial_targets=None):
    """Create an MPFB2 hm08 human, apply anatomical facial morphs, bake targets, and calibrate exact metric height."""
    from bl_ext.blender_org.mpfb.services.humanservice import HumanService
    from bl_ext.blender_org.mpfb.services.targetservice import TargetService

    human = HumanService.create_human(
        mask_helpers=True,
        detailed_helpers=True,
        extra_vertex_groups=True,
        feet_on_ground=True,
        scale=0.1,
        macro_detail_dict=macro_dict,
    )
    if facial_targets:
        for tname, weight in facial_targets.items():
            tpath = TargetService.target_full_path(tname)
            if tpath:
                TargetService.load_target(human, tpath, weight=weight, name=tname)

    TargetService.bake_targets(human)

    # Scale uniformly to exact canonical metric height and plant soles strictly on z = 0.0
    bmin, bmax = measure_body_bounds(human)
    cur_h = max(0.2, bmax.z - bmin.z)
    scale_corr = target_height_m / cur_h
    for v in human.data.vertices:
        v.co = Vector((v.co.x * scale_corr, v.co.y * scale_corr, (v.co.z - bmin.z) * scale_corr))
    human.data.update()
    bmin2, bmax2 = measure_body_bounds(human)
    print(f"[DH_CORE] Calibrated hm08 base: target={target_height_m:.3f}m actual={bmax2.z - bmin2.z:.3f}m (z=[{bmin2.z:.4f},{bmax2.z:.4f}])")
    return human


def load_target_deltas(target_name, scale_corr=1.0):
    """Parse an MPFB2 .target.gz file directly into {vert_index: Vector(dx, dy, dz)} in Blender meters."""
    from bl_ext.blender_org.mpfb.services.targetservice import TargetService

    tpath = TargetService.target_full_path(target_name)
    if not tpath:
        print(f"[WARN] Missing MPFB target: {target_name}")
        return {}
    opener = gzip.open if str(tpath).endswith(".gz") else open
    deltas = {}
    with opener(tpath, "rt", encoding="utf-8", errors="ignore") as f:
        for line in f:
            line = line.strip()
            if not line or line.startswith("#"):
                continue
            parts = line.split()
            if len(parts) >= 4:
                try:
                    vi = int(parts[0])
                    # MakeHuman target axes: (x, y_up, z_fwd) in decimeters -> Blender (x, -z_fwd, y_up) in meters (*0.1)
                    dx = float(parts[1]) * 0.1 * scale_corr
                    dy = -float(parts[3]) * 0.1 * scale_corr
                    dz = float(parts[2]) * 0.1 * scale_corr
                    deltas[vi] = Vector((dx, dy, dz))
                except ValueError:
                    continue
    return deltas


def build_anatomical_blendshapes(human_obj, scale_corr=1.0):
    """Build 28+ anatomical FACS + Vietnamese viseme Shape Keys directly from MPFB2 expression targets.

    Preserves all 9 legacy M2-A QA names (Blink_L/R, Viseme_AA/OH/EE/MM, Brow_Concern, Mouth_Warm, Mouth_Sorrow)
    plus rich Vietnamese phonetic visemes (PP, FF, TH, CH, IH, OO, UH) and expressive facial shapes.
    """
    mesh = human_obj.data
    if not mesh.shape_keys:
        human_obj.shape_key_add(name="Basis", from_mix=False)
    basis = mesh.shape_keys.key_blocks["Basis"]

    # Cache raw MPFB expression target deltas
    needed_targets = [
        "eye-left-closure", "eye-right-closure",
        "eye-left-opened-up", "eye-right-opened-up",
        "eye-left-slit", "eye-right-slit",
        "eyebrows-left-inner-up", "eyebrows-right-inner-up",
        "eyebrows-left-extern-up", "eyebrows-right-extern-up",
        "eyebrows-left-down", "eyebrows-right-down",
        "eyebrows-left-up", "eyebrows-right-up",
        "mouth-open", "mouth-pursing", "mouth-protusion", "mouth-retraction",
        "mouth-corner-puller", "mouth-depression", "mouth-depression-retraction",
        "mouth-compression", "mouth-elevation", "mouth-eversion",
        "mouth-upward-retraction", "mouth-part-later",
        "nose-left-elevation", "nose-right-elevation", "nose-compression",
    ]
    raw = {t: load_target_deltas(t, scale_corr=scale_corr) for t in needed_targets}

    # Combine anatomical expression targets into clean, game-ready FACS & Vietnamese Viseme shape keys
    recipes = [
        # --- Legacy M2-A required 9 names (100% QA compatible) ---
        ("Blink_L", [("eye-left-closure", 1.18)]),
        ("Blink_R", [("eye-right-closure", 1.18)]),
        ("Viseme_AA", [("mouth-open", 0.72), ("mouth-part-later", 0.25)]),
        ("Viseme_OH", [("mouth-open", 0.44), ("mouth-pursing", 0.68), ("mouth-protusion", 0.48)]),
        ("Viseme_EE", [("mouth-open", 0.22), ("mouth-retraction", 0.76), ("mouth-corner-puller", 0.28)]),
        ("Viseme_MM", [("mouth-compression", 0.58)]),
        ("Brow_Concern", [("eyebrows-left-inner-up", 0.90), ("eyebrows-right-inner-up", 0.90),
                          ("eyebrows-left-down", 0.18), ("eyebrows-right-down", 0.18)]),
        ("Mouth_Warm", [("mouth-corner-puller", 0.64), ("mouth-upward-retraction", 0.36),
                        ("eye-left-slit", 0.20), ("eye-right-slit", 0.20)]),
        ("Mouth_Sorrow", [("mouth-depression", 0.62), ("mouth-depression-retraction", 0.38),
                          ("eyebrows-left-inner-up", 0.32), ("eyebrows-right-inner-up", 0.32)]),

        # --- Full Vietnamese phonetic viseme suite ---
        ("Viseme_PP", [("mouth-compression", 0.98)]),
        ("Viseme_FF", [("mouth-compression", 0.42), ("mouth-depression", 0.36), ("mouth-retraction", 0.22)]),
        ("Viseme_TH", [("mouth-open", 0.22), ("mouth-retraction", 0.26), ("mouth-elevation", 0.20)]),
        ("Viseme_CH", [("mouth-open", 0.18), ("mouth-protusion", 0.42), ("mouth-eversion", 0.34)]),
        ("Viseme_IH", [("mouth-open", 0.16), ("mouth-retraction", 0.50)]),
        ("Viseme_OO", [("mouth-open", 0.18), ("mouth-pursing", 0.98), ("mouth-protusion", 0.78)]),
        ("Viseme_UH", [("mouth-open", 0.34), ("mouth-pursing", 0.22)]),
        ("Jaw_Open", [("mouth-open", 0.86)]),

        # --- Expressive FACS micro-performance shapes ---
        ("Eye_Wide_L", [("eye-left-opened-up", 0.85)]),
        ("Eye_Wide_R", [("eye-right-opened-up", 0.85)]),
        ("Eye_Squint_L", [("eye-left-slit", 0.78)]),
        ("Eye_Squint_R", [("eye-right-slit", 0.78)]),
        ("Brow_InnerUp", [("eyebrows-left-inner-up", 0.95), ("eyebrows-right-inner-up", 0.95)]),
        ("Brow_OuterUp_L", [("eyebrows-left-extern-up", 0.92)]),
        ("Brow_OuterUp_R", [("eyebrows-right-extern-up", 0.92)]),
        ("Brow_Down_L", [("eyebrows-left-down", 0.92)]),
        ("Brow_Down_R", [("eyebrows-right-down", 0.92)]),
        ("Mouth_Smile_L", [("mouth-corner-puller", 0.65), ("mouth-upward-retraction", 0.35)]),
        ("Mouth_Smile_R", [("mouth-corner-puller", 0.65), ("mouth-upward-retraction", 0.35)]),
        ("Nose_Wrinkle", [("nose-left-elevation", 0.65), ("nose-right-elevation", 0.65), ("nose-compression", 0.35)]),
    ]

    n_verts = len(mesh.vertices)
    for kname, terms in recipes:
        sk = human_obj.shape_key_add(name=kname, from_mix=False)
        # Copy basis first
        for i in range(n_verts):
            sk.data[i].co = basis.data[i].co.copy()
        # Accumulate weighted target deltas
        acc = {}
        for tname, wt in terms:
            for vi, dvec in raw.get(tname, {}).items():
                if vi >= n_verts:
                    continue
                # Split left/right unilaterally for Mouth_Smile_L (+X) / Mouth_Smile_R (-X)
                if kname == "Mouth_Smile_L":
                    x = basis.data[vi].co.x
                    side_w = max(0.0, min(1.0, (x + 0.008) / 0.016))
                elif kname == "Mouth_Smile_R":
                    x = basis.data[vi].co.x
                    side_w = max(0.0, min(1.0, (-x + 0.008) / 0.016))
                else:
                    side_w = 1.0
                if side_w <= 1e-4:
                    continue
                if vi in acc:
                    acc[vi] = acc[vi] + dvec * (wt * side_w)
                else:
                    acc[vi] = dvec * (wt * side_w)
        for vi, dvec in acc.items():
            sk.data[vi].co = basis.data[vi].co + dvec

    print(f"[DH_CORE] Built {len(recipes)} anatomical facial BlendShapes on {human_obj.name}")
    return [r[0] for r in recipes]


def transfer_shapekeys_by_surface(src_obj, dst_obj, query_y_offset=0.0, body_only=True):
    """Project all shape keys from src_obj (hm08 face) onto dst_obj (eyelashes/eyebrows)
    via nearest-face barycentric interpolation so lashes & brows deform seamlessly during blinks & speech!"""
    if not src_obj.data.shape_keys:
        return
    src_mesh = src_obj.data
    dst_mesh = dst_obj.data
    if not dst_mesh.shape_keys:
        dst_obj.shape_key_add(name="Basis", from_mix=False)

    # Build BVH over triangulated Basis mesh of src_obj (only body vertices!)
    bm = bmesh.new()
    bm.from_mesh(src_mesh)
    bvg = src_obj.vertex_groups.get("body") if body_only else None
    if bvg is not None:
        # Remove helper vertices from BVH so lashes bind strictly to actual eyelid skin!
        deform_layer = bm.verts.layers.deform.active
        if deform_layer is not None:
            to_del = [v for v in bm.verts if bvg.index not in v[deform_layer]]
            bmesh.ops.delete(bm, geom=to_del, context="VERTS")
    # Store original vertex index in an int layer before triangulation!
    bm.verts.ensure_lookup_table()
    orig_idx_map = {}
    # Match surviving verts back to src_mesh vertex index by coordinate hash
    coord_to_idx = {}
    for i, v in enumerate(src_mesh.vertices):
        key = (round(v.co.x, 5), round(v.co.y, 5), round(v.co.z, 5))
        coord_to_idx[key] = i
    for v in bm.verts:
        key = (round(v.co.x, 5), round(v.co.y, 5), round(v.co.z, 5))
        orig_idx_map[v.index] = coord_to_idx.get(key, 0)

    bmesh.ops.triangulate(bm, faces=bm.faces)
    bm.verts.ensure_lookup_table()
    bm.faces.ensure_lookup_table()
    bvh = BVHTree.FromBMesh(bm)

    # Precompute nearest triangle + barycentric weights for every vertex of dst_obj
    # Optional inward +Y query offset locks protruding eyelash tips strictly to their eyelid root margin!
    bindings = []
    for dv in dst_mesh.vertices:
        q = Vector((dv.co.x, dv.co.y + query_y_offset, dv.co.z)) if query_y_offset else dv.co
        loc, _norm, f_idx, _dist = bvh.find_nearest(q)
        if f_idx is None:
            bindings.append(None)
            continue
        face = bm.faces[f_idx]
        v0, v1, v2 = face.verts[0], face.verts[1], face.verts[2]
        # Compute barycentric weights on triangle (v0, v1, v2)
        p0, p1, p2 = v0.co, v1.co, v2.co
        e0, e1, ep = p1 - p0, p2 - p0, loc - p0
        d00, d01, d11 = e0.dot(e0), e0.dot(e1), e1.dot(e1)
        d20, d21 = ep.dot(e0), ep.dot(e1)
        denom = d00 * d11 - d01 * d01
        if abs(denom) < 1e-10:
            w0, w1, w2 = 1.0, 0.0, 0.0
        else:
            w1 = max(0.0, min(1.0, (d11 * d20 - d01 * d21) / denom))
            w2 = max(0.0, min(1.0, (d00 * d21 - d01 * d20) / denom))
            w0 = max(0.0, 1.0 - w1 - w2)
            s = w0 + w1 + w2
            w0, w1, w2 = w0 / s, w1 / s, w2 / s
        bindings.append((
            (orig_idx_map[v0.index], w0),
            (orig_idx_map[v1.index], w1),
            (orig_idx_map[v2.index], w2),
        ))
    bm.free()

    src_basis = src_mesh.shape_keys.key_blocks["Basis"].data
    dst_basis = dst_mesh.shape_keys.key_blocks["Basis"].data

    for kb in src_mesh.shape_keys.key_blocks:
        if kb.name == "Basis":
            continue
        dsk = dst_obj.shape_key_add(name=kb.name, from_mix=False)
        sdata = kb.data
        for i, bnd in enumerate(bindings):
            if bnd is None:
                dsk.data[i].co = dst_basis[i].co.copy()
                continue
            delta = Vector((0.0, 0.0, 0.0))
            for src_vi, w in bnd:
                delta += (sdata[src_vi].co - src_basis[src_vi].co) * w
            dsk.data[i].co = dst_basis[i].co + delta

    print(f"[DH_CORE] Transferred {len(src_mesh.shape_keys.key_blocks)-1} shape keys -> {dst_obj.name}")


def strip_helper_geometry_preserving_shapekeys(human_obj, keep_z_ranges=None, extra_keep_fn=None):
    """Remove MPFB helper geometry & optionally occluded body polygons while preserving ALL Shape Keys cleanly!"""
    # Note: Deleting geometry in Edit Mode preserves all existing Shape Keys intact in Blender 4.5!
    for mod in list(human_obj.modifiers):
        if mod.type == "MASK":
            human_obj.modifiers.remove(mod)

    bvg = human_obj.vertex_groups.get("body")
    body_idx = bvg.index if bvg else -1

    bpy.ops.object.select_all(action="DESELECT")
    human_obj.select_set(True)
    bpy.context.view_layer.objects.active = human_obj
    bpy.ops.object.mode_set(mode="EDIT")
    bm = bmesh.from_edit_mesh(human_obj.data)
    deform_layer = bm.verts.layers.deform.active

    to_delete = []
    for v in bm.verts:
        in_body = (body_idx < 0) or (deform_layer is not None and body_idx in v[deform_layer])
        if not in_body:
            to_delete.append(v)
            continue
        if extra_keep_fn is not None and not extra_keep_fn(v.co):
            to_delete.append(v)

    bmesh.ops.delete(bm, geom=to_delete, context="VERTS")
    bmesh.update_edit_mesh(human_obj.data)
    bpy.ops.object.mode_set(mode="OBJECT")
    print(f"[DH_CORE] Cleaned {human_obj.name}: remaining verts={len(human_obj.data.vertices)} polys={len(human_obj.data.polygons)} shapes={len(human_obj.data.shape_keys.key_blocks) if human_obj.data.shape_keys else 0}")


def create_pbr_material(name, color_tex=None, normal_tex=None, mask_tex=None,
                        base_color=(0.8, 0.8, 0.8, 1.0), roughness=0.5, metallic=0.0,
                        sss_weight=0.0, coat_weight=0.0, alpha_mode=None, normal_strength=0.85,
                        uv_tile=1.0):
    """Build a realistic Blender 4.5 Principled BSDF material with exact Unity material slot name."""
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()

    out = nodes.new("ShaderNodeOutputMaterial")
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.inputs["Base Color"].default_value = base_color
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    if sss_weight > 0.0:
        bsdf.inputs["Subsurface Weight"].default_value = sss_weight
        bsdf.inputs["Subsurface Scale"].default_value = 0.012
        bsdf.inputs["Subsurface Radius"].default_value = (1.0, 0.35, 0.20)
    if coat_weight > 0.0:
        bsdf.inputs["Coat Weight"].default_value = coat_weight
        bsdf.inputs["Coat Roughness"].default_value = 0.04
        bsdf.inputs["Coat IOR"].default_value = 1.38
    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])

    tex_coord = None
    mapping = None
    if uv_tile != 1.0:
        tex_coord = nodes.new("ShaderNodeTexCoord")
        mapping = nodes.new("ShaderNodeMapping")
        mapping.inputs["Scale"].default_value = (uv_tile, uv_tile, 1.0)
        links.new(tex_coord.outputs["UV"], mapping.inputs["Vector"])

    def load_img(fname, non_color=False):
        p = TEX_DIR / fname
        if not p.exists():
            return None
        img = bpy.data.images.load(str(p), check_existing=True)
        if non_color:
            img.colorspace_settings.name = "Non-Color"
        return img

    if color_tex:
        img = load_img(color_tex, non_color=False)
        if img:
            t = nodes.new("ShaderNodeTexImage")
            t.image = img
            if mapping:
                links.new(mapping.outputs["Vector"], t.inputs["Vector"])
            links.new(t.outputs["Color"], bsdf.inputs["Base Color"])
            if alpha_mode in ("CLIP", "BLEND"):
                links.new(t.outputs["Alpha"], bsdf.inputs["Alpha"])

    if normal_tex:
        img = load_img(normal_tex, non_color=True)
        if img:
            t = nodes.new("ShaderNodeTexImage")
            t.image = img
            if mapping:
                links.new(mapping.outputs["Vector"], t.inputs["Vector"])
            nm = nodes.new("ShaderNodeNormalMap")
            nm.inputs["Strength"].default_value = normal_strength
            links.new(t.outputs["Color"], nm.inputs["Color"])
            links.new(nm.outputs["Normal"], bsdf.inputs["Normal"])

    if mask_tex:
        img = load_img(mask_tex, non_color=True)
        if img:
            t = nodes.new("ShaderNodeTexImage")
            t.image = img
            if mapping:
                links.new(mapping.outputs["Vector"], t.inputs["Vector"])
            # Mask A is Unity Smoothness -> Blender Roughness = 1 - Smoothness
            inv = nodes.new("ShaderNodeMath")
            inv.operation = "SUBTRACT"
            inv.inputs[0].default_value = 1.0
            links.new(t.outputs["Alpha"], inv.inputs[1])
            links.new(inv.outputs["Value"], bsdf.inputs["Roughness"])

    return mat


def assign_material(obj, mat):
    obj.data.materials.clear()
    obj.data.materials.append(mat)


def set_smooth(obj):
    for p in obj.data.polygons:
        p.use_smooth = True


def build_hair_card_strip(bm, uv_layer, points, normals, width_root, width_tip, u_min, u_max,
                          arch=0.0022, v_root=0.98, v_tip=0.02):
    """Add one multi-segment gently arched hair card ribbon with proper root-to-tip UVs into bmesh."""
    n = len(points)
    if n < 2:
        return
    # 3 vertices across width (left, center-arched, right) so cards catch rich cylindrical light!
    rows = []
    for i in range(n):
        t = i / float(n - 1)
        p = Vector(points[i])
        nrm = Vector(normals[i]).normalized()
        if i == 0:
            tang = (Vector(points[1]) - p).normalized()
        elif i == n - 1:
            tang = (p - Vector(points[-2])).normalized()
        else:
            tang = (Vector(points[i + 1]) - Vector(points[i - 1])).normalized()
        side = tang.cross(nrm)
        if side.length < 1e-5:
            side = Vector((1, 0, 0))
        else:
            side.normalize()
        # Smooth width profile (tapers gently at both ends)
        w = (width_root * (1.0 - t) + width_tip * t) * (0.78 + 0.22 * math.sin(t * math.pi))
        v_left = bm.verts.new(p - side * (w * 0.5))
        v_mid = bm.verts.new(p + nrm * arch)
        v_right = bm.verts.new(p + side * (w * 0.5))
        rows.append((v_left, v_mid, v_right, v_root + (v_tip - v_root) * t))

    u_mid = 0.5 * (u_min + u_max)
    for i in range(n - 1):
        l0, m0, r0, tv0 = rows[i]
        l1, m1, r1, tv1 = rows[i + 1]
        for quad, uvs in (
            ((l0, m0, m1, l1), ((u_min, tv0), (u_mid, tv0), (u_mid, tv1), (u_min, tv1))),
            ((m0, r0, r1, m1), ((u_mid, tv0), (u_max, tv0), (u_max, tv1), (u_mid, tv1))),
        ):
            try:
                f = bm.faces.new(quad)
                f.smooth = True
                for loop, uv in zip(f.loops, uvs):
                    loop[uv_layer].uv = uv
            except ValueError:
                pass


def project_to_surface(bvh, pt, offset_m=0.003):
    """Snap a 3D point onto the head/body BVH surface with outward normal offset."""
    loc, nrm, _idx, _dist = bvh.find_nearest(Vector(pt))
    if loc is None:
        return Vector(pt), Vector((0, 0, 1))
    return loc + nrm.normalized() * offset_m, nrm.normalized()


def run_cloth_simulation(garment_obj, body_collider_obj, pin_vgroup_name, frames=30,
                         mass=0.28, tension=22.0, bending=1.6, air_damping=1.8):
    """Run real Blender 4.5 Cloth Physics Simulation of garment_obj draping over body_collider_obj."""
    scene = bpy.context.scene
    # Ensure body has Collision modifier
    col_mod = body_collider_obj.modifiers.get("BodyCollision")
    if col_mod is None:
        col_mod = body_collider_obj.modifiers.new("BodyCollision", "COLLISION")
    body_collider_obj.collision.thickness_outer = 0.0035
    body_collider_obj.collision.thickness_inner = 0.015
    body_collider_obj.collision.cloth_friction = 14.0

    # Add Cloth modifier on garment
    cloth_mod = garment_obj.modifiers.new("ClothSim", "CLOTH")
    s = cloth_mod.settings
    s.quality = 6
    s.mass = mass
    s.air_damping = air_damping
    s.tension_stiffness = tension
    s.compression_stiffness = tension * 0.85
    s.shear_stiffness = tension * 0.55
    s.bending_stiffness = bending
    s.tension_damping = 6.0
    s.bending_damping = 0.8
    if pin_vgroup_name and pin_vgroup_name in garment_obj.vertex_groups:
        s.vertex_group_mass = pin_vgroup_name
        s.pin_stiffness = 1.0

    cs = cloth_mod.collision_settings
    cs.use_collision = True
    cs.distance_min = 0.0035
    cs.friction = 12.0
    cs.use_self_collision = False  # keeps draping fast and tangle-free in batch mode

    scene.frame_start = 1
    scene.frame_end = frames + 2
    for f in range(1, frames + 1):
        scene.frame_set(f)

    # Bake/apply the physically draped cloth geometry cleanly into editable mesh!
    depsgraph = bpy.context.evaluated_depsgraph_get()
    eval_obj = garment_obj.evaluated_get(depsgraph)
    eval_mesh = eval_obj.to_mesh()
    for i, v in enumerate(garment_obj.data.vertices):
        v.co = eval_mesh.vertices[i].co.copy()
    eval_obj.to_mesh_clear()
    garment_obj.modifiers.remove(cloth_mod)
    garment_obj.data.update()
    scene.frame_set(1)
    print(f"[CLOTH SIM] Draped {garment_obj.name} over {body_collider_obj.name} for {frames} frames (verts={len(garment_obj.data.vertices)})")


def transfer_skin_weights(src_body, dst_mesh, rig_obj, smooth_iters=3, smooth_factor=0.45):
    """Transfer anatomical skin weights from rigged body onto garments/accessories and enforce 4-bone Unity limit."""
    dst_mesh.vertex_groups.clear()
    bpy.ops.object.select_all(action="DESELECT")
    dst_mesh.select_set(True)
    bpy.context.view_layer.objects.active = dst_mesh

    dt = dst_mesh.modifiers.new("WeightTransfer", "DATA_TRANSFER")
    dt.object = src_body
    dt.use_vert_data = True
    dt.data_types_verts = {"VGROUP_WEIGHTS"}
    dt.vert_mapping = "POLYINTERP_NEAREST"
    bpy.ops.object.datalayout_transfer(modifier=dt.name)
    bpy.ops.object.modifier_apply(modifier=dt.name)

    # Clean out helper/non-bone vertex groups and smooth transitions
    bone_names = set(b.name for b in rig_obj.data.bones)
    for vg in list(dst_mesh.vertex_groups):
        if vg.name not in bone_names:
            dst_mesh.vertex_groups.remove(vg)

    if smooth_iters > 0 and len(dst_mesh.vertex_groups) > 0:
        try:
            bpy.ops.object.mode_set(mode="WEIGHT_PAINT")
            bpy.ops.object.vertex_group_smooth(group_select_mode="ALL", factor=smooth_factor, repeat=smooth_iters)
            bpy.ops.object.vertex_group_limit_total(group_select_mode="ALL", limit=4)
            bpy.ops.object.vertex_group_normalize_all(group_select_mode="ALL", lock_active=False)
            bpy.ops.object.mode_set(mode="OBJECT")
        except Exception as e:
            bpy.ops.object.mode_set(mode="OBJECT")
            print(f"[WARN] Weight smooth fallback on {dst_mesh.name}: {e}")

    bind_rig(dst_mesh, rig_obj)


def weight_rigid(mesh_obj, rig_obj, bone_name):
    mesh_obj.vertex_groups.clear()
    vg = mesh_obj.vertex_groups.new(name=bone_name)
    vg.add(list(range(len(mesh_obj.data.vertices))), 1.0, "REPLACE")
    bind_rig(mesh_obj, rig_obj)


def strip_outer_cornea_shell(eye_obj):
    """Remove MakeHuman high-poly outer opaque cornea overlay polygons (UV u>0.78, v<0.28)
    so the photorealistic iris, pupil and scleral capillaries are directly exposed to Cycles Coat
    and Unity URP CTCC/DigitalHumanEye parallax refraction."""
    bm = bmesh.new()
    bm.from_mesh(eye_obj.data)
    uv_layer = bm.loops.layers.uv.active
    if uv_layer:
        to_del = []
        for f in bm.faces:
            us = [l[uv_layer].uv.x for l in f.loops]
            vs = [l[uv_layer].uv.y for l in f.loops]
            if (sum(us) / len(us)) > 0.78 and (sum(vs) / len(vs)) < 0.28:
                to_del.append(f)
        if to_del:
            bmesh.ops.delete(bm, geom=to_del, context="FACES")
            bm.to_mesh(eye_obj.data)
            eye_obj.data.update()
    bm.free()


def triangulate_ngons(mesh_obj):
    """Triangulate ONLY polygons with >4 vertices so FBX MikkTSpace tangent export succeeds with zero warnings."""
    if not mesh_obj.data.polygons or not any(len(p.vertices) > 4 for p in mesh_obj.data.polygons):
        return
    if mesh_obj.data.shape_keys:
        return
    bm = bmesh.new()
    bm.from_mesh(mesh_obj.data)
    ngons = [f for f in bm.faces if len(f.verts) > 4]
    if ngons:
        bmesh.ops.triangulate(bm, faces=ngons, quad_method="BEAUTY", ngon_method="BEAUTY")
        bm.to_mesh(mesh_obj.data)
        mesh_obj.data.update()
    bm.free()


def extract_garment_shell_from_body(body_obj, name, face_predicate, offset_m=0.009,
                                    smooth_iters=8, subdiv_levels=0, flare_fn=None, uv_scale=2.4):
    """Extract a watertight, continuous-shoulder all-quad garment base shell directly from body_obj's
    anatomical quad topology, inflate outward with optional skirt/sleeve flare, and assign clean cloth UVs."""
    dup = body_obj.copy()
    dup.data = body_obj.data.copy()
    dup.name = name
    dup.vertex_groups.clear()
    dup.modifiers.clear()
    if dup.data.shape_keys:
        dup.shape_key_clear()
    bpy.context.collection.objects.link(dup)

    bm = bmesh.new()
    bm.from_mesh(dup.data)
    bm.faces.ensure_lookup_table()

    del_faces = [f for f in bm.faces if not face_predicate(f.calc_center_median(), f)]
    if del_faces:
        bmesh.ops.delete(bm, geom=del_faces, context="FACES")

    # Remove stray isolated vertices/edges
    loose_v = [v for v in bm.verts if not v.link_faces]
    if loose_v:
        bmesh.ops.delete(bm, geom=loose_v, context="VERTS")

    bm.normal_update()
    # Initial anatomical inflation outward + custom tailoring flare (hip ease, sleeve looseness, pant drape)
    for v in bm.verts:
        extra = flare_fn(v.co, v.normal) if flare_fn else Vector((0.0, 0.0, 0.0))
        v.co = v.co + v.normal * offset_m + extra

    # Smooth tailoring silhouette (removes body muscle/rib indentation so garment drapes like real fabric!)
    if smooth_iters > 0:
        bmesh.ops.smooth_vert(bm, verts=list(bm.verts), factor=0.45, use_axis_x=True, use_axis_y=True, use_axis_z=True)
        for _ in range(smooth_iters - 1):
            bmesh.ops.smooth_vert(bm, verts=list(bm.verts), factor=0.35, use_axis_x=True, use_axis_y=True, use_axis_z=True)

    # Clean cylindrical + planar cloth UVs so cotton weave & twill run straight vertically!
    uv_layer = bm.loops.layers.uv.active or bm.loops.layers.uv.new("UVMap")
    for f in bm.faces:
        f.smooth = True
        for l in f.loops:
            co = l.vert.co
            ang = math.atan2(co.x, -co.y)
            u = (ang / math.tau + 0.5) * uv_scale
            v_uv = co.z * uv_scale
            l[uv_layer].uv = (u, v_uv)

    bm.to_mesh(dup.data)
    bm.free()
    dup.data.update()

    if subdiv_levels > 0:
        bpy.ops.object.select_all(action="DESELECT")
        dup.select_set(True)
        bpy.context.view_layer.objects.active = dup
        sub = dup.modifiers.new("TailorSubdiv", "SUBSURF")
        sub.levels = subdiv_levels
        sub.render_levels = subdiv_levels
        bpy.ops.object.modifier_apply(modifier=sub.name)

    return dup


def bind_rig(mesh_obj, rig_obj):
    mesh_obj.parent = rig_obj
    for m in list(mesh_obj.modifiers):
        if m.type == "ARMATURE":
            mesh_obj.modifiers.remove(m)
    mod = mesh_obj.modifiers.new("Armature", "ARMATURE")
    mod.object = rig_obj
    mod.use_vertex_groups = True


def normalize_skin_weights(mesh_obj, rig_obj):
    """Cap and normalize deform influences to match Unity's 4-weight importer with 0 unweighted vertices."""
    bone_names = {b.name for b in rig_obj.data.bones}
    for vg in list(mesh_obj.vertex_groups):
        if vg.name not in bone_names:
            mesh_obj.vertex_groups.remove(vg)
    groups = {g.index: g for g in mesh_obj.vertex_groups if g.name in bone_names}
    fallback_vg = mesh_obj.vertex_groups.get("mixamorig:Hips") or mesh_obj.vertex_groups.get("mixamorig:Head")
    if fallback_vg is None:
        fallback_vg = mesh_obj.vertex_groups.new(name="mixamorig:Hips")
        groups[fallback_vg.index] = fallback_vg
    for vertex in mesh_obj.data.vertices:
        weights = sorted(((g.weight, g.group) for g in vertex.groups
                          if g.group in groups and g.weight > 1e-5), reverse=True)
        if not weights:
            fallback_vg.add([vertex.index], 1.0, "REPLACE")
            continue
        kept = weights[:4]
        total = sum(w for w, _ in kept)
        for _, index in weights[4:]:
            groups[index].remove([vertex.index])
        for weight, index in kept:
            groups[index].add([vertex.index], weight / total, "REPLACE")


def make_skinned_lod(src_obj, new_name, ratio, rig_obj, keep_shapekeys=False, protect_fn=None):
    """Create a decimated skinned LOD copy preserving Armature deformation, protected silhouette features, and Shape Keys."""
    dup = src_obj.copy()
    dup.data = src_obj.data.copy()
    dup.name = new_name
    bpy.context.collection.objects.link(dup)

    had_shapekeys = bool(dup.data.shape_keys)
    # If decimation is requested on a mesh with shape keys, strip shape keys before Decimate, then re-transfer from src_obj!
    if dup.data.shape_keys and (not keep_shapekeys or ratio < 0.99):
        dup.shape_key_clear()

    if ratio < 0.99 and not dup.data.shape_keys:
        bpy.ops.object.select_all(action="DESELECT")
        dup.select_set(True)
        bpy.context.view_layer.objects.active = dup
        dec_vg = None
        if protect_fn is not None:
            # Build per-vertex material set so protect_fn(v, v_mat_names) can protect trim, badge, collar, etc.
            slot_names = [s.material.name if s.material else "" for s in dup.material_slots]
            v_mats = [set() for _ in range(len(dup.data.vertices))]
            for poly in dup.data.polygons:
                mname = slot_names[poly.material_index] if poly.material_index < len(slot_names) else ""
                for vi in poly.vertices:
                    v_mats[vi].add(mname)
            dec_vg = dup.vertex_groups.new(name="_LODDecimateable")
            for v in dup.data.vertices:
                w = 0.0 if protect_fn(v, v_mats[v.index]) else 1.0
                dec_vg.add([v.index], w, "REPLACE")
        dec = dup.modifiers.new("LODDecimate", "DECIMATE")
        dec.ratio = ratio
        dec.use_collapse_triangulate = False
        if dec_vg is not None:
            dec.vertex_group = "_LODDecimateable"
        dup.modifiers.move(len(dup.modifiers) - 1, 0)
        bpy.ops.object.modifier_apply(modifier=dec.name)
        vg_rem = dup.vertex_groups.get("_LODDecimateable")
        if vg_rem is not None:
            dup.vertex_groups.remove(vg_rem)

    triangulate_ngons(dup)
    if keep_shapekeys and had_shapekeys and not dup.data.shape_keys:
        transfer_shapekeys_by_surface(src_obj, dup, body_only=False)

    normalize_skin_weights(dup, rig_obj)
    bind_rig(dup, rig_obj)
    return dup


def export_character(fbx_path, rig_obj, all_meshes):
    """Export the selected character, its named LODs and the existing baked actions."""
    fbx_path = Path(fbx_path)
    fbx_path.parent.mkdir(parents=True, exist_ok=True)
    for m in all_meshes:
        triangulate_ngons(m)
    bpy.ops.object.select_all(action="DESELECT")
    rig_obj.select_set(True)
    for m in all_meshes:
        m.hide_viewport = False
        m.select_set(True)
    bpy.context.view_layer.objects.active = rig_obj

    tmp_fbx = fbx_path.with_name(f".{fbx_path.stem}.tmp.fbx")
    bpy.ops.export_scene.fbx(
        filepath=str(tmp_fbx),
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
    os.replace(tmp_fbx, fbx_path)


def save_and_export_character(name, rig_obj, lod0_meshes, lod1_meshes=None, lod2_meshes=None):
    """Stage generated files for review. Hand-edited production sources are promoted separately."""
    stage = Path(os.environ.get("CTCC_CHARACTER_STAGE", ROOT / "output" / "characters-generated")).resolve()
    stage.mkdir(parents=True, exist_ok=True)
    blend_path, fbx_path = stage / f"{name}.blend", stage / f"{name}.fbx"
    if blend_path.exists() or fbx_path.exists() or BLEND_DIR.resolve() == stage or FBX_DIR.resolve() == stage:
        raise FileExistsError(f"Choose an empty character staging directory: {stage}")
    all_meshes = list(lod0_meshes) + list(lod1_meshes or []) + list(lod2_meshes or [])
    for mesh in all_meshes:
        triangulate_ngons(mesh)
        normalize_skin_weights(mesh, rig_obj)
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path), relative_remap=True, copy=True)
    export_character(fbx_path, rig_obj, all_meshes)

    def count_tris(objs):
        return sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in (objs or []))

    sk_cnt = max((len(o.data.shape_keys.key_blocks) - 1 for o in lod0_meshes if o.data.shape_keys), default=0)
    print(f"[EXPORT] {name}: LOD0={count_tris(lod0_meshes)} tris | LOD1={count_tris(lod1_meshes)} tris | LOD2={count_tris(lod2_meshes)} tris | bones={len(rig_obj.data.bones)} | shapes={sk_cnt} | actions={len(bpy.data.actions)}")
    return blend_path, fbx_path
