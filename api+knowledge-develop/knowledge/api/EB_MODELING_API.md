# EB MODELING API — Runbook for Opus 4.8 (native ProSteel modeling from code)

> **THE BREAKTHROUGH (verified live 2026-07-07):** we can create **real native
> ProSteel objects** (`Ks_Shape` beams, miter cuts) **programmatically, with NO
> dialogs**, via a C# plugin (`EBAgentApi`) NETLOADed into AutoCAD, driven from
> Python (`eb_api.py`). A HEB 500 beam is created in ~3 seconds from the words
> "HEB 500". This is the path to true modeling mastery Amir demanded.

This runbook tells Opus exactly how to operate, extend, and rebuild the API.

---

## 1. Architecture (3 files)
```
Amir speaks/types in the console
        │  (Opus parses intent, resolves profile + geometry)
        ▼
app/eb_api.py   ── Python client: resolve_profile("HEB 500")->("HE500B","DIN_HEB")
        │           writes plugin/eb_cmd.txt, SendCommand EB_RUN3, reads eb_result.txt
        ▼
app/plugin/EBAgentApi3.dll   ── C# plugin INSIDE AutoCAD (NETLOADed)
        │           creates native Ps objects via ProStructuresNet.dll
        ▼
AutoCAD 2015 + ProSteel  ── real Ks_Shape / Ks_Plate / Ks_Bolt appear in the model
```

## 2. The discovery that made it possible
`C:\Program Files\Bentley\ProStructures Ss6 R1\AutoCAD 2015\Prg\ProStructuresNet.dll`
is ProSteel's **full .NET object model** (392 public types). Key creation classes
(reflected — see `app/plugin/api_dump_ProStructuresNet.txt`, 820 KB, the ground truth):

| Task | Class | Key calls |
|---|---|---|
| Beam/column | `Bentley.ProStructures.Steel.Shape.PsCreateShape` | `SetToDefaults(); SelectStandardSections(); SetCrossSection(name,catalog); SetInsertPoints(p1,p2); SetRotation(deg); Create()` |
| Plate | `...Steel.Plate.PsCreatePlate` | `SetToDefaults(); AppendEdgePoint(pt)×N; SetThickness(t); Create()` ⚠ see gaps |
| Bolt | `...Steel.Bolt.PsCreateBolt` | `SetToDefaults(); CreateSingleBolt(p1,p2,dia,styleName,0)` ⚠ needs valid style name |
| Cut/miter | `...Modification.Edit.PsCutObjects` | `SetToDefaults(); SetObjectId(idCut); SetAsMiterCutId(idOther,true); Apply()` ✅ works |
| Points | `...Geometry.Data.PsPoint` / `PsVector` | `new PsPoint(x,y,z)` |
| Section DB | `...Steel.Shape.PsShapeLoader` | `CatalogCount; GetCatalog(i); get_NameCount(i); GetName(i,k); FindKatalogFromKey(prefix,false)` |

Other create classes exist for everything: `PsCreateWorkframe`, `PsCreateBolt`,
`PsCreateConnection`, `PsCreateArcShape/BendShape`, `PsCreateHandrail`,
`PsCreatePositioning`, `PsCreatePartlist`, plus the whole ProConcrete set.

## 3. Section-name reality (CRITICAL)
The SHAPES DB has **357 catalogs**. Names are NOT what users say:
- "HEB 500" → DB key **`HE500B`** in catalog **`DIN_HEB`**  (letter moves to the END)
- "HEA 500" → `HE500A` / `DIN_HEA`   ·   "HEM 500" → `HE500M` / `DIN_HEM`
- "IPE 300" → `IPE300` / `DIN_IPE`   ·   "UPN 220" → `UPN220` / `DIN_UPN` (verify)
`eb_api.resolve_profile()` encodes these rules. For unknown families (RHS/SHS/CHS
hollow, e.g. the Tara plan's `RHS100x150x4`), run `op=dumpcat catalog=<CAT>` or
`op=sections filter=<X>` to dump exact keys, then extend `resolve_profile`.
Enumerate any catalog live: `python -c "import eb_api; print(eb_api.run('dumpcat',catalog='DIN_HEB'))"` → writes `plugin/eb_cat.txt`.

## 4. How to MODEL from daily speech (the main loop)
Per console message, Opus:
1. Parse: profiles ("HEB 500"), lengths ("6 מטר"→6000mm), counts, spacing, points.
2. Compute geometry (use current model via `eb_api.list_model()`; project MODEL.md; UCS).
3. Call eb_api verbs:
```python
import eb_api
eb_api.ensure_loaded()                                   # once per session
r1 = eb_api.beam("HEB 500", (0,0,0), (6000,0,0))         # -> EB_OK ... handle=2B8
r2 = eb_api.beam("HEB 500", (0,3000,0), (6000,3000,0))   # 2nd beam, 3 m apart
d  = eb_api.beam("HEA 500", (0,0,0), (6000,3000,0))       # diagonal
eb_api.miter(eb_api.handle_of(d), eb_api.handle_of(r1))   # angle-cut the diagonal
```
4. Verify: every op returns `EB_OK ... handle=<h> entities=<n>` (or `EB_ERR`/`EB_BUSY`). Report one professional sentence + speak it, log to project MODEL.md.
5. Missing param → assume engineering default, STATE it, proceed (office-colleague flow).

## 5. The 10-second SLA (respond-or-model)
- Each eb_api call ≈ 3s → single-member commands are well within 10s.
- Multi-member requests: model them one by one; if the batch would exceed ~8s, `console.cli_say` an interim ("בונה 3 קורות...") then continue — never silent.
- `eb_api.run()` guards on `IsQuiescent` and returns `EB_BUSY` instantly if AutoCAD has a dialog/command open (tell Amir: press ESC). Server is ThreadingHTTPServer.
- Opus MUST stay in the console loop during a work session: `python app/console.py wait` → parse → eb_api → `python app/console.py say "..."` → repeat.

## 6. Build / rebuild the plugin (all tools already on this machine, free)
- Compiler: `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` (verified).
- Sources: `app/plugin/EBAgentApi3.cs` (current). Compile:
```
csc /nologo /target:library /platform:x64 /out:EBAgentApi<N>.dll ^
  /r:"C:\Program Files\Autodesk\AutoCAD 2015\acmgd.dll" ^
  /r:"C:\Program Files\Autodesk\AutoCAD 2015\acdbmgd.dll" ^
  /r:"C:\Program Files\Autodesk\AutoCAD 2015\accoremgd.dll" ^
  /r:"C:\Program Files\Bentley\ProStructures Ss6 R1\AutoCAD 2015\Prg\ProStructuresNet.dll" ^
  /r:"C:\Program Files\Bentley\ProStructures Ss6 R1\AutoCAD 2015\Prg\PSN_HollowShapeBracing.dll" EBAgentApi<N>.cs
```
> ⚠️ **The last reference is not optional** — corrected 17/08/2026 while building v185. The
> command as written here for months omitted `PSN_HollowShapeBracing.dll` and the build fails
> with four `CS0246` errors on the `macrobrace` op. The macro assemblies are ordinary
> references: if a future op touches another `PSN_*` type, add that DLL the same way.
- **A NETLOADed DLL is file-locked** → on every change, compile to a NEW filename (EBAgentApi4.dll, 5...) and update `eb_api.py` DLL/RUN_CMD, OR restart AutoCAD to release the lock. Bump `EB_RUN<N>` command + class name `ApiCmds<N>` to avoid CommandMethod name clashes across loads.
- Load: `doc.SendCommand('(command "_NETLOAD" "…/EBAgentApiN.dll") ')`.
- **TRUSTEDPATHS**: SECURELOAD=1 blocks NETLOAD of untrusted paths → eb_api adds the plugin dir to `TRUSTEDPATHS` (do this once per session before NETLOAD; already handled in the launch flow).

## 7. Session-start procedure (on the trigger phrase, all automatic)
1. If AutoCAD/console down → run `EB PROSTEEL AGENT.bat`.
2. Poll COM ready (`GetActiveObject` + `GetAcadState().IsQuiescent`; retry on RPC_E_CALL_REJECTED ~4s).
3. Add plugin dir to TRUSTEDPATHS; `eb_api.ensure_loaded()` (NETLOAD); smoke `run('ping')` → expect EB_OK.
4. Confirm/create active project; `console.cly_say("מוכן")`; enter the console loop.

## 8. Open gaps (exact next steps — do NOT block modeling on these)
- **PLATE** `PsCreatePlate.Create()` returned false with 4 AppendEdgePoints. Next: try `SetAsRectangularPlate(L,W)` + `SetInsertMatrix(PsMatrix)` (build PsMatrix from origin+X+Y axes), or `SetNormalPosition(VerticalPosition)`, or `UseCurrentLayer(true)`; add per-step try/catch diagnostics. Ref block: api_dump, type `PsCreatePlate`.
- **BOLT** style name "M20" not in DB. Next: enumerate bolt styles — check ProStructures data dir for bolt-style files, or reflect a `PsBoltStyle` loader/manager; then pass the real style name to `CreateSingleBolt`. Bolts also usually need host objects (AddObject(id)) to punch holes — see `PsCreateBolt.AddObject` + `Create()` (pattern mode) vs `CreateSingleBolt` (standalone).
- **Full bolted connection**: once plate+bolt work, a connection = 2 plates (t20) + bolt pattern + weld flags; or use `PsCreateConnection` / the shipped `PSN_*` macros parametrized. Document once solved.

## 9. Proven results (this session)
✅ `EB_PING` · ✅ `beam HEB 500` (HE500B/DIN_HEB, Ks_Shape, ~3s) · ✅ 2nd beam @3m · ✅ diagonal `HEA 500` (HE500A/DIN_HEA) · ✅ `miter` angle-cut (Apply=1) · ✅ natural-name resolver · ⚠ plate/bolt = open gaps above.

## 10. Files
- `app/plugin/EBReflect.cs|.dll` — reflection dumper (EB_PING, EB_DUMPAPI).
- `app/plugin/EBAgentApi3.cs|.dll` — the modeling API (EB_RUN3; ops beam/plate/bolt/miter/list/sections/dumpcat).
- `app/plugin/api_dump_ProStructuresNet.txt` — the FULL reflected API (grep this for any signature).
- `app/eb_api.py` — Python client + resolve_profile + high-level verbs.
- Together with `knowledge/OPUS_48_MASTERY_REPORT.md` (strategy) this is the complete mastery kit.
