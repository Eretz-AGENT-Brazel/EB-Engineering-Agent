---
name: part-d-complete
description: "Part D (7 chapters) closed in one overnight run 11/08/2026; the model, the plugin version, and the four decisions waiting for Amir"
metadata: 
  node_type: memory
  type: project
  originSessionId: 2c4c6707-d5b7-4cf4-8b74-65f8d9758c43
  modified: 2026-08-11T16:46:56.279Z
---

חלק D של מדריך ProStructures נסגר **במלואו בלילה של 11/08/2026** — שבעה פרקים, חמשת השלבים לכל
אחד, הכל נדחף. הסך: **53 מתוך 79 פרקים**. נשאר **חלק C (22 פרקים)** ו-**B.30–B.33**.

**המודל:** `projects/SANDBOX/D-miscellaneous.dwg` — 98 ישויות, `vfy_fit bolts=28 OK=28
BOLT-NO-HOLE=0`. רצועות במקצב 60,000 מ-**x=360,000** (‏D.05 · D.02 · D.03 · D.04 · D.06).
**התוסף עלה v178 → v183** (אופים חדשים: `cog`, `unwind`, `usershape`, `block`, ו-`collision` מורחב).

⚠️ **כלל תפעולי שנלמד בלילה הזה:** **לעולם לא לפתוח שרטוט חדש כדי להתחיל חלק** — שרטוט חדש מעלה
דיאלוג "Measurement Unit" מודאלי שאינו ניתן לסגירה מקוד. **להעתיק DWG קיים ולפתוח אותו בנתיב מלא.**

**ארבע ההכרעות שממתינות לאמיר** (מפורטות ב-`knowledge/learning/manual/D/`):
1. ⭐⭐⭐ **פלט CNC נגיש מקוד** — `PsNCData` הוא כל דיאלוג ה-NC. **דורש מספור פוזיציות תחילה**,
   ו-`posauto` מוכן אבל **הסכימה היא של אמיר**. זו ההזדמנות הגדולה של החלק. ראה [[two-phase-program]].
2. ⛔ **חור ⌀19 ל-M16 (הפרקטיקה הרשומה) נותן אפס ברגים מ-DIN7990**, וחור ⌀23 נותן **M22 ולא M20** —
   `boltparts` בוחר את הבורג **מהחור**. החלטת ייצור. ראה [[bolts-follow-holes]].
3. **ספריית בלוקים של החברה** — D.2 הוכח כמסלול "לבנות פעם אחת ולהציב בכל פרויקט".
4. **קטלוג חתכים של EB** — 34 הבונים הפרמטריים עובדים; כתיבה ל-`Data\UserShapes` היא שינוי בהתקנה.

⚠️ **שני שומרים תוקנו בגלל ממצאי הלילה:** `app/vfy_joined.py` קרא לפלטה קדוחה-ולא-מוברגת "bolted";
ו-`collision` ברירת המחדל `minvol=1` רגישה מדי — **הכלל החדש הוא `minvol=100` בכל רצועה עם ברגים**.
