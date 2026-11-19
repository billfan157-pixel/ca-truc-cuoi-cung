"""M2-C Digital Human Character Builder Entrypoint (delegates to tools/character_pipeline/*).

Reproduces all three canonical characters from verified CC0 MPFB2 `hm08` anatomy, real Blender Cloth
Simulation, verified CMU MoCap BVH retargeting, and calibrated 2048x2048 PBR textures:
- CHAR002 Trần Ngọc Lan (`build_lan.py`)
- CHAR007 / C01 Bé An (`build_an.py`)
- CHAR005 Kẻ Giữ Cửa / The Gatekeeper (`build_gatekeeper.py`)
"""

import sys
from pathlib import Path

PIPE_DIR = Path(__file__).resolve().parents[1] / "character_pipeline"
sys.path.insert(0, str(PIPE_DIR))

import build_lan
import build_an
import build_gatekeeper


def main():
    build_lan.build_lan()
    build_an.build_an()
    build_gatekeeper.build_gatekeeper()


if __name__ == "__main__":
    main()
