# -*- coding: utf-8 -*-
"""
lesson.py — LEARNING MODE, driven from the chat.

The teaching loop Amir defined:
  1. he states the lesson number
  2. he states the lesson topic
  3. I turn learning mode on, he models, I watch; he stops so I can analyse
  4. we discuss what he wants me to take from it
  5. we keep mining the lesson until he closes it

Design rule that matters: WHILE AMIR IS WORKING I DO NOT TOUCH AUTOCAD.
Sending commands to a busy AutoCAD interferes with his session (and blocks).
The plugin's recorder writes a jsonl log from inside the process; `watch` only
reads that file from disk. AutoCAD is touched exactly twice: at start (snapshot
+ recorder on) and at stop (recorder off + snapshot).

What gets recorded (v21 upgrade — this is what lesson 1 was missing):
  * every command he runs (and every UNDO / cancel)
  * every object created, and then — once the command finishes — WHAT it is:
    profile + catalog + length + end points, or plate dims + contour vertices,
    plus holes drilled and any CONNECTION (type, plates, bolts) on it.

Usage
  python lesson.py wait                     # wait for AutoCAD, load plugin
  python lesson.py start 4 "topic text"     # open the lesson, recorder ON
  python lesson.py watch [seconds]          # live view, no AutoCAD contact
  python lesson.py stop                     # recorder OFF + closing snapshot
  python lesson.py analyze                  # the report
"""
import json
import os
import re
import sys
import time
from collections import Counter, OrderedDict, defaultdict

APP = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(APP)
PLUG = os.path.join(APP, "plugin")
LESSONS = os.path.join(ROOT, "projects")
STATE = os.path.join(ROOT, "data", "lesson_state.json")

sys.path.insert(0, APP)
import eb_api  # noqa: E402

# events that are noise for teaching purposes
NOISE_CMDS = {"", "U", "REDO"}


def log(m):
    try:
        sys.stdout.write(m + "\n")
        sys.stdout.flush()
    except Exception:
        pass


def st_load():
    if os.path.exists(STATE):
        try:
            return json.load(open(STATE, encoding="utf-8"))
        except Exception:
            pass
    return {}


def st_save(s):
    os.makedirs(os.path.dirname(STATE), exist_ok=True)
    json.dump(s, open(STATE, "w", encoding="utf-8"), ensure_ascii=False, indent=1)


# ---------------------------------------------------------------- connect
def wait_for_cad(timeout=600):
    """Poll until AutoCAD is up and the plugin answers. Never launches anything."""
    log("waiting for AutoCAD + ProSteel (open it whenever you're ready)...")
    t0 = time.time()
    shown = False
    while time.time() - t0 < timeout:
        try:
            r = eb_api.run("ping", wait=12)
            if isinstance(r, str) and r.startswith("EB_OK"):
                log("connected: " + r)
                log("           " + str(eb_api.run("whoami", wait=20)))
                return True
        except Exception:
            pass
        if not shown:
            log("   (still waiting — I'll keep checking every 5s)")
            shown = True
        time.sleep(5)
    log("timed out waiting for AutoCAD")
    return False


# ---------------------------------------------------------------- snapshot
def snapshot(tag, folder):
    """A full picture of the model: geometry + holes + connections.
    Taken only when Amir is NOT modelling."""
    out = {}
    log("  snapshot [%s] ..." % tag)
    for op, name, kw in (
        ("dumpfull2", "full", {"out": "eb_full_%s.txt" % tag}),
        ("dumpholes", "holes", {"out": "eb_holes_%s.txt" % tag, "maxx": 1e9}),
        ("dumppoly", "poly", {"out": "eb_poly_%s.txt" % tag, "maxx": 1e9}),
        ("connscan", "conn", {"out": "eb_conn_%s.txt" % tag, "maxx": 1e9}),
    ):
        r = eb_api.run(op, wait=240, **kw)
        out[name] = str(r)
        log("     %-10s %s" % (name, r))
        src = os.path.join(PLUG, kw["out"])
        if os.path.exists(src):
            try:
                dst = os.path.join(folder, kw["out"])
                open(dst, "w", encoding="utf-8").write(
                    open(src, encoding="utf-8-sig", errors="replace").read())
            except Exception as e:
                log("     copy failed: %s" % str(e)[:60])
    return out


def parse_counts(s):
    return dict((m.group(1), m.group(2)) for m in re.finditer(r"(\w+)=([\d.]+)", s or ""))


# ---------------------------------------------------------------- start
def start(num, topic):
    if not wait_for_cad():
        return
    name = "lesson-%s" % num
    folder = os.path.join(LESSONS, name)
    lf = os.path.join(folder, "learning")
    os.makedirs(lf, exist_ok=True)
    logpath = os.path.join(lf, "session_%s.jsonl" % time.strftime("%Y%m%d_%H%M%S"))

    log("\n=== LESSON %s — %s ===" % (num, topic))
    log("folder: %s" % folder)

    before = snapshot("L%s_before" % num, folder)

    log("\n  recorder ON")
    r = eb_api.run("learn_on", log=logpath, wait=30)
    log("     " + str(r))
    if not (isinstance(r, str) and r.startswith("EB_OK")):
        log("  !! recorder did not start — stopping here")
        return

    st = st_load()
    st.update({"num": str(num), "topic": topic, "folder": folder,
               "logpath": logpath, "started": time.strftime("%Y-%m-%d %H:%M:%S"),
               "before": before, "seen": 0, "part": 1, "parts": []})
    st_save(st)
    log("\nrecording. Model freely — I will not touch AutoCAD until you stop.")
    log("log: %s" % logpath)


def resume(topic):
    """Continue the SAME lesson with a new stage — nothing already recorded is
    overwritten. Used when Amir extends the situation (e.g. the engineer asks for
    a bigger base plate with ribs) and wants to show how he EDITS existing steel."""
    st = st_load()
    if not st.get("num"):
        log("no lesson to resume — use: lesson.py start <n> \"topic\"")
        return
    if not wait_for_cad():
        return
    num = st["num"]
    folder = st["folder"]
    part = int(st.get("part", 1)) + 1

    # archive the stage that just finished, so nothing is lost
    hist = st.get("parts", [])
    hist.append({"part": st.get("part", 1), "topic": st.get("topic", ""),
                 "logpath": st.get("logpath", ""), "before": st.get("before", {}),
                 "after": st.get("after", {}), "started": st.get("started", ""),
                 "stopped": st.get("stopped", "")})

    lf = os.path.join(folder, "learning")
    os.makedirs(lf, exist_ok=True)
    logpath = os.path.join(lf, "session_%s_part%d.jsonl" % (time.strftime("%Y%m%d_%H%M%S"), part))

    log("\n=== LESSON %s · STAGE %d — %s ===" % (num, part, topic))
    # the previous stage's "after" IS this stage's "before" as far as the model
    # goes, but take a fresh snapshot anyway so the stage stands on its own
    before = snapshot("L%sp%d_before" % (num, part), folder)

    log("\n  recorder ON (new log, previous stage untouched)")
    r = eb_api.run("learn_on", log=logpath, wait=30)
    log("     " + str(r))
    if not (isinstance(r, str) and r.startswith("EB_OK")):
        log("  !! recorder did not start")
        return

    st.update({"topic": topic, "logpath": logpath, "part": part, "parts": hist,
               "started": time.strftime("%Y-%m-%d %H:%M:%S"), "before": before})
    st.pop("after", None)
    st.pop("stopped", None)
    st_save(st)
    log("\nrecording stage %d. Nothing from stage 1 was overwritten." % part)
    log("log: %s" % logpath)


# ---------------------------------------------------------------- watch
def read_events(path):
    ev = []
    if not os.path.exists(path):
        return ev
    for line in open(path, encoding="utf-8-sig", errors="replace"):
        line = line.strip()
        if not line:
            continue
        try:
            ev.append(json.loads(line))
        except Exception:
            pass
    return ev


def summarise(ev):
    cmds = [e for e in ev if e.get("ev") == "cmd_start"]
    ends = [e for e in ev if e.get("ev") == "cmd_end"]
    cancels = [e for e in ev if e.get("ev") == "cmd_cancel"]
    adds = [e for e in ev if e.get("ev") == "obj_add"]
    dets = [e for e in ev if e.get("ev") == "obj_detail"]
    erases = [e for e in ev if e.get("ev") == "obj_erase"]
    names = Counter(e.get("name", "") for e in cmds if e.get("name") not in NOISE_CMDS)
    undos = sum(1 for e in cmds if e.get("name", "").upper() in ("U", "UNDO"))
    return {"events": len(ev), "cmds": len(cmds), "ends": len(ends),
            "cancels": len(cancels), "adds": len(adds), "details": len(dets),
            "erases": len(erases), "top": names.most_common(12), "undos": undos,
            "det": dets}


def watch(seconds=0):
    st = st_load()
    p = st.get("logpath", "")
    if not p:
        log("no lesson running (use: lesson.py start <n> \"topic\")")
        return
    log("watching %s" % os.path.basename(p))
    log("(reading the log only — AutoCAD is untouched)\n")
    t0 = time.time()
    last = -1
    while True:
        ev = read_events(p)
        s = summarise(ev)
        if s["events"] != last:
            last = s["events"]
            log("[%5.0fs] events=%-5d cmds=%-4d created=%-4d detailed=%-4d erased=%-3d cancels=%d"
                % (time.time() - t0, s["events"], s["cmds"], s["adds"],
                   s["details"], s["erases"], s["cancels"]))
            if s["top"]:
                log("          commands: " + ", ".join("%s x%d" % (n, c) for n, c in s["top"][:6]))
            for d in s["det"][-3:]:
                log("          + " + describe(d))
        if seconds and time.time() - t0 > seconds:
            break
        if not seconds:
            break
        time.sleep(4)


def describe(d):
    """one readable line for a created object"""
    bits = [d.get("class", "?")]
    if d.get("profile"):
        bits.append(d["profile"])
        if d.get("len"):
            bits.append("L=%s" % d["len"])
    if d.get("dims"):
        bits.append(d["dims"])
        if d.get("verts"):
            bits.append("%s verts" % d["verts"])
    if d.get("holes"):
        bits.append("HOLES=%s" % d["holes"])
    if d.get("conn"):
        bits.append("CONN[%s]" % d["conn"])
    if d.get("layer"):
        bits.append("(%s)" % d["layer"])
    if d.get("cmd"):
        bits.append("<- %s" % d["cmd"])
    return "  ".join(str(b) for b in bits)


# ---------------------------------------------------------------- stop
def stop():
    st = st_load()
    if not st.get("logpath"):
        log("no lesson running")
        return
    log("recorder OFF")
    log("   " + str(eb_api.run("learn_off", wait=30)))
    part = int(st.get("part", 1))
    tag = "L%s_after" % st.get("num", "x") if part == 1 \
        else "L%sp%d_after" % (st.get("num", "x"), part)
    after = snapshot(tag, st["folder"])
    st["after"] = after
    st["stopped"] = time.strftime("%Y-%m-%d %H:%M:%S")
    st_save(st)
    log("\nlesson %s recorded. run: python lesson.py analyze" % st.get("num"))


# ---------------------------------------------------------------- analyze
def analyze():
    st = st_load()
    if not st.get("logpath"):
        log("no lesson recorded")
        return
    ev = read_events(st["logpath"])
    s = summarise(ev)
    num, topic = st.get("num", "?"), st.get("topic", "")

    L = []

    def w(m):
        L.append(m)
        log(m)

    w("=" * 76)
    w("LESSON %s — %s" % (num, topic))
    w("=" * 76)
    w("recorded %s -> %s" % (st.get("started", "?"), st.get("stopped", "?")))

    w("\n### 1. WHAT CHANGED IN THE MODEL (before -> after)")
    b, a = parse_counts(st.get("before", {}).get("full", "")), parse_counts(st.get("after", {}).get("full", ""))
    for k in ("shapes", "plates", "bolts", "other"):
        if k in b or k in a:
            w("  %-8s %6s -> %-6s (%+d)" % (k, b.get(k, "0"), a.get(k, "0"),
                                            int(float(a.get(k, 0))) - int(float(b.get(k, 0)))))
    bh = parse_counts(st.get("before", {}).get("holes", ""))
    ah = parse_counts(st.get("after", {}).get("holes", ""))
    if bh or ah:
        w("  %-8s %6s -> %-6s (%+d)" % ("holes", bh.get("holes", "0"), ah.get("holes", "0"),
                                        int(float(ah.get("holes", 0))) - int(float(bh.get("holes", 0)))))
    bc = parse_counts(st.get("before", {}).get("conn", ""))
    ac = parse_counts(st.get("after", {}).get("conn", ""))
    if bc or ac:
        w("  %-8s %6s -> %-6s (%+d)" % ("joints", bc.get("links", "0"), ac.get("links", "0"),
                                        int(float(ac.get("links", 0))) - int(float(bc.get("links", 0)))))

    w("\n### 2. HOW HE WORKED (the method)")
    w("  events %d | commands %d | cancelled %d | created %d | erased %d"
      % (s["events"], s["cmds"], s["cancels"], s["adds"], s["erases"]))
    if s["cmds"]:
        w("  UNDO share: %.0f%%" % (100.0 * s["undos"] / s["cmds"]))
    w("  commands used:")
    for n, c in s["top"]:
        w("     %-24s x%d" % (n, c))

    w("\n### 3. WHAT HE BUILT (per object, with real parameters)")
    dets = s["det"]
    if not dets:
        w("  (no enriched objects — either nothing was created, or the")
        w("   command never fired cmd_end; check the raw log)")
    else:
        bycmd = defaultdict(list)
        for d in dets:
            bycmd[d.get("cmd", "?")].append(d)
        for cmd, arr in bycmd.items():
            w("\n  %s  ->  %d object(s)" % (cmd or "(no command)", len(arr)))
            fam = Counter()
            for d in arr:
                key = (d.get("class"), d.get("profile") or d.get("dims"),
                       d.get("verts"), d.get("holes"), d.get("conn"))
                fam[key] += 1
            for key, c in fam.most_common():
                cls, what, verts, holes, conn = key
                line = "     %-14s %-22s" % (cls or "?", what or "")
                if verts:
                    line += " verts=%s" % verts
                if holes:
                    line += " HOLES=%s" % holes
                if conn:
                    line += " CONN[%s]" % conn
                line += "  x%d" % c
                w(line)

        w("\n  joints created in this lesson:")
        cj = Counter(d["conn"] for d in dets if d.get("conn"))
        if cj:
            for k, c in cj.most_common():
                w("     %s  x%d" % (k, c))
        else:
            w("     (none)")

        w("\n  holes created in this lesson: %d objects carried holes"
          % sum(1 for d in dets if d.get("holes")))

    out = os.path.join(st["folder"], "files")
    os.makedirs(out, exist_ok=True)
    p = os.path.join(out, "lesson%s-analysis.md" % num)
    open(p, "w", encoding="utf-8").write("\n".join(L))
    log("\nsaved -> %s" % p)


# ---------------------------------------------------------------- main
if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8-sig", errors="replace")
    except Exception:
        pass
    a = sys.argv[1:] or ["status"]
    c = a[0]
    if c == "wait":
        wait_for_cad(int(a[1]) if len(a) > 1 else 600)
    elif c == "start":
        start(a[1] if len(a) > 1 else "X", " ".join(a[2:]) or "(no topic given)")
    elif c == "resume":
        resume(" ".join(a[1:]) or "(stage continues)")
    elif c == "watch":
        watch(int(a[1]) if len(a) > 1 else 0)
    elif c == "stop":
        stop()
    elif c == "analyze":
        analyze()
    elif c == "status":
        s = st_load()
        log("lesson: %s — %s" % (s.get("num", "-"), s.get("topic", "-")))
        log("log   : %s" % s.get("logpath", "-"))
        log("plugin: " + str(eb_api.run("learn_status", wait=25)))
    else:
        log(__doc__)
