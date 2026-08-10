# B.26 Haunches — chapter notes

*Read end to end 09/08/2026, pages 356–360 (fulltext lines 8429–8530).*

> *"ProSteel automatically generates haunches by clicking two shapes or by picking points on a
> construction line… This command enables the creation of tapered haunch connections… Therefore
> **the complete haunch creation is limited to one single function call**."*

## The dialog

`Upper Chord` / `Lower Chord` (width + thickness) · `Web` (plate thickness) ·
⭐ **`Coped Shape`** — *"the haunch is **not made from individual plates but from a coped shape**.
The shape size corresponds to the connection shape; all other shape size fields are **ignored**"* ·
`Bottom Stiffener` / `Top Stiffener` — ⭐ *"just select a **stiffener template**, which you
previously saved using the command 'Stiffeners'"* — **B.16's templates feed this chapter** ·
`Length`, `roof pitch`, `bottom width`, `head width`, `offset` ·
⚠️ *"a modification in **roof pitch** has only an effect if a haunch has been created **without
supporting shape**"* · `Cone Width` · `Create Group` ·
⭐ **`Bottom flange haunch`** — *"**no top flange is created**, e.g. for the construction of **frame
corners**"* · `Cone` (taper fitted to both shapes) ⚠️ *"the '**Fixed Size**' field cannot be checked
as well"* — mutually exclusive · `Fixed Size` · `Turn` · and a button that **copies an existing
haunch connection** by clicking it.

**Creating one:** click the shape to be connected *near the end* → ⭐ **ALT gives a bottom-flange
haunch directly** → then the supporting shape → ⭐ **RETURN / right-click = no support**, a simple
haunch on the shape alone → ⭐ **ALT on the support fits the haunch flanges and web to it**.
*"The **last settings** are used"* — stateful, like every other connection.
⭐ **Unrestricted positioning:** press **ESC** after the command, then click the intersection of the
base boundary line with the upper flange (*"consider using the virtual intersecting point without
Z"*) and the approximate haunch direction.

---

# MEASURED 09/08/2026

## The API maps cleanly

`PsHaunchConnection` + `PsHaunchLinkDataMgd`: `Length` · `TopHeight` · `BaseHeight` ·
`WebThickness` · `Slope` (roof pitch) · `IsConical` / `ConicalWidth` ·
⭐ `IsBottomTrain` (bottom-flange haunch) · `TurnBottomTrain` · `IsCopedShape` / `CopedHeight` ·
`SizeDependsToConnected` (Fixed Size) · `StiffenerAtSupport` / `StiffenerAtConnected` ·
`CreateGroup` · `TopChordOffset` · and the plane: **`XAxis` · `YAxis` · `InsertPoint`**.
One template ships: `Default/Standard` — L=1000, topH=0, baseH=500, web=8.

## ⛔⛔ The shipped template carries a ZERO PLANE — and it destroys the model

```
tmplPlane X=0,0,0  Y=0,0,0
```
Passing that straight through **stretched both rafters of a pitched portal to ~317,000 mm**, running
them clean across the drawing and into the B.8 band 300 m away. `create=True`, no error.

⇒ **Always set `XAxis`/`YAxis` explicitly before creating a haunch.** With the frame's real plane
(XZ) set, the rafter length was untouched: `3162.278 -> 3162.278`.
⇒ And this is why the op now carries a **blast guard**: it measures the connected shape's length
before and after and shouts if it grew. A destructive operation must never fail quietly.

## ⛔ Even with the plane fixed, the haunch builds at the SUPPORT'S ORIGIN

With `at = (330000, 0, 5000)` — the eaves — the parts appeared at **z = −10…+10**, the column's
base. `SetConnectionPoint` and `InsertPoint` did not move them. Drawing the column top-down was
prepared as a test but the frames were built explicitly instead.
⇒ **The haunch cannot be placed from code where you ask.** Not pursued further; the corner was
built from parts under control, which is what the frames below do.

---

# TWO PORTAL FRAMES, DETAILED PROPERLY

*Amir's brief: "אל תשאיר את החיבור ביניהם ככה שרירותי — תמדל אותו מדויק. תייצר 2 פרטים — אחד
בריתוך… השני בחיבורי פלטות וברגים."*

**The scheme, both frames:** span 6000, eaves 5000, apex 6000 → pitch **18.43°**. Columns HE300B,
rafters IPE400. Each rafter starts at its column's **inner face**, so the corner is a real
rafter-end-to-column joint and nothing overhangs.

## FRAME A — WELDED, x = 330000
- **Apex** mitred on the bisector — ⭐ *one* `miter` call took **both** rafters `cutPlanes 0 → 1`.
- **Eaves** column web stiffened opposite **both** rafter flanges, `weldflange`/`weldweb` on —
  **8 stiffener parts**, because a welded moment corner drives the rafter flange force straight
  into the column web.

⛔ **Standalone weld flags still cannot be created**: `PsCreateWeldFlag` refused 5/5 here
(`objectId=0`). Welds exist only where a **connection owns them** — B.22 produced four `Ks_WeldFlag`
that way. So a "fully welded" frame is modelled as welded *geometry* plus connection-owned welds,
never as free-standing weld objects.

## FRAME B — BOLTED, x = 346000
- **Apex**: an end plate each side of the ridge, drilled together and bolted —
  **4 holes / 4 holes / 4 bolts** ✅
- **Eaves**: `conn kind=endplate` (`PsStandardPlateConnection`) — **5 parts per corner**: an end
  plate against the column face plus **4 `Ks_Bolt`**, column **+4 holes**, grip **55 mm**. ✅
  The rafter gets no holes, correctly: the end plate is welded to it and the bolts pass plate +
  flange only.

## ⭐⭐ THREE lessons this joint taught, all measured

**1. The drill places holes by the HOST'S GEOMETRY, not at the point you pass.** Three times:
- aiming at the column flange put an **11 mm** hole on the column **axis** — the web;
- after `rot=90`, the same call gave a **300 mm** hole clean through both flanges;
- `drillspecial kind=blind` put its 19 mm hole on the **far** flange (x=345850), not the near one.

Each time the plate's holes ended up 160–310 mm away from the column's, far beyond `Gap distance`,
so **`boltparts` created nothing and said so correctly**.

**2. Grip length silently kills bolts.** A 300 mm through-hole plus a 20 mm plate asks for a
**320 mm** grip — outside every style's window (B.15: `PsBolt.GripMin`/`GripMax` are real and
enforced). Zero bolts, no exception.

**3. The structural catch: the columns were bending about their WEAK axis.** The 11 mm hole depth
on the column axis is what exposed it — an HE300B web is 11 mm. The section had been inserted with
its flanges facing ±Y while the frame stands in XZ. `rot=90` puts the strong axis in the frame
plane, where a portal column belongs.
⇒ **A hole depth is a section-orientation probe.** Web thickness vs flange thickness tells you
instantly which way a member is turned.

## ⇒ And the lesson the skill already carried, relearned the hard way

*"Reach for the connection class before building a detail out of B.14/B.15 primitives."*
The eaves was hand-rolled three times — plate + drill + bolt — and produced 0 bolts every time.
`conn kind=endplate` produced the whole corner, correctly drilled and bolted, on the first call.

---

# AMIR'S THREE CORRECTIONS — and what each one taught

*"1. המסגרת ריתוך לא עשתה טוב בכלל… 2. הפלטה בין 2 הקורות צריכה להיות ממורכזת יותר… לא אמורים
להיות חורים בפלאנגים של העמודים שאין בהם מפגש חיבור… וגם נדרש לרווח יותר את הברגים."*

## 1. The welded eaves was never a connection at all

The rafter's **sloped** end met the column's **vertical** face along a line, and the column stopped
at 5000 while the rafter reaches 5250 there — so the top flange overhung into thin air.

**Fixed:** columns run to **5250** (the rafter's top at the face); the rafter is drawn *into* the
column and then `cutat` trims the surplus, so its end butts **flat** on the flange —
B.12's *"the shorter section is always cut off"*, measured **3228.944 → 3137.497**. Stiffeners
opposite both rafter flanges, welded.

## 2. Stray holes in flanges with no connection — and why they could not be erased

Each frame-B column carried **8** holes: 4 real Ø18 on the inner flange from the end-plate
connection, and **4 Ø22 on the OUTER flange** left by a failed blind-drill probe.
⚠️ **Holes cannot be removed once drilled**, so a probe that misses leaves permanent damage.
⇒ **Probe on a throwaway member, never on the real one** — and if a probe does land on a real part,
the part must be rebuilt.

## 3. Bolt spacing — and a template that fixes it

The end-plate connection put its two rows **50 mm** apart. ⚠️ `nv` / `dv` / `nh` / `dh` **do not
land** — the connection uses its **template's** layout, the same "values that never arrive" seen on
the base plate and the purlin. So the fix is to choose the right template, not to pass numbers.

Measured across the seven shipped end-plate templates:

| template | result |
|---|---|
| `default/Standard` (200 tall, 2 rows) | rows **50 mm** apart — too tight |
| `example/example4` (340 tall) | 6 parts, **no holes in the column** |
| ⭐ **`example/example3`** (nV=3, `stiff=True`) | **16 parts**, 3 rows at z = 4595.8 / 5042.5 / 5222.5 — **spread 627 mm**, and it brings its own stiffeners |

⇒ **A real moment eaves: rows near the bottom flange, at mid-height and near the top flange.**
The apex plate was re-centred **on** the ridge (it had sat 120 below) with its bolts **260** apart.

## ⚠️⚠️ Deleting a connection's PARTS does not delete the CONNECTION

After purging the orphaned plates and bolts, re-running the eaves reported **`parts +0`**: the
**logical link survived** its own products, so ProSteel believed the joint was still there.
⇒ **To rebuild a connection, rebuild the members** — or remove the link explicitly. Clearing the
geometry alone leaves an invisible joint that blocks the next attempt.

⚠️ And the source of those orphans: a template comparison built connections on a throwaway column
and rafter **at the same coordinates**, then deleted only the column and rafter. **A connection's
products outlive the members they were made for.**

## Final state

| | FRAME A — welded, x=330000 | FRAME B — bolted, x=346000 |
|---|---|---|
| apex | mitred on the bisector, one call cuts both | plates centred on the ridge, 4/4 holes, **4 bolts**, 260 apart |
| eaves | rafter trimmed flat to the flange, 8 stiffeners welded | `example/example3` — 16 parts/side, **3 bolt rows spread 627 mm** |
| columns | to 5250, `rot=90` (strong axis in the frame plane) | same, and **0 stray holes** |
