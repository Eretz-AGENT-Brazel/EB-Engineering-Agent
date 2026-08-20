# -*- coding: utf-8 -*-
"""bridge_finish.py -- one command that closes out the bridge rebuild.

    python app/bridge_finish.py                       # gates only (the priority)
    python app/bridge_finish.py --annot               # gates, then the annotation clone
    python app/bridge_finish.py --annot-only

What it does, in this order and for these reasons:

  1. **The gates first.** They are what makes the rebuild a measurement instead of a claim,
     they take minutes, and they do not depend on the annotation being finished. Every gate
     is read on the rebuild AND on the source, because a gate whose reading on the source is
     unknown cannot tell a wrong model from a strict checker.
  2. **The annotation last, and optional.** `xclone` is instant on parts (0.4-0.5 s per call
     for 29 concrete shapes / 18 assemblies / 6 work frames / the grid) and pathologically
     slow on annotation: 200 dimensions measured 2 s, then 87 s, then ~40 MINUTES, and it
     BLOCKS THE WHOLE MACHINE while it runs (the busy instance keeps the COM registration
     and the file lock). So it goes last, in ONE call per class rather than chunks -- each
     call re-pays the reconciliation of styles, blocks and dimension associativity -- and
     never before the gates are in hand.

⚠️ It refuses to start if the drawing is not the rebuild, and it saves after every step.
"""
import io
import json
import os
import re
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
DEV = os.path.dirname(HERE)
if HERE not in sys.path:
    sys.path.insert(0, HERE)
import eb_api        # noqa: E402
import bridge_verify  # noqa: E402

PROJ = os.path.join(DEV, "projects", "bridge-bernie")
CACHE = os.path.join(PROJ, "source.json")
REBUILD = os.path.join(PROJ, "bridge model for amir - REBUILD.dwg")
SOURCE_NAME = "bridge model for amir.dwg"
SOURCE_COPY = os.path.join(PROJ, SOURCE_NAME)
ANNOT = ("AcDbRotatedDimension", "AcDbAlignedDimension", "AcDbDiametricDimension",
         "AcDbLine", "AcDbPoint", "AcDbPolyline", "AcDbCircle", "AcDb3dPolyline",
         "AcDbBlockReference")


def enter():
    eb_api.use(os.path.basename(REBUILD), task="finish the bridge rebuild",
               project="bridge-bernie")
    eb_api.build_target(REBUILD)
    eb_api.run("list", wait=900)
    act = eb_api._active_doc_name() or ""
    if "REBUILD" not in act:
        raise RuntimeError("active document is %r, not the rebuild" % act)
    print("in: " + act)


def gates():
    out = os.path.join(PROJ, "verify-%s.txt" % time.strftime("%Y-%m-%d-%H%M"))
    rc = bridge_verify.main([CACHE, REBUILD, SOURCE_COPY, out])
    print("gates written -> %s (exit %d)" % (out, rc))
    return rc


def annotation():
    """One call per class. Times each, so the cost curve is recorded rather than guessed."""
    d = json.load(io.open(CACHE, encoding="utf-8"))
    mpp = os.path.splitext(CACHE)[0] + "-map.json"
    mp = json.load(io.open(mpp, encoding="utf-8"))
    enter()
    for cls in ANNOT:
        hs = [e["h"] for e in d["entities"] if e["cls"] == cls and e["h"] not in mp]
        if not hs:
            print("%-24s nothing to do" % cls)
            continue
        print("%-24s %4d objects -- ONE call, this may take tens of minutes"
              % (cls, len(hs)))
        t = time.time()
        r = eb_api.run("xclone", wait=36000, **{"from": SOURCE_NAME,
                                                "handles": ",".join(hs)})
        print("   %.0f s -> %s" % (time.time() - t, r[:110]))
        try:
            for line in io.open(os.path.join(eb_api.channel(), "eb_xclone.txt"),
                                encoding="utf-8-sig"):
                f = line.rstrip("\n").split("\t")
                if len(f) == 2 and f[1] not in ("-", "MISSING") and not f[1].startswith("ERR"):
                    mp[f[0]] = f[1]
        except Exception as e:
            print("   could not read the map back: %s" % e)
        json.dump(mp, io.open(mpp, "w", encoding="utf-8"))
        print("   " + eb_api.run("save", wait=3600)[:80])
    print("mapped now: %d" % len(mp))


if __name__ == "__main__":
    args = sys.argv[1:]
    if "--annot-only" not in args:
        enter()
        gates()
    if "--annot" in args or "--annot-only" in args:
        annotation()
        print("\nre-running the gates after the annotation:")
        enter()
        gates()
