# B.19 Web Angle — chapter notes

*Read end to end 09/08/2026, pages 293–302 (fulltext lines 7041–7233). Command **`PS_STEGW`**
(Stegwinkel). API mapped from `API-SURFACE-RAW.txt`; nothing measured against a live model yet.*

> *"The shape to be connected is **cut to length** after the exact definition is entered. The
> connection, **drilling and bolting is carried out automatically**."*

⭐ This is the first chapter where one command does the **whole detail**: it cuts the beam back,
makes the angles, drills both legs, bolts them, welds them, and can cope the beam — from one pick
pair. Everything B.14/B.15 do by hand is a by-product here.

## Creating one

1. click the **shape to be connected**
2. click the **supporting shape** — or press **ENTER / right-click if there is none**

⭐ *"Two web angles will be fastened **opposite of each other on the ends** of the shape to be
connected **when a support shape is not selected**."* So no support ⇒ angles at both ends.

⚠️ *"The web angle command **retains the last connection settings**. Using this setting creates
the connection."* The dialog is stateful — a scripted run inherits whatever the last one left.

## Shapes

| field | meaning |
|---|---|
| `Shape Class,...` | the angle section. ⚠️ *"**Only** shapes that are equal-sided angles and unequal-sided angles can be selected"* |
| ⭐ `Use Flat` | the angle is made from **bent plate** instead of rolled angle. Give `Thickness`, `Long Leg`, `Short Leg`, `Bent Radius` — *"the program will determine the **actual length of the steel plate**"* (i.e. the development) |
| `Position` | `Left` or `Right`. ⭐ *"If **both** have been checked, a web angle is created at **each side of the web**"* |
| `Turn Angles` | swaps long and short leg so the **long side sits at the connecting shape** |
| `Gap` | clearance between the end of the connecting shape and the supporting shape |
| `Side Offset` | between the web of the connected shape and the angles — *"normally 0 or a slight clearance"* |
| `Vertical Offset` | shift off the connected shape's axis, up or right depending on position. **Negative reverses it** |

The `Vertical Offset` datum is a three-way choice, and it matters:

| option | measured from |
|---|---|
| `From Edge` | **upper edge of the shape** → upper edge of the web angle |
| `Lower Edge` | the **lower** edges instead |
| ⭐ `Up to First Bolt` | the **centre of the first bolt**, not an edge at all |

## Distances — three independent hole directions

| `Number` | holes where |
|---|---|
| `Shape Direction` | in **both** legs, crosswise to the connected shape |
| `Connect. Shape` | in the **long** leg (parallel to the connected shape) |
| `Support Shape...` | in the **short** leg (crosswise to the connecting shape) |

`Inner Distances` — *"refer to the **outer edge of the angle shape** for each direction"*.
`Dist. Between` — spacing between two holes, per direction.

⭐ **Slotted holes fall out of the same page:** *"If you indicate an additional **Slot Length** in
the input fields beside Number, the drill holes are carried out as **slotted holes** with the
indicated slot length."*
⚠️ `Absolute Inner Distance` — when checked, *"the inner distance of the holes in the short leg
means the distance **between the holes**"* — the datum changes meaning, it is not a mere offset.

## Connect · Welding · Cope

**Bolts:** `Bolt Style` (e.g. DIN 7990) · `Dia.` · `Workloose` *(the clearance, "usually 2 mm")* ·
`Diagonal Offset` *(a shift of the bolt axes between support and connecting bolts)* ·
`Gap Spacing` *(web of the supporting shape → the web angle)*.

**Welds:** `Weld Style`, and independently `Weld Side of Connecting Shape` / `Weld Side of
Supporting Shape`, each with its own thickness.

**Cope** — *"you can add a cope to the connecting shape"*: from a stored **template** or entered
directly; `Connect. Shape` = `Upper Side` · `Lower Side` · `Both`; plus a `Gap`.
`Safety...` adds a **safety cope**, and *"the inserted angle can be **shortened**"*.

## ⭐⭐ Standard Data — the first LOAD-driven selection in the manual

> *"You can select a web angle **according to the DAST guidelines**. Enter the desired **load**
> in the input fields. The **possible connection angles will be displayed** in the selection list."*

`H(kN)` and `Hz(kN)` go in; the database returns the connections that carry them. The list shows
`Shape` · `Bolt` · `Dia` · `Material` · `Shape Direction` (holes along) · `Transversal Dir.`
(holes across) · `W1, W2, W3…` distances.

⚠️ **Scope note for EB:** the company fabricates, it does not design the loads. This page is a
*lookup into a standard's own table*, not an engineering calculation of ours — useful for value
engineering and for reading what a designer specified, never as design authority.

## Form Group

`Create Group` (parts grouped on insert) · `With Bolts...` (bolts **and weld seams** join the
group) · ⭐ `Each Angle` (an individual group **per angle** rather than one for the connection).

---

## The API

`Bentley.ProStructures.Connection.Standard.PsWebAngleConnection`
`SetConnectionObjectId` (the beam) · **`SetSupportObjectId`** (the column — omit for the
both-ends case) · `SetConnectionPoint` · **`SetKatalog` + `SetKey`** (the angle section, the
`Shape Class` field) · `SetConnectionData` · `Create()` · `Check()` · `GetLink()` ·
templates (`GetTemplateCount` / `GetTemplateName` / `GetTemplate` / `AddTemplate`).

⭐ **Read-back exists here, unlike most connection classes:** `GetPlateId(Int32 Which)` returns the
created parts, and `get_PlateDataCount()` / `GetPlateDataName(Int32)` / `GetPlateData(String)`
reach the **DAST database entries**.

`PsWebAngleLinkDataMgd` — the dialog, field for field:

| dialog | property |
|---|---|
| `Use Flat` + its four inputs | `WebAngleIsFlatSteel` · `FlatSteelThickness` · `FlatSteelLongSide` · `FlatSteelShortSide` · `FlatSteelBendRadius` |
| `Position` · `Turn Angles` | `WebAnglePosition` *(Int32)* · `TurnWebAngles` |
| `Gap` · `Side Offset` · `Vertical Offset` | `DistanceToConnected` · `InnerOffsetSide` · `InnerOffsetVertical` |
| `From Edge` / `Lower Edge` / `Up to First Bolt` | `InsertOffsetFromShapeEdge` · `InsertOffsetFromDownSide` · **`InsertOffsetFromFirstHole`** |
| the three `Number` fields | `VerticalHoleCount` · `HorizontalHoleCountConnected` · `HorizontalHoleCountSupport` |
| inner distances / spacings | `HoleDistanceVerticalEdge` · `HoleDistanceVertical` · `HoleDistanceHorizontalEdgeConnected` · `HoleDistanceHorizontalConnected` · `HoleDistanceHorizontalEdgeSupport` · `HoleDistanceHorizontalSupport` |
| `Absolute Inner Distance` | `HoleDistanceHorizontalIsAbsolute` |
| **Slot Length** | `SlotAxisDistanceConnected` · `SlotAxisDistanceSupport` |
| bolts | **`BoltStyle` (a STRING)** · `BoltStyleCRC` · `BoltType` · `HoleDiameter` · `HoleWorkLoose` · `HoleAxisOffsetVertical` *(Diagonal Offset)* · `DistanceToSupportWebAngle` *(Gap Spacing)* |
| welds | `WeldStyleCRC` · `WeldToConnectedShape` · `WeldToSupportShape` · `WeldSeamConnect` · `WeldSeamSupport` · `WeldSeam` |
| cope | `CreateCope` · `SetCopeFromTemplate(String)` · `CopeRadius` · `CopeWebDistance` · `CopeDistanceEdge/Inside/Outside` × Top/Down · `CopeShapeFitType` · `CopeAlignToInnerEdge` · `ShortenAngle` *(the Safety option)* |
| cutting the beam back | `CutAlways` · `CutAtConnected` · `CutAngleOnTopLeft/TopRight/DownLeft/DownRight` · `CutBothSidesOnTop/Down` |
| `Create Group` / `With Bolts` / `Each Angle` | `CreateGroup` · `AddBoltsToGroup` · **`GroupIsSingleGroup`** |
| ⭐ **the DAST loads** | **`ShearX/Y/Z`** and **`MomentX/Y/Z`** |

⭐ **`BoltStyle` is a plain string here** — the stiffener class exposed only a `WeldStyleCRC` with
no name. So bolt style is settable directly, and the CRC is available alongside it.

⭐ **`ShearX/Y/Z` + `MomentX/Y/Z` are the first load-carrying fields found anywhere in this API.**
Whether they *select* a connection or merely record it is unknown and must be measured.

⚠️ **The manual's own command index is shifted by one chapter** in this range — it lists
*Webangle B.18*, *Shear Plates B.19*, *Butt-Joint B.20*, *Purlin B.21*, *Stiffeners B.15*, all one
behind. Navigate by the table of contents, never by the command index.

---

# MEASURED 09/08/2026

*Band at x ≥ 110000: four column/beam bays (HE300B + IPE400), **46 objects**.*

> ⚠️ **CORRECTION, 09/08 (found while reading B.23).** This section first recorded the angles as
> `BS EQUAL :: EA90x90x9`, because that is what was passed to `SetKey` / `SetKatalog`. It was
> never read back. The angles are in fact **`L90X9` from `DIN.DIN_WINK_GL`** — and
> **`EA90x90x9` does not exist**: `BS EQUAL` has 6, 7, 8, 10 and 12, no 9.
> See *"`SetKey` and `SetKatalog` do nothing"* below.

## The chapter's central claim is true

> *"The connection, **drilling and bolting is carried out automatically**."*

Read back off the beam with `mods`:

| beam | holeFields | polyCuts |
|---|---|---|
| before any connection | **0** | **0** |
| after a web angle | **1** | **1** |

One command drilled the beam **and** cut it back. Nothing else was asked for.

## What a connection actually produces

| variant | objects | breakdown |
|---|---|---|
| 2-bolt, with support | **8** | 2 × `Ks_Shape` angle **90×90×135** + 6 bolts |
| 3-bolt, with support | **11** | 2 × `Ks_Shape` angle **90×90×210** + 9 bolts |
| `Use Flat` cleat | **8** | 2 × **`Ks_BendShape`** 90×90×135 + 6 bolts |

⭐ **The angle length is derived from the bolt count**: 135 for two rows, 210 for three — a
difference of **75**, which is exactly the templates' own `vertOff = 75`.

⭐⭐ **`Use Flat` changes the ENTITY CLASS**, not just the shape: `Ks_BendShape` instead of
`Ks_Shape`. **A model audit that counts only `Ks_Shape` will not see these cleats at all.**

## The three shipped templates

| template | rows | bolt style | vertOff | cope | group |
|---|---|---|---|---|---|
| `default/Standard` | 2 | `8.8S` | 0 | no | no |
| `AutoConnect Metric v 18/2 Bolt` | **2** | `DIN7990` | **75** | yes | yes |
| `AutoConnect Metric v 18/3 Bolt` | **3** | `DIN7990` | **75** | yes | yes |

Both AutoConnect presets set `fromEdge = true` **and** `fromHole = true` — the 75 is measured from
the shape edge **to the first bolt centre**.
⭐ Here `BoltStyle` came back as a **readable string** (`'DIN7990'`, `'8.8S'`) **together with** its
CRC (`-1614854285`, `-401163854`) — the first place in this API where a style name and its
checksum appear side by side.

## ⛔ Three things that do not work

1. **No support shape ⇒ no connection.** The manual says press ENTER for the both-ends case;
   omitting `SetSupportObjectId` gives `Create()` **false** and zero objects. `Check()` still
   returned **1** — again not a predictor.
2. **`CreateCope = true` is inert.** A connection made with it is byte-identical in modifications
   to one made without (`holeFields=1 polyCuts=1` either way). Adding real geometry —
   `CopeDistanceEdgeTop 40`, `CopeDistanceInsideTop 60`, `CopeRadius 10`, `CopeWebDistance 15` —
   **still produced no extra modification.** ⇒ The cope is not reachable from this dialog's data.
   There is a dedicated `PsCopeConnection` in the same namespace, which is the likely real route.
3. **`GetPlateId` returns nothing** — correctly, as it happens: **a web angle produces no plates
   at all**, only shapes and bolts. The method is inherited and its name misleads.

## ⚠️ The DAST database is EMPTY on this installation

`get_PlateDataCount()` = **0**. The load-driven selection (`H(kN)` / `Hz(kN)` in, connections out)
has an API and a dialog but **no data behind it here** — nothing to select from. The
`ShearX/Y/Z` and `MomentX/Y/Z` fields exist and are settable, but with an empty database there is
nothing for them to drive, and whether they *select* or merely *record* remains unmeasured.

## ⛔ `SetKey` and `SetKatalog` do nothing — measured

Four connections, identical apart from the section asked for:

| asked for | got |
|---|---|
| `EA100x100x10` @ `BS EQUAL` *(valid)* | **`L90X9` @ `DIN.DIN_WINK_GL`** |
| `L120x12` @ `DIN WINKEL GLEICH` *(valid)* | **`L90X9` @ `DIN.DIN_WINK_GL`** |
| `L120x12` @ `DIN.DIN_WINK_GL` *(valid, stored form)* | **`L90X9` @ `DIN.DIN_WINK_GL`** |
| **nothing at all** | **`L90X9` @ `DIN.DIN_WINK_GL`** |

⇒ **`PsWebAngleConnection.SetKey()` and `SetKatalog()` are complete no-ops.** The angle section
comes from the **template**, and neither an invalid key nor a valid one changes it. Both accept
anything and report nothing. To change the section, the template must be changed.

⚠️ **Two naming schemes for the same catalogue.** `dumpcat` and `PsShapeLoader` use
`DIN WINKEL GLEICH`; the model stores `DIN.DIN_WINK_GL` — a `<library>.<catalog>` form. A key that
reads back one way is not the string you look it up with. (Same for `DIN FLACHEISEN` ↔
`DIN.DIN_FLACH`, which is how B.20/B.21's default "plates" show up as catalogue flat bars.)
