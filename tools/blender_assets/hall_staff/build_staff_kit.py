"""Kit B: Reusable Nursing & Staff Workstation Kit for STAFF Corridor (Blender 4.5 LTS).

Produces Cycles-baked B (2005 operational) / R (2012 abandoned) PBR assets:
  1. CTCC_PROP_NursingFilingDesk  - Completely redesigned Hero 1987 Vietnamese hospital nursing
                                    workstation (1.20 x 0.35 x 1.50m) with:
                                    * Exact recessed drawer cavity at Blender X=-0.27, Z=0.77
                                      (matches world nurseDrawerRoot (0.90, 0.77, 3.92) to the mm!)
                                    * Worn forearm/wrist polish zone at Blender X=+0.28 (world z=3.37,
                                      matching CH01_NURSE_DESK!) with open shift logbook, rubber stamp,
                                      ink pad, kidney tray, thermometer jar & ceramic pen cup pushed neatly
                                      to the side ("Giấy được xếp dồn bên kia cho khỏi vướng").
                                    * Multi-bay dovetailed ironwood chart pigeonhole hutch w/ readable
                                      Vietnamese hospital binder spines & articulated green-enamel
                                      gooseneck hospital reading sconce.
                                    * Framed cork notice board above desk (Z=1.60..2.18) with permanent
                                      side memos & ghost outline (Doc_WardOverflow mounts over it in B!).
  2. CTCC_STAFF_NurseDrawer       - Bespoke Cycles-baked interactive drawer fitting 100% flush inside
                                    the desk bay (front faces -Y -> Unity -X into STAFF when yaw=0!).
  3. CTCC_STAFF_NurseChair        - Hero 1987 Vietnamese tubular-steel & curved-slat varnished ironwood
                                    nursing duty chair (replaces legacy ArchiveChair on CHAIR-STAFF!).
  4. CTCC_STAFF_WallMedCabinet    - Slim wall-hung glazed medical supply cabinet (depth 0.24m, zero
                                    floor footprint) stocked w/ cotton drum, antiseptic bottles & gauze.
  5. CTCC_STAFF_UtilityProps      - Separate reusable STAFF nursing utility fittings:
                                    * BedStatusBoard (Ward Bed Occupancy & Shift Board, Khu A)
                                    * WallOscFan     (Vintage Vietnamese Điện Cơ oscillating wall fan)
                                    * LinenBin       (Compact enameled nursing dressing/linen hamper)
"""
from __future__ import annotations

import math
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))
sys.path.insert(0, str(HERE.parent / "loc02"))
sys.path.insert(0, str(HERE.parent / "loc03"))

import hero_lib as H  # noqa: E402
import hall_staff_lib as HS  # noqa: E402


# --------------------------------------------------------------------------------------
# 1. CTCC_PROP_NursingFilingDesk
#    Placed at (0.92, 0, 3.65), yaw=90f:
#      Blender +Y -> Unity +X (against east wall x=1.10 -> Y=+0.175)
#      Blender -Y -> Unity -X (front facing STAFF corridor -> Y=-0.175)
#      Blender +X -> Unity -Z (South side, toward chair z=2.83 & CH01_NURSE_DESK z=3.34!)
#      Blender -X -> Unity +Z (North side, where nurseDrawerRoot sits at world z=3.92 -> Blender X=-0.27!)
# --------------------------------------------------------------------------------------
def build_nursing_filing_desk():
    body = []
    bulb = []

    w, d = 1.20, 0.35
    half_w, half_d = w * 0.5, d * 0.5

    # 1. Welded sage-mint enameled tubular steel base frame with leveling rubber feet & reinforced side aprons
    for sx in (-half_w + 0.032, half_w - 0.032):
        for sy in (-half_d + 0.032, half_d - 0.032):
            body.append(H.cyl(f"ND_Foot_{sx:.2f}_{sy:.2f}", (sx, sy, 0.0), (sx, sy, 0.024), 0.017, "rubber", seg=12))
            body.append(H.box(f"ND_Leg_{sx:.2f}_{sy:.2f}", (sx, sy, 0.445), (0.030, 0.030, 0.85), "bed_enamel_green", bevel=0.003))
        # Side leg tie bars at Z=0.22
        body.append(H.box(f"ND_SideTie_{sx:.2f}", (sx, 0.0, 0.22), (0.026, d - 0.06, 0.026), "bed_enamel_green", bevel=0.003))

    # Reinforced lower storage shelf (Z = 0.225) on North half + slim rear tie bar on South kneehole so chair fits cleanly!
    body.append(H.box("ND_LowerShelf", (-0.18, 0.0, 0.232), (0.78, d - 0.06, 0.022), "cab_cream_mint", bevel=0.003))
    body.append(H.box("ND_RearStretch", (0.0, half_d - 0.032, 0.22), (w - 0.06, 0.024, 0.024), "bed_enamel_green", bevel=0.003))

    # Lower shelf organized hospital supplies (tidy clinical stack, NO red splats!)
    # Archival cardboard supply carton + folded green hospital linen + enamel dressing box
    body.append(H.box("ND_ArchiveBox", (-0.42, 0.01, 0.335), (0.28, 0.24, 0.185), "kraft", bevel=0.006))
    body.append(H.box("ND_ArchiveLid", (-0.42, 0.01, 0.432), (0.292, 0.252, 0.028), "kraft", bevel=0.004))
    box_lbl = H.box("ND_ArchiveTag", (-0.42, -0.118, 0.350), (0.12, 0.004, 0.075), "print:staff_med_labels", bevel=0.0)
    H.set_planar_print_uv(box_lbl, axis="Y", uv_rect=(0.0, 0.0, 0.5, 0.5))
    body.append(box_lbl)
    # Folded sterile green hospital linen towels on lower shelf
    for li in range(3):
        body.append(H.box(f"ND_LinenFold_{li}", (-0.12 + li * 0.005, 0.01, 0.268 + li * 0.044),
                          (0.24, 0.22, 0.040), "blanket_jade" if li != 1 else "cotton_sheet",
                          bevel=0.012, seg=2, rot=(0, 0, (li - 1) * 0.05)))
    # White enameled dressing tray box next to linen
    body.append(H.box("ND_EnamelTrayBox", (0.11, 0.01, 0.288), (0.16, 0.20, 0.088), "enamel_white", bevel=0.006))
    body.append(H.box("ND_EnamelTrayRim", (0.11, 0.01, 0.334), (0.168, 0.208, 0.012), "enamel_blue", bevel=0.003))

    # 2. Desk Apron & EXACT Recessed Drawer Housing Bay centered at Blender X = -0.27, Z = 0.77!
    #    Cavity spans X in [-0.485 .. -0.055] (width 0.43m), Z in [0.692 .. 0.848] (height 0.156m),
    #    open strictly toward front (-Y) so CTCC_STAFF_NurseDrawer (0.404w x 0.138h) slides in 100% flush!
    body.append(H.box("ND_ApronBack", (0.0, half_d - 0.012, 0.77), (w - 0.04, 0.020, 0.185), "cab_cream_mint", bevel=0.003))
    body.append(H.box("ND_ApronSideL", (-half_w + 0.014, 0.0, 0.77), (0.024, d - 0.02, 0.185), "cab_cream_mint", bevel=0.003))
    body.append(H.box("ND_ApronSideR", (half_w - 0.014, 0.0, 0.77), (0.024, d - 0.02, 0.185), "cab_cream_mint", bevel=0.003))
    # Left & Right inner walls of the drawer bay pocket at X = -0.495 and X = -0.045
    body.append(H.box("ND_BayWallL", (-0.502, 0.0, 0.77), (0.024, d - 0.02, 0.180), "cab_cream_mint", bevel=0.002))
    body.append(H.box("ND_BayWallR", (-0.038, 0.0, 0.77), (0.024, d - 0.02, 0.180), "cab_cream_mint", bevel=0.002))
    # Bottom floor & side steel slide rails of drawer bay pocket (Z = 0.682)
    body.append(H.box("ND_BayBottom", (-0.27, 0.01, 0.682), (0.45, d - 0.04, 0.018), "cab_cream_mint", bevel=0.002))
    for rx in (-0.484, -0.056):
        body.append(H.box(f"ND_BayRail_{rx:.3f}", (rx, 0.01, 0.752), (0.008, d - 0.06, 0.022), "satin_inox", bevel=0.001))
    # Right side (X = +0.05 .. +0.58, South writing knee bay) has a shallow under-desk apron rail (Z = 0.80..0.86) leaving generous kneehole!
    body.append(H.box("ND_KneeApron", (0.275, -half_d + 0.025, 0.832), (0.60, 0.022, 0.064), "bed_enamel_green", bevel=0.003))

    # 3. Sculpted Varnished Ironwood Writing Desktop (Z = 0.868..0.902) w/ calibrated forearm wear on X > +0.10!
    body.append(H.box("ND_WritingTop", (0.0, -0.006, 0.885), (w + 0.036, d + 0.034, 0.034), "wood_worn_desk", bevel=0.006, seg=2, grain="X"))
    # Brass protective corner edge strips on front corners
    for sx in (-1, 1):
        body.append(H.box(f"ND_CornerBrass_{sx}", (sx * (half_w + 0.014), -half_d - 0.018, 0.885), (0.014, 0.014, 0.036), "brass", bevel=0.002))

    # 4. Desktop Storytelling Props ("Giấy được xếp dồn bên kia cho khỏi vướng" -> pushed to X < +0.08, leaving X in [+0.14..+0.50] open for writing!)
    # Open 2-page Vietnamese Nursing Shift Logbook sitting cleanly forward of the lower-left binders (zero clipping!)
    body.append(H.box("ND_LogBookBase", (-0.34, -0.106, 0.908), (0.31, 0.156, 0.012), "cloth_navy", bevel=0.002, rot=(0, 0, 0.03)))
    log_p = H.box("ND_LogBookPages", (-0.34, -0.106, 0.918), (0.288, 0.142, 0.008), "print:staff_desk_log", bevel=0.001, rot=(0, 0, 0.03))
    H.set_planar_print_uv(log_p, axis="Z", uv_rect=(0.0, 0.0, 1.0, 1.0))
    body.append(log_p)

    # Stacked archival paper slips & red-ink hospital stamp + metal stamp pad beside logbook (X = -0.04)
    body.append(H.box("ND_SlipStackA", (-0.04, -0.04, 0.906), (0.16, 0.21, 0.008), "kraft", bevel=0.001, rot=(0, 0, -0.07)))
    body.append(H.box("ND_SlipStackB", (-0.03, -0.045, 0.913), (0.15, 0.195, 0.005), "paper", bevel=0.001, rot=(0, 0, 0.05)))
    # Hospital rubber stamp & open red-ink tin pad
    body.append(H.box("ND_InkPadBox", (0.11, -0.085, 0.910), (0.088, 0.058, 0.016), "steel_dark", bevel=0.002, rot=(0, 0, 0.14)))
    body.append(H.box("ND_InkPadFelt", (0.11, -0.085, 0.919), (0.076, 0.046, 0.004), "pccc_red", bevel=0.001, rot=(0, 0, 0.14)))
    body.append(H.lathe("ND_StampHandle", [
        (0.0, 0.0), (0.018, 0.0), (0.018, 0.010), (0.008, 0.022), (0.009, 0.055), (0.014, 0.068), (0.009, 0.076), (0.0, 0.076)
    ], (0.095, -0.015, 0.902), "wood", seg=16))

    # Glazed ceramic pen cup w/ blue/red hospital pens & stainless bandage scissors at far right back corner (X=+0.50, Y=-0.02)
    body.append(H.lathe("ND_PenCup", [
        (0.0, 0.0), (0.033, 0.0), (0.036, 0.008), (0.037, 0.094), (0.032, 0.094), (0.030, 0.010), (0.0, 0.010)
    ], (0.50, -0.035, 0.902), "ceramic", seg=20))
    body.append(H.cyl("ND_PenBlue", (0.495, -0.035, 0.945), (0.518, -0.052, 1.085), 0.0045, "enamel_blue", seg=10))
    body.append(H.cyl("ND_PenRed", (0.505, -0.028, 0.945), (0.482, -0.012, 1.072), 0.0045, "pccc_red", seg=10))
    body.append(H.cyl("ND_Pencil", (0.502, -0.042, 0.945), (0.525, -0.022, 1.068), 0.0042, "brass", seg=8))

    # Small stainless kidney tray & glass thermometer jar tucked neatly at far left edge (X=-0.54, Y=-0.05)
    body.append(H.box("ND_KidneyTray", (-0.535, -0.065, 0.912), (0.095, 0.155, 0.022), "satin_inox", bevel=0.010, seg=3, rot=(0, 0, -0.08)))
    body.append(H.cyl("ND_ThermoJar", (-0.535, 0.032, 0.902), (-0.535, 0.032, 0.992), 0.022, "enamel_white", seg=16, bevel=0.003))
    body.append(H.cyl("ND_ThermoCap", (-0.535, 0.032, 0.988), (-0.535, 0.032, 1.002), 0.024, "satin_inox", seg=16))

    # 5. Upper Dovetailed Varnished Ironwood Chart Pigeonhole Hutch (Z = 0.902..1.48, depth 0.22m against wall +Y)
    hy = half_d - 0.112
    body.append(H.box("ND_HutchBack", (0.0, half_d - 0.010, 1.195), (w, 0.020, 0.57), "wood", bevel=0.003, grain="X"))
    body.append(H.box("ND_HutchTop", (0.0, hy, 1.485), (w + 0.024, 0.236, 0.028), "wood_light", bevel=0.005, grain="X"))
    body.append(H.box("ND_HutchMid", (0.0, hy, 1.195), (w - 0.02, 0.214, 0.020), "wood_light", bevel=0.003, grain="X"))
    for kx in (-0.588, -0.20, 0.20, 0.588):
        body.append(H.box(f"ND_HutchDiv_{kx:.2f}", (kx, hy, 1.195), (0.022, 0.224, 0.56), "wood", bevel=0.003, grain="Z"))
    # Brass pigeonhole index label frames along mid & top shelf edge
    for lx in (-0.39, 0.0, 0.39):
        body.append(H.box(f"ND_IndexTag_{lx:.2f}", (lx, hy - 0.109, 1.195), (0.102, 0.006, 0.020), "brass", bevel=0.002))

    # Crisp upright archival hospital binders with readable printed Vietnamese spines (UV-sliced 4 columns!)
    binder_specs = [
        # Lower left cubby (X in [-0.57..-0.22], Z=0.91..1.18)
        (-0.535, 1.045, 0.044, 0.248, -2.0, 0),
        (-0.478, 1.040, 0.048, 0.238,  0.5, 1),
        (-0.418, 1.048, 0.050, 0.252,  2.2, 2),
        (-0.352, 1.036, 0.042, 0.228,  7.5, 3),
        # Upper right cubby (X in [+0.22..+0.57], Z=1.21..1.47)
        (0.258, 1.335, 0.048, 0.242, -1.5, 1),
        (0.320, 1.332, 0.052, 0.236,  0.5, 0),
        (0.386, 1.336, 0.046, 0.246,  3.2, 3),
        (0.450, 1.326, 0.042, 0.220,  9.5, 2),
    ]
    # Crisp upright archival hospital binders: solid cloth/kraft body + front printed Vietnamese spine plate (ZERO side UV streaks & clear of desktop logbook!)
    binder_kinds = ("cloth_navy", "kraft", "cloth_olive", "cloth_maroon")
    for bi, (bx, bz, bw, bh, tilt_deg, col_idx) in enumerate(binder_specs):
        body.append(H.box(f"ND_BinderBody_{bi}", (bx, hy + 0.006, bz), (bw, 0.172, bh),
                          binder_kinds[col_idx], bevel=0.003, rot=(0, math.radians(tilt_deg), 0)))
        sp_obj = H.box(f"ND_BinderSpine_{bi}", (bx, hy - 0.082, bz), (bw - 0.003, 0.004, bh - 0.006),
                       "print:staff_binder_spines", bevel=0.0, rot=(0, math.radians(tilt_deg), 0))
        u0, u1 = col_idx * 0.25 + 0.01, (col_idx + 1) * 0.25 - 0.01
        H.set_planar_print_uv(sp_obj, axis="Y", uv_rect=(u0, 0.02, u1, 0.98))
        body.append(sp_obj)

    # Horizontal patient chart folder stack in center-upper cubby + rolled bandage tin in center-lower cubby
    for si in range(4):
        body.append(H.box(f"ND_FlatChart_{si}", (-0.01 + si * 0.005, hy - 0.014, 1.216 + si * 0.016),
                          (0.31, 0.192, 0.013), "paper" if si % 2 else "kraft",
                          bevel=0.002, rot=(0, 0, (si - 1.5) * 0.045)))
    body.append(H.cyl("ND_CottonTin", (0.06, hy - 0.01, 0.902), (0.06, hy - 0.01, 1.035), 0.062, "satin_inox", seg=22, bevel=0.004))
    body.append(H.cyl("ND_CottonTinLid", (0.06, hy - 0.01, 1.032), (0.06, hy - 0.01, 1.050), 0.065, "satin_inox", seg=22, bevel=0.003))

    # 6. Articulated Vintage Green-Enamel Gooseneck Hospital Reading Sconce clipped to upper-right hutch (X=+0.48, Z=1.49)
    #    Points directly toward the writing corner (X=+0.30, Y=-0.10, Z=0.90) & aligns with "Staff desk sconce B" light!
    body.append(H.box("ND_LampClamp", (0.50, hy - 0.085, 1.488), (0.050, 0.062, 0.048), "steel_dark", bevel=0.004))
    body.append(H.cyl("ND_ClampScrew", (0.50, hy - 0.118, 1.474), (0.50, hy - 0.136, 1.474), 0.010, "brass", seg=12))
    body.append(H.pipe("ND_Gooseneck", [
        (0.50, hy - 0.085, 1.512), (0.50, hy - 0.105, 1.625),
        (0.45, hy - 0.185, 1.665), (0.37, hy - 0.245, 1.630)
    ], 0.0075, "satin_inox", res=5))
    body.append(H.lathe("ND_LampHood", [
        (0.011, 0.045), (0.026, 0.032), (0.034, 0.018), (0.076, -0.048),
        (0.072, -0.052), (0.030, 0.012), (0.012, 0.028)
    ], (0.355, hy - 0.258, 1.612), "enamel_lamp", seg=26, rot=(math.radians(28), math.radians(-22), 0)))
    bulb.append(H.lathe("ND_LampBulb", [
        (0.0, 0.018), (0.014, 0.014), (0.022, -0.006), (0.016, -0.026), (0.0, -0.032)
    ], (0.348, hy - 0.265, 1.592), "WarmGlow", seg=14, rot=(math.radians(28), math.radians(-22), 0)))

    # 7. Framed Cork Notice Board directly above desk (Z = 1.645..2.245, center Z=1.945 -> cleanly clears gooseneck lamp hood!)
    #    Cork backdrop has permanent side memos & center ghost outline; interactive Doc_WardOverflow mounts over it in Layer B!
    bw, bh = 0.72, 0.60
    by = half_d - 0.018
    cz_cork = 1.945
    body.append(H.box("ND_CorkBack", (0.0, by + 0.006, cz_cork), (bw - 0.04, 0.014, bh - 0.04), "wood", bevel=0.002))
    cface = H.box("ND_CorkSurface", (0.0, by - 0.004, cz_cork), (bw - 0.076, 0.008, bh - 0.076), "print:staff_cork_backdrop", bevel=0.0)
    H.set_planar_print_uv(cface, axis="Y", uv_rect=(0.0, 0.0, 1.0, 1.0))
    body.append(cface)
    body.append(H.box("ND_CorkFrameT", (0.0, by - 0.006, cz_cork + bh * 0.5 - 0.020), (bw, 0.034, 0.040), "wood_light", bevel=0.005, grain="X"))
    body.append(H.box("ND_CorkFrameB", (0.0, by - 0.006, cz_cork - bh * 0.5 + 0.020), (bw, 0.034, 0.040), "wood_light", bevel=0.005, grain="X"))
    for sx in (-1, 1):
        body.append(H.box(f"ND_CorkFrameS_{sx}", (sx * (bw * 0.5 - 0.020), by - 0.006, cz_cork), (0.040, 0.034, bh - 0.08), "wood_light", bevel=0.005, grain="Z"))
    # Brass corner pushpins along cork board top
    for px in (-0.16, 0.16):
        body.append(H.cyl(f"ND_Pin_{px:.2f}", (px, by - 0.008, cz_cork + 0.22), (px, by - 0.018, cz_cork + 0.22), 0.007, "brass", seg=10))

    return {"DeskBody": body, "DeskBulb": bulb}


# --------------------------------------------------------------------------------------
# 2. CTCC_STAFF_NurseDrawer (Bespoke interactive drawer fitting 100% flush inside desk bay!)
#    Origin at center (0, 0, 0) -> placed inside nurseDrawerRoot (0.922, 0.77, 3.92, rot Y=90) at yaw=0!
#    Front face at Y = -0.155 (faces -Y -> Unity -X into STAFF corridor!), fascia overlaps bay walls cleanly!
#    Drawer interior floor at Z = -0.056 -> ShiftGuideDrawerSheet (-0.025) & DrawerClip (-0.018) rest cleanly inside!
# --------------------------------------------------------------------------------------
def build_staff_nurse_drawer():
    objs = []
    w, d, h = 0.404, 0.305, 0.138
    fy = -d * 0.5

    # Molded sage-cream enameled steel front fascia (0.452m wide -> overlaps ND_BayWallL/R cleanly with zero side gaps!)
    objs.append(H.box("Drw_FrontFace", (0.0, fy, 0.0), (0.452, 0.022, h + 0.012), "cab_cream_mint", bevel=0.004))
    # Brushed brass card-index label holder frame + arched satin-inox bar pull facing -Y
    objs.append(H.box("Drw_LabelHolder", (0.0, fy - 0.013, 0.036), (0.098, 0.006, 0.028), "brass", bevel=0.0015))
    objs.append(H.box("Drw_LabelPaper", (0.0, fy - 0.0172, 0.036), (0.080, 0.002, 0.017), "paper", bevel=0.0))
    objs.append(H.pipe("Drw_PullHandle", [
        (-0.072, fy - 0.010, -0.012), (-0.068, fy - 0.042, -0.012),
        (0.068, fy - 0.042, -0.012), (0.072, fy - 0.010, -0.012)
    ], 0.007, "satin_inox", res=4))

    # Open-top steel drawer carcass (bottom Z = -0.058 so papers at -0.025 sit visibly atop inner folder!)
    objs.append(H.box("Drw_Bottom", (0.0, 0.008, -0.060), (w - 0.020, d - 0.024, 0.012), "breaker_grey", bevel=0.002))
    objs.append(H.box("Drw_Back", (0.0, d * 0.5 - 0.010, -0.008), (w - 0.020, 0.016, h - 0.020), "breaker_grey", bevel=0.002))
    for sx in (-1, 1):
        objs.append(H.box(f"Drw_Side_{sx}", (sx * (w * 0.5 - 0.012), 0.008, -0.008), (0.014, d - 0.024, h - 0.020), "breaker_grey", bevel=0.002))
        objs.append(H.box(f"Drw_SlideRail_{sx}", (sx * (w * 0.5 - 0.003), 0.012, -0.016), (0.009, d - 0.036, 0.018), "satin_inox", bevel=0.001))

    # Interior kraft folder base & spare pencils resting BELOW Unity's ShiftGuideDrawerSheet (Z=-0.025) and DrawerClip (-0.018)!
    objs.append(H.box("Drw_InnerFolder", (0.01, 0.008, -0.048), (0.31, 0.23, 0.010), "kraft", bevel=0.002, rot=(0, 0, 0.035)))
    objs.append(H.box("Drw_InnerPad", (-0.015, 0.012, -0.039), (0.26, 0.20, 0.006), "paper", bevel=0.001, rot=(0, 0, -0.03)))
    objs.append(H.cyl("Drw_SparePen", (0.145, -0.095, -0.048), (0.155, 0.065, -0.048), 0.0045, "enamel_blue", seg=10))
    return objs


# --------------------------------------------------------------------------------------
# 3. CTCC_STAFF_NurseChair (Hero 1987 Vietnamese hospital tubular-steel & ironwood duty chair!)
#    Origin at floor center (0,0,0), seat height 0.46m, total height 0.85m, width 0.42m, depth 0.44m.
#    Front faces -Y (matches MakeStaffChair rot Y=257 deg facing the desk writing corner!).
# --------------------------------------------------------------------------------------
def build_staff_nurse_chair():
    objs = []
    # Continuous bent sage-enamel tubular steel rear leg + backrest uprights (classic Vietnamese institutional chair!)
    for sx in (-0.185, 0.185):
        # Front leg curving smoothly into seat support rail & rear leg
        objs.append(H.pipe(f"Chr_SideFrame_{sx:.3f}", [
            (sx, -0.195, 0.018), (sx, -0.185, 0.432), (sx, -0.155, 0.448),
            (sx,  0.165, 0.448), (sx,  0.195, 0.425), (sx,  0.208, 0.018)
        ], 0.0125, "bed_enamel_green", res=5))
        # Upper backrest upright rising with gentle ergonomic 9-degree recline from seat frame
        objs.append(H.pipe(f"Chr_BackPost_{sx:.3f}", [
            (sx, 0.168, 0.448), (sx, 0.192, 0.62), (sx, 0.228, 0.845)
        ], 0.012, "bed_enamel_green", res=4))
        # Black rubber floor ferrules & top tube end-plugs
        objs.append(H.cyl(f"Chr_FootF_{sx:.3f}", (sx, -0.195, 0.0), (sx, -0.195, 0.028), 0.016, "rubber", seg=12))
        objs.append(H.cyl(f"Chr_FootB_{sx:.3f}", (sx,  0.208, 0.0), (sx,  0.208, 0.028), 0.016, "rubber", seg=12))
        objs.append(H.cyl(f"Chr_TopPlug_{sx:.3f}", (sx, 0.227, 0.840), (sx, 0.230, 0.855), 0.0125, "plastic_black", seg=12))

    # Welded under-seat cross stretchers & H-brace leg tie
    objs.append(H.cyl("Chr_FrontTie", (-0.185, -0.160, 0.436), (0.185, -0.160, 0.436), 0.010, "bed_enamel_green", seg=10))
    objs.append(H.cyl("Chr_RearTie", (-0.185, 0.155, 0.436), (0.185, 0.155, 0.436), 0.010, "bed_enamel_green", seg=10))
    objs.append(H.cyl("Chr_LowSideL", (-0.185, -0.190, 0.165), (-0.185, 0.202, 0.165), 0.009, "bed_enamel_green", seg=10))
    objs.append(H.cyl("Chr_LowSideR", (0.185, -0.190, 0.165), (0.185, 0.202, 0.165), 0.009, "bed_enamel_green", seg=10))
    objs.append(H.cyl("Chr_LowCenter", (-0.185, 0.010, 0.165), (0.185, 0.010, 0.165), 0.009, "bed_enamel_green", seg=10))

    # Contoured varnished ironwood saddle-shaped seat pan (waterfall front edge -> subtle dip -> rear support!)
    def seat_fn(u, v):
        x = (u - 0.5) * 0.415
        y = -0.205 + v * 0.395
        # Soft waterfall droop at front (v<0.14), ergonomic gentle saddle hollow in center
        waterfall = -0.022 * max(0.0, (0.15 - v) / 0.15) ** 1.6
        saddle = -0.010 * math.sin(u * math.pi) * math.sin(v * math.pi)
        z = 0.464 + waterfall + saddle
        return (x, y, z)

    objs.append(H.sculpted_grid("Chr_WoodSeat", 12, 12, seat_fn, "wood_light", thickness=0.022))

    # Twin ergonomically curved horizontal varnished ironwood backrest slats + chrome carriage bolts!
    for si, (cz, cy, sh) in enumerate(((0.635, 0.195, 0.105), (0.780, 0.218, 0.115))):
        def slat_fn(u, v, _cz=cz, _cy=cy, _sh=sh):
            x = (u - 0.5) * 0.395
            curve = -0.032 * math.cos((u - 0.5) * math.pi) + 0.032
            y = _cy + curve + (v - 0.5) * 0.018
            z = _cz + (v - 0.5) * _sh
            return (x, y, z)

        objs.append(H.sculpted_grid(f"Chr_BackSlat_{si}", 12, 5, slat_fn, "wood", thickness=0.018))
        for bx in (-0.172, 0.172):
            objs.append(H.cyl(f"Chr_Bolt_{si}_{bx:.3f}", (bx, cy + 0.018, cz), (bx, cy + 0.038, cz), 0.006, "satin_inox", seg=10))

    return objs


# --------------------------------------------------------------------------------------
# 4. CTCC_STAFF_WallMedCabinet (Slim wall-hung glazed medical supply cabinet for North STAFF!)
#    Back at Y=+0.12, protrudes -Y (depth 0.24m), width 0.96m, height 0.92m (bottom at Z=0, top Z=0.92).
#    Placed on STAFF East wall (x=1.10, y=1.18, z=10.60, yaw=90) -> faces -X into STAFF, ZERO floor footprint!
# --------------------------------------------------------------------------------------
def build_staff_wall_med_cabinet():
    body = []
    glass = []

    w, d, h = 0.96, 0.24, 0.92
    half_w, half_d = w * 0.5, d * 0.5

    # Cream-mint enameled steel wall cabinet carcass
    body.append(H.box("WMC_Back", (0.0, half_d - 0.010, h * 0.5), (w, 0.020, h), "cab_cream_mint", bevel=0.003))
    body.append(H.box("WMC_Top", (0.0, 0.0, h - 0.014), (w + 0.012, d + 0.010, 0.028), "cab_cream_mint", bevel=0.005))
    body.append(H.box("WMC_Bot", (0.0, 0.0, 0.014), (w, d, 0.028), "cab_cream_mint", bevel=0.004))
    for sx in (-1, 1):
        body.append(H.box(f"WMC_Side_{sx}", (sx * (half_w - 0.014), 0.0, h * 0.5), (0.028, d, h - 0.04), "cab_cream_mint", bevel=0.004))
    # Two interior steel shelves (Z = 0.31, 0.61) + vertical center mullion
    for sz in (0.31, 0.61):
        body.append(H.box(f"WMC_Shelf_{sz:.2f}", (0.0, 0.01, sz), (w - 0.06, d - 0.04, 0.016), "enamel_white", bevel=0.002))

    # Interior stocked hospital supplies visibly reading behind clear glass doors!
    # Bottom shelf (Z = 0.028..0.30): Stainless cotton sterilization drum + stack of white gauze packs + enamel tray
    body.append(H.cyl("WMC_CottonDrum", (-0.28, 0.01, 0.028), (-0.28, 0.01, 0.195), 0.082, "satin_inox", seg=22, bevel=0.005))
    body.append(H.cyl("WMC_DrumLid", (-0.28, 0.01, 0.192), (-0.28, 0.01, 0.214), 0.086, "satin_inox", seg=22, bevel=0.004))
    for gi in range(3):
        body.append(H.box(f"WMC_GauzePack_{gi}", (0.02 + gi * 0.135, 0.01, 0.088), (0.115, 0.145, 0.118),
                          "cotton_sheet", bevel=0.008))
    body.append(H.box("WMC_KidneyTray", (-0.08, 0.01, 0.048), (0.16, 0.11, 0.032), "satin_inox", bevel=0.008, seg=2))

    # Middle shelf (Z = 0.318..0.60): Amber antiseptic bottles + glass saline flasks w/ printed medical labels!
    for bi, (bx, br, bh, bkind, uv_q) in enumerate((
        (-0.36, 0.032, 0.165, "amber_glass",  (0.0, 0.5, 0.5, 1.0)),
        (-0.26, 0.032, 0.165, "amber_glass",  (0.5, 0.0, 1.0, 0.5)),
        (-0.15, 0.036, 0.195, "enamel_white", (0.5, 0.5, 1.0, 1.0)),
        (-0.04, 0.036, 0.195, "enamel_white", (0.5, 0.5, 1.0, 1.0)),
        ( 0.12, 0.030, 0.152, "amber_glass",  (0.0, 0.5, 0.5, 1.0)),
        ( 0.22, 0.030, 0.152, "enamel_blue",  (0.5, 0.0, 1.0, 0.5)),
        ( 0.34, 0.034, 0.175, "amber_glass",  (0.0, 0.5, 0.5, 1.0)),
    )):
        body.append(H.lathe(f"WMC_Bot_{bi}", [
            (0, 0), (br, 0), (br, bh * 0.72), (br * 0.45, bh * 0.88), (br * 0.45, bh), (0, bh)
        ], (bx, 0.01, 0.318), bkind, seg=14))
        body.append(H.cyl(f"WMC_Cap_{bi}", (bx, 0.01, 0.318 + bh), (bx, 0.01, 0.318 + bh + 0.018), br * 0.48, "plastic_black", seg=12))
        bl = H.box(f"WMC_Lbl_{bi}", (bx, 0.01 - br - 0.002, 0.318 + bh * 0.40), (br * 1.45, 0.003, bh * 0.44),
                   "print:staff_med_labels", bevel=0.0)
        H.set_planar_print_uv(bl, axis="Y", uv_rect=uv_q)
        body.append(bl)

    # Top shelf (Z = 0.618..0.89): Labeled medicine carton boxes & rolled cotton bandage cylinders
    for xi, (bx, bw, bh, uv_q) in enumerate((
        (-0.30, 0.22, 0.145, (0.0, 0.0, 0.5, 0.5)),
        (-0.04, 0.20, 0.165, (0.5, 0.5, 1.0, 1.0)),
        ( 0.24, 0.24, 0.135, (0.0, 0.5, 0.5, 1.0)),
    )):
        body.append(H.box(f"WMC_Carton_{xi}", (bx, 0.01, 0.618 + bh * 0.5), (bw, 0.155, bh), "kraft" if xi == 1 else "enamel_white", bevel=0.004))
        cl = H.box(f"WMC_CartonLbl_{xi}", (bx, -0.070, 0.618 + bh * 0.52), (bw * 0.72, 0.003, bh * 0.62), "print:staff_med_labels", bevel=0.0)
        H.set_planar_print_uv(cl, axis="Y", uv_rect=uv_q)
        body.append(cl)

    # Twin glazed front steel cabinet doors (-Y) with satin-inox handles, brass lock & small red medical cross badge!
    fy = -half_d + 0.012
    door_w = (w - 0.068) * 0.5
    for sx in (-1, 1):
        dx = sx * (door_w * 0.5 + 0.004)
        # Perimeter metal door stiles & rails (butt-jointed cleanly — ZERO corner coplanar AO!)
        body.append(H.box(f"WMC_DoorT_{sx}", (dx, fy, h - 0.048), (door_w - 0.085, 0.022, 0.042), "cab_cream_mint", bevel=0.003))
        body.append(H.box(f"WMC_DoorB_{sx}", (dx, fy, 0.048), (door_w - 0.085, 0.022, 0.042), "cab_cream_mint", bevel=0.003))
        body.append(H.box(f"WMC_DoorOut_{sx}", (dx + sx * (door_w * 0.5 - 0.021), fy, h * 0.5), (0.042, 0.022, h - 0.056), "cab_cream_mint", bevel=0.003))
        body.append(H.box(f"WMC_DoorIn_{sx}", (dx - sx * (door_w * 0.5 - 0.021), fy, h * 0.5), (0.042, 0.022, h - 0.056), "cab_cream_mint", bevel=0.003))
        glass.append(H.box(f"WMC_Glass_{sx}", (dx, fy, h * 0.5), (door_w - 0.080, 0.006, h - 0.130), "Glass", bevel=0.0))
        # Chrome pull handle near center
        hx = sx * 0.032
        body.append(H.pipe(f"WMC_Handle_{sx}", [
            (hx, fy - 0.010, 0.36), (hx, fy - 0.036, 0.36), (hx, fy - 0.036, 0.50), (hx, fy - 0.010, 0.50)
        ], 0.006, "satin_inox", res=3))
    # Brass keyhole cylinder & enameled red cross emblem on top brow
    body.append(H.cyl("WMC_Lock", (0.032, fy - 0.008, 0.28), (0.032, fy - 0.020, 0.28), 0.010, "brass", seg=12))
    body.append(H.box("WMC_CrossV", (-0.22, fy - 0.013, h - 0.048), (0.012, 0.004, 0.034), "pccc_red", bevel=0.001))
    body.append(H.box("WMC_CrossH", (-0.22, fy - 0.013, h - 0.048), (0.034, 0.004, 0.012), "pccc_red", bevel=0.001))

    return {"CabinetBody": body, "CabinetGlass": glass}


# --------------------------------------------------------------------------------------
# 5. CTCC_STAFF_UtilityProps (Separate reusable STAFF fittings: BedStatusBoard, WallOscFan, LinenBin)
#    BedStatusBoard & WallOscFan: back at X=0, face +X (yaw=0 on STAFF West wall x=-1.00!).
#    LinenBin: floor-standing at Z=0, depth 0.30m along Y, width 0.42m along X (tucked against East wall!).
# --------------------------------------------------------------------------------------
def build_staff_utility_props(only: str | None = None):
    # Part A: Framed Ward Bed Occupancy & Shift Board (0.98w x 0.74h) with aluminum chalk ledge & erasers!
    board = []
    if only in (None, "BedStatusBoard"):
        bw, bh = 0.98, 0.74
        board.append(H.box("BSB_Back", (0.010, 0.0, 0.0), (0.020, bw - 0.02, bh - 0.02), "anodized_alu", bevel=0.003))
        board.append(H.box("BSB_FrameT", (0.022, 0.0, bh * 0.5 - 0.018), (0.034, bw + 0.01, 0.036), "wood_light", bevel=0.004, grain="Y"))
        board.append(H.box("BSB_FrameB", (0.022, 0.0, -bh * 0.5 + 0.018), (0.034, bw + 0.01, 0.036), "wood_light", bevel=0.004, grain="Y"))
        board.append(H.box("BSB_FrameL", (0.022, -bw * 0.5 + 0.018, 0.0), (0.034, 0.036, bh - 0.072), "wood_light", bevel=0.004, grain="Z"))
        board.append(H.box("BSB_FrameR", (0.022, bw * 0.5 - 0.018, 0.0), (0.034, 0.036, bh - 0.072), "wood_light", bevel=0.004, grain="Z"))
        bface = H.box("BSB_Print", (0.022, 0.0, 0.0), (0.005, bw - 0.074, bh - 0.074), "print:staff_bed_board", bevel=0.0)
        H.set_planar_print_uv(bface, axis="X", uv_rect=(0.0, 0.0, 1.0, 1.0))
        board.append(bface)
        board.append(H.box("BSB_Tray", (0.056, 0.0, -bh * 0.5 + 0.012), (0.064, bw * 0.72, 0.012), "anodized_alu", bevel=0.002))
        board.append(H.box("BSB_Eraser", (0.058, -0.14, -bh * 0.5 + 0.030), (0.036, 0.11, 0.026), "wood", bevel=0.004))
        board.append(H.cyl("BSB_MarkerA", (0.058, 0.06, -bh * 0.5 + 0.024), (0.058, 0.19, -bh * 0.5 + 0.024), 0.007, "enamel_blue", seg=10))
        board.append(H.cyl("BSB_MarkerB", (0.066, 0.09, -bh * 0.5 + 0.024), (0.066, 0.21, -bh * 0.5 + 0.024), 0.007, "pccc_red", seg=10))

    # Part B: Vintage Vietnamese Điện Cơ Oscillating Wall Fan
    fan = []
    if only in (None, "WallOscFan"):
        fan.append(H.box("WF_WallBase", (0.048, 0.0, -0.05), (0.096, 0.125, 0.195), "pa_cream", bevel=0.014, seg=3))
        fan.append(H.cyl("WF_MountPlate", (0.006, 0.0, -0.05), (0.016, 0.0, -0.05), 0.052, "steel_dark", seg=16))
        for cy, cz1, col in ((-0.028, -0.46, "pa_cream"), (0.028, -0.42, "pccc_red")):
            fan.append(H.cyl(f"WF_Cord_{cy:.3f}", (0.068, cy, -0.145), (0.068, cy, cz1), 0.0022, "cotton_sheet", seg=6))
            fan.append(H.cyl(f"WF_Bead_{cy:.3f}", (0.068, cy, cz1 - 0.016), (0.068, cy, cz1 + 0.004), 0.009, col, seg=10, bevel=0.003))
        pitch = math.radians(18.0)
        fan.append(H.pipe("WF_Neck", [(0.082, 0.0, 0.02), (0.145, 0.0, 0.075), (0.185, 0.0, 0.088)], 0.018, "pa_cream", res=4))
        fan.append(H.lathe("WF_MotorDrum", [
            (0.0, -0.075), (0.058, -0.065), (0.068, -0.025), (0.068, 0.055), (0.050, 0.072), (0.0, 0.072)
        ], (0.205, 0.0, 0.088), "pa_cream", seg=20, rot=(0, math.radians(90) + pitch, 0)))

        fx, fz = 0.278, 0.062
        cp, sp = math.cos(pitch), math.sin(pitch)

        def head_pt(lx, ly, lz):
            return (fx + lx * cp + lz * sp, ly, fz - lx * sp + lz * cp)

        fan.append(H.cyl("WF_Hub", head_pt(-0.02, 0, 0), head_pt(0.028, 0, 0), 0.036, "plastic_black", seg=16, bevel=0.005))
        for i in range(3):
            ang = math.radians(i * 120.0 + 25.0)
            ca, sa = math.cos(ang), math.sin(ang)
            bc = head_pt(0.005, ca * 0.112, sa * 0.112)
            fan.append(H.box(f"WF_Blade_{i}", bc, (0.008, 0.165, 0.095), "fan_blade_teal", bevel=0.012, seg=2,
                             rot=(ang, pitch + math.radians(22), 0)))

        for rad, ax_off, tag in ((0.215, 0.0, "Rim"), (0.195, 0.042, "FrontOuter"), (0.118, 0.052, "FrontInner"), (0.185, -0.038, "RearRing")):
            pts = [head_pt(ax_off, math.cos(k * math.tau / 28) * rad, math.sin(k * math.tau / 28) * rad) for k in range(28)]
            fan.append(H.pipe(f"WF_Cage_{tag}", pts, 0.0045 if tag == "Rim" else 0.0028, "satin_inox", res=3, cyclic=True))
        for k in range(12):
            ang = k * math.tau / 12.0
            ca, sa = math.cos(ang), math.sin(ang)
            fan.append(H.pipe(f"WF_Spoke_{k}", [
                head_pt(0.054, ca * 0.048, sa * 0.048),
                head_pt(0.042, ca * 0.195, sa * 0.195),
                head_pt(0.000, ca * 0.215, sa * 0.215),
                head_pt(-0.038, ca * 0.185, sa * 0.185),
                head_pt(-0.045, ca * 0.055, sa * 0.055),
            ], 0.0024, "satin_inox", res=2))
        fan.append(H.cyl("WF_CenterLogo", head_pt(0.046, 0, 0), head_pt(0.058, 0, 0), 0.048, "pa_cream", seg=18, bevel=0.003))

    # Part C: Compact Enameled Hospital Linen & Dressing Hamper Bin
    lbin = []
    if only in (None, "LinenBin"):
        lw, ld, lh = 0.40, 0.28, 0.68
        for sx in (-lw * 0.46, lw * 0.46):
            for sy in (-ld * 0.44, ld * 0.44):
                lbin.append(H.cyl(f"LB_Leg_{sx:.2f}_{sy:.2f}", (sx, sy, 0.045), (sx, sy, lh), 0.011, "satin_inox", seg=10))
                lbin.append(H.cyl(f"LB_Wheel_{sx:.2f}_{sy:.2f}", (sx - 0.010, sy, 0.022), (sx + 0.010, sy, 0.022), 0.022, "rubber", seg=10))
        lbin.append(H.box("LB_BaseTray", (0.0, 0.0, 0.095), (lw - 0.02, ld - 0.02, 0.016), "satin_inox", bevel=0.003))
        lbin.append(H.pipe("LB_TopRim", [
            (-lw * 0.46, -ld * 0.44, lh), (lw * 0.46, -ld * 0.44, lh),
            (lw * 0.46, ld * 0.44, lh), (-lw * 0.46, ld * 0.44, lh)
        ], 0.012, "satin_inox", res=3, cyclic=True))
        lbin.append(H.box("LB_CanvasBag", (0.0, 0.0, 0.39), (lw - 0.05, ld - 0.05, 0.56), "cotton_sheet", bevel=0.026, seg=3))
        lbin.append(H.box("LB_BagCollar", (0.0, 0.0, lh - 0.032), (lw - 0.01, ld - 0.01, 0.068), "blanket_jade", bevel=0.010, seg=2))
        lbin.append(H.box("LB_Lid", (0.0, 0.0, lh + 0.020), (lw + 0.01, ld + 0.01, 0.026), "cab_cream_mint", bevel=0.007))
        lbin.append(H.pipe("LB_LidHandle", [(-0.05, -0.02, lh + 0.032), (-0.05, -0.02, lh + 0.056), (0.05, -0.02, lh + 0.056), (0.05, -0.02, lh + 0.032)],
                           0.0055, "satin_inox", res=3))

    return {
        "BedStatusBoard": board,
        "WallOscFan": fan,
        "LinenBin": lbin,
    }


def build_bed_status_board():
    return build_staff_utility_props("BedStatusBoard")["BedStatusBoard"]


def build_wall_osc_fan():
    return build_staff_utility_props("WallOscFan")["WallOscFan"]


def build_linen_bin():
    return build_staff_utility_props("LinenBin")["LinenBin"]


def run_split_fixes():
    HS.export_hs_asset(
        "CTCC_STAFF_BedStatusBoard", build_bed_status_board,
        {"STAFF_BedBoard": {"res": 1024, "bake_r": True}},
        make_lods=False, cam_dir=(1.85, -0.75, 0.25), ao_samples=10,
    )
    HS.export_hs_asset(
        "CTCC_STAFF_WallOscFan", build_wall_osc_fan,
        {"STAFF_WallFan": {"res": 1024, "bake_r": True}},
        make_lods=False, cam_dir=(1.45, -1.25, 0.35), ao_samples=10,
    )
    HS.export_hs_asset(
        "CTCC_STAFF_LinenBin", build_linen_bin,
        {"STAFF_LinenBin": {"res": 1024, "bake_r": True}},
        make_lods=False, cam_dir=(1.25, -1.45, 0.65), ao_samples=10,
    )


def run_all():
    HS.export_hs_asset(
        "CTCC_STAFF_NurseChair", build_staff_nurse_chair,
        {"STAFF_NurseChair": {"res": 1024, "bake_r": True}},
        make_lods=True, cam_dir=(1.35, -1.65, 0.72), ao_samples=12,
    )
    run_split_fixes()


if __name__ == "__main__":
    run_all()
