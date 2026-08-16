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
