# THE PARTS LIST, FROM CODE — and the empty file that looks exactly like a full one

*Opened 21/08/2026. `partlist` in the Python client fired ProSteel's own `_PS_CREATE_PARTLIST`,
a dialog, so the output chain ended wherever a human was sitting. It does not have to.*

---

## The op

`Bentley.ProStructures.Miscellaneous.PsCreatePartlist`:

```
GetPartlistTemplateNames()  -> ArrayList          read-only, and the only way to learn the
                                                  legal template names (here exactly one:
                                                  "Default/Standard")
CreateMDBFile(file, template, PsSelection) -> bool
PerformPartlist2(listfile, formatfile, single, KsPartlistAuto, target)
SetTolerances(len, wid, hgt, wgt)   SetCompareFilter(n)   SetToDefaults()
```

Wired as `op=partlist` in v200:

```
partlist templates=1                                 -> the legal names -> eb_partlist_templates.txt
partlist out=<file.mdb> template=<name> [box=...] [tol=l,w,h,wt] [filter=n]
```

**`CreateMDBFile` does not open a dialog.** Measured: the sandbox (16 objects) answered in
**1.1 s**; the bridge rebuild (19,719 selected parts) in **~500 s** and wrote **17.6 MB**. The
rows are real — `M 14x35 DIN912` ×263, `M 27x70 DIN6914` ×10, and a full BOM line reads

```
BRFL 200x10 | 200X10 | DIN.DIN_FLACHP | 4 | S355JR | SP=15623_base-plate | DIN 1025-2 HE 300 B | P4
```

⚠️ **Read an MDB as UTF-16, not ASCII.** Jet stores its strings UTF-16LE, so an ASCII scan of a
17.6 MB parts list finds 652 fragments of noise and looks like an empty file. The same file
scanned as UTF-16 has 19,834 strings. I called a good file empty once on that mistake.

## ⭐ A parts list reports only parts that carry a POSITION NUMBER

The same selection, the same call, before and after `posauto`:

| | bytes | part rows |
|---|---|---|
| before | 122,880 | **none** |
| after | 147,456 | real ones |

`CreateMDBFile` returned **true both times.** So an unnumbered model produces a valid,
plausible, EMPTY parts list — and the byte count cannot tell you, because the empty file is a
complete Access schema, 122,880 bytes of it. ⇒ **the output chain is `posauto` → `partlist`,
in that order**, and a size check is not a witness.

v201 therefore counts the numbered parts and reports `numbered=` in the result line, with
`[bytes is NOT a witness -- an empty list is a full Access schema]` spelled out.

## ⚠️ What is NOT yet trustworthy here

- **The `numbered=` count itself.** `PsSelection` has no indexer — it walks by callback,
  `DoForAll(PropertyAction)`, which hands each part's `PsObjectProperties` straight in. On the
  sandbox that walk answers 0 while `posauto` reports 10 written, and `props` also shows no
  position on those parts, so the two agree with each other and disagree with posauto. Until
  that is resolved on a model with *known* numbering, **`numbered=0` must be read as
  "unverified", not as "refuse"** — a guard that blocks legitimate work is worse than no guard.
- **`box=`** (`SelectAllObjectsInRange`) returned an EMPTY selection for a range that certainly
  contains parts, so its coordinate convention is not what I assumed. Untested = unusable.
- `PerformPartlist2` and the `KsPartlistAuto` modes (`kPartlistPrinter`, `kPartlistPreview`,
  `kPartlistExport`) are unexercised. `kPartlistExport` is the one to try for a non-MDB export.
