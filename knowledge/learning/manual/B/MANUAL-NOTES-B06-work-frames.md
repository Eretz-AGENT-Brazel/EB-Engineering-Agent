# B.6 Work Frames — chapter notes

*Read end to end 07/08/2026, pages 134–152 (including B.7 Choose View, which is the frame's
other half): B.6.1 Rectangular · B.6.2 Cylindrical · B.6.3 Wedge · B.6.4 Pyramidal ·
B.6.5 Create Views · B.6.6 Axes Names · B.6.7 Additional Axes · B.6.8 Options ·
B.6.9 User-defined Blocks.*

> *"**Any ProSteel model generation is started with the creation of one or several work
> frames.**"*

That sentence reorders the whole workflow. The frame is not scaffolding added for looks — it is
step one, and everything downstream (B.8.7's automatic column and girder insertion, the views a
modeller navigates by, the axis names that end up on the drawings) hangs off it.

A work frame does **two** things:
1. displays the system dimensions — the axis grid — as design-aid objects
2. **automatically creates the UCS systems of the views it defines.** *"A simple click of the
   mouse will change the view."*

## Four basic types

**Rectangular** · **Cylindrical** (also conical — separate base and top radii) ·
**Wedge-shaped** · **Pyramidal**. Everything below is the rectangular dialog; the others add a
few fields and share the rest.

| | |
|---|---|
| `Length` / `Width` / `Height` | either **overall + a number of regular fields**, or **each field individually** |
| `Absolute` | the height list holds **absolute** heights instead of per-field ones |
| `Axis Descriptions` | labels, movable afterwards by grips |
| `Insert Position` | where the frame sits relative to the insertion point |
| `Roof Angle` · `Centre Height` · `Ridge Width` | a gabled-roof frame. **Ridge width 0 or = the frame width ⇒ only a roof surface** |
| cylindrical | `Base` radius · `Top Radius` · `Height` · **`Segmentation`** (circle segments) |
| wedge | `At left` — which way the apex leans |
| pyramidal | `Roof Length` · `Ridge Width` |

**⭐ Asymmetrical divisions:** hold **ALT** while activating one of the three list values and type
the fields **comma-separated, or `number*distance`**. That is the *same syntax as a drill field
and as the girder distribution* — one grammar across the whole program.
**CTRL** while activating **deletes** the whole definition for that dimension.
⚠️ *"Existing field definitions cannot be modified using this method. They will be **completely
overwritten**."*

**Insertion:** pick the origin (right-click = `0,0,0`), then give the frame's **X-axis**
(right-click = the current UCS x-axis).

## Several frames in one model — group names

Frames are told apart by a **group name**, which becomes a **prefix on every view name**:
`hangar_TOP`, `platform_TOP`. Selected later with **`PS_SETUCS`**.
The manual's example: a `hangar` frame L=15000 B=8000 H=5000 with an asymmetrical length
division, X-axis = axis 1, Y-axis = axis A, origin at A/1 and H=0; then a `platform` frame
3000×3000×1500 at x=5000. Overlapping views of the same work plane can be omitted from the
subordinate frame — the main frame already provides them.

## ⭐ Work frames are OBJECTS

*"One of the great strengths of ProSteel is its object-orientation; and work frames are good
examples of this!"* — move or rotate a frame, or even a single work plane, with ordinary AutoCAD
commands and **the UCS settings follow**. Cut planes can be set per work plane afterwards through
*Change PS Properties*.

⚠️ **"The frame layer has to be UNLOCKED for any changes to take effect!"** — and `Lock Layer` is
an option on the frame itself (`PsCreateGrid.LockLayer`). A locked frame silently refuses edits.

## B.6.5 Create Views — why the frame is the navigation

Views on all surfaces at once: `Front` · `Side R` · `Side L` · `Back` · `Top` · `Underside` ·
`Roof R` · `Roof L`, **plus one view per axis**:

- **`Length Axis`** — a view for each axis into the depth
- **`Width Axis`** — one per axis into the width
- **`Height Axis`** — one per axis into the height

`Use Axis Descriptions` picks standardised names (`X_1`, `X_2`) or the real axis names;
`Height with Coordinates` appends the height; `Group Name` prefixes them all (`R1_X2`).

⭐ **`Distances Cut.Surfaces`** — each view **automatically hides everything beyond a given
distance**, front and back. That is how a modeller sees one bay at a time in a 2,000-object model.

**B.7 Choose View** is the other half: selecting a view puts the UCS in that work plane, looks at
it square on, **and activates its cut planes so only the objects in that slice are visible**.

## B.6.6 Axes names

`123` or `ABC` · text / circle / **block** border · connection line · start value · size · scale ·
distance · **`Main Axis`** (the parent frame's name when this one is subordinate; also usable as a
prefix) · `Suppress First/Last Axis` (they overlap where frames adjoin) ·
⭐ **`Avoid I, O`** — skip those letters in alphanumeric names to prevent confusion with 1 and 0 ·
`Decreasing` · `Position` (front/rear/left/right) · `2 Lines` · ⭐ **`Dynamic`** — names re-orient
themselves to the view direction so they stay readable · `Axis Gap` (2D details only).
**`EDIT`** gives per-axis control: name, main axis, a manual-override flag, and **invisible**.

## ⭐ B.6.7 Additional axes — build the grid from the architect's plan

Add a grid axis by **clicking an existing line**; numbering adapts. Add several at once; remove a
user axis the same way.

> *"This function helps you to create an axis grid **completely out of an existing 2D-axis
> plan**. First, you insert a temporary grid into the plan and then you align it to a reference
> point as desired. Afterwards, you add the grid axes by clicking on the existing 2D-axes."*

⇒ In the API this is **`PsGrid.addUserXaxis(Start, End)` / `addUserYaxis`** — and their existence
means `PsGrid` *is* meant to be driven; the open problem from B.8.7 is only how to **bind** it to
a frame that already exists.

## B.6.8 Options

`No Lines` · **`Axis on Edges`** (names ride the outer edge of the current view rather than a
fixed offset — *"it is helpful to switch on the 'Dynamic' option"*, and it needs a zoom or
Regenerate to refresh) · `Show Axis Lines` · `Height Grid Lines` · `3D Pattern` ·
**`Within`** — the grid is drawn inside the frame too, *"this enables snapping the endpoints of
the grid on the planes"* ⇒ that is what makes the grid usable as a drawing aid · `Roof` ·
**`Lock Layer`** · `Segmentation` · text style, line type, text scale, colours.

## B.6.9 User-defined blocks

Own blocks instead of circles/rectangles around the axis names, carrying **attributes that are
replaced by the actual axis names**. `From File` (blocks already in this drawing) · `DWG Blocks`
(loaded from outside) · `Block Path` · `Block Name X` / `Block Name Y`.

---

# B.6 in the API — measured 07/08/2026

Implemented in the model band at **x ≥ 40000** (to the right of B.8 in TOP view), drawing
`B08-insert-shapes.dwg`. Plugin `EBAgentApi94` / `EB_RUN94`, op **`frame`**.

## ⭐ The shape switch — `SetType(GridType)`

`PsCreateGrid.SetType(GridType)` where

```
Bentley.ProStructures.GridType = kRectangle | kCylinder | kWedge | kPyramid
```

**These are the manual's four frame types (B.6.1–B.6.4).** Without this call every roof and
radius value is *stored on the entity and never drawn*: the first cone came out with a bounding
box of 1721 mm — pure axis-text overhang — while `BottomRadius` read back a perfect 8000.
With `SetType(kCylinder)` the same parameters produce a 16000 × 16000 × 6000 frame.

⚠️ **A second, unrelated enum in another assembly is also called `GridType`**
(`aSa.PC.Shape.Graphics.GridType` = `CrossLines, Points, None`). Reflecting by bare name found
that one and led to the wrong conclusion — "GridType is a display mode" — which cost a whole
build cycle. *Resolve enums by full type name, and set them by member name, never by ordinal.*
(Measured ordinals here happen to be `kRectangle=0, kCylinder=1, kWedge=2, kPyramid=3`.)

## ⭐ Reading an EXISTING frame — the .NET path does not work, COM does

This was carried over unresolved from B.8.7. Resolved:

```python
o = doc.HandleToObject(handle)      # -> Ks_Grid, a real COM entity
o.Name, o.Length, o.Width, o.Height
o.LengthSteps, o.WidthSteps, o.HeightSteps      # 64-long arrays; first <Division> are real
o.GetEffectiveCoordSystem()                      # origin + X/Y/Z axes
o.GetBoundingBox()
```

`PsGrid.readProps(PsObjectProperties)` **cannot be made to work.** Evidence, not guesswork:
`PsObjectProperties.readFrom(id)` returns 0 **and `getObjectId()` returns the id I asked for** —
so it binds to the right object — yet `Name` stays empty and `readProps` leaves the grid at
`L=0 W=0 div=2x2`. Tried `readProps`, `writeProps`, and `init()` before `readFrom`. All three
bind nothing. **`PsGrid` is a creation/edit buffer, not a reader.**

⇒ Whole parallel API worth knowing: **`PSCOMWRAPPERLib`** (`Ks_ComGrid`, `Ks_ComWorkFrame`,
`Ks_ComCreateGrid`, …). It binds to existing entities, and it **writes** too — `LeftWedge` and
`VerticalWedge` (B.6.3 "At left") exist *only* as entity properties; `PsCreateGrid` has no
setter for them.

COM property names differ from the .NET ones — check both lists:
`Wide`→**`Width`**, `LengthDiv`→**`LengthDivision`**, `RoofWide`→**`RoofWidth`**,
`TextXPos`→**`TextXPosition`**, `TextStyle`→**`TextStyleName`**.

### Reading the real axis grid

```python
def axes(steps, div):                 # local coordinates of the axis lines
    c, out = 0.0, [0.0]
    for s in list(steps)[:div]:
        c += s; out.append(c)
    return out
```
Measured on `B6_RECT`: `LengthSteps → [6000, 7500, 6000]` — exactly what was supplied, read back
out of the entity. X axes `[0, 6000, 13500, 19500]`, Y `[0, 5000, 10000]`, 12 joints.
**No more computing joints from what I myself passed in.**

⚠️ **Orientation:** with `SetXYPlane(1,0,0 / 0,1,0)` the frame's **`Width` runs along WCS X and
`Length` along WCS Y** (measured from the bounding box, not assumed). Skipping `SetXYPlane`
altogether leaves the frame on whatever UCS is current — the first frame silently came out
rotated.

## ⭐ `GetBoundingBox` does not include the roof

Every gabled/wedge/pyramid frame reports `zTop = Height`, ignoring the ridge. Setting
`RoofAngle`, `RoofMiddle`, `RoofHeight`, `RoofWidth` in every combination never moves it.
The roof is nevertheless real — **the proof is in the work planes**:

```
B6_GABLE_TOP      tilt  0.0°
B6_GABLE_ROOF_L   tilt 15.0°     <- exactly the RoofAngle set
B6_GABLE_ROOF_R   tilt  3.4°     <- the other slope, driven by Centre Height
```

The two slopes differ because `Roof Angle` and `Centre Height` are independent dialog fields —
which is precisely why the dialog carries both. `B6_PYRAMID_TOP` inserts at the *inset, centred*
top face, confirming the pyramid.

⇒ **To verify frame geometry, measure the work planes, not the bounding box.**

## Group name → view names, confirmed

The frame creates one `Ks_WorkFrame` **per surface**, named `<group>_<view>`:
`B6_RECT_FRONT/BACK/TOP/BOTTOM/SIDE_R/SIDE_L`, plus `ROOF_R`/`ROOF_L` where a roof exists.
Exactly the manual's "group name prefixes them all".

⚠️ **Deleting a `Ks_Grid` orphans its `Ks_WorkFrame` planes** — 4 grids deleted left 30 planes
behind. Clean up by name prefix.

## Reachable, and used

`SetXYPlane` · `SetAllLengthSteps/Width/Height(Double[])` (one array call, no loop) ·
`SetLength/Width/HeightDivision` + `Set*Steps(i,v)` · `SetLowerRadius`/`SetUpperRadius`/
`SetRadiusDivision`/`SetSegments`(flag)/`SetRadiusView` · `SetRoofAngle/Middle/Width/Height/Length` ·
`DisplayAxisNames` · `LockLayer` · `SetDisplay3d` · `BuildFrames` · `SetFrontClip`/`SetBackClip` ·
`SetAllViews` + the eleven individual view flags ·
**`SetLeftTextSettings`/`SetRightTextSettings(Size, Scale, Distance, Type, Display, Order,
Position, Start, DoubleLine, Dynamic, First, Last)`** — the whole B.6.6 dialog in one call, one
side each, so the X run can be numeric and the Y run alphabetic.

## Not reachable / not true

- ⚠️ **`checkExistingGrids(name)` returned `True` for four brand-new, unused names.** It is not
  the collision test its name suggests. Do not gate on it.
- **`SetXViews`/`SetYViews`/`SetZViews` did not produce a view per axis** — `B6_RECT` got the six
  surface views and a single `Y_1`, not the full `X_1…X_4` / `Z_1…Z_3` set the manual describes.
- **B.6.7 Additional Axes — CLOSED 10/08/2026, see the audit section below.**
  `PsGrid.addUserXaxis(Start,End)` / `addUserYaxis` exist, but they live on `PsGrid`, which cannot
  bind to an existing frame, and `Ks_ComGrid` has no equivalent. ⛔ The route left untried here —
  `PsGrid.insert(Origin, Xaxis, Yaxis)` as an *alternative creator* — **was tried and it fails**:
  `addedX=0 addedY=0`, census unchanged.
- B.6.9 user-defined blocks: `UserBlockNameX/Y`, `UserBlockPath`, `UserBlockXScale/YScale` are
  all writable over COM. Not exercised.

---

# B.6 audit — measured 10/08/2026

*Part-B chapter-by-chapter audit (B.1 → B.29), record:
`knowledge/learning/audits/AUDIT-PART-B-2026-08-10.md`.
Verdict: **B.6 was assessed as the best-learned chapter in part B** — the standard the others were
measured against. The band (x 32 000 – 52 000, 41 `Ks_WorkFrame`) was **not modified: nothing in
it was wrong.** The only work was closing the one route the notes had left untried.*

## ⛔ B.6.7 — the untried route, tried. It fails.

New op **`gridaxes`**: build a fresh `PsGrid`, set `Length` and `Wide`, add the user axes, then
`insert()`.

```
addedX=0  addedY=0  readBackX=0  readBackY=0   census 836 -> 836
```

`addUserXaxis` returns **false** on an un-inserted grid, and `insert()` creates nothing.

Three claims were **re-verified rather than assumed** — B.4's retraction the same morning showed
what assuming costs:

| claim | re-checked |
|---|---|
| *"`IKs_ComGrid` has no user-axis equivalent"* | ✅ **true** — no `AddUserXaxis`, no `GetUserXaxis` |
| *"`PsGrid` cannot bind to an existing frame"* | 🛑 **RETRACTED — see below** |
| the untried `insert()` route | ❌ **fails** |

> ### 🛑🛑 RETRACTED 10/08/2026 during the B.23 audit — the binder EXISTS
>
> This paragraph used to read: *"`PsGrid` has the user axes and has **neither a creator nor a
> binder** — no `SetObjectId`, no `readFrom`, **no binder of any kind**. The two halves never meet
> in the API."* It went onto THE CEILING in that form.
>
> **`Bentley.ProStructures.Drawing.PsTransaction.GetObject(Int64, PsOpenMode, PsGrid&)` binds a
> live `PsGrid` to an existing `Ks_Grid`.** Measured on grid `2F1`:
>
> ```
> bind handle=2F1 cls=grid -> grid=True [name='A' len=24000 wide=15000 type=kRectangle
>                                        lenDiv=4 wideDiv=3 userX=0 userY=0 xDesc=3 yDesc=4]
> ```
>
> ⭐⭐ **The binder is not on the class. It is on the transaction** — and `GetObject` has **57
> overloads** covering nearly every ProSteel type. Every chapter that asked *"can this class bind?"*
> asked the class, and the class was the wrong place to ask.
>
> ⇒ ⛔ **B.6.7 stays closed — but for a completely different and much better reason.** With the
> grid bound, `addUserXaxis` was called on it. **It killed AutoCAD.** Isolated to that single call
> on a freshly saved model and it killed it again. It is now the third entry in
> `LETHAL-CALLS-do-not-invoke.md`.
>
> ⇒ So: **the two halves DO meet, and the meeting point is lethal.** *"Dialog-only"* remains the
> practical answer; *"they never meet"* was false.

⭐ `gridaxes` is **kept, because it IS the evidence** — and it reads **every axis back** instead of
trusting `addUserXaxis`' boolean.

## Still open, honestly

- `SetXViews` / `SetYViews` / `SetZViews` **do not produce a view per axis** — `B6_RECT` got the
  six surface views and a single `Y_1`. Still unexplained; low value, since the surface views are
  the ones actually used.
- **B.6.9 user-defined blocks** — writable over COM, **never exercised**. Drawing presentation,
  not modelling.
