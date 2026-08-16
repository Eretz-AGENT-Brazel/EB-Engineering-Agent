# Hinged assemblies — doors, lids and access chambers (lesson 7, 16/08/2026)

Built from Amir's own production model `LESSON 7- DIKE MODEL.dwg`: **שוחות למיכלים** — access
chambers for tanks, each with a two-leaf hinged lid and a padlock point. Every number below was
read back out of that model, and the whole chamber was then rebuilt from scratch and compared
part by part (worst geometry delta **0.239 mm** over 56 parts).

> **Amir's framing:** *"נדרשנו לייצר את השוחה עצמה ובנוסף אליה לייצר דלתות פתיחה עם צירים
> ומקום המיועד לנעילה עם מנעול. אז מה עשינו? מידלנו את השוחה עצמה בהתאם למידות שהיה צריך,
> ואת הדלת הראינו ב-2 מצבים — פתוח וסגור."*

---

## 🧲 THE THREE CONVENTIONS THAT MAKE THIS DETAIL TYPE WORK

### 1. The hinge pin is a PURCHASE MARKER, not a fabricated part

> *"את הציר עצמו מידלנו בפרופיל 16 ROUND. זה סימן בשבילנו שאנחנו צריכים להזמין ציר בקוטר
> 16 מ"מ מהספק — זה לא משהו שאנחנו מייצרים, אז זה רק סימבולי. זהו בעצם הפרופיל שעל גביו
> מתבצעת הפתיחה והסגירה."*

- Modelled as **`16ROUND`** (catalogue `AUSTRALIA.AS_ROUNDBARS`), **L = 100**, one per hinge,
  **4 per chamber** (2 per leaf).
- ⭐ **It carries no holes and nothing is drilled for it.** In the whole 116-entity model the
  **only** holes are the two padlock bores. A hinge pin that is bought, not made, does not need
  the iron rule applied to it — but that is **Amir's declaration for this part type**, exactly
  like a self-drilling screw, and never an assumption the agent may extend.
- ⌀16 pin running inside ⌀21.3 handle pipe elsewhere in the model = 5.3 mm diametral clearance;
  the pin itself sits **centred in the door's turned-down eave lip** (pin z 499 inside a lip
  spanning 487…511).

### 2. The lifting handles are HALF-INCH PIPE

> *"הידיות הרמה של דלתות השוחה — מידלנו אותם עם צינור חצי צול."*

- **`21.3x2.0CHS`** (`AUSTRALIA.AS_CHS_C350`) — ½″ nominal bore, OD 21.3.
- One handle = **2 legs (L 85.595) + 1 crossbar (L 171.3)**. The legs stand **perpendicular to
  the sloping leaf**, the bar runs **parallel to it**. **2 handles per leaf, 4 per chamber.**
- ⭐ **All three junctions are MITRED**, not straight-cut: leg `cutPlanes=1`, bar `cutPlanes=2`.
  (`cutat mode=straight` shortens the member — 85.6 → 64.4 — and is the wrong tool here.)

### 3. Show the door in BOTH states, and get the angle from the pin

⭐⭐⭐ **The same chamber is modelled twice — once closed, once open** — and the open leaves are
the closed leaves **rotated about the pin axis**, not redrawn.

```
west leaf  +100.000°   about the pin at (x −3762.212, z 499.003)
east leaf  −100.000°   about the pin at (x −2215.596, z 499.003)
radius error between the two states: 0.0002 mm
```

- **The rotation is exactly 100°, both leaves, mirror-symmetric.** Verified by taking the eave
  lip's free edge in the closed state and in the open state and measuring the radius about the
  pin: identical to 0.0002 mm, arc exactly 100.000°.
- ⭐ **Why past 90° and not at it:** at 100° the leaf's centre of gravity has crossed the hinge
  to the outboard side, so **the door rests open under its own weight** and the operator is not
  holding it. Anything at or under 90° falls shut.
- ⭐ **Everything mounted on a leaf rotates with it** — handles and the ridge cap included.
  Confirmed: rotating the closed cap's fold line +100° about the west pin lands on the open
  chamber's cap to **0.001 mm**.

---

## 📐 THE CHAMBER ITSELF — how the box is made

Measured on the closed chamber (outer **1515 × 1021 × 600**, plate **4 mm** throughout):

| part | what it is |
|---|---|
| 2 long walls | **1507 long, 6-POINT GABLE contour** — eave **511.645**, apex **600** |
| 2 end walls | plain rectangles **1013 × 511** (east) and **1013 × 511.645** (west) |
| frame | **`EA40X40X4`** angle right round the OUTSIDE at z 215.5…255.5, **mitred at all four corners** |
| lid | 2 bent-plate leaves + 1 folded ridge cap |
| hinges | 4 × ⌀16 round, 100 long |
| handles | 4 × ½″ pipe staples |
| lock | hasp + keeper, one ⌀25 bore through both |

### ⭐⭐ THE WALL IS A CONTOUR, AND ITS NAME LIES

`props` reports the long wall as **`name='PLATE 1507x600x4'`**. **It is not a 1507 × 600
rectangle.** It is a six-vertex gable:

```
local (x, y):  (−753.5, 211.645) → (0, 300) → (753.5, 211.645) → (753.5, −300) → (−753.5, −300)
```

⇒ **The plate NAME is generated from the bounding rectangle.** Building from the name produces a
wall that stands 88 mm proud of the roof line all along the eaves. **Read `dumpmodel` field 6 (the
local contour) or `plateinfo probe=poly`, never the name.**

### The pitch comes out of the gable, and the cap doubles it

```
rise 600 − 511.645 = 88.355   over   753.5      ⇒  roof pitch  6.6875°
ridge cap fold angle                            ⇒  13.376° = 2 × 6.6875°  exactly
```

- **The eave lip is bent 83.312° from the leaf**, i.e. `90 − 6.6875`, **so it hangs plumb** while
  the two side lips are bent square to the plate at 90°. Three lips, two different angles, and
  the odd one is the one you can see from outside.
- ⚠️ **The end-wall height 511.645 is not a typo** — it is the eave height, and it matches the
  gable's own shoulder exactly. (It was "normalised" to 511 during the rebuild and that was the
  error, not the 0.645.)

### The leaf, as a bent plate

```
folded footprint   779.1 (up the slope) × 1032 (along the ridge)
three 20 mm lips   r = 3   angles −83.312 / −90 / −90   (east leaf; west is mirrored, +ve)
developed blank    reported L × W = 800.552 × 1075.142
```

⭐⭐ **`L × W` on a `Ks_BendPlate` is the DEVELOPED BLANK — the cutting size**, not the folded
footprint. Two doors can be geometrically identical in the model and still order different plate.
⚠️ Building the base plate at the footprint and adding flanges outward reproduces the folded shape
to 0.002 mm but yields a **different blank** (1034 × 779 against 1075 × 801). **The folded shape
being right does not prove the cut size is right — check both.**

### The lock

- **Keeper** `80 × 50 × 10` on the box, **hasp** `155 × 125 × 10` on the door, both **shaped, not
  rectangular** — rounded corners carried as **bulge ±0.414** in the contour (a quarter arc).
- **One ⌀25 bore through both**, and the two holes **meet exactly**: the end of one is the start
  of the other, to 0.001 mm. That is what a padlock shackle needs and what proves the plies touch.
- Modelled **only on the closed chambers** — the lock means nothing in the open state.

---

## ⚠️ WHAT THE SOFTWARE DOES HERE, MEASURED

| finding | detail |
|---|---|
| **`miter` keeps the nominal length; `cutat mode=straight` does not** | mitred pipe stays `L=85.595`; straight-cut drops to 64.391. Both report `cutPlanes=1` |
| **One `miter` call cuts BOTH members** | 4 corner calls give every frame angle `cutPlanes=2` |
| ⭐⭐ **An angle's envelope and axis are IDENTICAL for all four rotations** | `ext` and `p1` cannot see a wrong-facing L. **Only the section frame `X`/`Y` can.** Match those to the original, do not guess `rot` |
| **`offx`/`offy` are inert on `beam` for an equal angle** | `ins` stayed `0,0` in four variants; the envelope is centred on the axis either way |
| **ProSteel re-centres a plate's contour** | an off-centre local origin in the source cannot be reproduced — the part lands identically in the world, but the local point list differs |
| ⛔ **`bend` has no parameter for `UseInnerRadius`** | Amir's caps carry `innerR=True`; ours cannot. Sweeping the fold radius 1→7 mm bottoms out at **0.229 mm** and cannot compensate. **v185 item** |
| **`VolumeWeightFlag` chooses the weight METHOD** | his parts `True`, code-built parts `False` — same steel, `2.540` vs `2.202` kg, and it **reaches the parts list**. Sync it |
| ⛔⛔ **`Mirrored` is NOT an inert flag** | blanket-syncing properties wrote it and **displaced 12 pipes by up to 340 mm**. `rw` means "has a setter", never "safe to set" |
| **`propfull` writes to a FILE** | the reply is a one-line receipt; diffing replies compares nothing |

---

## The rebuild, and what it proved

| group | parts | worst delta |
|---|---:|---|
| frame angles, pins, wall plates | 20 | **0.0000 mm** |
| door leaves, ridge caps | 6 | 0.002 mm (0.229 on the two caps — the `innerR` gap) |
| handle pipes | 24 | 0.055 mm |
| lock plates + bore | 2 | 0.023 mm, contours identical vertex-for-vertex |

⇒ **A production detail of this kind is reproducible from code to a few hundredths of a
millimetre** — provided each part is rebuilt from its **contour + modifications + insertion
frame**, and not from the summary line.

---

# Second example — the PUMP CHAMBER (lesson 7 continued, 16/08/2026)

`lesson 7- Pump Chamber.dwg`: **תא למשאבות** — a pump pit that also needed an opening lid.
Two chambers side by side, **one closed and one open**, exactly the state-pair convention above.
148 entities; 140 of them real parts. Rebuilt from an emptied drawing:
**82 shapes at ≤0.059 mm · 44 plates at ≤0.001 mm · 8 concrete solids at 0.0000 · all 26 cut
counts matching.** The six bent plates did not come right — see the open item at the end.

## What it adds to the convention set

| | DIKE chambers | Pump chamber |
|---|---|---|
| hinge pin | `16ROUND`, 4 per chamber | **`20ROUND`, 6 per chamber** = 3 hinges per lid, two lids |
| open angle | **100.000°** | **110°** |
| handles | ½″ pipe, bar 171.3 | ½″ pipe, bar **141.3** |
| lid | bent-plate leaves + ridge cap | `SHS60×60×4` frame + 2 mm skins + **16 louvre blades** |
| below | nothing | **8 concrete solids on `PC_Concrete`** |

⭐⭐ **Both models open the lid PAST 90°** — 100° and 110°. That is not decoration: past
vertical the leaf's centre of gravity crosses the hinge and **the lid rests open on its own**,
so nobody holds it while working in the pit. **Treat "past 90°" as the EB default for a hinged
lid, and read the exact angle out of the frames rather than assuming.**

⭐ **The concrete is information, not steel.** Amir: *"הקורות SOLID שממודלות בצבע תכלת הינם
יציקת בטון שמתבצעת בשטח עבור התא, ולכן מידלנו אותם על מנת להראות את המימדים של היציקה."*
Four `2400×120×300` beams and four `125×1080×150` pads on layer **`PC_Concrete`** — the site
pour's length and width, so the steel can be checked against what will actually be cast.
Exclude them from weights and part lists, exactly like the lesson-5 exam's slabs.

⭐ **The louvre blades are at exactly 20.000°** — 16 per chamber, 3 mm, a 10-point shaped
contour, one repeating part. That is the repetition principle in a ventilated lid: one cutting
program, thirty-two pieces.

## ⭐⭐⭐ HOW TO REPRODUCE A PART FROM ANOTHER MODEL — the method, corrected

This example paid for five mechanical rules that the first one did not reach:

1. **`dumpmodel`'s `p1→p2` can run OPPOSITE to the member's own +Z.** 42 of 82 shapes needed
   their endpoints swapped. Build once, compare the achieved `Z` with the target `Z`, and if
   they are opposed rebuild with the ends exchanged — *then* solve the rotation.
2. ⭐⭐ **The printed axes are rounded to 3 decimals — in `props` AND in `propfull`.** Feeding
   them straight back gives an error proportional to the part's length: on a 2361 mm plate a
   0.0003 tilt error is **0.103 mm**. Two cures, both measured: recognise a round angle
   (`0.342/0.94` is exactly 20° — substituting `sin20`/`cos20` took 16 blades from 0.035 to
   **0.0000**), or secant-solve the tilt against the source's extents (took two skins from
   0.103 to **0.0000**).
3. ⛔ **Never normalise the axis triad component-wise.** Normalising X and Z while leaving Y as
   printed makes the triad non-orthogonal, ProSteel re-derives its own frame, and the part lands
   **2.347 mm** out. Orthonormalise as a set, or pass what was printed.
4. ⭐⭐ **Correct the insertion point relative to the `at` you PASSED, never to the resulting
   bbox midpoint.** For a contour ProSteel re-centres, the two differ by the re-centring offset,
   and feeding the midpoint back injects that offset on every iteration.
5. ⚠️ **`solid kind=box` places from a corner, not the centre** (out by half the dimension).
   Build → measure → correct converges in one pass.

## ⛔ Open item — bent plates, and the honest number

`bend` can only fold **at an edge of the base plate**, so reproducing a source bent plate means
recovering the base width, and that is **not** `W − flangeLength`: the reported `L×W` is the
DEVELOPED blank and the bend deduction sits between them. Results:

```
C43  0.010 mm     D02  0.010 mm  (built by MIRRORING C43 -- see below)
D18  1.628 mm     DFD  4.366 mm     DFE 13.005 mm     E1B 25.943 mm
```

⭐⭐ **What did work, and is the rule to carry:** `D02` was not fitted at all — it was produced
by **mirroring the one plate that was already right**, and landed at **0.010 mm** with its fold
vertex exact. Hours of parameter search gave 5 mm; one mirror gave 0.010.
⇒ **When a part repeats, place it — do not re-derive it.** Build one correctly, then mirror /
rotate / align the siblings. That is Amir's own build-once-then-replicate, and here it was also
the *accurate* route, not merely the fast one.
🔜 The three remaining (`DFE`, `E1B`, `DFD`) are chamber A's plates translated `(35.5, 3630, 0)`
and rotated **110°** about the pin; placing them that way is the unfinished work.
