"""Read the current Ca trực cuối cùng design sources and production backlog."""

import argparse
import hashlib
import posixpath
import re
import sys
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from graphlib import CycleError, TopologicalSorter
from pathlib import Path
from typing import Optional
from zipfile import BadZipFile, ZipFile


DOCS = Path(__file__).resolve().parents[1]
SOURCES = {
    "01": "01_Game_Design_Document.docx",
    "02": "02_Narrative_Bible.docx",
    "03": "03_World_Architecture_Bible.docx",
    "04": "04_Level_Chapter_Design.docx",
    "05": "05_Art_Audio_Bible.docx",
    "06": "06_Technical_Design_Document.docx",
    "07": "07_Production_Database.xlsx",
}
SHEET_KEYS = {
    "Chapter gates": "Chapter",
    "Events": "ID",
    "Dialogue": "ID",
    "Puzzle specs": "ID",
    "Evidence Specs": "ID",
    "Patients": "ID",
    "Timeline": "ID",
    "Reveal order": "Chương",
    "Canon checks": "ID",
    "Plot twists": "ID",
    "Foreshadowing": "FS ID",
    "Payoff": "ID",
    "Narrative QA": "ID",
    "QA": "ID",
    "Assets": "ID",
    "Decisions": "ID",
    "Research": "ID",
    "Backlog": "ID",
    "Locations": "LOC",
    "Roadmap": "Milestone",
    "Canon Matrix": "Mốc / ID",
    "Provenance Matrix": "ID",
    "PZ10 Matrix": "Vòng",
    "Ending Matrix": "END",
    "Mystery Matrix": "Câu hỏi",
}
WORD = "{http://schemas.openxmlformats.org/wordprocessingml/2006/main}"
SHEET = "{http://schemas.openxmlformats.org/spreadsheetml/2006/main}"
OFFICE_REL = "{http://schemas.openxmlformats.org/officeDocument/2006/relationships}"
COMPLETE = "hoàn thành"


class SourceError(ValueError):
    """The source is missing data or uses a structure this reader cannot resolve."""


@dataclass(frozen=True)
class WordBlock:
    text: str
    heading: Optional[int] = None


@dataclass(frozen=True)
class Task:
    row: int
    milestone: str
    description: str
    dependencies: tuple[str, ...]
    priority: str
    status: str

    @property
    def complete(self) -> bool:
        return self.status.casefold() == COMPLETE


def paragraph_text(paragraph: ET.Element) -> str:
    pieces = []
    for node in paragraph.iter():
        if node.tag == WORD + "t":
            pieces.append(node.text or "")
        elif node.tag in (WORD + "br", WORD + "tab"):
            pieces.append(" ")
        elif node.tag == WORD + "drawing":
            pieces.append(" [Hình minh họa trong bản Word] ")
    return "".join(pieces).strip()


def word_blocks(path: Path) -> list[WordBlock]:
    with ZipFile(path) as package:
        body = ET.fromstring(package.read("word/document.xml")).find(WORD + "body")
    if body is None:
        raise SourceError(f"{path.name} thiếu phần nội dung Word.")

    blocks = []
    for node in body:
        if node.tag == WORD + "p":
            text = paragraph_text(node)
            if text:
                style = node.find(f"{WORD}pPr/{WORD}pStyle")
                style_id = style.get(WORD + "val", "") if style is not None else ""
                heading = re.fullmatch(r"Heading(\d+)", style_id)
                level = int(heading[1]) if heading else None
                blocks.append(WordBlock(text, level))
        elif node.tag == WORD + "tbl":
            for row in node.findall(WORD + "tr"):
                cells = [
                    " / ".join(filter(None, map(paragraph_text, cell.findall(WORD + "p"))))
                    for cell in row.findall(WORD + "tc")
                ]
                if any(cells):
                    blocks.append(WordBlock(" | ".join(cells)))
    return blocks


def select_section(blocks: list[WordBlock], query: str) -> list[WordBlock]:
    query = query.strip().casefold()
    if not query:
        raise SourceError("Cần nhập tên hoặc phần đầu của heading.")
    headings = [(i, block.text.casefold()) for i, block in enumerate(blocks) if block.heading is not None]
    matches = [i for i, name in headings if name == query]
    if not matches:
        matches = [i for i, name in headings if name.startswith(query)]
    if len(matches) != 1:
        candidates = ", ".join(blocks[i].text for i in matches[:8])
        detail = f" Các mục khớp gồm {candidates}." if candidates else ""
        raise SourceError(f"Cần một heading duy nhất. Dùng --outline để xem tên mục.{detail}")

    start = matches[0]
    level = blocks[start].heading
    end = next(
        (i for i, _ in headings if i > start and blocks[i].heading <= level),
        len(blocks),
    )
    return blocks[start:end]


def show_document(number: str, outline: bool, section: Optional[str]) -> None:
    path = DOCS / SOURCES[number]
    blocks = word_blocks(path)
    if section is not None:
        blocks = select_section(blocks, section)
    print(path.name)
    for block in blocks:
        if block.heading is not None:
            print(f"\n{'#' * block.heading} {block.text}")
        elif not outline:
            print(block.text)


def shared_text(node: ET.Element) -> str:
    runs = node.findall(SHEET + "r")
    if runs:
        return "".join(run.findtext(SHEET + "t", "") for run in runs)
    return node.findtext(SHEET + "t", "")


def sheet_rows(root: ET.Element, shared: list[str]) -> dict[int, dict[str, str]]:
    rows = {}
    for row in root.findall(f"{SHEET}sheetData/{SHEET}row"):
        cells = {}
        for cell in row.findall(SHEET + "c"):
            coordinate = cell.attrib["r"]
            column = coordinate.rstrip("0123456789")
            value = cell.findtext(SHEET + "v", "")
            cell_type = cell.get("t")
            if cell_type == "s" and value:
                value = shared[int(value)]
            elif cell_type == "inlineStr":
                inline = cell.find(SHEET + "is")
                value = shared_text(inline) if inline is not None else ""
            formula = cell.find(SHEET + "f")
            if formula is not None:
                cached = value or "trống"
                value = f"={formula.text or ''} [cache={cached}]"
            if value.strip():
                cells[column] = value
        if cells:
            rows[int(row.attrib["r"])] = cells
    return rows


def read_workbook() -> dict[str, dict[int, dict[str, str]]]:
    with ZipFile(DOCS / SOURCES["07"]) as package:
        workbook = ET.fromstring(package.read("xl/workbook.xml"))
        relations = ET.fromstring(package.read("xl/_rels/workbook.xml.rels"))
        targets = {
            link.attrib["Id"]: link.attrib["Target"]
            for link in relations
            if link.get("TargetMode") != "External"
        }
        shared = []
        if "xl/sharedStrings.xml" in package.namelist():
            shared_root = ET.fromstring(package.read("xl/sharedStrings.xml"))
            shared = [shared_text(node) for node in shared_root.findall(SHEET + "si")]

        data = {}
        for sheet in workbook.findall(f"{SHEET}sheets/{SHEET}sheet"):
            name = sheet.attrib["name"]
            target = targets[sheet.attrib[OFFICE_REL + "id"]]
            member = target.lstrip("/") if target.startswith("/") else posixpath.join("xl", target)
            root = ET.fromstring(package.read(posixpath.normpath(member)))
            data[name] = sheet_rows(root, shared)
        return data


def indexed_rows(data, name: str) -> dict[str, int]:
    if name not in data:
        raise SourceError(f"Thiếu sheet {name} trong Core 07.")
    rows = data[name]
    label = SHEET_KEYS[name]
    if rows.get(1, {}).get("A") != label:
        raise SourceError(f"{name}!A1 cần tiêu đề {label}.")
    indexes = {}
    for number, cells in rows.items():
        if number == 1:
            continue
        key = cells.get("A", "").strip().upper()
        if not key:
            raise SourceError(f"{name}!A{number} thiếu {label}.")
        if key in indexes:
            raise SourceError(f"{name}!A{number} trùng {key} ở dòng {indexes[key]}.")
        indexes[key] = number
    return indexes


def print_row(name: str, number: int, cells: dict[str, str]) -> None:
    entries = []
    for column, value in cells.items():
        display = " / ".join(value.splitlines()).replace("|", r"\|")
        entries.append(f"{column}{number}={display}")
    print(f"{name}!{number} | " + " | ".join(entries))


def show_sheet(data, name: str, key: Optional[str]) -> None:
    name_by_case = {label.casefold(): label for label in data}
    actual_name = name_by_case.get(name.strip().casefold())
    if actual_name is None:
        raise SourceError(f"Sheet {name} chưa được tìm thấy. Có các sheet " + ", ".join(data) + ".")
    rows = data[actual_name]
    if key is None:
        for number, cells in rows.items():
            print_row(actual_name, number, cells)
        return
    if actual_name not in SHEET_KEYS:
        raise SourceError(f"Mở toàn sheet {actual_name} để tra dữ liệu.")
    number = indexed_rows(data, actual_name).get(key.strip().upper())
    if number is None:
        raise SourceError(f"Không tìm thấy {key} trong {actual_name}.")
    print_row(actual_name, number, rows[number])


def read_backlog(data, milestones: dict[str, int]) -> dict[str, Task]:
    indexes = indexed_rows(data, "Backlog")
    if not indexes:
        raise SourceError("Backlog chưa có task.")
    rows = data["Backlog"]
    columns = {value: column for column, value in rows[1].items()}
    required = {"Milestone", "Task", "Depends on", "Priority", "Status"}
    missing_headers = sorted(required - columns.keys())
    if missing_headers:
        raise SourceError("Backlog thiếu cột " + ", ".join(missing_headers) + ".")

    tasks = {}
    for key, number in indexes.items():
        values = {label: rows[number].get(columns[label], "").strip() for label in required}
        required_values = sorted(label for label in required if label != "Depends on" and not values[label])
        if required_values:
            raise SourceError(f"Backlog dòng {number} thiếu " + ", ".join(required_values) + ".")
        milestone = values["Milestone"].upper()
        if milestone not in milestones:
            raise SourceError(f"{key} tham chiếu milestone {milestone} chưa có trong Roadmap.")
        raw_dependencies = values["Depends on"]
        dependencies = ()
        if raw_dependencies not in {"", "-", "–", "—"}:
            dependencies = tuple(part.strip().upper() for part in raw_dependencies.split(","))
        missing = [value or "(trống)" for value in dependencies if value not in indexes]
        if missing:
            raise SourceError(f"{key} có dependency chưa có trong Backlog gồm " + ", ".join(missing) + ".")
        tasks[key] = Task(number, milestone, values["Task"], dependencies, values["Priority"], values["Status"])

    graph = TopologicalSorter({key: task.dependencies for key, task in tasks.items()})
    try:
        graph.prepare()
    except CycleError as error:
        raise SourceError("Vòng dependency trong Backlog " + " → ".join(error.args[1]) + ".") from error
    return tasks


def open_dependencies(task: Task, tasks: dict[str, Task]) -> list[str]:
    return [dep for dep in task.dependencies if not tasks[dep].complete]


def show_task(data, tasks: dict[str, Task], key: str) -> None:
    key = key.strip().upper()
    if key not in tasks:
        raise SourceError(f"Không tìm thấy {key} trong Backlog.")
    task = tasks[key]
    print_row("Backlog", task.row, data["Backlog"][task.row])
    for dep in task.dependencies:
        print(f"  {dep} | {tasks[dep].status} | Backlog!{tasks[dep].row}")
    waiting = open_dependencies(task, tasks)
    print("Dependency trực tiếp còn mở: " + (", ".join(waiting) or "không có"))
    print(f"Đối chiếu cổng {task.milestone} trong Roadmap trước khi nhận việc.")


def show_status(data, milestones: dict[str, int], tasks: dict[str, Task], selected: Optional[str]) -> None:
    grouped = {milestone: [] for milestone in milestones}
    for key, task in tasks.items():
        grouped[task.milestone].append((key, task))

    roadmap = data["Roadmap"]
    columns = {value: column for column, value in roadmap[1].items()}
    if "Status" not in columns:
        raise SourceError("Roadmap thiếu cột Status.")
    print("Roadmap hiện tại | trạng thái ghi trong sheet | task Backlog hoàn thành/tổng")
    for milestone, number in milestones.items():
        status = roadmap[number].get(columns["Status"], "(trống)")
        done = sum(task.complete for _, task in grouped[milestone])
        print(f"  {milestone} | {status} | {done}/{len(grouped[milestone])}")

    if selected is not None:
        selected = selected.strip().upper()
        if selected not in milestones:
            raise SourceError(f"Không tìm thấy {selected} trong Roadmap.")
    else:
        selected = next((
            milestone for milestone, items in grouped.items()
            if any(task.priority.upper() == "MUST" and not task.complete for _, task in items)
        ), None)
        if selected is None:
            print("\nBacklog đã hết task MUST đang mở. Đối chiếu các cổng Roadmap để nghiệm thu.")
            return

    print(f"\n{selected} | cổng và task chưa hoàn thành")
    number = milestones[selected]
    print_row("Roadmap", number, roadmap[number])
    for key, task in grouped[selected]:
        if not task.complete:
            waiting = ", ".join(open_dependencies(task, tasks)) or "đủ dependency trực tiếp"
            print(f"  {key} | {task.priority} | {task.status} | {waiting} | {task.description}")
    print("Đối chiếu cổng Roadmap và bằng chứng trước khi nhận việc hoặc đóng milestone.")


def check_sources(data, tasks: dict[str, Task]) -> None:
    for filename in SOURCES.values():
        if filename.endswith(".docx") and not any(
            block.heading is not None for block in word_blocks(DOCS / filename)
        ):
            raise SourceError(f"{filename} thiếu heading để tra mục.")
    for name in ("Dashboard", "Changelog", *SHEET_KEYS):
        if not data.get(name):
            raise SourceError(f"Thiếu dữ liệu ở sheet {name}.")
        if name in SHEET_KEYS and not indexed_rows(data, name):
            raise SourceError(f"Sheet {name} chưa có ID dữ liệu.")

    decisions = data["Decisions"]
    headers = decisions[1].keys()
    extra = [
        f"{column}{number}" for number, row in decisions.items() if number != 1
        for column in row if column not in headers
    ]
    if extra:
        print("Lưu ý Core 07 | Cột chưa có tiêu đề ở Decisions: " + ", ".join(extra))
    for key, task in tasks.items():
        waiting = open_dependencies(task, tasks)
        if task.complete and waiting:
            print(f"Lưu ý Backlog | {key} đã hoàn thành trong khi dependency còn mở: " + ", ".join(waiting))
    print(f"Đã kiểm cấu trúc {len(SOURCES)} file Core, {len(data)} sheet và DAG của {len(tasks)} task.")


def make_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    document = commands.add_parser("doc", help="Đọc nội dung Word hoặc tra heading.")
    document.add_argument("number", choices=[number for number in SOURCES if number != "07"])
    views = document.add_mutually_exclusive_group()
    views.add_argument("--outline", action="store_true")
    views.add_argument("--section", help="Tên heading hoặc phần đầu duy nhất của tên mục.")
    sheet = commands.add_parser("sheet", help="Đọc sheet Excel, tùy chọn tra theo ID cột A.")
    sheet.add_argument("name")
    sheet.add_argument("id", nargs="?")
    task = commands.add_parser("task", help="Đọc một dòng Backlog và dependency.")
    task.add_argument("id")
    status = commands.add_parser("status", help="Đọc Roadmap và nhóm việc còn mở.")
    status.add_argument("--milestone")
    commands.add_parser("check", help="Kiểm cấu trúc nguồn, sheet, ID và DAG Backlog.")
    commands.add_parser("hashes", help="In SHA-256 của bảy file nguồn.")
    return parser


def main() -> None:
    args = make_parser().parse_args()
    if args.command == "doc":
        show_document(args.number, args.outline, args.section)
    elif args.command == "hashes":
        for filename in SOURCES.values():
            digest = hashlib.sha256((DOCS / filename).read_bytes()).hexdigest()
            print(f"{digest}  docs/{filename}")
    else:
        data = read_workbook()
        if args.command == "sheet":
            show_sheet(data, args.name, args.id)
            return
        milestones = indexed_rows(data, "Roadmap")
        tasks = read_backlog(data, milestones)
        if args.command == "task":
            show_task(data, tasks, args.id)
        elif args.command == "status":
            show_status(data, milestones, tasks, args.milestone)
        else:
            check_sources(data, tasks)


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    sys.stderr.reconfigure(encoding="utf-8")
    try:
        main()
    except (OSError, BadZipFile, ET.ParseError, KeyError, IndexError, ValueError) as error:
        sys.exit(f"Lỗi nguồn: {error}")
