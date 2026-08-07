# 📕 פרק `B.13` — עורך הפלטות · **נסגר הרמטית**

*‏06/08/2026. נקרא במלואו: שורות 5,473–5,632 (‏159 שורות) = הפתיחה + `B.13.1` + `B.13.2` +
`B.13.3`. עמודים 226–234.*

**למה הפרק הזה:** בשיעור 3 "עיצבתי" **214 ריפים** בהחלפת פוליגון (`SetPolygon`), כי לא ידעתי
שיש דרך אחרת. הפרק הזה הוא הדרך האחרת.

---

## 1 · מה עורך הפלטות נותן שהמניפולציה הרגילה לא

> *"All commands can also be carried out via the normal manipulation. The use of the plate editor
> is **recommended when poly-plates have to be processed in complex situations** because it can
> **hide the other component parts** and it **automatically enters the plate level**."*

⇒ שני יתרונות: הסתרת שאר החלקים, וכניסה אוטומטית למישור הפלטה.
`Hide Parts` — ⚠️ *"valid **from the next selection onward**"*, לא מיידי.

## 2 · `B.13.1` פעולות בוליאניות על הקונטור

| פרמטר | משמעות |
|---|---|
| **`Boolean Operation`** | `Add` · `Subtract` · `Common` |
| **`Side`** | מאיזה צד של הפלטה מתבצע העיבוד |
| **`Contour`** | הפוליגון כ**גוף מלא** או כ**קו קונטור**. ⚠️ *"the poly-line **has to be closed**, even if it is used as contour"* |
| **`Distance`** | היסט ביחס לקונטור (כשמשמש כקונטור מלא) |
| **`Milling Width`** | רוחב הקונטור, אם הוא קונטור **כרסום** |
| **`Depth`** | עומק העיבוד, אם הפוליגון **אינו** רציף לכל העובי |
| **`Continued`** | הפוליגון משמש כגוף בעובי הפלטה — כלומר **רציף לכל העומק** |

בנוסף: הגדרת קונטור לפי **נקודות**, או לפי **בחירת פוליליין / מעגל / קשת**;
ו**הוספה/מחיקה של צלע** לפלטה.

⇒ **זו הדרך הנכונה לעצב קונטור**, במקום להחליף את כל הפוליגון ב-`SetPolygon`.

## 3 · 🔑 `B.13.2` קיטום ועיגול — התשובה לריפים

### קיטום פינה (Chamfer)
| שדה | ערך |
|---|---|
| **`Layout`** | **`straight` · `convex` · `concave`** |
| **`Radius / 1st Edge`** | הרדיוס (קמור/קעור) **או** אורך הצלע הראשונה (ישר) |
| **`2nd Edge`** | אורך הצלע השנייה בקיטום ישר |

**הקיטום של אמיר — 80×80 על ריפ 120×120 — הוא פשוט:**
`Layout = straight` · `1st Edge = 80` · `2nd Edge = 80`.
**לא פוליגון מצויר. שלושה פרמטרים.**

וזה מסביר את שמות תבניות הריפים שמניתי (`op=conntemplates`):
`half convex` · `half rounded` · `full rounded` · `full convex` · `full champfered` ·
`half chamfered` — **הם בדיוק `Layout` × (חצי/מלא).**

### עיגול צלע (Rounding Off)
מוגדר לפי **`Radius`** או לפי **`Height`** (הגובה מעל הצלע הישרה).
⚠️ **התוכנה מציגה את הגבולות:** `Min. Radius` ו-`Max. Height` — כלומר **אין צורך לנחש
מה אפשרי**, היא אומרת.

## 4 · 🔑 `B.13.3` עיבוד צלעות — ופענוח `PsEdgeChamfer`

> *"The edges can be **chamfered, rounded off, been equipped with a radius or seamed**...
> **six different kinds** of edge processing are available."*

| שדה | משמעות מדויקת |
|---|---|
| **`Layout`** | סוג העיבוד — **שישה סוגים** |
| **`Top Side` / `Bottom Side`** | העיבוד מתבצע בצד העליון / התחתון של הפלטה, **בנפרד** |
| **`Var1`** | *"either the **length of the first edge**, the **rounding radius**, or the **depth of the seam**"* |
| **`Var2`** | *"either the **length of the second edge** or the **height of the seam**"* |
| **`Selected Edge`** | *"the edges are displayed in a **numbered** way... **if the starting value is equal to the end value, the processing will be carried out ALL AROUND**. Select e.g. **`0-1, 2-3`** for the two opposite sides."* |

### ההתאמה ל-`PsEdgeChamfer` — עכשיו קריא
| המדריך | ה-API |
|---|---|
| `Layout` | `EdgeLayout` |
| `Top Side` / `Bottom Side` | `Topside` / `Downside` · `TopEdgeLayout` / `DownEdgeLayout` |
| `Var1` (צלע ראשונה / רדיוס / עומק תפר) | **`TopVar1`** / **`DownVar1`** |
| `Var2` (צלע שנייה / גובה תפר) | **`TopVar2`** / **`DownVar2`** |
| האוגן שעליו מתבצע | `FlangeIndex` |
| תיאור | `Description` · `TopsideDescription` · `DownsideDescription` |

**‏`TopVar1`/`TopVar2` היו שמות חסרי משמעות. עכשיו הם צלע ראשונה וצלע שנייה** —
ומשמעותם משתנה לפי `Layout`. **לעולם לא הייתי מנחש שאותו שדה הוא גם רדיוס וגם עומק תפר.**

---

## מה זה משנה אצלי

| # | מה | במקום |
|---|---|---|
| 1 | **קיטום ריפ = 3 פרמטרים** (`Layout`, `1st Edge`, `2nd Edge`) | ‏214 פוליגונים שהחלפתי ביד |
| 2 | עיצוב קונטור = **בוליאני עם קונטור סגור** | החלפת כל הפוליגון ב-`SetPolygon` |
| 3 | ‏`Min. Radius` / `Max. Height` — **התוכנה אומרת את הגבולות** | ניחוש |
| 4 | ‏`Selected Edge` — **מספור צלעות**, ו-`0-0` = מסביב | טיפול בכל צלע בנפרד |
| 5 | `Top`/`Bottom` **בנפרד** | הנחה שהעיבוד סימטרי |

## מה נשאר פתוח
- ⚠️ מהם **ששת** סוגי עיבוד הצלע (`EdgeLayout`) — המדריך אומר "שישה" ולא מונה אותם.
  **לבדוק ב-`enumdump` על `EdgeLayout`.**
- ⚠️ האם `PsEdgeChamfer` נוצר עצמאית או רק כחלק ממודיפיקציה של פלטה
  (`PsEditPlateModification`).
- ⚠️ האם `Min. Radius`/`Max. Height` נגישים לקריאה מה-API — הם ידע שימושי לאימות.
