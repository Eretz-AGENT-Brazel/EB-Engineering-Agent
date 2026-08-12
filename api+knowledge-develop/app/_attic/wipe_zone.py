# -*- coding: utf-8 -*-
"""
wipe_zone.py — delete everything beyond a given X (the smoke-test zone).

Work rule: smoke tests live at X>=40000 and are removed immediately, so the real
model never accumulates test artefacts (this went wrong twice before).
"""
import os
import sys

APP = os.path.dirname(os.path.abspath(__file__))
PLUG = os.path.join(APP, "plugin")
sys.path.insert(0, APP)
import eb_api  # noqa: E402


def log(m):
    try:
        sys.stdout.write(m + "\n"); sys.stdout.flush()
    except Exception:
        pass


def xyz(s):
    try:
        return tuple(float(x) for x in s.split(","))
    except Exception:
        return None


def main():
    minx = float(sys.argv[1]) if len(sys.argv) > 1 else 40000.0
    log("scanning for objects at X >= %g ..." % minx)
    log("  " + str(eb_api.run("dumpfull2", out="eb_wipe_scan.txt", wait=180)))

    victims = []
    p = os.path.join(PLUG, "eb_wipe_scan.txt")
    for line in open(p, encoding="utf-8-sig", errors="replace"):
        f = line.rstrip("\n").split("\t")
        if not f or not f[0]:
            continue
        h = f[1] if len(f) > 1 else ""
        pt = None
        if f[0] == "SHAPE" and len(f) >= 6:
            pt = xyz(f[4])
        elif f[0] in ("PLATE", "BOLT") and len(f) >= 3:
            pt = xyz(f[2])
        elif f[0] == "OTHER":
            continue
        if pt and pt[0] >= minx:
            victims.append((h, f[0]))

    log("  found %d objects to delete" % len(victims))
    ok = fail = 0
    for h, kind in victims:
        r = eb_api.delete(h)
        if isinstance(r, str) and r.startswith("EB_OK"):
            ok += 1
        else:
            fail += 1
            log("    FAIL %s %s: %s" % (kind, h, str(r)[:60]))
    log("  deleted %d, failed %d" % (ok, fail))
    log("  " + str(eb_api.run("whoami", wait=25)))
    log("  " + str(eb_api.run("dumpholes", out="eb_holes_after_wipe.txt", wait=90)))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8-sig", errors="replace")
    except Exception:
        pass
    main()
