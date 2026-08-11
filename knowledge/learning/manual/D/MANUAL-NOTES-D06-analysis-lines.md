<div dir="rtl" align="right">

# D.6 — קווי אנליזה סטטית אפקטיביים

**המדריך:** עמ' 978–981 · שורות 25309–25427 · נקרא במלואו.
**רצועה:** `D06-analysis` ב-x = 660,000 · גריד `41E` על `_STRIPS`.

> ⚠️ **הבהרת גבול:** זו **הכנת מודל לייצוא** לתוכנת אנליזה. **לא חושב עומס ולא נבדק דבר** — זהו
> תפעול תוכנה (Phase 1), לא תכן (Phase 2, שנעול).

---

## מה הפרק עושה

לכל פרופיל יש **קו אנליזה אפקטיבי**, וברירת המחדל שלו היא **קו המרכז**. הממשקים לתוכנות אנליזה
מייצאים את הקו האפקטיבי במקום קו המרכז, וניתן לשנות אותו **בלי להזיז את הפלדה** — כדי **לאזן
אקסצנטריות** או **לסגור צמתים פתוחים**.

הצבע מעיד: **אדום = קצה פתוח · ירוק = מחובר לשכן**.

**כפתורי הדיאלוג:** `Automatic` (מנסה לחבר ולסגור מערכת; ⚠️ *"the penetrating lines are **not
divided** at the nodes — you can force this division later during export"*) · `Update colours`
(**רק בודק ומעדכן תצוגה**, לא משנה קווים) · `Manual link` (החלק הראשי שומר מקום, המחוברים מתייחסים
אליו).
**הגדרות:** `Check colours` · `Check connect` · `Hide` (מסגרות עבודה, אלמנטים מבניים, פוליפלטות,
ברגים, חלקי חיבור) · **`Shorten offsets` + `Max. Offset`** (מקצר קצוות "מזופפים" בצומת; ערכים
גדולים מהמקסימום נחשבים **קונסולה** ולא מטופלים).
**בתכונות החלק:** `Start X,Y,Z` · `End X,Y,Z` · `Manual input` · `Connected with` ·
**`No Discrimination`** (מונע חלוקת המוט לקטעים בין צמתים בעת ייצוא).

---

## ✅ הפרק נגיש **במלואו** מקוד — ושוב זה ה-COM שמציל

⭐ **המחלקה המנוהלת `PsAnalysisDisplay` ממופה אחד-לאחד לדיאלוג:**

| הדיאלוג | ה-API |
|---|---|
| `Check colours` / `Check connect` | `CheckColor` / `CheckConnection` |
| חמשת ה-`Hide` | `HideFrames` · `HideObjects` · `HidePlates` · `HideBolts` · `HideSubs` |
| `Shorten offsets` + `Max. Offset` | **`SubCantilever`** + **`MaxCantileverOffset`** |
| כפתור `Automatic` | **`PerformAutomaticConnection()`** |
| כפתור `Update colours` | `UpdateAnalysisColor()` |
| כפתור `Manual link` | **`PerformSetLinkBetween(id1, id2)`** (גם על `PsMiscTools`) |
| בחירה | `CollectShapesFromDrawing()` / `CollectShapesFromSelection(sel)` |
| תבניות | `LoadFromTemplate()` / `WriteToTemplate()` |

⭐⭐ **אבל נתוני הקו עצמו אינם על משטח ה-.NET — הם על אובייקט ה-COM של הפרופיל**
(`IKs_ComAnalysis`), ונגישים ישירות מפייתון דרך `doc.HandleToObject(handle)`:

```
GetAnalysisLine(start, end)      SetAnalysisLine(start, end)
GetAnalysisIsConnected(...)      SetAnalysisIsConnected(...)
GetAnalysisIsChanged(...)        SetAnalysisIsChanged(...)
GetAnalysisIsProtected(...)      SetAnalysisIsProtected(...)
GetAnalysisVectors(a, b)         SetAnalysisVectors(a, b)
```

⇒ **מופע נוסף של הכלל: כש-.NET לא מגיע, ה-COM כן.** ‏(אחרי `PsGrid` ב-B.6.)
⭐ וגם **`Ks_ComAnalysis` נגיש מבחוץ** דרך `GetInterfaceObject('PSCOMWRAPPER.Ks_ComAnalysis')` —
`EnableAnalysisView`, `SetAnalysisDisplay`, `UpdateAnalysisColor` רצו בלי חריגה.

---

## 🧪 הניסוי — מסגרת אקסצנטרית מכוונת, בדיוק המקרה של הפרק

המדריך: *"if the end is open and not a cantilever, then you are dealing with **eccentric shapes**
in most cases and you could create a closed system by means of this function."*

```
עמוד A  HE200B  662000, 0, 0…4000
עמוד B  HE200B  668000, 0, 0…4000
קורה    IPE300  662000,300,4000 → 668000,300,4000     ← 300 מ"מ מחוץ למישור העמודים
```

**לפני — הקו האפקטיבי שווה לקו המרכז, בדיוק כדברי המדריך:**
```
41F  col A   start=(662000, 0, 0)     end=(662000, 0, 4000)     connected=False
420  col B   start=(668000, 0, 0)     end=(668000, 0, 4000)     connected=False
421  beam    start=(662000, 300, 4000) end=(668000, 300, 4000)  connected=False
```
⭐ **`connected=False` בשלושתם — נכון:** הצומת באמת פתוח, בבנייה. **המכשיר מדווח את הפגם שהפרק
קיים כדי לתקן.**

**הכתיבה — וזו ההבטחה המרכזית של הפרק:**
```
SetAnalysisLine(662000,0,4000 → 668000,0,4000)
  → נקרא בחזרה:  start=(662000, 0, 4000)  end=(668000, 0, 4000)     ✅
  → הפלדה:       ext 662000,225,3850;668000,375,4150   לפני ואחרי — זהה לחלוטין  ✅
```
⇒ ⭐⭐⭐ ***"modify the effective lines **independently of the insertion of a shape**"* — מאומת.**
הקו הזיז את עצמו, הפרופיל לא זז במילימטר.

**⭐⭐ והבונוס שלא ביקשתי:** `GetAnalysisVectors` החזיר **`((0, −300, 0), (0, −300, 0))`** —
**ההיסט מקו המרכז לקו האנליזה, בכל קצה בנפרד.** לא הזנתי אותו; הוא נגזר. ⇒ **זו האקסצנטריות
עצמה, כווקטור** — בדיוק הכמות שהפרק מדבר עליה.

**הדגלים — נכתבים ונקראים:**
```
לפני:  connected=False changed=False protected=False
SetAnalysisIsChanged/Connected/Protected(True)
אחרי:  connected=True  changed=True  protected=True     ✅
```

---

## ⚠️ מה שלא עבד, ואיפה הגבול

**`UpdateAnalysisColor()` לא שינה את `connected`** — והוא **לא אמור**: המדריך מבחין במפורש בין
*"here the program **only checks** and the display is updated"* לבין `Automatic`, ש*"tries to
create a connection and **can also modify the effective line**"*. הצומת היה פתוח, והבודק אמר פתוח.

⇒ **הדגלים הם נתונים.** מי שמבצע את הפעולה קובע אותם; הדיאלוג עושה זאת בשבילך, ומקוד צריך לכתוב
אותם בעצמך. ⚠️ **וזו סכנה אמיתית: אפשר להצהיר `connected=True` על צומת פתוח גאומטרית.** במדגם
הזה ההצהרה מוצדקת — הקו האפקטיבי אכן הועבר למישור העמודים — והמדגם **מתויג ב-`Note1`/`Note2`**
במפורש כך שלא ייקרא כאילו נסגר מעצמו.

⛔ **`PerformAutomaticConnection()` לא הורץ** — הוא על המחלקה המנוהלת ולא ב-COM, ודורש אופ תוסף.
**נמפה, לא נמדד**, ואני אומר זאת ככה ולא אחרת.

---

## 🧾 BUILD PROOF

```
רצועה D06-analysis   גריד 41E על _STRIPS, ext 659,999…716,001
collision box=…      parts=3 collisions=0 minvol=100     ← שלושת הפרופילים במודל, נמדדו
3 פרופילים           2 × HE200B + IPE300 אקסצנטרי — מסגרת עם צומת פתוח מכוון
נקרא מהמודל          קווי אנליזה של שלושת החלקים, לפני ואחרי
נכתב ואומת           SetAnalysisLine + שלושת הדגלים, כולם נקראו בחזרה
לא זז                ext הקורה זהה לפני ואחרי — הראיה שהקו עצמאי מהפלדה
census סוף הפרק      98 ישויות
```

⭐ **`collisions=0` כאן הוא ממצא ולא רק ראיה:** הקורה אקסצנטרית ב-300 מ"מ ולכן **אינה נוגעת**
בעמודים — צומת פתוח **גם גאומטרית וגם באנליזה**, וזה בדיוק המקרה שהפרק מתאר.

**‏JOINT AUDIT:** הפרק **אינו בונה מכלול מוברג** — אין ברגים ואין חורים. שלושת הפרופילים ידווחו
`FLOATING`, וזה נכון: **המסגרת מכוונת להיות לא-מחוברת** — היא הדגמה של צומת פתוח, שהיא כל הנושא.
⚠️ לא נוספה שורה ל-`joints-legacy.tsv`.

---

## שאלות פתוחות ⏳

1. ‏`PerformAutomaticConnection()` — **הפונקציה המרכזית של הפרק, ולא הורצה.** דורשת אופ תוסף.
2. ‏`SubCantilever` + `MaxCantileverOffset` (‏"Shorten offsets") — **לא נבדקו.**
3. ‏`No Discrimination` — מונע חלוקת מוט לקטעים בייצוא; שייך ל-D.7 ולכן **לא נבדק** לפי הוראת אמיר.
4. ‏`PerformSetLinkBetween` קיים **בשתי מחלקות** (`PsAnalysisDisplay` ו-`PsMiscTools`) — לא נבדק
   אם הן עושות אותו דבר.
5. ⏳ **לאמיר:** האם ארץ ברזל בכלל מייצאת לתוכנת אנליזה? אם לא — הפרק שווה בעיקר בגלל
   **`GetAnalysisVectors` כמד אקסצנטריות**, שהוא שימושי בפני עצמו לביקורת מודל.

</div>
