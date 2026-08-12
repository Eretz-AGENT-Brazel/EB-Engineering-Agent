# 📕 פרק `B.18` — פלטות בסיס לפי DSTV · **נסגר הרמטית**

*‏06/08/2026. נקרא במלואו: שורות 6,910–7,040 (‏130 שורות). עמודים 287–302.*

**למה הפרק הזה:** זה הפרט שבניתי בשיעורים 4 ו-5 — **ומעולם לא קראתי עליו.** הוא מסביר
שלושה דברים שלא הבנתי, ומתקן מסקנה שגויה שהסקתי ממדידה.

---

## 1 · 🔴 הקיצור — נמדד, ושתי הגרסאות היו חלקיות

| מקור | הטענה |
|---|---|
| המדידה שלי בשיעור 4 | המאקרו מקצר את העמוד ב**עובי הפלטה** (20) |
| המדריך `B.18` | *"`Shorten Column` — the column is shortened by the **lining** value"* |

**המדידה שהכריעה (06/08, `connbase` על HE300B באורך 4000):**

| `grout` | עובי פלטה | אורך לפני → אחרי | **קוצר ב-** |
|---|---|---|---|
| **0** | 20 | 4000 → 3980 | **20** |
| **35** | 20 | 4000 → 3945 | **55** |

### ✅ הכלל האמיתי
```
קיצור העמוד  =  עובי הפלטה  +  עובי ה-grout
```
**המדידה שלי הייתה נכונה למקרה אחד (grout=0) והכללתי ממנה.** בדיוק הכשל שתועד ב-
`per-project-not-universal`. **והמדריך לבדו גם לא היה מספיק** — "lining value" דו-משמעי.
**רק שילוב של השניים נתן את התשובה.**

**ולמה זה הגיוני:** ראש העמוד חייב להישאר במפלס התכנוני, אז העמוד מתקצר בכל מה שמתחתיו —
הפלטה **וגם** מצע ה-grout. ⇒ **זה אורך המסור בבית המלאכה.**

## 2 · 🔴 למה אורך העוגן "גרפי בלבד" — זו לא בחירה של אמיר

> **`Tie Bolts`** — *"Anchor bolts are **only displayed as symbols and cannot be detailed**."*

אמיר אמר בשיעור 4: *"אני משאיר את האורך שלך באופן גרפי בלבד"*, ואני רשמתי את זה כהעדפה
שלו. **זו מגבלה של התוכנה, לא העדפה.** ‏`Tie Bolts` מייצר **סמלים** שלא נכנסים לפירוט.

**⇒ לא לרדוף אחרי אורך של `Tie Bolt`. אין מה לתקן שם.**

## 3 · 🔴 `Use Dowel` — המסלול לעוגנים אמיתיים

> *"**Use Dowel** — Dowel elements are created as **volume bodies**...
> **Input Field** — you can specify a **database file** from which the dowel definitions can be
> taken... **In Bolt Partlist** — the dowels are taken over into the bolt part list...
> **No Detailing** — the dowels are not taken over into the DetailCenter."*

**שלוש התאמות למה שמדדתי:**
1. *"created as **volume bodies**"* ⇒ העוגנים במודל של אמיר הם **`Ks_VolBody`** — בדיוק מה שמדדתי.
2. **Dübel = dowel.** הקובץ `Data\Bolts\Duebel.mdb` שמצאתי הוא **מסד הנתונים הזה**.
3. *"you can **specify a database file**"* ⇒ **צריך לציין מסד.** זה כנראה למה `PsCreateFastener`
   החזיר אפס אובייקטים בכל שם סגנון שניסיתי.

> ⏸️ **פריט דחוי במפורש** — אמיר: *"תעבור למחברי קורות, אל תתעכב על זה."* רשום כאן כדי שלא
> יאבד, לא כדי לרדוף אחריו עכשיו.

בנוסף: **`Label`** — שם העוגן לרשימת החלקים, עם המשתנים **`$(ID)`** (קוטר פנימי) ו-**`$(OD)`**
(קוטר חיצוני).

## 4 · שני שדות חורים נפרדים — פנימי וחיצוני

| | |
|---|---|
| **פנימי** | `Hole Distance` לציר X · `Hole Distance` לציר Y · `Hole Diameter`. זה ה-DAST הסטנדרטי (2 חורים; מרחק נוסף ⇒ 4) |
| **חיצוני** | `Hole Field Width` = **מספר + פסיעה** בציר X · `Hole Field Height` = **מספר + פסיעה** בציר Y · קוטר נפרד |

⚠️ **תנאי כישלון שקט:** *"Outer drill holes are **only created if BOTH axes have a valid
description**."* ⇒ תיאור חלקי = אפס חורים חיצוניים, בלי הודעה.

**ההתאמה ל-API** (`PsBaseplateLinkDataMgd`): `CreateInnerHoles` / `CreateOuterHoles` ·
`HoleDistanceHorizontalInner` / `…VerticalInner` · `HoleCountHorizontalOuter` /
`HoleCountVerticalOuter` · `HoleDistanceHorizontalOuter` / `…VerticalOuter` ·
`HoleDiameterInner` / `HoleDiameterOuter`.
**עכשיו ברור למה יש `Inner` ו-`Outer` בשמות** — אלה שני שדות שונים, לא וריאציות.

**וגם:** `Tie Bolts` = עוגנים ל**חורים הפנימיים**. **`...Outside`** = גם לשדה החיצוני
(`AnchorBoltsOutside` ב-API).

## 5 · שאר הפרמטרים

| פרמטר | משמעות |
|---|---|
| **`Grout Thickness`** | *"additional space between the 'Base Plate' (point of reference) and lower edge base plate. Once you have entered the supporting shape, **all length dimensions will be adjusted correctly**"* ⇒ `LiningThickness` ב-API |
| **`In Shape Direction`** | *"the plate is always attached **normally beneath the shape**. If the shape is slanted in the space, **the plate is slanted as well**"* ⇒ `PlateInShapeDirection` — **זה הפרמטר שטעיתי בו באוריינטציה** |
| **`Form Group`** | *"the shape and the plate are arranged to form a **group**"* ⇒ `CreateGroup`. **זה מה שהיה מונע מהפלטה להישאר מאחור בסיבוב** |
| `As Polyplate` | פלטה במקום שטוח ⇒ `BasePlateIsPolyPlate` |
| ריתוכים | `Flange Side` / `Web Side`, כל אחד עם עובי שדורס את הסגנון ⇒ `WeldToFlange`/`WeldToWeb` + `WeldSeamFlange`/`WeldSeamWeb` |

## 6 · `Standard Definitions` — בסיס לפי עומס

> *"Data base entries are saved for certain **DIN shapes**... **Support Load** — enter the minimum
> load of supports which is known. **The program searches the entries supporting this load or a
> higher load.** **Hole Dia.** · **Concrete Quality** — quality of concrete foundations."*

בוחרים רשומה בלחיצה כפולה. ⚠️ **אבל:** *"please note that **only the plate dimensions and the
inner drill holes** are set. **Other settings are not modified.**"*
⇒ בחירה מהמסד **לא** מגדירה עוגנים, ריתוכים או שדה חיצוני. אלה נשארים כפי שהיו.

⚠️ *"the data concerning the load to be transmitted refer to **vertical supports** and connections
according to DAST"* — לא תקף לעמוד משופע.

**זה שוב כלי value engineering** (עומס → פלטה מינימלית מתאימה, כולל **איכות הבטון**) —
ושוב **שלב 2 שנעול**. לדעת שקיים = מותר. לפסוק לפיו = לא עכשיו.

---

## רשימת פעולה מהפרק

| # | מה | מקור |
|---|---|---|
| 1 | **קיצור = עובי פלטה + grout.** לתקן בכל מקום שרשמתי "עובי הפלטה" | §1 |
| 2 | **לא לרדוף אחרי אורך `Tie Bolt`** — הוא סמל שלא ניתן לפירוט | §2 |
| 3 | **`Form Group` בכל פלטת בסיס** — הפלטה חייבת להיות בקבוצה עם העמוד | §5 |
| 4 | **`In Shape Direction`** הוא פרמטר האוריינטציה. לשלוח במפורש | §5 |
| 5 | שדה חיצוני דורש **שני הצירים** מתוארים, אחרת אפס חורים בשקט | §4 |
| 6 | ‏`Use Dowel` + מסד נתונים = המסלול לעוגנים אמיתיים (**דחוי**) | §3 |

## מה נשאר פתוח
- ⚠️ מבנה `Duebel.mdb` והדרך לציין אותו כ-`Input Field` דרך ה-API. **דחוי לפי הוראת אמיר.**
- ⚠️ האם `Standard Definitions` (חיפוש לפי עומס) נגיש ב-API. **שלב 2 — לא לגעת.**
- ⚠️ ‏`connbase` מחזיר `host_holes=0 anchors_with_body=0` גם כשהוא מדווח `create=True` —
  עקבי עם §2 ו-§3, אבל **החורים בעמוד** הם עניין נפרד שצריך בדיקה.

---

## ‏AUDIT 10/08/2026 — הפריט השלישי נמדד ו**לא** נסגר

שני הפריטים האחרים לא נגעתי בהם בכוונה: מבנה `Duebel.mdb` **נדחה ע"י אמיר** (*"תעבור למחברי
קורות, אל תתעכב על זה"*), ו-`Standard Definitions` הוא בחירה לפי עומס — **שלב 2, נעול**.

הרצה על HE 300 B טרי עם `template=default/Standard`:

```
connbase host=15CA create=True host_holes=0 anchors_with_body=103 anchor_bbox=50x0x0
חורים בעמוד אחרי         : 0
פלטות בטביעת הרגל (±600) : 0
Ks_VolBody בטביעת הרגל   : 0
```

⚠️ **שלושה מספרים שלא מתיישבים.** ה-op מדווח 103 עוגנים עם גוף, בעוד סריקה עצמאית של אותה
טביעת רגל לא מוצאת **לא פלטה ולא גוף**, ו-`anchor_bbox=50x0x0` מנוון. ‏`anchors_with_body=103`
נראה כמו ספירה של כל המודל ולא של המחבר הזה.

⇒ **לא נסגר, ולא נכתב כאילו כן.** הצעד הבא מוגדר: **לברר מה `anchors_with_body` באמת סופר**
לפני שסומכים על דוח של פלטת בסיס — זה המספר שאמור להגיד אם כלל הברזל מתקיים.

⚠️ **ושאלת כלל הברזל עצמה פתוחה:** עוגן מחבר את **הפלטה** לבטון, ולכן `host_holes=0` על
ה**עמוד** אולי נכון — עוגן לא עובר דרך עמוד. **הפלטה היא האלמנט שחייב לשאת את החורים**, וההרצה
הזאת לא ייצרה פלטה שהסריקה מצאה.
