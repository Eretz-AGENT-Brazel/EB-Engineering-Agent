# PARALLEL MODELS — working on more than one drawing at a time

*Opened 17/08/2026, by Amir's decision: he must be able to work on his own project while the
agent works on another, and the agent itself must hold several models, one task each.
Everything here was measured on this machine on that day. The authority for the RULES is this
file; the authority for the code is `app/worksession.py` + `app/eb_api.py`.*

---

## ⭐⭐⭐ The one measurement that decides the whole design

> **Exactly ONE AutoCAD is reachable over COM at any moment — the earliest-registered
> process that is still alive. Every later process is invisible, not merely hard to tell
> apart.**

How it was measured (17/08/2026, two `acad.exe` running, one holding `D-miscellaneous.dwg`,
the other `B08-insert-shapes.dwg`):

| what was asked | what came back |
|---|---|
| enumerate the Running Object Table | **two** entries under the same class moniker `!{0B628DE4-07AD-4284-81CA-5B439F67C5E6}` |
| bind entry 1 → `HWND`, `Documents` | `hwnd=853884`, docs = `D-miscellaneous`, `E-structural-elements` |
| bind entry 2 → `HWND`, `Documents` | **the same** `hwnd=853884`, the same documents |
| `GetActiveObject("AutoCAD.Application")` | the same instance again |
| kill that process, re-probe **12 s later** | one entry, `hwnd=10491676`, docs = `B08-insert-shapes` |

⇒ the second process **does** register itself; the class moniker simply resolves to the
first registration until it goes away. Reachability **transfers on exit**, it is not shared.

⚠️ A **document** moniker (`…\B08-insert-shapes.dwg`) appeared in the ROT once, bound
correctly, and had vanished from the table minutes later with the process still running.
It is a transient registration and **not a dependable route to a hidden instance.**

### 🛑 What this retracts

`eb_api.acad_instances()` and `SKILL.md` both carried, from 06/08/2026:
*"Both instances publish the SAME class moniker, so they cannot be told apart by name, only
by asking each one which drawings it holds."* — **the asking cannot happen.** Both monikers
answer for one instance. The pin-based instance selection built on that sentence
(`_app()` choosing among `len(cands) > 1`) has **never been able to fire**, because the list
is never longer than one. Recorded in `qc/retracted.tsv`.

---

## What follows for the two things Amir asked for

### 1. He works on his own model while the agent works on another

**Two ways, and only these two work:**

| | how | what protects him |
|---|---|---|
| ⭐ **A second AutoCAD, started AFTER the agent's** *(recommended — see the window note)* | he launches his own AutoCAD | it is **invisible to COM**. The agent physically cannot send it a command, a NETLOAD, or a variable write |
| **Same AutoCAD, two documents** | he opens his drawing in the agent's AutoCAD and runs `worksession.py claim <dwg>` | the agent **refuses to switch away** from a document that is not registered as its own, refuses every op aimed at his model, and refuses to close it. All three measured |

⚠️ **Why the order of that table was flipped on the day it was written:** the same-AutoCAD
arrangement is safe for the DATA and unusable for the EYES. **One MDI window shows one
document at a time**, so the moment the agent models, the view leaves whatever Amir was
reading. Nothing is damaged — he simply loses the screen. Measured directly: with `E` in
front and the agent assigned to `D`, the op refused rather than steal the view, and the way
to make progress was to give the front drawing to the agent. ⇒ **if he wants to LOOK at his
model while the agent works, he needs his own window, and it must be launched second.**

⛔ **The one arrangement that does not work: his AutoCAD started FIRST and the agent's
second.** Then the agent's own instance is the hidden one, and every op refuses with
`EB_ERR unreachable: attached to the wrong AutoCAD: it holds [...], not '<model>' -- and 1
further AutoCAD process is running that COM CANNOT reach`. Nothing lands anywhere wrong —
but nothing works either, until one of the two is closed.

### 2. The agent holds several models, each with its own task

**Inside one AutoCAD, and that is enough.** Measured working: two models open, ops
alternating between them, each op landing in its own model (`99` entities vs `732`), each
model's measurements written into **its own mailbox**.

The mechanism is three parts:
1. **A work session per model** (`worksession.py`): full path, owner, project, task, opened
   and touched times, agent-session id.
2. **A mailbox per model** — `app/plugin/ch/<slot>/`. The slot is computed **on both sides**
   from the drawing (`worksession.slot_of` ≡ `ApiCmds.SlotOf`, FNV-1a over the full path), so
   python and the plugin agree without negotiating. Verified live: python computed
   `d_miscellaneous_dwg_1cef7b2a` and the plugin, asked what channel it was using, answered
   the same string.
3. **Activate-then-fire, under one lock per AutoCAD window.** AutoCAD is single-threaded and
   every op resolves `MdiActiveDocument`, so several models in one instance means bringing a
   document forward and firing, serialised.

⚠️ **Therefore "at the same time" means interleaved, not simultaneous** — one AutoCAD runs
one command at a time. Two models make progress in the same session; they do not execute in
parallel. Genuine simultaneity would need two reachable instances, and there is only ever one.

---

## The rules the code now enforces (and where each was proven)

| # | rule | proof |
|---|---|---|
| **R1** | A model is entered explicitly and left explicitly | `worksession.py open/close`, selftest |
| **R2** | An explicit `dwg=` may **not** overrule the session — it refuses | selftest + live: `EB_ERR pin conflict` |
| **R3** | A model held by someone else is untouchable: op, NETLOAD and variable write alike | live: `EB_ERR hands-off` |
| **R4** | Consent expires (10 h, or a different agent run) and must be re-confirmed | selftest: `EB_ERR stale session` |
| **R5** | While anyone else holds a model, an **unpinned** op is refused outright | selftest: `EB_ERR no work session` |
| **R6** | The agent never switches away from a document that is not registered as its own | live: `EB_ERR blocked: … not registered to anyone` |
| **R7** | Identity is the **full path**; the file name is a label (`dwgpath=` in v185) | two `test.dwg` in different folders get different mailboxes |
| **R8** | A command file older than 300 s is never executed | plugin `CmdMaxAgeSeconds` — otherwise a stale shared command would run in whatever document happened to be in front |
| **R9** | ⭐ **An ASSIGNMENT outranks everything.** While Amir has locked the agent to one model — or to a SET of models, each with its own task — it cannot enter anything outside the set: not by `use()`, not by `open_model()`, not through `EB_MODEL`. *Within* the set, `dwg=`/`EB_MODEL`/`switch` choose which member this op works, and the switch is total (session, pin, channel, activation) | selftest 43/43: `EB_ERR assigned` · `ASSIGNED to '…' -- refusing to enter` · env-var bypass tested both outside (refused) and inside (selects) the set. Live 17/08: both of Amir's situations end-to-end — situation 1 (one model + his own second AutoCAD, 6/6) and situation 2 (a set with per-model tasks, 4/4) |

> **Ownership and assignment are two different questions.** `claim` says *"do not touch
> MINE"*; `assign` says *"work on THIS one and nothing else"*. The agent legitimately wants
> several models at once — assignment is how Amir says *not today*. Only he lifts it
> (`worksession.py unassign`). The operating procedure lives in **`WORKING-ON-MODELS.md`**
> at the repo root, next to `SESSION-START.md`, because it is the page he runs commands from.

⚠️ **Diagnostics are deliberately exempt** (`ping`, `whoami`, `env`, `docs`): they change
nothing, they are what you reach for when the wrong document is in front, and they go to the
shared mailbox so they answer even when no channel matches.

---

## What is still open

- **`_close_stray_docs` refuses whenever a second `acad.exe` exists** (06/08 rule, unchanged).
  With the hidden-instance measurement it is now clear that this is not merely cautious: the
  routine could not have reached the other process anyway.
- **The shared mailbox can still replay a stale command** if a caller with no work session
  writes one and a different document is in front within 300 s. By construction only
  read-only diagnostics are written there by `run()`; a *legacy* script that never calls
  `use()` is the remaining path, and the gate refuses those whenever anyone else holds a
  model (R5).
- **Not measured:** whether a second instance can be reached by launching AutoCAD with
  `/automation` or by a DDE/`ObjectDBX` route. Neither was needed once the one-instance
  design was proven, and both would have to answer the ProSteel question again.
