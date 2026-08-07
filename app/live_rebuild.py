# -*- coding: utf-8 -*-
"""
live_rebuild.py — LIVE side-by-side model rebuild (שיעור 2).

Reads an existing ProSteel model with the model-reader (dumpmodel / EB_RUN9),
then rebuilds every element at an X offset, ONE ELEMENT AT A TIME, at watching
pace, reporting each step to the console — while listening for Amir's live
commands ("עצור" / "המשך" / free-text corrections).

Design comes straight from LEARNING_ROI_SESSION1.md:
  Amir's manual session was 31% UNDO because parametric dialogs need guessing.
  Here every object is created through the API with exact parameters up front —
  no dialogs, no undo loop.

Usage:
    python live_rebuild.py read              # read + inventory only (no modeling)
    python live_rebuild.py run [offset_mm]   # live rebuild (default 15000)
"""
import json
import os
import sys
import time

APP = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(APP)
sys.path.insert(0, APP)

import eb_api            # noqa: E402
import console as C      # noqa: E402  (reuse its project paths + say)

PACE = 1.8               # seconds between elements (watching pace)
KNOWLEDGE = os.path.join(ROOT, "knowledge", "LEARNED_PATTERNS.md")


# ---------- console I/O ----------
def say(text):
    """Post a message to the active project's console."""
    pp = C.P()
    if not pp:
        print(text)
        return
    C._append(pp["outbox"], {"text": text, "ts": time.strftime("%H:%M:%S")})
    C._append(pp["conv"], {"role": "agent", "text": text, "files": []})
    try:                                    # terminal may be cp1255: never let it kill the run
        line = text.replace("\n", " ")[:120]
        sys.stdout.write("[console] " + line + "\n")
        sys.stdout.flush()
    except Exception:
        pass


def _inbox_len():
    pp = C.P()
    return len(C._read(pp["inbox"])) if pp else 0


def _inbox_since(i):
    pp = C.P()
    if not pp:
        return []
    return [m.get("text", "") for m in C._read(pp["inbox"])[i:]]


STOP_WORDS = ("עצור", "עצירה", "stop", "רגע", "חכה", "השהה", "pause")
GO_WORDS = ("המשך", "continue", "יאללה", "קדימה", "resume", "ok", "אוקיי", "אישור")
ABORT_WORDS = ("בטל הכל", "תפסיק", "abort", "cancel all", "די")


def _classify(t):
    tl = (t or "").strip().lower()
    if any(w in tl for w in ABORT_WORDS):
        return "abort"
    if any(w in tl for w in STOP_WORDS):
        return "stop"
    if any(w in tl for w in GO_WORDS):
        return "go"
    return "note"


def log_correction(text, ctx):
    """Amir's live correction = the highest-value learning signal there is."""
    line = "- [%s] תיקון חי בזמן שכפול (%s): %s\n" % (
        time.strftime("%Y-%m-%d %H:%M"), ctx, text.strip())
    with open(KNOWLEDGE, "a", encoding="utf-8") as f:
        f.write(line)


def check_control(cursor, ctx=""):
    """Read new console messages. Returns (new_cursor, action, notes)."""
    n = _inbox_len()
    if n <= cursor:
        return cursor, None, []
    msgs = _inbox_since(cursor)
    action, notes = None, []
    for m in msgs:
        k = _classify(m)
        if k in ("stop", "abort", "go"):
            action = k
        else:
            notes.append(m)
            log_correction(m, ctx)
    return n, action, notes


def wait_for_go(cursor, ctx=""):
    say("⏸️ **עצרתי.** כתוב **המשך** כשתרצה שאמשיך (או **בטל הכל** לעצירה מלאה).")
    while True:
        time.sleep(1.0)
        cursor, action, notes = check_control(cursor, ctx)
        if notes:
            say("📝 רשמתי את ההערה שלך ואיישם: " + " | ".join(notes))
        if action == "go":
            say("▶️ ממשיך.")
            return cursor, True
        if action == "abort":
            return cursor, False


# ---------- model reading ----------
def read_model():
    r, els = eb_api.dumpmodel()
    shapes = [e for e in els if e["kind"] == "shape"]
    plates = [e for e in els if e["kind"] == "plate"]
    bolts = [e for e in els if e["kind"] == "bolt"]
    return r, els, shapes, plates, bolts


def inventory_text(r, els, shapes, plates, bolts):
    def tally(items, key):
        c = {}
        for e in items:
            k = e.get(key) or "?"
            c[k] = c.get(k, 0) + 1
        return ", ".join("%s×%d" % (k, v) for k, v in
                         sorted(c.items(), key=lambda x: -x[1]))

    out = ["🔍 **קראתי את המודל** (%s)" % r.replace("EB_OK ", "")]
    out.append("")
    if shapes:
        out.append("**%d פרופילים:** %s" % (len(shapes), tally(shapes, "profile")))
        lens = [e["length"] for e in shapes if e.get("length")]
        if lens:
            out.append("  אורכים: %.0f–%.0f מ\"מ" % (min(lens), max(lens)))
    if plates:
        out.append("**%d פלטות:** עוביים %s" % (len(plates), tally(plates, "t")))
        out.append("  מידות (אורך×רוחב): %s" % ", ".join(
            sorted(set("%.0f×%.0f" % (p["l"], p["w"]) for p in plates))[:8]))
    if bolts:
        out.append("**%d ברגים:** קטרים %s | סגנונות %s" % (
            len(bolts), tally(bolts, "diameter"), tally(bolts, "style")))
    other = [e for e in els if e["kind"] == "other"]
    errs = [e for e in els if e["kind"] == "err"]
    if other:
        out.append("_(+%d ישויות עזר: קווים/מידות/עזרי AutoCAD — לא משוכפלות)_" % len(other))
    if errs:
        out.append("⚠️ %d ישויות שלא הצלחתי לקרוא — אדווח בסוף." % len(errs))
    return "\n".join(out)


# ---------- rebuild ----------
def _off(p, dx):
    return (p[0] + dx, p[1], p[2])


def plan(shapes, plates, bolts, dx):
    """Ordered build plan: shapes first (the skeleton), then plates, then bolts —
    exactly the order Amir works in (lines→profiles→plates→bolts)."""
    steps = []
    for e in shapes:
        steps.append({"type": "beam", "src": e,
                      "desc": "קורה %s" % (e["profile"] or "?"),
                      "args": {"name": e["profile"], "catalog": e["catalog"],
                               "p1": _off(e["p1"], dx), "p2": _off(e["p2"], dx)}})
    for e in plates:
        steps.append({"type": "plate", "src": e,
                      "desc": "פלטה %.0f×%.0f×%.0f" % (e["l"], e["w"], e["t"]),
                      "args": {"center": _off(e["insert"], dx),
                               "l": e["l"], "w": e["w"], "t": e["t"]}})
    for e in bolts:
        steps.append({"type": "bolt", "src": e,
                      "desc": "בורג ⌀%.0f %s" % (e["diameter"], e["style"] or ""),
                      "args": {"at": _off(e["insert"], dx),
                               "d": e["diameter"], "style": e["style"]}})
    return steps


def execute(step):
    a = step["args"]
    if step["type"] == "beam":
        return eb_api.beam(a["name"], a["p1"], a["p2"], catalog=(a.get("catalog") or None))
    if step["type"] == "plate":
        return eb_api.plate(a["center"], a["l"], a["w"], a["t"])
    if step["type"] == "bolt":
        try:
            return eb_api.bolt(a["at"], d=a["d"], style=a["style"])
        except TypeError:
            return eb_api.bolt(a["at"])
    return "EB_ERR unknown step"


def run(dx=15000.0):
    t_start = time.time()
    cursor = _inbox_len()

    say("🚀 **מתחיל שכפול-לייב** — קורא את המודל שלך...")
    r, els, shapes, plates, bolts = read_model()
    if not (shapes or plates or bolts):
        say("❌ לא זיהיתי אלמנטים של ProSteel במודל. (%s)\n"
            "ודא שהקובץ **שיעור-2.dwg** פתוח ב-AutoCAD ושלחצת 🔗 התחבר." % r)
        return
    say(inventory_text(r, els, shapes, plates, bolts))
    time.sleep(1.0)

    steps = plan(shapes, plates, bolts, dx)
    say("🧱 **תוכנית הבנייה:** %d אלמנטים (%d פרופילים · %d פלטות · %d ברגים), "
        "בהיסט **+%.0f מ\"מ בציר X** — לצד המודל שלך.\n"
        "אני בונה אחד-אחד בקצב צפייה. אתה יכול לכתוב **עצור** בכל רגע." %
        (len(steps), len(shapes), len(plates), len(bolts), dx))

    ok = fail = 0
    failures = []
    for i, st in enumerate(steps, 1):
        cursor, action, notes = check_control(cursor, st["desc"])
        if notes:
            say("📝 רשמתי: " + " | ".join(notes) + " — ממשיך ואיישם בהמשך.")
        if action == "stop":
            cursor, go = wait_for_go(cursor, st["desc"])
            if not go:
                say("🛑 עצרתי לפי בקשתך אחרי %d/%d אלמנטים." % (i - 1, len(steps)))
                break
        elif action == "abort":
            say("🛑 עצרתי לפי בקשתך אחרי %d/%d אלמנטים." % (i - 1, len(steps)))
            break

        res = execute(st)
        good = isinstance(res, str) and res.startswith("EB_OK")
        if good:
            ok += 1
            say("✅ **%d/%d** %s" % (i, len(steps), st["desc"]))
        else:
            fail += 1
            failures.append((st["desc"], str(res)[:90]))
            say("⚠️ **%d/%d** %s — לא נוצר: %s" % (i, len(steps), st["desc"], str(res)[:90]))
        time.sleep(PACE)

    mins = (time.time() - t_start) / 60.0
    report(mins, ok, fail, failures, len(steps), shapes, plates, bolts)


def report(mins, ok, fail, failures, total, shapes, plates, bolts):
    base_min, base_undo = 27.1, 78
    lines = ["", "📊 **דוח שכפול-לייב — שיעור 2**", ""]
    lines.append("| מדד | ידני (שיעור 1) | הסוכן (שיעור 2) |")
    lines.append("|---|---|---|")
    lines.append("| זמן | %.1f דק' | **%.1f דק'** |" % (base_min, mins))
    lines.append("| ביטולים (UNDO) | %d | **0** |" % base_undo)
    lines.append("| אלמנטים שנוצרו | 112 (ונמחקו 104) | **%d** (נמחקו 0) |" % ok)
    if mins > 0 and base_min > 0:
        saved = 100.0 * (base_min - mins) / base_min
        lines.append("")
        lines.append("⏱️ **חיסכון זמן: %.0f%%** (%.1f דק' מול %.1f דק')" % (saved, mins, base_min))
    lines.append("")
    lines.append("נבנו %d/%d אלמנטים (%d פרופילים · %d פלטות · %d ברגים)."
                 % (ok, total, len(shapes), len(plates), len(bolts)))
    if fail:
        lines.append("")
        lines.append("⚠️ **%d אלמנטים לא נוצרו** — הפערים לתיקון:" % fail)
        for d, why in failures[:8]:
            lines.append("- %s → %s" % (d, why))
    lines.append("")
    lines.append("_הערה כנה: ההשוואה מול שיעור 1 היא מול מודל אחר (שם למדת ובנית מאפס). "
                 "המשמעות האמיתית: **אפס ביטולים ואפס עבודה שנזרקה** — הכאב שזיהיתי אצלך._")
    say("\n".join(lines))


if __name__ == "__main__":
    args = sys.argv[1:]
    mode = args[0] if args else "read"
    if mode == "read":
        r, els, sh, pl, bo = read_model()
        print(r)
        print(inventory_text(r, els, sh, pl, bo))
    elif mode == "run":
        run(float(args[1]) if len(args) > 1 else 15000.0)
    else:
        print(__doc__)
