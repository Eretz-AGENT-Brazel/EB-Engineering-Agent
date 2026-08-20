---
name: survey-files
description: "קבצי מדידה (surveyor DWGs) — always METRES not mm, and converting them to SOLID + clash-checking is the human modeller's job, not mine"
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 72a34aac-db7d-4945-9865-7ba7e7c82cb2
  modified: 2026-08-20T13:34:23.827Z
---

⚡ אמיר, 20/08/2026 (שיעור 8, ללא מידול):

1. **קובץ מדידה תמיד במטרים ולא במילימטרים — ×1000, "וזה עניין קריטי".** והקובץ עצמו מצהיר
   `INSUNITS=0` ("unitless") ⇒ שום INSERT/XREF/הדבקה לא יתריע. ⚠️ ו-×1000 לבד לא מספיק: המדידה
   יושבת על רשת מקומית (ב-Tara: 20000/10000) ⇒ **מזיזים לראשית מקומית קודם, ואז משנים קנה מידה.**
2. ⭐ **ההמרה ל-SOLID ובדיקת ההתנגשויות והאילוצים היא באחריות הממדל האנושי** — *"אנחנו העובדים
   בחברה"*. אני **בוסט**: קורא, מודד, מצליב ומסמן. **לא לוקח את הצעד הזה לעצמי.**
3. **ה-0.00 נקבע ע"י ארץ ברזל לכל פרויקט בנפרד** — לא מתוך הקובץ. הצהרה של שורה אחת לפני כל EXTRUDE.

**Why:** מבנה קיים לא נבנה לפי התכנית — בטארה תכנית הארכיון אמרה רצפה 3.50 והשטח מדד 3.546.
מי שמתכנן על התכנית מגיע לשטח שגוי ב-46 מ"מ. ובלי הצהרת יחידות בקובץ, טעות ×1000 עוברת בשקט.

**How to apply:** ‏⛔ לא להריץ `bootstrap()`/`enforce_metric()` על קובץ לקוח — הוא כותב `INSUNITS=4`
לתוך שרטוט שהוא במטרים. להתחבר עם `worksession.py assign` + `use()` + אופ רגיל, קריאות בלבד,
ולטבוע את הקובץ (size+mtime) לפני ואחרי. המבנה שתמיד יחזור: **הקווים משוטחים על מישור אחד
(‏Tara: `Z=106.490`) והנקודות נושאות את ה-Z האמיתי (`ELEV` ≡ Z) — התכנית אומרת איפה, הנקודות
אומרות כמה גבוה, והחיבור הוא הקוד.** ובכניסה למודל הפלדה: **מסובבים את המדידה עד שצירי המבנה
הופכים לצירי העולם** (בטארה 62.0993°) — משם כל המודל אורתוגונלי.
הידע המלא: הסקיל, `references/survey-files.md` · השיעור: `api+knowledge-develop/lessons/LESSON-08-SURVEY-FILES.md`.

קשור: [[buildable-or-not-modelled]] · [[consult-before-widening]] · [[per-project-not-universal]] ·
[[read-back-what-you-gave]] · [[one-error-three-masks]]
