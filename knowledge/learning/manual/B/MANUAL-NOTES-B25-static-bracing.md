# B.25 Static Bracing — chapter notes

*Read end to end 09/08/2026, pages 346–355 (fulltext lines 8207–8428): B.25.1 Settings ·
B.25.2 Creation of Bracing Parts · the worked roof-bracing example.*

## What separates it from B.24, in the manual's own words

> *"In contrast to the dynamic bracing, this bracing here **doesn't react to modifications**.
> However, it **can be created individually piece-by-piece**."*

Two preconditions, both stated up front:
- ⚠️ *"The program **always** enters bracings **in the active UCS plane**"* — put the UCS origin in
  the bracing plane and align it parallel.
- ⚠️ *"Since the rods are aligned in accordance with the **system lines**, they should have been
  **created previously**."* For a cross-stay with uniformly staggered rods, *"the middle of the
  system will be sufficient"*.

## B.25.1 Settings — three pages

**Shape Definition:** `Shape Type` · `Resolution` · `Shape Class` · `Shape Size` · `UCS-Position`
(the alignment of the gussets, and hence of the rods, wrt the UCS) ·
⭐ **`Rod Position`** — *"**Front** means on the positive Z-axis, **Back** on the negative Z-axis,
**Both** on both sides, and **Centered** that a rod is positioned in the middle of the axis"* ·
`Rod Insertion` (middle / gravity line / root line) · `Rotation` (0 / +90 / −90) · `Mirror` ·
`Plate Thickness`.

**Bolts:** `Bolt Style` · `Dia` · `Workloose` · `Number Shape` (bolts along the rod) ·
`Number Cross` (transverse) · `Drill Hole Pos.` · `Distance Cross` ·
⭐ **`Weld Bracing`** — *"the shapes and gusset plates are **not drilled**. The dimensions of the
gusset plates, however, are determined **as if drill holes existed**."* ·
`Plate Without…` (square corners instead of bevelled) · `Create Group` (one group per rod) ·
`With Bolts`.

**Edge Distance:** `Edge–1st Hole` (rod front edge → outermost hole axis) · `Hole–Hole`
(pitch along the rod) · `Hole–Edge` (in the gusset) · `Limit Edges` ·
⭐⭐ **`Bracing Rod`** — *"specify by which value the bracing rod has to be **shortened** after its
insertion. **Thus, the rod will be kept in tension**."*

⇒ That last one is real fabrication knowledge: **the rod is cut deliberately short so it goes in
pre-tensioned.** Nothing in the geometry hints at it; only the dialog says so.

## B.25.2 — six separate commands, and that is the point

| button | what it does |
|---|---|
| **BRACING** | rods **and** gussets in one operation. ⚠️ *"**not suitable** for the connection of several rods from **different systems** — use the individual functions instead"* |
| **SINGLE ROD** | insert single rods, already drilled |
| **DRILL ROD** | drill **existing** shapes at their ends, per the dialog |
| **PLATE AUTO** | pick the drilled rods, then click boundary lines; *"the program will try to find a suitable plate dimension by **keeping the edge distances and the boundary lines**"* |
| **PICK PLATE** | you set the gusset shape yourself |
| **UCS** | align the UCS plane afterwards, by three clicked points |

**The worked example (a roof bracing whose diagonals cross with no middle gusset):**
`Plate Middle` for the UCS position · **`Front` for the first rod, `Rear` for the second** so they
do not collide · BRACING → click a system line → click the **limit edges** (the inner web line of
the roof girder) → ⭐ **`b` = Back undoes a wrong pick** → ENTER or right-click to confirm →
a dialog shows the **calculated rod length**, which you may round → then the gusset boundary lines.
⭐ For a bracing **with** a gusset at the crossing: *"do **not** stagger the rods. Open, shorten and
drill the rods **manually** at the point of intersection"*, then one gusset via PLATE AUTO.

---

# MEASURED 09/08/2026

## ⭐ One class serves both chapters — and the difference is a single flag

`Bentley.ProStructures.StructuralObject.PsBracing` carries **`getDynamicStatus()` /
`setDynamicStatus(bool)`**. B.24 is that flag on, B.25 is it off — exactly the manual's *"in
contrast to the dynamic bracing"*. There is no separate static class.

The rest of the dialog maps cleanly onto it:

| dialog | method |
|---|---|
| `Rod Position` | `setLayout(BracingLayout)` — measured: `kAtFront, kAtBack, kCrossed, kCentered, kDoubled, kButterFly, kQuatro` |
| bracing kind | `setType(BracingType)` — `kNormBracing, kRodBracing, kPipeBracing` |
| ⭐ `Bracing Rod` shortening | **`setShapeShorting(double)`** |
| `Weld Bracing` | `setWeldStatus(bool)` |
| `Plate Thickness` / `Without…` | `setPlateThick` · `setPlateType` · `setPlateWide` |
| `Number Shape` / `Number Cross` / `Distance Cross` | `setProfHoleCount` · `setCrossHoleCount` · `setHoleDistCross` |
| the Edge Distance page | `setEdgeHole` · `setHoleHole` · `setHoleEdge` · `setEdgeBorder` |
| ⭐ the **limit edges** you click | `setBorderObjects(id1, id2)` · `addBorder(start, end)` |
| the system line | `setStartPoint` · `setEndPoint` |
| `Create Group` / `With Bolts` | `setGroupStatus` · `setGroupPipeBoltStatus` |

⭐ **`insert(PsPoint Origin, PsVector X_axis, PsVector Y_axis)` takes the plane AS ARGUMENTS** — it
does not read the active UCS. Which is why setting the UCS never helped in B.24: the plane was
already a parameter.

## ⛔ Thirteen configurations. `insert()` does not create from code.

Seven more tried here, on the B.24 portal (two HE300B at x=190000/194000, y=0, so the bracing
plane is **XZ**, not the world XY):

| tried | `insert()` | census |
|---|---|---|
| **static** (`dynamic=0`), XZ plane | False | 723→723 |
| static + `layout=front` | False | 723→723 |
| static + `welded` | False | 723→723 |
| static + `no gussets` | False | 723→723 |
| static + `shorten=5` | False | 723→723 |
| **dynamic**, XZ plane (control) | False | 723→723 |
| static, world XY (control) | False | 723→723 |

Added to B.24's six, that is **thirteen**. Both leads this chapter suggested — the static flag,
and the corrected XZ plane — were **refuted**.
⇒ **B.25's integrated BRACING command is interactive, exactly like B.24's.** Its own worked example
says so in every line: *click* the system line, *click* the limit edges, press ENTER, *click* the
boundary lines.

## ✅ But B.25 is buildable BY COMPOSITION — and the manual says to

*"To a large extent, you can make the program automatically create the components of a bracing.
However, **it is possible as well to generate single components** such as gusset plates, etc.
**individually**."* Its six buttons are separable, and every piece is a call this agent already has:

| button | the composed equivalent | status |
|---|---|---|
| SINGLE ROD | a shape along the system line | ✅ |
| DRILL ROD | `PsDrillObject` | ✅ |
| PLATE AUTO / PICK PLATE | a gusset plate | ✅ |
| bolts | **DRILL first, then bolt the two parts** | ✅ |

**Built at x = 310000:** a 4000 × 4000 bay — two HE200B columns, an IPE300 head beam, four 380 ×
380 × 10 gussets, and two crossing **L90X9** rods staggered ±60 (the manual's Front / Rear), each
cut **6 mm short at each end** so it goes in pre-tensioned. Four bolted ends, `Edge–1st Hole = 60`
and `Hole–Hole = 120` straight off the Edge Distance page.

```
d1+bl  gusset+2  rod+2  bolts+2      d2+tl  gusset+2  rod+2  bolts+2
d1+tr  gusset+2  rod+2  bolts+2      d2+br  gusset+2  rod+2  bolts+2
```

## ⚠️ The mistake worth keeping: holes placed SYMMETRICALLY about the corner

The first pass put the two bolts at ±70 either side of the corner work point. One of each pair
landed **beyond the end of the rod**, so the gusset got 2 holes, the rod 1, and only **1 bolt**
formed — **an unfilled hole in the gusset**, the mirror of the iron rule and the same defect
`kBoltet` produces in B.22.

The Edge Distance page says exactly how to avoid it: holes are measured **inward from the rod's
front edge** (`Edge–1st Hole`, then `Hole–Hole`), never spread about the joint centre. Rebuilt that
way, all four ends came out 2/2/2.
⚠️ **Holes cannot be removed once drilled** — fixing it meant deleting and rebuilding both rods and
all four gussets.

⭐ And note `Weld Bracing` is the one legitimate no-hole case in this chapter: it suppresses the
drilling **because there are no bolts at all**, while still sizing the gussets *as if* there were.
That is not an exception to the iron rule — it is a joint with nothing to bolt.
