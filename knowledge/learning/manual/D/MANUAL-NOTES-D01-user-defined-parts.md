<div dir="rtl" align="right">

# D.1 — חלקים מוגדרי-משתמש (User-Defined Component Parts)

**המדריך:** עמ' 912–928 · שורות 23655–24152 · נקרא במלואו, ארבעת תתי-הפרקים.
**מודל:** `D-miscellaneous.dwg` · **תוסף:** נבנו **v180 → v182** בפרק הזה.
**פקודות (‏E.10):** `PS_CREATE_SPEZPART` (‏D.1.1) · `PS_CREATE_SOPRO` (‏D.1.2).

---

## ⭐⭐⭐ מה הפרק הזה, בשורה אחת

**‏D.1 הוא הפרק שמייצר את מה ש-B.8 למד לצרוך.** ביקורת B.8 מצאה ארבע מסדי-נתוני חתכים שאפשר
להכניס מהם — `kind=special` · `roofwall` · `combi` · `weld` — ויכלה רק לצרוך את מה שכבר קיים.
זה הצד המייצר. ואומת מהדיסק:

```
Data\UserShapes   → 68 קטלוגים      (kind=special, 1,528 חתכים ב-B.8)
Data\CombiShapes  → 15 קטלוגים      (kind=combi, 88)
Data\WeldShapes   →  3 קטלוגים      (kind=weld, 4)
```

**המספרים תואמים בדיוק** את מה ש-B.8 מדד מצד ה-API. אישור עצמאי משני כיוונים.

---

## D.1.4 — פרופילים פרמטריים ✅ **נגיש במלואו, ונמדד**

**המדריך:** *"At the very moment, **34 shape types** to be processed and created are available."*

**‏`PsUserShapeManager` נושא בדיוק 34 מתודות `BuildUserShape_*`** — ו-`ParametricUserShapeType`
מונה `kNoParams` + **34** ערכים. **שלושה מקורות בלתי-תלויים, אותו מספר.**

### הבדיקה: כל בנייה מול **צורה סגורה** של שטח החתך

‏`CutArea` קריא על המנהל, ולכן "הקריאה החזירה True" הופך למדידה אמיתית:

| kind | args | שטח ביד (מ"מ²) | `cutArea` שנקרא |
|---|---|---|---|
| `flat` | 100,10 | **1,000** | **0.001** ✅ |
| `rectangle` | 200,120 | **24,000** | **0.024** ✅ |
| `round` | 20 | 314.2 | 0.000 |
| `pipe` | 219.1,203.1 | 5,305.5 | 0.005 |
| `i_symetric` | 300,200,10,15 | 8,700 | 0.009 |
| `i_unsymetric` | 400,200,20,12,300,16 | 13,168 | 0.013 |
| `t_symetric` | 200,150,12,8 | 3,304 | 0.003 |
| `rectpipe` | 200,100,6,6,6,6 | 3,456 | 0.003 |

⭐⭐ **‏`PsUserShapeManager.CutArea` הוא ב-מ"ר, בעוד ש-`PsObjectProperties.CutArea` על חלק מוצב הוא
ב-סמ"ר** (‏HE300B נקרא 149 מול 149 סמ"ר בקטלוג). **אותו שם, שתי מחלקות, שתי יחידות.**
היחידה מוצמדת ע"י שני המקרים המדויקים (`1,000→0.001` ו-`24,000→0.024`); כל השאר עקביים לרמת
הדיוק המודפסת (שלוש ספרות אחרי הנקודה), ולכן **מאששים ואינם מכריעים לבדם** — נאמר כאן במפורש.

### ⭐⭐ אימות שני, בלתי תלוי לגמרי: שטח הצביעה

ל-I‏ 300×200, ‏tw=10, ‏tf=15 דווח **`paintArea = 1.38`**. חישוב ההיקף ביד:

```
אגף עליון   200 + 2(15) + 2(95) = 420
אגף תחתון                        = 420
מוט         2 × (300 − 2·15)     = 540
                          סה"כ   = 1,380 מ"מ  ⇒  1.38 מ"ר למטר אורך   ✅ מדויק
```

⇒ הגאומטריה נכונה, והכמויות הן **לכל מטר**.

### מה עוד נמדד

| | |
|---|---|
| **`Draw()`** | ⭐ יוצר **`AcDbBlockReference` על שכבה 0** — מפקד 72→73. זו **התצוגה המקדימה של הדיאלוג** (*"by means of preview you can control how your shape has to look"*), **לא** הכנסת פרופיל. אין כאן `Ks_Shape` |
| **`WriteFile`** | ✅ כתב **`.psp` אמיתי, 1,748 בתים** לנתיב שנתתי |
| **`LoadFile`** | ✅ סבב הלוך-חזור מלא — `name` שרד, `cutArea`/`paintArea`/`H` זהים |
| ⭐⭐ **המפתח הוא שם הקובץ** | כתבתי `key='EBI300'` וקריאה חוזרת החזירה **`key='EB-testshape'`** — שם הקובץ. **זה בדיוק הכלל של B.8** (*"address a section by its FILENAME; the internal name field lies"*), **מאושר עכשיו מהצד המייצר** |
| **רזולוציות** | `poly=1/0/0` — נוצרה **NORMAL בלבד**. המדריך אומר זאת מראש: *"three different display modes (low, normal and high – **which must be created individually**)"* ✅ |

⛔ **לא כתבתי אף קטלוג לתוך `Data\UserShapes`.** ‏`WriteFile`/`WriteShapeSystem` משנים את **התקנת
התוכנה של אמיר**; ה-`.psp` הודגם לנתיב בטוח בתוך הפרויקט. אותו כלל כמו A.5 ו-A.6.

---

## D.1.2 — חתכים מיוחדים · חתכי שילוב

**ארבע קבוצות-אב:** חתכים מיוחדים כלליים · חתכי גג-קיר · **חתכי שילוב** · **חתכי ריתוך**.
שתי הראשונות נוצרות מציור מתאר; חתך שילוב מורכב מפרופילים קיימים; חתכי ריתוך מדיאלוג ייעודי.

✅ **בוני השילוב נגישים מקוד** — שלושה: `BuildCombiShapeDoubleL(Depth,Width,Thick,Space)` ·
`BuildCombiShapeDouble(Key,Katalog,Space,Type)` · `BuildCombiShapeCoverPlate(Key,Katalog,Width,Thickness,Type)`.
`combi_doublel 100,100,10,12` בנה והחזיר `cutArea=0.004` ⇒ 4,000 מ"מ² = **שתי זוויות L100×10**
(1,900 מ"מ² כל אחת ≈ 3,800, פלוס עיגולי שורש) — סדר גודל נכון, לא נבדק לצורה סגורה מדויקת.

⭐ **חתך שילוב ניתן לפירוק** — *"you can dissolve the combination shapes into their individual
parts and any processing of the individual segments is kept"* — ולכן הוא כלי תכנון לגיטימי, לא רק
נוחות. ⚠️ המדריך: **אסור שהפרופילים יהיו משוקפים**, ורצוי לסדר אותם כקטעים באורך ~100 מ"מ.

### ⭐⭐⭐ מסלול Variant + DBASE — קיים במסמך, **ולא בשימוש בהתקנה הזאת**

הדרך להגדיר **סדרה שלמה** של חתכים מטבלה: תיקייה תחת `...\sopro` ששמה הוא "סוג החתך", ובתוכה
`rules.dat` (ASCII) + קובץ `.dbf`. משתנים `$1,$2,$3…` מוחלפים מהרשומות.

```
FILE=TEST.DBF · RESOLUTION=LOW|NORMAL|HIGH · THICKNESS=d · STARTPOINT(x,y)
LINEAR_TO(dx,dy) · LINEAR_AT(x,y) · ARC_TO(r,phi) · ARC_AT(cx,cy,ex,ey)
INSERTPOINT(x,y)  ← עד 16, וקבוצה אחת לכל הרזולוציות · RESOLUTION=END
```
מבנה ה-DBF: `KEY NAME NOTE1 NOTE2 ITEM MATERIAL WEIGHT FIELD1…FIELDn`.
⚠️ המדריך: **אסור `.psp` באותה תיקייה** — סוג הוא או-או.
⭐ `THICKNESS=d` מאפשר לתאר רק את **הסיב הנייטרלי** (פוליגון פתוח) והתוכנה סוגרת אותו — בדיוק
המסלול לפרופילים מגולגלים-קרים.

**נמדד:** סריקת `Data\` כולה לא מצאה **ולו `rules.dat` אחד**. ⇒ המסלול מתועד ו**אף קטלוג מותקן אינו
מממש אותו**. ⭐ ומכיוון שהוא **מבוסס-קבצים**, הוא נגיש בלי דיאלוג — אותה תובנה כמו A.3.2 (*"התבניות
הן קבצים על הדיסק"*). ⏳ **יצירת תיקיית סוג חדשה היא שינוי בהתקנה — החלטת אמיר.**

---

## D.1.3 — חתכי ריתוך ⛔ **היוצר מסרב, שלוש תצורות**

⭐⭐⭐ **`PsCreateUserShape` הוא הדיאלוג "Create Weld Shapes I-Type" במיפוי אחד-לאחד:**

| הדיאלוג | ה-API |
|---|---|
| Top Flange | `SetUpperPolygon(Width, Height)` + `SetUpperMaterial/Article` |
| **Web Plate** | `SetFlangePolygon(...)` + `SetFlangeMaterial/Article` ⚠️ השם של המוצר, לא שלי |
| Bottom Flange | `SetLowerPolygon(...)` + `SetLowerMaterial/Article` |
| **Treat Parts** | **`SetTreatAsSingles`** — *"treated like the contained individual parts at positioning, parts list and detailing"* |
| Check Flats | `SetExplodeToPlates` |
| Create Flange | `SetCreateSegments` |
| *"possible to change the calculated weight"* | **`SetWeight`** · וגם `SetCutArea`/`SetPaintArea` |

⛔ **`Create()` מחזיר False והמפקד אינו זז — בשלוש תצורות:**

```
1) שלוש הפלטות + Treat Parts=1        → False, 73→73
2) שלוש הפלטות + Treat Parts=0        → False, 73→73
3) + SetHeight/SetWidth/SetCutArea/SetWeight + SetCrossSection(key,catalog)  → False, 73→73
```
**ושורת הפקודה של ProSteel שתקה בשלושתן** (`eb_log` עטף את הריצה). ⇒ לפי כלל שלושת הסטרייקים,
ו**כיוון ששלושתן נכשלו באותה צורה בדיוק** — הסימן שהמסלול סגור ולא שעדיין לומדים —
**נרשם כסגור ולא ימשיך להיות מותמר.** ⏸️ הדרך שנשארה היא הדיאלוג `PS_CREATE_SOPRO`.

> ⚠️ **זה חבל, וזה שווה לומר:** קורת פלטות מרותכת היא הפריט בעל הערך הגבוה ביותר בפרק ל-ארץ ברזל.
> ⭐ **אבל יש מסלול חלופי מוכח:** ‏B.8 מדד ש-`bendshape` הוא **היוצר היחיד עם `SelectWeldSections`**
> ומגיע ל-`Data\WeldShapes` (`I950x300x30`, `K900x400`). כלומר **צריכה** של חתך ריתוך עובדת מקוד;
> רק **הגדרת** חתך חדש חסומה.

---

## D.1.1 — חלקים מיוחדים ⛔ דיאלוג בלבד

*"add parts list information to any existing component parts … this part will be recognized as
ProSteel component part"*, כולל ישויות שלא נוצרו ב-ProSteel. ‏`ESC` יוצר בלוק מהחלקים; `ALT` מייצר
כמה חלקים מיוחדים עם אותן תכונות.

⛔ **לא נמצאה מחלקה מנוהלת.** סריקת המשטח ל-`SpezPart|SpecialPart|SpecialShape` החזירה רק את
`PsUserShapeManager`, `PsCreateUserShape` ואת תאומי ה-COM (`Ks_ComDefineUserShape`,
`Ks_ComUserShapes`). הפקודה `PS_CREATE_SPEZPART` היא אינטראקטיבית (בחירת חלקים, ואז נקודת הכנסה)
⇒ **לא הורצה** בסשן לא מאויש.
⚠️ המדריך מציין מגבלה אמיתית: *"a 2D-depiction cannot be generated when no volume parts are
contained"* — חלק מיוחד בלי נפח נכנס לרשימת החומרים אך אינו מפורט.
⭐ **התאומים ב-COM לא נוסו** — וזה בדיוק המקום שבו COM חילץ את .NET ב-B.6 (`PsGrid`). **הליד הבא
בשמו**, ולא נבדק הלילה.

---

## 🧾 BUILD PROOF

```
census אחרי הפרק      73 ישויות  (69 מ-D.5 + צינור + מוט ACIS + AcDb3dSolid + block reference של Draw)
usershape             14 טיפוסים נבנו והוחזרו True; 8 אומתו מול צורה סגורה
paintArea             1.38 מ"ר/מ' — היקף מחושב ביד, תואם מדויק
.psp                  1,748 בתים נכתבו ונטענו חזרה
weld_i                3 תצורות, כולן False, מפקד 73→73
```
**אין מכלול מוברג חדש בפרק הזה** ⇒ ביקורת הצמתים של הרצועה לא השתנתה מ-D.5 (מצוטטת שם).
הישויות שנוספו הן הגדרת חתך ותצוגה מקדימה, לא אלמנטים במבנה.

---

## מה נוסף ל-API

| אופ | מה הוא נותן |
|---|---|
| **`usershape`** | ‏`list=1` · ‏34 הבונים הפרמטריים + 3 בוני שילוב · `draw=1` · `write=`/`load=` · `kind=weld_i` (מסרב, מתועד) · קורא בחזרה `cutArea`/`paintArea`/`H`/`poly` |

---

## שאלות פתוחות ⏳

1. ⏳ **לאמיר:** האם לפתוח קטלוג `EB` תחת `Data\UserShapes` ולכתוב אליו את חתכי החברה? זה **שינוי
   בהתקנה** ולכן החלטה שלו. אם כן — ‏`WriteFile` הוכח שעובד, וכל 34 הטיפוסים זמינים.
2. ⏳ **מסלול ה-Variant + DBASE** הוא הדרך לייצר **סדרה** מטבלה, והוא מבוסס-קבצים בלבד. אף קטלוג
   מותקן אינו משתמש בו. שווה הדגמה — גם היא שינוי בהתקנה.
3. תאומי ה-COM (`Ks_ComDefineUserShape`) **לא נוסו** — הליד הפתוח על חתכי הריתוך ועל D.1.1.
4. ‏`combi_doublel` לא נבדק מול צורה סגורה מדויקת (רק סדר גודל).

</div>
