# E.3 Structural Element — Handrail

*Read 11/08/2026, pages 1066–1093 (fulltext lines 27596–28332) — **27 pages, the largest chapter in
part E**. Plugin v177 → **v178**.*

> *"This function generates a handrail along a previously drawn 3-dimensional polyline. After
> calling the function, you will be prompted to pick this polyline, and the program then constructs
> the handrail along this line."*

⭐ **This is the one structural element in part E that actually builds from code**, and E.1 already
named why: *"an element is creatable from code **only** where a separate `PsCreate*` class exists —
`PsCreateHandrail` is the only one, and it works."* E.3 is where that gets spent.

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

## ⭐ AND THE TOP RAIL NEEDS NO CUT — the element gets that junction right

```
top rail centre z 2024.15, RO 48.3  ->  underside exactly 2000.00
the three middle posts stop at exactly 2000.000   (tangent)
the two end posts run to 2048.269 ≈ the rail's top
```

⇒ **Only the infill penetrates.** That is why 15 and not 20, and it is a point in the element's
favour: it terminates posts at the top rail deliberately.

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
