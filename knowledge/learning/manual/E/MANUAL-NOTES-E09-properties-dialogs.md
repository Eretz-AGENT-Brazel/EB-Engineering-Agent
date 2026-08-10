# E.9 — ProSteel Properties Dialogs

*Manual pp. 1129–1172, 17 sub-sections. Read 10/08/2026, implemented in
`projects/SANDBOX/E-structural-elements.dwg`, band **x 0 – 44 000**.*

---

## The headline

**E.9 reads like a reference chapter. It is not — it is a WRITE SURFACE.**

Every field the manual documents, tab by tab, is a property on `PsObjectProperties`,
and that class carries **`writeTo(Int64 ObjId)`**. Measured: 14 fields set in one call
on an HE300B column, `writeTo.rc=0`, **14/14 survived a re-read from a fresh instance**,
and AutoCAD itself drew the result (the column turned green from `ColorIndex=3` and grew
a centre line from `CenterLineMode=1`) — a confirmation by a route entirely independent
of the read-back.

⇒ **The whole properties dialog is now code.** `op=propset handle=… <AnyPropertyName>=<value>`

---

## How the dialog decides what to show — and why the API does not

Three ways in: right-click → *ProSteel properties* · the command `PS_PROP_CHECK` · the
COM **Double-Click** module.

⭐ *"ProSteel analyses which parts were selected and only provides the properties valid
for these parts."*

⭐⭐ **The filter trick, worth knowing at the keyboard:** *"if you want to modify all plates
of a model, you have to make sure that the **first selected object is a plate**. All other
selected elements are filtered accordingly."* So: pick one part of the type you want, then
select the whole model, and ProSteel filters to that type by itself.

The dialogs fall into three families: (1) specific to a part type · (2) processing actions,
*"not displayed if several parts have been selected"* · (3) valid for all parts.

> ⚠️ **THE API DOES NOT FILTER.** `PsObjectProperties` exposes all ~120 members on every
> part. An HE300B column happily reports `KlemmLen=0`, `Tension=0`, `MountingBolt=False` —
> three bolt fields, meaningless on a column, and no error. **The dialog's type filtering is
> a UI feature; in code, knowing which fields apply is the caller's job.** `op=propfull`
> prints `not-applicable-to-this-part` for the ones that actually throw, which on a shape is
> only 3 — so absence of an exception proves nothing.

---

## The map: dialog tab → API property

Built by cross-referencing all 17 sub-sections against the reflection dump, then read back
off a real column, a real plate and a real bolt. `op=propfull handle=X [tab=N]` prints a
part under exactly these headings.

### 1 — Layout (E.9.1 / E.9.2 / E.9.3): how the part is DRAWN

| dialog | property |
|---|---|
| Layout | `ObjectDisplayMode` |
| Holes | `HoleDisplayMode` |
| Only Outer Cont. | `OuterContourMode` |
| **Modeller / Acis** | `ModellerMode` |
| 2D Display · 2D-Section | `DisplayAs2dMode` · `DisplayAs2dSectionMode` |
| Center Line · COG Line · Pitch Lines | `CenterLineMode` · `COGLineMode` · `PitchLineMode` |
| Part Label · Short name | `DrawName` · `DrawShortName` |
| ECS Axes · Direction marker | `ECSAxisMode` · `DirectionMarkMode` |
| Transparency | `Transparency` |

⚠️ On the modeller, from the dialog side, the same warning B.10/B.11 paid for from the
geometry side: *"ACIS is much slower but more precise… it is **not possible to mix ACIS-
and facet-modeller volume models**."*

⭐ Plates additionally have **`Grid`** — *"a plate grid is displayed on the upper side of the
plate. This grid represents a gridiron or similar things."* That is how a **grating/tread**
is represented, and `Symbol Direction Size` sets its symbol size.
Also `Only view ports` + the command `PS_VIEWPORT_DISPLAY`.

### 2 — Shape Type (E.9.1)

`Key` · `Katalog` · `Resolution` · `ShapeClass` · `ObjectType` **(read-only)** · `InternalName`

⭐ `Key` vs `InternalName`: on the test column, `Key='HE300B'` but `InternalName='HE 300 B'`.
**The key is the lookup string, the internal name is for display.** Consistent with the
long-standing rule: *search the catalogue for a key, never assemble one*.
`Article='DIN 1025-2'` — the part carries its own standard.

⭐ **`Treat As Single`** for combi/weld shapes is in this tab in the dialog.

### 3 — Position (E.9.1 / E.9.2)

`Origin` · `XAxis` / `YAxis` / `ZAxis` · `InsertMatrix` · `InsertX` / `InsertY` · `Scale` ·
`MirrorFlag` / `YMirrorFlag` / `Mirrored` · `MidLineStart` / `MidLineEnd` **(read-only)** ·
`UseUserMidpoint` / `HasUserMidpointDefined`

Plates additionally: `Thickness`, `Height Offset` (insertion plane above the ECS plane),
`Insert Edge`, and ⚠️ **`Rectangular` is one-way** — *"If this option is deactivated, you
cannot reset it."*

⚠️ `PartOrigin` read `kRailConnectionPlate` on a plain column — a meaningless value on a
shape. **Do not trust `PartOrigin` outside the part types that define it.**

### 4 — Data (E.9.1 / E.9.2 / E.9.3): the parts-list identity

`Name` · `Material` · `Note1` · `Note2` · `Posnum` · `Sendnum` (shipping) · `Originalnum` ·
`Article` (item) · `Count` · `TotalCount` · `PartListFlag` (Adopt / In Partlist) ·
`BoltListFlag` · `DontDetailFlag` · `DontPositionFlag` · `TransportName`

⚠️ `TotalCount` — *"only correct after positioning"*. `Modified` — *"whether this shape has
been modified since last detailing; after scanning with the DetailCenter this field is
automatically deleted"*.

> ### ⭐⭐ A PLATE'S NAME CANNOT BE WRITTEN
> E.9.2's Data tab lists **`Name` twice**, which reads like a typo:
> > *Name — the name of the plate for parts list and output.*
> > *Name — **the final name built according to the settings with dimensions**.*
>
> It is not a typo. **Measured on four separate plates: `Name` is silently ignored** — it
> stayed `PLATE 400x300x20` every time — while the same call on three shapes took the new
> name immediately. A plate's name is **generated from its dimensions**, so writing it is
> overwritten by the rebuild. `PsObjectProperties.doInterprete(String)` is very likely the
> template resolver behind it.
>
> ⇒ To label a plate, use **`Note1` / `Note2` / `Posnum`** — all three stick.

### 5 — Values (E.9.1 / E.9.2 / E.9.3): the numbers

| dialog | property | measured on the test column (HE300B, 4 m) |
|---|---|---|
| Length / Width / Height | `Length` / `Wide` / `Height` | 4000 / 300 / 300 |
| Weight | `Weight` | **468 kg** = 117 kg/m × 4 ✓ |
| Vol Weight | `VolumeWeightFlag` | *"weight calculated via the volume modeler, not from length × kg/m"* |
| Addition | `LenAdd` | — |
| — | `CutArea` | **149** cm² = the HE300B section area ✓ |
| — | `PaintArea` | **6.92** m² = 1.73 m²/m × 4 ✓ — a real quotation quantity |
| Fixed Form (plate) | `FixedFormFlag` | *"the plate can only be modified by manipulations such as cuts or Boolean operations"* |
| Slope (conical) | `SlopedHeight` | 300 on a prismatic shape |

**Every number is metric and correct against the catalogue.**

#### ⭐⭐ The bolt tab is where the iron rule lives

| dialog (E.9.3) | property |
|---|---|
| **Grip Length** — *"the calculated clamping length of the bolt"* | **`KlemmLen`** |
| **Pre-Tension** — *"the pre-tension of the bolt in percent"* | **`Tension`** |
| **Mounting Bolt** — *"assembly bolt, in contrast to workshop bolts; this has an influence on the assembly parts list"* | **`MountingBolt`** |
| Dia | `Diameter` |
| In Bolt List | `BoltListFlag` |

> 🧲 **`KlemmLen` is ProSteel's own answer to the iron rule.** The grip length is the
> thickness of the packet the bolt actually clamps. If it equals the sum of the thicknesses
> of the parts the bolt is supposed to connect, the bolt genuinely passes through them —
> and that is a statement from ProSteel's own calculation rather than from my proximity
> matcher, which `vfy_bolts` itself declares blind to *which part* a hole belongs to.
> **Not yet built into a check. This is the single most valuable follow-up in the chapter.**

Also in E.9.3: `Countersunk` · `Hexagon Socket` · component-by-component display and
parts-list flags (`Bolt` / `Washer` / `Nut` / `Lock Nut` / `Tapered Washer` in U- or I-form /
`2nd Washer`) · a button to reverse the insertion direction ·
⭐ **`Mounting Space`** — *"the assembly room is displayed as well. It mainly serves for
verifying whether an assembly is possible"* — i.e. **wrench clearance**, a real buildability
check that ProSteel will draw for you.

### 6 — Assignments (E.9.17)

`DetailStyleId` (Detail Style) · `DisplayClass` (Display) · `AreaClass` (Area Class) ·
**`FamilyClass`** (Parts Family — *"e.g. support or girder"*) · `ProcessStatus` ·
`LayerName` · `StyleName`

⭐ `FamilyClass` has its own dedicated writer as well: `UpdateFamilyClass(ObjId, Index)`.
It read `-1` (unassigned) on a fresh shape.

### 7 — State (not on any tab)

`ModifyFlag` · `Modified4Reactor` · `BlockModify` · `BlockRecalcFlag` · `VirginGroupFlag` ·
`VirginViewFlag` · `IndependendForDetailing` · `AnalysisMode` · the three `*FlagsLong` words.

⚠️ `ModifyFlag`, `VirginGroupFlag` and `VirginViewFlag` each exist **twice** — once as
`Boolean` (readable) and once as `PartModification` (**write-only**: *"Property Get method
was not found"*). Those three are the entire `not-applicable=3` count on a shape.

---

## E.9.12 – E.9.16 — the modification tabs

`op=mods handle=X` already reports exactly these six counters, and each was calibrated on a
clean IPE beam (all zero) and then made to move:

| manual | counter | proved |
|---|---|---|
| E.9.12 Drill Holes / Bolted Joints | `holeFields` | 0 → 1 ✓ |
| E.9.12 Boolean Operations | `subBodies` | (B.12) |
| E.9.13 Flat Cuts | `cutPlanes` | 0 → 1 ✓ (and it shortened the beam 6000 → 5875) |
| E.9.14 Poly-Cuts | `polyCuts` | 0 → 1 ✓ |
| E.9.15 Edge Processing | `facets` | (B.12) |
| E.9.16 Notches (Outlets) | `outlets` | ❌ see below |

**E.9.12 Boolean** names the fields `Length` / `Width` / `Height` / `Operation` / `Type` of
the **discharge-solid** (the cutting body), and ⚠️ *"it is important here to keep the modeller
on **'Variable'** to be able to switch over the modeling of the complete component part
correctly"* — the third face of the ACIS/facet warning. There is also a button that
**creates the discharge-solid as a new element**, i.e. the cutting body can be recovered as
a real object.

**E.9.13 Flat Cuts** = `Phi Dx` / `Phi Dy` / `Center of Rotation`.
**E.9.14 Poly-Cuts** = `Length` / `Offset`.
**E.9.16 Notches** = `Length` / `Width` / `Depth` / **`Insertion Position`**.

### ❌ Notches — unresolved, and B.12's note corrected

✅ **Corrected for certain.** `OutletType` is
`kUndefinedOutlet, kOutletRectangle, kOutletTriangle, kOutletArc, kOutletInversArc`
— so **0 is UNDEFINED**, and B.12's note that *"types 0/1/2 = rectangular / wedge / circular"*
is **wrong**. The real mapping is **1 = rectangle · 2 = triangle · 3 = arc · 4 = inverse arc**.
(The enum trap again: values are measured from the surface dump, never inferred.)

❌ **Not resolved.** `outlet` creates nothing here — all four types, with and without the
third dimension, across three normals: `applyRc=0` and `outlets` stays 0, on a clean beam as
readily as on a modified one. **B.12's claim that the counter climbed 0 → 1 → 2 → 3 is not
reproducible and is hereby downgraded to unverified.**

Two hypotheses were tested and both failed:
1. *The missing third dimension.* E.9.16 names Length / Width / Depth and `PsOutlet` has
   `SetLength` — which the op never called. **Added; still `applyRc=0`.**
2. *The wrong type numbers.* Corrected as above. **Still `applyRc=0`.**

Untried, for whoever returns to it (worth three strikes, no more):
`SetXYPlane(XAxis, YAxis)` — the same *zero-plane* failure mode that stretched a rafter to
317 000 mm in B.26 · `SetXPosition/SetYPosition/SetZPosition(PositionSelection)` — which is
E.9.16's own **Insertion Position** field, values `kLeft, kRight, kDown, kTop, kCenter,
kGravity, kPitch, kUser` · `SetAutomatic(bool)` · `SetFlag(int)`.

---

## E.9.17 Common Properties

**Group** — the group / subgroup / assembly the part belongs to, with buttons to walk the
hierarchy. Reachable through the B.28/B.29 group ops.

**Logical Links** — ⭐ *the links are listed **on the part**, not on the connection.* Fields:
`Active` (and it can be **deactivated** here) · `Index` · `Link Type` · **`Modification`**
(*"the modifications of the part caused by this link"*) · `Name` · `Ident`.
⚠️ *"`Ident` and `Name` only exist when the option **Logical Links/Extended Input** has been
activated in the global settings"* — which explains why those two often read empty.

> **This is the correct end of last session's `connverify` failure.** `PsEditConnection` has
> no binder, so `LinkType` always read `kUndefinedLink`. `PsEditLogicalLink` — bound to the
> **part** via `SetObjectId` — works, and `connscan` has been using it all along.
>
> COM offers more still: `Ks_ComEditLogicalLink` gives `LogicalLinkCount`,
> `GetLogicalLinkByNumber`, **`SetLogicalLinkByNumber`** and
> **`RemoveLogicalLinkByNumber2(n, DeleteParts, Verbose)`** — deleting a **single** link, with
> or without its parts. And `IKs_ComLogicalLink` carries `Set…LinkData` for all eleven
> connection kinds. **That is a route to EDIT an existing connection's parameters, which is
> B.27's open problem — noted here, not chased, because it is not this chapter's scope.**

**Assignments** — see tab 6 above.

---

## What was built

`op=propfull` · `op=propset` · `op=propcopy` · `op=changesection` (plugin v128 → v131)

### `propfull handle=X [tab=N]`
All ~120 properties under the dialog's own tab headings, each marked `rw` or `r-`.
93 readable on a shape, 3 not applicable. Writes `eb_propfull.txt`.

### `propset handle=X <PropertyName>=<value> …`
Any writable property, matched case-insensitively. Sets, calls `writeTo(oid)`, then
**re-reads from a fresh instance and reports before → after per field**, because a
`writeTo` returning 0 proves exactly as much as a `Create()` returning true — nothing.
Result line carries `stuck=` / `ignored=`.

### `propcopy src=X dst=Y [tabs=4,6] [dryrun=1]`
"Match properties" for ProSteel, on `copyFrom()`. **Defaults to tabs 4+6 — Data and
Assignments, the non-geometric identity** — because an unfiltered `copyFrom` carries
`Origin`/`XAxis`/`Length` and would move and resize the target. Measures the destination's
extents before and after and prints `geom=same` or `geom=MOVED` either way.
Measured: 25 fields copied, 62 skipped, destination unmoved.

### `changesection handle=X key=HE400B [cat=] [force=1]`
`PsObjectProperties.ChangeShapeType(oid, Key, Katalog, ShapeType)` — **swap the section of an
existing shape in place**. The modeller's own move: an IPE300 that has to become an IPE500
after a load change, without rebuilding it.

Measured on a free beam, IPE300 → IPE500:

```
key    IPE300 -> IPE500      length 6000 -> 6000   (kept)
L/W/H  6000/150/300 -> 6000/200/500                (IPE500 flange = 200 ✓)
weight 253.2 -> 544.2 kg      (42.2 and 90.7 kg/m × 6 m ✓)
extents grew symmetrically about the same midline
```

The `ShapeType` argument is derived from the part's own `ObjectType` rather than passed
blind — ProSteel keeps the shape *system* (normal / weld / combi / sopro / dawa) separate
from the section key, and passing the wrong one would move the part between systems as a
side effect.

---

## 🚨 THE FINDING THAT MATTERS: a section change BREAKS a connection

Two identical specimens, built the same way — HE300B column, IPE300 beam, end-plate joint
from `example/example3`. Specimen A left alone; specimen B given IPE300 → IPE400 by
`changesection`. **Counted by AutoCAD class, not by the bolt matcher:**

| | Ks_Shape | Ks_Plate | Ks_Bolt |
|---|---:|---:|---:|
| A — untouched | 4 | 8 | **6** |
| B — after the section change | 4 | 8 | **4** |

**Two bolts destroyed, and `vfy_bolts` found their two holes still in the end plate,
abandoned.** The section swap itself is correct. The **joint** is left inconsistent — and an
inconsistent joint is a wrong shop drawing that nothing downstream complains about.

**Remedy, proven immediately after:** `connkill` → change the section → rebuild the
connection. Result: 6 bolts, 12 holes, 6 matched, **0 orphans**.

⇒ **`changesection` now REFUSES on a part that carries logical links**, naming the count and
the remedy, and requires `force=1` to override. It reports `links=n->m` either way. Same
family as the blast guard: an instrument that can quietly break a model must say so.

---

## Two regressions this chapter caught in existing ops

1. **`plate layer=` was silently dropped.** The layer was applied on the `via=ecs` branch
   only — and nearly every plate takes `via=matrix`. Found because E.9.17's Assignments tab
   made me read `LayerName`, which came back `0` on a plate created with `layer=E09-props`.
   **A parameter an op accepts and then ignores is worse than one it refuses.** Fixed.
2. **The four new ops were not in the parameter allowlist at all**, so they were unfiltered.
   Registered. (`propset` is deliberately left open — any of ~120 property names is a legal
   key, and it reports `UNKNOWN PROPERTY` itself, which is a better error than a generic
   refusal.)

---

## The band in the model — `E-structural-elements.dwg`, x 0 – 44 000

Every specimen is labelled **through E.9's own Data tab**, so the model documents itself:
`Note1` says what the part is for, `Note2` what was measured on it.

| x | handle | what |
|---|---|---|
| 0 – 6 300 | `2BE` `2C9` | **specimen A** — intact bolted joint. The column is green with a centre line because `propset` wrote `ColorIndex=3` and `CenterLineMode=1` |
| 8 000 – 14 000 | `2C7` | modifications rig — IPE300→IPE500, plus a drill / flat cut / poly-cut. **The bare hole is deliberate** |
| 20 000 – 26 300 | `2E7` `2E8` | **specimen B** — the section-swap test, joint killed and rebuilt |
| 30 000 – 32 000 | `313` `314` `315` | `propcopy` source and destination, plus the plate-layer regression |
| 40 000 – 44 000 | `316` | the notch attempts — **nothing was created** |

**Verification kit over the whole band:** `vfy_bolts` → 12 bolts, 25 holes, 12 matched,
**BOLT-NO-HOLE = 0** 🧲, HOLE-NO-BOLT = 1 (the deliberate one) · `vfy_size` → 41 parts,
0 oversize, largest 6 130 mm.

---

## Open, carried forward

- **Build the `KlemmLen` check** — grip length vs the sum of connected thicknesses. A
  statement from ProSteel instead of from a proximity matcher.
- **Notches** — the four untried setters above.
- **`Ks_ComEditLogicalLink.Set…LinkData`** — editing a live connection. B.27's problem,
  reachable from COM. **Ask before starting: it is a different chapter.**
- `Mounting Space` — ProSteel will draw wrench clearance. Not yet touched.
- `PPS Z-Part` (E.9.5) has no counterpart in `PsObjectProperties`.
