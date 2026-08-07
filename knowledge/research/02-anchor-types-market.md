# ANCHORAGE OF STEEL TO CONCRETE — TECHNICAL BRIEF
### Raw material for an engineering report · Eretz Barzel · English source text for Hebrew translation

---

## PART 1 — THE ENGINEERING PRINCIPLES (the "WHY", for a fabricator)

### 1.1 An anchor is a load path with five possible weak links, not a bolt

A fabricator instinctively thinks "the bolt is M24, grade 8.8, so it's good for X kN." For anchorage to concrete this is almost always wrong. The steel is usually **not** the weakest link. Both modern codes (EN 1992-4:2018 and ACI 318-19 Ch.17) require the designer to check **every** failure mode and take the smallest:

**In tension:**
| # | Failure mode | Physical description |
|---|---|---|
| 1 | Steel failure | The rod necks and breaks. The *only* ductile mode. |
| 2 | Concrete cone (breakout) | A cone/pyramid of concrete tears out, apex at the embedded head, ~35° half-angle. Brittle. |
| 3 | Pull-out | The anchor slides out: the head crushes the concrete locally (cast-in), or the bond/expansion slips (post-installed). |
| 4 | Splitting | The member splits along the anchor line — happens when edges/spacings/member thickness are small. |
| 5 | Concrete blow-out | Near a free edge with **deep** embedment, the concrete blows out sideways at the head level while the surface stays intact. Only relevant when `c < 0.5·h_ef`. |

**In shear:**
| # | Failure mode | Physical description |
|---|---|---|
| 6 | Steel shear (with or without lever arm) | Bolt shears; if there is a grout gap it also bends. |
| 7 | Concrete pry-out | Short stiff anchor kicks a spall out of the concrete **behind** it. |
| 8 | Concrete edge failure | A half-cone of concrete breaks out **towards the free edge**. Usually the governing mode near edges. |

**The single most important message for the shop and for the site:** modes 2, 4, 5, 7 and 8 are all controlled by **geometry** — embedment depth, edge distance, spacing, member thickness — not by bolt grade. Upgrading from 8.8 to 10.9 buys nothing if the concrete cone governs.

### 1.2 Why embedment depth is worth more than diameter

The concrete cone resistance grows with `h_ef^1.5`. Doubling `h_ef` multiplies cone capacity by 2.83. Doubling the *bolt diameter* changes the cone capacity by **zero**. This is the single most counter-intuitive fact for a detailer.

### 1.3 Cracked vs uncracked concrete — the assumption that halves your capacity

Reinforced concrete in a tension zone **is cracked** at service load (that is what the rebar is for). A crack passing through the anchor destroys the radial confinement the anchor relies on.

- EN 1992-4 concrete cone factor: `k1 = 12.7` (uncracked) vs `8.9` (cracked) for cast-in headed → **–30 %**
- Post-installed straight fasteners: `11.0` vs `7.7` → **–30 %**
- ACI 318-19: `ψ_c,N = 1.25` (cast-in, uncracked) / `1.4` (post-installed, uncracked), else 1.0

For expansion anchors the loss is worse than the formula suggests: a crack opening under load **releases the expansion force**. This is why "suitable for cracked concrete" is a *qualification* (a tested property in the ETA / ESR), not a calculation. An anchor without cracked-concrete qualification must not be used in a tension zone, in a seismic zone, or anywhere fatigue/crack cycling is expected.

### 1.4 Why cast-in beats post-installed on capacity, and loses on tolerance

| | Cast-in | Post-installed |
|---|---|---|
| Load transfer | Mechanical bearing on a head — no reliance on the drilled-hole surface | Friction (expansion), keying (undercut/screw), or bond (adhesive) |
| Concrete disturbance | None | Drilling damages the hole wall; dust/water destroys bond |
| Best `k1` (EN 1992-4) | 8.9 / 12.7 | 7.7 / 11.0 |
| Tolerance | Poor — bolts are cast blind, before the steel exists | Excellent — drilled to the actual steel |
| Installation quality risk | Low | **High** — hole cleaning is the #1 failure cause for adhesives |
| Deep embedment | Easy | Limited (`h_ef ≤ 20·d` for bonded) |

The whole industry of anchor products exists to resolve this one tension.

### 1.5 The grout gap: why a 30 mm gap can halve your shear capacity

When a base plate sits on grout/mortar, the shear is applied **with a lever arm**. The bolt is no longer in pure shear — it bends. EN 1992-4:

```
V_Rk,s,M = α_M · M_Rk,s / l_a
l_a = 0.5·d_nom + t_mortar + 0.5·t_baseplate
M_Rk,s = M⁰_Rk,s · (1 − N_Ed / N_Rd,s)
M⁰_Rk,s = 1.2 · W_el · f_uk ,   W_el = π·d³/32
α_M = 2.0 (full restraint at both ends), 1.0 (free rotation at the fixture)
```
Note the second term: **any simultaneous tension directly reduces the bending resistance.** This is why grout gaps must be kept small, why a shear lug/nib is preferred for heavy shear, and why "the bolt is grade 8.8" is not an answer.

---

## PART 2 — THE TWO REGULATORY WORLDS

### 2.1 Europe (the relevant one for Israel)

**Design code:** `EN 1992-4:2018` — *Eurocode 2, Part 4: Design of fastenings for use in concrete*. Published July 2018; replaced CEN/TS 1992-4 (2009), ETAG 001 Annex C (1997), EOTA TR 029 (bonded, 2007) and EOTA TR 045 (seismic, 2013).

Scope limits worth knowing:
- Applies to `h_ef ≥ 40 mm` (EN 1992-4 §1 / EOTA TR 047 §2.2)
- `f_uk ≤ 1000 N/mm²` for the anchor steel
- Covers **cast-in headed fasteners, anchor channels, and post-installed fasteners holding a valid ETA**
- Static, quasi-static, fatigue, seismic and fire

**Product qualification chain:**
```
EAD (European Assessment Document)  →  testing at a notified Technical Assessment Body
      ↓
ETA (European Technical Assessment)  →  CE marking + Declaration of Performance
      ↓
EN 1992-4 design, using the characteristic values printed in the ETA
```
Key EADs:
- **EAD 330232-00-0601** — Mechanical fasteners for use in concrete (wedge, drop-in, undercut, screw)
- **EAD 330499-00-0601** — Bonded fasteners
- **EAD 330008** — Anchor channels
- **EOTA TR 047** — Design of anchor channels (supplement to EN 1992-4)
- **EOTA TR 082 (2024-04)** — Design of bonded fasteners in concrete under fire

**Critical practical point:** EN 1992-4 contains almost no product data. Every `k1`, `τ_Rk`, `N_Rk,s`, `s_min`, `c_min`, `h_min` comes from the ETA of the specific product. **An anchor without an ETA cannot be designed to EN 1992-4.**

### 2.2 USA

- **Design:** ACI 318-19 Chapter 17 (was Appendix D up to ACI 318-11)
- **Qualification:** ACI 355.2 (mechanical) / ACI 355.4 (adhesive)
- **Acceptance criteria:** ICC-ES **AC193** (mechanical + screw anchors), **AC308** (adhesive), **AC446** (headed cast-in specialty inserts)
- **Output:** an **ESR** (Evaluation Service Report) from ICC-ES

### 2.3 Israel

Verified: **ת"י 1225 חלק 1.1** (structural steel design) was re-published June 2023, running in parallel for 3 years with the previous 1998/2009 version. Beyond that, I could not verify from public sources a specific Israeli standard that supersedes EN 1992-4 for anchor design.

**Honest position for the report:** In Israeli practice the anchor products themselves are almost universally supplied with a European **ETA** (Hilti, fischer, Würth, Peikko, Leviat/Halfen all market in Israel under ETA), and the design is performed to **EN 1992-4** using the manufacturer's software (Hilti PROFIS Engineering, fischer FiXperience, Peikko Designer). US-sourced products carry an ICC-ES ESR and are designed to ACI 318. **This should be confirmed with the מכון התקנים / the project's engineer of record rather than asserted.** Note also that Israel is a seismically active country (EN 1998 / ת"י 413 territory), so the C1/C2 discussion in Part 7 is directly relevant, not academic.

---

## PART 3 — ALL DESIGN FORMULAS

### 3.1 EN 1992-4:2018 — Tension

**Partial safety factors (EN 1992-4 Table 4.1)**
```
γ_Ms  = 1.2 · (f_uk / f_yk)  ≥ 1.4                       (steel, tension)
γ_Ms  = 1.0 · (f_uk / f_yk)  ≥ 1.25   for f_uk ≤ 800 MPa and f_yk/f_uk ≤ 0.8   (steel, shear)
      = 1.5                          otherwise
γ_Mc  = γ_c · γ_inst                                     (all concrete modes)
γ_c   = 1.5
γ_inst (=γ₂) = 1.0 high installation safety
             = 1.2 normal installation safety
             = 1.4 low but still acceptable   [EOTA TR 029 §3.2.2.1; tension only, =1.0 in shear]
γ_Mp = γ_Msp = γ_Mc
```

**(1) Steel failure — EN 1992-4 §7.2.1.3**
```
N_Rd,s = N_Rk,s / γ_Ms          N_Rk,s = c · A_s · f_uk
```
`A_s` = tensile stress area; `c` = reduction for cut threads (EN 1993-1-8 §3.6.1(3)).

**(2) Concrete cone breakout — §7.2.1.4**
```
N_Rk,c = N⁰_Rk,c · (A_c,N / A⁰_c,N) · ψ_s,N · ψ_re,N · ψ_ec,N · ψ_M,N

N⁰_Rk,c = k1 · √f_ck · h_ef^1.5          [N, MPa, mm]

k1 = 8.9  cracked   / 12.7 uncracked   — cast-in headed fasteners
k1 = 7.7  cracked   / 11.0 uncracked   — post-installed straight fasteners (typical; ETA governs)

c_cr,N = 1.5 · h_ef        s_cr,N = 2 · c_cr,N = 3 · h_ef
A⁰_c,N = s_cr,N² = 9 · h_ef²

ψ_s,N  = 0.7 + 0.3 · (c / c_cr,N) ≤ 1.0          c = smallest edge distance
ψ_re,N = 0.5 + h_ef / 200 ≤ 1.0                  shell-spalling (dense surface reinforcement)
ψ_ec,N = 1 / (1 + 2·e_N / s_cr,N) ≤ 1.0          eccentricity of the tension resultant
ψ_M,N  = 2 − z / (1.5·h_ef) ≥ 1.0                compression benefit under bending
         = 1.0 if c < 1.5h_ef, or C/T < 0.8, or z/h_ef ≥ 1.5
```

**(3) Pull-out of cast-in headed fasteners — §7.2.1.5**
```
N_Rk,p = k2 · A_h · f_ck

k2 = 7.5  cracked  /  10.5 uncracked
A_h = (π/4)(d_h² − d²)         circular head/washer
A_h = a_wp² − (π/4)·d²         square plate washer
constraint: d_h ≤ 6·t_h + d    (the head must be thick enough to be rigid)
```
Symbols: `d_h` = head/washer outside diameter, `d` = shank diameter, `t_h` = head thickness, `a_wp` = plate washer side.

**(4) Combined pull-out + concrete cone, bonded fasteners — §7.2.1.6 (from EOTA TR 029 §5.2.2.3)**
```
N⁰_Rk,p = π · d · h_ef · τ_Rk            [N, mm, MPa]

N_Rk,p = N⁰_Rk,p · (A_p,N / A⁰_p,N) · ψ_s,Np · ψ_g,Np · ψ_ec,Np · ψ_re,Np

s_cr,Np = 20 · d · (τ_Rk,ucr / 7.5)^0.5  ≤ 3 · h_ef      [mm]   (τ_Rk,ucr for C20/25)
c_cr,Np = s_cr,Np / 2
A⁰_p,N  = s_cr,Np²
```
Embedment range: **min `h_ef` (never less than 4d and 40 mm) to `20·d`** — EOTA TR 029 §1.

**(5) Pull-out of hooked bolts** — **no formula exists in EN 1992-4.** See Part 5.

**(6) Splitting — §7.2.1.7**
Not required if `c ≥ 1.5·h_ef` (single) or `c ≥ 1.8·h_ef` (group) **and** `h ≥ h_min`; or if supplementary reinforcement limits `w_k ≤ 0.3 mm`. (As stated in Peikko HPM Technical Manual notes 2 & 3.)

**(7) Concrete blow-out — §7.2.1.8.** Only required when `c < 0.5·h_ef`.
```
N⁰_Rk,cb = k5 · c1 · √A_h · √f_ck        k5 = 8.7 cracked / 12.2 uncracked
A⁰_c,Nb  = (4·c1)²

ψ_s,Nb  = 0.7 + 0.3 · c2/(2·c1) ≤ 1                      (7.28)
ψ_g,Nb  = √n + (1 − √n)·s2/(4·c1) ≥ 1,  with s2 ≤ 4·c1   (7.29)
ψ_ec,Nb = 1 / (1 + 2·e_N/(4·c1))                         (7.30)
```

**(8) Supplementary reinforcement — §7.2.1.9**
```
N_Rk,re = Σ A_s,re,i · f_yk,re            with f_yk,re ≤ 600 N/mm²

N⁰_Rd,a = (l1 / (α1·α2)) · π · Ø · f_bd  ≤  A_s,re · f_yk,re / γ_Ms,re      (7.33)
```
Detailing (§7.2.2.2): ribbed bars, **Ø ≤ 16 mm**, within **0.75·c1** of the fastener, anchorage length in the breakout body **min l1 = 10Ø** (straight) or **4Ø** (hooked/bent/looped).

### 3.2 EN 1992-4:2018 — Shear

```
(1) Steel, no lever arm — §7.2.2.3.1
    V⁰_Rk,s = k6 · A_s · f_uk
    k6 = 0.6 for f_uk ≤ 500 MPa ; 0.5 for f_uk > 500 MPa
    × 0.8 if h_ef/d_nom < 5 AND concrete class < C20/25

(2) Steel with lever arm — §7.2.2.3.2   (see §1.5 above)

(3) Concrete pry-out — §7.2.2.4
    V_Rk,cp = k8 · N_Rk,c
    k8 = 1 for h_ef < 60 mm ; k8 = 2 for h_ef ≥ 60 mm

(4) Concrete edge failure — §7.2.2.5
    V⁰_Rk,c = k9 · d_nom^α · l_f^β · √f_ck · c1^1.5
    k9 = 1.7 cracked / 2.4 uncracked
    α = 0.1·(l_f/c1)^0.5      β = 0.1·(d_nom/c1)^0.2
    l_f = min(h_ef, 12·d_nom)                    for d_nom ≤ 24 mm
    l_f = min[h_ef, max(8·d_nom, 300)]           for d_nom > 24 mm
    A⁰_c,V = 4.5 · c1²

    ψ_s,V  = 0.7 + 0.3·c2/(1.5·c1) ≤ 1.0
    ψ_h,V  = (1.5·c1 / h)^0.5 ≥ 1.0
    ψ_ec,V = 1 / (1 + 2·e_V/(3·c1)) ≤ 1.0
    ψ_α,V  = 1 / √[cos²α_V + (0.5·sin α_V)²] ≥ 1.0
    ψ_re,V = 1.0 (no edge reinforcement) — higher with dense edge reinforcement
```

### 3.3 EN 1992-4 — Interaction

```
Steel    (N_Ed/N_Rd,s)²    + (V_Ed/V_Rd,s)²    ≤ 1.0      [Table 7.3]
Concrete (N_Ed/N_Rd,i)^1.5 + (V_Ed/V_Rd,i)^1.5 ≤ 1.0      [Eq. 7.55]
```
`i` = the governing concrete mode in each direction.

### 3.4 EN 1992-4 — Anchor channels (EOTA TR 047)

Same cone equation but the **channel-specific critical spacing is different**:
```
N⁰_Rk,c = k1 · √f_ck · h_ef^1.5          k_cr,N / k_ucr,N from the ETA

s_cr,N = 2 · (2.8 − 1.3·h_ef/180) · h_ef  ≤ 3·h_ef        (TR 047 Eq. 7.8)

ψ_ch,s,N = 1 / [ 1 + Σ (1 − s_i/s_cr,N)^1.5 · (N_i/N⁰) ]   ≤ 1.0   (Eq. 7.7)
```
Note the consequence: for `h_ef < 180 mm`, `s_cr,N = 3h_ef`; **for `h_ef > 180 mm` the formula gives less than 3h_ef** — deep channel anchors interact less than isolated anchors, because the channel redistributes.

### 3.5 ACI 318-19 Chapter 17

```
Steel, tension:    N_sa = A_se,N · f_uta       f_uta ≤ min(1.9·f_ya, 125 ksi)

Concrete breakout: N_cbg = (A_Nc/A_Nco)·ψ_ec,N·ψ_ed,N·ψ_c,N·ψ_cp,N·N_b

   N_b = k_c · λ_a · √f'c · h_ef^1.5           for h_ef < 11 in.
   N_b = 16 · λ_a · √f'c · h_ef^(5/3)          for 11 in. ≤ h_ef ≤ 25 in.

   k_c = 24  CAST-IN anchors
   k_c = 17  POST-INSTALLED anchors (may be raised by ACI 355.2 testing)

   A_Nco = 9·h_ef²
   ψ_c,N = 1.25 cast-in / 1.4 post-installed, uncracked ; else 1.0
   ψ_ed,N = 0.7 + 0.3·c_a,min/(1.5·h_ef) ≤ 1.0

Pull-out, HEADED:  N_p = 8 · A_brg · f'c
Pull-out, HOOKED:  N_p = 0.9 · f'c · e_h · d_a      valid only for 3d_a ≤ e_h ≤ 4.5d_a
                   N_pn = ψ_c,P · N_p ,   ψ_c,P = 1.4 uncracked / 1.0 cracked
                   φ = 0.70
```
> **⚠ Frequently mis-quoted.** Several online sources state `k_c = 17` for cast-in and `24` for post-installed. **This is reversed.** AISC Design Guide 1 (2nd ed.), pp. 20 and 36, uses `24` for cast-in headed anchor rods explicitly (`N_cbg = φψ₃·24·√f'c·h_ef^1.5`). Cast-in gets the higher factor because it is not installed into a damaged drilled hole.

**Geometric limits of the CCD model (AISC DG1 §3.2.2):** valid for anchor diameters **≤ 2 in. (≈50 mm)** and tensile embedment **≤ 25 in. (≈635 mm)**. Breakout cone at **~34–35°** (1 : 1.5 slope), idealised as square in plan.

**ACI definition of a hooked bolt** (Caltrans BDM 5.51 glossary, quoting ACI 318): *"cast-in anchor anchored mainly by bearing of the 90-degree bend (L-bolt) or 180-degree bend (J-bolt) against the concrete, at its embedded end, and having a minimum `e_h` equal to `3d_a`."*

---

## PART 4 — THE CAST-IN FAMILY, TYPE BY TYPE

### 4.1 Headed anchor rods (hex head / nut + washer) — **the reference standard**

**Load transfer:** pure mechanical bearing of the head/nut on concrete. No dependence on bond, friction, or hole condition. Predictable, code-covered, and the only cast-in type for which ACI and EN both give complete pull-out models.

**Material — USA:** `ASTM F1554`, three grades, colour-coded (AISC DG1 §2.5):
| Grade | Colour code | Use |
|---|---|---|
| **36** (250 MPa) | **Blue** | Default. Most common. Weldable without supplement. |
| **55** (380 MPa) | **Yellow** | Large tension from moment connections or uplift. Order with **supplement S1** (carbon equivalent ≤ 0.45 %) if welding may be needed. |
| **105** (725 MPa) | **Red** | Special high-strength; use only when Gr 36/55 in a larger diameter is impossible. |

Threads: UNC, Class 2a. **All** nuts should be **heavy hex** (ASTM A563 Grade A, or DH for Grade 105), especially with oversize base plate holes.

**Material — Europe:** threaded rod grade 4.6 / 5.6 / 5.8 / 8.8 to EN ISO 898-1; nuts EN ISO 4032 class 8; washers S355J2+N to EN 10025-2; hot-dip galvanising to EN ISO 10684.

**Bearing areas** (AISC DG1 Table 3.2) — for `N_p = 8·A_brg·f'c`:
| Rod Ø (in) | Rod area A_r (in²) | Bearing area A_brg (in²) |
|---|---|---|
| 5/8 | 0.307 | 0.689 |
| 3/4 | 0.442 | 0.906 |
| 7/8 | 0.601 | 1.22 |
| 1 | 0.785 | 1.50 |
| 1-1/8 | 0.994 | 1.81 |
| 1-1/4 | 1.23 | 2.24 |
| 1-1/2 | 1.77 | 3.13 |

**AISC DG1's warning on plate washers** (§2.5, verbatim intent): *"The addition of plate washers or other similar devices does not increase the pullout strength of the anchor rod and can create construction problems by interfering with reinforcing steel placement or concrete consolidation under the plate. Thus, it is recommended that the anchorage device be limited to either a heavy hex nut or a head on the rod. As an exception, the addition of plate washers may be of use when high-strength anchor rods are used or when concrete blowout could occur."*

This is worth quoting in the report because fabricators routinely weld big square plates on the embedded end believing it helps. It only helps when:
- the rod is high-strength (Gr 105 / 10.9) and a hex nut cannot develop it, **or**
- concrete **blow-out** near an edge is the concern (increasing `A_h` raises `N_Rk,cb`).

Otherwise it obstructs rebar and traps voids under the plate — which *reduces* real capacity.

**Typical embedment (`h_ef`) ranges** — from the worked examples and product data below:
| Rod / thread | Practical `h_ef` for full steel development |
|---|---|
| M16 / 5/8" | 150–250 mm |
| M20 / 3/4" | 200–330 mm |
| M24 / 7/8"–1" | 250–400 mm |
| M30 | 330–520 mm |
| M39 | 500–700 mm |
| M52 | 900–1000 mm |

### 4.2 Headed studs (welded)

Cylindrical, unthreaded, drawn-arc stud-welded to a plate. Material grades per **EN ISO 13918** (Europe) / AWS D1.1 Chapter 7 Type B (USA). The shank has **zero design bond** — the entire load goes through the head. Same `N_Rk,p = k2·A_h·f_ck` model. Typical `d` = 10–25 mm, `h_ef` = 50–300 mm. Used for embed plates, composite action, and shear transfer in embedded plates.

### 4.3 Anchor rods with a plate washer / anchor plate at the embedded end

Same equations as 4.1 with `A_h = a_wp² − (π/4)d²`. Governed by the rigidity constraint `d_h ≤ 6·t_h + d`: **a thin, wide plate does not work** — it dishes and the bearing pressure concentrates at the shank. For a 40 mm rod, a plate washer effective diameter of 100 mm requires `t_h ≥ (100−40)/6 = 10 mm` minimum.

### 4.4 Threaded rod with nut + washer embedded — the "poor man's headed anchor"

Functionally identical to 4.1 provided the nut is a **heavy hex** nut and, preferably, tack-welded or double-nutted so it cannot back off during concrete placement. This is the detail AISC DG1 explicitly recommends as the replacement for hooked rods.

### 4.5 Proprietary cast-in anchor bolt systems — **Peikko HPM® / PPM® / HULCO®**

The modern European answer for column bases and machine bases. A **forged head** on a **ribbed reinforcing bar** with a rolled thread at the top.

**Peikko HPM® L Rebar Anchor Bolt** — ETA-02/0006, bar B500B to EN 10080, nuts class 8, washers S355J2+N. Manufacturing tolerance: length ±10 mm, thread length +5/−0 mm.

*Geometry (Peikko HPM Technical Manual, 06/2024, Tables 6 and 7):*
| | HPM 16 L | HPM 20 L | HPM 24 L | HPM 30 L | HPM 39 L |
|---|---|---|---|---|---|
| Thread | M16 | M20 | M24 | M30 | M39 |
| Thread stress area (mm²) | 157 | 245 | 352 | 561 | 976 |
| Bar Ø (mm) | 16 | 20 | 25 | 32 | 40 |
| Total length L (mm) | 280 | 350 | 430 | 500 | 700 |
| Thread length A (mm) | 140 | 140 | 170 | 190 | 200 |
| **h_ef (mm)** | **165** | **223** | **287** | **335** | **502** |
| Head Ø d_h (mm) | 38 | 46 | 55 | 70 | 90 |
| Head height k (mm) | 10 | 12 | 13 | 15 | 18 |
| Washer (mm) | Ø40-6 | Ø44-6 | Ø56-6 | Ø65-8 | Ø90-10 |
| **c_min (mm)** | **50** | **70** | **70** | **100** | **130** |
| **s_min (mm)** | **80** | **100** | **100** | **130** | **150** |
| **h_min (mm)** | **260** | **320** | **385** | **435** | **605** |
| Weight (kg) | 0.7 | 1.2 | 2.2 | 4.1 | 9.2 |
| Colour code | Yellow | Blue | Grey | Green | Orange |

`h_min = h_ef + k + c_nom` with `c_nom = 85 mm` (for foundations cast directly against soil). **Verified for all five sizes.**

*Resistances (Table 10, C20/25):*
| | 16 L | 20 L | 24 L | 30 L | 39 L |
|---|---|---|---|---|---|
| `N_Rk,s` (kN) | 86.2 | 134.6 | 193.9 | 308.3 | 536.7 |
| `N_Rk,p` uncracked (kN) | 195.9 | 283.0 | 395.8 | 639.3 | 1072.1 |
| `N_Rk,p` cracked (kN) | 140.0 | 202.2 | 282.7 | 456.6 | 765.8 |
| `V⁰_Rk,s` (kN) | 43.1 | 67.3 | 96.9 | 154.2 | 268.3 |
| `M⁰_Rk,s` (kNm) | 0.183 | 0.356 | 0.616 | 1.236 | 2.837 |

Declared factors: `k_ucr,N = 12.7`, `k_cr,N = 8.9`, `s_cr,N = s_cr,sp = 3h_ef`, `c_cr,N = c_cr,sp = 1.5h_ef`, `γ_Mp = γ_Mc = 1.5`, `γ_Ms = 1.4`.
Concrete-grade increase factor `c` for `N_Rk,p`: **C25/30 → 1.25, C30/37 → 1.50, C35/45 → 1.75, C40/50 → 2.00, C45/55 → 2.25, C50/60 → 2.50** (i.e. exactly `f_ck/20`, consistent with `N_Rk,p ∝ f_ck`).

Displacements under tension (Table 11): short-term δ⁰_N = 0.3–0.6 mm; long-term δ∞_N = 0.6–1.2 mm; seismic C2: 1.1 mm (DLS) / 2.5 mm (ULS).

**Peikko PPM® High-Strength Anchor Bolt** (Technical Manual 10/2023) — for the heaviest bolted column connections:
| | PPM 30 L | PPM 36 L | PPM 39 L | PPM 45 L | PPM 52 L | PPM 60 L |
|---|---|---|---|---|---|---|
| c_min (mm) | 120 | 140 | 150 | 160 | 180 | 180 |
| s_min (mm) | 130 | 160 | 180 | 200 | 280 | 280 |
| **h_ef (mm)** | **522** | **568** | **692** | **777** | **905** | — |
| h_min (mm) | 620 | 665 | 790 | 875 | 1005 | — |
| k (mm) | 13 | 12 | 13 | 13 | 15 | 15 |

Note `h_ef` of **900 mm** for M52 — this is the scale at which cast-in systems live.

### 4.6 Anchor bolt assemblies with sleeves for adjustability

Two distinct techniques:

**(a) Debonding sleeve / foam tube over the upper shank.** Deliberately removes bond over a defined length so the bolt has a controlled **stretch length**. Essential for seismic ductility (ACI 318-19 §17.10.5.3(a)(iii): the ductile steel element needs a stretch length **≥ 8·d_a**). Peikko offers this as a standard accessory on HPM/PPM.

**(b) Post-installing the anchor bolt into a cast-in corrugated tube.** The tube is cast into the foundation; after the steel is erected and surveyed, the bolt is dropped in and grouted. Gives **large positional tolerance** with cast-in-class capacity.

*Peikko HPM in corrugated tubes (HPM Technical Manual Table 29):*
| Bolt | Tube Ø (mm) | h_ef (mm) | Tube length h_ef+50 | **N_Rd (kN)** | Foundation | Grout | Tolerance Δx (mm) |
|---|---|---|---|---|---|---|---|
| HPM 16 L | 100 | 165 | 215 | 62 | C20/25 | C25/30 | 31 |
| HPM 20 L | 100 / 125 | 223 | 273 | 96 | C20/25 | C25/30 | 27 / 39 |
| HPM 24 L | 100 / 125 | 287 | 337 | 139 | C20/25 | C25/30 | 22 / 35 |
| HPM 30 L | 125 / 150 | 335 | 385 | 220 | C20/25 | C25/30 | 27 / 40 |
| HPM 39 L | 150 / 180 | 495 | 545 | 383 | C20/25 | C25/30 | 30 / 45 |

Design shear stress between concrete and corrugated tube: **τ_cd = 2.7 N/mm²**. Grout: non-shrink, self-compacting, `f_ck,cube ≥ 25 N/mm²`, max aggregate 3–4 mm (8 mm if concrete).
**Limitations (important):** static and quasi-static only — **not seismic, not fatigue** — and this configuration is **not covered by ETA-02/0006**; it is manufacturer technical data for machinery anchorage.

### 4.7 Cast-in channels (Halfen / Leviat, Jordahl, Ancon)

A hot-rolled or cold-formed C-profile with welded/forged anchors, cast flush with the concrete face. A **T-head bolt** slides in and is turned 90°. Gives **continuous positional adjustment along the channel** — the only cast-in system that does.

**Halfen HTA-CE** (Technical Product Information B 13.1-E; ETA-09/0339):
- Profiles (width/height, mm): HTA 28/15, 29/20, 38/17, 38/23, **40/22**, 49/30, **50/30**, **52/34**, 53/34, **55/42**, **72/48**
- Bolts **HS** (plain) and **HSR** (with nib — takes load in the channel longitudinal direction), grade **8.8**
- HSR torque: **M16 = 200 Nm, M20 = 400 Nm**
- Materials: 1.0038 / 1.0044 carbon steel; **1.4404 / 1.4571** stainless
- Concrete range: **C12/15 to C90/105**
- Design per **CEN/TS 1992-4-3 → EOTA TR 047 → EN 1992-4**

*Fatigue — stress amplitude at N = 2×10⁶ cycles (Halfen B 13.1-E):*
| Profile / anchor config | Material | Δσ_F = F_o − F_u (kN), tension | Approved bolts |
|---|---|---|---|
| 29/20-B6, 29/20-Q | 1.0044 | 2.0 | M12, M16 |
| 38/23-B6, 38/23-Q | 1.4404 / 1.4571 | 2.4 | M16 |
| 40/22-B6, 40/22-Q | — | 2.0 | M16 |
| 50/30-B6, 50/30-Q | — | 2.4 | M16, M20 |
| 52/34-Q | 1.0038 | 7.0 | M20 |
| 53/34-B6, 53/34-Q | 1.4404 / 1.4571 | 4.0 (10) | M16, M20 |
| 55/42-Q | 1.0038 | 8.0 | M24 |
| HZA 64/44-Q/L | 1.0044 | 15.0 | M20, M24 |

**Verifications required (TR 047):** steel failure of the anchor, pull-out, **failure of the channel–anchor connection**, **local bending of the channel lips**, **channel bolt failure**, concrete cone, and concrete edge failure. The lip-bending and connection checks are unique to channels and are the usual governing modes — this is why channel capacity does not scale with bolt grade.

**When to choose a channel:** façade fixings, curtain walls, rail/crane fixings, MEP support runs, anywhere the final fixing position is unknown at concreting, and anywhere fatigue matters (channels have published fatigue data; almost no post-installed anchor does).

---

## PART 5 — WHY HOOKED (L / J) BOLTS ARE DISCOURAGED

### 5.1 The mechanism is wrong

A headed anchor transfers load by **bearing of a rigid head over an annular area**. A hooked bolt transfers load by **bearing of the shank against the inside of the bend** over a short strip — and the bend can **straighten**.

AISC Design Guide 1, 2nd ed., §3.2.2 states it plainly: *"Hooked anchor rods can fail by straightening and pulling out of the concrete. This failure is precipitated by a localized bearing failure of the concrete above the hook. A hook is generally not capable of developing the required tensile strength. Therefore, hooks should only be used when tension in the anchor rod is small."*

And §2.5: *"Hooked-type anchor rods have been extensively used in the past. However, hooked rods have a very limited pullout strength compared with that of headed rods or threaded rods with a nut for anchorage. Therefore, current recommended practice is to use headed rods or threaded rods with a nut for anchorage."*

### 5.2 ACI 318-19 restricts them arithmetically

```
HEADED:  N_p = 8 · A_brg · f'c
HOOKED:  N_p = 0.9 · f'c · e_h · d_a       valid only for   3·d_a ≤ e_h ≤ 4.5·d_a
```

Two independent restrictions bite:
1. **`e_h ≥ 3d_a`** is a *minimum* — shorter hooks are not permitted at all (ACI 318-19 §17.6.3.2.2).
2. **`e_h ≤ 4.5d_a`** is a *cap on the credit* — a longer hook earns **no additional pull-out capacity**. This is because the tested range only extends to 4.5d_a; beyond that the bend simply straightens.

### 5.3 The killer result: hooked bolts can never be ductile

Take the best possible case, `e_h = 4.5·d_a`:
```
N_p     = 0.9 · f'c · (4.5·d_a) · d_a  = 4.05 · f'c · d_a²
N_sa    = A_se · f_uta ≈ 0.75 · (π/4) · d_a² · f_uta = 0.589 · d_a² · f_uta

N_p / N_sa = 4.05·f'c / (0.589·f_uta) = 6.88 · f'c / f_uta      ← d_a CANCELS OUT
```

**The ratio is independent of diameter.** For typical materials:

| f'c (cylinder) | f_uta | N_p / N_sa |
|---|---|---|
| 25 MPa (C25/30) | 400 MPa (Gr 4.6 / F1554-36) | **0.43** |
| 25 MPa | 500 MPa (Gr 5.8 / F1554-55) | **0.34** |
| 25 MPa | 800 MPa (Gr 8.8) | **0.22** |
| 30 MPa (C30/37) | 500 MPa | 0.41 |

Applying the φ factors (φ = 0.70 pull-out vs 0.75 steel) makes it worse by a further ~7 %.

**Conclusion for the report:** *at its maximum permitted hook extension, an L- or J-bolt develops only 22 – 43 % of the tensile strength of its own shank, regardless of diameter.* Ductile (steel-governed) behaviour — which every seismic code demands — is **arithmetically impossible** with a hooked bolt.

### 5.4 EN 1992-4 simply does not cover them

This is the sharper European statement, and it is worth being precise about because it is different in kind from the ACI restriction:

- EN 1992-4 §7.2.1.5 gives pull-out **only** for fasteners with a defined **head bearing area `A_h`** (`N_Rk,p = k2·A_h·f_ck`). A hooked bar has **no `A_h`**.
- EN 1992-4's scope covers *cast-in headed fasteners, anchor channels, and post-installed fasteners holding an ETA*. **A plain hooked bar is none of these.**
- Therefore a hooked cast-in bolt has **no design route within EN 1992-4 at all**. It would have to be treated as a **reinforcing bar anchorage** under EN 1992-1-1 §8.4 (bond `f_bd` along the bar plus the hook factor `α1`), which is a *completely different mechanism* — bond over the full embedded length, valid only inside a properly reinforced member with satisfied bond conditions, and not valid for the concentrated-load, edge-affected, potentially-cracked situation a base plate creates.

So: **ACI 318 permits hooked bolts but penalises them so heavily they are useless for real loads. EN 1992-4 does not permit them at all.** Both conclusions point the same way.

### 5.5 What replaced them

| Old detail | Modern replacement | Why |
|---|---|---|
| L-bolt / J-bolt cast in | **Threaded rod + heavy hex nut** (+ washer) at the embedded end | Full `8·A_brg·f'c` / `k2·A_h·f_ck` bearing; ductile if `h_ef` adequate |
| Hooked bolt in a heavy base | **Hot-forged headed anchor rod** | Same, with a compact head |
| Hooked bolt for high-strength rod | **Rod + plate washer / anchor plate** | Raises `A_h` where a nut alone can't develop the rod |
| Hooked bolt in a precast/column base | **Peikko HPM® / PPM® ribbed anchor bolt with forged head** (ETA-02/0006) | ETA-covered, seismic-qualified, ductile by design |
| Hooked bolt for adjustable façade fixing | **Cast-in channel** (Halfen HTA, Jordahl JTA, Ancon) | Adjustable + fatigue data |
| Hooked bolt on an existing structure | **Bonded anchor** (Hilti HIT, fischer FIS) | Higher and more predictable capacity |

**One legitimate remaining use for hooks:** *supplementary/anchor reinforcement* — hairpins, stirrups and loops that surround the anchor and carry the breakout force back into the member. Here the hook is a **rebar anchorage detail**, not the anchor itself, and EN 1992-4 §7.2.2.2(4) explicitly allows `min l1 = 4Ø` for bars with a hook, bend or loop **provided the reinforcement encloses and contacts the fastener shaft**.

---

## PART 6 — THE POST-INSTALLED FAMILY, TYPE BY TYPE

### 6.1 Torque-controlled expansion anchors (wedge anchor / through-bolt)

**Mechanism.** A tapered cone bolt is pulled up into an expansion clip by tightening the nut to `T_inst`. The clip is pressed against the hole wall. Load is transferred by **friction** and by local keying into the concrete surface. Under load the bolt is pulled slightly further, the cone drives the clip harder — **follow-up expansion** — which is the property that makes an anchor cracked-concrete capable.

**Real data — fischer FAZ II** (ETA-05/0069, EAD 330232-00-0601 Option 1 for cracked concrete; also ICC-ES ESR-2948). Cone bolt `f_uk ≥ 1000 N/mm²`, nut ≥ class 8.

*Installation parameters (fischer Technical Product Information, status 07/2022):*
| | M6 | M8 | M8 | M10 | M10 | M12 | M12 | M16 | M16 | M20 | M24 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| **h_ef (mm)** | 40 | 35 | 45 | 40 | 60 | 50 | 70 | 65 | 85 | 100 | 125 |
| Drill depth h₁ (mm) | 51.5 | 49.5 | 59.5 | 57.0 | 77.0 | 68.5 | 88.5 | 87.5 | 107.5 | 130.0 | 158.5 |
| **Drill Ø d₀ (mm)** | 6 | 8 | 8 | 10 | 10 | 12 | 12 | 16 | 16 | 20 | 24 |
| d_cut,max hammer (mm) | 6.40 | 8.45 | 8.45 | 10.45 | 10.45 | 12.50 | 12.50 | 16.50 | 16.50 | 20.55 | 24.55 |
| Spanner SW (mm) | 10 | 13 | 13 | 17 | 17 | 19 | 19 | 24 | 24 | 30 | 36 |
| **T_inst (Nm)** | **8** | **20** | **20** | **45** | **45** | **60** | **60** | **110** | **110** | **200** | **270** |
| h_min (mm) | 81.5 | 80 | 89.5 | 87 | 107 | 100 | 118.5 | 140 | 140 | 170 | 206.5 |
| **c_min (mm)**, uncracked | 45 | 40 | 40 | 45 | 45 | 55 | 55 | 65 | 65 | 95 | 135 |
| **s_min (mm)**, uncracked | 35 | 40 | 40 | 40 | 40 | 50 | 50 | 65 | 65 | 95 | 100 |
| **c_min (mm)**, cracked | 45 | 40 | 40 | 45 | 45 | 55 | 55 | 65 | 65 | 85 | 100 |
| **s_min (mm)**, cracked | 35 | 35 | 35 | 40 | 40 | 50 | 50 | 65 | 65 | 95 | 100 |

Note the **variable embedment** for M8–M16 (two `h_ef` values per size) — a genuinely useful design lever: an M12 can be set at 50 mm or 70 mm depending on member thickness.

*Seismic design resistance, C20/25 cracked (fischer, EN 1992-4):*
| h_ef (mm) | 45 (M8) | 40 (M10) | 60 (M10) | 50 (M12) | 70 (M12) | 65 (M16) | 85 (M16) | 100 (M20) | 125 (M24) |
|---|---|---|---|---|---|---|---|---|---|
| `N_Rd,C1` (kN) | 3.1 | 5.2 | 5.3 | 7.2 | 10.7 | 10.7 | 16.0 | 20.4 | 28.5 |
| `N_Rd,C2` (kN) | — | 1.8 | 3.4 | 2.9 | 4.9 | 10.7 | 14.3 | 20.4 | — |

**Look at M12 @ h_ef=50: C1 = 7.2 kN but C2 = 2.9 kN — a 60 % drop for the same anchor.** This single comparison is the best argument in the whole brief for specifying the seismic category explicitly.

**Advantages:** cheapest per unit; instantly loadable (no cure time); simple visual verification (torque + protrusion mark); through-fixing possible.
**Limits:** requires expansion room → **large `c_min` and `s_min`**; generates splitting forces at installation; sensitive to hole diameter and to over/under-torquing; poor in fire; not usually suitable for hollow or very low-strength base material.
**Choose when:** medium loads, adequate edge distances, uncracked or cracked-qualified, non-seismic or C1, no fire requirement.
**Product families:** Hilti **HST3 / HST4 / HSL-4** (HSL4 = heavy-duty, ETA-19/0556 static+seismic, ETA-19/0858 fatigue; two embedment depths e.g. 65 and 80 mm; sizes M8–M24), Hilti **KB-TZ2** (US market, ICC-ES), fischer **FAZ II / FAZ II Plus** (FAZ II Plus: C1 **and** C2 for M10–M24, with or without the **FFD filling disc**), Würth **W-FAZ**, DeWalt/Powers **Power-Stud+ SD**.

### 6.2 Displacement-controlled expansion anchors (drop-in)

**Mechanism.** An internally threaded sleeve is dropped into the hole; a setting punch drives an internal plug/cone downward (or a cone upward) through a **fixed displacement**. The sleeve flares. Load transfer is by friction only, with **no follow-up expansion**.

**Consequence:** if a crack opens, the expansion force is lost and there is no mechanism to recover it. Drop-ins are therefore, as a family, **poor candidates for cracked concrete and for seismic**, and most are qualified for **uncracked concrete only**.

Other structural limitations:
- **Shallow `h_ef`** — the sleeve length is the embedment; typical 25–65 mm. `h_ef < 60 mm` also puts you in the `k8 = 1` pry-out band (EN 1992-4 §7.2.2.4), halving shear-related concrete capacity relative to deeper anchors.
- Set flush with the surface → the fixture can be removed and replaced (their main selling point).
- **Setting is unverifiable after the fact** unless the setting mark is visible; under-driving is the classic site defect.
- Typical range 1/4"–3/4" (M6–M16 equivalent). Simpson Strong-Tie DIAB: 1/4"–3/4"; HDIA: 1/4"–5/8"; allowable loads up to ~2,900 lb (12.9 kN) tension and 4,000 lb (17.8 kN) shear in 4,000 psi concrete.

**Choose when:** light, non-structural, uncracked, removable fixtures — suspended services, temporary works, ceiling hangers. **Do not use** for column bases, seismic restraint or anything life-safety critical.

### 6.3 Undercut anchors — **the highest-performance post-installed type**

**Mechanism.** A conical undercut is cut into the concrete at the base of the hole (either with a special tool or, in self-undercutting products, by the anchor itself during setting). The anchor's expansion segments then sit in a **positive mechanical interlock** — a form fit, like a cast-in head — not a friction fit.

**Why this matters:** because the load path is bearing rather than friction, undercut anchors:
- behave like cast-in headed anchors in **cracked concrete** (crack opening doesn't release them);
- generate **almost no splitting force at installation**, so they permit **smaller edge distances and spacings** than expansion anchors of equal capacity;
- give the best available **seismic C2** and **fatigue** performance of any post-installed type.

**Real data — Hilti HDA** (ETA-99/0009 static/seismic/fire; ETA-18/0974 fatigue; BZS D 09-601 shock/civil defence; Z-21.1-1987 nuclear; 100-year working life):
| | M10 | M12 | M16 | M20 |
|---|---|---|---|---|
| **h_ef (mm)** | **100** | **125** | **190** | **250** |
| Drill Ø d₀ (mm) | 20 | 22 | 30 | 37 |
| Drill depth h₁ (mm) | 107 | 133 | 203 | 266 |
| **T_inst (Nm)** | 50 | 80 | 120 | 300 |
| Bolt Ø d_B (mm) | 10 | 12 | 16 | 20 |
| Sleeve Ø d_S (mm) | 19 | 21 | 29 | 35 |
| **h_min (mm)** | 180 | 200 | 270 ¹ | 350 |
| **s_min / c_min (mm)** | 80 | 90 | 120 | 150 |
| **s_cr,N = s_cr,sp (mm)** | 300 | 375 | 570 | 750 |
| **c_cr,N = c_cr,sp (mm)** | 150 | 190 | 285 | 375 |
| `N_Rd` uncracked C20/25 (kN) | 30.7 | 44.7 | 84.0 | 128.0 |
| `N_Rd` cracked C20/25 (kN) | 26.5 | 37.1 | 69.5 | 104.9 |
| `N_Rd,C2` seismic cracked (kN) | 26.5 | 37.1 | 69.5 | 104.9 |
¹ *300 mm with TE 70-(04) rotary hammers.*

Confirm the geometry rules: `s_cr,N = 3·h_ef` and `c_cr,N = 1.5·h_ef` exactly, for all four sizes. `s_min` and `c_min` are only **0.6–0.8 · h_ef**, i.e. an undercut anchor can be placed far closer to an edge than a wedge anchor of the same capacity.

Note also **`N_Rd,C2` = `N_Rd` cracked** — the undercut anchor loses *nothing* going from static-cracked to seismic C2. Compare the fischer FAZ II wedge anchor above, which loses up to 60 %.

Variants: **HDA-P** (through-bolt), **HDA-T** (internally threaded, protected sleeve — much better in fire), **-PR/-TR** (stainless), **-PF/-TF** (Hilti technical data versions).
Other family: **fischer ZYKON FZA / FZA-I / FZP** (ETA for cracked concrete, fire class R120), sizes to M16.

**Choose when:** high loads, cracked concrete, small edge distances, seismic C2, fatigue, dynamic/shock, nuclear, or safety-critical anchorage of primary steel.
**Cost:** the most expensive post-installed type, needs a large-diameter hole (M16 → Ø30 mm) and often a dedicated tool. Specify it where it earns its keep.

### 6.4 Screw anchors (concrete screws)

**Mechanism.** A hardened, specially-threaded screw is driven into a plain drilled hole; the thread **cuts its own keyway** into the concrete. Load transfer is by mechanical interlock along the engaged thread — a distributed form fit. **No expansion force at all.**

**Real data — Hilti HUS4** (ETA-20/0867; static, quasi-static, seismic C1 & C2, and fire):
| Size | h_nom options (mm) | **h_ef (mm)** | Drill Ø d₀ | d_f,max | h_min (mm) | s_min / c_min |
|---|---|---|---|---|---|---|
| 8 | 40 / 60 / 70 | **30.6 / 47.6 / 56.1** | 8 | 12 | 80 / 100 / 120 | 35 / 35 |
| 10 | 55 / 75 / 85 | **42.5 / 59.5 / 68.0** | 10 | 14 | 100 / 130 / 140 | 40 / 40 |
| 12 | 60 / 80 / 100 | **45.9 / 62.9 / 79.9** | 12 | 16 | 110 / 130 / 150 | 50 / 50 |
| 14 | 65 / 85 / 115 | **49.3 / 66.3 / 91.8** | 14 | 18 | 75 / 95 / 125 | — |
| 16 | 85 / 130 | **66.6 / 104.9** | 16 | 20 | 95 / 140 | — |

**Critical detailing point most people miss:** `h_ef` is **substantially less than `h_nom`** (e.g. size 12 at 100 mm nominal gives only 79.9 mm effective). The first turns of thread near the surface do not count. Always design on `h_ef`.

Second point: **screw anchors have larger characteristic spacings than other types** because thread-cutting induces splitting:
```
size 8:      s_cr,N = 3.00·h_ef      c_cr,N = 1.50·h_ef
size 10, 12: s_cr,N = 3.30·h_ef      c_cr,N = 1.65·h_ef
splitting:   s_cr,sp = 3·h_ef        c_cr,sp = 1.5·h_ef
```

*Design resistance, C20/25 (Hilti, EN 1992-4):*
| Size / h_nom | 8/40 | 8/60 | 8/70 | 10/55 | 10/75 | 10/85 | 12/60 | 12/80 | 12/100 | 16/85 | 16/130 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `N_Rd` uncracked (kN) | 5.6 | 10.8 | 13.8 | 7.2 | 14.7 | 18.4 | 10.2 | 16.4 | 23.4 | 14.7 | 30.7 |
| `N_Rd` cracked (kN) | 3.7 | 7.5 | 9.6 | 5.3 | 10.5 | 12.9 | 6.7 | 11.5 | 16.4 | 10.7 | 21.3 |
| `V_Rd` uncracked (kN) | 5.6 | 15.0 | 17.5 | 9.1 | 23.0 | 25.6 | 20.4 | 31.1 | 35.9 | 35.6 | 58.5 |

**Advantages:** no expansion force → smallest possible edge distances of any mechanical type; **removable and re-usable**; immediate load; extremely fast installation (drill + drive); excellent for temporary works, formwork, and repetitive fixings; qualified for cracked concrete, seismic C1/C2, and fire.
**Limits:** modest capacity per unit; requires an impact wrench with controlled speed; the hole diameter tolerance is critical (an oversize hole strips the keyway); most families top out around size 16–20.
**Other families:** fischer **ULTRACUT FBS II**, Würth **W-BS**, Simpson **Titen HD**, DeWalt **Screw-Bolt+**.
Hilti also markets a **bonded screw** (HUS4 with a mortar capsule) — a hybrid that remains fully removable.

### 6.5 Bonded / adhesive anchors

**Mechanism.** A threaded rod or rebar is set in a drilled hole filled with a two-component resin. Load transfer is by **bond over the whole embedded length** — so the load is distributed rather than concentrated at one point, giving very high capacity, no expansion force, and the smallest edge distances of any post-installed system.

**Chemistries:**
| Type | Character | Typical use |
|---|---|---|
| **Vinylester / hybrid (HY)** | Fast cure, wide temperature window, good in damp holes, moderate bond strength | General structural, medium loads, quick turnaround. Hilti **HIT-HY 200**, fischer **FIS V**, Würth **WIT-VM** |
| **Epoxy (RE)** | Slowest cure, highest bond strength, best long-term/sustained load and creep performance, best for **post-installed rebar** and deep embedment | High loads, rebar connections, water-saturated / flooded holes, long design life. Hilti **HIT-RE 500 V4**, fischer **FIS EM Plus**, Würth **WIT-EA** |
| **Polyester** | Cheapest, styrene-based, low performance | Non-structural only |
| **Capsule (glass/foil)** | Pre-dosed, one anchor per capsule, low operator error | Rebar dowels, repeat work |

**Real data — Hilti HIT-RE 500 V4 epoxy** (ETA-20/0541; C20/25 to C50/60, cracked and uncracked; 50 and 100-year working life; assessed for hammer-drilled, hollow-drill-bit and diamond-cored holes):

*Standard embedment and required member thickness, HAS / HAS-U threaded rods:*
| Size | M8 | M10 | M12 | M16 | M20 | M24 | M27 | M30 | M33 | M36 | M39 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| **h_ef (mm)** | 80 | 90 | 110 | 125 | 170 | 210 | 240 | 270 | 300 | 330 | 360 |
| **h (mm)** | 110 | 120 | 140 | 161 | 214 | 266 | 300 | 340 | 374 | 410 | 444 |
| **h_ef,max = 20·d (mm)** | 160 | 200 | 240 | 320 | 400 | 480 | 540 | 600 | — | — | — |
| h for h_ef,max (mm) | 190 | 230 | 270 | 360 | 445 | 540 | 600 | 670 | — | — | — |

*Design tension resistance, uncracked C20/25, standard h_ef:*
| Rod | M8 | M10 | M12 | M16 | M20 | M24 | M27 | M30 | M33 | M36 | M39 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `N_Rd` HAS-U 5.8 (kN) | 19.5 | 28.0 | 37.8 | 45.8 | 72.7 | 99.8 | 121.9 | 145.5 | 142.0 | 163.8 | 186.7 |
| `N_Rd` HAS 8.8 (kN) | 13.7 | 21.7 | 31.6 | 45.8 | 72.7 | 99.8 | 80.2 | 98.1 | 121.3 | 142.8 | 170.6 |

Also qualified with **rebar B500B** sizes 8–40 mm (h_ef 80–360 mm) — this is the "post-installed rebar" application, structurally different from an anchor.

**The variable-embedment advantage:** bonded anchors are the only family where the designer freely chooses `h_ef` anywhere between `h_ef,min` (never less than `4d` and 40 mm) and `h_ef,max = 20d`. This is enormously useful when a thin slab or a nearby edge constrains the design.

**Limits and risks:**
- **Hole cleaning is the dominant failure cause.** Dust films the hole wall and the bond drops catastrophically. Hilti's **SafeSet™** (hollow drill bit that extracts dust during drilling) and the **HIT-Z** cone-shaped helical rod eliminate or bypass cleaning; fischer offers equivalent hollow-drill systems. On an Israeli site with hot, dusty conditions, this is a real specification decision, not marketing.
- **Sustained load / creep.** Long-term loading requires a reduction factor `ψ_sus` per EN 1992-4; e.g. HIT-RE 500 V4 `ψ⁰_sus = 0.88` for hammer-drilled, hollow-drill and diamond-cored (with roughening tool) holes.
- **Elevated temperature.** Every ETA quotes in-service temperature ranges; e.g. HIT-RE 500 V4 range I is −40 °C to +40 °C with max long-term/short-term base material temperature +24 °C/+40 °C. Resins soften — this matters in Israel.
- **Overhead / upward-inclined sustained tension** installations require a **certified installer and continuous special inspection** (ACI 318 §17.11; equivalent provisions in EN practice).
- Cure time before loading; sensitive to base material temperature at injection.

**Choose when:** retrofitting to existing concrete, small edge distances, deep embedment needed, post-installed rebar connections, high loads with no room for a wedge anchor's expansion zone.

### 6.6 Bonded expansion anchors — the hybrid

**Mechanism.** A rod whose embedded end is a **cone or helical taper** (rather than plain threaded) is set in resin. Under load, the taper wedges into the hardened mortar sleeve, generating **follow-up expansion inside the bond layer**. This gives the anchor cracked-concrete robustness that a plain bonded rod lacks, plus the small edge distances of a bonded system.

**Product:** **Hilti HIT-Z / HIT-Z-R** anchor rod with HIT-HY 200 — described by Hilti as *"a torque-controlled bonded anchor"* whose cone-shaped helix means it *"is not affected by uncleaned, hammer-drilled holes in dry or water saturated concrete in base materials above 5 °C."* Also permits **variable embedment depth**, with head markings for post-installation length verification. fischer's **FHB-A dyn** occupies a similar niche.

**Choose when:** you want bonded-anchor geometry with cracked-concrete/seismic reliability and you cannot guarantee hole cleaning quality on site. This is, practically, the most site-robust structural post-installed option available.

### 6.7 Comparison table

| | Wedge (torque) | Drop-in (displacement) | Undercut | Screw | Bonded | Bonded expansion |
|---|---|---|---|---|---|---|
| Load transfer | Friction | Friction | **Form fit** | Form fit | **Bond** | Bond + wedge |
| Typical Ø | M6–M24 | M6–M16 | M10–M20 | 6–16 mm | M8–M39 | M10–M20 |
| Typical h_ef | 35–125 mm | 25–65 mm | 100–250 mm | 30–105 mm | 40–600 mm | 60–200 mm |
| c_min / h_ef | ~1.0 | ~1.0 | **0.6–0.8** | ~0.7 | **~0.5** | ~0.5 |
| Cracked concrete | If qualified | Rarely | **Excellent** | Yes | If qualified | **Excellent** |
| Seismic C2 | Some, reduced | No | **Yes, no loss** | Yes | Some | Yes |
| Fatigue | Rare | No | **Yes (ETA)** | No | Rare | Rare |
| Fire | Poor | Poor | **Good (esp. -T)** | Good | Needs TR 082 | Moderate |
| Removable | No | Fixture only | No | **Yes** | No | No |
| Immediate load | **Yes** | **Yes** | **Yes** | **Yes** | No (cure) | No (cure) |
| Site sensitivity | Medium | High | Medium | Medium | **Very high** | Low |
| Relative cost | 1 | 0.7 | 5–8 | 1.5 | 2–3 | 3–4 |

---

## PART 7 — SEISMIC AND FIRE QUALIFICATION

### 7.1 Seismic — European C1 / C2

Source: **ETAG 001 Annex E (2013-04-08)**, now carried into **EAD 330232** (mechanical) and **EAD 330499** (bonded), with design in **EN 1992-4 §9**.

**Precondition:** complete assessment for cracked **and** uncracked concrete (Options 1–6) *before* any seismic qualification. Anchors qualified only for "multiple use / non-structural" (ETAG 001 Part 6) are excluded. Anchors must be placed **outside plastic hinge zones**.

**The two categories:**
| | **C1** | **C2** |
|---|---|---|
| Output | Strength (forces) only | **Strength AND displacements** (DLS and ULS) |
| Max crack width Δw | **0.5 mm** | **0.8 mm** |
| Tests | C1.1 pulsating tension, C1.2 alternating shear | C2.1a reference tension to failure, C2.3 pulsating tension, C2.4 alternating shear, **C2.5 crack cycling** |
| Min tests per series | 5 (all diameters) | 5 (all diameters) |
| Equivalent to | ACI 355.2 seismic qualification | **More demanding than anything in the US system** |

**C1 loading history** (Tables 2.2 and 2.3), applied with the crack held open at Δw = 0.5 mm:
```
Tension C1.1 :  N_eq (10 cycles) → N_i = 0.75 N_eq (30 cycles) → N_m = 0.5 N_eq (100 cycles)
Shear   C1.2 : ±V_eq (10 cycles) → ±V_i = 0.75 V_eq (30 cycles) → ±V_m = 0.5 V_eq (100 cycles)
N_eq = 0.5 · N_Ru,m · (f_c,C1.1/f_c,3)^n   (concrete/bond failure)
V_eq = 0.5 · V_Ru,m · (f_u,C1.2/f_u,5)
Cycling frequency 0.1 – 2 Hz
```

**C2 loading histories** — note the crack width *increases through the test*:
```
C2.3 (pulsating tension):        C2.4 (alternating shear):
N/N_max  cycles  Δw (mm)         ±V/V_max cycles  Δw (mm)
 0.2      25      0.5             0.2      25      0.8
 0.3      15      0.5             0.3      15      0.8
 0.4       5      0.5             0.4       5      0.8
 0.5       5      0.5             0.5       5      0.8
 0.6       5      0.8             0.6       5      0.8
 0.7       5      0.8             0.7       5      0.8
 0.8       5      0.8             0.8       5      0.8
```

**C2.5 — crack cycling (Table 2.7).** This is the test that kills most products. The anchor is held under constant tension while the crack is **opened and closed 59 times** with progressively increasing width:
```
Δw (mm) :  0.1  0.2  0.3  0.4  0.5  0.6  0.7  0.8     SUM
cycles  :   20   10    5    5    5    5    5    4  =   59
load    :  N_w1 N_w1 N_w1 N_w1 N_w1 N_w2 N_w2 N_w2

N_w1 = 0.4 · N_u,m,C2.1a · (correction)      (Eq. 2.20–2.22)
N_w2 = 0.5 · N_u,m,C2.1a · (correction)      (Eq. 2.23–2.25)
Crack closed each cycle by C_test = 0.1·f_c,C2.5·A_g, up to C_test,max = 0.15·f_c,C2.5·A_g
Cycling frequency ≤ 0.5 Hz
```

**When each category is required — ETAG 001 Annex E Table 1.1:**
| Seismicity `a_g·S` | Importance Class I | II | III | IV |
|---|---|---|---|---|
| Very low, `a_g·S ≤ 0.05 g` | ETAG 001 Parts 1–5 (no seismic qualification) | ← | ← | ← |
| Low, `0.05 g < a_g·S ≤ 0.1 g` | **C1** | **C1** (non-structural) or **C2** (structural) | | **C2** |
| `a_g·S > 0.1 g` | **C1** | **C2** | **C2** | **C2** |

where `a_g = γ_I·a_gR` (EN 1998-1 §3.2.1, §4.2.5) and `S` = soil factor.

**Directly relevant to Israel:** much of the country has `a_g` in the 0.1–0.3 g band. For **structural** connections in Importance Class II and above, this table points to **C2**.

**Design options under EN 1992-4 §9 / EOTA TR 045** (per Mahrenholtz & Pregartner, NZSEE 2016):
1. **Elastic design** — seismic actions on the anchor amplified by up to **2.5×**
2. **Capacity design** — anchor designed for the overstrength of the attached element
3. **Ductile anchor design** — requires steel failure to govern; **C2 qualification is mandatory** for this option
4. (Maximum force design — ACI 318 only)

**The `α_gap` factor.** Hole clearance in the fixture lets the plate slam back and forth during cycling, and the resulting impact is destructive. Both Hilti and fischer publish:
```
α_gap = 1.0   with a filling set / filling disc (annular gap filled)
α_gap = 0.5   without
```
So **not filling the annular gap halves the seismic shear resistance.** fischer's FFD filling disc and Hilti's seismic filling set exist for this reason. This is a *fabrication and installation* instruction, not a design one — it must appear on the shop drawings.

### 7.2 Europe vs USA — an honest comparison

From Mahrenholtz & Pregartner (2016):
- **C1 corresponds to ACI 355.2 seismic qualification.** **C2 is more demanding than anything in the ACI system** — the US has only one seismic category.
- In the US, seismic-qualified anchors are required from **SDC C** upward (short-period factor `S_DS` ≈ 0.133 g).
- In Europe, `a_g·S = 0.125–0.25 g` already falls in the "low seismicity" band where **C2 is often required**.
- Net: *"the European process of qualification and design of anchors for seismic applications is significantly more conservative than the US requirements."*
- **Caveat the paper itself raises:** the C1/C2 selection table ignores any attenuating effect of the building's own seismic response or ductility on anchor demand (number of cycles, actual crack width). *"For these reasons, the selection parameters for C1 versus C2 are still debated in Europe."* National Annexes may differ.

**Where sources disagree:** whether C1 or C2 is appropriate for a given project is genuinely contested and is a **National Annex / member-state decision**. The report should say this rather than presenting Table 1.1 as absolute.

### 7.3 Fire

**Design routes:**
- **EN 1992-4 Annex D** (informative) — covers **cast-in and mechanical** anchors only
- **EOTA TR 082 (June 2023, amended 2024-04)** — covers **bonded** fasteners under fire (explicitly out of scope of Annex D)
- Partial safety factor under fire: **`γ_M,fi = 1.0`** (Hilti HDA datasheet, per EN 1992-4)

**The number that matters — steel reduction factors `k_fi,s(θ)`** for anchor elements *directly exposed to ISO 834-1 fire, without protection* (EOTA TR 082 Annex B, Table B.1, carbon-steel threaded rods):

| Size | R15 | R30 | R45 | R60 | R90 | R120 | R180 |
|---|---|---|---|---|---|---|---|
| M6 | 0.212 | 0.092 | 0.060 | 0.051 | 0.039 | 0.030 | 0.018 |
| M8 | 0.216 | 0.093 | 0.061 | ↓ | ↓ | ↓ | ↓ |
| M10 | 0.219 | 0.093 | 0.061 | | | | |
| M12 | — | 0.095 | 0.062 | | | | |
| M16 | 0.232 | 0.099 | 0.064 | | | | |
| M20 | 0.263 | 0.102 | | | | | |
| M24 | 0.321 | 0.112 | | | | | |
| M30 | 0.369 | 0.134 | | | | | |
| M39 | 0.466 | — | | | | | |

*(Stainless, Table B.3, is marginally lower at R15 — e.g. M16 = 0.211 — and identical from R60 onward.)*

**Read that again: at R90 an unprotected anchor retains about 4 % of its ambient steel resistance; at R120, 3 %.** Larger diameters help only marginally, and only in the first 30 minutes.

Product-level confirmation:
- **fischer FAZ II M16 @ h_ef 85** — `N_Rd,fi` = 6.8 (R30) → 6.8 (R60) → 6.6 (R90) → 5.2 kN (R120), against ~16 kN static cracked design.
- **Hilti HDA-P M16 @ h_ef 190** — `N_Rd,fi` = 3.14 (R30) → 2.36 (R60) → 2.04 (R90) → 1.57 kN (R120), against **69.5 kN** static cracked. That is **4.5 % at R30**.
- **Hilti HDA-T M16** (internally threaded, steel sleeve shields the bolt) reaches ~12.1 kN at R30 — roughly **4× the HDA-P value**. The load-bearing element sits deeper and cooler.

**Practical conclusions for the report:**
1. If a fire rating is required, anchor capacity is essentially always governed by fire, not by the ambient case.
2. **Detail matters enormously**: internally-threaded / sleeved versions vastly outperform through-bolts.
3. **Encasing the base plate and the top of the anchors in concrete or intumescent protection is usually cheaper than sizing anchors for the bare-steel fire case.**
4. Available ratings: fischer FAZ II R30–R120; fischer FZA R120; Hilti HDA R30–R120 via ETA-99/0009.

---

## PART 8 — PEIKKO COLUMN SHOES AND BOLTED COLUMN CONNECTIONS

### 8.1 The concept

Instead of casting starter bars and grouting a pocket, or welding on site, the column arrives with **steel shoes** cast into its base corners. Each shoe has a bolt hole. The foundation has **cast-in anchor bolts** in the matching pattern. The column is lowered, nuts run down, shims levelled, joint grouted. The result is a **moment-resisting, stiff, immediately stable connection** with **no bracing and no site welding** — Peikko quote **10 minutes** per column.

Originally developed for precast concrete columns, but the same logic applies to steel columns, and the anchor-bolt half of the system (HPM/PPM) is used directly under steel base plates.

### 8.2 HPKM® Column Shoe — ETA-13/0603

Pairs with **HPM® Anchor Bolts** or **COPRA® Anchoring Couplers**. Column concrete **C30/37 to C70/85**; joint grout strength ≥ the column concrete grade.

*Minimum column cross-sections (HPKM Technical Manual, Table 2):*
| | HPKM 16 | HPKM 20 | HPKM 24 | HPKM 30 | HPKM 39 |
|---|---|---|---|---|---|
| A1 (mm) | 115 | 120 | 125 | 140 | 180 |
| **b_min (mm)** — 2 shoes | **230** | **240** | **250** | **280** | **360** |
| A2 (mm) | 135 | 145 | 150 | 175 | 225 |
| **d_min (mm)** — c/c layout | **270** | **290** | **300** | **350** | **450** |

*Concrete cover of the main anchor bars (Table 3):*
| | HPKM 16 | HPKM 20 | HPKM 24 | HPKM 30 | HPKM 39 |
|---|---|---|---|---|---|
| Corner position `C_c` (mm) | 40 | 42 | 42 | 44 | 46 |
| Middle position `C_m` (mm) | 55 | 58 | 60 | 63 | 72 |

*Cover of the shoe plates by exposure class (Table 1, 50-year design life):*
X0 → not required; XC1 → 25 mm or coating; XC2/XC3 → 35 mm or coating; XC4 → 40 mm; XD1/XS1 → 45 mm; XD2/XS2 → 50 mm.

**Critical condition:** *"The structural properties of HPKM Column Shoes are guaranteed only if supplementary reinforcement is provided in the column in accordance with rules of Annex A ... in addition to the main reinforcement designed to resist internal forces in the column."* This is not optional and is the most commonly missed requirement.

### 8.3 BOLDA® Column Shoe — ETA-20/0529

Peikko's current generation: *"the only ETA-assessed bolted column connection for high loads worldwide."* Full-scale tested for **bending, stiffness, shear and fire**. Pairs with **PPM® High-Strength Anchor Bolts** or COPRA couplers. Column concrete **C35/45 or higher**.

*Minimum column cross-sections (BOLDA Technical Manual 10/2021, Table 1):*
| | BOLDA 30 | BOLDA 36 | BOLDA 39 | BOLDA 45 | BOLDA 52 |
|---|---|---|---|---|---|
| **2 shoes:** B1,min / S1,min / E (mm) | 310 / 210 / 50 | 360 / 240 / 60 | 395 / 275 / 60 | 440 / 320 / 60 | 500 / 360 / 70 |
| **4 shoes:** B2,min / S2,min / S3,min / E | 350 / 250 / 160 / 50 | 405 / 285 / 180 / 60 | 450 / 330 / 205 / 60 | 510 / 390 / 230 / 60 | 550 / 410 / 275 / 70 |
| **Circular:** d_C,min / S4,min / S5,min / E | 400 / 300 / 212 / 50 | 460 / 340 / 240 / 60 | 510 / 390 / 276 / 60 | 575 / 455 / 322 / 60 | 650 / 510 / 361 / 70 |

with `S5,min = (d_C,min − 2E)/√2`.

Combine with the PPM table in §4.5: a **BOLDA 52 / PPM 52 L** connection needs a column of at least 500 mm and a foundation at least **1005 mm deep** with `h_ef = 905 mm`. That is the scale of the system.

### 8.4 Standard bolt group layouts (Peikko PPL templates)

Peikko supplies pre-fabricated bolt group templates, e.g.:
- **PPL39-4 360 × 360** — 4 × M39 square
- **PPL39-4 500 × 400** — 4 × M39 rectangular
- **PPL30-6 280 × (190+190)** — 6 × M30
- **PPL30-8 (190+190) × (190+190)** — 8 × M30
- **PPL30-3 300 × 300** — 3 × M30 triangular
- **PPL24-8 D400** — 8 × M24 on a Ø400 circle

The template is the whole point: the bolts arrive pre-fixed in a jig at guaranteed spacing, and **the tolerance problem moves from the site to the factory.**

### 8.5 Why this system matters for a steel fabricator

| Traditional cast-in bolts + base plate | Column shoe / anchor bolt system |
|---|---|
| Bolts positioned by the concrete contractor | Bolts positioned by a factory-made template |
| Tolerance absorbed by oversize plate holes | Tolerance absorbed by the system + grout |
| Site welding often needed to correct | No site welding |
| Temporary bracing during erection | Immediately stable |
| No ETA — engineer takes full responsibility | ETA-covered, `N_Rk` values declared, seismic and fire tested |
| Design by first principles | Design by Peikko Designer® + EN 1992-4 |

---

## PART 9 — WORKED EXAMPLES

### Example 1 — EN 1992-4: cast-in headed anchor bolt, Peikko HPM 24 L

**Given:** HPM 24 L (M24 thread on Ø25 mm B500B ribbed bar, forged head `d_h` = 55 mm), `h_ef` = 287 mm, concrete **C20/25 uncracked**, no edge or spacing influence, static load.

**(a) Steel failure**
```
N_Rk,s = 193.9 kN            [Peikko Table 10]
Check:  193.9 kN / 353 mm² = 549 N/mm²  →  consistent with B500B (f_uk ≈ 540–550) ✔
γ_Ms = 1.4
N_Rd,s = 193.9 / 1.4 = 138.5 kN
```

**(b) Pull-out (head bearing)**
```
A_h = (π/4)(d_h² − d²) = (π/4)(55² − 25²) = (π/4)(3025 − 625) = 1885 mm²
N_Rk,p = k2 · A_h · f_ck = 10.5 × 1885 × 20 = 395 840 N = 395.8 kN
```
**This matches Peikko's published `N_Rk,p` = 395.8 kN exactly.** (Verified likewise for all five HPM sizes — 195.9 / 283.0 / 395.8 / 639.3 / 1072.1 kN. This is a good independent confirmation of both the EN 1992-4 formula and the manufacturer's data, and is worth showing in the report.)
```
γ_Mp = 1.5  →  N_Rd,p = 395.8 / 1.5 = 263.9 kN
```

**(c) Concrete cone**
```
N⁰_Rk,c = k_ucr,N · √f_ck · h_ef^1.5 = 12.7 × √20 × 287^1.5
        = 12.7 × 4.4721 × 4862.1 = 276 140 N = 276.1 kN
γ_Mc = 1.5  →  N_Rd,c = 184.1 kN
```

**(d) Governing**
```
N_Rd = min(138.5 ; 263.9 ; 184.1) = 138.5 kN   →  STEEL GOVERNS ✔ ductile
```
This is the design intent of a proprietary cast-in system: the geometry is chosen so that the **steel** is the weak link.

**(e) Now bring the anchor near an edge — `c` = 70 mm (`c_min`)**
```
c_cr,N = 1.5 h_ef = 430.5 mm   s_cr,N = 3 h_ef = 861 mm
A⁰_c,N = 861² = 741 321 mm²
A_c,N  = (c + 1.5h_ef) × 3h_ef = (70 + 430.5) × 861 = 430 930 mm²
A_c,N/A⁰_c,N = 0.581

ψ_s,N  = 0.7 + 0.3 × (70/430.5) = 0.749
ψ_re,N = 0.5 + 287/200 = 1.94 → 1.0
N_Rk,c = 276.1 × 0.581 × 0.749 = 120.2 kN
N_Rd,c = 80.1 kN
```
```
N_Rd = min(138.5 ; 263.9 ; 80.1) = 80.1 kN   →  CONCRETE CONE GOVERNS
```

**Capacity fell by 42 % purely because of edge distance.** Also, `c = 70 mm < 1.5·h_ef = 430 mm`, so the splitting exemption is lost → **supplementary reinforcement per Peikko Annex A2 is mandatory**.

*Detailing summary:* HPM 24 L needs `h_min` = 385 mm, `c_min` = 70 mm, `s_min` = 100 mm, but to reach **full** capacity it needs `c ≥ 430 mm` and `s ≥ 861 mm`. **These two sets of numbers must both appear on the drawing.**

---

### Example 2 — ACI 318 / AISC DG1: hooked vs headed anchor rod
*(after AISC Design Guide 1, 2nd ed., Example 4.5.2, pp. 35–36)*

**Given:** W-column base, 4 anchor rods, net uplift `T_u` = 69.8 kips (LRFD) → **17.5 kips/rod**. `f'c` = 4,000 psi, uncracked (`ψ₄ = 1.4`), anchor in the middle of a large spread footing (no edge constraint). Rod: **7/8-in. ASTM F1554 Grade 36**.

**(a) Steel — OK**
```
R_n = 0.75 · F_u · A_r = 0.75 × 58 × 0.6013 = 26.1 kips
φR_n = 0.75 × 26.1 = 19.6 kips/rod > 17.5 ✔
```

**(b) Try a 3½-in. hook (L-bolt)**
```
e_h = 3.5 − 0.875 = 2.625 in.    (note: exactly 3·d_a — the ACI minimum)
φN_p = φ · 0.9 · f'c · d_o · e_h · ψ₄
     = 0.70 × 0.9 × 4000 × (7/8) × 2.625 × 1.4
     = 8,100 lb = 8.10 kips  <  17.50 kips   ✘ FAIL
```
> *DG1's own comment:* "As mentioned earlier in this Guide, the use of hooked anchor rods is generally not recommended. Their use here is to demonstrate their limited pull out strength."
>
> **A 3½-inch hook develops 46 % of the required rod tension. To pass, the hook would have to be ~7½ in. long — which ACI does not allow to be counted (cap at 4.5d_a = 3.94 in.).**

**(c) Use a heavy hex nut instead**
```
φN_pn (7/8-in. rod, heavy hex nut, f'c = 4 ksi) = 20.5 kips > 17.5 ✔
```
**Same rod, same concrete, same hole — 2.5× the capacity, just by replacing the bend with a nut.**

**(d) Required embedment for concrete breakout — `h_ef` = 13 in.**
```
Group of 4 at 4-in. square spacing:
A_N   = (1.5×13 + 4 + 1.5×13)² = 43² = 1,849 in²
A_No  = 9 × 13² = 1,521 in²

φN_cbg = φ · ψ₃ · 16 · √f'c · h_ef^(5/3) · (A_N/A_No)          [h_ef > 11 in.]
       = 0.70 × 1.25 × 16 × √4000 × 13^1.667 × (1849/1521)
       = 0.70 × 1.25 × 16 × 63.246 × 71.86 × 1.2157
       = 77,400 lb = 77.4 kips  >  69.8 kips ✔
```

**(e) Now confine it in a 20-in. square pier**
```
Max edge distance = 8 in.  →  effective h_ef limited to 8/1.5 = 5.33 in.
φN_cbg = 0.70 × 1.25 × 24 × √4000 × 5.33^1.5 × (400 / (9 × 5.33²))
       = 25.5 kips  <  69.8 kips  ✘ FAIL
```
**Message:** in a slender pier the breakout cone cannot develop at all, and **anchor (supplementary) reinforcement becomes mandatory** — the anchors must hand their load to vertical bars and ties that carry it down into the pier.

---

### Example 3 — EN 1992-4: post-installed bonded anchor, Hilti HIT-RE 500 V4 M20

**Given:** M20 HAS-U 8.8 threaded rod, HIT-RE 500 V4 epoxy, `h_ef` = 170 mm, **C20/25 uncracked**, hammer-drilled and cleaned hole, no edge or spacing influence, in-service temperature range I, short-term loading.

**(a) Steel**
```
A_s(M20) = 245 mm²;  grade 8.8 → f_uk = 800 MPa, f_yk = 640 MPa
N_Rk,s = 245 × 800 = 196.0 kN
γ_Ms = 1.2 × (800/640) = 1.5
N_Rd,s = 196.0 / 1.5 = 130.7 kN
```

**(b) Concrete cone**
```
N⁰_Rk,c = k1 · √f_ck · h_ef^1.5 = 11.0 × √20 × 170^1.5
        = 11.0 × 4.4721 × 2216.5 = 109 030 N = 109.0 kN
N_Rd,c = 109.0 / 1.5 = 72.7 kN
```

**(c) Combined pull-out + cone (bond)**
```
N⁰_Rk,p = π · d · h_ef · τ_Rk = π × 20 × 170 × τ_Rk = 10 681 · τ_Rk
```

**(d) Compare with Hilti's published value**
```
Hilti ETA-20/0541, HAS-U 8.8, M20, h_ef 170, uncracked C20/25:   N_Rd = 72.7 kN
```
**Exactly the concrete-cone value.** So for this configuration the design is governed by **concrete cone breakout**, not bond and not steel — and the calculation independently confirms `k1 = 11.0` for post-installed fasteners in uncracked concrete. The bond `τ_Rk` must exceed ≈ 10.2 MPa for cone to govern, which epoxies comfortably do.

**(e) Geometry rules**
```
Cone:      c_cr,N  = 1.5 × 170 = 255 mm     s_cr,N  = 510 mm
Bond:      s_cr,Np = 20 × 20 × (10.2/7.5)^0.5 = 466 mm ≤ 3h_ef = 510 mm  → 466 mm
           c_cr,Np = 233 mm
Member:    h = 214 mm (Hilti table)
Max h_ef:  20 × 20 = 400 mm (member thickness then 445 mm)
```
**Design lever:** if the member were thicker, increasing `h_ef` from 170 → 300 mm would raise the cone capacity by `(300/170)^1.5` = **2.35×** — to ~171 kN — at which point **steel would govern at 130.7 kN** and the anchor becomes ductile. *This is the single most valuable design move available with bonded anchors, and it costs only drilling time.*

---

## PART 10 — PRACTICAL FABRICATION AND DETAILING IMPLICATIONS

### 10.1 Tolerances — the real reason cast-in anchorages fail

**The conflict.** Division 3 (concrete) specs reference ACI 117; Division 5 (steel) specs reference AISC. They are incompatible.

| Source | Requirement |
|---|---|
| ACI 117-90 §2.3 (old) | ±1 in. (25 mm) on embedded items — **far too loose** |
| AISC Code of Standard Practice §7.5 | Variation between centres of **any two anchor rods in a group ≤ 1/8 in. (3 mm)** — **far too tight** |
| **ACI 117-10 §2.3.4.1** | Top of anchor bolt, **vertical: ±1/2 in. (13 mm)** |
| **ACI 117-10 §2.3.4.2** | Horizontal, per bolt diameter: **±1/4 in. (6 mm)** for 3/4 & 7/8 in.; **±3/8 in. (10 mm)** for 1, 1-1/4, 1-1/2 in.; **±1/2 in. (13 mm)** for 1-3/4, 2, 2-1/2 in. |
| ASCC Position Statement #14 | Endorses the ACI 117-10 values as achievable |

AISC guidance quoted by ASCC: *"If bolts are misplaced up to 1/2 inch, the oversized base plate holes normally allow the base plate and column to be placed near or on the column line. If the bolts are misplaced by more than 1/2 inch, then corrective work is required."*

**AISC recommended oversized base-plate holes:**
| Bolt Ø | Hole Ø | Bolt Ø | Hole Ø |
|---|---|---|---|
| 3/4 in. | 1-5/16 in. | 1-1/2 in. | 2-5/16 in. |
| 7/8 in. | 1-9/16 in. | 1-3/4 in. | 2-3/4 in. |
| 1 in. | 1-13/16 in. | 2 in. | 3-1/4 in. |
| 1-1/4 in. | 2-1/16 in. | 2-1/2 in. | 3-3/4 in. |

DG1 suggests adding a further **1/4 in.** and using a **heavy plate washer** over the hole.

**Metric equivalent (ASI Design Guide 7, AS 4100 §14.3.5.2 — directly usable practice):**
- Hole diameter may be up to **6 mm larger** than the anchor bolt diameter
- **A plate washer, minimum 6 mm thick, is required under the nut whenever the hole is more than 3 mm larger than the bolt**
- Holes should be **drilled**, not flame cut

### 10.2 Base plate and grout detailing (ASI DG7 §4.2 — practical, metric)

1. **Use four anchor bolts** as standard. Two-bolt arrangements only for short columns or door posts. (US OSHA requires 4 unless the post weighs < 140 kg.)
2. **Base plate thickness:** minimum **12 mm** for posts and lightly loaded columns; **20 mm minimum for normal applications.** Preferred thicknesses: **12, 16, 20, 25, 28, 32, 36, 40 mm.**
   → EN 1992-4 §6.2.1 links this to design: the linear-strain (rigid plate) load distribution to the bolts is only valid *"if the base plate remains elastic under design actions and its deformation remains negligible in comparison with the axial displacement of the fasteners."* **A thin base plate invalidates your anchor design.**
3. **Grout inspection hole** 50–75 mm diameter for base plates larger than 600 mm in either direction, to let grout rise and prevent air pockets.
4. **Grout gap:** minimum **25 mm for grouting**, **50 mm for mortar bedding**. Grout characteristic cube strength ≥ **2× the foundation concrete**.
5. **Welding:** prefer fillet welds to butt welds. Avoid "weld all round" — welds across flange toes and around web fillets add little strength and cost a lot. For I-columns, welding one side of each flange and both sides of the web is usually adequate. For RHS/SHS, weld the flat portions only, avoid the radiused corners.
6. **Shims:** survey the base plate area and place shims to the correct underside level before erection. Show **permanent** shims on the shop drawings; leave **temporary** shims to the erector.
7. **Shear key / nib** welded under the base plate when shear is significant — far more reliable than bolts in bending across a grout gap.
8. Plate dimensions, pitch and gauge must be chosen so **the anchor bolts do not clash with the foundation reinforcement.** This is the single most common source of site improvisation.

### 10.3 Anchor rod fabrication

- **Bending:** ASTM F1554 permits both cold and hot bending. **Never bend in the threaded portion.** (If a hooked rod must be made, thread after bending or protect the thread.)
- **Weldability:** F1554 Grade 36 is normally weldable as supplied. **Grade 55 should be ordered with Supplement S1** (carbon equivalent ≤ 0.45 %) if any field welding is foreseeable. Grade 105 — do not weld.
- **Nuts:** always **heavy hex**, especially with oversize holes. Tack-weld or double-nut the embedded nut so it cannot back off during placing.
- **Galvanising:** hot-dip to EN ISO 10684 / ASTM F2329. Nuts must be **over-tapped** to suit; specify the matching nut. Beware hydrogen embrittlement risk in high-strength grades.
- **Colour coding (F1554):** Blue = Gr 36, Yellow = Gr 55, Red = Gr 105. Peikko HPM uses its own: Yellow/Blue/Grey/Green/Orange for M16/20/24/30/39. **Use the colour code in receiving inspection.**
- **Thread projection:** provide enough thread for a levelling nut below the plate plus the fixing nut plus at least 2–3 thread pitches proud. Peikko HPM thread lengths: 140/140/170/190/200 mm for M16–M39 — generous for exactly this reason.
- **Setting templates:** a rigid steel template (not timber) with the bolts fixed top and bottom, tied to the formwork or to the rebar cage, is the only reliable way to hit the ACI 117 tolerances. Peikko sells these as PPL bolt groups.

### 10.4 Post-installed anchor installation — the quality control list

| Risk | Control |
|---|---|
| Wrong hole diameter | Specify `d₀` **and** `d_cut,max` on the drawing (e.g. FAZ II M16: `d₀` = 16 mm, `d_cut,max` = 16.50 mm hammer / 16.45 mm diamond). Check drill bit wear. |
| Wrong hole depth | Specify `h₁`, not just `h_ef`. Use a stop drill bit. |
| **Uncleaned hole (adhesives)** | Specify brush + blow cycles, or specify a **hollow drill bit system** (Hilti SafeSet, fischer hollow-drill) or a **bonded expansion anchor (HIT-Z)** that tolerates uncleaned holes. |
| Under/over torque | Specify `T_inst` on the drawing (FAZ II M16 = 110 Nm; HDA M16 = 120 Nm; HDA M20 = 300 Nm). Calibrated torque wrench, recorded. |
| Rebar strike | Rebar scanning before drilling. Undercut/bonded anchors can often be shifted; cast-in cannot. |
| Insufficient member thickness | `h_min` is a hard product limit, e.g. HDA M20 = 350 mm, FAZ II M24 = 206.5 mm, HUS4 size 12/100 = 150 mm. |
| Diamond coring | Reduces bond and mechanical interlock. Only permitted where the ETA covers it, often with a roughening tool. |
| Cure time not observed | Adhesive cure time is temperature dependent — publish the table on site. |
| **Seismic gap not filled** | Specify the filling set / FFD disc. Without it `α_gap` = 0.5 — **half the seismic shear resistance.** |
| Overhead sustained tension | Certified installer + continuous inspection (ACI 318 §17.11). |

### 10.5 Design decisions the fabricator should push back on

1. **"Just use J-bolts, they're cheaper."** They develop 22–43 % of the rod. Ask for headed rods or nutted rods. Cost difference is a nut.
2. **"Make the base plate thinner to save weight."** It invalidates the rigid-plate assumption in EN 1992-4 §6.2.1.
3. **"Weld a big plate washer on the end, it'll be stronger."** It won't (AISC DG1 §2.5), and it blocks the rebar and traps voids. Only justified for high-strength rods or blow-out control.
4. **"Put the bolts closer to the edge, the footing is big enough."** Edge distance drives `ψ_s,N`, `A_c,N` and the whole concrete edge check. Example 1 shows a 42 % loss.
5. **"No fire rating on the base, it's below the slab."** Confirm it. If a rating applies, the anchors need protection or gross oversizing — see §7.3.
6. **"Any anchor with an approval will do."** Ask three questions: (i) cracked concrete? (ii) seismic **C1 or C2**? (iii) fire class? All three must be on the ETA, and all three must be on the drawing.
7. **"We'll fix the misplaced bolts on site."** Establish before pouring: who owns the tolerance, ACI 117-10 or AISC. Put the accepted tolerance in the contract.

---

## PART 11 — SOURCES ACTUALLY USED

**Codes and pre-normative documents**
1. DIN EN 1992-4:2019-04 / EN 1992-4:2018(E) — *Eurocode 2, Part 4: Design of fastenings for use in concrete* (preview extract, §7.2.1.8–7.2.2.3) — https://standards.iteh.ai/catalog/standards/cen/a8d47a68-f072-4eed-81af-5f9e7364eb84/en-1992-4-2018
2. EOTA **TR 029** — *Design of Bonded Anchors* — https://www.eota.eu/sites/default/files/uploads/Technical%20reports/tr029am.pdf
3. EOTA **TR 047** — *Design of Anchor Channels* (Sept 2015, amended Sept 2017 / 2018-03) — https://www.eota.eu/sites/default/files/uploads/Technical%20reports/eota-tr-047-design-of-anchor-channels-2018-03.pdf
4. EOTA **TR 082:2024-04** — *Design of Bonded Fasteners in Concrete under Fire Conditions* — https://www.eota.eu/sites/default/files/uploads/Technical%20reports/TR082_DESIGN%20BONDED%20FASTENERS%20IN%20CONCRETE%20UNDER%20FIRE_2024-04.pdf
5. **ETAG 001 Annex E** (2013-04-08) — *Assessment of metal anchors under seismic actions* — https://www.eota.eu/sites/default/files/uploads/ETAGs/etag-001-annex-e-2013-04-08-2.pdf

**Design guides and technical papers**
6. **AISC Steel Design Guide 1, 2nd Edition** — *Base Plate and Anchor Rod Design*, J.M. Fisher & L.A. Kloiber (full text; §2.5, §3.2.2, Table 3.2, Examples 4.5.2 and 4.9.1)
7. Mahrenholtz, P. & Pregartner, T. (2016) — *Qualification and design of seismic anchors — Requirements in New Zealand and Australia*, NZSEE Conference — https://www.nzsee.org.nz/db/2016/Papers/P-65%20Mahrenholtz.pdf
8. IDEA StatiCa — *Code-check of anchors (EN)* (full EN 1992-4 implementation with clause numbers) — https://www.ideastatica.com/support-center/support-center-knowledge-base/check-of-anchors-according-to-eurocode
9. Buhler, J. (Würth Group, 2021/2023) — *EN 1992-4 (Eurocode 2): Design of concrete structures Part 4* — https://www.tvz.hr/wp-content/uploads/2023/12/2311-EN1992-4-new.pdf
10. Caltrans **Bridge Design Memo 5.51** (Aug 2021) — *Anchorage to Concrete: Cast-In Anchors* (ACI 318 definitions) — https://dot.ca.gov/-/media/dot-media/programs/engineering/documents/bridgedesignmemos/05/202108-bdm0551-anchoragetoconcrete-castinanchors-a11y.pdf
11. Liebler, M. (2016) — *PDHonline Course S293: Anchorage to Concrete* (ACI pull-out formulas, ductility discussion) — https://pdhonline.com/courses/s293/s293content.pdf
12. ASCC **Position Statement #14** — *Anchor Bolt Tolerances* (ACI 117-10 §2.3.4, AISC oversize holes) — as published in *Concrete International*
13. Australian Steel Institute — **Design Guide 7: Pinned base plate connections for columns**, T.J. Hogan (2011), §4.2 Base plate detailing — https://www.steel.org.au/getattachment/f68b3f37-530a-4316-8c4e-e2617a95b7de/Detailing-considerations-Design-Guide-7_bk745.pdf
14. Wald, F. et al. — *Column Bases* (prEN 1993-1-8 base plate model); Jaspart, Wald, Weynand & Gresnigt — *Steel column base classification*
15. STRUCTURE magazine — *ACI 318-25 Changes to Anchorage and Reinforcing Bar Provisions* — https://www.structuremag.org/article/aci-318-25-changes-to-anchorage-and-reinforcing-bar-provisions/

**Manufacturer technical manuals (primary product data)**
16. **Peikko HPM® Rebar Anchor Bolt — Technical Manual, Industrial applications**, 06/2024 (ETA-02/0006) — https://media.peikko.com/file/dl/i/71TI4A/hE2HG3SxBHCXXrs54YYxuw/HPM_Industrial_PEIKKO_GROUP_01_Technical_Manual_Web.pdf
17. **Peikko PPM® High-Strength Anchor Bolt — Technical Manual**, 10/2023 — https://media.peikko.com/file/dl/i/J_4N3A/5SM9-2bze-HWGqVrLezBvg/PPM_PEIKKO_GROUP_004_Technical_Manual_Web.pdf
18. **Peikko HPKM® Column Shoe — Technical Manual** (ETA-13/0603) — https://f.nordiskemedier.dk/2dwaa1kmt8ry0iia.pdf
19. **Peikko BOLDA® Column Shoe — Technical Manual**, 10/2021 (ETA-20/0529) — https://www.byggfaktadocu.se/teknisk-beskrivning-bolda-1300331/fil-files/BOLDATechnical%20Manual.pdf
20. **Hilti HIT-RE 500 V4 Injection Mortar — Product Technical Datasheet** (ETA-20/0541) — https://files-ask.hilti.com/original/ml/mlmjr16v1x.pdf
21. **Hilti HDA Undercut Anchor — Product Technical Datasheet** (ETA-99/0009, ETA-18/0974, BZS D 09-601, Z-21.1-1987) — https://www.buildsite.com/pdf/hilti/HDA-Undercut-Anchor-HDA-P-HDA-PF-HDA-PR-HDA-T-and-HDA-TR-Product-Data-2910333.pdf
22. **Hilti HUS4 Screw Anchor — Product Technical Datasheet** (ETA-20/0867) — https://files-ask.hilti.com/original/uu/uudzpjuzoq.pdf
23. **fischer Bolt Anchor FAZ II — Technical Product Information / ExpertGuide**, status 07/2022 (ETA-05/0069, EAD 330232-00-0601, ICC-ES ESR-2948) — https://www.fischer-international.com/-/media/fixing-systems/rebrush/fiint/service/planungshilfen/planner/expertguides/04-tpi-c-35-ma-faz-ii-fischer-expertguide-asia-07-2022-en.pdf
24. **fischer White Paper — The importance of the new EN 1992-4 standard** — https://www.fischer.co.uk/-/media/fixing-systems/rebrush/fiuk/white-papers-blog-post/fischer_whitepaper_en1992-4_uk.pdf
25. **HALFEN Cast-In Channels, Technical Product Information B 13.1-E** (2014; ETA-09/0339, HTA-CE / HZA / HGB / HTU) — https://www.ancon.co.nz/downloads/5247/Halfen_Anchor_Channels.pdf
26. **DeWalt/Powers Mechanical Anchors Technical Guide for the Design Professional** (ACI 318 / ICC-ES side; `k_cr`/`k_uncr`, anchor categories) — https://anchors.dewalt.com/anchors/_documents/uploads/mechanical-anchors_techguide_manual.pdf
27. Hilti — *SafeSet™ Technology* and *HIT-Z Anchor Rod* product pages — https://ask.hilti.com/article/safeset-technology/xl5hpr ; https://www.hilti.com/c/CLS_FASTENER_7135/CLS_ANCHOR_RODS_ELEMENTS_7135/r25625358
28. Simpson Strong-Tie SE Blog — *Anchor Anatomy 101: Drop-In Internally Threaded Anchors* — https://seblog.strongtie.com/2026/02/anchor-anatomy-101-drop-in-internally-threaded-anchors/
29. Peikko — *3 ways to connect precast columns to foundation* and HULCO® Anchor Bolt — https://www.peikko.com/peikkoway/blogs/3-ways-to-connect-precast-columns-to-foundation-choose-yours/

---

## APPENDIX — FLAGGED UNCERTAINTIES (do not present these as settled)

1. **Israeli regulatory position.** ת"י 1225 Part 1.1 (structural steel, June 2023) is verified. I could **not** verify from public sources which standard Israel mandates for anchor design, nor an Israeli equivalent to EN 1992-4. The report should state that ETA/EN 1992-4 is the *de facto* route via the products supplied, and recommend confirmation with מכון התקנים הישראלי. One search result claiming "ILNAS-EN 1992-4 = Israeli adoption" is **wrong** — ILNAS is the Luxembourg standards body.

2. **Peikko HPM seismic values.** The manual's `N_Rk,s,C1/C2` = 83.6 kN, `N_Rk,p,C1/C2` = 62.5 kN and `V⁰_Rk,s,C1/C2` = 26.8 kN appear **constant across all five sizes (16 L to 39 L)**. This is plausible (a single tested configuration applied conservatively) but the PDF table layout was partially scrambled in extraction. **Verify against ETA-02/0006 before publishing these numbers.**

3. **Hilti HDA fire values.** The HDA-P/-T/-PR/-TR fire tables were partly scrambled. The HDA-P figures (M16: 3.14 / 2.36 / 2.04 / 1.57 kN at R30/60/90/120) are internally consistent and consistent with the TR 082 `k_fi,s` table, but the HDA-T figures should be re-read from the source datasheet before quoting.

4. **`k_c` in ACI 318.** Multiple secondary web sources reverse the values. **Correct: `k_c` = 24 for cast-in, 17 for post-installed.** Confirmed against AISC DG1 (2nd ed.) pp. 20 and 36. If the report is challenged on this, cite DG1.

5. **`k1` = 8.9 / 12.7 and 7.7 / 11.0.** These are the standard values and are consistent across IDEA StatiCa's EN 1992-4 implementation, the Peikko HPM manual's declared factors, and the independent verification in Worked Example 3 (`k1` = 11.0 reproduces Hilti's published `N_Rd` exactly). However, EN 1992-4 itself states that `k_cr,N` and `k_ucr,N` **shall be taken from the ETA**. For any specific product, use the ETA value, not the generic one.

6. **C1 vs C2 selection.** Genuinely contested in Europe, and a National Annex decision. Mahrenholtz & Pregartner explicitly note the selection parameters "are still debated." Present Table 1.1 as a *recommendation*, not a rule.

7. **fischer FAZ II `h_min` values.** The M16/65 and M16/85 rows both read 140 mm in the extracted table. Plausible under a `max(2h_ef, h₁+30, 100)` rule but worth a re-read of the source PDF.

**Working files** (extracted text of every source, for re-checking any number in this brief):
`C:\Users\User\AppData\Local\Temp\claude\C--Users-User-Desktop\a4c7fddc-7ed7-48b9-a359-e4b37dc8fdbe\scratchpad\anchors\`