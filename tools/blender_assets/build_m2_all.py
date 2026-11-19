"""Run the complete M2 Blender 4.5 LTS asset & rigged character suite."""

import sys
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_characters
import build_m2_architecture
import build_m2_props
import common as C


def main():
    t0 = time.time()
    print("=== M2 BLENDER 4.5 LTS PRODUCTION SUITE ===")
    build_m2_architecture.run_all()
    build_m2_props.run_all()
    build_characters.run_all()
    dt = time.time() - t0
    blends = list(C.BLEND_DIR.glob("*.blend"))
    fbxs = list(C.FBX_DIR.glob("*.fbx"))
    renders = list(C.RENDER_DIR.glob("*.png"))
    print(f"=== M2 BLENDER COMPLETE in {dt:.1f}s | {len(blends)} .blend | {len(fbxs)} .fbx | {len(renders)} M2 renders ===")


if __name__ == "__main__":
    main()
