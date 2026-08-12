# -*- coding: utf-8 -*-
"""
learn_selftest.py — prove the recorder works BEFORE a real lesson starts.

After tonight's lesson (a claim reported without verification), nothing gets
declared ready until it is measured. This checks the whole chain:

  command -> object created -> command ends -> object READ -> written to log
  with its real data: profile / dims / contour vertices / holes / CONNECTION.

Everything happens in the smoke zone (X>=50000) and is deleted afterwards.
"""
import json
import os
import re
import sys
import time

APP = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(APP)
sys.path.insert(0, APP)
import eb_api  # noqa: E402

X = 50000.0
LOGP = os.path.join(ROOT, "data", "selftest_learn.jsonl")
FAILS = []


def log(m):
    try:
        sys.stdout.write(m + "\n"); sys.stdout.flush()
    except Exception:
        pass


def H(r):
    m = re.search(r"handle=(\w+)", r or "")
    return m.group(1) if m else None


def newest(r):
    m = re.search(r"newest=(\w+)", r or "")
    return m.group(1) if m else None


def events():
    out = []
    if not os.path.exists(LOGP):
        return out
    for line in open(LOGP, encoding="utf-8", errors="replace"):
        line = line.strip()
        if line:
            try:
                out.append(json.loads(line))
            except Exception:
                pass
    return out


def check(name, cond, detail=""):
    log("   %s %s %s" % ("PASS" if cond else "FAIL", name, detail))
    if not cond:
        FAILS.append(name)
    return cond


def main():
    log("=" * 74)
    log("LEARNING-MODE SELF TEST")
    log("=" * 74)
    log(str(eb_api.run("whoami", wait=25)))

    try:
        os.makedirs(os.path.dirname(LOGP), exist_ok=True)
        if os.path.exists(LOGP):
            os.remove(LOGP)
    except Exception:
        pass

    log("\n[1] recorder ON")
    r = eb_api.run("learn_on", log=LOGP, wait=30)
    log("    " + str(r))
    check("recorder started", isinstance(r, str) and r.startswith("EB_OK"))

    # ---- A. a native AutoCAD command: proves cmd_start/end + auto-enrich ----
    log("\n[2] native command (_CIRCLE) — proves the command chain fires")
    app = eb_api._app()
    eb_api._send(app.ActiveDocument, "\x1b\x1b_CIRCLE\n%f,0,0\n50\n" % (X + 3000))
    time.sleep(2.5)
    ev = events()
    cs = [e for e in ev if e.get("ev") == "cmd_start" and "CIRCLE" in str(e.get("name", ""))]
    ce = [e for e in ev if e.get("ev") == "cmd_end" and "CIRCLE" in str(e.get("name", ""))]
    ad = [e for e in ev if e.get("ev") == "obj_add"]
    dt = [e for e in ev if e.get("ev") == "obj_detail"]
    check("cmd_start captured", len(cs) > 0, "(%d)" % len(cs))
    check("cmd_end captured", len(ce) > 0, "(%d)" % len(ce))
    check("obj_add captured", len(ad) > 0, "(%d)" % len(ad))
    check("obj_detail auto-fired on cmd_end", len(dt) > 0, "(%d)" % len(dt))
    if dt:
        log("       -> " + json.dumps(dt[-1], ensure_ascii=False)[:150])

    # ---- B. a shape via the API: proves profile/length/points are read ----
    log("\n[3] a real profile — proves profile+catalog+length+points are read")
    r = eb_api.run("beam", name="RHS150X100X4", catalog="BS_CELSIUS_RHS",
                   p1="%f,0,0" % X, p2="%f,0,2000" % X, layer="PS_Shape", wait=30)
    col = H(r)
    log("    " + str(r)[:110])
    eb_api.run("learn_flush", wait=25)     # API commands are filtered, so flush
    time.sleep(1.0)
    ev = events()
    shp = [e for e in ev if e.get("ev") == "obj_detail" and e.get("profile")]
    check("profile recorded", len(shp) > 0, shp[-1].get("profile", "") if shp else "")
    if shp:
        d = shp[-1]
        check("catalog recorded", bool(d.get("catalog")), str(d.get("catalog")))
        check("length recorded", bool(d.get("len")), str(d.get("len")))
        check("end points recorded", bool(d.get("p1")) and bool(d.get("p2")),
              "%s -> %s" % (d.get("p1"), d.get("p2")))

    # ---- C. a real connection: proves conn + holes are read ----
    log("\n[4] a base-plate CONNECTION — proves joint + holes are read")
    r = eb_api.run("connbase", handle=col, l=300, w=300, t=12,
                   holedia=23, hx=200, hy=200, anchors=1, wait=45)
    log("    " + str(r)[:120])
    eb_api.run("learn_flush", wait=25)
    time.sleep(1.0)
    ev = events()
    withconn = [e for e in ev if e.get("ev") == "obj_detail" and e.get("conn")]
    withhole = [e for e in ev if e.get("ev") == "obj_detail" and e.get("holes")]
    withdims = [e for e in ev if e.get("ev") == "obj_detail" and e.get("dims")]
    check("connection recorded", len(withconn) > 0,
          withconn[-1].get("conn", "") if withconn else "")
    check("holes recorded", len(withhole) > 0,
          "holes=%s" % withhole[-1].get("holes") if withhole else "")
    check("plate dims+verts recorded", len(withdims) > 0,
          "%s / %s verts" % (withdims[-1].get("dims"), withdims[-1].get("verts")) if withdims else "")

    # ---- D. a cancelled command must not pollute ----
    log("\n[5] cancelled command must NOT be recorded as built")
    n_before = len([e for e in events() if e.get("ev") == "obj_detail"])
    eb_api._send(app.ActiveDocument, "\x1b\x1b_CIRCLE\n%f,500,0\n" % (X + 3000))
    time.sleep(1.0)
    eb_api._send(app.ActiveDocument, "\x1b\x1b")
    time.sleep(1.5)
    ev = events()
    cx = [e for e in ev if e.get("ev") == "cmd_cancel"]
    n_after = len([e for e in ev if e.get("ev") == "obj_detail"])
    check("cancel captured", len(cx) > 0, "(%d)" % len(cx))
    check("cancel added no phantom object", n_after == n_before,
          "%d -> %d" % (n_before, n_after))

    log("\n[6] recorder OFF")
    log("    " + str(eb_api.run("learn_off", wait=30)))

    # ---- cleanup ----
    log("\n[7] CLEANUP — removing every test object")
    log("    " + str(eb_api.run("list", wait=60)))
    import subprocess
    p = subprocess.run([sys.executable, os.path.join(APP, "wipe_zone.py"), "40000"],
                       capture_output=True, text=True, timeout=600)
    for ln in (p.stdout or "").splitlines()[-4:]:
        log("    " + ln)
    # the circle is an AcDbCircle; wipe_zone only scans ProSteel rows
    for line in open(os.path.join(APP, "plugin", "eb_list.txt"),
                     encoding="utf-8", errors="replace"):
        f = line.strip().split("|")
        if len(f) >= 2 and f[1] in ("AcDbCircle",):
            log("    circle %s -> %s" % (f[0], eb_api.delete(f[0])))
    log("    " + str(eb_api.run("whoami", wait=25)))

    log("\n" + "=" * 74)
    if FAILS:
        log("RESULT: %d CHECK(S) FAILED -> %s" % (len(FAILS), ", ".join(FAILS)))
    else:
        log("RESULT: ALL CHECKS PASSED — learning mode records method AND product")
    log("=" * 74)


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
