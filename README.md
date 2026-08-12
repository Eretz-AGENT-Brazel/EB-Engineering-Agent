# 🏗️ EB PROSTEEL AGENT

**Claude כממדל הפלדה של ארץ ברזל** — מפעיל AutoCAD 2015 + ProStructures (ProSteel) V8i SS6 באמת,
לא בסימולציה. המטרה, בניסוח של אמיר: *"שתיקח ממני את העבודה השחורה ותגיע לכל הפונקציות
הקיימות בתוכנה."*

---

## ⚡ מאיפה מתחילים

| רוצה... | קרא |
|---|---|
| **להבין לאן הפרויקט הולך** | [`agent-brain/PROGRAM.md`](agent-brain/PROGRAM.md) — תוכנית-העל של אמיר, שני השלבים, וחוק הרישום |
| **לראות מה נעשה ומה נשאר** | [`PROGRESS.md`](PROGRESS.md) — טבלת מעקב לפי פרקי המדריך |
| **להמשיך עבודה מאתמול** | [`knowledge/learning/RESUME-HERE.md`](knowledge/learning/RESUME-HERE.md) |
| **לדעת איך למדל בפועל** | הסקיל: `~/.claude/skills/prosteel-modeling/` — *זה מה שהסוכן טוען* |
| **להריץ** | `EB PROSTEEL AGENT.bat` · ואת AutoCAD פותחים ידנית עם פרופיל ProStructures |

---

## 🗺️ מפת הרשויות — לכל עובדה בית אחד

> ⭐⭐ **זה הלב של הסידור.** הכשל של 10/08/2026 לא היה חוסר סדר — הוא היה ש**לאותה עובדה
> היו כמה בתים ואף אחד לא שמר עליהם מסכימים**. הטבלה הזאת קובעת מי הרשות, וכל השאר מצביע אליה.

| סוג העובדה | **הרשות** | מי מצטט אותה |
|---|---|---|
| **איך מפעילים op** | `~/.claude/skills/.../references/plugin-ops.md` | הסקיל, ההערות |
| **מה התוכנה עושה בדיאלוג** | `knowledge/learning/manual/<חלק>/MANUAL-NOTES-*` | הסקיל |
| **מה ה-API מסרב לעשות** | `knowledge/learning/findings/THE-CEILING-what-code-cannot-reach.md` | הכול |
| **⛔ קריאות שהורגות את AutoCAD** | `knowledge/learning/findings/LETHAL-CALLS-do-not-invoke.md` | הסקיל |
| **מה הופרך ומתי** | `qc/retracted.tsv` — *וגם הדלק של השומר* | השומר |
| **מה נמדד בביקורת פרק** | `knowledge/learning/audits/AUDIT-*.md` | הערות הפרק |
| **המשטח הגולמי של ה-API** | `knowledge/api/API-SURFACE-RAW.txt` | הכול |
| **קטלוגי חתכים** | `knowledge/SECTION-CATALOGUES.md` | הסקיל, הזיכרון |
| **הידע ההנדסי** | `knowledge/steel/` | שלב 2 |
| **מי אמיר ואיך הוא עובד** | `~/.claude/projects/.../memory/` + `MEMORY.md` | כל סשן |

⚠️ **גרסת התוסף הקנונית היא `app/eb_api.py` ותו לא** — שם `DLL` ו-`RUN_CMD` מוצהרים.
כל מספר גרסה אחר במסמך כלשהו הוא ציטוט מתוארך, לא מקור.

---

## 📁 מבנה התיקייה

```
EB PROSTEEL AGENT/
├── PROGRESS.md             מעקב פרקים
├── README.md               הקובץ הזה — המפה
│
├── agent-brain/            🧠 המוח של הפרויקט
│   ├── PROGRAM.md          תוכנית-העל + חוק הרישום            ← אמיר קובע
│   ├── skill-prosteel-modeling/   גיבוי-מראה של הסקיל (sync.py — לא לערוך ידנית)
│   └── memory/             גיבוי-מראה של הזיכרון (sync.py — לא לערוך ידנית)
│
├── app/                    הקוד החי
│   ├── console.py          שרת הקונסולה (מוקפא 05/08)
│   ├── eb_api.py           ⭐ הגשר לתוסף — הרשות לגרסה
│   ├── acad.py             עטיפת COM
│   ├── context.py nlu.py prosteel.py learn.py standards_kb.py
│   ├── plugin/             🔒 לא להזיז — הנתיב צרוב ב-.cs המקומפל
│   │   ├── EBAgentApi<N>.cs    153 גרסאות = ההיסטוריה הכתובה של ה-API
│   │   └── eb_cmd/eb_result    ערוץ הפקודות (מתחלף בכל op)
│   └── _attic/             סקריפטים חד-פעמיים משיעורים שהסתיימו
│
├── knowledge/              ⭐ כל מה שנלמד
│   ├── api/                משטח ה-API הגולמי (רפלקציה)
│   ├── learning/
│   │   ├── RESUME-HERE.md      נקודת חזרה
│   │   ├── manual/A|B|E/       הערות לכל פרק במדריך
│   │   ├── audits/             ביקורות פרקים — מרשם ההפרכות
│   │   ├── findings/           THE CEILING · LETHAL CALLS
│   │   ├── lessons/            יומני שיעורים עם אמיר
│   │   └── plan/
│   ├── steel/ recipes/ research/
│   └── SECTION-CATALOGUES.md
│
├── qc/                     ⭐ השומרים
│   ├── consistency.py      ← להריץ לפני כל קומיט
│   ├── selftest_consistency.py
│   └── retracted.tsv       רשימת הטענות המופרכות
│
├── projects/               מודלים: SANDBOX + שיעור-N/ (כולל יומני בנייה)
├── _archive/handoffs/      מסמכי מסירה בין סשנים — היסטוריה מתוארכת
└── standards/ qc/ data/ assets/
```

---

## 🚦 לפני כל קומיט

```bash
python qc/consistency.py
```

עוצר אם: טענה מופרכת עומדת חיה · זיכרון לא באינדקס · גיבוי הסקיל לא תואם · גרסת תוסף שגויה ·
פרק בביקורת בלי סימון בהערות שלו. הנימוק המלא: [`agent-brain/PROGRAM.md`](agent-brain/PROGRAM.md) § חוק הרישום.

⚠️ **`agent-brain/sync.py` מגבה, הוא לא כותב.** ההודעה `backup already current` פירושה
"הגיבוי תואם", לא "הידע נשמר".

---

## 🧲 כללי ברזל

1. **בורג חייב לעבור דרך חור מדולל בכל אלמנט שהוא מחבר.** אין "חומר". החריג היחיד הוא בורג
   קודח, ורק בהצהרה מפורשת של אמיר לכל מקרה. שתיקה = שגיאה קריטית.
2. **LISP אסור.**
3. **מטרי תמיד.**
4. **לשמור את המודל אחרי כל שלב.**
5. **`sandbox.dwg` הוא של אמיר** — לא לשמור, לא לשנות, לא לסגור.
