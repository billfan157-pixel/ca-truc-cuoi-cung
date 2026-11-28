"""LOC01 Landscape, Vehicle & Exterior Props Kit B — Sculpted Motorbike, Campus Directory Kiosk,
Curved Terrazzo Bench, Cast-Iron Monsoon Drain Grate, Raised Brick Planter & Wet Soil Mound,
Walkway Curb Run, Centrifugal Utility Pole & Cobra Streetlamp, Yard Storytelling Props &
Buttress-Root Tropical Courtyard Tree.

All 9 assets built procedurally in Blender 4.5 LTS with Cycles multi-pass PBR baking
(B 2005 operational <-> R 2012 monsoon-weathered), LOD0-2 FBX, and editable .blend sources
saved to ArtSource/Blender/LOC01/.
"""
from __future__ import annotations

import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))
sys.path.insert(0, str(HERE.parent / "loc02"))

import common as C  # noqa: E402
import hero_lib as H  # noqa: E402
import loc01_lib as L  # noqa: E402


# --------------------------------------------------------------------------------------
# 1. Minh's Sculpted Super-Dream-Era Vietnamese Underbone Motorbike (CTCC_PROP_CommuterMotorbike)
#    Modeled directly from user's reference silhouette (100% fictional AR03 branding 'SUPER 100')!
# --------------------------------------------------------------------------------------

def _make_wheel(objs, prefix, y_pos, r_outer=0.295, r_rim=0.228, tire_w=0.062):
    """Slender 17-inch bright chrome wire-spoked motorbike wheel (32 crossed chrome spokes,
    large cast-silver aluminum drum brake hub, sculpted low-profile commuter tire)."""
    cz = r_outer
    # Sculpted toroidal tire via lathe rotated 90° around Y (axle along X)
    tire_prof = []
    segs_p = 14
    r_tube = (r_outer - r_rim) * 0.56
    r_mid = (r_outer + r_rim) * 0.50
    for i in range(segs_p + 1):
        a = i * math.tau / segs_p
        rr = r_mid + math.cos(a) * r_tube
        hh = math.sin(a) * (tire_w * 0.5)
        tire_prof.append((rr, hh))
    objs.append(H.lathe(f"{prefix}_Tire", tire_prof, (0.0, y_pos, cz), "ext_moto_tire",
                        seg=36, rot=(0.0, math.radians(90), 0.0)))
    # Bright polished chrome concave 17" wheel rim
    rim_prof = [
        (r_rim - 0.018, -0.022), (r_rim + 0.005, -0.024), (r_rim + 0.006, -0.018),
        (r_rim - 0.012, 0.0),
        (r_rim + 0.006, 0.018), (r_rim + 0.005, 0.024), (r_rim - 0.018, 0.022),
    ]
    objs.append(H.lathe(f"{prefix}_Rim", rim_prof, (0.0, y_pos, cz), "ext_moto_chrome",
                        seg=32, rot=(0.0, math.radians(90), 0.0)))
    # Large bright cast-silver aluminum brake drum hub (prominent visual feature of Dream wheels!)
    objs.append(H.cyl(f"{prefix}_Hub", (-0.056, y_pos, cz), (0.056, y_pos, cz), 0.072, "ext_moto_alu", seg=22))
    objs.append(H.cyl(f"{prefix}_HubFlangeL", (-0.044, y_pos, cz), (-0.036, y_pos, cz), 0.079, "ext_moto_alu", seg=22))
    objs.append(H.cyl(f"{prefix}_HubFlangeR", (0.036, y_pos, cz), (0.044, y_pos, cz), 0.079, "ext_moto_alu", seg=22))
    objs.append(H.cyl(f"{prefix}_Axle", (-0.088, y_pos, cz), (0.088, y_pos, cz), 0.016, "ext_moto_chrome", seg=12))
    # 28 crisp criss-crossed chrome wire spokes
    for k in range(28):
        ang = k * math.tau / 28.0
        side = -0.038 if (k % 2 == 0) else 0.038
        ang2 = ang + (0.34 if k % 2 == 0 else -0.34)
        p0 = (side, y_pos + math.cos(ang) * 0.070, cz + math.sin(ang) * 0.070)
        p1 = (0.0, y_pos + math.cos(ang2) * (r_rim - 0.014), cz + math.sin(ang2) * (r_rim - 0.014))
        objs.append(H.cyl(f"{prefix}_Spoke_{k}", p0, p1, 0.0024, "ext_moto_chrome", seg=5))


def build_commuter_motorbike_hero():
    """Museum-grade early-2000s Vietnamese Super-Dream-era underbone commuter motorbike
    modeled directly from the user's reference image (100% fictional 'SUPER 100' branding per AR03!).
    Forward = +Y (front axle y=+0.63, rear axle y=-0.60, wheelbase 1.23m, total length 1.94m)."""
    objs = []
    decal_objs = []
    # 1. Slender 17" 28-Spoke Wire Wheels w/ Bright Cast-Silver Drum Hubs
    _make_wheel(objs, "FrontW", 0.63, r_outer=0.295, r_rim=0.228, tire_w=0.060)
    _make_wheel(objs, "RearW", -0.60, r_outer=0.295, r_rim=0.228, tire_w=0.068)

    # 2. Bright Silver Telescopic Front Forks, Brake Cables, Sculpted Plum-Maroon Fender & Chrome Front Rack (baga mũi)
    for sx in (-0.068, 0.068):
        # Silver cast aluminum lower fork leg + upper chrome stanchion (26° classic Dream steering rake!)
        objs.append(H.cyl(f"ForkLower_{sx:+.2f}", (sx, 0.63, 0.295), (sx, 0.49, 0.595), 0.023, "ext_moto_alu", seg=14))
        objs.append(H.cyl(f"ForkSeal_{sx:+.2f}", (sx, 0.495, 0.585), (sx, 0.482, 0.615), 0.025, "ext_moto_leather", seg=12))
        objs.append(H.cyl(f"ForkUpper_{sx:+.2f}", (sx, 0.485, 0.605), (sx, 0.355, 0.905), 0.016, "ext_moto_chrome", seg=12))
    # Curved black front brake & speedometer cables hugging right fork leg down to drum hub
    objs.append(H.pipe("FrontBrakeCable", [(0.055, 0.42, 0.72), (0.095, 0.47, 0.46), (0.072, 0.57, 0.31)], 0.005, "ext_moto_leather", res=3))
    objs.append(H.pipe("FrontSpeedoCable", [(0.048, 0.41, 0.74), (0.088, 0.52, 0.42), (0.068, 0.60, 0.30)], 0.0045, "ext_moto_leather", res=3))

    # Sculpted Deep Plum-Maroon Front Fender wrapping tightly over front tire + flared fork skirts!
    fender_pts = []
    for k in range(13):
        a = math.radians(24 + k * 9.5)
        fender_pts.append((0.0, 0.63 + math.cos(a) * 0.316, 0.295 + math.sin(a) * 0.316))
    fender_prof = [(-0.068, -0.022), (-0.055, 0.012), (0.0, 0.024), (0.055, 0.012), (0.068, -0.022)]
    objs.append(H.sweep("FrontFender", fender_prof, fender_pts, "ext_moto_maroon", closed=False, caps=False, up=(0, 0, 1)))
    # Flared plum-maroon upper fork-crown shoulder cheeks covering the lower stem behind front fender
    for sgn in (-1.0, 1.0):
        objs.append(H.box(f"ForkSkirt_{sgn:+.0f}", (sgn * 0.072, 0.495, 0.575), (0.036, 0.145, 0.165),
                          "ext_moto_maroon", bevel=0.014, seg=3, rot=(math.radians(-24), 0, 0)))

    # Iconic Chrome Tubular Front Fender Luggage/Basket Rack (baga mũi Dream!)
    objs.append(H.pipe("FrontRackRim", [
        (-0.085, 0.54, 0.655), (-0.095, 0.78, 0.665), (-0.055, 0.84, 0.668),
        (0.055, 0.84, 0.668), (0.095, 0.78, 0.665), (0.085, 0.54, 0.655),
    ], 0.0065, "ext_moto_chrome", res=4))
    for rx in (-0.045, 0.0, 0.045):
        objs.append(H.cyl(f"FrontRackBar_{rx:+.2f}", (rx, 0.545, 0.655), (rx, 0.835, 0.667), 0.0045, "ext_moto_chrome", seg=8))
    for sgn in (-1.0, 1.0):
        objs.append(H.cyl(f"FrontRackStay_{sgn:+.0f}", (sgn * 0.082, 0.74, 0.660), (sgn * 0.075, 0.51, 0.595), 0.0055, "ext_moto_chrome", seg=8))
    # Black vertical front basket clamp stem right at base of nose apron
    objs.append(H.box("FrontRackClamp", (0.0, 0.535, 0.705), (0.028, 0.022, 0.115), "ext_moto_exhaust",
                      bevel=0.004, rot=(math.radians(-22), 0, 0)))

    # 3. Sculpted Continuous Inverted-J / S-Curve Pearl-White Leg Shield ('yếm xe Dream') & Plum-Maroon Front Nose ('mặt nạ')
    # Central plum-maroon nose fairing panel ('mặt nạ') sitting cleanly IN FRONT of upper white leg shield!
    objs.append(H.box("FrontNoseApron", (0.0, 0.445, 0.745), (0.145, 0.135, 0.34), "ext_moto_maroon",
                      bevel=0.032, seg=3, rot=(math.radians(-24), 0, 0)))
    objs.append(H.box("FrontEmblemShield", (0.0, 0.502, 0.818), (0.076, 0.018, 0.092), "ext_moto_chrome",
                      bevel=0.008, rot=(math.radians(-24), 0, 0)))
    objs.append(H.box("FrontEmblemCore", (0.0, 0.510, 0.818), (0.054, 0.008, 0.066), "ext_moto_maroon",
                      bevel=0.004, rot=(math.radians(-24), 0, 0)))

    # TRUE SUPER-DREAM SIDE SILHOUETTE LEG SHIELD (slim along upper fork -> sweeping gracefully back in an
    # unmistakable deep concave U-curve below the step-through throat, tapering into the pointed belly chin!)
    for side, sgn in (("L", -1.0), ("R", 1.0)):
        def dream_shield_fn(u, v, s=sgn):
            # v in [0..1]: 0 = bottom belly tip (z=0.22), 1 = upper neck collar (z=0.87)
            # u in [0..1]: 0 = sleek front edge hugging the 24° fork, 1 = concave S-curved rear trailing edge!
            # Front edge runs straight-and-sleek along fork tube (never bulges fatly forward!)
            z_front = 0.22 + v * 0.65
            y_front = 0.24 + v * 0.22 + 0.035 * math.sin(v * math.pi)
            # Rear edge traces the EXACT iconic Dream J-curve:
            #   v=0.00 -> chin point (y=+0.22, z=0.22)
            #   v=0.25 -> sweeps BACK past engine crankcase to (y=+0.06, z=0.35)
            #   v=0.45 -> sweeps FARTHEST BACK to deep U-throat at (y=-0.05, z=0.46)
            #   v=0.65 -> curves sharply FORWARD & UP along knee wing (y=+0.24, z=0.64)
            #   v=1.00 -> slender neck collar behind stem (y=+0.34, z=0.87)
            if v < 0.44:
                t = v / 0.44
                y_rear = 0.22 - 0.27 * (t ** 0.85)
                z_rear = 0.22 + 0.25 * t
            else:
                t = (v - 0.44) / 0.56
                # Sharp graceful concave scoop rising up the steering neck!
                y_rear = -0.05 + 0.39 * (t ** 0.52)
                z_rear = 0.47 + 0.40 * t
            y = y_front * (1.0 - u) + y_rear * u
            z = z_front * (1.0 - u) + z_rear * u
            # Contoured outward knee flare: slim at top neck (0.11m), flared cleanly across mid-wings (0.195m), tucked at chin (0.095m)
            wing_w = 0.095 + 0.105 * math.sin(v * math.pi) * (0.45 + 0.55 * math.sin(u * math.pi * 0.8))
            x_inner = 0.058 + 0.015 * (1.0 - v)
            x = s * (x_inner + (wing_w - x_inner) * (u ** 0.65))
            return (x, y, z)
        objs.append(H.sculpted_grid(f"DreamLegShield_{side}", 12, 18, dream_shield_fn, "ext_moto_ivory", thickness=0.011))
        # Iconic black recessed triangular engine air-intake duct clearly visible on the lower white side wing!
        objs.append(H.box(f"ShieldVent_{side}", (sgn * 0.148, 0.205, 0.425), (0.024, 0.092, 0.038),
                          "ext_moto_exhaust", bevel=0.008, rot=(math.radians(-14), 0, math.radians(sgn * -16))))
        objs.append(H.cyl(f"ShieldBolt_{side}", (sgn * 0.115, 0.165, 0.285), (sgn * 0.145, 0.165, 0.285),
                          0.0065, "ext_moto_chrome", seg=8))
    # Lower white chin belly bridge joining left & right leg shields cleanly beneath cylinder head
    objs.append(H.box("LegShieldChin", (0.0, 0.235, 0.230), (0.135, 0.095, 0.048), "ext_moto_ivory",
                      bevel=0.014, rot=(math.radians(-18), 0, 0)))

    # 4. Sleek Plum-Maroon Step-Through Frame, Tapered Side Covers ('cốp xe') w/ Flush Decals & Swept Tail ('đuôi xe')
    # Deep graceful U-shaped step-through spine bridging back of white shield into mid body!
    objs.append(H.box("StepThroughBridge", (0.0, -0.02, 0.475), (0.148, 0.26, 0.082), "ext_moto_maroon",
                      bevel=0.024, seg=3, rot=(math.radians(-12), 0, 0)))
    # Sculpted plum-maroon mid fuselage ('cốp xe' + under-seat frame) tapering smoothly back to tail lamp!
    mid_pts = [
        (0.0, 0.02, 0.505),
        (0.0, -0.14, 0.565),
        (0.0, -0.38, 0.595),
        (0.0, -0.60, 0.655),
        (0.0, -0.85, 0.708),
    ]
    mid_prof = [
        (-0.070, -0.092), (-0.108, -0.045), (-0.114, 0.032), (-0.092, 0.082), (0.0, 0.092),
        (0.092, 0.082), (0.114, 0.032), (0.108, -0.045), (0.070, -0.092),
    ]
    objs.append(H.sweep("DreamMainBody", mid_prof, mid_pts, "ext_moto_maroon", closed=True, caps=True, up=(0, 0, 1)))
    # Smoothly blended lower plum-maroon forward side fairing hugging under-seat frame
    for sgn in (-1.0, 1.0):
        objs.append(H.box(f"LowerSideSkirt_{sgn:+.0f}", (sgn * 0.082, -0.22, 0.485), (0.042, 0.34, 0.115),
                          "ext_moto_maroon", bevel=0.018, seg=3, rot=(math.radians(10), 0, 0)))

    # Flush Curved Period Gold/Ivory/Crimson Side Graphics ('SUPER 100') conforming tightly to body curvature!
    for side, sgn in (("L", -1.0), ("R", 1.0)):
        # Mid cover contoured decal panel hugging DreamMainBody side (y in [-0.46..-0.10], z in [0.49..0.64])
        def mid_decal_fn(u, v, s=sgn):
            # u in [0..1] rear -> front along y, v in [0..1] bottom -> top along z
            y = -0.45 + u * 0.35
            z_bot = 0.515 - u * 0.055
            z_top = 0.642 - u * 0.025
            z = z_bot + v * (z_top - z_bot)
            x = s * (0.1155 + 0.004 * math.sin(v * math.pi))
            return (x, y, z)
        mid_dec = H.sculpted_grid(f"SideCoverDecal_{side}", 8, 6, mid_decal_fn, "print:motorbike_decal",
                                  thickness=0.002, print_uv=True, uv_rect=(0.0, 0.5, 1.0, 1.0))
        if sgn < 0:
            H.set_planar_print_uv(mid_dec, axis="X", uv_rect=(0.0, 0.5, 1.0, 1.0), flip_u=True)
        decal_objs.append(mid_dec)

        # Rear tail flush contoured crimson/ivory stripe (y in [-0.82..-0.54], z in [0.65..0.72])
        def tail_decal_fn(u, v, s=sgn):
            y = -0.82 + u * 0.28
            z_bot = 0.665 - u * 0.022
            z_top = 0.724 - u * 0.018
            z = z_bot + v * (z_top - z_bot)
            x = s * (0.112 + 0.003 * math.sin(v * math.pi))
            return (x, y, z)
        tail_dec = H.sculpted_grid(f"TailStripeDecal_{side}", 6, 4, tail_decal_fn, "print:motorbike_decal",
                                   thickness=0.002, print_uv=True, uv_rect=(0.0, 0.0, 1.0, 0.5))
        if sgn < 0:
            H.set_planar_print_uv(tail_dec, axis="X", uv_rect=(0.0, 0.0, 1.0, 0.5), flip_u=True)
        decal_objs.append(tail_dec)

    # Flush Wrap-Around Taillight Assembly (ruby-red center brake lens + clear side blinkers)
    objs.append(H.box("TaillightRed", (0.0, -0.875, 0.692), (0.146, 0.040, 0.068), "ext_red_lens", bevel=0.012))
    objs.append(H.box("TaillightGlow", (0.0, -0.892, 0.694), (0.086, 0.014, 0.036), "CrimsonGlow", bevel=0.005))
    for sgn in (-1.0, 1.0):
        objs.append(H.box(f"RearBlinker_{sgn:+.0f}", (sgn * 0.088, -0.858, 0.678), (0.044, 0.042, 0.048), "Glass", bevel=0.008))

    # Sleek Curved Swept Black Rear Splash Mudguard ('dè sau' arcing organically down around rear tire!) + Plate!
    rmud_pts = []
    for k in range(10):
        a = math.radians(118 + k * 7.8)
        r_arc = 0.325 + max(0.0, (k - 5) * 0.018)
        rmud_pts.append((0.0, -0.60 + math.cos(a) * r_arc, 0.295 + math.sin(a) * r_arc))
    rmud_prof = [(-0.072, -0.012), (-0.056, 0.010), (0.0, 0.016), (0.056, 0.010), (0.072, -0.012)]
    objs.append(H.sweep("RearMudguard", rmud_prof, rmud_pts, "ext_moto_exhaust", closed=False, caps=False, up=(0, 0, 1)))
    plate = H.box("LicensePlate", (0.0, -0.922, 0.495), (0.168, 0.008, 0.132), "print:motorbike_details",
                  bevel=0.002, rot=(math.radians(32), 0, 0))
    H.set_planar_print_uv(plate, axis="Y", uv_rect=(0.0, 0.0, 0.5, 1.0), flip_u=True)
    decal_objs.append(plate)

    # 5. Iconic Super-Dream Contoured Dual Saddle (plump bulbous rounded front nose + sleek low profile!)
    #    & Signature Wrap-Around Chrome Rear Grab-Bar ('tay dắt inox')
    def seat_fn(u, v):
        # u in [0..1] around cross-section (-1..+1 X), v in [0..1] front nose (y=+0.05) -> rear tail (y=-0.78)
        y = 0.05 - v * 0.83
        ang = (u - 0.5) * math.pi * 1.48
        w_half = 0.118 + 0.022 * math.sin(v * math.pi) - 0.026 * max(0.0, (0.15 - v) / 0.15)
        z_base = 0.652 + v * 0.090
        # Plump rounded Dream front nose swelling smoothly up from the step-through notch!
        nose_round = math.sin(min(1.0, v / 0.16) * math.pi * 0.5) ** 0.72
        tail_round = math.sin(min(1.0, (1.0 - v) / 0.10) * math.pi * 0.5) ** 0.78
        h_crown = (0.112 - 0.012 * math.sin(v * math.pi)) * nose_round * tail_round
        x = math.sin(ang) * w_half
        z = z_base + max(0.0, math.cos(ang)) * h_crown
        return (x, y, z)
    objs.append(H.sculpted_grid("DreamDualSaddle", 16, 20, seat_fn, "ext_moto_leather", thickness=0.018))
    # Subtle closed seat bottom pan so no hollow edge shows from low angle
    objs.append(H.box("SeatBasePan", (0.0, -0.38, 0.695), (0.21, 0.74, 0.045), "ext_moto_exhaust",
                      bevel=0.012, rot=(math.radians(-6), 0, 0)))

    # Iconic Chrome Tubular Wrap-Around Rear Grab-Bar ('tay dắt inox' hugging rear sides & rising into rear loop!)
    objs.append(H.pipe("DreamGrabBar", [
        (-0.118, -0.46, 0.705), (-0.128, -0.62, 0.725), (-0.124, -0.77, 0.745),
        (-0.088, -0.87, 0.822), (0.0, -0.895, 0.842), (0.088, -0.87, 0.822),
        (0.124, -0.77, 0.745), (0.128, -0.62, 0.725), (0.118, -0.46, 0.705),
    ], 0.0115, "ext_moto_chrome", res=5))

    # 6. Aerodynamic Plum-Maroon Handlebar Headlight Cowl ('đầu đèn'), Wrap-Around Clear Headlamp & Tall Black Mirrors
    # Steering neck shroud joining cowl cleanly to white leg shield
    objs.append(H.cyl("SteeringNeckCover", (0.0, 0.36, 0.845), (0.0, 0.33, 0.965), 0.068, "ext_moto_maroon", seg=16, r2=0.062))
    # Swept sculpted plum-maroon headlight nacelle
    objs.append(H.box("HeadlightCowlMain", (0.0, 0.345, 0.995), (0.265, 0.215, 0.135), "ext_moto_maroon",
                      bevel=0.042, seg=3, rot=(math.radians(-12), 0, 0)))
    objs.append(H.box("HeadlightVisorTop", (0.0, 0.365, 1.045), (0.225, 0.165, 0.042), "ext_moto_maroon",
                      bevel=0.016, seg=3, rot=(math.radians(-16), 0, 0)))
    # Large crystal-clear wrap-around trapezoidal headlamp lens + bright chrome reflector rim + side flush clear turn signals!
    objs.append(H.box("HeadlampBezel", (0.0, 0.448, 0.992), (0.175, 0.028, 0.105), "ext_moto_chrome", bevel=0.010, rot=(math.radians(-10), 0, 0)))
    objs.append(H.box("HeadlampGlass", (0.0, 0.462, 0.992), (0.155, 0.022, 0.088), "WarmGlow", bevel=0.008, rot=(math.radians(-10), 0, 0)))
    for sgn in (-1.0, 1.0):
        objs.append(H.box(f"FrontBlinkerLens_{sgn:+.0f}", (sgn * 0.118, 0.425, 0.985), (0.048, 0.072, 0.068),
                          "Glass", bevel=0.010, rot=(math.radians(-8), 0, math.radians(sgn * -24))))
        objs.append(H.sphere(f"FrontBlinkerBulb_{sgn:+.0f}", (sgn * 0.106, 0.415, 0.985), (0.024, 0.024, 0.024), "ext_amber_lens", seg=8, rings=6))
    # Recessed Speedometer Instrument Cluster facing rider
    speedo = H.box("SpeedometerDial", (0.0, 0.295, 1.055), (0.168, 0.088, 0.008), "print:motorbike_details",
                   bevel=0.002, rot=(math.radians(34), 0, 0))
    H.set_planar_print_uv(speedo, axis="Z", uv_rect=(0.5, 0.0, 1.0, 1.0))
    decal_objs.append(speedo)

    # Black Switch Pods, Ribbed Rubber Grips, Silver Brake Lever & Tall Black Angled Mirrors (matching reference photo!)
    for sgn in (-1.0, 1.0):
        objs.append(H.cyl(f"HandleBar_{sgn:+.0f}", (sgn * 0.125, 0.325, 0.985), (sgn * 0.325, 0.245, 0.978), 0.014, "ext_moto_exhaust", seg=10))
        objs.append(H.box(f"SwitchPod_{sgn:+.0f}", (sgn * 0.175, 0.305, 0.982), (0.052, 0.052, 0.056), "ext_moto_exhaust", bevel=0.008))
        objs.append(H.cyl(f"HandleGrip_{sgn:+.0f}", (sgn * 0.215, 0.288, 0.980), (sgn * 0.345, 0.236, 0.975), 0.018, "ext_moto_leather", seg=12))
        # Tall sleek black mirror stalks + vertical-angled teardrop/pentagon black mirrors!
        objs.append(H.pipe(f"MirrorStalk_{sgn:+.0f}", [
            (sgn * 0.165, 0.32, 1.025), (sgn * 0.185, 0.315, 1.135), (sgn * 0.225, 0.305, 1.195)
        ], 0.0055, "ext_moto_exhaust", res=4))
        objs.append(H.box(f"MirrorHead_{sgn:+.0f}", (sgn * 0.242, 0.302, 1.235), (0.095, 0.032, 0.105), "ext_moto_exhaust",
                          bevel=0.014, seg=3, rot=(math.radians(10), 0, math.radians(sgn * -16))))
    objs.append(H.pipe("RightBrakeLever", [(0.185, 0.345, 0.975), (0.285, 0.315, 0.965)], 0.006, "ext_moto_alu", res=3))

    # 7. Exposed Cast-Silver Aluminum Engine Crankcase, Horizontal Finned Cylinder, Silver Swingarm,
    #    Passenger Footpeg Bracket, Twin Chrome-Shrouded Shocks & Iconic Black Muffler w/ Chrome Heat Shield!
    # Cast-silver aluminum crankcase & circular right clutch cover (bright visible hero mechanical anchor!)
    objs.append(H.box("EngineCrankcase", (0.0, 0.02, 0.285), (0.195, 0.285, 0.175), "ext_moto_alu", bevel=0.026, seg=3))
    objs.append(H.cyl("ClutchCoverR", (0.065, 0.035, 0.285), (0.118, 0.035, 0.285), 0.088, "ext_moto_alu", seg=22))
    objs.append(H.cyl("ClutchBossR", (0.112, 0.042, 0.285), (0.128, 0.042, 0.285), 0.034, "ext_moto_alu", seg=16))
    objs.append(H.cyl("StatorCoverL", (-0.065, 0.015, 0.285), (-0.115, 0.015, 0.285), 0.082, "ext_moto_alu", seg=20))
    # Horizontal finned cylinder block protruding forward under leg shield
    objs.append(H.cyl("CylinderBlock", (0.0, 0.145, 0.305), (0.0, 0.305, 0.335), 0.062, "ext_moto_alu", seg=16))
    for fi in range(5):
        fy = 0.165 + fi * 0.028
        objs.append(H.box(f"CylFin_{fi}", (0.0, fy, 0.315 + fi * 0.005), (0.135, 0.008, 0.115), "ext_moto_alu", bevel=0.002))
    # Lower chrome frame downtube / footpeg cradle + rider pegs + kickstarter pedal
    objs.append(H.pipe("LowerFrameTube", [(0.0, 0.18, 0.205), (0.0, -0.08, 0.195), (0.0, -0.26, 0.255)], 0.012, "ext_moto_alu", res=4))
    objs.append(H.cyl("RiderFootpegBar", (-0.23, 0.075, 0.215), (0.23, 0.075, 0.215), 0.014, "ext_moto_leather", seg=10))
    objs.append(H.pipe("KickStarter", [(0.11, -0.05, 0.275), (0.145, -0.08, 0.255), (0.165, -0.14, 0.335), (0.125, -0.15, 0.345)],
                       0.0085, "ext_moto_chrome", res=4))

    # Bright Silver Cast-Aluminum Rear Swingarm ('gắp sau') + Triangular Passenger Footpeg Bracket ('gác chân sau')!
    for sgn in (-1.0, 1.0):
        objs.append(H.box(f"Swingarm_{sgn:+.0f}", (sgn * 0.095, -0.37, 0.295), (0.028, 0.48, 0.056), "ext_moto_alu", bevel=0.008))
        # Triangular aluminum passenger footpeg hanger bracket + tilted ribbed black passenger footpeg
        objs.append(H.cyl(f"PassBracketA_{sgn:+.0f}", (sgn * 0.105, -0.18, 0.275), (sgn * 0.148, -0.34, 0.375), 0.013, "ext_moto_alu", seg=10))
        objs.append(H.cyl(f"PassBracketB_{sgn:+.0f}", (sgn * 0.105, -0.44, 0.305), (sgn * 0.148, -0.34, 0.375), 0.012, "ext_moto_alu", seg=10))
        objs.append(H.cyl(f"PassFootpeg_{sgn:+.0f}", (sgn * 0.148, -0.34, 0.375), (sgn * 0.235, -0.375, 0.425), 0.017, "ext_moto_leather", seg=12))
    # Enclosed metallic silver chain guard on left side (-X)
    objs.append(H.box("ChainGuardL", (-0.105, -0.34, 0.298), (0.044, 0.58, 0.135), "ext_moto_alu", bevel=0.014))

    # Twin Signature Chrome-Shrouded Rear Shock Absorbers ('phuộc sau ống inox' + black upper collar!)
    for sgn in (-1.0, 1.0):
        objs.append(H.cyl(f"ShockLowerChrome_{sgn:+.0f}", (sgn * 0.118, -0.575, 0.315), (sgn * 0.118, -0.505, 0.585),
                          0.024, "ext_moto_chrome", seg=16))
        objs.append(H.cyl(f"ShockUpperCap_{sgn:+.0f}", (sgn * 0.118, -0.505, 0.585), (sgn * 0.118, -0.482, 0.695),
                          0.026, "ext_moto_exhaust", seg=16))
        objs.append(H.cyl(f"ShockSpringRing_{sgn:+.0f}", (sgn * 0.118, -0.512, 0.560), (sgn * 0.118, -0.500, 0.605),
                          0.027, "ext_moto_chrome", seg=16))

    # ICONIC SUPER-DREAM EXHAUST: Satin-Black Tapered Megaphone Muffler + Gleaming Bolt-On Chrome Heat Shield ('ốp pô inox')!
    objs.append(H.pipe("ExhaustHeader", [
        (0.025, 0.25, 0.245), (0.095, 0.19, 0.185), (0.135, 0.02, 0.195), (0.148, -0.22, 0.235)
    ], 0.016, "ext_moto_exhaust", res=5))
    objs.append(H.cyl("ExhaustBodyBlack", (0.148, -0.20, 0.238), (0.162, -0.78, 0.312), 0.036, "ext_moto_exhaust", seg=20, r2=0.056))
    objs.append(H.cyl("ExhaustEndCap", (0.162, -0.78, 0.312), (0.164, -0.825, 0.318), 0.054, "ext_moto_exhaust", seg=18, r2=0.030))
    # Gleaming curved brushed-chrome rectangular bolt-on muffler heat shield ('ốp pô inox') w/ 2 visible chrome bolts!
    objs.append(H.box("ExhaustHeatShield", (0.214, -0.565, 0.288), (0.016, 0.34, 0.068), "ext_moto_chrome",
                      bevel=0.008, seg=3, rot=(math.radians(-7.5), 0, 0)))
    for by_s in (-0.435, -0.695):
        bz_s = 0.272 + (-0.435 - by_s) * 0.13
        objs.append(H.cyl(f"ShieldBolt_{by_s:.2f}", (0.214, by_s, bz_s), (0.228, by_s, bz_s), 0.0085, "ext_moto_alu", seg=8))

    # Angled side kickstand seated solidly on ground z=0 on left side (-X)
    objs.append(H.cyl("SideKickstand", (-0.105, -0.05, 0.22), (-0.255, -0.14, 0.01), 0.011, "ext_moto_exhaust", seg=8))
    objs.append(H.box("KickstandFoot", (-0.260, -0.145, 0.008), (0.044, 0.054, 0.014), "ext_moto_exhaust", bevel=0.002))
    return {"Body": objs, "Decals": decal_objs}


# --------------------------------------------------------------------------------------
# 2. Hero Campus Wayfinding Directory Kiosk (CTCC_LOC01_CampusDirectoryBoard)
# --------------------------------------------------------------------------------------

def build_campus_directory_board():
    """Sculpted glass-front roofed tubular-steel Vietnamese hospital campus directory kiosk.
    Replaces ALL 4 flat primitive cubes at (-3.65, 0, -14.8) yaw 55°!
    Keeps exact display center z=1.38, width 1.66, height 1.24 so Sign_CampusMap sits flush inside!"""
    objs = []
    # Concrete base plinth footings + bolted steel anchor flanges (x = ±0.86)
    for sx in (-0.86, 0.86):
        objs.append(H.box(f"DirFoot_{sx:+.2f}", (sx, 0.0, 0.09), (0.24, 0.24, 0.18), "ext_concrete_washed", bevel=0.008))
        objs.append(H.box(f"DirFlange_{sx:+.2f}", (sx, 0.0, 0.19), (0.16, 0.16, 0.022), "ext_iron_wrought", bevel=0.003))
        objs.append(H.cyl(f"DirPost_{sx:+.2f}", (sx, 0.0, 0.19), (sx, 0.0, 2.08), 0.042, "ext_iron_wrought", seg=14))
        # Cross-brace struts under cabinet
        objs.append(H.cyl(f"DirBrace_{sx:+.2f}", (sx, 0.0, 0.56), (sx * 0.72, 0.0, 0.76), 0.014, "ext_iron_wrought", seg=8))
    # Lower horizontal tie rail
    objs.append(H.cyl("DirTieRail", (-0.84, 0.0, 0.56), (0.84, 0.0, 0.56), 0.018, "ext_iron_wrought", seg=10))

    # Recessed Aluminum/Iron Vitrine Display Cabinet (center z=1.38, w=1.68, h=1.28)
    # Back cabinet box (behind y = -0.025 so Sign_CampusMap at y=-0.036 sits crisply in front!)
    objs.append(H.box("DirCabinetBack", (0.0, 0.025, 1.38), (1.68, 0.095, 1.28), "ext_iron_wrought", bevel=0.006))
    # Perimeter mitered vitrine bezel frame protruding around map face (to y=-0.068)
    objs.append(H.box("DirBezelTop", (0.0, -0.038, 2.00), (1.70, 0.064, 0.055), "anodized_alu", bevel=0.004))
    objs.append(H.box("DirBezelBot", (0.0, -0.038, 0.76), (1.70, 0.064, 0.055), "anodized_alu", bevel=0.004))
    for sx in (-0.825, 0.825):
        objs.append(H.box(f"DirBezelSide_{sx:+.2f}", (sx, -0.038, 1.38), (0.055, 0.064, 1.24), "anodized_alu", bevel=0.004))
    # Protective vitrine clear glass cover just in front of Sign_CampusMap (y=-0.058)
    objs.append(H.box("DirVitrineGlass", (0.0, -0.056, 1.38), (1.58, 0.006, 1.18), "Glass", bevel=0.0))

    # Sloped Corrugated Galvanized Rain Canopy Hood (z=2.08..2.24) + Twin Hooded Night Lamps!
    objs.append(H.box("DirRoofFrame", (0.0, -0.06, 2.10), (1.96, 0.52, 0.055), "ext_dado_green",
                      bevel=0.006, rot=(math.radians(-10), 0, 0)))
    objs.append(H.box("DirRoofSheet", (0.0, -0.06, 2.145), (2.02, 0.58, 0.028), "ext_galv_zinc",
                      bevel=0.003, rot=(math.radians(-10), 0, 0)))
    for ri in range(9):
        rx = -0.92 + ri * (1.84 / 8.0)
        objs.append(H.box(f"DirRoofRib_{ri}", (rx, -0.06, 2.165), (0.028, 0.56, 0.022), "ext_galv_zinc",
                          bevel=0.002, rot=(math.radians(-10), 0, 0)))
    # Twin hooded cabinet gooseneck lights illuminating map at night
    for lx in (-0.46, 0.46):
        objs.append(H.pipe(f"DirLampArm_{lx:+.2f}", [(lx, -0.04, 2.05), (lx, -0.24, 2.08), (lx, -0.26, 2.02)], 0.008, "ext_iron_wrought", res=4))
        objs.append(H.lathe(f"DirLampHood_{lx:+.2f}", [(0.015, 0.0), (0.055, -0.035), (0.062, -0.045)],
                            (lx, -0.26, 2.03), "enamel_lamp", seg=14, rot=(math.radians(24), 0, 0)))
        objs.append(H.sphere(f"DirLampBulb_{lx:+.2f}", (lx, -0.24, 1.99), (0.042, 0.042, 0.036), "WarmGlow", seg=10, rings=6))
    return objs


# --------------------------------------------------------------------------------------
# 3. Hero Curved Vietnamese Hospital Terrazzo Bench (CTCC_PROP_TerrazzoBench)
# --------------------------------------------------------------------------------------

def build_terrazzo_bench_hero():
    """Authentic sculpted curved Vietnamese hospital terrazzo courtyard bench (ghế đá mài,
    1.54m wide x 0.58m deep x 0.84m tall) with fine aggregate stone finish (ZERO cow-print!)
    and stenciled 'BỆNH VIỆN ĐA KHOA AN SINH · 1987' institutional backrest lettering."""
    objs = []
    # Two sculpted flared cast-terrazzo pedestal legs at x = ±0.56
    for sx in (-0.56, 0.56):
        # Flared foot plinth + tapered leg pier + arched under-seat corbel + angled backrest spine
        objs.append(H.box(f"BenchFoot_{sx:+.2f}", (sx, -0.02, 0.045), (0.14, 0.54, 0.09), "ext_terrazzo_fine", bevel=0.012))
        objs.append(H.box(f"BenchLeg_{sx:+.2f}", (sx, -0.02, 0.24), (0.11, 0.40, 0.34), "ext_terrazzo_fine", bevel=0.012))
        objs.append(H.box(f"BenchSeatArm_{sx:+.2f}", (sx, -0.03, 0.405), (0.115, 0.46, 0.065), "ext_terrazzo_fine", bevel=0.010))
        objs.append(H.box(f"BenchBackSpine_{sx:+.2f}", (sx, 0.225, 0.62), (0.105, 0.072, 0.46),
                          "ext_terrazzo_fine", bevel=0.010, rot=(math.radians(-14), 0, 0)))
        # Galvanized tie-bolt heads on leg sides
        objs.append(H.cyl(f"BenchBolt_{sx:+.2f}", (sx - 0.062, 0.16, 0.41), (sx + 0.062, 0.16, 0.41), 0.009, "ext_galv_zinc", seg=8))

    # Contoured Ergonomic Curved Seat Slab (1.54 m wide, gentle front bullnose roll!)
    seat_pts = [(-0.77, 0.0, 0.445), (0.77, 0.0, 0.445)]
    seat_prof = [
        (-0.26, -0.026), (-0.275, -0.008), (-0.255, 0.018), (-0.16, 0.026),
        (0.0, 0.016), (0.16, 0.022), (0.21, 0.010), (0.21, -0.024),
    ]
    objs.append(H.sweep("BenchSeatSlab", seat_prof, seat_pts, "ext_terrazzo_fine", closed=True, caps=True, up=(0, 0, 1)))

    # Sculpted Angled Curved Backrest Slab (1.54 m wide, z=0.52..0.84) + Printed Stencil Plate!
    objs.append(H.box("BenchBackSlab", (0.0, 0.218, 0.685), (1.54, 0.056, 0.34),
                      "ext_terrazzo_fine", bevel=0.014, seg=3, rot=(math.radians(-14), 0, 0)))
    # Crisp printed institutional stencil seamlessly integrated into backrest front face (-Y)
    back_print = H.box("BenchBackStencil", (0.0, 0.188, 0.685), (1.36, 0.004, 0.28),
                       "print:bench_stencil", bevel=0.0, rot=(math.radians(-14), 0, 0))
    H.set_planar_print_uv(back_print, axis="Y", flip_u=False, flip_v=False)
    objs.append(back_print)
    return objs


# --------------------------------------------------------------------------------------
# 4. Hero Monsoon Cast-Iron Storm Drain Grate & Curb Frame (CTCC_ENV_DrainGrateAndCurb)
# --------------------------------------------------------------------------------------

def build_drain_grate_hero():
    """Cycles-baked heavy cast-iron storm drain grate seated in a chamfered concrete catch basin
    frame (1.16 x 0.44 m footprint, top flush near z=0.022)."""
    objs = []
    # Recessed dark catch trough interior
    objs.append(H.box("DrainTrough", (0.0, 0.0, -0.015), (1.06, 0.34, 0.025), "ext_wet_soil", bevel=0.002))
    # Beveled concrete catch-basin outer perimeter curb frame
    for sy in (-0.195, 0.195):
        objs.append(H.box(f"DrainCurbH_{sy:+.2f}", (0.0, sy, 0.010), (1.18, 0.068, 0.036), "ext_concrete_washed", bevel=0.006))
    for sx in (-0.555, 0.555):
        objs.append(H.box(f"DrainCurbV_{sx:+.2f}", (sx, 0.0, 0.010), (0.072, 0.33, 0.036), "ext_concrete_washed", bevel=0.006))
    # Heavy Cast-Iron Grate Frame + 14 Chamfered Monsoon Bars + Central Spine
    for sy in (-0.152, 0.152):
        objs.append(H.box(f"GrateRimH_{sy:+.2f}", (0.0, sy, 0.016), (1.04, 0.028, 0.024), "ext_cast_iron", bevel=0.003))
    for sx in (-0.506, 0.506):
        objs.append(H.box(f"GrateRimV_{sx:+.2f}", (sx, 0.0, 0.016), (0.028, 0.28, 0.024), "ext_cast_iron", bevel=0.003))
    objs.append(H.box("GrateSpine", (0.0, 0.0, 0.015), (1.00, 0.026, 0.022), "ext_cast_iron", bevel=0.003))
    for i in range(15):
        bx = -0.455 + i * (0.910 / 14.0)
        objs.append(H.box(f"GrateBar_{i}", (bx, 0.0, 0.018), (0.022, 0.292, 0.020), "ext_cast_iron", bevel=0.003))
    return objs


# --------------------------------------------------------------------------------------
# 5. Hero Raised Brick Courtyard Planter & Wet Soil Mound (CTCC_LOC01_PlanterCurbAndWalkway)
# --------------------------------------------------------------------------------------

def build_courtyard_planter_hero():
    """Cycles-baked 1987 Vietnamese hospital raised red-brick & washed-concrete courtyard planter
    (4.56 x 3.92 m outer footprint, replacing flat timber-bark Unity boxes at (8.40, 0, -15.02)!).
    Includes weathered brick retaining walls, beveled concrete mitered coping stones, corner
    drainage weep holes, and a sculpted organic wet tropical soil & mulch mound!"""
    objs = []
    W, D = 4.54, 3.92
    hw, hd = W * 0.5, D * 0.5
    wt = 0.24

    # 1. Perimeter Concrete Base Toe, Running-Bond Brick Retaining Wall & Mitered Coping Stones
    for sy in (-hd + wt * 0.5, hd - wt * 0.5):
        objs.append(H.box(f"PlToeH_{sy:+.2f}", (0.0, sy, 0.03), (W + 0.06, wt + 0.05, 0.06), "ext_concrete_washed", bevel=0.006))
        objs.append(H.box(f"PlBrickH_{sy:+.2f}", (0.0, sy, 0.165), (W, wt, 0.23), "ext_brick_1987", bevel=0.006))
        objs.append(H.box(f"PlCapH_{sy:+.2f}", (0.0, sy, 0.305), (W + 0.08, wt + 0.07, 0.068), "ext_concrete_washed", bevel=0.009))
    for sx in (-hw + wt * 0.5, hw - wt * 0.5):
        objs.append(H.box(f"PlToeV_{sx:+.2f}", (sx, 0.0, 0.03), (wt + 0.05, D - 2 * wt + 0.06, 0.06), "ext_concrete_washed", bevel=0.006))
        objs.append(H.box(f"PlBrickV_{sx:+.2f}", (sx, 0.0, 0.165), (wt, D - 2 * wt, 0.23), "ext_brick_1987", bevel=0.006))
        objs.append(H.box(f"PlCapV_{sx:+.2f}", (sx, 0.0, 0.305), (wt + 0.07, D - 2 * wt + 0.02, 0.068), "ext_concrete_washed", bevel=0.009))
    # 4 Subtle Corner Pier Caps anchoring planter corners architecturally
    for sx in (-hw + wt * 0.5, hw - wt * 0.5):
        for sy in (-hd + wt * 0.5, hd - wt * 0.5):
            objs.append(H.box(f"PlCorner_{sx:+.1f}_{sy:+.1f}", (sx, sy, 0.19), (0.34, 0.34, 0.38), "ext_concrete_washed", bevel=0.010))

    # 2. Sculpted Organic Wet Tropical Soil & Mulch Mound (rises gently around tree at local (+0.35, +0.22)!)
    def soil_fn(u, v):
        x = (u - 0.5) * (W - 2 * wt + 0.06)
        y = (v - 0.5) * (D - 2 * wt + 0.06)
        # Gentle organic mound peaking near tree trunk (+0.35, +0.22) + subtle root swells & uneven humus
        d_tree = math.hypot(x - 0.35, y - 0.22)
        mound = 0.14 * math.exp(-(d_tree / 1.45) ** 2)
        ripples = 0.022 * math.sin(x * 3.8 + y * 2.5) + 0.015 * math.cos(x * 6.2 - y * 4.9)
        edge_drop = -0.05 * (max(abs(u - 0.5), abs(v - 0.5)) ** 2.4)
        z = 0.21 + mound + ripples + edge_drop
        return (x, y, z)
    objs.append(H.sculpted_grid("PlanterSoilMound", 18, 16, soil_fn, "ext_wet_soil", thickness=0.04))

    # 3. Mossy dark garden rocks nestled subtly into the wet planter soil (natural dark basalt/moss, never pale white dots!)
    rng = random.Random(711)
    for si in range(6):
        ang = si * (math.tau / 6.0) + 0.35
        rad = 0.78 + (si % 3) * 0.32
        rx = 0.35 + math.cos(ang) * rad
        ry = 0.22 + math.sin(ang) * rad * 0.85
        if abs(rx) < hw - 0.50 and abs(ry) < hd - 0.50:
            objs.append(H.sphere(f"GardenStone_{si}", (rx, ry, 0.25),
                                 (0.18 + rng.uniform(0, 0.08), 0.14 + rng.uniform(0, 0.07), 0.10 + rng.uniform(0, 0.04)),
                                 "ext_bark_tropical", seg=10, rings=6, rot=(0, 0, rng.uniform(0, 3.14))))
    return objs


# --------------------------------------------------------------------------------------
# 6. Modular Courtyard Walkway Curb & Gutter Apron Run (CTCC_LOC01_WalkwayCurbRun)
# --------------------------------------------------------------------------------------

def build_walkway_curb_run():
    """Modular 3.40 m precast concrete walkway curb & recessed gutter run (4 interlocking 0.84 m
    blocks along Y, bevelled low profile 0.052m high so player steps effortlessly over it!)."""
    objs = []
    n_blocks = 4
    blen = 0.835
    gap = 0.012
    total = n_blocks * blen + (n_blocks - 1) * gap
    for i in range(n_blocks):
        cy = -total * 0.5 + blen * 0.5 + i * (blen + gap)
        # Precast concrete curb stone w/ beveled walkway lip + flush drainage gutter apron
        objs.append(H.box(f"CurbStone_{i}", (0.0, cy, 0.026), (0.14, blen, 0.052), "ext_concrete_washed", bevel=0.010))
        objs.append(H.box(f"GutterApron_{i}", (0.135, cy, 0.008), (0.15, blen, 0.016), "ext_concrete_washed", bevel=0.004))
    return objs


# --------------------------------------------------------------------------------------
# 7. Hero Vietnamese Centrifugal Concrete Utility Pole & Cobra Streetlamp (CTCC_ENV_UtilityPoleAndLamp)
# --------------------------------------------------------------------------------------

def build_utility_pole_hero():
    """Cycles-baked authentic Vietnamese centrifugal concrete power pole (8.60 m tall) with
    climbing iron rungs, weathered municipal meter box, 'CẤM TRÈO · ĐIỆN ÁP CAO' warning stencil,
    double galvanized crossarms, brown porcelain insulators & cobra-head sodium streetlamp!
    Streetlamp arm extends toward +Y (north/into street or courtyard depending on yaw)."""
    objs = []
    H_POLE = 8.60
    # Concrete base collar + tapered centrifugal concrete shaft
    objs.append(H.cyl("PoleFooting", (0.0, 0.0, 0.0), (0.0, 0.0, 0.24), 0.195, "ext_concrete_washed", seg=16, r2=0.165))
    objs.append(H.cyl("PoleShaft", (0.0, 0.0, 0.20), (0.0, 0.0, H_POLE), 0.152, "ext_concrete_washed", seg=16, r2=0.088))

    # Printed high-voltage hazard stencil wrap ("CẤM TRÈO · ĐIỆN ÁP CAO · TRỤ 04-AS") at eye level (z=1.50..2.02)
    warn_band = H.cyl("PoleWarningBand", (0.0, 0.0, 1.50), (0.0, 0.0, 2.02), 0.144, "print:pole_warning", seg=16, r2=0.140, cap=False)
    H.set_cylindrical_print_uv(warn_band, 1.50, 2.02, angle_offset=math.pi * 0.5)
    objs.append(warn_band)

    # Weathered galvanized municipal electrical meter / streetlamp breaker box strapped at z=2.35
    objs.append(H.box("PoleMeterBox", (0.0, -0.20, 2.38), (0.28, 0.16, 0.38), "breaker_grey", bevel=0.006))
    objs.append(H.cyl("PoleMeterConduit", (0.0, -0.14, 2.56), (0.0, -0.11, 6.85), 0.014, "ext_galv_zinc", seg=8))
    for sz in (2.26, 2.50, 4.20, 6.10):
        r_at = 0.152 - (sz / H_POLE) * 0.064
        objs.append(H.cyl(f"PoleStrap_{sz:.1f}", (0.0, 0.0, sz), (0.0, 0.0, sz + 0.028), r_at + 0.010, "ext_iron_wrought", seg=16))

    # Alternating steel climbing step rungs (z=2.90..6.90)
    for k in range(10):
        rz = 2.90 + k * 0.42
        sgn = -1.0 if (k % 2 == 0) else 1.0
        r_at = 0.152 - (rz / H_POLE) * 0.064
        objs.append(H.pipe(f"ClimbStep_{k}", [
            (sgn * (r_at - 0.01), 0.0, rz), (sgn * (r_at + 0.14), 0.0, rz), (sgn * (r_at + 0.14), 0.0, rz + 0.03)
        ], 0.008, "ext_iron_wrought", res=3))

    # Double Galvanized Steel Overhead Crossarms + Brace Struts + 8 Glazed Ceramic Insulators
    for az in (7.48, 8.12):
        objs.append(H.box(f"CrossArm_{az:.2f}", (0.0, 0.0, az), (1.76, 0.065, 0.072), "ext_galv_zinc", bevel=0.004))
        for sgn in (-1.0, 1.0):
            objs.append(H.cyl(f"ArmBrace_{az:.2f}_{sgn:+.0f}", (sgn * 0.56, 0.0, az - 0.02),
                              (sgn * 0.08, 0.0, az - 0.42), 0.011, "ext_galv_zinc", seg=8))
        for ix in (-0.78, -0.32, 0.32, 0.78):
            objs.append(H.lathe(f"Insulator_{az:.2f}_{ix:+.2f}", [
                (0.0, 0.0), (0.018, 0.0), (0.018, 0.055), (0.042, 0.068),
                (0.026, 0.088), (0.044, 0.108), (0.024, 0.132), (0.0, 0.138)
            ], (ix, 0.0, az + 0.036), "ext_ceramic_brown", seg=12))

    # Curved Cobra-Head Sodium Streetlamp Arm & Housing (extends +1.55m toward +Y at z=6.95)
    objs.append(H.pipe("LampBracketArm", [
        (0.0, 0.09, 6.42), (0.0, 0.58, 6.82), (0.0, 1.24, 7.06), (0.0, 1.56, 7.08)
    ], 0.024, "ext_galv_zinc", res=5))
    objs.append(H.cyl("LampArmBrace", (0.0, 0.10, 6.08), (0.0, 0.68, 6.84), 0.012, "ext_galv_zinc", seg=8))
    # Authentic tapered aluminum cobra-head sodium streetlamp shell + warm ribbed lens
    objs.append(H.box("CobraHoodRear", (0.0, 1.62, 7.10), (0.20, 0.30, 0.11), "anodized_alu", bevel=0.022, seg=3))
    objs.append(H.box("CobraHoodFront", (0.0, 1.84, 7.09), (0.28, 0.34, 0.12), "anodized_alu", bevel=0.028, seg=3))
    objs.append(H.sphere("CobraSodiumLens", (0.0, 1.82, 7.025), (0.23, 0.32, 0.09), "WarmGlow", seg=16, rings=10))
    return objs


# --------------------------------------------------------------------------------------
# 8. Hero Exterior Storytelling Yard Props (CTCC_LOC01_CourtyardProps, separate_parts=True)
# --------------------------------------------------------------------------------------

def build_courtyard_props_pack():
    """3 modular Vietnamese hospital exterior storytelling props:
       - TrashBin:          Concrete-base green enameled hospital courtyard slatted trash bin
       - Bollard:           Weathered painted concrete/steel wheel-stop gate approach bollard
       - ElectricalCabinet: Outdoor weathered galvanized yard-lighting junction cabinet w/ conduit"""
    # 1) Slatted Hospital Courtyard Waste Bin
    trash = [
        H.cyl("TrashBase", (0.0, 0.0, 0.0), (0.0, 0.0, 0.14), 0.21, "ext_concrete_washed", seg=16),
        H.cyl("TrashInnerDrum", (0.0, 0.0, 0.14), (0.0, 0.0, 0.72), 0.185, "ext_galv_zinc", seg=18),
        H.cyl("TrashTopRing", (0.0, 0.0, 0.70), (0.0, 0.0, 0.75), 0.205, "ext_dado_green", seg=18),
        H.lathe("TrashRainCap", [(0.235, 0.0), (0.22, 0.03), (0.08, 0.11), (0.0, 0.12)],
                (0.0, 0.0, 0.88), "ext_dado_green", seg=18),
    ]
    for k in range(3):
        a = k * math.tau / 3.0
        trash.append(H.cyl(f"TrashCapPost_{k}", (math.cos(a) * 0.185, math.sin(a) * 0.185, 0.72),
                           (math.cos(a) * 0.185, math.sin(a) * 0.185, 0.89), 0.008, "ext_iron_wrought", seg=8))
    for k in range(12):
        a = k * math.tau / 12.0
        trash.append(H.box(f"TrashSlat_{k}", (math.cos(a) * 0.195, math.sin(a) * 0.195, 0.43),
                           (0.016, 0.048, 0.54), "ext_dado_green", bevel=0.003, rot=(0, 0, a)))

    # 2) Weathered Concrete & Steel Approach Bollard (0.72m tall)
    bollard = [
        H.cyl("BolFoot", (0.0, 0.0, 0.0), (0.0, 0.0, 0.10), 0.135, "ext_concrete_washed", seg=14),
        H.cyl("BolShaft", (0.0, 0.0, 0.08), (0.0, 0.0, 0.66), 0.095, "ext_concrete_washed", seg=14, r2=0.085),
        H.cyl("BolReflectBand", (0.0, 0.0, 0.46), (0.0, 0.0, 0.56), 0.091, "pccc_red", seg=14, r2=0.089),
        H.sphere("BolCap", (0.0, 0.0, 0.66), (0.17, 0.17, 0.11), "ext_cast_iron", seg=14, rings=8),
    ]

    # 3) Outdoor Galvanized Yard Electrical Junction Cabinet (back at y=0, protrudes -Y)
    elec = [
        H.box("ElecCabBody", (0.0, -0.10, 0.0), (0.44, 0.20, 0.58), "breaker_grey", bevel=0.006),
        H.box("ElecCabHood", (0.0, -0.115, 0.305), (0.48, 0.24, 0.032), "ext_galv_zinc",
              bevel=0.004, rot=(math.radians(-8), 0, 0)),
        H.box("ElecCabDoor", (0.0, -0.206, 0.0), (0.39, 0.016, 0.52), "breaker_grey", bevel=0.004),
        H.box("ElecCabHasp", (-0.155, -0.222, 0.0), (0.036, 0.024, 0.072), "ext_iron_wrought", bevel=0.002),
        H.cyl("ElecConduitUp", (0.12, -0.06, 0.29), (0.12, -0.06, 0.92), 0.015, "ext_galv_zinc", seg=8),
        H.cyl("ElecConduitDn", (-0.12, -0.06, -0.29), (-0.12, -0.06, -0.82), 0.015, "ext_galv_zinc", seg=8),
    ]
    return {"TrashBin": trash, "Bollard": bollard, "ElectricalCabinet": elec}


# --------------------------------------------------------------------------------------
# 9. Hero Buttress-Root Vietnamese Tropical Courtyard Tree (CTCC_ENV_CourtyardTree_A)
#    Cycles-baked organic gnarled trunk + TropicalLeaf passthrough layered canopy!
# --------------------------------------------------------------------------------------

def _make_leaf_cluster(name, center, radius, yaw, tilt, mat_kind="TropicalLeaf"):
    """3D arched intersecting botanical frond cluster using TropicalLeaf alpha card UV [0..1]."""
    bm = bmesh.new()
    uv_layer = bm.loops.layers.uv.new("UVMap")
    cx, cy, cz = center
    rot_base = Matrix.Rotation(yaw, 4, "Z") @ Matrix.Rotation(tilt, 4, "X")
    # 3 intersecting arched leaf fans per cluster for lush 3D volume from every angle!
    for f_idx in range(3):
        fa = f_idx * (math.tau / 3.0)
        m_fan = rot_base @ Matrix.Rotation(fa, 4, "Z")
        nx, ny = 3, 4
        verts = []
        for j in range(ny + 1):
            v = j / float(ny)
            row = []
            for i in range(nx + 1):
                u = i / float(nx)
                lx = (u - 0.5) * radius * 1.35 * math.sin(max(0.15, v) * math.pi * 0.85)
                ly = v * radius * 1.45
                # Natural tropical frond droop & cupped cross-section
                lz = (0.28 * math.sin(v * math.pi) - 0.22 * (v ** 2.2) - 0.09 * ((u - 0.5) * 2.0) ** 2) * radius
                p = m_fan @ Vector((lx, ly, lz)) + Vector((cx, cy, cz))
                row.append(bm.verts.new(p))
            verts.append(row)
        for j in range(ny):
            for i in range(nx):
                f = bm.faces.new((verts[j][i], verts[j][i + 1], verts[j + 1][i + 1], verts[j + 1][i]))
                f.smooth = True
                uvs = [
                    (i / float(nx), j / float(ny)),
                    ((i + 1) / float(nx), j / float(ny)),
                    ((i + 1) / float(nx), (j + 1) / float(ny)),
                    (i / float(nx), (j + 1) / float(ny)),
                ]
                for loop, uv in zip(f.loops, uvs):
                    loop[uv_layer].uv = uv
    mesh = bpy.data.meshes.new(name + "_mesh")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.clear()
    obj.data.materials.append(H.get_mat(mat_kind))
    obj["kind"] = mat_kind
    return obj


def build_courtyard_tree_a_hero():
    """Hero Vietnamese tropical hospital courtyard tree (cây bàng / xà cừ, ~6.4m tall).
    Sculpted buttress root flare & gnarled organic trunk baked in Cycles (`ext_bark_tropical`)
    + multi-tiered horizontally spreading bàng-style lush botanical foliage (`TropicalLeaf` passthrough)."""
    objs = []
    # 1. Gnarled Main Trunk (z=0..3.6m, gently leaning organic taper)
    trunk_pts = [
        (0.0, 0.0, -0.08, 0.34),
        (0.02, -0.01, 0.65, 0.25),
        (0.06, 0.03, 1.55, 0.22),
        (0.03, 0.06, 2.55, 0.19),
        (-0.02, 0.04, 3.45, 0.14),
    ]
    for i in range(len(trunk_pts) - 1):
        x0, y0, z0, r0 = trunk_pts[i]
        x1, y1, z1, r1 = trunk_pts[i + 1]
        objs.append(H.cyl(f"TrunkSeg_{i}", (x0, y0, z0), (x1, y1, z1), r0, "ext_bark_tropical", seg=14, r2=r1))

    # 5 Organic Tapered Tropical Buttress Roots flaring seamlessly into the wet planter soil (buried to z=-0.16)
    for k in range(5):
        a = k * (math.tau / 5.0) + 0.25
        objs.append(H.cyl(f"ButtressUpper_{k}", (math.cos(a) * 0.10, math.sin(a) * 0.10, 0.86),
                          (math.cos(a) * 0.34, math.sin(a) * 0.34, 0.22), 0.125, "ext_bark_tropical", seg=10, r2=0.088))
        objs.append(H.cyl(f"ButtressLower_{k}", (math.cos(a) * 0.34, math.sin(a) * 0.34, 0.22),
                          (math.cos(a) * 0.64, math.sin(a) * 0.64, -0.16), 0.088, "ext_bark_tropical", seg=10, r2=0.042))

    # 2. Tiered Bàng-Style Spreading Primary & Secondary Boughs + Lush Layered Foliage Clusters
    rng = random.Random(901)
    tiers = [
        # (z_fork, n_limbs, reach, z_end_lift, r_limb, leaf_rad)
        (2.45, 5, 2.05, 0.65, 0.092, 1.12),
        (3.25, 5, 2.35, 0.95, 0.076, 1.18),
        (3.85, 4, 1.55, 1.25, 0.058, 1.05),
    ]
    c_idx = 0
    for t_i, (zf, n_limbs, reach, lift, r_limb, lrad) in enumerate(tiers):
        for k in range(n_limbs):
            a = k * (math.tau / n_limbs) + t_i * 0.62 + rng.uniform(-0.18, 0.18)
            p0 = (0.0, 0.04, zf)
            p1 = (math.cos(a) * reach * 0.55, math.sin(a) * reach * 0.55, zf + lift * 0.48)
            p2 = (math.cos(a) * reach, math.sin(a) * reach, zf + lift)
            objs.append(H.cyl(f"LimbA_{t_i}_{k}", p0, p1, r_limb, "ext_bark_tropical", seg=9, r2=r_limb * 0.68))
            objs.append(H.cyl(f"LimbB_{t_i}_{k}", p1, p2, r_limb * 0.68, "ext_bark_tropical", seg=8, r2=r_limb * 0.38))
            # Sub-twig fork
            a2 = a + (0.48 if k % 2 == 0 else -0.48)
            p3 = (p1[0] + math.cos(a2) * reach * 0.52, p1[1] + math.sin(a2) * reach * 0.52, p1[2] + lift * 0.45)
            objs.append(H.cyl(f"Twig_{t_i}_{k}", p1, p3, r_limb * 0.52, "ext_bark_tropical", seg=7, r2=r_limb * 0.26))
            # Lush botanical foliage canopies seated cleanly at limb & twig tips!
            objs.append(_make_leaf_cluster(f"Canopy_{c_idx}", p2, lrad, a, rng.uniform(-0.22, 0.18)))
            c_idx += 1
            objs.append(_make_leaf_cluster(f"Canopy_{c_idx}", p3, lrad * 0.88, a2, rng.uniform(-0.22, 0.18)))
            c_idx += 1
    # Crown top foliage cluster
    objs.append(_make_leaf_cluster("Canopy_Crown", (0.0, 0.0, 5.25), 1.25, 0.4, 0.05))
    return objs


def run_all():
    assets = [
        ("CTCC_PROP_CommuterMotorbike",       build_commuter_motorbike_hero, {
            "LOC01_Motorbike":       {"res": 2048, "bake_r": True, "parts": ["Body"]},
            "LOC01_MotorbikeDecals": {"res": 1024, "bake_r": True, "parts": ["Decals"]},
        }, True,  False, (2.25, -0.45, 0.32)),
        ("CTCC_LOC01_CampusDirectoryBoard",   build_campus_directory_board,  {"LOC01_DirectoryBoard": {"res": 1024, "bake_r": True}}, True,  False, (1.15, -2.10, 0.55)),
        ("CTCC_PROP_TerrazzoBench",           build_terrazzo_bench_hero,     {"LOC01_TerrazzoBench":  {"res": 1024, "bake_r": True}}, True,  False, (1.25, -1.85, 0.65)),
        ("CTCC_ENV_DrainGrateAndCurb",        build_drain_grate_hero,        {"LOC01_DrainGrate":     {"res": 1024, "bake_r": True}}, True,  False, (0.95, -1.35, 1.15)),
        ("CTCC_LOC01_PlanterCurbAndWalkway",  build_courtyard_planter_hero,  {"LOC01_Planter":        {"res": 2048, "bake_r": True}}, True,  False, (1.45, -1.85, 1.15)),
        ("CTCC_LOC01_WalkwayCurbRun",         build_walkway_curb_run,        {"LOC01_CurbRun":        {"res": 1024, "bake_r": True}}, True,  False, (1.35, -1.55, 0.85)),
        ("CTCC_ENV_UtilityPoleAndLamp",       build_utility_pole_hero,       {"LOC01_UtilityPole":    {"res": 1024, "bake_r": True}}, True,  False, (1.45, -2.05, 0.42)),
        ("CTCC_LOC01_CourtyardProps",         build_courtyard_props_pack,    {"LOC01_YardProps":      {"res": 1024, "bake_r": True}}, False, True,  (1.25, -1.75, 0.72)),
        ("CTCC_ENV_CourtyardTree_A",          build_courtyard_tree_a_hero,   {"LOC01_TreeBark":       {"res": 1024, "bake_r": True}}, True,  False, (1.55, -2.15, 0.55)),
    ]
    only = set()
    if "--only" in sys.argv:
        idx = sys.argv.index("--only")
        only = set(sys.argv[idx + 1:])
    for name, fn, specs, lods, sep, cam in assets:
        if only and name not in only:
            continue
        print(f"=== BUILDING {name} ===", flush=True)
        L.export_loc01_asset(name, fn, specs, make_lods=lods, separate_parts=sep, cam_dir=cam, render=True, ao_samples=10)
    print("LOC01_LANDSCAPE_PROPS_DONE", flush=True)


if __name__ == "__main__":
    run_all()
