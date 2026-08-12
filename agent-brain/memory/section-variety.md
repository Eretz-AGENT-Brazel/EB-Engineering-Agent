---
name: section-variety
description: "Amir's 07/08/2026 working habit — reach for the whole 357-catalogue section library, not just HEB/IPE; and section keys are opaque strings"
metadata: 
  node_type: memory
  type: feedback
  originSessionId: a4c7fddc-7ed7-48b9-a359-e4b37dc8fdbe
  modified: 2026-08-07T12:43:30.644Z
---

⚡ אמיר, 07/08/2026: *"תגוון קצת עם הפרופילים — אל תתקע רק על HEB או IPE. תנסה לגוון קצת ולצאת
מהקופסא להכיר וללמוד עוד הרבה סוגים של חתכי פרופילים."*
והבהרה שלו מיד אחר כך: *"כל החתכים נמצאים בתוכנה — רק ביקשתי שתנסה להיחשף אליהם יותר ולנסות
לעבוד עם כמה שיותר סוגים."*

**Why:** זו **הנחיית הרגל עבודה**, לא בקשת מחקר ולא גילוי — הספרייה תמיד הייתה שם. עד לאותה נקודה
כל מודל שבניתי השתמש ב-5 סוגי חתכים בלבד — מתוך 357 קטלוגים סטנדרטיים, ‏ו**זה היה רק חצי מהתמונה** (ראה התיקון בסוף). פס B.6 שנבנה אחרי ההערה:
52 מוטות, **14 חתכים שונים**, 343 מטר.

**How to apply:** בכל פרק/מודל — לבחור את החתך שהאלמנט באמת רוצה. זוויתנים בודדים וכפולים
(`DIN WINKEL GLEICH/HD/VD/4T/DIA`), תעלות (`DIN_U`/`UPE`/`UAP`), חלולים חם וקר (`RQ`/`RR`/`RO`),
חצאי קורות (`DIN_HALBE IPE`→`HIPE300`), T (`T80`), עמודים כבדים (`Peiner_HD`/`HL`), מגולגל קר
(`Z160`, `C150x75x6,5`), פסים, מוטות, מוט מוברג, ואפילו עץ. המפה המלאה:
`EB PROSTEEL AGENT\api+knowledge-develop\knowledge\learning\findings\SECTION-CATALOGUES.md`.

⚠️ **מפתח חתך הוא מחרוזת אטומה — לחפש, לא להרכיב.** `HD260x68,15` עם **פסיק** גרמני מול
`RO 219.1x6.3` עם **נקודה** באותה ספרייה · `DIN HALBE IPE` לא קיים אבל `DIN_HALBE IPE` כן ·
ומה שמזינים אינו מה שנשמר (`DIN_QUADRATROHR`→`DIN.DIN_QUADROHR`, מפתחות עוברים לאותיות גדולות),
ולכן **מודל שנקרא ב-dumpfull לא ניתן להזנה חוזרת ישירה**.

קשור: [[no-silent-skipping]] · [[two-apis-com-wrapper]] · [[per-project-not-universal]]


---

## ⚠️ תוקן 10/08/2026 — 357 הוא חצי התמונה

ביקורת B.8 מצאה ש-`PsCreateShape` חושף **ארבעה בוררים** וה-op קרא רק לראשון. מעבר
ל-357 הסטנדרטיים יושבים על הדיסק:

| מאגר | `kind=` | קטלוגים | **חתכים** |
|---|---|---:|---:|
| `UserShapes` | `special` | 68 | **1 528** |
| `RoofWall` | `roofwall` | 20 | **270** |
| `CombiShapes` | `combi` | 15 | **88** |
| `WeldShapes` | `weld` — רק דרך `bendshape` | 3 | 4 |
| | | **106** | **1 890** |

⭐ **והמשפחות שמשנות ל-EB יושבות דווקא שם:** מרזבים קרים Z/C/Σ
(`SCHRAG_z-pfetten`, `SCHRAG_c-riegel`, `Sadef_zed/cee/sigma`, `sbe_c/z/zeta`, `ayrsh_zeta`,
`ayrshire_eb`) — **מה ש-B.22 היה צריך ולא היה לו**; פסי מנוף (`Kranschienen_Form_A`,
`krupp_z*`); מסילות Halfen (`halfen_hl/hm/np/p`); פחים מכופפים (`Kantteile`);
דק פלדה (`Steel Deck`); מדרגות (`stair`).

⚠️ **התיקייה היא ה-`catalog=`, שם קובץ ה-`.psp` הוא ה-`name=`.** חלק מהקטלוגים הם
טבלאות `.dbf` ושם השם הוא שדה **`KEY`**. ולפנות לחתך לפי **שם הקובץ** — שדה השם
הפנימי משקר (`R273x28-H440.psp` קורא חזרה `R244.5x22.2-H420`).

⇒ ההרגל עצמו לא השתנה — **לבחור את החתך שהאלמנט באמת רוצה.** רק שמגוון
הבחירה גדול פי שישה ממה שרשום כאן קודם.
