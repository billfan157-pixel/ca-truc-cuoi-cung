#!/usr/bin/env python3
"""
Analyze Campus Performance Benchmark CSVs and generate Before/After comparison boards
+ verified visual gallery in docs/campus/evidence/ (high-quality JPEG for clean PR review).
"""
import csv
import shutil
import statistics
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent.parent
BEFORE_DIR = ROOT / "docs" / "ch01r" / "evidence"
AFTER_DIR = ROOT / "output" / "campus" / "qa-final-v2-module"
BENCH_DIR = ROOT / "output" / "campus" / "bench-final-v2"
EV_DIR = ROOT / "docs" / "campus" / "evidence"
EV_DIR.mkdir(parents=True, exist_ok=True)

FONT_PATH = next(
    (
        str(p)
        for p in [
            ROOT / "Prototype" / "Assets" / "CTCC" / "Resources" / "CTCC" / "BeVietnamPro-SemiBold.ttf",
            Path(r"C:\Windows\Fonts\segoeuib.ttf"),
            Path(r"C:\Windows\Fonts\segoeui.ttf"),
        ]
        if p.exists()
    ),
    None,
)


def fnt(size: int):
    return ImageFont.truetype(FONT_PATH, size) if FONT_PATH else ImageFont.load_default()


def analyze_benchmark():
    result_map = {}
    for line in (BENCH_DIR / "result.txt").read_text(encoding="utf-8").splitlines():
        if "=" in line:
            k, v = line.split("=", 1)
            result_map[k.strip()] = v.strip()

    rows = list(csv.DictReader((BENCH_DIR / "frames.csv").open(encoding="utf-8")))
    ms = [float(r["wall_ms"]) for r in rows]
    main = [float(r["main_ms"]) for r in rows if float(r["main_ms"]) > 0]
    rend = [float(r["render_ms"]) for r in rows if float(r["render_ms"]) > 0]
    gpu = [float(r["gpu_ms"]) for r in rows if float(r["gpu_ms"]) > 0]
    draws = [int(r["draws"]) for r in rows]
    setpass = [int(r["setpass"]) for r in rows]
    tris = [int(r["triangles"]) for r in rows]
    sys_mib = [int(r["system_bytes"]) / (1024 * 1024) for r in rows]
    uni_mib = [int(r["unity_bytes"]) / (1024 * 1024) for r in rows]

    def pct(arr, p):
        s = sorted(arr)
        return s[min(len(s) - 1, int(len(s) * p))]

    boot_s = (int(result_map["ready_qpc"]) - int(result_map["awake_qpc"])) / int(result_map["qpc_hz"])
    print("=== FULL CH01-CH04 CAMPUS PERFORMANCE BENCHMARK (1920x1080 Medium, Intel Arc iGPU) ===")
    print(f"passed={result_map.get('passed')} | frames={len(rows)} | boot_to_ready={boot_s:.2f}s | lod_groups={result_map.get('lod_groups')}")
    print(f"Wall Frame Time: P50={statistics.median(ms):.2f} ms ({1000/statistics.median(ms):.1f} FPS) | P95={pct(ms,0.95):.2f} ms ({1000/pct(ms,0.95):.1f} FPS) | P99={pct(ms,0.99):.2f} ms")
    print(f"Main Thread P50={statistics.median(main):.2f} ms (P95={pct(main,0.95):.2f} ms) | Render P50={statistics.median(rend):.2f} ms | GPU P50={statistics.median(gpu):.2f} ms (P95={pct(gpu,0.95):.2f} ms)")
    print(f"Draw Calls: med={statistics.median(draws):.0f}, P95={pct(draws,0.95)}, max={max(draws)} | SetPass med={statistics.median(setpass):.0f}")
    print(f"Triangles: med={statistics.median(tris):,.0f}, P95={pct(tris,0.95):,.0f}, max={max(tris):,}")
    print(f"Memory: System med={statistics.median(sys_mib):.1f} MiB (max={max(sys_mib):.1f} MiB) | Unity alloc med={statistics.median(uni_mib):.1f} MiB (max={max(uni_mib):.1f} MiB)")

    # Per-stage breakdown
    by_stage = {}
    for r in rows:
        by_stage.setdefault(r["stage"], []).append(float(r["wall_ms"]))
    print("\n--- KEY STAGE P50 / P95 (ms) ---")
    for st in [
        "gate-R", "approach-R", "lan-reception-B", "hall-B", "room-entry-B",
        "approach-return-R", "hall-R-torch", "emergency-R-torch",
        "ch03-stairs-and-archive", "ch03-hunter-functional", "ch04-technical-stair", "ch04-morgue"
    ]:
        if st in by_stage:
            vals = by_stage[st]
            print(f"  {st:26s}: n={len(vals):4d} | P50={statistics.median(vals):6.2f} ms ({1000/statistics.median(vals):5.1f} FPS) | P95={pct(vals,0.95):6.2f} ms")

    acts = list(csv.DictReader((BENCH_DIR / "actions.csv").open(encoding="utf-8")))
    print("\n--- KEY TRANSITION & STREAMING ACTIONS (ms) ---")
    for a in acts:
        if any(k in a["action"] for k in ("swap", "load", "handoff", "M", "power", "finish", "F101")):
            print(f"  {a['action']:26s}: {float(a['ms']):8.2f} ms")


def make_before_after(before_path: Path, after_path: Path, out_name: str, title: str, left_cap: str, right_cap: str):
    if not before_path.exists() or not after_path.exists():
        print(f"Skipping {out_name}: missing {before_path} or {after_path}")
        return
    im_l = Image.open(before_path).convert("RGB").resize((1280, 720), Image.Resampling.LANCZOS)
    im_r = Image.open(after_path).convert("RGB").resize((1280, 720), Image.Resampling.LANCZOS)
    canvas = Image.new("RGB", (2600, 845), (18, 22, 26))
    draw = ImageDraw.Draw(canvas)
    draw.rectangle([0, 0, 2600, 68], fill=(26, 36, 44))
    draw.text((28, 18), title, font=fnt(28), fill=(242, 246, 244))
    canvas.paste(im_l, (16, 82))
    canvas.paste(im_r, (1304, 82))
    draw.rectangle([16, 804, 1296, 840], fill=(34, 42, 48))
    draw.rectangle([1304, 804, 2584, 840], fill=(28, 56, 44))
    draw.text((28, 811), left_cap, font=fnt(19), fill=(224, 228, 230))
    draw.text((1316, 811), right_cap, font=fnt(19), fill=(216, 246, 226))
    out = EV_DIR / out_name
    canvas.save(out, quality=91)
    print(f"Saved {out}")


def make_showcase_grid(items, out_name: str, title: str, cols=3, tile_w=840, tile_h=472):
    rows_n = (len(items) + cols - 1) // cols
    gap = 14
    header_h = 68
    cap_h = 34
    W = cols * tile_w + (cols + 1) * gap
    H = header_h + rows_n * (tile_h + cap_h + gap) + gap
    canvas = Image.new("RGB", (W, H), (16, 20, 24))
    draw = ImageDraw.Draw(canvas)
    draw.rectangle([0, 0, W, header_h], fill=(26, 36, 44))
    draw.text((24, 18), title, font=fnt(27), fill=(242, 246, 244))
    for idx, (fname, cap) in enumerate(items):
        p = AFTER_DIR / fname
        if not p.exists():
            print(f"Missing showcase tile: {p}")
            continue
        r, c = divmod(idx, cols)
        x = gap + c * (tile_w + gap)
        y = header_h + gap + r * (tile_h + cap_h + gap)
        im = Image.open(p).convert("RGB").resize((tile_w, tile_h), Image.Resampling.LANCZOS)
        canvas.paste(im, (x, y))
        draw.rectangle([x, y + tile_h, x + tile_w, y + tile_h + cap_h], fill=(28, 36, 42))
        draw.text((x + 12, y + tile_h + 6), cap, font=fnt(17), fill=(230, 238, 236))
    out = EV_DIR / out_name
    canvas.save(out, quality=91)
    print(f"Saved {out}")


def export_individual_jpegs():
    selected = [
        "campus-ap00-R.png",
        "campus-ap01-R.png",
        "campus-ap01-B.png",
        "campus-courtyard-diag-R.png",
        "campus-ap02-reception-B.png",
        "campus-ap04-room101-window-R.png",
        "campus-ap04-room101-window-B.png",
        "campus-kb-maingate-R.png",
        "campus-kb-maingate-B.png",
        "campus-kd-ambulance-R.png",
        "campus-kd-ambulance-B.png",
        "campus-upper-s1-loc05-R.png",
        "campus-b1-e05b-R.png",
        "campus-aerial-SW-R.png",
        "campus-aerial-SW-B.png",
        "campus-aerial-NE-R.png",
        "campus-aerial-NE-B.png",
    ]
    for name in selected:
        src = AFTER_DIR / name
        if src.exists():
            dst = EV_DIR / name.replace(".png", ".jpg")
            Image.open(src).convert("RGB").save(dst, quality=90)
    print(f"Exported {len(selected)} 1920x1080 campus gallery JPEGs to {EV_DIR}")


if __name__ == "__main__":
    analyze_benchmark()
    make_before_after(
        BEFORE_DIR / "loc01-exterior-R.png",
        AFTER_DIR / "campus-ap00-R.png",
        "before-after-ap00-exterior.jpg",
        "SO SÁNH TRƯỚC / SAU — GÓC NHÌN CỔNG PHỤ ĐÊM (AP00 · REALITY R 2012)",
        "TRƯỚC (HEAD ac0c270): Mặt đứng đơn lẻ lơ lửng + khoảng tối vô định",
        "SAU (Campus Expansion): Khối K-A 3 tầng 1987, ô gạch bông gió, biển AN SINH & cổng phụ trực đêm",
    )
    make_before_after(
        BEFORE_DIR / "loc01-courtyard-R.png",
        AFTER_DIR / "campus-ap01-R.png",
        "before-after-ap01-courtyard.jpg",
        "SO SÁNH TRƯỚC / SAU — SÂN BỆNH VIỆN NHÌN VỀ KHỐI CHÍNH (AP01 · REALITY R 2012)",
        "TRƯỚC (HEAD ac0c270): Facade bay lơ lửng ở y=+3.6m, không thấy quy mô bệnh viện",
        "SAU (Campus Expansion): Công trình 3 tầng tiếp đất thực thụ, sảnh đón sodium & hành lang sâu vào Cấp cứu",
    )
    make_before_after(
        BEFORE_DIR / "room101-R.png",
        AFTER_DIR / "campus-ap04-room101-window-R.png",
        "before-after-room101-lightcourt.jpg",
        "SO SÁNH TRƯỚC / SAU — PHÒNG 101 NHÌN RA SÂN SÁNG NỘI BỘ (AP04 · REALITY R 2012)",
        "TRƯỚC (HEAD ac0c270): Ngoài cửa sổ Bắc chỉ có mảng tường phẳng chặn kín",
        "SAU (Campus Expansion): Thông tầng Sân Sáng 3 tầng, các cánh bệnh phòng đối diện & tán cây trong mưa đêm",
    )
    make_showcase_grid(
        [
            ("campus-aerial-SW-R.png", "1. Tổng thể Tây-Nam (R · 2012): 6 khối nhà, 4 cổng, mái tôn rỉ & mưa đêm"),
            ("campus-aerial-SW-B.png", "2. Tổng thể Tây-Nam (B · 2005): Ca trực vận hành, đèn huỳnh quang toàn khu"),
            ("campus-aerial-NE-B.png", "3. Tổng thể Đông-Bắc (B · 2005): K-C 3F, K-F, Đài nước +17.5m, Sân sau -1.9m"),
            ("campus-kb-maingate-R.png", "4. Cổng Chính & K-B Ngoại trú (R · 2012): Cổng vòm 1987, K-B 2F & K-A 3F"),
            ("campus-kd-ambulance-R.png", "5. K-D Cấp Cứu & Mái đón xe (R · 2012): Biển đỏ CẤP CỨU & xe cứu thương bỏ lại"),
            ("campus-b1-e05b-R.png", "6. Tầng hầm B1 · Hố quan sát E05b (R · 2012): Cửa sau Khu C bịt gạch 2004 (W-Q03)"),
        ],
        "campus-showcase-6panel.jpg",
        "BỆNH VIỆN ĐA KHOA AN SINH — TỔNG HỢP QUẦN THỂ 6 KHỐI NHÀ & ĐỐI CHIẾU R (2012) / B (2005)",
    )
    export_individual_jpegs()
