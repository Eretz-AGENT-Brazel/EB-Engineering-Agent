# THE BOLT LENGTH IS NOT A PARAMETER — it is looked up from the grip

*21/08/2026. `bolt len=` was passed on all 5,463 bolts of the bridge rebuild and quietly
ignored: every family came out exactly one step short. The fix came from reading ProSteel's own
bolt database, which is an Access file sitting on disk.*

---

## The symptom

Counts per family were right and every length was wrong:

| source | rebuild |
|---|---|
| 1,996 × `M12 x 50 8.8/S` | 1,996 × `M12 x 45 8.8/S` |
| 191 × `M16 x 55` | 191 × `M16 x 50` |
| 342 × `M20 x 65` | 342 × `M20 x 60` |
| 673 × `M24 x 120` | 673 × `M24 x 110` |

## Where the answer lives

```
Data\Bolts\Australia.mdb  @ AS_Bolt_88s      <- style 8.8S  (yes, the AUSTRALIAN table)
Data\Bolts\DinBolts.mdb   @ SCH912, SCH6914  <- styles DIN912, DIN6914
```

`op=styles` names the file and table for every one of the 27 installed styles. Each row carries
`DM`, `LAENGE` and — the part that matters — **`KLEMMMIN` / `KLEMMMAX`**, the grip band that
selects that length:

```
M12:  30[15-24.75]  35[24.75-29.75]  40[29.75-34.75]  45[34.75-39.75]  50[39.75-44.75]  60[44.75-54.75]
```

⇒ **the grip needed for a wanted length is arithmetic, not a search:**

```
grip = (KLEMMMIN + KLEMMMAX)/2 − DELTA(diameter)
```

`DELTA` is the nut + washer + protruding thread the grip does not include. Measured once per
diameter by building bolts at known grips and reading the designation back:

| | M12 | M14 (DIN912) | M16 | M20 | M24 | M27 (DIN6914) |
|---|---|---|---|---|---|---|
| DELTA | 15.75 | 15 | 20 | 23.5 | 28.5 | 35 |

That prediction scored **14/14** on every 8.8S designation the bridge uses, first try, including
`M20 x 120` (grip 84) and `M24 x 330` (grip 277.5).

## What it was worth

5,405 bolts rebuilt in 130 s: **the axis-length spectrum is now identical to the source's** —
40:154, 45:115, 50:2020, 55:767, 60:344, 65:342, 75:686, 85:102, 90:128, 120:681, 140:8, 330:58
— and **5,405 of 5,680 bolt midpoints match to 0.1 mm (95.16%)**. `p1` = the source axis start,
`p2` = `p1 + grip·direction`; the bolt then spans its full length from `p1` on its own.

## The 275 that cannot be built here, and why

| | n | reason |
|---|---|---|
| `M12 x 210 / 350 / 400 / 450` | 177 | **AS_Bolt_88s stops at M12 x 60.** No such row exists |
| `M12 x 150 IG 8.8G` | 40 | the style `IG 8.8G` is not among the 27 installed |
| `M24 x 32.769` | 58 | not a catalogue length at all |

⚠️ And an earlier claim is corrected: "8.8S accepts only grip 24 for M12, grips 40–400 all
REFUSED" was true only for M12 — whose table really does end at 60 — and completely false in
general. M20 took grip 84 and M24 took 277.5 without complaint.

## What the source's own labels say

The rebuild's designations differ from the source's on 12 names, and in every case the source is
the odd one:

- 96 bolts named `M 14x45 **8.8/S**` and 10 named `M 27x75 **8.8/S**` — but AS_Bolt_88s contains
  **no M14 and no M27 rows at all**. The rebuild carries them as DIN912 / DIN6914, the tables
  that do contain those sizes, at the same lengths.
- 58 bolts named `1"` / `1" x 330 8.8/S` — imperial, in a metric model. The rebuild carries them
  as `M24 x 330` with the same 330 mm axis.
- 40 named `M12 x 150 4.6/S` whose style field says `IG 8.8G`. The name and the style disagree
  inside the source itself.

Reported as measurement. The file is Bernie's.
