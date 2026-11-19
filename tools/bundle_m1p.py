"""Package a verified M1-P standalone without editor logs or debug symbols."""

import argparse
from hashlib import file_digest
from pathlib import Path
import shutil
from zipfile import ZIP_DEFLATED, ZipFile, ZipInfo


ROOT = Path(__file__).resolve().parents[1]
DLL = 'CaTrucCuoiCung_Data/Managed/Assembly-CSharp.dll'


def digest(path):
    with path.open('rb') as stream:
        return file_digest(stream, 'sha256').hexdigest()


def member(name):
    entry = ZipInfo(name, (2026, 11, 19, 0, 0, 0))
    entry.compress_type = ZIP_DEFLATED
    return entry


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('build', type=Path)
    parser.add_argument('archive', type=Path)
    args = parser.parse_args()
    build, archive = args.build.resolve(), args.archive.resolve()
    identity = dict(line.split('=', 1) for line in (build / 'build-identity.txt').read_text(encoding='utf-8-sig').splitlines())
    if identity['development'] != 'False' or identity['prototype_diff'] or digest(build / DLL) != identity['assembly_sha256']:
        raise SystemExit('Packaging requires a clean, identified Windows release build.')

    guide = f"""CA TRỰC CUỐI CÙNG · M1-P · Windows x64
Bản dựng 0.2.1 · {identity['revision'][:12]}

Giải nén cả gói vào cùng một thư mục và mở CaTrucCuoiCung.exe.
Mức Medium mặc định, có thể đổi Low/Medium/High trong Cài đặt & Trợ năng.

WASD / chuột     Di chuyển / nhìn
E                Tương tác
Tab / Enter      Mở sổ trực / chọn hành động
Q                Mở điểm neo phòng 101 từ CH02
F                Đèn pin
Esc              Đóng trang hoặc tạm dừng
Soi 3D           Kéo chuột hoặc WASD để xoay, lăn chuột hoặc Q/Z để thu phóng

Cài đặt có điều khiển thay thế, giảm chuyển động, cỡ chữ 130%, độ sáng,
âm lượng tổng/âm nền, mono và phụ đề môi trường.
Lưu tay tại bàn trực. Tự lưu bám các mốc của ca trực.
"""
    notices = (ROOT / 'Prototype/ThirdPartyNotices.md').read_text(encoding='utf-8').replace(
        'Assets/CTCC/Art/Resources/CTCC/OFL.txt', 'BeVietnamPro-OFL.txt')
    extras = {'HUONG-DAN.txt': guide.encode('utf-8-sig'), 'ThirdPartyNotices.md': notices.encode(),
              'BeVietnamPro-OFL.txt': (ROOT / 'Prototype/Assets/CTCC/Art/Resources/CTCC/OFL.txt').read_bytes(),
              'BUILD.txt': (build / 'build-identity.txt').read_bytes()}
    temporary = archive.with_suffix('.zip.tmp')
    try:
        with ZipFile(temporary, 'w') as bundle:
            for path in sorted(build.rglob('*')):
                relative = path.relative_to(build)
                if (not path.is_file() or any('DoNotShip' in part for part in relative.parts) or
                        path.suffix.lower() in {'.log', '.pdb'} or path.name == 'build-identity.txt'):
                    continue
                with path.open('rb') as source, bundle.open(member(relative.as_posix()), 'w') as destination:
                    shutil.copyfileobj(source, destination, 1024 * 1024)
            for name, content in extras.items():
                bundle.writestr(member(name), content)
        with ZipFile(temporary) as validation:
            names = set(validation.namelist())
            if not {'CaTrucCuoiCung.exe', 'UnityPlayer.dll', DLL, *extras} <= names or validation.testzip():
                raise ValueError('The release archive failed verification.')
            if not all(any(name.startswith(prefix) for name in names) for prefix in ('MonoBleedingEdge/', 'D3D12/')):
                raise ValueError('The release archive is missing Windows player dependencies.')
        temporary.replace(archive)
    finally:
        temporary.unlink(missing_ok=True)
    print(f'{archive}\nfiles={len(names)} bytes={archive.stat().st_size}\nsha256={digest(archive)}')


if __name__ == '__main__':
    main()
