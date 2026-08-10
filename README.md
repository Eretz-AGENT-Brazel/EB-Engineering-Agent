# EB PROSTEEL AGENT
**Eretz Barzel · Steel Modeling AI** — your AI co-modeler for AutoCAD 2015 + ProSteel.

> 📊 **[מעקב לימוד המדריך — PROGRESS.md](PROGRESS.md)** · 79 פרקים, אחוזי התקדמות, וסדר הלימוד המתוכנן.
> 📚 **[תהליך הלמידה כולו — knowledge/learning/](knowledge/learning/README.md)** · מדריך, שיעורים, ממצאים וביקורות, בתיקייה אחת.
> מתעדכן בסוף כל פרק.

You talk to the agent (type / voice / sketch / upload), and it models steel in
ProSteel with you — fast, like a pro. The "brain" is Claude (free, via Claude Code);
this software is the cockpit.

## How to start (one click)
Double-click **`EB PROSTEEL AGENT`** (the desktop icon, or the `.bat` in this folder).
It launches **AutoCAD 2015 + ProSteel**, starts the console server, and opens the
**workspace window**. Then talk to the agent — work is discussed **only in the console**.

## The workspace (console)
- **Chat** — type or 🎤 speak (Hebrew/English), agent replies are shown and read aloud.
- **ProSteel command palette** (sidebar) — one click fires the right ProSteel tool.
- **📸 Screenshot + ✏️ Sketch** — capture the screen and mark it up (pencil, colors), send it.
- **📎 Files** — upload a client **PDF / DWG** plan; the agent analyzes it.
- **Live status** — ProSteel connection, model element count, current project.
- **RTL/LTR + He/En** toggles.

## Folder layout
| Folder | What |
|---|---|
| `app/` | The program (Python). `console.py` = workspace; `acad.py` = AutoCAD bridge; `prosteel.py` = command dispatcher; `plan2steel.py` = plan analyzer; `models.py` = projects. |
| `knowledge/` | ProSteel knowledge base (82-command reference, manual index, full searchable manual, agent guide). |
| `projects/` | One folder per project, with its own `MODEL.md` memory + plan analysis. Resume any time. |
| `data/` | Runtime: conversations + uploads + brand logo. |
| `assets/` | Company logo + icon. |

## Per-project memory
Every project has its own folder under `projects/` with a `MODEL.md` the agent reads
when you return — so it always knows exactly where you stopped.

## Notes
- Free — no API key. Brain = Claude in Claude Code.
- ProSteel modeling commands open dialogs; the agent fires the exact right tool and uses
  seed-and-copy + skeleton-first techniques. See `knowledge/KNOWLEDGE.md`.
- Course manual OCR pending (image-based); main 1179-page manual is fully searchable.
