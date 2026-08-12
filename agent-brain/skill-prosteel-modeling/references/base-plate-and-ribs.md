# Base plate to floor, and ribs — the worked lesson

*Learned from Amir in learning mode, 31/07/2026, two stages. Source model:
`EB PROSTEEL AGENT/api+knowledge-develop/projects/lesson-4/שיעור-4.dwg`; exam model `מבחן-שיעור-4.dwg`.
The numbers here are examples from these cases — the **method** generalises, the figures do not.*

---

## Stage 1 — creating the detail

### What Amir actually pressed
Two commands did all the steel:

| Command | Result |
|---|---|
| `PS_INS_PROF` | the column — **HE260B**, catalogue `FRANCE.FR_HEB` |
| `PS_GROUNDPL` | the **DSTV base-plate macro** — once, and it produced the whole detail |

Everything else was navigation (`PS_GLOBAL_VIEW`, `-VIEW`, `VSCURRENT`, orbit). He first tried a
**400×16** plate, undid it, and settled on **300×14** — the size is chosen *for the column*, not taken
from a default.

### What the macro produced in one action
Plate · 4 holes · 4 anchor bolts · welds · **and a shortened column**.

### The measurable proof of shortening
```
column HE260B :  z = 14 … 6000     length 5986   (not 6000)
plate         :  z =  0 … 14       "Plate 300x14"
```
The macro **took the plate thickness out of the column** so the design level at the top is preserved.
That 5986 is the length the saw cuts. Miss this and either the column is too long or the plate is
buried below the floor.

### The trap that caught the agent
The plate is created as a **flat profile from `DIN.DIN_FLACH`**, named `Plate 300x14` — it is a
`Ks_Shape`, **not** a `Ks_Plate`. In an earlier model the same thing appeared as `BRFL 300x12`,
`BRFL 300x10`, `BRFL 180x10`; the agent catalogued them as "flats and stiffeners" and rebuilt them as
bare profiles with no holes, no anchors and no joint. **That was the root cause of "main plates with
no holes".**

### Detail parameters (this case)
`300×300×14` · hole **⌀23** (M20) · spacing **200 × 145** · anchors ⌀20 · joint type 13
`Baseplate Connection`: 1 plate + 4 bolts.

**Hole spacing is not symmetric** (200 across, 145 deep) — it leaves the fitter room to get a spanner
in **between the HEB flanges**. Plate 300 on a 260 column = 20 mm each side for the weld.

---

## Stage 2 — the engineer of record asks for more

**The situation Amir posed:** the detail was shown to the structural engineer who signs the building.
He **approved the plate thickness and the hole spacing**, and asked to **enlarge the plate so ribs can
be added** — more "meat" at the joint, contributing to stability.

### What changed, and what deliberately did not
| Parameter | Before | After |
|---|---|---|
| plate length | 300 | **500** |
| plate width | 300 | 300 |
| **thickness** | 14 | **14 — untouched** |
| **hole spacing** | 200×145 | **200×145 — untouched** |
| hole count | 4 | 4 |
| joint count | 2 | 2 |

No new joint, no extra holes, no deleted plate. **One parameter in the existing connection's dialog,
and the joint recomputed the plate** (its geometry moved from `647→347` to `747→247`).

> **The principle: what the engineer approved is frozen. Everything else is a parameter.**
> A design change *tunes* the connection; it does not dismantle it.

### Recording caveat
The dialog edit produced **no AutoCAD command** — Amir opened the joint by double-click / Properties.
Event-based recording catches the *result* (300→500) but not the *action*. To capture such edits, diff
connection parameters before/after (or watch `ObjectModified`), not just command events.

---

## The rib (ריפ)

### How Amir built it
```
DIMLINEAR  →  erase the dimension  →  LINE  →  LINE  →  JOIN  →  PS_PLATE
```
He **measured in place** to decide the size, **drew** the contour as lines, **joined** them into a
polyline, and `PS_PLATE` turned it into a plate. He did not compute vertices.

### The shape, and why
All 8 ribs identical: `-60,60 → -20,60 → 60,-20 → 60,-60 → -60,-60`
= a **120×120 plate with an 80×80 diagonal corner removed.**

The exam drawing's rib: **100×100×10**, 25 mm left at the top edge and 25 at the outer edge →
`-50,-50 → 50,-50 → 50,-25 → -25,50 → -50,50` — a **75×75** cut.

**Why the corner goes:** what remains is the triangle **in the load path** from the column face down
into the base plate. Steel outside that path carries nothing, costs money, and gets in the welder's
way. Secondary reasons: the profile has an inner corner radius that a square rib would not seat
against, and a sharp re-entrant corner is a stress raiser.

⚠️ **Do not assume a fixed chamfer.** ProSteel's stiffener templates (`half/full chamfered`,
`half/full convex`, `half/full rounded`, `ShapeType` 0/1/2) do not fix a size. Measured reality:
**~2/3 of the corner**, not the ~15 mm the agent first guessed from the template list.

### Placement
```
15:08:26  -VIEW  →  15:08:28  PS_COPY
15:08:50  -VIEW  →  15:08:53  PS_COPY
15:09:36  -VIEW  →  15:09:38  PS_COPY
```
`-VIEW` four times, each **2–3 seconds before** a placement run, because `PS_COPY` and `MIRROR` work in
the current view plane. **Align the view to the plane the rib lives in, then place.** Eight ribs came
from **one**, via `PS_COPY` ×7 + `MIRROR` ×2 + `ROTATE`.

---

## Reproducing this through the API (what passed the exam)

Task: SHS 200/200/8 column 3.5 m · base plate 400×400×20, 4 holes ⌀23 at 300×300 · 8 ribs
100×100×10 with the drawing's chamfer. All 18 acceptance checks passed after two corrections.

```
1. column     beam name=RQ200x8 catalog=DIN_QUADRATROHR_KALT p1=0,0,0 p2=0,0,3500
              (SHS 200x200x8 lives in the DIN cold-formed square-tube catalogue)

2. base plate connbase handle=<col> l=400 w=400 t=20 holedia=23 hx=300 hy=300
                       anchors=1 anchordia=20 anchorgrip=400 anchorkey=30
                       anchordetail=1 shorten=1
              -> plate z 0..20 (ON the floor), column z 20..3500 (L=3480),
                 4 holes dia 23 drilled by the joint itself, 4 anchors with nuts

3. ribs       for each of 4 faces, two positions +-50 from the face centre:
                 plate  center=<gap centre> l=100 w=100 t=10 ex/ey/ez per face
                 setpoly handle=<h> pts=-50,-50;50,-50;50,-25;-25,50;-50,50
              (a rectangle placed correctly, then its contour replaced — the
               API equivalent of Amir's LINE+JOIN+PS_PLATE, and it preserves holes)
```

**Rib positions:** ±50 from each face centre keeps them clear of the anchor holes at (±150, ±150), so
nothing blocks spanner access.

### The two corrections, both instructive
1. **Plate below the floor.** Without `shorten=1` the plate came out `z −20…0` and the column stayed
   `0…3500`. The lesson is called "base plate **to the floor**" — z=0 *is* the floor, nothing belongs
   under it. Fix: remove the joint with its parts (`RemoveAllLogicalLinks(deleteParts)`) and rebuild
   with `ShortenShape` on.
2. **Invisible anchors.** `anchors=1` alone gives four blocks with **diameter 0** — present in the
   database, nothing to see. `AnchorBoltDiameter=20` gave bodies (`20×20×420`); the **nut** only
   appeared with `AnchorBoltKeySize=30` (SW30 for M20), bbox → `34.6×30×425`, where 34.6 = 30/cos30°
   is a hex nut measured across corners.
   **Amir's verdict: unsatisfactory — this is configured automatically in the software and should
   appear directly.** Open item for the next lesson, including washers.

## Acceptance checklist for a column base detail
- [ ] column profile and **design level** correct
- [ ] column **shortened** by the plate thickness; plate sits **on** z=0, nothing below
- [ ] plate size and thickness per drawing
- [ ] hole **count**, **diameter** and **spacing** per drawing; edge distance checked
- [ ] holes read back from the model (`PsSingleHoleArray`), not inferred
- [ ] the plate exists as a **connection**, with its parameters readable
- [ ] anchors present, correct diameter, **with nuts**, visible
- [ ] ribs: count, size, **contour matches the drawing**, unique-vertex count > 4
- [ ] ribs seated on the plate, clear of the holes, symmetric about the column
