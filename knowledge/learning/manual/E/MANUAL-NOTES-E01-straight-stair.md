# E.1 Structural Element — Straight Stair

*Read end to end 11/08/2026, pages 1040–1051 (fulltext lines 27109–27340). Band: `E.01-STRAIGHT-STAIR`
grid at x = 120 000. Plugin v172 → v176.*

> *"This function generates a complete staircase construction with several stair heads including
> handrail and corresponding working frames… you just click the starting point and a point in the
> direction of the stairs (treading direction). The number of required individual steps will be
> **automatically calculated** based on your specifications."*

⭐ **The shape of the command:** you give it a **direction and three sizes**; it works out the step
count, the exact rise and the exact tread. **Riser Count and Angle are RESULTS, not inputs** — the
dialog shows them greyed. That is the whole idea of the chapter.

⭐ *"the complete construction including the handrail can be saved afterwards as **template**"*, and
*"At staircases with handrail, this handrail will be created as **structural element of its own**"*
⇒ **the stair and its handrail are two objects, not one.** That is why E.3 exists separately.

---

## The eight dialog pages

### 1 · Dimensions
| field | meaning |
|---|---|
| `Width` | across the **outer steel edge** |
| `Length` | the **entire** stair construction |
| `Height` | start point → **upper edge of the stair head including gridiron** |
| `Riser` | the **desired** rise |
| ⭐ `Upper Insertion Point` | the insert point is at the **top** and the stair runs **downwards** |
| `Riser Count` · `Angle` · `Treading real` · `Actual Rise` | ⭐ **results** — but each of the last two *"can be specified as a fixed value before, and the construction is calculated again based on it"* |
| `Dynamic` | watch the changes on screen |

### 2 · Landings
`Lower Landing` — ⚠️ *"to create the stair head you have to **check the input field in front of it**.
Otherwise the staircase cheeks are **cut at the basic plane** and the stair head is omitted."*
`Upper Landing` · `Create No Upper Platform` (*"cut off perpendicularly"*) · `US-Definition` ·
`Stair Foot Length` — ⭐ *"if you enter **0**, the staircase cheek directly hits the basic plane"*.

⚠️ **Two fields exist only when the foot length is 0:**
`Vertical Section` (a vertical cut from the front edge of the cheek) and
`Ground Distance` (*"a gap between the staircase foot and the basic plane, e.g. for foundations"*).

`Web Grating` (height of the attached gridiron) · `Side Offset` (its projection beyond the outer
edge; **negative shrinks it**) · `Upper Edge Equal` (*"side offset is set to 0"*).

### 3 · Platforms
`No. of Platforms` (intermediate stair heads) · `Inner Distance` (*"between two staircases
including cheek shapes"*, for change-over heads) · `Platform No.` (which one you are editing) ·
⭐ `Angle to prev.` — *"you can create the **bent running** of the staircase or stair heads for
change over (**180°**). **Negative values create an opposite sense of rotation.**"*

### 4 · Shapes
*"Here you select the shapes for the stair and the stair head **cheeks**."*

### 5 · Bolts
`Drill Stringer` (*"the stair cheeks are drilled to permit bolting the steps or the handrail"*) ·
`Bolt…/Handrail` · `Bolt…/Step`, each with its own bolt type.

### 6 · Steps
`Create Steps`, and three kinds: **standard steps · shape steps · block steps**.
`Increment` (the depth) · **`b, c, d`** — *"b is the distance of the holes from the **upper edge**
of the steps, c is the **height of the fastening**, d is the **hole spacing** of both mounting
holes"* · `Hole Distance` · `Hole Dia.` · `Slot Length` (*"the axes of the **rear oblong** step
hole"* — ⭐ the back hole is slotted, for thermal movement and fit-up) · `Offset`.

### 7 · Handrail
`Create Handrail` · `Simplified` (*"not modelled, only displayed as **system lines**"*) ·
`Start`/`End`/`Side`/`Height Offset` — ⭐ *"in case of staircases **without basic stair head** you
can move the start of the handrail to a position where a **bolt mounting is possible**"*.
`Connection` = **Automatic · Vertical · By Side · Individual** (the last uses the handrail
template's own setting) · `Side Offset` (>0 inserts a **vertical connecting plate**) ·
`Hand. Template`.

### 8 · Work Frame
`Created Views` — Front / Lateral Left / Lateral Right / Top · `Group Name` · `Edge Distance`
(*"the size of the different frames is decreased, so you can better select frames lying next to
each other"*).

---

## The API — `Bentley.ProStructures.StructuralObject.PsStairs`

### ⭐⭐ THE API IS IN GERMAN WHILE THE DIALOG IS IN ENGLISH

No amount of staring at the English dialog produces these names:

| dialog | API | German |
|---|---|---|
| `Riser` | **`setSteigung` / `getSteigung`** | *Steigung* = rise |
| `Treading real` | **`setAuftritt` / `getAuftritt`** | *Auftritt* = tread |
| `Lower`/`Upper Landing` | **`setPodestDown` / `setPodestTop`** | *Podest* = landing |
| Shapes → the cheeks | **`setWangenShape(Katalog, Key)`** | *Wange* = stringer / cheek |
| `Web Grating` | **`setGitterRost`** | *Gitterrost* = grating |
| `No. of Platforms` | **`setEtageCount` / `getEtage(i)`** | *Etage* = storey |

⇒ **Search the API surface by GERMAN construction terms when an English dialog word finds
nothing.**

### The rest of the mapping
| dialog | API |
|---|---|
| Width / Height / Length | `setWide` · `setHeigth` *(sic — misspelled in the API)* · `setLength`, and all three are also arguments of `Insert` |
| `Upper Insertion Point` · `Dynamic` | `UseUpperInsertPoint` · `DynamicStatus` |
| `Riser Count` · `Angle` · `Treading real` | ⭐ **computed**: `computeSteigung(i, ref StepCount)` · `computeAngle(i)` · `computeAuftritt(i, StepCount)` |
| `Create No Upper Platform` · `US-Definition` | `NoTopPodest` · `PodestUsDefinition` |
| `Stair Foot Length` / `Vertical Section` / `Ground Distance` | `setStairFootLength` · `setStairFootVerticalCutOffset` · `setStairFootHorizontalCutOffset` *(the last two are **inferred** from the field order — not measured)* |
| `Inner Distance` | `setDistanceBetween` |
| per-platform `Angle to prev.`, `Length`, `Height` | **`PsStairsLevel`** — `.Phi` · `.Length` · `.Height` · `.PodestDown` · `.PodestTop` · `.Flags`, via `getEtage(i)` / `setEtage(i, level)` |
| Steps | `StepStatus` · `StepType` · `setStepShape(Kat,Key)` · `setStepBlock` · `setStep_b/_c/_d/_e/_Size` · `setStep_HoleDistance` · `setStep_Dm` · `setStep_HoleAxis` · `StepOffset` · `StepGroup` |
| Bolts | `DrillStatus` · `RailBoltStatus` + **`RailBoltCRC`** · `StepBoltStatus` + **`StepBoltCRC`** |
| Handrail | `LeftRailStatus` · `RightRailStatus` · `RailFrameOnly` (= `Simplified`) · `RailStartOffset` · `RailEndOffset` · `RailSideOffset` · `RailConnectionType` · `setRailIds` · `getLeftPolyRailId` / `getRightPolyRailId` |
| Work Frame | `setWorkFrame(ShapeView, bool)` · `WorkFrameName` · `WorkFrameOffset` · `getWorkFrameId` |

⚠️ **Bolt styles are CRC-only here**, as on the splice class in B.21 — a *name* must be resolved
through `PsObjectStyleList` first. The `stair` op does that (`railboltstyle=` / `stepboltstyle=`).

---

# MEASURED 11/08/2026

## 🛑 CORRECTION to B.23 — `PsStairs` DOES have a creator

B.23's audit table recorded `PsStairs` and `PsCircularStairs` under *"creation route: ❌ none"*, and
that reshaped the part-E roadmap. **It is wrong, and the reason is capitalisation:**

```
PsStairs          Boolean Insert(PsPoint, PsVector, PsVector, Double Wide, Double Height, Double Len)   ← capital I
PsCircularStairs  Void init() · Boolean insert(PsPoint, PsVector, PsVector)                             ← lowercase
PsLadder          Void insert(PsPoint, PsVector, PsVector, PsPoint End)                                 ← lowercase
PsBracing         Boolean insert(PsPoint, PsVector, PsVector)                                           ← lowercase
PsPortalFrame     Void init() · Int32 insert(PsPoint, PsVector, PsVector)                               ← lowercase
```

**A search for `insert(` misses `Insert(`.** Same family as B.13's .NET-vs-COM enum split and
B.21's `HoleWorkloose` / `HoleWorkLoose`.

✅ **Confirmed with no creator at all:** `PsTruss`, `PsJoist` (E.5, E.8).

## ⛔ But the method existing is not the method working — seven configurations refuse

```
A  minimal (Insert's three sizes only)          Insert=False   census 82->82
B  + etages=1                                   Insert=False   census 82->82
C  + rise=180                                   Insert=False   census 82->82
D  + wange DIN_U/U200                           Insert=False   census 82->82
E  etages + rise                                Insert=False   census 82->82
F  etages + rise + wange                        Insert=False   census 82->82
G  F + steps (stair/step10)                     Insert=False   census 82->82
```

⭐ **And this is NOT B.24's transport failure.** The read-back after every attempt shows
`wide=1000 height=3000 len=4000` — **exactly what was passed.** `wange` round-trips too
(`DIN_U/U200`). **The geometry arrives and the creator still refuses.**

⚠️ **`computeSteigung(0, ref stepCount)` writes GARBAGE into its out-parameter** on a stair that
was not created — `-2010406392`, a different huge negative each run — and `computeAngle(0)` returns
a constant **0.785 rad (= π/4)**, a default rather than a computation.
⇒ **Neither is a measurement until the stair exists.** An out-parameter that is never written is
not zero; it is whatever was on the stack.

## ⭐⭐⭐ It is not `PsStairs`. It is the whole self-inserting family.

```
structprobe -> PsStairs.Insert        = False   census 82->82
               PsLadder.insert (Void)          census 82->82
               PsPortalFrame.init+insert = 0    census 82->82
```

Plus B.24's `PsBracing.insert()` — refused under ten configurations — and B.23's
`PsGussetConnection`, which has no creator at all.

> ### ⭐⭐⭐ THE RULE FOR THE WHOLE OF PART E, measured on the decisive counter-example
> **A structural element is creatable from code only where ProSteel ships a SEPARATE `PsCreate*`
> class with `SetToDefaults()` + `Create()`.** The `StructuralObject` classes that insert
> **themselves** all refuse.
>
> **`PsCreateHandrail` is the only `PsCreate*` in the whole `StructuralObject` namespace — and it
> works.** `Create()=True`, **16 objects**, a complete industrial handrail (see E.3).
>
> ⇒ ⚠️ **B.23's table was a map of METHOD EXISTENCE and has been read as a map of CAPABILITY.**
> The two are not the same, and the difference is seven refusals.

## ⇒ E.1's verdict

**A straight stair cannot be created from code in this product.** The command is
interactive by design — the manual's own first line is *"you just **click** the starting point and
a point in the direction of the stairs"* — and this is B.24's conclusion reached again from a
different chapter: *"This is not a gap in the agent's knowledge. It is the product's shape."*

**What IS reachable, and it is not nothing:**
* every parameter above — **settable and readable**, so a stair built by Amir in the dialog can be
  read, audited and reported on in full;
* `PsTransaction.GetObject(..., PsStairs&)` binds an existing one (B.23) — ⚠️ **untested on a real
  stair, because there is none in this model to bind**;
* `retrieveGeometrie()` and `getEtageUcs(i)` for the geometry and each storey's frame;
* the **composition route**, which is how B.25's braced bay was built: stringers, treads, holes and
  bolts are all ops that work.

## Model state
Band `E.01-STRAIGHT-STAIR`, grid at x = 120 000 (handle `3B5`). **Nothing was created by the seven
attempts — census 82 → 82 throughout**, so the band holds only its grid. Saved.

## Still open
* ⚠️ **`PsCircularStairs.init() + insert()`** — the one self-inserting class with an `init()` that
  was **not** probed here. It belongs to **E.2** and is tested there.
* ⚠️ **`Vertical Section` / `Ground Distance` → `setStairFootVerticalCutOffset` /
  `setStairFootHorizontalCutOffset`** — **inferred from field order, not measured.** They cannot be
  measured until a stair can be created.
* ⚠️ Binding a real `Ks_Stairs` — needs one to exist. **A stair drawn by Amir in the dialog would
  close this in one call**, and would also settle the two inferred fields above.
