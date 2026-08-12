# A.1 — General Information

*Manual pp. 15–18. Read 10/08/2026. Implemented in
`projects/SANDBOX/A-dialogs-templates-settings.dwg`, strip **A.01**, x −3 000 … 53 000.*

The shortest chapter in the manual — two sub-sections and about a page and a half. It is also
the one that should have been read **first**, two weeks ago.

---

## ⭐⭐ The headline: the manual documents an OLDER program than the one we run

A.1.2 says `PS_VERSION` prints the running build, and gives a worked example:

> *"You are using ProSteel · Version **V8i (SELECTseries 3) — Version 8.11.3.48** dated from
> **Aug 25 2010**"*

Measured here, from the assembly **actually loaded into AutoCAD** (new op `env`):

| | version | |
|---|---|---|
| **the manual's own example** | `8.11.3.48` | SelectSeries **3**, Aug 2010 |
| **what is running** | `08.11.11.161` | ProStructures **Ss6 R1**, © 2013 |

Same `8.11`, but **build 3.48 against 11.161 — eight SelectSeries apart and three years
later.** AutoCAD is 2015 (`R20.0`, `ACADVER 20.0s`).

And A.1.1 warns about exactly this, in its own words:

> *"there may be differences due to **intermediate updates of the program after publication of
> this manual**."*

⇒ **This retroactively explains a finding from earlier today.** E.10's chapter column is off by
one through part B and by two or three in part C — because **chapters were inserted between SS3
and SS6** and the reference table was never renumbered. The one row citing `B.11` correctly is
*"Create ACIS body reference"*, the chapter that was inserted.

⇒ **Every "the manual says X" carries an unstated "…in SS3".** And the reverse holds too:
this build may do things the manual never mentions.

### The rest of A.1.1, briefly

* The manual describes **ProSteel Professional for AutoCAD**. ProSteel also ships on other CAD
  platforms (via *CAL — CAD Abstract Layer*) and **in several licence models with different
  performance ranges**, so a function in the book may simply not exist in a given licence.
* It assumes AutoCAD is already known: **object snap, UCS, blocks**. It does not teach them.

---

## What was built

### `op=env [full=1]` — A.1.2 without touching the command allowlist

Reports the versions from **inside the running process**, which is strictly stronger than
reading a file on disk: it says what is **loaded**, not what is installed. And it needs no
AutoCAD command, so the `cmd` allowlist stays at the nine entries Amir set.

It also showed which ProSteel plug-ins are live:

```
ProStructuresNet       file 08.11.11.161   asm 1.0.6045.20863
PSN_PlugInBase         file 18.0.6045.20902
PSN_ConnectionCenter                            <- the 62 macros mapped in B.24
PSN_DoubleClick                                 <- the module E.9 named as a way into the
                                                   properties dialog
```

⚠️ It also showed **eleven old `EBAgentApi*` assemblies still loaded** (127 … 138). Every
`NETLOAD` adds an assembly that never unloads — which is *why* each rebuild needs a new
filename, and a small ongoing cost of the development loop.

> ### 🛑 A correction I had to make to my own op, mid-chapter
> The first `env` printed `ProStructuresNet=1.0.6045.20863` next to the manual's `8.11.3.48`
> and called it a comparison. **It is not one.** An assembly has **two** versions and they are
> different numbers: `GetName().Version` is the **assembly** version (1.0.6045.20863) and
> `FileVersionInfo.FileVersion` is the **product** version (08.11.11.161) — and only the
> second is comparable to the manual's. Fixed; both are now printed, side by side and labelled.

---

## ⭐⭐ The finding that came out of trying to stamp the build into the drawing

The plan was small: write the build into the strip's grid via `propset`, so the drawing carries
the version it was made with. It **failed silently** — `stuck=0 ignored=2`.

Tested one field at a time, on a `Ks_Grid` and on a `Ks_Shape` for contrast:

| field | on a `Ks_Grid` | on a `Ks_Shape` |
|---|---|---|
| `Note1` | ❌ discarded | ✅ sticks |
| `Name` | ❌ discarded | ✅ sticks |
| `Article` | ❌ discarded | — |
| `AreaClass` | ✅ sticks | ✅ sticks |

**All of them are reported `rw` by `propfull`, and `writeTo` returns `rc=0` in every case.**

⇒ **`rw` means only "the .NET property has a setter". It does NOT mean the value survives
`writeTo`.** Whether it does depends on the **part type**, and the API gives no signal at all —
same return code whether the field lands or is thrown away.

⇒ This corrects what I wrote in E.9's notes and printed in `propfull`'s own legend this
morning: *"rw = writable, and writeTo(oid) commits it"*. **Wrong, and now fixed in the op.**

⇒ And it makes `propset`'s read-back the point rather than a nicety: **it is the only thing
that distinguishes a field that works from one that is silently discarded.** Same lesson as
the plate's generated `Name`, the base plate's ignored numbers, and `Create()` returning true
having built nothing. The API's success codes describe the *call*, never the *effect*.

---

## The strip

**A.01-GENERAL-INFO**, x −3 000 … 53 000, on layer `_STRIPS` with the six part-A strips laid
out at the 60 000 pitch.

One specimen, `12A` — an HE300B carrying the chapter's own evidence in its `Note1`/`Note2`:
the build it was made on, and the fact that a shape accepts those notes while the grid three
metres away does not.

`env` output: `app/plugin/eb_env.txt`.
