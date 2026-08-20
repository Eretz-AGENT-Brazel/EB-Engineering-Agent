# Survey files (קבצי מדידה) — reading the real space before designing steel into it

Built from **lesson 8 with Amir, 20/08/2026** — a lesson with no modelling in it at all. The file
read: `10660-15512.dwg`, an interior survey of Tara's cheese hall, taken 13/05/2026 and delivered
01/06/2026; then the gallery that was designed into it
(`CBC TARA GELLERY 3D MODEL FOR FAB 26.07.26`, model 5 of the company-model series).

Every number below was read out of those two drawings. Nothing was written, nothing was saved.

---

## 1. Why the file exists — Amir's framing

> *"כאשר אנחנו ניגשים לביצוע פרויקט, אנחנו לא תמיד מייצרים פרויקט מאפס — כלומר אנחנו צריכים להיכנס
> לחלל מסוים, או לנכנס מסוים ושם לבצע את העבודה. לכן אנחנו משתמשים בקבצי מדידה. אנחנו הולכים לשטח,
> ולוקחים את כל המידות, הגבהים וכל מה שנחוץ עבורנו על מנת להתחשב בכל האילוצים של הפרויקט."*

**His worked example, and it is the whole point:** the client hands over a drawing from his archive
saying the concrete floor was poured at **3.50 m**. The team goes to site and measures **the TILING,
not the concrete** — and reads **3.546 m**. That 46 mm is construction deviation, and it is real.
A structure designed on the archive drawing arrives on site 46 mm wrong.

⇒ **The survey is not a nice-to-have reference. It is the difference between a design that fits and
one that does not.**

### The division of labour Amir set, in his words
> *"אני רוצה שבאחריות הממדל האנושי (אנחנו העובדים בחברה) תהיה להמיר את תוכן קובץ המדידה לאלמנטים
> SOLID ולבדוק התנגשויות ואילוצים. אבל תוספת שלך וסיוע בדבר הוא בוסט שיכול לתרום המון."*

| | |
|---|---|
| **the human modeller** | converts the survey content into SOLIDs, and runs the clash/constraint check |
| **the agent** | reads, measures, cross-checks, surfaces contradictions and missing data — **a boost, not a substitute.** Do not take that step over. |

---

## 2. 🛑 THE UNIT RULE — and why the file will never warn you

> **A survey file is ALWAYS in METRES. Steel is in millimetres. Scale ×1000.**
> Amir: *"קבצי מדידה תמיד משורטטים על פי יחידות של מטר… וזה עניין קריטי."*

Measured on the delivered drawing:

```
INSUNITS    = 0      <- "unitless". The file does not declare metres.
MEASUREMENT = 1      LUNITS = 2      LUPREC = 3
```

⇒ **Nothing in the drawing knows its own units**, so nothing — not `INSERT`, not `XREF`, not a
paste — will ever flag a 1000× error. The habit is the only guard.

### ⚠️ ×1000 alone is the wrong move — the local grid bites first
The survey sat on a **local grid**, benchmark `P1` at exactly **(20000, 10000)** with `ELEV 0.00`
(plus `P2`, `P3`, `P30`–`P33` as stations). Those are metres. Scale first and the job lands
**20,000,000 mm — 20 km — from the origin.**

> **Order: MOVE the benchmark to a local origin FIRST, then SCALE ×1000.**

### ⛔ And never let the toolchain "fix" the units for you
`eb_api.bootstrap()` calls `enforce_metric()`, which **writes `INSUNITS=4` (millimetres) into the
active drawing**. On a survey file that stamps the *wrong* unit into the customer's own file.
Attach with `use()` + a plain op (`ping`, `list`) instead. On this lesson the plugin channel was
already live and needed no netload at all.

---

## 3. ⭐⭐ The shape of the thing: a flat plan + a coded 3-D point cloud

This is the single fact that turns "chaos" into something readable.

| half of the file | what it carries |
|---|---|
| **the linework** — walls, concrete, crane rail, ducts | **every polyline flattened onto ONE plane.** Here `Z = 106.490` for all of it. X and Y only. |
| **the points** — blocks with attributes | each block sits at its **true Z**, and its `ELEV` attribute **equals that Z exactly**. Here 0.00 → 6.94 |

> **The plan says WHERE. The points say HOW HIGH. Nothing in the geometry joins them — the join is
> the CODE.**

That is exactly why the office workflow is *take the elements that matter, EXTRUDE each to its own
measured height, and place it*: the file cannot do it for you, and no amount of 3-D orbiting will
make a height appear.

### 🧲 The first question is never "what does it look like" — it is "what is 0.00 here"
Amir: **"אנחנו מגדירים זה כל פרויקט ואת ה-0.00 שלו."**
⇒ **The datum is an Eretz Barzel decision, taken per project, and it must be stated in one line
before anything is extruded.** The file's own zero is only the surveyor's benchmark. This survey's
zero is the floor of the room, not sea level — but that is a fact about this file, not a rule.

---

## 4. The code system, as this surveyor writes it

Each measured point is a block with `NAME` / `ELEV` / `CODE`, inserted on a layer named after its
code. The point symbols are `PO0` / `PO3` / `POPB`; feature blocks carry more.

| what was found | reading | measured elevations |
|---|---|---|
| `DRZPA`, `Code B`, `16L`, `ENH` | floor points | −0.01 … +0.07 |
| `KIR` / `KIR-L`, `BAIT` / `BAIT-L` | wall and building contour; the point description reads **`ר.ק.`** (רום קרקע) | 0.07 / 0.52–0.54 |
| `BETON-L` (dashed) | concrete — **two identical rectangles 1.014 × 0.348 m**, 8.649 m apart, each with a full vertical stack of points above it | at floor level |
| `Code T` / `TV` / `TH` | ducts. `--T.HASHMAL` = **תעלת חשמל**, drawn as a pair of lines **0.284 m apart** ⇒ tray width | T 2.14–2.41 · **TH 3.18–3.19 AND 4.10–4.13** |
| `Code HAG` / `HAG2` / `HAGV` | the crane. **Amir: 4.40 = תחתית מנוף · 4.62 = הנקודה העליונה** | 4.39–4.63 |
| `Code A` / `A6` / `F`, `POPB` | structure overhead | A 2.36–4.18 · A6 6.48–6.57 · F 6.94 |
| `Code AM`, `S1`–`S4`, `S33` | dense clusters on the crane columns — **not identified, and Amir does not need them** | 0.70–4.47 |
| `Code 20L` | a vertical run at one XY | 0.81 → 1.37 → 1.69 → 6.55 |
| `NSH` | **שוחה** — a manhole block with `TL` / `IL` / `TYPE=D400` / `DEPTH` | cover −0.03; **`IL = −100.00` is a PLACEHOLDER, not an invert** |
| `--MSILAT-MANOF` | **מסילת מנוף** — each rail drawn as a pair of lines **0.230 m apart** ⇒ beam width ~230 mm | rails 7.87 m apart, centre to centre |

⭐ **The surveyor pinned photo numbers into the plan** — texts `תמונה 9` and `תמונה 14` sit at real
XY coordinates, one at each crane rail, keying the drawing to the photo folder delivered beside it.
**Worth asking every surveyor for; it is the cheapest possible link between the file and the site.**

### 🔤 The Hebrew is stored in the SHX font's own encoding, and it is reversed
Text comes back as bytes `0x80`–`0x9A`. Decode: `chr(0x05D0 + b − 0x80)` over the 27 letters
(א…ת including finals, in Unicode order), **then reverse the whole string**, keeping digit runs in
their own order. That turns `\x8c\x8e™‡ \x9a\x8c’\x9a` into **תעלת חשמל**. Without it the labels —
which are half the meaning of the file — are invisible.
⚠️ **And the surveyor's Hebrew is not reliable.** Amir: *"מי שמייצר לנו את קובץ המדידה לא דובר עברית
כל כך טוב, אז הוא טועה לפעמים."* A label that decodes to a non-word (`מילוא`) is a spelling slip, not
a term to look up — **ask, or read the photo he keyed to it.**

---

## 5. ⛔ Seven traps, all measured on one delivered file

1. **All linework on one Z plane** (106.490 here). Extrude it where it lies and the result is 106 m
   in the air. Flatten deliberately, to the datum you declared.
2. **One code, two heights.** `Code TH` held **3.19 and 4.12 m** on the same layer — one duct in
   plan, two ducts 0.9 m apart in space. A plan read alone merges them.
3. **The same measured point twice.** Point 56 exists at `Z = 4.630` *and* at `Z = 106.490` with
   `ELEV = 4.62`.
4. **Real points parked on `Defpoints`** — a non-plotting layer, *and* frozen. Five here. They do
   not print and they are not on screen.
5. **Half the drawing on layer `0`** — 44 of 68 polylines and all 12 texts. Layer filtering will not
   find it.
6. **Texts at an absurd Z** (1414.986 here). A 3-D window selection grabs or misses them at random.
7. **`POELEV` / `PONAME` / `POCODE` frozen** ⇒ the plan *looks* as if it carries no heights at all
   until they are thawed.

⇒ **None of these is malice or incompetence — it is what a survey deliverable looks like.** Budget
for the clean-up as part of the job, and do it on a copy.

---

## 6. 🛑 The mistake I made reading it: a bbox gives the size, never the diagonal

I read the crane rails' plan direction off a **bounding box** and reported **62.7°**. The real vertex
order says **117.30°** — the *other* diagonal. With it, the crane span went from a wrong 5.12 m to a
measured **7.87 m** between rail centrelines.

⭐ **The tell was present before the correction and I did not act on it:** at 62.7° the rails sat
35° off the building's axis, which explains nothing. At 117.30° they are **89.64° — perpendicular**,
and every other number falls into place.

> **When an angle you derived explains nothing, suspect the derivation.** A two-point line has two
> diagonals in its bounding box and the box cannot say which one it is. Read the vertex order.

Same family as this skill's standing rule *"measure the span, never copy the dump's angle"*.

---

## 7. ⭐⭐⭐ Bringing the survey into the steel model — the step that leaves no trace in either file

The finished gallery carries the survey **inside it**, on a layer named **`MEDIDA`** (the surveyor's
own word — his project path reads `…_MEDIDA PNIMIT 13-05-26`). Measured on the block reference:

| | |
|---|---|
| scale | **1.000** — the ×1000 was applied to the block's **content**, not to the insertion |
| rotation | **62.0993°** |
| Z shift | **−102,273.5 mm** |

And the rotation is the whole craft:

```
building axis    27.66° + 62.0993° =  89.76°   ≈  the model's Y axis
crane rails     117.30° + 62.0993° = 179.40°   ≈  the model's X axis
```

> ⇒ **The survey was turned until the BUILDING's own axes became the world axes.** That is why every
> member of the steel model afterwards lands on a round number.

**A real building is not aligned to the WCS.** Align the survey to the building once, at the start,
and the entire model downstream becomes orthogonal — or skip it and fight a 62° skew in every
dimension for the rest of the project.

The Z shift was chosen on the same logic: the plan plane (106.490 m) lands at **+4216.5 mm**, which
leaves the raw point cloud parked ~102 m below the model — still present, still available, and out
of the way.

**What the walls became:** one `AcDb3dSolid` on layer `WALL`, `x −627…5570 · y 515…12180 ·
z −94…8607` — a room **6.20 × 11.66 m, 8.70 m high** — plus three solids at `z −94…446`, the
**kerb/skirting** along the walls that the site photographs show as a raised strip beside the
hexagonal tiled floor. That is the EXTRUDE step, in the finished model.

---

## 8. ⭐ What a survey actually buys the steel — read as numbers, on this job

The gallery's entire scheme falls out of **two measured elevations**:

```
8607  roof / wall head
7856  top of the A/C units                 ┐  751 mm spare under the roof
6607  DECK  (marug 4+1 trays)              ┘
                                           ┐  1,986 mm of clearance
4620  top of the crane        (Code HAGV)  ┘
4400  underside of the crane  (Code HAG)   ← below this is the crane's world
4120 / 3190  the two cable trays (Code TH)
2240  existing structure — where the access starts
   0  floor
```

- The deck is **6606.5 mm**, plan `x −44…2736 · y 821…11912` ⇒ a strip **2.78 × 11.09 m**.
- The units are **1223 × 4087 × 1400 mm** each, base 6456/6460.
- Handrail: 19 posts, **1117–1151 mm** above the deck, both long edges.
- Of the **40 steel parts that cross the crane's height band**, every one sits at `x −400…900` or
  `x 2400…2700` — **pushed to the two edges, the middle left clear.**
- The column is **split at z ≈ 3175 into 3254 + 3250 mm** — a transport/erection splice, and the
  reason 19 parts cluster at that exact height.
- **Access starts at +2.24 m, not from the floor** — landing on structure the survey shows already
  exists there (`Code T`, 2.14–2.41).

⇒ 🧲 **Two numbers off a point cloud decided the level of the whole structure.**

### ⚠️ And the thing a survey can never give you
**A crane is a SWEPT VOLUME, not an obstacle.** The measured position is where the bridge happened
to stand that morning. Its travel, its bridge depth and the hoist hanging below it come from the
crane's own data — **a question for Amir, never a reading.** The same holds for anything that moves:
doors, curtains (`וילון` is labelled here), trolleys, pallet routes.

Two more constraints that are not steel and still bind: **an exit sign must stay visible** (two are
labelled in this file, on brackets off the crane runway beam), and **a temporary curtain may or may
not be staying** — ask.

---

## 9. Working discipline on a customer's survey file

1. **Read only.** Do not save, do not model in it, do not write system variables into it.
2. **Fingerprint it at the start and at the end** — `os.path.getsize` + `getmtime` — and report both.
   On this lesson: `1,573,844 bytes · 01/06/2026 13:57:32`, identical before and after.
3. **Lock onto it explicitly** — `worksession.py assign "<dwg>" --task "…"` then `verify`, and read
   the `MATCH` line back. Anything else in the same AutoCAD then reads `not registered` and the agent
   refuses to touch it or even to switch the view away from it.
4. **The delivery is a folder, not a file.** This one carried the eTransmit report (which named the
   surveyor's own project path and told us it is an AutoCAD **Map** drawing converted back to
   AutoCAD 2000), the SHX fonts, the plot styles, a ZIP of the whole thing, and **33 photographs and
   9 videos from the day of the survey**. **Read the report and look at the photographs** — the file
   answers *where* and *how high*; only the photographs answer *what is that thing*.

---

## 10. A second survey, read the same day — Carlsberg K10 (20/08/2026)

Amir handed over a live project folder: the surveyor's `10010-15672.dwg`, the EB model
`19.08.2026 Carlsberg K10.dwg` that the survey was embedded into, and a photo set. The job is a
steel structure to carry an **automated pallet conveyor** at the Carlsberg plant, line 10.
**Read only — nothing was modelled.** What the second file confirms, and what it adds:

### ✅ The unit rule held again, unprompted
`INSUNITS = 0` on this survey too — **two surveys, two different offices, both silent about being
in metres.** Local grid again (~20000/10000), 32.5 × 48 m surveyed.
⚠️ **81 polylines and 3 texts on `Defpoints`** here — the non-plotting-layer trap, second file running.

### ⭐⭐ THE SURVEY AND THE SOLIDS TRACED FROM IT ARE ONE LOCKED PAIR
```
MEDIDA        ins = (-1789.829, 3597.688, 0)   scale 1.000   rot = 353.82203°
WALLS SOLID   ins = (-1789.829, 3597.688, 0)   scale 1.000   rot = 353.82203°
```
**Identical insertion point, identical scale, identical rotation.** The extruded walls and the survey
they were traced from are inserted as a pair, so **they physically cannot drift apart** — move one and
you move both. That is the convention to copy, and it is what makes the solids trustworthy months later.
⭐ **And the rotation is per project, not a constant:** Tara **+62.0993°**, Carlsberg **−6.178°**. The
*method* generalises (turn the survey until the building's axes are the world's); the *number* never does.

### ⭐ THE CLIENT'S STATED DIMENSION SHOULD BE FINDABLE AS A MEASURED OBJECT
Amir quoted the customer's numbers — **conveyor 550 mm, pallet 180 mm** — before anything was opened.
In the model the conveyor solids read **3750 × 1582 × 550, z 3550 → 4100**: the 550 is there, exact, as
a modelled body. ⇒ 🧲 **Go and find the number they told you inside the model. If it is not there as a
measured object, either the model or the brief has moved.** The stack that fell out:
```
5900  top of pallet load     CRATES 1200 × 1000 × 1800  (the 180 pallet is inside this, not separate)
4100  top of conveyor        19 supplier blocks, ALL at z=4100, at 3090 centres
3550  underside of conveyor  <- 4100 - 550, the customer's figure
3462  top of our 6 mm plate      (an 88 mm gap to the conveyor -- a question, not an assumption)
3456  top of our steel = top of the existing 300 mm slab (3156..3456)
  16  top of base plates      10 off, 16 mm; columns 3020; beams 200 then 220
```
⭐ Supplier content travels in the model: one block is named **`SL_TUR_ROL1000_R03`** — the conveyor
maker's own part. **Look for the vendor's blocks before asking for an interface drawing.**

### 🗂️ HOW THE EXISTING SITUATION IS ORGANISED IN AN EB PROJECT MODEL
Existing plant steel gets **its own named layers and is modelled as real members** — here
`OLD SMALL BRIDGE` (136 members, 24.9 × 3.62 m, z 5700→6680, its deck one 24932 × 2532 × 80 element
on 43 cross members at 597 centres, **skewed in plan**) and `OLD BIG BRIDGE` (38 members, z 5280→6700).
Everything that is *not* steel is a solid on its own layer: `WALLS SOLID`, `DOOR`, `CONVEYORS`,
`SL_Conveyors`, `CRATES`, plus `MEDIDA` for the survey itself. **Our new steel stays on `PS_*`.**
⇒ the clash question becomes answerable by layer, and the model reads at a glance.

### 🛑 AND THE TRAP I ALMOST FELL INTO AGAIN, IN A NEW COSTUME
Testing the pallets against the existing bridge **by LAYER BOUNDING BOX** said all three crates clash.
That is not a clash test — a layer's bbox is the envelope of 136 members. Re-run **per member**, the
answer survived but the reason changed: the crates intersect **one specific element, `223F`** (the
bridge deck, underside 5700) by **646 mm in plan and 200 mm in height**, over the full 24.9 m.
⇒ 🧲 **A clash is a statement about two BODIES. Any test that compares groups is a screening pass, and
its output is a candidate list — never a finding.** Same family as *"a bbox gives the size, never the
diagonal"* (§6) and as the collision-vs-contour lesson in the main skill file.
⚠️ **And it was reported to Amir as a question, not a verdict** — the 1800 crate may be a placeholder,
and he is the authority on what the plant actually runs.
