# Bolt audit — `B08-insert-shapes.dwg`, 10/08/2026

*Run at Amir's approval, after `boltparts` was found refusing geometry that measured perfect
and the question arose whether earlier bands carry fewer bolts than intended.*

**835 parts · 194 bolts · 593 holes · 20 chapter bands.** Nothing in the drawing was modified —
every op used was a read.

---

## 🛑 First, a retraction

The audit's first pass reported **20 bolts clamping material with no hole**, on the strength of
`vfy_grip`, built this morning out of E.9's `KlemmLen`. **That reading was wrong and none of it
was reported as fact.** Two measurements killed the premise:

1. **Bolt `F82`** is an `M 16x75 Mu DIN7990` reporting `klemm=50`. The only steel on its axis is
   an **L 90x9** leg (9 mm) and a 10 mm gusset. **There is no 50 mm of material there.**
2. **Every** `M 20x70 DIN6914` in the drawing reports `klemm=39`, and **every**
   `M 20x70 Mu DIN7990` reports `42` — identical within a type, across different joints.

⇒ **`KlemmLen` is essentially a property of the bolt TYPE and LENGTH, not of the packet it
clamps.** It coincided with the packet exactly (39 = 19 + 20) on the one joint it was first
calibrated against, because there ProSteel had *chosen* the bolt to suit the packet. **One
calibration case is not a calibration** — the rule already written in the skill, applied to
itself one step too late.

`vfy_grip` has been **removed** and replaced by `vfy_fit`, which judges only measured geometry.
The retraction is recorded in the op's own source, because the mistake is more instructive than
the fix.

---

## What is actually wrong, all of it measured

### 1. 🧲 Seven bolts with **no hole at all** — iron-rule violations

| band | bolts | what they are |
|---|---|---|
| **B.26 portal BOLTED**, apex | `1050` `1051` `1052` `1053` | 4 × `M 20x70 Mu DIN7990` at y=±60, z=5790 / 5970 |
| B.15 bolts (x≈210 000) | `B74` `B75` `B76` | `M 16x65`, **`M 16x0`** (zero length), **`M 20x660`** (a 660 mm rod) |

**The four at the apex are the serious ones.** They sit at coordinates where **no hole exists** —
the nearest is **81.5 mm away**, at y=±75, z=5870. They are **orphans left behind by a rebuild**,
the same pattern as the four found on 09/08 in the braced bay: the joint was rebuilt at slightly
different coordinates and the earlier bolts were never removed.

⚠️ **This is in the frame built for Amir on 09/08 after his own correction.** It was declared
clean at the time on the strength of a check that counted only what had just been built.

The three in the B.15 band are **experiment residue** from studying bolts — a zero-length bolt
and a 660 mm rod are not connections. They should be cleared, but they are not a detailing fault.

### 2. Eight redundant bolts — the apex joint is bolted **three times over**

`vfy_dupes`, tol 3 mm:

```
x3 at 348992,-75,5870   M 20x70 Mu DIN7990   10DD, 1134, 119C
x3 at 348992, 75,5870   M 20x70 Mu DIN7990   10DE, 1135, 119D
x3 at 348992,-75,6130   M 20x70 Mu DIN7990   10DF, 1136, 119E
x3 at 348992, 75,6130   M 20x70 Mu DIN7990   10E0, 1137, 119F
```

Four hole positions, **twelve bolts**, eight of them redundant.

⭐ **A duplicate is invisible to every bolt-vs-hole check** — each copy matches the same hole
perfectly, so `vfy_bolts` reports it as clean. It shows up in the **parts list**, which would
order three times the bolts, and on the shop floor.

### 3. Ten redundant holes, in two parts

`AE6` — 6 redundant · `10E9` — 4 redundant. Drilled more than once at the same coordinates.
Harmless in the model; **wrong on an NC file**, which would drive the drill twice.

### 4. Eight oversized bolts — B.25's braced bay

`F82`…`F89`, all `M 16x75 Mu DIN7990`, clamping a **19 mm** packet (a 10 mm gusset + a 9 mm
angle leg).

**The threshold is calibrated by the model itself, not assumed.** Across ten healthy bolt types
in this drawing, `spare = nominal length − packet` sits in a tight band:

| bolt | n | spare (min / median / max) |
|---|---:|---|
| M 16x45 Mu DIN7990 | 14 | 25.5 / 25.5 / 27.5 |
| M 16x50 Mu DIN7990 | 70 | 26.0 / 30.0 / 30.0 |
| M 16x55 Mu DIN7990 | 21 | 26.4 / 28.4 / 28.4 |
| M 16x60 Mu DIN7990 | 12 | 9.0 / 29.0 / 29.0 |
| M 20x70 Mu DIN7990 | 12 | 30.0 / 30.0 / 30.0 |
| M12 x 40 8.8/S | 12 | 22.5 / 22.5 / 22.5 |
| M16 x 50 8.8/S | 6 | 26.4 / 26.4 / 26.4 |
| **M 16x75 Mu DIN7990** | **8** | **56.0 / 56.0 / 56.0** |

**22–31 mm is a nut plus a washer plus a few protruding threads.** 56 mm is about **30 mm of
bolt hanging out of the joint**. They should be M16×45 or M16×50 — both of which the drawing
already uses everywhere else.

### 5. Eighteen `SHORT` flags — *not* defects, an instrument limit

Where two bolt rows sit closer together than the 12 mm matching tolerance, each bolt sweeps in
its neighbour's holes, the packet reads too thick and the spare too small. `vfy_fit` prints this
blind spot in its own result line. **Read the `owners` column before acting on a SHORT.**

---

## The instruments, after the correction

| op | what it judges | what it cannot see |
|---|---|---|
| `vfy_bolts` | does every bolt have holes near it | **which part** a hole belongs to |
| **`vfy_fit`** | `spare = nominal − packet`; oversized / short bolts | over-counts when bolt rows are closer than `tol` |
| **`vfy_dupes`** | bolts at the same point; holes at the same point in one part | nothing else — it is pure coincidence detection |
| `vfy_touch` | gap or overlap between two parts, as a number | rotated parts (axis-aligned extents) |
| `vfy_size` | has a member been stretched | grids and work frames are skipped by design |

⭐ `KlemmLen` is still printed by `vfy_fit`, **as information only. It judges nothing.**

---

## What has NOT been done

**Nothing has been fixed.** The approval covered running the checks. Every item above is a
deletion or a substitution in a model of record, and those are Amir's call:

1. delete the 4 orphan bolts at the apex, and the 8 redundant copies — **12 bolts**
2. delete the 3 experiment bolts in the B.15 band
3. replace the 8 M16×75 in B.25 with M16×50
4. clear the 10 redundant holes in `AE6` and `10E9`

## Still open, and separate

**`boltparts` refuses geometry that measures perfect** — two 20 mm plates, `vfy_touch` reporting
zero separation, holes meeting exactly and running the same way, same diameter. The audit did
**not** find evidence that this cost the older bands their bolts: the bands are fully bolted, and
their faults are *duplication and orphans*, not absence. **The refusal remains unexplained and
belongs to B.15.**
