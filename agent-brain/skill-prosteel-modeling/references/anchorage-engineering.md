# Column base anchorage — the engineering behind the detail

*Distilled from a deep study of EN 1992-4, EN 1993-1-8, EN 1090-2, ACI 318-19 Ch.17, ACI 117 and
AISC Design Guide 1, applied to Eretz Barzel's measured details (lessons 4–5). Every number below was
computed and is reproducible. Full report:
`EB PROSTEEL AGENT\api+knowledge-develop\research\ביסוס-ועיגון-עמודי-פלדה.pdf` (28 pp).*

> **Scope note.** This is *understanding*, for modelling a detail knowingly and for asking the right
> question. It is **not** design authority — Phase 2 (standards / design checking) is locked until
> Amir opens it. Never present these numbers to anyone as a design verification.

---

## 1. An anchor is a load path, not a bolt

Six failure mechanisms. **Five are in the concrete, and all five are brittle.**

| # | Mechanism | Governed by | How to improve it |
|---|---|---|---|
| 1 | Steel yield | `A_s · f_uk` | bigger / higher grade — **the only ductile one** |
| 2 | **Concrete cone** | **`h_ef^1.5`** — the *depth*, to the power 1.5 | **go deeper** |
| 3 | Pull-out | head bearing area | bigger head / washer plate |
| 4 | Splitting | member thickness, edge distance | thicker member, transverse rebar |
| 5 | **Edge breakout in shear** | **`c1^1.5`** — the *edge distance* | move away from the edge |
| 6 | Pry-out | short anchor kicks the concrete out behind it | deeper (`h_ef ≥ 60 mm`) |

## 2. Depth beats diameter — the rule that inverts intuition

```
N⁰Rk,c = k1 · √f_ck · h_ef^1.5        k1 = 7.7 cracked · 11.0 uncracked
```
**The anchor diameter does not appear in this formula at all.**

- 120 → 200 mm deep: `(200/120)^1.5 = 2.15` ⇒ **+115 %**
- M20 → M24 at the same depth: **+0 %** for cone failure

**When tension fails, go deeper. When shear fails, move off the edge or add a shear lug.**

## 3. Ductility criterion — rarely checked, and Eretz Barzel's detail does not meet it

Ductile means the steel yields before the concrete breaks: `N_Rk,c ≥ N_Rk,s`. For C30/37 cracked:

| Anchor | `h_ef` needed | as ×d |
|---|---|---|
| M20 grade 4.6 | 175 mm | 8.8 d |
| M20 grade 8.8 | **278 mm** | 13.9 d |
| M24 grade 8.8 | 358 mm | 14.9 d |

Hence the familiar **`h_ef ≥ 12·d`**. AISC DG1 Table 2.2 says the same in numbers: minimum embedment
**12 d** (F1554 Gr.36), 17 d (Gr.55), 19 d (Gr.105).

## 4. Group effect — it erases capacity silently

Anchors closer than `s_cr,N = 3·h_ef` share one cone. Eretz Barzel's floor plate
(6 × M20, spacings 156 and 81 mm, `h_ef` = 120 ⇒ `s_cr,N` = 360 mm) is far inside that:

```
A⁰c,N = 9·h_ef²           = 129,600 mm²
A_c,N = 516 × 522         = 269,352 mm²
ratio = 2.078   (not 6.0) ⇒ group efficiency 34.6 %

N_Rd,c(group) = 76.8 kN total  ≈ 12.8 kN per anchor
vs 6 × steel 8.8 = 784 kN      ⇒ about 10 % of the steel
```
**Six anchors at that spacing behave like two.** No count check and no dump will ever show this.

## 5. Shear — the finding that matters most for detailing

> **EN 1993-1-8 §6.2.2(5): where the base-plate holes are oversized, anchor bolts must NOT be counted
> in shear.** A bolt only takes shear once it *touches* the hole wall; with 8–14 mm clearance the plate
> must slip 8–14 mm first.

What is left is friction: `F_f,Rd = C_f,d · N_c,Ed` with **`C_f,d` = 0.20** (EN, sand-cement mortar)
against **μ = 0.55** (AISC DG1) — a factor of 2.75 between the two codes. **Israel works to Eurocode,
so 0.20 governs.** And friction is proportional to `N` — it vanishes exactly when wind uplift arrives.

### The grout lever-arm trap
EN 1992-4 requires the grout layer ≤ `0.5·d` (**10 mm for M20**). A normal 25–50 mm bed exceeds it, so
the anchor works in shear **plus bending**:

| Condition | `V_Rd` per M20 8.8 | Loss |
|---|---|---|
| Direct shear, no clearance | 94.1 kN | — |
| Grout 10 mm (at the limit) | 41.5 kN | 56 % |
| **Grout 30 mm, plate restrained** | **20.8 kN** | **78 %** |
| Grout 30 mm, plate free to rotate | 10.4 kN | 89 % |

**Where real shear exists, design a shear lug.** It is the only path that is stiff, predictable and
independent of the axial load.

## 6. Why an anchor hole is so much bigger than a bolt hole

It is a tolerance budget, not generosity:

| Source | Tolerance | Standard |
|---|---|---|
| Anchor position in the pour | **± 6.4 mm** | ACI 117 |
| Bolt-to-bolt within a group | ± 3.2 mm | AISC Code of Standard Practice |
| Hole drilling in the shop | ± 2 mm | EN 1090-2 |

```
d_hole = d + 2 × max deviation + assembly play
M20:    20 + 2×6.4 + 1  ≈  33 mm      = AISC DG1 Table 2.3
```

| Joint type | Bolt | Hole | Clearance |
|---|---|---|---|
| Steel-to-steel, EN 1090-2 | M20 | 22 | 2 mm |
| Anchor that must carry shear, EN 1992-4 Table 6.1 | M20 | 22 | 2 mm |
| **Eretz Barzel, lesson 4** | M20 | **23** | **3 mm** ← a *structural bolt* clearance |
| **Eretz Barzel, lesson 5** | M20 | **28** | **8 mm** |
| Anchor, AISC DG1 Table 2.3 | M20 | **33** | **13 mm** |

**⌀23 for a cast-in anchor is too tight** — it only works if the anchors are set in a steel template
that stays in the pour, or if the holes are drilled after the fact. **⌀28 is reasonable, but a standard
⌀37 washer bears only 4.5 mm on it.** A plate washer is then mandatory:
```
thickness t ≈ d/3          M20 ⇒ 7 → use 8 or 10 mm
side/dia   D ≥ 2·d_hole − d_rod       ⌀28 hole, M20 ⇒ ≥ 36 mm; in practice 60×60×10 with a ⌀22 hole
```
⚠ AISC DG1 §2.5: **a plate washer does not increase pull-out strength.** It only solves bearing on the
oversized hole.

## 7. Anchor types — and one to stop accepting

| Type | Verdict |
|---|---|
| **Headed rod / nut + washer plate** | The reference case. Every code formula is written for it. Cheapest, strongest per unit depth, most predictable |
| Nut + thick washer as the head | Mechanically equivalent — the right way for a steel shop to make its own. `N_Rk,p = k2 · A_h · f_ck`; a ⌀60 washer on M20 gives ~565 kN, never governing |
| **Hooked L / J bar** | **Do not accept.** It carries by *straightening*, not bearing — soft, creeping, unpredictable. ACI 318-19 §17.6.1 limits it arithmetically; **EN 1992-4 does not cover it at all** (no formula). It can never be ductile. If a spec shows one, propose a headed rod at the same depth |
| Post-installed expansion (wedge) | Medium; applies radial splitting pressure during setting — risky near an edge or rebar |
| Post-installed undercut | Highest of the post-installed family; true mechanical interlock |
| **Chemical / bonded** | **The right choice for plates onto existing concrete columns and walls.** No setting pressure, and it *fills the hole* ⇒ no clearance ⇒ it can carry shear. Cost: cleaning the hole is critical — a dirty hole loses up to 60 % |

`PsCreateFastener` maps one-to-one onto this list: `CreateFastenerStraightAnchorBolt` ·
`CreateFastenerHookAnchorBolt` · `CreateFastenerBendAnchorBolt` · `CreateFastenerHeadBolt` ·
`CreateFastenerHexStud` · `CreateFastenerRoundStud`.

## 8. Cracked vs uncracked

`k1` = 7.7 cracked vs 11.0 uncracked — **a 43 % difference**. "Uncracked" may only be assumed where the
concrete at the anchor is proven to stay in compression under every load combination. For foundations,
slabs and columns under moment, **assume cracked**.

## 9. Vertical faces are the harder case

| | Floor | Wall / concrete column |
|---|---|---|
| Dominant force | compression | **tension + shear** |
| Friction available | yes, ∝ N | **none** — there is no normal force |
| Edge distance | usually generous | **limited by the member width** |
| Anchor | cast-in | **post-installed, almost always** |
| Concrete state | often uncracked mid-slab | **cracked** |

Amir's three wall plates at 1500 mm pitch are **not belt-and-braces** — they change the static scheme
from a cantilever to a continuous beam over three supports, which is what makes the forces per plate
manageable.

**Supplementary reinforcement** (EN 1992-4 §7.2.1.9) lets rebar crossing the cone take the tension
instead. In a reinforced concrete column with an anchor deep enough to cross the ties, cone failure
stops governing — but that requires an explicit calculation, not an assumption.

---

## What this changes when modelling

1. **`h_ef` = 120 mm is a detailing default, not a design value.** It is correct for a pinned,
   compression-only base where the anchors are erection restraint. It gives ~10 % of the steel capacity
   in tension, with brittle failure. **Ask what `V_Ed` and the uplift are before treating it as final.**
2. **A 200–250 mm slab cannot physically hold `h_ef` = 278 mm.** That is not a failure of the detail —
   it is the fact that dictates either a pinned base or a thickened pad.
3. `PsCreateFastener` takes **`GroutThickness`** and **`Extrusion`** as arguments. Those are the exact
   quantities this study says govern the shear capacity and the nut seating — so model them knowingly.
4. `PsBaseplateLinkDataMgd.LiningThickness` **is the grout layer**. It is not cosmetic.
5. Where the drawing shows a shear lug, it is load-bearing, not stiffening. Never omit it as "extra".

## Gaps found in Eretz Barzel's current standard detail

| Gap | Why it matters |
|---|---|
| **No plate washer on the ⌀28 holes** | A standard ⌀37 washer bears 4.5 mm on a ⌀28 hole and will dish through. Needs 60×60×10 |
| **No code-recognised shear path** | Oversized holes ⇒ bolts cannot be counted; no shear lug; only `0.20·N` friction, which disappears under uplift |
| **`h_ef` = 120 mm used as a default** | Fine for compression-only; not for uplift or moment. Worth writing down as a limit, not a rule |

⚠ These are observations for the shop to consider, framed as questions — **not a design verdict.**
