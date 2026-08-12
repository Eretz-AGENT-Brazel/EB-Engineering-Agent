# איך מושג דיוק — עבודה על צירים, נקודות עבודה וסבולות

## למה זה חשוב

מודל פלדה הוא לא ציור — הוא הוראת ייצור. כל קואורדינטה שהסוכן קובע הופכת לחתך במסור, לקידוח, ל-NC file ולחלק שמגיע לאתר. מודל "נראה נכון" ובכל זאת בלתי-בנייה הוא התוצאה השכיחה ביותר של פיקים חופשיים (free picking), אורכים שנגררו מ-centreline בלי הכנות קצה, ונקודות הכנסה שגויות. שגיאה של 3 mm בנקודת עבודה אחת מתגלגלת דרך 20 אלמנטים ל-60 mm באתר — ואז חותכים בלהבה, ואיכות המסגרת נשברת. דיוק אינו "לצייר בזהירות"; הוא **שיטה**: רשת צירים → נקודות עבודה → קווי מערכת → הכנות קצה → סבולות מוצהרות → בדיקות אוטומטיות.

---

## 1. רשת צירים ונקודות עבודה (grid & work points)

הבסיס הוא **grid** מפורש: צירים A/B/C ו-1/2/3 ורמות (TOS – Top Of Steel). לכל אלמנט יש **work point (WP)** — הנקודה הגיאומטרית שממנה נמדדות הסבולות. לפי AISC 303 §7.13, סבולות ההקמה מוגדרות ביחס ל-work points ול-work lines, וב-אלמנט לא-אופקי ה-WP הוא **מרכז החתך בכל קצה של ה-shipping piece**. המשמעות המעשית: הסוכן לא בוחר "איפה שנראה טוב" — הוא **snap** לחיתוך צירים, ל-WP, או ל-endpoint של construction line שהוא עצמו יצר במידה עגולה.

בפרקטיקה: מסגרת פלטפורמה מ-RHS150X100X4 — הצירים נקבעים על **פני הבטון / קו התמיכה**, ה-TOS נקבע כמידה עגולה (למשל +3500), ורק אז נגזר קו המערכת של הקורה.

## 2. קו מערכת (system line) מול פנים (faces)

מודלים נבנים על **קווי מערכת**, לא על פאות. הכלל: קו המערכת הוא ציר הכובד/הציר הגיאומטרי של החתך, ומיקום החתך סביבו נקבע ע"י ה-insertion point. חריגים מקצועיים חשובים:

- **זוויתנים EA60X60X6** — לא ממודלים על ה-centroid אלא על **ה-heel** (מפגש שתי הפאות החוץ). כך הזוויתן נשען פיזית על הפח/הצינור, וקווי הברגים נמדדים מה-heel (gauge line).
- **תעלות U140 (UPN140)** — מרכז הכובד מוסט מגב ה-web בכ-17.5 mm ⚠ (לפי טבלת הקטלוג FRANCE.FR_UPN). מודל על ה-centroid במקום על גב ה-web = שגיאה של ~17.5 mm בכל stringer של מדרגות.
- **פסי שטח 300X12** — ממודלים על פאה (משטח מגע), לא על מרכז העובי, כי הם משמשים כ-bearing/מדרך.
- **צינורות CHS (BSC21.3X2.5, 26,9X2,6, 42,4X3,2)** — תמיד על הציר; אבל האורך אינו אורך הציר (סעיף 5).

## 3. Insertion point / reference point ב-ProSteel

ל-shape יש **9 נקודות ייחוס** במלבן החוסם החתך (מרכז, 4 פינות, 4 אמצעי-צלע), ובנוסף אפשר לתת offset. ההמרה שהסוכן חייב לדעת לעשות אוטומטית:

- RHS150X100X4 קורה שצריכה TOS = 3500, כאשר h=150: `z(system line) = 3500 − 75 = 3425` אם ה-insertion הוא **מרכז**; או `z = 3500` עם insertion = **top-centre**. עדיף להצהיר top-centre ולהשאיר את ה-Z במידה עגולה.
- SHS100X100X3 עמוד: insertion = centre, ו-`z` בסיס = מידה עגולה של פני הפלטה.
- ב-ProSteel יש **Object View / Object UCS** — יישור ה-UCS למשטח האלמנט, כאשר ראשית ה-UCS נופלת אנכית מנקודת הפיק אל **ה-centreline** (Object View) או בנקודת הפיק עצמה (Centered Object View). זה הכלי שהופך פיק "בעין" לפיק על הציר.

## 4. UCS / workframe / construction lines כפיגום הדיוק

לפני יצירת אלמנט בונים **workframe** (רשת עזר תלת-ממדית) או UCS מקומי. שני חוקים:

1. **אין יצירת אלמנט ב-WCS כשהאלמנט משופע.** משפעים (בריסים מ-40X2.5SHS, אלכסונים) נוצרים ב-UCS שמיושר למישור הבריס — כך זוויות החיתוך יוצאות אנליטיות ולא מעוגלות.
2. **construction lines** מ-WP ל-WP הן ה"אמת" — האלמנט נוצר מ-endpoint ל-endpoint שלהן, ולא מפינת אלמנט אחר (העברת שגיאה).

## 5. עיגול קואורדינטות ולמה זה קריטי

כל WP צריך ליפול על **מידה עגולה** — 1 mm, ועדיף 5/10 mm. מודל שבו עמוד יושב ב-Z=3424.9973 מייצר חיתוכים ב-0.0027 mm, clash reports שקריים, וקבצי NC עם מידות לא-מעוגלות. במיכלים זה מובנה: **מהלכי מעטפת של 2015 mm** — מידה עגולה מוצהרת, ולכן חישוב הגובה הוא כפולה מדויקת ולא סכימת שאריות.

## 6. הכנות קצה שמשנות את האורך האמיתי

זה הפער הגדול בין "אורך ציר" ל"אורך חיתוך":

| הכנה | מה עושה | סדר גודל |
|---|---|---|
| **setback / shortening** | קיצור מכוון לפינוי | 5–10 mm לכל קצה |
| **cope / notch** | חיתוך אוגן/web לפינוי | עומק notch תכנוני 50 mm (BCSA) |
| **miter / bevel cut** | חיתוך אלכסוני | לפי זווית הבריס |
| **profile / saddle cut** | חיתוך אוכף בצינור | ראה חישוב מטה |
| **weld root gap** | מרווח שורש | 0–3 mm |
| **LengthAddition** | עודף מכוון לחיתוך באתר | 10–50 mm |

**חישוב saddle קונקרטי** — BSC21.3X2.5 (OD 21.3, r=10.65) נפגש בניצב במעקה 42,4X3,2 (OD 42.4, R=21.2):
`עומק אוכף = R − √(R² − r²) = 21.2 − √(449.4 − 113.4) = 21.2 − 18.33 = 2.87 mm`
אורך החיתוך = מרחק ציר-לציר **פחות 18.3 mm** (הסיב הארוך) עד **פחות 21.2 mm** (הקצר). מי שמזמין את הצינור באורך ציר-לציר קיבל חלק ארוך ב-18 mm.

**כלל הפער בריתוך**: EN 1090-2 מגביל root gap ב-fillet weld ל-**3 mm** מקסימום, ומחייב **הגדלת ה-throat** בהתאם לפער (EN 1090-2 §7.5.8 ⚠). לכן מודל עם פער 0 שנבנה בפועל עם 3 mm = ריתוך חסר.

## 7. סבולות ייצור מול סבולות הקמה

- **EN 1090-2 Annex B** מפריד **essential tolerances** (מה שנדרש לעמידות ויציבות מכנית) מ-**functional tolerances** (התאמה ומראה), עם **class 1** (ברירת מחדל, פחות תובעני) ו-**class 2**. הטבלאות: B.1–B.14 ייצור, B.15–B.25 הקמה ⚠ (ערכים מדויקים — לפי הטבלה, לא מהזיכרון).
- **ישרות** — L/750 עד L/1000 (מינ' 2 mm) הוא טווח הערכים המצוטט; L/1000 גם סבולת המפעל לפי ASTM A6.
- **ISO 13920** (מבנים מרותכים — בדיוק העולם של פלטפורמות ומעקות): מידה לינארית 400–1000 mm: class B **±4 mm**, class C **±6 mm**; 2000–4000 mm: B **±6**, C **±10**; זוויתי (רגל >400): B **±10′**, C **±20′**; ישרות/מישוריות ל-1000 mm: B **6 mm**, C **12 mm**.
- **AISC 303**: אורך אלמנט ≤10 m **±3 mm**, >10 m **±6 mm**; cut-to-cut **±1.5 mm**; מיקום חור **±1.5 mm**; אנכיות עמוד בהקמה **1:500** (מקס' 25 mm לקומה, 50 mm למבנה); מרווח מגע קורה-עמוד מקס' **3 mm**.
- **סבולות מפעל של חתכים חלולים** (EN 10219-2 ⚠): מידה חוץ ~±1%, עובי ~±10%, ישרות ~0.2% מהאורך. לכן RHS150X100X4 יכול להגיע 149 או 151 — **אין להסתמך על מידה נומינלית לפינוי של 1–2 mm**.

## 8. שגיאה מצטברת

הכלל: **תמיד למדוד מ-datum, אף פעם לא משרשרת**. 12 מדרגות מ-EA60X60X6 שכל אחת ממוקמת מהקודמת ב-±1 mm → ±12 mm בקצה. אותן 12 מדרגות שממוקמות כל אחת מהצירים → ±1 mm. מכאן: chain dimensioning אסור; מידות מצטברות (running dimensions) מ-WP אחד.

## 9. Clash detection ובדיקות

בדיקת clash אינה "אחרי הכול" אלא **אחרי כל תת-הרכבה**. יש להגדיר **tolerance/clearance** לבדיקה — בלעדיה כלים מדווחים על חפיפות מיקרוסקופיות ומייצרים מאות פריטים חסרי-משמעות. הבחנה בין **hard clash** (פלדה בפלדה) ל-**clearance clash** (פינוי נדרש, למשל מרווח מפתח ברגים).

---

## כללי אצבע מקצועיים

1. **e₁ ≥ 1.2·d₀** ו-**e₂ ≥ 1.2·d₀**; **p₁ ≥ 2.2·d₀**, **p₂ ≥ 2.4·d₀** (EN 1993-1-8 Table 3.3).
2. **d₀ = d + 1 mm** ל-M12/M14, **d + 2 mm** ל-M16–M24, **d + 3 mm** מ-M27 (EN 1993-1-8 Table 3.1 ⚠).
3. דוגמה EA60X60X6 עם M12: d₀=13 → e₁ ≥ 15.6 → **בפועל 20 mm**; p₁ ≥ 28.6 → **בפועל 30 mm**; מקס' edge = 4t+40 = **64 mm**.
4. **fit-up gap טיפוסי 2–3 mm**; קיצור מכוון של end-plate beam: **5 mm** (עד 2 mm לקורות רגילות).
5. **root gap ל-fillet weld ≤ 3 mm**, ומעליו הגדלת throat (EN 1090-2 §7.5.8 ⚠).
6. **מרווח מגע base plate / קורה-עמוד ≤ 3 mm** (AISC 303).
7. **ישרות L/750 עד L/1000, מינ' 2 mm**; **אנכיות עמוד 1:500** (מקס' 25 mm/קומה, 50 mm/מבנה).
8. **אורך מיוצר ±3 mm** (≤10 m) / **±6 mm** (>10 m); **מיקום חור ±1.5 mm**.
9. **ISO 13920 class B/C** למסגרות מרותכות: ±4/±6 mm ל-400–1000 mm.
10. **e ≤ 0.25·h₀** — אקסצנטריות noding מותרת בצומת K מ-RHS (IIW/CIDECT ⚠).
11. **חורי אוורור/ניקוז לגלוון**: מינימום **12 mm** קוטר; לחתך חלול סגור — פתח בשטח **25%–30%** משטח החתך (ASTM A385 §12.4 ⚠).
12. **מהלכי מעטפת מיכל 2015 mm**; **לא יותר מ-3 ריתוכים בצומת אחד**.
13. כל WP על כפולה של **5 mm** כברירת מחדל; אורכי חיתוך מעוגלים ל-**1 mm**.

## טעויות נפוצות

- **פיק חופשי על גיאומטריה קיימת** במקום snap ל-grid/WP — יוצר קואורדינטות שבורות. פתרון: קודם construction lines, אחר כך אלמנטים.
- **insertion point שנשאר "centre"** כשהכוונה ל-TOS — כל הפלטפורמה יורדת ב-h/2 (75 mm ב-RHS150X100X4).
- **מודל זוויתן על ה-centroid** — EA60X60X6 "צף" 17 mm מהמשטח.
- **אורך = מרחק ציר-לציר** בצינורות — שגיאה של 18–21 mm בכל מוט מעקה.
- **פער 0 בין חלקים מרותכים** — לא בונים כך; יש להצהיר 2 mm.
- **שרשור מידות** במקום מ-datum → שגיאה מצטברת.
- **clash check בלי tolerance** → מאות דיווחי רעש, ומפספסים את ההתנגשות האמיתית.
- **הסתמכות על מידה נומינלית** לפינוי צר — סבולות מפעל של EN 10219 אוכלות אותו.
- **UCS שנשאר ב-WCS** בעבודה על בריסים משופעים → זוויות מעוגלות ולא-מדויקות.

## מה זה אומר לסוכן

**חייב לדעת לפני כל יצירת אלמנט (אין ברירת מחדל שקטה):**
1. catalog + profile מדויק (למשל `BRITAIN.BS_CELSIUS_RHS / RHS150X100X4`).
2. WP התחלה ו-WP סוף — כקואורדינטות מוחלטות ביחס ל-datum, במידה עגולה.
3. **insertion / reference point** (centre / top-centre / heel / corner) + offset.
4. סיבוב סביב הציר (rotation) — קריטי ל-U140 ול-EA.
5. הכנות קצה: setback, cope, miter, gap, LengthAddition.

**מה מותר לו לקבוע לבד (ברירות מחדל בטוחות):** gap 2 mm, root gap 0 בציור עם הצהרה ≤3 mm בייצור, notch 50 mm, e₁/p₁ לפי הכללים 1–3, עיגול ל-5 mm, ISO 13920 class C כברירת מחדל למסגרת מרותכת, חורי גלוון 12 mm.

**מה חייב לשאול את אמיר:** מהי מערכת ה-datum ומה ה-TOS; האם הפריט מגולוון (משנה חורים ופערים); tolerance class 1 או 2 לפי EN 1090-2; execution class; האם יש LengthAddition לחיתוך באתר; האם הצומת gap או overlap; מי מבצע את ההקמה (משפיע על סבולות ההרכבה).

**בדיקות לאחר מודלינג (checking routine קבוע):**
1. כל הקואורדינטות של WP הן כפולות של 5 mm (או מוצדקות).
2. כל אורך חיתוך ≠ אורך ציר בכל מקום שיש saddle/cope/miter — אימות מספרי.
3. clash check עם clearance מוצהר, הפרדה hard/clearance.
4. בדיקת e₁/e₂/p₁/p₂ לכל קבוצת ברגים מול d₀.
5. סכימת שגיאה: השוואת מידה כוללת מ-datum מול סכום החלקים.
6. חתכים חלולים סגורים — קיום חורי אוורור/ניקוז אם מגולוון.
7. צמתי מיכל — לא יותר מ-3 ריתוכים; מהלכים כפולה של 2015 mm.
8. דיווח מפורש של כל הנחה שהסוכן קבע לבד, כדי שאמיר יאשר.

## 📐 כללי אצבע (טבלה מרוכזת)

| כלל | ערך | חל על | מקור |
|---|---|---|---|
| Minimum end/edge distance for bolt holes | **e1 ≥ 1.2·d0, e2 ≥ 1.2·d0** | bolted connections, normal clearance round holes | EN 1993-1-8 Table 3.3 |
| Minimum bolt spacing | **p1 ≥ 2.2·d0, p2 ≥ 2.4·d0** | bolted connections | EN 1993-1-8 Table 3.3 |
| Nominal hole clearance | **d0 = d+1mm (M12,M14); d+2mm (M16–M24); d+3mm (≥M27) ⚠** | normal round holes | EN 1993-1-8 Table 3.1 (verify against national annex) |
| Maximum edge distance | **e ≤ 4t + 40 mm (e.g. 64 mm for EA60X60X6, t=6)** | exposed bolted connections | EN 1993-1-8 Table 3.3 |
| Typical fit-up gap between assembled parts | **2–3 mm** | welded and bolted fit-up in light steel weldments | industry practice; EN 1090-2 §7.5.8 for weld compensation |
| Maximum root gap for fillet welds before throat must be increased | **3 mm ⚠** | fillet welds | EN 1090-2 §7.5.8 |
| Intentional shortening of end-plate beams for fit-up | **2 mm typical, 5 mm where fit-up problems anticipated** | end-plate / full-depth connections | detailing practice (BCSA / SCI guidance) |
| Design notch (cope) depth for beam-to-beam connections | **50 mm** | notched secondary beams | BCSA standard connections |
| Notch clearance at supporting beam flange | **t/2 + 2 mm** | notched beam ends | SCI/BCSA connection design practice |
| Member straightness tolerance | **L/750 to L/1000, minimum 2 mm ⚠** | fabricated members | EN 1090-2 Annex B (essential/functional tables B.1–B.14); ASTM A6 gives L/1000 mill camber/sweep |
| Column plumb (erection) | **1:500 of distance between working points; max 25 mm per storey, 50 mm total** | erected columns | AISC 303 §7.13.1.1 |
| Fabricated member length tolerance | **±3 mm for L ≤ 10 m; ±6 mm for L > 10 m** | cut-to-length members | AISC 303 §6.4.1 |
| Cut-to-cut dimension and hole position tolerance | **±1.5 mm (1/16 in)** | shop fabrication | AISC 303 §6.4.1 / §6.4.2 |
| Maximum contact/bearing gap | **3 mm (base plate bearing, beam-to-column contact)** | erection fit-up | AISC 303 §7.5.3 / §7.13.4 |
| General welded-structure linear tolerance, class B / class C | **400–1000 mm: ±4 / ±6 mm; 1000–2000 mm: ±5 / ±8 mm; 2000–4000 mm: ±6 / ±10 mm** | welded platforms, stairs, handrails, frames | ISO 13920 |
| General welded-structure angular tolerance, class B / class C | **leg >400 mm: ±10′ / ±20′; leg 120–400 mm: ±20′ / ±30′** | welded assemblies | ISO 13920 |
| Form tolerance (straightness/flatness/parallelism) over 1000 mm | **class A 3 mm, B 6 mm, C 12 mm, D 24 mm** | welded assemblies | ISO 13920 |
| Mill tolerances for cold-formed hollow sections | **outside dimension ≈ ±1%, wall thickness ≈ ±10%, straightness ≈ 0.2% of length ⚠** | RHS150X100X4, SHS100X100X3, 50X3.0SHS, 40X2.5SHS | EN 10219-2 (values to be confirmed from the standard's tables) |
| Noding eccentricity limit in gapped/overlapped K-joints | **e ≤ 0.25·h0 ⚠** | RHS/CHS truss joints | CIDECT DG3 / IIW (2009) recommendations |
| Vent/drain holes for hot-dip galvanizing of hollow sections | **minimum 12 mm diameter (10 mm absolute min); single opening 25%–30% of cross-sectional area ⚠** | closed CHS/RHS/SHS members that are galvanized | ASTM A385 §12.4; Galvanizers Association guidance |
| Saddle (profile) cut shortening for a branch tube meeting a chord tube | **depth = R − √(R²−r²); cut length = centreline distance − √(R²−r²) … − R. For BSC21.3X2.5 into 42,4X3,2: depth 2.87 mm, shortening 18.3–21.2 mm** | CHS handrail/balustrade infill (BSC21.3X2.5, 26,9X2,6) | geometry (derived) |
| UPN channel centroid offset from web back | **≈17.5 mm for U140 ⚠** | U140 stair stringers (FRANCE.FR_UPN) | section catalogue tables (verify per catalog entry) |
| Coordinate rounding discipline | **work points on multiples of 5 mm; cut lengths rounded to 1 mm** | all modelling | detailing practice |
| Tank shell course height and weld junction rule | **courses of 2015 mm; no more than 3 welds meeting at a junction** | Eretz Barzel steel tanks | company fabrication rule |
| EN 1090-2 functional tolerance class default | **class 1 is the default for routine execution; class 2 requires special (more expensive) measures** | specification of fabrication and erection accuracy | EN 1090-2 Annex B (tables B.1–B.14 fabrication, B.15–B.25 erection) |
| Datum discipline against cumulative error | **measure every element from a single datum, never chain; 12 chained elements at ±1 mm give ±12 mm** | repetitive elements (stair treads, purlins, balusters) | detailing practice |


## 🤖 מה זה אומר לסוכן

- Before creating any Ks_Shape the agent must have five parameters resolved: catalog+profile string, start WP, end WP, insertion/reference point (centre / top-centre / heel / corner) with offset, and rotation about the axis. None of these may be silently guessed.
- Insertion point conversion must be automatic: for RHS150X100X4 at TOS 3500, system-line z = 3425 if insertion is centre, or z = 3500 if insertion is top-centre. The agent should prefer top-centre so the round number stays in the model.
- Angles (EA60X60X6, EA80X80X8) must be placed on the heel, not the centroid; U140 must be placed on the web back, accounting for the ~17.5 mm centroid offset.
- Centreline length is never the cut length for CHS branches. The agent must compute the saddle shortening √(R²−r²) and report both the nominal centreline distance and the fabricated cut length.
- The agent must create construction lines / a workframe and a local UCS before modelling sloped members, then snap to endpoints — never pick free geometry off an existing solid.
- Safe defaults the agent may apply without asking: 2 mm fit-up gap, 50 mm notch depth, e1/p1 per EN 1993-1-8, 5 mm coordinate rounding, ISO 13920 class C for welded frames, 12 mm galvanizing vent holes.
- Must ask Amir: the datum system and TOS, whether the item is galvanized, EN 1090-2 tolerance class 1 or 2, execution class, whether any LengthAddition for site cutting is wanted, gap vs overlap at hollow-section joints, and who erects.
- Post-modelling checks the agent should run every time: (1) all WP coordinates on 5 mm multiples, (2) cut length verified against centreline length wherever a cope/miter/saddle exists, (3) clash check with an explicit clearance value, separating hard clashes from clearance clashes, (4) e1/e2/p1/p2 versus d0 for every bolt group, (5) overall dimension from datum versus sum of parts, (6) vent/drain holes present on closed galvanized sections, (7) tank junctions ≤3 welds and courses a multiple of 2015 mm.
- Clash detection must always be run with a declared tolerance; without it the agent will generate hundreds of microscopic false positives and miss the real interference.
- The agent must report every assumption it defaulted, so Amir can approve or override — precision is a declared contract, not an implicit one.


---
## ✅ אימות אדוורסרי
רמת ביטחון: **🟡 בינוני** · אושרו: 10 · **נפסלו: 12** · ספק: 6

המודול נכון בשיטה ובעיקרון (צירים → נקודות עבודה → הכנות קצה → סבולות מוצהרות), וכמה מהחישובים המקוריים שלו מדויקים לחלוטין — חשבון האוכף (עומק 2.87 mm, קיצור 18.3–21.2 mm) אומת מחדש מהגיאומטריה, וכך גם UPN140 ey=17.5 mm, e₁/p₁ לפי EN 1993-1-8 Table 3.3, ברירת המחדל class 1 ומבנה Annex B ב-EN 1090-2 (אומת מול הטקסט המקורי של BS EN 1090-2:2018), ו-e ≤ 0.25h₀ לפי CIDECT/IIW. אבל חלק ניכר מהמספרים ה"אקטואביליים" שגוי או ממוקם בסטנדרט הלא-נכון: אין מגבלת 3 mm ל-root gap ב-EN 1090-2 §7.5.8 (הכלל האמיתי הוא a = a_nom + 0,7h, והמגבלה מגיעה מ-ISO 5817 617: ~1–1.5 mm לריתוך a=5), טבלאות ISO 13920 שגויות בשלושה מקומות (class B לינארי, כל הזוויות, ומחלקות הצורה שהן E/F/G/H ולא A/B/C/D), סבולות AISC (אורך ±3/±6 mm, מרווח מגע 3 mm, מיקום חור ±1.5 mm, אנכיות 25/50 mm) אינן הערכים שבקוד, ומינימום ישרות הוא 5 mm (class 1) / 3 mm (class 2) ולא 2 mm. החמור מבחינה מעשית: חורי הגלוון — EN ISO 14713-2 דורש 30 mm ל-SHS100 ו-14 mm ל-SHS40, ו-ASTM A385 דורש קצוות פתוחים לגמרי לחתכים שבהם H+W<203 mm, כך שברירת המחדל "12 mm" תייצר פריטים שהמגלוון יסרב לקבל. בנוסף יש לסמן במפורש כתלויי-סטנדרט/מדינה: d₀ (EN 1090-2 Table 11 מול נוהג בריטי של +2 mm), אנכיות עמוד (h/300 לפי EN 1090-2 מול 1:500 לפי AISC), notch 50 mm (נוהג BCSA בריטי), ומהדורת EN 1993-1-8 (קיימת מהדורת 2024 עם מספור שונה). המודול גם סותר את עצמו פי שלושה בעניין קיצור מכוון (2 / 5 / 5–10 mm) — חובה לאחד לפני הטמעה.

### ⚠️ תיקונים — הערך הנכון הוא זה שלמטה (עדיף על הגוף):
- **d0 = d+1 (M12,M14); d+2 (M16–M24); d+3 (≥M27), source 'EN 1993-1-8 Table 3.1'** → נכון: **Values are right, the source is wrong: these clearances are EN 1090-2:2018 Table 11, not EN 1993-1-8 Table 3.1 (which is bolt fyb/fub for classes 4.6–10.9).** — Verified against the primary text of BS EN 1090-2:2018 Table 11: normal round holes = 1 mm for d=12 and 14; 2 mm for 16, 18, 20, 22, 24; 3 mm for 27–36. Three footnotes the module drops and that chang
- **Typical fit-up gap between assembled parts 2–3 mm; agent may default to a declared 2 mm ga** → נכון: **For fillet-welded light steelwork a declared 2–3 mm gap already exceeds the ISO 5817 fit-up limit for normal throats: quality level C allows h ≤ 0,5 + 0,2a (max 3 mm) and level B h ≤ 0,5 + 0,1a (max 2 mm) for t > 3 mm — i.e. ~1,5 mm for a 5 mm fillet at level C, ~1,0 mm at level B.** — Primary text, ISO 5817:2014 imperfection 617 'Incorrect root gap for fillet welds', t > 3 mm: D h ≤ 1 + 0,3a max 4 mm; C h ≤ 0,5 + 0,2a max 3 mm; B h ≤ 0,5 + 0,1a max 2 mm. A blanket 2 mm 'safe defaul
- **Maximum root gap for fillet welds is 3 mm before the throat must be increased (EN 1090-2 §** → נכון: **EN 1090-2:2018 §7.5.8.1(b) states no 3 mm cap. It says: if a gap h exceeds the imperfection limit it may be compensated by increasing the throat, a = a_nom + 0,7h. The numeric gap limit comes from EN ISO 5817 imperfection 617 and is throat- and quality-level dependent (level C max 3 mm, level B max 2 mm).** — Quoted verbatim from BS EN 1090-2:2018 §7.5.8.1 'Fillet welds — General', item b). The module cites the right clause but invents the number and omits the one formula an agent actually needs (a = a_nom
- **Member straightness tolerance L/750 to L/1000, minimum 2 mm** → נכון: **EN 1090-2:2018 Table B.6 item 3 (manufacturing): straightness Δ = ±L/1000 but |Δ| ≥ 5 mm (class 1) / ≥ 3 mm (class 2) — the floor is 5 mm or 3 mm, not 2 mm. L/750 is a different thing: Table B.25, an ESSENTIAL ERECTION tolerance for beams in bending and components in compression that are unrestrained ('no functional tolerance specified').** — Both values verified in the primary text of BS EN 1090-2:2018. The module conflates a manufacturing tolerance with an erection tolerance and states a floor that is 2,5× too tight. ASTM A6 mill camber/
- **Column plumb: 1:500 of distance between working points; max 25 mm per storey, 50 mm total ** → נכון: **1:500 for an individual piece is correct. The 25/50 mm envelope is misdescribed: AISC 303 limits EXTERIOR columns to within 1 in (25 mm) TOWARD and 2 in (50 mm) AWAY FROM THE BUILDING LINE over the first 20 stories, increasing 1/32 in per additional story to a maximum of 2 in toward / 3 in away; interior columns 1 in either way. It is a cumulative envelope about a plan line, not 'per storey' + 'total'.** — Bigger flag: 1:500 is a US figure. For a fabricator working to EN 1090-2 the governing erection tolerance is Table B.18 — inclination of a column between adjacent storeys Δ = ±h/300 (essential, and cl
- **Fabricated member length tolerance ±3 mm for L ≤ 10 m; ±6 mm for L > 10 m (AISC 303 §6.4.1** → נכון: **AISC: ±1/16 in (1,6 mm) for members ≤ 30 ft and ±1/8 in (3,2 mm) for members > 30 ft — the module's figures are roughly double. Under EN 1090-2:2018 Table B.6 item 1: cut length on the centreline = ±(L/5000 + 2) mm class 1 and ±(L/10000 + 2) mm class 2 (so ±4 mm / ±3 mm at 10 m), and ±1 mm where ends are prepared for full contact bearing; item 2 allows ±50 mm where an adjacent component can compensate.** — AISC values from two independent reproductions of AISC 303; EN values from the primary BS EN 1090-2:2018 text. Also note AISC 303-22/-27 moved fabrication and erection tolerances into Section 11, so '
- **Cut-to-cut dimension and hole position tolerance ±1.5 mm (1/16 in) (AISC 303 §6.4.1/§6.4.2** → נכון: **For a European job use EN 1090-2:2018 Table B.8: individual hole within a group ±2 mm (class 1) / ±1 mm (class 2); position of hole group ±2 / ±1 mm; spacing between hole groups ±5 / ±2 mm (±2 / ±1 mm where one piece is connected by two groups); ovalisation ±1 / ±0,5 mm; notch depth and length −0 / +3 mm (class 1), −0 / +2 mm (class 2).** — ±1,5 mm is not an EN value, and the cited AISC clause is wrong — in AISC 303-16 §6.4 is 'Fabrication Tolerances' and §6.4.2 addresses curved members, not holes. Note also EN 1090-2 Table B.14 item 3: 
- **Maximum contact/bearing gap 3 mm for base plates and beam-to-column contact (AISC 303 §7.5** → נכון: **AISC 360 §M4.4: lack of contact bearing up to a gap of 1/16 in (2 mm) is permitted regardless of splice type; if the gap exceeds 2 mm but is ≤ 1/4 in (6 mm) and investigation shows insufficient contact area, it shall be packed with non-tapered steel shims. EN 1090-2 Table B.6 item 5 is far tighter for surfaces FINISHED for full contact bearing: gap 0,5 mm (class 1) / 0,25 mm (class 2), with no high spot proud by more than that.** — '3 mm' sits between the two real thresholds and belongs to neither standard; it appears to come from a secondary aggregator site (steelcalculator.app) whose clause numbers (§7.5.3, §7.13.4) I could no
- **ISO 13920 linear tolerance, class B / C: 400–1000 ±4/±6; 1000–2000 ±5/±8; 2000–4000 ±6/±10** → נכון: **EN ISO 13920 Table 1: 400–1000 → B ±3, C ±6; 1000–2000 → B ±4, C ±8; 2000–4000 → B ±6, C ±11. (Class A: ±2/±3/±4; class D: ±9/±12/±16.)** — Verified against a fabricator's controlled reproduction of the EN ISO 13920 tables (Groupe TMA INS-033.1, which reproduces all four classes across all eleven size bands). The module's class-B column i
- **ISO 13920 angular tolerance, class B / C: leg >400 mm ±10′/±20′; leg 120–400 mm ±20′/±30′** → נכון: **EN ISO 13920 Table 2, by length of the SHORTER leg — up to 400: A ±20′, B ±45′, C ±1°, D ±1°30′; >400 to 1000: A ±15′, B ±30′, C ±45′, D ±1°15′; >1000: A ±10′, B ±20′, C ±30′, D ±1°. There is no 120–400 mm band for angles.** — The module's numbers are the class A/B column mislabelled as B/C, i.e. 2–3× tighter than the standard, and the size bands are invented. Same source as above; the standard also gives the equivalent mm/
- **ISO 13920 form tolerance over 1000 mm: class A 3 mm, B 6 mm, C 12 mm, D 24 mm** → נכון: **Straightness/flatness/parallelism in EN ISO 13920 uses a SEPARATE class set E/F/G/H (not A–D). Table 3: >400–1000 → E 1,5, F 3, G 5,5, H 9 mm; >1000–2000 → E 2, F 4,5, G 9, H 14 mm.** — Independently corroborated by the primary text of BS EN 1090-2:2018 §11.3.3, which names the alternative criteria as 'class C for length and angular dimensions' and 'class G for straightness, flatness
- **Vent/drain holes for hot-dip galvanizing: minimum 12 mm diameter (10 mm absolute min); sin** → נכון: **For the sections this module actually names, EN ISO 14713-2:2020 Table 1 requires far larger holes: SHS/round 40 → 14 mm (one hole) or 12 mm (two holes); 50 → 16/12 mm; 80 or RHS100×60 → 25/16 mm; SHS100 or RHS120×80 → 30 mm single, 20 mm for two holes, ~14–15 mm for four; RHS160×80 → 35/25 mm. 10 mm is only the absolute health-and-safety minimum. ASTM A385's 25–30% of cross-sectional area applies only where full-open venting is impossible AND the section is large: per A385 Fig. 9, H+W ≥ 24 in → 25%, 16–24 in → 30%, 8–16 in → 40%, and H+W under 8 in (203 mm) must be LEFT COMPLETELY OPEN with no end plates or internal gussets — which covers SHS100×100 (H+W = 200 mm), while RHS150×100 (250 mm ≈ 9,8 in) falls in the 40% band.** — Verified from the primary ASTM A385/A385M-11 text (§12.3, §12.4) and from EN ISO 14713-2:2020 Table 1 as reproduced by the UK Galvanizers Association (HDG Datasheet 18). Two further items the module m

### ❓ טעון בירור:
- Intentional shortening of end-plate beams: 2 mm typical, 5 mm where fit-up problems antici — Could not confirm 2 mm or 5 mm in SCI/BCSA material. steelconstruction.info 'Simple connections' quantifies instead a nominal GAP between beam end and
- Notch clearance at supporting beam flange = t/2 + 2 mm — A matching sentence exists in SCI P358-derived material ('the clearance between the top flange of the main beam and the notched end of the secondary b
- EN 10219-2 mill tolerances: outside dimension ≈±1%, wall thickness ≈±10%, straightness ≈0. — I could not open EN 10219-2:2019 itself, only mill/stockist reproductions, so the exact straightness figure and the 2019-edition values are unverified
- Coordinate rounding discipline: work points on multiples of 5 mm; cut lengths rounded to 1 — Detailing convention, no standard basis; harmless as a default but dangerous as an override. EN 1090-2 hole and notch tolerances are ±1–2 mm, and the 
- Tank shell courses of 2015 mm; no more than 3 welds meeting at a junction (company fabrica — Internal Eretz Barzel rule — not externally verifiable and correctly labelled as such. Flag for the agent: the tank codes express the underlying conce
- ProSteel specifics: 9 reference points on the section bounding box, and Object View places — Software behaviour, not verified against Bentley ProSteel documentation in this review. It is plausible and internally consistent, but since the whole

> **הבהרה:** מודול לימוד/עזר. ערכים תלויי-תקן או נספח לאומי מסומנים ⚠ — האחריות ההנדסית על מהנדס מוסמך. **וכל פרויקט לגופו.**
