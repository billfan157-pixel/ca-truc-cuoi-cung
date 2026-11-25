"""Export the vertical slice and M3 chapter dialogue + guarded beat contracts from Core 07."""

import argparse
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "docs" / "agent"))
from project_docs import DESIGN_LOCK_REV, SOURCES, read_workbook, workbook_provenance

EXPORTED_CHAPTERS = ("CH01", "CH02", "CH03", "CH04", "CH05")


def run(rev: str | None = DESIGN_LOCK_REV) -> None:
    source_rev, source_sha256 = workbook_provenance(rev)
    sheet = read_workbook(rev=rev)["Dialogue"]
    entries = []
    conditions = []
    known = set()
    locked = {"CH01-D16b", "CH04-D06"}
    staff_reactions = {"CH01-D17": "OBS_CORRIDOR_NOTICE", "CH01-D18": "OBS_SHIFT_LOG"}
    exception_monologues = {"CH01-D19": "CH01-EX01"}
    ch05_guarded = {
        "CH05-D04": ("B05", "Sau nhóm 3", ["CH05-T04"]),
        "CH05-D06": ("B07", "E05b", ["CH05-T05"]),
        "CH05-D08": ("B06", "B06", ["ValveSabotageFound"]),
    }
    for number, row in sheet.items():
        if number == 1:
            continue
        identity = row["A"]
        if identity in locked:
            chapter, context = row["B"], row["D"]
            if chapter != "CH04" or any(marker not in context for marker in ("B06", "H05 verified", "PT02 presented")):
                raise ValueError(f"Core 07 conditions changed for {identity}: {chapter} / {context}")
            conditions.append({"id": identity, "chapter": chapter, "beat": "B06",
                               "requiredEvents": ["H05.verified", "PT02.presented"]})
        if identity in staff_reactions:
            chapter, context = row["B"], row["D"]
            if chapter != "CH01" or any(marker not in context for marker in ("STAFF", "lớp B", staff_reactions[identity])):
                raise ValueError(f"Core 07 conditions changed for {identity}: {chapter} / {context}")
            conditions.append({"id": identity, "chapter": chapter, "beat": "STAFF",
                               "requiredEvents": ["CH01-T03"]})
        if identity in exception_monologues:
            chapter, context = row["B"], row["D"]
            if chapter != "CH01" or any(marker not in context for marker in ("B05", "03:17", exception_monologues[identity])):
                raise ValueError(f"Core 07 conditions changed for {identity}: {chapter} / {context}")
            conditions.append({"id": identity, "chapter": chapter, "beat": "B05",
                               "requiredEvents": ["CH01-T07", "CH01-EX01"]})
        if identity in ch05_guarded:
            chapter = row["B"]
            beat, marker, req_events = ch05_guarded[identity]
            if chapter != "CH05" or marker not in row.get("D", ""):
                raise ValueError(f"Core 07 conditions changed for {identity}: {chapter} / {row.get('D', '')}")
            conditions.append({"id": identity, "chapter": chapter, "beat": beat,
                               "requiredEvents": req_events})
        if row.get("B") not in EXPORTED_CHAPTERS:
            continue
        if identity in known:
            raise ValueError(f"Duplicate dialogue ID {identity}")
        known.add(identity)
        entries.append({
            "id": identity, "chapter": row["B"], "speaker": row.get("C", ""),
            "context": row.get("D", ""), "copy": row.get("E", ""),
        })
    guarded = locked | staff_reactions.keys() | exception_monologues.keys() | ch05_guarded.keys()
    assert guarded <= known and {condition["id"] for condition in conditions} == guarded
    assert {
        "CH01-D03", "CH01-D03b", "CH01-D03c", "CH01-D04", "CH01-D04c",
        "CH01-D05", "CH01-D05b", "CH01-D05c", "CH01-D07", "CH01-D07b", "CH01-D15", "CH01-D16", "CH01-D19",
        "CH02-D04", "CH03-D01", "CH03-D02", "CH03-D03", "CH03-D04", "CH03-D05", "CH03-D06",
        "CH05-D01", "CH05-D02", "CH05-D03", "CH05-D04", "CH05-D05", "CH05-D06", "CH05-D07", "CH05-D08",
    } <= known
    payload = {
        "sourceRevision": source_rev,
        "sourceFile": f"docs/{SOURCES['07']}",
        "sourceSha256": source_sha256,
        "entries": entries,
        "conditions": conditions,
    }
    dest = ROOT / "Prototype/Assets/CTCC/Art/Resources/CTCC/Dialogue.json"
    dest.parent.mkdir(parents=True, exist_ok=True)
    dest.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(
        f"{len(entries)} CH01–05 rows exported from {payload['sourceFile']} @ {source_rev[:12]} "
        f"(sha256={source_sha256[:16]}...), validated by Chapter and stable ID"
    )


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    group = parser.add_mutually_exclusive_group()
    group.add_argument(
        "--rev",
        default=DESIGN_LOCK_REV,
        help=f"Git revision của Core 07 để xuất thoại (mặc định: Design Lock {DESIGN_LOCK_REV[:7]}).",
    )
    group.add_argument(
        "--working-tree",
        action="store_true",
        help="Chỉ dùng khi kiểm thử thủ công file docs/07 trên working tree.",
    )
    args = parser.parse_args()
    run(None if args.working_tree else args.rev)


if __name__ == "__main__":
    main()
