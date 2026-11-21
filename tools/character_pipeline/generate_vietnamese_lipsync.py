"""Offline Vietnamese Phoneme-to-Viseme Forced Aligner & Facial Performance Generator (M2-C).

Replaces runtime audio-amplitude mouth flapping with authored, phonetically aligned Vietnamese
facial performance tracks synchronized to each VO WAV clip's real acoustic timeline.

Outputs:
- Prototype/Assets/CTCC/Art/Resources/CTCC/LipSync/<LineId>.json  (30 Hz co-articulated viseme + brow + jaw + nod curves)
- ArtSource/Animation/LipSync/<LineId>.json                       (editable archive copy)
- output/m2c/lipsync-verification.png                             (overridable with --evidence-dir)
"""

import argparse
import json
import hashlib
import math
import re
import shutil
import unicodedata
import wave
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
FONT_PATH = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Resources" / "CTCC" / "BeVietnamPro-SemiBold.ttf"
DIALOGUE_JSON = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Resources" / "CTCC" / "Dialogue.json"
VO_DIR = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Audio" / "VO"
OUT_RES_DIR = ROOT / "Prototype" / "Assets" / "CTCC" / "Art" / "Resources" / "CTCC" / "LipSync"
OUT_SRC_DIR = ROOT / "ArtSource" / "Animation" / "LipSync"
EVIDENCE_DIR = ROOT / "output" / "m2c"

FPS = 30.0
VISEME_KEYS = ["PP", "FF", "TH", "CH", "AA", "EE", "IH", "OH", "OO", "UH", "MM"]

# Vietnamese vowel base map (preserving vowel quality while stripping the 6 lexical tones)
VOWEL_BASE = {}
for base, chars in {
    "a": "aàáảãạ",
    "ă": "ăằắẳẵặ",
    "â": "âầấẩẫậ",
    "e": "eèéẻẽẹ",
    "ê": "êềếểễệ",
    "i": "iìíỉĩị",
    "y": "yỳýỷỹỵ",
    "o": "oòóỏõọ",
    "ô": "ôồốổỗộ",
    "ơ": "ơờớởỡợ",
    "u": "uùúủũụ",
    "ư": "ưừứửữự",
}.items():
    for ch in chars:
        VOWEL_BASE[ch] = base

# Tone emphasis score (sac/hoi/nga carry stronger prosodic brow/head accents)
ACCENT_TONES = set("áắấéếíýóốớúứảẳẩẻểỉỷỏổởủửãẵẫẽễĩỹõỗỡũữ")

ONSETS = [
    "ngh", "ng", "nh", "ch", "tr", "th", "ph", "kh", "gh", "gi", "qu",
    "b", "c", "d", "đ", "g", "h", "k", "l", "m", "n", "p", "q", "r", "s", "t", "v", "x"
]
CODAS = ["ng", "nh", "ch", "m", "n", "p", "t", "c"]


def strip_tone(word: str):
    """Strip Vietnamese lexical tone while preserving ă, â, ê, ô, ơ, ư, đ; also detect prosodic emphasis."""
    w = word.lower()
    emph = any(ch in ACCENT_TONES for ch in w)
    out = []
    for ch in w:
        if ch in VOWEL_BASE:
            out.append(VOWEL_BASE[ch])
        elif ch == "đ":
            out.append("đ")
        elif ch.isalpha():
            out.append(ch)
    return "".join(out), emph


def expand_numbers(text: str) -> str:
    """Expand canonical numbers spoken in Vietnamese VO (e.g., 101 -> một linh một, C01 -> xê không một)."""
    text = re.sub(r"\b101\b", "một linh một", text)
    text = re.sub(r"\b12\b", "mười hai", text)
    text = re.sub(r"\bC01\b", "xê không một", text, flags=re.IGNORECASE)
    text = re.sub(r"\bC02\b", "xê không hai", text, flags=re.IGNORECASE)
    text = re.sub(r"\bC12\b", "xê mười hai", text, flags=re.IGNORECASE)
    text = re.sub(r"\b2005\b", "hai nghìn không trăm linh năm", text)
    return text


def onset_to_viseme(onset: str):
    if onset in ("b", "p", "m"):
        return ("PP", 0.085, 0.95)
    if onset in ("ph", "v"):
        return ("FF", 0.080, 0.90)
    if onset in ("tr", "ch", "s", "x", "gi"):
        return ("CH", 0.075, 0.78)
    if onset in ("t", "th", "đ", "d", "n", "l", "r", "nh"):
        return ("TH", 0.065, 0.72)
    if onset == "qu":
        return ("OO", 0.075, 0.85)
    return None


def coda_to_viseme(coda: str):
    if coda in ("m", "p"):
        return ("PP", 0.095, 0.95)
    if coda in ("n", "t"):
        return ("TH", 0.060, 0.65)
    if coda in ("ch", "nh"):
        return ("EE", 0.055, 0.45)
    return None


def vowel_cluster_to_visemes(vstr: str):
    """Map Vietnamese vowel/diphthong cluster to ordered (viseme, weight, jaw) stages."""
    if not vstr:
        return [("UH", 0.45, 0.25)]
    # Check common Vietnamese diphthongs & triphthongs with smooth labial transitions
    rules = [
        ("uôi", [("OO", 0.78, 0.35), ("OH", 0.72, 0.48), ("EE", 0.68, 0.24)]),
        ("ươi", [("UH", 0.75, 0.42), ("EE", 0.68, 0.24)]),
        ("uyê", [("OO", 0.82, 0.30), ("EE", 0.80, 0.42)]),
        ("oai", [("OO", 0.72, 0.36), ("AA", 0.88, 0.72), ("EE", 0.65, 0.26)]),
        ("iêu", [("IH", 0.70, 0.28), ("EE", 0.76, 0.40), ("OO", 0.78, 0.32)]),
        ("yêu", [("IH", 0.70, 0.28), ("EE", 0.76, 0.40), ("OO", 0.78, 0.32)]),
        ("uô", [("OO", 0.82, 0.36), ("OH", 0.78, 0.50)]),
        ("ươ", [("UH", 0.78, 0.46), ("AA", 0.55, 0.48)]),
        ("iê", [("IH", 0.72, 0.30), ("EE", 0.78, 0.44)]),
        ("yê", [("IH", 0.72, 0.30), ("EE", 0.78, 0.44)]),
        ("oa", [("OO", 0.75, 0.36), ("AA", 0.90, 0.74)]),
        ("oe", [("OO", 0.75, 0.34), ("EE", 0.80, 0.48)]),
        ("ua", [("OO", 0.82, 0.35), ("AA", 0.78, 0.62)]),
        ("ưa", [("UH", 0.76, 0.42), ("AA", 0.74, 0.60)]),
        ("ia", [("IH", 0.74, 0.28), ("AA", 0.80, 0.64)]),
        ("ai", [("AA", 0.88, 0.72), ("EE", 0.70, 0.28)]),
        ("ay", [("AA", 0.82, 0.64), ("EE", 0.68, 0.28)]),
        ("ây", [("UH", 0.76, 0.46), ("EE", 0.68, 0.28)]),
        ("ao", [("AA", 0.88, 0.72), ("OH", 0.78, 0.44)]),
        ("au", [("AA", 0.84, 0.66), ("OO", 0.80, 0.34)]),
        ("âu", [("UH", 0.76, 0.46), ("OO", 0.82, 0.34)]),
        ("oi", [("OH", 0.84, 0.58), ("EE", 0.68, 0.28)]),
        ("ôi", [("OH", 0.82, 0.50), ("EE", 0.70, 0.26)]),
        ("ơi", [("UH", 0.76, 0.48), ("EE", 0.68, 0.26)]),
        ("ui", [("OO", 0.82, 0.32), ("EE", 0.68, 0.24)]),
        ("ưi", [("UH", 0.72, 0.36), ("EE", 0.68, 0.24)]),
        ("êu", [("EE", 0.80, 0.40), ("OO", 0.80, 0.32)]),
        ("eo", [("EE", 0.82, 0.50), ("OH", 0.76, 0.44)]),
        ("iu", [("IH", 0.74, 0.26), ("OO", 0.80, 0.30)]),
        ("uy", [("OO", 0.80, 0.30), ("IH", 0.72, 0.24)]),
    ]
    for pat, seq in rules:
        if vstr == pat:
            return seq
    # Single vowels
    ch = vstr[0]
    if ch in ("a", "ă"):
        return [("AA", 0.90, 0.72)]
    if ch in ("e",):
        return [("EE", 0.82, 0.52)]
    if ch in ("ê",):
        return [("EE", 0.80, 0.38)]
    if ch in ("i", "y"):
        return [("IH", 0.76, 0.26)]
    if ch in ("o",):
        return [("OH", 0.88, 0.62)]
    if ch in ("ô",):
        return [("OH", 0.84, 0.46)]
    if ch in ("u",):
        return [("OO", 0.86, 0.34)]
    if ch in ("â", "ơ", "ư"):
        return [("UH", 0.78, 0.44)]
    return [("UH", 0.65, 0.36)]


def syllable_to_events(raw_word: str):
    """Decompose one Vietnamese syllable into ordered phonetic viseme events."""
    clean, emph = strip_tone(raw_word)
    if not clean:
        return [], False
    onset = ""
    rest = clean
    for o in ONSETS:
        if rest.startswith(o):
            # Special case: 'gi' before vowel vs standalone 'gì'
            if o == "gi" and len(rest) == 2:
                onset = "gi"
                rest = "i"
            else:
                onset = o
                rest = rest[len(o):]
            break

    coda = ""
    for c in CODAS:
        if len(rest) > len(c) and rest.endswith(c):
            coda = c
            rest = rest[:-len(c)]
            break

    events = []
    ov = onset_to_viseme(onset)
    if ov:
        events.append({"v": ov[0], "w": ov[2], "jaw": 0.08 if ov[0] in ("PP", "FF") else 0.22, "rel": 0.22})

    v_seq = vowel_cluster_to_visemes(rest)
    v_share = 0.58 if (ov and coda_to_viseme(coda)) else (0.76 if (ov or coda_to_viseme(coda)) else 1.0)
    for vk, vw, vjaw in v_seq:
        events.append({"v": vk, "w": vw, "jaw": vjaw, "rel": v_share / len(v_seq)})

    cv = coda_to_viseme(coda)
    if cv:
        events.append({"v": cv[0], "w": cv[2], "jaw": 0.04 if cv[0] == "PP" else 0.16, "rel": 0.20})

    # Normalize relative durations within syllable
    tot = sum(e["rel"] for e in events) or 1.0
    for e in events:
        e["rel"] /= tot
    return events, emph


def read_wav_mono(path: Path):
    with wave.open(str(path), "rb") as wf:
        sr = wf.getframerate()
        ch = wf.getnchannels()
        sw = wf.getsampwidth()
        raw = wf.readframes(wf.getnframes())
    if sw == 2:
        data = np.frombuffer(raw, dtype="<i2").astype(np.float32) / 32768.0
    else:
        data = np.frombuffer(raw, dtype=np.uint8).astype(np.float32) / 128.0 - 1.0
    if ch > 1:
        data = data.reshape(-1, ch).mean(axis=1)
    return data, sr


def extract_speech_segments(sig: np.ndarray, sr: int, min_pause_s=0.14):
    """Detect real active speech spans in the WAV using 10ms RMS energy envelope."""
    hop = int(sr * 0.010)
    win = int(sr * 0.025)
    n_frames = max(1, (len(sig) - win) // hop + 1)
    rms = np.zeros(n_frames, dtype=np.float32)
    zcr = np.zeros(n_frames, dtype=np.float32)
    for i in range(n_frames):
        s0 = i * hop
        frame = sig[s0:s0 + win]
        rms[i] = math.sqrt(float(np.mean(frame * frame)) + 1e-10)
        zcr[i] = float(np.mean((frame[1:] >= 0) != (frame[:-1] >= 0))) if len(frame) > 1 else 0.0

    # Smooth RMS gently
    k = np.array([0.15, 0.25, 0.35, 0.25, 0.15], dtype=np.float32)
    k /= k.sum()
    rms_s = np.convolve(rms, k, mode="same")
    thresh = max(0.012, float(np.percentile(rms_s, 85)) * 0.10)
    active = rms_s > thresh

    # Bridge short gaps (< min_pause_s) inside phrases
    gap_frames = int(min_pause_s / 0.010)
    i = 0
    while i < n_frames:
        if not active[i]:
            j = i
            while j < n_frames and not active[j]:
                j += 1
            if i > 0 and j < n_frames and (j - i) < gap_frames:
                active[i:j] = True
            i = j
        else:
            i += 1

    segs = []
    i = 0
    while i < n_frames:
        if active[i]:
            j = i
            while j < n_frames and active[j]:
                j += 1
            if (j - i) >= 4:
                segs.append((i * 0.010, j * 0.010))
            i = j
        else:
            i += 1

    dur = len(sig) / float(sr)
    if not segs:
        segs = [(0.05, max(0.15, dur - 0.08))]
    return segs, rms_s, zcr, dur


def align_line(line_id: str, speaker: str, text: str, wav_path: Path):
    """Align Vietnamese phrase & syllable visemes to actual acoustic energy spans in WAV."""
    sig, sr = read_wav_mono(wav_path)
    segs, rms_s, _zcr, dur = extract_speech_segments(sig, sr)
    loud_reference = max(0.03, float(np.percentile(rms_s, 80)))

    expanded = expand_numbers(text)
    is_question = "?" in text
    # Split into clauses by punctuation (., ?, …, !)
    raw_clauses = [c.strip() for c in re.split(r"[.?!…]+", expanded) if c.strip()]
    clause_words = [[w for w in re.findall(r"[^\W\d_]+", c, flags=re.UNICODE)] for c in raw_clauses]
    clause_words = [cw for cw in clause_words if cw]
    if not clause_words:
        clause_words = [["a"]]

    # Match clauses to acoustic segments proportionally by syllable count
    all_syllables = []
    total_speech_time = sum(b - a for a, b in segs)
    if len(segs) == len(clause_words):
        pairs = list(zip(segs, clause_words))
    else:
        # Flatten words across total acoustic speech spans weighted by segment duration
        flat_words = [w for cw in clause_words for w in cw]
        pairs = []
        cursor = 0
        for idx, (s0, s1) in enumerate(segs):
            if idx == len(segs) - 1:
                take = flat_words[cursor:]
            else:
                share = max(1, int(round(len(flat_words) * ((s1 - s0) / max(1e-4, total_speech_time)))))
                take = flat_words[cursor:cursor + share]
                cursor += len(take)
            if take:
                pairs.append(((s0, s1), take))

    # Within each acoustic segment, distribute syllable durations weighted by local energy & syllable complexity
    n_out = max(2, int(math.ceil(dur * FPS)) + 1)
    vis = {k: np.zeros(n_out, dtype=np.float32) for k in VISEME_KEYS}
    jaw = np.zeros(n_out, dtype=np.float32)
    brow = np.zeros(n_out, dtype=np.float32)
    nod = np.zeros(n_out, dtype=np.float32)
    breath = np.zeros(n_out, dtype=np.float32)

    aligned_debug = []

    for seg_idx, ((t0, t1), words) in enumerate(pairs):
        # Pre-phrase gentle inhale breath 0.22s before clause starts
        b_start = max(0.0, t0 - 0.25)
        for fi in range(int(b_start * FPS), min(n_out, int(t0 * FPS) + 2)):
            u = (fi / FPS - b_start) / max(0.05, t0 - b_start)
            breath[fi] = max(breath[fi], math.sin(max(0.0, min(1.0, u)) * math.pi) * 0.75)

        # Compute cumulative acoustic energy CDF inside [t0, t1] so syllables lock onto real vocal peaks!
        i0 = max(0, int(t0 * 100))
        i1 = min(len(rms_s), max(i0 + 2, int(t1 * 100)))
        e_slice = np.maximum(rms_s[i0:i1], 0.008) ** 0.65
        cdf = np.cumsum(e_slice)
        cdf = (cdf - cdf[0]) / max(1e-5, cdf[-1] - cdf[0])
        times_slice = np.linspace(t0, t1, len(cdf))

        # Syllable relative weights (longer diphthongs / final word slightly longer)
        syl_weights = []
        for wi, w in enumerate(words):
            ev, _ = syllable_to_events(w)
            wt = 0.85 + 0.22 * len(ev) + (0.28 if wi == len(words) - 1 else 0.0)
            syl_weights.append(wt)
        w_cdf = np.cumsum([0.0] + syl_weights)
        w_cdf /= w_cdf[-1]

        for wi, w in enumerate(words):
            st0 = float(np.interp(w_cdf[wi], cdf, times_slice))
            st1 = float(np.interp(w_cdf[wi + 1], cdf, times_slice))
            if st1 <= st0 + 0.02:
                st1 = min(dur, st0 + 0.04)
            events, emph = syllable_to_events(w)
            aligned_debug.append((st0, st1, w, [e["v"] for e in events]))

            # Subtle prosodic brow accent & head nod on stressed/question syllables
            accent_str = (0.55 if emph else 0.18) + (0.35 if (is_question and wi >= len(words) - 2) else 0.0)
            if wi == 0 or emph or (is_question and wi == len(words) - 1):
                mid_t = 0.5 * (st0 + st1)
                for fi in range(max(0, int((st0 - 0.06) * FPS)), min(n_out, int((st1 + 0.12) * FPS))):
                    ft = fi / FPS
                    g = math.exp(-(((ft - mid_t) / max(0.08, (st1 - st0) * 0.75)) ** 2))
                    brow[fi] = max(brow[fi], min(1.0, accent_str * g))
                    nod[fi] = max(nod[fi], min(1.0, (0.42 if emph else 0.22) * g))

            # Place phonetic visemes across [st0, st1] modulated by actual local RMS energy!
            cur = st0
            for ev in events:
                ev_dur = max(0.025, (st1 - st0) * ev["rel"])
                et0, et1 = cur, min(st1, cur + ev_dur)
                cur = et1
                # Anticipatory lead (35ms) + soft release (45ms) for realistic co-articulation
                lead = 0.045 if ev["v"] in ("PP", "FF", "OO", "OH") else 0.028
                tail = 0.045
                f_start = max(0, int((et0 - lead) * FPS))
                f_end = min(n_out, int((et1 + tail) * FPS) + 1)
                center = 0.5 * (et0 + et1)
                half_w = max(0.032, 0.55 * (et1 - et0) + 0.025)
                for fi in range(f_start, f_end):
                    ft = fi / FPS
                    # Smooth raised cosine / Gaussian co-articulation pulse
                    dist = abs(ft - center) / half_w
                    if dist >= 1.5:
                        continue
                    pulse = math.exp(-(dist * dist * 1.35))
                    # Modulate vowel intensity gently by real acoustic loudness at ft
                    r_idx = min(len(rms_s) - 1, max(0, int(ft * 100)))
                    loud = min(1.15, max(0.45, rms_s[r_idx] / loud_reference))
                    val = ev["w"] * pulse * (1.0 if ev["v"] in ("PP", "FF") else loud)
                    vis[ev["v"]][fi] = max(vis[ev["v"]][fi], min(1.0, val))
                    # Dampen jaw during bilabial closure PP / labiodental FF!
                    if ev["v"] == "PP" and val > 0.45:
                        jaw[fi] = min(jaw[fi], 0.06)
                    else:
                        jaw[fi] = max(jaw[fi], min(0.85, ev["jaw"] * pulse * loud))

    # Temporal smoothing (human lip biomechanics: ~65ms smooth response, preventing jitter)
    smooth_k = np.array([0.16, 0.24, 0.30, 0.20, 0.10], dtype=np.float32)
    smooth_k /= smooth_k.sum()
    for k in VISEME_KEYS:
        vis[k] = np.clip(np.convolve(vis[k], smooth_k, mode="same"), 0.0, 1.0)
    jaw = np.clip(np.convolve(jaw, smooth_k, mode="same"), 0.0, 0.85)
    # Re-enforce crisp bilabial closures (PP suppresses jaw & open vowels!)
    pp_mask = np.clip((np.maximum(vis["PP"], vis["MM"]) - 0.28) * 1.8, 0.0, 1.0)
    jaw *= (1.0 - pp_mask * 0.85)
    for open_v in ("AA", "OH", "EE", "UH"):
        vis[open_v] *= (1.0 - pp_mask * 0.72)

    brow_k = np.array([0.1, 0.2, 0.4, 0.2, 0.1], dtype=np.float32)
    brow = np.clip(np.convolve(brow, brow_k, mode="same"), 0.0, 1.0)
    nod = np.clip(np.convolve(nod, brow_k, mode="same"), 0.0, 1.0)
    breath = np.clip(np.convolve(breath, brow_k, mode="same"), 0.0, 1.0)

    # At the end of the clip the rig returns to its neutral mouth, including short takes
    # whose final utterance is close to the end of the WAV.
    release = np.clip((dur - np.arange(n_out) / FPS) / 0.075, 0.0, 1.0)
    for channel in (*vis.values(), jaw, brow, nod, breath):
        channel *= release

    # Compact Unity JsonUtility-friendly structure (rounded to 3 decimals)
    frames = []
    for fi in range(n_out):
        frames.append({
            "t": round(fi / FPS, 3),
            "pp": round(float(vis["PP"][fi]), 3),
            "ff": round(float(vis["FF"][fi]), 3),
            "th": round(float(vis["TH"][fi]), 3),
            "ch": round(float(vis["CH"][fi]), 3),
            "aa": round(float(vis["AA"][fi]), 3),
            "ee": round(float(vis["EE"][fi]), 3),
            "ih": round(float(vis["IH"][fi]), 3),
            "oh": round(float(vis["OH"][fi]), 3),
            "oo": round(float(vis["OO"][fi]), 3),
            "uh": round(float(vis["UH"][fi]), 3),
            "mm": round(float(vis["MM"][fi]), 3),
            "jaw": round(float(jaw[fi]), 3),
            "brow": round(float(brow[fi]), 3),
            "nod": round(float(nod[fi]), 3),
            "breath": round(float(breath[fi]), 3),
        })

    track = {
        "lineId": line_id,
        "speaker": speaker,
        "text": text,
        "duration": round(dur, 3),
        "fps": int(FPS),
        "method": "vietnamese_g2p_acoustic_envelope_heuristic",
        "audioSha256": hashlib.sha256(wav_path.read_bytes()).hexdigest(),
        "frames": frames,
    }
    return track, sig, sr, aligned_debug


def render_verification_plot(samples_debug, evidence_dir=EVIDENCE_DIR):
    """Render visual waveform + aligned Vietnamese syllables + viseme curves for QA evidence."""
    w, row_h = 1500, 215
    h = 74 + len(samples_debug) * row_h
    im = Image.new("RGB", (w, h), (16, 22, 21))
    draw = ImageDraw.Draw(im)
    f_hdr = ImageFont.truetype(str(FONT_PATH), 20)
    f_sub = ImageFont.truetype(str(FONT_PATH), 13)
    f_lbl = ImageFont.truetype(str(FONT_PATH), 14)
    f_syl = ImageFont.truetype(str(FONT_PATH), 12)
    draw.text((20, 14), "M2-C OFFLINE VIETNAMESE PHONEME-TO-VISEME FORCED ALIGNMENT VERIFICATION", fill=(232, 232, 214), font=f_hdr)
    draw.text((20, 42), "Waveform (slate) | AA (red), EE (green), OH (amber), OO (blue), PP (magenta), Jaw (white) aligned to real VO WAV timestamps", fill=(156, 178, 168), font=f_sub)

    colors = {
        "aa": (235, 110, 90),
        "ee": (110, 215, 155),
        "oh": (240, 190, 90),
        "oo": (125, 175, 240),
        "pp": (220, 120, 230),
        "jaw": (245, 245, 235),
    }

    for idx, (track, sig, sr, dbg) in enumerate(samples_debug):
        y0 = 70 + idx * row_h
        draw.rectangle([16, y0 + 4, w - 16, y0 + row_h - 8], fill=(22, 30, 29), outline=(54, 70, 65))
        draw.text((28, y0 + 10), f"{track['lineId']} ({track['speaker']}, {track['duration']:.2f}s): {track['text']}", fill=(230, 235, 220), font=f_lbl)

        x_left, x_right = 30, w - 30
        plot_w = x_right - x_left
        mid_y = y0 + 96
        dur = max(0.1, track["duration"])

        # Draw waveform
        step = max(1, len(sig) // plot_w)
        for px in range(plot_w):
            s0 = int((px / plot_w) * len(sig))
            chunk = sig[s0:s0 + step]
            amp = float(np.max(np.abs(chunk))) if len(chunk) else 0.0
            dy = int(amp * 44)
            draw.line([(x_left + px, mid_y - dy), (x_left + px, mid_y + dy)], fill=(70, 96, 94))

        # Draw aligned syllable labels
        for st0, st1, word, vlist in dbg:
            sx0 = x_left + int((st0 / dur) * plot_w)
            sx1 = x_left + int((st1 / dur) * plot_w)
            draw.line([(sx0, y0 + 34), (sx0, y0 + 145)], fill=(42, 58, 55))
            if sx1 - sx0 >= 22:
                draw.text((sx0 + 3, y0 + 34), word, fill=(190, 220, 200), font=f_syl)

        # Draw viseme curves
        base_y = y0 + row_h - 18
        frames = track["frames"]
        for key, col in colors.items():
            pts = []
            for fr in frames:
                px = x_left + int((fr["t"] / dur) * plot_w)
                py = base_y - int(fr[key] * 58)
                pts.append((px, py))
            if len(pts) >= 2:
                draw.line(pts, fill=col, width=2)

    evidence_dir.mkdir(parents=True, exist_ok=True)
    out = evidence_dir / "lipsync-verification.png"
    im.save(out, optimize=True)
    print(f"[LIPSYNC] Saved visual verification plot -> {out}")


def main(evidence_dir=EVIDENCE_DIR, lines=None):
    raw = json.loads(DIALOGUE_JSON.read_text(encoding="utf-8"))
    entries = {e["id"]: e for e in raw["entries"]}
    def clean_quote(eid, n=1):
        txt = entries[eid]["copy"]
        m = re.findall(r"“([^”]+)”", txt)
        return m[n - 1].strip() if (m and n <= len(m)) else txt.strip()

    # All character-spoken lines in M2–M3 slice (Lan, Bé An, Gatekeeper, Bà Lành)
    character_lines = [
        ("CH01-D03", "Lan", clean_quote("CH01-D03")),
        ("CH01-D03c", "Lan", clean_quote("CH01-D03c")),
        ("CH01-D04c", "Lan", clean_quote("CH01-D04c")),
        ("CH01-D05", "Lan", clean_quote("CH01-D05")),
        ("CH01-D05c", "Lan", clean_quote("CH01-D05c")),
        ("CH01-D06", "Minh", clean_quote("CH01-D06")),
        ("CH01-D07", "Lan", clean_quote("CH01-D07")),
        ("CH01-D08", "Lan", clean_quote("CH01-D08")),
        ("CH01-D09", "Lan", clean_quote("CH01-D09")),
        ("CH01-D16", "Lan", clean_quote("CH01-D16")),
        ("CH02-D03", "An", clean_quote("CH02-D03")),
        ("CH02-D05", "Gatekeeper", clean_quote("CH02-D05")),
        ("CH03-D03", "BaLanh", clean_quote("CH03-D03")),
        ("CH03-D05", "Gatekeeper", clean_quote("CH03-D05")),
    ]
    if {"CH01-D16b", "CH04-D06"}.intersection(line_id for line_id, _, _ in character_lines):
        raise RuntimeError("LOCK-V22-02: CH04 reveal lines cannot enter this lip-sync pack")

    if lines:
        selected = set(lines)
        if unknown := selected - {line_id for line_id, _, _ in character_lines}:
            raise ValueError("Unknown lip-sync line IDs: " + ", ".join(sorted(unknown)))
        character_lines = [line for line in character_lines if line[0] in selected]

    OUT_RES_DIR.mkdir(parents=True, exist_ok=True)
    OUT_SRC_DIR.mkdir(parents=True, exist_ok=True)

    missing = [line for line, _, _ in character_lines if not (VO_DIR / f"{line}.wav").is_file()]
    if missing:
        raise FileNotFoundError("Missing character WAVs: " + ", ".join(missing))
    debug_plot_items = []
    for line_id, speaker, text in character_lines:
        wav_path = VO_DIR / f"{line_id}.wav"
        track, sig, sr, dbg = align_line(line_id, speaker, text, wav_path)
        payload = json.dumps(track, ensure_ascii=False, separators=(",", ":"))
        (OUT_RES_DIR / f"{line_id}.json").write_text(payload, encoding="utf-8")
        (OUT_SRC_DIR / f"{line_id}.json").write_text(json.dumps(track, ensure_ascii=False, indent=2), encoding="utf-8")
        print(f"[LIPSYNC] {line_id:8s} ({speaker:10s}) {track['duration']:4.2f}s | {len(track['frames']):3d} frames | {len(dbg):2d} syllables -> {line_id}.json")
        if line_id in ("CH01-D03", "CH01-D05", "CH01-D07", "CH02-D03", "CH02-D05"):
            debug_plot_items.append((track, sig, sr, dbg))

    if debug_plot_items:
        render_verification_plot(debug_plot_items, evidence_dir)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--evidence-dir", type=Path, default=EVIDENCE_DIR)
    parser.add_argument("--lines", nargs="+", help="Align only the specified dialogue IDs")
    args = parser.parse_args()
    main(args.evidence_dir, args.lines)
