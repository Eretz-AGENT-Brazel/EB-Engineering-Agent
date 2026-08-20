---
name: bulk-ops-and-batch
description: "⚡ On a big model the file protocol IS the cost (~0.28s/op) — use dumpparts/batch/erase/wipe/xclone, born 20/08/2026 on the bridge model"
metadata: 
  node_type: memory
  type: project
  originSessionId: e1e7e219-55ad-4b68-8b41-c82b511ba272
  modified: 2026-08-20T15:32:38.316Z
---

⚡ **מודל גדול ⇒ הצוואר הוא הפרוטוקול, לא התוכנה.** נמדד ב-20/08/2026 על מודל הגשר
(21,737 ישויות): כל אופ עולה **~0.28 שנ' של הלוך-חזור** בלי קשר למה שהוא עושה
(`ping` 0.336 · `props` 0.284 · `mods` 0.251). חמישה אופים נולדו מזה, וכולם ב-v188–v193:

- **`dumpparts`** — props + מודיפיקציות של **כל** הישויות בקריאה אחת: **5.3 שנ'** במקום **2.1 שעות**.
- **`batch file=…`** — קובץ של אופים דרך אותו dispatcher: **0.002 שנ' לאופ (×140)**, ועם
  **שורת תוצאה לכל פריט**. ‏11,657 חורים ב-11 שנ'; 7,807 לוחות ב-111 שנ'.
- **`erase handles=` / `wipe cls=`** — מחיקה בטרנזקציה אחת (6,666 לוחות ב-0.4 שנ').
- **`xclone from=<שרטוט פתוח> handles=`** — ‏`WblockCloneObjects` בין מסמכים: הדרך הישרה
  למחלקה שאין לה יוצר, או ל**קטלוג חתכים שלא מותקן במכונה הזאת**.

⭐ **למה `batch` לא שבר כלום:** ה-switch יצא מה-CommandMethod ל-`Exec(op,kv)` ו-`Result()`
נשפך לבאפר ⇒ **כל 200+ האופים הקיימים עובדים בבאטץ' בלי שאחד מהם נגע**. השומרים נשארו על
ה**קריאה** (מסמך שגוי, טריות, נעילה) — נכון, כי כל הפריטים נוחתים באותו מסמך.

**How to apply:** לפני שמתחילים סגמנט של אלפי אופים — לשאול "האם זה יכול לצאת בבאטץ' אחד",
ולפני קריאה פר-חלק — "האם יש דאמפ שעונה על הכל בקריאה אחת". הפירוט המלא:
`api+knowledge-develop/knowledge/learning/findings/BULK-AND-BATCH.md` והסקיל `plugin-ops.md`.
קשור: [[bridge-model-rebuild]] · [[drill-not-polycut]] · [[read-back-what-you-gave]]
