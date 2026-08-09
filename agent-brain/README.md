# 🧠 agent-brain — גיבוי המוח של הסוכן

התיקייה הזו היא **עותק גיבוי** של שני נכסים שחיים מחוץ לתיקיית הפיתוח, על המחשב בלבד.
עד 09/08/2026 הם לא היו בריפו כלל — כלומר כל הידע שנצבר היה קיים בעותק יחיד.

| מה | המקור החי על המחשב | מה זה |
|---|---|---|
| `skill-prosteel-modeling/` | `~/.claude/skills/prosteel-modeling/` | הסקיל — כל מה שנלמד ונמדד על ProSteel, ‏10 קבצים |
| `memory/` | `~/.claude/projects/C--Users-User-Desktop/memory/` | קבצי הזיכרון — 26 עובדות שנשמרות בין שיחות |

⚠️ **המקור החי הוא ב-`~/.claude/`, לא כאן.** הסוכן קורא וכותב שם. התיקייה הזו היא **תצלום**,
והיא מתיישנת ברגע שמשהו משתנה בצד החי.

## איך לרענן

```bash
python agent-brain/sync.py
```

מעתיק מ-`~/.claude/` לכאן ומדווח מה השתנה. **להריץ כשלב 4 של כל פרק**, יחד עם הטמעת הסקיל,
לפני הקומיט.

## שחזור למחשב חדש

```bash
cp -r agent-brain/skill-prosteel-modeling  ~/.claude/skills/prosteel-modeling
cp    agent-brain/memory/*.md              ~/.claude/projects/C--Users-User-Desktop/memory/
```

הסקיל נטען אוטומטית לפי `SKILL.md`; קובץ `MEMORY.md` הוא האינדקס שנטען בתחילת כל שיחה.
