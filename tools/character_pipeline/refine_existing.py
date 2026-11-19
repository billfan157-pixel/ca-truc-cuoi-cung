"""Stage LOD and surface refinements from an editable production .blend."""

import argparse
import math
from pathlib import Path
import re
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import dh_core as dh


RATIOS = {"Eye_L": (0.55, 0.30), "Eye_R": (0.55, 0.30),
          "Teeth": (0.22, 0.09), "Tongue": (0.55, 0.32),
          "Hair": (0.65, 0.42), "HairClip": (0.60, 0.35)}


def character_meshes():
    return [o for o in bpy.context.scene.objects
            if o.type == "MESH" and re.search(r"_LOD[012]$", o.name)]


def normalize_skin(mesh, bone_names):
    """Cap and normalize deform influences to match Unity's four-weight importer."""
    groups = {g.index: g for g in mesh.vertex_groups if g.name in bone_names}
    for vertex in mesh.data.vertices:
        weights = sorted(((g.weight, g.group) for g in vertex.groups
                          if g.group in groups and g.weight > 0), reverse=True)
        if not weights:
            raise ValueError(f"{mesh.name} has an unweighted vertex at {vertex.index}")
        kept = weights[:4]
        total = sum(w for w, _ in kept)
        for _, index in weights[4:]:
            groups[index].remove([vertex.index])
        for weight, index in kept:
            groups[index].add([vertex.index], weight / total, "REPLACE")


def refine_lan_surface(rig):
    # A small one-sided change around the cheek and chin, applied consistently to the
    # neutral pose and every expression key. Eyes, eyelids and lips retain their fit.
    for lod in range(3):
        obj = bpy.data.objects[f"Lan_Body_LOD{lod}"]
        head = obj.matrix_world.inverted() @ rig.matrix_world @ rig.data.bones["mixamorig:Head"].head_local
        deltas = []
        for v in obj.data.vertices:
            x, y, z = v.co - head
            cheek = math.exp(-((x - 0.043) / 0.021)**2 - ((y + 0.059) / 0.027)**2
                             - ((z + 0.007) / 0.021)**2)
            chin = math.exp(-((x + 0.017) / 0.024)**2 - ((y + 0.073) / 0.024)**2
                            - ((z + 0.062) / 0.018)**2)
            deltas.append(Vector((0.0012 * cheek, -0.0024 * cheek - 0.0013 * chin, -0.0010 * chin)))
        if obj.data.shape_keys:
            for key in obj.data.shape_keys.key_blocks:
                for pt, delta in zip(key.data, deltas):
                    pt.co += delta
        for vertex, delta in zip(obj.data.vertices, deltas):
            vertex.co += delta
        obj.data.update()

    skin = bpy.data.materials["LanSkin"].node_tree
    bsdf = next(n for n in skin.nodes if n.type == "BSDF_PRINCIPLED")
    bsdf.inputs["Subsurface Weight"].default_value = 0.11
    bsdf.inputs["Specular IOR Level"].default_value = 0.32
    normal = bsdf.inputs["Normal"]
    prior_normal = normal.links[0].from_socket if normal.is_linked else None
    coord = skin.nodes.new("ShaderNodeTexCoord")
    pore = skin.nodes.new("ShaderNodeTexNoise")
    pore.label = "Skin pore relief"
    pore.inputs["Scale"].default_value = 1430
    pore.inputs["Detail"].default_value = 2
    bump = skin.nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.12
    bump.inputs["Distance"].default_value = 0.00022
    skin.links.new(coord.outputs["Object"], pore.inputs["Vector"])
    skin.links.new(pore.outputs["Fac"], bump.inputs["Height"])
    if prior_normal is not None:
        skin.links.new(prior_normal, bump.inputs["Normal"])
    skin.links.new(bump.outputs["Normal"], normal)


def fill_lods(rig, prefix):
    body = bpy.data.objects[f"{prefix}_Body_LOD0"]
    body_far = bpy.data.objects[f"{prefix}_Body_LOD2"]
    dh.triangulate_ngons(body_far)
    dh.transfer_shapekeys_by_surface(body, body_far, body_only=False)
    for part, ratios in RATIOS.items():
        high = bpy.data.objects.get(f"{prefix}_{part}_LOD0")
        if high is None:
            continue
        for lod, ratio in enumerate(ratios, 1):
            name = f"{prefix}_{part}_LOD{lod}"
            if bpy.data.objects.get(name) is None:
                dh.make_skinned_lod(high, name, ratio, rig)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("stage", type=Path)
    parser.add_argument("--export-only", action="store_true")
    parser.add_argument("--save-as", type=Path, help="Save a reviewed source copy with remapped paths")
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:])
    source, stage = args.source.resolve(strict=True), args.stage.resolve()
    blend_path = stage / source.name
    fbx_path = stage / source.with_suffix(".fbx").name
    if not args.export_only and blend_path.exists():
        raise FileExistsError(blend_path)
    if fbx_path.exists() or stage == dh.FBX_DIR.resolve():
        raise FileExistsError(fbx_path)
    if args.save_as and (not args.export_only or args.save_as.resolve().parent != dh.BLEND_DIR.resolve()):
        parser.error("--save-as expects --export-only and a path in ArtSource/Blender")

    bpy.ops.wm.open_mainfile(filepath=str(source), load_ui=False)
    scene = bpy.context.scene
    rig, = [o for o in scene.objects if o.type == "ARMATURE"]
    stage.mkdir(parents=True, exist_ok=True)
    if not args.export_only:
        prefix = rig.name.split("_", 1)[0]
        if prefix == "Lan":
            refine_lan_surface(rig)
        fill_lods(rig, prefix)
        hair = bpy.data.materials[{"Lan": "LanHairCard", "An": "AnHairCard", "GK": "GatekeeperHairCard"}[prefix]]
        bsdf = next(n for n in hair.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
        bsdf.inputs["Specular IOR Level"].default_value = 0.24
        bsdf.inputs["Roughness"].default_value = 0.52
        for mesh in character_meshes():
            normalize_skin(mesh, {bone.name for bone in rig.data.bones})
            dh.triangulate_ngons(mesh)
        bpy.ops.wm.save_as_mainfile(filepath=str(blend_path), relative_remap=True, copy=True)

    dh.export_character(fbx_path, rig, character_meshes())
    if args.save_as:
        bpy.context.preferences.filepaths.save_version = 0
        bpy.ops.wm.save_as_mainfile(filepath=str(args.save_as.resolve()), relative_remap=True, copy=True)


if __name__ == "__main__":
    main()
