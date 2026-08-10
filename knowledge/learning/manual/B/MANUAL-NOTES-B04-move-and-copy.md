# B.4 Move and Copy Parts — chapter notes

*Read end to end 09/08/2026, pages 107–116 (fulltext lines 2495–2726): B.4.1 Move/Copy ·
B.4.2 Turn · B.4.3 Mirror · B.4.4 Align · **B.4.5 Clone** · **B.4.6 Rotate** · B.4.7 Settings.*

## Why ProSteel has its own move and copy at all

> *"ProSteel objects are treated as AutoCAD elements, and can be copied or moved the same way.
> **This is correct**, but the ProSteel copy and move commands offer… the option of **limiting the
> direction of the move**."*

Two reasons, and both are about the *mouse*, not the geometry:

⚠️ *"Using AutoCAD **object snaps in a view** may result in points being selected that are **not in
the proper plane**. The ProSteel copy and move commands prevent this by limiting the direction of
the move to the current UCS plane or even to one axis."*

⭐ **Group awareness** — *"If several individual components have been assembled into construction
modules or groups, this command can be used to process the **entire group by selecting just one
part**… This will eliminate unnecessary searching and collecting of the parts within a selection
set."* Two buttons on every page: act on **single parts**, or act on **whole groups**.

⇒ **From code, neither reason applies.** The agent hands over an exact vector and never snaps, and
it selects by explicit handle. So B.4.1–B.4.3 are already covered by `copy` / `mirror` / `rotate`.
**What is not free is Align, Clone and the rotated distribution** — those are real algorithms.

## B.4.1 Move/Copy · B.4.2 Turn · B.4.3 Mirror

`Alignment` = **3D** (free) · **2D** (start and end perpendicular to the UCS plane, so movement
stays *in* that plane) · **X-Axis / Y-Axis / Z-Axis** (parallel to that axis only) · **Free**
(*"the elements 'drag' with the crosshairs"*). `Multiple` repeats the move.

**Turn:** `Axis` = **Free Axis** (two clicked points) or ⭐ **Object Axis** — *"one of the (parallel)
coordinate axes of the **element UCS**"*, with the chosen axis *"displayed at the object selected
first **in colour**"*. `Angle` from a preset list or `Free`. **`Turn+Copy`** rotates a copy instead.

**Mirror:** `Method` = **2-Points** (⚠️ *"the mirror plane is then **perpendicular to the current
view**… it is best to use a **perpendicular view** of the component"*) or **3 Points** (a plane in
space, view-independent). **`Mirror+Copy`**.

⇒ ⚠️ The 2-point mirror **depends on the current view**. From code, prefer the 3-point form — it
cannot be wrong for a reason you cannot see.

## ⭐ B.4.4 Align

> *"Specify **two coordinate systems**, which are then aligned **congruently** with one another.
> The calculated required **movement and rotation** are applied to the selected parts."*

`Method` = **3-Point** (origin + a point on X + a point on Y, for each system) or **Surface**
(⚠️ *"you can only make use of **one element**"*; pick an edge next to the wanted face and the
available surfaces light up; *"the point of origin is always regarded as the **bottom point** of the
surface"*). **`Align+Copy`** aligns a copy. The manual's own example: *"the left shape was aligned
on the right shape **as a copy**. It is **moved and rotated at the same time**."*

## ⭐⭐ B.4.5 Clone — transfer manipulations between identical parts

> *"transfer the **manipulations** performed on a component or an entire construction group to other
> components. A prerequisite for cloning is that the parts have a **position number** and that these
> **match**… only parts with the **same position number** as the original part will be considered."*

Five switchable categories: **Cuts** (*"includes the mitred cuts as well"*) · **Drill Holes** ·
**PolyCut** · **Notches** (outlets) · **Boolean**.

The use case is exactly Eretz Barzel's: *"if a hangar has been constructed with **many identical
supports** and holes have to be added later to each support, they may be added to **just one** and
then transferred to all of the others."*

⚠️⚠️ **The trap, stated by the manual itself:** *"the transfer of the manipulations refers to the
**coordinate system of the parts**. If you… would like to transfer a drill hole to a part 100 mm
from the right but its parts coordinate system originates from the **left**, this component will
receive the new boring **100 mm from the left** as well."*
⇒ **A part inserted the other way round gets its holes mirrored, silently.** Cloning onto parts
whose insertion direction was not controlled will produce a wrong model that looks right.

## ⭐ B.4.6 Rotate — the rotated copy with vertical offset

> *"permits a rotated copy with **vertical offset** to distribute e.g. the **steps of a spiral
> staircase**."*

Two methods: `Number` + `Angle` **between** the steps, **or** an `Angle` **area** with the steps
distributed inside it. `Vertical Offset` is the rise per element.
The illustration: *"The construction on the left has been created by **rotating the flat steel
around the central tube**."*

## B.4.7 Settings

`Swap Effect` — inverts what ALT means during selection (normally single part; ALT = group).
`Group only if the main part is selected` — otherwise any part of a group selects the whole group.

---

# MEASURED 09/08/2026

*Band at x = 270000: two spiral staircases and an alignment, built with two new ops.*

## ⭐ The spiral staircase — B.4.6, both methods, exact

New op `spiral` (`DeepCloneObjects` + `Matrix3d.Rotation` × `Matrix3d.Displacement`).
Newel **`RO 114.3x6`** (`DIN RUNDROHR`), treads as 700 × 250 × 10 plates.

| method | asked | measured step angle |
|---|---|---|
| `count` | n=15, angle=**24** between | **24.00°** |
| `area` | n=8, angle=**180** total | ⭐ **22.5°** = 180 / 8 |

The two methods measurably differ, exactly as the manual describes.

Read back through COM, all 16 treads of the first stair:
```
angle°    0.00  24.00  48.00 …  336.00  360.00      (exactly 24° apart)
z          0.0  190.0  380.0 …  2660.0  2850.0      (exactly 190 rise)
radius   460.0  460.0  460.0 …   460.0   460.0      (constant)
```
A full 360° turn, 2850 rise inside a 3200 newel. 15 copies, 0 failures.

## ⭐ Align — B.4.4's 3-point method, verified by coordinates

New op `align`, one `Matrix3d.AlignCoordinateSystem` — the same call the UCS op uses.
Target system: origin (270000, 15000, **500**), X axis at **45°**.

```
source  270000,12000,0    -> 272000,12000,0        UNCHANGED  (Align+Copy)
copy    270000,15000,500  -> 271414.114,16414.314,500
exact 2000 mm at 45°:        271414.214,16414.214,500
```
⇒ **Agreement to 0.14 mm, and the residual is exactly PERPENDICULAR to the axis** — so the angle
and the origin are right; the offset is at the level of the section's own reference-point
convention, far below any fabrication tolerance.
⇒ The manual's *"moved and rotated at the same time"* is literal: one matrix does both.

⚠️ **Orthonormalise before building the matrix.** X and Y as picked are not guaranteed
perpendicular; feeding them raw **shears** the part instead of moving it. The op recomputes
`Z = X × Y` then `Y = Z × X`, and refuses if X and Y are parallel.

## ⛔ B.4.5 Clone — the transfer is dialog-only (established 06/08, still true)

`PsDrillObject.TakeoverDrills(PsSelection, PsSelection)` is **the only manipulation transfer in the
whole API**, and it **transfers nothing from code**. Five call sequences were tried with selections
proven correct (`srcSel=1 tgtSel=3`, `Find()` true for both):
configure-only · configure + `Apply()` · without `SetToDefaults` · per-target subject · selections
built from AutoCAD's own pick set. All five: **changed = 0**. There is no `PS_CLONE` command token
in the manual either.

✅ **The working route is composition, not the API call:** read the source's holes, then drill them
again on each target (`clonedrills variant=9`). The position-number gate is implemented as the
manual specifies — `clonedrills posnum=1` reads `PsObjectProperties.Posnum` from the source and
selects only parts carrying the same number, refusing outright if the source has none.

⚠️ **Still missing:** the other four categories — Cuts, PolyCut, Notches, Boolean. The same
composition would work (read the modification, re-apply it), and B.12 supplied every one of the
re-application calls.

---

## 🛠 A tooling bug this chapter exposed

`modal_dialogs()` — the guard that stops the agent running while a dialog waits — enumerated
**every `#32770` window on the desktop**, with no check of which process owned it. Its own docstring
said *"every top-level dialog **owned by AutoCAD**"*; the code never tested ownership.

Caught live: the guard reported *"Sheet Information"*, then *"Open"*, while AutoCAD itself was
verifiably clean — they were **Amir's own windows, in another application**. Any Open or Save-As box
he touched froze the agent completely. Amir: *"אתה רק על האוטוקאד אל תתערב"*.

Fixed by matching the dialog's process id against AutoCAD's (found from its main window title, so
it never depends on an executable name). ⚠️ **This does not relax the guard** — a real AutoCAD or
ProSteel dialog still blocks exactly as before, and if the pid cannot be determined it falls back to
the old desktop-wide scan, because blocking wrongly is safer than running into a dialog.
