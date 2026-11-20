"""CMU MoCap BVH Retargeting & Lifelike Character Performance Authoring for Blender 4.5 (M2-C).

Retargets real optical motion capture from the commercial-clear CMU Graphics Lab Motion Capture
Database (cgspeed 2010 MotionBuilder-friendly BVH release) onto MPFB2 `mixamo_unity` rigs using
invariant world-space T-pose delta rotations, temporal smoothing, foot-floor grounding, cyclic loop
crossfading, articulated 5-finger hand poses, chest breathing, and custom hero staging.
"""

import math
from pathlib import Path

import bpy
from mathutils import Euler, Matrix, Quaternion, Vector

ROOT = Path(__file__).resolve().parents[2]
CMU_DIR = ROOT / "ArtSource" / "Animation" / "CMU"

# Exact hierarchical bone correspondence: CMU cgspeed BVH -> MPFB2 mixamo_unity
CMU_TO_MIXAMO = [
    ("Hips", "mixamorig:Hips"),
    ("LowerBack", "mixamorig:Spine"),
    ("Spine", "mixamorig:Spine1"),
    ("Spine1", "mixamorig:Spine2"),
    ("Neck1", "mixamorig:Neck"),
    ("Head", "mixamorig:Head"),
    ("LeftShoulder", "mixamorig:LeftShoulder"),
    ("LeftArm", "mixamorig:LeftArm"),
    ("LeftForeArm", "mixamorig:LeftForeArm"),
    ("LeftHand", "mixamorig:LeftHand"),
    ("RightShoulder", "mixamorig:RightShoulder"),
    ("RightArm", "mixamorig:RightArm"),
    ("RightForeArm", "mixamorig:RightForeArm"),
    ("RightHand", "mixamorig:RightHand"),
    ("LeftUpLeg", "mixamorig:LeftUpLeg"),
    ("LeftLeg", "mixamorig:LeftLeg"),
    ("LeftFoot", "mixamorig:LeftFoot"),
    ("RightUpLeg", "mixamorig:RightUpLeg"),
    ("RightLeg", "mixamorig:RightLeg"),
    ("RightFoot", "mixamorig:RightFoot"),
]


def _import_cmu_bvh(bvh_path: Path):
    """Import a cgspeed CMU BVH (120 fps, Y-up, +Z facing) cleanly into Blender (Z-up, -Y facing)."""
    bpy.ops.object.select_all(action="DESELECT")
    # cgspeed BVH has frame 1 as +Z facing T-pose in Y-up. Importing with axis_forward='-Z', axis_up='Y'
    # maps BVH +Z forward -> Blender -Y forward (matching MPFB2 human!) and BVH +Y up -> Blender +Z up!
    bpy.ops.import_anim.bvh(
        filepath=str(bvh_path),
        global_scale=0.056444,  # CMU inches/scale -> approx meters (we also compute exact leg-ratio!)
        frame_start=1,
        use_fps_scale=False,
        update_scene_fps=False,
        update_scene_duration=False,
        axis_forward="-Z",
        axis_up="Y",
        rotate_mode="QUATERNION",
    )
    src = bpy.context.active_object
    return src


def _world_rot3(arm_obj, bone_name):
    pb = arm_obj.pose.bones.get(bone_name)
    if pb is None:
        return Matrix.Identity(3)
    return (arm_obj.matrix_world @ pb.matrix).to_3x3().normalized()


def _bone_world_head(arm_obj, bone_name):
    pb = arm_obj.pose.bones.get(bone_name)
    if pb is None:
        return Vector((0, 0, 0))
    return arm_obj.matrix_world @ pb.head


def _set_target_tpose(rig_obj):
    """Align MPFB2 mixamo_unity relaxed A-pose into a clean reference T-pose matching CMU frame 1."""
    for pb in rig_obj.pose.bones:
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = Quaternion((1, 0, 0, 0))
        pb.location = Vector((0, 0, 0))
    bpy.context.view_layer.update()

    # Rotate LeftArm (+X) & RightArm (-X) upward from relaxed A-pose (~-45 deg) to horizontal T-pose!
    for side, target_dir in (("Left", Vector((1, 0, 0))), ("Right", Vector((-1, 0, 0)))):
        for seg in ("Arm", "ForeArm"):
            bname = f"mixamorig:{side}{seg}"
            pb = rig_obj.pose.bones.get(bname)
            if not pb:
                continue
            cur_dir = (pb.tail - pb.head).normalized()
            q_world = cur_dir.rotation_difference(target_dir)
            m_world = (rig_obj.matrix_world @ pb.matrix).to_3x3().normalized()
            m_new = q_world.to_matrix() @ m_world
            _apply_world_rot_to_pose_bone(rig_obj, pb, m_new)
            bpy.context.view_layer.update()

    # Align legs vertically straight down (-Z) to match CMU T-pose legs cleanly
    for side in ("Left", "Right"):
        for seg in ("UpLeg", "Leg"):
            bname = f"mixamorig:{side}{seg}"
            pb = rig_obj.pose.bones.get(bname)
            if not pb:
                continue
            cur_dir = (pb.tail - pb.head).normalized()
            q_world = cur_dir.rotation_difference(Vector((0, 0, -1)))
            m_world = (rig_obj.matrix_world @ pb.matrix).to_3x3().normalized()
            _apply_world_rot_to_pose_bone(rig_obj, pb, q_world.to_matrix() @ m_world)
            bpy.context.view_layer.update()


def _apply_world_rot_to_pose_bone(rig_obj, pb, target_world_mat3):
    """Set pb.rotation_quaternion so its evaluated world 3x3 orientation equals target_world_mat3."""
    rest_mat = pb.bone.matrix_local.to_3x3()
    if pb.parent:
        parent_world = (rig_obj.matrix_world @ pb.parent.matrix).to_3x3().normalized()
        parent_rest = pb.parent.bone.matrix_local.to_3x3()
        # World = parent_world @ (parent_rest^T @ rest_mat) @ basis
        rel_rest = parent_rest.transposed() @ rest_mat
        basis3 = (parent_world @ rel_rest).transposed() @ target_world_mat3
    else:
        basis3 = (rig_obj.matrix_world.to_3x3() @ rest_mat).transposed() @ target_world_mat3
    pb.rotation_mode = "QUATERNION"
    pb.rotation_quaternion = basis3.to_quaternion().normalized()


def apply_finger_curl(rig_obj, side, curl_index=0.22, curl_middle=0.28, curl_ring=0.35, curl_pinky=0.42,
                      curl_thumb=0.18, spread=0.0):
    """Pose articulated 3-phalanx fingers cleanly (CMU mocap omits fingers!)."""
    fingers = [
        ("Index", curl_index, -spread),
        ("Middle", curl_middle, 0.0),
        ("Ring", curl_ring, spread * 0.8),
        ("Pinky", curl_pinky, spread * 1.4),
    ]
    sgn = 1.0 if side == "Left" else -1.0
    for fname, curl, sp in fingers:
        for seg, mult in ((1, 0.72), (2, 1.05), (3, 0.85)):
            pb = rig_obj.pose.bones.get(f"mixamorig:{side}Hand{fname}{seg}")
            if pb:
                pb.rotation_mode = "QUATERNION"
                e = Euler((curl * mult, 0.0, sp * sgn if seg == 1 else 0.0), "XYZ")
                pb.rotation_quaternion = e.to_quaternion()
    for seg, angles in (
        (1, (curl_thumb * 0.55, 0.25 * sgn, -0.32 * sgn)),
        (2, (curl_thumb * 0.85, 0.0, -0.10 * sgn)),
        (3, (curl_thumb * 0.75, 0.0, 0.0)),
    ):
        pb = rig_obj.pose.bones.get(f"mixamorig:{side}HandThumb{seg}")
        if pb:
            pb.rotation_mode = "QUATERNION"
            pb.rotation_quaternion = Euler(angles, "XYZ").to_quaternion()


def add_pose_delta(rig_obj, bone_name, euler_xyz, weight=1.0):
    """Compose a gentle local Euler delta onto a pose bone's current quaternion."""
    pb = rig_obj.pose.bones.get(bone_name)
    if not pb:
        return
    pb.rotation_mode = "QUATERNION"
    dq = Euler((euler_xyz[0] * weight, euler_xyz[1] * weight, euler_xyz[2] * weight), "XYZ").to_quaternion()
    pb.rotation_quaternion = (pb.rotation_quaternion @ dq).normalized()


def add_world_delta(rig_obj, bone_name, rx_deg=0.0, ry_deg=0.0, rz_deg=0.0):
    """Apply an exact world-space XYZ Euler rotation (degrees) onto a pose bone regardless of local bone roll."""
    pb = rig_obj.pose.bones.get(bone_name)
    if not pb:
        return
    bpy.context.view_layer.update()
    cur_w = (rig_obj.matrix_world @ pb.matrix).to_3x3().normalized()
    delta_w = Euler((math.radians(rx_deg), math.radians(ry_deg), math.radians(rz_deg)), "XYZ").to_matrix()
    _apply_world_rot_to_pose_bone(rig_obj, pb, delta_w @ cur_w)


def aim_bone_world(rig_obj, bone_name, target_dir_world, twist_deg=0.0):
    """Rotate pose bone cleanly so its world-space head->tail vector points along target_dir_world."""
    pb = rig_obj.pose.bones.get(bone_name)
    if not pb:
        return
    bpy.context.view_layer.update()
    p0 = rig_obj.matrix_world @ pb.head
    p1 = rig_obj.matrix_world @ pb.tail
    cur_dir = (p1 - p0).normalized()
    tgt_dir = Vector(target_dir_world).normalized()
    q_align = cur_dir.rotation_difference(tgt_dir)
    if abs(twist_deg) > 1e-4:
        q_align = Quaternion(tgt_dir, math.radians(twist_deg)) @ q_align
    cur_w = (rig_obj.matrix_world @ pb.matrix).to_3x3().normalized()
    _apply_world_rot_to_pose_bone(rig_obj, pb, q_align.to_matrix() @ cur_w)


def solve_two_bone_leg_ik(rig_obj, side, target_toe_world, foot_vec_world):
    """Analytically solve 2-bone leg IK (UpLeg -> Leg -> Foot) so ToeBase lands at target_toe_world."""
    upleg_name = f"mixamorig:{side}UpLeg"
    leg_name = f"mixamorig:{side}Leg"
    foot_name = f"mixamorig:{side}Foot"

    upleg_pb = rig_obj.pose.bones.get(upleg_name)
    leg_pb = rig_obj.pose.bones.get(leg_name)
    foot_pb = rig_obj.pose.bones.get(foot_name)
    if not (upleg_pb and leg_pb and foot_pb):
        return

    L1 = (upleg_pb.bone.tail_local - upleg_pb.bone.head_local).length
    L2 = (leg_pb.bone.tail_local - leg_pb.bone.head_local).length
    L3 = (foot_pb.bone.tail_local - foot_pb.bone.head_local).length

    foot_dir = Vector(foot_vec_world).normalized()
    target_ankle = Vector(target_toe_world) - foot_dir * L3

    bpy.context.view_layer.update()
    H = rig_obj.matrix_world @ upleg_pb.head
    vec_ha = target_ankle - H
    dist = vec_ha.length
    max_reach = (L1 + L2) * 0.998
    min_reach = abs(L1 - L2) + 0.01
    d_clamped = max(min_reach, min(max_reach, dist))
    dir_ha = vec_ha.normalized() if dist > 1e-5 else Vector((0.0, 0.0, -1.0))

    cos_a = max(-1.0, min(1.0, (L1 * L1 + d_clamped * d_clamped - L2 * L2) / (2.0 * L1 * d_clamped)))
    sin_a = math.sqrt(max(0.0, 1.0 - cos_a * cos_a))

    # Forward knee pole (-Y in Blender metric space, with slight natural outward knee alignment)
    sgn = 1.0 if side == "Left" else -1.0
    pole = Vector((0.035 * sgn, -1.0, 0.05)).normalized()
    perp = pole - dir_ha * pole.dot(dir_ha)
    if perp.length < 1e-5:
        perp = Vector((0.0, -1.0, 0.0))
    else:
        perp.normalize()

    K = H + (dir_ha * cos_a + perp * sin_a) * L1
    aim_bone_world(rig_obj, upleg_name, K - H)
    bpy.context.view_layer.update()

    K_actual = rig_obj.matrix_world @ leg_pb.head
    aim_bone_world(rig_obj, leg_name, target_ankle - K_actual)
    bpy.context.view_layer.update()

    A_actual = rig_obj.matrix_world @ foot_pb.head
    aim_bone_world(rig_obj, foot_name, Vector(target_toe_world) - A_actual)
    bpy.context.view_layer.update()


def retarget_cmu_clip(rig_obj, action_name, bvh_filename, src_start, src_end, step=4,
                      in_place=True, cyclic_blend=6, scale_rot=1.0, bone_scales=None,
                      post_frame_fn=None, target_world_speed=None, stride_cycles=3):
    """Retarget a real CMU BVH slice [src_start..src_end] (120fps, step=4 => 30fps) onto rig_obj.

    - Computes exact world-space T-pose delta rotations (immune to axis/roll mismatch!)
    - Scales hip bob by exact leg-length ratio
    - Grounds planted stance foot solidly at z = 0.0 (eliminates foot floating/sinking!)
    - Applies optional cyclic crossfade & post_frame_fn (e.g., articulated fingers, clipboard hold, breathing)
    """
    bvh_path = CMU_DIR / bvh_filename
    if not bvh_path.exists():
        raise FileNotFoundError(f"Missing CMU BVH source: {bvh_path}")

    scene = bpy.context.scene
    src_rig = _import_cmu_bvh(bvh_path)

    # 1. Capture CMU Frame 1 T-pose world orientations & leg length
    scene.frame_set(1)
    bpy.context.view_layer.update()
    cmu_tpose = {cmu_b: _world_rot3(src_rig, cmu_b) for cmu_b, _ in CMU_TO_MIXAMO}
    cmu_hips0 = _bone_world_head(src_rig, "Hips")
    cmu_foot0 = _bone_world_head(src_rig, "LeftFoot")
    cmu_leg_h = max(0.2, cmu_hips0.z - cmu_foot0.z)

    # 2. Capture target rig T-pose world orientations & rest foot clearance
    _set_target_tpose(rig_obj)
    tgt_tpose = {tgt_b: _world_rot3(rig_obj, tgt_b) for _, tgt_b in CMU_TO_MIXAMO}
    tgt_hips0 = _bone_world_head(rig_obj, "mixamorig:Hips")
    tgt_foot0 = _bone_world_head(rig_obj, "mixamorig:LeftFoot")
    tgt_toe0 = _bone_world_head(rig_obj, "mixamorig:LeftToeBase")
    tgt_rfoot0 = _bone_world_head(rig_obj, "mixamorig:RightFoot")
    tgt_rtoe0 = _bone_world_head(rig_obj, "mixamorig:RightToeBase")
    tgt_leg_h = max(0.2, tgt_hips0.z - tgt_foot0.z)
    foot_sole_offset = min(tgt_foot0.z, tgt_toe0.z)
    hip_scale = tgt_leg_h / cmu_leg_h
    rest_toe_pos = {"Left": tgt_toe0.copy(), "Right": tgt_rtoe0.copy()}
    rest_foot_vec = {"Left": (tgt_toe0 - tgt_foot0).copy(), "Right": (tgt_rtoe0 - tgt_rfoot0).copy()}

    # Sample CMU frames at 30fps (120 / 4)
    src_frames = list(range(src_start, src_end + 1, step))
    n_out = len(src_frames)

    # Record per-frame quaternions & hip displacement so we can smooth & loop cleanly!
    recorded_quats = {tgt_b: [] for _, tgt_b in CMU_TO_MIXAMO}
    recorded_hips_dz = []
    recorded_hips_dx = []

    # Measure mean heading yaw in CMU clip so walk always faces forward (-Y) cleanly!
    scene.frame_set(src_frames[0])
    bpy.context.view_layer.update()
    h0 = _bone_world_head(src_rig, "Hips")
    scene.frame_set(src_frames[-1])
    bpy.context.view_layer.update()
    h1 = _bone_world_head(src_rig, "Hips")
    disp = Vector((h1.x - h0.x, h1.y - h0.y, 0.0))
    if disp.length > 0.25:
        # Angle from forward (0, -1) to displacement
        yaw_corr = math.atan2(disp.x, -disp.y)
    else:
        # Use Hips facing on first frame
        hm = _world_rot3(src_rig, "Hips") @ cmu_tpose["Hips"].transposed()
        fwd = hm @ Vector((0, -1, 0))
        yaw_corr = math.atan2(fwd.x, -fwd.y)
    q_yaw_fix = Quaternion((0, 0, 1), -yaw_corr)
    m_yaw_fix = q_yaw_fix.to_matrix()

    # Reset target rig before sampling
    if rig_obj.animation_data:
        rig_obj.animation_data.action = None

    for sf in src_frames:
        scene.frame_set(sf)
        bpy.context.view_layer.update()

        # Reset target rig pose first each frame
        for pb in rig_obj.pose.bones:
            pb.rotation_mode = "QUATERNION"
            pb.rotation_quaternion = Quaternion((1, 0, 0, 0))
            pb.location = Vector((0, 0, 0))

        # Apply hierarchical world-space deltas from Hips down to extremities!
        for cmu_b, tgt_b in CMU_TO_MIXAMO:
            pb = rig_obj.pose.bones.get(tgt_b)
            if not pb:
                continue
            r_cmu = _world_rot3(src_rig, cmu_b)
            # World delta from CMU T-pose, yaw-normalized to -Y forward
            delta_w = m_yaw_fix @ (r_cmu @ cmu_tpose[cmu_b].transposed()) @ m_yaw_fix.transposed()
            # Optional rotation scaling (e.g., dampen exaggerated arms/hips for calm demeanor)
            b_scale = (bone_scales or {}).get(tgt_b, scale_rot)
            if abs(b_scale - 1.0) > 1e-3:
                q_d = delta_w.to_quaternion()
                delta_w = Quaternion((1, 0, 0, 0)).slerp(q_d, b_scale).to_matrix()

            target_w = delta_w @ tgt_tpose[tgt_b]
            _apply_world_rot_to_pose_bone(rig_obj, pb, target_w)
            bpy.context.view_layer.update()
            recorded_quats[tgt_b].append(pb.rotation_quaternion.copy())

        # Hip vertical bob + lateral weight shift (in-place removes root forward drift)
        cur_hip = _bone_world_head(src_rig, "Hips")
        rel_hip = m_yaw_fix @ (cur_hip - h0)
        dz = (cur_hip.z - cmu_hips0.z) * hip_scale
        dx = rel_hip.x * hip_scale * 0.45 if in_place else rel_hip.x * hip_scale
        recorded_hips_dz.append(dz)
        recorded_hips_dx.append(dx)

    # Remove temporary CMU BVH armature & its imported BVH action cleanly
    src_act = src_rig.animation_data.action if (src_rig.animation_data and src_rig.animation_data.action) else None
    src_data = src_rig.data
    bpy.data.objects.remove(src_rig, do_unlink=True)
    if src_data and src_data.users == 0:
        bpy.data.armatures.remove(src_data)
    if src_act:
        bpy.data.actions.remove(src_act, do_unlink=True)

    # 3. Gentle 3-tap quaternion smoothing (removes optical marker jitter while preserving crisp motion!)
    for tgt_b, qlist in recorded_quats.items():
        if len(qlist) >= 3:
            sm = [qlist[0]]
            for i in range(1, len(qlist) - 1):
                q_prev = qlist[i - 1]
                q_cur = qlist[i]
                q_next = qlist[i + 1]
                if q_prev.dot(q_cur) < 0:
                    q_prev = -q_prev
                if q_next.dot(q_cur) < 0:
                    q_next = -q_next
                avg = q_prev.slerp(q_next, 0.5).slerp(q_cur, 0.55)
                sm.append(avg.normalized())
            sm.append(qlist[-1])
            recorded_quats[tgt_b] = sm

    # 4. Seamless cyclic blend over last `cyclic_blend` frames if requested
    if cyclic_blend > 0 and n_out > cyclic_blend * 2:
        for tgt_b, qlist in recorded_quats.items():
            q0 = qlist[0]
            for k in range(cyclic_blend):
                idx = n_out - cyclic_blend + k
                alpha = (k + 1) / float(cyclic_blend)
                alpha = alpha * alpha * (3.0 - 2.0 * alpha)
                qi = qlist[idx]
                qt = q0 if qi.dot(q0) >= 0 else -q0
                qlist[idx] = qi.slerp(qt, alpha).normalized()
        for arr in (recorded_hips_dz, recorded_hips_dx):
            v0 = arr[0]
            for k in range(cyclic_blend):
                idx = n_out - cyclic_blend + k
                alpha = (k + 1) / float(cyclic_blend)
                alpha = alpha * alpha * (3.0 - 2.0 * alpha)
                arr[idx] = arr[idx] * (1.0 - alpha) + v0 * alpha

    # 5. Bake into clean Blender Action with foot-floor clamping & post_frame_fn!
    act = bpy.data.actions.new(name=action_name)
    act.use_fake_user = True
    if not rig_obj.animation_data:
        rig_obj.animation_data_create()
    rig_obj.animation_data.action = act

    # Remove mean lateral drift for in-place clips
    mean_dx = sum(recorded_hips_dx) / max(1, len(recorded_hips_dx)) if in_place else 0.0

    for fi in range(n_out):
        frame_no = fi + 1
        t_norm = fi / float(max(1, n_out - 1))
        scene.frame_set(frame_no)

        for pb in rig_obj.pose.bones:
            pb.rotation_mode = "QUATERNION"
            pb.rotation_quaternion = Quaternion((1, 0, 0, 0))
            pb.location = Vector((0, 0, 0))

        for _, tgt_b in CMU_TO_MIXAMO:
            pb = rig_obj.pose.bones.get(tgt_b)
            if pb:
                pb.rotation_quaternion = recorded_quats[tgt_b][fi].copy()

        # Apply character-specific pose refinements (articulated fingers, subtle breathing, arm refinement)
        if post_frame_fn is not None:
            post_frame_fn(rig_obj, t_norm, fi)

        bpy.context.view_layer.update()

        # Ground lowest foot/toe strictly onto z = 0.0 (or solve analytical stride-locked 2-bone leg IK if target_world_speed is set!)
        hips_pb = rig_obj.pose.bones.get("mixamorig:Hips")
        if hips_pb and target_world_speed is not None and target_world_speed > 0.1:
            fps = 30.0
            n_period = max(1, n_out - 1)
            cycle_frames = max(6.0, float(n_period) / float(max(1, stride_cycles)))
            # Keep hips slightly lowered by 3.4 cm with subtle double-frequency gait bob so 2-bone IK reaches full stride cleanly
            gait_phase = (fi / cycle_frames) % 1.0
            bob_z = -0.036 + 0.007 * math.cos(gait_phase * math.tau * 2.0)
            sway_x = 0.011 * math.sin(gait_phase * math.tau)
            hip_rest_inv = hips_pb.bone.matrix_local.to_3x3().transposed()
            hips_pb.location = hip_rest_inv @ Vector((sway_x, 0.0, bob_z))
            bpy.context.view_layer.update()

            stance_frac = 0.50
            stance_frames = stance_frac * cycle_frames
            d_stance = target_world_speed * (stance_frames / fps)

            for side, ph_off in (("Left", 0.0), ("Right", 0.5)):
                u = (gait_phase + ph_off) % 1.0
                base_toe = rest_toe_pos[side]
                base_fvec = rest_foot_vec[side]
                if u <= stance_frac + 1e-6:
                    # Stance phase: foot is planted flat on floor and sweeps backward (+Y) at exact constant target_world_speed!
                    s_st = u / stance_frac
                    ty = base_toe.y + d_stance * (s_st - 0.5)
                    tz = base_toe.z
                    fvec = base_fvec
                else:
                    # Swing phase: foot lifts well above stance threshold (> +18 mm) and swings forward from +d_stance/2 to -d_stance/2
                    s_sw = (u - stance_frac) / (1.0 - stance_frac)
                    s_smooth = s_sw * s_sw * (3.0 - 2.0 * s_sw)
                    ty = base_toe.y + d_stance * (0.5 - s_smooth)
                    lift = 0.016 + 0.042 * math.sin(s_sw * math.pi)
                    tz = base_toe.z + lift
                    # Subtle natural ankle dorsiflexion during swing
                    pitch_tilt = -0.012 * math.sin(s_sw * math.pi)
                    fvec = Vector((base_fvec.x, base_fvec.y, base_fvec.z + pitch_tilt))
                solve_two_bone_leg_ik(rig_obj, side, Vector((base_toe.x + sway_x * 0.35, ty, tz)), fvec)
        elif hips_pb:
            lf = _bone_world_head(rig_obj, "mixamorig:LeftFoot").z
            rf = _bone_world_head(rig_obj, "mixamorig:RightFoot").z
            lt = _bone_world_head(rig_obj, "mixamorig:LeftToeBase").z
            rt = _bone_world_head(rig_obj, "mixamorig:RightToeBase").z
            lowest = min(lf, rf, lt, rt)
            floor_corr = foot_sole_offset - lowest
            # Convert world translation (dx, 0, floor_corr) into Hips bone local space
            hip_rest_inv = hips_pb.bone.matrix_local.to_3x3().transposed()
            world_disp = Vector((recorded_hips_dx[fi] - mean_dx, 0.0, floor_corr))
            hips_pb.location = hip_rest_inv @ world_disp

        # Keyframe all pose bones cleanly
        for pb in rig_obj.pose.bones:
            pb.keyframe_insert(data_path="rotation_quaternion", frame=frame_no)
            if pb.name == "mixamorig:Hips":
                pb.keyframe_insert(data_path="location", frame=frame_no)

    print(f"[MOCAP] Retargeted {bvh_filename}[{src_start}..{src_end}] -> Action '{action_name}' ({n_out} frames @ 30fps, floor-grounded)")
    return act


def author_layered_action(rig_obj, action_name, n_frames, frame_fn):
    """Author a reference-driven keyframed action with floor-grounded hips, organic breathing & articulated hands."""
    scene = bpy.context.scene
    act = bpy.data.actions.new(name=action_name)
    act.use_fake_user = True
    if not rig_obj.animation_data:
        rig_obj.animation_data_create()
    rig_obj.animation_data.action = act

    # Measure rest foot clearance
    for pb in rig_obj.pose.bones:
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = Quaternion((1, 0, 0, 0))
        pb.location = Vector((0, 0, 0))
    bpy.context.view_layer.update()
    foot_sole_offset = min(
        _bone_world_head(rig_obj, "mixamorig:LeftFoot").z,
        _bone_world_head(rig_obj, "mixamorig:LeftToeBase").z,
    )

    for fi in range(n_frames):
        frame_no = fi + 1
        t_norm = fi / float(max(1, n_frames - 1))
        scene.frame_set(frame_no)

        for pb in rig_obj.pose.bones:
            pb.rotation_mode = "QUATERNION"
            pb.rotation_quaternion = Quaternion((1, 0, 0, 0))
            pb.location = Vector((0, 0, 0))

        hip_offset_world = frame_fn(rig_obj, t_norm, fi) or Vector((0, 0, 0))
        bpy.context.view_layer.update()

        # Ground lowest foot cleanly onto z = 0.0!
        hips_pb = rig_obj.pose.bones.get("mixamorig:Hips")
        if hips_pb:
            lowest = min(
                _bone_world_head(rig_obj, "mixamorig:LeftFoot").z,
                _bone_world_head(rig_obj, "mixamorig:RightFoot").z,
                _bone_world_head(rig_obj, "mixamorig:LeftToeBase").z,
                _bone_world_head(rig_obj, "mixamorig:RightToeBase").z,
            )
            floor_corr = foot_sole_offset - lowest
            hip_rest_inv = hips_pb.bone.matrix_local.to_3x3().transposed()
            hips_pb.location = hip_rest_inv @ Vector((hip_offset_world.x, hip_offset_world.y, floor_corr + hip_offset_world.z))

        for pb in rig_obj.pose.bones:
            pb.keyframe_insert(data_path="rotation_quaternion", frame=frame_no)
            if pb.name == "mixamorig:Hips":
                pb.keyframe_insert(data_path="location", frame=frame_no)

    print(f"[ANIM] Authored Action '{action_name}' ({n_frames} frames @ 30fps, floor-grounded)")
    return act
