"""Master reproducible runner for all LOC02 Reception Hero Blender 4.5 assets.
Run via:
  "C:/Program Files/Blender Foundation/Blender 4.5/blender.exe" --background --python tools/blender_assets/loc02/build_all_loc02.py
"""
import subprocess
import sys
import time
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

# Ensure archival Vietnamese print textures exist before Blender runs
if not (HERE.parents[2] / "ArtSource" / "LOC02" / "prints" / "an01_ledger_spread.png").exists():
    import make_prints  # noqa: F401

import build_counter
import build_partition
import build_desk_props
import build_workstation
import build_hall_arch


def main():
    t0 = time.time()
    for mod in (build_counter, build_partition, build_desk_props, build_workstation, build_hall_arch):
        ts = time.time()
        print(f"\n========== {mod.__name__} ==========", flush=True)
        mod.run()
        print(f"---------- {mod.__name__} done in {time.time() - ts:.1f}s ----------", flush=True)
    print(f"\n[LOC02 HERO SUITE COMPLETE] Total elapsed: {time.time() - t0:.1f}s", flush=True)


if __name__ == "__main__":
    main()
