# B.22 Purlin Connection — chapter notes

*Read end to end 09/08/2026, pages 317–328 (fulltext lines 7542–7768).*

> *"This function permits the connection of **purlin courses to roof girders**. Different kinds of
> connection are possible… as standard **bolted connection**, as connection with a **purlin socket
> made out of a bent flat steel** or by means of a **splice** or a **shape**."*

⭐ **The whole command is database-driven.** *"The assignments can be managed in a **database** to
allow the selection of the suitable connections for the **different shape sizes** (e.g. the correct
size of a purlin socket). When the command is selected, the specified connections available for
selection are offered in a list."* Every one of the four pages ends with the same `List` field.
Building that database is referred out: *"please refer to the technical supplement or ask your
ProSteel dealer."*

## The four kinds, and what they share

| | |
|---|---|
| **Bolted Connection** | purlin bolted straight down to the girder |
| **Purlin Socket** | a **bent flat steel** shoe; two bolt diameters — `Dia` girder↔socket, `Dia Side` socket↔purlin |
| **Connection Plate** | a plate, optionally **welded** to the girder, with an optional perpendicular **Supporting Plate** |
| **Connection Shape** | a section (e.g. a channel set upright); `Perpendicular`, `Turn` 90°, `Shape Type` — *"Special shapes can be used as well"* |

Common to all four:

- `Number Transv.` / `Distance Transv.` — holes across the purlin, and their axis spacing
- `Number Length` / `Distance Length` — holes along the purlin run
- `Bolts` (e.g. DIN 7990) · `Dia` · `Workloose`
- ⭐ `Offset` — *"positive values move the drill holes **in shape direction of the purlin course**;
  negative values move them in the opposite direction"*
- `Create Group` — ⭐ *"If the roof girder is **already belonging to another** construction group,
  the [part] will be assigned to **this other group**"* — the girder's existing group wins
- `With Bolts` — the bolts join the group too
- `Opposite Side` — the part is attached on the far side of the purlin course

⭐ **Zero means "off", twice over:**
- `Backer Plates` — *"If you enter the value **0**, **no backer plates** will be created."*
- Connection Plate — *"If you enter the value **0 for w2**, only **one hole** will be created in
  longitudinal direction."*

**Connection Plate specifics:** `Length` (along the purlin) · `Height` (vertical) · `Thickness` ·
`Supporting Plate` (a separate dialog: `Thickness`, `Length`, `Height`, and **inner (1) / outer (2)
chamfers**, each horizontal and vertical) · `Weld Seam` with an `Af` throat and a seam type.

**Connection Shape specifics:** `Length` · `Number` + `w3` spacing · `Base Drill Holes` (girder holes
to the **outer edge of the purlin course**) · `Lateral Drill Holes` (purlin holes to the **upper edge
of the roof girder**) · `Dia` and `Dia Side` · `Perpendicular` · `Turn`.

---

# MEASURED 09/08/2026

*Band at x = 250000: an IPE400 roof girder with **U160 channel purlins** crossing over it — the
classic real choice for מריצים, and a deliberate step away from HEB/IPE everywhere. Second girder
at 254000 and a third at 258000 for the follow-ups.*

## The API

`PsPurlinConnection` — `SetSupportObjectId` (girder) · `SetConnectionObjectId` (purlin 1) ·
**`SetPurlin2Id`** (purlin 2) · `SetConnectionPoint` · `SetConnectionData` · `Create()`.

`PsPurlinLinkDataMgd` maps the dialog almost one-to-one:

| dialog | property |
|---|---|
| the four kinds | ⭐ **`PurlinType`** |
| `Number/Distance Transv.` | `HoleCountSupport` / `HoleDistanceSupport` |
| `Number/Distance Length` | `HoleCountPurlin` / `HoleDistancePurlin` |
| `Bolts` | `BoltStyle` · `BoltType` · `BoltStyleCRC` |
| `Dia` / `Dia Side` | `HoleDiameter` / `HoleDiameterSocket` |
| `Workloose` / `Offset` | `HoleWorkloose` / `InsertOffset` |
| `Create Group` / `With Bolts` | `CreateGroup` / `AddBoltsToGroup` |
| `Opposite Side` | `UseOppositePosition` |
| `Backer Plates` | `FillerPlateThickness` / `FillerPlateWidth` |
| plate size | `Length` / `Height` / `Thickness` / `Width` |
| `Weld Seam` | `WeldSeam` · `WeldStyleCRC` · **`WeldToSupportShape`** |
| `Base` / `Lateral Drill Holes` | `BaseLength` / `SideLength` |
| `Shape Type`, special shapes | `UseSpecialShapeAtSocket` |

**`PurlinType`, measured** (never inferred from declaration order):
`kBoltet = 0` · `kShoe = 1` · `kCleat = 2` · `kShapeBased = 3`.

## ⚠️ The template NAME does not set the TYPE

```
Default/Standard              -> kBoltet (0)
Default/Example-Purlinshoe    -> kShoe   (1)
Default/Example-Purlinshape   -> kBoltet (0)   ← named "shape", carries BOLTED
```
⇒ **`Default/Example-Purlinshape` is not a shape connection.** To get one, take a template and set
`PurlinType = kShapeBased` yourself. `kCleat` has **no shipped template at all**.
⚠️ Note the case: purlin templates are **`Default/`**, cope templates are **`default/`**. The
convention is not uniform — copy the string, do not type it.

## What each type actually builds — all four confirmed by entity type

| `PurlinType` | product |
|---|---|
| `kBoltet` | no part — the purlin bolts straight to the girder |
| `kShoe` | ⭐ **`Ks_BendShape`** — a *bent* flat steel, exactly as the manual describes |
| `kCleat` | `FL 100x10` (`DIN_FLACH`), standing on the girder's top flange, z 200→300 |
| `kShapeBased` | ⭐ **`L 120x80x8`** (`DIN_WINK_UGL`) — an unequal angle |

Overriding `PurlinType` on `Default/Standard` genuinely switches the product: the same template
gave a flat bar for `kCleat` and an angle for `kShapeBased`.

## ⭐ A second purlin is NOT required

The plugin had refused without `p2`, on the reasoning that *"a purlin connection joins two purlin
runs OVER a girder."* That was an **assumption, never measured**. With `p2` omitted:
`check=1`, census **+1**, girder **0→4 holes**, purlin **0→2 holes**, one bolt. A gable or end
purlin is fully supported.
*(The old guard also hid a crash: `IdFromHandle("")` throws — the identical bug the cope op had.)*

## ⭐⭐ `Create()` returned False on every single successful connection

Seven connections built, all seven reported **`create=False`**, all seven created real geometry.
**Third class to do this**, after `PsCreateBendPlate` and `PsCopeConnection`.
⇒ The rule is now general enough to state plainly: **in this product, `Create()` is not evidence.**
Read the parts back — `mods`, hole counts, census.

## ⚠️⚠️ `Default/Standard` builds a cleat plate attached to NOTHING

The shipped template carries **`WeldToSupportShape = False`**. With `PurlinType=kCleat` it produces
a plate that is properly bolted to both purlins (plate 4 holes, 2 in each purlin) and fixed to the
girder by **nothing at all** — no girder holes, no weld.

That is the same defect Amir caught by eye in B.19: *"שים לב שיש שם חופש — הזוויתנים מנותקים לחלוטין
מהעמוד."*

✅ **`WeldToSupportShape = 1` fixes it** — four **`Ks_WeldFlag`** objects appear around the plate
footprint at z = 200, the girder's top-flange plane: two along the sides, two at the ends.
⭐ They are created even though the drawing has **`weldStyleCount = 0`** — weld *objects* do not
depend on weld *styles* being present.

⇒ **For a cleat, always set `WeldToSupportShape` explicitly.** The template default is unsafe.

## ⚠️⚠️ `kBoltet` drills more holes than it bolts

🧲 Amir's iron rule is *no bolt without a hole*. This is its **mirror**, and it is just as much a
defect: **holes with no bolt.**

| type | purlins | girder holes | purlin holes | **bolts** |
|---|---:|---:|---:|---:|
| **`kBoltet`** | 2 | **4** | 2 each | **2** |
| **`kBoltet`** | 1 | **4** | 2 | **1** |
| `kShoe` | 2 | 2 | 1 each | 4 |
| `kCleat` | 2 | 0 | 2 each | 4 |
| `kShapeBased` | 2 | 2 | 1 each | 4 |

`kBoltet` produces the full **2 × 2** field the template asks for (`HoleCountSupport = 2` ×
`HoleCountPurlin = 2`), spaced 56 × 72 exactly as `HoleDistanceSupport` / `HoleDistancePurlin` say —
and then bolts **one position per purlin**. The other three types are balanced.

**The hole depths say which positions get used.** On a purlin, per station:
```
z 200 → 208.82   (the bottom flange only)   ← always bolted
z 200 → 360      (clean through the section) ← never bolted
```

**Tested and ruled out:**
- ⛔ *my geometry* — collinear purlins and a proper side-by-side lap (offset 56) behave identically
- ⛔ *grip length* — U160, U100 and U80 all give the same 4 holes / 2 bolts. Section depth is
  irrelevant, so the B.15 grip-window explanation does **not** apply here.

⇒ **Cause not established.** What is established is the behaviour, and that it is reproducible.
⇒ **Practical consequence for EB:** those unfilled holes are fabricated, reach the shop drawings
and the NC data, and an empty hole through a purlin is a QA reject. **Audit hole count against bolt
count after every purlin connection** — do not assume the connection balanced them.

## ⚠️ `dumpmodel` is blind to bolts and plates — COM is not

`dumpmodel` reported `plates=0 bolts=0` with **264 error rows**, every one of them a `Ks_Plate` or
`Ks_Bolt` failing with *"Object reference not set to an instance of an object."*

An intermediate conclusion here — *"no bolt entities in the band"* — was drawn from that blind
instrument and was **wrong**. The correction:

```python
app, doc = A._app_doc()
o = doc.HandleToObject(handle)      # 152 of 152 Ks_Bolt bound, ZERO failures
o.InsertPoint                       # real coordinates
```

⇒ **Third confirmation that `PSCOMWRAPPERLib` binds where the managed side will not.**
⇒ And a discipline point: **an instrument that reports zero must be shown to report non-zero
somewhere** before its zero is believed. The control here was the B.15 band, where bolts are known
to exist — it read zero there too, which is what exposed the blindness.
