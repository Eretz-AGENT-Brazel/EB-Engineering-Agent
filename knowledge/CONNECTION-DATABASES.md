# Connection databases — B.17.3's company-connection route

*Surveyed 10/08/2026 during the part-B audit. Read through `op=dbase file=<path>`, which drives `PsDBaseDatabase`.*

> The manual's own HINT: *"create a database with frequently utilized and maybe **company-specific connections**, which are then always available to all program users within your company."*

⭐⭐ **The route to encoding Eretz Barzel's own connection standard.** These are plain dBASE files — the manual notes they can be edited with *"any standard dBASE editor"* — and `PsDBaseDatabase` exposes `PutRecord` and `AppendNewRecord` as well as reads.

⭐ **And they speak B.14's language.** `BasePlate.dbf` stores `HOLEX = "2*100"` and `HOLEY = "1*"` — the same drill-field layout syntax `drillfield x= y=` takes. The connection databases and the drilling API are one vocabulary.

Root: `C:\Program Files\Bentley\ProStructures Ss6 R1\AutoCAD 2015\Prg\Plugins`

| macro | file | records | fields |
|---|---|---:|---|
| `AECChute` | `AECChute.dbf` | 59 | `SHAPE`, `LENGTH`, `WIDTH`, `THICKNESS`, `OFFSETX`, `OFFSETY`, `BOLTTYPE`, `DIAMETER`, `WORKLOOSE`, `ANGLE`, `SHAPETYPE`, `SHAPESIZE`, `LOWERONLY`, `PITCH1`, `PITCH2`, `BRACEBYANG`, `DISTANCE`, `UPPEREND`, `LOWEREND` |
| `BasePlate` | `BasePlate.dbf` | 56 | `SHAPE`, `CODE`, `LENGTH`, `WIDTH`, `THICKNESS`, `DIAMETER`, `WORKLOOSE`, `HOLEX`, `HOLEY`, `AF`, `AS` |
| `BasePlateChinese` | `BasePlate.dbf` | 56 | `SHAPE`, `CODE`, `LENGTH`, `WIDTH`, `THICKNESS`, `DIAMETER`, `WORKLOOSE`, `HOLEX`, `HOLEY`, `AF`, `AS` |
| `BeamBeamClamp` | `BeamBeamClamp.dbf` | 5 | `DIAMETER`, `OFFSET` |
| `PipeStrap` | `PipeStrap.dbf` | 33 | `STANDARD`, `SIZE`, `LENGTH`, `WIDTH`, `DIAMETER`, `THREAD_LEN`, `THREAD_DIA`, `SHAPE_TYPE`, `SHAPE_SIZE` |
| `PurlinBeamBraceFly` | `PurlinBeamBraceFly.dbf` | 60 | `SHAPE`, `LENGTH`, `WIDTH`, `THICKNESS`, `OFFSETX`, `OFFSETY`, `BOLTTYPE`, `DIAMETER`, `WORKLOOSE`, `ANGLE`, `SHAPETYPE`, `SHAPESIZE`, `LOWERONLY`, `PITCH1`, `PITCH2`, `BRACEBYANG`, `DISTANCE`, `UPPEREND`, `LOWEREND` |

⚠️ **Read only for now.** These files live in `Program Files` and define how connections get built; `op=dbase` deliberately does not write. Adding EB's own connections is a decision for Amir, not a side effect of an audit.

---

## ⭐ STEP 1 — read and understand (Amir, 10/08/2026: *"שלב 1 — רק תקרא ותבין"*)

Steps 2 and 3 — building an EB table, and writing anything into the software — are **explicitly
not to be done**. This section is the understanding, and nothing else.

### What a connection standard looks like when it is written down

**`BasePlate.dbf` — 56 rows, and every single one is `UB`** (Australian Universal Beam), 28
distinct sections from `150UB14.0` to `610UB125`. **Zero HEA / HEB / IPE.**

| field | distinct values | what it is |
|---|---|---|
| `SHAPE` | 28 | the column section the row applies to |
| `CODE` | 56 | ⭐ the **designation to quote** — `BBP 15 150UB14.0` |
| `LENGTH` / `WIDTH` | 15 / 6 | the plate, 180–? × 160…250 |
| `THICKNESS` | **3** — 6, 8, 10 | |
| `DIAMETER` | **3** — 16, 20, 24 | |
| `WORKLOOSE` | **1** — 2 mm | hole play, one value for the whole standard |
| `HOLEX` | **1** — `2*100` | ⭐ B.14's layout string |
| `HOLEY` | 2 — `1*`, `2*100` | so every row is a **2-bolt or 4-bolt** pattern |
| `AF` / `AS` | 2 each — 5, 8 | flange / web weld throat |

⇒ ⭐⭐ **The lesson is the SHAPE of it, not the numbers.** A written connection standard is:
**section → a handful of dimensions + a hole-layout string + two weld sizes**, and the value sets
are **deliberately tiny** — three thicknesses, three diameters, one play, one hole pattern. That
narrowness is the point: it is what makes a standard buildable and purchasable, not a menu.

### The one table that is directly EB-relevant

**`PurlinBeamBraceFly.dbf` — 60 rows, 47 of them `Z` sections.** The fly-brace table: for a given
Z-purlin, which angle to use and how to fix it.

```
SHAPE   Z…(47) · DHS…(12)     SHAPETYPE  AS_EA_ANGLE | DIN WINKEL GLEICH
SHAPESIZE  50x6EA · L60x6 · L75x8        BOLTTYPE  4.6s | DIN7990
ANGLE / PITCH1 / PITCH2  0 or 45         DISTANCE  0, 300, 450, 600, 900, 1200
```

⇒ **Z-purlins are exactly what B.8 unlocked** (`SCHRAG_z-pfetten`, `Sadef_zed`, `sbe_z`,
`ayrsh_zeta`). The purlin catalogue and the fly-brace table are two halves of one job.

### ⚠️ What this does NOT mean

The shipped tables are **another company's standard, for another country's sections**. They are
worth reading as a **form**, not as content. And per Amir, Eretz Barzel has **no written standard
at all** — see the memory `no-written-standard`. So there is nothing here to translate; if a table
is ever wanted it has to be **extracted from Amir and from the models already built**, which is a
different and larger job.

**Frozen here on instruction. Do not build a table. Do not write to the software.**
