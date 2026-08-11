# E.2 Structural Element — Winding Stair

*Read end to end 11/08/2026, pages 1052–1065 (fulltext lines 27341–27596). Band:
`E.02-WINDING-STAIR` grid at x = 180 000. Plugin v176 → v177.*

> *"This function creates a winding stair as a **spiral stair** or with **outer and inner strings**
> including a banister rail. All you need to do is click on the **Center** of the spiral stair and
> on a point on the **Starting line** (footfall edge)."*

## ⚠️⚠️ THE MANUAL'S OWN WARNING — and for a fabricator it outranks everything else here

> *"Since a spiral staircase including banisters is a complex construction, the use of structural
> elements is currently to be seen in the fields of **OVERVIEWS AND VISUALIZATION**.
> **ALL OF THE CONSTRUCTION INFORMATION REQUIRED HAS NOT YET BEEN TAKEN INTO CONSIDERATION.**"*

⇒ ⭐⭐⭐ **Bentley is saying this element is not fabrication-ready.** ארץ ברזל **מייצרת** — so a
spiral stair produced by this command is a **picture**, not a shop drawing, and must not be sent to
the floor without being detailed properly. **That sentence is the chapter's most valuable line and
it has nothing to do with the API.**

---

## The nine dialog sections

### 1 · Dimensions
| field | meaning |
|---|---|
| ⭐ `Design` | **`Central struts`** — a central strut + an outer string · **`Strings on both sides`** — an outer and an inner string |
| `Direction of rotation` | ⚠️ *"A **clockwise spiral climbs in an anticlockwise direction**"* — the naming is the opposite of the climb |
| `Outer radius` / `Inner radius` | from the centre of the spiral |
| `Gradient` | *"the distance from beginning to end **reached at an angle of rotation of 360°**"* — i.e. **rise per full turn**, not per step |
| `Total angle` | ⭐ *"Should you require **2 full turns**, input **720**"* |
| `Step angles` | the horizontal angle per step, *"referring to the respective distance from the front edge"* |
| `Step angle overhang` | *"an enlargement or decrease in the stair size… **whereby the front edge remains constant**"* |

### 2 · Dimensioning — central struts
`Lower`/`Upper overhang` (lengthen or shorten the strut) · `Create foot plate` (*"the strut is
**not** shortened in the process"*) · `As plastic sheet` · `Plate thickness`/`width`/`height` ·
`Hole diameter` — ⭐ *"if you input **0**, there are **no holes** made in the foot plate"* ·
`Hole distance width` / `height`.

### 3 · Profiles
`Assembly` (which profile group the settings apply to) · `Profile type`/`class`/`size` ·
`Vertical` (Middle / Lower edge / Upper edge) · `Horizontal` (Middle / Left / Right) · `Angle` ·
⭐ `String profile same` (the inner string follows the outer) ·
⭐ `Insert profiles` — *"the profiles defined are **actually inserted**. So you can **abandon the
outer string** in a spiral staircase, for example."*

### 4 · Steps
`Create steps` — **Standard** or **Block** steps · ⭐ `Grouping` = **No Group / Subgroup /
Assembly** (B.28's hierarchy, chosen right here at creation) · `Height difference` (the footfall
height of the first step from ground level).

### 5 · Steps — central struts *(when `Central struts` is chosen)*
`Plate thickness` · `Bending radius` (*"the inner radius of the fold"*) · `Front`/`Back edge
lengths` (the straight piece of the inner edge) · `Straight outer edge` (*"string of the arch"*,
otherwise adjusted to the spiral radius) · `Open at the top` (which way the fold goes) ·
⭐ `Enable banisters` — *"the steps are **drilled on the outer side** to take the banister posts"*,
with `Number of posts`, `Diameter`, and **`Distance from front edge` and `Spacing` in RADIAN
MEASURE** ⚠️ — angles, not millimetres.

### 6 · Steps — strings on both sides
`Step width` · ⭐ `Angular extension` (*"the outer edge is bigger than the inner edge by this
value, **creating an angular stair**"*) · `b, c, d` — **the same three mounting-hole dimensions as
E.1** · `Hole spacing` · `Hole diameter` · `Oblong hole axis` (the **back** hole is slotted, as in
E.1) · `Indent` · `Bent outer edge`.

### 7 · Banister
`Inner`/`Outer banister` · `Simplified display` (system lines only) · `Banister connection` =
**Automatic / Perpendicular / Laterally / Individual** · `Side offset` · `Height offset` ·
`Side spacing` (>0 inserts a vertical connecting sheet) · `Banister template`.
⭐ *"For stairs with banisters **the latter is created as an individual structural element**"* —
the same split as E.1, and the reason E.3 stands on its own.

### 8 · Screws
`Drill strings for steps` · `Screw string/Step` · `Drill strings for banister` ·
`Screw strings/banister`, each with its own screw type.

### 9 · Options
`Data path` — *"the complete path to the **block files**, if you wish to use stairs from user
blocks."*

---

## The API — `PsCircularStairs`

`CircularStairsLayout` = **`kSinglePost`** (central struts) · **`kDoubleStringer`** (strings both
sides) — the `Design` field, measured from the enum, not inferred.

| dialog | API |
|---|---|
| `Design` | `setLayout(CircularStairsLayout)` |
| `Outer`/`Inner radius` | `setOuterRadius` · `setInnerRadius` |
| `Gradient` | **`setRising`** |
| `Total angle` | `setAngle` |
| `Step angles` / `overhang` | `setStepAngle` · `setStepAngleOverlap` |
| the three profile groups | `setCentralPostShape{Status,Class,Size,Type,View,Insert}` · `setOuterStringerShape…` · `setInnerStringerShape…` |
| central-strut foot plate | `setPostCreateBasePlate` · `setPostBasePlate{Thick,Wide,Length,HoleDm,HoleHor,HoleVer,AsPoly}` · `setPostLowerOffset` / `setPostUpperOffset` |
| Steps | `setStepStatus` · `setStepType` · `setStepPlateThickness` · `setStepWide` · `setStepTaper` (= `Angular extension`) · `setStepBendRadius` · `setStepFrontFlangeLength` / `setStepBackFlangeLength` · `setStepOuterEdgeStraight` · `setStepOpenOnTop` · `setStep_b/_c/_d` · `setStepHoleDist` · `setStepHoleDm` · `setStepSlottetHoleAxis` *(sic)* · `setStepOffset` · `setStepHeightOffset` · **`setStepGroupStatus(Int32)`** = the No Group / Subgroup / Assembly choice |
| step-mounted banister posts | `setCentralStepAllowRail` · `setCentralStepPostNumber` · `setCentralStepPostHoleDm` · `setCentralStepPostDistance` · `setCentralStepPostInBetween` · `setCentralStepPostVerticalOffset` |
| Banister | `setInnerRailStatus` · `setOuterRailStatus` · `setRailFrameOnly` · `setRailConnectionType` · `setRailInner{Start,End,Side,SideX,SideY}Offset` and the `Outer` twins · `setRailIds(InnerPolyId, OuterPolyId)` |
| Screws | `setDrillStepsStatus` · `setDrillRailStatus` · `setStepBoltStatus` + **`setStepBoltCRC`** · `setRailBoltStatus` + **`setRailBoltCRC`** |
| the created parts | `get_CentralPostId` · `get_OuterStringerId` · `get_InnerStringerId` · `get_CentralPostBasePlateId` · `appendStepId`/`appendBoltId` and their `clear*` twins |

⚠️ **Bolt styles are CRC-only again** (B.21) — a name must be resolved through `PsObjectStyleList`.
⭐ **Note `setStepSlottetHoleAxis`** — misspelled in the API, like `setHeigth` on `PsStairs`.
**Copy these names; do not type them.**

---

# MEASURED 11/08/2026

## ⛔⛔ `insert()` RETURNS **TRUE** AND CREATES NOTHING

Five configurations, in the `E.02-WINDING-STAIR` band:

| | `insert()` | census | read-back |
|---|---|---|---|
| A `init()` + defaults | **True** | 100 → 100 | all zero |
| B **no `init()`** (control) | **True** | 100 → 100 | all zero |
| C central strut, sized | **True** | 100 → 100 | `kSinglePost outerR=1200 innerR=100 angle=360` ✓ |
| D strings both sides | **True** | 100 → 100 | `kDoubleStringer outerR=1200 innerR=400 angle=360` ✓ |
| E D + profiles + steps | **True** | 100 → 100 | `outerStr=True'DIN_FLACH/200X10'` ✓ |

> ### ⭐⭐⭐ This is the DANGEROUS direction of B.12's rule
> B.12 established that *"`Create()` lies in BOTH directions — it returns `False` while succeeding
> **and** `True` while doing nothing."* Every structural creator so far has lied the **safe** way:
> `PsStairs.Insert=False`, `PsBracing.insert=False`, `PsPortalFrame=0`.
> **`PsCircularStairs.insert()` is the first to lie the OTHER way.**
>
> ⚠️ **A `False` at least tells you something went wrong. A `True` over an unchanged census is a
> silent no-op**, and any code that trusted the boolean would report a staircase that does not
> exist. **Only the census caught it.**

⭐ **And every setter round-trips.** `layout`, `outerRadius`, `innerRadius`, `angle`, and
`outerStringerShapeClass/Size` all read back exactly as written. **So it is not transport (B.24's
NaN) and it is not the configuration.**

⭐ **`init()` makes no difference at all** — case A (with) and case B (without) are byte-identical
in both result and read-back. ⇒ **`init()` is not the missing precondition for this class either**,
which is the same answer `PsPortalFrame` gave in E.1.

## ⇒ E.2's verdict

**A winding stair cannot be created from code**, and it confirms E.1's rule from the one class that
had a plausible extra precondition. **The `StructuralObject` self-inserting family refuses without
exception; only the separate `PsCreate*` creator (E.3's handrail) works.**

⚠️ **And even if it did build, the manual says the product is for overview and visualisation.**
For EB that is the operative fact.

**What IS reachable:** every parameter above is settable and readable, so a spiral stair drawn by
Amir in the dialog can be read and audited in full — including which parts it owns, through
`get_CentralPostId` / `get_OuterStringerId` / `get_InnerStringerId` /
`get_CentralPostBasePlateId`.

## Model state
Band `E.02-WINDING-STAIR`, grid at x = 180 000 (handle `42C`). **Five attempts created nothing —
census 100 → 100 throughout.** Saved.

## Still open
* ⚠️ **Binding a real `Ks_CircularStairs`** — `PsTransaction.GetObject` has an overload for it
  (B.23), but there is none in this model to bind. **A spiral stair drawn by Amir would close it.**
* ⚠️ **`Distance from front edge` / `Spacing` in RADIAN MEASURE** — read in the manual, never
  exercised, and a units trap worth remembering.
* ⚪ The `Data path` for user-block stairs — read, not pursued.
