# EB AI — Mission Control (Portal) — Build Plan

> A local "home base" for the whole Eretz Barzel AI program, so the work lives in a
> permanent, navigable place instead of a disposable chat window.
> **The chat becomes throwaway; the portal is the memory.**

- **Owner:** Amir Arzuan · **Created:** 2026-06-17
- **Home:** `C:\Users\User\Desktop\.claude\portal\`
- **Relationship:** the visual front-end to `..\web\RUNNING-PLAN.md`, the decision log,
  and `..\web\knowledge\`. Part of the program in [[ai-development-hub]].

## Locked decisions (2026-06-17)
1. **Access:** local-only on Amir's PC; architected so it can be hosted for the team later.
2. **Capture:** Claude curates the log each session now; investigate auto-pull from Claude
   Code transcripts later.
3. **Interactivity:** fully interactive — Amir can add/edit/check-off items in the portal.

## Architecture (stay light)
Same stack as TankForge — **vanilla HTML/CSS/JS, no build, no database.**
- `portal/server.py` — stdlib Python (`http.server`): serves the static app **and** a small
  JSON API (`GET/POST/PATCH/DELETE /api/<collection>`) that reads/writes `portal/data/*.json`.
- `portal/data/*.json` — the durable store. **Amir edits via the UI; Claude reads/writes the
  same files directly.** This is the shared substrate.
- `portal/app/` — `index.html`, `style.css`, `app.js` (+ a tiny Markdown renderer for
  RUNNING-PLAN / knowledge pages).
- `Portal.bat` (+ desktop shortcut) — one click: starts the server, opens
  `http://localhost:8190` (distinct port from TankForge's 8123).
- Local-only = bind to localhost. Hosting later = swap the server host + add auth, no rewrite.

## Data model (`portal/data/*.json`)
| File | Holds | Key fields |
|---|---|---|
| `program.json` | top-level meta | tracks, current phase, last-updated |
| `roadmap.json` | phases (both tracks) | id, track, title, status, outcome |
| `sessions.json` | one entry per working session | id, date, title, summary, changes[], decisions[], files[] |
| `decisions.json` | the decision log | id, date, decision, why, track |
| `suggestions.json` | inbox (2-way) | id, date, from (claude\|amir), project, text, status (open/accepted/deferred/done), notes |
| `tasks.json` | actions | id, title, track, owner (amir/claude), status (todo/doing/done/blocked) |
| `questions.json` | things awaiting Amir | id, date, question, context, answer |
| `projects.json` | project registry | id, name, status, path, summary, links |

## Sections (pages)
1. **Dashboard** — current phase, progress per track, **Next 3 Actions**, **Open Questions
   (awaiting you)** pulled to top, recent change feed.
2. **Roadmap** — the unified two-track plan as a visual timeline/board with status.
3. **Sessions** — chronological log of every working session (the "don't get lost" core).
4. **Inbox** — suggestions both ways; add/edit/triage (open → accepted/deferred/done).
5. **Decisions** — searchable decision log, each with date + why.
6. **Tasks** — checklist by track/owner.
7. **Knowledge** — browse `knowledge/` + `standards/` (rendered Markdown).
8. **Projects** — TankForge, CAD Agents, AcadAgent — status, links, open items.

## The sync workflow (what keeps it alive)
End of each working session, Claude appends a `sessions.json` entry and updates
`decisions / suggestions / tasks / questions`. Amir triages the Inbox and checks off tasks
in the UI between sessions; Claude reads those edits at the start of the next session.
This is the visual layer of the "easy flow process" already in RUNNING-PLAN §5.

## Build sequence
- **Phase 1 — Foundation:** `server.py` (static + JSON API), data store seeded from today's
  real state, **Dashboard + Roadmap**, `Portal.bat` one-click launch. → a working home page.
- **Phase 2 — Interactive core:** Sessions log, Inbox (add/edit/triage), Open Questions,
  Tasks (check-off). The write API + forms.
- **Phase 3 — Depth:** Decisions view, Knowledge browser, global search, free notes.
- **Phase 4 — Later/optional:** auto-summarize Claude Code transcripts into Sessions;
  team hosting + auth; richer editing.

## Open questions for Amir
- Visual style: match TankForge's engineering look, or a distinct "command center" feel?
- Port 8190 OK, or prefer another?
