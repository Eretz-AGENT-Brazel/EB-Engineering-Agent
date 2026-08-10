# B.8 Insert Shapes — chapter notes

*Read end to end 07/08/2026, pages 154–173: B.8.1 Straight Shapes · B.8.2 Bent Shapes ·
B.8.3 Additional Settings · B.8.4 Shape Series · B.8.5 Shape Segment · B.8.6 Girder Position ·
B.8.7 Automatic Insertion.*

> *"The actual design work with ProSteel starts when shapes are inserted into the model…
> First, make sure that the shapes are correctly positioned. **Since the shop drawings are
> based on this position, proceed with care.**"*

The manual's own workflow, in order: pick a view and draw construction lines → insert shapes and
correct → adapt (shorten) and make connections → **copy identical parts**. That last step is
Amir's "build once then replicate", written into the software's own method.

---

## ⭐ The rule that explains the orientation problem

A shape is inserted from **two 3D points**, positioned so that *"if you stood at the end point
and looked into the direction of the starting point, the view corresponds to the depiction on the
monitor."* Two points do not fix the rotation, so a **third** is used:

- you may indicate it explicitly, **or**
- **the two points are perpendicular in the WCS → alignment follows the WCS x-axis**, **or**
- the points are free in space → the x-axis is aligned **as parallel as possible to the WCS
  xy-plane**

⇒ **That is why a vertical HE300B column came out with its web facing a particular way** — and
why an end plate attached on the x side drilled straight through the web. Not a guess to be made:
a documented rule. *(Measured the hard way on 06/08 before this chapter was read.)*

## 5 shape types

| type | what it is |
|---|---|
| **Standard Shapes** | from the delivered database |
| **Special Shapes** | drawn cross-section, saved as a shape |
| **Roof-Wall-Shapes** | user shapes optimised for roof/wall |
| **Combination Shapes** | built from several already-defined types |
| **Weld Shapes** | user-defined, weldable from plates |

`Resolution` (Low/Normal/High) affects only the **display** of special shapes, changeable later.

**`Key`** — *"Each shape has its own clear access key… can be entered directly here to be able to
create **non-standardised shape sizes** of tubes, flat steel, round iron."* ⇒ the route to sizes
that are not in a catalogue.

## Defaults on the insertion dialog

`Material` · `Layer` · `Part Family` · `Detail Style` · `Display Class` · `Area Class` ·
`Description` (can drive colour and layer) · `Item` · `Create Group`
- **`Delta X` / `Delta Y`** — insertion offset, **only enterable when the insertion point is
  'Free'** (the largest point shown on the monitor)
- **`Length`** — *"Inputs in this field **overwrite** the length specified by the insertion
  points."*
- **`Turn`** — rotation about the insertion axis

**Insertion points** are shown on the monitor: corners and centres, plus **smaller** ones
(hole gauge, centre of gravity, manual points on special shapes) and one **bigger** one
(free placing).

## 7 insertion commands

| command | note |
|---|---|
| **ALONG LINE** | uses a line's endpoints. A **cone** appears for asymmetrical shapes to swap start/end = **mirror about the y-axis**. ⭐ **A POLY-LINE gives a cranked shape**, bend radius from the option tab or the minimum if 0 |
| **ALONG 2 POINTS** | two picked points |
| **ALONG DIAGONAL** | the start keeps the chosen insertion position; **the end uses the OPPOSITE one** (centre-lower-edge → centre-upper-edge). For diagonals |
| **ALONG 3 POINTS** | third point defines alignment; dynamic mode auto-on |
| **MULTIPLE ALONG LINE** | several lines at once. Overlapping endpoints get a **mitred cut** (radius 0) or **arcs** of the given radius. ⚠️ *"only safe if you insert along the middle line"* |
| **ALONG VIEW DIRECTION** | needs a fixed `Length` |
| **ALONG CROSS-SECTION DIRECTION** | needs a fixed `Length`; inserts on the UCS xy-plane, going back into depth |

**Additional functions:** `INTERRUPT DIALOG` — inserted shapes stay **connected to the dialog** so
later edits propagate to them; this button breaks the link without deleting ·
`MIRROR` (all connected shapes, by swapping insertion points) · `TURN POSITIVE/NEGATIVE` ·
⭐ **`FETCH INSERTION DATA` — pick an existing shape and adopt its insertion parameters.**
*(That is "learn the settings from the model", built into the software.)*

## B.8.2 Bent shapes — constant radius

`ALONG ARC` (pick an existing arc; its position in space does not matter) ·
`ALONG 3 POINTS` (centre, start, end — **perpendicular to the current UCS**).
⚠️ The 3-point method **cannot place a shape freely in space and cannot make a 180° arc.**
Settings for bent shapes are stored **separately** from straight ones.

## B.8.3 Additional settings

`Height` above the UCS plane · **`Start Offset` / `End Offset`** (at the ends of straight shapes) ·
`Radius` (rounding where shapes are inserted several times) · `Scale` (2D depiction) ·
**`Horizontal Dist.` / `Vertical Dist.`** — spacing when the shape class allows several shapes
(`SHAPECLASSLAYOUT=HORDOUBLE, QUADRUPLE, DIAGONAL`) ⇒ back-to-back angles and the like ·
`Angular Insertion` · `Insert…X,Y Plane` · `Orientate` (rotate in **90° increments** after
insertion) · `Dynamic` · ⭐ **`Reference Points`** — the insertion points are stored **in** the
shape so they can be traced back and dimensioned later · `3 Point Method` · `As 2D-Shape` ·
`Close Dialog` · `Keep Length` · ⭐ **`Enable Chains at Points Input`** — the last point becomes
the **start of the next shape** · `Adjust to Work frame`

## B.8.4 Shape series — why a catalogue can be "missing"

Three lists: **Available Shape Series** (by country, registered by a `*.cfg` in the shape data
directory) → **Available Shape Classes** → **Current Shape**.

⚠️ *"Please note that in **all ProSteel functions** only the shape classes in the right selection
list are offered for selection."* ⇒ **a catalogue absent from the current list is invisible
everywhere**, not just here.

`Metric` / `Imperial` / **`Automatic`** — *"in a metric drawing only metric shape names will be
displayed."* ⇒ relevant to the standing metric rule.
**`Preferred Level`** — shape records carry a priority; show only those at or below a chosen level.

## B.8.5 Shape segment — cranked shapes

Add a **straight** or **bent** segment to the end of a straight or already-cranked shape; delete
the last segment; read and modify an existing one.
- Straight: `Length`
- Bent: `Radius` · `Angle` (opening angle) · `Rotation` (about the shape's longitudinal x-axis) ·
  `Turn Angle`

## B.8.6 Girder position — secondary girders between two mains

Pick main girder 1 **at the end where the laying starts** (*"it is important to pick the correct
end"*), then main girder 2, then give the distribution as **`number*distance`, comma separated** —
**the same syntax as a drill field**.

⚠️ *"the subordinate girders are **in any case** inserted in a way that they are aligned to the
**upper edge** of the main girders (**the position setting in the dialog for shape insertion
doesn't matter**)."*
They can be **notched immediately**, and `Connect AEC-lines` links the static effect lines to the
mains — *"this saves you some work during transfer to a static program."*
ALT while calling → insert a subordinate girder **along a line**.
Individual girders: pick two mains at the relevant ends and give the distances to each end.

## B.8.7 Automatic insertion — ⭐ the grid

> *"A frequent application is e.g. the generation of a **supporting grid** with previously
> defined joints."*

**Insert Columns** — a list where **each line is a column SEGMENT** (`Starting Height` /
`End Height`), then four ways to place it:
1. at a picked position
2. at the **intersection of two picked lines**
3. ⭐ **at every intersection of the WORK FRAME AXES (grid) inside a rectangle** — pick lower-left
   and upper-right
4. the same inside **any polygon**

**Insert Girder** — girders along **all grid axes** in an area, *"finally, these are divided at
all intersection points of the grid axes"*. Be in **plan view** first (girders follow the current
UCS). ALT → polygon area; CTRL → rectangle.

⇒ **This is the lesson-5 exam, native.** Twenty columns on a grid is one command against the work
frame, not a replication loop — and the girders come out already split at every intersection.
**The work frame (B.6) is the prerequisite**, which makes B.6 the natural chapter to read next.

---

## AUDIT 10/08/2026 — what was missing

### The five shape types are five DATABASES, and four were unreachable

The folder is the **catalog**, the `.psp` file is the **section**:

```
Data/UserShapes/<catalog>/<section>.psp      Special / Sopro   68 catalogs, 1528 sections
Data/RoofWall/<catalog>/<section>.psp        Roof-Wall         20 catalogs,  270 sections
Data/CombiShapes/<catalog>/<section>.psp     Combination       15 catalogs,   88 sections
Data/WeldShapes/<catalog>/<section>.psp      Weld               3 catalogs,    4 sections
```

Some catalogs store a **`.dbf` table** instead of `.psp` files (`SCHRAG_z-pfetten`,
`SCHRAG_c-riegel`, `Kantteile`, `Steel Deck`). There the section name is the row's **`KEY`**.

⚠️ **Address a section by its FILENAME.** `Dreiecksbinder/R273x28-H440.psp` reads back with
`name='R244.5x22.2-H420'` — the internal name field disagrees with the geometry; the filename
does not.

```
beam kind=standard   name="HE 300 B"                            (default, unchanged)
beam kind=special    catalog=SCHRAG_z-pfetten  name=Z140-15     Z purlin
beam kind=roofwall   catalog=Bardage           name=4-250-36bx100
beam kind=combi      catalog=Dreiecksbinder    name=R273x28-H440
```

**Families that matter here** — cold-formed purlins (`SCHRAG_z-pfetten`, `SCHRAG_c-riegel`,
`Sadef_zed/cee/sigma`, `sbe_c/z/zeta`, `ayrsh_zeta`, `ayrshire_eb`), crane rails
(`Kranschienen_Form_A`, `krupp_zr/ztg/zth`, `kbk_kran`), Halfen cast-in channels
(`halfen_hl/hm/np/p`), bent sheet (`Kantteile`), decking (`Steel Deck`), stairs (`stair`).

### B.8.2 Bent Shapes — op `bendshape`

`PsCreateBendShape` was never used. It is also **the only creator with `SelectWeldSections`**:
the straight creator has four selectors, the bent one has five. Welded plate girders
(`WeldShapes/I-Profile/I950x300x30`, `Kastentraeger/K900x400`) are reachable **only** this way.

```
bendshape name="HE 200 B" pts=0,0,0;0,2500,0;2000,4000,0        polyline path
bendshape name="RO 88.9x5" circle=1500                          ring
bendshape name=... helix=r,angle,rising,resolution[,left]       helix
bendshape kind=weld catalog=I-Profile name=I950x300x30 pts=...  welded girder
```

⚠️ **A bent shape needs ≥ 3 path points.** Two return nothing — same section name, 2 points
fails and 3 succeeds.

⚠️ **`handle=` (`ConvertFromPolyline`) does not follow arcs.** It creates a shape, and the shape
is not the path: a 90° bulge left a vertex **650 mm outside** the result's bounding box, and
`Update()` on the polyline changes nothing. The op therefore reports **`pathfit=ok`** or
**`pathfit=MISMATCH n/N_vertices_outside_by_Xmm`** on every call — check it. Straight `pts=`
paths always read `ok`.
