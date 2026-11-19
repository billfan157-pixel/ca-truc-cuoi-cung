"""Run the existing foreground route at 60 Hz and restore the original display mode."""

import argparse
import csv
import ctypes as ct
import json
from pathlib import Path
import struct
import subprocess


ROOT = Path(__file__).resolve().parents[2]
ENUM_CURRENT_SETTINGS = 0xFFFFFFFF
CDS_TEST = 2
CDS_FULLSCREEN = 4
DM_DISPLAYFREQUENCY = 0x00400000
DEVMODEW_SIZE = 220


def current_mode(user32):
    mode = ct.create_string_buffer(DEVMODEW_SIZE)
    # DEVMODEW offsets for dmSize, dmPelsWidth, dmPelsHeight and dmDisplayFrequency.
    struct.pack_into("H", mode, 68, DEVMODEW_SIZE)
    if not user32.EnumDisplaySettingsW(None, ENUM_CURRENT_SETTINGS, mode):
        raise OSError("Unable to read the primary display mode")
    width, height, _, hz = struct.unpack_from("4I", mode, 172)
    return mode, (width, height, hz)


def foreground_share(path, column):
    with path.open(encoding="utf-8-sig", newline="") as stream:
        samples = [row[column] == "1" for row in csv.DictReader(stream)]
    return sum(samples) / len(samples) if samples else 0


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("executable", type=Path)
    parser.add_argument("presentmon", type=Path)
    parser.add_argument("evidence", type=Path)
    parser.add_argument("--runs", type=int, default=3, choices=range(1, 10))
    parser.add_argument("--stress", action="store_true", help="Include the synthetic character stress stages")
    parser.add_argument("--mode", choices=("both", "paced", "uncapped"), default="both")
    args = parser.parse_args()
    exe, presentmon = args.executable.resolve(strict=True), args.presentmon.resolve(strict=True)
    evidence = args.evidence.resolve()
    evidence.mkdir(parents=True)
    user32 = ct.windll.user32
    original, before = current_mode(user32)
    changed = before[2] != 60
    try:
        if changed:
            target = ct.create_string_buffer(original.raw, DEVMODEW_SIZE)
            fields, = struct.unpack_from("I", target, 72)
            struct.pack_into("I", target, 72, fields | DM_DISPLAYFREQUENCY)
            struct.pack_into("I", target, 184, 60)
            if user32.ChangeDisplaySettingsW(target, CDS_TEST) != 0:
                raise OSError(f"Primary display rejected {before[:2]} at 60 Hz")
            if user32.ChangeDisplaySettingsW(target, CDS_FULLSCREEN) != 0:
                raise OSError("Could not activate temporary 60 Hz display mode")
        _, actual = current_mode(user32)
        if actual[2] not in (59, 60):
            raise OSError(f"Display mode is still {actual}")
        (evidence / "display.json").write_text(json.dumps({"previous": before, "test": actual}) + "\n")
        modes = (0,) if args.stress else {"both": (1, 0), "paced": (1,), "uncapped": (0,)}[args.mode]
        for vsync in modes:
            good_runs = 0
            for attempt in range(1, args.runs + 3):
                dest = evidence / f"vsync{vsync}-{attempt}"
                cmd = ["pwsh.exe", "-NoProfile", "-File", str(ROOT / "tools" / "measure_m2.ps1"),
                       "-Executable", str(exe), "-PresentMon", str(presentmon), "-Evidence", str(dest),
                       "-VSync", str(vsync)]
                if args.stress:
                    cmd.append("-CharacterStress")
                subprocess.run(cmd, check=True)
                shares = {"unity": foreground_share(dest / "frames.csv", "focused"),
                          "windows": foreground_share(dest / "process.csv", "foreground")}
                accepted = min(shares.values()) >= 0.99
                (dest / "foreground-validation.json").write_text(
                    json.dumps({"accepted": accepted, "minimum": 0.99, **shares}) + "\n", encoding="utf-8")
                print(f"{dest.name} viewport focus: {shares}, accepted={accepted}", flush=True)
                if accepted:
                    good_runs += 1
                if good_runs == args.runs:
                    break
            if good_runs < args.runs:
                raise RuntimeError(f"Only {good_runs}/{args.runs} runs met foreground criteria at VSync {vsync}")
    finally:
        if changed and user32.ChangeDisplaySettingsW(original, CDS_FULLSCREEN) != 0:
            raise OSError(f"Failed to restore original display mode {before}")


if __name__ == "__main__":
    main()
