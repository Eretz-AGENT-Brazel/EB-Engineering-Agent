# Beam connections in ProSteel — the API surface, mapped before the lesson

*Built 02/08/2026 by reflection over `ProStructuresNet.dll`, ahead of lesson 6 (beam connections and
beam modelling). Nothing here has been run yet — this is the map, not the measurement.
**Every claim marked ⚠ must be verified in the model before it is repeated as fact.***

---

## The one structural difference from a base plate

A base plate has **one** member. A beam connection has **two** — and the API says so:

```csharp
conn.SetConnectionObjectId(beamId);    // the member being connected  (the beam)
conn.SetSupportObjectId(supportId);    // the member it lands on      (column / girder)
conn.SetConnectionPoint(pt);           // which end / where
```

`SetSupportObjectId` does not exist on `PsBasePlateConnection`. Getting this pair the right way round
is the first thing that can go wrong, and it will look plausible either way.

## All six classes share one shape — learn it once

Every connection class in `Bentley.ProStructures.Connection.Standard` has the identical surface:

```csharp
var c = new PsShearPlateConnection();          // or WebAngle / Cope / Haunch / Purlin / StandardPlate
c.SetToDefaults();

int n = c.GetTemplateCount();                  // ← ENUMERATE THE FACTORY'S OWN TEMPLATES
string nm = c.GetTemplateName(i);              //    never invent numbers
var d  = c.GetTemplate(nm);                    //    → Ps*LinkDataMgd, fully populated

d.HoleDiameter = 23;                           // change ONLY what the drawing dictates
c.SetConnectionData(d);

c.SetConnectionObjectId(beamId);
c.SetSupportObjectId(supportId);
c.SetConnectionPoint(pt);

int rc = c.Check();                            // validate BEFORE creating
bool ok = c.Create();

// read back what it actually produced — this is the verification instrument:
int plates = c.get_PlateDataCount();
long pid   = c.GetPlateId(0);
var link   = c.GetLink();
```

**`GetTemplateCount` / `GetTemplateName` / `GetTemplate` is the antidote to root cause #4
("invention instead of inheritance").** The base-plate disaster — invented `grip=400`, `key=36`,
428 mm anchors — happened because `new Ps…LinkDataMgd()` was used instead of `GetTemplate()`.
An empty parameter object has **every field zero**.

> **First action of lesson 6, before anything is modelled:** run `GetTemplateCount()` +
> `GetTemplateName(i)` on all six classes and write the list down. That is the factory's own
> vocabulary, and it takes seconds.

---

## The six classes, and which real joint each one is

| Class | The joint | PS_ command | Params |
|---|---|---|---|
| `PsShearPlateConnection` | **Fin plate / shear tab** — one plate welded to the support, bolted to the beam web | `PS_SCHEARPLATE` *(Bentley's own typo)* | 64 |
| `PsWebAngleConnection` | **Double-angle cleat / Stegwinkel** — angles both sides of the web | `PS_STEGW` | 74 |
| `PsStandardPlateConnection` | **End plate**, incl. moment end plates, haunches, backer plates | `PS_ENDPLATE*` | **132** |
| `PsCopeConnection` | **Cope / notch** — cutting the beam end around its support | `PS_NOTCH` / `PS_NOTCH_MAN` | 37 |
| `PsHaunchConnection` | **Haunch / Voute** — a tapered depth increase at the joint | `PS_VOUTE` | 19 |
| `PsPurlinConnection` | **Purlin cleat**, incl. a second purlin (`SetPurlin2Id`) | `PS_PFETTE` | 27 |

Plus already in use: `PsSpliceJointConnection` (`PS_LASCHE`, splice), `PsStiffenerConnection`
(`PS_RIP`, ribs), `PsBasePlateConnection` (`PS_GROUNDPL`).

Additional beam-related macros exist as separate **managed** assemblies in `Prg\`
(⚠ not yet reflected): `PSN_BeamColumnEndPlate` · `PSN_BeamColumnMoment` · `PSN_BeamColumnFlange` ·
`PSN_BeamColumnWeb` · `PSN_BeamColumnSeated` · `PSN_BeamBeamShear` · `PSN_BeamBeamSplice` ·
`PSN_BeamBeamStiffener` · `PSN_BeamBeamClamp` · `PSN_WebMoment` · `PSN_DualGusset` ·
`PSN_AngleSplice` · `PSN_ColumnSplice`. Each ships a `.chm` in `Prg\Plugins\` and a worked sample
DWG in `Samples\COM Macros\`.

---

## Parameters worth knowing before the lesson

### Every beam connection carries its design forces
```
ShearX · ShearY · ShearZ · MomentX · MomentY · MomentZ
```
Present on ShearPlate, WebAngle and StandardPlate. The connection object **stores the forces it was
designed for**. Do not use these for design checking — Phase 2 is locked — but know they exist, and
never overwrite them with zeros by rebuilding a joint from an empty object.

### `HoleWorkLoose` = hole clearance
Present on every class. This is where Amir's shop rule lives: **M16 → ⌀19, M20 → ⌀23, 3 mm clearance**.
Set it from the standard, not from the default.

### `CreateGroup`
Present on every class. **Makes the joint's parts one group.** This is the API expression of Amir's
central principle — the detail is a unit, so it copies and rotates as a unit. The lesson-5 failure
where the base plate stayed behind when the column rotated is exactly what this prevents.

### The cope is built into the shear connection
`PsShearPlateLinkDataMgd` owns the cope directly: `CreateCope`, `CopeRadius`, `CopeShapeFitType`,
`CopeDistanceEdgeTop/Down`, `CopeDistanceInsideTop/Down`, `CopeWebDistance`, `CopeAlignToInnerEdge`.
Likewise on `PsWebAngleLinkDataMgd`. **So a coped fin-plate joint is ONE object, not a connection plus
a separate notch** — do not model the cope by hand and do not add a second `PsCopeConnection` on top.
Use the standalone `PsCopeConnection` only where there is no bolted connection to carry it.

### Ratholes are a real parameter
`PsCopeLinkDataMgd.FirstRatholeDiameter` / `SecondRatholeDiameter`, plus `FirstStraightInFlange`,
`FirstCenterToFlange`, `FirstFaceToFlange`, `Radius`, `EdgeType`. A cope is not a rectangular bite out
of the beam — it has a radius and usually a rathole so the weld can run through. Modelling it as a
plain cut is the beam-connection equivalent of modelling a rib as a rectangle.

### `PsStandardPlateLinkData` is the big one — 132 parameters
It covers, in a single object:
- the end plate itself (`Length`, `Width`, `Thickness`, `PlateType`, `PlateAngle`, `EndPlateIsPolyPlate`)
- **haunches** top and bottom (`HaunchAtTopSide/DownSide`, `HaunchLength`, `HaunchFlangeWidth/Thickness`,
  `HaunchWebThickness`, `HaunchIsCopedShape`, `HaunchStiffenerAtConnected/AtSupport`)
- a **second plate**, a **top plate**, **backer plates**, **web plates**, **filler plates**
- holes (`HoleDiameter`, `HorizontalHoleCount`, `VerticalHoleCount`, the `HoleDistance*` family,
  `HolesAsymmetric`, `HoleListUsed`, `WithoutHoles`)
- welds (`WeldSeamFlange`, `WeldSeamWeb`, `WeldToFlange`, `WeldToWeb`, `WeldStyleCRC`)
- `WithStiffeners`, `WithFillerPlates`, `ConnectionType`

⚠ With 132 parameters, **`GetTemplate()` is not optional** — building one of these from an empty object
is the 428 mm anchor mistake at four times the scale.

### `WebAngleIsFlatSteel`
`PsWebAngleConnection` can produce **flat bar cleats instead of angles** (`FlatSteelThickness`,
`FlatSteelLongSide`, `FlatSteelShortSide`, `FlatSteelBendRadius`). Worth knowing before assuming a
detail needs an angle section that isn't in stock.

---

## What to watch Amir do in lesson 6

Written as questions to answer from the recording, not assumptions:

1. **Which macro does he reach for** for a simple beam-to-column shear connection — fin plate, web
   angle, or end plate? And is it the built-in class or a `PSN_*` plugin?
2. **Does he cope the beam separately, or let the connection do it?** (The API supports both.)
3. **Which template does he start from**, and which parameters does he then change? Everything he does
   *not* change is factory standard and must be inherited, never re-entered.
4. **How does he pick the support vs the connected member** — the order of the two picks.
5. **Bolt count and pitch** — is it from the drawing, from the template, or from a rule of thumb?
6. **Does he weld to the flange, the web, or both**, and what seam size?
7. **What does he do at the beam end that is NOT the connection** — cope, rathole, end cut, camber.
8. **How does he replicate it** across a floor of identical beams. This is the ten-minute question.

## Gates for a beam connection (extend `detail_audit`)

Relationship checks, not counts — the same discipline as the base plate:

| Check | Assertion |
|---|---|
| Beam end reaches its support | gap between beam end face and support face = declared ± 0.5 mm |
| Cope clears the support flange | cope depth ≥ support flange thickness + declared clearance |
| Bolts pass through real holes | for every bolt axis, a collinear hole exists in **both** parts |
| Hole pattern orientation | pattern axes agree with the **web** plane, not the flange plane |
| Plate is on the correct side | plate normal · (support→beam vector) sign as declared |
| Joint is one group | `CreateGroup` produced a group containing plate + bolts + welds |
| Nothing floats | every generated plate touches both members it welds/bolts to |
| No duplicate joints | one logical link per beam end — `PsEditLogicalLink` count == expected |

## Open, unverified — resolve by experiment, not by assumption

- ⚠ Template names and counts for all six classes — run `GetTemplateCount`/`GetTemplateName` first.
- ⚠ Whether `Check()` returns a meaningful code, and what its values mean.
- ⚠ Whether `Create()` on a beam connection shortens or cuts the beam the way `PS_GROUNDPL` shortens a
  column (`ShortenShape`). **Assume it has side effects until measured** — a census delta after every
  create is mandatory.
- ⚠ Whether the 13 `PSN_Beam*` plugin assemblies are reachable the same way as the built-in classes.
- ⚠ What `PS_SCHEARPLATE` (with the typo) is actually registered as in the ARX string table.

---

# ✅ MEASURED 06/08/2026 — this file was written before anything was run

Everything above was the map. This is what the sandbox actually showed.

## The scoreboard

| kind | class | template used | result |
|---|---|---|---|
| **shear** (fin plate) | `PsShearPlateConnection` | `AutoConnect Metric v 18/3 Bolt` | ✅ **verified in full** |
| **webangle** | `PsWebAngleConnection` | `AutoConnect Metric v 18/3 Bolt` | ✅ works (delta 11) |
| **endplate** | `PsStandardPlateConnection` | `example/example1` | ✅ works (delta 5) |
| **haunch** | `PsHaunchConnection` | `Default/Standard` | ✅ works (delta 2) |
| **cope** | `PsCopeConnection` | `AutoConnect .../10mm Cope` | ✅ works — **but creates no objects** |
| **purlin** | `PsPurlinConnection` | `Default/Standard` | ❌ `Create()` false |

**5 of 6.**

## The shear connection, verified by relationship

```
SHAPE 114  HE300B      the column
SHAPE 116  IPE300      the beam
SHAPE 11A  210X10  <-  the fin plate the connection made, at x=-8.55 (half the column web)

holes:  116 (beam web) : 3
        11A (fin plate): 3          <- BOTH parts drilled, by the connection itself
        diameter       : 19 = template's M16 + play 3   (Amir's M16→⌀19 rule)
bolts:  M 16x45 Mu DIN7969, pitch 75, matching the "3 Bolt" template
```
**Both members drilled** is what makes it a connection rather than a plate plus bolts —
the exact thing that failed for three lessons ("304/304 bolts, zero holes").

## The three template families installed here

`GetTemplateCount` / `GetTemplateName` / `GetTemplate` returned **23 templates across all nine
connection classes** in under a minute. The metric family is **`AutoConnect Metric v 18/…`**.

⚠️ **Every template ships with `play` (HoleWorkLoose) = 2.** Eretz Barzel's rule is **3**.
Send it explicitly on every connection or you inherit the German default.

## What measurement corrected

1. **A cope creates nothing — it modifies the beam.**
   `Create()` returned **False**, census delta **0**, and the beam went **4850 → 4840** with its
   end pulled back 100 → 150. Judging by census called a working operation a failure.
   ⇒ `op=conn` now reports `geomChanged` and the member's length/extents before and after,
   and the verdict is `delta > 0` **OR** `geomChanged`.
2. **The connection cuts the connected member.** Always re-measure length afterwards.
3. **`get_PlateDataCount()` returns 0** even when parts were created (endplate returned 1).
   Not a reliable instrument — read the dump.
4. **Ops slow down as the model grows.** `wait=60` sufficed at 18 objects and timed out at 45.
   Two early "failures" were **timeouts**, not failures — I reported them wrongly.
5. **Purlin**: `B.22` says the connection is roof girder + purlin **socket** + purlin **course**,
   and `SetPurlin2Id` expects **two purlin segments**. My test used one continuous purlin
   passing over the rafter. **The geometry was wrong, not the capability.**

## Groups: the result that was not asked for

After `conn … group=1`, querying every object showed the group contained:

```
IN   column (main) · base plate · 4 anchors · THE FIN PLATE
OUT  the beam · the 3 shear bolts
```

**The fin plate joined the column's group by itself** — because it is welded to the column,
so it ships with it. The beam is a separate shipping piece and the bolts go in on site.
**The group structure came out fabrication-correct without being told.** That is what
`B.28` means by groups encoding how the steel ships and erects.
