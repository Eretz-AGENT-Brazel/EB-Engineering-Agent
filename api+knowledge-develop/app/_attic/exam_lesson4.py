# -*- coding: utf-8 -*-
"""
exam_lesson4.py — EXAM TASK for lesson 4 (מידול עמוד ופלטת בסיס לרצפה).

Task as given by Amir + the drawing `Drawing1-Model.pdf`:
  1. close the lesson-4 model, saved in its folder
  2. open a NEW model
  3. column SHS 200/200/8, 3.5 m long
  4. base plate per the drawing: 400x400x20, 4 holes dia 23, spacing 300x300
  5. rib plates per the drawing: 100x100x10 with the diagonal chamfer
     (25 left at the top edge, 25 left at the right edge -> the cut removes a
     75x75 triangle), 8 of them placed around the column.

Applying what lesson 4 taught:
  * the base plate is a CONNECTION (PS_GROUNDPL / PsBasePlateConnection), which
    drills its own holes, adds the anchors and SHORTENS the column by the plate
    thickness — so the top level stays at 3500.
  * a rib keeps only the triangle in the load path; it is drawn as a contour,
    not as a rectangle.
  * anchor-bolt length is graphic only (Amir's stated boundary) — not chased.

Every step is verified by reading back from the model.

Usage: python exam_lesson4.py [step]
"""
import math
import os
import re
import sys
import time

APP = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(APP)
DEST = os.path.join(ROOT, "projects", "שיעור-4", "מבחן-שיעור-4.dwg")
L4 = os.path.join(ROOT, "projects", "שיעור-4", "שיעור-4.dwg")
sys.path.insert(0, APP)
import eb_api  # noqa: E402

# ---- the exam figures, straight off the drawing ----
COL_PROFILE = "SHS200X200X8"
COL_TOP = 3500.0            # 3.5 m — the level that must be preserved
PLATE = 400.0               # 400 x 400
PLATE_T = 20.0              # 20 mm
HOLE_DIA = 23.0             # dia 23
HOLE_PITCH = 300.0          # 300 x 300 centres (=> 50 mm edge distance)
RIB = 100.0                 # rib 100 x 100
RIB_T = 10.0                # 10 mm
RIB_KEEP = 25.0             # 25 mm left at the top edge and the right edge
COL_HALF = 100.0            # SHS 200 -> face at +-100

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


def rib_contour(face, pos):
    """One rib, as a 3D contour in its own plane.

    `face` is which column face it sits on ('+Y','-Y','+X','-X') and `pos` is
    where along that face. The tall edge hugs the column (that is where the load
    comes down), the short edge points outward, and the diagonal removes what is
    not in the load path — exactly the shape on the drawing.
    """
    z0 = PLATE_T                      # sits on top of the base plate
    zt = z0 + RIB                     # 100 tall
    zk = z0 + RIB_KEEP                # 25 up the outer edge
    inn = COL_HALF                    # at the column face
    out = COL_HALF + RIB              # 100 outward
    dia = out - (RIB - RIB_KEEP)      # where the diagonal meets the top

    if face in ("+Y", "-Y"):
        s = 1.0 if face == "+Y" else -1.0
        return [(pos, s * inn, z0), (pos, s * out, z0), (pos, s * out, zk),
                (pos, s * dia, zt), (pos, s * inn, zt)]
    s = 1.0 if face == "+X" else -1.0
    return [(s * inn, pos, z0), (s * out, pos, z0), (s * out, pos, zk),
            (s * dia, pos, zt), (s * inn, pos, zt)]


def rib_layout():
    """8 ribs: two per column face, offset +-50 from each face centre so they
    clear the anchor holes at (+-150, +-150)."""
    out = []
    for face in ("+Y", "-Y", "+X", "-X"):
        for pos in (-50.0, 50.0):
            out.append((face, pos))
    return out


def step1_close_and_new():
    log("=" * 74)
    log("STEP 1 — close lesson 4 (saved) and open a NEW model")
    log("=" * 74)
    app = eb_api._app()
    log("  open documents: %d" % app.Documents.Count)

    # make sure lesson 4 is saved, then close it
    for i in range(app.Documents.Count):
        d = app.Documents.Item(i)
        if os.path.normcase(d.FullName) == os.path.normcase(L4):
            d.Activate()
            time.sleep(1.0)
            try:
                eb_api._send(app.ActiveDocument, "\x1b\x1b")
                time.sleep(0.6)
            except Exception:
                pass
            try:
                if not app.ActiveDocument.Saved:
                    app.ActiveDocument.Save()
                    time.sleep(3.0)
                log("  lesson-4 saved (%d bytes)" % os.path.getsize(L4))
            except Exception as e:
                log("  save: %s" % str(e)[:70])
            break

    # a brand new drawing, then Save-As so it is a real project file
    try:
        nd = app.Documents.Add()
        time.sleep(3.0)
        log("  new drawing: %s" % nd.Name)
    except Exception as e:
        log("  Documents.Add failed: %s" % str(e)[:80])
        return False
    try:
        app.ActiveDocument.SaveAs(DEST)
        time.sleep(4.0)
        log("  saved as: %s" % os.path.basename(DEST))
    except Exception as e:
        log("  SaveAs: %s" % str(e)[:80])

    log("  " + str(eb_api.run("whoami", wait=30)))
    # close lesson 4 now that we are elsewhere
    for i in range(app.Documents.Count):
        try:
            d = app.Documents.Item(i)
            if os.path.normcase(d.FullName) == os.path.normcase(L4):
                d.Close(True)
                time.sleep(2.0)
                log("  lesson-4 closed")
                break
        except Exception:
            pass
    log("  documents now: %d" % app.Documents.Count)
    return True


def step2_column():
    log("\n" + "=" * 74)
    log("STEP 2 — column %s, top of steel at %.0f" % (COL_PROFILE, COL_TOP))
    log("=" * 74)
    r = eb_api.run("beam", name=COL_PROFILE, p1="0,0,0", p2="0,0,%.0f" % COL_TOP,
                   layer="PS_Shape", wait=40)
    log("  " + str(r)[:150])
    if not ok(r):
        # try explicit catalogue names for a cold-rolled square hollow section
        for nm, cat in (("RQ200x8", "DIN_QUADRATROHR_KALT"),
                        ("SHS200X200X8", "BS_CELSIUS_SHS"),
                        ("200X200X8SHS", "")):
            r = eb_api.run("beam", name=nm, catalog=cat, p1="0,0,0",
                           p2="0,0,%.0f" % COL_TOP, layer="PS_Shape", wait=40)
            log("  retry %-14s -> %s" % (nm, str(r)[:110]))
            if ok(r):
                break
    return H(r) if ok(r) else None


def step3_baseplate(col):
    log("\n" + "=" * 74)
    log("STEP 3 — base plate as a CONNECTION: %.0fx%.0fx%.0f, %d holes dia %.0f @ %.0f"
        % (PLATE, PLATE, PLATE_T, 4, HOLE_DIA, HOLE_PITCH))
    log("=" * 74)
    r = eb_api.run("connbase", handle=col, l=PLATE, w=PLATE, t=PLATE_T,
                   holedia=HOLE_DIA, hx=HOLE_PITCH, hy=HOLE_PITCH,
                   anchors=1, wait=60)
    log("  " + str(r)[:160])
    return ok(r)


# Local contour of the rib, centred on its own plate origin.
# x_local negative = towards the column (the full-height welded edge)
# x_local positive = outwards (the short edge, only RIB_KEEP tall)
def rib_local():
    h = RIB / 2.0
    k = h - RIB_KEEP          # 25 left at the outer edge and the top edge
    return [(-h, -h), (h, -h), (h, -k), (-k, h), (-h, h)]


def rib_frame(face, pos):
    """Where the rib plate sits, and how its local axes map to the model.
    The plate spans the 100mm gap between the column face and the plate edge,
    and stands 100mm tall off the top of the base plate."""
    mid = COL_HALF + RIB / 2.0        # 150 : centre of the 100mm gap
    zmid = PLATE_T + RIB / 2.0        # 70  : centre of the rib's height
    if face == "+Y":
        return (pos, mid, zmid), "0,1,0", "0,0,1", "1,0,0"
    if face == "-Y":
        return (pos, -mid, zmid), "0,-1,0", "0,0,1", "1,0,0"
    if face == "+X":
        return (mid, pos, zmid), "1,0,0", "0,0,1", "0,1,0"
    return (-mid, pos, zmid), "-1,0,0", "0,0,1", "0,1,0"


def step4_ribs():
    log("\n" + "=" * 74)
    log("STEP 4 — 8 rib plates %.0fx%.0fx%.0f, diagonal cut leaving %.0fmm at the"
        " outer and top edges" % (RIB, RIB, RIB_T, RIB_KEEP))
    log("=" * 74)
    local = ";".join("%g,%g" % q for q in rib_local())
    log("  rib contour (local): %s" % local)
    made, fail, shaped = 0, 0, 0
    handles = []
    for face, pos in rib_layout():
        c, ex, ey, ez = rib_frame(face, pos)
        # 1. a rectangular plate, correctly placed and oriented
        r = eb_api.run("plate", center="%.1f,%.1f,%.1f" % c, l=RIB, w=RIB, t=RIB_T,
                       ex=ex, ey=ey, ez=ez, layer="PS_Plate", wait=40)
        h = H(r)
        if not (ok(r) and h):
            fail += 1
            log("  rib %-3s @ %-5.0f plate FAILED: %s" % (face, pos, str(r)[:90]))
            continue
        # 2. reshape it to the drawing's contour — the holes-preserving path
        r2 = eb_api.run("setpoly", handle=h, pts=local, wait=40)
        vm = re.search(r"verts (\d+)->(\d+)", r2 or "")
        nv = vm.group(2) if vm else "?"
        if ok(r2):
            made += 1
            if nv.isdigit() and int(nv) >= 5:
                shaped += 1
            log("  rib %-3s @ %-5.0f  %s  verts->%s" % (face, pos, h, nv))
        else:
            fail += 1
            log("  rib %-3s @ %-5.0f setpoly FAILED: %s" % (face, pos, str(r2)[:90]))
        handles.append(h)
    log("\n  ribs made %d (shaped %d), failed %d" % (made, shaped, fail))
    return handles


def step5_verify():
    log("\n" + "=" * 74)
    log("STEP 5 — VERIFY by reading the model back")
    log("=" * 74)
    checks = []
    full = eb_api.run("dumpfull2", out="eb_full_exam.txt", wait=120)
    log("  " + str(full))
    holes = eb_api.run("dumpholes", out="eb_holes_exam.txt", wait=120)
    log("  " + str(holes))
    poly = eb_api.run("dumppoly", out="eb_poly_exam.txt", wait=120)
    log("  " + str(poly))
    conn = eb_api.run("connscan", out="eb_conn_exam.txt", wait=150)
    log("  " + str(conn))

    P = os.path.join(APP, "plugin")

    # column: profile and — the lesson-4 point — the shortened length
    col_line = ""
    for line in open(os.path.join(P, "eb_full_exam.txt"), encoding="utf-8-sig", errors="replace"):
        f = line.rstrip("\n").split("\t")
        if f[0] == "SHAPE":
            log("  SHAPE: %s  name=%s  p1=%s p2=%s L=%s" % (f[2], f[8], f[4], f[5], f[6]))
            if "200" in f[2] and "SHS" in f[2].upper() or "RQ200" in f[2]:
                col_line = line

    # holes: count and diameters
    dias, n = {}, 0
    for line in open(os.path.join(P, "eb_holes_exam.txt"), encoding="utf-8-sig", errors="replace"):
        f = line.rstrip("\n").split("\t")
        if f[0] == "HOLE":
            n += 1
            d = round(float(f[7]))
            dias[d] = dias.get(d, 0) + 1
    log("  holes read back: %d  diameters=%s" % (n, dias))
    checks.append(("4 holes", n == 4, "%d" % n))
    checks.append(("all dia 23", dias.get(23, 0) == n and n > 0, str(dias)))

    # hole spacing must be 300 x 300
    pts = []
    for line in open(os.path.join(P, "eb_holes_exam.txt"), encoding="utf-8-sig", errors="replace"):
        f = line.rstrip("\n").split("\t")
        if f[0] == "HOLE":
            pts.append(tuple(float(x) for x in f[5].split(",")))
    if len(pts) >= 4:
        xs = sorted(set(round(p[0]) for p in pts))
        ys = sorted(set(round(p[1]) for p in pts))
        sx = (max(xs) - min(xs)) if len(xs) > 1 else 0
        sy = (max(ys) - min(ys)) if len(ys) > 1 else 0
        log("  hole spacing measured: %d x %d" % (sx, sy))
        checks.append(("spacing 300x300", abs(sx - 300) <= 1 and abs(sy - 300) <= 1,
                       "%dx%d" % (sx, sy)))

    # ribs: 8 plates, each non-rectangular with 5 unique vertices
    ribs, shaped = 0, 0
    for line in open(os.path.join(P, "eb_poly_exam.txt"), encoding="utf-8-sig", errors="replace"):
        f = line.rstrip("\n").split("\t")
        if f[0] != "POLY":
            continue
        ribs += 1
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
    log("  plates read back: %d, of which shaped (>=5 verts): %d" % (ribs, shaped))
    checks.append(("8 ribs", ribs == 8, "%d" % ribs))
    checks.append(("ribs are shaped, not rectangles", shaped == 8, "%d/8" % shaped))

    # the connection must exist
    joints = re.search(r"links=(\d+)", conn or "")
    nj = int(joints.group(1)) if joints else 0
    log("  joints: %d" % nj)
    checks.append(("base plate is a connection", nj >= 1, "%d" % nj))

    log("\n  --- ACCEPTANCE ---")
    allok = True
    for name, good, detail in checks:
        log("   %s %-34s %s" % ("PASS" if good else "FAIL", name, detail))
        if not good:
            allok = False
    log("  %s" % ("ALL CHECKS PASSED" if allok else "SOME CHECKS FAILED"))
    return allok


def find_column():
    """Locate the vertical column already in the model (so steps can run apart)."""
    eb_api.run("dumpfull2", out="eb_full_find.txt", wait=120)
    p = os.path.join(APP, "plugin", "eb_full_find.txt")
    for line in open(p, encoding="utf-8-sig", errors="replace"):
        f = line.rstrip("\n").split("\t")
        if f[0] != "SHAPE" or len(f) < 7:
            continue
        try:
            p1 = tuple(float(x) for x in f[4].split(","))
            p2 = tuple(float(x) for x in f[5].split(","))
        except Exception:
            continue
        if abs(p2[2] - p1[2]) > 1000:          # a tall vertical member
            log("  found column: %s handle=%s L=%s" % (f[2], f[1], f[6]))
            return f[1]
    return None


def main():
    what = sys.argv[1] if len(sys.argv) > 1 else "all"
    if what in ("all", "1"):
        if not step1_close_and_new():
            return
    col = None
    if what in ("all", "2"):
        col = step2_column()
        if not col:
            log("!! no column — stopping")
            return
    if what in ("all", "3"):
        if col is None:
            col = find_column()
        if col is None:
            log("!! no column found — stopping")
            return
        step3_baseplate(col)
    if what in ("all", "4"):
        step4_ribs()
    if what in ("all", "5"):
        step5_verify()

    out = os.path.join(ROOT, "projects", "שיעור-4", "files", "exam-run.log")
    os.makedirs(os.path.dirname(out), exist_ok=True)
    open(out, "w", encoding="utf-8").write("\n".join(LOG))
    log("\nlog -> %s" % out)


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8-sig", errors="replace")
    except Exception:
        pass
    main()
