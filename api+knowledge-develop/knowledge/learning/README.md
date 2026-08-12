# 📚 `learning/` — the whole learning process, in one place

*Consolidated 10/08/2026 at Amir's request: **"כל התהליך למידה שלנו — תרכז אותו בתוך תיקייה
אחת מרוכזת תחת knowledge."***

Everything from the first graded lesson to the current manual chapter lives under this one
folder. Nothing about learning sits loose at the top of `knowledge/` any more.

---

## The map

| folder | what is in it | count |
|---|---|---:|
| **`manual/`** | reading the manual, chapter by chapter — the core of the work | |
| `manual/B/` | part **B** — modelling and parts lists, `MANUAL-NOTES-B01…B29` | 29 |
| `manual/E/` | part **E** — `E.9` properties dialogs · `E.10` corrected command table · `E.11` own notes | 3 |
| `manual/A/` | part **A** — dialogs, templates, input options, project manager, global settings, `MANUAL-NOTES-A01…A06` | 6 |
| `manual/` (root) | `manual_fulltext.txt` (the source, 1,179 pp) · the index · the command list | 3 |
| **`lessons/`** | the graded lessons with Amir, **before** the manual was started — protocol, log, retrospectives, skills matrix | 12 |
| **`findings/`** | conclusions that cross chapters and outlive them | 6 |
| **`audits/`** | verification records — what was checked, and what the check got wrong | 9 |
| **`plan/`** | how the learning is meant to run | 1 |

### The two documents at the root of this folder

* **`RESUME-HERE.md`** — the stopping point from 05/08, before the manual was started.
* **`QUESTIONS-FOR-AMIR.md`** — questions collected while working alone. These belong to the
  *"what is right for fabrication"* axis, where **Amir is the authority and the documentation
  is not**.

---

## Where to look first

| question | file |
|---|---|
| **What will the API refuse to do?** | `findings/THE-CEILING-what-code-cannot-reach.md` |
| Which sections exist, and the traps | `findings/SECTION-CATALOGUES.md` |
| What did the bolt audit find, and what did it get wrong? | `audits/AUDIT-B08-bolts-2026-08-10.md` |
| How is a chapter supposed to be learned? | `lessons/LESSON-PROTOCOL.md` |
| What has been closed so far? | `../../PROGRESS.md` |

⭐ **The single most useful file is `findings/THE-CEILING…`.** Read it *before* chasing any
creator: if the route is closed there, go straight to the composition workaround listed beside
it. Nine chapters each paid for that list separately.

---

## What is deliberately NOT here

These stayed where they are, and moving them would break something:

| | why |
|---|---|
| `knowledge/LEARNED_PATTERNS.md` · `section_catalog_map.json` | **read by running code** — `learn.py`, `eb_api.py` |
| `knowledge/steel/` · `recipes/` · `research/` | read by `console.py`, `standards_kb.py`, `make_recipe.py` |
| `knowledge/api/` | the API surface dump — **reference**, not learning. **771 types is a retracted figure** — see `knowledge/api/` itself for the current dump |
| `knowledge/HOW_AMIR_MODELS.md` · `AMIR-ANSWERS-CONNECTIONS.md` · `KNOWLEDGE.md` | **Amir's own input**, not something the agent worked out |
| `projects/sandbox/*.dwg` | the models. `projects/` **is** their organised place, and `eb_api` resolves them through it. Map: `projects/sandbox/README-MODELS.md` |
| `app/plugin/eb_cmd.txt` · `eb_result.txt` · `eb_expect_dwg.txt` | the command channel. The plugin holds its directory as a **compiled-in constant** |

---

## The models

The practice drawings are **not** here — they are in `projects/sandbox/`, which is where the
running system looks for them, with their own map in `projects/sandbox/README-MODELS.md`:

* **`B08-insert-shapes.dwg`** — part B, 20 chapter bands laid out along +X
* **`E-structural-elements.dwg`** — part E, **one lesson per strip** with a visible gap between
  strips and a named grid on each
* `sandbox.dwg` — general scratch drawing, free to work in *(the old do-not-touch rule was
  withdrawn 12/08/2026 — Amir clarified the file is not his)*
* `_archive/` — snapshots, kept rather than deleted

---

## ⚠️ מדוע האינדקס הזה נבדק, 10/08/2026

סקירה בלתי-תלויה מצאה שהקובץ הזה — **הקובץ שקורא נוחת בו ראשון** — היה מיושן בארבע טענות
נפרדות: חלק A נכתב "ריק, הבא בתור" בזמן שיש בו שש הערות; ספירות הקבצים היו ישנות; מספר
טיפוסי ה-API שהוא ציטט כבר הופרך; והוא הפנה לסקריפטים שעברו ל-`app/_attic/`.

⇒ ⭐ **אינדקס מיושן גרוע מאין אינדקס**, כי הוא נקרא כאילו הוא נכון. הספירות כאן מחושבות
מהדיסק ולא מהזיכרון, ו-`python qc/consistency.py` בודק את שאר השרשרת לפני כל קומיט.
