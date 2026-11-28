"""Master reproducible builder for the HALL & STAFF Architectural & Nursing Hero Kits (Blender 4.5 LTS).

Usage:
  1. Generate prints first (system Python w/ Pillow):
       python tools/blender_assets/hall_staff/make_hall_staff_prints.py
  2. Run full Blender 4.5 Cycles PBR bake & FBX/LOD export:
       blender -b --python-exit-code 1 -P tools/blender_assets/hall_staff/build_all.py
  3. Selective builds (for fast parallel execution or Pass 2 refinement):
       blender -b --python-exit-code 1 -P tools/blender_assets/hall_staff/build_all.py -- --hall-arch
       blender -b --python-exit-code 1 -P tools/blender_assets/hall_staff/build_all.py -- --hall-doors
       blender -b --python-exit-code 1 -P tools/blender_assets/hall_staff/build_all.py -- --staff
"""
from __future__ import annotations

import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))
sys.path.insert(0, str(HERE.parent / "loc02"))
sys.path.insert(0, str(HERE.parent / "loc03"))

import build_hall_arch_kit as B1  # noqa: E402
import build_hall_doors_mep as B2  # noqa: E402
import build_staff_kit as B3  # noqa: E402
import hall_staff_lib as HS  # noqa: E402


def run_pass2():
    """Rebuild ONLY the 9 Pass 2 refined assets cleanly with updated prints, lighting & geometry."""
    HS.export_hs_asset(
        "CTCC_HALL_CorridorBeams", B1.build_hall_corridor_beams,
        {"HALL_CeilingArch": {"res": 1024, "bake_r": True}},
        make_lods=False, cam_dir=(0.95, -2.15, -0.35), ao_samples=8,
    )
    HS.export_hs_asset(
        "CTCC_STAFF_CorridorArch", B1.build_staff_corridor_arch,
        {"STAFF_CeilingArch": {"res": 1024, "bake_r": True}},
        make_lods=False, cam_dir=(0.95, -2.15, -0.35), ao_samples=8,
    )
    HS.export_hs_asset(
        "CTCC_ENV_EmergencyDoorFrame", B2.build_emergency_door_frame,
        {"HALL_ERDoorFrame": {"res": 1024, "bake_r": True}},
        make_lods=False, cam_dir=(1.15, -1.95, 0.25), ao_samples=10,
    )
    HS.export_hs_asset(
        "CTCC_ENV_EmergencySwingDoorLeaf", B2.build_emergency_swing_door_leaf,
        {"HALL_ERDoorLeaf": {"res": 1024, "bake_r": True}},
        make_lods=True, separate_parts=True, cam_dir=(1.05, -1.85, 0.22), ao_samples=10,
    )
    HS.export_hs_asset(
        "CTCC_PROP_FireExtinguisher", B2.build_fire_extinguisher,
        {"HALL_FireStation": {"res": 1024, "bake_r": True}},
        make_lods=True, cam_dir=(1.05, -1.75, 0.30), ao_samples=12,
    )
    HS.export_hs_asset(
        "CTCC_HALL_BladeSign", B2.build_blade_sign,
        {"HALL_BladeSign": {"res": 1024, "bake_r": True}},
        make_lods=False, cam_dir=(1.15, -1.75, -0.20), ao_samples=10,
    )
    HS.export_hs_asset(
        "CTCC_PROP_NursingFilingDesk", B3.build_nursing_filing_desk,
        {"STAFF_NursingDesk": {"res": 2048, "bake_r": True}},
        make_lods=True, separate_parts=True, cam_dir=(1.15, -1.75, 0.35), ao_samples=12,
    )
    HS.export_hs_asset(
        "CTCC_STAFF_NurseDrawer", B3.build_staff_nurse_drawer,
        {"STAFF_NurseDrawer": {"res": 1024, "bake_r": True}},
        make_lods=False, cam_dir=(1.15, -1.55, 0.72), ao_samples=10,
    )
    HS.export_hs_asset(
        "CTCC_STAFF_BedStatusBoard", B3.build_bed_status_board,
        {"STAFF_BedBoard": {"res": 1024, "bake_r": True}},
        make_lods=False, cam_dir=(1.85, -0.75, 0.25), ao_samples=10,
    )


def main():
    args = set(sys.argv[1:])
    if "--pass2" in args:
        run_pass2()
    elif "--hall-arch" in args:
        B1.run_all()
    elif "--hall-doors" in args:
        B2.run_all()
    elif "--staff" in args:
        B3.run_all()
    elif "--fix-doors" in args:
        B2.run_split_fixes()
    elif "--fix-staff" in args:
        B3.run_split_fixes()
    else:
        B1.run_all()
        B2.run_all()
        B3.run_all()


if __name__ == "__main__":
    main()
