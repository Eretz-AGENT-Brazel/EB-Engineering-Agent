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

---

## B.20 — Shear Plates ⭐⭐⭐ **the part tells you what it is made of, and nothing was reading the field**

*notes 213 lines · band x = 130 000 · plugin v156 → **v160***

### 1 · Was the chapter learned deeply?
**Yes.** All six dialog pages, the entity-class switch behind `Poly-Plates`, the cut-type
difference from B.19 (`cutPlanes` vs `polyCuts`), the three templates with their DIN 7969
default, and the cope-template naming convention found by probing eight candidate names. It also
left three items **named with the test that would close them**, which is why they closed.

### 2 · `Turn Flat` — the named check, made, and it turned into something bigger

> *the note:* *"⚠️ **No property for `Turn Flat`.** … Dialog-only unless it hides behind
> `BoltType`-style indirection. **Worth checking the COM twin before concluding.**"*

**Checked. The COM twin is property-for-property identical** — `Ks_ComShearPlateLinkData` and
`PsShearPlateLinkDataMgd` differ only by COM's `GetData`/`SetData` blob accessors and .NET's
`UnmanagedObject` pointers. **There is no `Turn Flat` in either API.** A measured negative.

But the question underneath it — *what does this checkbox change about what gets ordered?* — is
answerable, and the answer was sitting in a field nothing was reading.

> ### ⭐⭐⭐ The part's own NAME states its mill product
>
> | `name` | `key` | `cat` | what it means |
> |---|---|---|---|
> | **`FL 150x10`** | `150X10` | `DIN.DIN_FLACH` | an entry in **`DIN FLACHEISEN`** — flat bar, stock |
> | **`BRFL 160x15`** | `160X15` | `DIN.DIN_FLACH` | an entry in **`DIN_BREITFLACHEISEN`** — wide flat, stock |
> | **`Plate 135x10`** | `135X10` | `DIN.DIN_FLACH` | ⚠️ **in neither catalogue — it will be cut from plate** |
>
> **`key` and `cat` are identical in all three cases and tell you nothing.** Only `name` does.

The two catalogues shipped here are disjoint, and both were enumerated rather than assumed:

```
DIN FLACHEISEN        419 names   widths 10,11,…,140,150                  (no 135)
DIN_BREITFLACHEISEN   338 names   widths 160,180,200,220,240,250,…,1200   (no 210)
```

**And the rule was proved by prediction, not by observation.** Three fresh bays, the derived
plate depth driven onto a stock FL width, a stock BRFL width, and a width that is stock in
neither — **each part's name stated in advance, before the connection was run**:

| `holevert` | derived depth | predicted | **measured** |
|---:|---:|---|---|
| 110 | 150 | `FL 150x10` | ✅ **`FL 150x10`** |
| 120 | 160 | `BRFL 160x10` | ✅ **`BRFL 160x10`** |
| 125 | **165** | `Plate 165x10` | ✅ **`Plate 165x10`** |

⭐ **165 is the probe that discriminates**: it lies *inside* the wide-flat width range and is not
a stock width. It got `Plate`. ⇒ **the test is catalogue MEMBERSHIP, not a size range.**

⇒ ⚠️ **The fabrication consequence, measured on the band as it already stood:** the shear plate
derives its depth from the bolt count — **135 for two rows, 210 for three** — and neither is
stock. Two of the four plates in B.20's own band are `Plate`, not bar. `Turn Flat` is exactly the
control that would swap which dimension gets tested, and **it cannot be reached from code.**
What *can* be reached is the hole geometry, and `name` is how you check the result.

⛔ **Not claimed:** what a parts list prints. The manual puts `Turn Flat` in ordering terms and
the object's name agrees with it, but the parts-list half is **part C** and was not run.

### 3 · `Position` — a whole capability that was read and never exercised

Every shipped template has `pos=0`. `PlatePosition` is a bare `Int32` with no declared enum, so
the ordinals were swept on six identical fresh bays — **count *and* position, because B.16 showed
that a count decides nothing**:

| `pos` | new objects | plate at | bolt |
|---|---:|---|---|
| **0** | 5 | **+9.30** from the web centre | M16 × 45 |
| **1** | 5 | **−9.30** | M16 × 45 |
| **2** | **6** | **+9.30 *and* −9.30** — a plate each side | **M16 × 55** |
| 3 · 4 · 5 | **0** | — | — |

⭐ **`pos=2` is `Both`, and the bolts grew by themselves: 45 → 55 mm.** Ten more millimetres of
packet, one step up the bolt table, no bolt parameter touched. *Bolts follow the packet.*

⚠️ **`Create()` returned `True` on 3, 4 and 5 and built nothing** — B.12's rule, a fourth time.

### 4 · `GetPlateId` — the measurement stands; what was inferred from it does not

The note's *"a method that does not work on either class"* is **true and stays**: `GetPlateId(i)`
returns 0 for every index, on every run, still. What was missing is that the plate is
**attributable anyway**, and the whole joint with it:

```
beam   15EE  type=17/kConnectWithSchearPlate    parts=1610,1611  bolts=1612…1615  target=15ED
column 15ED  type=12/kConnectedBy               (empty)                           target=15EE
plate  1610  type=18/kSchearPlateConnectionLink (empty)                           target=15EE
bolt   1612  NO LINK AT ALL
```

⇒ ⭐⭐ **The connected shape is the joint's owner** — the only member holding the roster and the
only one pointing at the support. Everything else holds a back-pointer. **A bolt carries no link,
so a bolt cannot be traced to its connection from the bolt's side.**

⇒ ⭐ **`LinkObjectCount` is always 2, and the two slots are the two sides of the web.** `pos=0`
fills slot 0, `pos=1` fills slot 1, `pos=2` fills both. The dialog's `left / right / Both` is
literally the shape of the data structure — and it was invisible until the empty slot was printed
as `-` instead of as `0`.

**And the settings ARE readable back — through one getter of two that look alike:**

| call | result |
|---|---|
| `PsLogicalLink.GetShearPlateLinkData()` on any joint member | ⛔ `PlateThickness=0` — reads nothing |
| **`PsShearPlateConnection.GetLink().GetLinkData(0)`** | ✅ `t=18 pos=2 nV=2 nH=2 dV=140 dia=22` — **exactly what was set** |

`GetLinkData(1)` and `(2)` are zeroed; index 0 is the live one. ⚠️ Only reachable straight after
`Create()` — `PsShearPlateConnection` has no binder to an existing joint, the same structural dead
end B.6 found in `PsGrid`.

### 5 · ⛔ AND THE OP SHIPPED A DRILLED, UNBOLTED JOINT — reporting `EB_OK`

The read-back test used `dia=22` and produced **two plates and not one bolt.** Isolated on four
fresh bays:

| | plates | bolts |
|---|---:|---:|
| `t=10 dia=16` | 2 | **4** ✅ |
| `t=10` **`dia=22`** | 2 | **0** ⛔ |
| `t=18 dia=16` | 2 | **4** ✅ |
| `t=18` **`dia=22`** | 2 | **0** ⛔ |

⇒ **The diameter is the killer; the thickness is irrelevant.** `HoleDiameter` names the *hole*;
the bolt comes from `BoltStyle`. A ⌀22 hole against the default `8.8S` has no bolt to match, and
the connection **drops the bolt instead of refusing.**

> ### ⛔⛔ This is B.15's ~400 failed bolts arriving through a connection class
> B.15: *"a grip that has no row in the bolt table fails silently."* Here it is a **diameter**
> that has no row in the **style** — and the product is precisely what iron rule 1 forbids:
> **holes with nothing through them.**
>
> ⚠️ **And my own op called it `EB_OK`, because its success test was "the census grew".** The same
> failure as B.9's `dumpmodel` and B.14's `plate9`: **a summary number standing in for the thing
> it was summarising.**

### 6 · What was fixed — plugin v156 → v160

| | |
|---|---|
| ⛔ **iron-rule guard** | `shearplate` now returns **`EB_ERR`** when it creates plates and zero bolts, and says why. Verified: fires on `dia=22`, silent on `dia=16` |
| **`connscan` printed raw 64-bit pointers** | `parts=140688488454768,0` → **handles**, empty slots as `-`. Unusable output made usable — **and it is what exposed the two-slot structure** |
| ⚠️ **`GetXxxLinkData() != null` is NOT a type test** | every getter returns a live object on every link, full of zeros. One shear-plate joint was tagged `t17/BASEPLATE/RIB/SPLICE/SHEARPLATE/WEBANGLE/COPE`. The type now comes from **`lk.Type`**, and a block of zeros is not printed at all |
| `SHEARPLATE[present]` ×3 | three words true of every link ever scanned. Replaced with real parameters |
| link type printed as a bare ordinal | now **`17/kConnectWithSchearPlate`** — read from the software instead of counted by hand |
| `shearplate` reports its own product | `own:[ links=1 [kConnectWithSchearPlate parts=… bolts=…] GetLink()=ok readback[0]: … ]` |

**This matters beyond B.20:** `connscan` is the instrument B.21 – B.26 are audited with.

### Model state
Band x = 130 000. **Seven joints kept, every one bolted** — checked with the fixed `connscan`,
not assumed: y 12 000 / 15 000 / 18 000 = the ordinal sweep · y 30 000 / 33 000 / 36 000 = the
naming prediction · y 39 000 = the link read-out. **Erased:** the three bays whose ordinal
produced nothing, and every y ≥ 42 000 strip — **three of which were drilled and unbolted.**
Census 1 115 → **1 087**, saved.

### Still open
* **What `Turn Flat` does to a parts list** — the object's name is measured; the list is part C.
* **`get_PlateDataCount() = 0`** — the load database is empty here as in B.19. Product-wide, and
  phase-2 regardless.
* **The cope** — unchanged from 09/08, still not reachable from this connection's link data.
  `PsCopeConnection` is the route and **B.12 proved it works**; linked now, as B.19 was.
* ⚠️ **`dia=` on the other connection ops has NOT been swept.** The bolt-drop was measured on
  `shearplate` alone. `webangle`, `splice`, `haunch` and `connbase` take the same parameter and
  **were not tested**, so this is a **task for their chapters, not a finding about them.**

---

## B.21 — Splice Joints ⛔⛔ **the chapter could not produce a bolted joint at all, and had not noticed**

*notes 183 lines · band x = 150 000 · plugin v160 → **v162** · `eb_api.dumpmodel` fixed*

### 1 · Was the chapter learned deeply?
**Yes, and its best finding is still its best finding** — *six checkboxes produce **eight** plates*,
because the web crosses the inner flange face and each inner plate becomes two strips. Nothing in
the manual says that. The dialog is fully mapped, the `Weld` variant was built, and `Ks_WeldFlag`
was correctly identified as a new entity class.

**But every measurement in it was taken on a joint with no bolts in it, and the chapter counted
plates and hole fields and never counted bolts.**

### 2 · ⛔⛔ `BoltStyleCRC = 0` — the connection drills and does not bolt

The fixed `connscan` from B.20 was pointed at B.21's own band as its first real use:

| bay | plates | **bolt slots** | hole fields on the beam |
|---|---:|---:|---:|
| y 0 `default/Standard` | 4 | **0** | **2** |
| y 3 000 `default/example2` | 8 | **0** | **2** |
| y 6 000 web only | 2 | **0** | **1** |
| y 9 000 welded | 4 | 32 | 0 |

⇒ **Three joints, drilled, with nothing through the holes.** Iron rule 1, in the reference model
since 09/08.

**The cause is in the templates, and it is total:**

```
[0] default/example2   … dia=16 workloose=2  boltCRC=0 …
[1] default/Standard   … dia=16 workloose=2  boltCRC=0 …
```

⇒ ⭐⭐⭐ **Both shipped templates carry NO BOLT STYLE.** And `PsSpliceJointLinkDataMgd` is the one
class of the three with **no `BoltStyle` string** — only the CRC — so there was no way to supply
one. The 09/08 note recorded the missing string as a curiosity. **It was the reason the chapter's
entire product was unbolted.**

**Fixed and measured on two fresh collinear pairs:**

| | new objects | plates | **real bolts** |
|---|---:|---:|---:|
| shipped, as it has always run | 4 | 4 | **0** ⛔ |
| **`boltstyle=DIN7990`** | **20** | 4 | **16** ✅ |

16 = 2×2 per flange × 2 + 2×2 per web × 2 — exactly the hole pattern that was already being
drilled. New op parameter **`splice boltstyle=<name>`** resolves the name through
`PsObjectStyleList` to the CRC the class wants (`DIN7990 → -1614854285`); `boltstylecrc=` takes a
raw value.

### 3 · ⚠️⚠️ A WELD FLAG OCCUPIES A BOLT SLOT — and it defeats a bolt-count guard

The welded bay reported `bolts=32`. Reading those 32 objects: **every one sits on layer
`PS_Weld`.** They are `Ks_WeldFlag`.

> ### ⭐⭐ `getBoltObjectId` / `BoltObjectCount` do not mean bolts. They mean **fasteners**.
> A link reporting **32 bolts can hold zero bolts** — which is the exact shape of an iron-rule
> violation that **passes** a bolt-count check.
>
> ⚠️ **This weakens the guard I shipped in v160 one chapter earlier.** It counted link slots, so a
> weld flag would have satisfied it. Corrected in v161: `CountRealBolts` opens each fastener and
> classifies it, and the splice guard exempts a deliberately welded joint rather than a boltless
> one.

### 4 · ⚠️ And the same failure class again — this time in the PYTHON client

Chasing "how many bolts are actually in that band" gave **0**, which happened to be right. Then it
gave 0 for the fixed band too.

```
every plate  (241) reports InsertPoint = 0,0,0
every bolt   (305) reports InsertPoint = 0,0,0
```

B.9's audit fixed the **plugin** to emit a world bounding box for exactly this reason.
**`eb_api.dumpmodel()` never parsed it.** So `els[i]['insert']` located every plate and every bolt
at the origin, and *any* "what is in this region" question answered **zero bolts, everywhere in
the model** — a false negative indistinguishable from a true one.

**Fixed, and the contrast measured on the same data:**

```
bolts found via insert[] : 0
bolts found via center[] : 62      (y15000:16  y18000:16  y21000:16  y24000:8  y-6000:6)
```

⇒ ⭐ **B.9's lesson had been applied to the producer and not to the consumer.** A fix that stops
at the file format is half a fix.

### 5 · ✅ `TopPlateLap` / `SidePlateLap` — an INFERRED mapping, now measured

The note marked these *(inferred — "lap" is an overlap length)*. B.11's rule says an inference
filed beside measurements is a **to-do**. Done, as a prediction: a welded splice at
`toplap=100 sidelap=100` must give **200 mm** plates.

```
predicted  200
measured   176F FL 150x10  200 x 150 x 10      1781 Plate 128x10  200 x 128 x 10
           1778 FL 150x10  200 x 150 x 10      1782 Plate 128x10  200 x 128 x 10
```

⇒ ⭐ **The lap is a HALF-length: plate length = 2 × lap**, the overlap onto each member, and the
flange and web plates each obey their own value. A **bolted** splice ignores it — those plates
measure 298, set by the bolt pattern.

### 6 · ⭐ B.20's naming rule pays off one chapter later

Read straight off the rebuilt band:

| part | name | stock? |
|---|---|---|
| flange plates | **`FL 150x10`** | ✅ a real `DIN FLACHEISEN` entry |
| web plates | **`Plate 128x10`** | ⚠️ **no** |
| inner flange strips | **`Plate 39x10`** | ⚠️ **no** |

⇒ The web plate's width comes from the web depth less the flange thicknesses, so it lands on a
stock width **only by accident**. In this band it does not. *(What a parts list does with it is
part C and untested — as in B.20.)*

### 7 · `GetPlateId` — third class, same zero
Returns nothing here too. The logical-link route from B.20 was propagated into `splice`, and it
reports `parts= boltSlots= realBolts= weldFlags=` per link.

### Model state
Band x = 150 000. **Every joint left is either bolted or deliberately welded:**
y 9 000 welded (32 weld flags) · y 15 000 the `DIN7990` proof (16 bolts) · y 18 000 `Standard`
4 plates / 16 bolts · y 21 000 `example2` 8 plates / 16 bolts · y 24 000 web-only 2 plates /
8 bolts · y 27 000 the lap test (welded, 200 mm plates).
**Erased:** the three drilled-and-unbolted originals at y 0 / 3 000 / 6 000 — every demonstration
they carried was rebuilt bolted — and the two defect probes at y 12 000. Census **1 189**, saved.

### Still open
* **`Diagonal`** (`WeldDiagonal`) and **`Single Side`** — read, mapped, **never exercised**. The
  `Single Side` → `WeldToSupportShape` mapping is still marked *inferred* and stays marked.
* ⚠️ **The transversal `Dist. Between` dual meaning** — the manual says it is a spacing at two
  bolts and an outer-to-outer span at three or more. **Quoted, not measured.** The test is B.14's:
  build at 2 and at 3 and compare the **gaps**, never the counts.
* **`get_PlateDataCount() = 0`** — third of three. Product-wide, phase-2.
* ⚠️ **Whether the other connection classes also ship with `BoltStyleCRC = 0`.** `webangle`,
  `haunch` and `connbase` were **not** checked. A **task** for their chapters — and after this
  chapter it is the first thing to check in each.

---

## B.22 — Purlin Connection ⭐⭐⭐ **the chapter's one unexplained item is solved, and the cause is geometric**

*notes 190 lines · band x = 250 000 · no plugin change needed*

### 1 · Was the chapter learned deeply?
**Yes — it is one of the two or three strongest notes in part B.** `PurlinType` measured rather
than inferred from declaration order, the template-name-does-not-set-the-type trap, all four
products confirmed by entity class, the `Default/` vs `default/` case difference, `Create()`
returning `False` on all seven successes, and the `WeldToSupportShape=False` cleat-attached-to-
nothing defect that matches what Amir caught by eye in B.19.

It left exactly one thing open, and it left it honestly: *"Cause not established."*

### 2 · ⭐⭐⭐ `kBoltet` drills four and bolts two — solved

The note ruled out geometry and grip length and stopped. **What it never did was compare
POSITIONS** — B.16's rule, unapplied here. Done:

```
girder C7A   (249964,1472,186.5)->200   (249964,1528,186.5)->200
             (250036,1472,186.5)->200   (250036,1528,186.5)->200
purlin C7B   (249964,1472,200)->360   depth 160.00
             (249964,1528,200)->208.82 depth   8.82
purlin C7C   (250036,1472,200)->360   depth 160.00
             (250036,1528,200)->208.82 depth   8.82
```

⇒ ⚠️ **First: the note's own table was a miscount.** It reads *"girder holes 4 · purlin holes 2
each · bolts 2"* as though the girder were over-drilled. **The holes are 4 and 4 and every one is
exactly coaxial.** Nothing is over-drilled. The imbalance is entirely in **which of the four
matched positions can take a bolt.**

⇒ ⭐⭐⭐ **And the depths say why.** The U160 stands with its 160 mm depth vertical on the girder's
top flange. `HoleDistanceSupport = 56` puts the pair at ±28 from the purlin axis, and at **−28 the
drill line falls on the channel's WEB stood on edge** — 160 mm in the drill direction, and a bolt
there would clamp the width of a plate seen edge-on. **At +28 it lands on the bottom FLANGE,
8.82 mm.** That is the one you bolt a channel down through, and that is the one that gets a bolt.

**The drill field is applied across the purlin without regard to where the section's material is.**

### 3 · The fix, measured two ways

| setting | girder holes | **bolts** | purlin hole depths | |
|---|---:|---:|---|---|
| `nSup=2 dSup=56` *(shipped)* | 4 | **2** | **160.00** + 8.82 | ⛔ two holes on the web |
| **`HoleCountSupport=1`** | 2 | **2** | **11.06** | ✅ balanced, on the axis |
| **`HoleDistanceSupport=20`** | 4 | **4** | **11.86** and **10.26** | ✅ **balanced, both on the flange** |
| `HoleDistanceSupport=-56` | 4 | 2 | 160.00 + 8.82 | ⚠️ **the negative was silently normalised to +56** |

⇒ ⭐ **`HoleDistanceSupport = 20` is a fully balanced two-bolt purlin detail on a U160.** The
shipped 56 straddles the web.

⚠️ **The depths also measure the DIN channel's taper**: 11.86 at −10, 11.06 on the axis, 10.26 at
+10, 8.82 at +28. The flange thins outward, and the hole depth follows it exactly.

### 4 · ⚠️ Two traps found while fixing it, both about cleanup rather than about purlins

> **① Erasing a part does NOT erase its bolts.** Deleting purlins `C7B`/`C7C` left `C8E` and `C8F`
> standing at the old gauge — **bolts connecting nothing**, which is iron rule 1 from the other
> side. Found only because the station then counted **6** bolts where 4 were expected.
> ⇒ **After erasing any bolted part, sweep for orphan bolts.**

> **② A parameter that was never sent is not a parameter that does nothing.** The first sweep
> built three labelled cases and forgot the `set=` — three identical results that read exactly
> like *"`HoleCountSupport` has no effect."* The op echoes what it applied; **read the echo.**
> This is the same shape as B.16's count trap, one step earlier in the chain.

### 5 · B.21's task, answered here rather than assumed

B.21 handed every remaining connection chapter one question: **what bolt style does the template
carry?** For the purlin class, measured:

```
Default/Standard              BoltStyle=8.8S     CRC=-401163854
Default/Example-Purlinshoe    BoltStyle=DIN7990  CRC=-1614854285
Default/Example-Purlinshape   BoltStyle=DIN7990  CRC=-1614854285
```

⇒ ✅ **All three carry a real style.** B.21's `BoltStyleCRC = 0` defect is **specific to the splice
class**, not a product-wide pattern — a measured negative that narrows the concern for B.23–B.26.

### Model state
Band x = 250 000. **The `kBoltet` demonstration at y = 1 500 was rebuilt with the balanced gauge**
— 4 holes, 4 bolts, both hole lines on the flange — and the orphaned hole field its predecessor
left on the girder was removed (`killholefield`, field identified by deleting and reading the
holes back, not by guessing: the two live stations at y 3 368 and 7 448 survived untouched).
The two fix demonstrations are kept at y 21 000 (`HoleCountSupport=1`) and y 23 500
(`HoleDistanceSupport=20`). Everything else built for the sweep — four stations carrying orphan
holes, plus their throwaway girder — was erased. Census **1 201**, saved.

### Still open
* ⚠️ **Whether a channel's web line is always at `−` from the axis.** Measured on U160 in this
  build only; the orientation may follow the purlin's insertion direction. **Do not generalise the
  sign** — measure the depths and take the shallow line.
* **The connection database** the whole chapter is built around — *"please refer to the technical
  supplement or ask your ProSteel dealer."* Not present on this installation, and building one is
  Amir's call.
* `kCleat` still has **no shipped template**; the `WeldToSupportShape=False` cleat defect stands as
  recorded — **always set it explicitly.**

---

## B.23 — Gusset Plates ⭐⭐⭐ **the chapter is right about gussets, and it uncovered the binder every other chapter had been looking for in the wrong place**

*notes 199 lines · band x = 170 000 · plugin v162 → **v165** · one CEILING entry retracted, one
new LETHAL call*

### 1 · Was the chapter learned deeply?
**Yes, and it is the chapter that did the most with the least.** Its central command has no API
route at all, so Amir had it built from primitives instead — two complete connections, an angle
node and a shaped-gusset SHS/channel node, every dimension measured, two mistakes caught by
measuring, and a collision check run as proof rather than asserted.

Its verdict — *"a gusset plate can be **read and edited** from code, but **not created**"* — was
half measured. The **not created** half was: `PsGussetConnection` has no `Create`, no `insert`, no
`SetConnectionObjectId`. **The read-and-edit half was inference from a property list**, and nothing
had ever bound the class to anything.

### 2 · ⭐⭐⭐ `PsTransaction.GetObject` — 57 overloads, used nowhere

Looking for the gusset's read route turned up one line:

```
Bentley.ProStructures.Drawing.PsTransaction
   M  Boolean GetObject(Int64 Id, PsOpenMode Mode, PsGussetConnection& entObject)
```

…and **fifty-six more overloads beside it** — `PsGrid`, `PsEditConnection`, `PsWeldFlag`,
`PsPositionFlag`, `PsBoltStyle`, `PsShape`, `PsPlate`, `PsBolt`, `PsAssembly`, `PsBracing`,
`PsPortalFrame`, `PsStairs`, `PsJoist`, `PsTruss`, `PsHandrail`, `PsLadder`, `PsWorkframe`,
`PsBendPlate`, `PsBendShape`, `PsArcPlate`…

> ### ⭐⭐ THE BINDER IS NOT ON THE CLASS. IT IS ON THE TRANSACTION.
> Every chapter that asked *"can I bind this class to an existing object?"* asked it of the
> **class** — does `PsGrid` have `SetObjectId`, does `PsShearPlateConnection` have `readFrom`.
> The answer was always no, and the question was always in the wrong place.
> **Nothing in the plugin had ever used `PsTransaction`.**

**New op `bind`, and it works** — measured on objects whose identity was already known:

```
2F1  Ks_Grid   grid=True  [name='A' len=24000 wide=15000 type=kRectangle lenDiv=4 wideDiv=3 …]
456  Ks_Plate  plate=True [name='B9_RECT 400x250x12' L=400 H=12 verts=5 rect=True]
2C6  Ks_Shape  shape=True [key='HE300B' cat='DIN.DIN_HEB']
```

### 3 · 🛑 B.6's CEILING entry is retracted

B.6's audit recorded, and put on THE CEILING: *"`PsGrid` cannot bind to an existing frame — no
`SetObjectId`, no `readFrom`, **no binder of any kind**… the two halves never meet in the API."*

**Grid `2F1` is a real `Ks_Grid` and it bound on the first attempt, with correct values.**
The claim is withdrawn in the B.6 notes, on THE CEILING, and in `qc/retracted.tsv` in all three
wordings it was written in.

### 4 · ⛔⛔ And the meeting point is LETHAL — a third entry on the list

With the grid bound, `addUserXaxis` was called on it. **AutoCAD died** — `EB_TIMEOUT`,
*"AutoCAD Error Report"*. Then, following the protocol exactly: model saved immediately before,
**one call, its own run**, `probe=addx` running `addUserXaxis` and nothing else.

**Dead again.**

⇒ **`PsGrid.addUserXaxis` on a `PsTransaction`-bound grid is the third lethal call**, after
`PsPlate.computeObjectWeigth` and `PsVolume.checkHoleEdgeDistance`.

⚠️ **And it is a different shape from the first two.** They were `check*`/`compute*` methods; this
is an ordinary mutator. What they share is the route — a managed wrapper into native code that
expects a context the caller has not established.

⇒ ⭐ **Reading a bound object is safe** (grid, plate, shape, all read back correctly, repeatedly).
**Writing through one killed the session on the first attempt.** Every mutator on a bound object
is now suspect, and `getUserXaxis`/`getUserYaxis` are **UNKNOWN, not safe** — the add died before
they were reached.

⇒ **B.6.7 stays closed, for a far better reason than before:** not *"the halves never meet"*, but
*"they meet, and the meeting point kills the session."*

### 5 · ⛔⛔ `GetObject` does NOT type-check — and that is worse than a crash

Asked for a `Ks_Shape` as a `PsGrid`, it returned **`True`** and handed back a reinterpreted
pointer:

```
bind 2C6 cls=grid -> grid=True [name='HE 300 B' len=281474976713490 wide=NaN
                                lenDiv=0 wideDiv=0 xDesc=234 yDesc=140]
```

A **read** gives nonsense that looks like data. A **write** through that handle would corrupt the
object. ⇒ `bind` now reads the entity's real class first and **refuses** a mismatch, quoting the
measurement.

### 6 · The gusset itself — the chapter's own verdict stands and is now complete
* **Not creatable:** unchanged. `PsGussetConnection` has no creator, and there is no COM twin.
* **Readable and editable:** now **measured, not inferred** — `bind cls=gusset` is the route, via
  `PsTransaction`. ⚠️ There is no `kGussetPlate` object in this model to bind (the band's gussets
  are hand-built `Ks_Plate`), so the route is proven on its siblings and **not yet on a gusset**.
  Named as a task, not claimed as a finding.
* ⭐ The indirect route — **a bracing connection produces gusset plates, and `PsBracing.insert()`
  exists** — is untouched here and belongs to **B.24**, which is next. The `LogicalLinkType` enum
  supports it: `kBracingPlate`, `kBracingLasche`, `kBracingConnect`, `kBracingFiller`.

### Model state
**Nothing was built and nothing was changed.** Two crashes, both recovered with nothing lost —
the model had been saved immediately before each. Census **1 203**, matching the pre-crash state.

⭐ **The recovery procedure itself was corrected**, at the cost of a wasted restart: launching with
`/t <template>` creates a new `Drawing1`, which makes ProStructures raise a modal **Measurement
Unit** prompt that **cannot be dismissed from code** (`BM_CLICK` and `WM_COMMAND`/`BN_CLICKED`
both ignored, six attempts). **Pass the DWG path on the command line instead** — opening an
existing drawing never asks, and no second document is created.

⚠️ **And `Get-Process acad` is not the test.** The second crash left the process alive and
`Responding=True` while the op never returned. **`modal_dialogs()` is the test.**

### Still open
* **Binding an actual `kGussetPlate`** — no such object exists in this model yet.
* **Which other mutators on bound objects are lethal** — `addUserYaxis`, `deleteUserXaxisAt`,
  `setAxisLength`, `createAxisDescriptions`, and `getUserXaxis`/`getUserYaxis` whose status is
  genuinely unknown. **Each costs a crash to find out; none is worth it without a reason.**
* `*2`-style distances (a multiple of the **bolt diameter**, not a repeat count) — dialog grammar,
  and with no creation route there is nothing to send it to.

---

## B.24 — Dynamic Bracing ⭐⭐ **the verdict stands; the REASON was wrong, and the real one is measurable**

*notes 360 lines (ratio 1.02) · band x = 190 000 · plugin v165 → **v168***

### 1 · Was the chapter learned deeply?
**Yes — it is the most complete chapter note in part B**, and it did the hardest thing: it went
looking for a second route when the first failed, found the **62 `PSN_*` macro assemblies** nobody
had touched, drove one of them, and recorded that ENTER — not ESC — clears a parked macro prompt.
It also caught the `flange=` drilling trap by reading coordinates when the counts were perfect.

Its closure was: *"bracing cannot be created from code in this product, **by design**. Both routes
lead to a pick."*

### 2 · ⭐⭐ The verdict is right. The reason is not — the system line never arrives

The six 09/08 configurations all reported `insert()=False`. **None of them read the system line
back.** The op now does, and it changes the picture completely:

```
cross=0, single bar          insert()=False   line1=(NaN,NaN,NaN)->(NaN,NaN,NaN)   line2=(0,0,0)->(0,0,0)
cross=1 + second line        insert()=False   line1=(NaN,…)                        line2=(NaN,…)
cross=1 + line + noGussets   insert()=False   line1=(NaN,…)                        line2=(NaN,…)
cross=0 + welded             insert()=False   line1=(NaN,…)                        line2=(0,0,0)->(0,0,0)
```

> ### ⭐⭐⭐ Read the two `line2` values against each other
> **Untouched, it reads a clean `(0,0,0)`. Set, it reads `NaN`.**
> The getter works. **Setting the geometry is what produces the garbage.**

⇒ **`insert()` has never once been given a system line.** It was not refusing a well-formed
request; it was refusing a bracing with no line. The old note read the refusal as *"the product is
interactive by design"* — the interactivity finding is true of the **macro** route and was proved
there, but for `PsBracing` it was an inference from a `False` whose cause had not been read back.

⚠️ **And `crossedMode` on the same object round-trips perfectly** (`False`/`True` as set), as do
`cat`, `size` and `shapeType` — which the 09/08 note had already verified. So it is specifically
**the `PsPoint` setters** that fail, on an object whose every other setter works.

### 3 · Four hypotheses tested and excluded

| hypothesis | test | result |
|---|---|---|
| the **cross system line** was never supplied (a cross stay has TWO diagonals, and `setCrossStartPoint`/`setCrossEndPoint` were **not exposed by the op at all**) | exposed them, supplied the second diagonal | ❌ still `NaN`, still `False` |
| the `PsPoint` was **garbage-collected** before the native side read it (B.9's dead-handle trap) | held every point in a local and added `GC.KeepAlive` past `insert()` | ❌ unchanged |
| the setters accept only a **PsPoint the API itself issued** | `ptmode=api` — read the object's own point out, mutate `x`/`y`/`z` in place, write it back | ❌ unchanged |
| a **single bar** avoids whatever the cross geometry needs | `cross=0`, and `welded=1` (no borings), and `nogussets=1` | ❌ unchanged |

**Ten configurations now: six from 09/08 and four today.** All `insert()=False`, and now with the
cause localised to a specific family of setters rather than left as a whole-command refusal.

### 4 · ⚠️ What is NOT established, stated plainly

Two readings fit the data and **this audit did not separate them**:

* **the setter stores nothing usable**, or
* **the setter stores correctly and the getter cannot read it back after a write.**

The clean `(0,0,0)` on the untouched line proves the getter *can* return a real value; it does not
prove it can return one *after a set*. ⇒ **A task, not a finding.** The next test is
`listInformation()`, which prints the object's own state to the command line — a third channel,
independent of both the setter and the getter, and `app/eb_log.py` already exists to capture what
ProSteel writes there.

⇒ Either way **B.24's practical closure is unchanged and now better founded:** a bracing cannot be
built from code. `PSN_HollowShapeBracing` stops at *"Choose support shape"*, and `PsBracing` cannot
be told where the bracing goes.

### 5 · What this means for B.23, restated
B.23's gusset is produced *by* the bracing command (*"the entire bracing **including gusset plate**
is generated"*). Both routes to a bracing are closed, so the gusset stays hand-built —
**unchanged.** ⚠️ But the *reason* now differs between the two routes and should not be blurred:
the macro is **interactive**; `PsBracing` **cannot receive its geometry**. Two different walls.

### 6 · What was added to the op
`crossp1=` / `crossp2=` (the second system line — B.24's whole `Cross Bracing` field, previously
unreachable), `ptmode=new|api`, and a **read-back of both system lines and `crossedMode` printed
before `insert()` decides**. That read-back is the instrument that produced this section, and it
is the thing the 09/08 run lacked.

### Model state
**Nothing built, nothing changed.** Ten `insert()` attempts, census **1 203 → 1 203** throughout,
saved. The 09/08 host frames (welded `HE200B`+`IPE300`, bolted `UC203`+`UB305`) are untouched.

### Still open
* ⚠️ **Setter or getter** — see §4. `listInformation()` is the named next test.
* The `PSN_*` macros stay off-limits unattended. ⭐ **ENTER, not ESC**, clears a parked prompt —
  unchanged and still the most operationally useful line in this chapter.
* **Bracing catalogues** (dBASE files for the three rod types) — read in the manual, never located
  on disk. B.17.3's `dbase` op could read them if the files were found.

---

## B.25 — Static Bracing ⭐⭐⭐ **the 31 mm gap is diagnosed: B.23's own seating rule, written the same day and not applied**

*notes 150 lines · band x = 310 000 · no plugin change needed*

### 1 · Was the chapter learned deeply?
**Yes, and it produced the best single piece of fabrication knowledge in part B:**
⭐ *"specify by which value the bracing rod has to be **shortened** after its insertion. **Thus,
the rod will be kept in tension**."* Nothing in the geometry hints at it. It also found the one
class that serves both chapters (`setDynamicStatus`), read the six separable buttons as an
instruction to **compose**, and then composed the bay by hand.

### 2 · ⚠️ CORRECTED — nothing was refuted, because nothing was tested

The note reads: *"Both leads this chapter suggested — the static flag, and the corrected XZ plane —
were **refuted**."* Seven configurations, all `insert()=False`.

**B.24's audit found that `PsBracing` never receives its system line at all.** Re-run here with
`dynamic=0`, the static case, with the line now read back:

```
static bracing, XZ plane   insert()=False   line1=(NaN,NaN,NaN)->(NaN,NaN,NaN)
```

⇒ **The static configuration fed the creator the same NaN as the dynamic one.** *"Refuted"* is
too strong: a lead is not refuted by a test that could not have succeeded whatever the lead's
merit. ⇒ **`insert()` remains unusable — thirteen configurations agree — but the static flag and
the XZ plane are back to UNTESTED, not disproved.**

⭐ The chapter's own explanation of the UCS still stands and is still good:
**`insert(Origin, X_axis, Y_axis)` takes the plane as arguments**, so the active UCS was never
going to matter.

### 3 · ⭐⭐⭐ The 31 mm gap — measured, systematic, and now explained

The handoff carries it as ⏳ *awaiting Amir's decision*. It is measured here and **not touched**.

**It is not one bolt. It is all eight:**

```
verdict         bolt name                nominal packet spare gap klemm holes owners
GAP-IN-PACKET   F82  M 16x75 Mu DIN7990    75     19     56   31   50    2   F72:10,F76:9
GAP-IN-PACKET   F83  …                     75     19     56   31   50    2   F72:10,F76:9
GAP-IN-PACKET   F84 F85                    75     19     56   31   50    2   F75:10,F76:9
GAP-IN-PACKET   F86 F87                    75     19     56   31   50    2   F74:10,F77:9
GAP-IN-PACKET   F88 F89                    75     19     56   31   50    2   F73:10,F77:9
```

**Eight bolts, four joints, 31 mm of air in every one.** `BOLT-NO-HOLE=0`, `OVERSIZED=0`,
`SHORT=0` — the iron rule itself is satisfied and the bolt lengths are right for the packet as
modelled. The fault is the packet.

**And the geometry names the cause:**

```
gusset F72   y  −55.0 … −65.0     a 10 mm plate, centred on y = −60
rod    F76   y −105.0 … −15.0     an L 90x9 -- a 90 mm ENVELOPE, centred on y = −60
             its drilled leg:  y −96 … −105
                               −65 → −96  =  31 mm of air
```

⇒ ⭐⭐ **The rod's AXIS was put at ±60 to match the gusset. But an angle's material is at the
edges of its envelope, not at its axis** — the drilled leg sits 96 mm out, not 60.

> ### 🛑 And B.23 had already written the rule down, on the same day
> B.23's hand-built node records: *"⭐ **The seating rule:** an `L90x9`'s envelope is 90 wide
> **centred on its axis**. To land its face on a 12 mm gusset straddling y = 0, the axis goes to
> **y = 6 + 45 = 51**."* — i.e. **axis = gusset face + 45**.
>
> Applied here: the gusset face is at **−65**, so the rod axis belongs at **−110**, not −60.
> **The rule was derived on 09/08 and not applied to a bay built on 09/08.**
>
> ⇒ ⭐ **Third instance of the same pattern this audit keeps finding** — after B.19's *"likely"*
> that had become certain, and B.24's NaN. **A true finding that does not travel is worth
> nothing**, and the guard cannot see it: a stoplist catches contradictions, not silence.

### 4 · ⏳ NOT FIXED — and this is the right place to stop

Moving a rod in a braced bay is a **scheme** change. Two ways out, both Amir's:

| | |
|---|---|
| **move the rods** to axis ±110 so each leg face seats on its gusset | changes the stagger, and the stagger is what keeps the two diagonals from colliding — the crossing clearance would have to be re-checked |
| **pack the joint** with a 31 mm packer at each of the four ends | keeps the scheme, adds eight parts and a fabrication operation |

⚠️ The one thing that is **not** an option is leaving it: a bolted lap with an air gap puts the
bolt in bending and cannot be fabricated as drawn.

⇒ **The bay is left exactly as found**, and `vfy_fit` reports it in one line for whenever Amir
wants to decide.

### 5 · What the chapter got right and keeps
* ⭐ **Compose, don't wait for the command.** The manual says so outright and the bay proves it:
  17 objects — 3 frame shapes, 2 rods, 4 gussets, 8 bolts — every one from a call that works.
* ⭐ **Holes measured inward from the rod's front edge**, never spread about the joint centre.
  The first pass did the latter and produced an unfilled gusset hole — the same defect `kBoltet`
  produces in B.22.
* ⭐ `Weld Bracing` is the one legitimate no-hole case: it suppresses drilling **because there are
  no bolts**, while still sizing the gussets *as if* there were. Not an exception to the iron rule.

### Model state
**Nothing built, nothing changed.** One extra `insert()` attempt (still `False`), census
**1 203**. The bay at x = 310 000 is untouched, including the gap.

### Still open
* ⏳ **The 31 mm gap — Amir's decision**, now with the arithmetic and two costed options.
* ⚠️ **The static flag and the XZ plane are UNTESTED again**, not refuted. They can only be
  retested once `PsBracing` can be given a system line — which is B.24's open item.
* `Resolution` and `Shape Class` on the Shape Definition page — read, never exercised.

---

## B.26 — Haunches ⭐⭐⭐ **the placement failure is named exactly — and the audit found twelve orphan bolts of its own making**

*notes 183 lines · bands x = 330 000 (welded) and 346 000 (bolted) · plugin v168 → **v169***

### 1 · Was the chapter learned deeply?
**Yes, and it is the chapter Amir corrected three times** — each correction producing a rule that
outlives it: a hole depth is a **section-orientation probe** (an 11 mm hole means you drilled the
web); **holes cannot be removed, so probe on a throwaway member**; and **deleting a connection's
parts does not delete the connection**. It also carries the blast guard that catches the shipped
template's zero plane before it stretches a rafter 300 m across the drawing.

Its one abandoned item: *"the haunch builds at the **support's origin** … `SetConnectionPoint` and
`InsertPoint` did not move them. **Not pursued further.**"*

### 2 · ⭐ First: `InsertPoint` round-trips. This is not B.24's failure.

B.24 had just found that `PsBracing`'s `PsPoint` **setters store garbage** — the value never
arrives. The obvious question was whether the haunch fails the same way. It does not:

```
haunch … ex=1,0,0 ey=0,0,1 at=400000,20000,5000
  [readback X=1,0,0  Y=0,0,1  InsertPoint=400000,20000,5000]
```

**Exactly what was set.** ⇒ ⭐⭐ **Two failures that look identical from outside are different
underneath:** `PsBracing` never receives its geometry; `PsHaunchLinkDataMgd` receives it and
**ignores it** — THE CEILING §2's *"parameter that never arrives"*, alongside B.17's
`PlateIsRotated` and B.26's own `nv`/`dv`/`nh`/`dh`.

### 3 · ⭐⭐⭐ And *"the support's origin"* is wrong. It is the WORLD Z ORIGIN.

Five configurations, each on its own throwaway column-and-rafter pair:

| what was tried | eaves at | plates landed at |
|---|---|---|
| world `at`, column z 0…5000 | 5 000 | **z = 0** |
| **local** `at = 0,0,5000` | 5 000 | **z = 0** |
| **local** `at = 0,5000,0` | 5 000 | **z = 0** |
| **column drawn TOP-DOWN** — origin at z = 5 000 | 5 000 | **z = 0** |
| **column based at z = 2 000**, eaves at 7 000 | 7 000 | **z = 0** |

⇒ The fourth and fifth kill the recorded explanation outright. With the column drawn top-down its
origin **is** the eaves, and with the column based at 2 000 its origin is 2 000 — **and the parts
still land at zero.**

⇒ ⭐⭐ **The haunch's parts are pinned to z = 0 absolutely. `x` and `y` are correct.** Only the
height is lost. *(And the top-down test is the one B.26 itself prepared and never ran:*
*"Drawing the column top-down was prepared as a test but the frames were built explicitly
instead."* **It was run. It does not help.**)

⚠️ **And the connection demonstrably knows where the joint is** — it trimmed the rafter at the
eaves every single time, `3162.278 → 2385.67`, on all five. **The cut is placed correctly and the
parts are not.** That is a far more specific fact than *"the parts appeared at the column's base"*,
and it is what makes the workaround findable: **build the corner with the eaves at z = 0.**

### 4 · ⛔⛔ TWELVE ORPHAN BOLTS — and this audit put them there

A model-wide `vfy_fit` returned **`BOLT-NO-HOLE=12`** — the iron rule, twelve times. They are at
x ≈ 130 200, y = 44 987 / 50 985 / 56 987: **the B.20 shear-plate isolation bays**, whose plates
and beams were erased four chapters ago.

> ### ⭐⭐ Two of this audit's own findings, combining into the defect
> B.20's cleanup erased *"everything in the band with y ≥ 41 000"* and filtered bolts by
> `els[i]['insert'][0]`. **B.21 later measured that `insert` is `(0,0,0)` for every plate and
> every bolt in the model** — so that filter matched **no bolt at all**, silently, and the sweep
> reported success. B.22 then found that **erasing a part does not erase its bolts.**
>
> ⇒ **The B.20 sweep could not have worked, and the fix that reveals it was found one chapter
> later.** Nothing detected it until a whole-model iron-rule check was run — which is the argument
> for running one.

**Erased. `BOLT-NO-HOLE` 12 → 0.** Census 1 205 → **1 193**, saved.

⇒ ⭐ **Rule: after any sweep that erases parts, re-run `vfy_fit` over the WHOLE model, not the
band.** A band-local check cannot see what a band-local filter missed.

### 5 · ⏳ The twelve unassemblable bolts — measured, not touched

Amir's decision is pending and `PsBolt.RecomputeBoltLength()` was **not** fired.

```
SHORT  11A6…11AB   M 20x70 DIN6914   nominal 70  packet 79  spare −9   4 holes
SHORT  11BC…11C1   M 20x70 DIN6914   nominal 70  packet 59  spare 11   3 holes
```

**Six are shorter than the packet they pass through** (spare −9: the bolt does not reach) and
**six leave 11 mm** where a nut and washer need 22–31. Exactly as recorded.

⚠️ **And the whole-model sweep found twenty more like them**, not previously listed:
`SHORT=32` in total — e.g. `152E…1533` and `1547…` at **spare 3**, in the B.12/B.19 bands.
⇒ **The pending decision is bigger than twelve.** Reported, untouched.

### Model state
**Whole-model verification, the first in this audit:**

```
bolts=301  OK=261  BOLT-NO-HOLE=0 🧲  OVERSIZED=0  GAP-IN-PACKET=8  SHORT=32
```

`BOLT-NO-HOLE=0` — **the iron rule is clean across the entire model.** The eight
`GAP-IN-PACKET` are B.25's braced bay and the 32 `SHORT` are the pending bolt-length decision;
both are Amir's and both are left exactly as found. Six throwaway haunch probes at x = 400 000
erased. Census **1 193**, saved.

### Still open
* ⛔ **Haunch placement.** The parts are pinned to z = 0 and `InsertPoint` is ignored. The only
  route left is to **build the corner at z = 0 and move the assembly** — untested, and it is a
  workaround rather than a fix.
* `IsCopedShape` — *"the haunch is not made from individual plates but from a **coped shape**"* —
  read, mapped, **never exercised**.
* `StiffenerAtSupport` / `StiffenerAtConnected` take **B.16's stiffener templates**. The link is
  recorded in both chapters and has never been driven.
* ⏳ **32 SHORT bolts** — Amir's, now counted properly.
