# B.10 Insert Solids — chapter notes

*Read end to end 09/08/2026, pages 189–192 (fulltext lines 4531–4629).*

## ⭐⭐ Why ProSteel has its own solids at all — and the warning, with its reason

> *"For volume modelling, ProSteel does **not use the AutoCAD solid command ACIS**, but rather a
> **modified version, which works faster and produces smaller graph files**. Consequently, you
> **cannot combine ProSteel objects and AutoCAD 3D solids** (e.g. subtract their volumes). In case
> you do combine objects, there will be **no errors, but nothing will happen!**"*

B.12.7 gives the same warning; **this chapter gives the reason** — speed and file size. And the
consequence: *"in order to give you the same performance range as with AutoCAD, **all solids have
been redefined**."*

⭐ *"As these solids are **real ProSteel objects**, they can be processed with ProSteel commands
(e.g. **drilled**). These solids can be **detailed as normal component part**."*
⇒ So a solid enters the parts list and the shop drawings like any other member.

⚠️ *"the component parts need a **clear direction for detailing**. The **X-axis of the UCS valid at
insertion** is taken as standard"* — changeable afterwards via *Change PS Properties*.

## The ten commands

| command | inputs |
|---|---|
| `PS_SOLID_BOX` | two diagonal corners. ⚠️ if the volume can't be derived (both points on a UCS plane) *"the program prompts you to enter the missing dimensions"* |
| `PS_SOLID_SPHERE` | centre + diameter |
| `PS_SOLID_CYLINDER` | start + end + radius |
| `PS_SOLID_CONE` | axis + **start and end radius** → cone **or truncated cone** |
| `PS_SOLID_TORUS` | rotation axis + outer and inner radius |
| ⭐ `PS_SOLID_ROTATE` | a poly-line + a rotation axis + an optional angle. ⭐ *"the poly-line can also rotate around the **last drawn line segment** if you press **ESC**"* |
| ⭐ `PS_SOLID_CONICPIPE` | *"a pipe that is **conical on the inside and outside**"* — axis, then outer and inner radius at **each** end |
| ⭐⭐ `PS_SOLID_RECT2CIRCLE` | *"a transition from a **circular to a rectangular** cross-section"* — the rectangle's two corners, then the circle |
| ⭐ `PS_SOLID_HULL` | *"an **enveloping solid** formed by any points… almost any shape (**without arcs**)"* |
| `PS_SOLID_EXTRUDE` | a poly-line + a height; *"extruded along **positive Z-direction of UCS**"* |

⇒ **`RECT2CIRCLE` and `CONICPIPE` are Eretz Barzel's own trade** — round-to-square transitions and
conical pipes are hoppers, chutes and tank outlets.

---

# MEASURED 09/08/2026 — band at x = 370000

## One class covers the whole chapter

**`Bentley.ProStructures.Steel.Primitive.PsCreatePrimitive`** — a namespace this project had never
opened:

```
CreateBox(L,W,H) · CreateSphere(R) · CreateCylinder(R,L) · CreateCone(startR,endR,H)
CreateTorus(outer,inner,length) · CreateConicPipe(oOut,oIn,eOut,eIn,length)
CreateRect2Circle(radius, baseCorner, otherCorner, circleCenter)
CreateRotation(revolution, axisStart, axisEnd) · CreateHull() · CreateExtrusion(height,taper,twist)
SetInsertPoint · SetNormal · SetXYPlane(X,Y) · SetPolygon · SetPoints · ObjectId
```

⭐ **`CreateExtrusion` takes `taper` and `twist`** — the manual mentions only the height.
⭐ `SetXYPlane(X, Y)` sets the plane explicitly; after the haunch template's zero plane stretched a
rafter to 317,000 mm, it is set on every call here.
⚠️ Every creator returns **`Void`** — nothing is knowable from the return. Read `ObjectId` and the
census.

## Eight of the ten build, and the sizes are exact

| kind | asked | measured |
|---|---|---|
| box | 600 × 400 × 300 | **600 × 400 × 300** |
| sphere | r 250 | 499 × 498 × 500 |
| cylinder | r 180, len 900 | 359 × 360 × 900 |
| cone | r 350 → 120, h 800 | 698 × 699 × 800 |
| torus | outer 400, inner 120 | 920 × 919 × 120 |
| **conicpipe** | Ø1000/940 → Ø500/440, len 1500 | **999 × 1000 × 1500** |
| **rect2circle** | 800 × 800 rect, r 300, 1000 tall | **800 × 800 × 1000** |
| extrude | 600 × 400 polygon, h 700 | **600 × 400 × 700** |

✅ **And the chapter's claim holds: a solid IS a real ProSteel object.** The box was drilled with
`PsDrillObject` and read back **0 → 1 hole**.

## ⭐ `CreateTorus`'s third argument is undocumented AND mandatory

The manual says only *"outer and inner radius"*. The signature has a third parameter, `length`:

```
length = 0   ->  nothing created, silently
length = 1   ->  created
length = 200 ->  created
length = 400 ->  created
```
⇒ **A torus with `length = 0` fails silently.** Give it a value.

## ⭐⭐ `SetPolygon` takes LOCAL 2D coordinates, not world

The extrusion was first fed a polygon in **world** coordinates (x ≈ 370000) and came out
**5120 × 5552 × 700** instead of 600 × 400 × 700 — the world x was read as a local offset. Fed the
same shape as `-300,-200 … 300,200`, it measured **600 × 400 × 700** exactly.
⇒ **The polygon lives in the primitive's own plane** (set by `SetInsertPoint` + `SetXYPlane`).
⚠️ And that is almost certainly why `CreateRotation` refused: it also takes a polygon.

## ⛔ Still open

- **`rotate`** — refused with a world polygon and with a local one; the axis framing is untested.
- **`hull`** — needs **`SetPoints(PsDataPointArray)`**, not `SetPolygon`; the op passes a polygon,
  so this was never a fair test. A plugin change is required to try it properly.

---

## AUDIT 10/08/2026 — both open items closed

### `hull` works — it needed `SetPoints(PsDataPointArray)`

New parameter **`dpts=`** on the `solid` op (the `pts=` polygon route was never what `CreateHull`
wanted):

```
solid kind=hull dpts=-400,-300,0;400,-300,0;400,300,0;-400,300,0;-100,0,600;100,0,600
   ->  800 x 600 x 600, exact
```

⚠️ **A perfect box fails.** Eight points forming an exact box create nothing; jitter the corners
by 10 mm and the same eight build. Exactly-planar faces are degenerate here — use `kind=box`.

### `rotate` works — three rules, all measured

1. ⭐ **The polygon is LOCAL 2D (`x,y` only).** A profile given as `200,0,0;500,0,0;500,0,400;…`
   collapses to a zero-area line and fails. Write it as `200,0;500,0;500,400;200,400`.
2. ⭐⭐ **The axis is in WORLD coordinates** while the polygon is local. Pass the axis through the
   insert point, or the profile sweeps around the world origin — the first success measured
   **761 000 mm** across.
3. ⭐ **The axis must lie IN the profile's plane.** A Z axis is perpendicular to the local XY plane
   and gives a degenerate revolve — that is geometry, not a refusal.

```
solid kind=rotate pts=200,0;500,0;500,400;200,400 at=370000,-9000,0 \
      axis1=370000,-9000,0 axis2=370000,-8000,0
   ->  1000 x 400 x 1000, exactly a 300x400 profile revolved at radius 200..500
```

⚠️ **`rev` is ignored** — 90, 180 and 360 give identical solids. A full revolve every time.
