# E.5 Structural Element — Truss Girder

> ## ⏸️ THIS CHAPTER IS **NOT CLOSED**.
> The band holds a truss and the JOINT AUDIT is clean, but **12 bolts have a negative `spare` —
> they cannot be assembled — and 16 real member interferences remain at the nodes.**
> `PROGRESS.md` carries it as ⬜. The findings below are measured and stand on their own; the
> artefact does not.

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

# WHAT IS IN THE BAND, AND WHAT IS WRONG WITH IT

```
length 6000 · 4 segments × 1500 · side height 800 · roof 10° · ridge z 1328.9
3 chords L100X10 · 9 web members L60X6 (2 end posts + 3 verticals + 4 diagonals)
10 gussets 320×320×10 seated at y −40…−30 · 40 bolts M16 DIN7990        = 62 parts

JOINT AUDIT   CLEAN -- every member bolted or declared welded
vfy_fit       bolts=252  OK=240  BOLT-NO-HOLE=0   ⛔ SHORT=12, worst spare = −5 mm
collision     48 in band -- 32 on a bolt axis, ⛔ 16 REAL member interferences
chord joints  8 of ~11 bolted
```

⛔ **12 bolts with a negative `spare` cannot be assembled** — the same class Amir's B08 audit flagged
("12 bolts in B.26 that cannot be assembled"). ⛔ **16 interferences remain** even after the web
members were set back 60 mm from each node.

## 🛑 THE PROCESS LESSON, WHICH MATTERS MORE THAN THE GEOMETRY

Five build passes on one truss. Each fixed what the last measurement shouted, and each exposed the
next fault: seating → bolt axis → missing end posts → node interference → short bolts.

> **That is convergence by trial inside the model, not detailing.** A truss node is detailed **once**
> — edge distances, packet thickness, bolt length, member set-back from the chord, leg orientation —
> and then built. Amir's verdict on the result was *"appalling"*, and he was right. The cause was
> method, not knowledge.

⚠️ **It is the same root as E.4's frame**: there, a green `vfy_fit` replaced looking at the thing;
here, the next measurement replaced designing the thing.

---

# ⏸️ What E.5 needs before it can close
* ⏸️ **Amir's node detail.** There is no EB details document — the detail is his. Two questions
  stand: single angles on a one-sided gusset or **pairs** with the gusset between them, and how much
  **set-back from the chord face**. And whether EB welds its trusses at all, which would remove the
  bolt discussion entirely.
* ⛔ The 12 unassemblable bolts and the 16 interferences.
* ⬜ `Fit Shape Ends` and the four distance fields — read, and they are precisely the fields that
  encode the node offsets this build guessed at.
* ⬜ The twin-shape (`Plate Width`, `Front`/`Back`) chord variant — read, not built.
* ⬜ `PSN_TRUSS.UserConnection` + `ClsParameters` — the non-interactive macro route, as B.24 did.
