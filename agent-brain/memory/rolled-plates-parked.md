---
name: rolled-plates-parked
description: "Parked at Amir's request 07/08/2026 — make ROLLED plates in ProSteel, as its own separate development, to be done together"
metadata: 
  node_type: memory
  type: project
  originSessionId: a4c7fddc-7ed7-48b9-a359-e4b37dc8fdbe
  modified: 2026-08-07T13:54:30.287Z
---

אמיר, 07/08/2026: *"סיקרנת אותי לגמרי. פשוט שים את זה בצד כנקודה להמשך — **שננסה ביחד לייצר
פלטות מגולגלות בתוכנה** וניתן לזה מקום נפרד ופיתוח נוסף שלנו."*

**סטטוס: מוקפא במכוון. לא להתחיל בלי אמיר.**

מה שכבר ידוע ולא צריך לחקור מחדש (מקריאת מפת ה-API, **לא נמדד על מודל חי**):

- `PsCreateArcPlate` — `SetStartPoint` / `SetEndPoint` / `SetCenterPoint` / `SetNormal` /
  **`SetBigArc(Boolean)`** / `SetRotation` / `SetWidth` / `SetThickness`.
  ‏`SetBigArc` הוא מקש ה-ALT מהמדריך (קשת מעל 180°). **אין `SetRadius`** — הקשת מוגדרת בשלוש
  נקודות, ולכן **לא ידוע על איזה פן היא יושבת** (פנימי / אמצע עובי / חיצוני). זו בדיוק שאלת
  קוטר פנימי מול חיצוני, ושווה t על הרדיוס.
- `PsArcPlate.NeutralRadius` — **קריאה בלבד**, פלט ולא קלט.
- `PsCreatePlate.SetAsRadialPlate(Double Radius)` — פלטה עגולה (תחתית / כיפה שטוחה).
- `PsCreateUnfold` — **לא ידוע אם הוא מקבל פלטה מגולגלת או רק פלטה מכופפת.** זו השאלה
  הקריטית: בלעדיה אין מסלול מקורס מגולגל לפריסה שטוחה.
- מקדם התיקון `K` של המדריך — **ההיסק** (לא נמדד) הוא ש-`K` = פי 2 מ-k-factor התעשייתי, כי
  התיקון מוכפל בחצי העובי. מקדם 0.4 שיוזן ישירות ייתן k אפקטיבי של 0.2 ובלנק קצר מדי.
  ‏**K ניתן להצבה רק כארגומנט `KValue` של `PsBendPlate.CreateOfTwoPlates(...)`** — אין תכונת K
  על `PsBendPlate` ולא על `PsBendPlateFlange`, ולכן מסלול `AddFlange` אולי אינו שולט בפריסה.
- `PsGlobalSettings.ArcPlateAsAcis` + `ArcResolution` הם **ברמת המודל**, לא לפי אובייקט. במידול
  פאות, קורס מגולגל הוא קירוב פריזמטי ולכן **המשקל המחושב נמוך מהאמת**.

⚠️ כל השורות למעלה הן קריאה של מפת טיפוסים. אף אחת לא נבדקה מול מודל חי.

קשור: [[consult-before-widening]] · [[tankforge-project]] · [[seam-developed-layout]] ·
[[head-geometry-flat]] · [[vessels-stage1]]
