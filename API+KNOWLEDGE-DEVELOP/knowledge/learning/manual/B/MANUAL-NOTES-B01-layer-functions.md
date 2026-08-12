# B.1 Layer Functions — chapter notes

*Read end to end 09/08/2026, pages 85–88 (fulltext lines 1904–2008).*

> *"The program is equipped with an **automatic layer control**. Normally you **don't have to take
> care of this**. If you use the program commands to create different objects such as shapes,
> dimensions, welding symbols, etc., these are created **on their own layer**."*

One command, `PS_LAYER`, with eleven parameters:

| parameter | effect |
|---|---|
| `LFRAMEON` / `LFRAMEOFF` | the work-area frame layer |
| `LELEMON` / `LELEMOFF` | the **main elements**: shape, roof/wall shapes, plates, construction lines, bolts |
| `LADDION` / `LADDIOFF` | the **additional** ones: dimensioning, midlines, position flags, relative heights, welding symbols |
| `LOBJECT` | the layer of a **clicked** element becomes current |
| `LOBJECTOFF` | the layer of a clicked element is switched off |
| `LCONSTAKT` | construction lines become current — ⭐ *(black lines)* |
| `LCONSTON` | construction lines on — ⭐ *(brown lines)* |
| `LCONSTOFF` | construction lines off |
| `LNULL` | AutoCAD layer `0` becomes current |

⭐ The colour note is the current-vs-visible distinction made visible: **current = black, merely on
= brown.**

---

# MEASURED 09/08/2026

## The manual's three groups map one-to-one onto real layers

| group | the manual's words | the actual layers |
|---|---|---|
| element | *shape, roof/wall shapes, plates, construction lines, bolts* | `PS_Shape` `PS_RoofWall` `PS_Plate` `PS_Const` `PS_Bolt` |
| additional | *dimensioning, midlines, position flags, relative heights, welding symbols* | `PS_Dim` `PS_Mid` `PS_Pos` `PS_Elev_flag` `PS_Weld` |
| work frame | — | `PS_Workfram` |

All present. Others in the table: `PS_Object`, `PS_Solid`, `PS_Hatch`, `PS_Hidden`, `PS_Text`,
`PS_Crash`, plus the ProConcrete `PC_*` family.
⇒ **The group switches are simply "turn these five on or off".** Their semantics are reachable by
layer name over COM (`Layer.LayerOn`) without the command.
⚠️ `PS_LAYER` itself is **not on the plugin's `CmdAllow`** list. That allowlist is a deliberate
safety control and was not widened for this — the group behaviour was reproduced by name instead.

## ⚠️⚠️ The automatic control follows the COMMAND, not the object type

Measured over everything built today — 88 parts were sitting on layer **`0`**:

```
Ks_Plate 69 · Ks_VolBody 14 · Ks_BendPlate 3 · Ks_ArcPlate 2
```

Every one of them created by calling a `Ps*Create*` class **directly**. Objects made by the shape
creator and by the connection classes landed correctly (`PS_Shape` 341, `PS_Bolt` 198,
`PS_Weld` 40, `PS_Workfram` 41).

⇒ **A direct API creator drops its part on whatever layer is current.** The consequences are all
real: `LELEMOFF` would not hide them, any layer-based filter or audit misses them, and they do not
take the layer's colour.

✅ **Fixed:** all 88 moved to their own layer — 74 → `PS_Plate`, 14 → `PS_Solid`, 0 failures.
Layer `0` now holds a single object, and that one is a `PcRebarManager` — a manager, not a part.

```
after:  PS_Shape 341 · PS_Plate 199 · PS_Bolt 198 · PS_Workfram 41 · PS_Weld 40 ·
        PS_Solid 14 · PS_Object 6 · 0 → 1
```

⇒ **Set the layer explicitly on every direct creation.** `plate9` already takes `layer=`; the
others need it passing too. Otherwise the model looks right and behaves wrong the moment anyone
switches layers.

---

# 🛑 CORRECTED BY THE AUDIT — 10/08/2026

**The conclusion above was the wrong fix, and the diagnosis was incomplete.** The strays were
not caused by "a direct API creator drops its part on whatever layer is current" as a fact of
the API. They were caused by **my own code calling `UseCurrentLayer(true)`**.

Measured with the new op **`layerprobe`** — three plates, one variable, the current layer
deliberately forced to a junk value so a right answer could not be luck:

| | call | landed on |
|---|---|---|
| A | `UseCurrentLayer(true)` | ❌ `ZZ_PROBE_WRONG` |
| **B** | **`UseCurrentLayer(false)`, no `SetLayer`** | ✅ **`PS_Plate`** |
| C | `UseCurrentLayer(false)` + `SetLayer("PS_Plate")` | ✅ `PS_Plate` |

⇒ ⭐⭐⭐ **B.1's opening sentence is true for the API too.** *"Automatic layer control… normally
you don't have to take care of this."* ProSteel assigns the part's own layer the moment you
stop insisting on the current one. **On 09/08 I fixed the symptom in the model and left the
cause in the code.**

**What was actually done (plugin v139 → v141):**

* Six call sites changed from `UseCurrentLayer(true)` to `(false)`. Verified against a wrong
  current layer, with **no `layer=` passed**: `plate` → `PS_Plate` · `polyplate` → `PS_Plate` ·
  `beam` → `PS_Shape`.
* ⇒ **`layer=` is an OVERRIDE, not a requirement.** Do not add it to ops that do not need it.
* ⚠️ **Solids are the real exception.** `PsCreatePrimitive` exposes **neither `SetLayer` nor
  `UseCurrentLayer`** — it cannot be told where to build. `solid` therefore assigns the layer
  **after** creation, defaulting to `PS_Solid`. Verified: `box`, `sphere` → `PS_Solid`.
* Ten other ops still take no `layer=`; that is now believed harmless but is **assumed, not
  measured** — probe each with `layerprobe` when its chapter is audited.
