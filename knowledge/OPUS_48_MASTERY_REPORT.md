# OPUS 4.8 MASTERY REPORT — Full Programmatic Control of AutoCAD 2015 + ProSteel V8i

> **תקציר בעברית:** זהו הדו"ח המחייב עבור המוח (Opus 4.8) של EB PROSTEEL AGENT.
> המטרה: לא "ירי פקודות" שפותחות חלונות — אלא **מידול מלא, פרמטרי, ברמה הגבוהה ביותר**,
> ישירות מהוראות בשפה חופשית של אמיר (עברית/אנגלית), בתקשורת יומיומית ומקצועית,
> עם תגובה/ביצוע בתוך 10 שניות לכל פקודה.
> הדרך: בניית **EB Modeling API** — תוסף ‎.NET בתוך AutoCAD שיוצר אובייקטי ProSteel
> אמיתיים (PsShape, PsBolt, PsConnection...) בקוד, בלי שום דיאלוג.

**Audience:** the Claude model (Opus 4.8) operating this platform in any future session.
**Status of every fact below:** verified live on THIS machine (2026-07 timeframe) unless marked ⏳.

---

## 1. Mission definition — what "full mastery" means

Amir's explicit requirement (source: `EB PROSTEEL AGENT SOFTWARE.pdf` + V2 + chat directives):

1. **Real modeling, not command-firing.** "לא ביצוע פקודות רגילות אלא מידול מלא בתוכנה בהתאם להגדרות שאני אתן לו." A request like *"תמדל 2 קורות HEB 500 באורך 6 מטר במרחק 3 מטר"* must produce **two native ProSteel members with correct profile, position, and orientation in the live drawing** — not open the Shapes dialog for him.
2. **Daily professional communication.** He talks to the agent like to a colleague engineer (free Hebrew/English, voice or text, sketches, uploaded plans). The agent converses naturally AND acts.
3. **Speed SLA: ≤ 10 seconds** from command to modeled-result-or-meaningful-response. Never silent, never stuck "thinking".
4. Work is discussed **only in the console** (`app/console.py` UI). One project = one folder = own chat/files/memory.
5. North star: increasing autonomy — from co-modeling to modeling whole structures from a client PDF/DWG.

---

## 2. The three control layers (discovered + verified on this machine)

### Layer 1 — AutoCAD COM automation (WORKS TODAY, in `app/acad.py`)
- `win32com.client.GetActiveObject("AutoCAD.Application")` → live app; `doc.ModelSpace` add-methods (AddLine, AddCircle, AddBox, AddCylinder...), `CopyObjects`, layer control, `SaveAs`, `doc.SendCommand(...)`, `app.ZoomExtents()`.
- Reads any DWG (`extract()` enumerates entities — used to analyze client plans).
- **Pitfalls (hard-won):**
  - `SendCommand` **blocks forever** if AutoCAD is not quiescent (dialog/command open). ALWAYS guard: `app.GetAcadState().IsQuiescent` (non-blocking even when busy). If busy → reply instantly "AutoCAD עסוק, לחץ ESC".
  - After launching AutoCAD, COM calls throw `RPC_E_CALL_REJECTED (-2147418111)` while loading → retry loop (~4s sleeps, up to ~120s).
  - Every thread must `pythoncom.CoInitialize()`. Points are `VARIANT(VT_ARRAY|VT_R8, [x,y,z])`.
  - Prefer COM property sets over SendCommand (e.g. view change = `doc.ActiveViewport.Direction = <vector VARIANT>` then reassign `doc.ActiveViewport = vp` — instant, no command line).
  - Launch ProSteel correctly (plain acad.exe does NOT load it):
    `acad.exe /p "...\Prg\ProStructures_SS6.1ACAD_E001_409.arg" /t Ps191_Metric /ld ProStructuresLoader.arx` (workdir `...\AutoCAD 2015\Dwg`). Ready-made: `EB PROSTEEL AGENT.bat`.

### Layer 2 — ProSteel command layer (LIMITED — dialogs)
- 82 commands catalogued in `knowledge/PROSTEEL_COMMANDS.md`; NL→command dispatcher in `app/prosteel.py` (`command_for("תכניס קורה")` → `PS_INS_PROF`).
- **Modeling commands open MODAL dialogs** (Shapes: type→class→size→points). Useful to *assist Amir working manually*; NOT the path to autonomous modeling. This layer alone is why the agent previously felt like "regular commands" — insufficient.

### Layer 3 — ProStructures .NET object model (THE MASTERY PATH — verified, not yet built)
**Discovery (verified by strings/reflection scan):** `C:\Program Files\Bentley\ProStructures Ss6 R1\AutoCAD 2015\Prg\ProStructuresNet.dll` (+ `KsKernel.dll`) exposes the **complete native object model**:

`PsShape` (straight member!), `PsArcShape`, `PsBendShape`, `PsPlate`/`PsArcPlate`/`PsBendPlate`, `PsBolt`, `PsBoltStyle`, `PsConnection`, `PsGussetConnection`, `PsEditConnection`, `PsWorkframe`, `PsBracing`, `PsPortalFrame`, `PsJoist`, `PsHandrail`, `PsAssembly`, `PsPrimitive`, `PsEditShapeModification` (cuts/copes!), `PsWeldFlag`, `PsPositionFlag`, ... (also full ProConcrete set).

All the PSN_*.dll connection macros Bentley ships are built ON this API and loaded via `_NETLOAD` — proof the pattern works inside AutoCAD 2015.

- It is an **in-process AutoCAD .NET API** (depends on `Acdbmgd 20.0`) → cannot be driven from outside; **we must write our own plugin DLL that runs inside AutoCAD**.
- Build tooling **already on this machine** (verified): `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` ✅, references `acmgd.dll`/`acdbmgd.dll`/`accoremgd.dll` in `C:\Program Files\Autodesk\AutoCAD 2015` ✅, `ProStructuresNet.dll` ✅. **No purchases, no installs** — consistent with Amir's no-cost mandate.

---

## 3. The deliverable: **EB Modeling API** (`EBAgentApi.dll`)

A small C# class library, compiled ON this machine with csc.exe, NETLOADed into AutoCAD. It exposes **parameter-complete, dialog-free commands** that the Python side calls via one `SendCommand` line. This turns free speech into native modeling.

### 3.1 Command surface (v1 — sized to the acceptance test, then grow)
```
EB_BEAM    <shapeClass> <shapeSize> <x1,y1,z1> <x2,y2,z2> [rotationDeg]
           → new PsShape with profile from the SHAPES DB, between two points.
EB_BEAM_BETWEEN <handleA> <handleB> <shapeClass> <shapeSize> [offsets]
           → diagonal/secondary member snapped between two existing members.
EB_MITER   <handle> <end:start|end> <planeDef>       (PsEditShapeModification)
           → angle-cut a member where it meets another.
EB_PLATE   <x,y,z> <normal> <width> <height> <thickness>       (PsPlate)
EB_BOLTS   <pattern:rect|circ> <count> <M-size> <holeDia> <at...>  (PsBolt/PsBoltStyle)
EB_COPY    <handle> <dx,dy,dz> [n]        → native array of members
EB_INFO    <handle> | EB_LIST             → JSON of ProSteel data to the command line
           (lets the brain SEE the model: profiles, positions, handles)
```
Each command: parses args → creates/edits native Ps objects → prints a single-line `EB_OK {json}` or `EB_ERR {reason}` to the command line (readable via COM `doc.GetVariable("LASTPROMPT")` or a results file) → returns in <1s.

### 3.2 Build & load protocol (Opus does this, ~minutes, one time + on changes)
1. Write `EBAgentApi.cs` in `app/plugin/`.
2. Compile:
   `csc.exe /target:library /out:EBAgentApi.dll /r:"acmgd.dll" /r:"acdbmgd.dll" /r:"accoremgd.dll" /r:"ProStructuresNet.dll" EBAgentApi.cs` (full paths).
3. Load: `doc.SendCommand('_NETLOAD "...\\EBAgentApi.dll" ')` — add to session-start procedure (or registry demand-load `HKLM\...\AutoCAD\R20.0\ACAD-E001:409\Applications`).
4. Smoke-test: `EB_BEAM HEB 500 0,0,0 6000,0,0` → expect `EB_OK`.
- ⏳ Exact ProStructuresNet class signatures (constructors of `PsShape`, profile lookup by class/size, `PsEditShapeModification` usage) are the ONE unknown. Resolution order: (a) reflect the DLL *inside* AutoCAD via a tiny bootstrap command that dumps `PsShape` members to a file; (b) mine `Samples\COM Macros\*.dwg` (contain VBA using Ksx objects — same model); (c) match method names against the German-rooted kernel naming (Ks = Kernsystem). Budget one focused session; every later capability rides on it.

### 3.3 Fallback layer (if a specific Ps ctor resists): **pywinauto dialog automation**
`pip install pywinauto` (free). Drive the real dialogs invisibly: open `PS_INS_PROF`, set Shape class/size fields, click insertion — verified UI paths exist in the decompiled help (`bus were deleted; help is re-extractable from ProSteel_AutoCAD.chm`). Slower (~2-4s) but still inside SLA. Use ONLY where Layer 3 has a gap; log each use as tech-debt to replace.

---

## 4. The 10-second SLA architecture (respond OR model, never silence)

**Rule: every console message gets a response ≤10s. Three lanes:**

| Lane | What | Latency | Implementation |
|---|---|---|---|
| A — instant local | view/zoom/tool-open, EB_* API calls with fully-specified params | 0.03–2s | `_quick_action()` in console.py (already live; extend to parse EB_* modeling phrases → SendCommand to the plugin) |
| B — resident brain loop | anything needing interpretation (multi-step modeling, plan questions) | 2–10s | **The brain must be IN the listening loop during work sessions**: `python console.py wait 280` in a continuous cycle. On pull: act via lanes A/COM/plugin, `say` the result. Never leave the loop while a session is active. |
| C — honest deferral | long work (analyze a 100-page plan) | ack ≤10s | Immediately `say("קיבלתי — מנתח, ~2 דקות")`, then work, then report. |

**Enforcement details (all already coded, keep them):** ThreadingHTTPServer (no single-call freeze) · IsQuiescent guard before any COM · ESC-ESC prefix on SendCommands · busy → instant "לחץ ESC" reply · server auto-relaunch check at session start (it dies between turns) · UI must be hard-refreshed (Ctrl+R) after server restart — consider adding a version stamp + auto-reload (open TODO).

**Session-start checklist (run on the trigger phrase, ≤60s total):** launch `.bat` if AutoCAD/console down → poll COM ready → `_NETLOAD` EBAgentApi → set/confirm active project → `say("מוכן")` → enter Lane-B loop.

---

## 5. Natural language → modeling (how daily speech becomes API calls)

Pipeline per message: **parse intent** (Hebrew/English; profiles like "HEB 500", lengths "6 מטר"→6000mm, spacing, counts) → **resolve geometry** (current model state via `EB_LIST`/COM extract; project memory MODEL.md; active UCS/view) → **emit EB_* commands** → **verify** (EB_OK + entity count/handles) → **reply in one professional sentence** (what was modeled, dimensions, handles) + speak it.
- Ambiguity policy: if a parameter is missing, make the engineering-standard assumption, state it, and proceed ("הנחתי גובה 0, תגיד אם אחרת") — do NOT stall on questions for defaults. Amir wants office-colleague flow.
- Units: mm, metric (Ps191_Metric). Record every modeled step in the project's MODEL.md history.

## 6. Acceptance test — exact execution plan (each step ≤10s)

1. **"תעבור למבט TOP"** → Lane A, `ActiveViewport.Direction=(0,0,1)` + ZoomExtents. ✅ already works (34ms).
2. **"2 קורות HEB 500 באורך 6מ', מרחק 3מ'"** → `EB_BEAM HEB 500 0,0,0 6000,0,0` + `EB_BEAM HEB 500 0,3000,0 6000,3000,0` (or EB_COPY). Native PsShapes.
3. **"אלכסון HEA 500 ביניהן, חתוך בזווית"** → `EB_BEAM_BETWEEN` the two handles + `EB_MITER` both ends (PsEditShapeModification).
4. **"חיבור מתוברג: RHS + 2 פלטות 20מ"מ מרותכות, 4 ברגים M20, קדח 23"** → `EB_PLATE` ×2 (t=20) + `EB_BOLTS rect 4 M20 23` + weld flags; if the full native connection resists v1 → build it from plates+bolts primitives (still native objects) and say exactly what was placed.

**Do not run the test before:** plugin compiled, NETLOADed, smoke-tested, and lanes rehearsed end-to-end once.

## 7. Roadmap after the test passes
R1 connections library (EndPlate/Gusset/Shear via PSN macros parametrized or Ps objects) → R2 plan-driven modeling (plan2steel members → EB_BEAM batch = the 2D→3D dream) → R3 production chain (PS_POS → PS_CREATE_PARTLIST → PS_DETCENTER → PS_NC_DATA) → R4 autonomy (PDF in, model out, Amir reviews).

## 8. Standing rules (unchanged, binding)
Free brain = Claude via Claude Code (no paid API) · work talk ONLY in the console · Hebrew replies via UTF-8 file + `console.cli_say` (terminal mangles Hebrew) · per-project folders under `projects/` · full file paths in `knowledge/`, `app/` · memory file `acad-agent.md` is the session bootstrap — keep it updated.
