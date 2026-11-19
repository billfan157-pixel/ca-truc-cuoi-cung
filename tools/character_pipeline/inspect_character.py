"""Inspect the editable or exported character in Blender, without saving it."""

import argparse
import json
import re
import sys
from statistics import median
from pathlib import Path

import bpy


def stride(rig, speed):
    """Estimate signed stance slip using the lower toe and the authored forward axis (-Y)."""
    scene = bpy.context.scene
    action, = [act for act in bpy.data.actions if act.name in ("Lan_StepOut", "GK_StalkWalk")]
    rig.animation_data.action = action
    toes = [rig.pose.bones[f"mixamorig:{side}ToeBase"] for side in ("Left", "Right")]
    fps = scene.render.fps / scene.render.fps_base
    previous = None
    velocities = []
    for frame in range(int(action.frame_range[0]), int(action.frame_range[1]) + 1):
        scene.frame_set(frame)
        points = [rig.matrix_world @ toe.head for toe in toes]
        if previous is not None:
            for i in range(2):
                if points[i].z > min(p.z for p in points) + 0.009 or previous[i].z > min(p.z for p in previous) + 0.009:
                    continue
                if abs(points[i].z - previous[i].z) * fps <= 0.16:
                    velocities.append((points[i].y - previous[i].y) * fps)
        previous = points
    if not velocities:
        raise ValueError(f"No stance samples in {action.name}")
    match = median(velocities)
    return {"action": action.name, "assumed_world_speed_mps": speed, "samples": len(velocities),
            "stance_forward_compensation_median_mps": match,
            "longitudinal_slip_median_mps": median(abs(v - speed) for v in velocities),
            "longitudinal_slip_p95_mps": sorted(abs(v - speed) for v in velocities)[round((len(velocities)-1)*0.95)]}


def inspect():
    rigs = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
    if len(rigs) != 1:
        raise ValueError(f"Expected one armature, found {len(rigs)}")
    rig = rigs[0]
    deform = {bone.name for bone in rig.data.bones}
    meshes = {}
    for obj in sorted(bpy.context.scene.objects, key=lambda item: item.name):
        lod = re.search(r"_LOD([012])$", obj.name)
        if obj.type != "MESH" or not lod:
            continue
        mesh = obj.data
        skin = {group.index for group in obj.vertex_groups if group.name in deform}
        unweighted = sum(not any(g.group in skin and g.weight > 1e-5 for g in v.groups)
                         for v in mesh.vertices)
        influences = max((sum(g.group in skin and g.weight > 1e-5 for g in v.groups)
                          for v in mesh.vertices), default=0)
        weight_error = max((abs(sum(g.weight for g in v.groups if g.group in skin) - 1)
                            for v in mesh.vertices), default=0.0)
        meshes[obj.name] = {
            "lod": int(lod[1]), "vertices": len(mesh.vertices),
            "triangles": sum(len(face.vertices) - 2 for face in mesh.polygons),
            "max_influences": influences, "unweighted": unweighted,
            "max_weight_sum_error": weight_error,
            "materials": [slot.material.name if slot.material else None for slot in obj.material_slots],
            "shapes": list(mesh.shape_keys.key_blocks.keys())[1:] if mesh.shape_keys else [],
        }
    missing_images = [image.name for image in bpy.data.images if image.source == "FILE"
                      and not image.packed_file and not Path(bpy.path.abspath(image.filepath)).is_file()]
    return {"blender": bpy.app.version_string, "bones": sorted(deform), "meshes": meshes,
            "actions": sorted(action.name for action in bpy.data.actions), "missing_images": missing_images}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("asset", type=Path)
    parser.add_argument("report", type=Path)
    parser.add_argument("--stride-speed", type=float)
    parser.add_argument("--verify", action="store_true", help="Fail source/export checks for the M2-C.1 model")
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:])
    if args.asset.suffix.lower() == ".fbx":
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=str(args.asset.resolve()), use_anim=True)
    else:
        bpy.ops.wm.open_mainfile(filepath=str(args.asset.resolve()), load_ui=False)
    report = inspect()
    if args.stride_speed is not None:
        rig, = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
        report["stride"] = stride(rig, args.stride_speed)
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    if args.verify:
        verify(report)


def verify(report):
    meshes = report["meshes"]
    prefix, = {name.split("_", 1)[0] for name in meshes}
    additional = {"Lan": ["Uniform", "HairClip", "Teeth", "Tongue"],
                  "An": ["Clothes", "Teeth", "Tongue"], "GK": ["Coat", "KeyRing"]}[prefix]
    required_shapes = set(meshes[f"{prefix}_Body_LOD0"]["shapes"])
    if len(required_shapes) != 29 or {"Blink_L", "Blink_R", "Viseme_PP", "Jaw_Open"} - required_shapes:
        raise ValueError(f"{prefix}: incomplete LOD0 facial shapes")
    if report["missing_images"]:
        raise ValueError(f"{prefix}: unresolved sources {report['missing_images']}")
    for level in range(3):
        for part in ["Body", "Hair", "Eye_L", "Eye_R", *additional]:
            name = f"{prefix}_{part}_LOD{level}"
            mesh = meshes.get(name)
            if mesh is None or not mesh["triangles"] or not mesh["materials"] or None in mesh["materials"]:
                raise ValueError(f"Missing mesh or material: {name}")
            if mesh["unweighted"] or mesh["max_influences"] > 4 or mesh["max_weight_sum_error"] > 0.001:
                raise ValueError(f"Invalid skin weights: {name}")
            if part == "Body" and set(mesh["shapes"]) != required_shapes:
                raise ValueError(f"Facial shapes differ at {name}")


if __name__ == "__main__":
    main()
