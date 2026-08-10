# A.4 — Input Options

*Manual pp. 33–38, three sub-sections. Read 10/08/2026. Strip **A.04**, x 177 000 … 233 000.*

A catalogue of keyboard and mouse modifiers — on paper the most "ceiling" chapter in the book,
since every item needs a hand on a key. It is worth reading anyway, because **three of its
entries explain behaviours that have cost real work**, and one of them turns out to have a
settings equivalent I can reach.

---

## ⭐⭐ A.4.2 — `RETURN` at a selection prompt selects EVERYTHING

> *"In most of the functions, and in the case you want to select all parts, you can **answer the
> selection with RETURN**. Then, **all parts of the current drawing will be selected**."*

⇒ **This finally explains a rule recorded empirically weeks ago:** *recovery from a parked macro
prompt is ENTER, not ESC.* A macro stuck waiting for a selection is not being cancelled by
ENTER — it is being **given a valid selection** (everything), so it completes instead of
hanging. The rule was right; the reason was unknown.

And the other half of the same page:

> *"When you interrupt the selection with **ESC and SHIFT**, the **filter** function is selected
> and **the function is not left**."*

A filter mid-command — e.g. *all parts longer than 2000 mm* — with the original command then
carried out on the filtered set. Documented under `PS_SEARCH`.

## A.4.1 — the input-field extensions

`Pick Length` · `Pick Length without Z` · `Pocket Calculator`, from the right-click menu.
⚠️ **Only in fields taking a length, distance or coordinate** — *"not available for fields where
you enter texts, angles, or general digits such as factors or scaling values."*

---

## ⭐⭐⭐ A.4.3 — `ALT` suppresses the LINK UPDATE

The one line in this chapter that matters most:

> **General** — *"The pressed **ALT-key avoids working off the link update**. No updates due to
> modifications are carried out. You can e.g. use this function to **move parts without causing
> a reaction of the connected parts**."*

**That is the mechanism behind every re-run that has cost me work:**

| what happened | when |
|---|---|
| a section change on a connected beam **destroyed two bolts** and orphaned their holes | E.9, today |
| cloning a detail made its base-plate connection **re-run in every copy** (+4 anchors each) | lesson 5 |
| a rebuilt apex left **four orphan bolts** at the old coordinates | B.26, found today |

Not a quirk — the documented default. A.3.1 calls it **dynamic mode**: *"each modification of a
parameter is directly translated into a modification of the corresponding object"*, and says it
*"may be reasonable to deactivate this automatic update in the global settings"*.

### And the switches are reachable

`Ks_ComGlobalSettings` (found in A.2) carries them. **Read on this installation, nothing
changed:**

| setting | value |
|---|---|
| `DynamicConnections` | **True** |
| `LinksActiv` / `LinksActivUpdate` | **True / True** |
| `LinksPassiv` / `LinksPassivUpdate` | **True / True** |
| `LinksActiveUpdateInt` / `LinksPassiveUpdateInt` | 2 / 2 |
| `RecalcWhenNeeded` | False |
| `BlockStructureObjectsUpdate` | False |
| `DeleteUselessLinks` | True |

⇒ **Every connection re-runs on every change, right now.**

> ⛔ **Not changed, and not to be changed without Amir.** These are global settings on his
> working installation — they alter how the software behaves for him too, not just for the
> agent. The pattern that would be safe is the one already used for the UCS: **toggle → do the
> one operation → restore in a `finally` block**, so nothing can be left switched off. That is a
> proposal, not something done.

## ⭐ The other ALT / CTRL entries worth keeping

| key | where | what it does |
|---|---|---|
| **`CTRL`** | **Bolted Joint** | *"the bolted joint command **saves the last created bolt**… the stored data are deleted and the program is forced to **read the bolt data from a file**"* — ⚠️ **there is a bolt CACHE.** A possible lead on `boltparts` refusing geometry that measures perfect. |
| `CTRL` | Modify Properties | modify **exactly one** part — the counterpart of E.9's *"the first selected object sets the filter"* |
| `CTRL` | NC-Data | **debug mode**, with graphical output |
| `CTRL` | Positioning | switches off the subsequent recalculation |
| **`ALT`** | **Edit Drill Holes** | *"cancel the **blocking of bolt fields**… These then can be **deleted** as well. **Use this option carefully!**"* ⇒ ⭐ holes belonging to a bolt field are **blocked** against deletion — which is why *"holes cannot be removed"* looked true until `DeleteHoleField` was found today |
| `ALT` | Connect | *"avoids **type check** and everything is connected"* |
| `ALT` | Haunch | on the connection shape → a ceiling joist; on the supporting shape → **plate thicknesses adapt to the support** |
| `ALT` | Osnap | reference points become snap targets |
| `ALT` | Insert Shapes | a pick rotates +90°, ALT rotates **−90°** |
| `ALT` | Bolt Grips | bolt endpoints move **freely in space** rather than along the bolt axis |
| `ALT` | Hangar Frame / Lattice Girder | width taken **from the picked points** instead of the typed value |

---

## The strip

**A.04-INPUT-OPTIONS**, one specimen (`17F`) carrying the two findings in its notes. There is
nothing here to model: the chapter is entirely input behaviour, and its value is in what it
explains rather than what it builds.

## Carried forward

* ⭐⭐ **Ask Amir about suppressing the link update** for the duration of an edit. It would have
  prevented three separate incidents, and the settings are already located.
* ⭐ **The bolt cache** (`CTRL` at Bolted Joint) — a lead worth following when `boltparts`'
  unexplained refusal is next picked up.
