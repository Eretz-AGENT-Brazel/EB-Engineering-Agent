# What the manual says that the API alone never tells you

*Distilled from reading `manual_fulltext.txt` (1,179 pp) chapter by chapter on 06/08/2026.
Seven chapters closed completely: `B.12.6` · `B.13` · `B.14` · `B.17` · `B.18` · `B.28` · `B.29`.
Full notes: `EB PROSTEEL AGENT\api+knowledge-develop\knowledge\learning\manual\<part>\MANUAL-NOTES-*.md`.*

> **The realisation that changes the learning economics: THE MANUAL IS THE API DOCUMENTATION.**
> Every dialog field maps one-to-one onto a property. `Create Group` → `CreateGroup`,
> `Gap` → `DistanceToSupport`, `Rotate Connection` → `PlateIsRotated`, `Facet Horizontal` →
> `TopHaunchPlateFacetDistance1`. Reflection gives the *names*; only the manual gives the
> *semantics* — what happens when `Middle = 0`, when `Left/Right` matter, what a
> prerequisite is. **All 1,179 pages are API documentation.**

---

## 1 · The unit of work is not what I assumed

| I built | The software's unit | Chapter |
|---|---|---|
| a loop of single holes | a **drill hole FIELD**, one operation | `B.14` |
| a pile of handles | a **GROUP** — "functions apply to the complete group, even if you select one part" | `B.28` |
| hand-drawn rib polygons | a **CHAMFER** = 3 parameters | `B.13` |
| my own duplicate hash | **positioning** with declared tolerances | `B.29` |
| drilling every copy | **Clone Manipulations** — drill one, transfer to all | `B.4.5` |

**And they interlock.** Group gives the unit; positioning defines "the same part"; then transfer
and comparison work. Build order is therefore:
```
one detail → GROUP → POSITION (with tolerances) → replicate / transfer / compare
```
Not: build → replicate → drill → check.

## 2 · Drill hole fields (`B.14`)

```
Number1*Pitch1, IntermediatePitch1, Number2*Pitch2, ...
   Shape/X Dir : along the profile      Cross/Y Dir : across it
   example     : x = 2*60,200,1*,200,3*40   y = 2*100
```
- Longitudinal only ⇒ leave Y **empty**. Crosswise only ⇒ X **must** contain `1*`.
- One field **cannot** mix one-hole and two-hole crosswise groups.
- **`W` instead of a pitch uses the shape's own marking gauges** (`2*W`) — so bolt gauges
  are never invented. `PositionSelection.kPitch` is the same idea as an enum.
- **`Diameter` is the BOLT diameter, not the hole.** Hole = bolt + `Workloose`.
  Measured: `dia=23, play=3` produced a **⌀26** hole; `dia=20, play=3` produced **⌀23**.
- ProSteel's default clearance is **2 mm**; Eretz Barzel's rule is **3 mm** — always send it.
  A per-diameter clearance table exists (`Global Settings / Bolts`) and would encode it once.
- **Edge-distance table**: per hole diameter, separately for shapes and plates.
  ⚠️ *"the warning is only a hint; the drill hole will nevertheless be inserted"*, and
  *"the message might not appear before end of the action"* — it never blocks.

### Slotted holes — closed 06/08 after being open since lesson 2
`PsDrillObject` has **no slot-length setter**. The manual calls the dimension
**`Rectangle Hole Axis`**, and the only candidate is **`SetAxisDistance(len)`** — verified:

```
two identical plates, one round one slotted (slot=40):
  lhm=0  slot reported as ONE CENTRED hole, slotted flag 0   <- blind
  lhm=1  one end,  flag 1
  lhm=2  BOTH ends: x=2580 and x=2620  ->  40 mm exactly
```
- **`LongHoleMode` is a READ mode, not a write mode** — hence a constructor argument of
  `PsSingleHoleArray`. **Only `kDoubleHole=2` reveals a slot's length.**
- `getMaximalLength` returns **0** in every mode — it is not the instrument.
- ⚠️ The old code used `SetHoleStep(slot, dia)` — that is a **step hole**, a different feature.

### Hollow sections — measured, and invisible to counting
```
SHS 200x200x8, one hole:
  without SetIgnoreInnerContour :  z 100 -> 92    =   8 mm  = ONE WALL
  with it                       :  z 100 -> -100  = 200 mm  = both walls
```
The hole **count is identical either way**. Always set `IgnoreInnerContour` on RHS/SHS/CHS.

Also available and previously unused: `SetDrillType` (top / down / **both** flanges),
`SetHoleBoltType` (**shop bolt vs site/montage bolt**), `SetHoleType`
(normal / blind / weld-crack), `TakeoverDrills(source, target)`.

## 3 · Connections (`B.17`, `B.18`, `B.20`, `B.22`)

- **Pick order: the shape TO BE CONNECTED first, then the SUPPORT.**
  `SetConnectionObjectId` = connected, `SetSupportObjectId` = support. Support is optional
  and changes the result (no support ⇒ a plain end plate).
- **The connection CUTS the connected member.** Measured: IPE300 4850 → 4840. Always
  re-measure length after creating a connection.
- `Automatic` layout: the **critical angle is ~45°** between splice and standard plate, and
  **flange-type must be requested explicitly** — otherwise the plate always goes on the web.
- **`Length = 0`** makes the plate length variable, driven by `Offset Top` / `Offset Bottom`.
  Positive offsets shrink the plate inward; negative ones extend it beyond the section.
- **`Gap` = `DistanceToSupport`**, *"to consider finishing tolerances"* — this is the 18.5 mm
  offset seen in Bentley's own sample model.
- **`Rotate Connection` = `PlateIsRotated`** — turns the whole connection 180° when top and
  bottom came out swapped. **This is the fix for the flange-orientation class of error**;
  no delete-and-rebuild needed.
- Hole spacing semantics: if `Upside = 0` **and** `Downside = 0`, only `Middle` counts;
  if `Middle = 0`, holes distribute **evenly** between the outer two. `Left`/`Right` only
  matter with 4 rows. `Offset` is measured from the top (negative ⇒ from the bottom).
- ⚠️ **Stiffener templates are a LIVE LINK**: editing a template **retroactively changes
  stiffeners already in the model**.
- `Support Stiffeners` have a prerequisite: a bottom-flange haunch **with** a stiffener **and**
  a cover plate — otherwise nothing is created.
- **DAST connections are selected BY LOAD**: enter kN and only connections that carry it are
  listed, each with designation, plate size, hole counts, whether stiffeners/backers/
  strengthening are required, and **`Af`/`As`** (flange/web weld thickness).
  ⚠️ Phase 2 — knowing it exists is Phase 1; using it to judge a design is not.
- User-defined connections live in a **dBASE** database — the route to encoding a
  company-standard connection set (`PsDBaseDatabase` exists in the API).
- **Purlin** = roof girder + purlin **socket** + purlin **course**, and `SetPurlin2Id` means it
  expects **two purlin segments**, not one continuous purlin.

### Base plates (`B.18`) — three corrections to what I believed
1. **Shortening = plate thickness + grout**, not plate thickness.
   Measured: grout 0 → −20; grout 35 → **−55**. My lesson-4 measurement was right for
   *one case* and I generalised it.
2. **`Tie Bolts` "are only displayed as symbols and cannot be detailed"** — the graphic-only
   anchor length is a **software limit, not Amir's preference**. Nothing to fix there.
   (Their measured weight is 0, consistent.)
3. **Inner and Outer are two SEPARATE hole fields**, not variants — hence
   `HoleDistance*Inner` vs `HoleCount*Outer` in the API.
   ⚠️ *"Outer drill holes are only created if BOTH axes have a valid description."*
- `Use Dowel` creates dowels as **volume bodies** from a **database file** —
  `Ks_VolBody` is exactly what Amir's anchors measure as, and `Data\Bolts\Duebel.mdb`
  is that database. A live lead for the anchor problem.
- `Form Group` = `CreateGroup`; `In Shape Direction` = `PlateInShapeDirection`.

## 4 · Groups (`B.28`) — they encode how steel ships

| Level | What it is on the shop floor |
|---|---|
| **Subgroup** | purchase / stock parts, preassembled |
| **Component part group** | **what ships to site as one piece** |
| **Assembly** | what is combined **on site** — has **no main part** |

- Group = main part + accessories. **Group structure feeds the parts lists and the automatic
  2D workshop drawings** — so it is not a modelling convenience.
- Export/import a group as a block **keeps the structure**, but ⚠️ *"Do not use the standard
  AutoCAD command"* — and explode with **`PS_EXPLODE`**.
- **`Check Groups`** is a whole QA suite: orphan parts (no group), groups without a main part
  (**they get dissolved**), single-part group release, search by posnum (`5,7,17-28`),
  and **`Compare+Modify`** — *"groups with the same position number that are not recognised as
  identical will be corrected"*. **That is exactly the lesson-5 failure.**
- Nested groups: selecting a part returns the **lowest** level; you must climb explicitly.
- ⚠️ `Check Groups` is **not in the .NET API** (all 8,622 public types searched). The identity
  engine underneath it **is**, via `PsCreatePositioning`.

## 5 · Positioning (`B.29`) — infrastructure, not an output step

Three separate chapters depend on it: Clone Manipulations, Compare+Modify, Group Detection.
**Without position numbers, none of them work.**

- **Column vs beam is decided BY ANGLE.** `SetColumnTol` / `SetBeamTol` are **angle**
  tolerances (relative to the WCS XY-plane), not dimensional ones.
- Two independent ways to recognise equal parts:
  - **geometry** — every outer edge compared. `SetMinLineLength` (short lines ignored),
    `SetLengthTol`, **`SetHolesTol` = deviation of the drill-hole AXES** (position, not
    diameter), `SetWeightTol`.
  - **volume** — a real volume comparison with tolerances as a **percentage** (0.1 % parts,
    0.2 % groups).
- Position number and **shipping number** are two separate fields (`Posnum`, `Sendnum`).
- Five independent counters: single / subgroup / group / assembly / connection.
  ⚠️ Start each at **0** to have numbering begin at 1.
- Connection naming: consecutive · **PosNum+PosNum** (connected + supporting) · PosNum+Index.
- `B.29.4` has a **part-to-part diff**: *"you can see in detail where the parts differ"*.
- `OrigPosnum` is protected — changed only by its own command.

## 6 · Plate editor (`B.13`) — and what `PsEdgeChamfer`'s fields mean

- **Chamfer a corner**: `Layout` = **straight / convex / concave**, `Radius/1st Edge`,
  `2nd Edge`. Amir's 80×80 chamfer on a 120×120 rib is exactly `straight, 80, 80` —
  **three parameters, not a drawn polygon**.
  This also explains the stiffener template names: `half/full` × `chamfered/convex/rounded`.
- **Edge processing**: six kinds; `Top Side` / `Bottom Side` set **separately**;
  **`Var1`** = first-edge length **or** rounding radius **or** seam depth;
  **`Var2`** = second-edge length **or** seam height. ⇒ that is `TopVar1`/`TopVar2`/
  `DownVar1`/`DownVar2` in `PsEdgeChamfer`, whose meaning shifts with `EdgeLayout`.
- `Selected Edge`: edges are **numbered**; `0-1, 2-3` selects two opposite sides, and
  **start == end means all round**.
- **The software states its own limits** — `Min. Radius` and `Max. Height` are displayed.
- Contour editing is **Boolean** (`Add`/`Subtract`/`Common`) with a **closed** polyline,
  `Distance` offset, `Milling Width`, `Depth`, `Continued` — not wholesale polygon replacement.

## 7 · Copy, mirror, and transferring work (`B.4`)

- Direction can be **constrained** (3D / 2D / X / Y / Z / Free) — *"prevents points being
  selected that are not in the proper plane"*.
- **`Turn+Copy`** rotates a copy in one operation. `B.4.6` distributes rotated copies with a
  vertical offset (spiral stairs).
- **Clone Manipulations** transfers `Cuts` · `Drill Holes` · `PolyCut` · `Notches` · `Boolean`
  from one part to all parts **with the same position number**.
  ⚠️ The transfer is relative to **each part's own coordinate system** — a part whose origin
  is on the other end receives the hole mirrored. A silent-error source.
- Drill holes can also be adopted part-to-part directly (`B.14.1`), and `Shift` picks an
  **XRef** part.

## 8 · Collision check (`D.5.1`)

- Works on part **volumes**; creates visible collision solids you can step through.
- **`Mounting Area`** — *"helps you check whether bolts can be MOUNTED or not"*, using the
  bolt's installation space. That is a **buildability** check, not just overlap.
- `Min. Volume` suppresses noise from tolerance-sized overlaps.
- ⚠️ Cost grows with the **square** of the part count — *"more reasonable to check a certain
  junction point at a time"*. Do not run it on a whole model by default.
- **XRef drawings ARE evaluated** here.

## 9 · Driving the software from code — what actually works

| Route | Verdict |
|---|---|
| **.NET classes** | **The primary path.** No dialog, a return value, measurable |
| **`Editor.Command`** (in `accoremgd.dll`) | Works — `PS_REGEN` proven. But **`PS_` commands are dialog-driven**; `PS_POS` opened *"ProSteel Positionflags and Positioning"* and did nothing headless. **Not general automation** |
| `PsCreatePositioning.Internal*` | The only remaining route to numbering from code. **Unverified — a guessed call order is invention** |

### 🔴 A dialog can block the software while the API says it is idle
Measured 06/08: with that dialog open and blocking, AutoCAD reported
`quiescent = True`, `CMDACTIVE = 0`. **Amir saw the dialog; the API did not.**
⇒ `_quiescent()` means "no command-line command", **not** "ready".
`eb_api.modal_dialogs()` enumerates visible `#32770` windows; `run()` returns
**`EB_DIALOG`** rather than queueing behind one. ⚠️ The detector found the dialog on a scan
but has **not** been proven end-to-end on a live one — still to verify.

## 10 · Names seen in the manual, then confirmed in my own model

```
BRFL 300x20          <- B.20 "Turn Flat": FL 110x10 becomes BRFL 250x10
Tie Bolt, weight 0   <- B.18 "anchor bolts ... cannot be detailed"
M 16x45 -> hole 23   <- template holeDia 16 + play 3 = Amir's M16→⌀19 rule
```
