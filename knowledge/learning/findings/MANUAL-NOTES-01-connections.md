# 📖 הערות מהמדריך — סבב 1: מחברים, קדיחה, העתקה, קבוצות

*‏06/08/2026. נקרא מתוך `manual_fulltext.txt` (‏1,179 עמ'). זהו הארטיפקט שמוכיח שקראתי —
לא מפה ולא אינדקס. כל שורה כאן היא ציטוט או מסקנה ישירה מהטקסט.*

**מה נקרא בסבב הזה:** `B.4` העתקה/הזזה/סיבוב/שיקוף/שכפול-עיבודים · `B.14` קדיחה וחיבורי
ברגים · `B.16` ריפים · `B.17` מחברי פלטות · `B.20` פלטות גזירה · `B.21` חיבורי אורך ·
`B.22` מריצים · `B.26` הנצים · `B.27` עורך המחברים · `B.28` קבוצות.

---

## 🔴 חמישה ממצאים שסותרים או משלימים מה שחשבתי

### 1. ‏Clone Manipulations — התשובה לכשל של מבחן 5

> *"if a hangar has been constructed with many identical supports and **holes have to be
> added later to each support, they may be added to just one support and then transferred
> to all of the others**."*

**‏19 פלטות איבדו 2 חורים כל אחת ואני קדחתי אותן אחת-אחת.** הדרך הנכונה: לקדוח **אחת**
ולהעביר. מה שמועבר: `Cuts` (כולל מיטר) · `Drill Holes` · `PolyCut` · `Notches` · `Boolean`.

**⚠️ שני תנאים קריטיים:**
- *"only parts with the **same position number** as the original part will be considered"*
  ⇒ **מספור אינו שלב פלט בסוף — הוא כלי מידול.** בלי מספור אין העברת עיבודים.
- *"the transfer refers to the **coordinate system of the parts**"* — חלק שמערכת הצירים שלו
  מתחילה מצד שני יקבל את החור מהצד השני. מקור לשגיאה שקטה.

### 2. הפרט חייב להיות **קבוצה** — לא רשימת handles

> *"If several individual components have been assembled into groups, this command can be
> used to **process the entire group by selecting just one part** of the group."*

**זה בדיוק הכשל שבו פלטת הבסיס לא הסתובבה עם העמוד** ("כמו לדבר לקיר"). בניתי `replicate`
עם רשימת handles מפורשת; הדרך של התוכנה היא **קבוצה**. ויש `Turn+Copy` — סיבוב והעתקה
בפעולה אחת.
`B.28`: *"groups can be stored as **block** and taken over into other drawings – the structure
remains unchanged. **The AutoCAD standard commands for blocks cannot be used**"* (יש פונקציות
ייעודיות). וגם: **מבנה הקבוצות מזין את רשימות החלקים ואת השרטוטים האוטומטיים.**

### 3. ברגים **כן** קודחים — במסלול אחר ממה שבדקתי

> *"the bolts automatically **create the necessary drill holes in all participating component
> parts**. You don't have to specify in which flanges — this is automatically determined."*

קבעתי כ"מבוי סתום מדוד" ש-`CreateSingleBolt`+`AddObject` לא קודח. **זה נכון לאותה קריאה,
ושגוי כהכללה.** לפקודת ה-Bolted Connection יש:
- `Touch Plane` — מחפש **משטחי מגע** בעצמו; ה-UCS לא חייב להיות ניצב.
- `Update auto` — *"a **logical link** is created between the participating parts"* ⇒ עדכון אוטומטי.
- `Gap` — מרחק מגע מרבי; מעליו **לא ייווצר חיבור** (מועמד לכישלון שקט).
- *"the element selected **first** represents the **main element**. All others are only bolted
  elements... drill hole fields of the bolted elements **depend on** the main element's and
  cannot be modified independently."*
- ⚠️ *"the bolts are **not assigned to a group**. This has to be made later using 'Group'."*

### 4. ‏`Search Closest Flat` — מקור הבלבול "פלטה או פרופיל"

> *"**Use Flat**: a flat steel is used instead of a poly-plate... **Search Closest Flat**: the
> current polyplate is **replaced by the closest fitting flat steel**."*

**זה מסביר למה פלטות הבסיס במודלים של אמיר הן `Ks_Shape` מקטלוג `DIN_FLACH` ולא `Ks_Plate`** —
הבלבול שעלה לי שיעור שלם. וב-`B.20`: *"**Turn Flat**: FL 110x10 becomes **BRFL** 250x10"* —
**מקור השם `BRFL`** שראיתי אצלו.

### 5. ‏`Round to` — עקרון החזרתיות של אמיר, כפרמטר בתוכנה

> *"The **Round to** value describes a rounding accuracy applicable to the width of the
> calculated stiffened plate... it is possible to permit only **dimensions divisible by 5, so
> that flat steel bars can be used**."*

**התוכנה בנויה לצמצם את מגוון החתכים.** זה בדיוק *"7 עוביים ו-100 תוכניות חיתוך = מתכון
לטעויות"*. אמיר לא המציא את העיקרון — הוא הפרקטיקה שהתוכנה מניחה.

---

## מחברים — הכללים התפעוליים

### סדר הבחירה (מאשר את סדר הפרמטרים ב-`op=conn`)
> *"you are first prompted for **the shape to be connected**, which you have to click **at the
> end to be connected**. Then you have to click **the supporting shape** — or ignore it with RETURN."*

- ‏`SetConnectionObjectId` = הקורה · `SetSupportObjectId` = התומך. **הסדר שלי נכון.**
- **בלי תומך** ⇒ *"you can create a simple end plate at the shape to be connected"* /
  *"only one or two web plates are attached to the end"*. כלומר `support` אופציונלי ומשנה התנהגות.
- *"The shape to be connected is **cut to the proper length**"* ⇒ **המחבר חותך את הקורה.**
  יש לו תופעת לוואי על האורך, כמו `ShortenShape` בפלטת בסיס. **לאמת בכל יצירה.**

### ריפים (`B.16`)
> *"In the case of symmetric shapes such as HEA, HEB, **two opposite stiffeners** are created...
> When only one is needed, just delete the other. **However, it will not be restored during an update**."*

- תמיד בזוגות. מחיקה של אחד = הוא לא יחזור בעדכון (וזה גם אומר שעדכון לא ישחזר אותו — טוב לדעת).
- ‏`Full` / `Half` / `By Length` (‏10–90% מגובה הגוף) / `Square` (משולש).
- ‏`Flange Offset` · `Web Distance` · `Offset` (שלילי = בליטה החוצה) · `Radius` ברדיוס הפרופיל.

### קדיחה (`B.14`)
- **שדות חורים, לא חורים בודדים:** *"groups consisting of 2x2 holes will be drilled in one operation"*.
- **תחביר פריסה:** `Number1*Pitch1, IntermediatePitch1, Number2*Pitch2, ...`
  `Shape/X Dir` = בכיוון הפרופיל · `Cross/Y Dir` = ניצב לו.
  ⇒ **זו התשובה ל-2×3 מול 3×2** — הצירים מפורשים בתחביר.
- **עומק הקדיחה תמיד לאורך ציר Z של ה-UCS הפעיל.** *"it is easier to work in one of the views"*
  ⇒ **מבט לפני קדיחה**, בדיוק כמו שאמיר עושה מבט לפני מיקום.
- ‏`Addition` / `Workloose` = מרווח החור. *"which **usually consists of 2 mm**"* —
  **הכלל של אמיר הוא 3.** תמיד לשלוח במפורש.
- סוגי חור: `Normal` · `Countersunk` (עומק+זווית) · `Step Hole` · **thread holes**.
- ‏`Shape Centre` — *"the insertion point is perpendicular to the shape centre... you will always
  obtain **symmetrical** drill holes"*. שימושי לפריסה מרכזית.
- רשימת הקטרים נמצאת ב-`..\prg\pro_st3d.hdr` — **קובץ שאפשר לקרוא.**

### עורך המחברים (`B.27`) — בודק מובנה שלא הכרתי
> *"If **Verify Connections** has been checked, all connections are verified with regard to
> **collisions and marginal distances**... **Green** = the connection is correct ·
> **Yellow** = the hole distances are not observed · **Red** = the connection has collisions."*

- **מרחקי קצה נבדקים על ידי התוכנה.** לא צריך לכתוב את הבדיקה הזאת מאפס.
- *"**Edit Single Connection**: select the part and click **MODIFY CONNECTION** in the right-click
  context menu"* — עריכה במקום. **הפער היחיד ביכולת שזיהיתי — קיים בממשק.**
- אפשר **להחליף סוג מחבר ולהעביר נתונים**: *"you can replace a plate connection by a web angle"*.
- ‏`COM-Connections` — *"all external connections realized via the **COM-PlugIn's**"* היא **קטגוריה
  נפרדת**. ⇒ מסביר למה `connscan` שלי החזיר `withlinks=0` על מודל דוגמה של Bentley שיש בו
  `Ks_Connection`: הסורק שלי רואה רק מחברים סטנדרטיים.
- ⚠️ *"the modifications will not be valid before **re-opening of the command a second time**"*.
- ‏`Delete with` — מחיקת מחבר מוחקת **את כל מה שהוא יצר**.

### מריצים (`B.22`) — למה `Create()` שלי החזיר False
המחבר הוא **קורת גג + נעל מריץ (purlin socket) + מהלך מריץ (purlin course)**, עם
`Dia` לחיבור קורה↔נעל ו-`Dia Side` לחיבור נעל↔מריץ, `Backer Plates`, `Opposite Side`.
ב-API יש **`SetPurlin2Id`** ⇒ הוא מצפה ל**שני מקטעי מריץ**. אני בניתי מריץ **רציף אחד** שעובר
מעל הקורה. **הגיאומטריה שלי הייתה שגויה, לא היכולת.**

### פלטות גזירה (`B.20`)
`Cut Plate` (חיתוך בפרופילים משופעים) · `Normal to...` (הפלטה ניצבת לקורה במקום בכיוונה) ·
`Poly-Plates` במקום שטוחים · `Turn Flat` · `Position` (צד הגוף).

### חיבורי אורך (`B.21`)
דורש ש**שני הפרופילים יהיו מיושרים במשטחים המחוברים**. `Gap Distance` · `Upper/Lower Side` ·
`Single Side` (מרותך) · `Upper/Lower Inside` · `Diagonal` (מוברג בצד אחד ומרותך בשני) ·
`Create Group` · `With Bolts`.

---

## העתקה, סיבוב ושיקוף (`B.4`) — הכלים לעיקרון של אמיר

| | |
|---|---|
| **הגבלת כיוון** | `3D` · `2D` (במישור ה-UCS) · `X/Y/Z-Axis` · `Free`. *"prevents points being selected that are not in the proper plane"* — מונע את בדיוק סוג הטעות שעשיתי |
| **`Multiple`** | הזזה/העתקה חוזרת |
| **סיבוב** | `Free Axis` (שתי נקודות) · `Object Axis` (ציר של האלמנט) · זווית · **`Turn+Copy`** |
| **`B.4.6 Rotate`** | העתקה מסובבת עם **היסט אנכי** — לחלוקת מדרגות בגרם לולייני |
| **קבוצות** | פעולה על קבוצה שלמה מבחירת חלק אחד; `ALT` מחליף בין יחיד לקבוצה |

---

## מה עוד לא נקרא (מוצהר במפורש)

`B.9` פלטות · `B.12` שינויים תלת-ממדיים · `B.13` עורך הפלטות · `B.23` גאסטים ·
`B.24/25` איגוד דינמי וסטטי · `B.29` מספור (‏33 עמ') · `B.31–33` רשימות חלקים ·
כל פרק `C` (‏DetailCenter, ‏461 עמ') · `D` שונות כולל `D.5.1` בדיקת התנגשויות.

**נקרא בסבב הזה: ~800 שורות מתוך 30,084.** זה **2.7%**, לא 100%. הסבב הבא ימשיך מ-`B.29`
מספור — כי הוא התברר כתנאי מוקדם ל-Clone Manipulations, ולא רק כשלב פלט.

---

# 📖 סבב 2 — מספור (`B.29`) ובדיקת התנגשויות (`D.5.1`)

*נקרא אחרי שסבב 1 גילה שמספור הוא **תנאי מוקדם** להעברת עיבודים, ולא שלב פלט.*

## מספור — כלי מידול, לא רק פלט

> *"It **searches parts of the same type** and defines their number in the model."*

### חמש רמות מספור נפרדות, כל אחת עם מונה משלה
`Last Single` · `Last Subgroup` · `Last Group` · `Last Assembly` · `Last Connection`.
⚠️ *"you have to enter the value **0** at the beginning, if you want to start counting at 1."*

### שתי סכמות שונות — ואסור לבלבל
`Send Number` — *"the automatic positioning either adds **position numbers** or **shipping
numbers**"*. שתי מערכות מספור לשתי מטרות (ייצור מול משלוח).

### מה קובע ש"זה אותו חלק"
- **`Equal Part Detection`**: *"Parts are considered identical when deviations with respect to
  **dimensions and drill diameter** are within the values given as **reference tolerances**."*
  ⇒ **קוטר הקדיחה הוא קריטריון זהות.** פלטה עם ⌀23 ופלטה עם ⌀28 אינן אותו חלק.
- **`Group Detection`**: *"Groups are identical when **identical single parts are arranged in the
  same mounting position**... single parts are only compared using their **position number**."*
  ⇒ **תלות סדר: קודם ממספרים חלקים בודדים, רק אחר כך קבוצות.**
- יש דיאלוג נפרד לבחירת אילו **תכונות** (שם, הערה...) מבדילות בין חלקים.

### פרטים תפעוליים
- `Type`: `Numerical` · `Alphanumerical` (A..Z, AA, AB) · `Mixed` (A1, A2...).
- `1st/2nd Sorting List` — סדר לפי סוג חלק, ובתוכו לפי אורך → משקל → וכו'.
- **`Flats like`** — *"Flats (which actually are 'shapes') are treated **like any plates** at
  positioning."* ⇒ שוב הבלבול פלטה/פרופיל, **ויש לו הגדרה**.
- `Save Single Part Pos.No.` → המספר הישן נשמר בשדה **`OrigPosnum`**. שימושי לרוויזיות.
- `Use XRefs` — ממספר גם חלקים בהתייחסויות חיצוניות.
- `Connections` מקבלים **ID**, אבל *"only if they have been **named** before"*.
- אפשר להחליף את כל מנוע המספור ב-**PlugIn חיצוני** *"exactly according to the guidelines of
  your company"* ⇒ מסלול אפשרי לתקן המספור של ארץ ברזל.

## בדיקת התנגשויות (`D.5.1`) — יותר ממה שציפיתי

> *"An important task of modern CAD systems is to **avoid errors during construction**...
> In addition this function helps you to check **whether bolts can be mounted or not**."*

- **`Mounting Area`** — נלקח בחשבון מרחב ההתקנה של הבורג (מוגדר בסגנון הבורג).
  ⇒ **זו בדיקת ישימות ייצור**, לא רק חפיפת נפחים. לא ידעתי שקיימת.
- *"The check is made on the base of the parts **volume**"* — נוצרים **גופי התנגשות** גלויים,
  ואפשר לדפדף ביניהם אחד-אחד ולעשות זום.
- **`Min. Volume`** — סף נפח מינימלי, *"avoids that your attention is distracted by collisions
  which are due to minimal tolerances"*.
- ⚠️ *"the required time increases with the **square** of the number of selected parts...
  it is often more reasonable to check only a **certain junction point** at a time."*
  ⇒ **לא להריץ על מודל שלם כברירת מחדל.** לבדוק צומת-צומת. (מתחבר להערה של אמיר על 200
  אובייקטים, ולמדידה שלי שהפעולות מאטות עם גודל המודל.)
- **`At collision check, xref-drawings are evaluated as well`** ⇒ **XREF כן נכללים כאן**,
  בניגוד לחשש שלי. (עדיין לבדוק אם זה נכון לשאר הפעולות.)
- זוגות לבדיקה: פרופיל×(פרופיל/פלטה/בורג/גוף) · פלטה×(פלטה/בורג/גוף) · בורג×(בורג/גוף) ·
  גוף×גוף. `Highest Resolution` להעלאת דיוק זמנית.

---

**נקרא עד כה: ~1,100 שורות מתוך 30,084 = 3.7%.**
הסבב הבא: `B.12` שינויים תלת-ממדיים ו-`B.13` עורך הפלטות — שם נמצאת עיצוב קונטור הפלטה
(הריפים שציירתי ביד), ואחריהם `B.23` גאסטים.
