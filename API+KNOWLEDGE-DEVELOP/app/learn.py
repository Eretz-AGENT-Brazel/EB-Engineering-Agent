"""
learn.py - analyze a learning-mode recording into a Hebrew digest + accumulate
patterns into knowledge/LEARNED_PATTERNS.md.

The loop: record (L0/L1) -> digest here -> Amir sees what the agent learned ->
approves -> patterns get codified into nlu.py / eb_api macros over time.
"""
import os
import json
import glob
import time
import collections

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROJECTS = os.path.join(ROOT, "projects")
PATTERNS = os.path.join(ROOT, "knowledge", "LEARNED_PATTERNS.md")


def latest_session(pid):
    d = os.path.join(PROJECTS, pid, "learning")
    files = sorted(glob.glob(os.path.join(d, "session_*.jsonl")))
    # newest non-trivial session (more than just start/stop)
    for f in reversed(files):
        try:
            if len(open(f, encoding="utf-8-sig").readlines()) > 2:
                return f
        except Exception:
            pass
    return files[-1] if files else None


def _events(path):
    out = []
    for line in open(path, encoding="utf-8-sig"):
        line = line.strip()
        if line:
            try:
                out.append(json.loads(line))
            except Exception:
                pass
    return out


def digest(session_file):
    ev = _events(session_file)
    cmds = [e.get("name", "?") for e in ev if e.get("ev") == "cmd_start"]
    objs = [e.get("class", "?") for e in ev if e.get("ev") == "obj_add"]
    erases = sum(1 for e in ev if e.get("ev") == "obj_erase")
    cancels = sum(1 for e in ev if e.get("ev") == "cmd_cancel")
    cfreq = collections.Counter(cmds)
    ofreq = collections.Counter(objs)
    bigrams = collections.Counter(zip(cmds, cmds[1:]))
    undo_after = collections.Counter()
    for a, b in zip(cmds, cmds[1:]):
        if b.upper() in ("U", "UNDO"):
            undo_after[a] += 1

    L = []
    L.append("📊 **סיכום למידה** (%s)" % os.path.basename(session_file))
    L.append("• %d פקודות, %d אובייקטים נוצרו, %d נמחקו, %d בוטלו." % (len(cmds), len(objs), erases, cancels))
    if cfreq:
        top = ", ".join("%s×%d" % (c, n) for c, n in cfreq.most_common(6))
        L.append("• פקודות נפוצות: %s" % top)
    if ofreq:
        topo = ", ".join("%s×%d" % (c, n) for c, n in ofreq.most_common(6))
        L.append("• אובייקטים: %s" % topo)
    seqs = [("%s→%s" % (a, b), n) for (a, b), n in bigrams.most_common(4) if n > 1]
    if seqs:
        L.append("• רצפים חוזרים: " + ", ".join("%s (×%d)" % (s, n) for s, n in seqs))
    if undo_after:
        L.append("• ⚠️ ביטול (UNDO) אחרי: " + ", ".join("%s×%d" % (c, n) for c, n in undo_after.most_common(3))
                 + " — סימן שהפעולה הזו איטית/בעייתית עבורך.")
    # proposals
    props = []
    for (a, b), n in bigrams.most_common(3):
        if n > 1 and b.upper() not in ("U", "UNDO"):
            props.append("מאקרו שמריץ %s ואז %s ברצף" % (a, b))
    for c, n in cfreq.most_common(3):
        props.append("פקודה מהירה בשפה חופשית ל-%s (השתמשת ×%d)" % (c, n))
    if props:
        L.append("💡 **הצעות ללמידה:** " + "; ".join(props[:4]))
        L.append("אם תאשר, אוסיף אותן ל-API ולשפה של הסוכן.")

    summary = "\n".join(L)
    _append_patterns(session_file, cfreq, ofreq, bigrams)
    return summary


def _append_patterns(session_file, cfreq, ofreq, bigrams):
    try:
        block = ["\n## %s — %s" % (time.strftime("%Y-%m-%d %H:%M"), os.path.basename(session_file))]
        block.append("- commands: " + json.dumps(dict(cfreq), ensure_ascii=False))
        block.append("- objects: " + json.dumps(dict(ofreq), ensure_ascii=False))
        seqs = {("%s>%s" % (a, b)): n for (a, b), n in bigrams.items() if n > 1}
        block.append("- sequences: " + json.dumps(seqs, ensure_ascii=False))
        header = "" if os.path.exists(PATTERNS) else "# LEARNED PATTERNS — what the agent has observed Amir do\n"
        with open(PATTERNS, "a", encoding="utf-8") as f:
            f.write(header + "\n".join(block) + "\n")
    except Exception:
        pass


def analyze_active(pid):
    if not pid:
        return "אין פרויקט פעיל."
    f = latest_session(pid)
    if not f:
        return "לא נמצאה הקלטת למידה. הדלק 🎓 מצב למידה ועבוד קצת קודם."
    return digest(f)
