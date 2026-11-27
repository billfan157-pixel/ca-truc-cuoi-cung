"""Master reproducible builder for the LOC03 Ward 101 & Store Room Blender 4.5 Hero Kit.

Usage:
  Full rebuild (all 29 assets):
    blender -b --python-exit-code 1 -P tools/blender_assets/loc03/build_all.py
  Selective Pass 2 refinement rebuild:
    blender -b --python-exit-code 1 -P tools/blender_assets/loc03/build_all.py -- --pass2
"""
from __future__ import annotations

import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))
sys.path.insert(0, str(HERE.parent / "loc02"))

import ward_lib as W  # noqa: E402
import build_ward_beds_curtain as B1  # noqa: E402
import build_ward_furniture_arch as B2  # noqa: E402
import build_ward_props_store as B3  # noqa: E402


def run_pass2():
    # 1. Refined cloth-simmed beds (zero pin-pinch!) & soft Bed 4 folded trace
    W.export_ward_asset("CTCC_PROP_WardBed", B1.build_ward_bed,
                        {"LOC03_WardBed": {"res": 2048, "bake_r": True}}, make_lods=True, cam_dir=(1.45, -1.55, 0.92), ao_samples=10)
    W.export_ward_asset("CTCC_LOC03_WardBedLived", B1.build_ward_bed_lived,
                        {"LOC03_WardBedLived": {"res": 2048, "bake_r": True}}, make_lods=True, cam_dir=(1.45, -1.55, 0.92), ao_samples=10)
    W.export_ward_asset("CTCC_PROP_Bed4TracesWorking", B1.build_bed4_traces_working,
                        {"LOC03_Bed4Trace": {"res": 1024, "bake_r": False}}, make_lods=False, separate_parts=True, cam_dir=(0.95, -1.35, 0.95), ao_samples=10)

    # 2. Refined east-wall sink (15x15 square ceramic tiles, luminous mirror, shortened supply pipe), WardArch + R-only Bed4Conduit, windows & doors (1024², zero coplanar AO)
    W.export_ward_asset("CTCC_LOC03_WardSink", B2.build_ward_sink,
                        {"LOC03_WardSink": {"res": 1024, "bake_r": True}}, make_lods=True, cam_dir=(1.15, -1.85, 0.55), ao_samples=10)
    W.export_ward_asset("CTCC_LOC03_WardArch", B2.build_ward_arch,
                        {"LOC03_WardArch": {"res": 1024, "bake_r": True}}, make_lods=False, cam_dir=(1.40, -1.55, -0.45), ao_samples=8)
    W.export_ward_asset("CTCC_LOC03_Bed4ConduitR", B2.build_bed4_conduit_r,
                        {"LOC03_Bed4Conduit": {"res": 512, "bake_r": True}}, make_lods=False, render=False, ao_samples=8)
    W.export_ward_asset("CTCC_ENV_SteelWindow_Wide", lambda: B2._build_steel_window(2.14),
                        {"LOC03_WindowWide": {"res": 1024, "bake_r": True}}, make_lods=True, cam_dir=(0.85, -2.15, 0.30), ao_samples=8)
    W.export_ward_asset("CTCC_ENV_SteelWindow_Mid", lambda: B2._build_steel_window(2.09),
                        {"LOC03_WindowMid": {"res": 1024, "bake_r": True}}, make_lods=True, render=False, ao_samples=8)
    W.export_ward_asset("CTCC_ENV_SteelWindow_Narrow", lambda: B2._build_steel_window(1.28),
                        {"LOC03_WindowNarrow": {"res": 1024, "bake_r": True}}, make_lods=True, render=False, ao_samples=8)
    W.export_ward_asset("CTCC_ENV_WardDoorFrame", B2.build_ward_door_frame,
                        {"LOC03_WardDoorFrame": {"res": 1024, "bake_r": True}}, make_lods=True, cam_dir=(1.15, -2.05, 0.45), ao_samples=8)
    W.export_ward_asset("CTCC_ENV_WardDoorLeaf", B2.build_ward_door_leaf,
                        {"LOC03_WardDoorLeaf": {"res": 1024, "bake_r": True}}, make_lods=True, cam_dir=(1.15, -2.05, 0.40), ao_samples=8)

    # 3. Refined prints & Store payoff (room-facing G.04 tag, crisp saline label, 1024² DryShelf)
    W.export_ward_asset("CTCC_PROP_IVStand", B3.build_iv_stand,
                        {"LOC03_IVStand": {"res": 1024, "bake_r": True}}, make_lods=True, render=False, ao_samples=8)
    W.export_ward_asset("CTCC_PROP_DryShelf", B3.build_dry_shelf,
                        {"LOC03_DryShelf": {"res": 1024, "bake_r": True}}, make_lods=True, render=False, ao_samples=8)
    W.export_ward_asset("CTCC_PROP_DismantledBedFrame", B3.build_dismantled_bed_frame,
                        {"LOC03_StorePayoff": {"res": 2048, "bake_r": True}}, make_lods=True, cam_dir=(0.85, 1.95, 0.65), ao_samples=8)


def main():
    if "--pass2" in sys.argv:
        run_pass2()
    else:
        B1.run_all()
        B2.run_all()
        B3.run_all()


if __name__ == "__main__":
    main()
