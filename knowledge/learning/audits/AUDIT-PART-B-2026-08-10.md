# 🔍 Part-B self-audit — B.1 … B.29

*Commissioned by Amir, 10/08/2026:*

> *"אני קצת חושש מזה שבמקריות נגענו רק עכשיו בפרק A — ולא עבדנו על פי סדר הפרקים כמו שצריך…
> תעבור פרק פרק בתוך חלק B, החל מפרק B.1 ועד לפרק B.29, ותעשה בקרה על כל פרק ופרק בנפרד.
> …זהו פרק הליבה של פיתוח יכולות המידול שלך בתוכנה ובהקשר ישיר לזה פיתוח ה-API."*

Three questions per chapter, his:

1. **Is there anything I can improve?**
2. **Did I learn the chapter deeply, and do I have from it everything the API development needs?**
3. **If there is something to improve — do it, in the models I practised on.**

**Method, fixed before starting so it cannot drift:**

| step | what it means |
|---|---|
| **1 · re-read the manual** | the chapter's own text again, looking for what I never touched |
| **2 · re-read my notes** | what I claimed — and which of it was *measured* rather than assumed |
| **3 · inspect the model band** | is the implementation actually there, and correct |
| **4 · check API coverage** | which classes/methods the chapter implies, and which ops exist |
| **5 · fix** | in `B08-insert-shapes.dwg`, the band the chapter owns |
| **6 · verdict** | written, including what is still open |

**Model:** `projects/SANDBOX/B08-insert-shapes.dwg` — 834 entities at the start of the audit,
post-fix state from this morning's bolt audit (179 bolts, 0 iron-rule violations).

---

## Where the reading was thinnest — checked before starting

Notes lines ÷ manual lines. Not a quality measure on its own, but it says where to look hardest.

| chapter | manual | notes | ratio | |
|---|---:|---:|---:|---|
| **B.29 Positioning** | **878** | **125** | **0.14** | ⚠️ biggest chapter, thinnest notes |
| B.8 Insert Shapes | 564 | 153 | 0.27 | ⚠️ |
| B.5 Display / Assign | 367 | 109 | 0.30 | ⚠️ |
| B.3 3D Object Views | 333 | 102 | 0.31 | ⚠️ |
| B.12 3D-Modifications | 769 | 322 | 0.42 | |
| B.2 Construction Utilities | 153 | 64 | 0.42 | |
| B.17 Plate Connections | 446 | 208 | 0.47 | |
| B.28 Group Structure | 304 | 164 | 0.54 | |
| B.6 Work Frames | 437 | 242 | 0.55 | |
| B.15 Bolts | 340 | 213 | 0.63 | |
| B.13 Plate Editor | 159 | 102 | 0.64 | |
| B.14 Drilling | 326 | 218 | 0.67 | |
| B.1 Layer Functions | 105 | 71 | 0.68 | |
| B.9 Insert Plates | 378 | 268 | 0.71 | |
| B.24 Dynamic Bracing | 352 | 359 | 1.02 | |
| B.10 Insert Solids | 99 | 104 | 1.05 | |
| B.11 ACIS reference | 75 | 100 | 1.33 | |
| B.16 Stiffeners | 166 | 255 | 1.54 | |
| B.7 Choose View | 58 | 107 | 1.84 | |
| B.23 Gusset Plates | 86 | 198 | 2.30 | |
| B.18 · B.19 · B.20 · B.21 · B.22 · B.25 · B.26 · B.27 | | | 0.8–1.2 | |

---

# The chapters

---

## B.1 — Layer Functions ✅ **improved at the root**

*Manual 1904–2008 (105 lines) · notes 71 lines · band: none (B.1 is model-wide)*

### 1 · Was the chapter learned deeply?
**Yes.** The notes carry the eleven `PS_LAYER` parameters, the three layer groups mapped
one-to-one onto real layer names, the black-vs-brown construction-line distinction, and the
09/08 measurement that found **88 parts on layer 0**.

### 2 · Does the API development have what it needs?
**It did not, and the gap was of my own making.**

The notes ended with a TODO from 09/08 — *"`plate` already takes `layer=`; the others need it
passing too"* — and the audit found that **11 of 17 creation ops still could not take a layer
at all**, while the plate paths called `UseCurrentLayer(true)` unconditionally.

Before adding a parameter to eleven ops I asked the better question: **what does the creator do
if told not to use the current layer and given no layer either?** New op **`layerprobe`**, three
plates, one variable, with the current layer deliberately forced to a junk value so a correct
result could not be luck:

| | call | landed on |
|---|---|---|
| A | `UseCurrentLayer(true)` | ❌ `ZZ_PROBE_WRONG` |
| **B** | **`UseCurrentLayer(false)`, no `SetLayer`** | ✅ **`PS_Plate`** |
| C | `UseCurrentLayer(false)` + `SetLayer` | ✅ `PS_Plate` (control) |

> ### ⭐⭐⭐ The 88 strays were self-inflicted
> B.1's opening line — *"automatic layer control… normally you don't have to take care of
> this"* — **is true for the API as well.** The plugin was overriding it. I had fixed the
> **symptom** in the model on 09/08 and left the **cause** in the code for a day.

### 3 · What was fixed
* **Six call sites** changed from `UseCurrentLayer(true)` to `(false)` (plugin v139 → v141).
  Verified against a wrong current layer: `plate` → `PS_Plate`, `polyplate` → `PS_Plate`,
  `beam` → `PS_Shape`, **with no `layer=` passed at all.**
* ⭐ **`layer=` is now an override, not a necessity.**
* **Solids are a genuine exception.** `PsCreatePrimitive` exposes **no `SetLayer` and no
  `UseCurrentLayer`** — it cannot be told where to build. So `solid` now assigns the layer
  after creation, defaulting to **`PS_Solid`**. Verified: `box` and `sphere` land on `PS_Solid`
  against a wrong current layer.
* New op **`layerprobe`** kept — it is how this class of question gets answered, and it cleans
  up after itself (probe layer restored, probe parts erased).

### Model state
Unchanged and correct: 834 entities, every part on its own layer, layer `0` holding only a
`PcRebarManager`. **The 09/08 fix held.** All audit scaffolding was removed and the entity
count returned to 834 exactly.

### Still open
* ⚠️ Ten further ops (`stiffener`, `weld`, `bend`, `bendtwo`, `grid`, `workframe`, `frame`,
  `boltfield`, `boltparts`, `threadedrod`) still take no `layer=`. **Most are now harmless** —
  their creators respect ProSteel's automatic control — but that has been *verified only for
  plates, shapes and solids*. The rest are **assumed, not measured**, and should be probed the
  same way when their chapters come up.
