---
name: per-project-not-universal
description: "Amir's rule: findings from one model are project-specific — generalize the METHOD, never the numbers"
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 5085ae2a-8410-44d0-8793-a97d55bd8dfb
  modified: 2026-07-29T08:15:15.784Z
---

When analyzing one of Amir's models, findings are **specific to that project** — do NOT turn them into universal rules about "how Amir models". He said explicitly (2026-07-29): "כל פרויקט הוא פרויקט בפני עצמו והדרישות שלו... מה שרשמת בתגליות 1+2 הם ספציפית עבור המודל הנ"ל".

**Why:** each job has its own geometry, standards and constraints. Carrying numbers from a previous project into a new one produces confidently wrong modeling. Models will get bigger and more complex over time.

**How to apply:** what generalizes is the **method**, not the values — for every new model: run `dumpmodel` + `app/model_analyze.py`, derive that model's own parameters (grid vs snap-to-geometry, dominant connection type, repeated families, spacings, levels), and state findings as "in this model…". Ask when unclear instead of inferring from a previous project. See [[acad-agent]]; the concrete example is `knowledge/HOW_AMIR_MODELS.md` (שיעור 2: 30.3mm snap = half-section, 164 T-junctions, 295mm rung pitch — all that model's own).
