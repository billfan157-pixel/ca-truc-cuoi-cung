"""LOC03 Ward 101 hero-asset production library (Blender 4.5 LTS).

Extends LOC02's battle-tested Cycles PBR baker (`loc02/hero_lib.py`) with:
  1. Real Blender 4.5 Cloth Physics Simulation (`simulate_cloth_drape`) for blankets,
     draped towels, privacy curtains, and folded cloth traces — producing authentic physical
     folds, gravity drape, pin bunching, and soft creases instead of rigid sine waves!
  2. Calibrated 1987-2005 Vietnamese hospital ward procedural PBR shaders (B 2005 vs R 2012)
     tuned in Linear sRGB for Unity URP ACES Filmic.
  3. Direct output into ArtSource/Blender/LOC03/, Prototype/Assets/CTCC/Art/Meshes/,
     Prototype/Assets/CTCC/Art/Textures/LOC03_*.png, and output/loc03-hero/blender-renders/.
"""
from __future__ import annotations

import math
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))
sys.path.insert(0, str(HERE.parent / "loc02"))

import common as C  # noqa: E402
import hero_lib as H  # noqa: E402

# Redirect hero_lib output paths to LOC03 (textures & FBX still go to shared Unity Art directories!)
ROOT = C.ROOT
SRC_DIR = ROOT / "ArtSource" / "Blender" / "LOC03"
RENDER_DIR = ROOT / "output" / "loc03-hero" / "blender-renders"
PRINT_DIR = ROOT / "ArtSource" / "LOC03" / "prints"
SRC_DIR.mkdir(parents=True, exist_ok=True)
RENDER_DIR.mkdir(parents=True, exist_ok=True)
PRINT_DIR.mkdir(parents=True, exist_ok=True)

H.SRC_DIR = SRC_DIR
H.RENDER_DIR = RENDER_DIR
H.PRINT_DIR = PRINT_DIR


# --------------------------------------------------------------------------------------
# Real Blender Cloth Physics Simulation Helper (Blankets, Curtains, Towels, Folded Cloth)
# --------------------------------------------------------------------------------------

def create_cloth_grid(
    name: str,
    nx: int,
    ny: int,
    pos_fn,
    kind: str,
    pin_fn=None,
    uv_scale=(1.0, 1.0),
):
    """Create a clean quad grid mesh ready for Blender CLOTH physics simulation."""
    verts, faces = [], []
    pin_indices = []
    for j in range(ny + 1):
        for i in range(nx + 1):
            u, v = i / float(nx), j / float(ny)
            verts.append(pos_fn(u, v))
            if pin_fn is not None and pin_fn(u, v):
                pin_indices.append(j * (nx + 1) + i)
            if i and j:
                k = j * (nx + 1) + i
                faces.append((k - nx - 2, k - nx - 1, k, k - 1))
    mesh = bpy.data.meshes.new(name + "_mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    uv = mesh.uv_layers.new(name="UVMap")
    puv = mesh.uv_layers.new(name="PRINT")
    su, sv = uv_scale
    for poly in mesh.polygons:
        poly.use_smooth = True
        for li in poly.loop_indices:
            vi = mesh.loops[li].vertex_index
            j, i = divmod(vi, nx + 1)
            uu, vv = i / float(nx), j / float(ny)
            uv.data[li].uv = (uu * su, vv * sv)
            puv.data[li].uv = (uu, vv)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    if pin_indices:
        vg = obj.vertex_groups.new(name="PIN")
        vg.add(pin_indices, 1.0, "REPLACE")
    H.set_kind(obj, kind)
    return obj


def simulate_cloth_drape(
    cloth_obj,
    collider_objs,
    frames: int = 24,
    mass: float = 0.42,
    tension: float = 18.0,
    bending: float = 0.55,
    air_damping: float = 1.4,
    collision_dist: float = 0.008,
    self_collide: bool = False,
    solidify_thickness: float = 0.007,
    subsurf_after: int = 0,
    wind_strength: float = 0.0,
    wind_dir=(0.0, 1.0, 0.0),
    cleanup_colliders: bool = False,
):
    """Run real Blender 4.5 CLOTH physics simulation over `collider_objs` and bake the settled
    frame into a permanent clean mesh with optional solidify & smooth normals."""
    scene = bpy.context.scene
    scene.frame_start = 1
    scene.frame_end = frames
    scene.frame_set(1)

    added_col_mods = []
    for col in collider_objs:
        if col is None:
            continue
        mod = col.modifiers.new("TempClothCol", "COLLISION")
        col.collision.thickness_outer = collision_dist
        col.collision.thickness_inner = 0.015
        col.collision.cloth_friction = 36.0
        added_col_mods.append((col, mod))

    wind_obj = None
    if abs(wind_strength) > 1e-4:
        bpy.ops.object.effector_add(type="WIND", location=(0, 0, 0))
        wind_obj = bpy.context.object
        wind_obj.field.strength = wind_strength
        wind_obj.field.noise = 1.8
        wind_obj.rotation_euler = Vector(wind_dir).normalized().to_track_quat("Z", "Y").to_euler()

    cmod = cloth_obj.modifiers.new("ClothSim", "CLOTH")
    cs = cmod.settings
    cs.quality = 6
    cs.mass = mass
    cs.tension_stiffness = tension
    cs.compression_stiffness = tension * 0.8
    cs.shear_stiffness = tension * 0.65
    cs.bending_stiffness = bending
    cs.air_damping = air_damping
    if "PIN" in cloth_obj.vertex_groups:
        cs.vertex_group_mass = "PIN"
        cs.pin_stiffness = 1.5
    cc = cmod.collision_settings
    cc.use_collision = True
    cc.distance_min = collision_dist
    cc.friction = 14.0
    cc.use_self_collision = self_collide
    if self_collide:
        cc.self_distance_min = max(0.006, collision_dist * 0.85)
        cc.self_friction = 10.0

    # Step frames cleanly so Blender computes the full cloth solve in background mode
    for f in range(1, frames + 1):
        scene.frame_set(f)

    # Convert evaluated cloth mesh at final frame into static geometry!
    depsgraph = bpy.context.evaluated_depsgraph_get()
    eval_obj = cloth_obj.evaluated_get(depsgraph)
    new_mesh = bpy.data.meshes.new_from_object(eval_obj)
    old_mesh = cloth_obj.data
    cloth_obj.modifiers.remove(cmod)
    cloth_obj.data = new_mesh
    if old_mesh.users == 0:
        bpy.data.meshes.remove(old_mesh)

    # Restore kind/color attributes lost on mesh swap
    kind = cloth_obj.get("kind", "cotton_sheet")
    H.set_kind(cloth_obj, kind)

    if subsurf_after > 0:
        sub = cloth_obj.modifiers.new("Subsurf", "SUBSURF")
        sub.levels = subsurf_after
        sub.render_levels = subsurf_after
        bpy.ops.object.select_all(action="DESELECT")
        cloth_obj.select_set(True)
        bpy.context.view_layer.objects.active = cloth_obj
        bpy.ops.object.modifier_apply(modifier=sub.name)

    if solidify_thickness > 0.0:
        sol = cloth_obj.modifiers.new("Solidify", "SOLIDIFY")
        sol.thickness = solidify_thickness
        sol.offset = 1.0
        sol.use_rim = True
        bpy.ops.object.select_all(action="DESELECT")
        cloth_obj.select_set(True)
        bpy.context.view_layer.objects.active = cloth_obj
        bpy.ops.object.modifier_apply(modifier=sol.name)

    C.set_smooth_by_angle(cloth_obj, 62.0)

    # Clean up temporary collision modifiers / dummy colliders / wind
    for col, mod in added_col_mods:
        try:
            col.modifiers.remove(mod)
        except Exception:
            pass
    if wind_obj is not None:
        bpy.data.objects.remove(wind_obj, do_unlink=True)
    if cleanup_colliders:
        for col in collider_objs:
            if col is not None:
                try:
                    bpy.data.objects.remove(col, do_unlink=True)
                except Exception:
                    pass
    scene.frame_set(1)
    return cloth_obj


# --------------------------------------------------------------------------------------
# Vietnamese 1987-2005 Hospital Ward Procedural PBR Materials (Linear sRGB Calibrated)
# --------------------------------------------------------------------------------------

def _k_ward_bed_enamel(b, A, P, gv, rnd, edge, cav):
    """1987 Vietnamese hospital sage-mint enameled tubular steel bed frame (reads clearly against dark dado!)."""
    return H._k_paint(
        b, A, P, gv, rnd, edge, cav,
        col_b=H.srgb(0.41, 0.56, 0.49),
        col_r=H.srgb(0.28, 0.38, 0.34),
        rough_b=0.30, rough_r=0.56,
        primer=H.srgb(0.48, 0.49, 0.46),
        rust=H.srgb(0.30, 0.16, 0.09),
        metal_sheet=True,
    )


def _k_cab_cream_mint(b, A, P, gv, rnd, edge, cav):
    """1987 Vietnamese pale mint-cream enameled hospital steel cabinet (clean, realistic subtle corner wear, NO red splats!)."""
    return H._k_paint(
        b, A, P, gv, rnd, edge, cav,
        col_b=H.srgb(0.66, 0.73, 0.67),
        col_r=H.srgb(0.45, 0.50, 0.46),
        rough_b=0.30, rough_r=0.56,
        primer=H.srgb(0.50, 0.52, 0.48),
        rust=H.srgb(0.28, 0.16, 0.10),
        metal_sheet=True,
    )


def _k_window_green(b, A, P, gv, rnd, edge, cav):
    """1987 oil-painted rolled-steel casement & diamond hoa sắt window grille (never crushes black!)."""
    return H._k_paint(
        b, A, P, gv, rnd, edge, cav,
        col_b=H.srgb(0.22, 0.34, 0.28),
        col_r=H.srgb(0.16, 0.23, 0.19),
        rough_b=0.38, rough_r=0.64,
        primer=H.srgb(0.36, 0.38, 0.35),
        rust=H.srgb(0.32, 0.17, 0.09),
        metal_sheet=True,
    )


def _k_satin_inox(b, A, P, gv, rnd, edge, cav):
    """Bright brushed hospital stainless steel (gleams cleanly under indoor URP reflection probes!)."""
    base, rough, _, h0, _ = H._k_steel(b, A, P, gv, rnd, edge, cav, tone=H.srgb(0.84, 0.86, 0.85))
    metal = b.mixf(0.62, 0.50, A)
    rough = b.mixf(0.26, 0.44, A)
    return base, rough, metal, h0, 0.10


def _k_galv_conduit(b, A, P, gv, rnd, edge, cav):
    """Galvanized electrical steel conduit pipe & junction box."""
    base, rough, _, h0, _ = H._k_steel(b, A, P, gv, rnd, edge, cav, tone=H.srgb(0.62, 0.65, 0.63))
    return base, b.mixf(0.42, 0.64, A), b.mixf(0.45, 0.30, A), h0, 0.18


def _k_cotton_sheet(b, A, P, gv, rnd, edge, cav):
    """Starched off-white hospital cotton sheet & pillowcase with subtle weave & R yellowing/damp stains."""
    w1 = b.wave(P, 260.0, "BANDS", "X", profile="SIN")
    w2 = b.wave(P, 260.0, "BANDS", "Y", profile="SIN")
    weave = b.math("ADD", w1, w2)
    base = b.mix(H.srgb(0.84, 0.84, 0.79), H.srgb(0.62, 0.60, 0.52), A)
    base = b.mix(base, b.vmath("SCALE", base, 0.88, out="Vector"), b.math("MULTIPLY", b.mapr(weave, 0.5, 1.5), 0.25))
    stain = b.mapr(b.noise(P, 2.6, 4, 0.62, 0.4), 0.54, 0.74)
    base = b.mix(base, H.srgb(0.42, 0.39, 0.31), b.math("MULTIPLY", stain, b.math("MULTIPLY", A, 0.62)))
    base = b.mix(base, H.srgb(0.20, 0.21, 0.19), b.math("MULTIPLY", cav, 0.26))
    return base, b.mixf(0.86, 0.94, A), 0.0, b.math("MULTIPLY", weave, 0.28), 0.18


def _k_blanket_hospital(b, A, P, gv, rnd, edge, cav, variant="jade"):
    """Authentic Vietnamese hospital woven cotton blanket with institutional border stripes."""
    w1 = b.wave(P, 210.0, "BANDS", "X", profile="SIN")
    w2 = b.wave(P, 210.0, "BANDS", "Y", profile="SIN")
    weave = b.math("ADD", w1, w2)
    # Subtle lengthwise stripe woven into blanket fabric using PRINT UV or Object Y
    y = b.sep(P)[1]
    stripe = b.mapr(b.wave(b.comb(y, y, y), 14.0, "BANDS", "X", distortion=0.25), 0.76, 0.86)
    if variant == "jade":
        col_b = b.mix(H.srgb(0.25, 0.46, 0.37), H.srgb(0.46, 0.62, 0.54), b.math("MULTIPLY", stripe, 0.52))
        col_r = b.mix(H.srgb(0.20, 0.31, 0.26), H.srgb(0.32, 0.40, 0.36), b.math("MULTIPLY", stripe, 0.45))
    elif variant == "blue":
        col_b = b.mix(H.srgb(0.26, 0.40, 0.50), H.srgb(0.56, 0.65, 0.69), b.math("MULTIPLY", stripe, 0.55))
        col_r = b.mix(H.srgb(0.21, 0.28, 0.34), H.srgb(0.37, 0.42, 0.44), b.math("MULTIPLY", stripe, 0.45))
    else:  # pediatric warm plaid/cream-sage for Bed 4 trace
        col_b = b.mix(H.srgb(0.38, 0.52, 0.45), H.srgb(0.68, 0.72, 0.64), b.math("MULTIPLY", stripe, 0.58))
        col_r = b.mix(H.srgb(0.28, 0.36, 0.32), H.srgb(0.46, 0.48, 0.42), b.math("MULTIPLY", stripe, 0.48))

    base = b.mix(col_b, col_r, A)
    base = b.mix(base, b.vmath("SCALE", base, 0.80, out="Vector"), b.math("MULTIPLY", b.mapr(weave, 0.4, 1.6), 0.34))
    dust = b.mapr(b.noise(P, 3.2, 4, 0.62), 0.50, 0.74)
    base = b.mix(base, H.srgb(0.34, 0.35, 0.31), b.math("MULTIPLY", dust, b.math("MULTIPLY", A, 0.45)))
    base = b.mix(base, H.srgb(0.08, 0.10, 0.09), b.math("MULTIPLY", cav, 0.36))
    return base, b.mixf(0.88, 0.95, A), 0.0, b.math("MULTIPLY", weave, 0.38), 0.24


def _k_mattress_ticking(b, A, P, gv, rnd, edge, cav):
    """Hospital blue-and-ivory striped vinyl/cotton mattress ticking."""
    x = b.sep(P)[0]
    stripe = b.mapr(b.wave(b.comb(x, x, x), 36.0, "BANDS", "X"), 0.55, 0.72)
    cb = b.mix(H.srgb(0.74, 0.76, 0.71), H.srgb(0.48, 0.58, 0.62), b.math("MULTIPLY", stripe, 0.45))
    cr = b.mix(H.srgb(0.52, 0.51, 0.44), H.srgb(0.36, 0.40, 0.41), b.math("MULTIPLY", stripe, 0.42))
    base = b.mix(cb, cr, A)
    stain = b.mapr(b.noise(P, 2.2, 4, 0.6), 0.56, 0.74)
    base = b.mix(base, H.srgb(0.32, 0.29, 0.21), b.math("MULTIPLY", stain, b.math("MULTIPLY", A, 0.60)))
    base = b.mix(base, H.srgb(0.12, 0.12, 0.10), b.math("MULTIPLY", cav, 0.32))
    return base, b.mixf(0.52, 0.74, A), 0.0, b.math("MULTIPLY", b.noise(P, 220.0, 2), 0.25), 0.18


def _k_curtain_ward(b, A, P, gv, rnd, edge, cav):
    """Institutional hospital privacy curtain with upper ventilation mesh band (Z > -0.40m) + lower pleated fabric."""
    z = b.sep(P)[2]
    # Curtain drops from Z=0 down to Z=-1.92m -> upper mesh band spans Z in [-0.36..-0.05]
    in_mesh = b.math("MULTIPLY", b.mapr(z, -0.37, -0.34), b.mapr(z, -0.04, -0.07))
    # Open-weave mesh grid pattern in upper header band
    mx = b.mapr(b.wave(P, 95.0, "BANDS", "X", profile="SIN"), 0.35, 0.65)
    mz = b.mapr(b.wave(P, 95.0, "BANDS", "Z", profile="SIN"), 0.35, 0.65)
    mesh_hole = b.math("MULTIPLY", mx, mz)
    # Fine cotton weave on lower curtain panel
    w1 = b.wave(P, 190.0, "BANDS", "X", profile="SIN")
    w2 = b.wave(P, 190.0, "BANDS", "Z", profile="SIN")
    weave = b.math("ADD", w1, w2)

    col_b = H.srgb(0.54, 0.68, 0.60)
    col_r = H.srgb(0.32, 0.40, 0.35)
    base = b.mix(col_b, col_r, A)
    base = b.mix(base, b.vmath("SCALE", base, 0.84, out="Vector"), b.math("MULTIPLY", b.mapr(weave, 0.5, 1.5), 0.28))

    # Mesh header is slightly lighter cream-sage weave with dark mesh openings
    mesh_col = b.mix(H.srgb(0.70, 0.76, 0.70), H.srgb(0.45, 0.49, 0.44), A)
    mesh_col = b.mix(mesh_col, H.srgb(0.14, 0.18, 0.16), b.math("MULTIPLY", mesh_hole, 0.78))
    base = b.mix(base, mesh_col, in_mesh)

    # Bottom hem damp & dust accumulation in R
    hem_grime = b.math("MULTIPLY", b.mapr(z, -1.35, -1.90), b.math("ADD", 0.14, b.math("MULTIPLY", A, 0.62)))
    base = b.mix(base, H.srgb(0.16, 0.19, 0.16), b.math("MULTIPLY", hem_grime, 0.65))
    base = b.mix(base, H.srgb(0.10, 0.13, 0.11), b.math("MULTIPLY", cav, 0.32))
    height = b.mixf(b.math("MULTIPLY", weave, 0.25), b.math("SUBTRACT", 0.5, mesh_hole), in_mesh)
    return base, b.mixf(0.84, 0.94, A), 0.0, height, 0.24


def _k_ceramic_tile_white(b, A, P, gv, rnd, edge, cav):
    """15x15cm glazed white Vietnamese hospital ceramic wall splashback tiles with cement grout & R mildew.
    Splashback sits in X-Z plane (back at Y=0), so tile grout grid runs along X and Z!"""
    gx = b.mapr(b.wave(P, 20.94, "BANDS", "X", profile="SAW"), 0.0, 0.065, 1.0, 0.0)
    gz = b.mapr(b.wave(P, 20.94, "BANDS", "Z", profile="SAW"), 0.0, 0.065, 1.0, 0.0)
    grout = b.math("MAXIMUM", gx, gz, clamp=True)
    tile_b = H.srgb(0.86, 0.87, 0.83)
    tile_r = H.srgb(0.66, 0.66, 0.59)
    tile = b.mix(tile_b, tile_r, A)
    # Subtle vertical water stain streaks in R
    streak = b.mapr(b.noise(b.vmath("MULTIPLY", P, (12.0, 4.0, 2.2)), 1.5, 4, 0.6), 0.50, 0.72)
    tile = b.mix(tile, H.srgb(0.40, 0.36, 0.25), b.math("MULTIPLY", streak, b.math("MULTIPLY", A, 0.62)))
    grout_col = b.mix(H.srgb(0.54, 0.56, 0.52), H.srgb(0.24, 0.25, 0.20), A)
    base = b.mix(tile, grout_col, grout)
    rough = b.mixf(b.mixf(0.16, 0.38, A), 0.88, grout)
    height = b.math("SUBTRACT", 0.5, b.math("MULTIPLY", grout, 0.85))
    return base, rough, 0.0, height, 0.36


def _k_mirror_aged(b, A, P, gv, rnd, edge, cav):
    """Hospital wall mirror: softly luminous satin-silver glass in B (reads cleanly under indoor probes!),
    foxed & mottled desilvered edges in R."""
    edge_fox = b.math("MULTIPLY", b.mapr(b.noise(P, 8.5, 4, 0.65), 0.42, 0.66), A)
    subtle_mist = b.mapr(b.noise(P, 3.5, 3, 0.55), 0.35, 0.75)
    col_b = b.mix(H.srgb(0.58, 0.65, 0.63), H.srgb(0.72, 0.76, 0.74), b.math("MULTIPLY", subtle_mist, 0.35))
    col_r = b.mix(H.srgb(0.44, 0.49, 0.48), H.srgb(0.22, 0.25, 0.24), b.math("MULTIPLY", edge_fox, 0.78))
    base = b.mix(col_b, col_r, A)
    rough = b.mixf(0.20, 0.42, b.math("ADD", b.math("MULTIPLY", A, 0.45), b.math("MULTIPLY", edge_fox, 0.50), clamp=True))
    metal = b.mixf(0.56, 0.34, b.math("ADD", b.math("MULTIPLY", A, 0.35), b.math("MULTIPLY", edge_fox, 0.45), clamp=True))
    return base, rough, metal, b.math("MULTIPLY", edge_fox, 0.28), 0.10


def _k_sedge_mat(b, A, P, gv, rnd, edge, cav):
    """Vietnamese woven sedge sleeping mat (chiếu cói) with tight reed ribs."""
    rib = b.wave(P, 180.0, "BANDS", "X", distortion=1.2, detail=2.0)
    cross = b.wave(P, 55.0, "BANDS", "Y", distortion=0.6)
    base = b.mix(H.srgb(0.74, 0.64, 0.41), H.srgb(0.50, 0.43, 0.28), A)
    base = b.mix(base, b.vmath("SCALE", base, 0.74, out="Vector"), b.math("MULTIPLY", rib, 0.42))
    base = b.mix(base, H.srgb(0.14, 0.12, 0.08), b.math("MULTIPLY", cav, 0.38))
    return base, b.mixf(0.76, 0.88, A), 0.0, b.math("ADD", b.math("MULTIPLY", rib, 0.65), b.math("MULTIPLY", cross, 0.25)), 0.38


def _k_slipper_honeycomb(b, A, P, gv, rnd, edge, cav):
    """Iconic Vietnamese ivory-yellow EVA honeycomb rubber slippers (dép tổ ong)."""
    base = b.mix(H.srgb(0.84, 0.78, 0.55), H.srgb(0.64, 0.57, 0.40), A)
    dirt = b.mapr(b.noise(P, 18.0, 4, 0.6), 0.48, 0.72)
    base = b.mix(base, H.srgb(0.32, 0.28, 0.21), b.math("MULTIPLY", dirt, b.math("ADD", 0.14, b.math("MULTIPLY", A, 0.48))))
    base = b.mix(base, H.srgb(0.16, 0.14, 0.11), b.math("MULTIPLY", cav, 0.45))
    return base, b.mixf(0.68, 0.84, A), 0.0, b.math("MULTIPLY", b.noise(P, 260.0, 2), 0.22), 0.18


def _k_amber_glass(b, A, P, gv, rnd, edge, cav):
    """Dark amber apothecary antiseptic glass bottle."""
    base = b.mix(H.srgb(0.34, 0.14, 0.045), H.srgb(0.26, 0.12, 0.045), A)
    return base, b.mixf(0.12, 0.34, A), 0.05, b.math("MULTIPLY", b.noise(P, 140.0, 1), 0.06), 0.06


def _k_towel_striped(b, A, P, gv, rnd, edge, cav):
    """Cotton terry face towel hanging from bedside cabinet / sink rail."""
    w1 = b.wave(P, 280.0, "BANDS", "X")
    w2 = b.wave(P, 280.0, "BANDS", "Z")
    terry = b.math("ADD", w1, w2)
    base = b.mix(H.srgb(0.80, 0.82, 0.76), H.srgb(0.56, 0.56, 0.48), A)
    base = b.mix(base, b.vmath("SCALE", base, 0.84, out="Vector"), b.math("MULTIPLY", b.mapr(terry, 0.5, 1.5), 0.30))
    base = b.mix(base, H.srgb(0.18, 0.19, 0.16), b.math("MULTIPLY", cav, 0.32))
    return base, 0.92, 0.0, b.math("MULTIPLY", terry, 0.42), 0.28


def _k_concrete_beam(b, A, P, gv, rnd, edge, cav):
    """Limewashed 1987 structural reinforced concrete ceiling beam & pilaster (matches PlasterB/PlasterR!)."""
    return H._k_paint(
        b, A, P, gv, rnd, edge, cav,
        col_b=H.srgb(0.68, 0.70, 0.62),
        col_r=H.srgb(0.45, 0.48, 0.43),
        rough_b=0.76, rough_r=0.90,
        primer=H.srgb(0.46, 0.45, 0.40),
        metal_sheet=False,
    )


def _k_ward_dado_pilaster(b, A, P, gv, rnd, edge, cav):
    """Bottle-green oil-painted dado lower section on Room 101 corner pilasters (matches DadoB/DadoR!)."""
    return H._k_paint(
        b, A, P, gv, rnd, edge, cav,
        col_b=H.srgb(0.145, 0.310, 0.240),
        col_r=H.srgb(0.095, 0.195, 0.158),
        rough_b=0.34, rough_r=0.58,
        primer=H.srgb(0.36, 0.34, 0.28),
        metal_sheet=False,
    )


def _k_copper_rust(b, A, P, gv, rnd, edge, cav):
    """Aged galvanized/iron sink drain trap pipe with oxidation & rust in R."""
    base = b.mix(H.srgb(0.60, 0.62, 0.60), H.srgb(0.34, 0.22, 0.14), b.math("MULTIPLY", A, 0.85))
    patina = b.math("MULTIPLY", b.mapr(b.noise(P, 26.0, 4), 0.42, 0.66), A)
    base = b.mix(base, H.srgb(0.28, 0.13, 0.06), b.math("MULTIPLY", patina, 0.78))
    rough = b.mixf(0.36, 0.78, A)
    metal = b.mixf(0.68, 0.24, A)
    return base, rough, metal, b.math("MULTIPLY", b.noise(P, 180.0, 2), 0.30), 0.20


def register_ward_kinds():
    H.KINDS.update({
        "bed_enamel_green": _k_ward_bed_enamel,
        "cab_cream_mint":   _k_cab_cream_mint,
        "window_green":     _k_window_green,
        "satin_inox":       _k_satin_inox,
        "galv_conduit":     _k_galv_conduit,
        "cotton_sheet":     _k_cotton_sheet,
        "blanket_jade":     lambda b, A, *a: _k_blanket_hospital(b, A, *a, variant="jade"),
        "blanket_blue":     lambda b, A, *a: _k_blanket_hospital(b, A, *a, variant="blue"),
        "blanket_bed4":     lambda b, A, *a: _k_blanket_hospital(b, A, *a, variant="bed4"),
        "mattress_ticking": _k_mattress_ticking,
        "curtain_ward":     _k_curtain_ward,
        "ceramic_tile":     _k_ceramic_tile_white,
        "mirror_aged":      _k_mirror_aged,
        "sedge_mat":        _k_sedge_mat,
        "slipper_honey":    _k_slipper_honeycomb,
        "amber_glass":      _k_amber_glass,
        "towel_terry":      _k_towel_striped,
        "concrete_beam":    _k_concrete_beam,
        "dado_pilaster":    _k_ward_dado_pilaster,
        "copper_rust":      _k_copper_rust,
    })


register_ward_kinds()


def get_mat_extended(kind: str):
    """Also passthrough SalineFluid, ObsCard, and F101 for direct Unity material mapping!"""
    if kind in ("SalineFluid", "ObsCard", "F101"):
        m = bpy.data.materials.get(kind)
        if m is None:
            if kind == "SalineFluid":
                return C.get_material("SalineFluid")
            m = bpy.data.materials.new(kind)
            m.use_nodes = True
            bsdf = m.node_tree.nodes.get("Principled BSDF")
            bsdf.inputs["Roughness"].default_value = 0.85
            tex_file = C.TEX_DIR / f"{kind}.png"
            if tex_file.exists():
                tn = m.node_tree.nodes.new("ShaderNodeTexImage")
                tn.image = bpy.data.images.load(str(tex_file), check_existing=True)
                m.node_tree.links.new(tn.outputs["Color"], bsdf.inputs["Base Color"])
        return m
    return H.get_mat(kind)


# Monkey-patch hero_lib.get_mat so H.set_kind / H.box / H.cyl / etc. understand SalineFluid, ObsCard, F101
_ORIG_GET_MAT = H.get_mat


def _patched_get_mat(kind: str):
    if kind in ("SalineFluid", "ObsCard", "F101"):
        return get_mat_extended(kind)
    return _ORIG_GET_MAT(kind)


H.get_mat = _patched_get_mat


def make_uv_plane(name: str, loc, size_xy, mat_key: str, rot=(0, 0, 0)):
    """Create a 0..1 planar UV-mapped quad mesh for passthrough Unity document materials (ObsCard / F101)."""
    w, h = size_xy
    bm = bmesh.new()
    v0 = bm.verts.new((-w * 0.5, -h * 0.5, 0.0))
    v1 = bm.verts.new((w * 0.5, -h * 0.5, 0.0))
    v2 = bm.verts.new((w * 0.5, h * 0.5, 0.0))
    v3 = bm.verts.new((-w * 0.5, h * 0.5, 0.0))
    face = bm.faces.new((v0, v1, v2, v3))
    uv_layer = bm.loops.layers.uv.new("UVMap")
    for loop, uv in zip(face.loops, ((0.0, 0.0), (1.0, 0.0), (1.0, 1.0), (0.0, 1.0))):
        loop[uv_layer].uv = uv
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new(name + "_mesh")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = loc
    obj.rotation_euler = rot
    obj.data.materials.append(get_mat_extended(mat_key))
    obj["kind"] = mat_key
    return obj


def export_ward_asset(
    asset_name: str,
    build_fn,
    atlas_specs: dict,
    make_lods: bool = True,
    separate_parts: bool = False,
    cam_dir=(1.45, -1.55, 0.85),
    render: bool = True,
    ao_samples: int = 12,
):
    H.SRC_DIR = SRC_DIR
    H.RENDER_DIR = RENDER_DIR
    H.PRINT_DIR = PRINT_DIR
    return H.export_hero_asset(
        asset_name,
        build_fn,
        atlas_specs,
        make_lods=make_lods,
        separate_parts=separate_parts,
        cam_dir=cam_dir,
        render=render,
        ao_samples=ao_samples,
        passthrough_kinds=("Glass", "FrostedGlass", "DeadTube", "LiveTube", "WarmGlow", "SalineFluid", "ObsCard", "F101"),
    )
