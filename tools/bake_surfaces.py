"""Bake the authored An Sinh surface kit, signs and localized wear masks."""

import math
import sys
from io import BytesIO
from pathlib import Path
from zipfile import ZipFile

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[1]
RAW = ROOT / "output" / "material-staging" / "source"
DEST = Path(sys.argv[1]) if len(sys.argv) > 1 else RAW.parent / "textures"
RNG = np.random.default_rng(101)
SIZE = 2048


def field(size, step):
    base = Image.fromarray(RNG.integers(0, 256, (step, step), dtype=np.uint8))
    return np.asarray(base.resize((size, size), Image.Resampling.BICUBIC), dtype=np.float32) / 255


def source(asset, channel):
    with ZipFile(RAW / (asset + ".zip")) as package:
        name = next(name for name in package.namelist() if name.endswith("_" + channel + ".jpg"))
        image = Image.open(BytesIO(package.read(name))).convert("RGB")
        if image.size != (SIZE, SIZE):
            image = image.resize((SIZE, SIZE), Image.Resampling.BICUBIC)
        return np.asarray(image, dtype=np.float32) / 255


def save(name, pixels):
    img = Image.fromarray(np.uint8(np.clip(pixels, 0, 1) * 255))
    img.save(DEST / (name + ".png"))


def rgb(*channels):
    return np.stack(channels, axis=-1)


def luma(image):
    return image @ np.array([.30, .59, .11], dtype=np.float32)


def center(values):
    return values - np.median(values)


def map_set(name, color, normal, smoothness, occlusion=1.0, metal=0.0):
    save(name + "_Color", color)
    save(name + "_Normal", normal)
    shape = color.shape[:2]
    ao = np.broadcast_to(occlusion, shape)
    metallic = np.broadcast_to(metal, shape)
    smooth = np.broadcast_to(smoothness, shape)
    save(name + "_Mask", np.stack((metallic, ao, np.zeros(shape), smooth), axis=-1))


def normal_map(height, amount):
    dx = (np.roll(height, 1, axis=1) - np.roll(height, -1, axis=1)) * amount
    dy = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) * amount
    v = rgb(dx, dy, np.ones_like(dx))
    return (v / np.linalg.norm(v, axis=-1, keepdims=True)) * .5 + .5


def wall():
    base = luma(source("PaintedPlaster017", "Color"))
    normal = source("PaintedPlaster017", "NormalGL")
    rough = luma(source("PaintedPlaster017", "Roughness"))
    broad = field(SIZE, 16)
    peel = np.clip((field(SIZE, 33) * .52 + broad * .48 - .78) * 4.6, 0, 1)
    freckles = np.minimum(.35, np.clip((field(SIZE, 330) - .80) * 2.2, 0, 1))
    detail = center(base) * .32
    for name, tone, aged in (
        ("PlasterB", (.76, .78, .70), False),
        ("PlasterR", (.56, .58, .52), True),
        ("DadoB", (.39, .57, .49), False),
        ("DadoR", (.28, .39, .34), True),
    ):
        color = np.full((SIZE, SIZE, 3), tone, dtype=np.float32) + detail[..., None]
        if aged:
            color += center(broad)[..., None] * .10 - freckles[..., None] * .19
            color = color * (1 - peel[..., None]) + np.array([.44, .41, .32]) * peel[..., None]
        map_set(name, color, normal, .16 + (1 - rough) * (.24 if aged else .38), .91 + (1 - peel) * .08)


def tile():
    agg = luma(source("Terrazzo013", "Color"))
    normal = source("Terrazzo013", "NormalGL")
    y, x = np.ogrid[:SIZE, :SIZE]
    s = SIZE // 8
    u, v = (x % s) / s, (y % s) / s
    grout = (np.minimum(np.minimum(u, 1-u), np.minimum(v, 1-v)) < .012).astype(np.float32)
    # Original cement-tile design. Border and small lozenges sit in the aggregate.
    edge_distance = np.abs(u-.5) + np.abs(v-.5)
    inlay = ((edge_distance > .405) & (edge_distance < .44)).astype(np.float32)
    inlay += ((np.abs(u-.5) < .022) & (np.abs(v-.5) < .095)).astype(np.float32) * .42
    inlay += ((np.abs(v-.5) < .022) & (np.abs(u-.5) < .095)).astype(np.float32) * .42
    parity = ((x//s + y//s) % 2).astype(np.float32)
    fade = field(SIZE, 11)
    damage = np.clip((fade * .57 + field(SIZE, 68) * .43 - .71) * 4, 0, 1)
    variation = center(agg) * .29 + (parity-.5) * .034
    for name, tone, age in (("FloorB", (.65, .64, .55), .25), ("FloorR", (.53, .54, .48), 1.0)):
        color = np.full((SIZE, SIZE, 3), tone, dtype=np.float32) + variation[..., None]
        motif = inlay * (.80 - damage * age * .48)
        color = color * (1 - motif[..., None] * .67) + np.array([.29,.37,.31]) * motif[..., None] * .67
        color -= grout[..., None] * (.21 + age*.06) + damage[..., None] * age * .10
        bump = normal * .50 + normal_map(1.0 - grout * .28, 1.6) * .50
        map_set(name, color, bump, (.50 if age < 1 else .37) + fade * .17, 1 - grout * .38)


def stock():
    for asset, key, tint, gloss, metallic in (
        ("Fabric085", "Cotton", (.73,.74,.66), .16, 0),
        ("Metal032", "Steel", (.53,.58,.58), .55, .88),
        ("Tiles040", "Ceramic", (.67,.71,.66), .55, 0),
    ):
        luminance = center(luma(source(asset, "Color")))
        color = np.full((SIZE, SIZE, 3), tint, dtype=np.float32) + luminance[..., None] * .52
        normal = source(asset, "NormalGL")
        roughness = luma(source(asset, "Roughness"))
        map_set(key, color, normal, np.clip(gloss + center(1-roughness)*.42, .05, .95), .94, metallic)


def decals():
    frost = 1024
    y, x = np.mgrid[0:frost, 0:frost].astype(np.float32) / (frost-1)
    detail = field(frost, 150)
    wash = field(frost, 16)
    track = np.maximum(0, 1 - np.abs(x - .54 - y * .14) * (1.6 + 3*y))
    runs = np.maximum(0, np.sin(x*157 + wash*2.7)) * .32
    margin = np.maximum(0, np.sin(x * np.pi)) * np.power(np.maximum(0, np.sin(y*np.pi)), .67)
    fall = np.clip((1 - y) * (.44 + wash*.8), 0, 1)
    deposit = np.clip(track * fall * margin * (runs + .42) * (wash*.85+detail*.31), 0, 1)
    save("WaterRun", np.stack((.20+wash*.11,.22+wash*.10,.15+wash*.09,deposit*1.72), axis=-1))
    halo = np.exp(-((x-.51)**2*8 + (y-.49)**2*10)) * margin
    chips = np.clip((wash * .52 + detail * .48 - .57) * 3.4, 0, 1) * halo
    save("PaintLoss", np.stack((.36+detail*.22,.32+detail*.19,.24+detail*.16,chips), axis=-1))
    pooled = np.maximum(0, (halo**.8 * (.49 + wash*.68) - .36)) * margin
    save("DampFloor", np.stack((.11+detail*.065,.17+detail*.062,.16+detail*.075,pooled*.48), axis=-1))
    rust = np.maximum(0, (np.exp(-np.abs(x-.48)*14)*fall*3-.62)) * margin * wash
    save("RustRun", np.stack((.38+detail*.17,.17+detail*.14,.09+detail*.12,np.clip(rust,0,.70)), axis=-1))
    soot = np.clip((halo * (.62 + wash*.75) - .28) * 1.4, 0, .85)
    save("SootSwatch", np.stack((.13+detail*.09,.13+detail*.07,.12+detail*.06,soot), axis=-1))

    size = 768
    wheel = Image.new("RGBA", (size, size))
    d = ImageDraw.Draw(wheel)
    for x in (169, 584):
        for y in (174, 598):
            d.arc((x-54,y-71,x+54,y+71), 14, 359, fill=(49,44,37,185), width=13)
            d.line((x-16,y-30,x+15,y+40), fill=(49,44,37,125), width=4)
    for i in range(52):
        k = i / 51
        x, y = 588 + int(k*168), 597 - int(k*268)
        d.ellipse((x-13,y-7,x+16,y+7), fill=(50,45,39,int(135*(.8-k*.55))))
    wheel.filter(ImageFilter.GaussianBlur(3)).save(DEST / "WheelMarks.png")


def typeface(size, strong=False):
    return ImageFont.truetype(str(RAW / ("BeVietnamPro-SemiBold.ttf" if strong else "BeVietnamPro-Regular.ttf")), size)


def signs():
    def sign(name, big, small, palette=("f2e6c9", "253f38"), size=(1024, 390)):
        paper, ink = ("#" + v for v in palette)
        width, height = size
        img = Image.new("RGB", size, paper)
        d = ImageDraw.Draw(img)
        d.rounded_rectangle((15,15,width-15,height-15),4,outline=ink,width=9)
        title_font, suffix_font = typeface(146,True), typeface(36,True)
        while d.textlength(big,font=title_font) > width - 130:
            title_font = typeface(title_font.size-4, True)
        d.text((width/2,height*.49),big,font=title_font,fill=ink,anchor="mm")
        if small:
            d.text((width/2,height*.79),small,font=suffix_font,fill=ink,anchor="mm")
        for x in (42, width-42):
            for y in (42, height-42):
                d.ellipse((x-6,y-6,x+6,y+6),fill="#635d4c")
        img.save(DEST / (name + ".png"))

    sign("Room101","101","PHÒNG BỆNH",("e0d8ba","384c40"))
    sign("Welcome","AN SINH","BỆNH VIỆN · 1987",("33554a","e9ead3"))
    sign("Emergency","CẤP CỨU","LỐI CÁNG",("6a3730","f4e8ce"))
    sign("Station","BÀN TRỰC","KHU NỘI TRÚ")
    sign("FileA","ĐÃ BÀN GIAO","PHÒNG 101",("e0e3cf","324e48"))
    sign("FileB","CHỜ KIỂM TRA","PHÒNG 101",("e0e3cf","324e48"))
    sign("FileC","CÓ SAI LỆCH","PHÒNG 101",("e0e3cf","324e48"))
    sign("Demolition","KHU VỰC ĐÓNG CỬA","DỰ KIẾN THÁO DỠ 18/11/2012",("b8ad83","493a29"))
    sign("Staff","LỐI NHÂN VIÊN","CẤP CỨU",("c2c0a3","38443b"))
    sign("Store","KHO GIƯỜNG","PHỤC VỤ NỘI TRÚ",("bfbc9c","414737"))
    for k in range(1,5):
        sign("Socket"+str(k),str(k),"",("cec8ae","34362a"),(240,212))

    def form(name, heading, left, lines):
        width, height = (770, 1024)
        img = Image.new("RGB", (width,height), "#d4c4a0")
        d = ImageDraw.Draw(img)
        d.rectangle((22,22,width-22,height-22),outline="#868165",width=3)
        d.text((46,47),"BỆNH VIỆN AN SINH",font=typeface(27,True),fill="#455446")
        d.text((46,117),heading,font=typeface(44,True),fill="#33473c")
        d.text((46,207),left,font=typeface(26),fill="#4a453b")
        for i,line in enumerate(lines):
            y=297+i*72
            d.line((45,y-17,width-47,y-17),fill="#a19a79",width=2)
            d.text((46,y),line,font=typeface(26),fill="#514735")
        d.line((45,858,width-47,858),fill="#8d8261",width=2)
        d.text((46,891),"Bản lưu tại bàn trực",font=typeface(24),fill="#665943")
        img.save(DEST / (name + ".png"))

    form("F101","PHIẾU BÀN GIAO","18/11/2005   •   Phòng 101   •   F101",
         ("Số chưa bàn giao theo bản kê", "0", "Đối chiếu thực tế trước khi ký."))
    form("ObsCard","THẺ BÀN GIAO","18/11/2005   •   Phòng 101",
         ("Giường 4","CHƯA BÀN GIAO","Kiểm tra lại"))
    form("BedList","BẢN KÊ GIƯỜNG","Nội trú   •   Phòng 101",("Vị trí 1    •    Vị trí 2    •    Vị trí 3", "Tổng theo bản kê: 03"))
    form("Transfer","PHIẾU CHUYỂN","17/11/2005   •   Bản lưu nội bộ",("An","101 / 4  •  Khám sốt tạm", "Trở lại C01"))

    drawing = Image.new("RGB",(700,500),"#cbbb97")
    d=ImageDraw.Draw(drawing)
    d.line(((88,298),(92,197),(307,82),(550,203),(546,376),(93,384),(88,298)),fill="#9e5545",width=11)
    d.line(((193,367),(199,217),(404,222),(407,365)),fill="#46576b",width=12)
    d.line(((207,357),(198,218),(402,219),(401,363)),fill="#506579",width=5)
    d.arc((263,280,312,336),178,357,fill="#594b36",width=8)
    d.rectangle((266,310,309,342),outline="#594b36",width=7)
    d.line(((474,286),(468,233),(521,239),(518,290),(474,286)),fill="#6b7951",width=6)
    d.ellipse((569,48,623,108),outline="#a18445",width=7)
    for k in range(8):
        angle=k*math.tau/8
        d.line(((596+34*math.cos(angle),77+34*math.sin(angle)),(596+47*math.cos(angle),77+47*math.sin(angle))),fill="#a18445",width=4)
    drawing.save(DEST / "Drawing.png")


if __name__ == "__main__":
    DEST.mkdir(parents=True, exist_ok=True)
    wall()
    tile()
    stock()
    decals()
    signs()
    print(f"Wrote {len(list(DEST.glob('*.png')))} material, decal and prop maps to {DEST}")
