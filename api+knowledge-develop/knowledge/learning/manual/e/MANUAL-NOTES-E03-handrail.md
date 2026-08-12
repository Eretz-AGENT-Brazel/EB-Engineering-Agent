# E.3 Structural Element — Handrail

*Read 11/08/2026, pages 1066–1093 (fulltext lines 27596–28332) — **27 pages, the largest chapter in
part E**. Plugin v177 → **v178**.*

> *"This function generates a handrail along a previously drawn 3-dimensional polyline. After
> calling the function, you will be prompted to pick this polyline, and the program then constructs
> the handrail along this line."*

⭐ **This is the one structural element in part E that actually builds from code**, and E.1 already
named why: *"an element is creatable from code **only** where a separate `PsCreate*` class exists —
`PsCreateHandrail` is the only one, and it works."* E.3 is where that gets spent.

> ⚠️ **OCR caveat.** The fulltext mangles some UI labels — `"Adap tLast"`, `"Plate Wiidth"`,
> `"Stretch Poportionally"`, `"Handlrails"`, `"Comp.Part Group"`. **Do not treat these as exact
> dialog captions.**

---

# THE DIALOG — ~28 FIELDS ACROSS 14 TABS

*The creator exposes four of them. Everything below is the dialog, and it is the map of what
`PsHandrail`'s ~70 properties are FOR.*

### Generalities
`Layout` — **parametrical construction** *(all settings under 'Dimensions')* vs **user-defined
blocks** *(all settings under 'Blocks')* · `Dynamic` — *"verify the modified settings directly and
immediately on screen. A modification of the polyline has immediate effects, too"* · `Draw Diagonal`
— an auxiliary diagonal *"to facilitate its selection"* · `No Auto-Update` · ⭐ `Group Status` =
**No Group / Component Part Group / Assembly** · `Group Name`, *"Wildcards for the overall
dimensions (`$(L)`, `$(W)`, `$(H)`) are permitted"*.

### Dimensions — ⚠️ **all heights measured PERPENDICULAR to the polyline**
| field | the manual's words |
|---|---|
| `Connection Height` | *"The distance between the drawn polyline and the **beginning of the posts** including possible fastening plate."* |
| ⭐ `Railing Height` | *"The distance of the **upper edge of the newel posts** or the center of the railing head (if in place) measured perpendicular to the polyline."* |
| `Upper`/`Middle`/`Lower Rail Height` | *"The **center** distance of the … knee-high rail measured perpendicular to the polyline."* |
| `Handrail`/`Knee Rail`/`Kick Plate Radius` | ⭐ *"If a radius is specified the corresponding **shape bends** are applied; otherwise a **mitred joint** is cut."* |
| `Thickening Dia.` | *"the thickening radius at the gusset points between posts and e.g. knee-high guardrails."* |

### Post Distance
⭐ `Post Spacing` mode = **`Normal`** *"The segment is regularly divided and the pre-determined post
distance is adapted"* / `Adapt First` / `Adapt Last` / `Adapt First/Last` ·
`Post Distance` — *"the program … spaces out the newel posts in regular intervals and **rounds the
value up or down as needed**"* · ⭐ `Maximum` — *"the values … are **never rounded up**. The post
distance is regarded as the maximum value. If necessary, **additional posts are inserted**."* ·
⭐ `Edge Offset` — *"The spacing of the corner posts between two handrail segments **starting with
the intersection of the polyline segments**."* ·
`Min.Segment Length` — *"the minimum length … from which on posts are created. Thus you avoid e.g.
the creation of posts at the inner edge of intermediate landings at staircases with changing
direction."* · `Min.Segment 2nd Post` — below it *"only one post will be created in the middle"*.

### Offsets
`Posts Inside` · `Keep All Equal` · `Start`/`End Offset` — *"Projection of the corresponding shape
starting from the middle of the first/last rail post towards the outside"* · per-shape overrides.

### ⭐⭐ Segments — **the fabrication-transport lever**
`Create Segments` — *"subdivided into several independent segments, each having their own start, end
and inner posts"* · `Length maximum…` · `Segment Length` · `Gap` — *"the distance from 1 segment to
the other"* · `Distribution` = `Equally` / `Adapt First` / `Adapt Last` · own `Group Status`/`Name`.
⇒ **A length cap plus a gap is how a rail becomes shippable sections.** For a fabricator this tab
is the one with money in it.

### End form — separately for start and end
**without End Offset:** `Leave` / `Up to Outer Edge` / `Mitre Joint` / `Round Off` *(bent part)*.
`Radius` — ⭐ *"either as absolute value or as **many times the amount of the handrail diameter**.
Enter e.g. `*2`."* · **with End Offset:** `Leave` / `Mitre Joint` / `Round Off` ·
`Combine with` — an additional perpendicular part down to a chosen knee rail · `Close`.

### Shapes
⭐ *"the corresponding section is generated **only if checked** appropriately in the check box… if
you would like to generate a handrail with only two knee-high guardrails, then deactivate one of
the three options."* ⭐ ***"All shapes are available for selection."***
`Comp.Part Group` · `Shape Class` · `Shape Size` · `Shape Type` · `Insertion Plane` (Center / Lower
/ Upper Edge) · `Angle` · `Mirror` · **`+`** *"settings of another component part group are copied
into the current selection"*.
⭐ **ALT shortcut** (manual line 901): holding ALT at shape-class selection **takes over the
previous type** — and the same applies to toggling whether a shape is used at all.

### Shapes ▸ Kick plate
⚠️ *"The kick plate will only be created **if you have added kick plates as shapes**."*
`Other Side` · `Side Offset` *"space for individual fastenings or grouts"* · `Height Offset`
measured **from the polyline** to the baseboard's lower edge.

### Shapes ▸ Filler Rods — *vertical infill bars, a whole feature*
*"additional vertical filler rods as safety device against falling through… distributed anew within
two posts."*
`Min. Segment Length` · `Edge Distance` · `Intermediate Distance` *(corrected to fit)* ·
`Distance Top`/`Bottom` · `Insertion Offset` · `Insertion from`/`up to` · `Insertion Position`
(front edge / center / rear edge) · `Cut at` (inner edges / center / outer edges) ·
⭐⭐ **`If Collision`** = **`Ignore`** *"The collision is ignored"* / **`Divide`** *"The filler rod
is divided at the knee-high guardrails"* / **`Perforate`** *"The knee-high guardrails are perforated
so that the filler rod can run through it"*.
⛔ **`Ignore` is the fabrication trap — it leaves interpenetrating solids in the model.**

### Posts
*"You can taper the post on top or bottom by means of cuts…"* `Top horizontal` — ⭐ *"**Two cuts are
made, one at the outside and one at the inside** of the handrail"* · `Top vertical` ·
`Bottom horizontal` · `Bottom vertical`.

### Post ▸ Post Connection
⭐ `Layout` = **`None`** *(no fastening)* / **`Vertical`** *(base plates, perpendicular to the post)*
/ **`Lateral`** *(plates or bent connections welded to the outside)*.
`Outside` *(lateral only)* · ⭐ **`For Diagonals`** — *"other dimensions for diagonal handrail
segments **e.g. at a stringer**. This option is only available for **vertical** connections."*
← **exactly the E.1 stair case** ·
`Plate Layout` *(lateral)* = `Complete Plate` / `Shorten Outer` / `Only Right Side` / `Only Left
Side` ⚠️ *the manual's Right/Left descriptions read inverted relative to their names — quoted as
printed, not verified* · `Projection Side` · `Plate Width`/`Length`/`Thickness` · `Hole Dia` ·
`w (horizontal)` · ⭐ `w (vertical)` — *"You can thus create a plate with four holes. **If you enter
the value 0, only two holes are created**"* ← **the specimen's plates have exactly 2 holes** ·
⭐ **`Zinc Hole Dia`** — *"an additional **zinc outlet hole**"* ← a galvanising drain, generated by
the element · `Side Offset` · `Start Radius` *(bent post)* · `As Poly-plate` · `Turn by 90°`.

### ⭐⭐⭐ Post ▸ Post-Handrail — **this is where the penetration is decided**
`Layout` = `Leave` *"No processing is made"* / ⭐ **`Straight Cut`** *"The post is **shortened up to
the lower edge of the handrail**"* / `Complex Cut` *(two diagonal cuts)* / `With Rod` /
⭐ **`Boolean`** *"The post is **exactly adapted to the handrail by means of a boolean cut**"*.
`Gap Distance` · `Cover Thickness`/`Diameter`.

### ⭐⭐⭐ Post ▸ Post-Knee-high Guardrail — **and this is where MY 15 COLLISIONS CAME FROM**
`Layout` = `Leave` / `Straight Cut` / `Complex Cut` / ⭐ **`Drill`** *"At the position of the
knee-high guardrails, **the posts are drilled so that the shapes can run through them as a whole**"*
/ ⭐ **`Boolean`** *"exactly adapted to the posts by means of a boolean cut"*.
`Gap Distance` · `Hole Diameter`.

### Assignments
Per-shape material / grade / coating / parts-list attributes. The manual does not enumerate them.

---

## The two `Layout` variants

| | parametrical | user-defined blocks |
|---|---|---|
| settings live in | Dimensions, Post Distance, Offsets, Segments, End form, Shapes, Posts, Post connections | Blocks, Blocks▸Post Distribution, ▸Segments, ▸Posts, ▸Fillings |
| geometry from | *"directly created from the default settings in your dialogs"* | *"any user-defined blocks which will be **automatically adapted within certain borders**"* |
| why | — | *"it is possible to create any handrail constructions you like which would be **difficult to seize by means of parameters**"* |

⭐ **Block authoring, the operative rules:** any ProSteel or AutoCAD element may be used, but
*"additional ProSteel parts list information **has to be added** to the AutoCAD-elements"* · **7 post
slots** (start, end, straight intermediate, ±90° corner, ± deviating angle) · **posts and fillings
are defined in WCS**, `+z` = rail height, `+x` = the polyline's running direction; a post's origin
is its insertion point, ⚠️ **a filling's origin is the MIDDLE of the block** ·
`Note 2` codes: **`P`** post · **`C`** connecting elements · **`R`** rails/kick boards (`R,NC` =
do not combine) · **`FB`** elements to be distributed · block attributes `DP`, `DP1`/`DP2`, and
`FBSTART`/`FBEND` accepting **`MIN=100` / `MAX=110`** syntax.
⭐ In the worked example `DP1`/`DP2` sit *"in the drill holes of the upper butt straps"* — **the
drill hole is the registration datum between filling block and post block.**

---

# ⭐⭐ THE POLYLINE IS THE DATUM — everything the chapter says about it

* It must **exist before** the command: *"you will be prompted to pick this polyline."*
* ⭐ The handrail stays a **live child** of it: *"The spatial development of the handrail may still
  be modified at a later time by **changing the polyline using its grips**"*, and with `Dynamic` on
  *"a modification of the polyline has immediate effects, too."*
* **All heights** — connection, railing, all three knee rails, the kick plate — are measured **from
  the polyline, perpendicular to it.** ⇒ **the polyline is neither the handrail centreline nor the
  nosing line. It is the datum.**
* Post spacing is computed **within each polyline segment**, and `Edge Offset` runs **from the
  intersection** of segments.
* ⛔ **Stated limitation, verbatim:** *"However, **only one deviating intermediate angle is
  possible**. You could use corners of 45° beside the right-angled corners for your handrail, but
  **not corners of 45° and 30° at the same time**."*
* The polyline's **running direction** sets post `+X` and the sign of corner angles.

---

# ⭐⭐⭐ THE ARCHITECTURE — THE CREATOR IS ALMOST EMPTY

```
PsCreateHandrail   6 methods    SetToDefaults · SetPolygon · SetConnectionType
                                SetOutside · SetSideOffset · Create()
PsHandrail        ~70 properties  PostSpace · PostShape{Class,Size,Type,View} · PostStatus
                                  RailHeight · RailShape* · Upper/Middle/LowerHeight+Status+Shape
                                  FootStatus · FootShape* · FootDx/Dy/Radius
                                  RailPlate{Thick,Wide,Length,Diameter,…}
                                  StartOffset · EndOffset · EdgeSpace · KneeRadius · EndRadius
                                  EndPostInside · MinPostLength · DiagonalStatus · AddRail(ShapeId)
```

> ### ⭐⭐⭐ THE CREATOR BUILDS FROM DEFAULTS. THE CONFIGURATION LIVES ON THE PRODUCT.
> Every dimension the E.3 dialog offers — post spacing, all four rail courses, the foot plate, the
> connection plates — is a property of **`PsHandrail`, the object `Create()` leaves behind**, not a
> parameter of the creator. **This is a different shape from every `PsCreate*` class met so far**,
> where the parameters sat on the creator (B.20's shear plate, B.25's bracing, E.1's shape).
>
> ⇒ **Without a binder the chapter is only half reachable**: you can make *a* handrail, never *the*
> handrail you wanted. That is what v178 adds.

---

# ⭐⭐ v178 — `bind cls=handrail`, AND WHY IT IS NOT A REOPENED DOOR

A `Ks_HandRail` is **exactly the entity that killed AutoCAD on 10/08/2026**. The `bind` op's own
source records it: the old no-`cls` *"try them all"* path reinterpreted it as a grid/gusset/plate/
shape, and `GetObject` returns **True** for the wrong type and hands back a garbage pointer.

The typed overload exists and the API surface declares it:

```
Boolean GetObject(Int64 Id, PsOpenMode Mode, PsHandrail& entObject)     API-SURFACE-RAW.txt:69882
```

so v178 adds `{ "handrail", "HandRail" }` to the real-class guard and binds through that overload.
**The guard reads `ObjectClass.Name` BEFORE any bind and refuses a mismatch** — verified as a
control, not assumed:

```
bind 3FD cls=plate     -> EB_ERR bind REFUSED handle=3FD is a Ks_HandRail, not a 'plate'
bind 3FD cls=handrail  -> EB_OK  ... the full property set
```

⇒ ⭐ **The lethal call was the WRONG TYPE, not the bind.** This is memory's *"בטיחות לפי טיפוס ולא
לפי פעולה"* — safety by type, not by action — applied literally.

---

# THE SPECIMEN, DECODED — handle `3FD`, 14 parts at x=120 000, y=27 000

```
POSTS   space=1000  status=True  maximal=False  'DIN RUNDROHR/RO48.3x3.6'  minLen=600/500
RAIL    h=1000      status=True  'DIN RUNDROHR/RO48.3x3.6'
UPPER   h=750  on   MIDDLE h=500  on   LOWER h=250  on    all 'DIN RUNDROHR/RO26.9x2.6'
FOOT    OFF         would be 'DIN FLACHEISEN/100x8'
PLATE   t=8 w=60 l=150 dia=13 offW1=100          <- the base plate, and the survey found exactly
                                                    5 plates 60x8 x150 with 2 holes each
GEOM    conn=1 outside=0 edgeSpace=200 diagonal=True dynamic=True
```

## ⭐⭐ COURSE HEIGHTS ARE MEASURED TO THE TUBE'S **UNDERSIDE**, FROM THE POLYLINE

Predicted from the properties, checked against the geometry — **all four match to 0.01 mm**:

| course | height | OD | underside | + OD/2 | measured centre |
|---|---:|---:|---:|---:|---:|
| LOWER | 250 | 26.9 | 1250.00 | 13.450 | **1263.45** ✓ |
| MIDDLE | 500 | 26.9 | 1500.00 | 13.450 | **1513.45** ✓ |
| UPPER | 750 | 26.9 | 1750.00 | 13.450 | **1763.45** ✓ |
| RAIL | 1000 | 48.3 | 2000.00 | 24.150 | **2024.15** ✓ |

⇒ ⭐ **A height is to the underside, not the centre.** Four independent confirmations, two different
tube diameters.

### 🛑 …AND THE MANUAL SAYS *CENTRE*. THE TWO DO NOT AGREE.

> `Upper`/`Middle`/`Lower Rail Height` — *"The **center** distance of the knee-high rail measured
> perpendicular to the polyline."*

Measured, the three knee rails sit with their **underside** at `polyline + h`, not their centre —
`250 → 1263.45`, and `1263.45 − 250 = 1013.45`, not 1000. ⚠️ **The measurement is what the software
did; the sentence is what the manual says. They differ by exactly OD/2 on all three courses.**

⭐ **`Railing Height` is the exception and it matches the manual perfectly:** *"the distance of the
**upper edge of the newel posts**"* — polyline 1000 + 1000 = **2000.000**, and the middle posts stop
at exactly 2000.000. So `RailHeight` is a **post-top** dimension, not a rail dimension, which is
also why the rail head's centre lands at 2000 + OD/2.

## ⭐⭐ `PostSpace` IS A TARGET, NOT THE SPACING

```
polyline 4000 − 2 × EdgeSpace 200 = 3600 usable
3600 divided into the fewest gaps ≤ PostSpace 1000  ->  4 gaps of 900
measured post x: 120200 121100 122000 122900 123800  ->  spacing 900   ✓
```

⇒ **Asking for 1000 gets 900.** The same derivation held on the stair rail: 5259.6 − 400 = 4859.6
over 5 gaps = **971.9**. ⚠️ **Never quote `PostSpace` as the built spacing** — read the posts.

---

# THE THREE PATH SHAPES, MEASURED

| path | result |
|---|---|
| **flat, straight** (the specimen) | 6 posts + 6 base plates + 1 top rail + 3 infill = **14** |
| **sloped** — rake 34.78°, the stair | 6 posts + 6 base plates + 1 top rail + 3 infill = **16** per side |
| **helical** — r 1100, 32 chords | **128 parts: 32 top-rail chords + 96 infill, ZERO posts, ZERO base plates** |

## ⛔⛔ A HELICAL PATH YIELDS RAILS ONLY — AND THE ARITHMETIC SAYS WHY

Every polyline segment is treated as its own run, and `EdgeSpace = 200` is set back from **each
end** of it. A 32-chord helix at r = 1100 gives chords of **233.9 mm**, and `233.9 − 400 < 0`, so
**no post can fit in any segment.** Classified by measurement, not inferred from profile counts:
`VERTICAL members = 0`.

> ### ⭐⭐ ON A SEGMENTED PATH, POSTS SURVIVE ONLY WHERE `chord > 2 × EdgeSpace`.
> Fewer, longer chords buy posts; more, shorter chords buy a smoother curve and lose them.
> **And the chord trap from E.2 applies to the path itself**: the dip is `r·(1 − cos(half the
> segment angle))` — 5.3 mm at 32 chords, 21.2 mm at 16.

⭐ **Here that is not a defect but a fit**: E.2's spiral already carries 16 balusters at exactly
r = 1100, so a rails-only helix is precisely what it needed. The polyline vertices were placed
**on the balusters** (angle `i·22.5 + 4`, r 1100, z = that step's top) so every chord spans post to
post, which is how a segmented rail is actually made.

## ⚠️ THE RAIL HEIGHT ON A RAKED PATH IS **NOT RESOLVED**

`RailHeight = 1000` is honoured **exactly** on a horizontal path (above, to 0.01 mm). On a rake it
is neither a vertical nor a perpendicular offset of 1000:

| path | rake | polyline → rail-axis, vertical | perpendicular |
|---|---:|---:|---:|
| flat specimen | 0° | **1024.15** = 1000 + OD/2, exact | 1024.15 |
| E.1 stair rail | 34.78° | **1034.15** | 849.42 |
| E.2 spiral rail | 22.18° | **965.85** | — |

**Not monotonic in rake**, so it is not a simple projection either way. All seven `Ks_HandRail`
objects in the model were bound and carry **identical** properties (`RailHeight=1000`,
`PostSpace=1000`, `EdgeSpace=200`), so this is a geometric convention and not a property
difference. ⚠️ **Recorded as measured and OPEN. No formula is offered, because none was proven.**

---

# ⭐⭐⭐ PENETRATIONS — THE MANUAL NAMES BOTH THE DETAIL AND THE CURE

The specimen reported **15 collisions**, and they decoded exactly: **5 posts × 3 infill rails**, at
the post x-stations and the rail z-levels. ProSteel's own handrail element **runs its infill
straight through its posts and leaves the penetration uncut.**

B.12, manual p. 204:

> *"You can also subtract one shape from an other to create penetrations, e.g. to obtain slotted
> tubes, **penetrated handrail posts** or others."*

Proven on a throwaway crossing pair before being applied: **1 → 0 collisions, both parts survive.**

```
op=boolean handle=<post> tool=<rail> mode=sub
```

⚠️ **The weight does NOT move** — `wt=3.976 -> 3.976`. The **modification signature** is the only
witness: `s0 -> s1`. Same lesson the plugin's own `DetailCut` comment carries.

## 🛑 CORRECTION — THE ELEMENT CAN CUT ITS OWN PENETRATIONS. I DID IT BY HAND BECAUSE OF A SETTING.

*Written after reading the chapter properly. The first version of this section said "ProSteel's own
handrail element runs its infill through its posts and leaves the penetration uncut", full stop.
**That is true of the DEFAULT and false of the element.*** `Post ▸ Post-Knee-high Guardrail` has:

> `Drill` — *"At the position of the knee-high guardrails, **the posts are drilled so that the
> shapes can run through them as a whole**"*
> `Boolean` — *"exactly adapted to the posts by means of a **boolean cut**"*

⇒ **The specimen was built on `Leave`.** The 15 collisions were a *setting*, not a *limitation*, and
the manual's own remedy is one dialog field. The hand-applied `op=boolean` reached the right
geometry by the long road.

⭐ **And the same field decides `Filler Rods ▸ If Collision`** — `Ignore` / `Divide` / **`Perforate`**.
⛔ *"`Ignore`"* is the fabrication trap: it leaves interpenetrating solids in the model on purpose.

⚠️ **Still open, and now sharper:** all these are `PsHandrail` properties, and **writing a property
is untested**. Whether setting the Layout to `Boolean` on an existing handrail rebuilds it is the
question that would make the hand-pass unnecessary.

## ⭐ AND THE TOP RAIL NEEDS NO CUT — the element gets that junction right

```
top rail centre z 2024.15, RO 48.3  ->  underside exactly 2000.00
the three middle posts stop at exactly 2000.000   (tangent)
the two end posts run to 2048.269 ≈ the rail's top
```

⇒ **Only the infill penetrates.** That is why 15 and not 20 — and the chapter names the setting that
produced it: `Post ▸ Post-Handrail` = ⭐ **`Straight Cut`**, *"the post is shortened up to the lower
edge of the handrail."* **2000.000 exactly is that sentence, measured.** The same specimen therefore
demonstrates both halves at once: `Straight Cut` on the top junction, `Leave` on the knee rails.

## ⭐ ONE PASS IS NOT ENOUGH — LOOP UNTIL DRY

Each post meets **two adjacent top-rail chords at its vertex** plus several infill chords, and
cutting one penetration can expose the next. Pairing is by **measured bbox overlap**, never by
assumption about which chord crosses which post.

```
181 collisions -> one pass, 165 subtractions -> 78 -> second pass, 101 subtractions -> 24
```

The 24 that remain are the older E.9/E.10/E.11 bands and predate this work.

---

# STEP 2 — WHAT IS BUILT

*Three handrails, **92 new parts**, all by `PsCreateHandrail`.*

| where | path | parts |
|---|---|---|
| **E.1 stair, left** | stringer top edge, rake 34.78° | 6 posts + 6 base plates + 1 rail + 3 infill = **16** |
| **E.1 stair, right** | mirrored | **16** |
| **E.2 spiral** | helix, 16 vertices on the balusters | 15 top-rail chords + 45 infill = **60** |

The stair path is the **stringer's top edge**, so the posts stand on the stringer as they would be
detailed:

```
stringer axis (120000,−495,0) -> (124320,−495,3000), FL300x10 rot=90 => 300 mm in the rake plane
unit along  = (0.82135, 0, 0.57038)     unit normal = (−0.57038, 0, 0.82135)
top edge    = axis + 150 × normal = axis + (−85.56, 0, 123.20)
```

⭐ **The rail follows the rake exactly** — rail rise/run = **0.6944** = 3000/4320 — **and the posts
stay plumb**, which is correct detailing and was verified, not assumed.

| verification | result |
|---|---|
| `vfy_fit` model-wide | **`bolts=184 OK=184 BOLT-NO-HOLE=0 GAP-IN-PACKET=0 OVERSIZED=0 SHORT=0`** |
| `collision` — E.1 stair + its rails | **0** |
| `collision` — E.2 spiral + its rail | **0** |
| `collision` — E.3 specimen | **0** |
| `collision` — E.9/E.10/E.11 | 24, pre-existing and untouched |

Inventory read back from the model: **E.1 210 parts · E.2 141 parts · E.3 specimen 14 parts.**

---

---

# ⭐⭐ THE E.03 BAND — CORNERS, MEASURED

*Band `E.03-HANDRAIL`, grid handle `1143` at x = 240 000 — the 60 000 rhythm continued from E.01
(120 000) and E.02 (180 000). **38 parts.***

The stair rails are single-segment paths and exercise none of the chapter's corner behaviour, so the
band carries a flat three-segment path: a straight run, a **90° corner**, then a **45° corner**.

```
posts, read back                              segment
240000.0     0.0                              straight, x 240000 -> 244000
240950.0     0.0                                  spacing 950, and the last post is
241900.0     0.0                                  243800 = 200 SHORT of the corner
242850.0     0.0
243800.0     0.0
244000.0   200.0    <- 200 PAST the corner    90 deg leg, y 0 -> 3000
244000.0  1066.7                                  spacing 866.7
244000.0  1933.3
244000.0  2800.0    <- 200 short of the next corner
244141.4  3141.4    <- 200 ALONG the 45 deg direction: 200/sqrt2 = 141.4      45 deg leg
244801.3  3801.3
245461.1  4461.1
246121.0  5121.0
```

> ### ⭐⭐⭐ THERE IS NO POST AT A CORNER. THERE ARE TWO, ONE `EdgeSpace` EITHER SIDE OF IT.
> Which is the `Edge Offset` sentence, measured: *"the spacing of the corner posts between two
> handrail segments **starting with the intersection of the polyline segments**."* On the 45° leg
> the 200 is measured **along the segment**, giving 141.4 in each of x and y — so it is a true
> along-path offset, not a coordinate one.

⇒ ⭐⭐ **This independently explains the helical result.** Every polyline segment is its own run and
needs `2 × EdgeSpace` before a post can exist. The 32-chord helix offered 233.9 mm against 400 mm of
required set-back, so it produced **zero** posts. Two unrelated geometries, one arithmetic.

⚠️ **AND ONE THING DOES NOT AGREE.** On this multi-segment path the **outer** ends carry a post at
the polyline end **exactly** (240000.0 and 246121.0). On the single-segment specimen and on the
stair, the outer ends were set back by 200 (`120200…123800` on a `120000→124000` path). Both bind
with `EdgeSpace=200`. **Measured, not explained** — recorded rather than smoothed over.

⭐ End posts run to **2048.3** and intermediate posts stop at **2000.0**, reproducing the flat
specimen exactly and confirming `Straight Cut` is the default on `Post ▸ Post-Handrail`.

---

# Still open

* ⚠️ **The rail height convention on a raked path** — measured three times, unexplained. Above.
* ⚠️ **WRITING a `PsHandrail` property is untested.** The bind is read-only. Whether setting
  `PostSpace` or `RailHeight` on an existing handrail rebuilds its geometry is *the* question for
  making this element configurable, and it needs its own guarded path.
* ⬜ **3 orphan `Ks_HandRail` objects** from the path probes — their parts were erased, the parent
  objects were not. Harmless (no geometry, no effect on `vfy_fit` or `collision`) but untidy, and
  there is no safe way yet to tell an orphan from a live one without deleting it to find out.
* ⬜ **The base plates carry 2 holes each and no bolts.** `FootStatus=False` and nothing anchors the
  rail to a floor. The manual's `Bolt…/Handrail` field is the route; not exercised.
* ⬜ **`AddRail(Int64 ShapeId)`** — the element will adopt a shape you supply. Read, never used.
* ⬜ **User-defined blocks** (the second `Layout` variant) — read, not pursued.
* ⚪ **The 14-part specimen is not grouped** — `groupinfo` returns `parts=0 isMain=False name=`.
  `PsCreateHandrail` leaves its output loose; B.28's grouping would have to be applied separately.
