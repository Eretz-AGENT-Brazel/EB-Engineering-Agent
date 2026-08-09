---
name: tankforge-shell-built
description: What the TankForge software shell already contains (phase 1 done)
metadata: 
  node_type: memory
  type: project
  originSessionId: f91b529d-dc89-40b6-9887-8daf2489a303
---

Phase 1 of [[tankforge-project]] (software shell) is built and verified. Added on top of the original prototype without breaking the 3D model / sliders / UL-142 calc:

- `shell.css` + `shell.js` (new); `index.html` restructured into a `#workspace` (app bar + `#app` row). `model.js`, `ui.js`, `style.css` untouched.
- App bar: brand, dropdown menus (קובץ/עריכה/תצוגה/עזרה), editable project name, save/export buttons (disabled placeholders).
- Floating tool rail (AutoCAD-style): select/move/rotate/measure/dim/annotate/section. Only "section" is wired (jumps to cut panel); rest are visual placeholders.
- Right dock with tabs: PROPERTIES (empty state, selection not wired yet) and AGENT chat.
- Agent chat is UI-only: greeting, suggestion chips, send (canned honest reply), and WORKING voice dictation via Web Speech API (he-IL, Chrome). No engine connected.

Preview: static site served via `.claude/launch.json` (python http.server on port 8123, dir "web 2"). Notes: (1) headless preview throttles requestAnimationFrame so the 3D viewport looks frozen/black in screenshots AND preview_screenshot wedges on the live WebGL loop — verify via preview_eval instead; it renders fine in a real browser. (2) http.server sends no cache headers so the browser caches JS — assets carry a `?v=N` query that must be bumped on every change (currently v=5).

Bugs fixed along the way: removed an `Object.assign(mesh,{position:...})` in buildManholes that threw "Cannot assign to read only property 'position'" and broke the whole render; removed the ground plane (showed as a blue edge-on line); added self-healing `ensureSize()` in the render loop (canvas was stuck 300×150 with broken camera.aspect at init).

**Standards work (done):** UL 142 (aboveground) + UL 58 (underground) PDFs are in `C:\Users\User\Desktop\Claude\SKILLS\Steel Tank\תקנים - UL\`. Created `web 2/KNOWLEDGE.md` (cited rule base = build spec + future agent training). Added a "סוג מיכל ותקן" sidebar panel: installation (above/under) drives the governing standard AND the available construction types — above: single/secondary/diked (UL 142 terms); under: single/TYPE II (standoffs)/TYPE I (wrapped 300–360°, UL 58 terms). KEY FACT: Type I/II are UL 58 (underground) only; UL 142 has no Type I/II. `runCalc()` routes to runCalcUL142 (Table 13.1 thickness by capacity AND diameter; L/D≤6) or runCalcUL58 (min shell 3.12/2.36mm, head Table 5.1, L/D≤8 or ≤5 for Type I; external-pressure Roark calc flagged ⏳ NOT yet implemented — it's the governing underground calc). Single-wall hides the inner shell.

**2026-06-16 — folder loss + English rebuild:** the `web 2\` folder was lost; the whole project was rebuilt from conversation context into `C:\Users\User\Desktop\.claude\web\` and fully translated to **English / LTR** (assets cache-busted to `?v=7`; `.claude/launch.json` now serves `--directory .claude/web` on port 8123). Verified: lang=en, dir=ltr, no Hebrew in UI, 60 fps, UL 142↔UL 58 routing works.

**Standards deep-dive: DONE (solo).** The multi-agent `ul-standards-mastery` workflow failed TWICE — spawning ~40 agents (~925K tokens each run) instantly exhausts the account **session limit**, producing 0 files. Lesson: for this account, big agent fan-outs are a non-starter; do standards extraction in the main loop instead. I read UL 58 + UL 142 fully from `C:\Users\User\Desktop\_stdsrc\` and hand-wrote three exhaustive, clause-cited English docs into `.claude/web/standards/`: **UL58.md** (12.8KB — incl. full Roark §5.2 vars + teq, stiffeners, all tables 5.1/5.2/9.1/10.1/10.2/13.x/3.1/3.2/3.3, testing, marking), **UL142.md** (9.6KB — scope, materials+CE formula, joints Table 6.1, Table 13.1, heads/compartment, secondary, diked, supports, tests, marking, capacity/wetted-area), **COMPUTABLE_RULES.md** (5.1KB — calc-engine spec grouping every computable rule by id/§/formula/status). A 3rd dock tab "Standards" (Standards Library) shows highlights of both with §citations + status chips; banner now points to the full docs. Roark closed-form is an image in the source PDF — only variables are machine-readable; flagged to transcribe before coding.

**Current editions (researched 2026-07):** our digests are from **Edition 9** of each, but the CURRENT editions are **UL 142 Ed. 11 (12 Dec 2025)** — added scope for tanks operating >1 psi and <15 psi, plus renumbering — and **UL 58 Ed. 10 (31 Jan 2018)** — largely editorial. Core engineering (Type I/II, Roark §5.2, Table 13.1/5.1, L/D, testing) is stable across editions, but clause numbers shifted; cite the current edition for any formal UL submittal. (UL also publishes a "UL58-Roark" calc spreadsheet.)

**Packaged & shared (2026-07):** the standards knowledge is now a personal skill **`steel-tank-standards`** at `C:\Users\User\.claude\skills\steel-tank-standards\` (SKILL.md + references/UL58.md, UL142.md, COMPUTABLE_RULES.md). Also published as a Claude Team org onboarding guide for the company (esp. bernie@eretzbarzel.com): **https://claude.ai/claude-code/onboard/_B9Iwt5NCIck** (short_code `_B9Iwt5NCIck`; source `TankForge/ONBOARDING.md`). Org guides are team-wide (can't target one email); Bernie accesses via the link.

Next up per roadmap: implement the Roark external-pressure calc (governing underground check); element selection + live PROPERTIES editing. Deferred: TYPE I partial-wrap geometry, Diked geometry, remaining table lookups (vent sizing, vertical/secondary thickness).
