# OPUS 4.8 WORK PLAN — Build the full EB API for ProSteel + AutoCAD 2015

> **חלוקת תפקידים (הוראת אמיר):** Fable 5 = מתכנן ומנחה (מסמך זה). **Opus 4.8 = המבצע — כל עבודת הקידוד.**
> **המטרה:** ‎API מלא ורשמי לתוכנות ProSteel V8i SS6 + AutoCAD 2015 — "EB API v1.0" —
> שמאפשר לסוכן למדל קונסטרוקציות פלדה אמיתיות מדיבור חופשי, ≤10 שניות לפקודה, בלי דיאלוגים.

**Executor:** Opus 4.8, in Claude Code, on this machine. Work phase by phase, in order.
Every fact below was verified LIVE on 2026-07-07 unless marked ⏳. When in doubt:
`grep app/plugin/api_dump_ProStructuresNet.txt` — it is the ground truth (the full
reflected API of ProStructuresNet.dll, 392 types).

---

## 0. Standing rules (binding, from Amir)
1. ≤10s respond-or-model per command. Never silent. `EB_BUSY` replies are instant.
2. Native Ps objects only — **no 3D solids, no dialogs** in the modeling path.
3. Work conversation happens ONLY in the console (`app/console.py`); Hebrew replies via UTF-8 file + `console.cli_say` (terminal mangles Hebrew).
4. Free stack only (csc.exe + pywin32 + stdlib; no purchases, no paid API).
5. After EVERY phase: update `knowledge/EB_MODELING_API.md` (runbook) + the memory file `acad-agent.md`. Log modeled steps into the active project's MODEL.md.
6. DLL version discipline: a NETLOADed DLL is file-locked → each rebuild = new filename `EBAgentApi<N>.dll`, new command `EB_RUN<N>`, new class `ApiCmds<N>`; update `eb_api.py` (DLL, RUN_CMD). After an AutoCAD restart you may reuse/consolidate.
7. Test protocol per change: compile → NETLOAD → op smoke test → assert `EB_OK` → only then integrate.

## 1. Verified current state (do not re-derive)
- ✅ Working chain: `eb_api.py` → `plugin/eb_cmd.txt` → `EB_RUN3` (in `EBAgentApi3.dll`) → `plugin/eb_result.txt`.
- ✅ Proven ops: `beam` (native Ks_Shape, ~3s), `miter` (PsCutObjects.SetAsMiterCutId, Apply=1), `sections`, `dumpcat`, `list`, `ping`.
- ✅ Profile truth: "HEB 500" = key `HE500B` in catalog `DIN_HEB` (357 catalogs; names space-padded). `eb_api.resolve_profile()` maps HEA/HEB/HEM/IPE/UPN.
- ✅ Compile: `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:library /platform:x64` + refs `acmgd/acdbmgd/accoremgd` (AutoCAD dir) + `ProStructuresNet.dll` (Prg dir).
- ✅ NETLOAD requires plugin dir in `TRUSTEDPATHS` (SECURELOAD=1). Set once per profile via `doc.SetVariable`.
- ✅ COM discipline: `CoInitialize` per thread; retry `RPC_E_CALL_REJECTED` (~4s × N); guard every op on `GetAcadState().IsQuiescent`.
- ❌ Gap A: `plate` — `PsCreatePlate.Create()` returned false (polygon path).
- ❌ Gap B: `bolt` — style "M20" rejected; `Data\Bolts\` holds only .bmp icons → real style names/storage unknown.
- ❌ Gap C: RHS/SHS/CHS families unresolved (needed for Tara: `RHS100x150x4`).

## 2. Phase plan (execute in order)

### PHASE A — Session bootstrap automation (foundation, ~30 min)
Goal: one function call brings the whole stack up from cold.
1. In `eb_api.py` add `bootstrap()`: (a) if AutoCAD down → launch via the .bat's acad.exe line; (b) poll COM ready; (c) ensure TRUSTEDPATHS contains plugin dir; (d) NETLOAD current DLL; (e) `run('ping')` → assert EB_OK; (f) return status dict.
2. Wire into console server start and into the session-start trigger procedure.
3. **Done when:** from AutoCAD-closed state, `python -c "import eb_api; print(eb_api.bootstrap())"` ends with ping EB_OK, unattended.

### PHASE B — PLATE (Gap A, ~1-2 h)
The dump shows `PsCreatePlate` needs positioning context. Try in this order, adding a diagnostic op that reports WHICH step failed:
1. **Matrix path:** `PsMatrix m = new PsMatrix(); m.SetFromPointAndNormal(new PsPoint(cx,cy,cz), new PsVector(0,0,1)); cp.SetInsertMatrix(m); cp.SetAsRectangularPlate(L,W); cp.SetThickness(T); cp.Create();`
2. If false: add `cp.UseCurrentLayer(true);` and/or `cp.SetNormalPosition(<VerticalPosition enum>)` (reflect enum values from dump; try each).
3. If still false: polygon path + matrix (`AppendEdgePoint`×4 AFTER `SetInsertMatrix`), then `SetFromRectangle(PsRectangle)`.
4. Instrument: wrap each call in try/catch, return `EB_ERR plate step=<name> ex=<msg>`; also compare `Census()` before/after.
5. **Done when:** `eb_api.plate((0,0,300),430,220,20)` → EB_OK + a real Ks_Plate appears (class name in result), in ≤5s.

### PHASE C — BOLTS (Gap B, ~2-3 h, discovery-driven)
Storage of bolt styles is the unknown. Discovery sequence:
1. **Find style names:** (a) run ProSteel's `PS_BOLT` dialog ONCE manually via Amir? NO — dialog-free mandate for the pipeline, but READING the config is allowed: search `%APPDATA%\Bentley`, `C:\ProgramData\Bentley`, and the Prg/Data trees for style files (extensions to try: *.psbs, *.kbd, *.dbf, *.mdb, *.xml with 'bolt' inside; use Grep on binary-ish text too). (b) Reflect deeper: grep the dump for a bolt-style ENUMERATOR you may have missed (`PsBoltStyle` PROPs, static members, `ValueTable`). (c) Fallback: `op=boltprobe` — loop candidate names {"M20","DIN 933","DIN 6914","HV","8.8","4.6","M20x60","DIN931 M20"} × `CreateSingleBolt`; report first success.
2. **If no style exists:** create one programmatically: `PsCreateBoltStyle cbs; cbs.SetToDefaults(); cbs.CreateNewStyle("EB_M20"); cbs.setBoltDescription(...); cbs.WriteTo("EB_M20");` then use "EB_M20". (Signatures already in the dump — setNutDatabase/setWasherDatabase etc.; enumerate the referenced databases the same way as shapes if needed.)
3. **Hole Ø23 + through-parts:** bolts must punch holes: use `PsCreateBolt.AddObject(idPlate/idBeam)` + pattern `Create()` (not only CreateSingleBolt). Hole diameter: check dump for `SetHoleDiameter`/`Tolerance`/`SetDiameter` on PsCreateBolt/PsBoltStyle; if absent, holes via `PsCutObjects`/drill classes (grep dump: `Drill`).
4. **Done when:** `eb_api.bolt(...)` → EB_OK with a real bolt entity; and a 4-bolt rectangular pattern op `boltfield` works through a plate+flange (holes visible), ≤10s.

### PHASE D — Full bolted connection (the acceptance-test finale, ~1-2 h)
Compose B+C into one macro op `conn_bolted`: inputs = two member handles + plate size/thickness + bolt spec (count, M-size, hole dia) + weld marks.
1. Compute interface geometry from members (`PsShape.GetMidLine` via attach-to-id — investigate `SetObjectId`/`PsUtils` binding in dump; fallback: Python computes from stored creation points in the project log).
2. Place 2× plate t20, 4× M20 bolts Ø23 holes, weld flags (`PsCreateWeldFlag` — enumerate styles via `GetWeldStyleName(i)`).
3. **Done when:** one console sentence in Hebrew produces the full connection ≤10s (or staged with interim say()s), and it matches Amir's spec: "RHS + 2 פלטות 20מ"מ מרותכות, 4 ברגים M20, קדח 23".

### PHASE E — Complete the profile resolver (Gap C, ~1 h)
1. Run `dumpcat` for hollow-section catalogs (from the 357 list: `DIN_QUADRATROHR_KALT` [SHS], `DIN_RECHTECKROHR_KALT` [RHS], pipe catalogs; also AISC/BS/AS families for future).
2. Build once `knowledge/section_catalog_map.json`: family → catalog + key format (write a small script that dumps ALL catalogs → parses names → saves the map). Load it in `resolve_profile` (cache).
3. **Done when:** `resolve_profile("RHS100x150x4")` returns a working (key, catalog) and `beam("RHS 100x150x4", ...)` → EB_OK.

### PHASE F — Console fast-lane modeling (speed, ~1-2 h)
1. Extend `_quick_action` in `console.py`: regex-parse simple Hebrew/English modeling sentences — "תמדל קורה HEB 500 מ0,0,0 עד 6000,0,0", "קורה בין X לY", "שכפל את הקורה 3 מטר ימינה" — call `eb_api` DIRECTLY in-server (import it; guard IsQuiescent). Instant native modeling with zero brain latency.
2. Anything ambiguous still queues to the brain loop (Lane B).
3. **Done when:** typing a fully-specified beam sentence in the console models it in ≤5s with no Claude involvement.

### PHASE G — Model awareness (read-back, ~1 h)
1. Enrich `op=list`: for each Ks_Shape, attach profile name + endpoints. Investigate binding an existing ObjectId to PsShape (grep dump for `FromObjectId`/`SetObjectId`/`PsUtils`/`PsTransaction`). Fallback: maintain `projects/<id>/model_log.jsonl` written by eb_api on every create (handle, profile, p1, p2) — the brain references members by that.
2. **Done when:** the brain can answer "מה יש במודל?" with profiles+positions, and target "הקורה הראשית" reliably.

### PHASE H — Structure ops (~1 h)
`workframe` (PsCreateWorkframe/PsCreateGrid), `copy` (native array via acdbmgd deep-clone or Ps copy — verify class survives copy as native), `delete <handle>`, `undo mark/rollback` (acad UNDO group around each op for one-command rollback).
**Done when:** grid + copy + undo verbs work from the console.

### PHASE I — Production chain (~2 h, dialog-assisted allowed here)
`position` (PsCreatePositioning), `partlist` (PsCreatePartlist → file export), then DetailCenter (`PS_DETCENTER`) and `PS_NC_DATA` — these may open dialogs; acceptable: agent prepares everything and fires the tool for Amir to confirm. Document each.
**Done when:** a modeled test structure yields position numbers + a parts list file automatically.

### PHASE J — Acceptance test + performance report (~30 min)
Run Amir's full test end-to-end in a fresh project via console only:
TOP view → 2× HEB 500 6m @3000 → diagonal HEA 500 mitered → full bolted connection.
Measure per-command wall time; write `knowledge/ACCEPTANCE_REPORT.md` (times, handles, screenshots via console 📸). **Done when:** every step ≤10s (or staged with interim replies) and Amir approves.

## 3. How-to references (for the executor)
- **Add an op:** edit newest `EBAgentApi<N>.cs` → add `case` in `Run()` + method → compile to `<N+1>` names → NETLOAD → smoke test → bump `eb_api.py`.
- **Find any signature:** `Grep pattern app/plugin/api_dump_ProStructuresNet.txt` (types start with `=== TYPE`).
- **Compile template:** see §1; C#5 only (no `$""`, no `?.`), x64, escape regexes carefully.
- **Console loop (work session):** `python app/console.py wait 280` → act → `python app/console.py say "<hebrew>"` → repeat. Interim say() for anything >8s.
- **Pitfall log:** SendCommand blocks when not quiescent · RPC_E_CALL_REJECTED on startup · DLL file-lock · TRUSTEDPATHS · space-padded DB keys · Hebrew=UTF-8 files only.

## 4. Definition of Done — "EB API v1.0"
1. `bootstrap()` cold-start ✅ 2. beam/plate/bolt/miter/conn_bolted/copy/workframe/list/undo all EB_OK ✅ 3. resolver covers HEA/HEB/HEM/IPE/UPN/RHS/SHS/CHS ✅ 4. console fast-lane models simple sentences ≤5s ✅ 5. acceptance test passed + report ✅ 6. runbook + memory updated ✅.

## 5. Risks & honest limits
- Some Ps creation classes may require dialog-context defaults → mitigate with SetToDefaults + template manager; worst case: that ONE feature is dialog-assisted, documented.
- Bolt-style storage may be per-user config not yet created (fresh install) → create programmatic style "EB_M20" (C.2) — acceptable and persistent.
- AutoCAD 2015-era COM stability: keep retries; never assume state; always re-check quiescent.
