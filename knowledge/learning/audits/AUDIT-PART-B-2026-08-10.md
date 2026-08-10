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

---

## B.7 — Choose View ✅ **B.3's debt paid, and the claim resolved rather than refuted**

*Manual 3531–3588 (58 lines) · notes 107 lines · ratio 1.84*

### 1 · Was the chapter learned deeply?
**Yes, and completely** — it is the shortest chapter in part B and the notes are twice its
length. Every control is mapped: `Zoom Extents`, `Clipping-Plane`, the `Double Click` behaviour
and its CTRL inversion, activate-view, **activate-only-the-UCS**, delete, create-by-rectangle,
view-via-2-points, view-on-object and UCS-on-object.

⭐ And the structural finding stands: **the whole chapter is `IKs_ComWorkFrame` — nothing in
B.7 has a managed counterpart**, so it is reachable straight from Python through
`doc.HandleToObject(handle)`. No plugin needed for any of it.

### 2 · The debt from B.3, paid

B.3.6 claims *"when switching to one of the standard views, these values are **overwritten**"*.
On 09/08 that was tried with an **AutoCAD** view change and the distances survived; the note
correctly recorded it as **untested** rather than refuted, because a ProSteel standard view is a
work-frame view activated through `SetActive` — which is B.7's business, not B.3's.

Tested properly today:

```
GLOBAL cut-plane pair          500 / 500      (A.6, Ks_ComGlobalSettings)
work frame 2E8, as found       500 / 500
after SetClipDistances         1750 / 1250
after SetActive(True, True)    1750 / 1250    ← SURVIVED
```

⇒ **Not a refutation — a resolution.** The three chapters only disagree if one assumes a single
set of numbers. There are two:

| | |
|---|---|
| **B.3.6's `Distance`** | the **current session's** cut-plane distances |
| **a view's `GetClipDistances`** | that **view's own stored pair** |
| **the global pair** (A.6) | the default a fresh view inherits — **500 / 500** here |

Activating a view **applies the view's own pair**, which is precisely what *"your entered values
are overwritten"* describes from the dialog's side. B.3.6, B.3.5, B.6.5's *Distances
Cut.Surfaces* and B.7's `Clipping-Plane` checkbox all describe the same machinery from four
angles, and they agree once the three places are told apart.

⭐ Which also confirms 09/08's other finding from the other side: **clipping is controlled by
`SetActive`'s second argument, not by `EnableFrontClip`** — the toggle is the *act of
activation*, while the distances are ordinary stored values that persist.

### 3 · What was fixed
Nothing needed fixing. The test frame's distances were **restored to 500 / 500** so the model
is left exactly as found.

### Still open
* `SetXViews`/`SetYViews`/`SetZViews` not producing a view per axis — carried over from B.6,
  still unexplained, still low value.

---

## B.8 — Insert Shapes ⭐⭐⭐ **the biggest gap found so far: 1 890 sections and a whole sub-chapter never touched**

*Manual 3589–4152 (564 lines) · notes 153 lines (ratio 0.27, the lowest in part B) · band x −1000…16000*

### 1 · Was the chapter learned deeply?
**The notes cover all seven sub-sections** — B.8.1 Straight, B.8.2 Bent, B.8.3 Additional
Settings, B.8.4 Shape Series, B.8.5 Shape Segment, B.8.6 Girder Position, B.8.7 Automatic
Insertion. The low ratio was misleading, exactly as in B.5 and B.6.

**But the reading was of the dialog, and the implementation reached one corner of it.** The band
held 37 `Ks_Shape` and **two section names**: `HE 300 B` ×26 and `IPE 300` ×8, plus 3 flats.
Zero `Ks_BendShape`.

### 2 · Two real gaps, both fixed

#### Gap 1 — four of the chapter's five shape types were unreachable

`PsCreateShape` exposes **four selectors** and the `beam` op hardcoded the first:

```
SelectStandardSections()      <- the only one ever called
SelectSpecialSections()       SelectRoofWallSections()      SelectCombinationSections()
```

The shipped databases are not empty. Counted on disk:

| database | catalogs | **sections** |
|---|---:|---:|
| `UserShapes` (Special / Sopro) | 68 | **1 528** |
| `RoofWall` | 20 | **270** |
| `CombiShapes` | 15 | **88** |
| `WeldShapes` | 3 | 4 |
| | **106** | **1 890** |

⇒ **1 890 section definitions shipped with the product, reachable, and never used — because of
one hardcoded line.** Fixed: `beam kind=standard|special|roofwall|combi`.

⚠️ **The storage layout had to be measured, not assumed.** The *folder* is the catalog and the
`.psp` file is the section — my first five attempts passed the folder name as the section name
and all failed. Some catalogs are **`.dbf` tables instead** (`SCHRAG_z-pfetten`, `Kantteile`,
`Steel Deck`); there the section name is the row's **`KEY`** field.

⚠️ **The internal name field is not the key.** `Dreiecksbinder/R273x28-H440.psp` reads back as
`name='R244.5x22.2-H420'` while W/H = 273/440 match the *filename*. **Address sections by
filename.**

**Eleven built and read back**, all distinct, in a new strip at y −8000…−5000:

| catalog | section | W×H | what it is |
|---|---|---|---|
| `ayrshire_eb` | `eb_16020+00` | 160×90 | cold-formed purlin, 6.3 kg/m |
| `SCHRAG_z-pfetten` | `Z140-15` | — | **Z purlin** (`.dbf`) |
| `SCHRAG_c-riegel` | `C105-15` | — | **C rail** (`.dbf`) |
| `Kantteile` | `H200-B150` | — | bent sheet part |
| `Kranschienen_Form_A` | `A_100` | — | **crane rail** |
| `halfen_hl` | `hl_2626` | — | **Halfen cast-in channel** |
| `stair` | `step10` | — | stair tread |
| `Steel Deck` | `CFD3-22GA` | — | decking |
| `Bardage` | `4-250-36bx100` | **1000**×38 | cladding panel (roof-wall) |
| `Dreiecksbinder` | `R273x28-H440` | 273×440 | lattice girder, 225 kg/m (combination) |

⭐ **This is the answer to `section-variety`, and it is bigger than the 357 standard catalogs.**
The cold-formed purlin world — Z, C, Σ, zeta, from SCHRAG, Sadef, SBE, Ayrshire — is what
**B.22 Purlins** needed and never had.

#### Gap 2 — B.8.2 *Bent Shapes* had **no op at all**

`PsCreateBendShape` appeared **nowhere in the plugin**. The `bend` op bends a **plate**
(`PsCreateBendPlate`). So the whole sub-chapter was read and never built.

⭐ **And it is the only route to the fifth shape type.** Measured across .NET and COM alike:

| creator | selectors |
|---|---|
| `PsCreateShape` (straight) | Combination, RoofWall, Special, Standard — **4** |
| `PsCreateBendShape` (bent) | Combination, RoofWall, Special, Standard, **Weld** — **5** |
| `IKs_ComCreateBendShape`, `Ks_ComCreateBendShapeClass` | same 5 |

New op **`bendshape`** — path from `pts=` / `circle=` / `helix=` / `handle=`, plus the same
`kind=` selector. Five built in a strip at y −11000, including **two welded plate girders**
(`I950x300x30`, `K900x400`) that were previously unreachable by any route.

⚠️ **Measured rule: a bent shape needs ≥ 3 path points.** Two points return nothing at all —
`K900x400` failed on 2 and succeeded on 3, the section name being identical.

### 3 · A trap found and guarded, not papered over

`handle=` uses `PsPolygon3d.ConvertFromPolyline`, the documented B.8.2 workflow — draw the path,
then apply a section. It **creates a shape and the shape does not follow the polyline.** A
polyline `(4000,−14000) → (4000,−11500) → (6500,−11500)` with a 90° bulge produced extents of
**x 3950…5250, y −14000…−11500** — the third vertex at x = 6500 is not inside the result at all.
Length read 5049 where a true quarter-circle predicts 5277 and a straight path reads 4985.
Calling `Update()` on the polyline first changes nothing (identical to the millimetre).

**Why it is not left as a footnote:** a route that silently builds *different* geometry from the
one asked for is the worst kind of trap — worse than one that refuses. So `bendshape` now
measures itself:

```
pts=   ->  pathfit=ok
bulged ->  pathfit=MISMATCH 1/3_vertices_outside_by_650mm
```

Every vertex of the requested path must lie inside the created part's bounding box. The three
unfaithful shapes and their source polylines were **deleted** — a reference model must not carry
geometry that does not match its own input.

### Still open
* **Why `ConvertFromPolyline` drops the arc.** Measured precisely, not explained. `circle=` also
  reads 9251 against 2πR = 9425 (98 %), so ProSteel appears to facet arcs — but that does not
  account for a vertex falling 650 mm outside. Recorded with numbers; not guessed at.
* `SetCrossSectionType(SectionType)` on the straight creator — a *sixth* axis of choice next to
  the four selectors, never exercised.
* B.8.4 Shape Series / B.8.5 Shape Segment / B.8.7 Automatic Insertion — read, and no API
  entry point has been looked for yet.

---

## B.9 — Insert Plates ⭐⭐ **the chapter is well learned; the AUDIT found the bug in my own tooling**

*Manual 4153–4529 (376 lines) · notes 268 lines (ratio 0.71) · band x 69 800 – 91 126*

### 1 · Was the chapter learned deeply?
**Yes, and honestly.** All five sub-sections, the seven insertion methods, the `CL` warning, where
a plate's reported Length and Width come from, the ALT arc > 180°, B.9.4's **segment tree** and
`Correction Value Unwinding` K, the `Insert Edge` values the manual refuses to list, a section
titled **"Four places this API lies, all measured"**, and a **⛔ call that must never be made**
(`PsPlate.computeObjectWeigth` kills AutoCAD outright — reproduced twice, once with a marker file
that survived). That last one is the kind of note that pays for itself.

### 2 · ⭐⭐⭐ The real finding: `dumpmodel` had been blind to every plate and every bolt

```
before:  shapes=349  plates=0    bolts=0    other=152  err=357
after :  shapes=349  plates=178  bolts=179  other=152  err=0
```

**178 plates and 179 bolts — the entire plate and bolt population of the part-B model — were
written as `ERR` rows** with *"Object reference not set to an instance of an object."* Every time
this session I "measured the model" with `dumpmodel`, plates and bolts were absent, and the loss
was reported only as an `err=` count that I never opened.

⚠️ **This is the same failure mode as B.4's retraction, in my own code:** a number that looked
like a summary was hiding the thing it was summarising.

**The cause is one unguarded dereference.** `InsertPoint` was fetched *inside* a `try` and read
*outside* it:

```csharp
PsPoint ip = null;
try { ip = pl.InsertPoint; } catch { }     // survives
if (ip == null) ip = new PsPoint(0,0,0);   // not null...
...F(ip.x)...                              // ...and throws anyway
```

A `PsPoint` handed back with a dead native handle **is not null and throws when read**. Shapes
escaped because they use `GetMidLine` into locally constructed points. Fixed at both sites.

⚠️ **And half a fix was not a fix.** With the crash guarded, all 178 plates came back carrying
`InsertPoint = 0,0,0` and a polygon in **local** coordinates — present in the dump and with no
world position at all. `PLATE` and `BOLT` rows now carry the **world bounding box** as well.

### 3 · B.9.3 Gratings — closed, where the notes had left it UNVERIFIED

The notes concluded the weight-reduction claim was untestable, because the only weight call known
kills the session. **It is testable.** `props` reads a plate's weight through
**`PsObjectProperties`** — E.9's surface — and never goes near `computeObjectWeigth`.

| what | measured |
|---|---|
| the setting | ⭐ **`Ks_ComGlobalSettings.PlateRasterWeightReduction`** — *Raster* is German for grating. **Shipped at 10 %** |
| does the `Grid` flag stick? | ✅ **yes, and it is readable**: `DisplayFlagsLong` **16436 → 24628** (bit **8192**) and `PitchLineMode` **False → True** |
| does the weight move? | ❌ **no.** 1000×500×30 reads **117.75 kg** — plain, flagged, at 0 %, at 10 % and at 35 % alike. 0.015 m³ × 7850 exactly |

⇒ **The `Grid` flag is a real, settable, readable property, and the raster reduction does not
touch the object's weight.** The object carries the gross figure. The manual places the reduction
in *"the weight for the parts list"* — **that half is still untested**, and is not claimed here.

⚠️ The global setting was changed to 35 % for the test and **restored to the shipped 10 %**; the
first restore attempt was rejected while AutoCAD was busy and was retried until it read back 10.

### 4 · What was left alone
`SetGrid` / `SetGridDirection` were already implemented in `plate9` — B.9.3 was **not** an
unimplemented sub-chapter, only an unverified claim. The band's 12 plates were not modified.

### Still open (unchanged from the notes, honestly)
* The grating **catalogue** — `Data/Plates/ImpGrating.mdb` and `Platten-Bleche-Roste.mdb` exist on
  disk, and `PsCreatePlate` has **no database selector at all**. Both insert buttons are pick-based.
* `CHECK BENT PLATE` — no API counterpart.
* Whether `K` is settable anywhere but `CreateOfTwoPlates`' `KValue`.

---

## B.10 — Insert Solids ⭐⭐⭐ **both "never fairly tested" entries closed — and both WORK**

*Manual 4529–… · notes 104 lines · band x = 370 000*

### 1 · Was the chapter learned deeply?
**Yes, and it earned its findings.** All ten commands, the warning about why ProSteel has its own
solids, `CreateTorus`' undocumented **third mandatory argument** (`length = 0` fails *silently*),
and ⭐⭐ **`SetPolygon` takes LOCAL coordinates** — caught because an extrusion came out
5120 × 5552 instead of 600 × 400. Eight of ten built with exact dimensions, and a solid was
proven to be a real ProSteel object by drilling it.

Its "Still open" section was **precise about why** each item was open — which is what made them
closable today.

### 2 · `hull` — it was never a fair test, and it works

> *the notes:* *"needs `SetPoints(PsDataPointArray)`, not `SetPolygon`; the op passes a polygon,
> so this was never a fair test. **A plugin change is required to try it properly.**"*

The plugin change was made (`solid dpts=…`, feeding a real `PsDataPointArray`) and **`CreateHull`
builds**:

| points | asked | measured |
|---|---|---|
| 6-point wedge | 800 × 600 × 600 | **800 × 600 × 600** ✅ |
| 4-point tetrahedron | 600 × 500 × 500 | **600 × 500 × 500** ✅ |
| 8-point **perfect box** | 600 × 600 × 500 | ❌ **nothing created** |
| 8-point box, corners jittered ±10 | — | **610 × 610 × 500** ✅ |

⚠️ **A perfect box defeats it.** Jitter the corners by 10 mm and the same eight points build.
Exactly-planar faces appear to be degenerate for the hull algorithm. Use `box` for boxes.

### 3 · `rotate` — closed, and the reason is geometry, not a refusal

Two hypotheses, both confirmed:

| | |
|---|---|
| ⭐ the polygon is **LOCAL 2D — `x,y` only** | the first axis-Z profile `200,0,0;500,0,0;500,0,400;200,0,400` collapsed to a **zero-area line** and failed; the same shape as `200,0;500,0;500,400;200,400` builds |
| ⭐⭐ the **axis is in WORLD coordinates** while the polygon is local | the first success came out **761 000 mm** across — swept around the world origin, 370 000 away. Put the axis through the insert point and a 300 × 400 profile at radius 200…500 measures **1000 × 400 × 1000**, exactly right |

⇒ ⭐ **And the axis must lie IN the profile's plane.** Axis Z is *perpendicular* to the local XY
plane the polygon lives in, so the profile sweeps within itself — a degenerate revolve. **Not a
bug; invalid geometry.** Every in-plane axis built.

⚠️ **`rev` does not arrive.** 90°, 180° and 360° produce **identical** solids —
1000 × 400 × 1000 all three. A full revolve every time. This is THE CEILING §2's signature: the
call works and the number is ignored.

### 4 · What this closes on THE CEILING
`CreateHull` and `CreateRotation` both leave §3 *"never fairly tested"* — **both work.** The
lesson is the same one B.4 taught in July and again this morning: **a verdict is only as good as
the test that produced it**, and "never fairly tested" is an honest label that must actually be
acted on rather than carried forward.

### Still open
* Why a **perfect box** defeats `CreateHull` — measured, not explained.
* Whether `rev` is degrees at all, or an enum/flag whose values I have not found.

---

## B.11 — Create ACIS Body Reference ⭐ **the chapter is sound; one of its EXPLANATIONS is retracted**

*notes 100 lines · band x = 380 000*

### 1 · Was the chapter learned deeply?
**Yes.** Both API classes mapped, the whole chain measured end to end, `GetFromMassProp`
correctly identified as *the manual's ESC-at-the-first-X-point* (the inertia-axes mode), and
⭐⭐ the manual's warning proved **literally true by census** — deleting the ACIS body takes the
reference with it, two entities gone for one delete. It also recorded a **measurement trap**:
a COM `HandleToObject` check right after the delete still reported the reference alive; only the
census (and a later fresh read) showed it gone.

**Re-verified today**, because it is cheap and the warning matters: `other` **168 → 166** on one
delete, and the reference then answers **`EX:eWasErased`** — the true signal, exactly as recorded.

### 2 · 🛑 The retraction — `massProp = false`

The notes did not just record the refusal; they **explained** it:

> *"The non-inertia mode needs the component coordinate system supplied first via
> **`SetInsertMatrix`** — with no matrix there are no axes, and the call declines."*

**`SetInsertMatrix` had never been called.** The explanation was reasoning, written where a
measurement belongs. Tested today — new `acisref ucs=origin;xaxis;yaxis`, building a real
`PsMatrix` via `SetCoordinateSystem`:

| call | result |
|---|---|
| `massProp=1` | ✅ `refId=13FB`, census +1 |
| `massProp=0`, no matrix | ❌ `refId=0`, census unchanged |
| `massProp=0` **+ world UCS** (`ucsSet` confirmed) | ❌ `refId=0`, census unchanged |
| `massProp=0` **+ a rotated UCS** (`ucsSet` confirmed) | ❌ `refId=0`, census unchanged |

⇒ ⭐ **The explanation is wrong and is withdrawn.** What remains is only the measurement:
**`massProp=false` refuses, and why is not known.**

*A reading, offered as a reading and not as a finding:* `GetFromMassProp=false` is plausibly the
manual's **three-point pick** mode — the user clicks the component's coordinate system — which
would put it under THE CEILING's one-line rule. **Not measured, not claimed.**

### 3 · The pattern this makes three times today
B.4 (`TakeoverDrills`), B.9 (*"the weight cannot be verified"*), B.10 (*"never a fair test"*) and
now B.11. Each time a conclusion outran its evidence, and each time the audit's only job was to
**run the test the note itself named**.

⇒ ⭐⭐ **Rule, now explicit:** an explanation written next to a measurement must say which of the
two it is. If a note names the missing call, the note is a **to-do**, not a finding.

---

## B.12 — 3D-Modifications ⭐⭐ **the largest and strongest chapter note in part B**

*Manual 4734–5434 · notes **322 lines** · all seven sub-sections*

### 1 · Was the chapter learned deeply?
**Yes — this is the reference standard.** B.12.6's cope, left open by both B.19 and B.20, is
solved here. And its measured half contains three findings that changed how everything else is
tested:

* ⭐⭐ **`Create()` lies in BOTH directions** — the cope returns `False` while succeeding *and*
  `True` while doing nothing. Only a read-back is evidence.
* ⭐⭐ **A template carries state the property dump does not expose.** A hand-built copy, identical
  on every property `PsCopeLinkDataMgd` exposes, did nothing; the template worked. ⇒ *`GetTemplate`
  and override; never construct link data from scratch.*
* ⚠️⚠️ **`PsObjectProperties.Weight` is the NOMINAL weight** — a 1200×600×20 plate reads 113.04 kg
  after two boolean subtractions *and* five ⌀60 holes. It ignores all material removal.

### 2 · ⭐ A cross-link the audit found: B.12 already explains B.9's grating result

Today's B.9.3 test measured 117.75 kg on a grating-flagged plate at every raster percentage and
concluded the object carries the gross figure. **B.12 had already established why**: the weight
`PsObjectProperties` reports is nominal and ignores holes and booleans alike. The two findings are
the same fact reached from two chapters — and B.9's conclusion is *stronger* for it, since a
number that ignores a ⌀60 hole was never going to show a 10 % raster reduction.

⇒ ⚠️ **The cost of not cross-reading:** B.9's test was run without recalling B.12's finding, and
would have been misread if it had come out any other way.

### 3 · The one unresolved item — attacked, and honestly still open

> *the note:* *whether `edge=2` genuinely produced **access holes (ratholes)** — `polyCuts=1`
> proves a cut, not its shape. Confirming it needs a shaded view, and `VSCURRENT` is not in the
> plugin's `CmdAllow` allowlist. **Not widening that allowlist without Amir's approval.**

That refusal was right and stands. A route needing **no view and no new permission** was tried
instead: build the same cope twice — `edge=2` (Access Holes) against `edge=0` (bevelled Edge) —
and distinguish them geometrically.

```
holes  edge=2  beam=140A  polyCuts=1   ext 400010.5,-6075,1350 ; 403000,-5925,1650
bevel  edge=0  beam=140B  polyCuts=1   ext 405010.5,-6075,1350 ; 408000,-5925,1650
```

⇒ **The two are indistinguishable at bounding-box level** — identical extents to the 0.1 mm, and
identical `mods` counts. A volumetric probe was considered and **not run**, because placing it
needs to know where the cut *ends*, and **no op exposes a polyCut's polygon**. A probe placed on a
guess that reported "no collision" would prove nothing.

⇒ **Still open, with the missing capability now named:** *a reader for a polyCut's polygon.* The
contrast pair is kept in the model, labelled, as the evidence and the starting point.

⚠️ The first attempt built the beam starting at the column **face** and produced `polyCuts=0` on
both — a cope needs the beam to run **through** the support, not butt against it. Worth recording:
a cope that silently does nothing may be a geometry error, not an API refusal.

---

## B.13 — Plate Editor ⭐⭐ **all three open items answered; a new .NET-vs-COM disagreement found**

*Manual 5434–5633 (~200 lines) · notes 102 lines · marked "closed hermetically"*

### 1 · Was the chapter learned deeply?
**Yes.** It is the chapter that turned *"214 polygons replaced by hand"* into three parameters,
and it correctly identified `Selected Edge` numbering with `0-0` meaning all round, and Top/Bottom
being independent. Its three open items were each stated **with the test that would close them** —
which is why all three closed today.

### 2 · The three items

**① What are the SIX edge-processing kinds?** The manual says *"six"* and never lists them.

```
Bentley.ProStructures.EdgeLayout
   kUnknownEdge, kFacet, kRadius, kRounded, kInverted, kFold, kNotch
```
⇒ **Six real kinds**, plus the undefined sentinel. **All six applied to six plates and read back
correctly** (`layout=1…6`, `tv=25,25`, top and bottom both set) — proven, not quoted.

⚠️ ⭐⭐ **NEW FINDING — the two APIs disagree.** The COM twin is **short by one**:

```
.NET  Bentley.ProStructures.EdgeLayout   kUnknownEdge kFacet kRadius kRounded kInverted kFold kNotch   (7)
COM   PSCOMWRAPPERLib.KsEdgeLayout       kUnknownEdge kFacet kRadius kRounded kInverted kFold          (6)
```

**`kNotch` exists only in .NET.** This is B.6's enum trap in a new form: there, two enums with the
same name in different assemblies; here, the *same* enum with **different members** across the two
APIs. ⇒ **When the two APIs disagree, the .NET one is the complete one** — and `two-apis-com-wrapper`
now needs the reverse caveat: COM rescues .NET when binding fails, but it can also be **behind** it.

**② Is `PsEdgeChamfer` creatable on its own?** ⇒ **It is constructible but has no creator.** Its
entire surface is a destructor plus properties — no `Create`, no `insert`, no `writeTo`. It is a
**data payload** assigned to **`em.PlateBreakEdge`** on the edit-modification object. That is the
only route, and it is the one the plugin already uses.

**③ Are `Min. Radius` / `Max. Height` readable?** ⇒ **No.** The only `MinimumRadius` /
`EffectiveMinimumRadius` in the whole API surface belong to **ISM's radial grid** and have nothing
to do with plate edges. **They are dialog-side validation only** — the software will tell a human
the limits and will not tell the code.

### 3 · A loose thread worth following
`PsEditPlateModification` exposes **`DisplayAsRaster`** — *Raster* again, the same word behind
B.9.3's grating flag. It may be the read/write route for that flag on an existing plate, which
`SetGrid` only offers at creation. **Not tested.**

### What was added to the model
Six plates at x = 410 000, one per edge kind, labelled — the enum made visible.

---

## B.14 — Drilling / Bolted Connections ⭐⭐⭐ **all three open items closed — one of them cost two AutoCAD crashes**

*Manual 5633–5900 · notes 218 lines · the chapter Amir gave a whole evening to*

### 1 · Was the chapter learned deeply?
**Yes.** The mental model (*a hole FIELD, not a hole*), the layout syntax, the four ways to create
holes with different behaviour each, the edge-distance system, and B.14.2 correcting an earlier
wrong conclusion of mine. It left **three** open items, each with the test named.

### 2 · ⛔ The edge-distance table — reachable, and **LETHAL**

> *the note:* *"where exactly does the edge-distance table sit, and is it reachable from the API?"*

**It is reachable.** `PsVolume.checkHoleEdgeDistance(Int32)` is the manual's check, and
`PsBaseObject.BlockHoleEdgeDistanceCheck` is the per-part suppression flag. A new `edgecheck` op
was written for it.

**It kills AutoCAD.** Isolated in stages, each with a save:

```
plate created   -> saved -> survived
hole drilled    -> saved -> survived
edgecheck ALONE, on a saved model  ->  process gone
```

No exception, no dialog, `EB_TIMEOUT` on the Python side and an empty `Get-Process acad`. **Second
member of a family** — the first is `PsPlate.computeObjectWeigth` from B.9, which cost five plates.

⇒ **New file: `knowledge/learning/findings/LETHAL-CALLS-do-not-invoke.md`** — both calls, what they
have in common (`check*`/`compute*` methods on the entity classes), the list of untested
neighbours, the isolate-and-save test protocol, and the recovery procedure.

⇒ The op was **not deleted**. It stays, refusing with an explanation, so the next attempt reads it
instead of paying again.

⇒ **Answer to B.14's question: the edge-distance table is dialog-only in practice.**

⚠️ **Nothing was lost** — the model had been saved immediately before. That was the protocol, and
it is the whole difference from B.9's five lost plates.

### 3 · ✅ The layout string — `SetLinearHoleField` takes the dialog's own syntax

Counts prove nothing (`3*70` and `2*60,200,1*` both give 6 holes). **Spacings prove it:**

| `x=` | measured gaps |
|---|---|
| `3*70` | **70, 70** — uniform |
| `2*60,200,1*` | **60, 200** — non-uniform, exactly as written |

⇒ **The same string syntax, honoured in full**, including the intermediate pitch and the trailing
`1*`.

### 4 · ✅ `W` — the marking gauge works through the API

> *the manual:* *"you can enter the **predefined marking gauges of the shape** by typing the letter
> **W** instead of a pitch, e.g. `2*W`"*

Proved by **contrast**, not by a hole count:

| beam | `y=` | measured cross gap |
|---|---|---|
| **HE 300 B** | `2*W` | **120 mm** |
| **IPE 300** | `2*W` | **80 mm** |
| HE 300 B | `2*100` *(control)* | **100 mm** |

Two sections, one command, **different gauges** — so `W` reads the section's own table. The
numeric control lands on exactly 100, so the two paths are distinct. No dialog appeared.

⇒ ⭐⭐ **Inherit the gauge; never invent bolt spacing.** This is the drilling half of the same
principle as *choose a template, do not pass numbers*.

### 5 · A defect in my own tooling, found in passing

**Nine plates built during this audit were stacked at the origin.** `plate9 mode=rect` places from
**`at=`**; I passed **`p1=`**, which is a valid key *for the op* (the `poly` and `pts` modes use
it) and therefore passed the strict-parameter guard **in silence**.

⚠️ ⭐ **The guard gave false confidence: valid FOR THE OP is not valid FOR THE MODE.** Fixed —
`plate9 mode=rect` now refuses `p1/p2/p3/pts/radius` and creates nothing. The nine plates were
moved to the strips their labels already pointed at, and the crash probe deleted.

**This is the audit's own lesson turned on the auditor:** a call that silently does something
other than what was asked is the worst kind, and I shipped one this afternoon.

### Plugin versions this chapter
v150 (edgecheck added) → **v151** (edgecheck disabled after the crash) → **v152** (the `plate9`
mode guard).

---

## B.15 — Bolts ⭐⭐ **B.15.4 was read and never implemented — and it is entirely reachable**

*notes 213 lines · all four sub-sections*

### 1 · Was the chapter learned deeply?
**Yes, and it produced three of the session's most useful traps:**
* ⛔ **"Drilling first is no longer necessary" is false at this API entry point.** Two plates with
  zero holes → `created=0`. Drilled first → 2 bolts, cleanly. ⭐ **Amir's own sequence — DRILL,
  then pick the two parts — is the only route available to code.**
* ⚠️⚠️ **`BoltStyle`, `BoltType` and `Diameter` are WRITE-ONLY** — the dump prints them as
  ordinary properties and they have no get accessor.
* ⭐ **`get_BoltStyleName(int)` is an INDEXER, invisible in the dump.**

### 2 · The gap: B.15.4 *Sort* had no op at all

Every button in that dialog is a method on **`PsObjectStyleList`**, and none was implemented:

| B.15.4 dialog | API |
|---|---|
| create a new style | `Append(name)` · `appendUniversalStyle(name)` |
| load one from file | `readStyleFromFile(styleId)` · `ReadFromFile()` |
| delete ⚠️ *without confirmation* | `DeleteAt(index)` |
| move up / down | `MoveUp` · `MoveDown` |
| ⭐ **update all styles from disk** | **`Synchronize(loadAll)`** · `Reload()` |

⭐ **Why the last one matters for fabrication.** The manual: *"The styles are stored as **objects
in the drawing**. When the style definition is modified on the hard disk, normally the
modifications are **not transferred** to the internal objects."* ⇒ **A bolt style is frozen into
the model the moment it is used.** Editing it on disk changes nothing in an existing drawing until
`Synchronize` runs. New op **`stylelist`**, and it works:

```
stylelist action=list type=0
  -> count=27 readFromFile=1
     [0]4.6S/crc=1851029805  [2]8.8S/crc=-401163854  [16]DIN6914/crc=-410302868
     [20]DIN7990/crc=-1614854285  … 27 entries with their CRCs
stylelist action=sync type=0   -> synchronized loadAll=True
```

⚠️ **`delete` is gated behind `confirm=DELETE`** — the manual says it deletes without asking, and
a style is referenced by every bolt that uses it.

### 3 · Two measurements the op paid for

⭐ **`Initialize()` alone leaves `Count` at 0. `ReadFromFile()` is what fills the list** — 0 → 27
on the same object. That is the same mechanism as the frozen-style warning: the definitions live
on disk and must be pulled in.

⭐⭐ **A FOURTH instance of the indexer trap, and it is now a rule.** The type dump prints
`Entry` and `Index` as plain properties; the compiler says they are
**`get_Entry(short)`** and **`get_Index(string)`**. After `get_ParentFlangeIndex(int)`,
`get_WeldStyleName(int)` and `get_BoltStyleName(int)`:

> ⭐ **When a `String` or `Int32` property looks like it should be a list, it is an indexer — and
> the type dump will not tell you. Let the compiler tell you.**

### Still open
* `PsBolt.RecomputeBoltLength()` exists and is **used nowhere**. It is the obvious candidate for
  B.26's twelve unassemblable bolts (six shorter than their packet, six leaving 11 mm for a 16 mm
  nut) — **awaiting Amir's decision on those**, so not fired here.
* `WriteStyleToFile` / `WriteToFile` — writing a style back to disk. Not in B.15.4's dialog list
  and not exercised.

---

## B.16 — Insert Stiffeners ⭐ **the chapter is excellent; its one open item closes with a NEGATIVE answer**

*notes 255 lines · band x = 90 000*

### 1 · Was the chapter learned deeply?
**Yes — it is among the two or three best in part B.** It did not stop at the dialog: it derived
the formula the manual only gestures at, decoded `Layout`/`ShapeType` **by the bulge rather than
the vertex count**, and produced three findings that generalise:

* ⭐ **`LengthType` is write-effective and read-broken.** Writing 0/1/2/3 gives full / exactly half
  / by-length / square. But **reading it back from a template does not reflect the template** —
  `half convex` and `full convex` both read `1`, `half rounded` and `full rounded` both read `2`.
  ⇒ **The template NAMES are correct; the exposed property is not.**
* ⛔ **A stiffener cannot be created without a template** — `template=none` gives `Create()=false`,
  while `Check()` returns **1 either way**, so `Check()` predicts nothing.
* ⭐ **Weld marks are real holes** — `CenterPunchType=1` puts 2 on the girder, one per stiffener.
* **One command, two stiffeners** — `created=2` across **33 insertions** without exception.

### 2 · The open item, closed

> *the note:* *"The manual's third option exists in the dialog; its ordinal is not 2 and remains
> unmeasured."*

`CenterPunchType` is a plain `Int32` with **no declared enum**, so the ordinals were swept on six
fresh IPE 400 girders, counting the holes the girder actually gains:

| value | marks |
|---|---|
| 0, 2, 4, 5 | **none** |
| 1, 3 | **2** |

⚠️ **Counting was too coarse to conclude anything, so the positions were compared** — and `1` and
`3` put their marks at **identical coordinates**: `dx 995 / 1005` (the two stiffeners, 10 mm
apart = the plate thickness), `dy 1.5 / 4.3`, to the 0.1 mm.

⇒ ⭐ **The ordinals collapse into two behaviours: none, and at-centre. No value in 0–5 reaches the
dialog's third option.** *"At Edges" is not reachable through `CenterPunchType` from this API
entry point* — a measured negative, not an unknown.

⇒ And the method matters as much as the answer: **two values producing the same COUNT is not two
values doing the same thing until the POSITIONS agree.** The count nearly closed this the wrong
way round.

### What was added to the model
Six labelled IPE 400 girders at x 102 000, one per swept ordinal — the sweep itself is the
evidence and is left in place.

---

## B.17 — Plate Connections ⭐⭐ **the company-connection database is real, on disk, and readable**

*notes 208 lines · the chapter whose own conclusion was "the manual IS the API documentation"*

### 1 · Was the chapter learned deeply?
**Yes.** The software's plate-type decision rule, the two dimension conventions, the
non-intuitive hole-layout semantics, the 132 haunch parameters, and — importantly — it correctly
identified **B.17.2's DAST selector as the value-engineering tool Amir described** *and then
refused to use it*, because load-based selection is **phase 2 and locked**. That restraint was
right and it stands: **the DAST item was not touched today either.**

### 2 · ⭐⭐ B.17.3 — the company connection database, closed

> *the manual:* *"you can create a database containing **user-defined plate connections**… **HINT:**
> create a database with frequently utilized and maybe **company-specific connections**, which are
> then **always available to all program users within your company**."*

The note left this as *"where the dBASE database sits, and whether `PsDBaseDatabase` reaches it"*.
**Both answered.** They sit in `Prg/Plugins/`, one per connection macro, and the class reads them.
New op **`dbase`**:

```
dbase file=…\Plugins\BasePlate\BasePlate.dbf
  -> records=56 fields=11
     SHAPE, CODE, LENGTH, WIDTH, THICKNESS, DIAMETER, WORKLOOSE, HOLEX, HOLEY, AF, AS
     row 0: 150UB14.0 | BBP 15 150UB14.0 | 180 | 160 | 8 | 16 | 2 | 2*100 | 1* | 5 | 5
```

| macro | records | fields |
|---|---:|---:|
| `AECChute` | 59 | 19 |
| `BasePlate` / `BasePlateChinese` | 56 | 11 |
| `BeamBeamClamp` | 5 | 2 |
| `PipeStrap` | 33 | 9 |
| `PurlinBeamBraceFly` | 60 | 19 |

⭐⭐ **And they speak B.14's language.** `HOLEX` reads **`2*100`** and `HOLEY` reads **`1*`** —
*the same drill-field layout string* `drillfield x= y=` takes. **The connection databases and the
drilling API are one vocabulary**, which is what makes encoding EB's own connections realistic.

⚠️ **`dbase` is READ ONLY on purpose.** `PsDBaseDatabase` exposes `PutRecord` and
`AppendNewRecord`; these files live in `Program Files` and define how connections get built.
Writing them is Amir's decision, not a side effect of an audit. Map:
`knowledge/CONNECTION-DATABASES.md`.

### 3 · `Rotate Connection` — exposed, writable, and inert

The note asked whether it is `PlateIsRotated`. **It is** — on `PsStandardPlateLinkData`. But the
only route to it was **`connset`, which is QUARANTINED** for having crashed AutoCAD four times and
once left the drawing unsaveable. **The quarantine stands**; instead the single boolean was added
to the *safe* `conn` op, which already writes nine template properties the same way.

```
conn kind=endplate … rotated=1   ->  create=True … PlateIsRotated=True
```

The property is **set and reads back True**. And the connection is **byte-identical**: both runs
produced the same eight plates — 140×130×10 ×2, 481.6×8×202.3, 10×65×138.3 ×2, 140×260×10 ×2,
300×290×10.

⇒ ⭐ **Another "parameter that never arrives"** — THE CEILING §2's signature, and consistent with
this chapter's own base-plate / end-plate / purlin finding. **Set, confirmed, ignored.**

### Still open — deliberately
* **B.17.2's DAST load-based connection selector — PHASE 2, LOCKED.** Knowing the tool exists is
  phase 1; using it to rule on a design is not, and it was not touched.
* Writing to the connection databases — reachable, and Amir's call.

---

## B.18 — Base Plates ⚠️ **two items left alone on instruction; the third is NOT closed, and says so**

*notes 125 lines*

### 1 · Was the chapter learned deeply?
**Yes.** It measured the column shortening and found *both* earlier accounts partial; it explained
**why the anchor length is "graphic only"** — a property of the software, not a choice of Amir's;
and it decoded `Use Dowel` correctly against the model: *"dowel elements are created as **volume
bodies**"* matches the `Ks_VolBody` anchors measured in Amir's own model, **Dübel = dowel**, and
`Data\Bolts\Duebel.mdb` is that database.

### 2 · Two items deliberately untouched

* **`Duebel.mdb`'s structure and how to name it as the `Input Field`** — ⏸️ **deferred by Amir**,
  verbatim: *"תעבור למחברי קורות, אל תתעכב על זה."* Recorded so it is not lost; **not chased.**
* **`Standard Definitions` — load-based base-plate selection.** 🔒 **Phase 2, locked.** Same line
  as B.17.2's DAST selector.

### 3 · The third item — measured, and it did NOT resolve

> *the note:* *"`connbase` returns `host_holes=0 anchors_with_body=0` even when it reports
> `create=True` — the holes in the column are a separate matter that needs checking."*

Run on a fresh HE 300 B with `template=default/Standard`:

```
connbase host=15CA added=1 check=1 create=True host_holes=0 anchors_with_body=103
         anchor_bbox=50x0x0
column holes after       : 0
plates found in the footprint (±600 mm of the column): 0
Ks_VolBody found in the footprint                    : 0
```

⚠️ **Three numbers that do not reconcile.** The op reports 103 anchors with bodies while an
independent scan of the same footprint finds **no plate and no volume body**, and
`anchor_bbox=50x0x0` is degenerate. At least one of these is measuring the wrong thing —
`anchors_with_body=103` looks like a model-wide count rather than this connection's.

⇒ **NOT CLOSED, and deliberately not written up as though it were.** The next step is specific:
**find out what `anchors_with_body` actually counts** before trusting any base-plate report, since
it is the number that would tell us whether the iron rule is satisfied.

⚠️ **And the iron-rule question behind it is still unanswered:** an anchor connects the base
**plate** to the concrete, so `host_holes=0` on the *column* may well be correct — a column is not
an element the anchor passes through. **The plate is the element that must carry the holes**, and
this run did not produce a plate the scan could find.

### Method note
This chapter is written as an **open item with a named next step**, not as a closure. Three
conclusions were withdrawn today because reasoning had been filed as a finding; a fourth will not
be added by rounding a confusing measurement up to an answer.

---

## B.19 — Web Angle ✅ **nothing to fix; the finding is a link that was never made**

*notes 226 lines · every claim measured*

### 1 · Was the chapter learned deeply?
**Yes, and it is unusually rigorous.** Three dead ends closed with evidence, the catalogue naming
trap found (`DIN WINKEL GLEICH` for lookup vs `DIN.DIN_WINK_GL` as stored — *"a key that reads
back one way is not the string you look it up with"*), and the load-driven selection correctly
identified as value-engineering material **and left alone as phase 2**.

⭐ **`SetKey` and `SetKatalog` are complete no-ops** — four connections, including one with no key
at all, every one produced `L90X9` @ `DIN.DIN_WINK_GL`. The section comes from the **template**.

### 2 · The one thing worth changing

Dead end #2 ends: *"there is a dedicated `PsCopeConnection` in the same namespace, **which is the
likely real route**."*

✅ **B.12 proved it** — from a template, support mandatory, `polyCuts` 0 → 1 — and it sits on THE
CEILING's workaround table. **Nothing ever came back to B.19 to say so.** A reader who lands here,
which is exactly where you land when a web angle will not cope, still reads *"likely"* and
rediscovers a solved problem.

⇒ ⭐ **Not a false claim — an un-propagated true one**, and the guard cannot see it: a stoplist
catches contradictions, not a *"likely"* that has since become certain. **Only reading the
neighbouring chapter does.** Linked in both directions now.

### Nothing was built
The chapter needs no new op. Its DAST half is untestable here (`get_PlateDataCount()=0`) **and**
phase-2 regardless, and that is the correct place to leave it.
