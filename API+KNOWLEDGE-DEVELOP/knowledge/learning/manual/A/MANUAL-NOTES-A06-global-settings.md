# A.6 — Global Settings

*Manual pp. 45–68+, **776 lines — the largest chapter in part A**. Read 10/08/2026.
Strip **A.06**, x 297 000 … 353 000.*

The defaults for everything. A.6 says so itself, and it is the sentence that ties this chapter
to E.9, closed this morning:

> *"Identical values for the corresponding ProSteel-components resulting from these settings
> **can be modified at any time for each part** via the 'Change PS Properties' command."*

⇒ **A.6 sets the defaults; E.9 is the per-part override.** Two halves of one coin, and until
today I had only learned one.

Reached with **no dialog** through the object found in A.2:
`GetInterfaceObject('PSCOMWRAPPER.Ks_ComGlobalSettings')`. **Everything below was read.
Nothing was written.**

---

## ⭐⭐⭐ The plate name — E.9's mystery, solved across three chapters

E.9 found that a plate's `Name` cannot be written: set it, `writeTo` returns 0, and the name
comes back unchanged as `PLATE 400x300x20`. A.6 explains why.

| A.6 field | what it is |
|---|---|
| **`Description of Name`** | *"the **format default** of the plate name as it is used e.g. at labelling **and in the property fields**"* |
| Format structure | *"names like e.g. **`$(N)`** serve as **variable** for the current value… The 'Name' of the plate is the **constant** part like plate, grating, etc."* |
| `Position Flags` / `Export` | separate formats for flags and for parts-list export |
| **`Desired Plate Name`** | the name used for **PPS / NC output** — ⚠️ *"Keep this name later, otherwise there will be **compatibility problems with output**"* |
| `Round at…` | rounds the dimensions **in the name only** — *"doesn't have any influence on the actual dimensions"* |
| **`Length greatest…`** | *"The **greatest** value of dimensions is always regarded as plate **length**. This doesn't depend on how you inserted the plate."* |

**Read off the running installation:**

```
PlateNameTemplate        '$(N) $(L)x$(W)x$(T)'
PlateNameBigValue        True          <- "length greatest"
PlateTextName            True
PlateRoundNameTo         1.0           (length/width/height each 1.0)
```

⇒ **`PLATE 500x250x12` is that template rendered.** The name is not stored — it is
**re-rendered every time**, which is exactly why writing it does nothing.

### The chain, across three chapters

| | |
|---|---|
| **A.2** | the **language configuration** supplies the constant `$(N)` — `PLATE` in English, `BLECH` under Deutsch |
| **A.6** | `PlateNameTemplate = '$(N) $(L)x$(W)x$(T)'` renders it with the live dimensions |
| **E.9** | therefore `Name` on a plate is **read-only in effect**, and `Note1`/`Note2`/`Posnum` are where a label belongs |

---

## ⭐⭐ Logical Links — the four modes, and why everything re-runs

A.6's Logical Links page offers exactly:

| | |
|---|---|
| `Create Passive` | active *and* passive links referencing the parts are created |
| **`No Update`** | *"Any changes made to a component are **not passed on**"* |
| **`On Request`** | *"You are **prompted**"* |
| **`Automatic Update`** | *"changes … **immediately passed on** to its associated components"* |

**Read on this installation:**

```
LinksActiv  True    LinksActivUpdate  True    LinksActiveUpdateInt  2
LinksPassiv True    LinksPassivUpdate True    LinksPassiveUpdateInt 2
DynamicConnections  True     BlockStructureObjectsUpdate False     DeleteUselessLinks True
```

⇒ **`…UpdateInt = 2` is Automatic Update, on both active and passive links.** That is the
documented cause of the two bolts destroyed by a section change (E.9), the base plate that
re-ran in every clone (lesson 5), and the four orphans at B.26's apex. A.4's `ALT` key is the
per-operation escape; these settings are the global one.

### And two more that explain silent refusals

* ⭐⭐ **`Lock Part Properties`** — *"The part properties created **under the control of a
  logical link** can only be **read but not edited**."* ⇒ A part governed by a connection has
  its connection-owned fields **locked**, and the API reports success anyway. Another member of
  the family that includes the plate's generated name.
* ⭐⭐ **`Allow additional data`** — *"You can enter additional data of logical links **e.g. an
  identification code**."* ⇒ **This is E.9.17's "Logical Links / Extended Input"** — the switch
  that makes a link's `Ident` and `Name` exist at all. They read empty because it is off.
* **`Structural Elements…`** — suppresses the automatic update of bracing, staircases and the
  rest.

---

## ⭐ Two numbers worth having

```
MaterialSpecificWeight(0..3)   7850.0      [4] 2700.0   [5] 3500.0
PlateRasterWeightReduction     10.0
```

* **7850 kg/m³ — ProSteel's steel density is exactly the Eretz Barzel factory rule.** A company
  constant, confirmed against the software rather than assumed. (2700 = aluminium.)
* **`PlateRasterWeightReduction = 10 %`** — the weight allowance for **grating** plates, i.e.
  the `Grid` flag found on the plate's Layout tab in E.9.2. A grating is billed at 90 % of a
  solid plate of the same size.

Also there: `Plates…Calculation` — the weight and paint area can be computed *"according to the
exact form, according to the **rubber tape-method** (as if a rubber tape was tightened around
the plate)"*, and that choice **changes the parts-list output**. ⇒ A weight discrepancy between
two models can be a *setting*, not an error.

## Revision Check — the tolerances that decide "changed"

*"Here, you specify the tolerances to be used for comparing the component parts… line length as
well as the comparison of drill holes, lengths and weight. Modifications are determined by
means of a **type of comparison which is also used for positioning**."*

⇒ That is the same comparison behind `CheckTwoPartsAreEqual` — the only identity test that
sees cuts. **Its tolerances are settings**, which means "are these two parts the same" has a
configurable answer.

`Single Parts / At Changes` — when a positioned part changes, its number is **Keep** /
**Confirm** / **Delete**.

## The rest of the dialog, in one line each

`Options` (general) · `Grips` — ⚠️ *"when no grip is entered for an object, the AutoCAD command
**Stretch** cannot be used"* · `Shapes` · `Straight Plates` · `Bolts` · `Work Frame` ·
`Assembly` · `Values` — resolution of circular volumes for the facet modeller (`ArcResolution
31`, `BodyResolution 31`, `BoltResolution 23`) · `Display` · `Colours` · `Monitor` ·
`Configuration` — iso-views, layers, flat-steel tables · `Dialogs` ·
**`Files`** — *"only visible in **expert mode**"*.

---

## What was NOT done

⛔ **Nothing was written.** Every value above was read. These are global settings on Amir's
working installation: they change how ProSteel behaves for him, not only for the agent.

Two are worth proposing, and both are his call:

1. **`Allow additional data`** (extended link input) — it costs nothing and would make a
   connection's `Ident`/`Name` readable, which is the missing half of `connscan`.
2. **Suppressing the link update around a single edit** — `LinksActivUpdate` / `LinksPassiv‑
   Update` / `DynamicConnections`, in the UCS pattern: **toggle → one operation → restore in a
   `finally`.** It would have prevented three separate incidents.

## The strip

**A.06-GLOBAL-SETTINGS**, two specimens: a plate (`PLATE 500x250x12`, the template rendered)
and a shape carrying the findings in its notes.
