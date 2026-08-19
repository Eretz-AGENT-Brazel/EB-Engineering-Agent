# ⛔ LETHAL CALLS — these kill AutoCAD outright

*Opened 10/08/2026, during the part-B audit, when the second one was found. The first was
recorded inside B.9's chapter notes and stayed there; a second member makes it a **family**, and a
family needs one place to look before calling anything unfamiliar.*

**Two signatures, not one** *(corrected 13/08/2026 — the fifth entry hangs instead of dying)*:
1. **DEAD** — the process disappears. **No exception, no error dialog, no `EB_ERR`**; the plugin
   never returns, Python reports `EB_TIMEOUT`, and `Get-Process acad` comes back empty. Anything
   unsaved is gone.
2. **SPINNING** — the process stays listed with `Responding=False`, **CPU pinned at a full core
   and memory flat**. It does not recover; the disk file is untouched.

⇒ **`EB_TIMEOUT` alone tells you neither.** Check `Get-Process acad` for *both* existence **and**
`Responding`/`CPU` before deciding whether to wait, kill, or look for a dialog.

---

## The list

| call | class | found | how it was proved |
|---|---|---|---|
| **`computeObjectWeigth(bool)`** | `PsPlate` | B.9, 08/08 | reproduced twice; the second time with a marker file written immediately before the call, which survived reading *"about to run: weight on 501"* |
| **`checkHoleEdgeDistance(int)`** | `PsVolume` | **B.14 audit, 10/08** | isolated in stages — plate created **and saved**, hole drilled **and saved**, both survived; the call **alone**, on a saved model, killed the process |
| **`addUserXaxis(PsPoint, PsPoint)`** | **`PsGrid`**, on a grid bound through `PsTransaction.GetObject` | **B.23 audit, 10/08** | killed it **twice**. First with four calls in one run; then **isolated to this call alone**, on a freshly saved model, with `probe=addx` — dead again |
| **binding + reading a `PsEditConnection`** | `PsTransaction.GetObject(Int64, PsOpenMode, PsEditConnection&)` | **B.27 audit, 10/08** | **first call**, on beam `15EE`. Not isolated: bind-then-read is one call, so which half is the killer is unknown |
| ⏳ **`SetFileName("*.mdb")`** | **`PsDBaseDatabase`** (op `dbase`) | **lesson 6, 13/08** | ‏`Data\Bolts\Australia.mdb` (479 KB). **A HANG, not a crash** — the process stays listed, `Responding=False`, **CPU pinned at a full core** and memory **flat at 610 MB for 2 minutes** (an endless loop, not a slow parse). Killed and relaunched. **Guarded in `eb_api.run`: `dbase` now refuses any file that is not `.dbf`** |
| **`GetPolygon(PsPolygon3d)`** | **`PsBendShape`** (op `bendshapeinfo probe=path`) | **19/08/2026, model 5** | isolated **by name** with the `plateinfo` probe pattern. `probe=safe` (Key/Katalog/CrossSectionType/offsets/axes) and `probe=ref` (`GetReferenceLine`) both returned and left the process alive; `probe=path` -- `GetPolygon` plus `GetVertexPoint`/`getVertex` -- killed it, `Get-Process acad` empty. ⭐ **The suspect was wrong:** I had bet on `ComputeObjectLength()` because this file says `compute*`/`check*` are the family, and pre-marked it as the culprit. Only the isolation found the truth |

### ⏳ THE FIFTH ONE IS A NEW SHAPE: IT HANGS INSTEAD OF DYING

`PsDBaseDatabase` reads **dBASE (`.dbf`)**. Handed an **Access `.mdb`** it never returns.
⇒ ⭐ **The signature at the top of this file — "the process disappears" — is no longer the only
one.** Two failure shapes now: **dead** (`Get-Process acad` empty) and **spinning**
(`Responding=False`, CPU at 100 % of a core, memory flat). Tell them apart before deciding what
to do: a spin will not recover, so waiting is wasted; and unlike a crash it leaves the disk file
untouched, so a kill costs only the session.
⚠️ **What made this cheap:** the model had been **saved and verified two minutes earlier**
(139,923 bytes), so the kill cost nothing. That is the protocol below, and it paid again.
⚠️ The bolt/section databases are `.mdb` and there is **no ACE/Jet provider on this machine**
(re-checked 13/08: `SQLOLEDB, MSDataShape, Csp, ADsDSOObject, Windows Search, MSDASQL, MSDAOSP`
— none can open Access). ⇒ **A bolt table is measured by building specimens, not by reading the
database.** The measured result: [[BOLT-STYLES-AND-HOLES]].

### ⚠️ The third one is a different shape from the first two, and that matters

`addUserXaxis` is **not** a `check*`/`compute*` method. It is an ordinary-looking mutator on an
ordinary-looking object. What it has in common with the other two is the *route*: a managed
wrapper reaching into native code that expects a context the .NET caller has not established.

### 🛑 And the FOURTH one corrected the rule the third one produced

B.23 wrote: *"⭐ **Reading** a `PsTransaction`-bound object **is safe** — `PsGrid`,
`PsGussetConnection`, `PsPlate` and `PsShape` all read back correctly, repeatedly. **Writing**
through one killed the session on the first attempt. ⇒ treat every **mutator** as suspect."*

**`PsEditConnection` is a plain READ, and it is lethal.** Three safe types were three data points,
not a law.

> ⇒ ⭐⭐ **SAFETY IS PER TYPE, NOT PER OPERATION.**
> Read-safe **so far**: `PsGrid`, `PsGussetConnection`, `PsPlate`, `PsShape` — measured repeatedly.
> Read-**lethal**: `PsEditConnection`.
> Everything else among `GetObject`'s 57 overloads is **unknown**, and each answer costs a crash.

⇒ ⚠️ Untested mutators on `PsGrid` alone: `addUserYaxis`, `deleteUserXaxisAt`,
`deleteUserYaxisAt`, `setAxisLength`, `createAxisDescriptions`, `insert`.
**`getUserXaxis` / `getUserYaxis` were never reached** — the add died first, so **their status is
UNKNOWN, not safe.**

---

## What they have in common

Both are **compute/check methods on the entity classes** — not creators, not property reads.
They are the API's own "work out something expensive about this part" calls, and both go straight
through the managed wrapper into native code that expects a context the .NET caller has not set up.

⚠️ ⭐ **Therefore: treat every `check*` / `compute*` method on `PsVolume`, `PsPlate`, `PsShape`
and `PsBaseObject` as suspect until proven otherwise.** Neighbours of the two known killers,
untested and to be approached the same way:

```
PsVolume.checkDoubleHoles(Int32)
PsVolume.checkValidHoleFields(Int32, Int32)
PsVolume.computeHoleField(Int32, Int32, DrillAcuracy, Boolean, Boolean)
PsBaseObject.checkLogicalLinks()
PsBaseObject.containsParametricValue(Double, Int32)
PsPlate.computeObjectWeigth(Boolean)          <- known lethal
```

## The protocol for testing a suspect call

Paid for twice. Follow it:

1. **Save the model first.** Not "recently" — immediately before.
2. **Isolate.** One call, in its own run, with nothing else in the script. The first B.14 attempt
   ran `plate9 → drill → edgecheck` twice in a loop and proved nothing about which one died.
3. **Stage and save between stages,** so a survival is evidence too.
4. **Check `Get-Process acad` afterwards** — `EB_TIMEOUT` alone does not distinguish a hung
   session from a dead one.

## Recovery, when it happens anyway

### ⭐ CORRECTED 10/08/2026 — pass the DRAWING, not a template

The procedure below used to launch with `/t <template>`, which creates a **new** `Drawing1` — and
a new drawing makes ProStructures raise a modal **"Measurement Unit"** prompt (*"this setting is
persistent and cannot be changed at a later date"*). That dialog blocks everything, and it
**cannot be dismissed programmatically**: `BM_CLICK` and `WM_COMMAND`/`BN_CLICKED` to its Metric
button were both ignored, six attempts. The session had to be killed and started again.

**Put the drawing on the command line instead. Opening an existing drawing never asks.**

```powershell
Stop-Process -Name acad -Force
Start-Sleep -Seconds 5
$acad = 'C:\Program Files\Autodesk\AutoCAD 2015\acad.exe'
$arg  = 'C:\Program Files\Bentley\ProStructures Ss6 R1\AutoCAD 2015\Prg\ProStructures_SS6.1ACAD_E001_409.arg'
$dwg  = 'C:\Users\User\Desktop\EB PROSTEEL AGENT\projects\SANDBOX\B08-insert-shapes.dwg'
$wd   = 'C:\Program Files\Bentley\ProStructures Ss6 R1\AutoCAD 2015\Dwg'
Start-Process $acad -ArgumentList "`"$dwg`"",'/p',"`"$arg`"",'/ld','ProStructuresLoader.arx' -WorkingDirectory $wd
Start-Sleep -Seconds 75
```

That is the **ProStructures shortcut's own** profile and loader — anything else starts plain
AutoCAD with no ProSteel. Then `enforce_metric()`, `_netload()`, `ping`, and a census.
**No second document is created, so nothing has to be closed.**

### The dialogs that appear afterwards, and what to do with each

| dialog | do |
|---|---|
| **`AutoCAD Error Report`** | `WM_CLOSE`. ⛔ **Never press *Send Report*** — that transmits data to Autodesk |
| **`Error Report - Cancelled`** | follows the above; click **OK** |
| **`Drawing Recovery`** | click **Close**. ⚠️ **Do not recover** — the disk file was saved immediately before the lethal call and is the good one; an autosave is older |
| **`Measurement Unit`** | ⛔ **avoid it entirely** by passing the drawing, as above. It cannot be closed from code |

⚠️ **`Get-Process acad` is not the test.** The third lethal call left the process alive and
`Responding=True` while the op never returned. **Check `modal_dialogs()`** — an
*"AutoCAD Error Report"* window means it died even though the process is still listed.

---

## What this costs, and why the list exists

The B.14 crash lost nothing, because the model had been saved between stages. The **B.9** crash
lost five plates. The difference was entirely the protocol above.

⇒ **Before calling any unfamiliar `check*`/`compute*` method, look here first.** Related:
[[THE-CEILING-what-code-cannot-reach]] — that file is about what the API *refuses* to do; this
one is about what it does *to you*.
