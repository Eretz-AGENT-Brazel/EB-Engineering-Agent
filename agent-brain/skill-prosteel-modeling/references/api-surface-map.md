# The ProStructures API surface — look it up before you build it

*Extracted 02/08/2026 by reflection over the installed DLLs. Not from the web, not from memory.*

```
C:\Program Files\Bentley\ProStructures Ss6 R1\AutoCAD 2015\Prg\
  161 DLLs · 116 managed · 20,802 types · 8,622 public · 24,946 signatures · 3 failures

  RELEVANT — the actual ProStructures API:
    ProStructuresNet.dll             392 public   the main .NET API
    Interop.ProSteel_COM.dll         631 public   a PARALLEL COM API, never touched
    PSN_*.dll (62) · PC3D*.dll (12)  ~200         connection macros + concrete, all managed
    Bentley.Structural.Ism.Api.3.0   184 public   ISM — interop with analysis packages
    System.Data.SQLite.x64            69 public   removes the Access-MDB bitness problem
                                    ────────
                                    ~1,400 relevant public types

  NOT the API — third-party, do not count them:
    DocumentFormat.OpenXml         4,364        Office export
    combit.ListLabel21.*          ~2,150        the REPORT ENGINE (part lists are L&L reports)
    aSa.PC.*                        ~160        third-party rebar detailing
```

**The agent was using 26 of ~1,400.**
Full map: `EB PROSTEEL AGENT\api+knowledge-develop\knowledge\api\API-SURFACE-RAW.txt` (3.0 MB, every signature) ·
per-assembly counts: `knowledge\api\API-ASSEMBLY-INDEX.txt` ·
guide with the "hand-built vs already exists" table: `knowledge\api\API-SURFACE.md`.

> **This number was wrong three times before it was right:** 325 (one assembly) → 771
> (75 assemblies) → 8,622 (all of them, unfiltered) → **~1,400 (filtered).** The first pass
> concluded "there is no end-plate or gusset class" while `PSN_BasePlate.dll` and 73 other
> managed assemblies sat unopened in the same folder. **A conclusion from one sample is not a
> conclusion, and an unfiltered count is not a finding.**

## How to extract it again (any assembly, any version)

PowerShell, ReflectionOnly, with a resolver so parameter types resolve:

```powershell
$prg="…\Prg"; $acad="C:\Program Files\Autodesk\AutoCAD 2015"
$h=[System.ResolveEventHandler]{ param($s,$e)
  $n=(New-Object System.Reflection.AssemblyName $e.Name).Name
  foreach($d in @($prg,$acad)){ $p=Join-Path $d ($n+".dll")
    if(Test-Path $p){return [System.Reflection.Assembly]::ReflectionOnlyLoadFrom($p)} }
  try{[System.Reflection.Assembly]::ReflectionOnlyLoad($e.Name)}catch{$null} }
[System.AppDomain]::CurrentDomain.add_ReflectionOnlyAssemblyResolve($h)
```
Without the resolver, `GetParameters()` throws on every method that touches `Acdbmgd` or `System.Data`,
and you silently get names without signatures. **With it: 4,796 signatures, 0 failures.**

⚠ This is *type* metadata only — safe. **Blind reflection over live `Ks_*` objects crashes AutoCAD.**

---

## The table that matters: hand-built vs already there

| What was built by hand (and what it cost) | What already exists |
|---|---|
| Anchor bolts — copy a bolt, rotate, compute length. **~12 fix rounds, 400 `bolt` failures** | `PsCreateFastener.CreateFastenerStraightAnchorBolt(Dm, **Extrusion**, TopEmbedment, MiddleEmbedment, BottomEmbedment, ThreadLength, **PlateThickness**, **GroutThickness**, …)` + Hook / Bend / HeadBolt / HexStud / RoundStud |
| `replicate` = `Database.DeepCloneObjects` + `Matrix3d` (AutoCAD level) | `PsMiscTools.ObjectsCopy(PsSelection, PsMatrix) → PsSelection` |
| Mirroring — never implemented (39 mirrors missed in lesson 3) | `PsMiscTools.Mirror3d(Id, p1, p2, p3)` |
| Duplicate hunting by hand. **754 and 642 anchors instead of 480** | `PsSelection.RemoveDuplicates()` |
| Box selection that re-selected its own copies → snowball | `PsSelection.SelectAllObjectsInRange(…, Min, Max)` + `SetSelectionFilter` |
| "I have no eyes" — relationship checks never written | **`PsGeometryFunctions`** — 47 methods with `SetTolerance` |
| Detail didn't rotate as a unit; base plate stayed behind | `PsObjectGroup` · `PsCreateAssembly` · `…LinkDataMgd.CreateGroup` |
| Flange orientation — wrong twice | `PsBaseplateLinkDataMgd.PlateInShapeDirection` |
| Hole pattern 2×3 instead of 3×2 | `PsDrillObject.SetLinearHoleField(dia, XField, YField)` — explicit axes |
| Rib chamfer drawn as a polygon (214 ribs) | `PsEdgeChamfer` (`EdgeLayout`, `TopVar1/2`, `DownVar1/2`) |
| Slotted holes — open debt since lesson 2 | `PsDrillObject.SetRotateSlottedHoles` + `LongHoleMode` + `HoleGeometrie` |
| Copying drilling between parts, hole by hole | `PsDrillObject.TakeoverDrills(Source, Target)` |
| Clash detection — never built | `PsCollisionCheck` — 22 pair flags, `Apply()`, `BodyCount` |
| Part numbering — never built | `PsCreatePositioning` + **`RecordIdenticalRecord`** (the software finds identical parts itself) |
| Part list — never built | `PsCreatePartlist.CreateMDBFile(File, Template, Selection)` |
| Plate development | `PsCreateUnfold` — `GetOuterGeo`, `GetInnerGeo` |
| Concrete as illustrative `AcDb3dSolid` | `PsCreateConcreteSlab / Wall / Footing` + full rebar (33 classes) |
| Image export — `PNGOUT` wrote no file | `PsMiscTools.CreateStlFile / CreateSatFile` · `PsSelection.WriteXmlFile` |
| Welds — never modelled | `PsCreateWeldFlag` · `WeldSeam*` / `WeldTo*` on every connection |

## The eyes that already exist — `PsGeometryFunctions`

Root causes #1 and #2 in the retrospective ("I verify counts, not relationships" / "I have no eyes")
have a ready-made instrument, with tolerance built in:

```csharp
Double  GetDistanceBetween      (PsPoint a, PsPoint b)
Double  GetDistanceToPlane      (PsPoint p, PsPoint origin, PsVector normal)
Int32   IsPointOnLine           (PsPoint p, PsPoint s, PsPoint e, Int32 type)
Int32   IsVectorPerpendicularTo (PsVector v1, PsVector v2)
Int32   IsVectorParallelTo      (PsVector v1, PsVector v2)
Int32   IsVectorAlignedTo       (PsVector v1, PsVector v2)
Double  GetAngleBetweenVectors  (PsVector v1, PsVector v2)
PsPoint OrthoProjectPointToPlane(PsPoint p, PsPoint origin, PsVector normal)
Void    SetTolerance            (…)
+ ~35 more: cross/dot products, line and plane intersections, arcs, polar points
```

| Real error from lessons 4–5 | The check that would have caught it |
|---|---|
| Flange orientation reversed, twice | `IsVectorPerpendicularTo(flangeNormal, wallNormal)` |
| Wall anchors floating in mid-air | `GetDistanceToPlane(head, plateFace, n) == 0` |
| Nut floating 10 mm — shipped twice | `GetDistanceBetween(nutTop, rodEnd) ≤ tol` |
| Column floating 180 mm above the slab | `GetDistanceToPlane(colBase, slabTop, +Z) == 0` |
| Hole row on the wrong axis | `IsVectorParallelTo(holeAxis, plateLongAxis)` |
| 754 anchors instead of 480 | `PsSelection.RemoveDuplicates()` |

## Four whole capabilities never touched

- **`PsObjectGroup`** — `AddSubParts(PsSelection)`, `Create()`, `CreateAssembly(origin, X, Y)`,
  `getAllPartsOf`, `computeWeight(id, withoutBolts)`, `ComputeDimension(id, out L, out W, out H)`,
  `WeightCenterOfGroup`. **This is Amir's "build one detail, then replicate" as an API object.**
- **`PsCollisionCheck`** — `CheckShapeToShape`, `CheckPlateToPlate`, `CheckBoltToBolt`,
  `CheckConcreteToShape`, … 22 flags · `UseBoltMountSpace` · `MinVolume` · `Apply()` → int · `BodyCount`.
- **`PsCreatePositioning`** — `SetColumnPrefix/BeamPrefix`, `SetLengthTol/HolesTol/WeightTol`,
  `SetEqualPart*`, and the `Record*` family incl. `RecordIdenticalRecord`, `RecordWeight`, `RecordHandle`.
- **`PsCreatePartlist`** — `CreateMDBFile`, `PerformPartlist2`, `GetPartlistTemplateNames`,
  `SetTolerances`. ⚠ Reading the MDB from Python needs a 64-bit ACE OLEDB provider — not verified
  present. Read it inside AutoCAD and emit TSV, or use the DBF path (`PsDBaseDatabase`).

## Drilling, in full — `PsDrillObject` (31 methods)

```
SetSingleHoleField(dia)                       SetLinearHoleField(dia, XField, YField)   ← explicit axes
SetRadialHoleField(dia, radius, count)        SetRadialHoleRange(from, to)
SetHoleType · SetHoleBoltType · SetDrillType  SetRotateSlottedHoles(bool)  ← slotted holes
SetHoleDepth · SetDeepStart · SetHoleCounter · SetHoleStep
SetHoleWorkloose(v)   ← HOLE CLEARANCE: M16→⌀19, M20→⌀23 (3 mm, Amir's shop rule)
SetXYPlane(X, Y) · SetNormal · SetInsertPoint · SetCoordinateSystem
SetXPosition / SetYPosition (PositionSelection) · SetXOffset / SetYOffset
SetIgnoreInnerContour · SetMidLineAlignement
TakeoverDrills(DrillSource, DrillTarget)      ← copy drilling between parts
Apply() → int · GetModifyIndex()
```

## Also installed, also never opened

- **`Prg\Plugins\*.chm`** — 34 help files, one per connection macro (BasePlate, BeamColumnEndPlate,
  DualGusset, Array3D, …). ⚠ `hh.exe -decompile` did not work non-interactively and 7-Zip is absent;
  extraction is an open item.
- **`Samples\COM Macros\`** — 25 worked sample DWGs, one per macro.
- **`Samples\Detailing\`** — 12 samples covering Assembly / Group / Connection dimensioning.
- **`knowledge\learning\manual\manual_fulltext.txt`** — the 1,179-page ProSteel manual, extracted since 18/06/2026.

## Reading order

1. `PsGeometryFunctions` — the eyes
2. `PsMiscTools` — copy / mirror / export
3. `PsSelection` — selection, filtering, duplicate removal
4. `PsCreateFastener` — anchors
5. `PsObjectGroup` + `PsCreateAssembly` — the detail as a unit
6. `PsDrillObject` — drilling in full
7. `Ps*LinkDataMgd` — the parameter set of each connection
8. `PsCollisionCheck` · `PsCreatePositioning` · `PsCreatePartlist` — quality gates
9. `Bentley.ProStructures` — the 127 enums that feed all of the above
