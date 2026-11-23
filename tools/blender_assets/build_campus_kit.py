"""An Sinh Hospital Full Campus Modular 3D Architectural Kit (1987-2005) — Blender 4.5 LTS.

Generates 14 modular, real-scale, pivot-aligned architectural & campus FBX assets with automatic LOD0/LOD1/LOD2
and editable .blend sources in ArtSource/Blender/:

1.  CTCC_ENV_MainFacadeBay_1987      (4.0 x 4.0 m K-A 1987 ochre masonry bay w/ brise-soleil & hoa sắt window)
2.  CTCC_ENV_StairwellBreezeBay_1987 (4.0 x 4.0 m 1987 perforated concrete breeze-block stairwell tower bay)
3.  CTCC_ENV_ClinicFacadeBay_2002    (4.0 x 4.0 m 1998-2002 K-B/K-E cream stucco bay w/ casement window & AC unit)
4.  CTCC_ENV_WardBalconyBay_2004     (4.0 x 4.0 m 2001-2004 K-C inpatient ward bay w/ cantilevered balcony & rail)
5.  CTCC_ENV_CorrugatedRoofModule    (8.0 x 8.0 m Vietnamese pitched ribbed corrugated iron roof w/ purlins & gutter)
6.  CTCC_ENV_CoveredWalkwayBay       (4.0 x 4.0 m open-sided hospital connecting walkway w/ steel truss & T8 tube)
7.  CTCC_ENV_AmbulanceCanopy         (6.5 x 13.0 m K-D Emergency porte-cochère w/ trusses, bollards & roof sign frame)
8.  CTCC_ENV_MainHospitalArchGate    (8.6 x 5.0 m monumental Cổng chính entrance arch, piers & wrought-iron gates)
9.  CTCC_ENV_WaterTowerAndRoofKit    (16.5 m Vietnamese hospital concrete water tower + stainless tank & ladder)
10. CTCC_ENV_RooftopTankCluster      (Rooftop twin stainless water tanks + stair bulkhead + lightning mast)
11. CTCC_ENV_UtilityPoleAndLamp      (8.5 m centrifugal concrete utility pole + cobra-head sodium streetlamp)
12. CTCC_ENV_ParkingShedBay          (6.0 x 4.5 m steel-post corrugated motorbike shelter for K-B forecourt)
13. CTCC_PROP_VintageAmbulance       (5.1 m 1990s Vietnamese boxy hospital ambulance van — fictional original mesh)
14. CTCC_ENV_B1SealedDoorE05b        (2.4 x 2.6 m B1 Khu C 2004 bricked-in rear door evidence assembly for W-Q03)

Coordinate convention (matching HospitalAuthoring.PlaceBlenderModel at yaw=0):
- Front exterior face faces -Y (maps to Unity -Z South at yaw=0, -X West at yaw=90, +Z North at yaw=180, +X East at yaw=-90)
- Base rests cleanly on Z=0 (Unity Y=0)
"""

import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import common as C


def build_main_facade_bay_1987():
    """4.0m wide x 4.0m tall 1987 K-A Vietnamese provincial hospital exterior wall bay.
    Features ochre limewash wall, structural slab band, corner pilasters, recessed 3-pane steel window
    with welded iron security grille (hoa sắt), concrete rain hood (ô văng), and twin angled brise-soleil blades."""
    objs = []
    # Main recessed masonry spandrel & jamb walls (front face at Y=0, thickness 0.28m into +Y)
    objs.append(C.make_box("WallSpandrelLow", (0.0, 0.14, 0.56), (4.0, 0.28, 1.12), "ExteriorFacade", bevel=0.01, uv_scale=2.2))
    objs.append(C.make_box("WallSpandrelHigh", (0.0, 0.14, 3.42), (4.0, 0.28, 1.16), "ExteriorFacade", bevel=0.01, uv_scale=2.2))
    for sx in (-1.62, 1.62):
        objs.append(C.make_box(f"WallPier_{sx:.1f}", (sx, 0.14, 2.00), (0.76, 0.28, 1.80), "ExteriorFacade", bevel=0.01, uv_scale=2.2))

    # Horizontal concrete floor slab band (top/bottom stringcourse) & vertical chamfered structural pilasters
    objs.append(C.make_box("SlabBand", (0.0, 0.04, 3.84), (4.04, 0.44, 0.32), "BaseTrim", bevel=0.012, uv_scale=2.0))
    for px in (-1.94, 1.94):
        objs.append(C.make_box(f"Pilaster_{px:.1f}", (px, 0.02, 2.00), (0.24, 0.34, 4.00), "BaseTrim", bevel=0.012, uv_scale=2.0))

    # Window opening: X in [-1.24..+1.24] (w=2.48m), Z in [1.12..2.84] (h=1.72m)
    objs.append(C.make_box("WinSill", (0.0, -0.05, 1.10), (2.68, 0.40, 0.09), "BaseTrim", bevel=0.008, uv_scale=1.5))
    objs.append(C.make_box("WinFrameTop", (0.0, 0.08, 2.82), (2.48, 0.10, 0.05), "Frame", bevel=0.004))
    objs.append(C.make_box("WinFrameBot", (0.0, 0.08, 1.16), (2.48, 0.10, 0.05), "Frame", bevel=0.004))
    objs.append(C.make_box("WinTransom", (0.0, 0.08, 2.38), (2.46, 0.10, 0.05), "Frame", bevel=0.004))
    for mx in (-1.22, -0.42, 0.42, 1.22):
        objs.append(C.make_box(f"WinMullion_{mx:.2f}", (mx, 0.08, 1.99), (0.048, 0.09, 1.68), "Frame", bevel=0.004))

    # Window glass backing (swaps WindowDarkR in R 2012 <-> WindowLitB in B 2005!)
    objs.append(C.make_box("WinGlassPane", (0.0, 0.16, 1.99), (2.42, 0.024, 1.64), "WindowDarkR", bevel=0.0))

    # Upper transom louver blades (4 tilted glass blades)
    for k in range(4):
        lz = 2.46 + k * 0.088
        objs.append(C.make_box(f"Louver_{k}", (0.0, 0.08, lz), (2.38, 0.08, 0.012), "FrostedGlass", bevel=0.001, rot=(math.radians(32), 0, 0)))

    # Exterior security iron grille bars (hoa sắt)
    for gz in (1.42, 1.88, 2.28):
        objs.append(C.make_cylinder(f"GrilleH_{gz:.2f}", (-1.20, 0.03, gz), (1.20, 0.03, gz), 0.009, "Frame", segments=6))
    for i in range(9):
        gx = -1.04 + i * 0.26
        objs.append(C.make_cylinder(f"GrilleV_{i}", (gx, 0.03, 1.18), (gx, 0.03, 2.36), 0.009, "Frame", segments=6))

    # Projecting concrete rain hood (ô văng) + 2 angled brise-soleil sun-shading blades (lam chắn nắng)
    objs.append(C.make_box("RainHood", (0.0, -0.26, 2.94), (2.82, 0.64, 0.10), "BaseTrim", bevel=0.010, rot=(math.radians(4), 0, 0), uv_scale=1.6))
    for side in (-1.36, 1.36):
        objs.append(C.make_box(f"FinCheek_{side:.1f}", (side, -0.22, 2.48), (0.09, 0.50, 0.96), "BaseTrim", bevel=0.008))
    for fz in (2.56, 2.18):
        objs.append(C.make_box(f"BriseBlade_{fz:.2f}", (0.0, -0.34, fz), (2.68, 0.24, 0.052), "ExteriorFacade", bevel=0.006, rot=(math.radians(16), 0, 0)))

    # Interior-side plaster return + dado so open-backed light-court/corridor rooms look authentic
    objs.append(C.make_box("IntDado", (0.0, 0.27, 0.56), (3.96, 0.04, 1.12), "DadoR", bevel=0.004, uv_scale=2.2))
    return objs


def build_stairwell_breeze_bay_1987():
    """4.0 x 4.0 m iconic 1987 Vietnamese hospital perforated concrete breeze-block stairwell bay (gạch thông gió)."""
    objs = []
    # Side structural piers + slab band
    for sx in (-1.78, 1.78):
        objs.append(C.make_box(f"StairPier_{sx:.1f}", (sx, 0.14, 2.00), (0.46, 0.38, 4.00), "ExteriorFacade", bevel=0.014, uv_scale=2.0))
    objs.append(C.make_box("StairSlabBand", (0.0, 0.04, 3.84), (4.04, 0.44, 0.32), "BaseTrim", bevel=0.012, uv_scale=2.0))
    objs.append(C.make_box("StairSillBand", (0.0, 0.06, 0.18), (3.60, 0.36, 0.36), "BaseTrim", bevel=0.010, uv_scale=2.0))

    # 8 cols x 7 rows of hollow concrete breeze-blocks (3D shadow-casting lattice!)
    for row in range(7):
        bz = 0.56 + row * 0.45
        objs.append(C.make_box(f"BreezeRow_{row}", (0.0, -0.02, bz), (3.24, 0.20, 0.065), "BaseTrim", bevel=0.004))
    for col in range(9):
        bx = -1.56 + col * 0.39
        objs.append(C.make_box(f"BreezeCol_{col}", (bx, -0.02, 2.02), (0.065, 0.20, 3.30), "BaseTrim", bevel=0.004))
    # Inner diagonal diamond vents inside alternate rows
    for row in range(6):
        bz = 0.785 + row * 0.45
        for col in range(8):
            bx = -1.365 + col * 0.39
            objs.append(C.make_box(f"BreezeRing_{row}_{col}", (bx, -0.01, bz), (0.20, 0.16, 0.20), "ExteriorFacade",
                                   bevel=0.004, rot=(0, math.radians(45), 0)))
    # Interior stairwell recessed glow/dark backing plane
    objs.append(C.make_box("StairGlassBack", (0.0, 0.24, 2.00), (3.28, 0.04, 3.34), "WindowDarkR", bevel=0.0))
    return objs


def build_clinic_facade_bay_2002():
    """4.0 x 4.0 m 1998-2002 Vietnamese hospital Outpatient / Admin addition bay (K-B / K-E).
    Cleaner pale-cream stucco, blue-green dado plinth, double steel/aluminum casement window,
    continuous concrete cornice, rain downspout/conduit, and period wall-mounted AC condenser box."""
    objs = []
    objs.append(C.make_box("ClinicLowWall", (0.0, 0.14, 0.52), (4.0, 0.28, 1.04), "CampusStuccoR", bevel=0.01, uv_scale=2.2))
    objs.append(C.make_box("ClinicHighWall", (0.0, 0.14, 3.38), (4.0, 0.28, 1.24), "CampusStuccoR", bevel=0.01, uv_scale=2.2))
    for sx in (-1.66, 1.66):
        objs.append(C.make_box(f"ClinicPier_{sx:.1f}", (sx, 0.14, 1.92), (0.68, 0.28, 1.76), "CampusStuccoR", bevel=0.01, uv_scale=2.2))

    # Crisp 2000s accent band + floor cornice + corner pilasters
    objs.append(C.make_box("ClinicAccentBand", (0.0, -0.02, 0.96), (4.02, 0.08, 0.14), "DadoR", bevel=0.005))
    objs.append(C.make_box("ClinicCornice", (0.0, -0.14, 3.84), (4.04, 0.56, 0.30), "BaseTrim", bevel=0.012, uv_scale=2.0))
    for px in (-1.94, 1.94):
        objs.append(C.make_box(f"ClinicCol_{px:.1f}", (px, 0.02, 2.00), (0.22, 0.32, 4.00), "CampusStuccoR", bevel=0.010, uv_scale=2.0))

    # Wide 2002 casement window (2.64m x 1.72m)
    objs.append(C.make_box("CWinSill", (0.0, -0.04, 1.04), (2.78, 0.36, 0.08), "BaseTrim", bevel=0.006))
    objs.append(C.make_box("CWinHood", (0.0, -0.22, 2.84), (2.86, 0.52, 0.09), "BaseTrim", bevel=0.008))
    objs.append(C.make_box("CWinTop", (0.0, 0.07, 2.75), (2.64, 0.08, 0.045), "MetalOld", bevel=0.003))
    objs.append(C.make_box("CWinBot", (0.0, 0.07, 1.10), (2.64, 0.08, 0.045), "MetalOld", bevel=0.003))
    objs.append(C.make_box("CWinTransom", (0.0, 0.07, 2.34), (2.62, 0.08, 0.045), "MetalOld", bevel=0.003))
    for mx in (-1.30, -0.44, 0.44, 1.30):
        objs.append(C.make_box(f"CWinStile_{mx:.2f}", (mx, 0.07, 1.92), (0.045, 0.08, 1.68), "MetalOld", bevel=0.003))
    objs.append(C.make_box("CWinGlass", (0.0, 0.14, 1.92), (2.58, 0.024, 1.62), "WindowDarkR", bevel=0.0))

    # 2002 wall-mounted split-AC outdoor condenser unit on steel L-brackets (period renovation tell!)
    objs.append(C.make_box("AC_Box", (1.42, -0.18, 0.56), (0.76, 0.28, 0.52), "MetalOld", bevel=0.008))
    objs.append(C.make_cylinder("AC_FanGrille", (1.28, -0.32, 0.56), (1.28, -0.33, 0.56), 0.19, "Black", segments=14))
    for bx in (1.16, 1.68):
        objs.append(C.make_box(f"AC_Bracket_{bx:.2f}", (bx, -0.16, 0.28), (0.04, 0.34, 0.04), "Frame", bevel=0.003))
    return objs


def build_ward_balcony_bay_2004():
    """4.0 x 4.0 m 2001-2004 K-C Inpatient Ward Block cantilevered balcony bay.
    Includes 1.35m deep concrete balcony slab, solid low parapet + tubular iron handrail,
    vertical privacy sun-fin, recessed ward double-door + window, and soffit fluorescent fixture."""
    objs = []
    # Balcony floor slab projecting -1.35m along -Y
    objs.append(C.make_box("BalconySlab", (0.0, -0.58, 0.10), (4.02, 1.48, 0.20), "BaseTrim", bevel=0.012, uv_scale=1.8))
    objs.append(C.make_box("BalconySoffit", (0.0, -0.58, 3.88), (4.02, 1.48, 0.24), "BaseTrim", bevel=0.012, uv_scale=1.8))
    # Balcony front railing: 0.42m masonry dwarf wall + steel posts + rails to 1.08m
    objs.append(C.make_box("BalconyDwarfWall", (0.0, -1.24, 0.40), (4.00, 0.14, 0.44), "CampusStuccoR", bevel=0.008, uv_scale=1.8))
    objs.append(C.make_box("BalconyCap", (0.0, -1.24, 0.64), (4.02, 0.18, 0.06), "BaseTrim", bevel=0.006))
    objs.append(C.make_box("BalconyTopRail", (0.0, -1.24, 1.08), (4.00, 0.05, 0.045), "MetalOld", bevel=0.004))
    objs.append(C.make_box("BalconyMidRail", (0.0, -1.24, 0.86), (4.00, 0.035, 0.03), "MetalOld", bevel=0.003))
    for i in range(11):
        px = -1.85 + i * 0.37
        objs.append(C.make_cylinder(f"BalPost_{i}", (px, -1.24, 0.66), (px, -1.24, 1.08), 0.012, "MetalOld", segments=8))
    # Structural balcony column & vertical sun-shading fin on left edge
    objs.append(C.make_box("BalCol", (-1.90, -1.20, 2.00), (0.24, 0.26, 3.88), "CampusStuccoR", bevel=0.010, uv_scale=1.8))
    objs.append(C.make_box("BalPrivacyFin", (-1.90, -0.58, 2.00), (0.12, 1.24, 3.76), "BaseTrim", bevel=0.008, uv_scale=1.8))

    # Recessed inner ward facade wall at Y=+0.14
    objs.append(C.make_box("InnerSpandrel", (0.62, 0.14, 0.56), (2.76, 0.24, 1.12), "CampusStuccoR", bevel=0.008, uv_scale=2.0))
    objs.append(C.make_box("InnerHeader", (0.0, 0.14, 3.24), (4.00, 0.24, 1.16), "CampusStuccoR", bevel=0.008, uv_scale=2.0))
    objs.append(C.make_box("InnerPierL", (-1.82, 0.14, 1.50), (0.36, 0.24, 2.76), "CampusStuccoR", bevel=0.008))
    objs.append(C.make_box("InnerPierM", (-0.54, 0.14, 1.50), (0.32, 0.24, 2.76), "CampusStuccoR", bevel=0.008))
    objs.append(C.make_box("InnerPierR", (1.82, 0.14, 1.50), (0.36, 0.24, 2.76), "CampusStuccoR", bevel=0.008))
    # Balcony ward door (left) & wide ward window (right)
    objs.append(C.make_box("BalDoorFrame", (-1.18, 0.10, 1.38), (1.02, 0.08, 2.48), "Frame", bevel=0.004))
    objs.append(C.make_box("BalDoorGlass", (-1.18, 0.14, 1.72), (0.86, 0.024, 1.32), "WindowDarkR", bevel=0.0))
    objs.append(C.make_box("BalDoorPanel", (-1.18, 0.12, 0.62), (0.88, 0.045, 0.92), "WoodDoor", bevel=0.004))
    objs.append(C.make_box("BalWinFrame", (0.64, 0.10, 1.92), (2.08, 0.08, 1.64), "Frame", bevel=0.004))
    objs.append(C.make_box("BalWinGlass", (0.64, 0.14, 1.92), (1.96, 0.024, 1.52), "WindowDarkR", bevel=0.0))
    # Soffit fluorescent tube under balcony ceiling (swaps DeadTube <-> LiveTube!)
    objs.append(C.make_box("BalTubeHousing", (0.0, -0.62, 3.74), (1.24, 0.12, 0.06), "MetalOld", bevel=0.004))
    objs.append(C.make_cylinder("BalTubeBulb", (-0.56, -0.62, 3.69), (0.56, -0.62, 3.69), 0.018, "DeadTube", segments=8))
    return objs


def build_corrugated_roof_module():
    """8.0 x 8.0 m pitched Vietnamese corrugated metal hospital roof module (eave Z=0, ridge Z=1.65m along X)."""
    objs = []
    span_x = 8.0
    half_y = 4.25
    ridge_z = 1.68
    slope_len = math.hypot(half_y, ridge_z)
    angle = math.atan2(ridge_z, half_y)

    # Two pitched corrugated roof planes with crisp flat-shaded ridges & galvanized gutter eaves
    for side in (-1, 1):
        cy = side * (half_y * 0.5)
        cz = ridge_z * 0.5 + 0.06
        slope = C.make_box(f"RoofSlope_{side}", (0.0, cy, cz), (span_x, slope_len, 0.065),
                           "CorrugatedRoofR", bevel=0.0, rot=(-side * angle, 0, 0), uv_scale=2.2)
        slope.data.polygons.foreach_set("use_smooth", [False] * len(slope.data.polygons))
        objs.append(slope)
        # Structural steel purlins visible under overhanging eaves
        for k in range(3):
            frac = 0.25 + k * 0.35
            py = side * half_y * frac
            pz = ridge_z * (1.0 - frac) - 0.04
            objs.append(C.make_box(f"Purlin_{side}_{k}", (0.0, py, pz), (span_x - 0.1, 0.07, 0.10), "Frame", bevel=0.003))
        # Rain gutter & fascia board along eave
        objs.append(C.make_box(f"Gutter_{side}", (0.0, side * (half_y + 0.05), 0.02), (span_x + 0.04, 0.14, 0.12), "MetalOld", bevel=0.005))

    # Ridge cap flashing along X
    objs.append(C.make_box("RidgeCap", (0.0, 0.0, ridge_z + 0.11), (span_x + 0.06, 0.34, 0.08), "MetalOld", bevel=0.008))
    # End gable truss infill beams (open steel trusses)
    for ex in (-span_x * 0.48, 0.0, span_x * 0.48):
        objs.append(C.make_box(f"TieBeam_{ex:.1f}", (ex, 0.0, 0.06), (0.09, half_y * 1.9, 0.11), "Frame", bevel=0.004))
        objs.append(C.make_box(f"KingPost_{ex:.1f}", (ex, 0.0, ridge_z * 0.5), (0.08, 0.08, ridge_z - 0.10), "Frame", bevel=0.004))
    return objs


def build_covered_walkway_bay():
    """4.0m long (X) x 4.0m wide (Y) x 3.2m tall Vietnamese hospital covered connecting walkway (mái nối hành lang).
    Open-sided concrete columns + brick plinths, painted steel roof trusses, pitched corrugated iron roof,
    low terrazzo safety curbs, and overhead fluorescent T8 tube (DeadTube/LiveTube)."""
    objs = []
    # Terrazzo walkway deck slab + side drainage curbs
    objs.append(C.make_box("WalkDeck", (0.0, 0.0, 0.06), (4.02, 3.80, 0.12), "ConcretePavingR", bevel=0.008, uv_scale=2.0))
    for sy in (-1.82, 1.82):
        objs.append(C.make_box(f"WalkCurb_{sy:.1f}", (0.0, sy, 0.24), (4.00, 0.18, 0.36), "WeatheredBrick", bevel=0.008, uv_scale=1.2))
        objs.append(C.make_box(f"WalkCurbCap_{sy:.1f}", (0.0, sy, 0.44), (4.02, 0.22, 0.06), "BaseTrim", bevel=0.006))
        # Steel tubular handrail between columns
        objs.append(C.make_cylinder(f"WalkRail_{sy:.1f}", (-1.85, sy, 0.96), (1.85, sy, 0.96), 0.022, "MetalOld", segments=10))
        # Structural columns at X = -1.85, +1.85
        for sx in (-1.85, 1.85):
            objs.append(C.make_box(f"ColPlinth_{sx:.1f}_{sy:.1f}", (sx, sy, 0.42), (0.34, 0.34, 0.84), "WeatheredBrick", bevel=0.008))
            objs.append(C.make_box(f"ColShaft_{sx:.1f}_{sy:.1f}", (sx, sy, 1.82), (0.24, 0.24, 2.04), "ExteriorFacade", bevel=0.010, uv_scale=1.6))

    # Overhead steel longitudinal beams & triangular truss rafters
    for sy in (-1.82, 1.82):
        objs.append(C.make_box(f"EaveBeam_{sy:.1f}", (0.0, sy, 2.86), (4.02, 0.14, 0.16), "Frame", bevel=0.004))
    for tx in (-1.85, 0.0, 1.85):
        objs.append(C.make_box(f"TrussTie_{tx:.1f}", (tx, 0.0, 2.86), (0.07, 3.74, 0.08), "Frame", bevel=0.003))
        objs.append(C.make_box(f"TrussPost_{tx:.1f}", (tx, 0.0, 3.24), (0.06, 0.06, 0.72), "Frame", bevel=0.003))
        for sy in (-1, 1):
            objs.append(C.make_cylinder(f"TrussWeb_{tx:.1f}_{sy}", (tx, sy * 1.0, 2.88), (tx, 0.0, 3.55), 0.022, "Frame", segments=6))

    # Pitched corrugated metal roof overhanging eaves to +/- 2.35m (flat shaded for crisp steel sheen)
    ang = math.atan2(0.76, 2.35)
    slen = math.hypot(2.35, 0.76)
    for sy in (-1, 1):
        wr = C.make_box(f"WalkRoof_{sy}", (0.0, sy * 1.18, 3.26), (4.06, slen, 0.055),
                        "CorrugatedRoofR", bevel=0.0, rot=(-sy * ang, 0, 0), uv_scale=2.0)
        wr.data.polygons.foreach_set("use_smooth", [False] * len(wr.data.polygons))
        objs.append(wr)
    objs.append(C.make_box("WalkRidge", (0.0, 0.0, 3.66), (4.08, 0.24, 0.07), "MetalOld", bevel=0.005))

    # Overhead conduit & hospital T8 tube fixture hanging under center truss
    objs.append(C.make_box("WalkTubeHousing", (0.0, 0.0, 2.78), (1.28, 0.13, 0.06), "MetalOld", bevel=0.004))
    objs.append(C.make_cylinder("WalkTubeBulb", (-0.58, 0.0, 2.73), (0.58, 0.0, 2.73), 0.018, "DeadTube", segments=8))
    return objs


def build_ambulance_canopy():
    """Iconic Vietnamese Hospital Emergency Ambulance Porte-Cochère (K-D Mái đón xe cấp cứu).
    Width (X) = 13.0m (runs along K-D East face when placed at yaw=-90), Depth (Y) = 6.5m projecting toward -Y,
    Clear height = 4.15m so ambulances drive straight underneath! Includes trusses, columns, hazard bollards,
    rooftop sign frame for illuminated red 'CẤP CỨU', and recessed ceiling floodlight housings."""
    objs = []
    # Main cantilevered concrete + steel canopy deck (center Y = -3.25m, spanning X = -6.5..+6.5, Z = 4.15..4.75)
    objs.append(C.make_box("CanopyDeck", (0.0, -3.25, 4.35), (13.2, 6.6, 0.34), "BaseTrim", bevel=0.016, uv_scale=2.2))
    # Bold crimson emergency fascia wrapping 3 exposed edges!
    objs.append(C.make_box("FasciaFront", (0.0, -6.58, 4.48), (13.34, 0.14, 0.62), "RedPaint", bevel=0.010))
    objs.append(C.make_box("FasciaStripe", (0.0, -6.66, 4.32), (13.36, 0.04, 0.10), "AmbulanceWhite", bevel=0.003))
    for sx in (-6.62, 6.62):
        objs.append(C.make_box(f"FasciaSide_{sx:.1f}", (sx, -3.25, 4.48), (0.14, 6.72, 0.62), "RedPaint", bevel=0.010))

    # 4 heavy reinforced-concrete octagonal/chamfered support columns on front edge (Y = -5.85) with yellow/black crash bases
    for cx in (-5.6, -1.9, 1.9, 5.6):
        objs.append(C.make_box(f"AmbColBase_{cx:.1f}", (cx, -5.85, 0.45), (0.56, 0.56, 0.90), "HazardYellow", bevel=0.014))
        objs.append(C.make_box(f"AmbColStripe_{cx:.1f}", (cx, -5.85, 0.45), (0.58, 0.58, 0.24), "Black", bevel=0.005))
        objs.append(C.make_box(f"AmbColShaft_{cx:.1f}", (cx, -5.85, 2.52), (0.42, 0.42, 3.30), "CampusStuccoR", bevel=0.016, uv_scale=1.8))
        objs.append(C.make_box(f"AmbColCap_{cx:.1f}", (cx, -5.85, 4.10), (0.56, 0.56, 0.18), "BaseTrim", bevel=0.010))
        # Steel roof beam connecting column back to K-D main wall
        objs.append(C.make_box(f"AmbBeam_{cx:.1f}", (cx, -3.00, 4.04), (0.24, 6.00, 0.28), "Frame", bevel=0.008))

    # Raised patient stretcher unloading apron curb + sloped concrete ramp on building side (Y = -1.6..0)
    objs.append(C.make_box("UnloadApron", (0.0, -0.85, 0.08), (12.4, 1.70, 0.16), "ConcretePavingR", bevel=0.010, uv_scale=1.8))
    objs.append(C.make_box("UnloadCurb", (0.0, -1.72, 0.08), (12.4, 0.12, 0.16), "HazardYellow", bevel=0.006))

    # Steel crash bollards flanking entrance & apron
    for bx in (-4.8, -2.6, 2.6, 4.8):
        objs.append(C.make_cylinder(f"Bollard_{bx:.1f}", (bx, -1.85, 0.0), (bx, -1.85, 0.85), 0.065, "HazardYellow", segments=12))
        objs.append(C.make_cylinder(f"BollardBand_{bx:.1f}", (bx, -1.85, 0.52), (bx, -1.85, 0.68), 0.068, "RedPaint", segments=12))

    # Rooftop backlit emergency sign box structure atop front edge (Z = 4.80..6.20 — visible across entire campus!)
    objs.append(C.make_box("RoofSignBox", (0.0, -6.30, 5.52), (5.60, 0.38, 1.48), "MetalOld", bevel=0.012))
    for sx in (-2.4, 0.0, 2.4):
        objs.append(C.make_cylinder(f"SignStrut_{sx:.1f}", (sx, -6.08, 4.65), (sx, -5.20, 4.65), 0.032, "Frame", segments=8))
        objs.append(C.make_cylinder(f"SignBrace_{sx:.1f}", (sx, -6.08, 6.05), (sx, -5.20, 4.65), 0.028, "Frame", segments=8))

    # 6 recessed ceiling fluorescent bay light housings under canopy soffit
    for lx in (-4.2, 0.0, 4.2):
        for ly in (-2.4, -4.6):
            objs.append(C.make_box(f"CanopyLamp_{lx:.1f}_{ly:.1f}", (lx, ly, 4.15), (1.30, 0.36, 0.08), "MetalOld", bevel=0.005))
            objs.append(C.make_box(f"CanopyTube_{lx:.1f}_{ly:.1f}", (lx, ly, 4.10), (1.18, 0.24, 0.03), "DeadTube", bevel=0.002))
    return objs


def build_main_hospital_arch_gate():
    """8.8m wide x 5.2m tall monumental Vietnamese public hospital Main Public Gate (Cổng chính at -30, -30).
    Twin stepped masonry piers, overhead concrete archway header for 'BỆNH VIỆN ĐA KHOA AN SINH',
    side pedestrian wicket portal, and heavy wrought-iron hospital double gates (padlocked/chained in R)."""
    objs = []
    # Main twin monumental vehicle gate piers at X = -3.15, +3.15
    for sx in (-3.15, 3.15):
        objs.append(C.make_box(f"MainPierBase_{sx:.1f}", (sx, 0.0, 0.55), (0.86, 0.86, 1.10), "WeatheredBrick", bevel=0.014, uv_scale=1.2))
        objs.append(C.make_box(f"MainPierBody_{sx:.1f}", (sx, 0.0, 2.55), (0.72, 0.72, 2.95), "ExteriorFacade", bevel=0.016, uv_scale=1.6))
        objs.append(C.make_box(f"MainPierCap_{sx:.1f}", (sx, 0.0, 4.12), (0.92, 0.92, 0.22), "BaseTrim", bevel=0.012))
        # Pier globe lamp post on outer shoulder
        objs.append(C.make_cylinder(f"PierLampPost_{sx:.1f}", (sx, -0.32, 4.22), (sx, -0.32, 4.65), 0.032, "MetalOld", segments=10))
        objs.append(C.make_sphere(f"PierGlobe_{sx:.1f}", (sx, -0.32, 4.78), (0.28, 0.28, 0.30), "WarmGlow", segs=12, rings=8))

    # Flanking outer pedestrian wicket pier at X = -4.85, +4.85
    for sx in (-4.85, 4.85):
        objs.append(C.make_box(f"SidePier_{sx:.1f}", (sx, 0.0, 1.45), (0.50, 0.50, 2.90), "ExteriorFacade", bevel=0.012, uv_scale=1.6))
        objs.append(C.make_box(f"SidePierCap_{sx:.1f}", (sx, 0.0, 2.96), (0.60, 0.60, 0.14), "BaseTrim", bevel=0.010))
        # Wicket header bar
        objs.append(C.make_box(f"WicketHeader_{sx:.1f}", (sx * 0.82, 0.0, 2.68), (1.40, 0.24, 0.24), "BaseTrim", bevel=0.008))

    # Spanning reinforced-concrete main entrance arch fascia beam (Z = 4.10..5.25)
    objs.append(C.make_box("ArchBeam", (0.0, 0.0, 4.65), (7.60, 0.56, 1.12), "ExteriorFacade", bevel=0.018, uv_scale=2.0))
    objs.append(C.make_box("ArchRoofCap", (0.0, 0.0, 5.28), (8.00, 0.84, 0.18), "BaseTrim", bevel=0.014))
    objs.append(C.make_box("ArchCrossEmblem", (0.0, -0.31, 5.62), (0.62, 0.12, 0.62), "AmbulanceWhite", bevel=0.008))
    objs.append(C.make_box("ArchCrossV", (0.0, -0.36, 5.62), (0.14, 0.06, 0.44), "RedPaint", bevel=0.004))
    objs.append(C.make_box("ArchCrossH", (0.0, -0.36, 5.62), (0.44, 0.06, 0.14), "RedPaint", bevel=0.004))

    # Closed wrought-iron main vehicular double gates (spanning X = -2.75..+2.75, H = 2.35m)
    for side in (-1, 1):
        gx0 = side * 0.06
        gx1 = side * 2.74
        mid = (gx0 + gx1) * 0.5
        w = abs(gx1 - gx0)
        objs.append(C.make_box(f"GateFrameTop_{side}", (mid, 0.0, 2.24), (w, 0.06, 0.06), "MetalOld", bevel=0.004))
        objs.append(C.make_box(f"GateFrameMid_{side}", (mid, 0.0, 1.10), (w, 0.06, 0.06), "MetalOld", bevel=0.004))
        objs.append(C.make_box(f"GateFrameBot_{side}", (mid, 0.0, 0.16), (w, 0.06, 0.06), "MetalOld", bevel=0.004))
        objs.append(C.make_box(f"GateKick_{side}", (mid, 0.0, 0.42), (w - 0.06, 0.03, 0.50), "Frame", bevel=0.003))
        for i in range(13):
            px = min(gx0, gx1) + 0.12 + i * ((w - 0.24) / 12.0)
            objs.append(C.make_cylinder(f"GatePicket_{side}_{i}", (px, 0.0, 0.66), (px, 0.0, 2.42), 0.012, "MetalOld", segments=6))
    return objs


def build_water_tower_and_roof_kit():
    """16.5m tall quintessential Vietnamese district hospital concrete water tower (đài nước bệnh viện)
    for K-F Logistics / North horizon landmark! Hexagonal concrete support legs, ring beams, tank drum,
    conical roof, steel access ladder & feed pipes."""
    objs = []
    # Concrete base plinth
    objs.append(C.make_box("WT_Base", (0.0, 0.0, 0.30), (4.8, 4.8, 0.60), "BaseTrim", bevel=0.02, uv_scale=2.0))
    # 6 tapered reinforced concrete tower legs rising to Z = 11.2m
    leg_r_bot = 2.05
    leg_r_top = 1.65
    for i in range(6):
        a = i * math.tau / 6.0
        p0 = (math.cos(a) * leg_r_bot, math.sin(a) * leg_r_bot, 0.5)
        p1 = (math.cos(a) * leg_r_top, math.sin(a) * leg_r_top, 11.3)
        objs.append(C.make_cylinder(f"WT_Leg_{i}", p0, p1, 0.21, "BaseTrim", segments=8, r_end=0.18))
    # 3 horizontal ring tie-beams at Z = 4.0, 7.6, 11.0m
    for rz, rr in ((4.0, 1.92), (7.6, 1.78), (11.0, 1.66)):
        for i in range(6):
            a0 = i * math.tau / 6.0
            a1 = (i + 1) * math.tau / 6.0
            p0 = (math.cos(a0) * rr, math.sin(a0) * rr, rz)
            p1 = (math.cos(a1) * rr, math.sin(a1) * rr, rz)
            objs.append(C.make_cylinder(f"WT_Tie_{rz:.0f}_{i}", p0, p1, 0.13, "BaseTrim", segments=6))

    # Central vertical steel riser/drain pipes
    objs.append(C.make_cylinder("WT_PipeMain", (0.22, 0.0, 0.5), (0.22, 0.0, 11.5), 0.09, "MetalOld", segments=10))
    objs.append(C.make_cylinder("WT_PipeOver", (-0.24, 0.12, 0.5), (-0.24, 0.12, 11.5), 0.06, "MetalOld", segments=8))

    # Elevated cylindrical concrete water tank (Z = 11.2..15.2m, radius 2.55m) + walkway balcony + conical roof
    objs.append(C.make_cylinder("WT_FloorCone", (0.0, 0.0, 11.0), (0.0, 0.0, 11.8), 1.70, "BaseTrim", segments=16, r_end=2.68))
    objs.append(C.make_cylinder("WT_TankDrum", (0.0, 0.0, 11.8), (0.0, 0.0, 15.1), 2.58, "ExteriorFacade", segments=18, uv_scale=2.4))
    objs.append(C.make_cylinder("WT_RoofCone", (0.0, 0.0, 15.1), (0.0, 0.0, 16.25), 2.74, "BaseTrim", segments=18, r_end=0.22))
    # Lightning rod & aviation red warning beacon atop spire!
    objs.append(C.make_cylinder("WT_Spire", (0.0, 0.0, 16.2), (0.0, 0.0, 17.5), 0.025, "Steel", segments=6))
    objs.append(C.make_sphere("WT_Beacon", (0.0, 0.0, 16.45), (0.18, 0.18, 0.22), "CrimsonGlow", segs=10, rings=6))
    return objs


def build_rooftop_tank_cluster():
    """Authentic 1995-2005 Vietnamese hospital rooftop cluster: twin horizontal stainless-steel water tanks
    (bồn nước inox Sơn Hà style) on angle-iron stands + concrete stair bulkhead + vent stacks + lightning mast."""
    objs = []
    # Concrete stair bulkhead / machine penthouse (3.6 x 4.2 x 2.65m)
    objs.append(C.make_box("RoofPenthouse", (-2.2, 0.0, 1.32), (3.4, 4.0, 2.64), "ExteriorFacade", bevel=0.015, uv_scale=2.2))
    objs.append(C.make_box("RoofPentSlab", (-2.2, 0.0, 2.70), (3.85, 4.45, 0.18), "BaseTrim", bevel=0.012))
    objs.append(C.make_box("RoofPentDoor", (-2.2, -2.01, 1.05), (1.05, 0.08, 2.05), "MetalOld", bevel=0.006))

    # Twin horizontal cylindrical stainless-steel rooftop water tanks on steel legs
    for tx in (1.4, 3.1):
        objs.append(C.make_cylinder(f"InoxTank_{tx:.1f}", (tx, -1.15, 1.48), (tx, 1.15, 1.48), 0.58, "Steel", segments=16, bevel=0.02))
        for ly in (-0.85, 0.0, 0.85):
            for lx in (-0.42, 0.42):
                objs.append(C.make_cylinder(f"TankLeg_{tx:.1f}_{lx:.1f}_{ly:.1f}", (tx + lx, ly, 0.0), (tx + lx, ly, 1.05), 0.032, "MetalOld", segments=6))
        objs.append(C.make_box(f"TankCradle_{tx:.1f}", (tx, 0.0, 0.95), (0.94, 1.85, 0.06), "MetalOld", bevel=0.004))
    # Galvanized roof exhaust ventilator pipes + lightning mast
    objs.append(C.make_cylinder("LightningMast", (-2.2, 0.0, 2.75), (-2.2, 0.0, 5.40), 0.024, "Steel", segments=6))
    for vx in (-0.2, 0.35):
        objs.append(C.make_cylinder(f"VentStack_{vx:.1f}", (vx, 1.2, 0.0), (vx, 1.2, 1.35), 0.08, "MetalOld", segments=10))
        objs.append(C.make_cylinder(f"VentCap_{vx:.1f}", (vx, 1.2, 1.38), (vx, 1.2, 1.50), 0.14, "MetalOld", segments=10, r_end=0.02))
    return objs


def build_utility_pole_and_lamp():
    """8.5m Vietnamese centrifugal concrete street utility pole (cột điện bê tông ly tâm)
    with twin steel crossarms, ceramic insulators, junction box, and curved cobra-head sodium streetlamp."""
    objs = []
    # Tapered concrete pole (Z = 0..8.5m)
    objs.append(C.make_cylinder("PoleShaft", (0.0, 0.0, 0.0), (0.0, 0.0, 8.50), 0.155, "BaseTrim", segments=12, r_end=0.095))
    # Metal fuse/meter box clamped at Z = 2.6m
    objs.append(C.make_box("MeterBox", (0.0, -0.18, 2.55), (0.32, 0.18, 0.46), "MetalOld", bevel=0.006))
    # Twin high-level steel crossarms + ceramic pin insulators
    for cz in (7.65, 8.15):
        objs.append(C.make_box(f"CrossArm_{cz:.2f}", (0.0, 0.0, cz), (1.75, 0.08, 0.08), "Frame", bevel=0.004))
        for ix in (-0.75, -0.35, 0.35, 0.75):
            objs.append(C.make_cylinder(f"Insul_{cz:.2f}_{ix:.2f}", (ix, 0.0, cz + 0.04), (ix, 0.0, cz + 0.16), 0.036, "Ceramic", segments=8))
    # Curved tubular gooseneck lamp arm projecting along -Y (toward road/courtyard!)
    pts = [(0.0, -0.10, 6.45), (0.0, -0.65, 6.85), (0.0, -1.45, 7.12), (0.0, -1.95, 7.14)]
    objs.append(C.make_pipe("LampArm", pts, 0.028, "MetalOld", bevel_res=3))
    objs.append(C.make_box("CobraHood", (0.0, -2.18, 7.16), (0.28, 0.56, 0.14), "MetalOld", bevel=0.015))
    objs.append(C.make_box("CobraLens", (0.0, -2.18, 7.07), (0.22, 0.44, 0.05), "WarmGlow", bevel=0.006))
    return objs


def build_parking_shed_bay():
    """6.0m x 4.6m corrugated-iron Vietnamese hospital motorbike parking shelter (nhà xe bệnh nhân)."""
    objs = []
    # Concrete wheel stops & slab
    objs.append(C.make_box("ParkSlab", (0.0, 0.0, 0.05), (6.0, 4.6, 0.10), "ConcretePavingR", bevel=0.006, uv_scale=2.0))
    # 6 tubular steel posts (front Z=2.65m, back Z=2.25m for mono-pitch rain runoff)
    for sx in (-2.75, 0.0, 2.75):
        objs.append(C.make_cylinder(f"PostF_{sx:.1f}", (sx, -1.95, 0.08), (sx, -1.95, 2.68), 0.048, "Frame", segments=10))
        objs.append(C.make_cylinder(f"PostB_{sx:.1f}", (sx, 1.95, 0.08), (sx, 1.95, 2.26), 0.048, "Frame", segments=10))
        objs.append(C.make_cylinder(f"Rafter_{sx:.1f}", (sx, -2.25, 2.72), (sx, 2.25, 2.24), 0.036, "Frame", segments=8))
    # Longitudinal purlins
    for py, pz in ((-1.8, 2.68), (0.0, 2.48), (1.8, 2.28)):
        objs.append(C.make_box(f"ShedPurlin_{py:.1f}", (0.0, py, pz), (6.10, 0.06, 0.07), "Frame", bevel=0.003))
    # Sloped corrugated metal roof sheet (flat shaded for crisp steel sheen)
    ang = math.atan2(0.46, 4.6)
    sr = C.make_box("ShedRoof", (0.0, 0.0, 2.52), (6.24, 4.85, 0.05), "CorrugatedRoofR", bevel=0.0, rot=(ang, 0, 0), uv_scale=2.0)
    sr.data.polygons.foreach_set("use_smooth", [False] * len(sr.data.polygons))
    objs.append(sr)
    return objs


def build_vintage_ambulance():
    """Original 1990s-2000s boxy Vietnamese hospital ambulance van (5.05m long along Y, 1.92m wide along X, 2.28m tall).
    Fictional generic boxy van silhouette with red medical stripe, red cross panels, roof lightbar,
    windshield, side frosted patient-bay windows, rear double doors, bumpers, and 4 tires."""
    objs = []
    # Lower chassis & van body
    objs.append(C.make_box("VanLowerBody", (0.0, 0.0, 0.72), (1.88, 4.90, 0.86), "AmbulanceWhite", bevel=0.045, segments=2))
    objs.append(C.make_box("VanRedStripe", (0.0, 0.0, 0.84), (1.91, 4.92, 0.16), "RedPaint", bevel=0.008))
    # Upper cabin & tall ambulance roof shell
    objs.append(C.make_box("VanUpperBody", (0.0, 0.25, 1.56), (1.80, 4.25, 0.86), "AmbulanceWhite", bevel=0.055, segments=2))
    # Sloped front nose & windshield (sloping cleanly backward into the van roofline!)
    objs.append(C.make_box("VanNose", (0.0, -2.06, 1.46), (1.76, 0.54, 0.76), "AmbulanceWhite", bevel=0.04, rot=(math.radians(-22), 0, 0)))
    objs.append(C.make_box("Windshield", (0.0, -2.18, 1.54), (1.62, 0.06, 0.64), "Glass", bevel=0.004, rot=(math.radians(-22), 0, 0)))
    # Side cabin & frosted medical-bay windows
    for sx in (-0.91, 0.91):
        objs.append(C.make_box(f"CabWin_{sx:.1f}", (sx, -1.42, 1.50), (0.03, 0.78, 0.52), "Glass", bevel=0.002))
        objs.append(C.make_box(f"BayWin_{sx:.1f}", (sx, 0.45, 1.52), (0.03, 2.15, 0.52), "FrostedGlass", bevel=0.002))
        # Red Cross emblem on side panels
        objs.append(C.make_box(f"CrossV_{sx:.1f}", (sx * 1.04, 0.45, 1.52), (0.02, 0.10, 0.34), "RedPaint", bevel=0.002))
        objs.append(C.make_box(f"CrossH_{sx:.1f}", (sx * 1.04, 0.45, 1.52), (0.02, 0.34, 0.10), "RedPaint", bevel=0.002))
    # Rear double door seam + glass
    objs.append(C.make_box("RearWin", (0.0, 2.40, 1.52), (1.42, 0.04, 0.54), "FrostedGlass", bevel=0.002))
    objs.append(C.make_box("RearSeam", (0.0, 2.44, 1.15), (0.03, 0.04, 1.65), "Rubber", bevel=0.002))

    # Front grille, bumpers, headlamps & rooftop red/white emergency siren lightbar!
    objs.append(C.make_box("FrontBumper", (0.0, -2.50, 0.38), (1.92, 0.16, 0.22), "Rubber", bevel=0.015))
    objs.append(C.make_box("RearBumper", (0.0, 2.50, 0.38), (1.92, 0.16, 0.22), "Rubber", bevel=0.015))
    objs.append(C.make_box("FrontGrille", (0.0, -2.47, 0.72), (1.12, 0.06, 0.24), "Black", bevel=0.005))
    for lx in (-0.70, 0.70):
        objs.append(C.make_box(f"Headlamp_{lx:.1f}", (lx, -2.47, 0.74), (0.28, 0.06, 0.16), "WarmGlow", bevel=0.004))
        objs.append(C.make_box(f"Taillamp_{lx:.1f}", (lx, 2.47, 0.68), (0.14, 0.05, 0.36), "RedPaint", bevel=0.004))
    # Roof emergency siren lightbar
    objs.append(C.make_box("SirenBarBase", (0.0, -1.15, 2.04), (1.28, 0.26, 0.07), "MetalOld", bevel=0.006))
    objs.append(C.make_box("SirenLensL", (-0.42, -1.15, 2.14), (0.42, 0.22, 0.14), "CrimsonGlow", bevel=0.010))
    objs.append(C.make_box("SirenLensR", (0.42, -1.15, 2.14), (0.42, 0.22, 0.14), "CrimsonGlow", bevel=0.010))
    objs.append(C.make_box("SirenSpeaker", (0.0, -1.15, 2.14), (0.34, 0.22, 0.14), "Steel", bevel=0.006))

    # 4 Wheels (tires + steel rims)
    for wx in (-0.88, 0.88):
        for wy in (-1.52, 1.48):
            objs.append(C.make_cylinder(f"Tire_{wx:.1f}_{wy:.1f}", (wx - 0.11, wy, 0.35), (wx + 0.11, wy, 0.35), 0.35, "Rubber", segments=16))
            objs.append(C.make_cylinder(f"Rim_{wx:.1f}_{wy:.1f}", (wx - 0.12, wy, 0.35), (wx + 0.12, wy, 0.35), 0.20, "Steel", segments=12))
    return objs


def build_b1_sealed_door_e05b():
    """2.4 x 2.6 m high-detail architectural evidence assembly for Core 03 §3.5 / W-Q03:
    The 2004 bricked-in rear exit door of Khu C (LOC12 East face at x=34.5, z=42, y=-3.4).
    Shows the original 1987 concrete lintel, cut timber/steel jamb scars, interrupted terrazzo base trim,
    and recessed rushed 2004 red-brick + coarse mortar infill proving the exit was deliberately sealed."""
    objs = []
    # Original 1987 concrete door header lintel & jamb plaster reveals flanking the 1.48m old doorway
    objs.append(C.make_box("OrigLintel_1987", (0.0, 0.0, 2.34), (2.10, 0.34, 0.26), "BaseTrim", bevel=0.012, uv_scale=1.6))
    for sx in (-0.86, 0.86):
        objs.append(C.make_box(f"OrigJambPlaster_{sx:.2f}", (sx, 0.0, 1.12), (0.22, 0.30, 2.24), "PlasterR", bevel=0.010, uv_scale=1.8))
        # Sawed-off hinge anchor stubs left in the 1987 jamb!
        objs.append(C.make_box(f"OldHingeScar_{sx:.2f}_A", (sx * 0.86, -0.14, 0.45), (0.04, 0.05, 0.14), "MetalOld", bevel=0.003))
        objs.append(C.make_box(f"OldHingeScar_{sx:.2f}_B", (sx * 0.86, -0.14, 1.85), (0.04, 0.05, 0.14), "MetalOld", bevel=0.003))
        # Interrupted 1987 terrazzo skirting stopping abruptly at the old doorway!
        objs.append(C.make_box(f"CutSkirting_{sx:.2f}", (sx * 1.15, -0.14, 0.07), (0.44, 0.06, 0.14), "BaseTrim", bevel=0.005))

    # Recessed 2004 brick infill block (sits 5cm deeper than surrounding 1987 wall so seam casts sharp raking shadow!)
    objs.append(C.make_box("BrickInfillCore_2004", (0.0, 0.04, 1.11), (1.50, 0.24, 2.22), "BrickInfill2004", bevel=0.008, uv_scale=1.1))
    # Sloppy hand-troweled grey mortar collar squeezing around the top and jamb seams
    objs.append(C.make_box("MortarSeamTop", (0.0, -0.09, 2.20), (1.54, 0.05, 0.09), "BaseTrim", bevel=0.006))
    for sx in (-0.75, 0.75):
        objs.append(C.make_box(f"MortarSeamSide_{sx:.2f}", (sx, -0.09, 1.11), (0.07, 0.05, 2.20), "BaseTrim", bevel=0.006))
    # Old worn concrete door threshold sill still sticking out below the new brick!
    objs.append(C.make_box("OldThresholdSill", (0.0, -0.16, 0.04), (1.68, 0.34, 0.08), "BaseTrim", bevel=0.008))
    return objs


def run_all():
    assets = [
        ("CTCC_ENV_MainFacadeBay_1987", build_main_facade_bay_1987, True, (1.1, -2.0, 0.55)),
        ("CTCC_ENV_StairwellBreezeBay_1987", build_stairwell_breeze_bay_1987, True, (1.0, -2.0, 0.55)),
        ("CTCC_ENV_ClinicFacadeBay_2002", build_clinic_facade_bay_2002, True, (1.1, -2.0, 0.55)),
        ("CTCC_ENV_WardBalconyBay_2004", build_ward_balcony_bay_2004, True, (1.2, -1.9, 0.60)),
        ("CTCC_ENV_CorrugatedRoofModule", build_corrugated_roof_module, True, (1.4, -1.8, 0.95)),
        ("CTCC_ENV_CoveredWalkwayBay", build_covered_walkway_bay, True, (1.5, -1.7, 0.75)),
        ("CTCC_ENV_AmbulanceCanopy", build_ambulance_canopy, True, (1.5, -1.9, 0.65)),
        ("CTCC_ENV_MainHospitalArchGate", build_main_hospital_arch_gate, True, (1.2, -2.1, 0.55)),
        ("CTCC_ENV_WaterTowerAndRoofKit", build_water_tower_and_roof_kit, True, (1.4, -1.9, 0.70)),
        ("CTCC_ENV_RooftopTankCluster", build_rooftop_tank_cluster, True, (1.4, -1.8, 0.85)),
        ("CTCC_ENV_UtilityPoleAndLamp", build_utility_pole_and_lamp, True, (1.5, -1.8, 0.45)),
        ("CTCC_ENV_ParkingShedBay", build_parking_shed_bay, True, (1.4, -1.8, 0.70)),
        ("CTCC_PROP_VintageAmbulance", build_vintage_ambulance, True, (1.5, -1.7, 0.65)),
        ("CTCC_ENV_B1SealedDoorE05b", build_b1_sealed_door_e05b, True, (1.0, -1.9, 0.55)),
    ]
    total = 0
    for name, fn, lods, cam in assets:
        # render=False during batch build for speed; we verify inside live Unity URP 1920x1080!
        total += C.export_asset(name, fn, make_lods=lods, render=False, cam_dir=cam)
    print(f"[CAMPUS KIT COMPLETE] Exported {len(assets)} modular assets ({total} LOD0 tris total)")


if __name__ == "__main__":
    run_all()
