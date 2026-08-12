# A.2 — Language Selection

*Manual p. 19. Read 10/08/2026. Implemented in
`A-dialogs-templates-settings.dwg`, strip **A.02**, x 57 000 … 113 000.*

Half a page, and it is not about menu labels.

---

## ⭐⭐ The language selects a whole CONFIGURATION, not a translation

The manual is explicit about what changes, and it is far more than the interface:

> *"If you switch over e.g. from German to English, **nothing will happen first** in the running
> mode **except the language of the dialogs**. However, if you start ProSteel anew … the
> **configuration** of the language setting will be **loaded as well** … This means that a plate
> will not be created with the name **Blech** but with the name **Plate**."*
>
> *"The **search paths** of the program for **blocks, temp- and varia-directories** will change
> as well and **another ProSteel configuration** will be available."*

Two consequences that matter to us:

1. **The names parts are GENERATED with are language output.** E.9 established that a plate's
   `Name` cannot be written — it is generated from the dimensions. A.2 says where the wording
   comes from: **the language configuration.**
2. **Templates, blocks and temp live under the language.** Which means the templates I have
   been calling by name all week are **files inside a language-specific folder**.

---

## Measured — and the language is literally in the path

⭐⭐ **The whole of A.6's settings dialog is reachable as an object**, and this is how it was
found. `Ks_ComGlobalSettings` is an **in-process** COM server: `Dispatch()` from outside fails
with *"Invalid class string"*, but AutoCAD will hand it over:

```python
acad = GetActiveObject('AutoCAD.Application')
gs   = acad.GetInterfaceObject('PSCOMWRAPPER.Ks_ComGlobalSettings')
```

Read out of it, on this installation:

| | |
|---|---|
| `BlockCenterPath` | `…\Localised\`**`English`**`\UserBlocks` |
| **`TemplatePath(True)`** | `…\localised\`**`english`**`\Varia\`**`Metric`** |
| `TemplatePath(False)` | `…\localised\english\Varia` |
| `TempPath(True)` | `…\localised\english\Temp\Metric` |
| `ApplicationPath` | `…\autocad 2015\prg` |
| `CombinationsShapePath` | `…\Data\CombiShapes` |
| `GetDataPath(0/1/2)` | `Data\` · `Data\Shapes` · `Data\Bolts` — 3 … 23 all fall back to `Data\` |

**The word `english` is inside the path.** And `Localised\` on disk holds **five** sets:
`Australia · Deutsch · English · NewZealand · USA_Canada`. Switching the language swaps that
folder — and with it the templates, the blocks and the generated names.

⭐ **`AllowUnits=True` adds `\Metric`.** So the configuration is keyed on **language × unit
system**, not language alone. On a metric-only shop that is one fewer thing to get wrong, but
it is worth knowing the Imperial tree exists beside it.

### The claim, tested in the model

A plate built in strip A.02 comes back named **`PLATE 400x300x20`** — English, from
`localised\english`. Under `Deutsch` the same plate would be **`BLECH 400x300x20`**, exactly as
the manual says. The specimen (`13F`) carries that in its own notes.

---

## 🔑 What this opens for the next chapter

`TemplatePath(True)` → `…\localised\english\Varia\Metric`, and that folder is full of `.tpl`
files:

```
BasePlate.tpl 1 188      KsxBasePlate.tpl 8 308     KssStairs.tpl 55 509
BeamColumnWeb.tpl 828    KsxBeamBeamShear.tpl 748   KssTruss.tpl 322 335
BoxBeamSplice.tpl 450    KsxBoxBeamShear.tpl 654    CircularPlatform.tpl 1 030
```

⇒ **A.3.2's Template Manager is these files.** Its `IMPORT` / `EXPORT` buttons move them. So
the workflow proposed to Amir is concrete and not a hope: **he configures Eretz Barzel's
standard detail once in the dialog and saves it as a template → it lands here as a `.tpl` →
the agent loads it by name from code → and the file can be copied, backed up and version
controlled.** Carried into A.3.

---

## Not done, and why

**The language was not changed.** Switching it rewrites the search paths and, on the next
start, the whole configuration — on Amir's working installation. That is a settings change with
consequences well beyond this chapter, and it is his to make. The behaviour is documented and
the current state is measured; that is what the chapter needed.
