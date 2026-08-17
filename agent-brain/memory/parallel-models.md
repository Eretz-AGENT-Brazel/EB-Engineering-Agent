---
name: parallel-models
description: ⚡ עובדים רק על המודל שנכנסו אליו במפורש; אמיר עובד במקביל. ורק מופע AutoCAD אחד נגיש ל-COM
metadata: 
  node_type: memory
  type: project
  originSessionId: 89299c18-2966-4e7b-8794-b67bf8b31808
  modified: 2026-08-17T10:45:00.433Z
---

⚡ **נקבע ע"י אמיר, 17/08/2026:** הוא עובד על פרויקט אחד בזמן שהסוכן עובד על אחר, ולסוכן עצמו
כמה מודלים עם משימה נפרדת לכל אחד.

**מה זה מחייב אותי, בפועל:**
1. **נכנסים למודל במפורש לפני האופ הראשון** — `eb_api.use(<dwg>, task=...)` או
   `with eb_api.model(<dwg>):`. אין "המודל שהיה פתוח". משמרת מאתמול **מסרבת** עד `confirm`.
2. **שרטוט שאינו רשום כשלי — לא נוגעים בו, ואפילו לא מחליפים ממנו תצוגה.** אמיר מכריז
   בעלות ב-`python app/worksession.py claim "<dwg>"`.
3. ⛔⛔ **רק מופע AutoCAD אחד נגיש ל-COM בכל רגע** — המוקדם שעדיין חי. שתי רשומות ב-ROT
   נקשרות לאותו מופע (נמדד 17/08: HWND זהה; הריגת הראשון הפכה את השני לנגיש ב-12 שניות).
   ⇒ ריבוי המודלים חי **בתוך מופע אחד**; ‏AutoCAD שני של אמיר בטוח כי אי אפשר להגיע אליו.
   ⇒ אם ה-AutoCAD שלו נפתח **ראשון** — הכל מסרב `EB_ERR unreachable` עד שסוגרים אחד.
4. **‏`reachable=0` בזמן ש-acad.exe חי = דיאלוג חוסם** (Drawing Recovery אחרי כיבוי כפוי),
   לא AutoCAD מת.

הנוהל המלא והמדידות: `api+knowledge-develop/knowledge/learning/findings/PARALLEL-MODELS.md`.
הכלים: `app/worksession.py` · הפרוטוקול: תיבת דואר לכל מודל ב-`app/plugin/ch/<slot>/` מ-v185.
קשור: [[no-silent-skipping]] · [[two-apis-com-wrapper]] · [[dev-track-folder]]
