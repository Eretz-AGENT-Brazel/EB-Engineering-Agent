# B.11 Create ACIS body reference — chapter notes

*Read end to end 09/08/2026, pages 193–194 (fulltext lines 4630–4705).*

## What it is for — and the line that decides whether you need it

> *"Although ProSteel makes its own 3D-base elements, it may occur nevertheless that you must use
> **AutoCAD-3D-base elements (ACIS solids)** in your model."*

| what you want from the ACIS solid | what you need |
|---|---|
| only *"the determination of some **interfering edges**"* or *"machine parts in **2D-overviews**"* | ⭐ nothing — *"you can do that **without special measures**"* |
| ⭐ *"**dimensioned component part drawings**"* | **an ACIS body reference** |

With the reference, *"the ACIS body is then assigned **parts list characteristics** and its own
**component coordinate system** is available in the **DetailCenter as a stand-alone component**."*
⇒ **This is how a non-ProSteel solid — a bought-in machine part, an imported model — enters the
parts list and gets a shop drawing.**

An extended form exists for massive construction *"which can also cover **reinforcement
elements**"* — see the ProConcrete manual.

## Creating the linkage

Select the ACIS component, then set the **component coordinate system**: two points for the X axis
plus a point on the positive XY plane.

⭐ **Two ESC shortcuts, and they are different:**
- **ESC instead of the XY point** — *"the x-axis you selected then **remains valid** and the
  **current view direction becomes the z-axis**."*
- ⭐⭐ **ESC at the FIRST point of the x-axis** — *"then the **inertia axes** determine the x, y and
  z-axis of the component coordinate system **based on the size of their moments of inertia**."*
  Useful *"if a particular alignment is not required"*.

The reference then appears *"as a **symbol at the center of mass**"* and the PS component
characteristics dialog opens.

⭐ **Why the axes matter:** *"the coordinate system later has an effect on the **orientation and
dimensioning of the 2D-workshop plans**… In particular with **irregular bodies** a practical
orientation can be determined this way."* In the worked example, *"the **x axis determines the
horizontal orientation**. The view of the **XY-plane is always the front view**."*

⚠️ *"The original ACIS component **remains unchanged and is the definitive object**. That is, **if
this is deleted, then the reference is also deleted**."*

---

# MEASURED 09/08/2026 — band at x = 380000

## The API is two small classes

```
Bentley.ProStructures.Steel.PsCreateSolidReference
    Create(Int64 PickedSolidId, Boolean GetFromMassProp) -> Int64
    SetInsertMatrix(PsMatrix)      the 3-point component coordinate system
Bentley.ProStructures.Steel.PsSolidReference
    SolidId · IsSolidErased · GetInsertUcs(PsMatrix)
```
⭐ **`GetFromMassProp` is the manual's ESC-at-the-first-X-point** — the inertia-axes mode.
⭐ **`IsSolidErased` exists precisely for the manual's warning** about the original being definitive.

## The whole chain, measured

```
1. a ProSteel solid                     box 900×600×400        census 840
2. CreateAsAcisBody(id)                 -> class AcDb3dSolid   census 841
3. Create(solid, massProp=TRUE)         -> refId               census 842
4. read back                            solidId=<the body>  isSolidErased=False  ucs=OK
```

✅ **Step 2 confirms B.12.7's escape hatch is real.** `PsEditShapeModification.CreateAsAcisBody`
produced a genuine **`AcDb3dSolid`** — an AutoCAD solid, not a ProSteel one — as a **new** object
alongside the original, which survives.

## ⭐ `massProp = false` REFUSES on its own

```
Create(solid, true)   -> refId, census +1     ✅
Create(solid, false)  -> refId = 0, census unchanged
```
⇒ The non-inertia mode needs the component coordinate system supplied first via
**`SetInsertMatrix`** — with no matrix there are no axes, and the call declines. **The inertia-axes
mode is the only one that works with no further input**, which is exactly what the manual offers it
for.

## ⭐⭐ The manual's warning is literally true — and the census is what proved it

Deleting the ACIS body:
```
census   840 (box) -> 841 (acis) -> 842 (reference) -> delete the acis body -> 840
handles  box EXISTS · acis body GONE · reference GONE
```
⇒ **The reference dies with its solid**, exactly as stated. Two entities vanished for one delete.

⚠️ And a measurement trap worth keeping: **immediately after the delete, a COM `HandleToObject`
check still reported the reference as existing.** Only the census — and a later, fresh COM read —
showed it gone. The first read-back was a stale-cache artefact.
⇒ **After a delete, re-acquire the document before believing an existence check**; prefer the
census, which settled correctly at once. (Reading the reference through the plugin at that moment
threw **`eWasErased`**, which was the true signal.)
