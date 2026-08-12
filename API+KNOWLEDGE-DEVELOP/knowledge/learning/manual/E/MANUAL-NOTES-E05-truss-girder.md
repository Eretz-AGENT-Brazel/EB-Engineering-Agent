# E.5 Structural Element — Truss Girder

> ## ✅ CLOSED 11/08/2026, on Amir's call — *"סיימנו סופית עם פרק E.5, אפשר לסמן בירוק"*.
> The node was re-detailed once and rebuilt once, and the defect that mattered is gone:
> **`bolts=252 OK=252 BOLT-NO-HOLE=0 GAP-IN-PACKET=0 SHORT=0 OVERSIZED=0`** — the twelve
> unassemblable bolts no longer exist. **JOINT AUDIT CLEAN.**
> ⚠️ **Residual, stated plainly:** 51 collisions in the band, 31 of them on a bolt axis (the class
> E.4 proved the checker over-reports) and **20 others that were not chased down**. Amir owns the
> "good enough for us" call; this file owns being true about it.

*Read 11/08/2026, pages 1100–1103 (fulltext lines 28446–28531) — **85 lines, the shortest chapter in
part E**. Band `E.05-TRUSS-GIRDER`, grid `1588` at x = 360 000. Plugin v178.*

> *"This function generates a truss girder from the upper and lower cord as well as the diagonals
> and many existing intermediate studs. Upper and lower cords can also be generated from two
> side-by-side shapes."*

---

# THE DIALOG — three tabs

### Dimensions
| field | the manual's words |
|---|---|
| `Type` | *"the different variations of the framework (**parallel or alternating diagonals with or without intermediate studs**)"* |
| `Layout` | which side the bracing is carried out on |
| `Length` | the complete length of the girder |
| `Height` | *"from bottom edge of lower cord up to **intersection of upper edges of upper cords in the middle** (top of roof ridge)"* |
| `Roof Angle` | slope of the upper cord |
| ⭐ `Side Height` | *"from bottom edge of lower cord up to upper edge of upper cord **measured on the sides**. This value **remains constant**, while the value in the height field can change if you adjust the pitch of the roof **and vice versa**."* |
| `Outer`/`Inner Distance` | the axis distance of intermediate studs at the rim / at the centreline |
| `Segment Dist.` / `Segment Numb.` | junction spacing / how many segments |
| ⭐ `Plate Width` | *"the spacing of the shapes, if, for example, upper and lower cords are generated from **two side-by-side (U-shaped) shapes**"* |

⭐⭐ **`Height` and `Side Height` are the same reciprocal pair as E.4's `Centre Height`/`Roof Angle`** —
each moves the other, and `Side Height` is the one that holds still.

### Shapes
`Shape Offset` (which shape the settings apply to) · `Shape Type` · `Shape Class` · `Shape Size` ·
⭐ `Front`/`Back` — *"the selected shapes are added to the corresponding side… you can also generate
a truss girder from **two side-by-side U-shaped shapes**"*.

### Distances
`Top Chord` / `Top Chord Inside` / `Bottom Chord` — the chord's distance from the object frame at the
rim and at the centreline · ⭐ **`Fit Shape Ends`** *(twice, once for diagonals and once for studs)* —
*"the diagonals at upper/lower cord are **cut to fit**"* · `Top`/`Bottom Diagonal` and
`Top`/`Bottom Vertical` — *"the distance of the diagonal / stud from the cord, **if e.g. connective
plates are to be inserted**"*.

⇒ ⭐ **`Fit Shape Ends` and the four distance fields ARE the node detail**: how far each web member
stands off the chord so a gusset can sit between them. That is exactly the dimension this build got
wrong.

---

# ⭐ A THIRD CATEGORY, BESIDE "WORKS" AND "REFUSES"

```
PsStairs.Insert          False      refuses
PsCircularStairs.insert  True       lies -- returns True and builds nothing  (E.2)
PsPortalFrame.insert     0          refuses                                  (E.4)
PsTruss                  --         NO insert(), NO init(), NO PsCreateTruss. NOTHING TO CALL.
```

`PsTruss` is a pure property bag — `copyFrom` · `computeHeightAt` · `retrieveGeometrie` ·
`get_ControledElements` — with 49 properties (`TopChoordKatalog`/`Key`/`Type`, `DownChoord…`,
`Diagonal…`, `Vertical…`, `TrussType`, `Midspan`/`MidspanCount`, `Inspan`, `Outspan`, `SideHeight`).

⇒ ⭐ **"there is no creator" is a different finding from "the creator refuses",** and only the second
is worth probing. Nothing was probed here because there is nothing to call.

⭐ **`PSN_TRUSS.StartAutoCAD.CreateTruss()` does exist** — a Connection Center macro.
⛔ **NOT CALLED.** The plugin's own B.24 record measured `PSN_HollowShapeBracing.InitialCall()`
printing a prompt and parking the session, with the note *"NEVER call it unattended"*, and
`CreateTruss()` is the same kind of entry point. Reaching a truss through the macro's **parameter**
surface (`UserConnection` + `ClsParameters`, as B.24 did) is the open route.

---

# ⭐⭐⭐ `propfull` IS THE ORIENTATION INSTRUMENT — NOT THE HOLE DEPTH

The hole-depth trick from E.1/B.26/E.4 answers *"did the drill land where I meant"*. It does **not**
answer *"which way is this section facing"* — because for an **equal angle both legs are the same
thickness**:

```
L100X10 and L60X6, drilled at the member axis:
   from +Y = 10 · from +Z = 10 · for rot 0, 90, 180 AND 270
```

**Four rotations, one reading.** The instrument is blind here. `propfull` is not:

| `rot` | XAxis | YAxis | ZAxis |
|---:|---|---|---|
| 0 | +x | +y | the member axis |
| 90 | +y | −x | the member axis |
| 180 | −x | −y | the member axis |
| 270 | −y | +x | the member axis |

⇒ ⭐ **`rot` rotates the part's XAxis/YAxis about its ZAxis, and `propfull` reads all three back in
one call.** Read the orientation; do not infer it.

---

# ⭐ TWO LAYOUT RULES THIS BUILD PAID FOR

**① A bolt group is laid out along the MEMBER'S OWN axis.** Offsetting the two bolt positions in
world X bolted every vertical and failed every diagonal — a diagonal's leg runs along the diagonal.
**6 of 12 ends, purely from walking the wrong axis.** Same family as E.1's *"stagger the groups
along the member"*.

**② The seating rule caught me with it written down.**

```
L60X6 leg, measured in situ:  y −30 … −24
gusset as first placed:       y  +5 … +15      -> 35 mm apart, 12 refusals
```

E.1's sentence is in the skill — *"an angle's material is at the EDGE of its envelope, not at its
axis"* — and the answer was **already in the model**: the holes on the failed members named the leg
position exactly.

> ### ⭐ WHEN `boltparts` SAYS "holes further apart than Gap distance", READ THE HOLES.
> They contain the geometry you got wrong. Both E.4's far-flange drilling and E.5's seating error
> were diagnosed in one call each, from holes that were already there.

---

# ⭐⭐⭐ THE NODE, DETAILED ONCE — THE THREE-BAND RULE

Five passes failed because of ONE fact I never checked: **chords and web members were both placed
with their axes at y = 0, so both had their legs in y −30…−24 — the same volume.** That single error
produced every symptom: the interferences, the three-ply packets, the twelve short bolts, and the
look Amir called appalling.

The detail, worked out before touching the model this time — **three y-bands that touch and do not
interpenetrate**:

```
chord       leg  y −30 … −24        axis y = 0
gusset           y −40 … −30        welded to the chord leg's face at −30
web member  leg  y −46 … −40        seated on the gusset's outer face at −40
```

> ### ⭐⭐ AN ANGLE HAS TWO LEGS, AND ITS ENVELOPE IS SQUARE.
> Seating one leg is not enough. The **outstanding** leg sweeps the full 60 mm of the envelope, and
> at `axis y = −16` it ran from −46 all the way to **+14** — straight through the gusset and into the
> chord. That was the 29 collisions sitting exactly on the gusset mid-plane.
>
> ⇒ **Place an angle by its ENVELOPE (`axis ± half the leg`), not by the one leg you care about.**
> `axis y = −70` puts the envelope at −100…−40, and `rot=180` puts the seated leg on the **+y** edge
> at −46…−40, so the outstanding leg points away from the joint.

⚠️ **And the probe could not tell me this either.** An L60×6 envelope is **square**, so the
y-footprint reads −30…+30 for `rot` 0, 90, 180 **and** 270 — four rotations, one answer. Both
instruments (hole depth at the axis, footprint by drilling) are blind to rotation on an equal angle.
**Only `propfull`'s XAxis/YAxis answers it.**

⇒ Every bolt now crosses **exactly two plies**: gusset + chord = 20 mm, gusset + web = 16 mm. That
is why `SHORT` went from 12 to 0.

---

# WHAT IS IN THE BAND

```
length 6000 · 4 segments × 1500 · side height 800 · roof 10° · ridge z 1328.9
3 chords L100X10 (axis y 0) · 9 web members L60X6 (axis y −70, rot=180, set back 60 from each node)
   2 end posts + 3 verticals + 4 diagonals
10 gussets 320×320×10 (y −40…−30) · 40 bolts M16 DIN7990          = 62 parts

vfy_fit       bolts=252  OK=252  BOLT-NO-HOLE=0  GAP-IN-PACKET=0  SHORT=0  OVERSIZED=0
JOINT AUDIT   CLEAN -- every member bolted or declared welded
collision     51 in band: 31 on a bolt axis, 20 others NOT chased down
```

---

# 🛑 THE PROCESS LESSON, WHICH OUTLASTS THE CHAPTER

Six build passes on one truss. Passes 1–5 each fixed what the last measurement shouted and each
exposed the next fault: seating → bolt axis → missing end posts → node interference → short bolts.
**Pass 6 worked because it started from the joint instead of from the last error message.**

> A truss node is detailed **once** — envelope bands, edge distances, packet thickness, bolt length,
> set-back from the chord, which way the outstanding leg points — and then built.
> Amir's verdict on pass 5 was *"appalling"*, and he was right. The cause was **method, not
> knowledge**, and the same root as E.4's frame: there a green `vfy_fit` replaced looking at the
> thing, here the next measurement replaced designing it.

---

# Still open
* ⚠️ **20 collisions in the band were not chased down** — closed on Amir's call, recorded here.
* ⬜ **Amir's own node detail** is still unstated: single angles on a one-sided gusset (what is built)
  or **pairs** with the gusset between them, and whether EB welds its trusses at all.
* ⬜ **4 web ends and 3 chord joints did not bolt** — the shared-gusset hole accumulation.
* ⬜ `Fit Shape Ends` and the four distance fields — read, and they are precisely the fields that
  encode the node offsets this build guessed at.
* ⬜ The twin-shape (`Plate Width`, `Front`/`Back`) chord variant — read, not built.
* ⬜ `PSN_TRUSS.UserConnection` + `ClsParameters` — the non-interactive macro route, as B.24 did.
