# EB API V2 — תוכנית א'–ת': מידול בשפה חופשית במהירות מכונה

> ⚠️ **מסמך היסטורי — תוכנית שבוצעה ועבר זמנה.** כל משפט כאן הוא **כוונה** מרגע הכתיבה,
> לא תיאור של המצב הנוכחי. בפרט *"EBAgentApi7 הופך לתוסף הקנוני"* — זה קרה, ומאז עברו
> עוד עשרות גרסאות.
>
> ⭐ **הגרסה הקנונית היא מה ש-`app/eb_api.py` מצהיר, ושום מקום אחר.**

<!-- DATED-LOG -->

> **חלוקת תפקידים (הוראת אמיר):** Fable 5 תכנן (מסמך זה). **Opus 4.8 מבצע את כל הקוד.**
> **דרישת אמיר, מילה במילה:** "שהוא ימדל איתי בשפה חופשית ומהר — שפעולות יקרו במהירות מירבית."
> לא דוגמאות גנריות. שיחת עבודה חופשית בקונסול → פלדה אמיתית במודל, תוך שניות.

**Executor:** Opus 4.8. Work the phases IN ORDER (P0 → P6). Do not skip P0 — everything sits on it.
All line numbers verified 2026-07-16. Ground truth for Ps* signatures: `app/plugin/api_dump_ProStructuresNet.txt`.

---

## 1. ROOT-CAUSE ANALYSIS of the failed live test (observed, not guessed)

Live command: **"תיצור לי קורה UPN 200 על ציר X"** (project "טסט-בדיקה"). What happened:

| # | Root cause | Evidence | Consequence |
|---|---|---|---|
| RC1 | **Stale-result lie.** `eb_api.run()` on timeout returns the OLD `eb_result.txt` (`eb_api.py:189`) | Reply said `HE200B handle=ED entities=2` — byte-identical to the PREVIOUS selftest's result; resolver had correctly produced `('U200','DIN_U')` | Wrong/false success reports; user loses trust |
| RC2 | **Brain-in-the-loop latency.** Every console message waits for Claude (`console.py wait` → model turn → `say`) — tens of seconds to minutes, plus Hebrew re-read roundtrips | The user waited ~2 min for one beam | "הסוכן נתקע, לא מגיב" |
| RC3 | **Fast-lane too narrow.** `_try_model` (console.py:169) fires only on "profile + ≥2 explicit coordinate pairs" (line 175) | "על ציר X" has no coordinates → fell to the slow brain queue | 95% of natural sentences miss the fast path |
| RC4 | **No transport verification.** `EB_RUN6` SendCommand can silently not-execute (busy doc / focus / wrong active document — project DWG was never the active doc: Drawing1 was) | Command "sent" but nothing ran | Stuck feeling + stale reads |
| RC5 | **UI feedback slow.** 1.4s poll (console.py:592), no instant ack, no auto-reload after server restart | Feels laggy even when fast | "לא מגיב" perception |

**Conclusion:** the modeling path must run **entirely inside the console server** (Python, milliseconds), with a **verified request/response protocol** to the plugin. Claude moves OUT of the per-command loop — reserved for genuinely complex design requests, asynchronously.

## 2. TARGET ARCHITECTURE (V2)

```
Amir types/speaks freely in the console
   │
   ├── TIER 1 (target < 2s, covers ≥95% of commands):
   │     nlu.py — Hebrew/English modeling grammar (NO LLM, pure Python)
   │     context.py — session memory (last objects, workplane, defaults)
   │     → eb_api (reqid protocol) → EB_RUN7 plugin → native Ps objects
   │     → instant verified reply in the chat + optional voice
   │
   └── TIER 2 (async, only truly complex/ambiguous):
         queued to Claude; console immediately says "מעביר לתכנון — רגע"
```

## 3. PHASES FOR OPUS 4.8

### P0 — Bulletproof transport (the foundation) 🔴 do first
1. **Request-ID protocol.** `eb_api.run()` generates `reqid` (uuid4 hex-8), writes it as first line of `eb_cmd.txt`. Plugin (**EBAgentApi7.cs**, cmd `EB_RUN7`, class `ApiCmds7` — new name, DLL lock rule) echoes `reqid=<id>` inside every result line. `run()` accepts ONLY a result whose reqid matches; on deadline → return `EB_TIMEOUT reqid=<id>` (NEVER stale content). Delete the `eb_api.py:189` fallback.
2. **Atomic result write:** plugin writes `eb_result_tmp.txt` then File.Move → `eb_result.txt` (no torn reads).
3. **Active-document guard:** new plugin op `whoami` returns active doc name + entity count; `eb_api` exposes `ensure_doc(dwg_path)` — if the project DWG isn't the active document, activate/open it (Documents.Item(...).Activate()) before any modeling op. The console's open-CAD button already creates `projects/<id>/<id>.dwg`; every Tier-1 command must run against it.
4. **Auto-NETLOAD watchdog:** after send, if result doesn't arrive AND `LASTPROMPT` contains `Unknown command "EB_RUN7"` → add TRUSTEDPATHS + NETLOAD + retry once, automatically.
5. **Keep-alive:** console server pings (op=ping) every 60s in a background thread; status chip shows real engine state (green = ping OK <5s ago).
✅ **Acceptance P0:** 50 consecutive ops (mixed ping/beam/undo) — zero stale results, zero silent failures, each ≤3s; kill-and-restart AutoCAD mid-run recovers automatically.

### P1 — The NLU interpreter (free Hebrew/English → modeling) 🔴 the heart
New `app/nlu.py`, used by `_quick_action` (replaces `_try_model`). Pure Python, compiled regex + token rules — millisecond parse. MUST handle (with engineering defaults, He+En):
- **Intents:** create beam/column/plate/bolt/connection · copy/array · move · delete · undo · view/zoom · list/"מה יש במודל" · save.
- **Profiles:** existing `resolve_profile` (HEA/HEB/HEM/IPE/UPN/RHS/SHS + map). "קורה"/"עמוד" without profile → use context default (last used; initial default HEB 200, SAY the assumption).
- **Geometry grammar:**
  - explicit points: "מ-0,0,0 עד 6000,0,0"
  - **axes:** "על ציר X/Y/Z" → from cursor (default 0,0,0) along that axis, length = stated or default 6000. THE FAILED SENTENCE MUST PASS: "תיצור לי קורה UPN 200 על ציר X" → U200/DIN_U from (0,0,0) to (6000,0,0).
  - lengths+units: "6 מטר"→6000, "60 ס"מ"→600, bare numbers = mm.
  - vertical: "עמוד" → along Z, default height 3000.
  - counts+spacing: "4 עמודים כל 3 מטר", "2 קורות במרחק 3 מטר".
  - relative: "מהקצה של הקורה האחרונה", "ביניהם", "מעל", "במקביל במרחק X".
- **Context (`app/context.py`, JSON per project):** last handle per type, list of created objects (profile,p1,p2,handle), cursor point, workplane, defaults. Powers "האחרונה", "אותו דבר", "תמחק את זה", "תמשיך".
- **Clarify-fast:** if truly missing a slot → INSTANT question in chat ("לאיזה אורך? [ברירת מחדל 6 מ']"), and proceed on next message; never silently queue to Claude.
✅ **Acceptance P1:** 25-sentence He/En suite (write `app/test_nlu.py`, offline-parse + live-execute), including the exact failed sentence; each Tier-1 sentence executes correctly ≤2s parse + ≤3s create.

### P2 — Chaining & composites
"4 עמודים HEB 240 כל 3 מטר ואז תחבר אותם בקורות IPE 300" → plan of ops executed sequentially with ONE summary reply; conn_bolted via "חבר אותם בחיבור מתוברג"; miter via "חתוך בזווית". Multi-op >8s → interim progress message.
✅ 5 composite sentences pass live.

### P3 — Verified feedback (never lie again)
Every op reply built from the VERIFIED result: profile, from→to, handle, Δentities. Failure → exact reason + what to try. Optional flags per user pref: auto-zoom to new object; voice on/off. Log to `model_log.jsonl` + MODEL.md.

### P4 — Console speed & resilience
Poll 1400ms → 400ms (or SSE); instant local echo + "working" (already exists — keep); **UI version stamp**: server embeds build-id, page auto-reloads when it changes (kills the stale-JS-after-restart class of bugs); auto-reconnect banner.

### P5 — Claude tier (async, rare)
Only sentences NLU marks "complex design" (e.g., "תבנה גלריה כמו בתוכנית שהעליתי") → queue + instant ack "מעביר לתכנון, אענה כאן". Claude loop unchanged (`wait`/`say`). NEVER a simple modeling verb.

### P6 — Live acceptance with Amir (the only test that counts)
Amir, in the console, his own words, ≥10 commands: create/modify/copy/delete/connect/view. Measure per-command wall time in the UI (show "⚡0.8s" tag on each reply). Target: **every Tier-1 command ≤3s end-to-end.** Write `knowledge/V2_ACCEPTANCE.md` with the transcript + timings.

## 4. CLEANUPS (do during P0)
- Delete stray wrong beam from the failed test (Drawing1: handle ED HE200B) / start fresh project file.
- `open_project_cad` must also `ensure_doc` + set context file; wire the button result into context.
- Consolidate: EBAgentApi7.dll becomes the single canonical plugin (v3–v6 sources stay for history; remove dead DLLs after AutoCAD restart releases locks).
- Update `EB_MODELING_API.md` + memory (`acad-agent.md`) after each phase — mandatory.

## 5. DEFINITION OF DONE (V2)
1. The failed sentence works in ≤3s with the RIGHT profile. 2. Zero stale results possible (reqid). 3. ≥95% of Amir's session commands handled by Tier 1 ≤3s. 4. Engine status visibly green/red + self-heals. 5. Amir's live P6 session passes and he signs off.
