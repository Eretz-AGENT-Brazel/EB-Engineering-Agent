# B.7 Choose View — chapter notes

*Read end to end 09/08/2026, pages 152–153 (fulltext lines 3531–3588). A short chapter, and a
structural one: it explains what a "view" actually **is** in this product.*

## ⭐ A view is not a camera angle — it is three things at once

> *"Use this command to select the views defined by the **work frames** command or by adding new
> views. Once you select a view, ProSteel **places the UCS into the selected work plane** and
> displays the 3D model **looking at the plane vertically**. The specified **cut planes are
> activated at the same time** so that **only the objects within this area are visible**."*

So activating a view does all of:
1. **moves the UCS** onto the work plane,
2. **points the camera** perpendicular to it,
3. **switches on clipping planes** so everything outside the slab disappears.

⇒ **B.7 is the reader/activator for what B.6 creates.** Views are not a separate kind of object —
they are the work frames themselves. Everything in this chapter therefore lives on the work frame.

The dialog lists *"the views which are the result of the settings for a work frame with the area
name 'R1'"*, sorted; names are editable *"manually via Change Properties"*.

## The dialog

| control | meaning |
|---|---|
| `Zoom Extents` | *"After a view has been activated a Zoom Extents is carried out at once"* |
| `Clipping-Plane` | *"All elements **in front of or behind** the view reaching beyond the defined distances are **not displayed**"* |
| `Double Click` | activates the view and closes the dialog; ⭐ **CTRL inverts the setting** |

Six buttons: activate the view and close · ⭐ **activate only the UCS — *"The view is not
modified"*** · **delete the view from the model** · **create a view** (asks a name, then a
rectangle; *"created in the **current UCS**"*) · ⭐ **view via 2 points** (*"specify the **target**
point of the view as well as the **camera** point"*) · ⭐ **view on an object** (pick the object,
then choose among **coloured arrows**) · **UCS on an object** (same picking, see `PS_ObjectUCS`).

---

# MEASURED 09/08/2026

## ⭐ The whole chapter is `PSCOMWRAPPERLib.IKs_ComWorkFrame` — no plugin needed

Nothing in B.7 has a managed counterpart. Every control maps onto the COM work-frame object,
reachable straight from Python:

```python
o = doc.HandleToObject(handle)          # a Ks_ComWorkFrame
```

| dialog | COM member |
|---|---|
| activate, with the two checkboxes | ⭐ **`SetActive(ZoomExtents, UseClipPlanes)`** |
| the clip distances | `GetClipDistances(&front,&back)` / `SetClipDistances(front, back)` |
| the clip toggles | `EnableFrontClip` / `EnableBackClip` |
| "view via 2 points" | `GetCameraView(&eye,&target)` / `SetCameraView(eye, target)` |
| "activate only the UCS" | `GetInsertUcs(&matrix)` |
| "delete the view" | `Delete()` / `Erase()` |
| the view rectangle | `GetRectangularExtents(rect)` |

⇒ **Fourth time this project has found a whole feature living only on the COM side.**

## What B.6 actually generated — read back from the model

41 work frames exist. The four belonging to one B.6 frame (x 40000…50000) read as:

```
32A  eye (45000,  -100, 3750) -> target (45000,     0, 3750)   looking +Y   FRONT
32C  eye (45000, 19600, 3750) -> target (45000, 19500, 3750)   looking -Y   BACK
32E  eye (45000,  9750, 7600) -> target (45000,  9750, 7500)   looking -Z   TOP
330  eye (45000,  9750, -100) -> target (45000,  9750,    0)   looking +Z   BOTTOM
```

⭐ **The camera is placed exactly 100 mm off the target plane, every time.** That is the
convention: `eye = target + 100 × normal`. Useful for writing `SetCameraView` by hand — match it.

⚠️ Those four carry `clip 0/0`, i.e. **generated views arrive with no clip distances set.** The
clipping the chapter describes does nothing until distances are given.

## ⚠️ The clip toggle that matters is the ARGUMENT, not the property

Measured on view `32A`:
```
SetClipDistances(1500, 1500)  -> reads back 1500 / 1500     ✅ sticks
EnableFrontClip = True        -> reads back False           ⛔ does NOT stick
SetActive(zoom=True, clip=True)  -> the model IS clipped     ✅
```
⇒ `EnableFrontClip` / `EnableBackClip` are a **trap**: writing them looks accepted and changes
nothing, while clipping is controlled entirely by `SetActive`'s second argument. Set the
**distances** as properties; set the **on/off** through the call.

## Proof, and a note on what counts as proof here

Two screenshots of the same view, `SetActive(True, False)` then `SetActive(True, True)`:
with clipping on, the four tall columns of the B.8 band collapse to a bare line and the lower
half of the B.6 frame disappears — everything beyond ±1500 of the work plane is gone, exactly as
*"all elements in front of or behind the view… are not displayed"* says.

⭐ **This is the one chapter where a screenshot IS the evidence.** Everywhere else this project
insists on reading geometry back, because an image cannot show whether a hole exists. Here the
claim is *about what is displayed*, so the display is the correct instrument — and reading
geometry back would prove nothing, since clipping changes no geometry at all.

## Open

- `op=workframe at= x= y=` returned `EB_ERR workframe create failed`. Not chased: B.7 is about
  *choosing* views and 41 already exist, but creating one from code is unresolved.

---

# AUDIT 10/08/2026

*Part-B chapter-by-chapter audit, B.1 → B.29. Full record:
`knowledge/learning/audits/AUDIT-PART-B-2026-08-10.md`. B.7's job here was to pay the debt B.3
left: B.3's audit refused to run the `SetActive` test itself and handed it to this chapter.*

## ⭐ B.3.6's "overwritten" — resolved into THREE storage places, not one contradiction

B.3.6 claims *"when switching to one of the standard views, these values are **overwritten**"*.
On 09/08 that was tried with an **AutoCAD** view change and the distances survived; it was recorded
as **untested rather than refuted**, because a ProSteel standard view is a work-frame view
activated through `SetActive` — which is B.7's business, not B.3's.

Tested properly today, on work frame `2E8`:

```
GLOBAL cut-plane pair          500 / 500      (Ks_ComGlobalSettings, read in A.6)
work frame 2E8, as found       500 / 500
after SetClipDistances         1750 / 1250
after SetActive(True, True)    1750 / 1250    <- SURVIVED
```

⇒ ⭐ **MEASURED: `SetActive(True, True)` does NOT overwrite a view's stored distances.**

The chapters only disagree if one assumes a single set of numbers. There are **three places**:

| # | place | what it holds |
|---|---|---|
| 1 | B.3.6's `Distance` field | the **current session's** cut-plane distances |
| 2 | a view's `GetClipDistances` | that **view's own stored pair** — `1750 / 1250` after the write above, and still `1750 / 1250` after activation |
| 3 | `Ks_ComGlobalSettings.ObjCutPlaneDistance` / `ObjCutPlaneDistanceRear` | the **global default a fresh view inherits** — **500 / 500** on this installation (A.6) |

⚠️ **What follows is an EXPLANATION, not a measurement.** The measurement is the four lines above
and nothing else — this audit withdrew four conclusions elsewhere in part B that were reasoning
filed as findings.

*The reading:* activating a view **applies the view's own pair** (place 2), which is precisely what
*"your entered values are overwritten"* describes from the dialog's side (place 1). Read that way,
B.3.6's **"0 = no cut planes are created"**, B.3.5's *"can only be done using the **global
settings**"* and this chapter's own **`clip 0/0` on generated views** (measured 09/08, above) all
agree: a generated view arrives with **no clipping at all** until distances are given, and 500/500
is the default it would inherit — not a value that activation forces back onto a view that already
holds its own.

⇒ Cross-reference: `MANUAL-NOTES-B03-3d-object-views.md`, audit section of the same date, where
the same three-way split is written from B.3's side.

⭐ And it confirms 09/08's other finding from the opposite direction: **clipping is switched by
`SetActive`'s second argument, not by `EnableFrontClip`** — the distances are ordinary stored
values that persist across activation.

## Model state

Nothing needed fixing. `2E8`'s distances were **restored to 500 / 500**, so the model is left
exactly as found.

## Open — added 10/08

- `SetXViews` / `SetYViews` / `SetZViews` still do not produce a view per axis — carried over from
  B.6, still unexplained, still low value: the surface views are the ones used.
