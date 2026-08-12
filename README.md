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
| **להמשיך עבודה מאתמול** | [`API+KNOWLEDGE-DEVELOP/knowledge/learning/RESUME-HERE.md`](API+KNOWLEDGE-DEVELOP/knowledge/learning/RESUME-HERE.md) |
| **לדעת איך למדל בפועל** | הסקיל: `~/.claude/skills/prosteel-modeling/` — *זה מה שהסוכן טוען* |
| **להריץ** | `EB PROSTEEL AGENT.bat` · ואת AutoCAD פותחים ידנית עם פרופיל ProStructures |

---

## 🛤️ שני נתיבי הפיתוח — הסידור של אמיר, 12/08/2026

| נתיב | איפה | מצב |
|---|---|---|
| **נתיב 1 — API וידע הנדסי** | [`API+KNOWLEDGE-DEVELOP/`](API+KNOWLEDGE-DEVELOP/README.md) | 🔵 פעיל — כאן קורה הכול |
| **נתיב 2 — ידיעת התקנים** | `standards/` | 🔒 נעול עד הכרזת אמיר (פאזה 2) |

מנתיב 1 ייבנה בסוף **המוצר**: הקונסולה/פלאגין שיוטמע רשמית במחשבי החברה — הסוכן
בתוך התוכנה, ממדל ופותר בעיות הנדסיות. גם כל השיפורים והעדכונים בהמשך יבוצעו בתוכו.

⚠️ **מוסכמת נתיבים:** במסמכים שבתוך `API+KNOWLEDGE-DEVELOP/` (הסקיל, ההערות, הביקורות),
נתיבים כמו `app/plugin/...` או `knowledge/learning/...` הם **יחסיים לתיקיית הנתיב** —
לא לשורש הריפו.

---

## 📁 מבנה התיקייה

```
EB PROSTEEL AGENT/
├── README.md               הקובץ הזה — המפה
├── PROGRESS.md             מעקב פרקי המדריך
├── EB PROSTEEL AGENT.bat   הלאנצ'ר (קונסולה — מוקפאת 05/08)
│
├── agent-brain/            🧠 עיקרון הפרויקט
│   ├── PROGRAM.md          תוכנית-העל + חוק הרישום            ← אמיר קובע
│   ├── skill-prosteel-modeling/   גיבוי-מראה של הסקיל (sync.py — לא לערוך ידנית)
│   └── memory/             גיבוי-מראה של הזיכרון (sync.py — לא לערוך ידנית)
│
├── API+KNOWLEDGE-DEVELOP/  ⭐ נתיב פיתוח 1 — יש לה README משלה
│   ├── app/                הקוד: ה-API, התוסף (plugin/), הקונסולה
│   ├── qc/                 השומרים: consistency.py + selftest + שלוש הרשימות
│   ├── knowledge/          הידע: הערות המדריך, ממצאים, מפת ה-API, פלדה
│   ├── lessons/            שיעורים 1–5: תיעוד, לקחים, ביקורות
│   ├── projects/           המודלים: SANDBOX (פרקים A–E) + שיעור-1..5
│   ├── research/           מחקרים הנדסיים — ביסוס ועיגון עמודי פלדה
│   └── data/ · assets/     מצב הקונסולה + אייקונים
│
├── standards/              🔒 נתיב פיתוח 2 — תקנים (פאזה 2, לא נוגעים)
└── Z-ARCHIVE/handoffs/     מסמכי מסירה בין סשנים — היסטוריה מתוארכת
                            (ה-Z מכוונת: שומרת את הארכיון אחרון במיון של GitHub)
```

---

## 🗺️ מפת הרשויות — לכל עובדה בית אחד

> ⭐⭐ **זה הלב של הסידור.** הכשל של 10/08/2026 לא היה חוסר סדר — הוא היה ש**לאותה עובדה
> היו כמה בתים ואף אחד לא שמר עליהם מסכימים**. הטבלה הזאת קובעת מי הרשות, וכל השאר מצביע אליה.
> הנתיבים כאן מהשורש; `DEV/` = ‏`API+KNOWLEDGE-DEVELOP/`.

| סוג העובדה | **הרשות** | מי מצטט אותה |
|---|---|---|
| **איך מפעילים op** | `~/.claude/skills/.../references/plugin-ops.md` | הסקיל, ההערות |
| **מה התוכנה עושה בדיאלוג** | `DEV/knowledge/learning/manual/<חלק>/MANUAL-NOTES-*` | הסקיל |
| **מה ה-API מסרב לעשות** | `DEV/knowledge/learning/findings/THE-CEILING-what-code-cannot-reach.md` | הכול |
| **⛔ קריאות שהורגות את AutoCAD** | `DEV/knowledge/learning/findings/LETHAL-CALLS-do-not-invoke.md` | הסקיל |
| **מה הופרך ומתי** | `DEV/qc/retracted.tsv` — *וגם הדלק של השומר* | השומר |
| **מה נמדד בביקורת פרק** | `DEV/knowledge/learning/audits/AUDIT-*.md` | הערות הפרק |
| **המשטח הגולמי של ה-API** | `DEV/knowledge/api/API-SURFACE-RAW.txt` | הכול |
| **קטלוגי חתכים** | `DEV/knowledge/learning/findings/SECTION-CATALOGUES.md` | הסקיל, הזיכרון |
| **הידע ההנדסי** | `DEV/knowledge/steel/` + `DEV/research/` | שלב 2 |
| **מי אמיר ואיך הוא עובד** | `~/.claude/projects/.../memory/` + `MEMORY.md` | כל סשן |

⚠️ **גרסת התוסף הקנונית היא `DEV/app/eb_api.py` ותו לא** — שם `DLL` ו-`RUN_CMD` מוצהרים.
כל מספר גרסה אחר במסמך כלשהו הוא ציטוט מתוארך, לא מקור.

---

## 🚦 לפני כל קומיט

```bash
python "API+KNOWLEDGE-DEVELOP/qc/consistency.py"
```

עוצר אם: טענה מופרכת עומדת חיה · זיכרון לא באינדקס · גיבוי הסקיל לא תואם · גרסת תוסף שגויה ·
פרק בביקורת בלי סימון בהערות שלו. הנימוק המלא: [`agent-brain/PROGRAM.md`](agent-brain/PROGRAM.md) § חוק הרישום.
ואחרי כל קומיט — **פוש**: אמיר מתעדכן דרך GitHub.

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
