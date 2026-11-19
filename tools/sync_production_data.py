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
        if row.get("B") not in ("CH01", "CH02"):
            continue
        if identity in known:
            raise ValueError(f"Duplicate dialogue ID {identity}")
        known.add(identity)
        entries.append({
            "id": identity, "chapter": row["B"], "speaker": row.get("C", ""),
            "context": row.get("D", ""), "copy": row.get("E", ""),
        })
    assert not locked & known and {condition["id"] for condition in conditions} == locked
    assert {"CH01-D03", "CH01-D04", "CH01-D05", "CH01-D07", "CH01-D15", "CH01-D16", "CH02-D04"} <= known
    dest = ROOT / "Prototype/Assets/CTCC/Art/Resources/CTCC/Dialogue.json"
    dest.parent.mkdir(parents=True, exist_ok=True)
    dest.write_text(json.dumps({"entries": entries, "conditions": conditions}, ensure_ascii=False, indent=2) + "\n",
                    encoding="utf-8")
    print(f"{len(entries)} CH01/02 rows from Core 07 Dialogue, validated by Chapter and stable ID")


if __name__ == "__main__":
    run()
