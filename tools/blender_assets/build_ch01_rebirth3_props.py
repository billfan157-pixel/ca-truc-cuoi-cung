"""Build CH01 Rebirth III Phase 1 props in Blender 4.5 LTS:
- CTCC_PROP_GateMailbox.fbx: Weathered painted iron hospital mailbox (0.26m W x 0.34m H x 0.14m D)
  mounted flush on the South (-Y in Blender / -Z in Unity) face of the East Gate Pillar at (5.36, 1.12, -20.17).
  Exported with separate parts (MailboxBody, MailboxLid, MailboxPackageVisual) so the lid swings cleanly
  outward and the damp kraft envelope + note + exterior brass gate key hide on atomic pickup.
- CTCC_PROP_MinhPhone.fbx: Authentic 2012 Vietnamese bar-style mobile phone (0.054m x 0.116m x 0.014m)
  for 3D inspection and B01 SMS/call framing.
"""

from pathlib import Path
import math
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import bpy
import common as C


def build_gate_mailbox():
    """Dimensions: W=0.26m (X in [-0.13, +0.13]), H=0.34m (Z in [-0.17, +0.17]), D=0.14m (Y in [-0.07, +0.07]).
    Rear plate sits flush at Y=+0.07 (against East Gate Pillar); front access door faces -Y (South toward road)."""
    body = [
        # Rear mounting plate flush against pillar
        C.make_box("MailboxBackPlate", (0.0, 0.064, 0.0), (0.256, 0.012, 0.336), "MetalOld", bevel=0.002),
        # Left, right, bottom, inner back shell
        C.make_box("MailboxSideL", (-0.123, 0.002, -0.008), (0.012, 0.124, 0.308), "MetalPaint", bevel=0.002),
        C.make_box("MailboxSideR", (0.123, 0.002, -0.008), (0.012, 0.124, 0.308), "MetalPaint", bevel=0.002),
        C.make_box("MailboxBottom", (0.0, 0.002, -0.158), (0.258, 0.128, 0.014), "MetalOld", bevel=0.002),
        # Sloped monsoon rain hood extending slightly forward to shield mail slot
        C.make_box(
            "MailboxRainHood",
            (0.0, -0.008, 0.156),
            (0.268, 0.156, 0.016),
            "MetalOld",
            bevel=0.003,
            rot=(math.radians(11.0), 0.0, 0.0),
        ),
        # Upper front mail-slot lip & weathered rust trim bar
        C.make_box("MailboxSlotBar", (0.0, -0.060, 0.120), (0.242, 0.012, 0.022), "MetalOld", bevel=0.002),
        # Four corner mounting bolts on rear flange
        C.make_box("MountBoltTL", (-0.105, 0.055, 0.128), (0.012, 0.008, 0.012), "Steel", bevel=0.001),
        C.make_box("MountBoltTR", (0.105, 0.055, 0.128), (0.012, 0.008, 0.012), "Steel", bevel=0.001),
        C.make_box("MountBoltBL", (-0.105, 0.055, -0.132), (0.012, 0.008, 0.012), "Steel", bevel=0.001),
        C.make_box("MountBoltBR", (0.105, 0.055, -0.132), (0.012, 0.008, 0.012), "Steel", bevel=0.001),
        # Small exterior brass wire hook on right inner lip for hanging gate key outside envelope
        C.make_pipe(
            "MailboxKeyHook",
            [(0.092, -0.010, 0.055), (0.092, -0.036, 0.055), (0.092, -0.044, 0.042), (0.092, -0.038, 0.032)],
            0.0022,
            "Steel",
            bevel_res=3,
        ),
    ]

    # Hinged front access door (side-hinged along EAST vertical seam X = +0.120, Y = -0.062 so it swings outward South-East away from the gate!)
    lid = [
        C.make_box("MailboxDoorPanel", (0.0, -0.062, -0.018), (0.236, 0.010, 0.258), "MetalPaint", bevel=0.002),
        # Embossed horizontal slot rain flap + latch handle on west (gate-facing) side
        C.make_box("MailboxSlotFlap", (0.0, -0.070, 0.082), (0.184, 0.007, 0.026), "MetalOld", bevel=0.002),
        C.make_box("MailboxLatchPlate", (-0.098, -0.071, -0.018), (0.022, 0.012, 0.044), "Steel", bevel=0.002),
        C.make_box("MailboxLatchKnob", (-0.098, -0.080, -0.018), (0.014, 0.012, 0.020), "Brass", bevel=0.002),
        # Hinge barrel cylinders along east edge (+X)
        C.make_cylinder("HingeTop", (0.120, -0.064, 0.062), (0.120, -0.064, 0.098), 0.006, "Steel", segments=10),
        C.make_cylinder("HingeBot", (0.120, -0.064, -0.118), (0.120, -0.064, -0.082), 0.006, "Steel", segments=10),
    ]

    # Atomic package inside mailbox (hidden cleanly once CH01-MAILBOX-PICKUP commits!):
    # Damp kraft envelope + white folded note tucked in front + brass gate key hanging visibly outside envelope!
    package = [
        C.make_box(
            "PackageKraftEnvelope",
            (-0.014, 0.012, -0.030),
            (0.168, 0.018, 0.228),
            "KraftPaper",
            bevel=0.002,
            rot=(math.radians(-6.0), 0.0, math.radians(3.0)),
        ),
        C.make_box(
            "PackageExternalNote",
            (-0.026, -0.004, -0.016),
            (0.115, 0.006, 0.145),
            "Paper",
            bevel=0.001,
            rot=(math.radians(-8.0), 0.0, math.radians(-4.0)),
        ),
        # Visible brass gate key hanging outside the damp kraft envelope (Core 04: "chìa vẫn ngoài bì")
        C.make_cylinder("PackageGateKeyBow", (0.088, -0.036, 0.016), (0.088, -0.032, 0.016), 0.014, "Brass", segments=14),
        C.make_box("PackageGateKeyShaft", (0.088, -0.034, -0.022), (0.006, 0.004, 0.056), "Brass", bevel=0.001),
        C.make_box("PackageGateKeyBit", (0.079, -0.034, -0.042), (0.014, 0.004, 0.016), "Brass", bevel=0.001),
        # Secondary oxidized Wing-C key + manila tag tucked behind gate key
        C.make_box(
            "PackageWingCKeyShaft",
            (0.066, -0.024, -0.028),
            (0.006, 0.004, 0.054),
            "MetalOld",
            bevel=0.001,
            rot=(0.0, math.radians(8.0), 0.0),
        ),
        C.make_box(
            "PackageWingCTag",
            (0.060, -0.022, -0.068),
            (0.026, 0.003, 0.034),
            "Paper",
            bevel=0.001,
            rot=(0.0, math.radians(6.0), 0.0),
        ),
    ]

    return {
        "MailboxBody": body,
        "MailboxLid": lid,
        "MailboxPackageVisual": package,
    }


def build_minh_phone():
    """Authentic 2012 Vietnamese bar-style feature phone (~0.054m W x 0.116m H x 0.014m D), facing -Y."""
    parts = [
        # Matte dark-slate chassis
        C.make_box("PhoneBody", (0.0, 0.0, 0.0), (0.054, 0.013, 0.116), "Black", bevel=0.003),
        # Brushed silver trim bezel around front face
        C.make_box("PhoneBezel", (0.0, -0.0062, 0.002), (0.048, 0.002, 0.106), "Steel", bevel=0.001),
        # Recessed backlit LCD screen area (upper half)
        C.make_box("PhoneScreenBacking", (0.0, -0.0072, 0.022), (0.042, 0.0015, 0.052), "MonitorScreen", bevel=0.0005),
        # Earpiece speaker grille
        C.make_box("PhoneSpeakerSlot", (0.0, -0.0073, 0.051), (0.016, 0.001, 0.0025), "Black", bevel=0.0003),
        # D-pad navigation ring
        C.make_box("PhoneNavPad", (0.0, -0.0076, -0.012), (0.018, 0.0022, 0.014), "Steel", bevel=0.001),
        C.make_box("PhoneNavCenter", (0.0, -0.0080, -0.012), (0.009, 0.0022, 0.007), "Black", bevel=0.0006),
        # Call (green-tinted metal) & End (red) soft keys
        C.make_box("PhoneKeyCall", (-0.016, -0.0076, -0.012), (0.011, 0.0018, 0.012), "MetalPaint", bevel=0.0006),
        C.make_box("PhoneKeyEnd", (0.016, -0.0076, -0.012), (0.011, 0.0018, 0.012), "RedPaint", bevel=0.0006),
    ]
    # 4 x 3 tactile T9 keypad buttons
    for row in range(4):
        for col in range(3):
            kx = -0.014 + col * 0.014
            kz = -0.025 - row * 0.0085
            parts.append(
                C.make_box(f"PhoneKey_{row}_{col}", (kx, -0.0077, kz), (0.011, 0.0018, 0.0062), "MetalOld", bevel=0.0006)
            )
    return parts


C.export_asset("CTCC_PROP_GateMailbox", build_gate_mailbox, make_lods=False, render=False, separate_parts=True)
C.export_asset("CTCC_PROP_MinhPhone", build_minh_phone, make_lods=False, render=False, separate_parts=False)
