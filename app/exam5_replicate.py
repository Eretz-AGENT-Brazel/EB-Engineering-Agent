# -*- coding: utf-8 -*-
"""
exam5_replicate.py — Amir's method, with the software's own tools only.

  1. delete every column's steel except column 1 (concrete untouched)
  2. finish column 1: its 2 middle-row floor anchors + 18 wall anchors
  3. replicate the whole detail to the other 19 places — one `replicate` call
     each (DeepCloneObjects + displacement + rotation about Z)

No LISP anywhere. Every operation is a ProSteel/AutoCAD API call through the
plugin.
"""
import json
import os
import re
import sys
import time

APP = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(APP)
sys.path.insert(0, APP)
import eb_api  # noqa: E402

ST = os.path.join(ROOT, "projects", "שיעור-5", "files", "exam5_state.json")
DEST = os.path.join(ROOT, "projects", "שיעור-5", "מבחן-שיעור-5.dwg")
F_HX = 156.0
W_HX, W_HZ = 452.0, 200.0
WP_T = 20.0
HALF = 420.0                      # box half-size around a column's detail
FACE_ANG = {"+X": 0.0, "+Y": 90.0, "-X": 180.0, "-Y": 270.0}


def log(m):
    try:
        sys.stdout.write(m + "\n"); sys.stdout.flush()
    except Exception:
        pass


def ok(r):
    return isinstance(r, str) and r.startswith("EB_OK")


def entities():
    """non-concrete entities with their centres, via COM read only"""
    out = []
    for e in eb_api._app().ActiveDocument.ModelSpace:
        try:
            if e.ObjectName in ("AcDb3dSolid", "PcRebarManager"):
                continue
            mn, mx = e.GetBoundingBox()
            out.append({"h": e.Handle, "cls": e.ObjectName,
                        "c": ((mn[0]+mx[0])/2, (mn[1]+mx[1])/2, (mn[2]+mx[2])/2),
                        "sz": (mx[0]-mn[0], mx[1]-mn[1], mx[2]-mn[2])})
        except Exception:
            pass
    return out


def main():
    t0 = time.time()
    st = json.load(open(ST, encoding="utf-8"))
    cols, centres = st["columns"], st["centres"]
    first = cols[0]
    log("=" * 70)
    log("EXAM 5 — one detail, then replicate (software API only, no LISP)")
    log("=" * 70)
    log("column 1: face %s at (%.1f, %.1f)" % (first["face"], first["x"], first["y"]))

    # ---- 1. keep only column 1 ----
    log("\n[1] deleting the other 19 columns' steel")
    al = entities()
    kill = [s for s in al
            if not (abs(s["c"][0] - first["x"]) <= HALF and abs(s["c"][1] - first["y"]) <= HALF)]
    log("    total steel %d, deleting %d" % (len(al), len(kill)))
    n = 0
    for s in kill:
        if ok(eb_api.delete(s["h"])):
            n += 1
    log("    deleted %d (%.1f min)" % (n, (time.time()-t0)/60))

    # ---- 2. complete column 1 ----
    log("\n[2] completing column 1")
    anch = [s for s in entities() if s["cls"] == "Ks_VolBody" and max(s["sz"]) < 1000]
    log("    anchors already on it: %d" % len(anch))
    src = anch[0]
    zf = src["c"][2]

    # the two middle-row floor anchors, using the software's replicate
    for dx in (-F_HX/2.0, F_HX/2.0):
        r = eb_api.run("replicate",
                       box="%.1f,%.1f,%.1f,%.1f" % (src["c"][0]-30, src["c"][1]-30,
                                                    src["c"][0]+30, src["c"][1]+30),
                       to="%.3f,%.3f" % (first["x"] + dx - src["c"][0],
                                         first["y"] - src["c"][1]),
                       wait=40)
        log("    floor anchor dx=%+.0f -> %s" % (dx, str(r)[:80]))

    # a horizontal master: replicate the anchor far away, then rotate it 90 about Y
    # (rotation about Y is not in `replicate`, so use the dedicated modify path)
    log("    wall anchors: need a horizontal one")
    log("    " + str(eb_api.run("replicate",
                                box="%.1f,%.1f,%.1f,%.1f" % (src["c"][0]-30, src["c"][1]-30,
                                                             src["c"][0]+30, src["c"][1]+30),
                                to="%.3f,%.3f" % (60000 - src["c"][0], -src["c"][1]),
                                wait=40))[:90])

    log("\n    NOTE: rotating about Y needs a matrix `replicate` does not expose yet;")
    log("    stopping here rather than falling back to LISP (Amir's rule).")
    log("\nelapsed %.1f min" % ((time.time()-t0)/60))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
