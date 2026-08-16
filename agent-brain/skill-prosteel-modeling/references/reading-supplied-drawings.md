# 📐 Reading a supplied drawing — as geometry, not as a picture

*Born 13/08/2026 on the first real customer job (MARTAR pile-insertion template, Bernie).
A customer PDF is not a lesson model: nobody will tell you the answer, and the numbers that
are not dimensioned are still binding on the fabricator. This is how to get them.*

## ⭐⭐ The rule

> **Never scale a drawing off the rendered image.** A PDF from SolidWorks (or any CAD) carries
> the **vector geometry**. Pull the segments out, fit the scale to a dimension you can read, and
> then every undimensioned feature becomes a measurement instead of a guess.

Eyeballing a 200-wide chord at 3 mm/px puts a ±15 mm error on every number, and 15 mm is the
difference between a plate that fits and one that goes back to the saw.

## The method

```python
import fitz                                    # PyMuPDF — present on this machine
doc = fitz.open(pdf_path); p = doc[0]

segs = []                                      # every straight edge in the sheet
for path in p.get_drawings():
    for it in path['items']:
        if   it[0] == 'l': segs.append((it[1].x, it[1].y, it[2].x, it[2].y))
        elif it[0] == 're':
            r = it[1]
            segs += [(r.x0,r.y0,r.x1,r.y0), (r.x1,r.y0,r.x1,r.y1),
                     (r.x1,r.y1,r.x0,r.y1), (r.x0,r.y1,r.x0,r.y0)]

words = p.get_text('words')                    # dimension TEXT with x/y — the scale anchors
```

1. **Split the sheet into its views first** by y/x band (plan / side / section / iso). Each view
   can sit at its own scale; never mix them.
2. **Fit the scale inside one view** from two features whose real distance you know — a stated
   overall length, or a dimension text you can pair with its extension lines.
   Cross-check with a *second* known distance before trusting it.
3. **Cluster the long lines.** Accumulate total length per y (horizontals) and per x (verticals);
   the structural edges stand out from the dimension lines by an order of magnitude.
4. **Then read the undimensioned features off the cluster positions.**

⚠️ **Dashed and hidden lines arrive as hundreds of 3 mm segments.** Filtering by individual
segment length loses whole members — accumulate per coordinate instead.

## 🎯 Reading a hollow section: FOUR parallel lines, not two

A tube in a projected view draws as **outer silhouette + corner-radius tangent lines**:

```
|   |                            |   |
2821 2839                     3001 3019     <- x, mm
 \___/                          \___/
outer ..... 197.7 = the 200 face ..... outer
      tangents 161.7 apart = the corner radii (~20 mm on RHS200x10)
```

⇒ **the outermost pair is the section; the inner pair is not a second part.** Reading the inner
pair as geometry is how a 200 section becomes a phantom 160 one. The inner *void* (180 for a
10 mm wall) shows up as its own pair again when the view is a true section.

## ✅ Prove the reading before you model — use the stated weight

The drawing said **2450 kg**. Two readings of the bracing were on the table:

| reading | steel | verdict |
|---|---:|---|
| braces span the full 5100 bay (the customer's own preliminary weight sheet) | 2790 kg | **wrong by 14%** |
| braces span 2420 in x, 150 inboard of the cross members (the vectors) | 2401 kg + plates ≈ 2450 | ✅ |

⭐⭐ **A stated total weight is a free, independent check on the whole geometry** — it catches a
mis-read member length that no amount of staring at the view will. Run it *before* building.
Other free checks: a dimension chain that must sum to the overall (`200 + 5×2720 + 200 = 14000`),
and symmetry about the mid-span.

## 🛑 What the drawing cannot tell you — and what to do about it

Undimensioned *mounting* is the recurring gap: the same lug reads as "on the top face" in one
view and "on the inner face" in another, and both look plausible at render resolution.

> **Ask one line. Do not average the two readings.**
> On this job **three separate details were rejected, and not one was a geometry error** — the
> dimensions were exact every time. All three were *how the part mounts*:
>
> | rejected | I had it | it actually was |
> |---|---|---|
> | pin plate assembly | on the chord's top face | **internal**, off the inner face |
> | the same, again | plate 150 out × 100 high | **100 out × 150 high** |
> | the end plate's bend | folded toward the pipe | folded **back over the profile** |
>
> Each rejection cost a rebuild of 24–36 parts. **A guess presented as fact would have cost a
> fabricated part**, which is the whole point of rule zero.

## ⭐ When the customer draws the answer, read the geometry — do not scale his sketch either

Amir settled the corner-plate contour by drawing a **polyline in the model itself**. Read it with
COM rather than measuring the screenshot:

```python
for e in doc.ModelSpace:
    if 'Polyline' in e.EntityName:
        co = list(e.Coordinates)        # AcDb3dPolyline -> x,y,z triples
```
It gave `(508,1302) (728,1302) (728,1210) (600,1082) (508,1082)` — a 220 × 220 square with a
128 × 128 chamfer on the inner corner — exact, and mirrorable to the other three corners about the
assembly centre. ⚠️ **Its z values were meaningless** (42 / 50 / 58 — whatever his osnap caught);
the contour lives in X-Y. Take the plan shape from the polyline, take the height from the stack.

And when the answer arrives, **re-derive the position from the drawing anyway**: the customer's
sketch gave the plate sizes, but the drawing's own side view carried the 123 mm vertical extent
that fixed the shelf's elevation — 20 shelf + 100 upright, exactly the stack he described.
The sketch and the vectors agreeing is what let the third attempt be the last one.

## ⚠️ And where the customer's own paperwork disagrees with itself

A preliminary weight sheet in the same folder assumed a different brace layout and a
59.3 kg/m section. The **drawing governs**; the sheet is a dated estimate. But read it — it named
the sections (`RHS 200x200x10`, `RHS 150x100x8`, `RHS 120x80x8`) and separated the base frame
from the guides, which is free scope information.

🧲 **Section choice follows the weight, not the label.** `RHS200x200x10` exists in three
catalogues here at three different masses — `SHS200X200X10`/`BS_HYBOX_SHS` **57.0 kg/m**,
`RQ200x10`/`DIN_QUADRATROHR` **58.817**, `QR200x10`/`MSH QR` **58.82**. Pick the one that
reproduces the stated weight, and say which you picked.
See [`SECTION-CATALOGUES`](../../../projects/) — the key is opaque, always look it up.
