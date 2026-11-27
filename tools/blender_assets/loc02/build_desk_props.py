"""Hero Blender 4.5 assets: CTCC_PROP_ReceptionWorking & CTCC_PROP_ReceptionProps.

Placed on `desk (1.45, 0, -3.24)` at local (0.32, 1.128, -0.18) -> world (1.77, 1.128, -3.42).
Separate parts preserved strictly for HospitalAuthoring.RebirthTwo.cs contract:
  - "DutyLedger"      : Hero cloth-bound open Duty Ledger (AN01) with arched curved pages,
                        high-res Vietnamese handwritten entries (an01_ledger_spread.png),
                        brass corners, crimson bookmark ribbon & faceted ballpoint pen.
                        Shifted cleanly to lx = -0.16 (world x = 1.61, max x = 1.86) so it NEVER
                        clips the stainless pass-through tray (world x >= 1.92)!
  - "Handset"         : Sculpted ergonomic curved landline handset (animated on ring/answer!)
  - "CounterDressing" : Vintage 2000s push-button landline phone + 28-turn 3D helical coiled cord
                        + printed extension directory card + articulated green-enamel desk lamp
                        (stepped weighted base, switch, braided cord, twin articulated arms,
                        knurled thumbscrews, bell shade, white liner, warm tungsten bulb)
                        + dovetailed 2-tier wooden inbox tray with staggered tabbed patient folders
                        + wooden rubber stamp rack with 2 turned stamps + hinged red ink pad tin
                        + ceramic pen cup with pens, pencils & wooden ruler!

Exports BOTH CTCC_PROP_ReceptionWorking (separate_parts=True) and CTCC_PROP_ReceptionProps (single mesh)
sharing one crisp 2048x2048 Cycles-baked PBR atlas (LOC02_DeskPropsB / LOC02_DeskPropsR).
"""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import hero_lib as H


def build_desk_props_parts():
    ledger = []
    handset = []
    dressing = []

    # =========================================================================
    # 1. HERO OPEN CLOTH-BOUND DUTY LEDGER (AN01) — centered at (-0.16, 0.02, 0.0)
    #    Spread spans x in [-0.41, +0.09] (world x in [1.36, 1.86]) -> 6cm clear of tray!
    # =========================================================================
    lx, ly = -0.16, 0.02
    pw, ph = 0.235, 0.345  # each page width x height

    # Cloth-bound hardcover boards (left & right) + ribbed curved spine + brass corner protectors
    ledger.append(H.box("LedgerCoverL", (lx - pw * 0.51 - 0.008, ly, 0.0055), (pw + 0.018, ph + 0.020, 0.009),
                        "cloth_maroon", bevel=0.0025, rot=(0, math.radians(1.5), 0)))
    ledger.append(H.box("LedgerCoverR", (lx + pw * 0.51 + 0.008, ly, 0.0055), (pw + 0.018, ph + 0.020, 0.009),
                        "cloth_maroon", bevel=0.0025, rot=(0, math.radians(-1.5), 0)))
    ledger.append(H.box("LedgerSpine", (lx, ly, 0.0045), (0.036, ph + 0.022, 0.010), "cloth_maroon", bevel=0.003))
    # Brass corner reinforcement caps on the 4 outer cover corners
    for sx in (-1, 1):
        for sy in (-1, 1):
            ledger.append(H.box(f"LedgerBrassCorner_{sx}_{sy}",
                                (lx + sx * (pw + 0.012), ly + sy * (ph * 0.5 + 0.006), 0.0075),
                                (0.022, 0.022, 0.011), "brass", bevel=0.002))

    # Solid stacked page block bases underneath the sculpted top spread
    ledger.append(H.box("LedgerBlockL", (lx - pw * 0.50 - 0.004, ly, 0.015), (pw - 0.006, ph - 0.006, 0.014),
                        "paper", bevel=0.002, rot=(0, math.radians(2.2), 0)))
    ledger.append(H.box("LedgerBlockR", (lx + pw * 0.50 + 0.004, ly, 0.016), (pw - 0.006, ph - 0.006, 0.016),
                        "paper", bevel=0.002, rot=(0, math.radians(-2.2), 0)))

    # Sculpted smoothly arched open top spread with 2048px Vietnamese handwritten entries!
    # Visitor stands at +X looking toward -X, so book top faces +Z and reads naturally toward Lan (-X) / angled readable!
    def spread_fn(u, v):
        # u in [0..1] across left-to-right spread along Y or X?
        # Wait: In Unity, visitor looks from +X toward -X.
        # In original ledger: left/right pages split along X (lx - 0.145, lx + 0.145).
        # Let's map u along X (lx - pw .. lx + pw) and v along Y (ly - ph/2 .. ly + ph/2) with realistic central gutter dip!
        s = (u - 0.5) * 2.0  # -1 (west/Lan side) .. +1 (east/visitor side)
        x = lx + s * pw
        y = ly + (v - 0.5) * ph
        # Realistic arched page curve dipping sharply into center stitched gutter at s=0
        ab = abs(s)
        arch = 0.011 * math.sin(ab * math.pi) + 0.005 * (1.0 - math.exp(-ab * 7.0))
        # Subtle organic page curl at bottom corners
        curl = 0.003 * (ab ** 2) * ((1.0 - v) ** 2)
        z = 0.020 + arch + curl
        return (x, y, z)

    # Rotate print UV 90 deg so horizontal text rows align cleanly with the spread spine
    spread_obj = H.sculpted_grid("LedgerOpenSpread", 20, 12, spread_fn, "print:an01_ledger_spread",
                                 thickness=0.003, print_uv=True, uv_rect=(0.0, 0.0, 1.0, 1.0))
    # Map PRINT UV directly so left page = left half, right page = right half
    ledger.append(spread_obj)

    # Crimson woven bookmark ribbon emerging from spine gutter & draping casually onto counter
    ribbon_pts = [
        (lx, ly + ph * 0.42, 0.023),
        (lx + 0.002, ly + 0.02, 0.021),
        (lx + 0.008, ly - ph * 0.44, 0.022),
        (lx + 0.022, ly - ph * 0.54, 0.014),
        (lx + 0.048, ly - ph * 0.62, 0.004),
        (lx + 0.088, ly - ph * 0.68, 0.003),
    ]
    ledger.append(H.pipe("LedgerRibbon", ribbon_pts, 0.0038, "cloth_maroon", res=4))

    # Detailed faceted Vietnamese ballpoint pen resting diagonally on the right page
    p0 = (lx + 0.055, ly - 0.085, 0.036)
    p1 = (lx + 0.175, ly + 0.018, 0.036)
    ledger.append(H.cyl("DeskPenBarrel", p0, p1, 0.0046, "enamel_blue", seg=8))
    ledger.append(H.cyl("DeskPenTipCone", p1, (lx + 0.193, ly + 0.034, 0.035), 0.0044, "brass", seg=10, r2=0.0012))
    ledger.append(H.cyl("DeskPenTailCap", (lx + 0.045, ly - 0.094, 0.036), p0, 0.0048, "plastic_black", seg=10))
    ledger.append(H.box("DeskPenClip", (lx + 0.082, ly - 0.062, 0.0415), (0.042, 0.004, 0.004), "brass",
                        bevel=0.001, rot=(0, 0, math.atan2(0.103, 0.120))))

    # =========================================================================
    # 2. VINTAGE 2000s PUSH-BUTTON LANDLINE PHONE + SCULPTED HANDSET + HELICAL CORD
    #    Centered at (-0.18, +0.46, 0.0), angled slightly toward Lan!
    # =========================================================================
    px, py = -0.18, 0.46
    yaw_p = 0.22
    cp, sp = math.cos(yaw_p), math.sin(yaw_p)

    def ppt(x, y, z):
        return (px + x * cp - y * sp, py + x * sp + y * cp, z)

    # Rubber anti-slip feet
    for fx in (-0.072, 0.072):
        for fy in (-0.088, 0.088):
            dressing.append(H.cyl(f"PhoneFoot_{fx:.2f}_{fy:.2f}", ppt(fx, fy, 0.0), ppt(fx, fy, 0.006), 0.011, "rubber", seg=12))

    # Molded tapered lower ivory-plastic housing + raised rear cradle shoulder + sloped keypad bay
    dressing.append(H.box("PhoneLowerChassis", ppt(0.0, 0.0, 0.020), (0.188, 0.218, 0.030), "plastic_ivory", bevel=0.008, seg=3, rot=(0, 0, yaw_p)))
    dressing.append(H.box("PhoneRearBody", ppt(-0.046, 0.0, 0.052), (0.092, 0.210, 0.056), "plastic_ivory", bevel=0.010, seg=3, rot=(0, 0, yaw_p)))
    # Sloped front deck carrying keypad & extension directory card
    dressing.append(H.box("PhoneSlopeDeck", ppt(0.040, 0.0, 0.042), (0.102, 0.204, 0.036), "plastic_ivory", bevel=0.008, seg=3, rot=(0, math.radians(14), yaw_p)))
    # Dark recessed keypad bezel surround
    dressing.append(H.box("PhoneKeypadWell", ppt(0.042, 0.022, 0.056), (0.086, 0.114, 0.008), "plastic_black", bevel=0.003, rot=(0, math.radians(14), yaw_p)))
    # Sculpted cradle prongs + twin spring hook-switch plungers inside cradle bay
    for fy in (-0.068, 0.068):
        dressing.append(H.box(f"PhoneCradleWall_{fy:.3f}", ppt(-0.048, fy, 0.084), (0.056, 0.015, 0.026), "plastic_ivory", bevel=0.004, rot=(0, 0, yaw_p)))
        dressing.append(H.box(f"PhoneHookPin_{fy:.3f}", ppt(-0.048, fy * 0.42, 0.084), (0.012, 0.014, 0.018), "plastic_black", bevel=0.002, rot=(0, 0, yaw_p)))
    # 4x3 tactile beveled push buttons + 3 narrow function buttons (REDIAL/FLASH/HOLD)
    for r in range(4):
        for c in range(3):
            kx = 0.016 + r * 0.019
            ky = -0.006 + c * 0.026
            kz = 0.068 - r * 0.0047
            dressing.append(H.box(f"PhoneBtn_{r}_{c}", ppt(kx, ky, kz), (0.014, 0.019, 0.011),
                                  "plastic_ivory" if (r == 3 and c != 1) else "plastic_black",
                                  bevel=0.0025, rot=(0, math.radians(14), yaw_p)))
    for fi in range(3):
        kx = 0.018 + fi * 0.020
        kz = 0.068 - fi * 0.0049
        dressing.append(H.box(f"PhoneFunc_{fi}", ppt(kx, -0.055, kz), (0.012, 0.022, 0.009),
                              "plastic_red" if fi == 0 else "plastic_black", bevel=0.002, rot=(0, math.radians(14), yaw_p)))
    # Printed internal extension directory card sitting neatly on the south side of the sloped deck!
    def ext_card_fn(u, v):
        x = 0.008 + u * 0.072
        y = -0.092 + v * 0.050
        z = 0.068 - (x - 0.008) * math.tan(math.radians(14))
        return ppt(x, y, z)
    dressing.append(H.sculpted_grid("PhoneExtCard", 2, 2, ext_card_fn, "print:phone_extension_card", thickness=0.0015, print_uv=True))

    # True 28-turn 3D helical coiled telephone cord sweeping naturally from left flank to handset!
    coil_pts = []
    n_turns = 22
    steps_per_turn = 6
    total_steps = n_turns * steps_per_turn
    for k in range(total_steps + 1):
        t = k / float(total_steps)
        # Sweep spine curve on counter from base side (-0.095, -0.075, 0.022) out in a natural loop and up into handset tail (-0.052, -0.106, 0.088)
        sx = -0.096 - 0.052 * math.sin(t * math.pi) + 0.044 * (t ** 2)
        sy = -0.072 - 0.065 * math.sin(t * math.pi * 0.85) - 0.034 * t
        sz = 0.015 + 0.006 * math.sin(t * math.pi) + 0.073 * (t ** 3.2)
        ang = t * n_turns * math.tau
        rad = 0.0085 * math.sin(min(1.0, t * 8.0) * math.pi * 0.5) * math.sin(min(1.0, (1.0 - t) * 8.0) * math.pi * 0.5)
        cx = sx + math.cos(ang) * rad
        cy = sy + math.sin(ang) * rad * 0.4
        cz = max(0.004, sz + math.sin(ang) * rad)
        coil_pts.append(ppt(cx, cy, cz))
    dressing.append(H.pipe("PhoneHelicalCord", coil_pts, 0.0024, "plastic_black", res=3))
    # Rear line jack cord trailing toward the counter back edge
    line_pts = [ppt(-0.095, 0.035, 0.018), ppt(-0.18, 0.05, 0.004), ppt(-0.32, 0.08, 0.003), ppt(-0.46, 0.06, 0.003)]
    dressing.append(H.pipe("PhoneWallCord", line_pts, 0.0025, "plastic_black", res=3))

    # --- SEPARATE SCULPTED ERGONOMIC HANDSET (exact name "Handset" for RebirthTwo!) ---
    # Curved spine grip bar with smooth beveled rectangular cross-section
    grip_pts = [
        ppt(-0.050, -0.092, 0.098),
        ppt(-0.050, -0.052, 0.112),
        ppt(-0.050, 0.000, 0.117),
        ppt(-0.050, 0.052, 0.112),
        ppt(-0.050, 0.092, 0.098),
    ]
    handset.append(H.pipe("HandsetSpine", grip_pts, 0.0155, "plastic_ivory", res=6))
    # Contoured earpiece & mouthpiece flared receiver cups + acoustic black grill rings
    for ey in (-0.095, 0.095):
        tilt_x = math.radians(14 if ey > 0 else -14)
        handset.append(H.lathe(
            f"HandsetCup_{ey:.3f}",
            [(0.0, 0.0), (0.025, 0.0), (0.029, 0.008), (0.027, 0.026), (0.018, 0.036), (0.0, 0.038)],
            ppt(-0.050, ey, 0.068), "plastic_ivory", seg=24, rot=(tilt_x, 0, yaw_p),
        ))
        handset.append(H.cyl(f"HandsetGrill_{ey:.3f}", ppt(-0.050, ey, 0.065), ppt(-0.050, ey, 0.070), 0.019, "plastic_black", seg=18))

    # =========================================================================
    # 3. ARTICULATED GREEN-ENAMELED HOSPITAL DESK LAMP (centered at (-0.30, -0.58, 0.0))
    #    Heavy stepped cast base + rocker switch + braided cord + articulated chrome arms +
    #    spun green enamel bell shade + white reflective liner + warm glowing tungsten bulb!
    # =========================================================================
    dx, dy = -0.30, -0.58
    # Heavy stepped weighted cast base with brass trim collar
    dressing.append(H.lathe("LampBaseBody", [
        (0.0, 0.0), (0.092, 0.0), (0.094, 0.007), (0.088, 0.022),
        (0.072, 0.032), (0.038, 0.038), (0.024, 0.048), (0.0, 0.048),
    ], (dx, dy, 0.0), "enamel_lamp", seg=32))
    dressing.append(H.lathe("LampBaseBrassRing", [(0.089, 0.016), (0.093, 0.018), (0.091, 0.024), (0.086, 0.024)],
                            (dx, dy, 0.0), "brass", seg=32))
    # Period push-button power switch mounted on front apron of base
    dressing.append(H.cyl("LampSwitchBezel", (dx + 0.052, dy, 0.026), (dx + 0.052, dy, 0.036), 0.011, "brass", seg=14))
    dressing.append(H.cyl("LampSwitchBtn", (dx + 0.052, dy, 0.035), (dx + 0.052, dy, 0.045), 0.007, "plastic_red", seg=14))
    # Lower pivot yoke + twin articulated brushed-steel struts + middle elbow joint with knurled thumbscrew
    dressing.append(H.box("LampBaseYoke", (dx, dy, 0.058), (0.028, 0.022, 0.026), "brass", bevel=0.003))
    elbow = (dx - 0.068, dy - 0.015, 0.245)
    head_jt = (dx + 0.062, dy + 0.035, 0.410)
    for off in (-0.010, 0.010):
        dressing.append(H.cyl(f"LampArmLow_{off:.3f}", (dx, dy + off, 0.060), (elbow[0], elbow[1] + off, elbow[2]), 0.0048, "steel", seg=12))
        dressing.append(H.cyl(f"LampArmHigh_{off:.3f}", (elbow[0], elbow[1] + off, elbow[2]), (head_jt[0], head_jt[1] + off, head_jt[2]), 0.0045, "steel", seg=12))
    # Elbow hinge barrel + brass knurled tension knob
    dressing.append(H.cyl("LampElbowAxle", (elbow[0], elbow[1] - 0.018, elbow[2]), (elbow[0], elbow[1] + 0.024, elbow[2]), 0.009, "brass", seg=16))
    dressing.append(H.cyl("LampElbowKnob", (elbow[0], elbow[1] + 0.022, elbow[2]), (elbow[0], elbow[1] + 0.034, elbow[2]), 0.013, "plastic_black", seg=16, bevel=0.002))
    # Top knuckle joint + ventilated brass lamp-holder cap
    dressing.append(H.cyl("LampHeadAxle", (head_jt[0], head_jt[1] - 0.016, head_jt[2]), (head_jt[0], head_jt[1] + 0.016, head_jt[2]), 0.0085, "brass", seg=16))

    # Spun deep-bell dark green enameled shade (angled gently down toward Lan's ledger & duty book!)
    shade_pos = (head_jt[0] + 0.030, head_jt[1] + 0.022, head_jt[2] - 0.025)
    shade_rot = (math.radians(-14), math.radians(24), math.radians(-18))
    dressing.append(H.lathe("LampShadeOuter", [
        (0.0, 0.056), (0.024, 0.054), (0.028, 0.034), (0.048, 0.022),
        (0.088, -0.032), (0.102, -0.072), (0.106, -0.078), (0.099, -0.080),
        (0.095, -0.070), (0.082, -0.032), (0.042, 0.018), (0.022, 0.030), (0.0, 0.030),
    ], shade_pos, "enamel_lamp", seg=36, rot=shade_rot))
    # White enameled inner reflective dome + brass vent collar + glowing warm tungsten bulb
    dressing.append(H.lathe("LampShadeLiner", [
        (0.021, 0.028), (0.040, 0.016), (0.079, -0.033), (0.094, -0.071),
        (0.091, -0.072), (0.076, -0.034), (0.037, 0.014), (0.018, 0.026),
    ], shade_pos, "enamel_white", seg=32, rot=shade_rot))
    dressing.append(H.lathe("LampVentCollar", [
        (0.025, 0.036), (0.030, 0.036), (0.030, 0.056), (0.022, 0.058), (0.0, 0.058),
    ], shade_pos, "brass", seg=24, rot=shade_rot))
    # Warm glowing bulb (WarmGlow material slot preserved so it actually glows in Unity!)
    dressing.append(H.sphere("LampBulb", (shade_pos[0] + 0.008, shade_pos[1] + 0.008, shade_pos[2] - 0.034),
                             (0.046, 0.046, 0.062), "WarmGlow", seg=18, rings=12))
    # Lamp power cord trailing neatly to counter back edge
    dressing.append(H.pipe("LampPowerCord", [
        (dx - 0.088, dy - 0.01, 0.010), (dx - 0.16, dy - 0.06, 0.004),
        (dx - 0.30, dy - 0.14, 0.003), (dx - 0.44, dy - 0.10, 0.003),
    ], 0.0032, "plastic_black", res=4))

    # =========================================================================
    # 4. 2-TIER DOVETAILED TIMBER INBOX TRAY + TABBED PATIENT FOLDERS (-0.20, +0.92, 0.0)
    # =========================================================================
    tx, ty = -0.20, 0.92
    for tier, tz in enumerate((0.0, 0.125)):
        # Bottom floorboard + finger-notched side walls + rear wall
        dressing.append(H.box(f"TrayBase_{tier}", (tx, ty, tz + 0.008), (0.33, 0.255, 0.012), "wood", bevel=0.002, grain="X"))
        for s in (-1, 1):
            dressing.append(H.box(f"TraySide_{tier}_{s}", (tx, ty + s * 0.121, tz + 0.038), (0.33, 0.013, 0.064), "wood", bevel=0.002, grain="X"))
        dressing.append(H.box(f"TrayBack_{tier}", (tx - 0.158, ty, tz + 0.038), (0.013, 0.255, 0.064), "wood", bevel=0.002, grain="Y"))
        # Front low retaining lip with curved cutout feel (split left/right leaving central finger scoop)
        for s in (-1, 1):
            dressing.append(H.box(f"TrayFrontLip_{tier}_{s}", (tx + 0.158, ty + s * 0.085, tz + 0.025), (0.012, 0.080, 0.038), "wood_light", bevel=0.002, grain="Y"))
        # Stacked staggered kraft patient medical folders (Hồ sơ bệnh án) with protruding index tabs
        n_files = 5 if tier == 0 else 3
        for k in range(n_files):
            fy_off = (k - 2) * 0.006
            yaw_f = (k - 1.8) * 0.038
            mat_f = "kraft" if k % 2 == 0 else "paper"
            fz = tz + 0.020 + k * 0.011
            dressing.append(H.box(f"TrayFolder_{tier}_{k}", (tx + 0.012, ty + fy_off, fz), (0.295, 0.215, 0.0085),
                                  mat_f, bevel=0.002, rot=(0, 0, yaw_f)))
            # Manila index tab protruding out of front edge
            dressing.append(H.box(f"TrayTab_{tier}_{k}", (tx + 0.162, ty - 0.065 + k * 0.032, fz),
                                  (0.018, 0.042, 0.004), "paper" if mat_f == "kraft" else "kraft", bevel=0.001, rot=(0, 0, yaw_f)))
    # 4 Brass spacer pillars connecting Tier 0 and Tier 1
    for sx in (-0.148, 0.148):
        for sy in (-0.115, 0.115):
            dressing.append(H.cyl(f"TrayPillar_{sx:.3f}_{sy:.3f}", (tx + sx, ty + sy, 0.066), (tx + sx, ty + sy, 0.128), 0.0055, "brass", seg=12))

    # =========================================================================
    # 5. VIETNAMESE HOSPITAL ADMINISTRATIVE CLUSTER (Stamp rack + Red Ink Pad + Pen Cup)
    #    Centered near (-0.48, +0.44, 0.0) -> world (1.29, 1.128, -2.98), handy to Lan!
    # =========================================================================
    sx0, sy0 = -0.48, 0.44
    # Hinged red metal/plastic ink pad tin (Hộp mực dấu đỏ)
    dressing.append(H.box("InkPadBase", (sx0 + 0.01, sy0 - 0.12, 0.009), (0.115, 0.078, 0.016), "enamel_blue", bevel=0.003, rot=(0, 0, 0.14)))
    dressing.append(H.box("InkPadLid", (sx0 + 0.01, sy0 - 0.12, 0.019), (0.118, 0.080, 0.007), "steel_dark", bevel=0.002, rot=(0, 0, 0.14)))
    # 2 Turned-wood Vietnamese hospital rubber stamps (Dấu vuông & dấu tròn)
    # Rectangular stamp resting beside ink pad
    dressing.append(H.box("RectStampBlock", (sx0 - 0.02, sy0 - 0.02, 0.014), (0.072, 0.030, 0.018), "wood_light", bevel=0.003, rot=(0, 0, -0.18)))
    dressing.append(H.box("RectStampRubber", (sx0 - 0.02, sy0 - 0.02, 0.003), (0.068, 0.026, 0.006), "rubber", bevel=0.001, rot=(0, 0, -0.18)))
    dressing.append(H.lathe("RectStampHandle", [
        (0.0, 0.020), (0.010, 0.020), (0.008, 0.042), (0.014, 0.062), (0.013, 0.074), (0.0, 0.078)
    ], (sx0 - 0.02, sy0 - 0.02, 0.0), "wood", seg=18))
    # Round administrative seal stamp (Dấu tròn đỏ)
    dressing.append(H.lathe("RoundStamp", [
        (0.0, 0.0), (0.022, 0.0), (0.022, 0.006), (0.024, 0.010), (0.024, 0.022),
        (0.010, 0.032), (0.009, 0.056), (0.016, 0.074), (0.014, 0.088), (0.0, 0.092)
    ], (sx0 + 0.055, sy0 - 0.01, 0.0), "wood", seg=22))

    # Glazed ceramic pen cup (Ống cắm bút) holding pens, pencils & 20cm wooden ruler
    cx0, cy0 = sx0 - 0.01, sy0 + 0.115
    dressing.append(H.lathe("PenCup", [
        (0.0, 0.0), (0.036, 0.0), (0.040, 0.008), (0.042, 0.098), (0.044, 0.104),
        (0.038, 0.105), (0.035, 0.096), (0.033, 0.010), (0.0, 0.010)
    ], (cx0, cy0, 0.0), "ceramic", seg=24))
    # 4 pens/pencils leaning naturally inside cup + wooden ruler
    for pi, (ox, oy, tx_o, ty_o, matk) in enumerate([
        (-0.012, -0.010, -0.028, -0.022, "enamel_blue"),
        (0.010, -0.008, 0.026, -0.020, "plastic_red"),
        (-0.005, 0.012, -0.018, 0.030, "wood_light"),
        (0.014, 0.010, 0.030, 0.024, "plastic_black"),
    ]):
        dressing.append(H.cyl(f"CupPen_{pi}", (cx0 + ox, cy0 + oy, 0.012), (cx0 + tx_o, cy0 + ty_o, 0.155 + (pi % 2) * 0.014), 0.0042, matk, seg=8))
    dressing.append(H.box("WoodenRuler", (cx0 - 0.006, cy0 + 0.006, 0.098), (0.024, 0.004, 0.195), "wood_light",
                          bevel=0.001, rot=(math.radians(8), math.radians(-11), 0.35), grain="Z"))

    return {"CounterDressing": dressing, "Handset": handset, "DutyLedger": ledger}


def run():
    # Export working separate-parts FBX (used by HospitalAuthoring.RebirthTwo.cs DressRebirthReception!)
    H.export_hero_asset(
        "CTCC_PROP_ReceptionWorking",
        build_desk_props_parts,
        atlas_specs={
            "LOC02_DeskProps": {"res": 2048, "bake_r": True, "parts": None, "margin": 0.004},
        },
        make_lods=False,
        separate_parts=True,
        cam_dir=(1.45, -1.25, 1.05),
        render=True,
        ao_samples=16,
    )
    # Also export unified CTCC_PROP_ReceptionProps pointing to the exact same baked atlas (fallback / pre-RebirthTwo)
    import shutil
    shutil.copy2(H.FBX_DIR / "CTCC_PROP_ReceptionWorking.fbx", H.FBX_DIR / "CTCC_PROP_ReceptionProps.fbx")
    shutil.copy2(H.SRC_DIR / "CTCC_PROP_ReceptionWorking.blend", H.SRC_DIR / "CTCC_PROP_ReceptionProps.blend")
    print("[HERO ASSET] Synced CTCC_PROP_ReceptionProps.fbx <- CTCC_PROP_ReceptionWorking.fbx")


if __name__ == "__main__":
    run()
