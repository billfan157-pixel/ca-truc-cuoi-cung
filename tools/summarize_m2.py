"""Correlate release player timing, PresentMon and Windows process samples by QPC."""

import argparse
import csv
import json
import math
from collections import Counter
from pathlib import Path


def rows(path):
    with path.open(encoding="utf-8-sig", newline="") as stream:
        return list(csv.DictReader(stream))


def metadata(path):
    return dict(line.split("=", 1) for line in path.read_text(encoding="utf-8-sig").splitlines() if "=" in line)


def values(samples, field):
    return [float(row[field]) for row in samples if row[field] not in ("", "NA") and float(row[field]) > 0]


def distribution(samples):
    if not samples:
        return None
    ordered = sorted(samples)
    return dict(n=len(ordered), p50=ordered[math.ceil(len(ordered) * .50) - 1],
                p95=ordered[math.ceil(len(ordered) * .95) - 1],
                p99=ordered[math.ceil(len(ordered) * .99) - 1], max=ordered[-1],
                over_33_3=sum(sample > 33.3 for sample in ordered))


def summarize(folder):
    frames = rows(folder / "frames.csv")
    result = metadata(folder / "result.txt")
    identity = metadata(folder / "identity.txt")
    first_qpc, last_qpc = int(frames[0]["qpc"]), int(frames[-1]["qpc"])
    pid = identity["pid"]
    candidates = [row for row in rows(folder / "presents.csv") if row["ProcessID"] == pid]
    if not candidates:
        raise ValueError(f"{folder} has no presents for the audited Windows process")
    swap_chain = Counter(row["SwapChainAddress"] for row in candidates).most_common(1)[0][0]
    display = [row for row in candidates if row["SwapChainAddress"] == swap_chain]
    warm = [row for row in display if first_qpc <= int(row["CPUStartQPC"]) <= last_qpc]
    process_path = folder / "process.csv"
    process = rows(process_path) if process_path.exists() else []
    process_warm = [row for row in process if first_qpc <= int(row["qpc"]) <= last_qpc]
    stages = {}
    for stage in dict.fromkeys(row["stage"] for row in frames):
        selected = [row for row in frames if row["stage"] == stage]
        wall = values(selected, "wall_ms")
        stages[stage] = dict(max_wall_ms=max(wall, default=0), over_33_3=sum(sample > 33.3 for sample in wall),
                             max_cpu_main_ms=max(values(selected, "main_ms"), default=0),
                             max_gpu_ms=max(values(selected, "gpu_ms"), default=0))
    hz = int(result["qpc_hz"])
    launch = int(identity.get("launch_qpc", 0))
    ready = int(result.get("ready_qpc", 0))
    awake = int(result.get("awake_qpc", 0))
    first_present = min(int(row["CPUStartQPC"]) for row in display)
    return dict(run=folder.name, identity=identity, configuration=result,
                app_focus_frames=sum(row["focused"] == "1" for row in frames), route_frames=len(frames),
                foreground_process_samples=sum(row["foreground"] == "1" for row in process_warm),
                warm_process_samples=len(process_warm),
                startup_ms=dict(launch_to_first_present=(first_present - launch) * 1000 / hz if launch else None,
                                launch_to_ready=(ready - launch) * 1000 / hz if launch and ready else None,
                                awake_to_ready=(ready - awake) * 1000 / hz if awake and ready else None),
                unity={field: distribution(values(frames, field)) for field in ("wall_ms", "main_ms", "render_ms", "gpu_ms")},
                presentmon={field: distribution(values(warm, field)) for field in ("FrameTime", "DisplayedTime", "CPUBusy", "GPUTime")},
                memory_bytes=dict(working_set_peak=max(values(process, "working_set_bytes"), default=None),
                                  private_peak=max(values(process, "private_bytes"), default=None),
                                  unity_allocated_peak=max(values(frames, "unity_bytes"), default=None)),
                stages=stages)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("runs", type=Path, nargs="+")
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    args.output.write_text(json.dumps([summarize(folder) for folder in args.runs], indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
