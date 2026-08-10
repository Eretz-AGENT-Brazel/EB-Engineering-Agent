# 📕 פרק `B.29` — מספור · **נסגר** (עם היקף מוצהר)

*‏06/08/2026. עמודים 376–407, שורות 8,947–9,860.*

## 🔍 היקף הקריאה — נאמר במפורש, לא מוסתר

| תת-פרק | מה יש בו | עומק הקריאה |
|---|---|---|
| `B.29.1` מספור אוטומטי | זהות, סבולות, תחיליות, מיון | ✅ **לעומק** |
| `B.29.4` פקודות עזר | חיפוש, מחיקה, **השוואת שני חלקים** | ✅ **לעומק** |
| `B.29.5` מספר שרטוט | תחילית/סופית למספר המיקום | ✅ נקרא |
| `B.29.2` הכנסה ידנית | **דגלוני מיקום** (סימון 2D) | ⚪ לידיעה |
| `B.29.3` חלוקה אוטומטית | דגלונים | ⚪ לא נקרא |
| `B.29.6` תצוגת דגלונים | דגלונים | ⚪ לא נקרא |
| `B.29.7` ניהול סגנונות | דגלונים | ⚪ לא נקרא |

**הסיבה:** ארבעת האחרונים עוסקים ב**דגלוני מיקום** — סימון בשרטוט 2D, כלומר **שרשרת הפלט
שאמיר דחה במפורש** לפרק נפרד ("דבר ראשון נועלים מודל, ואחר כך עוברים לתכניות").
**זו החלטת היקף מוצהרת, לא דילוג.** כשנגיע לשרשרת הפלט — הם ייקראו.

---

## 1 · 🔴 מספור אינו שלב פלט — הוא תשתית מידול

שלושה פרקים נפרדים הפנו לכאן:
- **`B.4.5` Clone Manipulations** — *"only parts with the **same position number** will be considered"*
- **`B.28.3` Compare+Modify** — משווה קבוצות **לפי מספר מיקום**
- **`B.29.1` Group Detection** — *"single parts are only compared **using their position number**"*

⇒ **בלי מספור, שלושת הכלים החזקים ביותר לעבודה חזרתית פשוט לא עובדים.**
זה משנה את סדר העבודה: מספור בא **לפני** שכפול והעברת עיבודים, לא אחרי.

## 2 · 🔴 התוכנה מסווגת עמוד מול קורה **לפי זווית**

> *"**Columns** — the designation for all **vertical** shapes. In the attribute field **Position
> Tolerance** you enter an **angle** within which the shape is still considered to be vertical.
> **Beams** — for all **horizontal** shapes... Vertical and horizontal are always related to the
> model, i.e. to the **XY-plane of the WCS**. **Other** — all other groups."*

- הסיווג **תמיד ביחס לחלק הראשי** של הקבוצה.
- `Family Prefixes` — תחילית ממחלקת המשפחה, אם החלק שייך לאחת.

**תיקון להבנה שלי:** ב-API יש `SetColumnTol` / `SetBeamTol`. הנחתי שאלה סבולות **ממדיות**.
**הן סבולות זווית.** לא הייתי מגלה את זה בלי המדריך.

## 3 · 🔴 שתי שיטות **שונות** לזהות חלקים זהים

### (א) גיאומטריה
> *"all **outer edges** of a component part are determined and **individually compared**...
> You can positively exclude that e.g. **rounding errors in the case of bevel cuts or notches**
> lead to different parts."*

| הסבולת | מה היא עושה | ב-API |
|---|---|---|
| **`Minimum Line Length`** | *"Lines up to the specified length are **not considered** for a comparison"* | `SetMinLineLength` |
| **`Length Tolerance`** | סטיות אורך בין שני קווים עד לערך — מתעלמים | `SetLengthTol` |
| **`Drill Hole Tolerance`** | *"deviations of the **drill hole AXES**"* | `SetHolesTol` |
| **`Weight Tolerance`** | הפרשי משקל עד לערך — מתעלמים | `SetWeightTol` |

⚠️ **תיקון להערה קודמת שלי:** רשמתי ש-`SetHolesTol` הוא **קוטר** הקדיחה. הסעיף המפורט
אומר **סטיית ציר החור** — כלומר **מיקום**, לא קוטר. שני דברים שונים לגמרי.
*(הניסוח הקצר ב-`B.29.1` — "dimensions and drill diameter" — הוא שהטעה אותי. הפירוט גובר.)*

### (ב) נפח
> *"a **real volume comparison**... The tolerances can be set for groups and single parts as
> **relative value** to the compared volume (e.g. **0.1 percent** for component parts and
> **0.2 percent** for groups)."*

⇒ `SetVolCheck` · `SetMinSingleVol` · `SetMinGroupVol`. **סבולת יחסית באחוזים**, לא מוחלטת.

## 4 · שם המחבר — שלוש סכמות
> `Consecutive Numbering` · **`PosNum+PosNum`** (שילוב מספרי המיקום של **המחובר והתומך**) ·
> **`PosNum+Index`** (מספר המחובר + אינדקס רץ)

⇒ `SetConnectionIdentLayout`. ומחברים ממוספרים **רק אם ניתן להם שם** (`Only with Names`).

## 5 · `B.29.4` — פקודות עזר, וכלי שחיפשתי

| הפקודה | מה היא עושה |
|---|---|
| **🔴 השוואת שני חלקים** | *"Select the two parts and the result is displayed in a dialog box... you can see **in detail where the parts differ** from each other."* **‏diff ברמת החלק** |
| חיפוש המספר הגבוה | המספר הגבוה ביותר של קבוצה/חלק בבחירה |
| חיפוש לפי מספר | תצוגה: `Hide` / `Check` / `Zoom Extents` |
| מחיקת מספרים | עם מסנן `Single Parts` / `Groups` |
| **קיבוע מספר מקורי** | *"enters the current position number as **original** position number. The original number **can only be modified using this command**"* ⇒ `OrigPosnum` מוגן |
| עריכת מספרים | דיאלוג לבחירה. **ALT** = קבוצות · **Ctrl** = **להציג גם חלקים בלי מספר** |

**‏diff ברמת החלק** הוא בדיוק מה שהיה מסביר את מבחן 5: 19 פלטות עם 4 חורים מול אחת עם 6 —
הוא היה מראה **איפה** ההבדל, לא רק שיש.

## 6 · `B.29.5` — מספר שרטוט בתוך מספר המיקום
`Pos 1234 – 100`. משתנים: **`$(DWGNUM)`** · **`$(DWGIDX)`**. תחילית או סופית.
⚠️ *"added in a **separate step after detailing**"* — כלומר שייך לשרשרת הפלט.

---

## מה זה משנה בסדר העבודה שלי

**היה:** בנה → שכפל → קדח → בדוק
**צריך להיות:**
```
בנה פרט אחד  →  קבוצה  →  מספור (עם סבולות)
                              ↓
              Clone Manipulations (קדח אחת, העבר לכולן)
              Compare+Modify      (תפוס חלקים שאינם באמת זהים)
              Equal Part Detection (גיאומטריה או נפח)
```

**כל מה שבניתי ביד — `replicate` עם handles, לולאת קדיחה, `audit.py` — יושב על התשתית
הזאת בתוכנה, והיא דורשת מספור כדי לעבוד.**

## רשימת פעולה
| # | מה | מקור |
|---|---|---|
| 1 | **למספר לפני שכפול**, לא אחרי | §1 |
| 2 | `SetColumnTol`/`SetBeamTol` = **זווית**. לא לשלוח להם מידות | §2 |
| 3 | `SetHolesTol` = סטיית **ציר** החור, לא קוטר | §3 |
| 4 | לבחור מודע בין זיהוי **גיאומטרי** לזיהוי **נפחי** | §3 |
| 5 | ‏`Minimum Line Length` מונע ש"קווים זעירים" יפצלו חלקים זהים | §3 |

## מה נשאר פתוח
- ⚠️ האם השוואת שני חלקים (`B.29.4`) נגישה ב-API, או רק כדיאלוג.
- ⚠️ האם אפשר להריץ מספור **במצב בדיקה בלבד** בלי לכתוב מספרים למודל.
  ‏`SetOverrideExisting(false)` הוא מועמד — **טעון ניסוי לפני שימוש על מודל אמיתי.**
- ⚪ ‏`B.29.2/3/6/7` (דגלוני מיקום) — נקראים כשנגיע לשרשרת הפלט.
