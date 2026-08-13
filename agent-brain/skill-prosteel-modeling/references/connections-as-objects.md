# Connections are objects — how a steel joint really exists in ProSteel

*Learned 29–31/07/2026 with Amir. His words: **"every connection is a function created in the
software, and there you can see all the data of each connection."** The pointer he gave:
**PS PROPERTIES** for any element, **PS CONNECTION** for any joint.*

---

## What you see vs what is there

Look at a steel model and you see plates, bolts and holes. That is the **output**. What ProSteel
actually stores is a **connection object** — a *logical link* attached to a part, holding the joint's
**recipe**: plate sizes, hole diameter and spacing, thickness, weld throats, bolt type and count, rib
shape. The plates and bolts are generated from it and stay tied to it.

**Consequence:** model a rectangle and a cylinder in the right place and you get **the shadow of the
joint**. It photographs fine and it is wrong — no holes, no welds, no relationship between the parts,
and the part list and NC output are wrong. Amir: *"a bolt that just passes through and looks like it
drills is a critical error."*

## Reading a joint (PS CONNECTION)

`PsEditLogicalLink` is the Connection Editor (`PS_EDIT_CONNECTIONS`) as an object:

```
SetObjectId(id)              pick a part
get_LogicalLinkCount()       how many joints sit on it
GetLogicalLinkByNumber(n)    one PsLogicalLink
```
From each link: `Type` · `Name` · `Ident` · `Description` · `LinkObjectCount` / `BoltObjectCount` ·
`getLinkObjectId(i)` / `getBoltObjectId(i)` — and the recipe itself via `GetBasePlateLinkData()`,
`GetStiffenerLinkData()`, `GetSpliceJointLinkData()`, `GetShearPlateLinkData()`,
`GetWebAngleLinkData()`, `GetCopeLinkData()`.

## What a real Eretz Barzel model looks like

Access platform, 175 members — **82 parts carrying 92 joints**:

| Type | Name | Count | What it is |
|---|---|---|---|
| 10 | **Brace Plate** | 39 | gusset for the diagonals — the most common joint |
| 11 | **Endplate connection** | 17 | 15 of them produced **2 plates + 4 bolts** each |
| 12 | Conn. Shape | 11 | member-to-member |
| 0 | Unknown | 13 | modifications |
| 1 / 36 | Cut Position | 6 | cuts |
| 13 | **Baseplate Connection** | 3 | base plates |
| 2 | Cut through | 2 | through-cut |

Base-plate recipes read straight out of that model:
- `180×180×10` · hole **⌀23** · spacing 100×100 · anchors ⌀20
- `300×300×12` · hole **⌀23** · spacing 200×200 · anchors ⌀20

⌀23 is M20 — exactly the rule Amir had stated verbally. **The joint carries the rule, not just the
result.** Also: 104 `Ks_VolBody` objects on layer `PS_Bolt` in that model are connection-generated
bolts (building one base-plate joint produces exactly those).

## Holes: what drills and what does not

| Method | Drills? | Evidence |
|---|---|---|
| `CreateSingleBolt` + `AddObject(host)` — "a bolt with hosts" | ❌ **No** | plate + bolt with 2 hosts → holes read back = **0** |
| **`PsDrillObject`** | ✅ Yes | `rc=1`, hole confirmed on read-back |
| **the connection object itself** | ✅ Yes, unasked | a base-plate joint created 5 objects and **4 holes** |

**The verification instrument is `PsSingleHoleArray`** — `Count`, `getHole`, `getFromSlottedHole`,
`getMaximalLength`. A screenshot of a bolt crossing a plate looks the same with and without a hole, so
a screenshot is never evidence. This mistake was made and caught: a whole model was reported as
"ProSteel drilled real holes" when it contained **zero**.

## Hole sizes follow the element

From 713 holes in one real model:

| Diameter | Count | Bolt |
|---|---|---|
| ⌀23 | 176 | M20 |
| ⌀19 | 236 | M16 |
| ⌀15 | 37 | M12 |
| ⌀14 | 14 | M12 |
| ⌀13 | 190 | M12 |
| ⌀12 | 60 | M10 |

**301 of 713 were M10/M12** — guardrails (40×2.5 SHS), handrails (⌀26.9), ladder rungs (⌀21) do not
take M16. And **284 of the holes were in profiles**, not plates. Never apply one bolt size across a
model; never drill only into plates.

🧲 **Clearance is 2 mm** — M16→⌀18 · M20→⌀22 · M24→⌀26, bolt style **`8.8S`**. Iron rule, Amir,
**13/08/2026**, and it also happens to be EN 1090-2's normal clearance.
🛑 **It replaced the 3 mm rule** that this file recorded from the access-platform model (⌀19 ×236,
⌀23 ×176). ⚠️ **Those counts stay true of that model** — pre-13/08 work legitimately carries +3;
read it as it is and never "correct" it unasked. New work is +2.

## Ribs / stiffeners (ריפים)

Amir's definition: on the plates of a joint (e.g. RHS↔RHS) sit small plates that are **not exactly
rectangular** — they are welded on and contribute to the joint's stability.

ProSteel offers **7 stiffener templates**: `half/full chamfered` (`ShapeType=0`),
`half/full convex` (1), `half/full rounded` (2), `Standard`.

**Why the corner is cut** — in order of importance:
1. What remains is the **triangle in the load path** from the member into the plate. Steel outside that
   path carries nothing and costs money.
2. The profile has an **inner corner radius**; a square rib would not seat against it.
3. A sharp re-entrant corner is a **stress raiser**, and the welder needs continuous access around the
   rib toe.

**Sizes are not fixed.** Measured: 15×15 off a 100×150 rib that ProSteel generated itself; **80×80**
off Amir's 120×120 rib; **75×75** on a 100×100 rib in his drawing. Roughly two-thirds of the corner in
his practice. Read or measure — do not assume.

**Data signature of a cut plate:** a bbox family with "nearly identical but not identical" dimensions
(e.g. 201–206 × 187–188 × 10) is a **cut contour**, not a rectangle. A true rectangle family reports
one size.

## Splices

Four column splices found in one model (RHS150 stacked on RHS150):
- one **bolted**: a pair of 290×240×12 plates + 150×70×10 ribs + 10 bolts, 24 mm gap
- three **welded**: cover plates 240×120×6 + 240×70×6, zero gap

Tool: `PS_LASCHE` / `PsSpliceJointConnection`, whose recipe carries `HoleDiameter`, `HoleWorkloose`,
plate thicknesses for web and flange, hole counts per direction, and lap lengths.

## Rules that follow from all this

1. **Build the joint, not the parts.** Macro first (`PS_GROUNDPL`, `PS_ENDPLATE`, `PS_LASCHE`,
   `PS_GUSSET_PLATE`, `PS_RIP`); polygon plate + `PS_DRILL` only when no macro fits.
2. **Every bolt passage gets a modelled hole — profiles included.**
3. **A rib is never a generic rectangle.**
4. **Bolt diameter follows the element**, not a global default.
5. **Verification means reading the datum back** — `PsSingleHoleArray` for holes, `GetPolygon` for
   shape, `PsEditLogicalLink` for joints. Not a screenshot, not an echo of the input.
6. **A design change tunes the connection's parameters**; it does not delete and rebuild.
