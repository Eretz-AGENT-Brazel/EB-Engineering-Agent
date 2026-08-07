# 📕 פרק `B.14` — קדיחה וחיבורי ברגים · **נסגר הרמטית**

*‏06/08/2026. נקרא במלואו: שורות 5,632–5,958 (‏326 שורות) = הפתיחה + `B.14.1` + `B.14.2` +
`B.14.3`. עמודים 235–261 במדריך. אין בפרק הזה חלק שלא נקרא.*

**למה דווקא הפרק הזה ראשון:** החורים הם מה שנכשלתי בו הכי הרבה — "304 ברגים ואפס חורים",
פריסה 2×3 במקום 3×2, ‏38 חורים חסרים במבחן 5.

---

## 1 · המודל המנטלי: **שדה חורים**, לא חור

> *"The program manages drill holes in the form of **Drill Hole Fields**. This means that groups
> consisting, for instance, of 2 x 2 holes will be **drilled in one operation**."*

אני קדחתי חור-חור בלולאה. **התוכנה חושבת בשדות.** זו לא נוחות — זה המבנה שהמחברים,
העברת העיבודים והמספור נשענים עליו.

## 2 · תחביר הפריסה — התשובה ל-2×3 מול 3×2

```
Number1*Pitch1, IntermediatePitch1, Number2*Pitch2, IntermediatePitch2, ...
```

| שדה | משמעות |
|---|---|
| **`Shape / X Dir`** | בכיוון הפרופיל · לפלטות = כיוון X של ה-UCS |
| **`Cross / Y Dir`** | ניצב לכיוון הפרופיל · לפלטות = כיוון Y של ה-UCS |

**הדוגמה מהמדריך:**
```
shape / X Dir :  2*60,200,1*,200,3*40
cross / Y Dir :  2*100
```
קריאה: 2 חורים בפסיעה 60 → מרווח 200 → חור אחד (`1*`, אפשר להשמיט פסיעה) → מרווח 200 →
3 חורים בפסיעה 40. ובניצב: 2 חורים בפסיעה 100.

**⚠️ שלושה כללים קשיחים:**
- רק בכיוון האורך ⇒ **להשאיר את שדה הרוחב ריק**.
- רק בכיוון הרוחב ⇒ **חובה לכתוב `1*` באורך.**
- *"One single drill hole field **cannot** cover mixed groups consisting of one and two holes in
  crosswise direction!"* ⇒ פריסה מעורבת דורשת **שני שדות נפרדים**.

### 🔑 `W` — מדי הסימון של הפרופיל
> *"You can enter the **predefined marking gauges of the shape** by typing the letter **W**
> instead of a pitch, e.g. `2*W`."*

**זה מבטל את הצורך להמציא מרווחי ברגים.** לפרופיל יש מדי סימון תקניים והתוכנה יודעת אותם.
אם לא הוגדר מד לחתך — התוכנה תבקש אחד. ⇒ **שורש "המצאה במקום ירושה" נסגר גם כאן.**

### פריסה רדיאלית
`Number` · `Radius` · `Area` (טווח הזווית) · `Start` (זווית התחלה).

## 3 · פרמטרים שלא הכרתי

| פרמטר | מה זה |
|---|---|
| **`Rectangle Hole Axis`** | **אורך ציר החור המלבני** ⇒ **זה החוב הפתוח של חורים אובליים** |
| **`Layout`** | `Drill Through` (מקצה לקצה) · `Drill Blind Hole` (+`Depth`) · **`Weld Crack`** (סימון קטן) |
| **`Flange`** | לקדוח באוגן עליון / תחתון / **בשניהם**. נקבע לפי מיקום החלק וציר ה-Z הנוכחי |
| **`Shape Centre`** | נקודת ההכנסה ניצבת למרכז החתך ⇒ **חורים סימטריים תמיד** |
| **`Offset`** | `Rectangular` (x,y) או **`Polar`** (מרחק+זווית). **בפרופילים ציר ההכנסה הוא הציר האורכי, לא X** |
| **`Create Thread`** | חור הברגה — **ללא `Workloose` כלל**, ומוצג נכון ב-2D וב-3D |
| **`Hole Type`** | `Normal` · `Countersunk` (+`Depth`,`Angle`) · `Step Hole` (+`Depth`, קוטר עליון). *"influences the later 2D-depiction"* |
| `Pitch Lines` / `Centre Lines` | קווי עזר שמוצגים בזמן הקדיחה ונעלמים אחריה |

### 🔴 `Ignore Inner Contours` — קריטי לפרופילים חלולים
> *"the drill hole is **not interrupted at concave chambers**, which occur due to inner contours.
> Thus, it is possible to **drill through a square tube completely**. Otherwise, **only one side
> would be drilled**."*

**מבחן שיעור 4 היה SHS 200×200×8.** בלי הדגל הזה קדיחה בפרופיל חלול קודחת דופן אחת בלבד —
וזה נראה תקין בספירה. **בדיקה חובה בכל RHS/SHS/CHS.**

## 4 · מרווח החור — היכן הוא באמת יושב

- **`Diameter`** — מרשימה שנשמרת ב-**`..\prg\pro_st3d.hdr`** (קובץ קריא). אפשר לאפשר קלט חופשי
  ב-Global Settings.
- **`Addition`** = המרווח בין קוטר הבורג לקוטר החור. **וגם:** *"In the above list, you can also
  define a **Workloose for the bolt diameters**. However, you have to **activate the use in the
  Global Settings / Bolts**."*
  ⇒ יש **טבלת מרווחים לכל קוטר** — כלומר מקום לקודד את הכלל של אמיר (M16→⌀19, M20→⌀23,
  מרווח 3) **פעם אחת**, במקום לשלוח `play=3` בכל קריאה. ברירת המחדל של התוכנה היא 2.

## 5 · 🔴 טבלת מרחקי הקצה — מערכת שלמה שלא ידעתי שקיימת

> *"ProSteel can **automatically verify the admissible edge distance** during drilling...
> You can specify the admissible edge distances of **shapes and plates** for **each hole
> diameter** in a table."*

| | |
|---|---|
| **מבנה הטבלה** | `Dia.` · `Shapes` (מרחק קצה לפרופילים) · `Plates` (לפלטות) |
| **הפעלה** | אוטומטית — רק אם מופעל ב-Global Settings. גם ידנית, על בחירת חלקים |
| **הסימון** | חלק שנכשל **נצבע**; מנקים את הסימון עם **`PS_REGEN`** |
| **עריכת הטבלה** | דורש **מצב `Expert`** ב-Global Settings |

**⚠️ שתי אזהרות מפורשות במדריך:**
1. *"the warning is **only a hint**; the corresponding drill hole will **nevertheless be inserted**."*
   ⇒ **התוכנה לא חוסמת. חור פסול ייווצר.**
2. *"this message might **not appear before end of the action**."*
   ⇒ אי אפשר להסתמך על תזמון האזהרה. **צריך לקרוא את התוצאה, לא לחכות להתראה.**

**החיבור למחקר העיגון:** מרחקי קצה הם בדיוק מה שמחקר העיגון עסק בו (`c_cr,N = 1.5·h_ef`).
כאן יש מקום לקודד את התקן — אבל **בפלדה**, לא בבטון. שני דברים שונים שאסור לבלבל.

## 6 · ארבע דרכים ליצור חורים — ולכל אחת התנהגות אחרת

| # | הפעולה | מה קורה |
|---|---|---|
| 1 | **קדיחה בחלק אחד** | בוחרים חלק, נקדח |
| 2 | **קדיחה בכמה חלקים בו-זמנית** | ⚠️ *"alignment is made according to the **first picked part**"* |
| 3 | **אימוץ חורים מחלק לחלק** | *"first click all shapes **with** holes, then all shapes **to adopt**"*. *"drill holes from a **copied connecting plate** may be rapidly transferred to a shape"*. ⚠️ **זמין רק אם לא נבחר `Bolted Connection`**. **Shift = לבחור חלק מ-XRef** |
| 4 | **חיבור ברגים** (`B.14.2`) | הברגים **יוצרים את החורים בעצמם** |

**זו דרך שלישית להעביר עיבודים** (מעבר ל-Clone Manipulations ול-`TakeoverDrills` ב-API).

## 7 · חיבור ברגים (`B.14.2`) — מתקן מסקנה שגויה שלי

> *"the bolts are automatically inserted and **the necessary drill holes are created**.
> You **don't have to specify in which flanges** this has to be done because this is
> **automatically determined** by ProSteel."*

קבעתי כ"מבוי סתום מדוד" ש"ברגים עם hosts לא קודחים". **נכון ל-`CreateSingleBolt`; שגוי כהכללה.**

| פרמטר / כלל | פירוט |
|---|---|
| **מינימום 2 חלקים** | *"you have to select **at least 2** component parts"* |
| **`Gap`** | *"maximum distance of one contact surface to the other; **if the distance is bigger, no bolted connection will be created**"* ⇒ **מועמד ראשי לכישלון שקט** |
| **`Touch Plane`** | התוכנה **מחפשת משטחי מגע בעצמה**; ה-UCS לא חייב להיות ניצב לכיוון הקדיחה |
| **`Update auto`** | *"a **logical link** is created between the participating parts"* ⇒ עדכון אוטומטי בשינוי חלק |
| **`Single Hole Bolt`** | מותר גם בורג לחלק **אחד** |
| **`Turn`** | הברגים מסובבים לפני ההכנסה |
| **`Length Addition`** | תוספת אורך לברגים |

**⚠️ שלוש התנהגויות שחייבים לדעת:**
1. *"the element selected **first** represents the **main element**. All others are only bolted
   elements... the drill hole fields of the bolted elements **depend on** the main element's and
   **cannot be modified independently**."* ⇒ **סדר הבחירה קובע היררכיה.** לערוך → לערוך את הראשי.
2. *"the bolts are **not assigned to a group**. This assignment has to be made **later** using
   the command 'Group'."* ⇒ **ברגים לא נכנסים לקבוצה מעצמם.**
3. הוספת חלק לחיבור קיים ⇒ **החורים נוצרים אוטומטית**. הסרת חלק ⇒ **החורים נמחקים אוטומטית**.

---

## מה זה משנה אצלי — רשימת פעולה

| # | מה לשנות | מקור |
|---|---|---|
| 1 | לעבור מקדיחת חור-חור ל**שדה חורים** עם התחביר `n*pitch,gap,...` | §2 |
| 2 | להשתמש ב-**`W`** לפסיעות במקום להמציא מספרים | §2 |
| 3 | **`Ignore Inner Contours` בכל פרופיל חלול** — אחרת נקדחת דופן אחת | §3 |
| 4 | לקודד את מרווח 3 מ"מ ב**טבלת הקטרים** במקום `play=3` בכל קריאה | §4 |
| 5 | לקרוא ולהשתמש ב**טבלת מרחקי הקצה** — ולזכור שהיא **לא חוסמת** | §5 |
| 6 | לבדוק את **`Gap`** לפני שמכריזים שחיבור ברגים "נכשל" | §7 |
| 7 | **`Rectangle Hole Axis`** = הפתרון לחורים אובליים (חוב מ-שיעור 2) | §3 |
| 8 | לזכור ש**ברגים לא נכנסים לקבוצה** — להוסיף אותם במפורש | §7 |

## מה נשאר פתוח בפרק הזה
- ⚠️ האם `PsDrillObject.SetLinearHoleField(dia, XField, YField)` מקבל את **אותו תחביר מחרוזת**
  (`2*60,200,1*`) — הפרמטרים הם `String XField, String YField`, מה שמאוד מרמז שכן. **לבדוק בניסוי.**
- ⚠️ איפה בדיוק יושבת טבלת מרחקי הקצה, והאם היא נגישה ב-API.
- ⚠️ האם `W` נתמך גם דרך ה-API או רק בדיאלוג.

---

## ✅ נסגר במדידה (06/08) — חורים אובליים, החוב מ**שיעור 2**

### מה שהיה שגוי בקוד שלי
```csharp
if (slot > 0) { d.SetHoleStep(slot, dia); }     // ✗ SetHoleStep = חור מדרגה, לא אובלי
```
`SetHoleStep` הוא `Step Hole` מ-`B.14.3` (עומק מדרגה + קוטר עליון). **דבר אחר לגמרי.**

### מה שנכון
`PsDrillObject` **אין לו מתודה לאורך החריץ** — הרשימה המלאה נבדקה. אבל יש
**`SetAxisDistance(Double)`**, והמדריך אומר על `Rectangle Hole Axis`:
*"you enter the **length of the rectangle hole axis** here."*

```csharp
if (slot > 0) {
    d.SetAxisDistance(slot);            // = Rectangle Hole Axis
    d.SetRotateSlottedHoles(rotate);    // כיוון החריץ
}
```

### האימות — שתי פלטות זהות, חור עגול מול אובלי
```
ROUND    כל המצבים :  חור אחד ב-x=2000                    slotted=False
SLOTTED  lhm=0      :  חור אחד ב-x=2600 (המרכז)            slotted=0   ← מטעה!
         lhm=1      :  חור אחד ב-x=2580                    slotted=1
         lhm=2      :  שני חורים ב-2580 ו-2620             slotted=1
                       2620 − 2580 = 40  ←  בדיוק האורך שנשלח
```

### 🔴 `LongHoleMode` הוא מצב **קריאה**, לא כתיבה
זו הסיבה שהוא ארגומנט ב-**constructor** של `PsSingleHoleArray` ולא setter על הקדיחה:

| מצב | איך החריץ מדווח |
|---|---|
| `kSingleHole=0` | **חור אחד במרכז, ודגל slotted=0** ⇒ **עיוורון מוחלט לחריצים** |
| `kLongHole=1` | חור אחד בקצה, דגל 1 |
| **`kDoubleHole=2`** | **שני הקצוות** ⇒ **המצב היחיד שחושף את האורך** |

**‏`getMaximalLength` מחזיר 0 בכל המצבים — הוא אינו המכשיר.** האורך = המרחק בין שני
החורים ב-`lhm=2`.

### הבאג שזה חשף במכשיר האימות שלי
`op=holes` ו-`op=dumpholes` השתמשו ב-**`lhm=0` כברירת מחדל** ⇒ **לא היו מזהים חור אובלי
לעולם.** גם אם הייתי מייצר אותם נכון, הייתי מדווח "אין".
**תוקן ל-`lhm=2`** (v44). אימות על אותו מודל: `slotted=0` לפני, **`slotted=2` אחרי.**

⇒ **הלקח החוזר: מכשיר מדידה חדש נבדק מול מצב שאני כבר יודע מה בו.** זו הפעם הרביעית היום.

### פרמטרים נוספים מ-`B.14` שיושמו באותה גרסה
`innercontour=1` → `SetIgnoreInnerContour` (**פרופיל חלול נקדח בשתי הדפנות**) ·
`play=` → `SetHoleWorkloose` · `flange=0|1|2` → `SetDrillType` (עליון/תחתון/**שניהם**) ·
`bolttype=` → `SetHoleBoltType` (**בורג מפעל מול בורג הרכבה באתר**).
