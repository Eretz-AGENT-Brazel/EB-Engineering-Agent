---
name: counter-may-read-a-flag
description: "A census counter can be reading a stored creation flag rather than measuring geometry — read what a counter counts before trusting it as a gate"
metadata:
  type: feedback
---

⚡⚡ נמדד 21/08/2026 על מודל 3. ‏`dumppoly nonrect=` נראה כמו בדיקת צורה, ובפועל הוא קורא
‏`pl.RectangleMode` — **דגל יצירה שמור**, והמאפיין **read-only** (‏`csc: CS0200`). לוח שנולד מלבני
יקרא `nonrect=0` לנצח, גם כשהקונטור שלו מדויק ל-0.0000 מ"ם. ⇒ **השער שהיה רשום למודל 3
("`nonrect=132` בשני השרטוטים") לא היה קשה — הוא היה בלתי אפשרי.**

**Why:** מונה שמדווח על דגל בזמן שהקורא מבין אותו כמדידת גיאומטריה **יפסול מודל נכון ויאשר מודל
שגוי**. זו אותה משפחה של [[read-back-what-you-gave]] ושל [[measure-dont-infer]], רק מהצד של
המכשיר ולא מהצד של האופ.

**How to apply:** לפני שמסתמכים על מונה כשער — **לקרוא את המימוש שלו** (מאיזה מאפיין הוא נגזר).
אם הוא נגזר מדגל: לחפש שער שנגזר מהגיאומטריה. במודל 3 השער שעבד הוא **השוואת הקונטור במרחב
העולם**: כל שרטוט נותן קונטור במסגרת הלוח (`dumppoly`) ואת המסגרת עצמה (`dumpparts` →
‏`org`/`X=`/`Y=`), ומהצירוף מקבלים קואורדינטות חד-משמעיות — נמדד **0.0000 מ"ם על 132 לוחות**.
🐛 ואותו יום: ‏`eb_api.save()` בדק נתיב `projects/sandbox/` **מקודד קשה** ⇒ דיווח "לא נשמר" על
שמירה שעבדה, ולא לקח גיבוי לאף מודל פרויקט. **בודק שנבדק רק במסלול המצליח שלו אינו בדוק.**

קשור: [[measure-dont-infer]] · [[read-back-what-you-gave]] · [[no-silent-skipping]] ·
[[model-you-cannot-see]] · [[partorigin-tells-the-route]]
