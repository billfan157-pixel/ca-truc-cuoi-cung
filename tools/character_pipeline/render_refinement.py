"""Render matched character portraits with the same camera and lighting in Blender."""

import argparse
import math
from pathlib import Path
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from render_character_qa import (aim_cam, find_armature_and_face, render_wireframe_overlay,
                                 reset_rest_pose, setup_neutral_studio)


def turntable(scene, rig, output, height, samples):
    idle_name = {"Lan": "Lan_IdleDesk", "An": "An_IdleLook", "GK": "GK_IdleStill"}[rig.name.split("_", 1)[0]]
    rig.animation_data.action = bpy.data.actions[idle_name]
    scene.frame_set(15)
    posed = [bone.matrix_basis.copy() for bone in rig.pose.bones]
    rig.animation_data.action = None
    for bone, matrix in zip(rig.pose.bones, posed):
        bone.matrix_basis = matrix
    center = Vector((0, 0, 0.51 * height))
    camera, _, _, _ = setup_neutral_studio(scene, center, diag=height, samples=samples, res=(384, 512))
    camera.rotation_mode = "QUATERNION"
    scene.frame_start = 1
    scene.frame_end = 24
    scene.render.fps = 12
    scene.render.image_settings.file_format = "FFMPEG"
    scene.render.ffmpeg.format = "MPEG4"
    scene.render.ffmpeg.codec = "H264"
    scene.render.ffmpeg.constant_rate_factor = "MEDIUM"
    scene.render.filepath = str((output / "turntable.mp4").resolve())
    for frame in range(1, 25):
        angle = math.tau * (frame - 1) / 24.0
        camera.location = center + Vector((math.sin(angle) * height * 1.72,
                                          -math.cos(angle) * height * 1.72, height * 0.06))
        camera.rotation_quaternion = (center - camera.location).to_track_quat("-Z", "Y")
        camera.data.lens = 64
        camera.keyframe_insert("location", frame=frame)
        camera.keyframe_insert("rotation_quaternion", frame=frame)
    bpy.ops.render.render(animation=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("out", type=Path)
    parser.add_argument("--lod", type=int, default=0, choices=range(3))
    parser.add_argument("--samples", type=int, default=20)
    parser.add_argument("--speech", action="store_true")
    parser.add_argument("--expressions", action="store_true")
    parser.add_argument("--turntable-height", type=float)
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:])
    bpy.ops.wm.open_mainfile(filepath=str(args.source.resolve()), load_ui=False)
    textures = Path(__file__).resolve().parents[2] / "Prototype/Assets/CTCC/Art/Textures"
    for image in bpy.data.images:
        if image.source == "FILE" and not image.packed_file and not Path(bpy.path.abspath(image.filepath)).is_file():
            fallback = textures / Path(image.filepath).name
            if fallback.is_file():
                image.filepath = str(fallback)
    rig, face = find_armature_and_face()
    reset_rest_pose(rig)
    scene = bpy.context.scene
    midpoint = sum((rig.matrix_world @ rig.data.bones[f"mixamorig:{side}Eye"].head_local
                    for side in ("Left", "Right")), Vector()) / 2
    center = midpoint - Vector((0, 0.048, 0.030))
    camera, _, lights, world = setup_neutral_studio(scene, center, diag=0.58, samples=args.samples, res=(560, 560))
    aim_cam(camera, center + Vector((0.17, -0.72, 0.015)), center, 74)
    args.out.mkdir(parents=True, exist_ok=True)
    for obj in scene.objects:
        if obj.type == "MESH" and obj.name[-5:-1] == "_LOD":
            obj.hide_render = not obj.name.endswith(f"_LOD{args.lod}")
            if not obj.hide_render and args.speech and obj.data.shape_keys:
                obj.data.shape_keys.key_blocks["Viseme_AA"].value = 0.48
                obj.data.shape_keys.key_blocks["Jaw_Open"].value = 0.32
    scene.camera.data.dof.use_dof = False
    if args.turntable_height:
        # Remove the portrait-only rig before setting up the full-body render.
        for obj in [camera, *lights, bpy.data.objects["_StudioFloor"]]:
            bpy.data.objects.remove(obj, do_unlink=True)
        turntable(scene, rig, args.out, args.turntable_height, args.samples)
        return
    scene.render.filepath = str((args.out / "neutral.png").resolve())
    bpy.ops.render.render(write_still=True)
    if args.expressions:
        for name, values in {
            "blink": {"Blink_L": 0.98, "Blink_R": 0.98},
            "warm": {"Mouth_Warm": 0.62, "Mouth_Smile_L": 0.14},
            "concern": {"Brow_Concern": 0.56, "Brow_InnerUp": 0.25},
            "aa": {"Viseme_AA": 0.52, "Jaw_Open": 0.22},
            "pp": {"Viseme_PP": 0.70},
        }.items():
            for key in face.data.shape_keys.key_blocks:
                key.value = values.get(key.name, 0)
            scene.render.filepath = str((args.out / f"{name}.png").resolve())
            bpy.ops.render.render(write_still=True)
        for key in face.data.shape_keys.key_blocks:
            key.value = 0
        render_wireframe_overlay(scene, camera, (args.out / "wireframe.png").resolve(),
                                 camera.location, center, camera.data.lens)
    lights[0].data.size = 0.032
    lights[0].data.color = (1.0, 1.0, 1.0)
    lights[1].hide_render = True
    lights[2].hide_render = True
    world.node_tree.nodes.get("Background").inputs["Strength"].default_value = 0.16
    scene.render.filepath = str((args.out / "hard-key.png").resolve())
    bpy.ops.render.render(write_still=True)


if __name__ == "__main__":
    main()
