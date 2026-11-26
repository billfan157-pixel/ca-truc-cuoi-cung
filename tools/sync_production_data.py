"""Export the Core 07 dialogue + guarded beat contracts for the Unity runtime.

Core v3.3.3–v3.6.2 (DEC-VO-01 / DEC-V34..V361) adds the presentation contract:
`Presentation Mode`, `Needs VO`, `Runtime Display Copy (Silent Mode)` and
`UI Style / Audio Rule`. The canonical runtime projection is shipped as
`docs/dialogue_silent_exploration_runtime.json`; this script validates that
projection against the workbook and writes the compact Unity resource.
"""

import argparse
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "docs" / "agent"))
from project_docs import SOURCES, read_workbook, workbook_provenance

EXPORTED_CHAPTERS = ("CH01", "CH02", "CH03", "CH04", "CH05")
CANONICAL_RUNTIME = ROOT / "docs" / "dialogue_silent_exploration_runtime.json"
DEST = ROOT / "Prototype/Assets/CTCC/Art/Resources/CTCC/Dialogue.json"


def load_presentation() -> dict:
    payload = json.loads(CANONICAL_RUNTIME.read_text(encoding="utf-8-sig"))
    decision_ref = str(payload.get("decisionRef", ""))
    if "DEC-VO-01" not in decision_ref:
        raise ValueError(f"Canonical dialogue projection does not include DEC-VO-01: {decision_ref!r}")
    lines = payload.get("lines") or []
    if len(lines) != 143:
        raise ValueError(f"Canonical dialogue projection has {len(lines)} lines, expected 143")
    by_id = {}
    for line in lines:
        identity = str(line.get("id", ""))
        if not identity or identity in by_id:
            raise ValueError(f"Duplicate or empty canonical dialogue ID: {identity!r}")
        by_id[identity] = line
    return payload, by_id


def variant_rows(line: dict) -> list[dict]:
    variants = line.get("variants") or []
    return [
        {
            "variantId": variant.get("variantId", ""),
            "branch": variant.get("branch", ""),
            "beat": variant.get("beat", ""),
            "presentationMode": variant.get("presentationMode", line.get("presentationMode", "")),
            "needsVO": bool(variant.get("needsVO", line.get("needsVO", False))),
            "displaySpeaker": variant.get("displaySpeaker", line.get("displaySpeaker", "")),
            "showSpeakerPrefix": bool(variant.get("showSpeakerPrefix", line.get("showSpeakerPrefix", False))),
            "isItalicSilentThought": bool(variant.get("isItalicSilentThought", line.get("isItalicSilentThought", False))),
            "runtimeCopy": variant.get("runtimeCopy", line.get("runtimeCopy", "")),
            "uiAudioRule": variant.get("uiAudioRule", line.get("uiAudioRule", "")),
        }
        for variant in variants
    ]


def guarded_conditions(sheet: dict) -> list[dict]:
    conditions = []
    locked = {"CH01-D16b", "CH04-D06"}
    staff_reactions = {"CH01-D17": "OBS_CORRIDOR_NOTICE", "CH01-D18": "OBS_SHIFT_LOG"}
    exception_monologues = {"CH01-D19": "CH01-EX01"}
    ch05_guarded = {
        "CH05-D04": ("B05", "Sau nhóm 3", ["CH05-T04"]),
        "CH05-D06": ("B07", "E05b", ["CH05-T05"]),
        "CH05-D08": ("B06", "B06", ["ValveSabotageFound"]),
    }
    for row in sheet.values():
        identity = row.get("A")
        if identity in locked:
            chapter, context = row.get("B"), row.get("D", "")
            if chapter != "CH04" or any(marker not in context for marker in ("B06", "H05 verified", "PT02 presented")):
                raise ValueError(f"Core 07 conditions changed for {identity}: {chapter} / {context}")
            conditions.append({"id": identity, "chapter": chapter, "beat": "B06",
                               "requiredEvents": ["H05.verified", "PT02.presented"]})
        if identity in staff_reactions:
            chapter, context = row.get("B"), row.get("D", "")
            if chapter != "CH01" or any(marker not in context for marker in ("STAFF", "lớp B", staff_reactions[identity])):
                raise ValueError(f"Core 07 conditions changed for {identity}: {chapter} / {context}")
            conditions.append({"id": identity, "chapter": chapter, "beat": "STAFF",
                               "requiredEvents": ["CH01-T03"]})
        if identity in exception_monologues:
            chapter, context = row.get("B"), row.get("D", "")
            if chapter != "CH01" or any(marker not in context for marker in ("B05", "03:17", exception_monologues[identity])):
                raise ValueError(f"Core 07 conditions changed for {identity}: {chapter} / {context}")
            conditions.append({"id": identity, "chapter": chapter, "beat": "B05",
                               "requiredEvents": ["CH01-T07", "CH01-EX01"]})
        if identity in ch05_guarded:
            chapter = row.get("B")
            beat, marker, req_events = ch05_guarded[identity]
            if chapter != "CH05" or marker not in row.get("D", ""):
                raise ValueError(f"Core 07 conditions changed for {identity}: {chapter} / {row.get('D', '')}")
            conditions.append({"id": identity, "chapter": chapter, "beat": beat,
                               "requiredEvents": req_events})
    return conditions


def run(rev: str | None = None) -> None:
    source_rev, source_sha256 = workbook_provenance(rev)
    workbook = read_workbook(rev=rev)["Dialogue"]
    sheet = {row["A"]: row for number, row in workbook.items() if number != 1 and row.get("A")}
    conditions = guarded_conditions(sheet)

    if rev is not None:
        # Historical export: only the legacy columns existed before Core v3.3.3.
        entries = []
        for identity, row in sheet.items():
            if row.get("B") not in EXPORTED_CHAPTERS:
                continue
            entries.append({
                "id": identity, "chapter": row.get("B", ""), "speaker": row.get("C", ""),
                "context": row.get("D", ""), "copy": row.get("E", ""),
                "presentationMode": "VO_DIALOGUE", "needsVO": True,
                "displaySpeaker": row.get("C", ""), "showSpeakerPrefix": True,
                "isItalicSilentThought": False, "runtimeCopy": row.get("E", ""),
                "legacyCopy": row.get("E", ""), "uiAudioRule": "", "variants": [],
            })
    else:
        canonical, by_id = load_presentation()
        if set(by_id) != set(sheet):
            missing = sorted(set(sheet) - set(by_id))
            extra = sorted(set(by_id) - set(sheet))
            raise ValueError(f"Canonical/workbook ID mismatch; missing={missing[:8]} extra={extra[:8]}")
        entries = []
        for identity, line in by_id.items():
            row = sheet[identity]
            if line.get("chapter") != row.get("B"):
                raise ValueError(f"Chapter mismatch for {identity}: {line.get('chapter')} / {row.get('B')}")
            if line.get("legacyCopy", "") != row.get("E", ""):
                raise ValueError(f"Legacy copy mismatch for {identity}")
            if line.get("presentationMode") not in {
                "VO_DIALOGUE", "VO_REACTION_SFX", "INNER_SUB_SILENT", "CUT_TO_JOURNAL", "UI_NON_VO"
            }:
                raise ValueError(f"Unknown presentation mode for {identity}: {line.get('presentationMode')}")
            entries.append({
                "id": identity,
                "chapter": line.get("chapter", ""),
                "speaker": line.get("speaker", ""),
                "displaySpeaker": line.get("displaySpeaker", ""),
                "context": line.get("context", ""),
                "presentationMode": line.get("presentationMode", ""),
                "needsVO": bool(line.get("needsVO", False)),
                "showSpeakerPrefix": bool(line.get("showSpeakerPrefix", False)),
                "isItalicSilentThought": bool(line.get("isItalicSilentThought", False)),
                "runtimeCopy": line.get("runtimeCopy", ""),
                "legacyCopy": line.get("legacyCopy", ""),
                "pocketNoteText": line.get("pocketNoteText", ""),
                "stageDirection": line.get("stageDirection", ""),
                "timecodeCard": line.get("timecodeCard", ""),
                "uiAudioRule": line.get("uiAudioRule", ""),
                "variants": variant_rows(line),
            })
        payload_meta = {
            "schemaVersion": canonical.get("schemaVersion", ""),
            "designRevision": canonical.get("designRevision", ""),
            "decisionRef": canonical.get("decisionRef", ""),
            "uiPresentationSpec": canonical.get("uiPresentationSpec", {}),
            "ambientBarks": canonical.get("ambientBarks", []),
            "ch01WorldFirst": canonical.get("ch01WorldFirst", {}),
        }

    if rev is None:
        payload = {
            **payload_meta,
            "sourceRevision": source_rev,
            "sourceFile": f"docs/{SOURCES['07']}",
            "sourceSha256": source_sha256,
            "entries": entries,
            "conditions": conditions,
        }
    else:
        payload = {
            "sourceRevision": source_rev,
            "sourceFile": f"docs/{SOURCES['07']}",
            "sourceSha256": source_sha256,
            "entries": entries,
            "conditions": conditions,
        }

    DEST.parent.mkdir(parents=True, exist_ok=True)
    DEST.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(
        f"{len(entries)} dialogue rows exported from {payload['sourceFile']} @ {source_rev[:12]} "
        f"(sha256={source_sha256[:16]}...), presentation={payload.get('decisionRef', 'legacy')}"
    )


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    group = parser.add_mutually_exclusive_group()
    group.add_argument(
        "--rev",
        default=None,
        help="Git revision lịch sử của Core 07 để xuất thoại (mặc định: đọc Core v3.6.2 trên working tree).",
    )
    group.add_argument(
        "--working-tree",
        action="store_true",
        help="Đọc trực tiếp docs/07_Production_Database.xlsx trên working tree (hành vi mặc định).",
    )
    args = parser.parse_args()
    run(None if args.working_tree else args.rev)


if __name__ == "__main__":
    main()
