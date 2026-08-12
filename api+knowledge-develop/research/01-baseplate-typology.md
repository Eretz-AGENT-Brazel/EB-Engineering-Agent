I have gathered primary sources including the full text of EN 1993-1-8:2005, EN 1090-2:2018, AISC Design Guide 1 (2nd ed.), and Wald's Eurocode background papers. Here is the brief.

---

# COLUMN BASE PLATES — TYPOLOGY, DESIGN PHILOSOPHY AND DETAILING
## Technical brief for a professional engineering report — Eretz Barzel

---

## PART 1 — THE GOVERNING ENGINEERING PRINCIPLES (the "WHY")

### 1.1 The central idea: a base plate is *not* rigid, and that changes everything

The traditional design of a column base assumed the base plate was a **rigid** slab: uniform bearing stress underneath, plane sections remain plane, linear stress block in the concrete, tension solved from equilibrium. This "has proved satisfactory in service over many years, [but] the approach ignores the flexibility of the base plate in bending (even when it is strengthened by stiffeners), the holding down assemblies and the concrete" — *Wald, CESTRUCO §7, citing DeWolf & Ricker 1990*.

The Eurocode replaced this with a **flexible-plate / equivalent-rigid-plate** model. The physical reality is:

- Under load, a thin plate **dishes**. The bearing pressure is **not** uniform — it concentrates in a band following the footprint of the column section, and dies away with distance from the flanges/web.
- Rather than model that non-uniform pressure, EN 1993-1-8 replaces the flexible plate with an **equivalent rigid plate of smaller area** (the *effective area*, `A_eff`) carrying a **uniform** pressure equal to the full joint bearing strength `f_jd`.
- The width of the strip of plate that "works" either side of the steel section is the **additional bearing width `c`**. Beyond `c`, the plate is assumed to carry nothing.

**Why a fabricator should care:** the strength of the base does *not* increase in proportion to plate area. Making the plate bigger in plan buys almost nothing once you exceed the effective area; making it **thicker** buys a lot, because `c ∝ t`. This is the single most important economic fact about base plates.

### 1.2 Derivation of `c` — the plate as a cantilever (Wald, CESTRUCO Q&A 7.1)

Take a unit-width strip of plate, cantilevering a distance `c` beyond the steel section, loaded by uniform concrete pressure `f_j`:

```
Elastic moment resistance per unit length:   M' = (1/6) · t² · f_yd          ... (7.1)
Applied moment per unit length (cantilever): M' = (1/2) · f_j · c²           ... (7.2)
Equate:                (1/2)·f_j·c² = (1/6)·t²·f_y                            ... (7.3)
                       →   c = t · sqrt( f_y / (3 · f_j · γ_M0) )             ... (7.4)
```

Note the **elastic** section modulus `t²/6`, not the plastic `t²/4`. This is deliberate: "By limiting the deformations of the base plate to the elastic range a uniform stress under the base plate may be assumed... It also ensures that the yield strength of the base plate is not exceeded." (*Wald Q&A 7.1*)

### 1.3 Why the concrete is allowed such high stress (3-D confinement)

`f_jd` can be several times `f_cd`. The reason is that the plate loads only a small patch on top of a large block: the concrete under the patch is in **triaxial compression**, and failure occurs by an **inverted pyramid punching out** — not by simple crushing.

- "In this case, the experimental resistance was about **6.25** higher than compression resistance of the concrete." (*Wald Q&A 7.4*)
- Validation against **50 tests** (DeWolf 1978; Hawkins 1968a) on cubes 150–330 mm: "The bearing capacity of test specimens at concrete failure is in the range from **1.4 to 2.5** times the capacity calculated according to prEN 1993-1-8 with an average value of **1.75**." (*Wald Q&A 7.3*) — i.e. the code is systematically conservative by ~75 %.

### 1.4 Why grout is *not* the weak link

Intuition says a 30 MPa grout under a plate stressed to 25 MPa is marginal. It isn't:

> "It was found that the thin layer of grout does not affect the resistance of the concrete in bearing. It is expected that the grout layer is in three-dimensional compression, i.e. the grout between the concrete and the base plate, is similar to a **liquid**." — *Wald Q&A 7.2*

The grout is confined on all sides by the surrounding grout and by the plate above/concrete below. It cannot fail in uniaxial crushing. This is why `β_j = 2/3` is permitted with a grout of only 0.2 × the concrete strength, and why the check reverts to a 45° spread through the grout layer when the layer is thick or weak (*Wald Fig. 7.4*).

### 1.5 Why base plates almost never develop prying — and why anchor bolts still must be checked for it

In an end-plate beam-to-column joint, the plate is thin and the bolts short, so the plate edge bears back on the flange and **prying** develops. In a base plate the plate is **thick** and the anchor bolt is **long** (embedment + grout + plate). The T-stub therefore lifts bodily off the concrete without edge contact. This produces a failure mode not present in Table 6.2 of the code, which Wald calls **Mode 1\***:

```
F_T,1-2,Rd = 2 · ℓ_eff · m_pl,Rd / m          ... (7.10)
```
(no `n` term, no prying force `Q`).

Boundary — **prying may develop only if** the bolt is short enough:
```
L_b  ≤  L_b,lim = 8.82 · m³ · A_s / ( Σℓ_eff · t³ )       ... Wald (7.11)
```
EN 1993-1-8 Table 6.11 states the same limit as `L_b ≤ 8.8·m³·A_s/(Σℓ_eff·t³)`.

**Free length of an embedded anchor** (EN 1993-1-8 Table 6.2, verbatim from the standard):
> "…the anchor bolt elongation length, taken equal to the sum of **8 times the nominal bolt diameter**, the grout layer, the plate thickness, the washer and half the height of the nut."

i.e. `L_b = 8d + t_grout + t_p + t_washer + h_nut/2`. Wald writes it as `L_b = L_bf + L_be` with `L_be = 8d`.

**Code rule that surprises people — EN 1993-1-8 §6.2.6.11(2):**
> Prying forces "should **not** be taken into consideration when determining [the] thickness of the base plate. Prying forces should be taken into account when determining the anchor bolts."

So: design the **plate** with no prying, design the **bolts** with prying.

---

## PART 2 — PINNED VS FIXED: THE TYPOLOGY AND THE REAL STIFFNESS QUESTION

### 2.1 What the two idealisations actually mean

| | **Nominally pinned base** | **Moment-resisting ("fixed") base** |
|---|---|---|
| Design intent | Transmit N and V; transmit **no significant** moment | Transmit N, V **and** M into the foundation |
| EN 1993-1-8 clause for classification | §5.2.2.2 (stiffness), §5.2.3.2 (strength) | §5.2.2.3 / §5.2.3.3 |
| Typical bolt count | **4** anchor bolts, close to the column footprint (inside the flanges or just outside), on one gauge line each side | 4–8+ bolts, **outside the flanges**, on a wide lever arm; often 2 rows per side |
| Plate thickness (typical practice) | 15–25 mm for UB/UC/HEA/HEB up to ~300 mm serial size | 30–60 mm; > 50 mm usually triggers stiffeners |
| Plate plan size | Column depth/width + ~50–100 mm each way | Governed by anchor lever arm; N + 2×(bolt edge + clearance) |
| Anchorage | Often cast-in L-bolt / hooked bar, low grade | Headed anchors or anchor plates; hooks **prohibited** above f_yb = 300 N/mm² (§6.2.6.12(5)) |
| Stiffeners | None | Often 2 or 4 vertical ribs per flange, or a full "stool" |
| Weld | Fillet all round, typically 6–10 mm | Often CJP to the flanges, fillet to the web; or CJP all round |

- **Nominally pinned, EN 1993-1-8 §5.2.2.2(1)–(2):** "shall be capable of transmitting the internal forces, without [developing] significant moments which might adversely affect the members or the structure as a whole", and "shall be capable of accepting the resulting rotations under the design loads."
- **Nominally pinned by strength, §5.2.3.2(3):** "may be classified as nominally pinned if its design moment resistance `M_j,Rd` is not greater than **0.25 times** the design moment resistance required for a full-strength joint, provided that it also has sufficient rotation capacity."
- **OSHA (USA, mandatory) — AISC DG1 §1.0:** "Recent regulations of the U.S. Occupational Safety and Health Administration… require **four anchor rods in almost all column-base-plate connections** and require all columns to be designed for a specific bending moment to reflect the stability required during erection with an ironworker on the column. This regulation has essentially eliminated the typical detail with two anchor rods except for small post-type structures that weigh less than 300 lb." Not law in Israel/EU, but universally good practice — a 2-bolt base is unstable during erection.

### 2.2 The stiffness classification of column bases — EN 1993-1-8 §5.2.2.5(2)

This is the clause that formalises "how fixed is fixed". The four sub-clauses (5.2a–5.2d) read:

**In frames where the bracing system reduces horizontal displacement by at least 80 % and where second-order effects may be neglected — the base may be classified as RIGID if:**

```
(5.2a)   λ̄₀ ≤ 0.50                    →  rigid regardless of S_j,ini
(5.2b)   0.50 < λ̄₀ < 3.93             →  S_j,ini ≥ 7·(2·λ̄₀ − 1) · E·I_c / L_c
(5.2c)   λ̄₀ ≥ 3.93                    →  S_j,ini ≥ 48 · E·I_c / L_c
```
**Otherwise (all other frames, i.e. sway/unbraced):**
```
(5.2d)   S_j,ini ≥ 30 · E·I_c / L_c
```
where
- `λ̄₀` = relative slenderness of the column assuming **both ends pinned**
- `I_c` = second moment of area of the column
- `L_c` = storey height of the column
- `E` = 210 000 N/mm²

**Background derivation** (*Wald & Jaspart, HERON 53 (2008) 1/2, pp. 69–82*):
- The 48 figure comes from a **5 % criterion on ultimate frame resistance** for a non-sway frame with the column fixed at its top: `S_j,ini ≥ 48·EI_c/L_c` (Eq. 8). If the top is pinned instead, the requirement relaxes to `40·EI_c/L_c` (Eq. 9) — the more onerous 48 is adopted.
- The 30 figure comes from a **displacement criterion** for sway frames. Wald & Jaspart tabulate the required `S̄ = S_j,ini·L_c/(EI_c)` against the permitted increase `ω` in lateral deflection relative to a truly rigid base:

| Permitted increase in sway deflection (100ω) | Required `S_j,ini` |
|---|---|
| 20 % | ≥ 15 · EI_c/L_c |
| **10 %** | **≥ 30 · EI_c/L_c** ← adopted in EN 1993-1-8 |
| 5 % | ≥ 60 · EI_c/L_c |

- The classification diagram (HERON Fig. 7; Wald JRC slide 113) marks two lines: `S_j,ini,c,n = 30·EI_c/L_c` (rigid boundary) and `12·EI_c/L_c` (the practical semi-rigid / effectively-pinned boundary), plotted for `λ̄₀ = 1.36`.

**⚠ THE CRITICAL HONEST STATEMENT — there is no pinned boundary.** Wald & Jaspart:
> "A stiffness boundary allowing to distinguish simple joints from semi-rigid ones may be [derived]… The value obtained is however **so low that all actual column bases are practically classified as semi-rigid**; therefore a semi-continuous modelling is always required. …As a consequence, **no pinned classification boundary is derived and proposed here**."

They add the pragmatic caveat: "even if a joint is semi-rigid, nothing prevents the designer to consider it as pinned, as this is [conservative for the members]."

**Practical meaning for the report:** *Every* real exposed base plate is semi-rigid. "Pinned" and "fixed" are modelling conveniences, not physical descriptions. The engineer chooses which conservative idealisation to use — and must detail consistently with the choice.

### 2.3 How big is the error? — hard numbers

Two column bases on the same HE 200 B, from Wald's own worked calibration (JRC Brussels workshop 2014, slide 111), concrete block 500×500 plan, h = 1000 mm, M24 anchor bolts of 420 mm free length:

| Base plate | Effective plan `a₁ = b₁` | `S_j,ini` |
|---|---|---|
| **t = 12 mm** | 280 × 280 mm | **7 100 kNm/rad** |
| **t = 40 mm** | 420 × 420 mm | **74 800 kNm/rad** |

**A factor of >10 in rotational stiffness from plate thickness and plan size alone.** Neither is "pinned"; neither is "fixed".

And from the fully worked moment base in §5.2 below: a 30 mm plate, 420×420, 4×M24, HE 200 B, `L_c` = 4.0 m gives

```
S_j,ini = 20 799 kNm/rad  =  6.96 · E·I_c / L_c
```
→ **6.96 < 30** (sway boundary) **and 6.96 < 12** → **semi-rigid in both braced and unbraced frames**, despite being a substantial 30 mm moment base with 4×M24. *This one number is the most persuasive item in the whole brief.*

### 2.4 What to actually put in a global analysis

Because the code gives no pinned boundary, industry guidance uses **fractional base fixity**. From SCI practice (SCI P397 §6.4; historically BS 5950 cl. 5.1.3.3), as reported by MasterSeries:

| Situation | Base stiffness to use in analysis |
|---|---|
| Nominally pinned base, ULS frame stability (`α_cr`) | **10 %** of column stiffness |
| Nominally pinned base, SLS deflections | **20 %** of column stiffness |
| Nominally pinned base, plastic collapse analysis | 0 (true pin) |

Reported effect: introducing 20 % base fixity at SLS **reduced eaves horizontal movement by ~40 %** in a portal frame, while mid-span vertical deflection changed **< 5 %**. (Base stiffness dominates *sway*, barely affects *vertical bending*.)

AISC DG1 §1.0 makes the same warning from the other direction:
> "Improper characterization can lead to error in the computed drifts, leading to **unrecognized second-order moments if the stiffness is overestimated**, or **excessive first-floor column sizes if the stiffness is underestimated**."

---

## PART 3 — EXPOSED vs POCKET vs PEDESTAL-MOUNTED

### 3.1 Exposed base plate (the default)
Plate above the concrete, separated by a grout bed, anchored by cast-in or post-installed bolts. **This is the only type EN 1993-1-8 §6.2.8 covers.**

- Strength and stiffness come from a **force couple**: concrete bearing on one side, anchor bolt tension on the other. Lever arm ≈ (column depth/2 + bolt distance) + (column depth/2 − ~c).
- Limitation: to develop a large moment you need either a very thick plate or a lot of anchorage. Kanvinde et al. (UC Davis) note exposed bases become "impractical owing to the necessity of thick base plates and numerous anchor rods" for mid- to high-rise moment frames.
- Explicitly **not covered**: "The influence of the support of the concrete foundation, which may be considerable in certain ground conditions, is not covered in prEN 1993-1-8." (*Wald §7 intro*) — i.e. soil/footing flexibility is on top of everything computed here.

### 3.2 Pocket base (embedded in a blockout / "pocket foundation")
The column stub is dropped into a formed pocket and the pocket is filled with concrete. Moment is resisted by **horizontal bearing of the embedded flanges against the pocket walls** — a couple over the embedment depth — not by anchor bolts at all.

**EN 1090-2:2018 §9.5.5 (execution — verbatim requirements):**
> "Pocket bases containing columns shall be filled with **dense concrete having a characteristic compressive strength not less than that of the surrounding concrete**."
> "In pocket bases, the embedded length of the column shall be initially surrounded with concrete to a sufficient length to provide stability in the temporary state and then **remain undisturbed for a period sufficient to gain at least half of its characteristic compressive strength**, before removal of any temporary props and wedges."

Pocket/keyed connections for concrete columns are covered by EN 1992-1-1 §10.9.6 (smooth, rough or keyed internal walls); EN 14991 covers precast foundation elements.

**Advantages:** very high fixity, no thick plate, no big anchor cage, cheap steelwork.
**Disadvantages:** wet trade on the critical path, formwork for the pocket, corrosion at the concrete/steel interface line, no adjustment after the second pour, difficult inspection.

### 3.3 Embedded column base (ECB) — the seismic/high-rise variant
The bottom of the column *and its base plate* are cast into the footing, usually with a bearing/stiffener plate at the top of the embedment. Moment resisted by **vertical + horizontal bearing** of the embedded segment against the concrete.

- Terminology: **shallowly embedded** (Barnwell 2015) vs **deeply embedded** (Grilli & Kanvinde 2015). Full-scale tests used ~3 m column stubs cast into footings.
- **Rotational capacity from tests: 0.03–0.08 rad** for embedded bases; **0.04–0.10** for exposed base plates. Both types are ductile.
- Key finding: even bases designed as "strong bases" (stronger than the adjacent column) "**exhibit significant flexibility. However, they are typically simulated as fixed… and this results in unrealistically optimistic estimates of building response**" (*Torres Rodas, UC Irvine dissertation*).

### 3.4 Pedestal-mounted (base plate on a concrete pier/stub column)
Structurally identical to exposed, but the **concentration factor is limited** because the pedestal is not much larger than the plate.

- **AISC DG1 §3.1.4**: three cases — Case I `A₂ = A₁`; Case II `A₂ ≥ 4A₁`; Case III `A₁ < A₂ < 4A₁`. "Base plates resting on piers often meet the case that A₂ is larger than A₁ but less than 4A₁, which leads to Case III." In Case III both `A₁` and `A₂` are unknown → iterate.
- **DG1 §2.7 fabrication trap:** "Anchor rods in piers should **never** extend below the bottom of the pier into the footing because this would require that the anchor rods be partially embedded prior to forming the pier, which makes it almost impossible to maintain alignment. When the pier height is less than the required anchor rod embedment length, **the pier should be eliminated** and the column extended to set the base plate on the footing."

---

## PART 4 — ALL FORMULAS, WITH SYMBOL DEFINITIONS

### 4.1 EN 1993-1-8:2005 — Equivalent T-stub in COMPRESSION (§6.2.5)

**§6.2.5(1)** — the T-stub in compression models two components together:
> "the steel base plate in bending under the bearing pressure on the foundation; the concrete and/or grout joint material in bearing."

```
(6.4)  F_C,Rd  =  f_jd · b_eff · ℓ_eff

(6.5)  c  ≤  t · sqrt( f_y / ( 3 · f_jd · γ_M0 ) )

(6.6)  f_jd  =  β_j · F_Rdu / ( b_eff · ℓ_eff )
```

| Symbol | Definition | Value / note |
|---|---|---|
| `F_C,Rd` | design compression resistance of the T-stub | N |
| `b_eff`, `ℓ_eff` | effective width and length of the equivalent T-stub flange | **notional**, not physical (§6.2.5 NOTE) |
| `f_jd` | design bearing strength of the joint | N/mm² |
| `c` | additional bearing width | mm |
| `t` | T-stub flange thickness = **base plate thickness** | mm |
| `f_y` | yield strength of the base plate | 235 / 275 / 355 N/mm² |
| `γ_M0` | partial factor, steel cross-sections | 1.00 (recommended) |
| `β_j` | foundation **joint material coefficient** | **2/3** (see 4.2) |
| `F_Rdu` | concentrated design resistance from EN 1992-1-1 §6.7, with `A_c0 = b_eff·ℓ_eff` | N |

**§6.2.5(5)–(6) — the two geometric cases (Fig. 6.4):**
- (a) *Short projection*: physical projection of the plate beyond the section **< c** → effective area is truncated at the physical plate edge.
- (b) *Large projection*: physical projection **> c** on any side → "the part of the additional projection beyond the width `c` should be **[neglected]**".

**§6.2.8.2 — column base under axial force only (Figure 6.19):**
> "The design resistance… may be determined by adding together the individual design resistance of the **three T-stubs**… (Two T-stubs under the column flanges and one T-stub under the column web.) **The three T-stubs should not be overlapping.**"

For an I/H section, non-overlapping:
```
A_eff = 2·(b_c + 2c)·(t_f + 2c)  +  (h_c − 2t_f − 2c)·(t_w + 2c)
```
each factor additionally limited by the physical plate dimensions.

### 4.2 The joint coefficient β_j and the grout rule — EN 1993-1-8 §6.2.5(7), verbatim

> "`β_j` is the foundation joint material coefficient, which may be taken as **2/3** provided that the characteristic strength of the grout is **not less than 0.2 times** the characteristic strength of the concrete foundation and the thickness of the grout is **not greater than 0.2 times the smallest width of the steel base plate**. In cases where the thickness of the grout is **more than 50 mm**, the characteristic strength of the grout should be **at least the same** as that of the concrete foundation."

Compactly:
```
β_j = 2/3     if    f_ck,g ≥ 0.2 · f_ck      AND    t_g ≤ 0.2 · min(a ; b)
              and, if t_g > 50 mm, additionally  f_ck,g ≥ f_ck
```
Where those are not met: **check the grout layer separately**, as a plate on a 45° stress spread through the grout (*Wald Fig. 7.4*), i.e. treat the grout like a second concrete block loaded over `(b_eff + 2t_g)·(ℓ_eff + 2t_g)`.

**⚠ Note on β_j = 1.0:** Wald's JRC slide 82 states that where the grout is of *higher* quality than the concrete block, `β_j` may be taken between **2/3 and 1.0** (the grout layer may be neglected — Stark & Bijlaard 1988). EN 1993-1-8 itself only gives 2/3. Use 2/3 for design; mention 1.0 only as background.

### 4.3 Concentrated bearing — EN 1992-1-1 §6.7

```
F_Rdu = A_c0 · f_cd · sqrt( A_c1 / A_c0 )   ≤   3.0 · f_cd · A_c0
```
with (per Wald's JRC restatement of EN 1992-1-1 Fig. 6.29):
```
A_c0 = b₁ · d₁          (the loaded area = b_eff · ℓ_eff)
A_c1 = b₂ · d₂          (the design distribution area, same shape, concentric)
h ≥ (b₂ − b₁)   and   h ≥ (d₂ − d₁)          [45° / 1:2 spread through the block depth h]
b₂ ≤ 3·b₁      and   d₂ ≤ 3·d₁
```
Defining the **concentration factor**:
```
k_j = sqrt( A_c1 / A_c0 ) = sqrt( (a₁·b₁) / (a·b) )       →      f_jd = β_j · k_j · f_cd
```

**⚠⚠ SOURCES DISAGREE — SAY THIS EXPLICITLY IN THE REPORT:**

| Reading | Cap applied to | Resulting max `f_jd` | Source |
|---|---|---|---|
| **(A)** Cap the square root: `k_j ≤ 3.0` | `k_j` | `f_jd ≤ (2/3)·3.0·f_cd = ` **2.00 f_cd** | Strict reading of EN 1992-1-1 §6.7 as invoked by EN 1993-1-8 §6.2.5(7); IDEA StatiCa implements `k_j = √(A_c1/A_eff) ≤ 3.0` |
| **(B)** Cap the product: `f_jd ≤ 3.0 f_cd` | `f_jd` | **3.00 f_cd** | Wald's own EC3 worked examples (JRC 2014, slides 117 & 123): "`f_jd = β_j·√(A_c1/A_c0)·f_cd ≤ 3.0 f_cd`" — his moment example returns `f_jd = 24.0 MPa` with `f_cd = 10.67 MPa`, i.e. `2.25 f_cd` (allowed under B, **not** allowed under A) |
| **(C)** ENV 1993-1-1 Annex L / prEN 2003 legacy: `a₁ ≤ 5a`, so `k_j ≤ 5.0` | `k_j` | `f_jd ≤ (2/3)·5·f_cd = ` **3.33 f_cd** | Wald Q&A 7.3/7.4 (eqs. 7.5a, 7.5b, 7.6, 7.7); superseded but still found in older software |

**Recommendation for the report:** use **(A)**, `f_jd ≤ 2.0 f_cd`, for compliant design under EN 1993-1-8:2005 + EN 1992-1-1; note (B) and (C) as background and as the reason legacy calculations show higher values. The difference is a factor of **1.5–1.67 on the compressive capacity of the base** — this is not a rounding issue.

**Legacy (ENV/prEN, Wald eqs. 7.5–7.7)** — retained here because it is what many older Israeli/European calculation sheets contain:
```
a₁ = min( a + 2·a_r ; 5a ; a + h ; 5·b₁ )  ,  a₁ ≥ a
b₁ = min( b + 2·b_r ; 5b ; b + h ; 5·a₁ )  ,  b₁ ≥ b
k_j = sqrt( a₁·b₁ / (a·b) )
f_j = (2/3) · k_j · f_ck / γ_C
```
where `a`, `b` = base plate dimensions; `a_r`, `b_r` = edge distances from plate to block edge; `h` = block depth.

### 4.4 EN 1993-1-8 — Equivalent T-stub in TENSION, base-plate specific

**Effective lengths, bolts INSIDE the column flanges (Wald CESTRUCO Table 7.2):**

| Prying occurs | No prying (base plates) |
|---|---|
| `ℓ₁ = 2αm − (4m + 1.25e)` | `ℓ₁ = 2αm − (4m + 1.25e)` |
| `ℓ₂ = 2πm` | `ℓ₂ = 4πm` |
| `ℓ_eff,1 = min(ℓ₁ ; ℓ₂)` ; `ℓ_eff,2 = ℓ₁` | `ℓ_eff,1 = min(ℓ₁ ; ℓ₂)` ; `ℓ_eff,2 = ℓ₁` |

**Effective lengths, bolts OUTSIDE the column flanges (Wald CESTRUCO Table 7.1; also JRC slide 91):**

| | Prying occurs | No prying |
|---|---|---|
| ℓ₁ | `4m_x + 1.25e_x` | `4m_x + 1.25e_x` |
| ℓ₂ | `2π m_x` | `4π m_x`  ⚠ *JRC slide 91 prints `2π m_x` here — CESTRUCO Table 7.1 gives `4π m_x`; use `4π m_x`, consistent with the Mode 1\* doubling* |
| ℓ₃ | `0.5 b` (b = plate width) | `0.5 b` |
| ℓ₄ | `2m_x + 0.625e_x + 0.5p` | `2m_x + 0.625e_x + 0.5p` |
| ℓ₅ | `2m_x + 0.625e_x + e` | `2m_x + 0.625e_x + e` |
| ℓ₆ | `π m_x + 2e` | `2π m_x + 4e` |
| ℓ₇ | `π m_x + p` | `2π m_x + 2p` |
| | `ℓ_eff,1 = min(ℓ₁…ℓ₇)` ; `ℓ_eff,2 = min(ℓ₁,ℓ₃,ℓ₄,ℓ₅)` | same |

Symbols: `m_x` = bolt to weld toe (in the projecting direction), `e_x` = bolt to plate edge (projecting direction), `e` = bolt to plate edge (transverse), `p` = bolt pitch, `b` = plate width, `α` from the standard α-chart.

**`m` with a fillet weld (Wald JRC slide 122):** `m = e_c − 0.8·√2·a_w`, where `a_w` is the fillet weld throat.

**T-stub tension resistances:**
```
Mode 1  (complete flange yielding, prying):     F_T,1,Rd = 4·M_pl,1,Rd / m
Mode 1* (no contact — BASE PLATES):             F_T,1-2,Rd = 2·M_pl,1,Rd / m
Mode 2  (bolt failure + flange yielding):       F_T,2,Rd = (2·M_pl,2,Rd + n·ΣF_t,Rd)/(m + n)
Mode 3  (bolt failure):                         F_T,3,Rd = Σ F_t,Rd
with  M_pl,1,Rd = 0.25 · Σℓ_eff,1 · t² · f_y / γ_M0
      n = e_min  but  n ≤ 1.25 m
```

**Yield lines for bolts outside the column flange corner (Wald Q&A 7.6, virtual work):**
```
Internal energy:   W_i = m_pl · ( xy/x + xy/y )   ... (7.12)
External energy:   W_e = F_pl · δ                  ... (7.13)
Virtual displacement:  δ = c · sqrt(x² + y²) / (x·y)   ... (7.15)
→  F_pl = m_pl · c · (x² + y²)^(1/2) ... 
→  ℓ_eff = c · (x² + y²)^(1/2) / (x·y)  · (x·y)   ... (7.19)
```
with `x`, `y` = bolt coordinates from the plate corner, `c` = perpendicular distance from the corner to the yield line, `α` = yield-line deviation angle. Five distinct corner yield-line patterns identified and confirmed by FE (Wald Fig. 7.12, Table 7.3); Cases 4 and 5 mirror Cases 2 and 1.

Case 1 (fan): `ℓ_eff,1 = π·m`
Case 2 (straight): `ℓ_eff,2 = b/4`
Case 3 (corner): `ℓ_eff,3 = [ (a−a_c) + (b−b_c) ] / 8 · ( e_a/e_b + e_b/e_a )`

### 4.5 EN 1993-1-8 §6.2.8.3 — moment resistance of a column base (Table 6.7)

Components on each side:
- **Tension side** `F_T,Rd` = min( column web in tension §6.2.6.3 ; base plate in bending under tension §6.2.6.11 )
- **Compression side** `F_C,Rd` = min( concrete in compression under the flange §6.2.6.9 ; column flange & web in compression §6.2.6.7 )

Lever arms (`z_T,l`, `z_C,l`, `z_T,r`, `z_C,r` measured from the column neutral axis, Fig. 6.18); `e = M_Ed / N_Ed`.

Table 6.7, four load quadrants; e.g. for **left in tension / right in compression** (`N_Ed ≤ 0`, `e > z_T,l`), with `z = z_T,l + z_C,r`:
```
M_j,Rd = min [  F_T,l,Rd · z / ( z_T,l/e + 1 )  ;  F_C,r,Rd · z / ( z_C,r/e − 1 ) ]
```
Sign convention (verbatim): "`M_Ed` > 0 is clockwise, `N_Ed` > 0 is tension."

**§6.2.8.3(1) important simplification:** "the contribution of the concrete portion **just under the column web** (T-stub 2 of Figure 6.19) to the [compressive] capacity is **omitted**." So for moment, only the flange T-stubs count — unlike the pure-axial case.

**§6.2.6.12(2) — the lever arm limit:** "When calculating the tension forces in the anchor bolts due to bending moments, the lever arm should not be taken as more than the distance between the centroid of the bearing area on the compression side and the centroid of the bolt group on the tension side. **NOTE: Tolerances on the positions of the anchor bolts may have an influence.**"

### 4.6 EN 1993-1-8 §6.3.4 + Table 6.11/6.12 — ROTATIONAL STIFFNESS

**Component stiffness coefficients (Table 6.11), units of length (mm):**

```
k13  (concrete in compression, including grout)
     k13 = E_c · sqrt( b_eff · ℓ_eff ) / ( 1.275 · E )

k15  (base plate in bending under tension, per bolt row)
     with prying:     k15 = 0.85 · ℓ_eff · t_p³ / m³
     without prying:  k15 = 0.425 · ℓ_eff · t_p³ / m³

k16  (anchor bolts in tension)
     with prying:     k16 = 1.6 · A_s / L_b
     without prying:  k16 = 2.0 · A_s / L_b

k14  (base plate in bending under compression) = ∞   ["already taken into consideration in k13"]
k19  (welds) = ∞
```

**Table 6.11 NOTE 1 (verbatim, and hugely practical):**
> "When calculating `b_eff` and `ℓ_eff` the distance `c` should be taken as **1.25 times the base plate thickness**."

i.e. for **stiffness** you do NOT re-solve eq. (6.5); you simply use `c = 1.25·t_p`. This is the derived elastic equivalent (see 4.7).

**Table 6.12 — assembly:**
```
1/k_T,l = 1/k15,l + 1/k16,l        (tension, left)
1/k_T,r = 1/k15,r + 1/k16,r        (tension, right)
k_C,l = k13,l   ;   k_C,r = k13,r  (compression)
```
For left in tension / right in compression, `z = z_T,l + z_C,r`:
```
S_j,ini  =  e · z² · E  /  [  ( e + e_k ) · ( 1/k_T,l + 1/k_C,r )  ]
with      e_k = ( z_C,r · k_C,r − z_T,l · k_T,l ) / ( k_C,r + k_T,l )
          e   = M_Ed / N_Ed
```
Generalised (Wald HERON, eqs. 9–13):
```
e₀ = ( z_c,r·k_c,r − z_t,l·k_t,l ) / ( k_c,r + k_t,l )       ... (9)   [eccentricity of zero rotation]
S_j,ini = e · E · z² / [ (e + e₀) · Σ(1/k_i) ]                ... (11)
μ = ( 1.5 · M_Sd / M_Rd )^2.7   ≥ 1                            ... (12)   [shape factor]
S_j = e · E · z² / [ (e + e₀) · μ · Σ(1/k_i) ]                 ... (13)
```

**§5.1.2(3)–(4):** if `M_j,Ed ≤ (2/3)·M_j,Rd`, use `S_j,ini`. As a simplification use `S_j,ini/η` for all moment values, with `η` from Table 5.2.

### 4.7 Where the "1.275" and the "1.25 t" come from (Steenhuis, Wald, Sokol & Stark, HERON 53 (2008) 51–68)

Rigid rectangular plate on an elastic half-space (Lambe & Whitman):
```
δ_r = α · F · a_rig / ( E_c · A_r )     ... (10)      with  α ≈ 0.85·sqrt(L/a_rig)  for ν ≈ 0.15
```
Flexible → equivalent rigid conversion:
```
c_fl = t · ( π² · ξ · E_c / (12 E) )^(1/4)  ≈ 1.98 t     ... (20)   [for E_c ≈ 30 GPa, E = 210 GPa, ξ = 2.2]
c_r  = c_fl · 2/π  =  1.25 · t                            ... (21)   ← the Table 6.11 NOTE 1 rule
a_eq,el = t_w + 2·c_r + ... = t_w + 2.5 · t               ... (22)
```
Surface-quality/grout reduction factor **1.5** (tests showed reductions of **1.0 to 1.55**; a 30 mm-deep degraded upper concrete layer was proposed by Sokol & Wald). Hence:
```
k_c = E_c · a_eq,el · L / ( 1.5 · 0.85 · E )  =  E_c · a_eq,el · L / ( 1.275 · E )     ... (23)
```
**`1.275 = 1.5 × 0.85`** — 1.5 is the workmanship penalty, 0.85 is the half-space shape factor. Tell the fabricator this: **base stiffness is explicitly penalised by 50 % for surface quality.** Clean, well-filled grout is a structural requirement, not housekeeping.

### 4.8 Shear transfer — EN 1993-1-8 §6.2.2(6)–(8)

Four mechanisms (Wald Fig. 7.14): (a) friction plate/grout/footing; (b) shear + bending of anchor bolts; (c) a shear key / shear lug (I-stub, T-section or pad welded under the plate); (d) direct contact by recessing the plate into the concrete.

```
Friction:        F_f,Rd = C_f,d · N_c,Ed          [ = 0 if the column is in tension ]
Bolt in shear:   F_2,vb,Rd = α_b · f_ub · A_s / γ_M2 ,   α_b = 0.44 − 0.0003 · f_yb
                 (f_yb ≤ 640 N/mm²;  also check F_1,vb,Rd per §3.6.1;  take the smaller)
Combined:        F_v,Rd = F_f,Rd + n · F_vb,Rd     ... (6.3)      [n = number of anchor bolts]
```

**Friction coefficient — SOURCES DISAGREE:**

| Value | Condition | Source |
|---|---|---|
| **0.20** | sand-cement mortar | **EN 1993-1-8:2005 §6.2.2(6)** (final published text) |
| — | "for other types of grout the coefficient of friction should be determined by testing in accordance with EN 1990, Annex D" | **EN 1993-1-8:2005 §6.2.2(6)** |
| 0.30 | "special grout" | prEN 1993-1-8:2003 / Wald Q&A 7.7 — **this value was removed** from the final EN |
| 0.40 | thin grout layer < 3 mm, with `γ_Mf = 1.5` | CEB Guide 1997 / Eligehausen 1990, via Wald Q&A 7.7 |

**Wald Q&A 7.9 — crucial detailing caveat:** "**Only the anchor bolts in the compressed part of the base plate** may be used to transfer shear force." And on bolt bending (CEB model, Fig. 7.15): "the anchor bolts will act as a cantilever of span equal to the thickness of the grout increased by 0.5 d. When rotation of the nut is prevented by the base plate, the span is reduced to L/2."

**EN 1992-4:2018 (via Hilti technical note, Eq. 1) — grout thickness directly reduces shear:**
```
V_Rk,s = ( 1 − 0.01 · t_grout ) · k₇ · V⁰_Rk,s        [ t_grout in mm ]
```
→ a **40 mm grout bed costs 40 % of the anchor shear capacity.** Validity conditions (EN 1992-4 §6.2.2.3(2)):
1. at least two fasteners spaced ≥ 10d resist shear in the direction of the force;
2. **no bending moment or net tension** on the connection;
3. `t_grout ≤ min( 40 mm ; 5d )` (5d₀ for sleeve anchors);
4. grout completely fills the void;
5. **grout compressive strength ≥ concrete strength and ≥ 30 N/mm²**.

Hilti's SOFA method (adopting ACI 318 §17.7.1.2.1) instead uses `V_Rk,s,grout = 0.8·k₇·V⁰_Rk,s`, constant with thickness, relaxed to grout pads up to **100–130 mm**.

**Anchor bending (EN 1992-4:2018 Eq. 7.37; numbers from the RPP technical manual):**
```
V_Rk,s = α_M · M_Rk,s / ℓ_a
M_Rk,s = M⁰_Rk,s · ( 1 − N_Sd / N_Rd,s )
M⁰_Rk,s = 1.2 · W_el · f_uk ,     W_el = π·d³/32 ,     α_M = 2.0 (nut rotation restrained)
γ_Ms = 1.25
```

### 4.9 Anchorage — EN 1993-1-8 §6.2.6.12 and the CEB/EN 1992-4 route

**§6.2.6.12(3):** resistance = smaller of the bolt tension resistance (§3.6) and the **bond** resistance per EN 1992-1-1.
**§6.2.6.12(4):** four permitted fixings — a hook, a washer plate, another embedded load-distributing member, or a tested/approved fixing.
**§6.2.6.12(5) (hard prohibition):** "When the bolts are provided with a hook, the anchorage length should be such as to prevent bond failure before yielding of the bolt… **This type of anchorage should not be used for bolts with a yield strength f_yb higher than 300 N/mm².**" → **no hooked bars with grade 5.6 / 8.8 / 10.9 anchors.**
**§6.2.6.12(6):** with a washer plate or other load-distributing member, **"no account should be taken of the contribution of bond. The whole of the force should be transferred through the load distributing device."**

**Bolt tension (Wald eq. 7.21, EN 1993-1-8 Table 3.4):**
```
N_Sd ≤ F_t,Rd = 0.9 · β_b · A_s · f_ub / γ_Mb        β_b = 0.85 for cut threads (if applicable)
```

**Four failure modes to check for a cast-in headed anchor (Wald Q&A 7.10; now EN 1992-4:2018 §7.2.1):**
```
steel:           N_Rd,s  = A_s · f_yb / γ_Mb            ... (7.22)
pull-out:        N_Rd,p  = N_Rk,p / γ_Mp = p_k·A_h/γ_Mp ,   p_k = 11.0·f_ck (non-cracked)   ... (7.26–7.27)
                 A_h = π(d_h² − d²)/4  (circular head)  or  a_h² − πd²/4  (square head)      ... (7.28)
concrete cone:   N_Rd,c = N⁰_Rd,c · (A_c,N / A⁰_c,N) · Ψ_s,N · Ψ_ec,N · Ψ_re,N · Ψ_ucr,N      ... (7.29)
                 N_Rk,c = k₁ · f_ck^0.5 · h_ef^1.5 / γ_Mc ,   k₁ = 11 (N/mm)^0.5 non-cracked  ... (7.30)
                 p_cr,N ≈ 3.0 · h_ef                                                          ... (7.32)
                 Ψ_s,N = 0.7 + 0.3·e/e_cr,N ≤ 1                                                ... (7.33)
                 Ψ_ucr,N = 1.4  (non-cracked concrete)
splitting:       N_Sd ≤ N_Rd,sp = N_Rk,sp / γ_Msp                                              ... (7.25)
```
Detailing limits to suppress splitting (Wald 7.34–7.36):
```
spacing        p_min = min( 5·d_h ; 50 mm )
edge distance  e_min = min( 3·d_h ; 50 mm )
block height   h_min = h_ef + t_h + c_∅
splitting check may be omitted if  e > 0.5 · h_ef  in all directions
```

### 4.10 AISC Design Guide 1 (2nd ed., 2006) — the American cantilever/yield-line route

**Concrete bearing (AISC 360 §J8, ACI 318 §10.17):**
```
Eq. J8-1 (full area):      P_p = 0.85 · f'_c · A₁
Eq. J8-2 (larger support): P_p = 0.85 · f'_c · A₁ · sqrt( A₂/A₁ )  ≤  1.7 · f'_c · A₁
Stress form:               f_p(max) = 0.85·f'_c·sqrt(A₂/A₁)  ≤  1.7·f'_c
                           sqrt(A₂/A₁) ≤ 2   ⟺   A₂ ≤ 4A₁
φ = 0.60 (AISC Spec J8)  /  0.65 (ACI 318 §9.3)  —  DG1 authors recommend 0.65
Ω = 2.50 (ASD)
```
> "This apparent conflict exists due to an oversight in the AISC Specification development process. The authors recommend the use of the ACI-specified φ factor in designing column base plates." — *DG1 §3.1.1*

**Base plate yielding — the three cantilevers (DG1 §3.1.2, after Thornton 1990; Drake & Elkin 1999):**
```
m  = ( N − 0.95·d ) / 2                 [cantilever parallel to the web, past 0.95×column depth]
n  = ( B − 0.80·b_f ) / 2               [cantilever parallel to the flange, past 0.80×flange width]
n' = sqrt( d · b_f ) / 4                [yield-line cantilever for the region BETWEEN the flanges]

X  = [ 4·d·b_f / (d + b_f)² ] · ( P_u / (φ_c·P_p) )      (LRFD)
   = [ 4·d·b_f / (d + b_f)² ] · ( Ω_c·P_a / P_p )        (ASD)
λ  = 2·sqrt(X) / ( 1 + sqrt(1 − X) )  ≤ 1               [conservative to take λ = 1.0]

ℓ  = max( m , n , λ·n' )

t_min = ℓ · sqrt( 2·P_u / ( 0.90 · F_y · B · N ) )       (LRFD)
t_min = ℓ · sqrt( 2·Ω·P_a / ( F_y · B · N ) ) , Ω=1.67   (ASD)
```
Symbols: `N` = plate length (in the direction of `d`), `B` = plate width, `d` = column depth, `b_f` = column flange width, `F_y` = plate yield stress, `P_u`/`P_a` = required axial compression.

**Plan optimisation (DG1 §3.1.4):**
```
Δ = ( 0.95·d − 0.80·b_f ) / 2 ;    N ≈ sqrt(A₁,req) + Δ ;    B = A₁,req / N
```
> "Since ℓ is the maximum value of m, n and λn′, the thinnest base plate can be found by minimizing m, n and λ. This is usually accomplished by proportioning the base plate dimensions so that **m and n are approximately equal**."

**HSS / Pipe (DG1 §3.1.3):** rectangular HSS — both m and n computed with yield lines at **0.95×** depth and width; round HSS/Pipe — both at **0.80×** diameter; **λ is not used**.

**Small moment (DG1 §3.3), `e ≤ e_crit`:**
```
e = M_r / P_r ;      q_max = f_p(max) · B ;      e_crit = N/2 − P_r/(2·q_max)
Y = N − 2e ;         f_p = P_r / ( B·(N − 2e) )
t_p(req) = 1.5 · m · sqrt( f_p / F_y )                     (LRFD, Y ≥ m)     [ASD: 1.83·m·sqrt(f_p/F_y)]
t_p(req) = 2.11 · sqrt( f_p · Y·(m − Y/2) / F_y )          (LRFD, Y < m)     [ASD: 2.58·sqrt(...)]
```
(substitute `n` for `m` when `n` governs)

**Large moment (DG1 §3.4), `e > e_crit`:**
```
T = q_max · Y − P_r                                     [anchor rod required tension]
Y = ( f + N/2 ) ± sqrt[ ( f + N/2 )² − 2·P_r·(e + f)/q_max ]
Real solution requires:   ( f + N/2 )²  ≥  2·P_r·(e + f)/q_max      ... (3.4.4)
```
where `f` = distance from column centreline to the anchor rod centreline. If (3.4.4) fails → **enlarge the plate**.

---

## PART 5 — WORKED EXAMPLES

### 5.1 WORKED EXAMPLE A — Eurocode, nominally pinned base, axial compression only
*(Data from Wald, JRC Eurocodes Workshop, Brussels 2014, slides 116–119; recomputed here)*

**Given**
- Column **HE 200 B**: h_c = 200 mm, b_c = 200 mm, t_f = 15 mm, t_w = 9 mm
- Base plate **340 × 340 × 18 mm**, **S235** (f_y = 235 N/mm²)
- Concrete block **850 × 850 × 900 mm**, **C12/15**
- γ_C = 1.50, γ_M0 = 1.00

**Step 1 — concrete design strength**
```
f_cd = f_ck/γ_C = 12/1.5 = 8.00 N/mm²
k_j = sqrt(A_c1/A_c0) = sqrt(850·850 / 340·340) = 850/340 = 2.50
f_jd = β_j · k_j · f_cd = (2/3) · 2.50 · 8.00 = 13.33 N/mm²
Check cap:  2.0·f_cd = 16.0 N/mm²  (reading A)    →  13.33 < 16.0    ✔ not governing
(Wald's slide quotes the cap as 3.0·f_cd = 24.0 N/mm² — reading B; here it makes no difference)
```

**Step 2 — additional bearing width**
```
c = t · sqrt( f_y / (3·f_jd·γ_M0) ) = 18 · sqrt( 235 / (3 · 13.33 · 1.00) )
  = 18 · sqrt(5.875) = 18 × 2.424 = 43.6 ≈ 43.7 mm         [matches source]
```
Check the plate is big enough for full `c`: overhang = (340 − 200)/2 = **70 mm > 43.7 mm** ✔ (large-projection case, §6.2.5(6): the extra 26.3 mm each side is **neglected**).

**Step 3 — effective area, three non-overlapping T-stubs (§6.2.8.2, Fig. 6.19)**
```
Flange T-stubs (×2):   ℓ_eff = b_c + 2c = 200 + 87.4 = 287.4 mm   (≤ 340 ✔)
                       b_eff = t_f + 2c = 15 + 87.4 = 102.4 mm
                       inner edge at 100 − 15 − 43.7 = 41.3 mm from the axis → no overlap ✔
Web T-stub:            b_eff = h_c − 2t_f − 2c = 200 − 30 − 87.4 = 82.6 mm
                       ℓ_eff = t_w + 2c = 9 + 87.4 = 96.4 mm

A_eff = 2·(287.4 × 102.4) + (82.6 × 96.4) = 58 860 + 7 963 = 66 823 mm²
```

**Step 4 — resistance**
```
N_j,Rd = A_eff · f_jd = 66 823 × 13.33 = 890 700 N ≈ 891 kN
```

> ⚠ **Source discrepancy, report it:** Wald's slide 119 quotes `A_eff = 72 266 mm²` (→ ≈ 963 kN) using a *wrapped-perimeter* effective area (the full rectangle `(b_c+2c)×(h_c+2c)` less the two re-entrant notches) rather than the three non-overlapping T-stubs of §6.2.8.2. The wrapped method is **~8 % less conservative**. For code compliance use the three-T-stub value.

**Sanity check on utilisation:** the plate is 340×340 = 115 600 mm² but only 66 823 mm² (**58 %**) is structurally active. Enlarging the plate to 400×400 would add **zero** capacity. Going from 18 mm to 25 mm plate raises c to 60.7 mm and A_eff to ≈ 87 000 mm² → **N_Rd ≈ 1 160 kN, +30 % for 39 % more steel weight on one small plate.** This is the message for the fabricator.

---

### 5.2 WORKED EXAMPLE B — Eurocode, moment-resisting base: resistance AND stiffness AND classification
*(Data and results from Wald, JRC Brussels 2014, slides 120–129; all steps independently re-verified below)*

**Given**
- Column **HE 200 B** (h_c = 200, b_c = 200, t_f = 15, t_w = 9), `I_c = 56.96 × 10⁶ mm⁴`, storey height **L_c = 4.0 m**
- Base plate **420 × 420 × 30 mm**, **S235**
- Concrete block **1600 × 1600 × 1000 mm**, **C16/20**
- **4 × M24 anchor bolts**, A_s = 353 mm², f_ub = 360 N/mm², free length L_b = 261.5 mm
- Bolt geometry: `e_c` = 60 mm (bolt to column face), `e_a` = 50 mm, `e_b` = 90 mm, pitch `p` = 240 mm
- Fillet weld `a_w` = 6 mm
- Applied axial force **F_Ed = 500 kN**
- γ_C = 1.50, γ_M0 = 1.00, γ_Mb = 1.25

**Step 1 — tension component: anchor bolts**
```
F_T,3,Rd = 2 · 0.9 · f_ub · A_s / γ_Mb = 2 × 0.9 × 360 × 353 / 1.25 = 183 000 N = 183.0 kN
```

**Step 2 — tension component: base plate in bending**
```
m = e_c − 0.8·√2·a_w = 60 − 0.8 × 1.414 × 6 = 60 − 6.79 = 53.2 mm

ℓ₁ = 4m + 1.25·e_a    = 4(53.2) + 1.25(50)              = 275.3 mm
ℓ₂ = 4π·m             = 4π(53.2)                        = 668.6 mm
ℓ₃ = 0.5·b            = 0.5(420)                        = 210.0 mm   ← GOVERNS
ℓ₄ = 0.5p + 2m + 0.625·e_b = 120 + 106.4 + 56.25        = 282.7 mm
ℓ₅ = e_a + 2m + 0.625·e_b  = 50 + 106.4 + 56.25         = 212.7 mm
ℓ₆ = 2π·m + 4·e_b     = 334.3 + 360                     = 694.3 mm
ℓ₇ = 2π·m + 2p        = 334.3 + 480                     = 814.3 mm

ℓ_eff,1 = 210 mm

F_T,1-2,Rd = 2·M_pl,1,Rd / m = 2 · (0.25 · 210 · 30² · 235 / 1.0) / 53.2
           = 2 × 11 103 750 / 53.2 = 417 400 N = 417.4 kN
```
*(The source slide prints 370.0 kN, corresponding to m = 60 mm rather than 53.2 mm — an internal inconsistency in the slide. Either way the plate does not govern.)*

```
F_T,Rd = min( 183.0 ; 417.4 ) = 183.0 kN     →  ANCHOR BOLTS GOVERN
```

**Step 3 — compression component: concrete**
```
f_cd = 16/1.5 = 10.67 N/mm²
Wald (slide 123):  k_j = 1420/420 = 3.381  →  f_jd = (2/3)(3.381)(10.67) = 24.0 N/mm²  [reading B]
Strict EN 1992-1-1 (reading A):  k_j ≤ 3.0  →  f_jd = (2/3)(3.0)(10.67) = 21.3 N/mm²
```
> ⚠ Report both. The remainder of this example follows the source with `f_jd = 24.0 N/mm²`. With `f_jd = 21.3 N/mm²` the required `A_eff` rises to 32 070 mm² and `M_j,Rd` falls to ≈ 101 kNm (only −2 %), because the tension side governs.

**Step 4 — force equilibrium (plastic distribution)**
```
F_Ed + F_T,Rd = A_eff · f_jd
A_eff = (500 000 + 183 000) / 24.0 = 28 458 mm²
```

**Step 5 — effective width on the compression side**
```
c = t · sqrt( f_y/(3·f_jd·γ_M0) ) = 30 · sqrt( 235/(3 × 24.0) ) = 30 × 1.807 = 54.2 mm
ℓ_eff = b_c + 2c = 200 + 108.4 = 308.4 mm
b_eff = A_eff / ℓ_eff = 28 458 / 308.4 = 92.3 mm
Check:  b_eff = 92.3 mm  ≤  t_f + 2c = 15 + 108.4 = 123.4 mm      ✔
```

**Step 6 — lever arms and moment resistance**
```
r_t = h_c/2 + e_c            = 100 + 60                       = 160.0 mm
r_c = h_c/2 + c − b_eff/2    = 100 + 54.2 − 46.15             = 108.1 mm

M_j,Rd = F_T,3,Rd·r_t + A_eff·f_jd·r_c
       = 183 000 × 160.0  +  28 458 × 24.0 × 108.1
       = 29.28 ×10⁶  +  73.83 ×10⁶  =  103.1 ×10⁶ N·mm

M_j,Rd = 103.1 kNm    (at N_Ed = 500 kN)
```

**Step 7 — stiffness coefficients (Table 6.11)**
```
k_b = k16 = 2.0 · A_s / L_b      = 2.0 × 353 / 261.5                    = 2.70 mm   (no prying)
k_p = k15 = 0.425 · ℓ_eff · t³/m³ = 0.425 × 210 × 30³ / 53.2³
          = 0.425 × 210 × 27 000 / 150 569                              = 16.0 mm
k_c = k13 = E_c · sqrt(a_eq,el · b_c) / (1.275·E)
      a_eq,el = t_f + 2.5·t = 15 + 75 = 90 mm ;  sqrt(90 × 200) = 134.2 mm
          = 27 000 × 134.2 / (1.275 × 210 000)                          ≈ 13.8 mm

1/k_T = 1/k_b + 1/k_p = 1/2.70 + 1/16.0   →   k_T = 2.31 mm
```
**Note how dominant the anchor bolt is:** `k_b = 2.70` vs `k_p = 16.0` vs `k_c = 13.8`. The bolt contributes **86 %** of the tension-side flexibility. *"the elongation of the anchor bolts mainly determines the stiffness behaviour of column base connections subjected primarily to bending moments"* (Steenhuis et al.).

**Step 8 — rotational stiffness**
```
Stiffness lever arms (to the column neutral axis):
   z_t = r_t = 160.0 mm     ;    z_c = h_c/2 − t_f/2 = 100 − 7.5 = 92.5 mm
   z   = z_t + z_c = 252.5 mm

e₀ = ( k_c·z_c − k_T·z_t ) / ( k_c + k_T )
   = ( 13.8×92.5 − 2.31×160.0 ) / ( 13.8 + 2.31 )
   = ( 1276.5 − 369.6 ) / 16.11 = 56.4 mm

e  = M_j,Rd / F_Ed = 103.1×10⁶ / 500×10³ = 206.2 mm

S_j,ini = e · E · z² / [ (e + e₀) · Σ(1/k_i) ]
        = 206.2 × 210 000 × 252.5² / [ (206.2 + 56.4) × (1/2.31 + 1/13.8) ]
        = 2.761×10¹² / ( 262.6 × 0.5054 )
        = 2.080×10¹⁰ N·mm/rad

S_j,ini = 20 800 kNm/rad          [source: 20 799 kNm/rad — exact match]
```

**Step 9 — classification (§5.2.2.5)**
```
S̄ = S_j,ini · L_c / (E · I_c) = 20 799×10⁶ × 4000 / ( 210 000 × 56.96×10⁶ ) = 6.96

Unbraced/sway:  6.96 < 30  →  NOT rigid  →  SEMI-RIGID
Braced (12 boundary): 6.96 < 12  →  SEMI-RIGID
```

**Conclusion for the report:** a 30 mm plate on a 420×420 footprint with 4×M24 into a 1.6 m footing — what any fabricator would draw as a "fixed base" — is **semi-rigid**, at only **7 EI_c/L_c** against a rigid boundary of **30 EI_c/L_c**. To make it genuinely rigid you would need roughly **4× the stiffness**, which means shortening `L_b` (impossible — it is 8d by definition), or increasing `A_s` (more/bigger bolts), or increasing `t_p` and `ℓ_eff`, or moving the bolts further out to increase `z`. In practice: **more bolts on a longer lever arm**, not a thicker plate.

---

### 5.3 WORKED EXAMPLE C — AISC Design Guide 1, Example 4.1 (axial only, no confinement)

**Given**
- **W12×96** column: d = 12.7 in (323 mm), b_f = 12.2 in (310 mm)
- Concrete pedestal **24 in × 24 in** (610 × 610 mm), f′_c = 3 ksi (20.7 MPa)
- Base plate F_y = 36 ksi (248 MPa)
- **P_u = 700 kips (3 114 kN)** LRFD; P_a = 430 kips ASD
- Conservatively `A₂ = A₁` (Case I). φ = 0.65.

```
A₁,req = P_u / (φ · 0.85 · f'_c) = 700 / (0.65 × 0.85 × 3) = 422 in²   (272 300 mm²)
Δ = (0.95d − 0.80b_f)/2 = (0.95×12.7 − 0.80×12.2)/2 = (12.065 − 9.76)/2 = 1.15 in
N ≈ sqrt(422) + 1.15 = 20.54 + 1.15 = 21.7 in    →  try N = 22 in
B = 422/22 = 19.2 in                              →  use B = 20 in
A₁ = 22 × 20 = 440 in² > 422 in²   ✔

φP_p = 0.65 × 0.85 × 3 × 440 × sqrt(440/440) = 729 kips  >  700 kips   ✔

m = (22 − 0.95×12.7)/2 = (22 − 12.065)/2 = 4.97 in
n = (20 − 0.80×12.2)/2 = (20 − 9.76)/2  = 5.12 in       ← governs
X = [4 × 12.7 × 12.2 / (12.7+12.2)²] × (700/729) = 0.96
λ = 2·sqrt(0.96)/(1 + sqrt(1−0.96)) = 1.63  →  take λ = 1.0
n' = sqrt(12.7 × 12.2)/4 = 3.11 in ;  λn' = 3.11 in

ℓ = max(4.97 ; 5.12 ; 3.11) = 5.12 in

t_min = 5.12 × sqrt( 2×700 / (0.90 × 36 × 20 × 22) ) = 1.60 in       (LRFD)
t_min = 5.12 × sqrt( 2×430×1.67 / (36 × 20 × 22) )   = 1.54 in       (ASD)
```
**Result: base plate 20 in × 22 in × 1¾ in  ≈ 508 × 559 × 44.5 mm.**
Anchor rods: "Since no anchor rod forces exist, the anchor rod size can be determined based on the OSHA requirements… **Use 4 ¾-in.-diameter rods, ASTM F1554 Grade 36. Rod length = 12 in.**" (≈ 4 × M20, 300 mm long).

**DG1 Example 4.2 (same column, using confinement, Case III):** with `A₁,req` reduced to 211 in² → try **N = 16 in, B ≈ 13.2 in**, i.e. roughly **half the plan area** for the same load, at the cost of a thicker plate. This is the classic size-vs-thickness trade-off, quantified.

---

### 5.4 WORKED EXAMPLE D — AISC DG1 Example 4.7, large-moment base (realistic detail)

**Given:** W12×96 (d = 12.7 in, b_f = 12.2 in); P_D = 100 k, P_L = 160 k; M_D = 1 000 k·in, M_L = 1 500 k·in; f′_c = 4 ksi; F_y = 36 ksi; A₂ = A₁.

```
P_u = 1.2(100) + 1.6(160) = 376 kips  ;  M_u = 1.2(1000) + 1.6(1500) = 3 600 k·in
e = M_u/P_u = 3600/376 = 9.57 in

TRY N = B = 19 in:   q_max = 42.0 k/in ;  e_crit = 19/2 − 376/(2×42.0) = 5.02 in   →  e > e_crit  (large moment)
   anchor rod edge distance 1.5 in  →  f = 19/2 − 1.5 = 8.0 in
   Check (3.4.4):   (f + N/2)² = (8.0 + 9.5)² = 306
                    2P_u(e+f)/q_max = 2(376)(9.57+8.0)/42.0 = 315
                    315 > 306   →  NO REAL SOLUTION  →  ENLARGE THE PLATE

TRY N = B = 20 in:   q_max = 2.21 × 20 = 44.2 k/in ;  f = 20/2 − 1.5 = 8.5 in ;  e_crit = 5.75 in
   Check (3.4.4):   (f + N/2)² = (8.5 + 10)² = 342
                    2P_u(e+f)/q_max = 2(376)(9.57+9.5)/44.2 = 324
                    324 < 342   →  REAL SOLUTION EXISTS   ✔
   Y = (f + N/2) − sqrt[ (f+N/2)² − 2P_u(e+f)/q_max ] = 19.5 − sqrt(342 − 324) = 19.5 − 4.24 ≈ 15.3 in
   T_u = q_max·Y − P_u = 44.2(15.3) − 376 ≈ 300 kips
```
**Fabrication lesson:** a **1 inch (25 mm)** increase in plate dimension turned an impossible base into a workable one. In moment bases the *plan size / anchor lever arm* is the primary variable — not thickness.

An adjacent DG1 example (§4.6, small moment) finishes as **"Use a base plate 12″ × 19″ × 1′-7″"** (≈ 305 mm thick × 483 × 483 — note the 12-in dimension is the thickness call-out sequence in DG1's B × t × N convention), with `n = 4.62 in` governing over `m = 3.47 in`, `t_p(req) = 1.36 in`.

---

## PART 6 — STIFFENERS AND RIBS ON BASE PLATES

### 6.1 When they are needed
EN 1993-1-8 says simply (via Wald §7 intro): *"In general they are designed with unstiffened base plates, but **stiffened base plates may be used where the connection is required to transfer high bending moments**."*

Practical triggers:
1. **Plate thickness becomes uneconomic or unavailable.** DG1 §2.2: plates come in ⅛ in (3 mm) increments up to 1¼ in (32 mm), then ¼ in (6 mm) increments. Above ~60 mm you are into special procurement, difficult flame-cutting, heavy handling, and thick-plate through-thickness (Z-quality) concerns.
2. **The anchor bolt lever arm is long** and the plate cantilever `m_x` from the weld toe to the bolt would demand an absurd `t_p` (`t_p ∝ sqrt(F·m)`).
3. **A shear lug or heavy CJP weld** would otherwise be needed. DG1 §2.2: *"A possible exception… is the case of moment-type bases that resist large moments. For example, in the design of a crane building, the use of a **seat or stool** at the column base may be more economical, if it eliminates the need for large CJP groove welds to heavy plates that require special material specifications."*
4. **Uplift-dominated bases** (masts, silo legs, tank legs, pipe racks) where anchors sit far outside the section.

### 6.2 The economics — the single most important sentence for a fabricator
> "When designing base plate connections, it is important to consider that **material is generally less expensive than labor** and, where possible, **economy may be gained by using thicker plates rather than detailing stiffeners** or other reinforcement to achieve the same strength with a thinner base plate." — *AISC DG1 §2.2*

A rib requires: cutting to a profile, bevelling, fit-up, 2–4 fillet welds each, distortion control, and access for painting. A thicker plate requires: one flame cut. **Do not stiffen unless the plate is genuinely uneconomic or unbuildable.**

### 6.3 How stiffeners change the design

**(a) Compression side.** The stiffener becomes part of the "column cross-section" for the purpose of building the effective area. Górski (MATEC Web Conf. 219, 02019, 2018): *"The column cross-section together with vertical stiffeners, enlarged on both sides by additional bearing width `c`, determines the available effective area of concrete."* The `c` strip now runs along both faces of every rib, so `A_eff` grows roughly by `2·c·L_rib` per rib.

**(b) Tension side.** The rib subdivides the plate into panels and changes the yield-line pattern. The T-stub `m` is now measured from the rib toe (or weld toe on the rib), not from the column flange — this is the whole point: **a rib halves `m`, and `F_T,1-2,Rd = 2M_pl/m`, so it roughly doubles the plate's tension capacity for the same thickness.** New effective lengths must be derived by yield-line analysis; the code tables in Table 6.6 / Wald Table 7.1 do **not** cover stiffened patterns. Górski notes: *"The actual code [EN 1993-1-8] does **not provide guidelines** for calculations of column bases with such complex geometry."*

**(c) Stiffness.** k15 is `∝ ℓ_eff·t³/m³`. Halving `m` multiplies k15 by **8**. This is the most efficient way to stiffen a base plate — far more efficient than plate thickness (which is only cubic once, and increases `c` only linearly).

**(d) Analysis basis.** Górski's procedure assumes a **rigid column core + stiffeners**, so the plate under those elements deforms linearly (Bernoulli–Euler), with a rectangular concrete stress block `x_eff = λ·x`, `λ = 0.8` for concrete ≤ C50/60 (EN 1992-1-1), and limits set by FE (Autodesk Simulation Mechanical, 8-node brick + 4-node tet, surface-to-surface contact, nonlinear material).

**(e) The counter-argument — the "stiff support" fallacy.** Bentley RAM Connection documentation: gussets/stiffeners are often *neglected* in base plate design because *"if the gusset plate provides stiff support… the base plate bend lines will extend diagonally from near the column flange tip to near the gusset edge, but due to **column web bending flexibility at the gusset-to-column interface, a stiff support cannot be assumed**."* → A rib welded only to the column **web** or to a thin flange may not deliver the assumed support. **Ribs must land on the column flanges, or be paired with a matching rib inside the section.**

### 6.4 Typical rib geometry (industry practice — state as practice, not code)
| Parameter | Typical value |
|---|---|
| Rib thickness `t_r` | ≈ column web thickness, or `t_p/2`; commonly 10–20 mm; ≥ 8 mm |
| Rib height `h_r` | 1.5–2.5 × the plate outstand it stiffens; commonly 150–400 mm |
| Rib top | chamfered 30°–45° to avoid a hard stiffness discontinuity and a weld-return notch |
| Rib toe at the plate | stop 10–15 mm short of the plate edge, or run out and seal-weld |
| Weld to plate | fillet both sides, `a` ≈ 0.5·t_r (full-strength fillet per EN 1993-1-8: 0.5t for normal force, 0.3t for shear — Wald JRC slide 68) |
| Number | 2 per flange (either side of each anchor bolt) is the standard "4-rib" moment base; 4 per flange for heavy masts |
| Cope at the plate/column corner | 10–15 mm radius, to let all three welds run out cleanly and avoid triaxial restraint |

---

## PART 7 — GROUT: PURPOSE, TYPES, THICKNESS

### 7.1 What grout is actually for
1. **Load transfer** — fills the gap so the plate bears on a continuous surface rather than on shims. *"Grout serves as the connection between the steel base plate and the concrete foundation to transfer compression loads."* (DG1 §2.10)
2. **Levelling** — takes up the difference between the surveyed top of concrete and the required steel setting-out level.
3. **Corrosion protection** — excludes water from under the plate and around the bolts. (Hilti: an incompletely dry-packed pad *"allow[s] moisture to enter and pool, which can lead to accelerated corrosion and degradation of the connection, **even in comparison to an ungrouted connection**."*)
4. **Shear transfer by friction** — `F_f,Rd = C_f,d · N_c,Ed` (see 4.8).

### 7.2 Thickness rules — five different sources, all quoted

| Rule | Value | Source |
|---|---|---|
| For `β_j = 2/3` | `t_g ≤ 0.2 · min(a ; b)` (min. base plate dimension) | **EN 1993-1-8 §6.2.5(7)** |
| Above 50 mm | `f_ck,grout ≥ f_ck,concrete` (no longer allowed to be 0.2×) | **EN 1993-1-8 §6.2.5(7)** |
| Above 50 mm (background) | "Where the thickness of the grout is more than 50 mm, the characteristic strength of the grout should be at least the same as that of the concrete foundation" | **Wald Q&A 7.2** |
| EN 1090-2 execution, `t ≤ 25 mm` | **neat Portland cement** | **EN 1090-2:2018 §5.9** |
| EN 1090-2, `25 mm < t < 50 mm` | **fluid** Portland cement mortar, **not leaner than 1:1** cement : fine aggregate | **EN 1090-2:2018 §5.9** |
| EN 1090-2, `t ≥ 50 mm` | **dry-as-possible** Portland cement mortar, **not leaner than 1:2** cement : fine aggregate | **EN 1090-2:2018 §5.9** |
| EN 1090-2, fine concrete | permitted **only** for gaps of nominal thickness **≥ 50 mm** | **EN 1090-2:2018 §5.9** |
| AISC, on a finished floor | **1 in. (25 mm)** may be adequate | **AISC DG1 §2.10** |
| AISC, on a footing or pier | normally **1½ to 2 in. (38–50 mm)** | **AISC DG1 §2.10** |
| AISC, large plates / shear lugs | may require more | **AISC DG1 §2.10** |
| EN 1992-4 anchor shear validity | `t_grout ≤ min(40 mm ; 5d)` | **EN 1992-4:2018 §6.2.2.3(2)** |
| Hilti SOFA relaxation | grout pads up to **100 mm** (method valid to 130 mm) | **Hilti, Grouted Stand-off, v1.2 (2023)** |
| Packings/shims left in | must be enclosed by grout with **min. 25 mm cover** | **EN 1090-2:2018 §9.5.4** |

**Reconciling the "0.2 × minimum plate dimension" rule:** for a 340 mm square plate, `0.2 × 340 = 68 mm` — generous. For a small 200 mm plate, `0.2 × 200 = 40 mm`. Combined with the EN 1992-4 anchor-shear limit of 40 mm, and AISC's 38–50 mm practical range, **25–50 mm is the working band**, with **≤ 40 mm** preferred whenever anchor shear matters.

### 7.3 Grout strength

| Requirement | Value | Source |
|---|---|---|
| For `β_j = 2/3` | `f_ck,g ≥ 0.2 · f_ck` | EN 1993-1-8 §6.2.5(7) |
| For `t_g > 50 mm` | `f_ck,g ≥ f_ck` | EN 1993-1-8 §6.2.5(7) |
| AISC recommendation | *"Grout should have a design compressive strength **at least twice the strength of the foundation concrete**."* | AISC DG1 §2.10 |
| EN 1992-4 anchor shear | grout ≥ concrete **and ≥ 30 N/mm²** | EN 1992-4:2018 §6.2.2.3(2) |
| Pocket bases | infill concrete `f_ck` **≥ surrounding concrete** | EN 1090-2:2018 §9.5.5 |

Note the apparent conflict: EN allows grout at **0.2×** concrete strength; AISC demands **2×** — a factor of 10 apart. Both are defensible: EN relies on the liquid/triaxial confinement argument (§1.4) and then *checks the grout separately* when the condition is not met; AISC simply removes the question by specifying strong grout, then says *"it is conservative to use the concrete compressive strength for f′_c in the above equations"* (DG1 §3.1.1). **Recommendation: specify grout ≥ concrete strength as a default. It costs almost nothing and eliminates a whole class of checks.**

### 7.4 Non-shrink cementitious vs epoxy

| | **Non-shrink cementitious** | **Epoxy (resin) grout** |
|---|---|---|
| Compressive strength | **25–55 MPa** typical; MasterFlow 928 / SikaGrout 928 to ASTM C1107 Grades B & C | **80–100+ MPa** |
| Creep | measurable | **near-zero** |
| Shrinkage | non-shrink formulation required; plain mortar shrinks and leaves voids | negligible on cure |
| Temperature | **better** high-temperature resistance — preferred for hot environments | limited by resin `T_g`; degrades in heat |
| Dynamic/impact loading | static or light dynamic | **heavy dynamic, vibration, impact** — turbines, presses, large rotating machinery |
| Chemical resistance | moderate | **high** — chemical plants, offshore |
| Cost | baseline | **up to 5×** cementitious |
| Placement | easier, flowable, forgiving | more complex, 3-component, exotherm on thick pours |
| Aggregate extension | MasterFlow 928: where thickness exceeds **100 mm**, add clean graded **10 mm** aggregate up to **1:1 by weight** | per manufacturer |
| Usable temperature range (M928) | **4 to 32 °C** (40–90 °F) at fluid consistency | per manufacturer |

**For a steel fabrication company in Israel:** cementitious non-shrink is correct for essentially all building/industrial column bases. Reserve epoxy for machine bases, crane rail supports under impact, and chemically aggressive environments.

### 7.5 Dry-pack vs flowable — an explicit warning
Hilti (2023): *"Dry-pack grouts… are susceptible to errors that could lead to **incomplete filling of the space** between the plate and the concrete. There is also a risk of incomplete mixing. Such conditions could lead to cracking, voids, degradation, inconsistent/low grout strength, and uneven stress transfer… **dry packing is recommended only if you can ensure the quality of installation meets engineering requirements.**"*
For flowable grout, watch **air entrapment at the underside of the plate** — *"a possible location for water ingress and pooling."* Provide **air escape / vent holes** (EN 1090-2 §9.5.5 d).

### 7.6 EN 1090-2:2018 §9.5.5 — the execution requirements, verbatim
```
a) mixed and used per manufacturer's recommendations, notably regarding consistency;
   material shall NOT be mixed or used below 0 °C unless the manufacturer permits;
b) poured under a suitable head so that the space is COMPLETELY FILLED;
c) tamping and ramming against properly fixed supports if specified/recommended;
d) vent holes shall be provided as necessary.
Immediately before grouting, the space under the plate shall be free from liquids, ice,
debris and contaminants.
The external profile of grouting shall allow water to be DRAINED AWAY from the steel.
Where water or corrosive liquid could be entrapped, the grout around base plates shall NOT
be surcharged such that it rises ABOVE THE LOWEST SURFACE OF THE BASE PLATE.
```
That last sentence is a drawing note that should appear on every Eretz Barzel base plate detail.

### 7.7 Grout holes
DG1 §2.10: *"Grout holes are not required for most base plates. For plates **24 in. (610 mm) or less in width**, a form can be set up and the grout can be forced in from one side until it flows out the opposite side. When plates become larger or when shear lugs are used, it is recommended that **one or two grout holes** be provided. Grout holes are typically **2 to 3 in. (50–75 mm) in diameter** and are typically **thermally cut** in the base plate."*

---

## PART 8 — PRACTICAL FABRICATION AND DETAILING IMPLICATIONS

### 8.1 Plate thickness — practical minima and increments
| Item | Value | Source |
|---|---|---|
| Minimum for posts and light HSS columns | **½ in. ≈ 13 mm** | AISC DG1 §2.2 |
| Minimum commonly specified for structural columns | **¾ in. ≈ 19 mm** | AISC DG1 §2.2 |
| Widely-used rule of thumb | `t_p ≥ t_f` (column flange thickness) | industry practice |
| Available increments (US) | ⅛ in (3 mm) up to 1¼ in (32 mm); ¼ in (6 mm) above | AISC DG1 §2.2 |
| Material | *"There is seldom a reason to use high-strength material, since increasing the thickness will provide increased strength where needed."* | AISC DG1 §2.2 |
| Standardisation | *"The base plate sizes specified should be standardized during design to facilitate purchasing and cutting of the material."* | AISC DG1 §2.2 |
| Plan shape | *"Most column base plates are designed as square to match the foundation shape and more readily accommodate square anchor rod patterns. Exceptions… moment-resisting bases and columns adjacent to walls."* | AISC DG1 §2.2 |

**Cross-check for `c`:** a useful check for the detailer — *the plate outstand beyond the column footprint should be ≈ c, not more*. With S235 and `f_jd ≈ 15 N/mm²`, `c ≈ 2.3·t`. So a 20 mm plate "uses" ~46 mm of outstand; a 30 mm plate ~69 mm. **Outstand much greater than 2.3t is dead steel** (it only buys anchor-bolt edge distance).

### 8.2 Anchor bolt holes — the single biggest source of site problems
**AISC DG1 Table 2.3 — Recommended Sizes for Anchor Rod Holes in Base Plates**

| Rod dia. (in) | ≈ metric | Hole dia. (in) | ≈ mm | Min. washer dim. (in) | ≈ mm | Min. washer thk. (in) | ≈ mm |
|---|---|---|---|---|---|---|---|
| ¾ | M20 | 1 5/16 | 33 | 2 | 51 | ¼ | 6 |
| ⅞ | M22 | 1 9/16 | 40 | 2½ | 64 | 5/16 | 8 |
| 1 | M24 | 1 13/16 | 46 | 3 | 76 | ⅜ | 10 |
| 1¼ | M30 | 2 1/16 | 52 | 3 | 76 | ½ | 13 |
| 1½ | M36 | 2 5/16 | 59 | 3½ | 89 | ½ | 13 |
| 1¾ | M42 | 2¾ | 70 | 4 | 102 | ⅝ | 16 |
| 2 | M48 | 3¼ | 83 | 5 | 127 | ¾ | 19 |
| 2½ | M64 | 3¾ | 95 | 5½ | 140 | ⅞ | 22 |

Notes from DG1 §2.6:
- Hole is **~1.8× the rod diameter** for M24 and below — far larger than a structural bolt hole. This is deliberate: *"it is important to provide as large a hole as possible to accommodate setting tolerances."*
- *"The washer diameters shown are sized to **cover the entire hole when the anchor rod is located at the edge of the hole**."*
- *"Plate washer thickness be approximately **one-third the anchor rod diameter**."*
- *"Washers should **not** be welded to the base plate, **except** when the anchor rods are designed to resist shear at the column base."*
- *"ASTM F436 washers are not used on anchor rods because they generally are of insufficient size."* Washers **need not be hardened**.

**EN 1090-2:2018 §11.2.3.3 (Column bases), verbatim:**
> "Holes in baseplates and other plates used for fixing to supports should be dimensioned to allow clearances to match the permitted deviations for the supports to those for the steelwork. **This may require the use of large washers** between the nuts on the holding down bolts and the top of the baseplate."

**EN 1090-2:2018 §11.2.3.2 (Foundation bolts):**
> "The position of the centre points of a group of foundation bolts or other support shall not deviate by more than **± 6 mm** from its specified position relative to the secondary system. A best-fit position should be chosen to assess a group of adjustable foundation bolts."

**EN 1090-2:2018 §9.5.2 (bolt sleeves):**
> "Foundation bolts intended to move in sleeves should be provided with sleeves **three times the diameter of the bolt with a minimum of 75 mm**."

> ⚠ **Design consequence, and it is not academic:** with a 46 mm hole for an M24 anchor, the bolt can sit up to **11 mm off** its nominal position in any direction. EN 1993-1-8 §6.2.6.12(2) NOTE says *"Tolerances on the positions of the anchor bolts may have an influence"* on the lever arm. In a moment base with `z ≈ 250 mm`, an 11 mm bolt shift is a **4–5 % change in M_j,Rd** — and the plate can also rotate. **Check the moment resistance at the worst credible bolt position, not the nominal one.**

### 8.3 Anchor bolt sizing, layout and threads (AISC DG1 §2.7)
- *"Use ¾-in.-diameter ASTM F1554 Grade 36 rod material whenever possible. Where more strength is required, consider **increasing rod diameter up to about 2 in. in Grade 36 material before switching to a higher-strength material grade**."* (European equivalent: prefer larger 4.6/5.6 anchors over small 8.8 anchors — also avoids the §6.2.6.12(5) hook prohibition.)
- *"Anchor rod details should always specify **ample threaded length**… at least **3 in. (75 mm) greater than required**, to allow for variations in setting elevation."*
- *"the typical layout should have **four anchor rods in a square pattern**."*
- Edge distance: *"even an edge distance that provides a clear dimension as small as **2 in. (50 mm)** of material from the edge of the hole to the edge of the plate will normally suffice"* when the hole edge is not subject to lateral force — larger if slotting may be needed, and larger still if the hole edge carries shear.
- *"Anchor rod layouts must be coordinated with the reinforcing steel."*

### 8.4 Welding the column to the plate
- DG1 §2.4: *"Most column base plates are **shop welded** to the column shaft."*
- Finishing, AISC Spec §M2.8: bearing surfaces need milling — with **two exceptions**: *"The **bottom surface need not be milled when the base plate is to be grouted**, and the top surface need not be milled when CJP groove welds are used to connect the column to the base plate."* → **grouted bases do not need machined undersides.** Big cost saving; state it explicitly.
- Full-strength fillet weld sizes (Wald JRC slide 68, EN 1993-1-8): `a ≈ 0.5 t` for a normal force, `a ≈ 0.3 t` for shear. By steel grade: S235 → `a = 0.37 t`; S355 → `a = 0.45 t`; S420N → `a = 0.58 t`; S460N → `a = 0.61 t`.
- **Weld capacity rule, EN 1993-1-8 §6.2.3(4):** *"In all joints, the sizes of the welds should be such that the moment resistance of the joint `M_j,Rd` is always limited by the design resistance of its **other basic components, and not by the design resistance of the welds**."* Never let the weld be the weak link in a base plate.
- Thermal cutting of the plate perimeter: AISC Spec §M2.2 requires free edges under calculated static tensile stress to be free of round-bottom gouges > 3/16 in (5 mm) and sharp V-notches. *"Because free edges of the base plate are **not** subject to tensile stress, these requirements are **not mandatory** for the perimeter edges; however, they provide a workmanship guide."* (DG1 §2.3)

### 8.5 Erection method — three options, with their consequences (DG1 §2.9)
| Method | Description | Watch out for |
|---|---|---|
| **Setting nut and washer** (§2.9.1) | levelling nuts under the plate, top nuts above | *"the setting nut will transfer load to the anchor rod"* even after grouting — check anchor push-out at the bottom of the footing. *"limited to columns that are relatively lightly loaded during erection."* EN 1090-2 §9.5.4 permits leaving levelling nuts in place, *"unless otherwise specified"*, provided they don't jeopardise the bolt in service. |
| **Setting plate** (§2.9.2) | a loose plate set and grouted to level before the column arrives | warping of setting plate or base plate; *"consider when the column is being erected in an excavation where water and soil may wash under the base plate."* Steel templates can be reused as setting plates. |
| **Shim stack** (§2.9.3) | steel shim packs ≈ 4 in (100 mm) wide at the four plate edges | EN 1090-2 §9.5.4: shims must present a **flat surface** and be *"of adequate size, strength and rigidity to avoid local crushing of the substructure concrete"*; if grouted, **min. 25 mm grout cover**. EN 1090-2 §11.2.3.5 (full contact bearing): shims max **3 mm thick**, **no more than three at any point**. |
| **Large plates** (§2.9.4) | wedge shims or levelling/adjusting screws; historically three adjusting screws (Fig. 2.2) | reduces handling weight; gives a fully grouted plate to receive a heavy column |

**Templates (DG1 §2.8):** *"Templates should be made for each anchor rod setting pattern. Typically… plywood on site… The anchor rods can be held securely in place and relatively straight by using a **nut on each side of the template**."* Steel plate or angle-frame templates for large assemblies. *"only placement drawings that have been designated as 'Released for Construction' should be used."*

### 8.6 EN 1090-2:2018 — the base-plate-relevant execution clauses at a glance
| Clause | Requirement |
|---|---|
| **§5.6.7** | Foundation bolts (materials/marking) |
| **§5.9** | Grouting materials: cement-based grout / special grout / fine concrete; thickness bands (see 7.2) |
| **§9.5.1** | Condition and location of supports checked **before** erection; nonconformities documented |
| **§9.5.2** | Erection shall not commence until location/levels comply with §11.2; compliance survey documented; bolt sleeves 3d ≥ 75 mm; pre-stressed grillage bolts must have **no adhesion to the concrete over their full length** |
| **§9.5.3** | Settlement compensation by grouting or packing is acceptable unless otherwise specified |
| **§9.5.4** | Shims/temporary supports; 25 mm grout cover; bridges — packings not left in |
| **§9.5.5** | Grouting and sealing (see 7.6); pocket bases (see 3.2) |
| **§9.5.6** | Anchoring devices set per their specification; avoid damage to concrete |
| **§11.2.3.2** | Foundation bolt group position **± 6 mm** |
| **§11.2.3.3** | Base plate hole sizes to absorb support deviations; large washers |
| **§11.2.3.5** | Full contact bearing: shims ≤ 3 mm, max 3 per point; may be secured by fillet or partial-penetration butt weld over the shims |

### 8.7 The Israeli standards position
- **ת"י 1225** — *Steel Structures Code: General* (חוקת מבני פלדה). Part 1 dated 1 December 1998 (superseding the 1991 version on 10 January 1999). Scope: *"design and execution of buildings, structures and components made of rolled steel"*, permanent or temporary, **excluding** thin-walled cold-formed members and prestressed steel structures.
- **ת"י 1225 חלק 1.1** and **חלק 1.8** were published **June 2023** — the part numbering (1.1, 1.8) mirrors EN 1993-1-1 and **EN 1993-1-8**, i.e. the Israeli standard is being restructured on the Eurocode 3 part scheme. SII states Part 1.1 (June 2023) and the older Part 1 (Dec 1998, amended to Feb 2009) are both in force for a **3-year transition period**.
- **Practical recommendation for the report:** design column bases to **EN 1993-1-8 + EN 1992-1-1 + EN 1992-4**, cite **ת"י 1225 חלק 1.8** as the Israeli adoption, and confirm the Israeli National Annex values for `γ_M0`, `γ_M2`, `γ_C` and `β_j` before finalising any calculation. *(This bullet should be verified against the current SII edition before publication — the SII product page did not state the EN adoption explicitly.)*

### 8.8 Anchor bolt repairs — because it will happen (DG1 §2.11)
DG1 devotes a whole section to it: rods in the wrong position (§2.11.1), bent or not vertical (§2.11.2), projection too long or too short (§2.11.3), pattern rotated 90° (§2.11.4). Two governing constraints:
1. **OSHA requires any modification of anchor rods to be approved by the Engineer of Record.**
2. *"The added setting tolerance is especially important when the full or near-full strength of the rod in tension is needed for design purposes, because **almost any field fix in this case will be very difficult**."*
→ **In a moment base, get the anchors right the first time. In a pinned base, you have room to recover.**

---

## PART 9 — SUMMARY OF THE KEY NUMBERS, ONE TABLE

| Quantity | Value | Source |
|---|---|---|
| `c` (additional bearing width) | `t·sqrt(f_y/(3 f_jd γ_M0))`; ≈ **2.3 t** for S235 at f_jd ≈ 15 MPa | EN 1993-1-8 §6.2.5(4) eq. (6.5) |
| `c` for **stiffness** calculations | **1.25 · t_p** | EN 1993-1-8 Table 6.11 NOTE 1 |
| `β_j` | **2/3** | EN 1993-1-8 §6.2.5(7) |
| grout strength for β_j = 2/3 | `f_ck,g ≥ 0.2 f_ck` | EN 1993-1-8 §6.2.5(7) |
| grout thickness for β_j = 2/3 | `t_g ≤ 0.2 · min(a;b)` | EN 1993-1-8 §6.2.5(7) |
| grout > 50 mm | `f_ck,g ≥ f_ck` | EN 1993-1-8 §6.2.5(7) |
| `F_Rdu` cap | `≤ 3.0 f_cd A_c0` | EN 1992-1-1 §6.7 |
| `f_jd` max (strict) | **2.0 f_cd** | EN 1992-1-1 §6.7 + EN 1993-1-8 §6.2.5(7) |
| `f_jd` max (Wald's EC3 examples) | **3.0 f_cd** | Wald JRC 2014 slides 117, 123 |
| `f_jd` max (ENV legacy, k_j ≤ 5) | **3.33 f_cd** | Wald Q&A 7.3/7.4 |
| Code conservatism vs 50 tests | test/predicted = **1.4 – 2.5**, mean **1.75** | Wald Q&A 7.3 |
| Triaxial enhancement observed | ≈ **6.25 ×** uniaxial | Wald Q&A 7.4 |
| `k13` | `E_c·sqrt(b_eff·ℓ_eff)/(1.275·E)` ; 1.275 = 1.5 (workmanship) × 0.85 (shape) | EN 1993-1-8 Table 6.11 / HERON 53 eq. (23) |
| `k15` | `0.85 ℓ_eff t_p³/m³` (prying) ; `0.425 ℓ_eff t_p³/m³` (no prying) | EN 1993-1-8 Table 6.11 |
| `k16` | `1.6 A_s/L_b` (prying) ; `2.0 A_s/L_b` (no prying) | EN 1993-1-8 Table 6.11 |
| `L_b` | `8d + t_grout + t_p + t_washer + h_nut/2` | EN 1993-1-8 Table 6.2 note |
| Prying limit | `L_b ≤ 8.8 m³ A_s/(Σℓ_eff t³)` | EN 1993-1-8 Table 6.11 / Wald (7.11) |
| Rigid boundary, sway frames | `S_j,ini ≥ 30 EI_c/L_c` | EN 1993-1-8 §5.2.2.5(2) eq. (5.2d) |
| Rigid boundary, braced, `λ̄₀ ≥ 3.93` | `S_j,ini ≥ 48 EI_c/L_c` | eq. (5.2c) |
| Rigid boundary, braced, `0.5 < λ̄₀ < 3.93` | `S_j,ini ≥ 7(2λ̄₀ − 1) EI_c/L_c` | eq. (5.2b) |
| Rigid, `λ̄₀ ≤ 0.5` | rigid regardless | eq. (5.2a) |
| Semi-rigid / pinned practical boundary | `12 EI_c/L_c` | Wald & Jaspart, HERON 53 Fig. 7 |
| Pinned boundary in the code | **does not exist** | HERON 53 §3 |
| Nominally pinned base in analysis | 10 % fixity ULS, 20 % SLS | SCI P397 §6.4 / BS 5950 5.1.3.3 |
| Friction, sand-cement mortar | `C_f,d = 0.20` | EN 1993-1-8 §6.2.2(6) |
| Friction, other grout | by testing per EN 1990 Annex D | EN 1993-1-8 §6.2.2(6) |
| Friction, thin grout < 3 mm | 0.40 with `γ_Mf = 1.5` | CEB Guide 1997 |
| Anchor shear factor | `α_b = 0.44 − 0.0003 f_yb`, `f_yb ≤ 640 MPa` | EN 1993-1-8 §6.2.2(7) |
| Grout reduction on anchor shear | `(1 − 0.01 t_grout)`, `t_grout ≤ min(40; 5d)`, grout ≥ 30 MPa | EN 1992-4:2018 §6.2.2.3(2) |
| Hooked anchor limit | not permitted for `f_yb > 300 N/mm²` | EN 1993-1-8 §6.2.6.12(5) |
| Anchor group position tolerance | **± 6 mm** | EN 1090-2:2018 §11.2.3.2 |
| Bolt sleeve size | **3d, min 75 mm** | EN 1090-2:2018 §9.5.2 |
| Grout cover over shims | **25 mm min** | EN 1090-2:2018 §9.5.4 |
| Min. plate thickness (practice) | 13 mm (posts/light HSS); 19 mm (structural columns) | AISC DG1 §2.2 |
| AISC `m`, `n`, `n'` | `(N−0.95d)/2`, `(B−0.80b_f)/2`, `sqrt(d·b_f)/4` | AISC DG1 §3.1.2 |
| AISC `t_min` | `ℓ·sqrt(2P_u/(0.9 F_y B N))` | AISC DG1 §3.1.2 |
| AISC bearing | `0.85 f'_c A₁ sqrt(A₂/A₁) ≤ 1.7 f'_c A₁`; φ = 0.65 (ACI) | AISC 360 §J8, DG1 §3.1.1 |
| AISC grout | strength ≥ 2 × concrete; 25 mm on slab, 38–50 mm on footing/pier | AISC DG1 §2.10 |
| Rotation capacity, tests | exposed 0.04–0.10 rad; embedded 0.03–0.08 rad | Torres Rodas / Grilli & Kanvinde / Barnwell |

---

## PART 10 — WHERE SOURCES DISAGREE (collect these in the report; do not hide them)

1. **Cap on the concentration factor.** `k_j ≤ 3.0` (EN 1992-1-1 §6.7 strict) → `f_jd ≤ 2.0 f_cd`; vs. Wald's own EC3 worked examples applying the cap to `f_jd` → `f_jd ≤ 3.0 f_cd`; vs. ENV 1993-1-1 Annex L `k_j ≤ 5` → `f_jd ≤ 3.33 f_cd`. **Factor of up to 1.67 on base compression capacity.**
2. **Grout strength.** EN 1993-1-8 permits `0.2 f_ck`; AISC DG1 demands `2 f_ck`; EN 1992-4 demands `≥ f_ck` and `≥ 30 MPa` for anchor shear.
3. **Friction coefficient for "special grout."** 0.30 in prEN 1993-1-8:2003 (and still quoted in Wald's Q&A 7.7); **deleted** from EN 1993-1-8:2005, which requires testing. Do not use 0.30 without justification.
4. **T-stub effective length `ℓ₂` for bolts outside the flange, no-prying case.** `4π m_x` (Wald CESTRUCO Table 7.1) vs `2π m_x` (Wald JRC 2014 slide 91). Use `4π m_x` — consistent with the doubling logic of Mode 1\*.
5. **Effective area construction.** Three non-overlapping T-stubs (EN 1993-1-8 §6.2.8.2, Fig. 6.19) vs. a wrapped perimeter band (used in Wald's slide 119). The wrapped method gave ≈ 8 % more area in Example A. Use the code method.
6. **Resistance factor for bearing on concrete.** φ = 0.60 (AISC 360 §J8) vs φ = 0.65 (ACI 318 §9.3). DG1 authors explicitly recommend 0.65 and call the 0.60 an oversight.
7. **Grout pad thickness limit for anchor shear.** EN 1992-4: 40 mm and 5d. Hilti SOFA/ACI 318: up to 100–130 mm with a flat 0.8 factor. Both are in current commercial software.
8. **Base fixity in analysis.** EN 1993-1-8 gives no pinned boundary and requires semi-rigid modelling in principle; SCI/BS practice permits 10 %/20 % fractional fixity; most design offices still model pinned or fixed. All three are "compliant" in different ways.

---

## PART 11 — SOURCES ACTUALLY USED

**Codes and standards (primary text obtained and read)**
1. **BS EN 1993-1-8:2005**, *Eurocode 3: Design of steel structures — Part 1-8: Design of joints*. Clauses read: §5.1.2, §5.2.2 (incl. 5.2.2.5), §5.2.3, §6.1.3 (Table 6.1), §6.2.2(6)–(9), §6.2.4 (Table 6.2), §6.2.5 (eqs. 6.4–6.6), §6.2.6.7–6.2.6.12, §6.2.8 (Table 6.7, Figs. 6.18–6.19), §6.3.1–6.3.4 (Tables 6.11, 6.12), §6.4. — https://www.phd.eng.br/wp-content/uploads/2015/12/en.1993.1.8.2005-1.pdf
2. **BS EN 1090-2:2018**, *Execution of steel structures and aluminium structures — Part 2: Technical requirements for steel structures*. Clauses read: §5.6.7, §5.9, §6.4, §9.4, §9.5.1–9.5.6, §11.2.2–11.2.3, Annex B tolerance tables. — https://sazvarsazeh.azarestan.com/wp-content/uploads/2022/02/BS-EN-1090-2-2018.pdf
3. **EN 1992-1-1**, *Eurocode 2 — Part 1-1*, §6.7 *Partially loaded areas* (via EN 1993-1-8 §6.2.5(7) and Wald's restatement of Fig. 6.29). Cross-checked at IDEA StatiCa, *Partially loaded areas (PLA)* — https://www.ideastatica.com/support-center/partially-loaded-areas-pla
4. **EN 1992-4:2018**, *Design of fastenings for use in concrete* — §6.2.2.3, §7.2.1–7.2.3, Eqs. (7.36), (7.37), Tables 7.1, 7.3 (accessed via the two manufacturer technical manuals below).
5. **ת"י 1225 / SI 1225**, *Steel Structures Code: General* — Israeli Standards Institute standard page (Part 1: Dec 1998; Parts 1.1 and 1.8: June 2023). — https://www.sii.org.il/he/דפי-לובי/כללי/תקינה/דף-תקן/?id=346e84a6-b8c8-4576-ac00-c1137f38eb3c

**Design guides**
6. **AISC Design Guide 1, 2nd edition (2006)**, *Base Plate and Anchor Rod Design*, Fisher & Kloiber. Sections read in full: §1.0, §2.2–2.12 (incl. Table 2.3), §3.1–3.5, worked Examples 4.1, 4.2, 4.6, 4.7. — http://www.abarsazeha.com/images/ScinteficResources/DesignGuide/AISC%20Design%20Guide%2001%20-%20Base%20Plate%20And%20Anchor%20Rod%20Design%202nd%20Ed.pdf

**Eurocode background papers (the authors of the EN 1993-1-8 column base rules)**
7. **Wald, F.**, *Column Bases*, CESTRUCO Chapter 7 (Q&A 7.1–7.10, Tables 7.1–7.3, Figs. 7.1–7.20), Czech Technical University. — https://people.fsv.cvut.cz/~wald/CESTRUCO/Texts_of_lessons/07-GB_Column_Bases.pdf
8. **Wald, F., Sokol, Z., Steenhuis, M. & Jaspart, J.-P.** (2008), *Component method for steel column bases*, **HERON 53(1/2), 3–20**. — https://heronjournal.nl/53-12/1.pdf
9. **Wald, F. & Jaspart, J.-P.** (2008), *Steel column base classification*, **HERON 53(1/2), 69–82** — full derivation of the 48 / 40 / 30 / 15 / 60 boundaries. — http://heronjournal.nl/53-12/4.pdf
10. **Steenhuis, C. M., Wald, F., Sokol, Z. & Stark, J. W. B.** (2008), *Resistance and Stiffness of Concrete in Compression and Base Plate in Bending* — derivation of `c_r = 1.25 t`, `a_eq,el = t_w + 2.5t`, and the 1.275 factor. — https://people.fsv.cvut.cz/wald/Clanky%20v%20Adobe%20(Pdf)/05_CB3-0-2004-Compression_rev3.pdf
11. **Wald, F.** (2014), *Bolts, welds, column base*, Eurocodes: Design of steel buildings with worked examples, **JRC/EC Workshop, Brussels, 16–17 October 2014** — slides 70–129, containing the two Eurocode worked examples reproduced in Part 5. — https://eurocodes.jrc.ec.europa.eu/sites/default/files/2022-06/06_Eurocodes_Steel_Workshop_WALD.pdf

**Stiffened base plates**
12. **Górski, M.** (2018), *Design procedure for steel column bases with stiffeners*, **MATEC Web of Conferences 219, 02019** (BalCon 2018). — https://www.matec-conferences.org/articles/matecconf/pdf/2018/78/matecconf_balcon2018_02019.pdf
13. Bentley RAM Connection documentation, *Include contribution of gussets / stiffeners in base plate design*. — https://docs.bentley.com/LiveContent/web/RAM%20Connection%20Connection%20Design-v2026/Help/en/Topic/rc/Topic/Help/c-rc%20Include%20contribution%20of%20gussets%20stiffeners%20in%20base%20plate%20design.html

**Manufacturer technical manuals (primary technical documents)**
14. **Hilti** (2023), *Hilti Method for Anchor Design in Grouted Stand-off Connections*, v1.2 — McBride, Rocha & Figoli. EN 1992-4 grout reduction, SOFA method, limits. — https://files-ask.hilti.com/original/cf/cfecdz4zhb.pdf
15. **Hilti**, *Hilti Method for Anchor Design in Ungrouted Stand-off Connections* — https://files-ask.hilti.com/original/pu/pu5ebwyxp4.pdf
16. **R-Steel**, *RPP Base Bolt Technical Manual*, v. 26.08.2025 — EN 1992-4 anchor formulas, M16–M39 resistance tables, edge/splitting reinforcement tables, installation tolerances. — https://www.rsteel.eu/wp-content/uploads/RPP-Technical-manual_EN_26.08.2025.pdf
17. **Peikko Group** (2023), *PPM® High-Strength Anchor Bolt Technical Manual* — https://media.peikko.com/file/dl/i/J_4N3A/5SM9-2bze-HWGqVrLezBvg/PPM_PEIKKO_GROUP_004_Technical_Manual_Web.pdf
18. **Peikko Group**, *HPM® Rebar Anchor Bolt Technical Manual* — https://media.peikko.com/file/dl/i/YGW97Q/Sw7hRcrYXrl99a_IZ3nRug/HPMPeikkoGroup002TMAWeb.pdf
19. **Master Builders Solutions**, *MasterFlow® 928* technical data (ASTM C1107 Grades B & C; 4–32 °C; 10 mm aggregate above 100 mm) — https://www.emisupply.com/media/document/file/m/a/master-builders-masterflow-928-tds.pdf

**Software theory manuals (useful for cross-checking clause interpretation)**
20. **IDEA StatiCa**, *Concrete in compression* — c, f_jd, β_j, k_j ≤ 3.0, iterative A_eff. — https://www.ideastatica.com/support-center/concrete-in-compression
21. **IDEA StatiCa**, *Base Plate Connections (AISC)* — https://www.ideastatica.com/support-center/base-plate-connections-aisc
22. **SCIA Engineer help**, *Stiffness coefficients* / *Stiffness classification* (EN 1993-1-8 Tables 6.11, 6.12) — https://help.scia.net/19.1/en/por/steelconnectionstb/stiffness_coefficients.htm and .../stiffness_classification.htm
23. **Graitec Advance Design**, *Rotational Stiffness* — https://www.graitec.com/Help/Advance_Design_Steel_Connection/En/Rotational_Stiffness.htm

**Base fixity in global analysis**
24. **SCI**, *NCCI: Column base stiffness for global analysis*, SN045a-EN-EU — https://www.steelconstruction.info/images/1/1a/SN045a.pdf *(referenced; direct fetch blocked by the host — content reported via secondary sources)*
25. **MasterSeries**, *Utilising the stiffness of nominally pinned bases in frame design* (citing SCI P397 §6.4 and BS 5950 cl. 5.1.3.3; 10 % ULS / 20 % SLS) — https://www.masterseries.com/blog/2019/utilising-the-stiffness-of-nominally-pinned-bases-in-frame-design
26. **SCI P398**, *Joints in steel construction: Moment-resisting joints to Eurocode 3* — https://steelconstruction.info/images/5/5d/SCI_P398.pdf *(referenced; host returned 403 — not read directly)*
27. **SCI P358**, *Joints in steel construction: Simple joints to Eurocode 3* — the companion volume covering nominally pinned bases; the UK NA to BS EN 1993-1-8 permits Green Book standard connections to be classified as nominally pinned.

**Embedded / pocket bases and real base stiffness**
28. **Torres Rodas, P.** (2017), *Hysteretic and Stiffness Models for Exposed and Embedded Column Base Connections*, PhD dissertation, UC Irvine (with Zareian & Kanvinde) — Ch. 2, 3, 5; test matrices from Grilli & Kanvinde (2015) and Barnwell (2015). — https://escholarship.org/content/qt40r716p7/qt40r716p7.pdf
29. **Kanvinde, A., Grilli, D. & Zareian, F.** (2012), *Rotational Stiffness of Exposed Column Base Connections: Experiments and Analytical Models*, ASCE J. Struct. Eng. 138(5) — https://ascelibrary.org/doi/10.1061/(ASCE)ST.1943-541X.0000495
30. **Torres Rodas, P., Zareian, F. & Kanvinde, A.** (2017), *Rotational Stiffness of Deeply Embedded Column–Base Connections*, ASCE J. Struct. Eng. 143(8) — https://ascelibrary.org/doi/10.1061/(ASCE)ST.1943-541X.0001789
31. **Kanvinde, A.** (2022), AISC Faculty Fellowship report on column bases — https://www.aisc.org/media/1x2bmccf/aisc-lrr-2022-01_kanvinde_column-bases.pdf *(referenced; host returned 403)*

**Also consulted for cross-checking**
32. Steel Tube Institute, *HSS Base Plate Design for Axial Compression and Bending Moment* — https://steeltubeinstitute.org/resources/hss-base-plate-design-for-axial-compression-and-bending-moment/
33. Sandhu, B. S., *Steel Column Base Plate Design*, AISC Engineering Journal — https://ej.aisc.org/index.php/engj/article/download/214/213

---

**Local working files** (text extractions retained for follow-up work, all absolute paths):
`C:\Users\User\AppData\Local\Temp\claude\C--Users-User-Desktop\a4c7fddc-7ed7-48b9-a359-e4b37dc8fdbe\scratchpad\` containing `en1993_1_8.txt` (EN 1993-1-8:2005, 135 pp), `en1090_2.txt` (EN 1090-2:2018, 208 pp), `aisc_dg1.txt` (AISC DG1 2nd ed., 69 pp), `wald_colbases.txt` (CESTRUCO Ch. 7), `wald_jrc.txt` (JRC 2014 slides, 136 pp), `heron_component.txt`, `heron_classification.txt`, `wald_compression.txt`, `stiffeners.txt`, `hilti_grouted.txt`, `rpp.txt`, `ecb_thesis.txt`, plus the extraction script `extract.py`.