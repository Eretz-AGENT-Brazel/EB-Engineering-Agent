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
| **Clone / transfer of manipulations** | `PsDrillObject.TakeoverDrills` is the only transfer in the API and moves **nothing** — 5 call sequences with selections proven correct (`srcSel=1 tgtSel=3`, `Find()` true), `changed=0` every time | B.4.5 |
| **Standalone weld flags** | `PsCreateWeldFlag.Create()` returns false, `objectId=0` — **5/5** in B.26. Welds exist only where a **connection owns them** (B.22 produced four `Ks_WeldFlag` that way) | B.19, B.22, B.26 |
| **Haunch placement at a point** | With the plane fixed, the parts still build at the **support's origin**; `SetConnectionPoint` and `InsertPoint` do not move them | B.26 |
| **Cope from a connection class** | `CreateCope=true` + valid template left the beam byte-identical, on both B.19 and B.20. ✅ **Solved separately** by `PsCopeConnection` — see the workarounds | B.19, B.20 |
| **The cope's ESC route** (notch at a shape end, no second shape) | `Check()=0` without a support, whatever `UseShapeEndCope` says; and the class has **no `SetConnectionPoint`** | B.12.6 |
| **Bolting with no pre-drilled holes** | The manual says drilling first *"is not necessary any more"*. `PsCreateBolt.AddObject + Create()` returns `create=False` with 0 holes; drilled first, it bolts cleanly | B.15 |
| **`PsCreateFastener`** (anchor bolts) | **Nothing created** — 4 kinds × 3 styles × with/without host id × embedment segments, verified by diffing `ModelSpace` handles | B.11 / staircase |
| **The 62 `PSN_*` macros** | The Connection Center's own connections. They instantiate and give real metric defaults — but every entry point prompts | B.24 |

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
