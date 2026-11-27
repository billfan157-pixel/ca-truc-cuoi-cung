"""LOC03 Ward 101 Medical Props, Patient Life Storytelling & Store Room Payoff (Blender 4.5 LTS).

Produces Cycles-baked PBR hero assets:
  1. CTCC_PROP_IVStand           -> LOC03_IVStand (1024² B/R + SalineFluid) — NaCl 0,9% printed label, satin stainless!
  2. CTCC_PROP_ThermosAndMug     -> LOC03_ThermosMug (1024² B/R) — Vintage crimson/gold tin thermos + enamel mug + cloth towel!
  3. CTCC_PROP_WashbasinAndMat   -> LOC03_WashbasinMat (1024² B/R) — Blue-rim enamel basin + woven sedge mat (chiếu cói)!
  4. CTCC_PROP_HoneycombSlippers -> LOC03_Slippers (512² B/R) — Contoured ivory-yellow honeycomb slippers!
  5. CTCC_PROP_CH01CareSupplies  -> LOC03_CareProps (1024² B-only) — Enamel tray, gauze stack & CỒN 70° / POVIDON-IOD bottles!
  6. CTCC_PROP_PedalBinWorking   -> LOC03_PedalBin (1024² B/R, separate BinShell, BinLid, BinPedal, BinHinge!)
  7. CTCC_PROP_ClipboardF101     -> LOC03_Clipboard (1024² B-only + F101) — Varnished masonite board + spring clip + pen!
  8. CTCC_PROP_Bed4ChartHolder   -> LOC03_ChartHolder (1024² B-only + ObsCard) — Weighted stainless stanchion + crisp ObsCard!
  9. CTCC_PROP_DryShelf          -> LOC03_DryShelf (2048² B/R) — Archive timber shelving (exact Z=0.86 & Z=1.74 E11 shelves!)
 10. CTCC_PROP_DismantledBedFrame -> LOC03_StorePayoff (2048² R-only) — Bed 4 dismantled arches + G.04 tag + rolled mattress + cloth-sim blanket stack!
 11. CTCC_PROP_PatientBag        -> LOC03_PatientBag (1024² R-only) — Slumped woven patient tote w/ brass zipper & straps!
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
from build_ward_beds_curtain import _make_contoured_slipper_hero  # noqa: E402


def build_iv_stand():
    """Satin stainless 5-wheel rolling hospital IV pole (1.92 m) with translucent NaCl 0,9% bottle & coiled drip tube."""
    objs = []
    objs.append(H.cyl("BaseHub", (0, 0, 0.068), (0, 0, 0.135), 0.032, "satin_inox", seg=16, bevel=0.004))
    for i in range(5):
        ang = math.radians(i * 72.0)
        ca, sa = math.cos(ang), math.sin(ang)
        objs.append(H.pipe(f"StarLeg_{i}", [
            (ca * 0.02, sa * 0.02, 0.105), (ca * 0.14, sa * 0.14, 0.088), (ca * 0.26, sa * 0.26, 0.072)
        ], 0.012, "satin_inox", res=4))
        cx, cy = ca * 0.26, sa * 0.26
        objs.append(H.cyl(f"IVWheel_{i}", (cx - sa * 0.012, cy + ca * 0.012, 0.026),
                          (cx + sa * 0.012, cy - ca * 0.012, 0.026), 0.026, "rubber", seg=14))
        objs.append(H.cyl(f"IVWheelPin_{i}", (cx, cy, 0.035), (cx, cy, 0.072), 0.008, "satin_inox", seg=10))

    objs.append(H.cyl("OuterPole", (0, 0, 0.12), (0, 0, 1.18), 0.014, "satin_inox", seg=14))
    objs.append(H.cyl("LockCollar", (0, 0, 1.155), (0, 0, 1.215), 0.021, "plastic_black", seg=14, bevel=0.003))
    objs.append(H.cyl("ThumbScrew", (0.018, 0, 1.185), (0.052, 0, 1.185), 0.006, "satin_inox", seg=10))
    objs.append(H.cyl("ThumbKnob", (0.048, 0, 1.185), (0.062, 0, 1.185), 0.015, "plastic_black", seg=12, bevel=0.003))
    objs.append(H.cyl("InnerPole", (0, 0, 1.18), (0, 0, 1.90), 0.0095, "satin_inox", seg=14))
    objs.append(H.sphere("TopFinial", (0, 0, 1.905), (0.026, 0.026, 0.026), "satin_inox", seg=14, rings=10))

    for s in (-1, 1):
        objs.append(H.pipe(f"IVHook_{s}", [
            (0, 0, 1.88), (s * 0.075, 0, 1.88), (s * 0.125, 0, 1.845),
            (s * 0.145, 0, 1.875), (s * 0.120, 0, 1.905)
        ], 0.0045, "satin_inox", res=3))

    bx = 0.128
    objs.append(H.pipe("BottleStrap", [
        (bx, 0, 1.855), (bx, 0.039, 1.79), (bx, 0.039, 1.675),
        (bx, -0.039, 1.675), (bx, -0.039, 1.79), (bx, 0, 1.855)
    ], 0.003, "plastic_ivory", res=3))
    objs.append(H.lathe("SalineBottle", [
        (0.0, 1.815), (0.036, 1.812), (0.039, 1.795), (0.039, 1.665),
        (0.026, 1.635), (0.015, 1.622), (0.015, 1.595), (0.0, 1.595)
    ], (bx, 0, 0), "SalineFluid", seg=20))
    objs.append(H.cyl("BottleStopper", (bx, 0, 1.585), (bx, 0, 1.608), 0.016, "rubber", seg=12))
    # Printed cylindrical NaCl 0.9% label band wrapped around saline bottle!
    lbl = H.cyl("BottleLabel", (bx, 0, 1.682), (bx, 0, 1.772), 0.0404, "print:saline_label", seg=20, cap=False)
    H.set_cylindrical_print_uv(lbl, 1.682, 1.772, angle_offset=math.pi * 0.5)
    objs.append(lbl)

    objs.append(H.cyl("DripChamber", (bx, 0, 1.51), (bx, 0, 1.582), 0.0085, "SalineFluid", seg=12))
    objs.append(H.pipe("IVTubing", [
        (bx, 0, 1.51), (bx - 0.01, 0.015, 1.38), (bx - 0.045, 0.032, 1.24),
        (bx - 0.095, 0.025, 1.12), (bx - 0.055, -0.025, 1.05), (bx + 0.015, -0.035, 1.15),
        (bx + 0.045, -0.010, 1.28), (bx + 0.020, 0.025, 1.16)
    ], 0.0032, "SalineFluid", res=3))
    objs.append(H.box("RollerClamp", (bx - 0.028, 0.024, 1.30), (0.016, 0.014, 0.042), "plastic_ivory", bevel=0.002))
    return objs


def build_thermos_and_mug():
    """Vintage Vietnamese crimson/gold patterned 2L tin thermos (phích nước vỏ sắt) + blue-rim enamel mug + folded towel."""
    objs = []
    tx, ty = -0.07, 0.02
    # Main patterned tin barrel (cylindrical UV wrap with non-branded vintage crimson/gold band!)
    barrel = H.cyl("ThermosBarrel", (tx, ty, 0.024), (tx, ty, 0.296), 0.0705, "print:thermos_band", seg=26, cap=False)
    H.set_cylindrical_print_uv(barrel, 0.024, 0.296)
    objs.append(barrel)
    objs.append(H.cyl("ThermosBaseRing", (tx, ty, 0.0), (tx, ty, 0.026), 0.0725, "satin_inox", seg=24, bevel=0.003))
    objs.append(H.lathe("ThermosShoulder", [
        (0.0725, 0.294), (0.052, 0.345), (0.037, 0.355), (0.037, 0.372), (0.0, 0.372)
    ], (tx, ty, 0), "satin_inox", seg=24))
    objs.append(H.lathe("ThermosCap", [
        (0.044, 0.342), (0.041, 0.398), (0.036, 0.404), (0.0, 0.404)
    ], (tx, ty, 0), "thermos_red", seg=22))
    objs.append(H.pipe("ThermosHandle", [
        (tx - 0.068, ty, 0.275), (tx - 0.118, ty, 0.255),
        (tx - 0.120, ty, 0.120), (tx - 0.068, ty, 0.095)
    ], 0.007, "plastic_ivory", res=4))

    mx, my = 0.095, -0.02
    objs.append(H.lathe("EnamelMug", [
        (0.0, 0.0), (0.042, 0.0), (0.044, 0.008), (0.044, 0.088),
        (0.048, 0.094), (0.044, 0.096), (0.039, 0.088), (0.039, 0.008), (0.0, 0.008)
    ], (mx, my, 0), "enamel_white", seg=24))
    objs.append(H.lathe("MugBlueRim", [(0.0435, 0.089), (0.049, 0.094), (0.0435, 0.097), (0.039, 0.090)], (mx, my, 0), "enamel_blue", seg=24))
    objs.append(H.pipe("MugHandle", [
        (mx + 0.042, my, 0.076), (mx + 0.074, my, 0.068),
        (mx + 0.072, my, 0.026), (mx + 0.042, my, 0.020)
    ], 0.0055, "enamel_white", res=3))

    # Soft folded face towel beside mug
    objs.append(H.box("FoldedTowelA", (0.07, 0.085, 0.013), (0.17, 0.12, 0.024), "towel_terry", bevel=0.010, seg=3, rot=(0, 0, 0.12)))
    objs.append(H.box("FoldedTowelB", (0.072, 0.082, 0.035), (0.16, 0.115, 0.022), "blanket_jade", bevel=0.010, seg=3, rot=(0, 0, 0.06)))
    return objs


def build_washbasin_and_mat():
    """Under-bed white/blue-rimmed enamel hospital washbasin + rolled woven sedge sleeping mat (chiếu cói)."""
    objs = []
    objs.append(H.lathe("Washbasin", [
        (0.0, 0.0), (0.135, 0.0), (0.155, 0.022), (0.195, 0.112), (0.214, 0.122),
        (0.218, 0.115), (0.192, 0.108), (0.150, 0.020), (0.130, 0.006), (0.0, 0.006)
    ], (0, 0, 0), "enamel_white", seg=28))
    objs.append(H.lathe("BasinBlueRim", [(0.205, 0.118), (0.219, 0.122), (0.219, 0.113), (0.205, 0.111)], (0, 0, 0), "enamel_blue", seg=28))
    # Folded damp washcloth resting inside basin
    objs.append(H.box("BasinCloth", (-0.03, 0.02, 0.024), (0.15, 0.12, 0.026), "towel_terry", bevel=0.010, seg=3, rot=(0.06, -0.05, 0.35)))

    # Woven sedge mat roll with red woven cloth end borders & natural hemp twine tie
    objs.append(H.cyl("ReedMatRoll", (-0.32, 0.34, 0.056), (0.62, 0.30, 0.056), 0.055, "sedge_mat", seg=22, bevel=0.006))
    for mx in (-0.31, 0.60):
        objs.append(H.cyl(f"MatBinding_{mx:.2f}", (mx, 0.32, 0.056), (mx + 0.026, 0.319, 0.056), 0.0565, "thermos_red", seg=22))
    objs.append(H.cyl("MatTwine", (0.14, 0.321, 0.056), (0.158, 0.320, 0.056), 0.058, "kraft", seg=18))
    return objs


def build_honeycomb_slippers():
    objs = []
    objs.extend(_make_contoured_slipper_hero("AdultL_", (-0.075, -0.02, 0.0), yaw=0.16, scale=1.0, mirror=1.0))
    objs.extend(_make_contoured_slipper_hero("AdultR_", (0.085, 0.04, 0.0), yaw=-0.24, scale=1.0, mirror=-1.0))
    return objs


def build_care_supplies():
    """Bed 1 bedside tray: enameled dressing bowl, cotton gauze pads & labeled CỒN 70° / POVIDON-IOD amber bottles."""
    objs = []
    objs.append(H.lathe("EnamelBowl", [
        (0, 0.008), (0.078, 0.008), (0.082, 0.016), (0.104, 0.062), (0.110, 0.069),
        (0.102, 0.069), (0.097, 0.060), (0.074, 0.020), (0, 0.020)
    ], (-0.071, 0, 0), "enamel_white", seg=24))
    objs.append(H.lathe("BowlBlueRim", [(0.101, 0.066), (0.111, 0.070), (0.105, 0.071), (0.099, 0.065)], (-0.071, 0, 0), "enamel_blue", seg=24))
    objs.append(H.box("GauzeStack", (-0.076, 0.010, 0.038), (0.091, 0.073, 0.028), "cotton_sheet", bevel=0.009, seg=2, rot=(0, 0, math.radians(11))))

    for idx, (x, y, h, rect) in enumerate([
        (0.085, 0.040, 0.125, (0.02, 0.76, 0.48, 0.98)),
        (0.134, -0.033, 0.106, (0.02, 0.51, 0.48, 0.73)),
    ]):
        objs.append(H.lathe(f"AmberBottle_{idx}", [
            (0, 0), (0.022, 0), (0.025, 0.007), (0.025, h - 0.025),
            (0.012, h - 0.011), (0.012, h), (0, h)
        ], (x, y, 0), "amber_glass", seg=16))
        objs.append(H.cyl(f"PhenolicCap_{idx}", (x, y, h - 0.007), (x, y, h + 0.009), 0.014, "plastic_black", seg=14))
        lbl = H.cyl(f"MedLbl_{idx}", (x, y, 0.024), (x, y, h - 0.032), 0.0258, "print:med_labels", seg=16, cap=False)
        H.set_cylindrical_print_uv(lbl, 0.024, h - 0.032)
        # Remap cylindrical [0..1] UVs cleanly into the bottle's exact label sub-rect!
        ru0, rv0, ru1, rv1 = rect
        puv = lbl.data.uv_layers["PRINT"].data
        for l in puv:
            l.uv = (ru0 + l.uv[0] * (ru1 - ru0), rv0 + l.uv[1] * (rv1 - rv0))
        objs.append(lbl)
    return objs


def build_pedal_bin_working():
    """Hero hospital foot-pedal medical waste bin. MUST preserve EXACT part names:
    'BinShell', 'BinLid', 'BinPedal', 'BinHinge' (animated by Ch01PedalBin in DressRebirthWard!)."""
    body = H.lathe("EnamelShell", [
        (0.0, 0.0), (0.136, 0.0), (0.144, 0.015), (0.144, 0.346), (0.150, 0.354),
        (0.136, 0.356), (0.130, 0.343), (0.129, 0.029), (0.124, 0.019), (0.0, 0.019),
    ], (0, 0, 0), "cab_cream_mint", seg=32)
    base_ring = H.cyl("BaseRubberRing", (0, 0, 0.0), (0, 0, 0.016), 0.146, "rubber", seg=28)
    lining = H.lathe("InnerLiner", [
        (0, 0.024), (0.123, 0.024), (0.128, 0.032), (0.129, 0.270), (0.131, 0.286),
    ], (0, 0, 0), "satin_inox", seg=24)

    lid = H.lathe("LidDome", [
        (0.0, 0.398), (0.055, 0.394), (0.118, 0.378), (0.150, 0.356),
        (0.152, 0.346), (0.144, 0.346), (0.138, 0.355), (0.0, 0.386)
    ], (0, 0, 0), "satin_inox", seg=32)
    pedal_arm = H.box("PedalArm", (0, -0.155, 0.032), (0.034, 0.088, 0.014), "satin_inox", bevel=0.002)
    pedal_pad = H.box("PedalPad", (0, -0.192, 0.038), (0.072, 0.044, 0.016), "rubber", bevel=0.004)
    hinge = H.box("RearHinge", (0, 0.148, 0.342), (0.058, 0.024, 0.038), "satin_inox", bevel=0.003)

    return {
        "BinShell": [body, base_ring, lining],
        "BinLid": [lid],
        "BinPedal": [pedal_arm, pedal_pad],
        "BinHinge": [hinge],
    }


def build_clipboard_f101():
    """Hero varnished masonite hospital clipboard (exact 0.265 x 0.365 m) with crisp F101 front plane,
    stamped steel clip, brass rivets & ballpoint pen (shared by Room 101 wall AND Lan's right hand!)."""
    objs = []
    objs.append(H.box("Board", (0, 0.006, 0), (0.265, 0.011, 0.365), "wood", bevel=0.004, grain="Z"))
    objs.append(H.box("PaperUnder", (0.003, -0.002, -0.008), (0.224, 0.004, 0.312), "paper", bevel=0.001, rot=(0, 0.014, 0)))
    objs.append(W.make_uv_plane("PaperTop", (0, -0.0055, -0.006), (0.222, 0.308), "F101", rot=(math.radians(90), 0, 0)))
    objs.append(H.box("ClipPlate", (0, -0.012, 0.155), (0.105, 0.011, 0.036), "satin_inox", bevel=0.003))
    objs.append(H.pipe("ClipWire", [
        (-0.042, -0.017, 0.145), (-0.042, -0.025, 0.175),
        (0.042, -0.025, 0.175), (0.042, -0.017, 0.145)
    ], 0.004, "satin_inox", res=3))
    for rx in (-0.036, 0.036):
        objs.append(H.cyl(f"ClipRivet_{rx:.2f}", (rx, 0.012, 0.155), (rx, -0.019, 0.155), 0.005, "brass", seg=10))
    objs.append(H.cyl("BoardPen", (0.105, -0.013, 0.01), (0.118, -0.013, 0.145), 0.005, "enamel_blue", seg=10))
    return objs


def build_bed4_chart_holder():
    """Grounded satin-stainless bedside chart stanchion for Bed 4 (OBS101B) with crisp ObsCard UV plane."""
    objs = []
    objs.append(H.lathe("StandBase", [
        (0.0, 0.012), (0.168, 0.012), (0.172, 0.022), (0.155, 0.048),
        (0.042, 0.068), (0.022, 0.095), (0.0, 0.095)
    ], (0, 0, 0), "bed_enamel_green", seg=26))
    for k in range(3):
        a = k * math.tau / 3.0
        gx, gy = math.cos(a) * 0.145, math.sin(a) * 0.145
        objs.append(H.cyl(f"Glide_{k}", (gx, gy, 0.0), (gx, gy, 0.014), 0.016, "rubber", seg=10))
    objs.append(H.cyl("LowerPole", (0, 0, 0.08), (0, 0, 0.92), 0.015, "bed_enamel_green", seg=14))
    objs.append(H.cyl("LockCollar", (0, 0, 0.89), (0, 0, 0.95), 0.022, "satin_inox", seg=16, bevel=0.003))
    objs.append(H.cyl("UpperPole", (0, 0, 0.94), (0, 0, 1.54), 0.011, "satin_inox", seg=14))
    objs.append(H.cyl("TopCap", (0, 0, 1.535), (0, 0, 1.565), 0.016, "plastic_ivory", seg=12))
    objs.append(H.cyl("CrossBar", (-0.115, -0.018, 1.49), (0.115, -0.018, 1.49), 0.007, "satin_inox", seg=10))
    for hx in (-0.075, 0.075):
        objs.append(H.pipe(f"Hook_{hx:.2f}", [(hx, -0.018, 1.505), (hx, -0.045, 1.505), (hx, -0.045, 1.455)], 0.0045, "satin_inox", res=3))
    objs.append(H.box("AluminumPlate", (0, -0.044, 1.335), (0.235, 0.008, 0.305), "satin_inox", bevel=0.003))
    objs.append(H.box("BottomLip", (0, -0.056, 1.188), (0.235, 0.026, 0.012), "satin_inox", bevel=0.002))
    objs.append(H.box("SpringClip", (0, -0.054, 1.462), (0.088, 0.014, 0.026), "satin_inox", bevel=0.002))
    objs.append(W.make_uv_plane("ObsCardSheet", (0, -0.050, 1.332), (0.188, 0.250), "ObsCard", rot=(math.radians(90), 0, 0)))
    return objs


def build_dry_shelf():
    """Varnished archival timber shelving unit in Store Room (Kho giường: 2.12 x 0.54 x 2.35 m).
    STRICTLY preserves main E11 table top at Z=0.86m (shelf at 0.84, thick 0.042) and upper shelf at Z=1.74m!"""
    objs = []
    w, d, h = 2.12, 0.54, 2.35
    for sx in (-w * 0.5 + 0.03, w * 0.5 - 0.03):
        for sy in (-d * 0.5 + 0.03, d * 0.5 - 0.03):
            objs.append(H.box(f"Post_{sx:.2f}_{sy:.2f}", (sx, sy, h * 0.5), (0.058, 0.058, h), "wood", bevel=0.004, grain="Z"))
            # Corner iron reinforcing angle brackets at shelf levels
            for sz in (0.81, 1.69):
                objs.append(H.box(f"Bracket_{sx:.2f}_{sy:.2f}_{sz:.2f}", (sx, sy, sz), (0.066, 0.066, 0.036), "steel_dark", bevel=0.002))
    for k in range(8):
        px = -w * 0.5 + 0.14 + k * 0.262
        objs.append(H.box(f"BackPlank_{k}", (px, -d * 0.5 + 0.012, h * 0.5), (0.252, 0.018, h - 0.08), "wood_light", bevel=0.002, grain="Z"))
    for sz, thick in ((0.14, 0.028), (0.48, 0.026), (0.84, 0.042), (1.32, 0.026), (1.72, 0.036), (2.28, 0.034)):
        objs.append(H.box(f"Shelf_{sz:.2f}", (0, 0, sz), (w, d, thick), "wood_light" if sz in (0.84, 1.72) else "wood", bevel=0.004, grain="X"))
    for s in (-1, 1):
        objs.append(H.box(f"SidePanel_{s}", (s * (w * 0.5 - 0.014), 0, 0.48), (0.024, d - 0.04, 0.72), "wood", bevel=0.003, grain="Z"))

    # Labeled archive storage boxes & ledgers on non-E11 shelves (leaving Z=0.86 & Z=1.74 E11 bays completely clear!)
    for k in range(3):
        bx = -0.66 + k * 0.48
        objs.append(H.box(f"LowArchiveBox_{k}", (bx, 0.02, 0.285), (0.38, 0.40, 0.24), "kraft", bevel=0.005))
        objs.append(H.box(f"LowBoxLid_{k}", (bx, 0.02, 0.415), (0.40, 0.42, 0.042), "kraft", bevel=0.004))
        objs.append(H.box(f"LowBoxLabel_{k}", (bx, 0.224, 0.30), (0.14, 0.004, 0.072), "paper", bevel=0.001))
    spine_uvs = [(0.02, 0.38, 0.48, 0.48), (0.02, 0.26, 0.48, 0.36), (0.02, 0.14, 0.48, 0.24), (0.02, 0.02, 0.48, 0.12)]
    for k in range(6):
        bx = -0.82 + k * 0.082
        objs.append(H.box(f"MidBinder_{k}", (bx, 0.02, 1.48), (0.064, 0.32, 0.28),
                          "cloth_navy" if k % 2 == 0 else "kraft", bevel=0.004, rot=(0, (k - 2.5) * 0.02, 0)))
        sp = H.box(f"MidSpine_{k}", (bx, 0.182, 1.48), (0.052, 0.004, 0.23), "print:med_labels", bevel=0.001)
        H.set_planar_print_uv(sp, axis="Y", uv_rect=spine_uvs[k % 4], rot90=True)
        objs.append(sp)
    objs.append(H.box("TopArchiveBoxA", (-0.52, 0.02, 1.90), (0.44, 0.38, 0.30), "kraft", bevel=0.006))
    objs.append(H.box("TopArchiveBoxB", (0.05, 0.02, 1.88), (0.36, 0.38, 0.26), "kraft", bevel=0.006))
    return objs


def build_dismantled_bed_frame():
    """Emotional Store Room storytelling payoff (Kho giường): the missing 4th hospital bed from Room 101,
    dismantled and leaning against the wall with a hanging kraft inventory tag ('GIƯỜNG SỐ 04 · LƯU KHO 2012'),
    twine-tied rolled striped mattress & REAL cloth-simulated dusty stacked blankets!"""
    objs = []
    tilt = math.radians(-11)

    # Matching 1987 painted-green bed deck rails + 11 spring slats leaning against wall
    objs.append(H.box("FrameRailTop", (0, 0.04, 1.02), (1.92, 0.045, 0.055), "bed_enamel_green", bevel=0.005, rot=(tilt, 0, 0)))
    objs.append(H.box("FrameRailBot", (0, -0.12, 0.16), (1.92, 0.045, 0.055), "bed_enamel_green", bevel=0.005, rot=(tilt, 0, 0)))
    for i in range(11):
        sx = -0.88 + i * 0.176
        objs.append(H.box(f"SpringSlat_{i}", (sx, -0.04, 0.59), (0.042, 0.018, 0.84), "bed_enamel_green", bevel=0.003, rot=(tilt, 0, 0)))

    # Detached tubular headboard & footboard matching Room 101's Beds 1–3!
    for idx, (ox, oy, yaw) in enumerate(((-0.48, -0.22, 0.08), (0.42, -0.26, -0.11))):
        pts = C.make_rounded_u(0, 0.0, 0.98, 0.40, radius=0.085, arc_steps=6)
        ca, sa = math.cos(yaw), math.sin(yaw)
        wpts = [(ox + x * ca - (y - 0.14 * z) * sa, oy + x * sa + (y - 0.14 * z) * ca, z) for x, y, z in pts]
        objs.append(H.pipe(f"DetachedBoard_{idx}", wpts, 0.020, "bed_enamel_green", res=4))
        for bx in (-0.28, -0.12, 0.04, 0.20, 0.28):
            objs.append(H.cyl(f"BoardBar_{idx}_{bx:.2f}",
                              (ox + bx * ca, oy + bx * sa, 0.22),
                              (ox + bx * ca, oy + bx * sa - 0.12, 0.94), 0.009, "bed_enamel_green", seg=8))

    # Hanging kraft liquidation tag ('GIƯỜNG SỐ 04 · PHÒNG 101 · LƯU KHO 2012') tied prominently onto room-facing (+Y) rail!
    # At yaw=180 in Store Room (15.55, 0, 5.02), Blender +Y faces Unity -Z (straight toward the player entering Kho giường!)
    objs.append(H.pipe("TagString", [(0.16, 0.04, 1.00), (0.16, 0.08, 0.86)], 0.0028, "kraft", res=2))
    tag = H.box("Bed4InventoryTag", (0.16, 0.085, 0.745), (0.175, 0.005, 0.21), "print:med_labels", bevel=0.001, rot=(-0.08, 0, 0.06))
    H.set_planar_print_uv(tag, axis="Y", uv_rect=(0.51, 0.01, 0.99, 0.49), flip_u=True)
    objs.append(tag)

    # Stacked dusty hospital blankets with cloth-simulated top drape!
    for k, (bz, mat) in enumerate(((0.055, "blanket_jade"), (0.165, "cotton_sheet"), (0.275, "blanket_blue"))):
        objs.append(H.box(f"BlanketFold_{k}", (-1.18, -0.04, bz), (0.48, 0.36, 0.105), mat,
                          bevel=0.026, seg=3, rot=(0, 0, (k - 1) * 0.08)))
    col_stack = H.box("__ColStack", (-1.18, -0.04, 0.19), (0.48, 0.36, 0.34), "blanket_jade", bevel=0.02)
    col_floor = H.box("__ColFloor", (-1.18, -0.04, -0.02), (1.4, 1.4, 0.04), "cotton_sheet", bevel=0.0)

    def top_blanket_init(u, v):
        x = -1.18 + (u - 0.5) * 0.62
        y = -0.04 + (v - 0.5) * 0.52
        return (x, y, 0.385 + 0.012 * math.sin(u * 9.0 + v * 6.0))

    drape = W.create_cloth_grid("TopDrapeBlanket", 22, 18, top_blanket_init, "cotton_sheet")
    W.simulate_cloth_drape(
        drape, [col_stack, col_floor], frames=22, mass=0.45, tension=15.0, bending=0.45,
        collision_dist=0.008, solidify_thickness=0.012, cleanup_colliders=True,
    )
    objs.append(drape)

    # Twine-tied rolled striped hospital mattress leaning beside frame
    objs.append(H.cyl("RolledMattress", (1.14, -0.06, 0.0), (1.14, -0.06, 0.88), 0.175, "mattress_ticking", seg=20, bevel=0.022))
    for tz in (0.24, 0.62):
        objs.append(H.cyl(f"MattressTwine_{tz:.2f}", (1.14, -0.06, tz), (1.14, -0.06, tz + 0.014), 0.180, "kraft", seg=20))
    return objs


def build_patient_bag():
    """Slumped woven hospital canvas patient tote bag resting on Store Room shelf with brass zipper & handles."""
    objs = []

    def bag_pos(u, v):
        x = (u - 0.5) * 0.36
        y = (v - 0.5) * 0.23
        ex = math.sin(u * math.pi)
        ey = math.sin(v * math.pi)
        dome = 0.165 * (ex ** 0.55) * (ey ** 0.55)
        slump = -0.025 * math.exp(-((x / 0.09) ** 2 + (y / 0.06) ** 2))
        wrinkle = 0.008 * math.sin(u * 19.0 + v * 13.0) * ex * ey
        return (x, y, 0.016 + dome + slump + wrinkle)

    objs.append(H.sculpted_grid("BagBody", 24, 18, bag_pos, "cloth_olive", thickness=0.026))
    objs.append(H.box("BagBase", (0, 0, 0.014), (0.33, 0.20, 0.022), "cloth_olive", bevel=0.010, seg=3))
    objs.append(H.pipe("BagZipper", [
        (-0.125, 0, 0.145), (-0.05, 0, 0.165), (0.0, 0, 0.162), (0.05, 0, 0.165), (0.125, 0, 0.145)
    ], 0.0045, "brass", res=3))
    for sy in (-0.065, 0.065):
        objs.append(H.pipe(f"BagStrap_{sy:.2f}", [
            (-0.085, sy, 0.12), (-0.075, sy * 1.35, 0.205),
            (0.075, sy * 1.35, 0.205), (0.085, sy, 0.12)
        ], 0.008, "cotton_sheet", res=3))
    return objs


def run_all():
    W.export_ward_asset("CTCC_PROP_IVStand", build_iv_stand,
                        {"LOC03_IVStand": {"res": 1024, "bake_r": True}}, make_lods=True, cam_dir=(1.40, -1.50, 0.45), ao_samples=8)
    W.export_ward_asset("CTCC_PROP_ThermosAndMug", build_thermos_and_mug,
                        {"LOC03_ThermosMug": {"res": 1024, "bake_r": True}}, make_lods=True, cam_dir=(1.20, -1.55, 0.85), ao_samples=8)
    W.export_ward_asset("CTCC_PROP_WashbasinAndMat", build_washbasin_and_mat,
                        {"LOC03_WashbasinMat": {"res": 1024, "bake_r": True}}, make_lods=True, cam_dir=(1.25, -1.55, 0.95), ao_samples=8)
    W.export_ward_asset("CTCC_PROP_HoneycombSlippers", build_honeycomb_slippers,
                        {"LOC03_Slippers": {"res": 512, "bake_r": True}}, make_lods=False, cam_dir=(1.10, -1.45, 0.95), ao_samples=8)
    W.export_ward_asset("CTCC_PROP_CH01CareSupplies", build_care_supplies,
                        {"LOC03_CareProps": {"res": 1024, "bake_r": False}}, make_lods=False, cam_dir=(1.25, -1.45, 0.95), ao_samples=8)
    W.export_ward_asset("CTCC_PROP_PedalBinWorking", build_pedal_bin_working,
                        {"LOC03_PedalBin": {"res": 1024, "bake_r": True}}, make_lods=False, separate_parts=True, cam_dir=(1.25, -1.55, 0.75), ao_samples=8)
    W.export_ward_asset("CTCC_PROP_ClipboardF101", build_clipboard_f101,
                        {"LOC03_Clipboard": {"res": 1024, "bake_r": False}}, make_lods=False, cam_dir=(0.45, -1.90, 0.30), ao_samples=8)
    W.export_ward_asset("CTCC_PROP_Bed4ChartHolder", build_bed4_chart_holder,
                        {"LOC03_ChartHolder": {"res": 1024, "bake_r": False}}, make_lods=False, cam_dir=(0.75, -1.85, 0.45), ao_samples=8)
    W.export_ward_asset("CTCC_PROP_DryShelf", build_dry_shelf,
                        {"LOC03_DryShelf": {"res": 1024, "bake_r": True}}, make_lods=True, cam_dir=(0.85, 1.95, 0.55), ao_samples=8)
    W.export_ward_asset("CTCC_PROP_DismantledBedFrame", build_dismantled_bed_frame,
                        {"LOC03_StorePayoff": {"res": 2048, "bake_r": True}}, make_lods=True, cam_dir=(1.05, -1.85, 0.65), ao_samples=8)
    W.export_ward_asset("CTCC_PROP_PatientBag", build_patient_bag,
                        {"LOC03_PatientBag": {"res": 1024, "bake_r": True}}, make_lods=False, cam_dir=(1.15, -1.50, 0.95), ao_samples=8)


if __name__ == "__main__":
    run_all()
