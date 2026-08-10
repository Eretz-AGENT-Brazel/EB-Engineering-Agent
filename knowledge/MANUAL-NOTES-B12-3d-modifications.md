# B.12 3D-Modifications — chapter notes

*Read end to end 09/08/2026, pages 195–225 (fulltext lines 4705–5473): B.12.1 Divide/Combine ·
B.12.2 Modify Shapes · B.12.3 Modify Plates · B.12.4 Wall Processing · B.12.5 Additional Settings ·
**B.12.6 Notch between two Shapes** · B.12.7 Boolean Operations.*

> *"When inserting the component parts, you have paid attention to inserting the shape at the
> correct position… **Very often component parts overlap and must be adapted subsequently.**"*

The chapter that turns "the members meet on paper" into "the members fit". Commands are grouped by
what they act on: **all parts · shapes only · plates only · walls**.

## B.12.1 Divide / Combine

**Cut at Line** — trim or extend at a boundary, like AutoCAD's trim.
The boundary is a construction line **perpendicular to the active UCS plane**, which becomes the
cut plane — so a slanted UCS gives a slanted cut, and the line counts as **infinite**.
⭐ **ALT while clicking the end EXTENDS instead of cutting.**
⚠️ `Distance` shortens the shape after the cut, and it is a **perpendicular** distance between
shape and cut plane.

**Cut at Shape** — cut or extend against another shape.
⚠️ *"When the shape is cut, **the shorter section is always cut off**."*
⚠️⚠️ *"The plane actually hit by the **centerline** (or the extended centerline) of the shape to be
cut will be the cut plane. **If the centerline does not meet any surface, no cut can be made!**"*
⭐ *"a **logical link** is created between the parts… if one part is modified, **the cut will be
automatically updated**."*

**Divide a Shape** — split at a cutting line.
⚠️ `Distance` shortens **both** new ends, so *"the arising gap has the **double** distance value."*
⭐ `At Plane` — three points define the plane. One point alone is taken perpendicular to the
centreline.
⭐ *"The information for the parts lists is **identical for both parts** with that of the initial
shape, **except for the length**."*

⭐ **The platform recipe, worked in the manual:** lay the beams through as single members, then
divide them at the crossing girders. *"You need not insert each partial shape and **the risk for
dimensional errors is eliminated**."* For an IPE300 crossing member (flange 150) the separation
distance is **75** — half the flange. Remove a girder later and `Combine` closes the gap again.

**Combine two Shapes** — only if *"exactly aligned"*; otherwise the function aborts. Parts-list
data comes from the **first** shape selected, except length.
⭐ **ALT skips the checks** — *"you can combine any shape with any shape."*

**Outlets (milling out)** — rectangular · wedge · circular. Width / Height / Depth plus a
`Position` for each, relative to the insertion point; the wedge adds a tip position
horizontally and vertically.

## B.12.2 Modify Shapes

**Shorten** three ways: by two picked points (their **perpendicular distance to the centreline** is
the amount) · by an explicit value · by the stored default.
⚠️ *"If you pick in the **middle third** of the shape length, **each end** is shortened by half the
specified value."*
**Extend** works the same — ⚠️ *"however, this doesn't work if the selected end has been modified
by a cut."*

**Mitred cut**, three variants: by the **bisecting line** (⚠️ *"if the height of the two shapes
differs, the outer edges are not aligned"*) · by the **intersection points of outer and inner
edges** (*"even shapes of different height are correctly cut aligned"*) · and a variant that cuts
both so an **arc element can be inserted** with a given `Radius` — absolute or `*2`-style, a
multiple of the first shape's maximum diameter. `Gap` is kept between the shapes.
⭐ These cutting commands also create **logical links**, so the cut updates itself.

## B.12.3 Modify Plates

Add a contour edge (a new grip-movable point at the picked position) · delete an edge ·
**transfer a contour to other plates** ·
⭐ *"adapt the **basic polygon** of a plate processed by cuts to the current form. The corresponding
modifications become **superfluous and are deleted**."* — useful *"for data export if
modifications of the contour are not supported there."*
Mitred cuts between plates as for shapes, with a `Gap`.

## B.12.5 Additional Settings — the productivity page

**Multiple Selection** (cut several parts at one line / at one object / divide / poly-cut without
re-selecting the function) and **Loop** (repeat the command until explicitly interrupted) for cut,
divide, connect, shorten, lengthen, poly-cut, mitre, insert/delete corner.
`Close Dialog` — close after one action, or stay open to pick another command.

---

## ⭐⭐ B.12.6 Notch between two Shapes — the cope, at last

*Left open by B.19 and B.20, where `CreateCope` on the connection classes did nothing. This is the
real command.*

Click **the shape to be notched**, then **the shape giving the contour**.
⚠️ *"the connection is created on the base of your **last setting**"* — stateful like its siblings.

| field | meaning |
|---|---|
| `Layout` | `Fit Shape Start` (**the normal cope**) · `Fit Shape Middle` · `Fit Shape End` |
| `Corner Layout` | `Edge` (bevelled) · `Radial` (adapted radii) · ⭐ **`Access Holes`** — *"holes are drilled in the **inner corners** of the cope"*, radius presettable |
| `Inner Edge` | distances refer to the **outside** of the flanges; with `Inner Edge` to the **inside**; with `Center` to the **shape end** (*"the exact position depends on the `Web Distance` value"*) |
| `Both Sides Equal` | the bottom takes the top's data, and the fields are filled in |
| `Exchange Sides` | swaps top and bottom — *"so that you can **turn** the connection"* |
| `Distances` | set separately for the upper and lower flange |

⭐ **`GET FLANGE THICKNESS`** — *"allows an **unknown shape** to be clicked. The flange thickness is
then entered in the input fields for Top and Bottom Flange Inside… a flange distance can be
swiftly determined **without knowing the shape**."* `+ Distance` is added to the result.
*"As reference, you should select the setting **'Outer Edge'**."*

⭐ **Transfer to many:** click all the shapes that are to receive the cope, then the shapes to be
coped — one setting, many copes.

**Standardized Notches** — a dBASE list of predefined notches, editable in any dBASE editor.

### Notch at the end of a shape — no second shape needed

⭐ *"Just enter **ESC** instead of selecting a second shape and select the **end** of the shape."*

`Notch Type` = **`Shape End`** (simple) or ⭐ **`US-Cope`** (a moment cope) · `Edge Layout` =
square / drill out / round off · `Web Distance` (the shape is shortened) · `Both Sides Equal` ·
`Turn` (rotates the notch 90° to the longitudinal axis) · `Center Hole-Flange` ·
`Diameter` · `Center Hole-End` (*"without taking the web distance into consideration"*).

### US-Notch (moment cope)

> *"In California, special connections are produced which have to be **earthquake-proof**. Here,
> the joists adjoining the flange are notched."*

⚠️ *"Different from the notches described up to now, the default settings of this notch are
**transformed into a poly-cut**. The option for **corner treatment is not available** here."*

Its eight dimensions: `Web Distance` **FW** · `Flange Thickness` **t** · `Angle of Intersection`
**A1, A2** · `Flange Distance` **FF1, FF2** · `Center Hole-Flange` **CT1, CT2** · `Diameter`
**D1, D2** · `Hole Center` **FCL1, FCL2** · `Center Hole-End` **B1, B2**.

## ⚠️⚠️ B.12.7 Boolean Operations — and the loudest silent-failure warning in the manual

> *"ProSteel does **not** use the AutoCAD volume modeller ACIS but the modeller which is used in
> Architectural Desktop… Consequently, **you cannot process ProSteel objects with the Boolean
> operations of AutoCAD**… In case you do combine objects, **there will be no errors, but nothing
> will happen!**"*

⇒ AutoCAD's own `UNION` / `SUBTRACT` on a ProSteel object **does nothing and says nothing**. Use
ProSteel's own:

| command | behaviour |
|---|---|
| **`PS_ADD`** | click both; the new object takes the parts-list data of the **first** clicked |
| **`PS_SUB`** | click the target first, then the volumes to subtract — ⚠️ **those are deleted** |

⚠️ *"you will **not** create two independent objects in case you 'split' the first object in the
process."*
An escape hatch exists: convert the ProSteel object to an ACIS solid via its properties and use
AutoCAD's booleans — *"your drawings will then become **larger and more sluggish**."*

---

## The API

⭐ **`Bentley.ProStructures.Connection.Standard.PsCopeConnection`** — and it is in the
`Connection.Standard` family, **the one that creates**:
`SetConnectionObjectId` (the shape to notch) · `SetSupportObjectId` (the contour shape) ·
`SetConnectionData(PsCopeLinkDataMgd)` · `Create()` · `Check()` · templates · `GetPlateId`.

`PsCopeLinkDataMgd`:

| dialog | property |
|---|---|
| `Layout` | `ShapeFitType` |
| `Corner Layout` + its radius | `EdgeType` · `Radius` |
| `Inner Edge` / `Center` | `AlignToInnerEdge` · `AlignToMiddle` |
| `Both Sides Equal` · `Turn` | `BothSidesEqual` · `Rotate` |
| the distance family, top and bottom | `DistanceEdgeTop/Down` · `DistanceInsideTop/Down` · `DistanceOutsideTop/Down` |
| `Web Distance` | `WebDistance` · `WebDistance2` |
| ⭐ notch at the shape end | **`UseShapeEndCope`** · `CutAtStart` · `ShapeLength` |
| `Notch Type` | `CopeType` |
| the GET FLANGE THICKNESS result | `FlangeThickness` |
| the US-cope poly-cut | `PolyCutType` · `SlopeCut` · `RotatedSubBody` |
| access holes | `HoleFieldLinkIndex` · `HoleFieldIndices` |

⭐ **The US-cope's eight dimensions, one property each:**
`FirstSupportFaceAngle`/`Second…` (**A1/A2**) · `FirstFaceToFlange`/`Second…` (**FF1/FF2**) ·
`FirstCenterToFlange`/`Second…` (**CT1/CT2**) · **`FirstRatholeDiameter`**/`Second…` (**D1/D2**) ·
`FirstCenterOfHole`/`Second…` (**FCL1/FCL2**) · `FirstStraightInFlange`/`Second…` (**B1/B2**).

⭐ **"Rathole"** is the American term for the access hole at a cope corner — the manual calls it
`Access Holes`, the API calls it a rathole. Two names, one feature.

---

# MEASURED 09/08/2026

*Band at x = 230000: a girder HE300B with four IPE240 beams framing in, the platform-recipe
divide, a shorten/extend rig, two mitred pairs, three boolean pairs and an outlet beam.*

## ⭐⭐ The cope works — and `Create()` lies in BOTH directions

`PsCopeConnection` creates. The proof is not its return value but `mods`: **`polyCuts` 0 → 1** on
exactly the beams that took a cope, and 0 on the ones that did not.

| what was passed | `check` | `create` | `polyCuts` | truth |
|---|---|---|---|---|
| template `default/Standard` + support | 1 | **False** | 0 → **1** | ⭐ **succeeded while reporting False** |
| template + `edge=2 rathole=20 radius=10` | 1 | **False** | 0 → **1** | succeeded |
| every field set by hand, **no template**, + support | 1 | **True** | 0 → 0 | ⛔ **reported True and did nothing** |
| `UseShapeEndCope=1`, **no support** | **0** | False | 0 → 0 | refused |

⇒ **`Create()` is worthless in both directions here.** `PsCreateBendPlate` returns False while
succeeding; this class does that *and* the opposite. Only a fresh read-back is evidence.

## ⭐⭐ A template carries state the property dump does not expose

The hand-built attempt set `fit=1 radius=8.5 web=5 outer=20/20 inside=30/30 edge=5/5` — **identical
on every property `PsCopeLinkDataMgd` exposes** to what `GetTemplate("default/Standard")` returns.
The template worked; the hand-built copy did nothing.

⇒ **Always `GetTemplate(...)` and override it.** Never construct link data from scratch. The
variant that added `edge`/`rathole`/`radius` *on top of* a template worked perfectly — that is the
supported shape of the call.

## ⛔ The manual's ESC route is dialog-only

*"Just enter ESC instead of selecting a second shape"* — a notch at the shape end with no second
shape. From code, `SetSupportObjectId` is **mandatory**: without it `Check()` returns **0** and
nothing is created, whatever `UseShapeEndCope` and `CutAtStart` are set to. `PsCopeConnection` also
has **no `SetConnectionPoint`**, so there is no way to say *which* end.

⇒ **Second instance of this exact pattern today.** B.15's *"the components had to be drilled first,
which now is not necessary any more"* is likewise unreachable from the API. **A capability the
manual describes is a claim about the DIALOG, not about the API.** Test it before relying on it.

⚠️ And here `Check()` *did* discriminate — 1 when it would create, 0 when it would not. That is not
a general rule for the product (other classes return 1 on both outcomes), but for this class it is
a usable pre-flight.

## The three cope templates

```
[0] default/Standard                    shapeFit=1 radius=8.5 web=5  outer=20/20 inside=30/30 edge=5/5
[1] AutoConnect Metric v 18/10mm Cope   shapeFit=1 radius=0   web=10 outer=10/10 inside=25/25 edge=10/10
[2] AutoConnect Metric v 18/20mm Cope   shapeFit=1 radius=0   web=20 outer=20/20 inside=40/40 edge=20/20
```
All three carry `shapeFit=1`; a freshly constructed `PsCopeLinkDataMgd` carries **`shapeFit=0`**.
`get_PlateDataCount()` = **0**, as on every other connection class in this product.

⭐ A cope lands as a **poly-cut**. The manual says that of the *US-cope* specifically — measured,
it is true of the **ordinary** cope too.

## B.12.1 divide and combine — both clean

| | measured |
|---|---|
| `SplitAtPoint` | an 8000 beam split at 2000 → original keeps **2000**, a **new** member holds 6000; census +1 |
| again on the 6000 half | → 4000 + a new 2000; census +1 |
| `ConnectWith` | 2000 + 4000 → **6000**, census **−1** — the second member is consumed |

⚠️ Split the **right half**. The first attempt split the original a second time, but the original
was by then only 2000 long and the point lay outside it — `EB_ERR`, no change. The API was right.

## ⭐ `ChangeLengthAtSide(id, side, len)` — `len` is a SIGNED DELTA

Not a new length. Measured on one IPE160:
`2000 → 3500 → 5000` with `len=+1500` twice, then `5000 → 4200 → 3400` with `len=-800` twice.
**Positive extends, negative shortens, and it applies to the chosen side.** The dialog calls these
two separate buttons; the API is one signed call.

## ⭐ One mitre call cuts BOTH members

`miter(a, b)` alone took both from `cutPlanes=0` to `cutPlanes=1`. The reciprocal `miter(b, a)`
returns `applied=0` and is **redundant** — call it once, on the pair.
Both variants behave the same: `type=0` (bisecting line) and `type=1` (outer/inner edge
intersection).

## B.12.7 boolean — and a third mode the manual never mentions

`add` / `sub` / **`common`** all apply; `subBodies` climbs 0 → 1 → 2 per operation.
`SubBodyType.kCommenBody` — **intersection** — appears nowhere in the chapter. *(Bentley's spelling,
not a typo here.)*

## ⚠️⚠️ `PsObjectProperties.Weight` is the NOMINAL weight

A 1200×600×20 plate reports **113.04 kg** — exactly 1.2 × 0.6 × 0.02 × 7850. It stayed 113.04 after:

- two boolean **subtractions**, and
- **five ⌀60 holes** (≈2.2 kg of steel, ~2%),

across a `PS_REGEN` and a fresh read. So the figure ignores **all** material removal, not just
booleans.

⇒ **Never judge a cut by the weight** — that reads the wrong thing, and an early suspicion in this
session that a subtraction had silently failed came from exactly that mistake. Judge it by
`mods` (`subBodies` / `polyCuts` / `outlets` / `cutPlanes`).
⇒ For **ordering** material this is arguably the number EB wants (the gross plate). For a finished
part weight it is not. Worth knowing which one a parts list is quoting.

## Outlets

> ### 🛑 RETRACTED 10/08/2026 — see `MANUAL-NOTES-E09-properties-dialogs.md`
> This section used to read: *"Types 0 / 1 / 2 (rectangular / wedge / circular) all apply to a
> shape; the `outlets` count climbs 0 → 1 → 2 → 3."* **Both halves of that are wrong.**
>
> **The type numbering.** `OutletType` is
> `kUndefinedOutlet, kOutletRectangle, kOutletTriangle, kOutletArc, kOutletInversArc` — so
> **0 is UNDEFINED**, and the real mapping is **1 = rectangle · 2 = triangle · 3 = arc ·
> 4 = inverse arc**. Read off the surface dump. (The enum trap, again: values are measured,
> never inferred from the order of the neighbouring names.)
>
> **The claim that it worked.** Not reproducible. On 10/08, all four types — with and without
> `SetLength`, across three normals, on a clean beam and on a modified one — returned
> `applyRc=0` with `outlets` unchanged at 0. **Downgraded to unverified.**
>
> Untried, worth three strikes when someone returns to it: `SetXYPlane(XAxis, YAxis)` (the same
> zero-plane failure that stretched a rafter to 317,000 mm in B.26) ·
> `SetXPosition/SetYPosition/SetZPosition(PositionSelection)` — which is E.9.16's own
> **Insertion Position** field · `SetAutomatic(bool)` · `SetFlag(int)`.

## ⚠️ A composed section key fails silently-ish

`name="HEB300"` → `create=False`, census unchanged, no exception. The real DIN key is **`HE300B`**.
⇒ **Search the catalogue for the key; never assemble one.** `sections filter=…` writes the list.

## Not resolved

- Whether `edge=2` genuinely produced **access holes (ratholes)** in the corners — `polyCuts=1` proves
  a cut, not its shape. Confirming it needs a shaded view, and `VSCURRENT` is not in the plugin's
  `CmdAllow` allowlist. **Not widening that allowlist without Amir's approval** — it is a safety
  control, and `UCS` was only ever added after he approved it explicitly.
