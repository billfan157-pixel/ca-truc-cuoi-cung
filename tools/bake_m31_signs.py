"""Original enamel signs for the playable An Sinh stairs and basement."""

from pathlib import Path
from random import Random

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1] / "Prototype" / "Assets" / "CTCC" / "Art"
FONTS = ROOT / "Resources" / "CTCC"
DEST = ROOT / "Textures"
SIGN_SIZE = (832, 220)


def make_sign(name, title, floor, seed):
    rng = Random(seed)
    image = Image.new("RGB", SIGN_SIZE, "#d0d0af")
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle((8, 8, 823, 211), radius=18, outline="#314244", width=8)
    draw.line((31, 161, 798, 161), fill="#354543", width=4)
    heading = ImageFont.truetype(str(FONTS / "BeVietnamPro-SemiBold.ttf"), 73)
    label = ImageFont.truetype(str(FONTS / "BeVietnamPro-Regular.ttf"), 27)
    draw.text((416, 94), title, font=heading, fill="#222f34", anchor="mm")
    draw.text((416, 184), floor, font=label, fill="#2e3f42", anchor="mm")
    for x in (35, 795):
        draw.ellipse((x - 8, 27, x + 8, 43), fill="#5d5540")
        draw.line((x - 4, 35, x + 4, 35), fill="#2e3028", width=2)
    for _ in range(420):
        x = rng.randrange(SIGN_SIZE[0])
        y = rng.choice((rng.randrange(24), rng.randrange(197, 220)))
        draw.line((x, y, x + rng.randrange(2, 12), y), fill=rng.choice(("#a99b73", "#786b50", "#999679")))
    image.save(DEST / f"{name}.png", optimize=True)


if __name__ == "__main__":
    for number, (name, title, floor) in enumerate((
        ("M31_KhuB", "KHU B", "CẦU THANG LÊN TẦNG 2"),
        ("M31_Kho", "KHO HỒ SƠ", "CẦU THANG LÊN TẦNG 3"),
        ("M31_Service", "KỸ THUẬT", "CẦU THANG XUỐNG B1"),
        ("M31_Morgue", "NHÀ XÁC", "KHU PHỤ TRỢ · B1"),
    )):
        make_sign(name, title, floor, 3100 + number)
