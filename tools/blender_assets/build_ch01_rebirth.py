"""Build the individually authored parts of the CH01 working props."""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import bpy
import common as C
from build_furniture import build_reception_props
from build_props import build_pedal_bin
from build_ch01r_props import build_bed4_blanket_trace


def reception():
    parts = build_reception_props()
    handset = [part for part in parts if part.name.startswith("Handset")]
    ledger = [part for part in parts if part.name.startswith(("Ledger", "DeskPen"))]
    dressing = [part for part in parts if part not in handset and part not in ledger]
    return {"CounterDressing": dressing, "Handset": handset, "DutyLedger": ledger}


def enamel_bin():
    parts = build_pedal_bin()
    old_body = parts.pop(0)
    bpy.data.objects.remove(old_body, do_unlink=True)
    body = C.make_lathe("EnamelShell", [
        (0.0, 0.0), (.135, 0.0), (.142, .015), (.142, .346), (.148, .354),
        (.136, .356), (.130, .343), (.129, .029), (.124, .019), (0.0, .019),
    ], (0, 0, 0), "MetalPaint", segments=32)
    lining = C.make_lathe("Clean liner seam", [
        (0, .024), (.123, .024), (.128, .032), (.129, .270), (.131, .286),
    ], (0, 0, 0), "MetalOld", segments=26)
    return {"BinShell": [body, lining], "BinLid": [parts[0]],
            "BinPedal": parts[1:3], "BinHinge": [parts[3]]}


def drawer_contents():
    pencil = C.make_cylinder("Pencil shaft", (-.052, 0, .004), (.050, 0, .004), .004, "WoodDoor", segments=8)
    ferrule = C.make_cylinder("Pencil ferrule", (-.058, 0, .004), (-.052, 0, .004), .0043, "Steel", segments=8)
    tip = C.make_cylinder("Graphite", (.050, 0, .004), (.059, 0, .004), .0016, "Black", segments=8)
    gauze = C.make_lathe("Cloth roll", [
        (0, 0), (.019, 0), (.023, .004), (.023, .028), (.018, .031), (.006, .031), (.006, .005),
    ], (0, 0, 0), "Cotton", segments=22)
    gauze_tab = C.make_box("Gauze end", (.018, .009, .017), (.018, .004, .020), "CottonOld", bevel=.0018)
    clip = [
        C.make_box("Folded steel", (0, 0, .007), (.029, .016, .014), "MetalOld", bevel=.002),
        C.make_pipe("Wire handles", [(-.009, -.007, .008), (-.014, -.024, .018),
            (.014, -.024, .018), (.009, -.007, .008)], .0013, "Steel", bevel_res=3),
    ]
    return {"DrawerPencil": [pencil, ferrule, tip], "DrawerGauze": [gauze, gauze_tab], "DrawerClip": clip}


def bed4_traces():
    parts = build_bed4_blanket_trace()
    return {
        "Bed4CottonTrace": [part for part in parts if part.name.startswith("FoldedBlanket")],
        "Bed4RubberTrace": [part for part in parts if part.name.startswith("Small")],
        "Bed4ContactTrace": [part for part in parts if not part.name.startswith(("FoldedBlanket", "Small"))],
    }


for name, fn in (("CTCC_PROP_ReceptionWorking", reception),
                 ("CTCC_PROP_PedalBinWorking", enamel_bin),
                 ("CTCC_PROP_CH01DrawerContents", drawer_contents),
                 ("CTCC_PROP_Bed4TracesWorking", bed4_traces)):
    C.export_asset(name, fn, make_lods=False, render=False, separate_parts=True)
