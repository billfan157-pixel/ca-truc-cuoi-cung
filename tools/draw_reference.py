"""Compose the asset reference sheet from the authored materials and prop maps."""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont, ImageOps

ROOT = Path(__file__).resolve().parents[1]
MAPS = ROOT / "Prototype/Assets/CTCC/Art/Textures"
OUT = ROOT / "docs/prototype/loc03-reference.png"
INK = "#eff4e9"
MUTED = "#c3c9bd"
BACKGROUND = "#1f2924"
RULE = "#43554a"


def font(size, bold=False):
    name = "BeVietnamPro-" + ("SemiBold" if bold else "Regular") + ".ttf"
    return ImageFont.truetype(str(ROOT / "Prototype/Assets/CTCC/Art/Resources/CTCC" / name), size)


def put(canvas, name, box):
    image = Image.open(MAPS / (name + ".png")).convert("RGB")
    canvas.paste(ImageOps.fit(image, (box[2], box[3]), method=Image.Resampling.LANCZOS), box[:2])


def outline(draw, xy, label, note):
    draw.text(xy, label, font=font(24, True), fill=INK)
    draw.multiline_text((xy[0], xy[1]+42), note, font=font(20), fill=MUTED, spacing=6)


def build():
    page = Image.new("RGB", (1680, 1300), BACKGROUND)
    draw = ImageDraw.Draw(page)
    draw.text((64, 52), "Phòng 101 · Vật liệu và đạo cụ", font=font(46, True), fill=INK)
    draw.text((66, 123), "An Sinh  /  TASK023   •   Bản mẫu tự dựng từ các quan sát có nguồn", font=font(25), fill=MUTED)
    draw.line((67, 180, 1613, 180), fill=RULE, width=2)
    draw.text((68, 212), "B  ·  Diện mạo năm 2005", font=font(29, True), fill=INK)
    draw.text((868, 212), "R  ·  Hiện trường năm 2012", font=font(29, True), fill=INK)
    for x, suffix in ((68,"B"),(868,"R")):
        put(page,"Plaster"+suffix+"_Color",(x,277,374,240))
        put(page,"Dado"+suffix+"_Color",(x,517,374,120))
        put(page,"Floor"+suffix+"_Color",(x+387,277,358,360))
    outline(draw,(68,662),"Tường và chân tường","Vữa trát, sơn sáng và dải lau được.\nHư hỏng R ở chân tường và gờ cửa.")
    outline(draw,(455,662),"Sàn gạch bông 20 cm","Hoa văn và mạch gạch trùng R/B.\nR bạc màu ở tuyến xe và mép ẩm.")
    outline(draw,(868,662),"Dấu thời gian có vị trí","R24 dẫn cách vẽ thấm và bong.\nMảng cháy áp dụng cho B1/C.")
    outline(draw,(1253,662),"Phản xạ thay theo ẩm","Dùng map nhám và normal.\nKhông thêm lỗ hoặc đổi cốt sàn.")
    draw.line((67,785,1613,785),fill=RULE,width=2)
    put(page,"Room101",(69,837,340,130))
    put(page,"ObsCard",(435,820,208,278))
    put(page,"Transfer",(669,820,208,278))
    put(page,"Drawing",(904,820,358,270))
    put(page,"Cotton_Color",(1288,820,321,270))
    outline(draw,(69,1012),"Biển men hư cấu","Đọc theo chữ và vị trí.\nCùng khung cửa trong R/B.")
    draw.text((435,1126),"Thẻ B",font=font(22,True),fill=INK)
    draw.text((669,1126),"Phiếu chuyển R",font=font(22,True),fill=INK)
    draw.text((904,1126),"Tranh An · bản ghép",font=font(22,True),fill=INK)
    draw.text((1288,1126),"Vải dệt · nệm và rèm",font=font(22,True),fill=INK)
    draw.line((67,1191,1613,1191),fill=RULE,width=2)
    draw.text((67,1220),"F · Mẫu An Sinh. Vân nền từ ambientCG CC0. Nguồn ảnh thực tế và giới hạn U ở research.md.",font=font(20),fill=MUTED)
    page.save(OUT)


if __name__ == "__main__":
    build()
