# -*- coding: utf-8 -*-
r"""Rebuild the shapes whose SECTION FRAME came out wrong, choosing the frame by measurement.

THE FINDING. bridge_build.shapes reads the section frame out of the property bag and passes
props X -> ax, props Y -> ay (with rot=0, because the axes carry the rotation). That is right
for 6,064 of the bridge's 6,240 shapes and wrong for the rest, where the two axes are the other
way round. Measured on SHS80X80X3.6 handle 17D879, whose source body spans
[1690.160, 851.118, 1929.898]:

    ax=X ay=Y rot=0          -> [1656.109, 851.133, 1929.913]   err 34.051   <- what the build did
    rot=-71.132 only         -> [1664.300, 830.489, 1944.714]   err 25.860
    ax=Y ay=X rot=0          -> [1690.154, 851.102, 1929.897]   err  0.016   <- correct
    rot only, ends reversed  -> [1691.511, 861.311, 1944.714]   err 14.816

An 80x80 tube turned to the wrong angle changes its own bounding box by up to
80*sqrt(2) - 80 = 33.1 mm, which is the whole of that 34 mm error. 1,229 shapes read back with
the rotation sign flipped; only those whose section is not symmetric about that flip show it as
geometry, which is why the body gate sees 176 and not 1,229.

There is no rule to infer, so BOTH mappings are built and the closer one is kept. Then the
part's modifications go back on: cut planes chosen the same way (every subset tried, closest
kept -- see bridge_cut_select.py), poly-cuts with their recorded frame, holes with the
exit-wall recipe.
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


def save_map(m):
    json.dump(m, io.open(os.path.join(PROJ, "source-map.json"), "w", encoding="utf-8"))


def main():
    thresh = float(sys.argv[1]) if len(sys.argv) > 1 else 1.0
    limit = int(sys.argv[2]) if len(sys.argv) > 2 else 0
    eb_api.use(os.path.basename(RB), task="reframe shapes", project="bridge-bernie")
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
    pcs = load("polycuts.json")
    holes = load("holes_resolved.json")
    cuts = src["cuts"]
    byh = dict((s["h"], s) for s in src["shapes"])
    want = {}
    for s in src["shapes"]:
        try:
            lo, hi = s["bbox"].split(";")
            lo = [float(x) for x in lo.split(",")]
            hi = [float(x) for x in hi.split(",")]
            want[s["h"]] = [hi[i] - lo[i] for i in range(3)]
        except Exception:
            pass

    todo = []
    for h, w in want.items():
        t = mp.get(h)
        if not t:
            continue
        try:
            e = max(abs(bbox(t)[i] - w[i]) for i in range(3))
        except Exception:
            continue
        if e >= thresh:
            todo.append((e, h, t))
    todo.sort(reverse=True)
    if limit:
        todo = todo[:limit]
    print("shapes with a body off by >=%.1f mm: %d" % (thresh, len(todo)))

    def axis(pr, k):
        v = (pr.get(k) or "").replace("/", ",")
        return v if v.count(",") == 2 else None

    def build(s, swap):
        pr = s.get("props", {})
        a, b = axis(pr, "X"), axis(pr, "Y")
        kw = dict(kind="standard", name=s["sec"], catalog=s["cat"],
                  p1=s["p1"], p2=s["p2"], mirror=s["mir"])
        if s.get("layer"):
            kw["layer"] = s["layer"]
        if a and b:
            if swap:
                kw["ax"], kw["ay"], kw["rot"] = b, a, 0
            else:
                kw["ax"], kw["ay"], kw["rot"] = a, b, 0
        else:
            kw["rot"] = s["rot"]
        r = eb_api.run("beam", wait=W, **kw)
        i = r.find("handle=")
        if i < 0 or not r.startswith("EB_OK"):
            return None
        return r[i + 7:].split()[0]

    t0 = time.time()
    fixed, kept_same, failed = [], 0, 0
    for (e0, h, old) in todo:
        s = byh[h]
        w = want[h]

        def err(x):
            return max(abs(bbox(x)[i] - w[i]) for i in range(3))

        alt = build(s, True)
        if alt is None:
            failed += 1
            continue
        ealt = err(alt)
        if ealt >= e0 - 0.01:
            eb_api.run("erase", wait=W, handles=alt)
            kept_same += 1
            continue

        eb_api.run("erase", wait=W, handles=old)
        mp[h] = alt

        cs = [c for c in cuts.get(h, []) if isinstance(c.get("n"), list)]
        if cs:
            def apply_cuts(sub):
                eb_api.run("killcut", wait=W, handle=alt, all=1)
                for c in sub:
                    eb_api.run("planecut", wait=W, handle=alt,
                               at="%.4f,%.4f,%.4f" % tuple(c["ip"]),
                               normal="%.8f,%.8f,%.8f" % tuple(c["n"]))
                return err(alt)

            best_e, best_sub = None, None
            if len(cs) <= 4:
                subs = []
                for mask in range(1 << len(cs)):
                    subs.append([i for i in range(len(cs)) if mask & (1 << i)])
                subs.sort(key=lambda x: -len(x))
                for sub in subs:
                    e = apply_cuts([cs[i] for i in sub])
                    if best_e is None or e < best_e - 1e-6:
                        best_e, best_sub = e, sub
                    if best_e < 0.05:
                        break
            else:
                keep = []
                cur = apply_cuts([])
                for i in range(len(cs)):
                    e = apply_cuts([cs[k] for k in keep + [i]])
                    if e <= cur + 0.01:
                        keep.append(i)
                        cur = e
                best_e, best_sub = cur, keep
            apply_cuts([cs[i] for i in best_sub])

        for c in pcs.get(h, []):
            try:
                d = math.sqrt(sum((c["end"][k] - c["ins"][k]) ** 2 for k in range(3)))
                pts = ";".join(",".join("%.5f" % q for q in v) for v in c["verts"])
                eb_api.run("polycut", wait=W, handle=alt, shape="pts", pts=pts,
                           at="%.4f,%.4f,%.4f" % tuple(c["ins"]),
                           xaxis="%.8f,%.8f,%.8f" % tuple(c["xaxis"]),
                           yaxis="%.8f,%.8f,%.8f" % tuple(c["yaxis"]),
                           depth="%.4f" % d)
            except Exception:
                pass

        hl = holes.get(h) or []
        if hl:
            lo, hi = s["bbox"].split(";")
            lo = [float(x) for x in lo.split(",")]
            hi = [float(x) for x in hi.split(",")]
            cen = [(lo[i] + hi[i]) / 2.0 for i in range(3)]
            items = []
            for hh in hl:
                a, b = hh["a"], hh["b"]
                da = sum((a[i] - cen[i]) ** 2 for i in range(3))
                db = sum((b[i] - cen[i]) ** 2 for i in range(3))
                if da >= db:
                    outer, inner = a, b
                else:
                    outer, inner = b, a
                v = [outer[i] - inner[i] for i in range(3)]
                L = math.sqrt(sum(x * x for x in v))
                if L <= 0:
                    continue
                kw = dict(handle=alt, dia=hh["d"], play=0,
                          at="%.4f,%.4f,%.4f" % tuple((a[i] + b[i]) / 2.0 for i in range(3)),
                          n="%.6f,%.6f,%.6f" % tuple(x / L for x in v),
                          depth="%.4f" % L)
                if hh.get("slot"):
                    kw["slot"] = hh["slot"]
                items.append(("drill", kw))
            if items:
                eb_api.batch(items, wait=W)

        fixed.append((e0, err(alt), h, old, alt))
        if len(fixed) % 20 == 0:
            save_map(mp)
            print("   %d fixed, %.0fs" % (len(fixed), time.time() - t0))

    save_map(mp)
    print("reframed %d, left alone %d, build failed %d, %.0fs"
          % (len(fixed), kept_same, failed, time.time() - t0))
    for a, b, h, o, n in sorted(fixed, reverse=True)[:14]:
        print("   %9.2f -> %8.3f mm   %s   %s -> %s" % (a, b, h, o, n))
    if fixed:
        print("summed worst-axis error: %.1f -> %.1f mm"
              % (sum(f[0] for f in fixed), sum(f[1] for f in fixed)))
    print(eb_api.run("save", wait=W)[:70])


if __name__ == "__main__":
    main()
