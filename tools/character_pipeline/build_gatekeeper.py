"""Build Digital Human: CHAR005 Kẻ Giữ Cửa / The Gatekeeper (M2-C Validated Pipeline).

Canon specs (Core 02 / Core 04 / Core 05):
- Tall austere 1.940 m gaunt adult custodian/orderly silhouette
- Heavy smoke-charred, singed knee-length medical overcoat (`áo blouse cháy xém`, `CharredBlouse`)
  with high standing darkened collar, dark institutional trousers (`DarkTrouser`), heavy worn boots (`ClinicalShoe`)
- Real gaunt anatomical human head/face veiled in soot & shadow (`GatekeeperSkin`) — ZERO gore, ZERO fangs
- Articulated 65-bone rig (`mixamo_unity` + swinging `KeyRing` bone parented to `mixamorig:RightHand`)
  gripping a detailed forged metal hospital keyring (`MetalTrim`) synced to `SFX-KEY` cadence
- 3 weighted performances: `GK_IdleStill`, `GK_StalkWalk` (CMU MoCap `08_04.bvh` retargeted to heavy stride),
  and `GK_InspectReach`
- Game-ready LOD0 / LOD1 / LOD2 + editable ArtSource/Blender/CTCC_CHR_Gatekeeper.blend + Unity FBX
"""

import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import dh_core as DH
import retarget_cmu as RC

from bl_ext.blender_org.mpfb.services.assetservice import AssetService
from bl_ext.blender_org.mpfb.services.humanservice import HumanService
from bl_ext.blender_org.mpfb.services.targetservice import TargetService


def create_gatekeeper_materials():
    mats = {}
    mats["GatekeeperSkin"] = DH.create_pbr_material(
        "GatekeeperSkin", color_tex="GatekeeperSkin_Color.png", normal_tex="GatekeeperSkin_Normal.png",
        mask_tex="GatekeeperSkin_Mask.png", roughness=0.74, sss_weight=0.08, normal_strength=0.95,
    )
    mats["LanEye"] = DH.create_pbr_material(
        "LanEye", color_tex="LanEye_Color.png", normal_tex="LanEye_Normal.png",
        roughness=0.14, coat_weight=0.55, normal_strength=0.35,
    )
    mats["LanTeeth"] = DH.create_pbr_material(
        "LanTeeth", color_tex="LanTeeth_Color.png", roughness=0.30, coat_weight=0.25,
    )
    mats["LanTongue"] = DH.create_pbr_material(
        "LanTongue", color_tex="LanTongue_Color.png", roughness=0.40, sss_weight=0.22,
    )
    mats["GatekeeperHairCard"] = DH.create_pbr_material(
        "GatekeeperHairCard", color_tex="GatekeeperHair_Color.png", normal_tex="GatekeeperHair_Normal.png",
        roughness=0.62, coat_weight=0.02, alpha_mode="BLEND", normal_strength=0.72,
    )
    mats["CharredBlouse"] = DH.create_pbr_material(
        "CharredBlouse", color_tex="CharredBlouse_Color.png", normal_tex="CharredBlouse_Normal.png",
        mask_tex="CharredBlouse_Mask.png", roughness=0.92, normal_strength=1.05, uv_tile=1.4,
    )
    mats["DarkTrouser"] = DH.create_pbr_material(
        "DarkTrouser", color_tex="DarkTrouser_Color.png", normal_tex="DarkTrouser_Normal.png",
        mask_tex="DarkTrouser_Mask.png", roughness=0.88, normal_strength=0.82, uv_tile=1.5,
    )
    mats["ClinicalShoe"] = DH.create_pbr_material(
        "ClinicalShoe", color_tex="ClinicalShoe_Color.png", normal_tex="ClinicalShoe_Normal.png",
        mask_tex="ClinicalShoe_Mask.png", roughness=0.60, normal_strength=0.75,
    )
    mats["MetalTrim"] = DH.create_pbr_material(
        "MetalTrim", base_color=(0.56, 0.52, 0.44, 1.0), roughness=0.36, metallic=0.86,
    )
    return mats


def split_and_rig_eyes(eyes_obj, rig_obj, eye_mat):
    DH.strip_outer_cornea_shell(eyes_obj)
    bm = bmesh.new()
    bm.from_mesh(eyes_obj.data)
    bm_l, bm_r = bm.copy(), bm.copy()
    bm.free()
    bmesh.ops.delete(bm_l, geom=[v for v in bm_l.verts if v.co.x < 0.0], context="VERTS")
    bmesh.ops.delete(bm_r, geom=[v for v in bm_r.verts if v.co.x >= 0.0], context="VERTS")
    out = []
    for name, bmx, bone_name in (
        ("GK_Eye_L_LOD0", bm_l, "mixamorig:LeftEye"),
        ("GK_Eye_R_LOD0", bm_r, "mixamorig:RightEye"),
    ):
        me = bpy.data.meshes.new(name + "_mesh")
        bmx.to_mesh(me)
        bmx.free()
        obj = bpy.data.objects.new(name, me)
        bpy.context.collection.objects.link(obj)
        DH.set_smooth(obj)
        DH.assign_material(obj, eye_mat)
        DH.weight_rigid(obj, rig_obj, bone_name)
        out.append(obj)
    bpy.data.objects.remove(eyes_obj, do_unlink=True)
    return out[0], out[1]


def rig_oral_cavity(teeth_obj, tongue_obj, rig_obj, teeth_mat, tongue_mat):
    teeth_obj.name = "GK_Teeth_LOD0"
    tongue_obj.name = "GK_Tongue_LOD0"
    for v in teeth_obj.data.vertices:
        v.co.x *= 0.89
    teeth_obj.data.update()
    zs = [v.co.z for v in teeth_obj.data.vertices]
    z_mid = 0.5 * (min(zs) + max(zs))
    teeth_obj.vertex_groups.clear()
    vg_head = teeth_obj.vertex_groups.new(name="mixamorig:Head")
    vg_jaw = teeth_obj.vertex_groups.new(name="mixamorig:Jaw")
    for v in teeth_obj.data.vertices:
        (vg_head if v.co.z >= z_mid - 0.001 else vg_jaw).add([v.index], 1.0, "REPLACE")
    DH.set_smooth(teeth_obj)
    DH.assign_material(teeth_obj, teeth_mat)
    DH.bind_rig(teeth_obj, rig_obj)

    DH.set_smooth(tongue_obj)
    DH.assign_material(tongue_obj, tongue_mat)
    DH.weight_rigid(tongue_obj, rig_obj, "mixamorig:Jaw")


def add_keyring_bone(rig_obj):
    """Add swinging `KeyRing` bone hanging cleanly from `mixamorig:RightHand`."""
    bpy.ops.object.select_all(action="DESELECT")
    rig_obj.select_set(True)
    bpy.context.view_layer.objects.active = rig_obj
    bpy.ops.object.mode_set(mode="EDIT")
    rhand = rig_obj.data.edit_bones.get("mixamorig:RightHand")
    if rhand and "KeyRing" not in rig_obj.data.edit_bones:
        kr = rig_obj.data.edit_bones.new("KeyRing")
        kr.parent = rhand
        kr.use_connect = False
        kr.use_deform = True
        hand_dir = (rhand.tail - rhand.head).normalized()
        kr.head = rhand.head + hand_dir * 0.075 + Vector((0.0, -0.015, -0.025))
        kr.tail = kr.head + Vector((0.0, 0.0, -0.145))
    bpy.ops.object.mode_set(mode="OBJECT")


def build_keyring_prop(rig_obj, metal_mat):
    """Build detailed forged hospital iron/brass keyring with 7 vintage ward keys rigged to `KeyRing`."""
    kr_b = rig_obj.data.bones["KeyRing"]
    top = kr_b.head_local.copy()
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")

    # Heavy split iron ring (torus built from clean quads)
    ring_c = top + Vector((0.0, 0.0, -0.022))
    R_maj, R_min = 0.024, 0.0032
    nu, nv = 20, 8
    grid = []
    for i in range(nu):
        u = (i / float(nu)) * math.tau
        ring = []
        for j in range(nv):
            v = (j / float(nv)) * math.tau
            px = (R_maj + R_min * math.cos(v)) * math.cos(u)
            py = R_min * math.sin(v)
            pz = (R_maj + R_min * math.cos(v)) * math.sin(u)
            ring.append(bm.verts.new(ring_c + Vector((px, py, pz))))
        grid.append(ring)
    for i in range(nu):
        ni = (i + 1) % nu
        for j in range(nv):
            nj = (j + 1) % nv
            f = bm.faces.new((grid[i][j], grid[ni][j], grid[ni][nj], grid[i][nj]))
            f.smooth = True
            for l in f.loops: l[uv].uv = (0.5, 0.5)

    # 7 hanging vintage ward keys fanned naturally from bottom of split ring!
    rng = random.Random(198705)
    for ki in range(7):
        ang = math.radians(-26.0 + ki * 8.6 + rng.uniform(-2.0, 2.0))
        yaw = math.radians(-40.0 + ki * 15.0)
        k_len = 0.068 + (ki % 3) * 0.011
        pivot = ring_c + Vector((math.sin(ang) * 0.018, math.cos(yaw) * 0.006, -R_maj + 0.002))
        rot = Matrix.Rotation(yaw, 4, "Z") @ Matrix.Rotation(ang, 4, "Y")
        # Oval key bow
        bmesh.ops.create_cube(
            bm, size=1.0,
            matrix=Matrix.Translation(pivot) @ rot @ Matrix.Translation(Vector((0, 0, -0.012))) @ Matrix.Diagonal((0.019, 0.0035, 0.020, 1.0)),
        )
        # Key shaft
        bmesh.ops.create_cube(
            bm, size=1.0,
            matrix=Matrix.Translation(pivot) @ rot @ Matrix.Translation(Vector((0, 0, -0.022 - k_len * 0.5))) @ Matrix.Diagonal((0.0055, 0.0032, k_len, 1.0)),
        )
        # Key bit teeth at bottom
        bmesh.ops.create_cube(
            bm, size=1.0,
            matrix=Matrix.Translation(pivot) @ rot @ Matrix.Translation(Vector((0.0065, 0, -0.016 - k_len))) @ Matrix.Diagonal((0.011, 0.0028, 0.015, 1.0)),
        )
    for f in bm.faces:
        for l in f.loops:
            l[uv].uv = (0.5, 0.5)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    me = bpy.data.meshes.new("GK_KeyRing_mesh")
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new("GK_KeyRing_LOD0", me)
    bpy.context.collection.objects.link(obj)
    DH.assign_material(obj, metal_mat)
    DH.weight_rigid(obj, rig_obj, "KeyRing")
    return obj


def build_gatekeeper_garments(body_clean_copy, rig_obj, mats):
    """Build tall austere cloth-simulated scorched overcoat (`CharredBlouse`), dark trousers, and heavy worn boots."""
    neck_z = rig_obj.data.bones["mixamorig:Neck"].head_local.z
    hip_z = rig_obj.data.bones["mixamorig:Hips"].head_local.z
    knee_z = rig_obj.data.bones["mixamorig:LeftLeg"].head_local.z
    l_hand = rig_obj.data.bones["mixamorig:LeftHand"]
    wrist_x = abs(l_hand.head_local.x) - 0.018

    # 1. FULL-LENGTH dark worn institutional trousers (boot top -> full waist/pelvis, ZERO hollow leg gap!)
    def tr_pred(c, f):
        return 0.055 <= c.z <= hip_z + 0.18 and abs(c.x) <= 0.29

    def tr_flare(co, nrm):
        t = max(0.0, min(1.0, (hip_z - co.z) / max(0.2, hip_z - 0.055)))
        out = nrm * (0.010 + 0.015 * t)
        if abs(co.x) < 0.035 and co.z < hip_z - 0.05:
            out.x *= 0.25
        return out

    trousers = DH.extract_garment_shell_from_body(
        body_clean_copy, "GK_Trousers", tr_pred,
        offset_m=0.007, smooth_iters=8, flare_fn=tr_flare, uv_scale=2.0,
    )
    vg_tp = trousers.vertex_groups.new(name="Pin")
    for v in trousers.data.vertices:
        if v.co.z >= hip_z + 0.06:
            vg_tp.add([v.index], 1.0, "REPLACE")
    DH.run_cloth_simulation(trousers, body_clean_copy, "Pin", frames=18, mass=0.40, tension=24.0, bending=2.4)
    trousers.vertex_groups.clear()
    DH.assign_material(trousers, mats["DarkTrouser"])

    # Combined collider (body + inflated full trousers) so long overcoat drapes cleanly OUTSIDE trousers!
    tr_col = trousers.copy()
    tr_col.data = trousers.data.copy()
    tr_col.name = "_GKTrCol"
    bpy.context.collection.objects.link(tr_col)
    for v in tr_col.data.vertices:
        v.co += v.normal * 0.010
    tr_col.data.update()

    comb_col = body_clean_copy.copy()
    comb_col.data = body_clean_copy.data.copy()
    comb_col.name = "_GKCombCol"
    bpy.context.collection.objects.link(comb_col)
    bpy.ops.object.select_all(action="DESELECT")
    comb_col.select_set(True)
    tr_col.select_set(True)
    bpy.context.view_layer.objects.active = comb_col
    bpy.ops.object.join()

    # 2. Towering heavy knee-length scorched overcoat (`CharredBlouse`)
    # Extract clean upper coat (waist -> neck + full-length sleeves), purge any tiny disconnected hand scraps,
    # then EXTRUDE a continuous, wide, flaring knee-length coat skirt directly from the bottom waist edge loop!
    coat_hem_z = knee_z + 0.02
    waist_cut_z = hip_z + 0.065
    sleeve_max_x = wrist_x - 0.012

    def coat_pred(c, f):
        if c.z < waist_cut_z or c.z > neck_z + 0.028:
            return False
        if abs(c.x) > sleeve_max_x:
            return False
        return True

    def coat_flare(co, nrm):
        extra = Vector((0.0, 0.0, 0.0))
        if abs(co.x) < 0.27:
            # Straight broad heavy-overcoat torso drape (no tight chest cling!)
            extra += nrm * 0.020
            if co.y < -0.02 and abs(co.x) < 0.19 and waist_cut_z <= co.z <= neck_z - 0.14:
                target_y = -0.175 + 0.020 * (abs(co.x) / 0.19) ** 2
                if co.y > target_y:
                    extra.y += (target_y - co.y) * 0.72
            # Smooth elliptical flare at bottom waist edge so extruded coat skirt clears trousers cleanly!
            if co.z < waist_cut_z + 0.12:
                tw = max(0.0, min(1.0, (waist_cut_z + 0.12 - co.z) / 0.12))
                rad = Vector((co.x, co.y + 0.02, 0.0))
                if rad.length > 0.01:
                    extra += rad.normalized() * (0.024 * tw)
        else:
            # Heavy loose coat sleeves
            extra += nrm * 0.018
        return extra

    coat = DH.extract_garment_shell_from_body(
        body_clean_copy, "GK_Coat", coat_pred,
        offset_m=0.011, smooth_iters=10, flare_fn=coat_flare, uv_scale=1.9,
    )

    # Purge any stray small disconnected mesh islands (<50 verts) so ZERO floating cloth scraps ever exist!
    bm_c = bmesh.new()
    bm_c.from_mesh(coat.data)
    uv_c = bm_c.loops.layers.uv.active or bm_c.loops.layers.uv.new("UVMap")
    visited = set()
    islands = []
    for v in bm_c.verts:
        if v in visited:
            continue
        comp, stack = [], [v]
        visited.add(v)
        while stack:
            cur = stack.pop()
            comp.append(cur)
            for e in cur.link_edges:
                nb = e.other_vert(cur)
                if nb not in visited:
                    visited.add(nb)
                    stack.append(nb)
        islands.append(comp)
    islands.sort(key=len, reverse=True)
    if len(islands) > 1:
        stray = [v for isl in islands[1:] for v in isl]
        bmesh.ops.delete(bm_c, geom=stray, context="VERTS")

    # Extrude CONTINUOUS knee-length overcoat skirt downward in 8 quad rings from the waist boundary loop!
    # Guarantees a solid, imposing, uninterrupted long coat silhouette all the way to the knees!
    waist_edges = [
        e for e in bm_c.edges
        if e.is_boundary and all(v.co.z < waist_cut_z + 0.075 and abs(v.co.x) < 0.28 for v in e.verts)
    ]
    if waist_edges:
        w_vs = list({v for e in waist_edges for v in e.verts})
        # Level the waist start ring cleanly
        for v in w_vs:
            v.co.z = waist_cut_z
        n_rings = 8
        cur_e = waist_edges
        for ri in range(1, n_rings + 1):
            frac = ri / float(n_rings)
            target_z = waist_cut_z + (coat_hem_z - waist_cut_z) * frac
            ret = bmesh.ops.extrude_edge_only(bm_c, edges=cur_e)
            nvs = [g for g in ret["geom"] if isinstance(g, bmesh.types.BMVert)]
            nes = [g for g in ret["geom"] if isinstance(g, bmesh.types.BMEdge) and all(v in nvs for v in g.verts)]
            nfs = [g for g in ret["geom"] if isinstance(g, bmesh.types.BMFace)]
            for v in nvs:
                rad = Vector((v.co.x, v.co.y + 0.02, 0.0))
                if rad.length > 1e-4:
                    rad_n = rad.normalized()
                else:
                    rad_n = Vector((0.0, -1.0, 0.0))
                # Gentle A-line overcoat flare (+6.5cm wider at knees so strides never clip trousers!)
                flare_step = 0.0095
                v.co.x += rad_n.x * flare_step
                v.co.y += rad_n.y * flare_step
                # Ensure minimum wide overcoat radius around full trousers
                min_rx = 0.235 + 0.070 * frac
                min_ry = 0.165 + 0.055 * frac
                ell = math.hypot(v.co.x / min_rx, (v.co.y + 0.02) / min_ry)
                if 0.05 < ell < 1.0:
                    v.co.x = v.co.x / ell
                    v.co.y = (v.co.y + 0.02) / ell - 0.02
                # Organic subtly singed uneven edge at bottom 2 rings!
                tatter = 0.0
                if ri >= n_rings - 1:
                    ang = math.atan2(v.co.x, v.co.y)
                    tatter = (0.024 * math.sin(ang * 5.0 + 0.7) + 0.012 * math.cos(ang * 11.0)) * (1.0 if ri == n_rings else 0.45)
                v.co.z = target_z + tatter
            for f in nfs:
                f.smooth = True
                for l in f.loops:
                    ang = math.atan2(l.vert.co.x, -l.vert.co.y)
                    l[uv_c].uv = ((ang / math.tau + 0.5) * 1.9, l.vert.co.z * 1.9)
            cur_e = nes

    # Extrude austere standing collar rising to jawline/chin (NOT covering nose!)
    neck_edges = [
        e for e in bm_c.edges
        if e.is_boundary and all(v.co.z > neck_z - 0.055 and abs(v.co.x) < 0.14 for v in e.verts)
    ]
    if neck_edges:
        neck_vs = list({v for e in neck_edges for v in e.verts})
        mean_nz = sum(v.co.z for v in neck_vs) / len(neck_vs)
        for v in neck_vs:
            v.co.z = v.co.z * 0.35 + mean_nz * 0.65
        cur_e = neck_edges
        for dz, dr in ((0.026, 0.008), (0.024, 0.009)):
            ret = bmesh.ops.extrude_edge_only(bm_c, edges=cur_e)
            nvs = [g for g in ret["geom"] if isinstance(g, bmesh.types.BMVert)]
            nes = [g for g in ret["geom"] if isinstance(g, bmesh.types.BMEdge) and all(v in nvs for v in g.verts)]
            nfs = [g for g in ret["geom"] if isinstance(g, bmesh.types.BMFace)]
            for v in nvs:
                rad = Vector((v.co.x, v.co.y + 0.015, 0.0))
                if rad.length > 1e-4:
                    rad.normalize()
                # Higher at nape/sides, cleanly dipped below chin/mouth in front!
                front_dip = -0.016 * max(0.0, -v.co.y / 0.08)
                back_boost = 0.012 if v.co.y > 0.0 else front_dip
                v.co += Vector((rad.x * dr, rad.y * dr, dz + back_boost))
            for f in nfs:
                f.smooth = True
                for l in f.loops:
                    l[uv_c].uv = (0.5 + l.vert.co.x * 3.0, l.vert.co.z * 2.0)
            cur_e = nes

    bmesh.ops.recalc_face_normals(bm_c, faces=list(bm_c.faces))
    bm_c.to_mesh(coat.data)
    bm_c.free()
    coat.data.update()

    # Run real Blender Cloth Simulation on heavy scorched overcoat over combined collider!
    vg_cp = coat.vertex_groups.new(name="Pin")
    for v in coat.data.vertices:
        if v.co.z >= neck_z - 0.14 and abs(v.co.x) < 0.28:
            vg_cp.add([v.index], 1.0, "REPLACE")
        elif abs(v.co.x) >= 0.24 and v.co.z > neck_z - 0.24:
            vg_cp.add([v.index], 0.85, "REPLACE")
    DH.run_cloth_simulation(coat, comb_col, "Pin", frames=22, mass=0.45, tension=24.0, bending=2.4)
    coat.vertex_groups.clear()

    bpy.ops.object.select_all(action="DESELECT")
    coat.select_set(True)
    bpy.context.view_layer.objects.active = coat
    sol_c = coat.modifiers.new("CoatSolid", "SOLIDIFY")
    sol_c.thickness = 0.0028
    sol_c.offset = -0.35
    bpy.ops.object.modifier_apply(modifier=sol_c.name)
    DH.assign_material(coat, mats["CharredBlouse"])
    bpy.data.objects.remove(comb_col, do_unlink=True)

    # 3. Heavy closed-toe worn clinical boots (smooth silhouette, ZERO toe ridges, ZERO ankle gap!)
    bm_bt = bmesh.new()
    uv_bt = bm_bt.loops.layers.uv.new("UVMap")
    for sgn in (1.0, -1.0):
        fverts = [v.co for v in body_clean_copy.data.vertices if v.co.z <= 0.11 and v.co.x * sgn > 0.006]
        if not fverts:
            continue
        x0, x1 = min(p.x for p in fverts), max(p.x for p in fverts)
        y0, y1 = min(p.y for p in fverts), max(p.y for p in fverts)
        cx_f = 0.5 * (x0 + x1)
        hw = 0.5 * (x1 - x0) + 0.009
        y_toe = y0 - 0.015
        y_heel = y1 + 0.012
        rings = []
        n_r = 16
        stations = [
            (0.00, 0.50, 0.085, 0.006),
            (0.08, 0.85, 0.125, 0.000),
            (0.28, 0.92, 0.122, 0.000),
            (0.50, 0.96, 0.078, 0.000),
            (0.75, 1.00, 0.056, 0.000),
            (0.90, 0.82, 0.042, 0.000),
            (0.97, 0.54, 0.028, 0.004),
            (1.00, 0.18, 0.016, 0.008),
        ]
        for fy, ws, tz, bz in stations:
            y = y_heel + (y_toe - y_heel) * fy
            rx = hw * ws
            rz = 0.5 * (tz - bz)
            cz = 0.5 * (tz + bz)
            ring = []
            for k in range(n_r):
                a = (k / float(n_r)) * math.tau
                sa = math.sin(a)
                pz = bz if sa < -0.35 else (cz + sa * rz)
                ring.append(bm_bt.verts.new(Vector((cx_f + math.cos(a) * rx, y, max(0.0, pz)))))
            rings.append(ring)
        for j in range(len(rings) - 1):
            for k in range(n_r):
                nk = (k + 1) % n_r
                f = bm_bt.faces.new((rings[j][k], rings[j][nk], rings[j + 1][nk], rings[j + 1][k]))
                f.smooth = True
                for l in f.loops: l[uv_bt].uv = (0.5, 0.5)
        for end_ring, flip in ((rings[0], True), (rings[-1], False)):
            cv = bm_bt.verts.new(sum((v.co for v in end_ring), Vector((0, 0, 0))) / len(end_ring))
            for k in range(n_r):
                nk = (k + 1) % n_r
                f = bm_bt.faces.new((end_ring[nk], end_ring[k], cv) if flip else (end_ring[k], end_ring[nk], cv))
                f.smooth = True
                for l in f.loops: l[uv_bt].uv = (0.5, 0.5)
    bmesh.ops.recalc_face_normals(bm_bt, faces=list(bm_bt.faces))
    me_bt = bpy.data.meshes.new("GK_Boots_mesh")
    bm_bt.to_mesh(me_bt)
    bm_bt.free()
    boots = bpy.data.objects.new("GK_Boots", me_bt)
    bpy.context.collection.objects.link(boots)
    DH.assign_material(boots, mats["ClinicalShoe"])

    # Join overcoat, trousers & boots cleanly into `GK_Coat_LOD0` and transfer skin weights!
    bpy.ops.object.select_all(action="DESELECT")
    for o in (coat, trousers, boots):
        o.select_set(True)
    bpy.context.view_layer.objects.active = coat
    bpy.ops.object.join()
    garment = bpy.context.active_object
    garment.name = "GK_Coat_LOD0"
    DH.set_smooth(garment)
    DH.triangulate_ngons(garment)
    DH.transfer_skin_weights(body_clean_copy, garment, rig_obj, smooth_iters=4, smooth_factor=0.50)

    print(f"[GARMENT] Built Gatekeeper scorched overcoat, trousers & boots ({len(garment.data.polygons)} polys)")
    return garment


def build_gatekeeper_animations(rig_obj):
    """Build Gatekeeper's 3 austere weighted actions (`GK_IdleStill`, `GK_StalkWalk` CMU 08_04, `GK_InspectReach`)."""

    def gk_hands(rig, grip_r=0.72):
        # Right hand firmly grips metal keyring; left hand austere & tense
        RC.apply_finger_curl(rig, "Right", grip_r, grip_r + 0.08, grip_r + 0.12, grip_r + 0.14, 0.55, 0.0)
        RC.apply_finger_curl(rig, "Left", 0.34, 0.40, 0.46, 0.52, 0.24, 0.0)

    # 1. GK_StalkWalk — Real CMU MoCap deliberate heavy walk (`08_04.bvh`), upright austere gait + swinging KeyRing!
    def post_stalk(rig, t, fi):
        gk_hands(rig, grip_r=0.78)
        # Swing metal keyring rhythmically with each heavy stride (synced to SFX-KEY!)
        sway = math.sin(t * math.tau * 2.0)
        RC.add_world_delta(rig, "KeyRing", rx_deg=14.0 * sway, ry_deg=6.0 * math.cos(t * math.tau * 2.0))
        # Subtle forward austere brow-down head angle (keeps eyes shadowed under brow!)
        RC.add_world_delta(rig, "mixamorig:Head", rx_deg=5.5)

    RC.retarget_cmu_clip(
        rig_obj, "GK_StalkWalk", "08_04.bvh",
        src_start=60, src_end=300, step=4,
        in_place=True, cyclic_blend=8,
        scale_rot=0.86,
        bone_scales={
            "mixamorig:LeftArm": 0.65, "mixamorig:RightArm": 0.58,
            "mixamorig:Head": 0.45, "mixamorig:Spine2": 0.72,
        },
        post_frame_fn=post_stalk,
        target_world_speed=1.78,
        stride_cycles=3,
    )

    # 2. GK_IdleStill — Towering motionless watchfulness, heavy breathing, slight keyring pendulum
    def idle_frame(rig, t, fi):
        s1 = math.sin(t * math.tau)
        RC.add_world_delta(rig, "mixamorig:Hips", rx_deg=0.8, rz_deg=0.6 * s1)
        RC.aim_bone_world(rig, "mixamorig:LeftUpLeg", Vector((0.04, 0.0, -1.0)))
        RC.aim_bone_world(rig, "mixamorig:LeftLeg", Vector((0.02, 0.02, -1.0)))
        RC.aim_bone_world(rig, "mixamorig:RightUpLeg", Vector((-0.04, 0.0, -1.0)))
        RC.aim_bone_world(rig, "mixamorig:RightLeg", Vector((-0.02, 0.02, -1.0)))
        RC.add_world_delta(rig, "mixamorig:Spine1", rx_deg=1.5 - 1.2 * s1)
        RC.add_world_delta(rig, "mixamorig:Spine2", rx_deg=2.0 - 1.4 * s1)
        RC.add_world_delta(rig, "mixamorig:Neck", rx_deg=3.0)
        RC.add_world_delta(rig, "mixamorig:Head", rx_deg=5.5, rz_deg=1.8 * s1)

        RC.aim_bone_world(rig, "mixamorig:LeftArm", Vector((0.14, -0.04, -0.99)))
        RC.aim_bone_world(rig, "mixamorig:LeftForeArm", Vector((0.04, -0.18, -0.98)))
        RC.aim_bone_world(rig, "mixamorig:RightArm", Vector((-0.14, -0.06, -0.99)))
        RC.aim_bone_world(rig, "mixamorig:RightForeArm", Vector((-0.04, -0.22, -0.97)))
        RC.add_world_delta(rig, "KeyRing", rx_deg=4.5 * s1, ry_deg=2.5 * math.cos(t * math.tau))
        gk_hands(rig, grip_r=0.76)
        return Vector((0.0, 0.0, 0.0))

    a_idle = RC.author_layered_action(rig_obj, "GK_IdleStill", 120, idle_frame)

    # 3. GK_InspectReach — Authoritative halt & slow forward raise of keyring hand at the 1.75 m non-lethal buffer!
    def reach_frame(rig, t, fi):
        s = math.sin(t * math.pi)
        s = s * s
        RC.add_world_delta(rig, "mixamorig:Hips", rx_deg=1.5 * s, rz_deg=-4.5 * s)
        RC.aim_bone_world(rig, "mixamorig:LeftUpLeg", Vector((0.04, 0.02, -1.0)))
        RC.aim_bone_world(rig, "mixamorig:LeftLeg", Vector((0.02, 0.03, -1.0)))
        RC.aim_bone_world(rig, "mixamorig:RightUpLeg", Vector((-0.04, -0.05 * s, -1.0)))
        RC.aim_bone_world(rig, "mixamorig:RightLeg", Vector((-0.02, 0.05 * s, -1.0)))
        RC.add_world_delta(rig, "mixamorig:Spine1", rx_deg=2.8 * s)
        RC.add_world_delta(rig, "mixamorig:Spine2", rx_deg=2.2 * s, rz_deg=3.5 * s)
        RC.add_world_delta(rig, "mixamorig:Neck", rx_deg=2.0)
        RC.add_world_delta(rig, "mixamorig:Head", rx_deg=4.0, ry_deg=3.5 * s)

        RC.aim_bone_world(rig, "mixamorig:LeftArm", Vector((0.16, -0.05, -0.98)))
        RC.aim_bone_world(rig, "mixamorig:LeftForeArm", Vector((0.04, -0.22, -0.97)))
        r_arm = Vector((-0.14, -0.06, -0.99)).lerp(Vector((-0.08, -0.68, -0.73)), s)
        r_for = Vector((-0.04, -0.22, -0.97)).lerp(Vector((0.04, -0.96, 0.26)), s)
        RC.aim_bone_world(rig, "mixamorig:RightArm", r_arm)
        RC.aim_bone_world(rig, "mixamorig:RightForeArm", r_for)
        RC.add_world_delta(rig, "KeyRing", rx_deg=18.0 * math.sin(t * math.tau * 2.0) * (0.3 + 0.7 * s))
        gk_hands(rig, grip_r=0.80)
        return Vector((0.0, -0.03 * s, 0.0))

    RC.author_layered_action(rig_obj, "GK_InspectReach", 90, reach_frame)
    rig_obj.animation_data.action = a_idle
    bpy.context.scene.frame_set(15)


def build_gatekeeper():
    DH.fresh_metric_scene()

    # 1. Calibrated gaunt towering 1.940 m austere Vietnamese male (100% Asian `hm08` base + Vietnamese facial structure!)
    macro = TargetService.get_default_macro_info_dict()
    macro["gender"] = 1.0
    macro["age"] = 0.66          # gaunt weathered middle-older adult
    macro["weight"] = 0.25       # lean, gaunt silhouette
    macro["muscle"] = 0.42
    macro["height"] = 0.92
    macro["proportions"] = 0.56
    macro["race"] = {"asian": 1.0, "caucasian": 0.0, "african": 0.0}

    facial_targets = {
        "head-scale-depth-decr": 0.12,
        "l-cheek-bones-incr": 0.22,
        "r-cheek-bones-incr": 0.22,
        "l-cheek-inner-decr": 0.20,
        "r-cheek-inner-decr": 0.20,
        "l-eye-epicanthus-in": 0.18,
        "r-eye-epicanthus-in": 0.18,
        "l-eye-eyefold-down": 0.22,
        "r-eye-eyefold-down": 0.22,
        "l-eye-scale-decr": 0.10,
        "r-eye-scale-decr": 0.10,
        "eyebrows-trans-down": 0.16,
        "nose-scale-depth-decr": 0.18,
        "nose-compression-compress": 0.16,
        "nose-point-width-incr": 0.14,
        "nose-nostrils-width-incr": 0.16,
        "nose-flaring-incr": 0.10,
        "chin-prominent-decr": 0.08,
        "chin-height-incr": 0.08,
    }
    human = DH.create_calibrated_human(macro, target_height_m=1.940, facial_targets=facial_targets)
    human.name = "GK_Body_Raw"

    def load_part(cat, frag):
        p = AssetService.find_asset_absolute_path(frag, cat)
        if not p:
            raise FileNotFoundError(f"Missing CC0 asset {cat}/{frag}")
        return HumanService.add_mhclo_asset(p, human, asset_type=cat, subdiv_levels=0, material_type="NONE")

    eyes_obj = load_part("eyes", "high-poly/high-poly.mhclo")
    teeth_obj = load_part("teeth", "teeth_base/teeth_base.mhclo")
    tongue_obj = load_part("tongue", "tongue01/tongue01.mhclo")
    brows_obj = load_part("eyebrows", "eyebrow004/eyebrow004.mhclo")
    hair_obj = load_part("hair", "short04/short04.mhclo")
    brows_obj.name = "GK_Eyebrows_LOD0"
    hair_obj.name = "GK_Hair_LOD0"

    # 2. Anatomical facial BlendShapes for subtle shadowed CH02-D05 speech & stern brow
    DH.build_anatomical_blendshapes(human, scale_corr=1.18)
    DH.transfer_shapekeys_by_surface(human, brows_obj)

    # 3. Full 64-bone `mixamo_unity` rig + swinging `KeyRing` bone
    rig = HumanService.add_builtin_rig(human, "mixamo_unity", import_weights=True)
    rig.name = "GK_Armature"
    add_keyring_bone(rig)

    mats = create_gatekeeper_materials()
    mats["LanEyebrow"] = DH.create_pbr_material(
        "LanEyebrow", color_tex="LanEyebrow_Color.png", roughness=0.65, alpha_mode="BLEND",
    )

    eye_l, eye_r = split_and_rig_eyes(eyes_obj, rig, mats["LanEye"])
    rig_oral_cavity(teeth_obj, tongue_obj, rig, mats["LanTeeth"], mats["LanTongue"])
    DH.set_smooth(brows_obj)
    DH.assign_material(brows_obj, mats["LanEyebrow"])
    DH.weight_rigid(brows_obj, rig, "mixamorig:Head")
    DH.set_smooth(hair_obj)
    DH.assign_material(hair_obj, mats["GatekeeperHairCard"])
    DH.weight_rigid(hair_obj, rig, "mixamorig:Head")

    keyring_obj = build_keyring_prop(rig, mats["MetalTrim"])

    # 4. Cloth-simulated scorched overcoat, dark trousers & worn heavy clinical boots
    body_for_cloth = human.copy()
    body_for_cloth.data = human.data.copy()
    body_for_cloth.name = "_GKClothBody"
    bpy.context.collection.objects.link(body_for_cloth)
    if body_for_cloth.data.shape_keys:
        body_for_cloth.shape_key_clear()
    DH.strip_helper_geometry_preserving_shapekeys(body_for_cloth, extra_keep_fn=None)

    garment_obj = build_gatekeeper_garments(body_for_cloth, rig, mats)

    # 5. Keep shadowed anatomical head/neck + full forearms & soot-stained articulated hands (generous sleeve overlap!)
    neck_z = rig.data.bones["mixamorig:Neck"].head_local.z
    l_fore = rig.data.bones["mixamorig:LeftForeArm"]
    forearm_keep_x = abs(l_fore.head_local.x) - 0.02

    def gk_keep_body(co):
        if co.z > neck_z - 0.06:
            return True
        if abs(co.x) > forearm_keep_x:
            return True
        return False

    DH.strip_helper_geometry_preserving_shapekeys(human, extra_keep_fn=gk_keep_body)
    DH.set_smooth(human)
    DH.assign_material(human, mats["GatekeeperSkin"])
    DH.bind_rig(human, rig)

    bpy.ops.object.select_all(action="DESELECT")
    human.select_set(True)
    brows_obj.select_set(True)
    bpy.context.view_layer.objects.active = human
    bpy.ops.object.join()
    human = bpy.context.active_object
    human.name = "GK_Body_LOD0"

    bpy.data.objects.remove(body_for_cloth, do_unlink=True)

    # 6. Game-ready LOD0 / LOD1 / LOD2 (all 8 parts across all 3 LODs + 29 BlendShapes on Body_LOD0..2!)
    lod0_meshes = [human, eye_l, eye_r, teeth_obj, tongue_obj, hair_obj, garment_obj, keyring_obj]

    def protect_gk_collar(v, _mats):
        return v.co.z >= neck_z - 0.055 and abs(v.co.x) < 0.16

    body_lod1 = DH.make_skinned_lod(human, "GK_Body_LOD1", ratio=1.0, rig_obj=rig, keep_shapekeys=True)
    garment_lod1 = DH.make_skinned_lod(garment_obj, "GK_Coat_LOD1", ratio=0.55, rig_obj=rig, keep_shapekeys=False, protect_fn=protect_gk_collar)
    hair_lod1 = DH.make_skinned_lod(hair_obj, "GK_Hair_LOD1", ratio=0.65, rig_obj=rig, keep_shapekeys=False)
    key_lod1 = DH.make_skinned_lod(keyring_obj, "GK_KeyRing_LOD1", ratio=0.65, rig_obj=rig, keep_shapekeys=False)
    eye_l_lod1 = DH.make_skinned_lod(eye_l, "GK_Eye_L_LOD1", ratio=0.55, rig_obj=rig, keep_shapekeys=False)
    eye_r_lod1 = DH.make_skinned_lod(eye_r, "GK_Eye_R_LOD1", ratio=0.55, rig_obj=rig, keep_shapekeys=False)
    teeth_lod1 = DH.make_skinned_lod(teeth_obj, "GK_Teeth_LOD1", ratio=0.22, rig_obj=rig, keep_shapekeys=False)
    tongue_lod1 = DH.make_skinned_lod(tongue_obj, "GK_Tongue_LOD1", ratio=0.55, rig_obj=rig, keep_shapekeys=False)
    lod1_meshes = [body_lod1, garment_lod1, hair_lod1, key_lod1, eye_l_lod1, eye_r_lod1, teeth_lod1, tongue_lod1]

    body_lod2 = DH.make_skinned_lod(human, "GK_Body_LOD2", ratio=0.42, rig_obj=rig, keep_shapekeys=True)
    garment_lod2 = DH.make_skinned_lod(garment_obj, "GK_Coat_LOD2", ratio=0.32, rig_obj=rig, keep_shapekeys=False, protect_fn=protect_gk_collar)
    hair_lod2 = DH.make_skinned_lod(hair_obj, "GK_Hair_LOD2", ratio=0.42, rig_obj=rig, keep_shapekeys=False)
    key_lod2 = DH.make_skinned_lod(keyring_obj, "GK_KeyRing_LOD2", ratio=0.50, rig_obj=rig, keep_shapekeys=False)
    eye_l_lod2 = DH.make_skinned_lod(eye_l, "GK_Eye_L_LOD2", ratio=0.30, rig_obj=rig, keep_shapekeys=False)
    eye_r_lod2 = DH.make_skinned_lod(eye_r, "GK_Eye_R_LOD2", ratio=0.30, rig_obj=rig, keep_shapekeys=False)
    teeth_lod2 = DH.make_skinned_lod(teeth_obj, "GK_Teeth_LOD2", ratio=0.09, rig_obj=rig, keep_shapekeys=False)
    tongue_lod2 = DH.make_skinned_lod(tongue_obj, "GK_Tongue_LOD2", ratio=0.32, rig_obj=rig, keep_shapekeys=False)
    lod2_meshes = [body_lod2, garment_lod2, hair_lod2, key_lod2, eye_l_lod2, eye_r_lod2, teeth_lod2, tongue_lod2]

    for m in lod1_meshes + lod2_meshes:
        m.hide_render = True
        m.hide_viewport = True

    build_gatekeeper_animations(rig)
    DH.save_and_export_character("CTCC_CHR_Gatekeeper", rig, lod0_meshes, lod1_meshes, lod2_meshes)


if __name__ == "__main__":
    build_gatekeeper()
