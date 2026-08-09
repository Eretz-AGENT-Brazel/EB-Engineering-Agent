# B.16 Insert Stiffeners — chapter notes

*Read end to end 09/08/2026, pages 262–268 (fulltext lines 6299–6464). Command **`PS_RIP`**;
oblique stiffeners have their own **`PS_RIP_ANGLE`**. ⚠️ The manual's own command index lists
"Stiffeners **B.15** PS_RIP" — the chapter is B.16. API mapped from `API-SURFACE-RAW.txt`;
nothing measured against a live model yet.*

> *"Although stiffeners are common **poly-plates or flats**, the program **already calculates their
> dimensions according to the shape** and user specifications."*

That is the whole point of the chapter, and it is the direct answer to how lesson 3 was done: 214
ribs were built by hand as polygons. A stiffener is not a plate you draw — it is a plate the
program **derives from the shape it sits in**. You give a point and a few rules; it works out the
height from the web, the width from the flanges, and the corner radii from the section.

## ⭐ One command makes TWO stiffeners

> *"In the case of **symmetric shapes** such as HEA, HEB, etc., **two opposite stiffeners** are
> created between the web and the two flanges. When only one stiffener is needed for the structure,
> just **delete the other one**. However, it **will not be restored during an update**."*

⇒ Expect a census of **+2 per insertion** on an I-section. And deleting the spare is a one-way
door with respect to the connection's own update — worth knowing before auditing a model and
"helpfully" regenerating.

## Insertion

Works in views or in the global view. *"Normal"* stiffeners run **vertically to the shape axis**.

1. pick the **shape**
2. pick the **centre of the insertion point** — *"Your pick point is generally placed
   **perpendicular to the shape axis**"*, so clicking anywhere on a construction line drawn along
   the flange projects onto the axis correctly
3. for `Half Stiffener` or `To Measure` only: pick the **fastening side** — *"Click the flange side
   with which the stiffeners are to be in contact."* `Full Stiffener` needs no side, it *"is
   inserted to fit"*.

## Dimensions

| field | meaning |
|---|---|
| `Layout` | the shape of the **inner corners** |
| `Use Flat..` | a **flat steel** instead of a poly-plate — *"processed by Boolean operations in a way as if its shape would be identical with a poly-plate"* |
| `Search Closest Flat` | *"The current polyplate is **replaced by the closest fitting flat steel**. No user-specific flat steel is created."* |
| `Full Stiffener` | height over the **complete** web height |
| `Half Stiffener` | half the web height, on **the side you picked** |
| `By Length` | your height — ⚠️ *"**Only values between 10–90 % of the web height** can be used"* |
| `Square` | height per the specification, but the layout is a **triangle** |
| `Thickness` | from the table, or free *"if this setting has been activated"* — the same thickness-list gate as B.9 |
| `Flange Offset` | clearance to the flange ⇒ **height** slightly reduced |
| `Web Distance` | clearance to the web ⇒ **width** slightly reduced |
| `Length` | the length when `To Measure` is chosen |
| `Offset` | clearance to the **outer edge of the flanges** ⇒ width reduced. ⭐ *"If the stiffener is to slightly **project towards the outside**, type a **negative** value."* |
| ⭐ `Round to` | rounding applied to the **width**, *"carried out **after** calculation of the projection"* |
| `Radius` | the radius at the shape's own root radii. ⚠️ **`0` means "import the shape radius"** — not "no radius". With `Insert at Slant` the radius is *"bridged by a slanted edge"* |

⭐ **Why `Round to` exists** — the manual says it plainly: *"it is possible, e.g., to permit only
dimensions divisible by 5, **so that flat steel bars can be used**."* That is a fabrication
economy control, not a drafting nicety: it forces the derived width onto a stock flat size.

**Alignment button:** *"After insertion, the stiffener first is placed in the **middle** of the
insertion point. However, you can also align it to the **upper or lower edge** by clicking
repeatedly on it."*

## Connect — welds, and marks that reach the NC data

`Weld Style` · `Flange Side` (+ a `Thickness` override) · `Web Side` (+ its own override).

⭐ **`Weld Mark`** — *"**small drill holes** serving as weld marks… These weld marks then can be
output in the **workshop drawings or in the NC-data**."*
`None` · `At Center` · `At Edges` (one at each outer edge).

⇒ A weld mark is a real hole in the steel, produced so the welder knows where the stiffener lands.
It is fabrication data, not annotation.

`Create Group` — *"the stiffeners and the shape are arranged to create a group. If the shape is
**already part of another group**, the stiffeners are allocated to **that other** group."*

## Options

`Angle insertion` — insert on a slant; you are prompted for a direction line, and *"the necessary
**extension of the stiffener width** is automatically calculated."*
`Angle` — the position angle **related to the centerline**.
`2D-Section` — an automatic 2D section right at the stiffener; *"The cutting plane is situated
slightly in front of the stiffener and it ends slightly behind… the cutting direction is always in
shape direction."*

## Oblique stiffeners (`PS_RIP_ANGLE`)

Same flow, but you click a **position line** giving the orientation. ⚠️ *"The distances refer to
the **outer corners** of the stiffener plates"* — a different datum from the normal case.

⭐ The manual's own precision tip: *"Show the **center line** of the shape in the top view and use
the **'virtual point of intersection without Z'**, in order to get an exact center point of
insertion using the 'point of intersection' of the axis and the construction line."*

---

## The API — where each field lives

`Bentley.ProStructures.Connection.Standard.PsStiffenerConnection`
`SetConnectionObjectId(Int64)` (the shape) · `SetConnectionPoint(PsPoint)` (the manual's *centre of
the insertion point*) · `SetConnectionData(PsStiffenerLinkDataMgd)` · `Create()` · `Check()` ·
`GetLink()`.

`Bentley.ProStructures.Connection.LinkData.PsStiffenerLinkDataMgd` is the dialog:

| manual | property |
|---|---|
| `Layout` | `ShapeType` *(Int32, no enum)* |
| Full / Half / By Length / Square | `LengthType` *(Int32, no enum)* |
| `Length` | `Length` |
| `Thickness` | `Thickness` |
| `Flange Offset` | `FlangeDistance` |
| `Web Distance` | `WebDistance` |
| `Offset` | `Offset` |
| `Round to` | `RoundTo` |
| `Radius` | `Radius` |
| align to upper edge | `TopAligned` |
| `Weld Style` | **`WeldStyleCRC`** *(Int32)* |
| flange / web weld thickness | `WeldSeamFlange` · `WeldSeamWeb` |
| `Weld Mark` | `CenterPunchType` *(Int32, no enum)* |
| `Create Group` | `CreateGroup` |
| `Angle insertion` · `Angle` | `InsertWithAngle` · `InsertAngle` |

⚠️ **Four raw `Int32` fields with no enum behind them** — `ShapeType`, `LengthType`,
`CenterPunchType`, and `WeldStyleCRC`. Their values cannot be guessed and must be measured.
`WeldStyleCRC` is worse than the others: it is a **checksum of a style name**, so there is no
ordinal to try.

⭐ **The way in is the template API**, not experimentation:
`GetTemplateCount()` · `GetTemplateName(i)` · `GetTemplate(name)` → a fully populated
`PsStiffenerLinkDataMgd`. Reading the shipped templates yields **valid, measured values for every
opaque Int32 at once** — the same lesson as B.18, where anchors turned out to be creatable only
from a template.

**Not found in the API:** `Use Flat..`, `Search Closest Flat`, `2D-Section`. Dialog-only until
proven otherwise.

---

# MEASURED 09/08/2026

*Band at x ≥ 90000 in the teaching drawing: 5 girders (HE300B, IPE400, UB457x152x60) and
**66 stiffeners** — 33 pairs. Every number below was read back out of the model.*

## ⭐ The derivation formula — the thing the manual only gestures at

HE300B (`b=300, tw=11, tf=19, r=27`), template defaults `flangeDist=1, webDist=2, roundTo=5`:

```
width  = (b − tw)/2 − webDist − offset   = 142.5  → roundTo 5 → 140   ✓ measured 140
height = h − 2·tf − 2·flangeDist          = 260                        ✓ measured 260
anchor = from the WEB FACE outward:  y −7.5 … −147.5                   ✓
```

Confirmed one field at a time on the same girder:

| change | measured |
|---|---|
| baseline | W 140 · H 260 |
| `offset = +20` | W **120** — inward |
| `offset = −20` | W **160** — ⭐ **projects outward**, exactly as the manual says |
| `roundTo = 1` | W **142** (true value 142.5) |
| `roundTo = 25` | W **125** ⇒ **rounds DOWN** to the multiple |
| `flangeDist = 10` | H **242** (= 260 − 2×9) |
| `webDist = 15` | W **125** |
| `thickness = 20` | t **20** |

⇒ `roundTo` is a **fabrication control**: it forces the derived width onto a stock flat size, and
it truncates rather than rounding to nearest.

## ⭐ `Layout` (`ShapeType`) decoded — by the BULGE, not the vertex count

All three layouts give **7 vertices and identical extents**. Vertex count cannot tell them apart;
ProSteel polygons carry a **bulge per vertex**. The real contour of the chamfered stiffener:

```
(−70,−130) (43,−130) (70,−103) (70,103) (43,130) (−70,130)
```

The corner cut is **27 × 27** = the HE300B root radius — which confirms *"If this value is 0, the
shape radius is imported"*.

| `ShapeType` | `Layout` | bulge at the corner |
|---|---|---|
| **0** | chamfered | `0` — a straight cut |
| **1** | convex | **`+0.414`** |
| **2** | rounded | **`−0.414`** |

`bulge = tan(θ/4)`, so 0.414 ⇒ **θ = 90°**, a quarter circle; the sign gives the direction.

⚠️ **Two reader traps found here.**
`getVertexAsPoint(i, PsPoint)` returns **(x, y, _bulge_)** — the z slot is the bulge, not a height.
`getVertexbyValue(i, a, b, c)` is documented as `(X, Y, Bulge)` and actually delivers
**(bulge, y, 0)** — the x is not there at all. Three `Double&` parameters of the same type mean
**the compiler cannot catch a wrong order**. The first reading looked plausible and was wrong.

## ⭐ `LengthType` — write-effective, read-broken

Set explicitly on IPE400 (full web 371):

| value | measured height | meaning |
|---|---|---|
| **0** | 371 | **Full** |
| **1** | 185.5 | **Half** (exactly half) |
| **2** | 371 | **By Length** — with `Length = 0` it falls back to full |
| **3** | 80 (= the width) | **Square** — the manual's triangle layout |

But **reading it back from a template does not reflect the template.** All seven shipped templates
were built on one girder (UB457, full web 426.1) and measured:

| template | `LengthType` as read | measured |
|---|---|---|
| `half convex` / `full convex` | both **1** | **HALF** / **FULL** |
| `half rounded` / `full rounded` | both **2** | **HALF** / **FULL** |
| `half chamfered` / `full champfered` / `Standard` | all **0** | **HALF** / **FULL** / **FULL** |

⇒ **The template NAMES are correct in every case; the exposed `LengthType` is not.** Writing the
property works; reading it from `GetTemplate()` proves nothing. Choose full/half by the template
name, or set `LengthType` yourself.

## ⛔ A stiffener cannot be created without a template

`template=none` → `Create()` **false**, nothing made — while `Check()` still returned **1**.
Exactly the B.18 anchor lesson: **template-only creation**. And `Check()` returned `1` both when
the create succeeded and when it failed, so it is not a predictor of anything.

## Weld marks are real holes

Hole count on the girder, measured before and after:

| `CenterPunchType` | girder holes |
|---|---|
| **0** — None | 0 |
| **1** — At Center | **2** (one per stiffener of the pair) |
| **2** | **no marks** ⇒ 2 is *not* "At Edges" |

The manual's third option exists in the dialog; its ordinal is not 2 and remains unmeasured.

## Oblique insertion works from code

`InsertWithAngle = true` + `InsertAngle` produces the slant without the interactive direction line:

| angle | footprint along the girder | height |
|---|---|---|
| 0° | 10 (= the thickness) | 260 |
| 45° | **413.3** | 243.4 |

⇒ *"The necessary extension of the stiffener width is automatically calculated"* — confirmed.

## Confirmed as written

**One command, two stiffeners.** `created=2` on every insertion into a symmetric section, without
exception across 33 insertions.
