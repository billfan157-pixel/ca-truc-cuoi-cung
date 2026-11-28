"""Master reproducible runner for the complete LOC01 Exterior & Entrance Hero Asset Kit
(Prints + Architectural Kit A + Landscape/Vehicle/Props Kit B)."""
from __future__ import annotations

import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
BLENDER = Path(r"C:\Program Files\Blender Foundation\Blender 4.5\blender.exe")


def main():
    subprocess.run([sys.executable, str(HERE / "make_loc01_prints.py")], check=True)
    for script in ("build_loc01_arch_kit.py", "build_loc01_landscape_props.py"):
        subprocess.run([str(BLENDER), "--background", "--python", str(HERE / script)], check=True)
    print("LOC01_ALL_BLENDER_ASSETS_DONE")


if __name__ == "__main__":
    main()
