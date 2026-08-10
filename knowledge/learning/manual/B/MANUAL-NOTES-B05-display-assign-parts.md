# B.5 Display / Assign parts — chapter notes

*Read end to end 09/08/2026, pages 117–126 (fulltext lines 2727–2960).*

The chapter defines **four independent classification systems**. Each element sits in exactly one
of each — *"it will be removed from one class if it is assigned to another"* — and the layer (B.1)
is a fifth axis on top.

## B.5.1 Hide

Six commands: `PS_Hide` · `PS_Hide_Exclude` (hide everything *except* the picked parts) ·
`PS_Hide_Group` / `PS_Hide_Group_Exclude` (whole group from one part) ·
`PS_Hide_Plane` / `PS_Hide_Exclude_Plane` (whole work plane).
Hidden parts *"are made invisible **and cannot be selected**"*.
⭐ HINT: *"most practical if **individual** parts are to be hidden. To hide/show **entire groups** it
is better to use **display classes**."*

## B.5.2 Regenerate
⚠️ *"This command is **not** identical with the AutoCAD command 'Regenerate' since the AutoCAD
command **does not reactivate the components**."*
⭐ *"You have to use this command, for example, **to generate a parts list**, since all processed
parts are **automatically hidden there for control purposes**."*

## B.5.3 Display Classes
*"Organize ProSteel-objects from different layers into display classes, **independent of the
AutoCAD layers**."* Buttons: hide / show a class, ⭐ **hide or show all the OTHERS**, assign,
remove. `Count` raises how many classes the drawing has. `Complete Groups` selects the whole group
from one part.

## B.5.4 Area Classes
⭐ *"While the display classes serve to present the 3D construction in a clearly arranged manner,
the **area classes** are better suited for a **logical structuring of the model into construction
sections**… These can be used as **selection and sorting criteria during the detailing process**."*
⚠️ *"In case of **overlapping** with the display classes, **the last carried out action will be
valid**."*

## ⭐⭐ B.5.5 Part Families — the richest of the four
*"Belonging to family classes permits an **automatic allocation of position number prefixes**, as
well as a differentiation of the constructive groups by **different colours**. In addition,
components belonging to one family class can have a **common detail style** for detailing."*

Per family: `Description` · ⭐ **`Pos Prefix`** *"appears in front of each position number"* ·
`Colour` · `Detail Style` · and 2D line settings (visible / invisible / centre lines, each with a
detail colour and line type).
⚠️ *"The 2D line settings are **only activated if the component parts are displayed in 2D**. These
settings **don't have any effects on the model display**."*
⭐ Three assignment modes: `Single Parts` (*"**all data** of the family classes will be adopted"*) ·
`Groups` (*"**only the prefixes** … for the position number of the group"*) · `Both`.

---

# MEASURED 09/08/2026

## All four live on `PsObjectProperties`, one field each

```
Visible        B.5.1 Hide / B.5.2 Regenerate
DisplayClass   B.5.3
AreaClass      B.5.4
FamilyClass    B.5.5
UpdateFamilyClass(objId, index)   the "transfer the family's changes to the parts" button
```
Also there: `DetailStyleId`, `DontDetailFlag`, `IndependendForDetailing`, `ObjectDisplayMode`.

## ⭐⭐ Part families are ALREADY IN USE — by the software, not by the modeller

Audit of the whole drawing, 839 parts:

```
d0/a0/f-1  = 798        unassigned
d0/a0/f100 = 4    f101 = 3    f103 = 1
d0/a0/f200 = 4    f201 = 4
d0/a0/f300 = 5    f301 = 5
d0/a0/f400 = 1    f401,f403,f404,f405,f406,f407,f408 = 1 each
```

⇒ **Everything a connection class generated carries a FamilyClass; everything created by hand
carries `-1`.** The families are banded (100s, 200s, 300s, 400s) per connection kind. So the
position-number prefix machinery is already running underneath — and hand-built parts are outside
it.
⚠️ **Display class and area class were 0 everywhere *at the moment of this census*** — nothing
had ever been structured.
🛑 **Corrected 10/08/2026** — as a standing statement this is now false. The area classes assigned
further down this page are real and persist: the 10/08 audit counted **`AreaClass` on 305 parts**.
Only **`DisplayClass`** stayed 0 everywhere, and that too ended on 10/08 (see below).

## ✅ Area classes assigned — the manual's "construction sections", applied

One class per lesson band, which is exactly what B.5.4 is for:

| area | band | parts |
|---:|---|---:|
| 1 | B.15 bolts | 8 |
| 2 | B.12 3D-modifications | 27 |
| 3 | B.22 purlins | 60 |
| 4 | B.4 move/copy | 29 |
| 5 | the spiral staircase | 85 |
| 6 | B.25 bracing | 21 |
| 7 | portal frame, welded | 20 |
| 8 | portal frame, bolted | 64 |
| 9 | B.10 solids | 9 |
| 10 | B.11 ACIS reference | 1 |

**324 parts, 324 changed, 0 failures**, and the read-back confirms each band carries its own class:
`a1=8 a2=27 a3=60 a4=29 a5=85 a6=21 a7=20 a8=64 a9=9 a10=1`.

⭐ One part read back **`HIDDEN`** — `Visible=false` — which is B.5.1's state made visible in the
data, and a reminder that *"all processed parts are automatically hidden"* during a parts list.

⇒ **Practical use for EB:** area class is the natural home for a construction section, a delivery
lot or an assembly — and B.5.4 says outright that it is a **sorting criterion in detailing**. Family
class is where the **position-number prefix** comes from, so assigning it is a prerequisite for
sensible numbering, not a cosmetic step.

---

# AUDITED 10/08/2026

*Chapter audit, part B. Record: `knowledge/learning/audits/AUDIT-PART-B-2026-08-10.md` §B.5.*

## ⚠️ The gap — census of all 820 parts

| system | assigned | unassigned |
|---|---:|---:|
| `AreaClass` | 305 | 515 |
| `FamilyClass` | 40 *(all of them by connection classes, none by hand)* | **780** |
| **`DisplayClass`** | **0** | **820** |

⇒ ⚠️ **Three of the chapter's four assignment systems were implemented; the fourth was read and
never touched.** B.5.3 Display Classes had **zero** parts in the whole model.

⚠️ The count differs from the 09/08 block above (839 parts, 324 area-class assignments). The audit
record does not account for the difference; do not guess at it.

## ✅ B.5.3 exercised — `DisplayClass` is writable and it sticks

Six fresh beams: display class **1** on three, **2** on three, area class **12** on all six.

* ✅ `DisplayClass` reads back **1** and **2** respectively.
* ✅ **B.5.4's independence claim is CONFIRMED** — *"area and display classes are completely
  independent from one another"*. One part holds **`d1` and `a12` simultaneously**, and the op's
  own tally reports them as separate axes: `d1/a12/f-1=3`, `d2/a12/f-1=3`.
* ✅ Per-part visibility toggles both ways.

## ⚠️ What was NOT tested — stated plainly

B.5.4's *"in case of **overlapping** with the display classes, the **last carried out action** will
be valid"* is about **class-level** hide and show — the B.5.3 buttons. The test hid and showed by
**coordinate window**, which writes `PsObjectProperties.Visible` **per part** — the equivalent of
`PS_HIDE`, not of the class buttons. **The overlap rule remains untested.**

⭐ But the data *explains* it: there is **one `Visible` boolean per part**, and both systems write
that same flag. *"Last carried out action wins"* therefore describes **a single shared flag, not a
priority system**.
🛑 That paragraph is an **EXPLANATION, not a measurement.** Keeping the two apart is the main
lesson of this audit — four conclusions were withdrawn today because reasoning had been filed as a
finding.

## ⛔ Deliberately NOT done — Amir's call, not mine

* **Assigning display classes across the model.** B.5.3's own examples are *"bracings, bay rails,
  curtain walls"* — **structural function**. That is a taxonomy for Amir to define, not for me to
  invent.
* **`FamilyClass` on the 780 hand-built parts.** A family carries a **position-number prefix**, a
  **colour** and a **detail style**, so assigning one is a fabrication decision.
  ⚠️ It is also a prerequisite for prefixed position numbers, so it will matter at B.29.

⇒ **The capability is proven; the scheme is Amir's.**

## Model state
The six test beams were erased afterwards.
