# B.23 Gusset Plates — chapter notes

*Read end to end 09/08/2026, pages 328–331 (fulltext lines 7769–7854). Command
**`PS_GUSSET_PLATE`**. Geometry built in the band at x ≥ 170000.*

> *"This command is used to create a gusset plate which **combines several shapes** with each
> other. The form is **optimally defined on the base of the shapes to be connected** and further
> **limiting edges**. The shapes and the gusset plate are **automatically drilled and combined
> with each other by bolts**."*

Unlike B.19–B.21, which attach a known plate to a known face, here **the plate's outline is
computed from the members it joins**. You do not draw it and you do not dimension it — you say
which shapes meet and which edges bound the result.

## Creating one

1. select the **shapes to be connected**
2. then, optionally, **further shapes as limiting edges** — *"These limiting edges **influence the
   form** of the gusset plate."*

⭐ The manual's own example: *"you can connect **three shapes with a support** using a gusset plate
and it is **guaranteed that the gusset plate is fit tightly to the web of the support**."*

## Fields

| field | meaning |
|---|---|
| `Plate Thickness` · `Bolt Style` · `Dia` · `Workloose` | as elsewhere |
| `Offset` | *"the value by which the gusset plate has to **extend beyond the shape edges** in transversal shape direction"* |
| ⭐ `Gusset Pos` | `Plate Center` — in the centre of the shapes; `Plate Upper` and/or `Plate Lower` — at the upper and/or lower edge. *"It is possible to connect shapes with **two plates** as well."* |
| ⭐⭐ `Limiting Shape` | *"it is **not the edge of a limiting shape situated next to the gusset plate** which is used as borderline, but the **opposite line**… it is possible to design a gusset plate in a way that it **overlaps e.g. the complete flange** of a limiting shape"* |
| ⭐ `Weld Bracing` | *"the shapes and the plate are **not drilled**. It is possible to combine them with each other using a **weld**."* |
| `Form Group` / `With Bolts` | as elsewhere |
| ⭐ `Use Existing Holes` | *"Existing holes are used **instead of drilling new ones**"* |

Four buttons: **add** a connected shape (*"the form of the gusset plate is **calculated anew**"*) ·
**remove** one · **add a limiting edge** (*"you can e.g. extend a gusset plate up to a support"*) ·
**remove a limiting edge**.

⇒ The gusset is a **live object**: adding a member re-solves its outline.

## Drill Hole Distances

`Number Shape` · `1 Edge 1 Hole` (shape edge → first hole) · `Hole – Hole` ·
`n Hole - Edge` (last hole → **edge of the plate**) · `Number Transv. Dir.` ·
`Transversal Distance` · `Drill Hole Pos.` (the insertion axis).

## ⭐⭐ A new input grammar: distances as multiples of the bolt diameter

> *"you can indicate all distances either as **absolute values** or as **many times the amount of
> bolt diameter**. Enter e.g. **`*2`** for the double value."*

That is the detailer's own convention written into the dialog — edge distance `*2`, pitch `*3` —
and it means a gusset's hole pattern **rescales automatically when the bolt diameter changes**.

⚠️ Note this is a *different* grammar from B.6/B.8/B.14's `Number*Pitch`. Here the `*` multiplies
the **bolt diameter**; there it repeats a **spacing**. Same character, different meaning, different
dialogs.

---

# MEASURED 09/08/2026 — and the verdict is negative

## ⛔ There is no creation route for a gusset plate

`Bentley.ProStructures.StructuralObject.PsGussetConnection` exists, and `kGussetPlate` is a
first-class `ObjectType`. But the class has **only getters and setters**:

```
getDistanceFront / getDistanceBehind / getDistanceBetween / getDistanceCross
get/setMaterialIndex, ArticleN, LayerN, Detail/Display/Area/FamilyClassIndex
listInformation, getObjectType
```

**No `Create()`. No `insert()`. No `SetConnectionObjectId`. No template API. No COM twin.**

⇒ **A gusset plate can be read and edited from code, but not created.** This is the first chapter
in the programme whose central command is unreachable.

## Where the boundary actually falls

`StructuralObject` is a mixed namespace, and it was worth measuring which members have a way in:

| class | creation route |
|---|---|
| `PsCreateHandrail` | ✅ `Create()` |
| `PsBracing` | ✅ `insert(Origin, XAxis, YAxis)` |
| `PsLadder` | ✅ `insert(PickPoint, XAxis, YAxis, End)` |
| `PsPortalFrame` | ✅ `init()` + `insert(PickPoint, XAxis, YAxis)` |
| `PsStairs` · `PsCircularStairs` | ❌ none |
| `PsJoist` · `PsTruss` | ❌ none |
| **`PsGussetConnection`** | ❌ **none** |

⇒ Handrails, ladders, bracings and portal frames **are** scriptable; stairs, joists, trusses and
gussets are not. That reshapes the roadmap for part E (EB's own products) and is recorded there.

⭐ **The likely indirect route:** a bracing connection is exactly what produces gusset plates, and
`PsBracing.insert()` does exist. **B.24 Dynamic Bracing is the place to test whether a gusset can
be obtained that way** — and it is the next chapter.

## What was built

The manual's example, as geometry ready for a gusset: a **HE300B column** (the support, whose web
the plate must fit tightly against) and **three `L90X9` members meeting at one node** at
`170000, 0, 2500` — a horizontal member and two diagonals.

## ⚠️ A silent failure found here, which corrects B.19

Checking why `EA90x90x9` would not build as a shape revealed that **it does not exist**: `BS EQUAL`
holds 6, 7, 8, 10 and 12 — no 9. Yet B.19's web-angle connections had accepted that key without
complaint. Testing four cases — a valid `BS EQUAL` key, a valid DIN key in two catalogue-name
forms, and no key at all — **every one produced `L90X9` from `DIN.DIN_WINK_GL`**.

⇒ **`PsWebAngleConnection.SetKey()` and `SetKatalog()` are complete no-ops.** The section comes
from the template. The B.19 notes have been corrected.

⚠️ And **catalogues have two names**: `DIN WINKEL GLEICH` for lookup, `DIN.DIN_WINK_GL` as stored
in the model. Neither string works in the other place.


---

# The connection built by hand — 09/08/2026, at Amir's direction

The chapter has no creation route, so Amir asked for the detail to be built from primitives
instead: *"תשכפל את הזוויתנים והעמוד בטור ותייצר מחבר גזירה עם ברגים עבור כל הזוויתנים"*, with a
sketch in TOP view.

⚠️ **His correction, which changed the geometry:** the backing plate is welded to the **WEB**, not
to the flange. It therefore sits **between the flanges**, and the gusset leaves through the flange
opening. Measured on HE300B: flanges span X at y = ±150, web faces at **x = ±5.5**.

| part | measured |
|---|---|
| backing plate (his 🔴) | 12 mm, `x 170006..170018`, 260 × 600, on the web face between the flanges |
| gusset (his 🟡) | 12 mm, `x 170018..170418`, 400 × 700, straddling the bracing plane |
| the joint between them | the backing plate's far face is **exactly** the gusset's start — 170018 |
| the three L90x9 | envelope `y 6006..6096` — their face **exactly** on the gusset face at 6006 |
| bolts | **2 per angle**, M16 in Ø18 holes, `DIN7990` |

⭐ **The seating rule:** an L90x9's envelope is 90 wide **centred on its axis**. To land its face on
a 12 mm gusset straddling y = 0, the axis goes to **y = 6 + 45 = 51**. Nothing else needed moving.

Then replicated into a row of four: **48 objects** — 16 shapes, 8 plates, 24 bolts. Every node read
back identical: gusset 6 holes, each angle 2, six bolts.

## What this exercise taught

- ⛔ **`boltparts` fails SILENTLY without an explicit `style`.** `(default)` gives `create=False`
  and zero bolts; `DIN7990` works instantly. The plugin's own failure hint blamed the gap distance
  and was wrong.
- ⭐ **`drill hosts=A,B` makes one hole through both parts** in a single call — the correct way to
  produce a shared hole. Verified 0→6 on the gusset and 0→2 on each angle.
- ⭐ **`replicate` preserves holes and bolts** — 12 entities × 3 copies, nothing dropped.
- ⛔ **Welds are not creatable standalone.** `PsCreateWeldFlag.Create()` returned **false** with no
  style, with a sign, and with `makeweld=0`; `WeldStyleCount` reads **0** while the object-style
  probe reports **4** weld styles. Yet B.21's splice produced **32 `Ks_WeldFlag`** as a
  by-product. Amir: *"זה בסדר - לא קריטי לי הריתוכים בנתיים"* — left open deliberately.

⇒ The pattern of the whole day, once more: **connection classes work, standalone creators do not.**


## Second connection — SHS column, UPN beams, shaped gusset (09/08)

Amir asked for the same principle on a **hollow section** with **channels** instead of angles, a
**shaped** gusset instead of a rectangle, and **4 bolts per beam**.

| part | measured |
|---|---|
| column | `RQ200x6` (SHS 200/200/6), wall face at **x = 170100** |
| backing plate | 12 mm, `x 170100..170112`, **200 wide** to match the tube, 640 tall |
| gusset | 12 mm, an **8-vertex polygon**, cut square across the end of each member |
| three `U160` | web flat on the gusset, all three read back at `y 20006..20071` |
| bolts | **4 per beam** = 12, M16 in Ø18, `DIN7990`, 12 distinct positions |

⭐ **Channel orientation, measured not assumed:** viewed along its own axis a `U160` opens its C
towards **+Y**, so the **web is on the LOW-Y edge** of the 65 mm envelope. Axis = gusset face
**+ 32.5**.

⭐ **The gusset outline comes from the members.** For a member with unit axis **u**, unit
perpendicular **p** and reach **L**, its end corners are **`L·u ± (d/2)·p`**. Eight such points,
joined, give a gusset cut square at each member and tapered between them.
⚠️ `plate9 mode=poly` takes points in the **insertion frame**, not WCS.

### Two mistakes, both caught by measuring

1. **Bolt groups merged.** Inner rows at 100 mm along each member put holes **11 mm** apart at the
   node; ProSteel merged the groups and `boltparts` made **14 bolts at 10 positions**, two of them
   carrying three bolts. `create=True` throughout. Rows moved out to 170/260 and 290/380 →
   closest spacing **65.4 mm**, and a clean 4+4+4 at 12 distinct positions.
   ⛔ **Holes cannot be removed once drilled** — the fix required deleting and rebuilding the
   gusset and all three channels.
2. **The channels collided.** Amir: *"אני לא רוצה שתהיה התנגשות בין ה-UPN."* A 45° diagonal
   leaving the same node as a 160-deep horizontal has its lower corner at `(axis+56.6, axis−56.6)`
   — inside it. Setting that corner on the horizontal's face gives **t ≥ 193**, so the diagonals
   now start at **t = 200** and the gusset lengthens along them.
   ⇒ Proven, not assumed: `collision box='169800,19800,1500;171500,20200,3600'` →
   **`parts=17 collisions=0`**. ⚠️ The op takes a **box**, not handles.

---

# AUDITED 10/08/2026 — the gusset verdict stands, and looking for its read route found the binder

*Full record: `AUDIT-PART-B-2026-08-10.md` § B.23. Plugin v162 → v165. One CEILING entry
retracted, one new entry on `LETHAL-CALLS-do-not-invoke.md`.*

## The chapter's verdict, completed

*"A gusset plate can be **read and edited** from code, but **not created**."*

| half | status |
|---|---|
| **not created** | ✅ **measured and unchanged** — `PsGussetConnection` has no `Create`, no `insert`, no `SetConnectionObjectId`, and no COM twin |
| **read and edited** | ⚠️ was **inference from a property list**. The route is now found and proven on siblings — but **not yet on a gusset**, because this model contains no `kGussetPlate` object (the band's gussets are hand-built `Ks_Plate`). **A task, not a finding.** |

## ⭐⭐⭐ `PsTransaction.GetObject` — the binder, with 57 overloads, used nowhere

```
Bentley.ProStructures.Drawing.PsTransaction
   Boolean GetObject(Int64 Id, PsOpenMode Mode, PsGussetConnection& entObject)
   … and 56 more: PsGrid, PsEditConnection, PsWeldFlag, PsPositionFlag, PsBoltStyle,
     PsShape, PsPlate, PsBolt, PsAssembly, PsBracing, PsPortalFrame, PsStairs, PsJoist,
     PsTruss, PsHandrail, PsLadder, PsWorkframe, PsBendPlate, PsBendShape, PsArcPlate …
```

> ⭐⭐ **The binder is not on the class. It is on the transaction.** Every chapter that asked
> *"can this class bind to an existing object?"* asked the **class**, and the class was the wrong
> place to ask.

New op **`bind`**, measured on objects of known identity:

```
2F1  Ks_Grid   -> [name='A' len=24000 wide=15000 type=kRectangle lenDiv=4 wideDiv=3 …]
456  Ks_Plate  -> [name='B9_RECT 400x250x12' L=400 H=12 verts=5 rect=True]
2C6  Ks_Shape  -> [key='HE300B' cat='DIN.DIN_HEB']
```

## ⛔⛔ AND WRITING THROUGH A BOUND OBJECT KILLED AUTOCAD — TWICE

`addUserXaxis` on the bound grid: dead. Isolated per the protocol — saved immediately before, one
call in its own run, `probe=addx` — **dead again.** Third entry on `LETHAL-CALLS-do-not-invoke.md`.

⭐ **Reading a bound object is safe.** Grid, plate and shape all read back correctly, repeatedly.
**Writing killed it on the first attempt.** ⚠️ `getUserXaxis` / `getUserYaxis` are **UNKNOWN, not
safe** — the add died before they were reached.

## ⛔⛔ `GetObject` DOES NOT TYPE-CHECK

```
bind 2C6 (a Ks_Shape) cls=grid -> grid=True [len=281474976713490 wide=NaN xDesc=234]
```

`True`, with a reinterpreted pointer. A read gives nonsense that looks like data; a write would
corrupt the object. **`bind` now reads the entity's real class first and refuses a mismatch.**

## 🛑 What this retracts in B.6

B.6's *"`PsGrid` … no `SetObjectId`, no `readFrom`, **no binder of any kind** … the two halves
never meet in the API"* is **withdrawn** — see `MANUAL-NOTES-B06` and `qc/retracted.tsv`.
**B.6.7 stays closed**, but because the halves meet at a call that kills the session, not because
they never meet.

## The lead this hands to B.24

A bracing connection is what produces gusset plates, and `PsBracing.insert()` exists. The
`LogicalLinkType` enum backs it: **`kBracingPlate`, `kBracingLasche`, `kBracingConnect`,
`kBracingFiller`**. That is B.24's business and it is the next chapter.

## Model state

**Nothing was built and nothing was changed.** Two crashes, nothing lost — saved immediately
before each. Census **1 203**.
