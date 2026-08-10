# THE CEILING — what ProSteel will not do from code

*Consolidated 09/08/2026 from nine chapters. Every entry was measured, not inferred.*

**The pattern, in one line:**
> ⭐⭐ **Anything that requires a MOUSE PICK is unreachable from the API.**

That is not a list of accidents. It is the shape of the product: ProSteel's interactive commands
own the picking, and the managed classes expose the *data* of a connection but not the act of
choosing what it connects. Once that is accepted, the response is always the same — **build the
thing by composition from calls that do work.**

---

## 1. Closed — measured, repeatedly, do not retry

| what | evidence | chapter |
|---|---|---|
| **Bracing** (`PsBracing.insert()`) | **13 configurations** — shape type, two catalogue spellings, `recalcPoints`, minimal, UCS, static flag, layout, welded, no-gussets, shorten, dynamic control, wrong-plane control. `insert()=False`, census unchanged, every time | B.24 + B.25 |
| **The bracing MACRO** (`PSN_HollowShapeBracing`) | `InitialCall()`, `CreateClone(params)`, `Create()` — **all three print *"Choose support shape"* and park the session**; all return 0 | B.24 |
| **Gusset plates** | `PsGussetConnection` has **no creator at all**. The manual: *"the entire bracing including gusset plate is generated"* — so the gusset inherits the bracing's interactivity | B.23 + B.24 |
| ~~**Clone / transfer of manipulations**~~ | 🛑 **RETRACTED 10/08 — see below. `TakeoverDrills` WORKS.** | B.4.5 |
| **Standalone weld flags** | `PsCreateWeldFlag.Create()` returns false, `objectId=0` — **5/5** in B.26. Welds exist only where a **connection owns them** (B.22 produced four `Ks_WeldFlag` that way) | B.19, B.22, B.26 |
| **Haunch placement at a point** | With the plane fixed, the parts still build at the **support's origin**; `SetConnectionPoint` and `InsertPoint` do not move them | B.26 |
| **Cope from a connection class** | `CreateCope=true` + valid template left the beam byte-identical, on both B.19 and B.20. ✅ **Solved separately** by `PsCopeConnection` — see the workarounds | B.19, B.20 |
| **The cope's ESC route** (notch at a shape end, no second shape) | `Check()=0` without a support, whatever `UseShapeEndCope` says; and the class has **no `SetConnectionPoint`** | B.12.6 |
| **Bolting with no pre-drilled holes** | The manual says drilling first *"is not necessary any more"*. `PsCreateBolt.AddObject + Create()` returns `create=False` with 0 holes; drilled first, it bolts cleanly | B.15 |
| **`PsCreateFastener`** (anchor bolts) | **Nothing created** — 4 kinds × 3 styles × with/without host id × embedment segments, verified by diffing `ModelSpace` handles | B.11 / staircase |
| **B.6.7 Additional Axes** (build the grid from an architect's 2D plan) | ⭐ **Structural, and now named:** `PsCreateGrid` is the creator and has **no user-axis methods**; `PsGrid` has `addUserXaxis`/`addUserYaxis` and **no creator and no binder** — no `SetObjectId`, no `readFrom`. The two halves never meet. The one recorded untried route, `PsGrid.insert()`, was tried 10/08: `addedX=0 addedY=0`, census unchanged. `IKs_ComGrid` has no equivalent either (re-verified) | B.6.7 |
| **The 62 `PSN_*` macros** | The Connection Center's own connections. They instantiate and give real metric defaults — but every entry point prompts | B.24 |

---

## 🛑 RETRACTED 10/08/2026 — `TakeoverDrills` was never broken

Found during the part-B audit. The entry above said the transfer *"moves nothing — 5 call
sequences with selections proven correct, `changed=0` every time"*.

**It transfers. Measured today**, on three fresh identical HE300B beams:

```
source  135E   hole  y 150 → −150  at z=1500,  ⌀22
variant 1  →  135F   hole  y 1050 → 1350   z=1500  ⌀22      changed=1
variant 2  →  1360   hole  y 1450 → 1750   z=1500  ⌀22      changed=1
variant 3  →  1361   hole  y 1850 → 2150   z=1500  ⌀22      changed=1
variants 4, 5 also transferred · 6, 7, 8 have no case and do nothing
```

Each hole sits at **its own beam's centre**, same z, same diameter — the transfer is real
geometry, not a count. `variant 1` is `SetToDefaults` + `SetObjectId(src)` +
`TakeoverDrills(sSrc, sTgt)` and **nothing else**; there is no fall-through to the composition
route, which is `variant 9`.

### What I got wrong, and how

Re-reading B.4.5 for the audit turned up a precondition the notes had quoted but the test had
never honoured: *"A prerequisite for cloning is that the parts have a **position number** and
that these **match**."* The model carries **no position numbers at all** — 0 distinct `Posnum`
across 400 parts — so the 06/08 test violated the documented precondition.

⚠️ **But the control disproved my own new hypothesis too.** With the source numbered `CTRL`:

| target | result |
|---|---|
| no position number | `changed=1` |
| a **different** position number | `changed=1` |
| the **same** position number | `changed=1` |

⇒ **The position number is not the gate either.** I cannot reconstruct why 06/08 returned zero,
and I am not going to invent a reason. What is established is only this: **it works now, on
identical parts, verified by geometry.**

### The lesson, which is the useful part

*"Selections proven correct"* proved the **selection sets** were valid. It did **not** prove the
call had been given what the manual asks for. **A "closed, do not retry" verdict is only as good
as the preconditions the test honoured** — and a wrong entry on this list is expensive, because
its whole purpose is to stop anyone looking again.

⇒ **Anything on this list that was closed without the manual's stated preconditions being
checked should be re-tested.**

⚠️ **One real caveat, exactly as B.4.5 warns.** The source hole ran **−Y** and the targets' run
**+Y**: *"the transfer of the manipulations refers to the **coordinate system of the parts**"*.
Immaterial for a through-hole; **it matters for a countersink or a slot**, and it is why a
mirrored target receives a mirrored modification.

### Still not exposed
The other four Clone categories — **Cuts, PolyCut, Notches, Boolean** — have no API entry point.
`TakeoverDrills` is still the only transfer *method*; what changed is that it works.

---

## 2. Partly closed — the parameters do not arrive

Not a refusal: the call *works*, but the numbers passed are ignored and the **template's** values are
used instead.

| class | evidence |
|---|---|
| **Base plate** (`connbase`) | `anchordrill=140` → 100; hole spacing ±150 → ±75. **Workaround: build it, then POSITION the assembly** (`align copy=0`) |
| **End plate** (`conn kind=endplate`) | `nv/dv/nh/dh` ignored; the template's 2 rows at 50 mm are used. **Workaround: choose the right template** — `example/example3` gives 3 rows spread **627 mm** |
| **Purlin** | Same signature of behaviour |

⇒ **The rule: choose a template, do not pass numbers.** And read the result back — the template
carries state the property dump does not show (B.12: hand-built link data with *identical* visible
values did nothing).

## 3. Still unknown — never fairly tested

| what | why it is open |
|---|---|
| `CreateRotation` (rotational solid) | Refused with a world polygon **and** a local one; the axis framing was never varied |
| `CreateHull` | Needs **`SetPoints(PsDataPointArray)`**, and the op passed a `PsPolygon`. **Never a fair test** |
| Clone's other four categories | Only Drill Holes was attempted. Cuts, PolyCut, Notches, Boolean untried |
| `ClsParameters.ReadFromTemplate` / `WriteToTemplate` | ⭐ The one PSN route not tried — it may bypass the pick |
| `PsEditConnection.LinkType` | Always `kUndefinedLink`; there may be a binder that was not found |

---

## The workarounds that DO work — build by composition

| wanted | composed from |
|---|---|
| **static bracing** | shapes on the system lines + `PsDrillObject` + `boltparts`, gussets as plates — a full braced bay, 2/2/2 at four ends (B.25) |
| **a bolted moment corner** | ⭐ **`conn kind=endplate` with the right template** — hand-rolling it 3× gave 0 bolts; the connection class built it on the **first call** (B.26) |
| **a spiral staircase** | primitives + bent plates + `spiral` (B.4.6), collars sized to the riser (staircase task) |
| **anchor bolts** | the **base-plate connection**, then position the assembly for the embedment |
| **transferring holes** | read the source's holes, drill them again (`clonedrills variant=9`) |
| **a cope** | `PsCopeConnection` **from a template**, support mandatory |
| **welds** | let a connection own them (`WeldToSupportShape=1`, stiffener `weldflange`/`weldweb`) |

---

## How to use this file

**Before chasing any creator, look here.** If it is in §1, do not spend a strike on it — go
straight to the workaround. If it is in §2, choose a template. If it is in §3, it is worth
**three** attempts and no more (see the three-strikes rule).

⚠️ And record new entries here as they are found, rather than rediscovering them chapter by
chapter. Nine chapters each paid for this list separately.
