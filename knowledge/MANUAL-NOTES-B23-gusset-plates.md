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
