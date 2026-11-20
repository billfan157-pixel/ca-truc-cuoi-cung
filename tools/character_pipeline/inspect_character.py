"""Inspect the editable or exported character in Blender, without saving it."""

import argparse
import json
import re
import sys
from statistics import median
from pathlib import Path

import bpy


def stride(rig, speed):
    """Estimate signed stance slip using both ToeBase bones and deformed shoe-sole mesh contact along forward (-Y)."""
    scene = bpy.context.scene
    action = next(
        act for act in bpy.data.actions
        if not act.name.startswith("Key")
        and any(act.name == n or act.name.endswith("|" + n) for n in ("Lan_StepOut", "GK_StalkWalk"))
    )
    if rig.animation_data is None:
        rig.animation_data_create()
    rig.animation_data.action = action
    if hasattr(rig.animation_data, "action_slot") and getattr(action, "slots", None):
        rig.animation_data.action_slot = action.slots[0]
    toes = [rig.pose.bones[f"mixamorig:{side}ToeBase"] for side in ("Left", "Right")]
    shoe_obj = bpy.data.objects.get("Lan_Uniform_LOD0") or bpy.data.objects.get("GK_Coat_LOD0")
    # Identify left/right forefoot shoe-sole contact vertex indices in bind pose (z <= 0.005m, forefoot y in [-0.14, -0.03])
    sole_indices = ([], [])
    if shoe_obj is not None:
        for v in shoe_obj.data.vertices:
            if v.co.z <= 0.005 and -0.14 <= v.co.y <= -0.03:
                (sole_indices[0] if v.co.x >= 0.0 else sole_indices[1]).append(v.index)

    fps = scene.render.fps / scene.render.fps_base
    previous = None
    prev_soles = None
    velocities = []
    sole_velocities = []
    depsgraph = bpy.context.evaluated_depsgraph_get()
    for frame in range(int(action.frame_range[0]), int(action.frame_range[1]) + 1):
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        points = [rig.matrix_world @ toe.head for toe in toes]
        if previous is not None:
            for i in range(2):
                if points[i].z > min(p.z for p in points) + 0.005 or previous[i].z > min(p.z for p in previous) + 0.005:
                    continue
                if abs(points[i].z - previous[i].z) * fps <= 0.08:
                    velocities.append((points[i].y - previous[i].y) * fps)
        previous = points

        if shoe_obj is not None and sole_indices[0] and sole_indices[1]:
            eval_obj = shoe_obj.evaluated_get(depsgraph)
            eval_mesh = eval_obj.to_mesh()
            mw = shoe_obj.matrix_world
            cur_soles = []
            for side_idx in range(2):
                pts = [mw @ eval_mesh.vertices[vi].co for vi in sole_indices[side_idx]]
                min_z = min(p.z for p in pts)
                mean_y = sum(p.y for p in pts) / len(pts)
                cur_soles.append((mean_y, min_z))
            eval_obj.to_mesh_clear()
            if prev_soles is not None:
                min_cur_z = min(s[1] for s in cur_soles)
                min_prev_z = min(s[1] for s in prev_soles)
                for i in range(2):
                    if cur_soles[i][1] > min_cur_z + 0.009 or prev_soles[i][1] > min_prev_z + 0.009:
                        continue
                    if abs(cur_soles[i][1] - prev_soles[i][1]) * fps <= 0.18:
                        sole_velocities.append((cur_soles[i][0] - prev_soles[i][0]) * fps)
            prev_soles = cur_soles

    if not velocities:
        raise ValueError(f"No stance samples in {action.name}")
    match = median(velocities)
    out = {
        "action": action.name,
        "assumed_world_speed_mps": speed,
        "samples": len(velocities),
        "stance_forward_compensation_median_mps": round(match, 4),
        "longitudinal_slip_median_mps": round(median(abs(v - speed) for v in velocities), 4),
        "longitudinal_slip_p95_mps": round(sorted(abs(v - speed) for v in velocities)[round((len(velocities) - 1) * 0.95)], 4),
    }
    if sole_velocities:
        out["shoe_sole_samples"] = len(sole_velocities)
        out["shoe_sole_forward_compensation_median_mps"] = round(median(sole_velocities), 4)
        out["shoe_sole_slip_median_mps"] = round(median(abs(v - speed) for v in sole_velocities), 4)
        out["shoe_sole_slip_p95_mps"] = round(sorted(abs(v - speed) for v in sole_velocities)[round((len(sole_velocities) - 1) * 0.95)], 4)
    return out


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
                  "An": ["Clothes", "Teeth", "Tongue"],
                  "GK": ["Coat", "KeyRing", "Teeth", "Tongue"]}[prefix]
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
