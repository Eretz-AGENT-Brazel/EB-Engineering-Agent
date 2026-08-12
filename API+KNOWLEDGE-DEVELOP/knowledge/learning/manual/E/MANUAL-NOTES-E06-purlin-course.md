# E.6 Structural Element — Purlin Course

> ## ✅ CLOSED 11/08/2026.
> **23 members · 14 cleats · 48 `Ks_WeldFlag` · JOINT AUDIT CLEAN · 0 collisions in the band.**
> It closed on the second pass, and the first pass is kept below because the diagnosis is the
> chapter's real content.

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

# THE BUILD — AND THE DIAGNOSIS BETWEEN THE TWO PASSES

The detail was fixed **before** the first build call, per E.5's method rule, and the geometry came
out exactly as designed:

```
girders   IPE300 rot=90, axis z=0 -> spans z −150…+150, top flange at z=+150
          along x 420000…429000, at y=0 and y=6000
purlins   U160 @ DIN_U, LOWER EDGE FLUSH at z=+150 -> axis z = 230   (the chapter's own words)
grid      1500 over 9000 -> 7 purlins, 6 gaps
```

## ⛔ PASS 1 CONNECTED NOTHING — AND SAID `EB_OK` TEN TIMES

```
10 of 14 `purlin` calls -> EB_OK,  band: 0 plates, 0 bolts, 0 Ks_WeldFlag
JOINT AUDIT -> *** 9 FLOATING MEMBERS ***
```

⭐⭐ **The JOINT AUDIT caught it before Amir saw the model.** Check 9 was written after E.4 was
committed with two rafters joined to nothing; on the very next chapter it caught the same failure
mode. That is the whole purpose of it.

## ⭐⭐ A HOLE FIELD IS NOT A HOLE

The witness that explained pass 1:

```
mods  on the girder ->  holeFields=5
holes on the girder ->  count=0
```

⇒ **The connection registered five hole FIELDS and drilled nothing.** `op=holes` counts real holes
only, which is why `holes`, the JOINT AUDIT and the bolt count all read zero while `mods` showed
activity. **Two different witnesses; check both.**

## ⭐ AND `create` IS USELESS IN BOTH DIRECTIONS ON THIS OP

B.22 had already measured *"`Create()` returned False on every single successful connection"*.
Pass 1 added the other half: **ten `EB_OK` results that built nothing.**
⇒ **On a purlin connection the only witnesses are the PART COUNT, the weld flags and the JOINT
AUDIT.** Never the boolean.

## ✅ PASS 2 — B.22's MEASURED-GOOD CONFIGURATION

The template dump named the two faults precisely:

```
Default/Standard ships   PurlinType = kBoltet        <- NOT kCleat
                         WeldToSupportShape = False  <- B.22: "a cleat plate attached to NOTHING"
```

⚠️ **B.22's rule, confirmed here: *the template NAME does not set the TYPE*.** Both had to be set:

```
set = "PurlinType=kCleat;WeldToSupportShape=1"
```

All 14 calls returned `EB_OK` with `census +5` each, and this time the model agrees:

```
band          23 members: 2 × IPE300, 7 × U160, 14 × FL 100×10 cleats
cleats        171.4 long, from the girder's top flange (z 3.55) to the purlin underside (z 175)
              4 holes each
welds         48 Ks_WeldFlag
JOINT AUDIT   CLEAN
collision     0 in the band
vfy_fit       bolts=252 OK=252 BOLT-NO-HOLE=0 GAP-IN-PACKET=0 SHORT=0
```

> ### ⭐⭐ THE WELDS HERE ARE **PROVEN**, NOT DECLARED.
> Every other chapter closed its welded joints with a declaration to `vfy_joined`, because B.23
> established welds are not creatable from code. **E.6's connection creates 48 real `Ks_WeldFlag`
> objects** — the weld exists in the model as an entity. That is a stronger form of evidence than a
> declaration, and it is the first time in part E it has been available.

⚠️ **The cleats carry 4 holes each and there are no bolts** — holes waiting for fasteners, B.22's
*"`kBoltet` drills more holes than it bolts"* family. Recorded, not papered over.

---

# Still open
* ⚠️ **The 14 cleats carry 56 holes and no bolts.** Whether the fasteners should come from the
  template or be added separately is unresolved.
* ⬜ **`PS_Pfette`** — the chapter names the command. The interactive route is untested and the `cmd`
  allow-list is capped at 9 entries by Amir's decision.
* ⬜ `Angle` (diagonal courses), `Cut at Edge`, and the `Free Grid` distance list — read, not built.
* ⬜ `UseJoists` + `JoistTemplateName` — the E.8 hand-off, to be picked up with E.8.
