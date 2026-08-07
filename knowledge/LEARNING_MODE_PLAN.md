# LEARNING MODE — תוכנית ל-Opus 4.8: הסוכן צופה באמיר, לומד, ומשתפר

> **הרעיון של אמיר:** "הסוכן יסתכל איך אני ממדל ועובד בתוכנה, יילמד ממני, וככה לאט-לאט
> נצבור ידע עד שהוא יהיה תותח וה-API יעבוד מושלם."
> **העיקרון:** צפייה → תיעוד → ניתוח → קידוד → אישור אמיר → ה-NLU/API גדל. לולאה מתמשכת.

**Executor:** Opus 4.8. Fable 5 planned. Phases L0→L4 in order.
Technical ground truth: the plugin runs IN-PROCESS in AutoCAD — it can subscribe to real
.NET events (CommandWillStart/CommandEnded on Document, ObjectAppended/Erased on Database).
That is the entire trick: we hear EVERYTHING Amir does, natively, with zero polling.

---

## L0 — Event recorder in the plugin (EBAgentApi8 / EB_RUN8) 🔴 the core
1. New plugin version (DLL-lock rule: new filename+cmd+class). Add a static recorder:
   - Hook on Initialize (ExtensionApplication) + DocumentActivated: attach to the active
     Document: `CommandWillStart`, `CommandEnded`, `CommandCancelled`; and to its Database:
     `ObjectAppended`, `ObjectErased`, `ObjectModified`.
   - Every event → append ONE json line to the active learning log:
     `{ts, ev, name?, class?, handle?, doc}`  (ev ∈ cmd_start|cmd_end|obj_add|obj_mod|obj_erase)
   - On `cmd_end` also write census count. Buffer+flush per line (File.AppendAllText is fine).
   - **Noise filter:** skip EB_RUN*, REGEN*, ZOOM*, PAN, QSAVE?, U (keep U! undo is a signal),
     and obj_mod storms (throttle: max 1 obj_mod line per handle per command).
2. Ops: `learn_on logpath=<path>`, `learn_off`, `learn_status`. Static bool + path.
3. Python (`eb_api.py`): `learn_start()` → creates `projects/<id>/learning/session_<stamp>.jsonl`,
   sends learn_on; `learn_stop()`; `learn_status()`.
✅ Done when: with learning ON, Amir manually inserts a ProSteel shape via the SHAPES dialog
and the log shows cmd_start/cmd_end (PS_INS_PROF) + obj_add (Ks_Shape, handle) lines.

## L1 — Console UI: מצב למידה 🎓
1. Project-card button **"🎓 מצב למידה"** (toggle): POST `/learn/toggle` → eb_api.learn_start/stop.
   Active state: button highlighted + header chip **● מקליט** (red dot), and an agent message:
   "🎓 מצב למידה פעיל — עבוד רגיל ב-ProSteel, אני צופה ולומד. לחץ שוב לעצירה."
2. `/status` returns learning:on/off so the chip survives reloads.
3. On stop: agent message with a mini-summary (N commands, M objects) + "רוצה שאנתח? כתוב: נתח את הלמידה".
✅ Done when: toggle works from the console, chip reflects state, log grows while Amir works.

## L2 — Semantic enrichment (מה בדיוק נוצר)
After each cmd_end with obj_adds: enrich each new object (best-effort, in the SAME plugin op cycle or via a follow-up `learn_enrich` op): class, layer, and for Ks_Shape try profile
name + endpoints (attach PsShape by ObjectId — investigate `SetObjectId`/PsUtils in the api dump; fallback: geometric extents). Write an `obj_info` line per handle.
✅ Done when: after Amir inserts HEB 300 manually, the log contains its profile string.

## L3 — Learning digest (הניתוח) 🧠
1. `app/learn.py`: `digest(session_file)` → Hebrew summary: command frequencies, command
   bigrams (sequences), objects created per command, profiles used, undo patterns
   (undo right after X = X was wrong/slow — a UX signal!). Saves/updates
   `knowledge/LEARNED_PATTERNS.md` (append per session, structured).
2. Console: the sentence "נתח את הלמידה" (NLU intent) → runs digest → posts the Hebrew
   summary in the chat (Tier 1, fast). The DEEP analysis — proposing new NLU phrases,
   macro candidates ("אמיר תמיד עושה X ואז Y ואז Y — מאקרו אחד"), API gaps (commands he
   uses that we don't cover) — is a Tier-2 Claude task reading the jsonl + PATTERNS file.
3. **Codify loop (the whole point):** Claude proposes → shows Amir in the console
   ("למדתי: כשאתה אומר 'פלטת חיזוק' אתה מתכוון ל-PS_RIP עם ...; להוסיף?") → on approval,
   extends `nlu.py` vocabulary / adds an eb_api macro + records in LEARNED_PATTERNS.md.
✅ Done when: after one real recorded session, at least 3 concrete learned proposals are
shown and, once approved, actually work as new console commands.

## L4 — Teach-by-example (עשה כמו שעשיתי)
"תעשה שוב מה שעשיתי" / "תחזור על הרצף האחרון" → replay the last recorded command sequence
programmatically where our API covers it; where it doesn't — honest reply naming the gap
(which becomes the next learning item). Optional: "קרא לזה <שם>" saves it as a named macro.
✅ Done when: a recorded 3-step sequence replays as one console sentence.

## Standing rules
- Recording is LOCAL only (jsonl in the project folder), Amir can delete anytime; show
  clearly when recording is on. Never record outside learning mode.
- Never block modeling: event handlers must be try/catch-silent and fast (file append only).
- Every phase: update EB_MODELING_API.md + memory (acad-agent.md).
- The V2 speed rules stay (reqid, in-server NLU, EB_BUSY behavior).

## Definition of Done
1. Toggle-record-stop works from the console with visible state. 2. A real manual session
by Amir produces an enriched log. 3. Digest gives a useful Hebrew summary. 4. At least
one learned pattern is codified into the NLU/API and used successfully. 5. The loop is
documented so every future session grows the knowledge.
