# EN 1993-1-3 / 1-4 / 1-11 / 1-12 — חלקים משלימים ל-EC3 (חתכים מקופלים בקר, אל-חלד, אלמנטים במתיחה, פלדות בחוזק גבוה)

> מודול ייחוס פנימי לסיוע-תכן בלבד. מהנדס רשוי נושא באחריות המלאה. אין לצטט טקסט מוגן; להלן עובדות, מספרי/כותרות סעיפים והסברים מקוריים עם הפניה.

## 1. היקף ותחולה
ארבעת החלקים הם **supplementary/additional parts** ל-EN 1993-1-1: הם מרחיבים או משנים כללי-בסיס למקרים ספציפיים ואינם עומדים בפני עצמם.
- **EN 1993-1-3** — General rules; Supplementary rules for **cold-formed members and sheeting**. חל על אלמנטי פלדה מקופלים-בקר בעובי דק (profiles, purlins, cassettes, trapezoidal sheeting). שולט כאשר קיימת רגישות לקריסה מקומית (local), עיוותית (distortional) וגלובלית עקב דופן דקה. תחום העובי הגרעיני t_cor ≈ 0.45–15 mm (§1.1(3)); מחוץ לתחום זה נדרש אימות בבדיקות.
- **EN 1993-1-4** — **Stainless steel** structures. משנה כללי EC3 בגלל התנהגות מאמץ-מעוות **לא-לינארית** (Ramberg–Osgood), מודול נמוך יותר, וחיזוק-בעבודה (work hardening). מכסה austenitic, ferritic, duplex/lean-duplex.
- **EN 1993-1-11** — **tension components** (rods, ropes, strands, bundles). חל על אלמנטים במתיחה טהורה מיוצרים-מראש, בעיקר לגשרים תלויים/מיתרים וגגות מוטתים; אינו חל על גידי דריכה בבטון (EN 1992).
- **EN 1993-1-12** — **additional rules** להרחבת EN 1993 עד **S700** (מעל S460). שולט בכל שימוש בפלדת HSS מעבר לתחום EN 1993-1-1.

## 2. מפת סעיפים (כותרות/מספרים בלבד)
**1-3:** 1 General · 2 Basis of design · 3 Materials · 4 Durability · 5 Structural analysis (5.5 local & distortional buckling) · 6 ULS (6.1 cross-section, 6.2 buckling, 6.3 laterally restrained members) · 7 SLS · 8 Joints · 9 Design assisted by testing · 10 Special (sheeting, diaphragm, liner trays).
**1-4:** 1 General · 2 Basis / materials (2.1 grades, 2.3 γM) · 3 Durability · 4 SLS · 5 ULS (classification, resistance, buckling) · 6 Joints · 7 Fire · Annex A/B/C.
**1-11:** 1 General · 2 Basis of design (exposure classes) · 3 Materials (moduli) · 4 Durability/corrosion protection · 5 Structural analysis · 6 ULS · 7 SLS (stress limits) · 8 Vibration · 9 Fatigue · Annexes A–C.
**1-12:** 1 General · 2 Basis · 3 Materials · 4 Durability · 5 Structural analysis · 6 ULS · 7 Fatigue · plus clause-by-clause modifications to parts 1-1…1-11 and 2…6.

## 3. שיטות תכן ובדיקות (בלשוני)
**1-3 — Effective width / effective cross-section:** מודדים את השפעת הקריסה המקומית ע"י צמצום רוחב הלוחות הלחוצים לרוחב יעיל b_eff = ρ·b̄. תהליך: (1) λ̄_p = √(f_yb/σ_cr) = (b̄/t)/(28.4·ε·√k_σ); (2) ρ (per EN 1993-1-5) — פנים: ρ=(λ̄_p−0.055(3+ψ))/λ̄_p²≤1; זיז: ρ=(λ̄_p−0.188)/λ̄_p². **Distortional buckling** של מקשיח קצה/ביניים (§5.5.3): מודל קפיץ אלסטי K התומך במקשיח, ממנו σ_cr,s, ואז λ̄_d=√(f_yb/σ_cr,s) → χ_d: χ_d=1 ל-λ̄_d≤0.65; χ_d=1.47−0.723λ̄_d ל-0.65<λ̄_d<1.38; χ_d=0.66/λ̄_d ל-λ̄_d≥1.38. מפחיתים עובי/שטח יעיל של המקשיח: A_s,red=χ_d·A_s (או t_red=χ_d·t) בתהליך איטרטיבי. חיזוק-בעבודה: f_ya=f_yb+(f_u−f_yb)·k·n·t²/A_g ≤ (f_u+f_yb)/2, עם k=7 (roll forming, 5 אחרת), n=מספר כיפופי 90° ב-r≤5t (§3.2.2). בדיקות נוספות: web crippling, flange curling, torsional-flexural buckling, ε=√(235/f_yb).
**1-4 — התנהגות לא-לינארית:** אין "מדרגת כניעה"; f_y = R_p0.2. סיווג חתכים עם ε מתוקן ε=[235/f_y · E/210000]^0.5 (E=200 GPa). כפילות בבדיקות: (a) resistance עם f_y ו-γ_M0; (b) buckling עם עקומות מותאמות ל-stainless (α, λ̄_0 שונים מפחם); (c) SLS עם **secant modulus** E_S=E/[1+0.002(E/σ)(σ/f_y)^n] כדי לתפוס את ריכוך הנוקשות. חיבורים/net-section עם γ_M2.
**1-11 — resistance מתיחה:** ULS (§6.2): F_Ed/F_Rd ≤ 1, כאשר F_Rd = min{ F_uk/1.5 ; F_k/γ_R }, F_uk=כוח שבירה אופייני, F_k=כוח proof אופייני (Table 6.1). ל-Group B (ropes): F_uk = K·d²·R_r (K=breaking-force factor, d=קוטר, R_r=rope grade). ניתוח עם **secant modulus** (Group B/C) ואפקט catenary דרך מודול אפקטיבי E_t. SLS (§7.2): הגבלת מאמץ למחלק מ-σ_uk.
**1-12 — התאמות:** בדיקת דוקטיליות של החומר; net-section עם γ_M12; איסור ניתוח פלסטי מלא (מותר רק non-linear plastic עם פלסטיפיקציה חלקית ב-plastic zones); איסור capacity design; ריתוכי מילוי אורכיים ב-lap joints ≤ 50a.

## 4. פרמטרים וערכים מרכזיים (עובדות + הפניה)
**1-3:** t_cor 0.45–15 mm (§1.1(3)); f_ya per §3.2.2, k=7 roll-forming; χ_d לפי §5.5.3.2 (הענפים לעיל); γ_M0=1.0, γ_M1=1.0, γ_M2=1.25 (§2(3), מ-EN 1993-1-1). דרגות מצופות טיפוסיות: S220GD…S350GD (EN 10346), וכן S235–S355 (§3.1, Table 3.1).
**1-4:** E=200000 N/mm², G≈76900 N/mm², ν=0.3, ρ=7900 kg/m³ (§2.1.3). **γ_M0=1.1, γ_M1=1.1, γ_M2=1.25** (§2.3, recommended). f_y/f_u נומינליים (Table 2.1, תלוי product form — סדיל/פס-קר/פלטה): austenitic 1.4301 ≈ f_y 210–230 / f_u 520–540; 1.4307 ≈ 200–220/500–520; 1.4401 & 1.4404 ≈ 220–240/520–530; duplex 1.4462 ≈ 460–500/640–700; lean-duplex 1.4162/1.4362 ≈ 400–450/600–650; ferritic 1.4003 ≈ 250–280/450; 1.4016 ≈ 240–260/430–450.
**1-11:** E (Group A/rods) = 210000 N/mm² (§3.2.1); ropes/strands נמוך יותר — נומינלי ≈150000 (spiral strand) / ≈160000 (fully-locked coil) N/mm², secant מ-בדיקות (§3.2.2–3.2.3). F_Rd = min(F_uk/1.5, F_k/γ_R), γ_R (NDP) recommended 1.0 (§6.2). F_k: Group A → F_0.1k (EN 10138-1); Group B → proof per EN 10264; Group C → F_0.1k (Table 6.1). מגבלות מאמץ SLS: שלב הקמה 0.60/0.55·σ_uk (Table 7.1); שירות 0.50·σ_uk (עם כפיפה) / 0.45·σ_uk (בלעדיה) (Table 7.2); בדיקה 0.45·σ_uk. רדיוס אוכף r₁ ≥ max(30d, 400·σ...) (§6.4).
**1-12:** תחום S500/S550/S620/S690/S700 (EN 10025-6, Q&T) ו-S500MC…S700MC (EN 10149-2) (Tables 1–2). דוקטיליות (§3.2.2, recommended NDP): **f_u/f_y ≥ 1.05; elongation ≥ 10%; ε_u ≥ 15·f_y/E**. γ_M12 = 1.25 ל-net section (§6.2.3(2), eq 6.7a: N_u,Rd=0.9·A_net·f_u/γ_M12). מקדם הפחתה ל-fracture toughness (EN 1993-1-10) = 0.8. ריתוך: undermatched electrodes מותרים; longitudinal lap fillet ≤ 50a.

## 5. קשר ת"י ↔ EN
מכון התקנים הישראלי (SII) מאמץ את סדרת ה-Eurocodes; חלקי EN 1993 מאומצים כ-ת"י EN 1993-1-x עם **National Annex ישראלי** הקובע את ה-NDPs (בעיקר γ_M, ובחירת דרגות פלדה מותרות). ארבעת החלקים הללו מאומצים בדרך-כלל **בהפניה** דרך ה-NA. עומסים ופעולות: ת"י 412 (עומסים/רוח), ת"י 413 (רעידות אדמה), ובקרת ביצוע פלדה בהתאם ל-EN 1090 / ת"י מקביל. המיפוי ל-1-3/1-4/1-11/1-12 ישיר ברובו; ההתאמה הלאומית מצומצמת בעיקר לערכי γ_M ולרשימת הדרגות. **[ראה uncertainties — מספרי ת"י מדויקים טעונים אימות מול קטלוג SII].**

## 6. מהדורה ותוקף (נכון ליולי 2026)
- **EN 1993-1-3:2006 (+AC:2009)** — הגרסה בתוקף/מנדטורית ברוב ה-NAs; **EN 1993-1-3:2024** (דור-שני) פורסמה, בתקופת דו-קיום ויישום לאומי.
- **EN 1993-1-4:2006 (+A1:2015, +A2:2020)** — בתוקף; **EN 1993-1-4:2025** (דור-שני) פורסמה.
- **EN 1993-1-11:2006** — בתוקף; FprEN 1993-1-11:2025 → **EN 1993-1-11:2026** בהליכי אימות/פרסום.
- **EN 1993-1-12:2007 (+AC:2009)** — בתוקף; מהדורת דור-שני (מרחיבה מעל S700 בחלק מהחלקים) פורסמה. סטטוס: תקנים הרמוניים תחת CPR; מנדטוריות נקבעת ע"י ה-National Annex ורשות הרישוי המקומית.

## 7. הערות מעשיות ליצרן/מקים פלדה
- **1-3:** לתעד את t_cor (ללא ציפוי) בחישוב; ניצול f_ya מותר רק כאשר החתך אינו נשלט ע"י קריסה מקומית — לרוב מוותרים עליו לצד הבטוח. רדיוסי כיפוף גדולים מדי מבטלים את יתרון החיזוק-בעבודה; לבקר r/t.
- **1-4:** אל-חלד מתעוות יותר (מודול נמוך) — לצפות לשקיעות גדולות יותר ולתכנן ל-SLS. ריתוך austenitic דורש בקרת חום ומניעת sensitization/HAZ; להפריד כלי-עבודה מפלדת פחם (זיהום ברזל → קורוזיה). דרגת קורוזיה (CRC) לפי סביבה.
- **1-11:** כבלים דורשים pre-stretching (עד ~0.45·F_uk) לייצוב המודול לפני cutting-to-length; להקפיד על הגנת קורוזיה דו-שכבתית ל-Group C; מאמצי SLS ≤ 0.45–0.50·σ_uk כדי לשמור מרווח עייפות. עוגנים/אוכפים מתוכננים לכוח השבירה.
- **1-12:** S690/S700 רגישים לריכוזי מאמץ ולסדקים — להימנע מ-notches, לבקר toughness (EN 1993-1-10), ולהשתמש ב-heat-input נמוך; אין להסתמך על מפרקים חצי-קשיחים ולא על תכן פלסטי מלא; net-section עם γ_M12=1.25.

## 8. הפניות סעיף
EN 1993-1-3:2006 — §1.1(3), §3.1/Table 3.1, §3.2.2, §5.5.3.1–3.2, §6.1–6.2 (effective width per EN 1993-1-5 §4.4). EN 1993-1-4:2006(+A1/A2) — §2.1.3, §2.3, Table 2.1, §5 (classification, buckling, secant modulus). EN 1993-1-11:2006 — §1.3, Table 1.1, §3.2.1–3.2.3, §6.2 & eq 6.2 + Table 6.1, §7.2 + Tables 7.1/7.2. EN 1993-1-12:2007 — §1, §3.2.2, §5.4.3, §6.2.3(2) eq 6.7a, Tables 1–2.

---
## ✅ אימות אדוורסרי
רמת ביטחון: **🟢 גבוה** · אושרו: 16 · נפסלו: 0 · ספק: 1.

אומת: כמעט כל הטענות המספריות והקריטיות-לבטיחות נכונות. מקדמי חלקיות לאל-חלד (γM0=γM1=1.1, γM2=1.25), E=200000, חוזקי 1.4301 ו-1.4462, מקדמי חתכים מקופלים-בקר (γM0=γM1=1.0, γM2=1.25), נוסחת fya (k=7), ענפי χd (כולל 0.66/λd הליניארי — לא בריבוע), גבול S700, דרישות דוקטיליות (fu/fy≥1.05, התארכות≥10%, εu≥15fy/E) ו-γM12=1.25 — כולם CONFIRMED מול מקורות סמכותיים. שתי הערות קלות: (1) טווח tcor 0.45–15 מ"מ נכון אך הסעיף להמלצה הוא §3.2.4(1) ולא §1.1(3); (2) גבולות המאמץ ל-SLS ב-EN 1993-1-11 (0.45–0.60·σuk) סבירים בסדר-גודל אך לא אומתו במדויק, וסדר "עם/בלי כפיפה" (0.50 עם כפיפה מול 0.45 בלעדיה) נראה הפוך ומחייב אימות מול טבלאות 7.1/7.2. לא זוהתה העתקה מילולית של טקסט תקן מוגן.

### ❓ טעון בירור נוסף:
- EN 1993-1-11 SLS: service 0.50 σuk (with bending)/0.45 (without); construction 0.60/0.55 (Tables 7.1/7.2) — Could not access Tables 7.1/7.2 (only binary PDFs). General ~0.45 σuk service limit matches stay-cable practice, but 'with bending' having a HIGHER limit (0.50) than 'without' (0.45) is counterintuitive — verify ordering against the actual tables.

> 🔒 בדיקת זכויות יוצרים: No verbatim reproduction of copyrighted standard prose detected. The module reproduces equations (fya, χd branches) and clause titles/numbers — these are facts/formulae, not protected expression, and are presented with attribution and original explanation. Acceptable, but full tables (e.g., Table 2.1 strengths, Tables 7.1/7.2 stress limits) should continue to be given as referenced data points rather than copied verbatim.

> **סימון:** ✓ אומת מקובץ רשמי · ⭑ מבוסס-Eurocode (לא בהכרח הערך הישראלי המחייב) · ⚠ טעון אימות מול הנוסח המחייב/הנספח הלאומי. *כלי-עזר — האחריות על מהנדס מוסמך.*
