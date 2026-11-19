"""Export the M1 dialogue subset from the Core 07 master sheet."""

import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "docs" / "agent"))
from project_docs import read_workbook


def run():
    sheet = read_workbook()["Dialogue"]
    entries = []
    known = set()
    for number, row in sheet.items():
        if number == 1 or row.get("B") not in ("CH01", "CH02"):
            continue
        identity = row["A"]
        if identity in known:
            raise ValueError(f"Duplicate dialogue ID {identity}")
        known.add(identity)
        entries.append({
            "id": identity, "chapter": row["B"], "speaker": row.get("C", ""),
            "context": row.get("D", ""), "copy": row.get("E", ""),
        })
    assert "CH01-D16b" not in known
    assert {"CH01-D03", "CH01-D04", "CH01-D05", "CH01-D07", "CH01-D15", "CH01-D16", "CH02-D04"} <= known
    dest = ROOT / "Prototype/Assets/CTCC/Art/Resources/CTCC/Dialogue.json"
    dest.parent.mkdir(parents=True, exist_ok=True)
    dest.write_text(json.dumps({"entries": entries}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"{len(entries)} CH01/02 rows from Core 07 Dialogue, validated by Chapter and stable ID")


if __name__ == "__main__":
    run()
