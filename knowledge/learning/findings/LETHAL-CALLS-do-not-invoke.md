# ⛔ LETHAL CALLS — these kill AutoCAD outright

*Opened 10/08/2026, during the part-B audit, when the second one was found. The first was
recorded inside B.9's chapter notes and stayed there; a second member makes it a **family**, and a
family needs one place to look before calling anything unfamiliar.*

**The signature is always the same:** the process disappears. **No exception, no error dialog, no
`EB_ERR`** — the plugin never returns, the Python side reports `EB_TIMEOUT`, and
`Get-Process acad` comes back empty. Anything unsaved is gone.

---

## The list

| call | class | found | how it was proved |
|---|---|---|---|
| **`computeObjectWeigth(bool)`** | `PsPlate` | B.9, 08/08 | reproduced twice; the second time with a marker file written immediately before the call, which survived reading *"about to run: weight on 501"* |
| **`checkHoleEdgeDistance(int)`** | `PsVolume` | **B.14 audit, 10/08** | isolated in stages — plate created **and saved**, hole drilled **and saved**, both survived; the call **alone**, on a saved model, killed the process |

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

```powershell
Stop-Process -Name acad -Force
Start-Process 'C:\Program Files\Autodesk\AutoCAD 2015\acad.exe' -ArgumentList `
  '/p','"C:\Program Files\Bentley\ProStructures Ss6 R1\AutoCAD 2015\Prg\ProStructures_SS6.1ACAD_E001_409.arg"',`
  '/t','Ps191_Metric','/ld','ProStructuresLoader.arx' `
  -WorkingDirectory 'C:\Program Files\Bentley\ProStructures Ss6 R1\AutoCAD 2015\Dwg'
```

That is the **ProStructures shortcut's own** profile, template and loader — anything else starts
plain AutoCAD with no ProSteel. Then reopen the drawing over raw COM (`eb_api._app_doc()` refuses
while the wrong drawing is open), close the empty `Drawing1`, and `NETLOAD` the plugin again.

⚠️ **A crash dialog may appear titled *"AutoCAD Error Report"*.** Close it with `WM_CLOSE`.
**Never press its send button** — that transmits data to Autodesk.

---

## What this costs, and why the list exists

The B.14 crash lost nothing, because the model had been saved between stages. The **B.9** crash
lost five plates. The difference was entirely the protocol above.

⇒ **Before calling any unfamiliar `check*`/`compute*` method, look here first.** Related:
[[THE-CEILING-what-code-cannot-reach]] — that file is about what the API *refuses* to do; this
one is about what it does *to you*.
