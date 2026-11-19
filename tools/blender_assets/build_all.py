"""Run the complete M0-R Blender 4.5 asset suite and assemble visual inspection contact sheets."""

import sys
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_architecture
import build_furniture
import build_props
import build_vegetation
import common as C


def main():
    t0 = time.time()
    print("=== M0-R BLENDER 4.5 ASSET PRODUCTION ===")
    build_architecture.run_all()
    build_furniture.run_all()
    build_props.run_all()
    build_vegetation.run_all()
    dt = time.time() - t0
    blends = list(C.BLEND_DIR.glob("*.blend"))
    fbxs = list(C.FBX_DIR.glob("*.fbx"))
    renders = list(C.RENDER_DIR.glob("*.png"))
    print(f"=== COMPLETE in {dt:.1f}s | {len(blends)} .blend | {len(fbxs)} .fbx | {len(renders)} renders ===")


if __name__ == "__main__":
    main()
