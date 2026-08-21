# -*- coding: utf-8 -*-
"""Re-apply the bridge's cut planes and keep whichever result measures closer to the source.

⚠️ THE NAME OF THIS FILE IS A FOSSIL. It was written to test a flag, and the flag turned out
to do nothing. Kept as-is because what it does works, and because the wrong hypothesis is worth
reading next to the right one.

WHAT I THOUGHT: every cut plane in Bernie's model reads Flag=0 and every one the rebuild made
reads Flag=1 (PsCutPlane's default), so the flag was never transferred and that was the reason
311 plates carried a body 1 mm or more off -- 308 of them with MORE material. v206 added
`planecut flag=`, this pass tried both flags per part, and 148 of 232 parts improved with the
summed worst-axis error halving, 1,693.7 -> 931.6 mm. Consistent halving on every part.

WHAT IS ACTUALLY TRUE, from a clean A/B on plate 4A88B3 -> 57B1 afterwards:
    after killcut all           bbox y = 617.545      (the uncut body)
    flag=0 applied              bbox y = 546.224   stored Flag=1
    flag=1 applied              bbox y = 546.224   stored Flag=1
    the source wants                     522.451
**The flag makes no difference and is not even stored** -- PsCutObjects overwrites it. And a
fresh read of the whole rebuild confirms it: 4,459 planes at Flag=1 and one at Flag=0, against
the source's 2,790 at Flag=0.

⭐⭐ SO WHAT HALVED THE ERROR IS THE **ORDER**. The original build applied cut planes before the
facets, holes and poly-cuts existed and left that plate at y = 569.998; killing the cut and
re-applying it to the fully-modified body gives 546.224, every time. The same plane, the same
parameters, a different body underneath ⇒ a different result. Modification order is part of the
geometry, not bookkeeping.

⚠️ And the rest of the gap is not the cuts at all: the UNCUT body is already 617.545 where the
source's finished plate is 522.451, so the source removes 95 mm and its single cut plane removes
71.3. Something in the facet/contour family owns the remainder. Measured, not guessed at.
"""
import io
import json
import math
import os
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import eb_api

HERE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROJ = os.path.join(HERE, "projects", "bridge-bernie")
RB = os.path.join(PROJ, "bridge model for amir - REBUILD-3.dwg")
W = 1200


def load(n):
    return json.load(io.open(os.path.join(PROJ, n), encoding="utf-8"))


def main():
    limit = int(sys.argv[1]) if len(sys.argv) > 1 else 0
    eb_api.use(os.path.basename(RB), task="cut flags", project="bridge-bernie")
    eb_api.build_target(RB)
    eb_api.run("props", wait=W, handle="183")          # pin the document before COM
    app, doc = eb_api._app_doc()

    def bbox(h):
        # "Call was rejected by callee" comes back whenever AutoCAD is mid-regen after a
        # cut. Retry rather than lose the part.
        last = None
        for i in range(8):
            try:
                o = doc.HandleToObject(h)
                lo, hi = o.GetBoundingBox()
                return [hi[i] - lo[i] for i in range(3)]
            except Exception as e:
                last = e
                time.sleep(0.3 + 0.3 * i)
        raise last

    src = load("source.json")
    mp = load("source-map.json")
    cuts = src["cuts"]
    want = {}
    for p in src["plates"]:
        try:
            want[p["h"]] = [float(x) for x in p["dims"].split(",")]
        except Exception:
            pass
    for sh in src["shapes"]:
        try:
            lo, hi = sh["bbox"].split(";")
            lo = [float(x) for x in lo.split(",")]
            hi = [float(x) for x in hi.split(",")]
            want[sh["h"]] = [hi[i] - lo[i] for i in range(3)]
        except Exception:
            pass

    # the parts worth touching: they have cut planes AND their body is off by >= 1 mm
    todo = []
    for s, cs in cuts.items():
        t = mp.get(s)
        if not t or s not in want or not cs:
            continue
        try:
            d = max(abs(bbox(t)[i] - want[s][i]) for i in range(3))
        except Exception:
            continue
        if d >= 1.0:
            todo.append((d, s, t))
    todo.sort(reverse=True)
    print("parts with cut planes and a body off by >=1 mm: %d" % len(todo))
    if limit:
        todo = todo[:limit]
        print("trial on the worst %d" % len(todo))

    t0 = time.time()
    better = same = worse = 0
    gains = []
    for (d0, s, t) in todo:
        cs = cuts[s]
        best = (d0, None)
        for flag in (0, 1):
            r = eb_api.run("killcut", wait=W, handle=t, all=1)
            if not r.startswith("EB_OK"):
                break
            items = [("planecut", dict(handle=t,
                                       at="%.4f,%.4f,%.4f" % tuple(c["ip"]),
                                       normal="%.8f,%.8f,%.8f" % tuple(c["n"]),
                                       flag=flag))
                     for c in cs if isinstance(c.get("n"), list)]
            eb_api.batch(items, wait=W)
            try:
                d = max(abs(bbox(t)[i] - want[s][i]) for i in range(3))
            except Exception:
                continue
            if d < best[0] - 1e-6:
                best = (d, flag)
        # leave the part in its best state
        if best[1] is None:
            same += 1
            continue
        eb_api.run("killcut", wait=W, handle=t, all=1)
        eb_api.batch([("planecut", dict(handle=t,
                                        at="%.4f,%.4f,%.4f" % tuple(c["ip"]),
                                        normal="%.8f,%.8f,%.8f" % tuple(c["n"]),
                                        flag=best[1]))
                      for c in cs if isinstance(c.get("n"), list)], wait=W)
        better += 1
        gains.append((d0, best[0], best[1], s, t))
    print("improved %d, unchanged %d, %.0fs" % (better, same, time.time() - t0))
    if gains:
        print("worst-error before -> after (flag kept):")
        for a, b, f, s, t in sorted(gains, reverse=True)[:14]:
            print("   %8.2f -> %8.2f mm  flag=%d  %s->%s" % (a, b, f, s, t))
        tot0 = sum(g[0] for g in gains)
        tot1 = sum(g[1] for g in gains)
        print("summed worst-axis error over the improved parts: %.1f -> %.1f mm" % (tot0, tot1))
    print(eb_api.run("save", wait=W)[:80])


if __name__ == "__main__":
    main()
