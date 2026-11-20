"""Stage Unity's archive and morgue props under interior lighting in Blender."""

import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import common as C

PRESENTATION = C.ROOT / "output" / "m31" / "blender" / "interiors"


def prop(source, place, heading=0):
    path = C.BLEND_DIR / f"{source}.blend"
    with bpy.data.libraries.load(str(path)) as (incoming, imported):
        imported.objects = [name for name in incoming.objects if name == source or name == source + "_LOD0"]
    obj, = imported.objects
    bpy.context.collection.objects.link(obj)
    obj.location += Vector(place)
    obj.rotation_euler.z += math.radians(heading)
    return obj


def room(width, depth, clear):
    C.PREVIEW_PALETTE["FloorR"] = ((.27, .29, .25), 0, .68, "FloorR")
    C.make_box("Terrazzo tile deck", (0, 0, -.12), (width, depth, .24), "FloorR")
    C.make_box("Ceiling slab", (0, 0, clear + .11), (width, depth, .22), "PlasterR")
    for side in (-1, 1):
        C.make_box("Paint dado", (side * width * .5, 0, .575), (.18, depth, 1.15), "DadoR")
        C.make_box("Lime plaster", (side * width * .5, 0, (clear + 1.15) * .5),
                   (.18, depth, clear - 1.15), "PlasterR")
        C.make_box("Cove base", (side * (width * .5 - .079), 0, .06), (.065, depth, .12), "BaseTrim",
                   bevel=.004)
    C.make_box("Far dado", (0, depth * .5, .575), (width, .18, 1.15), "DadoR")
    C.make_box("Far plaster", (0, depth * .5, (clear + 1.15) * .5), (width, .18, clear - 1.15), "PlasterR")


def area(name, position, target, watts, color, size):
    light = bpy.data.lights.new(name, "AREA")
    light.energy, light.color, light.shape, light.size = watts, color, "DISK", size
    obj = bpy.data.objects.new(name, light)
    bpy.context.collection.objects.link(obj)
    obj.location = position
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def archive():
    room(7, 9, 3.3)
    prop("CTCC_ENV_ArchiveShelfRack", (-2.86, -.80, 0), 90)
    prop("CTCC_ENV_ArchiveShelfRack", (-2.86, 1.40, 0), 90)
    prop("CTCC_PROP_KnittingBagC02", (-2.67, -.85, 1.10), 72)
    prop("CTCC_PROP_UnnamedBoxC12", (-2.67, 1.45, 1.10), 85)
    C.make_box("Reading desk", (-1.65, .16, .88), (.63, .90, .085), "WoodDoor", bevel=.019)
    for x in (-1.89, -1.41):
        for y in (-.15, .47):
            C.make_box("Reading desk leg", (x, y, .422), (.056, .056, .844), "WoodDoor", bevel=.006)
    prop("CTCC_PROP_PatientRegisterE03", (-1.62, .16, .934), -14)
    prop("CTCC_PROP_ArchiveChair", (-1.70, 1.10, 0), 128)
    area("High window spill", (-.75, 2.8, 2.98), (-2.7, -.5, 1), 185, (.69, .82, .90), 2.1)
    area("Task light pool", (-.98, -.12, 1.92), (-1.72, .20, .9), 45, (1, .81, .52), .39)
    return (1.9, -2.43, 1.52), (-1.52, .62, 1.04), 31


def morgue():
    room(6, 6, 3.1)
    prop("CTCC_PROP_MorgueDryCabinet", (-2.37, 1.18, 0), 90)
    prop("CTCC_PROP_MedicineCart", (-.70, 2.18, 0), 12)
    # Set the working papers away from the door sweep.
    C.make_box("Writing ledge", (-2.38, -.25, .88), (.57, .88, .07), "WoodDoor", bevel=.009)
    C.make_box("Duplicate intake sheets", (-2.31, -.18, .925), (.24, .32, .016), "KraftPaper", bevel=.002)
    area("Running circuit bounce", (1.75, -.65, 2.88), (-2.4, 1.0, 1.05), 205, (.72, .88, .79), 1.7)
    area("Drawer task lamp", (-1.44, .61, 2.54), (-2.3, 1.15, 1.06), 71, (1, .83, .61), .7)
    return (1.85, -1.36, 1.54), (-1.88, 1.23, 1.04), 31


def render(name, stage):
    scene = C.fresh_scene()
    eye, target, focal_length = stage()
    camera = bpy.data.objects.new("Inspection view", bpy.data.cameras.new("Inspection view"))
    bpy.context.collection.objects.link(camera)
    camera.location = eye
    camera.rotation_euler = (Vector(target) - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.data.lens = focal_length
    scene.camera = camera
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 40
    scene.cycles.use_denoising = True
    scene.render.resolution_x = 1080
    scene.render.resolution_y = 720
    scene.render.image_settings.file_format = "PNG"
    scene.view_settings.view_transform = "AgX"
    PRESENTATION.mkdir(parents=True, exist_ok=True)
    scene.render.filepath = str(PRESENTATION / f"{name}.png")
    bpy.ops.render.render(write_still=True)
    # Keep the editable lighting study with portable project-relative texture paths.
    bpy.ops.wm.save_as_mainfile(filepath=str(PRESENTATION / f"{name}.blend"), relative_remap=True, compress=True)


if __name__ == "__main__":
    render("LOC07_archive", archive)
    render("LOC10_morgue", morgue)
