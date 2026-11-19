"""Place two original engine captures beside each other at the same scale."""
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont, ImageStat

ROOT = Path(__file__).resolve().parents[1]
FONT = ROOT / "Prototype/Assets/CTCC/Art/Resources/CTCC/BeVietnamPro-SemiBold.ttf"

folder = Path(sys.argv[1])
dest = Path(sys.argv[2])
board = Image.new("RGB", (1640, 524), "#16221e")
draw = ImageDraw.Draw(board)
font = ImageFont.truetype(str(FONT),23)
for i,(filename,caption) in enumerate((("room101-R.png","R · Hiện trường năm 2012"),("room101-B.png","B · Phòng bệnh theo hồ sơ"))):
    original = Image.open(folder / filename).convert("RGB")
    assert min(ImageStat.Stat(original).stddev)>7, f"{filename} contains no usable scene"
    original.thumbnail((800,450),Image.Resampling.LANCZOS)
    x=13+i*814
    board.paste(original,(x,13))
    draw.text((x+7,479),caption,font=font,fill="#e6ebdf")
dest.parent.mkdir(exist_ok=True,parents=True)
board.save(dest,quality=93)
