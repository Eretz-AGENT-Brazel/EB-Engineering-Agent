# -*- coding: utf-8 -*-
"""
exam_accept.py — final acceptance of the lesson-4 exam, measured against the
drawing, then save and capture.

Every number below is read back out of the model; nothing is echoed from input.
"""
import math
import os
import subprocess
import sys
import time

APP = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(APP)
PLUG = os.path.join(APP, "plugin")
OUT = os.path.join(ROOT, "projects", "שיעור-4", "files")
DWG = os.path.join(ROOT, "projects", "שיעור-4", "מבחן-שיעור-4.dwg")
sys.path.insert(0, APP)
import eb_api  # noqa: E402

# what the drawing and the task require
REQ = {"col_profile": "200",     # SHS 200/200/8
       "col_len": 3500.0,        # 3.5 m to the design level
       "plate": 400.0, "plate_t": 20.0,
       "holes": 4, "hole_dia": 23.0, "pitch": 300.0,
       "ribs": 8, "rib": 100.0, "rib_t": 10.0, "rib_keep": 25.0}

L = []
CH = []


def log(m):
    L.append(m)
    try:
        sys.stdout.write(m + "\n"); sys.stdout.flush()
    except Exception:
        pass


def check(name, good, detail=""):
    CH.append((name, good, detail))
    log("   %s %-38s %s" % ("PASS" if good else "FAIL", name, detail))


def rows(fn, kind):
    out = []
    p = os.path.join(PLUG, fn)
    if not os.path.exists(p):
        return out
    for line in open(p, encoding="utf-8-sig", errors="replace"):
        f = line.rstrip("\r\n").split("\t")
        if f and f[0] == kind:
            out.append(f)
    return out


def main():
    log("=" * 74)
    log("EXAM ACCEPTANCE — lesson 4: column + base plate to the floor + 8 ribs")
    log("=" * 74)
    log(str(eb_api.run("whoami", wait=30)))
    for op, kw in (("dumpfull2", {"out": "eb_full_acc.txt"}),
                   ("dumpholes", {"out": "eb_holes_acc.txt"}),
                   ("dumppoly", {"out": "eb_poly_acc.txt"}),
                   ("connscan", {"out": "eb_conn_acc.txt"})):
        log("  " + str(eb_api.run(op, wait=180, **kw)))

    log("\n--- column ---")
    col = None
    plate = None
    for f in rows("eb_full_acc.txt", "SHAPE"):
        z1 = float(f[4].split(",")[2])
        z2 = float(f[5].split(",")[2])
        log("  %-10s %-16s z %.0f..%.0f  L=%s" % (f[2], f[8], z1, z2, f[6]))
        if abs(z2 - z1) > 1000:
            col = {"prof": f[2], "z0": z1, "z1": z2, "L": float(f[6])}
        else:
            plate = {"prof": f[2], "name": f[8], "z": z1, "L": float(f[6])}

    check("column is a 200 square hollow", col is not None and "200" in col["prof"],
          col["prof"] if col else "-")
    check("design level at 3500", col is not None and abs(col["z1"] - REQ["col_len"]) < 1,
          "top z=%.0f" % col["z1"] if col else "-")
    check("column shortened by plate thickness",
          col is not None and abs(col["z0"] - REQ["plate_t"]) < 1,
          "starts z=%.0f, L=%.0f" % (col["z0"], col["L"]) if col else "-")

    log("\n--- base plate ---")
    check("plate sits ON the floor (0..20)",
          plate is not None and abs(plate["z"] - REQ["plate_t"] / 2) < 1,
          "centre z=%.0f" % plate["z"] if plate else "-")
    check("plate is 400 long", plate is not None and abs(plate["L"] - REQ["plate"]) < 1,
          "L=%.0f" % plate["L"] if plate else "-")
    check("plate thickness 20 (from its profile name)",
          plate is not None and "20" in plate["prof"], plate["prof"] if plate else "-")

    log("\n--- holes ---")
    hs = rows("eb_holes_acc.txt", "HOLE")
    dias = {}
    pts = []
    for f in hs:
        d = round(float(f[7]))
        dias[d] = dias.get(d, 0) + 1
        pts.append(tuple(float(x) for x in f[5].split(",")))
    check("4 holes", len(hs) == REQ["holes"], "%d" % len(hs))
    check("all holes dia 23", dias.get(23, 0) == len(hs) and len(hs) > 0, str(dias))
    if len(pts) >= 4:
        xs = [p[0] for p in pts]
        ys = [p[1] for p in pts]
        sx, sy = max(xs) - min(xs), max(ys) - min(ys)
        check("hole spacing 300 x 300", abs(sx - REQ["pitch"]) < 1 and abs(sy - REQ["pitch"]) < 1,
              "%.0f x %.0f" % (sx, sy))
        edge = REQ["plate"] / 2 - max(abs(min(xs)), abs(max(xs)))
        check("edge distance 50", abs(edge - 50) < 1, "%.0f" % edge)

    log("\n--- ribs ---")
    pol = rows("eb_poly_acc.txt", "POLY")
    shaped, sizes, contours = 0, set(), set()
    for f in pol:
        u = []
        for c in f[6].split(";"):
            try:
                q = tuple(round(float(x), 1) for x in c.split(","))
                if q not in u:
                    u.append(q)
            except Exception:
                pass
        if len(u) >= 5:
            shaped += 1
        xs = [p[0] for p in u]
        ys = [p[1] for p in u]
        if xs and ys:
            sizes.add((round(max(xs) - min(xs)), round(max(ys) - min(ys))))
        contours.add(f[6])
    check("8 rib plates", len(pol) == REQ["ribs"], "%d" % len(pol))
    check("every rib is shaped (5 verts), not a rectangle", shaped == len(pol) and shaped > 0,
          "%d/%d" % (shaped, len(pol)))
    check("all ribs 100 x 100", sizes == {(100, 100)}, str(sizes))
    check("all ribs identical contour", len(contours) == 1, "%d distinct" % len(contours))

    # the drawing: 25mm left at the outer and top edge -> the cut is 75x75
    if contours:
        c = list(contours)[0]
        want = "-50,-50,0;50,-50,0;50,-25,0;-25,50,0;-50,50,0"
        check("rib contour matches the drawing", c.strip() == want, c[:60])

    log("\n--- rib placement ---")
    centres = []
    for f in rows("eb_full_acc.txt", "PLATE"):
        centres.append(tuple(float(x) for x in f[2].split(",")))
    faces = {}
    for c in centres:
        key = ("+Y" if c[1] > 140 else "-Y" if c[1] < -140 else
               "+X" if c[0] > 140 else "-X" if c[0] < -140 else "?")
        faces[key] = faces.get(key, 0) + 1
    log("  ribs per column face: %s" % faces)
    check("2 ribs on each of the 4 faces",
          sorted(faces.values()) == [2, 2, 2, 2] and "?" not in faces, str(faces))
    zs = set(round(c[2]) for c in centres)
    check("ribs stand on the plate (centre z=70)", zs == {70}, str(zs))

    log("\n--- connection ---")
    lk = rows("eb_conn_acc.txt", "LINK")
    bp = [x for x in lk if "BASEPLATE[" in x[5] and "L=0 W=0" not in x[5]]
    check("base plate exists as a CONNECTION", len(bp) >= 1, "%d joints total" % len(lk))
    if bp:
        import re as _re
        m = _re.search(r"BASEPLATE\[([^\]]*)\]", bp[0][5])
        log("  joint parameters: %s" % (m.group(1) if m else "?"))

    log("\n" + "=" * 74)
    bad = [n for n, g, d in CH if not g]
    if bad:
        log("RESULT: %d FAILED -> %s" % (len(bad), "; ".join(bad)))
    else:
        log("RESULT: ALL %d CHECKS PASSED" % len(CH))
    log("=" * 74)

    # save
    log("\nsaving...")
    try:
        app = eb_api._app()
        d = app.ActiveDocument
        eb_api._send(d, "\x1b\x1b")
        time.sleep(0.8)
        d.Save()
        time.sleep(4.0)
        log("  saved: %s (%d bytes)" % (os.path.basename(DWG), os.path.getsize(DWG)))
    except Exception as e:
        log("  save failed: %s" % str(e)[:90])

    os.makedirs(OUT, exist_ok=True)
    open(os.path.join(OUT, "exam-acceptance.md"), "w", encoding="utf-8").write("\n".join(L))
    log("\nreport -> %s" % os.path.join(OUT, "exam-acceptance.md"))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8-sig", errors="replace")
    except Exception:
        pass
    main()
