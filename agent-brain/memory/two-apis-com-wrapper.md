---
name: two-apis-com-wrapper
description: "ProStructures has TWO APIs — when a .NET Ps* class won't bind to an existing entity, the PSCOMWRAPPERLib COM object will"
metadata: 
  node_type: memory
  type: project
  originSessionId: a4c7fddc-7ed7-48b9-a359-e4b37dc8fdbe
  modified: 2026-08-10T15:11:00.000Z
---

⚡ **יש שני ממשקי API, לא אחד.** מלבד `ProStructuresNet.dll` קיים **`PSCOMWRAPPERLib`** — אותם
אובייקטים כישויות COM אמיתיות של אוטוקאד (`Ks_ComGrid`, `Ks_ComWorkFrame`, `Ks_ComShape`,
`Ks_ComCreateGrid`…), נגישים מפייתון בלי קריאת plugin בכלל:

```python
o = doc.HandleToObject(handle)     # -> Ks_Grid / Ks_Shape / ...
o.Name, o.Length, o.LengthSteps    # קורא
o.LeftWedge = True; o.Update()     # וגם כותב
```

חלק ממחלקות ה-.NET הן **חוצצי יצירה שלא נקשרים לישות קיימת**. המקרה המוכח: `PsGrid`.
`PsObjectProperties.readFrom(id)` מחזיר 0 **ו-`getObjectId()` מחזיר בדיוק את ה-id שביקשתי** —
כלומר הוא כן נקשר — ובכל זאת `Name` ריק ו-`readProps` מחזיר `L=0`. גם `writeProps`, גם `init()`
לפני. שלושת המסלולים מתים. דרך COM אותו אובייקט מחזיר מיד שם, מידות, מספרי חלוקה ו**מערכי
המרווחים**.

**Why:** במשך שני פרקים הסקתי "לא נגיש" ממה שהיה רק "לא נגיש בממשק אחד". זה חסם את קריאת מסגרות
העבודה מ-B.8.7 עד שנפתר ב-B.6 (07/08/2026).

**How to apply:** לפני שאני מכריז שמאפיין לא נגיש — לנסות את עטיפת ה-COM. היא גם מגיעה לאפשרויות
שה-.NET לא חושף כלל (`LeftWedge`/`VerticalWedge` אין להם setter ב-`PsCreateGrid`). שמות שונים בין
השניים: `Wide`→`Width`, `LengthDiv`→`LengthDivision`, `RoofWide`→`RoofWidth`.

---

⚡ **אישוש שלישי, 09/08/2026 (B.22).** ה-op ‏`dumpmodel` מדווח `plates=0 bolts=0` עם **264 שורות
שגיאה**, כל אחת `Ks_Plate` או `Ks_Bolt` עם *"Object reference not set"*. דרך COM:

```python
app, doc = A._app_doc()
doc.HandleToObject(h).InsertPoint    # 152 מתוך 152 Ks_Bolt נקשרו, אפס כשלים
```

⚠️ **והלקח המתודי שנלווה לזה, שחשוב יותר מהמקרה עצמו:** הסקתי "אין ברגים בפס" מתוך אותו כלי עיוור,
וזו הייתה **טעות**. מה שחשף אותה היה **ביקורת**: הרצתי את הכלי על הפס של B.15, שבו ידוע בוודאות
שקיימים ברגים — וגם שם הוא החזיר אפס.

⇒ **כלי שמחזיר אפס חייב להוכיח שהוא מחזיר לא-אפס במקום כלשהו לפני שמאמינים לאפס שלו.**
זו הצורה המדויקת של [[no-silent-skipping]] בכיוון ההפוך: לא "דיווחתי בוצע בלי ארטיפקט", אלא
"דיווחתי *לא קיים* על סמך מכשיר שלא נבדק".


---

⚡ **עדכון 10/08/2026 — הבאג ב-`dumpmodel` תוקן בשורש. לא לעקוף אותו יותר.**

הרשומה למעלה מתארת כלי עיוור וממליצה לעקוף דרך COM. בביקורת חלק B (B.9)
נמצא השורש: `InsertPoint` נשלף בתוך `try` ונקרא **מחוצה לו** — `PsPoint` שחוזר עם מצביע
ניטיבי מת אינו null וזורק בקריאה. תוקן ב-v146/v147:

```
before:  shapes=349  plates=0    bolts=0    err=357
after :  shapes=349  plates=178  bolts=179  err=0
```

ושורות `PLATE`/`BOLT` נושאות עכשיו **תיבה תוחמת עולמית** — `InsertPoint` של פלטה קורא `0,0,0`
והפוליגון שלה מקומי, כך שבלעדיה לפלטה אין מיקום בכלל.

⚠️ **הלקח המתודי שנוסף:** תיעוד של כלי שבור בלי לתקן אותו משאיר את הבאג בחיים
עוד יום. העקיפה עבדה, ולכן אף אחד לא חזר לשורש — וכל מדידת מודל בין 09/08 ל-10/08
ראתה מודל בלי פלטות ובלי ברגים. ⭐ **כשמוצאת עקיפה — לפתוח גם פריט לתיקון השורש.**

---

⚡ **והסייג ההפוך, 10/08/2026 (B.13):** COM לא רק מציל את .NET — הוא גם יכול להיות
**מאחוריו**:

```
.NET  Bentley.ProStructures.EdgeLayout   ... kFold, kNotch     (7)
COM   PSCOMWRAPPERLib.KsEdgeLayout       ... kFold             (6)
```

אותו שם enum, **חברים שונים**. ⭐ **כששני ה-APIs חלוקים — ה-.NET הוא השלם.**

קשור: [[prosteel-api-surface]] · [[positioning-solved]] · [[no-silent-skipping]] ·
[[per-project-not-universal]]
