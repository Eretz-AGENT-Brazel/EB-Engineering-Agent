# EXECUTION OF STEEL COLUMN ANCHORAGE — TECHNICAL BRIEF
### Setting out, tolerances, levelling, grouting, defects, repair and QA
*Raw material for an engineering report. All figures tagged with source. Where sources conflict, both are given and the conflict is named.*

---

## PART 0 — READING NOTE ON SOURCE RELIABILITY

Values below are marked:
- **[P]** = read directly in the full primary/technical document (I extracted and read the text)
- **[S]** = obtained from a search summary or a secondary page I could not open in full — treat as indicative, verify before publishing a number in a contract document

---

## PART 1 — THE GOVERNING ENGINEERING PRINCIPLES (THE "WHY")

### 1.1 The central conflict: two trades, two accuracies, one interface

A column base is the only place in a steel building where a **cast-in-place concrete component** (accuracy ±10–25 mm) must mate with a **shop-fabricated steel component** (accuracy ±2 mm). Everything in this brief follows from that ratio of roughly 10:1.

The industry does not solve this by making concrete more accurate. It solves it by **deliberately building slack into the steel** — oversized holes, adjustable levelling devices, and a variable-thickness grout bed — and then **removing the slack after alignment** by grouting and tightening. A fabricator who treats an anchor-rod hole like a bolt hole has removed the only adjustment mechanism the erector has.

> AISC DG1 §2.6 states it plainly: *"The most common field problem is anchor rod placements that either do not fit within the anchor rod hole pattern or do not allow the column to be properly positioned."* **[P]**

### 1.2 Three distinct load paths exist in sequence — and they are not the same

This is the single most misunderstood point on site.

| Stage | Load path for vertical load | Load path for shear | Anchor rods carry |
|---|---|---|---|
| **1. Erection (before grout)** | Column → base plate → levelling nuts / shims / levelling plate → concrete | Friction at shims only; essentially nothing | **Compression** (if levelling nuts), plus erection-stability moment |
| **2. Grout placed, not cured** | Same as stage 1 | Same as stage 1 | Same as stage 1 |
| **3. Grout cured (design condition)** | Column → base plate → **grout** → concrete | Friction under plate + anchor dowel action + shear lug | Tension and shear only |

Two consequences that cause real failures:

1. **A base that is never grouted never reaches Stage 3.** The anchor rods stay in compression and bending forever. CROSS Safety Report 1190 documents a pipe/cable rack frame loaded with services before grouting: *"the cast-in bolts... were not designed to take compression or lateral loads and could have buckled, bent or been damaged."* The expert panel: *"Bolt failure, baseplate failure and excessive lateral deflections of superstructures under loads could result from such errors."* **[P]**

2. **Levelling nuts put the anchor rod into compression permanently.** AISC DG1 §2.9.1: *"when designing anchor rods using setting nuts and washers, it is important to remember these rods are also loaded in compression and their strength should be checked for push out at the bottom of the footing... Even after the base plate is grouted, the setting nut will transfer load to the anchor rod."* **[P]**

### 1.3 Why grout thickness is a *structural* variable, not a builder's convenience

Once the base plate is standing off the concrete on a bed of grout, every anchor rod that carries shear becomes a **short cantilever/beam in bending**, not a bolt in pure shear. This is codified.

**EN 1992-4:2018 §6.2.2.3** permits shear to be taken *without* lever arm only if all of the following hold **[P — via Hilti method paper reproducing the clause; [S] confirms the d/2 wording]**:

1. At least two fasteners spaced ≥ 10·d resist shear in the direction of the shear force
2. There is **no bending moment and no net tension** on the connection
3. The mortar/grout layer thickness **t_grout ≤ 0,5·d** (Hilti's paper states the limit as *min(40 mm; 0,5·d)*)
4. The grout **completely fills** the void between plate and concrete
5. Grout compressive strength ≥ concrete strength **and ≥ 30 N/mm²**
6. (per the clause wording) the concrete surface beneath is **rough**

**This is the sleeper clause.** For M20, 0,5·d ≈ 10 mm. A normal 25–50 mm bedding **automatically fails condition 3**, so almost every real column base in Europe must either (a) design the anchors for bending with a lever arm, or (b) take shear by friction or a shear lug and design the anchors for tension only.

Peikko tabulates the limit using the **stress-area diameter d_b**, which is stricter still:

**Peikko HPM Technical Manual 06/2024, Table 8 — max joint height for t_grout ≤ 0,5·d_b [P]**

| Anchor bolt | max t_grout (mm) |
|---|---|
| HPM 16 (M16) | < 8 |
| HPM 20 (M20) | < 10 |
| HPM 24 (M24) | < 12 |
| HPM 30 (M30) | < 15 |
| HPM 39 (M39) | < 19 |

Peikko adds: *"For seismic loads (category C1 and C2), the maximum admissible joint height is equal to t_Grout (Table 8). Bigger joint height is not admissible."* **[P]**

> **Conflict to report explicitly:** Hilti states the EN 1992-4 grout cap as *min(40 mm, 0,5 d)*; Peikko applies *0,5·d_b* (stress-area diameter) giving 8–19 mm. Peikko is the stricter and, on the clause wording (*t_grout ≤ 0,5 d*), the more literal reading. In the Hilti worked example the 44 mm pad was rejected against the 40 mm limit, so both readings gave the same verdict.

### 1.4 Why the grout must be *fully* filled, not merely present

Grout is a **bearing material**, and bearing stress = load / *contact* area. A base plate with 40 % voids under it does not carry 60 % of the load at 100 % of the stress — it carries 100 % of the load at ~250 % of the stress, concentrated wherever the plate happens to touch. That local overstress crushes the grout, the plate settles, the column shortens, and load sheds into the anchor rods.

The STRUCTURE magazine article on structural grouting records the mechanism directly: a Colorado steel frame (2015) where *"[a] shim stack on concrete pilaster supporting four stories... concentrated load caused it to punch into the top of the concrete pilaster, resulting in failure... and a two-inch drop of the structural frame."* The same source cites the 2012 Miami parking structure collapse where *"grout was not placed, as required, at the base of an interior column to adequately transfer the column load to the footing."* **[P]**

Acceptance limits on voids, from **ACI 351.1R-99 §4 (base-plate test)** **[P]**:
- Cementitious grout: *"Small, randomly distributed placing voids that account for less than 5% of the plate area are usually considered acceptable."*
- Epoxy grout: *"some users accept uniformly distributed voids of up to 25% of bearing area if the resulting baseplate bearing stress is less than the allowable stresses provided by the manufacturer."*

### 1.5 Why anchor rods are *not* preloaded bolts

Structural bolts are preloaded to develop friction. Anchor rods generally are **not**. AISC DG1 §2.9: *"It is important in all methods that the erector tighten all of the anchor rods before removing the erection load line... This is not intended to induce any level of pretension, but rather to ensure that the anchor rod assembly is firm enough to prevent column base movement during erection."* **[P]**

The exception is the **double-nut moment joint** (sign structures, poles, some moment bases), which *is* pretensioned — see §8.2.

---

## PART 2 — SETTING OUT AND TEMPLATES

### 2.1 The placement drawing and who does the layout

**AISC DG1 §2.8 [P]:**
- The **structural steel detailer** should coordinate all anchor rod details with the column-base-plate assembly, not the foundation designer working alone.
- *"only placement drawings that have been designated as 'Released for Construction' should be used for this important work."* Setting bolts off an approval-stage drawing is named as a specific failure mode.
- *"Layout (and after-placement surveying) of all anchor rods should be done by an experienced construction surveyor... A typical licensed land surveyor may or may not have the necessary knowledge and experience for this type of work."*

### 2.2 Template types

**AISC DG1 §2.8 [P]** — *"Templates should be made for each anchor rod setting pattern."*

| Type | Construction | Advantages | Limitations |
|---|---|---|---|
| **Plywood template** | Made on site; bolts held by a nut each side of the template | Cheap; easy to nail to timber formwork; holds rods straight | Low stiffness; swells when wet; not for close tolerances |
| **Steel plate / angle-frame template** | Flat plate or welded angle frame, nailing holes provided | *"used for very large anchor rod assemblies requiring close setting tolerances"*; **can be reused as the setting (levelling) plate** | Cost; must be positively secured |
| **Embedded template (lower template)** | Left permanently in the concrete | Maintains rod alignment during the pour; may form part of the anchorage | Must be *"kept as small as possible to avoid interference with the reinforcing steel"*; concrete must be consolidated around it *"to eliminate voids... especially important if the template serves as part of the anchorage"* |

### 2.3 The two-template method

The two-template method uses a **lower (embedded or near-bottom) template** and an **upper (exposed) template** on the same bolt group, with the rods nutted against both.

- The **lower template** fixes the bolt spacing at the anchorage end and, critically, holds the rods **plumb**. A single upper template fixes the spacing at the tip but lets the rods swing about it during the pour — which is exactly the mechanism DG1 §2.11.2 blames for out-of-plumb rods: *"If the rods are not properly secured in the template, or if there is reinforcing steel interference, the rods may end up at an angle to the vertical that will not allow the base plate to be fit over the rods."* **[P]**
- The **upper template** sets the projection and the position relative to the grid, and is removed after the pour: *"If a top template is above the concrete surface, it may be removed 24 hours after placing the concrete."* (DG1 App. A, step 7) **[P]**
- Sequencing differs: *"When using a single exposed template, the reinforcing steel can be placed before positioning the anchor rods in the form. With the embedded template, the anchor rod assembly must be placed first and the reinforcing steel placed around or through the template."* **[P]**

**Manufacturer practice** — Peikko PPL templates: steel plate, alignment marks to the module line, concrete poured through a central hole, detached and reused **[S]**. Peikko HPKM column shoes: *"Installation tolerance of column shoe in crosswise direction of the column is ± 2 mm."* **[P]**

RPP (rsteel) Technical Manual §6 **[P]**: *"Template plates are to be used when forming bolt groups from the base bolts. The template plates help to obtain the correct spacing of base bolts and general alignment and positioning of the bolt groups relative to the building or structure. Furthermore, the template plates help to obtain the correct installation level of the bolts as well as help with protecting the threads of the base bolts during concrete casting."*

Rule of thumb from fabrication practice **[S]**: template made *"mirroring the exact baseplate with a reduced thickness, typically 4 mm."*

### 2.4 After the pour — the survey and hand-over gate

**AISC DG1 §2.8 [P]:** *"When the templates are removed, the anchor rods should be surveyed and grid lines marked on each setting. The anchor rods should then be cleaned and checked to make sure the nuts can be easily turned and that the vertical alignment is proper. If necessary, the threads should be lubricated. OSHA requires the contractor to review the settings and notify the Engineer of Record of any anchor rods that will not meet the tolerance required for the hole size specified."*

**OSHA 29 CFR 1926.755 [P]:**
- (a)(1) *"All columns shall be anchored by a minimum of 4 anchor rods (anchor bolts)."*
- (a)(2) Each anchor-rod assembly, including the column-to-base-plate weld and the foundation, must resist *"a minimum eccentric gravity load of 300 pounds (136.2 kg) located 18 inches (.46 m) from the extreme outer face of the column in each direction at the top of the column shaft."*
- (a)(3) *"Columns shall be set on level finished floors, pre-grouted leveling plates, leveling nuts, or shim packs which are adequate to transfer the construction loads."*
- (b)(1) *"Anchor rods (anchor bolts) shall not be repaired, replaced or field-modified without the approval of the project structural engineer of record."*
- (b)(2) Written notification to the erector is required before erecting a column whose rods were repaired/modified.

**RPP inspection checklist [P]** — before casting: correct bolts and templates (c/c dimensions, thread size), no delivery damage, positions within tolerance, levels within tolerance, required reinforcement installed, installation frame horizontal, threads protected. After casting: re-check bolt group position, *"Dimensions that are greater than the tolerance requirements are to be reported to the structural designer"*, threads protected with tape/plastic tube.

### 2.5 Detailing rules that come from the setting process

**AISC DG1 §2.7 [P]:**
- *"Use ¾-in.-diameter ASTM F1554 Grade 36 rod material whenever possible. Where more strength is required, consider increasing rod diameter up to about 2 in. in ASTM F1554 Grade 36 material before switching to a higher-strength material grade."*
- *"Whenever possible, threaded lengths should be specified at least 3 in. [75 mm] greater than required, to allow for variations in setting elevation."*
- *"the typical layout should have four anchor rods in a square pattern"*, symmetrical in both directions, with as few different layouts as possible.
- Clear dimension from **hole edge to plate edge**: *"even an edge distance that provides a clear dimension as small as 2 in. [51 mm] of material from the edge of the hole to the edge of the plate will normally suffice, although field issues with anchor rod placement may necessitate a larger dimension to allow some slotting of the base plate holes."*
- **Piers:** *"Anchor rods in piers should never extend below the bottom of the pier into the footing because this would require that the anchor rods be partially embedded prior to forming the pier, which makes it almost impossible to maintain alignment. When the pier height is less than the required anchor rod embedment length, the pier should be eliminated and the column extended to set the base plate on the footing."*
- **Fast-track / complex layouts:** *"it may be better to use special drilled-in epoxy-type anchor rods rather than cast-in-place rods."*
- **Wedge anchors are excluded:** *"wedge-type mechanical anchors... are not recommended for anchor rods because they must be tensioned to securely lock in the wedge device. Column movement during erection can cause wedge-type anchor rods to loosen."*

---

## PART 3 — TOLERANCES ON ANCHOR BOLT POSITION AND LEVEL

### 3.1 AISC Code of Standard Practice §7.5.1 — the classic five clauses

Quoted verbatim in **AISC DG1 §2.8** from **AISC 303-05** **[P]**:

> (a) *"The variation in dimension between the centers of any two Anchor Rods within an Anchor-Rod Group shall be equal to or less than **⅛ in.**"* → **3 mm**
> (b) *"The variation in dimension between the centers of adjacent Anchor-Rod Groups shall be equal to or less than **¼ in.**"* → **6 mm**
> (c) *"The variation in elevation of the tops of Anchor Rods shall be equal to or less than plus or minus **½ in.**"* → **±13 mm**
> (d) *"The accumulated variation in dimension between centers of Anchor-Rod Groups along the Established Column Line through multiple Anchor-Rod Groups shall be equal to or less than **¼ in. per 100 ft**, but not to exceed a total of **1 in.**"* → **6 mm per 30 m, max 25 mm**
> (e) *"The variation in dimension from the center of any Anchor-Rod Group to the Established Column Line through that group shall be equal to or less than **¼ in.**"* → **6 mm**

### 3.2 The 2016 change — AISC 303 harmonised with ACI 117

**Important and frequently missed.** AISC 303-16 revised §7.5.1 to adopt the **per-diameter** tolerances that ASCC proposed and ACI 117 adopted, replacing the flat ⅛ in / ¼ in values. **[S]**

> *"In 2016, AISC 303 chose to use the original anchor bolt tolerances proposed by ASCC and adopted by ACI 117, so the anchor bolt or rod tolerances are now the same in the key construction industry documents."* — ASCC, *A Tolerance Compatibility Success* **[S]**
> *"In Section 7.5.1 [of AISC 303-16], tolerances for anchor-rod placement have been revised for consistency with the hole sizes provided in the AISC Steel Construction Manual and the tolerances given in ACI 117."* **[S]**

**Report both.** Many current specifications, textbooks and DG1 itself still quote the pre-2016 ⅛ in / ¼ in values. If your project specification says "per AISC Code of Standard Practice", the **edition matters**.

### 3.3 ACI 117-10 (R2015) — the concrete side, read in full **[P]**

**§2.3 Placement of embedded items (excluding dowels in slabs-on-ground)**

| Clause | Item | Tolerance |
|---|---|---|
| 2.3.1 | Clearance to nearest reinforcement | greater of bar dia., largest aggregate size, or **1 in. (25 mm)** |
| 2.3.2 | Centerline of assembly from specified location | Horizontal **±1 in.**; Vertical **±1 in.** (±25 mm) |
| 2.3.3 | Surface of assembly from surface of element — assembly ≤ 12 in. | **±½ in. per 12 in.**, but not less than **±¼ in.** |
| 2.3.3 | Assembly dimension > 12 in. | **±½ in.** (±13 mm) |
| **2.3.4.1** | **Top of anchor bolt from specified elevation, vertical** | **±½ in. (±13 mm)** |
| **2.3.4.2** | **Centerline of individual anchor bolts from specified location, horizontal** | ¾ in. & ⅞ in. bolts: **±¼ in. (±6 mm)**<br>1 in., 1¼ in., 1½ in. bolts: **±⅜ in. (±10 mm)**<br>1¾ in., 2 in., 2½ in. bolts: **±½ in. (±13 mm)** |

The ACI 117-10 commentary R2.3.3 states the linkage explicitly: *"The tolerance for the elevation of the top of anchor bolts is consistent with that contained in the American Institute of Steel Construction's Code of Standard Practice (AISC 303-10). The tolerance for the location of anchor bolts is based on using oversized holes per the AISC Design Guide 1..."* **[P]**

**Foundation elevation and flatness — this is what drives grout thickness [P]:**

| Clause | Item | Tolerance |
|---|---|---|
| 3.3.1 | Top surface of foundations, vertical | **+½ in. / −2 in.** (**+13 mm / −50 mm**) |
| 3.3.2 | Top surface of drilled piers, vertical | **+1 in. / −3 in.** |
| 3.4.2 | Top of footings at interface with supported element | max gap under a **10 ft straightedge ≤ +½ in.** |
| 5.2.1.5 | Fabricated bearing surface assemblies flush with concrete | ±½ in. |
| 5.2.1.9 | Embedded plates | ±1 in. |
| 5.2.1.10 | Inserts and assemblies with inserts | ±½ in. |

> **Read §3.3.1 carefully: the top of a footing may legally be 50 mm LOW.** That single number is why a nominal 25 mm grout bed must be detailed as a *range*, and why anchor rod threaded length must carry +75 mm spare.

**The historic conflict (still live in older specs):** ACI 117-**90** §2.3 allowed **±1 in.** on vertical, lateral and level alignment of embedded items — 8× the old AISC ⅛ in. ASCC PS#14 **[P]**: *"Clearly, these two requirements are not compatible. The ACI 117 tolerance is too lenient for anchor bolts, and the AISC tolerance is too tight... Because both tolerances are specified in the contract documents, arguments are inevitable."* DG1 §2.8 recommends resolving it by *"requir[ing] that the anchor rods be set in accordance with the AISC Code of Standard Practice tolerance requirements... It may be helpful to actually list the tolerance requirements instead of simply providing a reference."* **[P]**

### 3.4 EN 1090-2 — the European values

EN 1090-2 places foundation-bolt deviations among the **functional** (not essential) tolerances. In **EN 1090-2:2008** these are **Annex D, clause D.2.20, "Functional tolerances — Concrete foundations and supports"** **[P — title confirmed; values via NSCS]**. In **EN 1090-2:2018** the tolerance annex was renumbered to **Annex B**, with erection tolerances in **Tables B.15 to B.25** and Class 1 / Class 2 functional tolerance columns **[P]**.

The **UK National Structural Concrete Specification, Edition 4, §10.4 "Foundation bolts and similar inserts"** reproduces these values and states in a note: ***"Deviations are coordinated with BS EN 1090-2: 2008 Cl. D.2.20."*** **[P]** — this is the most reliable freely-available reproduction of the EN values:

**§10.4.1 — Preset bolt PREPARED FOR ADJUSTMENT** (bolt in a sleeve, cone, pocket or bolt-box)

| Criterion | Permitted deviation |
|---|---|
| Distance of centre of a bolt group from intended design position | **±6 mm** |
| Location of bolt at tip, from centre of bolt group (Δy, Δz) | **±10 mm** |
| Protrusion Δp | **−5 mm ≤ Δp ≤ +25 mm** |

**§10.4.2 — Preset foundation bolt NOT prepared for adjustment** (cast rigidly, no pocket)

| Criterion | Permitted deviation |
|---|---|
| Distance of centre of a bolt group from intended design position | **±3 mm** |
| Location of bolt at tip, from centre of bolt group (Δy, Δz) | **±3 mm** |
| Vertical protrusion Δp | **−5 mm ≤ Δp ≤ +45 mm** |
| Horizontal protrusion Δx | **−5 mm ≤ Δx ≤ +45 mm** |

> **This split is the key European concept and has no direct AISC equivalent.** EN accepts that if the bolt can move (sleeve/pocket), the *group* must be right to ±6 mm but the individual bolt tip may be ±10 mm out — it will be nudged into place. If the bolt is cast solid, it must be right to ±3 mm, because nothing can be adjusted afterwards. **A fabricator must know which of the two he is being handed**, because the two demand entirely different base-plate hole sizes.

**Supporting foundation tolerances, NSCS Edition 4 [P]:**

| Clause | Item | Permitted deviation |
|---|---|---|
| 10.3.1 | Base support (foundation) plan position | **±25 mm** |
| 10.3.2 | Base support vertical position, supporting **concrete** superstructure | **±20 mm** |
| 10.3.2 | Base support vertical position, supporting **steel** superstructure | **−15 mm / +5 mm** |
| 10.5.1 | Column/wall element centreline position on plan | ±10 mm |
| 10.1.6 | "Box principle" applied to an individual element | ±20 mm |

NSCS Guidance on 10.3.2 **[P]**: *"Two values are given for this tolerance as one is fixed to coordinate with BS 1090 [sic]. Particular attention is drawn to the need to specify adequate thickness of any grout bed beneath follow-on items such as steel base plates, and also projection lengths of cast-in anchor bolts. This is required as the tolerances in the top level of foundations and bolt projections affect the achieved thickness of grout bed."*

> Note the **asymmetry**: −15/+5 mm. The concrete is permitted to be 15 mm low but only 5 mm high, precisely so the grout bed can never be squeezed to zero.

### 3.5 Manufacturer tolerances (proprietary anchor systems)

**RPP Base Bolt Technical Manual, 26.08.2025, §6.1 [P]:**
- *"The positional tolerance of the bolt group for installation of the precast concrete elements = ±10 mm."*
- *"Tolerance for the level of the top of the base bolt = ±20 mm."*

**Table 13 — RPP base bolt heights and spacing tolerance [P]**

| Base bolt | Grout thickness (mm) | Height of top of bolt above concrete (mm) | Spacing tolerance within group (mm) |
|---|---|---|---|
| M16 | 50 | 105 | ±3 |
| M20 | 50 | 115 | ±3 |
| M24 | 50 | 130 | ±3 |
| M30 | 50 | 150 | ±3 |
| M39 | 60 | 180 | ±3 |

**Peikko HPKM [P]:** column-shoe installation tolerance crosswise to the column **±2 mm**.

### 3.6 Consolidated tolerance comparison table

| Quantity | AISC 303-05 §7.5.1 | AISC 303-16 / ACI 117-10 §2.3.4 | EN 1090-2 D.2.20 (adjustable) | EN 1090-2 D.2.20 (fixed) |
|---|---|---|---|---|
| Bolt-to-bolt within a group | ±3 mm (⅛ in) | — (per-bolt basis instead) | ±10 mm at tip from group centre | ±3 mm at tip from group centre |
| Individual bolt from specified location | — | ±6 mm (¾–⅞ in) / ±10 mm (1–1½ in) / ±13 mm (1¾–2½ in) | — | — |
| Group centre from column line / design position | ±6 mm (¼ in) | — | **±6 mm** | **±3 mm** |
| Group-to-adjacent-group | ±6 mm (¼ in) | — | — | — |
| Accumulated along column line | 6 mm/30 m, max 25 mm | — | — | — |
| **Elevation / protrusion of bolt top** | **±13 mm (±½ in)** | **±13 mm (±½ in)** | **−5 to +25 mm** | **−5 to +45 mm** |
| Top of foundation concrete | (not in AISC) | **+13 / −50 mm** (ACI 117 §3.3.1) | −15 / +5 mm supporting steel (NSCS 10.3.2) | same |

---

## PART 4 — WHY BASE PLATE HOLES ARE OVERSIZED: THE ARITHMETIC

### 4.1 The reference point: a structural bolt hole

| Code | Standard (normal) hole clearance |
|---|---|
| **EN 1090-2:2018 Table 11 / EN 1993-1-8 Table 3.1** | **1 mm** for M12, M14; **2 mm** for M16–M24; **3 mm** for ≥M27 **[S, corroborated]** |
| **AISC 360 §J3.3** | **1/16 in (1,6 mm)** for d ≤ ⅞ in; **⅛ in (3,2 mm)** for d ≥ 1 in **[P]** |
| EN oversize round hole | 3 mm (M12/M14), 4 mm (M16–M22), 6 mm (M24), 8 mm (≥M27) **[S]** |

EN 1090-2:2018 confirms the M12/M14 = 1 mm baseline indirectly: its list of options requiring project agreement includes *"If 12 and 14 mm or countersunk bolts may be used in **2 mm clearance holes**"* — i.e. 2 mm is a departure from the default. **[P]**

**Anchor rod holes are 7 to 10 times this.**

### 4.2 AISC DG1 Table 2.3 — Recommended Sizes for Anchor Rod Holes in Base Plates

Decoded directly from the AISC fraction font in the DG1 2nd-edition PDF **[P]**, cross-checked against ASCC PS#14's independent transcription of the AISC Manual (Table 14-2) **[P]**:

| Rod dia. in (mm) | **Hole dia.** in (mm) | **Diametral clearance** in (mm) | Min. washer dim. in (mm) | Min. washer thk. in (mm) |
|---|---|---|---|---|
| ¾ (19,1) | **1-5/16 (33,3)** | **9/16 (14,3)** | 2 (51) | ¼ (6) |
| ⅞ (22,2) | **1-9/16 (39,7)** | 11/16 (17,5) | 2½ (64) | 5/16 (8) |
| 1 (25,4) | **1-13/16 (46,0)** | 13/16 (20,6) | 3 (76) | ⅜ (10) |
| 1¼ (31,8) | **2-1/16 (52,4)** | 13/16 (20,6) | 3 (76) | ½ (13) |
| 1½ (38,1) | **2-5/16 (58,7)** | 13/16 (20,6) | 3½ (89) | ½ (13) |
| 1¾ (44,5) | **2-¾ (69,9)** | 1 (25,4) | 4 (102) | ⅝ (16) |
| 2 (50,8) | **3-¼ (82,6)** | 1¼ (31,8) | 5 (127) | ¾ (19) |
| 2½ (63,5) | **3-¾ (95,3)** | 1¼ (31,8) | 5½ (140) | ⅞ (22) |

> **Data-integrity note:** the DG1 PDF copy I extracted renders the 2½ in row's hole as 3-¼ in, duplicating the 2 in row. ASCC PS#14 (published in *Concrete International*, transcribing the AISC Manual) gives **3-¾ in for the 2½ in bolt**, and the progression only makes sense that way. **Use 3-¾ in (95 mm)** and treat the DG1 print as a typographic artefact.

**Table notes (DG1) [P]:** *"1. Circular or square washers meeting the size shown are acceptable. 2. Adequate clearance must be provided for the washer size selected. 3. See discussion below regarding the use of alternate 1-1/16-in. hole size for ¾-in.-diameter anchor rods, with plates less than 1 in. thick."*

**The ¾ in exception [P]:** for columns in **axial compression only**, a ¾ in rod may use a **1-1/16 in (27 mm)** hole in plates < 1¼ in thick — this allows **punching** instead of thermal cutting and permits standard ASTM F844 washers. DG1 warns: *"This potential fabrication savings must be weighed against possible problems with placement of anchor rods out of tolerance."*

**Where the hole sizes came from [P]:** *"These hole sizes originated in the first edition of Design Guide 1, based on field problems in achieving the column setting tolerances required for the previous somewhat smaller recommended sizes. They were later included in the AISC Steel Construction Manual."* They are **empirical**, derived from field failures — not from a calculation.

### 4.3 The arithmetic — reconstructing the hole size from the tolerances

**Method A — direct from ACI 117 per-bolt tolerance (matches AISC's own stated basis)**

An individual bolt may sit anywhere in a circle of radius Δ about its true position (ACI 117 §2.3.4.2). If the base plate holes were perfect, the minimum hole to guarantee entry is:

```
d_hole ≥ d_rod + 2·Δ_bolt
```

then add a fabrication allowance Δ_fab for the plate hole position:

```
d_hole ≥ d_rod + 2·(Δ_bolt + Δ_fab)
```

| Rod | Δ_bolt (ACI 117) | d_rod + 2Δ_bolt | AISC hole | Implied 2·Δ_fab |
|---|---|---|---|---|
| ¾ in (19,1) | ±¼ in (6,4) | 1¼ in (31,8) | 1-5/16 (33,3) | **1/16 in (1,6 mm)** |
| ⅞ in (22,2) | ±¼ in (6,4) | 1⅜ in (34,9) | 1-9/16 (39,7) | 3/16 in (4,8 mm) |
| 1 in (25,4) | ±⅜ in (9,5) | 1¾ in (44,5) | 1-13/16 (46,0) | **1/16 in (1,6 mm)** |
| 1¼ in (31,8) | ±⅜ in (9,5) | 2 in (50,8) | 2-1/16 (52,4) | **1/16 in (1,6 mm)** |
| 1½ in (38,1) | ±⅜ in (9,5) | 2¼ in (57,2) | 2-5/16 (58,7) | **1/16 in (1,6 mm)** |
| 1¾ in (44,5) | ±½ in (12,7) | 2¾ in (69,9) | 2-¾ (69,9) | **0** |
| 2 in (50,8) | ±½ in (12,7) | 3 in (76,2) | 3-¼ (82,6) | ¼ in (6,4 mm) |
| 2½ in (63,5) | ±½ in (12,7) | 3½ in (88,9) | 3-¾ (95,3) | ¼ in (6,4 mm) |

**Result: the AISC anchor-rod hole is, almost exactly, `d_rod + 2 × (ACI 117 placement tolerance) + 0 to ¼ in`.** For the mid-range sizes the fabrication allowance is a clean **1/16 in ≈ 1,6 mm**, which is the standard bolt-hole clearance. That is a defensible, quotable derivation.

**Method B — statistical (RSS) combination, using the AISC 303-05 tolerances**

Take the three independent contributors that put one rod off-centre from its hole:
- group centre off the column line: **±6 mm** (§7.5.1(e))
- rod off the group centre: **±3 mm** (§7.5.1(a))
- plate hole position (fabrication): **±2 mm** (typical EN 1090-2 functional tolerance for hole-group position)

Worst-case linear stack: Δ = 6 + 3 + 2 = **11 mm** → hole = d + 22 mm
Root-sum-square: Δ = √(6² + 3² + 2²) = √49 = **7,0 mm** → hole = **d + 14 mm**

The AISC ¾ in hole clearance is **14,3 mm**. The RSS combination reproduces it to within 0,3 mm.

> **This is the headline number for the report.** The oversize hole is not generosity; it is the *root-sum-square of three legitimate, code-permitted tolerances*, doubled. A fabricator who reduces it is betting that all three deviations will be near zero simultaneously.
>
> (Method B is my reconciliation, not a code statement — present it as such.)

### 4.4 The washer geometry rule

DG1 **[P]**: *"The washer diameters shown in Table 2.3 are sized to cover the entire hole when the anchor rod is located at the edge of the hole."* That gives a closed-form rule:

If the rod is hard against the hole wall, its centre is offset by `(d_hole − d_rod)/2`. A washer centred on the rod must still cover the hole, so:

```
d_washer ≥ 2·d_hole − d_rod
```

Check ¾ in: 2(33,3) − 19,1 = 47,5 mm; AISC specifies **51 mm** ✔
Check 2½ in: 2(95,3) − 63,5 = 127 mm; AISC specifies **140 mm** ✔

And thickness, from AISC DG7 as cited in DG1 **[P]**:

```
t_washer ≈ d_rod / 3
```

DG1 adds: *"The same thickness is adequate for all grades of ASTM F1554, since the pull-through criterion requires appropriate stiffness as well as strength."*

Other washer rules **[P]**:
- *"Washers should not be welded to the base plate, except when the anchor rods are designed to resist shear at the column base."*
- *"ASTM F436 washers are not used on anchor rods because they generally are of insufficient size."*
- *"Washers for anchor rods are not, and do not need to be, hardened."*
- Plate washers are *"usually custom fabricated by thermal cutting the shape and holes from plate or bar stock."*

### 4.5 The European position — much tighter holes, but the annulus must be FILLED

This is a genuine and important divergence.

**EN 1992-4:2018 Table 6.1 — maximum clearance hole d_f in the fixture** (as tabulated by Peikko Table 2) **[P]**:

| Bolt | d_f per EN 1992-4 Table 6.1 (mm) | Clearance (mm) | Peikko min. d_f1 for spring pins (mm) |
|---|---|---|---|
| M16 | 18 | +2 | 20 |
| M20 | 22 | +2 | 25 |
| M24 | 26 | +2 | 30 |
| M30 | 33 | +3 | 40 |
| M39 | 42 | +3 | 50 |

Also quoted for smaller sizes **[S]**: M8→9, M10→12, M12→14.

Peikko's governing statement **[P]**: *"The hole diameter Ø d_f must correspond to EN 1992-4 Table 6.1. **Bigger hole diameters Ø d_f exceeding the values, are subject to injection grouting or require the use of spring pins.**"* And: *"An equal load share to all HPM Rebar Anchor Bolts involved is guaranteed when the annular gap between the bolt thread and the fixture is filled."*

Two accepted ways to fill the annulus **[P]**:
1. **Spring pins** to EN ISO 13337:2009, length `l = t_fix − 1 mm`, sized Ø16,5 / 21,5 / 25,5 / 32,5 / 40,5 mm for M16/20/24/30/39
2. **Injection grout** with minimum compressive strength **40 N/mm²** (approvals ETA-12/0164, ETA-19/0601, ETA-20/0603), injected through a drilled **grouting washer** (4 mm injection hole) until it squeezes out

Peikko grouting-washer dimensions **[P]**: M16 → Ø17/Ø40×6 mm; M20 → Ø21/Ø44×6; M24 → Ø26/Ø56×6; M30 → Ø32/Ø65×8; M39 → Ø41/Ø90×10; injection hole Ø4 mm in all.

**European fabricator practice** (holding-down bolts, non-proprietary): clearance holes **bolt + 6 mm**, i.e. M20 → 26 mm, M24 → 30 mm; *"for bases thicker than 60 mm, this figure may need to be increased"* **[S — steelconstruction.info via search summary; verify before quoting]**.

### 4.6 The comparison table the report needs

| Bolt | Structural bolt hole (EN 1090-2 T.11) | EN 1992-4 T.6.1 max d_f | European HD-bolt practice (+6 mm) | AISC DG1 anchor rod hole (nearest size) | Ratio AISC / structural |
|---|---|---|---|---|---|
| M16 (⅝ in) | 18 mm (+2) | 18 mm | 22 mm | ~28 mm (⅝ in interpolated) | ~5–6× |
| **M20 (¾ in)** | **22 mm (+2)** | **22 mm** | **26 mm** | **33 mm (+14)** | **7×** |
| M24 (1 in) | 26 mm (+2) | 26 mm | 30 mm | 46 mm (+21) | 10× |
| M30 (1¼ in) | 33 mm (+3) | 33 mm | 36 mm | 52 mm (+21) | 7× |
| M36 (1½ in) | 39 mm (+3) | — | 42 mm | 59 mm (+21) | 7× |
| M48 (2 in) | 51 mm (+3) | — | 54 mm | 83 mm (+32) | 10× |

> **The engineering meaning of this divergence:** AISC buys erection tolerance with a big hole and a heavy plate washer, and accepts that the base plate can slip before bearing on the rod (DG1 §3.5.3: *"considerable slip of the base plate may occur before the base plate bears against the anchor rods. The effects of this slip must be evaluated by the engineer... due to placement tolerances, not all of the anchor rods will receive the same force"* **[P]**). EN 1992-4 refuses to accept the slip: it keeps the design hole at +2/+3 mm, and if you make it bigger for erection you must **fill the gap** (injection grout or spring pin) or **lose shear capacity**. Both are coherent; mixing them is not.

---

## PART 5 — LEVELLING METHODS

Three methods are recognised. **AISC DG1 §2.9 [P]:** *"There are three common methods of setting elevations: setting nuts and washers, setting plates, and shim stacks. Project requirements and local custom generally determine which of these methods is used."*

### 5.1 Levelling (setting) nuts and washers

**Method:** a nut + washer is run down each rod to the required underside-of-plate level; the plate is landed on the four nuts; top nuts and washers are fitted and tightened; the column is plumbed by differential adjustment of the levelling nuts.

**Advantages [P]:**
- Cheapest and fastest; *"The use of four anchor rods has made the setting nut and washer method of column erection very popular, as it is easy and cost effective."*
- *"Once the setting nuts and washers are set to elevation, there is little chance they will be disturbed."*
- *"The four-rod layout provides a stable condition for erection, especially if the anchor rods are located outside of the column area."*
- Elevation **and plumbness** both adjustable, and re-adjustable

**Disadvantages [P]:**
- **The rods carry the full erection compression.** *"their strength should be checked for push out at the bottom of the footing."*
- **The levelling nut keeps working after grouting.** *"Even after the base plate is grouted, the setting nut will transfer load to the anchor rod."*
- DG1 restricts its use: *"It is recommended that use of the setting nut and washer method be limited to columns that are relatively lightly loaded during erection."*
- The rod between the levelling nut and the concrete is an unbraced compression member — Hilti advises checking buckling where the exposed compression length **ℓ > 3·d** **[P]**
- Creates a **grouted stand-off** by definition — engages EN 1992-4 §6.2.2.3 (see §1.3)

**Practical detail [P]:** EN 1090-2:2018 clause 10.8 requires the project to state *"If levelling nuts on the foundation bolts under the base plate are to be removed"* — i.e. removal after grouting is an option that must be explicitly specified, not assumed.

### 5.2 Setting plates / levelling plates

**Method [P]:** a thin steel plate slightly larger than the base plate is grouted and tapped down to exact level *before* the column arrives. The column then lands on a true, hard, level surface.

**Dimensions [P]:**
- *"Setting plates are usually about **¼ in. [6 mm]** thick and slightly larger than the base plate."*
- *"Because a plate this thin has a tendency to warp when fabricated, setting plates are typically limited to a maximum dimension of about **24 in. [600 mm]**."*
- *"If the setting plate is also to be used as a template, the holes are made **1/16 in. [1,6 mm]** larger than the anchor rod diameter. Otherwise, standard anchor rod hole sizes are used."*

**Procedure [P]:** rods set → setting plate removed and rods checked → bearing area cleaned → elevation set with jam nuts or shims → grout spread → plate tapped down to elevation → *"The elevation should be rechecked after the plate is set to verify that it is correct. If necessary, the plate and grout can be removed and the process started over."*

**Advantages [P]:**
- *"a very positive method for setting column base elevations"*
- *"provide a positive check on anchor rod settings prior to the start of erection and provide the most stable erection base for the column"*
- *"should be considered when the column is being erected in an excavation where water and soil may wash under the base plate and make cleaning and grouting difficult after the column is erected"*
- No load in the anchor rods; the steel plate is a hard bearing surface from the first pick
- A steel template can be **reused** as the setting plate

**Disadvantages [P]:**
- *"somewhat more costly than setting nuts and washers"*
- *"One problem with using setting plates is that warping in either the setting plate or the base plate, or column movement during 'bolt-up', may result in gaps between the setting plate and base plate."* DG1 judges *"Generally, there will still be adequate bearing and the amount of column settlement required to close the gap will not be detrimental"*, and directs the acceptability of gaps to **AISC Specification §M4.4**.
- Not re-adjustable once the grout under it has set

### 5.3 Shim stacks / shim packs

**Method [P]:** *"Steel shim packs, approximately **4 in. [100 mm]** wide, are set at the four edges of the base plate."*

**Advantages [P]:**
- *"the advantage that **all compression is transferred from the base plate to the foundation without involving the anchor rods**"*
- *"The areas of the shim stacks are typically large enough to carry substantial dead load prior to grouting of the base plate."*
- Cheap, universally available, and the load path is honest

**Disadvantages:**
- **Point loading.** This is what killed the Colorado frame (STRUCTURE mag) — a shim stack punched into the top of a concrete pilaster and the frame dropped 50 mm **[P]**. Shim stacks are a *temporary* device sized for erection loads, not for four storeys of dead load.
- Shims obstruct grout flow and are hard to grout around fully.
- **ACI 351.1R-99 §5.5 [P]:** *"Shims and jack bolts have a direct impact on dry packing. Shims can be displaced causing movement, and both can prevent proper compaction."*
- Base plates *"are not generally designed to bear on shims"* **[S]**
- Removal is often specified but rarely done. ACI 351.1R-99 §9.1 quality-control item 7 **[P]**: *"Shims, wedges, or other leveling devices are removed, if required, and any necessary repairs are made."*
- EN 1090-2:2018 clause 10.8 requires the project to state *"If packings subsequently to be grouted, may be placed so that the grout does not totally enclose them"* and *"If material of shims is to be different from flat steel"* **[P]** — i.e. the default is that packings ARE fully enclosed by grout, and shims ARE flat steel.

**UK practice [S]:** *"Column bases are generally adjusted for level using steel shims or folding wedges positioned centrally under the base plate, with steel wedges symmetrically positioned around the perimeter close to corners and holding down bolts loosely tightened prior to setting out being checked and the base grouted."*

### 5.4 Levelling screws / adjusting screws — large base plates

**AISC DG1 §2.9.4 [P]:** where crane capacity or handling makes it advantageous to set the base plate before the column, the plate is furnished with **wedge-type shims** or **levelling/adjusting screws**: *"Leveling-screw assemblies consist of sleeve nuts welded to the sides of the plate and a threaded rod screw that can be adjusted. These plates should be furnished with hole sizes as shown in Table 2.3. The column shaft should be detailed with stools or erection aids, as required. Where possible, the column attachment to the base plate should avoid field welding because of the difficulty in preheating a heavy base plate for welding."*

### 5.5 Selection guidance

| Situation | Preferred method |
|---|---|
| Light column, ≤ ~2 storeys erection load, 4 rods outside the column footprint | Levelling nuts |
| Heavy column, high erection dead load, base plate ≤ 600 mm | Setting/levelling plate (pre-grouted) |
| Base plate > 600 mm, or plate must be set before the column | Levelling screws / adjusting screws on the plate |
| Wet excavation, risk of soil/water washing under the plate | Setting plate |
| Anchor rods designed for tension/moment at capacity | **Avoid levelling nuts** — do not add compression + bending to a rod sized for full tension |
| Seismic / dynamic / fatigue | Shim stack or setting plate; keep t_grout within EN 1992-4 limits; grout promptly |

---

## PART 6 — GROUTING

### 6.1 Grout strength

| Source | Requirement |
|---|---|
| **AISC DG1 §2.10 [P]** | *"Grout should have a design compressive strength **at least twice the strength of the foundation concrete**. This will be adequate to transfer the maximum steel bearing pressure to the foundation."* |
| **EN 1992-4 §6.2.2.3 [P]** | Grout strength **≥ base concrete strength and ≥ 30 N/mm²** |
| **Peikko HPM §1.2.2 [P]** | *"The grout must have a design compressive strength of at least **30 N/mm²** or equal to the strength of the rough base structure (EN 1992-4 Chapter 6.2.2.3)."* Injection grout for the annulus: **≥ 40 N/mm²** |
| **Peikko post-installed in corrugated tube [P]** | non-shrink self-compacting grout **f_ck,cube ≥ 25 N/mm²**, max aggregate Ø3 mm (alt. 4 mm) |
| **Sika / EN [S]** | BS EN 1504-6 Class R4 shrinkage-compensated grout |
| **ASTM (US)** | ASTM C1107 packaged dry hydraulic-cement non-shrink grout (Grades A/B/C by expansion mechanism) |

### 6.2 Cementitious vs. epoxy — the decision

| Property | Non-shrink cementitious (ASTM C1107 / EN 1504-6 R4) | Epoxy |
|---|---|---|
| Typical strength | 40–80 N/mm² | **120 N/mm²** (Hilti CB-G EG default in PROFIS) **[P]** |
| Substrate condition | **Saturated surface dry (SSD)** — must be wet | **Dry**, sandblasted to bright metal **[P]** |
| Dynamic / impact loading | Adequate for static; less tolerant | **Preferred** for vibrating machinery, impact |
| Chemical resistance | Poor to moderate | Excellent |
| Thermal expansion | Similar to concrete | ~2–3× concrete → thermal stress, needs expansion joints in long pours |
| Void tolerance (ACI 351.1R) **[P]** | ≤ 5 % of plate area | up to **25 %** if bearing stress permits |
| Curing | Moist cure ≥ 7 days (ACI 351.1R §8.1.2) **[P]** | Not moisture sensitive; protect from temperature extremes |
| Cost | Low | High |
| Base-plate release | Bonds to plate | *"the bottom and sides of the baseplate should be thoroughly waxed to prevent bonding"* for test plates **[P]** |

**ACI 351.1R-99 §6.4 [P]:** metal surfaces in contact with cementitious grout must be *"cleaned of all paint, oil, grease, loose rust, and other foreign matter."* For epoxy, *"the metal surface should be sandblasted to bright metal unless the manufacturer states sandblasting is not necessary."*

### 6.3 Grout thickness — minimum and maximum

**ACI 351.1R-99 §5.5, "Clearances" [P]** — the most quantitative source available:

> *"For flowable hydraulic cement and epoxy grouts placed by gravity, the minimum thickness should be about **1 in. (25 mm) for 1 ft (300 mm) flow length**. For each additional ft (300 mm) of flow length, the thickness should be increased about **½ in. (13 mm)** to a maximum of about **4 in. (100 mm)**. For grouts with a plastic consistency placed by gravity, the clearances should be increased by **½ to 1 in. (13 to 25 mm)** above that designated for flowable grouts. For fluid grouts, the clearance can generally be reduced by **¼ to ½ in. (6 to 13 mm)**, but **should not be reduced to less than 1 in. (25 mm)**."*
>
> *"For installations to be dry-packed, the clearances should be about **1 to 3 in. (25 to 75 mm)**. Large clearances make compaction impractical. To allow proper compaction, the **width of the area to be dry-packed from any direction should be less than 18 in. (460 mm)**."*

**A usable design rule from the above:**
```
t_grout,min ≈ 25 mm + 13 mm × (L_flow / 300 mm − 1)     [flowable, gravity]
capped at 100 mm
```
where `L_flow` = the longest distance the grout must travel from the placing point, in mm.

**AISC DG1 §2.10 [P]:**
> *"If the column is set on a finished floor, a **1-in. [25 mm]** space may be adequate, while on the top of a footing or pier, normally the space should be **1½ in. to 2 in. [38 to 50 mm]**. Large base plates and plates with shear lugs may require more space."*

**European practice [S]:** *"the minimum space between the underside of the base plate and the concrete foundation should be **25 mm** for grouting and **50 mm** for mortar bedding"*; *"Typically in the UK a nominal **25 mm** would be allowed for"*; *"the generally maximum thickness of a high strength free-flowing grout suggested by manufacturers is **110 mm**."*

**Manufacturer practice [P]:** RPP specifies **50 mm** grout thickness for M16–M30 and **60 mm** for M39.

**Design cap for EN 1992-4 "no lever arm" [P]:** t_grout ≤ 0,5·d (Peikko: 8/10/12/15/19 mm for M16/20/24/30/39). Hilti's SOFA method extends to **100 mm** for static conditions and states applicability up to **130 mm**, but that is a proprietary method outside the Eurocode.

> **Report this as a genuine tension:** constructability wants 25–50 mm; EN 1992-4 wants ≤ 0,5·d (≈10–12 mm for M20/M24). The resolution in practice is: **do not rely on anchor-rod shear across a normal grout bed.** Take shear by friction under the plate or by a shear lug, and design the rods for tension only. If you must use rod shear, do the lever-arm (bolt bending) check.

### 6.4 Grout holes and vent holes

**AISC DG1 §2.10 [P]:**
> *"Grout holes are not required for most base plates. For plates **24 in. [600 mm] or less in width**, a form can be set up and the grout can be forced in from one side until it flows out the opposite side. When plates become larger or when shear lugs are used, it is recommended that **one or two grout holes** be provided. Grout holes are typically **2 to 3 in. [50 to 75 mm]** in diameter and are typically **thermally cut** in the base plate. A form should be provided around the edge, and some sort of filling device should be used to provide enough head pressure to cause the grout to flow out to all of the sides."*

**ACI 351.1R-99 §5.2 [P]** — the authoritative detail:
> *"If grout cannot be placed from one edge and flowed to the opposite edge, **air vent holes must be provided** through the plate to prevent air entrapment. A vent hole **¼ to ½ in. (6 to 13 mm)** in diameter should be placed through the plate **at the intersection of all crossing stiffeners and at each point where air may be trapped**."*
>
> *"If possible, grout holes for placement of the grout should be located so that **grout does not travel more than about 48 in. (1,2 m)**. The grout holes should be placed so that grouting can be started at one hole and continued at other holes to insure that the grout flows under all areas of the plate."*
>
> *"Holes for **pumping** grout are typically **¾ to 2 in. (19 to 50 mm)** in diameter and **threaded for standard pipe threads**. Grout holes for **free-pouring** grout are typically **3 to 6 in. (75 to 150 mm)** in diameter."*
>
> *"The machine base should be detailed so that grout can be placed beneath the plate without trapping water or air in unvented corners. **If possible, perpendicular stiffeners should be placed above the plate.**"*

**Rules of thumb the fabricator can apply:**

| Plate width B | Requirement |
|---|---|
| B ≤ 600 mm, no stiffeners/shear lug | No grout hole; form + head box, place from one side |
| B > 600 mm, or shear lug present | 1–2 grout holes, Ø50–75 mm thermally cut (AISC) or Ø75–150 mm (ACI free-pour) |
| Any flow path > 1,2 m | Add a grout hole to break the flow path |
| Stiffeners on the underside | Ø6–13 mm vent at **every** stiffener intersection and every high point |
| Compartmented plate | Vent in the **corner** of each compartment, **paired / on both sides of every internal stiffener** **[S]** |

### 6.5 Formwork, head box and pouring technique

**ACI 351.1R-99 §6.5.2 [P]:**
- Forms rigid, tight-fitting, taped or caulked, extending **≥ 1 in. (25 mm)** above the highest grout elevation
- Coat with form oil/wax or line with polyethylene; **do not contaminate** the concrete or plate underside
- **Head box:** *"The headbox should begin **2 to 4 in. (50 to 100 mm)** from the plate and **slope away from the plate at about 45 degrees**. The slope on the form permits the grout to be poured under the plate with a minimum of turbulence and air entrapment."*
- Opposite (outlet) form: **50–100 mm** from the plate, extending **≥ 25 mm** above the plate underside
- **Head height:** *"the height above the highest grout elevation under the plate should be about **1/5 of the travel distance** for the grout"*
- Forms on sides **parallel** to the flow: *"generally be less than 1 in. (25 mm) from the plate"*
- Pumped placements: forms **≥ 4 in. (100 mm)** outside the plate on all sides

**Sika Method Statement (cementitious grouting of base plates) [P]:**
- Substrate roughened to **CSP 6 to 8**; all dust removed by sandblasting or high-pressure water
- Foundations **< 28 days** old kept wet **≥ 12 h**; older foundations **≥ 24 h**; free water removed; surface **SSD** ("dark matt appearance without glistening")
- *"Side and end forms shall be placed a **minimum of 25 mm** from the base being grouted. All vertical and horizontal edges shall be chamfered a **minimum of 15 mm at 45°**. In addition, the **top of the chamfer should be no more than 3 to 5 mm above the bottom of the base plate**."*
- Grout poured **within 15 minutes** of mixing to optimise expansion
- *"**Never grout from two places on the same application** as it will be difficult to determine if the entire void under the base plate has been filled."*
- *"Keep pouring until the grout is up to the top of the chamfer on the formwork. This will force the material to the underside of the baseplate and **achieve an effective bearing area without any voids**. Always pour grout **from opposite ends towards any vent holes**."*
- *"Make sure **vent holes are not obstructed**"*; *"Do not vibrate the formwork"*
- Substrate min. temperature **10 °C**; pre-condition product **18–29 °C for 48 h**; no application if frost risk within 24 h
- Mix **3 minutes** total, low-speed drill **300–450 rpm**
- **Dry pack:** ~2,8 L water per 25 kg bag; ball test in a gloved hand; *"Pack **no more than 40 mm of grout at a time** working from the back side of the form to the front"*
- Cure exposed surfaces **≥ 3 days**

**ACI 351.1R-99 §7.4.1 [P] — the placing rule that prevents voids:**
> *"**All placement should be made from one side of the plate.** Placement should begin at one end of the plate and continue at that point until the grout rises above the bottom of the plate on the opposite side. Then, the placement point or portable headbox should be moved slowly along the side of the plate from one end to the other... The **continuous movement of a single face of grout** prevents air entrapment. Grout should not be placed at various locations along one side because the movement of the grout cannot be monitored and air can easily be trapped between placing points. For the same reason, **grout should not be poured toward the center from opposite ends**."*
>
> *"To encourage flow of grout, **steel packing straps** can be inserted on placement side and moved slowly back and forth. **Chains should not be used** because they tend to entrap air bubbles."*

**Pumped placement [P]:** *"Grout should be pumped into that inlet until it flows up into an adjacent inlet and flows from the entire plate perimeter adjacent to the inlet... **Grout should not be pumped into more than one inlet simultaneously** or before grout flow has reached an adjacent inlet because air may be trapped."*

### 6.6 Anchor-bolt sleeves and pockets — grout them FIRST

**ACI 351.1R-99 §6.2 [P] — a sequencing rule that is routinely broken:**
> *"Anchor-bolt sleeves and holes that are to be grouted **should be grouted before pouring grout under the plate**. This is necessary to assure that the grout maintains contact with the plate. If the total placement is attempted in a single pour, air and (in the case of hydraulic cement grouts) unremoved water may rise to the grout surface. This will result in settlement of the grout, **seriously reducing the contact areas of the plate**."*

Sleeves must be *"cleaned of debris, dirt, and water by oil-free compressed air or vacuum"*; concrete in the holes saturated **24 h** then water removed. **[P]**

Peikko: *"**Bolt recesses must be grouted**... The grout must be non-shrink grade and strength according to plans. To avoid air being trapped in the joint, it is recommended to **pour grout from one side of the column only**."* **[S/P]**

### 6.7 Cut-back, curing and protection

**ACI 351.1R-99 §7.5, §8.1 [P]:**
- *"No forms or grout (except spillage) should be removed... until the grout has stiffened sufficiently to ensure that the grout will not sag below plate level when **cut back at a slope of about 45 deg from the bottom of the plate**. The sloped surface provides some later confinement for the grout under the plate and provides a more uniform dispersal of the compressive stresses near the plate edge."*
- Epoxy grouts *"are formed to the desired configuration and poured to the desired final elevation. Epoxy grouts are not generally cut back."*
- Moist cure: *"kept wet and saturated for at least **7 days**"*
- Cold weather: maintain **≈ 50 °F (10 °C) for at least 3 days** and protect from freezing for **3 additional days**
- Hot weather: ambient **below 100 °F (38 °C) for at least 3 days**
- Epoxy: *"At temperature near 0 °F (−18 °C), the polymerization of many epoxies will nearly cease"*; keep ambient < 38 °C

**STRUCTURE magazine [P]:** substrate **45–90 °F (7–32 °C)**; grout within **15 °F (8 °C)** of substrate temperature; minimum **24 h** cure between 45 °F and 90 °F; add pea gravel for joints **wider than 3 in. (75 mm)**.

### 6.8 Contractual interface — a named failure mode

**AISC DG1 §2.10 [P]:**
> *"Grouting is an interface between trades that provides a challenge for the specification writer. Typically, the grout is furnished by the concrete or general contractor, but the timing is essential to the work of the steel erector. Because of this, specification writers sometimes place grouting in the steel section. **This only confuses the issue** because the erector then has to make arrangements with the concrete contractor to do the grouting. **Grouting should be the responsibility of the concrete contractor**, and there should be a requirement to **grout column bases promptly** when notified by the erector that the column is in its final location."*

**STRUCTURE magazine [P]:** *"International Building Code has **no special inspection requirements** for grouting currently."* Recommends: document completion dates and locations in daily field reports; inspect joints before they are concealed; verify grouting completion before placing supported slabs; and notes that **ASTM C109 2-in. cube testing is not recommended** as representative of actual joint conditions.

---

## PART 7 — COMMON DEFECTS, SITE PROBLEMS AND PERMITTED REPAIRS

### 7.1 The governing rule for ALL repairs

**OSHA 1926.755(b)(1) [P]:** *"Anchor rods (anchor bolts) shall not be repaired, replaced or field-modified without the approval of the project structural engineer of record."*

**AISC DG1 §2.11 [P]:** *"On a case-by-case basis, the Engineer of Record must evaluate the relative merits of a proposed repair as opposed to rejecting the foundation and requiring the contractor to replace part of the foundation with new anchor rods per the original design. **Records should be kept of the repair procedure and the results.** The Engineer of Record may require special inspection or testing deemed necessary to verify the repair."*

### 7.2 Anchor rods in the wrong position

**Diagnostic questions DG1 asks first [P]:** *"Is the repair required for only one rod or for the entire pattern of rods? How far out of position is the rod or pattern, and what are the required strengths of the rods?"*

**The threshold [P]:** AISC Structural Steel Educational Council, quoted in ASCC PS#14: *"If bolts are misplaced up to **½ inch [13 mm]**, the oversized base plate holes normally allow the base plate and column to be placed near or on the column line. If the bolts are misplaced by **more than ½ inch**, then corrective work is required."*

| Situation | Permitted repair | Status | Source |
|---|---|---|---|
| Error found **before** base plate fabricated | Change the hole pattern, or use a different base plate | **Preferred** | DG1 §2.11.1 **[P]** |
| Rods interfere with the **column shaft** | Modify the column shaft by cutting and reinforcing sections of flange or web | Permitted, EOR design | DG1 §2.11.1 **[P]** |
| **One or two rods** misplaced, column already fabricated/shipped | **Slot the base plate and use a plate washer to span the slot** — *"the most common repair"* | **Standard accepted repair** | DG1 §2.11.1 **[P]** |
| **Entire pattern** off uniformly | *"cut the base plate off and offset the base plate to accommodate the out of tolerance. **It is necessary to check the base plate design for this eccentricity.** When removing the base plate, it may be required to turn the plate over to have a clean surface on which to weld the column shaft."* | Permitted, EOR check required | DG1 §2.11.1 **[P]** |
| Rod **more than a couple of inches (≈50 mm)** out | *"the best solution may be to cut off the existing rods and install new drilled-in epoxy-type anchor rods"* | Permitted with conditions | DG1 §2.11.1 **[P]** |

**Conditions on post-installed replacement anchors [P]:** *"carefully follow the manufacturer's recommendations and provide inspection as required in the applicable building code. **Locate the holes to avoid reinforcing steel** in the foundation. **If any reinforcing steel is cut, a check of the effect on foundation strength should be made.**"*

**Detailing the slot repair.** DG1 does not give slot dimensions, so derive them:
- Slot length ≈ existing hole diameter + 2 × (measured misplacement) + a working allowance
- The washer plate must cover the slot with the rod at either extreme: `plate length ≥ 2·L_slot − d_rod`, `plate width ≥ 2·d_hole − d_rod` (§4.4 rule)
- Washer plate thickness ≥ `d_rod/3` (DG7 rule) and checked for pull-through and bending across the slot
- **Weld the washer plate to the base plate only if the rods must carry shear** — DG1 §2.6: *"Washers should not be welded to the base plate, except when the anchor rods are designed to resist shear at the column base"*; DG1 §3.5.3: for shear transfer, *"a plate washer welded to the base plate between the anchor rod nut and the top of the base plate. The plate washers should have holes **1/16 in. [1,6 mm]** larger than the anchor rod diameter."* **[P]**
- Check base-plate net section and edge distance after slotting

### 7.3 Anchor rods bent or not vertical — bending and heating

**AISC DG1 §2.11.2 [P]** — the rules, and which are permitted:

> *"ASTM F1554 permits both cold and hot bending of anchor rods to form hooks; however, **bending in the threaded area can be a problem**. It is recommended that **only Grade 36 rods be bent in the field** and the **bend limited to 45° or less**. Rods up to about **1 in. [25 mm]** in diameter can be **cold bent**. Rods over 1 in. can be **heated up to 1,200 °F [650 °C]** to make bending easier. It is recommended that bending be done using a rod-bending device called a **hickey**. After bending, the rods should be **visually inspected for cracks**. If there is concern about the tensile strength of the anchor rod, the rod can be **load tested**."*

**Therefore:**

| Action | Status |
|---|---|
| Cold bend Grade 36 rod ≤ 25 mm dia., ≤ 45°, with a hickey | **Permitted** (with EOR approval) |
| Hot bend Grade 36 rod > 25 mm dia., heat ≤ 650 °C | **Permitted** (with EOR approval) |
| Bending **Grade 55 / Grade 105** (or class 8.8 / 10.9) rods in the field | **Not recommended** |
| Bending **in the threaded zone** | **Not recommended** |
| Bend > 45° | **Not recommended** |
| Heating above 650 °C, or heating quenched-and-tempered rods | **Prohibited in effect** — destroys the temper |
| Bending without post-bend crack inspection | **Not acceptable** |
| **Welding a nut to the rod to make up short engagement** | **Explicitly not recommended** — *"Welding the nut to the anchor rod is not a prequalified welded joint and is not recommended."* **[P]** |

**Physical damage [P]:** *"Rods can also be damaged in the field by equipment, such as when backfilling foundations or performing snow removal. **Anchor rod locations should be clearly flagged** so that they are visible to equipment operators working in the area."* (DG1 Figure 2.3 shows rods run over by a crane after being hidden under snow.)

### 7.4 Projection too long or too short

**AISC DG1 §2.11.3 [P]:**
- *"Anchor rod projections that are too short or too long must be investigated to determine if the correct anchor rods were installed. **If the anchor rod is too short, the anchor rod may be projecting below the foundation. If the rod projection is too long, the embedment may not be adequate** to develop the required tensile strength."*
- **Partial nut engagement:** *"A conservative estimate of the resulting nut strength can be made based on the percentage of threads engaged, **as long as at least half of the threads in the nut are engaged**."*
- **Erection-only rods, too short:** *"the most expedient solution may be to cut or drill another hole in the base plate and install a drilled-in epoxy-type anchor rod."*
- **Tension rods, too short:** extend by **coupling nut** (DG1 Table 2.4 gives IFI #128 hex coupling nut dimensions, ASTM A563 Gr. A) or by **welding on threaded rod**. *"This fix will require **enlarging the anchor rod hole** to accommodate the coupling nut along with **using oversize shims** to allow the plate washer and nut to clear the coupling nut."*
- **Welded extension:** permitted for **ASTM F1554 Grade 36** and **Grade 55 with supplement S1** only. *"Butt welding two round rods together requires special detailing that uses a **run out tab** in order to make a proper groove weld... The run-out tab can be trimmed off after welding, if necessary, and the rod can even be ground flush if required."* Splice bars are an alternative. *"Either of these welded details can be designed to develop a **full-strength splice**."* See AISC DG21.
- **Too long:** *"it is easy to add plate washers to attain an adequate thread length."*

**ASTM F1554 supplementary requirements [S]:** **S1** = weldable version of Grade 55 (carbon equivalent **CE ≤ 0,45**); **S2/S3** = permanent manufacturer/grade identification; **S4** = Charpy at **+40 °F**, min avg **15 ft-lb** for 3 specimens, no specimen below **12 ft-lb** (Grades 55 and 105).

### 7.5 Insufficient embedment

- If the projection is long, embedment is short (§7.4). The tension capacity chain is: steel → bond/pull-out → **concrete cone breakout** → splitting → blow-out. Reduced h_ef attacks the cone term, which varies as **h_ef^1,5** (EN 1992-4 §7.2.1.4 / ACI 318-19 §17.6.2). A 20 % embedment shortfall costs ~28 % of cone capacity.
- **Repair options:** supplementary reinforcement designed to carry the anchor force (EN 1992-4 §7.2.1.9 / Peikko Annexes A1/B1 **[P]**); or cut off and replace with post-installed anchors at full embedment.
- **Peikko's supplementary-reinforcement principle [P]:** *"If the anchor bolt's tensile or shear steel resistance cannot be fully developed due to concrete failure, then the supplementary reinforcement may be used to carry the forces from the anchor bolt."*

### 7.6 Hole not cleaned — bonded (adhesive) anchors

**The number:** *"If no or improper cleaning is performed, the **bond strength can be reduced up to or even more than 60 %** of the value for a cleaned hole."* — Hilti, *Influence of Borehole Cleaning* **[P]**

**Mechanism:** hammer drilling produces fine concrete dust that coats the borehole wall. Unremoved, the resin bonds to **dust**, not to the concrete substrate.

**The procedure:** blow–brush–blow, with system-specific numbers of iterations, a **proprietary steel wire brush of the correct diameter**, and a specified compressed-air pressure (oil-free). **[S]**

**Qualification linkage [S]:** *"In the subsequent reliability test, the same anchor configuration is tested at **half the number of cleaning steps** of the reference test... With the lowest possible reliability test cleaning procedure of **1 blow, 1 brush, 1 blow**, the lowest allowable reference test cleaning procedure is **2 blow, 2 brush, 2 blow**."* — i.e. the ETA/ICC-ES value is only valid for the MPII cleaning regime.

**Code response — ACI 318-19 [S]:**
- **§26.7.1(l):** adhesive anchors installed **horizontally or upwardly inclined** and resisting **sustained tension** must be installed by a **certified installer** (ACI/CRSI Adhesive Anchor Installer Certification, ACI CPP 680.1-17 or equivalent)
- **§26.7.2:** post-installed anchors shall be installed in accordance with the **Manufacturer's Printed Installation Instructions (MPII)**
- Those same anchors require **continuous special inspection**

### 7.7 Torque not applied / nuts loose

- **Erection-critical:** DG1 §2.9 — all rods must be tightened before the load line is released **[P]**
- **Galvanised rods loosen:** *"even properly tightened galvanized anchor rods can subsequently become loose, especially in the first few days after installation, presumably because of **creep in the galvanizing**. Therefore, a **final installation check should be made after at least 48 hours** using a calibrated wrench and **110 % of the torque calculated using the torque equation**."* **[P]**
- **Locking:** *"When it is required that the nuts be prevented from loosening, a **jam nut** or other suitable device can be used... Tack welding the top side of the top nut has been used, although this is not consistent with the AWS Structural Welding Code. While tack welding to the unstressed top of the anchor rod is relatively harmless, **under no circumstance should any nut be tack welded to the washer or the base plate**."* **[P]**
- **Lock washers must not be used** on anchor rods **[P]**
- **Galvanic rule:** *"Galvanized nuts or washers should not be used with unpainted weathering steel."* **[P]**
- **Galvanising friction:** *"galvanizing increases friction between the nut and the rod and even though the nuts are over tapped, special lubrication may be required."* **[P]** Mixing hot-dip (ASTM A153) rods with mechanically galvanised (ASTM B695) nuts *"may result in an unworkable assembly"* — buy rods and nuts from **one supplier, shipped pre-assembled**.

### 7.8 Grout voids

Causes (Hilti, ACI 351.1R, Sika) **[P]**: dry-pack surface-only packing; incomplete mixing; grouting from two points; pouring toward the centre from both ends; insufficient head; no vent holes at stiffener intersections; grouting the plate before the bolt sleeves; bleeding; premature drying; forms too close/too far.

**Hilti on dry pack [P]:** *"Dry-pack grouts... are susceptible to errors that could lead to incomplete filling of the space... Such conditions could lead to cracking, voids, degradation, inconsistent/low grout strength, and uneven stress transfer between the steel plate and the grout. **Cracking and voids allow moisture to enter and pool, which can lead to accelerated corrosion and degradation of the connection, even in comparison to an ungrouted connection.** For these reasons, **dry packing is recommended only if you can ensure the quality of installation meets engineering requirements**."*

**Beveled washers [P]:** required if the top face of the base plate slopes more than **1:20** (DG1 body text) or **1:40** (DG1 Table A2 note c) — *report both; DG1 is internally inconsistent*. Beveled washers *"can typically accommodate a slope up to 1:6."*

### 7.9 Defect–repair decision table (summary for the report)

| Defect | Repair | Permitted? |
|---|---|---|
| Bolt ≤ 13 mm out | None — oversized hole absorbs it | ✔ automatic |
| 1–2 bolts > 13 mm out | Slot plate + spanning plate washer | ✔ with EOR approval |
| Whole pattern out uniformly | Cut plate off column, re-offset, re-weld (check eccentricity) | ✔ with EOR design check |
| Bolt > ~50 mm out | Cut off; new drilled-in adhesive anchor | ✔ with EOR + special inspection |
| Rod out of plumb, Gr.36, ≤ 25 mm, ≤ 45° | Cold bend with hickey + crack inspection | ✔ with EOR approval |
| Rod out of plumb, Gr.36, > 25 mm | Hot bend ≤ 650 °C + crack inspection | ✔ with EOR approval |
| Rod out of plumb, Gr.55/105 or class 8.8/10.9 | Bending | ✘ not recommended |
| Bending in the threaded length | — | ✘ not recommended |
| Rod short, erection-only | New hole in plate + adhesive anchor | ✔ |
| Rod short, tension design | Coupling nut (enlarge hole + oversize shims) or full-strength welded splice (Gr.36 / Gr.55-S1 only) | ✔ with EOR design |
| Nut engagement < 50 % of nut threads | Accept on pro-rata basis | ✘ below 50 % |
| Welding the nut to the rod | — | ✘ not a prequalified joint |
| Tack weld nut to washer or base plate | — | ✘ prohibited |
| Rod too long | Add plate washers | ✔ |
| Grout omitted before loading | Stop; do not load | ✘ — a failure mode, not a defect |

---

## PART 8 — INSPECTION AND QA

### 8.1 What must be verified for post-installed anchors

**Special inspection classification [P — Hilti guidance article]:**
- Post-installed anchors in hardened concrete: **periodic** special inspection (IBC)
- Adhesive anchors horizontal/upwardly inclined under sustained tension: **continuous** special inspection, **certified installer**

**Verification checklist (all post-installed anchors) [P]:**
- Location, edge distance and spacing vs. contract documents
- Anchor type, material, size and **embedment depth**
- Installation per **MPII**

**Mechanical anchors:** drill bit type and size; hole depth; *"Use of a properly calibrated torque wrench is required for setting of many types of anchors"*; torque-controlled expansion anchors must reach the required torque **within one turn of the nut**.

**Adhesive anchors:** adhesive **expiry date** and storage; anchor element free of *"dust, mud, oil"*; *"Verification of hole cleaning procedures in accordance with the MPII is critical"*; **in-situ concrete temperature** for cure-time compliance; installer certification.

### 8.2 Torque verification of anchor rods

**AISC DG1 Appendix A — double-nut-moment joints [P].** This is the only place AISC gives a torque equation.

```
T_v = 0,12 · d_b · T_m
```
where
- `T_v` = **verification torque** (in.-kips)
- `d_b` = nominal body diameter of the anchor rod (in.)
- `T_m` = **minimum installation pretension** (kips) from DG1 Table A1

Notes **[P]**: *"Till (1994) has shown that a multiplier of **0,12** in this relationship is adequate for common sizes and coatings of anchor rods. Other researchers have suggested a value of **0,20** for less-well-lubricated rods."* → **flag this 0,12 vs 0,20 divergence in the report.**

**Table A1 basis [P]:** T_m = **50 %** of specified minimum tensile strength for F1554 Grade 36; **60 %** for Grades 55 and 105 and for A615/A706 bars, rounded to the nearest kip.

**Table A2 — Nut Rotation for Turn-of-Nut Pretensioning, UNC threads [P]**

| Rod diameter | F1554 Gr. 36 | F1554 Gr. 55 & 105, A615 Gr.60/75, A706 Gr.60 |
|---|---|---|
| ≤ 1½ in. | **1/6 turn** | **1/3 turn** |
| > 1½ in. | **1/12 turn** | **1/6 turn** |

Notes: *"Nut rotation is relative to anchor rod. The tolerance is **plus 20°**."* Applicable only to UNC threads. Beveled washer required if the nut is not in firm contact or the base plate outer face is sloped more than **1:40**.

**Procedure highlights [P]:**
1. Torque wrench must have a torque indicator **calibrated annually**, with certification available to the EOR; a torque multiplier may be used
2. Compute `T_v` per the equation above
3. **Rotation-capacity test on at least one rod from every lot**, before the rods are placed in the concrete, using the actual base plate (or an equivalent plate in grade, thickness and finish) restrained against movement
4. …
7. Top template may be removed **24 h** after placing concrete
8. Exposed rod cleaned with a wire brush and **re-lubricated if galvanised**
9. Visual inspection: no thread damage; position, elevation and projection within contract tolerances (default = AISC CoSP). **If the joint is designed for fatigue, misalignment from vertical shall be no more than 1:40.** Nuts run well past the levelling-nut elevation and backed off *"using an ordinary wrench without a cheater bar. Thread damage requiring unusually large effort should be reported to the Engineer of Record."*
10. Re-lubricate galvanised threads if lubricated more than 24 h earlier or wetted since
16. **Levelling nuts tightened to 20–30 % of `T_v` in a star pattern**
17. Reference position of the top nut marked on a flat intersection
— Pretensioning in **two full tightening cycles**, star pattern
— **Final check after ≥ 48 h at 110 % of `T_v`.** *"It is expected that properly tightened joints will not move even if 110 % of the minimum installation torque is applied. **If a rod assembly cannot achieve the required torque, it is very likely that the threads have stripped.**"*

**Manufacturer indicative torques — RPP Base Bolt Table 14 [P]** (column-shoe to base-bolt connection):

| Base bolt | T_min (Nm) | T_max (Nm) |
|---|---|---|
| M16 | 120 | 200 |
| M20 | 150 | 250 |
| M24 | 200 | 380 |
| M30 | 200 | 450 |
| M39 | 350 | 1000 |

### 8.3 Proof / pull testing of post-installed anchors

**California Building Code 2022 §1910A.5, "Tests for Post-Installed Anchors in Concrete" [P]** — the clearest codified regime:

- **Test load:** *"Twice the maximum allowable tension load or one and a quarter (1¼) times the maximum design strength"*, capped: *"Tension test load need not exceed **80 percent of the nominal yield strength** of the anchor element (= 0,8 A_se f_ya)."*
- **Frequency:**
  - Sill-plate bolting: **10 %** of anchors
  - **Structural applications: ALL such anchors shall be tested**
  - Non-structural components: **50 %** or alternate bolts in a group, *"including at least one-half the anchors in each group"*
- **Acceptance (hydraulic ram):** anchors must *"maintain the test load for a minimum of **15 seconds** and shall exhibit **no discernible movement** during the tension test, e.g., as evidenced by loosening of the washer under the nut."*
- **Torque option:** *"Torque-controlled post-installed anchors shall be permitted to be tested using torque based on an approved evaluation report."*
- **Witnessing:** *"The testing of the post-installed anchors shall be done in the presence of the **special inspector** and a report of the test results shall be submitted to the enforcement agency."*

**Hilti guidance on proof loading [P]:**
- Purpose: *"Verify proper anchor set **without causing damage** to correctly installed anchors."*
- Frequency: *"**No standardized percentage exists**; typical ranges are **5–20 %** of anchors by type/size, adjusted for application criticality. Highly redundant applications may require minimal 5 % sampling."*
- Load level: *"Historically set at **twice the allowable tension load**"*, ≈ **50 % of mean ultimate** anchor tension strength
- Timing: *"only after the **minimum cure time** specified in the MPII"*
- Acceptance: *"Anchors shall have **no visible indications of movement** during or after the application of the proof load."*
- Equipment: *"Hydraulic systems and torque wrenches must be **calibrated together as complete systems, traceable to NIST standards**."*

**Test method standards [S]:** ASTM E488/E488M-22 (strength of anchors in concrete); ASTM E1512 (bond performance of bonded anchors); ACI 355.2 (mechanical anchor qualification); ACI 355.4 (adhesive anchor qualification); ICC-ES AC308; EOTA TR 048/TR 049 in Europe.

> **Design a site proof-load regime like this:** define the proof load `N_p = min(2 × N_allow ; 0,8 A_se f_ya)`; define the sample (structural → 100 %; redundant non-structural → 5–20 %); hold **15 s**; accept only **zero visible movement**; witness by the special inspector; record on a per-anchor register.

### 8.4 In-service inspection of anchor rods (AISC DG1 Appendix, §A3) **[P]**

Applies to joints designed for fatigue and to all joints in Seismic Design Category D or higher after a significant seismic event.

1. **Anchor rod appearance** — draw the rod pattern, number clockwise. Check each rod for corrosion, gouges, cracks. *"Suspected cracks may be more closely examined using the **dye-penetrant technique**."* Heavy corrosion at the concrete interface implies worse corrosion hidden below. Verify all rods have top nuts **with washers**; **lock washers should not be used**; **galvanised nuts/washers not with unpainted weathering steel**. *"Check for inadequately sized washers for oversize holes."* If there is no grout pad, verify all rods have levelling nuts with washers. *"Note any anchor rods that are significantly misaligned or bent to fit in the base plate hole."*
2. **Sounding** — *"Anchor rods may be struck by a hammer (a **large ball peen hammer** is suggested) to detect broken bolts. Strike the side of the top nut and the top of the rod. **Good tight anchor rods will all have a similar ring. Broken or loose anchor rods will have a distinctly different and duller sound.**"*
3. **Tightness** — verify a sound tack weld (top of the top nut only) or a jam nut. *"Tack welds to the washer or the base plate are undesirable and should be reported."* Otherwise verify by applying **110 % of `T_v`**. *"If one nut in a joint is loose... it should be unscrewed, cleaned, inspected for possible thread [damage]... **If more than one nut is loose, the joint may have been poorly installed or fatigue problems may exist.**"*
4. **Ultrasonic testing** — required only if welded repairs have been made; or similar structures under similar loading have had fatigue problems; or the rods were not adequately designed for fatigue. *"The top of the rod or extension should be **ground flush** and the ultrasonic test and its interpretation should be in accordance with a procedure approved by a qualified engineer."*

Plus: keep the joint free of debris, water and vegetation; verify grout and concrete near the rods are in good condition; retighten as needed.

### 8.5 Grout QA

**ACI 351.1R-99 Chapter 9 [P]** — quality control shall ensure that:
1. The preblended cement or epoxy grout **has not exceeded its shelf life**
2. The foundation has been properly prepared, cleaned and **saturated** (cementitious) or **kept dry** (epoxy) and protected from contamination
3. **The formwork is tight and has adequate stiffness**
4. Required tests are performed at the specified frequency
5. Correct placing methods are used
6. **Curing is initiated at the correct time** and maintained for the correct period at the proper temperature
7. **Shims, wedges or other levelling devices are removed, if required**, and any necessary repairs made
8. Temperatures of baseplate and air are within specification

**Dry pack:** *"Dry-packing operations require **nearly constant inspection**... A worker can easily increase his production by using large layer thicknesses. If possible, an occasional dry-pack installation should be **dismantled** to check for areas of insufficient compaction."* **[P]**

**Epoxy:** at least one sample per shipment or production lot; field check of resin/hardener proportions by curing a *"small test cookie... in a toaster oven at elevated temperature."* **[P]**

**Void detection [P]:** *"[Sounding] methods generally use a **½ in. (13 mm) steel rod** to sound... dropped about 1 in. (25 mm). **A hollow sound indicates lack** [of contact]."* Sounding *"[is] not reliable for plates more than 1 in. (25 mm) thick."*

**Documentation [P]:** *"Documentation must be maintained for all job site inspection and testing. This documentation should include the **location of the installation, the type and brand of grout used, the environmental conditions at the time of grout placement, and the results of all physical tests** (for example, volume change, bleeding, and strength)."*

### 8.6 Documentation package — recommended minimum

1. **Released-for-Construction** anchor-rod layout drawing, signed
2. Template drawings and material certificates
3. **Pre-pour survey** record (template position, level, plumb) — signed by the surveyor
4. **Post-pour as-built survey** of every rod: x, y, top elevation, projection, plumb — against the specified tolerance table
5. Non-conformance register + EOR-approved repair procedures + repair records + any re-inspection/testing
6. OSHA 1926.755(b)(2) written notification to the erector
7. Levelling record: method used, shim/nut positions, achieved level and plumb, date
8. **Grout record per base:** date/time, product + batch, mix water, ambient/substrate/grout temperature, placement method, head height, vent operation, cut-back, curing method and duration, cube/cylinder results
9. Torque records: wrench calibration certificate, rotation-capacity test result per lot, installation torques, **48-hour 110 % re-check**
10. Post-installed anchor register: MPII reference, drill bit, hole depth, cleaning regime, adhesive lot + expiry, cure time, proof-load result, installer certification
11. Photographic record of the underside of the plate before grouting and of the grout cut-back after

---

## PART 9 — WORKED EXAMPLES

### 9.1 Worked Example 1 — Hole size for a 4 × M20 (¾ in) base, and why it must be 33 mm

**Given.** Cast-in ASTM F1554 Gr. 36 ¾ in (M20) rods, 4-rod square pattern at 300 mm centres. Concrete set by a contractor working to ACI 117-10. Base plate fabricated to EN 1090-2 Class 1.

**Step 1 — Tolerance budget.**

| Source | Clause | Value |
|---|---|---|
| Individual bolt centreline from specified location | ACI 117-10 §2.3.4.2 (¾ in bolt) | ±6,4 mm |
| Bolt-to-bolt within group (alternative check) | AISC 303-05 §7.5.1(a) | ±3,2 mm |
| Group centre from column line | AISC 303-05 §7.5.1(e) | ±6,4 mm |
| Base plate hole position (fabrication) | typical EN 1090-2 functional | ±2 mm |

**Step 2 — Method A (code-consistent, AISC's stated basis).**
```
d_hole ≥ d_rod + 2·Δ_bolt + 2·Δ_fab
       = 19,1 + 2(6,4) + 2(1,6)
       = 19,1 + 12,7 + 3,2
       = 35,0 mm
```
AISC DG1 Table 2.3 specifies **1-5/16 in = 33,3 mm** (clearance 14,3 mm).

**Step 3 — Method B (statistical, RSS).**
```
Δ_eff = √(6,4² + 3,2² + 2,0²) = √(41,0 + 10,2 + 4,0) = √55,2 = 7,4 mm
d_hole ≥ 19,1 + 2(7,4) = 33,9 mm  →  AISC's 33,3 mm
```

**Step 4 — Compare with a structural bolt.**
```
M20 structural bolt hole (EN 1090-2 Table 11):  22 mm   (clearance 2 mm)
M20 anchor rod hole (AISC DG1 Table 2.3):       33 mm   (clearance 14 mm)
Ratio: 7,0 : 1
```

**Step 5 — Washer.**
```
d_washer ≥ 2·d_hole − d_rod = 2(33,3) − 19,1 = 47,5 mm   →  AISC specifies 51 mm ✔
t_washer ≈ d_rod/3 = 19,1/3 = 6,4 mm                     →  AISC specifies ¼ in = 6 mm ✔
```

**Step 6 — Consequence for the plate size.**
Clear distance hole-edge to plate-edge, DG1 minimum **51 mm**:
```
B_min = 300 (bolt centres) + 33,3 (hole) + 2(51) = 435 mm
```
Round to **450 × 450 mm**. If the fabricator had used a "structural" 22 mm hole, the plate could have been 425 mm — an 11 mm saving that would have made the base **unbuildable** within the concrete tolerance.

**Step 7 — European alternative.**
If the same base is designed to EN 1992-4 with `d_f = 22 mm` (Table 6.1), the erector has **zero** adjustment. The base must then be either:
- (a) **adjustable bolts** in pockets/sleeves — EN 1090-2 D.2.20 §10.4.1, group ±6 mm, bolt tip ±10 mm — and the bolt is nudged into the 22 mm hole after the pour; or
- (b) a **26–30 mm** hole with the **annulus filled** by injection grout (≥ 40 N/mm²) or a spring pin per EN ISO 13337, otherwise the shear design must be reduced.

### 9.2 Worked Example 2 — A 44 mm grout pad destroys the anchor shear capacity
*(Reproduced from Hilti, "Method for Anchor Design in Grouted Stand-off Connections", v1.2, §4, EN 1992-4 route)* **[P]**

**Given.**
- Hilti HIT-RE 500 V4 adhesive anchor, **Grade 8.8 M24** threaded rod
- `d = 24 mm`, `A_s = 352,7 mm²`, `f_uk = 800 N/mm²`
- `γ_Ms,N = 1,5` (tension), `γ_Ms,V = 1,25` (shear) — ETA-20/0541
- `f_ck = 40 N/mm²` (concrete), `f_ck,grout = 50 N/mm²`
- **`t_grout = 44 mm`**, base plate 32 mm → `e₁ = 60 mm`
- `α_M = 2,0` (double curvature, full restraint)
- Bending moment present on the connection
- 4 anchors, spacing 400 mm

**Step 1 — Check EN 1992-4 §6.2.2.3(2) conditions for "shear without lever arm".**
```
1. Spacing:  10 d = 10 × 24 = 240 mm < 400 mm                       ✔
2. No bending moment or net tension                                  ✘ moment present
3. t_grout ≤ min(40 mm ; 0,5 d)  →  44 mm > 40 mm                    ✘ FAILS
4. Grout completely fills the void                                   ✔
5. f_grout ≥ f_concrete and ≥ 30 N/mm²  (50 ≥ 40, 50 ≥ 30)           ✔
```
→ **Eq. (7.36) is not available. Bolt bending per EN 1992-4 §6.2.2.3(3) / Eq. (7.37) is mandatory.**

**Step 2 — Basic steel resistances.**
```
N_Rk,s = A_s · f_uk = 352,7 × 800 = 282 kN
N_Rd,s = 282 / 1,50 = 188 kN                                  (> 140 kN demand ✔)
k₆ = 0,5                                    EN 1992-4 §7.2.2.3.1(1)
V⁰_Rk,s = k₆ · A_s · f_uk = 0,5 × 352,7 × 800 = 141 kN        Eq. (7.34)
```

**Step 3 — Anchor forces (M = 100 kNm, N = 20 kN, V = 80 kN, lever 0,4 m).**
```
Anchors 1,2:  N_Ed = −(1/2)(100/0,4) + 20/4 = −120 kN   (compression)
Anchors 3,4:  N_Ed = +(1/2)(100/0,4) + 20/4 = +130 kN   (tension)
All anchors:  V_Ed = 80/4 = 20 kN
```

**Step 4 — Bending resistance of the anchor.**
```
d_s = √(4 A_s/π) = √(4 × 352,7/π) = 21,2 mm     (stress-area diameter)
W_el = π d_s³/32 = π (21,2)³/32 = 934,3 mm³
M⁰_Rk,s = 1,2 · W_el · f_uk = 1,2 × 934,3 × 800 = 897 000 N·mm = 897 N·m     Eq. (7.39)
```

**Step 5 — Lever arm.**
```
a₃ = 0,5 d = 12 mm            (spalling depth)          Eq. (7.38)
e₁ = t_grout + t_bp/2 = 44 + 16 = 60 mm
ℓ_a = e₁ + a₃ = 60 + 12 = 72 mm                          Table 7.2
```

**Step 6 — Bending resistance reduced by axial force, then shear.**
```
M_Rk,s = M⁰_Rk,s (1 − |N_Ed| / N_Rd,s)                     Eq. (7.38)

Anchors 1,2:  M_Rk,s = 897 (1 − 120/188) = 325 N·m
Anchors 3,4:  M_Rk,s = 897 (1 − 130/188) = 277 N·m

V_Rk,s = α_M · M_Rk,s / ℓ_a                                Eq. (7.37)

Anchors 1,2:  V_Rk,s = 2,0 × 325 / 0,072 = 9,02 kN
Anchors 3,4:  V_Rk,s = 2,0 × 277 / 0,072 = 7,70 kN

V_Rd,s = V_Rk,s / γ_Ms,V

Anchors 1,2:  9,02 / 1,25 = 7,22 kN
Anchors 3,4:  7,70 / 1,25 = 6,16 kN
```

**Step 7 — Utilisation.**
```
Anchors 1,2:  β_V = 20 / 7,22 = 277 %      ✘ FAIL
Anchors 3,4:  β_V = 20 / 6,16 = 325 %      ✘ FAIL
```

**Interpretation for the fabricator.** The rods are M24 grade 8.8 — 141 kN characteristic shear each if bearing tight against the plate. Standing them off on 44 mm of grout drops the design shear to **6–7 kN each, a factor of ~20**. Nothing about the steel changed. Only the geometry did.

For comparison, if the same connection is analysed by Hilti's **SOFA method** (ACI-derived): `V_Rk,s,SOFA = 0,8 · k₇ · V⁰_Rk,s`, valid for grout pads to 100 mm with moment permitted. This is a proprietary method, **not** Eurocode-compliant.

**Related EN 1992-4 restriction [P]:** when bolt bending (Eq. 7.37) is used, the minimum edge distance is **the larger of 10·d and 60 mm**. For M24 → **240 mm**. *"For edge distances larger than this value, shear breakout resistance is not required to be calculated. Where closer edge distances are needed, EN 1992-4 does not offer a solution."*

### 9.3 Worked Example 3 — Realistic detail description: a 4 × M24 base for an HEB 300 column

**Concept.** Column HEB 300, factored axial 1 200 kN, shear 90 kN, moment 45 kNm. Foundation C30/37.

**Detail.**

| Item | Dimension | Basis |
|---|---|---|
| Base plate | **550 × 550 × 40 mm**, S355 | see edge-distance check below |
| Anchor rods | 4 × **M24** cast-in, class 8.8 (F1554 Gr.55 equiv.), at **380 × 380 mm** centres | 4-rod square pattern, DG1 §2.7 |
| Threaded length | required + **75 mm** | DG1 §2.7 (*"at least 3 in. greater than required"*) |
| **Anchor rod holes** | **Ø45 mm** (AISC ~1 in row = 46 mm) | DG1 Table 2.3 |
| **Plate washers** | **90 × 90 × 10 mm**, Ø26 mm hole | `d_w ≥ 2(45) − 24 = 66 mm`; `t ≥ 24/3 = 8 mm` — 90 × 10 adopted with margin |
| Grout bed | **30 mm** nominal, range **20–50 mm** | DG1 §2.10; ACI 117 §3.3.1 allows the pad 50 mm low |
| Grout | non-shrink cementitious, **f_ck ≥ 60 N/mm²** | ≥ 2 × concrete (DG1); ≥ 30 N/mm² and ≥ f_ck,concrete (EN 1992-4) |
| Levelling | steel shim packs **100 mm wide** at 4 edges + levelling nuts on 2 rods for plumb | DG1 §2.9.3 |
| Grout hole | none (550 < 600 mm) — head box on one side, outlet form opposite | DG1 §2.10 |
| Head box | starts **75 mm** from plate, 45° slope away; opposite form **75 mm** from plate, **25 mm** above plate underside; head height ≈ 550/5 = **110 mm** | ACI 351.1R §6.5.2 |
| Concrete surface | roughened to **≥ 3 mm** amplitude / CSP 6–8, saturated 24 h, SSD at pour | EN 1992-1-1 §6.2.5 & EN 1992-4 §6.2.2.3; Sika MS |
| Shear transfer | **by friction under the plate**, not by rod shear | see check below |

**Edge distance check.**
```
Clear hole-edge to plate-edge = (550 − 380)/2 − 45/2 = 85 − 22,5 = 62,5 mm  ≥ 51 mm  ✔
```
Had the plate been 500 mm: `(500−380)/2 − 22,5 = 37,5 mm < 51 mm` ✘. **The oversized hole, not the bolt force, sized the plate.**

**Shear transfer check (EN 1992-4).**
```
t_grout = 30 mm ;  0,5 d = 0,5 × 24 = 12 mm  →  30 > 12  ✘
```
Therefore rod shear would require the lever-arm check of WE2, which will not pass. **Take shear by friction instead:**
```
F_f,Rd = μ · N_Ed  with μ = 0,20 (base plate on grout, without tests)     Peikko Annex D / EN 1993-1-8 §6.2.2
       = 0,20 × 1 200 = 240 kN  ≥  90 kN  ✔
```
(AISC would be less conservative: DG1 §3.5.1 uses **μ = 0,55 for steel on grout, 0,70 for steel on concrete**, with `V_n = μ P_u ≤ 0,2 f'_c A_c`. **Report the divergence: 0,20 vs 0,55 is a factor of 2,75.**)

Since axial load can reverse under wind uplift, add a **shear lug** or hairpin bars for the load case with `N_Ed → 0`, per Peikko Annex D Figure 14.

**Setting-out spec to write on the drawing.**
> Anchor bolts to be set with a **two-template system**: lower steel template 8 mm plate wired to the reinforcement cage, upper steel template 10 mm plate fixed to the formwork, both drilled Ø25 mm (bolt + 1 mm). Bolts double-nutted against both templates. Set out from **Released-for-Construction drawing only**.
> **Tolerances:** group centre from column grid **±6 mm**; individual bolt at tip from group centre **±3 mm**; top of bolt elevation **−5 / +25 mm**; verticality **1:40**.
> Upper template to be removed not less than **24 h** after the pour. **All bolts to be surveyed after template removal** and the record submitted before erection. Threads to be protected and lubricated.

### 9.4 Worked Example 4 — Slot repair for two misplaced rods

**Situation.** On the base of WE3, rods 2 and 3 survey **19 mm** off position in the +x direction (group centre correct). Plate holes are Ø45 mm. Misplacement 19 mm > 13 mm ⇒ corrective work required (AISC SSEC threshold).

**Assess.** 19 mm is well under "a couple of inches", and only 2 of 4 rods. → **Slot the plate + spanning plate washer** (DG1 §2.11.1 standard repair).

**Design.**
```
Required slot length  L_slot ≥ d_hole + 2 × misplacement + working allowance
                             = 45 + 2(19) + 6 = 89 mm   →  adopt 90 mm × 45 mm slot
Slot orientation: parallel to the direction of misplacement (x)

Remaining edge distance, hole-edge to plate-edge, in x:
   (550 − 380)/2 − 90/2 = 85 − 45 = 40 mm     <  51 mm   ✘  marginal
```
→ Either widen the plate to **575 mm** (`85+12,5 − 45 = 52,5 mm ✔`), or accept 40 mm with an EOR check of the net section and washer bearing, since the rod will in fact sit near the slot centre.

```
Washer plate to cover the slot with the rod at either extreme:
   Length ≥ 2·L_slot − d_rod = 2(90) − 24 = 156 mm   →  adopt 160 mm
   Width  ≥ 2·d_hole − d_rod = 2(45)  − 24 = 66 mm   →  adopt 90 mm
   Thickness: ≥ d/3 = 8 mm; check bending across the 90 mm slot →  adopt 20 mm
   Washer hole: Ø26 mm (bolt + 2 mm) — or Ø25,6 mm (+1/16 in) if welded for shear
```

**Welding.** Rods are for tension + the shear is taken by friction ⇒ **do not weld the washer plate** (DG1 §2.6). If the EOR reassigns shear to the rods, the washer plate must be welded all round to the base plate and the washer hole reduced to **bolt + 1,6 mm**, with the rod bending checked over `ℓ_a = t_grout + t_washer/2`.

**Governance.** Submit the repair to the EOR (OSHA 1926.755(b)(1)); the controlling contractor issues written notice to the erector (1926.755(b)(2)); record the procedure, the as-repaired survey and any dye-penetrant/UT results.

---

## PART 10 — PRACTICAL FABRICATION AND DETAILING IMPLICATIONS

### 10.1 For the drawing office

1. **Never dimension an anchor-rod hole from a bolt-hole table.** Anchor-rod holes come from AISC DG1 Table 2.3 (or from EN 1992-4 Table 6.1 *plus a filling strategy*). Put the hole size on the drawing explicitly.
2. **Show the required plate washer on the same drawing as the hole**, with its size, thickness and hole. A big hole without its washer is a defect, not a detail.
3. **Size the base plate from the hole, not from the bearing calculation.** `B ≥ bolt centres + d_hole + 2 × 51 mm` is a hard constructability floor, and more if slotting may be needed.
4. **Add 75 mm of spare thread** to every cast-in rod.
5. **State on the drawing which EN 1090-2 regime applies** — "prepared for adjustment" (±6 mm group) or "not prepared for adjustment" (±3 mm group). They demand different holes and different foundation work.
6. **State the grout bed as a range** (e.g. 20–50 mm nominal 30 mm), never a single number, because ACI 117 §3.3.1 / NSCS 10.3.2 permit the concrete to be low.
7. **Decide the shear path explicitly on the drawing:** friction / shear lug / anchor rods in bending. If rods in bending, state `t_grout` as a *maximum*, and check EN 1992-4's `c ≥ max(10 d ; 60 mm)` edge distance.
8. **Grout holes and vents are a detailing responsibility.** Plates > 600 mm wide, plates with shear lugs, and any plate with underside stiffeners must be detailed with Ø50–75 mm grout holes and Ø6–13 mm vents at every stiffener intersection and high point.
9. **Put the tolerance table on the foundation drawing, in numbers.** DG1's explicit advice: *"It may be helpful to actually list the tolerance requirements instead of simply providing a reference."*
10. **Detail perpendicular stiffeners above the plate, not below**, wherever possible (ACI 351.1R §5.2) — stiffeners under the plate are void factories.

### 10.2 For the shop

1. **Anchor-rod holes above ~27 mm generally cannot be punched.** DG1 notes plate washers are *"usually custom fabricated by thermal cutting"*, and grout holes are *"typically thermally cut in the base plate."* Plan for plasma/oxy-fuel and account for kerf and dross removal on the bearing face.
2. **The ¾ in / 1-1/16 in exception is the only punching opportunity** (compression-only columns, plate < 32 mm). Confirm with the EOR before using it.
3. **Plate flatness at the bearing face matters** — a warped plate is a void generator. Setting/levelling plates ≤ 6 mm thick warp and are limited to 600 mm for that reason.
4. **Do not paint the underside of the base plate.** Metal in contact with cementitious grout must be *"cleaned of all paint, oil, grease, loose rust"*; epoxy requires sandblasting to bright metal.
5. **Ship galvanised rods and nuts pre-assembled from one supplier**, and specify it on the contract documents (it is not an ASTM requirement).
6. **Fabricate the template as a controlled item.** If the template is wrong, everything downstream is wrong and the error is discovered after the concrete has cured. Template hole centres should be checked against the base plate on the same marking-out.
7. **A steel template can be re-used as the levelling plate** — free value, but then its holes must be bolt + 1,6 mm, not the full oversize.

### 10.3 For the site

1. **Grout is on the critical path, not the snagging list.** Notify → grout → cure → load. CROSS 1190 and the Miami/Colorado cases are all sequence failures, not material failures.
2. **Place grout from ONE point only, moving along one side.** Never two points, never toward the centre from both ends. Grout sleeves and pockets *before* the plate.
3. **Head box: 50–100 mm clear, 45° slope away from the plate, head ≈ 1/5 of the travel distance.**
4. **Vents must be open and must be watched.** The grout is "in" when it exits the last vent, not when it exits the near side.
5. **Flag anchor rods.** They get run over.
6. **48-hour re-torque on galvanised rods** at 110 % of the verification torque.
7. **Survey and record every rod after the pour and before erection.** This is the last cheap moment to find a problem.

---

## PART 11 — KNOWN CONFLICTS BETWEEN SOURCES (to state explicitly in the report)

| # | Topic | Source A | Source B | Note |
|---|---|---|---|---|
| 1 | Anchor rod placement tolerance | AISC 303-05 §7.5.1: ±3 mm within group, ±6 mm group | AISC 303-16 / ACI 117-10 §2.3.4.2: ±6/±10/±13 mm by bolt diameter | AISC harmonised with ACI in 2016 **[S]**. Cite the edition. |
| 2 | Older concrete tolerance | ACI 117-90 §2.3: ±25 mm on embedded items | AISC ±3 mm | Historic, but still appears in old specifications. Named as irreconcilable by ASCC PS#14. |
| 3 | EN 1992-4 grout cap | Hilti: `min(40 mm ; 0,5 d)` | Peikko: `0,5 d_b` → 8/10/12/15/19 mm for M16–M39 | Peikko is the literal reading of "mortar layer ≤ d/2". Both reject a 25–50 mm bed. |
| 4 | Friction coefficient, plate on grout | AISC DG1 §3.5.1: **μ = 0,55** (steel on grout), 0,70 (steel on concrete) | Peikko / EN 1993-1-8 §6.2.2: **μ = 0,20** without tests | Factor 2,75. Use 0,20 for Eurocode work. |
| 5 | Torque coefficient | AISC DG1: `T_v = 0,12 d_b T_m` (Till 1994) | *"Other researchers have suggested a value of 0,20 for less-well-lubricated rods."* | DG1 states both. |
| 6 | Beveled washer trigger | DG1 body §A2.1: slope > **1:20** | DG1 Table A2 note c: slope > **1:40** | Internal inconsistency in DG1. Use 1:40 (conservative). |
| 7 | Base plate hole clearance philosophy | AISC: **+14 to +32 mm**, big plate washer, accept slip | EN 1992-4: **+2 to +3 mm**, or fill the annulus | Different, coherent systems. Do not mix. |
| 8 | Anchor rod hole for 2½ in rod | DG1 PDF copy: 3-¼ in | ASCC PS#14 quoting AISC Manual: **3-¾ in** | Almost certainly a typographic artefact in the DG1 file. Use 3-¾ in. |
| 9 | DG1 M⁰_Rk,s factor | Hilti ungrouted paper text: *"generally taken as 1,5 W_el f_uk"* | Hilti's own worked example and EN 1992-4 Eq. (7.39): **1,2 W_el f_uk** | Use **1,2**; the "1,5" is a text error. |
| 10 | Proof-load test frequency | CBC §1910A.5: **100 %** for structural applications | Hilti: *"No standardized percentage exists; typical ranges are 5–20 %"* | Jurisdiction-dependent. State the basis chosen. |

---

## PART 12 — NOTE ON ISRAELI STANDARDS (ת"י)

Coverage here is thin and I could not obtain clause-level content; treat as a research pointer rather than a citable result.

- **ת"י 1225 (חוקת מבני פלדה)** is the Israeli steel structures code, issued in parts. **חלק 1.1** (June 2023 edition), *"כללים כלליים לתכנון וביצוע"*, covers general rules for **design and execution**, including tolerances (סבולות) on manufacturing and assembly dimensions. It is *"מבוסס ברובו על התקן האירופאי (Eurocode 3)"* with adaptations for Israeli conditions. A three-year overlap applies: from the publication of Part 1.1 in the official register, both **ת"י 1225 חלק 1.1 (2023)** and **ת"י 1225 חלק 1 (December 1998)** are valid for 3 years. **[S]**
- **ת"י 1225 חלק 2** — *"הגנה על מבני פלדה מפני קורוזיה"*; **חלק 2.5** — *"חוקת מבני פלדה: הגנה מפני שיתוך — מערכות צבע"* (Corrosion protection — protective paint systems), published 31/03/2017, gazetted 11/05/2017, superseding the 2002 version. **[P — SII standard page]**
- **ת"י 1225 חלק 3** — *"תכן חיבורים"* (Design of connections). **[S]**
- I found **no evidence** that EN 1090-2 has been adopted in Israel as a numbered ת"י. **Recommendation for the report: verify directly with מכון התקנים הישראלי (sii.org.il)** whether ת"י 1225 חלק 1.1 (2023) incorporates EN 1090-2 execution classes (EXC1–EXC4) and its tolerance annex by reference, and whether the Israeli National Annex modifies any values. If it does not, specify **EN 1090-2:2018 Annex B** and **EN 1992-4:2018** explicitly in project specifications, together with the execution class.
- Also worth noting: **ת"י 1226** covers steel building drawings, and Israel adopts EN 206 for concrete, so EN 13670 / NSCS-type foundation tolerances are the natural companion set.

---

## PART 13 — SOURCES ACTUALLY USED

**Read in full (primary or full technical documents)**

1. AISC, *Steel Design Guide 1 — Base Plate and Anchor Rod Design*, 2nd Ed. (Fisher & Kloiber) — §§2.6–2.11, Table 2.3, Table 2.4, §3.4, §3.5, Appendix A (Tables A1, A2), in-service inspection. http://www.abarsazeha.com/images/ScinteficResources/DesignGuide/AISC%20Design%20Guide%2001%20-%20Base%20Plate%20And%20Anchor%20Rod%20Design%202nd%20Ed.pdf
2. ACI 117-10 (Reapproved 2015), *Specification for Tolerances for Concrete Construction and Materials and Commentary* — §2.3, §3.3, §3.4, §5.2. https://www.pa.gov/content/dam/copapwp-pagov/en/dli/documents/ucc/documents/2018-icc-code-review-public-comments/szoke-117-10.pdf
3. ASCC Position Statement #14, *Anchor Bolt Tolerances* (as published in *Concrete International*). https://ascconline.org/Portals/ASCC/Files/Position%20Statements/PS-14_AnchorBoltTolerances_09-11_Web_SC.pdf
4. ACI 351.1R-99, *Grouting Between Foundations and Bases for Support of Equipment and Machinery* — §§5.2, 5.4, 5.5, 6.2–6.5, 7.1–7.5, 8.1–8.2, 9.1–9.4. http://civilwares.free.fr/ACI/MCP04/3511r_99.pdf
5. *National Structural Concrete Specification for Building Construction*, Edition 4 (CONSTRUCT/UK) — §10.1–10.5, especially §10.4 (explicitly coordinated with BS EN 1090-2:2008 Cl. D.2.20). https://www.engineeringsurveyor.com/software/NSCS-Edition-4.pdf
6. Peikko Group, *HPM Rebar Anchor Bolt Technical Manual*, 06/2024 — Tables 2, 3, 4, 5, 8, 9, 29; Annex D; Annex E. https://media.peikko.com/file/dl/i/71TI4A/hE2HG3SxBHCXXrs54YYxuw/HPM_Industrial_PEIKKO_GROUP_01_Technical_Manual_Web.pdf
7. Peikko Group, *HPKM Column Shoe Technical Manual*, 05/2014. https://media.peikko.com/file/dl/i/lhtR6A/2P2ddfwoa6hpzpPJFRoYRg/HPKMPeikkoGroup002TMAWeb.pdf
8. R-Steel, *RPP Base Bolt Technical Manual*, 26.08.2025 — §§3, 4, 6 (Tables 13, 14). https://www.rsteel.eu/wp-content/uploads/RPP-Technical-manual_EN_26.08.2025.pdf
9. Hilti (McBride, Rocha, Figoli), *Hilti Method for Anchor Design in Grouted Stand-off Connections*, v1.2, July 2023 — §§1, 2, 4 (design example). https://files-ask.hilti.com/original/cf/cfecdz4zhb.pdf
10. Hilti, *Hilti Method for Anchor Design in Ungrouted Stand-off Connections* — §§2.1.2–2.2.2. https://files-ask.hilti.com/original/pu/pu5ebwyxp4.pdf
11. Sika Canada, *Method Statement — Cementitious Grouting of Machine Bases and Base Plates*, Nov 2017. https://can.sika.com/dms/getdocument.get/06365f03-0d83-31a1-ad53-a8cd85a630ab/Method%20Statement_Cementitious%20grouting.pdf
12. BSI, *BS EN 1090-2:2018* — preview/sample pages (Annex B structure, Table B.1 format, clause 10.8 grouting options, clause 7.4.1 hole clearances option list). https://www.normsplash.com/Samples/BSI/163653368/BS-EN-1090-2-2018-en-2.pdf

**Fetched web pages (technical, read as rendered)**

13. CROSS Safety Report 1190, *Failure to grout steel frame bases*. https://www.cross-safety.org/us/safety-information/cross-safety-report/failure-grout-steel-frame-bases-1190
14. STRUCTURE magazine, *Recommendations for Structural Grouting* (Mullins & Parker). https://www.structuremag.org/article/recommendations-for-structural-grouting/
15. IDEA StatiCa, *Code-check of anchors (EN)* — EN 1992-4 §7.2.2.3.2 formulas. https://www.ideastatica.com/support-center/support-center-knowledge-base/check-of-anchors-according-to-eurocode
16. Hilti, *Special Inspections Guidelines for Post-Installed Anchors*. https://www.hilti.com/engineering/articles/special-inspections-guidelines-for-post-installed-anchors
17. Hilti, *Influence of Borehole Cleaning*. https://www.hilti.vn/content/hilti/A2/VN/en/engineering/news-and-references/engineering-news/influence-of-borehole-cleaning.html
18. UpCodes, *Tests for Post-Installed Anchors in Concrete* — California Building Code 2022 §1910A.5. https://up.codes/s/tests-for-post-installed-anchors-in-concrete
19. UpCodes / OSHA, *29 CFR 1926.755 Column anchorage*. https://www.osha.gov/laws-regs/regulations/standardnumber/1926/1926.755 · https://up.codes/s/column-anchorage
20. Portland Bolt, *Fixing Misaligned Anchor Bolts*. https://www.portlandbolt.com/technical/faqs/fixing-misaligned-anchor-bolts/
21. Sika UK, *Avoiding a void with grout*. https://gbr.sika.com/en/construction/concrete-repair/document-downloads/knowledge-articles/Blogs/avoiding-a-void-with-grout.html
22. Steel Calculator, *Bolt Hole Size Chart — AISC J3.3* and *UK Bolt Hole Sizes — EN 1993-1-8*. https://steelcalculator.app/reference/bolt-holes/ · https://steelcalculator.app/reference/uk-bolt-hole-sizes/
23. מכון התקנים הישראלי — ת"י 1225 חלק 2.5 standard page. https://www.sii.org.il/he/דפי-לובי/כללי/תקינה/דף-תקן/?id=683499ed-8dba-4e89-b7ca-ee69b3b7d505_EH
24. שלמי עתון, *תקן ישראלי 1225 חלקים 1–3 — מבני פלדה (חוקת הפלדה)*. https://shlomi-atun.co.il/תקן-ישראלי-1225-חלקים-1-3-מבני-פלדה-חוקת-ה/

**Search-derived only (marked [S] above — verify before contractual use)**

25. ASCC, *A Tolerance Compatibility Success* (AISC 303-16 harmonisation with ACI 117). https://www.forconstructionpros.com/concrete/equipment-products/article/12322018/ascc-american-society-of-concrete-contractors-a-tolerance-compatibility-success *(403 on fetch; content via search summary)*
26. SteelConstruction.info, *Accuracy of steel fabrication* and base-plate/HD-bolt pages (SCI/BCSA). https://www.steelconstruction.info/Accuracy_of_steel_fabrication *(403 on fetch)*
27. SCI Guidance Note 5.03 *Geometrical tolerances* and 5.08 *Hole sizes and positions for preloaded bolts*. https://www.steelconstruction.info/images/d/d6/GN_5-03.pdf · https://www.steelconstruction.info/images/1/16/GN_5-08.pdf *(403 on fetch)*
28. AISC, *Installation of Anchor Rods, Foundation Bolts, and Other Embedded Items* (GC Toolbox Talk 10, 2025). https://www.aisc.org/media/2yaj55d0/gc_toolboxtalk10_2025.pdf *(403 on fetch)*
29. ANSI/AISC 303-16, *Code of Standard Practice for Steel Buildings and Bridges*. https://www.aisc.org/globalassets/aisc/publications/standards/a303-16w_0517.pdf *(403 on fetch — free download available directly from AISC; **obtain this and confirm §7.5.1 verbatim before publishing the tolerance table**)*
30. Dayton Superior, *Guide to Bearing Grouts*. https://www.daytonsuperior.com/docs/default-source/application-guides/guide-to-grouts-1-18-16.pdf *(403 on fetch)*
31. ACI 318-19 §§26.7.1(l), 26.7.2 (adhesive anchor installer certification, MPII) — via NYC DOB bulletin and Simpson Strong-Tie SE Blog. https://www.nyc.gov/assets/buildings/pdf/anchor_adhesive_certification_req.pdf

**Outstanding items to obtain before the report is finalised**
- **ANSI/AISC 303-16 (or -22) §7.5** verbatim — free from AISC; needed to settle conflict #1
- **EN 1090-2:2018 Annex B**, the table corresponding to former D.2.20 — needed to confirm the 2018 table number and whether values changed from 2008
- **EN 1992-4:2018 §6.2.2.3 and Table 6.1** verbatim — needed to settle conflict #3 and confirm the d_f table beyond M39
- **ת"י 1225 חלק 1.1 (2023)** — to establish the Israeli execution/tolerance regime and any National Annex deviations