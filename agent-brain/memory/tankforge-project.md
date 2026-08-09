---
name: tankforge-project
description: "TankForge — internal web platform for designing steel fuel tanks (ארץ ברזל), with planned embedded AI agent"
metadata: 
  node_type: memory
  type: project
  originSessionId: f91b529d-dc89-40b6-9887-8daf2489a303
---

TankForge is an internal platform Amir (ארץ ברזל / Eretz Barzel) is building to design steel fuel tanks in 3D, eventually with an embedded AI "agent" (Jarvis-like) that takes voice+text modeling commands, enforces standards, and produces precise production drawings. Goal: replace 2D AutoCAD workflow and eventually get מכון התקנים approval.

Source files live in `C:\Users\User\Desktop\TankForge\` — vanilla HTML/CSS/JS + Three.js (CDN, no build). **Consolidated 2026-06-18** into this single clean folder (was `web 2\`, then `.claude\web\`, both gone now). Layout: `app\` = the software (index.html, model.js, ui.js, style.css, shell.css, shell.js, KNOWLEDGE.md, standards\); `knowledge\` = office-brain (elements/materials/drawing-conventions/examples); `reference\` = standards-pdf + extracted-text; `RUNNING-PLAN.md` = program steering doc; `TankForge.lnk` + `launcher.vbs` = double-click launcher (starts `python -m http.server 8123` in app\ and opens the browser); `Stop TankForge.bat`. `.claude/launch.json` tankforge config now serves `TankForge/app`. The UI is **English / LTR** (was Hebrew/RTL) to align with the UL standards. The vision doc is `STEEL TANK WEB AND AGENT.pdf` (in their Google Drive, PHASE 3 folder).

**Agreed roadmap:** (1) software shell ✅ done; (2) element selection + PROPERTIES; (3) data layer — project save/load + POSITION NUMBERs; (4) AI agent (text→voice→sketch) — migrate to React/Vite at this point; (5) production output (shell/cap unrolling, PDF) — the hardest and most important phase per the doc.

**Architecture decision:** stay vanilla for now; move to React/Vite when the agent + complex state arrive (phase 4). See [[tankforge-shell-built]].

Key tank constants from the doc: steel plates std width 2000/2500mm (sometimes 1000/1500), length 3000-12000mm, thickness usually 6/8mm (up to 20mm+ per standard); steel 7.9 ton/m³. Each tank is a bespoke design — NOT a production line.
