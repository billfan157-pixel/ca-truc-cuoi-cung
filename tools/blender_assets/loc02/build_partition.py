"""Hero Blender 4.5 asset: CTCC_ENV_ReceptionPartition.

Authentic 1990s-2000s Vietnamese public hospital glazed ironwood reception counter screen:
  - Placed in Unity at (2.02, 1.125, -3.24), yaw=0, length 2.56m along Y (Unity Z)
  - 3-tier stepped cornice header crown + routed timber sill rail
  - 4 profiled vertical mullions with separate mitered glazing bead stops & brass corner brackets
  - Center service window with anodized sliding track bar, stainless finger pull, lower
    document pass-through opening (26cm clear), and recessed contoured stainless scoop tray
    spanning x in [-0.10, +0.14] so it NEVER clips AN01 DutyLedger!
  - Two slightly curled Scotch-taped Vietnamese period hospital notices inside the glass bays
  - Bolted ceiling suspension flanges + turnbuckles reaching exact ceiling z = 2.475m
  - Baked 2048x2048 PBR atlas (LOC02_PartitionB / LOC02_PartitionR) + preserved 'Glass' slot
    so Unity's ConfigureReceptionGlazing binds CTCC/ReceptionGlazing automatically!
"""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import hero_lib as H


def build_partition():
    frame = []
    glass = []

    # =========================================================================
    # 1. MOLDED TIMBER BASE SILL RAIL (z = 0.0 .. 0.062)
    # =========================================================================
    sill_prof = [
        (-0.056, 0.000), (0.056, 0.000), (0.056, 0.022), (0.046, 0.032),
        (0.044, 0.056), (0.036, 0.062), (-0.036, 0.062), (-0.044, 0.056),
        (-0.046, 0.032), (-0.056, 0.022),
    ]
    frame.append(H.sweep("RP_SillMolded", sill_prof, [(0.0, -1.28, 0.0), (0.0, 1.28, 0.0)],
                         "wood", grain="Y", smooth=38.0))
    # Brass base anchor angle brackets on inside (-X) of sill
    for by in (-1.20, -0.44, 0.44, 1.20):
        frame.append(H.box(f"RP_BaseBracket_{by:.2f}", (-0.054, by, 0.026), (0.026, 0.036, 0.046), "brass", bevel=0.002))
        frame.append(H.cyl(f"RP_BaseScrew_{by:.2f}", (-0.064, by, 0.032), (-0.070, by, 0.032), 0.004, "brass", seg=10))

    # =========================================================================
    # 2. STEPPED 3-TIER CORNICE HEADER CROWN (z = 1.07 .. 1.195)
    # =========================================================================
    header_prof = [
        (-0.048, 1.072), (0.048, 1.072), (0.048, 1.125),
        (0.058, 1.132), (0.058, 1.162),
        (0.074, 1.174), (0.074, 1.196),
        (-0.074, 1.196), (-0.074, 1.174),
        (-0.058, 1.162), (-0.058, 1.132), (-0.048, 1.125),
    ]
    frame.append(H.sweep("RP_HeaderCrown", header_prof, [(0.0, -1.32, 0.0), (0.0, 1.32, 0.0)],
                         "wood", grain="Y", smooth=38.0))
    # Decorative recessed frieze groove band along east/west header faces
    for sx in (-0.059, 0.059):
        frame.append(H.box(f"RP_HeaderBrassBead_{sx:.3f}", (sx, 0.0, 1.148), (0.006, 2.58, 0.009), "brass", bevel=0.001, grain="Y"))

    # =========================================================================
    # 3. 4 PROFILED VERTICAL TIMBER MULLIONS + GLAZING BEADS + BRASS BRACKETS
    # =========================================================================
    mullions = (-1.24, -0.42, 0.42, 1.24)
    for my in mullions:
        # Chamfered main post
        frame.append(H.box(f"RP_MullionCore_{my:.2f}", (0.0, my, 0.568), (0.076, 0.064, 1.02), "wood", bevel=0.006, seg=2, grain="Z"))
        # Subtle decorative fluting strips on visitor east & staff west faces
        for sx in (-0.040, 0.040):
            frame.append(H.box(f"RP_MullionFlute_{my:.2f}_{sx:.2f}", (sx, my, 0.568), (0.010, 0.036, 0.96), "wood_light", bevel=0.002, grain="Z"))
        # Brass L-corner mortise reinforcement plates at top & bottom of every mullion
        for bz in (0.088, 1.045):
            frame.append(H.box(f"RP_JointPlate_{my:.2f}_{bz:.2f}", (-0.041, my, bz), (0.006, 0.048, 0.044), "brass", bevel=0.001))
            for sy in (-0.014, 0.014):
                frame.append(H.cyl(f"RP_JointScrew_{my:.2f}_{bz:.2f}_{sy:.3f}", (-0.042, my + sy, bz), (-0.046, my + sy, bz), 0.0032, "brass", seg=8))

    # =========================================================================
    # 4. SIDE BAYS (South y in [-1.208, -0.452], North y in [+0.452, +1.208])
    #    Full glass + routed glazing bead strips + mid horizontal transom bar
    # =========================================================================
    for tag, y0, y1 in (("South", -1.208, -0.452), ("North", 0.452, 1.208)):
        ym = 0.5 * (y0 + y1)
        yw = y1 - y0
        # Transom rail at z = 0.84 with profiled lip
        frame.append(H.box(f"RP_Transom_{tag}", (0.0, ym, 0.845), (0.048, yw + 0.008, 0.036), "wood", bevel=0.004, grain="Y"))
        # Mitered inner glazing stop beads clamping the glass on both east & west sides
        for sx in (-0.016, 0.016):
            # Lower sub-pane beads (z = 0.062..0.827)
            frame.append(H.box(f"RP_BeadB_{tag}_{sx:.3f}", (sx, ym, 0.070), (0.012, yw, 0.014), "wood_light", bevel=0.002, grain="Y"))
            frame.append(H.box(f"RP_BeadMT_{tag}_{sx:.3f}", (sx, ym, 0.819), (0.012, yw, 0.014), "wood_light", bevel=0.002, grain="Y"))
            frame.append(H.box(f"RP_BeadL_{tag}_{sx:.3f}", (sx, y0 + 0.008, 0.445), (0.012, 0.014, 0.76), "wood_light", bevel=0.002, grain="Z"))
            frame.append(H.box(f"RP_BeadR_{tag}_{sx:.3f}", (sx, y1 - 0.008, 0.445), (0.012, 0.014, 0.76), "wood_light", bevel=0.002, grain="Z"))
            # Upper transom sub-pane beads (z = 0.863..1.072)
            frame.append(H.box(f"RP_BeadUT_{tag}_{sx:.3f}", (sx, ym, 1.064), (0.012, yw, 0.014), "wood_light", bevel=0.002, grain="Y"))
            frame.append(H.box(f"RP_BeadUB_{tag}_{sx:.3f}", (sx, ym, 0.871), (0.012, yw, 0.014), "wood_light", bevel=0.002, grain="Y"))
            frame.append(H.box(f"RP_BeadUL_{tag}_{sx:.3f}", (sx, y0 + 0.008, 0.968), (0.012, 0.014, 0.21), "wood_light", bevel=0.002, grain="Z"))
            frame.append(H.box(f"RP_BeadUR_{tag}_{sx:.3f}", (sx, y1 - 0.008, 0.968), (0.012, 0.014, 0.21), "wood_light", bevel=0.002, grain="Z"))
        # Clean thin glass pane (preserves Glass material slot -> CTCC/ReceptionGlazing!)
        glass.append(H.box(f"RP_Glass_{tag}", (0.0, ym, 0.568), (0.008, yw - 0.006, 1.00), "Glass", bevel=0.0))

    # =========================================================================
    # 5. CENTER SERVICE WINDOW BAY (y in [-0.388, +0.388])
    #    Pass-through slit z = 0.018..0.255, sliding glass sash above z = 0.265!
    # =========================================================================
    cyw = 0.776
    # Slender horizontal sliding rail bar (z = 0.265) with lower rubber cushion & anodized aluminum U-track
    frame.append(H.box("RP_PassBarWood", (0.0, 0.0, 0.272), (0.044, cyw + 0.008, 0.030), "wood", bevel=0.004, grain="Y"))
    frame.append(H.box("RP_PassBarRubber", (0.0, 0.0, 0.254), (0.034, cyw, 0.009), "rubber", bevel=0.002, grain="Y"))
    frame.append(H.box("RP_SlideTrackBot", (0.0, 0.0, 0.290), (0.022, cyw, 0.012), "steel", bevel=0.002, grain="Y"))
    frame.append(H.box("RP_SlideTrackTop", (0.0, 0.0, 1.064), (0.022, cyw, 0.014), "steel", bevel=0.002, grain="Y"))
    # Glazing beads along center bay sides (above pass bar)
    for sx in (-0.016, 0.016):
        frame.append(H.box(f"RP_CenterBeadL_{sx:.3f}", (sx, -0.380, 0.675), (0.012, 0.014, 0.78), "wood_light", bevel=0.002, grain="Z"))
        frame.append(H.box(f"RP_CenterBeadR_{sx:.3f}", (sx, 0.380, 0.675), (0.012, 0.014, 0.78), "wood_light", bevel=0.002, grain="Z"))
    # Vertical brushed-aluminum finger-pull handle bonded to the sliding glass pane edge
    frame.append(H.box("RP_GlassPullRail", (0.0, -0.348, 0.44), (0.022, 0.016, 0.14), "steel", bevel=0.003, grain="Z"))
    frame.append(H.box("RP_GlassPullGrip", (0.016, -0.348, 0.44), (0.012, 0.012, 0.095), "brass", bevel=0.002, grain="Z"))
    # Center upper glass pane
    glass.append(H.box("RP_CenterGlass", (0.0, 0.0, 0.678), (0.008, cyw - 0.008, 0.775), "Glass", bevel=0.0))

    # =========================================================================
    # 6. RECESSED CONTOURED STAINLESS DOCUMENT / COIN PASS-THROUGH SCOOP TRAY
    #    Spans x in [-0.10, +0.14] (world x in [1.92, 2.16]) — ZERO clip with AN01!
    # =========================================================================
    # Rolled outer flange resting cleanly over the sill cut-out (kept south of y=+0.21 so ZERO contact with F101 BOARD at z=-2.90!)
    tray_x0, tray_x1 = -0.100, 0.115
    tray_y0, tray_y1 = -0.28, 0.20
    tray_xm = 0.5 * (tray_x0 + tray_x1)
    tray_ym = 0.5 * (tray_y0 + tray_y1)
    tray_w = tray_x1 - tray_x0
    tray_len = tray_y1 - tray_y0
    # Outer bezel lip + beveled stepped surround
    frame.append(H.box("RP_TrayLipN", (tray_xm, tray_y1 - 0.015, 0.015), (tray_w, 0.030, 0.014), "steel", bevel=0.004, grain="X"))
    frame.append(H.box("RP_TrayLipS", (tray_xm, tray_y0 + 0.015, 0.015), (tray_w, 0.030, 0.014), "steel", bevel=0.004, grain="X"))
    frame.append(H.box("RP_TrayLipE", (tray_x1 - 0.015, tray_ym, 0.015), (0.030, tray_len, 0.014), "steel", bevel=0.004, grain="Y"))
    frame.append(H.box("RP_TrayLipW", (tray_x0 + 0.015, tray_ym, 0.015), (0.030, tray_len, 0.014), "steel", bevel=0.004, grain="Y"))
    # Concave brushed stainless scoop deck recessed inside the lip
    def scoop_fn(u, v):
        x = (tray_x0 + 0.020) + u * (tray_w - 0.040)
        y = (tray_y0 + 0.020) + v * (tray_len - 0.040)
        dip = math.sin(u * math.pi) * math.sin(v * math.pi)
        z = 0.015 - 0.009 * (dip ** 0.7)
        return (x, y, z)
    frame.append(H.sculpted_grid("RP_TrayScoopWell", 10, 12, scoop_fn, "steel", thickness=0.004, grain="Y"))
    # 4 recessed countersunk brass screws at tray flange corners
    for sx in (tray_x0 + 0.014, tray_x1 - 0.014):
        for sy in (tray_y0 + 0.014, tray_y1 - 0.014):
            frame.append(H.cyl(f"RP_TrayScrew_{sx:.3f}_{sy:.3f}", (sx, sy, 0.019), (sx, sy, 0.023), 0.004, "brass", seg=10))

    # =========================================================================
    # 7. TWO TAPED VIETNAMESE PERIOD NOTICES AFFIXED TO LOWER GLASS BAYS
    #    Printed front faces visitors (+X); blank cream paper backing faces staff (-X) so ZERO mirrored bleed!
    # =========================================================================
    def notice_sheet(name, cy, cz, w_y, h_z, uv_rect, curl=0.003):
        def fn_front(u, v):
            y = cy + (u - 0.5) * w_y
            z = cz + (v - 0.5) * h_z
            x = -0.0065 - curl * ((1.0 - v) ** 2) * abs(u - 0.5) * 2.0
            return (x, y, z)
        def fn_back(u, v):
            y = cy + (u - 0.5) * w_y
            z = cz + (v - 0.5) * h_z
            x = -0.0092 - curl * ((1.0 - v) ** 2) * abs(u - 0.5) * 2.0
            return (x, y, z)
        front = H.sculpted_grid(name, 4, 4, fn_front, "print:partition_notices", thickness=0.0014, print_uv=True, uv_rect=uv_rect)
        back = H.sculpted_grid(name + "_Back", 4, 4, fn_back, "paper", thickness=0.0012)
        return [front, back]

    # Top half of partition_notices.png (v in [0.52, 0.98]) on South bay
    notices = []
    notices.extend(notice_sheet("RP_NoticeSouth", -0.82, 0.25, 0.34, 0.22, (0.02, 0.52, 0.98, 0.98)))
    # Bottom half of partition_notices.png (v in [0.02, 0.48]) on North bay
    notices.extend(notice_sheet("RP_NoticeNorth", 0.82, 0.26, 0.32, 0.20, (0.02, 0.02, 0.98, 0.48)))
    # Yellowed Scotch-tape tabs at top corners of both notices
    for ny, nz, nw, nh in ((-0.82, 0.25, 0.34, 0.22), (0.82, 0.26, 0.32, 0.20)):
        for sgn in (-1, 1):
            notices.append(H.box(f"RP_Tape_{ny:.2f}_{sgn}", (-0.0055, ny + sgn * (nw * 0.44), nz + nh * 0.48),
                                 (0.002, 0.036, 0.016), "paper", bevel=0.0, rot=(sgn * 0.35, 0, 0)))

    # =========================================================================
    # 8. FLANGED CEILING SUSPENSION RODS + TURNBUCKLES (z = 1.196 .. 2.475)
    # =========================================================================
    for py in (-1.18, 1.18):
        # Bolted steel header mounting collar + 4 bolt heads
        frame.append(H.cyl(f"RP_HeadFlange_{py:.2f}", (0.0, py, 1.194), (0.0, py, 1.210), 0.044, "steel_dark", seg=20, bevel=0.002))
        frame.append(H.cyl(f"RP_HeadCollar_{py:.2f}", (0.0, py, 1.208), (0.0, py, 1.242), 0.026, "steel_dark", seg=16))
        # Lower threaded rod -> turnbuckle body -> upper rod
        frame.append(H.cyl(f"RP_RodLow_{py:.2f}", (0.0, py, 1.235), (0.0, py, 1.50), 0.015, "steel", seg=16))
        # Hex turnbuckle tension coupler
        frame.append(H.cyl(f"RP_Turnbuckle_{py:.2f}", (0.0, py, 1.50), (0.0, py, 1.61), 0.023, "brass", seg=6, bevel=0.002))
        frame.append(H.cyl(f"RP_LockNutL_{py:.2f}", (0.0, py, 1.482), (0.0, py, 1.502), 0.021, "steel_dark", seg=6))
        frame.append(H.cyl(f"RP_LockNutU_{py:.2f}", (0.0, py, 1.608), (0.0, py, 1.628), 0.021, "steel_dark", seg=6))
        frame.append(H.cyl(f"RP_RodHigh_{py:.2f}", (0.0, py, 1.62), (0.0, py, 2.465), 0.015, "steel", seg=16))
        # Ceiling anchor escutcheon plate right at z = 2.472 (ceiling y = 3.60)
        frame.append(H.cyl(f"RP_CeilFlange_{py:.2f}", (0.0, py, 2.458), (0.0, py, 2.476), 0.056, "steel_dark", seg=22, bevel=0.003))
        frame.append(H.cyl(f"RP_CeilCollar_{py:.2f}", (0.0, py, 2.432), (0.0, py, 2.460), 0.028, "steel_dark", seg=16))
        for k in range(4):
            a = k * math.pi * 0.5 + 0.4
            frame.append(H.cyl(f"RP_CeilBolt_{py:.2f}_{k}",
                               (math.cos(a) * 0.040, py + math.sin(a) * 0.040, 2.448),
                               (math.cos(a) * 0.040, py + math.sin(a) * 0.040, 2.460),
                               0.0045, "brass", seg=8))

    return {"Frame": frame, "Notices": notices, "Glass": glass}


def run():
    H.export_hero_asset(
        "CTCC_ENV_ReceptionPartition",
        build_partition,
        atlas_specs={
            "LOC02_Partition": {"res": 2048, "bake_r": True, "parts": ["Frame"], "margin": 0.004},
            "LOC02_Notices": {"res": 1024, "bake_r": True, "parts": ["Notices"], "margin": 0.008},
        },
        make_lods=True,
        separate_parts=False,
        cam_dir=(1.65, -1.25, 0.52),
        render=True,
        ao_samples=16,
    )


if __name__ == "__main__":
    run()
