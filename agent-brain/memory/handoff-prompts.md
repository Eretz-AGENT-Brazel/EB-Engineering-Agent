---
name: handoff-prompts
description: "Amir's workflow — always end a work package with a ready copy-paste prompt for the next model/session"
metadata: 
  node_type: memory
  type: feedback
  originSessionId: f91b529d-dc89-40b6-9887-8daf2489a303
---

When finishing a work package that another model/session will continue (e.g. a brief for Opus 4.8), always end the reply with a **ready-to-copy-paste prompt** the user can drop directly into the next chat — inside a code block, in Hebrew, containing: the absolute path of the brief/spec to read, where the code lives, what order to work in, what to ask before starting, and the constraint that outputs stay inside the project folder.

**Why:** Amir asked explicitly (2026-07-13): "תרשום לי את משפט ההנחיה... העתק הדבק ישירות לצאט. בכללי- תעשה איתי ככה להמשך הדרך." He orchestrates multiple models (Fable = analysis/spec, Opus 4.8 = code) and moves between chats.

**How to apply:** end such replies with a clearly-marked code block titled "העתק-הדבק ל-Opus" (or the relevant target); keep it self-contained — the target model has no access to this conversation. See [[vessels-stage1]] for the current handoff target.

**CORRECTION (2026-07-13, later):** This applies ONLY when genuinely handing to a *different* model/chat. When Amir is working continuously with me (Opus 4.8) inside the same ongoing thread — e.g. the [[acad-agent]] EB PROSTEEL AGENT work — do NOT append handoff prompts. He said explicitly: "אתה עובד כרגע על מודל OPUS 4.8 אין לך מה לכתוב הוראות." Just continue the work myself; the handoff-prompt habit annoyed him in that context.
