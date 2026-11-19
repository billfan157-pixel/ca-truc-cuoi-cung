"""Package playable Ca Truc Cuoi Cung M0-R Windows x64 build into a verified distributable ZIP."""
from hashlib import file_digest
from pathlib import Path
from zipfile import ZIP_DEFLATED, ZipFile

root = Path(__file__).resolve().parent.parent
out = root / "output"
source = out / "CTCC-M0R"
archive = out / "CaTrucCuoiCung-M0R-Windows.zip"
temporary = archive.with_suffix(".zip.tmp")
game_dll = "CaTrucCuoiCung_Data/Managed/Assembly-CSharp.dll"
must_ship = {"CaTrucCuoiCung.exe", "UnityPlayer.dll", game_dll, "HUONG-DAN.txt", "ThirdPartyNotices.md"}

dll_path = source / game_dll
with dll_path.open("rb") as stream:
    dll_sha = file_digest(stream, "sha256").hexdigest()
build_id = f"M0R-0.2.0-{dll_sha[:12]}"

scene_path = root / "Prototype/Assets/CTCC/Scenes/AnSinh.unity"
with scene_path.open("rb") as stream:
    scene_sha = file_digest(stream, "sha256").hexdigest()

guide = f"""CA TRỰC CUỐI CÙNG · M0-R (Visual & Gameplay Reconstruction) · 0.2.0
19/11/2026 · Windows x64 · Unity 6.3 LTS · URP 17.3
Bản dựng {build_id}

Giải nén cả gói vào một thư mục và mở CaTrucCuoiCung.exe.
Giữ thư mục CaTrucCuoiCung_Data và các DLL ở cạnh tệp EXE.

WASD và chuột    Di chuyển và nhìn (có cảm giác trọng lượng & bước chân theo bề mặt)
E                Tương tác vật lý (mở/đóng cửa gỗ phòng 101, kéo rèm, đẩy tủ, soi vật chứng 3D)
Trong soi 3D     Kéo chuột hoặc WASD để xoay vật chứng · Lăn chuột / Q-Z để thu phóng
Tab              Mở sổ trực. Trong bảng, Tab hoặc mũi tên chuyển nút
Enter            Chọn nút đang trỏ
Q                Mở điểm neo tại vạch đồng phòng 101 (từ CH02)
F                Đèn pin (có trễ tay cầm, bóng đổ thật và cảnh báo pin yếu)
Shift / Ctrl     Chạy / cúi
Esc              Đóng trang đang đọc / thoát soi 3D hoặc mở menu

Cài đặt hỗ trợ cách phím thay thế (Mũi tên / K / L / J), tăng cỡ chữ (đến 135%),
giảm chuyển động, tăng độ sáng, âm thanh mono, phụ đề âm thanh môi trường,
độ nhạy chuột và chỉnh riêng âm lượng tổng / nền / hiệu ứng.
Tự lưu chạy tại các mốc trong ca. Bàn trực ở lễ tân có lưu tay.
"""

notices = (root / "Prototype/ThirdPartyNotices.md").read_text(encoding="utf-8")
notices = notices.replace("Assets/CTCC/Art/Resources/CTCC/OFL.txt", "BeVietnamPro-OFL.txt")

with ZipFile(temporary, "w", compression=ZIP_DEFLATED, compresslevel=6) as bundle:
    for entry in sorted(source.rglob("*")):
        relative = entry.relative_to(source)
        if entry.is_file() and not any("DoNotShip" in part for part in relative.parts):
            bundle.write(entry, relative.as_posix())
    bundle.writestr("HUONG-DAN.txt", guide.encode("utf-8-sig"))
    bundle.writestr("ThirdPartyNotices.md", notices.encode("utf-8"))
    bundle.write(root / "Prototype/Assets/CTCC/Art/Resources/CTCC/OFL.txt", "BeVietnamPro-OFL.txt")

with ZipFile(temporary) as bundle:
    members = bundle.namelist()
    assert must_ship.issubset(members), f"Missing required files: {must_ship - set(members)}"
    assert any(name.startswith("MonoBleedingEdge/") for name in members)
    assert any(name.startswith("D3D12/") for name in members)
    assert not any("DoNotShip" in name for name in members)
    assert bundle.testzip() is None

temporary.replace(archive)
with archive.open("rb") as stream:
    zip_sha = file_digest(stream, "sha256").hexdigest()

print(f"BUILD_ID={build_id}")
print(f"ZIP={archive.resolve()}")
print(f"MEMBERS={len(members)} BYTES={archive.stat().st_size}")
print(f"ZIP_SHA256={zip_sha}")
print(f"DLL_SHA256={dll_sha}")
print(f"SCENE_SHA256={scene_sha}")
