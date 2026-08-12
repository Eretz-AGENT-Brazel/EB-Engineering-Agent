# -*- coding: utf-8 -*-
"""
r2_truth_test.py — R2.3: THE DRILLING TRUTH TEST.

The audit refuted the claim "bolts with hosts => ProSteel drilled real holes"
because it was never verified. This script settles it with evidence, not a
screenshot: build a plate + a bolt with hosts, then READ THE HOLES back.

Answer required: hosts => hole, YES or NO.
Runs in an empty scratch drawing, far from any real model.
"""
import os
import sys
import time

APP = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, APP)
import eb_api  # noqa: E402

X = 50000.0      # smoke zone, far away
LOG = []


def log(m):
    LOG.append(m)
    try:
        sys.stdout.write(m + "\n")
        sys.stdout.flush()
    except Exception:
        pass


def H(r):
    import re
    m = re.search(r"handle=(\w+)", r or "")
    return m.group(1) if m else None


def ok(r):
    return isinstance(r, str) and r.startswith("EB_OK")


def main():
    log("=" * 72)
    log("R2.3  DRILLING TRUTH TEST — does a bolt with hosts drill a real hole?")
    log("=" * 72)

    log("\n[0] plugin identity")
    log("    " + str(eb_api.run("whoami", wait=20)))

    log("\n[1] enum discovery (needed to pass correct ints, not guesses)")
    r = eb_api.run("enumdump", types="Drill", wait=25)
    log("    " + str(r))
    ep = os.path.join(APP, "plugin", "eb_enums.txt")
    if os.path.exists(ep):
        for line in open(ep, encoding="utf-8", errors="replace").read().splitlines():
            if line.startswith("ENUM"):
                log("    " + line)

    # ---- build the test connection: two plates + one bolt through both ----
    log("\n[2] build test assembly at X=%d (plate 200x200x10 + plate + bolt M20)" % X)
    p1 = eb_api.run("plate", center="%f,0,1000" % X, l=200, w=200, t=10,
                    ex="1,0,0", ey="0,1,0", ez="0,0,1", layer="PS_Plate", wait=25)
    log("    plate1: " + str(p1))
    h1 = H(p1)
    p2 = eb_api.run("plate", center="%f,0,1020" % X, l=200, w=200, t=10,
                    ex="1,0,0", ey="0,1,0", ez="0,0,1", layer="PS_Plate", wait=25)
    log("    plate2: " + str(p2))
    h2 = H(p2)

    if not (h1 and h2):
        log("    !! plates failed — aborting")
        return

    log("\n[3] holes BEFORE any bolt (baseline — must be 0)")
    for h in (h1, h2):
        log("    plate %s : %s" % (h, eb_api.run("holes", handle=h, wait=20)))

    log("\n[4] create bolt WITH hosts (exactly as build_from_scratch.py did)")
    b = eb_api.run("bolt", p1="%f,0,960" % X, p2="%f,0,1060" % X, dia=20,
                   style="DIN6914", hosts="%s,%s" % (h1, h2), len=100,
                   layer="PS_Bolt", wait=30)
    log("    bolt: " + str(b))
    hb = H(b)

    log("\n[5] holes AFTER the bolt  <<<< THE ANSWER")
    res = {}
    for h in (h1, h2):
        r = eb_api.run("holes", handle=h, wait=20)
        res[h] = r
        log("    plate %s : %s" % (h, r))

    got = 0
    for h, r in res.items():
        if r and "count=" in r:
            try:
                got += int(r.split("count=")[1].split()[0])
            except Exception:
                pass
    log("\n    >>> hosts => holes ?  %s   (total holes read back: %d)"
        % ("YES" if got > 0 else "NO", got))

    # ---- if hosts did NOT drill, prove the drill op does ----
    if got == 0:
        log("\n[6] hosts did NOT drill. Testing explicit PS drill (PsDrillObject)")
        d = eb_api.run("drill", hosts="%s,%s" % (h1, h2), at="%f,0,1010" % X,
                       dia=23, n="0,0,1", wait=30)
        log("    drill: " + str(d))
        for h in (h1, h2):
            log("    plate %s : %s" % (h, eb_api.run("holes", handle=h, wait=20)))
    else:
        log("\n[6] skipped (hosts already drill)")

    # ---- contour reading test on a plate ----
    log("\n[7] contour read test (PsPlate.GetPolygon)")
    log("    " + str(eb_api.run("platepoly", handle=h1, wait=20)))

    # ---- non-rectangular plate creation test (a rib shape) ----
    log("\n[8] non-rectangular plate (rib/gusset shape) creation test")
    pts = ";".join(["%f,600,1000" % X, "%f,750,1000" % X,
                    "%f,750,1100" % X, "%f,600,1150" % X])
    log("    " + str(eb_api.run("polyplate", pts=pts, t=10, layer="PS_Plate", wait=30)))

    log("\n[9] CLEANUP — delete everything in the smoke zone")
    log("    " + str(eb_api.run("dumpholes", out="eb_smoke_holes.txt", wait=30)))
    time.sleep(0.5)
    log("    wipe: " + str(eb_api.wipe_zone(X - 5000) if hasattr(eb_api, "wipe_zone") else "manual"))

    out = os.path.join(os.path.dirname(APP), "projects", "שיעור-3-מאפס",
                       "files", "R2-truth-test.log")
    open(out, "w", encoding="utf-8").write("\n".join(LOG))
    log("\nsaved -> " + out)


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
