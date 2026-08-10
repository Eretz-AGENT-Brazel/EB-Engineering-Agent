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
⚠️ **Display class and area class were 0 everywhere** — nothing had ever been structured.

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
