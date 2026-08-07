# -*- coding: utf-8 -*-
"""
exam5_dedupe.py — clean up what DeepClone did to the base-plate CONNECTION.

Cloning a detail that contains a base-plate connection re-runs that connection in
each copy. So every copy ended up with:
   * 4 EXTRA anchors — the connection's own, on top of the 24 that were cloned
   * only 4 holes in the base plate — the connection re-drills its own 4 corners,
     and the 2 centre holes I drilled by hand do not survive the clone

Fix, per position: delete the duplicate anchors (same point, twice) and drill the
two missing centre holes.
"""
import math
import os
import sys
import time

APP = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(APP)
PLUG = os.path.join(APP, "plugin")
sys.path.insert(0, APP)
import eb_api  # noqa: E402

HOLE = 28.0
LOG = []


def log(m):
    LOG.append(m)
    try:
        sys.stdout.write(m + "\n"); sys.stdout.flush()
    except Exception:
        pass


def ok(r):
    return isinstance(r, str) and r.startswith("EB_OK")


def anchors():
    for _ in range(6):
        try:
            out = []
            for e in eb_api._app().ActiveDocument.ModelSpace:
                try:
                    if e.ObjectName != "Ks_VolBody":
                        continue
                    mn, mx = e.GetBoundingBox()
                    sz = (mx[0]-mn[0], mx[1]-mn[1], mx[2]-mn[2])
                    if 20 < max(sz) < 1500:
                        out.append({"h": e.Handle, "sz": sz,
                                    "c": ((mn[0]+mx[0])/2, (mn[1]+mx[1])/2,
                                          (mn[2]+mx[2])/2)})
                except Exception:
                    pass
            return out
        except Exception:
            time.sleep(2)
    return []


def main():
    t0 = time.time()
    log("=" * 70)
    log("DEDUPE + complete the holes the clone did not carry")
    log("=" * 70)

    # ---- 1. duplicate anchors: same point, more than once ----
    al = anchors()
    log("\n[1] anchors in model: %d" % len(al))
    buckets = {}
    for a in al:
        k = (round(a["c"][0], 1), round(a["c"][1], 1), round(a["c"][2], 1))
        buckets.setdefault(k, []).append(a)
    dups = {k: v for k, v in buckets.items() if len(v) > 1}
    log("    distinct positions: %d | positions holding more than one: %d"
        % (len(buckets), len(dups)))
    killed = 0
    for k, v in dups.items():
        for a in v[1:]:                       # keep one, delete the rest
            if ok(eb_api.delete(a["h"])):
                killed += 1
    log("    duplicates removed: %d" % killed)
    log("    anchors now: %d (want 480)" % len(anchors()))

    # ---- 2. base plates that lost their two centre holes ----
    log("\n[2] finding base plates with only 4 holes")
    eb_api.run("dumpholes", out="eb_dd.txt", wait=300)
    eb_api.run("dumpfull2", out="eb_ddf.txt", wait=300)

    shapes = {}
    for line in open(os.path.join(PLUG, "eb_ddf.txt"), encoding="utf-8-sig", errors="replace"):
        f = line.rstrip("\n").split("\t")
        if f[0] == "SHAPE" and len(f) >= 7:
            try:
                p1 = [float(v) for v in f[4].split(",")]
                p2 = [float(v) for v in f[5].split(",")]
            except Exception:
                continue
            if abs(p2[2]-p1[2]) < 5 and p1[2] < 100:     # flat, near the floor
                shapes[f[1]] = {"c": ((p1[0]+p2[0])/2, (p1[1]+p2[1])/2, p1[2])}

    holes = {}
    pts = {}
    for line in open(os.path.join(PLUG, "eb_dd.txt"), encoding="utf-8-sig", errors="replace"):
        f = line.rstrip("\n").split("\t")
        if f[0] == "OBJ" and len(f) >= 5:
            holes[f[1]] = int(f[4])
        elif f[0] == "HOLE" and len(f) >= 6:
            pts.setdefault(f[1], []).append([float(v) for v in f[5].split(",")])

    short = [h for h in shapes if holes.get(h, 0) == 4]
    log("    base plates: %d | with only 4 holes: %d" % (len(shapes), len(short)))

    drilled = 0
    for h in short:
        p = pts.get(h, [])
        if len(p) < 4:
            continue
        xs = sorted(set(round(q[0], 1) for q in p))
        ys = sorted(set(round(q[1], 1) for q in p))
        z = p[0][2]
        # the two missing holes are the mid-points of the long side
        if (max(xs)-min(xs)) > (max(ys)-min(ys)):
            mids = [((min(xs)+max(xs))/2.0, y) for y in ys]
        else:
            mids = [(x, (min(ys)+max(ys))/2.0) for x in xs]
        for (mx_, my_) in mids:
            if ok(eb_api.run("drill", hosts=h, at="%.3f,%.3f,%.3f" % (mx_, my_, z),
                             dia=HOLE, n="0,0,1", wait=40, _log=False)):
                drilled += 1
    log("    holes drilled: %d (want %d)" % (drilled, len(short)*2))

    # ---- 3. verify ----
    log("\n[3] VERIFY")
    fin = anchors()
    log("    " + str(eb_api.run("dumpholes", out="eb_dd2.txt", wait=300)))
    tot = 0
    dist = {}
    for line in open(os.path.join(PLUG, "eb_dd2.txt"), encoding="utf-8-sig", errors="replace"):
        f = line.rstrip("\n").split("\t")
        if f[0] == "OBJ" and len(f) >= 5 and int(f[4]) > 0:
            n = int(f[4])
            tot += n
            dist[n] = dist.get(n, 0) + 1
    log("    anchors: %d (want 480)" % len(fin))
    log("    holes  : %d (want 480) | per-part: %s" % (tot, dist))
    log("    " + str(eb_api.run("dumpfull2", out="eb_dd3.txt", wait=300)))
    for i in range(5):
        try:
            d = eb_api._app().ActiveDocument
            d.Save(); time.sleep(5); log("    saved"); break
        except Exception:
            time.sleep(3)
    log("\n%.1f min" % ((time.time()-t0)/60))
    open(os.path.join(ROOT, "projects", "שיעור-5", "files", "dedupe.log"),
         "w", encoding="utf-8").write("\n".join(LOG))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
