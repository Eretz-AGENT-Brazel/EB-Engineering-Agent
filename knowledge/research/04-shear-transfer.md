Note: claude-sonnet-5[1m] (the safety classifier) was unavailable when reviewing this subagent's work. Please carefully verify the subagent's actions and output before acting on them.

I have gathered enough primary material. Writing up the technical brief.

# SHEAR TRANSFER AT STEEL COLUMN BASES — TECHNICAL BRIEF
### Raw material for a professional engineering report (Eretz Barzel). All figures tagged with source. English; terms given so they can be translated precisely.

---

## 0. SCOPE AND THE ONE-SENTENCE SUMMARY

A base plate does **not** transfer horizontal force the way a bolted steel-to-steel splice does. Four mechanisms exist, they are **not equally stiff**, and they do **not** act simultaneously unless the detail is deliberately built to make them act simultaneously:

| # | Mechanism | English term | Stiffness | Requires |
|---|---|---|---|---|
| (a) | Friction plate/grout | *friction* | Very stiff (≈0 slip) | **Net compression** on the plate |
| (b) | Bearing of a lug into concrete | *shear lug / shear key / shear stub / nib* | Very stiff | A grout pocket + reinforced pedestal |
| (c) | Direct contact of an embedded plate/column | *recessed / embedded base, blockout* | Very stiff | Blockout / pocket in the slab or footing |
| (d) | Shear + bending of the anchors | *anchor bolts in shear (with lever arm)* | **Very flexible** (5–30 mm slip) | Bolts actually bearing on the plate |

> "Initially, the force will be resisted by the 'stiff' mechanisms of friction (if compressive load is present) and shear key bearing (if a shear lug is provided). Once these are overcome, the lateral load may be resisted by other more 'flexible' mechanisms, such as anchor rod bearing." — Gomez, Kanvinde, Smith & Deierlein, *Shear Transfer in Exposed Column Base Plates*, AISC 2009, §4.2.

The four ways are enumerated identically in the Eurocode literature: friction; shear and bending of anchor bolts; a special shear key (block of I-stub, T-section or steel pad welded under the plate); direct contact by recessing the plate into the footing — Gresnigt, Romeijn, Wald & Steenhuis, *HERON* 53(1/2) 2008, §1, Fig. 1; also CESTRUCO Q&A 7.8, Fig. 7.14.

**The two facts a fabricator must internalise:**
1. **Friction dies the instant the column goes into net tension** (EN 1993-1-8 §6.2.2(6) NOTE: *"If the column is loaded by a tensile normal force, F_f,Rd = 0"*).
2. **Anchor bolts in oversized holes carry nothing until the plate has slipped**, and EN 1993-1-8 §6.2.2(5) explicitly forbids counting them when the holes *are* oversized.

---

## 1. FRICTION UNDER THE BASE PLATE

### 1.1 Eurocode: EN 1993-1-8:2005 §6.2.2 (verbatim structure)

**§6.2.2(5)** — the governing sentence for this whole brief:

> "In base plates if no special elements for resisting shear are provided, such as block or bar shear connectors, it should be demonstrated that the design friction resistance of the base plate, see 6.2.2(6), and, **in cases where the bolt holes are not oversized**, the design shear resistance of the anchor bolts, see 6.2.2(7), added up is sufficient to transfer the design shear force. The design bearing resistance of the block or bar shear connectors with respect to the concrete should be checked according to EN 1992."

**§6.2.2(6)** — friction:

```
F_f,Rd = C_f,d · N_c,Ed                                        ... EN 1993-1-8 Eq. (6.1)
```
- `F_f,Rd` = design friction resistance between base plate and grout layer [N]
- `C_f,d` = coefficient of friction between base plate and grout layer
  - **C_f,d = 0,20 for sand-cement mortar** (EN 1993-1-8:2005 §6.2.2(6))
  - for other types of grout, `C_f,d` shall be determined by **testing in accordance with EN 1990 Annex D**
- `N_c,Ed` = design value of the normal **compressive** force in the column [N]
- **NOTE: if the column is loaded by a tensile normal force, F_f,Rd = 0.**

**§6.2.2(7)** — anchor bolt shear (see §6 below):
```
F_vb,Rd = min( F_1,vb,Rd ; F_2,vb,Rd )
F_2,vb,Rd = α_bc · f_ub · A_s / γ_M2                            ... EN 1993-1-8 Eq. (6.2)
α_bc = 0,44 − 0,0003 · f_yb        with 235 N/mm² ≤ f_yb ≤ 640 N/mm²
```

**§6.2.2(8)** — total:
```
F_v,Rd = F_f,Rd + n · F_vb,Rd                                   ... EN 1993-1-8 Eq. (6.3)
```
`n` = number of anchor bolts in the base plate.

### 1.2 Where the "0,30" actually comes from — IMPORTANT CLARIFICATION

The value 0,30 is widely quoted, but it is **not** "concrete-to-concrete" in the Eurocode column-base literature. The background document to EN 1993-1-8 (Gresnigt et al., *HERON* 53(1/2) 2008, §2.1 and §5.1, Eqs. 33a/33b) proposes:

| Interface | C_f,d | Source |
|---|---|---|
| sand-cement mortar | **0,20** | HERON §2.1; adopted into EN 1993-1-8:2005 §6.2.2(6) |
| **special grout** (e.g. Pagel IV) | **0,30** | HERON §5.1 Eq. (33b) — *proposed* to CEN, only 0,20 was published |

Later positions (report both, and say they disagree):

| Source | Value | Note |
|---|---|---|
| EN 1993-1-8:2005 §6.2.2(6) | 0,20 | sand-cement mortar; the value in force in most NAs |
| FprEN 1993-1-8:2023 / EN 1993-1-8:2024, cl. D.3.1.4 | **0,30** for sand-cement mortar | per IDEA StatiCa knowledge base; the new generation raises 0,20 → 0,30 |
| SCI P398 (UK, *Joints in Steel Construction: Moment-resisting joints to Eurocode 3*), STEP 4 | "**A resistance of 0.3 times the total compression force may be assumed**" | UK practice already at 0,3 |
| CEB *Design of Fastenings in Concrete* (1996) §4.1 / fib Bull. 58 | μ = **0,40**, γ_Mf = 1,5 (ULS), 1,3 (fatigue), 1,0 (SLS) → effective **0,267** | `V_Rd,f = μ·C_Sd/γ_Mf` (HERON Eq. 17) |
| EN 1992-4:2018 §6.1(2) | **friction is neglected** in the design of fastenings | Deliberate — do not "double-dip" between EC3 and EC2-4 |

**CEB caveat that kills friction in normal steelwork:** "The friction force V_Rd,f should be neglected if the thickness of grout beneath the fixture is thicker than **3 mm** (e.g. in case of levelling nuts) and for anchorages close to an edge. … according to the CEB Guide, load transfer through friction should be neglected because in normal steel constructions the thickness of the grout is always more than 3 mm." (HERON §3.1). Real bedding is 20–40 mm (SCI P398 §5.4) or 25–50 mm (ASI DG7 §4.2 item 10). This is why EC3 and EC2-4 give different answers — **name this conflict explicitly in the report.**

### 1.3 American practice: AISC Design Guide 1 §3.5.1 / ACI 349

```
φV_n = φ · μ · P_u   ≤  0,2 f'c · A_c                          ... AISC DG1 (2nd ed.) §3.5.1
```
- μ = **0,55** for steel on grout
- μ = **0,70** for steel on concrete
- Cap: **0,2 f'c A_c** (A_c = contact area). φ = 0,75 (ACI shear-friction).
- "The contribution of the shear should be based on the **most unfavorable arrangement of factored compressive loads, P_u, that is consistent with the lateral force being evaluated, V_u**." (DG1 §3.5.1)

**PIP STE05121 (Process Industry Practices, *Anchor Bolt Design Guide*, Oct 2006) §8.2** — friction depends on **how deep the contact plane is**, which is the cleanest statement of why recessing helps:

| Condition | μ |
|---|---|
| Concrete placed against as-rolled steel, contact plane **a full plate thickness below the concrete surface** (embedded/recessed base) | **0,90** |
| Concrete or grout placed against as-rolled steel, contact plane **coincidental with the concrete surface** | **0,70** |
| **Grouted condition**, contact plane between grout and as-rolled steel **above** the concrete surface (normal base plate) | **0,55** |

PIP §8.1, two operational rules worth quoting in the report:
- "Care shall be taken to assure that the downward load that produces frictional resistance **occurs simultaneously with the shear load**."
- "The frictional resistance **should not be used in combination with the shear resistance of anchors** unless a mechanism exists to keep the base plate from slipping before the anchors can resist the load (**such as welding the washer to the base plate**)."

### 1.4 Measured friction — the only large-scale test data (use this to justify numbers)

Gomez, Kanvinde, Smith & Deierlein (AISC report, March 2009): 7 full-scale tests, ~600×600 base plate, ASTM A529 Gr.50 plate **with mill scale**, 1 in. (25 mm) grout pad, 26×26 in. grout area, cyclic slip protocol ±0.1/0.2/0.4/0.8/1.0 in.

| Item | Value |
|---|---|
| Axial loads applied | 43 / 112 / 261 kips (191 / 498 / 1161 kN) → bearing 64 / 166 / 386 psi (0,44 / 1,15 / 2,66 MPa) |
| **μ with steel shim stacks (Tests #1,#2)** | **0,46** (regression, R² > 0,98) |
| **μ on pure grout, no shims (Test #3)** | **0,45** |
| Recommended design value | **μ = 0,45** — "20 % lower than the design value featured in the AISC Design Guide One (0,55)" |
| Initial adhesion/bond peak | **≈ 2 × the sliding friction** — explicitly discarded, "may not be available in field conditions" |
| Shim gouging | raises μ later in the history — also discarded for design |

Literature values collected in the same report (§2.3.1): Cannon et al. 1975 → 0,53; Nagae et al. 2006 (cyclic) → 0,50–0,52; Cook & Klingner 1991 (steel/concrete) → 0,43; overall published range **0,43–0,65**.

**Physics a fabricator needs:** Baltay & Gjelsvik (1990) found μ for a **mill-scale** surface is *lower* than for a machined surface below ~10 000 psi bearing, because "the mill scale is harder than the steel and is therefore not penetrated by the concrete/grout particles at lower bearing stress levels." → **Blasting/removing mill scale under the plate raises friction; painting it lowers it.**

### 1.5 Grout-to-concrete interface (the *other* sliding plane)

EN 1992-1-1 §6.2.5 (interface between concretes cast at different times), `v_Rdi = c·f_ctd + μ·σ_n (+ ρ f_yd(...)) ≤ 0,5 ν f_cd`:

| Surface | c | μ |
|---|---|---|
| Very smooth | 0,025–0,10 | 0,5 |
| Smooth | 0,20 | 0,6 |
| **Rough (≥3 mm roughness)** | **0,40** | **0,7** |
| Indented | 0,50 | 0,9 |

(rough and indented rows confirmed; smooth/very-smooth rows from the standard's Table — verify before printing.)

Peikko HPM® Technical Manual (06/2024) §1.2.2 makes this a **site instruction**: "The top surface of the base structure … must be **rough (≥ 3 mm** according to EN 1992-1-1 Chapter 6.2.5 and EN 1992-4 Chapter 6.2.2.3), to enable the transfer of shear forces … A smooth surface causes reduced resistance for shear loads."

In the Gomez tests, "**all slip occurs between the grout and the steel base plate, rather than the grout and the concrete pedestal**" — i.e. the steel/grout plane is the weak one, so it is `C_f,d` that governs, not the concrete interface, *provided* the concrete was roughened.

### 1.6 When friction may NOT be used (checklist for the report)

- Column in net tension → `F_f,Rd = 0` (EN 1993-1-8 §6.2.2(6) NOTE).
- Seismic — "most seismic codes do not allow the surface friction mechanism for base plates" (Astaneh-Asl 2008, cited in Gomez §2.3.1).
- Grout layer > ½ anchor diameter (EN 1992-4 route) or > 3 mm (CEB route).
- Concrete edge breakout governs; post-installed anchors; short anchors with pull-out risk (IDEA StatiCa summary of EN 1992-4 / FprEN 1993-1-8 restrictions).
- Uplift/rocking, vibration, fatigue.
- **Do not add friction + anchor bolts** unless slip is prevented (PIP §8.1; EN 1993-1-8 §6.2.2(5) allows the sum only for non-oversized holes).

---

## 2. HOLES IN BASE PLATES vs HOLES IN STRUCTURAL CONNECTIONS

This is the single most misunderstood item on a shop drawing. **A base-plate anchor hole is not a bolt hole.**

### 2.1 Structural bolt holes — the reference point

**EN 1090-2:2018 §6.6.1, Table 11 — nominal clearance = d₀ − d:**

| Bolt d (mm) | 12 | 14 | 16 | 18 | 20 | 22 | 24 | ≥27 |
|---|---|---|---|---|---|---|---|---|
| Normal round | 1 | 1 | 2 | 2 | 2 | 2 | 2 | 3 |
| Oversize round | 3 | 3 | 4 | 4 | 4 | 4 | 6 | 8 |
| Short slot (on length) | +4 | +4 | +6 | +6 | +6 | +6 | +8 | +10 |
| Long slot (on length) | 1,5 d | | | | | | | |

*Verification note: the endpoints "oversize 3 mm (M12) up to 8 mm (M27)" and normal M20 → d₀ = 22, M24 → d₀ = 26 are confirmed by multiple secondary sources; one source instead quotes a flat "d₀ = d + 4" for oversize. **Check Table 11 against a copy of EN 1090-2:2018 before publishing the row values.***

Consequences of oversize holes for *structural* bolts: hardened washers required under head and nut; slip factor reduced by k_s = 0,85; bearing resistance ×0,7 (oversize/short slot), ×0,5 (long slot) — EN 1993-1-8 §3.6.1/§3.9.

**AISC 360 Table J3.3 (nominal hole dimensions, in.):**

| Bolt d | Standard | Oversize | Short slot | Long slot |
|---|---|---|---|---|
| ½ | 9/16 | 5/8 | 9/16 × 11/16 | 9/16 × 1¼ |
| 5/8 | 11/16 | 13/16 | 11/16 × 7/8 | 11/16 × 1 9/16 |
| ¾ | 13/16 | 15/16 | 13/16 × 1 | 13/16 × 1 7/8 |
| 7/8 | 15/16 | 1 1/16 | 15/16 × 1 1/8 | 15/16 × 2 3/16 |
| 1 | 1 1/16 | 1¼ | 1 1/16 × 1 5/16 | 1 1/16 × 2½ |
| ≥1 1/8 | d + 1/16 | d + 5/16 | (d+1/16) × (d+3/8) | (d+1/16) × 2,5 d |

**The AISC escape clause** — AISC 360 §J3.2: maximum hole sizes are per Table J3.3/J3.3M, *"except that larger holes, required for tolerance on location of anchor rods in concrete foundations, are permitted in column base details."*

### 2.2 Anchor-rod holes — AISC Design Guide 1, 2nd ed., **Table 2.3** (read from the source PDF)

> **Table 2.3 — Recommended Sizes for Anchor Rod Holes in Base Plates**

| Anchor rod dia. (in) | Hole dia. (in) | Min. washer dim. (in) | Min. washer thk. (in) | *metric equiv. rod / hole (mm)* | *hole/rod ratio* | *radial clearance (mm)* |
|---|---|---|---|---|---|---|
| ¾ | 1 5/16 | 2 | ¼ | 19,1 / 33,3 | **1,75** | 7,1 |
| 7/8 | 1 9/16 | 2½ | 5/16 | 22,2 / 39,7 | **1,79** | 8,7 |
| 1 | 1 13/16 | 3 | 3/8 | 25,4 / 46,0 | **1,81** | 10,3 |
| 1¼ | 2 1/16 | 3 | ½ | 31,8 / 52,4 | **1,65** | 10,3 |
| 1½ | 2 5/16 | 3½ | ½ | 38,1 / 58,7 | **1,54** | 10,3 |
| 1¾ | 2¾ | 4 | 5/8 | 44,5 / 69,9 | **1,57** | 12,7 |
| 2 | 3¼ | 5 | ¾ | 50,8 / 82,6 | **1,63** | 15,9 |
| 2½ | 3¼ | 5½ | 7/8 | 63,5 / 82,6 | 1,30 | 9,5 |

Notes as printed: (1) circular or square washers meeting the size shown are acceptable; (2) adequate clearance must be provided for the washer size selected; (3) an alternate 1 1/16 in. hole may be used for ¾ in. rods with plates < 1¼ in. thick (allows punching + standard ASTM F844 washers).

**So AISC base-plate holes are typically 1,5 to 1,8 × the rod diameter** — exactly the "1.5×d or more" the brief asked about. The clearance is **+9/16 in to +1¼ in (14 to 32 mm)**, versus +1/16 in (1,6 mm) for a structural bolt. That is a factor of ~10–20.

Rationale (DG1 §2.6): *"The most common field problem is anchor rod placements that either do not fit within the anchor rod hole pattern or do not allow the column to be properly positioned. Because OSHA requires any modification of anchor rods to be approved by the Engineer of Record, it is important to provide as large a hole as possible to accommodate setting tolerances."*

### 2.3 Anchor-rod holes — the tighter Commonwealth/European numbers

**AS 4100 cl. 14.3.5.2 / NZS 3404 cl. 14.3.5.2.2** (ASI Design Guide 7 §4.2 item 7; SCNZ FAQ):
- *"Hole sizes in base plates may be up to **6 mm larger** than the anchor bolt diameter. Holes would normally be **drilled**. Holes require a special plate washer of **6 mm** [AS 4100] / **4 mm** [NZS 3404] minimum thickness under the nut if the bolt hole is more than **3 mm** larger than the anchor bolt diameter."*

**Metric version of DG1 Table 2.3** (SCNZ Table 1, "after Fisher & Kloiber and reproduced in Hogan ASI DG7"):

| Anchor bolt | Hole ⌀ (mm) | Min. washer dim. (mm) | Min. washer thk. (mm) |
|---|---|---|---|
| M16 | 22 | 50 | 6 |
| M20 | 26 | 50 | 6 |
| M24 | 30 | 75 | 10 |
| M30 | 36 | 75 | 12 |
| M36 | 42 | 90 | 12 |

Notes: washer diameters sized to cover the entire hole when the bolt sits at the edge of the hole; circular or square acceptable; check clearance to the column face; washers usually cut from plate or flat bar; **washer thickness ≈ 30–40 % of bolt diameter**.

**UK:** SCI P358 uses **+6 mm** over the anchor size. SCI P398 Worked Example E.1 (305×305×118 UKC, 600×600×50 mm base plate, C30/37) actually uses M24 with **d₀ = 26 mm** — i.e. a *normal* clearance hole. Report both: UK practice runs much tighter than US practice, and this is a real cultural difference between drawings from different offices.

### 2.4 The tightest of all — EN 1992-4:2018 Table 6.1 (clearance hole in the fixture)

For a fastening to be designed **without lever arm**, the hole in the fixture must not exceed `d_f`:

| d or d_nom (mm) | 6 | 8 | 10 | 12 | 14 | 16 | 18 | 20 | 22 | 24 | 30 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| **d_f (mm)** | 7 | 9 | 12 | 14 | 16 | 18 | 20 | 22 | 24 | 26 | 33 |

(EOTA TR 054:2026-03 Table 3.1, which reproduces EN 1992-4 Table 6.1; independently confirmed by Peikko HPM® Technical Manual Table 2: HPM16→18, HPM20→22, HPM24→26, HPM30→33, HPM39→42, cited as "according to EN 1992-4 Table 6.1".)

That is **+2 mm on M24**, versus **+30 mm** for a 1 in. AISC anchor rod. **This is the crux of the whole subject.**

### 2.5 THE CONSEQUENCE — quantify the slip

For a base plate with 4 anchors:

- **Radial clearance** = (d₀ − d)/2. AISC 1 in. rod: (46,0 − 25,4)/2 = **10,3 mm**. EN 1090-2 normal M24: (26 − 24)/2 = **1,0 mm**.
- **Setting tolerance stacks on top.** AISC *Code of Standard Practice* §7.5.1 (as quoted in DG1 §2.8; fraction glyphs decoded):
  - (a) variation between centres of any two anchor rods **within a group** ≤ **1/8 in (3,2 mm)**
  - (b) between centres of **adjacent groups** ≤ **1/4 in (6,4 mm)**
  - (c) variation in **elevation** of rod tops ≤ **± 1/2 in (± 12,7 mm)**
  - (d) accumulated along an established column line ≤ 1/4 in per 100 ft, max 1 in total
  - (e) group centre to established column line ≤ 1/4 in
  - By contrast **ACI 117-90 §2.3 allows ±1 in (±25 mm)** for embedded items. DG1 §2.8: *"ACI 117 is much more generous for embedded items than the AISC Code of Standard Practice is for anchor rod tolerances"* — therefore **specify AISC (or an equivalent explicit table) in the concrete specification**, or the rods will legally be 25 mm out.
- **Result:** with a ±3,2 mm in-group tolerance and 10,3 mm radial clearance, the four rods sit at four *different* offsets. The plate slides ~10 mm, then **one** rod touches; the second touches only after further travel; the fourth may never touch at all.

Code and guide statements to quote:

- **EN 1993-1-8 §6.2.2(5)** — anchor bolt shear may be added *"in cases where the bolt holes are not oversized"*. Oversized ⇒ **F_v,Rd = F_f,Rd only**.
- **AISC DG1 §3.5.3** — *"Using the AISC-recommended hole sizes for anchor rods, which can be found in Table 2.3, considerable slip of the base plate may occur before the base plate bears against the anchor rods. The effects of this slip must be evaluated by the engineer. … due to placement tolerances, not all of the anchor rods will receive the same force. The authors recommend a cautious approach, such as **using only two of the anchor rods to transfer the shear**, unless special provisions are made to equalize the load to all anchor rods."*
- **SCI P398 §5.2** — *"It is not reasonable to assume that horizontal shear is distributed evenly to all the bolts passing through clearance holes in the baseplate, **unless washer plates are welded over the bolts in the final position**."*
- **SCI P398 STEP 4** — *"As the bolts are in clearance holes, some may not be in contact with the plate at all. This may be overcome by assuming that not all the bolts are effective."*
- **HERON §5.2 Remark** — *"The hole clearance may contribute considerably to the horizontal displacements. The hole clearance is not included in the above equations. Displacements due to the hole clearances may be prevented by measures to prevent the bolts moving in the holes, e.g. by **filling the hole clearances with a two component epoxy**"* (ECCS Publication No. 79, injection bolts).
- **Peikko** offers two production solutions for the annular gap: a **spring pin to EN ISO 13337** (Ø d_f1: HPM16→20, HPM20→25, HPM24→30, HPM30→40, HPM39→50 mm) or **injection grout of ≥ 40 N/mm²** squeezed in until it extrudes. Required for fatigue and seismic shear.
- **EN 1992-4 §6.3 (via TR 054 §6.3)** — *"An annular gap between an anchor and its fixture should be avoided in seismic design situations."*

---

## 3. PLATE WASHERS OVER OVERSIZED HOLES

### 3.1 Function

Two different jobs — do not confuse them:
1. **Pull-through** (tension): a standard washer collapses into a 46 mm hole. DG1 §2.6: *"The washer may be either a plain circular washer or a rectangular plate washer as long as the thickness is adequate to prevent pulling through the hole … the pull-through criterion requires appropriate **stiffness as well as strength**."*
2. **Shear transfer** (this brief): a **welded** plate washer with a *tight* hole converts an oversized hole into a normal hole, so all anchors bear at once.

### 3.2 Sizing

| Parameter | Value | Source |
|---|---|---|
| Washer **plan size** | sized to cover the entire hole when the rod sits at the **edge** of the hole; geometrically `≥ 2·d₀ − d` | DG1 §2.6 + Table 2.3 |
| Washer **thickness** | ≈ **⅓ of the anchor rod diameter** (DG1 Table 2.3 & Design Guide 7); **30–40 %** of bolt diameter (SCNZ/ASI); min **6 mm** (AS 4100) / **4 mm** (NZS 3404) whenever hole > d + 3 mm | DG1 §2.6; ASI DG7 §4.2(7) |
| Washer **hole**, when used for shear | **d + 1/16 in (d + 1,6 mm)** | **DG1 §3.5.3** |
| Material | **plain, NOT hardened.** *"Washers for anchor rods are not, and do not need to be, hardened"* (DG1 §2.6). PIP: *"If the design requires welding the washer to the base plate, **plain washers or steel plate (rather than hardened washers) must be specified to ensure that a good weld can be produced**."* ASTM F436 hardened washers are also *"generally of insufficient size"* (DG1 §2.6). | DG1 §2.6; PIP STE05121 §8.1 |
| Fabrication | *"Plate washers are usually custom fabricated by **thermal cutting** the shape and holes from plate or bar stock."* | DG1 §2.6 |
| When to weld | *"Washers should **not** be welded to the base plate, **except when the anchor rods are designed to resist shear** at the column base (see Section 3.5)."* | DG1 §2.6 |

### 3.3 The welding rule (design intent)

DG1 §3.5.3: *"Lateral forces can be transferred equally to all anchor rods, or to selective anchor rods, by using a **plate washer welded to the base plate between the anchor rod nut and the top of the base plate**. The plate washers should have holes **1/16 in. larger** than the anchor rod diameter. Alternatively, to transfer the shear equally to all anchor rods, a **setting plate** of proper thickness can be used and then **field welded** to the base plate after the column is erected."*

Sequence (this is what goes on the drawing):
1. Erect, plumb and level the column on shims/levelling nuts.
2. **Then** drop the plate washers over the rods, with the rods hard against the washer hole in the direction the shear will push.
3. **Then** fillet-weld the washers to the base plate (site weld).
4. **Then** grout.

Real tested detail (Gomez et al. §3.3.1.2):
- ¾ in. rods: machined square plate washers **2,5 × 2,5 × ¼ in (64 × 64 × 6,4 mm)**, hole **0,8 in (20,3 mm)** ≈ rod + 1/16 in.
- 1¼ in. rods: **3,5 × 3,5 × ½ in (89 × 89 × 12,7 mm)**, hole **1,3 in (33 mm)** ≈ rod + 1/8 in, thermally cut.
- **An additional loose washer was placed over the welded plate washer to prevent dishing of the welded washer under large rod tension.** Nuts snug + 1/8 turn.

### 3.4 Eurocode/EN 1090-2 side

EN 1090-2:2018 §8 (Bolting) requires washers under the turned part for non-preloaded assemblies and hardened washers for preloaded assemblies in oversize/slotted holes; taper washers where surfaces are not normal to the axis. **EN 1090-2 does not give a base-plate "washer plate" table** — the sizes above come from AISC / ASI / NZS. In the Eurocode world the design rule is the SCI one: *"washer plates with precise holes can be positioned over the bolts, and site welded to the base, ensuring that the bolts are all in bearing and that load is distributed evenly"* (SCI P398 STEP 4).

Note for the Israeli report: EN 1090-2 governs execution (hole types, washers, tolerances) and is what the fabrication class (EXC2/EXC3) is written against; EN 1993-1-8 governs the joint resistance; EN 1992-4 governs the anchorage into the concrete. Israel's steel design standard **ת"י 1225 חלק 1.1** (published June 2023, coexisting with the older ת"י 1225 חלק 1 for 3 years per the SII listing) follows the EN 1993 route — confirm the exact current designation and NA values with SII before printing clause references.

### 3.5 Weld design for the plate washer

Neither DG1 nor SCI gives a formula. Practical basis: the washer must deliver the anchor's design shear into the plate. Design a fillet weld all round the washer for `V_Ed,anchor` in shear (plus a small couple from the eccentricity `t_w/2`). Typical: 6 mm fillet, 3 sides or all round, on a 100×100×10 washer for M24. Keep the weld **off** the hole edge so the bearing zone is not heat-affected.

---

## 4. SHEAR LUG / SHEAR KEY / SHEAR STUB / NIB

Terminology: **shear lug** (AISC/ACI), **shear key** (general/US), **shear stub** (SCI/UK), **nib** (UK colloquial), **Schubknagge** (DE). In Hebrew reports the usual term is **מפתח גזירה** or **בליטת גזירה**.

### 4.1 When it is needed

- SCI P398 STEP 4: *"If the shear force cannot be transferred by friction or by the holding down bolts"* → shear stub, embedding the column, tie bars, or casting a slab around the column.
- PIP §9: *"Normally, friction and the shear capacity of the anchors used in a foundation adequately resist column base shear forces. In some cases, however, the engineer may find the shear force too great… **If the total factored shear loads are transmitted through shear lugs or friction, the anchor bolts need not be designed for shear.**"*
- ACI 318-19 §17.11.1.1.2: with a shear lug, *"anchor failure modes associated with shear loading do not need to be checked; therefore, the shear lug is assigned the entire shear demand"* — but **a minimum of four anchors is required**, and the anchors must still be checked for tension and for the tension/shear interaction of §17.11.1.1.3.

### 4.2 AISC DG1 §3.5.2 — bearing model (ACI 349-01 App. B basis)

```
φP_n = 0,80 f'c · A  +  1,2 (N_y − P_a)          for shear lugs
φP_n = 0,55 f'c · A_brg + 1,2 (N_y − P_a)        for bearing on a column or the side of a base plate
```
- `A` = **embedded** area of the lug — *"this does not include the portion of the lug in contact with the grout above the pier"*
- `A_brg` = contact area between base plate/column and concrete
- Confinement term: `φK_c(N_y − P_a)` with φ = 0,75 and **K_c = 1,6** → 1,2(N_y − P_a); `N_y = n·A_se·F_y` (yield of the tension anchors); `P_a` = factored external axial load (+ tension, − compression)
- Origin: ACI 349-01 §B.4.5.2 gives `φ·1,3 f'c A_1`; with φ = 0,60 → **≈ 0,80 f'c A_1**

**Concrete breakout in front of the lug** (DG1 §3.5.2 item 1, from ACI 349-01 App. B):
> *"the concrete design shear strength for the lug shall be determined based on a uniform tensile stress of **4φ√f'c** acting on an effective stress area defined by projecting a **45° plane** from the bearing edge of the shear lug to the free surface."* Bearing area of the lug excluded. **φ = 0,75.** (f'c in psi.)

**DG1 detailing rules for lugs (§3.5.2, items 2–4):**
2. *"As a rule of thumb, the authors generally require the **base plate to be of equal or greater thickness than the shear lug**."* Consider weak-axis bending of the plate from the lug force.
3. Multiple shear lugs permitted; ACI 349-01 App. B gives spacing criteria.
4. *"**Grout pockets must be of sufficient size for ease of grout placement. Nonshrink grout of flowable consistency should be used.**"*

### 4.3 AISC DG1 Example 4.9 — full worked shear lug (imperial, as printed)

W10×45 column, additional wind shear 23 kips nominal, f'c = 4 000 psi, pier 20 in wide, grout G = 2 in, lug/plate width 9 in. Anchors sized only for uplift → confinement term ignored.

```
V_u = 1,6 × 23 = 36,8 kips
Bearing:  (0,8)(4 000)(A)_req = 36 800     →  A_req = 11,5 in²
d = 11,5 / 9 = 1,28 in                      →  use d = 1,5 in embedment
Breakout:  a = 5,5 in (lug centred in 20 in pier);  B = 1,5 + 9,5 = 11,0 in
           A_v = (20)(11,0) − (1,5)(9) = 207 in²
           V_u = 4 φ √f'c · A_v = 4(0,75)√4000 × 207 / 1000 = 39,2 kips > 36,8  OK
Lug bending (cantilever):
           M_l = V (G + d/2) = 36,8 (2 + 1,5/2) = 101 kip·in
           Z = b t²/4 ;  φM_n = φ F_y b t²/4
           t_req = √[4 M_l /(φ F_y b)] = √[4(101)/(0,90 × 36 × 9)] = 1,18 in  →  use t = 1¼ in
           ⇒ base plate ≥ 1¼ in
Weld (fillet each side of lug):
           s = 1,25 + 0,3125(1/3)(2) = 1,46 in       (lever arm to weld centroid)
           f_c = M/(s·b) = 101/(1,46 × 9)  = 7,71 kip/in   (tension/compression couple)
           f_v = 1,6(23)/9                = 2,05 kip/in   (shear)
           f_r = √(7,71² + 2,05²)         = 7,98 kip/in
           5/16 in E70 fillet: φF_w = 0,75(0,60)(70) = 31,5 ksi
                               capacity = 0,3125 × 0,707 × 31,5 = 6,96 kip/in  < 7,98  NG
           ⇒ use 3/8 in fillet welds
```

### 4.4 PIP STE05121 §9 — the cleanest fabricator-facing procedure

```
V_app = V_ua − V_f                                   (shear left over after friction)
A_req = V_app / (0,85 φ f'c)         with φ = 0,65   (required bearing area)
H     = A_req / W  +  G                              (total lug height, W = lug width, G = grout thickness)
M_u   = (V_app / W) · (G + (H − G)/2)                (moment per unit width, cantilever)
t     = √[ 4 M_u / (0,9 f_ya) ]                      ("The shear lug should not be thicker than the base plate")
V_cb  = A_Vc · 4 φ √f'c              with φ = 0,85   (ACI 349-01 App. B §B.11 breakout, 45° projection)
```

**PIP Example 3** (14 in square base plate, factored DL 22,5 k, LL 65 k, V_u = 40 k, f_ya = 36 ksi, f'c = 3 ksi, grout G = 1 in, 2 ft square pedestal):

```
V_f    = 0,55 × 22,5 = 12,4 k    →  V_app = 40 − 12,4 = 27,6 kips
A_req  = 27,6 / (0,85 × 0,65 × 3) = 16,67 in²
W = 12 in  →  H = 16,67/12 + 1 = 2,39 in  →  use H = 3 in
M_u    = (27,6/12)(1 + (3−1)/2) = 4,61 kip·in/in
t      = √[4(4,61)/(0,9 × 36)] = 0,754 in  →  use ¾ in
Lug = 12 × 3 × ¾ in
Breakout: edge = (24 − 0,75)/2 = 11,63 in ;  A_V = 24(2 + 11,63) − (12 × 2) = 303 in²
V_cb = 303 × 4 × 0,85 × √3000 = 56 400 lb = 56,4 kips > 27,6  OK
```

Additional PIP §9 fabrication rules:
- *"The bearing on the shear lug is applied only on the portion of the lug adjacent to the concrete. Therefore, the engineer should **disregard the portion of the lug immersed in the top layer of grout**."*
- *"Grout must **completely surround** the lug plate or pipe section and must **entirely fill the slot** created in the concrete."*
- *"When using a **pipe section**, a hole approximately **2 in. in diameter** should be drilled through the base plate into the pipe section to allow grout placement and inspection."*

### 4.5 SCI P398 STEP 4 — the Eurocode/UK "shear stub" model (metric, ready to use)

Shear stubs are commonly **I-sections** welded under the plate into a pocket (Fig. 5.5). Design model (Fig. 5.6): triangular bearing on the vertical faces, **grout space ignored**, peak stress = `f_cd` of the weaker of concrete/bedding.

**Sizing rules of thumb:**
- stub section depth `h_s ≈ 0,4 × column section depth h_c`
- effective embedded depth `d_eff > 60 mm` and `≤ 1,5 × h_s`
- flange outstand slenderness `b_n/t_fn ≤ 20`

**Resistance (two-flanged section, I or H):**
```
V_Rd = b_s · d_eff · f_cd
```
**Secondary moment (eccentricity between applied shear and the reaction):**
```
M_sec,Ed = V_Ed · ( h_g + d_eff/3 )                h_g = grout thickness
N_sec,Ed = M_sec,Ed / ( h_s − t_fs )               force in the stub flange
Flange resistance = b_s · t_fs · f_ys / γ_M0
```
**Welds:** flange-to-plate weld designed as a **transverse** weld for `N_sec,Ed`; web-to-plate weld as a **longitudinal** weld for `V_Ed`.

**Column web check** for the concentrated flange force, effective breadth:
```
b_eff = t_fs + 2 s + 5 t_p          s = leg length of the weld to the stub flange, t_p = base plate thickness
```
**Shear resistance of the stub itself:**
```
V_Rd = A_vs · f_ys / (√3 γ_M0)
```

### 4.6 ACI 318-19 §17.11 — the current US code (shear lugs are now codified)

New in ACI 318-19 (was ACI 349 App. B / ACI 349.2R-07). Two concrete checks; steel design of the lug still per AISC DG1.

**Bearing, §17.11.2:**
```
V_brg,sl = 1,7 f'c · A_ef,sl · ψ_brg,sl            φ = 0,65
```
- `A_ef,sl` = effective bearing area — *"defined by the width of the shear lug perpendicular to the direction of the applied shear load, and a projected distance based on twice the shear lug thickness (t_sl)"*. Excludes the embedded base plate (cast-in lugs) and the portion above the concrete surface (post-installed lugs). ⇒ **the grout layer is explicitly ineffective.** *(Wording as summarised by Hilti and ASDIP — verify against the code text before printing, since the 2 t_sl limit is easy to misread.)*
- `ψ_brg,sl` = confinement modifier: **< 1,0 under net tension**, **= 1,0 with no axial load**, **> 1,0 under compression** (Eq. 17.11.2.2.1a/b/c). External moments producing tension/compression across the fracture surfaces are **not** counted.
- **Multiple lugs** may be summed *"provided the stress on a shear plane in the concrete at the bottom of the shear lugs, and extending between them, does not exceed **0,2 f'c**."*

**Concrete breakout, §17.11.3:** the anchor breakout equations with `c_a1` measured **from the bearing surface of the lug to the fixed edge**, `A_Vc` bounded by `1,5 c_a1` each side and `1,5 c_a1` below `h_ef,sl`; modifiers `ψ_ed,V`, `ψ_c,V` (1,0 cracked / **1,4** uncracked), `ψ_h,V`, `λ_a`. **φ = 0,65.** Special cases for edges parallel to the load, corners, and multiple lugs (§17.11.3.2–3.4).

**Both** `φV_brg,sl ≥ V_u` and `φV_cb,sl ≥ V_u` must be satisfied.

### 4.7 Test evidence — the 45° cone method is UNCONSERVATIVE for big footings

Gomez et al. 2009, Tests #6 and #7: I-shaped lug fabricated from plate, welded with 1,5 in fillet welds to the centre of the plate; bearing width **6 in (152 mm)**; lug length **7 in** (Test #6) and **4,5 in** (Test #7); grout pockets **9 × 9 in in plan, 7 in and 4 in deep**; shims to give **1,5 in grout** → embedment **5,5 in and 3,0 in** below the concrete surface; distance from the lug bearing face to the pedestal edge **≈ 20,25 in**.

Results:
- Two force peaks: the first (flexural splitting of the pedestal) with a **20–40 % load drop**, then a rise to a second peak with diagonal cracks ~30° to the load direction.
- **45° cone method (AISC DG1 / ACI 349): mean test/predicted = 0,51, COV = 0,14 → unconservative by ~2× for large edge distances**, because of the size effect in concrete.
- **CCD method: mean test/predicted = 1,07, COV = 0,19.**
- Recommendation: *"the reliable strength of concrete blowout due to shear key bearing be calculated as the **minimum of the two** previously described estimates."*
- *"The longer shear key is observed to be stronger on a unit basis… The larger local bearing stresses of a shorter lug may increase the likelihood of early crack initiation."*

**This is why ACI 318-19 moved to a CCD-type breakout equation for shear lugs. Report it — it is the single biggest recent change in this area.**

### 4.8 Typical dimensions to put on a drawing

| Item | Typical |
|---|---|
| Lug type | flat plate (uniaxial shear) or short I/H stub or pipe stub (biaxial shear) |
| Lug thickness | 20–32 mm plate; **never thicker than the base plate** (DG1 §3.5.2, PIP §9.2d) |
| Lug width | ≈ column flange width up to base-plate width; 150–350 mm typical |
| Embedded depth `d_eff` | ≥ 50 mm (PIP: "minimum lug height = grout thickness + 2 in"); SCI: > 60 mm and ≤ 1,5 × stub depth |
| Total projection below plate | grout thickness + `d_eff` (e.g. 30 + 95 = 125 mm) |
| Grout pocket | lug plan size + ≥ 75 mm all round; ≥ 50 mm deeper than the lug; e.g. 300×300×200 for a 160×160 stub |
| Welds | fillets both sides; DG1 Ex. 4.9 needed 3/8 in (10 mm); size for the couple `M_l/(s·b)` **plus** the direct shear |
| Grout | non-shrink, **flowable** consistency; ASI DG7: characteristic **cube** strength ≥ **2 × the foundation concrete** |
| Base plate | grout inspection/vent holes Ø50 mm, one per 0,5 m² of plate (SCI P398 §5.4, for plates ≥ 700×700); Ø100 mm if grouting through them. ASI DG7: plates > 600 mm get a 50–75 mm hole. |

---

## 5. EMBEDDING / RECESSING THE BASE PLATE

### 5.1 Why it works

The contact plane moves **below** the top of the concrete, so (i) the friction coefficient rises sharply and (ii) the plate edge itself bears directly on concrete.

- **PIP STE05121 §8.2:** μ = **0,90** when the contact plane is a full plate thickness below the concrete surface, vs **0,55** for the normal grouted base. **That is a 64 % increase in friction capacity for free.**
- **AISC DG1 §3.5.2:** for bearing against an embedded base plate or column section where the bearing area is adjacent to the concrete surface, ACI 318 gives
  ```
  φP_ubrg = 0,55 f'c · A_brg          A_brg = contact area of plate edge and/or column against concrete
  ```
  plus the confinement term `1,2(N_y − P_a)`.
- **SCI P398 STEP 4:** shear may be transferred *"Directly, by setting the base plate in a shallow pocket which is filled with concrete"* and *"Embedding the column in the foundation."*

### 5.2 AISC DG1 Example 4.8 — shallow embedment (worked)

W12×50, base plate **15 × 15 × 1,5 in**, 6 000 psi grout, factored shear **100 kips**:
```
Bearing on the base plate edge:  A_brg = 1,5 × 15 = 22,5 in²
   φV = 0,6 × 0,85 f'c A_brg = 0,6(0,85)(6)(22,5) = 68,8 kips
Remaining 100 − 68,8 = 31,2 kips must bear on the column flange (b_f = 8,08 in):
   A_brg,req = 31,2 / (0,6 × 0,85 × 6) = 10,2 in²
   flange embedment = 10,2 / (2 × 8,08) = 1,26 in  (2 flanges)
⇒ total embedment ≈ 4 in (100 mm) for flange + base plate
```
**So a 100 mm recess replaces the entire shear-lug/anchor problem for a 445 kN shear.** This is often the cheapest fabrication answer.

### 5.3 The "blockout" detail (very common in buildings)

Kanvinde et al., AISC LRR-2022-01, Ch. 3:
- The column passes through a **diamond-shaped blockout** (rotated 45° to the gridlines) left in the slab-on-grade; after erection the blockout is filled with plain concrete/grout, making a cold joint with the slab.
- **Depth of the blockout is usually 200–400 mm.**
- The diamond shape steers shrinkage cracks along the control joints.
- Embedment gives **as much as +50 % strength** compared with the exposed base plate alone (Richards et al. 2018; Hanks & Richards 2019; Grilli et al. 2017), *"which cannot be disregarded without introducing major conservatisms."*
- AISC Design Guide 1 **3rd edition (2024)** adds a whole new chapter on **embedded base connections**, plus seismic design; design procedures now based on **ACI 318-19**.

### 5.4 Fabrication implications of a recessed base

- The base plate must be **smaller than the pocket** by an erection clearance; the pocket must be reinforced (links/ties) to contain the bursting force.
- The recess must be **formed, not chipped** — a chipped pocket has no reliable bearing face.
- The plate edge bearing face should be a **sheared/sawn edge**, and the plate should be **level**, not tilted, or the contact area is a line.
- All void must be filled with flowable non-shrink grout; provide air-escape holes.

---

## 6. SHEAR VIA THE ANCHORS THEMSELVES — LEVER ARM, BENDING, GROUT

### 6.1 Why the anchor is not a bolt

- The grout *cannot* provide the bearing support a steel plate does: *"Because the grout does not have sufficient strength to resist bearing stresses between the bolt and the grout, **considerable bending of the anchor bolts may occur**."* (HERON §1)
- Therefore the anchor is a **short cantilever/beam in double or single curvature**, not a bolt in double shear.
- HERON §6.1 conclusion: *"**The shear strength of anchor bolts is considerably lower than the shear strength of bolts in bolted connections between steel plates.**"*

### 6.2 EN 1992-4:2018 §6.2.2.3 — "Shear loads with and without lever arm"

Shear may be assumed to act **without lever arm** only if **all** of:
1. The fixture is **steel** and in the anchorage area is fixed **directly** to the concrete **without an intermediate layer**, or with a **levelling layer of mortar of compressive strength ≥ 30 N/mm² and thickness ≤ d/2**;
2. The fixture is in contact with the anchor over a length of at least **0,5 t_fix**;
3. The clearance hole diameter `d_f` does not exceed **Table 6.1** (§2.4 above).

Otherwise, with lever arm:
```
M_Ed = V_Ed · l / α_M                                   ... EN 1992-4 (via EOTA TR 054 Eq. 3.1)
l = a_3 + e_1
   e_1 = distance between the shear load and the surface of the member
   a_3 = 0,5 d                    (no clamping at the concrete surface)
       = 0                        (if a washer and a nut are directly clamped to the surface)
   d   = anchor bolt / thread diameter
α_M = 1,0   no restraint — fixture can rotate freely. "This assumption is always conservative."
α_M = 2,0   full restraint — ONLY if the fixture cannot rotate AND the hole clearance is
            ≤ Table 6.1, or the anchor is clamped by nut and washer.
            "If restraint of the anchor is assumed the fixture shall be able to take up the
             restraint moment."
```
CEB (1996) stated the same limit differently: *"Full restraint (α_M = 2,0) may be assumed only if the fixture cannot rotate and the **hole is smaller than 1,2 d**."* (HERON §3.2). **Both formulations put a hard cap on hole size for α_M = 2,0.**

**Practical lever arm for a real base plate** (identical in three independent sources):
```
l_a = t_grout + 0,5 d_nom + 0,5 t_bp
```
- IDEA StatiCa (EN 1992-4 implementation): `l_a = 0,5·d_nom + t_mortar + 0,5·t_bp`, `α_M = 2` per EN 1992-4 §6.2.2.3
- Peikko HPM® Manual Table 8: `t_R = t_Grout + d_b/2 + t_Fix/2`
- Hilti (EN 1992-4 route): `l_a = e_1 + a_3`, `e_1` = concrete surface → **centreline of the steel plate**; `a_3 = 0,5 d` unclamped, 0 clamped

### 6.3 EN 1992-4 §7.2.2.3.2 — steel failure with lever arm

```
V_Rk,s,M = α_M · M_Rk,s / l_a                          ... EN 1992-4 Eq. (7.37)
M_Rk,s   = M⁰_Rk,s · ( 1 − N_Ed / N_Rd,s )
M⁰_Rk,s  = 1,2 · W_el · f_uk            [EN 1992-4; also ETAG 001 Annex C: M⁰ = 1,2 S f_u,min]
W_el     = π d³ / 32     (d = reduced/stress-area diameter if the shear plane is in the thread)
γ_Ms     = max( 1,0 · f_uk/f_yk ; 1,25 )   for f_uk ≤ 800 MPa and f_yk/f_uk ≤ 0,8
         = 1,5                              otherwise
```
> **Discrepancy to flag:** Hilti's ungrouted stand-off paper writes `M⁰_Rk,s` "generally taken as **1,5** W_el f_uk", while EN 1992-4, ETAG 001 Annex C, EOTA TR 054 and IDEA StatiCa all use **1,2**. Peikko's ETA values back-calculate to 1,2 (see §6.6). **Use 1,2 unless an ETA gives a product-specific value.**

Hilti PROFIS restates the same equation in US notation: `V_s^M = α_M·M_s/L_b`, `M_s = M_s⁰(1 − N_ua/φN_sa)`, `M_s⁰ = 1,2·S·f_u,min`, `S = π d_nominal³/32` (cast-in) or `π d_minor³/32` (post-installed), `L_b = z + n·d₀`, `n = 0,5` (no clamping) or `0` (clamping), `α_M` default **1,0** for stand-off without/with clamping and **2,0** for **stand-off with grouting**.

### 6.4 EN 1992-4 §6.2.2.3(2) — the grouted alternative (no lever arm, with a penalty)

If the joint is grouted and all five conditions below hold, the lever arm may be avoided and instead:
```
V_Rk,s = ( 1 − 0,01 · t_grout ) · k₇ · V⁰_Rk,s          t_grout in mm      ... EN 1992-4 Eq. (7.36) route
```
Conditions (Hilti, *Method for Anchor Design in Grouted Stand-off Connections*, v1.2 July 2023, §2.2.2, citing EN 1992-4 §6.2.2.3(2)):
1. At least **two fasteners spaced ≥ 10 d apart** resist shear in the direction of the shear force;
2. **No bending moment or net tension** on the connection;
3. **Grout thickness ≤ min(40 mm, 5 d)** (5 d₀ for sleeve anchors);
4. The grout **completely fills** the void between plate and concrete;
5. Grout compressive strength **≥ that of the concrete and ≥ 30 N/mm²**.

Penalty in numbers: 20 mm grout → ×0,80; 30 mm → ×0,70; **40 mm → ×0,60**.

Base value: `V⁰_Rk,s = k₆ A_s f_uk`, with **k₆ = 0,6** for `f_uk ≤ 500 MPa`, **0,5** otherwise; extra ×0,8 if `h_ef/d_nom < 5` and concrete < C20/25.

**ACI equivalent:** ACI 318 §17.7.1.2.1 — *"where anchors are used with a built-up grout pad, the nominal shear strength shall be multiplied by **0,80**."* DG1 §3.5.3 comments: *"No explanation of the reduction is provided; however, it is the authors' understanding that the requirement is to adjust the strength to account for **bending of the anchor rods within the grout pad**… the reduction is not required when AISC combined bending and shear checks are made on the anchor rods, and the resulting area of the anchor rod is 20 % larger than the rod without shear."*

### 6.5 EN 1993-1-8 §6.2.2(7) — the steelwork code's own anchor-shear rule

```
F_2,vb,Rd = α_bc · f_ub · A_s / γ_M2         γ_M2 = 1,25
α_bc = 0,44 − 0,0003 f_yb        235 ≤ f_yb ≤ 640 N/mm²
                                 (400 ≤ f_ub ≤ 800 N/mm², per the HERON proposal)
```
| Bolt class | f_yb | α_bc | vs. α_v = 0,6 for a normal bolt |
|---|---|---|---|
| 4.6 | 240 | 0,368 | **61 %** |
| 5.6 | 300 | 0,350 | 58 % |
| 8.8 | 640 | **0,248** | **41 %** |
| 10.9 | 900 | (outside range — **not permitted**) | — |

Origin (HERON §2.3 / NEN 6770): the simplified anchor rules were `0,375 f_ub A_s/γ_Mb` for grade 4.6 and `0,25 f_ub A_s/γ_Mb` for grade 8.8, vs `0,60 f_ub A_s/γ_Mb` for ordinary bolts. **The difference is ductility, not strength**: *"the lower ductility of 8.8 grade bolts compared to 4.6 grade bolts is reflected in the lower coefficient in the resistance function"* (HERON §6.1). **Practical rule: use 4.6 / F1554 Gr.36 anchors when shear must go through them.**

### 6.6 Real product numbers — Peikko HPM® rebar anchor bolts (ETA-based)

| | HPM 16 L | HPM 20 L | HPM 24 L | HPM 30 L | HPM 39 L |
|---|---|---|---|---|---|
| `V⁰_Rk,s` no lever arm (kN) | 43,1 | 67,3 | **96,9** | 154,2 | 268,3 |
| `M⁰_Rk,s` (N·m) | 183 | 356 | **616** | 1236 | 2837 |
| `M⁰_Rd,s` (N·m), γ_Ms = 1,5 | 122 | 237 | **410** | 823 | 1891 |
| **Max grout `t_Grout` ≤ 0,5 d_b (mm)** | < 8 | < 10 | **< 12** | < 15 | < 19 |
| Seismic: max joint height | = t_Grout above (no more) | | | | |
| Standard grout thickness on site (mm) | 8 | 10 | 12 | 15 | 19 |
| Bolt protrusion h_b (mm) | 105 | 115 | 130 | 150 | 180 |
| Installation tolerance (mm) | ±1 | ±1 | ±1 | ±1,5 | ±1,5 |
| Max hole in fixture d_f (mm) | 18 | 22 | **26** | 33 | 42 |
| Spring-pin hole d_f1 (mm) | 20 | 25 | 30 | 40 | 50 |
| Max install torque, general (N·m) | 20 | 45 | 75 | 125 | 290 |
| γ_Ms | 1,5 | | | | |

`V_Rd,s = α_m · M⁰_Rd,s / t_R`, `t_R = t_Grout + d_b/2 + t_Fix/2`; α_m = **1,0** standard installation, **2,0** stand-off installation. Displacements under shear: δ_V0 = 1,5 mm short-term, δ_V∞ = **2,3 mm** long-term (at V = 41 kN for HPM24).

### 6.7 Deformations — the real design driver

HERON §1 and §2: in the Stevin Laboratory tests (Delft, 1989), *"the deformations at rupture of the anchor bolts were between about **15 and 30 mm**, whilst grout layers had a thickness of 15, 30 and 60 mm."*

```
v_r = v + 0,5 d_b                (effective grout thickness in the model; also the CEB lever arm)
δ*_v = 2 v_r f_yb / E_a          (displacement at bolt yield)
δ_v1 = δ*_v · F_v,Sd / F*_v      ;    δ_v = max(δ_v1, δ_v2)
```
Worked check from HERON §2.2, test DT6 — **use this in the report, it is a real test**:
- 2 × M20 grade 8.8, measured f_ub = 1076 N/mm², f_y,b = 861 N/mm² (0,8 f_ub), A_s = 245 mm², ε_u = 12 %
- sand-cement mortar, **v = 30 mm** → v_r = 30 + 0,5(20) = **40 mm**
- tensile force in the column held at F_t = 141 kN (i.e. **net uplift**)
```
δ_h = 2 v_r f_y,b / E = 2(40)(861)/210 000 = 3,6 mm
F_h = [δ_h/√(δ_h² + v_r²) + f_w] · A_s f_y,b − f_w F_t
    = [3,6/√(3,6²+40²) + 0,20](245)(861)/1000 − 0,20(141) = 94 kN   at 3,6 mm slip
    at δ_h = 15 mm:  F_h = 227 − 28 = 199 kN
Design value from EN 1993-1-8:   F_v,Rd = 105 kN
Failure: rupture of the anchor bolts at the edge of the base plate, due to local high bending strains.
```
HERON §6.1 conclusions worth printing verbatim:
- *"The influence of a tensile force F_t in the column can be neglected for the determination of the shear resistance."*
- *"**The shear resistance is almost independent of the thickness of the grout layer. The deformations are greatly dependent on the thickness of the grout layer.**"*
- *"A 'better' grout, e.g. 'Pagel IV' gives lower deformations."*
- *"In the design, not only the shear resistance should be checked, but also the **deformations at serviceability and ultimate limit state**."*
- The CEB model is *"very conservative… especially when a large tensile force is present and/or the thickness of the grout layer is large"* because it ignores the beneficial confinement of the grout.

### 6.8 Anchor-rod bending — what the AISC tests actually saw

Gomez et al., Tests #4 (¾ in rods) and #5 (1¼ in rods), 4 rods at 24 in square, **2 in thick base plate with 2 1/16 in holes**, welded plate washers, grout 1–1¼ in, ASTM F1554 Gr.55, threads in the shear plane (root Ø 0,64 in and 1,10 in), constant **tension** = 31 % and 39 % of rod ultimate:

- *"the anchor rods initially bend in reverse curvature over a distance measured from the **top of the grout layer to the center of the plate washer** welded to the top surface of the base plate. However, **this bending length increases as the grout sustains damage** due to cyclic inelastic loading."*
- Grout cracked at the **0,2 in (5 mm)** slip cycle in both tests. Test #4 rod fractured **¼ in above the concrete surface, inside the grout**; Test #5 rod fractured **~1 in below the concrete surface**, with local concrete spalling cones **2–4 in diameter and 1–1¼ in deep** around each rod.
- Base plate rose vertically **0,35 in (9 mm)** (Test #4) and **0,50 in (13 mm)** (Test #5) during the cyclic lateral loading — i.e. the connection **jacks itself up** as the rods deform.
- *"the inside edge of the base plate hole impinges on some or all anchor rods, thereby resulting in a sudden decrease in the effective bending length"* — i.e. at large slip the **hole edge** becomes the bearing point, not the plate washer.
- *"Asymmetrical response was observed for both tests, attributed to the **irregular placement of anchor rods in the holes during construction** which induced constrained rod bending in one loading direction."*
- **Conclusion:** *"the method that neglects the effect of flexure is determined to be significantly unconservative. Thus, it is recommended that **flexure be considered in the design of anchor rods to resist shear**."* The DG1 lever arm (base-plate thickness + half the plate-washer thickness) is *"a reasonably conservative strength estimate for design."*

### 6.9 AISC combined bending+shear check — DG1 Example 4.11 (worked, imperial)

W10×45, wind shear 23 kips (LRFD V_u = 36,8 k), uplift P_u = 69,8 k, four rods, plate washers **welded** so all four share, base plate 1 in thick, washer 1/8 in:
```
Lever arm (half of base plate + washer):  (1,0 + 0,125)/2 = 0,563 in
Trial 1 1/8 in rods (A = 0,994 in²):
   f_v  = 36,8/(4 × 0,994) = 10,5 ksi
   M_l  = (36,8/4)(0,563)  = 5,18 kip·in ;   S = πd³/32 = π(1,125)³/6? → S = d³/6 = 0,237 in³
   f_tb = 5,18/0,237 = 21,9 ksi ;   f_ta = 69,8/(4 × 0,994) = 17,6 ksi
   f_t  = 21,9 + 17,6 = 39,5 ksi
   F_nt = 0,75 F_u = 43,5 ksi ;  F_nv = 0,4 F_u = 23,2 ksi (threads included)
   φF'_nt = φ[1,3 F_nt − F_nt f_v /(φ F_nv)] = 0,75[1,3(43,5) − 43,5(10,5)/(0,75×23,2)] = 22,7 ksi
   39,5 > 22,7   NG
Try 1½ in rods (A = 1,77 in²):
   f_v = 5,20 ksi ;  S = 0,563 in³ ;  f_tb = 5,18/0,563 = 9,20 ksi ;  f_ta = 9,86 ksi
   f_t = 19,1 ksi  <  φF'_nt   OK
```
**Message: adding the flexure term drove the rod from 1 1/8 in to 1½ in — a 78 % increase in area.**

### 6.10 Other constraints when the anchor carries shear with a lever arm

- **Minimum edge distance**: EN 1992-4 §7.2.2.5 restricts the lever-arm route to an edge distance ≥ **max(10 h_ef ; 60 d)**; beyond that, concrete edge breakout need not be calculated. For M24 that is **1440 mm** — usually not satisfied on a pedestal, so edge breakout must be checked or a different mechanism used.
- **Buckling**: *"Where anchors in compression have length l_a greater than **3 d**, it is advised that buckling resistance of the l_a portion of the anchor be verified"* (Hilti, both stand-off papers §2.1.3).
- **Concrete side/edge failure**: SCI P398 STEP 4 — bolts solidly cast in concrete may be designed on an **effective bearing length of 3 d** and an **average bearing stress of 2 f_cd** (f_cd of concrete or grout, whichever is weaker), *"all bolts must be completely surrounded by reinforcement and bolts whose centre is less than **6 d** from the edge of the concrete in the direction of loading should not be considered."*
- **Combined shear/tension** (SCI P398 STEP 4): `F_v,Ed/F_v,Rd + F_t,Ed/(1,4 F_t,Rd) ≤ 1,0`.
- **Concrete pry-out** (EN 1992-4 §7.2.2.4): `V_Rk,cp = k₈ N_Rk,c`, k₈ = 1 for h_ef < 60 mm, **2** for h_ef ≥ 60 mm.

---

## 7. LOAD DISTRIBUTION BETWEEN MULTIPLE ANCHORS IN SHEAR

### 7.1 The physical problem

Four anchors, four different offsets inside four oversized holes. As the plate slides:
- at slip = 0 → **friction only**;
- at slip = (smallest radial clearance) → **one** anchor bears;
- that anchor is now a short cantilever that yields at a few mm of tip deflection;
- only after it deflects does the second anchor pick up load, and so on;
- meanwhile the yielded anchors are stretching (second-order tension), which *adds* friction back in and stiffens the response (this is the mechanism in HERON Fig. 2 and Gomez Fig. 3.28).

So "4 × F_vb,Rd" is only real if the anchors are **ductile enough to redistribute** *and* the concrete never fails first.

### 7.2 The three code positions

**(a) Elastic / conservative (default):**
- AISC DG1 §3.5.3: *"use only **two of the anchor rods** to transfer the shear, unless special provisions are made to equalize the load to all anchor rods (Fisher, 1981)."*
- SCI P398 STEP 4: *"assume that **not all the bolts are effective**."*
- CESTRUCO Q&A 7.9 (F. Wald): *"**Only the anchor bolts in the compressed part of the base plate may be used to transfer shear force.**"*

**(b) Full sum, but only with tight holes:**
- EN 1993-1-8 §6.2.2(8): `F_v,Rd = F_f,Rd + n·F_vb,Rd` — permitted only under §6.2.2(5), i.e. **holes not oversized**.
- FprEN 1993-1-8:2023 §D.3.1.4 permits summing friction and anchor-bolt resistance **for traditional long anchors**.

**(c) Plastic redistribution — CEB conditions (HERON §3.3). Print these; they are the honest test of whether you may use all four anchors:**
1. Anchor arrangement adequate (base plates assumed to satisfy).
2. `R_d,c ≥ 1,25 (f_uk/f_yk) · R_d,s` — concrete capacity must exceed steel capacity. **For 8.8 anchors this means R_d,c ≥ 1,56 R_d,s.**
3. `f_uk ≤ 800 MPa`; `f_yk/f_uk ≤ 0,8`; **rupture elongation over 5 d ≥ 12 %**.
4. Reduced-section (threaded) rules: in tension, `N_uk` of the reduced section > 1,1 `N_yk` of the unreduced section, or the stressed reduced length ≥ 5 d. **In shear, the reduced section must begin ≥ 5 d below the concrete surface, or the threaded part must extend ≥ 2 d into the concrete.**
5. The steel fixture must be **embedded** or fastened without an intermediate layer, or with mortar **≤ 3 mm**.
6. **Clearance hole in the fixture ≤ 1,2 d.**

> HERON's blunt conclusion: *"For usual base plate construction this means that according to the CEB Design Guide, **plastic design is not allowed**."*

### 7.3 The engineering answer

| Situation | What you may count on |
|---|---|
| Holes to EN 1992-4 Table 6.1 (d + 2 mm), grout ≤ d/2, ≥30 MPa | All anchors, no lever arm, α_M = 2,0 |
| Holes to EN 1090-2 oversize, plate washers welded with d+2 mm holes | All anchors, but **with** lever arm (grout > d/2) |
| AISC-size holes, plate washers welded with d + 1/16 in holes | All anchors, with lever arm = t_p/2 + t_w/2 (DG1) |
| AISC-size holes, **no** welded washers | **Two anchors maximum** (DG1 §3.5.3), or none at SLS |
| Any of the above under seismic/fatigue | Fill the annular gap (spring pin / injection grout) or provide a shear lug |

---

## 8. WORKED EXAMPLES (metric, Eurocode — ready for the report)

### Example A — "The trap": a normal Israeli/European base plate that fails the code check

**Given:** HEB 300 column, S355. Base plate **500 × 500 × 30 mm**. Concrete C30/37 (f_ck = 30, f_cd = 20 N/mm²). Sand-cement mortar bedding **30 mm**. Four **M24 class 8.8** cast-in anchors (A_s = 353 mm², f_yb = 640, f_ub = 800), gauge 400 × 400. Base-plate holes drilled **Ø33 mm** (fabricator's normal "anchor holes"). 
**Actions (governing combination — maximum shear with minimum compression):** `V_Ed = 90 kN`, `N_c,Ed = 180 kN` compression.

**Step 1 — Friction (EN 1993-1-8 §6.2.2(6)):**
```
F_f,Rd = C_f,d · N_c,Ed = 0,20 × 180 = 36,0 kN      <  90 kN     NOT SUFFICIENT
(with the FprEN 1993-1-8:2023 / SCI P398 value 0,30 → 54,0 kN,  still < 90 kN)
```

**Step 2 — Anchors, as a naive designer would compute them (EN 1993-1-8 §6.2.2(7)):**
```
α_bc = 0,44 − 0,0003(640) = 0,248
F_2,vb,Rd = 0,248 × 800 × 353 / 1,25 = 56,0 kN per bolt
   [for comparison, a normal M24 8.8 bolt: 0,6 × 800 × 353/1,25 = 135,6 kN → the anchor is only 41 %]
F_v,Rd = 36,0 + 4(56,0) = 260 kN  >>  90 kN      "OK"
```

**Step 3 — Why Step 2 is INVALID.**
- EN 1090-2 Table 11: normal clearance for M24 = 2 mm (d₀ = 26); **oversize = 6 mm (d₀ = 30)**. The drawn hole is **Ø33 = +9 mm** — beyond even an oversize hole.
- **EN 1993-1-8 §6.2.2(5)** therefore forbids adding the anchor term. `F_v,Rd = F_f,Rd = 36,0 kN < 90 kN` → **FAIL by a factor of 2,5**.
- EN 1992-4 Table 6.1: `d_f` for M24 = **26 mm**. Ø33 ≫ 26 → the connection must in any case be designed **with lever arm**, and α_M = 1,0 (no restraint) unless clamped.
- Slip before first bearing: radial clearance = (33 − 24)/2 = **4,5 mm**; with a ±3 mm setting tolerance, one anchor may bear at 1,5 mm while another needs 7,5 mm. **The four anchors do not share.**

**Step 4 — What the anchors are actually worth, with welded plate washers.**
Weld a **100 × 100 × 10 mm** plate washer with a **Ø26** hole over each anchor after plumbing (thickness 10 mm ≈ d/3 = 8 mm, and ≥ the AS 4100 6 mm rule; plan size ≥ 2 d₀ − d = 2(33) − 24 = 42 mm, so 100 mm is ample and also covers the Ø33 hole with the rod at the edge).

Grout 30 mm > d/2 = 12 mm ⇒ **lever arm applies** (EN 1992-4 §6.2.2.3):
```
e₁ = t_grout + t_p/2 = 30 + 15 = 45 mm ;  a₃ = 0,5 d = 12 mm  →  l_a = 57 mm
For M24:  A_s = 353 mm² → d_s = √(4·353/π) = 21,2 mm
          W_el = π d_s³/32 = π(21,2)³/32 = 936 mm³
M⁰_Rk,s = 1,2 W_el f_uk = 1,2 × 936 × 800 = 899 000 N·mm = 899 N·m
γ_Ms = max(1,0 × 800/640 ; 1,25) = 1,25   →   M⁰_Rd,s = 719 N·m
N_Ed = 0 (compression case)  →  M_Rd,s = 719 N·m

α_M = 2,0 (welded washer, thick plate, 4 anchors restrain rotation):
   V_Rd,s,M = 2 × 719 000 / 57 = 25,2 kN per anchor  →  4 anchors = 100,8 kN
α_M = 1,0 (conservative, unclamped):
   V_Rd,s,M = 12,6 kN per anchor                     →  4 anchors =  50,4 kN
```
**So the anchor resistance falls from 56,0 kN/bolt (no lever arm) to 25,2 kN (α_M = 2) or 12,6 kN (α_M = 1): a 55 % to 78 % loss, purely because of the 30 mm grout.**

Check total with α_M = 2 (and noting EN 1992-4 §6.1(2) does **not** allow adding friction on the EC2-4 route):
`4 × 25,2 = 100,8 kN > 90 kN` — passes, **but** EN 1992-4 §7.2.2.5 then requires edge distance ≥ max(10 h_ef, 60 d = 1440 mm), which a 900 mm pedestal does not have → concrete edge breakout must be checked explicitly, and the SLS slip (Peikko: δ_V∞ = 2,3 mm; Delft: 3,6 mm at first yield) must be acceptable to the frame.

**Step 5 — The alternatives, ranked by cost.**
1. **Reduce the hole to Ø26 (EN 1992-4 Table 6.1)** and tighten the anchor setting (steel template, ±1 mm as Peikko specify). Then no lever arm applies *if* grout ≤ 12 mm — not achievable with a 500 mm plate.
2. **Weld plate washers** (above) → 100 kN, marginal, plus slip.
3. **Shear lug** → see Example B. Removes the problem entirely and the anchors then only carry tension.
4. **Recess the plate 100 mm** into the pedestal → μ rises to 0,90 (PIP) plus direct edge bearing; usually the cheapest if the foundation is not yet cast.

---

### Example B — Shear lug (shear stub) to SCI P398, metric

**Given:** HEB 300 column (h_c = 300 mm, t_w = 11 mm), base plate 500 × 500 × 30 mm, S355. Grout `h_g = 30 mm`. Concrete C30/37 → `f_cd = 20 N/mm²`. Pedestal 800 × 800 mm, lug centred. **V_Ed = 300 kN** with the column in net uplift (friction = 0).

**Choose the stub:** rule of thumb `h_s ≈ 0,4 h_c = 120 mm`; but the stub must also carry 300 kN in its own web. Try a cut length of **HEB 160** (h = b = 160, t_f = 13, t_w = 8, r = 15, A = 5430 mm²):
```
A_vs = A − 2 b t_f + (t_w + 2r) t_f = 5430 − 2(160)(13) + (8 + 30)(13) = 1764 mm²
V_pl,Rd = A_vs f_ys /(√3 γ_M0) = 1764 × 355 / 1,732 = 361,6 kN  >  300 kN   OK
```
**Embedded depth:**
```
V_Rd = b_s · d_eff · f_cd    →   d_eff = 300 000 / (160 × 20) = 94 mm
Check:  60 mm < 94 mm ≤ 1,5 h_s = 240 mm    OK
Total projection below the base plate = h_g + d_eff = 30 + 94 = 124 mm  →  use 130 mm
```
**Secondary moment and flange force:**
```
M_sec,Ed = V_Ed (h_g + d_eff/3) = 300 (30 + 31,3) = 18 400 kN·mm = 18,4 kN·m
N_sec,Ed = M_sec,Ed /(h_s − t_fs) = 18,4×10⁶ / (160 − 13) = 125 kN
Flange resistance = b_s t_fs f_ys/γ_M0 = 160 × 13 × 355 = 738 kN  >  125 kN   OK
```
**Welds** (S355, f_u = 490, β_w = 0,9, γ_M2 = 1,25):
```
Transverse (flange to base plate):  F_w,Rd/mm throat = K f_u /(√3 β_w γ_M2)
                                  = 1,225 × 490 /(1,732 × 0,9 × 1,25) = 308 N/mm per mm throat
   demand = 125 000 /(2 × 160) = 391 N/mm  →  a = 1,3 mm  →  use 6 mm fillet (minimum practical)
Longitudinal (web to base plate):   F_w,Rd/mm throat = f_u /(√3 β_w γ_M2) = 251 N/mm per mm throat
   demand = 300 000 /(2 × 134) = 1120 N/mm  →  a = 4,5 mm → s = 6,3 mm  →  use 8 mm fillet
```
**Column web check** for the concentrated flange force:
```
b_eff = t_fs + 2 s + 5 t_p = 13 + 2(8) + 5(30) = 179 mm
Resistance = 179 × 11 × 355 = 699 kN  >  125 kN   OK
```
**Detailing output for the shop:**
- Stub: **HEB 160 × 130 mm long**, welded centrally under the base plate, web parallel to the shear direction, 8 mm fillet to the web, 6 mm to the flanges (or 8 mm all round for simplicity).
- Grout pocket: **300 × 300 × 200 mm deep**, formed (not chipped), with links around it.
- Base plate: 30 mm ≥ stub flange 13 mm ✔ (DG1 rule: base plate ≥ lug thickness).
- Two Ø50 mm grout/vent holes in the base plate; flowable non-shrink grout, cube strength ≥ 2 × 37 MPa.
- Anchors now designed for **tension only** (ACI 318-19 §17.11.1.1.2 logic; PIP §9).

**Cross-checks to add in the report:**
- ACI 318-19 §17.11.2 bearing: `φV_brg,sl = 0,65 × 1,7 f'c A_ef,sl ψ_brg,sl`.
- Breakout: compute by **both** the 45° cone method (DG1/ACI 349) **and** the CCD method, and take the **minimum** — Gomez et al. found the 45° method has a mean test/predicted ratio of **0,51** for large edge distances.

---

### Example C — What a grout layer costs you, in one line (Peikko HPM 24 L)

**Given:** HPM 24 L cast-in anchor, base plate `t_Fix = 30 mm`, grout `t_Grout = 40 mm` (a normal site condition).

```
Peikko limit for "no lever arm":  t_Grout ≤ 0,5 d_b  →  < 12 mm.   40 mm ≫ 12 mm  →  lever arm REQUIRED.

Without lever arm (if the joint were ≤12 mm):
    V_Rd,s = V⁰_Rk,s / γ_Ms = 96,9 / 1,5 = 64,6 kN per anchor

With lever arm (Peikko Table 9):
    t_R = t_Grout + d_b/2 + t_Fix/2 = 40 + 12 + 15 = 67 mm
    M⁰_Rd,s = 410 N·m
    α_m = 2,0 (stand-off, restrained):  V_Rd,s = 2 × 410 000 / 67 = 12,2 kN     → −81 %
    α_m = 1,0 (standard, free to rotate): V_Rd,s = 1 × 410 000 / 67 =  6,1 kN   → −91 %
```
**Headline for the report: a 40 mm grout joint can remove 80–90 % of the shear capacity of an M24 anchor.** Four anchors go from 258 kN to 49 kN (α = 2) or 24 kN (α = 1).

---

## 9. FABRICATION AND DETAILING IMPLICATIONS (the "so what" for the shop)

**Drawing / detailing**
1. **Never** mark a base-plate anchor hole as a "bolt hole" on the shop drawing. Show the hole diameter explicitly and add the note "oversize hole for setting tolerance — shear NOT transferred by anchors" or "washer plate to be site-welded, see detail".
2. If shear goes through the anchors, the drawing must show: the **washer plate size, thickness, hole diameter (d + 2 mm), material (plain, not hardened), and the site weld**, and a note that the weld is made **after** plumbing and **before** grouting.
3. If a shear lug is used, the **grout pocket** must appear on the *foundation* drawing, not only the steel drawing. Size, depth, and the reinforcement around it are the concrete engineer's responsibility; coordinate. (SCI P398: *"Each of these solutions requires liaison between the steel designer and others."*)
4. State the anchor setting tolerance in the **concrete** specification. Otherwise ACI 117's ±25 mm applies instead of AISC's ±3,2 mm within a group.
5. Base plate thickness ≥ shear lug thickness (DG1). Check weak-axis bending of the plate caused by the lug force.

**Fabrication**
6. Anchor holes in base plates should be **drilled** where possible (ASI DG7 §4.2(7)); punching is only acceptable for thin plates and small holes (DG1 footnote 3 to Table 2.3).
7. Plate washers are **thermally cut from plate or flat bar** (DG1 §2.6); square is fine.
8. Keep mill scale under the plate if you want the tested μ = 0,45; **do not paint the underside** of a base plate that relies on friction. Never apply a slip-reducing primer to the bearing face.
9. Sawn/cold-sawn column ends give direct bearing and let the welds be minimised (ASI DG7 §4.2(5), AS 4100 14.4.4.2 / EN 1090-2 equivalent).
10. Ø50 mm grout/inspection holes: one per 0,5 m² of plate area, mandatory above 700 × 700 (SCI P398 §5.4); above 600 mm per ASI DG7.

**Erection and grouting**
11. Bedding thickness: **20–40 mm** (SCI P398 §5.4); **25 mm grout / 50 mm mortar minimum** (ASI DG7 §4.2(10)); DG1 §2.9: 1 in on a finished floor, 1½–2 in on a footing, "large base plates and plates with shear lugs may require more space."
12. *"It is important in all methods that the erector **tighten all of the anchor rods before removing the erection load line** so that the nut and washer are tight against the base plate. This is not intended to induce any level of pretension, but rather to ensure that the anchor rod assembly is firm enough to prevent column base movement during erection."* (DG1 §2.9)
13. Levelling nuts under the plate produce an **ungrouted stand-off** — the worst case for shear. If levelling nuts are used, they must be grouted over and the design must account for the full lever arm.
14. Grout must be **flowable and non-shrink**, must completely fill the void and the lug pocket, and must be **placed and inspected**. Hilti: *"Care should be taken to avoid air entrapment… well-placed air escape holes and formwork placement."* An incompletely grouted base moves the point of fixity of every anchor downwards and multiplies the bending moment.
15. Clean grout stubs out of the anchor holes before curing — in Gomez Test #4, 19 mm grout stubs left in the holes produced a large but **brittle** initial resistance that was explicitly discarded from the design model.
16. For fatigue or seismic shear, the annular gap must be filled: **spring pin to EN ISO 13337** or **injection grout ≥ 40 N/mm²** (Peikko installation instructions), or **two-component epoxy** (ECCS Publication No. 79 injection bolts, cited in HERON §5.2).
17. Where anchors resist shear, prefer **low-grade ductile anchors (4.6 / ASTM F1554 Gr.36)**. DG1 §2.7: *"Use ¾-in.-diameter ASTM F1554 Grade 36 rod material whenever possible… consider increasing rod diameter up to about 2 in. in Grade 36 material before switching to a higher-strength material grade."* Also *"specify threaded lengths at least 3 in. greater than required."*
18. Galvanizing: hot-dip (ASTM A153) or mechanical (ASTM B695) — **all threaded components of the assembly must be galvanized by the same process**; buy rods and nuts preassembled from one supplier; galvanizing increases nut/rod friction so special lubrication may be needed (DG1 §2.5). For EN work: EN ISO 1461 / EN ISO 10684, oversize nuts.

---

## 10. SUMMARY TABLE FOR THE REPORT

| Quantity | Value | Source |
|---|---|---|
| `C_f,d`, sand-cement mortar | **0,20** | EN 1993-1-8:2005 §6.2.2(6) |
| `C_f,d`, special grout | 0,30 | HERON Eq. (33b); adopted for sand-cement in FprEN 1993-1-8:2023 |
| UK friction rule | 0,3 × total compression | SCI P398 STEP 4 |
| μ, steel on grout / steel on concrete | 0,55 / 0,70, cap 0,2 f'c A_c | AISC DG1 §3.5.1 (ACI 349) |
| μ, recessed plate (contact plane below surface) | **0,90** | PIP STE05121 §8.2a |
| μ, CEB / fib | 0,40 with γ_Mf = 1,5 → 0,267; neglect if grout > 3 mm | CEB 1996 §4.1 (HERON Eq. 17) |
| μ measured, large-scale cyclic tests | **0,45–0,46** | Gomez/Kanvinde/Smith/Deierlein, AISC 2009 |
| Friction under column tension | **0** | EN 1993-1-8 §6.2.2(6) NOTE |
| Friction in EC2-4 fastening design | **neglected** | EN 1992-4 §6.1(2) |
| Anchor shear coefficient | α_bc = 0,44 − 0,0003 f_yb (0,248 for 8.8) | EN 1993-1-8 §6.2.2(7) |
| Anchor-hole clearance, EN 1992-4 | **d + 2 mm** (M24 → 26) | EN 1992-4 Table 6.1 |
| Anchor-hole clearance, EN 1090-2 oversize | 3 mm (M12) to 8 mm (≥M27); 6 mm for M24 | EN 1090-2 Table 11 |
| Anchor-hole clearance, AS 4100 / NZS 3404 | max **d + 6 mm** | AS 4100 14.3.5.2 / NZS 3404 14.3.5.2.2 |
| Anchor-hole clearance, AISC DG1 | **1,5 – 1,8 × d** (+14 to +32 mm) | DG1 Table 2.3 |
| Plate washer thickness | ≈ d/3 (30–40 % of d); ≥ 6 mm (AS) / 4 mm (NZ) if hole > d + 3 | DG1 §2.6; ASI DG7; SCNZ |
| Plate washer hole, for shear | **d + 1/16 in (d + 1,6 mm)** | DG1 §3.5.3 |
| Washer material | plain steel, **not hardened**, weldable | DG1 §2.6; PIP §8.1 |
| Shear lug bearing (US) | φ = 0,65; `0,80 f'c A` (DG1) or `1,7 f'c A_ef,sl ψ_brg,sl` (ACI 318-19 §17.11.2) | DG1 §3.5.2; ACI 318-19 |
| Shear lug bearing (EU) | `V_Rd = b_s d_eff f_cd` (2-flange stub), triangular, grout ignored | SCI P398 STEP 4 |
| Shear lug thickness | `t = √[4M_u/(0,9 f_y)]`, ≤ base plate thickness | DG1 Ex. 4.9; PIP §9.2 |
| Shear lug breakout | 45° cone at `4φ√f'c` **is unconservative** (test/pred = 0,51); use CCD, take the minimum | Gomez et al. §4.1.3; ACI 318-19 §17.11.3 |
| Lever arm | `l = a₃ + e₁`, `a₃ = 0,5 d` (0 if clamped); practical `l_a = t_grout + 0,5 d + 0,5 t_bp` | EN 1992-4 §6.2.2.3; IDEA; Peikko |
| Restraint factor | α_M = 1,0 free / **2,0 only if hole ≤ Table 6.1 (or < 1,2 d) and the fixture cannot rotate** | EN 1992-4 §6.2.2.3; CEB 1996 |
| Anchor bending resistance | `M⁰_Rk,s = 1,2 W_el f_uk`; `M_Rk,s = M⁰(1 − N_Ed/N_Rd,s)`; `V_Rk,s,M = α_M M_Rk,s / l` | EN 1992-4 §7.2.2.3.2 Eq. (7.37) |
| Grout penalty, EC2-4 no-lever-arm route | `(1 − 0,01 t_grout)`; valid only for `t_grout ≤ min(40 mm, 5d)`, grout ≥30 MPa, no moment/tension, ≥2 anchors at ≥10d | EN 1992-4 §6.2.2.3(2) / Eq. (7.36) |
| Grout penalty, ACI | ×**0,80** for built-up grout pads | ACI 318 §17.7.1.2.1 |
| Anchors effective in shear without welded washers | **2 of 4** | AISC DG1 §3.5.3 |
| Slip at anchor yield / at rupture | ≈ 3,6 mm / **15–30 mm** | HERON §2.2, Stevin 1989 |

---

## 11. SOURCES ACTUALLY USED

**Codes and standards (primary)**
1. **EN 1993-1-8:2005**, *Eurocode 3 — Design of joints*, §6.2.2 (Shear forces) — https://www.phd.eng.br/wp-content/uploads/2015/12/en.1993.1.2.2005.pdf *(companion copy of EN 1993-1-8 at the same host; clause text extracted directly)*
2. **EN 1992-4:2018**, *Eurocode 2 Part 4 — Design of fastenings for use in concrete* (§6.1(2), §6.2.2.3, Table 6.1, §7.2.2.3.1/2, §7.2.2.4, §7.2.2.5, Table 7.3) — preview: https://www.normsplash.com/Samples/DIN/189696674/DIN-EN-1992-4-2019-en.pdf
3. **EN 1090-2:2018**, *Execution of steel structures*, §6.6.1 and Table 11 (hole clearances); §8 (bolting/washers) — preview: https://www.normsplash.com/Samples/BSI/163653368/BS-EN-1090-2-2018-en-2.pdf
4. **EN 1992-1-1** §6.2.5 (shear at interfaces) — values via https://www.dlubal.com/en/downloads-and-information/examples-and-tutorials/verification-examples/001024
5. **ACI 318-19** §17.11 (attachments with shear lugs), §17.7.1.2.1 (grout pad ×0,8) — via refs 12, 13, 14
6. **EOTA Technical Report TR 054:2026-03** — reproduces the EN 1992-4 lever-arm method verbatim (Eq. 3.1, Table 3.1) — https://www.eota.eu/sites/default/files/uploads/Technical%20reports/TR054_2026-03%20-Design%20method%20for%20anchorages%20with%20metal%20injection%20anchors.pdf

**Design guides**
7. **AISC Design Guide 1, 2nd ed. (2006/2010)**, *Base Plate and Anchor Rod Design* — §2.6, Table 2.3, §2.8, §2.9, §3.5.1–3.5.5, Examples 4.8, 4.9, 4.11 — http://www.abarsazeha.com/images/ScinteficResources/DesignGuide/AISC%20Design%20Guide%2001%20-%20Base%20Plate%20And%20Anchor%20Rod%20Design%202nd%20Ed.pdf (mirror: https://files-ask.hilti.com/original/gd/gd8nj58a0r.pdf)
8. **AISC Design Guide 1, 3rd ed. (2024)**, *Base Connection Design for Steel Structures* — new embedded-base and seismic chapters, ACI 318-19 basis — https://learning.aisc.org/local/catalog/view/product.php?productid=2492
9. **SCI P398**, *Joints in Steel Construction: Moment-resisting joints to Eurocode 3* — §5.1–5.7 and STEP 4 (base plate shear, shear stubs), Table 5.1, Worked Example E.1 — https://steelconstruction.info/images/5/5d/SCI_P398.pdf
10. **PIP STE05121 (Oct 2006)**, *Anchor Bolt Design Guide* — §8 (friction, μ = 0,55/0,70/0,90), §9 (shear lug design), Appendix Example 3 — https://www.asdipsoft.com/documentation/Shear%20Lug%20Verification%20Example.pdf
11. **ASI Design Guide 7 (Hogan, 2011)**, *Pinned base plate connections for columns*, §4.2 — https://www.steel.org.au/getattachment/f68b3f37-530a-4316-8c4e-e2617a95b7de/Detailing-considerations-Design-Guide-7_bk745.pdf
12. **Steel Construction New Zealand — Technical FAQ**, base-plate hole and washer table (NZS 3404 cl. 14.3.5.2.2) — https://scnz.org/techincal-resources/frequently-asked-questions/

**Manufacturer technical documents**
13. **Hilti**, *Method for Anchor Design in Grouted Stand-off Connections*, v1.2, July 2023 (McBride, Rocha, Figoli) — EN 1992-4 Eq. (7.36) conditions, `(1 − 0,01 t_grout)`, SOFA method — https://files-ask.hilti.com/original/cf/cfecdz4zhb.pdf
14. **Hilti**, *Method for Anchor Design in Ungrouted Stand-off Connections* — EN 1992-4 Eq. (7.37), α_M, `l_a = e₁ + a₃`, buckling for `l_a > 3d` — https://files-ask.hilti.com/original/pu/pu5ebwyxp4.pdf
15. **Hilti PROFIS**, *Shear — Steel Failure with Lever Arm* (ETAG 001 Annex C Part 4.2.2.4 implementation) — https://files-ask.hilti.com/original/2x/2xnbrpqhpc.pdf
16. **Hilti Engineering**, *Integrating Shear Lug Design with ACI Anchoring-to-Concrete Provisions* (ACI 318-19 §17.11) — https://www.hilti.com/engineering/article/integrating-shear-lug-design-with-aci-anchoring-to-concrete-provisions/fo9ssa
17. **Peikko Group**, *HPM® Rebar Anchor Bolt — Technical Manual*, 06/2024 — Tables 2, 8, 9, 12, 16; installation instructions; spring-pin and injection-grout details — https://www.peikko.com (file: Peikko HPM Technical Manual 06/2024)

**Research and background**
18. **Gresnigt, Romeijn, Wald & Steenhuis**, "Column bases in shear and normal force," *HERON* Vol. 53 (2008) No. 1/2 — the background document to EN 1993-1-8 §6.2.2 — https://heronjournal.nl/53-12/5.pdf
19. **Gomez, Kanvinde, Smith & Deierlein**, *Shear Transfer in Exposed Column Base Plates*, report to AISC, March 2009 — https://www.aisc.org/media/0tqndeap/shear-transfer-in-exposed-column-base-plates.pdf
20. **Kanvinde et al.**, *Comprehensive Revision of Design Considerations for Column Bases*, AISC LRR-2022-01 (blockout/embedded bases) — https://www.aisc.org/media/1x2bmccf/aisc-lrr-2022-01_kanvinde_column-bases.pdf
21. **Wald, F. et al.**, *CESTRUCO — Design of Structural Connections to Eurocode 3: Frequently Asked Questions* (2003), Q&A 7.8–7.10 — https://people.fsv.cvut.cz/wald/CESTRUCO/CESTRUCO_Engl_2003.pdf
22. **IDEA StatiCa**, *Shear force transfer in base plate by friction and anchors* — https://www.ideastatica.com/support-center/shear-force-transfer-in-base-plate-by-friction-and-anchors
23. **IDEA StatiCa**, *Code-check of anchors (EN)* — https://www.ideastatica.com/support-center/support-center-knowledge-base/check-of-anchors-according-to-eurocode
24. **STRUCTURE magazine**, *Integrating Shear Lug Design with Anchoring-To-Concrete Provisions* — https://www.structuremag.org/article/integrating-shear-lug-design-with-anchoring-to-concrete-provisions/
25. **ASDIP**, *Shear Lug Design: Overview of the ACI Provisions* — https://www.asdipsoft.com/shear-lug-design-overview-of-the-aci-provisions/
26. **AISC Design Guide 1 Revisions and Errata List** — https://www.aisc.org/media/g25d5v4o/design-guide-1-revisions-and-errata-list-1.pdf *(access was blocked; check for Table 2.3 corrections before publishing that table)*
27. **מכון התקנים הישראלי**, ת"י 1225 חלק 1.1 (steel structures, EN 1993 route, June 2023) — https://www.sii.org.il/he/lobby/standardization/standard-page/?id=346e84a6-b8c8-4576-ac00-c1137f38eb3c_HE

---

## 12. ITEMS TO VERIFY BEFORE PUBLICATION (honest gaps)

1. **EN 1090-2:2018 Table 11 intermediate rows** — the endpoints (M12 oversize = 3 mm, M27 = 8 mm; normal M20 = 2 mm, M24 = 2 mm) are confirmed from independent sources; one source instead quotes a flat `d₀ = d + 4` for oversize. Check the table in the standard.
2. **AISC DG1 Table 2.3, last row** — as printed, a 2½ in rod has the same 3¼ in hole as a 2 in rod, which breaks the "washer covers the hole" geometry used for every other row. Check the AISC errata list (ref. 26).
3. **ACI 318-19 §17.11.2.1.2 `A_ef,sl`** — the "projected distance based on twice the shear lug thickness" is quoted consistently by Hilti and ASDIP but is easy to misapply; read the code figure R17.11.2.1.
4. **`M⁰_Rk,s = 1,2 W_el f_uk` vs Hilti's `1,5 W_el f_uk`** — use 1,2 (EN 1992-4/ETAG/EOTA/Peikko) unless a product ETA states otherwise.
5. **Israeli standards** — confirm the current designation and status of ת"י 1225 חלק 1.1 and the applicable National Annex values for γ_M2, and whether EN 1992-4 is formally referenced for anchorage, with SII.