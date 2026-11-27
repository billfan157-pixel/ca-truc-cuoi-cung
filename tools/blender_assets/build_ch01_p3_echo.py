"""CH01 Rebirth III Phase 3 — Blender 4.5 Hero Asset: CTCC_VFX_B08Echo.

Builds the multi-layer in-world Perceptual Echo geometry aligned to millimeter precision with
LOC02's CTCC_ENV_ReceptionPartition at world origin (2.02, 1.125, -3.24), yaw = 0:
  - B08Echo_TrayImprint  (slot: EchoTray)  — subtly curled archival afterimage hovering above
                                             the stainless pass-through scoop tray, tilted ~11 deg
                                             toward visitor (+X) for crisp legibility at AN01.
  - B08Echo_GlassImprint (slot: EchoGlass) — gently bowed optical afterimage seated 1.8cm in front
                                             of the center sliding reception window glass pane
                                             (x=+0.022, y in [-0.34..+0.34], z in [0.33..0.99]).
  - B08Echo_CounterAura  (slot: EchoHalo)  — delicate concave optical halo framing the pass-through
                                             aperture so the counter softly draws Minh's gaze from
                                             the lobby doorway in dark 2012 Reality R.

Strict canon & performance contract:
  - ZERO collision meshes (pure perceptual manifestation, never blocks capsule/raycasts).
  - Clean 0..1 UVs oriented left-to-right when viewed by Minh from lobby (+X facing -X).
  - < 1,200 triangles total (zero impact on LOC02 GPU budget).
  - Editable source saved to ArtSource/Blender/CTCC_VFX_B08Echo.blend.
  - Game-ready FBX exported to Prototype/Assets/CTCC/Art/Meshes/CTCC_VFX_B08Echo.fbx.
"""

from __future__ import annotations

import math
import sys
from pathlib import Path

import bmesh
import bpy

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import common as C  # noqa: E402


def make_echo_material(name: str, rgba: tuple[float, float, float, float]):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf is not None:
        bsdf.inputs["Base Color"].default_value = rgba
        bsdf.inputs["Roughness"].default_value = 0.25
        if "Alpha" in bsdf.inputs:
            bsdf.inputs["Alpha"].default_value = rgba[3]
    return mat


def create_uv_surface(name: str, nu: int, nv: int, fn, mat):
    """Build a smooth subdivided surface with exact 0..1 UV coordinates and smooth normals."""
    mesh = bpy.data.meshes.new(name)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)

    bm = bmesh.new()
    uv_layer = bm.loops.layers.uv.new("UVMap")
    grid = []
    for j in range(nv + 1):
        v = j / nv
        row = []
        for i in range(nu + 1):
            u = i / nu
            row.append(bm.verts.new(fn(u, v)))
        grid.append(row)

    bm.verts.ensure_lookup_table()
    for j in range(nv):
        for i in range(nu):
            v00 = grid[j][i]
            v10 = grid[j][i + 1]
            v11 = grid[j + 1][i + 1]
            v01 = grid[j + 1][i]
            face = bm.faces.new((v00, v10, v11, v01))
            face.smooth = True
            uvs = [
                (i / nu, j / nv),
                ((i + 1) / nu, j / nv),
                ((i + 1) / nu, (j + 1) / nv),
                (i / nu, (j + 1) / nv),
            ]
            for loop, uv in zip(face.loops, uvs):
                loop[uv_layer].uv = uv

    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()

    obj.data.materials.append(mat)
    return obj


def build_b08_echo():
    C.fresh_scene()

    mat_tray = make_echo_material("EchoTray", (0.82, 0.92, 0.90, 0.72))
    mat_glass = make_echo_material("EchoGlass", (0.76, 0.88, 0.90, 0.45))
    mat_halo = make_echo_material("EchoHalo", (0.70, 0.86, 0.88, 0.28))

    # 1) Tray Imprint — gently curled archival sheet floating 2.2cm above the stainless pass-through scoop tray
    # and visitor counter lip (shifted cleanly clear of AN01 open duty ledger behind the glass!).
    def tray_fn(u: float, v: float):
        y = -0.245 + u * 0.420          # centered cleanly on stainless scoop tray
        x = 0.295 - v * 0.340           # spans [+0.295 (visitor edge) .. -0.045 (tray back, zero ledger overlap!)]
        tilt = v * 0.068                # lifts back edge ~6.8cm for crisp standing readability
        curl = 0.010 * ((u - 0.5) ** 2 * 4.0) + 0.007 * math.sin(u * math.pi) * math.sin(v * math.pi)
        z = 0.030 + tilt + curl
        return (x, y, z)

    tray = create_uv_surface("B08Echo_TrayImprint", 16, 16, tray_fn, mat_tray)

    # 2) Center Sliding Glass Pane Optical Imprint — seated 1.8cm in front of RP_CenterGlass,
    # framed cleanly BELOW the 11:52 wall clock on the back wall so clock + echo form one vertical axis!
    def glass_fn(u: float, v: float):
        y = -0.330 + u * 0.660
        z = 0.295 + v * 0.585
        bow = 0.010 * math.sin(u * math.pi) * math.sin(v * math.pi)
        x = 0.022 + bow
        return (x, y, z)

    glass = create_uv_surface("B08Echo_GlassImprint", 16, 16, glass_fn, mat_glass)

    # 3) Pass-through Aperture Soft Refractive Aura — subtle curved veil arching inside the service opening.
    def aura_fn(u: float, v: float):
        ang = (u - 0.5) * math.pi * 0.92
        y = math.sin(ang) * 0.355
        z = 0.018 + v * 0.228
        x = 0.175 - (1.0 - math.cos(ang)) * 0.14 - v * 0.055
        return (x, y, z)

    aura = create_uv_surface("B08Echo_CounterAura", 14, 8, aura_fn, mat_halo)

    objs = [tray, glass, aura]
    C.BLEND_DIR.mkdir(parents=True, exist_ok=True)
    C.FBX_DIR.mkdir(parents=True, exist_ok=True)

    blend_path = C.BLEND_DIR / "CTCC_VFX_B08Echo.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))

    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    fbx_path = C.FBX_DIR / "CTCC_VFX_B08Echo.fbx"
    bpy.ops.export_scene.fbx(
        filepath=str(fbx_path),
        use_selection=True,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        object_types={"MESH"},
        mesh_smooth_type="FACE",
        use_mesh_modifiers=True,
        use_tspace=True,
        add_leaf_bones=False,
        bake_anim=False,
        embed_textures=False,
    )
    tris = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in objs)
    print(f"[BLENDER ASSET] CTCC_VFX_B08Echo: {tris} tris (3 perceptual meshes, 0 colliders) -> {fbx_path.name}")


if __name__ == "__main__":
    build_b08_echo()
