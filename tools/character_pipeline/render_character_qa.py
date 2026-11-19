"""Studio Visual Inspection, Wireframe, Expression Sheet, Turntable & Action Video Renderer (M2-C).

Opens an editable ArtSource/Blender/CTCC_CHR_*.blend directly in Blender 4.5 and renders real Cycles
verification proofs for rigorous visual critique before scaling or Unity acceptance:
- Full-body front, 3/4, side, back turnaround board
- Close-up neutral studio portrait (front, 3/4, profile) + tight eye/mouth/hand macro close-ups
- Wireframe topology inspection (face loops + full body)
- 12-panel Expression & Vietnamese Viseme Sheet (Neutral, Blink, Warm, Concern, Sorrow, AA, OH, EE, OO, PP, FF, JawOpen)
- 360-degree Turntable MP4 & Action Preview MP4s via Blender VSE
"""

import argparse
import json
import math
import subprocess
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
BLEND_DIR = ROOT / "ArtSource" / "Blender"
OUT_ROOT = ROOT / "output" / "m2c" / "blender-qa"
COMPOSE_SCRIPT = Path(__file__).resolve().parent / "compose_board.py"


def setup_neutral_studio(scene, target_pt, diag=1.6, samples=32, res=(640, 800)):
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = samples
    scene.cycles.use_denoising = True
    scene.cycles.max_bounces = 6
    scene.cycles.diffuse_bounces = 3
    scene.cycles.glossy_bounces = 3
    scene.cycles.transparent_max_bounces = 10
    scene.render.resolution_x = res[0]
    scene.render.resolution_y = res[1]
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.view_settings.view_transform = "AgX"
    scene.view_settings.look = "AgX - Base Contrast"
    scene.view_settings.exposure = -0.15

    # Neutral studio mid-gray gradient backdrop so skin tones, hair & clothes read with zero color cast!
    world = bpy.data.worlds.new("_NeutralStudioWorld")
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    bg.inputs["Color"].default_value = (0.14, 0.145, 0.15, 1.0)
    bg.inputs["Strength"].default_value = 0.55
    scene.world = world

    # Floor plane at z = 0
    bpy.ops.mesh.primitive_plane_add(size=10.0, location=(0.0, 0.0, 0.0))
    floor = bpy.context.object
    floor.name = "_StudioFloor"
    fmat = bpy.data.materials.new("_StudioFloorMat")
    fmat.use_nodes = True
    fmat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.12, 0.125, 0.13, 1.0)
    fmat.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.78
    floor.data.materials.append(fmat)

    def make_area(name, offset, energy, color, size):
        ld = bpy.data.lights.new(name, type="AREA")
        ld.energy = energy
        ld.color = color
        ld.size = size
        lo = bpy.data.objects.new(name, ld)
        lo.location = target_pt + Vector(offset) * diag
        lo.rotation_euler = (target_pt - lo.location).to_track_quat("-Z", "Y").to_euler()
        bpy.context.collection.objects.link(lo)
        return lo

    # Balanced neutral 3-point portrait studio lighting (5400K key, soft neutral-cool fill, subtle separation rim)
    lights = [
        make_area("_Key", (0.95, -1.25, 0.85), diag * diag * 90.0, (1.0, 0.96, 0.92), diag * 0.95),
        make_area("_Fill", (-1.15, -0.95, 0.35), diag * diag * 38.0, (0.86, 0.92, 1.0), diag * 1.25),
        make_area("_Rim", (-0.55, 1.25, 0.75), diag * diag * 65.0, (0.94, 0.96, 1.0), diag * 0.75),
    ]

    cd = bpy.data.cameras.new("_QACam")
    cd.lens = 70.0
    cam = bpy.data.objects.new("_QACam", cd)
    bpy.context.collection.objects.link(cam)
    scene.camera = cam
    return cam, floor, lights, world


def aim_cam(cam, pos, target, lens=70.0):
    cam.data.lens = lens
    cam.location = Vector(pos)
    cam.rotation_euler = (Vector(target) - cam.location).to_track_quat("-Z", "Y").to_euler()


def only_lod0():
    for o in bpy.data.objects:
        if o.type == "MESH":
            is_low = ("_LOD1" in o.name) or ("_LOD2" in o.name)
            o.hide_render = is_low
            o.hide_viewport = is_low


def find_armature_and_face():
    arm = next((o for o in bpy.data.objects if o.type == "ARMATURE"), None)
    face = next((o for o in bpy.data.objects if o.type == "MESH" and "_LOD0" in o.name and o.data.shape_keys and len(o.data.shape_keys.key_blocks) > 2), None)
    return arm, face


def reset_rest_pose(arm):
    if arm and arm.animation_data:
        arm.animation_data.action = None
    if arm:
        for pb in arm.pose.bones:
            pb.rotation_mode = "QUATERNION"
            pb.rotation_quaternion = (1, 0, 0, 0)
            pb.location = (0, 0, 0)


def make_board(panels, cols, cw, ch, title, subtitle, out_path):
    spec = {
        "panels": [[lbl, str(p)] for lbl, p in panels],
        "cols": cols,
        "cw": cw,
        "ch": ch,
        "title": title,
        "subtitle": subtitle,
        "out_path": str(out_path),
    }
    spec_file = Path(out_path).with_suffix(".board.json")
    spec_file.parent.mkdir(parents=True, exist_ok=True)
    spec_file.write_text(json.dumps(spec, ensure_ascii=False, indent=2), encoding="utf-8")
    subprocess.run(["python", str(COMPOSE_SCRIPT), str(spec_file)], check=True)


def render_wireframe_overlay(scene, cam, out_path, pos, target, lens=72.0):
    """Render true Cycles wireframe topology inspection over neutral clay shader!"""
    orig_mats = {}
    clay = bpy.data.materials.new("_ClayWireMat")
    clay.use_nodes = True
    nodes = clay.node_tree.nodes
    links = clay.node_tree.links
    nodes.clear()
    out = nodes.new("ShaderNodeOutputMaterial")
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    wf = nodes.new("ShaderNodeWireframe")
    wf.use_pixel_size = True
    wf.inputs["Size"].default_value = 0.85
    mix = nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.inputs["A"].default_value = (0.58, 0.56, 0.54, 1.0)
    mix.inputs["B"].default_value = (0.04, 0.06, 0.08, 1.0)
    links.new(wf.outputs["Fac"], mix.inputs["Factor"])
    links.new(mix.outputs["Result"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.65
    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])

    # Non-destructive Blender view-layer clay wireframe override (preserves multi-material slot indices!)
    vl = scene.view_layers[0]
    prev_override = vl.material_override
    vl.material_override = clay
    aim_cam(cam, pos, target, lens=lens)
    scene.render.filepath = str(out_path)
    bpy.ops.render.render(write_still=True)
    vl.material_override = prev_override


def encode_frames_mp4(frames_dir: Path, pattern: str, out_mp4: Path, fps=24):
    frames = sorted(frames_dir.glob(pattern))
    if not frames:
        return
    bpy.ops.wm.read_factory_settings(use_empty=True)
    img0 = bpy.data.images.load(str(frames[0]))
    w, h = int(img0.size[0]), int(img0.size[1])
    bpy.data.images.remove(img0)
    sc = bpy.context.scene
    sc.render.resolution_x = w
    sc.render.resolution_y = h
    sc.render.fps = fps
    sc.frame_start = 1
    sc.frame_end = len(frames)
    sc.render.image_settings.file_format = "FFMPEG"
    sc.render.ffmpeg.format = "MPEG4"
    sc.render.ffmpeg.codec = "H264"
    sc.render.ffmpeg.constant_rate_factor = "MEDIUM"
    sc.render.ffmpeg.ffmpeg_preset = "GOOD"
    sc.render.filepath = str(out_mp4)
    sc.sequence_editor_create()
    seq = sc.sequence_editor.sequences.new_image(name="Seq", filepath=str(frames[0]), channel=1, frame_start=1)
    for fp in frames[1:]:
        seq.elements.append(fp.name)
    bpy.ops.render.render(animation=True)
    print(f"[VIDEO] Encoded {out_mp4} ({len(frames)} frames @ {fps}fps)")


def inspect_character(char_name: str, height_m: float, render_videos: bool = False, base_samples: int = 24, videos_only: bool = False):
    blend_path = BLEND_DIR / f"{char_name}.blend"
    out_dir = OUT_ROOT / char_name
    out_dir.mkdir(parents=True, exist_ok=True)

    bpy.ops.wm.open_mainfile(filepath=str(blend_path))
    only_lod0()
    arm, face = find_armature_and_face()
    scene = bpy.context.scene

    # Put character into Idle frame 15 for natural lifelike inspection pose (NOT T-pose!)
    if arm and arm.animation_data and arm.animation_data.action:
        scene.frame_set(15)
        bpy.context.view_layer.update()

    mid_z = height_m * 0.52
    head_z = height_m * 0.915
    cam, floor, lights, world = setup_neutral_studio(scene, Vector((0, 0, mid_z)), diag=height_m, samples=base_samples, res=(540, 720))

    # 1. Full-body 4-angle turnaround (Front, 3/4, Side, Back)
    dist = height_m * 1.95
    if videos_only:
        _render_videos(scene, cam, arm, out_dir, char_name, dist, mid_z)
        return
    turn_panels = []
    for lbl, fname, ang_deg in [
        ("Full Front", "full_front.png", 0),
        ("Full 3/4", "full_34.png", 34),
        ("Full Side Profile", "full_side.png", 86),
        ("Full Back (Bun & Drape)", "full_back.png", 180),
    ]:
        rad = math.radians(ang_deg)
        pos = (math.sin(rad) * dist, -math.cos(rad) * dist, mid_z + 0.08)
        aim_cam(cam, pos, (0, 0, mid_z), lens=65.0)
        p = out_dir / fname
        scene.render.filepath = str(p)
        bpy.ops.render.render(write_still=True)
        turn_panels.append((lbl, p))

    # 2. Close-up portraits (Front, 3/4, Profile, Macro Eye/Mouth, Chest Badge & Hand Close-up)
    scene.render.resolution_x = 560
    scene.render.resolution_y = 560
    scene.cycles.samples = max(24, int(base_samples * 1.25))
    portrait_panels = []

    # Compute exact evaluated eye center & hand center from live posed rig!
    depsgraph = bpy.context.evaluated_depsgraph_get()
    eye_objs = [o for o in bpy.data.objects if o.type == "MESH" and not o.hide_render and "_Eye_" in o.name]
    if eye_objs:
        centers = []
        for eo in eye_objs:
            ev = eo.evaluated_get(depsgraph)
            me = ev.to_mesh()
            c = sum((eo.matrix_world @ v.co for v in me.vertices), Vector((0, 0, 0))) / max(1, len(me.vertices))
            ev.to_mesh_clear()
            centers.append(c)
        eye_mid = sum(centers, Vector((0, 0, 0))) / len(centers)
    else:
        eye_mid = Vector((0, -0.10, height_m * 0.925))

    face_mid = eye_mid + Vector((0.0, 0.02, -0.026 * (height_m / 1.60)))
    hand_pos = Vector((-0.14, -0.14, height_m * 0.56))
    if arm and "mixamorig:RightHand" in arm.pose.bones:
        rh = arm.pose.bones["mixamorig:RightHand"]
        hand_pos = arm.matrix_world @ rh.head.lerp(rh.tail, 0.55)

    scale = height_m / 1.60
    for lbl, fname, cam_pos, tgt_pos, lens in [
        ("Portrait Front", "portrait_front.png", face_mid + Vector((0.0, -0.54 * scale, 0.01)), face_mid, 85.0),
        ("Portrait 3/4", "portrait_34.png", face_mid + Vector((0.28 * scale, -0.46 * scale, 0.02)), face_mid, 85.0),
        ("Portrait Profile", "portrait_side.png", face_mid + Vector((0.56 * scale, 0.02, 0.01)), face_mid + Vector((0.0, 0.05 * scale, 0.0)), 80.0),
        ("Macro Eyes & Brows", "macro_eyes.png", eye_mid + Vector((0.02, -0.26 * scale, 0.004)), eye_mid, 105.0),
        ("Bust, Collar & ID Badge", "closeup_bust.png", face_mid + Vector((0.10 * scale, -0.58 * scale, -0.22 * scale)), face_mid + Vector((0.02, 0.0, -0.23 * scale)), 78.0),
        ("Articulated Hand Close-up", "closeup_hand.png", hand_pos + Vector((-0.16 * scale, -0.34 * scale, 0.04)), hand_pos, 95.0),
    ]:
        aim_cam(cam, cam_pos, tgt_pos, lens=lens)
        p = out_dir / fname
        scene.render.filepath = str(p)
        bpy.ops.render.render(write_still=True)
        portrait_panels.append((lbl, p))

    # 3. Wireframe Topology Inspection (Head Loops + Full Body)
    wf_head = out_dir / "wireframe_head.png"
    wf_body = out_dir / "wireframe_body.png"
    scene.cycles.samples = 16
    render_wireframe_overlay(scene, cam, wf_head, face_mid + Vector((0.22 * scale, -0.46 * scale, 0.01)), face_mid, lens=85.0)
    render_wireframe_overlay(scene, cam, wf_body, (0.75, -dist * 0.92, mid_z + 0.06), (0, 0, mid_z), lens=65.0)

    make_board(
        turn_panels + [("Wireframe Body", wf_body), ("Wireframe Head Topology", wf_head)],
        3, 480, 600,
        f"{char_name} — M2-C NEUTRAL STUDIO TURNAROUND & TOPOLOGY INSPECTION",
        "Blender 4.5.10 LTS Cycles · AgX · MPFB2 hm08 CC0 Base + Layered Hair Cards + Cloth Simulation",
        out_dir / f"{char_name}_turnaround_board.jpg",
    )
    make_board(
        portrait_panels,
        3, 520, 520,
        f"{char_name} — M2-C CLOSE-UP PORTRAIT, ANATOMY & MATERIAL INSPECTION",
        "Blender 4.5.10 LTS Cycles · Neutral Studio Lighting · SSS Skin, Eyes, Hair Cards, Collar & Hands",
        out_dir / f"{char_name}_portrait_board.jpg",
    )

    # 4. Expression & Vietnamese Viseme Sheet (if face has BlendShapes!)
    if face and face.data.shape_keys and len(face.data.shape_keys.key_blocks) >= 8:
        scene.render.resolution_x = 480
        scene.render.resolution_y = 480
        scene.cycles.samples = 22
        expr_tgt = face_mid + Vector((0.0, 0.0, -0.010 * scale))
        aim_cam(cam, expr_tgt + Vector((0.12 * scale, -0.48 * scale, 0.005)), expr_tgt, lens=82.0)

        shape_meshes = [o for o in bpy.data.objects if o.type == "MESH" and not o.hide_render and o.data.shape_keys and len(o.data.shape_keys.key_blocks) > 2]
        jaw_pb = arm.pose.bones.get("mixamorig:Jaw") if arm else None

        def clear_shapes():
            for mo in shape_meshes:
                for kb in mo.data.shape_keys.key_blocks:
                    kb.value = 0.0
            if jaw_pb:
                jaw_pb.rotation_mode = "QUATERNION"
                jaw_pb.rotation_quaternion = (1, 0, 0, 0)

        expr_specs = [
            ("Neutral", {}, 0.0),
            ("Blink Closed", {"Blink_L": 1.0, "Blink_R": 1.0}, 0.0),
            ("Warm Smile", {"Mouth_Warm": 0.90}, 0.0),
            ("Concern", {"Brow_Concern": 0.95}, 0.0),
            ("Sorrow", {"Mouth_Sorrow": 0.90, "Brow_InnerUp": 0.55}, 0.0),
            ("Viseme AA (Anh)", {"Viseme_AA": 0.95}, 0.16),
            ("Viseme OH (Rồi)", {"Viseme_OH": 0.95}, 0.10),
            ("Viseme EE (Trễ)", {"Viseme_EE": 0.95}, 0.05),
            ("Viseme OO (Chuyện)", {"Viseme_OO": 0.95}, 0.05),
            ("Viseme PP (Mình)", {"Viseme_PP": 0.95}, 0.0),
            ("Viseme FF (Phòng)", {"Viseme_FF": 0.95}, 0.03),
            ("Jaw Open (Teeth/Tongue)", {"Jaw_Open": 0.95}, 0.14),
        ]
        expr_panels = []
        for lbl, weights, jaw_rad in expr_specs:
            clear_shapes()
            for k, val in weights.items():
                for mo in shape_meshes:
                    if k in mo.data.shape_keys.key_blocks:
                        mo.data.shape_keys.key_blocks[k].value = val
            if jaw_pb and jaw_rad > 0.0:
                jaw_pb.rotation_mode = "XYZ"
                jaw_pb.rotation_euler = (jaw_rad, 0.0, 0.0)
            bpy.context.view_layer.update()
            fname = "expr_" + lbl.lower().replace(" ", "_").replace("(", "").replace(")", "").replace("/", "_") + ".png"
            p = out_dir / fname
            scene.render.filepath = str(p)
            bpy.ops.render.render(write_still=True)
            expr_panels.append((lbl, p))
        clear_shapes()

        make_board(
            expr_panels, 4, 420, 420,
            f"{char_name} — M2-C EXPRESSION & VIETNAMESE VISEME SHEET (28 BLENDSHAPES)",
            "Verifying eyelid closure, eyelash tracking, oral cavity/teeth/tongue & Vietnamese speech shapes",
            out_dir / f"{char_name}_expression_sheet.jpg",
        )

    # 5. Optional 360 Turntable + Action Preview Videos
    if render_videos:
        _render_videos(scene, cam, arm, out_dir, char_name, dist, mid_z)


def _render_videos(scene, cam, arm, out_dir: Path, char_name: str, dist: float, mid_z: float):
    # Turntable (36 frames @ 18fps = smooth 2s 360-deg spin)
    tt_dir = out_dir / "turntable_frames"
    tt_dir.mkdir(exist_ok=True)
    for old in tt_dir.glob("*.png"):
        old.unlink()
    scene.render.resolution_x = 540
    scene.render.resolution_y = 720
    scene.cycles.samples = 12
    for fi in range(36):
        rad = (fi / 36.0) * math.tau
        aim_cam(cam, (math.sin(rad) * dist, -math.cos(rad) * dist, mid_z + 0.06), (0, 0, mid_z), lens=65.0)
        scene.render.filepath = str(tt_dir / f"tt_{fi:03d}.png")
        bpy.ops.render.render(write_still=True)

    # Action preview montage (all actions concatenated!)
    act_dir = out_dir / "action_frames"
    act_dir.mkdir(exist_ok=True)
    for old in act_dir.glob("*.png"):
        old.unlink()
    aim_cam(cam, (dist * 0.45, -dist * 0.90, mid_z + 0.08), (0, 0, mid_z), lens=62.0)
    af_idx = 0
    for act in sorted(bpy.data.actions, key=lambda a: a.name):
        if arm:
            arm.animation_data.action = act
        f0, f1 = int(act.frame_range[0]), int(act.frame_range[1])
        step = max(1, (f1 - f0) // 24)
        for fr in range(f0, f1 + 1, step):
            scene.frame_set(fr)
            bpy.context.view_layer.update()
            scene.render.filepath = str(act_dir / f"act_{af_idx:04d}.png")
            bpy.ops.render.render(write_still=True)
            af_idx += 1

    encode_frames_mp4(tt_dir, "tt_*.png", out_dir / f"{char_name}_turntable.mp4", fps=18)
    encode_frames_mp4(act_dir, "act_*.png", out_dir / f"{char_name}_actions.mp4", fps=18)


def main():
    argv = []
    if "--" in __import__("sys").argv:
        argv = __import__("sys").argv[__import__("sys").argv.index("--") + 1:]
    ap = argparse.ArgumentParser()
    ap.add_argument("--char", default="CTCC_CHR_Lan")
    ap.add_argument("--height", type=float, default=1.60)
    ap.add_argument("--samples", type=int, default=24)
    ap.add_argument("--videos", action="store_true")
    ap.add_argument("--videos-only", action="store_true")
    args = ap.parse_args(argv)
    inspect_character(args.char, args.height, render_videos=args.videos, base_samples=args.samples, videos_only=args.videos_only)


if __name__ == "__main__":
    main()
