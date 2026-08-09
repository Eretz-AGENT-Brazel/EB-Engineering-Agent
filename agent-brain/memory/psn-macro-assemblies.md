---
name: psn-macro-assemblies
description: "ProStructures ships 62 PSN_*.dll macro assemblies — the Connection Center's own connections — but every entry point is interactive"
metadata: 
  node_type: memory
  type: project
  originSessionId: a4c7fddc-7ed7-48b9-a359-e4b37dc8fdbe
  modified: 2026-08-09T12:58:25.448Z
---

⚡ **מלבד `ProStructuresNet.dll` יש 62 assemblies של מאקרו** בתיקייה
`C:\Program Files\Bentley\ProStructures Ss6 R1\AutoCAD 2015\Prg\PSN_*.dll`.
אלה **החיבורים של ה-Connection Center** — אותם חיבורים שאמיר משתמש בהם מהממשק.

הם מכילים בדיוק את מה שחסם אותי:
`PSN_HollowShapeBracing` (B.24) · `PSN_DualGusset` + `PSN_WraparoundGusset` (B.23) ·
`PSN_BasePlate` · **`PSN_STAIRS` (50 טיפוסים)** · **`PSN_HANDRAIL` (46)** · `PSN_Truss` (49) ·
שמונה `PSN_BeamColumn*` · חמישה `PSN_BeamBeam*` · `PSN_CircularPlatform` · `PSN_CatWalk` ·
`PSN_RodBrace` · `PSN_PipeFlange` ועוד.

**הארכיטקטורה שונה לגמרי ממחלקות ה-`Ps*`:**
```
UserConnection   Create() · InitialCall() · CreateClone(ClsParameters) · Build/BuildI ·
                 Draw/DrawI · Edit/EditI · GetIdentifier() · GetDescription()
ClsParameters    SetDefaultValues(bool metric) · ReadFrom/WriteTo Connection|Clone|Template
                 + כל שדה בדיאלוג כתכונה
```
צריך לקמפל מול ה-DLL הספציפי. המחלקות **חיות**: מזדהות בשמן ומחזירות ברירות מחדל מטריות אמיתיות
(לייצוב: M20 `DIN7968`, פלטה 10, מרווח 10, מרווח פנוי 20).

⛔ **אבל כל נקודות הכניסה אינטראקטיביות.** ‏09/08/2026: `InitialCall()`, `CreateClone(params)`
ו-`Create()` — כל אחת הדפיסה *"Choose support shape"* והקפיאה את הסשן, כולן החזירו 0 ולא יצרו
כלום, גם כש-`ConnId1`/`ConnId2` הוצבו מראש למזהים אמיתיים.

⇒ זה מה שסוגר את B.24: **ייצוב אינו ניתן ליצירה מקוד — בתכנון.** אותו היגיון סוגר את הגאסט של
B.23, שנוצר על ידי פקודת הייצוב ולכן יורש ממנה את האינטראקטיביות.

⚠️⚠️ **שחזור מסשן תקוע: ENTER, לא ESC.** סשן שנתקע בשורת מאקרו שרד `eb_escape.py` ×4,
‏`PostMessage ESC` ×6 **וגם `SendInput` עם הקשות ESC אמיתיות ×5** — `ping` החזיר `EB_BUSY` אחרי
כולן. **ENTER אחד שחרר מיד**, כי בשורת בחירה ENTER פירושו "סיימתי לבחור, בלי בחירה" והמאקרו נוטש.

**How to apply:**
- ❌ **לעולם לא לקרוא לנקודת כניסה של `PSN_*` בלי השגחה** — זה מקפיא את הסשן.
- ✅ להוסיף ENTER לרצף השחזור לפני ESC.
- ✅ המשטח הזה עדיין שווה מיפוי לקריאה (ברירות מחדל, שמות פרמטרים) גם אם היצירה אינטראקטיבית.
- ⭐ נקודה פתוחה: `ReadFromTemplate`/`WriteToTemplate` עם `PsTemplateManager` — לא נוסה, ואולי
  זה המסלול שכן עוקף את הבחירה.

קשור: [[two-apis-com-wrapper]] · [[prosteel-api-surface]] · [[no-silent-skipping]]
