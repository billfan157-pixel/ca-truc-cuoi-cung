"""Package a verified M2 Vertical Slice Windows x64 standalone without editor logs or debug symbols."""

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


def read_identity(path):
    return dict(line.split('=', 1) for line in path.read_text(encoding='utf-8-sig').splitlines() if '=' in line)


def member(name):
    entry = ZipInfo(name, (2026, 11, 19, 0, 0, 0))
    entry.compress_type = ZIP_DEFLATED
    return entry


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('build', type=Path)
    parser.add_argument('archive', type=Path)
    parser.add_argument('--allow-dirty', action='store_true',
                        help='Allow packaging before final git commit (only for staging checks).')
    parser.add_argument('--evidence-identity', type=Path,
                        help='Commit-linked identity checked against the original build and executable.')
    parser.add_argument('--label')
    args = parser.parse_args()
    build, archive = args.build.resolve(), args.archive.resolve()
    original_identity = build / 'build-identity.txt'
    identity = read_identity(original_identity)
    if identity.get('development') != 'False' or digest(build / DLL) != identity.get('assembly_sha256'):
        raise SystemExit('Packaging requires a verified Windows release build matching build-identity.txt.')
    published_identity = args.evidence_identity or original_identity
    revision = identity.get('revision', 'unknown')
    if args.evidence_identity:
        evidence = read_identity(published_identity)
        relevant = {'unity', 'development', 'assembly_sha256'} | {
            key for key in identity if key.startswith('scene_') and key.endswith('_sha256')
        }
        if (any(evidence.get(key) != identity.get(key) for key in relevant)
                or evidence.get('exe_sha256') != digest(build / 'CaTrucCuoiCung.exe')
                or not evidence.get('source_commit')):
            raise SystemExit('Evidence identity does not match this Windows player.')
        revision = evidence['source_commit']
    elif not args.allow_dirty and identity.get('prototype_diff'):
        raise SystemExit('Packaging requires a clean git tree under Prototype/ (commit first or pass --allow-dirty for staging).')

    title = args.label or 'M2 VERTICAL SLICE'
    version = args.label or '0.3.0-m2'
    guide = f"""CA TRỰC CUỐI CÙNG · {title} · Windows x64
Bản dựng {version} · {revision[:12]}
Phạm vi: CH01 cô đọng & đầu CH02 (LOC01–LOC04: Cổng/Sân viện, Sảnh lễ tân, Phòng 101 & Kho lưu, Cấp cứu).

Giải nén cả gói vào cùng một thư mục và mở CaTrucCuoiCung.exe.
Mức Medium 1080p/60 Hz mặc định, có thể đổi Low/Medium/High trong Cài đặt & Trợ năng.

ĐIỀU KHIỂN CHÍNH:
WASD / chuột     Di chuyển / nhìn
Shift            Chạy nhanh (không bắt buộc — mọi tình huống đều có thể đi bộ thoát)
E (hoặc K)       Tương tác / thu thập vật chứng / soi hồ sơ
Tab (hoặc J)     Mở Sổ trực (Hồ sơ, Vật chứng & Bản ghi lời thoại)
Q                Mở Điểm neo Phòng 101 từ CH02 để đổi chiều R/B hoặc sửa quyết định F101
F                Đèn pin (có pin dự phòng tại quầy trực)
Esc              Đóng trang hoặc tạm dừng

SOI 3D VẬT CHỨNG:
Kéo chuột hoặc WASD để xoay, lăn chuột hoặc Q/Z để thu phóng.

TRỢ NĂNG & LƯU TRỮ:
Cài đặt hỗ trợ: Phím thay thế (K/J), Giảm chuyển động, Cỡ chữ 130%, Độ sáng,
Âm lượng tổng / Âm nền / Lời thoại / Nhạc, Mono, Phụ đề định hướng môi trường,
Gợi ý tiến trình (60/120/180 giây) và Chế độ Cốt truyện.
Lưu tay tại bàn trực; tự lưu nguyên tử (atomic save) bám các mốc của ca trực.
"""
    notices = (ROOT / 'Prototype/ThirdPartyNotices.md').read_text(encoding='utf-8').replace(
        'Assets/CTCC/Art/Resources/CTCC/OFL.txt', 'BeVietnamPro-OFL.txt')
    extras = {
        'HUONG-DAN.txt': guide.encode('utf-8-sig'),
        'ThirdPartyNotices.md': notices.encode('utf-8'),
        'BeVietnamPro-OFL.txt': (ROOT / 'Prototype/Assets/CTCC/Art/Resources/CTCC/OFL.txt').read_bytes(),
        'BUILD.txt': published_identity.read_bytes(),
    }
    archive.parent.mkdir(parents=True, exist_ok=True)
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
