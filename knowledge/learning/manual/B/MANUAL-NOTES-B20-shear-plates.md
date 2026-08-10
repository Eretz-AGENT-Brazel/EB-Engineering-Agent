# B.20 Shear Plates — chapter notes

*Read end to end 09/08/2026, pages 303–311 (fulltext lines 7234–7405). Command **`PS_SCHEARPLATE`**.
API mapped from `API-SURFACE-RAW.txt`; nothing measured against a live model yet.*

> *"After you have indicated the specifications the shape to be connected is **cut to fit** and the
> connection is created by the program **automatically including all drill holes and bolt
> connections**."*

**This chapter is the twin of B.19.** Same six pages (Shapes · Distances · Connect · Cope ·
Standard Data · Form Group), same pick order, same load database, same one-command-does-everything
behaviour. The difference is the product: a **web plate** instead of a pair of angles.

⇒ Read B.19's notes alongside; only the deltas are written out here.

## Creating one

Click the **shape to be connected**, then the **support shape**, each at the connection point —
*"or use the enter key or the right mouse button if you don't have any support shape"*.

⭐ *"If you do not select a support shape, **only one or two web plates are attached to the end**
of the shape to be connected."* (In B.19 the no-support case gave two angles at **both ends**;
here it gives plates at **one** end.)

⚠️ *"If you select the shear plate command, it is **based on your last settings**"* — stateful,
exactly like B.19.

## Shapes — what differs from the web angle

| field | meaning |
|---|---|
| `Thickness` | thickness of the web plate |
| `Cut Plate` | *"the plate is **cut at the connecting shape** in case of **bevelled** shapes"* |
| ⭐ `Normal to...` | *"the plate is **not** inserted in shape direction at bevelled connecting shapes but **always perpendicular** towards the connecting shape"* |
| ⭐ `Poly-Plates` | *"Poly-plates are inserted **instead of flats**"* — the part changes kind, not just size |
| ⭐⭐ `Turn Flat` | *"If a flat is used, it will be created with the size **length × 10 instead of width × 10**. (Example: **FL 110x10 becomes BRFL 250x10**)"* |
| `Position` | web side `left` / `right`; **`Both` puts a plate on each side of the web** |
| `Gap` | *"from the **outer edge of the support shape** to the **outer edge of the shape to be connected**"* |
| `Vertical Offset` | off the connected shape's axis, up or right by position; **negative reverses** |

The same three-way datum as B.19: `From Edge` (upper edge → upper edge) · `Lower Edge` ·
`Up to 1st. Bolt` (*"to the **centre of the first drill hole**"*).

⭐ **`Turn Flat` is a stock-selection control.** `FL` is a plain flat bar and `BRFL` is
*Breitflachstahl* — wide flat. Flipping which dimension counts as the width moves the part from one
mill product to another. A 110×10 flat and a 250×10 wide flat are **different things to order**,
and this checkbox decides which one the parts list asks for.

## Distances — two named groups

**In shape direction:** `Number` · `End Offset…` (*last hole centre → end of the web plate on the
connecting-shape side*) · `Connection…` (*last hole → end of the connecting shape on the support
side*) · `Dist. Between`.

**In transversal direction:** `Number` · `Edge Distance` (*holes → outer edge of the web plate*) ·
`Dist. Between`.

⇒ Note the asymmetry: the shape direction has **two different end references** (plate end and
shape end), the transversal direction only one (plate edge).

## Connect · Cope

Bolts: `Bolts` (style, e.g. DIN 7990) · `Dia` · `Workloose` (*"mostly 2 mm"*).
Welds: `Weld Style`, then `Weld Flange Side` / `Weld Web Side` with their own thicknesses.

Cope — here the manual calls it a **notch**: *"you can **notch** the connecting shape"*.
`Notch` (a stored variant **or** direct data) · `Cope Connect…` = `Upper Side` / `Lower Side` /
`Both` · a `Gap`.

## Standard Data — the load database, with capacities

Same DAST-style lookup as B.19: enter `H(kN)` / `Hz(kN)` and the available connection plates are
listed. The entries carry `Designation` · `Thickness` · `Material` · `Dia` · `Bolt` ·
`Shape Direction` · `Crosswise` · **`MaH, MaHz` — "Max. Load for this connection"** · `W1, W2, W3…`

⭐ Unlike B.19's list, this one states each connection's **capacity explicitly**. If the database
is populated, that is a directly usable number.

## Form Group

`Create Group` · `With Bolts...` (bolts **and** weld seams join) · ⭐ `Each Plate` (a group per
plate rather than one for the connection).

---

## The API

`Bentley.ProStructures.Connection.Standard.PsShearPlateConnection` — the same shape as the web
angle class: `SetConnectionObjectId` · `SetSupportObjectId` · `SetConnectionPoint` ·
`SetConnectionData` · `Create()` · `Check()` · `GetLink()` · templates · and the database
(`get_PlateDataCount` / `GetPlateDataName` / `GetPlateData`).
⭐ **`GetPlateId(Int32)` should actually deliver here** — unlike B.19, this connection *does* make
plates.

`PsShearPlateLinkDataMgd`:

| dialog | property |
|---|---|
| `Thickness` | `PlateThickness` |
| ⭐ `Poly-Plates` | **`ShearPlateIsPolyPlate`** |
| ⭐ `Normal to...` | **`NormalToCutPlane`** |
| `Cut Plate` | `CutAtConnected` · `CutAtSupport` |
| `Position` | `PlatePosition` *(Int32)* |
| `Gap` | `DistanceToConnected` · `DistanceToSupport` |
| `Vertical Offset` + its three datums | `InsertOffsetVertical` · `InsertOffsetFromShapeEdge` · `InsertOffsetFromDownSide` · `InsertOffsetFromFirstHole` |
| the two `Number` fields | `VerticalHoleCount` · `HorizontalHoleCount` |
| distances | `HoleDistanceVertical` · `HoleDistanceVerticalEdge` · `HoleDistanceHorizontal` · `HoleDistanceHorizontalInside` · `HoleDistanceHorizontalOutside` |
| slots | `SlotAxisDistance` |
| bolts | `BoltStyle` *(String)* · `BoltStyleCRC` · `BoltType` · `HoleDiameter` · `HoleWorkLoose` |
| welds | `WeldStyleCRC` · `WeldToConnectedShape` · `WeldToSupportShape` · `WeldSeamConnect` · `WeldSeamSupport` · `WeldSeam` |
| notch | `CreateCope` · `SetCopeFromTemplate` · ⭐ **`CheckCopeTemplate(String)`** · the `Cope*` distance family · `ConnCopeTopIndex` · `ConnCopeDownIndex` |
| groups | `CreateGroup` · `AddBoltsToGroup` · **`GroupEachPlate`** |
| loads | `ShearX/Y/Z` · `MomentX/Y/Z` |

⭐ **`CheckCopeTemplate(String)` is the lead B.19 was missing.** There, `CreateCope = true` plus
real cope geometry produced no notch at all and there was no way to tell whether the template name
was even valid. This class can be asked.

⚠️ **No property for `Turn Flat`.** The FL→BRFL switch — which decides what gets *ordered* — has no
counterpart in the managed surface. Dialog-only unless it hides behind `BoltType`-style
indirection. Worth checking the COM twin before concluding.

---

# MEASURED 09/08/2026

*Band at x ≥ 130000: four column/beam bays (HE300B + IPE400), four shear-plate variants.*

## The product, and the formula

| variant | objects | what they are |
|---|---|---|
| 2-bolt template | **3** | `Ks_Shape` **70×10×135** + 2 bolts |
| 3-bolt template | **4** | `Ks_Shape` **70×10×210** + 3 bolts |
| `Poly-Plates` on | **3** | **`Ks_Plate`** 70×10×135 + 2 bolts |
| `default/Standard`, t=15, 3×2 | **7** | `Ks_Shape` 274×15×160 + **6** bolts |

⇒ **objects = 1 plate + (`VerticalHoleCount` × `HorizontalHoleCount`) bolts.**
Only beam bolts appear — the plate is welded to the support and bolted to the connected shape,
exactly as a shear plate should be.

⭐ **Plate length is derived from the bolt count: 135 for two rows, 210 for three.** The 75
difference is the same pitch B.19's angles used. **Both chapters share one derivation rule.**

## ⭐⭐ `Poly-Plates` changes the ENTITY CLASS

`Ks_Shape` → **`Ks_Plate`**, with the geometry unchanged at 70×10×135.

The default product is a **`Ks_Shape`** — i.e. a **catalogue flat bar**, not a plate. That is what
the manual means by *"poly-plates are inserted **instead of flats**"*, and it is what makes
`Turn Flat` (FL vs BRFL) a real ordering decision.

⚠️ **This is the same trap as B.19's `Use Flat` → `Ks_BendShape`.** Two consecutive chapters each
hide an entity-class switch behind a checkbox. **Any parts-list or audit query filtered by entity
class will silently miss these parts.**

## The beam is cut differently from B.19

| | cut recorded on the beam |
|---|---|
| **B.19** web angle | `polyCuts` 0 → 1 |
| **B.20** shear plate | **`cutPlanes`** 0 → 1 |

Both drill it (`holeFields` 0 → 1). ⇒ A web angle cuts the beam back with a **poly cut**; a shear
plate uses a **flat plane cut**. An audit looking for one will not find the other.

## The three templates

| template | rows | bolt style | normalToCut | gap to support |
|---|---|---|---|---|
| `default/Standard` | 2 × 2 | `8.8S` | no | 10 |
| `AutoConnect Metric v 18/2 Bolt` | 2 × 1 | **`DIN7969`** | **yes** | 10 |
| `AutoConnect Metric v 18/3 Bolt` | 3 × 1 | **`DIN7969`** | **yes** | 10 |

⭐ The AutoConnect default here is **DIN 7969** — a different bolt standard from B.19's DIN 7990,
and the right one for a shear plate.

## ⭐ The cope-template naming convention, found

`CheckCopeTemplate` was probed with eight candidate names:

```
''  Standard  default  Notch  Cope  1  default/Notch   →  False
default/Standard                                       →  TRUE
```

⇒ Cope templates use the same **`default/<name>`** convention as connection templates. B.19's cope
attempt failed partly because it was never given a valid name — and there was no validator on that
class to say so.

## ⛔ But the cope still does not happen — now proven on BOTH chapters

With `CreateCope = true` **and** the validated name `default/Standard`:

| | beam modifications |
|---|---|
| B.20 shear plate, no cope | `cutPlanes=1 holeFields=1` |
| B.20 shear plate, **cope + valid template** | `cutPlanes=1 holeFields=1` — **identical** |
| B.19 web angle, **cope + valid template** (retested) | `polyCuts=1 holeFields=1` — **identical** |

⇒ **The cope is not reachable from either connection's link data.** A valid template name is
accepted and changes nothing measurable. `PsCopeConnection` — a separate class in the same
namespace — is the remaining candidate, and belongs with **B.12.6 Notch between two Shapes**.

## Two more things that do not deliver

- **`GetPlateId` returns nothing here either**, even though this connection genuinely produces a
  plate. In B.19 the empty result was at least explicable (no plates are made). Here it is simply
  a method that does not work on either class.
- ⚠️ **The load database is empty again**: `get_PlateDataCount()` = **0**, the same as B.19. So the
  `H(kN)` / `Hz(kN)` selection and the `MaH / MaHz` capacities have no data behind them on this
  installation — this is a **product-wide gap, not a B.19 quirk**.
