# B.24 Dynamic Bracing — chapter notes

*Read end to end 09/08/2026, pages 333–346 (fulltext lines 7855–8206): B.24.1 Common Settings ·
B.24.2 Shape Bracing · B.24.3 Rod Bracing · B.24.4 Pipe Bracing. Command **`PS_VERBAND`**
(*Verband* = bracing).*

> *"This function generates a wall bond or bracing… **The entire bracing including gusset plate is
> generated.**"*

⭐⭐ **That sentence closes the gap left open by B.23.** `PsGussetConnection` has no creator, but the
gusset does not need one: **the bracing command produces it.** A gusset plate in ProSteel is not a
thing you make — it is a thing a bracing *has*.

## Creating one

> *"first place your **user coordinate system over the bracing plane**… You are now prompted to
> click on a **system line** of the bracing. Finally, click on **both shapes** to which the bracing
> is to be joined."*

Three inputs: the plane (as the UCS), one system line, two host shapes.

## B.24.1 Common Settings — the parent folder

| field | meaning |
|---|---|
| `Shape Selection` | three boxes: **catalogue · type · size** |
| `Plate Thickness` | the gusset thickness |
| `Edge distance` | outermost edge of the bracing bar → the boundary edge of the bracing *(usually a shape edge)* |
| `Round shapes` | rounds off **the length of the shape** |
| `Plate width` | the **minimum** gusset width at the shape |
| `Position` | the gusset relative to the bracing plane: `Front Edge` · `Center` · `Rear Edge` |
| `Form` | the **shape** of the gusset plate |
| `Opening Angle` | the gusset's opening angle towards the bar, for the `Triangle bent` form |
| `Cross Bracing` | a cross stay; otherwise **one bar only**, on the clicked system line |
| ⭐ `Welded Bracing` | *"the bracing is welded in its entirety. **No borings are added** in that case"* |
| `Form Group` | group the bracing elements |
| `Symmetrical` | a cross stay stays symmetric when edited by grips; otherwise each bar moves alone |
| ⭐ `No Gusset Plates` | **no gussets at the supporting shapes** |
| `Dynamic` | the bracing updates live in the dialog — *"if you would like to modify many values, you can deactivate this option"* |

⭐ **Additional boundary edges** — the same idea as B.23's limiting shapes, but as *lines*:
> *"This method enables you to **lengthen a gusset plate up to the base plate**… If the vertical of
> a gusset plate end point cuts this boundary line, the gusset plate is **extended up to this
> point**. Otherwise, the **nearest end point of the line** will be accepted."*

Plus buttons to **stop gusset creation at the end of a bar**, and to **include or exclude a bar
that does not belong to the bracing** from the gusset calculation.

## Bolts folder

`Bolts` (type) · `Dm` (**hole** diameter) · `Work loose`.

⭐ Same `*n` grammar as B.23, but the multiplier is different here:
> *"You can either specify absolute values or **how many times the amount of the hole diameter**.
> If the entries have to be a multiple of the hole diameter, put an **asterisk (\*)** in front."*

⚠️ B.23 multiplies the **bolt** diameter; B.24 multiplies the **hole** diameter. Same asterisk,
two different data.

| field | |
|---|---|
| `Shape Direction` | holes along the bar — ⭐ *"(on **both ends**)"* |
| `Edge-1st hole` | bar end → first hole centre |
| `Hole-Hole` | spacing along the bar |
| `n-th Hole-Edge` | last hole → **the outer edge of the gusset plate** |
| `Cross Direction` · `Distribution` | count and spacing across the bar |

## B.24.2 Shape Bracing

⭐ **`Shape Position` — seven ways to place the bars against the gusset:**

| option | |
|---|---|
| `Front` / `Back` | all bars in front of / behind the plate |
| `Cross` | one in front, one behind |
| `Centered` | in the bracing plane |
| `Double` | one each side — **4 shapes for a cross stay** |
| `Replaced` | one each side, one offset left and one right — **4 shapes** |
| `4-Times` | one on each of front, rear, left, right — **8 shapes for a cross stay** |

`Shape distance` (for offset or 4-Times) · `Rotation Angle`.

**`Shape Insertion`** — which line the bars sit on: `Centred` (the shape axis) · `COG Line`
(centre of gravity) · `Pitch Line` · `Diagonal` (upper-left corner to lower-right) ·
`Inverse Diagonal`.
**`Hole Position`** — independently: `Centred` · `COG Line` · `Pitch Line`.

⇒ The insertion line and the hole line are **separate choices**. A bar can sit on its COG line and
be drilled on its pitch line.

`Offset` — projection of the **central** gusset past the bar edges; negative shrinks it.
`Shorten` — shorten the shapes after generation. `Mirror Shapes`.
⭐ `Center Hole` — at a cross stay with `Crossed`, *"a **common hole** is drilled into the shapes
that are crossing each other."*
⭐ `Divide All` — *"all shapes of a cross stay are **separated at the center gusset plate**."*

**Bind Plates** (batten plates between opposing shapes): `Distance` *(the program divides regularly
and rounds)* · `Offset` · `Number` of bolts · `Edge-1st hole` · `Hole-Hole` · `Dia` · `Work loose` ·
or **`Weld`** instead of bolts.

## B.24.3 Rod Bracing — three layouts

| layout | what it makes |
|---|---|
| **Tension Rod with Cam** | a **traversing** rod: *"The shapes connected with the bracing are **penetrated** and fastened in the back with a tension element."* `Offset` · `Hole Dia.` and `Slot Axis` — ⭐ **an oblong hole** · `Part Description` · `Create Element` with `Width`/`Thickness`/`Radius` |
| **Turnbuckle** | two rods joined by a tension element; a **butt strap welded to the rod end** connects to the shapes via a gusset. `Sliced` = the strap is slit and the rod welded in the centre; `Welded` = the rod is welded to the strap. Strap `Length`/`Width`/`Thickness` · `Offset` (overlap) · `Gap` (production tolerance for `Sliced`) · `Turnbuckle` length · `Dm` · ⭐ `Distance` — *"if the value **0** is indicated, the element will be placed in the **centre** of the bracing segment"* |
| **Tension Rod** | a rod with a welded-on connection element: `Butt Strap` length/width/thickness · `Bolt Diameter` · `Outer Radius` |

⭐ **Bracing Catalogues:** *"Databases may be generated for the tension elements of all three
types… select a **dBASE-file**… The dialog entries will then be **filled with the corresponding
values**."*

## B.24.4 Pipe Bracing

**`Butt Strap Type`** — five ways to join a pipe to a strap:
`Inserted` (welded into the pipe) · `Pipe sliced` (the pipe is slit) · `Strap sliced` (the strap is
slit) · `T-Shape` (a T-section welded on) · `Endplates` (a head plate on the pipe, then a strap).

`Butt Strap Length` / `Width` / `Thick` · `Butt Strap Offset` (overlap) ·
⭐ `Escape Gap` — *"width of the gap between pipe opening and butt straps"*.
`Create Group with Pipe` · `With Bolts`.

**Endplates group** (when `Endplates` is chosen): `Width` (or diameter) · `Thickness` ·
`Type` = `Edge` (square) / `Round` / ⭐ `Use Polyplates` *(the head plates become plates)*.

---

## The API — and it is complete

`Bentley.ProStructures.StructuralObject.PsBracing` — a full getter/setter pair for **every** field
above, plus:

`insert(PsPoint Origin, PsVector X_axis, PsVector Y_axis)` → **Boolean** — the creator.
`setStartPoint` / `setEndPoint` — the system line.
⭐ **`setBorderObjects(Int64 Obj1, Int64 Obj2)`** — the two host shapes, i.e. the manual's *"click
on both shapes to which the bracing is to be joined"*.
`addBorder(Start, End)` / `deleteBorder(Index)` / `getBorderCount` — the additional boundary edges.
`addAdditionalBracing(ShapeId)` / `removeAdditionalBracing` — include or exclude a foreign bar.
`recalcPoints()` · `copyFrom(PsBracing)` · `listInformation()`.

### The enums, resolved by full type name

```
Bentley.ProStructures.BracingType   = kNormBracing, kRodBracing, kPipeBracing
Bentley.ProStructures.BracingLayout = kAtFront, kAtBack, kCrossed, kCentered,
                                      kDoubled, kButterFly, kQuatro
Bentley.ProStructures.ShapePosition = kShapeStart, kShapeMiddle, kShapeEnd
```

⚠️⚠️ **The names are cross-wired.** `BracingLayout` is the manual's **`Shape Position`** field
(Front / Back / Cross / Centered / Double / **Replaced = `kButterFly`** / **4-Times = `kQuatro`**).
Meanwhile the enum actually *called* `ShapePosition` is what `setPlatePosition()` takes — the
**gusset's** position across the bracing plane. And there is a separate
`setShapePosition(Int32)` with no enum at all.
⇒ Three similarly named things, none of them meaning what its name suggests. **Resolve by
signature, never by name.**

⚠️ `setBoltStyleCRC` / `setWeldStyleCRC` are **CRC only** — no style name on this class, as on the
splice class in B.21.


---

# MEASURED 09/08/2026 — the creator does not fire

*Band at x ≥ 190000: a portal of two HE300B columns and an IPE400 beam, built as the host frame.*

## ⛔ `PsBracing.insert()` returns **false**

Five configurations, every one `insert()=False` with **zero objects created**:

| tried | result |
|---|---|
| `setShapeType(kNormalType)` explicit | ✗ |
| catalogue as `DIN WINKEL GLEICH` (the lookup name) | ✗ |
| catalogue as `DIN.DIN_WINK_GL` (the stored name) | ✗ |
| `recalcPoints()` before `insert()` | ✗ |
| minimal — no section, no plate, only the line and the two hosts | ✗ |

⭐ **The setters themselves work.** A read-back taken immediately before `insert()` returned
`cat='DIN.DIN_WINK_GL' size='L90X9' type=kNormalType` — exactly what was written. So the object
accepts and holds its configuration; only the creation step refuses.

The system line was checked against the frame: it runs from `190150` — the left column's face — to
`193850` — the right column's face, so both endpoints sit on their host shapes.

### The UCS hypothesis — tested and REFUTED

The manual insists the **UCS must be placed over the bracing plane** before the command, which was
the leading suspect. Amir approved the change; it was then implemented through the managed
`Editor.CurrentUserCoordinateSystem` rather than the command line, so nothing can be left pending,
with the restore in a `finally` block so a crash cannot leave the frame rotated.

Result, in one line:

```
ucsSetToPlane   insert()=False   ucsRestored
```

⇒ **The UCS was not the missing precondition.** The frame was genuinely set on the bracing plane,
`insert()` still refused, and the frame came back. Verified afterwards from the drawing itself:
`UCSORG (0,0,0)`, `UCSXDIR (1,0,0)`, `UCSYDIR (0,1,0)` — **back at World**.

Six configurations now tested. `PsBracing.insert()` does not create from code under any condition
that could be constructed. `UCS` stays on the allowlist — it was approved on its merits and is
useful elsewhere — but it did not solve this.

## The pattern this completes

Four standalone creators tested today, all refusing:

| class | result |
|---|---|
| `PsCreateWeldFlag.Create()` | **false** — yet a splice connection produced 32 `Ks_WeldFlag` |
| `PsGussetConnection` | **no creator at all** |
| `PsBracing.insert()` | **false** |
| *versus* `PsWebAngleConnection` / `PsShearPlateConnection` / `PsSpliceJointConnection` / `PsStiffenerConnection` | **all create cleanly** |

⇒ **The `Connection.Standard` family works from code. The `StructuralObject` and `Annotation`
families do not** — at least not without a precondition that has yet to be found.

## What this means for B.23's open question

The manual's answer is unambiguous: *"The entire bracing **including gusset plate** is
generated."* So a gusset **is** produced by the bracing command — the reason
`PsGussetConnection` has no creator is that a gusset is not a standalone object at all.
**But that route is not reachable from code either**, because the bracing creator itself refuses.

⇒ B.23's gusset remains **dialog-only**, and the hand-built detail (SHS + UPN, x ≥ 170000) is
currently the only way the agent can produce one.

---

# Two real frames, at Amir's direction (09/08)

The host frame first built for the bracing test was **not a frame**: two columns and a beam whose
axis sat at the column tops, touching, with no connection of any kind. Amir: *"מסגרת כזו… היא דבר
שאינו ניתן לביצוע — אין כאן סכימה יציבה של הקורה על גבי העמודים."* He asked for two more, one
**welded** and one **bolted**.

Both run the beam **along Y**, because an HE/UC column's flanges span X at `y = ±h/2` — a beam
along X meets only the flange tips and the gap between them, never a face.

## Frame 1 — welded · `HE200B` + `IPE300` · x ≈ 190000

The beam butts square against the flange face: measured `y 8100..11900` against faces at exactly
**8100** and **11900**. Zero gap — a weldable fit-up.

⭐ **The weld is not what makes it stable.** Eight stiffeners do: a pair in each column web
opposite each beam flange, at `z = 3350` and `z = 3650` (the IPE300's flange levels). Without
them the beam flange force folds a thin web. `collisions=0`.

11 objects: 3 shapes + 8 stiffener plates.

## Frame 2 — bolted · `UC203x203x46` + `UB305x165x40` · x ≈ 194000

A 20 mm **end plate** welded to each beam end and bolted to the column flange, **12 × M16** —
six per end, three rows of two.

⭐ The beam is **shortened by one plate thickness at each end** so the plate, not the beam, lands
on the flange: plate at `y 8101.6..8121.6`, exactly filling the gap between the flange face and
the beam end.

17 objects: 3 shapes + 2 plates + 12 bolts. `collisions=0`.

## ⚠️ The trap this exposed: the drill picks a flange by PARAMETER

One end bolted, the other refused — **while both reported 6 holes on the plate and 6 on the
column.** The coordinates told the real story:

```
end A ✓  plate  8121.6→8101.6    column  8101.6→8090.6    one continuous hole
end B ✗  plate 11898.4→11878.4   column 12101.6→12090.6   ← the FAR flange, 200 mm away
```

`PsDrillObject` takes a **`flange` parameter (0 top · 1 down · 2 both)** and defaults to the same
local face regardless of the insertion point — the near flange on one side of a frame, the far one
on the other.

⇒ **Always pass `flange=` when drilling a shape.** And the failure mode is the dangerous kind: the
**counts were perfect**. Only reading the coordinates found it, and only the bolts refusing raised
the question at all. **A count is not a position.**
⛔ Holes cannot be removed, so the fix was to delete and rebuild the column.