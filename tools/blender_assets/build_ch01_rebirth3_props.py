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
    """Authentic 2012 Vietnamese bar-style feature phone (~0.056m W x 0.120m H x 0.014m D), facing -Y.
    Exported with separate clickable key parts for true diegetic 3D hardware interaction (Phase 2.5)."""
    body = [
        # Matte dark-slate chassis + subtle rear battery cover contour
        C.make_box("PhoneChassis", (0.0, 0.0, 0.0), (0.056, 0.013, 0.120), "Black", bevel=0.003),
        # Brushed silver trim bezel around front face
        C.make_box("PhoneBezel", (0.0, -0.0063, 0.002), (0.050, 0.002, 0.110), "Steel", bevel=0.0012),
        # Recessed LCD screen backing (dynamic RenderTexture quad mounts cleanly right in front at y=-0.0079!)
        C.make_box("PhoneScreenBacking", (0.0, -0.0072, 0.023), (0.043, 0.0014, 0.053), "MonitorScreen", bevel=0.0005),
        # Earpiece speaker grille + subtle status LED
        C.make_box("PhoneSpeakerSlot", (0.0, -0.0074, 0.053), (0.016, 0.0012, 0.0024), "Black", bevel=0.0003),
        # Back camera lens ring for 360-degree realism
        C.make_cylinder("PhoneRearCam", (0.0, 0.0062, 0.036), (0.0, 0.0078, 0.036), 0.0055, "Steel", segments=14),
        C.make_cylinder("PhoneRearLens", (0.0, 0.0076, 0.036), (0.0, 0.0082, 0.036), 0.0032, "Black", segments=12),
        # Tactile 4 x 3 T9 numeric keypad (lower grid)
    ]
    for row in range(4):
        for col in range(3):
            kx = -0.0145 + col * 0.0145
            kz = -0.027 - row * 0.0084
            body.append(
                C.make_box(f"PhoneKey_{row}_{col}", (kx, -0.0078, kz), (0.0115, 0.0018, 0.0062), "MetalOld", bevel=0.0006)
            )

    # Distinct physically clickable 3D control buttons (each gets its own collider + inward press animation in Unity!)
    return {
        "PhoneBody": body,
        "PhoneKeyUp": [C.make_box("KeyUpMesh", (0.0, -0.0079, -0.0065), (0.016, 0.0024, 0.0046), "Steel", bevel=0.0006)],
        "PhoneKeyDown": [C.make_box("KeyDownMesh", (0.0, -0.0079, -0.0185), (0.016, 0.0024, 0.0046), "Steel", bevel=0.0006)],
        "PhoneKeyOk": [C.make_box("KeyOkMesh", (0.0, -0.0083, -0.0125), (0.011, 0.0026, 0.0076), "Black", bevel=0.0007)],
        "PhoneKeyCall": [C.make_box("KeyCallMesh", (-0.0165, -0.0079, -0.0155), (0.012, 0.0022, 0.0075), "MetalPaint", bevel=0.0006)],
        "PhoneKeyEnd": [C.make_box("KeyEndMesh", (0.0165, -0.0079, -0.0155), (0.012, 0.0022, 0.0075), "RedPaint", bevel=0.0006)],
        "PhoneKeySoftL": [C.make_box("KeySoftLMesh", (-0.0165, -0.0079, -0.0072), (0.012, 0.0020, 0.0048), "Steel", bevel=0.0005)],
        "PhoneKeyBack": [C.make_box("KeyBackMesh", (0.0165, -0.0079, -0.0072), (0.012, 0.0020, 0.0048), "Steel", bevel=0.0005)],
    }


def build_envelope_interactive():
    """Interactive 3D weatherproof kraft envelope (0.24m W x 0.165m H x 0.018m D) facing -Y.
    Separate animated flap + directly clickable tucked external note, folded 2005 letter, folded 19/11/2005 newspaper,
    and Wing-C key with tag for zero-tab physical-first inspection (Phase 2.5)."""
    body = [
        # Main weathered kraft envelope pouch (hollow-look front/back/side walls)
        C.make_box("EnvBackPanel", (0.0, 0.006, 0.0), (0.240, 0.003, 0.165), "KraftPaper", bevel=0.001),
        C.make_box("EnvFrontPanel", (0.0, -0.006, -0.005), (0.240, 0.003, 0.155), "KraftPaper", bevel=0.001),
        C.make_box("EnvSideL", (-0.119, 0.0, -0.002), (0.004, 0.015, 0.160), "KraftPaper", bevel=0.001),
        C.make_box("EnvSideR", (0.119, 0.0, -0.002), (0.004, 0.015, 0.160), "KraftPaper", bevel=0.001),
        C.make_box("EnvBottom", (0.0, 0.0, -0.081), (0.240, 0.015, 0.004), "KraftPaper", bevel=0.001),
        # Lower string-button clasp on back/front + hemp twine band holding external note
        C.make_cylinder("EnvClaspButton", (0.0, -0.0085, 0.020), (0.0, -0.0108, 0.020), 0.008, "RedPaint", segments=14),
        C.make_box("EnvTwineBand", (0.0, -0.0086, -0.018), (0.244, 0.0022, 0.0035), "ReedMat", bevel=0.0005),
    ]
    # Top hinged flap (hinge along top rear edge Z = +0.081, Y = +0.005; folds down over front -Y)
    flap = [
        C.make_box("EnvFlapTopFold", (0.0, -0.001, 0.081), (0.236, 0.014, 0.004), "KraftPaper", bevel=0.001),
        C.make_box("EnvFlapFace", (0.0, -0.0082, 0.052), (0.228, 0.0026, 0.056), "KraftPaper", bevel=0.001),
        C.make_cylinder("EnvFlapButton", (0.0, -0.0095, 0.036), (0.0, -0.0118, 0.036), 0.0075, "RedPaint", segments=14),
    ]
    # Tucked white folded external note on front exterior (clickable even BEFORE opening flap!)
    bay_note = [
        C.make_box("BayNoteSheet", (-0.048, -0.0102, -0.024), (0.102, 0.0032, 0.072), "Paper", bevel=0.0008,
                   rot=(0.0, math.radians(-3.5), 0.0)),
    ]
    # Folded blue-ink 2005 Lan letter tucked inside left pouch slot (slides up when flap opens)
    lan_letter = [
        C.make_box("LanLetterFolded", (-0.046, -0.001, 0.028), (0.104, 0.0034, 0.132), "Paper", bevel=0.0008,
                   rot=(0.0, math.radians(2.5), 0.0)),
    ]
    # Folded 19/11/2005 newsprint clipping tucked inside center-right pouch slot
    newspaper = [
        C.make_box("NewspaperFolded", (0.032, 0.002, 0.024), (0.112, 0.0038, 0.128), "CottonOld", bevel=0.0008,
                   rot=(0.0, math.radians(-2.0), 0.0)),
    ]
    # Oxidized Wing-C key & manila tag tucked inside right pouch slot
    wingc_key = [
        C.make_cylinder("EnvWingCKeyRing", (0.082, -0.001, 0.056), (0.082, 0.003, 0.056), 0.012, "Steel", segments=14),
        C.make_box("EnvWingCKeyShaft", (0.082, 0.001, 0.018), (0.006, 0.004, 0.058), "MetalOld", bevel=0.0008),
        C.make_box("EnvWingCKeyBit", (0.091, 0.001, -0.004), (0.014, 0.004, 0.016), "MetalOld", bevel=0.0008),
        C.make_box("EnvWingCTag", (0.084, -0.002, 0.032), (0.034, 0.0024, 0.052), "Paper", bevel=0.0006,
                   rot=(0.0, math.radians(-6.0), 0.0)),
    ]
    return {
        "EnvelopeBody": body,
        "EnvelopeFlap": flap,
        "EnvSlot_BayNote": bay_note,
        "EnvSlot_LanLetter": lan_letter,
        "EnvSlot_Newspaper": newspaper,
        "EnvSlot_WingCKey": wingc_key,
    }


def build_gate_key():
    """Sculpted antique brass hospital side-gate mortise key (~0.092m long) for Inventory 3D preview, Held Hand & Inspector."""
    parts = [
        # Ornate oval brass bow (ring handle)
        C.make_cylinder("KeyBowOuter", (0.0, 0.0, 0.032), (0.0, 0.005, 0.032), 0.017, "Brass", segments=20),
        C.make_cylinder("KeyBowInner", (0.0, -0.0006, 0.032), (0.0, 0.0056, 0.032), 0.0085, "Black", segments=16),
        # Turned brass collar & cylindrical barrel shaft
        C.make_cylinder("KeyCollar", (0.0, 0.0025, 0.014), (0.0, 0.0025, 0.018), 0.0052, "Brass", segments=14),
        C.make_cylinder("KeyShaft", (0.0, 0.0025, -0.044), (0.0, 0.0025, 0.015), 0.0034, "Brass", segments=14),
        # Multi-ward mortise teeth bit
        C.make_box("KeyBitMain", (0.010, 0.0025, -0.035), (0.018, 0.0036, 0.016), "Brass", bevel=0.0006),
        C.make_box("KeyBitNotch", (0.015, 0.0025, -0.029), (0.008, 0.0040, 0.006), "Brass", bevel=0.0004),
        # Split steel keyring loop
        C.make_cylinder("KeyRing", (0.0, 0.001, 0.052), (0.0, 0.004, 0.052), 0.012, "Steel", segments=18),
    ]
    return parts


def build_wingc_key():
    """Oxidized dark-iron Wing-C archival key with weathered manila label tag (~0.095m) for Inventory 3D preview & Inspector."""
    parts = [
        C.make_cylinder("WingCBow", (0.0, 0.0, 0.032), (0.0, 0.005, 0.032), 0.016, "MetalOld", segments=18),
        C.make_cylinder("WingCBowHole", (0.0, -0.0005, 0.032), (0.0, 0.0055, 0.032), 0.007, "Black", segments=14),
        C.make_cylinder("WingCShaft", (0.0, 0.0025, -0.044), (0.0, 0.0025, 0.016), 0.0036, "MetalOld", segments=12),
        C.make_box("WingCBitA", (0.010, 0.0025, -0.036), (0.018, 0.0038, 0.014), "MetalOld", bevel=0.0006),
        C.make_box("WingCBitB", (0.008, 0.0025, -0.022), (0.012, 0.0038, 0.006), "MetalOld", bevel=0.0004),
        # Hanging manila key tag tied to bow
        C.make_cylinder("WingCRing", (0.0, 0.001, 0.051), (0.0, 0.004, 0.051), 0.011, "Steel", segments=14),
        C.make_box("WingCTagCard", (0.032, 0.0025, 0.016), (0.044, 0.0026, 0.028), "Paper", bevel=0.0006,
                   rot=(0.0, math.radians(-12.0), 0.0)),
    ]
    return parts


C.export_asset("CTCC_PROP_GateMailbox", build_gate_mailbox, make_lods=False, render=False, separate_parts=True)
C.export_asset("CTCC_PROP_MinhPhone", build_minh_phone, make_lods=False, render=False, separate_parts=True)
C.export_asset("CTCC_PROP_EnvelopeInteractive", build_envelope_interactive, make_lods=False, render=False, separate_parts=True)
C.export_asset("CTCC_PROP_GateKey", build_gate_key, make_lods=False, render=False, separate_parts=False)
C.export_asset("CTCC_PROP_WingCKey", build_wingc_key, make_lods=False, render=False, separate_parts=False)
