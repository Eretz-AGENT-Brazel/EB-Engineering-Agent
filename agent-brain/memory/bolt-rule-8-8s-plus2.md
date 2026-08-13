---
name: bolt-rule-8-8s-plus2
description: "⚡ IRON RULE (Amir, 13/08/2026) — bolts default to 8.8S and every bolt hole is bolt ⌀ + 2 mm; supersedes the old +3 shop rule"
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 39bb8db1-bf71-4a6f-a201-eb0763a8cbaa
  modified: 2026-08-13T08:12:26.469Z
---

⚡ **כלל ברזל, אמיר 13/08/2026:** *"הברירת מחדל הם ברגי 8.8S. החורים לברגים הם קוטר הבורג + 2מ"מ."*
⇒ ‏M16→⌀18 · M20→⌀22 · M24→⌀26 · קריאה: `dia=<בורג> play=2` (‏`dia` הוא קוטר הבורג, החור יוצא `dia+play`).

**Why:** this **replaced** his own earlier +3 rule (M16→⌀19, M20→⌀23) on the day I measured the
style×hole matrix over 36 specimens: the `8.8S` table pairs on **+2 only and refuses ⌀19 and ⌀23**,
while the styles that accept +3 hand back the next bolt size up (⌀23→M22). His **dialog** can do
M20 in ⌀23; `PsCreateBolt` cannot. So +2 is both his decision and the only pairing the API builds.

**How to apply:** never choose a bolt style or a clearance per call — `DEFAULT_BOLT_STYLE = "8.8S"`
in `api+knowledge-develop/app/eb_api.py` is injected automatically, and drilling is always
`play=2`. **Models built before 13/08/2026 legitimately contain ⌀19/⌀23 — read them, never
"correct" them unasked.** Scope is bolt holes in steel; **cast-in anchors are a different case**
(a measured floor detail used ⌀28 for a ⌀20 anchor) — anything other than +2 is a question, not a
choice. Authority file: `knowledge/learning/findings/BOLT-STYLES-AND-HOLES.md`.
Related: [[bolts-follow-holes]] · [[no-silent-skipping]] · [[two-axes-authority]]
