---
name: tankforge-picking-overlays
description: TankForge — floating panels over the 3D viewport block click-picking; make them click-through except controls
metadata: 
  node_type: memory
  type: project
  originSessionId: f91b529d-dc89-40b6-9887-8daf2489a303
---

In the TankForge / EB-VESSELS app (`C:\Users\User\Desktop\TankForge\app`), the #1 recurring bug is **"clicking an element in the 3D view doesn't select it."** The cause is almost always one of two things:

1. **A floating HTML panel over the `#viewport` canvas intercepts the real click** (the raycast never runs). This bit us with `#float-props` (Properties, docked right) and `#unroll-panel` (developed-plate data, bottom-left). **Fix pattern:** overlay panels over the canvas must be **`pointer-events: none` (click-through) except their interactive controls** (inputs, buttons, drag-bar). Read-only info → fully click-through. Verify with `document.elementFromPoint(x,y)` over the model — it must return the canvas `#c`, not a panel div. Whenever adding a NEW panel that floats over the viewport, apply this immediately.
2. **Occlusion / Three.js raycaster ignores `visible`** — the outer shell hides the inner; clicks resolve to the front (or to a hidden mesh). Fixes already in place: pickAt filters by `_isVisible`, and cycles by LAYER on repeated clicks (outer→inner). Also picking uses **pointer events** (not mouse) because OrbitControls' `preventDefault` suppresses the compat mouse events.

**Why:** Amir hit this repeatedly ("why does this keep happening?", 2026-07-14). See [[vessels-stage1]]. When verifying picking, drive the REAL `pointerdown`/`pointerup` path (synthetic dispatch straight to the canvas bypasses overlays and hides the bug), and check `elementFromPoint`.
