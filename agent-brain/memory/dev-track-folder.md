---
name: dev-track-folder
description: "סידור 12/08/2026: כל נתיב פיתוח 1 (API + ידע הנדסי) יושב ב-api+knowledge-develop/ — ממנו ייבנה המוצר שיוטמע בחברה; תקנים = נתיב 2, נעול ב-standards/"
metadata: 
  node_type: memory
  type: project
  originSessionId: f7644a80-6f23-41e6-b129-0eb429982261
  modified: 2026-08-12T08:19:40.762Z
---

**הסידור של אמיר, 12/08/2026 — שני נתיבי פיתוח, תיקייה לכל נתיב:**

```
EB PROSTEEL AGENT/
├── PROGRESS.md · README.md · הלאנצ'ר          ← בשורש, בהחלטת אמיר
├── agent-brain/          עיקרון הפרויקט: PROGRAM.md + מראות הסקיל/הזיכרון (sync.py)
├── api+knowledge-develop/   ⭐ נתיב 1 — API וידע הנדסי (להלן DEV/)
│   ├── app/  qc/  knowledge/  lessons/  projects/  research/  data/  assets/
├── standards/            🔒 נתיב 2 — תקנים, נעול עד הכרזת אמיר
└── z-archive/handoffs/   מסירות בין סשנים (ה-Z מכוונת — ממוין אחרון בגיטהאב)
```

**החזון שאמיר הגדיר:** מתוך DEV ייבנה בסוף **המוצר** — הקונסולה/פלאגין שיוטמע רשמית
במחשבי עובדי החברה ("תעלה לאוויר כסוכן בחברה"), וגם כל השיפורים אחרי ההטמעה יבוצעו בתוכו.
צפויים עוד הרבה מודלים, מחקרים וידע — לשמור על ההפרדה: שיעורים ל-`lessons/`, מודלים
ל-`projects/`, מחקרים ל-`research/`, ידע נלמד ל-`knowledge/`.

**Why:** אמיר הרגיש שהמידע המצטבר יוצא משליטה וקבע מבנה יעד. הוא מסתנכרן דרך GitHub
([[push-every-commit]]) וצריך לדעת להתמצא לבד — README בשורש + README בתוך DEV הם המפות שלו.

**How to apply:**
- מוסכמת נתיבים: במסמכים שבתוך DEV (סקיל, הערות, PROGRESS) — `app/`, `qc/`, `knowledge/`,
  `projects/` יחסיים ל-DEV. מסמכי שורש כותבים נתיב מלא או `DEV/`.
- השומר: `python "api+knowledge-develop/qc/consistency.py"` — עודכן לעוגן DEV וסורק גם
  את lessons/ ו-research/. ‏selftest עודכן בהתאם.
- ⚠️ בגלל ההזזה נוצר **v184** (הנתיב הצרוב `const string Dir` עודכן): ‏`EBAgentApi184.cs`
  קומפל ל-DLL לפי הפקודה המתועדת ב-plugin-ops. בסשן ה-AutoCAD הבא — ‏NETLOAD של v184;
  ‏v183 שדולף מסשן ישן יכתוב לנתיב שכבר לא קיים.
- ✅ **אומת מקצה לקצה 12/08/2026:** ‏NETLOAD v184 מהנתיב החדש (`live after 1.0s`) · ‏`env` ·
  ‏`vfy_fit 360000..416000` על `D-miscellaneous.dwg` החזיר **בדיוק** את שורת הבסיס של D.5
  (`bolts=24 OK=24 BOLT-NO-HOLE=0 GAP=0 SHORT=0`). כלום לא נפגע.
- ⚡ **לקח: הזזת תיקיית התוסף מחייבת גם העברת TRUSTEDPATHS** — האמון של AutoCAD יושב על
  הנתיב, לא על הקובץ. ‏eb_api מזהיר על זה במפורש; התיקון מקוד: `doc.SetVariable("TRUSTEDPATHS",...)`
  (בוצע — הישן הוסר, החדש נוסף, נשמר בפרופיל). וגם: פתיחת AutoCAD+ProStructures מקוד עובדת —
  לשכפל את הקיצור: `acad.exe /p "...ProStructures_SS6.1ACAD_E001_409.arg" /t Ps191_Metric
  /ld ProStructuresLoader.arx`.
- הקונסולה: אמיר שוקל להחליף אותה במשהו אחר — ההחלטה נדחתה ("עוד מעט נגיע לזה"),
  בינתיים `console.py`, ‏data/ ו-assets/ נשארים כמו שהם בתוך DEV.

קשור: [[two-phase-program]] · [[acad-agent]] · [[eb-portal]]
