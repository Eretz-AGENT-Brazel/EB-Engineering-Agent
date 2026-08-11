# The plugin's op set — what exists and what it is verified to do

> ⭐⭐ **The canonical build is whatever `app/eb_api.py` declares — never a number written here.**
> Read `DLL` and `RUN_CMD` at the top of that file, and the source is `app/plugin/EBAgentApi<N>.cs`
> for the same `<N>`. This header used to name a specific build; it went **61 versions stale**
> while being the first thing read before every single op. The number is gone on purpose, and
> `python qc/consistency.py` now fails if any document contradicts `eb_api.py`.

*Python client: `app\eb_api.py`, plus `app\eb_shot.py` (screenshots) and `app\eb_log.py`
(the command-line channel). Every "verified" line below was measured by reading the model back —
the op count is not quoted here either, for the same reason.*

> ⚠️ **`_netload()` now PROVES the command is live** before returning, by pinging until it
> answers. It used to sleep one second and return `None`; on 06/08 the DLL loaded a moment
> late and the next three ops came back `EB_TIMEOUT` with nothing pointing at the cause.
> The same rule this file preaches, applied to the bridge itself.

> ## 💾 SAVE. The drawing on disk was SEVEN HOURS stale.
> 06/08/2026 — Amir: *"ממליץ לך לשמור את המודל, שאם המחשב ייכבה או התוכנה תקרוס שתהיה לך
> שמירה."* The file had last been written at **10:16**; it was then **17:21**. Every op since
> had gone into memory only. **A modelling session that cannot be recovered is a demonstration,
> not work.**
>
> `eb_api.save()` — saves, then **proves it by the file's mtime and size on disk** (not by
> `Save()`'s return), and keeps a timestamped `*-backup-YYYYMMDD-HHMM.dwg` beside it.
> ⇒ **Call it after every chapter, and before anything experimental.**
>
> ⚠️ And when AutoCAD is mid-command, COM does not fail cleanly — `doc.Name` and
> `doc.SendCommand` raise `<unknown>.<member>` while `app` still answers, and
> `app.ActiveDocument` raises **`Call was rejected by callee`**. That is not a broken
> dispatch: `IsQuiescent` is telling the truth. **ESC in the application clears it** — a
> keystroke, not a COM call, because COM is the thing that is blocked.
>
> ### 🩸 SAVE ONLY OVER COM. The plugin-side save DESTROYED the live drawing.
> Trying to make saving robust by moving it **into** the plugin did the opposite:
> `Database.SaveAs(sameName)` raises `eFileSharingViolation`, and `Editor.Command("_QSAVE")`
> wrote an **11 KB DWG while the document held 256 entities** — and that empty file replaced
> the good one. `SaveAs` to a *different* path produced the same 10 KB. Only
> **`doc.Save()` over COM** has ever produced a correct file (325 KB).
>
> ⇒ `eb_api.save()` uses COM, and **refuses any save that shrinks the drawing by more than
> half** — reporting it as a corrupt write and declining to take a backup of it. Without that
> guard, the corrupt write silently became the backup too.
>
> ⇒ **Recovery that worked:** the model was still intact *in memory* (256 entities) but could
> not be written by any route — a damaged database. Kill AutoCAD, keep the broken file for
> forensics rather than deleting it, restore the last good `*-backup-*.dwg`, reopen, and
> **verify by reading the model back** (228 entities, 219 position numbers, groups intact).
> Only the throwaway test objects made after the last good save were lost.
>
> ⇒ **And the reason there was anything to restore:** Amir said *"save the model, in case the
> machine dies"* half an hour earlier. The sandbox is not the deliverable — but an unsaved
> session is not a session.

> ### 🧭 The five rules this file exists to encode
> 1. **No return value means success.** `readFrom` → 0 is OK · `Apply()` → 1 is OK ·
>    `Create()` → **false** on a group that exists · `CreateFastener*` → the Int64 **is** the id,
>    0 is refusal. Same DLL, four conventions. **Read the model back, in a fresh object.**
> 2. **When the dump and the compiler disagree, the compiler wins.** The dump hides index
>    parameters on properties (`get_Entry(short)`, `get_Facet(int)`, `get_Groupname(long)`,
>    `get_EdgePoint(PositionSelection, PositionSelection)`) and renders `ref` as `Type&`.
> 3. **Enum values must be measured, never inferred from declaration order.** `FacetType`'s
>    usable values are 1–3 with 0 rejected; `EdgeLayout`'s are 0–6 sequential. Same file.
> 4. **ProSteel diagnoses itself on the command line** — a channel the API never returns.
>    `eb_log.mark()` / `problems()` around anything experimental.
> 5. **Announce before modelling, and point the camera.** Proving it to yourself and showing
>    it to Amir are two different obligations.

> **The DLL locks once NETLOADed**, so every rebuild needs a new filename + command + class.
> That is the 40-plus-version treadmill; it is a known tax, not an accident.

> **⚡ Unknown parameters are REFUSED (v48).** Each op declares the keys it reads; anything
> else returns `EB_ERR unknown parameter(s): … -- op=X accepts: …` and executes nothing.
> The table is **generated from the source** — regenerate it after adding or changing an op,
> or it will start lying. `op`/`dwg`/`reqid` are global and always allowed.

---

## Ops built on 06/08 — second block (v45 → v48)

| op | what it does | verified |
|---|---|---|
| **`boltinfo`** | B.15 — the bolt style/type tables via **`get_BoltStyleName(int)`**, the indexer the dump hides | ✅ 27 styles, 27 types |
| **`boltsingle`** | B.15.1 manual insertion **by grip length**: `from`/`to`/`dia`/`style`/`addlen` | ✅ 40 mm grip → a 65 mm bolt |
| **`nutonly`** | B.15.1 — a nut/washer with **no bolt**: `from`/`to`/`dia`/`style` | ✅ |
| **`threadedrod`** | B.15.3 — `from`/`to`/`dia`/**`offset`**/`style`. Offset projects at **both** ends | ✅ 600 + 2×30 = 660 |
| **`bracing`** | **B.24 `PS_VERBAND`.** `p1`/`p2` the system line · **`host1`/`host2`** the two shapes (`setBorderObjects`) · `type=NormBracing\|RodBracing\|PipeBracing` · `layout=` the manual's *Shape Position* · `cat`/`size`/`shapetype` · `platethick`/`platewide`/`plateside` · `cross` `sym` `welded` `nogussets` `group` `dynamic` `centerhole` `divideall` · `nprof`/`ncross`/`holeedge`/`holehole`/`holecross`/`dm`/`play`. Prints a **read-back before `insert()`** so a config failure is told apart from a creation failure | ⛔ **`insert()` false** in 5 configurations; setters verified working |
| **`weldstyles`** | Lists `PsCreateWeldFlag`'s style table. ⚠️ returns **0** while the object-style probe reports **4** weld styles | ⚠️ reads empty |
| **`weld`** | `from=`/`to=` the seam line · `style` `thick` `sign` `len` `roundabout` `onsite` `row` · **`makeweld=1`** asks for the weld and not only the flag | ⛔ **`Create()` returns false** in every variant tried — welds appear only as a by-product of a connection class (B.21 splice → 32 `Ks_WeldFlag`) |
| **`splicetemplates`** | B.21 — the 2 shipped splice templates (`default/Standard` 4 plates, `default/example2` all six positions) | ✅ |
| **`splice`** | **B.21 `PS_LASCHE`** — splices two **collinear** members. `handle=`+`support=` the two shapes · the six positions `topout`/`topin`/`downout`/`downin`/`webleft`/`webright` · `tflange`/`tweb` · `nflangev`/`nflangeh`/`nwebv`/`nwebh` · `gap` `dia` `workloose` `offflange` `offweb` · **`weldflange`/`weldweb`** (⭐ 0 bolts, 32 `Ks_WeldFlag` instead) · `welddiagonal` · `toplap`/`sidelap` | ✅ 4 variants; **6 checkboxes → 8 plates** |
| **`shearplatetemplates`** | B.20 — 3 templates + the load database + a **cope-template probe**. Found the convention: **`CheckCopeTemplate('default/Standard') = True`**, everything else False | ✅ |
| **`shearplate`** | **B.20 `PS_SCHEARPLATE`** — web plate instead of angles. Same arguments as `webangle` plus `thick` · **`poly=1`** (⚠️ product becomes **`Ks_Plate`** instead of `Ks_Shape`) · `normaltocut` · `cutconn`/`cutsup` · `nvert`/`nhoriz` · `holevert`/`holevertedge`/`holehoriz`/`holehorizin`/`holehorizout` · `slot` · `eachplate`. Validates the cope template before applying | ✅ 4 variants; **1 plate + nVert×nHoriz bolts**; beam `cutPlanes` 0→1 |
| **`webangletemplates`** | B.19 — the 3 shipped web-angle templates plus the DAST database. ⚠️ **`plateDataCount = 0`** on this installation: the load-based selection has no data behind it | ✅ |
| **`webangle`** | **B.19 `PS_STEGW`** — one call = the whole detail. `handle=` beam · **`support=`** column (**required**; omit it and `Create()` fails) · `at=` · `template=` · `key`+`catalog` (the angle section) · `pos` `turn` `nvert` `nconn` `nsup` `dia` `workloose` **`boltstyle`** (a plain string here) · `slotconn`/`slotsup` (turn holes into **slots**) · `flat=1`+`thick`/`longleg`/`shortleg`/`bendradius` (⚠️ produces **`Ks_BendShape`**) · `gap` `sideoff` `vertoff` `fromedge`/`fromdown`/`fromhole` · cope fields (⛔ **inert — no cope is produced**) · `group`/`boltsingroup`/`eachangle` · `shear`/`moment` (DAST, untestable while the DB is empty) | ✅ 4 bays; beam `holeFields` 0→1, `polyCuts` 0→1 |
| **`stifftemplates`** | B.16 — dumps all **7** shipped stiffener templates with every field. ⚠️ The **names** are reliable; the exposed `LengthType` is not | ✅ 7 templates, all built and measured |
| **`stiffener`** | **B.16 `PS_RIP`.** `handle=` the girder · `at=` the insertion centre · **`template=`** (required — creation fails without one) · `shapetype` (0 chamfered · 1 convex · 2 rounded) · `lengthtype` (0 Full · 1 Half · 2 By Length · 3 Square) · `thick` `flangedist` `webdist` `offset` (negative ⇒ projects out) `roundto` (truncates) `radius` (**0 = import the shape radius**) · `centerpunch` (1 = weld-mark holes) · `withangle`+`angle` · `creategroup`. Reports **every** new handle — one call makes **two** ribs | ✅ 33 pairs, formula verified to the mm |
| **`plate9`** | **B.9 Insert Plates, the whole dialog.** `mode=rect\|poly\|radial\|diagonal\|edges\|fromshape` · `at=` + `ex/ey/ez` (the insertion plane) · `t=` · `xpos/ypos` (`PositionSelection`) · **`vpos=kDown\|kMiddle\|kTop`** (the manual's `Insert Edge`) · `insheight` · `xoff/yoff` · `grid=1` + `griddir` · `name/material/article/layer/family/display/area/descr/style` · `check=1` runs `checkValidPlate()` first | ✅ 9 plates built and read back |
| **`arcplate`** | B.9.2 flat bent plate — `p1/p2/center/normal/w/t`, **`bigarc=1`** is the manual's ALT key (> 180°) | ✅ 90° and 270° measured |
| **`bend`** | B.9.4 `ADD SEGMENT` — `handle=` `at=` (the click that picks the reference edge) `len/front/rear/radius/angle`, `convert=1` on the first call only. **Returns the NEW handle** because the conversion replaces the entity | ✅ the manual's 3-segment example rebuilt |
| **`bendinfo`** | Reads the segment tree: per-flange length, angle (**both deg and rad**), radius, offsets, `vtx=` the base edge, `parent=`, grip points and vertices. `max=` scans **past** `FlangeCount`, which under-reports | ✅ parent[2]=0 confirmed |
| **`bendtwo`** | B.9.4 *Combine Plates* — `h1/h2/radius/at/inner/k/delete2`. `k` is `Correction Value Unwinding` (0 inner · 1 centre · 2 outer). ⚠️ **both source plates are erased** | ✅ 2 in → 1 bent plate |
| **`plateinfo`** | Read-back, **one named call at a time**: `probe=safe` (thickness · insertHeight · insertXY · WCS extents) or a comma list. ⛔ **never `probe=weight`** — `computeObjectWeigth` kills AutoCAD | ✅ / ⛔ crash reproduced twice |
| **`chamfer`** | Chamfer a plate corner by **three parameters**, the manual's way — `at` picks the corner, `d1`/`d2` are the two edge lengths, `type` the shape. `list=1` reports existing facets | ✅ measured on 5 identical ribs; see the FacetType table below |
| **`zoom`** | `handle=…` (one or many) or `all=1`, native `SetCurrentView` with a correct WCS→DCS transform so it is right in isometric too. ⚠️ **`margin` is a FRACTION, not millimetres** — the view is scaled by `1 + margin` (defaults 0.25 for a handle, 0.05 for all). `margin=900` asks for a view **901× the model** and looks exactly like a broken zoom: on a 51 m model it reported `w=46836827` and drew an empty screen | ✅ centred on 12000 as asked |
| **`view`** | `dir=iso\|sw\|se\|ne\|nw\|top\|bottom\|front\|back\|left\|right` | ✅ |
| **`hilite`** | Select parts so grips mark the spot; `clear=1` | ✅ |

### `chamfer` — creator vs reader, the mistake worth remembering

The first implementation used `PsEditModification.set_Facet(0, ch)`. It threw nothing, returned
nothing, and created nothing: `facets 0→0`. **`PsEditModification` READS and DELETES
modifications; it does not create them.** Creation goes through **`PsCutObjects`**, which owns
**all ten cut types**:

```csharp
PsVertexChamfer ch = new PsVertexChamfer();
ch.SetType((FacetType)1);        // 1 = straight chamfer -- see the table
ch.SetDistance1(d1);             // manual B.13.2 "Radius / 1st Edge"
ch.SetDistance2(d2);             // manual B.13.2 "2nd Edge"
ch.SetEdgePointId(oid, corner);  // pick the corner by a point on it
PsCutObjects cut = new PsCutObjects();
cut.SetToDefaults();
cut.SetObjectId(oid);
cut.SetAsFacetCut(ch);           // <- the creator
int rc = cut.Apply();            // rc = 1
```

**FacetType — measured, not assumed.** The reflected dump lists enum *names* with **no values**;
assuming `0..4` from the order was wrong. Five identical 120×120×10 ribs, `d1=80 d2=40`, then
read back with `chamfer list=1`:

| sent | stored | shape measured |
|---|---|---|
| 0 | **1** | rejected → falls back to the straight chamfer |
| **1** | 1 | **straight diagonal chamfer** = `kFacetTriangle` — *this is Amir's rib* |
| **2** | 2 | convex arc (rounded corner) = `kFacetArc` |
| **3** | 3 | concave arc = `kFacetInversArc` |
| 4 | **1** | rejected → falls back |

⇒ valid values are **1, 2, 3**; `kFacetUndefined = -1` and `kFacetRectangle = 0` are not
usable for a facet cut. Also measured: **repeat calls STACK** — a second call on the same
corner gave `facets 1→2` rather than replacing.

⚠️ **`contourVerts` does not change** (5→5). A modification is a separate layer from the
plate's base contour, so `GetPolygon` is the **wrong instrument** for "did the cut happen" —
use the facet count and the picture.

⚠️ **Reported weight is GROSS.** All five ribs reported **1.13 kg** = 120×120×10 uncut. The
manual's Global Settings has *"Volume Weight — the weight of the plates is determined by the
volume"*, and `PsObjectProperties.VolumeWeightFlag` is the per-object switch. Amir's ruling
(06/08): *"כשאני בודק באופן ידני משקל ראשוני לחומר אני לא מתחשב בסטיות כאלו — זה הפרשים
מינוריים, תלוי בסדר גודל של הפרויקט."* ⇒ gross is fine for preliminary take-off; the flag is
there if net is ever needed.

## 🔓 POSITIONING — the biggest blocker, opened 06/08/2026 (v49 → v51)

The **engine** really is locked behind the modal `PS_POS` dialog, and that is now settled, not
assumed: `PsCreatePositioning` has ~90 `Set*` configurators and ~60 `Internal*` step methods
but **no** public `Perform/Run/Execute/Apply/Create` and **no** `SetToDefaults`/`Initialize`.
There is no dash-prefixed or scripted variant — the manual's command reference lists exactly
one token. ⚠️ Two `Internal*` members (`InternalPositioningOptions`, `InternalDisplay*Result`)
**open dialogs** — never call them from code.

**But the three primitives the engine is built from are exposed separately**, so the pass can be
built out of ProSteel's own parts:

| primitive | signature | role |
|---|---|---|
| write the number | `PsObjectProperties.Posnum` / `.Sendnum` + `writeTo(Int64)` | ✅ measured |
| **decide what is identical** | `PsCompareDrawing.CheckTwoPartsAreEqual(Int64, Int64)` | ✅ measured |
| number format | `PsCreatePositioning.ConvertNum2Posnum` / `GetNextPosnum` | available |

| op | what it does | verified |
|---|---|---|
| **`posset`** | Write `Posnum`/`Sendnum` on one part, dialog-free | ✅ `withPosnum 0 → 5`, names preserved |
| **`equal`** | Both equality tests side by side, so the wrong one can never be picked by accident | ✅ |
| **`posauto`** | The whole pass: enumerate → cluster by equality → number → verify each. `dry=1` clusters without writing | ✅ 21 parts → 15 distinct, 175 comparisons, **2.6 s** |

### ⚠️ `IsEqualTo` DOES NOT SEE MODIFICATIONS — use `CheckTwoPartsAreEqual`

Measured on five ribs identical except for their corner cut:

| pair | `CheckTwoPartsAreEqual` | `IsEqualTo` |
|---|---|---|
| two identical straight chamfers | EQUAL ✅ | EQUAL ✅ |
| straight vs **arc** | **different** ✅ | EQUAL ❌ |
| straight vs **inverse arc** | **different** ✅ | EQUAL ❌ |
| **uncut** plate vs cut plate | **different** ✅ | EQUAL ❌ |
| plate vs HE300B column | different ✅ | different ✅ |

`IsEqualTo` compares the **nominal property block** — same root cause as the gross weight: to it
every one of those plates is `PLATE 120x120x10, 1.13 kg`. Using it would put three genuinely
different plates on **one position number**, and the shop would get one cutting drawing for
three different parts. **That is precisely the kindergarten-stairs failure, manufactured
automatically.** ⇒ For anything that decides *"is this the same part?"*, only
`CheckTwoPartsAreEqual`.

It is genuinely **geometric**, twice confirmed:
- a plate with **two** identical chamfers on the same corner = a plate with **one** → EQUAL
  (same material removed — correct);
- two `RR 200x200x8` differing only in their drilling → **different**, and they were numbered
  separately, which is what fabrication requires.

**Cost:** O(n × clusters), not O(n²) — each part is compared to one representative per cluster.

### B.29 completed (v75 → v76): Sendnum, groups, flags, and a 12× speed-up

| added | detail |
|---|---|
| `posauto field=send` | writes **`Sendnum`** instead of `Posnum` |
| `DontPositionFlag` | a part flagged *do not position* is **skipped and counted**, not numbered anyway |
| **`groupauto`** | numbers the **groups**, in a second pass |

**Group equality is a DIFFERENT rule from part equality** — manual B.29.1 *"Group Detection"*:
*"single parts are only compared using their position number because positioning has already
been carried out before."* ⇒ two groups match when their members' **position numbers** match;
geometry is not re-examined. That is why singles must be numbered first, and why `groupauto`
cannot reuse `CheckTwoPartsAreEqual`. It builds a signature from the sorted member posnums and
**warns loudly** when any member is still unnumbered.

**Measured** — four details, three identical and one with a 20 mm rib instead of 12:

```
3CA  P57+P74+P94  G9      3D6  P57+P74+P94  G9
3D0  P57+P74+P94  G9      3DC  P57+P74+P95  G10
```

Three share a group number; the odd one gets its own, because its rib got a different part
number. Exactly the manual's rule.

### ⚡ BUCKET BEFORE YOU COMPARE — 108.8 s → 8.7 s

The first `posauto` compared every part against one representative per cluster:
**217 parts, 95 clusters → 10,030 geometric comparisons, 108.8 seconds.** At Amir's real scale
(~2,000 objects) that is minutes — and the whole point of the op is to beat a person.

**Two parts with different nominal dimensions can never be geometrically equal**, so bucket by a
cheap signature first (class + name + L/W/H + weight) and run `CheckTwoPartsAreEqual` only
*within* a bucket:

| | before | after |
|---|---|---|
| comparisons | 10,030 | **1,333** |
| time | 108.8 s | **8.7 s** |
| **distinct parts** | **95** | **95** — unchanged, which is the proof |

⚠️ This does **not** reintroduce the `IsEqualTo` trap: two plates differing only by a cut share a
nominal signature, land in the **same** bucket, and are still separated by the geometric test.
Bucketing can only ever skip comparisons that were guaranteed to return false.
⇒ **An optimisation is only correct if the answer is bit-for-bit the same. Print both.**

### 🔴 There is NO consistent success convention — read the model back

`posset` first refused to write anything because it treated `readFrom`'s `0` as failure.
`0` is `eOk`. Meanwhile `PsCutObjects.Apply()` returns **1** on success. Same DLL, opposite
conventions — and `Create()` returns `false` on a group that exists.
⇒ **Never infer success from a return value. `posset`/`posauto` verify by constructing a BRAND
NEW property block and re-reading** — the block you wrote from still holds your value in memory
and will happily confirm a write that never landed.

⚠️ **`readFrom(id)` before every write, always.** `PsObjectProperties` is a *detached* property
block, not a live handle: writing a fresh one pushes a blank `Name`/`Article`/`Style` over the
object. Measured: with `readFrom` first, `PLATE 120x120x10` and `HE 300 B` survived intact.

## 📋 `props` — dead for weeks, and it was hiding the richest class in the API (v57)

It searched by **reflection** for a loader called `loadFrom`/`getFrom`/`SetObjectId`/`load`.
The method is **`readFrom(Int64)`**. Every call had returned `EB_ERR ... : no-loader`.
Written before the API was mapped — and left guessing long after guessing became unnecessary.
⇒ **When a stub predates the knowledge, delete the stub, don't extend it.**

**`PsObjectProperties` carries 100+ properties; this agent was using five.** What it gives:

| | |
|---|---|
| **`Origin` `XAxis` `YAxis` `ZAxis` `InsertMatrix`** | the **part coordinate system** — the frame the manual's Clone warning is about. This is what made rotated/mirrored cloning possible |
| **`PaintArea`** | painting surface in **m²** — verified: HE300B×4 m → **6.885**, a 200×200 plate → **0.09**. A real quotation quantity |
| **`CutArea`** | cut-face area in **cm²** — verified: HE300B → **149**, exactly the catalogue section area |
| `Length` `Wide` `Height` `Diameter` | part dimensions with no geometry maths |
| `Mirrored` `MirrorFlag` `YMirrorFlag` | mirror state |
| `KlemmLen` | grip length | 
| `Key` `Katalog` `Material` `Article` | profile identity |
| `MidLineStart/End` `GetExtents(ref,ref)` | axis and bounding box |
| `ProcessStatus` `DontPositionFlag` `DontDetailFlag` `PartListFlag` `BoltListFlag` | the manual's per-part switches |

⚠️ In the dump, **`Type&` means a `ref` parameter** — `GetExtents(PsPoint& , PsPoint&)` is
`GetExtents(ref …, ref …)`. Reading it as pass-by-object costs a compile cycle.

## 🔁 CLONE THE DRILLING — `clonedrills` (v52 → v58)

Manual B.4.5 *"Clone"*: **"A prerequisite for cloning is that the parts have a position number
and that these match … only parts with the same position number as the original part will be
considered."** That prerequisite is why this could not exist before `posauto`.

**The manual lists five transferable kinds — Cuts, Drill Holes, PolyCut, Notches, Boolean.
Exactly ONE is exposed in the API, and it does not work from code.**

### 🛑 `PsDrillObject.TakeoverDrills(PsSelection, PsSelection)` — **RETRACTED 10/08/2026: IT WORKS**

This section used to be headed *"five sequences, all dead"*, carried a table of `changed=0`, and
ended *"Clone is a dialog-only feature. Recorded so this is never re-investigated from scratch."*
**All of that is withdrawn.** Measured on three identical HE300B beams:

```
source  135E   hole  y 150 -> -150  at z=1500, dia 22
variant 1  ->  135F   hole y 1050 -> 1350   z=1500  dia 22    changed=1
variant 2  ->  1360   hole y 1450 -> 1750   z=1500  dia 22    changed=1
variant 3  ->  1361   hole y 1850 -> 2150   z=1500  dia 22    changed=1
variants 4, 5 also transfer;  6, 7, 8 have no case and do nothing
```

`variant 1` is `SetToDefaults` + `SetObjectId(src)` + `TakeoverDrills(sSrc, sTgt)` and **nothing
else** — there is no fall-through to the composition route, which is `variant 9`. Each hole sits
at **its own beam's centre**, same z, same diameter: real geometry, read back, not a count.

⚠️ **The posnum "gate" is not a gate either.** With the source numbered `CTRL`, the target
transferred with **no** posnum, with a **different** one, and with a **matching** one — `changed=1`
all three. Why the 06/08 run returned zero is **unknown and left unknown**.

⇒ ⭐ **The lesson, which outlives the capability:** *"selections proven correct"* proved the
selection **sets** were valid and nothing about the call's preconditions. **A closed-do-not-retry
verdict is only as good as the preconditions the test honoured**, and a wrong one is the most
expensive kind because its whole purpose is to stop anyone looking again.

⚠️ Still true, and it matters: the transfer is **relative to each part's own coordinate system**.
The source hole ran −Y and the targets' ran +Y. Immaterial for a through-hole; **it matters for a
countersink or a slot**, and a mirrored target receives a mirrored modification.

### ✅ What works: compose two things that already do

Read the source holes (`PsSingleHoleArray.getHole` → start, end, **hole** diameter), translate by
the extents delta, and create each one on the target (`SetInsertPoint` + `SetNormal` +
`SetHoleWorkloose(0)` + `SetSingleHoleField(dia)` + `Apply()`).
`Workloose(0)` because `getHole` returns the **hole** diameter — adding a bolt clearance on top
would grow the hole on every clone.

**Measured:** drill one of four → `changed=3 nowMatchSrc=3`, independent re-count `4,4,4,4`, and
`CheckTwoPartsAreEqual` says **EQUAL again** — precisely what the manual says Clone achieves:
*"all components are identical again after the transfer has been concluded."*

**v58: it now goes through the PART COORDINATE SYSTEM**, which is what the manual says the real
Clone does — `PsObjectProperties.Origin` / `XAxis` / `YAxis` / `ZAxis` give exactly that frame.
So **rotated and mirrored copies work** instead of being refused, and mirrored ribs are
everywhere in real steel. Equality is checked on the part's own `Length`/`Wide`/`Height`
(rotation-invariant), not on world extents.

**Measured** — one source drilled with a deliberately asymmetric 3-hole row, cloned onto a
90°-rotated, a 180°-rotated and an X-mirrored twin. Every hole landed where hand-calculation
said it would, and all 12 spans came back **exactly 12.0 mm = the plate thickness**, so every
hole passes fully through. Diameter stayed **18** on every clone — proof that
`SetHoleWorkloose(0)` is required, otherwise each generation would add clearance again.
*(The reported start/end face can flip between source and clone — the drill direction differs.
Irrelevant: check the SPAN, not which face is listed first.)*

⚠️ **It still REFUSES rather than guessing** when the target is a different size:
`SIZE-DIFFERS(...)-refused`. **Proven with a deliberate impostor** — a 260-long plate given the
same position number as eight 200-long ones was refused and left undrilled while the other
seven got 4 holes each. A mislabelled part must never be silently drilled wrong.

## 🔩 B.12 — BOOLEANS, DETAIL CUTS, DIVIDE / COMBINE (v70 → v71)

### `boolean` — the only way to do a boolean on steel

⚠️ Manual B.12.7 (p.225): ProSteel does **not** use the AutoCAD ACIS modeller, so AutoCAD's own
**UNION / SUBTRACT / INTERSECT silently do nothing** on ProSteel objects — *"there will be no
errors, but nothing will happen!"*

`SetAsBooleanCut` takes an **`Int64` — the id of an EXISTING solid** (the manual's
*"discharge-solid"*), not a constructed body. `SetSubBodyType` picks the operation:
`kSubBody` subtract · `kAddBody` add · `kCommenBody` keep the common volume.

**Measured** — a 400×400×20 plate with a second plate as the tool:

| mode | evidence |
|---|---|
| `sub` | with the tool crossing the full width, the extents' x-max dropped **13200 → 13050**, exactly the tool's near face |
| `add` | extents **grew to 13250**, past the plate's own edge, absorbing the tool |
| `common` | `L/W` collapsed to **150 × 200** — precisely the overlap region |

⚠️ **Weight does not move** (25.12 → 25.12 in all three) — the same gross-weight behaviour as
everywhere else. The witnesses are `subBodies 0→1` plus the **extents**.

### `detailcut` — and the reader that was blind to it

A detail cut removes **no material**: it plants a 2D section marker for detailing (manual p.571).
It reported as a failure until the instrument was fixed: **`PsEditModification` cannot see detail
cuts at all** — they are counted by **`PsEditShapeModification.DetailCutCount`**.
Measured: `detailCuts 0→1`. ⇒ **The op had worked all along; the instrument was wrong.**

### `shapeedit` — B.12.1 Divide / Combine, an entire section that was missed

These live on `PsEditShapeModification`, **not** on `PsCutObjects`, which is why sweeping the ten
cut types never found them.

| what | measured |
|---|---|
| `split=x,y,z` | IPE300 **3000 → 1500**, census **+1**, and it returns the new member's handle |
| `connect=<other>` | two collinear 1500 members → **3000**, census **−1** |
| `side=<n> len=<mm>` | trims/extends at one end — see below |
| `lengthat=<point> len=<mm>` | trims/extends the end **nearest that point** (a point at the left end with −800 moved the left end in by exactly 800) |

⚠️ **`ChangeLengthAtPoint/AtSide`'s `Length` is a DELTA, not a target length.** `len=2000` on a
3000 member gave **5000**, not 2000. Negative shortens.

⚠️ **`ObjectSide` measured** (−600 on a 3000 member, watching both ends):

| value | start moved | end moved | meaning |
|---|---|---|---|
| **0** | +600 | 0 | the **start** end |
| **1** | +300 | −300 | **both ends equally** |
| **2** | 0 | −600 | the **end** end |

A parameter called *Side* whose value `1` means *both sides* is not guessable — measure it.

### ❌ `SetAsRoundedCutId` — unreachable, and the manual never documents it

No change and **no complaint** on a plate, an IPE300, or an `RO 219.1×6.3` tube — while
`object` cut the same tube cleanly (2000 → 1102.72, a saddle cut). The manual has no "rounded
cut" modification anywhere; *"Straight Cut"* appears only inside the **handrail/guardrail macro**
(Leave / Straight Cut / Complex Cut / With Rod / Boolean / Drill). ⇒ `SetAsStraightCutId` and
`SetAsRoundedCutId` look like internal helpers of those macros; the first happens to work
generally, the second does not.

*(Aside: `dumpcat` writes **`eb_cat.txt`**, not `eb_catalog.txt` — a reading error made a working
op look broken for four catalogues. `DIN RUNDROHR` holds 232 round sections, e.g. `RO 219.1x8`.)*

## 📦 B.28 GROUPS — the chapter, closed (v68 → v69)

Groups encode **fabrication intent**, not drawing convenience:
**subgroup** = stock parts · **component part group** = ships as one piece · **assembly** =
combined on site. Only *creation* had been implemented; everything below was missing.

| op | what it gives |
|---|---|
| **`groupinfo`** | members · sub-parts · main part (`getMainPart` / `getMainPartOf` / `getTopMainPartOf` / `getAssemblyMainPartOf`) · **weight with and without bolts** · dimensions · centre of gravity · the group's own property block (posnum, sendnum, paint area, kind flags) |
| **`groupedit`** | `add=` · `remove=` · `name=` · `pos=` · `send=` · `delete=1` |

**Measured on a real detail** — HE300B 3 m + 500×500×20 base plate + two ribs:

```
parts=4 subParts=3 isMain=False main=<column> | members: 34E 34F 350 351
wt=397.786 wtNoBolts=397.786  dim=3000x500x500  cog=13000,41000,1326.836
groupProps  paint=5.909 m²  count=1/1  sub=False assembly=False weld=False
```

`groupedit` verified: rename ✅ · posnum ✅ · add **4→5** ✅ · remove **5→4** ✅ ·
delete **3→0 with all three parts still alive** ✅ *(a group is dissolved, the steel is not)*.

### Three things measured that the names do not tell you

1. **`Groupname` is an internal id, not the display name.** `get_Groupname(oid)` returns the
   main part's handle for a group, **`Sx…` for a subgroup and `Ax…` for an assembly**. The
   editable name is `PsGroupProperties.Name` (+ `NameChangedManually`).
2. **AN ASSEMBLY HAS NO MAIN PART — and passing one silently drops it.** `kind=assembly` with
   `main=<column>` produced an assembly of the two plates only: **26.886 kg**, the column gone
   without a word. Fixed by adding the main part as an ordinary member: **260.886 kg**, all
   four members present, exactly the hand calculation.
3. **An assembly is a real OBJECT** (`Ks_Assembly`, census **+1**); a group and a subgroup are
   metadata on the parts (census unchanged). And `CreateAssembly(Origin, XAxis, YAxis)` puts
   that object at `Origin` — left at 0,0,0 it landed with placeholder geometry
   (centre 100,100,100, extents 0,0,0…200,200,200) nowhere near its members. Default it to the
   members' centre.

### "Model once, place it 40 times" — the feature does not exist; the method does

There is **no** export-a-group-as-a-block, no Favorites, no template folder for 3D. The
manual's *"deposited in a library in the form of a block"* is **DetailCenter**, i.e. 2D
detailing output, not model reuse.

**What works is what Amir said from the start: build it once, then replicate.** Verified — a
grouped detail (column + base plate + rib + a 2×2 ⌀27 field) replicated with `replicate`:
the **copy came out grouped (`parts=3`) and kept its 4 holes**. ⇒ group the detail *before*
copying and the group travels with it.

## 🕳️ B.14.1 — THE DRILL FIELD SYNTAX, READ AND MEASURED

The manual's own definition: **`Number1*Pitch1, IntermediatePitch1, Number2*Pitch2, …`**
`Shape/X Dir` runs along the shape (x of the UCS for plates); `Cross/Y Dir` across it.
*"At a number = 1 you can omit the pitch."*

**The manual's own worked example, reproduced exactly:**

| | |
|---|---|
| `x = 2*60,200,1*,200,3*40` · `y = 2*100` | → **12 holes** = (2+1+3) × 2 ✅ |

**Three rules measured, two of them traps:**

1. ⚠️ **`y` must be `1*` — never omitted.** The dialog lets you leave the crosswise box empty;
   **the API returns ZERO holes, silently.** `x=4*70` with `y` omitted → **0**; with `y=1*` → **4**.
2. **Crosswise-only needs `x=1*`** — the manual says so explicitly, and it measures out: 
   `x=1*` · `y=3*80` → **3 holes**.
3. **`W` means the shape's own marking gauges** (`2*W`). On an **HE300B** → **4 holes**, laid out
   on the profile's gauges. On a **plate**, which has none, it silently yields **one row** —
   the manual says the program *"will prompt you to enter one"*; from the API there is no prompt,
   just a quieter answer. ⇒ **`W` is for shapes only.**

⚠️ **"One single drill hole field cannot cover mixed groups consisting of one and two holes in
crosswise direction"** — the manual's own limit; two fields are needed.

**Other layouts the chapter documents:** `Radial` (Number · Radius · Area · Start — implemented
as `drillspecial kind=radial`) · `Single Holes` (each position individually) ·
and three hole layouts: **Drill Through · Drill Blind Hole · Weld Crack** — the last being *"a
small marking"*, which is `HoleType.kHoleWeldSign = 2`.
`Flange` picks upper / lower / both; `Shape Centre` forces the insertion point perpendicular to
the shape centre so the holes come out symmetrical.

📌 *"Click this button to adopt the drill holes of one component part into another one… drill
holes from a copied connecting plate may be rapidly transferred to a shape."* That is
`TakeoverDrills`. 🛑 **This line used to say it does nothing from code — RETRACTED 10/08/2026, it
transfers.** See the retraction above (`clonedrills variant=1`); `variant=9` composes the same
result by re-drilling and remains a valid fallback.

## 🏛️ B.8 INSERT SHAPES — the foundation, read and implemented (v87 → v91)

*Chapter notes: `EB PROSTEEL AGENT\knowledge\MANUAL-NOTES-B08-insert-shapes.md`.
Worked in its own drawing, `B08-insert-shapes.dwg`.*

### ⭐ The orientation rule — documented all along

A shape is inserted from **two 3D points**, oriented so that *"if you stood at the end point and
looked into the direction of the starting point, the view corresponds to the depiction on the
monitor."* Two points do not fix the rotation, so a third is used:
**when the two points are perpendicular in the WCS, alignment follows the WCS x-axis**; when they
are free in space, the x-axis is made as parallel as possible to the WCS xy-plane.

⇒ That is exactly why a vertical HE300B came out with its web facing where it did, and why an end
plate on the x side drilled through the **web** instead of the flange. `SetXAxis` / `SetYAxis`
are the explicit override. **Measured the hard way before the chapter was read.**

### `shape` — the whole insertion dialog

| dialog field | property | measured |
|---|---|---|
| **insertion point** | `SetXPosition` / `SetYPosition(PositionSelection)` | see the table below |
| Delta X / Y | `SetXOffset` / `SetYOffset` | (at the 'Free' point) |
| **Start / End Offset** | `SetStartOffset` / `SetEndOffset` | ✅ |
| **Turn** | `SetRotation` | ✅ |
| **Length** (overrides the points) | `SetDirection(vector, length)` | ✅ exactly 1234 |
| Horizontal / Vertical Dist. | `SetHorizontal/VerticalDistance` | `SHAPECLASSLAYOUT` spacings |
| Material · Layer · Family · Detail · Display · Area · Article | the matching `Set*` | |
| the 5 shape types | `SelectStandard/Special/RoofWall/CombinationSections` | |

**Insertion point measured** — HE300B, the *same* two points every time, only the point changing:

| setting | where the profile actually sits |
|---|---|
| default / `kCenter` | y −150…150 (centred on the line) |
| **`xpos=kLeft`** | y **0…300** — the line is the left face |
| `xpos=kRight` | y −300…0 |
| **`ypos=kTop`** | z **−300…0** — the line is the **top** face |
| `ypos=kDown` | z 0…300 |

⇒ **This is how you put a beam's top flange on a level line** instead of computing offsets by
hand. It was the single biggest gap in the old `beam` op, which always centred.

**Start / End Offset measured** (two points 2000 apart):
`startoff=300` → length **1700**, starting at 300 · `endoff=-400` → length **2400** —
**a negative offset EXTENDS** · both 200 → **1600**, from 200 to 1800.

**Turn measured**: IPE300 at `rot=0` is 150 wide × 300 deep; at **`rot=90`** it is **300 × 150**;
at 45° the bounding box is 106 × 318 — the rotated envelope.

### ⭐ Non-catalogue sections — `flat=WxT`

B.8.1: *"**Key** … can be entered directly here to be able to create **non-standardised shape
sizes** of tubes, flat steel, round iron."* `PsCreateShape.CreateFlatSteel(Wide, Thick)` is that
route, and it needs **no catalogue entry at all**:

| asked | got |
|---|---|
| `flat=137x9` | `Plate 137x9`, section **137 × 9** |
| `flat=250x12` | **`BRFL 250x12`** — matched to a real catalogue name |
| `flat=83x6` | `Plate 83x6` |

⚠️ **`CreateFlatSteel` is a CREATOR, not a configurator** (it returns `Boolean`). Called before
the insertion points are set it returns `false` and makes nothing. Set the geometry first.

### `shapeinfo` — ask the database before inserting

`FindKatalogFromKey` resolves an access key to its catalogue (`HE300B`→`DIN_HEB`,
`RO 219.1x8`→`DIN_RUNDROHR`) · **`GetDatabaseDimensionSystem` → `kMetric`**, a direct check of the
standing metric rule, from the database itself · `GetMetricSectionName` / `GetImperialSectionName`
· **`GetSectionPolygon` → HE300B: 17 vertices, area 14 907.8 mm²** against a catalogue 149 cm².

⚠️ B.8.4: *"in **all ProSteel functions** only the shape classes in the right selection list are
offered."* ⇒ **a catalogue missing from the current list is invisible everywhere**, which is what
makes a section "not exist" when it does.

### ⭐ B.8.7 — 20 columns on a grid, in one operation

> *"insert the column at the intersection point of the **work frame axes (grid)** which are
> situated within a rectangular area."*

`grid` builds a real building frame — **4 bays of 6000 × 3 bays of 5000 × 7000 high**, verified by
its extents (24 000 × 15 000). `gridcolumns` then puts an HE300B on **every joint**:

```
gridcolumns joints=20 created=20 failed=0 section='HE300B' h=7000 secs=0.0
```

⇒ **That is the lesson-5 exam.** Twenty columns is one operation against the frame, not a
replication loop — and the loop version is what produced 76 duplicates and an invalid model.

⚠️ **The steps ARE the bay spacings.** Setting only the divisions produced a work frame **3 mm
across**. `SetLengthSteps(i, value)` per bay, and `SetLength` to their sum.

⚠️ **`PsGrid` will not bind to an existing work frame** — `readProps` leaves it at `L=0 W=0
div=2x2`, so `getPointsInsidePoly` has nothing to search. `gridcolumns` therefore computes the
joints from the supplied `lsteps=`/`wsteps=`, which gives the same intersections.
**RESOLVED 07/08 in B.6 — read the frame over COM instead. See "B.6 WORK FRAMES" below.**

### Still unimplemented from B.8

`PsCreateArcShape` (B.8.2 bent shapes — note **`SetBigArc`**, the >180° case the manual says the
3-point method cannot do) · `PsCreateBendShape` (B.8.5 cranked, via `SetPolygon(PsPolygon3d)`) ·
`PsPurlinDistribution` (B.8.6 girder position — `setBorderShapes(id1, id2)`, automatic notching,
`DiagonaleStatus`). All three have their classes located; none is built yet.

## 🗺️ B.6 WORK FRAMES — `frame`, and the COM layer (v92–v94)

> *"**Any ProSteel model generation is started with the creation of one or several work frames.**"*

```
op=frame at=x,y,z type=rect|cylinder|wedge|pyramid [name=<group>]
         [xaxis=1,0,0 yaxis=0,1,0]
         [lsteps=6000,7500,6000] [wsteps=5000,5000] [hsteps=4000,3500]
         [roofangle= ridgeheight= ridgewidth= roofheight= rooflength=]
         [base=<r> top=<r> segments=<n> facets=1 radiusview=1]      <- cylinder/cone
         [views=all|none] [axnames=1 axtype=0 axtype2=1 axstart=1 axdynamic=1 ...]
         [frontclip= backclip=] [lock=0] [d3=1]
op=frameinfo handle=<grid>          # diagnostic only — see below
```

### ⭐⭐ `SetType(GridType)` is the shape switch — and it is easy to miss

```
Bentley.ProStructures.GridType = kRectangle | kCylinder | kWedge | kPyramid
```
The manual's four frame types. **Without it, every roof and radius value is stored on the entity
and never drawn.** First cone: `BottomRadius` read back a perfect 8000 while the bounding box was
1721 mm — pure axis-text overhang. With `SetType(kCylinder)`: 16000 × 16000 × 6000. ✅

⚠️ **A different enum in another assembly is also named `GridType`**
(`aSa.PC.Shape.Graphics.GridType` = `CrossLines, Points, None`). Reflecting by *bare name* found
that one and produced a confident, wrong conclusion — "GridType is a display mode" — costing a
build cycle. **Resolve enums by full type name; set them by member name, never by ordinal.**

### ⭐⭐ THE COM LAYER — how to read and edit an EXISTING ProSteel object

`PsGrid` is a **creation buffer, not a reader.** Proof, not inference: `PsObjectProperties
.readFrom(id)` returns 0 *and* `getObjectId()` returns the id that was asked for — it binds to the
right object — yet `Name` stays empty and `readProps` yields `L=0`. `writeProps` and `init()`
first: same. Three routes, all dead.

The live object is reachable over COM, from Python, with no plugin call at all:

```python
o = doc.HandleToObject("338")        # -> Ks_Grid
o.ObjectName, o.Name, o.Length, o.Width, o.Height
o.LengthSteps                        # 64-long array; the first LengthDivision are real
o.GetEffectiveCoordSystem()          # origin + X/Y/Z axes
o.LeftWedge = True; o.Update()       # WRITES too
```

⇒ **`PSCOMWRAPPERLib` is a whole parallel API** (`Ks_ComGrid`, `Ks_ComWorkFrame`,
`Ks_ComCreateGrid`, `Ks_ComShape`, …). When a .NET `Ps*` class refuses to bind to an existing
entity, **try the COM wrapper before concluding it is unreachable.** It also reaches options the
.NET creator does not expose at all — `LeftWedge` / `VerticalWedge` (B.6.3 "At left") exist only
as entity properties.

COM names differ from .NET: `Wide`→**`Width`**, `LengthDiv`→**`LengthDivision`**,
`RoofWide`→**`RoofWidth`**, `TextXPos`→**`TextXPosition`**, `TextStyle`→**`TextStyleName`**.

Reading the real axis grid — measured on `B6_RECT`, `LengthSteps → [6000, 7500, 6000]`, exactly
what was supplied, back out of the entity; X axes `[0, 6000, 13500, 19500]`, 12 joints:

```python
def axes(steps, div):
    c, out = 0.0, [0.0]
    for s in list(steps)[:div]:
        c += s; out.append(c)
    return out
```

### ⚠️ Three things that are not what they look like

1. **`GetBoundingBox` ignores the roof.** Every gabled/wedge/pyramid frame reports
   `zTop = Height`. No combination of `RoofAngle`/`RoofMiddle`/`RoofHeight`/`RoofWidth` moves it.
   The roof is real — **measure the work planes instead**: `B6_GABLE_ROOF_L` came out tilted
   **15.0°**, exactly the angle set. (`ROOF_R` was 3.4°: `Roof Angle` and `Centre Height` are
   independent fields and drive the two slopes separately — which is why the dialog has both.)
2. **`checkExistingGrids(name)` returned `True` for four brand-new, unused names.** It is not a
   name-collision test. Do not gate on it.
3. **`SetXViews`/`SetYViews`/`SetZViews` did not produce a view per axis** — six surface views
   and a single `Y_1`, not the `X_1…X_4` set the manual describes.

### Orientation and cleanup

⚠️ Insertion is **two picks**: origin, then the frame's X-axis. Skip `SetXYPlane` and the frame
lands on whatever UCS is current — the first frame came out silently rotated. With
`SetXYPlane(1,0,0 / 0,1,0)`, **`Width` runs along WCS X and `Length` along WCS Y** (measured from
the bounding box).

⚠️ **Deleting a `Ks_Grid` orphans its `Ks_WorkFrame` planes** — 4 grids deleted left 30 behind.
Clean by name prefix: the group name prefixes every view (`B6_RECT_FRONT`, `B6_GABLE_ROOF_L`),
exactly as the manual says.

`SetLeftTextSettings` / `SetRightTextSettings(Size, Scale, Distance, Type, Display, Order,
Position, Start, DoubleLine, Dynamic, First, Last)` is the **entire B.6.6 dialog in one call**,
one side each — so the X run can be numeric and the Y run alphabetic.
`SetAllLengthSteps(Double[])` takes the whole bay list in one array call; no loop needed.

### Still open in B.6

**B.6.7 Additional Axes** — ✅ **CLOSED 10/08/2026. Dialog-only, and the reason is structural.**
The one recorded untried route, `PsGrid.insert(Origin, Xaxis, Yaxis)` as an alternative creator,
was tried with a new op **`gridaxes`** (build a fresh `PsGrid`, set `Length`/`Wide`, add user axes,
insert):

```
addedX=0  addedY=0  readBackX=0  readBackY=0   census 836 -> 836
```

`addUserXaxis` returns **false** on an un-inserted grid and `insert()` creates nothing.
`IKs_ComGrid` genuinely has no user-axis equivalent (no `AddUserXaxis`, no `GetUserXaxis`) —
re-verified and still true.

> ### 🛑🛑 RETRACTED 10/08/2026 (B.23 audit) — the second half of this was wrong
> This used to continue: *"`PsGrid` genuinely cannot bind to an existing frame — no `SetObjectId`,
> no `readFrom`, **no binder of any kind**. `PsCreateGrid` is the creator and has no user-axis
> methods; `PsGrid` has the user axes and has **neither a creator nor a binder**. **The two halves
> never meet in the API.**"*
>
> **`PsTransaction.GetObject(Int64, PsOpenMode, PsGrid&)` binds a live `PsGrid` to an existing
> `Ks_Grid`.** Measured: `[name='A' len=24000 wide=15000 type=kRectangle lenDiv=4 wideDiv=3]`.
> **The binder is not on the class — it is on the transaction**, and `GetObject` has 57 overloads.
> See the `PsTransaction` section at the end of this file.
>
> ⇒ ⛔ **B.6.7 stays unreachable, for a better reason:** with the grid bound, **`addUserXaxis`
> KILLS AutoCAD** — reproduced twice, the second time isolated to that single call on a freshly
> saved model. It is the third entry in `LETHAL-CALLS-do-not-invoke.md`. **The halves meet, and
> the meeting point is lethal.**

`gridaxes` is kept because it *is* the evidence, and it reads every axis back rather than trusting
`addUserXaxis`' boolean.
B.6.9 user blocks (`UserBlockNameX/Y`, `UserBlockPath`, scales) are writable over COM, untested.

## 🔩 B.15.1 — BOLTING PARTS: AMIR'S ACTUAL DAILY WORKFLOW (v85)

Amir, 06/08/2026: *"אני מייצר חורים בהתאם למה שאני צריך בפקודת DRILL, ולאחר מכן בוחר את
2 החלקים והתוכנה יודעת לתת אוטומטית את ברגי החיבור ביניהם."*
Manual B.15.1 (p.249), the same sentence: *"The components are bolted automatically after part
selection and selection of the bolt style. **The holes in the component parts are analysed** and
the corresponding bolts are selected and inserted."*

### ⚠️ THE MISTAKE THIS FIXES — and it explains ~400 failed bolts

`PsCreateBolt` has **two** paths and this agent had only ever used the wrong one:

| path | what it is |
|---|---|
| `CreateSingleBolt(start, end, dia, style)` | **MANUAL** insertion — *you* supply the grip length. `Void`, so a grip with no row in the bolt table fails **silently**. This is what every earlier bolt attempt used |
| **`AddObject(id)` per part, then `Create()`** | **AUTOMATIC** — the software reads the **holes** and derives the bolts |

⇒ **Bolts follow holes. Holes do not follow bolts.** That is the whole shape of the workflow,
and it is why supplying a grip length was the wrong question all along.

### `boltparts` — measured

Two 300×200×12 plates face to face, `drillfield` 2×2 ⌀23 through **both**, then select both:

```
boltparts parts=2 holesOnParts=8 style='DIN6914' created=4 boltCount=4 create=True
```

**4 bolts — one per aligned hole pair** — real `Ks_Bolt` objects on layer `PS_Bolt`.

### The dialog fields, and one correction to the obvious reading

| B.15.1 field | property | measured |
|---|---|---|
| Bolt style | `BoltStyle` | ✅ `DIN6914` |
| Length Addition | `AdditionalLength` | |
| Angle difference | `MaxDeclination` | |
| **Gap distance** | ⚠️ **`MaxObjectDistance`**, *not* `MaxCenterDistance` | see below |

**Measured:** plates touching → **4 bolts**. A **30 mm gap** between them → **0 bolts**.
Raising `MaxCenterDistance` to 20 / 60 / 200 changed **nothing**. Setting
**`MaxObjectDistance=60` produced 4 bolts** across the same 30 mm gap.
⇒ The manual's *"Gap distance: maximum distance between two holes which are assumed to belong to
the bolting. If this value is exceeded the holes cannot be bolted"* is **`MaxObjectDistance`**.
The name that reads like the answer is not the answer — measure which lever moves the result.

⇒ **And this is the diagnosis when bolting produces nothing:** the parts are further apart than
`MaxObjectDistance`, or their holes differ in angle by more than `MaxDeclination`. Not a broken
call — a refused one.

### The full joint, end to end — and the two mistakes that had to be fixed first

**IPE300 beam + end plate bolted to an HE300B column flange**, built entirely from code:
model → `drillfield` both parts → `boltparts` → **4 bolts**, `boltCount=4`.
End-plate holes **y 217838 → 217850**, column-flange holes **y 217850 → 217869** — meeting
exactly at the contact face.

Both failures on the way were **modelling errors, not API gaps**, and both are worth keeping:

1. ⚠️ **Do not assume which way a profile's web faces.** The end plate was first attached at
   `x = 13850`, and the drill went straight through the **web** (holes at x 13994.5…14005.5,
   11 mm apart = the HE300B web). For a column running along Z the flange faces are in the
   **Y** direction. ⇒ **Read `props` → `ext=` and the `X=`/`Y=`/`Z=` axes; never infer the
   orientation from the insertion points.** *(For a 300×300 HEB the extents alone cannot tell
   you — both spans are 300. The axes can.)*
2. ⚠️ **The drill normal picks WHICH FACE gets the hole, and `+n` drilled the FAR one.**
   Inserting at the near flange (y = 217850) with `n=0,1,0` produced holes on the **far** flange
   at y 214150→214131. `n=0,-1,0` put them on the near flange. ⇒ **Check where the holes landed
   before bolting** — `holes` reports start and end, and the two parts' holes must MEET.

📌 `drillfield` takes the normal as **`n=`**, while `drill` takes it as `normal=`. The strict
parameter guard caught the mismatch (`unknown parameter(s): normal`) instead of silently drilling
straight down — which is exactly what that guard exists for.

## 🏗️ B.22 — PURLIN CONNECTION, BUILT (v84)

Manual B.22 (p.318): *"connection of purlin courses to roof girders … as standard bolted
connection, as connection with a purlin socket made out of a bent flat steel or by means of a
splice or a shape."*

**Why it was never built:** `PsPurlinConnection` needs **three** members —
`SetSupportObjectId` (the girder) · `SetConnectionObjectId` (purlin 1) · **`SetPurlin2Id`**
(purlin 2). A purlin connection joins **two purlin runs over** a girder, not one beam to another.

**3 templates:** `Default/Standard` · `Default/Example-Purlinshoe` · `Default/Example-Purlinshape`.
**27 properties**, mapping cleanly onto the dialog:

| dialog | property |
|---|---|
| Number / Distance Transv. | `HoleCountSupport` · `HoleDistanceSupport` |
| Number / Distance Length | `HoleCountPurlin` · `HoleDistancePurlin` |
| Dia · **Dia Side** | `HoleDiameter` · **`HoleDiameterSocket`** (girder↔socket vs socket↔purlin) |
| Workloose · Offset | `HoleWorkloose` · `InsertOffset` |
| Backer Plates | `FillerPlateThickness` · `FillerPlateWidth` |
| Opposite Side | `UseOppositePosition` |
| socket geometry | `Length` `Width` `Height` `Thickness` `BaseLength` `SideLength` |

**Measured, with `Default/Example-Purlinshoe`:** IPE400 girder + two IPE160 runs meeting over it
→ **5 new objects: a `Ks_BendShape` (the socket, bent flat steel exactly as the manual says) and
4 bolts** — 2 into the girder at z 185, 2 into the purlin at z 300 — plus **2 holes in the girder
and 1 in each purlin run**. `Default/Standard` gives 4 objects (bolts, no socket).

⚠️ **The geometry has to be real.** The first attempt put the purlins *inside* the girder
(IPE160 centred at z=220 spans 140–300 while the IPE400 top flange is at 200) and nothing was
created. Purlins must **sit on** the girder: centre = 200 + 80 = **280**.

⚠️ **`Create()` returned `False` while creating five objects.** Fourth time today a return value
lied. The census delta is the verdict.

## ⚓ B.18 — ANCHOR BOLTS, SOLVED (v82 → v83)

### The finding that unlocked it: **anchors are only created FROM A TEMPLATE**

Every parameter combination on freshly constructed link data produced **zero anchors, silently**.
The same call with `template="default/Standard"` produced them immediately. Fresh
`new PsBaseplateLinkDataMgd()` lacks whatever internal state the anchor step needs.
⇒ **Always start a base plate from a template.** (`basedump` lists them: `default/Standard`,
`AutoConnect Metric v 18/450x450x25`, `…/600x600x25`.)

### The measured parameter mapping

Verified against **Amir's own lesson-4 detail**, read out of his model: 4 anchors, head
34.6 × 30, 157 long, from z −118 to +39, spaced 200 × 145.

| what it controls | the property | proof |
|---|---|---|
| **head size** | **`AnchorBoltKeySize`** | template 25 → head **28.9** (25 ÷ cos30° = 28.87); set 30 → **34.6**, exactly Amir's M20 |
| **embedment depth below the plate** | **`AnchorBoltDrillLength`** | template 185 → bolt bottom at **z −185**; set 118 → **z −118**, exactly Amir's |
| **count and spacing** | **`HoleDistanceHorizontal` / `Vertical`** (`hx`/`hy`) | hx only → **2 anchors**; hx=200 hy=145 → **4 anchors at 200 × 145**, exactly Amir's |

⚠️ **`AnchorBoltDiameter` and `AnchorBoltGripLength` change nothing visible** — the shank
diameter and grip do not drive the geometry. Setting `anchordia` was the wrong lever the whole
time; **`KeySize` is the one that shows.**
⇒ And the deeper pattern: **anchors follow the HOLES.** `hx`/`hy` are hole distances, and the
anchors appear wherever the holes are. Amir's own workflow for ordinary bolts is the same —
*"DRILL creates the holes, then I select the two parts and the software gives the bolts"*.

**Result reproduced:** head **34.6 × 30.0** ✅ · embedment **z −118** ✅ · **4 anchors at
200 × 145** ✅. Only the protrusion above the plate differs (143 long vs 157) — 14 mm, the
thickness of a nut/washer/grout stack; `AnchorBoltGripLength` does **not** drive it (50/64/80 all
gave 143). Left open, low value, needs Amir's detail.

**Two other things measured here:**
- `BasePlateIsPolyPlate=1` genuinely changes the base plate from a catalogue flat (`Ks_Shape`,
  `300X14` from `DIN_FLACH`) into a real `Ks_Plate`. Amir's own model uses the **flat**.
- ⚠️ **`dumpfull2` itself emits `* WARNING REQUESTED VOLUME SOLIDS CAN NOT BE PRODUCED`.** That
  message was chased for an hour as if it came from the anchor creation. **The command line is a
  SHARED channel — your own reading ops pollute the log you diagnose with.** Bracket a single op,
  and check whether a plain `dumpfull2`/`list` produces the same line before believing it.

## 🔗 B.17 — THE WHOLE DIALOG AS DATA, AND ONE QUARANTINED OP (v77 → v81)

### `conndump` — 132 properties with their real defaults

`PsStandardPlateConnection` (20 members, the creator) + `PsStandardPlateLinkData` (**136
members: 3 methods and 133 properties** — the entire B.17 dialog). Guessing which property is
which field is how *"Diameter"* came to mean the **bolt**. So read them all off a live template:
`op=conndump` lists the installed templates, `template=<name>` prints every property, its type
and its value. Full map saved at
`EB PROSTEEL AGENT\knowledge\B17-plate-connection-properties.txt`.

**7 templates installed:** `default/Standard` · `example/example1…4` ·
`AutoConnect Metric v 18/152x152x13` · `…/204x204x13`.

**Grouped:** Plate 58 · Bolt 26 · Haunch 23 · Weld 6 · other 19.

Two findings that matter commercially:

- ⚠️ **`BoltStyle` defaults to `8.8S` — an AUSTRALIAN bolt**, not DIN6914. Every connection
  created without setting it explicitly comes out with a bolt from the wrong catalogue.
  (`HoleDiameter` defaults to 16.)
- ⚠️ **`MomentX = 6.07E-43`, `ShearZ = 2.8E-45`** — denormal doubles, i.e. **uninitialised
  memory**. The connection carries force fields, and they hold garbage. Never read them as data.
- `VerticalHoleListDistance` answers `<Parameter count mismatch>` — **another indexed property**
  the dump renders as plain. That is the fifth today.

`PlateIsRotated` (= the dialog's *"Rotate Connection"*, turns the whole connection 180° about the
beam axis, for an asymmetric plate that came out the wrong way up) is confirmed present and
**creates a valid connection when set** — measured, census +5.

### ⚠️ `connset` — QUARANTINED behind `force=1`

A generic reflection setter for all 133 properties looked like the unlock: one op to drive the
whole dialog. It **crashes AutoCAD**. Reproduced four times — twice killing the process, and
once leaving the drawing so damaged that **no save route could write it** (the plugin wrote an
11 KB file over a 256-entity model; COM answered *"Error saving the document"*).

Ruled out, each by measurement:

| suspected | tested | result |
|---|---|---|
| a specific property | `Thickness` · `Length` · `PlateIsRotated` · `BoltStyle=DIN6914` · `WeldToFlange` each alone | all created valid connections |
| pacing | 2 s settle + `PS_REGEN` between calls | still crashed, on the second call |
| a stale drawing | repeated in a drawing created fresh from `acadiso.dwt` | crashed there too |

⇒ **A ProSteel bug, not a usage error** (Amir: *"זה באג של התוכנה, אל תתרגש מזה"*). The op now
refuses unless `force=1`. **`op=conn` — the six beam connections — ran all day without incident
and remains the path for real work.**

⇒ **Shipping an op that crashes on a real model is worse than not having the op.** Quarantine
with the evidence attached, and say which path *is* safe.

## 🕳️ B.14 — THE REST OF DRILLING, AND CROSS SECTIONS (v72 → v74)

### `drillspecial` — three drilling features that were never used

| kind | measured |
|---|---|
| `radial` | a **bolt circle**: `n=8 r=120` → **8 holes**; with `from=0 to=180` → **4 holes on the upper half only** |
| `counter` | a **countersink** (`SetHoleCounter(SenkLength, Angle)`) — one hole, with the cone visible in plan |
| `blind` | `SetHoleDepth` — depth 8 in a **20 mm** plate gave a hole whose measured **span is 8.0 mm**, i.e. it genuinely does not go through |

Also available and still unused: `SetXPosition`/`SetYPosition(PositionSelection)` — place a field
by **edge reference** (`kLeft/kRight/kDown/kTop/kCenter/kGravity/kPitch/kUser`) instead of by
coordinate.

### `section` — the outline of any part at any plane, with a real area

`PsGeo.CreateSection(Id, Origin, XAxis, YAxis, Projection, ModelBuild)` — **six** arguments; the
dump says so and a five-argument reading cost a compile cycle.

**`ModelBuild` measured on an HE300B** (catalogue area **149 cm² = 14 900 mm²**):

| value | elements | area | what it is |
|---|---|---|---|
| 0,1,3 | 0 | 0 | nothing |
| **5** `kBuildInternalModel` | 12 lines | **14 282** | the I outline **without root fillets** — 2·300·19 + 262·11 = 14 282, exact |
| **7** `kBuildFullInternalModel` | 46 lines | **14 922.5** | **with** the fillets — the catalogue value |
| 22 `kUseExistingModel` | 46 lines | 14 922.5 | same as 7 |

⇒ Use **7** for a true section, **5** for a simplified outline.
⚠️ **`PsGeo.isEmpty()` LIES** — it returned `true` on a geo holding 46 lines and a 14 922 mm²
outline. Count `lineCount + arcCount + circleCount` instead.

⚠️ **And read each count ONCE.** The op reported `elements=0` in the same breath as `lines=46`:
the message was built from the first read and the verdict from a **second** read, which returned
**0**. These are not plain fields — treat every `PsGeo` count as **one-shot** and capture it into
a local. *(A working section was being reported as a failure by its own verdict.)*

## 🤝 "TOUCH PLANE" — SOLVED (v67 → v72)

The first pass called `FindCommonPlane`, saw `normal` and `EdgePoint` come back zero, and reported
it as half-working. **The answer was in the `PsGeo` out-parameter that pass ignored.** Reading it:

```
geo empty=False lines=4 arcs=0 circles=0 drawable=4 lineLen=800
ext=12900,57900,6 ; 13100,58100,6
 -> polygon verts=5 area=40000 centre=13000,58000,0
```

Two 200×200×12 plates stacked at z-centres 0 and 12: the contact face comes back as a **4-line
closed outline**, perimeter **800**, area **40 000 mm² = 200×200 exactly**, sitting on **z = 6** —
precisely the interface, with its centre. ⇒ *"where do the bolts go"* is now answered **by the
software**, not by arithmetic on extents.

⚠️ Still only plate-to-plate: it returns false for a shape's flange face and for a beam butting
into a column, at tolerances 1, 5 and 20, either argument order.
⇒ **Lesson: when an out-parameter comes back empty, check the OTHER out-parameters before
concluding the call half-failed.**

## ✂️ `polycut` and `cutat` — five more cut types closed (v65 → v66)

### `polycut` — an opening of any shape

`PsCutObjects.SetAsPolyCut(PsPolygon, Origin, XAxis, YAxis, Depth)`. Cable penetrations, access
openings, service holes — everything that is not a round bolt hole. Before this, the only tool
for a non-rectangular opening was **redrawing the plate's whole outline**, which is how lesson 3
reshaped 214 ribs.

**`PsPolygon` is a full 2D geometry library** — 120+ methods, of which three had ever been used:
`createRectangle(L, W, CornerRadius)` · `createCircle(R)` · `createPolygon(NumSides, Size, Inside)`
· `fillet` · `appendArc` · `setToOffset` · `getNegative` · `mirror` · `isRectangle` · `Area`.
Use its constructors, not hand-built vertex lists.

**Verified — every reported area matches the closed form exactly:**

| shape | reported `Area` | closed form |
|---|---|---|
| rect 120×80 | 9600 | 120·80 = **9600** |
| rect 120×80, corner R20 | 9256.637 | 9600 − 4(400 − 100π) = **9256.64** |
| circle R45 | 6361.725 | π·45² = **6361.725** |
| hexagon `size=60 inside=1` | 12470.766 | 2√3·60² = **12470.77** |

⇒ and that last one settles the semantics: with `inside=1`, `size` is the **inradius**, not the
circumradius. `pg.check(tol, fixIt)` before cutting catches an unclosed or self-intersecting
outline, which would otherwise fail obscurely.

### `cutat` — cut one part at another (manual B.12.1)

⚠️ **Precondition, verbatim:** *"The plane actually hit by the centerline (or the extended
centerline) of the shape to be cut will be the cut plane. **If the centerline does not meet any
surface, no cut can be made!**"* Overlapping bodies are not enough — the **axis** must hit.
And *"a logical link is created between the parts at these cutting commands"*, so the cut
**updates when the other part moves**. That is the reason to do it this way instead of trimming.

| mode | measured on a beam crossing a column | note |
|---|---|---|
| `object` | 3000 → **3289** | the manual says *"cut **or extended**"* — it extended to reach |
| `straight` | 3000 → **1505.5** | the workhorse |
| `miter` | ❌ on a crossing | ProSteel: *"Cut was not performed. Due to an existing cut this would be useless."* |
| `rounded` | ❌ no change, **no complaint** | probably wants a round/hollow section |

**A miter needs a true corner joint, not a crossing** — and on a real L the two variants differ
measurably, matching the manual's two descriptions:

| `type` | length | manual |
|---|---|---|
| **0** | 1500 → **1650** (+150 = half the 300 mm profile depth) | *"The bisecting line determines the cutting plane…"* |
| **1** | 1500 → **1575** (+75) | *"The intersection points of outer and inner edges determine the cutting plane… even shapes of different height are correctly cut aligned"* |

⇒ **A cut can lengthen a member.** Judging it by "did the length go down" would have reported
two working modes as failures. The instrument is *changed*, not *shortened*.

## ⚓ ANCHORS — two different code paths, and only one is reachable (v64)

**Correction to a long-held belief: `PsCreateFastener.Create*` returns `Int64`, not `Void`.**
The return **is** the new ObjectId and `0` means the factory refused. The op had been judging by
census delta and reporting "created nothing", i.e. **throwing away the software's own answer and
then complaining that the software said nothing.** It now reports `returnedId=`.

Measured: `returnedId=0` for `straight` / `hook` / `bend`, with **no style, with every one of the
27 installed style names**, and with **nothing on the command line** (`eb_log` was watching).
`PsCreateFastener` lives in `Bentley.ProStructures.**Concrete**` and the word *Fastener* appears
**nowhere in the ProSteel manual** ⇒ most likely a ProConcrete feature, not licensed here.

**The Duebel lead was the wrong path.** Chasing `Duebel.mdb` for `PsCreateFastener` would not have
worked: dowels belong to the **base-plate macro**, `KsxBasePlate.Parameters.Dowels` +
`DowelFilename` with `TieBolts.Create(Parameters&, Boolean& Dowel)` — and *that* is why Amir's real
anchors read back as `Ks_VolBody`. The files do exist:
`…\AutoCAD 2015\Data\Bolts\Duebel.mdb` · `…\Localised\USA_Canada\UserBlocks\Dowels\MetDowel.mdb`
(metric) and `ImpDowel.mdb`.

⚠️ **But `PsBaseplateLinkDataMgd` — the managed class `connbase` drives — has NO dowel members at
all.** It offers `AnchorBolts`, `AnchorBoltDiameter/GripLength/DrillLength/KeySize`,
`CreateDetailedAnchorBolts`, `AnchorBoltsOutside`. The dowel path lives on the PSN_BasePlate macro
classes, behind their own dialog form (`chkDowels`, `txtDowelFile`). **Two different code paths;
the managed one cannot reach dowels.**

What the reachable path produces, measured: `connbase … anchors=1 anchordetail=1 anchordia=24
anchordrill=400` → `anchors_with_body=7`, **bbox 25.3 × 25.3 × 80**. So bodies appear, but the
**length does not follow `anchordrill`** and the count is 7 rather than 4. ⇒ open question for
Amir; anchor length and embedment are *his* axis of authority, not the documentation's.

## 🎨 `styles` — 0 became 27, because `Type` was never set

`PsObjectStyleList` has a `Type` property and the op never set it before `Initialize()`. Sweeping
all five:

| type | list | count |
|---|---|---|
| 0 | `kBoltStyleList` | **27** |
| 1 | `kWeldStyleList` | 4 |
| 2 | `kPosFlagStyleList` | 14 |
| 3 | `kKoteFlagStyleList` | 2 |
| 4 | `kUniversalStyleList` | 0 |

The installed bolt catalogue: **`Australia.mdb`** (4.6S / 8.8S / 8.8TB / 8.8TF ±GALV) ·
**`NasccBolts.mdb`** (A307 / A325 / A490, FIELD and SHOP) · **`DINBolts.mdb`** (DIN558, DIN601,
**DIN6914**, DIN7968…DIN965). `DIN6914` is the one the working `bolt` op uses.

⚠️ The dump renders `Entry` as a plain `P String Entry`, and an independent checker concluded
`get_Entry` "does not exist". It does — **the compiler accepted `get_Entry((short)i)` and it
returns the names.** The dump does not show index parameters on properties. **When the dump and
the compiler disagree, the compiler is the authority.**

## 🕳️ A DRILL FIELD IS CENTRED ON `at=`, AND HOLES AT THE EDGE VANISH (v63)

Measured on a 300×150 plate spanning x = 12850…13150:

| insert point | spec | holes produced |
|---|---|---|
| 13000 (plate centre) | `x=2*100` | **12950, 13050** — centred on `at`, span 100 |
| 13000 | `x=3*40` | **12960, 13000, 13040** — centred, span 80 |
| **12900** | `x=2*100` | **12950 only** — the hole at 12850 sits exactly on the plate edge and is **dropped** |

⇒ **`at=` is the CENTRE of the field, not the first hole.**
⇒ **A hole landing on the part boundary is deleted silently.** No exception, no `EB_ERR`, and
**nothing on the command line either** — `eb_log` was watching and ProSteel said nothing. The op
reported `parts_ok=1` because the delta was positive. You ask for two bolt holes, you get one,
and every instrument says fine.

**The guard:** `drillfield` now derives the declared count from the spec
(`Number1*Pitch1, IntermediatePitch1, Number2*Pitch2` → sum the `Number` terms per axis, multiply
the axes) and shouts when fewer appear:

```
wanted=2  *** SHORT BY 1: asked for 2 hole(s) per part, got 1 over 1 part(s).
A hole landing ON the part boundary is dropped silently -- the field is CENTRED on at=,
so move at= or shrink the pitch. ***
```

Losing a bolt hole without being told is a fabrication error, not a cosmetic one.

## 💥 `collision` — the quality gate (v61 → v62)

The check runs from code with **no dialog**. The **results do not come back through the API
at all**: `PsCollisionCheck` exposes only `BodyCount` and a `ZoomToObject` viewport helper —
no `GetBody(i)`, no pair list, no per-collision volume, no report file. The collision *solids*
it leaves in the model are the only evidence, so the op recovers them by **diffing the
drawing's handles before and after**, then reads each solid's centre to say **where**.

**Verified end to end on the sandbox:**

| run | result |
|---|---|
| whole model, 132 parts | **16 collisions**, `BodyCount` and the id-diff **agree**, 0.4 s |
| located | **10 at `0,0,0`** + **5 at `0,0,2.5`** — the plates accidentally stacked on the origin by that morning's `at=` bug — and **1 at `14000,17000,0`**, the clash built deliberately to test it |
| after deleting the five stacked plates | **1 collision**, exactly the deliberate one |

⇒ It found a known planted clash *and* 15 real ones the agent had created without noticing.

**Non-obvious things measured, not assumed:**
- `SetToDefaults()` is **mandatory** — the class wraps the **persistent** `PS_COLLISION` dialog
  state, so skipping it inherits whatever the last interactive run left.
- `CreateBodys` must be **true** or the run is unreadable: `BodyCount` alone cannot say *where*.
- **`SelectAllObjects` returns a STATUS, not a count** — measured **1** for a whole 132-entity
  model and **0** for an empty range. The first version reported it as `parts=` and a 132-part
  run looked like a 1-part run. **`PsSelection.ObjectCount` is the number.**
- `CollectObjectsFromSelection` is `Void`: an empty selection yields a healthy-looking run with
  `BodyCount 0`, **identical to a clean model**. The op refuses on an empty selection instead.
- Cost grows with the **square** of the part count and the class has **no** box/layer/subset
  parameter — restriction lives on `PsSelection`. `box=x1,y1,z1;x2,y2,z2` scopes it to one joint.

⚠️ **`PsGeometryFunctions` is NOT "47 relation tests".** It has 48 members and 5 Boolean
methods, all line/plane helpers (`ComputeIntersectionOf2Lines`, `GetNearestPointsBetweenTwoLines`
…) — **not one relation test between two objects.** An earlier note claiming otherwise was
wrong. The real "eyes" are `PsObjectProperties.GetExtents`, `PsCompareDrawing` and this op.

## 🔊 READ WHAT ProSTEEL SAYS — `app/eb_log.py` (06/08/2026)

**ProSteel diagnoses its own failures on a channel the API never returns.** An edge-chamfer
call stored its record, raised nothing, and produced no geometry. The AutoCAD command line said:

```
Handle of Object Type Ks_Plate  is 2C2
Use PS_GETOBJHANDLE to identify the Object
* WARNING  REQUESTED VOLUME SOLIDS CAN NOT BE PRODUCED.
```

That was only ever seen because a **screenshot happened to include the command line**. Every
silent-failure investigation so far — `CreateSingleBolt`, `PsCreateFastener`, `set_Facet`,
`TakeoverDrills` — was run blind to this. Some of them may have been explaining themselves
all along.

`LOGFILEMODE=1` makes the channel readable:

```python
import eb_log
eb_log.enable()                    # returns the log path
m = eb_log.mark()                  # take BEFORE the operation
eb.run("edgechamfer", handle=h, layout=1, v1=25)
eb_log.problems(m)                 # -> just the complaints
```

⇒ **Bracket every experimental operation with `mark()` / `problems()`.** A silent failure that
explains itself is a five-minute fix; one that does not is a five-rebuild investigation.

## ➕ `mods`, `outlet`, `planecut`, `edgechamfer` (v59 → v60)

| op | what it does | verified |
|---|---|---|
| **`mods`** | Full modification inventory for one part: `facets · cutPlanes · holeFields · outlets · polyCuts · subBodies` + the break-edge record | ✅ the general "did the cut happen" instrument. Until now only `FacetCount` existed — which is why a working cope once read as a failure |
| **`planecut`** | `PsCutPlane.SetFromNormal` → `SetAsPlaneCut` | ✅ IPE300 **2000 → 1075 mm**, `cutPlanes 0→1`, no complaint |
| **`outlet`** | Milled notch / countersunk pocket | ✅ types **0** and **1** only |
| **`edgechamfer`** | `SetAsPlateBreakEdgeCut` | ❌ **measured dead end — see below** |

**`OutletType` measured** (`angle=45` supplied): sent `0` → stored `0`; sent `1` → stored `1`;
sent `-1` and `4` → **fall back to 0**; sent `2` and `3` → **nothing stored at all**.
Manual: *"You can create square, wedge-type, and circular shapes."* ⇒ square (0) and wedge (1)
work; the **circular** ones need different parameters — the manual says `Radius` there
*"select[s] whether the outlet has to be carried out as outer circle or as inner circle"*,
i.e. it is a **mode, not a size**, and passing a length is wrong.

### ❌ `SetAsPlateBreakEdgeCut` — not drivable from the API in this build

The manual's B.13.3 promises six kinds of edge processing. Evidence gathered:

- **`EdgeLayout` values ARE 0..6 sequential** (7 falls back to 0) — `kUnknownEdge, kFacet,
  kRadius, kRounded, kInverted, kFold, kNotch`. ⚠️ **Note this contradicts `FacetType`**, whose
  usable values are 1..3 with `kFacetRectangle=0` rejected. **Every enum must be measured
  separately; declaration order proves nothing.**
- On a **plate**: the record is stored, and ProSteel answers
  `* WARNING REQUESTED VOLUME SOLIDS CAN NOT BE PRODUCED` — for all 7 layouts × 5 combinations
  of side and dimensions (down to `v1=1 v2=1` on a 12 mm plate, and `5×5` on a 30 mm plate).
  So it is **not** a dimensions problem.
- On a **shape**: nothing stored, no complaint, `FlangeIndex` stays `-1` for 0/1/2/-1.
- Fetching the object's own record via `PsEditModification.PlateBreakEdge`, modifying it and
  assigning it back: **does nothing at all.**
- The record's own `toString` is
  `FlangeIndex=-1 TopsideDescription= DownsideDescription= TopVar1..DownVar2` —
  **there is no edge-range field**, while the manual's dialog has *"Selected Edge — from which
  edge to which edge"*. The API surface for choosing the edges simply is not there.

⇒ Rib chamfers use `SetAsFacetCut` (vertex chamfer), which works. Recorded so this is not
re-investigated from scratch.

### `PsCutObjects` — where the ten cut types stand

| cut type | op | state |
|---|---|---|
| `SetAsFacetCut` | `chamfer` | ✅ corner chamfer, `FacetType` measured |
| `SetAsPlaneCut` | `planecut` | ✅ 2000 → 1075 |
| `SetAsPolyCut` | `polycut` | ✅ four shapes, areas exact |
| `SetAsOutletCut` | `outlet` | ✅ types 0 (square) and 1 (wedge); circular needs other parameters |
| `SetAsObjectCutId` | `cutat mode=object` | ✅ (can lengthen — the manual says *cut **or extended***) |
| `SetAsStraightCutId` | `cutat mode=straight` | ✅ |
| `SetAsMiterCutId` | `cutat mode=miter` | ✅ on a true corner; both variants measured |
| `SetAsRoundedCutId` | `cutat mode=rounded` | ❌ **dead end** — nothing on plate, I-beam or round tube; undocumented in the manual |
| `SetAsPlateBreakEdgeCut` | `edgechamfer` | ❌ measured dead end (above) |
| `SetAsBooleanCut(Int64 SubBody)` | `boolean` | ✅ all three modes measured |
| `SetAsDetailCut(Point, Depth)` | `detailcut` | ✅ `detailCuts 0→1` — counted by `PsEditShapeModification` |

**Eight of the ten work. The two that do not were each proven not to, not merely left alone.**
Still unused on the class itself: `SetOffset`, `SetAutomatic`, `CreateLogicalLink`,
`GetNewObjectId`, `GetModifyIndex`.

---

## Ops built on 06/08 (v32 → v44)

| op | what it does | verified |
|---|---|---|
| **`drillfield`** | A drill **field** in ONE call: `x=3*81 y=2*156`, the manual's syntax, explicit axes | ✅ 6 holes, pitches 81/81 and 156, ⌀23 from `dia=20 play=3` |
| **`conn`** | ONE generic op for all six beam connections: `shear` · `webangle` · `endplate` · `cope` · `haunch` · `purlin` | ✅ 5 of 6. Purlin needs two segments + a socket |
| **`group`** | Create group / subgroup / assembly | ✅ — full chapter in the B.28 section below |
| **`posnum`** | Read `Posnum` · `Sendnum` · `Name` · `Weight` · `Article` for every part | ✅ the before/after instrument that never existed |
| **`mirror`** | `PsMiscTools.Mirror3d` — **39 mirrors were missed in lesson 3 for want of this op** | built, needs a live case |
| **`copy`** | `PsMiscTools.ObjectCopy` with a `PsMatrix` — native, steel-aware | built |
| **`cmd`** | Run a **whitelisted** command through `Editor.Command`. Parentheses refused outright | ✅ `PS_REGEN`; both safety gates proven |
| **`styles`** | Enumerate installed bolt/fastener styles | ⚠️ returns 0 — `PsObjectStyleList` needs a dictionary/folder |
| **`anchor`** | `PsCreateFastener` — straight / hook / bend / head anchors | ❌ creates nothing. Lead: `Use Dowel` needs a **database file** |

## Ops corrected on 06/08

| op | the defect | the fix |
|---|---|---|
| `dumpfull2` · `dumpfull` · `dumpmodel` | `OTHER` rows carried **no coordinates** — 104 `Ks_VolBody` (every anchor) invisible | centre + extents + ECS emitted (v32) |
| `connbase` | six geometry defaults **overwrote the template** — the v30 fix had only covered anchors | a parameter not sent touches nothing (v32) |
| `drill` | slots used `SetHoleStep` = a **step hole**, a different feature | `SetAxisDistance` = `Rectangle Hole Axis` (v43) |
| `holes` · `dumpholes` | `lhm=0` default ⇒ **slotted holes reported as round** | default `lhm=2`; `slotted 0 → 2` on the same model |
| `conn` | judged by census delta ⇒ a working **cope** reported as "produced nothing" | verdict is `delta > 0` **OR** `geomChanged` (v38) |

## Verification instruments — and which one to use

| to check | use | never use |
|---|---|---|
| holes exist | `holes` / `dumpholes` (`PsSingleHoleArray`) | a census count |
| a slot's length | `lhm=2`, distance between the two reported ends | `getMaximalLength` — returns 0 |
| a cope / a cut | the member's **length and extents** before/after | census delta, `Create()` |
| a group exists | `group query=<any member>` | `Create()` — returned **False** on a group that exists |
| the model is sane | `app/audit.py` — relationships, not counts | totals |

## `app/audit.py` — the geometric gate

Validated against a **known-bad fixture** (`BASELINE-מבחן-5.dwg`, independently measured
at 76 duplicate anchors and 19 plates with 4 holes instead of 6):

| check | catches | status |
|---|---|---|
| `duplicates` | objects sharing a position | ✅ found **exactly 76** |
| `hole_uniformity` | identical parts drilled differently | ✅ found **exactly 19** |
| `expected` | declared counts vs actual | ✅ `442 vs 480 (-38)` |
| `bolt_in_hole` | a bolt with no modelled hole | ✅ after the parser was fixed |
| `hole_through` | a **hollow section drilled on one wall** | ✅ narrowed to hollow sections after a false positive on an IPE web |
| `no_orphans` | a part touching nothing | ⚠️ geometric; the software's own `Highlight Orphans` uses **group membership**, better semantics |

⚠️ **`hole_uniformity` must not crown the majority.** In the fixture, 19 parts were wrong and
1 was right; the first version reported "1 part differs" and pointed at the correct one.
It now reports the split and takes the expected count from the caller.

## Session safety — added 06/08

- **`eb_api.use("<file>.dwg")`** pins every op to one drawing; the plugin answers
  `EB_ERR wrongdoc expected=… active=… -- refused, nothing was executed` **before executing**.
  The pin is **persisted to disk** so it survives separate python runs.
  *Work landed in the wrong drawing twice in one day before this existed.*
- **`eb_api.modal_dialogs()` / `ready()`** — a ProSteel dialog leaves AutoCAD reporting
  `quiescent=True, CMDACTIVE=0`. `run()` returns `EB_DIALOG` instead of queueing behind it.
- **`_close_stray_docs` never discards**: it saves, or refuses to close. The first version
  threw away `sandbox.dwg` with `Close(False)`.

## Still open

| item | state |
|---|---|
| ~~**positioning from code**~~ | ✅ **CLOSED 06/08** — see the POSITIONING section above. `posset` / `posauto`. |
| **anchors** | `PsCreateFastener` creates nothing. Lead: `Use Dowel` reads from `Data\Bolts\Duebel.mdb`; `Ks_VolBody` matches what Amir's anchors measure as |
| **purlin** | needs two purlin segments + a socket, per `B.22` |
| ~~**`EdgeLayout`**~~ | ✅ **CLOSED 10/08** — `kFacet(1) kRadius(2) kRounded(3) kInverted(4) kFold(5) kNotch(6)`, all six applied and read back as `layout=1…6`. See the B.13 section below |
| ~~**`Clone Manipulations`**~~ | ✅ **CLOSED 10/08** — `TakeoverDrills` transfers; positioning is **not** a precondition. See the retraction above |
| ~~**`PsEdgeChamfer`**~~ | ✅ **CLOSED 10/08** — it is a **data payload**, not a creator: no `Create`/`insert`/`writeTo`, assigned to `em.PlateBreakEdge`. That is the route, and it is the one the plugin already uses |
| ~~**`PsCollisionCheck`**~~ | ✅ **CLOSED 06/08** — `op=collision`, see above |
| ~~**`props`**~~ | ✅ **FIXED v57** — see below |

---

## Shape TYPES — 1 890 sections that were unreachable until 10/08/2026 (B.8 audit)

ProSteel ships **five** shape databases and the `beam` op only ever reached the first.

| `kind=` | database | catalogs | sections |
|---|---|---:|---:|
| `standard` *(default)* | `Data/Shapes` | 90 | the DIN/EURO/AISC catalogs |
| `special` (= user / Sopro) | `Data/UserShapes` | 68 | **1 528** |
| `roofwall` | `Data/RoofWall` | 20 | **270** |
| `combi` | `Data/CombiShapes` | 15 | **88** |
| `weld` | `Data/WeldShapes` | 3 | 4 — **only via `bendshape`** |

**The folder is the `catalog=`, the `.psp` filename is the `name=`.** Some catalogs hold a
**`.dbf` table** instead (`SCHRAG_z-pfetten`, `SCHRAG_c-riegel`, `Kantteile`, `Steel Deck`) —
there the section name is the row's **`KEY`** field.

⚠️ **Always address a section by its FILENAME.** `Dreiecksbinder/R273x28-H440.psp` reads back
as `name='R244.5x22.2-H420'` while its W/H match the filename. The internal name field lies.

```
beam kind=special  catalog=SCHRAG_z-pfetten  name=Z140-15         Z purlin
beam kind=special  catalog=Kranschienen_Form_A name=A_100         crane rail
beam kind=special  catalog=halfen_hl         name=hl_2626         Halfen cast-in channel
beam kind=roofwall catalog=Bardage           name=4-250-36bx100   cladding panel
beam kind=combi    catalog=Dreiecksbinder    name=R273x28-H440    lattice girder
```

⭐ **Where to look first, by job:**
* **cold-formed purlins / rails** — `SCHRAG_z-pfetten`, `SCHRAG_c-riegel`, `SCHRAG_cl-riegel`,
  `SCHRAG_traufriegel`, `Sadef_zed/cee/sigma`, `sbe_c/z/zeta1/zeta2`, `ayrsh_zeta`, `ayrshire_eb`,
  `rp_profile`. **This is what B.22 Purlins needed and never had.**
* **crane runways** — `Kranschienen_Form_A`, `krupp_zr/ztg/zth`, `kbk_kran`
* **cast-in channels** — `halfen_hl/hm/np/p`
* **bent sheet** `Kantteile` · **decking** `Steel Deck` · **stairs** `stair`
* **curtain wall** — `hueck_*`, `schueco*`, `jansen_*`, `wicona*`

### `bendshape` — B.8.2 Bent Shapes

`PsCreateBendShape`. **The only creator with `SelectWeldSections`** — the straight creator has
four selectors, this one has five (measured identically in .NET and COM).

```
bendshape name="HE 200 B"  pts=0,0,0;0,2500,0;2000,4000,0     polyline path
bendshape name="RO 88.9x5" circle=1500                        ring
bendshape name=...         helix=r,angle,rising,resolution[,left]
bendshape kind=weld catalog=I-Profile name=I950x300x30 pts=... welded plate girder
```
plus `catalog` `kind` `rot` `refaxis` `layer` `handle`.

⚠️ **≥ 3 path points are required.** Two points create nothing — identical section name, 2 fails,
3 succeeds.

⚠️ **`handle=` (`ConvertFromPolyline`) does not follow arcs.** It builds a shape, and the shape
is not the path: a 90° bulge left a vertex **650 mm outside** the result's bounding box, and
`Update()` on the polyline changes nothing. Every call therefore reports

```
pathfit=ok                                          <- straight pts= paths
pathfit=MISMATCH 1/3_vertices_outside_by_650mm      <- the bulged polyline
```

**Read `pathfit` on every `bendshape` call.** A route that silently produces different geometry
from the one asked for is worse than one that refuses.

---

## ⚠️ `dumpmodel` was blind to plates and bolts until 10/08/2026

```
before:  shapes=349  plates=0    bolts=0    other=152  err=357
after :  shapes=349  plates=178  bolts=179  other=152  err=0
```

Every plate and every bolt was written as an `ERR` row. **If you read a `dumpmodel` result from
before this date, its plate and bolt counts are zero and the parts were there.** `err=` is not
noise — open it.

`PLATE` and `BOLT` rows now also carry a **world bounding box** as their last column: a plate's
`InsertPoint` reads `0,0,0` and its polygon is in **local** coordinates, so without it a plate has
no position at all.

## Plate weight — safe route vs lethal route

⛔ **`PsPlate.computeObjectWeigth(bool)` kills AutoCAD** — process gone, no exception, no dialog.

✅ **`op=props` reads the weight safely**, through `PsObjectProperties`:
`props handle=<plate>` → `wt=117.75` for 1000×500×30 (0.015 m³ × 7850).

### Gratings (B.9.3)
* `plate9 grid=1 [griddir=x,y,z]` — the flag **sticks and is readable**: `DisplayFlagsLong`
  gains bit **8192** and `PitchLineMode` becomes **True**.
* `Ks_ComGlobalSettings.PlateRasterWeightReduction` is the settings/plate percentage
  (*Raster* = grating), **shipped at 10 %**.
* ❌ **It does not move the object's weight** — 117.75 kg at 0 %, 10 % and 35 % alike. The object
  carries the gross figure; the reduction belongs to the parts list.
* `PsCreatePlate` has **no grating-database selector**; `Data/Plates/ImpGrating.mdb` and
  `Platten-Bleche-Roste.mdb` are dialog-only.

---

## `solid` — `hull` and `rotate` both work (closed 10/08/2026)

Both sat on THE CEILING under *"never fairly tested"*. Both build.

### `kind=hull` — use `dpts=`, not `pts=`
`CreateHull` takes `SetPoints(PsDataPointArray)`. The polygon route was the wrong input.

```
solid kind=hull dpts=-400,-300,0;400,-300,0;400,300,0;-400,300,0;-100,0,600;100,0,600
   ->  800 x 600 x 600, exact
```
⚠️ **A perfect box creates nothing.** Jitter the corners 10 mm and the same eight points build —
exactly-planar faces are degenerate. Use `kind=box` for boxes.

### `kind=rotate` — three rules
1. **The polygon is LOCAL 2D (`x,y` only).** `200,0,0;500,0,0;500,0,400;200,0,400` collapses to a
   zero-area line and fails. Write `200,0;500,0;500,400;200,400`.
2. **The axis is in WORLD coordinates** while the polygon is local — pass it through the insert
   point or the profile sweeps around the world origin (first success measured **761 000 mm**).
3. **The axis must lie IN the profile's plane.** A Z axis is perpendicular to the local XY plane
   → degenerate revolve. Geometry, not a refusal.

```
solid kind=rotate pts=200,0;500,0;500,400;200,400 at=370000,-9000,0 \
      axis1=370000,-9000,0 axis2=370000,-8000,0     ->  1000 x 400 x 1000
```
⚠️ **`rev` is ignored** — 90, 180 and 360 give identical solids.

---

## ⚠️ The two APIs can DISAGREE — `.NET` is the complete one

Known: when .NET will not bind to an existing entity, COM does (`doc.HandleToObject`). The reverse
caveat, found 10/08/2026 in B.13:

```
.NET  Bentley.ProStructures.EdgeLayout   kUnknownEdge kFacet kRadius kRounded kInverted kFold kNotch
COM   PSCOMWRAPPERLib.KsEdgeLayout       kUnknownEdge kFacet kRadius kRounded kInverted kFold
```

**`kNotch` exists only in .NET.** Same enum name, different members. **When they disagree, trust
.NET** — COM rescues .NET on binding and can be *behind* it on content.

### `edgechamfer layout=` — the six kinds the manual promises and never names
`1 kFacet · 2 kRadius · 3 kRounded · 4 kInverted · 5 kFold · 6 kNotch`
All six verified: applied to six plates, read back as `layout=1…6` with top and bottom both set.

`PsEdgeChamfer` is a **data payload, not a creator** — no `Create`/`insert`/`writeTo`; it is
assigned to `em.PlateBreakEdge`. And `Min. Radius`/`Max. Height` are **dialog-side validation
only**; nothing in the API surface exposes them.

---

## Drilling — what the layout string really does (B.14, verified 10/08/2026)

`drillfield handle= dia= x= y= at=` feeds `PsDrillObject.SetLinearHoleField`, and it takes the
**dialog's own layout syntax** — verified by spacing, not by hole count:

```
x=3*70          ->  gaps 70, 70          uniform
x=2*60,200,1*   ->  gaps 60, 200         non-uniform, honoured in full
```

Syntax: `Number1*Pitch1, IntermediatePitch, Number2*Pitch2, …`
Length only ⇒ leave the cross field empty. Cross only ⇒ you **must** write `1*` in the length.

### ⭐⭐ `W` — inherit the section's marking gauge, never invent bolt spacing

`W` in place of a pitch means *the shape's own predefined marking gauge*. It works through the API
and needs no dialog. Proved by contrast:

| beam | `y=` | measured |
|---|---|---|
| HE 300 B | `2*W` | **120 mm** |
| IPE 300 | `2*W` | **80 mm** |
| HE 300 B | `2*100` (control) | 100 mm |

Applies to **shapes**, not plates. If a section has no gauge defined the software asks — so use it
on catalogue sections.

## ⛔ `edgecheck` is DISABLED — it killed AutoCAD

`PsVolume.checkHoleEdgeDistance` is the manual's admissible-edge-distance check and it **ends the
process**: no exception, no dialog, `EB_TIMEOUT` and an empty `Get-Process acad`. The op remains,
refusing, so nobody rediscovers it. **The edge-distance table is dialog-only in practice.**
See `knowledge/learning/findings/LETHAL-CALLS-do-not-invoke.md`.

## ⚠️ `plate9 mode=rect` places from `at=`, not `p1=`

`p1` is a valid key for the op (the `poly`/`pts` modes use it) so the strict-parameter guard used
to accept it **in silence** — nine plates built on 10/08 stacked up at the origin while their
labels pointed at empty strips. **Valid FOR THE OP is not valid FOR THE MODE.** `mode=rect` now
refuses `p1/p2/p3/pts/radius` and creates nothing.

---

# Back-filled 10/08/2026 — items the part-B audit produced that never reached this file

*Found by an independent verification pass, not by me. Three chapters' worth of findings had gone
into the running audit record and into `SKILL.md` and stopped there.*

## B.12 — the cope, and two things that will cost you an hour each

⚠️ **A cope that does nothing may be a GEOMETRY error, not an API refusal.** Building the beam so
it **butts against** the support gave `polyCuts=0` on every attempt; running the beam **through**
the support gave `polyCuts=1` — same call, same parameters, same template.

⛔ **`edge=2` (Corner Layout = Access Holes) and `edge=0` (bevelled Edge) are indistinguishable
from the API.** Built as a contrast pair:

```
holes  edge=2  beam=140A  polyCuts=1   ext 400010.5,-6075,1350 ; 403000,-5925,1650
bevel  edge=0  beam=140B  polyCuts=1   ext 405010.5,-6075,1350 ; 408000,-5925,1650
```

Identical extents to 0.1 mm and identical `mods` counts. ⇒ **`polyCuts=1` proves a cut, not its
shape.** A volumetric probe was considered and **deliberately not run**: placing it needs to know
where the cut *ends*, and **no op exposes a polyCut's polygon** — a probe on a guess proves nothing
either way.

⇒ ⭐ **Missing capability, named: a reader for a polyCut's polygon.** Until then the rathole
question stays open. The contrast pair is kept in the model at x 400 000 / 405 000, labelled.

*(The other route — a shaded view — needs `VSCURRENT`, which is **not** in the `CmdAllow`
allowlist. That allowlist is a safety control and is not widened without Amir's explicit approval.)*

## Three ops that exist and were never catalogued here

| op | what it is for | why it was kept |
|---|---|---|
| **`layerprobe`** | B.1. Creates plates under `UseCurrentLayer(true)` vs `(false)` with the current layer deliberately set to junk, and reports where each landed | it is the evidence that ProSteel's automatic layer control works from the API, and that the 88 strays were self-inflicted |
| **`gridaxes`** | B.6.7. Builds a fresh `PsGrid`, sets `Length`/`Wide`, adds user axes, `insert()`s, then **reads every axis back** | it is the evidence that the route fails — and it does not trust `addUserXaxis`' boolean |
| **`acisref ucs=origin;xaxis;yaxis`** | B.11. Builds a real `PsMatrix` via `SetCoordinateSystem` and calls `SetInsertMatrix` before `Create` | it is what disproved the `massProp=false` explanation. `ucsSet` in the reply confirms the call was made |

## B.1 — the ten ops that still take no `layer=`

Their creators were **assumed** to behave, not measured: `stiffener`, `weld`, `bend`, `bendtwo`,
`grid`, `workframe`, `frame`, `boltfield`, `boltparts`, `threadedrod`.

⇒ After B.1's finding this is **probably fine** — `UseCurrentLayer(false)` with no `SetLayer`
yields the part's own layer, and connection classes were always correct. But *probably* is not
*measured*, and this list is here so the distinction stays visible.

## B.13 — a loose thread, recorded and not chased

`PsEditPlateModification` exposes **`DisplayAsRaster`** — *Raster* again, the same word behind
B.9.3's grating flag. It may be the read/write route for that flag on an **existing** plate, where
`SetGrid` only offers it at creation. **Not tested.**

---

## `stylelist` — B.15.4, the style selection list (added 10/08/2026)

```
stylelist action=list|sync|reload|readfile|append|moveup|movedown|delete  type=0..4
          [name=] [index=] [confirm=]
```
`type`: **0 bolt** · 1 weld · 2 posflag · 3 koteflag · 4 universal.

⭐⭐ **`action=sync` is the one that matters for fabrication.** The manual: *"the styles are
stored as **objects in the drawing**; when the style definition is modified on the hard disk the
modifications are **not transferred** to the internal objects."* ⇒ **A bolt style is frozen into
the model at the moment it is used.** Editing DIN7990 on disk changes nothing in an existing
drawing until `stylelist action=sync` runs.

⚠️ `action=delete` needs `confirm=DELETE` — the manual says it deletes without asking, and a style
is referenced by every bolt that uses it.

⭐ **`Initialize()` leaves `Count` at 0; `ReadFromFile()` fills it** — 0 → 27 on the same object.
If a style list looks empty, it has not been read from disk yet.

## ⭐⭐ The indexer rule — four instances, settled

`get_ParentFlangeIndex(int)` · `get_WeldStyleName(int)` · `get_BoltStyleName(int)` ·
`get_Entry(short)` / `get_Index(string)`.

> **When a `String` or `Int32` property looks like it should be a list, it IS an indexer — and the
> type dump prints it as a plain property. Let the compiler tell you.**

⛔ And the matching write-side trap: **`BoltStyle`, `BoltType` and `Diameter` are WRITE-ONLY.**
You can set a style and never read back which one is set.

---

## `dbase` — the connection databases (B.17.3, added 10/08/2026)

```
dbase file=<path.dbf> [row=N] [max=N] [out=eb_dbase.txt]
```

ProStructures ships one dBASE file per connection macro under
`…\ProStructures Ss6 R1\AutoCAD 2015\Prg\Plugins\<macro>\<macro>.dbf`:

| macro | records | fields |
|---|---:|---:|
| `AECChute` | 59 | 19 |
| `BasePlate` / `BasePlateChinese` | 56 | 11 |
| `BeamBeamClamp` | 5 | 2 |
| `PipeStrap` | 33 | 9 |
| `PurlinBeamBraceFly` | 60 | 19 |

⭐⭐ **They speak the drilling API's language.** `BasePlate.dbf` stores `HOLEX = "2*100"` and
`HOLEY = "1*"` — the same layout string `drillfield x= y=` takes. One vocabulary across the
connection databases and the drilling API.

⭐ This is the manual's route to a **company connection standard**: *"create a database with
frequently utilized and maybe company-specific connections, which are then always available to
all program users within your company."*

⚠️ **READ ONLY on purpose.** `PsDBaseDatabase` has `PutRecord`/`AppendNewRecord`, but these files
live in `Program Files` and define how connections get built. Full map:
`knowledge/CONNECTION-DATABASES.md`.

## `conn … rotated=1` — set, confirmed, and ignored

`PsStandardPlateLinkData.PlateIsRotated` is the dialog's *Rotate Connection*. It **writes and
reads back `True`** — and the connection is identical either way (the same eight plates,
140×130×10 ×2, 481.6×8×202.3, 10×65×138.3 ×2, 140×260×10 ×2, 300×290×10).

⇒ Another **parameter that never arrives**. ⚠️ Its other route, `connset`'s generic reflection
setter, is **QUARANTINED** — it crashed AutoCAD four times and once left the drawing unsaveable.
Do not un-quarantine it to reach a property that does nothing.

---

## ⛔ `dia=` ON A CONNECTION DROPS THE BOLTS — B.20 audit, 10/08/2026 (v156 → v160)

**Measured on four fresh bays, `shearplate`, `default/Standard`:**

| | plates | bolts |
|---|---:|---:|
| `t=10 dia=16` | 2 | **4** ✅ |
| `t=10` **`dia=22`** | 2 | **0** ⛔ |
| `t=18 dia=16` | 2 | **4** ✅ |
| `t=18` **`dia=22`** | 2 | **0** ⛔ |

**The diameter is the killer. The thickness is irrelevant.** `HoleDiameter` names the **hole**;
the bolt comes from **`BoltStyle`**. A ⌀22 hole against the default `8.8S` has no bolt to match,
and the connection **drops the bolt instead of refusing** — plates made, holes drilled, nothing
through them. That is iron rule 1 broken, silently.

> ⭐ **This is B.15's ~400 failed bolts arriving through a connection class.** There it was a
> *grip* with no row in the table; here it is a *diameter* with no row in the style.

**Rule: choose the diameter through the STYLE. Leave `dia` = bolt + workloose.**

**Guard added (v160):** `shearplate` returns **`EB_ERR … ⛔IRON-RULE`** when it creates plates and
zero bolts, naming the likely cause. Verified: fires on `dia=22`, silent on `dia=16`. The geometry
is **not** rolled back — deleting a caller's parts behind their back is worse — it is reported.

⚠️ **`webangle`, `splice`, `haunch`, `connbase` take the same parameter and were NOT tested.**
Assume nothing about them; sweep `dia` when their chapter comes up.

---

## 🔗 `connscan` — two defects fixed, and the fix is what found the structure (v157 → v158)

**Before:** `parts=140688488454768,0` — raw 64-bit ObjectId pointers. Unusable by any other op,
different every session, and the `0` read like data.
**After:** handles, with an empty slot printed as `-`:

```
MEMB  15EE  0  parts=1610,1611 (2/2 filled)  bolts=1612,1613,1614,1615 (4/4 filled)  target=15ED
MEMB  15EA  0  parts=1600,-    (1/2 filled)  bolts=1601,1602,1603,1604 (4/4 filled)  target=15E9
MEMB  15EC  0  parts=-,1608    (1/2 filled)  bolts=1609,160A,160B,160C (4/4 filled)  target=15EB
```

⇒ ⭐⭐ **`LinkObjectCount` is always 2 and the two slots are the two sides of the web.** `pos=0`
fills slot 0, `pos=1` fills slot 1, `pos=2` fills both. The dialog's `left / right / Both` **is**
the data structure — invisible until the empty slot stopped printing as `0`.

### ⚠️⚠️ `GetXxxLinkData() != null` IS NOT A TYPE TEST

Every one of `GetBasePlateLinkData` / `GetStiffenerLinkData` / `GetSpliceJointLinkData` /
`GetShearPlateLinkData` / `GetWebAngleLinkData` / `GetCopeLinkData` **returns a live object on
every link, full of zeros.** One shear-plate joint was therefore tagged
`t17/BASEPLATE/RIB/SPLICE/SHEARPLATE/WEBANGLE/COPE` and the whole type histogram was noise.

**The type is `lk.Type`, and it always was.** Now printed as `17/kConnectWithSchearPlate`, and a
block of zeros is not printed at all — a block of zeros is not information, it is a false reading.

### The joint's link topology — who owns what

```
connected shape   type=17/kConnectWithSchearPlate     parts=<plates>  bolts=<bolts>  target=<support>
support shape     type=12/kConnectedBy                empty                          target=<connected>
each plate        type=18/kSchearPlateConnectionLink  empty                          target=<connected>
each bolt         NO LINK AT ALL
```

⇒ **The connected shape owns the joint** — the only member holding the roster, the only one
pointing at the support. ⚠️ **A bolt carries no link, so it cannot be traced to its connection
from the bolt's side.**

---

## ⭐⭐⭐ THE PART'S NAME STATES ITS MILL PRODUCT — B.20, and it applies to every flat any connection makes

| `name` | means |
|---|---|
| **`FL 150x10`** | an entry in **`DIN FLACHEISEN`** — flat bar, widths 10…150. **Stock.** |
| **`BRFL 160x15`** | an entry in **`DIN_BREITFLACHEISEN`** — wide flat, 160/180/200/220/240/250…1200. **Stock.** |
| **`Plate 165x10`** | ⚠️ **in neither catalogue. The part will be cut from plate.** |

⚠️ **`key` and `cat` are identical in all three** — every one reads `key='165X10' cat='DIN.DIN_FLACH'`.
`DIN.DIN_FLACH` is a stored family label, not a resolvable catalogue (`dumpcat DIN_FLACH` returns
nothing; the lookup names are `DIN FLACHEISEN` and `DIN_BREITFLACHEISEN`). **Only `name` tells you.**

**Proved by prediction**, three bays, each name declared before the run: `holevert` 110 → depth
150 → `FL 150x10` ✅ · 120 → 160 → `BRFL 160x10` ✅ · **125 → 165 → `Plate 165x10`** ✅.
165 lies *inside* the wide-flat range and is not a stock width ⇒ **the test is catalogue
MEMBERSHIP, not a size range.**

⚠️ **`shapeinfo catalog=… name=…` CANNOT be used as an existence test** — `GetSectionPolygon`
generates the rectangle from the name and answers for `135x10` and `210x10`, which no catalogue
holds. **`dumpcat` enumerates the real names; that is the authority.**

### Why it matters, and where it bites

A shear plate derives its depth from the bolt count — **135 for two rows, 210 for three** — and
**neither is a stock width.** Move a hole 5 mm and a `Plate` becomes an `FL`. ⚠️ Two of the four
plates in B.20's own band are `Plate`, not bar.

⭐ **A web angle cannot have this problem** and the contrast is the point: `L 90x9` is a
**catalogue** section cut to length (`L=135` / `L=210`, section fixed), while a shear plate is a
section **invented per joint** at a fixed 70 mm stick-out (`L=70`, `W=135/210`).
**The two chapters do not share a derivation rule.**

⛔ **Not claimed:** what a parts list prints. The object's name is measured; the list is part C.

---

## `shearplate` — the ordinals, and the read-back that works

```
pos=0  one plate, +9.30 from the web centre     M16 x 45
pos=1  one plate, -9.30                          M16 x 45
pos=2  TWO plates, both sides                    M16 x 55   <- the bolt grew by itself
pos=3,4,5   nothing built, and Create() returned True
```

⭐ **`pos=2` is `Both`.** Ten more millimetres of packet moved the bolt one step up the table with
no bolt parameter touched — *bolts follow the packet.*

**Reading a connection's own settings back — two getters that look alike, one works:**

| call | result |
|---|---|
| `PsLogicalLink.GetShearPlateLinkData()` on any joint member | ⛔ `PlateThickness=0` |
| **`PsShearPlateConnection.GetLink().GetLinkData(0)`** | ✅ `t=18 pos=2 nV=2 nH=2 dV=140 dia=22` |

Index 0 is the live one; 1 and 2 are zeroed. ⚠️ **Only straight after `Create()`** — there is no
binder to an existing joint (the same structural dead end as `PsGrid` in B.6).

**`GetPlateId(i)` still returns 0 on every index.** It is not the route; the link is.

---

## 🔨 BUILDING THE PLUGIN — the exact command (recorded 10/08/2026)

Four `/r:` references, and **two of them are easy to miss**: the source uses
`PSN_HollowShapeBracing` (B.24), and it does **not** need the COM interop assembly.

```powershell
$csc  = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$acad = "C:\Program Files\Autodesk\AutoCAD 2015"
$prg  = "C:\Program Files\Bentley\ProStructures Ss6 R1\AutoCAD 2015\Prg"
$plug = "C:\Users\User\Desktop\EB PROSTEEL AGENT\app\plugin"
& $csc /nologo /target:library /platform:x64 /out:"$plug\EBAgentApi<N>.dll" `
  /r:"$acad\acmgd.dll" /r:"$acad\acdbmgd.dll" /r:"$acad\accoremgd.dll" `
  /r:"$prg\ProStructuresNet.dll" /r:"$prg\PSN_HollowShapeBracing.dll" `
  "$plug\EBAgentApi<N>.cs"
```

Bump **five** tokens in the copied source — `EB_RUN<N>` (×2), `ApiCmds<N>` (×2), `EBApp<N>` (×2) —
then set `DLL` and `RUN_CMD` in `app/eb_api.py`, which is the **only** authority for the version.

---

## ⛔⛔ A CONNECTION CAN SHIP WITH NO BOLT STYLE — B.21 audit, 10/08/2026 (v160 → v162)

**Both shipped splice templates carry `BoltStyleCRC = 0`.** With no style the connection drills
its hole fields and inserts **nothing**. Three joints stood in the reference model, drilled and
unbolted, from 09/08 until this audit — because the chapter counted plates and hole fields and
**never counted bolts**.

```
splice … (as shipped)          ->  4 plates,  0 bolts,  2 hole fields   ⛔
splice … boltstyle=DIN7990     ->  4 plates, 16 bolts                   ✅
```

⚠️ `PsSpliceJointLinkDataMgd` is the one connection class of the three with **no `BoltStyle`
string** — only `BoltStyleCRC` — so a name has to be resolved through `PsObjectStyleList` first.
`splice boltstyle=<name>` does that (`DIN7990 → -1614854285`); `boltstylecrc=` takes a raw value.

⇒ **FIRST THING TO CHECK ON ANY CONNECTION CLASS: what bolt style does the template carry?**
`webangle`, `haunch` and `connbase` have **not** been checked.

---

## ⚠️⚠️ A WELD FLAG OCCUPIES A BOLT SLOT

The welded splice reports `BoltObjectCount = 32`. All 32 objects sit on layer **`PS_Weld`** —
they are `Ks_WeldFlag`.

> ⭐⭐ **`getBoltObjectId` / `BoltObjectCount` mean FASTENERS, not bolts.** A link reporting 32
> bolts can hold **zero** bolts — the exact shape of an iron-rule violation that **passes** a
> bolt-count check.

**Never test the iron rule by counting link slots.** Open each fastener and classify it
(`CountRealBolts` in the plugin does this: class/layer containing `Weld` → weld flag, `Bolt` →
bolt). A deliberately **welded** joint legitimately has no bolts and no holes; a joint that was
**drilled** and has no bolts is the violation. Those are different, and only the class tells you.

---

## ⚠️ `dumpmodel`'s `insert` IS (0,0,0) FOR EVERY PLATE AND EVERY BOLT — use `center`

Measured over the whole model: **241 of 241 plates and 305 of 305 bolts** report
`InsertPoint = 0,0,0`. B.9's audit fixed the *plugin* to emit a world bounding box for exactly
this reason; the **Python client never parsed it**.

⇒ Any code locating a part through `els[i]['insert']` put it at the origin, and any "what is in
this region" question answered **zero bolts, everywhere in the model** — a false negative that
looks identical to a true one.

```
bolts in the splice band via insert[] : 0
bolts in the splice band via center[] : 62
```

**`eb_api.dumpmodel()` now returns `bbox` and `center` on plate and bolt rows. Use `center`.**
⭐ A fix that stops at the producer is half a fix.

---

## `splice` — the measured parameter meanings

| | |
|---|---|
| **`toplap` / `sidelap`** | ⭐ **HALF-lengths. Plate length = 2 × lap.** Predicted 200 from `toplap=100`, measured 200 on all four plates. **Only a WELDED splice obeys them** — a bolted splice's plates are set by the bolt pattern (298 in the band) |
| `weldflange` / `weldweb` | no bolts and no holes; `Ks_WeldFlag` objects instead, 8 per plate |
| six position flags | `topout topin downout downin webleft webright` — and **six flags give EIGHT plates**, because the web crosses the inner flange face and each inner plate becomes two strips |
| `boltstyle` | ⭐ **required for a bolted splice** — see above |

**What a splice orders** (B.20's naming rule): flange plates come out `FL 150x10` — stock — but
the web plates are `Plate 128x10` and the inner strips `Plate 39x10`, **neither of which is a
stock flat**. The web plate's width is the web depth less the flange thicknesses, so it hits a
stock width only by accident.

---

## ⭐⭐⭐ A DRILL FIELD DOES NOT KNOW WHERE THE SECTION'S METAL IS — B.22, 10/08/2026

A `kBoltet` purlin connection on a **U160** with the shipped `HoleDistanceSupport = 56` puts its
hole pair at **±28** from the purlin axis. The channel stands with its 160 mm depth vertical, so:

| offset | what the drill meets | depth |
|---|---|---|
| **−28** | the **WEB, stood on edge** | **160.00 mm** — a bolt clamps a plate seen edge-on |
| **+28** | the bottom **FLANGE** | **8.82 mm** — the one you bolt through |

⇒ Four holes, four *matched* holes, and **two bolts**. The bolt count was never wrong. **The gauge
was.** *(And the 09/08 note's "4 girder / 2 purlin" was a miscount — the holes are 4 and 4 and
exactly coaxial.)*

**The fix, measured:**

```
HoleDistanceSupport=20  ->  4 holes, 4 BOLTS, depths 11.86 and 10.26   <- both on the flange
HoleCountSupport=1      ->  2 holes, 2 BOLTS, depth 11.06              <- on the axis
HoleDistanceSupport=-56 ->  silently normalised to +56
```

⭐ **For a U160 purlin on an IPE girder: `HoleDistanceSupport = 20`.**

⚠️ **Do not generalise the sign.** The web fell on the `−` side for this section in this build; the
orientation may follow the insertion direction. **Read the hole depths and take the shallow line** —
the depth measures the metal, and it even shows the DIN channel's taper (11.86 / 11.06 / 10.26 /
8.82 as you move outward).

⇒ ⭐⭐ **The general rule: on any open section — channel, angle, Z or C purlin — check the hole
DEPTHS before trusting a hole field. A depth equal to the section's overall size means the drill
went through the void, and that hole will never be bolted.**

---

## ⚠️ ERASING A PART DOES NOT ERASE ITS BOLTS

Deleting two purlins left their two bolts standing — **bolts connecting nothing**, iron rule 1
from the other side. It surfaced only because the station then counted 6 bolts where 4 were
expected.

**After erasing any bolted part, sweep the region for orphan bolts** (`dumpmodel` + `center`, the
B.21 fix) and erase them too.

---

## ⚠️ A PARAMETER THAT WAS NEVER SENT IS NOT A PARAMETER THAT DOES NOTHING

A sweep of three `HoleCountSupport` values returned three identical results — which reads exactly
like *"this property has no effect."* The `set=` had been left off the call entirely.

**Every op echoes what it applied. Read the echo before drawing a negative conclusion.** This is
B.16's count trap moved one step earlier: there, two values gave the same count; here, the value
never arrived at all.

---

## Bolt styles on the connection templates — measured, 10/08/2026

`BoltStyleCRC = 0` means **no bolts at all** (B.21). Checked so far:

| class | templates | style |
|---|---|---|
| **splice** | both | ⛔ **`BoltStyleCRC = 0`** — pass `boltstyle=` or the joint is drilled and empty |
| **purlin** | all three | ✅ `8.8S` / `DIN7990`, real CRCs |
| shear plate | `default/Standard` | ✅ `8.8S` |
| web angle · haunch · base plate | — | **not checked** |

---

## ⭐⭐⭐ `PsTransaction.GetObject` — THE BINDER, AND IT IS NOT ON THE CLASS (B.23, 10/08/2026)

Every chapter that asked *"can I bind this class to an existing object?"* asked the **class** —
does `PsGrid` have `SetObjectId`, does `PsShearPlateConnection` have `readFrom`. The answer was
always no, and **the question was in the wrong place.**

```
Bentley.ProStructures.Drawing.PsTransaction        (using Bentley.ProStructures.Drawing;)
   Boolean GetObject(Int64 Id, PsOpenMode Mode, T& obj)      -- 57 OVERLOADS
```

`PsGrid` · `PsGussetConnection` · `PsEditConnection` · `PsWeldFlag` · `PsWeldFlagStyle` ·
`PsPositionFlag` · `PsPositionFlagStyle` · `PsBoltStyle` · `PsUniversalStyle` · `PsShape` ·
`PsPlate` · `PsBolt` · `PsAssembly` · `PsBracing` · `PsPortalFrame` · `PsHandrail` · `PsLadder` ·
`PsStairs` · `PsCircularStairs` · `PsJoist` · `PsTruss` · `PsWorkframe` · `PsBendPlate` ·
`PsBendShape` · `PsArcPlate` · `PsArcShape` · `PsPrimitive` · `PsSolidReference` ·
`PsPartFamilyData` · `PsDwgPartList` · `PsMesh` · the whole concrete/rebar family …

**Op: `bind handle=<h> cls=grid|gusset|plate|shape|editconn|weldflag|posflag`.** Measured:

```
2F1 Ks_Grid  -> [name='A' len=24000 wide=15000 type=kRectangle lenDiv=4 wideDiv=3 xDesc=3 yDesc=4]
456 Ks_Plate -> [name='B9_RECT 400x250x12' L=400 H=12 verts=5 rect=True]
2C6 Ks_Shape -> [key='HE300B' cat='DIN.DIN_HEB']
```

### ⛔⛔ TWO RULES, BOTH PAID FOR

**① READ ONLY. Writing through a bound object killed AutoCAD.**
`PsGrid.addUserXaxis` on a bound grid: dead. Isolated — saved immediately before, one call in its
own run — **dead again.** It is the third entry in `LETHAL-CALLS-do-not-invoke.md`, and unlike the
first two it is not a `check*`/`compute*` method but an **ordinary mutator**.
⇒ **Reading is safe and proven. Every MUTATOR on a bound object is suspect.**
⚠️ `getUserXaxis`/`getUserYaxis` are **UNKNOWN, not safe** — the add died before they ran.

**② `GetObject` DOES NOT TYPE-CHECK.**
```
bind <a Ks_Shape> cls=grid  ->  grid=True  [len=281474976713490  wide=NaN  xDesc=234]
```
It returns **`True`** and hands back a reinterpreted pointer. A read gives nonsense that looks
like data; a write would corrupt the object. **Always check the entity's real class first** —
`bind` now refuses a mismatch.

### 🛑 What this retracts

B.6 concluded and put on THE CEILING that *"`PsGrid` … has **no binder of any kind** … the two
halves never meet in the API."* **Withdrawn.** They meet — at a call that kills the session.
B.6.7 stays closed for that reason instead.

---

## ⛔ RECOVERY AFTER A CRASH — corrected 10/08/2026

**Pass the DRAWING on the command line. Never `/t <template>`.**

A template creates a new `Drawing1`, and a new drawing makes ProStructures raise a modal
**"Measurement Unit"** prompt (*"persistent and cannot be changed at a later date"*). It blocks
everything and **cannot be dismissed from code** — `BM_CLICK` and `WM_COMMAND`/`BN_CLICKED` to its
Metric button were both ignored, six attempts. Opening an existing drawing never asks, and no
second document is created, so nothing has to be closed afterwards.

```powershell
Stop-Process -Name acad -Force ; Start-Sleep -Seconds 5
Start-Process 'C:\Program Files\Autodesk\AutoCAD 2015\acad.exe' -ArgumentList `
  '"<full path to the .dwg>"','/p','"…\ProStructures_SS6.1ACAD_E001_409.arg"', `
  '/ld','ProStructuresLoader.arx' `
  -WorkingDirectory 'C:\Program Files\Bentley\ProStructures Ss6 R1\AutoCAD 2015\Dwg'
```

| dialog afterwards | do |
|---|---|
| `AutoCAD Error Report` | `WM_CLOSE`. ⛔ **never *Send Report*** |
| `Error Report - Cancelled` | click **OK** |
| `Drawing Recovery` | click **Close** — ⚠️ **do not recover**, the disk file is the good one |

⚠️ **`Get-Process acad` IS NOT THE TEST.** A crash can leave the process listed and
`Responding=True` while the op never returns. **`modal_dialogs()` is the test** — an
*"AutoCAD Error Report"* window means it died.

---

## ⭐⭐ A `False` YOU HAVEN'T READ BACK IS NOT A REFUSAL — B.24, 10/08/2026

`PsBracing.insert()` had been recorded as refusing under six configurations, closed as *"the
product is interactive by design"*. The op now reads the system line back before `insert()` runs:

```
cross=0   insert()=False   line1=(NaN,NaN,NaN)->(NaN,NaN,NaN)   line2=(0,0,0)->(0,0,0)
cross=1   insert()=False   line1=(NaN,…)                        line2=(NaN,…)
```

⭐⭐⭐ **The untouched line reads a clean `(0,0,0)`. The one that was SET reads `NaN`.** The getter
works; **setting the geometry produces the garbage**. `insert()` was never given a system line —
it was refusing a bracing with no geometry, in all ten configurations now tested.

⚠️ **On the same object, `crossedMode`, `cat`, `size` and `shapeType` all round-trip perfectly.**
So it is **specifically the `PsPoint` setters** that fail. Excluded by test: GC lifetime
(`GC.KeepAlive` past the call), point provenance (read the object's own point out, mutate in
place, write back), the missing second diagonal, and single-bar/welded/no-gusset variants.

⇒ ⭐ **THE RULE: before concluding that a creator refuses, READ BACK WHAT YOU GAVE IT.** A `False`
tells you the call failed; it never tells you the call was well formed. This is the same shape as
B.12's *"`Create()` lies in both directions"*, one step earlier in the chain.

⚠️ **What is NOT known:** whether the setter stores nothing, or stores fine and the getter cannot
read back after a write. `listInformation()` is the named next test — a third channel independent
of both.

**The practical closure is unchanged:** a bracing cannot be built from code. But the two routes
fail for **different** reasons and must not be blurred — `PSN_HollowShapeBracing` is
**interactive** (proven: it parks at *"Choose support shape"*), while `PsBracing` **cannot receive
its geometry**.

### `bracing` op — new parameters
`crossp1=` / `crossp2=` — the second diagonal of a cross stay, i.e. B.24's whole `Cross Bracing`
field, previously unreachable from code at all. `ptmode=new|api`. Both system lines and
`crossedMode` are printed before `insert()` decides.

---

## ⭐⭐⭐ AN ANGLE'S MATERIAL IS AT THE EDGE OF ITS ENVELOPE, NOT AT ITS AXIS (B.25, 10/08/2026)

**The seating rule, and what happens when it is not applied.**

An `L 90x9` occupies a **90 × 90 envelope centred on its axis**. Its two legs are at the *edges*
of that envelope. So a gusset that lands on the axis lands **in the air between the legs**.

```
axis  =  gusset face  ±  45          (for an L90x9: half the envelope)
```

**Measured in B.25's braced bay, where the rule was NOT applied:**

```
gusset F72   y  −55.0 … −65.0     a 10 mm plate, centred on the rod axis at −60
rod    F76   y −105.0 … −15.0     the L 90x9 envelope, also centred on −60
             drilled leg           y  −96 … −105
                                   −65 → −96  =  31 mm OF AIR
```

⇒ **All eight bolts in that bay read `GAP-IN-PACKET`, 31 mm each.** `BOLT-NO-HOLE=0` and the bolt
lengths are correct *for the packet as modelled* — the iron rule is satisfied and the joint is
still unbuildable. **A bolted lap with an air gap puts the bolt in bending.**

⚠️ ⭐ **`vfy_fit` is the instrument that catches it**, and it separates the two very different
meanings of a large spare:

```
GAP-IN-PACKET  the plies do NOT touch   <- a scheme error
OVERSIZED      the bolt is too long     <- a bolt error
```

**A bolt-length check alone would have reported these eight as fine.**

> ⭐⭐ **The pattern, three times in this audit:** B.19's *"likely"* that had become certain,
> B.24's `NaN`, and now B.23's seating rule — derived on 09/08 and not applied to a bay built on
> 09/08. **A true finding that does not travel is worth nothing, and no guard can see it** — a
> stoplist catches contradictions, not silence. Only reading the neighbouring chapter does.

---

## `PsBracing` — 13 configurations, and what they actually prove

B.24 and B.25 between them tried thirteen. **Every one fed `insert()` a system line that reads
back `(NaN,NaN,NaN)`** — see the B.24 section above. So:

* ✅ **`insert()` is unusable** — that conclusion is safe and repeatedly measured.
* ⚠️ **The individual hypotheses are NOT refuted** — the static flag (`setDynamicStatus(false)`),
  the XZ-vs-XY plane, the layout, the welded and no-gusset variants. **A lead is not refuted by a
  test that could not have succeeded whatever its merit.**

⭐ And the UCS question is settled by the signature, not by experiment:
**`insert(Origin, X_axis, Y_axis)` takes the plane AS ARGUMENTS.** The active UCS was never going
to matter.

⇒ **Build a static bracing by COMPOSITION** — the manual says to, and B.25's six separable buttons
are the design: a shape along the system line, `PsDrillObject` at each end, gusset plates, then
bolt the drilled parts. ⭐ Measure the holes **inward from the rod's front edge**
(`Edge–1st Hole`, then `Hole–Hole`), **never spread about the joint centre** — doing the latter
puts one hole of each pair past the end of the rod and leaves an unfilled hole in the gusset.

⭐ **And shorten the rod deliberately** (`setShapeShorting`): B.25's dialog says the rod is cut
short *"thus the rod will be kept in tension"*. Nothing in the geometry hints at it.

---

## ⭐⭐ TWO FAILURES THAT LOOK IDENTICAL FROM OUTSIDE — B.26, 10/08/2026

A connection ignores the position you give it. There are **two completely different reasons**, and
only a read-back tells them apart:

| | symptom | read-back | what it is |
|---|---|---|---|
| **`PsBracing`** (B.24) | `insert()=False` | `line1=(NaN,NaN,NaN)` | ⛔ **the value never arrived** |
| **`PsHaunchLinkDataMgd`** (B.26) | parts in the wrong place | `InsertPoint=400000,20000,5000` — exact | ⛔ **the value arrived and is IGNORED** |

⇒ **Always read the parameter back before naming the failure.** *"It doesn't work"* covers both,
and they need opposite fixes.

### The haunch: parts are pinned to z = 0, absolutely

Five configurations, each on its own throwaway pair:

```
world at, column z 0..5000     eaves 5000   -> plates z=0
local at = 0,0,5000            eaves 5000   -> plates z=0
local at = 0,5000,0            eaves 5000   -> plates z=0
column drawn TOP-DOWN          eaves 5000   -> plates z=0    <- origin IS the eaves
column based at z=2000         eaves 7000   -> plates z=0    <- origin is 2000
```

**`x` and `y` are correct. Only the height is lost.** ⚠️ And the connection *does* know where the
joint is — it trimmed the rafter at the eaves every time (`3162.278 → 2385.67`). **The cut is
placed correctly and the parts are not.**

⇒ The only route left is to **build the corner with the eaves at z = 0 and move the assembly** —
untested, and a workaround rather than a fix.

---

## ⛔⛔ AFTER ANY SWEEP THAT ERASES PARTS, RE-RUN `vfy_fit` OVER THE WHOLE MODEL

A model-wide check found **`BOLT-NO-HOLE=12`** — twelve bolts through nothing, left behind by an
earlier cleanup **in this same audit**:

* the sweep filtered bolts by `els[i]['insert'][0]`,
* and `insert` is **`(0,0,0)` for every plate and bolt in the model** (found one chapter later),
* so the filter matched **no bolt at all**, silently, and reported success —
* while **erasing a part does not erase its bolts**.

Three separate facts, each individually known, combining into an iron-rule violation that nothing
detected until a **whole-model** check was run.

⇒ ⭐ **A band-local check cannot see what a band-local filter missed.** Verify globally after
deleting.

```
vfy_fit                     -> bolts=301 OK=261 BOLT-NO-HOLE=0 🧲 OVERSIZED=0
                               GAP-IN-PACKET=8   SHORT=32
```

⚠️ **`GAP-IN-PACKET` and `SHORT` are not the same problem.** A gap is a **scheme** error (the
plies do not touch — B.25); SHORT is a **bolt-length** error. `vfy_fit` separates them because a
large "spare" means one of two very different things.

---

## 🛑 CORRECTION TO THE `PsTransaction` RULE ABOVE — safety is per TYPE (B.27, 10/08/2026)

The `PsTransaction` section above says *"**READ ONLY.** Reading is safe and proven; every MUTATOR
on a bound object is suspect."* That was three data points — `PsGrid`, `PsPlate`, `PsShape` — and
it does not generalise.

**`bind cls=editconn` — a plain READ of a `PsEditConnection` — killed AutoCAD on the first call.**

| | |
|---|---|
| **read-safe, measured repeatedly** | `PsGrid` · `PsGussetConnection` · `PsPlate` · `PsShape` |
| ⛔ **read-LETHAL** | **`PsEditConnection`** |
| ⛔ **write-LETHAL** | `PsGrid.addUserXaxis` |
| **unknown** | the other ~52 overloads — **each answer costs a crash** |

⇒ ⭐⭐ **SAFETY IS PER TYPE, NOT PER OPERATION.** `bind cls=editconn` is behind `force=1`.

---

## ⭐⭐⭐ THE WHOLE-MODEL CONNECTION INVENTORY WORKS — B.27

B.27 retracted its own `connverify` on two grounds. **One was wrong.**

> *"`getBoltObjectId(0)` and `getLinkObjectId(0)` returned **0 on every link**."*

They return the real objects. Measured on four joints of known content plus an unconnected
control:

```
15EE shear plate  parts=1610,1611 (2/2)  bolts=1612…1615 (4/4)   target=15ED
16F9 splice       parts=16FB…16FE (4/4)  bolts=16FF…170E (16/16) target=16FA
A45  unconnected  links=0                                        <- the control
```

The zero was `connverify`'s, not the API's — and it stayed hidden because `connscan` was printing
those ids as **raw 64-bit pointers**, where a 0 among them read as *"nothing there"*.

**And the chapter's real question — *"what type is this link?"* — is answered on
`PsLogicalLink.Type`, not on `PsEditConnection`:**

```
connscan   scanned=1147  withlinks=307  links=405  err=0
kShapeWithRip 94 · kStiffenerLink 102 · kConnectedBy 56 · kLascheConnectionLink 26 ·
kWebAngleConnectionLink 18 · kSchearPlateConnectionLink 13 · kConnectWithSchearPlate 11 ·
kConnectWithPurlin 11 · kAngleCutLink 10 · kConnectWithWebAngle 9 · … 20 types in all
```

⇒ **That is B.27's Group Display, from code.** Every link typed, with its owner, target, plates
and bolts. **The editor's INVENTORY half is fully reachable. Only its EDIT half is not** — and
that half is the lethal type.

⚠️ The traffic light 🟢 correct / 🟡 hole distances / 🔴 collisions lives on
`PsEditConnection.Status` and stays out of reach. **Judge by geometry instead:** `vfy_fit` for the
packet, `collision` for the red light.

---

## ⚠️ `connkill` REMOVES BY NUMBER, FROM A LIST THAT RENUMBERS

```
connkill handle=C7A number=0 deleteparts=0   ->   links 5->4
```

…and the link that was asked for **is still there**, now numbered **−1** with type
`kUndefinedLink`, while a different, real joint's owner-side link has gone. `number=-1` refuses,
so the stub cannot be removed.

✅ The **geometry** was unharmed — checked, not assumed: `vfy_fit` over the band read 22 bolts,
OK=22, `BOLT-NO-HOLE=0`.

⇒ ⭐ **Same trap as B.26's hole fields. Delete from the HIGHEST index down, and READ THE LIST BACK
afterwards** — the numbers are indices into a list that renumbers, and a removal can leave a stub
nothing removes.

---

## ⭐⭐ `grouporphans` — B.28.3's CHECK GROUPS, COMPOSED (B.28, 10/08/2026)

`Check Groups` as a **dialog** is not in the API — B.28 scanned all 8 622 public types and was
right. But the manual says what each button *does*, and all three are **membership questions**,
which `PsObjectGroup` answers:

| button | the manual | the query |
|---|---|---|
| **Mark Orphans** | *"the parts that **don't belong to a group**"* | `getMainPartOf(id) == 0` |
| main-part check | *"whether all the groups have a main part"* | `getMainPart() == 0` on a populated group |
| Release Single Part | one-member groups | `PartCount <= 1` |

```
grouporphans   steelParts=707  inGroup=293  ORPHAN=414
               GROUP-NO-MAIN=0  SINGLE-PART-GROUP=0  distinctGroups=65
```

⇒ ⭐ **A manual that describes a command in separable parts is an instruction to compose it** —
the same reading that produced B.25's braced bay from six buttons.

⚠️ **And validate the instrument before believing its number.** The first run showed `PartCount`
climbing monotonically with the handle and `mainPart` equal to the part's own handle — which looks
exactly like a global counter leaking. Cross-checked against `groupinfo`, a different route to the
same object: **the counts are real members** (`53C` → 7 members listed by handle; `541` → 21). The
suspicion was wrong and checking it was still right.

---

## ⭐⭐ WHAT THE SOFTWARE BUILDS, IT REGISTERS. WHAT YOU BUILD BY HAND, IT DOES NOT.

Third measurement of the same boundary:

| register | in it | not in it |
|---|---|---|
| **`FamilyClass`** (B.5) | everything a **connection class** generated — 40 parts | everything hand-built — 780 |
| **`AreaClass`** (B.5) | 305 | 515 |
| **groups** (B.28) | **293**, in 65 groups | **414 orphans** |

A B.20 shear plate sits in a group of three whose main part is **the column** — the connection
templates carry `CreateGroup=True` and build the group themselves. The beam of that same joint,
and a B.25 bracing rod, are orphans.

⇒ ⚠️ **Every downstream consumer reads that register** — parts lists, shipping marks,
`Display as 1 Part`, and B.29's position numbers. A hand-built detail is invisible to all of them
until it is grouped and classed.

⛔ **Do not group them in a loop.** A group encodes *stock part / dispatched part / site assembly* —
that is a **fabrication decision** and Amir's taxonomy, exactly as B.5 refused to invent a
display-class scheme. **The capability is proven; the scheme is his.**

---

## ⛔⛔ NOT ONE PART IN THE MODEL HAS A POSITION NUMBER — B.29, 10/08/2026

```
posnum   objects=1194   withPosnum=0
```

Three chapters key off it, in the manual's own words:

| chapter | |
|---|---|
| **B.4.5** Clone Manipulations | *"only parts with the **same position number** will be considered"* |
| **B.28.3** Compare+Modify | compares groups **by** position number |
| **B.29.1** Group Detection | *"single parts are only compared **using their position number**"* |

⇒ **All three are inoperative on this model and always have been.** B.29's own action-list item #1
is *"number BEFORE replicating, not after."*

⚠️ **And it places B.4's measurement properly.** That audit found `TakeoverDrills` transfers with
no position number, with a different one and with a matching one alike, and recorded *"why 06/08
returned zero is unknown."* The field is **blank on every part in the drawing**, so the "different"
and "same" cases were two hand-set values in an otherwise empty field. That is a measurement of
**what happens when the field is empty**, not a refutation of the manual's precondition.

---

## ✅ CHECK-ONLY NUMBERING — `posauto dry=1`

B.29 asked whether numbering can run without writing, and proposed
`PsCreatePositioning.SetOverrideExisting(false)`. **That engine cannot be run from code at all** —
no public `Perform`/`Run`/`Execute`/`Apply`, no `SetToDefaults`; the only orchestrator is the modal
`PS_POS` dialog. The capability exists because it was **built here**, on
`PsCompareDrawing.CheckTwoPartsAreEqual` — the one identity test that sees cuts (`IsEqualTo` is
the trap).

```
posauto dry=1 kinds=shape,plate tol=0.5
  ->  parts=677  distinct=272  buckets=205  comparisons=705
      written=0  failed=0   (DRY RUN)   secs=8.7

posnum  (re-read, independently)  ->  withPosnum=0     <- verified: nothing written
```

⭐ **Verify "nothing was written" with a SECOND, INDEPENDENT read** — not with the op's own
`written=0`. That is the same discipline as reading a part back after creating it.

### What the dry run measures about a model

**677 parts → 272 distinct positions ⇒ 405 are duplicates: a 2.5 : 1 repetition ratio.**
That is *"build once, then replicate"* measured on a real drawing, and it is what a numbering pass
is worth — each of those 405 only has to be detailed, checked and NC'd **once**.

⚠️ 205 buckets, 705 comparisons — bucketing keeps it near O(n) instead of 677²/2 ≈ 229 000.

---

## ⏳ THREE THINGS THIS AGENT MUST NOT DECIDE — the pattern, settled

| | the capability | the scheme |
|---|---|---|
| **B.5** display / family classes | proven — writable, readable | ⏳ **Amir's** — *"bracings, bay rails, curtain walls"* is a structural taxonomy |
| **B.28** groups | proven — `grouporphans` finds 414 orphans of 707 | ⏳ **Amir's** — a group encodes stock / dispatch / site assembly |
| **B.29** position numbers | proven — dry run in 8.7 s | ⏳ **Amir's** — family **prefixes**, and the column/beam **ANGLE** tolerance |

⇒ ⭐ **Where the input is a shop convention rather than geometry, measure and report. Do not
invent it.** ⚠️ B.29 is the one that blocks the most: B.4.5, B.28.3 and B.29.1 are all waiting on
it.

⚠️ **Two corrections B.29 made to itself, worth keeping:** `SetColumnTol`/`SetBeamTol` are **angle**
tolerances, not dimensional; and `SetHolesTol` is the drill **AXIS deviation**, not the diameter —
*"the detailed section overrides the summary."*

---

# 🏗️ PART E — STRUCTURAL ELEMENTS. The rule that governs all eight chapters.

*Measured 11/08/2026, E.1 + E.3.*

## ⭐⭐⭐ A structural element is creatable from code ONLY where a SEPARATE `PsCreate*` class exists

| class | route | measured |
|---|---|---|
| **`PsCreateHandrail`** | ⭐ separate creator · `SetToDefaults()` + `Create()` | ✅ **WORKS — `Create()=True`, 16 objects, a complete handrail** |
| `PsStairs` | self-insert · `Insert(...)` | ⛔ **False**, seven configurations |
| `PsLadder` | self-insert · `insert(...)` *(Void)* | ⛔ nothing created |
| `PsPortalFrame` | self-insert · `init()` + `insert(...)` | ⛔ returns 0, nothing created |
| `PsBracing` | self-insert · `insert(...)` | ⛔ False, ten configurations (B.24) |
| `PsGussetConnection` · `PsTruss` · `PsJoist` | — | ⛔ **no creator at all** |

⇒ **`PsCreateHandrail` is the only `PsCreate*` in the entire `StructuralObject` namespace, and it
is the only structural element that builds from code.** Everything else in part E is interactive
by design — B.24's conclusion, reached again from a different chapter.

> ### ⚠️⚠️ B.23's TABLE IS A MAP OF METHOD EXISTENCE, NOT OF CAPABILITY
> It listed `PsLadder`, `PsPortalFrame` and `PsBracing` under *"creation route ✅"* because the
> **method exists**. All three refuse. **Existence ≠ works**, and the difference is seven refusals
> on `PsStairs` alone.

## 🛑 AND `PsStairs` DOES HAVE A CREATOR — B.23 said it had none

The reason is **capitalisation**:

```
PsStairs          Boolean Insert(...)   ← CAPITAL I
PsLadder          Void    insert(...)
PsBracing         Boolean insert(...)
PsPortalFrame     Int32   insert(...)
PsCircularStairs  Boolean insert(...)
```

**A grep for `insert(` misses `Insert(`.** Same family as B.13's .NET-vs-COM enum split and B.21's
`HoleWorkloose` / `HoleWorkLoose`. ⇒ **Search case-insensitively, always.**
✅ Confirmed genuinely creatorless: `PsTruss`, `PsJoist`.

## ⭐⭐ THE API IS IN GERMAN WHILE THE DIALOG IS IN ENGLISH

`PsStairs` is unreadable from the English dialog alone:

| dialog | API | |
|---|---|---|
| `Riser` | `setSteigung` | *Steigung* = rise |
| `Treading` | `setAuftritt` | *Auftritt* = tread |
| `Landing` | `setPodestDown` / `setPodestTop` | *Podest* |
| the stair **cheeks** | `setWangenShape(Katalog, Key)` | *Wange* = stringer |
| `Web Grating` | `setGitterRost` | *Gitterrost* |
| `No. of Platforms` | `setEtageCount` / `getEtage(i)` | *Etage* = storey |

⇒ **When an English dialog word finds nothing in the API surface, search the GERMAN construction
term.**

## ⚠️ AN OUT-PARAMETER THAT WAS NEVER WRITTEN IS NOT ZERO

`computeSteigung(0, ref stepCount)` on a stair that does not exist returns garbage —
`-2010406392`, a **different** huge negative each run — and `computeAngle(0)` returns a constant
**0.785 rad (π/4)**, a default rather than a computation.
⇒ **A "computed" field is not a measurement until the object exists.**

---

## `handrail` — the one that works (E.3)

```
handrail pts=x,y,z;x,y,z;...  [conn=] [outside=0|1] [sideoffset=]
```

⭐ **`PsCreateHandrail.SetPolygon(Int64 PolygonId)` is the precondition: a handrail is built ALONG
AN EXISTING PATH.** The op draws the `Polyline3d` itself and reports its handle, so the path stays
auditable. Same workflow as B.8.2's bent shapes — draw the path, then apply the section.

**Measured on a 4 000 mm straight path — 14 shapes, read back from the model:**

```
5 posts     RO48.3x3.6   x 120200,121100,122000,122900,123800   z 1008->2000/2048   <- 900 mm pitch
5 base pl.  FL 60x8      150 long, one under each post, z 1004
1 top rail  RO48.3x3.6   z 2024, 3648.1 long   <- inset: it stops at the END POSTS, not the path ends
3 infill    RO26.9x2.6   z 1263 / 1513 / 1763, 4000 long, 250 apart
```

⇒ A proper industrial rail: **48.3 CHS posts and top rail, 26.9 CHS infill, ~1 040 mm high**.
⚠️ **The top rail is shorter than the path** (3 648 vs 4 000) because it spans post-to-post; the
infill rails run the full 4 000. **Two different length rules in one object** — a parts list that
assumes one will be wrong.

---

## ⛔⛔ `bind` WITHOUT `cls=` KILLED AUTOCAD — my own op bypassing my own guard

B.23 recorded that `PsTransaction.GetObject` **does not type-check**, and gave `bind` a guard that
refuses a mismatch. But with `cls=` **omitted** the op used to try grid, gusset, plate and shape in
turn — **walking straight past the guard.** Pointed at a `Ks_HandRail` it took the process down.

⇒ **`cls=` is now MANDATORY. There is no "try them all" mode.**
⇒ ⭐ **A guard with a bypass is not a guard.** When you write one, check every path into the
function, not the path you were thinking about.

---

## ⚠️ THE PLUGIN VERSION LIVES IN `app/eb_api.py` AND NOWHERE ELSE — including scripts

A test script did `E.DLL = E.DLL.replace("174", "175")` to avoid editing `eb_api.py`. `eb_api.py`
was still on **173**, so the `replace` silently did nothing while `E.RUN_CMD` was forced to
`EB_RUN175`. Result: `Unknown command "EB_RUN175"`, a netload loop that never converged, and ten
minutes lost to what looked like a plugin failure.

⇒ **Never override `DLL`/`RUN_CMD` in a script.** Edit `app/eb_api.py`. That is what "the canonical
version is `eb_api.py` and nothing else" is for.

---

## ⭐⭐ BUILDING A STRAIGHT STAIR BY COMPOSITION — E.1, 11/08/2026

The API's creator refuses, so the stair is built from parts, the way B.23's gusset and B.25's
braced bay were. **Verified: 128 bolts, `OK=128`, `BOLT-NO-HOLE=0`, `GAP-IN-PACKET=0`,
`collisions=0`.**

```
Height 3000 · riser wanted 180 -> 17 risers of 176.47 · 16 treads · 2R+T=623 -> T=270
Run 4320 · angle 34.78 deg · width 1000 across the outer steel edge

stringer  FL 300x10  rot=90        axis y = +-495   -> occupies +-490..+-500
cleat     L 60x6, 250 long         L: rot=270 · R: rot=180
                                   axis y = +-460, z = tread_top - 38
tread     plate 980 x 270 x 8      bottom seats on the cleat's horizontal leg
bolts     M12 DIN7990, 2 per joint · cleat->stringer at x_c +-80 · tread->cleat at x_c +-30
```

### ⭐⭐ THE ANGLE ORIENTATION SWEEP — and the hole DEPTH is the instrument

An angle placed by its axis tells you nothing about where its legs are. Measured by drilling each
`rot` from +Y at mid-height and reading the **depth**:

| `rot` | vertical leg | horizontal leg | depth read |
|---:|---|---|---|
| **0** | −Y | **bottom** | **6** = one leg |
| **90** | +Y | bottom | 6 |
| **180** | **+Y** | **TOP** | **60** = along a leg |
| **270** | **−Y** | **TOP** | **60** |

⇒ **Left-hand cleat `rot=270`; right-hand cleat `rot=180`.**
⚠️ **The right side is NOT mirrored for you** — B.22's *"do not generalise the sign"*, third time.
⭐ **6 mm = the drill met one leg. 60 mm = it ran along one.** Same instrument as B.22's channel
web and B.26's column strong axis.

### ⭐⭐ B.25's SEATING RULE, THIRD INSTANCE

Cleat axis on the stringer face (y = −490) put its vertical leg at **−514…−520** while the
stringer is at −490…−500 — **14 mm of air**, and `boltparts` refused with *"holes further apart
than Gap distance"*.

```
the leg spans (axis-30)..(axis-24)  ->  to seat it on the face at -490:  AXIS = -460
```

⇒ **An angle's material is at the EDGE of its envelope, not at its axis.**

---

## ⛔⛔ TWO CAUSES OF BOLT COLLISIONS, BOTH MEASURED

**① Two bolt groups on the same station line.** 64 collisions, and the geometry named it:

```
cleat->stringer   x 120048..120072   y -524..-476   z 126.47..150.47
tread->cleat      x 120048..120072   y -472..-448   z 141.47..184.47
```

Identical x, overlapping z, **4 mm apart in y**.
⇒ ⭐ **STAGGER THE GROUPS ALONG THE MEMBER** — ±80 and ±30 gives 50 mm clear. Collisions → **0**.

**② Re-running `boltparts` over parts that already carry holes.** Four bolting passes each
**re-drilled**; the stringers reached 66 holes each and the model filled with duplicate bolts.
**Holes cannot be removed.**

> ### ⭐⭐ NEVER RE-RUN A DRILLING OR BOLTING PASS OVER PARTS THAT ALREADY CARRY HOLES.
> Fix the geometry on a **throwaway probe** first, then run the real pass **once** — `drill
> hosts=A,B` for one hole through both parts, `boltparts` once per joint.
> **A re-run is not idempotent. It is destructive and irreversible**, and the only cure is to
> delete every affected part and rebuild — which is exactly what B.25 and B.26 also had to do.

---

## 🛑 `stair/step10` IS NOT A STAIR TREAD — B.8 corrected

B.8's audit listed catalogue `stair` / `step10` as *"stair tread"*. **Measured: `L=925 W=10.125
H=2`** — a 2 × 10 mm line-like symbol read from the catalogue NAME and never built. It carries
nothing. **Use a plate, grating, or a real tread section.**
