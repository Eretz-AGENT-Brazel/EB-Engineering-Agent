---
name: prosteel-modeling
description: How to model real steel structures in AutoCAD 2015 + ProStructures (ProSteel) V8i SS6 the way Eretz Barzel actually does it — the agent's job is to BE Amir's modeller, taking the grunt work and reaching every function the software has. Covers connections as parametric objects (base plates, end plates, fin plates, web cleats, copes, haunches, purlins, splices, gussets, stiffeners/ריפים), modelled holes at every bolt passage, non-rectangular plate contours, column shortening, build-once-then-replicate as both a modelling method and a fabrication-economics goal, metric-always, the absolute ban on LISP, and the mapped API surface (~1,400 relevant public types across 116 managed assemblies — PsCreateFastener, PsGeometryFunctions, PsMiscTools, PsObjectGroup, PsSelection, PsCollisionCheck, PsCreatePositioning, PsDrillObject, PsEditLogicalLink and the nine connection classes) plus the measured dead ends and a measured performance baseline. Also carries the column-base anchorage engineering (EN 1992-4 / EN 1993-1-8 / AISC DG1) for value engineering, since the company fabricates rather than designs loads. Use whenever modelling, editing, auditing or automating steel in ProSteel/AutoCAD, reading an existing steel model, or deciding whether a detail is modelled correctly for fabrication. Built from live lessons with Amir (Eretz Barzel); every claim was verified by reading the model back, never from a screenshot.
---

# Modelling steel in ProSteel — the Eretz Barzel way

Working knowledge built from live lessons with **Amir (ארץ ברזל)**, who models steel professionally
and corrects the agent in real time. Companion project: `C:\Users\User\Desktop\EB PROSTEEL AGENT`.

> ## ⚡ READ THIS ZEROTH — the job, the bar, and what failure means
> Amir defined all of this on **05/08/2026**, after grading lesson 5 a **failure**.
>
> **The job:** *"אני רוצה שתהיה **הממדל שלי** בתוכנה — שאני אדבר איתך כאילו אתה עובד איתי
> במשרד ואנחנו ממדלים ביחד מבנה פלדה ופותרים בעיות הנדסיות. שתיקח ממני את **העבודה השחורה**
> שבמידול ותגיע **לכל הפונקציות הקיימות בתוכנה** ותדע להיות הכי יעיל איתה שיש."*
> Not a tool that performs a task — a colleague who models.
>
> **The bar, in his words:**
> - *"אם אני כבנאדם יודע לבצע פעולות יותר מהר ממך — זה אומר שלא הצלחנו."*
> - *"אתה בינה מלאכותית, יש לך גישה להכל, יש לך מדריך PDF רשמי ששלחתי לך ללמידה. ברגע
>   שאתה אומר 'בטח אני מבצע' ופתאום נתקע — **זה כישלון**."*
> - *"מעניין לי את הזין כמה ברגים מידלת וכמה אלמנטים יש במודל."* **Counts are not the
>   deliverable.** Do what is required first; quantities and weights come afterwards, to draw
>   conclusions from. Never lead with a census.
> - **No promises.** *"אני מצפה ממך לכנות מלאה בתהליך ואני לא רוצה שתפזר לי הבטחות ולא
>   תעמוד בהם."* State what you will do, then let the measured result speak.
> - **פרה פרה** — one thing at a time, quality over speed of delivery.
>
> **Repetition is the whole approach — and it is BOTH things at once.** Amir corrected the
> agent for splitting them: it is a modelling method *and* a design objective, one approach
> serving efficiency and the prevention of execution and design errors.
> Why it is money: a past kindergarten-stairs job ran **7 plate thicknesses and 100 different
> plate cutting drawings with almost nothing repeating.** A shop worker handed that is *"מתכון
> מושלם לטעויות"* — plates uncut, parts mixed up. So repetition means **fewer plate types →
> fewer cutting drawings → fewer shop errors → money.** And if the engineer of record specified
> 7 thicknesses where 3 suffice, saying so **is** the value engineering the company wants.
> ⇒ **"יעילות! יעילות! יעילות!"**
>
> **And the warning that reframes every exercise so far:** *"אנחנו לא ממדלים ככה. זה מאד נדיר
> שאתן לך משימות של לפזר אלמנטים זהים בתוך מבנה מרובע שהכל סבבה ונוח. פרויקטים של פלדה יש
> להם **הרבה אילוצים**."* The 20-columns-in-a-grid exam was the **easy** case. Real work is
> irregular and constraint-driven, and the skill is *finding and imposing* repetition inside
> those constraints — not replicating what is already obviously identical.
>
> **Two axes of authority — never conflate them:**
> | | Source of truth |
> |---|---|
> | **Operating the software** — which commands/classes exist, how to drive them, what is efficient | **The documentation.** The manual (1,179 pp), 34 `.chm` files, 40 sample DWGs, `API-SURFACE-RAW.txt`. Here the agent is *expected to exceed Amir* — that is the point |
> | **What is correct for fabrication** — details, sizes, thicknesses, clearances | **Amir.** *"אלו דוגמאות שככה הוצאתי אותם לשטח לייצור והם עבדו מושלם."* Field-proven is a given, not a proposal |
> ⇒ A gap between a code clause and Amir's detail is a **question** ("what is the force here?"),
> never a correction.
>
> **Business context:** Eretz Barzel **fabricates; it does not design loads.** The engineering
> knowledge exists to influence the company's own commercial interest — spotting where a
> structural engineer over-specified plate thickness, bolt diameter or profile weight. So the
> question is never "is this safe?" but **"is it specified beyond what is required, and by what
> number?"** — and any such claim needs a code clause plus a calculation, or it is worthless in
> front of an engineer. The bar for honesty here is *higher*, not lower. *(Fuel tanks are a
> separate agent; this one is steel structures.)*
>
> **In a real project the human modeller hands over the existing situation** (concrete, site
> constraints — modelled as solids, exactly as in the lesson-5 exam) **and says what steel to
> model and how.** Surveyor DWG files exist and are deliberately out of scope for now.
> Scale is ~2,000 objects. Sharing with clients is **DWF → Autodesk Viewer**.
>
> **The failure that must not repeat:** the official manual sat extracted on this machine from
> **18/06/2026** and the agent found it on **05/08** — while being slower than Amir at the
> software. The access was never missing. **Not reading what you already have is the failure.**

> ## ⚡ READ THIS FIRST — build the detail ONCE, then replicate
> Amir: *"I modelled once and then replicated — **that is the whole principle**. You produce one
> detail that repeats, then copy and paste it wherever it's needed, and rotate or mirror it. It is
> that simple — and you go around in circles writing scripts for really simple operations."*
>
> Find the repeating unit (one column **complete** with its base plate, wall plates, holes and
> anchors), build it once, verify it hard, then **replicate** it to every location with
> `clonemodel` / `PS_COPY` / `_ARRAY` + rotate/mirror. **Never emit a loop of N identical creation
> commands** — on a 20-column job that is ~500 operations instead of ~70, seven times the work for
> the same model. When the replication tool is a dialog, find the scriptable equivalent or **ask
> Amir**; do not decompose it into hundreds of atomic calls.
> Full account: `references/build-once-then-replicate.md`
>
> **And since 06/08/2026 the software can be asked *which parts are actually identical*.**
> `PsCompareDrawing.CheckTwoPartsAreEqual(id, id)` is ProSteel's own geometric equality test,
> reachable from code with no dialog — wrapped as `op=equal` and `op=posauto`
> (`dry=1` clusters without writing). Measured: 21 parts → 15 distinct in 2.6 s.
> ⇒ **"Find the repetition" stopped being a judgement call and became a measurement.**
> Use it *before* deciding what to model once, and again afterwards to prove the model really
> does repeat: a cluster count higher than expected **is** the value-engineering finding.
>
> ⚠️ Its neighbour `PsObjectProperties.IsEqualTo` is a **trap** — it compares the nominal
> property block and cannot see cuts, holes or chamfers, so it calls a plain plate and a
> chamfered one identical. It would merge different parts onto one position number and send
> the shop one cutting drawing for several different parts. Details in `references/plugin-ops.md`.

> ## ⚡ READ THIS SECOND — look it up before you build it
> `Prg\` holds **161 DLLs, 116 of them managed: 20,802 types, 8,622 public, 24,946 signatures.**
> Filtered to what is actually the ProStructures API — `ProStructuresNet` (392) +
> `Interop.ProSteel_COM` (631, a **parallel COM API never touched**) + `PSN_*`/`PC3D*` (~200) +
> `Bentley.Structural.Ism.Api` (184, analysis interop) — that is **~1,400 relevant public types.**
> **The agent was using 26.** Nearly every hour lost in lessons 4–5 was a call to a function that
> already existed.
>
> **Before writing a loop, a helper, or any geometry by hand — search the map:**
> `EB PROSTEEL AGENT\knowledge\API-SURFACE-RAW.txt` (3.0 MB, every signature) ·
> `knowledge\API-ASSEMBLY-INDEX.txt` · guide `knowledge\API-SURFACE.md` ·
> `references/api-surface-map.md` · **what the plugin can already do:**
> `references/plugin-ops.md` (65 ops, what each is verified to do, and what is still open).
>
> ⚠️ **The dump is not the last word — the compiler is.** It hides index parameters on
> properties (`get_Entry(short)`, `get_EdgePoint(PositionSelection, PositionSelection)`) and
> renders `ref` as `Type&`. An independent check once declared `get_Entry` non-existent; it
> exists and works. **And enum values are NOT the declaration order** — `FacetType`'s usable
> values are 1–3 while `EdgeLayout`'s are 0–6, in the same assembly. **Measure every enum.**
>
> **And check your own number before you report it.** This figure was corrected three times:
> 325 (one assembly) → 771 (75 assemblies) → 8,622 (all of them, but polluted with
> `DocumentFormat.OpenXml` and `combit.ListLabel21`) → **~1,400 (filtered).** The first pass concluded
> "there is no end-plate or gusset class" — while `PSN_BasePlate.dll` and 73 other managed assemblies
> sat unopened in the same folder. **A conclusion from one sample is not a conclusion, and an
> unfiltered count is not a finding.**
>
> Useful by-products of the full sweep: `System.Data.SQLite` is present (so a part list need not go
> through an Access MDB and its 64-bit-provider problem), and part lists are **combit List & Label**
> reports.

> ## ⚠️ METRIC — ALWAYS
> Amir, 02/08/2026: *"אנחנו עובדים תמיד בשיטה המטרית — תסמן את זה בכניסה לתוכנה. תסמן metric.
> לא לסמן IMPERIAL."*
>
> **On every entry to AutoCAD/ProStructures, select METRIC. Never IMPERIAL.** Every dimension in
> this skill and in every Eretz Barzel drawing is millimetres. If a new drawing or a template dialog
> offers the choice, the answer is always metric — check `MEASUREMENT`/`INSUNITS` and the
> ProStructures unit setting (`PsUnits`) rather than assuming the default.

> ## 🛑 NO SILENT SKIPPING — Amir's formal demand, 06/08/2026
> *"זה מאד חמור בעיני... אני דורש ממך בתוקף לשנות ולשפר את הנושא הזה."*
>
> The agent committed to (1) read the manual "not skim, READ", (2) open 25 sample models,
> (3) sandbox work — then read **0.6 %** of the manual, opened **0** samples, jumped to (3),
> and reported progress. It admitted only when asked.
>
> **How Amir catches it:** *"מוזר לי שאתה עונה לי מהר ויש לך עוד כל כך הרבה עבודה לעשות."*
> **A fast reply to a long task is the tell.** The cause is optimising for a reply that
> *looks* like work: mapping 17 chapters produces a visible artifact in seconds, reading
> them produces nothing visible for an hour — so the map gets reported as if it were the
> reading. **A map is not the reading. A plan is not the doing. An index is not knowledge.**
>
> **The mechanism (not a promise — promises are banned):**
> 1. When committing to a task, state its SIZE and the ARTIFACT that will prove completion.
> 2. **No "done" without that artifact** — a file that can be opened and inspected.
> 3. If the reply is fast relative to the work claimed, **say so unprompted**.
> 4. Say plainly what was done AND what was skipped, in the same message, not when asked.

> ## 🎯 KNOW WHICH DRAWING YOU ARE IN — and make the code refuse
> On 06/08/2026 work landed in the wrong drawing **twice in one day**:
> 1. Two documents were open at once (a launch scratch plus the project). Amir saw the two
>    windows and asked which one was being worked on.
> 2. The "fix" for that closed the **sandbox** and kept the newly opened Bentley sample —
>    and every op afterwards (drill fields, copes, connections) went into the sample,
>    unnoticed, because `whoami` stopped being checked.
>
> Nothing was lost either time. That is luck, not process. **Neither good intentions nor a
> clever cleanup routine prevents this — only a gate that refuses.**
>
> **The mechanism (v39):** `eb_api.use("<file>.dwg")` pins the session; every op then carries
> `dwg=<file>`, and the plugin compares it to `MdiActiveDocument` and answers
> `EB_ERR wrongdoc expected=… active=… -- refused, nothing was executed`
> **before executing anything.** `open_project_cad()` pins automatically. `use(None)` disables.
>
> **Also:** never close a modified drawing with `Close(False)`. Save it, or refuse to close it.
> The first version of the stray-document cleanup discarded `sandbox.dwg` silently.
>
> **Later the same day it got worse: two AutoCAD *PROCESSES*.** Amir opened his own
> `Drawing1.dwg` in a second instance while the agent worked in `sandbox.dwg`, and said
> plainly: *"זה קובץ שאני עובד עליו — אל תסגור אותו."* Three separate things broke:
> 1. `GetActiveObject("AutoCAD.Application")` returns whichever instance registered in the
>    Running Object Table **first** — not the one being worked in. Both instances publish the
>    **same** class moniker, so they cannot be told apart by name, only by asking each one
>    which drawings it holds. ⇒ `eb_api.acad_instances()` + `_app()` now **choose by the pin
>    and refuse rather than guess**.
> 2. `_close_stray_docs` closes anything that is not `keep_name` — which, attached to the
>    wrong instance, would have **saved and closed Amir's working file**. It now refuses
>    outright whenever a second `acad.exe` is running. The pin is the protection; closing
>    documents was a workaround for a problem that no longer exists.
> 3. The screenshot tool photographed **his** window under **my** title (see 👁️ below).
>
> ⇒ **The user's own drawing is not a stray document. Never close what you did not open.**

> ## 🔇 A SILENT PARAMETER IS A SILENT FAILURE — v48
> Four plates were created with `at="11000,0,0"` and every one landed on the **origin,
> stacked**. `plate` takes `center`, not `at` — and the dispatcher swallowed the unknown key
> without a word. **Every call returned `EB_OK`.** The model was wrong, the log was clean, and
> the only reason it surfaced was a screenshot showing the view centred on 0,0.
>
> Same family as a `Void`-returning create method: *the absence of an error is not evidence of
> an action.* It has now bitten four times — `CreateSingleBolt`, `PsCreateFastener`,
> `set_Facet`, and this.
>
> **The mechanism:** the plugin holds a table of the keys each op actually reads, and answers
> `EB_ERR unknown parameter(s): at,plane -- op=plate accepts: center,ex,ey,ez,l,layer,normal,t,w`
> `-- refused, nothing was executed`.
> **The table is GENERATED from the source** (`Get(kv, "…")` per op method). Regenerate it
> whenever ops change — a hand-maintained list drifts and starts lying, which is worse than none.
> ⇒ **A misspelt parameter must be exactly as loud as a misspelt op.**

> ## 👁️ SHOW HIM — the model is not the only thing that has to be true
> Amir: *"תן לי איזה התראה קטנה בכל פעם כשאתה מתחיל למדל — אני רוצה לראות את זה בלייב...
> אל תעצור את הסשן, רק תודיע."* Announce **before** each model-modifying operation; never
> stop and wait.
>
> Then a chamfer worked — facet created, read back, measured — and he still said *"אני לא רואה"*.
> **An operation that succeeds outside the current view is indistinguishable from one that did
> nothing.** Proving it to yourself by reading the model back, and proving it to him by pointing
> the camera, are **two different obligations**.
>
> `op=view dir=iso|top|front|…` · `op=zoom handle=… | all=1` · `op=hilite handle=…` (v47),
> and `app/eb_shot.py` for the picture. All native — no LISP, no widening of the `cmd` allowlist.
>
> **The screenshot lied twice before it was trusted once:**
> - It grabbed *"the first `acad.exe` process"* → photographed Amir's `Drawing1.dwg`. ⇒ pick the
>   window **by title**, defaulting to the pinned drawing.
> - `SetForegroundWindow` **fails silently** when another process owns the foreground — i.e.
>   exactly when the user is working — so `CopyFromScreen` returned *his* pixels inside *my*
>   window rectangle. ⇒ use **`PrintWindow`**, which asks the window to draw itself, needs no
>   focus and does not interrupt him; fall back to a screen grab **only** after confirming the
>   target is really in front, otherwise **refuse and say why**.
>
> ⇒ **A picture of the wrong window is worse than no picture** — it is evidence for a false
> conclusion. Rule zero still holds: a screenshot is for *him*, never proof for *you*.

> ## 📖 THE MANUAL IS THE API DOCUMENTATION — read it before reaching for reflection
> Seven chapters were read end to end on 06/08/2026 and every one of them corrected
> something that reflection alone had left wrong or unknown:
> `B.12.6` cope · `B.13` plate editor · `B.14` drilling · `B.17` plate connections ·
> `B.18` base plates · `B.28` groups · `B.29` positioning.
>
> **Every dialog field is a property.** `Create Group`→`CreateGroup`, `Gap`→`DistanceToSupport`,
> `Rotate Connection`→`PlateIsRotated`, `Facet Horizontal`→`TopHaunchPlateFacetDistance1`.
> Reflection gives the **names**; only the manual gives the **semantics** — what happens when
> `Middle = 0`, which parameter is a *bolt* diameter rather than a hole diameter, what a
> prerequisite is, what silently does nothing. **All 1,179 pages are API documentation.**
>
> **Full digest: `references/manual-findings.md`.** Chapter notes:
> `EB PROSTEEL AGENT\knowledge\MANUAL-NOTES-*.md`. Read the digest before designing an op.
>
> Five things the manual changed that measurement then confirmed:
> - **`Diameter` in a drill field is the BOLT diameter** — hole = bolt + `Workloose`.
>   Measured: `dia=23,play=3` → a **⌀26** hole. Correct call is `dia=20,play=3` → ⌀23.
> - **Base-plate shortening = plate thickness + grout** (measured 20 and 55), not thickness.
> - **A cope creates no objects** — it shortens the beam (4850→4840). Census is the wrong
>   instrument; `Create()` returned **False** while it had worked.
> - **A hollow section drills ONE WALL** without `SetIgnoreInnerContour` (8 mm vs 200 mm) —
>   and the hole **count is identical either way**.
> - **`LongHoleMode` is a READ mode.** Only `kDoubleHole=2` reveals a slot's length; at `0`
>   a slotted hole reports as a plain round one, so the verification instrument was blind.

> ## 🔊 ProSTEEL DIAGNOSES ITSELF ON A CHANNEL THE API NEVER RETURNS
> A call stored its record, raised nothing, produced no geometry — and the **AutoCAD command
> line** said `* WARNING REQUESTED VOLUME SOLIDS CAN NOT BE PRODUCED`. That was seen only
> because a screenshot happened to include it. **Every silent-failure investigation before
> 06/08/2026 was run blind to this** — `CreateSingleBolt`, `PsCreateFastener`, `set_Facet`,
> `TakeoverDrills`. Some of them may have been explaining themselves all along.
>
> ```python
> import eb_log; eb_log.enable()
> m = eb_log.mark()          # BEFORE the operation
> eb.run(...)
> eb_log.problems(m)         # just the complaints
> ```
> ⇒ **Bracket every experimental operation.** A silent failure that explains itself is a
> five-minute fix; one that does not is a five-rebuild investigation.
> *(It diagnosed the edge-chamfer failure instantly, and told me why a miter was refused:
> "Cut was not performed. Due to an existing cut this would be useless.")*

> ## 🏛️ INSERT SHAPES PROPERLY — the insertion point, and the grid (B.8)
> **A profile is not just "between two points".** The insertion dialog carries an insertion
> POINT — the grid of dots drawn on its monitor — and it decides where the section sits relative
> to the line. Measured on an HE300B with the *same* two points: default centres it (y −150…150);
> **`xpos=kLeft`** puts the line on the left face (0…300); **`ypos=kTop`** puts it on the **top**
> face (z −300…0). ⇒ **That is how a beam's top flange lands on a level line** — not by computing
> offsets. `SetStartOffset`/`SetEndOffset` trim or (negative) **extend** each end;
> `SetRotation` is the dialog's *Turn*; `SetDirection(vector, length)` overrides the points.
>
> ⭐ **And orientation is a documented rule, not a mystery:** two points perpendicular in the WCS
> ⇒ **alignment follows the WCS x-axis**; free in space ⇒ x-axis as parallel as possible to the
> WCS xy-plane. `SetXAxis`/`SetYAxis` override it. *(A day was spent discovering this by
> measurement before the chapter was read.)*
>
> ⭐ **Twenty columns on a grid is ONE operation.** Build the work frame with real bay steps, then
> put a column on every joint: `gridcolumns joints=20 created=20 failed=0 … secs=0.0`.
> **The lesson-5 exam was exactly this**, and the replication-loop version produced 76 duplicates
> and an invalid model. Reach for the frame before reaching for a loop.

> ## 🔩 BOLTS FOLLOW HOLES — Amir's daily sequence, and the 400-failure mistake
> Amir, 06/08/2026: *"אני מייצר חורים בהתאם למה שאני צריך בפקודת DRILL, ולאחר מכן בוחר את
> 2 החלקים והתוכנה יודעת לתת אוטומטית את ברגי החיבור ביניהם."* And: *"אנחנו כמעט תמיד
> משתמשים בברגים רגילים, אין פה משהו מיוחד."*
> Manual B.15.1 says the same: *"the components are bolted automatically after part selection…
> **the holes in the component parts are analysed** and the corresponding bolts are inserted."*
>
> **`PsCreateBolt` has two paths, and every earlier attempt used the wrong one.**
> `CreateSingleBolt(start, end, dia, style)` is MANUAL — you supply the grip length, it returns
> `Void`, and a grip with no row in the bolt table fails **silently**. That is the whole story
> behind ~400 failed bolts and the 428 mm anchors.
> The right path is **`AddObject(id)` per part → `Create()`** (`op=boltparts`): the software
> reads the holes and derives the bolts.
>
> ⇒ **Model → DRILL → select the parts → bolts appear.** Never ask for a bolt of a given length.
> Verified end to end: an IPE300 with an end plate bolted to an HE300B flange — 4 holes each
> side meeting at the contact face, **4 bolts**.
>
> When bolting yields nothing it is a **refusal**, not a break: the parts are further apart than
> `MaxObjectDistance` (the dialog's *"Gap distance"* — **not** `MaxCenterDistance`, measured), or
> their holes differ in angle by more than `MaxDeclination`.

> ## 🔴 NO RETURN VALUE MEANS SUCCESS — check with a FRESH read-back
> In one DLL: `readFrom` → **0 is OK** (`eOk`) · `PsCutObjects.Apply()` → **1 is OK** ·
> `Create()` → **false on a group that exists** · `CreateFastener*` → the `Int64` **is** the
> new id and **0 means refused** · `TakeoverDrills`/`SetLinearHoleField` → `Void`.
> Four conventions, one assembly. Two ops were broken by *inventing* a convention: `posset`
> refused to write because it read `0` as failure, and `anchor` reported "created nothing"
> while discarding the very id that was the answer.
> ⇒ **Never infer success from a return value.** Re-read the model in a **brand-new** object —
> the one you wrote from still holds your value in memory and will happily confirm a write
> that never landed.

> ## 🪟 A CREATOR CAN REPLACE THE OBJECT, NOT CHANGE IT (B.9.4, measured 08/08)
> `PsCreateBendPlate.SetObjectId(flat) + Create()` returns **`false`** and yet **succeeds** —
> it **erases the flat plate and makes a new entity**. Every later call on the original handle
> throws `eWasErased`. `CreateOfTwoPlates` does the same: two plates in, one out, **both**
> originals erased.
> ⇒ After any conversion, **re-acquire the handle** (`cb.ObjectId`, or a model-space census
> diff) before doing anything else. A workflow that holds an id across a conversion is already
> broken, and the symptom appears one call later — where the cause is invisible.

> ## 🔢 A COUNT PROPERTY CAN LIE — and a unit can change mid-API (B.9.4, measured 08/08)
> - **`PsBendPlate.FlangeCount` counts only TOP-LEVEL segments.** A plate with 2a and 2b on the
>   base and 3 on top of 2a reports **2**, not 3. Loop to the count and every subordinate
>   segment is invisible. Scan **past** it; the terminator is `GetGripPoints` throwing
>   `NullReferenceException`.
> - **`AddFlange` takes DEGREES; `PsBendPlateFlange.Angle` returns RADIANS** (45 in → 0.785 out).
> - `get_ParentFlangeIndex(int)` is **indexed** — the type dump prints it as a plain property.
>   When the dump and the compiler disagree, the compiler wins. It is the dependency tree:
>   `-1` = attached to the base plate, otherwise the parent segment's index.

> ## 🧭 THE DIVIDING LINE: `Connection.Standard` CREATES, the rest does not (measured 09/08)
> Four standalone creators tested in one day, all refusing, against four connection classes that
> all work:
>
> | class | namespace | result |
> |---|---|---|
> | `PsWebAngleConnection` · `PsShearPlateConnection` · `PsSpliceJointConnection` · `PsStiffenerConnection` | `Connection.Standard` | ✅ **create cleanly** |
> | `PsCreateWeldFlag.Create()` | `Annotation` | ⛔ **false** — yet a splice made 32 `Ks_WeldFlag` |
> | `PsGussetConnection` | `StructuralObject` | ⛔ **no creator at all** |
> | `PsBracing.insert()` | `StructuralObject` | ⛔ **false** in five configurations |
>
> ⇒ **Reach for a connection class first.** When something in `StructuralObject` or `Annotation`
> is needed, expect it to be dialog-only until proven otherwise, and budget for that.
> ⭐ Diagnostic that separated cause from symptom on `PsBracing`: a **read-back taken immediately
> before `insert()`** returned exactly what had been written (`cat`, `size`, `shapeType`). The
> setters work; only the creation step refuses. Without that read-back the natural guess would
> have been a bad section key, and hours would have gone into it.
> ⛔ **The UCS hypothesis is REFUTED.** The manual insists the UCS must be over the bracing plane;
> Amir approved adding `UCS` to the allowlist, but the fix was then implemented through the
> managed `Editor.CurrentUserCoordinateSystem` — no command line, nothing left pending — with the
> restore in a `finally` so a crash cannot leave the frame rotated. Result:
> `ucsSetToPlane insert()=False ucsRestored`, and the drawing confirmed `UCSORG (0,0,0)` after.
> **Six configurations tested; `PsBracing.insert()` never fires.**
> ⭐ Two things worth keeping from this: **ask before widening a safety control** (Amir approved
> in one line), and then **prefer the managed property over the command line** — it is safer than
> what was asked for, and the `finally` makes the restore unconditional.
>
> ⭐ **B.24 answers B.23:** *"The entire bracing **including gusset plate** is generated."* A gusset
> has no creator because it is not a standalone object — it is something a bracing *has*. But that
> route is not reachable from code either, so a gusset stays dialog-only for now.

> ## 🪛 A BOLT WITH NO HOLE IS A SELF-DRILLING SCREW — AND ONLY AMIR DECLARES IT
> Amir, 09/08: *"זה נקרא **בורג קודח**… בורג שקודחים אותו לתוך הפרופיל מבלי לקדוח חור בטרם
> ההחדרה… חיבור שמתבצע בדרך כלל **בשטח**… ממדלים אותו **על מנת להבין כמה ברגים אנחנו צריכים
> להזמין** וכדי שהפרט הזה **לא יחמוק מאיתנו**."*
> A self-drilling screw cuts its own hole, so its missing hole is **correct**, not an omission.
> ⚠️ **In the model the two cases look identical** — a bolt with no hole. Only the intent differs.
> ⇒ The standing rule (a bolt through steel without a modelled hole is a **critical error**) still
> holds for ordinary bolts. The self-drilling case is the single exception, and
> **"אני רוצה שנגדיר את זה רק באישור של מי שממדל איתך — כלומר הממדל האנושי."**
> **Never create one on your own initiative, and never use one to paper over a failed bolting.**
> If a situation looks like it wants a self-drilling screw — **ask in one line.**

> ## 🔩 B.15 SETTLES THE BOLTING SEQUENCE (measured 09/08)
> The manual says components no longer have to be drilled first. **Not from this API.** Two
> overlapping plates with zero holes → `boltparts` gives `holesOnParts=0 create=False`. The same
> pair drilled first gives 2 holes and 2 bolts.
> ⇒ **`PsCreateBolt.AddObject + Create()` requires existing holes.** Amir's sequence — DRILL,
> then pick the two parts — is not merely his habit, it is **the only route available to code**.
>
> Three routes worth knowing, all working, all taking the style **by name**:
> `CreateSingleBolt(start,end,dia,style,addLen)` — by **grip length** ·
> `CreateSingleNut(...)` — a nut/washer with **no bolt** ·
> `CreateThreadedRod(start,end,dia,offset,style)` — offset projects at **both** ends
> (600 span + 2×30 measured as 660).
>
> ⚠️ **`BoltStyle`, `BoltType`, `Diameter` are WRITE-ONLY** — the dump shows plain properties,
> the compiler says no getter. And **`BoltStyleName` is INDEXED**: `get_BoltStyleName(int)`, the
> enumeration route, invisible in the dump. Third case today after `get_ParentFlangeIndex(int)`
> and `get_WeldStyleName(int)`. ⭐ **When a `String`/`Int32` property looks like it should be a
> list, try the indexer.**
>
> ⭐ And the two numbers that decide whether a bolt forms at all:
> **`MaxObjectDistance` = the dialog's `Gap distance`** — *"the maximum distance between two holes
> assumed to belong to the bolting; if exceeded the holes cannot be bolted"* — and
> **`MaxDeclination` = `Angle difference`** — *"if exceeded, the holes don't align"*.
> ⚠️ One error message, two causes: a missing style and out-of-range holes read the same. Check
> the geometry, not the last thing that broke.

> ## 🕳️ THE DRILL PICKS A FLANGE BY PARAMETER, NOT BY YOUR POINT (measured 09/08)
> Two identical end-plate connections, one at each end of the same beam. One bolted, one refused
> — while **both reported 6 holes on the plate and 6 on the column**. Reading the hole
> COORDINATES showed why:
> ```
> end A ✓  plate  8121.6→8101.6    column  8101.6→8090.6    one continuous hole
> end B ✗  plate 11898.4→11878.4   column 12101.6→12090.6   ← the FAR flange, 200 mm away
> ```
> `PsDrillObject` chose the wrong flange on the second column. The choice comes from the
> **`flange` parameter (0 top · 1 down · 2 both)** and defaults to the same local face every
> time — which is the near flange on one side of a frame and the far one on the other.
> ⇒ **Always pass `flange=` when drilling a shape.** And note the failure mode: the counts were
> perfect. Only the coordinates were wrong, and only the bolts refusing exposed it. **Count is
> not position.**
> ⛔ Holes cannot be removed, so the fix was to delete and rebuild the column.

> ## 🏗️ TWO STABLE BEAM-TO-COLUMN SCHEMES, BUILT AND MEASURED (09/08)
> Amir, on a frame built as three touching members: *"מסגרת כזו… היא דבר שאינו ניתן לביצוע — אין
> כאן סכימה יציבה של הקורה על גבי העמודים."* He was right: it had no connection at all.
>
> **Welded** — `HE200B` columns, `IPE300` beam. The beam butts **square against the flange face**
> (measured `y 8100..11900` against faces at exactly 8100 and 11900 — zero gap, a weldable
> fit-up). What makes it stable is not the weld: **8 stiffeners**, a pair in each column web
> opposite each beam flange, or the web folds. Those are creatable (B.16) and are what a checker
> looks for.
>
> **Bolted** — `UC203x203x46` columns, `UB305x165x40` beam. A 20 mm **end plate** welded to the
> beam end and bolted to the column flange, **12 × M16**, six per end in three rows. ⭐ The beam
> must be **shortened by one plate thickness at each end** so the plate — not the beam — lands on
> the flange.
>
> ⇒ Run the beam **along Y** to meet a flange FACE. An HE/UC column's flanges span X at
> `y = ±h/2`; a beam along X meets only the flange **tips** and the open space between them.

> ## 📏 BOLT GROUPS THAT CONVERGE ON ONE NODE MUST BE SPACED (measured 09/08)
> Three members bolted to one gusset, inner rows at 100 mm along each: the hole groups came
> within **11 mm** of each other, ProSteel **merged them**, and `boltparts` produced **14 bolts at
> 10 positions** — two positions carrying three bolts. `create=True` and the counts looked
> plausible; only listing bolt centres exposed it.
> ⇒ Keep hole centres **≥ ~40 mm apart for Ø18** (≈2.5 d). Moving the rows out to 170/260 and
> 290/380 gave a closest spacing of **65.4 mm** and a clean 4+4+4.
> ⛔ **A hole cannot be removed once drilled.** Fixing a bad hole layout means **deleting and
> rebuilding the parts**, not editing them. Plan the pattern before drilling.
>
> **Members converging on a node collide unless one is set back.** A 45° diagonal leaving the same
> point as a 160-deep horizontal has its lower corner 80 mm perpendicular below its axis, i.e. at
> `(axis+56.6, axis−56.6)` — inside the horizontal. Setting that corner on the horizontal's face
> gives the setback: here **t ≥ 193**, so the diagonals start at t = 200 and the gusset lengthens
> along them to keep the bolts on plate.
> ⇒ **Prove it, do not assume it:** `collision box='x,y,z;x,y,z'` (a box, **not** handles) →
> `parts=17 collisions=0`.

> ## 🔩 CHANNEL ORIENTATION — where a UPN's web actually is (measured 09/08)
> Viewed **along its own axis**, a `U160` from `DIN_U` opens its C towards **+Y**, so the **web
> sits on the LOW-Y edge** of the 65 mm envelope. To seat that web on a gusset face at `y = G`,
> put the member axis at **`G + 32.5`** (half the flange width). Verified: all three channels came
> back at `y 20006..20071` against a gusset face at 20006.
> ⇒ A section's envelope tells you the size, never which side the web is on. **Look along the
> axis once** — one `view dir=right` + zoom settles it for the whole family.

> ## ✂️ A SHAPED GUSSET IS JUST A POLYGON (measured 09/08)
> Amir: *"את הפלטה האנכית תעשה אותה מקומטת, לא מלבנית… שייראה יותר אסטתי המחבר."*
> `plate9 mode=poly` with 8 points, cut square across the end of each member and tapering between
> them. The corners come straight from the geometry: for a member with unit axis **u**, unit
> perpendicular **p** and connection reach **L**, its two end corners are **`L·u ± (d/2)·p`**.
> ⚠️ Polygon points are in the **insertion frame**, not WCS — set `ex`/`ey`/`ez` and give local
> `(u, v, 0)`.

> ## 🔧 BUILDING A BRACING CONNECTION BY HAND — the parts that work (measured 09/08)
> Amir's detail, built and verified four times: a **plate welded to the column WEB** (not the
> flange — he corrected this) sitting between the flanges, a **gusset perpendicular to it**
> sticking out through the flange opening, and the bracing angles **bolted to the gusset**.
> - **`boltparts` fails SILENTLY without an explicit `style`.** `style='(default)'` →
>   `create=False`, zero bolts, and the plugin's own hint blamed the gap distance — wrongly.
>   `style=DIN7990` → `created=2` immediately. **Always pass a bolt style.**
> - **`drill hosts=A,B`** puts one hole through both parts in a single call — the right way to
>   make a shared hole. Verified: gusset 0→6, each angle 0→2.
> - **`replicate` preserves holes AND bolts.** 12 entities × 3 copies, every copy read back with
>   the same 6/2/2/2 hole counts and 6 bolts. Nothing silently dropped.
> - Seating rule that made it fit: an L90x9's envelope is 90 wide **centred on its axis**, so to
>   land its face on a 12 mm gusset straddling y=0, put the axis at **y = 6 + 45 = 51**.
> ⛔ **Welds are NOT creatable standalone.** `PsCreateWeldFlag.Create()` returned **false** in
> three variants (no style / with sign / `makeweld=0`), and `WeldStyleCount` reads **0** while
> the style list reports **4**. Yet B.21's splice produced **32 `Ks_WeldFlag`** as a by-product.
> ⇒ Same pattern as everything else today: **the connection classes work, the standalone
> creators do not.**

> ## 🕳️ A SETTER THAT DOES NOTHING, AND A CHAPTER WITH NO WAY IN (B.23, measured 09/08)
> **`PsWebAngleConnection.SetKey()` / `SetKatalog()` are complete no-ops.** Four connections —
> a valid `BS EQUAL` key, a valid DIN key in two catalogue-name forms, and **no key at all** —
> every one produced `L90X9` from `DIN.DIN_WINK_GL`. The section comes from the **template**.
> This was caught only because `EA90x90x9` turned out **not to exist** (`BS EQUAL` has 6, 7, 8,
> 10, 12) and the connection had accepted it silently. An earlier note claiming that section was
> wrong and has been corrected. ⇒ **Setting a value is not evidence it was used.**
> ⚠️ **Catalogues have TWO names**: `DIN WINKEL GLEICH` to look up, `DIN.DIN_WINK_GL` as stored.
> Neither string works in the other place. Same for `DIN FLACHEISEN` ↔ `DIN.DIN_FLACH`.
>
> **`PsGussetConnection` has no creator at all** — getters and setters only, no `Create()`, no
> `insert()`, no template API, no COM twin. A gusset can be read and edited, never created.
> The boundary in `StructuralObject` is worth knowing before planning work:
> ✅ `PsCreateHandrail.Create()` · `PsBracing.insert()` · `PsLadder.insert()` ·
> `PsPortalFrame.init()+insert()` — ❌ `PsStairs`, `PsCircularStairs`, `PsJoist`, `PsTruss`,
> `PsGussetConnection`.

> ## 🧱 WELDS ARE OBJECTS, AND A SPLICE IS NOT WHAT THE CHECKBOXES SAY (B.21, measured 09/08)
> - **Six plate checkboxes produce EIGHT plates.** `Upper Inside` and `Lower Inside` each become
>   **two narrow strips**, because the web crosses the inner flange face. 2 outer + 4 inner +
>   2 web. The manual never says this.
> - **`Weld` converts the whole splice**: 0 bolts and **32 `Ks_WeldFlag` objects** appear instead
>   — eight per plate. **`Ks_WeldFlag` is a real entity class**, not annotation. The plate also
>   shortens (298 → 288): a welded splice takes its length from the `Length` field, not from the
>   bolt pattern, so it is not "the bolted one minus bolts".
> - **`holeFields` counts plate GROUPS, not plates**: 4 plates (flange + web) → 2 fields;
>   web-only → 1.
> - ⚠️ **`HoleWorkloose` here vs `HoleWorkLoose`** on the web-angle and shear-plate classes. One
>   capital letter, three classes, same concept. And this class has **no `BoltStyle` string** at
>   all — only the CRC — while its two siblings expose both.
> - ⚠️ **The connection database is empty on all three classes** (`get_PlateDataCount() = 0` for
>   web angle, shear plate and splice). It is a product-wide gap on this installation, so any
>   load-driven selection is unavailable.

> ## 🎭 A CHECKBOX CAN CHANGE THE ENTITY CLASS (B.19 + B.20, measured 09/08)
> Two consecutive connection chapters each hide this:
> - B.19 **`Use Flat`** → the cleat is **`Ks_BendShape`**, not `Ks_Shape`
> - B.20 **`Poly-Plates`** → the plate is **`Ks_Plate`**, not `Ks_Shape`
>
> Geometry identical (70×10×135 either way in B.20). Only the *kind of part* changes — and with
> it what the parts list orders: B.20's default product is a **catalogue flat bar**, which is
> what makes its `Turn Flat` (FL 110x10 → BRFL 250x10) a real purchasing decision.
> ⇒ **Never audit a model by entity class alone.** A query for `Ks_Shape` misses both.
>
> The same pair also cut the beam **differently**: web angle → `polyCuts` 0→1, shear plate →
> `cutPlanes` 0→1. Both drill (`holeFields` 0→1). Looking for one will not find the other.
>
> ⛔ **The `Cope` page does not work from either class.** `CreateCope = true` plus real geometry
> plus a template name validated by `CheckCopeTemplate('default/Standard') = True` still left the
> beam byte-identical. Proven on both B.19 and B.20. `PsCopeConnection` is the remaining route.
> ⭐ Cope templates use the **`default/<name>`** convention — the same as connection templates.
> ⚠️ **The load database is empty product-wide** (`get_PlateDataCount() = 0` on both classes), so
> the `H(kN)`/`Hz(kN)` selection and the `MaH`/`MaHz` capacities have nothing behind them here.

> ## 🔗 ONE COMMAND CAN BE THE WHOLE DETAIL (B.19 web angle, measured 09/08)
> `PS_STEGW` / `PsWebAngleConnection` cuts the beam to length, makes both angles, drills both
> legs, and bolts them — from one call. Measured on the beam with `mods`: `holeFields` and
> `polyCuts` both went **0 → 1** with nothing else asked for. Product per connection:
> **2 angles + 6 bolts** (2 rows) or **2 + 9** (3 rows), and the **angle length is derived from
> the bolt count** (135 vs 210 — the 75 difference is the template's own vertical pitch).
> ⇒ Reach for the connection class before building a detail out of B.14/B.15 primitives.
> ⭐ **`Use Flat` changes the ENTITY CLASS** to `Ks_BendShape`, not `Ks_Shape`. **An audit that
> counts only `Ks_Shape` will not see those cleats.**
> ⛔ Three limits, all measured: no support shape ⇒ `Create()` false (the manual's ENTER-for-none
> case is not reachable this way); **`CreateCope` is inert even with real cope geometry** — a
> dedicated `PsCopeConnection` exists and is the likely route; and `GetPlateId` returns nothing
> because a web angle **makes no plates at all**.

> ## 🧩 A RIB IS DERIVED, NOT DRAWN (B.16, measured 09/08)
> Lesson 3 built **214 ribs by hand** as polygons. `PS_RIP` derives them from the girder:
> ```
> width  = (b − tw)/2 − webDistance − offset,  then TRUNCATED to `RoundTo`
> height = h − 2·tf − 2·flangeDistance
> ```
> verified to the millimetre on HE300B (W 140, H 260) and IPE400. The corner cut equals the
> section's own root radius, because **`Radius = 0` means "import the shape radius"** — not
> "no radius". `Offset` **negative** makes the rib project past the flange. `RoundTo` truncates
> the width onto a stock flat size — a fabrication control, not a drafting one.
> ⭐ **One command makes TWO ribs** on a symmetric section (`created=2`, 33/33 insertions).
> ⛔ **A rib cannot be created without a template** (`Create()` false, nothing made) — the same
> template-only rule as anchors. And `Check()` returned `1` both on success and on failure.

> ## 🎭 A PROPERTY CAN BE WRITE-EFFECTIVE AND READ-BROKEN (B.16, measured 09/08)
> `PsStiffenerLinkDataMgd.LengthType` **works when set** (0 Full · 1 Half · 2 By Length ·
> 3 Square — measured against web height) but **lies when read from a template**: `half convex`
> and `full convex` both report `1`, yet build a half and a full rib respectively. All seven
> shipped templates were built and measured — **the NAMES are right every time, the exposed data
> is not.**
> ⇒ Reading a configuration object proves nothing. Only the model does.

> ## 📐 SAME-TYPED `ref` PARAMETERS HIDE A WRONG ORDER (B.16, measured 09/08)
> `PsPolygon.getVertexbyValue(Int32, Double&, Double&, Double&)` is documented `(X, Y, Bulge)`
> and actually delivers **`(bulge, y, 0)`** — no x at all. Three parameters of one type, so the
> **compiler cannot catch the mistake** and the numbers look plausible.
> Also: **`getVertexAsPoint` returns the BULGE in `PsPoint.z`.**
> ⇒ A plate contour's shape lives in the bulges: chamfered = `0`, convex = `+0.414`,
> rounded = `−0.414` (`bulge = tan(θ/4)`, so a quarter circle). **Vertex count cannot tell three
> corner layouts apart** — all three give 7 vertices and identical extents.
> When several parameters share a type, read the same value a **second way** before believing it.

> ## ⛔ `PsPlate.computeObjectWeigth()` KILLS AUTOCAD
> Process gone, no exception, no dialog. Reproduced twice; the second run wrote a marker file
> immediately before the call and it survived reading `about to run: weight on 501`.
> The first B.9 session lost five plates to it.
> ⇒ **Never call it.** And when a read-back routine makes many calls, have it write what it is
> **about to** do — a crash returns no result, so the only evidence is what was recorded first.

> **Rule zero: nothing here is claimed without measurement.** Every statement below was verified by
> reading the value back out of the model (`PsSingleHoleArray` for holes, `GetPolygon` for shapes,
> `PsEditLogicalLink` for joints). A screenshot of a bolt crossing a plate looks *identical* with or
> without a hole — so a screenshot is never proof. This rule exists because the agent was caught
> three times reporting success that was not there.

---

## The one idea that matters most

**The unit you model is the CONNECTION, not the object.**

A steel joint in ProSteel is a **parametric object** — a "logical link" that sits on a part and owns
its own recipe: plate sizes, hole diameter and spacing, welds, bolts, rib shape. The plates and bolts
you see are *generated from* that recipe.

Model plates and cylinders in the right places and you produce **the shadow of a connection**: it
looks plausible, but it has no holes, no welds, no relationship between parts — and the part list and
CNC output come out wrong. Amir's verdict on such a model: *"a bolt that just passes through and
looks like it drills is a critical error."*

So: **run the connection macro and give it parameters.** Hand-modelling is the fallback, never the
default.

**But the macro is not the criterion — correctness is.** Amir, teaching wall anchorage: *"base plates
to the floor are done through BASEPLATE DAST. I don't know of anything for producing base plates to a
wall, so I made an ordinary one and copied the anchor bolts onto the plate. **As long as the model is
modelled correctly and there is no mistake, that is also a good way.**"* Where a dedicated tool exists,
use it; where none exists, an accurately hand-modelled plate with copied bolts is fully legitimate.
Never force a macro onto a detail it was not made for.

| Need | Tool | API class | Params |
|---|---|---|---|
| Base plate to floor | `PS_GROUNDPL` | `PsBasePlateConnection` + `PsBaseplateLinkDataMgd` | 35 |
| End plate, moment plate, haunch | `PS_ENDPLATE*` | `PsStandardPlateConnection` | **132** |
| Fin plate / shear tab | `PS_SCHEARPLATE` *(their typo)* | `PsShearPlateConnection` | 64 |
| Double-angle web cleat | `PS_STEGW` | `PsWebAngleConnection` | 74 |
| Cope / notch a beam end | `PS_NOTCH`, `PS_NOTCH_MAN` | `PsCopeConnection` | 37 |
| Haunch | `PS_VOUTE` | `PsHaunchConnection` | 19 |
| Purlin cleat | `PS_PFETTE` | `PsPurlinConnection` | 27 |
| Splice between collinear members | `PS_LASCHE` | `PsSpliceJointConnection` | — |
| Stiffener / rib | `PS_RIP`, `PS_RIP_ANGLE` | `PsStiffenerConnection` + `PsStiffenerLinkDataMgd` | — |
| Gusset for a brace | `PS_GUSSET_PLATE` | ⚠ `PSN_DualGusset.dll` / `PSN_WraparoundGusset.dll` — managed, not yet reflected | — |
| **Anchor bolt / stud into concrete** | `PS_ANCHORBOLT`, `PS_SET_ANCHOR` | **`PsCreateFastener`** — Straight / Hook / Bend / HeadBolt / HexStud / RoundStud | — |
| Hole, incl. oblong | `PS_DRILL` | `PsDrillObject` (31 methods) | — |
| Read any joint's full recipe | `PS_EDIT_CONNECTIONS` | `PsEditLogicalLink` | — |
| Read any part's properties | PS Properties | `PsObjectProperties` | — |

**All six beam-connection classes share one identical shape** — `SetToDefaults` →
`GetTemplateCount`/`GetTemplateName`/`GetTemplate` → change only what the drawing says →
`SetConnectionObjectId` + **`SetSupportObjectId`** + `SetConnectionPoint` → `Check()` → `Create()` →
read back with `get_PlateDataCount`/`GetPlateId`. Learn it once, it works for all of them.
`SetSupportObjectId` is the thing a base plate does not have: **a beam connection knows two members.**

References: `references/connections-as-objects.md` · **`references/beam-connections.md`**

## What a real Eretz Barzel model contains

An access-platform model (175 members) carried **92 joints on 82 parts**: Brace Plate ×39 ·
Endplate ×17 (15 of them = 2 plates + 4 bolts each) · Conn.Shape ×11 · Baseplate ×3 · cuts ×8.
It also carried **713 modelled holes** — and **284 of those were in PROFILES, not plates.**

Hole diameters in that model: ⌀23 ×176 · ⌀19 ×236 · **⌀15 ×37 · ⌀14 ×14 · ⌀13 ×190 · ⌀12 ×60**.
So **301 of 713 holes were M10/M12** — because a 40×2.5 SHS guardrail or a ⌀21 ladder rung does not
take an M16. **Bolt size follows the element and the force, never one default for the whole model.**

### ⭐ Use the whole catalogue, not three sections

Amir, 07/08/2026: *"תגוון קצת עם הפרופילים — אל תתקע רק על HEB או IPE. תנסה לגוון קצת ולצאת
מהקופסא."* Clarified: *"כל החתכים נמצאים בתוכנה — רק ביקשתי שתנסה להיחשף אליהם יותר ולנסות לעבוד
עם כמה שיותר סוגים."* So it is a **working habit**, not a discovery: reach for the section the
element actually wants.

**357 catalogues are installed.** Angles (equal, unequal, and **double/quadruple** — `DIN WINKEL
HD/VD/4T/DIA`), channels (`DIN_U`/`UPE`/`UAP`), hollow sections hot and cold (`RQ`/`RR`/`RO`),
**split tees** (`DIN_HALBE IPE` → `HIPE300`), rolled tees (`T80`), heavy columns (`Peiner_HD`/`HL`),
cold-formed purlins (`Z160`, `C150x75x6,5`, `METSEC_C`…), flats, round and square bar, threaded rod,
bulb flats, even timber. Full map + traps: `knowledge/SECTION-CATALOGUES.md`.

⚠️ **A section key is an opaque string — look it up, never construct it.** `HD260x68,15` uses a
German decimal **comma** while `RO 219.1x6.3` in the same library uses a **point**; `DIN HALBE IPE`
does not exist but `DIN_HALBE IPE` does; and what you pass in is **not** what gets stored
(`DIN_QUADRATROHR` → `DIN.DIN_QUADROHR`, keys upper-cased), so a dumped model cannot be fed
straight back. Verify every planned key against `op=dumpcat` before building.

## Amir's confirmed practice (source of truth, not guesses)

- **M16 → hole ⌀19 · M20 → hole ⌀23** — 3 mm clearance, the shop's practice (EN 1090-2's default is
  2 mm; a declared shop practice wins over the standard's default). **This applies to steel-to-steel
  bolting.** Cast-in anchors are different: a measured floor detail used a **⌀28 hole for a ⌀20
  anchor — 8 mm clearance** — to absorb casting inaccuracy (which then requires a plate washer), while
  wall plates drilled into existing concrete used **⌀27 for M24, 3 mm**. Clearance follows how the
  anchor gets into the concrete, not one rule.
- **Some holes are oval/slotted** to ease field fit-up. A plate washer is required over a slotted hole.
- **Grating "marug 4+1" → model as a 4 mm plate** (not the bbox slab).
- **Anchor-bolt LENGTH is graphic only, deliberately not accurate** — Amir doesn't fully control it in
  the software and it isn't critical to him. Never chase anchor-bolt embedment, never "fix" it unasked.
  Their **presence, diameter and nut** are still expected to be visible.
- **What the engineer of record approves, you do not touch.** In lesson 4 he approved plate thickness
  and hole spacing; when more capacity was needed Amir changed *only* the plate length.

## Base plates, ribs, and editing an existing detail

The full worked lesson is in `references/base-plate-and-ribs.md`. The four principles:

1. **A base plate is a connection, not an object.** `PS_GROUNDPL` produces the plate, the holes, the
   anchors, the welds — **and it shortens the column by the plate thickness** so the design level at
   the top is preserved. That shortened length is the saw-cut length in the shop, not a graphic.
   In the API this is `ShortenShape = true`; leave it off and the plate ends up **below** the floor.
2. **A design change tunes the connection.** Change the parameter, let the joint recompute. Do not
   delete and rebuild, do not add stray plates.
3. **A rib is measured and drawn, not computed.** Amir's sequence: `DIMLINEAR` (then erase the dim) →
   `LINE` → `LINE` → `JOIN` → `PS_PLATE`. A rib is **never a plain rectangle**: the corner away from
   the load is cut off diagonally. ProSteel's own templates offer chamfered / convex / rounded, each
   half or full. **Chamfer size is not a fixed 15 mm** — measured examples were 80×80 off a 120×120
   rib and 75×75 off a 100×100 rib, i.e. roughly two-thirds of the corner. What remains is the
   triangle in the load path from column to plate.
4. **View before placement.** `PS_COPY` and `MIRROR` work in the *current view plane*, so Amir issues
   `-VIEW` two to three seconds before every placement run. Then one rib becomes eight by symmetry
   (`PS_COPY` / `MIRROR` / `ROTATE`), never eight separate creations.

## Reading an existing model correctly

- **A flat profile from `DIN.DIN_FLACH` named `Plate …` or `BRFL …` at a column foot IS a base plate**,
  produced by the base-plate macro. It is a `Ks_Shape`, **not** a `Ks_Plate`. Misreading these as
  "flats and stiffeners" was the single biggest modelling failure in this project: they were rebuilt
  as bare profiles with no holes, no anchors and no joint.
- `Ks_VolBody` on layer `PS_Bolt` = bolts generated by a connection.
- Anchor bolts appear as `AcDbBlockReference` on layer `PS_Bolt`.
- `RectangleMode` on a plate is **not** a reliable shape test — it stays `1` even after the contour is
  replaced. Count **unique** contour vertices instead (a closed rectangle reports 5 points, 4 unique).
- Plugin dump files are written **with a BOM**; open them as `utf-8-sig` or the first row of every
  file is silently dropped (this produced "7 ribs" when there were 8).

## Proven API paths, and the measured dead ends

Full detail in `references/api-proven-paths.md`. The essentials:

**Works**
- `PsDrillObject` (`SetObjectId` + `SetInsertPoint` + `SetNormal` + `SetSingleHoleField` + `Apply`) —
  drills a real hole in a plate *or* a profile.
- `PsSingleHoleArray(objId, LongHoleMode, …)` → `Count`, `getHole`, **`getFromSlottedHole`** (is it
  oblong), `getMaximalLength`. This is the verification instrument.
- `PsPlate.GetPolygon` / **`SetPolygon`** — read and *replace* a plate's contour in place, keeping its
  position, layer and **already-drilled holes**. This is how a rectangle becomes a proper rib.
- `PsCreatePlate` + `SetCoordinateSystem(origin,X,Y,Z)` + `SetAsRectangularPlate` + `SetThickness` —
  axis-preserving plate creation. Never derive the thickness axis by sorting bbox dimensions.
- `PsEditLogicalLink` — enumerate and read every joint on a part, including its full parameter set.
- Connection classes create joints that **drill their own holes**.

**Dead ends — measured, do not retry**
- **`CreateSingleBolt` + `AddObject(host)` does NOT drill.** Verified: plate + bolt with 2 hosts →
  holes read back = **0**. "Bolts with hosts" is not a connection.
- **`CreateSingleBolt` is declared `Void`** — no return value, no out-parameter, no exception. When the
  requested (diameter, grip) has no row in the bolt-style catalogue it **silently does nothing**. This
  caused ~400 consecutive "bolt failures" that looked style-related: the grip asked for was 280 mm and
  430 mm, far outside DIN 6914 (structural HV bolts, M24 tops out near 200 mm total). `PsBolt` exposes
  read-only `GripMin`/`GripMax`, so the window is real and enforced. **A long anchor rod is not a bolt
  — use `PsCreateFastener`.**
- `PsShape.MirrorFlag` cannot be written (read-only; `SetShapeMirror()` is a no-op; `YMirrorFlag` is a
  different flag). Mirroring is only achievable natively (`PS_COPY` mirror / `_MIRROR3D`) or by cloning.
- `Entity.Ecs` returns **identity** for plates and bolts; `PsShape.InsertPoint`, `COGPoint`,
  `WeightCenter` return **null**.
- Blind reflection over live `Ks_*` objects **crashes AutoCAD**. Type metadata (`Enum.GetNames`,
  `GetExportedTypes`) is safe; live-object reflection is not.
- COM: `Documents.Item(i).Save()` fails; **`app.ActiveDocument.Save()` works**. `Activate()` changes
  the logical active document but **not** the front MDI window.

## ⭐ There are TWO APIs. When .NET can't bind, use the COM wrapper

`ProStructuresNet.dll` is not the whole surface. **`PSCOMWRAPPERLib`** exposes the same objects as
real AutoCAD COM entities — `Ks_ComGrid`, `Ks_ComWorkFrame`, `Ks_ComShape`, `Ks_ComCreateGrid`, … —
reachable from Python with no plugin call at all:

```python
o = doc.HandleToObject(handle)     # -> Ks_Grid / Ks_Shape / ...
o.ObjectName, o.Name, o.Length     # reads
o.LeftWedge = True; o.Update()     # and writes
```

Several `Ps*` .NET classes are **creation buffers that cannot bind to an existing entity.**
`PsGrid` is the proven case: `PsObjectProperties.readFrom(id)` returns 0 *and* `getObjectId()`
returns the id asked for — it binds — yet `Name` is empty and `readProps` yields `L=0`. `readProps`,
`writeProps`, and `init()`-first all fail. Over COM the same object reads its name, spans, division
counts and **step arrays** immediately.

⇒ **Before concluding a property is unreachable, try the COM wrapper.** It also reaches dialog
options .NET never exposes (`LeftWedge`/`VerticalWedge` have no `PsCreateGrid` setter at all).
Names differ between the two: `Wide`→`Width`, `LengthDiv`→`LengthDivision`, `RoofWide`→`RoofWidth`.

## ⚠️ Enum names collide across assemblies — resolve by FULL type name

`GridType` exists twice. `Bentley.ProStructures.GridType` = `kRectangle | kCylinder | kWedge |
kPyramid` — the thing that decides what a work frame *is*. `aSa.PC.Shape.Graphics.GridType` =
`CrossLines | Points | None`. Reflecting by bare name found the second one and produced a
confident, wrong conclusion that cost a build cycle and left four frames silently shapeless.

Combined with the standing rule that **enum values must be measured, never inferred from
declaration order** (`FacetType` usable = 1,2,3; `OutletType` = 0,1 only): resolve enums by full
type name, and **set them by member name in C#, never by ordinal.**

## 🚫 LISP is banned — absolutely, by Amir

> *"מעכשיו כלל: אתה לא משתמש יותר בפקודת LISP. אך ורק בתוכנה עצמה ובפקודות שיש בה. נקודה"*
> and *"אני אראה את זה בחומרה רבה אם אגלה שאתה עובד ככה."*

No `(command …)`, no `(handent …)`, no `(ssadd)`, no `(setvar)`, no `.lsp`, no `.scr` that reaches for
any of them. This is a red line, not a preference. It has been violated **after** being stated — twice
in-session, and 18 call sites were still on disk days later. Lint for it; do not rely on intent.

**The legitimate ways to drive the software:**

| Route | What it is | Allowed |
|---|---|---|
| **.NET API in-process** | `ProStructuresNet.dll` + the 74 `PSN_*`/`PC3D*` assemblies | ✅ **primary** |
| **`Editor.Command(object[])`** | run any registered command with typed args. **In `accoremgd.dll`, not `acmgd.dll`** — the csc reference list must include it | ✅ with a whitelist |
| `Editor.SetImpliedSelection` | pre-select, then run a command on the selection | ✅ |
| `Document.SendStringToExecute` | queue a command string; asynchronous, poll for the result | ✅ |
| COM `doc.SendCommand("EB_RUN44\n")` | a bare command token from outside the process | ✅ |
| ~~`(command …)` in any form~~ | | 🚫 |

**Caveat that must not be glossed over:** `Localised\<lang>\Menus\ProStructures.mnl` **is an AutoLISP
file** auto-loaded with the ProStructures menu, and `Prg\load_net.lsp` is Bentley's own LISP bootstrap.
A `PS_` command registered by `ProStructure.arx` / `ProSteel.arx` is clean; one defined by the MNL is
not. **Whitelist against the ARX string tables — do not assume.**

**NETLOAD without LISP:** `FILEDIA=0`, then `SendStringToExecute("_NETLOAD\n<path>\n")`, then
`FILEDIA=1`. And **do not set `TRUSTEDPATHS` from code** — that is an AutoCAD security setting; ask
Amir to add the path once, then verify it and refuse to run without it.

## Working discipline (learned the hard way)

1. **Verify RELATIONSHIPS, not counts.** "24 holes ✓" was reported while the pattern was rotated 90°;
   "18/18 placed ✓" while every nut floated 10 mm off the plate. A correct count is not evidence. The
   standing gate checks: hole layout as **N columns × M rows** with actual spacings; anchor seating
   (embedment ±1, nut-to-plate gap 0±3, nothing in mid-air); contact (plate on concrete, column on
   plate); exact **and near** duplicates (same XY, different Z — the connection-rerun signature); and
   a delta vs the previous state where every change is explained. Full catalogue of the 15 errors this
   would have caught: `EB PROSTEEL AGENT/knowledge/lessons/RETROSPECTIVE-L4-L5.md`.
1b. **Report only what you measured.** Gate tables with evidence per row; no victory language before
   every gate is green. "Approved" is not immunity — re-audit after any adjacent change.
1c. **A value not in the drawing and not in the template is a QUESTION to Amir, never an invention.**
   (Invented anchorgrip=400/anchorkey=36 cost hours; asking "how deep does the anchor go?" got the
   answer — 120 mm — in one message.) And a parameter that is not sent must touch nothing — never bake
   a default into an op.
1d. **Fix in place; never fix by delete+rebuild loops.** Rebuilding erased approved corrections and
   re-shortened the column every cycle. Point fixes only, then the full audit.
1e. **Before writing a loop, ask "which command of the software does this?"** (PS_COPY array mode,
   _ARRAYRECT, the Connection Editor for edits). Element-by-element loops are the last resort, not the
   default.
2. **Never leave a half-entered command** on the modeller's command line. Prefix every send with
   ESC-ESC.
3. **Do not touch AutoCAD while the modeller is working** — sending to a busy AutoCAD interferes and
   blocks. Record from inside the process and read the log from disk.
4. Smoke tests at **X ≥ 40000**, deleted immediately. Keep a `PRISTINE-<name>.dwg` before any build.
5. **Every project on its own terms** — generalise the *method*, never the numbers.
6. **Read the documentation you already have before asking, guessing, or hand-building.**
   `knowledge/manual_fulltext.txt` (1,179 pp) · `Prg\Plugins\*.chm` (34) ·
   `Samples\COM Macros\` (25 worked DWGs, one per macro) · `Samples\Detailing\` (12) ·
   `knowledge/API-SURFACE-RAW.txt`. Manual chapters already mapped: `B.4` move/copy/mirror ·
   `B.9` plates · `B.12` 3D modifications · `B.13` plate editor · `B.14` drilling/bolted ·
   `B.16` stiffeners · `B.17` plate connections · `B.18` DSTV base plates · `B.20` shear plates ·
   `B.21` splices · `B.22` purlins · `B.23` gussets · `B.27` Connection Editor · `B.28` groups ·
   `B.29` positioning · `C.16` weld symbols · `D.5.1` collision check.

## Measured baseline — the number improvement is judged against

**05/08/2026, lesson-5 exam rebuilt end to end on a copy, stopwatch running, no new scripts:**

| | |
|---|---|
| Machine time | **3.0 min** (reset 1.1 + build chain 1.9) |
| Anchors | **556** — expected 480 · **+76 duplicates, all at z = −8 (the floor plates)** |
| Holes | **442** — expected 480 · 61 parts with 6, **19 parts with only 4** |
| Verdict | **Model invalid. All five scripts returned `rc=0`.** |

`19 copies × 4 surplus anchors = 76` · `19 × 2 missing holes = 38` — **retrospective error #13
exactly**: a replicated connection re-runs, adding anchors at its own default and deleting the
hand-drilled centre holes. The chain order was wrong (`dedupe` ran *before* `floor_anchors`).

**The lesson is not the timing — it is that speed was never the problem.** The 10-minute target
was beaten threefold on a defective model, and **Amir's eye was still the only relationship
checker in the system.** Full record: `EB PROSTEEL AGENT/knowledge/lessons/BASELINE-L5-2026-08-05.md`

## The engineering behind a column base

Full study (EN 1992-4 · EN 1993-1-8 · ACI 318-19 Ch.17 · AISC DG1):
`references/anchorage-engineering.md`. What changes how you model:

- **An anchor is a load path with six failure modes; five are in the concrete and all five are brittle.**
- **`N_Rk,c ∝ h_ef^1.5`, and the anchor diameter is not in the formula.** Tension fails ⇒ go deeper;
  M20→M24 at the same depth buys nothing.
- **Anchors closer than `3·h_ef` share one cone.** Amir's 6 × M20 at 156/81 mm spacing with
  `h_ef` = 120 behave like ~2 anchors — 34.6 % group efficiency. No count check will ever reveal this.
- **`h_ef` = 120 mm is a detailing default, not a design value** — right for a pinned compression-only
  base, wrong under uplift. Ask for `V_Ed` and the uplift before treating it as settled.
- **EN 1993-1-8 §6.2.2(5): oversized holes ⇒ anchor bolts may not be counted in shear.** What remains
  is friction at `C_f,d` = 0.20 (EN) — not 0.55 (AISC) — and it vanishes under uplift. Real shear needs
  a **shear lug**.
- **A 25–50 mm grout bed exceeds EN 1992-4's `0.5·d` limit** and costs 56–78 % of the anchor's shear
  capacity through the lever arm. `PsCreateFastener` takes `GroutThickness` as an argument, and
  `PsBaseplateLinkDataMgd.LiningThickness` **is** that grout layer — model it knowingly.
- **The big anchor hole is a tolerance budget** (ACI 117 ± 6.4 mm in the pour + ± 3.2 bolt-to-bolt +
  ± 2 drilling ⇒ 33 mm for M20 per AISC DG1). A standard ⌀37 washer bears only 4.5 mm on a ⌀28 hole,
  so an oversized hole **requires a plate washer** (t ≈ d/3, side ≥ 2·d_hole − d_rod ⇒ ~60×60×10).
- **Hooked L/J anchors: stop accepting them.** They carry by straightening, ACI limits them
  arithmetically, **EN 1992-4 has no formula for them at all**, and they can never be ductile.

> ⚠ This is understanding, for modelling knowingly and asking the right question. **It is not design
> authority** — Phase 2 is locked. Never present these numbers as a verification to anyone.

## Anchor bolts — why they came out invisible, and the fix

Creating a base-plate joint from `new PsBaseplateLinkDataMgd()` gives you an **empty parameter object:
every field is zero**. So `AnchorBoltDiameter = 0` → four anchor blocks with no body, invisible; and no
nut until `AnchorBoltKeySize` is set (SW30 for M20 → bbox `34.6×30×…`, where 34.6 = 30/cos30° is a hex
nut measured across corners).

Amir's own anchors, measured from his model, are `34.6 × 30 × 157` — **nuts included, first time**,
because he works from a **configured dialog template**.

> **Therefore: start from a template, not from an empty object.**
> `bp.GetTemplate(name)` — available base-plate templates in this installation are
> `AutoConnect Metric v 18/450x450x25`, `AutoConnect Metric v 18/600x600x25`, `default/Standard`
> (200×200×10, anchors on). Take the template, change only what the drawing dictates.
> Still to verify: whether the template also brings **washers**, and which template best matches
> Eretz Barzel's own detail.

## Duplicate check — a mandatory audit gate

A real model was found with **two identical plates at the same point** (same centre, same contour,
same four holes — 100 % overlap), a `PS_COPY` over-shoot. Amir confirmed it was a mistake and deleted
it. Such a model passes every *count* check and is still wrong. **Always test for exact duplicates
(same centre + same dimensions + same contour), not just totals.**

## Non-steel objects in a steel model

`AcDb3dSolid` bodies may be present purely as **illustration** — in one lesson a 200 mm concrete wall
and a 300 mm floor slab were modelled as solids so the intent was tangible. They are not steel:
exclude them from weights, part lists and NC output, and never "correct" them as if they were.
