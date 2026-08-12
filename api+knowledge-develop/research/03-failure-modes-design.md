# ANCHORAGE FAILURE MODES AND DESIGN VERIFICATION — TECHNICAL BRIEF
### Raw material for an engineering report (Eretz Barzel). All terms given in English with the exact code symbols.

---

## 0. STANDARDS LANDSCAPE — WHAT SUPERSEDED WHAT

**EN 1992-4:2018, "Eurocode 2 — Design of concrete structures — Part 4: Design of fastenings for use in concrete"** is now *the* European design standard for anchorage into concrete. It was approved by CEN on **9 March 2018**, published **September 2018**, and adopted nationally (e.g. DIN EN 1992-4:2019-04, BS EN 1992-4:2018) in **spring 2019**. The document comprises **133 pages**.

The title page states verbatim that it **"Supersedes CEN/TS 1992-4-1:2009, CEN/TS 1992-4-2:2009, CEN/TS 1992-4-3:2009, CEN/TS 1992-4-4:2009, CEN/TS 1992-4-5:2009"** *(source: DIN EN 1992-4:2019-04 preview, p.1 and EN cover page)*.

Historical chain *(source: Würth/TVZ presentation, J. Buhler; fischer white paper)*:

| Year | Document | Scope |
|---|---|---|
| 1997 | EOTA **ETAG 001 Annex C** | Metal anchors, static |
| 2004 | EOTA **TR 020** | Resistance to fire |
| 2007 | EOTA **TR 029** | Bonded anchors, variable embedment |
| 2009 | **CEN/TS 1992-4** parts 1–5 | Headed, channels, mechanical, bonded |
| 2013 | EOTA **TR 045** | Seismic |
| **2018** | **EN 1992-4** | **All of the above, one document** |
| 2023 | CEN **TR 082** | Bonded fasteners in fire |
| ~2026 | **prEN 1992-4** (2nd-generation EC2) | Distribution to NSBs no later than **30 March 2026** |

**Key structural changes CEN/TS → EN 1992-4** (from the DIN EN 1992-4:2019-04 National Foreword, items a)–v), verbatim list):
- (g) **All verifications moved from cube strength f_ck,cube to cylinder strength f_ck**; the k_i factors were adapted accordingly.
- (e) Partial factors for **accidental design situations ~15 % smaller** than permanent/transient (new Table 4.1 columns).
- (h) New factor **ψ_M,N** (favourable compression between fixture and concrete under bending).
- (i) New factor **ψ_sus** for sustained load / creep on bonded fasteners (Formula 7.14).
- (j) **ψ_re,V limited to cracked concrete** (was also allowed for uncracked in CEN/TS).
- (o) **Interaction rules restructured**: steel and non-steel failure verified *separately*.
- (n) Anchor-channel edge failure now uses c₁^(4/3) instead of c₁^1.5.
- (p) Fatigue resistances for concrete failure modes **reduced**.
- (r) New fire annex (Annex D); (v) plastic design moved out to CEN/TR 17081.

**Scope extension:** EN 1992-4 covers concrete classes **C12/15 to C90/105** (ETAG 001 covered C20/25–C50/60); in practice most ETAs are still limited to **C20/25–C50/60** *(fischer white paper, p.9)*. Groups up to **9 fasteners** are now permitted for fastenings without hole clearance, for all edge distances and load directions *(fischer white paper, p.10)*.

**Israel (ת"י):** There is no dedicated Israeli anchor-design standard. ת"י 466 (חוקת הבטון, parts 1–5) is the concrete code; ת"י 1225 covers fire. Israeli practice for anchorage to concrete follows **EN 1992-4 + ETA-approved products** (which is what Hilti / fischer / Würth / Peikko supply into Israel). *I could not verify from a primary SII source in this session whether SII has formally adopted EN 1992-4 — this must be confirmed with מכון התקנים before the report states it as fact.*

---

## 1. THE ENGINEERING PRINCIPLES — THE "WHY", FOR A FABRICATOR

### 1.1 An anchor does not fail where you think it fails
A fabricator's instinct is that "the bolt breaks". In reality **steel failure is usually the *least* likely mode**. In the worked examples below, the M24 anchor group has 753 kN of steel capacity but only **300 kN** of concrete-cone capacity. **The concrete decides the connection, not the bolt.** This is the single most important message for the shop.

### 1.2 The Concrete Capacity (CC / CCD) method
The whole framework is the **Concrete Capacity Design method**, published by CEB in 1995, based on **519 test series** evaluated by **Fuchs / Eligehausen / Breen (1995)** *(Würth/TVZ presentation, p.21)*. Its central experimental result:

```
Mean failure load:      N⁰u,c   = 13.5 · sqrt(f_cc,200) · hef^1.5
5 %-fractile:           N⁰5%    = k    · sqrt(f_cc,200) · hef^1.5
```

Three consequences a fabricator must internalise:

1. **The exponent is 1.5, not 1.0.** Cone capacity grows with hef^1.5. Doubling embedment gives **2.83×** the capacity — far more effective than upgrading the bolt grade.
2. **Cone capacity does not depend on anchor diameter or anchor type.** *(Würth/TVZ, p.21 — verbatim: "The characteristic resistance for the concrete cone failure does not depend on anchor diameter and anchor type!")* An M30 anchor at 100 mm embedment breaks out exactly the same cone as an M12 at 100 mm. Going up a bolt size buys you *nothing* against cone failure.
3. **The failure body is a cone at ≈35° from the concrete surface**, idealised in the code as a **pyramid** with a square base of side 3·hef, projected onto the concrete surface. Two anchors closer than 3·hef *share* one cone.

### 1.3 The projected-area idea (the core of the whole method)
The reference area of one undisturbed anchor is:

```
A⁰c,N = scr,N · scr,N = (3·hef)² = 9·hef²
```

The actual area A_c,N is the projected area of the *real* group, truncated by edges and by overlaps with neighbours. The group capacity is then scaled by **A_c,N / A⁰c,N**. Because overlapping cones share concrete, **n anchors never give n × the single-anchor capacity unless s ≥ 3·hef in both directions.**

### 1.4 Cracked concrete is the default
Concrete in a real structure cracks — EN 1992-1-1 §7.3 explicitly permits crack widths up to **w_max = 0.3 mm**. EN 1992-4 §4.7(1) NOTE: *"In general, it is conservative to assume that the concrete is cracked over its service life."* A crack through the anchor zone removes tensile capacity and drops the cone resistance by ≈ **30 %**.

### 1.5 The rigid-fixture assumption
EN 1992-4 §6.1(5) and §6.2.1(1)–(2): anchor forces may be found by **elastic analysis with a linear strain distribution** only if the base plate **remains elastic (σ_Ed ≤ σ_Rd) and its deformation is negligible compared with the axial displacement of the fasteners** *(Würth/TVZ, p.11)*. If the base plate flexes, load redistributes and the "most loaded anchor" is not the one you calculated. Hilti's own PROFIS reports carry the warning that the software **does not check** whether the rigid-plate assumption is valid.

---

## 2. DESIGN FORMAT AND PARTIAL FACTORS

### 2.1 Format
```
ULS:   E_d ≤ R_d                            (EN 1992-4 Formula 4.1)
       E_d ≤ R_k / γ_M                      (Formula 4.3)
SLS:   E_d ≤ C_d
```
Every load direction × every failure mode must be verified separately (EN 1992-4 §4.2.2). Design working life assumed ≥ **50 years**; reliability class **RC2**, β = 3.8 for a 50-year reference period *(CEN/TS 1992-4-1 §4.1.2 NOTE)*.

### 2.2 Table 4.1 — partial factors for resistance (EN 1992-4 recommended values)

*Primary source for the numbers below: Würth "Design Principles" manual Table 2, which reproduces EN 1992-4 Table 4.1; cross-checked against the Danish National Annex DS/EN 1992-4 DK NA:2024 Table 4.1 DK NA and against CEN/TS 1992-4-1 §4.4.3.*

| Failure mode | Symbol | Permanent & transient | Accidental |
|---|---|---|---|
| **Steel, fastener, TENSION** | γ_Ms | **= 1.2 · f_uk/f_yk ≥ 1.4** | = 1.05 · f_uk/f_yk ≥ 1.25 |
| **Steel, fastener, SHEAR** (with & without lever arm) | γ_Ms | **= 1.0 · f_uk/f_yk ≥ 1.25** when f_uk ≤ 800 N/mm² and f_yk/f_uk ≤ 0.8 | same form |
| | | **= 1.5** when f_uk > 800 N/mm² or f_yk/f_uk > 0.8 | = 1.3 |
| **Steel, supplementary reinforcement** | γ_Ms,re | **= 1.15** (= γ_s of EN 1992-1-1) | = 1.0 |
| **Concrete cone / edge / blow-out / pry-out** | γ_Mc | **= γ_c · γ_inst** | = γ_c · γ_inst |
| | γ_c | **= 1.5** | **= 1.2** |
| | γ_inst | ≥ 1.0 post-installed tension (from ETA); = 1.0 post-installed shear; = 1.0 cast-in (all directions) | same |
| **Concrete splitting** | γ_Msp | **= γ_Mc** | = γ_Mc |
| **Pull-out & combined pull-out/cone (bond)** | γ_Mp | **= γ_Mc** | = γ_Mc |
| Anchor-channel: anchor-to-channel connection | γ_Ms,ca | = 1.8 (CEN/TS) / 2.0 (DK NA) | 1.75 (DK NA) |
| Anchor-channel: local lip bending | γ_Ms,l | = 1.8 (CEN/TS) / 2.0 (DK NA) | 1.75 (DK NA) |
| Anchor-channel: channel bending | γ_Ms,flex | = 1.15 (CEN/TS) / 1.25 (DK NA) | 1.00 (DK NA) |
| Fatigue, steel | γ_Ms,fat | = 1.35 (CEN/TS recommended) | — |
| SLS | γ_M | = 1.0 | — |
| Fire | γ_M,fi | = 1.0 in absence of national regulations *(Hilti HSA datasheet)* | — |

**γ_inst tiers** *(CEN/TS 1992-4-1 §4.4.3.1.2, "given for information"; carried into EN 1992-4 via the ETA)*:

| γ_inst | Installation safety of system | Resulting γ_Mc (with γ_c = 1.5) |
|---|---|---|
| **1.0** | High (all cast-in; best post-installed) | **1.5** |
| **1.2** | Normal | **1.8** |
| **1.4** | Low but still acceptable | **2.1** |

*(the 1.5 / 1.8 / 2.1 table is given explicitly in the Würth/TVZ presentation, p.26)*

**Worked partial-factor examples (real values):**
- Grade 5.8 rod (f_uk = 500, f_yk = 400): tension γ_Ms = 1.2 × 1.25 = **1.5**; shear γ_Ms = 1.0 × 1.25 = **1.25**. Both values appear literally in the Hilti PROFIS report (γ_M,s = 1.500 tension, 1.250 shear).
- Grade 8.8 (f_uk = 800, f_yk = 640): tension γ_Ms = 1.2 × 1.25 = **1.5**; shear = 1.25.
- Grade 10.9 (f_uk = 1000 > 800): shear γ_Ms = **1.5**.
- Peikko HPM® L cast-in rebar anchor bolts, ETA-02/0006: **γ_Ms = 1.4** (tension steel), **γ_Ms = 1.5** (shear steel), **γ_Mp = γ_Mc = γ_Mcp = 1.5**.

**National variation is real.** The **UK NA to BS EN 1992-4:2018** adopts the recommended values throughout (4.4.1(2), 4.4.2.2(2), 4.4.2.3, 4.4.2.4, 4.7(2), C.2(2), D.2(2) — all "recommended values should be used"). The **Danish NA (2024)** deviates: γ_Ms tension ≥ **1.5** (not 1.4), γ_Ms shear ≥ **1.35** (not 1.25) / 1.65 for high-strength, γ_Ms,re = **1.20**, γ_c = **1.45**, and γ_c = 1.00 for accidental/seismic. *The report should state which NA is being applied.*

### 2.3 Partial factors for actions
γ_ind = **1.2** for concrete failure and **1.0** for other modes (indirect actions from restraint); γ_F,fat = **1.0** *(CEN/TS 1992-4-1 §4.4.2)*.

---

## 3. CRACKED vs UNCRACKED CONCRETE — THE DECISION THAT MOVES THE ANSWER 30 %

### 3.1 The k-factors

**EN 1992-4 Formula (7.2):  N⁰_Rk,c = k₁ · sqrt(f_ck) · hef^1.5   [N, mm, N/mm²]**

| Fastener type | k₁ cracked (k_cr,N) | k₁ uncracked (k_ucr,N) | Ratio | Source |
|---|---|---|---|---|
| **Post-installed** (mechanical, bonded) | **7.7** | **11.0** | 1.429 | Würth/TVZ p.22; fischer white paper p.9; Hilti PROFIS report (k1 = 7.700) |
| **Cast-in headed** fasteners | **8.9** | **12.7** | 1.427 | IDEA StatiCa EN anchor check; **Peikko HPM technical manual Table 10 (k_ucr,N = 12.7, k_cr,N = 8.9)** |

Predecessor values for comparison *(fischer white paper p.9)* — note these used **cube** strength:

| | ETAG 001 / TR 029 (f_ck,cube) | EN 1992-4 (f_ck) |
|---|---|---|
| k₁ cracked | 7.2 | **7.7** |
| k₁ uncracked | 10.1 | **11.0** |
| Headed, CEN/TS 1992-4-2 §6.2.5.1 NOTE | k_cr = **8.5**, k_ucr = **11.9** | 8.9 / 12.7 |

A pure cube→cylinder conversion would multiply by √(25/20) = 1.118 (i.e. 7.2 → 8.05). The adopted 7.7 is ~4 % below that, so **EN 1992-4 gives slightly lower cone resistances than ETAG 001** across the board. fischer states this explicitly: *"the base value of the characteristic resistance is lower for all concrete failure modes than before."*

For **shear/concrete edge failure the coefficient k₉ was NOT adapted** — it stayed at **1.7 (cracked) / 2.4 (uncracked)** even though the strength basis changed from f_ck,cube to f_ck, "due to re-evaluated test results and an extension of the equation's validity" *(fischer white paper p.9)*. Net effect: edge resistance in EN 1992-4 is ≈ **11 % lower** than under ETAG 001 for the same geometry.

### 3.2 How to decide (EN 1992-4 §4.7)
*Verbatim structure from the Würth/TVZ presentation, pp.30–31:*

- **4.7(1)** "The condition of the concrete for the service life of the fastening **shall be determined by the designer**. NOTE In general, it is conservative to assume that the concrete is cracked over its service life."
- **4.7(2)** Uncracked concrete may be assumed **only if it is proven** that under the **characteristic combination at SLS**, the fastener **over its entire embedment depth** lies in uncracked concrete. Satisfied if:

```
σ_L + σ_R ≤ σ_adm          (compressive stresses negative)
```
where
- **σ_L** = concrete stress from external loads **including the fastener loads**
- **σ_R** = stress from restraint of intrinsic (shrinkage) or extrinsic (support displacement, temperature) imposed deformation. **If no detailed analysis is made, σ_R = 3 N/mm² shall be assumed.**
- **σ_adm** = admissible tensile stress. **Recommended value σ_adm = 0** (an NDP; UK NA and DK NA both keep the recommended value).

**Practical reading:** with σ_R = 3 N/mm² already eating the whole budget, "uncracked" is rarely provable in a real reinforced-concrete member. **Default to cracked.** Danish NA supplementary note says it directly: *"Hvis der kan være tvivl om hvorvidt betonen kan betragtes som urevnet, betragtes betonen som revnet"* — if in doubt, treat as cracked.

### 3.3 Product qualification
k_cr,N may only be used for anchors that hold an **ETA for cracked concrete** *(Würth/TVZ p.22)*. Anchors qualified only for uncracked concrete **must not be used** where cracks can occur — this is a product-selection issue, not a calculation issue. Torque-controlled expansion anchors developed for uncracked concrete lose load abruptly when a crack opens through them.

### 3.4 Sensitivity table — N⁰_Rk,c, cracked, C20/25, k₁ = 7.7
*(Würth/TVZ presentation p.23/27 — verified by recomputation)*

| hef [mm] | 40 | 50 | 60 | 70 | 80 | 100 | 125 | 170 |
|---|---|---|---|---|---|---|---|---|
| **N⁰_Rk,c [kN]** | 8.7 | 12.1 | 16.0 | 20.1 | 24.6 | 34.4 | 48.1 | 76.3 |
| **N⁰_Rd,c [kN]** (γ_Mc = 1.5) | 5.8 | 8.1 | 10.6 | 13.4 | 16.4 | 22.9 | 32.0 | 50.8 |

Read this to the fabricator: **a single post-installed anchor at 100 mm embedment in cracked C20/25 is worth 22.9 kN — about 2.3 tonnes.** That is the whole story of most failed brackets.

---

## 4. TENSION FAILURE MODES — EN 1992-4 §7.2.1

### 4.0 Required verifications (EN 1992-4 Table 7.1)
*(reproduced in the Würth/TVZ presentation p.18 and in every PROFIS report)*

| Failure mode | Single fastener | Group — most loaded fastener | Group — group as a whole |
|---|---|---|---|
| Steel of fastener | N_Ed ≤ N_Rk,s/γ_Ms | N^h_Ed ≤ N_Rk,s/γ_Ms | — |
| Pull-out | N_Ed ≤ N_Rk,p/γ_Mp | N^h_Ed ≤ N_Rk,p/γ_Mp | — |
| **Concrete cone** | N_Ed ≤ N_Rk,c/γ_Mc | — | **N^g_Ed ≤ N_Rk,c/γ_Mc** |
| **Splitting** | N_Ed ≤ N_Rk,sp/γ_Msp | — | **N^g_Ed ≤ N_Rk,sp/γ_Msp** |
| **Blow-out** (headed only) | N_Ed ≤ N_Rk,cb/γ_Mc | — | **N^g_Ed ≤ N_Rk,cb/γ_Mc** |
| Combined pull-out + cone (bonded only) | N_Ed ≤ N_Rk,p/γ_Mp | N^h_Ed ≤ … | N^g_Ed ≤ N_Rk,p/γ_Mp |
| Steel of supplementary reinforcement | N_Ed,re ≤ N_Rk,re/γ_Ms,re | | |
| Anchorage of supplementary reinforcement | N_Ed,re ≤ N_Rd,a | | |

**Rule of thumb to give the detailer:** *steel and pull-out are checked on the single most loaded anchor; every concrete mode is checked on the group as a whole.*

---

### 4.1 (a) STEEL FAILURE IN TENSION — N_Rk,s (§7.2.1.3)

```
N_Rd,s = N_Rk,s / γ_Ms
N_Rk,s = A_s · f_uk           (value taken from the ETA / European Technical Product Specification)
```
- **A_s** = stressed cross-section (tensile stress area of the thread, ISO 898)
- **f_uk** = characteristic ultimate tensile strength (NOT yield — the strength calculation is explicitly based on f_uk, CEN/TS 1992-4-2 §6.2.3)
- γ_Ms = 1.2·f_uk/f_yk ≥ 1.4

Real values *(Hilti PROFIS report, HAS-U 5.8 M16)*: N_Rk,s = **78.500 kN**, γ_M,s = 1.500, **N_Rd,s = 52.333 kN**.
*(Peikko HPM® L, ETA-02/0006, Table 10)*: N_Rk,s = **86.2 / 134.6 / 193.9 / 308.3 / 536.7 kN** for HPM 16/20/24/30/39 L; γ_Ms = 1.4.

This is the only **ductile** mode. Everything below is brittle.

---

### 4.2 (b) CONCRETE CONE FAILURE — N_Rk,c (§7.2.1.4) — THE CCD METHOD

**Formula (7.1):**
```
N_Rk,c = N⁰_Rk,c · (A_c,N / A⁰_c,N) · ψ_s,N · ψ_re,N · ψ_ec,N · ψ_M,N
```
*(in PROFIS output ψ_ec,N is split into ψ_ec1,N · ψ_ec2,N for the two directions)*

**Formula (7.2):**
```
N⁰_Rk,c = k₁ · sqrt(f_ck) · hef^1.5          [N; f_ck in N/mm²; hef in mm]
k₁ = k_cr,N  = 7.7  (post-installed, cracked)    = 8.9  (cast-in headed, cracked)
   = k_ucr,N = 11.0 (post-installed, uncracked)  = 12.7 (cast-in headed, uncracked)
```

**Formula (7.3) — reference projected area:**
```
A⁰_c,N = scr,N · scr,N = (3·hef)² = 9·hef²
```

**Characteristic geometry:**
```
scr,N = 3 · hef          (characteristic spacing)
ccr,N = 1.5 · hef = 0.5 · scr,N     (characteristic edge distance)
```
Both are formally "given in the relevant ETA"; for headed and most post-installed fasteners they equal these values. **Verified against a real ETA** — Hilti HSA (ETA-11/0374): hef = 30/40/60 mm → ccr,N = 45/60/90 mm and scr,N = 90/120/180 mm. Exactly 1.5·hef and 3·hef.

**Actual projected area A_c,N** — truncate at c ≤ ccr,N and s ≤ scr,N. Standard cases *(CEN/TS 1992-4-2 Figure 4; identical in EN 1992-4 Figure 7.4)*:

```
Single anchor at an edge  (c1 ≤ ccr,N):
   A_c,N = (c1 + 0.5·scr,N) · scr,N

Two anchors at an edge  (c1 ≤ ccr,N, s1 ≤ scr,N):
   A_c,N = (c1 + s1 + 0.5·scr,N) · scr,N

Four anchors at a corner (c1,c2 ≤ ccr,N; s1,s2 ≤ scr,N):
   A_c,N = (c1 + s1 + 0.5·scr,N) · (c2 + s2 + 0.5·scr,N)

Group of 4 with no edge influence:
   A_c,N = (s1 + scr,N) · (s2 + scr,N)
```

**Formula (7.4) — edge disturbance factor:**
```
ψ_s,N = 0.7 + 0.3 · c / ccr,N  ≤ 1.0
```
c = *smallest* edge distance (corner / narrow member).
*Physical meaning:* the code already removes area for the missing cone; ψ_s,N removes a further ≤30 % because the stress distribution is disturbed near a free edge.

**Formula (7.5) — shell spalling / reinforcement factor:**
```
ψ_re,N = 0.5 + hef/200  ≤ 1.0        (hef in mm)
```
Applies for hef < 100 mm with dense reinforcement. **ψ_re,N = 1.0 regardless of hef if:**
- (a) reinforcement of **any diameter** at spacing **≥ 150 mm**, or
- (b) reinforcement **Ø ≤ 10 mm** at spacing **> 100 mm**.

*(CEN/TS 1992-4-2 §6.2.5.4; the same wording appears as the "Reinforcement" input line in Hilti PROFIS: "No reinforcement or Reinforcement spacing ≥ 150 mm (any Ø) or ≥ 100 mm (Ø ≤ 10 mm)" → ψ_re,N = 1.000)*
*Physical meaning:* dense surface mesh causes a shallow spalled shell of concrete to detach before the full cone can develop.

**Formula (7.6) — eccentricity factor:**
```
ψ_ec,N = 1 / (1 + 2·e_N / scr,N)  ≤ 1.0
```
e_N = eccentricity of the resultant tension force of the tensioned anchors relative to their centre of gravity. **If eccentricity exists in two directions, compute ψ_ec,N separately per direction and multiply the two.**

**Formula (7.7) — compression factor (NEW in EN 1992-4):**
```
ψ_M,N = 2 − z / (1.5·hef)  ≥ 1.0
```
z = internal lever arm between the tension resultant and the compression resultant on the fixture.
**ψ_M,N = 1.0 (i.e. no benefit) if any of:** c < 1.5·hef, or the ratio of compression force to sum of anchor tension forces < 0.8, or z/hef ≥ 1.5. *(condition set as implemented for EN 1992-4 Cl. 7.2.1.4(7) by IDEA StatiCa — verify against the code text before publishing)*.
*Physical meaning:* under a base-plate moment the compressed side of the plate clamps the concrete and confines the cone. Maximum benefit is a **factor of 2** *(Hilti, "Eurocode 2 Part 4" article)*. In the Hilti example (z = 206.7 mm, hef = 80 mm) 2 − 206.7/120 = 0.28 → floored at **1.000**. In practice ψ_M,N > 1 only for deep anchors and short lever arms.

**Narrow-member correction (CEN/TS §6.2.5.7; retained in EN 1992-4):** when **three or more** edges are closer than ccr,N, Formula (7.1) is over-conservative. Substitute
```
h'ef = max{ c_max/ccr,N ; s_max/scr,N } · hef
s'cr,N = scr,N · h'ef/hef ;  c'cr,N = ccr,N · h'ef/hef
```
Worked example from the code: c1=110, c2=100, c3=120 (=c_max), c4=80, s=210, hef=200 → **h'ef = 120/1.5 = 80 mm > 210/3 = 70 mm → h'ef = 80 mm.**

---

### 4.3 (c) PULL-OUT / PULL-THROUGH — N_Rk,p (§7.2.1.5)

**Cast-in headed fasteners — pull-out is limited by concrete crushing under the head:**
```
N_Rk,p = k₂ · A_h · f_ck
k₂ = 7.5  (cracked)        = 10.5 (uncracked)
A_h = (π/4)·(d_h² − d²)     bearing area of the head / washer plate
```
*(k₂ values: IDEA StatiCa EN implementation of §7.2.1.5. Cross-check against the predecessor: CEN/TS 1992-4-2 Formula (2) gave **N_Rk,p = 6·A_h·f_ck,cube·ψ_ucr,N** with ψ_ucr,N = 1.0 cracked / 1.4 uncracked. For C20/25, f_ck,cube = 1.25·f_ck, so 6 × 1.25 = **7.5** ✔ and 7.5 × 1.4 = **10.5** ✔. The two formulations are numerically identical — this is a strong independent confirmation.)*

**Post-installed mechanical anchors:** N_Rk,p is a **tested** value from the ETA (mechanical interlock / expansion friction), not a formula. Pull-through (the anchor sleeve slipping over the cone) is covered by the same ETA value.

Real values *(Peikko HPM® L, Table 10, C20/25)*:

| | 16 L | 20 L | 24 L | 30 L | 39 L |
|---|---|---|---|---|---|
| N_Rk,p uncracked [kN] | 195.9 | 283.0 | 395.8 | 639.3 | 1072.1 |
| N_Rk,p cracked [kN] | **140.0** | **202.2** | **282.7** | **456.6** | **765.8** |

Ratio uncracked/cracked = 195.9/140.0 = **1.40** ✔ (confirms ψ_ucr = 1.4).
Peikko's concrete-grade scaling factor Ψ_c (linear in f_ck, as the formula demands): C25/30 → 1.25; C30/37 → 1.50; C35/45 → 1.75; C40/50 → 2.00; C45/55 → 2.25; C50/60 → **2.50**.

---

### 4.4 (d) CONCRETE SPLITTING — N_Rk,sp (§7.2.1.7)

Two distinct problems:

**(i) Splitting during installation.** Avoided purely by geometry — comply with the ETA's **c_min, s_min, h_min**. *(EN 1992-4 §7.2.1.7(1); the Würth/TVZ presentation p.48 states it exactly: "Concrete splitting during installation is avoided by complying with minimum values for edge distances c_min, spacing s_min, member thickness h_min and requirements for reinforcement as given in the relevant European Technical Product Specification.")* The same minima must be observed even for headed fasteners that are **not torqued**, simply to allow placing and compaction of concrete.

**(ii) Splitting due to loading.** **No verification required if either:**
- (a) the edge distance in **all** directions is **c ≥ 1.0·ccr,sp for a single fastener** and **c ≥ 1.2·ccr,sp for a group**, and the member thickness **h ≥ h_min**; *(CEN/TS used c > 1.0/1.2·ccr,sp **and h ≥ 2·hef**; EN 1992-4 relaxed the thickness requirement from **2·hef to h_min** — a real benefit for thin slabs, per Hilti's "Eurocode 2 Part 4" article)*; **or**
- (b) N_Rk,c and N_Rk,p are calculated for **cracked** concrete **and** reinforcement resists the splitting forces and limits crack width to **w_k ≤ 0.3 mm**.

**Splitting reinforcement (CEN/TS 1992-4-2 Formula 17):**
```
A_s = 0.5 · Σ N_Ed / (f_yk / γ_Ms,re)          [mm²]
f_yk ≤ 500 N/mm²
```
i.e. reinforcement for **50 % of the total anchor tension**, at yield. This is a cheap, robust detail and should be a standard note on Eretz Barzel's foundation drawings.

**Otherwise (Formula 7.24 / CEN/TS Eq. 18):**
```
N_Rk,sp = N⁰_Rk · (A_c,N/A⁰_c,N) · ψ_s,N · ψ_re,N · ψ_ec,N · ψ_h,sp
with N⁰_Rk = min(N⁰_Rk,c ; N_Rk,p)
and ccr,N, scr,N REPLACED BY ccr,sp, scr,sp (from the ETA)
```

**Member-depth factor (Formula 7.25 / CEN/TS Eq. 19):**
```
ψ_h,sp = (h / h_min)^(2/3)  ≤  (2·hef / h_min)^(2/3)
```

**Peikko's practical restatement** (HPM manual, Table 15 note 2): splitting need not be checked if **c ≥ 1.5·hef for one bolt**, **c ≥ 1.8·hef for groups**, and **h ≥ h_min** — i.e. exactly 1.0 and 1.2 × ccr,sp with ccr,sp = 1.5·hef. Peikko additionally makes splitting reinforcement **mandatory** for HPM bolts (Table 10: *"A reinforcement must be present to resist the splitting forces and limit the crack width to w_k ≤ 0.3 mm"*).

**Real ccr,sp / scr,sp values** *(Hilti HSA, ETA-11/0374)* — note these are **larger than ccr,N/scr,N** and are **product-specific**, not 1.5·hef:

| Size | hef [mm] | scr,sp [mm] | ccr,sp [mm] | scr,N [mm] | ccr,N [mm] | h_min [mm] |
|---|---|---|---|---|---|---|
| M6 | 30 / 40 / 60 | 100 / 120 / 130 | 50 / 60 / 65 | 90 / 120 / 180 | 45 / 60 / 90 | 100 / 100 / 120 |
| M8 | 30 / 40 / 70 | 130 / 180 / 200 | 65 / 90 / 100 | 90 / 120 / 210 | 45 / 60 / 105 | 100 / 100 / 120 |
| M10 | 40 / 50 / 80 | 190 / 210 / 290 | 95 / 105 / 145 | 120 / 150 / 240 | 60 / 75 / 120 | 100 / 120 / 160 |

---

### 4.5 (e) CONCRETE BLOW-OUT (SIDE-FACE BLOW-OUT) — N_Rk,cb (§7.2.1.8)

**Only for headed / cast-in fasteners with a large bearing head close to an edge.** The head is deep in the member; the concrete cannot break out to the top, so it **blows out sideways** from the free face — a laterally ejected wedge, with no warning at the surface.

**Verification NOT required if c ≥ 0.5·hef in all directions** *(EN 1992-4 §7.2.1.8(1); CEN/TS §6.2.7; confirmed by Peikko Table 15 note 3)*.

```
N_Rk,cb = N⁰_Rk,cb · (A_c,Nb / A⁰_c,Nb) · ψ_s,Nb · ψ_g,Nb · ψ_ec,Nb        (Formula 7.26)

N⁰_Rk,cb = k₅ · c₁ · sqrt(A_h) · sqrt(f_ck)
k₅ = 8.7 (cracked)      = 12.2 (uncracked)
```
*(k₅ from IDEA StatiCa's EN 1992-4 implementation. Cross-check against CEN/TS 1992-4-2 Formula (21): N⁰_Rk,cb = **8 · c₁ · sqrt(A_h) · sqrt(f_ck,cube)** with ψ_ucr,N = 1.0/1.4. For C20/25, 8·√1.25 = 8.94 ≈ 8.7, and 8.7 × 1.4 = 12.2 ✔. Consistent to within the usual ~3 % code-recalibration.)*

```
A⁰_c,Nb = (4·c₁)²                                       (Formula 7.27 / CEN/TS Eq. 22)
A_c,Nb  = actual area, truncated by s < 4c₁, by c₂ < 2c₁, and by member depth
```
Explicit truncated areas *(verbatim from the DIN EN 1992-4:2019-04 preview, p.62, Figure 7.8)*:
```
a)  A_c,Nb = (c₂ + s₂ + 2c₁) · 4c₁       with c₂ ≤ 2c₁ ,  s₂ ≤ 4c₁
b)  A_c,Nb = (2c₁ + f) · (c₂ + s₂ + 4c₁) with f ≤ 2c₁ ,   s₂ ≤ 4c₁
```

**Modification factors** *(DIN EN 1992-4 preview p.63, verbatim Formulae 7.28–7.30)*:
```
(7.28)  ψ_s,Nb  = 0.7 + 0.3 · c₂ / (2·c₁)          ≤ 1.0
(7.29)  ψ_g,Nb  = sqrt(n) + (1 − sqrt(n)) · s₂/(4·c₁)   ≥ 1.0      with s₂ ≤ 4c₁
(7.30)  ψ_ec,Nb = 1 / (1 + 2·e_N/(4·c₁))           ≤ 1.0
```
n = number of tensioned fasteners in a row parallel to the edge.

**Note the group logic is INVERTED** relative to cone failure: ψ_g,Nb ≥ 1, i.e. closely spaced heads near an edge *help* each other (they mobilise a common wedge). At s₂ = 4c₁ it reduces to √n (full independence).

**Fabrication implication (AISC DG1 §2.5, second edition):** *"The addition of plate washers or other similar devices does not increase the pullout strength of the anchor rod and can create construction problems… As an exception, the addition of plate washers may be of use when high-strength anchor rods are used or when concrete blowout could occur."* Enlarging A_h is the direct remedy for blow-out (N ∝ √A_h), but only after checking that the plate does not clash with the reinforcement cage.

---

### 4.6 (f) BONDED (CHEMICAL) FASTENERS — COMBINED PULL-OUT / CONCRETE CONE, N_Rk,p (§7.2.1.6)

For bonded anchors the governing tension mode is a **combined bond + shallow-cone** failure. The equations below are transcribed from a real **Hilti PROFIS Engineering 3.0.84** report for **HIT-HY 200-A + HAS-U 5.8 HDG M16, ETA 11/0493**, which prints the EN 1992-4 equation numbers alongside every line:

```
(7.13)  N_Rk,p  = N⁰_Rk,p · (A_p,N / A⁰_p,N) · ψ_g,Np · ψ_s,Np · ψ_re,N · ψ_ec1,Np · ψ_ec2,Np

(7.14)  N⁰_Rk,p = ψ_sus · τ_Rk · π · d · hef

(7.14a) ψ_sus   = 1.0                          if α_sus ≤ ψ⁰_sus
        ψ_sus   = ψ⁰_sus + 1 − α_sus           if α_sus > ψ⁰_sus

(7.15)  scr,Np  = 7.3 · d · sqrt(ψ_sus · τ_Rk)   ≤ 3·hef        ;  ccr,Np = 0.5·scr,Np

(7.17)  ψ_g,Np  = ψ⁰_g,Np − (s/scr,Np)^0.5 · (ψ⁰_g,Np − 1)     ≥ 1.0

(7.18)  ψ⁰_g,Np = sqrt(n) − (sqrt(n) − 1) · (τ_Rk/τ_Rk,c)^1.5  ≥ 1.0

(7.19)  τ_Rk,c  = k₃ /(π·d) · sqrt(hef · f_ck)                  with k₃ = 7.7

(7.20)  ψ_s,Np  = 0.7 + 0.3 · c/ccr,Np          ≤ 1.0

(7.21)  ψ_ec,Np = 1 / (1 + 2·e_c,N/scr,Np)      ≤ 1.0
```

**Real numbers from that report** (M16, hef = 80 mm, C30/37 **cracked**):
- τ_Rk,ucr,20 = **18.00 N/mm²**; τ_Rk,cr = **8.85 N/mm²** (with concrete factor ψ_c = 1.041)
- k₃ = **7.700** → τ_Rk,c = 7.7/(π·16)·√(80·30) = **7.50 N/mm²**
- scr,Np = **240.0 mm** (= 3·hef, capped), ccr,Np = **120.0 mm**
- ψ⁰_sus = **0.740**, α_sus = 0.000 → ψ_sus = **1.000**
- N⁰_Rk,p = 1.0 × 8.85 × π × 16 × 80 = **35.595 kN**; N_Rk,p = 65.248 kN (group, A_p,N/A⁰_p,N = 105 600/57 600 = 1.833); γ_M,p = 1.500 → **N_Rd,p = 43.499 kN**
- Same geometry, cone failure: k₁ = 7.700, N⁰_Rk,c = **30.178 kN** → **N_Rd,c = 36.879 kN** (cone governs, 59 % utilisation vs 50 %)

**The ψ_sus creep factor — why it exists.** In **July 2006** in the Boston "Big Dig" tunnel, ceiling panels weighing **2.6 tonnes** fell onto a moving car, killing the passenger. NTSB found a **substandard epoxy that could not hold a constant load** *(fischer white paper, p.12)*. Creep testing that followed showed **transferable bond stress can be up to 40 % lower after 50 years** than at the start. Hence:

```
ψ⁰_sus from the ETA; if the ETA gives no value, EN 1992-4 recommends ψ⁰_sus = 0.6
α_sus = (sustained tension load) / (total tension load at ULS)
```
Both permanent loads and the sustained fraction of variable loads count as sustained. **For a bonded anchor carrying mostly dead load, up to 40 % of the bond capacity is simply gone.** This is one of the most important practical differences between EN 1992-4 and the old TR 029 / CEN/TS design results.

**Bonded anchors are also excluded from many supplementary-reinforcement benefits** — see §8.

---

## 5. SHEAR FAILURE MODES — EN 1992-4 §7.2.2

### 5.0 Required verifications (EN 1992-4 Table 7.2) — *transcribed verbatim from the DIN EN 1992-4:2019-04 preview, p.65*

| # | Failure mode | Single fastener | Group — most loaded | Group — group |
|---|---|---|---|---|
| 1 | Steel **without** lever arm | V_Ed ≤ V_Rk,s/γ_Ms | V^h_Ed ≤ V_Rk,s/γ_Ms | — |
| 2 | Steel **with** lever arm | V_Ed ≤ V_Rk,s,M/γ_Ms | V^h_Ed ≤ V_Rk,s,M/γ_Ms | — |
| 3 | **Concrete pry-out** | V_Ed ≤ V_Rk,cp/γ_Mc | — | V^g_Ed ≤ V_Rk,cp/γ_Mc |
| 4 | **Concrete edge** | V_Ed ≤ V_Rk,c/γ_Mc | — | V^g_Ed ≤ V_Rk,c/γ_Mc |
| 5 | Steel of supplementary reinforcement | N_Ed,re ≤ N_Rk,re/γ_Ms,re | | |
| 6 | Anchorage of supplementary reinforcement | N_Ed,re ≤ N_Rd,a | | |

Failure modes illustrated in Figure 7.9: **a) steel failure without lever arm; b) steel failure with lever arm; c) concrete pry-out failure; d) concrete edge failure.**

---

### 5.1 (a) STEEL FAILURE IN SHEAR — §7.2.2.3

**Without lever arm (§7.2.2.3.1) — Formulae (7.34)–(7.36), verbatim from the DIN preview p.67:**
```
(7.34)  V⁰_Rk,s = k₆ · A_s · f_uk
        k₆ = 0.6  for f_uk ≤ 500 N/mm²
           = 0.5  for 500 < f_uk ≤ 1000 N/mm²

        For hef/d < 5 AND concrete class < C20/25:  multiply V⁰_Rk,s by 0.8

(7.35)  V_Rk,s = k₇ · V⁰_Rk,s
        k₇ = 1   for single fasteners
        k₇ from ETA for groups; NOTE: k₇ = 1 for ductile steel,
                                 k₇ = 0.8 for steel with rupture elongation A₅ ≤ 8 %

(7.36)  V_Rk,s = (1 − 0.01·t_grout) · k₇ · V⁰_Rk,s     [t_grout in mm]
        — applies in UNCRACKED concrete when the conditions of 6.2.2.3(2) are met
```

*Validation:* M16 grade 5.8 (A_s = 157 mm², f_uk = 500): V⁰_Rk,s = 0.6 × 157 × 500 = **47 100 N = 47.1 kN** — exactly the value printed in the Hilti PROFIS report (V⁰_Rk,s = 47.100 kN, k₇ = 1.000, γ_M,s = 1.250, V_Rd,s = **37.680 kN**).

**Peikko HPM® L**, V⁰_Rk,s = **43.1 / 67.3 / 96.9 / 154.2 / 268.3 kN** for 16/20/24/30/39 L; k₇ = 1.0; γ_Ms = 1.5.

**Grout layer rule (§6.2.2.3 / CEN/TS §5.2.3.3):** shear may be taken as acting **without a lever arm** only if:
1. the fixture is **metal**, bearing directly on the concrete or on a **levelling mortar with compressive strength ≥ 30 N/mm² and thickness ≤ d/2**;
2. the fixture bears on the fastener over ≥ **0.5·t_fix**;
3. the hole diameter d_f does not exceed **Table 6.1**.

**Peikko** re-states this as **t_grout ≤ 0.5 × d_b** — otherwise the connection **must** be designed with a lever arm.

**With lever arm (§7.2.2.3.2) — Formulae (7.37)–(7.38):**
```
(7.37)  V_Rk,s,M = α_M · M_Rk,s / l_a
(7.38)  M_Rk,s   = M⁰_Rk,s · (1 − N_Ed / N_Rd,s)   ;   N_Rd,s = N_Rk,s/γ_Ms
        M⁰_Rk,s  = 1.2 · W_el · f_uk      ;   W_el = π·d³/32
```
Lever arm *(CEN/TS 1992-4-1 §5.2.3.4)*:
```
l = a₃ + e₁
a₃ = 0.5·d                   (standard case)
   = 0                       if a washer and nut are clamped directly to the concrete surface,
                             or if a levelling grout layer ≥30 N/mm² with t_grout > d/2 is present
α_M = 1.0  no restraint (fixture free to rotate)
    = 2.0  full restraint (fixture cannot rotate AND is clamped by nut + washer)
```
**Critical note in the code:** Formula (7.38) may only be used when N_Ed is a **tension** load. **If N_Ed is compression, design the fastener as a steel element to EN 1993-1-8.**
**Also:** if restraint (α_M = 2) is assumed, **the fixture and the fastened element must be able to take up the restraint moment** — i.e. the base plate must be thick enough. This is a common detailing error.

**Peikko HPM® L** M⁰_Rk,s = **183 / 356 / 616 / 1236 / 2837 kNmm** for 16/20/24/30/39 L.

---

### 5.2 (b) CONCRETE PRY-OUT — V_Rk,cp (§7.2.2.4)

The anchor rotates in the hole; the *far* side of the anchor kicks a wedge of concrete out **behind** the load direction. Governs for **short, stiff anchors far from an edge**.

*Verbatim from the DIN EN 1992-4:2019-04 preview, p.68 — Formulae (7.39a)–(7.39d):*
```
Headed / mechanical post-installed:
  (7.39a)  no supplementary reinforcement:    V_Rk,cp = k₈ · N_Rk,c
  (7.39b)  with supplementary reinforcement:  V_Rk,cp = 0.75 · k₈ · N_Rk,c

Bonded fasteners:
  (7.39c)  no supplementary reinforcement:    V_Rk,cp = k₈ · min{N_Rk,c ; N_Rk,p}
  (7.39d)  with supplementary reinforcement:  V_Rk,cp = 0.75 · k₈ · min{N_Rk,c ; N_Rk,p}
```
```
k₈ from the ETA;  typically  k₈ = 1  for hef < 60 mm
                             k₈ = 2  for hef ≥ 60 mm
```
*(k₈ values: IDEA StatiCa EN implementation; **Peikko** Table 12 gives k₈ = **2.0** for HPM bolts and notes "If supplementary reinforcement is present, the factor k₈ has to be multiplied by 0.75"; the Hilti PROFIS report shows k₈ = 2.000 for hef = 80 mm)*

**N_Rk,c here is computed for ALL fasteners in the group loaded in shear** — not only the tensioned ones.

**Opposing shear (torsion) — EN 1992-4 §7.2.2.4(4), verbatim:** for groups with shear components in **opposing directions** (e.g. a fixture loaded predominantly by torsion), the **most unfavourable single fastener** must be verified, and when calculating A_c,N and A_p,N *"it shall be assumed that there is a virtual edge (c = 0.5·s) in the direction of the neighbouring fastener(s)"*. This is a frequently missed check on torsionally loaded brackets.

---

### 5.3 (c) CONCRETE EDGE FAILURE — V_Rk,c (§7.2.2.5)

The most common concrete shear failure in practice: a **half-cone spalls off the free edge** in the direction of the shear.

*Formulae below transcribed verbatim from the DIN EN 1992-4:2019-04 preview, pp.70–72:*
```
(7.40)  V_Rk,c = V⁰_Rk,c · (A_c,V / A⁰_c,V) · ψ_s,V · ψ_h,V · ψ_ec,V · ψ_α,V · ψ_re,V

(7.41)  V⁰_Rk,c = k₉ · d_nom^α · l_f^β · sqrt(f_ck) · c₁^1.5
        k₉ = 1.7  cracked concrete
           = 2.4  uncracked concrete

(7.42)  α = 0.1 · (l_f / c₁)^0.5
(7.43)  β = 0.1 · (d_nom / c₁)^0.2

l_f = hef       for uniform shank diameter
    ≤ 12·d_nom  if d_nom ≤ 24 mm
    ≤ max{8·d_nom ; 300 mm}  if d_nom > 24 mm

(7.44)  A⁰_c,V = 4.5 · c₁²
```
A_c,V is the actual projected area on the **side face**, limited by:
- overlapping cones of adjacent fasteners (**s ≤ 3·c₁**)
- edges parallel to the load direction (**c₂ ≤ 1.5·c₁**)
- member thickness (**h < 1.5·c₁**)

Standard cases *(Figure 7.14)*:
```
a) single fastener at a corner, h ≥ 1.5c₁, c₂ ≤ 1.5c₁ :   A_c,V = 1.5·c₁ · (1.5·c₁ + c₂)
b) group at an edge in a thin member, h < 1.5c₁, s₂ ≤ 3c₁ : A_c,V = (2 · 1.5·c₁ + s₂) · h
c) group at a corner in a thin member:                     A_c,V = (1.5·c₁ + s₂ + c₂) · h
```

**Modification factors:**
```
(7.45)  ψ_s,V  = 0.7 + 0.3 · c₂ / (1.5·c₁)     ≤ 1.0
(7.46)  ψ_h,V  = (1.5·c₁ / h)^0.5              ≥ 1.0
(7.47)  ψ_ec,V = 1 / (1 + 2·e_V/(3·c₁))        ≤ 1.0
(7.48)  ψ_α,V  = sqrt( 1 / [ (cos α_V)² + (0.5·sin α_V)² ] )   ≥ 1.0
        α_V = angle between the design shear and the perpendicular to the edge, 0° ≤ α_V ≤ 90°
```
Note **ψ_h,V is a *bonus*, not a penalty** (≥1.0): when h < 1.5c₁ the ratio A_c,V/A⁰_c,V has already over-penalised the thin member, and ψ_h,V corrects that.
Note **ψ_α,V ≥ 1.0**: shear **parallel** to the edge (α_V = 90°) gives ψ_α,V = 1/0.5 = **2.0** — twice the resistance of shear perpendicular to the edge. *(CEN/TS used (0.4·sin α_V)², giving ψ_α,V = 2.5 at 90°; EN 1992-4 changed the coefficient to 0.5, i.e. reduced the benefit to 2.0. This is a genuine change worth flagging.)*

**ψ_re,V — edge reinforcement factor.** CEN/TS 1992-4-2 §6.3.5.2.7 values:
```
ψ_re,V = 1.0  cracked concrete, no edge reinforcement or stirrups
       = 1.2  cracked concrete with straight edge reinforcement (> Ø12 mm)
       = 1.4  cracked concrete with edge reinforcement AND closely spaced stirrups or wire mesh
              with spacing a < 100 mm and a ≤ 2·c₁ ;  or uncracked concrete
Condition: ψ_re,V > 1 in cracked concrete only if hef ≥ 2.5 × the concrete cover to the edge reinforcement
```
**⚠ SOURCES DISAGREE HERE.** The DIN EN 1992-4:2019-04 National Foreword, item **(j)**, states that in EN 1992-4 *"the factor ψ_re,V … has been **limited to cracked concrete**"* — i.e. the 1.2/1.4 bonus applies to cracked concrete only, and uncracked concrete no longer gets the 1.4 automatically. Hilti's summary article instead says EN 1992-4 *"eliminated the 20 % resistance increase for edge reinforcement previously allowed under ETAG 001."* **The report should state both readings and resolve them against the code text.** Hilti PROFIS in practice printed **ψ_re,V = 1.000** for the example case.

**Torsion correction (EN 1992-4 §7.2.2.5(8), verbatim):** for two-fastener groups in torsion with opposing shears, if the ratio of the verified-edge breakout resistance to the second fastener's breakout resistance (pry-out or edge) exceeds **0.7** and **s₂ ≤ s_crit**, V_Rk,c shall be multiplied by **0.8**, where
```
s_crit = 1.5·hef + 1.5·c₁   if the second fastener is governed by pry-out
s_crit = 1.5·c₁             if governed by concrete edge failure to a second (perpendicular) edge
```

**When the edge check may be skipped** *(CEN/TS 1992-4-2 §6.3.5.1; EN 1992-4 §7.2.2.5(1) uses the same numbers in the validity condition)*: for single fasteners and groups of ≤ 4 fasteners with **c > 10·hef or c > 60·d** (**smaller value decisive**), concrete edge failure need not be checked.

**Embedded base-plate caveat (§7.2.2.5(1), verbatim):** for embedded base plates with c ≤ max{10·hef; 60·d}, the provisions are valid only if the **base plate thickness t in contact with the concrete is smaller than 0.25·hef**. For shear with a lever arm the provisions are valid only if c > max{10·hef; 60·d}.

**Minimum spacing for edge failure (§7.2.2.5(4)):** **s_min ≥ 4·d_nom**.

**Narrow, thin member correction (CEN/TS §6.3.5.2.8):** when c₂,max < 1.5·c₁ **and** h < 1.5·c₁, replace c₁ by
```
single:  c'₁ = max{ c₂,max/1.5 ; h/1.5 }
group:   c'₁ = max{ c₂,max/1.5 ; h/1.5 ; s_max/3 }
```
Worked example from the code: s = 100, c₁ = 200, h = 120, c₂,₁ = 150, c₂,₂ = 100 → **c'₁ = 150/1.5 = 100 mm**.

---

## 6. COMBINED TENSION + SHEAR — §7.2.3

**The 2018 change:** *"EN 1992-4 introduces a new approach for verification in case of interaction, for which the evaluation is carried out separately according to failure mode. Up until now, the maximum ratio of impact and resistance of all possible failure modes under tensile or shear load was applied in the interaction equation. This approach provides conservative results, as various failure modes and the resulting forces are superimposed. Furthermore, the stresses can appear in varying places and in differing materials, for example concrete failure under tensile load or steel failure under shear load."* *(fischer white paper, p.14)*

Define
```
β_N = N_Ed / N_Rd    ≤ 1        β_V = V_Ed / V_Rd    ≤ 1
```

### 6.1 Steel failure decisive (both directions)
```
β_N² + β_V²  ≤  1.0
```
Verified for **every fastener** of the group if N_Ed and V_Ed differ between fasteners. **Not required** for shear with a lever arm (that case is already covered by Formula 7.38, which reduces M_Rk,s by the tension utilisation).

### 6.2 Failure modes OTHER than steel decisive
**Either** of the following, whichever the designer prefers *(CEN/TS 1992-4-2 §6.4.1.2, Formulae 47 and 48 — retained in EN 1992-4 §7.2.3)*:
```
Simplified / bilinear:      β_N + β_V   ≤ 1.2      with β_N ≤ 1 and β_V ≤ 1
Elliptical (less conservative):  β_N^1.5 + β_V^1.5 ≤ 1.0
```
**The largest value of β_N and of β_V across the different concrete failure modes must be inserted.**

### 6.3 Fastenings WITH supplementary reinforcement for tension OR shear only
```
β_N^k₇ + β_V^k₇  ≤  1.0        with k₇ from the ETA;  NOTE: k₇ = 2/3 by current experience
```
*(CEN/TS Formula 49; the exponent 2/3 is confirmed independently by **Peikko** HPM manual Table 14, which calls it **k₁₁ = 2/3** "Exponent according to EN 1992-4:2018, section 7.2.3")*.
This is the **most severe** of the four curves — a deliberate penalty for the tolerance/eccentricity uncertainty in reinforcement that only carries one load direction.

### 6.4 Seismic (EN 1992-4 Annex C, Formula C.9)
```
(N_Ed/N_Rd,i,eq)^k₁₅ + (V_Ed/V_Rd,i,eq)^k₁₅ ≤ 1
k₁₅ = 1     steel failure of the bolt
    = 2/3   fixings with additional reinforcement (for tension or shear loads only)
    = 1     all other applications
```
*(Peikko HPM manual §2.3, citing EN 1992-4:2018 Eq. (C.9))*. Seismic resistances additionally use α_eq = **0.75** for concrete-related failures and **1.0** for steel *(CEN/TS 1992-4-1 §8.4.2)*, and the concrete **shall be assumed cracked** (§8.2.3).

### 6.5 Illustrated hierarchy (least → most conservative at 45°)
| Curve | Equation | β at N=V |
|---|---|---|
| Steel, quadratic | β_N² + β_V² ≤ 1 | 0.707 |
| Concrete, 1.5-power | β_N^1.5 + β_V^1.5 ≤ 1 | 0.630 |
| Concrete, bilinear | β_N + β_V ≤ 1.2 | 0.600 |
| Supplementary reinf., 2/3-power | β_N^(2/3) + β_V^(2/3) ≤ 1 | 0.354 |

**Hilti PROFIS worked output:** steel β_N = 0.207, β_V = 0.003, α = 2.000 → 5 % utilisation; concrete β_N = 0.589, β_V = 0.009, α = 1.500 → 46 % utilisation.

---

## 7. GEOMETRY LIMITS — c_min, s_min, h_min, c_cr, s_cr

### 7.1 The two families of "critical" distances

| Symbol | Meaning | Typical value |
|---|---|---|
| **c_cr,N = 1.5·hef** | edge distance beyond which the **full** single-anchor cone resistance is available | 1.5·hef |
| **s_cr,N = 3·hef** | spacing beyond which anchors act **independently** | 3·hef = 2·c_cr,N |
| **c_cr,sp, s_cr,sp** | same, for **splitting** — **product-specific, from the ETA, usually larger than c_cr,N/s_cr,N** | see table §4.4 |
| **c_min, s_min, h_min** | **absolute minima** to prevent splitting *during installation* — **from the ETA only, never derived** | see below |

### 7.2 Real minima from a real ETA (Hilti HSA, ETA-11/0374)
| Size | hef [mm] | h_nom [mm] | **h_min [mm]** | d₀ drill [mm] | h₁,min drill depth [mm] | d_f,max fixture hole [mm] | T_inst [Nm] |
|---|---|---|---|---|---|---|---|
| M6 | 30/40/60 | 37/47/67 | 100/100/120 | 6 | 42/52/72 | 7 | 5 |
| M8 | 30/40/70 | 39/49/79 | 100/100/120 | 8 | 44/54/84 | 9 | 15 |
| M10 | 40/50/80 | 50/60/90 | 100/120/160 | 10 | 55/65/95 | 12 | 25 |

**General floor:** *"According to guidelines, the minimum component thickness in which anchors are installed is h ≥ 80 mm"* *(Würth Design Principles §1.9.5)*. Below that, premature splitting or reduced edge shear resistance must be explicitly accounted for.

**Hilti's own application recommendation by embedment** (HSA datasheet):
- 25 mm ≤ hef < 40 mm → **redundant non-structural applications, uncracked concrete only**
- hef ≥ 40 mm → single-point structural fastenings per EN 1992-4

### 7.3 Clearance hole in the fixture — EN 1992-4 **Table 6.1**
This is the fabricator's table. **If the hole in the base plate is bigger than d_f, the whole design is invalid** — Hilti prints this as a mandatory warning on every report: *"The design is only valid if the clearance hole in the fixture is not larger than the value given in Table 6.1 of EN 1992-4!"*

*(values from CEN/TS 1992-4-1 Table 1, carried into EN 1992-4 Table 6.1; independently confirmed for M8–M24 by the Hilti HAS-U setting-detail sheet and for M16–M39 by the Peikko HPM manual Table 2)*

| d or d_nom [mm] | 6 | 8 | 10 | 12 | 14 | 16 | 18 | 20 | 22 | 24 | 27 | 30 | (39) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| **d_f [mm]** | 7 | 9 | 12 | 14 | 16 | **18** | 20 | **22** | 24 | **26** | 30 | **33** | (42) |

So: **M20 anchor → Ø22 hole. M24 → Ø26. M30 → Ø33.** These are *tight* by steel-fabrication standards. If the erector needs more play, either:
- fill the annular gap with **injection grout (≥ 40 N/mm² compressive strength)**, or
- use a **spring pin to EN ISO 13337** in a larger hole d_f1 (Peikko: M16→Ø20, M20→Ø25, M24→Ø30, M30→Ø40, M39→Ø50), or
- accept that the fastener is **not effective in shear** and re-verify — EN 1992-4 §6.2.2 covers larger clearance holes explicitly. Slotted holes in the shear direction deliberately make a fastener ineffective in shear (a legitimate technique to protect edge anchors).

### 7.4 Other geometric limits
- **s_min ≥ 4·d_nom** for concrete edge failure *(EN 1992-4 §7.2.2.5(4))*
- Adjoining groups: **a > s_cr,N** between outer fasteners of adjacent groups *(CEN/TS 1992-4-2 §6.1.6)*
- Blow-out check waived when **c ≥ 0.5·hef**
- Edge-failure check waived when **c > min{10·hef ; 60·d}**
- Minimum edge distance and spacing shall be specified **with positive tolerances only**; if not, the effect of negative tolerances must be taken into account in design *(CEN/TS 1992-4-1 §6.1.4)*. **This is a drawing-office instruction: write "c = 150 mm (+ tolerance only)" not "c = 150 ± 15".**

---

## 8. SUPPLEMENTARY REINFORCEMENT (ANCHOR REINFORCEMENT) — THE ENGINEER'S ESCAPE ROUTE

### 8.1 The principle
**EN 1992-4 §7.2.2.2(1), verbatim:** *"When the design relies on supplementary reinforcement, concrete edge failure according to Table 7.2 and 7.2.2.5 need not to be verified but the supplementary reinforcement shall be designed according to 7.2.2.6 to resist the total load."* The identical logic applies in tension (§7.2.1.9 / CEN/TS §6.2.2).

**You do not "add reinforcement to help the cone". You *delete* the cone check and hang the entire anchor force off reinforcing bars that cross the failure surface.** The bars must be anchored on **both** sides of the assumed breakout body.

Terminology across codes *(Hilti "Design of large anchorages" technical note)*: ETAG 001 calls it **"hanger reinforcement"**, CEN/TS and EN 1992-4 call it **"supplementary reinforcement"**, ACI 318 calls it **"anchor reinforcement"**.

**ETAG 001 §7.2 threshold** (still a good rule of thumb): anchorages with a resulting **tensile load exceeding 60 kN characteristic (≈ 90 kN design)** need hanger reinforcement, *or* an embedment depth of at least **80 % of the member depth**.

### 8.2 Detailing rules — TENSION (EN 1992-4 §7.2.1.9 / CEN/TS 1992-4-2 §6.2.2)
- **Ribbed bars**, same diameter for all fasteners of a group, **f_yk ≤ 500 N/mm²**, **Ø ≤ 16 mm**; mandrel diameter per EN 1992-1-1.
- Detailed as **stirrups or loops**.
- Only bars within **0.75·hef** of the fastener are effective.
- Minimum anchorage length **inside** the failure cone: **l₁,min = 4·Ø** (bends/hooks/loops) or **10·Ø** (straight bars, with or without welded transverse bars).
- Anchored **outside** the assumed failure cone with a full **l_bd** per EN 1992-1-1; in reinforced members the bar tension must be transferred to the member reinforcement by adequate **lapping**, otherwise a strut-and-tie model must be verified.
- A **surface reinforcement** must be provided, designed for the strut-and-tie forces including the splitting forces of §7.2.1.7.

### 8.3 Detailing rules — SHEAR (EN 1992-4 §7.2.2.2, verbatim from the DIN preview pp.65–66)
Two permitted forms (Figure 7.10):
- **a) surface reinforcement** taking up shear, with a simplified strut-and-tie model (compression struts may be taken at **45°**) to design the edge reinforcement;
- **b) stirrups** / **c) loops**.

For **surface reinforcement (a)** all of these must be satisfied:
- (a) if sized for the most loaded fastener, **the same reinforcement is provided around all fasteners** considered effective for concrete edge failure;
- (b) **ribbed bars, f_yk ≤ 600 N/mm², Ø ≤ 16 mm**; *(note: 600, not 500 — EN 1992-4 raised this from CEN/TS's 500)*
- (c) bars **within 0.75·c₁** of the fastener;
- (d) anchorage length in the breakout body **l₁ ≥ 10·Ø** (straight) or **4·Ø** (hook/bend/loop);
- (e) the assumed breakout body is the **same** as for the concrete edge failure calculation;
- (f) **edge reinforcement along the member edge** provided and designed by strut-and-tie (45° struts).

For **stirrups/loops (b),(c)** — **EN 1992-4 §7.2.2.2(4), verbatim:** the reinforcement *"shall enclose and be in contact with the shaft of the fastener and be positioned as closely as possible to the fixture, because direct force transfer from the fastener to the supplementary reinforcement is assumed and therefore **no verification of the anchorage length in the breakout body is required**."*
**→ This is the single most valuable practical detail in the whole standard.** A stirrup physically touching the anchor shaft, close under the base plate, eliminates the anchorage check inside the cone.

### 8.4 Design of the supplementary reinforcement — two checks

**(1) Steel yield — EN 1992-4 Formula (7.31):**
```
N_Rk,re = Σ(i=1..n_re) A_s,re,i · f_yk,re          with f_yk,re ≤ 600 N/mm²
N_Rd,re = N_Rk,re / γ_Ms,re      ,  γ_Ms,re = 1.15
```
n_re = number of bars (legs) of supplementary reinforcement effective for one fastener.

*CEN/TS shear version added an efficiency factor:* **N_Rk,re = k₆ · n · A_s · f_yk**, with **k₆ = 1.0** for surface reinforcement and **k₆ = 0.5** for stirrups/loops — *"The factor k₆ = 0.5 … takes account of unavoidable tolerances in workmanship."* **Check whether EN 1992-4 retains this factor before using it in the report.**

*Hilti's practical form:* **N_Rd,re = n_re · A_s,re · f_d,av,re**, where f_d,av,re = f_yd if the bar carries no other load (f_yd ≤ 520 N/mm²), otherwise **f_d,av,re = f_yd − σ_d** where σ_d is the design stress already in the bar from the structure. **This is important: you may not count reinforcement that is already fully working.**

**(2) Anchorage in the breakout body — EN 1992-4 Formulae (7.32)–(7.33):**
```
N_Rd,a = Σ(i=1..n_re) N⁰_Rd,a,i

N⁰_Rd,a = l₁ · π · Ø · f_bd / (α₁ · α₂)     ≤   A_s,re · f_yk,re / γ_Ms,re
```
- **l₁** = anchorage length inside the breakout body, ≥ the minima of §8.2
- **f_bd** = design bond strength per **EN 1992-1-1 §8.4.2**: f_bd = 2.25·η₁·η₂·f_ctd. Hilti's shortcut for good bond and ≤ C50/60: **f_bd = 0.315·f_ck^(2/3)**
- **α₁, α₂** = EN 1992-1-1 §8.4.4 factors; **α₁ = 0.7** for hooked/bent bars

**Both checks must pass, on both sides of the failure surface.**

### 8.5 Force to be carried — shear case (EN 1992-4 Formula 6.6)
```
N_Ed,re = V_Ed · (e_s / z + 1)
```
- **e_s** = distance between the reinforcement and the shear force acting on the fixture
- **z** = internal lever arm of the concrete member ≈ **0.85·d**, with **z ≤ min{2·hef ; 2·c₁}**

### 8.6 Where it does NOT work
- The codes *"state that supplementary reinforcement can in general only be taken into account for **cast-in headed studs**"* *(Hilti technical note)*. For **post-installed anchors**, existing structural reinforcement may be counted **only if** it was foreseen for these loads at design stage, or if the surface reinforcement in place is **not fully used** for pre-existing loads.
- If the supplementary reinforcement **ends inside the concrete section** without leading the load further into the structure, **concrete cone / edge failure must be re-checked from the end of the supplementary reinforcement**.
- **Edge/surface reinforcement perpendicular to the anchor load is still required** to take the splitting forces, estimated by a 45° strut-and-tie model.
- Pry-out is **not** eliminated by supplementary reinforcement — it is only reduced by the factor **0.75** (Formulae 7.39b / 7.39d).

### 8.7 A real, buildable table
**Peikko HPM® L concrete-cone reinforcement, B500B** (Table 25):

| Anchor bolt | Stirrups (per bolt) ① | Surface bars ② | c_nom [mm] | R₁,max [mm] | **hef [mm]** | Stirrup width b [mm] |
|---|---|---|---|---|---|---|
| HPM 16 L | 4 Ø8 | Ø8 | 35 | 75 | **165** | 85 |
| HPM 20 L | 4 Ø8 | Ø8 | 35 | 85 | **223** | 90 |
| HPM 24 L | 4 Ø8 | Ø8 | 35 | 100 | **287** | 105 |
| HPM 30 L | 4 Ø10 | Ø8 | 35 | 100 | **335** | 125 |
| HPM 39 L | 4 Ø12 | Ø8 | 35 | 200 | **502** | 150 |

Conditions: concrete ≥ **C25/30** (good bond); c_nom ≤ 35 mm; clear distance between adjacent stirrup legs **≥ 21 mm** (EN 1992-1-1 §8.2, D_max = 16 mm aggregate); cover to stirrups ≥ 3Ø so that **α₁·α₂ = 0.7 × 1 = 0.7**. Continuity outside the cone achieved either by stirrups **enclosing the bottom reinforcement** of the member, or by **lap-splices** into the structural reinforcement.

**Note the embedment depths.** An HPM 24 L needs **287 mm** of embedment and an HPM 39 L needs **502 mm**. This must be coordinated with the pad/pier depth on the concrete drawings before steel fabrication starts.

---

## 9. ACI 318-19 CHAPTER 17 — THE PARALLEL FRAMEWORK

Same CCD physics, different presentation, different safety format (φ-factors instead of γ_M).

### 9.1 Tension
```
Steel:            Nsa  = Ase,N · futa                                (17.6.1.2)
                  futa ≤ min(1.9·fya ; 125,000 psi [860 MPa])

Breakout single:  Ncb  = (ANc/ANco) · Ψed,N · Ψc,N · Ψcp,N · Nb      (17.6.2.1a)
Breakout group:   Ncbg = (ANc/ANco) · Ψec,N · Ψed,N · Ψc,N · Ψcp,N · Nb  (17.6.2.1b)

Basic:            Nb   = kc · λa · sqrt(f'c) · hef^1.5               (17.6.2.2.1)
                  kc = 24 cast-in / 17 post-installed   [lb, psi, in]
                  kc = 10 cast-in / 7  post-installed   [N, MPa, mm]  (ACI 318M; derived by unit
                                                          conversion — verify against ACI 318M-19)

Large hef:        Nb = 16 · λa · sqrt(f'c) · hef^(5/3)  for 11 in ≤ hef ≤ 25 in   [lb,psi,in]
                     = 3.9 · λa · sqrt(f'c) · hef^(5/3) for 280 mm ≤ hef ≤ 635 mm [N,MPa,mm]

ANco = 9·hef²                                                        (17.6.2.1.4)
Ψed,N = 0.7 + 0.3·ca,min/(1.5·hef) ≤ 1.0                             (17.6.2.4.1)
Ψec,N = 1/(1 + 2·e'N/(3·hef))      ≤ 1.0                             (17.6.2.3.1)
Ψc,N  = 1.0 cracked ; 1.25 cast-in uncracked ; 1.40 post-installed uncracked
Ψcp,N = ca,min/cac ≥ 1.5hef/cac    ≤ 1.0   (post-installed, uncracked, no splitting reinf.)

Pullout:          Npn = Ψc,P · Np ;  Np = 8·Abrg·f'c (headed) ; Np = 0.9·f'c·eh·da (hooked)
                  Ψc,P = 1.0 cracked ; 1.4 uncracked

Side-face blowout: Nsb  = 160·ca1·sqrt(Abrg)·λa·sqrt(f'c)   [lb,psi,in]
                        = 13 ·ca1·sqrt(Abrg)·λa·sqrt(f'c)   [N,MPa,mm]
                   applies when hef > 2.5·ca1
                   Nsbg = (1 + s/(6·ca1)) · Nsb   for a group
```

### 9.2 Shear
```
Steel:     Vsa = 0.6·Ase,V·futa (bolts, hooked) ; Vsa = Ase,V·futa (welded headed studs)
           Built-up grout pads: multiply by 0.80

Breakout:  Vcb  = (AVc/AVco)·Ψed,V·Ψc,V·Ψh,V·Vb         (17.7.2.1a)
           Vcbg = (AVc/AVco)·Ψec,V·Ψed,V·Ψc,V·Ψh,V·Vb   (17.7.2.1b)
           AVco = 4.5·ca1²

Vb = lesser of:   7·(le/da)^0.2 · sqrt(da) · λa·sqrt(f'c)·ca1^1.5     (17.7.2.2.1a)
                  9 ·             λa·sqrt(f'c)·ca1^1.5                 (17.7.2.2.1b)
                  [SI: 0.6 and 3.7 respectively]
le = hef for constant-stiffness anchors ; = 2·da for torque-controlled expansion ; le ≤ 8·da

Ψed,V = 0.7 + 0.3·ca2/(1.5·ca1) ≤ 1.0
Ψec,V = 1/(1 + 2·e'V/(3·ca1))   ≤ 1.0
Ψh,V  = sqrt(1.5·ca1/ha)        ≥ 1.0
Ψc,V  = 1.0 cracked, no supplementary reinforcement
      = 1.2 cracked with ≥ No.4 (Ø13) edge reinforcement
      = 1.4 cracked with ≥ No.4 edge reinforcement AND stirrups at ≤ 4 in (100 mm)
      = 1.4 uncracked

Pryout:    Vcp = kcp·Ncp ; Vcpg = kcp·Ncpg
           kcp = 1.0 for hef < 2.5 in (65 mm) ; 2.0 for hef ≥ 2.5 in
```

### 9.3 Interaction and φ
```
17.8.3:  Nua/(φNn) + Vua/(φVn) ≤ 1.2
         full tension permitted if Vua ≤ 0.2·φVn ; full shear if Nua ≤ 0.2·φNn
Alternative (permitted): (Nua/φNn)^(5/3) + (Vua/φVn)^(5/3) ≤ 1.0
```
φ (Table 17.5.3): **0.75** steel tension (ductile), 0.65 brittle; **0.65** steel shear (ductile), 0.60 brittle; concrete modes: **0.75 (Condition A — supplementary reinforcement present)** / **0.70 (Condition B — cast-in, no supplementary reinforcement)**, with lower values (down to 0.45) for post-installed anchor Categories 2 and 3. *Search results in this session gave conflicting φ tabulations — the exact Table 17.5.3 values must be read off ACI 318-19 before publication.*
**Anchor reinforcement (17.5.2.1):** where developed per Chapter 25 on both sides of the breakout surface, the design strength of the anchor reinforcement may be used **instead of** the concrete breakout strength — the direct ACI analogue of EN 1992-4's supplementary reinforcement, with **φ = 0.75**.
**Seismic (17.10):** in SDC C–F, concrete-controlled strengths are further multiplied by **0.75**, and adhesive anchors need ACI 355.4 seismic qualification.

### 9.4 EN 1992-4 vs ACI 318-19 — headline comparison
| | EN 1992-4 | ACI 318-19 (SI) |
|---|---|---|
| Cone coefficient, cast-in, cracked | **8.9**·√f_ck·hef^1.5 | **10**·λ√f'c·hef^1.5 |
| Safety treatment | ÷ γ_Mc = 1.5 | × φ = 0.70 (Cond. B) |
| Net effective coefficient | 8.9/1.5 = **5.93** | 10 × 0.70 = **7.0** |
| Reference area | 9·hef² (identical) | 9·hef² (identical) |
| Deep-embedment relief | none | **hef^(5/3)** for 280–635 mm |
| Interaction | separate steel / concrete, exponent 2 / 1.5 or 1.2-bilinear | single 1.2-bilinear or 5/3 |
| Pull-out (headed) | 7.5·A_h·f_ck cracked | 8·A_brg·f'c × Ψc,P (1.0 cracked) |

**ACI is roughly 15–20 % more generous on tension cone capacity** for the same geometry. Load factors partly offset this (1.2D+1.6L vs 1.35G+1.5Q). **The report must state which code governs; results are not interchangeable.**

---

## 10. WORKED EXAMPLES

### EXAMPLE 1 — Cast-in headed anchor group under a moment-resisting column base
**Geometry:** 4 × M24 cast-in headed anchors (grade 8.8), square pattern **s₁ = s₂ = 300 mm**, **hef = 300 mm**, head diameter d_h = 44 mm. Concrete **C30/37, CRACKED**, pad thickness **h = 800 mm**, no edge influence (c ≥ 1.5·hef = 450 mm all round). γ_c = 1.5, γ_inst = 1.0 (cast-in).

**Step 1 — Steel (§7.2.1.3)**
A_s(M24) = 353 mm²; f_uk = 800, f_yk = 640 N/mm²
N_Rk,s = 353 × 800 = **282.4 kN/anchor**
γ_Ms = 1.2 × 800/640 = **1.50** → **N_Rd,s = 188.3 kN/anchor**; 4 anchors = **753 kN**

**Step 2 — Pull-out (§7.2.1.5)**
A_h = (π/4)(44² − 24²) = (π/4)(1936 − 576) = **1068 mm²**
N_Rk,p = k₂·A_h·f_ck = 7.5 × 1068 × 30 = **240.3 kN/anchor**
γ_Mp = 1.5 → **N_Rd,p = 160.2 kN/anchor**

**Step 3 — Concrete cone (§7.2.1.4) — the governing check**
```
N⁰_Rk,c = 8.9 × √30 × 300^1.5 = 8.9 × 5.4772 × 5196.2 = 253.3 kN
s_cr,N  = 3 × 300 = 900 mm ;  A⁰_c,N = 900² = 810 000 mm²
A_c,N   = (s₁ + s_cr,N)(s₂ + s_cr,N) = (300+900)² = 1200² = 1 440 000 mm²
A_c,N/A⁰_c,N = 1.778
ψ_s,N = 1.0 (no edge) ; ψ_re,N = 1.0 (hef = 300 > 200) ; ψ_ec,N = 1.0 ; ψ_M,N = 1.0 (conservative)

N_Rk,c = 253.3 × 1.778 = 450.4 kN
N_Rd,c = 450.4 / 1.5 = 300.3 kN   ← GOVERNS the group
```

**Step 4 — Splitting & blow-out**
c ≥ 1.5·hef = 450 mm ≥ 1.2·c_cr,sp (assuming c_cr,sp = 1.5·hef) and h ≥ h_min → **splitting check waived**, but splitting reinforcement A_s = 0.5·ΣN_Ed/(f_yk/γ_Ms,re) should still be detailed as good practice.
c ≥ 0.5·hef = 150 mm → **blow-out check waived**.

**Step 5 — The lesson**

| | per-anchor equivalent |
|---|---|
| Steel N_Rd,s | 188.3 kN |
| Pull-out N_Rd,p | 160.2 kN |
| **Cone (group ÷ 4)** | **75.1 kN** |

**The concrete cone is 2.5× weaker than the bolt.** The bolt grade is irrelevant to this connection.

**Step 6 — What actually helps: spacing.** Re-run with **s₁ = s₂ = 900 mm (= s_cr,N)**:
A_c,N = (900+900)² = 3 240 000 mm² → ratio = **4.0** → N_Rk,c = 1013 kN → **N_Rd,c = 675.6 kN**.
**Spreading the bolts to 3·hef more than doubles the connection capacity, at zero material cost.** Not always possible under a column flange — which is exactly why supplementary reinforcement exists.

**Step 7 — Design tension N_Ed = 480 kN > 300.3 kN. Apply supplementary reinforcement (§7.2.1.9).**
```
Yield check:   A_s,re,required = N_Ed · γ_Ms,re / f_yk,re = 480 000 × 1.15 / 500 = 1104 mm²
Provide 2 × Ø10 closed stirrups per anchor = 4 legs × 78.5 = 314 mm²/anchor → 1257 mm² total
N_Rk,re = 1257 × 500 = 628.5 kN ;  N_Rd,re = 628.5/1.15 = 546.5 kN  >  480 kN   ✔
```
```
Anchorage check (Formula 7.33), C30/37:
   f_ctk,0.05 = 2.0 ; f_ctd = 2.0/1.5 = 1.333 ; f_bd = 2.25 × 1.0 × 1.0 × 1.333 = 3.0 N/mm²
   α₁ = 0.7 (loops), α₂ = 1.0
   N⁰_Rd,a = l₁ · π · 10 · 3.0 / 0.7
     l₁ = 200 mm → 200 × 31.416 × 3.0/0.7 = 26.9 kN/leg     (< cap 78.5×500/1.15 = 34.1 kN)
        → 4 legs × 4 anchors × 26.9 = 431 kN  <  480 kN   ✘  NOT ENOUGH
     l₁ = 250 mm → 250 × 31.416 × 3.0/0.7 = 33.7 kN/leg  → capped at 34.1 kN
        → 4 × 4 × 33.7 = 539 kN  >  480 kN   ✔
```
**Result:** 2 × Ø10 B500 closed stirrups per anchor, each leg with **at least 250 mm** inside the cone (and full l_bd lapped into the pad reinforcement below), plus surface reinforcement designed by strut-and-tie. Bars must sit **within 0.75·hef = 225 mm** of each anchor.
Note that the anchorage check, not the yield check, was decisive — a very common outcome and the reason "just add a stirrup" fails on site.

---

### EXAMPLE 2 — Post-installed bonded anchors near a slab edge, shear-critical bracket
**Geometry:** 2 × **M16 HAS-U 5.8** with **HIT-HY 200-A** injection mortar (ETA 11/0493), **hef = 125 mm**, edge distance in the shear direction **c₁ = 100 mm**, spacing parallel to the edge **s₂ = 150 mm**, slab **h = 250 mm**, concrete **C25/30 CRACKED**, shear perpendicular to the edge. γ_c = 1.5; γ_inst = 1.0 (shear).

**Step 1 — Steel shear (§7.2.2.3.1)**
V⁰_Rk,s = k₆·A_s·f_uk = 0.6 × 157 × 500 = **47.1 kN**; k₇ = 1.0
γ_Ms = 1.0 × 500/400 = **1.25** → V_Rd,s = **37.7 kN/anchor** → **75.4 kN for the pair**

**Step 2 — Concrete edge failure (§7.2.2.5)**
```
α = 0.1·(l_f/c₁)^0.5 = 0.1·(125/100)^0.5 = 0.1118
β = 0.1·(d_nom/c₁)^0.2 = 0.1·(16/100)^0.2 = 0.0693
d_nom^α = 16^0.1118 = 1.363   ;   l_f^β = 125^0.0693 = 1.397

V⁰_Rk,c = k₉ · d_nom^α · l_f^β · √f_ck · c₁^1.5
        = 1.7 × 1.363 × 1.397 × √25 × 100^1.5
        = 1.7 × 1.363 × 1.397 × 5.0 × 1000 = 16.2 kN

A⁰_c,V = 4.5 × 100² = 45 000 mm²
h = 250 ≥ 1.5c₁ = 150 → full depth available
A_c,V  = (1.5c₁ + s₂ + 1.5c₁) × 1.5c₁ = (150 + 150 + 150) × 150 = 67 500 mm²
A_c,V/A⁰_c,V = 1.50

ψ_s,V = 1.0 (c₂ ≥ 1.5c₁)  ;  ψ_h,V = (150/250)^0.5 = 0.775 → floored at 1.0
ψ_ec,V = 1.0 ; ψ_α,V = 1.0 (perpendicular) ; ψ_re,V = 1.0 (cracked, no dense edge reinf.)

V_Rk,c = 16.2 × 1.50 = 24.3 kN
V_Rd,c = 24.3 / 1.5 = 16.2 kN     ← GOVERNS
```

**Step 3 — Pry-out (§7.2.2.4), for comparison**
```
N⁰_Rk,c = 7.7 × √25 × 125^1.5 = 7.7 × 5.0 × 1397.5 = 53.8 kN
s_cr,N = 375 mm ; A⁰_c,N = 140 625 mm²
A_c,N  = (c₁ + 0.5s_cr,N)(s₂ + s_cr,N) = (100+187.5)(150+375) = 287.5 × 525 = 150 938 mm²
ratio = 1.073 ;  ψ_s,N = 0.7 + 0.3×100/187.5 = 0.86 ; ψ_re,N = 1.0
N_Rk,c = 53.8 × 1.073 × 0.86 = 49.7 kN
Bond:  N⁰_Rk,p = ψ_sus·τ_Rk,cr·π·d·hef = 1.0 × 8.5 × π × 16 × 125 = 53.4 kN/anchor
k₈ = 2 (hef ≥ 60 mm) → V_Rk,cp ≈ 2 × 49.7 = 99.4 kN → V_Rd,cp = 66.3 kN   (not critical)
```

**Step 4 — The result**

| Mode | V_Rd (pair) |
|---|---|
| Steel | 75.4 kN |
| Pry-out | 66.3 kN |
| **Concrete edge** | **16.2 kN** |

**A 100 mm edge distance throws away 78 % of the connection.** Doubling c₁ to 200 mm multiplies V⁰_Rk,c by 2^1.5 = **2.83** (plus a larger A_c,V) — the single most powerful lever available.

**Step 5 — Rescue with supplementary shear reinforcement.** Required V_Ed = 40 kN.
```
d = 250 − 40 = 210 mm ;  z = 0.85 × 210 = 178.5 mm ≤ min(2hef = 250 ; 2c₁ = 200) ✔
e_s = 60 mm (reinforcement to line of shear)
N_Ed,re = V_Ed·(e_s/z + 1) = 40 × (60/178.5 + 1) = 40 × 1.336 = 53.4 kN

A_s,re = 53 400 × 1.15 / 500 = 123 mm²
Provide 2 × Ø8 stirrups (4 legs) = 201 mm²  →  N_Rd,re = 201 × 500/1.15 = 87.4 kN  >  53.4 kN ✔
```
Because the stirrups **enclose and contact the anchor shaft** and sit as close as possible to the fixture, **no anchorage check inside the breakout body is required** (§7.2.2.2(4)). Edge reinforcement along the slab edge must still be designed for the strut-and-tie forces at 45°.
**However:** the codes generally restrict supplementary reinforcement to **cast-in** fasteners. For this post-installed case, the existing slab reinforcement may be counted only if it was designed for these loads or is demonstrably not fully utilised. **In a real project this bracket should be re-detailed with cast-in anchors or a larger edge distance.**

---

## 11. FABRICATION AND DETAILING IMPLICATIONS — WHAT THIS MEANS ON THE SHOP FLOOR

### 11.1 Ten rules for the detailer

1. **hef is a design variable, not a shop decision.** *"From a structural design point of view, the engineer has to mention the anchor's effective anchorage depth in his or her detailed drawings. Only this value guarantees that suppliers provide anchors with the respective performance."* *(Würth Design Principles §1.9.1)* Put **hef** on the drawing, not just the bolt length.
2. **Base-plate holes must not exceed EN 1992-4 Table 6.1 d_f.** M20 → Ø22, M24 → Ø26, M30 → Ø33. Anything bigger invalidates the shear design unless the gap is grouted or spring-pinned.
3. **Bolt spacing beats bolt size.** Push toward s ≥ 3·hef before increasing diameter.
4. **Edge distance beats everything in shear.** V ∝ c₁^1.5.
5. **Specify minimum edge distance and spacing with POSITIVE tolerance only** *(CEN/TS 1992-4-1 §6.1.4)*.
6. **The grout layer decides whether shear acts with a lever arm.** Grout ≥ 30 N/mm² and t_grout ≤ d/2 → no lever arm. Thicker or weaker grout → lever-arm design, and the base plate must carry the restraint moment. Peikko requires injection grout ≥ **40 N/mm²** for the annular gap.
7. **The concrete surface under the plate must be rough (≥ 3 mm, EN 1992-1-1 §6.2.5 / EN 1992-4 §6.2.2.3)** for friction transfer of shear. *"A smooth surface causes reduced resistance for shear loads."* *(Peikko HPM §1.2.2)*
8. **Use headed rods or threaded rods with a nut for anchorage — not hooks.** AISC DG1 §2.5: *"hooked rods have a very limited pullout strength compared with that of headed rods or threaded rods with a nut for anchorage. Therefore, current recommended practice is to use headed rods or threaded rods with a nut."*
9. **Don't add plate washers reflexively.** They do **not** increase pull-out strength and interfere with reinforcement and concrete compaction — except deliberately, to fix a blow-out problem (larger A_h).
10. **Heavy hex nuts, ample thread length.** AISC DG1 §2.5/§2.7: all nuts should be **heavy hex** (A563 Gr A, or DH for high strength), and thread lengths specified at least **3 in. (75 mm) longer than required** to absorb setting-elevation variation.

### 11.2 Setting tolerances — the number-one field problem
AISC DG1 §2.8 states plainly that *"The most common field problem is anchor rod placements that either do not fit within the anchor rod hole pattern or do not allow the column to be properly positioned."*

**AISC Code of Standard Practice §7.5.1** anchor-rod setting tolerances (as quoted in DG1):
- (a) between centres of any two anchor rods **within a group**: ≤ **1/8 in. (3 mm)**
- (b) between centres of **adjacent groups**: ≤ **1/4 in. (6 mm)**
- (c) elevation of tops of anchor rods: **± 1/2 in. (13 mm)**
- (d) accumulated variation along an established column line: **1/4 in. per 100 ft**, total ≤ **1 in. (25 mm)**
- (e) from centre of a group to the established column line: ≤ **1/4 in. (6 mm)**

**ACI 117 §2.3** for embedded items allows **± 1 in. (25 mm)** — four times looser. DG1's recommendation: *"it is recommended that the project specifications … require that the anchor rods be set in accordance with the AISC Code of Standard Practice tolerance requirements, in order to clearly establish a basis for acceptance of the anchor rods."*
**→ Eretz Barzel should put an explicit tolerance clause in the concrete sub-contract, not rely on ACI 117.**

Use **rigid steel templates**, survey and mark grid lines after template removal, clean and check that every nut turns freely, lubricate threads if needed.

### 11.3 Coordination with the reinforcement cage — the real integration problem
- **Anchor rods in piers must never extend below the bottom of the pier into the footing** (DG1 §2.7) — that forces partial embedment before forming, which makes alignment impossible. If the pier is shorter than the required embedment, **delete the pier and extend the column**.
- Supplementary reinforcement must be inside 0.75·hef of the anchor **and** lapped into the member reinforcement — this must be drawn on the rebar drawings, not left to the site.
- Minimum clear distance between adjacent stirrup legs **≥ 21 mm** for 16 mm aggregate (EN 1992-1-1 §8.2, per Peikko).
- Peikko's HPM 24 L requires **hef = 287 mm** and HPM 39 L **502 mm** — check the pad depth before you order the bolts.

### 11.4 Anchor material and corrosion
- Preferred US spec: **ASTM F1554**, Grade 36 default, Gr 55 for uplift/moment, Gr 105 only when larger Gr 36/55 rods will not fit. Colour codes: **Gr 36 blue, Gr 55 yellow, Gr 105 red**.
- **All threaded components of a galvanised assembly must be galvanised by the same process** (hot-dip ASTM A153 or mechanical B695) — mixing processes gives an unworkable assembly. Buy rods and nuts from the same supplier, preassembled. Galvanising raises nut friction; special lubrication may be needed.
- European practice: HDG to **EN ISO 1461 / EN ISO 10684**, typically **50 µm** average, giving corrosion class **C3 ≥ 26 years** to ISO 9223 *(Peikko)*.
- **Wedge-type mechanical anchors are not recommended for column anchor rods**, because they must be tensioned to lock the wedge and column movement during erection can loosen them *(DG1 §2.5)*.

### 11.5 Base-plate welding (DG1 §2.4)
- Fillet welds preferred to groove welds for all but large moment bases.
- Avoid the weld-all-around symbol on wide-flange columns — weld across the flange toes and in the web-flange radius adds little strength at high cost.
- Example: a **5/16 in. (8 mm), 22 in. (560 mm) long fillet to each flange fully develops a 1 in. (25 mm) F1554 Gr 36 anchor rod** in tension (using the transverse-loading directional strength increase).
- Consider fillets on all faces up to 3/4 in. (19 mm) before resorting to groove welds.

### 11.6 Do not forget the concrete member itself
**EN 1992-4 Annex A (normative)** requires verification that the fastener loads can be transmitted to the supports of the concrete member per EN 1992-1-1, including the **shear resistance of the concrete member** under the concentrated anchor loads. Hilti PROFIS prints this as a standing warning: *"Checking the transfer of loads into the base material is required in accordance with EN 1992-4, Annex A!"* The anchorage calculation only proves the local anchorage — not that the pad, slab or wall can carry the load away.

---

## 12. QUICK-REFERENCE FORMULA SHEET (EN 1992-4:2018)

```
DESIGN FORMAT
  E_d ≤ R_d = R_k/γ_M ;  γ_Ms(T)=1.2·f_uk/f_yk≥1.4 ; γ_Ms(V)=1.0·f_uk/f_yk≥1.25
  γ_Mc = γ_c·γ_inst (γ_c=1.5) ; γ_Msp = γ_Mp = γ_Mc ; γ_Ms,re = 1.15

TENSION
  (7.1)  N_Rk,c   = N⁰_Rk,c·(A_c,N/A⁰_c,N)·ψ_s,N·ψ_re,N·ψ_ec,N·ψ_M,N
  (7.2)  N⁰_Rk,c  = k₁·√f_ck·hef^1.5    k₁: 7.7/11.0 post-inst.; 8.9/12.7 cast-in headed
  (7.3)  A⁰_c,N   = s_cr,N² = 9hef²     s_cr,N = 3hef ; c_cr,N = 1.5hef
  (7.4)  ψ_s,N    = 0.7 + 0.3·c/c_cr,N        ≤ 1
  (7.5)  ψ_re,N   = 0.5 + hef/200             ≤ 1   (=1 if reinf. spacing ≥150 or ≥100 for Ø≤10)
  (7.6)  ψ_ec,N   = 1/(1 + 2e_N/s_cr,N)       ≤ 1
  (7.7)  ψ_M,N    = 2 − z/(1.5hef)            ≥ 1
         N_Rk,p   = k₂·A_h·f_ck                k₂ = 7.5 cracked / 10.5 uncracked
  (7.13) N_Rk,p   = N⁰_Rk,p·(A_p,N/A⁰_p,N)·ψ_g,Np·ψ_s,Np·ψ_re,N·ψ_ec,Np   [bonded]
  (7.14) N⁰_Rk,p  = ψ_sus·τ_Rk·π·d·hef        ψ⁰_sus = 0.6 if not in ETA
  (7.15) s_cr,Np  = 7.3·d·√(ψ_sus·τ_Rk) ≤ 3hef
  (7.19) τ_Rk,c   = k₃/(π·d)·√(hef·f_ck)      k₃ = 7.7
  (7.24) N_Rk,sp  = min(N⁰_Rk,c;N_Rk,p)·(A_c,N/A⁰_c,N)·ψ_s,N·ψ_re,N·ψ_ec,N·ψ_h,sp
                     with c_cr,sp, s_cr,sp from ETA
  (7.25) ψ_h,sp   = (h/h_min)^(2/3) ≤ (2hef/h_min)^(2/3)
  (7.26) N_Rk,cb  = N⁰_Rk,cb·(A_c,Nb/A⁰_c,Nb)·ψ_s,Nb·ψ_g,Nb·ψ_ec,Nb   [c < 0.5hef only]
         N⁰_Rk,cb = k₅·c₁·√A_h·√f_ck          k₅ = 8.7 cracked / 12.2 uncracked
  (7.27) A⁰_c,Nb  = (4c₁)²
  (7.28) ψ_s,Nb   = 0.7 + 0.3·c₂/(2c₁)  ≤ 1
  (7.29) ψ_g,Nb   = √n + (1−√n)·s₂/(4c₁) ≥ 1
  (7.30) ψ_ec,Nb  = 1/(1 + 2e_N/(4c₁))  ≤ 1
  (7.31) N_Rk,re  = Σ A_s,re,i·f_yk,re   (f_yk,re ≤ 600 N/mm²)
  (7.33) N⁰_Rd,a  = l₁·π·Ø·f_bd/(α₁α₂) ≤ A_s,re·f_yk,re/γ_Ms,re

SHEAR
  (7.34) V⁰_Rk,s  = k₆·A_s·f_uk          k₆ = 0.6 (f_uk≤500) / 0.5 (500<f_uk≤1000)
  (7.35) V_Rk,s   = k₇·V⁰_Rk,s           k₇ = 1 single / 0.8 if A₅ ≤ 8 %
  (7.36) V_Rk,s   = (1 − 0.01·t_grout)·k₇·V⁰_Rk,s
  (7.37) V_Rk,s,M = α_M·M_Rk,s/l_a       α_M = 1 free / 2 fully restrained ; l = a₃+e₁, a₃=0.5d
  (7.38) M_Rk,s   = M⁰_Rk,s·(1 − N_Ed/N_Rd,s)
  (7.39) V_Rk,cp  = k₈·N_Rk,c  [×0.75 with supp. reinf.]  k₈ = 1 (hef<60) / 2 (hef≥60)
                  = k₈·min{N_Rk,c;N_Rk,p} for bonded
  (7.40) V_Rk,c   = V⁰_Rk,c·(A_c,V/A⁰_c,V)·ψ_s,V·ψ_h,V·ψ_ec,V·ψ_α,V·ψ_re,V
  (7.41) V⁰_Rk,c  = k₉·d_nom^α·l_f^β·√f_ck·c₁^1.5   k₉ = 1.7 cracked / 2.4 uncracked
  (7.42) α = 0.1·(l_f/c₁)^0.5      (7.43) β = 0.1·(d_nom/c₁)^0.2
  (7.44) A⁰_c,V   = 4.5·c₁²
  (7.45) ψ_s,V    = 0.7 + 0.3·c₂/(1.5c₁)  ≤ 1
  (7.46) ψ_h,V    = (1.5c₁/h)^0.5         ≥ 1
  (7.47) ψ_ec,V   = 1/(1 + 2e_V/(3c₁))    ≤ 1
  (7.48) ψ_α,V    = √[1/((cos α_V)² + (0.5 sin α_V)²)]  ≥ 1
         ψ_re,V   = 1.0 / 1.2 / 1.4 (cracked concrete only in EN 1992-4)

INTERACTION
  steel:             β_N² + β_V² ≤ 1
  concrete:          β_N + β_V ≤ 1.2      OR      β_N^1.5 + β_V^1.5 ≤ 1
  supp. reinf. (one direction only):  β_N^(2/3) + β_V^(2/3) ≤ 1
  seismic (C.9):     β_N^k₁₅ + β_V^k₁₅ ≤ 1 ,  k₁₅ = 1 or 2/3
```

---

## 13. RELIABILITY NOTES / WHERE SOURCES DISAGREE

1. **ψ_re,V in EN 1992-4** — DIN foreword item (j) says it was *limited to cracked concrete*; Hilti's summary article says the 20 % edge-reinforcement increase was *eliminated*. **Resolve against the code text.**
2. **ACI 318-19 kc** — the Williams Form ACI summary and the ACI code give **24 cast-in / 17 post-installed** (inch-lb); one blog source (panacheg.com) states these **reversed**. Use 24/17.
3. **ACI Table 17.5.3 φ values** — no primary source was obtained in this session. The φ = 0.75/0.70 Condition A/B split for concrete modes and 0.75/0.65 for ductile/brittle steel tension are widely quoted but must be verified against ACI 318-19 before publication.
4. **ACI 318M SI coefficients** (kc = 10/7, Nsb = 13, Vb = 0.6/3.7, Nb = 3.9·hef^(5/3)) were **derived by unit conversion** from the verified inch-pound values and reconcile to within 1 %, but were not read from ACI 318M-19 directly.
5. **k₆ = 0.5 efficiency factor for stirrup-type shear reinforcement** exists in CEN/TS 1992-4-2 Eq. (44). Whether EN 1992-4 §7.2.2.6 retains it was not confirmed.
6. **γ_Ms values** are Nationally Determined Parameters. The recommended values (1.4 / 1.25) were confirmed from the Würth manual, IDEA StatiCa and the Danish NA structure. **The Danish NA uses 1.5 / 1.35.** State which NA applies.
7. **Israeli adoption of EN 1992-4** was not confirmable from SII sources in this session — treat as an open item.
8. Extractions from **pdfcoffee.com** were discarded as unreliable (the returned "formulas" were internally inconsistent and did not match verified sources). Nothing from that source is used above.

---

## 14. SOURCES ACTUALLY USED

**Primary / code documents**
1. *DIN EN 1992-4:2019-04 (EN 1992-4:2018) — official preview, pp. 62–72* (clauses 7.2.1.8–7.2.2.5, Formulae 7.28–7.46, Table 7.2, Figures 7.7–7.14) — https://www.normsplash.com/Samples/DIN/189696674/DIN-EN-1992-4-2019-en-2.pdf
2. *DIN EN 1992-4:2019-04 — official preview, front matter* (supersession statement, National Foreword items a)–v), full Contents with clause page numbers) — https://www.normsplash.com/Samples/DIN/189696674/DIN-EN-1992-4-2019-en.pdf
3. *DD CEN/TS 1992-4-1:2009 — General* (full text: §4.4.3 partial factors, γ_inst tiers, Table 1 hole clearance, §5.2.3 shear distribution and lever arm, §8 seismic, Annex A) — https://www.sefindia.org/forum/files/DD_CEN_TS_1992-4-1-2009_161.pdf
4. *DD CEN/TS 1992-4-2:2009 — Headed Fasteners* (full text: cone, pull-out Eq.2, splitting Eqs.17–19, blow-out Eqs.20–27, supplementary reinforcement Eqs.28–29 & 44–45, edge failure Eqs.33–43, interaction Eqs.46–49) — https://www.sefindia.org/forum/files/DD_CEN_TS_1992-4-2-2009_144.pdf
5. *DS/EN 1992-4 DK NA:2024 — Danish National Annex* (Table 4.1 DK NA, all NDPs, γ values, σ_adm guidance) — https://www.bygningsreglementet.dk/media/ssqfdij2/dsen-1992-4-eurocode-2-betonkonstruktioner-del-4-dimensionering-af-befaestelsesdele-til-anvendelse-i-beton.pdf
6. *NA to BS EN 1992-4:2018 — UK National Annex* (confirms recommended values adopted throughout) — https://pdfcoffee.com/na-to-bs-en-1992-4-2018-pdf-free.html

**Manufacturer technical documents (ETA-based)**
7. *Hilti PROFIS Engineering 3.0.84 anchor design report* — HIT-HY 200-A + HAS-U 5.8 M16, ETA 11/0493, EN 1992-4 with printed equation numbers (7.1–7.7, 7.13–7.21, 7.34–7.48), real γ, k, τ, ψ values — https://files-ask.hilti.com/original/n3/n3rqw3rbpk.pdf
8. *Hilti HSA Expansion Anchor product data, ETA-11/0374* (hef, h_min, s_cr,N, c_cr,N, s_cr,sp, c_cr,sp, d₀, d_f,max, T_inst tables; hef application thresholds; γ_M,fi) — https://productdata.hilti.com/APQ_HC_RAW/ASSET_DOC_2027424.pdf
9. *Peikko HPM® Rebar Anchor Bolt Technical Manual, 06/2024* (ETA-02/0006; k_cr,N = 8.9, k_ucr,N = 12.7, γ_Ms = 1.4, N_Rk,s/N_Rk,p/V⁰_Rk,s/M⁰_Rk,s tables, k₈ = 2.0, k₁₁ = 2/3, Eq. C.9 seismic interaction, Annex A concrete-cone reinforcement Table 25, EN 1992-4 Table 6.1 hole diameters) — https://www.peikko.com (doc: HPM Rebar Anchor Bolt, Version PEIKKO GROUP 06/2024)
10. *Hilti "Design of large anchorages — Supplementary reinforcement", BU Anchors Technical Services, 03.12.2013* (60 kN threshold, f_d,av,re = f_yd − σ_d, N_Rd,a,i formula, f_bd = 0.315·f_ck^(2/3), 0.75·c₁ rule, 4Ø/10Ø minima) — https://files-ask.hilti.com/original/ht/... (Hilti technical note, "Large Anchorages Design")
11. *Würth "Design Principles — Anchors", Chapter 1* (Table 2 = EN 1992-4 Table 4.1 partial factors in full; failure-mode list; concrete class table; h ≥ 80 mm; k₉/k_cr/k_ucr; verification tables 4–6) — https://www.wurth.co.uk/media/downloads/pdf/anchors_2/literature/adm/01_Design_Principles.pdf
12. *Dr. Jochen Buhler (Würth), "EN 1992-4 (Eurocode 2): Design of concrete structures — Part 4", TVZ Zagreb, 2023* (history chart; Table 7.1; k₁ = 7.7/11.0; §4.7 cracked/uncracked text; σ_R = 3 N/mm²; γ_inst → γ_Mc table; N⁰_Rk,c table vs hef; A_c,N construction figures; Fuchs/Eligehausen/Breen 1995) — https://www.tvz.hr/wp-content/uploads/2023/12/2311-EN1992-4-new.pdf
13. *fischer white paper, "The importance of the new EN 1992-4 standard for the design of fastenings in concrete"* (5 key changes; k₁ 7.2→7.7, 10.1→11.0; k₉ unchanged 1.7/2.4; ψ_sus and the 2006 Boston tunnel collapse; interaction restructuring; C12/15–C90/105; 9-fastener groups) — fischer Group white paper "EN 1992-4"

**Software / implementation references (secondary, used for cross-checking)**
14. *IDEA StatiCa, "Code-check of anchors (EN)"* (k₁ 8.9/12.7 and 7.7/11.0; k₂ 7.5/10.5; k₅ 8.7/12.2; k₆, k₈, k₉; all ψ formulas; γ_Ms formulas; interaction exponents) — https://www.ideastatica.com/support-center/check-of-anchors-according-to-eurocode
15. *IDEA StatiCa, "Code-check of anchors (AISC/ACI)"* (ACI equation set, ψ factors, kcp, φ) — https://www.ideastatica.com/support-center/design-check-of-anchors-according-to-aisc

**ACI / AISC**
16. *Williams Form Engineering, "ACI 318 — Anchoring to Concrete, Design Considerations"* (kc = 24 cast-in / 17 post-installed; ANco = 9hef²; AVco = 4.5ca1²; Nsa, Ncb, Ncbg, Vsa, Vcb, Vb 7 & 9 equations; le rules; Vcp; interaction 17.8.3 ≤ 1.2; Ψc 1.25/1.40) — https://www.williamsform.com/wp-content/uploads/2025/08/ACI_318_Anchoring_to_Concrete.pdf
17. *SkyCiv, "ACI Anchor Checks — Understanding Anchor Failure Modes"* (Nsb = 160·ca1·√Abrg·λ·√f'c; Nsbg group form; Np = 8·Abrg·f'c; adhesive bond Na/Nag; design remedies per mode) — https://skyciv.com/docs/skyciv-base-plate-design/aci-anchor-checks-for-beginners-understanding-anchor-failure-modes-and-how-to-fix-them/
18. *AISC Steel Design Guide 1, "Base Plate and Anchor Rod Design", 2nd Edition* (Fisher & Kloiber) — §2.4 welding, §2.5 anchor rod material & F1554, §2.6 holes/washers, §2.7 sizing & layout, §2.8 setting tolerances (AISC COSP §7.5.1 vs ACI 117 §2.3), §2.9 erection methods
19. *ASCC Position Statement #14, "Anchor Bolt Tolerances"* (contractor-side view of the AISC/ACI tolerance conflict)

**Standards-status / background**
20. *fischer / DesignFiX, "Design of fastenings based on EN 1992-4"* (10.1→11.0, 7.2→7.7 confirmation; ψ_sus and ψ_M,N introduction) — https://www.designfix.de/en-1992-4/
21. *Hilti (UK), "Eurocode 2 – Part 4 (I.S. EN 1992-4:2018)"* (C12/15–C90/105; ψ_M,N max factor 2; splitting omission thickness 2hef → h_min; grout ≤40 mm & ≤5d rule; edge-reinforcement change) — https://www.hilti.co.uk/engineering/article/eurocode-2-part-4-is-en-1992-42018/9elckf
22. *CEN/TC 250 / JRC, "The second-generation Eurocodes: key changes and benefits"* (prEN 1992-4:2026, distribution to NSBs by 30 March 2026) — https://wrap.warwick.ac.uk/id/eprint/194783/2/JRC144386_01.pdf
23. *Fastener + Fixing Magazine, "Design of fastenings for use in concrete (EN 1992-4) – publication and the implication for anchor manufacturers and consumers"* — https://fastenerandfixing.com/construction-fixings/design-of-fastenings-for-use-in-concrete-en-1992-4-publication-and-the-implication-for-anchor-manufacturers-and-consumers/
24. *EN 1992-4:2018 catalogue entry, iTeh Standards* (scope, approval date, supersession) — https://standards.iteh.ai/catalog/standards/cen/a8d47a68-f072-4eed-81af-5f9e7364eb84/en-1992-4-2018

**Cited within the above but not directly consulted:** Fuchs, Eligehausen & Breen (1995), *Concrete Capacity Design (CCD) Approach for Fastening to Concrete*, ACI Structural Journal — the 519-test-series basis of the whole method; Eligehausen, Mallée & Silva, *Anchorage in Concrete Construction*, Ernst & Sohn, 2006 — the standard reference textbook. **Both should be obtained and cited directly in the final report.**