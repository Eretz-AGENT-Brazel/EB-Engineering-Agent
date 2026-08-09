# Build the detail once, then replicate — the method, not a tip

*Amir, 31/07/2026, after watching me spend far too long on a task he could do in
minutes:*

> *"If you noticed, when I gave you the lessons — **I modelled once and then
> replicated.** And that is the whole principle. You produce one detail that
> repeats, and then simply copy and paste it wherever it is needed, and rotate it
> or mirror it. It is that simple and that accessible — and you go around in
> circles writing scripts for really simple operations."*

**This is the single most important working principle in the whole project.**
It is not an optimisation. It is how steel is modelled.

---

## What it costs to ignore it

Exam task: 20 steel columns, each with a floor base plate and three wall plates,
every plate fully holed and bolted.

| | element-by-element (what I did) | build-once-then-replicate |
|---|---|---|
| one detail (column + base plate + 3 wall plates + 20 anchors) | ~30 operations | ~30 operations |
| × 20 locations | **~500 operations** | **19 copies + rotate / mirror** |
| total | 500+ | **~70** |
| wall clock | over an hour, two rebuilds | inside the 10-minute limit |

Seven times the work for an identical model — and every extra operation is another
chance to get a coordinate wrong.

## The evidence was in front of me three times

| lesson | what Amir did | what I wrote down | what I failed to do |
|---|---|---|---|
| 4 | one rib → `PS_COPY` ×7 + `MIRROR` ×2 + `ROTATE` = 8 ribs | "one and then symmetry" | treated it as a nice touch, not the method |
| 5 | **`PS_COPY` ×28** — the busiest command in the whole session | logged the count | never asked *why* it was the busiest |
| 5 | *"I made an ordinary plate and copied the anchor bolts onto it"* | recorded as his workaround | missed that copying **is** the technique |

I documented the symptom every time and never adopted the behaviour.

## Why I drifted into element-by-element work — and why that is no excuse

My command channel sends one instruction and reads one answer. ProSteel's
replication tools (`PS_COPY`, `_ARRAY`) are **dialogs**: they ask for a selection,
a base point, an array pattern, a count. Since I cannot answer a dialog, I got
into the habit of decomposing everything into atomic, dialog-free operations —
and then never questioned it.

**That habit deformed my idea of the work itself, not just the implementation.**

**And the tool was already mine.** In lesson 2 I built `clonemodel` —
`Database.DeepCloneObjects` + `TransformBy(Matrix3d.Displacement)` — which copies
objects **with everything attached**: holes, connections, layers, anchors, groups.
I used it to duplicate a whole model and never once thought of it as *"replicate a
detail"*. Swapping `Displacement` for `Matrix3d.Rotation` (or a mirror matrix) is a
one-line change and gives exactly copy / paste / rotate / mirror.

Nothing was missing but the method.

---

## The rule from now on

**Before creating the second instance of anything, stop and replicate the first.**

1. **Find the repeating unit.** Not "20 columns" — *one* column complete with its
   base plate, wall plates, holes and anchors.
2. **Build that unit once, correctly, and verify it.** All the checking effort goes
   here, on one detail.
3. **Replicate it** to every location: `clonemodel` with a displacement matrix, or
   `PS_COPY` / `_ARRAY` natively, then rotate or mirror per orientation.
4. **Verify the copies against the original**, not against the plan — they either
   match the verified unit or they do not.

**Corollary — never emit a loop of N identical creation commands.** If a loop is
about to run the same creation call more than about three times, the answer is a
replication operation instead.

**Corollary — when the tool is a dialog:** do not decompose it into hundreds of
atomic operations. Look for the scriptable equivalent (`clonemodel`, `_ARRAYRECT`,
native `_COPY` with a displacement), and if there is none, **ask Amir** rather than
grinding out the long way. He can often do it in seconds.

## Learned in blood: replicating a detail that CONTAINS a parametric connection

Exam 5 (20 columns, each with a base-plate connection) exposed two side effects
that pure geometry copying does not have:

1. **The cloned connection RE-RUNS itself in every copy.** Each copy gained
   4 extra anchors (the connection's own, at ITS default depth — not the
   approved one) on top of the 24 anchors that were cloned. 556 anchors instead
   of 480, and they are NOT exact duplicates (different Z), so a same-point
   dedupe misses them. Filter by the approved geometry (e.g. bolt bottom at the
   approved embedment) to find the connection's own extras.
2. **Manually drilled holes do not survive the clone.** The connection re-drills
   only its own hole field; the 2 centre holes added with `PsDrillObject`
   vanished in every copy (4 holes instead of 6). Re-drill them per copy after
   replication.

**Standard post-replication step: dedupe the connection's own output, re-drill
the manual holes, then run the geometric audit.** Measured cost: ~0.8 min for 20
positions — trivial next to the errors it removes.

Also: `RemoveAllLogicalLinks(deleteParts)` does **not** restore the column length
that `ShortenShape` took. A remove+rebuild cycle shortens the column again each
time — nine cycles left a column floating 180mm above the slab. **Never fix a
connection by delete+rebuild in a loop; fix parameters, or rebuild the column
with it.**

## Anchor-bolt seating (Amir's corrections, exam 5)

An anchor bolt is three segments: **embedment in the concrete** (Amir: 120 mm —
ask, never invent) → **through the plate** → **proud of the plate by exactly the
nut height** so the nut bears ON the plate. Two faults to never repeat:
- centring the bolt on the hole ⇒ it floats in mid-air past the plate;
- picking a catalogue length that leaves the nut hovering (172.5 long left a
  10 mm gap — 165 closed it to the software's length-step residual of 2.5).
And: **"approved" ≠ "verified"** — the floor anchors carried the same floating-nut
fault that was fixed on the wall, unchecked because the detail was "approved".
After any adjacent change, re-audit everything.

## Where replication fits with the other principles

- **Lesson 4:** the unit is the *connection*, not the object.
- **Lesson 5:** the macro is not the criterion, *correctness* is.
- **This one:** the unit is also the *detail*, and details are **replicated**.

Together: build one correct connection-bearing detail, verify it hard, then copy /
rotate / mirror it everywhere it belongs.
