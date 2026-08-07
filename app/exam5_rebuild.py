# -*- coding: utf-8 -*-
"""
exam5_rebuild.py — EXAM 5, corrected build.

Amir's two corrections:
  1. A steel column goes at EVERY concrete-column face that meets the floor slab
     from the inside — a corner column has 2 such faces, an edge column has 3.
     => 4 corners x 2 + 4 edges x 3 = 20 steel columns, not 8.
  2. The floor detail must match the drawing exactly: SIX holes dia 28,
     2 columns @156 across, 3 rows @81 up (72|156|72 / 69|81|81|69).
     The base-plate macro lays a 2x2 field, so the middle row plus its two bolts
     are added explicitly.

Wall plates per drawing: 600x500x20, holes dia 28, 2 columns @452, 3 rows @200
(74|452|74 / 50|200|200|50); 3 plates per column at 1500 pitch, top plate's top
edge 200 below the top of the steel.
"""
import json
import math
import os
import re
import sys
import time

APP = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(APP)
PLUG = os.path.join(APP, "plugin")
sys.path.insert(0, APP)
import eb_api  # noqa: E402

COL_NAME, COL_CAT = "HE300B", "DIN_HEB"
HEB = 300.0
COL_LEN = 6000.0
FP, FP_T = 300.0, 20.0
F_HX, F_ROW = 156.0, 81.0          # across 156 ; rows at -81, 0, +81
WP_W, WP_H, WP_T = 600.0, 500.0, 20.0
W_HX, W_HZ = 452.0, 200.0
HOLE = 28.0
ANCHOR = 24.0                      # dia 28 hole -> M24 anchor
PITCH, TOP_GAP = 1500.0, 200.0
LOG = []


def log(m):
    LOG.append(m)
    try:
        sys.stdout.write(m + "\n"); sys.stdout.flush()
    except Exception:
        pass


def H(r):
    m = re.search(r"handle=(\w+)", r or "")
    return m.group(1) if m else None


def ok(r):
    return isinstance(r, str) and r.startswith("EB_OK")


def concrete():
    doc = eb_api._app().ActiveDocument
    tall, flat = [], []
    for e in doc.ModelSpace:
        try:
            if e.ObjectName != "AcDb3dSolid":
                continue
            mn, mx = e.GetBoundingBox()
            d = {"min": mn, "max": mx, "sz": (mx[0]-mn[0], mx[1]-mn[1], mx[2]-mn[2])}
            (tall if d["sz"][2] > 1000 else flat).append(d)
        except Exception:
            pass
    return tall, flat


def wipe_steel():
    """Remove everything that is not concrete, so the rebuild starts clean."""
    doc = eb_api._app().ActiveDocument
    kill = []
    for e in doc.ModelSpace:
        try:
            if e.ObjectName in ("AcDb3dSolid", "PcRebarManager"):
                continue
            kill.append(e.Handle)
        except Exception:
            pass
    n = 0
    for h in kill:
        r = eb_api.delete(h)
        if isinstance(r, str) and r.startswith("EB_OK"):
            n += 1
    return n, len(kill)


def inward_faces(c, slab):
    """Which faces of this concrete column look INTO the slab area.
    A face is inward if the slab continues beyond it."""
    x0, x1, y0, y1 = c["min"][0], c["max"][0], c["min"][1], c["max"][1]
    sx0, sx1, sy0, sy1 = slab["min"][0], slab["max"][0], slab["min"][1], slab["max"][1]
    tol = 5.0
    out = []
    if x1 < sx1 - tol:
        out.append(("+X", x1, (y0 + y1) / 2.0))
    if x0 > sx0 + tol:
        out.append(("-X", x0, (y0 + y1) / 2.0))
    if y1 < sy1 - tol:
        out.append(("+Y", y1, (x0 + x1) / 2.0))
    if y0 > sy0 + tol:
        out.append(("-Y", y0, (x0 + x1) / 2.0))
    return out


def main():
    t0 = time.time()
    log("=" * 76)
    log("EXAM 5 (corrected) — a steel column at every inward concrete face")
    log("=" * 76)
    log(str(eb_api.run("whoami", wait=25)))

    tall, flat = concrete()
    slab = flat[0]
    slab_top = slab["max"][2]
    log("\nslab: x %.0f..%.0f  y %.0f..%.0f  top z=%.0f | %d concrete columns"
        % (slab["min"][0], slab["max"][0], slab["min"][1], slab["max"][1],
           slab_top, len(tall)))

    log("\nclearing previous steel...")
    d, t = wipe_steel()
    log("  deleted %d/%d" % (d, t))

    # plan the columns
    plan = []
    for c in sorted(tall, key=lambda q: (q["min"][0], q["min"][1])):
        faces = inward_faces(c, slab)
        cxm = (c["min"][0] + c["max"][0]) / 2.0
        cym = (c["min"][1] + c["max"][1]) / 2.0
        log("  concrete (%.0f,%.0f): %d inward faces %s"
            % (cxm, cym, len(faces), [f[0] for f in faces]))
        for face, coord, along in faces:
            if face == "+X":
                sx, sy = coord + WP_T + HEB / 2.0, along
            elif face == "-X":
                sx, sy = coord - WP_T - HEB / 2.0, along
            elif face == "+Y":
                sx, sy = along, coord + WP_T + HEB / 2.0
            else:
                sx, sy = along, coord - WP_T - HEB / 2.0
            plan.append({"face": face, "coord": coord, "along": along,
                         "x": sx, "y": sy})
    log("\n=> %d steel columns planned (corners 2 faces, edges 3)" % len(plan))

    z0 = slab_top
    z1 = z0 + FP_T + COL_LEN
    centres = [z1 - TOP_GAP - WP_H / 2.0 - k * PITCH for k in range(3)]
    log("   steel z %.0f..%.0f (L=%.0f) | wall-plate centres %s"
        % (z0 + FP_T, z1, COL_LEN, ", ".join("%.0f" % v for v in centres)))

    # ---- build ----
    built = []
    for i, p in enumerate(plan, 1):
        r = eb_api.run("beam", name=COL_NAME, catalog=COL_CAT,
                       p1="%.3f,%.3f,%.3f" % (p["x"], p["y"], z0),
                       p2="%.3f,%.3f,%.3f" % (p["x"], p["y"], z1),
                       layer="PS_Shape", wait=40, _log=False)
        h = H(r)
        if not (ok(r) and h):
            log("   col %d FAILED %s" % (i, str(r)[:80])); continue
        p["h"] = h
        built.append(p)
        if i % 5 == 0 or i == len(plan):
            log("   columns %d/%d" % (i, len(plan)))
    log("   built %d columns (%.1f min)" % (len(built), (time.time()-t0)/60))

    json.dump({"columns": built, "z0": z0, "z1": z1, "slab_top": slab_top,
               "centres": centres},
              open(os.path.join(ROOT, "projects", "שיעור-5", "files",
                                "exam5_state.json"), "w", encoding="utf-8"))
    out = os.path.join(ROOT, "projects", "שיעור-5", "files", "exam5-rebuild.log")
    os.makedirs(os.path.dirname(out), exist_ok=True)
    open(out, "w", encoding="utf-8").write("\n".join(LOG))
    log("\nstate saved — next: exam5_details.py")


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
