---
name: vessels-stage1
description: "VESSLES SOFTWARE- STAGE 1 — Amir's spec folder for evolving TankForge into \"EB – Steel Tank Modeling Software\"; Opus 4.8 build brief written"
metadata: 
  node_type: memory
  type: project
  originSessionId: f91b529d-dc89-40b6-9887-8daf2489a303
---

`C:\Users\User\Desktop\EB VESSELS AGENT\VESSLES SOFTWARE- STAGE 1\` is the Stage-1 spec/knowledge folder for evolving [[tankforge-project]] into **"EB – Steel Tank Modeling Software"** (brand: EB-VESSELS). **Amir's rule (2026-07-13): EVERYTHING lives inside `TankForge\`** — the spec folder was moved into it; code is developed directly in `TankForge\app\` (no copies). NOTE: an empty leftover shell of the old folder may remain on the Desktop (the Knowledge guide .docx was open/locked in Word during the move) — safe to delete once Word is closed.

**The deliverable I produced (2026-07-12):** `OPUS-4.8-DEVELOPMENT-BRIEF.md` — a full implementation brief for **Opus 4.8** (the model Amir designated to write the code), + `_BRIEF-ASSETS\` (extracted doc texts, the knowledge guide's 7 embedded drawings, 10015 GA render).

Key domain facts learned from the folder (docx sources: Vision + Knowledge guide):
- **Steel density 7,850 kg/m³** (correction: app's calcGeom used 7900 — must fix). Units: kg.
- Cups (heads) are called **CUP** in Amir's UI naming (INNER/OUTER CUP, RIGHT/LEFT by FRONT view). Flat CNC disc (Ø19.5mm center hole) → edge-bent. **PREBEND DIAMETER = computed, read-only** (CNC instruction); AFTERBEND + BENDING LENGTH editable. Ø>plate width → 2 pieces + center seam. Default junction: cup inserted into shell, 2–5mm weld gap.
- Shell = **courses**: plate ordered 2000→arrives **2015mm**, 3mm weld gap between courses; plate length **L=(I.D+t)×π** (worked example: I.D3200 t8 → ≈10,080; layout 5×2015+1500+gaps=11,590).
- **Invariants:** openings must not land on course seams; longitudinal seams (I.W.S/O.W.S) staggered so **max 3 welds meet** (never 4); example: OWS at top, IWS ±40° alternating.
- **HEAT NUMBER** traceability per plate + certified welder per seam → per-project "Material Quality" booklet (example: tank 10015, customer Hershkovitz).
- `PLANES EXAMPLES\10015 TII.pdf` = the gold-standard A0 GA drawing (UL 58 TYPE II Ø1800) the Documents module must eventually reproduce.
- Stage-1 product spec: branded home screen (image supplied) → New/Edit model wizard (name/project#/client/volume/above-under/type/folder + auto description), TANK FILE & STANDARD tab, Standards+Properties move LEFT, new DOCUMENTS tab RIGHT (AI Agent stays), light/dark toggle, save/load project JSON, per-course & per-cup PROPERTIES engine, element toggles (manhole/neck/flange/שוחה/baffles/rings/legs — deep specs later + checklist), ASME pressure vessels = "coming soon" (future track).
- Brief recommends copying `TankForge\app` → `VESSLES SOFTWARE- STAGE 1\app` as the working copy (pending Amir's OK).

**BUILD STATUS (Opus 4.8, 2026-07-13): Stage-1 P0–P5 built & verified in-browser** (developed directly in `TankForge\app`; spec folder moved to `TankForge\VESSLES SOFTWARE- STAGE 1`). New app files: `tankmodel.js` (pure course/head/opening/weld engine), `properties.js` (selection PROPERTIES + live edit), `home.js`+`home.css` (home screen, new-model wizard, save/open `.ebtank`, light/dark theme), `stage1ui.js` (TANK FILE tab, element toggles, Documents generator). Assets in `app/assets/`. Cache-busting via `?b=N` query on local assets (bump on change; currently b=7 — the app is served over the local http.server so query is safe). Kickoff decisions: heads=**HEAD**, inset=per-project, `.ebtank`=Windows assoc, volume→auto-suggest dims. Verified acceptance: course layout 11590→[2015×5,1500]; plate length (Ø−t)·π (ID3200/t8→10078); PREBEND 3467+80→3547; density 7850. **Deferred (per brief / flagged):** Properties+Standards still in RIGHT dock (left-migration pending), UL 58 Roark calc, ASME, agent engine, real drawings, deep element geometry (see `VESSLES.../PENDING-ELEMENTS-CHECKLIST.md`), seamless `.ebtank` auto-load. Install: `TankForge/install-ebtank-association.bat`.

**2026-07-13 (later): opening rendering fixed** — replaced the ugly full-height slot-cut (partial CylinderGeometry) with `courseCylWithHoles()` in model.js: a custom BufferGeometry cylinder (axis along X, +Y top) that cuts **real round holes** where openings sit (skips quads inside each hole in x–arc space), + collar pad + weld bead. Verified by ray-cast (downward ray passes through the top hole and hits the far wall). Assets cache-bumped to `?b=10`.

**New skill created: `steel-tank-manufacturing`** (`C:\Users\User\.claude\skills\steel-tank-manufacturing\` — SKILL.md + references/manufacturing-process.md). v1 starter from web research: fabrication process (plate→courses→heads→fit-up→weld→NDT→test→fittings), materials/traceability (EN 10204 3.1 heat numbers), welding (SAW/FCAW + distortion), NDT, testing, fittings, and the design-code decision guide (UL 142/UL 58/API 650/620/ASME VIII/EN 14015). Companion to `steel-tank-standards`. To be expanded with Eretz Barzel shop practice.
