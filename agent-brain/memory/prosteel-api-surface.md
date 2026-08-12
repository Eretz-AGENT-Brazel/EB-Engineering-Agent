---
name: prosteel-api-surface
description: "ProStructures exposes 771 public .NET types across 75 assemblies — reflect over them, never guess or hand-build"
metadata: 
  node_type: memory
  type: project
  originSessionId: a4c7fddc-7ed7-48b9-a359-e4b37dc8fdbe
  modified: 2026-08-12T07:47:17.616Z
---

‏02/08/2026 — עשיתי רפלקציה על קובצי ProStructures עצמם ומצאתי שאני משתמש ב-**26 מתוך 771**
טיפוסים ציבוריים (3.4%). כמעט כל שעה שבזבזתי בשיעורים 4–5 הייתה קריאה לפונקציה שכבר קיימת.

```
C:\Program Files\Bentley\ProStructures Ss6 R1\AutoCAD 2015\Prg\
  ProStructuresNet.dll   392 public types · 4,796 signatures
  PSN_*.dll      (62)    מאקרו החיבורים — כולם .NET מנוהל (כולל PSN_BasePlate)
  PC3D*.dll      (12)    רכיבי בטון — כולם .NET מנוהל
  = 75 assemblies · 771 public types
```

**המפה שמורה:** `EB PROSTEEL AGENT\api+knowledge-develop\knowledge\api\API-SURFACE-RAW.txt`
(383 KB) + `API-SURFACE.md` (המדריך, עם טבלת "מה בניתי ביד מול מה שקיים").
דוח מלא: `api+knowledge-develop\lessons\תוכנית-השתלטות-על-התוכנה.pdf`.
*(נתיבים עודכנו 12/08/2026 — סידור התיקייה: נתיב הפיתוח עבר ל-`api+knowledge-develop\`.)*

**המחלקות שפותרות את הכשלים החוזרים:** `PsCreateFastener.CreateFastenerStraightAnchorBolt`
(עוגנים, עם Extrusion/Embedment/PlateThickness/GroutThickness) · ~~`PsGeometryFunctions`
(47 בדיקות יחסים = ה"עיניים")~~ **← טעות, תוקן 06/08/2026: יש בו 48 חברים ורק 5 בוליאניים,
וכולם עזרי גיאומטריה של קווים/מישורים — אין בו ולו בדיקת יחס אחת בין שני אובייקטים.
ה"עיניים" האמיתיות הן `PsObjectProperties.GetExtents` + `PsCompareDrawing` +
`PsCollisionCheck`.** · `PsMiscTools.ObjectsCopy/Mirror3d` · `PsSelection.RemoveDuplicates()`
· `PsObjectGroup` (הפרט כיחידה אחת) · `PsCollisionCheck` · `PsEdgeChamfer` ·
`PsBaseplateLinkDataMgd.PlateInShapeDirection` (אוריינטציית אוגן) + `CreateGroup` +
`CreateDetailedAnchorBolts`.

**תעלומת `bolt` נפתרה:** נכשלה 400 פעם כי ביקשתי אחיזה 280/430 מ"מ — אין שורה כזאת בטבלת
DIN 6914 (ברגי HV מבניים). `CreateSingleBolt` מוגדר `Void` ⇒ כישלון שקט. `boltprobe` הצליחה
כי אחיזתה 60. הכלי הנכון למוט עיגון הוא `PsCreateFastener`, לא בורג.

**LISP:** `ProStructures.mnl` הוא AutoLISP שנטען עם התפריט, ו-`Prg\load_net.lsp` הוא bootstrap
של Bentley ב-LISP. לכן `Editor.Command` נקי **רק** אם הטוקן רשום ב-`ProStructure.arx`/`ProSteel.arx`
— צריך רשימת היתר, לא הנחה. ל-NETLOAD: `FILEDIA=0` + `SendStringToExecute("_NETLOAD\n<path>\n")`.

**גם 40 מודלי דוגמה של Bentley מותקנים** ב-`Samples\COM Macros\` (25) ו-`Samples\Detailing\` (12),
והמדריך (1,179 עמ') מחולץ אצלנו מ-18/06 ב-`knowledge\manual_fulltext.txt`.

**איך ליישם:** לפני שאתה כותב לולאה או בונה משהו ביד — **חפש ב-API-SURFACE-RAW.txt**.
לפני שאתה מסיק מסקנה על מה שאין ב-API — ודא שסרקת את כל 75 ה-assemblies, לא אחד.

⚠ **סיכון פתוח (02/08/2026):** ניקיתי את כל ה-LISP מהקוד החי. `_netload()` ב-`app/eb_api.py`
עבר ל-`FILEDIA=0` + `SendStringToExecute("_NETLOAD\n<path>\n")` — **לא נבדק מול AutoCAD חי.**
אם הפלאגין לא נטען בסשן הבא והשגיאה היא "Unknown command EB_RUN.." — הסיבה שם, לא בפעולה.
לבדוק את זה ראשון כשהתוכנה נפתחת. 4 סקריפטים מתים עם LISP הועברו ל-`app/_attic/`.

קשור: [[two-phase-program]] · [[acad-agent]] · [[anchorage-findings]] · [[per-project-not-universal]]


---

## עדכון 06/08/2026 — יום קריאה ויישום

**נקראו במלואם 7 פרקים מהמדריך** (~2,800 שורות): `B.12.6` · `B.13` · `B.14` · `B.17` ·
`B.18` · `B.28` · `B.29`. הערות: `knowledge/MANUAL-NOTES-*.md`.
**התובנה:** *המדריך הוא תיעוד ה-API* — כל שדה בדיאלוג הוא תכונה. רפלקציה נותנת **שמות**,
המדריך נותן **סמנטיקה** (מה קורה כש-`Middle=0`, איזה פרמטר הוא קוטר **בורג** ולא חור).

**הפלאגין: v31 → v44.** קנוני נכון ל-06/08/2026: `EBAgentApi44.dll` / `EB_RUN44`. ‏48 פעולות.
מסמך מלא: `~/.claude/skills/prosteel-modeling/references/plugin-ops.md`.

**חמישה תיקונים שנמדדו (לא הוסקו):**
- `dia` בשדה חורים הוא קוטר ה**בורג**; חור = בורג + `play`. ‏`dia=23,play=3` נתן **⌀26**.
- קיצור פלטת בסיס = **עובי פלטה + grout** (‏20 ו-55 נמדדו), לא עובי בלבד.
- **קיטום לא יוצר אובייקטים** — הוא מקצר את הקורה (‏4850→4840). `Create()` החזיר **False**
  והפעולה עבדה.
- **פרופיל חלול נקדח בדופן אחת** בלי `SetIgnoreInnerContour` (‏8 מ"מ מול 200) — **הספירה זהה**.
- **`LongHoleMode` הוא מצב קריאה.** רק `kDoubleHole=2` חושף אורך חריץ; ב-0 חור אובלי
  מדווח כעגול ⇒ מכשיר האימות היה עיוור.

**חורים אובליים נסגרו** (חוב מ-שיעור 2): `SetAxisDistance` = `Rectangle Hole Axis`.
הקוד הקודם השתמש ב-`SetHoleStep` שהוא **חור מדרגה**, פונקציה אחרת.

**‏5 מ-6 מחברי קורות עובדים.** `op=conn` גנרית לכולם. המריץ דורש שני מקטעים + נעל.

⚠️ **בטיחות:** דיאלוג של ProSteel משאיר את AutoCAD עם `quiescent=True, CMDACTIVE=0` —
**ה-API לא רואה אותו.** נוסף `modal_dialogs()`/`ready()`, ו-`run()` מחזיר `EB_DIALOG`.
ונוסף **`use("<file>.dwg")`** — נעילת שרטוט שנשמרת לדיסק; הפלאגין מסרב עם `EB_ERR wrongdoc`.
*(עבודה נכנסה לשרטוט הלא נכון פעמיים באותו יום לפני זה.)*

**פתוח:** עוגנים · `PsEdgeChamfer` · Clone Manipulations · `PsCollisionCheck`.
*(מספור מהקוד נפתר בהמשך היום — ראה [[positioning-solved]].)*

---

## עדכון 06/08/2026 — המשך: v44 → v51, ושלושה שומרים חדשים

**קיטום פינה (`chamfer`) עובד.** הטעות ששווה לזכור: `PsEditModification` **קורא ומוחק**
מודיפיקציות — הוא לא יוצר. היוצר הוא **`PsCutObjects`** (‏`SetAsFacetCut` + `Apply()`),
ובידיו **עשרה** סוגי חיתוך, מתוכם תשעה עוד לא בשימוש — כולל
`SetAsPlateBreakEdgeCut` (עיבוד קצה) ו-`SetAsPolyCut` (חור בצורה חופשית).
‏`FacetType` **נמדד** (ה-dump נותן שמות בלי ערכים): **1 = קיטום ישר · 2 = קשת · 3 = קשת
הפוכה**; ‏0 ו-4 נדחים ונופלים ל-1. ⚠️ `contourVerts` **לא משתנה** — מודיפיקציה היא שכבה
נפרדת מקו המתאר, אז `GetPolygon` הוא המכשיר הלא נכון לשאלה "האם החיתוך קרה".

**שלושה כשלים שקטים נסגרו במנגנון:**
1. **פרמטר לא מוכר נבלע.** ארבע פלטות נוצרו עם `at=` (השם הנכון `center=`) — כולן נערמו על
   הראשית, וכל קריאה החזירה `EB_OK`. עכשיו הפלאגין **מסרב** לפרמטר לא מוכר, לפי טבלה
   **שנגזרת אוטומטית מהקוד**. ⇒ שגיאת כתיב בפרמטר חייבת להיות רועשת כמו שגיאה בשם פעולה.
2. **שני תהליכי AutoCAD.** `GetActiveObject` נתפס למופע שנרשם ראשון — לא לזה שעובדים בו,
   ושני המופעים מפרסמים אותו moniker. ⇒ `acad_instances()` בוחר לפי הנעילה ומסרב לנחש,
   ו-`_close_stray_docs` **מסרב לפעול** כששני תהליכים פתוחים. *אמיר: "זה קובץ שאני עובד
   עליו — אל תסגור אותו."* ⇒ **הקובץ של המשתמש אינו "שרטוט זר".**
3. **צילום מסך שיקר פעמיים.** בחר "התהליך הראשון" → צילם את החלון של אמיר; ואז
   `SetForegroundWindow` **נכשל בשקט** כשתהליך אחר בפוקוס (כלומר בדיוק כשהמשתמש עובד),
   ו-`CopyFromScreen` החזיר את הפיקסלים שלו. ⇒ בחירה **לפי כותרת** + **`PrintWindow`**
   (לא גונב פוקוס), ואם נכשל — **מסרב** במקום להחזיר תמונה מפוקפקת.

**פעולות תצוגה חדשות (v47):** `view dir=…` · `zoom handle=…|all=1` · `hilite` +
`app/eb_shot.py`. הסיבה: קיטום הצליח ואמיר לא ראה אותו — **פעולה שמצליחה מחוץ לתצוגה
הנוכחית אינה נבדלת מפעולה שלא קרתה.** להוכיח לעצמי בקריאה חוזרת ולהראות לו במצלמה הן
**שתי חובות נפרדות**.

**משקל מדווח הוא ברוטו** — חמש פלטות חתוכות שונה דיווחו 1.13 ק"ג זהה. המתג הוא
`VolumeWeightFlag` (במדריך: *"Volume Weight — the weight of the plates is determined by the
volume"*). פסיקת אמיר: לאומדן ראשוני זה זניח, לא לרדוף אחריו.

---

## עדכון 06/08/2026 — סוף היום: v51 → v64, ושלושה כלי אבחון חדשים

**🔊 הגילוי החשוב ביותר: ProSteel מאבחן את עצמו בערוץ שה-API לא מחזיר.**
קריאה נכשלה בשקט, וב**שורת הפקודה** היה כתוב
`* WARNING REQUESTED VOLUME SOLIDS CAN NOT BE PRODUCED`. ראיתי את זה רק כי צילום מסך
כלל במקרה את שורת הפקודה. **כל חקירות הכישלון השקט עד אז נעשו בעיוורון לערוץ הזה.**
נבנה `app/eb_log.py` (‏`LOGFILEMODE=1`): ‏`mark()` לפני פעולה, `problems()` אחריה.
⇒ **לעטוף כל פעולה ניסיונית בסוגריים האלה.**

**כלי אבחון נוספים:** `op=mods` (מלאי מודיפיקציות מלא לחלק — ספירת פאסטים/מישורי חיתוך/
שדות חורים/מגרעות/פוליחיתוכים/גופים) · `op=props` תוקן (חיפש `loadFrom` בשם רפלקציה,
השם הוא **`readFrom`** — היה מת שבועות) · `op=collision` (‏132 חלקים ב-0.4 שנ').

**‏`PsObjectProperties` הוא מכרה:** 100+ תכונות, השתמשתי ב-5. הכי חשובות:
`Origin/XAxis/YAxis/ZAxis` = **מערכת הצירים של החלק** (בזכותה השכפול עובד על חלקים
מסובבים ומשוקפים) · **`PaintArea`** = מ"ר צביעה (‏HE300B×4מ' → 6.885) · **`CutArea`** =
שטח חתך בס"מ² (‏HE300B → 149, בדיוק הקטלוג) · `Length/Wide/Height` · `KlemmLen`.

**מלכודות שנמדדו:**
- **שדה קידוח ממורכז על `at=`**, לא מתחיל בו. וחור שנופל **בדיוק על קצה החלק נמחק בשקט** —
  בלי שגיאה ובלי תלונה ביומן. נוסף שומר "ביקשתי מול נוצר".
- **`SelectAllObjects` מחזיר סטטוס ולא ספירה** (‏1 למודל של 132). ‏`ObjectCount` הוא המספר.
- **אין קונבנציית הצלחה**: `readFrom`→0=הצלחה · `Apply()`→1=הצלחה · `Create()`→false על
  קבוצה קיימת · `CreateFastener*`→**Int64 שהוא ה-ObjectId**, ‏0=סירוב.
- **כשהדאמפ והמהדר חלוקים — המהדר צודק.** הדאמפ לא מציג פרמטרי אינדקס של תכונות;
  ‏`get_Entry((short)i)` קיים ועובד למרות שהדאמפ מראה `P String Entry`.
- **`Type&` בדאמפ = פרמטר `ref`.**

**מבוי סתום מדוד:** `SetAsPlateBreakEdgeCut` (עיבוד קצה) — נשמר אך לא מייצר גיאומטריה,
בכל 7 הפריסות × 5 צירופי מידות/צד; על פרופיל לא נשמר כלל; ובלוק הרשומה **אין שדה
לבחירת קצה** בכלל. ‏`TakeoverDrills` — 5 רצפי קריאה עם בחירות מוכחות, אפס העברה.

**עוגנים — שני מסלולים:** ‏`PsCreateFastener` יושב ב-`ProStructures.Concrete` ו"Fastener"
לא מופיע במדריך ProSteel ⇒ ככל הנראה ProConcrete שלא ברישיון (מחזיר 0 עם כל 27 הסגנונות).
העוגנים האמיתיים של אמיר (`Ks_VolBody`) מגיעים מ-**Dowel של מאקרו פלטת הבסיס**
(`KsxBasePlate.Parameters.DowelFilename`) — ול-`PsBaseplateLinkDataMgd` המנוהל **אין
שום חבר Dowel**. שני מסלולים נפרדים. שאלה פתוחה לאמיר.

**‏`styles` תוקן:** לא הוגדר `Type` לפני `Initialize` ⇒ החזיר 0. עכשיו: 27 סגנונות ברגים
(‏Australia · NasccBolts ‏A325/A490 · DINBolts כולל DIN6914), 4 ריתוך, 14 דגלי מיקום.
