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

## ⭐ How to READ an MDB on this machine

64-bit Python has no OLEDB provider installed. **32-bit PowerShell does:**

```
C:\Windows\SysWOW64\WindowsPowerShell1.0\powershell.exe
   Provider=Microsoft.Jet.OLEDB.4.0;Data Source=<file.mdb>
```

Which turns guesswork into a row count. The file holds two tables, `DwgHeader` (1 row) and
**`Partlist`**, and `Partlist` carries the whole fabrication schema — **127 columns**:

```
_NAME _COUNT _KEY _CATALOG _POS__NUMBER _SHIPPING_NO_ _MATERIAL _LENGTH _WIDTH _HIGH
_WEIGHT _SECTAREA _PAINTAREA _LEN_ADDITION _CONGROUP _INSERTX _INSERTY
_PHI_Start _PHI_End _PHI_ST_A/E _PHI_FL_A/E        <- the end-cut angles
_DetDwgNr _DetDwgIdx _DetDwgName _OrigPos _ShapeClass
_NumHoles _NumCuts _NumPCuts _NumOutlets _NumBodies _NumFacets
_Shape_depth _Flange_With _Flange_Thickness _Web_thickness _Clearance _Price
_Coating _BoltDia _Grip_length _Mirrored _Handle _COGX _COGY _COGZ _SPEZWEIGHT ...
```

A row reads `Tube Rond 219.1x4 | 1 | RO219.1X4 | FRANCE.FR_TUBE_ROND | P418 | S235JRG2 | ...`.
⇒ **everything a workshop needs is reachable from code**, including the per-part hole and cut
counts and the centre of gravity.

## ⚠️ What is NOT yet trustworthy here

- **ROW COMPLETENESS IS UNSOLVED, and this is the open end.** On the same drawing, the same op
  wrote **17.6 MB** at 14:29 and **166 rows** every time since. The cause is the selection: with
  the FIRST boolean true the `Partlist` table comes out EMPTY (`1,0,0`, `1,1,0` and `1,1,1` all
  did, the last of them while selecting 19,897 parts), and `0,1,1` is the only mode measured to
  write rows at all — 166 of them, for a model with 14,062 numbered parts. Worse, the mapping is
  not stable across runs: `(true,false,false)` answered 19,719 at 14:29 and 386 at 14:42, same
  drawing, same active document, verified by COM. ⇒ v205 defaults to `sel=0,1,1`, reports
  `parts=<n>/<entities>` and `numbered=`, and **the file must be read back with the Jet provider
  before any list is believed.**
- **`box=`** (`SelectAllObjectsInRange`) returned an EMPTY selection for a range that certainly
  contains parts, so its coordinate convention is not what I assumed. Untested = unusable.
- `PerformPartlist2` and the `KsPartlistAuto` modes (`kPartlistPrinter`, `kPartlistPreview`,
  `kPartlistExport`) are unexercised. `kPartlistExport` is the one to try next, and it may well
  be the call that respects the whole selection.

### Refuted along the way

**"`DoForAll` consumes the selection it walks."** It looked certain — v200 had no walk and wrote
17.6 MB, v201 added a walk before `CreateMDBFile` and every list since came out empty. v204
moved the walk after the write, onto a selection of its own, and the list was **still empty**.
The walk was never the cause. ⇒ ⭐ two changes in one version is one experiment too few; the
version that "explains" a regression is a suspect, not a verdict.


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
