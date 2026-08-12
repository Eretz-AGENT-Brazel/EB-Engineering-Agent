# HANDOFF — Part D. Read this, sync, then STOP.

## ⛔ DO NOT START WORKING.

Your only job in your first reply is to **orient yourself and report what you found**. Amir will
give you the tasks. Do not open a chapter, do not touch AutoCAD, do not build anything, do not
commit. **Read, verify, summarise, wait.**

---

## Where you are

Development folder: `C:\Users\User\Desktop\EB PROSTEEL AGENT`
Launcher: `EB PROSTEEL AGENT.bat` — starts AutoCAD 2015 + ProStructures V8i SS6 and the console.
Everything is committed and pushed; `main == origin/main`, working tree clean as of the handoff.

The company is **ארץ ברזל** — a steel **fabricator**, not a design office. Amir is the modeller you
are learning to be. Talk to him in Hebrew.

---

## Step 1 — sync yourself (read, do not run)

Read these, in this order:

1. **`PROGRESS.md`** — the master table. The five-step procedure per chapter is at the top, and
   **the standing rule Amir added on 11/08/2026 is in that same block: no more cutting corners.**
2. **`~/.claude/skills/prosteel-modeling/SKILL.md`** and
   **`references/plugin-ops.md`** (~3000 lines) — the accumulated lessons. `plugin-ops.md` is the
   one that matters most; its last third is all of part E.
3. **`knowledge/learning/manual/D/`** — *empty*. Part D has not been started.
4. **`qc/built-legacy.tsv`** and **`qc/joints-legacy.tsv`** — the two backlogs. Read the headers.

Then run **`python qc/consistency.py`** — read-only, safe. It must print `CLEAN`. If it does not,
say so and stop.

---

## Where the programme stands

```
A  interface, templates, settings      6 / 6    100% ✅
B  modelling and BOM                  29 / 33    87.9%   (B.30–B.33 left)
C  detailing and drawings              0 / 22     0%
D  miscellaneous                       0 / 7      0%   <- YOU
E  structural elements + appendices   11 / 11   100% ✅
                                      46 / 79    58.2%
```

**Part D, 7 chapters, none started** (manual fulltext line numbers in brackets):

| | chapter | subject |
|---|---|---|
| D.1 | User-Defined Component Parts | special parts, welded profiles, parametric profiles [23655] |
| D.2 | BlockCenter | block management [24152] |
| D.3 | Roof / Wall Panels | [24781] |
| D.4 | Dispatch Bolts / Blocks | [24898] |
| D.5 | Auxiliary Tools | ⭐ collision check, centre of gravity, pipe unrolling, DWG creation [25060] |
| D.6 | Effective Static Analysis Lines | |
| D.7 | Data Exchange | import/export, interfaces, RSTAB |

⭐ **D.5 is the one to look at first when Amir asks** — it contains the collision check we have been
using all along, and **pipe unrolling**, which touches the parked rolled-plate work.

---

## The five steps, per chapter (Amir's, not negotiable)

1. **Learn** the chapter end to end, write `knowledge/learning/manual/D/MANUAL-NOTES-D0N-*.md`
2. **Build it in a dedicated band**, reading every result **back from the model**
3. **Save** after every step
4. **Embed** in the skill (`SKILL.md` + `references/plugin-ops.md`), then `python agent-brain/sync.py`
5. **Commit and push**, with a message that records what was **measured**

> A chapter is ✅ only when all five are done. **Proving a command unreachable is a finding, not an
> implementation.** Learned-but-not-built stays ⬜.

**Band convention:** one strip per lesson on a 60 000 mm pitch along +X, bounded by a named
`Ks_Grid` on layer `_STRIPS`. Part E used 120 000 → 540 000. **Pick the next free x and check the
model first.**

---

## ⛔ The guards — they will block your commit, and that is the point

`qc/consistency.py` has nine checks. Three were written this session because I failed the thing they
now catch:

* **Check 8 — build proof.** Every ✅ row must cite a part count / `vfy_fit` / `collision`, or the
  explicit line `NO MODEL ARTEFACT -- <why>`. *Written after E.1 was committed green with no
  staircase in the model.*
* **Check 9 — joint audit.** Any chapter that builds a **bolted** assembly must cite
  `app/vfy_joined.py`. *Written after E.4 was committed with `vfy_fit bolts=196 OK=196` and **both
  rafters carrying zero holes** — joined to nothing. `vfy_fit` checks each BOLT against the parts it
  is LINKED to and is blind to a member that was never in the joint.*
* **`app/vfy_joined.py <xmin> <xmax> [--welded H1,H2,…]`** — asks from the MEMBER's side: what joins
  this part to anything? Every member must be **bolted** or **declared welded**. Welds are
  legitimate (B.23: they are not creatable from code) — **declaring them is the price.**

⚠️ **The two legacy lists may ONLY shrink.** Adding a row to make a guard go green is the exact
failure they exist to prevent. `B.15`, `B.26` and `E.1` are flagged **PRIORITY** in
`joints-legacy.tsv` — real bolted assemblies that have never been joint-audited.

---

## ⛔ Hard rules — do not re-learn these the expensive way

* **The iron rule:** a bolt must pass through a **drilled hole in every element it joins**. The only
  exception is a self-drilling screw, and only when Amir declares it for that specific case.
  **Silence is a critical error.**
* **Holes cannot be removed.** Never re-run a drilling or bolting pass over parts that already carry
  holes — **delete and rebuild instead.** A re-run is destructive and irreversible.
* **LISP is banned.** Metric always.
* **`sandbox.dwg` is Amir's** — do not save, change or close it.
* **`LETHAL-CALLS-do-not-invoke.md`** — read it before any new API path. Safety is **by TYPE, not by
  action**: `bind` requires `cls=` for exactly this reason.
* **The `wrongdoc` guard**: pass `dwg=` and every op refuses if the wrong drawing is active. ⚠️ Amir
  works in AutoCAD **in parallel on other files**. If the guard refuses, **stop and ask** — do not
  switch his active document.
* ⏸️ **Parked by Amir, do not open on your own initiative:** the four decisions from part B · phase 2
  (DAST / Standard Definitions) · `connset` (quarantined) · `Duebel.mdb` · any write to the connector
  databases.

---

## ⭐ The method lesson from part E — this is the one worth carrying

Part E was closed in one session, and the two things that cost the most were not knowledge gaps:

1. **A green check replaced looking at the thing.** `vfy_fit` passed while the frame's main members
   were joined to nothing. Amir found it by opening the drawing.
2. **The next measurement replaced designing the thing.** Five build passes on one truss, each
   fixing the last error message and exposing the next fault. The sixth worked because it started
   from the joint.

> **Detail the connection ONCE — envelope bands, edge distances, packet thickness, bolt length,
> set-back, which way the outstanding leg points — and then build.**

And the instruments that actually answer questions:
* **`propfull`** gives a part's `Origin`/`XAxis`/`YAxis`/`ZAxis` and `Wide`/`Height`. `rot` turns
  XAxis/YAxis about the member axis. ⭐ **Read the SIGN too — `Wide` can point in −x.**
* **When `boltparts` refuses, READ THE HOLES** (`op=holes`). They contain the geometry you got wrong.
* **`mods` and `holes` are different witnesses** — a hole *field* is not a hole.
* **`collision` is not idempotent**: it leaves `Ks_VolBody` behind and the next run counts its own
  residue. **Only the first run after a clean is a number.**
* **A booleans/`create` return value proves nothing** — `Create()` lies in both directions.

---

## Your first reply

Report, in Hebrew:
1. that you have read the above and the four files listed in step 1;
2. the output of `python qc/consistency.py` (it should be CLEAN, with the two backlogs printed);
3. the current git state (`git status -sb`, last commit);
4. whether AutoCAD is running and which document is active — **without changing it**;
5. your one-paragraph understanding of what Part D is and which chapter you would suggest starting
   with, and why.

Then **stop and wait for Amir's instructions.**
