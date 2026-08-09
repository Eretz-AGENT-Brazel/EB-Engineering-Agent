---
name: standards-mastery
description: "Amir's directive to make the EB PROSTEEL AGENT a master of Israeli+European steel-design & loading standards and act as the company's engineering-standards consultant"
metadata: 
  node_type: memory
  type: project
  originSessionId: 5085ae2a-8410-44d0-8793-a97d55bd8dfb
  modified: 2026-07-29T11:54:24.258Z
---

On 2026-07-13 Amir gave a firm, standing directive: the [[acad-agent]] EB PROSTEEL AGENT must become a **master of the standards** and serve as **ארץ ברזל's engineering-standards consultant / design-aid** (יועץ לעמידה בתנאי תקן). Scope now: **European (EN/Eurocode) + Israeli (ת"י) standards for steel-structure design and characteristic loads (עומסים אופייניים)**. He said liability never falls on the agent — it is a planning/efficiency aid; a licensed engineer holds responsibility. This knowledge base + the modeling API together are the foundation for future development.

**Why:** He was frustrated that earlier standards work was only a cited index, not real mastery ("אל תפנה אלי כל עוד אתה לא מאסטר בידיעת התקנים"). He wants deep, embedded, usable standards knowledge.

**How to apply:**
- Build/deepen the knowledge base under `standards/` (modules per domain: loads EN 1990/1991 + ת"י 412/414/413; steel EN 1993 all parts + ת"י 1225; materials/bolts/welding; execution EN 1090; corrosion ISO 12944/1461) and wire it into the software as a **consultation engine** the agent actually uses (NLU standards-consult intent + a KB browser/search in the 📐 תקנים tab).
- Research from **official sources only** (SII, CEN, JRC); **cite** number+part+edition+clause; **never reproduce copyrighted standard text** — synthesize in own words, state facts/values with citation. Legal reading: Israeli official standards free read-only at ibr.sii.org.il; Eurocodes paid (see [[acad-agent]] STANDARDS section for verified access/editions).
- **Do NOT append handoff prompts** in this continuous Opus 4.8 work — see [[handoff-prompts]] correction. Work autonomously; don't ping Amir until genuinely done and ready to act as the engineering consultant.
- Delivery method chosen: a large multi-domain research Workflow (18 domains, research→adversarial-verify) → write verified Hebrew modules into `standards/kb/` → consult engine.

**⏸️ DEFERRED TO PHASE 2 (2026-07-29):** per [[two-phase-program]], all standards work (incl. D0 IBR reading, D1 propagation, NA checklist closure) is ON HOLD until Amir declares Phase 1 (modeling mastery) complete. The built infrastructure (standards/kb, consult engine, IBR access) waits as an asset.

**AMIR'S CORE INTENT (2026-07-14, angry escalation — top priority):** what he wanted ALL ALONG is for the agent to **go to ibr.sii.org.il itself with the built-in browser, READ the official Israeli standards (ת"י 1225 ח'1.1+1.8, 412, 413, 414), extract & verify the values legally (facts/values/section titles only — no protected prose), close the 37 NA-checklist items, upgrade ⭑/⚠→✓**, and produce a purchase list for whatever isn't on IBR (Amir buys → drops in standards/pdfs/). This is now **D0 (top of OPUS-UPGRADE-DIRECTIVES.md)** — do it before anything else. If IBR shows a license/registration dialog — stop and let Amir click it himself.

**IBR READING GATE (2026-07-14):** Opus browsed ibr.sii.org.il itself: search/list is open (no login), category "תקנים מחייבים בחקיקה" `#/standards/2`. Confirmed by direct viewing ✓: ת"י 1225 חלק 1.1 (2023) official title "תכן מבנים מפלדה: כללים כלליים וכללים עבור בניינים" + חלק 1.8 (2023) "תכן מבנים מפלדה: תכן מחברים" — both free "לצפייה", mandatory under תקנות התכנון והבנייה. **BUT reading the document content requires a one-time-per-session REGISTRATION form** (`#/registration/<id>`) asking ת.ז/name/email/city/org/phone/address — personal data → Amir must fill it himself (agent must not). After Amir registers in the SAME browser pane, the session unlocks and the agent can read. Report: standards/qc/IBR-READING-REPORT.md. Official title correction: "תכן מבנים מפלדה" (not "תכן מבני פלדה"). ת"י 412/413/414 not yet checked on IBR (blocked at gate).

**FABLE 5 QC AUDIT (2026-07-14):** Amir switched to Fable 5 for an independent QC pass (Fable = audit-only, Opus = sole coder). Verdict 68/100. Systemic failure found: adversarial-verification corrections were APPENDED to modules but NOT propagated into bodies (12+ wrong statements remain, incl. safety-critical S355 fu=490-vs-510, swapped snow ψ rows, "default EXC2" which 2018 removed, ת"י 412 part-swap, fake "ת"י 1055"); cross-file contradictions between modules and data.json; verification quality uneven (module 14's verifier false-confirmed fu=490); consult engine ~33% full-hit on real Hebrew questions (substring bugs רוח/רוחב, no Hebrew morphology); coverage gaps in core business (seismic-steel outdated 413:2009, no tank-codes module, no crane-loads module). Deliverables for Opus in `standards/qc/`: **FABLE5-QC-AUDIT.md** (findings), **OPUS-UPGRADE-DIRECTIVES.md** (D1-D20, P0=propagation pass first), **ACCEPTANCE-TESTS.md** (T/A-F/R/G question bank; mastery = ≥90% + 100% traps). Opus must run directives in order and pass the test bank before declaring mastery.
