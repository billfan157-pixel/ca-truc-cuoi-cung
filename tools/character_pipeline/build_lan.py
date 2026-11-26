"""Build Hero Benchmark Digital Human: CHAR002 Trần Ngọc Lan (M2-C Iteration 2).

Canon specs (Core 02 §2.02, Core 05 §02):
- 23-year-old Vietnamese nurse (2005), 1.600 m height, natural human appearance & warm exhausted poise
- Neat tied-back dark Vietnamese hair (`tóc buộc gọn`) via verified CC0 MakeHuman `ponytail01` + enamel blue barrette
- Anatomical face/eyes/eyelids/brows/lashes/teeth/tongue/hands (29 FACS + Vietnamese viseme BlendShapes)
- Continuous-shoulder cloth-simulated 2005 Vietnamese nurse blouse (`áo blouse trắng ngả vàng`, MoH 1895/1997
  standing collar & pale-blue trim), tailored dark cotton-twill trousers, anatomical black clinical flats,
  vintage wristwatch, and scratched laminated An Sinh Hospital ID badge (`TRẦN NGỌC LAN`)
- 65-bone articulated rig (`mixamo_unity` + `ClipboardSocket_R` on right hand)
- 5 lifelike performance actions (CMU MoCap `07_04.bvh` & `18_08.bvh` + reference-driven Idle/Listen/Handover)
- Verified game-ready LOD0 / LOD1 / LOD2 + editable ArtSource/Blender/CTCC_CHR_Lan.blend + Unity FBX
"""

import math
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector
from mathutils.bvhtree import BVHTree

sys.path.insert(0, str(Path(__file__).resolve().parent))
import dh_core as DH
import retarget_cmu as RC

from bl_ext.blender_org.mpfb.services.assetservice import AssetService
from bl_ext.blender_org.mpfb.services.humanservice import HumanService
from bl_ext.blender_org.mpfb.services.targetservice import TargetService


def create_lan_materials():
    """Exact Unity-compatible material slots for Lan."""
    mats = {}
    mats["LanSkin"] = DH.create_pbr_material(
        "LanSkin", color_tex="LanSkin_Color.png", normal_tex="LanSkin_Normal.png",
        mask_tex="LanSkin_Mask.png", roughness=0.52, sss_weight=0.38, normal_strength=0.72,
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
    mats["LanHairCard"] = DH.create_pbr_material(
        "LanHairCard", color_tex="LanHair_Color.png", normal_tex="LanHair_Normal.png",
        roughness=0.44, coat_weight=0.06, alpha_mode="BLEND", normal_strength=0.65,
    )
    mats["NurseBlouse"] = DH.create_pbr_material(
        "NurseBlouse", color_tex="NurseBlouse_Color.png", normal_tex="NurseBlouse_Normal.png",
        mask_tex="NurseBlouse_Mask.png", roughness=0.86, normal_strength=0.72, uv_tile=1.6,
    )
    mats["NurseTrim"] = DH.create_pbr_material(
        "NurseTrim", base_color=(0.56, 0.72, 0.74, 1.0), roughness=0.82,
    )
    mats["DarkTrouser"] = DH.create_pbr_material(
        "DarkTrouser", color_tex="DarkTrouser_Color.png", normal_tex="DarkTrouser_Normal.png",
        mask_tex="DarkTrouser_Mask.png", roughness=0.84, normal_strength=0.76, uv_tile=1.5,
    )
    mats["ClinicalShoe"] = DH.create_pbr_material(
        "ClinicalShoe", color_tex="ClinicalShoe_Color.png", normal_tex="ClinicalShoe_Normal.png",
        mask_tex="ClinicalShoe_Mask.png", roughness=0.50, normal_strength=0.65,
    )
    mats["LanBadge"] = DH.create_pbr_material(
        "LanBadge", color_tex="LanBadge.png", roughness=0.26, coat_weight=0.42,
    )
    mats["MetalTrim"] = DH.create_pbr_material(
        "MetalTrim", base_color=(0.74, 0.76, 0.78, 1.0), roughness=0.28, metallic=0.88,
    )
    mats["EnamelBlue"] = DH.create_pbr_material(
        "EnamelBlue", base_color=(0.06, 0.14, 0.24, 1.0), roughness=0.26, coat_weight=0.45,
    )
    return mats


def split_and_rig_eyes(eyes_obj, rig_obj, eye_mat):
    """Strip MakeHuman opaque outer cornea dome, split into Left/Right eyeballs, and bind rigidly."""
    DH.strip_outer_cornea_shell(eyes_obj)

    bm = bmesh.new()
    bm.from_mesh(eyes_obj.data)
    bm_l = bm.copy()
    bm_r = bm.copy()
    bm.free()

    bmesh.ops.delete(bm_l, geom=[v for v in bm_l.verts if v.co.x < 0.0], context="VERTS")
    bmesh.ops.delete(bm_r, geom=[v for v in bm_r.verts if v.co.x >= 0.0], context="VERTS")

    out = []
    for name, bmx, bone_name in (
        ("Lan_Eye_L_LOD0", bm_l, "mixamorig:LeftEye"),
        ("Lan_Eye_R_LOD0", bm_r, "mixamorig:RightEye"),
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
    """Weight upper teeth to Head and lower teeth + tongue to Jaw (narrowing rear gums slightly for zero cheek poke)."""
    teeth_obj.name = "Lan_Teeth_LOD0"
    tongue_obj.name = "Lan_Tongue_LOD0"
    # Gently narrow back molar arch 11% so extreme Jaw_Open never clips through slender Asian cheek walls!
    for v in teeth_obj.data.vertices:
        v.co.x *= 0.89
        if v.co.y > -0.095:
            v.co.y += 0.002
    teeth_obj.data.update()
    zs = [v.co.z for v in teeth_obj.data.vertices]
    z_mid = 0.5 * (min(zs) + max(zs))
    teeth_obj.vertex_groups.clear()
    vg_head = teeth_obj.vertex_groups.new(name="mixamorig:Head")
    vg_jaw = teeth_obj.vertex_groups.new(name="mixamorig:Jaw")
    for v in teeth_obj.data.vertices:
        if v.co.z >= z_mid - 0.001:
            vg_head.add([v.index], 1.0, "REPLACE")
        else:
            vg_jaw.add([v.index], 1.0, "REPLACE")
    DH.set_smooth(teeth_obj)
    DH.assign_material(teeth_obj, teeth_mat)
    DH.bind_rig(teeth_obj, rig_obj)

    DH.set_smooth(tongue_obj)
    DH.assign_material(tongue_obj, tongue_mat)
    DH.weight_rigid(tongue_obj, rig_obj, "mixamorig:Jaw")


def sculpt_lan_craniofacial_features(human_obj):
    """Sculpt authentic 23-year-old Vietnamese female craniofacial anatomy on human_obj before shape-key creation.

    Refines:
    - Palpebral fissure & inter-canthal balance (prevents pinched inner eye corners, opens expressive warm almond eyes)
    - Supratarsal double-eyelid crease & soft night-shift lower-lid tear-trough cushion
    - Anterior malar cheek fullness & softened lateral zygomatic arch + smooth oval V-taper jawline
    - Subtle nasal dorsum, rounded nasal tip/alar crease, defined philtrum columns (`nhân trung`) & natural lip vermilion roll
    """
    me = human_obj.data
    head_verts = [v for v in me.vertices if v.co.z > 1.35]
    if not head_verts:
        return

    # Locate exact eye center height & depth from existing orbital vertices
    eye_region = [v.co for v in head_verts if 0.015 < abs(v.co.x) < 0.048 and v.co.y < -0.065 and 1.45 < v.co.z < 1.53]
    eye_z = sum(p.z for p in eye_region) / len(eye_region) if eye_region else 1.488
    eye_y = min(p.y for p in eye_region) if eye_region else -0.088
    mouth_region = [v.co for v in head_verts if abs(v.co.x) < 0.025 and v.co.y < -0.080 and 1.39 < v.co.z < 1.445]
    mouth_z = sum(p.z for p in mouth_region) / len(mouth_region) if mouth_region else 1.418

    for v in head_verts:
        co = v.co
        x, y, z = co.x, co.y, co.z
        ax = abs(x)
        sgn = 1.0 if x >= 0.0 else -1.0

        # 1. Inter-canthal release & almond palpebral aperture balance
        dz_eye = z - eye_z
        if y < eye_y + 0.028 and abs(dz_eye) < 0.026 and 0.006 < ax < 0.055:
            # Gently release inner canthus outward (+1.35 mm) so eyes never look cross-eyed or pinched
            w_inner = math.exp(-(((ax - 0.016) / 0.008) ** 2) - ((dz_eye / 0.010) ** 2))
            co.x += sgn * 0.00135 * w_inner
            # Crisp upper tarsal arch (+0.85 mm lift at mid-pupil, subtle supratarsal crease recession)
            if dz_eye > 0.0:
                w_ulid = math.exp(-(((ax - 0.031) / 0.013) ** 2) - (((dz_eye - 0.007) / 0.0055) ** 2))
                co.z += 0.00085 * w_ulid
                w_crease = math.exp(-(((ax - 0.031) / 0.014) ** 2) - (((dz_eye - 0.011) / 0.0032) ** 2))
                co.y += 0.00075 * w_crease
            else:
                # Subtle lower-lid aegyo-sal / night-shift tear-trough cushion
                w_llid = math.exp(-(((ax - 0.030) / 0.013) ** 2) - (((dz_eye + 0.008) / 0.0045) ** 2))
                co.y -= 0.00045 * w_llid

        # 2. Anterior malar apple fullness & softened lateral zygomatic width
        if y < -0.025 and 1.415 < z < 1.482:
            w_malar = math.exp(-(((ax - 0.036) / 0.016) ** 2) - (((z - (eye_z - 0.026)) / 0.020) ** 2))
            co.y -= 0.0012 * w_malar
            w_lat_cheek = math.exp(-(((ax - 0.062) / 0.014) ** 2) - (((z - (eye_z - 0.018)) / 0.024) ** 2))
            co.x -= sgn * 0.0015 * w_lat_cheek

        # 3. Smooth oval Vietnamese V-taper jawline & balanced feminine chin
        if 1.365 < z < 1.435 and y < -0.010:
            w_jaw = math.exp(-(((z - 1.398) / 0.026) ** 2)) * max(0.0, min(1.0, (ax - 0.028) / 0.035))
            co.x -= sgn * 0.0018 * w_jaw
            w_chin = math.exp(-((ax / 0.018) ** 2) - (((z - (mouth_z - 0.034)) / 0.014) ** 2))
            if y < -0.070:
                co.y -= 0.0008 * w_chin

        # 4. Defined philtrum columns (`nhân trung`) & fuller natural lip vermilion tubercle
        dz_mouth = z - mouth_z
        if y < -0.078 and abs(dz_mouth) < 0.024 and ax < 0.030:
            if 0.006 < dz_mouth < 0.020 and ax < 0.012:
                # Central philtrum dimple groove + twin ridges
                groove = math.exp(-((ax / 0.0032) ** 2)) - 0.65 * math.exp(-(((ax - 0.0055) / 0.0025) ** 2))
                w_ph = math.sin(((dz_mouth - 0.006) / 0.014) * math.pi)
                co.y += 0.00055 * groove * w_ph
            # Central upper & lower lip vermilion fullness
            w_lip = math.exp(-((ax / 0.015) ** 2) - ((dz_mouth / 0.0085) ** 2))
            co.y -= 0.00085 * w_lip

    me.update()
    print("[SCULPT] Applied 23yo Vietnamese craniofacial anatomical sculpt pass to Lan_Body_Raw")


def finish_lan_hair(hair_obj, human_head_obj, rig_obj, hair_mat, clip_mat):
    """Groom Lan's hair by reshaping CC0 `ponytail01` into a compact low nurse bun + layering 26 arched 3D hair-card ribbons."""
    hair_obj.name = "Lan_Hair_LOD0"
    DH.set_smooth(hair_obj)
    DH.assign_material(hair_obj, hair_mat)

    neck_z = rig_obj.data.bones["mixamorig:Neck"].head_local.z
    head_z = rig_obj.data.bones["mixamorig:Head"].head_local.z

    # 1. Reshape stiff protruding rear ponytail into a neat, compact low occipital nurse bun (`tóc búi thấp gọn gàng`)
    # and gently soften the front hairline step so layered hair cards blend seamlessly!
    bun_center = Vector((0.0, 0.096, neck_z + 0.056))
    for v in hair_obj.data.vertices:
        co = v.co
        # Rear ponytail tail behind occiput -> curl & gather into a rounded low nurse chignon/bun
        if co.y > 0.072 and co.z < head_z + 0.038:
            t_tail = max(0.0, min(1.0, (co.y - 0.072) / 0.075))
            rel = co - bun_center
            # Compress long dangling tail into a neat 6.4cm x 4.6cm x 5.0cm woven bun sphere
            target_pt = bun_center + Vector((
                rel.x * (0.78 - 0.22 * t_tail),
                max(-0.012, min(0.036, rel.y * 0.42)),
                max(-0.026, min(0.026, (rel.z + 0.022 * t_tail) * 0.48)),
            ))
            v.co = co.lerp(target_pt, 0.82 * t_tail)
        # Front forehead hairline margin -> seat closer to scalp so layered framing cards lead the silhouette
        elif co.y < -0.055 and co.z > head_z + 0.010:
            v.co.y += 0.0014
    hair_obj.data.update()

    # 2. Layer 22 multi-segment arched 3D hair-card ribbons (`build_hair_card_strip`) hugged tightly to the hair cap & hairline BVH
    cap_bm = bmesh.new()
    cap_bm.from_mesh(hair_obj.data)
    bmesh.ops.triangulate(cap_bm, faces=cap_bm.faces)
    cap_bvh = BVHTree.FromBMesh(cap_bm)
    cap_bm.free()

    head_bm = bmesh.new()
    head_bm.from_mesh(human_head_obj.data)
    bmesh.ops.triangulate(head_bm, faces=head_bm.faces)
    head_bvh = BVHTree.FromBMesh(head_bm)
    head_bm.free()

    bm_h = bmesh.new()
    bm_h.from_mesh(hair_obj.data)
    uv_h = bm_h.loops.layers.uv.active or bm_h.loops.layers.uv.new("UVMap")

    h_zs = [v.co.z for v in bm_h.verts]
    crown_z = max(h_zs) - 0.010
    part_x = -0.012  # natural subtle left-of-center part

    # Layer A: 12 Front Hairline & Temple Framing Locks (sampled from dense vertical strand strips in LanHair_Color.png!)
    front_specs = [
        (-1.0, -0.004, -0.060, -0.048, -0.072, 0.042, -0.068, 0.010, 0.016, 0.009, 0.15, 0.34),
        (-1.0, -0.012, -0.066, -0.056, -0.066, 0.032, -0.070, -0.002, 0.017, 0.010, 0.17, 0.35),
        (-1.0, -0.022, -0.070, -0.063, -0.056, 0.022, -0.070, -0.012, 0.016, 0.009, 0.65, 0.84),
        (-1.0, -0.034, -0.066, -0.067, -0.044, 0.012, -0.066, -0.020, 0.015, 0.008, 0.18, 0.36),
        (-1.0, -0.044, -0.056, -0.069, -0.030, 0.002, -0.058, -0.026, 0.014, 0.008, 0.66, 0.85),
        (-1.0, -0.008, -0.072, -0.042, -0.076, 0.046, -0.062, 0.018, 0.013, 0.006, 0.22, 0.36),
        (1.0, 0.004, -0.060, 0.046, -0.073, 0.042, 0.066, 0.010, 0.016, 0.009, 0.15, 0.34),
        (1.0, 0.012, -0.067, 0.055, -0.067, 0.032, 0.069, -0.002, 0.017, 0.010, 0.17, 0.35),
        (1.0, 0.022, -0.070, 0.062, -0.056, 0.022, 0.069, -0.012, 0.016, 0.009, 0.65, 0.84),
        (1.0, 0.034, -0.066, 0.067, -0.044, 0.012, 0.066, -0.020, 0.015, 0.008, 0.18, 0.36),
        (1.0, 0.044, -0.056, 0.069, -0.030, 0.002, 0.058, -0.026, 0.014, 0.008, 0.66, 0.85),
        (1.0, 0.008, -0.072, 0.040, -0.076, 0.046, 0.060, 0.018, 0.013, 0.006, 0.22, 0.36),
    ]
    for _sgn, sx, sy, tx, ty, tz_off, ex, ez_off, wr, wt, u0, u1 in front_specs:
        p0_raw = Vector((part_x + sx, sy, crown_z - 0.008))
        p1_raw = Vector((tx * 0.75, ty - 0.004, head_z + tz_off + 0.010))
        p2_raw = Vector((tx, ty, head_z + tz_off))
        p3_raw = Vector((ex, ty + 0.022, head_z + ez_off))
        anchors = [p0_raw, p1_raw, p2_raw, p3_raw]
        pts, nrms = [], []
        n_seg = 6
        for k in range(n_seg):
            u = k / float(n_seg - 1)
            iu = 1.0 - u
            raw_p = (anchors[0] * (iu ** 3) + anchors[1] * (3 * u * (iu ** 2)) +
                     anchors[2] * (3 * (u ** 2) * iu) + anchors[3] * (u ** 3))
            proj_p, proj_n = DH.project_to_surface(cap_bvh, raw_p, offset_m=0.0011)
            pts.append(proj_p)
            nrms.append(proj_n)
        DH.build_hair_card_strip(bm_h, uv_h, pts, nrms, wr, wt, u0, u1, arch=0.0009, v_root=0.84, v_tip=0.22)

    # Layer B: 6 Woven Low-Bun Wrap Cards + 4 Delicate Sideburn/Nape Baby-Hair Wisps (real dense strand UVs!)
    for bi in range(6):
        ang0 = (bi / 6.0) * math.tau
        r_bun = 0.025
        pts, nrms = [], []
        for k in range(5):
            ang = ang0 + (k / 4.0) * 1.35
            offset = Vector((math.cos(ang) * r_bun, 0.016 + 0.008 * math.sin(k * 0.8), math.sin(ang) * r_bun * 0.85))
            p = bun_center + offset
            n = offset.normalized()
            pts.append(p)
            nrms.append(n)
        DH.build_hair_card_strip(bm_h, uv_h, pts, nrms, 0.015, 0.010, 0.16, 0.35, arch=0.0014, v_root=0.82, v_tip=0.28)

    for sgn in (-1.0, 1.0):
        for wisp_pts in (
            [Vector((0.066 * sgn, -0.022, head_z + 0.002)),
             Vector((0.067 * sgn, -0.024, head_z - 0.015)),
             Vector((0.064 * sgn, -0.020, head_z - 0.030))],
            [Vector((0.040 * sgn, 0.056, neck_z + 0.040)),
             Vector((0.034 * sgn, 0.052, neck_z + 0.020)),
             Vector((0.028 * sgn, 0.046, neck_z + 0.002))],
        ):
            pts, nrms = [], []
            for rp in wisp_pts:
                pp, pn = DH.project_to_surface(head_bvh, rp, offset_m=0.0015)
                pts.append(pp)
                nrms.append(pn)
            DH.build_hair_card_strip(bm_h, uv_h, pts, nrms, 0.011, 0.005, 0.66, 0.84, arch=0.0006, v_root=0.80, v_tip=0.18)

    bmesh.ops.recalc_face_normals(bm_h, faces=list(bm_h.faces))
    bm_h.to_mesh(hair_obj.data)
    bm_h.free()
    hair_obj.data.update()
    DH.set_smooth(hair_obj)

    # Weight crown & hairline rigidly to Head, softly blending lower bun/nape tips into Neck
    hair_obj.vertex_groups.clear()
    vg_head = hair_obj.vertex_groups.new(name="mixamorig:Head")
    vg_neck = hair_obj.vertex_groups.new(name="mixamorig:Neck")
    for v in hair_obj.data.vertices:
        if v.co.y > 0.035 and v.co.z < neck_z + 0.048:
            t = max(0.0, min(0.35, (neck_z + 0.048 - v.co.z) / 0.12))
            vg_head.add([v.index], 1.0 - t, "REPLACE")
            vg_neck.add([v.index], t, "REPLACE")
        else:
            vg_head.add([v.index], 1.0, "REPLACE")
    DH.bind_rig(hair_obj, rig_obj)

    # Seat Lan's curved dark-blue enamel nurse barrette (`Lan_HairClip_LOD0`) clasping the top of the low bun
    clip_center = bun_center + Vector((0.0, 0.016, 0.012))

    bm_c = bmesh.new()
    uv_c = bm_c.loops.layers.uv.new("UVMap")
    # Sleek curved enamel nurse barrette (5.8cm wide x 1.3cm tall) clasping the tied ponytail
    nx, nz = 14, 4
    w_bar, h_bar, th_bar = 0.058, 0.013, 0.005
    front_grid, back_grid = [], []
    for j in range(nz + 1):
        vf = j / float(nz) - 0.5
        fr, bk = [], []
        for i in range(nx + 1):
            uf = i / float(nx) - 0.5
            ang = uf * math.radians(82.0)
            r = 0.032
            px = math.sin(ang) * r
            py = (math.cos(ang) - 1.0) * r * 0.75
            pz = vf * h_bar
            fr.append(bm_c.verts.new(clip_center + Vector((px, py + th_bar, pz))))
            bk.append(bm_c.verts.new(clip_center + Vector((px, py, pz))))
        front_grid.append(fr)
        back_grid.append(bk)

    def add_q(vs):
        f = bm_c.faces.new(vs)
        f.smooth = True
        for l, uv in zip(f.loops, ((0, 0), (1, 0), (1, 1), (0, 1))):
            l[uv_c].uv = uv

    for j in range(nz):
        for i in range(nx):
            add_q((front_grid[j][i], front_grid[j][i + 1], front_grid[j + 1][i + 1], front_grid[j + 1][i]))
            add_q((back_grid[j][i + 1], back_grid[j][i], back_grid[j + 1][i], back_grid[j + 1][i + 1]))
    for i in range(nx):
        add_q((front_grid[0][i + 1], front_grid[0][i], back_grid[0][i], back_grid[0][i + 1]))
        add_q((front_grid[nz][i], front_grid[nz][i + 1], back_grid[nz][i + 1], back_grid[nz][i]))
    for j in range(nz):
        add_q((front_grid[j][0], front_grid[j + 1][0], back_grid[j + 1][0], back_grid[j][0]))
        add_q((front_grid[j + 1][nx], front_grid[j][nx], back_grid[j][nx], back_grid[j + 1][nx]))

    bmesh.ops.recalc_face_normals(bm_c, faces=list(bm_c.faces))
    me_c = bpy.data.meshes.new("Lan_HairClip_mesh")
    bm_c.to_mesh(me_c)
    bm_c.free()
    clip_obj = bpy.data.objects.new("Lan_HairClip_LOD0", me_c)
    bpy.context.collection.objects.link(clip_obj)
    DH.assign_material(clip_obj, clip_mat)
    DH.weight_rigid(clip_obj, rig_obj, "mixamorig:Head")

    print(f"[HAIR] Configured CC0 ponytail01 Lan_Hair_LOD0 ({len(hair_obj.data.polygons)} polys) + Enamel Barrette")
    return hair_obj, clip_obj


def _project_patch_onto_bvh(bvh, name, center_x, z_bot, z_top, width, thickness, offset_m, mat, nx=6, nz=8,
                            trim_top_rows=0, trim_mat=None):
    """Build a crisp all-quad rectangular garment patch (placket, pocket, ID badge) conformed strictly to a cloth BVH."""
    bm = bmesh.new()
    uv_layer = bm.loops.layers.uv.new("UVMap")
    front_grid, back_grid = [], []

    for j in range(nz + 1):
        vf = j / float(nz)
        z = z_bot + (z_top - z_bot) * vf
        row_f, row_b = [], []
        for i in range(nx + 1):
            uf = i / float(nx)
            x = center_x + (uf - 0.5) * width
            # Raycast straight backward (+Y) from front (-0.45) onto draped blouse surface!
            hit, nrm, _idx, _dist = bvh.ray_cast(Vector((x, -0.45, z)), Vector((0.0, 1.0, 0.0)), 0.70)
            if hit is None:
                hit, nrm, _idx, _dist = bvh.find_nearest(Vector((x, -0.15, z)))
            nrm = nrm.normalized() if nrm is not None else Vector((0.0, -1.0, 0.0))
            if nrm.y > 0.0:
                nrm = -nrm
            row_b.append(bm.verts.new(hit + nrm * offset_m))
            row_f.append(bm.verts.new(hit + nrm * (offset_m + thickness)))
        front_grid.append(row_f)
        back_grid.append(row_b)

    for j in range(nz):
        is_trim = (trim_top_rows > 0 and j >= nz - trim_top_rows)
        for i in range(nx):
            f = bm.faces.new((front_grid[j][i], front_grid[j][i + 1], front_grid[j + 1][i + 1], front_grid[j + 1][i]))
            f.smooth = True
            f.material_index = 1 if (is_trim and trim_mat is not None) else 0
            u0, u1 = i / float(nx), (i + 1) / float(nx)
            v0, v1 = j / float(nz), (j + 1) / float(nz)
            for l, uv in zip(f.loops, ((u0, v0), (u1, v0), (u1, v1), (u0, v1))):
                l[uv_layer].uv = uv
    # Closed outer rim walls (all quads!)
    for i in range(nx):
        for vs, mi in (
            ((front_grid[0][i + 1], front_grid[0][i], back_grid[0][i], back_grid[0][i + 1]), 0),
            ((front_grid[nz][i], front_grid[nz][i + 1], back_grid[nz][i + 1], back_grid[nz][i]), 1 if (trim_top_rows > 0 and trim_mat) else 0),
        ):
            f = bm.faces.new(vs)
            f.smooth = True
            f.material_index = mi
            for l, uv in zip(f.loops, ((0, 0), (1, 0), (1, 1), (0, 1))):
                l[uv_layer].uv = uv
    for j in range(nz):
        mi = 1 if (trim_top_rows > 0 and j >= nz - trim_top_rows and trim_mat) else 0
        for vs in (
            (front_grid[j][0], front_grid[j + 1][0], back_grid[j + 1][0], back_grid[j][0]),
            (front_grid[j + 1][nx], front_grid[j][nx], back_grid[j][nx], back_grid[j + 1][nx]),
        ):
            f = bm.faces.new(vs)
            f.smooth = True
            f.material_index = mi
            for l, uv in zip(f.loops, ((0, 0), (1, 0), (1, 1), (0, 1))):
                l[uv_layer].uv = uv

    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    me = bpy.data.meshes.new(name + "_mesh")
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    if trim_mat is not None:
        obj.data.materials.append(trim_mat)
    return obj


def build_lan_garments(body_clean_copy, rig_obj, mats):
    """Build Lan's complete 2005 Vietnamese nurse uniform using continuous body-extracted all-quad patterns,
    sequential two-stage Blender Cloth Simulation (trousers -> combined collider -> continuous-shoulder blouse),
    surface-projected pockets/placket/ID badge, anatomical clinical slip-on flats, and wristwatch."""

    # ------------------------------------------------------------------
    # STAGE 1: TAILORED INSTITUTIONAL DARK COTTON-TWILL TROUSERS
    # Extend ankles cleanly to z = 0.036 so trousers drape over flat clinical shoes with ZERO air gap!
    # ------------------------------------------------------------------
    def trouser_pred(c, f):
        return 0.036 <= c.z <= 0.960 and abs(c.x) <= 0.245

    def trouser_flare(co, nrm):
        # Relaxed straight-leg hospital trousers: gentle ease over hips/thighs, straighter drape over knees & calves!
        extra_r = 0.008
        if co.z < 0.72:
            # Straight-leg trouser drape from upper thigh down to ankle (prevents skin-tight legging look!)
            t_leg = max(0.0, min(1.0, (0.72 - co.z) / 0.65))
            extra_r += 0.015 * t_leg
        # Slightly fill inner thigh gap cleanly without merging left & right legs
        out = nrm * extra_r
        if abs(co.x) < 0.032 and co.z < 0.74:
            out.x *= 0.25
        return out

    trousers = DH.extract_garment_shell_from_body(
        body_clean_copy, "Lan_Trousers", trouser_pred,
        offset_m=0.006, smooth_iters=10, subdiv_levels=0, flare_fn=trouser_flare, uv_scale=2.2,
    )
    # Pin waistband & upper seat lightly so trousers stay seated at waist while legs drape with gravity
    vg_tpin = trousers.vertex_groups.new(name="Pin")
    for v in trousers.data.vertices:
        if v.co.z >= 0.875:
            vg_tpin.add([v.index], 1.0, "REPLACE")
        elif v.co.z >= 0.80:
            vg_tpin.add([v.index], (v.co.z - 0.80) / 0.075, "REPLACE")
    DH.run_cloth_simulation(trousers, body_clean_copy, "Pin", frames=18, mass=0.36, tension=24.0, bending=2.2)
    trousers.vertex_groups.clear()

    # Give trouser ankle hems subtle fabric thickness (2.0 mm solidify)
    bpy.ops.object.select_all(action="DESELECT")
    trousers.select_set(True)
    bpy.context.view_layer.objects.active = trousers
    sol_t = trousers.modifiers.new("HemSolid", "SOLIDIFY")
    sol_t.thickness = 0.0022
    sol_t.offset = -0.25
    bpy.ops.object.modifier_apply(modifier=sol_t.name)
    DH.assign_material(trousers, mats["DarkTrouser"])

    # ------------------------------------------------------------------
    # STAGE 2: COMBINED COLLIDER (BODY + INFLATED DRAPED TROUSERS)
    # Guarantees ZERO hip/trouser pokethrough under the white nurse blouse!
    # ------------------------------------------------------------------
    tr_col = trousers.copy()
    tr_col.data = trousers.data.copy()
    tr_col.name = "_LanTrouserCol"
    bpy.context.collection.objects.link(tr_col)
    # Inflate trouser collider by +7mm so blouse drapes comfortably OUTSIDE trousers with generous air gap!
    for v in tr_col.data.vertices:
        v.co += v.normal * 0.0075
    tr_col.data.update()

    # Now trim hidden upper trouser pelvis/thigh polygons cleanly tucked inside the blouse skirt (z > 0.745),
    # and softly taper the top tucked trouser edge inward so trousers can NEVER poke through the blouse hem in ANY pose!
    bm_tr = bmesh.new()
    bm_tr.from_mesh(trousers.data)
    del_tr = [f for f in bm_tr.faces if f.calc_center_median().z > 0.748]
    if del_tr:
        bmesh.ops.delete(bm_tr, geom=del_tr, context="FACES")
        loose = [v for v in bm_tr.verts if not v.link_faces]
        if loose:
            bmesh.ops.delete(bm_tr, geom=loose, context="VERTS")
    for v in bm_tr.verts:
        if v.co.z > 0.655:
            t_tuck = min(1.0, (v.co.z - 0.655) / 0.090)
            # Pull tucked upper trouser leg gently toward leg center so blouse hem floats cleanly outside!
            leg_cx = 0.092 if v.co.x >= 0.0 else -0.092
            v.co.x = v.co.x * (1.0 - 0.14 * t_tuck) + leg_cx * (0.14 * t_tuck)
            v.co.y = v.co.y * (1.0 - 0.14 * t_tuck) - 0.015 * (0.14 * t_tuck)
    bm_tr.to_mesh(trousers.data)
    bm_tr.free()
    trousers.data.update()

    comb_col = body_clean_copy.copy()
    comb_col.data = body_clean_copy.data.copy()
    comb_col.name = "_LanCombinedCollider"
    bpy.context.collection.objects.link(comb_col)
    bpy.ops.object.select_all(action="DESELECT")
    comb_col.select_set(True)
    tr_col.select_set(True)
    bpy.context.view_layer.objects.active = comb_col
    bpy.ops.object.join()

    # Smooth & bridge the raw naked breast cones on `comb_col` into a dignified inner-garment bust bridge
    # so the woven cotton nurse coat NEVER vacuum-clings into cleavage or under-bust during cloth simulation!
    for v in comb_col.data.vertices:
        co = v.co
        if 0.88 <= co.z <= 1.28 and co.y < -0.01 and abs(co.x) < 0.175:
            wz = math.sin(max(0.0, min(1.0, (co.z - 0.88) / 0.40)) * math.pi)
            wx = max(0.0, 1.0 - (abs(co.x) / 0.175) ** 2)
            # Smooth convex inner-garment support envelope across sternum, bust & under-bust
            bridge_y = -0.130 + 0.024 * ((abs(co.x) / 0.165) ** 2) + 0.012 * abs(co.z - 1.12) / 0.20
            if co.y < bridge_y:
                # Compress sharp naked nipple cones back into smooth inner support
                co.y = co.y * (1.0 - 0.85 * wz) + bridge_y * (0.85 * wz)
            else:
                # Bridge the recessed sternum valley & under-bust hollow smoothly forward
                co.y = co.y + (bridge_y - co.y) * (0.90 * wz * wx)
    comb_col.data.update()

    # ------------------------------------------------------------------
    # STAGE 3: CONTINUOUS-SHOULDER 2005 VIETNAMESE NURSE BLOUSE (`ÁO BLOUSE`)
    # ------------------------------------------------------------------
    # Measure left/right wrist X so 3/4 sleeves terminate cleanly above Lan's mid-forearms (~0.365 m)
    l_fore = rig_obj.data.bones["mixamorig:LeftForeArm"]
    sleeve_cutoff_x = abs(l_fore.head_local.lerp(l_fore.tail_local, 0.56).x)
    neck_base_z = rig_obj.data.bones["mixamorig:Neck"].head_local.z

    def blouse_pred(c, f):
        if c.z < 0.675 or c.z > neck_base_z + 0.002:
            return False
        # Carve clean V-neck throat & collar bed so folded Lan_Collar sits cleanly over the blouse without burial!
        if c.y < -0.028 and abs(c.x) < 0.054 and c.z > neck_base_z - 0.038 * (1.0 - abs(c.x) / 0.054):
            return False
        # Exclude forearms/hands past 3/4 nurse sleeve cutoff
        if abs(c.x) > sleeve_cutoff_x:
            return False
        # Exclude inward-facing inner-thigh/crotch bridge faces so the lower coat skirt stays 100% open & flat!
        if c.z < 0.825 and abs(c.x) < 0.068 and -0.065 < c.y < 0.055:
            return False
        return True

    def blouse_flare(co, nrm):
        extra = Vector((0.0, 0.0, 0.0))
        if abs(co.x) < 0.215:
            if co.z < 1.02:
                t_hip = max(0.0, min(1.0, (1.02 - co.z) / 0.34))
                rad = Vector((co.x, co.y + 0.02, 0.0))
                if rad.length > 0.01:
                    rad_n = rad.normalized()
                    extra += rad_n * (0.018 + 0.030 * t_hip)
                    min_rx = 0.185 + 0.025 * t_hip
                    min_ry = 0.125 + 0.022 * t_hip
                    ell = math.hypot(co.x / min_rx, (co.y + 0.02) / min_ry)
                    if ell < 1.0 and ell > 0.05:
                        push = (1.0 - ell) * t_hip
                        extra += Vector((co.x / ell - co.x, (co.y + 0.02) / ell - (co.y + 0.02), 0.0)) * push
                else:
                    extra += nrm * (0.018 + 0.024 * t_hip)
            else:
                extra += nrm * 0.010
            # Dignified loose-draping Vietnamese medical coat front (bridges cleanly across sternum & under-bust)
            if 0.88 <= co.z <= 1.26 and co.y < -0.02 and abs(co.x) < 0.165:
                target_y = -0.142 + 0.024 * (abs(co.x) / 0.160) ** 2 + 0.010 * max(0.0, (1.06 - co.z) / 0.12)
                w_z = math.sin(max(0.0, min(1.0, (co.z - 0.88) / 0.38)) * math.pi)
                w_x = max(0.0, 1.0 - (abs(co.x) / 0.165) ** 2)
                extra.y += (target_y - co.y) * 0.88 * w_z * w_x
        else:
            extra += nrm * 0.012
        return extra

    blouse = DH.extract_garment_shell_from_body(
        body_clean_copy, "Lan_Blouse", blouse_pred,
        offset_m=0.008, smooth_iters=14, subdiv_levels=0, flare_fn=blouse_flare, uv_scale=2.4,
    )

    # Assign material slots: 0 = NurseBlouse, 1 = NurseTrim (pale-blue MoH 1895/1997 trim!)
    blouse.data.materials.clear()
    blouse.data.materials.append(mats["NurseBlouse"])
    blouse.data.materials.append(mats["NurseTrim"])

    bm_b = bmesh.new()
    bm_b.from_mesh(blouse.data)
    uv_b = bm_b.loops.layers.uv.active or bm_b.loops.layers.uv.new("UVMap")
    bm_b.edges.ensure_lookup_table()
    bm_b.faces.ensure_lookup_table()

    # Smooth the open V-neckline boundary ring before cloth simulation (folded collar is attached post-sim in Stage 4!)
    neck_edges = [
        e for e in bm_b.edges
        if e.is_boundary
        and all(v.co.z > neck_base_z - 0.055 and abs(v.co.x) < 0.125 for v in e.verts)
    ]
    if neck_edges:
        neck_verts = list({v for e in neck_edges for v in e.verts})
        for _ in range(4):
            bmesh.ops.smooth_vert(bm_b, verts=neck_verts, factor=0.45, use_axis_x=True, use_axis_y=True, use_axis_z=True)

    # Extrude crisp pale-blue cuff trim band on both 3/4 sleeve boundary loops!
    for sgn in (-1.0, 1.0):
        cuff_edges = [
            e for e in bm_b.edges
            if e.is_boundary
            and all(v.co.x * sgn > sleeve_cutoff_x - 0.035 for v in e.verts)
        ]
        if cuff_edges:
            ret = bmesh.ops.extrude_edge_only(bm_b, edges=cuff_edges)
            new_verts = [g for g in ret["geom"] if isinstance(g, bmesh.types.BMVert)]
            new_faces = [g for g in ret["geom"] if isinstance(g, bmesh.types.BMFace)]
            for v in new_verts:
                v.co += Vector((sgn * 0.012, -0.002, -0.011))
            for f in new_faces:
                f.smooth = True
                f.material_index = 1  # NurseTrim pale-blue cuff band!
                for l in f.loops:
                    l[uv_b].uv = (0.5, 0.5)

    bmesh.ops.recalc_face_normals(bm_b, faces=list(bm_b.faces))
    bm_b.to_mesh(blouse.data)
    bm_b.free()
    blouse.data.update()

    # Run real Blender Cloth Simulation on the continuous-shoulder blouse over `_LanCombinedCollider`!
    vg_bpin = blouse.vertex_groups.new(name="Pin")
    for v in blouse.data.vertices:
        if v.co.z >= neck_base_z - 0.090 and abs(v.co.x) < 0.24:
            vg_bpin.add([v.index], 1.0, "REPLACE")
        elif 0.90 <= v.co.z <= neck_base_z - 0.090 and v.co.y < -0.03 and abs(v.co.x) < 0.155:
            vg_bpin.add([v.index], 0.80, "REPLACE")
        elif abs(v.co.x) >= 0.22 and v.co.z > 1.18:
            vg_bpin.add([v.index], 0.85, "REPLACE")
    DH.run_cloth_simulation(blouse, comb_col, "Pin", frames=24, mass=0.25, tension=24.0, bending=2.4)
    blouse.vertex_groups.clear()

    # Post-sim Sternum & Bust Tension Bridge + Smooth Pass: starched woven cotton poplin drapes cleanly with zero bust-tip cones
    for v in blouse.data.vertices:
        co = v.co
        if 0.88 <= co.z <= 1.25 and co.y < -0.03 and abs(co.x) < 0.165:
            wz = math.sin(max(0.0, min(1.0, (co.z - 0.88) / 0.37)) * math.pi)
            wx = max(0.0, 1.0 - (abs(co.x) / 0.165) ** 2)
            coat_front_y = -0.145 + 0.026 * ((abs(co.x) / 0.160) ** 2) + 0.014 * ((co.z - 1.11) / 0.18) ** 2
            co.y = co.y * (1.0 - 0.94 * wz * wx) + coat_front_y * (0.94 * wz * wx)
    bm_sm = bmesh.new()
    bm_sm.from_mesh(blouse.data)
    chest_verts = [v for v in bm_sm.verts if 0.94 <= v.co.z <= 1.24 and v.co.y < -0.045 and abs(v.co.x) < 0.16]
    if chest_verts:
        for _ in range(10):
            bmesh.ops.smooth_vert(bm_sm, verts=chest_verts, factor=0.48, use_axis_x=False, use_axis_y=True, use_axis_z=False)
    bm_sm.to_mesh(blouse.data)
    bm_sm.free()
    blouse.data.update()

    # Apply subtle clean cloth thickness (2.2 mm) so hem & cuffs have solid tailored edges!
    bpy.ops.object.select_all(action="DESELECT")
    blouse.select_set(True)
    bpy.context.view_layer.objects.active = blouse
    sol_b = blouse.modifiers.new("BlouseSolid", "SOLIDIFY")
    sol_b.thickness = 0.0022
    sol_b.offset = -0.35
    bpy.ops.object.modifier_apply(modifier=sol_b.name)

    # Remove temporary combined collider
    bpy.data.objects.remove(comb_col, do_unlink=True)

    # ------------------------------------------------------------------
    # STAGE 4: POST-SIM FOLDED NURSE COLLAR, PLACKET, BUTTONS, POCKETS & FLAT RIGID ID BADGE
    # ------------------------------------------------------------------
    blouse_bvh = BVHTree.FromObject(blouse, bpy.context.evaluated_depsgraph_get())
    detail_objs = []

    # Post-Sim Parametric 28-Segment Folded Vietnamese Nurse Collar (`cổ bẻ áo blouse điều dưỡng viền xanh`
    # per MoH 1895/1997/QĐ-BYT): built post-sim so cloth physics never crumples the crisp folded collar leaf!
    bm_col = bmesh.new()
    uv_col = bm_col.loops.layers.uv.new("UVMap")
    n_col = 28
    col_rings = []
    # Angles from left front lapel (-54 deg from +X, i.e. x>0, y<0) around back (+pi/2, y>0) to right front lapel (+234 deg, x<0, y<0)
    ang_start = -math.radians(52.0)
    ang_end = math.pi + math.radians(52.0)
    neck_cy = -0.012
    for ri in range(5):
        ring_vs = []
        for ci in range(n_col + 1):
            uf = ci / float(n_col)
            theta = ang_start + (ang_end - ang_start) * uf
            back_w = math.sin(uf * math.pi)
            front_w = 1.0 - back_w
            rx0, ry0 = 0.056, 0.054
            dx = math.cos(theta)
            dy = math.sin(theta)
            base_z = neck_base_z + 0.002 - 0.018 * front_w
            if ri == 0:
                r_scale, dz = 1.00, -0.008
            elif ri == 1:
                r_scale, dz = 0.99, 0.012 + 0.005 * back_w
            elif ri == 2:
                r_scale, dz = 1.07, 0.023 + 0.006 * back_w
            elif ri == 3:
                # Folded outer collar leaf flaring cleanly OVER shoulders & front V-lapels
                r_scale = 1.38 + 0.18 * front_w
                dz = 0.004 - 0.009 * front_w
            else:
                # Crisp 5.5 mm pale-blue MoH 1895/1997 piping border (`NurseTrim`, material index 1)
                r_scale = 1.49 + 0.22 * front_w
                dz = -0.002 - 0.013 * front_w
            px = dx * rx0 * r_scale
            py = neck_cy + dy * ry0 * r_scale
            pz = base_z + dz
            pt = Vector((px, py, pz))
            # Push outer folded leaf & blue piping rings strictly OUTSIDE blouse_bvh so ZERO collar burial occurs!
            if ri >= 2:
                rad_dir = Vector((dx, dy, 0.32)).normalized()
                # Check outward ray from neck axis + downward ray from above shoulder onto draped blouse
                axis_pt = Vector((0.0, neck_cy, pz - 0.006))
                hit_out, nrm_out, _, _ = blouse_bvh.ray_cast(axis_pt, Vector((dx, dy, 0.0)).normalized(), 0.24)
                if hit_out is not None:
                    r_hit = math.hypot(hit_out.x, hit_out.y - neck_cy)
                    r_cur = math.hypot(pt.x, pt.y - neck_cy)
                    clearance = 0.0045 if ri == 2 else (0.0065 if ri == 3 else 0.0080)
                    if r_cur < r_hit + clearance:
                        scale_push = (r_hit + clearance) / max(r_cur, 1e-4)
                        pt.x *= scale_push
                        pt.y = neck_cy + (pt.y - neck_cy) * scale_push
                hit_top, nrm_top, _, _ = blouse_bvh.ray_cast(Vector((pt.x, pt.y, pz + 0.09)), Vector((0.0, 0.0, -1.0)), 0.18)
                if hit_top is not None:
                    min_z = hit_top.z + (0.0045 if ri == 3 else 0.0055)
                    if pt.z < min_z:
                        pt.z = min_z
                        pt += rad_dir * 0.0015
            ring_vs.append(bm_col.verts.new(pt))
        col_rings.append(ring_vs)

    for ri in range(4):
        mat_idx = 1 if ri == 3 else 0
        for ci in range(n_col):
            v00, v01 = col_rings[ri][ci], col_rings[ri][ci + 1]
            v11, v10 = col_rings[ri + 1][ci + 1], col_rings[ri + 1][ci]
            f = bm_col.faces.new((v00, v01, v11, v10))
            f.smooth = True
            f.material_index = mat_idx
            u0, u1 = ci / float(n_col), (ci + 1) / float(n_col)
            v0, v1 = ri / 4.0, (ri + 1) / 4.0
            for l, uv in zip(f.loops, ((u0, v0), (u1, v0), (u1, v1), (u0, v1))):
                l[uv_col].uv = uv
    bmesh.ops.recalc_face_normals(bm_col, faces=list(bm_col.faces))
    me_col = bpy.data.meshes.new("Lan_Collar_mesh")
    bm_col.to_mesh(me_col)
    bm_col.free()
    collar_obj = bpy.data.objects.new("Lan_Collar", me_col)
    bpy.context.collection.objects.link(collar_obj)
    collar_obj.data.materials.append(mats["NurseBlouse"])
    collar_obj.data.materials.append(mats["NurseTrim"])
    bpy.ops.object.select_all(action="DESELECT")
    collar_obj.select_set(True)
    bpy.context.view_layer.objects.active = collar_obj
    sol_col = collar_obj.modifiers.new("CollarSolid", "SOLIDIFY")
    sol_col.thickness = 0.0020
    sol_col.offset = 0.0
    bpy.ops.object.modifier_apply(modifier=sol_col.name)
    detail_objs.append(collar_obj)

    # Center front button placket conformed smoothly to draped chest/waist/skirt
    placket = _project_patch_onto_bvh(
        blouse_bvh, "Lan_Placket", center_x=0.0, z_bot=0.695, z_top=neck_base_z - 0.026,
        width=0.030, thickness=0.0026, offset_m=0.0012, mat=mats["NurseBlouse"], nx=3, nz=16,
    )
    detail_objs.append(placket)

    # 5 mother-of-pearl buttons along center placket
    bm_btn = bmesh.new()
    uv_btn = bm_btn.loops.layers.uv.new("UVMap")
    for k in range(5):
        bz = neck_base_z - 0.058 - k * 0.104
        hit, nrm, _, _ = blouse_bvh.ray_cast(Vector((0.0, -0.45, bz)), Vector((0.0, 1.0, 0.0)), 0.70)
        if hit is not None:
            nrm = (-nrm if nrm.y > 0 else nrm).normalized()
            pos = hit + nrm * 0.0050
            rot = nrm.to_track_quat("Z", "Y").to_matrix().to_4x4()
            bmesh.ops.create_icosphere(
                bm_btn, subdivisions=2, radius=1.0,
                matrix=Matrix.Translation(pos) @ rot @ Matrix.Diagonal((0.0056, 0.0056, 0.0020, 1.0)),
            )
    for f in bm_btn.faces:
        f.smooth = True
        for l in f.loops:
            l[uv_btn].uv = (0.5, 0.5)
    me_btn = bpy.data.meshes.new("Lan_Buttons_mesh")
    bm_btn.to_mesh(me_btn)
    bm_btn.free()
    btn_obj = bpy.data.objects.new("Lan_Buttons", me_btn)
    bpy.context.collection.objects.link(btn_obj)
    DH.assign_material(btn_obj, mats["NurseTrim"])
    detail_objs.append(btn_obj)

    # Left chest pocket (on Lan's left chest = +X) + 2 lower hip patch pockets with pale-blue top trim!
    p_chest = _project_patch_onto_bvh(
        blouse_bvh, "Lan_Pocket_Chest", center_x=0.082, z_bot=1.065, z_top=1.158,
        width=0.076, thickness=0.0028, offset_m=0.0014, mat=mats["NurseBlouse"],
        nx=4, nz=6, trim_top_rows=1, trim_mat=mats["NurseTrim"],
    )
    p_hip_l = _project_patch_onto_bvh(
        blouse_bvh, "Lan_Pocket_HipL", center_x=0.105, z_bot=0.745, z_top=0.875,
        width=0.105, thickness=0.0032, offset_m=0.0015, mat=mats["NurseBlouse"],
        nx=5, nz=6, trim_top_rows=1, trim_mat=mats["NurseTrim"],
    )
    p_hip_r = _project_patch_onto_bvh(
        blouse_bvh, "Lan_Pocket_HipR", center_x=-0.105, z_bot=0.745, z_top=0.875,
        width=0.105, thickness=0.0032, offset_m=0.0015, mat=mats["NurseBlouse"],
        nx=5, nz=6, trim_top_rows=1, trim_mat=mats["NurseTrim"],
    )
    detail_objs.extend([p_chest, p_hip_l, p_hip_r])

    # Flat Rigid Laminated An Sinh Hospital ID Badge (`TRẦN NGỌC LAN`) suspended from top clip above left chest pocket!
    hit_b, _nrm_b, _, _ = blouse_bvh.ray_cast(Vector((0.082, -0.45, 1.202)), Vector((0.0, 1.0, 0.0)), 0.70)
    by_anchor = (hit_b.y if hit_b is not None else -0.136) - 0.0045
    bm_bdg = bmesh.new()
    uv_bdg = bm_bdg.loops.layers.uv.new("UVMap")
    bw_h, bh_h, b_th = 0.036, 0.022, 0.0022
    bcx, bcz = 0.082, 1.202
    b_rot = Euler((math.radians(5.0), 0.0, math.radians(-8.0)), "XYZ").to_matrix()
    f_corners = [
        Vector((bcx, by_anchor, bcz)) + b_rot @ Vector((-bw_h, -b_th, -bh_h)),
        Vector((bcx, by_anchor, bcz)) + b_rot @ Vector((bw_h, -b_th, -bh_h)),
        Vector((bcx, by_anchor, bcz)) + b_rot @ Vector((bw_h, -b_th, bh_h)),
        Vector((bcx, by_anchor, bcz)) + b_rot @ Vector((-bw_h, -b_th, bh_h)),
    ]
    b_corners = [
        Vector((bcx, by_anchor, bcz)) + b_rot @ Vector((-bw_h, 0.0, -bh_h)),
        Vector((bcx, by_anchor, bcz)) + b_rot @ Vector((bw_h, 0.0, -bh_h)),
        Vector((bcx, by_anchor, bcz)) + b_rot @ Vector((bw_h, 0.0, bh_h)),
        Vector((bcx, by_anchor, bcz)) + b_rot @ Vector((-bw_h, 0.0, bh_h)),
    ]
    fv = [bm_bdg.verts.new(p) for p in f_corners]
    bv = [bm_bdg.verts.new(p) for p in b_corners]
    ff = bm_bdg.faces.new((fv[0], fv[1], fv[2], fv[3]))
    ff.smooth = False
    for l, uv in zip(ff.loops, ((0.0, 0.0), (1.0, 0.0), (1.0, 1.0), (0.0, 1.0))):
        l[uv_bdg].uv = uv
    fb = bm_bdg.faces.new((bv[3], bv[2], bv[1], bv[0]))
    for l in fb.loops:
        l[uv_bdg].uv = (0.05, 0.05)
    for k in range(4):
        nk = (k + 1) % 4
        fs = bm_bdg.faces.new((fv[nk], fv[k], bv[k], bv[nk]))
        for l in fs.loops:
            l[uv_bdg].uv = (0.05, 0.05)
    bmesh.ops.recalc_face_normals(bm_bdg, faces=list(bm_bdg.faces))
    me_bdg = bpy.data.meshes.new("Lan_IDBadge_mesh")
    bm_bdg.to_mesh(me_bdg)
    bm_bdg.free()
    badge = bpy.data.objects.new("Lan_IDBadge", me_bdg)
    bpy.context.collection.objects.link(badge)
    DH.assign_material(badge, mats["LanBadge"])
    detail_objs.append(badge)

    # Metal lanyard alligator clip at top center of flat badge
    bm_cl = bmesh.new()
    uv_cl = bm_cl.loops.layers.uv.new("UVMap")
    clip_pos = Vector((bcx, by_anchor, bcz)) + b_rot @ Vector((0.0, -b_th * 0.5, bh_h + 0.003))
    bmesh.ops.create_cube(
        bm_cl, size=1.0,
        matrix=Matrix.Translation(clip_pos) @ b_rot.to_4x4() @ Matrix.Diagonal((0.014, 0.0045, 0.012, 1.0)),
    )
    for f in bm_cl.faces:
        for l in f.loops:
            l[uv_cl].uv = (0.5, 0.5)
    me_cl = bpy.data.meshes.new("Lan_BadgeClip_mesh")
    bm_cl.to_mesh(me_cl)
    bm_cl.free()
    cl_obj = bpy.data.objects.new("Lan_BadgeClip", me_cl)
    bpy.context.collection.objects.link(cl_obj)
    DH.assign_material(cl_obj, mats["MetalTrim"])
    detail_objs.append(cl_obj)

    # ------------------------------------------------------------------
    # STAGE 5: SMOOTH CLOSED-TOE BLACK CLINICAL LEATHER LOAFERS (`Lan_Shoes`)
    # Built as sleek almond-toe anatomical loafers fitted to Lan's exact foot bounds (ZERO individual toe ridges!)
    # ------------------------------------------------------------------
    bm_sh = bmesh.new()
    uv_sh = bm_sh.loops.layers.uv.new("UVMap")
    for side_sgn in (1.0, -1.0):
        fverts = [v.co for v in body_clean_copy.data.vertices if v.co.z <= 0.072 and v.co.x * side_sgn > 0.005]
        if not fverts:
            continue
        x0, x1 = min(p.x for p in fverts), max(p.x for p in fverts)
        y0, y1 = min(p.y for p in fverts), max(p.y for p in fverts)
        cx_f = 0.5 * (x0 + x1)
        hw = 0.5 * (x1 - x0) + 0.006
        y_toe = y0 - 0.010
        y_heel = y1 + 0.008
        # Build smooth closed-toe leather nurse flat loaf along Y (heel -> midfoot -> rounded almond toe!)
        rings = []
        n_ring = 16
        # Profile stations: (frac_y from heel->toe, width_scale, top_z, bottom_z)
        stations = [
            (0.00, 0.42, 0.042, 0.004),
            (0.06, 0.78, 0.068, 0.000),
            (0.22, 0.88, 0.070, 0.000),
            (0.45, 0.94, 0.060, 0.000),
            (0.70, 1.00, 0.044, 0.000),
            (0.86, 0.84, 0.032, 0.000),
            (0.95, 0.56, 0.022, 0.002),
            (1.00, 0.16, 0.012, 0.005),
        ]
        for fy, ws, tz, bz in stations:
            y = y_heel + (y_toe - y_heel) * fy
            rx = hw * ws
            rz = 0.5 * (tz - bz)
            cz = 0.5 * (tz + bz)
            ring = []
            for k in range(n_ring):
                a = (k / float(n_ring)) * math.tau
                px = cx_f + math.cos(a) * rx
                # Flatten sole cleanly underneath!
                sa = math.sin(a)
                pz = bz if sa < -0.35 else (cz + sa * rz)
                ring.append(bm_sh.verts.new(Vector((px, y, max(0.0, pz)))))
            rings.append(ring)
        for j in range(len(rings) - 1):
            for k in range(n_ring):
                nk = (k + 1) % n_ring
                f = bm_sh.faces.new((rings[j][k], rings[j][nk], rings[j + 1][nk], rings[j + 1][k]))
                f.smooth = True
                u0, u1 = k / float(n_ring), (k + 1) / float(n_ring)
                v0, v1 = j / float(len(rings) - 1), (j + 1) / float(len(rings) - 1)
                for l, uv in zip(f.loops, ((u0, v0), (u1, v0), (u1, v1), (u0, v1))):
                    l[uv_sh].uv = uv
        # Cap heel & toe cleanly with quads/tris (no ngons!)
        for end_ring, flip in ((rings[0], True), (rings[-1], False)):
            c_pt = sum((v.co for v in end_ring), Vector((0, 0, 0))) / len(end_ring)
            cv = bm_sh.verts.new(c_pt)
            for k in range(n_ring):
                nk = (k + 1) % n_ring
                vs = (end_ring[nk], end_ring[k], cv) if flip else (end_ring[k], end_ring[nk], cv)
                f = bm_sh.faces.new(vs)
                f.smooth = True
                for l in f.loops:
                    l[uv_sh].uv = (0.5, 0.5)
    bmesh.ops.recalc_face_normals(bm_sh, faces=list(bm_sh.faces))
    me_sh = bpy.data.meshes.new("Lan_Shoes_mesh")
    bm_sh.to_mesh(me_sh)
    bm_sh.free()
    shoes = bpy.data.objects.new("Lan_Shoes", me_sh)
    bpy.context.collection.objects.link(shoes)
    DH.assign_material(shoes, mats["ClinicalShoe"])

    # ------------------------------------------------------------------
    # STAGE 6: VINTAGE BRUSHED-STEEL NURSE WRISTWATCH ON LEFT WRIST
    # ------------------------------------------------------------------
    l_hand = rig_obj.data.bones["mixamorig:LeftHand"]
    l_fore_b = rig_obj.data.bones["mixamorig:LeftForeArm"]
    wrist_pos = l_hand.head_local.lerp(l_fore_b.head_local, 0.14)
    wrist_axis = (l_hand.head_local - l_fore_b.head_local).normalized()
    w_rot = wrist_axis.to_track_quat("Z", "Y").to_matrix().to_4x4()

    bm_w = bmesh.new()
    uv_w = bm_w.loops.layers.uv.new("UVMap")
    # Slim fitted watch band wrapping left wrist (all quads, zero ngons!)
    n_seg = 16
    r_w, h_w = 0.0275, 0.011
    ring0, ring1 = [], []
    for i in range(n_seg):
        a = (i / float(n_seg)) * math.tau
        p0 = Matrix.Translation(wrist_pos) @ w_rot @ Vector((math.cos(a) * r_w, math.sin(a) * r_w * 0.85, -h_w * 0.5))
        p1 = Matrix.Translation(wrist_pos) @ w_rot @ Vector((math.cos(a) * r_w, math.sin(a) * r_w * 0.85, h_w * 0.5))
        ring0.append(bm_w.verts.new(p0))
        ring1.append(bm_w.verts.new(p1))
    for i in range(n_seg):
        ni = (i + 1) % n_seg
        f = bm_w.faces.new((ring0[i], ring0[ni], ring1[ni], ring1[i]))
        f.smooth = True
        for l in f.loops:
            l[uv_w].uv = (0.5, 0.5)
    # Slim round watch bezel sitting on dorsal wrist side
    dial_pos = wrist_pos + (w_rot.to_3x3() @ Vector((0.0, r_w * 0.90, 0.0)))
    bmesh.ops.create_icosphere(
        bm_w, subdivisions=2, radius=1.0,
        matrix=Matrix.Translation(dial_pos) @ w_rot @ Matrix.Diagonal((0.0125, 0.0045, 0.0125, 1.0)),
    )
    for f in bm_w.faces:
        f.smooth = True
        for l in f.loops:
            l[uv_w].uv = (0.5, 0.5)
    me_w = bpy.data.meshes.new("Lan_Watch_mesh")
    bm_w.to_mesh(me_w)
    bm_w.free()
    watch = bpy.data.objects.new("Lan_Watch", me_w)
    bpy.context.collection.objects.link(watch)
    DH.assign_material(watch, mats["MetalTrim"])

    # Join all uniform garments & accessories into single clean `Lan_Uniform_LOD0` and transfer skin weights!
    uniform_parts = [blouse, trousers, shoes, watch] + detail_objs
    bpy.ops.object.select_all(action="DESELECT")
    for p in uniform_parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = blouse
    bpy.ops.object.join()
    uniform_obj = bpy.context.active_object
    uniform_obj.name = "Lan_Uniform_LOD0"
    DH.set_smooth(uniform_obj)
    DH.triangulate_ngons(uniform_obj)

    DH.transfer_skin_weights(body_clean_copy, uniform_obj, rig_obj, smooth_iters=4, smooth_factor=0.50)

    # Guarantee wristwatch stays cleanly weighted to LeftForeArm/LeftHand
    vg_lf = uniform_obj.vertex_groups.get("mixamorig:LeftForeArm") or uniform_obj.vertex_groups.new(name="mixamorig:LeftForeArm")
    for v in uniform_obj.data.vertices:
        if (v.co - wrist_pos).length < 0.038:
            for vg in uniform_obj.vertex_groups:
                vg.remove([v.index])
            vg_lf.add([v.index], 1.0, "REPLACE")

    print(f"[GARMENT] Built continuous-shoulder cloth-sim Lan_Uniform_LOD0 ({len(uniform_obj.data.polygons)} polys)")
    return uniform_obj


def add_clipboard_socket_bone(rig_obj):
    """Add `ClipboardSocket_R` bone parented to `mixamorig:RightHand` for exact physical F101 handover."""
    bpy.ops.object.select_all(action="DESELECT")
    rig_obj.select_set(True)
    bpy.context.view_layer.objects.active = rig_obj
    bpy.ops.object.mode_set(mode="EDIT")
    rhand = rig_obj.data.edit_bones.get("mixamorig:RightHand")
    if rhand and "ClipboardSocket_R" not in rig_obj.data.edit_bones:
        sock = rig_obj.data.edit_bones.new("ClipboardSocket_R")
        sock.parent = rhand
        sock.use_connect = False
        sock.use_deform = True
        hand_dir = (rhand.tail - rhand.head).normalized()
        sock.head = rhand.head + hand_dir * 0.062 + Vector((0.0, -0.020, -0.012))
        sock.tail = sock.head + Vector((0.0, -0.08, 0.0))
    bpy.ops.object.mode_set(mode="OBJECT")


def build_lan_animations(rig_obj):
    """Build Lan's 5 hero reception performance actions: 2 CMU MoCap + 3 world-axis authored performances."""

    def lan_relaxed_hands(rig, open_r=0.0, open_l=0.0):
        RC.apply_finger_curl(rig, "Left", 0.24 - 0.10 * open_l, 0.30 - 0.12 * open_l, 0.38 - 0.12 * open_l, 0.45 - 0.14 * open_l, 0.18, 0.04 * open_l)
        RC.apply_finger_curl(rig, "Right", 0.22 - 0.12 * open_r, 0.28 - 0.14 * open_r, 0.36 - 0.14 * open_r, 0.44 - 0.16 * open_r, 0.18, 0.06 * open_r)

    def add_breathing(rig, t, cycles=1.0, amp=1.0):
        b = math.sin(t * math.tau * cycles)
        RC.add_world_delta(rig, "mixamorig:Spine1", rx_deg=-0.9 * b * amp)
        RC.add_world_delta(rig, "mixamorig:Spine2", rx_deg=-1.1 * b * amp)
        RC.add_world_delta(rig, "mixamorig:LeftShoulder", ry_deg=-0.8 * b * amp)
        RC.add_world_delta(rig, "mixamorig:RightShoulder", ry_deg=0.8 * b * amp)

    # 1. Lan_StepOut — Real CMU MoCap calm female-paced walk (07_04.bvh -> 30fps, floor-grounded!)
    def post_step_out(rig, t, fi):
        lan_relaxed_hands(rig, open_r=0.14, open_l=0.10)
        add_breathing(rig, t, cycles=1.0, amp=0.75)

    RC.retarget_cmu_clip(
        rig_obj, "Lan_StepOut", "07_04.bvh",
        src_start=96, src_end=336, step=4,
        in_place=True, cyclic_blend=8,
        scale_rot=0.92,
        bone_scales={"mixamorig:LeftArm": 0.74, "mixamorig:RightArm": 0.74, "mixamorig:Head": 0.60},
        post_frame_fn=post_step_out,
        target_world_speed=1.70,
        stride_cycles=3,
    )

    # 2. Lan_TalkGesture — Natural grounded Vietnamese nurse speaking poise (facing Minh, gentle open-palm reception gestures!)
    def talk_frame(rig, t, fi):
        s1 = math.sin(t * math.tau)
        s2 = math.sin(t * math.tau * 2.0)
        c1 = math.cos(t * math.tau)
        gest_r = 0.5 + 0.5 * math.sin(t * math.tau * 2.0 - 0.5)
        gest_l = 0.5 + 0.5 * math.sin(t * math.tau - 0.8)

        # Grounded, attentive forward-facing stance with organic contrapposto sway
        RC.add_world_delta(rig, "mixamorig:Hips", rx_deg=1.0, ry_deg=1.6 * s1, rz_deg=2.2 * s1)
        RC.aim_bone_world(rig, "mixamorig:LeftUpLeg", Vector((0.03 + 0.015 * s1, -0.02, -1.0)))
        RC.aim_bone_world(rig, "mixamorig:LeftLeg", Vector((0.01, 0.03, -1.0)))
        RC.aim_bone_world(rig, "mixamorig:RightUpLeg", Vector((-0.03 + 0.015 * s1, 0.01, -1.0)))
        RC.aim_bone_world(rig, "mixamorig:RightLeg", Vector((-0.01, 0.04, -1.0)))

        # Subtle conversational upper-body lean, chest breath & gentle head emphasis
        RC.add_world_delta(rig, "mixamorig:Spine", rx_deg=1.8 + 0.6 * gest_r, ry_deg=-1.1 * s1, rz_deg=-1.2 * s1)
        RC.add_world_delta(rig, "mixamorig:Spine1", rx_deg=1.0 - 0.9 * s2)
        RC.add_world_delta(rig, "mixamorig:Spine2", rx_deg=-1.0 * s2, rz_deg=-0.8 * s1)
        RC.add_world_delta(rig, "mixamorig:Neck", rx_deg=0.8 + 1.4 * s2, ry_deg=1.6 * c1, rz_deg=1.8 * s1)
        RC.add_world_delta(rig, "mixamorig:Head", rx_deg=-0.6 + 1.8 * s2, ry_deg=2.2 * c1, rz_deg=2.0 * s1)

        # Right arm: calm waist-to-lower-chest conversational emphasis gesture (clear of hip pockets!)
        r_arm_dir = Vector((-0.22, -0.20 - 0.16 * gest_r, -0.95 + 0.08 * gest_r))
        r_for_dir = Vector((0.12 - 0.10 * gest_r, -0.76 - 0.22 * gest_r, -0.60 + 0.42 * gest_r))
        r_hnd_dir = Vector((0.14 - 0.10 * gest_r, -0.82 - 0.18 * gest_r, -0.54 + 0.46 * gest_r))
        RC.aim_bone_world(rig, "mixamorig:RightArm", r_arm_dir)
        RC.aim_bone_world(rig, "mixamorig:RightForeArm", r_for_dir)
        RC.aim_bone_world(rig, "mixamorig:RightHand", r_hnd_dir)

        # Left arm: poised softly forward in front of waist / counter edge (zero pocket clip!)
        l_arm_dir = Vector((0.22, -0.18 - 0.10 * gest_l, -0.96 + 0.05 * gest_l))
        l_for_dir = Vector((-0.26 - 0.10 * gest_l, -0.74 - 0.14 * gest_l, -0.60 + 0.24 * gest_l))
        l_hnd_dir = Vector((-0.28 - 0.08 * gest_l, -0.78 - 0.12 * gest_l, -0.54 + 0.24 * gest_l))
        RC.aim_bone_world(rig, "mixamorig:LeftArm", l_arm_dir)
        RC.aim_bone_world(rig, "mixamorig:LeftForeArm", l_for_dir)
        RC.aim_bone_world(rig, "mixamorig:LeftHand", l_hnd_dir)

        lan_relaxed_hands(rig, open_r=0.42 * gest_r + 0.12, open_l=0.22 * gest_l + 0.10)
        return Vector((0.008 * s1, -0.010 * gest_r, 0.0))

    RC.author_layered_action(rig_obj, "Lan_TalkGesture", 120, talk_frame)

    # 3. Lan_IdleDesk — Natural relaxed Vietnamese nurse standing poise (arms comfortably down, hands lightly poised!)
    def idle_frame(rig, t, fi):
        s1 = math.sin(t * math.tau)
        s2 = math.sin(t * math.tau * 2.0)
        c1 = math.cos(t * math.tau)

        # Gentle contrapposto weight shift & relaxed natural leg stance
        RC.add_world_delta(rig, "mixamorig:Hips", rx_deg=0.8, ry_deg=1.4 * s1, rz_deg=1.5 * s1)
        RC.aim_bone_world(rig, "mixamorig:LeftUpLeg", Vector((0.03 + 0.015 * s1, -0.02, -1.0)))
        RC.aim_bone_world(rig, "mixamorig:LeftLeg", Vector((0.01, 0.03, -1.0)))
        RC.aim_bone_world(rig, "mixamorig:RightUpLeg", Vector((-0.03 + 0.015 * s1, 0.01, -1.0)))
        RC.aim_bone_world(rig, "mixamorig:RightLeg", Vector((-0.01, 0.04, -1.0)))

        # Subtle organic spine counter-curve & natural breathing
        RC.add_world_delta(rig, "mixamorig:Spine", rx_deg=0.8, ry_deg=-1.0 * s1, rz_deg=-0.8 * s1)
        RC.add_world_delta(rig, "mixamorig:Spine1", rx_deg=0.6 - 0.9 * s2)
        RC.add_world_delta(rig, "mixamorig:Spine2", rx_deg=-1.0 * s2, rz_deg=-0.6 * s1)
        RC.add_world_delta(rig, "mixamorig:Neck", rx_deg=0.6 + 0.5 * s2, ry_deg=0.8 * c1, rz_deg=1.2 * s1)
        RC.add_world_delta(rig, "mixamorig:Head", rx_deg=-0.5 + 0.6 * s2, ry_deg=1.0 * c1, rz_deg=1.4 * s1)

        # Natural relaxed arms hanging down along sides with soft bent elbows & hands resting cleanly clear of hip pockets
        RC.aim_bone_world(rig, "mixamorig:LeftArm", Vector((0.21 + 0.015 * s1, -0.12, -0.970)))
        RC.aim_bone_world(rig, "mixamorig:LeftForeArm", Vector((-0.08, -0.56 - 0.03 * s2, -0.82)))
        RC.aim_bone_world(rig, "mixamorig:LeftHand", Vector((-0.10, -0.60, -0.79)))

        RC.aim_bone_world(rig, "mixamorig:RightArm", Vector((-0.21 + 0.015 * s1, -0.14, -0.968)))
        RC.aim_bone_world(rig, "mixamorig:RightForeArm", Vector((0.10, -0.58 - 0.03 * s2, -0.80)))
        RC.aim_bone_world(rig, "mixamorig:RightHand", Vector((0.12, -0.62, -0.77)))

        lan_relaxed_hands(rig, open_r=0.10, open_l=0.10)
        return Vector((0.010 * s1, 0.0, 0.0))

    a_idle = RC.author_layered_action(rig_obj, "Lan_IdleDesk", 120, idle_frame)

    # 4. Lan_Listen — Attentive empathetic listening poise (hands clasped softly in front of waist, gentle nod!)
    def listen_frame(rig, t, fi):
        s1 = math.sin(t * math.tau)
        s2 = math.sin(t * math.tau * 2.0)
        nod = math.exp(-(((t - 0.55) / 0.12) ** 2))

        RC.add_world_delta(rig, "mixamorig:Hips", rx_deg=0.6, ry_deg=-1.2 + 0.8 * s1, rz_deg=1.2)
        RC.aim_bone_world(rig, "mixamorig:LeftUpLeg", Vector((0.03, -0.02, -1.0)))
        RC.aim_bone_world(rig, "mixamorig:LeftLeg", Vector((0.01, 0.03, -1.0)))
        RC.aim_bone_world(rig, "mixamorig:RightUpLeg", Vector((-0.03, 0.01, -1.0)))
        RC.aim_bone_world(rig, "mixamorig:RightLeg", Vector((-0.01, 0.04, -1.0)))

        RC.add_world_delta(rig, "mixamorig:Spine", rx_deg=1.8, rz_deg=-0.8)
        RC.add_world_delta(rig, "mixamorig:Spine1", rx_deg=1.2 - 0.8 * s2)
        RC.add_world_delta(rig, "mixamorig:Spine2", rx_deg=-0.9 * s2, rz_deg=1.0)
        # Empathetic attentive head tilt & subtle understanding nod while Minh speaks CH01-D04!
        RC.add_world_delta(rig, "mixamorig:Neck", rx_deg=1.5 + 2.2 * nod, ry_deg=-2.5, rz_deg=0.8 * s1)
        RC.add_world_delta(rig, "mixamorig:Head", rx_deg=-0.8 + 4.2 * nod, ry_deg=-3.2, rz_deg=0.6 * s1)

        # Hands clasped naturally in front of waist/abdomen
        RC.aim_bone_world(rig, "mixamorig:LeftArm", Vector((0.18, -0.22, -0.958)))
        RC.aim_bone_world(rig, "mixamorig:LeftForeArm", Vector((-0.58, -0.72, -0.38 - 0.03 * s2)))
        RC.aim_bone_world(rig, "mixamorig:LeftHand", Vector((-0.64, -0.68, -0.34)))

        RC.aim_bone_world(rig, "mixamorig:RightArm", Vector((-0.18, -0.24, -0.953)))
        RC.aim_bone_world(rig, "mixamorig:RightForeArm", Vector((0.58, -0.74, -0.35 - 0.03 * s2)))
        RC.aim_bone_world(rig, "mixamorig:RightHand", Vector((0.64, -0.68, -0.32)))

        lan_relaxed_hands(rig, open_r=0.14, open_l=0.14)
        return Vector((0.006 * s1, 0.0, 0.0))

    RC.author_layered_action(rig_obj, "Lan_Listen", 96, listen_frame)

    # 5. Lan_Handover — Smooth lean forward & counter-height two-handed presentation of F101 Clipboard directly to Minh!
    def handover_frame(rig, t, fi):
        if t < 0.04:
            reach = 0.0
        elif t < 0.28:
            u = (t - 0.04) / 0.24
            reach = u * u * (3.0 - 2.0 * u)
        elif t < 0.82:
            reach = 1.0
        else:
            u = (t - 0.82) / 0.18
            reach = 1.0 - (u * u * (3.0 - 2.0 * u))

        sb = math.sin(t * math.tau)
        RC.add_world_delta(rig, "mixamorig:Hips", rx_deg=2.2 * reach, rz_deg=-4.0 * reach)
        RC.aim_bone_world(rig, "mixamorig:LeftUpLeg", Vector((0.03, 0.05 * reach, -1.0)))
        RC.aim_bone_world(rig, "mixamorig:LeftLeg", Vector((0.01, 0.04 + 0.04 * reach, -1.0)))
        RC.aim_bone_world(rig, "mixamorig:RightUpLeg", Vector((-0.03, -0.09 * reach, -1.0)))
        RC.aim_bone_world(rig, "mixamorig:RightLeg", Vector((-0.01, 0.03 + 0.08 * reach, -1.0)))

        RC.add_world_delta(rig, "mixamorig:Spine", rx_deg=3.5 * reach, rz_deg=2.2 * reach)
        RC.add_world_delta(rig, "mixamorig:Spine1", rx_deg=2.5 * reach - 0.7 * sb, rz_deg=1.5 * reach)
        RC.add_world_delta(rig, "mixamorig:Spine2", rx_deg=-0.8 * sb, rz_deg=2.0 * reach)
        RC.add_world_delta(rig, "mixamorig:Neck", rx_deg=-2.5 * reach, rz_deg=-2.0 * reach)
        RC.add_world_delta(rig, "mixamorig:Head", rx_deg=-2.2 * reach, ry_deg=1.5 * reach, rz_deg=-2.0 * reach)

        # Right arm lifts clipboard smoothly forward to chest/counter height directly toward Minh (-Y)!
        r_arm_dir = Vector((-0.15, -0.10, -0.98)).lerp(Vector((-0.04, -0.78, -0.62)), reach)
        r_for_dir = Vector((0.16, -0.48, -0.86)).lerp(Vector((0.14, -0.96, 0.24)), reach)
        r_hnd_dir = Vector((0.20, -0.50, -0.84)).lerp(Vector((0.08, -0.98, 0.18)), reach)
        RC.aim_bone_world(rig, "mixamorig:RightArm", r_arm_dir)
        RC.aim_bone_world(rig, "mixamorig:RightForeArm", r_for_dir)
        RC.aim_bone_world(rig, "mixamorig:RightHand", r_hnd_dir)

        # Left arm supports politely near lower chest/clipboard edge
        l_arm_dir = Vector((0.15, -0.08, -0.98)).lerp(Vector((0.10, -0.62, -0.77)), reach)
        l_for_dir = Vector((-0.14, -0.44, -0.88)).lerp(Vector((-0.42, -0.89, 0.12)), reach)
        l_hnd_dir = Vector((-0.18, -0.48, -0.86)).lerp(Vector((-0.44, -0.88, 0.10)), reach)
        RC.aim_bone_world(rig, "mixamorig:LeftArm", l_arm_dir)
        RC.aim_bone_world(rig, "mixamorig:LeftForeArm", l_for_dir)
        RC.aim_bone_world(rig, "mixamorig:LeftHand", l_hnd_dir)

        RC.apply_finger_curl(rig, "Right", 0.30 + 0.32 * reach, 0.36 + 0.34 * reach, 0.42 + 0.32 * reach, 0.48 + 0.28 * reach, 0.26 + 0.18 * reach, 0.02)
        RC.apply_finger_curl(rig, "Left", 0.22 + 0.14 * reach, 0.28 + 0.14 * reach, 0.36 + 0.12 * reach, 0.44 + 0.10 * reach, 0.18, 0.04)
        return Vector((0.0, -0.045 * reach, 0.0))

    RC.author_layered_action(rig_obj, "Lan_Handover", 78, handover_frame)

    # Set default active action to Lan_IdleDesk at natural mid-pose
    rig_obj.animation_data.action = a_idle
    bpy.context.scene.frame_set(20)


def build_lan():
    DH.fresh_metric_scene()

    # 1. Calibrated 23yo Vietnamese young woman (`1.600 m` height, 100% Asian `hm08` base + Vietnamese morphology!)
    macro = TargetService.get_default_macro_info_dict()
    macro["gender"] = 0.0
    macro["age"] = 0.49          # ~23yo young adult
    macro["weight"] = 0.38       # natural slender Vietnamese nurse build
    macro["muscle"] = 0.32
    macro["height"] = 0.48
    macro["proportions"] = 0.54
    macro["race"] = {"asian": 1.0, "caucasian": 0.0, "african": 0.0}

    # Authentic 23yo Vietnamese female craniofacial proportions (avoiding over-stacked epicanthus/zygomatic targets
    # on top of Asian 1.0 macro base!) + dedicated BMesh anatomical sculpt pass!
    facial_targets = {
        "head-oval": 0.24,
        "head-scale-horiz-incr": 0.04,
        "head-scale-depth-decr": 0.10,
        "l-cheek-bones-incr": 0.14,
        "r-cheek-bones-incr": 0.14,
        "l-cheek-volume-incr": 0.12,
        "r-cheek-volume-incr": 0.12,
        "chin-width-decr": 0.12,
        "chin-prominent-decr": 0.08,
        "chin-height-decr": 0.05,
        "l-eye-epicanthus-in": 0.16,
        "r-eye-epicanthus-in": 0.16,
        "l-eye-eyefold-down": 0.08,
        "r-eye-eyefold-down": 0.08,
        "l-eye-push1-out": 0.16,
        "r-eye-push1-out": 0.16,
        "l-eye-corner2-up": 0.05,
        "r-eye-corner2-up": 0.05,
        "eyebrows-trans-up": 0.06,
        "eyebrows-angle-up": 0.08,
        "nose-scale-depth-decr": 0.18,
        "nose-compression-compress": 0.16,
        "nose-hump-decr": 0.18,
        "nose-point-width-incr": 0.12,
        "nose-nostrils-width-incr": 0.12,
        "nose-flaring-incr": 0.08,
        "nose-scale-vert-decr": 0.06,
        "mouth-scale-horiz-decr": 0.06,
        "mouth-upperlip-height-incr": 0.14,
        "mouth-lowerlip-height-incr": 0.14,
        "mouth-angles-up": 0.06,
    }
    human = DH.create_calibrated_human(macro, target_height_m=1.600, facial_targets=facial_targets)
    human.name = "Lan_Body_Raw"
    sculpt_lan_craniofacial_features(human)

    # 2. Load CC0 anatomical eyes, teeth, tongue, gentle natural Vietnamese eyebrows (eyebrow001), eyelashes & ponytail01!
    def load_part(cat, frag):
        p = AssetService.find_asset_absolute_path(frag, cat)
        if not p:
            raise FileNotFoundError(f"Missing CC0 asset {cat}/{frag}")
        return HumanService.add_mhclo_asset(p, human, asset_type=cat, subdiv_levels=0, material_type="NONE")

    eyes_obj = load_part("eyes", "high-poly/high-poly.mhclo")
    teeth_obj = load_part("teeth", "teeth_base/teeth_base.mhclo")
    tongue_obj = load_part("tongue", "tongue01/tongue01.mhclo")
    brows_obj = load_part("eyebrows", "eyebrow001/eyebrow001.mhclo")
    lashes_obj = load_part("eyelashes", "eyelashes02/eyelashes02.mhclo")
    hair_raw = load_part("hair", "ponytail01/ponytail01.mhclo")

    brows_obj.name = "Lan_Eyebrows_LOD0"
    lashes_obj.name = "Lan_Eyelashes_LOD0"

    # 3. Build 29 anatomical facial BlendShapes & transfer accurately to brows/lashes!
    DH.build_anatomical_blendshapes(human)
    DH.transfer_shapekeys_by_surface(human, brows_obj)
    DH.transfer_shapekeys_by_surface(human, lashes_obj, query_y_offset=0.008)

    # 4. Add MPFB2 64-bone `mixamo_unity` rig + `ClipboardSocket_R`
    rig = HumanService.add_builtin_rig(human, "mixamo_unity", import_weights=True)
    rig.name = "Lan_Armature"
    add_clipboard_socket_bone(rig)

    mats = create_lan_materials()

    # 5. Rig eyes (outer cornea stripped!), teeth, tongue, brows, lashes, and multi-layer groomed hair + nurse barrette!
    eye_l, eye_r = split_and_rig_eyes(eyes_obj, rig, mats["LanEye"])
    rig_oral_cavity(teeth_obj, tongue_obj, rig, mats["LanTeeth"], mats["LanTongue"])

    DH.set_smooth(brows_obj)
    DH.assign_material(brows_obj, mats["LanEyebrow"])
    DH.weight_rigid(brows_obj, rig, "mixamorig:Head")

    DH.set_smooth(lashes_obj)
    DH.assign_material(lashes_obj, mats["LanEyelash"])
    DH.weight_rigid(lashes_obj, rig, "mixamorig:Head")

    hair_obj, clip_obj = finish_lan_hair(hair_raw, human, rig, mats["LanHairCard"], mats["EnamelBlue"])

    # 6. Build full-body clean copy for garment extraction, cloth collision & smooth weight transfer!
    body_for_cloth = human.copy()
    body_for_cloth.data = human.data.copy()
    body_for_cloth.name = "_LanClothBody"
    bpy.context.collection.objects.link(body_for_cloth)
    if body_for_cloth.data.shape_keys:
        body_for_cloth.shape_key_clear()
    DH.strip_helper_geometry_preserving_shapekeys(body_for_cloth, extra_keep_fn=None)

    # 7. Extract continuous-shoulder trousers + blouse, run two-stage Blender Cloth Sim, project pockets/badge, shoes & watch!
    uniform_obj = build_lan_garments(body_for_cloth, rig, mats)

    # 8. Strip MakeHuman helper geometry AND body triangles fully hidden inside uniform (keep wide cuffs/neck overlap!)
    neck_z = rig.data.bones["mixamorig:Neck"].head_local.z
    l_fore = rig.data.bones["mixamorig:LeftForeArm"]
    sleeve_x = abs(l_fore.head_local.lerp(l_fore.tail_local, 0.56).x) - 0.045  # generous overlap inside 3/4 sleeve!

    def lan_keep_body(co):
        # Always keep head, neck, ears & upper collarbone cleanly visible
        if co.z > neck_z - 0.055:
            return True
        # Always keep forearms & articulated hands!
        if abs(co.x) > sleeve_x:
            return True
        # Keep lower ankles (0.03..0.11m) so zero air gap ever appears between trouser cuff & flat clinical loafer!
        if 0.030 <= co.z <= 0.115:
            return True
        # Hide torso/pelvis/legs/feet fully covered by blouse + trousers + clinical flats
        return False

    DH.strip_helper_geometry_preserving_shapekeys(human, extra_keep_fn=lan_keep_body)
    DH.set_smooth(human)
    DH.assign_material(human, mats["LanSkin"])
    DH.bind_rig(human, rig)

    # Join brows & lashes into `Lan_Body_LOD0` so ALL 29 facial BlendShapes live on ONE SkinnedMeshRenderer!
    bpy.ops.object.select_all(action="DESELECT")
    human.select_set(True)
    brows_obj.select_set(True)
    lashes_obj.select_set(True)
    bpy.context.view_layer.objects.active = human
    bpy.ops.object.join()
    body_lod0 = bpy.context.active_object
    body_lod0.name = "Lan_Body_LOD0"

    # Remove temporary cloth collider body
    bpy.data.objects.remove(body_for_cloth, do_unlink=True)

    # 9. Generate game-ready LOD1 & LOD2 (keeping full 29 BlendShapes on LOD1 & LOD2 body, and protecting collar/trim/badge!)
    lod0_meshes = [body_lod0, eye_l, eye_r, teeth_obj, tongue_obj, hair_obj, clip_obj, uniform_obj]

    def protect_uniform_features(v, mat_names):
        if mat_names & {"NurseTrim", "LanBadge", "MetalTrim"}:
            return True
        if v.co.z >= neck_z - 0.045 and abs(v.co.x) < 0.145:
            return True
        return False

    body_lod1 = DH.make_skinned_lod(body_lod0, "Lan_Body_LOD1", ratio=1.0, rig_obj=rig, keep_shapekeys=True)
    uniform_lod1 = DH.make_skinned_lod(uniform_obj, "Lan_Uniform_LOD1", ratio=0.58, rig_obj=rig, keep_shapekeys=False, protect_fn=protect_uniform_features)
    hair_lod1 = DH.make_skinned_lod(hair_obj, "Lan_Hair_LOD1", ratio=0.68, rig_obj=rig, keep_shapekeys=False)
    clip_lod1 = DH.make_skinned_lod(clip_obj, "Lan_HairClip_LOD1", ratio=0.60, rig_obj=rig, keep_shapekeys=False)
    eye_l_lod1 = DH.make_skinned_lod(eye_l, "Lan_Eye_L_LOD1", ratio=0.55, rig_obj=rig, keep_shapekeys=False)
    eye_r_lod1 = DH.make_skinned_lod(eye_r, "Lan_Eye_R_LOD1", ratio=0.55, rig_obj=rig, keep_shapekeys=False)
    teeth_lod1 = DH.make_skinned_lod(teeth_obj, "Lan_Teeth_LOD1", ratio=0.22, rig_obj=rig, keep_shapekeys=False)
    tongue_lod1 = DH.make_skinned_lod(tongue_obj, "Lan_Tongue_LOD1", ratio=0.55, rig_obj=rig, keep_shapekeys=False)
    lod1_meshes = [body_lod1, uniform_lod1, hair_lod1, clip_lod1, eye_l_lod1, eye_r_lod1, teeth_lod1, tongue_lod1]

    body_lod2 = DH.make_skinned_lod(body_lod0, "Lan_Body_LOD2", ratio=0.45, rig_obj=rig, keep_shapekeys=True)
    uniform_lod2 = DH.make_skinned_lod(uniform_obj, "Lan_Uniform_LOD2", ratio=0.34, rig_obj=rig, keep_shapekeys=False, protect_fn=protect_uniform_features)
    hair_lod2 = DH.make_skinned_lod(hair_obj, "Lan_Hair_LOD2", ratio=0.42, rig_obj=rig, keep_shapekeys=False)
    clip_lod2 = DH.make_skinned_lod(clip_obj, "Lan_HairClip_LOD2", ratio=0.35, rig_obj=rig, keep_shapekeys=False)
    eye_l_lod2 = DH.make_skinned_lod(eye_l, "Lan_Eye_L_LOD2", ratio=0.30, rig_obj=rig, keep_shapekeys=False)
    eye_r_lod2 = DH.make_skinned_lod(eye_r, "Lan_Eye_R_LOD2", ratio=0.30, rig_obj=rig, keep_shapekeys=False)
    teeth_lod2 = DH.make_skinned_lod(teeth_obj, "Lan_Teeth_LOD2", ratio=0.09, rig_obj=rig, keep_shapekeys=False)
    tongue_lod2 = DH.make_skinned_lod(tongue_obj, "Lan_Tongue_LOD2", ratio=0.32, rig_obj=rig, keep_shapekeys=False)
    lod2_meshes = [body_lod2, uniform_lod2, hair_lod2, clip_lod2, eye_l_lod2, eye_r_lod2, teeth_lod2, tongue_lod2]

    for m in lod1_meshes + lod2_meshes:
        m.hide_render = True
        m.hide_viewport = True

    # 10. Author Lan's 5 hero reception performance animations
    build_lan_animations(rig)

    # 11. Save editable .blend source & export Unity FBX
    DH.save_and_export_character("CTCC_CHR_Lan", rig, lod0_meshes, lod1_meshes, lod2_meshes)


if __name__ == "__main__":
    build_lan()
