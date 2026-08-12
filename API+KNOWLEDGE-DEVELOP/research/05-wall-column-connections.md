# ANCHORING STEEL MEMBERS TO VERTICAL CONCRETE ELEMENTS (COLUMNS AND WALLS)
## Technical brief — raw material for engineering report
### Eretz Barzel | prepared for a steel detailer/fabricator and engineering audience

---

## 0. SCOPE AND STANDARDS MAP

| Topic | Governing document | Notes |
|---|---|---|
| Anchor design (cast-in headed, post-installed mechanical, bonded, anchor channels) | **EN 1992-4:2018** (DIN EN 1992-4:2019-04) | Replaced ETAG 001 Annex C and CEN/TS 1992-4. All resistances now on **cylinder** strength f_ck (previously cube), so k-factors changed |
| Anchor product qualification | **EAD 330499-01-0601** (bonded), **EAD 330232** (mechanical), **EAD 330001** (headed) → issued as an **ETA** | The ETA supplies τ_Rk, h_ef range, c_min, s_min, h_min, ψ_sus, γ_inst |
| Headed studs as shear connectors | **EN 1994-1-1:2004 §6.6** | Composite action; different model from EN 1992-4 |
| Stud connectors used to anchor a steel member to an RC column | **EOTA TR 081:2022-06** (with EAD 160202-00-0301) | *Directly on-topic*: §2.2.5 "Connection between structural steel member and supporting reinforced concrete member" |
| Anchor channels | **EOTA TR 047** and EN 1992-4 §7.4 | |
| Steel plate / T-stub / weld design | **EN 1993-1-8:2005** (§6.2.5 for the base/anchor plate) | |
| Concrete corbels, partially-loaded areas | **EN 1992-1-1:2004** §6.5.3 (corbels), §6.7 (PLA), §8.4 (bond) | |
| Execution / tolerances | **EN 1090-2:2018** | |
| Stud geometry | **EN ISO 13918:2008** type **SD** | |
| US route | **ACI 318-19 Ch. 17**, **AISC Design Guide 1 (2nd ed.)**, **ACI 355.2 / 355.4** | Different symbols, same physics |
| Israel | **ת"י 466** (חוקת הבטון), **ת"י 1225 חלק 1.1** (June 2023 — Eurocode-aligned steel code; the 1998 חלק 1 remains valid in parallel for 3 years) | ⚠️ I could **not** verify a dedicated Israeli anchor standard. Normal Israeli practice is to design to **EN 1992-4 using the product's ETA**, or to ACI 318 Ch.17 using an ICC-ES report for US-origin products. Confirm with the SII before citing this in the report. |

**Units convention used below:** N, mm, MPa unless stated. ACI equations are quoted in their own unit systems and flagged.

---

## 1. THE ENGINEERING PRINCIPLES — THE "WHY" FOR A FABRICATOR

### 1.1 A steel-to-concrete connection is a *concrete* problem, not a steel problem

The single most important fact for the workshop: **in almost every real steel-to-concrete-column connection, the concrete governs, not the bolt.** In the worked examples in §11:

- an M20 8.8 chemical anchor has **N_Rd,s ≈ 131 kN** in steel, but only **≈ 18 kN** once the concrete cone is reduced by the edges of a 400 mm column — a factor of **7**;
- a cast-in plate with six ø19 studs has **357 kN** of steel shear capacity but only **≈ 97 kN** of concrete edge capacity.

So when a detailer "upsizes the bolt" to fix a failing connection, usually **nothing happens**. What changes the answer is *geometry*: embedment depth, edge distance, spacing, plate size, and reinforcement.

### 1.2 Why a vertical concrete element is harder than a slab

| | Slab / foundation | Column or wall |
|---|---|---|
| Edges near the anchor | Usually one, often none | **Two or three**, and they are close |
| Member width available | Large | Column is typically 300–600 mm; anchor group + 2×edge distance must fit inside it |
| Reinforcement | Two orthogonal mats, generous cover | **Dense cage**: 4–12 longitudinal bars in the corners plus links at 100–200 mm. The best anchor position (the corner) is exactly where the rebar is |
| Load direction | Shear usually acts *away* from edges | Vertical shear from a beam acts **parallel** to the two side faces, and tension from the moment is limited by **both** side faces simultaneously |
| Consequence of failure | Local | The column is a primary compression member. Splitting or blowout of a column face is a **structural**, not a local, failure |
| Redundancy | Slab redistributes | Little |

The direct consequence: **the effective embedment depth of a column anchor is capped by the column depth**, and the effective cone area is capped by the column width. In a 300 mm column you simply cannot develop a full concrete cone from a 170 mm deep anchor — the cone would need to be 510 mm across (3·h_ef) and only 300 mm exists.

### 1.3 The failure-mode hierarchy — what actually breaks

**Tension (EN 1992-4 Table 7.1):**
1. **Steel failure** of the anchor rod / stud — ductile, desirable, almost never governs near an edge
2. **Pull-out** — head or bond pulls through the concrete
3. **Concrete cone breakout** — a ~35° cone (EN 1992-4 idealises it as a pyramid with base 3·h_ef square) is pulled out
4. **Splitting** — the member cracks along the anchor axis; the anchor acts like a wedge
5. **Side-face blowout** — a deeply embedded headed anchor close to a face **spits out a lump of the side cover** without any cone forming at the surface. This is the failure mode people forget in narrow columns
6. **Failure of the supplementary reinforcement** (steel or anchorage)

**Shear (EN 1992-4 Table 7.2):**
1. **Steel failure** without lever arm
2. **Steel failure with lever arm** (stand-off / thick grout) — the anchor works in bending
3. **Concrete pry-out** — the anchor rotates and spalls concrete on the *far* side
4. **Concrete edge failure** — a half-cone breaks out toward a free edge. **This is the governing mode in columns.**
5. / 6. Failure of supplementary reinforcement (steel / anchorage)

**Principle to communicate to the fabricator:** modes 1–2 are *steel*, and they are ductile and predictable. Modes 3–5 are *concrete*, brittle, and depend entirely on where you drill the hole. Good design forces the failure into the steel — either by having enough concrete, or by adding **anchor (supplementary) reinforcement** that catches the breakout body.

### 1.4 The three actions and how each is transferred

For a steel beam or column fixed against a concrete column/wall face by an end plate:

**(a) Vertical load V_Ed** — transferred by:
- **Shear in the anchors** (bearing of the shank on the concrete just below the surface), and/or
- **Friction** under the compressed part of the plate, V = μ·D_Ed. EOTA TR 081 §2.2.5.6 permits this *only* if you can prove the contact: "appropriate measures shall be taken on site to ensure that frictional forces can be transmitted between the solid surfaces." Note TR 081 uses **two** friction coefficients: μ_inf (favourable, reduces anchor shear) and **μ_sup (unfavourable, increases the shear pushed toward the concrete edge)** — the same friction is a benefit in one check and a penalty in another, and/or
- **Direct bearing** on a nib, seating cleat, corbel or shear lug (best solution — see §8).

**(b) Horizontal load N_Ed (axial pull/push)** — tension goes into the anchors; compression goes straight into the concrete through the plate (a partially-loaded-area check, EN 1992-1-1 §6.7).

**(c) Moment M_Ed** — resolved into a **tension chord** and a **compression chord** with an internal lever arm. EOTA TR 081 §2.2.5.4, Eq. (20) gives the end-plate model explicitly:

```
Z_Ed = M_Ed/z0 + N_Ed·(0.5·h_plate)/z0 - ...        [TR 081 Eq. (20)]
D_Ed = M_Ed/z0 - N_Ed - 0.5·N_Ed·(...)/z0            [TR 081 Eq. (22)]
z0 = 0.9·d
```
where `d` = distance from the centroid of the main tension reinforcement (top anchor row) to the bottom edge of the end plate, `h_plate` = height of the fixture end plate. Then per anchor:
```
N_Ed(anchor) = Z_Ed / n_sc,T                          [TR 081 Eq. (25)]
V_Ed(anchor) = (V_Ed - V_Ed,fr,inf) / n_sc            [TR 081 Eq. (26)]
```
`n_sc,T` = number of connectors in the tension chord, `n_sc` = total number.

For a corbel-type connection TR 081 Eq. (17)–(19) adds a mandatory horizontal restraint force **H_Ed ≥ 0.2·V_Ed** from bearing friction — exactly matching the EN 1992-1-1 §6.5.3 corbel rule.

**The detailing consequence:** the tension chord (top row of anchors) is what sizes the connection. Getting the lever arm z0 as large as possible — i.e. making the **end plate tall**, not wide — is the cheapest single improvement. In a column, height is free (no edge), width is not.

---

## 2. DESIGN FORMULAE — EN 1992-4:2018, COMPLETE SET

### 2.1 Verification format

```
Design action ≤ Design resistance,  for EVERY failure mode, and for
  (a) the most loaded single fastener, and (b) the group.
```
Partial factors:
```
γ_Ms  (tension, steel)  = 1.2·(f_uk/f_yk) ≥ 1.4
γ_Ms  (shear, steel)    = 1.0·(f_uk/f_yk) ≥ 1.25   for f_uk ≤ 800 MPa and f_yk/f_uk ≤ 0.8
                        = 1.5                        otherwise
γ_Mc  = γ_c · γ_inst    (concrete failure modes)
        γ_c = 1.5 (EN 1992-1-1 recommended), γ_inst from the ETA (1.0 / 1.2 / 1.4 by installation-safety class)
        γ_inst = 1.0 for SHEAR loading
γ_Ms,re = 1.15 (supplementary reinforcement)
γ_M2  = 1.25 (EN 1993-1-8, bolts/welds)
```
*Source: EN 1992-4 §4.4.3 / IDEA StatiCa EN anchor check documentation.*

### 2.2 TENSION

**Steel failure (§7.2.1.3)**
```
N_Rd,s = N_Rk,s / γ_Ms      with   N_Rk,s = A_s · f_uk        (from the ETA)
```
`A_s` = tensile stress area of the threaded rod / stud shank; `f_uk` = characteristic ultimate strength.

**Pull-out, cast-in headed fasteners and headed studs (§7.2.1.5)**
```
N_Rk,p = k2 · A_h · f_ck
  k2 = 7.5  (cracked concrete)
  k2 = 10.5 (uncracked concrete)
  A_h = bearing area of the head
      = (π/4)(d_h² − d²)  circular head
      = a_wp² − (π/4)d²   square washer plate
  constraint: d_h ≤ 6·t_h + d
```
`d_h` = head diameter, `t_h` = head thickness, `d` = shank diameter.

**Pull-out / combined pull-out + cone, bonded fasteners (§7.2.1.6)**
```
N⁰_Rk,p = ψ_sus · π · d · h_ef · τ_Rk
```
`τ_Rk` = characteristic bond strength from the ETA (function of concrete class, cracked/uncracked, temperature range, drilling and cleaning method). **`ψ_sus` is the sustained-load factor introduced by EN 1992-4** — the whole point of §9 below. It is a *product-dependent* factor, based on ψ⁰_sus in the ETA and on α_sus = (sustained action)/(total action at ULS).

> *Real value:* Hilti HIT-HY 200 technical datasheet (per ETA): **"For long term loading please apply ψ_sus = 0.74."** i.e. a **26 % penalty** on bond strength for permanently sustained tension.

**Concrete cone breakout (§7.2.1.4)** — the workhorse equation
```
N_Rk,c = N⁰_Rk,c · (A_c,N / A⁰_c,N) · ψ_s,N · ψ_re,N · ψ_ec,N · ψ_M,N

N⁰_Rk,c = k1 · √f_ck · h_ef^1.5
  k1 = 8.9  cast-in headed fasteners / headed studs, CRACKED concrete
  k1 = 12.7 cast-in headed, UNCRACKED
  k1 = 7.7  post-installed (mechanical & bonded), CRACKED
  k1 = 11.0 post-installed, UNCRACKED

c_cr,N  = 1.5·h_ef          (critical edge distance)
s_cr,N  = 3.0·h_ef          (critical spacing)
A⁰_c,N  = s_cr,N² = (3 h_ef)²
A_c,N   = the ACTUAL projected area, clipped by member edges and by
          overlapping neighbouring cones

ψ_s,N  = 0.7 + 0.3·(c / c_cr,N) ≤ 1.0      c = smallest edge distance
ψ_re,N = 0.5 + h_ef/200 ≤ 1.0               (shell-spalling factor; = 1.0 for h_ef ≥ 100 mm)
ψ_ec,N = 1 / (1 + 2·e_N/s_cr,N) ≤ 1.0       computed per direction, then multiplied
ψ_M,N  = 2 − z/(1.5 h_ef) ≥ 1.0             favourable compression under the plate;
                                             = 1.0 if c < 1.5h_ef, or C/ΣN < 0.8, or z/h_ef ≥ 1.5
```
**Three-or-more-close-edges rule (§7.2.1.4):** if the fastening has ≥ 3 edges within c_cr,N (i.e. a narrow column), you must replace h_ef by a *reduced* value
```
h'_ef = max{ (c_max/c_cr,N)·h_ef ,  (s_max/s_cr,N)·h_ef }
```
and recompute **everything** (N⁰_Rk,c, c_cr,N, s_cr,N, A_c,N, A⁰_c,N, ψ_s,N, ψ_ec,N) with h'_ef. **This is the clause that bites in columns** — it prevents you from claiming credit for embedment you cannot develop. Practically it means: *in a narrow column, driving the anchor deeper stops helping beyond a certain depth.*

**Splitting (§7.2.1.7)**
```
c_cr,sp, s_cr,sp = 2·c_cr,sp   from the ETA
```
> *Real value:* Hilti HAS-U + HIT-HY 200 (from the technical datasheet, per ETA):
> `c_cr,sp = 1.0·h_ef` for h/h_ef ≥ 2.00; `= 4.6·h_ef − 1.8·h` for 2.00 > h/h_ef > 1.3; `= 2.26·h_ef` for h/h_ef ≤ 1.3.
> Note the perverse result: **a thin member (h/h_ef ≤ 1.3) needs c_cr,sp = 2.26·h_ef** — for h_ef = 170 mm that is **384 mm of edge distance**, impossible in a 400 mm column. In columns you normally suppress splitting by keeping h/h_ef ≥ 2 and by relying on the column links as splitting reinforcement.

**Side-face (concrete) blowout (§7.2.1.8)** — *only headed fasteners, and only when c ≤ 0.5·h_ef*
```
N_Rk,cb = N⁰_Rk,cb · (A_c,Nb / A⁰_c,Nb) · ψ_s,Nb · ψ_g,Nb · ψ_ec,Nb

N⁰_Rk,cb = k5 · c1 · √A_h · √f_ck
  k5 = 8.7  (cracked)      k5 = 12.2 (uncracked)
A⁰_c,Nb  = (4·c1)²
s_cr,Nb  = 4·c1        (anchors are a "blowout group" if s ≤ 4c1)

ψ_s,Nb  = 0.7 + 0.3·c2/(2·c1) ≤ 1                       [EN 1992-4 Eq. (7.28)]
ψ_g,Nb  = √n + (1−√n)·s2/(4c1) ≥ 1  , with s2 ≤ 4c1     [Eq. (7.29)]
ψ_ec,Nb = 1 / (1 + 2·e_N/(4c1)) ≤ 1                     [Eq. (7.30)]

Actual areas [EN 1992-4 Fig. 7.8]:
  a) A_c,Nb = 4·c1·(c2 + s2 + 2·c1)      for c2 ≤ 2c1 , s2 ≤ 4c1
  b) A_c,Nb = (2·c1 + f)·(4·c1 + s2)     for f ≤ 2c1 , s2 ≤ 4c1
```
**Why this matters in a column:** blowout has *no cone at the surface*. A deeply embedded headed anchor 60 mm from the side face of a 300 mm column will spall a slab of side cover off the column at a load far below the cone capacity, and the anchor plate on the front face will show no distress at all until it happens. It is only checked when `c1 ≤ 0.5·h_ef` — but in narrow columns that condition is met all the time.

### 2.3 SHEAR

**Steel failure, no lever arm (§7.2.2.3.1)**
```
V⁰_Rk,s = k6 · A_s · f_uk                              [Eq. (7.34)]
  k6 = 0.6  for f_uk ≤ 500 MPa
  k6 = 0.5  for 500 < f_uk ≤ 1000 MPa
  × 0.8 if h_ef/d < 5 AND concrete class < C20/25

V_Rk,s = k7 · V⁰_Rk,s                                  [Eq. (7.35)]
  k7 = 1.0 single fastener; k7 = 1.0 for ductile steel in a group;
  k7 = 0.8 for steel with rupture elongation A5 < 8 %

with a grout layer t_grout ≤ d/2, in uncracked concrete:
V_Rk,s = (1 − 0.01·t_grout)·k7·V⁰_Rk,s                 [Eq. (7.36)]
```

**Steel failure WITH lever arm (stand-off / thick grout) (§7.2.2.3.2)**
```
V_Rk,s,M = α_M · M_Rk,s / l_a                          [Eq. (7.37)]
M_Rk,s   = M⁰_Rk,s · (1 − N_Ed/N_Rd,s)                 [Eq. (7.38)]
M⁰_Rk,s  = 1.2 · W_el · f_uk ,  W_el = π d³/32
α_M      = 1.0 (free rotation at the fixture) … 2.0 (full restraint both ends)
l_a      = 0.5·d_nom + t_grout + 0.5·t_baseplate
```
**Fabrication consequence:** every extra millimetre of grout or packing multiplies the anchor's bending moment. A 25 mm grout bed under a stand-off plate can halve the shear capacity of an M20 anchor. Detail packs tight, or use a shear nib.

**Concrete pry-out (§7.2.2.4)**
```
without supplementary reinforcement:   V_Rk,cp = k8 · N_Rk,c            [Eq. (7.39a)]
with supplementary reinforcement:      V_Rk,cp = 0.75 · k8 · N_Rk,c     [Eq. (7.39b)]
bonded fasteners:                      V_Rk,cp = k8 · min{N_Rk,c ; N_Rk,p}   [(7.39c/d)]
  k8 from the ETA; commonly k8 = 1 for h_ef < 60 mm, k8 = 2 for h_ef ≥ 60 mm
```
For anchor groups loaded in *opposing* directions (torsion), assume a **virtual edge at c = 0.5·s** toward the neighbouring fastener when computing A_c,N (§7.2.2.4(4), Fig. 7.11).

**CONCRETE EDGE FAILURE (§7.2.2.5)** — the critical one for columns
```
V_Rk,c = V⁰_Rk,c · (A_c,V / A⁰_c,V) · ψ_s,V · ψ_h,V · ψ_ec,V · ψ_α,V · ψ_re,V   [Eq. (7.40)]

V⁰_Rk,c = k9 · d_nom^α · l_f^β · √f_ck · c1^1.5                                  [Eq. (7.41)]
  k9 = 1.7 cracked ;  k9 = 2.4 uncracked
  α  = 0.1·(l_f/c1)^0.5                                                          [Eq. (7.42)]
  β  = 0.1·(d_nom/c1)^0.2                                                        [Eq. (7.43)]
  l_f = h_ef                for a uniform-diameter shank
      = 12·d_nom            for d_nom ≤ 24 mm  (cap)
      = max{8·d_nom; 300}   for d_nom > 24 mm  (cap)
  → l_f = min(h_ef, cap)

A⁰_c,V = 4.5·c1²                                                                  [Eq. (7.44)]
A_c,V  = actual projected area on the SIDE face, limited by:
         overlapping cones  (s ≤ 3·c1)
         parallel edges     (c2 ≤ 1.5·c1)
         member thickness   (h < 1.5·c1)
  e.g. single fastener at a corner: A_c,V = 1.5c1·(1.5c1 + c2)
       group at an edge, thin member: A_c,V = (2·1.5c1 + s2)·h

ψ_s,V  = 0.7 + 0.3·c2/(1.5·c1) ≤ 1.0                                              [Eq. (7.45)]
ψ_h,V  = (1.5·c1/h)^0.5 ≥ 1.0                                                     [Eq. (7.46)]
ψ_ec,V = 1/(1 + 2·e_V/(3·c1)) ≤ 1.0
ψ_α,V  = 1/√(cos²α_V + (0.5·sin α_V)²) ≥ 1.0
ψ_re,V = reinforcement factor (see note below)
```

**⚠️ THE MOST USEFUL SINGLE NUMBER IN THIS WHOLE BRIEF:**
For shear acting **parallel** to the edge (α_V = 90°), `ψ_α,V = 1/√(0 + 0.25) = **2.0**`.

This is why a beam hanging off the face of a column often works: the vertical shear runs *parallel* to the two side faces, so the edge resistance is **doubled**. If the same connection had to resist a horizontal thrust *toward* a side face, its capacity would halve. **Tell the detailer: orientation of the load relative to the free edge is worth a factor of two.**
(ACI 318-19 §17.7.2.1(c) says the same thing differently: for shear parallel to an edge, V_cb may be taken as **twice** the perpendicular value with ψ_ed,V = 1.0.)

**ψ_re,V (§7.2.2.5(13)):** takes account of edge reinforcement and closely-spaced stirrups. ⚠️ **Sources disagree**: IDEA StatiCa's implementation and the CivilWeb summary both state ψ_re,V = 1.0 and note that values > 1.0 are only permitted in **cracked** concrete when h_ef > 2.5 × cover to the edge reinforcement (EN 1992-4 explicitly limited this factor to cracked concrete in the 2018 edition — see the DIN foreword). The commonly-cited values from the predecessor documents are **1.0 (no edge reinforcement) / 1.2 (straight edge bars ≥ ø12) / 1.4 (edge bars + closely-spaced stirrups or mesh at ≤ 100 mm)**. **Verify against the purchased copy of EN 1992-4 §7.2.2.5(13) before publishing these three numbers.**

**Narrow / thin member rule:** when the fastening is close to *several* edges, replace c1 with
```
c1' = max{ c2,max/1.5 ; h/1.5 ; s2,max/3 }
```
and use c1' throughout. Again — the code refuses to let a narrow column pretend to be a wide one.

**Torsion caveat (§7.2.2.5(8)):** a two-anchor group in torsion with overlapping breakout bodies — multiply V_Rk,c by **0.8** if the resistance ratio > 0.7 and s2 ≤ s_crit, where s_crit = 1.5h_ef + 1.5c1 (second fastener governed by pry-out) or 1.5c1 (governed by edge failure at a second edge).

**Which anchors count:** §7.2.2.5(2) — **only the fasteners closest to the edge** are used for the edge-failure verification. §7.2.2.5(4): minimum spacing in a group **s_min ≥ 4·d_nom** for edge-failure purposes.

**Embedded base plate rule (§7.2.2.5(1)):** the provisions are valid only if the base plate thickness in contact with the concrete `t < 0.25·h_ef` when `c ≤ max{10h_ef; 60d}`.

### 2.4 INTERACTION
```
Steel:    (N_Ed/N_Rd,s)² + (V_Ed/V_Rd,s)² ≤ 1.0                [EN 1992-4 Eq. (7.54)]
Concrete: (N_Ed/N_Rd,i)^1.5 + (V_Ed/V_Rd,i)^1.5 ≤ 1.0           [Eq. (7.55)]
          using the LARGEST utilisation among the concrete modes in each direction
```
ACI 318 equivalent: `(N_ua/φN_n)^ζ + (V_ua/φV_n)^ζ ≤ 1.0` with **ζ = 5/3**, or the simplified tri-linear with the 1.2 sum.

### 2.5 SUPPLEMENTARY (ANCHOR) REINFORCEMENT — the professional solution to the edge problem

This is the single most important design tool for anchoring into a column, and it is under-used in practice.

**The principle:** if you provide reinforcement that crosses the assumed breakout body and is properly anchored on both sides of it, **you do not have to verify the concrete breakout mode at all** — instead you design the reinforcement to carry the *entire* load. EN 1992-4 §7.2.2.2(1): *"When the design relies on supplementary reinforcement, concrete edge failure according to Table 7.2 and 7.2.2.5 need not be verified but the supplementary reinforcement shall be designed according to 7.2.2.6 to resist the total load."*

**Detailing rules — EN 1992-4 §7.2.2.2(3), verbatim numbers:**
- (a) if sized for the most loaded fastener, **the same reinforcement must be provided around all fasteners** counted as effective for edge failure
- (b) **ribbed bars, f_yk ≤ 600 N/mm², diameter not larger than 16 mm**, mandrel diameter per EN 1992-1-1
  > ⚠️ *Sources disagree:* the EN 1992-4 text reads **f_yk ≤ 600 MPa**; the CivilWeb summary states 500 MPa. Use **600** (code text) but note B500B is what you will actually buy.
- (c) bars must be **within 0.75·c1** of the fastener (for shear). For tension: **within 0.75·h_ef**
- (d) **anchorage length inside the breakout body: min l1 = 10·ø for straight bars** (with or without welded transverse bars), **min l1 = 4·ø for bars with a hook, bend or loop**
- (e) the breakout body assumed must be the same as for the edge-failure calculation
- (f) **edge reinforcement must be provided along the member edge** and designed by a strut-and-tie model; a **45° compression strut** may be assumed as a simplification
- §7.2.2.2(2): the reinforcement must be anchored **outside** the failure body with l_bd per EN 1992-1-1, and lapped into the member's own reinforcement
- §7.2.2.2(4): **if stirrups or loops are used, they must enclose and be in contact with the anchor shaft, positioned as close as possible to the fixture** — then no anchorage-length verification inside the breakout body is required, because direct force transfer from anchor to stirrup is assumed. **This is the detail to draw.**

**Resistances:**
```
Steel failure of the supplementary reinforcement (§7.2.1.9.1):
  N_Rk,re = Σ_{i=1..n_re} A_s,re,i · f_yk,re ,     f_yk,re ≤ 600 N/mm²      [Eq. (7.31)]
  N_Rd,re = N_Rk,re / γ_Ms,re         (γ_Ms,re = 1.15)

Anchorage failure in the cone (§7.2.1.9.2):
  N_Rd,a = Σ N⁰_Rd,a,i                                                       [Eq. (7.32)]
  N⁰_Rd,a = (l1 · π · ø · f_bd) / (α1 · α2) ≤ A_s,re·f_yk,re/γ_Ms,re         [Eq. (7.33)]
     l1     = anchorage length inside the breakout body (≥ the minimum in §7.2.1.2(2)d)
     f_bd   = design bond strength, EN 1992-1-1 §8.4.2
     α1, α2 = EN 1992-1-1 §8.4.4 shape/cover coefficients
```
Tension in the reinforcement caused by a **shear** load (EN 1992-4 Eq. (6.6)):
```
N_Ed,re = V_Ed · (e_s/z + 1)
   e_s = distance between the supplementary reinforcement and the shear force on the fixture
   z   = internal lever arm ≈ 0.85·d , with d = min{2h_ef ; 2c1}
```
⚠️ I could not open the clause text for Eq. (6.6) itself; the symbol definitions above come from secondary descriptions of EN 1992-4. **Check the exact form of Eq. (6.6) in the purchased standard.**

**Practical translation for the drawing:** *"Provide 2 no. ø12 B500B closed links at 80 mm centres, wrapped tight around the shafts of the top anchors, hard against the back face of the anchor plate, lapped 40ø into the column cage."* That sentence can multiply a connection's capacity by 3–5.

### 2.6 ACI 318-19 / AISC EQUIVALENTS (for cross-checking and for US-origin products)

```
Tension steel:   φN_sa = φ·A_se,N·f_uta                        φ = 0.75 (ductile) / 0.65
Tension cone:    φN_cbg = φ·(A_Nc/A_Nco)·ψ_ec,N·ψ_ed,N·ψ_c,N·ψ_cp,N·N_b
    A_Nco = 9·h_ef²        (compare EN: (3h_ef)² = 9h_ef²  — identical geometry)
    N_b   = k_c·λ_a·√f'c·h_ef^1.5      k_c = 24 (cast-in), 17 (post-installed, or ETA value)
    for 11 in ≤ h_ef ≤ 25 in:  N_b = 16·λ_a·√f'c·h_ef^(5/3)
    ψ_ed,N = 0.7 + 0.3·c_a,min/(1.5h_ef) ≤ 1
    ψ_c,N  = 1.0 cracked / 1.25 uncracked (cast-in)
    ψ_ec,N = 1/(1 + 2e'_N/(3h_ef))
    ψ_cp,N = min(c_a,min/c_ac , 1)
Pullout:         φN_pn = φ·ψ_c,P·8·A_brg·f'c          (headed) ; ψ_c,P = 1.0 / 1.4
Side-face blowout: N_sb = 160·c_a1·√A_brg·λ_a·√f'c   [in.-lb]   ≡ 13·c_a1·√A_brg·λ_a·√f'c [SI]
                   applies when h_ef > 2.5·c_a1
Shear steel:     φV_sa = φ·0.6·A_se,V·f_uta   (×0.8 with a grout/mortar joint)
Shear breakout:  φV_cbg = φ·(A_Vc/A_Vco)·ψ_ec,V·ψ_ed,V·ψ_c,V·ψ_h,V·V_b
    A_Vco = 4.5·c_a1²        (identical to EN 1992-4 Eq. 7.44)
    V_b   = min{ 7·(l_e/d_a)^0.2·√d_a·λ_a·√f'c·c_a1^1.5 ; 9·λ_a·√f'c·c_a1^1.5 }
    ψ_ed,V = 0.7 + 0.3·c_a2/(1.5·c_a1) ≤ 1
    ψ_h,V  = √(1.5c_a1/h_a) ≥ 1
    ψ_c,V  = 1.0 cracked / 1.4 uncracked
Pryout:          φV_cp = φ·k_cp·N_cp ,  k_cp = 1.0 (h_ef < 2.5 in) / 2.0 (h_ef ≥ 2.5 in)
Interaction:     (N_ua/φN_n)^(5/3) + (V_ua/φV_n)^(5/3) ≤ 1.0
```
**Cross-check note for the report:** the two codes are the same physics (both descend from the Concrete Capacity Design / CCD method) but they are **not** numerically identical — EN uses characteristic resistances ÷ γ_Mc = 1.5, ACI uses nominal × φ = 0.65–0.75, and the k-factors differ because EN works on cylinder strength and ACI on f'c with different calibration. **Do not mix them inside one calculation.**

---

## 3. THE BEARING / ANCHOR PLATE ON A CONCRETE COLUMN FACE — TYPICAL DETAIL

### 3.1 Typical arrangement

**The standard industrial detail** for a steel beam framing into the side of an RC column:

```
   ┌─── RC column 400×400, C30/37 ───┐
   │  ▓▓▓ column cage: 8ø20 + ø10@150 │
   │                                  │
   │   ●        ●   ← top (tension) anchor row
   │        ┌───────────┐
   │        │ end plate │──── shear tab / cleat ──── IPE/HEA beam
   │   ●    │  20 mm    │  ●
   │        └───────────┘
   │   ●        ●   ← bottom (compression) row
   └──────────────────────────────────┘
```

| Parameter | Typical value | Basis |
|---|---|---|
| Plate thickness t_p | **15–30 mm**; must satisfy t_p ≥ c·√(3·f_jd·γ_M0/f_y) (EN 1993-1-8 §6.2.5(4) rearranged) | EN 1993-1-8 |
| PCI/US rule for cast-in embeds | t_plate ≥ **2/3 × stud diameter**, and ≥ 0.5 × anchor diameter | PCI Design Handbook |
| Common US standard embed | **¾ in (19 mm) plate with ¾ in × 6 in headed studs** | PCI practice |
| Number of anchors | **minimum 4**; typically 4–8; OSHA requires 4 for column bases | AISC DG1 §2.7 |
| Anchor rows | 2 (moment couple) or 3 | |
| Plate proportion | **tall and narrow**, not square — maximise the lever arm z0, minimise edge encroachment | TR 081 Eq. (20) |
| Edge of plate to column arris | ≥ 40–50 mm for concrete cover + tolerance | practice |
| Base plate hole sizes (AISC DG1 Table 2.3, for cast-in rods) | ¾″ rod → 1 5⁄16″ hole, min washer 2″ × ¼″ thick; 1½″ rod → 2¾″ hole, 4″ washer × 5⁄8″; 2″ rod → 3¼″ hole, 5″ washer × ¾″ | AISC DG1 |
| Post-installed anchor clearance hole in the fixture, d_f (max) | M12 → 14; M16 → 18; M20 → 22; M24 → 26; M30 → 33 mm | Hilti HAS-U setting details |

### 3.2 The plate must be stiff enough to be believed

Both EN 1992-4 and TR 081 assume a **rigid** plate so that the linear strain distribution / chord model is valid ("*The calculations assume that the steel plate is sufficiently rigid such that linear strain distribution will be valid (analogous to Bernoulli hypothesis)*" — Peikko WELDA Technical Manual §1.1). If the plate is flexible, the corner anchors take far more than their share (prying), and the tension-row assumption is wrong. **Rule for the detailer: if the plate visibly dishes when you tighten it, the calculation is void.** Add stiffeners (ריפים) between the beam web/flanges and the plate.

### 3.3 Stand-off (ungrouted) connections

Where the plate is held off the concrete on levelling nuts (common when the concrete face is out of plumb):
- the anchor works in **bending + shear + tension**;
- use EN 1992-4 §7.2.2.3.2 with α_M = 2.0 only if the fixture genuinely restrains rotation (a thick plate with nuts both sides);
- buckling of the exposed anchor length must be considered;
- Hilti publish an alternative "SOFA" method for ungrouted stand-off, with a reduction factor on concrete shear failure due to anchor bending.

**Detailing preference:** avoid stand-off on a column face. Either grout the gap solid with a non-shrink cementitious grout (then t_grout ≤ d/2 lets you use Eq. 7.36), or shim tight with steel packs and seal.

### 3.4 Compression transfer — partially loaded area

The compression chord bears directly on the concrete. Check EN 1992-1-1 §6.7:
```
F_Rdu = A_c0 · f_cd · √(A_c1/A_c0)  ≤  3.0 · f_cd · A_c0
   A_c0 = loaded area (plate contact area, less holes)
   A_c1 = design distribution area, geometrically similar to A_c0 and concentric with it
   f_cd = α_cc·f_ck/γ_c
```
The `≤ 3.0 f_cd A_c0` cap is the reason a small bearing plate on a big column face carries surprisingly high load — **up to 3× the cylinder design strength** because of confinement. But the *transverse tension* generated by the spreading must be reinforced (links).

---

## 4. POST-INSTALLED ANCHORING INTO EXISTING COLUMNS AND WALLS

### 4.1 What actually constrains the drilling

1. **Hole depth.** Drill depth = h_ef (+ allowance). In a 300 mm column you cannot use h_ef = 250 mm without risk of breaking out the back face. Practical limit: **h_ef ≤ (column depth − 100 mm)**.
2. **Drill access.** A rotary hammer needs roughly `h_ef + 250 mm` of clear space behind the drill. Against a wall corner or next to an existing pipe rack this is often the real limit, not the code.
3. **Drilling deviation.** Without a drill rig, hole axis deviation of **1–2°** over 200 mm depth (3.5–7 mm at the bottom) is normal. Two anchors drilled independently will not be parallel. **This is why clearance holes in anchor plates are generous (d_f up to d + 4 mm for chemical anchors) and why anchor plates should be used as their own drilling template.**
4. **Concrete condition.** EN 1992-4 §4.7 requires you to *determine* whether the concrete is cracked or uncracked in the anchor region. **In a column, the anchor zone under a beam reaction is in the tension face of a bending column — assume CRACKED unless proven otherwise.** This costs you ~30 % (k1: 11.0 → 7.7; k9: 2.4 → 1.7; k2: 10.5 → 7.5). Do not let anyone use uncracked values on a column face.
5. **Existing concrete strength.** In an old building this must be established by cores or rebound + cores. If the column turns out to be C16/20 the whole design shifts.
6. **Age of concrete for adhesives.** ACI 318-19 §26.7 requires concrete to be **aged at least 21 days** before installing an adhesive anchor (previously 7 days).

### 4.2 Rebar interference and rebar scanning — the real-world governing issue

**A 400×400 column typically contains 4–8 longitudinal ø16–ø25 bars in the corners and links ø8–ø10 at 100–200 mm centres, with 25–35 mm cover.** Every corner region — the natural place for an anchor group — is occupied.

**Scanning practice (this is now standard of care, not optional):**

| Method | Typical equipment | Depth / accuracy | What it finds |
|---|---|---|---|
| **Ferroscan** (pulse-induction electromagnetic) | Hilti PS 300 | to **200 mm**, ±3 mm; estimates bar diameter, spacing and cover | steel reinforcement only, near-surface, high accuracy |
| **GPR** (ground penetrating radar) | Hilti PS 1000 X-Scan, Proceq GP8000 | **300–700 mm** | rebar, post-tension tendons, conduits, voids |

**Best practice is to use both:** the ferroscan gives precise near-surface bar positions and cover; GPR fills in the deeper picture and finds non-ferrous items. Then mark the drilling positions on the concrete face in paint, photograph, and *record on the as-built drawing*.

**What to do when the drill hits rebar (Hilti's published position, and the correct engineering position):**
1. **Move the fastening point** — the first and preferred solution.
2. If the geometry cannot move, consider cutting the bar — **but the decision to continue drilling upon hitting rebar rests with the Engineer of Record (EOR)**, because it is outside the scope of the anchor manufacturer's testing and approval.
3. Never cut a **post-tensioning tendon** — severing a prestressed tendon causes immediate and potentially catastrophic failure. GPR before drilling is mandatory where PT is possible.
4. Where cutting a *link* is unavoidable, the EOR should assess loss of confinement and shear capacity; a link is often more important than a longitudinal bar for anchoring purposes (see §5.5 — the links **are** your anchor reinforcement).

**Tools that reduce rebar strikes:**
- **Hollow drill bits (HDB)** with integrated dust extraction — remove dust as you drill, so hole cleaning is automatic and the operator can feel the bit better;
- **diamond core drilling** — cuts *through* rebar cleanly, but produces a **smooth hole wall**, which reduces bond strength for adhesives. Bond values for diamond-drilled holes are separately listed (lower) in the ETA and **must not be taken from the hammer-drilled table**;
- drilling aids / rigs to control perpendicularity.

### 4.3 Effect of the column reinforcement cage

Three separate effects, all worth stating in the report:

1. **Obstruction** (above).
2. **Beneficial confinement.** Column links crossing the potential breakout body *are* supplementary reinforcement, provided they satisfy the §7.2.2.2(3) rules (within 0.75c1 / 0.75h_ef, ø ≤ 16, anchored). EOTA TR 081 §2.2.5.13 explicitly says: *"Existing column ties may be taken into account for the cross-sectional area of splitting reinforcement when placed in the section of splitting failure."* **You get this for free if you position the anchor group to land between two link levels rather than on one.**
3. **Load path.** The anchor force must ultimately reach the column's own longitudinal reinforcement. TR 081 §2.2.5.2 requires, for a cast-in stud connector plate on a column: *one column longitudinal bar at each corner on the near face of the connection*, plus *additional longitudinal reinforcement between the sockets on the near face*, plus a **stepped distribution of column ties as splitting reinforcement below each connector layer at s ≤ 100 mm and s ≤ 50 mm** in the immediate load-introduction zone.

### 4.4 Anchor product data — real numbers (Hilti HIT-HY 200-A/R + HAS-U rod, per ETA)

These are the *setting details* — the numbers that decide whether your detail can be built:

| Anchor size | M8 | M10 | M12 | M16 | M20 | M24 | M27 | M30 |
|---|---|---|---|---|---|---|---|---|
| Drill bit ø d0 [mm] | 10 | 12 | 14 | 18 | 22 | 28 | 30 | 35 |
| **h_ef,min** [mm] | 60 | 60 | 70 | 80 | 90 | 96 | 108 | 120 |
| **h_ef,max** [mm] | 160 | 200 | 240 | 320 | 400 | 480 | 540 | 600 |
| Max clearance hole in fixture d_f [mm] | 9 | 12 | 14 | 18 | 22 | 26 | 30 | 33 |
| Max torque T_max [Nm] | 10 | 20 | 40 | 80 | 150 | 200 | 270 | 300 |
| **s_min** [mm] | 40 | 50 | 60 | 75 | 90 | 115 | 120 | 140 |
| **c_min** [mm] | 40 | 45 | 45 | 50 | 55 | 60 | 75 | 80 |
| Min member thickness h_min | h_ef + 30 mm ≥ 100 mm (small sizes) / **h_ef + 2·d0** (large sizes) | | | | | | | |

*Source: Hilti HIT-HY 200-A/R Injection Mortar technical datasheet (data "according to the ETA approval for the product"), update May-2020.*

Also from the same datasheet — **the numbers that catch people out:**
- **ψ_sus = 0.74** for long-term (sustained) loading;
- **c_cr,sp** = 1.0·h_ef / (4.6h_ef − 1.8h) / 2.26·h_ef depending on h/h_ef (see §2.2);
- **c_cr,N = 1.5·h_ef**;
- Temperature ranges: **I** (base material −40 to +40 °C, max long-term +24 °C), **II** (to +80 °C, long-term +50 °C), **III** (to +120 °C, long-term +72 °C). Higher temperature range = lower bond strength;
- **Working time / curing time vs base-material temperature (HY 200-R):**

| Base material temp | Max working time t_work | Min curing time t_cure |
|---|---|---|
| −10 to −5 °C | 3 h | 20 h |
| −5 to 0 °C | 2 h | 8 h |
| 0 to 5 °C | 1 h | 4 h |
| 5 to 10 °C | 40 min | 2.5 h |
| 10 to 20 °C | 15 min | 1.5 h |
| 20 to 30 °C | 9 min | 1 h |
| 30 to 40 °C | 6 min | 1 h |

(For the fast-cure HY 200-A the working time at 20–30 °C is only **4 minutes**.) **On a hot Israeli summer day on a south-facing column face, the installer has minutes, not hours.** This must appear in the method statement.

---

## 5. THE EDGE PROBLEM IN A CONCRETE COLUMN — THE GOVERNING ISSUE

### 5.1 Why edges dominate

Look at the exponents:
- concrete cone: `N ∝ h_ef^1.5` — capacity grows with embedment
- **concrete edge in shear: `V ∝ c1^1.5`** — capacity grows with the **1.5 power of the edge distance**

Halving the edge distance from 110 mm to 55 mm reduces V⁰_Rk,c by a factor of `2^1.5 = 2.83` — and then the projected-area ratio A_c,V/A⁰_c,V shrinks too. In a narrow column the answer collapses fast.

Meanwhile, the geometry available is fixed by the column: for a column of width **b** with an anchor group of horizontal spacing **s2**:
```
c2 (each side) = (b − s2)/2
```
So s2 and c2 trade directly against each other, and *both* matter:
- **large s2** → good for the tension cone area A_c,N and good for the moment lever, **bad** for edge distance
- **small s2** → good edge distance, **bad** cone area (cones overlap) and small torsional resistance

**There is an optimum, and it is usually s2 ≈ b/2 to 2b/3.** Worth stating explicitly in the report, with the example in §11.

### 5.2 The three edge-related failure modes to check in a column

| Mode | When it applies | Formula | Symptom |
|---|---|---|---|
| **Concrete edge in shear** V_Rk,c | Always, for both side faces | §2.3 | Half-cone spalls off the side face of the column |
| **Side-face blowout** N_Rk,cb | Headed anchors only, **c1 ≤ 0.5·h_ef** | §2.2 | Slab of side cover blows off; no surface distress |
| **Splitting** N_Rk,sp | c < c_cr,sp, or h/h_ef small | ETA | Vertical crack along the anchor line; column loses confinement |

### 5.3 Mitigations — in order of engineering preference

1. **Move the anchor group inward / reduce s2.** Free. Costs you lever arm and cone area — check both.
2. **Increase h_ef.** Helps the tension cone linearly-ish (h_ef^1.5) and helps l_f in the shear formula weakly (l_f^β, β ≈ 0.07–0.17 — almost nothing). **Deeper embedment barely helps concrete edge shear.** This surprises people. Watch the `h'_ef` reduction of §2.2 in narrow members, and the risk of hitting the back face.
3. **Make the plate larger / add more anchors in the vertical direction.** Vertical spacing s1 is "free" — there is no edge above or below (mid-height of a column). Adding rows increases A_c,V (see the `(3c1 + s1 + s1 + 3c1)` term) and shares the tension. **This is the cheapest real fix: go tall.**
4. **Rotate the load.** If the resultant shear can be made parallel to the near edge instead of toward it, ψ_α,V = 2.0 — a free doubling.
5. **Add anchor reinforcement (§2.5).** Closed links ø10–ø12 B500B wrapped around the anchor shafts, hard against the plate, lapped into the cage. Then **the concrete edge check is waived** and the reinforcement carries everything. Requires: chasing the cover, drilling for the links, or — much better — a cast-in solution.
6. **Through-bolt (§6).** Eliminates the concrete tension limit states completely.
7. **Wrap the column** with a steel collar / FRP so that the "bracket" clamps rather than anchors. Used for heavy retrofit loads.
8. **Add a concrete or steel corbel** so the vertical load is carried in bearing, not by anchors (§8).

---

## 6. THROUGH-BOLTING A CONCRETE WALL OR COLUMN

### 6.1 When it is used

- edge distances fail and cannot be improved;
- the existing column is narrow (250–350 mm) and heavily reinforced;
- high sustained tension where adhesive creep is a concern (§9);
- fire performance is critical (steel bolt in a hole, no adhesive to soften);
- both faces are accessible (**this is the hard constraint** — it usually rules the detail out for a perimeter column against a façade, and rules it in for an internal column or a core wall).

### 6.2 Why it works — the limit states disappear

Through-bolting **converts the connection from an anchorage problem into a clamping problem.** The concrete is placed in *compression* between a front plate and a rear backing plate. There is:
- **no concrete cone** (the tension is reacted by bearing on the far face, not by cone pull-out);
- **no side-face blowout** (no embedded head inside the section);
- **no bond, so no creep, no cure time, no temperature derating**;
- **no rebar interference for the anchorage** — although you still must scan, because you are now drilling *right through* the section and will certainly meet links.

Remaining limit states:
1. **Bolt tension and shear** (EN 1993-1-8): `F_t,Rd = k2·f_ub·A_s/γ_M2` with k2 = 0.9; `F_v,Rd = α_v·f_ub·A_s/γ_M2`.
2. **Bearing of the backing plate on the far face** — EN 1992-1-1 §6.7 partially loaded area (§3.4). Usually enormous.
3. **Punching / local shear of the backing plate through a thin wall** — EN 1992-1-1 §6.4. Governs for walls < ~200 mm.
4. **Bearing of the bolt shank on the concrete at the hole** for the shear component. There is **no code equation** for this; established practice is to assume an effective bearing length of about **3 × bolt diameter** measured from the loaded face. *(Flag this in the report as practice, not code.)*
5. **Global effect on the column** — you have just put a 26 mm hole through a compression member. Check the reduced section, and check that you have not cut a link.

### 6.3 Detailing

```
FRONT (steel side)                          BACK (concrete side)
┌──────────────┐                            ┌──────┐
│  end plate   │══════ through-bolt ════════│ back │
│   25 mm      │  (M20/M24 8.8, full length)│ plate│
│              │                            │ 20mm │
└──────────────┘                            └──────┘
   beam cleat                                square washer plate,
                                             typically 120–200 mm square
```
- **Hole diameter:** drill 4–6 mm oversize for a straight run; for long holes through 400+ mm, oversize more and **grout the annulus** with a cementitious grout or resin after alignment, otherwise the bolt rattles and the connection has slip.
- **Backing plate size:** typically **120 × 120 × 15 mm to 200 × 200 × 25 mm** — size it so the concrete bearing check passes with a comfortable margin and so it distributes over more than one link spacing. Deburr the concrete face and bed the plate on a thin epoxy mortar to guarantee full contact (a rocking backing plate is a hinge).
- **Corrosion/appearance:** the back plate is visible. In industrial buildings this is normally accepted; in architectural exposure it is not.
- **Preload:** through-bolts can be genuinely preloaded (unlike most post-installed anchors), which gives a stiff, slip-free connection and mobilises friction. But relaxation into the concrete over time (creep of concrete under the washer) means preload decays — **do not rely on preload for the ULS shear path** unless you can re-torque.
- **Drilling:** must be done from one side with a rig; a hand-held drill will wander and exit the far face in the wrong place. For a 400 mm column, exit-point error of 10–20 mm is normal without a rig.

---

## 7. CAST-IN SOLUTIONS FOR NEW CONCRETE — ALWAYS THE BETTER ANSWER

**Design rule to put in the report in bold:** *if the concrete is not yet cast, never specify a post-installed anchor.* A cast-in plate with headed studs, placed in the formwork, avoids every problem in §4 and §5 and costs less.

### 7.1 Cast-in plate with headed studs (Nelson studs) — the most common industrial detail

**Geometry — EN ISO 13918:2008 Table 10, type SD (shear connector):**

| d1 (shank ø) | 10 | 13 | 16 | 19 | 22 | 25 |
|---|---|---|---|---|---|---|
| d5 head diameter [mm] | 19 | 25 | 32 *(may be 29 for shear)* | 32 | 35 | 41 |
| h1 head thickness [mm] | 7 | 8 | 8 | **10** | 10 | 12 |
| d3 weld collar ø [mm] | 13 | 17 | 21 | 23 | 29 | 31 |
| l1 − l2 (burn-off) [mm] | +3 | +4 | +4.5 | **+5** | +5 | +5.5 |

**⚠ Fabrication point:** `l1` is the stud length *before* welding, `l2` is the length *after* welding. **For a ø19 stud you lose 5 mm in the arc.** If the drawing calls for h_ef = 170 mm, order studs 175 mm long. Every stud schedule must state which length it means. Tolerance on l2 is +1/−2 mm.

**Material (EN ISO 13918 / Peikko WELDA):**
- **SD1** (plain steel, the standard shear connector): **f_yk ≥ 350 N/mm², f_uk ≥ 450 N/mm², A5 ≥ 15 %**
- **SD3** (stainless): f_p0.2 ≥ 350, f_uk ≥ 500, A5 ≥ 25 %
- Plate: S355J2+N to EN 10025-2 (stainless: 1.4301 / 1.4401 to EN 10088-2)

**Design — two different models, and you must not mix them:**

**(a) As an ANCHOR (transferring load out of the steel into the concrete) → EN 1992-4.** Use §2.2/§2.3 above with `k1 = 8.9 (cracked) / 12.7 (uncracked)`, plus the head pull-out check `N_Rk,p = k2·A_h·f_ck`. **This is the correct model for a bracket on a column face.**

**(b) As a COMPOSITE SHEAR CONNECTOR (in a composite beam/slab) → EN 1994-1-1 §6.6.3.1:**
```
P_Rd = min {  0.8·f_u·(π d²/4) / γ_V        (stud steel shear)
              0.29·α·d²·√(f_ck·E_cm) / γ_V  (concrete crushing around the shank) }
  α = 0.2·(h_sc/d + 1)  for 3 ≤ h_sc/d ≤ 4
  α = 1.0               for h_sc/d > 4
  γ_V = 1.25
  E_cm = 22·(f_cm/10)^0.3  [GPa],  f_cm = f_ck + 8
```
Detailing rules, EN 1994-1-1 §6.6.5.5 / §6.6.5.1:
- nominal stud height **h_sc ≥ 3·d**
- head diameter **≥ 1.5·d**, head depth **≥ 0.4·d** (SD studs comply automatically)
- spacing in the direction of shear **≥ 5·d**; transverse **≥ 2.5·d** in solid slabs, **≥ 4·d** otherwise
- clear distance from stud to flange edge **≥ 20 mm**

*Worked value:* ø19 stud, C30/37 (E_cm = 33 GPa), f_u = 450 MPa, h_sc = 100 mm:
```
steel:    0.8 × 450 × 283.5 / 1.25 = 81.6 kN
concrete: 0.29 × 1.0 × 19² × √(30 × 33 000) / 1.25 = 83.3 kN
P_Rd = 81.6 kN per stud
```
(Published UK table value for 19 × 100 in C30/37: **79.3 kN** — the small difference is the assumed f_u and E_cm. Quote both, note the source.)

**EN 1992-4 detailing for a cast-in stud anchor plate — real minimums (Peikko WELDA, per ETA-16/0430):**

| Stud nominal size d [mm] | 10 | 13 | 16 | 19 | 22 | 25 |
|---|---|---|---|---|---|---|
| min h_ef [mm] | 50 | 50 | 50 | 75 | 75 | 75 |
| **s_min** [mm] | 50 | 50 | 50 | 70 | 70 | 70 |
| **c_min** [mm] | 50 | 50 | 50 | 70 | 70 | 70 |
| **h_min** (member) | h_ef + t_h + c_nom | | | | | |

**Representative capacities — Peikko WELDA Anchor Plates, Table 6 (single action only, no supplementary reinforcement, plate remote from edges):**

| Plate B×L−H [mm] | Tension +N_Rd [kN] | Shear V_Rd [kN] |
|---|---|---|
| WELDA 100×100−68 | 16.5 | 29.2 |
| WELDA 100×100−108 | 38.2 | 47.7 |
| WELDA 150×150−70 | 21.7 | 42.4 |
| WELDA 150×150−110 | 45.8 | 52.8 |
| WELDA 150×150−162 | 74.5 | 90.6 |
| WELDA 200×200−72 | 27.2 | 55.8 |
| WELDA 200×200−112 | 53.4 | 95.5 |
| WELDA 200×200−162 | 82.8 | 143.2 |
| WELDA 250×250−165 | 99.6 | 150.2 |
| WELDA 300×300−165 | 102.8 | 151.1 |
| WELDA Strong 150×150−220 | 115 | 142 |
| WELDA Strong 150×250−220 | 229 | 227 |

*(H = overall depth including studs. Tension governed by concrete cone; shear values assume the plate is far from an edge. ⚠ These were read from a text extraction of the manual — verify against the current Peikko Technical Manual before quoting in a signed document.)*

**The key message from this table:** a 300×300 plate is worth only **103 kN in tension** without supplementary reinforcement — because the cone governs. Peikko's own note: *"The tensile and bending resistances… are calculated assuming that the tensile and bending capacity is limited by concrete cone failure. The tensile and bending resistances can be further increased using supplementary reinforcement designed and detailed to prevent concrete cone failure."* And: *"The shear resistances are calculated assuming that the plate is far away from the edge. In practice, close edge distances can limit the resistances."*

Tolerance allowance built into those figures: **eccentricity of 10 % of the plate side length, max 20 mm.** Larger installation eccentricity must be designed for explicitly. That is the realistic accuracy of a cast-in plate — **±20 mm** — and it must be reflected in the steelwork's slotted holes.

### 7.2 Stud connectors with threaded sockets (EOTA TR 081 / EAD 160202-00-0301)

A refinement that solves the biggest practical problem with cast-in plates — **the plate has to be flat, flush and in the right place**. Instead, cast in *rebar with a forged rectangular anchor head at one end and a threaded socket at the other*, and bolt the steel end plate on afterwards.

- Anchor head bearing area ≈ **8 × the stress cross-section of the bar** (TR 081 §1.4.1)
- Concrete C20/25 to C70/85
- Full design method for a steel member on an RC column: TR 081 §2.2.5, formulae (17)–(42), summarised in §1.4 above
- Local blow-out at the socket: `V_Rd,c,local = k8·d_socket²·(f_ck·R_p0.2)^0.5 / γ_c` [Eq. (33)]
- Edge failure: `V_Rd,c,bcj = k9·l_socket·b_plate·(f_ck)^0.25·α_cc/γ_c` [Eq. (36)]
- Splitting reinforcement, primary and secondary [Eqs. (37)–(42)]:
```
Z_Ed,sp1 = (V_Ed/(4·n_sc))·(1 − d_socket/max p2)          primary, directly under each connector layer
A_sw,sp1 = Z_Ed,sp1/f_ywd
Z_Ed,sp2,T = 0.25·V_Ed·(1 − Σp2,i/b_col)                  secondary, tension chord
Z_Ed,sp2,C = 0.25·V_Ed,c·(1 − Σp2,i/b_col)                secondary, compression chord
   distributed over h_sp = (2/3)·b_plate from each connector layer
   b_col limited to ≤ 3·p2,i
```
- Mandatory column detailing (§2.2.5.2): a longitudinal bar at **each corner on the near face**, additional longitudinal bars **between the sockets**, and **stepped column ties at s ≤ 100 mm, reducing to s ≤ 50 mm** below each connector layer.
- Positioning plates (mounting templates) are **not** counted in the structural analysis; provide a **ø4 mm vent hole** in the middle of the positioning plate if ventilation is required for concreting.
- Corrosion: temporary protection by Sendzimir galvanizing; permanent by hot-dip to **EN ISO 1461**.

### 7.3 Cast-in anchor channels

Halfen HTA / Hilti HAC type. A rolled or cold-formed channel with anchors welded to the back, cast flush in the face. Design to **EN 1992-4 §7.4** with **EOTA TR 047**.

**Advantages for column work:**
- **infinite adjustability along the channel** — kills the ±20 mm placement problem entirely;
- the V-shaped anchor forms in modern channels are specifically developed so that *"loads can be taken close to slab edges where shear loads occur"* (Hilti HAC literature) — i.e. they are engineered for the edge problem;
- no drilling, no rebar strikes.

**Disadvantages:** foam filler strips must be cleaned out; the channel is a line of weakness for spalling if over-loaded transversely; channel bolts must be the manufacturer's own (a normal bolt in a channel is an uncontrolled product).

### 7.4 Weld plates / anchor plates with rebar tails

The low-tech version: a plate with **deformed reinforcing bar anchors** welded to the back (deformed bar anchors, DBA) instead of headed studs. These transfer by **bond over the bar length + hook**, not by head bearing, so they must be checked for bond/anchorage per EN 1992-1-1 §8.4, not by the head pull-out equation. Cheaper, but longer (needs a full anchorage length l_bd, typically 40–50ø), so only viable in deep members.

---

## 8. BRACKETS AND CORBELS SUPPORTING STEEL FROM CONCRETE

Where the vertical load is large (> ~150–200 kN), stop trying to hang it on anchors and **support it in bearing**.

### 8.1 Concrete corbel — EN 1992-1-1 §6.5.3 + Annex J.3

**Classification by shear span a_c vs corbel depth h_c:**

| a_c / h_c | Design method |
|---|---|
| a_c < 0.4·h_c | Special strut-and-tie models |
| **0.4·h_c < a_c < h_c** | **Simple strut-and-tie model** (the normal case) |
| a_c > h_c | Design as a cantilever beam |

**Mandatory horizontal force:** *"the corbel should be designed for the vertical force F_v and a horizontal force H_c ≥ 0.2·F_v acting at the bearing area"* — this accounts for bearing friction, shrinkage and thermal movement of the supported steel beam. **This is the clause fabricators most often forget, and it is why a corbel needs proper welded/looped main tie reinforcement, not a bent-up bar.**

**Detailing:**
- Main tension tie A_s,main at the top, **fully anchored beyond the node** — loops or welded transverse bars, never a plain straight bar
- **Closed horizontal links, A_s,link ≥ 0.5·A_s,main**, to confine the compression strut
- **A_s,link ≥ A_s,main when a_c < 0.5·h_c**
- If **a_c > 0.5·h_c**, closed **vertical** links are required instead/in addition

### 8.2 Proprietary steel corbels (Peikko PCs and equivalents)

A machined column plate is cast into the column; the corbel plate bolts onto it after striking. Used to support **steel beams, composite steel-concrete beams, RC beams and walls**.

**Minimum column/wall sizes — Peikko PCs Corbel Technical Manual, Table 1 (corbel in the middle of the column):**

| Model | H_min / b_min [mm] | d_min [mm] |
|---|---|---|
| PCs 2 | 280 / 280 | 280 |
| PCs 3 | 280 / 280 | 280 |
| PCs 5 | 280 / 280 | 280 |
| PCs 7 | 380 / 380 | 380 |
| PCs 10 | 380 / 380 | 380 |
| PCs 15 | 380 / 450 | 450 |

Additional key points from that manual:
- **Standard concrete class is C30/37.** For lower classes, reduce the resistance: **C25/30 → ×0.90; C20/25 → ×0.79 (PCs 2–PCs 10) or ×0.67 (PCs 15)**
- *"Horizontal headed stud bars on the single-sided column part create a risk of concrete cone failure, which must be tied to the column with supplementary reinforcement"* — i.e. **the corbel system does not work without its dedicated supplementary reinforcement**, which is in addition to the column's own reinforcement
- If the column part is **not** in the middle of the column, the minimum edge distance of the column plate is **b_min/2**
- The eccentric reaction generates a bending moment in the column that the project engineer must design for

### 8.3 Steel bracket welded to a cast-in plate

The pragmatic industrial detail: cast in a **300 × 400 × 25 mm plate with 6–8 ø19 studs**, then site-weld a fabricated bracket (two side plates + a seat plate + a top gusset) to it. The vertical reaction is transferred to the plate mainly by the *bottom* row of studs in shear plus bearing of the plate edge, and the moment by the tension chord. Design to EN 1992-4 (§2) and use the TR 081 chord model (§1.4).

**Fabrication note:** site-welding to a cast-in plate is only sound if (a) the plate is flush and clean of laitance, (b) the plate is thick enough not to distort (≥ 20 mm), and (c) the weld heat cannot cook the concrete behind. For plates ≤ 15 mm with studs directly behind the weld, restrict heat input and use multiple small passes.

---

## 9. CHEMICAL / BONDED ANCHORS: ORIENTATION, CLEANING, CURING, AND THE SUSTAINED-LOAD PROBLEM

### 9.1 Why this section exists — the Big Dig, 10 July 2006

**The facts:**
- Fort Point Channel Tunnel, Boston. A suspended concrete ceiling panel of **26 short tons (52,000 lb; ~24,000 kg)**, measuring **20 ft × 40 ft (6.1 × 12.2 m)**, fell onto a car. **One fatality** (Milena Del Valle), one injury.
- The panels were hung from **adhesive (epoxy) anchors installed overhead** into the tunnel roof.
- **NTSB probable cause: the use of an epoxy anchor adhesive with poor creep resistance** — specifically Powers Fast Set epoxy, a formulation not capable of sustaining long-term loads. NTSB summarised it as **"epoxy creep."**
- Contributory: **half as many bolts were used as the original design called for**; the bolts were too short; **there was no redundancy** in the hanger system; and there was no timely tunnel inspection programme.
- Warning signs were ignored: anchor displacements were observed as early as **October 1999**, and there was a **partial ceiling collapse on 17 December 2001**. A **1998** Inspector-General report had already flagged the bolt-and-epoxy system in the Ted Williams Tunnel.
- After the collapse, **242 potentially dangerous bolt fixtures** were identified. Repair/inspection cost **$54 million**; the Bechtel/Parsons Brinckerhoff settlement was **$405 million**.

**The mechanism, explained for the fabricator:** a polymer adhesive is not steel. Under a *constant* load it does not simply hold — it flows, very slowly (primary creep), then at a roughly constant rate (secondary creep), and then, if the stress is high enough, it accelerates and fails (tertiary creep). At room temperature and low stress this takes centuries; at elevated temperature and high stress it takes months. **The tunnel ceiling was a permanent dead load, applied in the worst orientation, in a warm, damp tunnel. Every factor was against the adhesive.**

### 9.2 The qualification and design response

**Europe (EN 1992-4 + EAD 330499-01-0601):**
- EN 1992-4 §7.2.1.6(2), Formula (7.14) introduced the **product-dependent factor ψ_sus** on the bond strength, to take account of the influence of sustained load. This is one of the headline changes from ETAG 001 Annex C to EN 1992-4 (listed explicitly in the DIN EN 1992-4 foreword).
- ψ_sus depends on a **basic value ψ⁰_sus given in the ETA** and on **α_sus = (sustained action)/(total action at ULS)**.
- The ETA value is established by **sustained-load creep test series B14 and B15** under EAD 330499: confined tests in uncracked **C20/25**, at normal ambient temperature *and* at the maximum long-term temperature, on **M12 or the smallest size**, with extrapolated-displacement and residual-load criteria.
- **Real number:** Hilti HIT-HY 200 → **ψ_sus = 0.74** (a 26 % reduction on bond for permanently sustained tension).

**USA (ACI 355.4 + ACI 318):**
- Adhesive anchors must be qualified to **ACI 355.4** (or ICC-ES **AC308**), which includes a sustained-load creep test programme. Method: a **stress-vs-time-to-failure (SvTTF)** semi-log plot, "failure" defined as **the onset of tertiary creep**, with the trend line extrapolated to a **100-year design life**.
- **ACI 318 reduced the bond strength factor for sustained tension from 0.75 to 0.55** — i.e. an adhesive anchor resisting sustained tension may only use **55 %** of its characteristic bond strength.
- **ACI 318 §26.7.1(l): adhesive anchors resisting sustained loads must be installed by a person certified under the ACI/CRSI Adhesive Anchor Installation Certification (AAIC) programme.** The exam includes a written paper **and a practical test — blindly installing an adhesive anchor overhead into an inverted test tube which is then cut in half and graded for voids.**
- **Continuous special inspection** is required for horizontal and upwardly-inclined adhesive anchors in sustained tension.
- Some jurisdictions go further: California hospitals and schools (2022 CBC Table 1705A.3 footnote 3, referencing ACI 318-19 §26.7.2) require **all horizontal and overhead adhesive anchors — irrespective of load condition — to be installed by a Certified Adhesive Anchor Installer (CAAI).**
- **Concrete must be aged ≥ 21 days** before adhesive anchor installation.

### 9.3 Installation direction — what changes and why

| Orientation | Difficulty | Failure mechanism if botched |
|---|---|---|
| **Downward (into a slab)** | Easy. Gravity holds the resin in the hole. Dust falls to the bottom — must be blown/brushed out | Dust plug at the base of the hole → reduced h_ef |
| **Horizontal (into a column/wall face)** | Moderate. Resin runs out; the rod must be supported until gel. **Air voids form along the top of the hole** as the rod is pushed in | Reduced effective bond area, always on the same side |
| **Upwardly inclined / overhead** | **Hardest.** Resin runs out under gravity; the rod must be **physically wedged** until cure; voids are invisible | Progressive bond loss + creep → the Big Dig |

**Rules for the method statement:**
1. **Inject from the bottom of the hole outward**, withdrawing the nozzle so that resin displaces air rather than trapping it. Use a **piston plug** and **extension tube** for deep and overhead holes — this is not optional for overhead work.
2. **Fill 2/3 of the hole depth**, then insert the rod with a slow twisting motion so the resin extrudes back out of the mouth of the hole. **Resin must visibly extrude — if it does not, the hole was under-filled and the anchor must be removed.**
3. **Wedge the rod** (plastic wedges/clips) for horizontal and overhead installation until t_cure has fully elapsed. Do not touch/load the anchor after gel time.
4. **Cleaning is the number-one cause of adhesive anchor failure.** For hammer-drilled holes the standard sequence is **blow – brush – blow (×2 each)** with the correct brush diameter and oil-free compressed air, or use **hollow drill bits with dust extraction** (which qualifies as a cleaning method in the ETA). *Bond strengths in the ETA are conditional on the specified cleaning method — a different method means different (lower) values, or none at all.*
5. **Water in the hole:** dry / wet / water-filled / submerged are separate qualification cases in the ETA with separate τ_Rk values. Do not assume the dry-hole value.
6. **Observe t_work and t_cure vs base-material temperature** (table in §4.4). Loading before t_cure is a common site failure.
7. **Sustained-load design:** apply ψ_sus (EN) or the 0.55 factor (ACI). If the connection is *permanently* loaded in tension and *overhead or horizontal*, seriously consider a **mechanical undercut anchor, a cast-in solution, or a through-bolt instead**.

**The one-line rule for the report:** *A bonded anchor in permanent tension, installed horizontally or overhead, is the exact load case that killed someone on the Big Dig. Either eliminate the sustained tension, or use a mechanical/cast-in/through-bolted alternative, or accept the code's ~26–45 % penalty and certified-installer + continuous-inspection regime.*

---

## 10. TYPICAL ANCHOR SIZES AND EMBEDMENTS — INDUSTRIAL BUILDINGS

Consolidated from the Hilti HAS-U/HIT-HY 200 setting table, Peikko WELDA installation parameters, AISC DG1 and normal practice. **Use as a starting point for a first pass, then verify every number by calculation.**

| Application | Typical anchors | h_ef | Notes |
|---|---|---|---|
| Light bracket / pipe support on a column face (< 15 kN) | 2–4 × **M12** chemical | 70–110 mm | h_ef,min = 70, c_min = 45, s_min = 60 |
| Secondary steel beam onto a column (30–80 kN reaction) | 4 × **M16** or 4 × **M20** chemical | 125–170 mm | M16: c_min 50, s_min 75. M20: c_min 55, s_min 90 |
| Main beam / bracket (80–200 kN) | 6 × **M20** or 4–6 × **M24** chemical, or cast-in plate | 170–240 mm | M24: d0 = 28 mm, c_min 60, s_min 115 |
| Heavy bracket / crane runway corbel (> 200 kN) | **Cast-in plate + ø19–ø25 studs**, or through-bolts, or a proprietary corbel | studs 150–250 mm | Do not use post-installed anchors here if avoidable |
| Cast-in plate, general industrial | **ø16 or ø19 SD studs**, 6–8 no., plate 20–25 mm | 100–200 mm | s_min = c_min = 50 mm (ø16) / 70 mm (ø19) |
| Cast-in plate, heavy | **ø22–ø25 SD studs** or WELDA Strong with B500B bars | 150–250 mm | |
| Column base plate (for reference) | **4–8 × M20–M36** cast-in, F1554 Gr 36 / 8.8 | 12–15 d (typical), range 12–25 d | AISC DG1: use **headed rods or threaded rods with a nut**, not hooked rods — *"hooked rods have a very limited pullout strength"* |

**Rules of thumb worth stating:**
- **h_ef ≈ 8–12 × d** for post-installed chemical anchors is the normal working range (M20 → 160–240 mm). The ETA range is much wider (M20: 90–400 mm) but the extremes are inefficient.
- **h_ef ≤ column depth − 100 mm.**
- **c ≥ 1.5·h_ef** to get the full cone — usually impossible in a column, which is exactly the point of §5.
- **Minimum member thickness h_min = h_ef + 30 mm ≥ 100 mm** (small sizes) or **h_ef + 2·d0** (large sizes) for chemical anchors; `h_ef + t_h + c_nom` for cast-in studs.
- Anchors ≥ **M20** need genuine drilling equipment and a drilling rig on a vertical face. Above **M24**, cast-in or through-bolt is almost always cheaper.

---

## 11. WORKED EXAMPLES

### EXAMPLE A — Post-installed anchor plate on an EXISTING concrete column, and how the column width decides everything

**Given:**
- Existing RC column **400 × 400 mm**, **C25/30**, **assumed cracked** (it is a bending column)
- Steel beam HEA 200 framing into the face, reaction **V_Ed = 80 kN** (vertical), applied at **e = 100 mm** from the plate face → **M_Ed = 8.0 kNm**
- End plate **300 wide × 400 high × 20 mm**, S355
- **4 × M20 HAS-U 8.8** with **HIT-HY 200-R**, **h_ef = 170 mm**
- Layout: horizontal spacing **s2 = 180 mm** → **c2 = (400 − 180)/2 = 110 mm** each side. Vertical spacing **s1 = 280 mm**. Top row 60 mm below the plate top.
- γ_c = 1.5, γ_inst = 1.0 → **γ_Mc = 1.5**

**Check 1 — minimums (Hilti setting table, M20):** c_min = 55 ≤ 110 ✓ ; s_min = 90 ≤ 180 ✓ ; h_min = h_ef + 2d0 = 170 + 44 = 214 ≤ 400 ✓ ; h_ef,min 90 ≤ 170 ≤ h_ef,max 400 ✓

**Check 2 — tension on the top anchor row (concrete cone, EN 1992-4 §7.2.1.4):**
```
N⁰_Rk,c = 7.7 × √25 × 170^1.5 = 7.7 × 5 × 2216.5 = 85 335 N = 85.3 kN
c_cr,N = 1.5 × 170 = 255 mm ;  s_cr,N = 510 mm
A⁰_c,N = 510² = 260 100 mm²
A_c,N  = (110 + 180 + 110) × (2 × 255) = 400 × 510 = 204 000 mm²   ← capped by the column width
A_c,N/A⁰_c,N = 0.784
ψ_s,N  = 0.7 + 0.3 × 110/255 = 0.829
ψ_re,N = 0.5 + 170/200 = 1.35 → 1.0
ψ_ec,N = ψ_M,N = 1.0

N_Rk,c = 85.3 × 0.784 × 0.829 = 55.5 kN   (for the 2-anchor row)
N_Rd,c = 55.5/1.5 = 37.0 kN → 18.5 kN per anchor
```
Compare steel: `N_Rd,s = A_s·f_uk/γ_Ms = 245 × 800 / 1.5 = 130.7 kN per anchor`.
**→ Concrete governs by a factor of 7.**

**Demand:** lever arm `d = 400 − 60 = 340 mm`, `z0 = 0.9 × 340 = 306 mm`
```
Z_Ed = M_Ed/z0 = 8.0/0.306 = 26.1 kN  → 13.1 kN per anchor
Utilisation = 13.1/18.5 = 0.71  ✓
```

**Check 3 — concrete edge failure in shear (EN 1992-4 §7.2.2.5):**
The shear is **vertical**, and the two side faces are **vertical** → the load is **parallel** to those edges → **ψ_α,V = 2.0**.
```
c1 = 110 mm ; l_f = min(h_ef, 12·d_nom) = min(170, 240) = 170 mm
α = 0.1 × (170/110)^0.5 = 0.1243     → d_nom^α = 20^0.1243 = 1.451
β = 0.1 × (20/110)^0.2 = 0.0711      → l_f^β  = 170^0.0711 = 1.441
V⁰_Rk,c = 1.7 × 1.451 × 1.441 × √25 × 110^1.5
        = 1.7 × 1.451 × 1.441 × 5 × 1153.7 = 20 505 N = 20.5 kN

A⁰_c,V = 4.5 × 110² = 54 450 mm²
A_c,V  = (1.5c1 + s1 + 1.5c1) × 1.5c1 = (165 + 280 + 165) × 165 = 610 × 165 = 100 650 mm²
A_c,V/A⁰_c,V = 1.849
ψ_s,V = 1.0 (far edge at 290 > 1.5c1) ; ψ_h,V = 1.0 ; ψ_ec,V = 1.0 ; ψ_re,V = 1.0
ψ_α,V = 2.0

V_Rk,c = 20.5 × 1.849 × 2.0 = 75.8 kN
V_Rd,c = 75.8/1.5 = 50.5 kN  per edge (the 2 anchors nearest that face)
```
**Demand:** 80/2 = **40 kN per edge**. Utilisation **0.79** ✓

**Check 4 — steel shear:** `V_Rd,s = 0.6 × 245 × 800 / 1.25 = 94.1 kN per anchor` vs 20 kN — not critical.

**RESULT: the connection works, but with almost no margin, and every governing check is a concrete edge check.**

---

**NOW CHANGE ONE THING: make the column 300 mm wide instead of 400 mm.**

Keeping s2 = 180 mm → **c2 = (300 − 180)/2 = 60 mm** (only just above c_min = 55 mm).
```
Tension:  A_c,N = 300 × 510 = 153 000 → ratio 0.588
          ψ_s,N = 0.7 + 0.3 × 60/255 = 0.771
          N_Rk,c = 85.3 × 0.588 × 0.771 = 38.7 kN → N_Rd,c = 25.8 kN → 12.9 kN/anchor
          Demand 13.1 kN → utilisation 1.02  ✗ MARGINAL FAIL

Shear:    c1 = 60 mm
          α = 0.1(170/60)^0.5 = 0.1683 → 20^0.1683 = 1.656
          β = 0.1(20/60)^0.2  = 0.0803 → 170^0.0803 = 1.511
          V⁰_Rk,c = 1.7 × 1.656 × 1.511 × 5 × 60^1.5 = 1.7×1.656×1.511×5×464.8 = 9 882 N = 9.88 kN
          A_c,V = (180 + 280) × 90 = 41 400 ; A⁰_c,V = 16 200 → ratio 2.556
          V_Rk,c = 9.88 × 2.556 × 2.0 = 50.5 kN → V_Rd,c = 33.7 kN
          Demand 40 kN → utilisation 1.19  ✗ FAIL
```
**A 25 % reduction in column width causes a 33 % loss of edge shear capacity and turns a working connection into a failing one — with identical anchors, identical embedment, identical steel.**

**Fix 1 — pull the anchors inward:** reduce s2 from 180 to **120 mm** → c2 = 90 mm.
```
Shear:  V⁰_Rk,c(c1=90) = 1.7 × 20^0.1374 × 170^0.0740 × 5 × 90^1.5
                       = 1.7 × 1.509 × 1.462 × 5 × 853.8 = 16 015 N = 16.0 kN
        A_c,V = (270 + 280) × 135 = 74 250 ; A⁰_c,V = 36 450 → 2.037
        V_Rk,c = 16.0 × 2.037 × 2.0 = 65.2 kN → V_Rd,c = 43.5 kN > 40 kN  ✓ (util. 0.92)

Tension: A_c,N still capped at 300 × 510 = 153 000 → 0.588
         ψ_s,N = 0.7 + 0.3 × 90/255 = 0.806
         N_Rk,c = 85.3 × 0.588 × 0.806 = 40.4 kN → 13.5 kN/anchor > 13.1 ✓ (util. 0.97)
```
It passes — barely. **Note the counter-intuitive result: moving the anchors CLOSER TOGETHER made the connection stronger,** because the edge distance controls more strongly than the group area.

**Fix 2 — the proper engineering fix: supplementary reinforcement.** Add **2 no. ø12 B500B closed links** around each anchor, hard against the plate, within 0.75 × c1 = 68 mm, lapped 40ø into the column cage. Then the edge check is waived (EN 1992-4 §7.2.2.2(1)) and:
```
N_Rk,re = 4 legs × 113 mm² × 500 = 226 kN → N_Rd,re = 226/1.15 = 196 kN >> 40 kN  ✓✓
```
The connection is now limited by the **steel**, which is where you want it. **But this requires chasing out cover and drilling for the links — which in an existing column is expensive and risky.** In practice, for an existing 300 mm column carrying 80 kN, **through-bolting (Example C) is the better answer.**

---

### EXAMPLE B — Cast-in plate with headed studs on a NEW column

**Given:**
- New RC column **400 × 400 mm**, **C30/37**, cracked assumed
- Cast-in plate **300 wide × 350 high × 20 mm S355**, with **6 × ø19 SD1 headed studs** (EN ISO 13918), h_ef = 170 mm
- Layout: 2 columns of studs at **s2 = 200 mm** → **c2 = 100 mm**; 3 rows at **s1 = 125 mm**
- Loads: **V_Ed = 150 kN**, **M_Ed = 18 kNm**
- Stud material SD1: f_yk = 350, **f_uk = 450 MPa**, A_s = 283.5 mm²
- ø19 SD stud head: **d5 = 32 mm, h1 = 10 mm** (EN ISO 13918 Table 10)

**Stud steel resistances:**
```
γ_Ms(tension) = 1.2 × 450/350 = 1.543 ≥ 1.4
N_Rd,s = 283.5 × 450 / 1.543 = 82.7 kN per stud
γ_Ms(shear)   = max(1.0 × 450/350 , 1.25) = 1.286
V_Rd,s = 0.6 × 283.5 × 450 / 1.286 = 59.5 kN per stud  →  6 studs = 357 kN
```

**Head pull-out (§7.2.1.5):**
```
A_h = (π/4)(32² − 19²) = (π/4)(1024 − 361) = 520.7 mm²
check d_h ≤ 6·t_h + d :  32 ≤ 6(10) + 19 = 79  ✓
N_Rk,p = 7.5 × 520.7 × 30 = 117 158 N = 117.2 kN  →  N_Rd,p = 78.1 kN per stud
```

**Concrete cone, top (tension) row of 2 studs:**
```
N⁰_Rk,c = 8.9 × √30 × 170^1.5 = 8.9 × 5.477 × 2216.5 = 108 053 N = 108.1 kN   [k1 = 8.9, cast-in headed, cracked]
A_c,N = 400 × 510 = 204 000 ; A⁰_c,N = 260 100  → 0.784
ψ_s,N = 0.7 + 0.3 × 100/255 = 0.818
N_Rk,c = 108.1 × 0.784 × 0.818 = 69.3 kN  →  N_Rd,c = 46.2 kN for 2 studs = 23.1 kN/stud
```
**Demand:** `d = 350 − 50 = 300`, `z0 = 0.9 × 300 = 270 mm`; `Z_Ed = 18/0.27 = 66.7 kN` → **33.3 kN/stud > 23.1 kN ✗ FAILS on concrete cone.**

**Side-face blowout:** not required here because c2 = 100 mm > 0.5·h_ef = 85 mm. *But if s2 were increased to 250 mm (c2 = 75 mm < 85), it would be:*
```
N⁰_Rk,cb = 8.7 × 75 × √520.7 × √30 = 8.7 × 75 × 22.82 × 5.477 = 81 555 N = 81.6 kN  (then × ψ_s,Nb ψ_g,Nb ψ_ec,Nb)
```
**→ widening the stud spacing to gain lever arm can trigger a brand-new failure mode. Watch the 0.5·h_ef trigger.**

**Concrete edge in shear (parallel, c1 = 100 mm, 3 studs per side):**
```
α = 0.1(170/100)^0.5 = 0.1304 → 19^0.1304 = 1.468
β = 0.1(19/100)^0.2  = 0.0717 → 170^0.0717 = 1.445
V⁰_Rk,c = 1.7 × 1.468 × 1.445 × √30 × 100^1.5 = 1.7 × 1.468 × 1.445 × 5.477 × 1000 = 19 757 N = 19.8 kN
A_c,V = (150 + 125 + 125 + 150) × 150 = 550 × 150 = 82 500 ; A⁰_c,V = 45 000 → 1.833
ψ_α,V = 2.0
V_Rk,c = 19.8 × 1.833 × 2.0 = 72.4 kN → V_Rd,c = 48.3 kN per side → 96.6 kN total
```
**Demand 150 kN → utilisation 1.55 ✗ FAILS on concrete edge in shear.**

**Summary of Example B:**

| Limit state | Resistance | Demand | Verdict |
|---|---|---|---|
| Stud steel, shear (6 studs) | **357 kN** | 150 kN | ✓ 0.42 |
| Head pull-out (per stud) | 78.1 kN | 33.3 kN | ✓ 0.43 |
| Stud steel, tension (per stud) | 82.7 kN | 33.3 kN | ✓ 0.40 |
| **Concrete cone, tension row** | **23.1 kN/stud** | 33.3 kN | ✗ **1.44** |
| **Concrete edge, shear** | **96.6 kN** | 150 kN | ✗ **1.55** |

**The steel is at 42 % and the concrete is at 155 %.** No amount of bigger studs will fix this.

**The fix — supplementary reinforcement, and it is nearly free because the concrete has not been poured yet:**
- **Tension:** 2 legs of **ø10 B500B** per stud, positioned within 0.75 × h_ef = 128 mm of each stud, anchored past the cone:
  `N_Rk,re = 2 × 78.5 × 500 = 78.5 kN` → `N_Rd,re = 78.5/1.15 = 68.3 kN per stud` — **comfortably above the stud's own steel capacity**, so the failure is now ductile steel yielding. ✓
- **Shear:** closed links **ø12 @ 80 mm** in contact with the stud shafts over the plate height, per EN 1992-4 §7.2.2.2(4), designed by the 45° strut-and-tie model to carry the full 150 kN, plus edge reinforcement along the column arris.
- **Splitting** (TR 081 §2.2.5.13): stepped column ties **at s ≤ 100 mm, reducing to s ≤ 50 mm** directly below each stud layer, over `h_sp = (2/3)·b_plate = 200 mm`.

**And the drawing note that makes it happen:** *"The reinforcement shown around the cast-in plate is structural anchor reinforcement to EN 1992-4 §7.2.2.2. It is additional to the column's own reinforcement and shall not be omitted, relocated or substituted."*

---

### EXAMPLE C — Through-bolting the narrow column from Example A

**Given:** the failing 300 mm wide column, V_Ed = 80 kN, M_Ed = 8 kNm.
**Detail:** front plate 300 × 400 × 25 mm; **4 × M20 8.8 through-bolts**; rear backing plates **150 × 150 × 20 mm** (one per bolt, or a single 150 × 400 strip); holes drilled ø26, annulus grouted.

```
Bolt tension (EN 1993-1-8 Table 3.4):
   F_t,Rd = k2·f_ub·A_s/γ_M2 = 0.9 × 800 × 245 / 1.25 = 141.1 kN per bolt
   Demand: Z_Ed = M_Ed/z0 = 8.0/0.306 = 26.1 kN → 13.1 kN per bolt
   Utilisation 0.09  ✓✓

Concrete bearing under the backing plate (EN 1992-1-1 §6.7):
   A_c0 = 150² − (π/4)(26²) = 22 500 − 531 = 21 969 mm²
   f_cd = α_cc·f_ck/γ_c = 1.0 × 30/1.5 = 20 MPa
   A_c1 limited by the 300 mm column width: A_c1 = 300 × 450 = 135 000 mm²
   √(A_c1/A_c0) = √6.15 = 2.48
   F_Rdu = 21 969 × 20 × 2.48 = 1 090 kN   ≤  3.0 × 20 × 21 969 = 1 318 kN   →  1 090 kN
   Demand 13.1 kN → utilisation 0.012  ✓✓✓

Shear: transferred by bolt bearing on the concrete over ≈ 3d = 60 mm (practice, not code) plus
       friction under the compression chord. 20 kN per bolt over 60 × 20 mm = 16.7 MPa — trivial.
```
**Every concrete tension limit state — cone, blowout, splitting, edge — has disappeared.** The remaining checks are steel checks with utilisations under 0.10. This is why through-bolting is the retrofit engineer's answer to a narrow column, and why the *only* real question is whether the back face is accessible.

**Residual checks that must still be done:** punching of the backing plate through the section (not critical at 300 mm), loss of column section at four ø26 holes, and whether any link has been cut.

---

## 12. FABRICATION AND DETAILING IMPLICATIONS — THE PRACTICAL SUMMARY

### 12.1 For the drawing office
1. **Never dimension an anchor group from the beam centreline alone.** Always dimension **edge distance c** to the concrete face and **spacing s** on the anchor plate drawing, and put them on the *steel* drawing too — the steel fixer needs them as much as the fitter.
2. **Show the assumed concrete grade, cracked/uncracked assumption, h_ef and the anchor product/ETA number on the drawing.** An anchor design is only valid for one specific product; substituting "an equivalent M20 chemical anchor" invalidates the calculation.
3. **Make anchor plates tall, not wide.** Height buys lever arm for free; width burns edge distance.
4. **Slot the holes in the steel, not in the concrete.** Cast-in plates have a real placement tolerance of about **±20 mm** (Peikko's own allowance is 10 % of the plate side, max 20 mm). Detail the connecting cleat with **long-slotted holes** or a site-welded shear tab.
5. **Draw the anchor reinforcement.** If the calculation relies on links around the anchors, they must be on the RC drawing, with a note that they are structural and not to be omitted. Coordinate with the concrete detailer — this is the single most common breakdown between the steel and concrete packages.
6. Specify **headed rods or threaded rods with a nut** for cast-in anchors, never hooked rods (AISC DG1: hooked rods "have a very limited pullout strength"). Plate washers on the rod do **not** increase pull-out and interfere with rebar — AISC DG1 recommends limiting the anchorage device to a heavy hex nut or a forged head, except where blowout governs.
7. State the **stud length before or after welding** unambiguously (EN ISO 13918: ø19 loses **5 mm** in the arc).

### 12.2 For the shop
8. **Anchor plate thickness ≥ 20 mm** for anything structural; the design assumes a rigid plate. Add stiffeners rather than relying on a thin plate.
9. **Stud welding:** ceramic ferrules per EN ISO 13918 §10; the weld collar must be complete around the full circumference. Bend-test sample studs (30° bend, no cracking at the weld) at the start of each shift and after any change of parameters.
10. **Hot-dip galvanizing to EN ISO 1461** for cast-in plates in anything but dry internal (X0) conditions. Peikko's standard WELDA plates are supplied with 40 µm protective paint only and are rated for **X0, 50-year design life** — that is *dry internal only*, which most Israeli industrial buildings are not.
11. Use the **anchor plate itself as the drilling template** for post-installed work — do not mark out from dimensions.

### 12.3 For site
12. **Scan before you drill. Every hole. Ferroscan + GPR.** Record and photograph.
13. **If you hit rebar: stop, and call the EOR.** Do not drill through it, do not move the hole 30 mm and hope.
14. **Clean the hole to the ETA's specified method** — blow/brush/blow ×2, or hollow drill bit with dust extraction. If a different method is used, the bond values are void.
15. **Observe t_work and t_cure against the actual base-material temperature** — not the air temperature, and not yesterday's. On a hot column face the working time can be **4 minutes**.
16. **For horizontal and overhead adhesive anchors: piston plug, inject from the back of the hole, wedge the rod, verify resin extrusion.** Consider requiring a certified installer (ACI/CRSI AAIC) as a contract condition for any sustained-tension adhesive anchor — it is standard practice in the US and is cheap insurance.
17. **Torque to T_max, not "as tight as it goes."** The table in §4.4 gives the maximum torque *specifically to avoid splitting during installation at minimum spacing and edge distance*. Over-torquing an anchor 50 mm from a column arris will crack the column.
18. **Proof-load test** a sample (typically 5–10 % of anchors, to 1.25–1.5 × the service load) on any critical retrofit anchorage, and always where the existing concrete strength was assumed rather than measured.

### 12.4 The decision tree to put in the report

```
Is the concrete already cast?
├─ NO  → CAST-IN. Anchor plate + headed studs, or stud connectors with
│        sockets (TR 081), or an anchor channel if position is uncertain.
│        Add the supplementary reinforcement at the same time — it is free now.
│
└─ YES → Is the load large (> ~150 kN) or permanently in tension?
    ├─ YES → Is the back face accessible?
    │        ├─ YES → THROUGH-BOLT.
    │        └─ NO  → Mechanical undercut anchors + designed anchor
    │                  reinforcement, or a steel collar around the column,
    │                  or a corbel/bearing solution.
    └─ NO  → Do the edge distances work?
             ├─ YES → Post-installed chemical anchors. Check ψ_sus.
             └─ NO  → Move inward / go taller / add anchor reinforcement /
                       through-bolt (in that order of cost).
```

---

## 13. SOURCES ACTUALLY USED

**Codes and technical reports (primary)**
1. *DIN EN 1992-4:2019-04 / EN 1992-4:2018(E) — Eurocode 2, Part 4: Design of fastenings for use in concrete* (preview extracts, clauses 7.2.1.8–7.2.2.6, Formulae 7.28–7.46) — https://www.normsplash.com/Samples/DIN/189696674/DIN-EN-1992-4-2019-en-2.pdf and https://www.normsplash.com/Samples/DIN/189696674/DIN-EN-1992-4-2019-en.pdf
2. *EOTA TR 081:2022-06 — Design methods for verification of load-bearing capacity of stud connectors for anchoring in reinforced concrete members* (§2.2.5 steel member to RC member; Eqs. 17–42) — https://www.eota.eu/sites/default/files/uploads/Technical%20reports/TR081_Design%20methods%20load-bearing%20stud%20connectors%20for%20anchoring_2024-06-21.pdf
3. *EOTA TR 047 — Design of Anchor Channels* — https://www.eota.eu/sites/default/files/uploads/Technical%20reports/EOTA%20TR%20047%20-%20Design%20of%20Anchor%20Channels_2021-05.pdf
4. *EAD 330499-01-0601 (Dec 2018) — Bonded fasteners for use in concrete* — https://files-ask.hilti.com/original/mb/mbngv9rmqh.pdf
5. *ISO 13918:2008 — Welding: studs and ceramic ferrules for arc stud welding* (Table 10, type SD dimensions) — https://stalwart-tech.com/wp-content/uploads/2021/07/ISO_13918.pdf
6. *AISC Steel Design Guide 1, 2nd ed. — Base Plate and Anchor Rod Design* (Tables 2.1–2.3, §2.5–2.8) — http://www.abarsazeha.com/images/ScinteficResources/DesignGuide/AISC%20Design%20Guide%2001%20-%20Base%20Plate%20And%20Anchor%20Rod%20Design%202nd%20Ed.pdf
7. *ת"י 466 חלק 1 — חוקת הבטון*, Standards Institution of Israel — https://www.sii.org.il/he/lobby/standardization/standard-page/?id=8da01f10-839e-48c7-8101-07143787bba0_HE
8. *ת"י 1225 חלק 1.1 — חוקת מבני פלדה* (June 2023 edition, in parallel with the 1998 חלק 1) — https://www.sii.org.il/he/דפי-לובי/כללי/תקינה/דף-תקן/?id=346e84a6-b8c8-4576-ac00-c1137f38eb3c_HE

**Manufacturer technical documents (design values)**
9. *Hilti HIT-HY 200-A/R Injection Mortar — Technical Datasheet (May 2020)*: setting details, c_min/s_min/h_ef ranges, ψ_sus = 0.74, curing/working times, c_cr,sp — https://www.resapol.com/wp-content/uploads/2021/12/Technical-data-sheet-for-Hilti-HIT-HY-200-injectable-mortar-in-concrete-Technical-information-ASSET-DOC-8258686-tds.pdf
10. *Peikko WELDA® and WELDA® Strong Anchor Plates — Technical Manual, rev. 06.1 (09/2024)*: installation parameters Table 1, materials Table 2, dimensions Table 3, resistances Tables 6–8, supplementary reinforcement Annexes A & B — https://media.peikko.com/file/dl/i/WiDMVw/lsF304OpvvHun7-mXUKvfQ/WELDAWELDA_Strong_PEIKKO_GROUP_06.1_Technical_Manual_Web.pdf
11. *Peikko PCs® Corbel — Technical Manual (04/2019)*: minimum column/wall sizes Tables 1–3, concrete-class reduction factors Table 4, supplementary reinforcement Annex A — https://media.peikko.com/file/dl/i/Zsk48g/6yzjn1t-mWO9NuXyaTbLQA/PCsPeikkoGroup003TMAWeb.pdf
12. *Hilti Anchor Channel (HAC) Technical Guide* — https://files-ask.hilti.com/original/j3/j3ykrhb1wx.pdf
13. *Hilti Product Technical Guide §4.1 — Anchor Principles & Design* (working principles, failure modes, long-term behaviour) — https://www.sefindia.org/forum/files/41_anchor_principles_and_design_130_148r021_740.pdf
14. *Hilti — Method for Anchor Design in Ungrouted Stand-Off Connections* (McBride & Rocha) — https://files-ask.hilti.com/original/pu/pu5ebwyxp4.pdf
15. *Hilti — Post-Installed Reinforcing Bar Guide* (drilling methods, hollow drill bits, deviation) — https://www.hilti.in/content/dam/documents/pdf/india/Post-Installed_Rebar_Guide_Technical_information.pdf

**Design-software documentation reproducing code equations (used for cross-checking coefficients)**
16. *IDEA StatiCa — Code-check of anchors (EN)*: complete EN 1992-4 formula set with k1/k2/k5/k8/k9 values, all ψ factors, γ factors — https://www.ideastatica.com/support-center/support-center-knowledge-base/check-of-anchors-according-to-eurocode
17. *IDEA StatiCa — Code-check of anchors (AISC/ACI 318)*: ACI 318 formula set with k_c, A_Nco = 9h_ef², A_Vco = 4.5c_a1², all ψ factors — https://www.ideastatica.com/support-center/design-check-of-anchors-according-to-aisc
18. *CivilWeb — Concrete Edge Failure* and *Supplementary Reinforcement* (EN 1992-4 summaries; note the f_yk discrepancy) — https://civilweb-spreadsheets.com/reinforced-concrete-design/concrete-anchorage-design-spreadsheet/concrete-edge-failure/ and https://civilweb-spreadsheets.com/reinforced-concrete-design/concrete-anchorage-design-spreadsheet/supplementary-reinforcement/

**Research and industry papers**
19. Anderson & Meinheit, *A Review of Headed-Stud Design Criteria in the Sixth Edition of the PCI Design Handbook*, PCI Journal, Jan–Feb 2007 (front-edge/side-edge/corner distance research, 364 shear tests) — https://www.pci.org/PCI_Docs/Publications/PCI%20Journal/2007/Janurary_and_February_2007/A%20Review%20of%20Headed-Stud%20Design%20Criteria%20in%20the%20Sixth%20Edition%20of%20the%20PCI%20Design%20Handbook.pdf
20. *Pryout Capacity of Cast-In Headed Stud Anchors*, PCI Journal, Mar–Apr 2005 — https://www.pci.org/PCI_Docs/Publications/PCI%20Journal/2005/March-April/Pryout%20Capacity%20of%20Cast-in%20Headed%20Stud%20Anchors.pdf
21. Wald, *Column Bases* (CESTRUCO), CTU Prague — EN 1993-1-8 base/anchor plate theory, β_j, effective area — https://people.fsv.cvut.cz/~wald/CESTRUCO/Texts_of_lessons/07-GB_Column_Bases.pdf
22. *Creep rate based time to failure prediction of adhesive anchor systems under sustained load* — https://arxiv.org/pdf/1905.00685

**Big Dig / adhesive anchor sustained load**
23. *Big Dig ceiling collapse* — Wikipedia (dates, panel mass 26 short tons / 20 × 40 ft, NTSB "epoxy creep", 242 fixtures, $54 M / $405 M) — https://en.wikipedia.org/wiki/Big_Dig_ceiling_collapse
24. *"Epoxy Creep" Main Factor in Big Dig Ceiling Panel Collapse*, Design News — https://www.designnews.com/assembly/-epoxy-creep-main-factor-in-big-dig-ceiling-panel-collapse
25. Simpson Strong-Tie SE Blog, *Changes Made to ACI 318 With Respect to Adhesive Anchors in Concrete* (0.55 vs 0.75 bond factor, ACI 318 §26.7.1(l) AAIC certification, 21-day concrete age, 2022 CBC Table 1705A.3) — https://seblog.strongtie.com/2014/08/changes-made-to-aci-318-with-respect-to-adhesives-anchors-in-concrete-what-engineers-need-to-know/

**Practice / scanning**
26. BritCut, *What Is Ferro Scanning?* and *Why Rebar Detection Matters Before Drilling or Coring Concrete* (PS 300 to 200 mm ±3 mm; PS 1000 / GP8000 to 300–700 mm) — https://www.britcut.co.uk/what-is-ferro-scanning-a-complete-guide/ , https://www.britcut.co.uk/why-rebar-detection-matters-before-drilling-or-coring-concrete/
27. GPRS, *GPR Concrete Scanning* — https://www.gp-radar.com/article/how-gpr-concrete-scanning-can-help-to-find-rebar-within-reinforced-concrete-to-prevent-structural-damage-when-saw-cutting-coring-or-drilling
28. steelconstruction.info, *Shear connection in composite bridge beams* and steelcalculator.app, *UK Shear Stud — EN 1994-1-1 Capacity Table P_Rd* — https://steelconstruction.info/Shear_connection_in_composite_bridge_beams , https://steelcalculator.app/reference/uk-shear-stud/

---

## 14. ITEMS TO VERIFY BEFORE THE REPORT IS SIGNED

I could not fully verify the following against a purchased primary source. Flagging them explicitly rather than presenting them as certain:

1. **ψ_re,V values (1.0 / 1.2 / 1.4)** and the exact conditions — EN 1992-4 §7.2.2.5(13). The 2018 edition restricted this factor to cracked concrete; the three numeric values come from secondary sources.
2. **EN 1992-4 Formula (6.6)** — the exact expression for N_Ed,re from V_Ed, and the definition of z and d.
3. **Peikko WELDA Table 6/7/8 numbers** — read from a text extraction of the PDF; the column mapping is self-consistent and monotonic but should be checked against the manual.
4. **τ_Rk bond values** for specific chemical anchors — deliberately omitted; take these from the current ETA for the exact product, concrete class, cracked/uncracked state, temperature range and drilling/cleaning method.
5. **Israeli standards position on anchors** — I found no dedicated Israeli anchor standard. Confirm with the SII whether ת"י 466 or a separate standard addresses fastenings, and confirm the current status of ת"י 1225 חלק 1.1 (June 2023) vs חלק 1 (1998).
6. **The "3d bearing length" rule for through-bolts** — this is established practice reported by practising engineers, not a code provision. Present it as such.