# E.6 Structural Element — Purlin Course

> ## ⏸️ **NOT CLOSED.** The members stand; the connections produced nothing.
> 9 members in the band (2 main girders + 7 purlins) and **the JOINT AUDIT reports all 9 FLOATING**.
> 10 of 14 `purlin` calls returned `EB_OK` and the band contains **0 plates, 0 bolts, and the model
> contains 0 `Ks_WeldFlag`** — so nothing was actually connected.
> ⭐ **The guard caught this before Amir saw it.** That is what check 9 was written for after E.4.

*Read 11/08/2026, pages 1104–1108 (fulltext lines 28531–28669). Band `E.06-PURLIN-COURSE`, grid
`1A78` at x = 420 000. Plugin v178.*

> ⭐ **Command name: `PS_Pfette`** — the first chapter in part E to print its own command name.

---

# THE CHAPTER

## Two modes, and the second one is the interesting one
1. **Fill an area** — click the lower-left and upper-right corners. *"The purlins are then inserted
   into this area with their **lower edge flush with the plane of the current user coordinate
   system**."*
2. ⭐ **Secondary beams between two main girders** — *"finish the input of area at the first point
   using **ESC**. Then, you are prompted to enter the first main girder, then the second one… The
   dimensions for the purlin course then are defined by the **end points of the main girders**. In
   addition, you have the possibility to **connect them with each other by means of a template**."*

## General
`Angle` — *"the purlins are rotated around this angle value, which means they are arranged in a
**diagonal** fashion"* · `Height Offset` (move the course in +Z) · `Dynamic` · `Symmetrical` ·
`Draw Diagonal` · ⭐ `Cut at Edge` — *"the purlins at the edges are **cut flush**. Values in Left
Projection and Right Projection are **then not considered**."*

## Dimensions
⭐ **`Fixed Grid`** — *"you can indicate… the **approximate** distance… The program divides the
distances regularly according to this specification and **the value is rounded up or down**
correspondingly. The actual distances then are displayed in the **Effective Grid** field."*
`Free Grid` — define the division yourself in the `Distances` list · `Turn` (distances inverted) ·
`Offset Bottom`/`Top` — *centreline* of the lowest/topmost purlin to the area's outer edge ·
`Offsets Fixed` — *"the selected distances are also kept in the grid; otherwise they are centered"* ·
`Offset Left`/`Right` — projection past the edge; ⚠️ *"In case of a **diagonal edge**, this value
references the **centerline** of the purlin; **negative values shorten** the purlin towards the
inside."*

> ### ⭐⭐ "ASK FOR A SPACING, GET A ROUNDED ONE" — THIRD INSTANCE
> `Fixed Grid` → `Effective Grid` is the same pattern as **E.3's `PostSpace`** (asked 1000, got 900)
> and **E.5's `Segment Dist.`**. ⇒ **Never quote the requested spacing as the built spacing. Read
> the members back.** Three chapters, one rule.

## Shapes
Selection lists — *"all shapes are available"* · `Position` (insertion position relative to the
insertion axis) · ⭐ `Mirror` — *"the shape position is mirrored at the length axis to turn the **web
side** e.g. at U-shapes"* · `Rotation`.
⭐ *"Alternatively, you can also use a **joist girder** instead of a standard shape (see structural
element 'Joist Girder')"* — **E.6 can host E.8's product**, selected by template on an extra tab.

---

# ⭐ THE API — "NO CREATOR", SECOND INSTANCE

`PsPurlinDistribution` has **no `insert()`, no `init()`, no `Create()`**, and `PsCreatePurlin*` does
not exist. That is E.5's third category again — *nothing to call*, as distinct from *the creator
refuses*.

But the class is rich, and two of its methods **are** the manual's second mode:

```
setBorderShapes(Int64 ShapeId1, Int64 ShapeId2)     <- "the first main girder, then the second one"
isSecondaryBeams()                                   <- which mode this distribution is in
addBorder(PsPoint Start, PsPoint End) · addPoly(Int64 Id) · getPolyCenter · getControledElements
```

Field → property, measured off the surface:

| dialog | property |
|---|---|
| `Fixed`/`Free Grid`, `Effective Grid` | `Raster` · `Pattern` · `UseEqualPattern` · `MaxPattern` |
| the four Offsets, `Offsets Fixed` | `OffsetDown/Top/Left/Right` · `KeepLowerOffset` · `KeepUpperOffset` |
| `Cut at Edge` · `Angle` · `Height Offset` · `Turn` | `EdgeCutStatus` · `Angle` · `InsertHeight` · `InversOrder` |
| Shapes tab | `Katalog` · `Key` · `ShapeType` · `ShapePosition` · `ShapeMirror` · `ShapeRotation` |
| the connecting template | ⭐ **`ConnectionTemplate` + `ConnectionType`** |
| the joist-girder alternative | ⭐ **`UseJoists` + `JoistTemplateName`** |
| cope at the border | `CopeHeight` · `CopeWeb` · `CopeWide` · `BorderGap` |

⇒ ⭐⭐ **`ConnectionType` is B.22's enum** — `kBoltet` / `kShoe` / `kCleat` / `kShapeBased`. **The
purlin course owns the purlin connection B.22 already mapped**, and `UseJoists`/`JoistTemplateName`
hands off to E.8. E.6 is the chapter that ties B.22 and E.8 together.

---

# WHAT IS IN THE BAND — AND WHY IT IS NOT ENOUGH

The detail was fixed **before** the first build call, per E.5's method rule:

```
girders   IPE300 rot=90, axis z=0 -> spans z −150…+150, top flange at z=+150
          along x 420000…429000, at y=0 and y=6000
purlins   U160 @ DIN_U, spanning y 0…6000, LOWER EDGE FLUSH at z=+150 -> axis z = 230
grid      1500 over 9000 -> 7 purlins, 6 gaps
joint     B.22's `purlin` op + template, with WeldToSupportShape=1
```

```
band          9 shapes (2 × IPE300, 7 × U160) · 0 plates · 0 bolts
JOINT AUDIT   *** 9 FLOATING MEMBERS ***
Ks_WeldFlag   0 in the entire model
collision     0 in the band
vfy_fit       bolts=252 OK=252 (unchanged -- this band contributed none)
```

⚠️ **10 of 14 `purlin` calls returned `EB_OK` and connected nothing.** `plates=0` on the four that
reported `EB_ERR`, and no plates from the ten that reported OK either.

⭐ **And `create=False` is NOT the signal here** — B.22 measured that *"`Create()` returned False on
every single successful connection"*. So the boolean is useless in both directions on this op, and
**the only witnesses are the plate count and the joint audit.**

## What B.22 already knew that this must respect
* ⚠️ *"`Default/Standard` builds a cleat plate attached to NOTHING"* — it ships
  `WeldToSupportShape = False`. Setting it to 1 was attempted here and produced **no weld flags at
  all**, so the `set=` route did not take on this path.
* ⚠️ *"The template NAME does not set the TYPE"* — `ConnectionType` must be set separately.
* ⭐ *"A second purlin is NOT required"* — a single purlin on a support is a valid input.

---

# ⏸️ What E.6 needs before it can close
* ⛔ **The connection route.** Either `set=WeldToSupportShape=1` reaches the template and the plate
  gets built, or the course's own `ConnectionTemplate`/`ConnectionType` must be driven through a
  bound `PsPurlinDistribution` — which needs a distribution to exist first, and there is no creator.
* ⬜ **`PS_Pfette`** — the chapter names the command. The interactive route is untested and the `cmd`
  allow-list is capped at 9 entries by Amir's decision.
* ⬜ `Angle` (diagonal courses), `Cut at Edge`, and the `Free Grid` distance list — read, not built.
* ⬜ `UseJoists` + `JoistTemplateName` — the E.8 hand-off, to be picked up with E.8.
