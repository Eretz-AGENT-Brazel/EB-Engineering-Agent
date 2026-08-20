# -*- coding: utf-8 -*-
"""bridge_holes_fix.py -- re-drill the parts whose holes went through the wrong wall.

    python app/bridge_holes_fix.py [--dry]

⛔ WHAT THE GATES FOUND, and it was a rule this project had already paid for once.

1,098 holes on 112 parts sit more than 20 mm from where the source has them -- and the
signature is unmistakable. On `135D5A` (HE300A) every hole matches in x and y and sits
**276 mm** away in z: the section is 290 mm deep with a 14 mm flange, so the rebuild drilled
the **opposite flange**. The affected sections are exactly the ones with two walls on the
drill ray: U220 (334 holes), SHS80X80X3.6 (196), HE200B (184), SHS150X150X5 (112),
HE300A (84), IPE160 (84), HE400B, SHS200X200X8.

> ⭐⭐ **THE FLANGE SELECTOR IS A CHOICE BETWEEN WALLS, AND THE POINT DOES NOT DECIDE IT.**
> `drill` resolves the position from the host's geometry, so the same `at=` lands on either
> flange depending on `flange=`. night_build.py carried a per-part retry for exactly this
> (measured on model 5: the leftovers were one exact number per section -- 54.0 mm on an
> EA60X60X6, i.e. leg 60 minus thickness 6). **I dropped that retry when I rewrote the segment
> to carry slotted holes, and the model paid for it 1,098 times.**
> ⇒ a segment rewritten for a new capability must inherit the corrections the old one earned.

The search is destructive -- each attempt wipes the part's hole fields -- so the winner is
re-applied at the end rather than assumed to still be in place (the 5d rule).
"""
import io
import json
import math
import os
import re
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
DEV = os.path.dirname(HERE)
if HERE not in sys.path:
    sys.path.insert(0, HERE)
import eb_api  # noqa: E402

PROJ = os.path.join(DEV, "projects", "bridge-bernie")
CACHE = os.path.join(PROJ, "source.json")
RESOLVED = os.path.join(PROJ, "holes_resolved.json")
REBUILD = os.path.join(PROJ, "bridge model for amir - REBUILD.dwg")
TOL = 0.01
VARIANTS = (1, 0, 2, None)


def num(s):
    return [float(v) for v in str(s).split(",")]


def read_holes():
    """{part handle: [(mid, depth, dia)]} straight from the drawing."""
    eb_api.run("dumpholes", wait=1800)
    out = {}
    cur = None
    for line in io.open(os.path.join(eb_api.channel(), "eb_holes_all.txt"),
                        encoding="utf-8-sig"):
        f = line.rstrip("\n").split("\t")
        if f[0] == "OBJ":
            cur = f[1]
        elif f[0] == "HOLE" and len(f) > 7:
            a, b = num(f[5]), num(f[6])
            out.setdefault(cur, []).append(
                ([(a[i] + b[i]) / 2 for i in range(3)], math.dist(a, b), float(f[7])))
    return out


def landed(want, have):
    """How many wanted holes have a hole within TOL of them."""
    n = 0
    for h in want:
        mid = h["a"] if isinstance(h, dict) else h[0]
        if isinstance(h, dict):
            a, b = h["a"], h["b"]
            mid = [(a[i] + b[i]) / 2 for i in range(3)]
        if have and min(math.dist(mid, c[0]) for c in have) <= TOL:
            n += 1
    return n


def main(dry=False):
    src = json.load(io.open(CACHE, encoding="utf-8"))
    want_all = json.load(io.open(RESOLVED, encoding="utf-8"))
    mp = json.load(io.open(os.path.splitext(CACHE)[0] + "-map.json", encoding="utf-8"))
    eb_api.use(os.path.basename(REBUILD), task="re-drill the wrong-flange parts",
               project="bridge-bernie")
    eb_api.build_target(REBUILD)
    eb_api.run("list", wait=900)
    act = eb_api._active_doc_name() or ""
    if "REBUILD" not in act:
        raise RuntimeError("active document is %r, not the rebuild" % act)
    print("in: " + act)

    have = read_holes()
    # which parts are wrong, measured now
    bad = []
    for own, lst in want_all.items():
        t = mp.get(own)
        if not t:
            continue
        got = have.get(t, [])
        ok = landed(lst, got)
        if ok < len(lst):
            bad.append((own, t, len(lst), ok))
    print("parts whose holes do not all land: %d (holes wanted %d, landed %d)"
          % (len(bad), sum(x[2] for x in bad), sum(x[3] for x in bad)))
    for own, t, n, ok in bad[:6]:
        print("   %s -> %s  %d/%d" % (own, t, ok, n))
    if dry or not bad:
        return

    best = {}
    for variant in VARIANTS:
        items = []
        for own, t, n, ok in bad:
            if best.get(own, (-1, None))[0] == n:      # already perfect
                continue
            # wipe this part's fields, then drill every hole under this variant
            m = re.search(r"holeFields=(\d+)", eb_api.run("mods", wait=900, handle=t))
            for i in range(int(m.group(1)) - 1, -1, -1) if m else []:
                items.append(("killholefield", {"handle": t, "field": i}))
            for h in want_all[own]:
                a, b = h["a"], h["b"]
                v = [b[i] - a[i] for i in range(3)]
                L = sum(x * x for x in v) ** 0.5
                if L <= 0:
                    continue
                kw = dict(handle=t, at="%.4f,%.4f,%.4f" % tuple(a),
                          n="%.6f,%.6f,%.6f" % tuple(x / L for x in v),
                          dia=h["d"], play=0)
                if h.get("slot"):
                    kw["slot"] = h["slot"]
                if variant is not None:
                    kw["flange"] = variant
                items.append(("drill", kw))
        if not items:
            break
        print("variant flange=%s : %d ops" % (variant, len(items)))
        t0 = time.time()
        for a in range(0, len(items), 2000):
            eb_api.batch(items[a:a + 2000], wait=3600)
        have = read_holes()
        improved = 0
        for own, t, n, _ in bad:
            ok = landed(want_all[own], have.get(t, []))
            if ok > best.get(own, (-1, None))[0]:
                best[own] = (ok, variant)
                improved += 1
        done = len([1 for own, t, n, _ in bad if best.get(own, (0,))[0] == n])
        print("   %.0fs -> %d parts improved, %d parts now complete of %d"
              % (time.time() - t0, improved, done, len(bad)))
        eb_api.run("save", wait=3600)

    print("\nbest variant per part:")
    hist = {}
    for own, (ok, v) in best.items():
        hist[v] = hist.get(v, 0) + 1
    print("   " + str(hist))
    left = [(own, t, n, best.get(own, (0, None))[0]) for own, t, n, _ in bad
            if best.get(own, (0, None))[0] < n]
    print("parts still incomplete: %d" % len(left))
    for own, t, n, ok in left[:8]:
        print("   %s -> %s  %d/%d  (best flange=%s)" % (own, t, ok, n, best.get(own, (0, None))[1]))
    # ⚠️ the LAST variant tried is what is in the drawing now -- re-apply each part's winner
    fix = [(own, t, n, best[own][1]) for own, t, n, _ in bad
           if own in best and best[own][1] != VARIANTS[-1]]
    if fix:
        print("re-applying the winning variant on %d parts" % len(fix))
        items = []
        for own, t, n, variant in fix:
            m = re.search(r"holeFields=(\d+)", eb_api.run("mods", wait=900, handle=t))
            for i in range(int(m.group(1)) - 1, -1, -1) if m else []:
                items.append(("killholefield", {"handle": t, "field": i}))
            for h in want_all[own]:
                a, b = h["a"], h["b"]
                v = [b[i] - a[i] for i in range(3)]
                L = sum(x * x for x in v) ** 0.5
                if L <= 0:
                    continue
                kw = dict(handle=t, at="%.4f,%.4f,%.4f" % tuple(a),
                          n="%.6f,%.6f,%.6f" % tuple(x / L for x in v),
                          dia=h["d"], play=0)
                if h.get("slot"):
                    kw["slot"] = h["slot"]
                if variant is not None:
                    kw["flange"] = variant
                items.append(("drill", kw))
        for a in range(0, len(items), 2000):
            eb_api.batch(items[a:a + 2000], wait=3600)
        eb_api.run("save", wait=3600)
    have = read_holes()
    tot = ok = 0
    for own, lst in want_all.items():
        t = mp.get(own)
        if not t:
            continue
        tot += len(lst)
        ok += landed(lst, have.get(t, []))
    print("FINAL: %d of %d wanted holes land within %.2f mm" % (ok, tot, TOL))


if __name__ == "__main__":
    main("--dry" in sys.argv[1:])
