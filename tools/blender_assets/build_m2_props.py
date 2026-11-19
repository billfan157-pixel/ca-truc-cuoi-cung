"""M2 High-Fidelity Medical Equipment, Exterior Storytelling Props & Tropical Flora built in Blender 4.5 LTS.

Covers LOC01, LOC02, LOC03 Store Room, and LOC04 Emergency Department:
- CTCC_PROP_CommuterMotorbike: Generic 2000s Vietnamese underbone motorbike parked outside side gate in rain (CH01 B01)
- CTCC_PROP_TerrazzoBench: Classic Vietnamese curved stone-chip courtyard bench (ghế đá mài)
- CTCC_PROP_EmergencyStretcher: Detailed LOC04 trauma/exam stretcher w/ crank backrest, side rails & IV mount
- CTCC_PROP_OxygenCylinderCart: Heavy green steel medical O2 tank w/ regulator, humidifier & 2-wheel steel hand truck
- CTCC_PROP_PatientMonitorCart: Rolling early-2000s multi-parameter CRT/LCD ECG monitor cart w/ coiled leads
- CTCC_PROP_InstrumentTrolley: 2-tier stainless steel emergency dressing cart w/ kidney tray, jars & drum
- CTCC_PROP_WallGasManifold: Wall-mounted hospital O2/vacuum outlet console + glass suction bottle
- CTCC_PROP_DismantledBedFrame: Missing 4th steel hospital bed dismantled against Store Room wall + folded blankets (E12 payoff!)
- CTCC_PROP_ReceptionDetails: Potted snake plant (cây lưỡi hổ), porcelain teapot & cups (Lan's tea), 2005 desk phone & SMS handset
- CTCC_PROP_EmergencyLightBox: Ceiling-suspended red/cream institutional CẤP CỨU illuminated lightbox
- CTCC_ENV_TropicalShrub: Lush curved 3D elephant-ear (cây ráy) & wet monsoon undergrowth clump
"""

import math
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import common as C


def build_commuter_motorbike():
    """Generic 2000s Vietnamese underbone commuter motorbike (no real brand/logos per AR03),
    length ~1.88 m along Y (front toward +Y), leaning subtly on kickstand!"""
    objs = []

    def wheel(name, cy, radius=0.285):
        w_objs = []
        # Tire torus built as segmented circular pipe
        pts = [(0.0, cy + math.cos(a) * radius, radius + 0.03 + math.sin(a) * radius)
               for a in [i * math.tau / 28 for i in range(28)]]
        w_objs.append(C.make_pipe(f"{name}_Tire", pts, 0.044, "Rubber", bevel_res=4, cyclic=True))
        rim_pts = [(0.0, cy + math.cos(a) * (radius - 0.045), radius + 0.03 + math.sin(a) * (radius - 0.045))
                   for a in [i * math.tau / 24 for i in range(24)]]
        w_objs.append(C.make_pipe(f"{name}_Rim", rim_pts, 0.015, "Steel", bevel_res=3, cyclic=True))
        # Hub + 12 wire spokes
        cz = radius + 0.03
        w_objs.append(C.make_cylinder(f"{name}_Hub", (-0.055, cy, cz), (0.055, cy, cz), 0.052, "Steel", segments=14))
        for k in range(12):
            ang = k * math.tau / 12.0
            sx = 0.032 if k % 2 == 0 else -0.032
            w_objs.append(C.make_cylinder(
                f"{name}_Spoke_{k}", (sx, cy, cz),
                (0.0, cy + math.cos(ang) * (radius - 0.05), cz + math.sin(ang) * (radius - 0.05)),
                0.0032, "Steel", segments=6))
        return w_objs

    objs.extend(wheel("FrontWheel", 0.62, 0.285))
    objs.extend(wheel("RearWheel", -0.62, 0.280))

    # Sweeping underbone step-through spine & rear body fairing (aged maroon-red metallic lacquer)
    objs.append(C.make_box("MainBody", (0, -0.18, 0.63), (0.24, 0.92, 0.24), "MotorbikePaint", bevel=0.04, segments=3, rot=(math.radians(-6), 0, 0)))
    objs.append(C.make_box("TailCowling", (0, -0.74, 0.69), (0.21, 0.36, 0.16), "MotorbikePaint", bevel=0.03, segments=2, rot=(math.radians(8), 0, 0)))
    objs.append(C.make_box("SpineColumn", (0, 0.42, 0.72), (0.16, 0.18, 0.46), "MotorbikePaint", bevel=0.025, rot=(math.radians(22), 0, 0)))
    # Classic ivory-white Vietnamese underbone front leg shields (yếm xe!)
    for sx in (-0.17, 0.17):
        objs.append(C.make_box(f"LegShield_{sx:.2f}", (sx, 0.36, 0.56), (0.045, 0.28, 0.52), "PlasticIvory", bevel=0.015, rot=(math.radians(22), math.radians(sx * -40), math.radians(sx * -55))))
    objs.append(C.make_box("FrontFender", (0, 0.60, 0.60), (0.14, 0.36, 0.06), "MotorbikePaint", bevel=0.02, rot=(math.radians(-18), 0, 0)))
    objs.append(C.make_box("RearFender", (0, -0.84, 0.52), (0.14, 0.12, 0.30), "Black", bevel=0.01, rot=(math.radians(25), 0, 0)))

    # Front telescopic forks + handlebar cowl + headlamp + mirrors
    for sx in (-0.075, 0.075):
        objs.append(C.make_cylinder(f"Fork_{sx:.2f}", (sx, 0.62, 0.315), (sx, 0.41, 0.94), 0.020, "Steel", segments=12))
    objs.append(C.make_box("HeadCowl", (0, 0.43, 0.96), (0.34, 0.22, 0.15), "MotorbikePaint", bevel=0.03, segments=3))
    objs.append(C.make_box("HeadlampGlass", (0, 0.545, 0.96), (0.17, 0.03, 0.10), "WarmGlow", bevel=0.008))
    # Handlebar grips & brake lever
    objs.append(C.make_cylinder("BarGrips", (-0.33, 0.39, 0.96), (0.33, 0.39, 0.96), 0.018, "Rubber", segments=12))
    for sx in (-0.21, 0.21):
        objs.append(C.make_pipe(f"MirrorStem_{sx:.2f}", [(sx, 0.41, 1.01), (sx * 1.35, 0.40, 1.15)], 0.0055, "Steel", bevel_res=3))
        objs.append(C.make_box(f"MirrorHead_{sx:.2f}", (sx * 1.48, 0.40, 1.17), (0.105, 0.028, 0.075), "Black", bevel=0.010, rot=(0, 0, sx * 0.4)))

    # Contoured black vinyl stepped saddle & chrome rear grab rail
    objs.append(C.make_box("Saddle", (0, -0.24, 0.78), (0.27, 0.74, 0.11), "Black", bevel=0.042, segments=3, rot=(math.radians(-4), 0, 0)))
    objs.append(C.make_pipe("GrabRail", [(-0.12, -0.60, 0.77), (-0.13, -0.84, 0.80), (0.13, -0.84, 0.80), (0.12, -0.60, 0.77)], 0.012, "Steel", bevel_res=3))

    # Horizontal air-cooled engine block, chrome exhaust muffler & twin rear shock absorbers
    objs.append(C.make_box("EngineBlock", (0, 0.08, 0.33), (0.22, 0.30, 0.22), "Steel", bevel=0.015))
    objs.append(C.make_pipe("ExhaustPipe", [(0.08, 0.12, 0.26), (0.14, -0.08, 0.24), (0.15, -0.78, 0.40)], 0.034, "Steel", bevel_res=4))
    objs.append(C.make_box("ChainGuard", (-0.12, -0.34, 0.31), (0.05, 0.56, 0.12), "MetalOld", bevel=0.01))
    for sx in (-0.115, 0.115):
        objs.append(C.make_cylinder(f"Shock_{sx:.2f}", (sx, -0.60, 0.34), (sx, -0.50, 0.67), 0.024, "Steel", segments=10))
    # Rear red taillight lens + license plate bracket (blank fictional plate)
    objs.append(C.make_box("TailLight", (0, -0.92, 0.71), (0.16, 0.03, 0.075), "RedPaint", bevel=0.008))
    objs.append(C.make_box("Plate", (0, -0.90, 0.56), (0.16, 0.012, 0.11), "PlasticIvory", bevel=0.002, rot=(math.radians(18), 0, 0)))
    # Side kickstand
    objs.append(C.make_cylinder("Kickstand", (-0.11, -0.06, 0.24), (-0.28, -0.14, 0.01), 0.011, "MetalOld", segments=8))
    # Footpegs
    objs.append(C.make_cylinder("FootpegBar", (-0.26, 0.04, 0.27), (0.26, 0.04, 0.27), 0.015, "Rubber", segments=10))
    return objs


def build_terrazzo_bench():
    """Classic Vietnamese public hospital courtyard terrazzo bench (ghế đá mài: 1.55m wide, curved concrete legs & back)."""
    objs = []
    # Two curved sculptural cast-concrete legs
    for sx in (-0.54, 0.54):
        objs.append(C.make_box(f"BenchLegBase_{sx:.2f}", (sx, 0.0, 0.05), (0.10, 0.52, 0.10), "TerrazzoBench", bevel=0.012))
        objs.append(C.make_box(f"BenchLegUpright_{sx:.2f}", (sx, 0.02, 0.24), (0.09, 0.40, 0.34), "TerrazzoBench", bevel=0.014))
        objs.append(C.make_box(f"BenchBackSupport_{sx:.2f}", (sx, 0.22, 0.62), (0.085, 0.09, 0.48), "TerrazzoBench", bevel=0.012, rot=(math.radians(-14), 0, 0)))
    # Smooth polished terrazzo seat slab & slightly reclined backrest slab
    objs.append(C.make_box("BenchSeat", (0, -0.02, 0.43), (1.56, 0.44, 0.065), "TerrazzoBench", bevel=0.018, segments=3))
    objs.append(C.make_box("BenchBack", (0, 0.23, 0.72), (1.56, 0.055, 0.34), "TerrazzoBench", bevel=0.016, segments=3, rot=(math.radians(-14), 0, 0)))
    return objs


def build_emergency_stretcher():
    """Detailed LOC04 Vietnamese Emergency Department trauma/examination stretcher (0.76 x 2.02 m, height 0.84 m)
    with stainless frame, raised fold-down tubular side rails, cranked headrest, cracked vinyl pad & IV rod."""
    objs = []
    # Lower tubular carriage chassis + wire shelf
    for sx in (-0.32, 0.32):
        objs.append(C.make_cylinder(f"LowRail_{sx:.2f}", (sx, -0.78, 0.26), (sx, 0.78, 0.26), 0.017, "Steel", segments=12))
    for sy in (-0.76, 0.76):
        objs.append(C.make_cylinder(f"LowCross_{sy:.2f}", (-0.32, sy, 0.26), (0.32, sy, 0.26), 0.017, "Steel", segments=12))
    for i in range(9):
        wy = -0.64 + i * 0.16
        objs.append(C.make_cylinder(f"WireShelf_{i}", (-0.30, wy, 0.27), (0.30, wy, 0.27), 0.005, "Steel", segments=6))

    # 4 Heavy rubber stretcher casters (Ø14 cm) + vertical support columns
    for sx in (-0.32, 0.32):
        for sy in (-0.74, 0.74):
            objs.append(C.make_cylinder(f"Wheel_{sx:.2f}_{sy:.2f}", (sx - 0.022, sy, 0.072), (sx + 0.022, sy, 0.072), 0.070, "Rubber", segments=16))
            objs.append(C.make_cylinder(f"WheelHub_{sx:.2f}_{sy:.2f}", (sx - 0.026, sy, 0.072), (sx + 0.026, sy, 0.072), 0.032, "Steel", segments=12))
            objs.append(C.make_cylinder(f"Col_{sx:.2f}_{sy:.2f}", (sx, sy, 0.14), (sx, sy, 0.74), 0.020, "Steel", segments=12))
            # Corner rubber wall-bumpers
            objs.append(C.make_cylinder(f"Bumper_{sx:.2f}_{sy:.2f}", (sx * 1.15, sy * 1.28, 0.72), (sx * 1.15, sy * 1.28, 0.77), 0.048, "Rubber", segments=14))

    # Upper deck perimeter frame & slatted steel sheet
    objs.append(C.make_box("DeckPan", (0, 0, 0.745), (0.70, 1.94, 0.045), "MetalPaint", bevel=0.008))
    # Vinyl trauma mattress + gently propped head cushion + draped hospital sheet
    objs.append(C.make_box("TraumaMattress", (0, -0.14, 0.81), (0.68, 1.48, 0.092), "MattressOld", bevel=0.028, segments=3))
    objs.append(C.make_box("HeadRestPad", (0, 0.73, 0.87), (0.68, 0.44, 0.088), "MattressOld", bevel=0.026, segments=3, rot=(math.radians(18), 0, 0)))
    objs.append(C.make_box("Pillow", (0, 0.74, 0.94), (0.48, 0.28, 0.095), "CottonOld", bevel=0.035, segments=3, rot=(math.radians(16), 0, 0.04)))

    # Raised stainless side guard rails (both sides) + push handles at foot & head
    for sx in (-0.37, 0.37):
        objs.append(C.make_pipe(f"SideRail_{sx:.2f}", [(sx, -0.52, 0.76), (sx, -0.48, 1.04), (sx, 0.48, 1.04), (sx, 0.52, 0.76)], 0.013, "Steel", bevel_res=4))
        for ry in (-0.20, 0.0, 0.20):
            objs.append(C.make_cylinder(f"RailBal_{sx:.2f}_{ry:.1f}", (sx, ry, 0.76), (sx, ry, 1.04), 0.008, "Steel", segments=8))
    for sy in (-0.98, 0.98):
        objs.append(C.make_pipe(f"PushBar_{sy:.2f}", [(-0.28, sy, 0.76), (-0.28, sy, 0.98), (0.28, sy, 0.98), (0.28, sy, 0.76)], 0.014, "Steel", bevel_res=4))

    # Corner-mounted IV pole attached to head-right corner
    objs.append(C.make_cylinder("ER_IVPole", (0.35, 0.92, 0.74), (0.35, 0.92, 1.88), 0.011, "Steel", segments=10))
    objs.append(C.make_pipe("ER_IVHook", [(0.24, 0.92, 1.84), (0.35, 0.92, 1.88), (0.46, 0.92, 1.84)], 0.005, "Steel", bevel_res=3))
    return objs


def build_oxygen_cylinder_cart():
    """Heavy Vietnamese hospital painted-green medical O2 cylinder (Ø22x145cm) with brass valve, gauges,
    bubble humidifier jar & welded 2-wheeled steel hand truck with safety chain."""
    objs = []
    # Tall domed high-pressure medical oxygen cylinder
    objs.append(C.make_lathe("OxygenTank", [
        (0.0, 0.06), (0.108, 0.06), (0.112, 0.08), (0.112, 1.12),
        (0.102, 1.20), (0.072, 1.27), (0.036, 1.32), (0.036, 1.37), (0.0, 1.37)
    ], (0, 0.02, 0), "GreenOxygen", segments=26))
    # Brass neck collar, top handwheel valve, pressure regulator & dual gauges
    objs.append(C.make_cylinder("ValveStem", (0, 0.02, 1.37), (0, 0.02, 1.49), 0.018, "Brass", segments=14))
    wheel_pts = [(0.042 * math.cos(k * math.tau / 14), 0.02 + 0.042 * math.sin(k * math.tau / 14), 1.50) for k in range(14)]
    objs.append(C.make_pipe("ValveWheel", wheel_pts, 0.008, "Black", bevel_res=3, cyclic=True))
    objs.append(C.make_cylinder("RegArm", (0, 0.02, 1.43), (0.11, -0.04, 1.43), 0.014, "Brass", segments=12))
    # Twin dial gauges
    for gx, gz in ((0.085, 1.49), (0.145, 1.47)):
        objs.append(C.make_cylinder(f"Gauge_{gx:.2f}", (gx, -0.02, gz), (gx, -0.065, gz), 0.030, "Steel", segments=16))
        objs.append(C.make_cylinder(f"GaugeFace_{gx:.2f}", (gx, -0.063, gz), (gx, -0.068, gz), 0.025, "Paper", segments=16))
    # Clear glass oxygen bubble humidifier bottle hanging from regulator
    objs.append(C.make_cylinder("HumidifierJar", (0.12, -0.04, 1.24), (0.12, -0.04, 1.38), 0.032, "SalineFluid", segments=16))
    objs.append(C.make_cylinder("HumidifierCap", (0.12, -0.04, 1.38), (0.12, -0.04, 1.41), 0.034, "PlasticIvory", segments=16))

    # 2-Wheeled tubular steel hand truck cart + base toe-plate + retaining collar & chain
    objs.append(C.make_box("CartBasePlate", (0, 0.02, 0.05), (0.30, 0.30, 0.016), "MetalOld", bevel=0.004))
    objs.append(C.make_pipe("CartFrame", [
        (-0.14, 0.14, 0.06), (-0.14, 0.14, 1.18), (-0.14, 0.25, 1.24),
        (0.14, 0.25, 1.24), (0.14, 0.14, 1.18), (0.14, 0.14, 0.06)
    ], 0.014, "MetalOld", bevel_res=4))
    # Curved cradle collar & safety chain wrapping tank at Z=0.82m
    collar = [(0.125 * math.cos(a), 0.02 + 0.125 * math.sin(a), 0.82) for a in [i * math.tau / 20 for i in range(20)]]
    objs.append(C.make_pipe("TankCradle", collar, 0.010, "Steel", bevel_res=3, cyclic=True))
    # Twin spoked rubber cart wheels on rear axle
    objs.append(C.make_cylinder("Axle", (-0.23, 0.17, 0.105), (0.23, 0.17, 0.105), 0.012, "Steel", segments=10))
    for sx in (-0.20, 0.20):
        objs.append(C.make_cylinder(f"CartWheel_{sx:.2f}", (sx - 0.022, 0.17, 0.105), (sx + 0.022, 0.17, 0.105), 0.102, "Rubber", segments=18))
        objs.append(C.make_cylinder(f"CartHub_{sx:.2f}", (sx - 0.026, 0.17, 0.105), (sx + 0.026, 0.17, 0.105), 0.048, "MetalOld", segments=14))
    return objs


def build_patient_monitor_cart():
    """Early-2000s rolling Vietnamese Emergency Department vital-signs monitor on steel mobile stand (height 1.38 m).
    Screen faces -Y so Unity can map MonitorECG display cleanly!"""
    objs = []
    # 5-leg caster base + sturdy central steel column
    for k in range(5):
        a = k * math.tau / 5.0
        ex, ey = math.cos(a) * 0.28, math.sin(a) * 0.28
        objs.append(C.make_cylinder(f"BaseArm_{k}", (0, 0, 0.085), (ex, ey, 0.065), 0.016, "MetalPaint", segments=10))
        objs.append(C.make_cylinder(f"BaseWheel_{k}", (ex - 0.012, ey, 0.028), (ex + 0.012, ey, 0.028), 0.028, "Rubber", segments=12))
    objs.append(C.make_cylinder("CenterPole", (0, 0, 0.08), (0, 0, 1.02), 0.026, "Steel", segments=16))
    # Accessory wire basket midway down pole
    objs.append(C.make_box("CuffBasket", (0, -0.10, 0.72), (0.28, 0.18, 0.13), "MetalPaint", bevel=0.008))
    objs.append(C.make_box("MonitorShelf", (0, 0, 1.02), (0.38, 0.30, 0.026), "MetalPaint", bevel=0.005))

    # Boxy early-2000s cream-ivory medical monitor housing (tilted slightly upward for standing nurse view)
    objs.append(C.make_box("MonitorBody", (0, 0.01, 1.19), (0.34, 0.25, 0.29), "PlasticIvory", bevel=0.016, segments=3, rot=(math.radians(-6), 0, 0)))
    objs.append(C.make_box("MonitorBezel", (0, -0.118, 1.20), (0.30, 0.024, 0.23), "Black", bevel=0.006, rot=(math.radians(-6), 0, 0)))
    objs.append(C.make_box("MonitorCRT", (0, -0.128, 1.205), (0.265, 0.010, 0.195), "MonitorScreen", bevel=0.002, rot=(math.radians(-6), 0, 0)))
    # Top carrying handle + front rotary control dial + push buttons
    objs.append(C.make_pipe("MonitorHandle", [(-0.11, 0, 1.33), (-0.11, 0, 1.385), (0.11, 0, 1.385), (0.11, 0, 1.33)], 0.011, "MetalOld", bevel_res=3))
    objs.append(C.make_cylinder("RotaryKnob", (0.115, -0.125, 1.08), (0.115, -0.148, 1.08), 0.015, "EnamelBlue", segments=14))
    for i in range(4):
        objs.append(C.make_box(f"Btn_{i}", (-0.10 + i * 0.048, -0.128, 1.078), (0.028, 0.012, 0.016), "MetalOld", bevel=0.002))
    # Hanging coiled ECG lead cables on right side of cart
    cable = [(0.18 + 0.03 * math.sin(k * 0.9), 0.02 + 0.035 * math.cos(k * 0.9), 1.12 - k * 0.038) for k in range(14)]
    objs.append(C.make_pipe("ECGCable", cable, 0.0055, "Black", bevel_res=3))
    return objs


def build_instrument_trolley():
    """2-Tier brushed stainless Vietnamese ER dressing/instrument trolley (0.72 x 0.46 x 0.86 m)
    with guard rails, kidney basin, cotton sterilization drum, glass antiseptic bottles & metal forceps."""
    objs = []
    # 4 Corner stainless tubular legs + casters
    for sx in (-0.33, 0.33):
        for sy in (-0.20, 0.20):
            objs.append(C.make_cylinder(f"TLeg_{sx:.2f}_{sy:.2f}", (sx, sy, 0.06), (sx, sy, 0.86), 0.013, "Steel", segments=10))
            objs.append(C.make_cylinder(f"TCaster_{sx:.2f}_{sy:.2f}", (sx - 0.012, sy, 0.034), (sx + 0.012, sy, 0.034), 0.034, "Rubber", segments=12))
    # Two stainless trays + perimeter guard rails + end push handles
    for tz in (0.28, 0.78):
        objs.append(C.make_box(f"Tray_{tz:.2f}", (0, 0, tz), (0.68, 0.42, 0.022), "Steel", bevel=0.004))
        objs.append(C.make_pipe(f"Guard_{tz:.2f}", [
            (-0.33, -0.20, tz + 0.065), (0.33, -0.20, tz + 0.065), (0.33, 0.20, tz + 0.065), (-0.33, 0.20, tz + 0.065)
        ], 0.006, "Steel", bevel_res=3, cyclic=True))
    objs.append(C.make_pipe("PushHandleL", [(-0.33, -0.20, 0.86), (-0.38, -0.20, 0.91), (-0.38, 0.20, 0.91), (-0.33, 0.20, 0.86)], 0.012, "Steel", bevel_res=3))

    # Medical instruments on top tray: sterilization cotton drum (hộp hấp bông), kidney tray, amber bottles
    objs.append(C.make_cylinder("CottonDrum", (-0.17, 0.02, 0.792), (-0.17, 0.02, 0.915), 0.088, "Steel", segments=22, bevel=0.006))
    objs.append(C.make_cylinder("DrumLid", (-0.17, 0.02, 0.912), (-0.17, 0.02, 0.930), 0.092, "Steel", segments=22, bevel=0.004))
    objs.append(C.make_box("KidneyTray", (0.12, -0.06, 0.806), (0.21, 0.11, 0.032), "Steel", bevel=0.014, segments=3, rot=(0, 0, 0.14)))
    for bx, by, bh, bmat in ((0.06, 0.09, 0.135, "Glass"), (0.15, 0.11, 0.115, "RedPaint"), (0.23, 0.07, 0.125, "SalineFluid")):
        objs.append(C.make_cylinder(f"Bottle_{bx:.2f}", (bx, by, 0.792), (bx, by, 0.792 + bh), 0.028, bmat, segments=14, bevel=0.004))
        objs.append(C.make_cylinder(f"Cap_{bx:.2f}", (bx, by, 0.792 + bh), (bx, by, 0.792 + bh + 0.022), 0.014, "Black", segments=12))
    # Folded sterile green surgical drapes on lower shelf
    objs.append(C.make_box("SterileStackA", (-0.10, 0.0, 0.33), (0.34, 0.28, 0.08), "Textile", bevel=0.014, segments=2))
    objs.append(C.make_box("SterileStackB", (0.18, 0.02, 0.34), (0.18, 0.24, 0.10), "Cotton", bevel=0.012, segments=2))
    return objs


def build_wall_gas_manifold():
    """Wall-mounted hospital emergency O2/vacuum headwall panel + flowmeter + glass suction jar (back Y=0, protrudes -Y)."""
    objs = []
    objs.append(C.make_box("HeadwallStrip", (0, -0.04, 0), (1.35, 0.08, 0.22), "MetalPaint", bevel=0.008))
    # O2 green outlet + Vacuum yellow/ivory outlet + electric duplex socket
    for ox, mat in ((-0.42, "GreenOxygen"), (-0.14, "SlipperYellow"), (0.36, "PlasticIvory")):
        objs.append(C.make_box(f"OutletBox_{ox:.2f}", (ox, -0.085, 0), (0.13, 0.04, 0.13), mat, bevel=0.005))
        objs.append(C.make_cylinder(f"OutletPort_{ox:.2f}", (ox, -0.095, 0), (ox, -0.125, 0), 0.022, "Steel", segments=14))
    # Vertical glass flowmeter column + bracketed suction bottle
    objs.append(C.make_cylinder("Flowmeter", (-0.42, -0.135, -0.10), (-0.42, -0.135, 0.08), 0.014, "Glass", segments=12))
    objs.append(C.make_cylinder("SuctionJar", (-0.14, -0.12, -0.36), (-0.14, -0.12, -0.14), 0.058, "SalineFluid", segments=18))
    objs.append(C.make_cylinder("SuctionLid", (-0.14, -0.12, -0.14), (-0.14, -0.12, -0.115), 0.062, "Black", segments=16))
    objs.append(C.make_pipe("SuctionTube", [(-0.14, -0.12, -0.11), (-0.02, -0.14, -0.06), (0.04, -0.13, -0.22), (-0.06, -0.13, -0.34)], 0.006, "PlasticIvory", bevel_res=3))
    return objs


def build_dismantled_bed_frame():
    """Physical storytelling payoff for E12 in Store Room (Kho giường): the missing 4th hospital bed frame
    disassembled & leaning against the storage wall, flanked by rolled mattress & stacked folded ward blankets!"""
    objs = []
    # Main steel bed frame standing upright on its side, leaning ~11 degrees against rear wall (Y=+0.15)
    tilt = math.radians(-11)
    objs.append(C.make_box("FrameRailTop", (0, 0.04, 1.02), (1.92, 0.045, 0.055), "MetalOld", bevel=0.005, rot=(tilt, 0, 0)))
    objs.append(C.make_box("FrameRailBot", (0, -0.12, 0.16), (1.92, 0.045, 0.055), "MetalOld", bevel=0.005, rot=(tilt, 0, 0)))
    for i in range(11):
        sx = -0.88 + i * 0.176
        objs.append(C.make_box(f"SpringSlat_{i}", (sx, -0.04, 0.59), (0.042, 0.018, 0.84), "MetalOld", bevel=0.003, rot=(tilt, 0, 0)))
    # Detached tubular headboard & footboard leaning slightly askew in front!
    for idx, (ox, oy, yaw) in enumerate(((-0.48, -0.22, 0.08), (0.42, -0.26, -0.11))):
        pts = C.make_rounded_u(0, 0.0, 0.96, 0.42, radius=0.08, arc_steps=6)
        ca, sa = math.cos(yaw), math.sin(yaw)
        wpts = [(ox + x * ca - (y - 0.14 * z) * sa, oy + x * sa + (y - 0.14 * z) * ca, z) for x, y, z in pts]
        objs.append(C.make_pipe(f"DetachedBoard_{idx}", wpts, 0.020, "MetalOld", bevel_res=4))
        for bx in (-0.28, -0.10, 0.10, 0.28):
            objs.append(C.make_cylinder(f"BoardBar_{idx}_{bx:.2f}", (ox + bx * ca, oy + bx * sa, 0.22),
                                        (ox + bx * ca, oy + bx * sa - 0.12, 0.92), 0.009, "MetalOld", segments=8))
    # Stacked folded dusty hospital blankets & rolled spare vinyl mattress beside frame
    for k, (bz, mat) in enumerate(((0.06, "TextileOld"), (0.18, "CottonOld"), (0.30, "TextileOld"), (0.42, "CottonOld"))):
        objs.append(C.make_box(f"BlanketStack_{k}", (-1.18, -0.04, bz), (0.46, 0.36, 0.11), mat, bevel=0.022, segments=3, rot=(0, 0, (k % 2 - 0.5) * 0.12)))
    objs.append(C.make_cylinder("RolledMattress", (1.16, -0.06, 0.0), (1.16, -0.06, 0.88), 0.18, "MattressOld", segments=18, bevel=0.02))
    return objs


def build_reception_details():
    """LOC02 storytelling cluster: Glazed ceramic pot w/ Vietnamese snake plant (cây lưỡi hổ) +
    Porcelain teapot & two tea cups on tray (cốc trà của Lan, Core 04 CH01 B03) + vintage 2005 monochrome mobile phone."""
    plant = []
    # Glazed ceramic floor pot (height 0.42 m)
    plant.append(C.make_lathe("CeramicPot", [
        (0.0, 0.0), (0.14, 0.0), (0.18, 0.20), (0.20, 0.38), (0.215, 0.41),
        (0.19, 0.42), (0.175, 0.36), (0.0, 0.36)
    ], (0, 0, 0), "Ceramic", segments=24))
    plant.append(C.make_cylinder("PotSoil", (0, 0, 0.33), (0, 0, 0.37), 0.175, "Bark", segments=18))
    # 11 tall sword-like snake plant blades radiating upward
    for i in range(11):
        ang = i * math.tau / 11.0
        r = 0.04 + (i % 3) * 0.038
        h = 0.44 + (i % 4) * 0.09
        tilt = 0.08 + (i % 3) * 0.08
        bx, by = math.cos(ang) * r, math.sin(ang) * r
        plant.append(C.make_box(f"SnakeBlade_{i}", (bx, by, 0.36 + h * 0.5), (0.052, 0.010, h), "GreenOxygen",
                                bevel=0.004, rot=(math.sin(ang) * tilt, -math.cos(ang) * tilt, ang)))

    tea = []
    # Small lacquered wooden tea tray + porcelain teapot + 2 cups (Lan's tea cup!)
    tea.append(C.make_box("TeaTray", (0, 0, 0.012), (0.34, 0.24, 0.018), "WoodDoor", bevel=0.004))
    tea.append(C.make_lathe("TeapotBody", [
        (0.0, 0.02), (0.048, 0.02), (0.068, 0.065), (0.064, 0.115), (0.042, 0.135),
        (0.044, 0.148), (0.025, 0.155), (0.012, 0.170), (0.0, 0.170)
    ], (-0.055, 0.0, 0), "Ceramic", segments=20))
    tea.append(C.make_pipe("TeapotSpout", [(-0.002, 0.0, 0.075), (0.035, 0.0, 0.112), (0.048, 0.0, 0.132)], 0.009, "Ceramic", bevel_res=3))
    tea.append(C.make_pipe("TeapotHandle", [(-0.115, 0.0, 0.122), (-0.152, 0.0, 0.100), (-0.145, 0.0, 0.055), (-0.112, 0.0, 0.048)], 0.007, "Ceramic", bevel_res=3))
    for cx, cy in ((0.085, 0.045), (0.095, -0.055)):
        tea.append(C.make_lathe(f"TeaCup_{cy:.2f}", [
            (0.0, 0.02), (0.024, 0.02), (0.032, 0.070), (0.028, 0.070), (0.022, 0.026), (0.0, 0.026)
        ], (cx, cy, 0), "Ceramic", segments=16))
        tea.append(C.make_lathe(f"TeaCupRim_{cy:.2f}", [(0.030, 0.067), (0.033, 0.071), (0.028, 0.071)], (cx, cy, 0), "EnamelBlue", segments=16))

    return {"PottedPlant": plant, "LanTeaSet": tea}


def build_emergency_lightbox():
    """Ceiling-suspended illuminated Vietnamese institutional CẤP CỨU lightbox (1.16 x 0.38 x 0.14 m)
    with aluminum perimeter frame & dual suspension rods."""
    objs = []
    objs.append(C.make_box("LightBoxCase", (0, 0, 0), (1.18, 0.14, 0.40), "MetalPaint", bevel=0.008))
    objs.append(C.make_box("LightBoxFaceF", (0, -0.071, 0), (1.12, 0.008, 0.34), "CrimsonGlow", bevel=0.002))
    objs.append(C.make_box("LightBoxFaceB", (0, 0.071, 0), (1.12, 0.008, 0.34), "CrimsonGlow", bevel=0.002))
    for rx in (-0.42, 0.42):
        objs.append(C.make_cylinder(f"HangRod_{rx:.2f}", (rx, 0, 0.20), (rx, 0, 0.78), 0.011, "Steel", segments=10))
    return objs


def build_tropical_shrub():
    """Lush curved 3D monsoon courtyard undergrowth (elephant-ear cây ráy & ferns) using curved alpha-clip frond cards."""
    objs = []
    # 7 crossing arched frond planes radiating naturally from root cluster
    specs = [
        (0.0, 0.0, 0.0, 1.05, 0.98, 0.18),
        (0.18, 0.12, 0.95, 0.92, 0.88, 0.26),
        (-0.20, 0.14, 2.05, 0.96, 0.92, 0.24),
        (0.16, -0.18, 3.15, 0.86, 0.82, 0.28),
        (-0.18, -0.15, 4.30, 0.90, 0.86, 0.22),
        (0.05, 0.22, 5.25, 0.78, 0.74, 0.32),
        (-0.08, -0.22, 1.50, 0.76, 0.72, 0.30),
    ]
    for idx, (ox, oy, yaw, w, h, droop) in enumerate(specs):
        ca, sa = math.cos(yaw), math.sin(yaw)
        nx, ny = 4, 5
        verts, faces = [], []
        for j in range(ny + 1):
            for i in range(nx + 1):
                u = i / float(nx)
                v = j / float(ny)
                lx = (u - 0.5) * w
                # Outward arch + tip droop like real monsoon elephant-ear leaves!
                ly = v * 0.45 * droop + (v * v) * 0.32
                lz = v * h - (v * v * v) * droop * 0.48 - abs(u - 0.5) * 0.09
                wx = ox + lx * ca - ly * sa
                wy = oy + lx * sa + ly * ca
                verts.append((wx, wy, max(0.0, lz)))
                if i and j:
                    k = j * (nx + 1) + i
                    faces.append((k - nx - 2, k - nx - 1, k, k - 1))
        mesh = bpy.data.meshes.new(f"ShrubCard_{idx}_mesh")
        mesh.from_pydata(verts, [], faces)
        mesh.update()
        uv = mesh.uv_layers.new(name="UVMap")
        for poly in mesh.polygons:
            poly.use_smooth = True
            for li in poly.loop_indices:
                vi = mesh.loops[li].vertex_index
                j, i = divmod(vi, nx + 1)
                uv.data[li].uv = (i / float(nx), j / float(ny))
        obj = bpy.data.objects.new(f"ShrubCard_{idx}", mesh)
        bpy.context.collection.objects.link(obj)
        C.assign_mat(obj, "TropicalShrub")
        objs.append(obj)
    return objs


def run_all():
    C.export_asset("CTCC_PROP_CommuterMotorbike", build_commuter_motorbike, make_lods=True, cam_dir=(1.5, -1.25, 0.55))
    C.export_asset("CTCC_PROP_TerrazzoBench", build_terrazzo_bench, make_lods=True, cam_dir=(1.3, -1.65, 0.65))
    C.export_asset("CTCC_PROP_EmergencyStretcher", build_emergency_stretcher, make_lods=True, cam_dir=(1.55, -1.35, 0.72))
    C.export_asset("CTCC_PROP_OxygenCylinderCart", build_oxygen_cylinder_cart, make_lods=True, cam_dir=(1.35, -1.65, 0.55))
    C.export_asset("CTCC_PROP_PatientMonitorCart", build_patient_monitor_cart, make_lods=True, cam_dir=(1.1, -1.75, 0.55))
    C.export_asset("CTCC_PROP_InstrumentTrolley", build_instrument_trolley, make_lods=True, cam_dir=(1.4, -1.5, 0.82))
    C.export_asset("CTCC_PROP_WallGasManifold", build_wall_gas_manifold, make_lods=False, cam_dir=(0.9, -1.7, 0.45))
    C.export_asset("CTCC_PROP_DismantledBedFrame", build_dismantled_bed_frame, make_lods=True, cam_dir=(1.1, -1.8, 0.65))
    C.export_asset("CTCC_PROP_ReceptionDetails", build_reception_details, make_lods=False, separate_parts=True, cam_dir=(1.4, -1.5, 0.75))
    C.export_asset("CTCC_PROP_EmergencyLightBox", build_emergency_lightbox, make_lods=False, cam_dir=(0.8, -1.8, 0.35))
    C.export_asset("CTCC_ENV_TropicalShrub", build_tropical_shrub, make_lods=False, cam_dir=(1.3, -1.5, 0.85))


if __name__ == "__main__":
    run_all()
