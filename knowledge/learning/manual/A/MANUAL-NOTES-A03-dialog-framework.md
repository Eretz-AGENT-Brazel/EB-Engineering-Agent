# A.3 — Dialog Framework (and the Template Manager)

*Manual pp. 21–32, five sub-sections. Read 10/08/2026. Implemented in
`A-dialogs-templates-settings.dwg`, strip **A.03**, x 117 000 … 173 000.*

**The most useful chapter in part A**, and probably one of the most useful in the manual for
what Amir asked me to be. It explains the mechanism behind the rule the whole week was built
on — *"choose a template, do not pass numbers"* — and it opens a route from **his hand** to
**my code**.

---

## A.3.1 — the buttons every ProSteel dialog has

`OK` · `CANCEL` · `HELP` · **`TEMPLATE`** · **`CLONE`** · **`UPDATE`** · `ROLL-OVER` · `GRAPHIC`

Two of them matter to us:

> ### ⭐⭐ `CLONE` — read the settings off an existing connection
> *"you can **read the settings of an existing connection and import it onto another one**.
> This function is especially suitable for automatic connections. It helps you to obtain an
> identical connection to an existing one, **the exact default settings of which, however, you
> don't know any more**."*
>
> That is the exact situation I have been in all week: a connection built from a template whose
> contents I could not name. ⚠️ **This is NOT B.4.5's `Clone`** — that one is the *modification*
> clone (`TakeoverDrills`), which 🛑 **RETRACTED 10/08/2026 — transfers, and was wrongly recorded
> here as doing nothing**. This is a **connection-settings** clone, a different thing with the same
> button name; the distinction between the two is the point of this note and still holds.

> ### ⭐ `UPDATE` — and the dynamic mode behind it
> *"Normally in ProSteel, you are working in a **dynamic mode**. Each modification of a
> parameter is **directly translated into a modification of the corresponding object**… In case
> of very complex structures or less powerful computers, it may be reasonable to **deactivate
> this automatic update in the global settings**."*
>
> That dynamic mode is why a connection **re-runs** when its inputs change — the behaviour that
> destroyed two bolts in E.9's section-change test and re-ran base plates on every clone in
> lesson 5. It is not a quirk; it is the documented default, and it has an off switch in A.6.

---

## ⭐⭐⭐ A.3.2 — the Template Manager

### What a template actually is

*"Many of these settings are repeated for construction tasks… it would be useful to save these
settings for later use."* A template is **the dialog's complete state, saved under a name**. The
manager works *"very similar to Windows Explorer"* — branches, drag & drop — and **the saved
entries depend on which dialog you opened it from**: the Work Frame command shows only work
frames, the Plates command only connection-plate settings.

### Where they live — measured, not guessed

`Ks_ComGlobalSettings.TemplatePath(True)` (see A.2) →
`…\localised\english\Varia\Metric` — and it holds **100 `.tpl` files**:

```
PsEndPlate.tpl    12 730     PsBasePlate.tpl    1 766     PsAnchorBolt.tpl   1 643
PsCope.tpl           990     PsStiffener.tpl    1 690     PsWebAngle.tpl     2 049
PSBRACING.tpl      7 602     PsGussetConnection.tpl 394   PsStaticBracing.tpl  793
PsFamilyClasses.tpl 21 653   PsAreaClasses.tpl  1 287     PsDisplayClass.tpl 3 548
PsHandRail.tpl    36 838     KssStairs.tpl     55 509     KssTruss.tpl     322 335
```

⚠️ **`PSBRACING.tpl` and `PsGussetConnection.tpl` exist** — the two things THE CEILING records
as having no working creator. Their *parameter sets* are shipped. That does not make the
creator work, but it is worth knowing the data is there.

**The format is not dBASE.** First bytes `ff ff 11 00 13 00` then the literal strings
`Ks_TreeMenuTemplate` and `Ks_TreeTemplate` — a Bentley serialised **tree**, which is exactly
the branch structure the manual describes.

### ⭐ The names finally decode: `branch/name`

Every template name the API hands back is **`branch/name`**, the slash being the folder
separator:

| | |
|---|---|
| `default/Standard`, `default/half convex`, `default/full chamfered` | the shipped defaults |
| **`example/example3`** | ← the one I have been passing to `conn kind=endplate` all week |
| **`AutoConnect Metric v 18/450x450x25`** · `…/600x600x25` · `…/2 Bolt` · `…/3 Bolt` | **a shipped METRIC connection library**, named by size or bolt count |

⭐ `AutoConnect Metric v 18` is precisely the *"template records similar in quality (e.g. only
with two variable dimensions)"* the dBASE section describes — a family generated as a table.

### ⚠️ The default templates use a DIFFERENT bolt standard

Read straight off the template list:

| template | bolt style |
|---|---|
| `default/Standard` (web angle, shear plate) | **`8.8S`** — an Australian bolt |
| `AutoConnect Metric v 18/2 Bolt`, `/3 Bolt` | **`DIN7990`** / `DIN7969` |

The model's own bolts are DIN7990 and DIN6914. ⇒ **`default/Standard` is not the neutral
choice it looks like.** Pick from the `AutoConnect Metric v 18` branch for DIN work.

### The company-standard workflow this makes possible

| A.3.2 says | what it means here |
|---|---|
| templates are saved as **permanent** settings in the **variants folder** | the `Varia` tree found above |
| *"available for **all users at the same time in a network** if the same folder has been set"* | **put `Varia` on a share and the whole office draws from one set of details** |
| the **last** settings are also saved per workstation, in a **temporary** folder | why "the last entry" comes back — and what `TempPath()` points at |
| **`EXPORT`** — *"made accessible to other users for import"* | a template is a file: back it up, version it, hand it over |
| **`BLOCK` / `UNBLOCK`** (expert mode) | **lock a company standard against being overwritten** |
| **version control** in the file structure | templates survive a ProSteel update; new fields take defaults |
| **dBASE import/export**, *"create data records using **Excel**"* | ⭐⭐ **generate a family of templates from a spreadsheet** |
| **favourites** | the short list that shows in the dialog |

⇒ **The bridge from Amir's hand to my code is real.** He configures Eretz Barzel's standard
detail once, saves it under a name, blocks it; it lands as a `.tpl` in `Varia`; I call it by
name from code, forever; and the file can be shared, backed up and versioned.

---

## What was built — the same column, two templates

Two identical HE300B columns in strip A.03, two base plates, **nothing passed but the template
name**. Read back off the model:

| | `default/Standard` | `AutoConnect Metric v 18/450x450x25` |
|---|---:|---:|
| plate L × W × t | 200 × 200 × **10** | **450 × 450 × 25** |
| hole ⌀ | 20 | 20 |
| anchor ⌀ / grip / drill / key | 18 / 50 / 185 / 25 | **0 / 0 / 0 / 0** |
| `detailed` anchors | 1 | 0 |

**Every number came from the template.** That is the whole mechanism behind the working rule,
demonstrated rather than inferred.

> ### ⚠️ And the trap that cost 10 points in lesson 4, now explained
> `default/Standard` carries **anchor dimensions** (⌀18, drill 185, `detailed=1`) but its
> **`AnchorBolts` flag is OFF**. So it produces the plate and the **holes** and **no anchor
> bodies** — which is exactly Amir's *"where are the nuts? it doesn't look realistic enough"*.
> The `AutoConnect` template has no anchor definition at all.
>
> ⇒ **Read a template's parameters before using it.** `conntemplates` / `stifftemplates` /
> `webangletemplates` / `shearplatetemplates` / `splicetemplates` print every field of every
> template. The template hides its own settings; those ops are how you look inside.

The two base plates leave **6 unfilled holes** between them — anchor holes with the anchor
switch off. Deliberate, and labelled as such on the specimens, exactly like E.9's bare hole.

---

## A.3.3 – A.3.5 — interface behaviour, all of it on the ceiling

* **A.3.3 RollOver** — dialogs fold up to their title bar when the mouse leaves and unfold when
  it returns. Per-dialog, or off globally in A.6.
* **A.3.4 Pick Frame ("monitor") and auxiliary graphics** — the picture on the right of a
  dialog. Where it is a *monitor*, the **small circles are reference-point marks and the active
  one shows red**. ⭐ That is what E.9.1's *"add / subtract up to 2 reference points"* refers
  to — the insertion reference is set by clicking the monitor.
* **A.3.5 Input extensions** — in most distance fields, the right-click menu offers
  **`Add Picked Length`** (and *without Z*) to take a dimension straight off the drawing, and
  **`Add Calculated Value`**, a pocket calculator.

All three are mouse-and-dialog features. They are worth knowing because they explain what a
dialog can do that code cannot — and because A.3.4 finally names the reference-point mechanism.

---

## Carried forward

* **`CLONE`** — reading a live connection's settings onto another. Worth a proper attempt when
  the connection editor is next opened; it is the missing "what is actually in this joint".
* **dBASE / Excel template generation** — the most promising unexplored route in the manual.
  ⛔ Not started: it writes into the shipped template set, and that is Amir's call.
* **Putting `Varia` on a network share** — an office-wide decision, his to make.
