"""Original near-field An Sinh props for the Chapter 01 art pass."""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import common as C


def drain():
    parts = []
    # The pipe begins at the downspout shoe and meets the canopy fascia at 2.78 m.
    parts.append(C.make_pipe("Galvanized downpipe", [
        (0.14, -0.14, 0.14), (0.12, -0.11, 0.17), (0, -0.045, 0.27),
        (0, -0.045, 2.45), (0.02, -0.084, 2.60), (0.23, -0.24, 2.76),
    ], 0.038, "MetalPaint", bevel_res=3))
    for z in (0.42, 1.24, 2.21):
        parts.append(C.make_cylinder("Folded saddle collar", (0, -0.045, z),
                                    (0, -0.045, z + 0.025), 0.044, "Steel", segments=12))
        parts.append(C.make_box("Wall tab", (-0.046, -0.018, z + 0.012),
                                (0.105, 0.025, 0.036), "MetalOld", bevel=0.003))
    parts.append(C.make_box("Patched anchor plaster", (0, 0.009, 1.26),
                            (0.17, 0.025, 0.24), "BaseTrim", bevel=0.009))
    return parts


def shutter():
    parts = [
        C.make_box("Replacement sash", (0, 0, 0), (0.63, 0.046, 0.49), "Frame", bevel=0.009),
        C.make_box("Rolled blind behind sash", (0, -0.026, 0.165),
                   (0.552, 0.034, 0.064), "WoodDoor", bevel=0.006),
    ]
    for i in range(5):
        parts.append(C.make_box("Brake-fold louver", (0, -0.044, -0.168 + i * 0.082),
                                (0.55, 0.079, 0.021), "MetalPaint", bevel=0.003,
                                rot=(math.radians(18), 0, 0)))
    for x in (-0.26, 0.26):
        parts.append(C.make_box("Side stile", (x, -0.055, 0),
                                (0.021, 0.029, 0.41), "Frame", bevel=0.003))
    return parts


def duty_chair():
    legs = [
        C.make_pipe("Bent tube side frame", [(x, -0.31, 0.025), (x, -0.14, 0.43),
                    (x, 0.15, 0.43), (x, 0.20, 0.025)], 0.014, "MetalPaint", bevel_res=3)
        for x in (-0.19, 0.19)
    ]
    for x in (-0.19, 0.19):
        legs.append(C.make_pipe("Backstay", [(x, 0.14, 0.36), (x, 0.19, 0.72),
                                           (x, 0.17, 0.80)], 0.012, "MetalPaint", bevel_res=3))
    return legs + [
        C.make_cylinder("Footrest cross tube", (-0.19, -0.234, 0.203), (0.19, -0.234, 0.203),
                        0.013, "MetalPaint", segments=14),
        C.make_box("Burnished plywood seat", (0, 0, 0.468),
                   (0.43, 0.398, 0.032), "WoodDoor", bevel=0.016, segments=3),
        C.make_box("Rounded backrest", (0, 0.182, 0.764), (0.40, 0.027, 0.19),
                   "WoodDoor", bevel=0.015, segments=3, rot=(math.radians(-6), 0, 0)),
    ]


def file_rack():
    parts = [
        C.make_box("Plywood back", (0, 0, 0.22), (1.17, 0.022, 0.44), "WoodDoor", bevel=0.006)
    ]
    for z in (0.014, 0.22, 0.432):
        parts.append(C.make_box("Dovetailed shelf", (0, -0.11, z),
                                (1.17, 0.25, 0.026), "WoodDoor", bevel=0.007))
    for x in (-0.572, -0.213, 0.178, 0.572):
        parts.append(C.make_box("Divider", (x, -0.11, 0.22),
                                (0.022, 0.25, 0.43), "WoodDoor", bevel=0.004))
    for x, z, width, height, lean, key in [
        (-0.46, 0.128, 0.041, 0.178, -5, "KraftPaper"),
        (-0.405, 0.122, 0.040, 0.165, -2, "Paper"),
        (-0.346, 0.117, 0.043, 0.153, 2, "Olive"),
        (0.031, 0.337, 0.074, 0.168, -4, "KraftPaper"),
        (0.107, 0.324, 0.053, 0.149, 5, "Paper"),
        (0.337, 0.040, 0.215, 0.023, 0, "KraftPaper"),
    ]:
        parts.append(C.make_box("Filed folder", (x, -0.115, z),
                                (width, 0.206, height), key, bevel=0.004,
                                rot=(0, math.radians(lean), 0)))
    return parts


def duty_book():
    return [
        C.make_box("Cloth-bound cover", (0, 0, 0.004), (0.316, 0.231, 0.010), "WoodDoor", bevel=0.004),
        C.make_box("Left laid sheets", (-0.076, -0.001, 0.013), (0.149, 0.216, 0.011), "Paper",
                   bevel=0.003, rot=(0, math.radians(-2), 0)),
        C.make_box("Right laid sheets", (0.076, 0.001, 0.015), (0.149, 0.216, 0.015), "Paper",
                   bevel=0.003, rot=(0, math.radians(3), 0)),
        C.make_cylinder("Binding fold", (0, -0.104, 0.017), (0, 0.104, 0.017),
                        0.003, "KraftPaper", segments=8),
    ]


def clean_utility():
    parts = [
        C.make_lathe("Enamel supply basin", [(0, 0.009), (0.077, 0.009), (0.081, 0.017),
                     (0.104, 0.062), (0.109, 0.069), (0.102, 0.069), (0.097, 0.061),
                     (0.074, 0.021), (0, 0.021)],
                    (-0.071, 0, 0), "MetalPaint", segments=24)
    ]
    for x, y, h in [(0.085, 0.04, 0.125), (0.134, -0.033, 0.106)]:
        parts.extend([
            C.make_lathe("Amber bottle", [(0, 0), (0.022, 0), (0.025, 0.007),
                         (0.025, h - 0.025), (0.012, h - 0.011), (0.012, h), (0, h)],
                         (x, y, 0), "Bakeli", segments=16),
            C.make_cylinder("Phenolic cap", (x, y, h - 0.007), (x, y, h + 0.009),
                            0.014, "Black", segments=16)
        ])
    parts.append(C.make_box("Folded gauze stack", (-0.077, 0.010, 0.038),
                            (0.091, 0.073, 0.028), "Cotton", bevel=0.009,
                            rot=(0, 0, math.radians(11))))
    return parts


if __name__ == "__main__":
    for name, builder in [
        ("CTCC_ENV_CH01CanopyDrain", drain),
        ("CTCC_ENV_CH01ReplacementShutter", shutter),
        ("CTCC_PROP_CH01DutyChair", duty_chair),
        ("CTCC_PROP_CH01FileRack", file_rack),
        ("CTCC_PROP_CH01DutyBook", duty_book),
        ("CTCC_PROP_CH01CareSupplies", clean_utility),
    ]:
        C.export_asset(name, builder, make_lods=True, render=False)
