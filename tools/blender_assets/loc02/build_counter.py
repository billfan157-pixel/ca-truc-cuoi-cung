"""Hero Blender 4.5 asset: CTCC_LOC02_ReceptionCounter.

1987 Vietnamese district hospital two-tier reception & night-duty counter (LOC02):
  - Origin matches `desk` transform in Unity: world (1.45, 0, -3.24), yaw=0
  - Blender +X = Unity +X (east, visitors' side), +Y = Unity +Z (north), +Z = Unity +Y (up)
  - Exact locked heights: main counter top z = 1.1275m, lower accessible wing z = 0.815m
  - Built-in recessed drawer housing pocket centered at (0.51, -1.82, 0.66) for DRAWER-DESK
  - Ergonomic staff side (west, Lan's side): recessed kneehole cavity (y in [-0.88, +0.90]),
    tubular steel footrest bar, twin under-desk wooden pedestals with paneled drawers,
    brass cup pulls, shelves & stacked bound archival registers.
  - Dual 2048x2048 Cycles-baked URP PBR atlases (LOC02_CounterTop & LOC02_CounterBody),
    each baked for both B (2005 operational) and R (2012 abandoned damp/weathered).
"""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import hero_lib as H


def build_counter():
    top_parts = []
    body_parts = []

    # =========================================================================
    # 1. MAIN COUNTER TOP SLAB (z = 1.032 .. 1.1275, x in [-0.74, +0.74], y in [-2.10, +2.00])
    #    South end extended cleanly to -2.10 so it overhangs the built-in DRAWER-DESK column flush!
    # =========================================================================
    # Cast terrazzo slab core (top surface exact z = 1.1275)
    top_parts.append(H.box("CounterTerrazzoSlab", (0.0, -0.05, 1.0875), (1.44, 4.06, 0.080), "terrazzo", bevel=0.004, seg=2))
    # Molded dark ironwood bullnose perimeter edge-banding framing the terrazzo top flush at z=1.1275
    bullnose_prof = [
        (-0.022, -0.044), (0.016, -0.044), (0.024, -0.036),
        (0.028, -0.014), (0.028, 0.018), (0.022, 0.034), (0.012, 0.040),
        (-0.022, 0.040),
    ]
    top_parts.append(H.sweep("TopTrimEast", bullnose_prof, [(0.715, -2.10, 1.0875), (0.715, 2.00, 1.0875)],
                             "wood", grain="Y", smooth=45.0))
    top_parts.append(H.sweep("TopTrimWest", [(-u, v) for u, v in bullnose_prof[::-1]],
                             [(-0.715, -2.10, 1.0875), (-0.715, 2.00, 1.0875)], "wood", grain="Y", smooth=45.0))
    for sy in (1.982, -2.082):
        top_parts.append(H.box(f"TopTrimEnd_{sy:.2f}", (0.0, sy, 1.0875), (1.486, 0.042, 0.084), "wood", bevel=0.008, seg=3, grain="X"))
    # Recessed brass inlay reveal strip along east & north top margin
    top_parts.append(H.box("TopBrassInlayE", (0.700, -0.05, 1.1265), (0.010, 4.02, 0.004), "brass", bevel=0.001, grain="Y"))
    top_parts.append(H.box("TopBrassInlayN", (0.0, 1.964, 1.1265), (1.40, 0.010, 0.004), "brass", bevel=0.001, grain="X"))
    # Recessed underside shadow-gap cove molding beneath the overhanging counter lip
    top_parts.append(H.box("UnderLipCoveE", (0.655, -0.05, 1.036), (0.065, 4.02, 0.024), "wood", bevel=0.004, grain="Y"))
    top_parts.append(H.box("UnderLipCoveN", (0.0, 1.930, 1.036), (1.34, 0.065, 0.024), "wood", bevel=0.004, grain="X"))
    top_parts.append(H.box("UnderLipCoveS", (0.0, -2.045, 1.036), (1.34, 0.065, 0.024), "wood", bevel=0.004, grain="X"))
    # Sleek satin-brass L-corner protectors flush on the 2 exposed visitor corners (NE & SE)
    for sy in (1.998, -2.098):
        sgn = 1.0 if sy > 0 else -1.0
        top_parts.append(H.box(f"TopCornerBrassA_{sy:.2f}", (0.742, sy - sgn * 0.036, 1.0875),
                               (0.010, 0.072, 0.080), "brass", bevel=0.002))
        top_parts.append(H.box(f"TopCornerBrassB_{sy:.2f}", (0.708, sy, 1.0875),
                               (0.072, 0.010, 0.080), "brass", bevel=0.002))

    # =========================================================================
    # 2. LOWER ACCESSIBLE COUNTER WING TOP (local (0.05, -2.32, 0), exact top z = 0.815)
    # =========================================================================
    lx0, ly0 = 0.05, -2.32
    top_parts.append(H.box("LowerTerrazzoSlab", (lx0, ly0, 0.7825), (1.21, 0.67, 0.065), "terrazzo", bevel=0.004, seg=2))
    top_parts.append(H.box("LowerTrimE", (lx0 + 0.612, ly0, 0.7825), (0.036, 0.715, 0.070), "wood", bevel=0.007, seg=3, grain="Y"))
    top_parts.append(H.box("LowerTrimW", (lx0 - 0.612, ly0, 0.7825), (0.036, 0.715, 0.070), "wood", bevel=0.007, seg=3, grain="Y"))
    top_parts.append(H.box("LowerTrimS", (lx0, ly0 - 0.342, 0.7825), (1.255, 0.036, 0.070), "wood", bevel=0.007, seg=3, grain="X"))
    top_parts.append(H.box("LowerBrassInlayE", (lx0 + 0.596, ly0, 0.814), (0.008, 0.65, 0.004), "brass", bevel=0.001, grain="Y"))
    top_parts.append(H.box("LowerCornerBrassSE_A", (lx0 + 0.630, ly0 - 0.326, 0.7825), (0.010, 0.056, 0.068), "brass", bevel=0.002))
    top_parts.append(H.box("LowerCornerBrassSE_B", (lx0 + 0.606, ly0 - 0.358, 0.7825), (0.056, 0.010, 0.068), "brass", bevel=0.002))

    # =========================================================================
    # 3. GROUNDED TERRAZZO PLINTH & BRUSHED STEEL KICKPLATE (z = 0.0 .. 0.115)
    # =========================================================================
    # Main plinth recessed 5cm inside apron for authentic toe-kick
    body_parts.append(H.box("MainPlinthCore", (0.04, -0.02, 0.058), (1.16, 3.92, 0.116), "terrazzo_dark", bevel=0.005, seg=2))
    # Brushed stainless floor scuff strip along visitor east & north toe-kick
    body_parts.append(H.box("KickPlateEast", (0.623, -0.02, 0.052), (0.008, 3.88, 0.092), "steel", bevel=0.002, grain="Y"))
    body_parts.append(H.box("KickPlateNorth", (0.04, 1.943, 0.052), (1.14, 0.008, 0.092), "steel", bevel=0.002, grain="X"))
    # Brass scuff bolts along kickplate
    for by in (-1.75, -1.10, -0.45, 0.20, 0.85, 1.50):
        body_parts.append(H.cyl(f"KickRivet_{by:.2f}", (0.625, by, 0.052), (0.631, by, 0.052), 0.0045, "brass", seg=10))
    # Lower wing plinth
    body_parts.append(H.box("LowerPlinth", (lx0, ly0, 0.052), (1.12, 0.64, 0.104), "terrazzo_dark", bevel=0.005, seg=2))
    body_parts.append(H.box("LowerKickE", (lx0 + 0.563, ly0, 0.048), (0.008, 0.62, 0.084), "steel", bevel=0.002, grain="Y"))
    body_parts.append(H.box("LowerKickS", (lx0, ly0 - 0.323, 0.048), (1.10, 0.008, 0.084), "steel", bevel=0.002, grain="X"))

    # =========================================================================
    # 4. VISITOR-FACING EAST APRON & NORTH/SOUTH RETURNS (Framed 1987 Green Wainscot + Wood Molding!)
    # =========================================================================
    # Main east apron masonry wall (x in [0.02, 0.625], split cleanly around DRAWER-DESK pocket at y in [-2.035, -1.605], z in [0.585, 0.735])
    # Main north/center body (y in [-1.605, +1.92])
    body_parts.append(H.box("ApronEastMain", (0.31, 0.1575, 0.575), (0.63, 3.525, 0.92), "paint_green", bevel=0.004, grain="Y"))
    # South drawer bay column (y in [-2.05, -1.605]): below pocket, above pocket, and rear pocket backing!
    body_parts.append(H.box("ApronDrawerBelow", (0.31, -1.8275, 0.348), (0.63, 0.445, 0.474), "paint_green", bevel=0.004, grain="Y"))
    body_parts.append(H.box("ApronDrawerAbove", (0.31, -1.8275, 0.885), (0.63, 0.445, 0.300), "paint_green", bevel=0.004, grain="Y"))
    body_parts.append(H.box("ApronDrawerBack", (0.14, -1.8275, 0.660), (0.29, 0.445, 0.160), "wood", bevel=0.003))
    body_parts.append(H.box("ApronDrawerSouthWall", (0.31, -2.042, 0.660), (0.63, 0.032, 0.160), "wood", bevel=0.003))
    # Molded wooden casing frame cleanly outlining the built-in DRAWER-DESK opening!
    dc_x = 0.628
    body_parts.append(H.box("DrawerTrimTop", (dc_x, -1.82, 0.742), (0.022, 0.465, 0.026), "wood", bevel=0.003, grain="Y"))
    body_parts.append(H.box("DrawerTrimBot", (dc_x, -1.82, 0.578), (0.022, 0.465, 0.026), "wood", bevel=0.003, grain="Y"))
    body_parts.append(H.box("DrawerTrimN", (dc_x, -1.595, 0.660), (0.022, 0.026, 0.188), "wood", bevel=0.003, grain="Z"))
    body_parts.append(H.box("DrawerTrimS", (dc_x, -2.045, 0.660), (0.022, 0.026, 0.188), "wood", bevel=0.003, grain="Z"))

    # Horizontal dado / chair-rail & bottom base timber moldings across east visitor face
    body_parts.append(H.box("EastTopRail", (0.630, -0.05, 0.995), (0.028, 3.96, 0.055), "wood", bevel=0.005, grain="Y"))
    body_parts.append(H.box("EastBotRail", (0.630, -0.05, 0.145), (0.028, 3.96, 0.055), "wood", bevel=0.005, grain="Y"))
    # Vertical timber pilasters dividing east apron into 3 balanced architectural bays
    # Center bay y in [-0.72, +0.50] cleanly frames the "BÀN TRỰC" sign (y in [-0.575, +0.355])!
    for py in (-1.56, -0.74, 0.52, 1.88):
        body_parts.append(H.box(f"EastPilaster_{py:.2f}", (0.630, py, 0.570), (0.030, 0.072, 0.84), "wood", bevel=0.005, grain="Z"))
    # Recessed chamfered wainscot panel picture-frame moldings inside each of the 3 bays
    for tag, y0, y1 in (("South", -1.49, -0.81), ("Center", -0.66, 0.44), ("North", 0.60, 1.80)):
        ym = 0.5 * (y0 + y1)
        yw = y1 - y0
        # Top & bottom recessed picture molding rails
        body_parts.append(H.box(f"BayMoldT_{tag}", (0.629, ym, 0.905), (0.018, yw, 0.024), "wood", bevel=0.003, grain="Y"))
        body_parts.append(H.box(f"BayMoldB_{tag}", (0.629, ym, 0.235), (0.018, yw, 0.024), "wood", bevel=0.003, grain="Y"))
        body_parts.append(H.box(f"BayMoldL_{tag}", (0.629, y0 + 0.012, 0.570), (0.018, 0.024, 0.69), "wood", bevel=0.003, grain="Z"))
        body_parts.append(H.box(f"BayMoldR_{tag}", (0.629, y1 - 0.012, 0.570), (0.018, 0.024, 0.69), "wood", bevel=0.003, grain="Z"))
    # Vertical brushed-brass corner bumper guards at NE & SE apron edges
    body_parts.append(H.box("ApronCornerGuardNE_A", (0.638, 1.908, 0.570), (0.016, 0.044, 0.86), "brass", bevel=0.002, grain="Z"))
    body_parts.append(H.box("ApronCornerGuardNE_B", (0.614, 1.926, 0.570), (0.044, 0.016, 0.86), "brass", bevel=0.002, grain="Z"))

    # North return end-panel (y = +1.92) framed nicely with timber molding
    body_parts.append(H.box("NorthTopRail", (-0.01, 1.925, 0.995), (1.30, 0.026, 0.055), "wood", bevel=0.005, grain="X"))
    body_parts.append(H.box("NorthBotRail", (-0.01, 1.925, 0.145), (1.30, 0.026, 0.055), "wood", bevel=0.005, grain="X"))
    for px in (-0.61, 0.02, 0.60):
        body_parts.append(H.box(f"NorthStile_{px:.2f}", (px, 1.925, 0.570), (0.062, 0.026, 0.84), "wood", bevel=0.004, grain="Z"))

    # =========================================================================
    # 5. LOWER ACCESSIBLE WING APRON (local (0.05, -2.32), z = 0.104 .. 0.748)
    # =========================================================================
    body_parts.append(H.box("LowerApronBody", (lx0, ly0, 0.426), (1.16, 0.64, 0.644), "paint_green", bevel=0.004))
    body_parts.append(H.box("LowerRailTopE", (lx0 + 0.584, ly0, 0.715), (0.022, 0.65, 0.045), "wood", bevel=0.004, grain="Y"))
    body_parts.append(H.box("LowerRailBotE", (lx0 + 0.584, ly0, 0.135), (0.022, 0.65, 0.045), "wood", bevel=0.004, grain="Y"))
    body_parts.append(H.box("LowerRailTopS", (lx0, ly0 - 0.324, 0.715), (1.18, 0.022, 0.045), "wood", bevel=0.004, grain="X"))
    body_parts.append(H.box("LowerRailBotS", (lx0, ly0 - 0.324, 0.135), (1.18, 0.022, 0.045), "wood", bevel=0.004, grain="X"))
    for px in (lx0 - 0.54, lx0, lx0 + 0.54):
        body_parts.append(H.box(f"LowerStileS_{px:.2f}", (px, ly0 - 0.324, 0.425), (0.052, 0.022, 0.58), "wood", bevel=0.004, grain="Z"))
    # Recessed lower wing west cubby/shelf face
    body_parts.append(H.box("LowerBackFrameT", (lx0 - 0.584, ly0, 0.715), (0.022, 0.65, 0.045), "wood", bevel=0.004, grain="Y"))
    body_parts.append(H.box("LowerBackFrameB", (lx0 - 0.584, ly0, 0.135), (0.022, 0.65, 0.045), "wood", bevel=0.004, grain="Y"))

    # =========================================================================
    # 6. STAFF WORKSTATION SIDE (WEST, x in [-0.66, 0.02]): ERGONOMIC KNEEHOLE + PEDESTALS!
    # =========================================================================
    # Kneehole back modesty panel at x = -0.02 (gives Lan ~68cm legroom from x = -0.70!)
    body_parts.append(H.box("KneeholeModesty", (-0.015, 0.01, 0.575), (0.032, 1.80, 0.90), "wood", bevel=0.003, grain="Y"))
    # Shallow under-desk wooden rail & subtle pencil drawer under the tabletop (z = 0.96..1.03, well above knees!)
    body_parts.append(H.box("KneeholeApronRail", (-0.64, 0.01, 1.005), (0.035, 1.78, 0.068), "wood", bevel=0.004, grain="Y"))
    body_parts.append(H.box("CenterTrayFace", (-0.662, 0.01, 1.005), (0.018, 0.68, 0.056), "wood_light", bevel=0.003, grain="Y"))
    body_parts.append(H.box("CenterTrayPull", (-0.678, 0.01, 1.005), (0.018, 0.092, 0.018), "brass", bevel=0.003))
    # Tubular painted-steel footrest rail & mounting flanges across the kneehole (x = -0.36, z = 0.165)
    body_parts.append(H.cyl("FootrestBar", (-0.36, -0.88, 0.165), (-0.36, 0.90, 0.165), 0.016, "steel_dark", seg=16))
    for fy in (-0.86, 0.88):
        body_parts.append(H.cyl(f"FootrestFlange_{fy:.2f}", (-0.36, fy - 0.01, 0.165), (-0.36, fy + 0.01, 0.165), 0.034, "brass", seg=14))
    # Under-desk power conduit & vintage twin bakelite power strip mounted on modesty wall
    body_parts.append(H.box("UnderDeskPowerStrip", (-0.045, -0.35, 0.84), (0.042, 0.24, 0.058), "plastic_ivory", bevel=0.004))
    body_parts.append(H.cyl("UnderDeskConduit", (-0.040, -0.86, 0.84), (-0.040, 0.88, 0.84), 0.009, "steel_dark", seg=10))

    # --- NORTH PEDESTAL (y in [+0.90, +1.91], x in [-0.66, 0.02]): 3 staff drawers + open top binder cubby ---
    body_parts.append(H.box("NorthPedBody", (-0.31, 1.405, 0.575), (0.64, 1.01, 0.92), "wood", bevel=0.004, grain="Z"))
    # Open binder shelf niche carved into upper north pedestal (visual depth via recessed dark bay + real binders!)
    body_parts.append(H.box("NorthCubbyShelf", (-0.54, 1.405, 0.72), (0.26, 0.92, 0.024), "wood_light", bevel=0.003, grain="Y"))
    body_parts.append(H.box("NorthCubbyTop", (-0.54, 1.405, 1.01), (0.26, 0.92, 0.024), "wood_light", bevel=0.003, grain="Y"))
    # 3 paneled staff drawers on lower north pedestal (z = 0.21, 0.39, 0.57) facing west (-X)
    for di, dz in enumerate((0.21, 0.39, 0.57)):
        body_parts.append(H.box(f"NorthDrawerFace_{di}", (-0.642, 1.405, dz), (0.024, 0.88, 0.158), "wood_light", bevel=0.004, grain="Y"))
        # Antique brass cup pull + index label frame
        body_parts.append(H.box(f"NorthDrawerPull_{di}", (-0.660, 1.405, dz - 0.008), (0.020, 0.095, 0.026), "brass", bevel=0.004))
        body_parts.append(H.box(f"NorthDrawerLabel_{di}", (-0.658, 1.405, dz + 0.042), (0.010, 0.088, 0.032), "brass", bevel=0.002))
    # Bound archival hospital ledgers standing on top open cubby (z = 0.732..0.98)
    for bi, (by, bw, bh, matk, tilt) in enumerate([
        (1.06, 0.046, 0.24, "cloth_maroon", 0.02),
        (1.12, 0.042, 0.23, "cloth_navy", 0.0),
        (1.18, 0.052, 0.245, "kraft", -0.03),
        (1.25, 0.044, 0.225, "cloth_olive", 0.01),
        (1.31, 0.048, 0.235, "cloth_maroon", 0.04),
        (1.42, 0.056, 0.24, "kraft", -0.06),
        (1.62, 0.26, 0.052, "paper", 0.0),   # flat stack of forms
        (1.62, 0.24, 0.042, "kraft", 0.0),
    ]):
        if bi < 6:
            body_parts.append(H.box(f"NorthArchBook_{bi}", (-0.57, by, 0.735 + bh * 0.5),
                                    (0.21, bw, bh), matk, bevel=0.004, rot=(tilt, 0, 0)))
        else:
            zoff = 0.762 if bi == 6 else 0.812
            body_parts.append(H.box(f"NorthArchStack_{bi}", (-0.56, by, zoff),
                                    (0.21, bw, bh), matk, bevel=0.003))

    # --- SOUTH PEDESTAL (y in [-1.96, -0.88], x in [-0.66, 0.02]): Framed cabinet door + archival storage ---
    body_parts.append(H.box("SouthPedBody", (-0.31, -1.42, 0.575), (0.64, 1.08, 0.92), "wood", bevel=0.004, grain="Z"))
    # Twin paneled cupboard doors facing west (-X)
    for si, sy in enumerate((-1.68, -1.16)):
        body_parts.append(H.box(f"SouthDoorLeaf_{si}", (-0.642, sy, 0.570), (0.024, 0.48, 0.82), "wood_light", bevel=0.004, grain="Z"))
        # Inset recessed molding frame on each cupboard door
        body_parts.append(H.box(f"SouthDoorTrimT_{si}", (-0.654, sy, 0.89), (0.010, 0.38, 0.022), "wood", bevel=0.002, grain="Y"))
        body_parts.append(H.box(f"SouthDoorTrimB_{si}", (-0.654, sy, 0.25), (0.010, 0.38, 0.022), "wood", bevel=0.002, grain="Y"))
        body_parts.append(H.box(f"SouthDoorTrimL_{si}", (-0.654, sy - 0.18, 0.57), (0.010, 0.022, 0.66), "wood", bevel=0.002, grain="Z"))
        body_parts.append(H.box(f"SouthDoorTrimR_{si}", (-0.654, sy + 0.18, 0.57), (0.010, 0.022, 0.66), "wood", bevel=0.002, grain="Z"))
        # Brass door knob + keyhole escutcheon
        ky = sy + (0.16 if si == 0 else -0.16)
        body_parts.append(H.cyl(f"SouthKnobStem_{si}", (-0.652, ky, 0.62), (-0.674, ky, 0.62), 0.007, "brass", seg=12))
        body_parts.append(H.sphere(f"SouthKnob_{si}", (-0.680, ky, 0.62), (0.024, 0.028, 0.028), "brass", seg=14, rings=10))
        # Barrel hinges
        hy = sy + (-0.235 if si == 0 else 0.235)
        for hz in (0.28, 0.86):
            body_parts.append(H.cyl(f"SouthHinge_{si}_{hz:.2f}", (-0.656, hy, hz - 0.028), (-0.656, hy, hz + 0.028), 0.006, "brass", seg=10))

    return {"CounterTop": top_parts, "CounterBody": body_parts}


def run():
    H.export_hero_asset(
        "CTCC_LOC02_ReceptionCounter",
        build_counter,
        atlas_specs={
            "LOC02_CounterTop": {"res": 2048, "bake_r": True, "parts": ["CounterTop"], "margin": 0.004},
            "LOC02_CounterBody": {"res": 2048, "bake_r": True, "parts": ["CounterBody"], "margin": 0.004},
        },
        make_lods=True,
        separate_parts=False,
        cam_dir=(1.65, -1.35, 0.72),
        render=True,
        ao_samples=16,
    )


if __name__ == "__main__":
    run()
