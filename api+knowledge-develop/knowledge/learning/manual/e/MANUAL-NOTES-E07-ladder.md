# E.7 Structural Element — Ladder

> ## ✅ CLOSED 11/08/2026.
> **24 parts · 6 bolts · JOINT AUDIT CLEAN · `vfy_fit bolts=258 OK=258 BOLT-NO-HOLE=0
> GAP-IN-PACKET=0 SHORT=0` · band collisions 31 → 5 after the `Fit… Rungs` cuts.**

*Read 11/08/2026, pages 1109–1117 (fulltext lines 28669–28843). Band `E.07-LADDER`, grid `1BFD`
at x = 480 000. Plugin v178.*

> *"This function is used to create a ladder including its fastening at the wall. In addition, you
> can add a safety cage to the ladder."*

---

# THE DIALOG — six tabs

### Dimensions
| field | the manual's words |
|---|---|
| `Width` | *"the width of the ladder as **clear dimension between the uprights**"* |
| `Height` | *"without possible projection as help for climbing out"* |
| ⭐ `Riser` | *"the **desired** distance between the rungs. The program divides the distances between first and last rung regularly according to this specification and **the value is rounded up or down** correspondingly."* |
| ⭐ `Actual Riser` | *"The **actual resulting** distance between the rungs is displayed."* |
| `Distance to Floor` / `Top Distance` | upper edge of the **first** / **last** rung to the ground / climbing-out surface |
| `Offset` · `Stringer` · `Sharp Bend` | the climbing-out help: its height, its depth, and *"the front jump-in… if it has to be created with a bend"* — all **axis measures** |
| ⭐ `Fit… Rungs` | *"the rungs are **adapted to the uprights**. Round tubes are e.g. **cut with each other**."* |

> ### ⭐⭐ "ASK FOR A SPACING, GET A ROUNDED ONE" — **FOURTH** INSTANCE
> E.3 `PostSpace` (asked 1000, got 900) · E.5 `Segment Dist.` · E.6 `Fixed Grid` → `Effective Grid` ·
> **E.7 `Riser` → `Actual Riser`.** Here the chapter gives the rounded value **its own read-only
> field**, which is the clearest statement of the pattern in the whole manual.
> ⇒ **Never quote a requested spacing as the built spacing.**

### Shapes
⭐ *"The **upright** shape can be rotated by increments of **90°** whereas the **rungs** can be
rotated by increments of **45°**."* · `Mirror`.

### Wall Mounting
⚠️ *"The wall shape is **only created if the Wall Shape field has been checked**."*
`Position` = **Inner Edge** *(at the inner edge of the upright, showing inward)* / **Centrally**
*(axis position at upright-axis height)* / **Outer Edge** · `Wall Distance` *(upright axes to wall
surface)* · `Distance to Floor` · ⭐ `Distance` *(again "rounded up or down")* · `Top Distance`.

### Safety Cage
⚠️ *"only created if the Safety Cage field has been checked"*.
`Radius` (of the cage stiffeners) · ⭐ `No. of Rungs` — *"how many **longitudinal** rungs… divided
regularly on the resulting **semicircle**"* · `Lower Radius` + `Lower Distance` — *"enter a bigger
radius if you want a **conical** cage… keep 0 in both if you do not"* · `Depth` · `Distance to
Floor` · ⭐ `Distance` *(rounded again)* · `Top Distance`.

### Lateral Climbing Out
`Position` — *"to the left, to the right or **to both sides**"* · `Upper Distance` · `Lower
Distance` · `Extended Distance` *(offset beyond the cage in the climbing-out direction)*.

### Assignments
Per-shape material / grade / coating.

---

# ⛔ `PsLadder` REFUSES — RE-MEASURED IN THIS BAND

```
structprobe kind=ladder at=486000,0,0  ->  PsLadder.insert(Void)  census 715 -> 715
entities 715 -> 715   (delta 0)
```

⭐ **`insert` is declared `Void`** — `insert(PsPoint PickPoint, PsVector X_axis, PsVector Y_axis,
PsPoint End)`. There is no boolean to read. **The census is the only verdict**, which is why the
plugin's probe was written that way.

⚠️ **This was re-measured rather than cited.** E.1 recorded `census 82→82` from a **single**
default-configuration run, where `PsStairs` had been refused under **seven**. E.4 set the precedent
of re-measuring in your own band; this is that, and it agrees.

⇒ **Category (b): a self-inserting `StructuralObject` that refuses.** Not category (c) — unlike
`PsTruss` and `PsPurlinDistribution`, `PsLadder` **does** have a self-insert method. It just does
nothing.

## 🛑 AND A LIVE CONTRADICTION IN OUR OWN NOTES, NOW FIXED

`SKILL.md` carried, until today:

```
✅ PsCreateHandrail.Create() · PsBracing.insert() · PsLadder.insert() · PsPortalFrame.init()+insert()
```

while `references/plugin-ops.md` carried `| PsLadder | self-insert · insert(...) (Void) | ⛔ nothing
created |`. **B.23's table mapped METHOD EXISTENCE and was read as capability.** Corrected in both
skill copies to the three real categories: **works · refuses · nothing to call.**

---

# ⭐ THE API IS ALL PROPERTIES, AND IT IS GERMAN

`PsLadder` has **three methods** (`insert`, `listInformation`, `retrieveGeometrie`) and **~65
properties**. Not one `get*`/`set*` — unlike `PsStairs`, which is method-heavy.

```
Holm…    = the UPRIGHTS   HolmShapeClass/Size/Type/Turn/Mirrored/Resolution
Sprosse… = the RUNGS      SprosseShapeClass/Size/Type/Turn/Mirrored/Resolution
Knick    = the BEND       KnickOffset  (the chapter's "Sharp Bend")
Back…    = WALL MOUNTING  CreateBacksupport · BackShape* · BackShapePosition · BackOffset ·
                          BackJump · BackSupportStart/Between/End
Cage…    = SAFETY CAGE    CreateCage · CageShape* · CageRadius · CageRadiusLower · CageCount ·
                          CageStart/Between/End · CageDeepth (sic) · CageDepthLower
SideExit… = LATERAL CLIMBING OUT   CreateSideExit · SideExitType · Upper/Lower/ExtensionDist
Wide · Height · Steps · LowerStep · UpperStep · TopOffset · Angle · DrawDiagonal
⭐ CutSprosseAtHolm   = the chapter's "Fit… Rungs", literally
```

⚠️ **`CageDeepth` is misspelled** while its sibling `CageDepthLower` is not — the `setHeigth` /
`setStepSlottetHoleAxis` family. **Copy these names; do not type them.**
⚠️ **There is no `compute*` method of any kind**, so **`Actual Riser` is not available from the
API** — it is the dialog's own arithmetic, and it has to be reproduced by hand.

---

# STEP 2 — WHAT IS BUILT

The chapter's arithmetic, reproduced rather than guessed:

```
Width 500 clear · Height 4000 · Distance to Floor 300 · Top Distance 200
usable 4000 − 300 − 200 = 3500 ;  3500 / 280 asked = 12.5 -> 12 gaps
⇒ ACTUAL RISER 291.67, 13 rungs          <- the chapter's own rounding, done by hand
```

```
uprights   FL 60×10 rot=0, at y=0 and y=500, z 0…4000
rungs      13 × RO 26.9×2.6 spanning y 0…500
brackets   3 × FL 60×10 rot=90, Wall Distance 200, at z 600 / 2000 / 3400
bolts      6 × M12 DIN7990, 2 per bracket, through 10 + 10 = 20 mm
cuts       26 boolean subtractions = the chapter's `Fit… Rungs` / `CutSprosseAtHolm`
```

| verification | result |
|---|---|
| `vfy_fit` | **`bolts=258 OK=258 BOLT-NO-HOLE=0 GAP-IN-PACKET=0 SHORT=0`** |
| **JOINT AUDIT** | **CLEAN** — 18 members, none floating |
| `collision` in band | **31 → 5** after the `Fit… Rungs` cuts |

## ⭐⭐ TWO PARTS THIN IN DIFFERENT DIRECTIONS CAN NEVER LAP

Two bolting passes failed before the detail was measured instead of assumed:

```
upright rot=90   Wide 60 along XAxis=+y · Height 10 along −x   => THIN IN X
bracket rot=0    Wide 60 along XAxis=−y · Height 10 along +z   => THIN IN Z
```

⇒ **A bolt needs one common thin direction.** And a member running along **X** can never be thin in
X, because its cross-section lives in the Y–Z plane — so the lap direction had to be **Y**, and
both parts had to be made thin in Y:

| member | axis | `rot=0` | `rot=90` |
|---|---|---|---|
| upright | z | **thin in Y** ✅ | thin in X |
| bracket | x | thin in Z | **thin in Y** ✅ |

⭐ **And the section is CENTRED on the origin** — the failed bracket's holes spanned y −40…+20
about an origin at y = −10, which is 60 wide centred on it. So a face at y = −5 needs an origin at
y = −10 for a 10-thick part.

> ### ⭐ THE INSTRUMENT IS `propfull`, AND IT SHOULD HAVE BEEN THE FIRST CALL.
> `Wide` runs along the part's **XAxis**, `Height` along its **YAxis**, and `rot` turns both about
> the member axis. One call answers "which way is this section facing"; two build passes were spent
> assuming it. Same lesson as E.5, one chapter later.

---

# Still open
* ⬜ **The safety cage** — `CreateCage` + 10 `Cage*` properties, including a **conical** variant and
  longitudinal bars *"divided regularly on the resulting semicircle"*. Read, not built: the hoops
  are arcs and would need `arcplate`/`bendshape`.
* ⬜ **Lateral climbing out** (`CreateSideExit`, left / right / both) — read, not built.
* ⬜ **The climbing-out help** (`Offset` / `Stringer` / `Sharp Bend` / `KnickOffset`) — read, not built.
* ⚠️ **5 collisions remain** in the band after the `Fit… Rungs` cuts, not chased down.
* ⬜ `Actual Riser` has no API equivalent — worth a helper if ladders become routine.
