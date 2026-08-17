---
name: eb-portal
description: "EB AI — Mission Control: local portal to manage the whole Eretz Barzel AI program (plan, sessions, suggestions, decisions)"
metadata: 
  node_type: memory
  type: project
  originSessionId: 591c2afd-6aa2-40bd-a600-f43236b14506
  modified: 2026-08-17T13:53:55.331Z
---

Management portal Amir asked for (2026-06-17) so the multi-track AI program ([[ai-development-hub]]) lives in a permanent, navigable home instead of a disposable chat window — "the chat becomes throwaway; the portal is the memory."

⚠️ **RETIRED AND ARCHIVED 2026-08-17.** It no longer lives on the Desktop. Amir asked whether `Desktop\.claude` could be deleted; nothing in it was critical to [[acad-agent]], but `portal/` was the **only copy in existence** (the Desktop is not a git repo; a profile-wide search returned one hit). So it was archived into the repo first and deleted second. **Home now: `EB PROSTEEL AGENT\z-archive\desktop-dot-claude-2026-08-17\portal\`** (on GitHub, commit `a6867ba`), with a README there covering revival and the restore-permissions-only path. Copy verified before deletion: `diff -r` silent, 16/16 files, 57,202/57,202 bytes. Spec: `portal/PLAN.md`.

**Why it was retired:** it worked, but it was abandoned in practice — `data/*.json` untouched since 2026-06-17, because the job it was built for got done another way: `SESSION-START.md` + `PROGRESS.md` + `RESUME-HERE.md` in the repo became the durable memory. The *need* was real and was met; the *tool* was not the thing that met it.

**Locked decisions:** (1) **local-only** on Amir's PC, architected to be hostable for the team later; (2) Claude **curates the session log each session** now, auto-pull from Claude Code transcripts investigated later; (3) **fully interactive** — Amir adds/edits/checks-off in the UI.

**Architecture (same light stack as [[tankforge-project]]):** vanilla HTML/CSS/JS, **no DB**. `portal/server.py` = stdlib Python `http.server` serving the static app + a small JSON API (`GET/POST/PATCH/DELETE /api/<collection>`) that reads/writes `portal/data/*.json`. Those JSON files are the **shared substrate** — Amir edits via UI, Claude reads/writes the same files directly. `Portal.bat` one-click launch → `http://localhost:8190` (distinct from TankForge's 8123). It also renders existing Markdown (`..\web\RUNNING-PLAN.md`, `..\web\knowledge\`) rather than duplicating it.

**Data store:** program, roadmap, sessions, decisions, suggestions (2-way inbox), tasks, questions, projects (each a `data/*.json`). **Sections:** Dashboard (next-3-actions + open-questions-awaiting-Amir on top), Roadmap, Sessions, Inbox, Decisions, Tasks, Knowledge, Projects.

**Workflow that keeps it alive:** end of each session Claude appends a `sessions.json` entry + updates decisions/suggestions/tasks/questions; Amir triages inbox & checks off tasks between sessions; Claude reads those edits next session. This is the visual layer of RUNNING-PLAN §5.

**Build phases:** P1 foundation; P2 sessions/inbox/tasks/questions; P3 decisions/knowledge/search; P4 (later) auto-transcript ingestion + team hosting. **Status 2026-06-17: BUILT & VERIFIED — Phases 1–3 all done** (only P4 remains). Files: `portal/server.py`, `portal/app/{index.html,style.css,app.js}` (vanilla SPA + tiny built-in markdown renderer), `portal/data/*.json` (8 collections seeded), `portal/Portal.bat` (one-click launch). Was registered in `Desktop\.claude\launch.json` as config **"portal"**, alongside TankForge's "tankforge" (8123) — ⚠️ **that launch.json was deleted with the folder on 2026-08-17, so `preview_start tankforge` no longer resolves either**; both configs are archived beside the portal and can be restored from there. **To revive:** copy `portal/` and `launch.json` back to `Desktop\.claude\`, then `Portal.bat` or `preview_start portal` → http://localhost:8190. Verified live: dashboard (waiting-on-you / next-actions / track-progress / recent-sessions), 8 nav sections, markdown rendering with tables, and full write round-trip (checkbox→PATCH→disk) all work. **Asset cache-bust:** `app.js`/`style.css` carry `?v=N` in index.html — bump on every edit (server sends no cache headers for static files), currently v=2. **Gotcha fixed:** a `\\'` escape in a single-quoted JS string silently killed the whole app (no console error) — watch string escaping in app.js template strings.
