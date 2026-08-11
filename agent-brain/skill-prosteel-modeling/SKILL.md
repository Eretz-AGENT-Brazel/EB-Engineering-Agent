---
name: prosteel-modeling
description: How to model real steel structures in AutoCAD 2015 + ProStructures (ProSteel) V8i SS6 the way Eretz Barzel actually does it — the agent's job is to BE Amir's modeller, taking the grunt work and reaching every function the software has. Covers connections as parametric objects (base plates, end plates, fin plates, web cleats, copes, haunches, purlins, splices, gussets, stiffeners/ריפים), modelled holes at every bolt passage, non-rectangular plate contours, column shortening, build-once-then-replicate as both a modelling method and a fabrication-economics goal, metric-always, the absolute ban on LISP, and the mapped API surface (~1,400 relevant public types across 116 managed assemblies — PsCreateFastener, PsGeometryFunctions, PsMiscTools, PsObjectGroup, PsSelection, PsCollisionCheck, PsCreatePositioning, PsDrillObject, PsEditLogicalLink and the nine connection classes) plus the measured dead ends and a measured performance baseline. Also carries the column-base anchorage engineering (EN 1992-4 / EN 1993-1-8 / AISC DG1) for value engineering, since the company fabricates rather than designs loads. Use whenever modelling, editing, auditing or automating steel in ProSteel/AutoCAD, reading an existing steel model, or deciding whether a detail is modelled correctly for fabrication. Built from live lessons with Amir (Eretz Barzel); every claim was verified by reading the model back, never from a screenshot.
---

# Modelling steel in ProSteel — the Eretz Barzel way

Working knowledge built from live lessons with **Amir (ארץ ברזל)**, who models steel professionally
and corrects the agent in real time. Companion project: `C:\Users\User\Desktop\EB PROSTEEL AGENT`.

> ## 🧲 IRON RULE — A BOLT PASSES THROUGH **HOLES**. NO EXCEPTIONS.
> Amir said this four separate times on 09/08 and closed with:
> **"כלל ברזל — תורה מסיני — אין כאן משחקים, זה קריטי מאוד הכלל הזה."**
>
> > *"אין דבר כזה חומר. כאשר אנחנו ממדלים בורג — זה **חייב** להיות שהוא **עובר דרך חורים
> > באלמנטים אותם הוא מחבר**."*
>
> **A modelled bolt must pass through a modelled HOLE in EVERY element it connects.** His wording
> is plural: a hole in one of the parts is not enough — each part the bolt joins needs its own.
> A bolt without that is a **CRITICAL ERROR**, not a detail to tidy up later.
>
> ⚠️ **This is the default, and it is absolute. Silence means critical error.** The one exception
> is a **self-drilling screw** (בורג קודח), which cuts its own hole on site and is modelled so the
> count reaches the parts list. But that is **a declaration Amir makes, per case — never a
> category the agent may apply**, and it requires **an exceptional instruction AND special
> approval**. In a **structural** connection there is no exception at all.
>
> ⛔ Never create a bolt without holes on my own initiative, and never use one to paper over a
> bolting that refused. If a situation seems to want one — **ask in one line**.
> *(Why this sits at the top: it was first recorded in a block halfway down this file, and Amir
> had to repeat it four times. A rule this critical belongs where it cannot be missed.)*

> ## 🧱 THE CEILING — CHECK IT BEFORE CHASING ANY CREATOR
> Full list: **`knowledge/learning/findings/THE-CEILING-what-code-cannot-reach.md`**. Consolidated 09/08 from nine
> chapters that each paid for it separately.
>
> ⭐⭐ **The pattern in one line: anything that requires a MOUSE PICK is unreachable from the API.**
> Not a run of accidents — the shape of the product. The managed classes expose a connection's
> *data* but not the act of choosing what it connects.
>
> **Closed, do not retry:** bracing (13 configurations) · the `PSN_*` macros (all prompt) · gussets
> (no creator; they come from the bracing) · ~~Clone / `TakeoverDrills`~~ **RETRACTED, see below** ·
> standalone weld flags · haunch placement at a point · the cope's ESC route · bolting with no
> pre-drilled holes · `PsCreateFastener`.
>
> ### 🛑 `TakeoverDrills` WAS NEVER BROKEN — retracted 10/08 by the part-B audit
> **It transfers drill holes.** Three fresh identical HE300B beams, `variant 1` = `SetToDefaults`
> + `SetObjectId(src)` + `TakeoverDrills` and nothing else: each target received a ⌀22 hole **at
> its own centre, same z** — real geometry, not a count. Five of eight variants worked.
> **What I got wrong:** the 06/08 verdict rested on *"selections proven correct"*, which proved
> the **selection sets** were valid and nothing about the call's preconditions. B.4.5 states one
> plainly — *"a prerequisite… is that the parts have a position number and that these match"* —
> and the model carries **no position numbers at all**.
> ⚠️ **The control then disproved that hypothesis too**: it transfers with no posnum, a different
> posnum and a matching one alike. Why 06/08 returned zero is **unknown, and left unknown**.
> ⇒ ⭐⭐ **A "closed, do not retry" verdict is only as good as the preconditions the test
> honoured** — and a wrong entry here is expensive, because its purpose is to stop anyone looking
> again. **Re-test anything closed without checking the manual's stated preconditions.**
> ⚠️ Real caveat, as B.4.5 warns: the source hole ran **−Y**, the targets **+Y**. *"The transfer
> refers to the coordinate system of the parts"* — immaterial for a through-hole, **not for a
> countersink or a slot**, and a mirrored target gets a mirrored modification.
> **Parameters that never arrive** (the call works, the numbers are ignored, the template wins):
> base plate · end plate · purlin. ⇒ **choose a template, do not pass numbers.**
> 🛑 **RETRACTED 10/08/2026.** This line used to read *"Never fairly tested, worth three strikes:
> `CreateRotation` · `CreateHull`…"* — **both work.** `CreateHull` needed a real
> `PsDataPointArray` (`solid dpts=`) and builds exactly; `CreateRotation` needs a **local 2D**
> polygon and a **world** axis that lies **in the profile's plane**.
> **Still genuinely untried:** Clone's other four categories (Cuts, PolyCut, Notches, Boolean)
> and `ClsParameters.ReadFromTemplate`.
>
> ⇒ **Before spending a strike, look it up.** If it is closed, go straight to the composition
> workaround — the file lists one for every case.

> ## 🔧 THE VERIFICATION KIT — USE THESE, DO NOT INVENT A CHECK (built 09/08)
> On 09/08 **four** separate findings turned out to be artefacts of a check invented on the spot:
> bounding boxes gave **two opposite answers** to "does it touch"; `dumpmodel` was **blind to
> bolts**; `connverify` produced **32 flags, every one false**; a COM handle read after a delete
> returned a **stale cache**. The software failed less often than the measurements did.
>
> Three instruments now live in the plugin. **Reach for these first.**
>
> | op | question | calibrated against |
> |---|---|---|
> | **`vfy_bolts minx= maxx=`** | do bolts and holes correspond, **by geometry**? | B.25's balanced bay (**0/0** ✅) and B.22's `kBoltet` (**9 unfilled** ✅) |
> | **`vfy_touch a= b=`** | do two parts touch? reports the **gap or overlap as a number** | tread vs collar → **overlap 2.0, X = −2.5**, matching the hand measurement |
> | **`vfy_size minx= maxx=`** | has anything been **stretched**? | the haunch that reached 317,000 mm |
>
> ⭐ **Each op prints its own blind spot in its result line.** `vfy_bolts` matches by proximity, so
> it cannot tell *which part* a bolt crosses; `vfy_touch` uses axis-aligned extents, so a **rotated**
> part can read as touching when its real contour does not — for a sector or a diagonal, read the
> contour (`plateinfo ext=`, `GetPolygon`). **An instrument that never says "I don't know" is the
> one that produced the 32 false flags.**
>
> ⭐⭐ **Calibration is the point, not the code.** Both non-trivial ops were wrong on first run and
> the *known cases* caught it:
> - `vfy_bolts` flagged 4 iron-rule violations in a bay believed clean — and it was **right**: four
>   **orphaned bolts** survived a rebuild in which only the rods and gussets were deleted. My
>   "2/2/2 at all four ends" had measured only what I had just built, not what was in the band.
> - `vfy_size` flagged a 15 × 24 m `Ks_Grid`. Grids and work frames are **layout**, legitimately
>   huge; the guard is about a **member** being stretched, so it now skips them.
> ⇒ **Never trust a new check until it has failed a known-bad case and passed a known-good one.**
>
> **Model baseline, 09/08:** 835 parts · 194 bolts · 593 holes · 187 matched · 0 oversize.
>
> ### 🛑 `KlemmLen` IS NOT THE PACKET — a check built on it was retracted the same day (10/08)
> E.9.3 calls `KlemmLen` *"the calculated clamping length of the bolt"*, so `vfy_grip` was built
> to compare it against the summed depths of a bolt's holes and call a shortfall *"the bolt
> clamps undrilled material"*. It flagged **20 bolts**. **All 20 were withdrawn before being
> reported**, because two measurements killed the premise:
> - `F82` is an **M16×75 reporting klemm=50**, and the only steel on its axis is an L90×9 leg
>   (9 mm) plus a 10 mm gusset. **There is no 50 mm of material there.**
> - **Every** M20×70 DIN6914 in the drawing reports 39; **every** M20×70 Mu DIN7990 reports 42 —
>   identical within a type, across different joints.
>
> ⇒ **`KlemmLen` is a property of the bolt TYPE and LENGTH.** It equalled the packet exactly
> (39 = 19+20) on the one joint it was first calibrated against **because ProSteel had chosen
> the bolt to suit that packet.** ⭐⭐ **One calibration case is not a calibration** — the rule
> was already written three lines above and still got applied one step too late.
>
> ### 🔧 What replaced it — `vfy_fit` and `vfy_dupes` (judge only measured geometry)
> | op | judges | blind to |
> |---|---|---|
> | **`vfy_fit`** | `spare = nominal length − packet`, and the **air between consecutive holes** | over-counts when two bolt rows sit closer than `tol` |
> | **`vfy_dupes`** | bolts at the same point · holes at the same point in one part | nothing else; it is pure coincidence detection |
>
> ⭐ **The model calibrates the threshold, not me.** Across ten healthy bolt types, `spare` sits
> in a tight **22–31 mm** band — a nut, a washer and a few protruding threads.
>
> ### 🛑 …AND THE SECOND RETRACTION, an hour later: "oversized" was wrong too
> Eight M16×75 on a 19 mm packet read `spare = 56 mm`, so they were about to be swapped for
> M16×50. **The bolt style list stopped it:** a ProSteel bolt style is the **standard**
> (`DIN7990`), not the length — **ProSteel picks the length from the packet.** So it had sized
> them for 50 mm. The holes say why:
> ```
> gusset F72   y −55 … −65     (10 mm)
> angle  F76   y −96 … −105    (9 mm)
>              31 mm of NOTHING in between
> ```
> ⇒ **The plies do not touch.** The bolt is exactly right for the assembly as modelled; the
> fault is a **31 mm air gap inside a bolted lap**, which puts the bolt in bending and cannot be
> fabricated without a packer. Swapping to M16×50 would have left it too short to reach.
> ⭐⭐ **A large `spare` means one of two opposite things** — an over-long bolt, or plies that do
> not touch. `vfy_fit` now measures the air between consecutive holes along the bolt axis and
> reports **`GAP-IN-PACKET`** separately. ⇒ **When a check fires, ask what ELSE would produce
> that number before acting on the first explanation.**
> ⚠️ Also real, and found the same way: **a bolt can be SHORTER than its packet** — 6 × M20×70
> through 79 mm of contiguous steel, and 6 more leaving 11 mm for a 16 mm nut. Neither can be
> assembled, and nothing but this check would have said so.

> ## ⚙️ THE WHOLE SETTINGS DIALOG IS AN OBJECT — `Ks_ComGlobalSettings` (A.2, 10/08)
> ⭐⭐ ProSteel's **global settings** (the A.6 dialog) are reachable with no dialog and no
> command. It is an **in-process** COM server, so `Dispatch()` from outside fails with
> *"Invalid class string"* — **AutoCAD has to hand it over**:
> ```python
> gs = GetActiveObject('AutoCAD.Application').GetInterfaceObject('PSCOMWRAPPER.Ks_ComGlobalSettings')
> ```
> It carries `TemplatePath()` · `TempPath()` · `GetDataPath()` · `SetProjectPath()` ·
> `ApplicationPath` · `BlockCenterPath` plus dozens of settings (`ArcResolution`,
> `BodyResolution`, `BoltResolution`, `AreaClassCount`, `CheckDWG`…).
>
> ⭐⭐ **THE TEMPLATES ARE FILES.** `TemplatePath(True)` →
> `…\localised\`**`english`**`\Varia\`**`Metric`**, full of `.tpl` files — `BasePlate.tpl`,
> `KsxBasePlate.tpl`, `KssStairs.tpl`, `KssTruss.tpl`… That is A.3.2's Template Manager on disk,
> and its IMPORT/EXPORT just moves them. ⇒ **Amir can configure Eretz Barzel's standard detail
> once in the dialog, save it as a template, and the agent then loads it by name from code —
> and the file can be backed up and version-controlled.**
> ⚠️ The configuration is keyed on **language × unit system**, and the language is literally in
> the path (`Localised\` holds Australia · Deutsch · English · NewZealand · USA_Canada).
> Changing the language swaps the templates, the blocks **and the generated part names** — a
> plate is `PLATE 400x300x20` here and would be `BLECH 400x300x20` under Deutsch. **Never change
> it; it is a whole-installation decision.**

> ## 📐 A TEMPLATE IS A SAVED DIALOG, AND ITS NAME IS `branch/name` (A.3.2, 10/08)
> The mechanism behind the week's working rule — *choose a template, do not pass numbers*.
> A template is **the dialog's complete state saved under a name**, in a folder tree. So
> `example/example3` is the template `example3` in the branch `example`.
>
> ⭐ **There is a shipped METRIC library: the `AutoConnect Metric v 18` branch** — named by size
> (`450x450x25`, `600x600x25`) or bolt count (`2 Bolt`, `3 Bolt`).
> ⚠️ **`default/Standard` is not the neutral choice it looks like** — the shipped defaults use
> bolt style **`8.8S`** (Australian) while the AutoConnect branch uses **`DIN7990`/`DIN7969`,
> which is what the models actually contain.**
>
> ⭐⭐ **Demonstrated: same op, same column, only the template name changed** —
> `default/Standard` → 200×200×10; `AutoConnect Metric v 18/450x450x25` → 450×450×25.
> **Every number came from the template; none was passed.**
>
> 🛑 **THE TRAP THAT COST 10 POINTS IN LESSON 4, now explained.** `default/Standard` carries
> anchor *dimensions* (⌀18, drill 185, `detailed=1`) but its **`AnchorBolts` flag is OFF** — so
> it builds the plate and the holes and **no anchor bodies**. That is exactly Amir's *"where are
> the nuts?"*. ⇒ **ALWAYS read a template's parameters before using it**: `conntemplates`,
> `stifftemplates`, `webangletemplates`, `shearplatetemplates`, `splicetemplates` print every
> field of every template. **A template hides its own settings; those ops are how you look
> inside.**
>
> ⭐ **`CLONE` (a dialog button) reads a live connection's settings onto another one** —
> *"the exact default settings of which, however, you don't know any more"*. Not yet tried.
> ⚠️ It is **not** the `Clone` on THE CEILING; that one is B.4.5's modification clone.
> ⭐ `UPDATE` and **dynamic mode**: *"each modification of a parameter is directly translated
> into a modification of the object"* — the documented reason a connection **re-runs** when its
> inputs change (E.9's section swap, lesson 5's clones). It has an off switch in A.6.
> 🔜 **dBASE / Excel template generation** (A.3.2) is the most promising unexplored route in the
> manual — and `Varia` on a network share would give the whole office one set of details.
> **Both are Amir's call.**

> ## ⌨️ WHY THINGS RE-RUN, AND WHY ENTER RECOVERS A MACRO (A.4, 10/08)
> ⭐⭐⭐ **`ALT` suppresses the LINK UPDATE** — *"avoids working off the link update… you can use
> this function to **move parts without causing a reaction of the connected parts**."*
> **That is the mechanism behind every re-run that has cost work:** the section change that
> destroyed two bolts (E.9), the base plate that re-ran in every clone (lesson 5), the four
> orphans at B.26's apex. Not a quirk — the documented **dynamic mode**.
> **And the switches are reachable** on `Ks_ComGlobalSettings`, currently all ON:
> `DynamicConnections` · `LinksActiv`/`LinksActivUpdate` · `LinksPassiv`/`LinksPassivUpdate` ·
> `RecalcWhenNeeded=False`.
> ⛔ **Do not change them without Amir** — they are global, and they change how the software
> behaves for him too. The safe shape is the UCS pattern: **toggle → one operation → restore in
> a `finally`.** Propose it; do not do it.
>
> ⭐⭐ **`RETURN` at a selection prompt selects EVERYTHING.** That is *why* ENTER (not ESC)
> recovers a parked macro — it is being handed a valid selection, not cancelled. A rule that was
> right for two weeks without a reason.
> ⭐ **`ESC`+`SHIFT` during selection opens the FILTER** without leaving the command.
> ⭐ **There is a BOLT CACHE** — *"the bolted joint command saves the last created bolt"*, and
> `CTRL` clears it and forces a re-read from file. **A live lead on `boltparts` refusing
> geometry that measures perfect.**
> ⭐ `ALT` at **Edit Drill Holes** *"cancels the blocking of bolt fields… these can then be
> deleted"* — holes owned by a bolt field are **blocked**, which is why "holes cannot be
> removed" looked true before `DeleteHoleField`.
> ⭐ `ALT` at **Connect** skips the type check; at **Haunch** on the support it **adapts plate
> thicknesses to the supporting shape**; at **Insert Shapes** it rotates −90° instead of +90°.

> ## ⚙️ A.6 IS THE DEFAULTS, E.9 IS THE PER-PART OVERRIDE (A.6, 10/08)
> A.6 says it itself: *"Identical values … resulting from these settings **can be modified at any
> time for each part** via the 'Change PS Properties' command."* All of it readable through
> `Ks_ComGlobalSettings`, no dialog. **Read, never written — these are global on Amir's machine.**
>
> ### ⭐⭐⭐ Why a plate's `Name` cannot be written — closed across THREE chapters
> ```
> PlateNameTemplate  '$(N) $(L)x$(W)x$(T)'      PlateNameBigValue True
> ```
> **A.2** gives the constant `$(N)` from the language config (`PLATE` / `BLECH`) · **A.6**
> renders it with the live dimensions · **E.9** therefore cannot store a name — it is
> **re-rendered every time**. ⇒ **Label a plate with `Note1`/`Note2`/`Posnum`.**
> ⚠️ `Desired Plate Name` is what **NC/PPS output** uses — *"keep this name, otherwise there
> will be compatibility problems with output"*.
>
> ### ⭐⭐ Two more silent-refusal explanations
> **`Lock Part Properties`** — *"part properties created **under the control of a logical link**
> can only be **read but not edited**"*. A part inside a connection has its connection-owned
> fields locked, and the API still returns success.
> **`Allow additional data`** — this is E.9.17's *"Logical Links / Extended Input"*, the switch
> that makes a link's `Ident`/`Name` exist. **It is OFF**, which is why they read empty.
>
> ### ⭐ Numbers worth carrying
> **`MaterialSpecificWeight = 7850`** — ProSteel's steel density is exactly the EB factory rule,
> confirmed rather than assumed (2700 = aluminium).
> **`PlateRasterWeightReduction = 10%`** — a **grating** (`Grid` on E.9.2's Layout tab) is billed
> at 90 % of a solid plate.
> ⚠️ **Weight and paint area have a METHOD setting** — exact form vs *"rubber tape"* — and it
> **changes the parts list**. ⇒ a weight difference between two models can be a *setting*.
> ⚠️ **Revision Check tolerances** feed the same comparison as positioning
> (`CheckTwoPartsAreEqual`) ⇒ **"are these two parts the same" has a configurable answer.**

> ## 🎨 LET PROSTEEL PICK THE LAYER — do not force the current one (B.1 audit, 10/08)
> ⭐⭐⭐ Every creator that exposes `UseCurrentLayer` will put the part on **its own** layer if you
> pass **`false` and set no layer at all**. Measured with `layerprobe` against a deliberately
> wrong current layer: `UseCurrentLayer(true)` → the junk layer · **`(false)` with no
> `SetLayer` → `PS_Plate`**.
> ⇒ **The 88 parts found on layer 0 on 09/08 were self-inflicted** — the plugin was overriding
> ProSteel's automatic layer control, which B.1's first sentence promises and which works.
> ⇒ **`layer=` is an OVERRIDE, not a requirement.** Default to letting the software choose.
> ⚠️ **`PsCreatePrimitive` (all solids) has NEITHER `SetLayer` NOR `UseCurrentLayer`** — it
> cannot be told, so `solid` assigns the layer *after* creation, defaulting to `PS_Solid`.
> ⭐ **`layerprobe`** is the op for this question, and it cleans up after itself.
> 💡 The wider lesson: **before adding a parameter to eleven call sites, ask whether the software
> already does the thing and is being overridden.**

> ## 🔩 TWO CORRECTIONS TO EARLIER NOTES, both found while fixing (10/08)
>
> ### ✅ HOLES **CAN** BE REMOVED — B.26's note was wrong
> **`PsEditModification.DeleteHoleField(handle)`** exists and works. Ops: **`holefields`** and
> **`killholefield`**.
> ⭐ **A hole FIELD is not a hole, and the difference decides the surgery.** `holefields` prints
> both counts and says which case you are in:
> - **fields == holes** → one hole per field, so a duplicate is a whole field. Delete it.
>   **Highest index first** — removing a field renumbers the ones after it.
> - **fields < holes** → the pattern lives *inside* the field, and no single hole can be
>   removed. Check first that the holes are unused, then delete the field and **re-drill** the
>   intended positions.
>
> ### ⚠️ A CONNECTION'S BOLT LIST CAN NAME BOLTS THAT NO LONGER EXIST
> At B.26's apex both rafters carried a **live** end-plate link each claiming six bolts — and
> **all twelve object ids were dead.** None matched any of the sixteen bolts actually present;
> they pointed at bolts deleted in an earlier rebuild.
> ⇒ **`getBoltObjectId()` is not an ownership record after an edit.** Before deleting a bolt
> because a link seems to own it — or keeping one for the same reason — **resolve the ids
> against the bolts that are really there.** (`propfull` prints the plugin's own `oid`;
> COM's `ObjectID` is a *different* numbering and comparing the two silently gives nonsense.)
> ⭐⭐ **A DUPLICATE BOLT IS INVISIBLE TO EVERY BOLT-VS-HOLE CHECK** — each copy matches the same
> hole perfectly, so `vfy_bolts` calls it clean. It surfaces in the **parts list**, not the
> geometry. B.26's apex was bolted **three times over**, 8 redundant bolts, undetected for a day.
>
> ⭐ **ProSteel itself refuses to bolt across an undrilled element** — three plates 20+15+20,
> outer two drilled, middle solid → `boltparts` refuses (*"holes further apart than 'Gap
> distance'"*). **Part of the iron rule is enforced by the software at bolting time.**
> ⇒ Violations come from **editing afterwards**, not from bolting badly. Every one found so far
> arrived that way: 4 orphans after a rebuild (09/08), 2 destroyed by a section change (10/08),
> 4 more orphans at B.26's apex (10/08). **Run the checks after every edit, not after the
> modelling.** Full audit: `knowledge/learning/audits/AUDIT-B08-bolts-2026-08-10.md`.
>
> ### ⚠️ OPEN — `boltparts` refused geometry that measures perfect (10/08)
> Two 20 mm plates, `vfy_touch` → TOUCHING with **Z separation 0**, holes `…,10→…,−10` and
> `…,30→…,10` — meeting exactly at z=10, same direction, same ⌀22. `boltparts` returned
> `create=False`; the single `bolt` op refused too. The canned reason (*gap distance / angle
> difference*) **is contradicted by the measurements**.
> **This is a live risk**: `boltparts` is the route for hand-composed connections and bolted
> B.25's braced bay. If it refuses valid geometry, earlier work may carry fewer bolts than
> intended. **Run `vfy_bolts` + `vfy_grip` over the older bands, and raise it with Amir before
> hand-composing the next connection.**

> ## 📝 THE PROPERTIES DIALOG IS A WRITE SURFACE (E.9, 10/08)
> Full notes: **`knowledge/learning/manual/E/MANUAL-NOTES-E09-properties-dialogs.md`**.
>
> ⭐⭐ **Every field in ProSteel's properties dialog is a property on `PsObjectProperties`, and that
> class has `writeTo(Int64 ObjId)`.** Measured: 14 fields set in one call, `writeTo.rc=0`, **14/14
> survived a re-read from a fresh instance**, and AutoCAD drew the result (the column turned green
> from `ColorIndex=3`, grew a centre line from `CenterLineMode=1`) — confirmation by a route
> completely independent of the read-back.
>
> | op | what it does |
> |---|---|
> | **`propfull handle= [tab=]`** | all ~120 properties under the dialog's own tab headings, `rw`/`r-` marked |
> | **`propset handle= <AnyProperty>=<v> …`** | writes any of them, then **re-reads and reports before → after per field** |
> | **`propcopy src= dst= [tabs=4,6]`** | "match properties"; defaults to Data + Assignments so it does **not** carry geometry |
> | **`changesection handle= key=HE400B`** | ⭐ **swap a section in place** — IPE300 → IPE500, length and position kept, weight recomputed correctly |
>
> ⚠️ **The API does NOT filter by part type — the dialog does.** A column happily reports
> `KlemmLen=0 Tension=0 MountingBolt=False` with no error. Knowing which fields apply is the
> caller's job.
> 🛑 **AND `rw` DOES NOT MEAN THE VALUE WILL SURVIVE.** It means only that the .NET property has
> a setter. Whether `writeTo` keeps it depends on the **part type**, and the API gives no signal
> — `rc=0` either way. Measured on a `Ks_Grid`: `Note1`, `Name` and `Article` are all marked
> `rw` and are all **silently discarded**, while `AreaClass` sticks; on a `Ks_Shape` all four
> stick. ⇒ **`propset`'s read-back is not a nicety, it is the only thing that tells the two
> apart.** (A.1, and it corrected this block's own first wording.)
> ⚠️ **A plate's `Name` cannot be written** — it is *generated* from the dimensions (E.9.2 lists
> `Name` twice for exactly this reason). Measured on 4 plates: ignored every time; the same call on
> 3 shapes worked. **Label a plate with `Note1` / `Note2` / `Posnum`.**
> 🧲 **`KlemmLen` is the bolt's GRIP LENGTH**, `Tension` its **pre-tension %**, `MountingBolt` the
> site-vs-shop flag. Grip length against the summed thicknesses of the connected parts is
> ProSteel's *own* answer to the iron rule — stronger than the proximity matcher, which is blind to
> *which* part a hole belongs to. **Not yet built into a check; the best follow-up available.**
> ⭐ Logical links live **on the part**: `PsEditLogicalLink.SetObjectId` binds (`PsEditConnection`
> never did — that was last session's `connverify` failure, from the wrong end).

> ## 🚨 CHANGING A SECTION BREAKS A CONNECTION — kill and rebuild (10/08)
> Two identical column + beam + end-plate specimens; one given IPE300 → IPE400 by `changesection`.
> Counted by **AutoCAD class**, not by the matcher:
>
> | | Ks_Shape | Ks_Plate | Ks_Bolt |
> |---|---:|---:|---:|
> | untouched | 4 | 8 | **6** |
> | after the section change | 4 | 8 | **4** |
>
> **Two bolts destroyed, their two holes left abandoned in the end plate.** The section swap itself
> is correct; the **joint** is silently wrong — and a silently wrong joint is a wrong shop drawing.
>
> ⇒ **`connkill` → change the section → rebuild the connection.** Proven: back to 6 bolts, 0 orphans.
> `changesection` now **refuses** on a part carrying logical links and requires `force=1`.
> ⚠️ **The general lesson: any op that can resize an existing member must be followed by `vfy_bolts`
> on that bay.** Same family as the blast guard.

> ## 📐 ONE LESSON, ONE STRIP — the separation must be VISIBLE (Amir, 10/08)
> *"שכל המידולים של כל שיעור יהיו בסטריפ מוגדר ונראה הפרדה בין שיעור לשיעור."*
>
> A practice model is read by a human, so the layout has to say where one lesson ends without
> anyone having to consult a file.
>
> * **Pitch 60 000 mm along +X: a 56 000 strip and a 4 000 gap.** The gap *is* the separator.
> * Each strip is bounded by a **named `Ks_Grid`** carrying the lesson's name
>   (`grid at= lsteps= wsteps= name=`), all on layer **`_STRIPS`** so they switch off in one click.
> * ⭐ **Label every specimen through the Data tab** — `propset Note1=` what it is for,
>   `Note2=` what was measured on it. The model then documents itself, and a deliberate oddity
>   (a bare hole built to calibrate `mods`) is never later misread as a defect.
>
> ⚠️ Two `grid` traps, both measured, both opposite to what the names suggest:
> **`lsteps`/`wsteps` are BAY SPACINGS, not counts** — `lsteps=1` builds a bay one millimetre
> wide — and the grid's **LENGTH runs along world Y, its WIDTH along world X**.

> ## 📖 E.10's chapter numbers are STALE — trust the COMMAND, re-derive the chapter (10/08)
> Corrected table: **`knowledge/learning/manual/E/E10-COMMAND-REFERENCE.md`** — 126 commands, chapter column
> re-derived from the manual's own table of contents, plus which agent op covers each.
>
> E.10 gives *Function / Chapter / Command*. Of the 50 rows whose function name matches a TOC
> chapter title **verbatim**: 26 cite the chapter correctly, **16 are off by exactly one**, 8 are
> off by two or three. Part B is right through B.10 and off by one from **B.11** onward; part C
> is off by 2–3; part E off by one from E.2; `RevisionCenter` cites D.6 but lives at **C.6**.
> ⭐ The cause is visible in the data: the one row citing B.11 correctly is *"Create ACIS body
> reference"* — the chapter **inserted** in a later edition that pushed everything after it down.
>
> ⇒ **The command names are reliable and are the interactive route to everything the API refuses.
> The chapter numbers are not — look the chapter up in the TOC.**
>
> 🔒 **The `cmd` allowlist runs 9 of those 126, and that is deliberate.** Most ProSteel commands
> are interactive; an unattended agent that starts one leaves the session parked. **Nothing is
> added without Amir saying so, one command at a time, with a reason.**

> ## ✋ THREE STRIKES — AFTER 3 REFUSALS, STOP TRYING (adopted 09/08)
> When a creator refuses, **try at most three configurations.** Then do one of:
> **(a)** build the thing by **composition** from calls that are known to work · **(b)** ask Amir in
> one line · **(c)** record it as dialog-only and move on.
>
> ⛔ **Never keep permuting parameters.** `PsBracing.insert()` was given **thirteen** configurations
> across B.24 and B.25 before being accepted as unreachable — about two hours, and the answer was
> visible after three. The bolted eaves in B.26 was hand-rolled **three times** from
> plate + drill + bolt, giving 0 bolts each time, when `conn kind=endplate` built the whole corner
> correctly **on the first call**.
> ⭐ **The tell:** if two attempts fail for *different* reasons you are still learning; if they fail
> the *same* way, the route is closed. **Count the attempts out loud in the report** so the budget
> is visible while it is being spent.

> ## 👁 SHOW THE SCHEME BEFORE THE DETAIL (adopted 09/08)
> Before spending time on bolt spacings, hole patterns or plate sizes, **show Amir the joint at the
> "is this structurally sensible" level** — one screenshot, one sentence of what carries what.
>
> Every scheme-level error on 09/08 was caught **by his eye, not by my measurements**, and each one
> arrived *after* the detailing was already finished:
> - staircase treads **floating 44.5 mm** off the collar — and I had "verified" them
> - the anchors were a **profile, not anchor bolts** — right geometry, wrong object, wrong list
> - the apex plate sat 120 below the ridge, bolts 50 mm apart
> - the welded eaves **was not a connection at all** — a sloped end meeting a flat face along a line
>
> ⇒ **My checks answer "did the API do what I asked". His eye answers "is this a real detail."**
> Different questions — and only the second protects the model. Ask it first, while it is cheap.

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
> 🛑 **CORRECTED 11/08/2026 — the old line here listed `PsBracing.insert()`, `PsLadder.insert()`
> and `PsPortalFrame.init()+insert()` under ✅. That was a map of METHOD EXISTENCE, not of
> capability, and every one of those three has since been MEASURED to create nothing.**
> The real boundary, from part E:
> ✅ **`PsCreateHandrail.Create()` — the ONLY structural creator that builds** (E.3).
> ⛔ **refuses / no-ops:** `PsStairs.Insert`=False · `PsCircularStairs.insert`=True-and-builds-nothing
> (E.2) · `PsPortalFrame.init()+insert()`=0, census unmoved (E.4) · `PsBracing.insert()`=False (B.24)
> · **`PsLadder.insert()` is `Void` and the census is the only verdict — 82→82 in E.1 and 715→715
> re-measured in the E.07 band on 11/08** (E.7).
> ⛔ **no creator at all:** `PsTruss` (E.5) · `PsPurlinDistribution` (E.6) · `PsGussetConnection`.
> ⇒ **Three categories, not two: works · refuses · nothing to call.**

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
> beam byte-identical. Proven on both B.19 and B.20.
> ✅ **RESOLVED 09/08 in B.12.6 — `PsCopeConnection` is the route, and it works.** See the B.12
> block below for the two rules that make it work: *start from a template*, and *the support
> object is mandatory*.
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
> dedicated `PsCopeConnection` exists and **is** the route (measured in B.12.6, below); and
> `GetPlateId` returns nothing because a web angle **makes no plates at all**.

> ## ✅ THE COPE, SETTLED (B.12.6, measured 09/08)
> `PsCopeConnection` — `SetConnectionObjectId(beam)` + `SetSupportObjectId(support)` +
> `SetConnectionData(...)` + `Create()`. Proof is **`mods`: `polyCuts` 0 → 1**. Two rules:
>
> ⭐⭐ **1. Always start from `GetTemplate(...)` and override it. Never build link data from
> scratch.** A hand-built `PsCopeLinkDataMgd` whose every exposed property *equalled*
> `default/Standard` did **nothing**. The template carries state the property dump does not show.
> Template + `edge`/`rathole`/`radius` overrides works perfectly — that is the supported shape.
>
> ⭐⭐ **2. `Create()` lies in BOTH directions here.** Template + support → `create=False` **and it
> succeeded**. Hand-built data → `create=True` **and nothing was made**. `Check()` is the usable
> pre-flight *for this class* (1 = will create, 0 = won't) — but only a read-back is proof.
> ⇒ **`PsPurlinConnection` then did the same on all seven of its connections.** Three classes now:
> `PsCreateBendPlate`, `PsCopeConnection`, `PsPurlinConnection`. **Treat `Create()` as carrying no
> information at all** and judge every connection by reading the parts back.
>
> ⛔ **The support object is mandatory.** The manual's ESC route — *"just enter ESC instead of
> selecting a second shape"*, a notch at the shape end — gives `Check()=0` and creates nothing,
> whatever `UseShapeEndCope`/`CutAtStart` say. There is also no `SetConnectionPoint` on this class,
> so no way to name an end. **Dialog-only.**
> ⭐ A cope lands as a **poly-cut** — the manual says that of the US-cope; it is true of the plain
> cope too. The API calls the corner access hole a **rathole** (`First/SecondRatholeDiameter`).

> ## ✂️ RESHAPING WHAT IS ALREADY THERE (B.12, measured 09/08)
> `PsEditShapeModification` — the class for *changing* members, not making them:
>
> | need | call | measured |
> |---|---|---|
> | divide | `SplitAtPoint(id, pt)` | 8000 → original keeps **2000**, a **new** member holds 6000; census **+1** |
> | combine | `ConnectWith(id, other)` | 2000 + 4000 → **6000**; census **−1**, the second is consumed |
> | shorten / extend | `ChangeLengthAtSide(id, side, len)` | ⭐ **`len` is a SIGNED DELTA, not a new length** — `+1500` twice gave 2000→3500→5000, `−800` twice gave 5000→4200→3400 |
> | ACIS escape hatch | `CreateAsAcisBody(id)` | B.12.7's way out — at the cost of a larger, slower drawing |
>
> ⭐ **The platform recipe (worked in the manual):** lay the beams through as *single* members, then
> divide them at the crossing girders — *"the risk for dimensional errors is eliminated"*. The
> separation distance is **half the crossing member's flange** (IPE300 → 75), and the dialog's
> `Distance` shortens **both** ends, so *"the arising gap has the DOUBLE distance value."*
>
> **`PsCutObjects`** for the rest: `SetAsMiterCutId` · `SetAsRoundedCutId` · `SetAsStraightCutId` ·
> `SetAsBooleanCut` + `SetSubBodyType` · `SetAsOutletCut` · `SetAsPolyCut` · `SetAsPlaneCut`.
> ⭐ **One mitre call cuts BOTH members** (`cutPlanes` 0→1 on each) — the reciprocal call returns
> `applied=0` and is redundant.
> ⭐ Boolean has a third mode the manual never mentions: `SubBodyType.**kCommenBody**` —
> intersection — alongside `kAddBody` and `kSubBody`.
>
> ⚠️⚠️ **AutoCAD's own booleans do NOTHING to a ProSteel object, silently.** The manual: ProSteel
> uses the Architectural Desktop modeller, not ACIS, so *"there will be **no errors, but nothing
> will happen**"*. Use `PS_ADD` / `PS_SUB` (or `PsCutObjects`), never `UNION`/`SUBTRACT`.
> `PS_ADD` takes the parts-list data of the **first** part clicked; `PS_SUB` **deletes** the tools.

> ## 🏠 PURLINS (B.22, measured 09/08)
> `PsPurlinConnection` — `SetSupportObjectId` (girder) + `SetConnectionObjectId` (purlin) +
> optionally `SetPurlin2Id` + `SetConnectionPoint` + `Create()`. Four kinds, via **`PurlinType`**
> (measured: `kBoltet=0 kShoe=1 kCleat=2 kShapeBased=3`), and each builds a different part:
> `kShoe` → **`Ks_BendShape`** (a bent flat steel) · `kCleat` → `FL 100x10` · `kShapeBased` →
> `L 120x80x8` · `kBoltet` → no part, the purlin bolts straight down.
>
> ⚠️ **The template NAME does not set the type.** `Default/Example-Purlinshape` carries
> **`kBoltet`**, not `kShapeBased`. `kCleat` has no shipped template at all. Take a template and
> set `PurlinType` yourself. ⚠️ Purlin templates are **`Default/`**, cope templates **`default/`** —
> copy the string, never type it.
>
> ⭐ **A second purlin is NOT required** — a gable/end purlin works with `p2` omitted. (The plugin
> used to refuse; that was an assumption, not a measurement.)
>
> ⚠️⚠️ **`WeldToSupportShape` defaults to False**, so a `kCleat` off `Default/Standard` gives a
> plate bolted to the purlins and fixed to the girder by **nothing** — Amir's "חופש" defect. Set it
> to 1 and four **`Ks_WeldFlag`** objects appear at the plate footprint. They are created even with
> **`weldStyleCount = 0`**: weld objects do not need weld styles to exist.
>
> ⚠️⚠️ **`kBoltet` drills more holes than it bolts** — a full 2×2 girder field (4 holes) and
> **one bolt per purlin**. The bolted positions are the flange-only passages (8.82 deep); the
> holes drilled clean through the section (160 deep) are never filled. Ruled out: my geometry
> (collinear *and* lapped behave the same) and grip length (U160/U100/U80 identical). The other
> three types are balanced. ⇒ **Audit hole count against bolt count after every purlin connection.**

> ## 🌀 MOVING WHAT EXISTS (B.4, measured 09/08)
> AutoCAD's own move/copy work on ProSteel objects — the manual says so plainly. ProSteel's
> versions exist for **the mouse**: they limit the move direction so a snap in a view cannot pick a
> point off-plane, and they let one pick select a whole **group**. **From code neither matters** —
> we pass an exact vector and select by handle. So `copy`/`mirror`/`rotate` already cover B.4.1–3.
> What is *not* free is Align and the rotated distribution:
>
> ⭐ **`align`** (B.4.4) — one `Matrix3d.AlignCoordinateSystem`, the same call the UCS op uses.
> Give two systems as origin + a point on X + a point on Y. *"Moved and rotated at the same time"*
> is literal. Measured: a copy landed on a 45°/z+500 target system to **0.14 mm**, the residual
> exactly perpendicular to the axis.
> ⚠️ **Orthonormalise first** (`Z = X×Y`, then `Y = Z×X`). Picked axes are not guaranteed
> perpendicular, and feeding them raw **shears** the part instead of moving it.
>
> ⭐ **`spiral`** (B.4.6) — rotated copy with vertical offset; the manual's own spiral-staircase
> illustration. Two methods, and they measurably differ: `count` treats `angle` as the gap
> **between** steps; `area` divides a **total** span by n (180 / 8 → 22.5°). Measured over 16
> treads: exactly 24° apart, exactly 190 rise, radius constant.
>
> ⚠️ **The 2-point mirror depends on the CURRENT VIEW** — *"the mirror plane is perpendicular to
> the current view"*. From code always prefer the 3-point form; it cannot be wrong for a reason
> you cannot see.
>
> 🛑 **RETRACTED 10/08/2026 — `TakeoverDrills` WORKS.** This block used to read *"Clone is
> dialog-only… it moves nothing from code — five call sequences, `changed=0` every time"*, and
> that a **matching Posnum** was the gate. **Both halves are false.** Measured on three identical
> HE300B beams: `SetToDefaults` + `SetObjectId(src)` + `TakeoverDrills(sSrc, sTgt)` and nothing
> else gives `changed=1`, each target receiving a ⌀22 hole at **its own** centre — verified by
> reading start, end and diameter back, not by a count. 5 of 8 variants transfer. The posnum
> control disproved the gate too: `changed=1` with **no** posnum, with a **different** one and
> with a **matching** one alike. Why the 06/08 run returned zero is unknown and is left unknown.
> `clonedrills variant=9` (read the holes, drill them again) still works — it is now a **choice**,
> not the only route.
> ⚠️⚠️ **And cloning is relative to each part's OWN coordinate system.** The manual's warning: a
> hole 100 from the right lands 100 **from the left** on a part whose PCS starts at the other end.
> **A part inserted the other way round gets its holes silently mirrored.**

> ## 🌀 A SPIRAL STAIRCASE, END TO END (built for Amir 09/08)
> Floor-to-floor 3000, Ø1600, SHS 200/200/4 column, 1″ railings, 4 mm treads. Every riser closed
> to **0.000 mm** and the flight lands exactly on the upper floor.
>
> **The design, all derived:** 17 risers → **176.471** each · 16 treads (the 17th arrival *is* the
> floor) · 30° per tread → 480° total · r 160→800 (640 clear) · going at the walking line **251.3**
> · **Blondel 2R+G = 604.3**, inside the 600–630 comfort band. *Twelve treads per full turn is the
> industry norm at Ø1600 — that is where 30° comes from, not from taste.*
>
> ⭐ **The anchoring detail that makes a SQUARE central column work.** Treads radiate at arbitrary
> angles but a square column has only four faces, so brackets cannot serve. Give each tread a
> **collar** — a short `RQ220x6` (208 clear over the 200 column) welded to the column — and make
> **the collar exactly one riser tall**. Stacked, the collars *set* the risers, and the tread welds
> to the top of its own collar. This is the classic spiral-stair detail and it is self-jigging.
>
> ⛔⛔ **THE TREADS MUST BE CUT TO THE COLLAR — Amir caught this by eye.** The first build gave the
> tread a constant inner radius (160) chosen "to clear the collar corners". That is backwards: a
> welded tread has to **touch**, not clear. Measured gap at the collar flats: **44.5 mm** — sixteen
> treads hanging in the air. For a square collar of half-width *a*, the inner edge must follow
> ```
> r(θ) = a / max(|cos θ|, |sin θ|)        a at a face, a·√2 at a corner
> ```
> with an extra vertex wherever a corner falls inside the tread's sector. Then it butts along its
> whole length and can be welded all round. **Whenever a round-ish part meets a square one, cut the
> contour to the square — never pick a radius that clears it.**
> ⭐ And the payoff: 30° per tread against a square's 90° symmetry gives only **THREE distinct tread
> types** for sixteen treads — three cutting programs, and 90° rotation maps the collar onto itself
> so `spiral` can place the rest.
>
> ⭐ **No gap between steps:** the tread is a bent plate — a 30° sector with a riser flap bent up
> along its **trailing radial edge**, meeting the next tread's leading edge exactly.
> ⚠️ **`AddFlange` reaches `len + radius` above the plate mid-plane, not `len`.** To land the flap
> on the underside of the next tread: `len = riser − t/2 − bendRadius`.
>
> ⚠️ **`polyplate` only builds in the z = 0 plane.** `PsCreatePlate` + `AppendEdgePoint` returns
> `create=False` at any other z, with no exception. Build the shaped plate flat, then move it — and
> for anything repeating, let **`spiral`** (B.4.6) place the rest.

> ## ⚠️ `dumpmodel` IS BLIND TO BOLTS AND PLATES — USE COM
> It reports `plates=0 bolts=0` with hundreds of `ERR … Object reference not set` rows. A
> conclusion drawn from it ("no bolts here") was wrong. `doc.HandleToObject(h).InsertPoint` bound
> **152 of 152** `Ks_Bolt` with zero failures.
> ⇒ **An instrument that reports zero must be shown to report non-zero somewhere before its zero
> is believed.** Run it against a region where the thing is known to exist.

> ## 🗂 FIVE INDEPENDENT WAYS TO CLASSIFY A PART (B.5, measured 09/08)
> All four of B.5's systems are one field each on **`PsObjectProperties`**, and the layer is a
> fifth axis on top:
> `Visible` (Hide / Regenerate) · `DisplayClass` · `AreaClass` · `FamilyClass`
> (+ `UpdateFamilyClass(objId, index)`, `DetailStyleId`, `DontDetailFlag`).
> **A part sits in exactly one of each** — assigning it to another class removes it from the first.
>
> ⭐⭐ **Part families are already in use — by the software.** Audit of 839 parts: everything a
> **connection class** generated carries a `FamilyClass` (banded 100s / 200s / 300s / 400s by
> connection kind); everything built **by hand** carries **`-1`**. Family class is what drives the
> **position-number prefix**, so hand-built parts are outside the numbering scheme until assigned.
> ⚠️ `DisplayClass` and `AreaClass` were **0 everywhere** — nothing had ever been structured.
>
> ⭐ **`AreaClass` is the right home for a construction section, a delivery lot or an assembly** —
> B.5.4 says outright it is a *selection and sorting criterion during the detailing process*.
> Measured: 324 parts assigned across ten bands, 0 failures, all confirmed by read-back.
> ⚠️ B.5.2's Regenerate is **not** AutoCAD's — AutoCAD's does not reactivate hidden parts, and
> **a parts list hides every processed part on purpose**, so Regenerate is how you get them back.

> ## 🎨 LAYERS: THE AUTOMATIC CONTROL FOLLOWS THE COMMAND, NOT THE OBJECT (B.1, measured 09/08)
> ProSteel puts each object on its own layer — but only when the object is made by the shape
> creator or a connection class. **A direct `Ps*Create*` call drops its part on whatever layer is
> current.** Measured over a full day of modelling: **88 parts stranded on layer `0`** — 69
> `Ks_Plate`, 14 `Ks_VolBody`, 3 `Ks_BendPlate`, 2 `Ks_ArcPlate` — while shapes, bolts, welds and
> work frames all landed correctly.
> 🛑 **CORRECTED 10/08/2026 — the 88 strays were self-inflicted.** This block used to end *"Pass
> the layer explicitly on every direct creation."* **That is the wrong lesson.** Asked what the
> creator does when told **not** to use the current layer and given no layer either (new op
> `layerprobe`, three plates, the current layer deliberately set to junk):
>
> | | landed on |
> |---|---|
> | `UseCurrentLayer(true)` | the junk layer ⬅ this is what the plugin was doing |
> | `UseCurrentLayer(false)` + **no** `SetLayer` | **`PS_Plate`** — the part's own layer |
>
> ⇒ ⭐ **B.1's opening sentence is true for the API as well: the automatic layer control works,
> normally you do not have to take care of it.** Fixed at six call sites; `plate`, `polyplate` and
> `beam` now land correctly with **no `layer=` at all**, and `layer=` is an **override**, not a
> requirement. ⚠️ **Solids are the genuine exception** — `PsCreatePrimitive` exposes neither
> `SetLayer` nor `UseCurrentLayer`, so the `solid` op assigns the layer *after* creation
> (default `PS_Solid`).
>
> Consequences of a stray are still real: `LELEMOFF` will not hide it, layer-based filters and
> audits miss it, and it does not take the layer colour.
>
> The manual's three switch groups map one-to-one onto real layer names, so their behaviour is
> reachable by name over COM (`Layer.LayerOn`) with no command at all:
> **element** `PS_Shape PS_RoofWall PS_Plate PS_Const PS_Bolt` ·
> **additional** `PS_Dim PS_Mid PS_Pos PS_Elev_flag PS_Weld` · **frame** `PS_Workfram`.
> ⚠️ `PS_LAYER` itself is not on `CmdAllow` — a deliberate safety control, not widened for this.

> ## 📦 GETTING AN ACIS SOLID INTO THE PARTS LIST (B.11, measured 09/08)
> An AutoCAD solid needs **no special measure** for interfering edges or 2D overviews — but for a
> **dimensioned component-part drawing** it needs an **ACIS body reference**, which gives it
> parts-list characteristics and its own component CS in the DetailCenter.
> ⇒ **This is how a bought-in machine part or an imported model reaches the parts list and a shop
> drawing.**
> ```
> PsEditShapeModification.CreateAsAcisBody(id)        -> a real AcDb3dSolid (B.12.7's escape hatch)
> PsCreateSolidReference.Create(solidId, massProp)    -> the reference
> PsSolidReference.SolidId · IsSolidErased · GetInsertUcs
> ```
> ⭐ `massProp=true` is the manual's **inertia-axes** mode and is **the only one that works.**
> 🛑 **RETRACTED 10/08/2026.** This line used to explain the refusal — *"`Create(solid, false)`
> returns 0 unless `SetInsertMatrix` supplied the 3-point component CS first"*. **`SetInsertMatrix`
> had never been called.** Tested with a world UCS and with a rotated one (`ucsSet` confirming the
> call in both): `refId=0`, census unchanged, every time. **The explanation is withdrawn —
> `massProp=false` refuses and why is NOT known.** *(A reading, not a finding: it is plausibly the
> manual's three-point pick mode, which would put it under the mouse-pick rule. Not measured.)*
> ⭐ The component CS drives **the orientation and dimensioning of the 2D workshop plan**: the X
> axis is the horizontal, and the **XY plane is always the front view**.
> ⭐⭐ **The reference dies with its solid** — measured by census: 840 → 841 (acis) → 842 (ref) →
> delete the body → **840**. Two entities gone for one delete, exactly as the manual warns.
> ⚠️ **But a COM `HandleToObject` check right after the delete still said the reference existed.**
> Only the census, and a fresh COM read, showed the truth. **After deleting, re-acquire the document
> before believing an existence check** — or trust the census, which settled immediately.

> ## 🧊 SOLIDS ARE REAL PARTS (B.10, measured 09/08)
> **`Bentley.ProStructures.Steel.Primitive.PsCreatePrimitive`** — the whole chapter in one class:
> `CreateBox` · `CreateSphere` · `CreateCylinder` · `CreateCone(startR,endR,H)` ·
> `CreateTorus(outer,inner,length)` · ⭐ **`CreateConicPipe`** · ⭐⭐ **`CreateRect2Circle`** ·
> `CreateRotation` · `CreateHull` · ⭐ `CreateExtrusion(height, **taper**, **twist**)` — taper and
> twist are undocumented. Plus `SetInsertPoint` · `SetNormal` · **`SetXYPlane(X,Y)`** · `SetPolygon`
> · `SetPoints` · `ObjectId`. Every creator returns **Void**: read `ObjectId` and the census.
> ⇒ **`RECT2CIRCLE` and `CONICPIPE` are EB's own trade** — round-to-square transitions and conical
> pipes are hoppers, chutes and tank outlets. Both build, at exact sizes.
> ✅ **A solid is a real ProSteel object**: the box drilled cleanly, 0 → 1 hole, and the manual says
> they detail as normal component parts.
>
> ⭐ **`CreateTorus`'s third argument (`length`) is undocumented AND mandatory** — `length = 0`
> creates nothing, silently; any positive value works.
> ⭐⭐ **`SetPolygon` takes LOCAL 2D coordinates, not world.** A polygon fed in world coordinates
> (x ≈ 370000) produced a **5120 × 5552** extrusion instead of 600 × 400; the same shape as
> `-300,-200 … 300,200` measured **600 × 400 × 700** exactly. This is very likely why
> `CreateRotation` refuses too — it also takes a polygon.
> ⭐ And B.10 gives the REASON behind B.12.7's silent-failure warning: ProSteel's modeller is not
> ACIS because its own *"works faster and produces smaller graph files"*.

> ## 🔗 THE CONNECTION EDITOR (B.27, measured 09/08)
> ✅ **`PsEditLogicalLink.RemoveLogicalLinkByNumber(n, DeleteParts)` — this is how you delete a
> connection.** B.26 cost several rebuilds because deleting a connection's *parts* leaves the
> **link alive**, and the next attempt then reports `parts +0` since ProSteel still believes the
> joint is there. Measured: `links 1 → 0`. (`RemoveAllLogicalLinks(bool)` for the lot.)
> ✅ `SetObjectId(id)` + `get_LogicalLinkCount()` is reliable — 74 of 321 parts, 98 links.
> ⭐ `LogicalLinkType` has **42 values** — `kCuttedByLink`, `kHolesFromLink`, `kCopedByLink`,
> `kConnectWithWebAngle`, `kConnectToBracing`, `kBoltedLink`, `kStiffenerLink`, **`kCOMLink`** … the
> vocabulary for auditing a model. And B.27's **COM-Connections** family is where the 62 `PSN_*`
> macros surface.
> ⭐ The manual's own QA gate: **green = correct · yellow = hole distances not observed · red =
> collisions** — exactly the defects found by hand in B.22 and B.26.
>
> ⛔⛔ **But the per-link detail is NOT readable, and an audit built on it had to be retracted.**
> `PsEditConnection` has **no binder**, so `LinkType` always returns `kUndefinedLink`; and
> `getBoltObjectId(0)` / `getLinkObjectId(0)` return 0 on every link. A verifier written on those
> reported **32 flagged connections, every one false** — it flagged the B.22 purlins, which carry
> 23 bolts measured through COM.
> ⇒ **An audit is only as honest as its instrument.** A verdict column that cannot fail looks like
> knowledge and is noise. **Judge a connection by GEOMETRY** — bolt positions against hole
> positions — never by an API field that may be returning its default.

> ## ⚠️⚠️ A HOLE'S DEPTH TELLS YOU WHERE IT REALLY WENT (B.26, measured 09/08)
> **The drill places holes by the HOST'S geometry, not at the point you pass.** Aiming at an
> HE300B's flange three different ways gave: an **11 mm** hole on the column **axis** (that is the
> *web*), a **300 mm** hole clean through both flanges after `rot=90`, and — via
> `drillspecial kind=blind` — a 19 mm hole on the **far** flange. Each time the mating plate's holes
> ended 160–310 mm away, so `boltparts` created nothing **and its Gap-distance message was right**.
> ⇒ **Always read hole coordinates and DEPTH back before bolting.** Depth is also a free
> section-orientation probe: web thickness vs flange thickness says instantly which way a member
> is turned — that is how a portal whose columns were bending about their **weak axis** was caught
> (`rot=90` fixed it).
> ⇒ And grip: a 300 mm through-hole + 20 mm plate asks for a **320 mm** grip, outside every style's
> window (B.15). Zero bolts, silently.
>
> ⭐⭐ **The lesson underneath: use the connection class.** The eaves of a bolted portal was
> hand-rolled three times from plate+drill+bolt and gave **0 bolts every time**.
> `conn kind=endplate` (`PsStandardPlateConnection`) built the whole corner — end plate, 4 holes in
> the column, **4 bolts at a 55 mm grip** — on the first call. This file already said *"reach for
> the connection class before building a detail out of B.14/B.15 primitives"*; it cost three
> rebuilds to relearn it.

> ## 🏗 HAUNCHES (B.26, measured 09/08)
> `PsHaunchConnection` + `PsHaunchLinkDataMgd`: `Length` · `TopHeight` · `BaseHeight` ·
> `WebThickness` · `Slope` · `IsConical`/`ConicalWidth` · **`IsBottomTrain`** (the frame-corner
> case: no top flange) · `IsCopedShape`/`CopedHeight` · `SizeDependsToConnected` ·
> `StiffenerAtSupport`/`StiffenerAtConnected` (⭐ these take **B.16 stiffener templates**) ·
> **`XAxis`/`YAxis`/`InsertPoint`**.
>
> ⛔⛔ **The shipped template carries a ZERO PLANE** (`X=0,0,0 Y=0,0,0`). Passing it through
> **stretched both rafters of a portal to ~317,000 mm**, across the whole drawing into another
> band 300 m away — `create=True`, no error. **Always set `XAxis`/`YAxis` explicitly.** With the
> frame's real plane the rafter was untouched (3162.278 → 3162.278).
> ⇒ **Any op that can resize an existing member needs a blast guard**: measure the length before
> and after and shout. A destructive operation must never fail quietly.
> ⛔ Even with the plane right, the haunch's parts build at the **support's origin**, not at the
> connection point — `SetConnectionPoint`/`InsertPoint` do not move them.

> ## 🔺 BRACING: ONE CLASS, TWO CHAPTERS, THIRTEEN REFUSALS (B.24 + B.25, measured 09/08)
> `PsBracing` serves **both** — it carries **`setDynamicStatus(bool)`**, and that flag *is* the
> difference between B.24 (dynamic, reacts to changes) and B.25 (static, doesn't). There is no
> separate static class.
> ⭐ **`insert(Origin, X_axis, Y_axis)` takes the plane AS ARGUMENTS** — it does not read the active
> UCS. That is why setting the UCS never helped: the plane was already a parameter.
> ⛔ **Thirteen configurations now refuse** — six in B.24 (shape type, two catalogue spellings,
> `recalcPoints`, minimal, UCS) and seven in B.25 (static, static+layout, static+welded,
> static+no-gussets, static+shorten, dynamic control, wrong-plane control). `insert()` = False,
> census unchanged, every time. **The integrated command is interactive. Stop trying.**
>
> ✅ **Build it by composition instead — the manual explicitly permits it:** *"it is possible as
> well to generate single components such as gusset plates, etc. individually."* SINGLE ROD → a
> shape · DRILL ROD → `PsDrillObject` · PLATE AUTO → a gusset plate · bolts → **drill, then bolt the
> pair**. A full braced bay built this way at x=310000: 2/2/2 holes-and-bolts at all four ends.
>
> ⭐ **Two details only the dialog knows.** `Bracing Rod` (`setShapeShorting`) — *"the value by which
> the rod is **shortened** after insertion. **Thus the rod will be kept in tension**"*: cut it short
> on purpose. And `Rod Position` **Front / Rear** (`setLayout`: `kAtFront, kAtBack, kCrossed,
> kCentered, kDoubled, kButterFly, kQuatro`) staggers crossing rods so they do not collide.
> ⭐ `Weld Bracing` (`setWeldStatus`) suppresses **all** drilling because there are **no bolts** —
> the gussets are still sized *as if* there were. Not an iron-rule exception; a joint with nothing
> to bolt.
>
> ⚠️ **Place bracing holes INWARD from the rod end, never symmetrically about the work point.**
> Spreading them ±70 about the corner put one of each pair past the end of the rod: gusset 2 holes,
> rod 1, **1 bolt — an unfilled hole**. The Edge Distance page is the recipe: `Edge–1st Hole`, then
> `Hole–Hole` along the rod. (And holes cannot be removed — fixing it meant rebuilding the parts.)

> ## ⭐⭐ THERE ARE 62 `PSN_*` MACRO ASSEMBLIES — THE CONNECTION CENTER'S OWN (found 09/08)
> `…\ProStructures Ss6 R1\AutoCAD 2015\Prg\PSN_*.dll` — a whole surface beyond `ProStructuresNet.dll`,
> and it holds exactly what has been blocking this project: **`PSN_HollowShapeBracing`** (B.24),
> **`PSN_DualGusset`** / `PSN_WraparoundGusset` (B.23), `PSN_BasePlate`, **`PSN_STAIRS`** (50 types),
> **`PSN_HANDRAIL`** (46), `PSN_Truss` (49), eight `PSN_BeamColumn*`, five `PSN_BeamBeam*`,
> `PSN_CircularPlatform`, `PSN_CatWalk`, `PSN_RodBrace`, `PSN_PipeFlange`, …
>
> Their shape is nothing like the `Ps*` classes:
> ```
> UserConnection   Create() · InitialCall() · CreateClone(ClsParameters) · Build/BuildI ·
>                  Draw/DrawI · Edit/EditI · GetIdentifier() · GetDescription()
> ClsParameters    SetDefaultValues(bool metric) · ReadFrom/WriteTo Connection|Clone|Template
>                  + every dialog field as a property
> ```
> They instantiate, identify themselves and give real metric defaults (bracing: M20 `DIN7968`,
> plate 10, gap 10, clearance 20). **Compile against the specific DLL to reach them.**
>
> ⛔ **But every entry point is INTERACTIVE.** `InitialCall()`, `CreateClone(params)` and `Create()`
> all print *"Choose support shape"* and park the session; all three returned 0 and created nothing,
> even with `ConnId1`/`ConnId2` pre-set to real ids.
> ⇒ **B.24 closed: bracing is not creatable from code, by design** — `PsBracing.insert()` refuses
> (six configurations) and the macro asks for picks. Same reasoning closes B.23's gusset, which the
> bracing command generates and therefore inherits the interactivity from.
>
> ## ⚠️⚠️ RECOVERY FROM A PARKED MACRO PROMPT: **ENTER**, NOT ESC
> A session stuck at a `PSN_*` prompt survived `eb_escape.py` ×4, PostMessage `ESC` ×6 **and real
> `SendInput` ESC keystrokes ×5** — `ping` returned `EB_BUSY` after every one. **One ENTER cleared
> it instantly** (at a selection prompt ENTER = "done selecting, nothing selected", so the macro
> aborts). Add ENTER to the recovery sequence, and **never call a `PSN_*` entry point unattended.**

> ## 👁 A "VIEW" IS A WORK FRAME (B.7, measured 09/08)
> Activating a view does three things at once: **moves the UCS** onto the work plane, **points the
> camera perpendicular** to it, and **switches on clipping planes** so everything outside the slab
> disappears. Views are not their own object type — they *are* the work frames B.6 creates.
>
> ⭐ **The whole chapter lives on `PSCOMWRAPPERLib.IKs_ComWorkFrame`** — no plugin needed, drive it
> straight from Python via `doc.HandleToObject(h)`:
> `SetActive(ZoomExtents, UseClipPlanes)` · `Get/SetClipDistances(front, back)` ·
> `Get/SetCameraView(eye, target)` (the manual's "view via 2 points") · `GetInsertUcs` (the
> "activate only the UCS" button) · `Delete()`.
>
> ⚠️ **`EnableFrontClip` / `EnableBackClip` are a trap** — writing `True` reads back `False` and
> changes nothing. Clipping is controlled by **`SetActive`'s second argument**. Set the *distances*
> as properties, the *on/off* through the call.
> ⚠️ **Generated views arrive with `clip 0/0`** — the clipping the manual describes does nothing
> until distances are set.
> ⭐ B.6's four auto views are FRONT/BACK/TOP/BOTTOM, and the camera sits **exactly 100 mm off the
> target plane** every time (`eye = target + 100·normal`). Match that when writing `SetCameraView`.
>
> ⭐⭐ **And the one place a screenshot IS evidence.** This file insists everywhere else on reading
> geometry back, because an image cannot show whether a hole exists. B.7's claim is *about what is
> displayed* — so the display is the right instrument, and reading geometry back would prove
> nothing, since clipping changes no geometry at all. **Match the instrument to the claim.**

> ## ⚠️⚠️ A BOUNDING BOX CANNOT ANSWER "DOES IT TOUCH?"
> Twice in one hour, checking whether spiral treads met their collar, an audit built on
> `GetBoundingBox()` returned a confident wrong answer — first "all sixteen fine", then "eleven
> floating". Both were the **instrument**, not the model.
> A bbox is **axis-aligned**, so for a rotated sector its corners are points the part does not
> occupy: a tread spanning 120°–150° reported a closest approach of 63.5 when its inner edge
> genuinely sat at 110. Rotate the part and the answer changes even though nothing moved.
> ⇒ **For contact, clearance or fit, read the real contour** — `plateinfo`'s `ext=`, `GetPolygon`,
> or the generating geometry. Keep bboxes for "roughly where is it".
> ⇒ And the general form: **when a verdict flips as the part rotates, suspect the test first.**

> ## ⚠️ NEVER ASSEMBLE A SECTION KEY — SEARCH FOR IT
> `name="HEB300"` → `create=False`, census unchanged, **no exception**. The real DIN key is
> **`HE300B`**. A composed key does not throw; it just quietly makes nothing.
> ⇒ Run `sections filter=…` and copy the key out of the list. This is the same lesson as the
> catalogue survey — *a section key is an opaque string*.

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

> ## ⚠️⚠️ THE MANUAL DESCRIBES THE DIALOG, NOT THE API
> Twice in one day a headline capability turned out to be unreachable from code:
> - B.15 — *"the components had to be **drilled first. Which now is not necessary any more**."*
>   `PsCreateBolt.AddObject + Create()` still returns `create=False` with no holes.
> - B.12.6 — *"just enter **ESC** instead of selecting a second shape"* (a notch at the shape end).
>   `PsCopeConnection` gives `Check()=0` without a support object, whatever flags are set.
>
> ⇒ **A feature described in the manual is a claim about the dialog.** Before building on one,
> test it and record the result. Both of these are *deliberate* dialog affordances — an
> interactive pick the API has no parameter for.

> ## ⚠️ `PsObjectProperties.Weight` IS THE NOMINAL WEIGHT (measured 09/08)
> A 1200×600×20 plate reports **113.04 kg** — exactly 1.2 × 0.6 × 0.02 × 7850 — and it stays
> 113.04 after **two boolean subtractions** and **five ⌀60 holes**, across a `PS_REGEN`.
> **It ignores all material removal.**
> ⇒ **Never judge whether a cut landed by the weight.** Judge it by `mods` — `polyCuts`,
> `cutPlanes`, `subBodies`, `outlets`, `holeFields`. A suspicion this session that a subtraction
> had silently failed came from reading the weight instead.
> ⇒ For **ordering** material this is arguably the right number (the gross plate); for a finished
> part weight it is not. Know which one a parts list is quoting.

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
bulb flats, even timber. Full map + traps: `knowledge/learning/findings/SECTION-CATALOGUES.md`.

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
  read-only `GripMin`/`GripMax`, so the window is real and enforced. **A long anchor rod is not a bolt.**
- ⛔⛔ **`PsCreateFastener` CREATES NOTHING — corrected 09/08.** This file used to send anchor rods
  here. Measured on the staircase base: **all four kinds** (`Straight` / `Hook` / `Bend` / `HeadBolt`)
  × **three styles** (`''`, `DIN7990`, `8.8S`) × with and without `SetObjectId(host)` × with the three
  embedment segments filled instead of zeroed — **twelve-plus combinations, zero entities**, confirmed
  by diffing `ModelSpace` handles over COM, not by a census. With a host id it returns a **non-zero
  value that is not an object id**, which is what made it look alive.
  ⚠️ A round bar (`RD20` @ `DIN RUNDSTAHL`) through the drilled hole gives correct *geometry*, but
  it is a **`Ks_Shape`** — it lands in the profile list, not the bolt list. Amir caught exactly that:
  *"למה זה פרופיל ולא בורג עיגון כמו שמידלנו בפלטת בסיס?"* Do not ship it as an anchor.
  ✅✅ **The anchor route that works is the BASE-PLATE CONNECTION** (`connbase`,
  `PsBasePlateConnection` **from a template**) — it produces four real **`Ks_VolBody`** anchor
  bolts plus the plate, in one call. Measured 09/08 on the staircase column.
  ⚠️ **But its numbers do not land.** `anchordrill=140` gave 100; hole spacing ±150 gave ±75.
  Only the *template's* values arrive — the same "values that never arrive" signature recorded
  earlier for this class. **Workaround: build it, then POSITION the assembly** (`align copy=0`) so
  the embedment comes out right. Getting 120 mm into concrete meant dropping the whole base
  assembly by 20, not setting a parameter. Spacing stays whatever the template says.
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
   would have caught: `EB PROSTEEL AGENT/knowledge/learning/lessons/RETROSPECTIVE-L4-L5.md`.
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
   `knowledge/learning/manual/manual_fulltext.txt` (1,179 pp) · `Prg\Plugins\*.chm` (34) ·
   `Samples\COM Macros\` (25 worked DWGs, one per macro) · `Samples\Detailing\` (12) ·
   `knowledge/api/API-SURFACE-RAW.txt`. Manual chapters already mapped: `B.4` move/copy/mirror ·
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
checker in the system.** Full record: `EB PROSTEEL AGENT/knowledge/learning/lessons/BASELINE-L5-2026-08-05.md`

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

---

# 🔎 THE PART-B AUDIT — 10/08/2026

*Amir commissioned a chapter-by-chapter self-audit of manual part B with three questions each: can
anything be improved, was the chapter learned deeply enough for the API work, and if there is
something to improve — do it in the practice models. B.1 → B.14 in this pass. What follows is what
changed, not a diary.*

## ⭐⭐⭐ The rule the whole audit produced

> **A verdict is only as good as the test that produced it.**

Four conclusions were **withdrawn** in one day, and every one of them had named its own missing
test and then been filed as a finding anyway:

| chapter | the claim | what the named test showed |
|---|---|---|
| **B.4** | `TakeoverDrills` *"moves nothing — 5 sequences, `changed=0`"* | **it transfers**, `changed=1`, geometry read back |
| **B.9** | *"the grating weight claim cannot be verified — the weight call is lethal"* | `props` reads plate weight **safely** through `PsObjectProperties` |
| **B.10** | `CreateHull` *"never a fair test — a plugin change is required"* | plugin changed → **it builds**, exactly |
| **B.11** | *"`massProp=false` declines because `SetInsertMatrix` was not called"* | `SetInsertMatrix` **was never called**; calling it changes nothing |

⇒ ⭐ **An explanation written next to a measurement must say which of the two it is. If a note
names the missing call, the note is a TO-DO, not a finding.**

⇒ ⭐ And its mirror, from B.1: **the 88 layer strays were self-inflicted.** Before blaming the
software, check what your own call passed.

## ⛔ LETHAL CALLS — read `knowledge/learning/findings/LETHAL-CALLS-do-not-invoke.md` first

Two calls **end the AutoCAD process**: no exception, no dialog, `EB_TIMEOUT` on the Python side and
an empty `Get-Process acad`.

| call | class |
|---|---|
| `computeObjectWeigth(bool)` | `PsPlate` |
| `checkHoleEdgeDistance(int)` | `PsVolume` |

⚠️ ⭐ **Both are `check*`/`compute*` methods on the entity classes. Treat every one of them as
suspect** — `checkDoubleHoles`, `checkValidHoleFields`, `computeHoleField`, `checkLogicalLinks` are
untested neighbours. **Save the model immediately before, isolate the call in its own run, and
check `Get-Process acad` afterwards** — a timeout does not distinguish a hung session from a dead
one. That protocol is why B.14's crash lost nothing and B.9's lost five plates.

⇒ **Plate weight has a safe route: `op=props` → `wt=`.** It goes through `PsObjectProperties` and
never near `computeObjectWeigth`. ⚠️ But it is the **NOMINAL** weight — unchanged by booleans and
by holes alike (a 1200×600×20 plate still reads 113.04 kg after two subtractions and five ⌀60
holes). Never judge a cut by the weight; judge it by `mods`.

⇒ **The edge-distance table is therefore dialog-only in practice.** `checkHoleEdgeDistance` is the
manual's admissible-edge-distance check and it cannot be called. The `edgecheck` op exists and
**refuses**, so nobody rediscovers it.

## ⭐⭐⭐ 1 890 sections that one hardcoded line made unreachable (B.8)

`PsCreateShape` exposes **four** selectors and the `beam` op called only the first.

```
beam kind=standard   name="HE 300 B"                             90 catalogs
beam kind=special    catalog=SCHRAG_z-pfetten name=Z140-15       68 catalogs, 1528 sections
beam kind=roofwall   catalog=Bardage  name=4-250-36bx100         20 catalogs,  270 sections
beam kind=combi      catalog=Dreiecksbinder name=R273x28-H440    15 catalogs,   88 sections
```

**The folder is the `catalog=`, the `.psp` filename is the `name=`.** Some catalogs are **`.dbf`
tables** instead (`SCHRAG_z-pfetten`, `SCHRAG_c-riegel`, `Kantteile`, `Steel Deck`) — there the
name is the row's **`KEY`**.

⚠️ **Address a section by its FILENAME.** `Dreiecksbinder/R273x28-H440.psp` reads back as
`name='R244.5x22.2-H420'` while its W/H match the filename. The internal name field lies.

⭐ **What this unlocks for EB:** cold-formed **purlins** (`SCHRAG_z-pfetten`, `SCHRAG_c-riegel`,
`Sadef_zed/cee/sigma`, `sbe_c/z/zeta`, `ayrsh_zeta`, `ayrshire_eb`) — what **B.22 needed and never
had**; **crane rails** (`Kranschienen_Form_A`, `krupp_z*`); **Halfen cast-in channels**
(`halfen_hl/hm/np/p`); bent sheet (`Kantteile`); decking (`Steel Deck`); stairs (`stair`).

### `bendshape` — B.8.2, which had no op at all
`PsCreateBendShape` appeared nowhere in the plugin (`bend` bends a **plate**). It is also the
**only creator with `SelectWeldSections`** — the straight creator has 4 selectors, the bent one 5 —
so welded plate girders (`I950x300x30`, `K900x400`) are reachable **only** this way.

```
bendshape name="HE 200 B" pts=0,0,0;0,2500,0;2000,4000,0     |  circle=R  |  helix=r,ang,rise,res
```
⚠️ **≥ 3 path points required** — two create nothing, same section name.
⚠️ **`handle=` (`ConvertFromPolyline`) does not follow arcs** — a 90° bulge left a vertex **650 mm
outside** the result. Every call now reports **`pathfit=ok`** or **`pathfit=MISMATCH …`**. **Read
it.** A route that silently builds different geometry from the one asked for is the worst kind.

## What else changed in the toolbox

| what | chapter |
|---|---|
| ⛔ **`dumpmodel` was BLIND** to every plate and every bolt until 10/08 — `plates=0 bolts=0 err=357`, one unguarded `InsertPoint` dereference. **Any dumpmodel result older than 10/08 has false zeros.** `err=` is not noise — open it | B.9 |
| `PLATE`/`BOLT` rows now carry a **world bounding box** — a plate's `InsertPoint` reads `0,0,0` and its polygon is in **local** coordinates | B.9 |
| **Gratings**: `plate9 grid=1` sticks and is readable — `DisplayFlagsLong` bit **8192** + `PitchLineMode=True`. `Ks_ComGlobalSettings.PlateRasterWeightReduction` ships at **10 %** and does **not** reach the object weight | B.9 |
| `solid kind=hull` needs **`dpts=`** (`PsDataPointArray`), not `pts=`. ⚠️ a **perfect box** creates nothing; jitter the corners 10 mm and the same eight points build | B.10 |
| `solid kind=rotate`: polygon is **LOCAL 2D (x,y)**, the **axis is WORLD**, and the axis must lie **in the profile's plane** (a Z axis is degenerate geometry, not a refusal). ⚠️ `rev` is ignored — 90/180/360 identical | B.10 |
| **`edgechamfer layout=`** — the six kinds the manual promises and never names: `1 kFacet · 2 kRadius · 3 kRounded · 4 kInverted · 5 kFold · 6 kNotch`, all applied and read back | B.13 |
| ⚠️ ⭐ **The two APIs can DISAGREE.** `.NET EdgeLayout` has **7** members, COM `KsEdgeLayout` has **6** — `kNotch` is .NET-only. Same enum name, different members. **When they disagree, .NET is the complete one.** COM rescues .NET on *binding* and can be *behind* it on *content* | B.13 |
| `PsEdgeChamfer` is a **data payload, not a creator** — no `Create`/`insert`/`writeTo`; assign it to `em.PlateBreakEdge`. `Min. Radius`/`Max. Height` are **dialog-side validation only** | B.13 |
| **`DisplayClass`** is writable, sticks, and is **independent** of `AreaClass` (one part holds `d1` and `a12` at once). It was **0 on all 820 parts** — a whole assignment system read and never used | B.5 |
| **`Ks_ComGlobalSettings.ObjCutPlaneDistance` / `…Rear`** = 500.0 — the global cut-plane distances B.3.5 calls "settings only". And `SetActive(True,True)` does **not** overwrite a view's own pair (1750/1250 survived) | B.3, B.7 |
| **B.6.7 Additional Axes is dialog-only, tested**: `PsCreateGrid` is the creator with no user-axis methods; `PsGrid` has the axes and no creator and no binder. The halves never meet | B.6 |
| ⚠️ **A cope that does nothing may be a GEOMETRY error, not an API refusal** — the beam must run **through** the support, not butt against it. Same call, `polyCuts` 0 → 1 | B.12 |
| Missing capability, named: **a reader for a polyCut's polygon.** Without it, `edge=2` (Access Holes) and `edge=0` (bevelled) copes are indistinguishable — identical extents to 0.1 mm | B.12 |
| **No ProSteel construction-line type exists** in the managed API — which is why `PS_CONST_DEL` is a **layer** sweep, not a type sweep | B.2 |

## ⭐⭐ Drilling: inherit, never invent (B.14)

`drillfield x= y=` takes the **dialog's own layout string** — proven by spacing, not by count:

```
x=3*70          ->  gaps 70, 70        uniform
x=2*60,200,1*   ->  gaps 60, 200       non-uniform, honoured in full
```

⭐⭐ **`W` in place of a pitch = the SECTION's own marking gauge**, and it works through the API
with no dialog:

| beam | `y=` | measured |
|---|---|---|
| **HE 300 B** | `2*W` | **120 mm** |
| **IPE 300** | `2*W` | **80 mm** |
| HE 300 B | `2*100` *(control)* | 100 mm |

⇒ **Never invent a bolt gauge.** This is the drilling half of *choose a template, do not pass
numbers*.

## ⚠️ And one the auditor did to himself

`plate9 mode=rect` places from **`at=`**, not `p1=`. `p1` is a valid key **for the op** (the `poly`
and `pts` modes use it), so the strict-parameter guard accepted it **in silence** — and nine plates
built during this audit stacked up at the origin while their labels pointed at empty strips.

⇒ ⭐ **Valid FOR THE OP is not valid FOR THE MODE.** The guard gave false confidence. `mode=rect`
now refuses `p1/p2/p3/pts/radius` and creates nothing.

---

## 🚦 חוק הרישום — נקבע 10/08/2026, אחרי כשל אמיתי

**מה קרה.** ביקורת של 14 פרקים הפריכה ארבע מסקנות. שלוש מהן **נשארו חיות בסקיל** שהסוכן טוען
וקורא. אחת מהן אפילו חתמה במשפט *"recorded so this is never re-investigated from scratch"* —
כלומר טקסט מיושן שהורה לקורא הבא **להפסיק לחפש** דבר שזה עתה הוכח שעובד. שלושה קומיטים לא נגעו
בשום קובץ סקיל, ו-`sync.py` דיווח כל הזמן `backup already current` — אמת מדויקת וחסרת ערך: הוא
משווה את הגיבוי לסקיל, ולא אומר דבר על האם הסקיל בכלל נכתב.

**הכשל לא היה חוסר סדר.** הוא היה שאותה עובדה חיה בכמה מקומות ו**שום דבר לא שמר עליהם מסכימים**.

### ⭐⭐ הכלל

> **ממצא לא "נרשם" עד שכל מקום שסותר אותו תוקן.**
>
> **וכשמפריכים משהו — לא מוחקים בשקט. כותבים את ההפרכה במקום שבו הטענה הישנה ישבה, ומוסיפים
> שורה ל-`qc/retracted.tsv`.** מחיקה שקטה מותירה את הקורא הבא בלי הסבר למה הכיוון הזה ננטש;
> שורה ב-`retracted.tsv` הופכת את הטעות לבלתי-חוזרת **על ידי מכונה במקום על ידי מזל**.

### שלושה כללי-משנה שנולדו מאותו יום

1. **סנכרון אינו כתיבה.** `sync.py` מגבה בלבד. `backup already current` פירושו "הגיבוי תואם",
   לא "הידע נשמר". לוודא שהקובץ **עצמו** השתנה — `git show --name-only` על הקומיט.
2. **אל תכתוב "אל תחקור שוב" על תוצאה שלילית.** ממצא שלילי טוב רק כמו התנאים שהבדיקה כיבדה.
   מותר לכתוב *מה נמדד*; אסור לכתוב *להפסיק להסתכל*.
3. **הסבר ליד מדידה חייב להצהיר מי הוא.** אם רשימה מנקבת בקריאה שלא בוצעה — זו **משימה**, לא
   ממצא.

### הנוהל, כשלב 4.5 בכל פרק

```bash
python qc/consistency.py
```

עוצר את הקומיט אם: טענה מופרכת עומדת חיה · זיכרון לא באינדקס · גיבוי הסקיל לא תואם · גרסת התוסף
שגויה · פרק בביקורת בלי סימון בהערות שלו. ‏`python qc/selftest_consistency.py` מוכיח שהשומר עצמו
עדיין תופס — שומר שמעולם לא נכשל מעולם לא נבדק.
