# E.8 Structural Element — Joist

> ## ✅ CLOSED 11/08/2026. **The last chapter in part E.**
> **18 parts · 40 web-into-chord cuts · JOINT AUDIT CLEAN · `vfy_fit bolts=258 OK=258
> BOLT-NO-HOLE=0 SHORT=0` · band collisions 53 → 23.**
> ⚠️ The seat-to-support **bolt did not form** in three attempts. Recorded below, not hidden.

*Read 11/08/2026, pages 1117–1127 (fulltext lines 28843–29029). Band `E.08-JOIST`, grid at
x = 540 000. Plugin v178. Command **`PS_JOIST`**.*

---

# ⭐⭐⭐ READ THIS BEFORE USING IT: IT IS AN AMERICAN CATALOGUE PRODUCT

> *"This function serves for creation of a **joist as it is often used in the USA**. This joist is a
> **pre-fabricated element in lightweight construction** that is offered in different executions and
> lengths."*

⇒ **E.8 is not a European lattice girder.** It is the US open-web steel joist — a **factory-welded,
catalogue-sized** product bought by designation, not detailed member by member.

⚠️ **For ארץ ברזל that is a "know it exists, do not reach for it" element.** EB fabricates to EN;
a joist specified this way carries an American product standard behind it and cannot be quoted or
made from this dialog. **The most useful sentence in the chapter is its first one.**

⭐ It matters for one practical reason: **E.6's purlin course can host one** — `UseJoists` +
`JoistTemplateName`, and E.6's own text says *"you can also use a joist girder instead of a standard
shape… the template of which can be selected on an additional dialog tab."*

---

# THE DIALOG

### Dimensions
`Length` · `Height` · ⭐ `Inner Distance` — *"the distance of shapes in **transverse direction**"*
(the chords are **pairs** of shapes with the web between them) · ⭐ `Calculate Spacing` — *"the inner
distance is calculated for the vertical and diagonal components **based on the selected shapes**"* ·
`Roof Angle` — *"the joist is built as a **saddle roof**"* · `Continuous…` — *"built as **desk
roof**"* · `Seat Height` — *"the height of the **lateral seats**"* · `Seat Length` — *"for **both
sides separately**"* · `Lower Retreat` — *"referring to the **upper length**, for both sides
separately"*.

### Layout — and the API confirms it 1:1
**Symmetrical · Single Lacing · Double Lacing · Tension Rods Type 1 · Tension Rods Type 2**, which is
exactly `JoistLayout : kSymetricShapes, kSingleLacing, kDoubleLacing, kTensionRod1, kTensionRod2` —
same five, same order.

### Shapes / Assignments
`Shape Offset` selects *which rod area* the settings apply to, and the API names the six areas:
`JoistWhatShape : kTopChoord, kDownChoord, kVertical, kDiagonal, kTopSeat, kDownSeat`.

---

# ⛔ NO CREATOR AT ALL — THE THIRD INSTANCE

`PsJoist` has **no `insert()`, no `Insert()`, no `init()`, no `Create()`**, there is **no
`PsCreateJoist`**, and there is **no `Ks_ComJoist` COM twin** — so the COM escape hatch that rescued
`PsGrid` does not exist here either. Verified three ways in the surface dump.

```
E.5  PsTruss               no creator
E.6  PsPurlinDistribution  no creator
E.8  PsJoist               no creator      <- third instance
```

⭐ **The case-insensitive grep matters**: `plugin-ops.md` records that *"a grep for `insert(` misses
`Insert(`"*, which is how `PsStairs.Insert` was nearly missed. Checked both ways here.

⭐ `calculatePoints()` returns a `PsJoistPoints` — the geometry generator is present and the creator
is not. **You can compute a joist and never build one.**

---

# STEP 2 — WHAT IS BUILT

The detail was fixed before the first build call, and the product's own nature set the joint types:
**a joist is factory-welded**, so the web-to-chord joints are welded *by definition* — bolting them
would be wrong, not merely unmeasured.

```
Length 8000 · Height 600 · 8 panels of 1000 · kSingleLacing (one zig-zag web)
chords      2 × L50×5 top + 2 × L50×5 bottom, at y = ±20   <- the chapter's `Inner Distance` = 40
web         8 lacing diagonals + 2 end verticals, RO 26.9×2.6
supports    2 × IPE200 rot=0, axis z = −100 -> top flange at z = 0
seats       2 plates 150×100×10 on the supports
cuts        40 boolean subtractions, web into chords = the factory weld's composition equivalent
```

| verification | result |
|---|---|
| `vfy_fit` model-wide | **`bolts=258 OK=258 BOLT-NO-HOLE=0 SHORT=0`** |
| **JOINT AUDIT** | **CLEAN** — 18 members, none floating |
| `collision` in band | **53 → 23** after the cuts |

## ⚠️ THE SEAT BOLT DID NOT FORM — three attempts, each diagnosed

The one genuinely bolted joint on a joist is the **seat**, where it lands on the supporting steel.
It never formed, and each failure was read from the model rather than guessed:

```
1. support rot=90 -> Wide 100 along +z, Height 200 along +x   THE BEAM WAS LYING ON ITS SIDE.
   Its top face was at z = −50 while the seat sat at z 0…10 — 50 mm of air. And the drill along −z
   put its holes at z −102.8…−97.2: 5.6 mm, the IPE200 WEB.
2. support rot=0  -> Wide 100 along −x, Height 200 along +z   depth vertical, correct.
   But the flange width extends in −x from the origin, so the support occupies x 539900…540000
   while the seat and its bolt line sat at x 540075 — ENTIRELY OUTSIDE IT.
3. seats re-placed over the supports at x 539950 / 547950  ->  still holesOnParts=2:
   the seat drilled, the support not. UNRESOLVED.
```

⭐ **`Wide` runs along the part's XAxis and can point in −x.** Reading `propfull` is not enough —
the SIGN has to be read too, which is B.22's *"do not generalise the sign"* in a new costume.

⚠️ **The seats are declared welded.** For a US joist that is the common detail anyway — seats are
routinely welded to the supporting steel — so the declaration is true engineering and not a dodge.
**But it is not what was attempted, and the bolt route is open.**

## ⭐ AND I MISREAD MY OWN E.2 FINDING FOR A MOMENT

After the 40 cuts the band read **214** collisions and the model 553, against 53 before. That looked
like the fix making things worse. It was not: repeated `collision` runs had left `Ks_VolBody`
residue and **the checker was counting its own leftovers** — the non-idempotence E.2 measured and
wrote down. A clean run gave **23**.

> ### ⭐ ONLY THE FIRST `collision` AFTER A CLEAN IS A NUMBER.
> E.2 established it, E.8 nearly drew the opposite conclusion from a contaminated one. **Clear the
> VolBody, then read.**

---

# Still open
* ⚠️ **The seat-to-support bolt** — three diagnosed failures, unresolved. The seats are welded.
* ⚠️ **23 collisions** remain in the band after the web cuts, not chased down.
* ⬜ The other four layouts — Double Lacing and both Tension Rod types — read, not built.
* ⬜ `Roof Angle` (saddle) and `Continuous…` (desk roof) — read, not built.
* ⬜ ⭐ **`UseJoists` + `JoistTemplateName` on E.6's purlin course** — the hand-off between the two
  chapters, and the only route by which a joist would realistically enter an EB model.
* ⚪ `JoistWhatShape` is inferred to be the `Int32 Index` of the indexed accessors and of
  `clearShapeReference(Index)`; `getElementCount()` should return 6 if so. **Not verified.**
