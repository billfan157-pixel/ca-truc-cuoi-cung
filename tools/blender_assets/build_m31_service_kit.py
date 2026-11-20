"""Original An Sinh service kit. Run with Blender 4.5 in background mode."""

import math
import sys
from pathlib import Path

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))
import common as C

C.RENDER_DIR = C.ROOT / "output" / "m31" / "blender"


def stair_tread():
    # The finished nosing runs across the old terrazzo. Three channels keep the
    # tread legible under grazing light; the back edge laps the next riser.
    parts = [C.make_box("Cast terrazzo", (0, 0, -.12), (1, 1.012, .24),
                        "TerrazzoBench", bevel=.012, segments=3)]
    for depth in (-.454, -.396, -.341):
        parts.append(C.make_box("Inlaid antislip channel", (0, depth, .000),
                                (.91, .017, .005), "BaseTrim", bevel=.002))
    parts.append(C.make_box("Rounded nosing", (0, -.497, -.020), (1.006, .043, .055),
                            "Steel", bevel=.011, segments=4))
    return parts


def dry_cabinet():
    parts = []
    for x in (-.66, .66):
        parts.append(C.make_box("Painted steel side", (x, 0, 1.0), (.04, .57, 1.84), "CabinetOld", bevel=.008))
        for y in (-.22, .22):
            parts.append(C.make_box("Raised damp-floor foot", (x * .94, y, .06), (.105, .09, .12),
                                    "MetalOld", bevel=.009))
    for z in (.13, 1.92):
        parts.append(C.make_box("Folded steel apron", (0, 0, z), (1.35, .59, .052), "MetalOld", bevel=.01))
    parts.append(C.make_box("Rear lock seam", (0, .264, 1), (1.31, .037, 1.79), "MetalOld", bevel=.004))
    for x in (-.323, .323):
        parts.append(C.make_box("Inset door", (x, -.279, 1), (.625, .036, 1.75), "CabinetOld", bevel=.014, segments=3))
        parts.append(C.make_box("Paper index plate", (x, -.305, 1.60), (.235, .016, .085), "KraftPaper", bevel=.003))
        for z in (.35, .385, .42, .455):
            parts.append(C.make_box("Recessed vent", (x, -.301, z), (.265, .007, .014), "Black", bevel=.003))
        grip_x = x * .29
        for z in (.95, 1.085):
            parts.append(C.make_cylinder("Handle foot", (grip_x, -.29, z), (grip_x, -.348, z), .012, "Steel", segments=12))
        parts.append(C.make_cylinder("Handle", (grip_x, -.348, .95), (grip_x, -.348, 1.085), .011, "Steel", segments=16))
    parts.append(C.make_cylinder("Recessed lock cylinder", (.06, -.297, 1.14), (.06, -.318, 1.14), .021, "Brass", segments=16))
    return parts


def load_panel():
    parts = [
        C.make_box("Rolled enamel casing", (0, 0, 0), (1.10, .235, .84), "CabinetOld", bevel=.025, segments=4),
        C.make_box("Door gasket", (0, -.116, 0), (1.045, .023, .79), "Rubber", bevel=.012),
        C.make_box("Enamel door", (0, -.139, 0), (1.02, .038, .765), "MetalPaint", bevel=.014, segments=3),
        C.make_box("Bakelite meter bezel", (0, -.17, .218), (.265, .026, .151), "Black", bevel=.02),
        C.make_box("Meter dial", (0, -.187, .218), (.216, .009, .106), "Paper", bevel=.009),
        C.make_box("Meter arm", (-.023, -.193, .216), (.007, .003, .078), "RedPaint", bevel=.001,
                   rot=(0, -.55, 0)),
    ]
    for channel, x in enumerate((-.33, -.11, .11, .33), start=1):
        parts.append(C.make_box(f"Isolated rocker {channel}", (x, -.185, -.08), (.075, .07, .145),
                                "PlasticIvory", bevel=.012, segments=3, rot=(.16, 0, 0)))
        parts.append(C.make_box("Personalized paper label", (x, -.164, -.208), (.138, .009, .042),
                                "KraftPaper", bevel=.003))
    for x in (-.44, .44):
        for z in (-.325, .328):
            parts.append(C.make_cylinder("Door fastener", (x, -.156, z), (x, -.174, z), .011, "Steel", segments=10))
    return parts


def medicine_cart():
    parts = []
    for side in (-1, 1):
        x = side * .37
        for y in (-.218, .218):
            parts.append(C.make_cylinder("Tubular upright", (x, y, .135), (x, y, .96), .018, "Steel", segments=12))
            parts.append(C.make_cylinder("Rubber caster", (x - .018, y, .066), (x + .018, y, .066), .061,
                                          "Rubber", segments=20))
            parts.append(C.make_cylinder("Caster cap", (x - .019, y, .066), (x - .024, y, .066), .027,
                                          "MetalOld", segments=14))
        parts.append(C.make_cylinder("End handle", (x, -.218, .96), (x, .218, .96), .018, "Steel", segments=12))
    for height in (.29, .61, .85):
        parts.append(C.make_box("Tray", (0, 0, height), (.75, .475, .022), "Steel", bevel=.009, segments=3))
        for side in (-1, 1):
            parts.append(C.make_box("Pressed tray lip", (0, side * .23, height + .027), (.742, .018, .038),
                                    "Steel", bevel=.006))
    parts.append(C.make_box("Paper dose ledger", (-.125, -.05, .876), (.23, .172, .021), "KraftPaper", bevel=.004,
                            rot=(0, 0, -.12)))
    for x, z in ((.175, .887), (.275, .889)):
        parts.append(C.make_cylinder("Opaque medicine container", (x, .06, z - .025), (x, .06, z + .062), .035,
                                      "PlasticIvory", segments=16))
        parts.append(C.make_cylinder("Jar closure", (x, .06, z + .062), (x, .06, z + .074), .037,
                                      "MetalPaint", segments=16))
    return parts


if __name__ == "__main__":
    for name, builder in (
        ("CTCC_ENV_TerrazzoTread", stair_tread),
        ("CTCC_PROP_MorgueDryCabinet", dry_cabinet),
        ("CTCC_PROP_ServiceLoadPanel", load_panel),
        ("CTCC_PROP_MedicineCart", medicine_cart),
    ):
        C.export_asset(name, builder)
