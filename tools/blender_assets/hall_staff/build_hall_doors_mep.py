"""Kit A (Part 2): Reusable Hospital Doors, Safety, Wayfinding & MEP Fixtures (Blender 4.5 LTS).

Produces Cycles-baked B (2005 operational) / R (2012 abandoned) PBR assets:
  1. CTCC_ENV_StaffCrossDoorFrame    - Molded 1987 ironwood passage casing wrapping x=[1.2..2.4], z=7.50
                                       cleanly on BOTH HALL & STAFF faces + stainless threshold + hinges.
  2. CTCC_ENV_StaffCrossDoor         - Upgraded 0.94 x 2.18m ironwood & frosted-glass staff door leaf
                                       w/ overhead closer, kickplates, curved anti-snag hospital levers.
  3. CTCC_ENV_EmergencyDoorFrame     - Heavy 1.40 x 2.20m institutional ER doorway frame + stainless
                                       jamb bumpers + ribbed stretcher threshold ramp (Door A & Door B!).
  4. CTCC_ENV_EmergencySwingDoorLeaf - Upgraded 0.692 x 2.18m sage-enamel & satin-inox ER swing door
                                       leaf w/ rounded gasketed porthole, tubular crash bars & rubber edge.
  5. CTCC_PROP_EmergencyLightBox     - Upgraded extruded anodized-aluminum hanging CẤP CỨU lightbox
                                       w/ ceiling flanges, suspension rods, electrical flex & louver vents.
  6. CTCC_PROP_PASpeaker             - Upgraded slanted ironwood/cream-enamel corridor PA intercom box
                                       w/ woven acoustic mesh, 9 louvers, volume knob & full riser conduit.
  7. CTCC_PROP_FireExtinguisher      - Upgraded Vietnamese PCCC MFZ4 fire extinguisher + printed label +
                                       yellowed 2005 inspection tag + pressure dial + fire alarm bell/box!
  8. CTCC_HALL_WayfindingAndMEP      - Reusable corridor fittings (separate parts):
                                       FloorMapBoard, BladeSign, HandSanitizer, BreakerPanel, WallVentGrille.
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
# 1. CTCC_ENV_StaffCrossDoorFrame (Wraps x=[1.18..2.42], z=[6.98..8.02], height 2.22m!)
#    Placed at (1.80, 0, 7.50), yaw=0: passage runs along X (-0.62 STAFF to +0.62 HALL),
#    clear opening width along Y in [-0.485..+0.485] (0.97m clear inside jamb liners).
# --------------------------------------------------------------------------------------
def build_staff_cross_door_frame():
    objs = []
    depth = 1.24   # wraps full 1.20m service spine pier + 2cm proud each side
    half_d = depth * 0.5
    open_w = 1.04
    h = 2.22
    jamb_t = 0.036

    # Deep interior ironwood reveal jamb linings (South Y=-0.502, North Y=+0.502) + Overhead soffit lining
    for sy in (-1, 1):
        jy = sy * (open_w * 0.5 - jamb_t * 0.5)
        objs.append(H.box(f"PassageJamb_{sy}", (0.0, jy, h * 0.5), (depth, jamb_t, h), "wood", bevel=0.004, grain="Z"))
        # Stainless lower jamb kick-guards inside passage
        objs.append(H.box(f"PassageGuard_{sy}", (0.0, jy - sy * (jamb_t * 0.5 + 0.003), 0.16),
                          (depth - 0.02, 0.006, 0.30), "satin_inox", bevel=0.0015))
    objs.append(H.box("PassageHeader", (0.0, 0.0, h - 0.018), (depth, open_w, 0.036), "wood", bevel=0.004, grain="Y"))

    # Door stop rabbet strip at HALL side (local X = +0.545 -> world x = 2.345, right behind hinge x=2.36!)
    for sy in (-1, 1):
        objs.append(H.box(f"StopStrip_{sy}", (0.528, sy * (open_w * 0.5 - jamb_t - 0.009), h * 0.5),
                          (0.026, 0.018, h - 0.03), "wood_light", bevel=0.002, grain="Z"))
    objs.append(H.box("StopTop", (0.528, 0.0, h - 0.044), (0.026, open_w - jamb_t * 2, 0.018), "wood_light", bevel=0.002, grain="Y"))

    # Molded stepped outer architrave casings on BOTH HALL (+X) and STAFF (-X) wall faces!
    for sx, tag in ((-1, "STAFF"), (1, "HALL")):
        ax = sx * (half_d - 0.004)
        for sy in (-1, 1):
            objs.append(H.box(f"ArchSide_{tag}_{sy}", (ax, sy * (open_w * 0.5 + 0.036), (h + 0.095) * 0.5),
                              (0.028, 0.092, h + 0.095), "wood_light", bevel=0.006, grain="Z"))
            objs.append(H.box(f"ArchPlinth_{tag}_{sy}", (ax + sx * 0.004, sy * (open_w * 0.5 + 0.036), 0.065),
                              (0.034, 0.098, 0.13), "terrazzo_dark", bevel=0.005))
        objs.append(H.box(f"ArchTop_{tag}", (ax, 0.0, h + 0.052), (0.032, open_w + 0.184, 0.098), "wood_light", bevel=0.006, grain="Y"))
        objs.append(H.box(f"ArchCrown_{tag}", (ax + sx * 0.008, 0.0, h + 0.106), (0.046, open_w + 0.215, 0.022), "wood", bevel=0.004, grain="Y"))

    # Ribbed brushed-stainless threshold plate across HALL doorway lip + recessed floor transition
    objs.append(H.box("StaffDoorThreshold", (0.555, 0.0, 0.007), (0.14, open_w - 0.04, 0.014), "satin_inox", bevel=0.005))
    for rx in (-0.036, -0.012, 0.012, 0.036):
        objs.append(H.box(f"ThreshRib_{rx:.3f}", (0.555 + rx, 0.0, 0.015), (0.007, open_w - 0.07, 0.003), "steel_dark", bevel=0.001))
    # Subtle brass threshold strip on STAFF entry side too
    objs.append(H.box("StaffEntryBrassStrip", (-0.585, 0.0, 0.004), (0.065, open_w - 0.04, 0.008), "brass", bevel=0.002))

    # 3 Brass hinge knuckles mounted on HALL South jamb (matching hinge at world 2.36, 7.03 -> local +0.56, -0.47)
    for k, hz in enumerate((0.28, 1.08, 1.88)):
        objs.append(H.cyl(f"StaffFrameHinge_{k}", (0.565, -0.472, hz - 0.055), (0.565, -0.472, hz + 0.055), 0.011, "brass", seg=12))

    return objs


# --------------------------------------------------------------------------------------
# 2. CTCC_ENV_StaffCrossDoor (Upgraded hero 0.94 x 2.18m hardwood & frosted-glass leaf)
#    Pivot at hinge edge X=0, Y=0, Z=0; extends along +X to 0.94m!
# --------------------------------------------------------------------------------------
def build_staff_cross_door():
    opaque = []
    glass = []
    w, h, t = 0.94, 2.18, 0.046
    stile = 0.112
    inner_w = w - stile * 2

    # Molded ironwood stiles & rails
    opaque.append(H.box("SCD_HingeStile", (stile * 0.5, 0.0, h * 0.5), (stile, t, h), "wood", bevel=0.004, grain="Z"))
    opaque.append(H.box("SCD_LatchStile", (w - stile * 0.5, 0.0, h * 0.5), (stile, t, h), "wood", bevel=0.004, grain="Z"))
    opaque.append(H.box("SCD_BotRail", (w * 0.5, 0.0, 0.14), (inner_w + 0.01, t, 0.28), "wood", bevel=0.004, grain="X"))
    opaque.append(H.box("SCD_MidRail", (w * 0.5, 0.0, 1.05), (inner_w + 0.01, t, 0.16), "wood", bevel=0.004, grain="X"))
    opaque.append(H.box("SCD_TopRail", (w * 0.5, 0.0, h - 0.085), (inner_w + 0.01, t, 0.17), "wood", bevel=0.004, grain="X"))

    # Lower recessed & raised beveled timber panel (z = 0.28..0.97)
    opaque.append(H.box("SCD_LowPanelBack", (w * 0.5, 0.0, 0.625), (inner_w - 0.01, t * 0.44, 0.69), "wood_light", bevel=0.003, grain="Z"))
    opaque.append(H.box("SCD_LowPanelRaise", (w * 0.5, 0.0, 0.625), (inner_w - 0.09, t * 0.78, 0.58), "wood", bevel=0.010, seg=2, grain="Z"))

    # Upper recessed frosted wired-glass observation pane (z = 1.13..2.01) + wooden glazing beads & cross muntins
    glass.append(H.box("SCD_Glass", (w * 0.5, 0.0, 1.57), (inner_w - 0.02, 0.010, 0.88), "FrostedGlass", bevel=0.0))
    opaque.append(H.box("SCD_MuntinH", (w * 0.5, 0.0, 1.57), (inner_w, 0.030, 0.028), "wood", bevel=0.003, grain="X"))
    opaque.append(H.box("SCD_MuntinV", (w * 0.5, 0.0, 1.57), (0.028, 0.024, 0.88), "wood", bevel=0.003, grain="Z"))
    for s in (-1, 1):
        opaque.append(H.box(f"SCD_BeadTop_{s}", (w * 0.5, s * 0.014, 2.00), (inner_w, 0.014, 0.020), "wood_light", bevel=0.002, grain="X"))
        opaque.append(H.box(f"SCD_BeadBot_{s}", (w * 0.5, s * 0.014, 1.14), (inner_w, 0.014, 0.020), "wood_light", bevel=0.002, grain="X"))

    # Brushed satin-inox stretcher kickplates, push escutcheons, brass cylinder & curved anti-snag levers (both sides)
    hx, hz = w - 0.088, 1.02
    opaque.append(H.box("SCD_LatchPlate", (w + 0.001, 0.0, hz), (0.004, 0.024, 0.175), "satin_inox", bevel=0.001))
    opaque.append(H.box("SCD_LatchBolt", (w + 0.008, 0.0, hz + 0.022), (0.014, 0.014, 0.024), "brass", bevel=0.002))
    for s in (-1, 1):
        ey = s * (t * 0.5 + 0.004)
        opaque.append(H.box(f"SCD_Kick_{s}", (w * 0.5, ey, 0.165), (w - 0.05, 0.005, 0.29), "satin_inox", bevel=0.0015))
        opaque.append(H.box(f"SCD_Esc_{s}", (hx, ey, hz - 0.022), (0.048, 0.008, 0.215), "satin_inox", bevel=0.003))
        opaque.append(H.cyl(f"SCD_Cyl_{s}", (hx, ey, hz - 0.082), (hx, ey + s * 0.007, hz - 0.082), 0.012, "brass", seg=14))
        opaque.append(H.pipe(f"SCD_Lever_{s}", [
            (hx, ey, hz), (hx, ey + s * 0.056, hz), (hx - 0.128, ey + s * 0.056, hz), (hx - 0.128, ey + s * 0.016, hz)
        ], 0.010, "satin_inox", res=4))

    # Overhead hydraulic door closer + scissor arm on STAFF side (-Y) & 3 brass hinge leaves
    opaque.append(H.box("SCD_CloserBox", (0.24, -t * 0.5 - 0.026, h - 0.082), (0.20, 0.044, 0.052), "anodized_alu", bevel=0.005))
    opaque.append(H.pipe("SCD_CloserArm", [
        (0.18, -t * 0.5 - 0.030, h - 0.052), (0.30, -t * 0.5 - 0.145, h + 0.008), (0.38, -t * 0.5 - 0.020, h + 0.028)
    ], 0.0075, "steel_dark", res=3))
    for k, hz_p in enumerate((0.28, 1.08, 1.88)):
        opaque.append(H.box(f"SCD_HingeLeaf_{k}", (0.026, -t * 0.5 - 0.002, hz_p), (0.048, 0.005, 0.10), "brass", bevel=0.001))

    return {"LeafBody": opaque, "LeafGlass": glass}


# --------------------------------------------------------------------------------------
# 3. CTCC_ENV_EmergencyDoorFrame (Heavy 1.40 x 2.20m ER portal casing for Door A & Door B!)
#    Origin at bottom center (0, 0, 0) -> placed at (4.0, 0, 16.0) & (0.05, 0, 16.0), yaw=0!
# --------------------------------------------------------------------------------------
def build_emergency_door_frame():
    objs = []
    w, h, wall_t = 1.40, 2.20, 0.25
    jamb_w = 0.072

    # Heavy enameled-steel/ironwood outer jambs + deep wall reveal casings + stainless stretcher bumper guards
    for s in (-1, 1):
        jx = s * (w * 0.5 + jamb_w * 0.5)
        objs.append(H.box(f"ERF_JambCore_{s}", (jx, 0.0, h * 0.5 + 0.035), (jamb_w, wall_t, h + 0.07), "er_door_sage", bevel=0.006))
        for sy in (-1, 1):
            objs.append(H.box(f"ERF_Architrave_{s}_{sy}", (s * (w * 0.5 + 0.056), sy * (wall_t * 0.5 + 0.011), h * 0.5 + 0.048),
                              (0.095, 0.024, h + 0.096), "er_door_sage", bevel=0.005))
        # Tall brushed satin-inox stretcher bumper guards wrapping lower 1.25m of jambs!
        objs.append(H.box(f"ERF_JambGuard_{s}", (s * (w * 0.5 + 0.036), 0.0, 0.62), (0.080, wall_t + 0.012, 1.24), "er_kick_inox", bevel=0.004))

    # Heavy header lintel box & stepped architrave header trim (both sides!)
    objs.append(H.box("ERF_HeaderCore", (0.0, 0.0, h + 0.036), (w + jamb_w * 2, wall_t, 0.072), "er_door_sage", bevel=0.006))
    for sy in (-1, 1):
        objs.append(H.box(f"ERF_HeaderTrim_{sy}", (0.0, sy * (wall_t * 0.5 + 0.012), h + 0.052),
                          (w + 0.21, 0.026, 0.096), "er_door_sage", bevel=0.006))
        objs.append(H.box(f"ERF_HeaderCap_{sy}", (0.0, sy * (wall_t * 0.5 + 0.018), h + 0.104),
                          (w + 0.24, 0.036, 0.022), "anodized_alu", bevel=0.003))

    # Twin recessed overhead spring-closer housing blocks
    for s in (-1, 1):
        objs.append(H.box(f"ERF_TopPivot_{s}", (s * (w * 0.5 - 0.10), 0.0, h - 0.010), (0.18, 0.072, 0.022), "satin_inox", bevel=0.003))

    # Ribbed brushed-stainless hospital stretcher ramp threshold plate (flush Z=0..0.015)
    objs.append(H.box("ERF_ThresholdBase", (0.0, 0.0, 0.007), (w + 0.02, 0.24, 0.014), "satin_inox", bevel=0.005))
    for ky in (-0.068, -0.034, 0.0, 0.034, 0.068):
        objs.append(H.box(f"ERF_ThreshRib_{ky:.3f}", (0.0, ky, 0.015), (w - 0.03, 0.009, 0.003), "steel_dark", bevel=0.001))

    return objs


# --------------------------------------------------------------------------------------
# 4. CTCC_ENV_EmergencySwingDoorLeaf (0.692 x 2.18m heavy swinging Vietnamese ER door leaf)
#    Pivot at hinge edge X=0, Y=0, Z=0 -> spans X in [0..0.692]!
#    Paired cleanly in Unity: Leaf 1 at x=0.004 yaw=0, Leaf 2 at x=1.396 yaw=180 -> ZERO gaps!
# --------------------------------------------------------------------------------------
def build_emergency_swing_door_leaf():
    opaque = []
    glass = []
    w, h, t = 0.692, 2.18, 0.052

    # Clean institutional sage-green enameled steel door body (NO ugly blotchy red splats!)
    # Lower panel (0..1.10), Top rail (1.92..2.18), Hinge stile (0..0.155), Meeting stile (0.535..0.692)
    opaque.append(H.box("ERL_LowerPanel", (w * 0.5, 0.0, 0.55), (w, t, 1.10), "er_door_sage", bevel=0.005))
    opaque.append(H.box("ERL_TopRail", (w * 0.5, 0.0, 2.05), (w, t, 0.26), "er_door_sage", bevel=0.005))
    opaque.append(H.box("ERL_HingeStile", (0.078, 0.0, 1.51), (0.156, t, 0.82), "er_door_sage", bevel=0.005))
    opaque.append(H.box("ERL_MeetStile", (w - 0.078, 0.0, 1.51), (0.156, t, 0.82), "er_door_sage", bevel=0.005))

    # Heavy brushed satin-inox stretcher kickplates on both faces (Z = 0.02..0.90) with perimeter rivets!
    for sy in (-1, 1):
        ky = sy * (t * 0.5 + 0.003)
        opaque.append(H.box(f"ERL_KickPlate_{sy}", (w * 0.5, ky, 0.46), (w - 0.04, 0.006, 0.88), "er_kick_inox", bevel=0.002))
        for rx in (0.055, w * 0.5, w - 0.055):
            for rz in (0.06, 0.46, 0.86):
                opaque.append(H.cyl(f"ERL_Rivet_{sy}_{rx:.2f}_{rz:.2f}",
                                    (rx, ky + sy * 0.002, rz), (rx, ky + sy * 0.0055, rz), 0.004, "steel_dark", seg=8))

    # Heavy black vulcanized rubber meeting-edge safety astragal bumper along X = w
    opaque.append(H.box("ERL_MeetRubber", (w - 0.004, 0.0, 1.09), (0.018, t + 0.004, 2.16), "rubber_ribbed", bevel=0.004))

    # Classic recessed hospital porthole viewing window with rounded-corner neoprene glazing gasket + frosted safety glass
    win_cx, win_cz = w * 0.5, 1.51
    win_w, win_h = 0.34, 0.78
    # Neoprene rubber surround frame (4 beveled bars + rounded corner bosses)
    opaque.append(H.box("ERL_GasketTop", (win_cx, 0.0, win_cz + win_h * 0.5), (win_w + 0.04, t + 0.014, 0.036), "rubber", bevel=0.008))
    opaque.append(H.box("ERL_GasketBot", (win_cx, 0.0, win_cz - win_h * 0.5), (win_w + 0.04, t + 0.014, 0.036), "rubber", bevel=0.008))
    for sx in (-1, 1):
        opaque.append(H.box(f"ERL_GasketSide_{sx}", (win_cx + sx * (win_w * 0.5 + 0.008), 0.0, win_cz),
                            (0.036, t + 0.014, win_h + 0.02), "rubber", bevel=0.008))
    glass.append(H.box("ERL_PortholeGlass", (win_cx, 0.0, win_cz), (win_w - 0.01, 0.012, win_h - 0.01), "FrostedGlass", bevel=0.0))

    # Heavy tubular brushed-stainless horizontal stretcher push crash bars + flange escutcheons on both faces!
    for sy in (-1, 1):
        py = sy * (t * 0.5 + 0.004)
        bar_y = sy * (t * 0.5 + 0.056)
        opaque.append(H.pipe(f"ERL_CrashBar_{sy}", [
            (0.095, py, 1.02), (0.095, bar_y, 1.02), (w - 0.095, bar_y, 1.02), (w - 0.095, py, 1.02)
        ], 0.016, "er_kick_inox", res=5))
        for fx in (0.095, w - 0.095):
            opaque.append(H.cyl(f"ERL_BarFlange_{sy}_{fx:.2f}", (fx, py - sy * 0.002, 1.02), (fx, py + sy * 0.008, 1.02), 0.028, "steel_dark", seg=14))

    # 3 Heavy double-action brass/steel hospital spring hinges along X = 0
    for hz in (0.30, 1.10, 1.88):
        opaque.append(H.cyl(f"ERL_SpringHinge_{hz:.2f}", (0.004, 0.0, hz - 0.068), (0.004, 0.0, hz + 0.068), 0.017, "satin_inox", seg=14))
        opaque.append(H.cyl(f"ERL_HingeCapT_{hz:.2f}", (0.004, 0.0, hz + 0.066), (0.004, 0.0, hz + 0.078), 0.019, "brass", seg=14))
        opaque.append(H.cyl(f"ERL_HingeCapB_{hz:.2f}", (0.004, 0.0, hz - 0.078), (0.004, 0.0, hz - 0.066), 0.019, "brass", seg=14))

    return {"ERDoorBody": opaque, "ERDoorGlass": glass}


# --------------------------------------------------------------------------------------
# 5. CTCC_PROP_EmergencyLightBox (Upgraded anodized aluminum hanging CẤP CỨU lightbox)
#    Origin at center (0,0,0) -> placed at (4.0 / 0.05, 2.94, 15.84); ceiling is at Z=+0.66!
# --------------------------------------------------------------------------------------
def build_emergency_lightbox():
    body = []
    glow = []

    bw, bd, bh = 1.18, 0.145, 0.39
    # Extruded anodized aluminum perimeter housing frame (4 mitered rails + corner brackets)
    body.append(H.box("ELB_CaseTop", (0.0, 0.0, bh * 0.5 - 0.016), (bw, bd, 0.032), "anodized_alu", bevel=0.005))
    body.append(H.box("ELB_CaseBot", (0.0, 0.0, -bh * 0.5 + 0.016), (bw, bd, 0.032), "anodized_alu", bevel=0.005))
    for sx in (-1, 1):
        body.append(H.box(f"ELB_CaseSide_{sx}", (sx * (bw * 0.5 - 0.016), 0.0, 0.0), (0.032, bd, bh - 0.05), "anodized_alu", bevel=0.005))
        # Side heat-dissipation cooling louver slots
        for kz in (-0.08, -0.04, 0.0, 0.04, 0.08):
            body.append(H.box(f"ELB_Vent_{sx}_{kz:.2f}", (sx * (bw * 0.5 + 0.001), 0.0, kz), (0.005, 0.078, 0.010), "steel_dark", bevel=0.001))
        # Riveted corner angle brackets
        for sz in (-1, 1):
            body.append(H.box(f"ELB_Corner_{sx}_{sz}", (sx * (bw * 0.5 - 0.018), 0.0, sz * (bh * 0.5 - 0.018)),
                              (0.044, bd + 0.008, 0.044), "steel_dark", bevel=0.003))

    # Recessed aluminum front/back acrylic bezel lip framing the glowing red face
    for sy in (-1, 1):
        fy = sy * (bd * 0.5 - 0.008)
        body.append(H.box(f"ELB_BezelTop_{sy}", (0.0, fy, bh * 0.5 - 0.030), (bw - 0.04, 0.016, 0.020), "steel_dark", bevel=0.002))
        body.append(H.box(f"ELB_BezelBot_{sy}", (0.0, fy, -bh * 0.5 + 0.030), (bw - 0.04, 0.016, 0.020), "steel_dark", bevel=0.002))
        glow.append(H.box(f"ELB_GlowFace_{sy}", (0.0, sy * (bd * 0.5 - 0.005), 0.0), (bw - 0.07, 0.012, bh - 0.072), "CrimsonGlow", bevel=0.001))

    # Twin ceiling suspension steel rods + top ceiling flange rosettes + knurled lock nuts + flexible electrical feed conduit!
    for rx in (-0.42, 0.42):
        body.append(H.cyl(f"ELB_Rod_{rx:.2f}", (rx, 0.0, bh * 0.5), (rx, 0.0, 0.66), 0.0095, "satin_inox", seg=12))
        body.append(H.cyl(f"ELB_BoxNut_{rx:.2f}", (rx, 0.0, bh * 0.5 - 0.004), (rx, 0.0, bh * 0.5 + 0.020), 0.020, "brass", seg=8))
        body.append(H.cyl(f"ELB_CeilFlange_{rx:.2f}", (rx, 0.0, 0.642), (rx, 0.0, 0.664), 0.042, "anodized_alu", seg=16))
    body.append(H.pipe("ELB_PowerFlex", [
        (0.0, 0.0, bh * 0.5), (0.0, 0.0, 0.36), (0.045, 0.02, 0.52), (0.0, 0.0, 0.66)
    ], 0.008, "rubber", res=4))
    body.append(H.cyl("ELB_CableGland", (0.0, 0.0, bh * 0.5 - 0.004), (0.0, 0.0, bh * 0.5 + 0.024), 0.016, "brass", seg=12))

    return {"LightBoxBody": body, "LightBoxGlow": glow}


# --------------------------------------------------------------------------------------
# 6. CTCC_PROP_PASpeaker (Upgraded hospital corridor PA intercom broadcast box!)
#    Back at Y=0, grille faces -Y tilted slightly downward.
#    Placed at (5.50, 2.62, 3.35), yaw=90 -> faces -X into HALL; conduit rises Z=+0.55 to y=3.17m!
# --------------------------------------------------------------------------------------
def build_pa_speaker():
    objs = []
    tilt = math.radians(8.5)

    # Slanted dovetailed varnished ironwood speaker cabinet (0.30w x 0.14d x 0.22h)
    objs.append(H.box("PA_Cabinet", (0.0, -0.068, 0.0), (0.30, 0.135, 0.22), "wood", bevel=0.007, seg=2, rot=(tilt, 0, 0), grain="X"))
    # Recessed cream-enamel front frame bezel + woven dark acoustic grille cloth backing
    objs.append(H.box("PA_FrontBezel", (0.0, -0.132, -0.008), (0.275, 0.016, 0.195), "pa_cream", bevel=0.004, rot=(tilt, 0, 0)))
    objs.append(H.box("PA_ClothBaffle", (0.0, -0.136, -0.009), (0.246, 0.008, 0.166), "acoustic_cloth", bevel=0.0, rot=(tilt, 0, 0)))
    # 9 Crisp angled cream-enamel acoustic louver slats + central vertical reinforcing rib
    for k in range(9):
        sz = -0.072 + k * 0.018
        objs.append(H.box(f"PA_Slat_{k}", (0.0, -0.141 - sz * 0.15, sz - 0.008),
                          (0.244, 0.012, 0.0075), "pa_cream", bevel=0.0015, rot=(math.radians(-16), 0, 0)))
    objs.append(H.box("PA_CenterRib", (0.0, -0.142, -0.009), (0.010, 0.012, 0.168), "pa_cream", bevel=0.0015, rot=(tilt, 0, 0)))

    # Small riveted brass speaker serial emblem at bottom center bezel
    objs.append(H.box("PA_Badge", (0.0, -0.130, -0.096), (0.056, 0.006, 0.016), "brass", bevel=0.0015, rot=(tilt, 0, 0)))

    # Side rotary transformer impedance/volume attenuator dial (+X side) + rear steel wall bracket
    objs.append(H.cyl("PA_VolPlate", (0.149, -0.062, -0.025), (0.156, -0.062, -0.025), 0.020, "satin_inox", seg=16))
    objs.append(H.cyl("PA_VolKnob", (0.154, -0.062, -0.025), (0.172, -0.062, -0.025), 0.012, "plastic_black", seg=14, bevel=0.002))
    objs.append(H.box("PA_WallBracket", (0.0, -0.006, 0.0), (0.22, 0.012, 0.16), "steel_dark", bevel=0.002))

    # Galvanized electrical conduit riser rising cleanly +0.55m into overhead east-wall ConduitRun (y=3.17m) — ZERO floating wire!
    objs.append(H.pipe("PA_ConduitRiser", [
        (0.085, -0.032, 0.095), (0.085, -0.020, 0.16), (0.085, -0.020, 0.555)
    ], 0.0085, "galv_conduit", res=4))
    objs.append(H.box("PA_ConduitClamp", (0.085, -0.018, 0.34), (0.034, 0.024, 0.016), "satin_inox", bevel=0.002))
    return objs


# --------------------------------------------------------------------------------------
# 7. CTCC_PROP_FireExtinguisher (Hero PCCC MFZ4 Extinguisher + Wall Cradle + Fire Alarm Bell!)
#    Back at Y=+0.095, front faces -Y. Placed at (2.60, 0.95, 5.66), yaw=180 -> back against West wall x=2.50, faces +X!
#    Matches CH01_EXTINGUISHER description: yellowed 2005 inspection tag + brighter metal under strap!
# --------------------------------------------------------------------------------------
def build_fire_extinguisher():
    objs = []

    # Heavy riveted wall-mounted steel backing plate & cradle bracket (mounts at Y=+0.092 against wall!)
    objs.append(H.box("PCCC_BackPlate", (0.0, 0.088, 0.44), (0.25, 0.014, 0.88), "breaker_grey", bevel=0.004))
    for sx in (-0.098, 0.098):
        for sz in (0.05, 0.83):
            objs.append(H.cyl(f"PCCC_MountBolt_{sx:.2f}_{sz:.2f}", (sx, 0.082, sz), (sx, 0.074, sz), 0.006, "satin_inox", seg=8))

    # Lower tank foot cradle shelf + protective steel retention strap (around Z=0.29m)
    objs.append(H.box("PCCC_FootRest", (0.0, 0.022, -0.008), (0.145, 0.135, 0.014), "steel_dark", bevel=0.003))
    objs.append(H.box("PCCC_StrapBack", (0.0, 0.075, 0.295), (0.175, 0.024, 0.036), "satin_inox", bevel=0.002))
    strap_pts = [
        (-0.082, 0.072, 0.295), (-0.084, -0.020, 0.295), (-0.055, -0.072, 0.295),
        (0.0, -0.084, 0.295), (0.055, -0.072, 0.295), (0.084, -0.020, 0.295), (0.082, 0.072, 0.295)
    ]
    objs.append(H.pipe("PCCC_RetainStrap", strap_pts, 0.0075, "satin_inox", res=4))
    # Brighter protected red-steel ring right beneath retention strap (CH01_EXTINGUISHER tactile clue!)
    objs.append(H.cyl("PCCC_BrightBand", (0.0, 0.0, 0.268), (0.0, 0.0, 0.292), 0.0792, "plastic_red", seg=28, cap=False))

    # Seamless red-enameled CO2/ABC powder steel cylinder (Ø15.8cm x 51cm)
    objs.append(H.lathe("PCCC_Tank", [
        (0.0, 0.0), (0.072, 0.0), (0.0785, 0.016), (0.0785, 0.395),
        (0.068, 0.442), (0.036, 0.478), (0.024, 0.492), (0.024, 0.512), (0.0, 0.512)
    ], (0.0, 0.0, 0.0), "pccc_red", seg=32))
    # Bottom black rubber protective foot ring
    objs.append(H.cyl("PCCC_BottomRing", (0.0, 0.0, -0.001), (0.0, 0.0, 0.028), 0.0802, "rubber", seg=28))

    # Crisp printed Vietnamese PCCC instruction wrap label (cylindrical PRINT UV around tank front -Y!)
    lbl = H.cyl("PCCC_WrapLabel", (0.0, 0.0, 0.115), (0.0, 0.0, 0.262), 0.0796, "print:pccc_extinguisher_label", seg=32, cap=False)
    H.set_cylindrical_print_uv(lbl, 0.115, 0.262, angle_offset=math.pi)
    objs.append(lbl)

    # Brass valve neck assembly, safety pin & pull ring, pressure gauge dial & black steel squeeze levers
    objs.append(H.cyl("PCCC_ValveNeck", (0.0, 0.0, 0.508), (0.0, 0.0, 0.562), 0.018, "brass", seg=16))
    objs.append(H.cyl("PCCC_ValveNut", (0.0, 0.0, 0.505), (0.0, 0.0, 0.524), 0.025, "brass", seg=6))
    objs.append(H.pipe("PCCC_CarryHandle", [(0.0, 0.0, 0.540), (-0.065, 0.0, 0.532), (-0.105, 0.0, 0.518)], 0.0075, "steel_dark", res=4))
    objs.append(H.pipe("PCCC_SqueezeLever", [(0.0, 0.0, 0.558), (-0.055, 0.0, 0.578), (-0.102, 0.0, 0.602)], 0.0075, "steel_dark", res=4))
    # Safety pull ring & wire lead seal
    ring_pts = [(-0.028 + 0.015 * math.cos(k * math.tau / 14), -0.022, 0.556 + 0.015 * math.sin(k * math.tau / 14)) for k in range(14)]
    objs.append(H.pipe("PCCC_SafetyRing", ring_pts, 0.0028, "satin_inox", res=3, cyclic=True))
    # Pressure gauge housing facing front (-Y)
    objs.append(H.cyl("PCCC_GaugeBody", (0.0, -0.016, 0.540), (0.0, -0.036, 0.540), 0.017, "satin_inox", seg=18))
    objs.append(H.cyl("PCCC_GaugeDial", (0.0, -0.034, 0.540), (0.0, -0.038, 0.540), 0.014, "enamel_white", seg=18))

    # Yellowed hanging PCCC 2005 inspection tag dangling on brass wire from valve neck (CH01_EXTINGUISHER clue!)
    objs.append(H.pipe("PCCC_TagWire", [(-0.015, -0.012, 0.522), (-0.048, -0.048, 0.478), (-0.064, -0.065, 0.445)], 0.0018, "brass", res=3))
    tag = H.box("PCCC_InspectTag", (-0.068, -0.068, 0.398), (0.064, 0.004, 0.094), "print:pccc_inspect_tag",
                bevel=0.001, rot=(math.radians(6), math.radians(-8), math.radians(18)))
    H.set_planar_print_uv(tag, axis="Y", uv_rect=(0.0, 0.0, 1.0, 1.0))
    objs.append(tag)

    # Curved ribbed rubber discharge hose & flared conical black discharge horn clipped to right side
    objs.append(H.pipe("PCCC_Hose", [
        (0.018, 0.0, 0.542), (0.076, -0.010, 0.525), (0.105, -0.022, 0.450),
        (0.108, -0.026, 0.250), (0.098, -0.022, 0.115)
    ], 0.009, "rubber_ribbed", res=4))
    objs.append(H.cyl("PCCC_NozzleHorn", (0.098, -0.022, 0.125), (0.094, -0.022, 0.022), 0.013, "plastic_black", seg=16, r2=0.028))

    # Iconic Vietnamese red circular fire alarm bell (chuông báo cháy) mounted on upper steel plate (Z=0.73m)!
    objs.append(H.cyl("PCCC_BellBase", (-0.045, 0.080, 0.73), (-0.045, 0.052, 0.73), 0.048, "steel_dark", seg=18))
    objs.append(H.lathe("PCCC_BellDome", [
        (0.0, 0.032), (0.042, 0.030), (0.064, 0.018), (0.072, 0.0), (0.068, -0.014), (0.062, -0.014)
    ], (-0.045, 0.048, 0.73), "pccc_red", seg=24, rot=(math.radians(90), 0, 0)))
    objs.append(H.cyl("PCCC_BellCap", (-0.045, 0.024, 0.73), (-0.045, 0.010, 0.73), 0.012, "brass", seg=12))
    # Red manual pull-box beside alarm bell (+X side, Z=0.73)
    objs.append(H.box("PCCC_PullBox", (0.066, 0.062, 0.73), (0.078, 0.042, 0.088), "pccc_red", bevel=0.005))
    objs.append(H.box("PCCC_PullGlass", (0.066, 0.040, 0.73), (0.056, 0.006, 0.058), "enamel_white", bevel=0.001))
    objs.append(H.cyl("PCCC_PullButton", (0.066, 0.040, 0.73), (0.066, 0.031, 0.73), 0.014, "plastic_black", seg=14))
    # Galvanized alarm conduit riser connecting cleanly up to West overhead ConduitRun (0.95 + 2.21 = 3.16m!)
    objs.append(H.cyl("PCCC_AlarmPipe", (0.0, 0.074, 0.86), (0.0, 0.074, 2.21), 0.0095, "galv_conduit", seg=12))
    for cz in (1.38, 1.92):
        objs.append(H.box(f"PCCC_PipeClamp_{cz:.2f}", (0.0, 0.076, cz), (0.036, 0.026, 0.018), "satin_inox", bevel=0.002))

    return objs


# --------------------------------------------------------------------------------------
# 8. CTCC_HALL_WayfindingAndMEP (5 reusable hospital corridor fittings as separate parts!)
#    All wall parts have back at X=0 and face +X (yaw=0 on West wall, yaw=180 on East wall).
#    BladeSign hangs from ceiling beam (origin at top mount Z=0, drops down to Z=-0.62m).
# --------------------------------------------------------------------------------------
def build_wayfinding_and_mep(only: str | None = None):
    # Part A: Framed Glass-Front Hospital Ground-Floor Directory & Evacuation Plan (1.06 x 0.78m)
    fmap = []
    if only in (None, "FloorMapBoard"):
        mw, mh = 1.06, 0.78
        fmap.append(H.box("FMap_Back", (0.012, 0.0, 0.0), (0.024, mw - 0.02, mh - 0.02), "wood", bevel=0.004, grain="Y"))
        fmap.append(H.box("FMap_FrameT", (0.030, 0.0, mh * 0.5 - 0.024), (0.044, mw + 0.02, 0.048), "wood_light", bevel=0.005, grain="Y"))
        fmap.append(H.box("FMap_FrameB", (0.030, 0.0, -mh * 0.5 + 0.024), (0.044, mw + 0.02, 0.048), "wood_light", bevel=0.005, grain="Y"))
        fmap.append(H.box("FMap_FrameL", (0.030, -mw * 0.5 + 0.024, 0.0), (0.044, 0.048, mh - 0.096), "wood_light", bevel=0.005, grain="Z"))
        fmap.append(H.box("FMap_FrameR", (0.030, mw * 0.5 - 0.024, 0.0), (0.044, 0.048, mh - 0.096), "wood_light", bevel=0.005, grain="Z"))
        mface = H.box("FMap_Print", (0.026, 0.0, 0.0), (0.005, mw - 0.094, mh - 0.094), "print:hall_floor_map", bevel=0.0)
        H.set_planar_print_uv(mface, axis="X", uv_rect=(0.0, 0.0, 1.0, 1.0))
        fmap.append(mface)
        for sy in (-mw * 0.44, mw * 0.44):
            for sz in (-mh * 0.44, mh * 0.44):
                fmap.append(H.cyl(f"FMap_Bolt_{sy:.2f}_{sz:.2f}", (0.048, sy, sz), (0.056, sy, sz), 0.006, "brass", seg=10))

    # Part B: Hanging Double-Sided Corridor Directional Blade Sign (0.98w x 0.36h, drops from ceiling slab Z=0 down to Z=-0.80!)
    #         Geographically accurate dual prints: -Y faces South (seen heading North to ER), +Y faces North (seen heading South to Reception)!
    blade = []
    if only in (None, "BladeSign"):
        bw, bh = 0.98, 0.36
        bc_z = -0.62
        blade.append(H.box("Blade_Case", (0.0, 0.0, bc_z), (bw, 0.042, bh), "anodized_alu", bevel=0.005))
        for sy, flip, pkind in ((-1, False, "print:hall_blade_sign"), (1, True, "print:hall_blade_sign_south")):
            bf = H.box(f"Blade_Face_{sy}", (0.0, sy * 0.022, bc_z), (bw - 0.04, 0.004, bh - 0.04), pkind, bevel=0.0)
            H.set_planar_print_uv(bf, axis="Y", uv_rect=(0.0, 0.0, 1.0, 1.0), flip_u=flip)
            blade.append(bf)
        for rx in (-0.34, 0.34):
            blade.append(H.cyl(f"Blade_Rod_{rx:.2f}", (rx, 0.0, bc_z + bh * 0.5), (rx, 0.0, 0.0), 0.008, "satin_inox", seg=12))
            blade.append(H.cyl(f"Blade_Flange_{rx:.2f}", (rx, 0.0, -0.015), (rx, 0.0, 0.004), 0.028, "anodized_alu", seg=14))

    # Part C: Wall-Mounted Alcohol Hand-Sanitizer Dispenser
    sani = []
    if only in (None, "HandSanitizer"):
        sani.append(H.box("Sani_BackPlate", (0.008, 0.0, 0.0), (0.016, 0.14, 0.28), "satin_inox", bevel=0.003))
        sani.append(H.box("Sani_Shelf", (0.056, 0.0, -0.105), (0.105, 0.11, 0.010), "satin_inox", bevel=0.002))
        sani.append(H.pipe("Sani_GuardRail", [
            (0.012, -0.052, -0.025), (0.095, -0.052, -0.025), (0.095, 0.052, -0.025), (0.012, 0.052, -0.025)
        ], 0.0045, "satin_inox", res=3))
        sani.append(H.cyl("Sani_Bottle", (0.056, 0.0, -0.098), (0.056, 0.0, 0.048), 0.036, "enamel_white", seg=18, bevel=0.006))
        sani.append(H.cyl("Sani_PumpNeck", (0.056, 0.0, 0.048), (0.056, 0.0, 0.092), 0.011, "enamel_blue", seg=12))
        sani.append(H.box("Sani_PumpNozzle", (0.082, 0.0, 0.086), (0.052, 0.014, 0.012), "enamel_blue", bevel=0.002))
        sani.append(H.pipe("Sani_ElbowArm", [
            (0.014, 0.0, 0.115), (0.072, 0.0, 0.115), (0.115, 0.0, 0.085), (0.115, 0.0, 0.045)
        ], 0.006, "satin_inox", res=3))

    # Part D: Institutional Wall-Mounted Electrical Distribution Breaker Panel (TỦ ĐIỆN TẦNG TRỆT)
    panel = []
    if only in (None, "BreakerPanel"):
        pw, pd, ph = 0.48, 0.11, 0.68
        panel.append(H.box("Brk_Box", (pd * 0.5, 0.0, 0.0), (pd, pw, ph), "breaker_grey", bevel=0.006))
        panel.append(H.box("Brk_Door", (pd + 0.008, 0.0, 0.0), (0.018, pw - 0.024, ph - 0.024), "breaker_grey", bevel=0.004))
        pface = H.box("Brk_Print", (pd + 0.018, 0.0, 0.0), (0.003, pw - 0.052, ph - 0.052), "print:breaker_schedule", bevel=0.0)
        H.set_planar_print_uv(pface, axis="X", uv_rect=(0.0, 0.0, 1.0, 1.0))
        panel.append(pface)
        panel.append(H.box("Brk_LatchPlate", (pd + 0.019, pw * 0.42, 0.0), (0.008, 0.036, 0.088), "steel_dark", bevel=0.002))
        panel.append(H.box("Brk_LatchTurn", (pd + 0.028, pw * 0.42, 0.0), (0.014, 0.016, 0.048), "satin_inox", bevel=0.002))
        for hz in (-0.22, 0.22):
            panel.append(H.cyl(f"Brk_Hinge_{hz:.2f}", (pd + 0.008, -pw * 0.5 + 0.008, hz - 0.026),
                               (pd + 0.008, -pw * 0.5 + 0.008, hz + 0.026), 0.006, "satin_inox", seg=10))
        for li, (ly, lkind) in enumerate(((-0.08, "pccc_red"), (0.0, "enamel_lamp"), (0.08, "brass"))):
            panel.append(H.cyl(f"Brk_Lamp_{li}", (pd + 0.012, ly, ph * 0.5 - 0.028), (pd + 0.028, ly, ph * 0.5 - 0.028), 0.011, lkind, seg=12))
        for cy in (-0.09, 0.09):
            panel.append(H.cyl(f"Brk_TopPipe_{cy:.2f}", (0.035, cy, ph * 0.5), (0.035, cy, ph * 0.5 + 1.24), 0.014, "galv_conduit", seg=12))
        panel.append(H.box("Brk_ClampTop", (0.030, 0.0, ph * 0.5 + 0.65), (0.036, 0.24, 0.024), "satin_inox", bevel=0.002))

    # Part E: High Wall Louvered Hospital Ventilation Exhaust Grille
    vent = []
    if only in (None, "WallVentGrille"):
        vw, vh = 0.44, 0.24
        vent.append(H.box("Vent_FrameT", (0.012, 0.0, vh * 0.5 - 0.014), (0.024, vw, 0.028), "anodized_alu", bevel=0.003))
        vent.append(H.box("Vent_FrameB", (0.012, 0.0, -vh * 0.5 + 0.014), (0.024, vw, 0.028), "anodized_alu", bevel=0.003))
        for sy in (-1, 1):
            vent.append(H.box(f"Vent_FrameS_{sy}", (0.012, sy * (vw * 0.5 - 0.014), 0.0), (0.024, 0.028, vh - 0.056), "anodized_alu", bevel=0.003))
        vent.append(H.box("Vent_BackDark", (0.003, 0.0, 0.0), (0.006, vw - 0.04, vh - 0.04), "plastic_black", bevel=0.0))
        for k in range(7):
            vz = -vh * 0.36 + k * (vh * 0.72 / 6.0)
            vent.append(H.box(f"Vent_Louver_{k}", (0.014, 0.0, vz), (0.020, vw - 0.052, 0.008), "anodized_alu",
                              bevel=0.001, rot=(0, math.radians(-26), 0)))

    return {
        "FloorMapBoard": fmap,
        "BladeSign": blade,
        "HandSanitizer": sani,
        "BreakerPanel": panel,
        "WallVentGrille": vent,
    }


def build_floor_map_board():
    return build_wayfinding_and_mep("FloorMapBoard")["FloorMapBoard"]


def build_blade_sign():
    return build_wayfinding_and_mep("BladeSign")["BladeSign"]


def build_hand_sanitizer():
    return build_wayfinding_and_mep("HandSanitizer")["HandSanitizer"]


def build_breaker_panel():
    return build_wayfinding_and_mep("BreakerPanel")["BreakerPanel"]


def build_wall_vent_grille():
    return build_wayfinding_and_mep("WallVentGrille")["WallVentGrille"]


def run_split_fixes():
    HS.export_hs_asset(
        "CTCC_HALL_FloorMapBoard", build_floor_map_board,
        {"HALL_FloorMap": {"res": 1024, "bake_r": True}},
        make_lods=False, cam_dir=(1.85, -0.75, 0.25), ao_samples=10,
    )
    HS.export_hs_asset(
        "CTCC_HALL_BladeSign", build_blade_sign,
        {"HALL_BladeSign": {"res": 1024, "bake_r": True}},
        make_lods=False, cam_dir=(0.65, -1.85, -0.20), ao_samples=10,
    )
    HS.export_hs_asset(
        "CTCC_HALL_HandSanitizer", build_hand_sanitizer,
        {"HALL_Sanitizer": {"res": 512, "bake_r": True}},
        make_lods=False, cam_dir=(1.55, -0.95, 0.35), ao_samples=10,
    )
    HS.export_hs_asset(
        "CTCC_HALL_BreakerPanel", build_breaker_panel,
        {"HALL_BreakerPanel": {"res": 1024, "bake_r": True}},
        make_lods=False, cam_dir=(1.65, -0.95, 0.30), ao_samples=10,
    )
    HS.export_hs_asset(
        "CTCC_HALL_WallVentGrille", build_wall_vent_grille,
        {"HALL_VentGrille": {"res": 512, "bake_r": True}},
        make_lods=False, render=False, ao_samples=8,
    )


def run_all():
    HS.export_hs_asset(
        "CTCC_ENV_StaffCrossDoorFrame", build_staff_cross_door_frame,
        {"HALL_StaffDoorFrame": {"res": 1024, "bake_r": True}},
        make_lods=True, cam_dir=(1.85, -1.35, 0.45), ao_samples=10,
    )
    HS.export_hs_asset(
        "CTCC_ENV_EmergencyDoorFrame", build_emergency_door_frame,
        {"HALL_ERDoorFrame": {"res": 1024, "bake_r": True}},
        make_lods=True, cam_dir=(1.15, -2.10, 0.45), ao_samples=10,
    )
    HS.export_hs_asset(
        "CTCC_ENV_EmergencySwingDoorLeaf", build_emergency_swing_door_leaf,
        {"HALL_ERDoorLeaf": {"res": 1024, "bake_r": True, "parts": ["ERDoorBody"]}},
        make_lods=True, cam_dir=(1.15, -2.05, 0.42), ao_samples=10,
    )
    HS.export_hs_asset(
        "CTCC_PROP_EmergencyLightBox", build_emergency_lightbox,
        {"HALL_ERLightBox": {"res": 1024, "bake_r": True, "parts": ["LightBoxBody"]}},
        make_lods=False, cam_dir=(0.95, -1.85, 0.30), ao_samples=10,
    )
    HS.export_hs_asset(
        "CTCC_PROP_PASpeaker", build_pa_speaker,
        {"HALL_PASpeaker": {"res": 1024, "bake_r": True}},
        make_lods=True, cam_dir=(1.15, -1.75, 0.35), ao_samples=10,
    )
    HS.export_hs_asset(
        "CTCC_PROP_FireExtinguisher", build_fire_extinguisher,
        {"HALL_FireStation": {"res": 1024, "bake_r": True}},
        make_lods=True, cam_dir=(1.15, -1.85, 0.45), ao_samples=12,
    )
    run_split_fixes()


if __name__ == "__main__":
    run_all()
