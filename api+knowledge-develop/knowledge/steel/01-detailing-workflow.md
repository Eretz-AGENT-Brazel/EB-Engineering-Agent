# תזרים העבודה של מתכנן/מפרט פלדה — מתוכנית לדגם

## למה זה חשוב

דגם פלדה הוא לא ציור — הוא **מסמך ייצור**. כל קו שגוי הופך לפלטה שנחתכה במידה לא נכונה, לחור שלא מתיישר באתר, ולמנוף שעומד יום. תזרים העבודה המקצועי (grid → WP → מפלסים → חיבורים → מספור → בדיקה → שרטוטים) הוא בדיוק מה שמבדיל דגם "שנראה נכון" מדגם שאפשר לחתוך ממנו פלדה. סדר הפעולות גם קובע את עלות השינוי: החלטה על מפלס TOS בשלב 1 עולה דקה; אותה החלטה אחרי מספור והפקת שרטוטים עולה שבוע עבודה ופלדה בזבל. מי שלא שולט בתזרים — יבנה דגם יפה שאי אפשר לייצר.

---

## 1. סדר הפעולות — ראשון, שני, שלישי

**שלב 0 — Job setup (לפני שנוצר אלמנט אחד):** יחידות (mm), דיוק, catalog של פרופילים, חומר ברירת מחדל (S235/S275/S355), סוגי ברגים (8.8 / A4-70), פרוטוקול מספור, תבניות שרטוט, וגמר (galvanized / paint system לפי ISO 12944).
**שלב 1 — Setting out:** בניית מערכת הצירים (grid) והמפלסים. שום אלמנט לא נוצר לפני זה.
**שלב 2 — Primary steel:** עמודים וקורות ראשיות, כל אחד על ה-work line שלו, בין WP ל-WP.
**שלב 3 — Secondary steel:** קורות משנה, purlins, bracing.
**שלב 4 — Connections:** פלטות, ברגים, ריתוכים, gussets, stiffeners, base plates.
**שלב 5 — Miscellaneous / light steel:** מדרגות, מעקות, סולמות, רשתות, קופינגים.
**שלב 6 — Numbering:** מספור single-part + assembly.
**שלב 7 — Checking & clash.**
**שלב 8 — Drawing output + BOM/NC.**

חשוב: **מספור הוא תמיד אחרי הגיאומטריה ואחרי החיבורים.** מספור לפני שהחיבורים סגורים מייצר מק"טים שקריים.

---

## 2. Grid, WP ו-work lines

**Grid:** אותיות בכיוון אחד (A, B, C) ומספרים בכיוון השני (1, 2, 3). חוק הזהב של הפירוט: **מודדים ומכתיבים מידות מקווי הצירים, לא מאלמנט לאלמנט** — כי הברגים העוגנים נקבעים לפי הצירים, והעמודים נקבעים לפי הברגים. שרשור מידות מאלמנט לאלמנט מצטבר לשגיאה.

**Working Point (WP):** נקודת ההצטלבות התיאורטית של קווי העבודה — לרוב חיתוך grid × מפלס. כל אלמנט נוצר **מ-WP ל-WP**, וה-work line שלו הוא הקו הצירי שהמתכנן חישב עליו.

**Insertion point / reference axis:** קריטי. RHS150X100X4 שמוכנס במרכז שונה בפועל מאותו פרופיל שמוכנס ב-Top-Center — הפרש 75mm במפלס. בפלטפורמות עובדים כמעט תמיד **Top of Steel = work line**, כלומר insertion ב-Top-Center, כדי שכל ה-RHS יהיו ישר-ישר מתחת לרצפה.

**אקסצנטריות בחיבורי profil חלולים:** בסבכות מ-RHS/CHS (למשל אלכסוני BSC21.3X2.5 על מיתר RHS150X100X4) קווי העבודה של האלכסונים צריכים להיפגש על ציר המיתר. אם לא — נוצר מומנט אקסצנטרי במיתר שחייב להיכנס לחישוב ⚠ (EN 1993-1-8 §7 / CIDECT Design Guide 3).

---

## 3. מפלסים: TOS / BOS / FFL

* **TOS** (Top of Steel) — פני הפלדה העליונים. השפה המקצועית של אמיר תהיה "TOS +4.250".
* **BOS** (Bottom of Steel) — תחתית הפלדה; רלוונטי לגובה חופשי (headroom) ולמפלס תחתית base plate.
* **FFL** (Finished Floor Level) — פני הרצפה הגמורה. **FFL ≠ TOS.**

הכלל: `TOS = FFL − עובי הריצוף/הגריל`. בפלטפורמה עם grating של 30mm: TOS = FFL − 30. עם checker plate 6mm: TOS = FFL − 6. אם הסוכן מקבל "מפלס הרצפה 4.250" הוא **חייב** לשאול: זה FFL או TOS?

---

## 4. שלושה דגמים ו-LOD

| דגם | מי מייצר | LOD | מה בפנים |
|---|---|---|---|
| **Design model** | מהנדס קונסטרוקציה | LOD 300–350 | קווי ציר, חתכים נכונים, חיבורים גנריים, כוחות |
| **Detailing model** | מפרט (detailer) | LOD 350–400 | חיבורים אמיתיים, פלטות, ברגים, ריתוכים, קיטומים |
| **Fabrication model** | בית המלאכה | LOD 400 (+500 as-built) | assemblies ממוספרות, NC/DSTV, nesting, HEAT |

הדגם של אמיר (175 פרופילים + 306 plates + 304 bolts) הוא **fabrication model** — יחס של ~1.75 פלטות ו-1.7 ברגים לכל פרופיל מעיד על חיבורים מפורטים לגמרי, לא על דגם תכנוני.

---

## 5. ארגון הדגם

* **Layers / classes:** הפרדה לפי תפקיד — PRIMARY, SECONDARY, BRACING, HANDRAIL, STAIR, GRATING, CONN-PLATE, BOLTS. אצל אמיר: U140 ו-300X12 יושבים ב-STAIR (זרים ומדרגות), EA60X60X6 ב-SECONDARY/frames, BSC21.3X2.5 ו-26,9X2,6 ב-HANDRAIL.
* **Phases / lots:** חלוקה לפי משלוח והתקנה. פלטפורמה שנשלחת בשבוע 1 היא PHASE 1 גם אם היא באותו בניין.
* **Assembly (shipping piece):** יחידה שמגיעה לאתר מרותכת כמכלול — למשל זרוע מדרגות: 2× U140 + 12× 300X12 מדרגות + פלטות. יש **main part** אחד (ה-U140) ואליו נתלים ה-single parts.
* **Sub-assembly:** תת-מכלול שמרותך בנפרד ואז מולחם למכלול הראשי — למשל מסגרת מעקה מ-BSC21.3X2.5.

---

## 6. מספור: part / assembly / piece marks

* **Single-part mark** (position number) — לכל חלק בודד: פרופיל+אורך+חורים+קיטומים. שני EA60X60X6 באורך 640 עם אותם חורים = אותו מספר. שינוי של 1mm באורך = מספר אחר.
* **Assembly mark** — למכלול השלם. קונבנציה נפוצה: single = `1, 2, 3…` או `p1, p2`; assembly = `B1, C1, S1` (Beam/Column/Stair) או `A1, A2`.
* **Piece mark** — המספר שנצבע פיזית על הפלדה ולפיו האתר מרכיב. בפועל = ה-assembly mark.
* ב-ProStructures/ProSteel המספור נעשה ב-`PS_POS` (Positioning), עם הפרדה בין Single Part Positioning ל-Main Part Positioning, prefixes נפרדים, ו-**Equal Part Settings / Compare Criteria** שקובעים מה נחשב "חלק זהה" (פרופיל, חומר, אורך, חורים, גמר). אם החומר לא נכנס ל-compare criteria — חלק מגולוון וחלק צבוע יקבלו אותו מספר. באג קלאסי.

---

## 7. בדיקת דגם לפני שחרור — checklist

1. **Clash / interference** — hard clash (חומר בחומר) ו-soft clash (מרווח התקנה, גישה למפתח ברגים ≥ 40mm סביב האום).
2. **Orphan check** — אלמנטים בלי חיבור, פלטות מרחפות, ברגים בלי חור.
3. **Connection completeness** — כל קצה אלמנט: או ריתוך או ברגים. אין קצה "באוויר".
4. **Grid conformity** — כל WP על צירים; אין אלמנט שהוזז ידנית ב-3mm.
5. **Numbering integrity** — אין מספר כפול, אין מספר חסר; BOM = ספירת הדגם.
6. **מפלסים** — כל TOS מול הרשימה של המהנדס.
7. **Fit-up** — מרווחים, קיטומים סביב ריתוכים, חורי אוורור/ניקוז לגלוון.
8. **Weight & CoG** לכל assembly — למנוף ולמשלוח.

---

## 8. תוצרי שרטוט

* **GA / Erection drawings** — תוכניות ותשקיפים עם grid, מפלסים ו-piece marks. לאתר.
* **Assembly / Shop drawings** — שרטוט לכל מכלול: כל החלקים, ריתוכים, ברגים, מידות מקצה.
* **Single-part drawings** — לחיתוך ולקידוח (בפועל מוחלף ב-NC/DSTV למכונה).
* **Anchor bolt plan / setting-out plan** — תוכנית ברגים עוגנים, המסמך הראשון שיוצא מהפרויקט כי הוא צריך להגיע ליציקה.
* **BOM / cutting list / bolt list** + מסמכי חומר (EN 10204 3.1, HEAT).

---

## 9. פלדה קלה מול שלד כבד — למה זה שונה

התמהיל של אמיר (RHS/SHS חלולים, EA60X60X6, צינורות CHS דקים, שטוחים 300X12, U140, אורכים 200–7000) הוא **פלדה קלה**: פלטפורמות, מדרגות, מעקות, מסגרות. ההבדלים המעשיים:

| | שלד כבד | פלדה קלה (אמיר) |
|---|---|---|
| חיבורים | ברגים, פלטות עבות, מומנט | **ריתוך בבית מלאכה**, ברגים רק בממשקי משלוח |
| דומיננטי | חוזק, יציבות, מומנט | **גיאומטריה, בטיחות, מראה** |
| grid | מחייב ומדויק | לוקאלי; לעיתים "קו פלטפורמה" במקום grid |
| assemblies | חלק בודד גדול | **מכלולים מרותכים גדולים** (זרוע מדרגות שלמה) |
| מגביל | חוזק חתך | **גודל אמבט הגלוון ומידות משאית** |
| תקן | EN 1993-1-1 | ת"י 1142 (מעקים), EN ISO 14122 (גישה למכונות) |

לכן בפלדה קלה: הכלל הראשון הוא **בטיחות ומידות תקן** (גובה מעקה, מרווחים, רום/שלח), הכלל השני הוא **התקנה** (האם המכלול נכנס לאמבט ולמשאית), והחוזק לרוב לא קריטי. בשלד כבד ההיפך.

---

## כללי אצבע מקצועיים

ראה רשימת `keyRules` — כולל edge distance, מרווחי fit-up, סובלנויות, חורי גלוון, גבהי מעקה ומידות מדרגות.

---

## טעויות נפוצות

1. **בלבול FFL/TOS** — הפלטפורמה יוצאת גבוהה בעובי הגריל. תמיד לאמת.
2. **Insertion point שגוי** — RHS150X100X4 שמוכנס Center במקום Top-Center = 75mm שגיאה מערכתית בכל הפלטפורמה.
3. **שרשור מידות** — מודדים מאלמנט לאלמנט ולא מה-grid; השגיאה מצטברת ל-15mm בסוף השורה.
4. **מספור לפני שהחיבורים סגורים** — כל מק"ט צריך מספור מחדש והשרטוטים לא תואמים.
5. **Compare criteria חלקי** — חלק מגולוון וחלק צבוע מקבלים אותו piece mark.
6. **שכחת חורי אוורור/ניקוז** ב-RHS/SHS/CHS לפני גלוון — פיצוץ באמבט או כיסי אבץ.
7. **אקסצנטריות לא מטופלת** בחיבורי CHS/RHS — מומנט לא מחושב במיתר.
8. **gap = 0 בחיבורי K** — אין מקום לריתוך, הצלעות נחתכות זו לזו.
9. **מכלול שלא נכנס למשאית / לאמבט** — מתגלה ביום המשלוח.
10. **אין גישה למפתח ברגים** — הבורג מודל אבל אי אפשר להדק אותו באתר.
11. **מודלים דגם "לתמונה"** במקום למדידה — אלמנטים באורך "בערך", בלי קיטומים.

---

## מה זה אומר לסוכן

**לפני יצירת כל אלמנט הסוכן חייב לדעת 8 פרמטרים:**
1. Catalog + profile (למשל `BRITAIN.BS_CELSIUS_RHS / RHS150X100X4`)
2. שתי נקודות: start WP + end WP (קואורדינטות מוחלטות מה-grid)
3. Insertion / reference point (Center / Top-Center / Bottom-Left)
4. Rotation סביב הציר האורכי
5. חומר (S235 / S275 / S355)
6. Layer / class ו-Phase
7. גמר (galvanized / painted) — משפיע על מספור ועל חורי אוורור
8. תפקיד: main part או single part במכלול

**מה לשאול את אמיר (אל תנחש):**
* המפלס הוא FFL או TOS? מה עובי הריצוף/הגריל?
* מה מערכת הצירים ומאיפה ה-origin?
* מה גבולות המכלול למשלוח (אמבט גלוון, מידות משאית)?
* חומר וגמר; דרגת EXC לפי EN 1090-2 ⚠ (EXC2 היא ברירת המחדל של התקן).
* קוטר ודרגת ברגים.
* האם החיבורים מרותכים בבית מלאכה או מוברגים באתר?

**מה מותר להגדיר כברירת מחדל (ולציין בקול):** יחידות mm; חומר S275; hole clearance 2mm ל-M12–M20; fit-up gap 2–3mm; שני ברגים מינימום בחיבור; layer לפי סוג פרופיל; מספור אחרי סיום הגיאומטריה.

**בדיקות שהסוכן חייב להריץ אחרי מודלינג:** clash hard+soft · אלמנטים ללא חיבור · כל קצה מחובר · כל WP על grid · מספור ללא כפילויות · השוואת BOM לספירת הדגם · לכל RHS/SHS/CHS סגור — חור אוורור אם הגמר גלוון · gap ≥ t1+t2 בכל חיבור K · משקל וממדי envelope לכל assembly.

**כלל אחרון:** כשהסוכן לא בטוח בערך שתלוי בתקן או ב-National Annex — הוא **לא ממציא**. הוא מציין ⚠ עם התקן והסעיף ומבקש אישור מאמיר.

## 📐 כללי אצבע (טבלה מרוכזת)

| כלל | ערך | חל על | מקור |
|---|---|---|---|
| מרחק מינימלי של חור מקצה בכיוון הכוח (end distance) | **e1 ≥ 1.2·d0** | כל חיבור מוברג — פלטות, gussets, base plates | EN 1993-1-8 Table 3.3 |
| מרחק מינימלי של חור מקצה בניצב לכוח (edge distance) | **e2 ≥ 1.2·d0** | כל חיבור מוברג | EN 1993-1-8 Table 3.3 |
| מרווח מינימלי בין חורים בכיוון הכוח (pitch) | **p1 ≥ 2.2·d0** | שורות ברגים | EN 1993-1-8 Table 3.3 |
| מרווח מינימלי בין חורים בניצב לכוח | **p2 ≥ 2.4·d0** | שורות ברגים | EN 1993-1-8 Table 3.3 |
| Normal clearance hole — הפרש קוטר חור לבורג | **+1mm ל-M12; +2mm ל-M14–M24; +3mm ל-M27 ומעלה (M16→d0=18, M20→d0=22)** | קידוח פלטות ופרופילים | EN 1090-2 §6.6 / EN 1993-1-8 Table 3.3 |
| מינימום ברגים בחיבור מוברג קונסטרוקטיבי | **2 ברגים** | כל חיבור אתר | נוהג מקצועי (BCSA/NSSS) |
| מרווח fit-up טיפוסי בין אלמנט לפלטה | **2–3mm** | חיבורי גזירה, fin plates | נוהג מקצועי; מוגדר בסובלנויות EN 1090-2 Annex B |
| מרווח מינימלי סביב אום לגישה עם מפתח | **≥ 40mm רדיוס פנוי** | בדיקת soft clash | נוהג התקנה (BCSA) |
| סובלנות ישרות (straightness) — mill tolerance לפרופיל מגולגל | **L/1000 (camber ו-sweep)** | קורות ועמודים מפרופיל תקני | ASTM A6; מקביל ב-EN 10034 / EN 10210-2 |
| סובלנות ישרות בייצור — essential/functional | **סדר גודל L/750 עד L/1000, מינימום 2mm ⚠ הערך המדויק תלוי בטבלה ובמחלקה** | פרופילים מרותכים ומכלולים | ⚠ EN 1090-2:2018 Annex B, Tables B.1–B.14 (Class 1 = ברירת מחדל, Class 2 = מהודק) |
| סובלנות אורך אלמנט מיוצר | **±3mm עד 10m; ±6mm מעל 10m** | חיתוך במסור/מכונה | AISC 303 (cross-check: EN 1090-2 Annex B) |
| סובלנות מיקום חור | **±1.5mm מהנומינלי** | קידוח | AISC 303 |
| סובלנות אנכיות עמוד (plumb) לקומה | **1/500 מגובה הקומה, מקסימום 25mm; סה"כ עד 50mm** | הקמה באתר | AISC 303 (cross-check: EN 1090-2 Annex B Tables B.15–B.25) ⚠ |
| סובלנות מפלס קורה בהקמה | **±5mm מהתיאורטי** | TOS באתר | AISC 303 |
| סובלנות מיקום בורג עוגן | **±6mm ממרכז** | anchor bolt plan | AISC 303 |
| מרווח מקסימלי מתחת ל-base plate לפני grout | **≤ 3mm** | מישוריות שטח מסעד | AISC 303 |
| מרווח (gap) מינימלי בחיבור K של פרופילים חלולים | **g ≥ t1 + t2 (סכום עובי דפנות שתי הצלעות)** | סבכות RHS/SHS/CHS — למשל BSC21.3X2.5 על RHS150X100X4 | EN 1993-1-8 §7 / CIDECT Design Guide 3 |
| טווח overlap מותר בחיבור K עם חפיפה | **25% ≤ λov ≤ 100%** | חיבורי overlap בפרופילים חלולים | ⚠ EN 1993-1-8 §7 Tables 7.x / CIDECT DG1 & DG3 |
| אקסצנטריות מותרת בצומת סבכה לפני שחייבים לחשב מומנט | **⚠ תלוי בגובה המיתר; יש לבדוק את הסעיף — אקסצנטריות מחוץ לתחום מחייבת הכללת המומנט בחישוב המיתר** | כל צומת שבו קווי העבודה לא נפגשים | ⚠ EN 1993-1-8 §7.1.2 / CIDECT DG3 |
| שטח חורי אוורור מינימלי בפרופיל חלול לגלוון | **≥ 25% משטח חתך הפרופיל** | RHS150X100X4, 50X3.0SHS, BSC21.3X2.5 סגורים | Galvanizers Association (UK) / GAA Australia — Venting & Drainage |
| קוטר מינימלי לחור אוורור | **מועדף 12mm; לא פחות מ-8mm** | פרופילים חלולים לפני גלוון | Galvanizers Association / GAA |
| קוטר מינימלי לחור ניקוז | **≥ 10mm, בנקודה הנמוכה בעת התלייה** | פרופילים חלולים ומכלולים סגורים | Galvanizers Association / GAA |
| גובה מעקה תקני — חלק ישר | **≥ 105 cm** | מעקות פלטפורמות, מרפסות, גלריות | ת"י 1142 |
| גובה מעקה לאורך החלק המשופע של מדרגות | **≥ 90 cm** | זרועות מדרגות | ת"י 1142 |
| מרווח מקסימלי בין רכיבי מעקה | **≤ 10 cm** | מילוי מעקה מצינורות CHS 26,9X2,6 / BSC21.3X2.5 | ת"י 1142 |
| מפלס פלדה מול רצפה גמורה | **TOS = FFL − עובי ריצוף/גריל (grating 30mm → TOS = FFL − 30)** | כל פלטפורמה | נוהג פירוט |
| דרגת ביצוע ברירת מחדל | **EXC2 — אלא אם המפרט דורש EXC3/EXC4 במפורש ⚠** | כל פרויקט תחת EN 1090 | EN 1090-2:2018 |
| סובלנויות חלופיות למכלולים מרותכים בכבדות | **ISO 13920 Classes A/B/C/D** | מכלולים מרותכים, מכלי פלדה | EN ISO 13920 (כאלטרנטיבה שמאזכר EN 1090-2) |
| LOD נדרש לפי סוג דגם | **design 300–350 · detailing 350–400 · fabrication 400** | הגדרת ציפיות מול המהנדס | AIA/BIMForum LOD + נוהג פירוט |


## 🤖 מה זה אומר לסוכן

- לפני יצירת Ks_Shape יש לדעת 8 פרמטרים: catalog+profile, start WP, end WP, insertion/reference point, rotation, material, layer+phase, ותפקיד במכלול (main/single part)
- insertion point הוא מקור השגיאות המערכתי הגדול: RHS150X100X4 ב-Center מול Top-Center = הפרש 75mm בכל הפלטפורמה. בפלטפורמות ברירת המחדל היא Top-Center כדי ש-TOS יהיה קו העבודה
- תמיד לשאול אם מפלס שניתן הוא FFL או TOS, ומה עובי הריצוף/גריל; לא להניח
- כל אלמנט נוצר מ-WP ל-WP שנגזרים מה-grid — אין הזזות ידניות ואין שרשור מידות מאלמנט לאלמנט
- מספור (PS_POS) מורץ רק אחרי שהגיאומטריה והחיבורים סגורים; compare criteria חייב לכלול חומר וגמר, אחרת מגולוון וצבוע יקבלו אותו piece mark
- להפריד single-part numbering מ-main-part (assembly) numbering עם prefixes נפרדים
- לתייג כל אלמנט ל-layer/class לפי תפקיד (PRIMARY / SECONDARY / BRACING / STAIR / HANDRAIL / GRATING / CONN-PLATE / BOLTS) ולפי phase משלוח
- בפלדה קלה המכלול המרותך הוא היחידה הלוגית (זרוע מדרגות = U140 main part + 300X12 מדרגות + פלטות) — לא האלמנט הבודד
- checklist אוטומטי אחרי מודלינג: clash hard+soft, אלמנטים ללא חיבור, כל קצה מחובר, כל WP על grid, מספור ללא כפילויות, BOM מול ספירת דגם, משקל ו-envelope לכל assembly
- אם הגמר גלוון — לבדוק חור אוורור ≥12mm ושטח כולל ≥25% מחתך בכל פרופיל חלול סגור (RHS/SHS/CHS)
- בסבכות פרופילים חלולים לוודא gap ≥ t1+t2 ולאמת שקווי העבודה של האלכסונים נפגשים על ציר המיתר; אחרת לדווח על אקסצנטריות
- לבדוק שכל assembly נכנס לאמבט הגלוון ולמידות משאית לפני שמכריזים על הדגם כמוכן
- anchor bolt plan הוא התוצר הראשון שיוצא — לייצר אותו לפני שרטוטי מכלול
- ערכים שתלויים בתקן או National Annex מסומנים ⚠ ומובאים עם התקן והסעיף; הסוכן לא ממציא ערך ומבקש אישור מאמיר


---
## ✅ אימות אדוורסרי
רמת ביטחון: **🟡 בינוני** · אושרו: 15 · **נפסלו: 6** · ספק: 9

הבדיקה מאשרת את ליבת המודול: מרווחי הברגים לפי EN 1993-1-8 Table 3.3 (1.2/1.2/2.2/2.4·d0), gap ≥ t1+t2 ו-25%≤λov≤100% בחיבורי K, EXC2 כברירת מחדל (סעיף 4.1.2), Class 1 כברירת מחדל בסובלנויות התפקודיות, ISO 13920, וגבהי המעקה לפי ת"י 1142 (105/90/10 ס"מ). לעומת זאת נמצאו חמישה ערכים שגויים שמסוכן להזין לסוכן מודלינג: מרווח fit-up ל-fin plate הוא 10mm נומינלי לפי SCI/BCSA ולא 2–3mm; שטח חורי הגלוון נקבע לפי קוטר ≥25% מאורך האלכסון של החתך ולא 25% משטח החתך, והמינימום המוחלט לחור אוורור הוא 10mm ולא 8mm; אין ב-AISC 303 כלל של "≤3mm מתחת ל-base plate" (הכלל האמיתי הוא M4.4 בתקן AISC 360 — 2mm חוסר מגע, ומרווח הגראוט המתוכנן הוא 25–50mm); וסובלנות האורך ±3/±6mm היא המקרה הרפוי בלבד — לאלמנט שמתחבר לפלדה אחרת התקן דורש ±1.6mm עד 30ft (9.14m, לא 10m). בנוסף יש בעיית מקוריות שיטתית: המודול מייחס סובלנויות אמריקאיות (AISC 303, ASTM A6) לפרויקט אירופאי — ישרות תקנית לפרופילים אירופיים היא 0.30%L ל-h≤180 לפי EN 10034 ו-0.2%L לפרופילים חלולים לפי EN 10210-2/10219-2, כלומר רפויה פי 2–3 מ-L/1000; וכן ההפרש בין M14 ל-M12 בטבלת מרווחי החורים (EN 1090-2 Table 11 מקבץ M12+M14 ב-1mm) חייב אימות מול נוסח התקן לפני שימוש.

### ⚠️ תיקונים — הערך הנכון הוא זה שלמטה (עדיף על הגוף):
- **Typical fit-up gap between member and plate = 2–3 mm, for shear connections and fin plates** → נכון: **Fin plates (SCI/BCSA 'Green Book' P358): nominal gap gh = 10 mm between beam end and supporting face for beams up to ~610 mm deep (larger, ~25 mm, sometimes used for deeper beams)** — steelconstruction.info/Simple_connections and SCI fin-plate design examples all use 10 mm. 2–3 mm is a welding/root-gap or shim figure, not a fin-plate fit-up gap. Feeding 2–3 mm to a detailing agent 
- **Mill straightness tolerance for rolled profiles = L/1000 (camber and sweep), 'ASTM A6, par** → נכון: **ASTM A6: camber ≈ 1/8 in per 10 ft ≈ L/960, but SWEEP for sections with narrow flanges is 1/4 in per 10 ft ≈ L/480. EN sections are much looser: EN 10034 straightness = 0.30% L (L/333) for h ≤ 180 mm, 0.15% L for 180 < h ≤ 360, 0.10% L for h > 360; EN 10210-2/10219-2 hollow sections = 0.2% L (L/500), plus 3 mm over any 1 m** — Steel for Life Blue Book 'Rolling tolerances to BS EN 10034'; BS EN 10210-2 Table 2. The 'parallel in EN' claim is the real problem: Amir's stock is European/British Celsius sections (RHS, SHS, CHS, U
- **Fabricated length tolerance ±3 mm up to 10 m; ±6 mm over 10 m (AISC 303)** → נכון: **AISC 303: members FRAMING to other structural steel — ±1/16 in (1.6 mm) for length ≤ 30 ft, ±1/8 in (3.2 mm) for > 30 ft. Members NOT framing to other steel — ±1/8 in and ±1/4 in (6 mm). Threshold is 30 ft = 9.14 m, not 10 m** — AISC 303 detailed-length clause (303-16 §6.4.1 / 303-22 §11.2.1). The module quotes the loose 'non-framing' pair as if it applied to everything; for a beam that must bolt into a fin plate the allowanc
- **Maximum gap under a base plate before grouting ≤ 3 mm (AISC 303)** → נכון: **No such rule in AISC 303. The bearing-fit rule is AISC 360 §M4.4: lack of contact up to 1/16 in (2 mm) is permitted; gaps > 1/16 in and ≤ 1/4 in (6 mm) must be packed with non-tapered steel shims. Designed grout space under a base plate is normally 25–50 mm (AISC Design Guide 1)** — As written the rule confuses contact-bearing fit with grout thickness and would make the agent flag every normal grouted base plate as an error.
- **Galvanizing vent hole area ≥ 25% of the profile's cross-sectional area** → נכון: **GAA / Galvanizers Association: at least one vent and one drain hole per hollow section, each with a DIAMETER equivalent to ≥ 25% of the diagonal cross-section dimension (or multiple holes of equivalent total area). For RHS150×100 (diagonal ≈ 180 mm) that means ≈ 45 mm — not '25% of the section area'** — gaa.com.au Venting & Draining Guide states 25% of the diagonal cross-section LENGTH. The module's wording is also ambiguous (steel area vs enclosed area) — for RHS150×100×4 it yields anything from ~24
- **Minimum vent hole diameter: 12 mm preferred, not less than 8 mm** → נכון: **Ideal minimum 12 mm; NO vent hole smaller than 10 mm unless agreed with the galvanizer (Galvanizers Association UK; GAA baseline 10 mm, up to 50 mm for large enclosed volumes)** — The 8 mm floor is below every published minimum found. 12 mm 'preferred' is right; replace 8 mm with 10 mm.

### ❓ טעון בירור:
- Normal clearance hole: +1 mm for M12; +2 mm for M14–M24; +3 mm for M27+ (M16→18, M20→22) — The M16–M24 = +2 mm and M27+ = +3 mm parts are consistent with every source checked. Putting M14 in the +2 mm band is almost certainly wrong — Table 1
- ≥ 40 mm clear radius around a nut for wrench access (soft clash) — No BCSA/normative '40 mm' rule found. 40 mm is a reasonable conservative rule of thumb for M16–M20 but must not be presented as a standard value, and 
- EN 1090-2 fabrication straightness ≈ L/750 to L/1000, min 2 mm; Class 1 = default, Class 2 — The Class 1 = default / Class 2 = stricter statement is CONFIRMED (steelconstruction.info 'Accuracy of steel fabrication': 'Class 1 being the less one
- Hole position tolerance ±1.5 mm from nominal (AISC 303) — Could not locate a ±1.5 mm value anywhere in AISC 303 — the attribution looks invented even if the magnitude is plausible. Under EN 1090 the governing
- Column plumb 1/500 of storey height, max 25 mm, total up to 50 mm (AISC 303) — civilengineeringx 'Field Tolerances' and AISC Fig. C-7.5 confirm the 1/500 ratio but the 25/50 mm figures are not 'max per storey / max total' — they 
- Erected beam level tolerance ±5 mm from theoretical (AISC 303) — No ±5 mm beam-elevation value found in AISC 303; ±5 mm reads like a BCSA/NSSS erection figure. Attribution unverified — the agent should not cite AISC
- Anchor bolt position tolerance ±6 mm from centre (AISC 303) — The ±6 mm applies group-to-gridline, not rod-to-rod. Using 6 mm inside a group would oversize base-plate holes wrongly; the intra-group tolerance is h
- LOD: design 300–350 · detailing 350–400 · fabrication 400 (with 500 = as-built) — Convention, not a standard. BIMForum/AIA LOD 100–500 exists, but LOD 500 is defined as field-VERIFIED as-built, not a higher geometric level, and the 
- Insertion at Center vs Top-Center of RHS150X100X4 = 75 mm difference — True only when the 150 mm dimension is vertical. If the section is laid with 100 mm vertical (common for platform framing) the offset is 50 mm. Presen

> **הבהרה:** מודול לימוד/עזר. ערכים תלויי-תקן או נספח לאומי מסומנים ⚠ — האחריות ההנדסית על מהנדס מוסמך. **וכל פרויקט לגופו.**
