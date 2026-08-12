<div dir="rtl" align="right">

# D.7 — החלפת נתונים (Data Exchange)

**המדריך:** עמ' 981–1038 · שורות 25427–27111 · **1,684 שורות, נקראו במלואן**, שמונת תתי-הפרקים.

> ## ⛔ NO MODEL ARTEFACT — קריאה ולמידה בלבד, בהוראת אמיר
> **‏11/08/2026, הכרעת אמיר לפני יציאתו:** *"רק לקרוא וללמוד את הפרק הזה — בלי ליישם אותו."*
> **לא הורץ ייצוא. לא הורץ ייבוא. לא נכתב ולא נקרא אף מסד נתונים. לא נוצר אף קובץ.**
> כל מה שלמטה הוא **מיפוי מהמדריך וממשטח ה-API** — ואני אומר זאת מראש כדי שאף שורה כאן לא
> תיקרא כמדידה. ⚠️ **מפה אינה קריאה, ומיפוי אינו מדידה.**

---

## מה הפרק, בקצרה

ייבוא וייצוא אל ומתוך תוכנות CAD אחרות, תוכנות אנליזה, מערכות PPS ומכונות CNC. הפורמטים הם
בעיקר **קובצי ASCII** של ועדות תקינה (בעיקר **DSTV** הגרמני), ולכן ניתנים לעריכה בכל עורך טקסט.

**התרחיש המעשי שהמדריך מתאר הוא בדיוק של יצרן:** מערכת סטטית לאומדן חתכים ← CAD להצעת מחיר ←
מודל מפורט ← חזרה לאנליזה ← ולבסוף **נתוני NC למכונות** ו**רשימת חומרים ל-PPS**.

**הבעיה המרכזית שהוא מודה בה:** *"the volume of exchangeable information always depends on the
complexity of interface definition and the **implementation degree of transmitter and receiver**"* —
כלומר תמיד יאבד מידע, והשאלה היחידה היא כמה.

---

## ⭐⭐⭐ הפריט בעל הערך הגבוה ביותר בכל חלק D — פלט ה-CNC נגיש מקוד

**`Bentley.ProStructures.Miscellaneous.PsNCData`** — **כל דיאלוג ה-NC של D.7.8, תכונה-לשדה:**

```
CreateNCFile(FileName, TemplateName, PsSelection)  ·  CreateNcFile()  ·  SetParts(sel)
LoadFromTemplate(name)  ·  LoadFromLastDialogSettings()  ·  Init()

Format · OrderNumber · DrawingNumber · OverwriteExistingFile · IncludeHeader
Create3DCuts · Create3DHoles · CreateSignLines · ForceMetric · MaxHoleOffset
ShapeAsAcis · PlateAsAcis · FolderPath · Folder · FileAppendix · FileNameWithP
FileNameFromShippingNumber · PWisePath
DXFConturLayer/Color · DXFInnerConturLayer/Color · DXFHoleLayer/Color · DXFTextLayer/Color
DXFAs2D · DXF2DAsArcsAndLines · DXFLongHoleAsTwoHoles · DXFPrecision · DXFTextHeight
**DXFTextFormatLine1 / DXFTextFormatLine2**   ← מחרוזות ה-wildcards
```

⇒ **הפקודה `PS_NC_DATA` נגישה במלואה מקוד, כולל תבניות.** ‏⏳ **לא הורצה.**
⚠️ **תנאי מוקדם מהמדריך: חייבים לבצע מספור פוזיציות קודם** — וזה קושר ישירות ל-B.29 ולעובדה
שנמדדה שם: **אף חלק במודל B08 לא נשא מספר פוזיציה.**

**התקן:** *"Standard Description of Steel Construction Parts for the NC-Control"*, ‏DSTV, מהדורה
שביעית, יולי 1998. הפעולות: **ניסור · קידוח · חיתוך בגז · ניקוב · סימון**.
**דוגמת קובץ NC מהמדריך** (מלמדת את המבנה):
```
ST                  ← התחלה
 123 / 125 / 3 / 3  ← הזמנה, שרטוט, ...
 RST_37-2           ← חומר
 HE200B  /  I       ← חתך וסוג
 2980.00 200.00 200.00 15.00 9.00 18.00 61.30 1.15 …   ← אורך, מידות, משקל, שטח
BO                  ← קידוחים
 o 492.48 o 140.00 22.00 0.00
 o 492.48 o  60.00 22.00 0.00
EN
```

---

## ⭐⭐ אוצר מילים שלם של שדות חלק — רשימת ה-wildcards

לפלט ה-DXF/NC יש רשימת משתנים ענקית, והיא בפועל **מילון השדות המלא של חלק** — כל אחד מהם מתאים
לשדה שאנחנו כבר קוראים ב-`PsObjectProperties`:

```
%POSNUM% %SENDNUM% %ORIGINALNUM% %NAME% %INTERNALNAME% %KEY% %KATALOG% %ARTICEL%
%NOTE1% %NOTE2% %HANDLE% %MATERIAL% %COUNT% %TOTALCOUNT% %PARTART% %STYLENAME%
%LENGTH% %WIDE% %HEIGTH% %SLOPEDHEIGTH% %WEIGHT% %CUTAREA% %PAINTAREA% %LENADD%
%INSERTX% %INSERTY% %SCALE% %COLORINDEX% %FREEDESCRIPTION%
%TENSION% %DM% %KLEMMLEN% %MOUNTINGBOLT%          ← שדות בורג
%ORIGIN% %XAXIS% %YAXIS% %ZAXIS%                   ← מערכת הצירים של החלק
%DISPLAYCLASS% %AREACLASS% %FAMILYCLASS% (+NAME)   ← שלוש מערכות הסיווג של B.5
%GROUPNAME% %GROUPPOSNUM% %GROUPSENDNUM% %GROUPWEIGHT% %GROUPLENGTH/WIDTH/HEIGHT%
%DWG_PROJECT_NAME% %DWG_ORDER_NUMBER% %DWG_CUSTOMER% %DWG_CHECKED_NAME% …
```
⭐ **זה מאשר מכיוון שלישי ש-`AreaClass`/`FamilyClass`/`DisplayClass` הם שדות אמיתיים במורד הזרם**
(‏B.5 מדד שהם ריקים בכל המודל, ו-B.28 הראה 414 יתומים) — **הם יוצאים לקובץ ה-NC.**

---

## הממשקים, וההשלכה המעשית של כל אחד

| ממשק | קובץ | ייבוא | ייצוא | מה עובר |
|---|---|---|---|---|
| **DSTV Product Interface** | `.stp` | ✅ | ✅ | חתכים ופלטות, מיקום במרחב · **בלי פירוט ובלי אלמנטי חיבור** · גם מערכת סטטית עם אקסצנטריות |
| **PS3 (ProSteel Standard)** | `.ps3` | — | ✅ | ⭐ **העתק כמעט מדויק** כולל **פירוט, קבוצות, רשימת חומרים, מידע שרטוט וברגים**. מבנה כמו `.ini`. ⚠️ **פעולות בוליאניות אובדות**, ומחתכים מיוחדים עובר **רק השם ולא החתך** |
| **PSB (Object-Exchange)** | `.psb` בינארי | ✅ | ✅ | ⭐⭐ **שומר את מלוא האינטליגנציה כולל הקישורים הלוגיים** — ProSteel אל ProSteel על מערכות CAD שונות |
| **SDNF** | `.sdf` | ✅ | ✅ | תקן 3.0. חבילות: 00 כותרת · 10 חתכים ישרים · 20 פלטות פוליגון · **22 חורים** · 60 חתכים כפופים |
| **CIMSteel/2** | `.stp` | ✅ | ✅ | ‏ISO 10303-21:94. **שלושה מודלים**: Analysis · Design · **Manufacturing (כולל חורים, חורים מוארכים וחיתוכים)** |
| **STAAD III / STAAD.pro** | `.std` | ✅ | ✅ | מערכת סטטית לחתכי תקן. ⚠️ STAAD.pro **אינו תומך** בפלטות, חתכים כפולים וחתכי ריתוך |
| **PXF (REBIS)** | `.pxf` | ✅ | ✅ | קבוצת נתונים 4000, חתכי תקן |
| **KISS** | `.kss` | — | ✅ בלבד | **רשימת חומרים בלבד**. ⚠️ קבוצות רק בעקיפין דרך מספר משלוח |
| **RSTAB (Dlubal)** | ישיר | ✅ | ✅ | **בלי קובץ**, שתי התוכנות על אותה מכונה. ⭐ **מנגנון שינויים חכם** — רק חתכים ונקודות שהשתנו · ⚠️ **חייבים לשמור אחרי ייצוא**, המידע יושב במסד השרטוט |
| **DSTV PPS** | ASCII | — | ✅ | רשימת חומרים למערכת ייצור. **דורש מספור פוזיציות** |
| **DSTV Structural Output** | `.sc2` | ✅ | ⛔ | *"only the flow static → CAD has been implemented"* |
| **DSTV NC** | `.nc` / DXF | — | ✅ | ⭐ פלט ה-CNC. **דורש מספור פוזיציות** |

---

## ⭐ שני מנגנונים ששווים בפני עצמם

### 1. רשימות החלפה (D.7.2) — טבלת תרגום שמות

*"‏HEA100 ו-HE-100A כנראה מתארים את אותו חתך — עבור המחשב זו בעיה גדולה."* רשימת החלפה ממפה
שמות זרים לשמות ProSteel, **לומדת** לאורך זמן, וקובעת גם את שם הייצוא.
קובץ בינארי **`.exm`**, וניתן לייצא/לייבא כ-**dBASE** ולערוך ב-Excel:
```
0 TYPE       (F) שורת קבוצה · (S) חתך · (M) חומר
1 FAMILY     סוג החתך (למשל DIN_HEA) או "Material"
2 FILE       קובץ החתכים של ProSteel
3 METRIC     4 IMPERIAL     ← השמות הפנימיים
5 EXCHANGE   שם ההחלפה הראשון = **שם הייצוא**
6..n IMPORT1..n   שמות חלופיים נוספים
```
⭐ **מטרי מול אימפריאלי:** אפשר לייבא שמות אימפריאליים לשרטוט מטרי; לייצוא צריך **שתי רשימות
נפרדות**. ⚠️ בשדות `TRANSMETRIC`/`TRANSZOLL` שבמסדי החתכים כבר יש סט התחלתי — הממשקים החדשים
אינם משתמשים בהם, אבל הם מוצע טוב לרשימה ראשונה.

### 2. פיצול וחיבור מוטות — וזה קושר ישירות ל-D.6

* **בייבוא — `Connect`:** מוטות רציפים מחוברים לחתך אחד, לפי `Max. Distance` ו-`Tolerance`.
  *"reasonable when you import data of static analysis programs as these are generating a system
  of bars from gusset to gusset although in reality it is a continuous shape."*
* **בייצוא — `Nodes`:** חתכים רציפים **מפוצלים** בצמתים, לפי `Max. Distance` ו-**`Min. Length`**
  (אם הקטע יוצא קצר מזה — **לא מפצלים, ומחשבים אקסצנטריות במקום**).
* **`Protection`** בייצוא **מתעלם** מדגל ההגנה — וזה בדיוק ה-`No Discrimination` ו-
  `SetAnalysisIsProtected` שנמדדו ב-**D.6**.
* ⭐⭐ **`Analysis`:** *"instead of the CAD-position of the actual shape an **alternative static
  effective line is exported**"* ⇒ **זו הסיבה שכל D.6 קיים.** שני הפרקים הם צד אחד של אותו מטבע.

⭐ **פורמט KISS למחרוזות מידה** — `($Lif3)` = אורך, אינץ', שבר, דיוק ⅛:
`מימד L/W/T` · `יחידות c/i/m` · `תצוגה d/f` · `דיוק 0..8`.

---

## מסלול ה-API — **ממופה, לא הורץ**

```
PsMiscTools.RunExchangeExport(PsSelection, TemplateName, ExportFileName)
PsMiscTools.RunExportIsm(TemplateName, DrawingFile, ExportFileName, IgnoreRepository)
PsNCData.CreateNCFile(FileName, TemplateName, PsSelection)     ← פלט ה-CNC
PsCreatePartlist.PerformPartlist / PerformPartlist2 / CreateMDBFile
```
⏳ **אף אחד מהם לא נקרא.** הם רשומים כאן כדי שהמסלול יהיה ידוע ליום שבו אמיר יאשר.

---

## 🧾 BUILD PROOF

```
NO MODEL ARTEFACT -- Amir instructed read-only on 11/08/2026: learn the chapter, do not
implement it. No export was run, no import was run, no database was read or written and no
file was produced. The chapter is closed on step 1 plus this declaration.
```

**‏JOINT AUDIT:** לא רלוונטי — לא נבנה דבר.

---

## שאלות פתוחות ⏳ — וזו הרשימה שהכי שווה לאמיר בכל חלק D

1. ⭐⭐⭐ **פלט CNC (`PsNCData`) נגיש במלואו מקוד.** ארץ ברזל **מייצרת**, וזה הפלט לרצפת הייצור.
   **זו ההזדמנות הגדולה של הפרק.** ⏳ ממתין לאישורך.
2. ⚠️ **תנאי מוקדם: מספור פוזיציות.** גם NC וגם PPS דורשים אותו, ו-B.29 מדד ש**אף חלק במודל אינו
   נושא מספר פוזיציה**. ‏`posauto` בנוי ומוכן (‏8.7 שניות ל-677 חלקים) — **הסכימה היא שלך**
   (קידומות משפחה, סבילות זווית).
3. ‏**PSB** הוא הפורמט היחיד ששומר קישורים לוגיים — הדרך להעביר מודל שלם בין מערכות בלי לאבד חיבורים.
4. ‏**KISS** מייצא רשימת חומרים בלבד — ייתכן שזה בדיוק מה שצריך למערכת ההזמנות, בלי מודל.
5. **רשימת החלפה** היא הדרך לקבל מודלים מקונסטרוקטורים בלי לתקן שמות חתכים ביד.
6. ⚠️ **מה נאבד בכל ממשק** מתועד למעלה — ובעיקר: ‏DSTV `.stp` **אינו מעביר פירוט ואלמנטי חיבור**,
   ו-PS3 **מאבד פעולות בוליאניות**. מי שמקבל מאיתנו קובץ צריך לדעת את זה מראש.

</div>
