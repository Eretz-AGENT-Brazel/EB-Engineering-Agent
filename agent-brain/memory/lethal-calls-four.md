---
name: lethal-calls-four
description: "ארבע קריאות מפילות את AutoCAD, ובטיחות היא לפי טיפוס ולא לפי פעולה; נוהל השחזור תוקן"
metadata: 
  node_type: memory
  type: reference
  originSessionId: f89cce12-25c2-4c86-93f4-482926c41685
  modified: 2026-08-10T17:30:34.609Z
---

⛔ **ארבע קריאות מפילות את AutoCAD.** לפני כל קריאה לא מוכרת:
`knowledge/learning/findings/LETHAL-CALLS-do-not-invoke.md`.

1. `PsPlate.computeObjectWeigth` · 2. `PsVolume.checkHoleEdgeDistance` ·
3. **`PsGrid.addUserXaxis`** על גריד שנקשר דרך `PsTransaction` (בודד ושוחזר) ·
4. **קשירה+קריאה של `PsEditConnection`** דרך `PsTransaction.GetObject`.

> ⭐⭐ **בטיחות היא לפי טיפוס, לא לפי פעולה.** B.23 הסיק *"קריאה מאובייקט קשור בטוחה, מוטציה
> חשודה"* על סמך שלושה טיפוסים. `PsEditConnection` הוא **קריאה** והוא קטלני.
> קריאה־בטוח **עד כה**: `PsGrid`, `PsGussetConnection`, `PsPlate`, `PsShape`.
> כל שאר 57 עומסי-היתר — **לא ידוע, וכל תשובה עולה קריסה.**

⭐ **נוהל השחזור תוקן (10/08/2026):** להעביר את **ה-DWG בשורת הפקודה**, לא `/t <template>`.
תבנית יוצרת `Drawing1` חדש, וזה מרים דיאלוג **"Measurement Unit"** מודאלי ש**אי אפשר לסגור
מהקוד** — `BM_CLICK` ו-`WM_COMMAND`/`BN_CLICKED` שניהם התעלמו, שישה ניסיונות.
פתיחת שרטוט קיים לא שואלת, וגם לא נוצר מסמך שני שצריך לסגור.

⚠️ **`Get-Process acad` אינו המבחן** — קריסה יכולה להשאיר את התהליך ברשימה עם
`Responding=True` בזמן שהאופ לא חוזר לעולם. **`modal_dialogs()` הוא המבחן**: חלון
*"AutoCAD Error Report"* פירושו שהוא מת.

⚠️ דיאלוגים אחרי קריסה: `AutoCAD Error Report` → `WM_CLOSE`, **לעולם לא *Send Report*** ·
`Error Report - Cancelled` → OK · `Drawing Recovery` → **Close, לא לשחזר** (הקובץ בדיסק הוא
הטוב, כי שומרים לפני כל קריאה חשודה).

קשור: [[no-silent-skipping]] · [[part-b-complete]]
