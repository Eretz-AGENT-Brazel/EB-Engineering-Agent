<div dir="rtl" align="right">

# D.2 — BlockCenter

**המדריך:** עמ' 929–949 · שורות 24152–24781 · נקרא במלואו, חמשת תתי-הפרקים.
**רצועה:** ‏x = 480,000 · **מודל:** `D-miscellaneous.dwg` · **תוסף:** נבנה **v183**.

---

## 🛑🛑 הפרק הזה מפריך מסקנה שלנו מ-B.28

‏`plugin-ops.md` רשם תחת B.28:

> *"**'Model once, place it 40 times' — the feature does not exist**; the method does. There is
> **no** export-a-group-as-a-block, no Favorites, no template folder for 3D. The manual's
> *'deposited in a library in the form of a block'* **is DetailCenter, i.e. 2D detailing output,
> not model reuse**."*

**הכל נסוג.** התכונה היא D.2, היא **תלת-ממדית**, והמדריך אומר את כל העניין במשפט אחד:

> *"As it is a matter of ProSteel-blocks, the behaviour of these construction groups after
> insertion **is the same as if they have just been created out of individual components**."*

⭐ **ולמה טעינו — וזה החלק שמועיל להבא:** חיפשנו במשטח הטיפוסים מחלקה בשם *BlockCenter*, לא מצאנו,
והסקנו שהיכולת חסרה. היא יושבת על **`PsBlock`** — שנקרא על שם המושג של AutoCAD ולא על שם התכונה של
ProSteel. ⇒ **לחפש את היכולת, לא את השם שהמוצר נתן לה.** נרשם ב-`qc/retracted.tsv`.

---

## ✅ המסלול המלא, נמדד מקצה לקצה

המקור: מדגם S2 מ-D.5 — שתי פלטות 300×200×20 וארבעה M22, שכבר עברו ביקורת כמוברגים.

```
1)  block action=createfile handles=20C,20D,20E,20F,210,211  file=EB-block-boltedlap.dwg
       → create=True   exists=True   bytes=35,996        ← קובץ DWG אמיתי על הדיסק

2)  block action=insert  file=<נתיב מלא ל-dwg>  name=''  at=25877,141839.7,0
       → returnedId≠0   census 73→74   blockDim=300x200x70   ← בדיוק תיבת המדגם

3)  block action=explode handle=… keep=1
       → parts=6   census 74→80

4)  קריאה חוזרת מהמודל:
       plate 399  300x200x20  centre = 480000, 5000, 10   holes count=4   Ks_Plate
       plate 39A  300x200x20  centre = 480000, 5000, 30   holes count=4   Ks_Plate
       bolt  39B…39E  dia=22.0  'M 22x70 Mu DIN7990'                      Ks_Bolt
       vfy_fit  bolts=4 OK=4 BOLT-NO-HOLE=0 GAP-IN-PACKET=0 SHORT=0
```

⇒ ⭐⭐⭐ **המשפט של המדריך מאומת: החלקים המפוצצים הם חלקי ProSteel אמיתיים, עם החורים שלהם, לא
הפניית בלוק אילמת.** הם ניתנים לקידוח, להברגה, לספירה ולביקורת בדיוק כמו חלקים שנבנו ידנית.

---

## ⚠️⚠️ מלכודת הפרמטרים — ארבע איותים, אחד עובד

‏`InsertBlock(PathToBlock, BlockName, PsMatrix, Layername, Color)` רוצה את **הנתיב המלא ל-`.dwg`
בתוך `PathToBlock`, ו-`BlockName` ריק**:

```
file=<תיקייה>            name=EB-block-boltedlap        →  returnedId=0
file=<תיקייה>            name=EB-block-boltedlap.dwg    →  returnedId=0
file=<תיקייה\>           name=EB-block-boltedlap        →  returnedId=0
file=<תיקייה\שם.dwg>     name=''                        →  returnedId≠0  ✅
```

זו אותה משפחה כמו `MaxObjectDistance` של B.15: **הפרמטר ששמו נשמע כמו התשובה אינו התשובה.**

## ⭐⭐ מטריצת ההכנסה היא **הזזה טהורה**, ומכובדת בדיוק

היא נוספת לבסיס הפנימי של הבלוק, שאינו מיקום החלקים במקור. מודדים את הבסיס **פעם אחת**:

```
הכנסה ב-(0,0,0)            → החלקים נוחתים ב-  blockBase = (454,123.0, −136,839.7, 0)
הכנסה ב-(480,000, 5,000)   → נוחתים ב-         blockBase + (480,000, 5,000)   בדיוק

⇒   origin = יעד − blockBase
```

שני ההפרשים נמדדו **מדויקים לחלוטין** (480,000.0 ו-5,000.0). ובהצבה לפי הכלל, מרכזי הפלטות נקראו
**480000, 5000** למילימטר.

---

## ⚠️ הפיצוץ **אינו** צורך את הפניית הבלוק

אחרי `BlockExplode` ההפניה שורדת **בבסיס הבלוק** ומכפילה את כל המכלול. חייבים למחוק אותה.

⚠️⚠️ **ושני מכשירים חלוקים עליה:** `props` על אותו ידית עונה **`rc=3`** (נקרא כמת) בעוד ש-`op=list`
עדיין מציג אותה. **המפקד הוא שצדק** — השארית הייתה אמיתית, ומחיקתה הורידה את הספירה 80 → 79.
⇒ ⭐ **קריאת "לא קיים" ממכשיר אחד אינה הוכחה. תפרוט את המפקד לפי מחלקה** — כך זה נתפס: `Ks_Plate 30`
ו-`AcDbBlockReference 2` כשציפיתי לאחת.

---

## מה עוד נמדד

| | |
|---|---|
| **תיקיית הבסיס** | ⭐ `Ks_ComGlobalSettings.BlockCenterPath` = `…\Localised\`**`English`**`\UserBlocks` — הספרייה **תלוית שפה**, בדיוק כמו התבניות ב-A.2 |
| **תוכן** | 13 תת-תיקיות · **165 קובצי DWG** של המערכת (WeldFlags · PositionFlags · StairSteps · Handrail · HoleDisplayBlocks · Foot Anchors · GridAxis · ManualCut · OpenWebJoistBlocks · Theaded Inserts · BenchMarks · CastIn Elements · ElevationFlags) |
| **מסד הנתונים** | ⭐ **אין קובץ מסד בתיקיית הבסיס** ⇒ מסד ה-BlockCenter **מעולם לא נוצר במכונה הזאת**. המדריך: מסד ריק נוצר בהפעלה ראשונה, ואפשר לסרוק את התיקייה כדי לאמץ קבצים קיימים |
| **‏`PsBlockReference`** | ⚠️ `BlockName` ו-`BlockPath` הם **write-only** (הקומפיילר; ה-dump מציג אותם כמחרוזות רגילות) — **מקרה שישי** של המלכודת. רק `PartsCount` נקרא |
| **הדיאלוג** | ⛔ ל-BlockCenter עצמו **אין מחלקה מנוהלת**. הכל יושב על `PsBlock` |

---

## מה שנשאר בדיאלוג (ונקרא, כדי שנדע מה יש)

* **מסד נתונים עם שדות מוגדרי-משתמש** — `Text / Integer / Floating Point / Floating Point (fix)`,
  שמות שדה באותיות גדולות בלבד, טקסט דיאלוג ותיאור לכל שדה. **מיון היררכי לפי תוכן השדות**
  (דוגמת המדריך: חתך ← תבנית חורים ← הגנת גזירה) — זו הדרך למצוא פרט דומה מתוך מאות.
* **מסנן תצוגה** עם `AND`/`OR` וסוגריים, ותבניות מסנן — *"there is no limit concerning the
  complexity of the query"*.
* **נקודות הכנסה נוספות** — כדורי תלת-ממד גלויים, **נשמרות בשרטוט ולא במסד**, ולכן שורדות מחיקת
  רשומה. ⭐ ניתנות להעתקה/הזזה/מחיקה בפקודות AutoCAD רגילות. אפשר לתת לכל נקודה **יישור משלה**.
* **עדכון מסד** — שלוש מדיניות לשדות התקן: `Do not replace` · `Always replace` · `Only replace
  blanks`. ⚠️ *"each drawing in question has to be **opened** for update"* — לכן זה איטי.
* ⛔ **`Create database new` מוחק את כל השדות מוגדרי-המשתמש ואת תוכנם.**

---

## 🧾 BUILD PROOF

```
census סוף הפרק        79 ישויות  (‏Ks_Plate 30 · Ks_Bolt 28 · Ks_Shape 10 · Ks_Grid 7 ·
                                   PcRebarManager 2 · AcDbBlockReference 1 · AcDb3dSolid 1)
רצועת D.02             6 חלקים — 2 Ks_Plate + 4 Ks_Bolt, כולם דרך block insert+explode
vfy_fit 479000..481000 bolts=4 OK=4 BOLT-NO-HOLE=0 GAP-IN-PACKET=0 SHORT=0
ארטיפקט על הדיסק       projects/sandbox/EB-block-boltedlap.dwg — 35,996 בתים
```

**‏JOINT AUDIT:** הרצועה מכילה מכלול מוברג, והוא נבדק — ארבעת הברגים עוברים דרך שמונה חורים אמיתיים
(4 בכל פלטה), וזו בדיוק אותה ביקורת שהופעלה על מקור המדגם ב-D.5.

---

## שאלות פתוחות ⏳

1. ⏳ **לאמיר — הזדמנות אמיתית:** זו הדרך לספריית פרטים סטנדרטיים של ארץ ברזל **בין פרויקטים**.
   האם לפתוח תיקיית בלוקים של החברה (לא בתוך ההתקנה) ולהתחיל לאסוף בה פרטים חוזרים?
2. בסיס הבלוק (‏454,123 / ‎−136,839.7) הוא מספר פנימי שלא פוענח — **הכלל שעובד הוא למדוד אותו פעם
   אחת לכל בלוק**, לא להבין מאיפה הוא. לא נחקר.
3. ‏`InsertBlockScaled` (עם `ScaleX/Y/Z`) **לא נבדק**.
4. ‏`CreateBlock` (בלוק בתוך השרטוט, בלי קובץ) ו-`AddBlockReference` **לא נבדקו**.
5. ‏`CreateStairsStepBlock` — עוזר ייעודי מאוד למדרגות, לא נבדק.
6. מסד ה-BlockCenter מעולם לא נוצר כאן; יצירתו היא פעולה בדיאלוג ולא נוסתה.

</div>
