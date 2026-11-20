"""M3.0 CH03 ('Những cái tên bị xóa') Architecture, Medical Archive Furniture & Hero Props in Blender 4.5 LTS.

Covers LOC05 (Trạm điều dưỡng), LOC06 (Hành lang ca trực), and LOC07 (Kho hồ sơ & phòng họp nhỏ):
- CTCC_ENV_ArchiveShelfRack: 5-tier 2005 Vietnamese hospital medical archive shelving rack with bound case folders,
  cardboard storage boxes, metal index tabs, and physical gap slots for C02 & C12 (LOC07 / CH03-PU01).
- CTCC_PROP_PatientRegisterE03: Open heavy hardcover internal patient register ledger (E03 — Sổ đăng ký bệnh nhân C01–C12)
  with cloth-bound spine, ruled ledger pages, and metal pen rest (LOC07).
- CTCC_PROP_KnittingBagC02: Bà Lành's (C02) woven cloth knitting bag, skein of crimson-red wool yarn, crossed bamboo
  knitting needles, unfinished knitted swatch & care tag (LOC07 / CH03-PU01 & CH03-PU02).
- CTCC_PROP_UnnamedBoxC12: C12 personal effects box with unmarked brass house key & blank string tag (LOC07 / CH03-Q03).
- CTCC_PROP_VintageRadio: Portable 2000s hospital staff room cassette/AM-FM radio used as acoustic distraction in LOC07
  Meeting Room bypass loop (TASK011 / CH03-Q04).
"""

import math
import sys
from pathlib import Path

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))
import common as C


def build_archive_shelf_rack():
    """2.12m tall x 1.24m wide x 0.44m deep 5-tier hospital archive rack with ledger binders & C02/C12 physical gaps."""
    objs = []
    w, d, h = 1.24, 0.44, 2.12
    # 4 slotted angle-iron corner uprights + top/bottom diagonal cross braces
    for sx in (-w * 0.48, w * 0.48):
        for sy in (-d * 0.46, d * 0.46):
            objs.append(C.make_box(f"Post_{sx:.2f}_{sy:.2f}", (sx, sy, h * 0.5), (0.036, 0.036, h), "CabinetOld", bevel=0.004))
            objs.append(C.make_box(f"Foot_{sx:.2f}_{sy:.2f}", (sx, sy, 0.012), (0.052, 0.052, 0.024), "Rubber", bevel=0.004))

    objs.append(C.make_cylinder("BackCrossA", (-w * 0.47, d * 0.46, 0.22), (w * 0.47, d * 0.46, h - 0.18), 0.009, "MetalOld", segments=8))
    objs.append(C.make_cylinder("BackCrossB", (w * 0.47, d * 0.46, 0.22), (-w * 0.47, d * 0.46, h - 0.18), 0.009, "MetalOld", segments=8))

    # 5 painted steel shelf decks
    shelf_zs = [0.16, 0.62, 1.08, 1.54, 2.00]
    for idx, sz in enumerate(shelf_zs):
        objs.append(C.make_box(f"Shelf_{idx}", (0, 0, sz), (w, d, 0.028), "CabinetPaint", bevel=0.004))
        # Front metal index label strip
        objs.append(C.make_box(f"ShelfTag_{idx}", (0, -d * 0.505, sz), (0.22, 0.006, 0.020), "Paper", bevel=0.001))

    # Populate shelves with bound medical record folders & archive boxes, leaving deliberate gaps at C02 (shelf 2 left) and C12 (shelf 3 right)!
    for s_idx, sz in enumerate(shelf_zs[:-1]):
        for slot in range(11):
            # Leave clear physical missing-file gaps at slot 2 on shelf 2 (C02) and slot 9 on shelf 3 (C12)!
            if (s_idx == 1 and slot in (2, 3)) or (s_idx == 2 and slot in (8, 9)):
                continue
            fx = -0.48 + slot * 0.095
            fh = 0.31 + (slot % 3) * 0.015
            tilt = math.radians(4.0 if slot in (4, 7) else 0.0)
            mat = "KraftPaper" if (slot + s_idx) % 3 == 0 else ("CabinetOld" if slot % 2 == 0 else "Wood")
            objs.append(C.make_box(f"Binder_{s_idx}_{slot}", (fx, -0.01, sz + 0.014 + fh * 0.5),
                                   (0.068, 0.29, fh), mat, bevel=0.004, rot=(0, tilt, 0)))
            objs.append(C.make_box(f"SpineLabel_{s_idx}_{slot}", (fx, -0.158, sz + 0.014 + fh * 0.62),
                                   (0.046, 0.005, 0.065), "Paper", bevel=0.001, rot=(0, tilt, 0)))
    return objs


def build_patient_register_e03():
    """Open hardcover 2005 patient register ledger (E03 — Sổ đăng ký bệnh nhân C01–C12) with cloth spine & brass pen."""
    objs = []
    # Left & right hardcover boards splayed open on archive table
    objs.append(C.make_box("CoverLeft", (-0.165, 0.0, 0.010), (0.31, 0.42, 0.014), "WoodDoor", bevel=0.003, rot=(0, math.radians(-2), 0)))
    objs.append(C.make_box("CoverRight", (0.165, 0.0, 0.010), (0.31, 0.42, 0.014), "WoodDoor", bevel=0.003, rot=(0, math.radians(2), 0)))
    objs.append(C.make_cylinder("ClothSpine", (0.0, -0.212, 0.012), (0.0, 0.212, 0.012), 0.022, "DarkTrouser", segments=14))

    #Thick arched cream ledger page stacks (left & right pages)
    objs.append(C.make_box("PagesLeft", (-0.155, 0.0, 0.024), (0.285, 0.395, 0.018), "Paper", bevel=0.005, segments=3, rot=(0, math.radians(-3), 0)))
    objs.append(C.make_box("PagesRight", (0.155, 0.0, 0.024), (0.285, 0.395, 0.018), "Paper", bevel=0.005, segments=3, rot=(0, math.radians(3), 0)))
    # Ribbon bookmark + vintage fountain pen resting diagonally across right page
    objs.append(C.make_box("Ribbon", (0.02, -0.14, 0.035), (0.016, 0.22, 0.002), "RedPaint", bevel=0.0005, rot=(0, 0, math.radians(12))))
    objs.append(C.make_cylinder("FountainPen", (0.06, -0.11, 0.038), (0.23, 0.07, 0.038), 0.0065, "Black", segments=12))
    objs.append(C.make_cylinder("PenNib", (0.042, -0.128, 0.038), (0.06, -0.11, 0.038), 0.0040, "Brass", segments=10))
    return objs


def build_knitting_bag_c02():
    """Bà Lành's (C02) woven cloth knitting bag, skein of red wool yarn, crossed bamboo knitting needles & care note."""
    objs = []
    # Soft slumped cloth tote/drawstring bag
    objs.append(C.make_box("ClothBag", (-0.04, 0.02, 0.065), (0.26, 0.22, 0.13), "CottonOld", bevel=0.038, segments=3, rot=(0, 0, math.radians(-8))))
    # Skein of warm red wool yarn (cuộn len đỏ của Bà Lành) nestled on top/front of bag
    objs.append(C.make_box("RedYarnBall", (0.04, -0.04, 0.115), (0.125, 0.105, 0.095), "RedPaint", bevel=0.042, segments=4, rot=(math.radians(12), math.radians(-10), math.radians(18))))
    # Unfinished knitted wool collar swatch draped beside yarn ball
    objs.append(C.make_box("KnittedSwatch", (-0.08, -0.06, 0.095), (0.15, 0.12, 0.022), "RedPaint", bevel=0.008, segments=2, rot=(math.radians(16), math.radians(-12), math.radians(-15))))
    # Crossed bamboo knitting needles skewering the yarn skein
    objs.append(C.make_cylinder("NeedleA", (-0.11, -0.13, 0.08), (0.16, 0.08, 0.16), 0.0042, "ReedMat", segments=10))
    objs.append(C.make_cylinder("NeedleB", (-0.08, 0.09, 0.15), (0.15, -0.12, 0.09), 0.0042, "ReedMat", segments=10))
    # Folded hospital care card (`Phiếu chăm sóc C02`)
    objs.append(C.make_box("CareNoteC02", (0.13, 0.04, 0.012), (0.12, 0.16, 0.006), "Paper", bevel=0.001, rot=(0, 0, math.radians(14))))
    return objs


def build_unnamed_box_c12():
    """C12 personal effects cardboard/wood storage box with unmarked brass house key on blank tag (CH03-Q03)."""
    objs = []
    # Worn kraft-board personal effects box with slightly ajar lid
    objs.append(C.make_box("EffectsBoxBase", (0.0, 0.0, 0.055), (0.28, 0.20, 0.11), "KraftPaper", bevel=0.006))
    objs.append(C.make_box("EffectsBoxLid", (0.015, 0.02, 0.116), (0.29, 0.21, 0.028), "KraftPaper", bevel=0.005, rot=(0, 0, math.radians(7))))
    objs.append(C.make_box("BoxLabelC12", (0.0, -0.102, 0.060), (0.11, 0.004, 0.048), "Paper", bevel=0.001))
    # Unmarked brass house key resting on top of a blank manila tag beside the box
    objs.append(C.make_box("BlankKeyTag", (0.18, -0.04, 0.005), (0.055, 0.095, 0.003), "Paper", bevel=0.001, rot=(0, 0, math.radians(-16))))
    objs.append(C.make_cylinder("KeyBow", (0.19, -0.01, 0.009), (0.19, -0.01, 0.014), 0.016, "Brass", segments=16))
    objs.append(C.make_box("KeyShaft", (0.175, -0.048, 0.011), (0.007, 0.055, 0.004), "Brass", bevel=0.001, rot=(0, 0, math.radians(-18))))
    objs.append(C.make_box("KeyTeeth", (0.163, -0.064, 0.011), (0.014, 0.016, 0.004), "Brass", bevel=0.001, rot=(0, 0, math.radians(-18))))
    return objs


def build_vintage_radio():
    """2000s portable cassette/AM-FM radio used as acoustic distraction in LOC07 Meeting Room loop (CH03-Q04)."""
    objs = []
    # Main dark plastic boombox chassis
    objs.append(C.make_box("RadioBody", (0.0, 0.0, 0.085), (0.36, 0.13, 0.17), "Black", bevel=0.016, segments=3))
    # Left & right circular speaker grilles
    for sx in (-0.115, 0.115):
        objs.append(C.make_cylinder(f"Speaker_{sx:.2f}", (sx, -0.062, 0.082), (sx, -0.068, 0.082), 0.052, "MetalOld", segments=20))
    # Central cassette deck window + analog tuning scale above
    objs.append(C.make_box("CassetteDoor", (0.0, -0.066, 0.065), (0.105, 0.008, 0.068), "FrostedGlass", bevel=0.002))
    objs.append(C.make_box("TuningScale", (0.0, -0.066, 0.132), (0.145, 0.006, 0.026), "Paper", bevel=0.001))
    objs.append(C.make_box("TuningNeedle", (0.025, -0.069, 0.132), (0.004, 0.004, 0.024), "RedPaint", bevel=0.0005))
    # Top control buttons, fold-down carrying handle & angled telescopic whip antenna
    for bx in (-0.045, -0.022, 0.0, 0.022, 0.045):
        objs.append(C.make_box(f"Btn_{bx:.3f}", (bx, -0.01, 0.174), (0.016, 0.024, 0.012), "Steel", bevel=0.002))
    objs.append(C.make_pipe("CarryHandle", [(-0.14, 0.0, 0.17), (-0.14, 0.0, 0.225), (0.14, 0.0, 0.225), (0.14, 0.0, 0.17)], 0.009, "Black", bevel_res=3))
    objs.append(C.make_cylinder("WhipAntenna", (0.14, 0.04, 0.17), (-0.08, 0.04, 0.48), 0.0035, "Steel", segments=10))
    return objs


def run_all():
    C.export_asset("CTCC_ENV_ArchiveShelfRack", build_archive_shelf_rack, make_lods=True, cam_dir=(1.4, -1.6, 0.65))
    C.export_asset("CTCC_PROP_PatientRegisterE03", build_patient_register_e03, make_lods=False, cam_dir=(1.2, -1.4, 1.1))
    C.export_asset("CTCC_PROP_KnittingBagC02", build_knitting_bag_c02, make_lods=False, cam_dir=(1.3, -1.4, 0.95))
    C.export_asset("CTCC_PROP_UnnamedBoxC12", build_unnamed_box_c12, make_lods=False, cam_dir=(1.3, -1.4, 0.95))
    C.export_asset("CTCC_PROP_VintageRadio", build_vintage_radio, make_lods=False, cam_dir=(1.3, -1.5, 0.75))


if __name__ == "__main__":
    run_all()
