"""Authentic 3D Branching Tropical Vietnamese Courtyard Trees (cây bàng / xà cừ silhouette) built in Blender 4.5.

Replaces primitive flat leaf cards with genuine root-flared curved trunks, multi-level organic boughs/twigs,
and crossed/layered UV-mapped botanical tropical twig cards using our painted TropicalLeaf_Color alpha-clip atlas.
"""

import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import common as C


def add_tapered_branch(bm, points, radii, segs=8):
    """Add a curved organic tapered branch tube directly into a bmesh."""
    rings = []
    for idx, (pt, rad) in enumerate(zip(points, radii)):
        p = Vector(pt)
        if idx == 0:
            tangent = (Vector(points[1]) - p).normalized()
        elif idx == len(points) - 1:
            tangent = (p - Vector(points[idx - 1])).normalized()
        else:
            tangent = (Vector(points[idx + 1]) - Vector(points[idx - 1])).normalized()
        up = Vector((0, 0, 1)) if abs(tangent.z) < 0.92 else Vector((1, 0, 0))
        side = tangent.cross(up).normalized()
        binorm = side.cross(tangent).normalized()
        ring = []
        for k in range(segs):
            a = math.tau * k / segs
            offset = (side * math.cos(a) + binorm * math.sin(a)) * rad
            ring.append(bm.verts.new(p + offset))
        rings.append(ring)
    for r_i in range(len(rings) - 1):
        r0, r1 = rings[r_i], rings[r_i + 1]
        for k in range(segs):
            nk = (k + 1) % segs
            bm.faces.new((r0[k], r0[nk], r1[nk], r1[k]))
    # Cap tip
    tip_c = bm.verts.new(Vector(points[-1]))
    for k in range(segs):
        bm.faces.new((rings[-1][k], rings[-1][(k + 1) % segs], tip_c))


def add_leaf_cluster(bm, uv_layer, origin, direction, size, rng):
    """Add a 3D curved botanical frond pair (two slightly cupped crossed cards) mapped [0..1] to TropicalLeaf atlas."""
    d = direction.normalized()
    up = Vector((0, 0, 1)) if abs(d.z) < 0.9 else Vector((1, 0, 0))
    right = d.cross(up).normalized()
    real_up = right.cross(d).normalized()

    for card_i in range(2):
        roll = (0.35 if card_i == 0 else -0.42) + rng.uniform(-0.22, 0.22)
        pitch = rng.uniform(-0.28, 0.22)
        rot = Matrix.Rotation(roll, 3, d) @ Matrix.Rotation(pitch, 3, right)
        c_right = rot @ right
        c_up = rot @ real_up
        c_dir = rot @ d

        w = size * rng.uniform(0.75, 1.05)
        l = size * rng.uniform(0.95, 1.25)
        # 2x2 slightly cupped quad (gives natural 3D light wrap instead of flat plane!)
        p00 = origin - c_right * (w * 0.5)
        p10 = origin + c_right * (w * 0.5)
        p01 = origin + c_dir * (l * 0.52) - c_right * (w * 0.54) + c_up * (w * 0.08)
        p11 = origin + c_dir * (l * 0.52) + c_right * (w * 0.54) + c_up * (w * 0.08)
        p02 = origin + c_dir * l - c_right * (w * 0.42) - c_up * (w * 0.05)
        p12 = origin + c_dir * l + c_right * (w * 0.42) - c_up * (w * 0.05)

        v00, v10 = bm.verts.new(p00), bm.verts.new(p10)
        v01, v11 = bm.verts.new(p01), bm.verts.new(p11)
        v02, v12 = bm.verts.new(p02), bm.verts.new(p12)

        f1 = bm.faces.new((v00, v10, v11, v01))
        f2 = bm.faces.new((v01, v11, v12, v02))
        uvs1 = [(0.02, 0.02), (0.98, 0.02), (0.98, 0.52), (0.02, 0.52)]
        uvs2 = [(0.02, 0.52), (0.98, 0.52), (0.98, 0.98), (0.02, 0.98)]
        for loop, uv in zip(f1.loops, uvs1):
            loop[uv_layer].uv = uv
        for loop, uv in zip(f2.loops, uvs2):
            loop[uv_layer].uv = uv


def build_courtyard_tree(seed=701, height=4.6, spread=2.1):
    """Build an organic 3D tropical courtyard tree with bark trunk/branches + botanical foliage crown."""
    rng = random.Random(seed)
    bm_wood = bmesh.new()
    bm_leaf = bmesh.new()
    uv_leaf = bm_leaf.loops.layers.uv.new("UVMap")

    # Main gently curving gnarled trunk with buttress root flare
    trunk_pts = []
    trunk_r = []
    steps = 7
    lean_x = rng.uniform(-0.18, 0.18)
    lean_y = rng.uniform(-0.18, 0.18)
    for i in range(steps):
        t = i / float(steps - 1)
        z = t * height * 0.58
        x = lean_x * t + math.sin(t * 3.1 + seed) * 0.09
        y = lean_y * t + math.cos(t * 2.7 + seed) * 0.09
        r = (0.21 if i == 0 else 0.16 * (1.0 - t * 0.45))
        trunk_pts.append((x, y, z))
        trunk_r.append(r)
    add_tapered_branch(bm_wood, trunk_pts, trunk_r, segs=10)

    # 4 buttress roots anchoring into courtyard soil
    for k in range(4):
        a = k * math.tau / 4.0 + rng.uniform(-0.3, 0.3)
        rpts = [(math.cos(a) * 0.08, math.sin(a) * 0.08, 0.42),
                (math.cos(a) * 0.24, math.sin(a) * 0.24, 0.14),
                (math.cos(a) * 0.44, math.sin(a) * 0.44, -0.05)]
        add_tapered_branch(bm_wood, rpts, [0.085, 0.062, 0.028], segs=6)

    # Tiered spreading tropical boughs (characteristic horizontal-tiered canopy of Vietnamese cây bàng!)
    crown_base = Vector(trunk_pts[-1])
    n_boughs = 7
    for b in range(n_boughs):
        tier = 0 if b < 4 else 1
        az = b * 2.39996 + rng.uniform(-0.25, 0.25)
        start_z = height * (0.46 + tier * 0.14 + rng.uniform(-0.04, 0.05))
        origin = Vector((lean_x * 0.7, lean_y * 0.7, start_z))
        b_len = spread * (1.05 if tier == 0 else 0.78) * rng.uniform(0.85, 1.15)
        elev = rng.uniform(0.28, 0.56) if tier == 0 else rng.uniform(0.58, 0.92)
        dir_xy = Vector((math.cos(az), math.sin(az), 0))
        p1 = origin + dir_xy * (b_len * 0.42) + Vector((0, 0, b_len * math.sin(elev) * 0.55))
        p2 = origin + dir_xy * (b_len * 0.78) + Vector((0, 0, b_len * math.sin(elev) * 0.85 + rng.uniform(-0.1, 0.15)))
        p3 = origin + dir_xy * b_len + Vector((0, 0, b_len * math.sin(elev) * 0.95))
        add_tapered_branch(bm_wood, [origin, p1, p2, p3], [0.072, 0.048, 0.028, 0.013], segs=7)

        # Secondary forked twigs + dense botanical leaf frond clusters along outer half of each bough
        for sub in range(4):
            t_s = 0.45 + sub * 0.18
            base_s = origin.lerp(p3, t_s)
            sub_az = az + (0.65 if sub % 2 == 0 else -0.65) + rng.uniform(-0.25, 0.25)
            sub_dir = Vector((math.cos(sub_az), math.sin(sub_az), rng.uniform(0.15, 0.65))).normalized()
            sub_len = b_len * rng.uniform(0.32, 0.52)
            tip_s = base_s + sub_dir * sub_len
            add_tapered_branch(bm_wood, [base_s, (base_s + tip_s) * 0.5 + Vector((0, 0, 0.08)), tip_s], [0.022, 0.014, 0.006], segs=5)
            # Attach 4 layered tropical leaf clusters around each twig tip & midpoint
            for lf in range(4):
                l_orig = base_s.lerp(tip_s, 0.45 + lf * 0.18) + Vector((rng.uniform(-0.14, 0.14), rng.uniform(-0.14, 0.14), rng.uniform(-0.08, 0.12)))
                l_dir = (sub_dir + Vector((rng.uniform(-0.6, 0.6), rng.uniform(-0.6, 0.6), rng.uniform(-0.2, 0.4)))).normalized()
                add_leaf_cluster(bm_leaf, uv_leaf, l_orig, l_dir, rng.uniform(0.58, 0.88), rng)

        # Terminal crown cluster at bough tip
        for lf in range(4):
            l_dir = (dir_xy + Vector((rng.uniform(-0.5, 0.5), rng.uniform(-0.5, 0.5), rng.uniform(0.1, 0.55)))).normalized()
            add_leaf_cluster(bm_leaf, uv_leaf, p3 + Vector((rng.uniform(-0.12, 0.12), rng.uniform(-0.12, 0.12), rng.uniform(-0.08, 0.12))),
                             l_dir, rng.uniform(0.65, 0.95), rng)

    # Top central leader crown foliage
    for lf in range(8):
        a = lf * math.tau / 8.0
        l_orig = crown_base + Vector((math.cos(a) * 0.28, math.sin(a) * 0.28, height * 0.32 + rng.uniform(-0.15, 0.25)))
        l_dir = Vector((math.cos(a) * 0.8, math.sin(a) * 0.8, rng.uniform(0.25, 0.75))).normalized()
        add_leaf_cluster(bm_leaf, uv_leaf, l_orig, l_dir, rng.uniform(0.70, 0.98), rng)

    bmesh.ops.recalc_face_normals(bm_wood, faces=bm_wood.faces)
    m_wood = bpy.data.meshes.new("TreeWood_mesh")
    bm_wood.to_mesh(m_wood)
    bm_wood.free()
    obj_wood = bpy.data.objects.new("TreeWood", m_wood)
    bpy.context.collection.objects.link(obj_wood)
    C.finalize_mesh(obj_wood, "TreeWood", "Bark", smooth_angle=55.0, uv_scale=0.65, apply_mods=False)

    m_leaf = bpy.data.meshes.new("TreeCanopy_mesh")
    bm_leaf.to_mesh(m_leaf)
    bm_leaf.free()
    obj_leaf = bpy.data.objects.new("TreeCanopy", m_leaf)
    bpy.context.collection.objects.link(obj_leaf)
    C.assign_mat(obj_leaf, "TropicalLeaf")
    for poly in m_leaf.polygons:
        poly.use_smooth = True

    return [obj_wood, obj_leaf]


def run_all():
    C.export_asset("CTCC_ENV_CourtyardTree_A", lambda: build_courtyard_tree(701, 4.7, 2.2), make_lods=True, render=True, cam_dir=(1.6, -1.8, 0.6))
    C.export_asset("CTCC_ENV_CourtyardTree_B", lambda: build_courtyard_tree(809, 3.9, 1.85), make_lods=True, render=True, cam_dir=(1.6, -1.8, 0.6))


if __name__ == "__main__":
    run_all()
