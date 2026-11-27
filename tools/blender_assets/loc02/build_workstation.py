"""Hero Blender 4.5 assets for Lan's Workstation & LOC02 Key Props:
  1. CTCC_PROP_CH01DutyChair     - Native-height (seat z=0.61m, zero 1.30x stretch!) ergonomic bent-plywood
                                   & green-vinyl padded counter stool-chair with welded circular footrest ring.
  2. CTCC_PROP_CH01FileRack      - Multi-compartment Vietnamese hospital wall pigeonhole file cabinet
                                   packed with 18 varied kraft folders, bound archive ledgers (printed spines!),
                                   brass index holders & hanging clipboard.
  3. CTCC_PROP_CH01DutyBook      - Sculpted arched-page open shift logbook with Vietnamese handwritten notes
                                   resting directly under Lan's pen at y=1.129m in B.
  4. CTCC_PROP_ReceptionDetails  - Separate parts:
       * "LanTeaSet"   : Slatted dark-wood tray + Bát Tràng cobalt-rimmed porcelain teapot + 2 delicate tea cups
                         with green tea liquid + vintage Vietnamese red-enamel vacuum thermos (phích nước)!
       * "PottedPlant" : Ribbed glazed ceramic floor pot + terracotta saucer + soil + 18 organically curved,
                         twisted, tapered sculpted snake plant (cây lưỡi hổ) leaves!
  5. CTCC_PROP_WallClock         - Separate parts: "ClockBody", "HandsR" (11:52), "HandsB" (03:17).
                                   42cm vintage hospital quartz clock with stepped metallic bezel, brass ring,
                                   high-res printed dial (B & R variants baked!), glass crystal, tapered 3D hands!
  6. CTCC_LOC02_SpareBattery     - Twin vintage Vietnamese 1.5V D-cell batteries ("PIN ĐẠI SAO MAI · R20")
                                   replacing the 4 primitive Unity cubes on the lower reception wing!
  7. CTCC_LOC02_DeskDrawer       - Upgraded interactive reception desk drawer matching our built-in counter
                                   pocket (dovetailed wood/enameled carcass, antique brass cup pull & label plate,
                                   steel side slides, interior folder & dividers).
"""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import hero_lib as H


# --------------------------------------------------------------------------------------
# 1. CTCC_PROP_CH01DutyChair (native seated counter-stool height: seat top = 0.615m!)
# --------------------------------------------------------------------------------------
def build_duty_chair():
    objs = []
    # 4 tapered splayed painted-steel tubular legs with welded upper H-frame & rubber floor ferrules
    # Chair faces -Y in local space (because HospitalAuthoring places yaw = -88 -> +X world)
    # Wait: let's match EXACT original orientation from build_ch01_art.py!
    # Original duty_chair: legs x in (-0.19, 0.19), front y = -0.31, back y = +0.20, backrest at y = +0.182!
    # Original was scaled (1, 1.30, 1) -> seat height 0.468 * 1.30 = 0.608m, backrest center 0.764 * 1.30 = 0.993m!
    # Building natively at seat = 0.605m, backrest = 0.985m eliminates all stretched-tube distortion!
    seat_z = 0.600
    for sx in (-0.195, 0.195):
        # Continuous bent tubular steel side frame from front foot -> under seat -> back foot
        objs.append(H.pipe(f"ChairSideFrame_{sx:.3f}", [
            (sx, -0.255, 0.022), (sx, -0.175, seat_z - 0.032),
            (sx, 0.165, seat_z - 0.032), (sx, 0.235, 0.022),
        ], 0.0135, "paint_olive", res=5))
        # Curved backrest support upright tubes welded to rear legs
        objs.append(H.pipe(f"ChairBackStay_{sx:.3f}", [
            (sx, 0.165, seat_z - 0.075), (sx, 0.195, seat_z + 0.16),
            (sx, 0.215, seat_z + 0.36), (sx, 0.205, seat_z + 0.43),
        ], 0.0115, "paint_olive", res=5))
        # Molded black rubber non-slip floor ferrules
        for fy in (-0.255, 0.235):
            objs.append(H.lathe(f"ChairFootCap_{sx:.2f}_{fy:.2f}", [
                (0.0, 0.0), (0.018, 0.0), (0.017, 0.026), (0.014, 0.034), (0.0, 0.034)
            ], (sx, fy, 0.0), "rubber", seg=14))

    # Welded under-seat crossbars + tubular circular-front footrest stretcher at z = 0.24m
    objs.append(H.cyl("ChairFrontBar", (-0.195, -0.155, seat_z - 0.032), (0.195, -0.155, seat_z - 0.032), 0.012, "paint_olive", seg=14))
    objs.append(H.cyl("ChairRearBar", (-0.195, 0.155, seat_z - 0.032), (0.195, 0.155, seat_z - 0.032), 0.012, "paint_olive", seg=14))
    objs.append(H.cyl("ChairFootrestFront", (-0.195, -0.222, 0.245), (0.195, -0.222, 0.245), 0.0125, "steel", seg=16))
    objs.append(H.cyl("ChairFootrestBack", (-0.195, 0.206, 0.245), (0.195, 0.206, 0.245), 0.011, "paint_olive", seg=14))
    for sx in (-0.195, 0.195):
        objs.append(H.cyl(f"ChairSideStretcher_{sx:.3f}", (sx, -0.222, 0.245), (sx, 0.206, 0.245), 0.010, "paint_olive", seg=12))

    # Sculpted contoured bent-plywood seat shell with ergonomic waterfall front lip
    def seat_fn(u, v):
        x = (u - 0.5) * 0.435
        y = (v - 0.5) * 0.405
        # Subtle dish contour + rounded waterfall drop at front edge (v -> 0)
        dish = -0.010 * math.sin(u * math.pi) * math.sin(v * math.pi)
        waterfall = -0.022 * (max(0.0, 0.22 - v) / 0.22) ** 2
        return (x, y, seat_z + dish + waterfall)

    objs.append(H.sculpted_grid("ChairPlywoodSeat", 12, 12, seat_fn, "wood", thickness=0.018, grain="X"))
    # Subtle tufted hospital-green stitched vinyl seat cushion pad inset atop the plywood
    def pad_fn(u, v):
        x = (u - 0.5) * 0.375
        y = (v - 0.5) * 0.345 + 0.012
        dome = 0.016 * (math.sin(u * math.pi) * math.sin(v * math.pi)) ** 0.55
        return (x, y, seat_z + 0.002 + dome)

    objs.append(H.sculpted_grid("ChairVinylPad", 10, 10, pad_fn, "vinyl_green", thickness=0.012))

    # Ergonomically curved bent-plywood lumbar backrest with brass carriage bolts
    def back_fn(u, v):
        ang = (u - 0.5) * 0.62
        r = 0.68
        x = math.sin(ang) * r
        y = 0.198 + (1.0 - math.cos(ang)) * r * 0.55 - (v - 0.5) * 0.022
        z = (seat_z + 0.375) + (v - 0.5) * 0.205
        return (x, y, z)

    objs.append(H.sculpted_grid("ChairBackrest", 12, 8, back_fn, "wood", thickness=0.016, grain="X"))
    for sx in (-0.182, 0.182):
        for dz in (-0.042, 0.042):
            objs.append(H.sphere(f"ChairBackBolt_{sx:.2f}_{dz:.2f}", (sx, 0.206, seat_z + 0.375 + dz),
                                 (0.012, 0.008, 0.012), "brass", seg=10, rings=8))
    return objs


# --------------------------------------------------------------------------------------
# 2. CTCC_PROP_CH01FileRack (Vietnamese hospital wall pigeonhole archival cabinet)
#    Placed at (-0.93, 1.39, -1.92), yaw = -90 (back at y=0, extends -Y toward room +X)
# --------------------------------------------------------------------------------------
def build_file_rack():
    objs = []
    w, d, h = 1.24, 0.27, 0.56
    # Tongue-and-groove timber backboard + molded cornice crown lip + bottom support corbels
    objs.append(H.box("RackBackBoard", (0, 0.0, h * 0.5), (w, 0.020, h), "wood", bevel=0.003, grain="X"))
    objs.append(H.box("RackCrown", (0, -d * 0.52, h + 0.012), (w + 0.048, d + 0.032, 0.028), "wood", bevel=0.005, grain="X"))
    # 3 horizontal shelves (bottom, mid, top)
    for sz in (0.014, 0.280, h - 0.014):
        objs.append(H.box(f"RackShelf_{sz:.3f}", (0, -d * 0.5, sz), (w, d, 0.024), "wood", bevel=0.004, grain="X"))
    # 5 vertical pigeonhole dividers creating 4 bays x 2 rows = 8 compartments
    div_xs = (-w * 0.5 + 0.012, -0.30, 0.0, 0.30, w * 0.5 - 0.012)
    for dx in div_xs:
        objs.append(H.box(f"RackDiv_{dx:.3f}", (dx, -d * 0.5, h * 0.5), (0.022, d, h - 0.02), "wood", bevel=0.003, grain="Z"))
    # Brass index label card holders mounted centered on shelf fronts under each compartment!
    for bay_x in (-0.45, -0.15, 0.15, 0.45):
        for sz in (0.014, 0.280):
            objs.append(H.box(f"RackLabel_{bay_x:.2f}_{sz:.2f}", (bay_x, -d - 0.003, sz), (0.068, 0.006, 0.020), "brass", bevel=0.001))
    # Wall mounting steel angle brackets underneath bottom shelf
    for bx in (-0.46, 0.46):
        objs.append(H.box(f"RackCorbel_{bx:.2f}", (bx, -0.075, -0.055), (0.026, 0.15, 0.11), "wood", bevel=0.005))

    # 18 authentic packed archival binders (printed spines!), kraft patient folders & flat form stacks!
    binder_specs = [
        # Bottom row (z base = 0.026)
        (-0.56, 0.026, 0.044, 0.225, -3, "cloth_maroon", 0),
        (-0.505, 0.026, 0.048, 0.232, -1, "cloth_navy", 1),
        (-0.448, 0.026, 0.042, 0.218, 2, "kraft", 2),
        (-0.395, 0.026, 0.046, 0.228, 4, "cloth_olive", 3),
        (-0.245, 0.026, 0.038, 0.210, -4, "kraft", 4),
        (-0.198, 0.026, 0.044, 0.222, -2, "paper", None),
        (-0.145, 0.026, 0.042, 0.215, 3, "kraft", 5),
        # Top row (z base = 0.292)
        (-0.55, 0.292, 0.052, 0.235, -2, "cloth_navy", 6),
        (-0.488, 0.292, 0.046, 0.225, 1, "cloth_maroon", 7),
        (-0.432, 0.292, 0.040, 0.212, 5, "kraft", None),
        (-0.08, 0.292, 0.046, 0.224, -6, "cloth_olive", 0),
        (0.06, 0.292, 0.048, 0.230, -3, "kraft", 1),
        (0.118, 0.292, 0.044, 0.218, 0, "paper", None),
        (0.172, 0.292, 0.046, 0.226, 4, "cloth_maroon", 2),
        (0.37, 0.292, 0.044, 0.220, -2, "cloth_navy", 3),
        (0.425, 0.292, 0.048, 0.228, 2, "kraft", 4),
    ]
    for bi, (bx, bz, bw, bh, lean, matk, sp_idx) in enumerate(binder_specs):
        b = H.box(f"RackBinder_{bi}", (bx, -0.125, bz + bh * 0.5), (bw, 0.215, bh), matk,
                  bevel=0.003, rot=(0, math.radians(lean), 0))
        objs.append(b)
        if sp_idx is not None:
            # Printed spine label facing -Y (outward into room!)
            v0 = 1.0 - (sp_idx + 1) / 8.0 + 0.015
            v1 = 1.0 - sp_idx / 8.0 - 0.015
            lbl = H.box(f"RackSpineLbl_{bi}", (bx, -0.234, bz + bh * 0.56), (bw * 0.72, 0.003, bh * 0.55),
                        "print:binder_spines", bevel=0.0, rot=(0, math.radians(lean), 0))
            H.set_planar_print_uv(lbl, axis="Y", uv_rect=(0.04, v0, 0.96, v1), rot90=True)
            objs.append(lbl)
    # Horizontal stacked paper forms in bottom-right bays (x = +0.15 and +0.45)
    for si, (sx, n_st) in enumerate(((0.15, 5), (0.45, 4))):
        for k in range(n_st):
            objs.append(H.box(f"RackFlatFile_{si}_{k}", (sx + (k % 2 - 0.5) * 0.012, -0.13, 0.036 + k * 0.018),
                              (0.235, 0.21, 0.014), "kraft" if k % 2 == 0 else "paper", bevel=0.002, rot=(0, 0, (k - 1.5) * 0.03)))
    return objs


# --------------------------------------------------------------------------------------
# 3. CTCC_PROP_CH01DutyBook (Lan's open active shift book right under her pen in B)
# --------------------------------------------------------------------------------------
def build_duty_book():
    objs = []
    pw, ph = 0.155, 0.220
    objs.append(H.box("DutyCoverL", (-pw * 0.51, 0, 0.004), (pw + 0.012, ph + 0.014, 0.007), "cloth_navy", bevel=0.002, rot=(0, math.radians(1.5), 0)))
    objs.append(H.box("DutyCoverR", (pw * 0.51, 0, 0.004), (pw + 0.012, ph + 0.014, 0.007), "cloth_navy", bevel=0.002, rot=(0, math.radians(-1.5), 0)))
    objs.append(H.box("DutySpine", (0, 0, 0.0035), (0.022, ph + 0.016, 0.008), "cloth_navy", bevel=0.002))
    objs.append(H.box("DutyBlockL", (-pw * 0.50, 0, 0.010), (pw - 0.004, ph - 0.004, 0.009), "paper", bevel=0.0015))
    objs.append(H.box("DutyBlockR", (pw * 0.50, 0, 0.011), (pw - 0.004, ph - 0.004, 0.011), "paper", bevel=0.0015))

    def page_fn(u, v):
        s = (u - 0.5) * 2.0
        x = s * pw
        y = (v - 0.5) * ph
        ab = abs(s)
        z = 0.015 + 0.007 * math.sin(ab * math.pi) + 0.003 * (1.0 - math.exp(-ab * 7.0))
        return (x, y, z)

    objs.append(H.sculpted_grid("DutyOpenPages", 16, 10, page_fn, "print:lan_dutybook_spread", thickness=0.0022, print_uv=True))
    return objs


# --------------------------------------------------------------------------------------
# 4. CTCC_PROP_ReceptionDetails (Separate parts: "LanTeaSet" + "PottedPlant")
# --------------------------------------------------------------------------------------
def build_reception_details():
    # --- PART A: LanTeaSet (Bát Tràng cobalt-rimmed porcelain tea set + vintage Vietnamese red vacuum thermos!) ---
    tea = []
    # Mitered gallery-rim dark wooden tea tray with recessed slatted deck & side carry cutouts
    tea.append(H.box("TeaTrayFloor", (0.0, 0.0, 0.008), (0.36, 0.25, 0.010), "wood", bevel=0.002, grain="X"))
    for sy in (-0.122, 0.122):
        tea.append(H.box(f"TeaTrayRimY_{sy:.3f}", (0.0, sy, 0.018), (0.37, 0.012, 0.026), "wood_light", bevel=0.0025, grain="X"))
    for sx in (-0.180, 0.180):
        tea.append(H.box(f"TeaTrayRimX_{sx:.3f}", (sx, 0.0, 0.020), (0.012, 0.256, 0.030), "wood_light", bevel=0.0025, grain="Y"))
    # Classic Bát Tràng white porcelain teapot with cobalt blue band, graceful S-spout & curved ear handle
    tx, ty = -0.068, 0.005
    tea.append(H.lathe("TeapotBody", [
        (0.0, 0.013), (0.044, 0.013), (0.046, 0.018), (0.068, 0.056), (0.070, 0.088),
        (0.056, 0.118), (0.040, 0.130), (0.044, 0.136), (0.035, 0.138), (0.034, 0.126), (0.0, 0.124)
    ], (tx, ty, 0.0), "ceramic", seg=28))
    # Cobalt blue decorative belly & shoulder rings
    tea.append(H.lathe("TeapotBlueBand", [(0.0695, 0.078), (0.0710, 0.082), (0.0700, 0.089), (0.0685, 0.086)], (tx, ty, 0.0), "enamel_blue", seg=28))
    # Domed lid with delicate finial knob
    tea.append(H.lathe("TeapotLid", [
        (0.0, 0.132), (0.033, 0.132), (0.036, 0.138), (0.026, 0.150), (0.007, 0.156),
        (0.012, 0.165), (0.009, 0.172), (0.0, 0.174)
    ], (tx, ty, 0.0), "ceramic", seg=24))
    tea.append(H.pipe("TeapotSpout", [
        (tx + 0.054, ty, 0.058), (tx + 0.086, ty, 0.082),
        (tx + 0.102, ty, 0.118), (tx + 0.114, ty, 0.130)
    ], 0.0085, "ceramic", res=5))
    tea.append(H.pipe("TeapotHandle", [
        (tx - 0.054, ty, 0.114), (tx - 0.095, ty, 0.112),
        (tx - 0.102, ty, 0.076), (tx - 0.090, ty, 0.044), (tx - 0.058, ty, 0.042)
    ], 0.0068, "ceramic", res=5))
    # 2 thin-walled porcelain tea cups with cobalt blue lip rim + dark green tea liquid inside!
    for ci, (cx, cy) in enumerate(((0.098, 0.052), (0.106, -0.052))):
        tea.append(H.lathe(f"TeaCup_{ci}", [
            (0.0, 0.013), (0.021, 0.013), (0.022, 0.018), (0.031, 0.046), (0.033, 0.066),
            (0.030, 0.067), (0.027, 0.046), (0.019, 0.021), (0.0, 0.021)
        ], (cx, cy, 0.0), "ceramic", seg=22))
        tea.append(H.lathe(f"TeaCupBlueRim_{ci}", [(0.031, 0.063), (0.0335, 0.066), (0.0295, 0.067)], (cx, cy, 0.0), "enamel_blue", seg=22))
        tea.append(H.cyl(f"TeaLiquid_{ci}", (cx, cy, 0.046), (cx, cy, 0.050), 0.026, "tea_liquid", seg=18))

    # Iconic Vietnamese vintage red-enameled cylindrical vacuum thermos (Phích nước) standing beside tray!
    fx, fy = -0.245, -0.035
    tea.append(H.lathe("ThermosBody", [
        (0.0, 0.0), (0.058, 0.0), (0.061, 0.012), (0.061, 0.255),
        (0.056, 0.278), (0.040, 0.292), (0.040, 0.302), (0.0, 0.302)
    ], (fx, fy, 0.0), "thermos_red", seg=28))
    tea.append(H.lathe("ThermosBaseRim", [(0.059, 0.0), (0.063, 0.004), (0.063, 0.024), (0.060, 0.026)], (fx, fy, 0.0), "steel", seg=28))
    tea.append(H.lathe("ThermosShoulder", [(0.061, 0.245), (0.062, 0.256), (0.056, 0.280), (0.041, 0.294), (0.038, 0.294)], (fx, fy, 0.0), "steel", seg=28))
    tea.append(H.lathe("ThermosCupCap", [(0.039, 0.292), (0.043, 0.296), (0.044, 0.346), (0.040, 0.352), (0.0, 0.352)], (fx, fy, 0.0), "plastic_ivory", seg=24))
    tea.append(H.pipe("ThermosHandle", [
        (fx - 0.058, fy, 0.235), (fx - 0.095, fy, 0.228), (fx - 0.098, fy, 0.105), (fx - 0.060, fy, 0.095)
    ], 0.0075, "plastic_ivory", res=5))

    # --- PART B: PottedPlant (Ribbed glazed ceramic pot + terracotta saucer + 18 sculpted curved snake-plant blades!) ---
    plant = []
    # Shallow glazed floor drip saucer
    plant.append(H.lathe("PotSaucer", [
        (0.0, 0.0), (0.165, 0.0), (0.185, 0.032), (0.175, 0.036), (0.155, 0.010), (0.0, 0.010)
    ], (0, 0, 0), "ceramic_pot", seg=28))
    # Ribbed stoneware ceramic planter pot
    plant.append(H.lathe("CeramicPot", [
        (0.0, 0.010), (0.138, 0.010), (0.152, 0.08), (0.175, 0.20), (0.194, 0.34),
        (0.205, 0.375), (0.218, 0.398), (0.214, 0.422), (0.192, 0.425),
        (0.184, 0.395), (0.174, 0.340), (0.0, 0.340)
    ], (0, 0, 0), "ceramic_pot", seg=32))
    # Textured organic soil mound inside pot
    plant.append(H.sphere("PotSoilMound", (0, 0, 0.345), (0.35, 0.35, 0.065), "soil", seg=20, rings=10))
    # 18 naturally arched, twisted, pointed sword-like Sansevieria (cây lưỡi hổ) blades!
    for i in range(18):
        ang = i * 2.3999632  # golden angle phyllotaxis!
        rad = 0.025 + 0.115 * math.sqrt((i + 0.5) / 18.0)
        bx, by = math.cos(ang) * rad, math.sin(ang) * rad
        height = 0.42 + (i % 5) * 0.095 + (0.10 if rad < 0.07 else 0.0)
        lean = 0.05 + rad * 1.15
        twist = ((i % 3) - 1) * 0.42
        ca, sa = math.cos(ang), math.sin(ang)

        def blade_fn(u, v, _bx=bx, _by=by, _h=height, _ln=lean, _tw=twist, _ca=ca, _sa=sa):
            # Tapered width peaking at mid-blade and sharpening to a natural sword tip at v=1
            half_w = 0.032 * (math.sin(v * math.pi) ** 0.58) * (1.0 - 0.15 * v) + 0.002 * (1.0 - v)
            s = (u - 0.5) * 2.0
            # Subtle longitudinal V-channel cupping + helical twist
            local_w = s * half_w
            cup = 0.008 * (s * s) * math.sin(v * math.pi)
            tw_a = _tw * v
            ct, st = math.cos(tw_a), math.sin(tw_a)
            rx = local_w * ct - cup * st
            ry = local_w * st + cup * ct
            # Outward arch along radial direction
            out = _ln * (v ** 1.65)
            z = 0.345 + v * _h - 0.06 * (_ln * v * v)
            wx = _bx + _ca * (out + ry) - _sa * rx
            wy = _by + _sa * (out + ry) + _ca * rx
            return (wx, wy, z)

        plant.append(H.sculpted_grid(f"SnakeLeaf_{i}", 4, 10, blade_fn, "snake_leaf", thickness=0.0038))

    return {"PottedPlant": plant, "LanTeaSet": tea}


# --------------------------------------------------------------------------------------
# 5. CTCC_PROP_WallClock (Separate parts: "ClockBody", "HandsR", "HandsB")
#    Mounted at (-0.99, 2.74, -3.26), yaw = 90 -> back at Y=0, protrudes -Y (toward room +X)!
# --------------------------------------------------------------------------------------
def build_wall_clock():
    body = []
    # Stepped retro metallic-enamel drum bezel + chrome inner accent ring (Ø44cm for crisp FPS legibility!)
    body.append(H.lathe("ClockCase", [
        (0.0, 0.0), (0.205, 0.0), (0.218, -0.010), (0.222, -0.048),
        (0.214, -0.064), (0.194, -0.066), (0.186, -0.036), (0.0, -0.036)
    ], (0, 0, 0), "paint_olive", seg=48, rot=(math.radians(90), 0, 0)))
    body.append(H.lathe("ClockBrassBezel", [
        (0.190, -0.064), (0.202, -0.067), (0.206, -0.060), (0.194, -0.056)
    ], (0, 0, 0), "brass", seg=48, rot=(math.radians(90), 0, 0)))
    # Crisp printed dial disc (baked separately for B clean ivory vs R damp-foxed!)
    dial = H.cyl("ClockDialPlate", (0, -0.033, 0), (0, -0.037, 0), 0.187, "print:clock_dial_B", seg=48)
    H.set_planar_print_uv(dial, axis="Y", uv_rect=(0.04, 0.04, 0.96, 0.96))
    body.append(dial)
    # 12 raised metallic baton hour pips around perimeter for crisp raking shadow depth
    for hr in range(12):
        a = math.tau * hr / 12.0
        px, pz = math.sin(a) * 0.162, math.cos(a) * 0.162
        body.append(H.box(f"ClockPip_{hr}", (px, -0.0385, pz),
                          (0.006 if hr % 3 == 0 else 0.0035, 0.0035, 0.018 if hr % 3 == 0 else 0.011),
                          "brass", bevel=0.0008, rot=(0, -a, 0)))
    # Brass central spindle nut (leave crystal rim open in center so 03:17 / 11:52 numerals stay 100% razor-sharp!)
    body.append(H.cyl("ClockSpindleNut", (0, -0.037, 0), (0, -0.056, 0), 0.010, "brass", seg=18, bevel=0.002))
    body.append(H.lathe("ClockInnerRim", [
        (0.180, -0.058), (0.190, -0.062), (0.194, -0.054), (0.184, -0.037)
    ], (0, 0, 0), "steel", seg=48, rot=(math.radians(90), 0, 0)))

    # Tapered sculpted 3D hands with realistic counterweight tails (R=11:52, B=03:17!)
    def make_hands(hour, minute, tag):
        hs = []
        ha = math.radians(hour * 30.0 + minute * 0.5)
        ma = math.radians(minute * 6.0)
        sa = math.radians(((minute * 6 + 42) % 360))
        # Hour hand (wide tapered sword + counterweight tail)
        hs.append(H.box(f"HourBlade_{tag}", (math.sin(ha) * 0.044, -0.044, math.cos(ha) * 0.044),
                        (0.011, 0.003, 0.118), "plastic_black", bevel=0.001, rot=(0, -ha, 0)))
        hs.append(H.box(f"HourTail_{tag}", (-math.sin(ha) * 0.018, -0.044, -math.cos(ha) * 0.018),
                        (0.014, 0.003, 0.034), "plastic_black", bevel=0.001, rot=(0, -ha, 0)))
        # Minute hand (longer slender blade)
        hs.append(H.box(f"MinBlade_{tag}", (math.sin(ma) * 0.066, -0.048, math.cos(ma) * 0.066),
                        (0.0075, 0.003, 0.158), "plastic_black", bevel=0.001, rot=(0, -ma, 0)))
        hs.append(H.box(f"MinTail_{tag}", (-math.sin(ma) * 0.022, -0.048, -math.cos(ma) * 0.022),
                        (0.010, 0.003, 0.040), "plastic_black", bevel=0.001, rot=(0, -ma, 0)))
        # Crimson sweep second hand with counterweight disc
        hs.append(H.box(f"SecBlade_{tag}", (math.sin(sa) * 0.060, -0.052, math.cos(sa) * 0.060),
                        (0.0032, 0.002, 0.165), "plastic_red", bevel=0.0005, rot=(0, -sa, 0)))
        hs.append(H.cyl(f"SecCounter_{tag}", (-math.sin(sa) * 0.030, -0.051, -math.cos(sa) * 0.030),
                        (-math.sin(sa) * 0.030, -0.054, -math.cos(sa) * 0.030), 0.008, "plastic_red", seg=12))
        return hs

    return {"ClockBody": body, "HandsR": make_hands(11, 52, "R"), "HandsB": make_hands(3, 17, "B")}


# --------------------------------------------------------------------------------------
# 6. CTCC_LOC02_SpareBattery (Twin vintage Vietnamese 1.5V D-cell flashlight batteries)
#    Replaces 4 primitive Unity boxes at (1.75, 0.835, -5.50)! Rests on z=0.0 (counter top 0.815m).
# --------------------------------------------------------------------------------------
def build_spare_battery():
    objs = []
    # Two cylindrical R20 D-cells (Ø33mm x 61mm) lying horizontally at casual angles on the lower counter!
    for idx, (cx, cy, yaw) in enumerate(((-0.022, 0.004, 0.16), (0.022, -0.008, -0.24))):
        # Build upright along Z then orient horizontally onto Z = radius (0.017)
        r = 0.0168
        L = 0.060
        ca, sa = math.cos(yaw), math.sin(yaw)
        # Body sleeve (printed retro Vietnamese "PIN ĐẠI SAO MAI 1.5V" wrap!)
        p0 = (cx - sa * (L * 0.46), cy + ca * (L * 0.46), r)
        p1 = (cx + sa * (L * 0.46), cy - ca * (L * 0.46), r)
        body = H.cyl(f"BattCell_{idx}", p0, p1, r, "print:battery_label", seg=24, bevel=0.0015)
        H.set_planar_print_uv(body, axis="Z", uv_rect=(0.0, 0.0, 1.0, 1.0))
        objs.append(body)
        # Positive metal crimped shoulder + brass button nipple
        p_pos = (cx - sa * (L * 0.50), cy + ca * (L * 0.50), r)
        p_tip = (cx - sa * (L * 0.56), cy + ca * (L * 0.56), r)
        objs.append(H.cyl(f"BattPosRim_{idx}", p0, p_pos, r * 0.96, "steel", seg=22))
        objs.append(H.cyl(f"BattPosCap_{idx}", p_pos, p_tip, r * 0.34, "brass", seg=16, bevel=0.001))
        # Negative zinc base plate
        p_neg = (cx + sa * (L * 0.50), cy - ca * (L * 0.50), r)
        objs.append(H.cyl(f"BattNegBase_{idx}", p1, p_neg, r * 0.96, "steel", seg=22))
    return objs


# --------------------------------------------------------------------------------------
# 7. CTCC_LOC02_DeskDrawer (Built-in wooden/enameled reception counter pull-out drawer)
#    Exact footprint matching CTCC_PROP_InteractiveDrawer (w=0.44, d=0.30, h=0.14, front at -Y)
# --------------------------------------------------------------------------------------
def build_desk_drawer():
    objs = []
    w, d, h = 0.44, 0.30, 0.14
    # Molded chamfered wooden drawer front face matching our hero reception counter cabinetry!
    objs.append(H.box("DeskDrawerFace", (0, -d * 0.5, 0), (w, 0.024, h), "wood_light", bevel=0.005, grain="X"))
    # Recessed decorative panel bevel + antique brass cup pull handle + brass index label frame
    objs.append(H.box("DeskDrawerInset", (0, -d * 0.5 - 0.011, 0), (w - 0.048, 0.005, h - 0.036), "wood", bevel=0.002, grain="X"))
    objs.append(H.pipe("DeskDrawerPull", [
        (-0.068, -d * 0.5 - 0.012, -0.012), (-0.058, -d * 0.5 - 0.036, -0.012),
        (0.058, -d * 0.5 - 0.036, -0.012), (0.068, -d * 0.5 - 0.012, -0.012),
    ], 0.0065, "brass", res=5))
    for sx in (-0.068, 0.068):
        objs.append(H.cyl(f"PullRosette_{sx:.3f}", (sx, -d * 0.5 - 0.012, -0.012), (sx, -d * 0.5 - 0.018, -0.012), 0.011, "brass", seg=12))
    objs.append(H.box("DeskDrawerLabelPlate", (0, -d * 0.5 - 0.014, 0.036), (0.088, 0.005, 0.026), "brass", bevel=0.0015))
    # Wooden box carcass + steel ball-bearing side slide channels
    objs.append(H.box("DeskDrawerBottom", (0, 0.005, -h * 0.44), (w - 0.032, d - 0.02, 0.012), "wood", bevel=0.002))
    objs.append(H.box("DeskDrawerBack", (0, d * 0.5 - 0.01, -0.006), (w - 0.032, 0.016, h - 0.024), "wood", bevel=0.002))
    for s in (-1, 1):
        objs.append(H.box(f"DeskDrawerSide_{s}", (s * (w * 0.5 - 0.020), 0.005, -0.006), (0.016, d - 0.02, h - 0.024), "wood", bevel=0.002))
        objs.append(H.box(f"DeskDrawerSlide_{s}", (s * (w * 0.5 - 0.009), 0.01, -0.018), (0.010, d - 0.03, 0.018), "steel", bevel=0.001))
    # Interior kraft folder backing & red rubber band/pencil divider strip (leaves room for Doc_DutyRoster quad!)
    objs.append(H.box("DeskDrawerFolder", (-0.005, 0.00, -h * 0.34), (0.31, 0.22, 0.009), "kraft", bevel=0.002, rot=(0, 0, 0.04)))
    objs.append(H.box("DeskDrawerDivider", (-0.155, 0.005, -0.032), (0.010, d - 0.04, 0.048), "wood_light", bevel=0.0015))
    return objs


def run():
    H.export_hero_asset(
        "CTCC_PROP_CH01DutyChair", build_duty_chair,
        atlas_specs={"LOC02_DutyChair": {"res": 1024, "bake_r": True, "parts": None}},
        make_lods=True, separate_parts=False, cam_dir=(1.45, -1.55, 0.85), render=True, ao_samples=14,
    )
    H.export_hero_asset(
        "CTCC_PROP_CH01FileRack", build_file_rack,
        atlas_specs={"LOC02_FileRack": {"res": 1024, "bake_r": True, "parts": None}},
        make_lods=True, separate_parts=False, cam_dir=(0.75, -1.85, 0.45), render=True, ao_samples=14,
    )
    H.export_hero_asset(
        "CTCC_PROP_CH01DutyBook", build_duty_book,
        atlas_specs={"LOC02_DutyBook": {"res": 1024, "bake_r": False, "parts": None}},
        make_lods=False, separate_parts=False, cam_dir=(0.35, -1.2, 1.55), render=True, ao_samples=12,
    )
    H.export_hero_asset(
        "CTCC_PROP_ReceptionDetails", build_reception_details,
        atlas_specs={
            "LOC02_TeaSet": {"res": 1024, "bake_r": False, "parts": ["LanTeaSet"]},
            "LOC02_Plant": {"res": 1024, "bake_r": True, "parts": ["PottedPlant"]},
        },
        make_lods=False, separate_parts=True, cam_dir=(1.35, -1.55, 0.72), render=True, ao_samples=14,
    )
    H.export_hero_asset(
        "CTCC_PROP_WallClock", build_wall_clock,
        atlas_specs={"LOC02_WallClock": {"res": 1024, "bake_r": True, "parts": None}},
        make_lods=False, separate_parts=True, cam_dir=(0.35, -1.95, 0.18), render=True, ao_samples=14,
    )
    H.export_hero_asset(
        "CTCC_LOC02_SpareBattery", build_spare_battery,
        atlas_specs={"LOC02_SpareBattery": {"res": 512, "bake_r": False, "parts": None}},
        make_lods=False, separate_parts=False, cam_dir=(1.1, -1.4, 1.0), render=True, ao_samples=12,
    )
    H.export_hero_asset(
        "CTCC_LOC02_DeskDrawer", build_desk_drawer,
        atlas_specs={"LOC02_DeskDrawer": {"res": 1024, "bake_r": True, "parts": None}},
        make_lods=False, separate_parts=False, cam_dir=(0.85, -1.55, 1.05), render=True, ao_samples=12,
    )


if __name__ == "__main__":
    run()
