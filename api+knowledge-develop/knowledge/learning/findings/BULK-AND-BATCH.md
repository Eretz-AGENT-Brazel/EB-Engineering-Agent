# BULK AND BATCH — when the model is big enough that the plumbing is the cost

*Opened 20/08/2026, on the night the bridge model arrived: 21,737 entities, ~15× anything
before it. Nothing here is a new ProSteel capability. It is the same ops, reached differently —
and on a model this size that difference is the difference between a night and a week.*

---

## The measurement that started it

The file protocol costs a fixed round trip per op, no matter how small the op is:

| op | measured | what it does |
|---|---|---|
| `ping` | **0.336 s** | nothing at all |
| `props` | **0.284 s** | reads one part's property bag |
| `mods` | **0.251 s** | reads one part's modification counts |

⇒ The cost is **not** the work. It is `write eb_cmd.txt` → `SendCommand` → AutoCAD's command
loop → `eb_result.txt` → a 0.12 s poll. So:

| what | the per-part way | the bulk way |
|---|---|---|
| props + mods for 14,018 parts | **2.1 hours** | `dumpparts` — **5.3 s** |
| ~43,000 build ops | **3.4 hours of protocol** | `batch` — **0.002 s/op**, i.e. ×140 |
| erasing 902 plates to rebuild them | 3+ min of COM deletes | `erase` — **under a second** |
| erasing 6,666 plates | — | `wipe cls=Ks_Plate` — **0.4 s** |

---

## The three ops

### `dumpparts` (v188) — every entity, props and modification counts, one call
```
op=dumpparts [out=eb_parts.txt] [cls=<substring>] [mods=0]
PART <tab> handle <tab> class <tab> layer <tab> extents <tab> props… <tab> mods…
```
Reads **every** model-space entity, not only parts: on the bridge model it answered for all
21,737 and got props for 21,737 of them (a dimension's property bag reads too), with
modification counts on the 14,044 that have them.

> ⚠️ **The modification read is GATED BY CLASS, on purpose.** `PsEditModification` is proven on
> `Ks_Shape`, `Ks_Plate` and `Ks_BendShape` and on nothing else — and **safety is per type, not
> per operation** ([[LETHAL-CALLS-do-not-invoke]]). A 14,000-part loop is the worst place in the
> world to discover that a type kills the session, because a loop cannot isolate anything.
> Unknown classes get props and extents; that is all.

### `batch` (v190) — many ops, one round trip
```
op=batch file=eb_batch.txt [out=eb_batch_out.txt] [stop=0|1]
   each line:  op=beam <tab> name=HEB500 <tab> p1=… <tab> p2=…
   each result: <line#> <tab> <op> <tab> <full result text>
```
**Why it was safe to add:** the switch that dispatches ops was lifted out of the CommandMethod
into `Exec(op, kv)`, and `Result()` now writes into a buffer when a batch is running. That is
the whole mechanism — **all 200+ existing ops became batchable without one of them being
touched.**

Three properties that are not negotiable, and why:
1. **Every item gets its own result line.** A batch that reported only a total would hide
   *which* part failed, and on a 1:1 rebuild the failures **are** the finding.
2. **Results flush to disk every 250 items**, with a progress file beside them. A batch of
   7,800 plates that dies at item 6,000 must still hand back the 6,000 it earned.
3. **The guards stay on the invocation, not the item** — wrong-drawing, command freshness,
   instance lock. That is correct: every item lands in the same active document, so one guard
   on the way in covers the file. The per-item check that *is* kept is the parameter allowlist.
   ⛔ Nested batch is refused, and `dbase` is refused inside a batch (it hangs AutoCAD).

### `erase` / `wipe` (v193) — deletion at the same scale
```
op=erase handles=<h,h,…>
op=wipe  cls=<class substring> | layer=<exact layer>
```
`wipe` **refuses to run with no filter**. There is no "wipe everything": the one thing a
destructive op must never do is empty a drawing on a typo.

---

## `xclone` (v192) — the honest route for what this machine cannot build

```
op=xclone from=<drawing open in this AutoCAD> handles=<h,h,…> [out=eb_xclone.txt]
```
`WblockCloneObjects` is called **on the source database** with an owner id belonging to the
**destination** database, under a document lock. It answers one line per asked handle:
`<source handle> <tab> <new handle>`, so the rebuild's map is exact.

**Two different walls make it necessary, and only these two:**

1. **A section catalogue this machine does not have.** 678 of the bridge model's 6,240 profiles
   name `PANEL`, `AMUDIM` or `HOESCHISODACH`. This machine's shapes database holds **357**
   catalogues and none of them is one of those. No `beam` call can conjure a section that is
   not installed — this is a **data** limit, not an API limit.
   > ⛔⛔ **AND THE FALLBACK IS WORSE THAN A REFUSAL.** `beam catalog=PANEL name='SVAHA
   > 1000X30'` did not fail — it built **431 members out of `DIN.DIN_FLACH`**, because the
   > resolver found the same *name* in a different catalogue. A part that arrives with the
   > wrong catalogue passes every count, every bbox and every position gate. **Check the
   > catalogue of what you built, not just the count.**
2. **A class with no creator at all.** `Ks_ConcretePanel` / `Ks_ConcreteSlab` /
   `Ks_ConcreteShape`, `AcDb3dSolid`, dimensions, `Ks_Grid`. For ProConcrete this was checked
   in **both** surfaces on 20/08/2026: `IKs_ComConcretePanel` carries `Copy`, `ArrayRectangular`,
   `Move`, `Mirror`, `Delete` — **and no creator**; and the twelve `PC3D*` managed assemblies
   are Forms and parameter holders (`CPSPC_PrecastPanelForm`, `CPSPC_PrecastPanelParameters`,
   `UserConnection`) — the interactive layer, exactly like the 62 `PSN_*` macros.

### ⏳ AND `xclone` HAS A COST CURVE — IT HUNG ON THE THIRD BATCH OF DIMENSIONS

Measured on the same night. In batches of 200 it cloned **678 profiles, 329 ProConcrete
objects, 7 solids, 26 bend shapes** and then dimensions at **2 s, 87 s, and never**: the third
batch pinned AutoCAD at one core for 14 minutes with memory flat, and the session had to be
killed (see [[LETHAL-CALLS-do-not-invoke]], entry seven).

> ⭐⭐ **2 s → 87 s → ∞ was the warning, in the log, before the hang.** A batch forty times
> slower than the identical batch before it means the call's cost depends on what the
> destination already holds — dimensions drag dimension styles, text styles and blocks, and each
> clone reconciles them against everything already cloned.
> ⇒ **Watch the per-chunk time and stop at the second jump.** And for the next attempt, try
> **one call for the whole class** rather than chunks: if the cost is per call over the existing
> set, fewer calls is strictly cheaper. That is the measurement to make.

⛔ **A hang ends the session, not just the op.** The hung instance keeps its COM registration,
so `GetActiveObject` resolves to it and never answers — and a healthy AutoCAD started afterwards
is **unreachable**, verified. It also keeps the drawing's file lock. Save before every risky
batch; the 20-second-old save is what made this cost nothing.

⇒ **Everything the API can build parametrically is built parametrically. The rest is cloned,
and every cloned part is declared as cloned.** A rebuild that hides which route each part took
is not a measurement.

---

## Two traps this scale exposed

### 1. `whoami` does not tell you where an op will land
It is a **diagnostic**: it goes to the shared mailbox and is **not** gated by `dwg=`, so it
answers for whatever document is in front. Measured 20/08/2026 — it named the *source* while
beams were landing correctly in the *rebuild*, which is the exact shape of the accident the
wrong-drawing guard exists to prevent, wearing the costume of the check for it.
⇒ **Fire a gated op first** (that activates the pin), **then read `eb_api._active_doc_name()`
through COM.**

### 2. One drawing, two channels
The mailbox slot is hashed from the drawing name — and `use("<basename>")` and
`open_model("<full path>")` hash **different strings**, so the same drawing gets two channel
directories (`…_db454627` from the path, `…_40083de7` from the name). The plugin looks in both,
so nothing breaks — but a script that writes `eb_full2.txt` through one and reads it through the
other would read a **stale file and never know**. Derive the channel once per process, from
`eb_api.channel()`, and never assemble the path by hand.
