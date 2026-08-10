# B.9 Insert Plates — chapter notes

*Read end to end 07/08/2026, pages 174–188 (fulltext lines 4153–4530): B.9.1 Flat Straight Plates ·
B.9.2 Flat Bent Plates · B.9.3 Gratings · B.9.4 Bent Plates · B.9.5 Additional Settings.
Nothing below has been measured against a live model yet — this is the reading.*

> *"you can insert plates of any shape into the model — in the program, such plates are called
> **poly-plates**… The other plate types commonly used for steel structures such as **base plates,
> end plates or stiffeners** can be created by the program using **automatic functions. The program
> also names them.**"*

That sentence draws the boundary of the chapter. B.9 is the **free-contour** plate: gussets,
connecting plates, butt straps. Base plates, end plates and stiffeners belong to B.16/B.17/B.18,
which name and classify them for you. Reaching for B.9 to make a base plate throws that away.

⚠️ The manual's own definition includes *"flat **or three-dimensionally bent** plates of equal
thickness"* — so bending is inside the poly-plate definition, not a separate object type. B.9.4 is
**155 of the chapter's 378 lines**.

## The field table (B.9.1)

No units and no defaults are stated anywhere in B.9.

| field | meaning |
|---|---|
| `Length` / `Width` | *"at rectangular insertion"* only |
| `Thickness` | ⚠️ **a list, not a free number** — from `..\Prg\pro_st3d.ptt`. Two documented escapes: edit that file, **or** *"switch on overwriting with any values in the 'Global Settings/Plates'"* |
| `Insertion Height` | *"the height of the plate **above the current UCS**"* — not a plate dimension |
| `X-Offset` / `Y-Offset` | *"related to the **selected insertion position**"* — typed, or picked on screen |
| `Item No.` | a direct item number |
| **`Grid`** | a grid on the upper side, *"to show that it **isn't a plate** but a component part such as e.g. gridirons"*. ⚠️ *"In the settings/plate, you can enter a **reduction of weight in percent** for this case"* — a display flag that moves a fabrication weight |
| `Insertion Plane` | which UCS. **`Object-ECS` only when entering by a selected contour**; **`Selected Areas` only for 'Rectangular Plate' + 'Insertion Point'** (UCS defined by indicating two lines); otherwise the current UCS |
| `Insert Edge` | *"the **vertical position** of the plate related to the current UCS **or** ECS, depending on the selected input form"* — the values are never listed |
| `Label` | from `..\Prg\pro_st3d.pdc`; the file *"can define a weight as well… indicated in plain text"*. ⚠️ *"**After selection of a name, the material is directly set as well.**"* |
| `Material` | *"List of all available materials"* — an index, not a string |
| `Part Family` | *"can influence the **colour**"* |
| `Description` | *"can influence the **colour and the layer**"* |
| `Detail Style` · `Display Class` · `Area Class` · `Layer` | as named |

## The seven insertion methods

| # | behaviour |
|---|---|
| 1 | **Free polygon** — picked directly, *"No construction lines or similar things are necessary."* ⚠️ *"Take care that **no crossings** are generated… Then, **plate creation will not be possible**."* |
| 2 | **From an existing contour** — *"a poly-plate, a **circle** or an **arc which is not closed**"*; built on the current UCS **or** the contour ECS |
| 3 | **Rectangular at an insertion point** — ⚠️ *"The form of **polygonal** plates can be modified as you like whereas **rectangular plates always remain rectangular** unless you change this status using Change Properties"* |
| 4 | **Along a line** — *"the **length of the line determines the plate length**"*; width and thickness from the dialog; then position, rotation and insertion point, *"rotated **around the insertion line**"* |
| 5 | **Four points** — *"These points **don't have to be situated in the current UCS**. The **first three** selected points specify the plane. The order is: bottom left; bottom right, top left, top right."* |
| 6 | **A flat → a poly-plate** — ⚠️ *"**All processing actions will be adopted.**"* Holes and cuts survive the conversion |
| 7 | **By diagonal** — x/y alignment from the current UCS |

Rotation cluster (method 4): `+Phi` · `-Phi` · `+90` · `-90` · `Rotation` (*"the value used for
rotation in the **first two** options"* — it does **not** feed ±90), and `=0`, *"return to the
original position of your plate after rotation"* (printed under B.9.2, belongs here).

⚠️ **`CL`** — *"After insertion, the plates are **still connected with the dialog**. Further
modifications are still possible. Use this button to **interrupt this connection**."* Until it is
pressed, later dialog edits retro-actively change plates you thought were finished. Same live-link
behaviour as B.8's `INTERRUPT DIALOG`.

## ⭐ Where a plate's reported Length and Width come from

> 1) *"Are there any **parallel edges** and is their **distance sufficient**?"*
> 2) *"Is there any **rectangular corner**?"*
> 3) *"**Search for the longest side**"*

*"Depending on the geometry, the found direction is recognized as **length direction**."*

⇒ For any non-rectangular plate the numbers on the parts list come from **this heuristic, not from
the geometry**. *"Sufficient"* is never quantified. `PLATE DIMENSIONS` overrides it manually —
*"First select the plate and then the direction"* — and the override *"can be cancelled in the
plate properties"*.

## B.9.2 Flat bent plates

Two options only: a bent plate **from an arc**, and a bent plate **from three points** —
⭐ *"Keep the **ALT-key** pressed at input and you can enter an arc with **> 180°**."*

## B.9.3 Gratings

> *"In principle, you can call any plate a grating… Another problem is to determine the **weight for
> the parts list** which traditionally can only be calculated in an **insufficient** way."*

⇒ The point of the grating feature is **weight honesty**, not geometry. ProSteel keeps gratings
(*"resp. tear plates, fence elements, etc"*) in **its own database files indicating the exact weight
per piece**.

⚠️ *"The dimensions are determined by the database and **cannot be modified later**."*

Two insert buttons: at a picked point, or ⭐ **distributed automatically within a boundary** —
*"you first have to click on the border (polyline) and then specify the alignment and the origin of
distribution"*, cross-referenced to *Roof and Wall Covering*.

⇒ **Two routes to a grating, and they are not equivalent:** the `Grid` flag on an ordinary plate
(a display mark + a percentage weight reduction) versus a real catalogue grating (exact weight per
piece). The first is a drawing convention; the second is a purchasing fact.

## ⭐⭐ B.9.4 Bent plates — segments are a TREE

> *"These represent a **single 3D-component part** which can be depicted in 2D as flat plate with the
> corresponding bending edges via **unfolding**."*

**A bent plate begins life as a flat one.** *"To generate a bent plate, you **first have to have
inserted a flat plate**. This plate **determines the alignment** of the bent plate. Now, click on
the BEND button."*

| field | meaning |
|---|---|
| `Flange Length` | *"the length of the segment **in the direction of the bending angle**"* |
| `Front Distance Edge` | *"the front offset **in the direction towards the reference edge**. **Positive values mean that inwards the segment becomes smaller.**"* |
| `Rear Distance Edge` | the same at the rear |
| **`Bending Radius`** | ⚠️ *"related to the **neutral fibre (half of the plate thickness)**. To avoid problems with the volume modeller, the radius should always be **a little bit more than half of the plate thickness**."* |
| `Bending Angle` | *"related to the **reference segment (reference edge)**"* — not to the world |

### ⭐ `Correction Value Unwinding` — K

> *"The unwinding is carried out using a correction factor. **The correction is multiplied with half
> of the plate thickness.**
> K = 0 – unwinding on the **inner** radius · K = 1 – on the **center** · K = 2 – on the **outer**
> radius. This correction factor is used for the **whole** bent plate."*

⇒ K is a **datum selector expressed as a number**, and it is per-plate, not per-bend. K = 1 is the
mid-thickness case that matches the `Bending Radius` datum.

`Length Calculation` — how an added butt strap's length is measured, *"up to the end of the butt
strap"*: from the **end of the bending segment** (`flange length`) · from the **outside edge** of the
base plate · from the **inside edge** · from the **center line**.

### Combine Plates to Bent Plates

Build a bent plate out of two existing plates. ⚠️ *"Prerequisite for a correct connection… is that
you can generate a **tangential transition** between the two plates."*
`Radius` · `Inside Radius` (*"whether the radius has to be used on the **inside or outside**"*) ·
`Delete` — ⚠️ not erasure: *"The existing plate is **deleted by taking it over into** the bent
plate."* Absorption.

### ⭐ Dependence of segments

The manual's worked example: segment **2a** added to base plate **1** at 45°; segment **3** added to
**2a** at 45° with front and rear distances; segment **2b** added to **1** at 90°.

> *"If you now **delete or modify** the segment (2a), you will also **delete or modify the depending
> segment (3)**."*

⇒ Segments form a **dependency tree rooted in the base plate**, and `ADD SEGMENT` fixes the parent
by *where you click*: *"you have to click on the plate at the desired **reference edge**… This
determines the alignment. The new plate segment is **always subordinate to the reference
segment**."* `REMOVE SEGMENT` takes *"**including all subordinate segments**"*. `MODIFY SEGMENT`
propagates. `CHECK BENT PLATE` validates one.

⚠️ **Important Hint** (the manual's own emphasis): *"Please take care that you **don't modify the
basic polygon** of a plate (e.g. by adding an edge, or similar things) **when you have already added
segments**… The reference edges of the segments would be modified as well and the consequence could
be an **undesired behaviour**."*

⇒ Order matters absolutely: **finish the contour first, then bend.** This is the opposite of the
B.13 plate-editor habit of reshaping a contour whenever convenient.

## B.9.5 Additional settings

`Always ECS` — *"If you want to insert a plate after an **existing contour**… This button has the
effect that you always insert according to the object ECS. **The settings on the first page will be
ignored.**"* Scoped to contour insertion by its own preface.
`File Path` + `Grating Catalogue` — the grating database files.
`Close` — close the dialog after insertion.

---

# What the API actually does — MEASURED 08–09/08/2026

*Built in the teaching drawing at x ≥ 70000: 9 poly-plates, 3 bent plates, 2 arc plates.
Everything below was read back from the model, not inferred.*

## The dialog → API map

| B.9 field / button | call |
|---|---|
| the seven insertion methods | `SetAsRectangularPlate(L,W)` · `AppendEdgePoint` ×n · `SetFromCircle` · `SetFromRectangle` · `SetAsRadialPlate(R)` · `CreateFromShape()` |
| `Insertion Plane` | `SetInsertMatrix(PsMatrix)` |
| the insertion-position grid | `SetXPosition` / `SetYPosition(PositionSelection)` |
| `X-Offset` / `Y-Offset` | `SetXOffset` / `SetYOffset` |
| **`Insert Edge`** | **`SetNormalPosition(VerticalPosition)`** |
| `Insertion Height` | `SetInsertHeight` |
| `Grid` + `SURFACE GRID` | `SetGrid(bool)` + `SetGridDirection(PsVector)` |
| `Label` · `Material` · `Item No.` | `SetName` · `SetMaterial(int)` · `SetArticle(string)` |
| "no crossings **or creation will not be possible**" | ⭐ **`checkValidPlate()` — pre-flightable** |
| `BEND` | `PsCreateBendPlate.SetObjectId` + `Create()` |
| `ADD SEGMENT` | `PsBendPlate.AddFlange(len, front, rear, radius, angle, point, out idx)` |
| `Combine Plates` (Radius · Inside Radius · K · Delete) | `CreateOfTwoPlates(id1, id2, r, pt, useInner, K, deleteSecond)` |
| B.9.2 arc, and the **ALT key** for > 180° | `PsCreateArcPlate` + **`SetBigArc(true)`** |

## ⭐ `Insert Edge` — the values the manual refuses to list

`Bentley.ProStructures.VerticalPosition = kDown, kTop, kMiddle`. Three identical 300×300×**20**
plates, one per value, measured by WCS extents:

| value | z range | where the material goes |
|---|---|---|
| `kDown` | `0 … +20` | entirely **above** the insertion plane |
| `kMiddle` | `−10 … +10` | straddles it — **the default** |
| `kTop` | `−20 … 0` | entirely **below** |

⚠️ **The names are inverted relative to intuition.** `kDown` puts the plate UP. The name says which
**face** lands on the plane, not which way the material goes.

## ⚠️ Four places this API lies, all measured

1. **`PsCreateBendPlate.Create()` returns `false` while succeeding.** It also **ERASES the flat
   plate and creates a new entity.** Every later call on the original handle throws `eWasErased`.
   The replacement id comes back on `cb.ObjectId` — read it, or find it by census diff.
2. **`CreateOfTwoPlates` likewise replaces**: two plates in, one bent plate out, both original
   handles erased. (The manual does say the second is *"deleted by taking it over into"* the bent
   plate — it does not say the first is replaced too.)
3. **`AddFlange` takes DEGREES; `PsBendPlateFlange.Angle` returns RADIANS.** 45 in → 0.785 out.
   Same API, two units.
4. ⭐ **`FlangeCount` counts only TOP-LEVEL segments.** A plate with segments 2a, 2b (on the base)
   and 3 (on 2a) reports **2**. Loop to `FlangeCount` and every subordinate segment is invisible.
   Scan past it; the terminator is `GetGripPoints` throwing `NullReferenceException`.

## ⭐ The dependency tree, confirmed

The manual's own worked example, rebuilt and read back — `get_ParentFlangeIndex` is **indexed**
(the type dump prints it as a plain property; the compiler gives it away):

```
[0] len=150  45°  off=0/0    vtx=1-2   parent = -1     segment 2a, on the base plate
[1] len=150  90°  off=0/0    vtx=3-4   parent = -1     segment 2b, on the base plate
[2] len=100  45°  off=20/20  vtx=1-2   parent =  0     segment 3, ON 2a
```

`StartVertex`/`EndVertex` name **which edge of the base polygon** the segment sits on — that is the
"reference edge" you pick by clicking.

⚠️ **A subordinate segment cannot be placed by clicking in the base plane.** Once the parent is
folded up its reference edge is no longer at z = 0, and `AddFlange` returns **−1**. The click point
must come from `GetFlangeVertexes(i, …)` on the parent. (`GetGripPoints` returns the same point
twice — degenerate; the vertex array is the useful one.)

## ⭐ What B.9.2 "Flat Bent Plate" actually is

An arc plate of `SetWidth(300)` on a 400 radius reads back `Radius=400`, **`NeutralRadius=550`**,
and a bounding box of **700 × 700 × 6** (6 = the thickness).

⇒ `Width` is **radial**, `NeutralRadius = Radius + Width/2`, and the object is a **flat annular
sector** — a plate lying in one plane whose edges are arcs. **It is not a rolled cylinder.**
`SetBigArc(true)` genuinely produces the > 180° case: `StartAngle..EndAngle` came back
`0 .. 4.712` rad = **270°**, against `0 .. 1.571` = 90° without it.

## ⛔ A call that must never be made

**`PsPlate.computeObjectWeigth(bool)` kills AutoCAD** — process gone, no exception, no dialog.
Reproduced twice; the second time with a marker file written immediately before the call, which
survived reading `about to run: weight on 501`. Any read-back routine that touches plate weight
takes the session with it, which is how the first B.9 run lost five plates.

⇒ **The B.9.3 grating weight-reduction claim is therefore UNVERIFIED.** The `Grid` flag is settable
and the flagged plate is otherwise identical to a plain one (same extents, same thickness). Whether
it moves the weight cannot be tested through this call.

## Still open

- The grating **catalogue** (`File Path` / `Grating Catalogue`) — no property for it was found on
  the settings surface; may be unreachable from the API.
- The automatic dimension heuristic — what *"sufficient"* distance means, and whether the chosen
  direction can be read back (`DimensionDirection` / `DimensionAlignement` untested; they sit next
  to the lethal call).
- `CHECK BENT PLATE` — no API counterpart identified.
- Whether `K` is settable anywhere other than `CreateOfTwoPlates`'s `KValue` argument.

---

## AUDIT 10/08/2026 — B.9.3 closed, and a tooling bug found

### ⭐ Plate weight IS readable — the lethal call is not the only route

`PsPlate.computeObjectWeigth` still kills AutoCAD; **do not call it.** But `op=props` reads the
weight through **`PsObjectProperties`** and is safe:

```
props handle=<plate>   ->   wt=117.75   (1000x500x30 = 0.015 m3 x 7850)
```

⇒ The B.9.3 weight claim was testable after all. **A "cannot be verified" verdict is only as
good as the routes that were tried** — the same lesson B.4 taught about preconditions.

### B.9.3 Gratings — measured

| | |
|---|---|
| the setting | `Ks_ComGlobalSettings.PlateRasterWeightReduction`, **shipped at 10 %** (*Raster* = grating) |
| `grid=1` sticks? | ✅ `DisplayFlagsLong` 16436 → **24628** (bit **8192**) and `PitchLineMode` → **True** |
| weight moves? | ❌ **no** — 117.75 kg at 0 %, 10 % and 35 % alike |

⇒ **The object carries the gross weight.** The reduction is a reporting-time transform; the
manual puts it in the parts list, and that half is untested.

⚠️ `PsCreatePlate` has **no grating-database selector** — `Data/Plates/ImpGrating.mdb` and
`Platten-Bleche-Roste.mdb` are on disk and both insert buttons are pick-based. Catalogue gratings
stay dialog-only; the `Grid` flag is the code route.
