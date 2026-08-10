# B.3 3D Object Views — chapter notes

*Read end to end 09/08/2026, pages 95–103 (fulltext lines 2162–2400).*

## B.3.1 Object View / Object-UCS

> *"The object view command is used to **align the UCS plane on an object**, which results in a
> **perpendicular view** on this plane."*

⭐ *"The object is aligned in such a way as to center the pick point on the screen and align its
**insertion direction parallel to the X-axis**. This means that a shape inserted into a work frame
**at a slant is displayed as running horizontally**, and the surrounding work frame is rotated
accordingly."*

`Object View` puts the origin **perpendicular from the pick point to the centreline**;
`Centered Object View` puts it **at the pick point**.
`Object-UCS` does the same but ⭐ **sets only the UCS — the view is not modified.**
Interaction: pick the part → **coloured coordinate crosshairs** appear → click the coloured circle
for the axis.
⭐ HINT: *"if the view does not match the model position, select the command again and confirm
immediately with RETURN"*, or ⭐ **hold ALT while selecting the view direction** to force alignment.

## B.3.2 Surface View / Surface-UCS

⭐ *"In contrast to the object view that only allows **6 possible view directions**… the surface
view offers you the option to look on **each surface** of the component part. In case of special
shapes with **sloping surfaces** it is easier to obtain the suitable view."*
Click at a **bordering edge between two surfaces**. `PS_FACE_VIEW_CEN` places the origin **in the
middle of the clicked line**. A UCS-only variant exists here too.

## B.3.3 Global View
Isometric from the right front by default, plus four more with direct calls —
`PS_GLOBAL_VIEW2 … PS_GLOBAL_VIEW5`, defined in Global Settings.

## B.3.4 Top View
⭐ *"identical with the AutoCAD command **vpoint 0,0,1**"*. It switches to the true model top view
**from within the global view**; *"it does **not have a function** in the other views because you
already view the plane vertically."*

## B.3.5 Free View
Target point first, then source point. ⚠️ *"The cutting plane command **is activated**. Changing the
distances is **not possible here** and can only be done using the global settings."*

## ⭐⭐ B.3.6 Cutting Plane — the machinery behind B.7's clipping

> *"ProSteel offers a command to **hide parts in front of and behind the current work plane**…
> prevents the **accidental manipulation of stacked shapes**."*
> *"When selecting one of the defined views or an object view, such objects are **automatically
> hidden**, provided that you have not turned off this command globally."*

`Off` · `On` · ⭐ `Flip` (*"alternately switched on or off **globally**"*) · `Distance`.
⚠️ *"First enter the **rear** distance and then the **front** distance. When you enter the distance
**0, no cut planes are created**."*
⚠️ *"When switching to one of the standard views, these values are **overwritten**."*

## B.3.7 Perspective View
A fictional camera: source (camera) + target (motif) + ⭐ **`Focal Distance`** — *"as in
photography… **larger values** zoom the object **as if using a telephoto lens**, **smaller** create
a **wide-angle** effect"* — plus `Distance` to move along the line of vision.
Functions: `Set` / `Off` / `Distance` / `Focal Distance`.

---

# MEASURED 09/08/2026

## ✅ 1. The top view IS `vpoint 0,0,1`

```
after `view dir=iso`   VIEWDIR = (1, -1, 1)
after `view dir=top`   VIEWDIR = (0,  0, 1)     ← exactly the manual's claim
```

## ✅ 2. Cut-plane distances set and read back cleanly

```
as found          1500 / 1500
set 0 / 0      -> 0 / 0          (the manual: "no cut planes are created")
set 1750/1250  -> 1750 / 1250
```

## ⚠️ 3. "A standard view overwrites the distances" — NOT REPRODUCED, and not fairly tested
### ✅ **RESOLVED 10/08/2026 — see the audit section at the end of this file. No longer open.**

After setting 1750/1250 and switching with `view dir=front`, the distances **survived unchanged**.
⇒ But the manual says *"switching to one of the **standard views**"*, and a ProSteel standard view
is a **work-frame view activated through B.7's `SetActive`** — not an AutoCAD view change.
**The claim is untested rather than refuted**, and is recorded that way.

## ⭐ What this chapter explains about B.7

B.7 measured that generated views arrive with **`clip 0/0`** and that `EnableFrontClip` would not
stick. B.3.6 supplies both halves of the reason: **0 means no cut planes are created at all**, and
clipping is a *mode* that view activation turns on — which is exactly why the working switch turned
out to be **`SetActive`'s second argument** rather than a stored property.
⇒ And the two chapters agree about the *distances*: those are ordinary stored values, and they
persist.

## ⭐ Why a "view" carries a UCS at all

B.3.1's promise — *insertion direction parallel to the X-axis*, so a slanted member reads
horizontally — is only possible because the view **turns the coordinate system onto the object**.
That is the same fact B.7 states from the other side: a view is a UCS + a direction + clip planes,
never merely a camera angle.

---

# AUDIT 10/08/2026 — the "NOT REPRODUCED" item **explained**, not re-tested

## ⭐⭐ There are two different places, and on 09/08 only one had been read

That is the whole of it. Nothing was re-run on B.3 today; what changed is that the second place
became readable.

| | value | source |
|---|---|---|
| a **generated view's** clip distances | `0 / 0` | B.7, measured 09/08 |
| the **GLOBAL** cut-plane distances | **`500 / 500`** | A.6, measured today |

The global pair was read from `Ks_ComGlobalSettings` — the in-process COM object **A.2 unlocked**:

```
Ks_ComGlobalSettings.ObjCutPlaneDistance       = 500.0
Ks_ComGlobalSettings.ObjCutPlaneDistanceRear   = 500.0
Ks_ComGlobalSettings.SetGlobalViewDirection(Number, Coord, newVal)   ← the matching writer
```

## ⭐ B.3.5's dead end is now a route

B.3.5's *"can only be done using the **global settings**"* and B.3.3's *"specified in the **Global
Settings**"* read on 09/08 as a full stop — the dialog and nothing else. With the settings object
in hand they are simply an **address**: those distances are reachable from code, and
`SetGlobalViewDirection` is how the five global views of B.3.3 are written.

## ⚠️ INFERENCE, not a measurement — labelled as such

⇒ B.3.6's *"when you enter the distance **0**, no cut planes are created"* + B.7's measured
*generated views arrive with `clip 0/0`* ⇒ **a generated view has no clipping at all until
distances are given**, and *"switching to one of the standard views overwrites these values"*
describes the standard view applying a **stored pair over whatever the view held**. B.3.6, B.3.5,
B.7 and A.6 stop contradicting each other once the places are told apart.

That paragraph is **reasoning about measurements taken elsewhere**, not a result of its own. It is
filed as an explanation, and today's rule is that an explanation never gets recorded as a finding.

## ⛔ The empirical half is deliberately NOT here

Activating a work-frame view through `SetActive` and watching what the distances become is a
**B.7** test — a ProSteel standard view is a work-frame view, which is B.7's business, not B.3's.
It was run in **B.7's audit**, and the numbers live there:

⇒ **`MANUAL-NOTES-B07-choose-view.md`** — read it before quoting the inference above.

## Model state
Untouched. B.3 owns no band; its 09/08 measurements were `VIEWDIR` and clip values, and neither
was rebuilt today.

## Still open
* `SetGlobalViewDirection` — **settable and never written**. Writing it changes Amir's
  installation defaults, so it stays on the same hold as the other A.6 writes.
* The perspective view (`Focal Distance`, `Distance`) — display-only by the manual's own words,
  *"only a display view and does not allow any changes"*. No API value beyond screenshots.
