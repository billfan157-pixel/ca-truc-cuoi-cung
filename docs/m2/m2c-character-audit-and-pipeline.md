# Ca trực cuối cùng · M2-C Character Audit, Digital Human Research & Pipeline Specification

**Date:** 2026-11-19 · **Baseline:** M2-A (`49bc06837cf3ebc72348ca4a5f86575f371a108a`) · **Core Design System:** v2.2  
**Production Stack:** Blender 4.5.10 LTS + MPFB 2.0.18 (`hm08` CC0) · Unity 6.3 LTS (`6000.3.28f1`) · URP 17.3 (Forward+)  
**Reference Hardware:** Intel Core Ultra 7 155H · Intel Arc iGPU · 16 GB RAM · Windows x64 D3D12 (1080p Medium @ 60 Hz VSync)

---

## 1. M2-A Baseline Character Audit (Evidence-Based)

Direct inspection of `ArtSource/Blender/CTCC_CHR_*.blend`, `tools/blender_assets/build_characters.py`, `tools/bake_m2_character_maps.py`, `output/m2a/m2a-blender-inspection.jpg`, and in-game 1920×1080 Unity captures (`output/m2a/release-module/loc02-lan-B.png`, `loc04-gatekeeper-R.png`) identified critical visual, anatomical, rigging, and performance failures that block high-end psychological horror believability:

| Character | M2-A Source Metrics | Critical Visual & Technical Defects Audited |
| --- | --- | --- |
| **CHAR002 · Trần Ngọc Lan** (`CTCC_CHR_Lan.blend`) | 18,636 tris across 5 meshes · 21 bones · 9 shape keys · 4 actions · No LODs | **1. Primitive geometry:** Head is a `primitive_uv_sphere_add` deformed by 12 analytic Gaussians (`make_sculpted_head`); torso/sleeves/trousers are radial polyline lofts; palms/fingers/shoes are beveled boxes and cylinders. **2. Missing anatomy:** Zero mouth cavity, zero teeth, zero tongue, flattened sphere ears, detached floating raycast eyelid patches, no eyebrows/eyelashes geometry. **3. Severe UV/skin-tone defect (visible in `loc02-lan-B.png`):** Cylindrical portrait UV (`LanSkin_Color.png`) paints dark hairline (`v > 0.74`) and under-chin shadow (`v < 0.20`), while box-UV-projected neck and forearms wrap onto the dark hairline region—causing Lan's neck and forearms to render dark brown/mismatched under reception lighting. **4. Hair & garment limits:** Solid dark helmet dome (`SmoothHairCap`) + sphere bun + 2 tubes; stiff conical blouse with glued-on box pockets and no cloth simulation folds; ID badge clips into chest pocket. **5. Rig & performance limits:** 21-bone rig has no jaw, no individual finger phalanges, no toe base, no clavicle/twist deformation; animations are pure mathematical sine-wave Euler rotations (`bake_pose_action`); lip-sync is raw RMS + zero-crossing audio amplitude flapping (`CharacterActor.cs:298-323`). |
| **CHAR007 · Bé An** (`CTCC_CHR_AnApparition.blend`) | 12,344 tris · **7 bones** · 0 shape keys · 2 actions · No LODs | **1. Extreme rig deficiency:** Only 7 bones (`Hips, Spine, Head, Arm_L, Arm_R, Leg_L, Leg_R`)—zero elbows, zero knees, zero wrists, zero ankles, zero neck; sphere hands (`AnHand_L/R`). **2. Animation defect:** `An_TurnFade` tilts the torso noticeably when viewed directly (flagged in `docs/m2/acceptance.md` §Phạm vi duyệt). **3. Shader erasure:** `HospitalAuthoring.Art.cs:585` replaces every material slot with `CTCC/ApparitionSoft`, discarding child skin, hair, striped pediatric cotton, and yellow honeycomb slippers in favor of a uniform translucent cyan silhouette. **4. Zero speech performance:** `enableLipSync = false`, `enableBlink = false` despite speaking canonical `CH02-D03`. |
| **CHAR005 · Kẻ Giữ Cửa** (`CTCC_CHR_Gatekeeper.blend`) | 14,756 tris · 20 bones · 0 shape keys · 3 actions · No LODs | **1. Primitive coat & limbs:** Conical lofted overcoat, cylinder sleeves, boxy hands/boots, egg-shaped UV sphere head. **2. Crushed readability (`loc04-gatekeeper-R.png`):** Symmetrical stiff silhouette crushes to near-pure black in LOC04 corridor with no scorched lapel catchlight, frayed cloth rim, or subtle anatomical brow/jaw depth beneath the obscured shadow. **3. Mechanical locomotion:** Sine-wave hip/leg oscillation without natural weight transfer, foot grounding, or realistic heavy stride cadence. |

---

## 2. Deep Research: Realistic Digital Human Production & URP 17.3 Adaptation

### 2.1 Anatomy, Facial Topology & Blender Studio Realistic Character Workflow
- **Anatomically credible starting mesh:** Adopting MakeHuman's `hm08` base topology via **MPFB 2.0.18** inside **Blender 4.5.10 LTS** eliminates sphere/cylinder anatomy entirely. `hm08` provides continuous quad-dominant edge flow with concentric orbital (`orbicularis oculi`) loops for clean eyelid closure, concentric labial (`orbicularis oris`) and nasolabial deformation loops for natural Vietnamese visemes and restrained emotional expressions, articulated 5-finger hands with knuckle loops, sculpted ears, clavicle/neck sternocleidomastoid definition, and real-world metric proportions.
- **Eye, Eyelid & Oral Cavity Anatomy:**
  - Separate anatomical left/right eyeballs (`eyes/high-poly` CC0) rigged to `Eye_L` / `Eye_R` bones, composed of an inner sclera + concave iris/pupil surface and a glossy convex outer cornea shell for physical refraction and wet catchlights.
  - Functional eyelids closing naturally along the eyeball curvature via anatomical `eye-left-closure` / `eye-right-closure` morph targets paired with fitted upper/lower eyelash meshes (`mindfront_eyelashes_02` CC0) deformed by matching blink blendshapes so lashes move cleanly with the lid margin.
  - Interior mouth cavity backed by CC0 upper/lower dental arches (`teeth_base`) and anatomical tongue (`tongue01`) rigged to the `Jaw` bone so wide vowels (`AA`, `OH`) reveal believable interior shadowing, teeth edge, and tongue rather than an empty flat wall.
- **Skin Shading & Micro-Detail:**
  - Seamless 2048² photo-referenced `hm08` UV skin atlas (`young_asian_female` / `young_asian_male` / `middleage_asian_male` CC0) calibrated to consistent warm Vietnamese skin tones across face, neck, ears, forearms, and hands—eliminating M2-A's neck/forearm UV mismatch permanently.
  - Dedicated PBR texture suite per character: **BaseColor** (calibrated hemoglobin variation, subtle under-eye/nasal/lip coloration, scalp darkening under hair), **Normal** (baked anatomical relief + micro-pores), **Mask** (`R=Metallic`, `G=AO/Cavity`, `B=Subsurface/Thickness`, `A=Smoothness` with oily T-zone/lip shine vs matte cheeks/neck), plus a shared tiled 512² **Micro-Pore Detail Normal** faded smoothly with camera distance.

### 2.2 Real-Time Hair Grooming & Hair Cards
- Hair helmets and tube locks fail close-up inspection. We adopt Blender Studio's **Curve-to-Hair-Cards** real-time pipeline:
  1. Groom dense 3D Blender Hair Curves on an orthographic calibration plane and render in **Blender Cycles** to bake an original **2048² Hair Card Atlas** (`HairCards_Color.png`, `HairCards_Alpha.png`, `HairCards_Normal.png`, `HairCards_Flow.png` encoding root-to-tip gradient, per-strand ID variation, and directional AO).
  2. Author Lan's canonical **2005 Vietnamese nurse low bun (`tóc buộc gọn`)** directly on her sculpted cranium in layered passes: darkened scalp base → swept crown/temple volume cards → layered twisted nape bun cards + dark-blue enamel nurse clip → delicate forehead/temple/nape baby-hair & flyaway wisps with feathered alpha edges.
  3. Fit CC0 eyebrow (`mindfront_eyebrows_02`) and eyelash (`mindfront_eyelashes_02`) alpha-card strips with exact vertex transfer for brow/blink shape keys.

### 2.3 Garment Construction & Blender Cloth Simulation
- Rather than lofts around a torso, clothing is constructed from tailored anatomical garment panels around the posed body mesh:
  - **Lan (`1895/1997/QĐ-BYT` nurse uniform):** Front left/right overlapping panels with center button placket & real sculpted buttons, tailored back panel with waist seams, standing/folded Vietnamese nurse collar with sage-teal piping trim, set-in two-piece 3/4 sleeves with cuffed trim, breast & hip patch pockets, laminated An Sinh ID badge (`Trần Ngọc Lan · Điều dưỡng`) on a metal clip, tailored charcoal-navy trousers, modest dark leather flat clinical shoes, and wristwatch.
  - **Physics Draping:** Run real **Blender 4.5 Cloth Simulation (`CLOTH` modifier)** with character body `COLLISION` mesh, cotton poplin/twill stiffness & bending parameters, and pin groups at collar/shoulders/waistband to generate genuine physical folds (shoulder pull, bust/waist drape, elbow creasing, hem fall, knee drape). Bake subdivided cloth folds into 2048² normal/AO maps, apply realistic edge thickness (`Solidify` at collar/cuffs/hems/placket), and transfer smooth anatomical skin weights from the body rig with elbow/shoulder corrective shaping.

### 2.4 Adapting Unity *The Heretic* & *Enemies* to URP 17.3 (Forward+ on Intel Arc iGPU)
Full HDRP diffusion profiles, compute strand hair, and ray-traced reflections exceed our 1080p Medium 60 Hz (`P95 ≤ 16.67 ms`) iGPU budget (`Intel Core Ultra 7 155H / Intel Arc`). We implement 5 SRP-Batcher-compatible **URP 17.3 Forward+ (`_CLUSTER_LIGHT_LOOP`)** custom HLSL shaders preserving full `DepthOnly`, `DepthNormals` (for SSAO), `ShadowCaster`, `LightProbes` SH L2 (for R/B swap), and `Fog`:

1. **`CTCC/DigitalHumanSkin`**: Pre-integrated dual-band RGB wrap subsurface scattering + warm hemoglobin translucency driven by thickness/curvature mask (`_MaskMap.b`), dual-lobe GGX specular (broad soft epidermis sheen + tighter oily micro-specular lobe), distance-faded micro-pore detail normal, and cavity/AO darkening in nostrils, eyelid creases, and lip seams.
2. **`CTCC/DigitalHumanEye`**: Dual-surface corneal refraction (IOR `1.376` parallax UV offset shifting the concave iris relative to view vector), radial iris fiber detail + dark limbal ring, opposite-light anterior-chamber caustic glow (*The Heretic* / *Enemies* technique), vascular sclera corner tint, upper-eyelid occlusion shadow band, and crisp wet corneal specular/environment reflection.
3. **`CTCC/DigitalHumanHair`**: Two-pass hair-card rendering: **Pass 1 (Opaque Cutout, `ZWrite On`, `ShadowCaster`)** locks core strand depth and casts grounded hairline shadows with zero sorting artifacts; **Pass 2 (Alpha-Blended Soft Edge, `ZWrite Off`)** renders feathered baby-hairs and flyaway tips. Lighting uses **Kajiya-Kay / Marschner dual-highlight anisotropic specular** (primary shifted specular + secondary warm-tinted transmitted highlight) plus root-to-tip AO.
4. **`CTCC/DigitalHumanFabric`**: Cotton poplin, dark twill, and Gatekeeper scorched wool/cotton shading featuring **Charlie grazing microfiber sheen**, cloth-simulated macro fold normals, tiled high-frequency weave micro-normal, and matte-soot roughness modulation.
5. **`CTCC/ApparitionHuman`** (for Bé An): Retains Bé An's full human skin, hair, striped pediatric cotton, and yellow honeycomb slipper PBR textures and lighting underneath a controllable `_Alpha` memory-vignette translucency + subtle warm ivory/pale-teal Fresnel edge glow (respecting `reduceMotion` and accessibility brightness).

### 2.5 Lifelike Performance, Motion Capture & Offline Vietnamese Lip-Sync
- **Body Animation:** Combine real optical motion capture from the **CMU Graphics Lab Motion Capture Database** (`cgspeed` MotionBuilder-friendly BVH conversions: `07_01`, `07_04` walk, `08_04` / `17_08` heavy slow walk, `16_17` walk-turn, `18_08` / `18_09` standing conversation with hand gestures) retargeted in Blender onto our 65-bone articulated rig, cleaned with **foot-contact floor clamping, hip grounding, smooth loop blending, articulated 5-finger hand poses, chest breathing, and custom hero choreography** for Lan's reception encounter (`Lan_IdleDesk`, `Lan_StepOut`, `Lan_Listen`, `Lan_TalkGesture`, `Lan_Handover` with attached 3D F101 clipboard prop!).
- **Offline Vietnamese Phoneme-to-Viseme Performance Alignment (Replacing Amplitude Flapping):**
  - Every Vietnamese VO clip (`CH01-D03..D16`, `CH02-D03`, `CH02-D05`) is decomposed deterministically by Vietnamese syllable structure (onset consonant + medial glide + nuclear vowel/diphthong + coda + tone) and aligned offline to the actual WAV acoustic envelope, voicing, spectral centroid, and F1/F2 formant energy boundaries (`tools/generate_vietnamese_lipsync.py`).
  - Outputs 30 Hz co-articulated performance JSON tracks (`Prototype/Assets/CTCC/Art/Resources/CTCC/LipSync/<LineId>.json`) driving **11 phonetic visemes** (`Viseme_PP`, `Viseme_FF`, `Viseme_TH`, `Viseme_CH`, `Viseme_AA`, `Viseme_EE`, `Viseme_IH`, `Viseme_OH`, `Viseme_OO`, `Viseme_UH`, `Viseme_MM`), `Jaw_Open`, phrase-locked **eyebrow accents**, gentle **speech head-nods**, and **breath/blink recovery timing**, synchronized directly to `voiceSource.time` in `CharacterActor.cs`.
  - Natural gaze behavior adds **eye-lead head turns** (eyes saccade toward Minh first, head follows naturally with critically damped easing), micro-saccades during eye contact, and blink-on-gaze-shift.

---

## 3. Tool, Base Mesh & Asset Licensing Evaluation Matrix

| Candidate | Quality / Fit | License & Commercial Status | Decision & Rationale |
| --- | --- | --- | --- |
| **MPFB 2.0.18 + MakeHuman CC0 System Packs** (`hm08`, `makehuman_system_assets_cc0`, `skins01_cc0`, `eyebrows01_cc0`, `eyelashes01_cc0`) | High anatomical credibility, concentric facial loops, eyes/teeth/tongue, East/SE Asian morphs & 2048² skin textures, native Blender 4.5 extension + Python API | Addon: GPL-3.0-or-later (tool only). **Core base mesh & verified system assets: CC0 1.0 Universal (Public Domain)** — free for commercial games without royalties. *(Note: individual non-CC0 community items like `rehmanpolanski_hair_bun_brown` marked AGPL3 were explicitly audited and excluded.)* | **ADOPTED (Primary Base Mesh & Anatomical Parts)** |
| **Original Blender 4.5 Hair Cards & Tailored Cloth-Sim Garments** | 100% period-accurate 1997–2005 Vietnamese hospital uniforms (`1895/1997/QĐ-BYT`), zero third-party likeness or branding (`AR03`) | **100% Original Project Creation** | **ADOPTED (All Hair, Clothing, Badges, Props)** |
| **CMU MoCap Database (`cgspeed` BVH 2010)** | Real optical human locomotion, turning, and conversational gesturing; maps directly to MPFB `cmu_mb` hierarchy | **Free for commercial use worldwide** (CMU / Bruce Hahne `READMEFIRST.txt`; acknowledgment included in `ThirdPartyNotices.md`; raw mocap not resold). | **ADOPTED (Body Motion Reference + Blender Cleanup/IK)** |
| **Bandai Namco Research Motion Dataset** | High quality stylized mocap | **CC BY-NC 4.0 (Non-Commercial)** | **REJECTED (License incompatible with commercial game)** |
| **Unreal MetaHuman** | High visual fidelity | **Unreal Engine EULA — strictly prohibited outside Unreal Engine** | **REJECTED (Illegal in Unity commercial builds)** |
| **Reallusion Character Creator 4 / Paid Base Meshes** | Commercial studio pipeline | **Paid commercial license required** | **DEFERRED / NOT PURCHASED** (No paid assets purchased; CC0 + Blender 4.5 meets M2-C quality bar without expenditure). |

---

## 4. Character Production Targets & Acceptance Gate

| Character | Target Height | LOD0 / LOD1 / LOD2 Tri Budget | Rig & Facial BlendShapes | Hero Benchmark & Acceptance Gate |
| --- | --- | --- | --- | --- |
| **CHAR002 · Trần Ngọc Lan** (Phase 2 — Build & Validate FIRST) | `1.60 m` | `LOD0 ~24–36k` · `LOD1 ~12–16k` · `LOD2 ~5–7k` | 65-bone articulated rig (5-finger hands, jaw, Eye_L/R, Clipboard socket) + 25+ facial blendshapes | Close-up Cycles + Unity neutral & LOC02 lighting renders, wireframe, expression sheet, turntable, full reception encounter footage with synced Vietnamese VO & clipboard handover. Must pass before scaling. |
| **CHAR007 · Bé An** (Phase 3) | `1.15 m` | `LOD0 ~16–22k` · `LOD1 ~8–11k` · `LOD2 ~4–5k` | Full articulated child rig (53+ bones incl. elbows/knees/fingers/eyes/jaw) + blink & Vietnamese viseme blendshapes | Preserves child skin, hair, striped cotton & yellow slippers beneath soft memory apparition effect; fixed `An_TurnFade` axis; lip-synced `CH02-D03`. |
| **CHAR005 · Kẻ Giữ Cửa** (Phase 3) | `1.94 m` | `LOD0 ~22–30k` · `LOD1 ~11–15k` · `LOD2 ~5–7k` | Full articulated adult rig + swinging `KeyRing` chain bone + jaw/brow subtle shapes | Tall austere canonical silhouette, cloth-simulated scorched overcoat with readable grazing folds, shadowed anatomical human head (no gore), weighted CMU-derived stalk gait + synced `SFX-KEY` & `CH02-D05`. |
