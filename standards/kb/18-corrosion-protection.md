# הגנה מפני שיתוך — ISO 12944 / ISO 1461 / ISO 14713

## 1. היקף ותחולה (Scope)

מודול זה מכסה את מערך התקנים להגנה מפני שיתוך (corrosion) של מבני פלדה בייצור ובהקמה. שלושה צירים משלימים:

- **ISO 12944** — הגנה בשיטת מערכות צבע מגן (protective paint systems) על פלדה חשופה, על פלדה מגולוונת בטבילה חמה (לפי ISO 1461) ועל ציפוי מתכתי בהתזה תרמית (ISO 2063). זהו התקן המרכזי לבחירת מערכת צבע לפי סביבה ותוחלת נדרשת.
- **ISO 1461** — דרישות ושיטות בדיקה לציפוי גילוון בטבילה חמה (hot dip galvanizing) על פריטי ברזל ופלדה מוגמרים (batch/general galvanizing). קובע עוביי ציפוי מינימליים.
- **ISO 14713** (חלקים 1–3) — הנחיות ותכן להגנה בציפויי אבץ: עקרונות תכן, קצבי שיתוך צפויים וניבוי אורך חיים; ISO 14713-2 מתמקד ב-hot dip galvanizing.
- **ISO 8501** — הערכה ויזואלית של ניקיון פני שטח לפני צביעה (rust grades ו-preparation grades).

התקן **חל** בכל פרויקט פלדה מבנית שנדרשת לו הגנה מפני שיתוך אטמוספרי, טבילה במים או קבורה בקרקע — גשרים, מבני תעשייה, מכלים, מבנים ימיים/offshore. ISO 12944 מיועד למהנדסי תכן, יועצי צבע ומבצעים, ומספק שפה משותפת לכתיבת מפרט הגנה. **חשוב:** התקן אינו חל על ציוד תחת מגע מזון/מי שתייה ואינו מחליף אחריות מהנדס רשוי.

## 2. מפת סעיפים (חלקי הסדרה)

- **ISO 12944-1** General introduction (מבוא, הגדרות, טבלת מונחים)
- **ISO 12944-2** Classification of environments (סיווג סביבות — קטגוריות C ו-Im)
- **ISO 12944-3** Design considerations (שיקולי תכן מבניים למניעת מלכודות שיתוך)
- **ISO 12944-4** Types of surface and surface preparation
- **ISO 12944-5** Protective paint systems (מערכות צבע לפי קטגוריה ותוחלת)
- **ISO 12944-6** Laboratory performance test methods
- **ISO 12944-7** Execution and supervision of paint work
- **ISO 12944-8** Development of specifications for new work and maintenance
- **ISO 12944-9** Offshore and related structures — CX ו-Im4 (נוסף ב-2018)

**ISO 1461** — Scope; Definitions; General requirements; Properties of the coating (Clause 6 — עובי ציפוי); Sampling; Test methods; Renovation; Declaration of conformity. **ISO 8501-1** — Rust grades (A–D); Preparation grades (Sa = blast, St = hand/power tool, Fl = flame).

## 3. שיטות תכן ובדיקות (Design methods & checks)

**שלב א — סיווג הסביבה (ISO 12944-2):** קובעים את קטגוריית הקורוזיביות מ-C1 עד CX (או Im1–Im4 לטבילה/קבורה) לפי אובדן מסה/עובי של דגם ייחוס בשנה הראשונה, או לפי טבלת דוגמאות סביבה טיפוסיות. עבור פלדת פחמן נמוכה:

`corrosivity ← thickness loss rₐ (µm/first year)` — סעיף 12944-2 Clause 5, Table 1.

**שלב ב — בחירת תוחלת (durability, 12944-5):** קובעים טווח תוחלת נדרש L/M/H/VH. **התוחלת אינה תקופת אחריות** אלא פרק הזמן הצפוי עד לתחזוקה ראשונה משמעותית (first major maintenance).

**שלב ג — בחירת מערכת צבע:** מצליבים קטגוריה × תוחלת ← מספר שכבות, סוג פריימר (למשל zinc-rich epoxy) ועובי יבש כולל NDFT. ככל שהקטגוריה גבוהה יותר והתוחלת ארוכה יותר — NDFT גבוה יותר.

**שלב ד — הכנת פני שטח (12944-4 + ISO 8501-1):** ברוב מערכות הצבע הביצועיות נדרש blast cleaning לדרגה **Sa 2½**. יש להעריך את מצב המצע (rust grade A–D) לפני עבודה.

**גילוון (ISO 1461) — לוגיקת בדיקה:**
1. מדידת עובי ציפוי מקומי (local coating thickness) בכל reference area; אסור שירד מ-local minimum.
2. עובי ממוצע (mean coating thickness) על הפריט/מדגם ≥ mean minimum.
3. המרה: `t_coat (µm) = m (g/m²) / ρ_Zn` כאשר ρ_Zn ≈ 7,2 g/cm³ ⇒ **1 µm ≈ 7,2 g/m²**.

**ניבוי אורך חיים לגילוון (ISO 14713-1):**

`Life_to_first_maintenance ≈ t_Zn / r_corr`

כאשר t_Zn עובי הציפוי (µm) ו-r_corr קצב שיתוך האבץ לקטגוריה (µm/year). הקצבים זהים לערכי ISO 9223 Table C.1.

## 4. פרמטרים וערכים מרכזיים (Facts + clause)

**ISO 12944-2:2017 — אובדן עובי פלדת פחמן נמוכה, שנה ראשונה (Table 1):**
- C1 (very low): ≤ 1,3 µm
- C2 (low): > 1,3 – 25 µm
- C3 (medium): > 25 – 50 µm
- C4 (high): > 50 – 80 µm
- C5 (very high): > 80 – 200 µm
- CX (extreme): > 200 – 700 µm

**אובדן מסה פלדה (Table 1):** C1 ≤ 10 g/m²; C2 > 10–200; C3 > 200–400 g/m².

**ISO 14713-1 / ISO 9223 — קצב שיתוך אבץ (µm/year, שנה ראשונה):**
- C1: < 0,1 · C2: 0,1–0,7 · C3: 0,7–2,1 · C4: 2,1–4,2 · C5: 4,2–8,4 · CX: 8,4–25

**ISO 12944-5:2019 — טווחי תוחלת (durability ranges):**
- Low (L): ≤ 7 שנים
- Medium (M): 7–15 שנים
- High (H): 15–25 שנים
- Very high (VH): > 25 שנים

**ISO 1461:2022 — עובי ציפוי גילוון מינימלי לפי עובי הפלדה (Clause 6, Table 3):**

| עובי פלדה | Local min (µm) | Mean min (µm) | Mean min (g/m²) |
|---|---|---|---|
| > 6 mm | 70 | 85 | 610 |
| > 3 – 6 mm | 55 | 70 | 505 |
| ≥ 1,5 – 3 mm | 45 | 55 | 395 |
| < 1,5 mm | 35 | 45 | 325 |

**ISO 8501-1:2007 — דרגות הכנה:** Sa 1 (קל), Sa 2 (יסודי), Sa 2½ (יסודי מאוד — הדרגה השכיחה במפרטים), Sa 3 (עד ברק מתכתי). Rust grades: A, B, C, D. St = ניקוי ידני/מכני, Fl = flame cleaning. (אין דרגת "Be" בתקן.)

**קטגוריות טבילה/קרקע (12944-2):** Im1 מים מתוקים · Im2 מי ים/מליחים · Im3 קרקע · Im4 מי ים עם הגנה קתודית (נוסף 2018).

## 5. קשר ת"י ↔ EN

ISO 12944, ISO 1461, ISO 14713 ו-ISO 8501 אומצו כתקנים אירופיים זהים (EN ISO ...) דרך CEN, ומכון התקנים הישראלי (SII) מאמץ אותם כ**ת"י בסימון SI ISO / ת"י EN ISO** עם דף לאומי (National Foreword). מבחינה טכנית — טבלאות הערכים והסעיפים **מופו ישירות** ללא שינוי מספרי. התאמה לאומית מתבטאת בעיקר בשפת ההקדמה ובהפניות; אין "national annex" מהותי המשנה ערכי עובי או קטגוריות. יש לאמת מול קטלוג SII את מספר הת"י ומהדורתו לפני ציטוט במפרט.

## 6. מהדורה ותוקף (נכון ליולי 2026)

- **ISO 12944** חלקים 1,2,3,4,5,6,7,8 — מהדורות 2017/2018; **חלק 9:2018** (offshore/CX). חלק 5 עודכן ל-**2019**. סטטוס: תקן וולונטרי בינלאומי; הופך למחייב כשמפרט/חוזה/רשות מפנה אליו.
- **ISO 1461:2022** — מהדורה רביעית, מחליפה 2009. חידוש מרכזי: הקלה לפלדות ultra-low reactivity (Si ≤ 0,01% ו-Al > 0,035%) — מותר להשתמש בקטגוריית עובי נמוכה יותר עם ציון ב-Declaration of Compliance.
- **ISO 14713-1:2017, -2:2019, -3:2017**.
- **ISO 8501-1:2007** (בבחינה לעדכון). 

התקנים עצמם וולונטריים; חובה משפטית נובעת מהפניה בהיתר/מפרט/דרישת מזמין.

## 7. הערות מעשיות ליצרן/מקים פלדה

- **תכן למניעת מלכודות (12944-3):** להימנע מכיסים סגורים, חפיפות (overlaps) לא אטומות ומדפים אוגרי מים; לתכנן ניקוז ולעגל פינות חדות (radius) לשיפור אחיזת צבע.
- **גילוון (1461):** לתכנן חורי ניקוז ואוורור (vent/drain holes) לפריטים חלולים — קריטי לבטיחות בטבילה ולאיכות; להימנע מהרכבות עם חללים כלואים.
- **תיאום עם EXC (EN 1090-2):** דרגת הביצוע (EXC1–EXC4) מכתיבה קפדנות בהכנת פני שטח, בתיעוד ובבקרה; EXC3/EXC4 → מפרט הגנה מחמיר, Sa 2½ ומעלה, תיעוד NDFT ובדיקות אחיזה.
- **ריתוכים ופינות:** stripe coat (מריחת יד נוספת) על קצוות, ריתוכים וברגים — נקודות התחלת השיתוך; פרופיל עוגן (anchor profile) יש להתאים לפריימר.
- **גילוון + צבע (Duplex system):** משלב עמידות אבץ ומחסום צבע; מאריך תוחלת מעבר לסכום המרכיבים, אך מחייב הכנת שטח מיוחדת (sweep blast) על משטח מגולוון.
- **טמפרטורה ולחות:** לצבוע רק כשטמפ' המצע ≥ 3°C מעל נקודת הטל, RH לרוב ≤ 85% (לפי יצרן הצבע ו-12944-7).

## 8. הפניות סעיף (Citations)

ISO 12944-2:2017 Clause 5 / Table 1 (steel & zinc loss, categories C1–CX); ISO 12944-2:2017 Clause 6 (Im1–Im4). ISO 12944-5:2019 Clause 5 (durability L/M/H/VH; system tables). ISO 12944-3:2017 (design). ISO 12944-9:2018 (CX/Im4). ISO 1461:2022 Clause 6 / Table 3 (coating thickness). ISO 14713-1:2017 Annex/Table (zinc corrosion rates, aligned to ISO 9223:2012 Table C.1). ISO 8501-1:2007 (rust & preparation grades Sa). EN 1090-2 (EXC coordination).

---
## ✅ אימות אדוורסרי
רמת ביטחון: **🟢 גבוה** · אושרו: 12 · נפסלו: 1 · ספק: 0.

כל הערכים המספריים והמהדורות המרכזיים במודול אומתו ונמצאו נכונים: טבלת אובדן עובי/מסה של פלדה (ISO 12944-2), קצבי שיתוך האבץ (ISO 14713-1/ISO 9223), טווחי התוחלת L/M/H/VH (ISO 12944-5), ועוביי הציפוי המינימליים בגילוון (ISO 1461 Table 3), לרבות המרת 1 µm ≈ 7.2 g/m². גם המהדורות אומתו: ISO 1461:2022 (מהדורה רביעית), ISO 12944-5:2019, ISO 12944-9:2018 ו-ISO 8501-1:2007. נמצאה שגיאה אחת לתיקון: דרגות ההכנה של ISO 8501-1 הן Sa, St ו-Fl בלבד — אין דרגה בשם "Be", ויש להסיר את "Be = blast to bare". המלצה נוספת: להפנות לסעיפים/טבלאות במקום לשכפל טבלאות תקן שלמות (סיכון זכויות יוצרים נמוך אך עדיף לצטט). רמת ביטחון כוללת: גבוהה.

### ⚠️ תיקוני אימות (יש להעדיף ערך זה):
- **ISO 8501-1 preparation grades listed as 'Sa, St, Fl, Be' with 'Be = blast to bare'** → נכון: **Preparation grades: Sa (blast), St (hand/power tool), Fl (flame). No 'Be'.** — ISO 8501-1 defines preparation grades only for blast-cleaning (Sa 1/2/2½/3), hand-and-power-tool (St 2/St 3) and flame cleaning (Fl). There is no 'Be' grade — this appears fabricated. Correct: Sa, St, Fl only.

> 🔒 בדיקת זכויות יוצרים: Section 4 reproduces the full ISO 12944-2 Table 1 category ranges, ISO 14713-1 zinc-rate table, and ISO 1461 Table 3 verbatim as data tables. Threshold numbers are non-copyrightable facts, but reproducing an entire standard table can raise concerns — prefer citing the clause/table rather than reprinting complete tables, and keep any surrounding descriptive prose paraphrased (as it currently is).; No verbatim copyrighted prose passages detected; narrative text is paraphrased in Hebrew. Overall copyright risk is low.

> **סימון:** ✓ אומת מקובץ רשמי · ⭑ מבוסס-Eurocode (לא בהכרח הערך הישראלי המחייב) · ⚠ טעון אימות מול הנוסח המחייב/הנספח הלאומי. *כלי-עזר — האחריות על מהנדס מוסמך.*
