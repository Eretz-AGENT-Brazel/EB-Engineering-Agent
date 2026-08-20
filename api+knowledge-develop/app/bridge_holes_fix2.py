# -*- coding: utf-8 -*-
"""bridge_holes_fix2.py -- choose the flange PER HOLE, not per part.

    python app/bridge_holes_fix2.py

⛔ WHY THE FIRST FIX WAS NOT ENOUGH, and it is the more interesting finding.

`bridge_holes_fix.py` did what night_build.py taught: for each part, try `flange=1/0/2/unset`
and keep the variant that lands every hole. The measurement came back:

    variant flange=1 -> 709 parts IMPROVED, 0 parts COMPLETE
    variant flange=0 -> 0 improved      variant 2 -> 0      unset -> 0

**Improved but never complete, on every single part.** That is not a wrong choice of variant --
it says no single choice exists: **those parts carry holes in BOTH walls.** A U220 with holes
through the web and through a flange cannot be satisfied by one selector, and one-selector-
per-part is the shape of the old fix, not of the problem.

> ⭐⭐⭐ **THE FLANGE IS A PROPERTY OF THE HOLE, NOT OF THE PART.** night_build's per-part
> search was right for models 1-6 because those parts were simple enough that all of a part's
> holes shared a wall. Inheriting a correction is right; inheriting its SCOPE without checking
> is how a fix stops working on a bigger model.

So: measure which wanted holes land under each variant, then drill every hole with the variant
that landed IT.

⚠️ Slots are measured with their own tolerance. A slot drilled at its centre with
`slot=<travel>` reads back (default `lhm=2`) as its TWO end circles, at +/- travel/2 -- so for
a slotted hole "landed" means a hole within travel/2 + 0.05 mm, and for a round hole 0.01 mm.
Using one tolerance for both would call 608 correct slots failures.
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
VARIANTS = (None, 1, 0, 2)
ROUND_TOL = 0.01


def mid_of(h):
    a, b = h["a"], h["b"]
    return [(a[i] + b[i]) / 2.0 for i in range(3)]


def tol_of(h):
    s = float(h.get("slot") or 0)
    return ROUND_TOL if s <= 0 else s / 2.0 + 0.05


def drill_kw(t, h, variant):
    a, b = h["a"], h["b"]
    v = [b[i] - a[i] for i in range(3)]
    L = sum(x * x for x in v) ** 0.5
    if L <= 0:
        return None
    kw = dict(handle=t, at="%.4f,%.4f,%.4f" % tuple(a),
              n="%.6f,%.6f,%.6f" % tuple(x / L for x in v), dia=h["d"], play=0)
    if h.get("slot"):
        kw["slot"] = h["slot"]
    if variant is not None:
        kw["flange"] = variant
    return kw


def read_holes():
    eb_api.run("dumpholes", wait=1800)
    out = {}
    cur = None
    for line in io.open(os.path.join(eb_api.channel(), "eb_holes_all.txt"),
                        encoding="utf-8-sig"):
        f = line.rstrip("\n").split("\t")
        if f[0] == "OBJ":
            cur = f[1]
        elif f[0] == "HOLE" and len(f) > 7:
            a, b = [float(x) for x in f[5].split(",")], [float(x) for x in f[6].split(",")]
            out.setdefault(cur, []).append([(a[i] + b[i]) / 2.0 for i in range(3)])
    return out


def wipe_and_drill(parts, want, variant):
    """One measurement pass: every part's fields cleared, every hole drilled one way."""
    items = []
    kills = eb_api.batch([("mods", {"handle": t}) for t in parts], wait=3600)
    nf = {}
    for (i, op, res) in kills:
        if 0 <= i < len(parts):
            m = re.search(r"holeFields=(\d+)", res)
            nf[parts[i]] = int(m.group(1)) if m else 0
    for t in parts:
        for k in range(nf.get(t, 0) - 1, -1, -1):
            items.append(("killholefield", {"handle": t, "field": k}))
    for own, t in parts.items() if isinstance(parts, dict) else []:
        pass
    return items, nf


def main():
    want_all = json.load(io.open(RESOLVED, encoding="utf-8"))
    mp = json.load(io.open(os.path.splitext(CACHE)[0] + "-map.json", encoding="utf-8"))
    eb_api.use(os.path.basename(REBUILD), task="per-hole flange assignment",
               project="bridge-bernie")
    eb_api.build_target(REBUILD)
    eb_api.run("list", wait=1800)
    act = eb_api._active_doc_name() or ""
    if "REBUILD" not in act:
        raise RuntimeError("active document is %r, not the rebuild" % act)
    print("in: " + act)

    pairs = [(own, mp[own]) for own in want_all if own in mp]
    have = read_holes()

    def landed_mask(pairs, have):
        """[(own, index)] -> True/False for every wanted hole."""
        out = {}
        for own, t in pairs:
            got = have.get(t, [])
            for i, h in enumerate(want_all[own]):
                m = mid_of(h)
                d = min([math.dist(m, g) for g in got]) if got else 9e9
                out[(own, i)] = d <= tol_of(h)
        return out

    base = landed_mask(pairs, have)
    tot = len(base)
    print("wanted holes: %d, landing now: %d" % (tot, sum(1 for v in base.values() if v)))

    # which parts still have a hole that does not land
    bad = sorted({own for (own, i), okv in base.items() if not okv})
    print("parts with at least one unlanded hole: %d" % len(bad))
    if not bad:
        return

    # ---- measure every variant, and remember WHICH HOLE each one satisfied -------------
    winner = {}
    for variant in VARIANTS:
        items = []
        nf = {}
        rows = eb_api.batch([("mods", {"handle": mp[own]}) for own in bad], wait=3600)
        for (i, op, res) in rows:
            if 0 <= i < len(bad):
                m = re.search(r"holeFields=(\d+)", res)
                nf[bad[i]] = int(m.group(1)) if m else 0
        for own in bad:
            t = mp[own]
            for k in range(nf.get(own, 0) - 1, -1, -1):
                items.append(("killholefield", {"handle": t, "field": k}))
            for h in want_all[own]:
                kw = drill_kw(t, h, variant)
                if kw:
                    items.append(("drill", kw))
        t0 = time.time()
        for a in range(0, len(items), 3000):
            eb_api.batch(items[a:a + 3000], wait=3600)
        have = read_holes()
        mask = landed_mask([(own, mp[own]) for own in bad], have)
        gained = 0
        for k, okv in mask.items():
            if okv and k not in winner:
                winner[k] = variant
                gained += 1
        print("   flange=%-4s %d ops, %.0fs -> %d more holes satisfied (total assigned %d)"
              % (variant, len(items), time.time() - t0, gained, len(winner)))

    # ---- apply: each hole with the variant that satisfied it ---------------------------
    items = []
    nf = {}
    rows = eb_api.batch([("mods", {"handle": mp[own]}) for own in bad], wait=3600)
    for (i, op, res) in rows:
        if 0 <= i < len(bad):
            m = re.search(r"holeFields=(\d+)", res)
            nf[bad[i]] = int(m.group(1)) if m else 0
    unassigned = 0
    for own in bad:
        t = mp[own]
        for k in range(nf.get(own, 0) - 1, -1, -1):
            items.append(("killholefield", {"handle": t, "field": k}))
        for i, h in enumerate(want_all[own]):
            v = winner.get((own, i))
            if v is None and (own, i) not in winner:
                unassigned += 1
                v = 1                     # nothing satisfied it; keep the best-known default
            kw = drill_kw(t, h, v)
            if kw:
                items.append(("drill", kw))
    print("applying per-hole flange: %d ops (%d holes nothing satisfied)"
          % (len(items), unassigned))
    t0 = time.time()
    for a in range(0, len(items), 3000):
        eb_api.batch(items[a:a + 3000], wait=3600)
    print("   %.0fs" % (time.time() - t0))
    print(eb_api.run("save", wait=3600)[:110])

    have = read_holes()
    final = landed_mask(pairs, have)
    okn = sum(1 for v in final.values() if v)
    print("FINAL: %d of %d wanted holes land (was %d)"
          % (okn, tot, sum(1 for v in base.values() if v)))
    still = sorted({own for (own, i), okv in final.items() if not okv})
    print("parts still imperfect: %d" % len(still))


if __name__ == "__main__":
    main()
