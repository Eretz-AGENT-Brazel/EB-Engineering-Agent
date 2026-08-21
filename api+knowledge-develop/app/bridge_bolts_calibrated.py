# -*- coding: utf-8 -*-
r"""Rebuild the bridge's bolts at their SOURCE DESIGNATIONS, by reading ProSteel's own bolt DB.

⭐⭐ THE LENGTH IS NOT A PARAMETER -- IT IS LOOKED UP FROM THE GRIP. `bolt len=` was passed on
every one of the 5,463 bolts in the first rebuild and ignored: every family came out ONE STEP
SHORT. 1,996 x "M12 x 50 8.8/S" in Bernie's model became 1,996 x "M12 x 45 8.8/S"; M16 55->50,
M20 65->60, M24 120->110, and so on -- the counts per family were right and every length was
wrong.

⭐ THE ANSWER IS IN THE BOLT DATABASE, and it is now readable with the 32-bit Jet provider:
    Data\Bolts\Australia.mdb @ AS_Bolt_88s     (style 8.8S)
    Data\Bolts\DinBolts.mdb  @ SCH912, SCH6914 (styles DIN912, DIN6914)
Each row carries DM, LAENGE and **KLEMMMIN / KLEMMMAX** -- the grip band that selects that
length. So the grip needed for a wanted length is not a guess:

    grip = (KLEMMMIN + KLEMMMAX)/2 - DELTA(diameter)

DELTA is the nut + washer + protruding thread that the grip does not include, measured once per
diameter against the table: M12 15.75, M16 20, M20 23.5, M24 28.5, M14/DIN912 15, M27/DIN6914 35.
That prediction scored **14/14** on every 8.8S designation the bridge uses, first try, including
M20 x 120 (grip 84) and M24 x 330 (grip 277.5).

⚠️ WHAT CANNOT BE BUILT with the installed libraries, and why -- 275 bolts:
  * M12 x 210 / 350 / 400 / 450 8.8/S  (177) -- AS_Bolt_88s stops at **M12 x 60**. There is no
    such row; Bernie's file has them from another library.
  * M12 x 150 "IG 8.8G"                 (40) -- the style does not exist here. The 27 installed
    styles are 4.6S, 8.8S, 8.8TB, 8.8TF (+GALV), A307/A325/A490, and the DIN family.
  * M24 x 32.769 8.8/S                  (58) -- not a catalogue length at all.
"""
import collections
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
KNOW = os.path.join(HERE, "knowledge")
RB = os.path.join(PROJ, "bridge model for amir - REBUILD-3.dwg")
W = 3600
DELTA = {("8.8S", "12"): 15.75, ("8.8S", "16"): 20.0, ("8.8S", "20"): 23.5,
         ("8.8S", "24"): 28.5, ("DIN912", "14"): 15.0, ("DIN6914", "27"): 35.0}
TABLE = {"8.8S": "AS", "DIN912": "SCH912", "DIN6914": "SCH6914"}


def bands():
    out = {}
    for line in io.open(os.path.join(KNOW, "bolt_table_AS_88s.tsv"), encoding="utf-8"):
        p = line.rstrip("\n").split("\t")
        if len(p) > 3 and p[0] != "DM":
            try:
                out[("AS", p[0], float(p[1]))] = (float(p[2]), float(p[3]))
            except ValueError:
                pass
    for line in io.open(os.path.join(KNOW, "bolt_table_DIN.tsv"), encoding="utf-8"):
        p = line.rstrip("\n").split("\t")
        if len(p) > 4 and p[0] != "TABLE":
            try:
                out[(p[0], p[1], float(p[2]))] = (float(p[3]), float(p[4]))
            except ValueError:
                pass
    return out


def grip_for(bd, style, dia, length):
    t = TABLE.get(style)
    d = DELTA.get((style, dia))
    if t is None or d is None:
        return None
    b = bd.get((t, dia, length))
    if b is None:
        return None
    g = (b[0] + b[1]) / 2.0 - d
    return g if g > 1.0 else None


def main():
    dry = "--dry" in sys.argv
    eb_api.use(os.path.basename(RB), task="bolts, calibrated", project="bridge-bernie")
    eb_api.build_target(RB)
    src = json.load(io.open(os.path.join(PROJ, "source.json"), encoding="utf-8"))
    bd = bands()

    plan, skip = [], collections.Counter()
    for b in src["bolts"]:
        try:
            L = float(b["len"])
        except (KeyError, ValueError):
            skip["no length"] += 1
            continue
        g = grip_for(bd, b.get("style", ""), b.get("dia", ""), L)
        if g is None:
            skip["M%s x %g %s" % (b.get("dia"), L, b.get("style"))] += 1
            continue
        a, c = [[float(x) for x in q.split(",")] for q in b["axis"].split(";")]
        v = [c[i] - a[i] for i in range(3)]
        n = math.sqrt(sum(x * x for x in v))
        if n <= 0:
            skip["zero axis"] += 1
            continue
        v = [x / n for x in v]
        p2 = [a[i] + g * v[i] for i in range(3)]
        plan.append(("bolt", dict(p1="%.4f,%.4f,%.4f" % tuple(a),
                                 p2="%.4f,%.4f,%.4f" % tuple(p2),
                                 dia=b["dia"], style=b["style"],
                                 layer=b.get("layer", "PS_Bolt"))))
    print("bolts to build: %d   not reproducible: %d" % (len(plan), sum(skip.values())))
    for k, n in skip.most_common():
        print("   - %-28s %d" % (k, n))
    if dry:
        return

    r = eb_api.run("wipe", wait=W, cls="Ks_Bolt")
    print("wipe existing bolts: %s" % r[:110])

    t0 = time.time()
    ok = 0
    notes = []
    for a in range(0, len(plan), 2000):
        rows = eb_api.batch(plan[a:a + 2000], wait=W)
        for (i, _o, res) in rows:
            if res.startswith("EB_OK"):
                ok += 1
            elif len(notes) < 10:
                notes.append(res[:120])
        print("   %d/%d built, %.0fs" % (ok, len(plan), time.time() - t0))
        print("   " + eb_api.run("save", wait=W)[:70])
    for n in notes:
        print("   !", n)
    print("built %d of %d" % (ok, len(plan)))


if __name__ == "__main__":
    main()
