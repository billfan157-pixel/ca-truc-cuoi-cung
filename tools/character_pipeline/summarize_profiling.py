"""Summarize Unity frames.csv + Intel PresentMon presents.csv + process.csv hardware profiling metrics."""

import csv
import json
import sys
from collections import defaultdict
from pathlib import Path


def pct(vals, p):
    if not vals:
        return 0.0
    s = sorted(vals)
    idx = min(len(s) - 1, max(0, int(round((p / 100.0) * (len(s) - 1)))))
    return s[idx]


def summarize(ev_dir: Path):
    frames_csv = ev_dir / "frames.csv"
    presents_csv = ev_dir / "presents.csv"
    proc_csv = ev_dir / "process.csv"
    res_txt = ev_dir / "result.txt"

    meta = {}
    if res_txt.exists():
        for line in res_txt.read_text(encoding="utf-8", errors="ignore").splitlines():
            if "=" in line:
                k, v = line.split("=", 1)
                meta[k.strip()] = v.strip()

    wall_ms, main_ms, render_ms, gpu_ms = [], [], [], []
    draws, setpass, tris, sys_mb, uni_mb = [], [], [], [], []
    frames_total = 0
    low_focus = 0
    by_stage = defaultdict(lambda: {"wall": [], "main": [], "gpu": [], "draws": [], "tris": []})

    if frames_csv.exists():
        with frames_csv.open("r", encoding="utf-8", errors="ignore") as f:
            for row in csv.DictReader(f):
                try:
                    w = float(row["wall_ms"])
                    m = float(row["main_ms"])
                    r = float(row["render_ms"])
                    g = float(row["gpu_ms"])
                    d = float(row["draws"])
                    sp = float(row["setpass"])
                    tr = float(row["triangles"])
                    sb = float(row["system_bytes"]) / (1024.0 * 1024.0)
                    ub = float(row["unity_bytes"]) / (1024.0 * 1024.0)
                except Exception:
                    continue
                if w <= 0:
                    continue
                frames_total += 1
                if row.get("focused") == "0":
                    low_focus += 1
                    continue
                wall_ms.append(w)
                if m > 0:
                    main_ms.append(m)
                if r > 0:
                    render_ms.append(r)
                if g > 0:
                    gpu_ms.append(g)
                if d > 0:
                    draws.append(d)
                if sp > 0:
                    setpass.append(sp)
                if tr > 0:
                    tris.append(tr)
                if sb > 0:
                    sys_mb.append(sb)
                if ub > 0:
                    uni_mb.append(ub)
                st = row.get("stage", "unknown")
                by_stage[st]["wall"].append(w)
                if m > 0:
                    by_stage[st]["main"].append(m)
                if g > 0:
                    by_stage[st]["gpu"].append(g)
                if d > 0:
                    by_stage[st]["draws"].append(d)
                if tr > 0:
                    by_stage[st]["tris"].append(tr)

    pm_frame, pm_gpu, pm_disp = [], [], []
    if presents_csv.exists():
        with presents_csv.open("r", encoding="utf-8", errors="ignore") as f:
            for row in csv.DictReader(f):
                app = row.get("Application", "")
                if app and "CaTrucCuoiCung" not in app:
                    continue
                for col, dst in (("FrameTime", pm_frame), ("GPUTime", pm_gpu), ("DisplayedTime", pm_disp)):
                    val = row.get(col, "")
                    if val and val != "NA":
                        try:
                            fv = float(val)
                            if 0.01 < fv < 500.0:
                                dst.append(fv)
                        except ValueError:
                            pass

    ws_mb, priv_mb = [], []
    process_background = 0
    if proc_csv.exists():
        with proc_csv.open("r", encoding="utf-8", errors="ignore") as f:
            for row in csv.DictReader(f):
                try:
                    ws_mb.append(float(row["working_set_bytes"]) / (1024.0 * 1024.0))
                    priv_mb.append(float(row["private_bytes"]) / (1024.0 * 1024.0))
                    process_background += row.get("foreground") == "0"
                except Exception:
                    pass

    summary = {
        "dir": str(ev_dir),
        "meta": meta,
        "frames_count": len(wall_ms),
        "focus": {"unity_frames_total": frames_total, "unity_frames_not_focused": low_focus,
                  "process_samples_total": len(ws_mb), "process_samples_not_focused": process_background},
        "valid_samples": {"unity_main": len(main_ms), "unity_render": len(render_ms),
                          "unity_gpu": len(gpu_ms), "presentmon_frame": len(pm_frame),
                          "presentmon_gpu": len(pm_gpu), "presentmon_displayed": len(pm_disp)},
        "unity_wall_ms": {"p50": round(pct(wall_ms, 50), 2), "p95": round(pct(wall_ms, 95), 2), "p99": round(pct(wall_ms, 99), 2)},
        "unity_main_ms": {"p50": round(pct(main_ms, 50), 2), "p95": round(pct(main_ms, 95), 2), "p99": round(pct(main_ms, 99), 2)},
        "unity_render_ms": {"p50": round(pct(render_ms, 50), 2), "p95": round(pct(render_ms, 95), 2)},
        "unity_gpu_ms": {"p50": round(pct(gpu_ms, 50), 2), "p95": round(pct(gpu_ms, 95), 2), "p99": round(pct(gpu_ms, 99), 2)},
        "draws": {"p50": int(pct(draws, 50)), "p95": int(pct(draws, 95)), "max": int(max(draws)) if draws else 0},
        "setpass": {"p50": int(pct(setpass, 50)), "p95": int(pct(setpass, 95)), "max": int(max(setpass)) if setpass else 0},
        "triangles": {"p50": int(pct(tris, 50)), "p95": int(pct(tris, 95)), "max": int(max(tris)) if tris else 0},
        "presentmon_frametime_ms": {"n": len(pm_frame), "p50": round(pct(pm_frame, 50), 2), "p95": round(pct(pm_frame, 95), 2), "p99": round(pct(pm_frame, 99), 2)},
        "presentmon_displayed_ms": {"n": len(pm_disp), "p50": round(pct(pm_disp, 50), 2), "p95": round(pct(pm_disp, 95), 2), "p99": round(pct(pm_disp, 99), 2)},
        "presentmon_gpu_ms": {"n": len(pm_gpu), "p50": round(pct(pm_gpu, 50), 2), "p95": round(pct(pm_gpu, 95), 2), "p99": round(pct(pm_gpu, 99), 2)},
        "memory_mib": {
            "working_set_peak": round(max(ws_mb), 1) if ws_mb else 0.0,
            "working_set_p50": round(pct(ws_mb, 50), 1) if ws_mb else 0.0,
            "private_peak": round(max(priv_mb), 1) if priv_mb else 0.0,
            "unity_alloc_peak": round(max(uni_mb), 1) if uni_mb else 0.0,
            "system_alloc_peak": round(max(sys_mb), 1) if sys_mb else 0.0,
        },
        "stages": {
            st: {
                "n": len(d["wall"]),
                "wall_p95": round(pct(d["wall"], 95), 2),
                "main_p95": round(pct(d["main"], 95), 2),
                "gpu_p95": round(pct(d["gpu"], 95), 2),
                "draws_p95": int(pct(d["draws"], 95)),
                "tris_p95": int(pct(d["tris"], 95)),
            }
            for st, d in by_stage.items()
        },
    }

    (ev_dir / "profiling-summary.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")

    print(f"=== PROFILING SUMMARY: {ev_dir.name} (quality={meta.get('quality')} vsync={meta.get('vsync')} hz={meta.get('hz')}) ===")
    print(f"Unity Wall Frame (n={len(wall_ms)}): P50={summary['unity_wall_ms']['p50']:.2f}ms | P95={summary['unity_wall_ms']['p95']:.2f}ms | P99={summary['unity_wall_ms']['p99']:.2f}ms")
    print(f"Unity Main Thread: P50={summary['unity_main_ms']['p50']:.2f}ms | P95={summary['unity_main_ms']['p95']:.2f}ms | RenderThread P95={summary['unity_render_ms']['p95']:.2f}ms")
    print(f"Unity GPU Time: P50={summary['unity_gpu_ms']['p50']:.2f}ms | P95={summary['unity_gpu_ms']['p95']:.2f}ms | P99={summary['unity_gpu_ms']['p99']:.2f}ms")
    print(f"Draws: P50={summary['draws']['p50']} P95={summary['draws']['p95']} Max={summary['draws']['max']} | SetPass: P50={summary['setpass']['p50']} P95={summary['setpass']['p95']} | Tris: P50={summary['triangles']['p50']} P95={summary['triangles']['p95']} Max={summary['triangles']['max']}")
    print(f"PresentMon FrameTime (n={len(pm_frame)}): P50={summary['presentmon_frametime_ms']['p50']:.2f}ms | P95={summary['presentmon_frametime_ms']['p95']:.2f}ms | P99={summary['presentmon_frametime_ms']['p99']:.2f}ms")
    print(f"PresentMon DisplayedTime (n={len(pm_disp)}): P50={summary['presentmon_displayed_ms']['p50']:.2f}ms | P95={summary['presentmon_displayed_ms']['p95']:.2f}ms | P99={summary['presentmon_displayed_ms']['p99']:.2f}ms")
    print(f"Memory (MiB): WorkingSet Peak={summary['memory_mib']['working_set_peak']:.1f} (P50={summary['memory_mib']['working_set_p50']:.1f}) | Unity Alloc Peak={summary['memory_mib']['unity_alloc_peak']:.1f} | SystemBytes Peak={summary['memory_mib']['system_alloc_peak']:.1f}")
    print("--- Key Stages (P95) ---")
    for st, d in summary["stages"].items():
        print(f"  {st:22s}: n={d['n']:4d} | wall={d['wall_p95']:5.2f}ms | main={d['main_p95']:5.2f}ms | gpu={d['gpu_p95']:5.2f}ms | draws={d['draws_p95']:3d} | tris={d['tris_p95']:7d}")


if __name__ == "__main__":
    summarize(Path(sys.argv[1]))
