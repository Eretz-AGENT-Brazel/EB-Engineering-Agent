# E.4 Structural Element — Hangar Frame

*Read 11/08/2026, pages 1094–1099 (fulltext lines 28334–28446) — **a short chapter, 112 lines**,
against E.3's 736. Band `E.04-HANGAR-FRAME`, grid `125E` at x = 300 000. Plugin v178.*

> *"This function generates a hangar frame from **two support members and two crossbars**, which can
> be connected to one another in different ways. This requires that you click on the outer edge
> insertion point of the left and right vertical frame members."*

⭐ Same live-child pattern as E.3's polyline: *"The dimensions of the frame can be changed at a later
time by modifying the **(yellow) object frame** using its grips."*

---

# THE DIALOG — five tabs

### Dimensions
| field | the manual's words |
|---|---|
| `Width` | *"the width of the hangar frame **across outer steel edge**"* — same convention as E.1's stair width |
| `Ridge Width` | only for an **asymmetrical** frame |
| ⭐ `Centre Height` | *"the ridge height of the crossbars. **Changing the value will affect the pitch of the roof**"* |
| ⭐ `Left`/`Right Roof Angle` | *"the roof pitch… **Changing the value will affect the ridge height**"* |
| `Left`/`Right Eave Height` | eaves height of the crossbars |
| ⭐ `Top Side Base` | *"the distance of the supports from floor level (the pick points of the supports) **required to add base plates**"* |
| `Left`/`Right Column Offset` | *"the projection of the support upper edge **beyond the height of the eaves**"* |
| ⭐ `Box Frames` | *"a frame is generated **only with a crossbeam**. In this case, the specifications for the ridge height are unimportant"* |
| `Symmetrical` · `Dynamic` · `Draw Diagonal` | — |

⭐⭐ **`Centre Height` and `Roof Angle` are two views of one number** — the manual says each changes
the other. There is no independent ridge height.

### Shapes
`Which Shape` · `Shape Type` · `Shape Class` · `Shape Size` · `Alignment`.
⚠️ *"at 'Symmetrical' setting, the **right shapes cannot be set** because the values of the left
shapes are applied to them."*

### ⭐⭐ Left and Right Knee — `Attachment`
| option | the manual |
|---|---|
| **`Adapt`** | *"The crossbar is **cut to the support**. You can specify a distance…"* |
| **`Angle Cut`** | *"crossbar and support are cut to have a **mitred joint**"*, with a gap |
| **`Haunch`** | *"the connection is designed as a **frame haunch**. You can select a **haunch template**"* |
| **`Free Plate`** | *"designed as a **plate connection**… select a **template**… `Turn` to rotate an unsymmetrical plate"* |
| `Align As is` | *"No connections are made"* |

⇒ ⭐⭐ **The knee tab is B.26's haunch and B.20's plate connection, reached BY TEMPLATE.** Exactly
A.3.2's rule — *choose a template, do not pass numbers* — and both plugin ops already take one:
`op=haunch beam= support= tmpl=` and `op=shearplate handle= support= template=`.

### Apex — `Attachment`
`Angle Cut` / `Free Plate` / `Align As is`.
🛑 **There is NO `Haunch` at the apex.**

### Assignments
Per-shape material / grade / coating, as every part-creating dialog has.

---

# ⭐⭐ THE API CONFIRMS THE MANUAL — independently

`PsPortalFrame`, 27 methods and 49 properties. The German continues E.1's `Steigung`/`Auftritt`:

```
St…  = Stiel   the supports      StLeft/RightKatalog · Key · ShapeType · Offset
Rg…  = Riegel  the crossbars     RgLeft/RightKatalog · Key · ShapeType
Voute = haunch                   LeftVouteTemplate · RightVouteTemplate
OkFoot = Oberkante Fuß           top-of-foundation level
Heigth / Wide                    misspelled, exactly like PsStairs.setHeigth
```

> ### 🛑 THERE IS NO `CenterVouteTemplate`.
> The three junctions each carry a connection code, a free-plate template and a bolt Ø —
> `Left/Center/RightConnection`, `…FreeTemplate`, `…BsplDm` — but the **haunch template exists only
> for left and right.** The manual says no haunch at the apex; **the property surface proves it.**
> Two independent sources, one fact.

⚠️ `Left/Center/RightConnection` are **bare `Int32` with no enum anywhere** — the attachment codes
must be discovered from a frame Amir inserts. Not guessed here.

---

# ⛔ IT DOES NOT BUILD — measured first-hand, in this band

```
structprobe kind=portal  ->  PsPortalFrame.init()+insert() = 0
entities 505 -> 505      (delta 0)
```

E.1 recorded this; it is **re-measured here rather than cited**, because E.1 is also the chapter
that was once closed *on a refusal* with nothing in the model. There is no `PsCreatePortalFrame`
anywhere in the 771-type map — of 27 `PsCreate*` classes, `PsCreateHandrail` is still the only
structural creator. ⇒ **E.4's step 2 is by composition.**

---

# ⭐⭐⭐ `rot=90`, MEASURED UNAMBIGUOUSLY AT LAST

B.26 found the portal columns bending about their **weak** axis and fixed it with `rot=90`. But
B.26 used **HE300B, where h = b = 300**, so the hole depth could not tell the two orientations
apart. E.1 hit the same trap again with `FL300×10`. **HE400B settles it** — h=400, b=300, tw≈13.5:

| | drilled along X | drilled along Y |
|---|---:|---:|
| `rot=0` | **13.5** (the web only) | **400** (both flanges) |
| `rot=90` | **400** (both flanges) | **13.5** (the web only) |

⇒ ⭐ **A portal stands in XZ, so the section's DEPTH must lie along X ⇒ `rot=90`**, on the columns
and the rafters alike. 13.5 is the web; 400 is the full depth; there is no ambiguity left.

---

# ⛔⛔ THE HAUNCH IS CLOSED FROM CODE — and B.26's reason for it is wrong

Four controlled attempts on the left knee, model saved before each:

| | what was set | where the haunch landed |
|---|---|---|
| 1 | template `Default/Standard` | plates at **z −12…+12**; rafter dragged **500 backwards** past the column |
| 2 | `ex=1,0,0 ey=0,0,1` explicit | plane **round-tripped** (`readback X=1,0,0 Y=0,0,1`); plates still at **z 0** |
| 3 | column rebuilt **reversed**, `Origin` at the eaves | plates still at **z 0**, bbox byte-identical |
| 4 | `at=` the knee | `InsertPoint` **round-tripped**; ignored |

**The template's own plane reads back as `X=0,0,0 Y=0,0,0` — a null plane.**

> ### 🛑 B.26 CONCLUDED *"the haunch builds at the SUPPORT'S ORIGIN"*. THAT IS NOT THE RULE.
> Attempt 3 tested it directly: the column was rebuilt with `p1` at the eaves, and its part
> `Origin` read back as **`300200,0,6000`** with `ZAxis 0/0/-1`. **The haunch still built at z = 0.**
> The origin moved; the output did not.
>
> ⇒ What is actually measured: **the parts take X and Y from the member and pin Z to world 0.**
> Plane and InsertPoint both round-trip and are both ignored. This is the scar's own sentence —
> *"a portal frame stands in XZ, not world XY"* — with four variables controlled instead of none.
> **THE CEILING.**

## ⚠️ AND THE BLAST GUARD CANNOT SEE THE BLAST IT WAS WRITTEN FOR

`op=haunch` carries a guard that shouts if the connected member grows. On attempt 1 it printed:

```
beamLen=9951.18->9951.18          <- the op's own reading
rafter measured independently:    9951.2 -> 10451.2   (grew exactly 500)
```

⇒ ⭐ **`LengthOf()` is read too early — before the geometry settles — so a runaway haunch would
pass the guard silently.** The guard needs to re-read, not trust its own snapshot. Found only
because the length was measured a second time from `dumpmodel` instead of believing the op.

---

# ⭐ B.22's SIGN RULE, FOR THE FOURTH TIME

The right knee refused to bolt: *"holes further apart than 'Gap distance'"*. The holes said why:

```
knee plate holes    x 319600 -> 319580     ✓ correct, on the plate
column holes        x 320000 -> 319976     ✗ the OUTER flange, 376 mm away
```

Drilling along `n = +x` from the span side ran **through** the column and landed on the far flange.
⇒ **`n = -1,0,0` on the right.** *The right side is not mirrored for you* — B.22, then B.26, then
E.1's cleats, now here. **Holes cannot be removed**, so the column and plate were **deleted and
rebuilt**, never re-drilled.

---

# STEP 2 — WHAT IS BUILT

```
span 20000 across outer steel edge · eaves 6000 on the rafter line · roof 10° · ridge z 7728.0
columns  HE400B rot=90, axes x 300200 / 319800, to z 6350 so they back the knee plates
rafters  IPE500 rot=90, from the column INNER faces (300400 / 319600) to the ridge
base     600×600×30, 4 × ⌀33 holding-down holes each (anchors are cast in, not modelled)
knee     500×200×20 end plate, 4 × M22 DIN7990 through the column's inner flange   ← `Free Plate`
apex     two 600×200×20 end plates perpendicular to the rafters, 4 × M22           ← `Free Plate`
adapt    both rafters `planecut` at the plate face                                 ← `Adapt`
```

**22 parts · 12 bolts.** `Haunch` was unavailable, so the knees use the chapter's other option —
which is what EB would bolt anyway.

| verification | result |
|---|---|
| `vfy_fit` model-wide | **`bolts=196 OK=196 BOLT-NO-HOLE=0 GAP-IN-PACKET=0 OVERSIZED=0 SHORT=0`** |
| `collision` — E.1 / E.2 / E.3 / E.03 bands | **0, 0, 0, 0** — untouched |
| `collision` — E.04 band | **25**, of which **24 lie on a bolt axis** — see below |
| `collision` — E.9/E.10/E.11 | 24, pre-existing |

---

# 🛑 CORRECTION TO E.3 — BOLTS ARE **NOT** EXEMPT FROM THE COLLISION CHECKER

E.3 concluded: *"Bolts are treated differently — E.1's 128 bolts all sit in drilled holes and
report 0."* **That does not hold here.** Measured at the left knee:

```
column inner flange   x 300376…300400
knee plate            x 300400…300420
bolt M22x75           x 300345…300420        through a drilled ⌀23 hole
collision reported at x 300388               inside that hole
```

**24 of the 25 remaining E.04 collisions lie on a bolt axis — 12 bolts × 2 parts each.** Every bolt
in the frame is flagged, while `vfy_fit` passes all 196 with `BOLT-NO-HOLE=0`.

⇒ ⚠️ **The E.3 sentence is withdrawn as a general rule.** What is certain: the checker honours a
boolean subtraction (`s`) and does not honour a drilled hole (`hf`) — proven in E.2 and E.3 — and
**bolts are not reliably excepted from that.** Why E.1's 128 M12 bolts reported zero and these 12
M22 bolts do not is **unresolved**; the visible differences are bolt size and that these pass
through a **rolled-section flange** rather than plate and angle. **Not explained, not guessed.**

⚠️ **These 24 were deliberately NOT "fixed".** Cutting real material out of a column to satisfy a
checker artefact is what E.2's note warns against: *"a 'fix' applied to a checker artefact destroys
good geometry."* The bolts sit in real, measured, full-depth holes.

---

# Still open
* ⚠️ **Why bolts flag here and not in E.1.** The single most important loose end; it decides whether
  `collision` can be trusted as a bolt check at all.
* ⚠️ **The `op=haunch` blast guard reads the length too early** and must be fixed before anyone
  relies on it.
* ⬜ **The three `…Connection` attachment codes are bare `Int32`** with no enum — they need reading
  off a frame Amir inserts in the dialog.
* ⬜ **`Box Frames`**, the asymmetrical frame (`Ridge Width`), and `Column Offset` — read, not built.
* ⬜ **1 non-bolt collision** remains in the band, unidentified.
* ⚪ ⚠️ **Band-phase conflict, inherited:** `E.01` at 120 000 sits inside the declared `E.11` strip
  (117 000 → 173 000). The E.01/E.02/E.03/E.04 series is a different 60 000 phase from
  E.09/E.10/E.11. E.04 at 300 000 is clear of everything, so it is not blocked — but the two phases
  should be reconciled.
