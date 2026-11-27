"""CH01 Rebirth III Phase 3 — Blender 4.5 Asset: CTCC_PROP_Loc08RecoveryBlotter.

Sculpted 1990s Vietnamese hospital administrative desk leather blotter & archival tray
placed on the clear half of the LOC08 Administrative Desk (6.85, 8.88, 31.82) to anchor
H08 Recovery for E10A_ENVELOPE and MINH_PHONE in LOC08 Reality R.

Uses canonical CTCC PBR material slots (Wood, DadoR, Brass, Paper) mapped automatically
by HospitalAuthoring.PlaceBlenderModel().
"""

from __future__ import annotations

import sys
from pathlib import Path

import bpy

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import common as C  # noqa: E402


def build_loc08_blotter():
    def _build():
        parts = []

        # Recessed ironwood desk blotter frame (0.46m deep X by 0.62m wide Y, 1.4cm high)
        parts.append(C.make_box("Blotter_WoodBase", (0.0, 0.0, 0.006), (0.44, 0.62, 0.012), "Wood", bevel=0.003))
        # Dark green institutional leatherette inlay writing pad
        parts.append(C.make_box("Blotter_LeatherInlay", (0.0, 0.0, 0.0125), (0.39, 0.56, 0.004), "DadoR", bevel=0.001))

        # Four mitered tarnished brass corner protectors
        for sx in (-1.0, 1.0):
            for sy in (-1.0, 1.0):
                parts.append(C.make_box(
                    f"Blotter_CornerX_{sx:+.0f}_{sy:+.0f}",
                    (sx * 0.195, sy * 0.294, 0.0145),
                    (0.052, 0.016, 0.005),
                    "Brass",
                    bevel=0.001,
                ))
                parts.append(C.make_box(
                    f"Blotter_CornerY_{sx:+.0f}_{sy:+.0f}",
                    (sx * 0.212, sy * 0.270, 0.0145),
                    (0.016, 0.060, 0.005),
                    "Brass",
                    bevel=0.001,
                ))

        # Rear raised wooden pen groove & brass clip bar along east edge (+X)
        parts.append(C.make_box("Blotter_PenRail", (0.185, 0.0, 0.020), (0.042, 0.56, 0.014), "Wood", bevel=0.002))
        parts.append(C.make_box("Blotter_BrassRule", (0.162, 0.0, 0.017), (0.006, 0.52, 0.005), "Brass", bevel=0.001))

        # Thin archival reference pad tucked neatly under the brass corner at the north edge
        parts.append(C.make_box("Blotter_RefSheet", (-0.01, 0.0, 0.0155), (0.31, 0.44, 0.0018), "Paper", bevel=0.0))
        return parts

    C.export_asset(
        "CTCC_PROP_Loc08RecoveryBlotter",
        _build,
        make_lods=False,
        render=False,
    )


if __name__ == "__main__":
    build_loc08_blotter()
