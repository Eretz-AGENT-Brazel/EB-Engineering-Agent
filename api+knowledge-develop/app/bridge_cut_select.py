# -*- coding: utf-8 -*-
r"""Choose which cut planes to apply BY MEASURING, because their stored state is not readable.

⭐ THE FINDING THIS EXISTS FOR. Plate 3E5DE9 carries five cut planes in Bernie's model. Applied
all five, the rebuilt plate is 194.98 mm deep; the source is 320. **Skipping plane 2 alone gives
320.0** and the whole body then matches the source to 0.079 mm. So that plane is recorded on the
part and removes no steel there -- and nothing in what the API exposes says so:

    plane 0  n=(0.378, 0.920, 0.102)   f=0
    plane 1  n=(-0.376,-0.916,-0.141)  f=0
    plane 2  n=(0.000, 0.000, 1.000)   f=0   <- applying this one costs 125.02 mm
    plane 3  n=(-0.376,-0.916, 0.141)  f=0
    plane 4  n=(0.374, 0.911,-0.175)   f=0

All five carry the same Flag, so the flag does not distinguish it (and PsCutObjects overwrites
Flag anyway -- see qc/retracted.tsv). `PsCutPlane` and `Ks_ComCutPlane` both expose SetOffset
with NO getter, so a plane's full state cannot be read back at all.

⇒ Stop trying to infer it. Apply each plane, measure the body against the source's own
dimensions, and KEEP THE PLANE ONLY IF IT HELPED. The reference is the source's `dims` for a
plate and its `bbox` span for a shape; the rebuild is measured through COM. The two agree to
better than 0.1 mm on parts that are already exact, which is fine for a 1 mm decision.

Only parts already off by >= the threshold are touched, so a part that is exact today cannot be
disturbed.
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
    thresh = float(sys.argv[1]) if len(sys.argv) > 1 else 1.0
    limit = int(sys.argv[2]) if len(sys.argv) > 2 else 0
    eb_api.use(os.path.basename(RB), task="cut selection", project="bridge-bernie")
    eb_api.build_target(RB)
    eb_api.run("props", wait=W, handle="183")
    app, doc = eb_api._app_doc()

    def bbox(h):
        last = None
        for i in range(10):
            try:
                lo, hi = doc.HandleToObject(h).GetBoundingBox()
                return [hi[k] - lo[k] for k in range(3)]
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

    todo = []
    for s, cs in cuts.items():
        t = mp.get(s)
        if not t or s not in want or not cs:
            continue
        try:
            e = max(abs(bbox(t)[i] - want[s][i]) for i in range(3))
        except Exception:
            continue
        if e >= thresh:
            todo.append((e, s, t))
    todo.sort(reverse=True)
    if limit:
        todo = todo[:limit]
    print("parts with cut planes and a body off by >=%.1f mm: %d" % (thresh, len(todo)))

    t0 = time.time()
    gains, worse, same = [], 0, 0
    for (e0, s, t) in todo:
        cs = [c for c in cuts[s] if isinstance(c.get("n"), list)]
        if not cs:
            continue

        def err():
            return max(abs(bbox(t)[i] - want[s][i]) for i in range(3))

        def apply(subset):
            eb_api.run("killcut", wait=W, handle=t, all=1)
            for c in subset:
                eb_api.run("planecut", wait=W, handle=t,
                           at="%.4f,%.4f,%.4f" % tuple(c["ip"]),
                           normal="%.8f,%.8f,%.8f" % tuple(c["n"]))
            return err()

        # ⭐ EXHAUSTIVE, NOT GREEDY. Greedy in the source's order locks in a bad plane: the
        # plane that over-cuts looks like an improvement against the UNCUT body, and every
        # plane after it then measures neutral. With <= 4 planes every subset is affordable
        # (16 trials), and the subsets are tried largest-first so a tie keeps more of the
        # source's record rather than less.
        best = (None, None)
        if len(cs) <= 4:
            idx = range(len(cs))
            subsets = []
            for mask in range(1 << len(cs)):
                subsets.append([i for i in idx if mask & (1 << i)])
            subsets.sort(key=lambda x: -len(x))
            for sub in subsets:
                e = apply([cs[i] for i in sub])
                if best[0] is None or e < best[0] - 1e-6:
                    best = (e, sub)
                if best[0] is not None and best[0] < 0.01:
                    break
        else:
            # too many to enumerate: greedy, but drop-if-worse in both directions
            keep = []
            cur = apply([])
            for i, c in enumerate(cs):
                e = apply([cs[k] for k in keep + [i]])
                if e <= cur + 0.01:
                    keep.append(i)
                    cur = e
            best = (cur, keep)
        if best[1] is not None:
            apply([cs[i] for i in best[1]])
        cur = best[0] if best[0] is not None else e0
        if cur < e0 - 0.01:
            gains.append((e0, cur, len(best[1]), len(cs), s, t))
        elif cur > e0 + 0.01:
            worse += 1
        else:
            same += 1
    print("improved %d, unchanged %d, worse %d, %.0fs"
          % (len(gains), same, worse, time.time() - t0))
    for a, b, k, n, s, t in sorted(gains, reverse=True)[:16]:
        print("   %9.2f -> %8.3f mm   kept %d of %d planes   %s->%s" % (a, b, k, n, s, t))
    if gains:
        print("summed worst-axis error over the improved parts: %.1f -> %.1f mm"
              % (sum(g[0] for g in gains), sum(g[1] for g in gains)))
    print(eb_api.run("save", wait=W)[:70])


if __name__ == "__main__":
    main()
