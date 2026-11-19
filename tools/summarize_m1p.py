"""Produce route and stage percentiles from Unity frame records and PresentMon 2.x ETW."""

import argparse
from bisect import bisect_right
from collections import Counter, defaultdict
from csv import DictReader
import json
from math import ceil, isfinite
from pathlib import Path


def stats(values):
    values = sorted(values)
    if not values:
        return None
    return {f'p{p}': round(values[ceil(len(values) * p / 100) - 1], 3) for p in (50, 95, 99)} | {
        'peak': round(values[-1], 3), 'samples': len(values),
    }


def number(value):
    try:
        result = float(value)
        return result if isfinite(result) and result >= 0 else None
    except (TypeError, ValueError):
        return None


def nonzero(records, key, divisor=1):
    return [value / divisor for row in records if (value := number(row.get(key))) is not None and value > 0]


def pairs(path):
    if not path.is_file():
        return {}
    return dict(line.split('=', 1) for line in path.read_text(encoding='utf-8-sig').splitlines() if '=' in line)


def csv_rows(path):
    if not path.is_file():
        return []
    with path.open(newline='', encoding='utf-8-sig') as source:
        return list(DictReader(source))


def summarize(folder):
    run = pairs(folder / 'result.txt')
    identity = pairs(folder / 'identity.txt')
    frames = csv_rows(folder / 'frames.csv')
    if run.get('passed') != 'True' or not frames:
        raise ValueError(f'{folder}: route did not complete')

    clocks = [int(row['qpc']) for row in frames]
    pid = identity.get('pid')
    presents = [row for row in csv_rows(folder / 'presents.csv')
                if (not pid or row['ProcessID'] == pid) and row['CPUStartQPC'] and row['SwapChainAddress']]
    chains = Counter(row['SwapChainAddress'] for row in presents)
    # Unity may create a startup swap chain. Select the dominant chain only inside the measured route.
    matched = [row for row in presents if clocks[0] <= int(row['CPUStartQPC']) <= clocks[-1]]
    measured_chains = Counter(row['SwapChainAddress'] for row in matched)
    if measured_chains:
        matched = [row for row in matched if row['SwapChainAddress'] == measured_chains.most_common(1)[0][0]]

    stages = defaultdict(list)
    for frame in frames:
        stages[frame['stage']].append(frame)
    displayed_by_stage = defaultdict(list)
    for present in matched:
        i = bisect_right(clocks, int(present['CPUStartQPC'])) - 1
        shown = number(present.get('DisplayedTime'))
        if shown is not None and shown > 0:
            displayed_by_stage[frames[i]['stage']].append(shown)
    shown = nonzero(matched, 'DisplayedTime')
    focus = [int(frame['focused']) for frame in frames if 'focused' in frame]

    return {
        'run': run,
        'identity': identity,
        'unity': {key: stats(nonzero(frames, key)) for key in ('wall_ms', 'main_ms', 'render_ms', 'gpu_ms', 'draws', 'setpass', 'triangles')},
        'unity_mib': stats(nonzero(frames, 'unity_bytes', 2**20)),
        'system_mib': stats(nonzero(frames, 'system_bytes', 2**20)),
        'runtime_ms': {key: stats(nonzero(frames, key, 10**6)) for key in ('nav_collect_ns', 'nav_bake_ns', 'probe_schedule_ns', 'save_ns', 'save_flush_ns')},
        'actions_ms': {row['action']: float(row['ms']) for row in csv_rows(folder / 'actions.csv')},
        'focused_pct': round(100 * sum(focus)/len(focus), 1) if focus else None,
        'etw': {'presents': len(matched), 'total_present_rows': len(presents), 'swap_chains': len(chains),
                'displayed': len(shown), 'displayed_pct': round(100 * len(shown) / len(matched), 2) if matched else None,
                'present_modes': dict(Counter(row['PresentMode'] for row in matched)),
                'displayed_ms': stats(shown), 'frame_ms': stats(nonzero(matched, 'FrameTime')),
                'cpu_busy_ms': stats(nonzero(matched, 'CPUBusy')), 'gpu_busy_ms': stats(nonzero(matched, 'GPUBusy'))},
        'stage': {name: {'wall_ms': stats(nonzero(rows, 'wall_ms')), 'gpu_ms': stats(nonzero(rows, 'gpu_ms')),
                         'draws': stats(nonzero(rows, 'draws')), 'displayed_ms': stats(displayed_by_stage[name])}
                  for name, rows in stages.items()},
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('runs', type=Path, nargs='+')
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    report = {folder.name: summarize(folder) for folder in args.runs}
    content = json.dumps(report, indent=2, ensure_ascii=False) + '\n'
    if args.output:
        args.output.write_text(content, encoding='utf-8')
    else:
        print(content, end='')


if __name__ == '__main__':
    main()
