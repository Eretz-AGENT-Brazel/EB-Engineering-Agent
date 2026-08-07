# -*- coding: utf-8 -*-
"""
exam5_build.py — EXAM 5: HEB 300 steel columns anchored to the floor and to the
concrete columns, facing INWARD.

From the drawing:
  BASE PLATE FOR FLOOR  t=20 : 300 x 300, holes dia 28, 2 columns @156, 3 rows @81
                               (72|156|72 across, 69|81|81|69 up)
  BASE PLATE FOR WALL   t=20 : 600 x 500, holes dia 28, 2 columns @452, 3 rows @200
                               (74|452|74 across, 50|200|200|50 up)

Amir's placement rule for the wall plates:
  3 per column, 1500 mm between plate edges (= 1500 pitch), and the TOP plate's
  top edge 200 mm below the top of the steel column.

Applying the lesson-5 debt fix: the base-plate connection is built FROM A
TEMPLATE, not from an empty parameter object — that is what makes the anchor
bolts come out complete (with nuts) on the first pass.
"""
import math
import os
import re
import sys
import time

APP = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(APP)
sys.path.insert(0, APP)
import eb_api  # noqa: E402

# ---- drawing figures ----
COL = "HEB300"
COL_LEN = 6000.0
SLAB_TOP = 30.0
FP, FP_T = 300.0, 20.0          # floor plate 300x300x20
F_HX, F_HY = 156.0, 162.0       # 2 columns @156 ; 3 rows @81 -> outer span 162
WP_W, WP_H, WP_T = 600.0, 500.0, 20.0
W_HX, W_HZ = 452.0, 200.0       # 2 columns @452 ; 3 rows @200
HOLE = 28.0
WALL_PITCH = 1500.0
TOP_GAP = 200.0
HEB = 300.0                     # HEB300 is 300 wide / 300 deep

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


def read_concrete():
    app = eb_api._app()
    tall, flat = [], []
    for e in app.ActiveDocument.ModelSpace:
        try:
            if e.ObjectName != "AcDb3dSolid":
                continue
            mn, mx = e.GetBoundingBox()
            d = {"min": mn, "max": mx, "sz": (mx[0]-mn[0], mx[1]-mn[1], mx[2]-mn[2])}
            (tall if d["sz"][2] > 1000 else flat).append(d)
        except Exception:
            pass
    return tall, flat


def inward_face(c, cx, cy):
    """Which concrete face looks toward the middle of the building, and therefore
    which side the steel column and its wall plates sit on.
    Columns at the extreme X anchor in the X direction; the ones that are middle
    in X anchor in Y."""
    x0, x1 = c["min"][0], c["max"][0]
    y0, y1 = c["min"][1], c["max"][1]
    mx, my = (x0 + x1) / 2.0, (y0 + y1) / 2.0
    dx, dy = cx - mx, cy - my
    if abs(dx) >= abs(dy):
        return ("+X", x1, my) if dx > 0 else ("-X", x0, my)
    return ("+Y", y1, mx) if dy > 0 else ("-Y", y0, mx)


def main():
    t0 = time.time()
    log("=" * 74)
    log("EXAM 5 — HEB 300 columns anchored to floor + concrete columns (inward)")
    log("=" * 74)
    log(str(eb_api.run("whoami", wait=25)))

    tall, flat = read_concrete()
    if not flat or not tall:
        log("!! concrete not found"); return
    slab_top = flat[0]["max"][2]
    xs = [(c["min"][0] + c["max"][0]) / 2.0 for c in tall]
    ys = [(c["min"][1] + c["max"][1]) / 2.0 for c in tall]
    cx, cy = (min(xs) + max(xs)) / 2.0, (min(ys) + max(ys)) / 2.0
    log("\nslab top z=%.1f | %d concrete columns | building centre (%.1f, %.1f)"
        % (slab_top, len(tall), cx, cy))

    # steel column runs from the slab top; the base plate takes 20mm out of it so
    # the physical length stays 6000
    z0 = slab_top
    z1 = z0 + FP_T + COL_LEN          # after shortening: z0+20 .. z1, L=6000
    top = z1
    # wall plates: top plate's TOP edge 200 below the column top, then 1500 pitch
    centres = [top - TOP_GAP - WP_H / 2.0 - k * WALL_PITCH for k in range(3)]
    log("steel column: z %.0f -> %.0f (L=%.0f after shortening)" % (z0 + FP_T, z1, COL_LEN))
    log("wall-plate centres z: %s" % ", ".join("%.0f" % c for c in centres))

    built = []
    for i, c in enumerate(sorted(tall, key=lambda q: (q["min"][0], q["min"][1])), 1):
        face, coord, along = inward_face(c, cx, cy)
        # steel column sits clear of the concrete: plate thickness + half the HEB
        if face == "+X":
            sx, sy = coord + WP_T + HEB / 2.0, along
        elif face == "-X":
            sx, sy = coord - WP_T - HEB / 2.0, along
        elif face == "+Y":
            sx, sy = along, coord + WP_T + HEB / 2.0
        else:
            sx, sy = along, coord - WP_T - HEB / 2.0
        log("\n--- steel column %d : concrete face %s, steel at (%.1f, %.1f) ---"
            % (i, face, sx, sy))

        # HEB 300 lives in the shapes DB as HE300B / DIN_HEB
        r = None
        for nm, cat in (("HE300B", "DIN_HEB"), ("HEB300", ""), ("HE 300 B", "DIN_HEB")):
            r = eb_api.run("beam", name=nm, catalog=cat,
                           p1="%.3f,%.3f,%.3f" % (sx, sy, z0),
                           p2="%.3f,%.3f,%.3f" % (sx, sy, z1),
                           layer="PS_Shape", wait=40)
            if ok(r):
                break
        h = H(r)
        if not ok(r):
            log("   column FAILED: %s" % str(r)[:110]); continue
        log("   column %s" % h)
        built.append({"h": h, "x": sx, "y": sy, "face": face, "coord": coord,
                      "along": along, "centres": centres})

    log("\n%d columns built in %.1f min" % (len(built), (time.time() - t0) / 60))
    import json
    st = os.path.join(ROOT, "projects", "שיעור-5", "files", "exam5_state.json")
    os.makedirs(os.path.dirname(st), exist_ok=True)
    json.dump({"columns": built, "z0": z0, "z1": z1, "slab_top": slab_top,
               "centres": centres}, open(st, "w", encoding="utf-8"))
    log("state -> %s" % st)
    out = os.path.join(ROOT, "projects", "שיעור-5", "files", "exam5-build.log")
    open(out, "w", encoding="utf-8").write("\n".join(LOG))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
