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

---

# STEP 2 — THE WINDING STAIR IS BUILT

*11/08/2026. Band `E.02-WINDING-STAIR`, centre x = 180 000. **81 parts.***

`insert()` refuses, so the stair is built by composition — as E.1's was, and B.23's gusset and
B.25's braced bay before it. The layout follows the chapter's own **`kSinglePost`**: a central
strut carrying the steps, with the banister posts seated in holes drilled through the **outer**
side of each step — the detail E.2 §5 describes in as many words.

```
centre (180000, 0) · 16 steps · step angle 22.5° · total angle 360° (one full turn)
rise/step 175 · total rise 2800 · outer radius 1150 · inner radius 115

post      RO 219.1x8   DIN RUNDROHR      z 0 … 3200
arm       FL 120x10    rot=0 = FLAT      r 115 … 1150 at the step's mid-angle
                                         axis z = top−13  ->  top face at top−8
step      poly plate 8 mm, TRUE SECTOR   at z = top−4  (at= is the MID-PLANE)
bolts     M12 DIN7990, 2 per step        step->arm at r 400 and r 1000, vertical
baluster  RO 42.4x3.2                    through a 44 mm hole, 4° off the arm
```

| verification | result |
|---|---|
| `vfy_fit` (model-wide) | **`bolts=184 OK=184 BOLT-NO-HOLE=0 GAP-IN-PACKET=0 OVERSIZED=0 SHORT=0`** |
| step tops | 175 … 2800, **all 15 gaps exactly 175.0** |
| `collision` in the E.02 band | **0** — after the 16 baluster penetrations were cut with `boolean`. See below |
| `collision` in the E.1 band | **0** — untouched |

## ⭐⭐ `polyplate` AND `plate9 mode=poly` ARE NOT THE SAME OP

`polyplate` refused every sector with `create=False`. Isolated in six configurations — square,
triangle, coarse sector, reversed winding, at the origin and at x = 186 000 — **all failed, and the
identical contour at z = 0 built.**

```
PolyPlate  : AppendEdgePoint only         -> the contour must lie at z=0
Plate9 poly: SetInsertMatrix(at,ex,ey,ez) -> the points are LOCAL, at= places it in the world
```

⇒ ⭐ **For a plate anywhere except z = 0, use `plate9 mode=poly`.** And `at` is the plate's
**mid-plane**, not its underside — `at z=167, t=8` measured back as `163…171`. Same ±t/2 rule as
E.1's treads.

## ⭐⭐ THE CHORD TRAP — a sector built from two inner points is not a sector

The first build put the step's inner edge between two points at r = 110. **A straight chord across
22.5° dips to `110·cos(11.25°) = 107.89`**, and the post `RO 219.1` has an outer radius of
**109.55** — so all 16 steps cut **1.66 mm** into the central strut.

**The collision points came back at radius 107.90.** The geometry named its own fault to two
decimal places.

> ### ⭐ SUBDIVIDE **BOTH** ARCS, NOT JUST THE OUTER ONE.
> An arc approximated by a polygon always falls **inside** the true radius. On the outer edge that
> is a harmless 1 mm of lost tread; on an inner edge running against a column it is interference.
> Fixed by subdividing the inner arc in 5 segments too and opening the inner radius to 115.

Rebuilt in **one clean pass** — the whole band deleted first, because the arms already carried
holes and E.1 established that a second drilling pass over a drilled part is irreversible.

## ⛔⛔ `collision` IS NOT IDEMPOTENT — IT COUNTS ITS OWN LEFTOVERS

The check **creates a `Ks_VolBody` solid per collision and leaves it in the drawing**. The next run
then treats those solids as parts:

```
run 1   parts=355  collisions=71   newSolids=71
run 2   parts=426  collisions=207  newSolids=207     <- 71 solids became 71 new parts
```

⇒ ⭐ **ERASE EVERY `Ks_VolBody` AFTER EVERY COLLISION RUN, AND ONLY TRUST THE FIRST RUN AFTER A
CLEAN.** A rising collision count across repeated runs is the checker's own residue, not a
worsening model. E.1 hit this as "crash solids" without naming the mechanism.

## ⭐⭐ THE COLLISION CHECK DOES NOT SUBTRACT A DRILLED HOLE FROM A PLAIN SHAPE

All 16 remaining collisions are a banister post sitting in the hole drilled for it. The hole is
**measured**: `dia=44, z 175 → 167, depth 8.0` — full thickness. The post is `RO 42.4` — **0.8 mm
of clearance all round.**

Proven by controlled test rather than asserted:

| | E.02 collisions |
|---|---|
| baseline | **16** |
| that one post deleted | **15** |
| same post rebuilt free-standing at r = 1300 | **15** |

⇒ **The seated post is the collider, and a measured full-depth hole does not clear it.** Bolts are
treated differently — E.1's 128 bolts all sit in drilled holes and report **0**.

⚠️ **So a non-zero collision count is not automatically a defect**, and this is the case that
proves it. The 16 here are the manual's own banister detail. **They are reported, not hidden, and
not "fixed" by abandoning the detail the chapter describes.**

### ⭐⭐⭐ RESOLVED IN E.3 — AND THE MECHANISM HAS A NAME: `hf` IS NOT `s`

*Added 11/08/2026 while working E.3.* B.12 p.204 names both the detail and the cure:

> *"You can also subtract one shape from an other to create penetrations, e.g. to obtain slotted
> tubes, **penetrated handrail posts** or others."*

`op=boolean handle=<step> tool=<baluster> mode=sub`, applied to all 16 — **E.02 collisions 16 → 0**,
model-wide 40 → 24. And the modification signature says exactly why:

```
mods[f0 c0 hf3 o0 p0 s0 ...] -> [f0 c0 hf3 o0 p0 s1 ...]
            ^^^ 3 HOLE FEATURES        ^^^ 1 SUBTRACTION
```

> ### ⭐⭐ A DRILLED HOLE AND A BOOLEAN SUBTRACTION ARE DIFFERENT MODIFICATION KINDS.
> `hf` is a **fabrication instruction** — it tells the shop to drill. `s` actually **removes solid
> volume**. **The collision checker honours `s` and ignores `hf`** — which is the precise form of
> what was measured above, and it is why the hole was real, full depth, and still flagged.
> Bolts are the exception: ProSteel pairs a bolt with its own hole, so E.1's 128 report 0.

⚠️ **The weight does NOT move on a boolean** — `wt=16.128->16.128`. The modification inventory is
the only witness, exactly as the plugin's own `DetailCut` comment says for detail cuts.

⇒ **The band now verifies at `collisions=0` with the manual's banister detail intact.** Both facts
were needed: the detail was never wrong, and neither was the checker — they measure different things.

## Model state
Band `E.02-WINDING-STAIR`, grid at x = 180 000 (handle `42C`). **Five `insert()` attempts created
nothing — census 100 → 100 throughout.** The stair standing there now was built by composition:
**1 post + 16 arms + 16 balusters + 16 step plates + 32 bolts = 81 parts.** Saved.

## Still open
* ⬜ **The banister rail itself** — 16 posts stand, nothing spans them. `PsCreateHandrail` (E.3)
  along a helical polyline is the obvious route and is the first thing to try in E.3.
* ⬜ **No landing at the top**, and no fixing of the strut to a floor — `setPostCreateBasePlate`
  exists on the refusing class, so a foot plate would have to be composed too.
* ⚠️ **Binding a real `Ks_CircularStairs`** — `PsTransaction.GetObject` has an overload for it
  (B.23), but there is none in this model to bind. **A spiral stair drawn by Amir would close it.**
* ⚠️ **`Distance from front edge` / `Spacing` in RADIAN MEASURE** — read in the manual, never
  exercised, and a units trap worth remembering.
* ⚪ The `Data path` for user-block stairs — read, not pursued.
