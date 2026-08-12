# The models in this folder — what is in each

*Classified 09/08/2026. Part B of the manual was learned and practised across **two** drawings;
this file says which is which so neither is opened by guesswork.*

---

## `B08-insert-shapes.dwg` — **the part-B practice model** ⭐

**835 parts.** Every chapter of part B that was implemented lives here, each in its own **band**,
laid out left to right along +X. One band per chapter, so any lesson can be found by its x range.

| x | what | chapter |
|---:|---|---|
| 0 – 15 000 | insert shapes | B.8 |
| 32 000 – 52 000 | work frames | B.6 |
| 70 000+ | insert plates | B.9 |
| 90 000+ | stiffeners / ribs | B.16 |
| 110 000+ | web angles | B.19 |
| 130 000+ | shear plates | B.20 |
| 150 000+ | splice joints | B.21 |
| 170 000+ | gusset plates, SHS/UPN node | B.23 |
| 190 000 – 196 000 | dynamic bracing + two frames | B.24 |
| 210 000+ | bolts | B.15 |
| 230 000 – 240 000 | 3D modifications — cope, divide, mitre, boolean | B.12 |
| 250 000 – 260 000 | purlin connections, four kinds | B.22 |
| 270 000 – 276 000 | move & copy — two spiral distributions, align | B.4 |
| **290 000 – 292 000** | **the spiral staircase** — floor to floor, 3000 mm | (Amir's task) |
| 310 000 – 316 000 | static bracing, a full braced bay | B.25 |
| **330 000 – 337 000** | **portal frame, WELDED** | B.26 |
| **346 000 – 353 000** | **portal frame, BOLTED** | B.26 |
| 368 000 – 373 000 | solids — 8 primitives incl. rect2circle, conic pipe | B.10 |
| 379 000 – 381 000 | ACIS body reference | B.11 |
| 390 000+ | construction lines and the measure check | B.2 |

**Area classes 1–10** were assigned to the ten most recent bands (B.5.4's *construction sections*),
so they can also be selected and sorted by class rather than by coordinate.

⚠️ **This is the agent's working model.** It is saved after every step and is safe to open.

---

## `E-structural-elements.dwg` — **the part-E practice model** ⭐ *(new 10/08/2026)*

Opened for part E on Amir's instruction: *"תפתח מודל חדש שבו תמדל את כל תרגילי היישום שאתה צריך
לפרק E."*

### 📐 One lesson, one STRIP — and the separation is visible in the model

Amir, 10/08: *"שכל המידולים של כל שיעור יהיו בסטריפ מוגדר ונראה הפרדה בין שיעור לשיעור."*

| lesson | strip (x) | boundary |
|---|---|---|
| **E.9** Properties Dialogs | −3 000 → 53 000 | `Ks_Grid` "E.09-PROPERTIES-DIALOGS" |
| **E.10** Command Reference | 57 000 → 113 000 | `Ks_Grid` "E.10-COMMAND-REFERENCE" |
| **E.11** Own Notes | 117 000 → 173 000 | `Ks_Grid` "E.11-OWN-NOTES" |

**Pitch 60 000 mm — a 56 000 strip and a 4 000 gap.** The gap *is* the separator: you can see
where one lesson ends without reading anything. Each strip is bounded by a **named grid**, so
the boundary carries the lesson's name, and all of them sit on layer **`_STRIPS`** so they can
be switched off in one click.

⚠️ Two things about `grid` that are the opposite of what the names suggest, both measured:
`lsteps` / `wsteps` are **bay spacings, not counts** (`lsteps=1` builds a bay one millimetre
wide), and the grid's **LENGTH runs along world Y, its WIDTH along world X**.

### What is inside each strip

**E.9 — the properties laboratory.** Specimen A is an intact bolted joint; the column is
**green with a centre line** because `propset` wrote `ColorIndex=3` and `CenterLineMode=1`, which
is the visual proof that the write surface reaches AutoCAD. Specimen B is the same joint after
a section change — the test that found **two bolts destroyed** — killed and rebuilt. Plus a
modifications rig (the bare hole there is **deliberate**), the `propcopy` pair, and the failed
notch attempts.

**E.10 — the classification rig.** A small portal with real end-plate joints at both ends,
whose members are classified by role: columns family 1, girder family 2. It demonstrates the
two commands E.9's write surface newly reached, `PS_FAMILY_CLASS` and `PS_PROCESS_STATUS`.

⭐ **Every specimen is labelled through E.9's own Data tab** — `Note1` says what the part is
for, `Note2` what was measured on it. The model documents itself; read a part's properties and
it tells you why it exists.

### 📐 Baseline — 10/08/2026

**79 parts** · 24 bolts · 49 holes · 24 matched · **0 iron-rule violations** · 0 oversize.
One unfilled hole, and it is the deliberate one, labelled as such on the part.

---

## `sandbox.dwg` — **Amir's own drawing** 🛑

**Amir's file.** Part B work from **07/08** is in here.

> 🛑 **Do not save, modify or close it.** Amir: *"זה קובץ שאני עובד עליו — אל תסגור אותו."*
> Read it if needed; never write to it.

---

## `_archive/` — snapshots, kept not deleted

15 timestamped `.dwg` snapshots from 06–07/08, plus the `BROKEN` / `rescue` / `crashtest` files
from the recovery on 06/08. **Nothing here was deleted** — it was moved out of the working folder
on Amir's instruction so the folder reads cleanly, and it is excluded from the repo.

---

## The other lesson folders

`projects/lesson-1 … שיעור-5` hold the graded lessons from before part B began, plus their
baselines and exam drawings. They are history, not working files.
