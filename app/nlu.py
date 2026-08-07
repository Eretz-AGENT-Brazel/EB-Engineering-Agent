"""
nlu.py - free Hebrew/English modeling interpreter (Tier 1, in-server, no LLM).

handle(text) -> a Hebrew reply string if it understood and executed the command,
or None if the sentence is complex/ambiguous (then it goes to the Claude tier).

Design goal: understand professional steel-modeling speech in milliseconds and
execute it via eb_api in ~1-2s. Engineering defaults are applied and STATED.
"""
import re
import eb_api
import context

PROF_RE = r"(HE\s?[ABM]\s*\d+|HE[ABM]\d+|IPE\s*\d+|IPN\s*\d+|UPN\s*\d+|U\s*\d+|RHS\s*[\dxX.×]+|SHS\s*[\dxX.×]+|CHS\s*[\dxX.×]+)"

# intent keyword sets (Hebrew + English)
KW = {
    "beam":   ["קורה", "קורות", "beam", "beams", "girder", "פרופיל"],
    "column": ["עמוד", "עמודים", "column", "columns", "post"],
    "plate":  ["פלטה", "פלטות", "לוח", "plate"],
    "delete": ["תמחק", "מחק", "תמחוק", "delete", "erase", "remove", "תסיר"],
    "undo":   ["בטל", "אחורה", "undo", "תחזיר אחורה"],
    "view":   ["מבט", "תצוגה", "view", "הסתכל", "תעבור ל"],
    "zoom":   ["זום", "zoom", "הצג הכל", "התאם", "מרכז תצוגה"],
    "list":   ["מה יש", "רשימה", "list", "מה במודל", "מה בנינו"],
}
VIEWS = {"top": ["top", "על", "מלמעלה"], "front": ["front", "חזית"],
         "iso": ["iso", "איזומטרי", "תלת", "3d"], "right": ["right", "ימין"],
         "left": ["left", "שמאל"], "back": ["back", "אחורי"], "bottom": ["bottom", "מלמטה"]}


def _has(t, key):
    return any(w in t for w in KW[key])


def _profile(text):
    m = re.search(PROF_RE, text, re.I)
    return m.group(1).strip() if m else None


def _points(text):
    pts = re.findall(r"(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)(?:\s*,\s*(-?\d+(?:\.\d+)?))?", text)
    return [tuple(float(x) if x else 0.0 for x in p) for p in pts]


def _length_mm(text):
    m = re.search(r"(\d+(?:\.\d+)?)\s*(?:מטר|מ['’]|meters?|\bm\b)", text, re.I)
    if m:
        return float(m.group(1)) * 1000.0
    m = re.search(r"(\d+(?:\.\d+)?)\s*(?:ס\"?מ|cm)", text, re.I)
    if m:
        return float(m.group(1)) * 10.0
    return None


def _axis(t):
    if re.search(r"ציר\s*z|אנכי|vertical|\bz\s*axis|axis\s*z|למעלה|כלפי מעלה", t):
        return "z"
    if re.search(r"ציר\s*y|axis\s*y|\by\s*axis", t):
        return "y"
    if re.search(r"ציר\s*x|axis\s*x|\bx\s*axis|אופקי|horizontal", t):
        return "x"
    return None


def _count(t):
    m = re.search(r"(\d+)\s*(?:קורות|עמודים|beams|columns)", t)
    return int(m.group(1)) if m else 1


def _view_name(t):
    for name, words in VIEWS.items():
        if any(w in t for w in words):
            return name
    return None


def _fmt(p):
    return "(%g,%g,%g)" % (p[0], p[1], p[2])


def _axis_vec(axis, length):
    return {"x": (length, 0, 0), "y": (0, length, 0), "z": (0, 0, length)}[axis]


def handle(text):
    """Entry point. Splits chained commands ("... ואז ..."), handles each in order."""
    parts = re.split(r"\s+(?:ואז|אחר[ -]?כך|ואחר[ -]?כך|אחרי זה|then|;)\s+", text or "", flags=re.I)
    parts = [p for p in parts if p.strip()]
    if len(parts) > 1:
        reps = []
        for p in parts:
            r = _handle_one(p)
            reps.append(r if r else ("(לא הבנתי: %s)" % p.strip()))
        return "\n".join(reps)
    return _handle_one(text)


def _handle_one(text):
    """Return a Hebrew reply if handled, else None (=> escalate to Claude)."""
    t = (text or "").lower().strip()
    if not t:
        return None
    ctx = context.load()
    # ---- live-run control (recognised so the console echoes them instantly;
    #      live_rebuild.py picks them up from the inbox) ----
    if re.fullmatch(r"(עצור|עצירה|stop|רגע|חכה|השהה|pause)\W*", t):
        return "⏸️ עוצר אחרי האלמנט הנוכחי. כתוב **המשך** כשתרצה שאמשיך."
    if re.fullmatch(r"(המשך|continue|resume|קדימה|יאללה)\W*", t):
        return "▶️ ממשיך."
    # ---- collision check (learned from שיעור 1: Amir runs PS_COLLISION manually) ----
    if ("התנגשות" in t or "התנגשויות" in t or "collision" in t or "קולוזי" in t
            or ("בדוק" in t and "חיתוך" in t)):
        r = eb_api.fire("PS_COLLISION")
        if r:
            return "🔍 הרצתי **בדיקת התנגשויות** (PS_COLLISION) — התוצאות בחלון של ProSteel."
        return "⚠️ לא הצלחתי להריץ בדיקת התנגשויות — ודא ש-AutoCAD מחובר ולא עסוק."
    # ---- analyze learning ----
    if ("נתח את הלמידה" in t or "מה למדת" in t or "נתח למידה" in t
            or ("analyze" in t and "learn" in t)):
        import learn
        return learn.analyze_active(context._active())
    # ---- bolted connection ----
    if ("חיבור" in t or "connect" in t or "connection" in t or "תחבר" in t or "לחבר" in t
            or ("חבר" in t and ("אות" in t or "ברג" in t or "מתוברג" in t))):
        pts = _points(text)
        o = context.last(ctx)
        at = pts[0] if pts else (tuple(o["p2"]) if o else tuple(ctx.get("cursor", [0, 0, 0])))
        r = eb_api.conn_bolted(at)
        if r.startswith("EB_OK"):
            return "🔩 יצרתי חיבור מתוברג ב-%s (2 פלטות + 4 ברגים M20)." % _fmt(at)
        return "⚠️ לא הצלחתי ליצור חיבור: %s" % r

    # ---- non-create intents first ----
    if _has(t, "undo"):
        eb_api.undo_back()
        return "↩️ ביטלתי את הפעולה האחרונה."
    if _has(t, "view"):
        vn = _view_name(t)
        if vn:
            eb_api.view(vn)
            return "👁️ עברתי למבט %s." % vn.upper()
    if _has(t, "zoom"):
        eb_api.zoom()
        return "🔍 התאמתי תצוגה."
    if _has(t, "list"):
        r = eb_api.list_model()
        n = len(ctx["objects"])
        return "📋 במודל יש %d אלמנטים שיצרנו. (%s)" % (n, r)
    if _has(t, "delete"):
        if re.search(r"אחרון|האחרונה|last", t):
            o = context.last(ctx)
            if o and o.get("handle"):
                eb_api.delete(o["handle"])
                ctx["objects"].pop()
                context.save(ctx)
                return "🗑️ מחקתי את %s האחרון." % o.get("profile", "האלמנט")
            return "אין אלמנט אחרון למחוק."
        return None  # deleting by description -> let Claude handle

    # ---- create beam / column ----
    is_col = _has(t, "column")
    is_beam = _has(t, "beam") or (_profile(t) and not is_col)
    if is_col or is_beam:
        prof = _profile(t) or ctx.get("last_profile", "HEB 200")
        assumed = []
        if not _profile(t):
            assumed.append("פרופיל %s (ברירת מחדל)" % prof)
        pts = _points(text)
        length = _length_mm(t)
        count = _count(t)
        axis = _axis(t)

        # geometry resolution
        if len(pts) >= 2:
            p1, p2 = pts[0], pts[1]
        else:
            p1 = pts[0] if pts else tuple(ctx.get("cursor", [0, 0, 0]))
            if is_col:
                h = length or ctx["defaults"]["height"]
                if not length:
                    assumed.append("גובה %g מ'" % (h / 1000.0))
                p2 = (p1[0], p1[1], p1[2] + h)
            else:
                ax = axis or "x"
                if not axis:
                    assumed.append("לאורך ציר X")
                L = length or ctx["defaults"]["length"]
                if not length:
                    assumed.append("אורך %g מ'" % (L / 1000.0))
                v = _axis_vec(ax, L)
                p2 = (p1[0] + v[0], p1[1] + v[1], p1[2] + v[2])

        # multi-object (count) — spaced perpendicular
        made = []
        spacing = ctx["defaults"]["spacing"]
        sp_axis = "y" if not is_col else "x"
        for i in range(count):
            off = i * spacing
            q1 = list(p1); q2 = list(p2)
            idx = {"x": 0, "y": 1, "z": 2}[sp_axis]
            q1[idx] += off; q2[idx] += off
            r = eb_api.beam(prof, tuple(q1), tuple(q2))
            if r.startswith("EB_OK"):
                made.append((eb_api.handle_of(r), tuple(q1), tuple(q2)))
                context.add_object(ctx, "column" if is_col else "beam", prof, q1, q2, eb_api.handle_of(r))
            else:
                return "⚠️ לא הצלחתי ליצור %s: %s" % (prof, r)

        kind_he = "עמוד" if is_col else "קורה"
        kind_plural = "עמודים" if is_col else "קורות"
        if count > 1:
            reply = "✅ יצרתי %d %s %s (מרחק %g מ')." % (count, kind_plural, prof, spacing / 1000.0)
        else:
            reply = "✅ יצרתי %s %s מ-%s ל-%s." % (kind_he, prof, _fmt(made[0][1]), _fmt(made[0][2]))
        if assumed:
            reply += "  (הנחתי: %s — תגיד אם אחרת.)" % ", ".join(assumed)
        return reply

    return None   # not understood as a simple modeling command -> Claude tier
