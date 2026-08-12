# A.5 — Project Manager

*Manual pp. 39–44, two sub-sections. Read 10/08/2026. Strip **A.05**, x 237 000 … 293 000.*

A.3.2 established that a template is a saved dialog stored in a folder. **A.5 says that folder
can belong to a project rather than to the workstation** — and that is the piece that turns a
template library into a way of working for a fabricator with many customers.

---

## ⭐⭐ Settings can be owned by the PROJECT, per file type

> *"Up to now, it was not possible to store the pre-settings, **templates** or even
> configurations **related to the project**. In the current version, a **project manager** has
> been implemented offering exactly these options."*
>
> *"You can choose for your **temporary, template, style and format** files whether these have
> to be managed **related to the project** or **related to the workstation**."*

And the manual's own worked example is a fabricator's:

> *"If you work e.g. with **detailing styles and formats related to the customer**, but use
> always the same settings and templates, you set the temporary and template files to **'not
> related to the project'**, the style and format files, however, **are related to the
> project**."*

⇒ **Company standard details stay global; the drawing look goes per customer.** Exactly the
split Eretz Barzel would want: the connections are the company's, the title block and
dimension style are the client's.

⭐ **`Copy from`** appears beside every project-local path: *"You don't have to work with a bare
installation, but you will find the usual environment, even if from now on it will only be
valid for this project."* ⇒ **seed the project's templates from the company standard, then let
that one job diverge without touching the standard.**

## What a project defines

| | |
|---|---|
| **Model Files** | ⚠️ *"it is **not possible to force AutoCAD** to use this path… you have to take care **manually**"* |
| **Detail Files** | the DetailCenter's preset |
| **Parts Lists** · **NC Files** · **PPS Files** · **Export Files** | each becomes the preset for its function ⭐ **NC is the CNC output** |
| **Temp Project** | the last dialog content and position, per project |
| **Templates Proj.** | ⭐⭐ *"all data … created via the template management"* |
| **Styles Project** | **all 5 style types**: detailing style, position flags, weld marks, elevations, bolts |
| **DWG Project** | drawing frame files |
| **Project Descr.** | 10 lines, shown when choosing a project |

⚠️ On the presets: *"only the **new** selection is concerned. If another project was activated
at the last selection… the path still is the one set for **that** project."* — a stale path
survives a project switch until the function is next used.

**`Load Project…`** re-activates the last project on start — *"the prerequisite is, however,
that ProSteel has been **quit as usual**"*. A crash loses it.

**DwgInfo across the project** — a project-wide drawing-information table each drawing can pull
from.

---

## Measured

`ProjectPath` is exposed on `Ks_ComGlobalSettings` and reads **empty**:

```
ProjectPath   (empty)          <- no project is loaded
DetailPath    …\AutoCAD 2015\Detail
NcPath        …\AutoCAD 2015\Nc
ExportPath    …\autocad 2015\Export
```

⇒ **No project is loaded, and all the output paths are the bare installation defaults.**

And A.5's own tell confirms it: *"The change of the AutoCAD main dialog informs you whether a
project has been loaded. **Name and file path of the loaded project are displayed there,
instead of** the information that you are working with a certain AutoCAD version."* The title
bar reads `Autodesk AutoCAD 2015` — no project.

⚠️ Noted in passing: the title bar's **drawing** name is stale (`[Drawing2.dwg]` while the open
file is `A-dialogs-templates-settings.dwg`). Second time today. **Never read the drawing name
off the caption — use `whoami`.**

`SetProjectPath(Path, Restore)` exists on the settings object. **Not called.** Creating or
switching a project reorganises where Amir's detail drawings, parts lists and NC files are
written; that is his to decide.

---

## What this is worth to Eretz Barzel — for Amir

The pieces from A.3.2 and A.5 fit together into something concrete:

1. **Company standard, once:** configure the standard end plate, base plate, stiffener and
   bolt style in the dialogs, save each as a template under an `EB/` branch, and **`BLOCK`**
   them against overwriting.
2. **Share it:** put the `Varia` folder on a network path and every workstation draws the same
   details.
3. **Per customer, per job:** create a project with **styles related to the project** and
   templates **not** related to it — so every job inherits the company's connections but
   carries the client's drawing style.
4. **And the agent calls them by name** — `conn kind=endplate template='EB/standard-endplate'`.

⛔ **None of this was done.** It creates folders and changes where files are written on Amir's
installation. It is a proposal, and a good one.

---

## The strip

**A.05-PROJECT-MANAGER**, one specimen (`193`) carrying the measured state in its notes: no
project loaded, and what a project would own if there were one.
