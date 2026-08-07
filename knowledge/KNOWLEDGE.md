# EB PROSTEEL AGENT — Master Knowledge & Operating Guide

This is the agent's brain-file for working in ProSteel like a professional steel
detailer at Eretz Barzel. Read this first; drill into the other files on demand.

## Knowledge files in this folder
- **PROSTEEL_COMMANDS.md** — all 82 ProSteel commands + what each does (the vocabulary).
- **PROSTEEL_MANUAL_INDEX.md** — full capability map (TOC of the 1179-page manual).
- **manual_fulltext.txt** — the entire manual as searchable text. *Don't read it all* —
  `grep` it for the topic you need (e.g. "Automatic Insertion", "Bolt Style", "DSTV").
- **../prosteel.py** — natural request (He/En) → exact ProSteel command (the dispatcher).

## How the agent operates
1. Connect: `from acad import Acad; a = Acad()` (AutoCAD+ProSteel must be open).
2. Fire ProSteel tools: `from prosteel import dispatch; dispatch("תכניס קורה", a)`.
3. Raw geometry / edits: use `acad.py` (lines, copy, move, layers, 3D, query).
4. Read a client plan: PDF → read directly; DWG → `a.open_drawing(path)` + `a.extract()`.

## The hard reality (and how we win anyway)
Most ProSteel commands **open a dialog** (you pick profile/params there) — they can't be
fully filled by a script. So "control like a pro" means:
- **Fire the exact right tool instantly** (the agent's value: knowing which of 82 commands).
- **Seed-and-copy** for repeated members: modeler places ONE native shape from SHAPES,
  agent duplicates it natively via AutoCAD `CopyObjects`/move/array (stays a real ProSteel object).
- **Skeleton-first**: agent draws the geometry skeleton (grid lines, member centerlines) with
  `acad.py`, then ProSteel's *Insert Shape along Line / Automatic Insertion* (B.8.7) places steel
  on the lines — this is the route to near-autonomous modeling from a 2D plan.
- **Pre-built templates** (ProSteel Template Manager / `PS_PROJECT`, `PS_GLOBAL_SETTINGS`):
  set default profiles/connections once so placement is fast and consistent.

## The Eretz Barzel production workflow (model → factory)
The agent should always think toward the end goal: **shop drawings for the factory floor.**
1. **Model** the structure in 3D (shapes, plates, bolts, connections) — PROSTEEL tab.
2. **Connections** down to the bolt: `PS_BOLT`, `PS_ENDPLATE`, `PS_GUSSET_PLATE`, `PS_SCHEARPLATE`,
   `PS_STEGW`, `PS_LASCHE`, `PS_GROUNDPL`, `PS_VOUTE`, `PS_NOTCH`, `PS_EDIT_CONNECTIONS`.
3. **Groups/Assemblies**: `PS_GROUP`, `PS_FAMILY_CLASS` — break into parts & groups.
4. **Position numbers**: `PS_POS` — number every part for production.
5. **Parts list / BOM**: `PS_CREATE_PARTLIST`.
6. **Shop drawings**: `PS_DETCENTER` (DetailCenter) — the 2D production drawings.
7. **CNC / NC data**: `PS_NC_DATA` — feed the factory machines.

## Modeling conventions
- Units: **millimetres, metric** (profile loaded as Ps191_Metric).
- Steel density 7.9 t/m³. European profiles (HEB, HEA, IPE, UPN...) live in the SHAPES DB.
- Keep agent-test geometry on a dedicated layer so it can be erased cleanly.
- Checkpoint discipline: it's a real production drawing — prefer reversible steps; `U` undoes.

## Learning loop (how the agent gets to "master")
- For any task the agent is unsure about: `grep -i "<topic>" knowledge/manual_fulltext.txt`
  to pull the exact procedure, then act.
- Record reusable command sequences that work into this file over time.
- The 310-page course manual is image-based (needs OCR) — pending; the 1179-page manual
  is the primary text source and is fully searchable here.
