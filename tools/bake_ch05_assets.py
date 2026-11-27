"""Generate authentic CH05 procedural architectural blueprints (PZ07 / E05) and fire-alarm bell loop.

All outputs are 100% original procedural PIL/NumPy/SciPy assets using SIL OFL 1.1 Be Vietnam Pro fonts
and Core v3.3.1 (3c0b955) fictional Bệnh viện Đa khoa An Sinh specifications.
"""

from __future__ import annotations

import hashlib
import json
import math
import wave
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
FONTS_DIR = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Resources" / "CTCC"
TEX_DIR = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Textures"
RES_DIR = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Resources" / "CTCC"

W, H = 1360, 1024


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    name = "BeVietnamPro-SemiBold.ttf" if bold else "BeVietnamPro-Regular.ttf"
    return ImageFont.truetype(str(FONTS_DIR / name), size)


def make_sheet_base(bg_rgb: tuple[int, int, int], seed: int, blueprint: bool = False) -> Image.Image:
    rng = np.random.default_rng(seed)
    grain = rng.normal(0.0, 1.0, (H // 4, W // 4)).astype(np.float32)
    grain_img = Image.fromarray(np.clip(grain * 0.5 + 0.5, 0, 1), mode="F").resize((W, H), Image.Resampling.BICUBIC)
    g = (np.asarray(grain_img, dtype=np.float32) - 0.5) * 0.08

    arr = np.empty((H, W, 3), dtype=np.float32)
    for c in range(3):
        arr[..., c] = bg_rgb[c] / 255.0 + g

    # Fold lines (quarters)
    yy = np.linspace(0.0, 1.0, H, dtype=np.float32)[:, None]
    xx = np.linspace(0.0, 1.0, W, dtype=np.float32)[None, :]
    fold_h = np.exp(-((yy - 0.50) * 140.0) ** 2) * 0.06
    fold_v = np.exp(-((xx - 0.50) * 140.0) ** 2) * 0.05
    arr -= (fold_h + fold_v)[..., None]

    # Subtle edge wear
    edge = np.clip((xx * 2 - 1) ** 2 * 0.45 + (yy * 2 - 1) ** 2 * 0.45 - 0.25, 0.0, 1.0)
    tint = np.array([0.07, 0.14, 0.20] if blueprint else [0.50, 0.43, 0.30], dtype=np.float32)
    arr = arr * (1.0 - edge[..., None] * 0.24) + tint * (edge[..., None] * 0.24)
    return Image.fromarray(np.uint8(np.clip(arr, 0.0, 1.0) * 255.0), mode="RGB")


def draw_b1_plan(
    draw: ImageDraw.ImageDraw,
    line_col: tuple[int, int, int],
    dim_col: tuple[int, int, int],
    accent_col: tuple[int, int, int],
    marker_col: tuple[int, int, int],
    sealed_2004: bool,
) -> None:
    """Draw shared B1 architectural footprint so Marker 1 (S2) and Marker 2 (East Column X=34.5) align exactly."""
    # Grid paper background
    for gx in range(90, 1270, 48):
        draw.line((gx, 165, gx, 845), fill=dim_col, width=1)
    for gy in range(165, 845, 48):
        draw.line((90, gy, 1270, gy), fill=dim_col, width=1)

    # Outer border
    draw.rectangle((64, 52, W - 64, H - 52), outline=line_col, width=4)
    draw.rectangle((74, 62, W - 74, H - 62), outline=dim_col, width=2)

    # Coordinate mapping for B1 rooms (shared exact geometry between both sheets!)
    # LOC10 Morgue: x[-1.5..4.5], z[36..42]
    # LOC09 Service: x[4.5..10.5], z[31..45.5]
    # LOC11 Vestibule: x[10.5..14.5], z[39..45]
    # LOC12 Khu C: x[14.5..34.5], z[35..49]
    def pt(wx: float, wz: float) -> tuple[int, int]:
        px = int(round(190 + (wx + 2.0) * 27.0))
        py = int(round(760 - (wz - 30.0) * 26.0))
        return px, py

    def room_box(x0: float, x1: float, z0: float, z1: float, label: str, sub: str) -> None:
        a = pt(x0, z1)
        b = pt(x1, z0)
        draw.rectangle((a[0], a[1], b[0], b[1]), outline=line_col, width=4)
        cx, cy = (a[0] + b[0]) // 2, (a[1] + b[1]) // 2
        draw.text((cx, cy - 14), label, font=font(20, bold=True), fill=line_col, anchor="mm")
        draw.text((cx, cy + 14), sub, font=font(15, bold=False), fill=line_col, anchor="mm")

    room_box(-1.5, 4.5, 36.0, 42.0, "NHÀ XÁC", "TẦNG B1 · CÁNH TÂY")
    room_box(4.5, 10.5, 31.1, 45.5, "KỸ THUẬT B1", "BUỒNG BƠM & VAN")
    room_box(10.5, 14.5, 39.0, 45.0, "SẢNH ĐỆM C", "HÀNH LANG CÁCH LY")
    room_box(14.5, 34.5, 35.0, 49.0, "KHU C", "BUỒNG ĐIỀU TRỊ CÁCH LY (12 GIƯỜNG)")

    # Internal bays in LOC12
    for bx in (21.0, 27.0):
        p0, p1 = pt(bx, 35.0), pt(bx, 40.8)
        draw.line((p0, p1), fill=line_col, width=3)

    # Stairwell S2 symbol entering LOC09 south
    s2a, s2b = pt(5.7, 31.1), pt(8.3, 27.5)
    draw.rectangle((s2a[0], s2a[1], s2b[0], s2b[1]), outline=line_col, width=3)
    for step_z in np.linspace(28.0, 30.8, 7):
        draw.line((pt(5.7, float(step_z)), pt(8.3, float(step_z))), fill=line_col, width=2)
    draw.text(pt(7.0, 26.8), "THANG S2", font=font(16, bold=True), fill=line_col, anchor="mm")

    # Door openings between LOC10-LOC09, LOC09-LOC11, LOC11-LOC12 (G06)
    for dx, dz in ((4.5, 39.0), (10.5, 42.0), (14.5, 42.0)):
        pa, pb = pt(dx, dz - 0.8), pt(dx, dz + 0.8)
        draw.line((pa, pb), fill=accent_col, width=5)

    # ARCHITECTURAL MARKER 1: Stairwell S2 Reference Axis
    m1 = pt(7.0, 31.1)
    draw.ellipse((m1[0] - 18, m1[1] - 18, m1[0] + 18, m1[1] + 18), outline=marker_col, width=4)
    draw.line((m1[0] - 26, m1[1], m1[0] + 26, m1[1]), fill=marker_col, width=3)
    draw.line((m1[0], m1[1] - 26, m1[0], m1[1] + 26), fill=marker_col, width=3)
    draw.text((m1[0] + 26, m1[1] + 26), "MỐC 1 · TRỤC THANG S2", font=font(17, bold=True), fill=marker_col)

    # ARCHITECTURAL MARKER 2: East Structural Column Grid (X = 34.5)
    m2 = pt(34.5, 49.0)
    draw.ellipse((m2[0] - 18, m2[1] - 18, m2[0] + 18, m2[1] + 18), outline=marker_col, width=4)
    draw.line((m2[0] - 26, m2[1], m2[0] + 26, m2[1]), fill=marker_col, width=3)
    draw.line((m2[0], m2[1] - 26, m2[0], m2[1] + 26), fill=marker_col, width=3)
    draw.text((m2[0] - 210, m2[1] - 34), "MỐC 2 · TRỤC CỘT ĐÔNG (TRỤC D)", font=font(17, bold=True), fill=marker_col)

    # CRITICAL CONTRADICTION POINT: East Emergency Exit
    ex_top = pt(34.5, 43.2)
    ex_bot = pt(34.5, 40.8)
    ex_mid = pt(34.5, 42.0)
    if not sealed_2004:
        # 2005 Posted Diagram: draws green open exit & arrow to east sunken court!
        draw.line((ex_top, ex_bot), fill=(26, 134, 72), width=10)
        arrow_end = (ex_mid[0] + 82, ex_mid[1])
        draw.line((ex_mid[0] - 28, ex_mid[1], arrow_end[0], arrow_end[1]), fill=(26, 134, 72), width=6)
        draw.polygon(
            [(arrow_end[0] + 16, arrow_end[1]), (arrow_end[0] - 8, arrow_end[1] - 12), (arrow_end[0] - 8, arrow_end[1] + 12)],
            fill=(26, 134, 72),
        )
        draw.text((ex_mid[0] + 16, ex_mid[1] - 42), "LỐI THOÁT HIỂM ĐÔNG", font=font(17, bold=True), fill=(22, 118, 62))
        draw.text((ex_mid[0] + 16, ex_mid[1] + 22), "Cửa D-04 → Sân trũng", font=font(14, bold=False), fill=(22, 118, 62))
    else:
        # June 2004 As-Built Blueprint: solid 220mm brick infill hatched red, door & handle removed!
        bx0, by0 = ex_top[0] - 10, ex_top[1]
        bx1, by1 = ex_bot[0] + 14, ex_bot[1]
        draw.rectangle((bx0, by0, bx1, by1), fill=(176, 48, 42), outline=(255, 224, 210), width=3)
        for hy in range(by0 + 6, by1, 10):
            draw.line((bx0, hy, bx1, hy - 8), fill=(255, 232, 220), width=2)
        draw.ellipse((ex_mid[0] - 40, ex_mid[1] - 46, ex_mid[0] + 40, ex_mid[1] + 46), outline=(255, 116, 98), width=4)
        draw.text((ex_mid[0] + 48, ex_mid[1] - 46), "XÂY BÍT GẠCH ĐẶC 220mm", font=font(17, bold=True), fill=(255, 198, 180))
        draw.text((ex_mid[0] + 48, ex_mid[1] - 20), "TRÁT VỮA HAI MẶT · THÁO KHUNG", font=font(15, bold=True), fill=(255, 228, 212))
        draw.text((ex_mid[0] + 48, ex_mid[1] + 6), "CỬA D-04 · HOÀN CÔNG 06/2004", font=font(14, bold=False), fill=(235, 240, 248))


def build_posted_map_2005() -> Path:
    img = make_sheet_base((228, 222, 202), seed=5501, blueprint=False)
    d = ImageDraw.Draw(img)
    draw_b1_plan(
        d,
        line_col=(32, 46, 40),
        dim_col=(188, 182, 162),
        accent_col=(140, 82, 42),
        marker_col=(164, 46, 38),
        sealed_2004=False,
    )
    d.text((100, 84), "BỆNH VIỆN ĐA KHOA AN SINH  ·  KHỐI K-A (BÁN HẦM B1)", font=font(20, bold=True), fill=(46, 60, 52))
    d.text((100, 114), "SƠ ĐỒ THOÁT HIỂM NIÊM YẾT BẢNG TRỰC — LƯU HÀNH NĂM 2005", font=font(27, bold=True), fill=(26, 40, 34))
    d.rectangle((92, 855, 1268, 948), outline=(72, 82, 74), width=2, fill=(218, 212, 190))
    d.text((112, 870), "HƯỚNG DẪN THOÁT HIỂM TẦNG BÁN HẦM B1 (NIÊM YẾT 2005):", font=font(18, bold=True), fill=(138, 42, 34))
    d.text(
        (112, 898),
        "Khi có sự cố, nhân viên trực hướng dẫn di tản theo trục cầu thang S2 hoặc lối thoát hiểm cửa D-04 phía Đông Khu C\n"
        "dẫn thẳng ra sân trũng ngoài trời cao độ -1,90 m. Bản niêm yết lưu hành tại bảng trực hành lang tầng trệt.",
        font=font(16, bold=False),
        fill=(34, 46, 40),
    )
    out = TEX_DIR / "Doc_Ch05_PostedMap2005.png"
    img.save(out)
    return out


def build_asbuilt_2004_e05() -> Path:
    img = make_sheet_base((28, 62, 98), seed=5502, blueprint=True)
    d = ImageDraw.Draw(img)
    draw_b1_plan(
        d,
        line_col=(226, 238, 248),
        dim_col=(46, 86, 126),
        accent_col=(246, 212, 122),
        marker_col=(255, 198, 86),
        sealed_2004=True,
    )
    d.text((100, 82), "BỆNH VIỆN ĐA KHOA AN SINH  ·  HỒ SƠ HOÀN CÔNG CẢI TẠO BÁN HẦM KỸ THUẬT B1", font=font(20, bold=True), fill=(186, 216, 242))
    d.text((100, 112), "BẢN VẼ HOÀN CÔNG CẢI TẠO B1 — THÁNG 06/2004  (MÃ HỒ SƠ: HC-B1/04)", font=font(27, bold=True), fill=(246, 248, 252))

    # Approval title block bottom-right & reason box bottom-left
    d.rectangle((92, 845, 865, 952), outline=(212, 228, 242), width=2, fill=(22, 50, 82))
    d.text((108, 856), "HẠNG MỤC CẢI TẠO 06/2004 · CỬA ĐÔNG D-04 (TRỤC CỘT D):", font=font(17, bold=True), fill=(255, 206, 112))
    d.text(
        (108, 882),
        "• Xây bít vĩnh viễn cửa thoát hiểm Đông bằng gạch đặc 220 mm, trát vữa xi măng hai mặt, tháo tay nắm & khung.\n"
        "• Lý do: Theo yêu cầu của đơn vị tài trợ Chương trình B1 — đảm bảo cô lập tuyệt đối, ngăn đối tượng điều trị\n"
        "  rời khỏi buồng thực nghiệm trong trạng thái mất định hướng.",
        font=font(14, bold=False),
        fill=(232, 240, 248),
    )

    d.rectangle((885, 845, 1268, 952), outline=(255, 148, 134), width=3, fill=(38, 52, 80))
    d.text((905, 858), "PHÊ DUYỆT HOÀN CÔNG", font=font(18, bold=True), fill=(255, 166, 154))
    d.text((905, 886), "GIÁM ĐỐC: PHẠM ĐỨC VĨNH", font=font(17, bold=True), fill=(246, 246, 242))
    d.text((905, 914), "Ngày ký: 14/06/2004  ·  Đã nghiệm thu", font=font(15, bold=False), fill=(208, 226, 242))

    out = TEX_DIR / "Doc_Ch05_AsBuilt2004_E05.png"
    img.save(out)
    return out


def build_fire_bell_loop() -> Path:
    """Synthesize a seamless 4.0s institutional hospital fire-alarm bell loop (softened for comfort/accessibility)."""
    rate = 48000
    dur = 4.0
    n = int(rate * dur)
    t = np.arange(n, dtype=np.float64) / rate
    bell = np.zeros(n, dtype=np.float64)

    # Mechanical striker bursts: 2 gentle pulsing rings per 4s cycle with warm metallic partials (no harsh shrillness)
    partials = [(640.0, 0.52), (1285.0, 0.34), (1960.0, 0.22), (2590.0, 0.12)]
    strikes = np.concatenate([
        np.arange(0.05, 1.35, 0.085),
        np.arange(2.05, 3.35, 0.085),
    ])
    hit_t = np.arange(int(0.28 * rate), dtype=np.float64) / rate
    hit_env = np.minimum(hit_t / 0.004, 1.0) * np.exp(-hit_t * 14.5)
    hit_wave = sum(amp * np.sin(2.0 * np.pi * f * hit_t) for f, amp in partials) * hit_env

    for onset in strikes:
        idx = int(onset * rate)
        indices = (idx + np.arange(len(hit_wave))) % n
        bell[indices] += hit_wave

    # Low corridor fire-draft rumble underneath
    rng = np.random.default_rng(20051118)
    freqs = np.fft.rfftfreq(n, 1.0 / rate)
    bed_weights = (freqs**2 / (freqs**2 + 45.0**2)) / (1.0 + (freqs / 260.0) ** 4)
    rumble = np.fft.irfft(np.fft.rfft(rng.normal(size=n)) * bed_weights, n=n)
    rumble /= max(np.std(rumble), 1e-6)

    # Warm corridor FFT bandpass on bell so it stays eerie and atmospheric rather than ear-piercing
    bell_spec = np.fft.rfft(bell)
    hp = (freqs**2) / (freqs**2 + 240.0**2)
    lp = 1.0 / (1.0 + (freqs / 3200.0) ** 4)
    bell_filt = np.fft.irfft(bell_spec * hp * lp, n=n)
    mix = bell_filt * 0.55 + rumble * 0.08
    mix -= np.mean(mix)
    rms_target = 10 ** (-26.0 / 20.0)
    mix *= rms_target / max(np.sqrt(np.mean(mix**2)), 1e-9)
    ceiling = 10 ** (-6.0 / 20.0)
    mix = ceiling * np.tanh(mix / ceiling)

    # Smooth crossfade at loop seam
    seam = int(0.04 * rate)
    fade = np.linspace(0.0, 1.0, seam)
    mix[:seam] = mix[:seam] * fade + mix[-seam:] * (1.0 - fade)

    out = RES_DIR / "CH05_FireBell.wav"
    out.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(out), "wb") as wf:
        wf.setparams((1, 2, rate, len(mix), "NONE", "not compressed"))
        wf.writeframes(np.rint(np.clip(mix, -0.94, 0.94) * 32767).astype("<i2").tobytes())
    return out


def main() -> None:
    TEX_DIR.mkdir(parents=True, exist_ok=True)
    RES_DIR.mkdir(parents=True, exist_ok=True)
    files = [build_posted_map_2005(), build_asbuilt_2004_e05(), build_fire_bell_loop()]
    manifest = {}
    for p in files:
        digest = hashlib.sha256(p.read_bytes()).hexdigest()
        manifest[str(p.relative_to(ROOT)).replace("\\", "/")] = {
            "bytes": p.stat().st_size,
            "sha256": digest,
        }
    print(json.dumps(manifest, indent=2))


if __name__ == "__main__":
    main()
