# The ProSteel .NET API — proven paths and measured dead ends

*Every entry here was tested against a live model and verified by reading the value back.
Ground truth for signatures: `EB PROSTEEL AGENT/app/plugin/api_dump_ProStructuresNet.txt`
(19,268 lines of reflected type metadata — grep it, don't read it whole).*

`ProStructuresNet.dll` lives in
`C:\Program Files\Bentley\ProStructures Ss6 R1\AutoCAD 2015\Prg`.

---

## 1. Holes — the verification instrument

Reading holes is what separates a real connection from a picture of one.

```csharp
PsSingleHoleArray arr = new PsSingleHoleArray(objId, (LongHoleMode)0, false, false, false);
int n = arr.Count;
PsPoint s = new PsPoint(0,0,0), e = new PsPoint(0,0,0);
double dm = 0;
arr.getHole(i, s, e, ref dm);          // start, end, diameter — in WORLD coordinates
bool slotted = arr.getFromSlottedHole(i);   // is it oblong?
double maxlen = 0; arr.getMaximalLength(i, ref maxlen);
```
- Constructor takes the **object id** directly — no `PsPlate` cast needed, which sidesteps the old
  cast failures.
- Works on plates **and** profiles (`Ks_Shape`, `Ks_BendShape`).
- `LongHoleMode`: `kSingleHole=0, kLongHole=1, kDoubleHole=2`.

## 2. Drilling

```csharp
PsDrillObject d = new PsDrillObject();
d.SetToDefaults();
d.SetObjectId(hostId);
d.SetInsertPoint(pt);                  // world coordinates
d.SetNormal(new PsVector(nx, ny, nz)); // hole axis
d.SetSingleHoleField(diameter);
int rc = d.Apply();
// then ALWAYS read the holes back — never trust rc
```
Oblong holes: `SetHoleType((HoleType)…)`, `SetHoleStep(len, dia)`, `SetRotateSlottedHoles(bool)`.
`HoleType`: `kHoleNormal=0, kHoleLenLimited=1, kHoleWeldSign=2, kHoleUndefined=-1`.
`HoleGeometrie`: `kCentric=0, **kLong=1**` — the oblong flag.

**Measured 608 of 616 holes drilled successfully** across a whole platform model (1.3 % failures on
thin tubes).

## 3. Plate contours — read and rewrite

```csharp
PsPlate pl = tr.GetObject(id, OpenMode.ForRead) as PsPlate;   // this cast DOES work
PsPolygon poly = new PsPolygon();
pl.GetPolygon(poly);                   // LOCAL 2-D contour, in the plate's own plane
int nv = poly.Count;
poly.getVertexAsPoint(i, pt);
```
Rewriting a contour **in place**, keeping position, layer and already-drilled holes:
```csharp
PsPlate pl = tr.GetObject(id, OpenMode.ForWrite) as PsPlate;
PsPolygon np = new PsPolygon();
np.init();
foreach (v in verts) np.appendVertex(v.x, v.y, 0.0);   // local coordinates
pl.SetPolygon(np);
pl.RecalculationFlag = true;
pl.computeMidLine(false, false);
pl.computeObjectDimension(false);
```
**214 rectangular plates were converted to chamfered ribs this way with zero holes lost.**

⚠️ `RectangleMode` stays `1` after a contour swap — it is not a shape test. Count **unique** vertices
(a closed rectangle = 5 points, 4 unique).

Creating a plate from scratch, axis-preserving:
```csharp
PsMatrix m = new PsMatrix();
m.SetCoordinateSystem(origin, xAxis, yAxis, zAxis);
PsCreatePlate cp = new PsCreatePlate();
cp.SetToDefaults(); cp.SetInsertMatrix(m);
cp.SetAsRectangularPlate(L, W); cp.SetThickness(T);
cp.Create();
```
Never derive the thickness axis by sorting bbox dimensions — that produced 96 plates rotated 90°.
`AppendEdgePoint` + `Create()` for a free contour works for 4 points but **failed for 5** in testing;
the reliable route to a shaped plate is *rectangle then `SetPolygon`*.

## 4. Connections — read

```csharp
PsEditLogicalLink ed = new PsEditLogicalLink();
ed.SetObjectId(partId);
int n = ed.get_LogicalLinkCount();
PsLogicalLink lk = ed.GetLogicalLinkByNumber(ed.get_LinkNumberFromIndex(i));
// lk.Type, lk.Name, lk.Ident, lk.LinkObjectCount, lk.BoltObjectCount
// lk.getLinkObjectId(k), lk.getBoltObjectId(k)
PsBaseplateLinkDataMgd bp = lk.GetBasePlateLinkData();     // the full recipe
PsStiffenerLinkDataMgd  rb = lk.GetStiffenerLinkData();
PsSpliceJointLinkDataMgd sp = lk.GetSpliceJointLinkData();
```
**Joint type numbers seen in real models:** 0 Unknown · 1/36 Cut Position · 2 Cut through ·
**10 Brace Plate** · **11 Endplate connection** · 12 Conn. Shape · **13 Baseplate Connection**.

`RemoveAllLogicalLinks(deleteParts)` removes a joint **together with the steel it generated** — the
correct way to rebuild with corrected parameters instead of orphaning a plate.

## 5. Connections — create

```csharp
PsBasePlateConnection bp = new PsBasePlateConnection();
bp.SetToDefaults();
bp.SetConnectionObjectId(columnId);
PsBaseplateLinkDataMgd d = new PsBaseplateLinkDataMgd();  // or bp.GetTemplate(name)
d.Length = 400; d.Width = 400; d.Thickness = 20;
d.HoleDiameter = 23; d.HoleDistanceHorizontal = 300; d.HoleDistanceVertical = 300;
d.AnchorBolts = true;
d.AnchorBoltDiameter = 20;      // defaults to 0 -> invisible anchors!
d.AnchorBoltGripDiameter = 20;
d.AnchorBoltGripLength = 400;   // graphic length
d.AnchorBoltKeySize = 30;       // SW30 -> the NUT appears (M20)
d.CreateDetailedAnchorBolts = true;
d.ShortenShape = true;          // shortens the column by the plate thickness
d.CreateGroup = true;
bp.SetConnectionData(d);
bp.Check();
bp.Create();                    // drills its own holes
```
Same pattern for `PsStiffenerConnection` (+ `SetConnectionPoint`), `PsSpliceJointConnection`
(+ `SetSupportObjectId`), `PsShearPlateConnection`, `PsWebAngleConnection`.

**Templates configured in the installation** (`GetTemplateCount` / `GetTemplateName` / `GetTemplate`):
- base plate: `AutoConnect Metric v 18/450x450x25`, `600x600x25`, `default/Standard 200x200x10`
- **stiffener (rib): 7 templates** — `half/full chamfered` (ShapeType 0), `half/full convex` (1),
  `half/full rounded` (2), `Standard`
- splice: `default/example2`, `default/Standard`

## 6. Element properties

`PsObjectProperties` is the PS Properties dialog as an object: `MirrorFlag`, `YMirrorFlag`, `Mirrored`,
`InsertMatrix`, `XAxis/YAxis/ZAxis`, `Origin`, `MidLineStart/End`, `InsertX/Y`, `MaterialIndex`,
`Posnum`. It must be loaded via a method taking the object id — enumerate its methods to find the
loader rather than assuming a name.

---

## Measured dead ends — do not retry

| Attempt | Result | Evidence |
|---|---|---|
| `CreateSingleBolt` + `AddObject(host)` to drill | **Never drills** | plate + bolt, 2 hosts → holes read back = **0** |
| `PsShape.MirrorFlag = …` | read-only per compiler | — |
| `SetShapeMirror()` | no-op | flag unchanged on read-back |
| `YMirrorFlag = true` | sets a **different** flag | diag `s0→M0Y0; s1→M0Y1` |
| bbox to detect mirroring | impossible | an equal angle's bbox is identical either way |
| `Entity.Ecs` for plates/bolts | returns **identity** | 314 plates + 304 bolts, all identity |
| `PsShape.InsertPoint` | **null** | 350 shapes |
| `COGPoint` / `WeightCenter` | **null** | 0/352 readable |
| reflection over live `Ks_*` objects | **crashes AutoCAD** | "Error Aborting" |
| `AppendEdgePoint` with 5 points | `Create()` returns false | 8/8 attempts |

**Mirroring is only reproducible natively** — `PS_COPY` in mirror mode, `_MIRROR3D`, or
`Database.DeepCloneObjects` (which preserves everything unwritable: mirror, insert offsets, layers,
holes, groups).

---

## Host-environment traps

- Plugin DLLs **lock while loaded** → every rebuild needs a new filename + command + class name.
- Compile with `csc.exe` **from PowerShell** (Git Bash mangles `/r:` flags). C# 5 only.
- Dump files are written **with a BOM** → read as `utf-8-sig`, or the first row of every file vanishes.
- COM: `Documents.Item(i).Save()` **fails**; `app.ActiveDocument.Save()` works.
  `Documents.Item(i).Close(false)` also fails silently — close via `ActiveDocument`.
- COM `Activate()` changes the logical active document but **not the front MDI window** — the modeller
  can be looking at a different drawing while you work.
- `ActiveDocument` can raise `AttributeError` while AutoCAD is momentarily busy — re-acquire and retry.
- `SendCommand` **blocks** when AutoCAD is not quiescent; guard on `GetAcadState().IsQuiescent`.
- Prefix every send with **ESC-ESC**, or a half-entered command is left on the modeller's command line.
- Screen capture is useless when AutoCAD is in the background, and `SetForegroundWindow` /
  `AppActivate` cannot raise it from a background process. `PNGOUT` did not write a file either —
  visual proof from a background session remains unsolved.
