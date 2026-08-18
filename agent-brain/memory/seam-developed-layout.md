---
name: seam-developed-layout
description: TankForge — professional developed-plate layout that accounts for I.W.S/O.W.S weld seams (the staggered TOP/BOTTOM-line drawing)
metadata: 
  node_type: memory
  type: project
  originSessionId: f91b529d-dc89-40b6-9887-8daf2489a303
---

Amir's lesson (2026-07-15) on presenting the plate development **professionally**, accounting for weld-seam angles. This is the key to positioning openings correctly, because **plates are CNC-cut FLAT first, then rolled** — so opening/seam positions on the flat plate depend on the seam angle. Continue this example into the OPENINGS topic. See [[vessels-stage1]], [[head-geometry-flat]].

**Core formulas doc (bilingual, EB-VESSELS-FORM-001), in `C:\Users\User\Desktop\EB VESSELS AGENT\VESSLES SOFTWARE- STAGE 1\`:** `EB-VESSELS - נוסחאות פריסת מעטפת (עברית).docx/.pdf` (RTL, bidi-fixed) + `EB-VESSELS - Shell Layout Formulas (English).docx/.pdf`. Living doc — keep expanding. Built via docx-js (`scratchpad/build_formulas2.js`), rendered/verified via Word COM (LibreOffice/pandoc absent; the skill's soffice.py is Unix-only). RTL fixes (both needed): (a) split each line into Hebrew (rightToLeft) vs Latin/number (LTR) runs or "I.D"→"D.I" reverses; (b) **paragraph alignment must be `AlignmentType.START`, NOT `RIGHT`** — under `<w:bidi/>` Word MIRRORS jc, so `RIGHT` renders flush-LEFT and `START` renders flush-right (verified by rendering a test doc). Centered title/formulas use CENTER (not mirrored). Original source: `C:\Users\User\Desktop\קובץ נוסחאות.docx`.

**Formulas:**
- Formula 1 (INNER): `L_iws = (X°/360°) · π · (I.D + t)`  — X° = angle between adjacent I.W.S seams.
- Formula (OUTER): `L_ows = (Y°/360°) · π · O.D`  — note: uses **O.D**, not (O.D − t). *(open question — asymmetry vs inner's mid-wall.)*

**Worked example:** I.D 2052, inner t 8, I.W.S = ±40° (⇒ 80° between seams).
- 80° ⇒ **1438 mm** ( (80/360)·π·(2052+8) = (80/360)·π·2060 = 1438 ). 40° ⇒ **719 mm** (half).
- Developed plate CUT length in the drawing = **6469 mm** = π·2060 (=6471.7) **− 3 mm weld gap** → open question vs lesson-1 where 10,085 = ceil(π·midwall) with NO gap subtracted.

**The layout (from Amir's drawing):** plates laid side-by-side by width (horizontal = axial, total 6631 = 2015+577+2015+2015+3·gap). Vertical = circumference. Alternate courses are **staggered vertically by 1438 mm** (the 80° arc) to represent the ±40° seam alternation. **TOP LINE / BOTTOM LINE / mid-line** drawn across and labelled; 719 (40°) and 1438 (80°) dimensioned; weld gaps (3) marked between plates. Openings are then positioned relative to these lines. Two "TOP LINE"s (strip edges) + one "BOTTOM LINE" (middle) suggests the strip is centred on the tank bottom.

**RESOLVED (Amir, 2026-07-15) + IMPLEMENTED (b=32):**
1. Both shells use **mid-wall / neutral fibre**: inner `π(I.D+t)`, outer `π(O.D−t)` (the docx's `O.D` for outer was wrong). `ebMidWallDia` + `ebSeamArc` in geometry.js.
2. Developed CUT length **subtracts the weld gap**: `ebPlateLength = ceil(π·midWall − weldGap)`. (Overrides lesson-1's 10,085 which omitted it.)
3. Strip = **2 TOP edges + 1 BOTTOM centre** (peel now pivots about the bottom). Alternate courses (even index = seam +wsDeg = "top when viewed from top") **raised by the 80°-arc stagger** (1438 for the example); odd stay. **No stagger when wsDeg = 0** (outer default O.W.S = 0 → outer not staggered, which is correct). Reference lines TOP/BOTTOM/TOP drawn across each shell + `stagger` / `½` dimension labels. In `unroll.js`.

Verified vs example: PLATE LENGTH 6469, seam-arc 80°→1438 / 40°→719, stagger 1438 (30°→1079), live-edit refresh works. **STILL a first visual pass — the exact line placement/stagger convention is being reviewed against Amir's drawing.** Next: openings on the developed plate (see PENDING checklist).
