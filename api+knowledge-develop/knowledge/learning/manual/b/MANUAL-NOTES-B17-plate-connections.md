# 📕 פרק `B.17` — מחברי פלטות · **נסגר הרמטית**

*‏06/08/2026. נקרא במלואו: שורות 6,464–6,910 (‏446 שורות) = הפתיחה + `B.17.1` (שני חלקים) +
`B.17.2` + `B.17.3`. עמודים 269–286. אין בפרק חלק שלא נקרא.*

**למה הפרק הזה:** הוא הליבה של שיעור 6, והוא מפענח את **132 הפרמטרים של
`PsStandardPlateLinkData`** שקודם היו שמות חסרי משמעות.

---

## 1 · העיקרון: מחבר אחד בפקודה אחת

> *"ProSteel automatically creates any plate connections such as stiff or rigid plates according
> to **DAST**, jointed plates, etc... In addition to the actual connection plate other components
> such as **stiffeners, backer plates, bolts and even reinforcement haunches** can be created with
> this command and **assigned to the proper component groups directly**.
> An entire plate connection can be created with **just one single command**."*

⇒ פלטה + מחזקים + פלטות גב + ברגים + הנצים + **שיוך לקבוצות** — הכל בפעולה אחת.

### סדר הבחירה (מאשר את `op=conn`)
> *"you are first prompted for **the shape to be connected**, which you have to click **at the end
> to be connected**. Then you have to click **the supporting shape** — or ignore it with RETURN...
> The shape to be connected is **cut to the proper length** and is fitted with the connection plate.
> The plate and supporting shape are **drilled and bolted together**."*

- ‏`SetConnectionObjectId` = הנתמך · `SetSupportObjectId` = התומך. **הסדר נכון.**
- **בלי תומך** ⇒ פלטת קצה פשוטה בקצה הקורה.
- ⚠️ **המחבר חותך את הקורה לאורך.** תופעת לוואי, כמו `ShortenShape` בפלטת בסיס. **למדוד אורך אחרי.**

## 2 · בחירת סוג הפלטה — כלל ההחלטה של התוכנה

> *"If you selected **'Automatic'**, the program decides which type to be used depending on the
> position of the shapes. **The critical angle, which differentiates between spliced and standard
> plate connection, is approximately 45°.** If you want to obtain the **'Flange'** type, you have
> to set this option **explicitly** because otherwise **always a plate connection at the web** will
> be created."*

⇒ ברירת המחדל היא **חיבור לגוף**. חיבור לאוגן חייב להיאמר במפורש.
פקודות ישירות: `PS_ENDPLATE_NORM` · `PS_ENDPLATE_SPLICE` · `PS_ENDPLATE_FLANGE`.

## 3 · מידות הפלטה — שתי מוסכמות שחייבים לדעת

| פרמטר | משמעות מדויקת |
|---|---|
| **`Width`** | ב-I: **מקביל לאוגן** |
| **`Length`** | ב-I: מקביל לגוף. **`Length = 0` ⇒ האורך הופך משתנה** ונגזר מ-`Offset Top`/`Offset Bottom` |
| **`Offset Top/Bottom`** | מרחק מקצה החתך. **חיובי מקטין** את הפלטה פנימה · **שלילי מגדיל** אותה מעבר לחתך |
| **`Gap`** | *"space between the supporting shape and the plate... to consider e.g. **finishing tolerances**"* ⇒ **זה ה-18.5 מ"מ שראיתי במודל הדוגמה של Bentley** |
| **`Plate Offset`** | `Horizontal` = הזזה מקבילה לאוגן · `Vertical` = מקבילה לגוף |
| **`Doubler Plate`** + **`Equal Plates`** | פלטה שנייה; `Equal` ⇒ זהה לראשונה |
| **`As Poly-Plate`** | פלטות במקום שטוחים |

### 🔑 `Rotate Connection` — התיקון לאוריינטציה הפוכה
> *"In case of asymmetric plates you can define the plate position here. Use this option to
> **turn the complete connection by 180° around the insertion axis, if upper and lower side were
> exchanged** at generation of the connection."*

**זו מחלקת השגיאות שנפלתי בה פעמיים** (אוגן במקום הלא נכון). יש לה פרמטר ייעודי, לא צריך
למחוק ולבנות מחדש.

## 4 · סמנטיקת פריסת החורים — לא אינטואיטיבית

### אנכית (לאורך)
| שדה | משמעות |
|---|---|
| `Upside` | מרחק שורת החורים העליונה מקצה הפלטה העליון |
| `Middle` | מרחק בין שורה ראשונה לשנייה. **אם `Middle = 0` ⇒ החורים מתחלקים אחיד בין שני הקיצוניים** |
| `Downside` | מרחק השורה התחתונה מקצה הפלטה התחתון |
| **כלל** | **אם `Upside = 0` וגם `Downside = 0` ⇒ רק `Middle` נחשב** |
| `Offset` | הזזת **כל** השורות ביחס לקצה העליון. **שלילי ⇒ ביחס לקצה התחתון** |
| `Asymmetrical` | כל מרחק נקבע בנפרד מרשימה · `Measured from` = מאיפה מתחילה החלוקה |

### אופקית (לרוחב)
`Left` / `Right` — משמעותיים **רק כשיש 4 שורות** · `Middle` — בין שתי השורות הפנימיות ·
`Offset` — ביחס לקצה **הימני**, שלילי ⇒ ביחס לשמאלי.

`Without Holes` — פלטה ללא חורים בכלל.

**`Workloose`** — *"in most cases **2 mm**"*. **הכלל של אמיר הוא 3.** שוב.

## 5 · 🔴 `B.17.2` — מחברי DAST נבחרים **לפי העומס**

> *"**Tension, Shear...** Here, you can enter a **maximum load for the connection (in kN)** for the
> corresponding load type. **In the list, only the connections suitable for these loads will be
> displayed.**"*

**מה כל שורה ברשימה מציגה:**
`Designation` (ייעוד תקני) · `Width` · `Thickness` · `Length` · `Horizontal`/`Vertical`
(מספרי חורים) · `Diameter` · `Bolt Standard` · **`Stiffeners`** (האם נדרשים) ·
**`Backer`** · **`Strengthening`** · `Projection` · **`Af`** (עובי ריתוך באוגן) ·
**`As`** (עובי ריתוך בגוף) · `Tension, Shear...` (העומס המרבי).

**זה בדיוק כלי ה-value engineering שאמיר תיאר** — מזינים את הכוח האמיתי ומקבלים את המחבר
הקל ביותר שנושא אותו, **עם ייעוד תקני לצטט מול קונסטרוקטור**.
⚠️ **אבל זה שלב 2 (בדיקת תכן) שנעול.** לדעת שהכלי קיים = שלב 1. להשתמש בו לפסיקה = לא עכשיו.

**מצב כישלון:** *"If — according to the guidelines — **no connection is defined** for the selected
shapes, **no dialog tab will appear**... you have to define yourself the dimensions."*
⇒ העדר דיאלוג אינו תקלה, הוא תשובה.

## 6 · `B.17.3` — מסד נתונים למחברים של החברה

> *"you can create a **database containing user-defined plate connections**... You can also process
> or export the data with **any standard dBASE editor**.
> **HINT:** create a database with frequently utilized and maybe **company-specific connections**,
> which are then **always available to all program users within your company**."*

⇒ **המסלול לקידוד תקן המחברים של ארץ ברזל.** פורמט dBASE, ובאפי יש **`PsDBaseDatabase`**.
היתרון על תבניות: *"can display a larger amount of data in a more clearly organized structure
because **all parameters are visible in the view**"*.

## 7 · רכיבים נלווים — מה מותר להוסיף למחבר

| רכיב | הערות |
|---|---|
| **`Backer Plates`** | אוטומטי או במידות מוגדרות (עובי/רוחב/אורך בנפרד) |
| **`Top Plate`** | פלטת כיסוי, `According to Girder` אוטומטי; עובי, מרחק קצה, היסט |
| **`Web Plates`** | שמאל/ימין; מידות + מספר וקוטר חורים + מרווח + היסט אופקי/אנכי ממרכז הפלטה |
| **`Stiffener`** | בתומך, בגובה האוגנים |
| **`Inner Stiffeners`** | נוספים בין הקיימים; מספר + מרחק מקצה עליון/תחתון |
| **`Support Stiffeners`** | ריפים **אלכסוניים** בתומך. ⚠️ **תנאי מוקדם:** *"the use of a **bottom flange haunch** having a stiffener on the side of the bottom flange haunch **and a cover plate**"* — בלי זה לא ייווצרו |

### ⚠️ תבניות ריפים הן **קישור חי**
> *"the settings for stiffeners **cannot be made independently here**. You only **select a template**...
> you mustn't forget that **if modifying this page after having modified this stiffener template,
> the existing stiffeners are updated as well** (they will obtain the dimensions of the modified
> template)."*

**עריכת תבנית משנה רטרואקטיבית ריפים קיימים במודל.** סכנה אמיתית בפרויקט חי.

## 8 · הנצים (Bottom Flange Haunch) — ופענוח 132 הפרמטרים

| שם בדיאלוג | הפרמטר ב-`PsStandardPlateLinkData` |
|---|---|
| `Haunch Length` | `HaunchLength` / `TopHaunchLength` / `DownHaunchLength` |
| `Cut Width` (ברוחב החיתוך) | `HaunchCopedHeight` |
| `Top Height` | `TopHaunchCopedHeight` |
| `Flange Width` / `Flange Thickness` | `HaunchFlangeWidth` / `HaunchFlangeThickness` |
| `Plate Thickness` (גוף ההנץ) | `HaunchWebThickness` |
| **`Facet Horizontal`** | **`TopHaunchPlateFacetDistance1`** |
| **`Facet Vertical`** | **`TopHaunchPlateFacetDistance2`** |
| `Facet Size` | `TopHaunchPlateFacetDistance` |
| **`Cropped Shape`** | **`HaunchIsCopedShape`** |
| `Rectangular Plate` | `TopHaunchPlateIsRectangle` |
| **`Perpendicular to Support`** | **`TopHaunchPlateIsNormalToSupport`** |
| `As Poly-Plate` | `TopHaunchPlateIsPoly` |
| `Rib Support` / `Rib Connection` | `HaunchStiffenerAtSupport` / `HaunchStiffenerAtConnected` |

**‏132 הפרמטרים היו שמות חסרי משמעות. עכשיו הם קריאים.** לא הייתי מנחש
ש-`FacetDistance1` הוא החלק **האופקי** של הקיטום.

## 9 · קבוצות והתראות

- **`Create Group`** — *"at insertion of the end plate connection, **groups are automatically
  created** out of the inserted parts"*, ועם **`With Bolts`** ו-**`With Welds`**.
  ⇒ **סותר את `B.14.2`**, שם בחיבור ברגים חופשי *"the bolts are **not** assigned to a group"*.
  **במחבר — כן. בחיבור ברגים חופשי — לא.** הבדל אמיתי בין שני המסלולים.
- **`Safety Copes`** — *"a **standard process in Northern America**"*; ‏4 מיקומים
  (עליון/תחתון × שמאל/ימין).
- ריתוכים: `Weld Style` + `Weld Flange Side` / `Weld Web Side`, עובי לכל אחד בנפרד.

---

## רשימת פעולה מהפרק

| # | מה | מקור |
|---|---|---|
| 1 | להוסיף **`Rotate Connection`** ל-`op=conn` — התיקון לאוריינטציה הפוכה | §3 |
| 2 | לשלוח `play` תמיד; ברירת המחדל 2 והכלל של אמיר 3 | §4 |
| 3 | לזכור ש-**`Length=0` ⇒ אורך משתנה** מ-`Offset Top/Bottom` | §3 |
| 4 | **למדוד את אורך הקורה אחרי כל מחבר** — הוא נחתך | §1 |
| 5 | להשתמש ב-**`Create Group`+`With Bolts`+`With Welds`** בכל מחבר | §9 |
| 6 | לחיבור לאוגן — **לציין במפורש**, אחרת תמיד לגוף | §2 |
| 7 | לבדוק את **`Support Stiffeners`** מול התנאי המוקדם שלהם | §7 |

## מה נשאר פתוח
- ⚠️ האם רשימת DAST נגישה ב-API (חיפוש מחבר לפי עומס) — **שלב 2, לא לגעת עכשיו**.
- ⚠️ מיקום מסד ה-dBASE של המחברים המוגדרים ונגישותו דרך `PsDBaseDatabase`.
- ⚠️ האם `Rotate Connection` חשוף כפרמטר ב-`PsStandardPlateLinkData` (`PlateIsRotated`?).

---

## 🔑 התובנה הגדולה של הפרק: **המדריך הוא תיעוד ה-API**

אחרי הקריאה בדקתי כל שדה בדיאלוג מול `PsStandardPlateLinkData`. **ההתאמה היא אחד-לאחד:**

| השדה בדיאלוג (מדריך) | התכונה ב-API |
|---|---|
| `Create Group` | `CreateGroup` |
| `With Bolts` / `With Welds` | `AddBoltsToGroup` / `AddWeldsToGroup` |
| `Gap` | **`DistanceToSupport`** |
| **`Rotate Connection`** | **`PlateIsRotated`** ✅ |
| `Backer Plates` | `BackerPlate` · `BackerPlateIsAuto` · `…Width/Length/Thickness` · `…IsPoly` |
| `Top Plate` | `TopPlate` · `TopPlateIsAuto` · `TopPlateThickness` · `TopPlateEdgeOffset` · `TopPlateOffset` |
| `Web Plates` left/right | `WebPlateLeft` / `WebPlateRight` · `WebPlateWidth/Length/Thickness` |
| חורי פלטת הגוף | `WebPlateHoleDiameter` · `WebPlateHoleSpacing` · `WebPlateVerticalHoleCount` |
| `Stiffener` | `WithStiffeners` |
| ריתוכים | `WeldSeamFlange` · `WeldSeamWeb` · `WeldToFlange` · `WeldToWeb` · `WeldStyleCRC` |
| זווית הפלטה | `PlateAngle` · `PlateAngleType` |

**המסקנה שמשנה את כלכלת הלמידה:**
**אין צורך בשני מקורות — המדריך מלמד את ה-API.** כל שדה שאני קורא עליו הוא פרמטר שאפשר
לשלוח מהקוד, וכל הסבר על התנהגות (מה קורה כש-`Middle=0`, מתי `Left/Right` משמעותיים, מה
התנאי המוקדם ל-`Support Stiffeners`) הוא **הסמנטיקה של אותו פרמטר** — מידע שרפלקציה
לעולם לא תיתן.

⇒ **כל 1,179 העמודים רלוונטיים, לא רק כמדריך למשתמש.**

---

## ‏AUDIT 10/08/2026 — שניים משלושת הפריטים נסגרו

### ⭐⭐ B.17.3 — מסד המחברים של החברה: נמצא, נקרא, וממופה

הם יושבים ב-`Prg/Plugins/`, אחד לכל מאקרו, ו-`PsDBaseDatabase` קורא אותם. ‏op חדש **`dbase`**:

```
dbase file=…\Plugins\BasePlate\BasePlate.dbf
  -> records=56 fields=11
     SHAPE CODE LENGTH WIDTH THICKNESS DIAMETER WORKLOOSE HOLEX HOLEY AF AS
```

| מאקרו | רשומות | שדות |
|---|---:|---:|
| `AECChute` | 59 | 19 |
| `BasePlate` / `BasePlateChinese` | 56 | 11 |
| `BeamBeamClamp` | 5 | 2 |
| `PipeStrap` | 33 | 9 |
| `PurlinBeamBraceFly` | 60 | 19 |

⭐⭐ **והם מדברים את השפה של B.14.** ‏`HOLEX` מכיל **`2*100`** ו-`HOLEY` מכיל **`1*`** — *בדיוק
מחרוזת פריסת הקידוח* ש-`drillfield x= y=` מקבל. **מסדי המחברים וה-API של הקידוח הם אוצר מילים
אחד**, וזה מה שהופך את קידוד מחברי ארץ ברזל למעשי.

⚠️ **`dbase` הוא קריאה בלבד במכוון.** ל-`PsDBaseDatabase` יש `PutRecord` ו-`AppendNewRecord`,
אבל הקבצים יושבים ב-`Program Files` ומגדירים איך מחברים נבנים. כתיבה היא החלטה של אמיר.
המפה: `knowledge/CONNECTION-DATABASES.md`.

### ‏`Rotate Connection` — חשוף, ניתן לכתיבה, וחסר השפעה

כן, זה **`PlateIsRotated`** על `PsStandardPlateLinkData`. אבל המסלול היחיד אליו היה **`connset`,
שנמצא בהסגר** אחרי שהפיל את AutoCAD ארבע פעמים והשאיר פעם שרטוט שלא ניתן לשמירה. **ההסגר
נשאר**; במקומו הבוליאני נוסף ל-op הבטוח `conn`.

```
conn kind=endplate … rotated=1   ->  create=True … PlateIsRotated=True
```

התכונה **נכתבת ונקראת חזרה כ-True**, והמחבר **זהה לחלוטין** — אותן שמונה פלטות בשני המקרים.

⇒ ⭐ **עוד "פרמטר שלא מגיע"** — אותה חתימה כמו פלטת בסיס / פלטת קצה / מרזב בפרק הזה עצמו.
**נקבע, אושר, נעלם.**

### נשאר פתוח במכוון
**בורר המחברים של DAST לפי עומס — שלב 2, נעול.** לדעת שהכלי קיים זה שלב 1; להשתמש בו לפסיקה — לא.
