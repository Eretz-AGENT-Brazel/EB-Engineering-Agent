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

> ### 🛑 CORRECTED 10/08/2026 — this paragraph used to read:
> *"⭐ **Plate length is derived from the bolt count: 135 for two rows, 210 for three.** The 75
> difference is the same pitch B.19's angles used. **Both chapters share one derivation rule.**"*
>
> **The length is 70 in both.** `props`: `717 L=70 W=135 H=10` · `71E L=70 W=210 H=10`. What the
> bolt count drives is the **section width** — the plate's depth — at a **fixed 70 mm stick-out**.
>
> ⚠️ **And the two chapters do NOT share one rule.** B.19's angles were re-measured the same day
> rather than edited to match: `L 90x9`, **`L=135` / `L=210`, `W=90 H=90`** — a **catalogue**
> section cut to length, its section never changing. B.19's sentence is **true**; only this one
> was false.
>
> ⇒ ⭐⭐ **That difference is the whole reason the FL/BRFL/Plate problem exists here and not
> there.** A web angle always lands on a real catalogue entry because only its length varies. A
> shear plate **re-derives its section for every joint** and can land on a width no mill rolls.

## ⭐⭐ `Poly-Plates` changes the ENTITY CLASS

`Ks_Shape` → **`Ks_Plate`**, with the geometry unchanged at 70 long × 135 wide × 10 thick.

The default product is a **`Ks_Shape`** — i.e. a **catalogue flat bar**, not a plate. That is what
the manual means by *"poly-plates are inserted **instead of flats**"*, and it is what makes
`Turn Flat` (FL vs BRFL) a real ordering decision.

⇒ ⭐ **And when the derived width is not stock, `Poly-Plates` is the HONEST representation.** The
part is called `Plate 135x10` because it will be cut from plate; modelling it as a `Ks_Plate` says
the same thing in the entity class. *(Reasoning, offered as reasoning — the parts-list half was
not tested.)*

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

---

# AUDITED 10/08/2026 — what changed

*Full record: `knowledge/learning/audits/AUDIT-PART-B-2026-08-10.md` § B.20. Plugin v156 → v160.*

## 🛑 CORRECTED — "plate length is derived from the bolt count"

The 09/08 note read: *"⭐ Plate length is derived from the bolt count: 135 for two rows, 210 for
three."*

**The length is 70 in both.** Read back with `props`:

```
717   name='Plate 135x10'  L=70   W=135  H=10
71E   name='Plate 210x10'  L=70   W=210  H=10
```

What the bolt count drives is the **section width** — the plate's depth — not its length. The
distinction is not pedantry: the FL / BRFL / Plate decision below is made on **exactly that
number**, so calling it "length" points the reader at the one dimension that does not matter.

## ⭐⭐⭐ The part's NAME states its mill product

| `name` | `key` | `cat` | meaning |
|---|---|---|---|
| `FL 150x10` | `150X10` | `DIN.DIN_FLACH` | in **`DIN FLACHEISEN`** — flat bar, stock |
| `BRFL 160x15` | `160X15` | `DIN.DIN_FLACH` | in **`DIN_BREITFLACHEISEN`** — wide flat, stock |
| **`Plate 135x10`** | `135X10` | `DIN.DIN_FLACH` | ⚠️ **in neither — the part is cut from plate** |

⚠️ **`key` and `cat` are identical in all three and tell you nothing.** `DIN.DIN_FLACH` is the
stored family label, not a resolvable catalogue — `dumpcat DIN_FLACH` returns nothing. The real
catalogues are `DIN FLACHEISEN` (419 names, widths 10…150) and `DIN_BREITFLACHEISEN` (338 names,
widths 160, 180, 200, 220, 240, 250 … 1200). They are disjoint, and **neither holds 135 or 210.**

**Proved by prediction**, three fresh bays, each name declared before the connection was run:

| `holevert` | depth | predicted | measured |
|---:|---:|---|---|
| 110 | 150 | `FL 150x10` | ✅ |
| 120 | 160 | `BRFL 160x10` | ✅ |
| 125 | **165** | `Plate 165x10` | ✅ |

165 sits *inside* the wide-flat range and is not a stock width ⇒ **the test is catalogue
MEMBERSHIP, not a size range.**

## `Turn Flat` — closed, as a measured negative

`Ks_ComShearPlateLinkData` and `PsShearPlateLinkDataMgd` carry **identical property sets**
(COM adds `GetData`/`SetData` blobs, .NET adds `UnmanagedObject`). **Neither has `Turn Flat`.**
The 09/08 note's *"worth checking the COM twin before concluding"* is now done.

⇒ From code the only lever on FL-vs-BRFL-vs-Plate is the **hole geometry**, and the `name` field
is how the result is checked. ⛔ What a **parts list** prints was not tested — that is part C.

## ⭐ `Position` — the ordinals, swept

| `pos` | plates | where | bolt |
|---|---:|---|---|
| **0** | 1 | +9.30 from web centre | M16 × 45 |
| **1** | 1 | −9.30 | M16 × 45 |
| **2** | **2** | **both sides** | **M16 × 55** |
| 3 · 4 · 5 | 0 | — | `Create()` returned **True** and built nothing |

⭐ At `pos=2` the **bolt lengthened by itself, 45 → 55** — one more 10 mm plate in the packet, one
step up the table, no bolt parameter touched.

## ⭐⭐ The joint's link topology — who owns what

```
beam   (connected)  type=17/kConnectWithSchearPlate     parts=<plates>  bolts=<bolts>  target=<column>
column (support)    type=12/kConnectedBy                empty                          target=<beam>
plate               type=18/kSchearPlateConnectionLink  empty                          target=<beam>
bolt                NO LINK AT ALL
```

⇒ **The connected shape owns the joint.** It is the only member holding the roster and the only
one pointing at the support. **A bolt cannot be traced to its connection from the bolt's side.**

⇒ **`LinkObjectCount` is always 2 — one slot per side of the web.** `pos` chooses which slot is
filled; `pos=2` fills both. The dialog's `left / right / Both` *is* the data structure.

## `GetPlateId` — still returns 0, and it no longer matters

The 09/08 measurement stands. The plates are reachable through the logical link instead, and the
**settings are readable back** through the getter that works:

| call | result |
|---|---|
| `PsLogicalLink.GetShearPlateLinkData()` | ⛔ `PlateThickness=0` on every joint member |
| **`PsShearPlateConnection.GetLink().GetLinkData(0)`** | ✅ `t=18 pos=2 nV=2 nH=2 dV=140 dia=22` |

⚠️ Only straight after `Create()`. There is no binder to an existing joint.

## ⛔⛔ `dia=` DROPS THE BOLTS — a drilled, unbolted joint that reported success

| | plates | bolts |
|---|---:|---:|
| `t=10 dia=16` | 2 | 4 ✅ |
| `t=10` **`dia=22`** | 2 | **0** |
| `t=18 dia=16` | 2 | 4 ✅ |
| `t=18` **`dia=22`** | 2 | **0** |

**The diameter is the killer; the thickness is irrelevant.** `HoleDiameter` names the *hole*; the
bolt comes from `BoltStyle`. A ⌀22 hole against the default `8.8S` has no bolt to match and the
connection **drops it silently** — iron rule 1 violated, and `EB_OK` returned, because the op's
success test was "the census grew".

⇒ **Rule: set the diameter through the STYLE. Leave `dia` = bolt + workloose.**
⇒ **Fixed in v160:** `shearplate` now answers `EB_ERR ⛔IRON-RULE` when it makes plates and no
bolts. Verified both ways.

⚠️ **`webangle`, `splice`, `haunch` and `connbase` take the same parameter and were NOT tested.**
That is a task for their chapters, not a finding about them.

## Band, after the audit

x = 130 000. Seven joints, **every one bolted** (checked with the fixed `connscan`):
y 12 000 / 15 000 / 18 000 = ordinal sweep · y 30 000 / 33 000 / 36 000 = naming prediction ·
y 39 000 = link read-out. The three no-result bays and every y ≥ 42 000 strip were erased —
three of the latter were drilled and unbolted. Census 1 115 → 1 087.
