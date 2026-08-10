# -*- coding: utf-8 -*-
"""
fix_clone_residual.py — repair the few objects that ProSteel's reactors
recomputed after the deep-clone.

Measured residual (שיעור 2, clone dx=+15000): 847/855 objects are identical.
The 8 that differ are all reactor side-effects:
  * two collinear RHS150X100X4 merged into one continuous member, dragging its
    base-plate assembly (2 plates + 4 bolts) down to the old joint level
  * one EA80X80X8 diagonal was re-trimmed from 2609mm to ~60mm
  * two plates were re-fitted 20mm shorter (80 -> 60)

Strategy: delete the recomputed copies and rebuild those objects at the exact
original geometry, then verify by read-back. Shapes use the parametric path
(profile+catalog+endpoints); plates use the axis-preserving matrix (proven).
"""
import os
import sys
import time

APP = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, APP)
import eb_api                     # noqa: E402

DUMP = os.path.join(APP, "plugin", "eb_full.txt")
SPLIT = 15000.0
DX = 15000.0


def xyz(s):
    try:
        return tuple(float(x) for x in s.split(","))
    except Exception:
        return None


def load():
    sh, pl, bo = [], [], []
    for line in open(DUMP, encoding="utf-8").read().splitlines():
        f = line.split("\t")
        if not f or not f[0]:
            continue
        if f[0] == "SHAPE" and len(f) >= 19:
            sh.append({"h": f[1], "prof": f[2], "cat": f[3], "p1": xyz(f[4]), "p2": xyz(f[5]),
                       "L": float(f[6] or 0), "rot": float(f[9] or 0), "lay": f[15]})
        elif f[0] == "PLATE" and len(f) >= 7:
            pl.append({"h": f[1], "c": xyz(f[2]), "d": xyz(f[3]), "lay": f[5]})
        elif f[0] == "BOLT" and len(f) >= 7:
            bo.append({"h": f[1], "c": xyz(f[2]), "d": xyz(f[3]), "lay": f[5]})
    return sh, pl, bo


def key_sh(e, off=0.0):
    a = (round(e["p1"][0] - off, 1), round(e["p1"][1], 1), round(e["p1"][2], 1))
    b = (round(e["p2"][0] - off, 1), round(e["p2"][1], 1), round(e["p2"][2], 1))
    return (e["prof"],) + tuple(sorted([a, b]))


def key_g(e, off=0.0):
    return ((round(e["c"][0] - off, 1), round(e["c"][1], 1), round(e["c"][2], 1))
            + tuple(round(v, 1) for v in e["d"]))


def diff():
    """Return (missing_originals, extra_copies) per class, keyed exactly."""
    sh, pl, bo = load()
    out = {}
    for nm, arr, kf, pk in (("shape", sh, key_sh, "p1"), ("plate", pl, key_g, "c"), ("bolt", bo, key_g, "c")):
        o = [e for e in arr if e[pk] and e[pk][0] < SPLIT]
        c = [e for e in arr if e[pk] and e[pk][0] >= SPLIT]
        ck = {}
        for e in c:
            ck.setdefault(kf(e, DX), []).append(e)
        missing = []
        for e in o:
            k = kf(e)
            if ck.get(k):
                ck[k].pop()
            else:
                missing.append(e)
        extra = [e for lst in ck.values() for e in lst]
        out[nm] = (missing, extra)
    return out


def plate_axes(d):
    """Axis-preserving params: thickness axis = smallest dim; keep L/W on their
    real global axes (this is what fixed the 96 rotated plates)."""
    dd = list(d)
    ti = dd.index(min(dd))
    rest = [i for i in range(3) if i != ti]
    i1, i2 = rest[0], rest[1]
    unit = lambda i: tuple(1.0 if k == i else 0.0 for k in range(3))
    return dd[i1], dd[i2], dd[ti], unit(i1), unit(i2), unit(ti)


def main(apply=False):
    d = diff()
    print("=== residual diff ===")
    for nm in ("shape", "plate", "bolt"):
        m, x = d[nm]
        print("%-6s missing=%d extra=%d" % (nm, len(m), len(x)))
    if not apply:
        print("\n(dry run — pass 'apply' to repair)")
        return

    # 1) delete the recomputed copies
    dels = 0
    for nm in ("shape", "plate", "bolt"):
        for e in d[nm][1]:
            r = eb_api.delete(e["h"])
            if isinstance(r, str) and r.startswith("EB_OK"):
                dels += 1
            time.sleep(0.15)
    print("deleted %d recomputed copies" % dels)
    time.sleep(1.0)

    # 2) rebuild the missing originals at +DX
    made = fail = 0
    for e in d["shape"][0]:                # [0] = originals missing from the copy
        p1 = (e["p1"][0] + DX, e["p1"][1], e["p1"][2])
        p2 = (e["p2"][0] + DX, e["p2"][1], e["p2"][2])
        r = None
        for _ in range(3):
            r = eb_api.run("beam", name=e["prof"],
                           catalog=(e["cat"].split(".")[-1] if e["cat"] else ""),
                           p1=eb_api._pt(p1), p2=eb_api._pt(p2), rot=e["rot"],
                           layer=e["lay"], wait=20, _log=False)
            if isinstance(r, str) and r.startswith("EB_OK"):
                break
            time.sleep(1.5)
        good = isinstance(r, str) and r.startswith("EB_OK")
        made += good
        fail += (not good)
        print("  shape %s L=%.0f -> %s" % (e["prof"], e["L"], "OK" if good else str(r)[:60]))
        time.sleep(0.3)

    for e in d["plate"][0]:
        L, W, T, ex, ey, ez = plate_axes(e["d"])
        c = (e["c"][0] + DX, e["c"][1], e["c"][2])
        r = None
        for _ in range(3):
            r = eb_api.run("plate", center=eb_api._pt(c), l=L, w=W, t=T,
                           ex="%g,%g,%g" % ex, ey="%g,%g,%g" % ey, ez="%g,%g,%g" % ez,
                           layer=e["lay"], wait=20, _log=False)
            if isinstance(r, str) and r.startswith("EB_OK"):
                break
            time.sleep(1.5)
        good = isinstance(r, str) and r.startswith("EB_OK")
        made += good
        fail += (not good)
        print("  plate %gx%gx%g -> %s" % (L, W, T, "OK" if good else str(r)[:60]))
        time.sleep(0.3)

    for e in d["bolt"][0]:
        dd = list(e["d"])
        ai = dd.index(max(dd))
        Ln = dd[ai]
        half = Ln / 2.0
        axis = [0.0, 0.0, 0.0]
        axis[ai] = 1.0
        c = (e["c"][0] + DX, e["c"][1], e["c"][2])
        p1 = tuple(c[k] - axis[k] * half for k in range(3))
        p2 = tuple(c[k] + axis[k] * half for k in range(3))
        r = None
        for _ in range(3):
            r = eb_api.run("bolt", p1=eb_api._pt(p1), p2=eb_api._pt(p2), dia=16,
                           len=Ln, layer=e["lay"], wait=20, _log=False)
            if isinstance(r, str) and r.startswith("EB_OK"):
                break
            time.sleep(1.5)
        good = isinstance(r, str) and r.startswith("EB_OK")
        made += good
        fail += (not good)
        print("  bolt L=%g -> %s" % (Ln, "OK" if good else str(r)[:60]))
        time.sleep(0.3)

    print("\nrebuilt %d, failed %d" % (made, fail))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main(apply=(len(sys.argv) > 1 and sys.argv[1] == "apply"))
