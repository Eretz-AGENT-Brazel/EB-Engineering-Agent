# 🔍 Part-B self-audit — B.1 … B.29

*Commissioned by Amir, 10/08/2026:*

> *"אני קצת חושש מזה שבמקריות נגענו רק עכשיו בפרק A — ולא עבדנו על פי סדר הפרקים כמו שצריך…
> תעבור פרק פרק בתוך חלק B, החל מפרק B.1 ועד לפרק B.29, ותעשה בקרה על כל פרק ופרק בנפרד.
> …זהו פרק הליבה של פיתוח יכולות המידול שלך בתוכנה ובהקשר ישיר לזה פיתוח ה-API."*

Three questions per chapter, his:

1. **Is there anything I can improve?**
2. **Did I learn the chapter deeply, and do I have from it everything the API development needs?**
3. **If there is something to improve — do it, in the models I practised on.**

**Method, fixed before starting so it cannot drift:**

| step | what it means |
|---|---|
| **1 · re-read the manual** | the chapter's own text again, looking for what I never touched |
| **2 · re-read my notes** | what I claimed — and which of it was *measured* rather than assumed |
| **3 · inspect the model band** | is the implementation actually there, and correct |
| **4 · check API coverage** | which classes/methods the chapter implies, and which ops exist |
| **5 · fix** | in `B08-insert-shapes.dwg`, the band the chapter owns |
| **6 · verdict** | written, including what is still open |

**Model:** `projects/SANDBOX/B08-insert-shapes.dwg` — 834 entities at the start of the audit,
post-fix state from this morning's bolt audit (179 bolts, 0 iron-rule violations).

---

## Where the reading was thinnest — checked before starting

Notes lines ÷ manual lines. Not a quality measure on its own, but it says where to look hardest.

| chapter | manual | notes | ratio | |
|---|---:|---:|---:|---|
| **B.29 Positioning** | **878** | **125** | **0.14** | ⚠️ biggest chapter, thinnest notes |
| B.8 Insert Shapes | 564 | 153 | 0.27 | ⚠️ |
| B.5 Display / Assign | 367 | 109 | 0.30 | ⚠️ |
| B.3 3D Object Views | 333 | 102 | 0.31 | ⚠️ |
| B.12 3D-Modifications | 769 | 322 | 0.42 | |
| B.2 Construction Utilities | 153 | 64 | 0.42 | |
| B.17 Plate Connections | 446 | 208 | 0.47 | |
| B.28 Group Structure | 304 | 164 | 0.54 | |
| B.6 Work Frames | 437 | 242 | 0.55 | |
| B.15 Bolts | 340 | 213 | 0.63 | |
| B.13 Plate Editor | 159 | 102 | 0.64 | |
| B.14 Drilling | 326 | 218 | 0.67 | |
| B.1 Layer Functions | 105 | 71 | 0.68 | |
| B.9 Insert Plates | 378 | 268 | 0.71 | |
| B.24 Dynamic Bracing | 352 | 359 | 1.02 | |
| B.10 Insert Solids | 99 | 104 | 1.05 | |
| B.11 ACIS reference | 75 | 100 | 1.33 | |
| B.16 Stiffeners | 166 | 255 | 1.54 | |
| B.7 Choose View | 58 | 107 | 1.84 | |
| B.23 Gusset Plates | 86 | 198 | 2.30 | |
| B.18 · B.19 · B.20 · B.21 · B.22 · B.25 · B.26 · B.27 | | | 0.8–1.2 | |

---

# The chapters

---

## B.1 — Layer Functions ✅ **improved at the root**

*Manual 1904–2008 (105 lines) · notes 71 lines · band: none (B.1 is model-wide)*

### 1 · Was the chapter learned deeply?
**Yes.** The notes carry the eleven `PS_LAYER` parameters, the three layer groups mapped
one-to-one onto real layer names, the black-vs-brown construction-line distinction, and the
09/08 measurement that found **88 parts on layer 0**.

### 2 · Does the API development have what it needs?
**It did not, and the gap was of my own making.**

The notes ended with a TODO from 09/08 — *"`plate` already takes `layer=`; the others need it
passing too"* — and the audit found that **11 of 17 creation ops still could not take a layer
at all**, while the plate paths called `UseCurrentLayer(true)` unconditionally.

Before adding a parameter to eleven ops I asked the better question: **what does the creator do
if told not to use the current layer and given no layer either?** New op **`layerprobe`**, three
plates, one variable, with the current layer deliberately forced to a junk value so a correct
result could not be luck:

| | call | landed on |
|---|---|---|
| A | `UseCurrentLayer(true)` | ❌ `ZZ_PROBE_WRONG` |
| **B** | **`UseCurrentLayer(false)`, no `SetLayer`** | ✅ **`PS_Plate`** |
| C | `UseCurrentLayer(false)` + `SetLayer` | ✅ `PS_Plate` (control) |

> ### ⭐⭐⭐ The 88 strays were self-inflicted
> B.1's opening line — *"automatic layer control… normally you don't have to take care of
> this"* — **is true for the API as well.** The plugin was overriding it. I had fixed the
> **symptom** in the model on 09/08 and left the **cause** in the code for a day.

### 3 · What was fixed
* **Six call sites** changed from `UseCurrentLayer(true)` to `(false)` (plugin v139 → v141).
  Verified against a wrong current layer: `plate` → `PS_Plate`, `polyplate` → `PS_Plate`,
  `beam` → `PS_Shape`, **with no `layer=` passed at all.**
* ⭐ **`layer=` is now an override, not a necessity.**
* **Solids are a genuine exception.** `PsCreatePrimitive` exposes **no `SetLayer` and no
  `UseCurrentLayer`** — it cannot be told where to build. So `solid` now assigns the layer
  after creation, defaulting to **`PS_Solid`**. Verified: `box` and `sphere` land on `PS_Solid`
  against a wrong current layer.
* New op **`layerprobe`** kept — it is how this class of question gets answered, and it cleans
  up after itself (probe layer restored, probe parts erased).

### Model state
Unchanged and correct: 834 entities, every part on its own layer, layer `0` holding only a
`PcRebarManager`. **The 09/08 fix held.** All audit scaffolding was removed and the entity
count returned to 834 exactly.

### Still open
* ⚠️ Ten further ops (`stiffener`, `weld`, `bend`, `bendtwo`, `grid`, `workframe`, `frame`,
  `boltfield`, `boltparts`, `threadedrod`) still take no `layer=`. **Most are now harmless** —
  their creators respect ProSteel's automatic control — but that has been *verified only for
  plates, shapes and solids*. The rest are **assumed, not measured**, and should be probed the
  same way when their chapters come up.

---

## B.2 — Construction Utilities ✅ **one real gap, closed**

*Manual 2009–2161 (153 lines) · notes 64 lines · band x ≈ 390 000*

### 1 · Was the chapter learned deeply?
**Yes, and the notes hold up.** They carry the whole dialog — `Direction`, `Line Type`,
`Distance`, `Scale`, `Angle`, `Number`, `Offset`, `Only in Plane`, `Create Reference Line`,
`Loop` — the eight direct `PS_CONST_*` commands, and both blocks of the measure dialog
including the **directional cosine** and the **angle relative to the UCS x-axis**.

The measurement behind them is sound and was checked two ways: `Dist Direct` **3472.751**
against AutoCAD's own `Length` for the same segment — **delta 0.00e+00** — and the direction
cosines square-summing to **1.000000000**.

### 2 · Does the API development have what it needs?
**Yes — but one claim rested on circular reasoning and has now been given a real basis.**

The notes asserted *"construction lines are ordinary AutoCAD lines on `PS_Const`"*. That was
concluded from lines **I had created myself as `AcDbLine`** — which proves nothing about what
ProSteel produces. Checked properly: **there is no ProSteel construction-line type anywhere in
the managed API surface** — no `PsConstructionLine`, no `Ks_Const*`, nothing. Combined with the
manual's own wording — the lines go on a layer *"so that all of them can be jointly hidden or
deleted"*, and `PS_CONST_DEL` deletes *"all construction lines drawn up to then **on the
layer**"* — the conclusion stands: **they are AutoCAD entities on a known layer, which is
exactly why the delete is a layer sweep and not a type sweep.**

### 3 · What was fixed

> ### ⭐ The band implemented only **half** of `Line Type`
> B.2.1 offers two kinds: standard lines *"the length of which is determined by projection"* and
> **X-lines *"which always run up to the edge of the screen"***. The band held **13 `AcDbLine`
> and no infinite lines at all.** Only the first kind had ever been built.

Added, on `PS_Const` so the layer sweep still catches them:

```
PS_Const before:  AcDbLine 13
PS_Const after :  AcDbLine 13 · AcDbXline 1 · AcDbRay 1
```

`AcDbXline` is the screen-edge kind (infinite both ways); `AcDbRay` is its half-open sibling,
worth having beside it so the family is complete.

### Still open
* `Scale` — *"distance/spacing converted to the scale of your drawing… allows actual dimensions
  to be used"*. Read but never exercised. Irrelevant while everything is modelled 1:1 in model
  space; **it would matter the moment a drawing is set to a scale**, and that belongs to part C.
* The `PS_CONST_*` commands remain off the `cmd` allowlist by design. The geometry they produce
  is reachable without them, which is what matters.

### ⭐ The takeaway that survives
**`PS_CONST_DVD` is a layout tool, not a drafting aid.** Purlin spacings, bolt rows, stair
stringers — anything at equal intervals — and **the start and end lines come free**. Measured:
4000 / 5 = 800 per segment, **6** perpendiculars for 5 divisions.

---

## B.3 — 3D Object Views ✅ **an open claim explained, not by re-testing but by A.6**

*Manual 2162–2494 (333 lines) · notes 102 lines · ratio 0.31, flagged for a hard look*

### 1 · Was the chapter learned deeply?
**Yes — the ratio was misleading.** The notes carry every sub-section and every detail that
matters: `Object View` vs **`Centered Object View`** (origin at the centreline vs at the pick
point), the alignment promise (*insertion direction parallel to the X-axis, so a slanted member
reads horizontally*), the **ALT / RETURN** re-align hint, `Object-UCS` setting **only** the UCS,
`PS_FACE_VIEW_CEN`, the **six** object-view directions versus the surface view's *any* face,
the five global views, `vpoint 0,0,1`, `Flip`, **0 = no cut planes**, and the perspective
camera's focal length.

They also contain something better than completeness: an item marked **"NOT REPRODUCED, and not
fairly tested"** rather than quietly dropped.

### 2 · Does the API development have what it needs?
**More than it did — because of A.2.** B.3.5 says the free view's clip distances *"can only be
done using the **global settings**"*, and B.3.3 says the five global views are *"specified in
the **Global Settings**"*. On 09/08 that was a dead end. It no longer is:

```
Ks_ComGlobalSettings.ObjCutPlaneDistance      500.0
Ks_ComGlobalSettings.ObjCutPlaneDistanceRear  500.0
Ks_ComGlobalSettings.SetGlobalViewDirection(Number, Coord, newVal)
```

### 3 · The open claim, explained

The unresolved item was B.3.6's *"when switching to one of the standard views, these values are
**overwritten**"* — set 1750/1250 on a view, switch with `view dir=front`, and they survived.

**The reason is now visible: there are two different places, and I had only ever read one.**

| | value | source |
|---|---|---|
| a **generated view's** clip distances | `0 / 0` | B.7, measured 09/08 |
| the **global** cut-plane distances | **`500 / 500`** | A.6, measured today |

⇒ B.3.6's *"0 = no cut planes are created"* + B.7's *"generated views arrive with clip 0/0"*
means **a generated view has no clipping at all until distances are given**, and *"a standard
view overwrites them"* means **the standard view applies the global pair over whatever the view
held.** Four chapters — B.3.6, B.3.5, B.7 and A.6 — agree once the settings object is in hand.

⛔ **The empirical half is not claimed.** Activating a work-frame view through `SetActive` and
watching the distances become 500/500 is a **B.7** test, and it will be run in B.7's audit
rather than here. One chapter at a time.

### Model state
Untouched. B.3 owns no band — it is view behaviour, and its measurements were `VIEWDIR` and
clip values, both re-confirmed rather than rebuilt.

### Still open
* `SetGlobalViewDirection` — the five global views are **settable and never have been**. Doing
  so changes Amir's installation defaults, so it goes on the same hold as the other A.6 writes.
* The perspective view (`Focal Distance`, `Distance`) is display-only by the manual's own
  statement — *"only a display view and does not allow any changes"* — so it has no API value
  beyond screenshots.

---

## B.4 — Move and Copy Parts 🛑 **a CEILING entry retracted**

*Manual 2495–2726 (232 lines) · notes 168 lines · band x ≈ 270 000*

### 1 · Was the chapter learned deeply?
**The reading was complete** — all seven sub-sections, including the alignment constraints
(3D / 2D / X / Y / Z / Free), `Turn+Copy`, `Mirror+Copy`, `Align+Copy`, the surface and 3-point
align methods, the rotate-with-vertical-offset that built the spiral staircase, and `Swap
Effect`. The notes even **quote** B.4.5's position-number prerequisite.

**But quoting a precondition is not honouring it**, and that is where the chapter failed.

### 2 · The failure — and it was on THE CEILING, the expensive place to be wrong

THE CEILING recorded: *"`PsDrillObject.TakeoverDrills` is the only transfer in the API and moves
**nothing** — 5 call sequences with selections proven correct, `changed=0` every time."*

**It transfers.** Measured on three fresh identical HE300B beams. `variant 1` is
`SetToDefaults` + `SetObjectId(src)` + `TakeoverDrills` and **nothing else** — there is no
fall-through to the composition, which is `variant 9`:

```
source  135E   hole y 150 → −150   z=1500  ⌀22
 → 135F        hole y 1050 → 1350  z=1500  ⌀22     changed=1
 → 1360        hole y 1450 → 1750  z=1500  ⌀22     changed=1
 → 1361        hole y 1850 → 2150  z=1500  ⌀22     changed=1
```

Each hole at **its own beam's centre** — verified by reading start / end / diameter back, not
by a count. Five of eight variants transferred; 6–8 have no `case` and correctly do nothing.

⚠️ **And the hypothesis that explained it was itself disproved.** B.4.5's stated precondition is
a matching position number, and the model carries **none** — so the 06/08 test was unfair. But
the control shows the gate is not there either:

| target | result |
|---|---|
| no position number | `changed=1` |
| a **different** position number | `changed=1` |
| the **same** position number | `changed=1` |

⇒ **Why 06/08 returned zero is unknown, and is left unknown rather than guessed at.** What is
established is that it works now, on identical parts, verified geometrically.

> ### ⭐⭐ The lesson is worth more than the capability
> *"Selections proven correct"* proved the **selection sets** were valid. It proved nothing
> about whether the call had been given what the manual asks for.
> **A "closed, do not retry" verdict is only as good as the preconditions the test honoured** —
> and a wrong entry on THE CEILING is the most expensive kind, because its entire purpose is to
> stop anyone looking again.
> ⇒ **Everything on that list closed without checking the manual's stated preconditions should
> be re-tested.**

### 3 · What was fixed
* THE CEILING's Clone entry **struck through**, with the measurement and an honest *"why it
  failed before is unknown"*.
* B.4's notes carry the same correction.
* The skill's CEILING block carries the retraction and the general lesson.
* ⚠️ **B.4.5's coordinate-system warning is confirmed in the data**: the source hole runs **−Y**
  and every target's runs **+Y**. *"The transfer refers to the coordinate system of the parts."*
  Immaterial for a through-hole; **it matters for a countersink or a slot**, and a mirrored
  target receives a mirrored modification.

### Model state
All 16 test beams erased. 834 baseline + the 2 construction lines added in B.2 = **836**.

### Still open
* The other four Clone categories — **Cuts, PolyCut, Notches, Boolean** — genuinely have no API
  entry point. `TakeoverDrills` remains the only transfer *method*; what changed is that it
  works.
* `clonedrills` still defaults to `variant 9`, the composition. **That default should probably
  change**, but not before the API route is exercised on **rotated and mirrored** targets —
  which is exactly where the coordinate-system warning bites.

---

## B.5 — Display / Assign parts ⚠️ **one of its four systems was never implemented**

*Manual 2727–3093 (367 lines) · notes 109 lines · ratio 0.30, flagged*

### 1 · Was the chapter learned deeply?
**Yes.** The notes carry all six sub-sections, the six `PS_HIDE*` variants, the difference
between ProSteel's `Regenerate` and AutoCAD's, `Count` raising the number of available classes,
`Complete Groups`, B.5.4's overlap sentence, and all of B.5.5 — the **position-number prefix**,
the colour, the common detail style, the separate 2D line settings, and the three assignment
modes (`Single Parts` adopts *all* the family data, `Groups` adopts **only the prefix**).

They also carry a sharp measured finding: *everything a connection class generated carries a
`FamilyClass`; everything created by hand does not* — so the prefix machinery runs underneath
and hand-built parts sit outside it.

### 2 · The gap — measured across all 820 parts

| system | assigned | unassigned |
|---|---:|---:|
| `AreaClass` | 305 | 515 |
| `FamilyClass` | 40 *(all by connection classes)* | **780** |
| **`DisplayClass`** | **0** | **820** |

⇒ ⚠️ **B.5.3 Display Classes was read and never exercised — zero parts in the whole model.**
Three of the chapter's four assignment systems were implemented; the fourth was not.

### 3 · What was done
Six fresh beams, display class **1** on three and **2** on three, area class **12** on all six:

* ✅ **`DisplayClass` is writable and sticks** — reads back 1 and 2 respectively.
* ✅ **B.5.4's independence claim confirmed** — *"area and display classes are completely
  independent from one another"*: a part held `d1 / a12` simultaneously, and the op's own tally
  reported them as separate axes (`d1/a12/f-1=3`, `d2/a12/f-1=3`).
* ✅ Per-part visibility toggles both ways.

> ### ⚠️ What I did NOT test, stated plainly
> B.5.4's *"in case of overlapping… the **last carried out action** will be valid"* is about
> **class-level** hide/show actions. My test hid and showed by **coordinate window**, which
> writes `PsObjectProperties.Visible` per part — the equivalent of `PS_HIDE`, not of the class
> buttons. **The overlap rule remains untested.**
>
> ⭐ But the data explains it. **There is one `Visible` boolean per part**, and both the display
> and area class actions write that same flag. *"Last carried out action wins"* is therefore a
> **description of a single shared flag, not a priority system** — which is why the rule reads
> the way it does. Recorded as an **explanation**, not as a measurement.

### Still open
* ⛔ **Assigning display classes across the model was NOT done, deliberately.** B.5.3's own
  examples are *"bracings, bay rails, curtain walls"* — **structural function**, which is a
  taxonomy for Amir to define, not for me to invent. The capability is proven; the scheme is his.
* Same for `FamilyClass` on the 780 hand-built parts: the family carries a **position-number
  prefix**, a colour and a detail style, so assigning one is a **fabrication decision**.
  ⚠️ It is also a **prerequisite for prefixed position numbers**, so it will matter when B.29 is
  audited.

---

## B.6 — Work Frames ✅ **the best-learned chapter so far; its one open route now closed**

*Manual 3094–3530 (437 lines) · notes 242 lines · band x 32 000 – 52 000, 41 `Ks_WorkFrame`*

### 1 · Was the chapter learned deeply?
**Yes — this is the standard the others should be measured against.** Every item checked was
present: the four basic types, `Absolute` heights, the **ALT** asymmetrical-division input
(`number*distance`) and **CTRL** to clear a dimension, roof angle / centre height / ridge width
(0 or = width → a single roof surface), the group name as a **prefix on every view name**,
*"work frames are OBJECTS"* and the UCS following them, the **layer-unlock warning**, all eleven
view flags, `Distances Cut.Surfaces`, and the whole axis-naming dialog down to **`Avoid I, O`**,
`Suppress First/Last`, `Decreasing`, `2 Lines`, `Dynamic` and `Axis Gap`.

And the measured half is the strongest in part B:

* ⭐ **`SetType(GridType)` is the shape switch** — without it every roof and radius value is
  *stored on the entity and never drawn*. The first cone had a 1721 mm bounding box (pure axis
  text) while `BottomRadius` read back a perfect 8000.
* ⭐ **The enum-collision trap**, caught and written down: a second `GridType` exists in another
  assembly (`aSa.PC.Shape.Graphics`) and reflecting by bare name found the wrong one.
* ⚠️ **`checkExistingGrids(name)` returned `True` for four brand-new names** — it is not the
  collision test its name implies.
* An honest **"Not reachable / not true"** section, which is what made this audit quick.

### 2 · The open route, now tried

B.6.7 *Additional Axes* — *"This function helps you to create an axis grid completely out of an
existing 2D-axis plan"*, i.e. build the grid from the architect's drawing. The notes left one
**recorded untried route**: `PsGrid.insert(Origin, Xaxis, Yaxis)` as an alternative creator.

**Tried today. It fails.** New op `gridaxes` builds a fresh `PsGrid`, sets `Length`/`Wide`, adds
user axes and inserts:

```
addedX=0  addedY=0  readBackX=0  readBackY=0   census 836 -> 836
```

`addUserXaxis` returns **false** on an un-inserted grid, and `insert()` creates nothing.

Three things were re-verified rather than assumed, because B.4 showed what assuming costs:

| claim | re-checked |
|---|---|
| *"`IKs_ComGrid` has no user-axis equivalent"* | ✅ **true** — no `AddUserXaxis`, no `GetUserXaxis` |
| *"`PsGrid` cannot bind to an existing frame"* | ✅ **true** — no `SetObjectId`, no `readFrom`, no binder of any kind |
| the untried `insert()` route | ❌ **fails** |

⇒ ⭐ **The difficulty is structural and now nameable: `PsCreateGrid` is the creator and has no
user-axis methods; `PsGrid` has the user axes and no creator and no binder.** The two halves
never meet in the API. **B.6.7 is genuinely dialog-only** — and that is now a *tested* closure,
not an open question left hanging.

### 3 · What was fixed
* New op **`gridaxes`** — kept, because it is the evidence, and it reads every axis back rather
  than trusting `addUserXaxis`' boolean.
* THE CEILING gains B.6.7 as a properly closed entry.
* The band was **not** modified: nothing in it was wrong.

### Still open
* `SetXViews` / `SetYViews` / `SetZViews` **do not produce a view per axis** — `B6_RECT` got the
  six surface views and a single `Y_1`. Still unexplained; low value, since the surface views
  are the ones used.
* B.6.9 user-defined blocks (`UserBlockNameX/Y`, `UserBlockPath`, scales) — writable over COM,
  **never exercised**. They are drawing presentation, not modelling.
