"""Render the M0 handoff from the same metre coordinates consumed by Unity."""

import json
from collections import deque
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1] / "docs" / "prototype"
DATA = json.loads((ROOT / "layout.json").read_text(encoding="utf-8"))
INK = "#e7efe9"
MUTED = "#a9bbb3"
PAPER = "#172421"
GRID = "#3b564e"
ROUTE = "#c4d9a4"
NOTES = "#ebc294"


def font(size, bold=False):
    return ImageFont.truetype("segoeuib.ttf" if bold else "segoeui.ttf", size)


def sheet(title, subtitle, size=(1600, 1120)):
    img = Image.new("RGB", size, PAPER)
    canvas = ImageDraw.Draw(img)
    canvas.text((63, 46), title, font=font(42, True), fill=INK)
    canvas.text((64, 104), subtitle, font=font(23), fill=MUTED)
    canvas.line((64, 154, size[0] - 64, 154), fill=GRID, width=2)
    return img, canvas


def key(canvas, y, label, detail, x=980):
    canvas.text((x, y), label, font=font(24, True), fill=INK)
    canvas.multiline_text((x, y + 37), detail, font=font(22), fill=MUTED, spacing=9)


def plan():
    img, draw = sheet("An Sinh · Mặt bằng tầng trệt", "TASK022 / TASK024   •   Tỷ lệ theo lưới 1 m   •   Tầng trệt ±0,00")
    metre = 18.3

    def p(x, z):
        return 319 + metre * x, 625 - metre * z

    def rect(x, z, w, d):
        return (*p(x, z + d), *p(x + w, z))

    for x in range(-7, 19):
        draw.line([p(x, -24), p(x, 24)], fill=GRID, width=1)
    for z in range(-24, 25):
        draw.line([p(-7, z), p(18, z)], fill=GRID, width=1)

    colors = {"LOC03": "#324d42", "LOC04": "#2b3937", "STORE": "#403c32", "LOC01": "#293939"}
    for room in DATA["rooms"]:
        x, z, w, d = (room[k] for k in ("x", "z", "w", "d"))
        draw.rectangle(rect(x, z, w, d), outline=MUTED, fill=colors.get(room["id"], "#243632"), width=2)
        if room["id"] not in ("LOC03", "STORE", "HALL", "STAFF"):
            draw.text(p(x + w / 2, z + d * .70), room["id"], font=font(17, True), fill=INK, anchor="mm")
            draw.text(p(x + w / 2, z + d * .70 - 1.25), room["label"], font=font(16), fill=MUTED, anchor="mm")
    for door in DATA["openings"]:
        x, z, half = door["x"], door["z"], door["width"] / 2
        start, end = (p(x-half,z),p(x+half,z)) if door["alongX"] else (p(x,z-half),p(x,z+half))
        draw.line([start, end], width=7, fill=ROUTE)
    for route in ([(4,-21.8),(4,19.6)], [(4,-2),(0.05,-2),(.05,19.6),(4,19.6)], [(4,4.75),(12.7,4.75),(12.7,3.62),(15.8,3.62)]):
        draw.line([p(*point) for point in route], width=2, fill=ROUTE, joint="curve")
    for bed in DATA["beds"]:
        x, z = bed["x"], bed["z"]
        w, d = DATA["bedWidth"], DATA["bedLength"]
        tint = "#aa936c" if bed["id"] == "4" else "#89a493"
        draw.rectangle(rect(x-w/2,z-d/2,w,d), fill=tint)
        draw.text(p(x,z), bed["id"], font=font(12,True), fill=PAPER, anchor="mm")
    for pad in DATA["pads"]:
        x, y = p(pad["x"], pad["z"])
        draw.ellipse((x-6,y-6,x+6,y+6), fill=NOTES)
        draw.text((x+8,y+3), pad["id"], font=font(13,True), fill=NOTES)
    draw.text(p(9.4, 9.55), "LOC03 · 8 × 6 m", font=font(18,True), fill=INK, anchor="mm")
    draw.text(p(16.9, 4.35), "Kho", font=font(16), fill=MUTED)
    draw.text(p(-.9, 11), "Lối phụ", font=font(13), fill=MUTED)
    draw.text(p(2.54, 12), "3 m", font=font(13), fill=MUTED)
    draw.line((132, 101, 132, 101), fill=GRID)
    draw.text((628, 211), "↑ BẮC", font=font(20, True), fill=INK)
    draw.line((800, 192, 800, 1038), fill=GRID, width=2)

    key(draw, 212, "Hai đường tới cấp cứu", "Trục chính 3,0 m và lối phụ 2,1 m.\nHai cửa thông thủy 1,4 m hội tụ tại LOC04.", 848)
    key(draw, 361, "Bốn vị trí tại phòng 101", "Giường 1–3 đặt tại chỗ. Vị trí 4 hiển thị\nổ cắm và dấu bánh trong R. Lối giữa 1,88 m.", 848)
    key(draw, 498, "Cửa phòng 1,2 m", "AP04 nằm trong phòng, cách cửa 0,92 m.\nVách B chỉ đổi cục bộ tại hốc 4.", 848)
    key(draw, 635, "Kho và đường hồi", "Tủ mở lối kho trong R sau đối chiếu E12.\nĐường sảnh luôn còn sau mọi quyết định F101.", 848)
    key(draw, 796, "Ký hiệu", "Đường xanh · tuyến đi bộ dự kiến\nNút vàng · vị trí đáp an toàn\nÔ vàng · bao hình khi kê thử giường 4", 848)
    draw.text((65, 1062), "Hình học hư cấu dành cho thử bố cục trong Unity.", font=font(18), fill=MUTED)
    img.save(ROOT / "floor-plan.png")


def section():
    img, draw = sheet("An Sinh · Mặt cắt chức năng", "TASK022   •   Cao độ Y theo Core 03 §3   •   Khoảng cách ngang minh họa")
    base = 683
    levels = {row["id"]: row["y"] for row in DATA["levels"]}

    def elevation(name):
        return round(base - 40 * levels[name])

    for name, value in levels.items():
        y = elevation(name)
        draw.line((196, y, 1514, y), fill=GRID, width=2)
        draw.text((62, y - 11), f"{name}  {value:+.2f}", font=font(17), fill=MUTED)

    for low, high, caption in (("TRET","TANG-2","LOC02 · lễ tân và sảnh"),("TANG-2","TANG-3","LOC05 · khu B"),("TANG-3","MAI","LOC07/08 · kho và hành chính")):
        bottom, top = elevation(low), elevation(high)
        draw.rectangle((377,top,1118,bottom), fill="#293d35", outline=MUTED, width=2)
        draw.text((552, (bottom+top)//2), caption, font=font(25,True), fill=INK, anchor="lm")

    yb, yr = elevation("B1"), elevation("SAN-SAU")
    for left, right, label in ((581,755,"LOC09\nKỹ thuật"),(755,907,"LOC11\nTiền C"),(907,1118,"LOC12\nKhu C")):
        draw.rectangle((left, yb-116, right, yb), fill="#343e36", outline=MUTED, width=2)
        draw.multiline_text(((left+right)/2, yb-66), label, font=font(18,True), anchor="mm", align="center", fill=INK)
    draw.rectangle((1138, yb-92, 1310, yb), fill="#303a34", outline=MUTED, width=2)
    draw.text((1224,yb-47), "LOC10", font=font(20,True), fill=INK, anchor="mm")
    draw.line((204,base,377,base), fill=ROUTE, width=5)
    draw.line((1118,yr,1510,yr), fill=NOTES, width=4)
    draw.line((231,base-25,420,base-25,420,elevation("TANG-2")-29,528,elevation("TANG-2")-29), width=5, fill=ROUTE)
    draw.text((218,base-51), "Sân trước", font=font(20,True), fill=ROUTE)
    draw.text((392,elevation("TANG-2")-66), "Thang chính", font=font(16), fill=ROUTE)
    draw.line((1051,base-42,622,base-42,622,yb-22,909,yb-22), width=4, fill="#9ebcc6")
    draw.text((908,base-80), "LOC04 → thang kỹ thuật", font=font(17), fill="#b3c9cd")
    draw.line((1118,yb,1150,yr), fill=NOTES, width=4)
    draw.rectangle((1107,yb-87,1127,yb), fill="#a47962", outline=NOTES, width=2)
    draw.line((1109,yb-82,1125,yb-10), fill=PAPER, width=3)
    draw.line((1125,yb-82,1109,yb-10), fill=PAPER, width=3)
    draw.text((1190,yr-42), "Sân sau · đường xe", font=font(23,True), fill=NOTES)
    draw.line((1450,yr,1450,yb-19,1310,yb-19), width=3, fill=NOTES)
    draw.text((1320,yb-47), "Cửa dịch vụ", font=font(16), fill=NOTES)
    key(draw, 887, "Cầu thang B ra sân độc lập", "Giữ tuyến qua sảnh khi B1 đóng.", 76)
    key(draw, 887, "Tiền C là đường vào chính", "Ô kính ở cửa LOC11 → LOC12.", 567)
    key(draw, 887, "Cửa sau bị bịt từ 2004", "Đoạn dẫn cũ dừng ở tường vữa.\nLOC10 có cửa phục vụ riêng.", 1057)
    draw.text((64, 1064), "F · Mặt cắt bố trí trò chơi, dùng để rà logic không gian; cao độ sân sau là giả định.", font=font(19), fill=MUTED)
    img.save(ROOT / "section.png")


def check():
    graph = {
        "LOC01": {"LOC02"},
        "LOC02": {"HALL", "STAFF", "LOC05"},
        "HALL": {"LOC03", "LOC04"},
        "STAFF": {"LOC04"},
        "LOC03": {"STORE"},
        "LOC04": {"LOC09"},
        "LOC09": {"LOC10", "LOC11"},
        "LOC11": {"LOC12"},
        "REAR": {"LOC10", "LOC09"},
    }
    for room, neighbors in tuple(graph.items()):
        for neighbor in neighbors:
            graph.setdefault(neighbor, set()).add(room)

    def reachable(start, target, blocked):
        todo = deque([start])
        seen = set(blocked)
        while todo:
            node = todo.popleft()
            if node in seen:
                continue
            if node == target:
                return True
            seen.add(node)
            todo.extend(graph[node] - seen)
        return False

    assert reachable("LOC05", "LOC01", {"LOC09", "REAR", "LOC11", "LOC12", "LOC10"})
    assert reachable("LOC01", "LOC05", {"LOC09", "REAR", "LOC11", "LOC12", "LOC10"})
    assert not reachable("REAR", "LOC12", {"LOC11"})
    assert not reachable("LOC10", "LOC12", {"LOC11"})
    for blocked in ({"HALL"}, {"STAFF"}):
        assert reachable("LOC02", "LOC04", blocked)
        assert reachable("LOC04", "LOC02", blocked)
    room = next(room for room in DATA["rooms"] if room["id"] == "LOC03")
    assert (room["w"], room["d"], DATA["wallHeight"]) == (8, 6, 3.6)
    for bed in DATA["beds"]:
        assert room["x"] < bed["x"] < room["x"] + room["w"]
        assert room["z"] - .001 <= bed["z"] - DATA["bedLength"]/2
        assert bed["z"] + DATA["bedLength"]/2 <= room["z"] + room["d"] + .001
    assert DATA["beds"][0]["z"] - DATA["beds"][2]["z"] - DATA["bedLength"] >= 1.85
    print("TASK022 · Core 03 elevations, LOC03 dimensions, independent B exit, no bypass of LOC11, two bidirectional LOC04 branches: PASS (draft topology).")


if __name__ == "__main__":
    check()
    plan()
    section()
