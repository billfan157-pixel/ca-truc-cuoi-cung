"""LOC03 Ward 101 Furniture, Handwashing Basin, Stools & Architectural Kit (Blender 4.5 LTS).

Produces Cycles-baked PBR hero assets (1-2 draw calls each instead of 5-9!):
  1. CTCC_PROP_BedsideCabinet    -> LOC03_BedsideCab (1024² B/R) — Cream-mint enameled cabinet + cloth-draped towel!
  2. CTCC_LOC03_Bed3Drawer       -> LOC03_Bed3Drawer (1024² B-only) — Seamless matching interactive drawer for Bed 3
  3. CTCC_PROP_MedicalCabinet    -> LOC03_MedCabinet (2048² B/R + Glass) — Richly stocked 2-door glass-and-louver steel cabinet
  4. CTCC_LOC03_WardSink         -> LOC03_WardSink (2048² B/R) — East-wall 1987 ceramic washbasin, tile splashback, brass tap, mirror & draped towel!
  5. CTCC_LOC03_WardStool        -> LOC03_WardStool (1024² B/R) — Classic Vietnamese round stainless-and-green hospital stool
  6. CTCC_LOC03_WardArch         -> LOC03_WardArch (2048² B/R) — Beveled concrete ceiling beams, corner pilasters & continuous galvanized conduit runs
  7. CTCC_ENV_SteelWindow_Wide/Mid/Narrow -> LOC03_WindowWide/Mid/Narrow (1024² B/R + FrostedGlass) — 1987 green steel casements & diamond hoa sắt!
  8. CTCC_ENV_WardDoorFrame/Leaf -> LOC03_WardDoor (2048² B/R + FrostedGlass) — Matching LOC02 varnished Vietnamese ironwood door!
  9. CTCC_ENV_FluorescentFixture -> LOC03_TubeFixture (1024² B/R + DeadTube)
 10. CTCC_ENV_WallOutlet         -> LOC03_WallOutlet (512² B/R) — Clean Bakelite double socket + red rocker switch (no floating stick!)
"""
from __future__ import annotations

import math
import sys
from pathlib import Path

import bpy

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))
sys.path.insert(0, str(HERE.parent / "loc02"))

import common as C  # noqa: E402
import hero_lib as H  # noqa: E402
import ward_lib as W  # noqa: E402
from build_ward_beds_curtain import _caster_wheel  # noqa: E402


def build_bedside_cabinet():
    """Authentic 1987 Vietnamese pale cream-mint enameled steel hospital bedside cabinet (0.49 x 0.43 x 0.78 m)
    with honed terrazzo top deck, 3-sided anti-spill rail, louvers, TS-AS-101 asset plate & cloth-draped towel!"""
    objs = []
    w, d = 0.49, 0.43

    # 4 Tubular legs with black rubber ferrules
    for sx in (-0.21, 0.21):
        for sy in (-0.18, 0.18):
            objs.append(H.cyl(f"FootRubber_{sx:.2f}_{sy:.2f}", (sx, sy, 0.0), (sx, sy, 0.032), 0.017, "rubber", seg=12))
            objs.append(H.cyl(f"LegTube_{sx:.2f}_{sy:.2f}", (sx, sy, 0.030), (sx, sy, 0.115), 0.014, "cab_cream_mint", seg=12))

    # Welded steel cabinet carcass (back, bottom, divider, inner shelf, sides)
    objs.append(H.box("BodyBack", (0, d * 0.5 - 0.012, 0.425), (w, 0.024, 0.64), "cab_cream_mint", bevel=0.005))
    objs.append(H.box("BodyBottom", (0, 0, 0.115), (w, d, 0.024), "cab_cream_mint", bevel=0.004))
    objs.append(H.box("BodyDivider", (0, 0.005, 0.52), (w - 0.02, d - 0.02, 0.020), "cab_cream_mint", bevel=0.003))
    objs.append(H.box("InnerShelf", (0, 0.015, 0.31), (w - 0.03, d - 0.04, 0.016), "cab_cream_mint", bevel=0.002))
    for s in (-1, 1):
        objs.append(H.box(f"BodySide_{s}", (s * (w * 0.5 - 0.012), 0, 0.425), (0.024, d, 0.64), "cab_cream_mint", bevel=0.005))

    # Honed terrazzo/ceramic top deck + 3-sided brushed stainless anti-spill gallery rail!
    objs.append(H.box("TopDeck", (0, 0, 0.745), (w + 0.02, d + 0.02, 0.026), "terrazzo", bevel=0.006))
    objs.append(H.box("GalleryBack", (0, d * 0.5 + 0.005, 0.774), (w + 0.024, 0.014, 0.036), "cab_cream_mint", bevel=0.004))
    for s in (-1, 1):
        objs.append(H.box(f"GallerySide_{s}", (s * (w * 0.5 + 0.005), 0.005, 0.774), (0.014, d + 0.016, 0.036), "cab_cream_mint", bevel=0.004))
    # Stainless guard wire atop gallery
    objs.append(H.pipe("GalleryRail", [
        (-w * 0.5, -d * 0.46, 0.798), (-w * 0.5, d * 0.48, 0.798),
        (w * 0.5, d * 0.48, 0.798), (w * 0.5, -d * 0.46, 0.798)
    ], 0.0045, "satin_inox", res=3))

    # Upper recessed drawer face + chrome pull + brass index holder
    dy = -d * 0.5 - 0.006
    objs.append(H.box("DrawerFront", (0, dy, 0.625), (w - 0.036, 0.022, 0.172), "cab_cream_mint", bevel=0.006))
    objs.append(H.pipe("DrawerPull", [
        (-0.082, dy - 0.011, 0.624), (-0.072, dy - 0.038, 0.624),
        (0.072, dy - 0.038, 0.624), (0.082, dy - 0.011, 0.624)
    ], 0.007, "satin_inox", res=4))
    objs.append(H.box("DrawerTagFrame", (0, dy - 0.012, 0.674), (0.078, 0.005, 0.024), "brass", bevel=0.001))

    # Lower vented steel door with 6 3D stamped louvers + chrome handle + TS-AS-101 inventory tag!
    objs.append(H.box("LowerDoor", (0, -d * 0.5 - 0.005, 0.315), (w - 0.036, 0.022, 0.375), "cab_cream_mint", bevel=0.006))
    for k in range(6):
        lz = 0.185 + k * 0.030
        objs.append(H.box(f"DoorLouver_{k}", (0, -d * 0.5 - 0.019, lz), (0.26, 0.013, 0.011), "cab_cream_mint",
                          bevel=0.002, rot=(math.radians(-28), 0, 0)))
        objs.append(H.box(f"DoorLouverDark_{k}", (0, -d * 0.5 - 0.015, lz - 0.005), (0.25, 0.005, 0.007), "plastic_black", bevel=0.0))
    objs.append(H.pipe("DoorHandle", [
        (0.16, -d * 0.5 - 0.015, 0.39), (0.16, -d * 0.5 - 0.042, 0.39),
        (0.16, -d * 0.5 - 0.042, 0.31), (0.16, -d * 0.5 - 0.015, 0.31)
    ], 0.0065, "satin_inox", res=3))
    tag = H.box("AssetTag", (0, -d * 0.5 - 0.018, 0.458), (0.098, 0.003, 0.036), "print:med_labels", bevel=0.001)
    H.set_planar_print_uv(tag, axis="Y", uv_rect=(0.52, 0.51, 0.98, 0.67))
    objs.append(tag)

    # Side stainless towel bar (+X) WITH REAL CLOTH-SIMULATED DRAPED HOSPITAL FACE TOWEL!
    bar_x = w * 0.5 + 0.046
    bar_z = 0.65
    objs.append(H.pipe("TowelBar", [
        (w * 0.5, -0.14, bar_z), (bar_x, -0.14, bar_z),
        (bar_x, 0.14, bar_z), (w * 0.5, 0.14, bar_z)
    ], 0.0065, "satin_inox", res=3))

    # Cloth-simulated towel draped naturally over towel bar
    col_bar = H.cyl("__ColBar", (bar_x, -0.16, bar_z), (bar_x, 0.16, bar_z), 0.012, "satin_inox", seg=10)
    col_side = H.box("__ColCabSide", (w * 0.5 - 0.01, 0, 0.44), (0.045, d + 0.02, 0.66), "cab_cream_mint", bevel=0.005)

    def towel_init(u, v):
        # u wraps across bar (-0.18..+0.18 in arc), v along bar length (-0.095..+0.095)
        y = -0.015 + (v - 0.5) * 0.19
        ang = (u - 0.5) * math.pi * 1.15
        # Wrap over top of bar and hang down both sides
        if abs(u - 0.5) < 0.14:
            x = bar_x + math.sin(ang * 2.5) * 0.015
            z = bar_z + 0.016 * math.cos(ang * 2.5)
        else:
            side = 1.0 if u > 0.5 else -1.0
            drop = (abs(u - 0.5) - 0.14) / 0.36
            x = bar_x + side * (0.015 + 0.004 * math.sin(v * 9.0))
            z = bar_z - drop * (0.21 if side > 0 else 0.16)
        return (x, y, z)

    towel = W.create_cloth_grid("HangingTowel", 20, 14, towel_init, "towel_terry",
                                pin_fn=lambda u, v: (0.44 < u < 0.56))
    W.simulate_cloth_drape(
        towel, [col_bar, col_side], frames=18, mass=0.25, tension=16.0, bending=0.35,
        collision_dist=0.005, solidify_thickness=0.005, cleanup_colliders=True,
    )
    objs.append(towel)
    return objs


def build_bed3_drawer():
    """Matching cream-mint enameled interactive pull-out drawer for Bed 3 (DRAWER-BED3 -> OBS101D).
    Exact match to CTCC_PROP_InteractiveDrawer origin/scale (0.44 x 0.30 x 0.14 m, front faces -Y)."""
    objs = []
    w, d, h = 0.44, 0.30, 0.14
    objs.append(H.box("DrawerFace", (0, -d * 0.5, 0), (w, 0.022, h), "cab_cream_mint", bevel=0.005))
    objs.append(H.pipe("DrawerChromePull", [
        (-0.08, -d * 0.5 - 0.010, 0.004), (-0.07, -d * 0.5 - 0.038, 0.004),
        (0.07, -d * 0.5 - 0.038, 0.004), (0.08, -d * 0.5 - 0.010, 0.004),
    ], 0.007, "satin_inox", res=4))
    objs.append(H.box("LabelFrame", (0, -d * 0.5 - 0.013, 0.044), (0.092, 0.006, 0.028), "brass", bevel=0.002))
    objs.append(H.box("DrawerBottom", (0, 0.005, -h * 0.44), (w - 0.03, d - 0.02, 0.012), "steel_dark", bevel=0.002))
    objs.append(H.box("DrawerBack", (0, d * 0.5 - 0.01, -0.008), (w - 0.03, 0.016, h - 0.024), "cab_cream_mint", bevel=0.002))
    for s in (-1, 1):
        objs.append(H.box(f"DrawerSide_{s}", (s * (w * 0.5 - 0.020), 0.005, -0.008), (0.016, d - 0.02, h - 0.024), "cab_cream_mint", bevel=0.002))
        objs.append(H.box(f"DrawerRail_{s}", (s * (w * 0.5 - 0.008), 0.01, -0.018), (0.010, d - 0.03, 0.018), "satin_inox", bevel=0.001))
    objs.append(H.box("InnerFolder", (-0.05, 0.02, -0.044), (0.24, 0.19, 0.008), "kraft", bevel=0.002, rot=(0, 0, 0.06)))
    return objs


def build_medical_cabinet():
    """Hero rolling 2-door glass-and-louver Vietnamese hospital steel medical cabinet (0.48 x 1.36 x 2.06 m).
    Doors face -X into Room 101! Single 2048² baked atlas + Glass = 2 draw calls instead of 9!"""
    objs = []
    d, w, h = 0.48, 1.36, 2.06
    base_z = 0.12

    for sx in (-0.17, 0.17):
        for sy in (-0.56, 0.56):
            _caster_wheel(objs, f"CabCaster_{sx:.2f}_{sy:.2f}", sx, sy, wheel_r=0.042, z_top=base_z + 0.01)

    # Outer enameled cabinet carcass (pale cream-mint 1987 hospital finish — NO garish red splats!)
    objs.append(H.box("CabBack", (d * 0.5 - 0.014, 0, (base_z + h) * 0.5), (0.028, w, h - base_z), "cab_cream_mint", bevel=0.005))
    objs.append(H.box("CabBottom", (0, 0, base_z + 0.018), (d, w, 0.036), "cab_cream_mint", bevel=0.005))
    objs.append(H.box("CabTop", (0, 0, h - 0.022), (d + 0.016, w + 0.016, 0.044), "cab_cream_mint", bevel=0.007))
    objs.append(H.box("CabMidDeck", (0.01, 0, 1.02), (d - 0.03, w - 0.04, 0.026), "cab_cream_mint", bevel=0.003))
    for sy in (-1, 1):
        objs.append(H.box(f"CabSide_{sy}", (0, sy * (w * 0.5 - 0.016), (base_z + h) * 0.5), (d, 0.032, h - base_z), "cab_cream_mint", bevel=0.006))
    objs.append(H.box("CabMullion", (-d * 0.5 + 0.022, 0, (base_z + h) * 0.5), (0.036, 0.036, h - base_z - 0.04), "cab_cream_mint", bevel=0.003))

    # Brushed stainless interior shelves
    for sz in (1.34, 1.68):
        objs.append(H.box(f"UpperShelf_{sz:.2f}", (0.02, 0, sz), (d - 0.08, w - 0.06, 0.018), "satin_inox", bevel=0.002))

    # Richly stocked hospital interior visible through glass panes:
    # 1) Soft folded hospital linen stacks on lower-left & mid-left shelves
    for s_idx, sz in enumerate((1.038, 1.352)):
        for stack_k in range(3):
            mat_k = "cotton_sheet" if stack_k % 2 == 0 else "blanket_jade"
            objs.append(H.box(f"LinenFold_{s_idx}_{stack_k}",
                              (0.035, -0.36 + (stack_k % 2) * 0.016, sz + 0.030 + stack_k * 0.052),
                              (0.28, 0.34, 0.048), mat_k, bevel=0.015, seg=3, rot=(0, 0, (stack_k - 1) * 0.03)))
    # 2) Labeled archival medical binders on lower-right shelf (readable spines facing -X!)
    spine_uvs = [
        (0.02, 0.38, 0.48, 0.48),
        (0.02, 0.26, 0.48, 0.36),
        (0.02, 0.14, 0.48, 0.24),
        (0.02, 0.02, 0.48, 0.12),
    ]
    for k in range(5):
        by = 0.16 + k * 0.088
        tilt = (k - 2) * 0.045
        objs.append(H.box(f"LedgerBinder_{k}", (0.03, by, 1.175), (0.28, 0.064, 0.29),
                          "kraft" if k % 2 == 0 else "cloth_navy", bevel=0.005, rot=(tilt, 0, 0)))
        sp = H.box(f"BinderSpine_{k}", (-0.112, by, 1.175), (0.004, 0.052, 0.24), "print:med_labels", bevel=0.001, rot=(tilt, 0, 0))
        H.set_planar_print_uv(sp, axis="X", uv_rect=spine_uvs[k % 4], rot90=True)
        objs.append(sp)
    # 3) Stainless cotton sterilization drum & kidney tray on middle-right shelf
    objs.append(H.cyl("CottonDrum", (0.03, 0.24, 1.352), (0.03, 0.24, 1.495), 0.085, "satin_inox", seg=20, bevel=0.005))
    objs.append(H.cyl("CottonDrumLid", (0.03, 0.24, 1.490), (0.03, 0.24, 1.512), 0.090, "satin_inox", seg=20, bevel=0.004))
    objs.append(H.box("KidneyTray", (0.01, 0.46, 1.368), (0.12, 0.22, 0.032), "satin_inox", bevel=0.012, seg=3, rot=(0, 0, 0.15)))
    objs.append(H.box("GauzeBox", (0.05, 0.47, 1.44), (0.22, 0.16, 0.11), "paper", bevel=0.004))
    # 4) Top shelf: labeled glass NaCl saline & amber antiseptic bottles (CỒN 70° / POVIDON-IOD)
    bottle_specs = [
        (-0.48, 0.175, 0.036, "ceramic", "saline_label", (0, 0, 1, 1)),
        (-0.37, 0.175, 0.036, "ceramic", "saline_label", (0, 0, 1, 1)),
        (-0.24, 0.145, 0.029, "amber_glass", "med_labels", (0.02, 0.76, 0.48, 0.98)),
        (-0.14, 0.130, 0.027, "amber_glass", "med_labels", (0.02, 0.51, 0.48, 0.73)),
        (0.20,  0.175, 0.036, "ceramic", "saline_label", (0, 0, 1, 1)),
        (0.32,  0.150, 0.030, "amber_glass", "med_labels", (0.02, 0.76, 0.48, 0.98)),
        (0.45,  0.132, 0.027, "amber_glass", "med_labels", (0.02, 0.51, 0.48, 0.73)),
    ]
    for k, (by, bh, br, bmat, pkey, rect) in enumerate(bottle_specs):
        objs.append(H.lathe(f"Bottle_{k}", [
            (0, 0), (br * 0.92, 0), (br, 0.008), (br, bh - 0.038),
            (br * 0.46, bh - 0.015), (br * 0.46, bh), (0, bh)
        ], (0.03, by, 1.690), bmat, seg=16))
        objs.append(H.cyl(f"BottleCap_{k}", (0.03, by, 1.690 + bh), (0.03, by, 1.690 + bh + 0.020), br * 0.48, "plastic_black", seg=12))
        lbl = H.box(f"BottleLbl_{k}", (0.03 - br - 0.001, by, 1.690 + bh * 0.46), (0.002, br * 1.35, bh * 0.42), f"print:{pkey}", bevel=0.0)
        H.set_planar_print_uv(lbl, axis="X", uv_rect=rect)
        objs.append(lbl)

    # Twin Front Doors (facing -X): Upper framed glass doors + Lower solid louvered steel doors!
    fx = -d * 0.5 - 0.012
    door_w = w * 0.5 - 0.028
    for side in (-1, 1):
        dy = side * (door_w * 0.5 + 0.012)
        u_mid_z, u_h = 1.53, 0.96
        objs.append(H.box(f"UpDoorTop_{side}", (fx, dy, 2.00), (0.026, door_w, 0.055), "cab_cream_mint", bevel=0.004))
        objs.append(H.box(f"UpDoorBot_{side}", (fx, dy, 1.06), (0.026, door_w, 0.055), "cab_cream_mint", bevel=0.004))
        for es in (-1, 1):
            objs.append(H.box(f"UpDoorStile_{side}_{es}", (fx, dy + es * (door_w * 0.5 - 0.028), u_mid_z),
                              (0.026, 0.056, u_h), "cab_cream_mint", bevel=0.004))
        objs.append(H.box(f"UpDoorGlass_{side}", (fx + 0.004, dy, u_mid_z), (0.006, door_w - 0.09, u_h - 0.09), "Glass", bevel=0.0))

        l_mid_z, l_h = 0.58, 0.85
        objs.append(H.box(f"LowDoor_{side}", (fx, dy, l_mid_z), (0.026, door_w, l_h), "cab_cream_mint", bevel=0.005))
        for k in range(6):
            lz = 0.25 + k * 0.036
            objs.append(H.box(f"CabLouver_{side}_{k}", (fx - 0.016, dy, lz), (0.014, door_w * 0.62, 0.012),
                              "cab_cream_mint", bevel=0.002, rot=(0, math.radians(-28), 0)))
            objs.append(H.box(f"CabLouverSlot_{side}_{k}", (fx - 0.013, dy, lz - 0.006), (0.005, door_w * 0.60, 0.008), "plastic_black", bevel=0.0))

        hy = side * 0.068
        for hz0, hz1 in ((1.14, 1.32), (0.78, 0.94)):
            objs.append(H.pipe(f"CabHandle_{side}_{hz0:.2f}", [
                (fx - 0.012, hy, hz0), (fx - 0.044, hy, hz0),
                (fx - 0.044, hy, hz1), (fx - 0.012, hy, hz1)
            ], 0.007, "satin_inox", res=3))

    objs.append(H.cyl("CabLockUp", (fx - 0.010, 0.032, 1.23), (fx - 0.022, 0.032, 1.23), 0.011, "brass", seg=14))
    objs.append(H.cyl("CabLockLow", (fx - 0.010, 0.032, 0.86), (fx - 0.022, 0.032, 0.86), 0.011, "brass", seg=14))
    return objs


def build_ward_sink():
    """Hero 1987 Vietnamese hospital wall-hung ceramic handwashing station for Room 101 East Wall!
    Back against wall Y=0, protrudes toward -Y (so at yaw=+90f on East wall x=13.70 it faces -X into Room 101!).
    Physically grounds the existing 'Broken capillary line from Ward 101 basin' decal at (13.64, 0.44, 6.57)!"""
    objs = []

    # 1. Glazed 15x15cm white ceramic tile splashback (1.05m wide x 0.96m tall, Z = 0.72..1.68)
    objs.append(H.box("TileSplashback", (0.0, -0.010, 1.20), (1.06, 0.020, 0.96), "ceramic_tile", bevel=0.003))
    # Molded white tile edge trim border
    objs.append(H.box("TileCapTop", (0.0, -0.016, 1.686), (1.08, 0.028, 0.022), "ceramic", bevel=0.004))

    # 2. Wall-hung vitreous china hospital basin (0.56w x 0.44d, rim at Z=0.84m!) on twin painted iron wall brackets
    for bx in (-0.20, 0.20):
        objs.append(H.box(f"BracketWall_{bx:.2f}", (bx, -0.016, 0.70), (0.036, 0.016, 0.24), "bed_enamel_green", bevel=0.003))
        objs.append(H.box(f"BracketArm_{bx:.2f}", (bx, -0.18, 0.765), (0.032, 0.34, 0.028), "bed_enamel_green", bevel=0.003))
        objs.append(H.cyl(f"BracketStrut_{bx:.2f}", (bx, -0.024, 0.60), (bx, -0.30, 0.755), 0.010, "bed_enamel_green", seg=10))

    # Sculpted curved ceramic washbasin bowl (outer body + recessed inner basin + rolled splash ledge + drain ring)
    objs.append(H.box("BasinBackLedge", (0.0, -0.068, 0.855), (0.56, 0.11, 0.095), "ceramic", bevel=0.016, seg=3))
    # Outer basin hull & recessed bowl built via parametric dish mesh
    def basin_outer(u, v):
        ang = u * math.tau
        r = 0.09 + v * 0.175
        rx = math.cos(ang) * (r * 1.04)
        ry = -0.235 + math.sin(ang) * (r * 0.76)
        z = 0.675 + (v ** 1.35) * 0.165
        return (rx, ry, z)

    objs.append(H.sculpted_grid("BasinBowl", 28, 12, basin_outer, "ceramic", thickness=0.024))
    # Basin rim lip ring
    rim_pts = [(math.cos(k * math.tau / 28) * 0.275, -0.235 + math.sin(k * math.tau / 28) * 0.202, 0.842) for k in range(28)]
    objs.append(H.pipe("BasinRim", rim_pts, 0.020, "ceramic", res=5, cyclic=True))
    objs.append(H.cyl("DrainRing", (0.0, -0.235, 0.688), (0.0, -0.235, 0.698), 0.028, "satin_inox", seg=16))
    objs.append(H.cyl("DrainHole", (0.0, -0.235, 0.690), (0.0, -0.235, 0.700), 0.019, "plastic_black", seg=14))

    # 3. Exposed galvanized/corroded S/P-trap drainpipe + cold water wall supply pipe & shutoff valve!
    objs.append(H.pipe("PTrapPipe", [
        (0.0, -0.235, 0.68), (0.0, -0.235, 0.50), (0.0, -0.205, 0.455),
        (0.0, -0.150, 0.455), (0.0, -0.125, 0.515), (0.0, -0.015, 0.515)
    ], 0.018, "copper_rust", res=5))
    objs.append(H.cyl("DrainFlange", (0.0, -0.008, 0.515), (0.0, -0.026, 0.515), 0.036, "copper_rust", seg=14))
    # Cold-water copper/galvanized supply riser & brass shutoff wheel terminating cleanly UNDER ceramic ledge (Z=0.80)!
    objs.append(H.pipe("SupplyPipe", [
        (-0.14, -0.045, 0.02), (-0.14, -0.045, 0.62), (-0.08, -0.045, 0.68), (-0.06, -0.045, 0.79)
    ], 0.009, "copper_rust", res=4))
    objs.append(H.cyl("ShutoffValve", (-0.14, -0.045, 0.58), (-0.14, -0.092, 0.58), 0.011, "brass", seg=12))
    objs.append(H.cyl("ShutoffWheel", (-0.14, -0.086, 0.58), (-0.14, -0.098, 0.58), 0.024, "plastic_red", seg=14))

    # 4. Classic Vietnamese wall-mounted swan-neck hospital faucet + cross-handle tap!
    objs.append(H.cyl("FaucetEscutcheon", (0.0, -0.016, 1.00), (0.0, -0.034, 1.00), 0.028, "satin_inox", seg=16))
    objs.append(H.pipe("FaucetSpout", [
        (0.0, -0.025, 1.00), (0.0, -0.085, 1.00), (0.0, -0.145, 1.04),
        (0.0, -0.195, 1.02), (0.0, -0.205, 0.955)
    ], 0.011, "satin_inox", res=5))
    objs.append(H.cyl("FaucetStem", (0.0, -0.068, 1.00), (0.0, -0.068, 1.055), 0.009, "brass", seg=12))
    objs.append(H.cyl("TapCrossA", (-0.036, -0.068, 1.052), (0.036, -0.068, 1.052), 0.006, "satin_inox", seg=10))
    objs.append(H.cyl("TapCrossB", (0.0, -0.104, 1.052), (0.0, -0.032, 1.052), 0.006, "satin_inox", seg=10))

    # 5. Recessed ceramic soap dish + worn hospital soap bar + glass/porcelain shelf under mirror
    objs.append(H.box("SoapDish", (-0.28, -0.062, 0.99), (0.14, 0.092, 0.024), "ceramic", bevel=0.008))
    objs.append(H.box("SoapBar", (-0.28, -0.066, 1.012), (0.082, 0.052, 0.022), "plastic_ivory", bevel=0.009, seg=3, rot=(0, 0, 0.14)))

    # 6. Wall-mounted wood/aluminum-framed hospital mirror (Z = 1.18..1.76) with desilvered foxed edges in R!
    objs.append(H.box("MirrorFrame", (0.0, -0.022, 1.46), (0.48, 0.026, 0.60), "satin_inox", bevel=0.005))
    objs.append(H.box("MirrorGlass", (0.0, -0.032, 1.46), (0.44, 0.008, 0.56), "mirror_aged", bevel=0.001))
    objs.append(H.box("MirrorShelf", (0.0, -0.072, 1.14), (0.46, 0.11, 0.016), "satin_inox", bevel=0.003))
    # Small antiseptic soap bottle sitting on mirror shelf
    objs.append(H.lathe("HandSoapBottle", [
        (0, 0), (0.026, 0), (0.028, 0.008), (0.028, 0.105), (0.014, 0.128), (0.014, 0.145), (0, 0.145)
    ], (0.13, -0.070, 1.148), "amber_glass", seg=14))

    # 7. Side wall stainless hand-towel bar (+X side of basin) + CLOTH-SIMULATED hanging cloth towel!
    tx0, tx1, ty, tz = 0.34, 0.62, -0.075, 1.04
    objs.append(H.pipe("SinkTowelBar", [
        (tx0, -0.014, tz), (tx0, ty, tz), (tx1, ty, tz), (tx1, -0.014, tz)
    ], 0.007, "satin_inox", res=3))
    col_bar = H.cyl("__SinkColBar", (tx0, ty, tz), (tx1, ty, tz), 0.012, "satin_inox", seg=10)
    col_wall = H.box("__SinkColWall", (0.48, 0.01, 0.90), (0.50, 0.04, 0.90), "ceramic_tile", bevel=0.0)

    def sink_towel_init(u, v):
        x = 0.48 + (v - 0.5) * 0.21
        if abs(u - 0.5) < 0.14:
            ang = (u - 0.5) * math.pi * 3.0
            y = ty - math.sin(ang) * 0.015
            z = tz + 0.016 * math.cos(ang)
        else:
            side = -1.0 if u > 0.5 else 1.0
            drop = (abs(u - 0.5) - 0.14) / 0.36
            y = ty + side * (0.015 + 0.005 * math.sin(v * 8.0))
            z = tz - drop * (0.28 if side < 0 else 0.22)
        return (x, y, z)

    stowel = W.create_cloth_grid("SinkHandTowel", 20, 14, sink_towel_init, "towel_terry",
                                 pin_fn=lambda u, v: (0.44 < u < 0.56))
    W.simulate_cloth_drape(
        stowel, [col_bar, col_wall], frames=18, mass=0.28, tension=16.0, bending=0.35,
        collision_dist=0.005, solidify_thickness=0.005, cleanup_colliders=True,
    )
    objs.append(stowel)
    return objs


def build_ward_stool():
    """Classic Vietnamese round tubular hospital attendant stool (ghế đẩu inox/sắt sơn xanh, Ø0.30m x H 0.48m)."""
    objs = []
    # Contoured spun brushed-stainless steel round seat pan with rolled edge
    objs.append(H.lathe("StoolSeat", [
        (0.0, 0.480), (0.115, 0.478), (0.145, 0.482), (0.154, 0.474),
        (0.154, 0.452), (0.144, 0.448), (0.140, 0.466), (0.0, 0.466)
    ], (0, 0, 0), "satin_inox", seg=28))
    # 4 Splayed painted tubular steel legs + rubber tips + circular footrest ring
    for i in range(4):
        ang = math.radians(45.0 + i * 90.0)
        ca, sa = math.cos(ang), math.sin(ang)
        objs.append(H.cyl(f"StoolLeg_{i}", (ca * 0.165, sa * 0.165, 0.022), (ca * 0.115, sa * 0.115, 0.466), 0.011, "bed_enamel_green", seg=12))
        objs.append(H.cyl(f"StoolTip_{i}", (ca * 0.168, sa * 0.168, 0.0), (ca * 0.164, sa * 0.164, 0.028), 0.014, "rubber", seg=12))
    ring_pts = [(math.cos(k * math.tau / 24) * 0.146, math.sin(k * math.tau / 24) * 0.146, 0.175) for k in range(24)]
    objs.append(H.pipe("FootRing", ring_pts, 0.0085, "satin_inox", res=4, cyclic=True))
    return objs


def build_ward_arch():
    """Room 101 1987 structural concrete ceiling beams, chamfered pilasters & continuous galvanized
    surface electrical conduit trunking dropping cleanly to all 4 bed outlets & ceiling fixtures!
    Origin at Room 101 center (9.70, 0.0, 5.00) in Unity -> local X in [-4.0..+4.0], Y in [-3.0..+3.0], Z in [0..3.60]!"""
    objs = []
    # Note: zero coplanar face intersections! Step each perpendicular beam cleanly by 4mm.

    # 1. Central structural transverse concrete beam (Unity x=9.70 -> local X=0, spans Y in [-2.88..+2.88])
    objs.append(H.box("CenterBeamMain", (0.0, 0.0, 3.40), (0.34, 5.74, 0.39), "concrete_beam", bevel=0.016, seg=2))
    objs.append(H.box("CenterBeamCap", (0.0, 0.0, 3.205), (0.36, 5.76, 0.032), "concrete_beam", bevel=0.008))

    # 2. North & South perimeter drop ring beams (Y = +/-2.89, X in [-3.98..+3.98])
    for sy, tag in ((2.89, "North"), (-2.89, "South")):
        objs.append(H.box(f"RingBeam_{tag}", (0.0, sy, 3.455), (7.96, 0.22, 0.28), "concrete_beam", bevel=0.014, seg=2))
    # West & East subtle perimeter cornice ledges (X = +/-3.90, stepped clear of North/South beams)
    for sx, tag in ((-3.91, "West"), (3.91, "East")):
        objs.append(H.box(f"Cornice_{tag}", (sx, 0.0, 3.49), (0.18, 5.54, 0.21), "concrete_beam", bevel=0.012, seg=2))

    # 3. Structural concrete pilasters under central beam ends (North wall at X=0, Y=+2.91; South wall at X=0, Y=-2.91)
    # Wait: South wall has WardRulesBoard at x=9.65 (local X=-0.05), so keep South center clear and place pilasters at X=-3.90 / +3.90 corners & North mid-pier x=8.83 (local X=-0.87)!
    for px, py, tag in ((-3.91, 2.91, "NW"), (3.91, 2.91, "NE"), (-3.91, -2.91, "SW"), (3.91, -2.91, "SE")):
        objs.append(H.box(f"PilDado_{tag}", (px, py, 0.625), (0.18, 0.18, 1.05), "dado_pilaster", bevel=0.010))
        objs.append(H.box(f"PilUpper_{tag}", (px, py, 2.26), (0.18, 0.18, 2.18), "concrete_beam", bevel=0.010))
        objs.append(H.box(f"PilBase_{tag}", (px, py, 0.05), (0.20, 0.20, 0.10), "terrazzo_dark", bevel=0.006))

    # 4. Continuous galvanized surface electrical conduit runs along ceiling & dropping cleanly to permanent Outlets 1–3!
    # NOTE (DEC-V361-CH01-01): Bed 4's south-wall conduit drop lives in separate R-only asset CTCC_LOC03_Bed4ConduitR!
    objs.append(H.pipe("CeilingTrunkW", [(-3.82, 0.0, 3.57), (-0.22, 0.0, 3.57), (-0.18, 0.0, 3.175), (0.18, 0.0, 3.175), (0.22, 0.0, 3.57), (3.82, 0.0, 3.57)], 0.011, "galv_conduit", res=4))
    # Branch ceiling conduit lines cleanly connecting West T8 (local -2.0, +1.55) and East Bed-4 T8 (local +1.95, -1.55)!
    objs.append(H.cyl("BranchTubeW", (-2.00, 0.0, 3.57), (-2.00, 1.55, 3.57), 0.009, "galv_conduit", seg=8))
    objs.append(H.cyl("BranchTubeE", (1.95, 0.0, 3.57), (1.95, -1.55, 3.57), 0.009, "galv_conduit", seg=8))
    for fx, fy in ((-2.00, 0.0), (1.95, 0.0), (-2.00, 1.55), (1.95, -1.55)):
        objs.append(H.box(f"JBox_{fx:.1f}_{fy:.1f}", (fx, fy, 3.565), (0.095, 0.095, 0.045), "galv_conduit", bevel=0.004))

    # South wall ceiling conduit header + permanent drop to Bed 3 ONLY (x=7.90 -> local X=-1.80, Z=1.35)
    objs.append(H.cyl("SouthHeaderConduit", (-3.75, -2.975, 3.24), (3.75, -2.975, 3.24), 0.010, "galv_conduit", seg=10))
    objs.append(H.cyl("DropConduit_Bed3", (-1.80, -2.975, 1.35), (-1.80, -2.975, 3.24), 0.0095, "galv_conduit", seg=10))
    objs.append(H.box("DropJBox_Bed3", (-1.80, -2.968, 3.24), (0.082, 0.036, 0.072), "galv_conduit", bevel=0.004))
    for cz in (1.75, 2.35, 2.90):
        objs.append(H.box(f"Clamp_Bed3_{cz:.1f}", (-1.80, -2.974, cz), (0.036, 0.024, 0.016), "satin_inox", bevel=0.002))

    # North wall conduit horizontal line at dado height (Z = 0.92, Y = +2.975) connecting Bed 1 & Bed 2
    objs.append(H.cyl("NorthDadoConduit", (-3.75, 2.975, 0.92), (3.75, 2.975, 0.92), 0.009, "galv_conduit", seg=10))
    for cx in (-3.20, -1.60, -0.50, 1.60, 2.80):
        objs.append(H.box(f"NorthClamp_{cx:.1f}", (cx, 2.974, 0.92), (0.016, 0.024, 0.034), "satin_inox", bevel=0.002))
    return objs


def build_bed4_conduit_r():
    """R-only galvanized vertical conduit drop to Bed 4 physical WallOutlet #4 (DEC-V361-CH01-01 canon purity!).
    Placed at Room 101 center (9.70, 0, 5.00) on real (Layer R) so Layer B Bed 4 wall stays spotless!"""
    objs = []
    ox = 1.85  # Unity x=11.55 -> local +1.85
    objs.append(H.cyl("DropConduit_Bed4", (ox, -2.975, 1.35), (ox, -2.975, 3.24), 0.0095, "galv_conduit", seg=10))
    objs.append(H.box("DropJBox_Bed4", (ox, -2.968, 3.24), (0.082, 0.036, 0.072), "galv_conduit", bevel=0.004))
    for cz in (1.75, 2.35, 2.90):
        objs.append(H.box(f"Clamp_Bed4_{cz:.1f}", (ox, -2.974, cz), (0.036, 0.024, 0.016), "satin_inox", bevel=0.002))
    return objs


def _build_steel_window(width: float):
    """Upgraded 1987 Vietnamese rolled-steel casement window with oil-painted green ironwork,
    welded diamond hoa sắt security grille, brass latches, frosted louver blades & honed terrazzo sill!"""
    objs = []
    sill_z, top_z, transom_z = 1.09, 2.94, 2.40
    h = top_z - sill_z
    wall_t = 0.24
    frame_y, frame_d, frame_w = 0.015, 0.072, 0.044

    # Beveled terrazzo sill (protrudes inward -Y by 4.5 cm) + exterior drip hood
    objs.append(H.box("TerrazzoSill", (0, -0.008, sill_z + 0.028), (width + 0.08, wall_t + 0.09, 0.056), "terrazzo", bevel=0.007))
    objs.append(H.box("ExteriorRainHood", (0, 0.24, top_z + 0.04), (width + 0.22, 0.42, 0.085), "concrete_beam", bevel=0.010, rot=(math.radians(-6), 0, 0)))

    # Outer rolled-steel perimeter frame
    objs.append(H.box("FrameBottom", (0, frame_y, sill_z + 0.068), (width, frame_d, frame_w), "window_green", bevel=0.004))
    objs.append(H.box("FrameTop", (0, frame_y, top_z - 0.022), (width, frame_d, frame_w), "window_green", bevel=0.004))
    objs.append(H.box("TransomBar", (0, frame_y, transom_z), (width, frame_d + 0.012, 0.052), "window_green", bevel=0.004))
    for s in (-1, 1):
        objs.append(H.box(f"FrameSide_{s}", (s * (width * 0.5 - frame_w * 0.5), frame_y, (sill_z + top_z) * 0.5),
                          (frame_w, frame_d, h - 0.04), "window_green", bevel=0.004))

    bays = 3 if width > 1.8 else 2
    bay_w = (width - frame_w * 2) / bays
    for i in range(1, bays):
        mx = -width * 0.5 + frame_w + i * bay_w
        # Step mullion depth 4mm narrower than outer frame so Cycles AO has zero coplanar cross-joint black dots!
        objs.append(H.box(f"Mullion_{i}", (mx, frame_y, (sill_z + 0.06 + top_z) * 0.5), (0.040, frame_d - 0.006, h - 0.085), "window_green", bevel=0.003))

    lower_mid_z = (sill_z + 0.08 + transom_z) * 0.5
    lower_h = transom_z - (sill_z + 0.09)
    for b in range(bays):
        bx = -width * 0.5 + frame_w + (b + 0.5) * bay_w
        sw = bay_w - 0.026
        for sx in (-1, 1):
            objs.append(H.box(f"SashV_{b}_{sx}", (bx + sx * (sw * 0.5 - 0.012), frame_y - 0.012, lower_mid_z),
                              (0.024, 0.036, lower_h - 0.02), "bed_enamel_green", bevel=0.003))
        for sz in (sill_z + 0.10, transom_z - 0.035, sill_z + 0.09 + lower_h * 0.45):
            objs.append(H.box(f"SashH_{b}_{sz:.2f}", (bx, frame_y - 0.012, sz), (sw, 0.034, 0.024), "bed_enamel_green", bevel=0.002))
        # Aged brass casement latch
        objs.append(H.box(f"LatchBase_{b}", (bx + sw * 0.45, frame_y - 0.034, lower_mid_z - 0.08), (0.018, 0.020, 0.065), "brass", bevel=0.002))
        objs.append(H.pipe(f"LatchHandle_{b}", [
            (bx + sw * 0.45, frame_y - 0.044, lower_mid_z - 0.08),
            (bx + sw * 0.45, frame_y - 0.072, lower_mid_z - 0.08),
            (bx + sw * 0.45, frame_y - 0.072, lower_mid_z - 0.16)
        ], 0.006, "brass", res=3))

    # 6 Tilted frosted transom glass louvers + aluminum side clips
    for k in range(6):
        lz = transom_z + 0.055 + k * 0.078
        objs.append(H.box(f"LouverBlade_{k}", (0, frame_y, lz), (width - 0.09, 0.086, 0.010), "FrostedGlass",
                          bevel=0.001, rot=(math.radians(34), 0, 0)))
        for b in range(bays):
            bx = -width * 0.5 + frame_w + (b + 0.5) * bay_w
            for sx in (-1, 1):
                objs.append(H.box(f"LouverClip_{k}_{b}_{sx}", (bx + sx * (bay_w * 0.5 - 0.016), frame_y, lz),
                                  (0.014, 0.068, 0.020), "satin_inox", bevel=0.001, rot=(math.radians(34), 0, 0)))

    # Welded Vietnamese geometric iron security grille (hoa sắt) in readable bottle-green oil paint!
    grille_y = frame_y - 0.062
    bar_r = 0.0075
    for gz in (sill_z + 0.24, sill_z + 0.68, sill_z + 1.12, transom_z - 0.04):
        objs.append(H.cyl(f"GrilleH_{gz:.2f}", (-width * 0.5 + 0.02, grille_y, gz),
                          (width * 0.5 - 0.02, grille_y, gz), bar_r, "window_green", seg=8))
    n_bars = max(6, int(round((width - 0.12) / 0.165)))
    for k in range(n_bars + 1):
        gx = -width * 0.5 + 0.06 + (width - 0.12) * k / n_bars
        objs.append(H.cyl(f"GrilleV_{k}", (gx, grille_y, sill_z + 0.06), (gx, grille_y, transom_z - 0.02), bar_r, "window_green", seg=8))
    for k in range(n_bars):
        x0 = -width * 0.5 + 0.06 + (width - 0.12) * k / n_bars
        x1 = -width * 0.5 + 0.06 + (width - 0.12) * (k + 1) / n_bars
        xm, zm = (x0 + x1) * 0.5, sill_z + 0.90
        dh, dw = 0.11, (x1 - x0) * 0.44
        pts = [(xm, grille_y, zm + dh), (xm + dw, grille_y, zm), (xm, grille_y, zm - dh), (xm - dw, grille_y, zm)]
        objs.append(H.pipe(f"GrilleDiamond_{k}", pts, 0.0055, "window_green", res=3, cyclic=True))
    return objs


def build_ward_door_frame():
    """Hero varnished Vietnamese ironwood ward door frame + louvered transom (matching LOC02 S1Door!)."""
    objs = []
    w, h, wall_t = 1.20, 2.22, 0.24
    jamb_w, transom_h = 0.068, 0.62

    for s in (-1, 1):
        jx = s * (w * 0.5 + jamb_w * 0.5)
        objs.append(H.box(f"JambCore_{s}", (jx, 0, (h + transom_h) * 0.5), (jamb_w, wall_t, h + transom_h), "wood", bevel=0.005, grain="Z"))
        objs.append(H.box(f"DoorStop_{s}", (s * (w * 0.5 + 0.009), -0.025, h * 0.5), (0.018, 0.028, h), "wood_light", bevel=0.003, grain="Z"))
        for side_y in (-1, 1):
            objs.append(H.box(f"Architrave_{s}_{side_y}", (s * (w * 0.5 + 0.052), side_y * (wall_t * 0.5 + 0.011), (h + transom_h + 0.07) * 0.5),
                              (0.088, 0.022, h + transom_h + 0.07), "wood_light", bevel=0.006, grain="Z"))
        objs.append(H.box(f"JambGuard_{s}", (s * (w * 0.5 + 0.036), 0, 0.14), (0.076, wall_t + 0.008, 0.28), "satin_inox", bevel=0.003))

    objs.append(H.box("HeaderBeam", (0, 0, h + 0.035), (w + 0.004, wall_t - 0.006, 0.070), "wood", bevel=0.005, grain="X"))
    objs.append(H.box("TopHead", (0, 0, h + transom_h + 0.032), (w + jamb_w * 2 + 0.04, wall_t + 0.028, 0.064), "wood_light", bevel=0.006, grain="X"))
    for side_y in (-1, 1):
        objs.append(H.box(f"HeaderTrim_{side_y}", (0, side_y * (wall_t * 0.5 + 0.011), h + transom_h + 0.052),
                          (w + 0.19, 0.022, 0.085), "wood_light", bevel=0.006, grain="X"))

    for sx in (-0.38, 0.0, 0.38):
        objs.append(H.box(f"TransomMullion_{sx:.2f}", (sx, 0, h + 0.07 + (transom_h - 0.07) * 0.5),
                          (0.032, 0.075, transom_h - 0.07), "wood", bevel=0.003, grain="Z"))
    for k in range(5):
        lz = h + 0.13 + k * 0.095
        objs.append(H.box(f"TransomBlade_{k}", (0, 0, lz), (w - 0.03, 0.085, 0.012), "FrostedGlass",
                          bevel=0.002, rot=(math.radians(32), 0, 0)))

    objs.append(H.box("ThresholdBase", (0, 0, 0.009), (w + 0.02, 0.145, 0.018), "satin_inox", bevel=0.006))
    for k in range(-2, 3):
        objs.append(H.box(f"ThresholdRib_{k}", (0, k * 0.024, 0.0185), (w - 0.02, 0.006, 0.003), "satin_inox", bevel=0.001))
    for k, hz in enumerate((0.28, 1.08, 1.88)):
        objs.append(H.cyl(f"FrameHinge_{k}", (-w * 0.5 + 0.008, -0.048, hz - 0.055),
                          (-w * 0.5 + 0.008, -0.048, hz + 0.055), 0.011, "brass", seg=12))
    return objs


def build_ward_door_leaf():
    """Hero varnished Vietnamese ironwood ward door leaf (1.18 x 2.20 m, hinge X=0, extends +X)."""
    objs = []
    w, h, t = 1.18, 2.20, 0.048
    stile = 0.125
    inner_w = w - stile * 2

    objs.append(H.box("HingeStile", (stile * 0.5, 0, h * 0.5), (stile, t, h), "wood", bevel=0.004, grain="Z"))
    objs.append(H.box("LatchStile", (w - stile * 0.5, 0, h * 0.5), (stile, t, h), "wood", bevel=0.004, grain="Z"))
    objs.append(H.box("BottomRail", (w * 0.5, 0, 0.15), (inner_w + 0.01, t, 0.30), "wood", bevel=0.004, grain="X"))
    objs.append(H.box("MidRail", (w * 0.5, 0, 1.08), (inner_w + 0.01, t, 0.16), "wood", bevel=0.004, grain="X"))
    objs.append(H.box("TopRail", (w * 0.5, 0, h - 0.09), (inner_w + 0.01, t, 0.18), "wood", bevel=0.004, grain="X"))

    objs.append(H.box("MidMullionLow", (w * 0.5, 0, 0.65), (0.085, t * 0.92, 0.72), "wood", bevel=0.004, grain="Z"))
    for px in (stile + inner_w * 0.25, stile + inner_w * 0.75):
        pw = inner_w * 0.44
        objs.append(H.box(f"LowerPanel_{px:.2f}", (px, 0, 0.65), (pw, t * 0.45, 0.66), "wood", bevel=0.003, grain="Z"))
        objs.append(H.box(f"LowerPanelRaise_{px:.2f}", (px, 0, 0.65), (pw - 0.05, t * 0.72, 0.56), "wood_light", bevel=0.010, grain="Z"))

    objs.append(H.box("ObsGlass", (w * 0.5, 0, 1.59), (inner_w - 0.02, 0.010, 0.86), "FrostedGlass", bevel=0.0))
    objs.append(H.box("GlassMuntinH", (w * 0.5, 0, 1.59), (inner_w, 0.032, 0.030), "wood_light", bevel=0.003, grain="X"))
    objs.append(H.box("GlassMuntinV", (w * 0.5, 0, 1.59), (0.030, 0.026, 0.88), "wood_light", bevel=0.003, grain="Z"))
    for s in (-1, 1):
        objs.append(H.box(f"KickPlate_{s}", (w * 0.5, s * (t * 0.5 + 0.002), 0.165), (w - 0.06, 0.004, 0.29), "satin_inox", bevel=0.001))

    hx, hz = w - 0.095, 1.02
    objs.append(H.box("LatchFaceplate", (w + 0.001, 0, hz), (0.004, 0.024, 0.18), "satin_inox", bevel=0.001))
    objs.append(H.box("LatchBolt", (w + 0.008, 0, hz + 0.025), (0.014, 0.014, 0.024), "brass", bevel=0.002))
    for s in (-1, 1):
        ey = s * (t * 0.5 + 0.004)
        objs.append(H.box(f"Escutcheon_{s}", (hx, ey, hz - 0.025), (0.048, 0.008, 0.215), "satin_inox", bevel=0.003))
        objs.append(H.cyl(f"KeyCylinder_{s}", (hx, ey, hz - 0.085), (hx, ey + s * 0.006, hz - 0.085), 0.012, "brass", seg=12))
        objs.append(H.pipe(f"LeverHandle_{s}", [
            (hx, ey, hz), (hx, ey + s * 0.056, hz),
            (hx - 0.132, ey + s * 0.056, hz), (hx - 0.132, ey + s * 0.014, hz)
        ], 0.010, "satin_inox", res=4))

    objs.append(H.box("CloserBody", (0.28, -t * 0.5 - 0.028, h - 0.085), (0.21, 0.046, 0.054), "satin_inox", bevel=0.005))
    objs.append(H.pipe("CloserArm", [(0.22, -t * 0.5 - 0.03, h - 0.055), (0.34, -t * 0.5 - 0.16, h + 0.01),
                                     (0.42, -t * 0.5 - 0.02, h + 0.03)], 0.008, "satin_inox", res=3))
    for k, hz_p in enumerate((0.28, 1.08, 1.88)):
        objs.append(H.box(f"HingeLeaf_{k}", (0.025, -t * 0.5 - 0.002, hz_p), (0.048, 0.005, 0.10), "brass", bevel=0.001))
    return objs


def build_fluorescent_fixture():
    """Cycles-baked 1.24 m Vietnamese dual T8 fluorescent batten fixture (2 draw calls: housing + tubes!)."""
    objs = []
    objs.append(H.box("BallastBase", (0, 0, -0.024), (0.145, 1.25, 0.048), "enamel_white", bevel=0.006))
    for s in (-1, 1):
        objs.append(H.box(f"ReflectorWing_{s}", (s * 0.095, 0, -0.054), (0.078, 1.25, 0.007), "enamel_white",
                          bevel=0.002, rot=(0, s * math.radians(24), 0)))
    for sy in (-1, 1):
        objs.append(H.box(f"EndCap_{sy}", (0, sy * 0.625, -0.044), (0.195, 0.014, 0.072), "enamel_white", bevel=0.003))
        for sx in (-0.048, 0.048):
            objs.append(H.box(f"Tombstone_{sy}_{sx:.2f}", (sx, sy * 0.605, -0.072), (0.026, 0.022, 0.048), "plastic_ivory", bevel=0.003))
    objs.append(H.cyl("StarterCan", (0.074, 0.42, -0.035), (0.096, 0.42, -0.035), 0.010, "plastic_ivory", seg=12))
    for i, sx in enumerate((-0.048, 0.048)):
        objs.append(H.cyl(f"T8Tube_{i}", (sx, -0.585, -0.082), (sx, 0.585, -0.082), 0.0135, "DeadTube", seg=16))
        for sy in (-1, 1):
            objs.append(H.cyl(f"TubeCollar_{i}_{sy}", (sx, sy * 0.585, -0.082), (sx, sy * 0.600, -0.082), 0.0142, "satin_inox", seg=14))
    return objs


def build_wall_outlet():
    """Clean 1987 Vietnamese surface Bakelite double socket + red rocker switch + brass screws + conduit entry gland.
    NO awkward dangling 40cm black stick (continuous conduits live in CTCC_LOC03_WardArch!)."""
    objs = []
    objs.append(H.box("OutletBackBox", (0, -0.018, 0), (0.148, 0.036, 0.096), "plastic_ivory", bevel=0.005))
    objs.append(H.box("OutletFaceplate", (0, -0.038, 0), (0.154, 0.010, 0.104), "plastic_ivory", bevel=0.004))
    for sx in (-0.042, 0.004):
        objs.append(H.cyl(f"SocketWell_{sx:.3f}", (sx, -0.041, 0), (sx, -0.045, 0), 0.017, "plastic_black", seg=16))
        for pin in (-0.008, 0.008):
            objs.append(H.cyl(f"PinHole_{sx:.3f}_{pin:.3f}", (sx + pin, -0.043, 0), (sx + pin, -0.048, 0), 0.0032, "brass", seg=8))
    objs.append(H.box("SwitchBezel", (0.048, -0.044, 0), (0.025, 0.008, 0.044), "plastic_black", bevel=0.002))
    objs.append(H.box("SwitchRocker", (0.048, -0.050, 0.002), (0.018, 0.014, 0.032), "plastic_red", bevel=0.002, rot=(math.radians(14), 0, 0)))
    # Compact conduit gland collar at top edge (meets vertical conduit drop cleanly!)
    objs.append(H.cyl("ConduitGland", (0, -0.018, 0.044), (0, -0.018, 0.072), 0.013, "galv_conduit", seg=12))
    for sx in (-0.064, 0.064):
        for sz in (-0.038, 0.038):
            objs.append(H.cyl(f"Screw_{sx:.2f}_{sz:.2f}", (sx, -0.041, sz), (sx, -0.046, sz), 0.003, "brass", seg=8))
    return objs


def run_all():
    W.export_ward_asset("CTCC_PROP_BedsideCabinet", build_bedside_cabinet,
                        {"LOC03_BedsideCab": {"res": 1024, "bake_r": True}}, make_lods=True, cam_dir=(1.25, -1.65, 0.80), ao_samples=10)
    W.export_ward_asset("CTCC_LOC03_Bed3Drawer", build_bed3_drawer,
                        {"LOC03_Bed3Drawer": {"res": 1024, "bake_r": False}}, make_lods=False, cam_dir=(1.10, -1.60, 0.85), ao_samples=8)
    W.export_ward_asset("CTCC_PROP_MedicalCabinet", build_medical_cabinet,
                        {"LOC03_MedCabinet": {"res": 2048, "bake_r": True}}, make_lods=True, cam_dir=(-1.80, -1.25, 0.55), ao_samples=10)
    W.export_ward_asset("CTCC_LOC03_WardSink", build_ward_sink,
                        {"LOC03_WardSink": {"res": 1024, "bake_r": True}}, make_lods=True, cam_dir=(1.15, -1.85, 0.55), ao_samples=10)
    W.export_ward_asset("CTCC_LOC03_WardStool", build_ward_stool,
                        {"LOC03_WardStool": {"res": 1024, "bake_r": True}}, make_lods=True, cam_dir=(1.35, -1.55, 0.85), ao_samples=8)
    W.export_ward_asset("CTCC_LOC03_WardArch", build_ward_arch,
                        {"LOC03_WardArch": {"res": 1024, "bake_r": True}}, make_lods=False, cam_dir=(1.40, -1.55, -0.45), ao_samples=8)
    W.export_ward_asset("CTCC_LOC03_Bed4ConduitR", build_bed4_conduit_r,
                        {"LOC03_Bed4Conduit": {"res": 512, "bake_r": True}}, make_lods=False, render=False, ao_samples=8)
    W.export_ward_asset("CTCC_ENV_SteelWindow_Wide", lambda: _build_steel_window(2.14),
                        {"LOC03_WindowWide": {"res": 1024, "bake_r": True}}, make_lods=True, cam_dir=(0.85, -2.15, 0.30), ao_samples=8)
    W.export_ward_asset("CTCC_ENV_SteelWindow_Mid", lambda: _build_steel_window(2.09),
                        {"LOC03_WindowMid": {"res": 1024, "bake_r": True}}, make_lods=True, render=False, ao_samples=8)
    W.export_ward_asset("CTCC_ENV_SteelWindow_Narrow", lambda: _build_steel_window(1.28),
                        {"LOC03_WindowNarrow": {"res": 1024, "bake_r": True}}, make_lods=True, render=False, ao_samples=8)
    W.export_ward_asset("CTCC_ENV_WardDoorFrame", build_ward_door_frame,
                        {"LOC03_WardDoorFrame": {"res": 1024, "bake_r": True}}, make_lods=True, cam_dir=(1.15, -2.05, 0.45), ao_samples=8)
    W.export_ward_asset("CTCC_ENV_WardDoorLeaf", build_ward_door_leaf,
                        {"LOC03_WardDoorLeaf": {"res": 1024, "bake_r": True}}, make_lods=True, cam_dir=(1.15, -2.05, 0.40), ao_samples=8)
    W.export_ward_asset("CTCC_ENV_FluorescentFixture", build_fluorescent_fixture,
                        {"LOC03_TubeFixture": {"res": 1024, "bake_r": True}}, make_lods=True, cam_dir=(1.20, -1.50, -0.75), ao_samples=8)
    W.export_ward_asset("CTCC_ENV_WallOutlet", build_wall_outlet,
                        {"LOC03_WallOutlet": {"res": 512, "bake_r": True}}, make_lods=False, cam_dir=(0.75, -1.85, 0.45), ao_samples=8)


if __name__ == "__main__":
    run_all()
