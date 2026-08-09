---
name: standards-agent-playbook
description: "Methodology playbook (from Amir's PDF) for building a steel-standards engineering agent — the ✓/⭑/⚠ marking system, repo structure, and honest-verification discipline"
metadata: 
  node_type: memory
  type: reference
  originSessionId: 5085ae2a-8410-44d0-8793-a97d55bd8dfb
---

Amir supplied a handoff/playbook PDF (`C:\Users\User\Downloads\דוח-הקמת-סוכן-הנדסי.pdf`, extracted text at scratchpad `report.txt`) written by another account's Claude Code agent that built a steel+concrete engineering knowledge repository. It codifies HOW to build a standards agent — adopt this method for [[standards-mastery]] / [[acad-agent]].

**Three inviolable rules:**
1. **Mark every value ✓/⭑/⚠** — ✓ verified from a legal source file the user supplied · ⭑ Eurocode/engineering-based (sound, but not necessarily the Israeli binding value) · ⚠ must be verified vs the binding text / National Annex. Never invent NA values or clause numbers — write ⚠ instead of guessing.
2. **Copyright** — Israeli standards are SII property (sold). Don't download pirated copies. Build the repo only from materials the user provides legally, or the free read-only official viewing (ibr.sii.org.il, Amendment 13). Eurocodes are CEN copyright, bought via a national body.
3. **Professional responsibility** — the agent is a checker-support/design-aid, never a replacement. Approval & liability are the registered engineer's. For any calc: recompute independently → cite exact clause → verify edition → say honestly if it's off.

**Method (5 phases):** (1) learn the user + set up one concentrated folder; (2) ingest the user's legal files (standards/ guides/ tools/); (3) deep-read each doc → structured summary + `versionSignals` (exact edition/date/SII-no. from the cover) → notes/; (4) verify editions vs sii.org.il → version-status.md; (5) build deep per-standard reference notes (knowledge/) with Israel↔EU cross + ✓/⭑/⚠, and a **National Annex verification list** of params NOT to assume from the EU default.

**Repo structure:** INDEX.md · 00-standards-map · version-status.md · national-annex-checklist.md · standards/ (legal PDFs) · guides/ · tools/ · notes/ · knowledge/ · calculations/. (EB PROSTEEL AGENT mirrors this under `standards/`.)

**Key cross-check flags to remember:** old ת"י 1225 E=205,000 & γs=1.08 vs EN 1993 E=210,000 & γM0=1.0 — never mix systems; two generations of load combinations (old Fd=γn·γf·Fk with γf=1.4/1.6 vs EN 1990 6.10/6.10a/6.10b); ת"י 1225 & 413 moved to 2023 editions (transition ended ~06/2026). Binding source is always SII — Eurocode values are the study skeleton, not the binding text.
