# 📕 פרק `B.28` — מבנה קבוצות · **נסגר הרמטית**

*‏06/08/2026. נקרא במלואו: שורות 8,643–8,947 (‏304 שורות) = `B.28.1` + `B.28.2` + `B.28.3` +
`B.28.4`. עמודים 364–375.*

**למה הפרק הזה:** "קבוצה" צפה כתשובה בשלושה פרקים נפרדים — כפתרון לפלטה שלא הסתובבה
עם העמוד, כתנאי לרשימות חלקים ולשרטוטים, וכ-`Form Group` בפלטת בסיס. **לא באמת ידעתי
מה קבוצה עושה.**

---

## 1 · 🔴 קבוצה אינה נוחות מידול — היא מקודדת את דרך הייצור וההרכבה

| הרמה | מה זה בפועל (ציטוט מהמדריך) |
|---|---|
| **Subgroup** | *"mostly correspond to the **purchase or stock parts** that in a preassembled condition are again used in component part groups or in assemblies"* |
| **Component part group** | *"mostly correspond to the **dispatched parts** that are delivered to the site in a **preassembled condition**"* |
| **Assembly** | *"consist of any component parts **without having a determined main part**... mostly correspond to several material groups, subgroups and single parts which are **combined on the site**"* |

⇒ **ההיררכיה אומרת: מה נקנה מוכן · מה נשלח כיחידה אחת · מה מורכב באתר.**
זה מתחבר ישירות להבחנה בין **מספר משלוח** למספר מיקום (`B.29`), ולסימוני הרכבה.

**מבנה:** קבוצה = **חלק ראשי** + חלקים נלווים. אסמבלי = **בלי חלק ראשי**.
קבוצות מקננות: מכניסים את ה**חלק הראשי** של תת-קבוצה לתוך הקבוצה שמעליה.

> *"**Certain functions apply to the complete group, even if you only select one part of the
> group.**"* ⇒ **זה הפתרון לפלטת הבסיס שנשארה מאחור בסיבוב.**

## 2 · יצירה, עריכה, וייצוא

| פעולה | הערות |
|---|---|
| **יצירה** | לוחצים על ה**חלק הראשי** ואז בוחרים את הנלווים. חלקים שנבחרו פעמיים — מתעלמים. **רק חלקי פלדה או חלקים מיוחדים** |
| **`Main Part Data`** | נתוני רשימת החלקים של הראשי הופכים לנתוני הקבוצה (**לא זמין לאסמבלי**) |
| **`Display as 1 Part`** | הקבוצה מוצגת כחלק אחד מלוכד (**לא לאסמבלי**) |
| **פירוק / מחיקה** | לוחצים על כל חלק בקבוצה |
| **הוספה / הסרה** | בחירת חלק אחד בוחרת את הקבוצה כולה, ואז מוסיפים/מסירים |
| **נתוני רשימה** | `Posnum` נלקח מהראשי אם `Take Main Part Info` מסומן. מוצגות **מידות חיצוניות ומשקל כולל**. נגיש גם דרך `Change PS Properties` |

### ⚠️ ייצוא/ייבוא קבוצה — לא עם פקודות AutoCAD
> *"store a group as external block (**like** an AutoCAD-'WBLOCK')... The group structure will
> remain unchanged... **Do not use the standard AutoCAD command for this task!**"*

- `Complete Group` — *"the complete group will always be exported, **even if only one component
  part was selected**"*.
- `Dissolve Blocks` — פיצוץ אוטומטי אחרי הייבוא, **המבנה נשמר**.
- פיצוץ ידני: **`PS_EXPLODE`**, לא `EXPLODE`.

⇒ **זו הדרך להעביר פרט מאושר בין שרטוטים** — כולל המבנה. מסלול אמיתי ל"ספר מתכונים".

### מצב בחירה
> *"whether the whole group has to be selected by selecting only one part, or whether the parts
> have to be independent... **you can select the whole group by clicking only one part when e.g.
> moving it by means of standard AutoCAD commands**."*

⇒ אפשר להפעיל את ההתנהגות הזאת גם לפקודות AutoCAD רגילות.

## 3 · 🔴 `B.28.3 Check Groups` — חבילת QA שלמה שלא ידעתי שקיימת

| הבדיקה | מה היא עושה | מה זה תופס אצלי |
|---|---|---|
| **בדיקת חלק ראשי** | *"checks whether all the groups have a main part. When a group without a main part is detected, **this group is dissolved**"*. קורה כשמוחקים את הראשי בלי לפרק | קבוצה שבורה שנראית תקינה |
| **`Mark / Highlight Orphans`** | *"display the parts that **don't belong to a group**"*, בצבע. מנוקה ב-`Regenerate` | **סמנטיקה טובה משלי** — בדקתי מגע גיאומטרי, וזו בדיקת **שייכות** |
| **`Release Single Part Groups`** | קבוצות עם חלק אחד בלבד משוחררות | רעש |
| **חיפוש לפי מספר מיקום** | תחביר: **`5,7,17-28`** (פסיקים, מקף לטווח). תצוגה: `Hide` / `Zoom` / `Zoom Extents` | איתור מהיר |
| **הסתרה לפי תפקיד** | `Main Parts` / `Single Parts` / `All Parts` | בידוד לבדיקה |
| **הצגת מבנה היררכי** | דיאלוג עם השתייכות, סטטוס החלק הראשי, והדגשה בצבע | הבנת מבנה מודל זר |
| **🔴 `Compare` + `Compare+Modify`** | *"groups having the **same position number**, which however are **not recognized as identical** at comparison, **will be corrected**. The position numbers are modified according to the settings of positioning"* | **בדיוק הכשל של מבחן 5** |

### למה `Compare+Modify` כל כך חשוב
במבחן 5 היו **20 פלטות רצפה באותו מספר מיקום** — 19 עם 4 חורים ואחת עם 6.
**התוכנה הייתה מזהה שהן לא זהות ומתקנת את המספור.** אני בניתי את הבדיקה הזאת מאפס
היום ב-`audit.py` (`check_hole_uniformity`).
⇒ יחד עם **`Equal Part Detection`** (`B.29`, שמשווה **מידות וקוטר קדיחה** בסבולות) —
**שער האחידות והכפילויות קיים בתוכנה.**

### מבנים מקוננים
> *"you will normally get the **lowest group level** at shape selection... However, you may
> **'scroll up and down' the structure** and thus modify the parent group properties."*

⇒ בחירת חלק מחזירה את הרמה הנמוכה ביותר. **צריך לטפס במפורש** כדי להגיע לקבוצת האב.

## 4 · `B.28.4` הגדרות
`Part Selection`: `Multiple` (כל חלק בנפרד) / `All` (הקבוצה כולה) ·
`Execute in Loop` (הפקודה חוזרת בלולאה בלי דיאלוג — ESC לעצירה) · `End Dialog`.

---

## ההתאמה ל-API — `PsObjectGroup` מכסה כמעט הכל

| המדריך | ה-API |
|---|---|
| יצירת קבוצה / תת-קבוצה / אסמבלי | `Create()` · `CreateSubGroup()` · `CreateAssembly(Origin, XAxis, YAxis)` |
| חלק ראשי | `getMainPart()` · `setMainPart(id)` · `IsMainPart(id)` · `getMainPartOf(id)` · `getTopMainPartOf(id)` |
| הוספה/הסרה | `AddMemberToGroup(group, id)` · `AddSubPart(id)` · `AddSubParts(sel)` · `RemoveMemberFromGroup(id)` |
| פירוק | `DeleteGroupFrom(id)` |
| חברי הקבוצה | `getAllParts(id)` · `getAllPartsOf(id, excludeSubMain, includeSubGroups)` · `getAllSubGroupPartsOf(id)` |
| נתוני רשימה | `computeWeight(id, withoutBolts)` · `ComputeDimension(id, out L, out W, out H)` · `WeightCenterOfGroup` |
| שם ומונים | `Groupname` · `PartCount` · `SubPartCount` |

**מה שנראה חסר ב-API:** `Check Groups` עצמו (Orphans, Compare+Modify) — לא מצאתי מקבילה
ישירה. ⚠️ **לבדוק אם זה נגיש דרך פקודת `PS_` ב-`Editor.Command`.**

---

## רשימת פעולה מהפרק

| # | מה | מקור |
|---|---|---|
| 1 | **כל פרט = קבוצה** לפני שכפול. זה מה שהיה מונע מהפלטה להישאר מאחור | §1 |
| 2 | לקבוע **חלק ראשי** מודע — הוא נותן את נתוני רשימת החלקים לקבוצה | §2 |
| 3 | לבנות היררכיה לפי **המציאות**: מלאי → משלוח → הרכבה באתר | §1 |
| 4 | להשתמש ב**ייצוא/ייבוא קבוצה** להעברת פרט מאושר בין שרטוטים (**לא WBLOCK**) | §2 |
| 5 | להריץ **`Check Groups`** לפני כל דיווח: יתומים · חלק ראשי · `Compare+Modify` | §3 |
| 6 | בקבוצות מקוננות — **לטפס במפורש** לרמה הנכונה | §3 |

## מה נשאר פתוח
- ⚠️ האם `Check Groups` (Orphans / Compare+Modify) נגיש ב-API או רק כפקודת `PS_`.
- ⚠️ האם ייצוא קבוצה כבלוק נגיש ב-API — `PsBlock` / `PsBlockReference` קיימים ב-`Drawing`.
- ⚠️ מה בדיוק ההבדל בהתנהגות בין `Component part group` ל-`Subgroup` בפועל
  (*"nearly behave identically"* — מה ה"nearly"?).

---

## ✅ תשובה לשאלה הפתוחה: האם `Check Groups` נגיש מהקוד

**‏`Check Groups` עצמו (Orphans · Compare+Modify · Release Single Part) — לא ב-API.**
סרקתי את כל 8,622 הטיפוסים הציבוריים: כל ההתאמות ל-`Orphan`/`Compare`/`Identical` הן
מ-assemblies של צד שלישי (`aSa`, `ISM`), לא מ-ProStructures.

**אבל מנוע זיהוי הזהות — שהוא הלב של `Compare+Modify` — כן חשוף,
דרך `PsCreatePositioning`:**

```csharp
// הפעלת זיהוי זהות, לכל רמה בנפרד
SetEqualPartSingles(bool)     SetEqualPartGroups(bool)      SetEqualPartSubGroups(bool)
SetEqualPartAssemblies(bool)  SetEqualPartConnections(bool)

// סבולות הייחוס -- "deviations with respect to dimensions and drill diameter" (B.29)
SetHolesTol(double)    // ← קוטר הקדיחה כקריטריון זהות
SetLengthTol(double)   SetWeightTol(double)   SetVolCheck(bool)
SetCompareFilter(PositioningCompareFilter)   // אילו תכונות מבדילות

// קריאת הפסיקה חזרה
RecordIdenticalRecord          // לחלק בודד
GroupRecordIdenticalRecord     // לקבוצה
SetOverrideExisting(bool)      // דריסת מספרים קיימים
```

### מה זה משנה
כתבתי היום ב-`app/audit.py` את `check_hole_uniformity` — קיבוץ לפי משפחה, ספירת חורים,
ודיווח על אי-אחידות. **התוכנה עושה את זה בעצמה, טוב יותר:**

| שלי | של התוכנה |
|---|---|
| אני מחליט מה "אותו חלק" (hash של מידות) | **סבולות מוגדרות** — אורך, משקל, נפח, **וקוטר קדיחה** |
| מדדתי ספירת חורים בלבד | משווה **מידות + קדיחה** לפי הקריטריונים שנבחרו |
| הרוב ≠ סמכות (תיקנתי את זה ידנית) | **מספר מיקום** הוא הסמכות, והוא מתוקן אוטומטית |

⇒ **הכיוון הנכון: להריץ מספור עם זיהוי זהות ולקרוא `RecordIdenticalRecord`** — במקום
להמציא הגדרת זהות משלי. הבדיקה שלי נשארת כשכבת ביניים עד שזה ייבנה.

⚠️ **פתוח:** האם אפשר להריץ את המנוע הזה **בלי** לכתוב מספרים למודל (מצב "בדיקה בלבד") —
`SetOverrideExisting(false)` הוא מועמד, אבל **טעון בדיקה בניסוי לפני שימוש על מודל אמיתי.**

---

# AUDITED 10/08/2026 — הדיאלוג לא ב-API, אבל הבדיקה כן

*הרשומה המלאה: `AUDIT-PART-B-2026-08-10.md` § B.28. תוסף v171 → **v172**.*

## ⭐⭐ `Check Groups` — הורכב מהשאילתות שכן חשופות

הפרק סרק את כל 8,622 הטיפוסים ומצא ש**הפקודה** אינה ב-API. נכון. אבל המדריך אומר מה כל כפתור
**עושה**, ושלושתם שאלות של **שייכות** — ו-`PsObjectGroup` עונה עליהן:

| הכפתור | המדריך | השאילתה |
|---|---|---|
| **Mark Orphans** | *"display the parts that **don't belong to a group**"* | `getMainPartOf(id) == 0` |
| בדיקת חלק ראשי | *"checks whether all the groups have a main part"* | `getMainPart() == 0` בקבוצה מאוכלסת |
| Release Single Part | קבוצה עם חלק אחד | `PartCount <= 1` |

⇒ ⭐ **אותו לקח כמו ששת הכפתורים הנפרדים ב-B.25: מדריך שמפרק פקודה לחלקים הוא הוראה להרכיב
אותה.** אופ חדש: **`grouporphans`**.

## ⚠️ והמכשיר אומת לפני שהמספר שלו נאמן

הריצה הראשונה החזירה `PartCount` שמטפס מונוטונית עם הידית — 7, 9, 11, 21, 23 — ו-`mainPart`
ששווה לידית של החלק **עצמו**. זה נראה בדיוק כמו מונה גלובלי שדולף, ו-B.27 בדיוק שילם על עמודת
ורדיקט שלא יכולה להיכשל.

**הצלבה מול `groupinfo`, שקורא את אותו אובייקט במסלול אחר:**

```
53C   parts=7   members 53C 558 559 55E 55F 560 561         main=53C   isMain=True
541   parts=21  21 חברים אמיתיים                              main=541   isMain=True
1610  parts=3   members 15ED 1610 1611                        main=15ED  isMain=False
15EE  parts=0   -- לא בקבוצה                                  ORPHAN
F76   parts=0   -- לא בקבוצה                                  ORPHAN
```

⇒ **הספירות אמיתיות.** הן מטפסות עם הידית כי המבנים הגדולים נבנו מאוחר יותר. החשד היה שגוי,
ולבדוק אותו היה נכון בכל זאת.

## התוצאה

```
grouporphans   steelParts=707  inGroup=293  ORPHAN=414
               GROUP-NO-MAIN=0   SINGLE-PART-GROUP=0   distinctGroups=65   err=0
```

✅ **שתי הבדיקות המבניות נקיות.** ⚠️ **414 מתוך 707 חלקי פלדה אינם שייכים לשום קבוצה.**

⭐⭐ **והפילוג אינו מקרי — הוא בין מה שהתוכנה בנתה לבין מה שנבנה ביד.** `1610`, פלטת גזירה מ-B.20,
יושבת בקבוצה של שלושה שהחלק הראשי שלה הוא **העמוד** — תבניות המחברים נושאות `CreateGroup=True`
ובונות את הקבוצה בעצמן. `15EE`, הקורה של אותו חיבור, ו-`F76`, מוט חיזוק מ-B.25 — יתומים.

> ⭐ **פעם שלישית שאותו גבול מופיע.** B.5 מדד אותו למערכות המחלקות: *"כל מה שמחלקת חיבור ייצרה
> נושא `FamilyClass`; כל מה שנבנה ביד — לא."* כאן זה שוב, עבור **קבוצות**.
> **מה שהתוכנה בונה, היא רושמת. מה שאנחנו בונים ביד — לא** — וכל צרכן במורד הזרם (רשימות חלקים,
> מספרי משלוח, `Display as 1 Part`) קורא את הרישום הזה.

## ⛔ לא קובצו — במכוון, ולפי התקדים של B.5

אפשר היה לקבץ 414 חלקים בלולאה. לא. §1 של הפרק אומר שקבוצה מקודדת **חלק מלאי / חלק משלוח /
הרכבה באתר** — זו **החלטת ייצור**, הטקסונומיה של אמיר, בדיוק כפי ש-B.5 סירב להמציא סכימת
מחלקות תצוגה. **היכולת הוכחה, הסכימה שלו.**

## מצב המודל
לא נבנה דבר, לא שונה דבר, לא קובץ דבר. **1,194** ישויות, נשמר.
