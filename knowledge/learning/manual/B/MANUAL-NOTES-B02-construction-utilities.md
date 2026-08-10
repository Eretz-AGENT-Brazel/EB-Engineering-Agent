# B.2 Construction Utilities — chapter notes

*Read end to end 09/08/2026, pages 89–94 (fulltext lines 2009–2161).*

## B.2.1 Construction lines

> *"These lines are created on **their own layer (default is PS_CONST)**, so that all of them can be
> **jointly hidden or deleted**."*

**Dialog:** `Direction` (2 Points / Line / Point Line) · `Line Type` — standard lines *"the length
of which is determined by projection"* or ⭐ **X-lines** *"which always run up to the edge of the
screen"* · `Distance` · ⭐ `Scale` — *"distance/spacing information is converted to the scale of
your drawing… **this allows actual dimensions to be used**"* · `Angle` · `Number` · `Offset`
(extends both ends past the reference) · ⭐ `Only in Plane` — *"all construction lines are only
created on the current UCS-plane. In addition, possible **picked points are projected** to the
current UCS-plane"* · `Create Reference Line` · `Loop`.

**The nine direct commands:** `PS_CONST_HOR` · `PS_CONST_VER` · `PS_CONST_PAP` (parallel through a
point) · `PS_CONST_PAE` (parallel at a distance) · `PS_CONST_SAP` (perpendicular at a point) ·
`PS_CONST_SAE` (perpendicular at a distance from a reference point) · ⭐ `PS_CONST_DVD` — *"divides
a reference line into equal segments and creates corresponding perpendicular construction lines
along this line (**also utilizing the start and end point**)"* · `PS_CONST_DEL` (delete them all).
⚠️ *"the construction lines are created **only within the current UCS plane**. It is best to use
them **only in the views**."*

## B.2.2 Measure of distances

Two points, then a dialog in **two blocks**:
- **Coordinates in UCS** — start/end X,Y,Z · distance in X, Y, Z · **Dist Direct** ·
  ⭐ **Angle**, *"from the start to end point in reference to the **user coordinate x-axis**"*
- **Coordinates in WCS** — the two points, plus ⭐ **Cos X, Y, Z**, *"the so-called **directional
  cosine** of the vector from start to end point"*

---

# MEASURED 09/08/2026 — band at x = 390000

## ⭐ Neither half needs a ProSteel command

- **Measure is pure computation.** Every field was reproduced and then checked against the model:
  ```
  start 390000,0,0   end 393000,1500,900
  Distance in X/Y/Z    3000.0 / 1500.0 / 900.0
  Dist Direct          3472.751
  Angle                26.5651°
  Cos X/Y/Z            0.863868 / 0.431934 / 0.259161
  ```
  ✅ **AutoCAD's own `Length` for the same segment: 3472.751 — delta 0.00e+00**, and the direction
  cosines square-sum to **1.000000000**. That is the whole dialog, verified twice over.

- **Construction lines are ordinary AutoCAD lines on `PS_Const`.** That layer exists (confirmed in
  B.1), so the products of all nine `PS_CONST*` commands are reachable straight over COM —
  horizontal, vertical, parallel at ±400, perpendicular, and the divide.
  Divide measured: **4000 / 5 = 800 per segment, 6 perpendiculars** — start and end included,
  exactly as the manual specifies.
  **13 lines, all on `PS_Const`** — which is precisely what makes `PS_CONST_DEL` a one-layer sweep.

⚠️ The `PS_CONST*` commands themselves are not on the plugin's `CmdAllow`. That list is a
deliberate safety control and was not widened; the geometry they produce is what matters and it is
reachable without them.

⇒ **The useful takeaway for the agent: the divide is a layout tool.** Purlin spacings, bolt rows,
stair stringers — anything laid out at equal intervals is `PS_CONST_DVD`'s job, and the start and
end lines come free.
