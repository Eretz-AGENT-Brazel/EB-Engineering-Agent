# A MODEL YOU CANNOT SEE IS NOT A MODEL

*21/08/2026, at the end of the bridge round. Nineteen acceptance gates measured that rebuild to
a tenth of a millimetre. Amir opened the file and saw concrete, a grid, and a cloud of bolts.*

---

## What he saw

Steel: none. `op=classify` answered in seconds:

```
EB_OK classify parts=19832 ... tally[d0/a0/f-1 HIDDEN=13967 ...]

   Ks_Plate    Visible=False   7,806
   Ks_Shape    Visible=False   6,143
   Ks_Bolt     Visible=True    5,405
   Ks_ConcretePanel  True        314
   Ks_Shape    Visible=True       68   <-- built that morning
   Ks_Plate    Visible=True        1   <-- built that morning
```

**13,967 parts carried `Visible=False`.** Every layer was on and unfrozen; the entities
themselves were hidden. And the only visible steel in the drawing was the 68 shapes and 1 plate
created that same morning — which is how the first build was identified as where it came from,
rather than anything done during the repairs.

The fix is one call and about seven seconds:

```
classify visible=1        -> parts=19832 changed=19832 failed=0
```

## Why every gate missed it

Because they were all geometry gates, and **a hidden part measures perfect.** It has the right
section, the right length to the micron, the right holes to 0.1 mm, the right weight. The
rebuild's numbers that afternoon were: holes 12,265 of 12,265 with 97.57% inside 0.1 mm, bolts
matching the source's length spectrum exactly, steel weight +0.089%, bodies 95.80% inside
0.1 mm. All true. All of it invisible.

⭐⭐ **The gate set answered "is it right?" and never asked "is it there?"** — and the second
question is the one the person opening the file asks first.

## The gate, added

`app/bridge_verify.visibility_gate()` now runs first in `gates()` and is a HARD failure:

```
gate: visible parts    0 of 19832 hidden (Visible=False)    PASS
```

⚠️ Note in passing how it first read `-1 of 19832 hidden ... FAIL`: with nothing hidden there is
no `HIDDEN=` token in the tally, the regex found nothing, and the sentinel `-1` read as a
failure. **Absence had to be taught to mean pass.** A checker that cannot tell "clean" from
"could not read" is the same class of mistake as the thing it was written to catch.

## The rule to carry

Beside every gate that measures how *close* something is, there must be one that asks whether it
is *present and visible at all*. The same applies to the rest of the chain and is worth checking
before any of it is trusted:

- parts hidden with `Visible=False` (this one — now gated)
- a layer switched off or frozen (checked here too: all part layers were on)
- a part on a layer that does not plot
- a parts list written with no rows in it (`partlist` returns **true** on an empty file — see
  `PARTLIST-FROM-CODE.md`)
- a drill that reports EB_OK and creates nothing (`drill` counted totals, not deltas — see
  `DRILL-WHICH-WALL.md`)

Every one of those is a success message over an absence. They are the same bug wearing different
clothes, and measuring harder never finds them.
