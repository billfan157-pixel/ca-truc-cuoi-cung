"""Export the vertical slice dialogue and the guarded beat contracts from Core 07."""

import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "docs" / "agent"))
from project_docs import read_workbook


def run():
    sheet = read_workbook()["Dialogue"]
    entries = []
    conditions = []
    known = set()
    locked = {"CH01-D16b", "CH04-D06"}
    staff_reactions = {"CH01-D17": "OBS_CORRIDOR_NOTICE", "CH01-D18": "OBS_SHIFT_LOG"}
    exception_monologues = {"CH01-D19": "CH01-EX01"}
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
        if row.get("B") not in ("CH01", "CH02", "CH03", "CH04"):
            continue
        if identity in known:
            raise ValueError(f"Duplicate dialogue ID {identity}")
        known.add(identity)
        entries.append({
            "id": identity, "chapter": row["B"], "speaker": row.get("C", ""),
            "context": row.get("D", ""), "copy": row.get("E", ""),
        })
    guarded = locked | staff_reactions.keys() | exception_monologues.keys()
    assert guarded <= known and {condition["id"] for condition in conditions} == guarded
    assert {
        "CH01-D03", "CH01-D03b", "CH01-D03c", "CH01-D04", "CH01-D04c",
        "CH01-D05", "CH01-D05b", "CH01-D05c", "CH01-D07", "CH01-D07b", "CH01-D15", "CH01-D16", "CH01-D19",
        "CH02-D04", "CH03-D01", "CH03-D02", "CH03-D03", "CH03-D04", "CH03-D05", "CH03-D06",
    } <= known
    dest = ROOT / "Prototype/Assets/CTCC/Art/Resources/CTCC/Dialogue.json"
    dest.parent.mkdir(parents=True, exist_ok=True)
    dest.write_text(json.dumps({"entries": entries, "conditions": conditions}, ensure_ascii=False, indent=2) + "\n",
                    encoding="utf-8")
    print(f"{len(entries)} CH01–04 rows from Core 07 Dialogue, validated by Chapter and stable ID")


if __name__ == "__main__":
    run()
