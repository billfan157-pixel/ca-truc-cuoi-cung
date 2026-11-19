"""Bake an original 2048x2048 Multi-Clump Hair Card PBR Atlas in Blender 4.5 Cycles.

Generates 4 game-ready real-time hair card columns (Root at V=1, Tip at V=0) from thousands of
curved 3D hair fibers rendered orthographically in Blender Cycles:
- Col 0 (U 0.00..0.28): Dense Base Clump (scalp foundation & bun core, clean tapered tips)
- Col 1 (U 0.28..0.56): Medium Layered Clump (crown, swept temples, natural parted clusters)
- Col 2 (U 0.56..0.80): Fine Breakup Strands (surface volume breakup & bun wrap)
- Col 3 (U 0.80..1.00): Baby Hairs & Flyaway Wisps (delicate hairline & nape softness)

Outputs (Prototype/Assets/CTCC/Art/Textures/ and ArtSource/Characters/Textures/):
- HairCards_Color.png  (2048x2048 RGBA: dark Vietnamese black-brown fiber albedo + crisp alpha)
- HairCards_Normal.png (2048x2048 RGB: tangent-space cylindrical fiber normal map)
- HairCards_Flow.png   (2048x2048 RGBA: R=StrandID, G=RootToTip, B=Depth/AO, A=Alpha)
"""

import math
import random
import shutil
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
TEX_DIR = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Textures"
SRC_TEX_DIR = ROOT / "ArtSource" / "Characters" / "Textures"


def create_hair_fiber_bundle(rng, col_name, u_min, u_max, count, r_min, r_max, waviness,
                             clump_pull, tip_spread, length_range, z_spread, is_wisp=False):
    """Build curved 3D beveled hair strand meshes on X in [-1, 1], Y in [-1, 1] (top=+1 root, bot=-1 tip)."""
    # Map U in [0,1] -> X in [-1, 1]
    x_min = u_min * 2.0 - 1.0
    x_max = u_max * 2.0 - 1.0
    x_center = 0.5 * (x_min + x_max)
    x_half = 0.5 * (x_max - x_min) * (0.86 if not is_wisp else 0.78)

    # Create a few sub-clump attractors so strands naturally group like real human hair!
    n_subclumps = 4 if not is_wisp else 3
    sub_centers = [x_center + ((i + 0.5) / n_subclumps - 0.5) * 2.0 * x_half * 0.82 for i in range(n_subclumps)]

    # Build a single Curve data block with many splines for super-fast Blender build & Cycles render!
    curve = bpy.data.curves.new(col_name + "_crv", "CURVE")
    curve.dimensions = "3D"
    curve.bevel_depth = 1.0  # scaled per-point via pt.radius!
    curve.bevel_resolution = 2
    curve.resolution_u = 6
    curve.use_fill_caps = True

    steps = 12
    for s_idx in range(count):
        spline = curve.splines.new("POLY")
        spline.points.add(steps)

        # Pick root position (Gaussian-weighted slightly denser in column center)
        sub_c = sub_centers[s_idx % n_subclumps]
        base_u = rng.gauss(0.0, 0.42)
        base_u = max(-1.0, min(1.0, base_u))
        x0 = x_center + base_u * x_half
        # Pull gently toward nearest sub-clump
        x0 = x0 * (1.0 - clump_pull * 0.45) + sub_c * (clump_pull * 0.45)

        length = rng.uniform(length_range[0], length_range[1])
        # Feather root starting Y smoothly so hairline cards melt organically into the scalp!
        root_jitter = 0.08 if not is_wisp else 0.18
        y_top = 0.992 - (rng.random() ** 1.7) * root_jitter
        y_bot = max(-0.995, y_top - length * 1.92)

        radius = rng.uniform(r_min, r_max)
        freq = rng.uniform(1.1, 2.5)
        phase = rng.uniform(0.0, math.tau)
        drift = rng.uniform(-tip_spread, tip_spread) * x_half
        z_layer = rng.uniform(-z_spread, z_spread)
        # Depth ordering: slightly thicker/darker strands in back, finer highlight wisps in front
        if s_idx > count * 0.72:
            z_layer += z_spread * 0.45
            radius *= 0.75

        for k in range(steps + 1):
            t = k / float(steps)  # 0 = root (top, Y=+1), 1 = tip (bottom, Y=-1)
            y = y_top + (y_bot - y_top) * t
            # Organic S-wave + tip taper convergence
            wave = math.sin(t * math.pi * freq + phase) * waviness * x_half * (0.25 + 0.75 * math.sin(t * math.pi))
            # Clump pinch near tip so cards terminate in natural locks instead of rectangular blocks
            tip_taper = (1.0 - 0.58 * (t ** 1.6)) if not is_wisp else (1.0 - 0.25 * t)
            cx = sub_c + (x0 - sub_c) * tip_taper + drift * (t ** 1.4) + wave
            cx = max(x_min + 0.008, min(x_max - 0.008, cx))
            cz = z_layer + math.sin(t * math.pi) * 0.015

            # Taper radius smoothly at BOTH root (subtle) and tip (sharp natural taper)!
            root_fade = 0.35 + 0.65 * math.sin(min(1.0, t * 5.0) * math.pi * 0.5)
            r_scale = (1.0 - 0.88 * (t ** 2.1)) * root_fade
            if is_wisp:
                r_scale *= (0.35 + 0.65 * math.sin(min(1.0, t * 3.5) * math.pi * 0.5))
            pt = spline.points[k]
            pt.co = (cx, y, cz, 1.0)
            pt.radius = max(0.0004, radius * r_scale)

    obj = bpy.data.objects.new(col_name, curve)
    bpy.context.collection.objects.link(obj)
    return obj


def setup_ortho_scene(res=2048):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 16
    scene.cycles.use_denoising = False
    scene.cycles.max_bounces = 2
    scene.cycles.diffuse_bounces = 1
    scene.cycles.glossy_bounces = 0
    scene.cycles.transparent_max_bounces = 4
    scene.render.resolution_x = res
    scene.render.resolution_y = res
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_depth = "8"

    world = bpy.data.worlds.new("AtlasWorld")
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    bg.inputs["Color"].default_value = (0.0, 0.0, 0.0, 1.0)
    bg.inputs["Strength"].default_value = 0.0
    scene.world = world

    cam_data = bpy.data.cameras.new("OrthoCam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = 2.0
    cam_data.clip_start = 0.01
    cam_data.clip_end = 10.0
    cam_obj = bpy.data.objects.new("OrthoCam", cam_data)
    cam_obj.location = (0.0, 0.0, 2.0)
    cam_obj.rotation_euler = (0.0, 0.0, 0.0)
    bpy.context.collection.objects.link(cam_obj)
    scene.camera = cam_obj
    return scene


def make_albedo_material():
    """Emission-shaded natural Vietnamese black-brown hair with per-strand melanin variation & depth AO."""
    mat = bpy.data.materials.new("HairAtlas_Albedo")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()

    out = nodes.new("ShaderNodeOutputMaterial")
    em = nodes.new("ShaderNodeEmission")
    geo = nodes.new("ShaderNodeNewGeometry")
    sep = nodes.new("ShaderNodeSeparateXYZ")
    links.new(geo.outputs["Position"], sep.inputs["Vector"])

    # Per-strand random melanin tint using Object/Position noise stretched strongly along Y
    noise = nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 180.0
    noise.inputs["Detail"].default_value = 4.0
    mapping = nodes.new("ShaderNodeMapping")
    mapping.inputs["Scale"].default_value = (12.0, 0.12, 8.0)
    links.new(geo.outputs["Position"], mapping.inputs["Vector"])
    links.new(mapping.outputs["Vector"], noise.inputs["Vector"])

    # Root-to-tip gradient: Y=+1 (root, slightly darker) -> Y=-1 (tip, slightly warmer brown-black)
    mr_y = nodes.new("ShaderNodeMapRange")
    mr_y.inputs["From Min"].default_value = 1.0
    mr_y.inputs["From Max"].default_value = -1.0
    mr_y.inputs["To Min"].default_value = 0.72
    mr_y.inputs["To Max"].default_value = 1.15
    links.new(sep.outputs["Y"], mr_y.inputs["Value"])

    # Depth AO from Z (-0.06 back -> +0.06 front)
    mr_z = nodes.new("ShaderNodeMapRange")
    mr_z.inputs["From Min"].default_value = -0.055
    mr_z.inputs["From Max"].default_value = 0.055
    mr_z.inputs["To Min"].default_value = 0.55
    mr_z.inputs["To Max"].default_value = 1.08
    links.new(sep.outputs["Z"], mr_z.inputs["Value"])

    # Color ramp: rich dark Vietnamese espresso-black with subtle warm chestnut strands
    ramp = nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.22
    ramp.color_ramp.elements[0].color = (0.022, 0.017, 0.015, 1.0)
    ramp.color_ramp.elements[1].position = 0.80
    ramp.color_ramp.elements[1].color = (0.072, 0.048, 0.036, 1.0)
    links.new(noise.outputs["Fac"], ramp.inputs["Fac"])

    # Multiply by depth AO & subtle root darkening
    mul_ao = nodes.new("ShaderNodeMath")
    mul_ao.operation = "MULTIPLY"
    links.new(mr_y.outputs["Result"], mul_ao.inputs[0])
    links.new(mr_z.outputs["Result"], mul_ao.inputs[1])

    mix_col = nodes.new("ShaderNodeMix")
    mix_col.data_type = "RGBA"
    mix_col.blend_type = "MULTIPLY"
    mix_col.inputs["Factor"].default_value = 1.0
    links.new(ramp.outputs["Color"], mix_col.inputs["A"])
    comb_ao = nodes.new("ShaderNodeCombineColor")
    links.new(mul_ao.outputs["Value"], comb_ao.inputs["Red"])
    links.new(mul_ao.outputs["Value"], comb_ao.inputs["Green"])
    links.new(mul_ao.outputs["Value"], comb_ao.inputs["Blue"])
    links.new(comb_ao.outputs["Color"], mix_col.inputs["B"])

    links.new(mix_col.outputs["Result"], em.inputs["Color"])
    em.inputs["Strength"].default_value = 1.0
    links.new(em.outputs["Emission"], out.inputs["Surface"])
    return mat


def make_normal_material():
    """Encode world/camera-space cylindrical strand normal into tangent-space RGB (0.5, 0.5, 1.0 flat)."""
    mat = bpy.data.materials.new("HairAtlas_Normal")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()

    out = nodes.new("ShaderNodeOutputMaterial")
    em = nodes.new("ShaderNodeEmission")
    geo = nodes.new("ShaderNodeNewGeometry")
    vmath_mul = nodes.new("ShaderNodeVectorMath")
    vmath_mul.operation = "SCALE"
    vmath_mul.inputs["Scale"].default_value = 0.5
    vmath_add = nodes.new("ShaderNodeVectorMath")
    vmath_add.operation = "ADD"
    vmath_add.inputs[1].default_value = (0.5, 0.5, 0.5)

    links.new(geo.outputs["Normal"], vmath_mul.inputs[0])
    links.new(vmath_mul.outputs["Vector"], vmath_add.inputs[0])
    links.new(vmath_add.outputs["Vector"], em.inputs["Color"])
    links.new(em.outputs["Emission"], out.inputs["Surface"])
    return mat


def make_flow_material():
    """Encode R=StrandID (specular shift), G=RootToTip (0=root,1=tip), B=DepthAO (0.25..1.0)."""
    mat = bpy.data.materials.new("HairAtlas_Flow")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()

    out = nodes.new("ShaderNodeOutputMaterial")
    em = nodes.new("ShaderNodeEmission")
    geo = nodes.new("ShaderNodeNewGeometry")
    sep = nodes.new("ShaderNodeSeparateXYZ")
    links.new(geo.outputs["Position"], sep.inputs["Vector"])

    # R: high-frequency strand ID along X
    mapping = nodes.new("ShaderNodeMapping")
    mapping.inputs["Scale"].default_value = (24.0, 0.06, 14.0)
    links.new(geo.outputs["Position"], mapping.inputs["Vector"])
    noise = nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 120.0
    noise.inputs["Detail"].default_value = 2.0
    links.new(mapping.outputs["Vector"], noise.inputs["Vector"])

    # G: root (Y=+1 -> 0) to tip (Y=-1 -> 1)
    mr_y = nodes.new("ShaderNodeMapRange")
    mr_y.inputs["From Min"].default_value = 1.0
    mr_y.inputs["From Max"].default_value = -1.0
    mr_y.inputs["To Min"].default_value = 0.0
    mr_y.inputs["To Max"].default_value = 1.0
    links.new(sep.outputs["Y"], mr_y.inputs["Value"])

    # B: depth AO from Z
    mr_z = nodes.new("ShaderNodeMapRange")
    mr_z.inputs["From Min"].default_value = -0.055
    mr_z.inputs["From Max"].default_value = 0.055
    mr_z.inputs["To Min"].default_value = 0.25
    mr_z.inputs["To Max"].default_value = 1.0
    links.new(sep.outputs["Z"], mr_z.inputs["Value"])

    comb = nodes.new("ShaderNodeCombineColor")
    links.new(noise.outputs["Fac"], comb.inputs["Red"])
    links.new(mr_y.outputs["Result"], comb.inputs["Green"])
    links.new(mr_z.outputs["Result"], comb.inputs["Blue"])
    links.new(comb.outputs["Color"], em.inputs["Color"])
    links.new(em.outputs["Emission"], out.inputs["Surface"])
    return mat


def bake_atlas():
    TEX_DIR.mkdir(parents=True, exist_ok=True)
    SRC_TEX_DIR.mkdir(parents=True, exist_ok=True)

    scene = setup_ortho_scene(res=2048)
    rng = random.Random(20051118)

    # 4 distinct hair-card columns across U in [0, 1]
    bundles = [
        # Col 0: Dense base clump (thick coverage, soft tapered tips)
        create_hair_fiber_bundle(rng, "Col0_DenseBase", 0.01, 0.27, count=420,
                                 r_min=0.0024, r_max=0.0046, waviness=0.06,
                                 clump_pull=0.25, tip_spread=0.14, length_range=(0.86, 0.99), z_spread=0.045),
        # Col 1: Medium layered clump (visible individual locks & separations)
        create_hair_fiber_bundle(rng, "Col1_MidClump", 0.29, 0.55, count=260,
                                 r_min=0.0018, r_max=0.0038, waviness=0.10,
                                 clump_pull=0.52, tip_spread=0.18, length_range=(0.76, 0.98), z_spread=0.045),
        # Col 2: Fine breakup strands (wispy grouped strands for surface layering)
        create_hair_fiber_bundle(rng, "Col2_FineBreakup", 0.57, 0.79, count=155,
                                 r_min=0.0014, r_max=0.0030, waviness=0.14,
                                 clump_pull=0.68, tip_spread=0.24, length_range=(0.68, 0.96), z_spread=0.040),
        # Col 3: Delicate baby-hairs & flyaways for hairline/temple/nape softness
        create_hair_fiber_bundle(rng, "Col3_BabyWisps", 0.81, 0.99, count=72,
                                 r_min=0.0010, r_max=0.0022, waviness=0.22,
                                 clump_pull=0.35, tip_spread=0.36, length_range=(0.55, 0.95), z_spread=0.030, is_wisp=True),
    ]

    # Pass 1: Albedo + Alpha (sRGB)
    mat_albedo = make_albedo_material()
    for o in bundles:
        o.data.materials.clear()
        o.data.materials.append(mat_albedo)
    scene.view_settings.view_transform = "Standard"
    scene.render.image_settings.color_mode = "RGBA"
    albedo_path = TEX_DIR / "HairCards_Color.png"
    scene.render.filepath = str(albedo_path)
    bpy.ops.render.render(write_still=True)

    # Pass 2: Normal map (Raw/Linear, neutral background (0.5, 0.5, 1.0))
    mat_norm = make_normal_material()
    for o in bundles:
        o.data.materials[0] = mat_norm
    scene.render.film_transparent = False
    scene.world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.5, 0.5, 1.0, 1.0)
    scene.world.node_tree.nodes["Background"].inputs["Strength"].default_value = 1.0
    scene.view_settings.view_transform = "Raw"
    scene.render.image_settings.color_mode = "RGB"
    norm_path = TEX_DIR / "HairCards_Normal.png"
    scene.render.filepath = str(norm_path)
    bpy.ops.render.render(write_still=True)

    # Pass 3: Flow / ID / Root-to-Tip / Depth AO + Alpha (Raw/Linear RGBA)
    mat_flow = make_flow_material()
    for o in bundles:
        o.data.materials[0] = mat_flow
    scene.render.film_transparent = True
    scene.world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.5, 0.5, 0.5, 1.0)
    scene.world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.0
    scene.view_settings.view_transform = "Raw"
    scene.render.image_settings.color_mode = "RGBA"
    flow_path = TEX_DIR / "HairCards_Flow.png"
    scene.render.filepath = str(flow_path)
    bpy.ops.render.render(write_still=True)

    # Copy editable masters to ArtSource/Characters/Textures/
    for p in (albedo_path, norm_path, flow_path):
        shutil.copy2(p, SRC_TEX_DIR / p.name)
    print(f"[HAIR ATLAS] Baked 2048x2048 HairCards_Color/Normal/Flow -> {TEX_DIR}")


if __name__ == "__main__":
    bake_atlas()
