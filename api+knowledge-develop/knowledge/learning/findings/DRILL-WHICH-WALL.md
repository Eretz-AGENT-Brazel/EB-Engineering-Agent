# WHICH WALL GETS THE HOLE — the drill rule, measured

*Opened 21/08/2026, after two wrong answers to the same question. The bridge rebuild had 1,112
holes that were not where Bernie put them, and both earlier explanations — a wrong flange
selector, then a reversed ray — were refuted by measurement. This is the third answer, and it
is the one that took the count to 4.*

---

## The rule

Built an `SHS80X80X3.6` in a sandbox band, walls at x = 40960 and 41040, and drilled the same
80 mm axis six ways:

| `at` | `n` | where the hole appeared |
|---|---|---|
| the OUTER **+x** face | **−x** | **the −x wall** (40960 → 40963.6) |
| the OUTER **−x** face | **+x** | **the +x wall** (41040 → 41036.4) |
| the INNER **−x** face | **−x** | the −x wall (40960 → 40963.6) |
| any of the above | + `innercontour=1` | ONE hole through the whole 80 mm |

⭐⭐ **The hole lands in the wall where the ray LEAVES the part, not where it enters.**

Which makes the recipe for reproducing a known hole `a→b`:

```
at    = the midpoint of the wanted hole
n     = away from the part's centre     (the outer end minus the inner end)
depth = |a − b|                          (SetHoleDepth — see below)
```

That scored **8/8** on every section whose geometry was *measured* — the four walls of an SHS80,
both flanges of an HE200B, both flanges of a U220. It scored 0/3 on an L70X7 and a plate, and
all three were my own fault: I invented where the leg and the plate face were instead of
reading them. The angle's leg is at z = −28…−35, not 0…−7, and `plate9 mode=rect` centres the
plate on its insert plane (−6…+6, not 0…12). Both holes came out exactly one leg / one
thickness long, at the geometry that actually existed. *Guessing the target is not a failure of
the rule.*

## `depth` — the setter the op never exposed

On an I-profile **none** of the above applies. An HE200B drilled from z = +92.5 with `n = +z`
gives one hole from **100 to −100** — the whole section — and `flange=0`, `flange=1`,
`flange=2`, `innercontour=0` and `innercontour=1` all produce exactly the same 200 mm hole.
(`flange=2` makes two of them.) There is no wall selection on a section whose outline is a
single closed loop.

`PsDrillObject.SetHoleDepth` is the answer, and v198 wires it: `depth=15` on that beam gives
**z 100 → 85**, the top flange alone. The depth is measured from the EXIT face inward, which is
the same rule again. `SetDeepStart` (`dstart=`) is wired next to it, along with `midline=`,
`xoff=`, `yoff=`, `xpos=`, `ypos=`, `counter=`, `step=` — of 27 setters on `PsDrillObject` the
op had been reaching 14.

Slots are unaffected: `slot=7` with `depth=5` keeps its 7 mm travel, read back both ways
(`lhm=1` one row, `lhm=2` two circles 7 mm apart), and `rotslot=1` turns the travel 90°.

## What it was worth on the bridge

Same gate, same model, before and after — source `lhm=2` against rebuild `lhm=2`, greedy
one-to-one per part, worst-endpoint distance:

| | before | after |
|---|---|---|
| within 0.1 mm | 10,779 (87.88%) | **11,973 (97.62%)** |
| within 20 mm | 11,153 (90.93%) | **12,261 (99.97%)** |
| beyond 20 mm | **652** | **0** |
| no partner at all | 460 | 4 |

Every hollow and I section went to zero: U220 334→0, SHS80 196→0, HE200B 184→0, HE300A 84→0,
IPE160 84→0, HE400B 30→0.

## Why the earlier answers were wrong

**"It is the flange selector."** A per-hole sweep of `flange=1/0/2/unset` moved nothing: unset
gained 178 holes, the three selectors gained zero.

**"It is a reversed ray."** ⚠️ Reversing `n` moves the exit wall too, so the hole swaps ends
instead of arriving. The sweep built on it "corrected" 542 rays and made the model **worse**
(exact 9,811 → 9,337) and had to be rolled back. It looked right because on the bridge every
bolt through a hollow section is stored as a PAIR of wall holes on one axis: both halves exited
through the same wall, so half of each pair landed on top of its partner and the other half was
missing. Part 639BF had 14 hole rows standing at 7 positions.

## The instrument that had been lying

`drill` counted the part's holes after the call and reported EB_OK if the count was above zero.
`HolesOf` returns the part's **total**, so a drill that creates nothing on a part that already
carries two holes answers 2 — and reports success. 24 holes on 11 plates were reported drilled
twice, once in a batch and once on an individual retry, and not one of them existed.

⇒ v199 reports **`made=`**, the delta on THIS call, and `made=0` is EB_ERR. The 24 refusals
then showed up immediately and honestly, three retries running.

## The hair's breadth

Those 24 turned out to be real refusals, and the reason is worth keeping:

```
at x = 184832.1595  ->  EB_ERR  made=0
at x = 184832.1500  ->  EB_OK   made=1
```

**0.0095 mm decides it.** The hole centre sits that far outside my rebuilt plate's contour and
ProSteel refuses — correctly; the centre is off the material. Moving 0.5 mm *along the ray* does
not help: the sensitive direction is IN THE PLANE of the plate. Pulling the insert point
0.02 mm toward the plate's centre landed 20 of the 24, and 0.02 mm is four orders of magnitude
below any fabrication tolerance.

The last 4 are not a drill problem at all. Their plate is `PLATE 470x320x12` in Bernie's model
and `PLATE 470x195x12` in mine — **125 mm narrower**, because the cut planes my rebuild applied
to it removed material that should have stayed. The holes fall on steel that is not there. That
belongs to the cut-plane gap, not this one.
