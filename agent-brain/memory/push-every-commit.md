---
name: push-every-commit
description: "אמיר עוקב אחרי העבודה דרך GitHub — כל קומיט נדחף מיד, לא משאירים את main מקדים את origin"
metadata: 
  node_type: memory
  type: feedback
  originSessionId: f7644a80-6f23-41e6-b129-0eb429982261
  modified: 2026-08-12T07:07:30.661Z
---

אמיר (12/08/2026, סבב סידור התיקייה): *"תעשה פוש — אני מתעדכן איתך בכל פעם דרך הגיטהאב."*

**Why:** הערוץ שבו אמיר רואה את ההתקדמות הוא GitHub (`Eretz-AGENT-Brazel/EB-Engineering-Agent`),
לא המחשב. קומיט שלא נדחף הוא עבודה שהוא לא רואה — כמו דיווח שלא נמסר.

**How to apply:** אחרי כל קומיט בריפו EB PROSTEEL AGENT — `git push` מיד, בלי לשאול.
זה משלים את שלב 5 של נוהל הפרקים ("קומיט **ופוש**") וחל גם על קומיטים של סידור/תחזוקה.
כרגיל, לפני כל קומיט: `python qc/consistency.py` חייב CLEAN.

קשור: [[two-phase-program]] · [[no-silent-skipping]]
