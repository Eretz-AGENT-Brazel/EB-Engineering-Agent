# E.10 — ProSteel Command Reference, corrected

*Manual pp. 1173–1178. Extracted 10/08/2026 straight from the manual text, then the chapter
column re-derived from the manual's own table of contents.*

---

## ⚠️ The chapter column in the printed table is STALE

E.10 gives three columns: **Function / Chapter / Command Name**. The command column is
reliable. **The chapter column is not.**

Tested exactly, with no guesswork: of the 126 rows, **50** have a function name that matches
a chapter title in the table of contents *verbatim*. Of those 50:

| | |
|---|---:|
| chapter cited correctly | **26** |
| **off by exactly one** | **16** |
| off by two or three | 8 |

The pattern is systematic, not random:

* **Part B is correct through B.10 and off by one from B.11 onward.**
  `3D-Modifications` is cited as B.11 — but B.11 is *Create ACIS body reference* and
  3D-Modifications is **B.12**. `Plate Editor` → really B.13. `Bolts` → really B.15.
  `Web Angle` → B.19. `Shear Plates` → B.20. `Purlin Connection` → B.22. `Gusset Plates` →
  B.23. `Connection Editor` → B.27. `Positioning` → B.29. `Drawing Information` → B.30.
* **Part C is off by two or three** — `Manual 2D-Cut` C.11 → really C.13, `Global Scale`
  C.9 → C.11, `2D-Cutout` C.16 → C.19, `Flatten Viewport` C.19 → C.22.
* **Part E is off by one from E.2 onward** — `Handrail` E.2 → really E.3, `Hangar Frame`
  E.3 → E.4, `Truss Girder` E.4 → E.5, `Ladder` E.6 → E.7, `Joist` E.7 → E.8.
* **`RevisionCenter` is in the wrong part entirely** — cited D.6, documented at **C.6**.

⭐ The tell, and the cause: **the one row that cites B.11 correctly is "Create ACIS body
reference" itself** — the chapter that was *inserted* in a later edition and pushed
everything after it down by one. The table was written against the older numbering and
the chapter column was never renumbered. Part C took two or three insertions the same way.

⇒ **Use the command names. Re-derive the chapter from the table of contents.** In the table
below, every chapter that could be checked and was wrong is struck through and corrected.
**24 rows corrected.**

---

## Coverage — how much of the command surface the agent reaches

**66 of 126 rows** map to an op in the plugin (124 distinct commands in the manual's table).
The rest are detailing, output and interface commands, which belong to parts C and D and
have not been studied yet.

* 🧱 **3** are on **THE CEILING** — the command exists, the API creator does not.
  That is not a gap in the agent; it is the shape of the product. Every one of them
  requires a mouse pick.
* ❌ **1** is a route that should work and does not (`PS_OUTLET`, see E.9's notes).
* ⭐ **2 were newly reached this morning** through E.9's write surface: `PS_FAMILY_CLASS`
  and `PS_PROCESS_STATUS` are now `propset FamilyClass=` and `propset ProcessStatus=`.

> ### 🔒 The `cmd` allowlist is deliberately tiny, and stays that way
> The plugin will run only **9** of these 126 commands: `PS_POS`, `PS_POS_SNG`,
> `PS_POS_BGR`, `PS_POS_DIFF`, `PS_COLLISION`, `PS_EXPLODE`, `PS_REGEN`,
> `PS_EDIT_CONNECTIONS`, `UCS`. Everything else is refused by name with the list printed.
>
> **This is a safety control of Amir's, not an oversight.** Most of these commands are
> interactive — they open a dialog or wait for a pick, and an unattended agent that starts
> one leaves the session parked. Nothing gets added here without Amir saying so, one command
> at a time, with a reason. `UCS` was added on 09/08 exactly that way.

---

## The table

*Command column verbatim from E.10. Chapter column corrected where it could be checked.*

| Function | Command | Chapter | Agent op |
|---|---|---|---|
| 2D-Cut Out | `PS_CUT_OUT` | ~~C.16~~ → **C.19** | — |
| 3D-Modifications | `PS_MODIFY` | ~~B.11~~ → **B.12** | `mods` `boolean` `miter` `cutat` `polycut` `planecut` |
| Add 3D-Volume | `PS_ADD` | B.11.7 | `boolean mode=add` |
| Add Shape Segment | `PS_ADD_SECTION` | B.8.5 | `shapeedit` |
| Area Classes | `PS_AREA_CLASS` | B.5.4 | `classify set=area` |
| Automatic 2D-Cut | `PS_ADD_2DCUT` | ~~C.18~~ → **C.21** | — |
| Benchmark | `PS_BENCHMARK` | C.6 | — |
| Bend Edged Plates | `PS_PLATE` | B.9.4 | `plate` `polyplate` `arcplate` `platepoly` |
| BlockCenter | `PS_BLOCKCENTER` | D.2 | — |
| Bolts | `PS_BOLT` | ~~B.14~~ → **B.15** | `bolt` `boltparts` `boltfield` `boltsingle` |
| Bracings | `PS_VERBAND` | B.24 | 🧱 **CEILING** — composed by hand instead (B.25) |
| Bracings, dynamic | `PS_BRACING` | B.23 | 🧱 **CEILING** — 13 configurations refused |
| Butt-Joint Connection | `PS_LASCHE` | B.20 | `splice` `connsplice` `splicetemplates` |
| Calculate Center of Gravity | `PS_WEIGHTCENTER` | D.5.2 | `props` — `Weight` / `VolumeWeightFlag` |
| Caption texts | `PS_TEXTFLAG` | C.17 | — |
| Chamfer Plate Edge | `PS_CHAMFER` | B.12.2 | `chamfer` `edgechamfer` |
| Circular Stairs | `PS_CIRCULAR_STAIRS` | E.2 | — |
| Clean AutoCAD-Drawing | `PS_CLEAN_PROXY` | D.5.6 | — |
| Clean AutoCAD-Drawing (Batch) | `PS_BATCH_CLEAN_DWG` | D.5.6 | — |
| Collision Check | `PS_COLLISION` | D.5.1 | `collision` · **on the cmd allowlist** |
| Connection-Editor | `PS_EDIT_CONNECTIONS` | ~~B.26~~ → **B.27** | `connscan` `conndump` `connkill` · **on the cmd allowlist** |
| Construction Lines | `PS_CONST` | B.2.1 | `grid` `gridpoints` `gridcolumns` |
| Containlist | `PS_DWG_CONTAINLIST` | B.32 | — |
| Convert ADT-Shapes | `PS_CONVERT_ADTSHAPES` | D.5.7 | — |
| Cranked 2D-Cut | `PS_CRANKEDVIEW` | C.5 | — |
| Create ACIS-Solid | `PS_CREATE_ACIS` | D.5.5 | `acis` |
| Create ACIS-Solid (Batch) | `PS_BATCH_CREATE_ACIS` | D.5.5 | — |
| Create ACIS body reference | `PS_SOLIDREFERENCE` | B.11 | `acisref` |
| Create 3D-Cone | `PS_SOLID_CONE` | B.10 | `cone` |
| Create 3D-Conic Pipe | `PS_SOLID_CONICPIPE` | B.10 | `conicpipe` |
| Create 3D-Cross-section transition | `PS_SOLID_RECT2CIRCLE` | B.10 | `solid` rect2circle |
| Create 3D-Cuboid | `PS_SOLID_BOX` | B.10 | `box` `solid` |
| Create 3D-Cylinder | `PS_SOLID_CYLINDER` | B.10 | `cylinder` |
| Create 3D-Extrusion Solid | `PS_SOLID_EXTRUDE` | B.10 | `extrude` |
| Create 3D-Rotation Solid | `PS_SOLID_ROTATE` | B.10 | ⚠️ `solid` refused — axis framing never varied |
| Create 3D-Solid | `PS_SOLID_HULL` | B.10 | ⚠️ `hull` — needs `SetPoints`, never fairly tested |
| Create 3D-Sphere | `PS_SOLID_SPHERE` | B.10 | `sphere` |
| Create 3D-Torus | `PS_SOLID_TORUS` | B.10 | `torus` |
| Cut Plane | `PS_CUTPLANE` | B.3.6 | `planecut` |
| Data Export | `PS_EXPORT` | D.7.5 | — |
| Data Import | `PS_IMPORT` | D.7.4 | — |
| DetailCenter | `PS_DETCENTER` | C.1 | — |
| Dimensioning Points | `PS_INSERT_MANDIM` | C.7 | — |
| Dispatch Bolts and Blocks | `PS_DISPATCH` | D.4 | — |
| Display Pickhelpers | `PS_PICKHELPER` | C.1.6 | — |
| Display Program Version | `PS_VERSION` | A.1.2 | `whoami` |
| Drawing Information | `PS_DWG_INFO` | ~~B.29~~ → **B.30** | — |
| Drawing Frame | `PS_FORMAT` | C.10 | — |
| Drawing Partslist | `PS_DWG_PARTLIST` | ~~B.31~~ → **B.32** | — |
| Drill and Bolt | `PS_DRILL` | B.13 | `drill` `drillfield` `touchdrill` `drillspecial` `holes` |
| DSTV NC-Interface | `PS_NC_DATA` | D.7.8 | — |
| DSTV PPS-Interface | `PS_PPS` | D.7.8 | — |
| DSTV Static-Interface | `PS_STATIK` | D.7.8 | — |
| Edit Exchange Map | `PS_EXCHANGE_MAP` | D.7.2 | — |
| Elevations | `PS_KOTE` | C.13 | — |
| Endplates | `PS_ENDPLATE` | B.16 | `conn kind=endplate` ⭐ template, not numbers |
| Face UCS | `PS_FACE_UCS` | B.3.2 | `view` |
| Face View | `PS_FACE_VIEW` | B.3.2 | `view` |
| Family Classes | `PS_FAMILY_CLASS` | B.5.5 | ⭐ **`propset FamilyClass=`** — new from E.9 |
| Flatten Viewport | `PS_VIEWPORT_FLATTEN` | ~~C.19~~ → **C.22** | — |
| Free View | `PS_FREEVIEW` | B.3.5 | `view` |
| Global Settings | `PS_GLOBAL_SETTINGS` | A.6 | — |
| Global Scale | `PS_SCALE` | ~~C.9~~ → **C.11** | — |
| Global View | `PS_GLOBAL_VIEW` | B.3.3 | `view` |
| Groundplates | `PS_GROUNDPL` | B.17 | `connbase` ⭐ template, not numbers |
| Groups | `PS_GROUP` | B.27 | `group` `groupauto` `groupedit` `groupinfo` |
| Gusset Plates | `PS_GUSSET_PLATE` | ~~B.22~~ → **B.23** | 🧱 **CEILING** — no creator; the gusset comes from the bracing |
| Hatches | `PS_VOUTE` | B.25 | `haunch` ⚠️ set the plane explicitly or it stretches the rafter |
| Hide | `PS_HIDE` | B.5.1 | — |
| Hide Exclude | `PS_HIDE_EXCLUDE` | B.5.1 | — |
| Hide Exclude Plane | `PS_HIDE_EXCLUDE_PLANE` | B.5.1 | — |
| Hide Group Exclude | `PS_HIDE_GROUP_EXCLUDE` | B.5.1 | — |
| Hide Group | `PS_HIDE_GROUP` | B.5.1 | — |
| Hide Plane | `PS_HIDE_PLANE` | B.5.1 | — |
| Hole Display | `PS_HOLE_DISPLAY_STYLE` | ~~C.17~~ → **C.20** | — |
| Insert Gratings | `PS_PLATE` | B.9.3 | `plate` `polyplate` `arcplate` `platepoly` |
| Insert Plates | `PS_PLATE` | B.9 | `plate` `polyplate` `arcplate` `platepoly` |
| Insert Shapes | `PS_INS_PROF` | B.8 | `beam` `shape` `shapeedit` `sections` |
| Intersection 3D-Volume | `PS_COMMEN` | B.11.7 | `boolean` intersection |
| Language Selection | `PS_LANGUAGE` | A.2 | — |
| Layer Functions | `PS_LAYER` | B.1 | `setlayer` |
| Manual 2D-Cut | `PS_MAN_CUT` | ~~C.11~~ → **C.13** | — |
| Manual 2D-Shortening | `PS_SHORT2D` | C.15 | — |
| Manual Dependency of Part | `PS_MANUAL_LINK` | C.8 | — |
| Manual Dimensioning | `PS_DIM` | ~~C.12~~ → **C.14** | — |
| Measure Distance | `PS_CONST_MSE` | B.2.2 | `vfy_touch` — reports the gap as a number |
| Move and Copy Parts | `PS_COPY` | B.4 | `copy` `replicate` `mirror` `rotate` `spiral` `align` `clonemodel` |
| Notch | `PS_NOTCH` | B.11.6 | `cope` ⭐ from a template; support is MANDATORY |
| Object View | `PS_OBJ_VIEW` | B.3.1 | `view` |
| Object-UCS | `PS_OBJ_UCS` | B.3.1 | `view` |
| Outlet | `PS_OUTLET` | B.11.1 | ❌ `outlet` — applyRc=0 every route tried (E.9) |
| Perspective View | `PS_PERSP` | B.3.7 | `view` |
| Plate Editor | `PS_PLATE_EDITOR` | ~~B.12~~ → **B.13** | `platepoly` `setpoly` |
| Positioning | `PS_POS` | ~~B.28~~ → **B.29** | `posnum` `posauto` `posset` · **on the cmd allowlist** |
| Process Partlist | `PS_CREATE_PARTLIST` | B.30.2 | — |
| Project Management | `PS_PROJECT` | A.5 | — |
| Process Status | `PS_PROCESS_STATUS` | B.5.6 | ⭐ **`propset ProcessStatus=`** — new from E.9 |
| Purlin Connection | `PS_PURLIN_CONN` | ~~B.21~~ → **B.22** | `purlin` `purlintype` |
| Regenerate | `PS_REGEN` | B.5.2 | **on the cmd allowlist** |
| RevisionCenter | `PS_REVISIONCENTER` | ~~D.6~~ → **C.6** | — |
| Roof and Wall Covering | `PS_ROWADISPATCH` | D.3 | — |
| Rounding off Plate Edge | `PS_BEND_EDGE` | B.12.2 | `bend` `bendtwo` `bendinfo` |
| Search Parts | `PS_SEARCH` | B.5.7 | — |
| Seize Partslist | `PS_PARTLIST` | B.30.1 | — |
| Shear Plates | `PS_SCHEARPLATE` | ~~B.19~~ → **B.20** | `shearplate` `shearplatetemplates` |
| Simulate Movement | `PS_KINEMATIK` | D.5.4 | — |
| Special Parts | `PS_CREATE_SPEZPART` | D.1.1 | — |
| Special Shapes | `PS_CREATE_SOPRO` | D.1.2 | — |
| Static Effective Lines | `PS_ANALYSIS` | D.6 | — |
| Stiffeners | `PS_RIP` | B.15 | `stiffener` `connstiff` `stifftemplates` |
| Structural Element Joist | `PS_JOIST` | ~~E.7~~ → **E.8** | — |
| Structural Element Truss Girder | `PS_TRUSS` | ~~E.4~~ → **E.5** | — |
| Structural Element Handrail | `PS_HANDRAIL` | ~~E.2~~ → **E.3** | — |
| Structural Element Hangar Frame | `PS_FRAME` | ~~E.3~~ → **E.4** | — |
| Structural Element Purlin Position | `PS_PFETTE` | E.5 | — |
| Structural Element Ladder | `PS_LADDER` | ~~E.6~~ → **E.7** | — |
| Structural Element Stairs | `PS_STAIRS` | E.1 | — |
| Subtract 3D-Volume | `PS_SUB` | B.11.7 | `boolean mode=sub` |
| Subtract Intersection 3D-Volume | `PS_COMMEN_SUB` | B.11.7 | `boolean` |
| Top View | `PS_TOPVIEW` | B.3.4 | `top` |
| Tubes Unwind | `PS_UNWIND` | D.5.3 | — |
| Visibility Classes | `PS_HIDE_CLASS` | B.5.3 | `classify set=display` |
| Webangle | `PS_STEGW` | ~~B.18~~ → **B.19** | `webangle` `webangletemplates` |
| Weldmarks | `PS_WELD` | C.14 | — |
| Workframe Views | `PS_SETBKS` | B.7 | `view` `front` `top` `left` `right` `iso` `ne` `nw` `se` `sw` |
| Workframes | `PS_WORKFRAME` | B.6 | `workframe` `grid` `frame` `frameinfo` |

---

## What was built for E.10 — strip `x 57,000 – 113,000`

E.10 is a reference chapter; there is no geometry in it to reproduce. What there *is* is a
claim worth testing: two of its commands became reachable this morning and had never had a
code route before.

**`PS_FAMILY_CLASS`** (Family Classes) and **`PS_PROCESS_STATUS`** (Process Status) are now
`propset FamilyClass=` and `propset ProcessStatus=`, off E.9's write surface.

So the strip carries a small portal — two HE300B columns, an IPE400 girder, **real bolted
end-plate joints at both ends** from `example/example3` — whose members are then classified
**by role**: the columns as family 1, the girder as family 2, all three at process status 1.

Measured:

```
🧲 vfy_bolts   12 bolts · 24 holes · 12 matched · BOLT-NO-HOLE=0 · HOLE-NO-BOLT=0
   propset     4/4 fields stuck on each of the three members, writeTo.rc=0
   classify    f1=2  f2=1  f-1=32       <- read back through a DIFFERENT op
```

That last line is the point. The assignment was written with `propset` and confirmed with
`classify`, which reaches the value by its own route — two columns carrying family 1, one
girder carrying family 2, and the 32 parts of the E.9 strip still unassigned at -1.

---

## 📐 The strip convention (introduced 10/08 at Amir's request)

*"שכל המידולים של כל שיעור יהיו בסטריפ מוגדר ונראה הפרדה בין שיעור לשיעור."*

**One lesson, one strip, and the separation is visible in the model itself.**

| lesson | strip (x) | boundary object |
|---|---|---|
| E.9 Properties Dialogs | −3 000 → 53 000 | `Ks_Grid` "E.09-PROPERTIES-DIALOGS" |
| E.10 Command Reference | 57 000 → 113 000 | `Ks_Grid` "E.10-COMMAND-REFERENCE" |
| E.11 Own Notes | 117 000 → 173 000 | `Ks_Grid` "E.11-OWN-NOTES" |

* **Pitch 60 000 mm: a 56 000 strip and a 4 000 gap.** The gap is the separator — you can see
  where one lesson ends without reading anything.
* Each strip is bounded by a **named `Ks_Grid`**, so the boundary carries the lesson's name.
* All boundaries live on layer **`_STRIPS`**, so they can be switched off in one click.
* ⚠️ `grid`'s `lsteps` / `wsteps` are **bay spacings, not counts** — `lsteps=1` builds a bay
  one millimetre wide. And **the grid's LENGTH runs along world Y, its WIDTH along world X**;
  both measured, both the opposite of what the names suggest.
