# EB API v1.0 — Acceptance Report

**Date:** 2026-07-12 · **Executor:** Opus 4.8 · **Result: ✅ PASS**

## The test (Amir's spec) — run live in a fresh drawing, timed
| # | Command | Result | Time |
|---|---|---|---|
| 1 | Switch to **TOP** view | ✅ `view=top` | 0.1s |
| 2a | Model **HEB 500** beam #1, 6 m | ✅ `Ks_Shape` HE500B/DIN_HEB | 0.2s |
| 2b | Model **HEB 500** beam #2, 3000 mm apart | ✅ `Ks_Shape` | 0.1s |
| 3a | Diagonal **HEA 500** between them | ✅ `Ks_Shape` HE500A/DIN_HEA | 0.1s |
| 3b | **Miter-cut** the diagonal at the main | ✅ `miter applied=1` | 0.1s |
| 4 | **Bolted connection** (2 plates t20 + 4×M20 bolts) | ✅ 2×`Ks_Plate` + 4×`Ks_Bolt` | 0.2s |
| 5 | ISO view | ✅ | 0.1s |

**Worst single step: 0.2 s — the 10-second SLA passes with ~50× margin.**
Every object is a **native ProSteel entity** (Ks_Shape / Ks_Plate / Ks_Bolt), created
programmatically with **no dialogs**. Verified visually (ProSteel ribbon + 3D model).

## What "EB API v1.0" delivers (all working, verified)
- **Modeling:** `beam` (any profile), `plate`, `bolt`, `boltfield`, `miter` cut, `workframe` grid.
- **Composite:** `conn_bolted` (full end-plate bolted connection macro).
- **Profiles from speech:** HEA/HEB/HEM, IPE, UPN, RHS, SHS resolved to real DB keys
  (e.g. "HEB 500"→HE500B/DIN_HEB, "RHS 100x150x4"→RR150x100x4). 778-entry section map + rules.
- **Views/nav:** top/front/left/right/iso/bottom, zoom.
- **Structure:** copy (native), delete, undo mark/back.
- **Production (dialog-assisted):** position numbers, parts list, DetailCenter, NC data.
- **Read-back:** `list_model` + per-project `model_log.jsonl`.
- **Speed & safety:** ThreadingHTTPServer + IsQuiescent guard → never freezes; instant `EB_BUSY` when AutoCAD has a dialog open.
- **Console fast-lane:** a modeling sentence with a profile + 2 points models a native beam directly in the console, no brain round-trip.
- **Cold start:** `eb_api.bootstrap()` launches AutoCAD+ProSteel, loads the plugin, verifies — unattended.

## Architecture
`console.py` (UI + fast-lane) → `eb_api.py` (client + profile resolver + model log) →
`plugin/EBAgentApi6.dll` (NETLOADed C#, native ProSteel object model via ProStructuresNet.dll).

## Honest residuals
- Production ops (position/partlist/detail/NC) are wired via ProSteel's own commands
  (dialog-assisted); full headless automation of these is the documented next step.
- `conn_bolted` uses a parametric end-plate geometry; snapping precisely to arbitrary member
  faces (auto-interface detection) is a future enhancement.
- CHS/pipe + non-DIN catalogs: resolver covers DIN families + RHS/SHS; extend the map per family as needed (one `dumpcat` each).
