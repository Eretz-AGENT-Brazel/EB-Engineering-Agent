# -*- coding: utf-8 -*-
"""night.py -- the overnight work queue for the "learn an existing model" track.

Amir drops drawings into ONE folder and goes to sleep. The folder IS the queue: every .dwg in
it that has no finished rebuild is pending. A scheduled run asks this file what to do next,
does one model end to end, records the result, and exits -- so a crash, a reboot or a closed
laptop costs at most the model in flight.

    python app/night.py list                     what is pending / done / parked
    python app/night.py next                     full path of the next model, or NONE
    python app/night.py slug  <dwg>              the projects/ folder name for a drawing
    python app/night.py set   <dwg> <status> [note]     pending|working|done|partial|parked

The queue file is MARKDOWN and lives in the repo on purpose: Amir reads it on GitHub in the
morning without opening anything.
"""
import io
import os
import re
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
DEV = os.path.dirname(HERE)
# ⛔ THE SOURCES LIVE INSIDE THE PROJECT, next to the rebuilds they feed. Amir, 19/08/2026:
# "כל הידע שלנו מרוכז רק שם, לא בשום תיקייה נפרדת". Moved that day out of a desktop
# folder; all seven files verified by sha256 before and after the move. Relative to this file,
# so the queue survives the repo being moved or cloned somewhere else.
SOURCE_DIR = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)),
                                          os.pardir, "projects", "sources"))
PROJECTS = os.path.join(DEV, "projects")
QUEUE = os.path.join(PROJECTS, "NIGHT-QUEUE.md")

STATUSES = ("pending", "working", "done", "partial", "parked")


def slug(dwg):
    """model 3_kuperdam DRILL BASE -3 - FAB.dwg -> model-3-kuperdam-drill-base-3-fab"""
    s = os.path.splitext(os.path.basename(dwg))[0].lower()
    s = re.sub(r"[^a-z0-9]+", "-", s).strip("-")
    return re.sub(r"-+", "-", s)


def drawings():
    if not os.path.isdir(SOURCE_DIR):
        return []
    return sorted(os.path.join(SOURCE_DIR, f) for f in os.listdir(SOURCE_DIR)
                  if f.lower().endswith(".dwg"))


def read_queue():
    """{basename: (status, note)} -- absent from the file means it has never been touched."""
    out = {}
    if not os.path.exists(QUEUE):
        return out
    for line in io.open(QUEUE, encoding="utf-8"):
        m = re.match(r"^\|\s*`([^`]+)`\s*\|\s*(\w+)\s*\|(.*)\|\s*$", line.strip())
        if m:
            out[m.group(1)] = (m.group(2), m.group(3).strip())
    return out


def write_queue(state):
    # ⛔⛔ AN EMPTY SOURCE FOLDER MUST NOT EMPTY THE QUEUE. Measured 19/08/2026: while the
    # sources were being moved into the project, SOURCE_DIR pointed for one minute at the folder
    # that had just been vacated, `drawings()` returned [], and one `night.py list` rewrote the
    # table with ZERO rows -- wiping four `done` and two `partial` marks in silence. The statuses
    # were only recoverable because the file is committed. The folder is the queue, so "the folder
    # is gone" is never a reason to declare the work undone: refuse, and say why.
    found = drawings()
    if not found and state:
        raise RuntimeError(
            "refusing to rewrite the queue: SOURCE_DIR holds no .dwg (%s) while the queue still "
            "records %d model(s). Fix the path -- do not let a missing folder erase the marks."
            % (SOURCE_DIR, len(state)))
    rows = []
    for p in found:
        b = os.path.basename(p)
        st, note = state.get(b, ("pending", ""))
        rows.append(u"| `%s` | %s | %s |" % (b, st, note))
    body = u"""<div dir="rtl" align="right">

# \U0001F319 NIGHT QUEUE — התור של הלילה

*התיקייה `%s` **היא** התור: כל `.dwg` בה מקבל שורה. אמיר מפיל קבצים לשם, הסוכן לוקח אותם
אחד-אחד. הקובץ הזה נכתב ע"י `app/night.py` — לא עורכים אותו ביד באמצע ריצה.*

| הסטטוס | פירושו |
|---|---|
| `pending` | ממתין |
| `working` | בעבודה עכשיו (ריצה אחת בכל רגע) |
| `done` | שוחזר ואומת מול המקור — שערי הקבלה ב-`projects/<שם>/README.md` |
| `partial` | נבנה חלקית ונשמר; החוסם רשום ב-README ובהערה כאן |
| `parked` | לא ניתן להתקדם בלי אמיר — הסיבה בהערה |

| המודל | סטטוס | הערה |
|---|---|---|
%s

⚠️ **שאלות הבנה נאספות ל-`NIGHT-QUESTIONS.md` ואינן עוצרות את המידול** — הוראת אמיר,
‏18/08/2026: *"אלו מודלים שכבר בוצעו בשטח והיו מדויקים ביותר… לגבי המידול, אין שום סיבה לעצור."*

</div>
""" % (SOURCE_DIR, u"\n".join(rows))
    with io.open(QUEUE + ".new", "w", encoding="utf-8", newline="\n") as f:
        f.write(body)
    os.replace(QUEUE + ".new", QUEUE)


def main(argv):
    state = read_queue()
    cmd = (argv[0] if argv else "list").lower()

    if cmd == "list":
        write_queue(state)
        for p in drawings():
            b = os.path.basename(p)
            st, note = state.get(b, ("pending", ""))
            print("%-10s %-50s %s" % (st, b, note))
        return 0

    if cmd == "next":
        # never hand out a second model while one is marked working
        for p in drawings():
            st, note = state.get(os.path.basename(p), ("pending", ""))
            if st == "working":
                # a run in flight owns the queue. Only when the mark has gone stale does the
                # next run reclaim it -- otherwise two runs fight over one drawing.
                m = re.search(r"started=(\d+)", note)
                age = int((time.time() - int(m.group(1))) / 60) if m else 999
                print("BUSY %s age=%dmin" % (p, age))
                return 0
        for p in drawings():
            if state.get(os.path.basename(p), ("pending", ""))[0] == "pending":
                print(p)
                return 0
        print("NONE")
        return 0

    if cmd == "slug":
        print(slug(argv[1]))
        return 0

    if cmd == "set":
        b = os.path.basename(argv[1])
        st = argv[2].lower()
        if st not in STATUSES:
            print("status must be one of: " + ", ".join(STATUSES))
            return 2
        note = " ".join(argv[3:]).replace("|", "/")
        if st == "working":
            note = (note + " started=%d" % int(time.time())).strip()
        state[b] = (st, note)
        write_queue(state)
        print("%s -> %s" % (b, st))
        return 0

    print(__doc__)
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
