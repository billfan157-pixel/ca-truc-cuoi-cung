"""Create Vietnamese character takes from licensed VieNeu v3 Turbo presets."""

import argparse
from importlib.metadata import version
import json
from pathlib import Path
import re

import numpy as np
from scipy.io import wavfile
from vieneu import Vieneu

from bake_m2_audio import extract_clean_lines


CAST = {
    "minh": "Thái Sơn",
    "lan": "Thục Đoan",
    "ba_lanh": "Thục Đoan",
    "an": "Ngọc Trân",
    "gatekeeper": "Đức Trí",
    "pa": "Minh Đức",
    "pa_cut": "Minh Đức",
}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("model", type=Path, help="Pinned VieNeu snapshot containing the requested ONNX graph")
    parser.add_argument("output", type=Path)
    parser.add_argument("--precision", choices=("fp32", "int8"), default="fp32")
    parser.add_argument("--lines", nargs="+", help="Rerender only these dialogue IDs")
    args = parser.parse_args()
    model = args.model.resolve()
    args.output.mkdir(parents=True, exist_ok=True)
    graph = "onnx_int8" if args.precision == "int8" else "onnx_update"
    synth = Vieneu(mode="v3turbo", device="cpu", backend="onnx", backbone_repo=str(model),
                   onnx_dir=str(model / graph), precision=args.precision, threads=4)
    voices = {voice_id for _, voice_id in synth.list_preset_voices()}
    if not set(CAST.values()) <= voices:
        raise RuntimeError(f"Missing VieNeu presets: {sorted(set(CAST.values()) - voices)}")
    lines = extract_clean_lines()
    selected = set(args.lines or (line_id for line_id, _, _ in lines))
    if unknown := selected - {line_id for line_id, _, _ in lines}:
        parser.error(f"Unknown dialogue IDs: {', '.join(sorted(unknown))}")
    metadata = args.output / "takes.json"
    record = {"sdk": version("vieneu"), "modelRevision": model.name, "takes": {}}
    if args.lines and metadata.exists():
        previous = json.loads(metadata.read_text(encoding="utf-8"))
        if (previous["sdk"], previous["modelRevision"]) != (record["sdk"], record["modelRevision"]):
            raise ValueError("The staged takes came from a different SDK or model revision")
        record["takes"] = previous["takes"]
    for index, (line_id, role, canonical) in enumerate(lines):
        if line_id not in selected:
            continue
        spoken = re.sub(r"\b101\b", "một linh một", canonical)
        spoken = re.sub(r"\bC01\b", "xê không một", spoken)
        if line_id == "CH01-D03c":
            spoken = spoken.replace("17 tháng 11", "mười bảy tháng mười một")
        if line_id == "CH01-D10":
            spoken = spoken.replace("…", "")
        # Keep pauses and intonation in the spoken copy. The preset supplies the speaking style.
        np.random.seed(10100 + index)
        samples = synth.infer(spoken, voice=CAST[role], temperature=0.63, top_k=20, top_p=0.92)
        if not len(samples) or not np.all(np.isfinite(samples)):
            raise ValueError(f"Invalid output for {line_id}")
        wavfile.write(args.output / f"{line_id}.wav", synth.sample_rate, samples.astype(np.float32))
        record["takes"][line_id] = {"role": role, "preset": CAST[role], "spoken": spoken, "precision": args.precision}
        print(f"{line_id} {CAST[role]} {len(samples) / synth.sample_rate:.2f}s", flush=True)
    metadata.write_text(json.dumps(record, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
