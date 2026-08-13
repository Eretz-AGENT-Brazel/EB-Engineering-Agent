# 🔩 BOLT STYLES × HOLE DIAMETERS — the measured matrix, and EB's default

*Opened 13/08/2026, lesson 6. **Authority file for: which bolt style to use, and which hole
diameter each style will actually accept.** Everything here was produced by building specimens
in the smoke-test strip and reading the created bolt back out of the model — never from the
style's name, never from the manual.*

> ## ⭐⭐⭐ THE DEFAULT, SET BY AMIR — 13/08/2026
> > *"אנחנו עובדים לרוב עם ברגים 8.8. אני צריך שזו תהיה ברירת המחדל שלך."*
>
> **The default bolt style is `8.8S`.** Not `DIN6914` (which is what the Python helpers shipped
> with, and which is a **10.9** HV bolt), not `DIN7990`, not `(default)` — `8.8S`.
> It is also the style his own lesson-6 bolts came out as: `M20 x 60 8.8/S`.

---

## The matrix — 36 specimens, one variable at a time (13/08/2026)

Two 10 mm plates face to face, six holes drilled at six diameters (`dia=d play=0`, so the hole
IS `d`), then **one `boltparts` call per style**. The cell shows the bolt ProSteel chose, read
back out of the model:

| style | ⌀18 | ⌀19 | ⌀22 | ⌀23 | ⌀26 | ⌀27 |
|---|---|---|---|---|---|---|
| **`8.8S`** | **M16 x 50 8.8/S** | ⛔ | **M20 x 55 8.8/S** | ⛔ | **M24 x 60 8.8/S** | M24 x 60 8.8/S |
| `8.8TB` | M16 x 50 8.8/TB | ⛔ | M20 x 55 8.8/TB | ⛔ | M24 x 60 8.8/TB | M24 x 60 8.8/TB |
| `8.8TF` | M16 x 50 8.8/TF | ⛔ | M20 x 55 8.8/TF | ⛔ | M24 x 60 8.8/TF | M24 x 60 8.8/TF |
| `DIN7990` | M 16x45 | ⛔ | M 20x50 | **M 22x50** | M 24x55 | M 24x55 |
| `DIN931` | M 16x45 | **M 18x55** | ⛔ | ⛔ | ⛔ | ⛔ |
| `DIN933` | M 16x45 | **M 18x45** | M 20x45 | ⛔ | M 24x50 | M 24x50 |

**How to read it:**
- ⭐ **All three 8.8 styles are one table** (`Australia.mdb@AS_Bolt_88s`) and behave identically.
  They pair on **+2 mm clearance**: 18→M16 · 22→M20 · 26→M24.
- ⛔ **They refuse ⌀19 and ⌀23 outright** — nothing is created and the API returns
  `create=False`. ProSteel says why on its own channel (see below).
- ⭐ **`DIN7990` is the only style that accepts ⌀23 — and it hands back M22, not M20.**
- ⭐ **`DIN933`/`DIN931` accept ⌀19 — and hand back M18, not M16.**
- ⚠️ **⌀27 → M24 in every style that has M24**, i.e. +3 is accepted at M24 while +3 at M16/M20
  is not. The table's rows are not a uniform rule; **look the pair up, never extrapolate it.**

### 🛑 The consequence, stated plainly
**Amir's shop rule — hole = bolt + 3 (M16→19, M20→23) — cannot be produced from code together
with a grade-8.8 bolt.** ⌀19 and ⌀23 are refused by the 8.8 table, and the styles that do accept
them return the next bolt size up. This is not an opinion about the practice; it is what the
installed tables contain.

**Three ways out, and it is Amir's call which:**
| # | what | cost |
|---|---|---|
| **1** *(recommended)* | keep **8.8**, drill at **+2** (`play=2`): 18 / 22 / 26 | the hole is 1 mm tighter than the shop habit. It is also EN 1090-2's normal clearance |
| 2 | keep **+3** holes, accept **`DIN7990`** | bolt goes up a size (M22 for a ⌀23 hole) and the grade is **not in the part-list name** |
| 3 | keep **+3** holes, place the bolt **manually** (`CreateSingleBolt`: start, end, dia, style) | bypasses the table, so M20 8.8/S in a ⌀23 hole is buildable — but it is the manual route with the silent-failure history (B.15) |

### ⭐ What `/S`, `/TB`, `/TF` mean — they are not variants of the same bolt
AS/NZS 1252 **tightening categories**: `8.8/S` snug-tightened · `8.8/TB` fully tensioned, bearing ·
`8.8/TF` fully tensioned, friction. Same bolt, different installation requirement — so the
suffix is a **specification for the shop**, not a modelling detail. Amir's models use **`/S`**.

---

## How a refusal announces itself

`boltparts` returns `create=False created=0 boltCount=0` and the plugin's canned hint blames
*"Gap distance / Angle difference"* — **which is wrong in this case and has misled this project
twice.** The real message is on ProSteel's own channel (`eb_log`):

```
* WARNING  CANNOT DETERMINE BOLTS  => NO BOLTS.
```

⇒ ⭐⭐ **Bracket every bolting attempt with `eb_log.mark()` / `problems()`.** The API's return
value cannot distinguish "no row in the table" from "your geometry is wrong", and the log can.

## The other two things measured on the way

- ⭐ **`8.8S` was refused on a 23 mm hole while `DIN7990` bolted the very same holes** — same
  geometry, same call, one variable. So a refusal is about the **style's table**, not the model.
- 🛑 **The dialog can do what the API cannot.** Amir's own `PS_BOLT` run put **M20 x 60 8.8/S**
  through those same ⌀23 holes. From code that pairing is unreachable. Another entry in the
  family of *"the manual describes the dialog, not the API"*.

## Related

[[SECTION-CATALOGUES]] — the same discipline for section keys: look the string up, never build it.
`THE-CEILING-what-code-cannot-reach.md` · `LETHAL-CALLS-do-not-invoke.md`.
Full lesson record: `../../../lessons/LESSON-06-BEAM-CONNECTIONS.md`.
