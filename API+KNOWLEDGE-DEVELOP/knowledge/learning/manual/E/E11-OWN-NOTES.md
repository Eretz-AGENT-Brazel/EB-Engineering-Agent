# E.11 — Own Notes

*Manual p. 1179. **The page is blank.** It carries a heading and nothing else — the last page
of the manual, left for the reader's own notes.*

So this is the reader's. Written 10/08/2026, after 31 chapters and every claim below measured
in a live drawing rather than read anywhere.

---

## The one instrument this chapter produced: `vfy_grip`

E.9 turned up a number the agent had been working without: **`KlemmLen`**, which E.9.3 calls
*"the calculated clamping length of the bolt"* — **ProSteel's own figure for the thickness of
the packet a bolt clamps.**

That closes a hole that had been open since the verification kit was built. `vfy_bolts` prints
its own blind spot in every result line: *it matches bolts to holes by proximity, so it cannot
tell which part a hole belongs to.* And that blind spot is exactly where a real violation
hides — a bolt clamping one drilled plate and one **undrilled** one looks perfect to a
proximity matcher.

Every `PsSingleHole` carries `Start` and `End`, so a hole's **depth is the thickness of the
material at that hole**. Therefore:

```
sum(depths of the holes matched to a bolt)   vs   KlemmLen

equal          → every element the bolt clamps is drilled            OK
sum < Klemm    → the bolt clamps material that is NOT drilled     🧲 IRON RULE
sum > Klemm    → holes counted that the bolt does not cross — read the coordinates
```

### Calibration — the known-good case, exactly

Run on the E.10 portal, twelve M20×70 DIN6914 in end-plate joints:

```
verdict  bolt  klemm  drilled  diff  holes  owners
OK       34A   39     39       0     2      340:19, 344:20
```

**19 + 20 = 39**, on all twelve. And the instrument does what `vfy_bolts` could not: it
**names the parts and their individual thicknesses** — part `340` contributing a 19 mm hole,
which is the HE300B flange thickness, and part `344` a 20 mm one, the end plate. The total is
checked against ProSteel's own number.

Whole model afterwards: **24 bolts, 24 OK, 0 undrilled.**

### ⚠️ The UNDRILLED branch is NOT calibrated — said plainly

My own rule: *never trust a new check until it has failed a known-bad case and passed a
known-good one.* The known-good passed exactly. **The known-bad could not be built**, and the
reason is itself the most interesting thing in the chapter.

> ### ⭐ ProSteel refuses to bolt across an undrilled element
> Three plates stacked 20 + 15 + 20 mm, faces touching, the outer two drilled and the middle
> one left solid — the classic iron-rule violation. `boltparts` **refused**:
> *"holes further apart than 'Gap distance' … cannot be bolted."*
>
> The bolting routine will not span the undrilled plate, because from its point of view the
> two holes are not contiguous. **A meaningful part of the iron rule is enforced by the
> software at bolting time.**

⇒ Which says something about where violations actually come from. Not from bolting badly —
ProSteel resists that. **From editing afterwards.** Both real violations found so far arrived
that way: four orphaned bolts that survived a rebuild in which only rods and gussets were
deleted (09/08), and two bolts destroyed by a section change on a connected member (10/08,
E.9). ⇒ **Run the checks after every edit, not after the modelling.**

---

## ⚠️ Open, and it matters: `boltparts` refused a perfectly valid stack

While building the calibration rig, `boltparts` refused a case that has nothing wrong with it,
and the refusal message does not explain it. Measured:

```
plate 387   z −10 … +10   t=20      hole  122000,0,10 → 122000,0,−10   ⌀22
plate 388   z +10 … +30   t=20      hole  122000,0,30 → 122000,0,+10   ⌀22
vfy_touch   TOUCHING, per-axis separation Z = 0
```

The two holes **meet exactly at z = 10**, run in the **same direction**, are the same diameter,
and the plates touch with zero gap. `boltparts` still returned `create=False`, and the single
`bolt` op refused too. The canned reason — *"holes further apart than 'Gap distance', or angles
differing by more than 'Angle difference'"* — **is contradicted by the measurements**: there is
no gap and no angle difference.

⚠️ **This is a live risk, not a curiosity.** `boltparts` is the route used for hand-composed
connections; B.25's braced bay was bolted with it. If it can refuse valid geometry, earlier
work may carry fewer bolts than intended. **`vfy_bolts` over the older bands would settle it,
and it has not been run since this was found.** Recorded here rather than chased, because it
belongs to B.15 and this is E.11. **Worth raising with Amir before the next connection is
hand-composed.**

---

## What the manual will never tell you — the short list

Everything below was paid for by measurement. None of it is on any page.

| | |
|---|---|
| **The manual describes the DIALOG, not the API.** | B.15's *"drilling first is not necessary any more"* is true of the dialog and false of `PsCreateBolt`. B.12.6's ESC route has no code equivalent. |
| **`Create()` carries no information.** | `PsCreateBendPlate`, `PsCopeConnection`, `PsPurlinConnection` have each returned true having built nothing, and false having built something. **Only a read-back proves anything.** |
| **`writeTo()` carries no information either.** | Same rule, learned again in E.9. `propset` re-reads from a *fresh* instance and reports before → after per field. |
| **Start from `GetTemplate()` and override.** | A hand-built link data with *identical exposed properties* does nothing. Templates carry hidden state the property dump does not show. |
| **Values that never arrive.** | Base plate, end plate and purlin ignore the numbers passed and use the template's. ⇒ **choose a template, do not pass numbers.** |
| **When .NET will not bind, COM will.** | `doc.HandleToObject` via `PSCOMWRAPPERLib`. Four confirmations. `PsEditConnection` has no binder; `PsEditLogicalLink` does. |
| **Enum values are MEASURED, never inferred.** | `OutletType` starts at `kUndefinedOutlet`, so `type=0` is *nothing* — which invalidated a B.12 note. `ShapeType` is `kNormalType…`, not `eShape…`. |
| **A section key is an opaque string.** | `HEB300` does not exist; the real key is `HE300B`. **Search the catalogue; never assemble a key.** |
| **Anything needing a MOUSE PICK is unreachable.** | Not a run of accidents — the shape of the product. Full list in `THE-CEILING-what-code-cannot-reach.md`. |
| **The API does not filter by part type; the dialog does.** | A column reports `KlemmLen=0 Tension=0 MountingBolt=False` with no error (E.9). |
| **A plate's name is generated, not stored.** | Writing `Name` on a plate is silently ignored — measured on four. E.9.2 lists `Name` twice for exactly this reason. |
| **E.10's chapter numbers are stale.** | Correct through B.10, off by one from B.11, off by 2–3 in part C. The commands are right; the chapter numbers are not. |

## The working rules that came out of the mistakes

1. **Three strikes.** After three refusals: compose, ask, or record it as dialog-only.
   `PsBracing.insert()` got thirteen configurations before the answer was accepted; it was
   visible after three.
2. **Show the scheme before the detail.** Every scheme-level error so far was caught by Amir's
   eye, and every one arrived *after* the detailing was finished. My checks answer "did the API
   do what I asked"; his eye answers "is this a real detail".
3. **Calibrate before trusting.** Both non-trivial verification ops were wrong on first run and
   the known cases caught them.
4. **An instrument must state its own blind spot.** The one that never says "I don't know" is
   the one that produced 32 false flags.
5. **Retract in writing.** Two notes were wrong and are now struck through in place, with the
   measurement that replaced them: B.12's outlet types, and B.12's claim that outlets worked at
   all.
6. **A parameter an op accepts and then ignores is worse than one it refuses.** `plate layer=`
   was silently dropped on the branch nearly every plate takes, for weeks.

---

## The strip

`E-structural-elements.dwg`, **x 117 000 → 173 000**, bounded by the grid "E.11-OWN-NOTES".

**It is empty, and that is the honest representation.** E.11 is a blank page; its content is
notes, not geometry. The instrument this chapter produced — `vfy_grip` — lives in the plugin
and is calibrated on the E.10 strip next door. The calibration rig built here was removed once
it had answered its question, because it carried undrilled holes and a model does not keep a
defect to commemorate an experiment.
