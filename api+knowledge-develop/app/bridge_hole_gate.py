# -*- coding: utf-8 -*-
"""The hole gate: how many of the source's holes does the rebuild reproduce, and how closely?

⚠️ TWO WAYS TO GET THIS WRONG, both of which I got wrong today (21/08/2026) before this file:

1. COMPARING THE TWO CONVENTIONS. `holes_resolved.json` is the lhm=1 list (11,657 rows, one
   row per slot); a `dumpholes lhm=2` is 12,265 rows (a slot is its two end circles). Matching
   one against the other reports ~600 phantom failures. It is the same mistake that destroyed
   2,133 holes on 20/08. ⇒ ALWAYS compare source lhm=2 against rebuild lhm=2.

2. ROUNDING A COORDINATE INTO A DICTIONARY KEY. round(x,1) puts 78642.45 and 78642.43 -- the
   same hole, 0.02 mm apart -- in different buckets, and the gate reported 889 plate holes as
   missing when every one of them was there. A geometric comparison needs a TOLERANCE, not a
   key. The endpoints also come back in either order, so the match must be order-insensitive.

Reports the whole distribution rather than one number, because "how many are exact" hides
whether the rest are 0.05 mm out or in the wrong wall.
"""
import collections
import io
import json
import math
import os
import sys

HERE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROJ = os.path.join(HERE, "projects", "bridge-bernie")
TOLS = (0.1, 0.5, 1.0, 5.0, 20.0)


def rows_from_dump(path):
    out = collections.defaultdict(list)
    for line in io.open(path, encoding="utf-8-sig"):
        p = line.rstrip("\n").split("\t")
        if p and p[0] == "HOLE" and len(p) > 7:
            out[p[1]].append(([float(x) for x in p[5].split(",")],
                              [float(x) for x in p[6].split(",")]))
    return out


def rows_from_json(path):
    d = json.load(io.open(path, encoding="utf-8"))
    out = collections.defaultdict(list)
    for h in d["holes"]:
        out[h["owner"]].append(([float(x) for x in h["a"].split(",")],
                                [float(x) for x in h["b"].split(",")]))
    return out


def pair_dist(w, g):
    """Distance between two holes, taking whichever end pairing is closer."""
    d1 = max(math.sqrt(sum((w[k][i] - g[k][i]) ** 2 for i in range(3))) for k in (0, 1))
    d2 = max(math.sqrt(sum((w[k][i] - g[1 - k][i]) ** 2 for i in range(3))) for k in (0, 1))
    return min(d1, d2)


def gate(src, reb, mapping):
    """Greedy nearest-first matching per part. Returns per-hole best distances."""
    dists, unmatched = [], []
    for s, want in src.items():
        have = list(reb.get(mapping.get(s), []))
        pairs = []
        for i, w in enumerate(want):
            for j, g in enumerate(have):
                d = pair_dist(w, g)
                if d <= 200.0:
                    pairs.append((d, i, j))
        pairs.sort()
        usedw, usedg = set(), set()
        for d, i, j in pairs:
            if i in usedw or j in usedg:
                continue
            usedw.add(i)
            usedg.add(j)
            dists.append((s, d))
        for i in range(len(want)):
            if i not in usedw:
                unmatched.append(s)
    return dists, unmatched


def main():
    dump = sys.argv[1] if len(sys.argv) > 1 else None
    src = rows_from_json(os.path.join(PROJ, "source.json"))
    reb = rows_from_dump(dump) if dump else rows_from_json(os.path.join(PROJ, "rebuild3.json"))
    mapping = json.load(io.open(os.path.join(PROJ, "source-map.json"), encoding="utf-8"))
    dists, unmatched = gate(src, reb, mapping)
    tot = sum(len(v) for v in src.values())
    print("source holes %d (lhm=2) | rebuild holes %d"
          % (tot, sum(len(v) for v in reb.values())))
    print()
    print("HOLE GATE -- worst-endpoint distance, greedy one-to-one per part")
    prev = 0
    for t in TOLS:
        n = len([1 for _s, d in dists if d <= t])
        print("   within %6.1f mm : %6d  (%.2f%%)   +%d" % (t, n, 100.0 * n / tot, n - prev))
        prev = n
    beyond = len([1 for _s, d in dists if d > TOLS[-1]])
    print("   beyond %6.1f mm : %6d" % (TOLS[-1], beyond))
    print("   no partner at all: %6d" % len(unmatched))
    src_cls = json.load(io.open(os.path.join(PROJ, "source.json"), encoding="utf-8"))
    cls = {x["h"]: x.get("sec", "") for x in src_cls["shapes"]}
    cls.update({x["h"]: "PLATE" for x in src_cls["plates"]})
    bad = [s for s, d in dists if d > TOLS[-1]] + unmatched
    if bad:
        print()
        print("the %d holes still wrong, by section:" % len(bad))
        for k, n in collections.Counter(cls.get(s, "?") for s in bad).most_common(14):
            print("   %-18s %d" % (k, n))
        print()
        print("parts involved: %d" % len(set(bad)))
        json.dump(sorted(set(bad)), io.open(os.path.join(PROJ, "hole-gate-open.json"), "w",
                                            encoding="utf-8"))
        print("   -> hole-gate-open.json")


if __name__ == "__main__":
    main()
