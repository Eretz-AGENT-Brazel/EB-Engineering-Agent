---
name: two-apis-com-wrapper
description: "ProStructures has TWO APIs — when a .NET Ps* class won't bind to an existing entity, the PSCOMWRAPPERLib COM object will"
metadata: 
  node_type: memory
  type: project
  originSessionId: a4c7fddc-7ed7-48b9-a359-e4b37dc8fdbe
  modified: 2026-08-07T12:43:02.436Z
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

קשור: [[prosteel-api-surface]] · [[positioning-solved]] · [[no-silent-skipping]]
