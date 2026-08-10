# B.21 Splice Joints — chapter notes

*Read end to end 09/08/2026, pages 312–317 (fulltext lines 7406–7541). Command **`PS_LASCHE`**
(*Lasche* = fishplate). API mapped from `API-SURFACE-RAW.txt`; nothing measured yet.*

> *"Use this command to create a **web and/or flange plate joint between two shapes**… After you
> have indicated the specifications, the shape to be connected is **cut to fit** and the program,
> **automatically including all drill holes and bolt connections**, creates the connection."*

The third member of the B.19–B.21 family, and the most three-dimensional of them: instead of one
plate or one pair of angles, a splice wraps the section with **up to six plates at once**.

⚠️ **A hard precondition the other two do not have:** *"Both of the shapes have to be **in
alignment along the surfaces to be connected**."* A splice joins two collinear members; it is not a
framing connection.

No second shape ⇒ *"plates are attached to the end of the first shape."*

⭐ The manual's own advice on scale: *"Take advantage of the dialog **templates**… However, if you
are working with a **great many** connections, we recommend using a **database with user-defined
types**."* — templates for the handful, a database for production.

## Options — six independent plate positions

| field | meaning |
|---|---|
| `Gap Distance` | the distance kept between the two shapes |
| `Bolts` · `Dia` · `Workloose` | usual; clearance *"usually 2 mm"* |
| `Upper Side` | plate on the **outer** upper face |
| `Lower Side` | plate on the **outer** lower face |
| `Upper Inside` | *"an **additional** plate… on the **inner** upper side"* |
| `Lower Inside` | the same below |
| `Left` / `Right` | plates on the **web** faces |
| ⭐ `Single Side` | *"the splice joint is attached to the side of the second shape… in the form of a **welded** connection"* |
| ⭐⭐ `Diagonal` | *"a **bolted** connection at one upper side and a **welded** connection at the other. **For the lower side, it is just the opposite.**"* |
| `Create Group` / `With Bolts` | as in B.19/B.20 |

⇒ Six positions: **top-outside, top-inside, bottom-outside, bottom-inside, web-left, web-right.**
A full flange-and-web splice is one command.

⭐ `Diagonal` is a fabrication idea, not a drawing one: bolt one side and weld the other, mirrored
top to bottom — so each half of the joint has one shop weld and one site bolt.

## Top/Bottom and Left/Right — two identical field sets

The flange splices and the web splices get their **own page with the same fields**:

| field | meaning |
|---|---|
| `Number Shape..` | bolts **along** the shape |
| `Edges Outside` | ⭐ the **projection of the splice beyond the last bolt** |
| `Edges Inside` | from the **starting point of the shape** to the first bolt |
| `Dist. Between` | bolt to bolt, along |
| `Number Cross..` | bolts **across** |
| `Edge Outside` | plate beyond the outmost bolts |
| `Inner Distance` | bolt to bolt, across |
| ⚠️ `Dist. Between` (transversal) | *"**If there are more than 2 bolts** in transversal direction, this value indicates the distance from **one outer bolt to the other**"* |
| `Thickness` | from the list, or free if that option is on |
| `Vertical` | *"moves the splices in transversal direction to the shape"* |
| ⭐ `Weld` | *"**no bolts and drill holes will be inserted**, but the parts will be **welded** with each other. **Length settings are still valid.**"* |
| `Length` | the length of the welded splice |
| `As` · `Weld Style` | overwrite the thickness with the weld style |

⚠️ **`Dist. Between` in the transversal direction means two different things** depending on the
bolt count — spacing at two bolts, outer-to-outer span at three or more. A value that reads
correctly at 2 will be wrong at 3.

⭐ **`Weld` turns the same splice into a welded one** — the plate stays, the holes and bolts go, and
the length still comes from the same fields. So a bolted and a welded splice are one parametric
object, not two.

**Data page** — appears *"when you have defined a database for the selected shape"*, otherwise not
shown at all.

---

## The API

`Bentley.ProStructures.Connection.Standard.PsSpliceJointConnection` — identical in shape to the
other two: `SetConnectionObjectId` · `SetSupportObjectId` · `SetConnectionPoint` ·
`SetConnectionData` · `Create()` · `Check()` · templates · `get_PlateDataCount` · `GetPlateId`.

`PsSpliceJointLinkDataMgd`, and the mapping is unusually clean:

| dialog | property |
|---|---|
| `Upper Side` / `Lower Side` | `ConnectFlangeTopOutside` · `ConnectFlangeDownOutside` |
| `Upper Inside` / `Lower Inside` | `ConnectFlangeTopInside` · `ConnectFlangeDownInside` |
| `Left` / `Right` | `ConnectWebLeft` · `ConnectWebRight` |
| `Gap Distance` | `DistanceBetweenObjects` |
| `Number Shape..` / `Number Cross..` | `HoleCountVertical{Flange,Web}` · `HoleCountHorizontal{Flange,Web}` |
| the distance family, **twice** | `HoleDistance{Vertical,VerticalEdge,Horizontal,HorizontalInside,HorizontalOutside}{Flange,Web}` + `HoleDistanceVerticalCenterFlange` |
| `Thickness`, per page | `PlateThicknessFlange` · `PlateThicknessWeb` |
| `Vertical` | `InsertOffsetFlange` · `InsertOffsetWeb` |
| `Weld` per page | `WeldToFlange` · `WeldToWeb` |
| `Length` of the welded splice | `TopPlateLap` · `SidePlateLap` *(inferred — "lap" is an overlap length)* |
| ⭐ `Diagonal` | **`WeldDiagonal`** |
| `Single Side` | `WeldToSupportShape` *(inferred)* |
| weld styles | **two** CRCs: `WeldHorizontalStyleCRC` · `WeldVerticalStyleCRC` |
| bolts | `BoltStyleCRC` · `BoltType` · `HoleDiameter` · **`HoleWorkloose`** |
| groups | `CreateGroup` · `AddBoltsToGroup` |

⚠️ **Two spelling traps against the sibling classes:**
- **`HoleWorkloose`** here vs **`HoleWorkLoose`** in `PsWebAngleLinkDataMgd` and
  `PsShearPlateLinkDataMgd`. One capital letter apart, three classes, same concept.
- **No `BoltStyle` string.** B.19 and B.20 both expose the style *name* beside its CRC; here only
  `BoltStyleCRC` exists, so the style cannot be set by name on this class.

⚠️ **No load fields.** `ShearX/Y/Z` and `MomentX/Y/Z` are on the web-angle and shear-plate classes
but **not here** — even though the manual mentions a database. So the splice database, if
populated, is not load-indexed the way the other two are.

---

# MEASURED 09/08/2026

*Band at x ≥ 150000: four pairs of collinear IPE400 beams meeting with a 20 mm gap.*

## The two shipped templates

| template | positions | plates |
|---|---|---|
| `default/Standard` | topOut · downOut · webL · webR | **4** |
| `default/example2` | **all six** — the two insides as well | **8** ← not 6 |

Both: `gap=10` · `tFlange=10` · `tWeb=10` · `2×2` holes on flange and web · `dia=16` ·
`workloose=2` · `topLap=144` · `sideLap=128`.

## ⭐ Six checkboxes produce EIGHT plates

`default/example2`, measured object by object:

```
2 × 298 × 128 × 10    the outer flange plates
4 × 298 ×  39 × 10    the INNER flange plates -- two per flange
2 × 266 ×  10 × 128   the web plates
```

⇒ **An inner flange plate cannot be one piece: the web crosses the inner face**, so each of
`Upper Inside` and `Lower Inside` becomes **two narrow strips, one either side of the web**.
2 outer + 4 inner + 2 web = 8. Nothing in the manual says this.

`default/Standard` gives the plain four: `2 × 298×150×10` flange + `2 × 266×10×128` web.
Turning both flange positions off leaves exactly the **2 web plates**.

## `holeFields` counts plate GROUPS, not plates

Read off beam A:

| variant | holeFields after |
|---|---|
| 4 plates (flange + web) | **2** |
| 2 plates (web only) | **1** |

⇒ One hole field per *family* of plates, not per plate and not per bolt.

## ⭐⭐ `Weld` turns the whole splice into a welded one — and welds are OBJECTS

`WeldToFlange = WeldToWeb = true` on the same `default/Standard` template:

| | plates | bolts | weld objects |
|---|---|---|---|
| bolted | 4 × `Ks_Shape` 298×150×10 | via 2 hole fields | 0 |
| **welded** | 4 × `Ks_Shape` **288**×150×10 | **0** | **32 × `Ks_WeldFlag`** |

Exactly as written: *"no bolts and drill holes will be inserted, but the parts will be welded with
each other."* Confirmed — **zero bolts**, and 36 new objects instead of 4.

⭐ **`Ks_WeldFlag` is a new entity class** — eight per plate, alternating `139×1×2` (along) and
`1×150×1` (across). Welds are modelled objects here, not annotation.

⚠️ The plate also got **shorter, 298 → 288**: a welded splice is not the bolted one with the bolts
removed; its length comes from the `Length` field instead of the bolt pattern.

## Confirmed / still open

- ✅ The precondition holds in practice: the two members were built **collinear** with a 20 mm gap
  and every variant created cleanly.
- ⚠️ **`GetPlateId` returns nothing here too** — a third class, same broken method.
- ⚠️ **The database is empty again**: `get_PlateDataCount()` = **0**. Third of three
  (`web angle`, `shear plate`, `splice`), which settles it as a **product-wide** gap on this
  installation rather than anything chapter-specific.

---

# AUDITED 10/08/2026 — what changed

*Full record: `AUDIT-PART-B-2026-08-10.md` § B.21. Plugin v160 → v162, and `eb_api.dumpmodel` fixed.*

## ⛔⛔ CORRECTED — every measurement above was taken on an UNBOLTED joint

The 09/08 table reads *"bolted | 4 × `Ks_Shape` 298×150×10 | **via 2 hole fields** | 0"*. The bolt
column says "via 2 hole fields", which is **not a bolt count**, and nobody counted the bolts.

```
y 0     Standard   4 plates   boltSlots=0   holeFields=2
y 3000  example2   8 plates   boltSlots=0   holeFields=2
y 6000  web only   2 plates   boltSlots=0   holeFields=1
```

⇒ **Three joints, drilled, with nothing through the holes.** Iron rule 1, in the model since
09/08. **Cause:**

```
[0] default/example2  … boltCRC=0 …
[1] default/Standard  … boltCRC=0 …
```

⭐⭐⭐ **Both shipped templates carry NO BOLT STYLE**, and this class is the one of the three with
**no `BoltStyle` string** — only the CRC. The note above flags the missing string as a curiosity.
**It was the reason the chapter's whole product was unbolted.**

**Fixed — `splice boltstyle=<name>` (v162), resolving the name through `PsObjectStyleList`:**

| | new objects | plates | real bolts |
|---|---:|---:|---:|
| shipped | 4 | 4 | **0** ⛔ |
| **`boltstyle=DIN7990`** | **20** | 4 | **16** ✅ |

## ⚠️⚠️ A WELD FLAG OCCUPIES A BOLT SLOT

The welded bay reports `BoltObjectCount = 32`. All 32 are on layer **`PS_Weld`** — `Ks_WeldFlag`.

⇒ **`getBoltObjectId` / `BoltObjectCount` mean FASTENERS, not bolts.** A link reporting 32 bolts
can hold zero. Any iron-rule check that counts slots is **defeated by welds** — v160's shear-plate
guard was. v161 opens each fastener and classifies it.

## ⚠️ `dumpmodel`'s `insert` is (0,0,0) for EVERY plate and EVERY bolt

All 241 plates and all 305 bolts. B.9 fixed the plugin to emit a world bbox; **the Python client
never parsed it**, so any region query answered *zero bolts everywhere in the model*.

```
bolts in the splice band via insert[] : 0
bolts in the splice band via center[] : 62
```

Fixed in `eb_api.dumpmodel()` — plate and bolt rows now carry `bbox` and `center`.
⇒ ⭐ **A fix that stops at the producer is half a fix.**

## ✅ `TopPlateLap` / `SidePlateLap` — the inferred mapping, measured

Predicted before the run: a welded splice at `toplap=100 sidelap=100` gives 200 mm plates.
Measured: **200 × 150 × 10** (flange) and **200 × 128 × 10** (web), all four.

⇒ ⭐ **The lap is a HALF-length: plate length = 2 × lap.** A **bolted** splice ignores it — its
plates measure 298, set by the bolt pattern.

## ⭐ What the splice actually orders (B.20's naming rule)

| part | name | stock? |
|---|---|---|
| flange plates | `FL 150x10` | ✅ a real `DIN FLACHEISEN` entry |
| web plates | **`Plate 128x10`** | ⚠️ no |
| inner flange strips | **`Plate 39x10`** | ⚠️ no |

The web plate's width is derived from the web depth less the flange thicknesses, so it lands on a
stock width only by accident. ⛔ The parts-list half is part C and untested.

## Band, after the audit

y 9 000 welded · y 15 000 `DIN7990` proof · y 18 000 `Standard` 4 plates/16 bolts ·
y 21 000 `example2` 8 plates/16 bolts · y 24 000 web-only 2/8 · y 27 000 lap test.
The three drilled-and-unbolted originals and the two defect probes were erased after every
demonstration they carried had been rebuilt bolted.

## Still open

* `Diagonal` (`WeldDiagonal`) and `Single Side` — mapped, **never exercised**. `Single Side` →
  `WeldToSupportShape` remains **inferred** and stays marked so.
* ⚠️ The transversal `Dist. Between` dual meaning is **quoted from the manual, not measured**.
  Test it B.14's way: build at 2 and at 3 transversal bolts and compare the **gaps**.
* ⚠️ **Whether `webangle`, `haunch` and `connbase` also ship with `BoltStyleCRC = 0` was NOT
  checked.** A task for their chapters — and after this it is the first thing to check in each.
