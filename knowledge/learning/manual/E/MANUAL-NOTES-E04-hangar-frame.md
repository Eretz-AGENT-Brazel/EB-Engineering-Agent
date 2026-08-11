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

# 🛑🛑 THE FIRST FRAME WAS SLOPPY. WHAT AMIR SAW, AND WHAT IT COST TO FIND.

*11/08/2026. The frame below is the **second** one. The first was committed, looked right in the
viewport, and reported `vfy_fit bolts=196 OK=196 BOLT-NO-HOLE=0`. Amir opened it and asked one
question: is this a sensible connection?*

```
1312  IPE500   holes=0
1314  IPE500   holes=0
```

> ### ⛔ BOTH RAFTERS CARRIED ZERO HOLES.
> All 12 bolts joined a **plate to a column**, or a **plate to a plate** at the apex. The plates
> merely *touched* the rafter ends. **It was not a frame** — two columns with plates bolted on, and
> two rafters resting nearby. In the shop it comes apart on the first lift.

And the rest of it was thin in every direction:
* **no haunch** at a 20 m portal knee — the API refused and I let that decide the engineering
  instead of saying out loud that the software cannot build the correct detail
* **no stiffeners** — 4 bolts pulling on a bare `HE400B` flange with nothing opposite
* **4 bolts at ±180 on a 500 plate** — arbitrary; a moment knee clusters them at the tension flange
* **30 mm edge distance** for a ⌀23 hole on a 200-wide plate

## ⚠️ AND `vfy_fit` COULD NOT SEE ANY OF IT

`vfy_fit` verifies each **bolt** against the parts it is **linked to**. It is structurally blind to
a member that was never in the joint at all. **A green check stood in for looking at the thing.**
Worse: `collision` *was* shouting — 24 hits — and I filed them as a checker artefact.

> ### ⭐⭐⭐ THE RULE THAT CAME OUT OF IT — `app/vfy_joined.py`
> Ask the question **from the member's side**: *what joins this part to anything?* Every member must
> be **bolted**, or **declared welded**, part by part. A welded joint is legitimate — B.23 proved
> welds are not creatable from code — but **declaring it is the price. Silence is what produced
> this frame.** Enforced by `qc/consistency.py` check 9; the backlog lives in `qc/joints-legacy.tsv`
> and may only shrink.

---

# STEP 2 — WHAT IS BUILT

```
span 20000 across outer steel edge · eaves 6000 on the rafter line · roof 10° · ridge z 7728.0
columns  HE400B rot=90, axes x 300200 / 319800, to z 6450 so they back the full end plate
rafters  IPE500 rot=90, from the end-plate faces (300430 / 319570) to the ridge
base     600×600×30, 4 × ⌀33 holding-down holes each (anchors are cast in, not modelled)

THE KNEE — a real haunched moment connection
  end plate   1115 × 300 × 30, EXTENDED: from above the rafter's top flange down past the
              haunch's bottom flange. 300 wide gives 60 mm edge distance to a ⌀26 hole.
  haunch      web triangle 2147 × 466 × 10 + bottom flange FL 200×16, 500 deep at the column,
              running 2000 along the rafter to die into the rafter's own bottom flange
  stiffeners  4 plates 352 × 142 × 15 inside each column, in line with the rafter top flange and
              the haunch bottom flange, one either side of the web — the load path v1 omitted
  bolts       10 × M24 DIN7990 per knee, two lines at ±90, five rows CLUSTERED AT THE TOP
  apex        two 700 × 300 × 30 plates perpendicular to the rafters, 8 × M24 through both
```

**22 members · 28 bolts.**

## ⭐ THE DRILL DIRECTION RULE, FINALLY STATED PROPERLY

The first build got the right knee wrong, the rebuild got the *left* knee wrong the same way. It is
not a left/right rule at all:

> ### ⭐⭐ THE DRILL DIRECTION MUST POINT **FROM THE COLUMN TOWARD THE PLATE**.
> ```
> left  column inner face 300400, plate 300400…300430  ->  n = +x
> right column inner face 319600, plate 319570…319600  ->  n = −x
> ```
> Travelling the other way the drill runs **through** the section and registers on the **far
> flange** — measured twice, at `300000→300024` and `320000→319976`, each ~400 mm from the plate.
> Holes cannot be removed, so both times the column and plate were **deleted and rebuilt**.

⚠️ `beam` has **no `flat=` key** — its keys are `|ax|ay|catalog|kind|layer|mirror|name|offx|offy|
p1|p2|rot|`. A flat bar comes from `DIN_FLACH` by name (`200X16`).

## THE JOINT AUDIT — `app/vfy_joined.py 298000 358000`

```
22 members, 28 bolts
  columns 151B / 1529      holes=10  bolted
  end plates 151C / 152A   holes=10  bolted
  apex plates 14FD / 14FE  holes=8   bolted
  base plates 14D9 / 14DC  holes=4   bolted
  rafters 14D8 / 14DB      holes=0   WELDED (declared)
  haunch webs, haunch flanges, 8 stiffeners   holes=0   WELDED (declared)

JOINT AUDIT CLEAN -- every member is bolted or declared welded.
```

⚠️ **The welds are declared, not built.** B.23: welds are not creatable from code. Every welded part
was checked for **contact** rather than assumed — rafter→end plate, haunch web→end plate, haunch
flange→end plate all measure a **0.00 mm gap**. ⇒ **This frame is shop-ready in geometry and needs
its welds specified by a person.** Said plainly, because not saying it is what produced v1.

| verification | result |
|---|---|
| `vfy_fit` model-wide | **`bolts=212 OK=212 BOLT-NO-HOLE=0 GAP-IN-PACKET=0 OVERSIZED=0 SHORT=0`** |
| **JOINT AUDIT** | **CLEAN** — 22 members, none floating |
| `collision` — E.1 / E.2 / E.3 / E.03 | **0, 0, 0, 0** — untouched |
| `collision` — E.04 band | **25**, the bolt-in-hole class — see below |
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
