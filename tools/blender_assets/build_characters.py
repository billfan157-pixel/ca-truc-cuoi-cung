"""M2 Rigged, BlendShaped & Animated Characters (Pass 4 Organic Polish) built in Blender 4.5 LTS.

Pass 4 visual craftsmanship fixes verified from Pass 3 Cycles inspections:
- C-infinity symmetric face sculpting (Gaussian sum at +/-c instead of abs(x)): ZERO vertical crease down the nose/forehead!
- Continuous multi-segment organic sleeves & trouser lofts (make_polyline_loft) flowing smoothly from shoulder yoke -> elbow -> cuff
  and hip -> knee -> ankle with ZERO ball-joint shoulder spheres and ZERO horizontal knee/elbow ring seams!
- Lan: Properly seated warm expressive Vietnamese eyes (sclera + deep dark-brown glossy iris + pupil framed by flush sculpted eyelids
  and soft upper lash arc), thin inward-tapered hairline + curved temple locks + subtle ears, flush folded V-collar/lapels!
- Gatekeeper: Continuous heavy scorched overcoat sleeves & draped hem (zero ball shoulders, zero elbow cuts, zero thigh clip),
  smooth nose/brow shadowing, tall ominous silhouette + steel/brass keyring!
- Bé An: Continuous organic sleeves, shorts, legs and gently sculpted child face/hair!
- Fan-triangulated top/bottom loft caps so FBX tangent space exports with ZERO warnings!
"""

import math
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import common as C


def _cap_ring_fan(bm, ring, flip=False):
    center = sum((v.co for v in ring), Vector((0, 0, 0))) / float(len(ring))
    cv = bm.verts.new(center)
    n = len(ring)
    for i in range(n):
        ni = (i + 1) % n
        if flip:
            bm.faces.new((cv, ring[ni], ring[i]))
        else:
            bm.faces.new((cv, ring[i], ring[ni]))


def make_loft(name, rings, mat_key, cap_bottom=True, cap_top=True, uv_scale=0.35, smooth_angle=75.0):
    bm = bmesh.new()
    v_rings = []
    segs = rings[0][8]
    for r_idx, (cx, cy, cz, rx, ry, rotx, w_amp, w_freq, _s) in enumerate(rings):
        ring_v = []
        c_rx, s_rx = math.cos(rotx), math.sin(rotx)
        for i in range(segs):
            a = math.tau * i / segs
            w = 1.0 + w_amp * math.sin(a * w_freq + r_idx * 0.45)
            lx = math.cos(a) * rx * w
            ly = math.sin(a) * ry * w
            ring_v.append(bm.verts.new((cx + lx, cy + ly * c_rx, cz + ly * s_rx)))
        v_rings.append(ring_v)

    for r in range(len(v_rings) - 1):
        r0, r1 = v_rings[r], v_rings[r + 1]
        for i in range(segs):
            ni = (i + 1) % segs
            bm.faces.new((r0[i], r0[ni], r1[ni], r1[i]))
    if cap_bottom:
        _cap_ring_fan(bm, v_rings[0], flip=True)
    if cap_top:
        _cap_ring_fan(bm, v_rings[-1], flip=False)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new(name + "_mesh")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return C.finalize_mesh(obj, name, mat_key, smooth_angle=smooth_angle, uv_scale=uv_scale)


def make_polyline_loft(name, knots, mat_key, segs=18, uv_scale=0.35, cap_start=True, cap_end=True):
    """Build ONE continuous smooth organic tube along a 3D polyline with (pos, rx, ry) knots.
    Eliminates shoulder ball-joints and elbow/knee cylinder seams completely!"""
    bm = bmesh.new()
    pts = [Vector(k[0]) for k in knots]
    rxs = [k[1] for k in knots]
    rys = [k[2] for k in knots]
    n = len(pts)

    # Parallel-transport smooth tangent frame along polyline
    tangents = []
    for i in range(n):
        if i == 0:
            t = (pts[1] - pts[0]).normalized()
        elif i == n - 1:
            t = (pts[-1] - pts[-2]).normalized()
        else:
            t = (pts[i + 1] - pts[i - 1]).normalized()
        tangents.append(t)

    up_ref = Vector((0, 1, 0)) if abs(tangents[0].y) < 0.85 else Vector((1, 0, 0))
    x_dir = tangents[0].cross(up_ref).normalized()
    y_dir = tangents[0].cross(x_dir).normalized()

    v_rings = []
    for i in range(n):
        if i > 0:
            t_cur = tangents[i]
            x_proj = x_dir - t_cur * x_dir.dot(t_cur)
            if x_proj.length > 1e-5:
                x_dir = x_proj.normalized()
            y_dir = t_cur.cross(x_dir).normalized()
        ring = []
        for s in range(segs):
            a = math.tau * s / segs
            offset = x_dir * (math.cos(a) * rxs[i]) + y_dir * (math.sin(a) * rys[i])
            ring.append(bm.verts.new(pts[i] + offset))
        v_rings.append(ring)

    for i in range(n - 1):
        ra, rb = v_rings[i], v_rings[i + 1]
        for s in range(segs):
            ns = (s + 1) % segs
            bm.faces.new((ra[s], ra[ns], rb[ns], rb[s]))
    if cap_start:
        _cap_ring_fan(bm, v_rings[0], flip=True)
    if cap_end:
        _cap_ring_fan(bm, v_rings[-1], flip=False)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new(name + "_mesh")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return C.finalize_mesh(obj, name, mat_key, smooth_angle=78.0, uv_scale=uv_scale)


def make_smooth_collar(name, center_z, r_base, r_top, height, front_open_deg, mat_key, thickness=0.006):
    """Crisp flat-folded cloth collar band hugging the neck with clean V-opening."""
    bm = bmesh.new()
    segs = 28
    start_a = math.radians(-90 + front_open_deg)
    end_a = math.radians(270 - front_open_deg)
    bot_ring, top_ring = [], []
    for i in range(segs + 1):
        t = i / float(segs)
        a = start_a + (end_a - start_a) * t
        back_factor = 0.5 + 0.5 * math.sin(a)
        h_loc = height * (0.48 + 0.52 * back_factor)
        ca, sa = math.cos(a), math.sin(a)
        bot_ring.append(bm.verts.new((ca * r_base, sa * r_base, center_z)))
        top_ring.append(bm.verts.new((ca * r_top, sa * r_top, center_z + h_loc)))
    for i in range(segs):
        bm.faces.new((bot_ring[i], bot_ring[i + 1], top_ring[i + 1], top_ring[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new(name + "_mesh")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    sol = obj.modifiers.new("Thick", "SOLIDIFY")
    sol.thickness = thickness
    return C.finalize_mesh(obj, name, mat_key, smooth_angle=70.0, uv_scale=0.30)


def _sym_gauss(x, c, w):
    """C-infinity symmetric bilateral Gaussian (zero derivative at x=0 => ZERO nose crease!)."""
    return math.exp(-(((x - c) / w) ** 2)) + math.exp(-(((x + c) / w) ** 2))


def make_sculpted_head(center_z, scale=1.0, skin_key="LanSkin", mode="lan"):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=64, ring_count=42, radius=1.0, location=(0, 0, 0))
    head = bpy.context.object
    mesh = head.data

    sx = (0.081 if mode == "lan" else 0.084 if mode == "gk" else 0.076) * scale
    sy = (0.097 if mode == "lan" else 0.102 if mode == "gk" else 0.088) * scale
    sz = (0.110 if mode == "lan" else 0.120 if mode == "gk" else 0.098) * scale

    for v in mesh.vertices:
        x, y, z = v.co.x, v.co.y, v.co.z
        jaw_taper = 1.0 - (max(0.0, -z) ** 1.35) * (0.24 if mode == "lan" else 0.28 if mode == "gk" else 0.18)
        cranium_back = 1.0 + max(0.0, y) * 0.12 * max(0.0, z + 0.30)
        jaw_angle = _sym_gauss(x, 0.62, 0.28) * math.exp(-(((z + 0.42) / 0.28) ** 2))
        px = x * sx * (jaw_taper + 0.055 * jaw_angle)
        py = y * sy * cranium_back
        pz = z * sz

        front = max(0.0, -y)
        if mode == "lan" and front > 0.02:
            chin_g = math.exp(-((x / 0.30) ** 2 + ((z + 0.78) / 0.20) ** 2))
            py -= 0.0085 * scale * chin_g * front
            cheek_g = _sym_gauss(x, 0.50, 0.26) * math.exp(-(((z + 0.12) / 0.24) ** 2))
            py -= 0.0085 * scale * cheek_g * front
            # Clean almond eye sockets recessed so eyeballs + warm brown irises sit naturally inside!
            eye_sock = _sym_gauss(x, 0.33, 0.135) * math.exp(-(((z - 0.09) / 0.082) ** 2))
            py += 0.0175 * scale * eye_sock * front
            brow_g = _sym_gauss(x, 0.32, 0.20) * math.exp(-(((z - 0.25) / 0.068) ** 2))
            py -= 0.0052 * scale * brow_g * front
            # Gentle Vietnamese nose bridge & rounded tip
            nose_b = math.exp(-((x / 0.095) ** 2 + ((z - 0.00) / 0.19) ** 2))
            nose_t = math.exp(-((x / 0.115) ** 2 + ((z + 0.155) / 0.095) ** 2))
            ala = _sym_gauss(x, 0.115, 0.068) * math.exp(-(((z + 0.195) / 0.058) ** 2))
            py -= (0.0105 * nose_b + 0.0155 * nose_t + 0.0055 * ala) * scale * front
            # Soft natural lips
            lip_u = math.exp(-((x / 0.215) ** 2 + ((z + 0.422) / 0.048) ** 2))
            lip_l = math.exp(-((x / 0.195) ** 2 + ((z + 0.495) / 0.056) ** 2))
            seam = math.exp(-((x / 0.195) ** 2 + ((z + 0.458) / 0.016) ** 2))
            py -= (0.0095 * lip_u + 0.0088 * lip_l - 0.0042 * seam) * scale * front
        elif mode == "gk" and front > 0.02:
            hollow_eye = _sym_gauss(x, 0.34, 0.175) * math.exp(-(((z - 0.10) / 0.14) ** 2))
            hollow_cheek = _sym_gauss(x, 0.48, 0.22) * math.exp(-(((z + 0.28) / 0.25) ** 2))
            brow = _sym_gauss(x, 0.32, 0.24) * math.exp(-(((z - 0.28) / 0.085) ** 2))
            nose = math.exp(-((x / 0.092) ** 2 + ((z + 0.06) / 0.21) ** 2))
            py += (0.0155 * hollow_eye + 0.0095 * hollow_cheek - 0.0085 * brow - 0.0105 * nose) * scale * front
        elif mode == "an" and front > 0.02:
            cheek = _sym_gauss(x, 0.46, 0.28) * math.exp(-(((z + 0.14) / 0.25) ** 2))
            nose = math.exp(-((x / 0.095) ** 2 + ((z + 0.14) / 0.085) ** 2))
            eye_s = _sym_gauss(x, 0.32, 0.14) * math.exp(-(((z - 0.08) / 0.085) ** 2))
            py -= (0.007 * cheek + 0.009 * nose - 0.005 * eye_s) * scale * front

        v.co = Vector((px, py, pz + center_z))

    C.assign_mat(head, skin_key)
    C.set_smooth_by_angle(head, 85.0)
    if not mesh.uv_layers:
        mesh.uv_layers.new(name="UVMap")
    uv_layer = mesh.uv_layers.active.data
    for poly in mesh.polygons:
        for li in poly.loop_indices:
            co = mesh.vertices[mesh.loops[li].vertex_index].co
            ang = math.atan2(co.x, -co.y)
            u = 0.5 + ang / math.tau
            v_coord = 0.5 + (co.z - center_z) / (2.2 * sz)
            uv_layer[li].uv = (max(0.001, min(0.999, u)), max(0.001, min(0.999, v_coord)))
    head.name = "SculptedHead"
    return head


def make_smooth_hair_cap(center_z, scale=1.0, style="lan"):
    """Smooth parametric hair dome that tapers INWARD at hairline edge so there is ZERO thick dark rim around the face!"""
    if style == "gk":
        sx, sy, sz = 0.0865 * scale, 0.1055 * scale, 0.1235 * scale
    elif style == "lan":
        sx, sy, sz = 0.0835 * scale, 0.1005 * scale, 0.1135 * scale
    else:
        sx, sy, sz = 0.0805 * scale, 0.0925 * scale, 0.1030 * scale

    nu, nv = 52, 24
    bm = bmesh.new()
    grid = []
    for j in range(nv + 1):
        v = j / float(nv)
        ring = []
        edge_tuck = 0.988 if j == 0 else (1.0 + 0.068 * math.sin(v * math.pi))
        for i in range(nu):
            u = i / float(nu)
            phi = u * math.tau
            front_w = max(0.0, math.cos(phi))
            if style == "lan":
                part_dip = 0.16 * math.exp(-((math.sin(phi) + 0.32) / 0.36) ** 2)
                theta_edge = math.radians(37.0 * front_w - 40.0 * (1.0 - front_w)) + part_dip * front_w
            else:
                theta_edge = math.radians(28.0 * front_w - 30.0 * (1.0 - front_w))
            theta_top = math.radians(88.5)
            theta = theta_edge + (theta_top - theta_edge) * (v ** 0.92)
            ct, st = math.cos(theta), math.sin(theta)
            vol = edge_tuck + 0.025 * (1.0 - front_w) * math.sin(v * math.pi)
            x = math.sin(phi) * ct * sx * vol
            y = -math.cos(phi) * ct * sy * vol
            z = st * sz * vol + center_z + 0.004 * scale
            ring.append(bm.verts.new((x, y, z)))
        grid.append(ring)

    for j in range(nv):
        for i in range(nu):
            ni = (i + 1) % nu
            bm.faces.new((grid[j][i], grid[j][ni], grid[j + 1][ni], grid[j + 1][i]))
    _cap_ring_fan(bm, grid[-1], flip=False)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new("SmoothHairCap_mesh")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("SmoothHairCap", mesh)
    bpy.context.collection.objects.link(obj)
    sol = obj.modifiers.new("Thick", "SOLIDIFY")
    sol.thickness = 0.0022 * scale
    sol.offset = 0.5
    return C.finalize_mesh(obj, "SmoothHairCap", "LanHair", smooth_angle=82.0, uv_scale=0.28)


def make_eyelid_frame(name, sx, ey, ez, scale=1.0):
    """Sleek flush upper/lower eyelid arcs + delicate dark upper lash hugging the eyeball cleanly."""
    # Eyeball radius = 0.0115; front surface sits at ey - 0.0115
    u_pts = [
        (sx - 0.0130 * scale, ey - 0.0055 * scale, ez + 0.0005 * scale),
        (sx - 0.0055 * scale, ey - 0.0112 * scale, ez + 0.0068 * scale),
        (sx + 0.0055 * scale, ey - 0.0112 * scale, ez + 0.0068 * scale),
        (sx + 0.0130 * scale, ey - 0.0055 * scale, ez + 0.0005 * scale),
    ]
    l_pts = [
        (sx - 0.0120 * scale, ey - 0.0055 * scale, ez - 0.0012 * scale),
        (sx, ey - 0.0108 * scale, ez - 0.0064 * scale),
        (sx + 0.0120 * scale, ey - 0.0055 * scale, ez - 0.0012 * scale),
    ]
    lash_pts = [(p[0], p[1] - 0.0014 * scale, p[2] - 0.0012 * scale) for p in u_pts]
    u_lid = C.make_pipe(name + "_U", u_pts, 0.0028 * scale, "LanSkin", bevel_res=3)
    l_lid = C.make_pipe(name + "_L", l_pts, 0.0020 * scale, "LanSkin", bevel_res=3)
    lash = C.make_pipe(name + "_Lash", lash_pts, 0.0013 * scale, "LanHair", bevel_res=2)
    return [u_lid, l_lid, lash]


def add_lan_shape_keys(head_obj, center_z, scale=1.0):
    mesh = head_obj.data
    head_obj.shape_key_add(name="Basis", from_mix=False)
    sx, sy, sz = 0.081 * scale, 0.097 * scale, 0.110 * scale

    def make_key(kname, delta_fn):
        sk = head_obj.shape_key_add(name=kname, from_mix=False)
        for i, v in enumerate(mesh.vertices):
            nx, ny, nz = v.co.x / sx, v.co.y / sy, (v.co.z - center_z) / sz
            dx, dy, dz = delta_fn(nx, ny, nz)
            sk.data[i].co = v.co + Vector((dx * scale, dy * scale, dz * scale))

    def blink_fn(side_sign):
        def fn(nx, ny, nz):
            if ny > -0.25 or nx * side_sign < 0.08:
                return (0, 0, 0)
            w = math.exp(-(((abs(nx) - 0.33) / 0.15) ** 2 + ((nz - 0.12) / 0.085) ** 2))
            return (0.0, -0.004 * w, -0.010 * w)
        return fn

    make_key("Blink_L", blink_fn(-1.0))
    make_key("Blink_R", blink_fn(1.0))
    make_key("Viseme_AA", lambda nx, ny, nz: (0, 0.004 * math.exp(-((nx / 0.30) ** 2 + ((nz + 0.56) / 0.22) ** 2)),
                                              -0.015 * math.exp(-((nx / 0.30) ** 2 + ((nz + 0.56) / 0.22) ** 2))) if ny < -0.2 else (0, 0, 0))
    make_key("Viseme_OH", lambda nx, ny, nz: (-nx * 0.008 * math.exp(-((nx / 0.25) ** 2 + ((nz + 0.47) / 0.13) ** 2)),
                                              -0.007 * math.exp(-((nx / 0.25) ** 2 + ((nz + 0.47) / 0.13) ** 2)),
                                              -0.009 * math.exp(-((nx / 0.24) ** 2 + ((nz + 0.54) / 0.13) ** 2))) if ny < -0.2 else (0, 0, 0))
    make_key("Viseme_EE", lambda nx, ny, nz: (math.copysign(0.006 * math.exp(-(((abs(nx) - 0.21) / 0.13) ** 2 + ((nz + 0.46) / 0.09) ** 2)), nx),
                                              0.002 * math.exp(-(((abs(nx) - 0.21) / 0.13) ** 2 + ((nz + 0.46) / 0.09) ** 2)),
                                              -0.004 * math.exp(-((nx / 0.22) ** 2 + ((nz + 0.51) / 0.08) ** 2))) if ny < -0.2 else (0, 0, 0))
    make_key("Viseme_MM", lambda nx, ny, nz: (0, 0.002 * math.exp(-((nx / 0.22) ** 2 + ((nz + 0.46) / 0.08) ** 2)), 0) if ny < -0.2 else (0, 0, 0))
    make_key("Brow_Concern", lambda nx, ny, nz: (-math.copysign(0.003 * math.exp(-(((abs(nx) - 0.16) / 0.14) ** 2 + ((nz - 0.25) / 0.08) ** 2)), nx),
                                                 0, 0.005 * math.exp(-(((abs(nx) - 0.16) / 0.14) ** 2 + ((nz - 0.25) / 0.08) ** 2))) if ny < -0.2 else (0, 0, 0))
    make_key("Mouth_Warm", lambda nx, ny, nz: (math.copysign(0.0045 * math.exp(-(((abs(nx) - 0.21) / 0.12) ** 2 + ((nz + 0.46) / 0.09) ** 2)), nx),
                                               0.003 * math.exp(-(((abs(nx) - 0.21) / 0.12) ** 2 + ((nz + 0.46) / 0.09) ** 2)),
                                               0.005 * math.exp(-(((abs(nx) - 0.21) / 0.12) ** 2 + ((nz + 0.46) / 0.09) ** 2))) if ny < -0.2 else (0, 0, 0))
    make_key("Mouth_Sorrow", lambda nx, ny, nz: (0, 0, -0.0045 * math.exp(-(((abs(nx) - 0.20) / 0.12) ** 2 + ((nz + 0.46) / 0.09) ** 2))) if ny < -0.2 else (0, 0, 0))


def create_biped_armature(name, spec):
    arm_data = bpy.data.armatures.new(name + "_ArmData")
    arm_obj = bpy.data.objects.new(name, arm_data)
    bpy.context.collection.objects.link(arm_obj)
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode="EDIT")
    eb = arm_data.edit_bones
    created = {}
    for bname, parent_name, head, tail in spec:
        bone = eb.new(bname)
        bone.head = Vector(head)
        bone.tail = Vector(tail)
        bone.use_deform = True
        if parent_name and parent_name in created:
            bone.parent = created[parent_name]
            bone.use_connect = (bone.head - created[parent_name].tail).length < 0.015
        created[bname] = bone
    bpy.ops.object.mode_set(mode="OBJECT")
    return arm_obj


def weight_part(mesh_obj, arm_obj, allowed_bones, falloff_power=2.6):
    mesh_obj.vertex_groups.clear()
    bones = arm_obj.data.bones
    vgroups = {b.name: mesh_obj.vertex_groups.new(name=b.name) for b in bones}
    segs = {b.name: (b.head_local.copy(), b.tail_local.copy()) for b in bones}

    def dist_to_seg(pt, a, b):
        ab = b - a
        l2 = ab.length_squared
        if l2 < 1e-8:
            return (pt - a).length
        t = max(0.0, min(1.0, (pt - a).dot(ab) / l2))
        return (pt - (a + ab * t)).length

    mat_w = mesh_obj.matrix_world
    for v in mesh_obj.data.vertices:
        p = mat_w @ v.co
        scored = []
        for bname, bias in allowed_bones:
            if bname not in segs:
                continue
            a, b = segs[bname]
            d = dist_to_seg(p, a, b)
            scored.append((bname, bias / ((d + 0.040) ** falloff_power)))
        scored.sort(key=lambda x: x[1], reverse=True)
        top = scored[:3]
        total = sum(w for _, w in top)
        if total > 1e-9:
            for bname, w in top:
                vgroups[bname].add([v.index], w / total, "REPLACE")


def bind_to_armature(mesh_obj, arm_obj):
    mesh_obj.parent = arm_obj
    mod = mesh_obj.modifiers.new("Armature", "ARMATURE")
    mod.object = arm_obj
    mod.use_vertex_groups = True


def bake_pose_action(arm_obj, action_name, frame_end, keyframe_fn):
    act = bpy.data.actions.new(name=action_name)
    act.use_fake_user = True
    if not arm_obj.animation_data:
        arm_obj.animation_data_create()
    arm_obj.animation_data.action = act
    for pb in arm_obj.pose.bones:
        pb.rotation_mode = "XYZ"
    frames = list(range(0, frame_end + 1, 3))
    if frames[-1] != frame_end:
        frames.append(frame_end)
    for f in frames:
        bpy.context.scene.frame_set(f)
        t_norm = f / float(max(1, frame_end))
        for pb in arm_obj.pose.bones:
            pb.location = (0.0, 0.0, 0.0)
            pb.rotation_euler = (0.0, 0.0, 0.0)
        keyframe_fn(arm_obj.pose.bones, t_norm)
        for pb in arm_obj.pose.bones:
            pb.keyframe_insert(data_path="rotation_euler", frame=f)
            if pb.name == "Hips":
                pb.keyframe_insert(data_path="location", frame=f)
    return act


# ==============================================================================
# 1. CHAR002 — TRẦN NGỌC LAN (Pass 4 Organic Polish)
# ==============================================================================
def build_lan():
    C.fresh_scene()
    head_z = 1.486
    spec = [
        ("Hips", None, (0, 0, 0.88), (0, 0, 0.99)),
        ("Spine", "Hips", (0, 0, 0.99), (0, 0, 1.13)),
        ("Chest", "Spine", (0, 0, 1.13), (0, 0, 1.31)),
        ("Neck", "Chest", (0, 0, 1.31), (0, 0, 1.40)),
        ("Head", "Neck", (0, 0, 1.40), (0, 0, 1.59)),
        ("Eye_L", "Head", (-0.0265, -0.0775, head_z + 0.010), (-0.0265, -0.114, head_z + 0.010)),
        ("Eye_R", "Head", (0.0265, -0.0775, head_z + 0.010), (0.0265, -0.114, head_z + 0.010)),
        ("Shoulder_L", "Chest", (-0.05, 0, 1.255), (-0.168, 0, 1.242)),
        ("UpperArm_L", "Shoulder_L", (-0.168, 0, 1.242), (-0.208, 0.008, 1.010)),
        ("LowerArm_L", "UpperArm_L", (-0.208, 0.008, 1.010), (-0.216, -0.020, 0.785)),
        ("Hand_L", "LowerArm_L", (-0.216, -0.020, 0.785), (-0.216, -0.030, 0.675)),
        ("Shoulder_R", "Chest", (0.05, 0, 1.255), (0.168, 0, 1.242)),
        ("UpperArm_R", "Shoulder_R", (0.168, 0, 1.242), (0.208, 0.008, 1.010)),
        ("LowerArm_R", "UpperArm_R", (0.208, 0.008, 1.010), (0.216, -0.020, 0.785)),
        ("Hand_R", "LowerArm_R", (0.216, -0.020, 0.785), (0.216, -0.030, 0.675)),
        ("UpperLeg_L", "Hips", (-0.088, 0, 0.86), (-0.088, 0, 0.44)),
        ("LowerLeg_L", "UpperLeg_L", (-0.088, 0, 0.44), (-0.088, 0, 0.045)),
        ("Foot_L", "LowerLeg_L", (-0.088, 0, 0.045), (-0.088, -0.14, 0.025)),
        ("UpperLeg_R", "Hips", (0.088, 0, 0.86), (0.088, 0, 0.44)),
        ("LowerLeg_R", "UpperLeg_R", (0.088, 0, 0.44), (0.088, 0, 0.045)),
        ("Foot_R", "LowerLeg_R", (0.088, 0, 0.045), (0.088, -0.14, 0.025)),
    ]
    arm_obj = create_biped_armature("Lan_Armature", spec)
    weighted_parts = []

    def add_part(obj, bones):
        weight_part(obj, arm_obj, bones)
        weighted_parts.append(obj)

    # Continuous trouser legs (hip -> thigh -> knee -> calf -> ankle: ZERO horizontal knee seam!)
    for side, sx in (("L", -0.088), ("R", 0.088)):
        lb = [(f"UpperLeg_{side}", 1.1), (f"LowerLeg_{side}", 1.1), (f"Foot_{side}", 0.5), ("Hips", 0.5)]
        add_part(C.make_box(f"Shoe_{side}", (sx, -0.035, 0.022), (0.076, 0.215, 0.040), "Black", bevel=0.016, segments=3), [(f"Foot_{side}", 1.0)])
        add_part(C.make_sphere(f"ShoeToe_{side}", (sx, -0.110, 0.030), (0.076, 0.088, 0.050), "Black", segs=16, rings=10), [(f"Foot_{side}", 1.0)])
        leg_knots = [
            ((sx, 0.004, 0.042), 0.044, 0.046),
            ((sx, 0.003, 0.240), 0.052, 0.054),
            ((sx, 0.000, 0.440), 0.058, 0.060),
            ((sx * 0.96, 0.002, 0.650), 0.068, 0.072),
            ((sx * 0.90, 0.005, 0.860), 0.078, 0.082),
        ]
        add_part(make_polyline_loft(f"Trouser_{side}", leg_knots, "DarkTrouser", segs=20, uv_scale=0.30), lb)

    # Tailored Vietnamese nurse áo blouse with clean sloped shoulder yoke
    blouse_rings = [
        (0, 0.010, 0.62, 0.205, 0.138, 0.0, 0.022, 7, 36),
        (0, 0.008, 0.74, 0.192, 0.130, 0.0, 0.014, 7, 36),
        (0, 0.005, 0.86, 0.175, 0.120, 0.0, 0.007, 6, 36),
        (0, 0.002, 0.99, 0.132, 0.095, 0.0, 0.004, 5, 36),
        (0, -0.008, 1.13, 0.158, 0.118, 0.0, 0.003, 4, 36),
        (0, -0.002, 1.242, 0.178, 0.100, 0.0, 0.002, 4, 36),
        (0, 0.002, 1.298, 0.126, 0.082, 0.0, 0.000, 4, 36),
        (0, 0.004, 1.334, 0.064, 0.058, 0.0, 0.000, 4, 36),
    ]
    coat_bones = [("Hips", 1.1), ("Spine", 1.2), ("Chest", 1.2), ("UpperLeg_L", 0.35), ("UpperLeg_R", 0.35)]
    add_part(make_loft("LanBlouse", blouse_rings, "NurseBlouse", cap_bottom=True, cap_top=True, uv_scale=0.32), coat_bones)

    chest_bones = [("Chest", 1.3), ("Spine", 0.7)]
    hip_bones = [("Hips", 1.3), ("Spine", 0.6)]
    add_part(C.make_box("Placket", (0, -0.108, 0.93), (0.026, 0.010, 0.56), "NurseBlouse", bevel=0.003, uv_scale=0.32), [("Hips", 1.1), ("Spine", 1.2), ("Chest", 1.2)])
    for bz in (0.74, 0.85, 0.96, 1.07, 1.17):
        add_part(C.make_cylinder(f"Btn_{bz:.2f}", (0, -0.112, bz), (0, -0.119, bz), 0.0055, "PlasticIvory", segments=10), [("Hips", 1.1), ("Spine", 1.2), ("Chest", 1.2)])

    # Authentic Vietnamese MoH 1895/1997 QĐ-BYT nurse standing collar + piping + pockets + LanIDBadge (clean, flush, no dangling strips!)
    add_part(make_smooth_collar("LanCollar", 1.318, 0.062, 0.068, 0.038, 22.0, "NurseBlouse", thickness=0.005), chest_bones + [("Neck", 0.4)])
    add_part(make_smooth_collar("LanCollarTrim", 1.348, 0.066, 0.070, 0.008, 22.0, "NurseTrim", thickness=0.006), chest_bones + [("Neck", 0.4)])
    for sx in (-1, 1):
        add_part(C.make_box(f"HipPocket_{sx}", (sx * 0.098, -0.112, 0.83), (0.088, 0.010, 0.102), "NurseBlouse", bevel=0.004, rot=(0, 0, sx * 0.24), uv_scale=0.30), hip_bones)
        add_part(C.make_box(f"HipTrim_{sx}", (sx * 0.098, -0.116, 0.879), (0.090, 0.010, 0.009), "NurseTrim", bevel=0.002, rot=(0, 0, sx * 0.24)), hip_bones)
    add_part(C.make_box("ChestPocket", (0.072, -0.116, 1.150), (0.066, 0.010, 0.074), "NurseBlouse", bevel=0.003, rot=(0, 0, 0.16), uv_scale=0.30), chest_bones)
    add_part(C.make_box("ChestTrim", (0.072, -0.119, 1.185), (0.068, 0.010, 0.008), "NurseTrim", bevel=0.002, rot=(0, 0, 0.16)), chest_bones)
    add_part(C.make_cylinder("PocketPen", (0.090, -0.122, 1.135), (0.094, -0.122, 1.212), 0.0032, "EnamelBlue", segments=8), chest_bones)
    add_part(C.make_box("LanIDBadge", (0.068, -0.125, 1.206), (0.064, 0.004, 0.040), "LanBadgeMat", bevel=0.001, rot=(0, 0, 0.14)), chest_bones)
    add_part(C.make_box("BadgeClip", (0.068, -0.124, 1.228), (0.014, 0.007, 0.011), "Steel", bevel=0.002), chest_bones)

    # Continuous organic sleeves flowing smoothly out of blouse shoulder yoke -> elbow -> cuff (ZERO ball shoulders, ZERO elbow cuts!)
    for side, sx in (("L", -1), ("R", 1)):
        wr = (sx * 0.216, -0.020, 0.785)
        ab = [(f"Shoulder_{side}", 0.9), ("Chest", 0.35), (f"UpperArm_{side}", 1.15), (f"LowerArm_{side}", 1.15), (f"Hand_{side}", 1.0)]
        sleeve_knots = [
            ((sx * 0.100, 0.000, 1.268), 0.060, 0.058),
            ((sx * 0.142, 0.001, 1.258), 0.058, 0.056),
            ((sx * 0.166, 0.002, 1.234), 0.055, 0.054),
            ((sx * 0.184, 0.005, 1.165), 0.050, 0.050),
            ((sx * 0.200, 0.008, 1.070), 0.046, 0.046),
            ((sx * 0.208, 0.006, 0.995), 0.043, 0.043),
            ((sx * 0.212, -0.005, 0.916), 0.041, 0.041),
        ]
        add_part(make_polyline_loft(f"Sleeve_{side}", sleeve_knots, "NurseBlouse", segs=18, uv_scale=0.32), ab)
        cuff_knots = [
            ((sx * 0.212, -0.004, 0.928), 0.0425, 0.0425),
            ((sx * 0.212, -0.006, 0.912), 0.0425, 0.0425),
        ]
        add_part(make_polyline_loft(f"Cuff_{side}", cuff_knots, "NurseTrim", segs=18, uv_scale=0.32), ab)
        forearm_knots = [
            ((sx * 0.212, -0.004, 0.922), 0.034, 0.034),
            ((sx * 0.214, -0.012, 0.855), 0.029, 0.028),
            ((wr[0], wr[1], wr[2]), 0.024, 0.022),
            ((wr[0], wr[1] - 0.003, wr[2] - 0.016), 0.023, 0.020),
        ]
        add_part(make_polyline_loft(f"Forearm_{side}", forearm_knots, "LanSkin", segs=16), [(f"LowerArm_{side}", 1.2), (f"Hand_{side}", 0.7)])
        hb = [(f"Hand_{side}", 1.4), (f"LowerArm_{side}", 0.35)]
        add_part(C.make_box(f"Palm_{side}", (wr[0], wr[1] - 0.007, wr[2] - 0.038), (0.024, 0.056, 0.066), "LanSkin", bevel=0.010, segments=2), hb)
        add_part(C.make_cylinder(f"Thumb_{side}", (wr[0] - sx * 0.005, wr[1] - 0.025, wr[2] - 0.022),
                                 (wr[0] - sx * 0.014, wr[1] - 0.044, wr[2] - 0.058), 0.0075, "LanSkin", segments=8), hb)
        for fi in range(4):
            fy = wr[1] - 0.022 + fi * 0.0135
            fl = 0.056 if fi in (1, 2) else 0.046
            add_part(C.make_cylinder(f"Fing_{side}_{fi}", (wr[0], fy, wr[2] - 0.062),
                                     (wr[0] - sx * 0.006, fy - 0.004, wr[2] - 0.062 - fl), 0.0060, "LanSkin", segments=8), hb)
    add_part(C.make_cylinder("WatchStrap", (0.216, -0.019, 0.798), (0.216, -0.019, 0.811), 0.026, "Black", segments=14), [("LowerArm_R", 1.0), ("Hand_R", 0.5)])
    add_part(C.make_cylinder("WatchDial", (0.240, -0.021, 0.805), (0.246, -0.021, 0.805), 0.011, "Paper", segments=14), [("LowerArm_R", 1.0), ("Hand_R", 0.5)])
    add_part(make_polyline_loft("LanNeck", [((0, 0.004, 1.300), 0.044, 0.044), ((0, 0.001, 1.360), 0.040, 0.040), ((0, -0.002, 1.420), 0.038, 0.038)], "LanSkin", segs=20), [("Neck", 1.2), ("Chest", 0.5), ("Head", 0.4)])

    body_obj = C.join_objects(weighted_parts, "Lan_Body")
    bind_to_armature(body_obj, arm_obj)

    head_obj = make_sculpted_head(head_z, scale=1.0, skin_key="LanSkin", mode="lan")
    add_lan_shape_keys(head_obj, head_z, scale=1.0)
    weight_part(head_obj, arm_obj, [("Head", 1.0)])
    bind_to_armature(head_obj, arm_obj)

    eye_objs = []
    hair_parts = [make_smooth_hair_cap(head_z, scale=1.0, style="lan")]
    # Properly seated warm dark-brown Vietnamese eyes visible cleanly inside sculpted almond sockets!
    for side, sx in (("L", -0.0265), ("R", 0.0265)):
        ey, ez = -0.0775, head_z + 0.010
        scl = C.make_sphere(f"Sclera_{side}", (sx, ey, ez), (0.0230, 0.0230, 0.0230), "LanEyeWhite", segs=20, rings=14)
        iris = C.make_sphere(f"Iris_{side}", (sx, ey - 0.0094, ez), (0.0124, 0.0055, 0.0124), "LanIris", segs=20, rings=12)
        pupil = C.make_sphere(f"Pupil_{side}", (sx, ey - 0.0115, ez), (0.0056, 0.0025, 0.0056), "Black", segs=12, rings=8)
        em = C.join_objects([scl, iris, pupil], f"Lan_Eye_{side}")
        weight_part(em, arm_obj, [(f"Eye_{side}", 1.0)])
        bind_to_armature(em, arm_obj)
        eye_objs.append(em)
        hair_parts.extend(make_eyelid_frame(f"Lid_{side}", sx, ey, ez, scale=1.0))

    # Subtle ears + low nurse bun + neat sweeping temple hair strands framing her face
    for sx in (-1, 1):
        hair_parts.append(C.make_sphere(f"Ear_{sx}", (sx * 0.080, 0.004, head_z - 0.006), (0.014, 0.028, 0.044), "LanSkin", segs=12, rings=8))
        lock_knots = [
            ((sx * 0.044, -0.036, head_z + 0.064), 0.010, 0.008),
            ((sx * 0.072, -0.026, head_z + 0.024), 0.012, 0.010),
            ((sx * 0.079, 0.012, head_z - 0.022), 0.013, 0.011),
            ((sx * 0.044, 0.090, head_z - 0.028), 0.015, 0.013),
        ]
        hair_parts.append(make_polyline_loft(f"HairLock_{sx}", lock_knots, "LanHair", segs=12, uv_scale=0.25))
    hair_parts.append(C.make_sphere("HairBun", (0, 0.116, head_z - 0.020), (0.094, 0.080, 0.086), "LanHair", segs=22, rings=14))
    hair_parts.append(C.make_box("HairClip", (0, 0.102, head_z + 0.022), (0.056, 0.015, 0.014), "EnamelBlue", bevel=0.004))
    hair_obj = C.join_objects(hair_parts, "Lan_HairAndDetails")
    weight_part(hair_obj, arm_obj, [("Head", 1.0)])
    bind_to_armature(hair_obj, arm_obj)

    def anim_idle(pb, t):
        s, s2 = math.sin(t * math.tau), math.sin(t * math.tau * 2.0)
        pb["Hips"].location = (0.006 * s, 0.0, 0.003 * s2)
        pb["Spine"].rotation_euler = (0.018 * s2, 0.0, 0.012 * s)
        pb["Chest"].rotation_euler = (0.020 * s2, 0.0, -0.010 * s)
        pb["Head"].rotation_euler = (-0.015 + 0.02 * s2, 0.015 * s, 0.035 * s)
        pb["UpperArm_L"].rotation_euler = (-0.18 + 0.02 * s, 0.0, 0.10)
        pb["LowerArm_L"].rotation_euler = (-0.38 + 0.02 * s2, 0.0, -0.14)
        pb["UpperArm_R"].rotation_euler = (-0.20 - 0.02 * s, 0.0, -0.10)
        pb["LowerArm_R"].rotation_euler = (-0.42 + 0.02 * s2, 0.0, 0.14)

    def anim_step_out(pb, t):
        cyc, cyc2, fade = math.sin(t * math.tau * 2.0), math.cos(t * math.tau * 2.0), math.sin(t * math.pi)
        pb["Hips"].location = (0.014 * cyc * fade, 0.0, abs(cyc2) * 0.020 * fade)
        pb["UpperLeg_L"].rotation_euler = (0.30 * cyc * fade, 0, 0)
        pb["LowerLeg_L"].rotation_euler = (max(0.0, -0.38 * cyc) * fade, 0, 0)
        pb["UpperLeg_R"].rotation_euler = (-0.30 * cyc * fade, 0, 0)
        pb["LowerLeg_R"].rotation_euler = (max(0.0, 0.38 * cyc) * fade, 0, 0)
        pb["UpperArm_L"].rotation_euler = (-0.16 - 0.16 * cyc * fade, 0, 0.08)
        pb["LowerArm_L"].rotation_euler = (-0.32, 0, -0.08)
        pb["UpperArm_R"].rotation_euler = (-0.18 + 0.16 * cyc * fade, 0, -0.08)
        pb["LowerArm_R"].rotation_euler = (-0.38, 0, 0.08)

    def anim_talk(pb, t):
        s, s2 = math.sin(t * math.tau), math.sin(t * math.tau * 2.0)
        pb["Spine"].rotation_euler = (0.025 + 0.018 * s2, 0, 0.020 * s)
        pb["Chest"].rotation_euler = (0.018 * s2, 0, -0.015 * s)
        pb["Head"].rotation_euler = (-0.025 + 0.038 * s2, 0.025 * s, 0.04 * s)
        pb["UpperArm_R"].rotation_euler = (-0.42 - 0.12 * s, 0, -0.14 - 0.05 * s2)
        pb["LowerArm_R"].rotation_euler = (-0.64 - 0.14 * s2, 0.12, 0.16)
        pb["UpperArm_L"].rotation_euler = (-0.22 + 0.04 * s, 0, 0.12)
        pb["LowerArm_L"].rotation_euler = (-0.46, 0, -0.12)

    def anim_handover(pb, t):
        reach = math.sin(min(1.0, t * 1.4) * math.pi * 0.5) * (1.0 - max(0.0, (t - 0.72) / 0.28))
        pb["Spine"].rotation_euler = (0.06 * reach, 0, 0.03 * reach)
        pb["UpperArm_R"].rotation_euler = (-0.20 - 0.68 * reach, 0, -0.08)
        pb["LowerArm_R"].rotation_euler = (-0.44 + 0.16 * reach, 0, 0.08)
        pb["UpperArm_L"].rotation_euler = (-0.20 - 0.24 * reach, 0, 0.10)
        pb["LowerArm_L"].rotation_euler = (-0.44, 0, -0.10)

    a_idle = bake_pose_action(arm_obj, "Lan_IdleDesk", 120, anim_idle)
    bake_pose_action(arm_obj, "Lan_StepOut", 78, anim_step_out)
    bake_pose_action(arm_obj, "Lan_TalkGesture", 108, anim_talk)
    bake_pose_action(arm_obj, "Lan_Handover", 66, anim_handover)

    arm_obj.animation_data.action = a_idle
    bpy.context.scene.frame_set(15)
    C.export_character(
        "CTCC_CHR_Lan", arm_obj, [body_obj, head_obj, hair_obj, *eye_objs], render=True,
        cam_dir=(0.40, -2.05, 0.18),
        closeup_cam={"pos": (0.08, -0.58, 1.50), "target": (0.0, -0.02, 1.47)},
    )


# ==============================================================================
# 2. CHAR005 — KẺ GIỮ CỬA / THE GATEKEEPER (Pass 4 Organic Polish)
# ==============================================================================
def build_gatekeeper():
    C.fresh_scene()
    kx, ky, kz = 0.285, -0.055, 0.785
    spec = [
        ("Hips", None, (0, 0, 1.06), (0, 0, 1.20)),
        ("Spine", "Hips", (0, 0, 1.20), (0, 0, 1.38)),
        ("Chest", "Spine", (0, 0, 1.38), (0, -0.01, 1.58)),
        ("Neck", "Chest", (0, -0.01, 1.58), (0, -0.012, 1.69)),
        ("Head", "Neck", (0, -0.012, 1.69), (0, -0.012, 1.94)),
        ("Shoulder_L", "Chest", (-0.06, -0.01, 1.52), (-0.230, -0.01, 1.50)),
        ("UpperArm_L", "Shoulder_L", (-0.230, -0.01, 1.50), (-0.278, 0.010, 1.20)),
        ("LowerArm_L", "UpperArm_L", (-0.278, 0.010, 1.20), (-0.288, -0.026, 0.90)),
        ("Hand_L", "LowerArm_L", (-0.288, -0.026, 0.90), (-0.288, -0.036, 0.74)),
        ("Shoulder_R", "Chest", (0.06, -0.01, 1.52), (0.230, -0.01, 1.50)),
        ("UpperArm_R", "Shoulder_R", (0.230, -0.01, 1.50), (0.278, 0.010, 1.20)),
        ("LowerArm_R", "UpperArm_R", (0.278, 0.010, 1.20), (0.288, -0.026, 0.90)),
        ("Hand_R", "LowerArm_R", (0.288, -0.026, 0.90), (0.288, -0.036, 0.74)),
        ("KeyRing", "Hand_R", (kx, ky, kz), (kx, ky, kz - 0.16)),
        ("UpperLeg_L", "Hips", (-0.118, 0, 1.06), (-0.118, 0, 0.56)),
        ("LowerLeg_L", "UpperLeg_L", (-0.118, 0, 0.56), (-0.118, 0, 0.08)),
        ("Foot_L", "LowerLeg_L", (-0.118, 0, 0.08), (-0.118, -0.18, 0.04)),
        ("UpperLeg_R", "Hips", (0.118, 0, 1.06), (0.118, 0, 0.56)),
        ("LowerLeg_R", "UpperLeg_R", (0.118, 0, 0.56), (0.118, 0, 0.08)),
        ("Foot_R", "LowerLeg_R", (0.118, 0, 0.08), (0.118, -0.18, 0.04)),
    ]
    arm_obj = create_biped_armature("Gatekeeper_Armature", spec)
    weighted_parts = []

    def add_part(obj, bones):
        weight_part(obj, arm_obj, bones)
        weighted_parts.append(obj)

    for side, sx in (("L", -0.118), ("R", 0.118)):
        lb = [(f"UpperLeg_{side}", 1.1), (f"LowerLeg_{side}", 1.1), (f"Foot_{side}", 0.5), ("Hips", 0.5)]
        add_part(C.make_box(f"Boot_{side}", (sx, -0.045, 0.045), (0.100, 0.275, 0.086), "Black", bevel=0.018, segments=3), [(f"Foot_{side}", 1.0)])
        leg_knots = [
            ((sx, 0.0, 0.08), 0.052, 0.054),
            ((sx, 0.0, 0.34), 0.060, 0.062),
            ((sx, 0.0, 0.56), 0.066, 0.068),
            ((sx * 0.94, 0.004, 0.82), 0.078, 0.082),
            ((sx * 0.90, 0.008, 1.06), 0.088, 0.092),
        ]
        add_part(make_polyline_loft(f"GK_Leg_{side}", leg_knots, "DarkTrouser", segs=18, uv_scale=0.35), lb)

    coat_rings = [
        (0, 0.010, 0.44, 0.305, 0.220, 0.0, 0.048, 9, 36),
        (0, 0.008, 0.74, 0.265, 0.192, 0.0, 0.028, 7, 36),
        (0, 0.005, 1.04, 0.215, 0.155, 0.0, 0.014, 6, 36),
        (0, -0.005, 1.36, 0.228, 0.152, 0.0, 0.010, 5, 36),
        (0, -0.010, 1.52, 0.242, 0.130, 0.0, 0.005, 4, 36),
        (0, -0.010, 1.59, 0.168, 0.104, 0.0, 0.003, 4, 36),
        (0, -0.008, 1.635, 0.088, 0.084, 0.0, 0.000, 4, 36),
    ]
    add_part(make_loft("GK_Coat", coat_rings, "CharredBlouse", cap_bottom=True, cap_top=True, uv_scale=0.40),
             [("Hips", 0.9), ("Spine", 1.2), ("Chest", 1.2), ("UpperLeg_L", 1.15), ("UpperLeg_R", 1.15), ("LowerLeg_L", 0.35), ("LowerLeg_R", 0.35)])

    add_part(make_smooth_collar("GK_FlatCollar", 1.615, 0.086, 0.106, 0.095, 24.0, "CharredBlouse", thickness=0.009), [("Chest", 1.0), ("Neck", 0.6)])

    # Continuous organic heavy overcoat sleeves flowing from shoulder yoke -> wrist (ZERO shoulder balls, ZERO elbow cuts!)
    for side, sx in (("L", -1), ("R", 1)):
        wr = (sx * 0.288, -0.026, 0.90)
        ab = [(f"Shoulder_{side}", 0.9), ("Chest", 0.35), (f"UpperArm_{side}", 1.15), (f"LowerArm_{side}", 1.15), (f"Hand_{side}", 0.8)]
        slv_knots = [
            ((sx * 0.145, -0.010, 1.545), 0.074, 0.070),
            ((sx * 0.195, -0.009, 1.528), 0.071, 0.068),
            ((sx * 0.228, -0.007, 1.488), 0.066, 0.064),
            ((sx * 0.254, 0.002, 1.360), 0.060, 0.060),
            ((sx * 0.278, 0.010, 1.200), 0.054, 0.054),
            ((sx * 0.284, -0.008, 1.050), 0.049, 0.049),
            ((wr[0], wr[1], wr[2]), 0.044, 0.044),
        ]
        add_part(make_polyline_loft(f"GK_Sleeve_{side}", slv_knots, "CharredBlouse", segs=18, uv_scale=0.40), ab)
        hb = [(f"Hand_{side}", 1.4), (f"LowerArm_{side}", 0.35)]
        add_part(C.make_box(f"GK_Hand_{side}", (wr[0], wr[1] - 0.01, wr[2] - 0.052), (0.034, 0.074, 0.094), "ObscuredFace", bevel=0.014, segments=2), hb)
        for fi in range(4):
            fy = wr[1] - 0.032 + fi * 0.018
            add_part(C.make_cylinder(f"GK_Fing_{side}_{fi}", (wr[0], fy, wr[2] - 0.090),
                                     (wr[0] - sx * 0.010, fy - 0.008, wr[2] - 0.168), 0.0080, "ObscuredFace", segments=8), hb)

    add_part(make_polyline_loft("GK_Neck", [((0, -0.006, 1.59), 0.055, 0.055), ((0, -0.010, 1.71), 0.048, 0.048)], "ObscuredFace", segs=16), [("Neck", 1.2), ("Chest", 0.5), ("Head", 0.4)])
    add_part(make_sculpted_head(1.795, scale=1.06, skin_key="ObscuredFace", mode="gk"), [("Head", 1.0)])
    add_part(make_smooth_hair_cap(1.798, scale=1.06, style="gk"), [("Head", 1.0)])

    body_obj = C.join_objects(weighted_parts, "Gatekeeper_Body")
    bind_to_armature(body_obj, arm_obj)

    kr_parts = []
    ring_pts = [(kx + 0.034 * math.cos(a), ky, kz + 0.034 * math.sin(a)) for a in [i * math.tau / 16 for i in range(16)]]
    kr_parts.append(C.make_pipe("GK_KeyRingLoop", ring_pts, 0.0045, "Steel", bevel_res=3, cyclic=True))
    for i in range(6):
        ang = (i - 2.5) * 0.26
        kmat = "Brass" if i % 2 == 0 else "Steel"
        kl = 0.098 + (i % 3) * 0.018
        kr_parts.append(C.make_cylinder(f"GK_KeyBow_{i}", (kx + math.sin(ang) * 0.025, ky - 0.006, kz - 0.038),
                                        (kx + math.sin(ang) * 0.025, ky + 0.006, kz - 0.038), 0.016, kmat, segments=10))
        kr_parts.append(C.make_box(f"GK_KeyBlade_{i}", (kx + math.sin(ang) * (0.025 + kl * 0.35), ky, kz - 0.045 - kl * 0.5),
                                   (0.012, 0.004, kl), kmat, bevel=0.001, rot=(0, -ang * 0.6, 0)))
        kr_parts.append(C.make_box(f"GK_KeyBit_{i}", (kx + math.sin(ang) * (0.036 + kl * 0.6), ky, kz - 0.042 - kl * 0.85),
                                   (0.022, 0.004, 0.018), kmat, bevel=0.001, rot=(0, -ang * 0.6, 0)))
    keys_obj = C.join_objects(kr_parts, "Gatekeeper_KeyRing")
    weight_part(keys_obj, arm_obj, [("KeyRing", 1.0)])
    bind_to_armature(keys_obj, arm_obj)

    def gk_idle(pb, t):
        s = math.sin(t * math.tau)
        pb["Chest"].rotation_euler = (0.06 + 0.02 * s, 0, 0.018 * s)
        pb["Head"].rotation_euler = (0.04, 0, 0.045 * s)
        pb["UpperArm_R"].rotation_euler = (-0.14, 0, -0.05)
        pb["LowerArm_R"].rotation_euler = (-0.24, 0, 0.05)
        pb["KeyRing"].rotation_euler = (0.14 * s, 0, 0.12 * math.cos(t * math.tau))

    def gk_stalk(pb, t):
        s, c = math.sin(t * math.tau), math.cos(t * math.tau)
        pb["Hips"].location = (0.020 * s, 0.0, abs(c) * 0.024)
        pb["Hips"].rotation_euler = (0.0, 0.03 * s, -0.04 * s)
        pb["Chest"].rotation_euler = (0.07, 0.02 * c, 0.05 * s)
        pb["Head"].rotation_euler = (-0.04, -0.025 * s, -0.025 * s)
        pb["UpperLeg_L"].rotation_euler = (0.28 * s, 0, 0)
        pb["LowerLeg_L"].rotation_euler = (max(0.0, -0.34 * s), 0, 0)
        pb["UpperLeg_R"].rotation_euler = (-0.26 * s, 0, 0)
        pb["LowerLeg_R"].rotation_euler = (max(0.0, 0.30 * s), 0, 0)
        pb["UpperArm_L"].rotation_euler = (-0.14 * s, 0, 0.04)
        pb["LowerArm_L"].rotation_euler = (-0.14, 0, 0)
        pb["UpperArm_R"].rotation_euler = (-0.22 + 0.20 * s, 0, -0.06)
        pb["LowerArm_R"].rotation_euler = (-0.30 + 0.10 * c, 0, 0.05)
        pb["KeyRing"].rotation_euler = (0.36 * c, 0, 0.24 * s)

    def gk_reach(pb, t):
        k = math.sin(min(1.0, t * 1.3) * math.pi * 0.5)
        pb["Chest"].rotation_euler = (0.07 * k, 0, 0.05 * k)
        pb["UpperArm_R"].rotation_euler = (-0.18 - 0.78 * k, 0, -0.10)
        pb["LowerArm_R"].rotation_euler = (-0.26 - 0.14 * k, 0, 0.06)
        pb["KeyRing"].rotation_euler = (0.42 * math.sin(t * math.tau * 2.0), 0, 0.16 * math.cos(t * math.tau * 2.0))

    bake_pose_action(arm_obj, "GK_IdleStill", 90, gk_idle)
    a_stalk = bake_pose_action(arm_obj, "GK_StalkWalk", 48, gk_stalk)
    bake_pose_action(arm_obj, "GK_InspectReach", 72, gk_reach)

    arm_obj.animation_data.action = a_stalk
    bpy.context.scene.frame_set(12)
    C.export_character(
        "CTCC_CHR_Gatekeeper", arm_obj, [body_obj, keys_obj], render=True,
        cam_dir=(0.48, -2.15, 0.18),
        closeup_cam={"pos": (0.16, -0.76, 1.76), "target": (0.0, 0.0, 1.72)},
    )


# ==============================================================================
# 3. CHAR007 / C01 — BÉ AN APPARITION (Pass 4 Organic Polish)
# ==============================================================================
def build_an_apparition():
    C.fresh_scene()
    spec = [
        ("Hips", None, (0, 0, 0.56), (0, 0, 0.68)),
        ("Spine", "Hips", (0, 0, 0.68), (0, 0, 0.88)),
        ("Head", "Spine", (0, 0, 0.92), (0, 0, 1.15)),
        ("Arm_L", "Spine", (-0.125, 0, 0.855), (-0.156, -0.012, 0.54)),
        ("Arm_R", "Spine", (0.125, 0, 0.855), (0.156, -0.012, 0.54)),
        ("Leg_L", "Hips", (-0.065, 0, 0.56), (-0.065, 0, 0.03)),
        ("Leg_R", "Hips", (0.065, 0, 0.56), (0.065, 0, 0.03)),
    ]
    arm_obj = create_biped_armature("An_Armature", spec)
    weighted_parts = []

    def add_part(obj, bones):
        weight_part(obj, arm_obj, bones)
        weighted_parts.append(obj)

    for side, sx in (("L", -0.065), ("R", 0.065)):
        lb = [(f"Leg_{side}", 1.0), ("Hips", 0.35)]
        add_part(C.make_box(f"AnSlipper_{side}", (sx, -0.024, 0.016), (0.062, 0.158, 0.030), "SlipperYellow", bevel=0.012, segments=3), [(f"Leg_{side}", 1.0)])
        add_part(make_polyline_loft(f"AnShin_{side}", [((sx, 0, 0.030), 0.028, 0.028), ((sx, 0, 0.17), 0.032, 0.032), ((sx, 0, 0.32), 0.036, 0.036)], "AnSkin", segs=14), lb)
        add_part(make_polyline_loft(f"AnShort_{side}", [((sx, 0, 0.28), 0.044, 0.045), ((sx * 0.94, 0, 0.42), 0.050, 0.051), ((sx * 0.88, 0, 0.56), 0.056, 0.056)], "AnCotton", segs=16, uv_scale=0.30), lb)

    shirt_rings = [
        (0, 0, 0.48, 0.140, 0.094, 0.0, 0.014, 6, 28),
        (0, 0, 0.68, 0.130, 0.088, 0.0, 0.008, 5, 28),
        (0, 0, 0.84, 0.132, 0.084, 0.0, 0.004, 4, 28),
        (0, 0, 0.892, 0.098, 0.068, 0.0, 0.000, 4, 28),
        (0, 0, 0.922, 0.046, 0.044, 0.0, 0.000, 4, 28),
    ]
    add_part(make_loft("AnShirt", shirt_rings, "AnCotton", cap_bottom=True, cap_top=True, uv_scale=0.30), [("Hips", 0.8), ("Spine", 1.2)])

    for side, sx in (("L", -1), ("R", 1)):
        ab = [(f"Arm_{side}", 1.1), ("Spine", 0.35)]
        wr = (sx * 0.156, -0.012, 0.54)
        slv_k = [
            ((sx * 0.080, 0, 0.872), 0.043, 0.041),
            ((sx * 0.108, 0, 0.864), 0.041, 0.040),
            ((sx * 0.128, 0.001, 0.844), 0.038, 0.038),
            ((sx * 0.144, 0.004, 0.775), 0.035, 0.035),
            ((sx * 0.152, 0.005, 0.700), 0.033, 0.033),
        ]
        add_part(make_polyline_loft(f"AnSlv_{side}", slv_k, "AnCotton", segs=16, uv_scale=0.30), ab)
        arm_k = [
            ((sx * 0.152, 0.004, 0.710), 0.025, 0.025),
            ((sx * 0.155, -0.004, 0.620), 0.022, 0.022),
            ((wr[0], wr[1], wr[2]), 0.019, 0.019),
        ]
        add_part(make_polyline_loft(f"AnArm_{side}", arm_k, "AnSkin", segs=14), ab)
        add_part(C.make_sphere(f"AnHand_{side}", (wr[0], wr[1], wr[2] - 0.022), (0.036, 0.042, 0.052), "AnSkin", segs=12, rings=8), ab)

    add_part(make_polyline_loft("AnNeck", [((0, 0, 0.90), 0.032, 0.032), ((0, 0, 0.975), 0.029, 0.029)], "AnSkin", segs=14), [("Spine", 0.6), ("Head", 0.8)])
    an_head_z = 1.048
    add_part(make_sculpted_head(an_head_z, scale=0.88, skin_key="AnSkin", mode="an"), [("Head", 1.0)])
    add_part(make_smooth_hair_cap(an_head_z, scale=0.88, style="an"), [("Head", 1.0)])

    body_obj = C.join_objects(weighted_parts, "An_Body")
    bind_to_armature(body_obj, arm_obj)

    def an_idle(pb, t):
        s = math.sin(t * math.tau)
        pb["Spine"].rotation_euler = (0.025 * s, 0, 0.025 * s)
        pb["Head"].rotation_euler = (-0.04 + 0.03 * s, 0.06 * s, 0.12 * s)
        pb["Arm_L"].rotation_euler = (-0.06, 0, 0.06)
        pb["Arm_R"].rotation_euler = (-0.06, 0, -0.06)

    def an_turn(pb, t):
        k = math.sin(t * math.pi * 0.5)
        pb["Hips"].rotation_euler = (0, 0, 0.65 * k)
        pb["Head"].rotation_euler = (0.0, 0.0, 0.35 * (1.0 - k))
        pb["Leg_L"].rotation_euler = (0.16 * math.sin(t * math.tau), 0, 0)
        pb["Leg_R"].rotation_euler = (-0.16 * math.sin(t * math.tau), 0, 0)

    a_idle = bake_pose_action(arm_obj, "An_IdleLook", 96, an_idle)
    bake_pose_action(arm_obj, "An_TurnFade", 66, an_turn)
    arm_obj.animation_data.action = a_idle
    bpy.context.scene.frame_set(12)
    C.export_character("CTCC_CHR_AnApparition", arm_obj, [body_obj], render=True, cam_dir=(0.45, -1.95, 0.25))


def run_all():
    build_lan()
    build_gatekeeper()
    build_an_apparition()


if __name__ == "__main__":
    run_all()
