---
name: partorigin-tells-the-route
description: "partOrigin says whether a part was made by a connection macro or by hand — the only field that exposes a standalone plate standing where a connection part belongs"
metadata:
  type: reference
---

⚡ נמדד 21/08/2026, מודל 3: שמונה ריפים אלכסוניים חופפים את ה-`HEB 500` שלהם ב-29.7×29.7×61.4
מ"ם, **ואף טרנספורמציה של הקונטור לא מזיזה את זה** (שמונה סיבובים/שיקופים ומעבר הפוך — כולם
נבנו ונמדדו). מול המקור הלוחות זהים ב-`org`, ב-bbox, ב-L/W/H, ב-`cutArea` (0.003 מ"ם²) ובמשקל
(1 גרם). ההבדל היחיד:

```
source  CE9   partOrigin = kRipOrigin        pos='1033'  count=1/8
rebuild 1941  partOrigin = kUndefinedOrigin  pos=''      count=1/0
```

**Why:** ריף שנוצר ע"י מאקרו ה-RIP שייך לחיבור — הוא והמאחז שלו הם **מכלול אחד**, ובודק
ההתנגשויות מתייחס אליהם כך. לוח עומד בפני עצמו באותו מקום ידווח כהתנגשות. ⇒ הכלל "היחידה היא
החיבור, לא האובייקט" מגיע כאן **כמדידה ולא כהמלצה**.

**How to apply:** ‏`dumpparts` מחזיר `partOrigin` לכל חלק. ‏`kUndefinedOrigin` על חלק שבמקור הוא
‏`kRip…`/חיבור = **לוח בודד במקום חלק של חיבור**, וזה פגם שרואים רק בשדה הזה — לא ב-bbox, לא
במשקל ולא בקונטור. בשחזור 1:1 להשוות `partOrigin` כשדה מפקד, לא רק ספירות.

קשור: [[counter-may-read-a-flag]] · [[copy-from-the-model]] · [[buildable-or-not-modelled]] ·
[[psn-macro-assemblies]]
