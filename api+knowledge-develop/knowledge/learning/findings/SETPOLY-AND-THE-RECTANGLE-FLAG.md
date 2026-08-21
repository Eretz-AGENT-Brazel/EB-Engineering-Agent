# `setpoly` works — and the gate that was waiting for it could never have passed

*Measured 21/08/2026, model 3 (`kuperdam DRILL BASE -3 - FAB`), the item Amir named as the one
not to walk into a project without. Two questions were open since 19/08: does the v187 bulge fix
actually carry an arc, and can the 132 ribs that were built as rectangles be repaired in place.*

---

## 1. The fix is real. The bulge round-trips exactly.

```
setpoly handle=190F  verts 5->7  rect 1->1  holes 0->0  set=ok
        pts=70.375,-220,0;-45.375,-220,-0.414;-70.375,-195,0;…
dumppoly  ->  POLY 190F Ks_Plate PS_Plate 7 1
              70.375,-220,0;-45.375,-220,-0.414;-70.375,-195,0;…
```

Vertex for vertex, bulge included. The v187 line — `appendVertex(x, y, n[2])` instead of
`…, 0.0` — does what it was written to do, and **`setpoly` keeps the holes** (`holes 0->0`, and
the model's 96 holes survived all 132 calls). The op is no longer an unread claim.

## 2. ⛔ But `dumppoly nonrect=` reads a FLAG, not the geometry — and the flag is read-only

`PolyOf()` gets it from `pl.RectangleMode`. That is a stored boolean, not a test of the shape:

```csharp
try { rectMode = pl.RectangleMode ? "1" : "0"; } catch { rectMode = "?"; }
```

A plate born rectangular keeps `RectangleMode = true` no matter what contour is written into it.
And it cannot be cleared:

```
EBAgentApi210.cs(15655,39): error CS0200: Property or indexer
'…PsPlate.RectangleMode' cannot be assigned to -- it is read only
```

⇒ **The gate this model was waiting for — *"`dumppoly` on both drawings, target `nonrect=132`
in both"* — was unreachable by construction.** Not hard: impossible. The source reads 132 because
its ribs were *created* as contours; a repaired plate reads 0 forever. An instrument that reports
a creation flag while its caller reads it as a shape test will fail a correct model and pass a
wrong one.

### The gate that does work: compare the contour in WORLD space

Both drawings dump their contours in each plate's own frame, and both dump each plate's frame
(`org`, `X=`, `Y=`). Lift both to world coordinates and the comparison is unambiguous:

```
plates compared in WORLD space: 132
worst vertex deviation from the source: 0.0000 mm
plates over 0.05 mm: 0
```

## 3. And the frame is not the source's frame — so MEASURE the transform, do not guess it

Writing the source's contour verbatim made the model **worse** (collisions 141 → 184): my plates'
local axes are not the source's. Searching transforms by collision count found `r+90`/`mxy`
(140 → 8), but the honest route is to compute it from both models' own frames:

```
u' = u·(Xs·Xr) + v·(Ys·Xr)        measured on all 132 plates:  (a,b,c,d) = (0,1,1,0)
v' = u·(Xs·Yr) + v·(Ys·Yr)        i.e. a swap, determinant -1 -> the bulge sign flips
```

**All 132 plates share one transform, and it is a reflection** — my rebuild's plate frames are
left-handed with respect to the source's (`rebuild Z = -source Z`). Same class of finding as
`wantspan=`/`spanErr=` on sections the same afternoon: *a frame the API does not hand you is
measured, never inferred.*

## 4. What the repair achieved, and the 8 that remain

| | before | after |
|---|---|---|
| rib contours | 132 rectangles | **132 true ribs, 0.0000 mm from the source** |
| holes | 96 | **96** (setpoly preserves them — the reason it exists) |
| collisions (`minvol=100`) | 140 | **8** |
| the source, measured the same day | — | **0** |

The residual 8 are the eight long diagonal ribs (`PLATE 593x141x30`) against the two `HEB 500`.
Their overlap is real — 29.7 × 29.7 × 61.4 mm — and **no contour transform touches it**: all eight
of the eight rotations/reflections were built and measured, and so was a reversed traversal with
the bulges moved to their new segments. Against the source, those plates agree on **org, bounding
box, L/W/H, cutArea (0.003 mm²) and weight (1 g)**.

⭐ **The one categorical difference is how they were made:**

```
source  CE9   partOrigin = kRipOrigin        pos='1033'  count=1/8
rebuild 1941  partOrigin = kUndefinedOrigin  pos=''      count=1/0
```

The source's ribs are **owned by the RIP (stiffener) connection**; mine are standalone plates.
That is the company's own rule — *the unit is the connection, not the object* — arriving as a
measurement: a connection-owned stiffener and its host are one assembly, and the eight that flag
are the eight where a standalone plate and a beam legitimately share material at the fit line.
⇒ **Open, and it is a modelling-route question, not a contour question:** rebuild those eight
through the RIP connection and re-measure.

## 5. Two more differences the census turned up

| | source | rebuild |
|---|---:|---:|
| `Ks_Shape` on layer `PS_Plate` | 8 | **0** (all 44 shapes went to `PS_Shape`) |
| `PcRebarManager` | 1 | **2** |

Neither is geometry; both are faithfulness. Recorded, not fixed.
