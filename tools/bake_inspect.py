"""Produce the clock face and three physical edges for the existing prop textures."""
from pathlib import Path
import math

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1] / "Prototype" / "Assets" / "CTCC" / "Art"
MAPS = ROOT / "Textures"


def draw_clock():
    clock = Image.new("RGBA", (768,768))
    d = ImageDraw.Draw(clock)
    p = 384
    d.ellipse((18,18,750,750),fill="#303531")
    d.ellipse((49,49,719,719),fill="#c9c4a8")
    d.ellipse((61,61,707,707),outline="#7f826b",width=5)
    label=ImageFont.truetype(str(ROOT/"Resources/CTCC/BeVietnamPro-SemiBold.ttf"),40)
    for minute in range(60):
        angle=math.tau*minute/60
        coord=lambda r: (p+math.sin(angle)*r,p-math.cos(angle)*r)
        d.line((coord(301),coord(278 if minute%5==0 else 294)),fill="#57594a",width=5 if minute%5==0 else 2)
        if minute%5==0:
            d.text(coord(243),str(minute//5 or 12),font=label,fill="#47483a",anchor="mm")
    for name,hour,minute in (("ClockB",3,17),("ClockR",11,52)):
        face=clock.copy()
        draw=ImageDraw.Draw(face)
        for degrees,reach,width in ((hour*30+minute*.5,144,17),(minute*6,229,10)):
            angle=math.radians(degrees)
            draw.line(((p,p),(p+math.sin(angle)*reach,p-math.cos(angle)*reach)),fill="#383d37",width=width)
        draw.ellipse((p-14,p-14,p+14,p+14),fill="#4a4c3a")
        face.save(MAPS/(name+".png"))


def tear():
    art=Image.open(MAPS/"Drawing.png").convert("RGBA")
    left=[(234,0),(249,98),(226,162),(248,247),(224,348),(241,500)]
    right=[(463,0),(473,113),(442,182),(465,254),(448,398),(471,500)]
    polygons=[[(0,0),*left,(0,500)],left+list(reversed(right)),right+[(700,500),(700,0)]]
    for i,outline in enumerate(polygons,1):
        mask=Image.new("L",art.size)
        ImageDraw.Draw(mask).polygon(outline,fill=255)
        fragment=art.copy()
        fragment.putalpha(mask)
        fragment.crop(mask.getbbox()).save(MAPS/f"DrawingPiece{i}.png")


def contact():
    width,height=256,512
    shade=Image.new("RGBA",(width,height))
    pixels=shade.load()
    for y in range(height):
        for x in range(width):
            u=(x/width-.5)*2
            v=(y/height-.5)*2
            envelope=max(0,1-abs(u)**3.4-abs(v)**4)
            center=math.exp(-(u*u*3.5+v*v*.9))
            pixels[x,y]=(37,39,34,round(95*envelope*center))
    shade.save(MAPS/"BedContact.png")


if __name__=="__main__":
    draw_clock()
    tear()
    contact()
