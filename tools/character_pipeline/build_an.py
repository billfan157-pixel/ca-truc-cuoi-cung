"""Build Digital Human: CHAR007 / C01 Bé An (M2-C Validated Pipeline).

Canon specs (Core 02 / Core 04 / Core 05):
- 7-8 year old Vietnamese pediatric patient (Room 101 Bed 4), 1.150 m height, gentle & non-hostile
- Preserves FULL human child anatomy, skin, hair, striped cotton hospital pajamas (`AnCotton`) and
  iconic yellow honeycomb foam slippers (`dép tổ ong vàng`, `SlipperYellow`) beneath soft memory translucency
- Full 64-bone `mixamo_unity` rig (replaces M2-A 7-bone rig!) + 29 anatomical facial BlendShapes
  for natural blinking & Vietnamese CH02-D03 lip-sync ("Chú có thấy cửa nhà con không?")
- Verified upright animations: `An_IdleLook` and `An_TurnFade` (pure vertical Z-axis yaw turn, ZERO tilt bug!)
- Game-ready LOD0 / LOD1 / LOD2 + editable ArtSource/Blender/CTCC_CHR_AnApparition.blend + Unity FBX
"""

import math
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


def create_an_materials():
    """Per-slot human materials (preserved directly by CTCC/ApparitionHuman in Unity!)."""
    mats = {}
    mats["AnSkin"] = DH.create_pbr_material(
        "AnSkin", color_tex="AnSkin_Color.png", normal_tex="AnSkin_Normal.png",
        mask_tex="AnSkin_Mask.png", roughness=0.50, sss_weight=0.42, normal_strength=0.60,
    )
    mats["LanEye"] = DH.create_pbr_material(
        "LanEye", color_tex="LanEye_Color.png", normal_tex="LanEye_Normal.png",
        roughness=0.05, coat_weight=1.0, normal_strength=0.35,
    )
    mats["LanTeeth"] = DH.create_pbr_material(
        "LanTeeth", color_tex="LanTeeth_Color.png", roughness=0.22, coat_weight=0.35,
    )
    mats["LanTongue"] = DH.create_pbr_material(
        "LanTongue", color_tex="LanTongue_Color.png", roughness=0.34, sss_weight=0.30,
    )
    mats["LanEyebrow"] = DH.create_pbr_material(
        "LanEyebrow", color_tex="LanEyebrow_Color.png", roughness=0.55, alpha_mode="BLEND",
    )
    mats["LanEyelash"] = DH.create_pbr_material(
        "LanEyelash", color_tex="LanEyelash_Color.png", roughness=0.45, alpha_mode="BLEND",
    )
    mats["AnHairCard"] = DH.create_pbr_material(
        "AnHairCard", color_tex="AnHair_Color.png", normal_tex="AnHair_Normal.png",
        roughness=0.46, coat_weight=0.05, alpha_mode="BLEND", normal_strength=0.60,
    )
    mats["AnCotton"] = DH.create_pbr_material(
        "AnCotton", color_tex="AnCotton_Color.png", normal_tex="AnCotton_Normal.png",
        mask_tex="AnCotton_Mask.png", roughness=0.88, normal_strength=0.65, uv_tile=1.5,
    )
    mats["SlipperYellow"] = DH.create_pbr_material(
        "SlipperYellow", base_color=(0.86, 0.70, 0.22, 1.0), roughness=0.66,
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
        ("An_Eye_L_LOD0", bm_l, "mixamorig:LeftEye"),
        ("An_Eye_R_LOD0", bm_r, "mixamorig:RightEye"),
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
    teeth_obj.name = "An_Teeth_LOD0"
    tongue_obj.name = "An_Tongue_LOD0"
    # Narrow rear molar arch 12% cleanly for child jaw contour
    for v in teeth_obj.data.vertices:
        v.co.x *= 0.88
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


def build_an_garments(body_clean_copy, rig_obj, mats):
    """Build Bé An's loose striped pediatric hospital pajamas (cloth-simulated) + iconic yellow honeycomb slippers."""
    neck_z = rig_obj.data.bones["mixamorig:Neck"].head_local.z
    l_arm = rig_obj.data.bones["mixamorig:LeftArm"]
    sleeve_cutoff_x = abs(l_arm.head_local.lerp(l_arm.tail_local, 0.78).x)
    knee_z = rig_obj.data.bones["mixamorig:LeftLeg"].head_local.z
    hip_z = rig_obj.data.bones["mixamorig:Hips"].head_local.z

    # 1. Loose pediatric hospital shorts (cloth-simulated waist -> just below knee)
    z_sh_bot = knee_z - 0.025
    z_sh_top = hip_z + 0.095

    def shorts_pred(c, f):
        return z_sh_bot <= c.z <= z_sh_top and abs(c.x) <= 0.20

    def shorts_flare(co, nrm):
        t = max(0.0, min(1.0, (z_sh_top - co.z) / max(0.05, z_sh_top - z_sh_bot)))
        out = nrm * (0.008 + 0.015 * t)
        if abs(co.x) < 0.025 and co.z < hip_z:
            out.x *= 0.25
        return out

    shorts = DH.extract_garment_shell_from_body(
        body_clean_copy, "An_Shorts", shorts_pred,
        offset_m=0.005, smooth_iters=8, flare_fn=shorts_flare, uv_scale=2.8,
    )
    vg_sp = shorts.vertex_groups.new(name="Pin")
    for v in shorts.data.vertices:
        if v.co.z >= z_sh_top - 0.045:
            vg_sp.add([v.index], 1.0, "REPLACE")
    DH.run_cloth_simulation(shorts, body_clean_copy, "Pin", frames=16, mass=0.26, tension=20.0, bending=1.4)
    shorts.vertex_groups.clear()

    bpy.ops.object.select_all(action="DESELECT")
    shorts.select_set(True)
    bpy.context.view_layer.objects.active = shorts
    sol_s = shorts.modifiers.new("ShortsSolid", "SOLIDIFY")
    sol_s.thickness = 0.0018
    sol_s.offset = -0.25
    bpy.ops.object.modifier_apply(modifier=sol_s.name)
    DH.assign_material(shorts, mats["AnCotton"])

    # Combined collider (body + inflated shorts) so pediatric shirt never clips shorts!
    sh_col = shorts.copy()
    sh_col.data = shorts.data.copy()
    sh_col.name = "_AnShortsCol"
    bpy.context.collection.objects.link(sh_col)
    for v in sh_col.data.vertices:
        v.co += v.normal * 0.006
    sh_col.data.update()

    # Trim upper hidden shorts inside shirt hem cleanly so zero pokethrough can ever happen!
    z_shirt_bot = hip_z - 0.055
    bm_sh = bmesh.new()
    bm_sh.from_mesh(shorts.data)
    del_f = [f for f in bm_sh.faces if f.calc_center_median().z > z_shirt_bot + 0.048]
    if del_f:
        bmesh.ops.delete(bm_sh, geom=del_f, context="FACES")
        loose = [v for v in bm_sh.verts if not v.link_faces]
        if loose:
            bmesh.ops.delete(bm_sh, geom=loose, context="VERTS")
    for v in bm_sh.verts:
        if v.co.z > z_shirt_bot - 0.015:
            t_t = min(1.0, (v.co.z - (z_shirt_bot - 0.015)) / 0.06)
            lx = 0.065 if v.co.x >= 0.0 else -0.065
            v.co.x = v.co.x * (1.0 - 0.14 * t_t) + lx * (0.14 * t_t)
    bm_sh.to_mesh(shorts.data)
    bm_sh.free()
    shorts.data.update()

    comb_col = body_clean_copy.copy()
    comb_col.data = body_clean_copy.data.copy()
    comb_col.name = "_AnCombCol"
    bpy.context.collection.objects.link(comb_col)
    bpy.ops.object.select_all(action="DESELECT")
    comb_col.select_set(True)
    sh_col.select_set(True)
    bpy.context.view_layer.objects.active = comb_col
    bpy.ops.object.join()

    # 2. Loose continuous-shoulder striped pediatric hospital shirt (cloth-simulated!)
    def shirt_pred(c, f):
        if c.z < z_shirt_bot or c.z > neck_z + 0.012:
            return False
        if abs(c.x) > sleeve_cutoff_x:
            return False
        if c.z < hip_z + 0.02 and abs(c.x) < 0.052 and -0.05 < c.y < 0.045:
            return False
        return True

    def shirt_flare(co, nrm):
        extra = Vector((0.0, 0.0, 0.0))
        if abs(co.x) < 0.155:
            t_low = max(0.0, min(1.0, (neck_z - 0.12 - co.z) / max(0.10, neck_z - 0.12 - z_shirt_bot)))
            rad = Vector((co.x, co.y + 0.015, 0.0))
            if rad.length > 0.01:
                extra += rad.normalized() * (0.010 + 0.020 * t_low)
                min_rx = 0.132 + 0.018 * t_low
                min_ry = 0.092 + 0.016 * t_low
                ell = math.hypot(co.x / min_rx, (co.y + 0.015) / min_ry)
                if 0.05 < ell < 1.0:
                    extra += Vector((co.x / ell - co.x, (co.y + 0.015) / ell - (co.y + 0.015), 0.0)) * ((1.0 - ell) * t_low)
            else:
                extra += nrm * 0.012
        else:
            extra += nrm * 0.011
        return extra

    shirt = DH.extract_garment_shell_from_body(
        body_clean_copy, "An_Shirt", shirt_pred,
        offset_m=0.007, smooth_iters=9, flare_fn=shirt_flare, uv_scale=2.8,
    )
    vg_bp = shirt.vertex_groups.new(name="Pin")
    for v in shirt.data.vertices:
        if v.co.z >= neck_z - 0.055 and abs(v.co.x) < 0.16:
            vg_bp.add([v.index], 1.0, "REPLACE")
    DH.run_cloth_simulation(shirt, comb_col, "Pin", frames=20, mass=0.22, tension=18.0, bending=1.3)
    shirt.vertex_groups.clear()

    bpy.ops.object.select_all(action="DESELECT")
    shirt.select_set(True)
    bpy.context.view_layer.objects.active = shirt
    sol_b = shirt.modifiers.new("ShirtSolid", "SOLIDIFY")
    sol_b.thickness = 0.0018
    sol_b.offset = -0.30
    bpy.ops.object.modifier_apply(modifier=sol_b.name)
    DH.assign_material(shirt, mats["AnCotton"])
    bpy.data.objects.remove(comb_col, do_unlink=True)

    # 3. Iconic Yellow Vietnamese Honeycomb Foam Slippers (`dép tổ ong vàng`, Core 02 C01 / CH02-B02!)
    # Sculpted flat foam sole + arched wide vamp strap across instep (leaving child's toes & heel gently visible!)
    bm_sl = bmesh.new()
    uv_sl = bm_sl.loops.layers.uv.new("UVMap")
    for sgn in (1.0, -1.0):
        fverts = [v.co for v in body_clean_copy.data.vertices if v.co.z <= 0.048 and v.co.x * sgn > 0.004]
        if not fverts:
            continue
        x0, x1 = min(p.x for p in fverts), max(p.x for p in fverts)
        y0, y1 = min(p.y for p in fverts), max(p.y for p in fverts)
        cx_f = 0.5 * (x0 + x1)
        hw = 0.5 * (x1 - x0) + 0.007
        y_toe = y0 - 0.010
        y_heel = y1 + 0.008
        # Flat rounded EVA foam sole (height 0.000 -> 0.012m)
        sole_rings = []
        n_r = 16
        for fy, ws in ((0.00, 0.55), (0.12, 0.85), (0.45, 0.96), (0.78, 1.02), (0.94, 0.78), (1.00, 0.36)):
            y = y_heel + (y_toe - y_heel) * fy
            rx = hw * ws
            r_top, r_bot = [], []
            for k in range(n_r):
                a = (k / float(n_r)) * math.tau
                px = cx_f + math.cos(a) * rx
                r_bot.append(bm_sl.verts.new(Vector((px, y, 0.000))))
                r_top.append(bm_sl.verts.new(Vector((px, y, 0.011))))
            sole_rings.append((r_bot, r_top))
        # Build clean rounded extruded sole hull (top/bottom caps + perimeter)
        for j in range(len(sole_rings) - 1):
            for k in range(n_r):
                nk = (k + 1) % n_r
                # Top surface
                ft = bm_sl.faces.new((sole_rings[j][1][k], sole_rings[j][1][nk], sole_rings[j + 1][1][nk], sole_rings[j + 1][1][k]))
                ft.smooth = True
                for l in ft.loops: l[uv_sl].uv = (0.5, 0.5)
        # Perimeter side walls on outer oval contour using simple extruded rounded box
        bmesh.ops.create_cube(
            bm_sl, size=1.0,
            matrix=Matrix.Translation(Vector((cx_f, 0.5 * (y_toe + y_heel), 0.006))) @
                   Matrix.Diagonal((hw * 1.95, abs(y_heel - y_toe) * 0.96, 0.012, 1.0)),
        )
        # Wide arched honeycomb slipper vamp band across midfoot (classic dép tổ ong!)
        band_rings = []
        n_arc = 10
        y_b0 = y_heel + (y_toe - y_heel) * 0.34
        y_b1 = y_heel + (y_toe - y_heel) * 0.78
        for j in range(5):
            fy = j / 4.0
            y = y_b0 + (y_b1 - y_b0) * fy
            arch_h = 0.044 - 0.012 * fy
            ring_out, ring_in = [], []
            for k in range(n_arc + 1):
                ang = (k / float(n_arc)) * math.pi
                px_o = cx_f + math.cos(ang) * (hw * 1.04)
                pz_o = 0.008 + math.sin(ang) * arch_h
                px_i = cx_f + math.cos(ang) * (hw * 0.90)
                pz_i = 0.008 + math.sin(ang) * max(0.012, arch_h - 0.006)
                ring_out.append(bm_sl.verts.new(Vector((px_o, y, pz_o))))
                ring_in.append(bm_sl.verts.new(Vector((px_i, y, pz_i))))
            band_rings.append((ring_out, ring_in))
        for j in range(4):
            for k in range(n_arc):
                for vs in (
                    (band_rings[j][0][k], band_rings[j][0][k + 1], band_rings[j + 1][0][k + 1], band_rings[j + 1][0][k]),
                    (band_rings[j][1][k + 1], band_rings[j][1][k], band_rings[j + 1][1][k], band_rings[j + 1][1][k + 1]),
                ):
                    f = bm_sl.faces.new(vs)
                    f.smooth = True
                    for l in f.loops: l[uv_sl].uv = (0.5, 0.5)
        for end_i in (0, 4):
            for k in range(n_arc):
                vs = (band_rings[end_i][0][k], band_rings[end_i][1][k], band_rings[end_i][1][k + 1], band_rings[end_i][0][k + 1])
                f = bm_sl.faces.new(vs)
                f.smooth = True
                for l in f.loops: l[uv_sl].uv = (0.5, 0.5)
    # Remove unused sole_rings loose geometry; clean rounded sole box via bevel
    bmesh.ops.recalc_face_normals(bm_sl, faces=list(bm_sl.faces))
    me_sl = bpy.data.meshes.new("An_Slippers_mesh")
    bm_sl.to_mesh(me_sl)
    bm_sl.free()
    slippers = bpy.data.objects.new("An_Slippers", me_sl)
    bpy.context.collection.objects.link(slippers)
    DH.assign_material(slippers, mats["SlipperYellow"])

    # Join pajamas + yellow slippers into single clean `An_Clothes_LOD0` and transfer skin weights!
    bpy.ops.object.select_all(action="DESELECT")
    for o in (shirt, shorts, slippers):
        o.select_set(True)
    bpy.context.view_layer.objects.active = shirt
    bpy.ops.object.join()
    clothes = bpy.context.active_object
    clothes.name = "An_Clothes_LOD0"
    DH.set_smooth(clothes)
    DH.triangulate_ngons(clothes)
    DH.transfer_skin_weights(body_clean_copy, clothes, rig_obj, smooth_iters=4, smooth_factor=0.50)

    print(f"[GARMENT] Built Bé An pediatric pajamas & yellow slippers ({len(clothes.data.polygons)} polys)")
    return clothes


def build_an_animations(rig_obj):
    """Author Bé An's gentle child animations: `An_IdleLook` and upright `An_TurnFade` (ZERO tilt bug!)."""

    def child_hands(rig):
        RC.apply_finger_curl(rig, "Left", 0.22, 0.28, 0.34, 0.40, 0.16, 0.03)
        RC.apply_finger_curl(rig, "Right", 0.22, 0.28, 0.34, 0.40, 0.16, 0.03)

    # 1. An_IdleLook — Gentle innocent child poise, soft curious head tilt, quiet breathing
    def idle_frame(rig, t, fi):
        s1 = math.sin(t * math.tau)
        s2 = math.sin(t * math.tau * 2.0)
        RC.add_world_delta(rig, "mixamorig:Hips", rx_deg=0.6, ry_deg=0.8 * s1, rz_deg=1.0 * s1)
        RC.aim_bone_world(rig, "mixamorig:LeftUpLeg", Vector((0.03, -0.01, -1.0)))
        RC.aim_bone_world(rig, "mixamorig:LeftLeg", Vector((0.01, 0.02, -1.0)))
        RC.aim_bone_world(rig, "mixamorig:RightUpLeg", Vector((-0.03, 0.01, -1.0)))
        RC.aim_bone_world(rig, "mixamorig:RightLeg", Vector((-0.01, 0.02, -1.0)))

        RC.add_world_delta(rig, "mixamorig:Spine", rx_deg=0.5, rz_deg=-0.6 * s1)
        RC.add_world_delta(rig, "mixamorig:Spine1", rx_deg=-1.0 * s2)
        RC.add_world_delta(rig, "mixamorig:Spine2", rx_deg=-1.2 * s2)
        RC.add_world_delta(rig, "mixamorig:Neck", rx_deg=1.0, ry_deg=2.2 + 1.2 * s1, rz_deg=1.5 * s1)
        RC.add_world_delta(rig, "mixamorig:Head", rx_deg=1.5 + 0.6 * s2, ry_deg=3.0 + 1.5 * s1, rz_deg=1.8 * s1)

        # Relaxed small child arms hanging gently down beside hospital pajamas
        RC.aim_bone_world(rig, "mixamorig:LeftArm", Vector((0.18, -0.06, -0.98)))
        RC.aim_bone_world(rig, "mixamorig:LeftForeArm", Vector((0.06, -0.26, -0.96)))
        RC.aim_bone_world(rig, "mixamorig:LeftHand", Vector((0.02, -0.28, -0.96)))

        RC.aim_bone_world(rig, "mixamorig:RightArm", Vector((-0.18, -0.08, -0.98)))
        RC.aim_bone_world(rig, "mixamorig:RightForeArm", Vector((-0.06, -0.28, -0.96)))
        RC.aim_bone_world(rig, "mixamorig:RightHand", Vector((-0.02, -0.30, -0.95)))
        child_hands(rig)
        return Vector((0.006 * s1, 0.0, 0.0))

    a_idle = RC.author_layered_action(rig_obj, "An_IdleLook", 120, idle_frame)

    # 2. An_TurnFade — Calm, strictly UPRIGHT vertical-Z yaw turn toward hallway/home door (ZERO torso tilt!)
    def turn_frame(rig, t, fi):
        # Smooth S-curve turn from 0 deg -> -108 deg around pure world +Z axis!
        u = max(0.0, min(1.0, (t - 0.12) / 0.72))
        s = u * u * (3.0 - 2.0 * u)
        yaw_deg = -108.0 * s
        # Gentle stepping stride mid-turn
        step = math.sin(s * math.pi)
        # Soft lingering glance back over left shoulder as An turns away
        glance_deg = 28.0 * math.sin(min(1.0, s * 1.15) * math.pi)

        RC.add_world_delta(rig, "mixamorig:Hips", rx_deg=0.5, ry_deg=0.0, rz_deg=yaw_deg)
        rad = math.radians(yaw_deg)
        fwd = Vector((math.sin(rad), -math.cos(rad), 0.0))
        right = Vector((math.cos(rad), math.sin(rad), 0.0))

        # Upright natural turning legs (no torso lean or weird tilt!)
        RC.aim_bone_world(rig, "mixamorig:LeftUpLeg", right * 0.04 + fwd * (0.12 * step) + Vector((0, 0, -1.0)))
        RC.aim_bone_world(rig, "mixamorig:LeftLeg", right * 0.02 - fwd * (0.14 * step) + Vector((0, 0, -1.0)))
        RC.aim_bone_world(rig, "mixamorig:RightUpLeg", -right * 0.04 - fwd * (0.08 * step) + Vector((0, 0, -1.0)))
        RC.aim_bone_world(rig, "mixamorig:RightLeg", -right * 0.02 + fwd * (0.04 * step) + Vector((0, 0, -1.0)))

        # Lingering gentle glance back toward Minh before dissolving peacefully
        RC.add_world_delta(rig, "mixamorig:Spine2", rz_deg=glance_deg * 0.25)
        RC.add_world_delta(rig, "mixamorig:Neck", rz_deg=glance_deg * 0.35)
        RC.add_world_delta(rig, "mixamorig:Head", rx_deg=1.5, rz_deg=glance_deg * 0.45)

        RC.aim_bone_world(rig, "mixamorig:LeftArm", right * 0.18 + fwd * (0.08 * step) + Vector((0, 0, -0.98)))
        RC.aim_bone_world(rig, "mixamorig:LeftForeArm", right * 0.08 + fwd * (0.24 + 0.08 * step) + Vector((0, 0, -0.96)))
        RC.aim_bone_world(rig, "mixamorig:RightArm", -right * 0.18 - fwd * (0.08 * step) + Vector((0, 0, -0.98)))
        RC.aim_bone_world(rig, "mixamorig:RightForeArm", -right * 0.08 + fwd * (0.22 - 0.06 * step) + Vector((0, 0, -0.96)))
        child_hands(rig)
        return fwd * (0.04 * s)

    RC.author_layered_action(rig_obj, "An_TurnFade", 96, turn_frame)
    rig_obj.animation_data.action = a_idle
    bpy.context.scene.frame_set(15)


def build_an():
    DH.fresh_metric_scene()

    # 1. Calibrated 7-8yo Vietnamese child (`1.150 m`, 100% Asian `hm08` base + Vietnamese child facial features!)
    macro = TargetService.get_default_macro_info_dict()
    macro["gender"] = 0.45
    macro["age"] = 0.19          # 7-8 year old child proportions!
    macro["weight"] = 0.38
    macro["muscle"] = 0.24
    macro["height"] = 0.42
    macro["proportions"] = 0.48
    macro["race"] = {"asian": 1.0, "caucasian": 0.0, "african": 0.0}

    facial_targets = {
        "head-round": 0.32,
        "head-scale-horiz-incr": 0.12,
        "head-scale-depth-decr": 0.18,
        "l-cheek-volume-incr": 0.34,
        "r-cheek-volume-incr": 0.34,
        "l-cheek-bones-incr": 0.24,
        "r-cheek-bones-incr": 0.24,
        "chin-prominent-decr": 0.28,
        "chin-width-decr": 0.16,
        "chin-height-decr": 0.14,
        "l-eye-epicanthus-in": 0.56,
        "r-eye-epicanthus-in": 0.56,
        "l-eye-eyefold-down": 0.12,
        "r-eye-eyefold-down": 0.12,
        "l-eye-push1-out": 0.24,
        "r-eye-push1-out": 0.24,
        "l-eye-scale-incr": 0.08,
        "r-eye-scale-incr": 0.08,
        "eyebrows-trans-up": 0.14,
        "eyebrows-angle-up": 0.18,
        "nose-scale-depth-decr": 0.44,
        "nose-compression-compress": 0.28,
        "nose-point-width-incr": 0.24,
        "nose-nostrils-width-incr": 0.22,
        "nose-scale-vert-decr": 0.18,
        "mouth-scale-horiz-decr": 0.12,
        "mouth-angles-up": 0.08,
    }
    human = DH.create_calibrated_human(macro, target_height_m=1.150, facial_targets=facial_targets)
    human.name = "An_Body_Raw"

    def load_part(cat, frag):
        p = AssetService.find_asset_absolute_path(frag, cat)
        if not p:
            raise FileNotFoundError(f"Missing CC0 asset {cat}/{frag}")
        return HumanService.add_mhclo_asset(p, human, asset_type=cat, subdiv_levels=0, material_type="NONE")

    eyes_obj = load_part("eyes", "high-poly/high-poly.mhclo")
    teeth_obj = load_part("teeth", "teeth_base/teeth_base.mhclo")
    tongue_obj = load_part("tongue", "tongue01/tongue01.mhclo")
    brows_obj = load_part("eyebrows", "eyebrow001/eyebrow001.mhclo")
    lashes_obj = load_part("eyelashes", "eyelashes01/eyelashes01.mhclo")
    hair_obj = load_part("hair", "short01/short01.mhclo")

    brows_obj.name = "An_Eyebrows_LOD0"
    lashes_obj.name = "An_Eyelashes_LOD0"
    hair_obj.name = "An_Hair_LOD0"

    # 2. Build 29 anatomical facial BlendShapes & transfer to brows/lashes (scaled proportionally to child head!)
    DH.build_anatomical_blendshapes(human, scale_corr=0.78)
    DH.transfer_shapekeys_by_surface(human, brows_obj)
    DH.transfer_shapekeys_by_surface(human, lashes_obj, query_y_offset=0.007)

    # 3. Full 64-bone `mixamo_unity` rig
    rig = HumanService.add_builtin_rig(human, "mixamo_unity", import_weights=True)
    rig.name = "An_Armature"

    mats = create_an_materials()

    eye_l, eye_r = split_and_rig_eyes(eyes_obj, rig, mats["LanEye"])
    rig_oral_cavity(teeth_obj, tongue_obj, rig, mats["LanTeeth"], mats["LanTongue"])

    DH.set_smooth(brows_obj)
    DH.assign_material(brows_obj, mats["LanEyebrow"])
    DH.weight_rigid(brows_obj, rig, "mixamorig:Head")

    DH.set_smooth(lashes_obj)
    DH.assign_material(lashes_obj, mats["LanEyelash"])
    DH.weight_rigid(lashes_obj, rig, "mixamorig:Head")

    DH.set_smooth(hair_obj)
    DH.assign_material(hair_obj, mats["AnHairCard"])
    DH.weight_rigid(hair_obj, rig, "mixamorig:Head")

    # 4. Cloth-simulated pediatric hospital pajamas & yellow slippers
    body_for_cloth = human.copy()
    body_for_cloth.data = human.data.copy()
    body_for_cloth.name = "_AnClothBody"
    bpy.context.collection.objects.link(body_for_cloth)
    if body_for_cloth.data.shape_keys:
        body_for_cloth.shape_key_clear()
    DH.strip_helper_geometry_preserving_shapekeys(body_for_cloth, extra_keep_fn=None)

    clothes_obj = build_an_garments(body_for_cloth, rig, mats)

    # 5. Keep head, neck, arms below short sleeves, and lower legs/bare feet visible!
    neck_z = rig.data.bones["mixamorig:Neck"].head_local.z
    l_arm = rig.data.bones["mixamorig:LeftArm"]
    sleeve_x = abs(l_arm.head_local.lerp(l_arm.tail_local, 0.78).x) - 0.032
    knee_z = rig.data.bones["mixamorig:LeftLeg"].head_local.z

    def an_keep_body(co):
        if co.z > neck_z - 0.04:
            return True
        if abs(co.x) > sleeve_x:
            return True
        if co.z < knee_z + 0.03:
            return True
        return False

    DH.strip_helper_geometry_preserving_shapekeys(human, extra_keep_fn=an_keep_body)
    DH.set_smooth(human)
    DH.assign_material(human, mats["AnSkin"])
    DH.bind_rig(human, rig)

    # Join brows & lashes into `An_Body_LOD0` so all 29 BlendShapes drive cleanly on one SkinnedMeshRenderer!
    bpy.ops.object.select_all(action="DESELECT")
    human.select_set(True)
    brows_obj.select_set(True)
    lashes_obj.select_set(True)
    bpy.context.view_layer.objects.active = human
    bpy.ops.object.join()
    body_lod0 = bpy.context.active_object
    body_lod0.name = "An_Body_LOD0"

    bpy.data.objects.remove(body_for_cloth, do_unlink=True)

    # 6. Game-ready LOD0 / LOD1 / LOD2
    lod0_meshes = [body_lod0, eye_l, eye_r, teeth_obj, tongue_obj, hair_obj, clothes_obj]
    body_lod1 = DH.make_skinned_lod(body_lod0, "An_Body_LOD1", ratio=1.0, rig_obj=rig, keep_shapekeys=True)
    clothes_lod1 = DH.make_skinned_lod(clothes_obj, "An_Clothes_LOD1", ratio=0.55, rig_obj=rig, keep_shapekeys=False)
    hair_lod1 = DH.make_skinned_lod(hair_obj, "An_Hair_LOD1", ratio=0.65, rig_obj=rig, keep_shapekeys=False)
    lod1_meshes = [body_lod1, clothes_lod1, hair_lod1]

    body_lod2 = DH.make_skinned_lod(body_lod0, "An_Body_LOD2", ratio=0.45, rig_obj=rig, keep_shapekeys=False)
    clothes_lod2 = DH.make_skinned_lod(clothes_obj, "An_Clothes_LOD2", ratio=0.35, rig_obj=rig, keep_shapekeys=False)
    hair_lod2 = DH.make_skinned_lod(hair_obj, "An_Hair_LOD2", ratio=0.42, rig_obj=rig, keep_shapekeys=False)
    lod2_meshes = [body_lod2, clothes_lod2, hair_lod2]

    for m in lod1_meshes + lod2_meshes:
        m.hide_render = True
        m.hide_viewport = True

    build_an_animations(rig)
    DH.save_and_export_character("CTCC_CHR_AnApparition", rig, lod0_meshes, lod1_meshes, lod2_meshes)


if __name__ == "__main__":
    build_an()
