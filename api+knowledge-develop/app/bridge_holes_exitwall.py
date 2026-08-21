# -*- coding: utf-8 -*-
"""Re-drill the misplaced holes on the bridge rebuild with the EXIT-WALL recipe.

⭐⭐ THE RULE, measured on a real SHS80X80X3.6 in the sandbox on 21/08/2026:
    `drill` puts the hole in the wall where the ray *LEAVES* the part, not where it enters.
        at = the OUTER +x face, n = -x  -> the hole appears in the -x wall
        at = the OUTER -x face, n = +x  -> the hole appears in the +x wall
        at = the INNER -x face, n = -x  -> the hole appears in the -x wall
So the naive recipe the first build used -- insert at the hole's start `a`, aim along a->b --
places the hole in the OPPOSITE wall whenever `a` is the outer face. On the bridge every
bolt through a hollow section is stored as a PAIR of wall holes sharing one axis, so exactly
half of each pair landed on top of its partner: part 639BF had 14 hole rows at 7 positions,
7 of its 14 wanted holes missing. 1,404 holes on 189 parts, the whole of the "unsolved
1,086" and then some.

⚠️ And this is why v194's "reverse the ray and retry" sweep could not fix them: reversing n
moves the exit wall too, so the hole just swapped ends instead of arriving. The sweep made
the model worse (exact 9,811 -> 9,337) and was rolled back.

THE RECIPE that scored 8/8 on every section type whose geometry was MEASURED rather than
guessed (SHS80 four walls, HE200B both flanges, U220 both flanges):
    at    = the midpoint of the wanted hole
    n     = away from the part's centre  (the outer end of a->b minus the inner end)
    depth = |a - b|                      (SetHoleDepth, new in v198)
`depth` is what makes it work on an I-profile, where the drill otherwise always crosses the
whole section -- flange=0/1/2 and innercontour=0/1 all measured as making no difference there.
Slots are unaffected: slot=7 with depth=5 keeps its 7 mm travel (checked lhm=1 and lhm=2).
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
W = 3600


def load(name):
    return json.load(io.open(os.path.join(PROJ, name), encoding="utf-8"))


def centres():
    """The bbox centre of every source part -- the reference for "which way is out"."""
    src = load("source.json")
    out = {}
    for r in src.get("shapes", []):
        try:
            lo, hi = r["bbox"].split(";")
            lo = [float(x) for x in lo.split(",")]
            hi = [float(x) for x in hi.split(",")]
            out[r["h"]] = [(lo[i] + hi[i]) / 2.0 for i in range(3)]
        except Exception:
            pass
    # A plate record carries no bbox -- it carries `c`, its centre, which sits at MID
    # THICKNESS. So for a plate the two ends of a hole are exactly equidistant from it and
    # "which way is out" is a tie. That is harmless: a plate has one wall, the drill crosses
    # the whole thickness either way, and the 60 plates skipped on the first run came back
    # identical from both directions.
    for r in src.get("plates", []):
        try:
            out[r["h"]] = [float(x) for x in r["c"].split(",")]
        except Exception:
            pass
    return out


def recipe(a, b, c):
    """at = midpoint, n = outward, depth = the wall thickness the hole crosses."""
    da = sum((a[i] - c[i]) ** 2 for i in range(3))
    db = sum((b[i] - c[i]) ** 2 for i in range(3))
    outer, inner = (a, b) if da >= db else (b, a)
    v = [outer[i] - inner[i] for i in range(3)]
    L = math.sqrt(sum(x * x for x in v))
    if L <= 0:
        return None
    return dict(at="%.4f,%.4f,%.4f" % tuple((a[i] + b[i]) / 2.0 for i in range(3)),
                n="%.6f,%.6f,%.6f" % tuple(x / L for x in v),
                depth="%.4f" % L)


def main():
    eb_api.use(os.path.basename(RB), task="exit-wall hole fix", project="bridge-bernie")
    eb_api.build_target(RB)
    mp = load("source-map.json")
    want = load("holes_resolved.json")
    cen = centres()
    # Default: EVERY part that carries holes. Rebuilding the whole hole layer from one measured
    # rule beats patching the parts a gate happened to flag -- the gate has been wrong twice
    # today, the rule has been measured on every section type in the model.
    parts = sorted(p for p in want if p in mp and p in cen)
    if len(sys.argv) > 1:
        sel = json.load(io.open(sys.argv[1], encoding="utf-8"))
        keep = set(x if isinstance(x, str) else x.get("src") for x in sel)
        parts = [p for p in parts if p in keep]
    print("parts to redo: %d   wanted holes on them: %d"
          % (len(parts), sum(len(want[p]) for p in parts)))

    # ---- phase 1: how many fields does each carry now? ----------------------------------
    t0 = time.time()
    rows = eb_api.batch([("mods", dict(handle=mp[p])) for p in parts], wait=W)
    nf = {}
    for (i, _op, r) in rows:
        n = 0
        for tok in r.split():
            if tok.startswith("holeFields="):
                n = int(tok.split("=")[1])
        nf[parts[i]] = n
    print("phase 1  fields read: %d parts, %d fields total, %.0fs"
          % (len(nf), sum(nf.values()), time.time() - t0))

    # ---- phase 2: wipe them, highest index first ----------------------------------------
    t0 = time.time()
    kills = []
    for p in parts:
        for k in range(nf.get(p, 0) - 1, -1, -1):
            kills.append(("killholefield", dict(handle=mp[p], field=k)))
    ok = 0
    for a in range(0, len(kills), 2000):
        rr = eb_api.batch(kills[a:a + 2000], wait=W)
        ok += len([1 for (_i, _o, r) in rr if r.startswith("EB_OK")])
    print("phase 2  fields killed: %d/%d, %.0fs" % (ok, len(kills), time.time() - t0))

    # ---- phase 3: re-drill with the exit-wall recipe ------------------------------------
    t0 = time.time()
    items, owner, skipped = [], [], 0
    for p in parts:
        for h in want[p]:
            kw = recipe(h["a"], h["b"], cen[p])
            if kw is None:
                skipped += 1
                continue
            kw.update(handle=mp[p], dia=h["d"], play=0)
            if h.get("slot"):
                kw["slot"] = h["slot"]
            items.append(("drill", kw))
            owner.append(p)
    ok, again = 0, []
    for a in range(0, len(items), 2000):
        rr = eb_api.batch(items[a:a + 2000], wait=W)
        for (i, _o, r) in rr:
            if r.startswith("EB_OK"):
                ok += 1
            else:
                again.append((items[a + i], owner[a + i]))
    print("phase 3  drilled: %d/%d (skipped %d), %.0fs"
          % (ok, len(items), skipped, time.time() - t0))

    # ---- phase 4: retry the refusals one at a time ---------------------------------------
    # ⭐ A drill that Apply()s to rc=0 inside a big batch lands perfectly when it is re-issued
    # on its own: all 24 holes that phase 3 refused on 11 plates were reproduced to 0.04 mm by
    # four different parameter sets, one call each. So the refusal is not geometry -- a part
    # taking several new hole fields back to back in ONE round trip rejects some of them, and
    # the op reported EB_ERR honestly rather than pretending. ⇒ ALWAYS RETRY, and retry alone.
    t0 = time.time()
    left = []
    for (it, src) in again:
        r = eb_api.run(it[0], wait=W, **it[1])
        if not r.startswith("EB_OK"):
            left.append((it, src))
    print("phase 4  retried %d alone, still refused %d, %.0fs"
          % (len(again), len(left), time.time() - t0))

    # ---- phase 5: the hair's breadth --------------------------------------------------------
    # ⭐ 0.0095 mm decides it. On 11 plates, 24 holes sit so close to the plate's edge that the
    # exact insert point falls a hundredth of a millimetre OUTSIDE my rebuilt contour and
    # ProSteel refuses -- correctly, the centre is off the material. Typing the same coordinate
    # rounded to 0.1 mm (184832.1500 instead of 184832.1595) drills it perfectly, and moving
    # 0.5 mm ALONG the ray does not help: the sensitive direction is IN THE PLANE of the plate.
    # So pull the insert point toward the part's centre in micro-steps and record how far it
    # had to move. 0.05 mm is four orders of magnitude below any fabrication tolerance, and a
    # hole present at 0.05 mm is far closer to Bernie's model than a hole missing.
    nudged, hard = [], []
    for (it, src) in left:
        at = [float(x) for x in it[1]["at"].split(",")]
        nn = [float(x) for x in it[1]["n"].split(",")]
        c = cen[src]
        v = [c[i] - at[i] for i in range(3)]
        d = sum(v[i] * nn[i] for i in range(3))
        v = [v[i] - d * nn[i] for i in range(3)]           # in-plane, toward the centre
        L2 = math.sqrt(sum(x * x for x in v))
        if L2 <= 0:
            hard.append((it, src, None))
            continue
        v = [x / L2 for x in v]
        done = None
        for step in (0.02, 0.05, 0.1, 0.25, 0.5, 1.0):
            kw = dict(it[1])
            kw["at"] = "%.4f,%.4f,%.4f" % tuple(at[i] + step * v[i] for i in range(3))
            r = eb_api.run(it[0], wait=W, **kw)
            if r.startswith("EB_OK"):
                done = step
                break
        if done is None:
            hard.append((it, src, None))
        else:
            nudged.append((it[1]["handle"], done))
    print("phase 5  nudged in-plane: %d landed, %d still refused" % (len(nudged), len(hard)))
    if nudged:
        byst = {}
        for _h, st in nudged:
            byst[st] = byst.get(st, 0) + 1
        print("         steps used: %s mm"
              % ", ".join("%.2f x%d" % (k, byst[k]) for k in sorted(byst)))
    for (it, src, _x) in hard[:20]:
        print("   ! refused even nudged:", it[1].get("handle"), it[1].get("at"))
    print(eb_api.run("save", wait=W)[:90])


if __name__ == "__main__":
    main()
