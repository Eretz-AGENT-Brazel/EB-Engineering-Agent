# -*- coding: utf-8 -*-
"""bridge_replace_fix.py -- put back the plates my own corrective pass pushed out of place.

    python app/bridge_replace_fix.py [--dry]

⛔⛔ WHAT WENT WRONG, because it is a rule and not a bug.

`bridge_build.plates()` builds each plate at `at = org` (measured correct), and then runs a
"measure and re-place" pass: read every plate's bounding-box centre, compare it with the
source's, and move whoever missed. That pass ran BEFORE the cuts, chamfers and poly-cuts were
applied -- and **the source's bounding-box centre already includes the effect of the source's
own modifications.** So the pass moved 902 correctly-placed plates by exactly the amount the
modifications would later account for, and then the modifications moved them again.

The gates found it, and the correlation is total:

    794 plates off position   ->  794 of 794 were touched by the re-place pass
                              ->  794 of 794 carry modifications in the source
    7,013 plates exact        ->  5,875 of them carry modifications too, and the pass
                                  never touched them

> ⭐⭐⭐ **A POSITION GATE MEASURED ON A BOUNDING BOX IS ONLY VALID WHEN BOTH SIDES CARRY THE
> SAME MODIFICATIONS.** Build, then apply every modification, and only then measure and
> correct. Correcting earlier means correcting against a target that includes geometry the
> part does not have yet.
> ⭐⭐ **And a correction must MOVE the part, never delete and rebuild it** -- a rebuild throws
> away the holes, cuts and chamfers the part has since acquired. `delete + create` was
> acceptable when the pass ran before the modifications; after them it is destructive.

So this file moves each offender by its measured delta through COM (`entity.Move`), which
preserves every modification, and re-measures.
"""
import io
import json
import math
import os
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
DEV = os.path.dirname(HERE)
if HERE not in sys.path:
    sys.path.insert(0, HERE)
import eb_api  # noqa: E402

PROJ = os.path.join(DEV, "projects", "bridge-bernie")
CACHE = os.path.join(PROJ, "source.json")
REBUILD = os.path.join(PROJ, "bridge model for amir - REBUILD.dwg")


def num(s):
    return [float(v) for v in str(s).split(",")]


def main(dry=False):
    src = json.load(io.open(CACHE, encoding="utf-8"))
    mp = json.load(io.open(os.path.splitext(CACHE)[0] + "-map.json", encoding="utf-8"))
    eb_api.use(os.path.basename(REBUILD), task="move the mis-corrected plates back",
               project="bridge-bernie")
    eb_api.build_target(REBUILD)
    eb_api.run("list", wait=900)
    act = eb_api._active_doc_name() or ""
    if "REBUILD" not in act:
        raise RuntimeError("active document is %r, not the rebuild" % act)
    print("in: " + act)

    # measure fresh -- never trust a cache for the thing you are about to change
    eb_api.run("dumpfull2", wait=1800)
    got = {}
    for line in io.open(os.path.join(eb_api.channel(), "eb_full2.txt"),
                        encoding="utf-8-sig"):
        f = line.rstrip("\n").split("\t")
        if f and f[0] == "PLATE":
            got[f[1]] = f[2]
    todo = []
    for p in src["plates"]:
        t = mp.get(p["h"])
        if not t or t not in got:
            continue
        want, have = num(p["c"]), num(got[t])
        d = [want[i] - have[i] for i in range(3)]
        if max(abs(v) for v in d) > 0.01:
            todo.append((t, have, want, math.dist(want, have)))
    print("plates to move: %d (worst %.3f mm)"
          % (len(todo), max([x[3] for x in todo]) if todo else 0.0))
    if dry or not todo:
        for t, have, want, dd in sorted(todo, key=lambda x: -x[3])[:8]:
            print("   %s  by %.3f mm" % (t, dd))
        return
    app, doc = eb_api._app_doc()
    # ⚠️ AutoCAD COM WANTS A VARIANT ARRAY OF DOUBLES, NOT A PYTHON LIST. Passing lists gave
    # E_INVALIDARG (0x80070057) on all 793 plates in 3 seconds -- a type error wearing the
    # costume of a modelling failure. `pt()` below is the whole fix.
    import pythoncom
    from win32com.client import VARIANT

    def pt(v):
        return VARIANT(pythoncom.VT_ARRAY | pythoncom.VT_R8, [float(x) for x in v])

    moved = failed = 0
    t0 = time.time()
    for t, have, want, dd in todo:
        try:
            ent = doc.HandleToObject(t)
            ent.Move(pt(have), pt(want))   # COM move: the modifications travel with the part
            moved += 1
        except Exception as e:
            failed += 1
            if failed <= 3:
                print("   %s: %s" % (t, str(e)[:110]))
        if moved and moved % 200 == 0:
            print("   %d/%d moved, %.0fs" % (moved, len(todo), time.time() - t0))
    print("moved %d, failed %d, %.0fs" % (moved, failed, time.time() - t0))
    print(eb_api.run("save", wait=3600)[:120])

    # re-measure, because a move that was not read back is not a move
    eb_api.run("dumpfull2", wait=1800)
    got2 = {}
    for line in io.open(os.path.join(eb_api.channel(), "eb_full2.txt"),
                        encoding="utf-8-sig"):
        f = line.rstrip("\n").split("\t")
        if f and f[0] == "PLATE":
            got2[f[1]] = f[2]
    left = 0
    worst = 0.0
    for p in src["plates"]:
        t = mp.get(p["h"])
        if not t or t not in got2:
            continue
        d = math.dist(num(p["c"]), num(got2[t]))
        if d > 0.01:
            left += 1
            worst = max(worst, d)
    print("plate CENTRE after the move: %d still off, worst %.4f mm" % (left, worst))


if __name__ == "__main__":
    main("--dry" in sys.argv[1:])
