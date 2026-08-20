# -*- coding: utf-8 -*-
"""bridge_clone_probe.py -- prove `xclone` one class at a time, before any loop.

    python app/bridge_clone_probe.py <source.json> <rebuild.dwg> <source.dwg>

The LETHAL-CALLS protocol, applied to a call nobody here has made before: save first, clone
ONE object, count the drawing before and after, read the result back, save again. One class
per step, so a crash names its own culprit.

⚠️ AND THE COUNT IS THE POINT, not the return value. `Ks_Assembly` and `Ks_ConcretePanel` own
other objects (members, rebar), and `WblockCloneObjects` follows ownership -- so cloning ONE
of them can land dozens of parts. A clone that quietly duplicates members would pass every
per-part gate in the verifier and inflate the model. Asking for 1 and getting 1 is the test.
"""
import io
import json
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)
import eb_api  # noqa: E402

PROBES = [
    ("AcDb3dSolid", "the survey/existing-condition solids"),
    ("Ks_ConcretePanel", "a ProConcrete wall panel -- may own rebar"),
    ("Ks_ConcreteSlab", "a ProConcrete slab"),
    ("Ks_ConcreteShape", "a ProConcrete beam/column"),
    ("Ks_BendShape", "a folded plate"),
    ("AcDbRotatedDimension", "one dimension"),
    ("AcDbBlockReference", "a block reference -- brings its definition"),
    ("Ks_WorkFrame", "a work frame"),
    ("Ks_Grid", "the grid -- the API cannot even touch this one safely"),
    ("Ks_Assembly", "an assembly -- MAY DRAG ITS MEMBERS"),
    ("__nocat__", "a profile whose catalogue this machine does not have"),
]


def count(label=""):
    r = eb_api.run("list", wait=600)
    m = re.search(r"list (\d+) entities", r)
    return int(m.group(1)) if m else -1


def main(cache, rbd, src):
    d = json.load(io.open(cache, encoding="utf-8"))
    eb_api.use(os.path.basename(rbd), task="probe xclone", project="bridge-bernie")
    eb_api.build_target(rbd)
    eb_api.run("list", wait=600)
    act = eb_api._active_doc_name() or ""
    if "REBUILD" not in act:
        raise RuntimeError("active document is %r, not the rebuild" % act)
    print("in: %s" % act)
    nocat = ("PANEL", "AMUDIM", "HOESCHISODACH")
    rows = []
    for cls, why in PROBES:
        if cls == "__nocat__":
            hs = [s["h"] for s in d["shapes"] if s["cat"] in nocat]
        else:
            hs = [e["h"] for e in d["entities"] if e["cls"] == cls]
        if not hs:
            rows.append((cls, "-", "no such object in the source", ""))
            continue
        h = hs[0]
        eb_api.run("save", wait=900)
        before = count()
        r = eb_api.run("xclone", wait=900, **{"from": os.path.basename(src), "handles": h})
        after = count()
        got = ""
        m = re.search(r"cloned=(\d+)", r)
        if m and int(m.group(1)) >= 1:
            try:
                line = io.open(os.path.join(eb_api.channel(), "eb_xclone.txt"),
                               encoding="utf-8-sig").read().strip().splitlines()[0]
                newh = line.split("\t")[-1]
                got = eb_api.run("props", wait=600, handle=newh)[:150]
            except Exception as e:
                got = "readback failed: %s" % e
        delta = after - before
        rows.append((cls, "%d -> %d (delta %+d)" % (before, after, delta),
                     r[:110], got))
        print("%-22s asked 1, drawing %s\n   %s\n   %s"
              % (cls, "%d -> %d (delta %+d)" % (before, after, delta), r[:150], got[:150]))
        # leave the drawing as we found it, so the probe costs nothing
        if delta > 0:
            try:
                line = io.open(os.path.join(eb_api.channel(), "eb_xclone.txt"),
                               encoding="utf-8-sig").read().strip().splitlines()[0]
                newh = line.split("\t")[-1]
                if newh not in ("-", "MISSING"):
                    eb_api.run("erase", wait=600, handles=newh)
            except Exception:
                pass
            back = count()
            if back != before:
                print("   ⚠️ %d object(s) LEFT BEHIND by the probe (%d vs %d) -- "
                      "the clone owns more than the one object asked for"
                      % (back - before, back, before))
    eb_api.run("save", wait=900)
    print("\n=== summary ===")
    for cls, delta, res, got in rows:
        print("%-22s %-24s %s" % (cls, delta, res[:80]))


if __name__ == "__main__":
    if len(sys.argv) < 4:
        print(__doc__)
        sys.exit(2)
    main(sys.argv[1], sys.argv[2], sys.argv[3])
