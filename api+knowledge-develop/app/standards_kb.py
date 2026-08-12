"""
standards_kb.py - engineering-standards consultation engine (Tier 1, in-server).

The EB PROSTEEL AGENT acts as a steel-standards consultant. This module answers
standards questions (Israeli ת"י + European EN/Eurocode) in milliseconds from a
local knowledge base, WITHOUT an LLM and WITHOUT reproducing copyrighted text.

Two knowledge layers, both under  standards/kb/ :
  1. data.json  -> a curated set of verified engineering FACTS (γ factors, steel
                   grades, class limits, categories...) each with a citation.
  2. *.md       -> deep per-domain reference modules (produced & verified by the
                   eb-standards-mastery research workflow).

Public API:
  is_standards_query(text) -> bool      # does this look like a standards question?
  consult(text)            -> str|None  # a cited Hebrew answer, or None (=> escalate)
  search(query, limit=6)   -> [ {slug,title,score,snippet}, ... ]   # for the UI
  modules()                -> [ {slug,title,file}, ... ]            # KB table of contents
  module_md(slug)          -> str|None                             # one module's markdown

Policy (binding): cite number+part+edition+clause; never reproduce protected text;
the agent is a design AID — a licensed engineer holds responsibility.
"""
import os
import re
import json
import glob

APP = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(APP)                 # api+knowledge-develop (the dev track)
REPO = os.path.dirname(ROOT)                # the repo root -- standards/ lives THERE (track 2)
KB_DIR = os.path.join(REPO, "standards", "kb")
# steel-modeling teaching modules (detailing workflow, precision, connections,
# ProSteel methodology, He<->En glossary, fabrication) — searchable alongside
# the standards modules so the console can consult practice, not only code.
STEEL_DIR = os.path.join(ROOT, "knowledge", "steel")

_CACHE = {"facts": None, "mods": None, "mtime": 0.0}

DISCLAIMER = "כלי-עזר בלבד — האימות והחתימה בידי מהנדס מוסמך."

# explicit fact-id -> knowledge-module slug (for the "read more" pointer)
FACT_MODULE = {
    "gamma-m": "en-1993-1-1-design-of-steel-structures",
    "load-factors": "en-1990-basis-of-structural-design-ec0",
    "steel-grades": "materials-en-10025-10210-10219-10164",
    "epsilon": "en-1993-1-1-design-of-steel-structures",
    "section-class": "en-1993-1-1-design-of-steel-structures",
    "bolt-grades": "en-1993-1-8-connections-design",
    "bolt-shear": "en-1993-1-8-connections-design",
    "weld-betaw": "en-1993-1-8-connections-design",
    "material-props": "en-1993-1-1-design-of-steel-structures",
    "exc-classes": "execution-en-1090",
    "corrosivity": "corrosion-protection",
    "galvanizing": "corrosion-protection",
    "snow-load": "en-1991-1-3-snow-loads",
    "wind-load": "en-1991-1-4-wind-actions",
    "psi-factors": "en-1990-basis-of-structural-design-ec0",
    "ltb": "en-1993-1-1-design-of-steel-structures",
    "fire": "en-1993-1-2-fire-design",
    "fatigue": "en-1993-1-9-1-10-fatigue-fracture",
    "toughness-z": "en-1993-1-9-1-10-fatigue-fracture",
    "ti1225": "ti-1225-israeli-steel-code",
    "israeli-loads": "ti-412-414-413-israeli-loads",
}

# ---- triggers -------------------------------------------------------------
# Strong tokens: their presence alone marks a standards question.
_STRONG = ["תקן", "תקנים", "תקינה", "eurocode", "en 19", "en19", "en1993", "en 1993",
           "en 1991", "en 1990", "en 1090", "iso 12944", "iso 1461", "iso 898",
           'ת"י', "ת''י", "γm", "gamma m", "1225", "1090", "12944",
           "10025", "14399", "en10025", "exc1", "exc2", "exc3", "exc4",
           "סיווג חתך", "מחלקת חתך", "מקדם בטיחות", "מודול אלסטיות", "מחלקת ביצוע",
           "צירוף עומסים", "קטגוריית קורוזיביות",
           # modeling-practice triggers (the new steel/ teaching modules)
           "נקודת עבודה", "קווי רשת", "סבולת", "סבולות", "פער התאמה", "fit-up",
           "חורי אוורור", "חורי ניקוז", "גרון", "מרווח ברגים", "מרחק מקצה",
           "מספור חלקים", "רשימת חלקים", "tos", "ffl", "מפלס פלדה",
           "איך ממדלים", "איך למדל", "מילון מונחים"]
# Weak topic tokens: need a question word to count as a standards question.
_WEAK = ["עומס", "עומסים", "רוח", "שלג", "סייסמי", "רעידת", "מקדם", "בטיחות",
         "סיווג", "מחלקת", "קריסה", "חיבור", "בורג", "ברגים", "ריתוך",
         "קורוזיה", "שיתוך", "גילוון", "אש", "התעייפות", "עייפות", "fatigue",
         "דרגת פלדה", "חוזק", "fy", "fu", "class ", "מודול", "אלסטיות", "צפיפות",
         "עמידות", "צירוף", "מהירות רוח", "טמפרטורה", "קשיחות", "עובי"]
# Stopwords excluded from module-search tokens (noise that inflates long modules).
_STOP = {"של", "מה", "מהו", "מהי", "מהם", "את", "על", "לפי", "איזה", "באיזה", "כמה",
         "זה", "זו", "עם", "אם", "יש", "לא", "כן", "או", "גם", "כל", "אל", "לי", "לו",
         "הוא", "היא", "הם", "מן", "כי", "רק", "צריך", "נדרש", "דרוש", "בין",
         "the", "of", "is", "for", "to", "and", "what", "which", "how", "in", "on", "a"}
_QWORDS = ["מה ", "מהו", "מהי", "כמה", "איזה", "באיזה", "האם", "לפי ", "מתי", "?",
           "what", "which", "how", "מהם", "כיצד", "צריך", "נדרש", "דרוש"]
_CREATE = ["תבנה", "בנה ", "צור ", "תצור", "הוסף", "תוסיף", "מדל", "למדל", "תמדל",
           "draw", "create", "model ", " add ", "תשרטט", "שרטט"]


def _norm(t):
    return (t or "").lower().strip()


def is_standards_query(text):
    t = _norm(text)
    if not t:
        return False
    if any(tok in t for tok in _STRONG):
        return True
    create = any(v in t for v in _CREATE)
    if create:
        return False   # a modeling command wins unless it explicitly names a standard
    weak = any(tok in t for tok in _WEAK)
    q = any(w in t for w in _QWORDS)
    return weak and q


# ---- knowledge base loading ----------------------------------------------
def _load():
    """Load facts + modules, cache until any kb file changes."""
    try:
        dirs = [d for d in (KB_DIR, STEEL_DIR) if os.path.isdir(d)]
        sig = 0.0
        for d in dirs:                      # signature: size+mtime of every file
            for f in glob.glob(os.path.join(d, "*")):
                try:
                    st = os.stat(f)
                    sig += st.st_mtime + st.st_size
                except OSError:
                    pass
        if _CACHE["facts"] is not None and sig == _CACHE["mtime"]:
            return
        facts = []
        dj = os.path.join(KB_DIR, "data.json")
        if os.path.exists(dj):
            facts = json.load(open(dj, encoding="utf-8")).get("facts", [])
        mods = []
        for d in dirs:
            kind = "תקן" if d == KB_DIR else "מידול"
            for fp in sorted(glob.glob(os.path.join(d, "*.md"))):
                base = os.path.basename(fp)
                if base.lower() in ("index.md", "readme.md") or base.startswith("_"):
                    continue
                with open(fp, encoding="utf-8") as fh:
                    txt = fh.read()
                slug = re.sub(r"^\d+[-_]", "", os.path.splitext(base)[0])
                m = re.search(r"^#\s+(.+)$", txt, re.M)
                title = m.group(1).strip() if m else slug
                mods.append({"slug": slug, "file": base, "title": title,
                             "text": txt, "kind": kind})
        _CACHE.update({"facts": facts, "mods": mods, "mtime": sig})
    except Exception:
        if _CACHE["facts"] is None:
            _CACHE.update({"facts": [], "mods": []})


_MARK_LEGEND = {"✓": "אומת מקובץ רשמי", "⭑": "מבוסס-Eurocode (לא בהכרח הערך הישראלי המחייב)",
                "⚠": "טעון אימות מול הנוסח המחייב / הנספח הלאומי"}


def _fmt_fact(f):
    mark = f.get("mark", "")
    head = (mark + " ") if mark else ""
    out = "📐 " + head + f["answer"]
    if f.get("cite"):
        out += "\n📖 מקור: " + f["cite"]
    if mark in _MARK_LEGEND:
        out += "\n%s = %s" % (mark, _MARK_LEGEND[mark])
    out += "\nℹ️ " + DISCLAIMER
    return out


def _tokens(t):
    return [w for w in re.split(r"[^0-9a-zא-ת.]+", _norm(t))
            if len(w) >= 2 and w not in _STOP]


def _search_modules(text, limit=6):
    _load()
    toks = set(_tokens(text))
    if not toks:
        return []
    hits = []
    for m in _CACHE["mods"]:
        body = m["text"].lower()
        title = m["title"].lower()
        score = 0
        for tk in toks:
            score += min(body.count(tk), 4)   # cap so long modules don't dominate
            if tk in title:
                score += 8   # title match is a strong signal
        if score <= 0:
            continue
        # best snippet: a heading/line containing the most query tokens
        best_line, best_lc = "", 0
        for ln in m["text"].splitlines():
            lc = sum(1 for tk in toks if tk in ln.lower())
            if lc > best_lc:
                best_lc, best_line = lc, ln.strip("# ").strip()
        hits.append({"slug": m["slug"], "title": m["title"],
                     "score": score, "snippet": best_line[:180]})
    hits.sort(key=lambda h: h["score"], reverse=True)
    return hits[:limit]


# ---- public API -----------------------------------------------------------
def consult(text):
    """Return a cited Hebrew answer, or None to escalate to the Claude tier."""
    _load()
    t = _norm(text)
    if not t:
        return None
    # 1) curated verified facts (best keyword coverage wins)
    best, best_sc = None, 0
    for f in _CACHE["facts"]:
        if f.get("all") and not all(k in t for k in f["all"]):
            continue
        sc = sum(1 for k in f.get("keys", []) if k in t)
        if sc > best_sc:
            best, best_sc = f, sc
    if best and best_sc > 0:
        ans = _fmt_fact(best)
        # add a "read more" pointer to the explicitly-linked module
        slug = FACT_MODULE.get(best.get("id"))
        title = None
        if slug:
            for m in _CACHE["mods"]:
                if m["slug"] == slug:
                    title = m["title"]
                    break
        if title:
            ans += "\n🔎 להרחבה: מודול «%s» בלשונית 📐 תקנים." % title
        return ans
    # 2) fall back to module search
    hits = _search_modules(text, 3)
    if hits and hits[0]["score"] >= 4:
        top = hits[0]
        out = "📐 הנושא מכוסה במודול «%s» (לשונית 📐 תקנים)." % top["title"]
        if top["snippet"]:
            out += "\n• %s" % top["snippet"]
        if len(hits) > 1:
            out += "\nמודולים קשורים: " + ", ".join(h["title"] for h in hits[1:])
        out += "\nℹ️ " + DISCLAIMER
        return out
    return None


def search(query, limit=6):
    return _search_modules(query, limit)


def modules():
    _load()
    return [{"slug": m["slug"], "title": m["title"], "file": m["file"],
             "kind": m.get("kind", "תקן")}
            for m in _CACHE["mods"]]


def module_md(slug):
    _load()
    for m in _CACHE["mods"]:
        if m["slug"] == slug:
            return m["text"]
    return None
