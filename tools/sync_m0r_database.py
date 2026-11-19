"""Update docs/07_Production_Database.xlsx cleanly with M0-R verified results while preserving all formulas & history."""

import sys
from pathlib import Path
import openpyxl

ROOT = Path(__file__).resolve().parents[1]
XLSX = ROOT / "docs" / "07_Production_Database.xlsx"


def upsert_row(ws, key_col, key_val, values):
    for r in range(2, ws.max_row + 1):
        if ws.cell(r, key_col).value == key_val:
            for c, val in enumerate(values, 1):
                if val is not None:
                    ws.cell(r, c).value = val
            return r
    new_r = ws.max_row + 1
    for c, val in enumerate(values, 1):
        ws.cell(new_r, c).value = val
    return new_r


def main():
    build_id = sys.argv[1] if len(sys.argv) > 1 else "M0R-0.2.0-Windows"
    perf_note = sys.argv[2] if len(sys.argv) > 2 else "Verified on Intel Arc D3D12 1080p"

    wb = openpyxl.load_workbook(XLSX, data_only=False)

    # 1. Research sheet: add R39, R40, R41 (M0-R Deep Research)
    ws_res = wb["Research"]
    upsert_row(ws_res, 1, "R39", [
        "R39",
        "TCVN 4470:1995 (bản trích §4.1–4.6 · LuatMinhKhue / LuatVietnam)",
        "https://tieuchuanxaydung.vsqi.gov.vn/tieuchuan/view?sohieu=TCVN+4470%3A1995",
        "Hành lang chờ 2,7–3,0 m, hành lang bên 2,1–2,4 m, trần 3,60 m, cửa sổ ≥ 20% diện tích sàn (Room 101 đạt 21,2%), bắt buộc kính chớp thông gió tự nhiên.",
        "Chuẩn hóa chi tiết cửa sổ 3 khoang có chớp kính, ô thoáng trên cửa 101 và tỷ lệ phòng bệnh M0-R."
    ])
    upsert_row(ws_res, 1, "R40", [
        "R40",
        "Tổng hợp quy cách nội thất & thiết bị y tế VN 1980–2005 (gạch bông 20×20, cửa gỗ pano kính, quạt trần 3 cánh Ø1400, đèn T8 1m2, tủ sắt kính 2 cánh, phích vỏ sắt, dép tổ ong)",
        "docs/prototype/research.md",
        "Kích thước kỹ thuật (mm) và đặc trưng vật liệu thời kỳ Đổi Mới cho buồng bệnh công lập VN. Toàn bộ nhãn/logo hư cấu theo AR03; niên đại model cụ thể giữ U.",
        "Chuyển trực tiếp thành 36 nguồn Blender 4.5 (.blend/LOD/FBX) và bộ PBR 2048² M0-R."
    ])
    upsert_row(ws_res, 1, "R41", [
        "R41",
        "Cơ chế xuống cấp vữa vôi & bê tông khí hậu nhiệt đới ẩm Việt Nam (mao dẫn, muối hóa efflorescence, rêu mốc)",
        "docs/prototype/research.md",
        "Độ ẩm >80% gây ẩm mao dẫn chân tường 20–45 cm, muối hóa phồng rộp sơn, rêu mốc dọc mép ẩm/cửa sổ và gỉ dưới đinh vít sắt.",
        "Dựng 7 URP DBuffer PBR decals theo quy luật trọng lực và ẩm cục bộ trong lớp R (2012)."
    ])

    # 2. Assets sheet: update M1-LOC03-REF and core Room 101 / Corridor rows with M0-R Blender & PBR provenance
    ws_ast = wb["Assets"]
    m0r_lic = "Tự tạo (Blender 4.5 .blend/FBX + synth WAV) / ambientCG CC0 1.0 / Be Vietnam Pro OFL 1.1"
    for aid in ("CH01-A02", "CH01-A03", "CH01-A07", "CH01-A08", "CH01-A10", "CH02-AS01", "ENV-KIT-01"):
        for r in range(2, ws_ast.max_row + 1):
            if ws_ast.cell(r, 1).value == aid:
                ws_ast.cell(r, 4).value = "M0-R Chuẩn hình ảnh LOC03"
                ws_ast.cell(r, 6).value = m0r_lic
    upsert_row(ws_ast, 1, "M1-LOC03-REF", [
        "M1-LOC03-REF",
        "CH01/CH02",
        "36 nguồn Blender 4.5 (ArtSource/Blender/*.blend + LOD0-2 FBX), 17 bộ PBR 2048² (AO/Smoothness chuẩn), 7 URP DBuffer decals, kính mưa, cây 3D, 3D Inspect, cửa/rèm/tủ vật lý, 19 WAV không gian + occlusion.",
        "M0-R Đã tái dựng & kiểm chứng",
        "Environment / Tech Art",
        "Prototype/ThirdPartyNotices.md"
    ])

    # 3. Decisions sheet: record DEC-M0R-01 visual quality benchmark decision
    ws_dec = wb["Decisions"]
    upsert_row(ws_dec, 1, "DEC-M0R-01", [
        "DEC-M0R-01",
        "PROJECT",
        "M0-R Visual & Gameplay Reconstruction: nâng vùng chuẩn Phòng 101 + hành lang lên asset Blender 4.5 (.blend + LOD0-2 + FBX), PBR hiệu chỉnh không ghi đè smoothness, URP DBuffer Decals, bóng đổ thực đèn cửa sổ/T8/đèn pin, SSAO 8-sample, 2 Reflection Probe 256px, tương tác vật lý cửa/rèm/tủ và 3D Physical Inspector. Giữ nguyên canon v2.2, mọi ID/contract gameplay và cổng review TASK026–028 tại M2.",
        "Áp dụng M0-R"
    ])

    # 4. QA sheet: update verified rows with M0-R Windows x64 build ID & evidence
    ws_qa = wb["QA"]
    pass_ids = {
        "CH01-Q01": "Pass M0-R trên bản Windows x64: nhánh A (D=1, deniedBay), G01 mở, lưu lịch sử đầu.",
        "CH01-Q02": "Pass M0-R trên bản Windows x64: nhánh B giữ D=0, rèm pendingBay tương tác được.",
        "CH01-Q03": "Pass M0-R trên bản Windows x64: nhánh C yêu cầu đủ OBS101A+OBS101B và chọn cặp.",
        "CH01-Q08": "Pass M0-R: 30 lần từ chối tại ngưỡng, 30 lần đổi tại AP04, rollback collider chèn và 10 lần reload.",
        "CH01-Q13": "Pass M0-R: vòng lặp cổng phụ 1 lượt (SEAM), seamBlocker kích hoạt sau CH01-T09.",
        "CH02-Q01": "Pass M0-R: E12 yêu cầu đủ LIST/SOCKETS/WHEELS, lăn tủ sắt 2 cánh mở kho giường.",
        "CH02-Q02": "Pass M0-R: thiếu phiếu chuyển chặn G02; đủ tranh+vòng C01+phiếu chuyển mới xác thực An và mở cấp cứu.",
        "LOG06-Q01": "Pass M0-R: phiếu chuyển 101/4→C01 bắt buộc lấy từ R trong tủ khô kho 101.",
        "V22-Q04": "Pass M0-R: DAG 90 task hợp lệ, 7 file Core đồng bộ; review chuyên gia TASK026–028 giữ ở cổng M2.",
    }
    for r in range(2, ws_qa.max_row + 1):
        qid = ws_qa.cell(r, 1).value
        if qid in pass_ids:
            ws_qa.cell(r, 5).value = "Pass"
            ws_qa.cell(r, 6).value = build_id
            ws_qa.cell(r, 7).value = pass_ids[qid] + f" ({perf_note}). docs/prototype/verification.md"
        elif qid == "V22-Q03":
            ws_qa.cell(r, 6).value = build_id
            ws_qa.cell(r, 7).value = "Xuất thoại theo Chapter; dữ liệu CH01/02 chặn D16b và CH04-D06. Phần CH04/07 còn mở ở M2/M3. docs/prototype/verification.md"

    # 5. Changelog sheet: append M0-R entry
    ws_cl = wb["Changelog"]
    upsert_row(ws_cl, 1, f"v2.2 / {build_id}", [
        f"v2.2 / {build_id}",
        "19/11/2026",
        "M0-R Visual & Gameplay Reconstruction",
        f"Tái dựng Phòng 101 & hành lang bằng 36 asset Blender 4.5, 17 bộ PBR 2048², 7 URP DBuffer decals, bóng đổ đa nguồn, 3D Physical Inspector, cửa/rèm/tủ vật lý và âm thanh không gian 3 reverb + occlusion. {perf_note}. Xem docs/prototype/verification.md.",
        "Đã kiểm chứng bản Windows x64"
    ])

    wb.save(XLSX)
    print(f"Updated {XLSX} for build {build_id}")


if __name__ == "__main__":
    main()
