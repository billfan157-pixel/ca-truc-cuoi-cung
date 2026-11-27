"""Hero Blender 4.5 architectural & hall assets exclusively for LOC02 Reception:
  1. CTCC_LOC02_S1Door       - Authentic 1987 Vietnamese institutional double-leaf panelled & louvered
                               timber stairwell door + deep molded architrave casing + frosted transom
                               panes + brushed stainless kickplates + brass mortise handles & hinges.
                               Replaces the giant zebra-striped primitive cube directly behind Lan!
                               Exact footprint: 2.65m wide (along Y), 2.40m tall (Z in [0..2.40]),
                               facing +X into LOC02 lobby!
  2. CTCC_LOC02_WaitingBench - Commercial-grade 4-seat perforated contoured stainless hospital waiting
                               bench (2.12m long along Y) with rolled ergonomic bucket shells, heavy
                               welded tubular beam, bolted floor flanges & arched armrests.
  3. CTCC_LOC02_WallFittings - Separate parts:
       * "RosterBoard"  : Glass-fronted dark-wood framed Vietnamese Hospital Duty Roster Notice Board
                          (BẢNG PHÂN CÔNG TRỰC · KHOA NỘI TRÚ) with crisp printed roster_board.png!
       * "SwitchBoard"  : Vintage 1987 Bakelite ceiling-fan 5-speed regulator box (hộp số quạt trần)
                          + twin hospital toggle switches + galvanized surface conduit risers & clamps!
  4. CTCC_LOC02_CeilingBeams - Authentic 1987 reinforced concrete ceiling beams (dầm bê tông) +
                               perimeter plaster cornice molding + chamfered structural wall pilasters
                               framing LOC02's 10 x 8m lobby (origin at lobby center (4, 0, -4), y=0..3.60)!
  5. CTCC_LOC02_CeilingFan   - Upgraded Vietnamese Điện Cơ 3-blade ceiling fan (separate FanMount & FanRotor)
                               with sculpted ventilated stator bell, brass equatorial ring, riveted
                               pitched steel blades & ceiling bell canopy!
"""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import hero_lib as H


# --------------------------------------------------------------------------------------
# 1. CTCC_LOC02_S1Door (Replaces ugly zebra-striped cube at (-1.07, 1.2, -4)!)
#    Origin at bottom center (0, 0, 0) -> placed at (-1.04, 0, -4.0), yaw=0
#    Spans Y in [-1.34, +1.34] (2.68m wide), Z in [0.0, 2.43] (2.43m tall), faces +X into LOC02!
# --------------------------------------------------------------------------------------
def build_s1_door():
    opaque = []
    glass = []

    w = 2.66
    h = 2.42
    # Deep molded timber outer architrave & door jamb frame wrapping the opening cleanly
    opaque.append(H.box("S1_JambL", (0.02, -w * 0.5 + 0.045, h * 0.5), (0.16, 0.09, h), "wood", bevel=0.006, grain="Z"))
    opaque.append(H.box("S1_JambR", (0.02, w * 0.5 - 0.045, h * 0.5), (0.16, 0.09, h), "wood", bevel=0.006, grain="Z"))
    opaque.append(H.box("S1_Header", (0.02, 0.0, h - 0.045), (0.16, w, 0.09), "wood", bevel=0.006, grain="Y"))
    # Stepped ornamental architrave casing trim protruding into LOC02 (+X)
    opaque.append(H.box("S1_CasingL", (0.088, -w * 0.5 - 0.012, h * 0.5 + 0.02), (0.032, 0.095, h + 0.04), "wood_light", bevel=0.005, grain="Z"))
    opaque.append(H.box("S1_CasingR", (0.088, w * 0.5 + 0.012, h * 0.5 + 0.02), (0.032, 0.095, h + 0.04), "wood_light", bevel=0.005, grain="Z"))
    opaque.append(H.box("S1_CasingTop", (0.092, 0.0, h + 0.028), (0.042, w + 0.14, 0.095), "wood_light", bevel=0.006, grain="Y"))
    # Center astragal meeting strip concealing seam between double leaves
    opaque.append(H.box("S1_Astragal", (0.056, 0.0, 1.15), (0.026, 0.046, 2.28), "wood", bevel=0.004, grain="Z"))

    # Twin symmetrical institutional Vietnamese hospital timber door leaves (left y<0, right y>0)
    leaf_w = (w - 0.18) * 0.5 - 0.006  # ~1.234m each
    for side in (-1, 1):
        yc = side * (leaf_w * 0.5 + 0.004)
        # Main vertical stiles (outer & inner) + horizontal rails (bottom, lock, upper, top)
        opaque.append(H.box(f"S1_StileOut_{side}", (0.018, yc + side * (leaf_w * 0.5 - 0.068), 1.16), (0.062, 0.136, 2.30), "wood", bevel=0.005, grain="Z"))
        opaque.append(H.box(f"S1_StileIn_{side}", (0.018, yc - side * (leaf_w * 0.5 - 0.068), 1.16), (0.062, 0.136, 2.30), "wood", bevel=0.005, grain="Z"))
        opaque.append(H.box(f"S1_RailBot_{side}", (0.018, yc, 0.125), (0.062, leaf_w - 0.24, 0.23), "wood", bevel=0.004, grain="Y"))
        opaque.append(H.box(f"S1_RailMid_{side}", (0.018, yc, 1.06), (0.062, leaf_w - 0.24, 0.16), "wood", bevel=0.004, grain="Y"))
        opaque.append(H.box(f"S1_RailTop_{side}", (0.018, yc, 2.22), (0.062, leaf_w - 0.24, 0.16), "wood", bevel=0.004, grain="Y"))
        # Center vertical mullion dividing each leaf cleanly into 2 balanced raised panels below + 2 upper louver/glazed bays!
        opaque.append(H.box(f"S1_MidStile_{side}", (0.018, yc, 1.16), (0.056, 0.088, 2.02), "wood", bevel=0.004, grain="Z"))

        # Lower recessed solid wood panels + raised beveled molding infill (z = 0.24..0.98)
        for sub in (-1, 1):
            sub_y = yc + sub * (leaf_w * 0.22)
            sub_w = leaf_w * 0.36
            opaque.append(H.box(f"S1_LowRecess_{side}_{sub}", (0.018, sub_y, 0.61), (0.032, sub_w, 0.74), "wood_light", bevel=0.003, grain="Z"))
            opaque.append(H.box(f"S1_LowRaised_{side}_{sub}", (0.040, sub_y, 0.61), (0.018, sub_w - 0.06, 0.64), "wood", bevel=0.006, seg=2, grain="Z"))

            # Upper section (z = 1.14..2.14): classic Vietnamese hospital angled ventilation louver slats (lower half) + frosted safety glass (upper half)!
            opaque.append(H.box(f"S1_MidBar_{side}_{sub}", (0.018, sub_y, 1.62), (0.054, sub_w + 0.02, 0.058), "wood", bevel=0.003, grain="Y"))
            # 7 angled wooden ventilation louver slats (z = 1.19..1.57)
            for li in range(7):
                lz = 1.20 + li * 0.056
                opaque.append(H.box(f"S1_Louver_{side}_{sub}_{li}", (0.020, sub_y, lz),
                                    (0.044, sub_w - 0.012, 0.013), "wood_light", bevel=0.002,
                                    rot=(0, math.radians(-32), 0), grain="Y"))
            # Frosted wired-glass observation pane in top bay (z = 1.65..2.14) + mitered beads
            glass.append(H.box(f"S1_TopGlass_{side}_{sub}", (0.018, sub_y, 1.895), (0.010, sub_w - 0.01, 0.48), "FrostedGlass", bevel=0.0))
            opaque.append(H.box(f"S1_GlassCross_{side}_{sub}", (0.018, sub_y, 1.895), (0.024, sub_w, 0.022), "wood", bevel=0.002, grain="Y"))

        # Brushed stainless steel bottom stretcher kickplate on +X face
        opaque.append(H.box(f"S1_KickPlate_{side}", (0.052, yc, 0.16), (0.006, leaf_w - 0.04, 0.30), "steel", bevel=0.002, grain="Y"))

        # Heavy brass mortise lock escutcheon + curved lever handle on +X face near center meeting stile
        hy = side * 0.085
        opaque.append(H.box(f"S1_Escutcheon_{side}", (0.054, hy, 1.05), (0.010, 0.052, 0.215), "brass", bevel=0.003))
        opaque.append(H.pipe(f"S1_Handle_{side}", [
            (0.058, hy, 1.08), (0.108, hy, 1.08), (0.108, hy + side * 0.115, 1.08), (0.096, hy + side * 0.125, 1.08)
        ], 0.0105, "brass", res=5))
        # 3 heavy barrel hinges along outer jamb edge
        hinge_y = side * (leaf_w + 0.002)
        for hz in (0.34, 1.18, 2.02):
            opaque.append(H.cyl(f"S1_Hinge_{side}_{hz:.2f}", (0.052, hinge_y, hz - 0.055), (0.052, hinge_y, hz + 0.055), 0.012, "brass", seg=12))

    return {"DoorBody": opaque, "DoorGlass": glass}


# --------------------------------------------------------------------------------------
# 2. CTCC_LOC02_WaitingBench (Commercial-grade 4-seat perforated stainless hospital bench)
#    Placed at (7.24, 0, -5.12), yaw=0 (length 2.12m along Y, seats face -X toward reception!)
# --------------------------------------------------------------------------------------
def build_waiting_bench():
    objs = []
    # Heavy welded dark-enamel rectangular horizontal spine beam + end caps
    objs.append(H.box("BenchSpine", (0.01, 0.0, 0.355), (0.072, 2.04, 0.062), "steel_dark", bevel=0.006, grain="Y"))
    # Two sculpted arched cast-chrome/steel leg assemblies with bolted floor flanges & rubber pads
    for sy in (-0.80, 0.80):
        objs.append(H.pipe(f"BenchLegArch_{sy:.2f}", [
            (-0.275, sy, 0.022), (-0.235, sy, 0.12), (-0.165, sy, 0.345),
            (0.165, sy, 0.345), (0.235, sy, 0.12), (0.275, sy, 0.022)
        ], 0.019, "steel", res=6))
        # Welded vertical strut tying arch to spine
        objs.append(H.cyl(f"BenchLegTie_{sy:.2f}", (0.01, sy, 0.24), (0.01, sy, 0.35), 0.016, "steel_dark", seg=12))
        for sx in (-0.275, 0.275):
            objs.append(H.lathe(f"BenchFoot_{sy:.2f}_{sx:.2f}", [
                (0.0, 0.0), (0.032, 0.0), (0.030, 0.014), (0.022, 0.026), (0.0, 0.026)
            ], (sx, sy, 0.0), "rubber", seg=16))

    # 4 ergonomically contoured single-piece curved perforated stainless hospital bucket seats!
    for k in range(4):
        cy = -0.765 + k * 0.510
        # Steel under-seat mounting saddle bracket tucked cleanly behind the contoured shell
        objs.append(H.pipe(f"SeatBracket_{k}", [
            (-0.14, cy, 0.388), (0.01, cy, 0.368), (0.215, cy, 0.388), (0.268, cy, 0.61)
        ], 0.012, "steel_dark", res=5))
        # Continuous contoured perforated steel shell (waterfall front lip -> cupped seat -> lumbar curve -> rolled top!)
        def shell_fn(u, v, _cy=cy):
            # u across width [-0.22..+0.22], v along profile from front waterfall lip (0) -> seat back (0.52) -> backrest top (1.0)
            y = _cy + (u - 0.5) * 0.445
            cup = 0.016 * ((u - 0.5) * 2.0) ** 2
            if v < 0.08:
                t = v / 0.08
                x = -0.225 + t * 0.035
                z = 0.388 + t * 0.042
            elif v < 0.50:
                t = (v - 0.08) / 0.42
                x = -0.190 + t * 0.365
                z = 0.430 - 0.028 * math.sin(t * math.pi) - 0.024 * t + cup
            elif v < 0.62:
                # Curved transition knee
                t = (v - 0.50) / 0.12
                ang = t * math.pi * 0.48
                x = 0.175 + math.sin(ang) * 0.048
                z = 0.406 + (1.0 - math.cos(ang)) * 0.068 + cup * 0.6
            else:
                t = (v - 0.62) / 0.38
                x = 0.223 + t * 0.056 - 0.012 * math.sin(t * math.pi)
                z = 0.474 + t * 0.345 + cup * 0.5
            return (x, y, z)

        objs.append(H.sculpted_grid(f"SeatShell_{k}", 8, 16, shell_fn, "perf_steel", thickness=0.012))
        # Smooth brushed-chrome edge trim rails flanking each seat shell
        for s in (-1, 1):
            ey = cy + s * 0.224
            objs.append(H.pipe(f"SeatTrim_{k}_{s}", [
                (-0.215, ey, 0.398), (-0.17, ey, 0.432), (0.16, ey, 0.412),
                (0.215, ey, 0.470), (0.275, ey, 0.812)
            ], 0.0085, "steel", res=4))

    # Swept chrome end armrests + center divider armrest
    for sy in (-1.015, 1.015):
        objs.append(H.pipe(f"Armrest_{sy:.3f}", [
            (-0.18, sy, 0.40), (-0.20, sy, 0.56), (-0.15, sy, 0.605),
            (0.14, sy, 0.605), (0.19, sy, 0.56), (0.18, sy, 0.40)
        ], 0.015, "steel", res=6))
    return objs


# --------------------------------------------------------------------------------------
# 3. CTCC_LOC02_WallFittings (Separate parts: "RosterBoard" + "SwitchBoard")
#    Back mounted flush at X=0, protrudes +X (placed on west wall or rotated to east wall!)
# --------------------------------------------------------------------------------------
def build_wall_fittings():
    # --- Part A: Framed Vietnamese Hospital Duty Roster Board (BẢNG PHÂN CÔNG TRỰC, 0.96 x 0.92m) ---
    board = []
    bw, bh = 0.96, 0.92
    # Deep molded dark wood display case box
    board.append(H.box("BoardCaseBack", (0.014, 0.0, 0.0), (0.026, bw - 0.02, bh - 0.02), "wood", bevel=0.004, grain="Y"))
    board.append(H.box("BoardFrameTop", (0.038, 0.0, bh * 0.5 - 0.028), (0.056, bw + 0.03, 0.056), "wood_light", bevel=0.006, grain="Y"))
    board.append(H.box("BoardFrameBot", (0.038, 0.0, -bh * 0.5 + 0.028), (0.056, bw + 0.03, 0.056), "wood_light", bevel=0.006, grain="Y"))
    board.append(H.box("BoardFrameL", (0.038, -bw * 0.5 + 0.028, 0.0), (0.054, 0.056, bh - 0.116), "wood_light", bevel=0.006, grain="Z"))
    board.append(H.box("BoardFrameR", (0.038, bw * 0.5 - 0.028, 0.0), (0.054, 0.056, bh - 0.116), "wood_light", bevel=0.006, grain="Z"))
    # Crisp printed duty roster baize & pinned sheets surface facing +X
    face = H.box("BoardPrintFace", (0.030, 0.0, 0.0), (0.006, bw - 0.11, bh - 0.11), "print:roster_board", bevel=0.0)
    H.set_planar_print_uv(face, axis="X", uv_rect=(0.0, 0.0, 1.0, 1.0))
    board.append(face)
    # Brass corner hinges & small cabinet lock on right frame edge
    for hz in (-0.28, 0.28):
        board.append(H.cyl(f"BoardHinge_{hz:.2f}", (0.064, -bw * 0.5 + 0.004, hz - 0.022), (0.064, -bw * 0.5 + 0.004, hz + 0.022), 0.0055, "brass", seg=10))
    board.append(H.cyl("BoardLock", (0.062, bw * 0.5 - 0.028, 0.0), (0.072, bw * 0.5 - 0.028, 0.0), 0.009, "brass", seg=14))

    # --- Part B: Vintage 1987 Bakelite Ceiling-Fan Speed Regulator + Twin Toggle Switchboard + Conduit ---
    sw = []
    # Wooden pattress mounting board
    sw.append(H.box("SwitchBackPlate", (0.012, 0.0, 0.0), (0.024, 0.26, 0.34), "wood", bevel=0.005, grain="Z"))
    # Upper vintage cream/ivory 5-speed rotary ceiling fan regulator box (Hộp số quạt trần Điện Cơ!)
    sw.append(H.box("FanRegBox", (0.044, 0.0, 0.068), (0.044, 0.125, 0.135), "plastic_ivory", bevel=0.008, seg=3))
    sw.append(H.cyl("FanRegDialRim", (0.065, 0.0, 0.068), (0.074, 0.0, 0.068), 0.032, "brass", seg=22))
    sw.append(H.box("FanRegKnob", (0.082, 0.0, 0.068), (0.022, 0.018, 0.046), "plastic_black", bevel=0.004, rot=(math.radians(35), 0, 0)))
    # Lower 3-gang hospital light switch panel (fluorescent lights + desk circuit!)
    sw.append(H.box("LightSwitchBox", (0.036, 0.0, -0.082), (0.030, 0.165, 0.095), "plastic_ivory", bevel=0.006))
    for k, sy in enumerate((-0.046, 0.0, 0.046)):
        sw.append(H.box(f"SwitchBezel_{k}", (0.052, sy, -0.082), (0.008, 0.024, 0.042), "plastic_black", bevel=0.002))
        sw.append(H.box(f"SwitchToggle_{k}", (0.059, sy, -0.082), (0.016, 0.016, 0.028), "plastic_red" if k == 0 else "plastic_ivory",
                        bevel=0.002, rot=(0, math.radians(14 if k != 2 else -14), 0)))
    # Twin galvanized steel conduit risers running up to ceiling + saddle clamps
    for cy in (-0.038, 0.038):
        sw.append(H.cyl(f"SwConduit_{cy:.3f}", (0.018, cy, 0.16), (0.018, cy, 2.05), 0.010, "steel_dark", seg=12))
    for cz in (0.48, 1.15, 1.78):
        sw.append(H.box(f"SwClamp_{cz:.2f}", (0.018, 0.0, cz), (0.028, 0.115, 0.020), "steel", bevel=0.002))

    return {"RosterBoard": board, "SwitchBoard": sw}


# --------------------------------------------------------------------------------------
# 4. CTCC_LOC02_CeilingBeams (1987 reinforced concrete ceiling beams + cornice + wall pilasters!)
#    Origin at LOC02 center world (4.0, 0, -4.0), yaw=0:
#    LOC02 spans local X in [-5.0, +5.0] (world [-1..9]), Y in [-4.0, +4.0] (world [-8..0]), Z in [0..3.60]!
# --------------------------------------------------------------------------------------
def build_ceiling_beams():
    objs = []
    # Main primary E-W girders (deeper) + secondary N-S crossbeams (shallower) so beam crossings have authentic stepped concrete depth & zero coplanar faces!
    for by in (-1.35, 1.55):
        objs.append(H.box(f"BeamEW_{by:.2f}", (0.0, by, 3.465), (9.66, 0.28, 0.27), "plaster", bevel=0.012, seg=2, grain="X"))
        objs.append(H.box(f"BeamEWTrim_{by:.2f}", (0.0, by, 3.326), (9.62, 0.30, 0.022), "paint_cream", bevel=0.004, grain="X"))
    for bx in (-1.50, 1.50):
        objs.append(H.box(f"BeamNS_{bx:.2f}", (bx, 0.0, 3.485), (0.26, 7.66, 0.23), "plaster", bevel=0.012, seg=2, grain="Y"))
        objs.append(H.box(f"BeamNSTrim_{bx:.2f}", (bx, 0.0, 3.366), (0.28, 7.62, 0.022), "paint_cream", bevel=0.004, grain="Y"))

    # Perimeter ceiling cove cornice molding along all 4 LOC02 walls (z = 3.47..3.60), butt-jointed cleanly at corners
    objs.append(H.box("CorniceWest", (-4.92, 0.0, 3.535), (0.16, 7.98, 0.13), "plaster", bevel=0.012, grain="Y"))
    objs.append(H.box("CorniceEast", (4.92, 0.0, 3.535), (0.16, 7.98, 0.13), "plaster", bevel=0.012, grain="Y"))
    objs.append(H.box("CorniceSouth", (0.0, -3.92, 3.535), (9.66, 0.16, 0.13), "plaster", bevel=0.012, grain="X"))
    objs.append(H.box("CorniceNorth", (0.0, 3.92, 3.535), (9.66, 0.16, 0.13), "plaster", bevel=0.012, grain="X"))

    # Chamfered structural concrete pilasters along East wall (world x=9 -> local x=+4.90) & South wall flanking entry (world x=2.5, 5.5)
    # Kept strictly shallow (0.16m) so zero nav/bench interference!
    pilaster_spots = [
        (4.92, -1.35, 0.16, 0.34),   # East wall mid-south (clear of bench at x=7.24!)
        (4.92, 1.55, 0.16, 0.34),    # East wall mid-north
        (-1.50, -3.92, 0.34, 0.16),  # South wall west of entry
        (1.50, -3.92, 0.34, 0.16),   # South wall east of entry
    ]
    for pi, (px, py, pw, pd) in enumerate(pilaster_spots):
        objs.append(H.box(f"PilasterUpper_{pi}", (px, py, 2.24), (pw, pd, 2.16), "plaster", bevel=0.010, grain="Z"))
        objs.append(H.box(f"PilasterDado_{pi}", (px, py, 0.635), (pw + 0.012, pd + 0.012, 1.05), "paint_green", bevel=0.008, grain="Z"))
        objs.append(H.box(f"PilasterPlinth_{pi}", (px, py, 0.055), (pw + 0.024, pd + 0.024, 0.11), "terrazzo_dark", bevel=0.006))
        objs.append(H.box(f"PilasterCap_{pi}", (px, py, 3.35), (pw + 0.04, pd + 0.04, 0.060), "paint_cream", bevel=0.005))

    return objs


# --------------------------------------------------------------------------------------
# 5. CTCC_LOC02_CeilingFan (Upgraded Vietnamese Điện Cơ 3-blade ceiling fan)
#    Origin at ceiling hook (0, 0, 0), drops cleanly down to Z = -0.52m (below ceiling beams!)
#    Separate parts: "FanMount" & "FanRotor"
# --------------------------------------------------------------------------------------
def build_ceiling_fan():
    mount = []
    # Top ceiling bell canopy + rubber damper collar + sturdy painted steel downrod (50cm drop!)
    mount.append(H.lathe("FanTopCanopy", [
        (0.014, 0.0), (0.078, -0.004), (0.074, -0.026), (0.026, -0.088), (0.015, -0.098)
    ], (0, 0, 0), "enamel_white", seg=32))
    mount.append(H.cyl("FanDownrod", (0, 0, -0.02), (0, 0, -0.44), 0.012, "paint_olive", seg=18))
    mount.append(H.lathe("FanLowCanopy", [
        (0.014, -0.34), (0.028, -0.36), (0.062, -0.415), (0.058, -0.432)
    ], (0, 0, 0), "enamel_white", seg=32))

    rotor = []
    mz = -0.475
    # Classic ribbed ventilated stator motor housing + equatorial brass trim ring + lower medallion cap
    rotor.append(H.lathe("FanMotorBell", [
        (0.018, mz + 0.052), (0.082, mz + 0.048), (0.126, mz + 0.028), (0.132, mz + 0.004),
        (0.130, mz - 0.016), (0.114, mz - 0.034), (0.068, mz - 0.044), (0.0, mz - 0.046)
    ], (0, 0, 0), "paint_olive", seg=36))
    rotor.append(H.lathe("FanTrimRing", [
        (0.130, mz + 0.007), (0.136, mz + 0.003), (0.136, mz - 0.006), (0.130, mz - 0.010)
    ], (0, 0, 0), "brass", seg=36))
    rotor.append(H.lathe("FanBottomCap", [
        (0.0, mz - 0.044), (0.052, mz - 0.044), (0.044, mz - 0.054), (0.0, mz - 0.056)
    ], (0, 0, 0), "brass", seg=24))
    # 3 classic 1400mm stamped enameled steel blades with bolted steel root forks & aerodynamic pitch
    for i in range(3):
        ang = math.radians(i * 120.0)
        ca, sa = math.cos(ang), math.sin(ang)
        rotor.append(H.box(f"FanArm_{i}", (ca * 0.185, sa * 0.185, mz - 0.016),
                           (0.165, 0.054, 0.011), "steel_dark", bevel=0.002, rot=(math.radians(7.5), 0, ang)))
        rotor.append(H.box(f"FanBlade_{i}", (ca * 0.465, sa * 0.465, mz - 0.022),
                           (0.52, 0.142, 0.0055), "enamel_white", bevel=0.0025, rot=(math.radians(8.5), math.radians(1.5), ang)))
        # Decorative lengthwise stiffening ribs pressed into each blade
        rotor.append(H.box(f"FanBladeRib_{i}", (ca * 0.455, sa * 0.455, mz - 0.018),
                           (0.46, 0.018, 0.004), "paint_olive", bevel=0.001, rot=(math.radians(8.5), math.radians(1.5), ang)))
        for bo in (-0.018, 0.018):
            rx, ry = ca * 0.242 - sa * bo, sa * 0.242 + ca * bo
            rotor.append(H.cyl(f"FanBolt_{i}_{bo:.3f}", (rx, ry, mz - 0.032), (rx, ry, mz - 0.008), 0.0055, "brass", seg=10))

    return {"FanMount": mount, "FanRotor": rotor}


def run():
    H.export_hero_asset(
        "CTCC_LOC02_S1Door", build_s1_door,
        atlas_specs={"LOC02_S1Door": {"res": 2048, "bake_r": True, "parts": ["DoorBody"], "margin": 0.004}},
        make_lods=True, separate_parts=False, cam_dir=(1.85, -1.15, 0.35), render=True, ao_samples=16,
    )
    H.export_hero_asset(
        "CTCC_LOC02_WaitingBench", build_waiting_bench,
        atlas_specs={"LOC02_WaitingBench": {"res": 1024, "bake_r": True, "parts": None}},
        make_lods=True, separate_parts=False, cam_dir=(-1.65, -1.35, 0.65), render=True, ao_samples=14,
    )
    H.export_hero_asset(
        "CTCC_LOC02_WallFittings", build_wall_fittings,
        atlas_specs={
            "LOC02_RosterBoard": {"res": 1024, "bake_r": True, "parts": ["RosterBoard"]},
            "LOC02_SwitchBoard": {"res": 1024, "bake_r": True, "parts": ["SwitchBoard"]},
        },
        make_lods=False, separate_parts=True, cam_dir=(1.75, -0.95, 0.28), render=True, ao_samples=14,
    )
    H.export_hero_asset(
        "CTCC_LOC02_CeilingBeams", build_ceiling_beams,
        atlas_specs={"LOC02_CeilingArch": {"res": 1024, "bake_r": True, "parts": None}},
        make_lods=False, separate_parts=False, cam_dir=(1.3, -1.5, -0.55), render=True, ao_samples=12,
    )
    H.export_hero_asset(
        "CTCC_LOC02_CeilingFan", build_ceiling_fan,
        atlas_specs={"LOC02_CeilingFan": {"res": 1024, "bake_r": True, "parts": None}},
        make_lods=False, separate_parts=True, cam_dir=(1.35, -1.45, -0.68), render=True, ao_samples=14,
    )


if __name__ == "__main__":
    run()
