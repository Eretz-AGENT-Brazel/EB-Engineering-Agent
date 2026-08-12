# -*- coding: utf-8 -*-
"""
exam5_fix_anchors.py — correct every anchor bolt in the detail.

Two faults, both mine:

 1. LENGTH. I passed anchorgrip=400 and anchorkey=36 to the connection, so the
    anchors came out 428 long with an SW36 nut. Amir's come out 157 long with an
    SW30 nut because he changes nothing the software already knows. Rule: from a
    template, change ONLY what the drawing dictates.

 2. POSITION. I centred each wall anchor on the hole centre, so a 428-long bolt
    stuck out 204mm past the plate, into thin air. An anchor's head sits ON the
    outer face of the plate and its body runs INTO the concrete.

Fix: rebuild the base-plate connection with default anchors, then place the wall
anchors so the head lands on the plate face.
"""
import os
import re
import sys
import time

APP = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(APP)
PLUG = os.path.join(APP, "plugin")
sys.path.insert(0, APP)
import eb_api  # noqa: E402

FACE_X = 1082.792          # concrete face of column 1 (+X face)
WP_T = 20.0
PLATE_OUT = FACE_X + WP_T  # 1102.792 — outer face of the wall plate
FP_TOP = 50.0              # top of the base plate
FP, FP_T = 300.0, 20.0
HX, HY = 156.0, 162.0
HOLE, ADIA = 28.0, 24.0
CX, CY = 1252.792, -8780.847
LOG = []


def log(m):
    LOG.append(m)
    try:
        sys.stdout.write(m + "\n"); sys.stdout.flush()
    except Exception:
        pass


def ok(r):
    return isinstance(r, str) and r.startswith("EB_OK")


def bodies():
    for attempt in range(6):
        try:
            out = []
            for e in eb_api._app().ActiveDocument.ModelSpace:
                try:
                    if e.ObjectName != "Ks_VolBody":
                        continue
                    mn, mx = e.GetBoundingBox()
                    sz = (mx[0]-mn[0], mx[1]-mn[1], mx[2]-mn[2])
                    if not (30 < max(sz) < 1500):
                        continue
                    out.append({"h": e.Handle, "sz": sz,
                                "c": ((mn[0]+mx[0])/2, (mn[1]+mx[1])/2, (mn[2]+mx[2])/2),
                                "mn": mn, "mx": mx, "axis": sz.index(max(sz))})
                except Exception:
                    pass
            return out
        except Exception:
            time.sleep(2.5)
    return []


def column():
    eb_api.run("dumpfull2", out="eb_fa.txt", wait=200)
    for line in open(os.path.join(PLUG, "eb_fa.txt"), encoding="utf-8-sig", errors="replace"):
        f = line.rstrip("\n").split("\t")
        if f[0] == "SHAPE" and len(f) >= 7:
            try:
                if abs(float(f[5].split(",")[2]) - float(f[4].split(",")[2])) > 1000:
                    return f[1]
            except Exception:
                pass
    return None


def main():
    t0 = time.time()
    log("=" * 74)
    log("FIX ANCHORS — default length from the template, head on the plate face")
    log("=" * 74)

    # ---- 1. delete every anchor I made ----
    old = bodies()
    log("\n[1] deleting my %d anchors" % len(old))
    n = sum(1 for b in old if ok(eb_api.delete(b["h"])))
    log("    deleted %d" % n)

    # ---- 2. rebuild the base-plate connection, no invented anchor values ----
    col = column()
    log("\n[2] rebuilding the base plate — anchordia only, NO grip, NO key size")
    log("    " + str(eb_api.run("connremove", handle=col, delparts=1, wait=90))[:90])
    r = eb_api.run("connbase", handle=col, template="default/Standard",
                   l=FP, w=FP, t=FP_T, holedia=HOLE, hx=HX, hy=HY,
                   anchors=1, anchordia=ADIA, anchordetail=1, shorten=1, wait=90)
    log("    " + str(r)[:140])

    a = bodies()
    log("    anchors now: %d" % len(a))
    if a:
        s = a[0]
        log("    size %s  -> length %.0f (Amir's are 157)"
            % (tuple(round(v, 1) for v in s["sz"]), max(s["sz"])))

    # ---- 3. the two centre holes + their anchors ----
    eb_api.run("dumpfull2", out="eb_fa2.txt", wait=200)
    plate = None
    for line in open(os.path.join(PLUG, "eb_fa2.txt"), encoding="utf-8-sig", errors="replace"):
        f = line.rstrip("\n").split("\t")
        if f[0] == "SHAPE" and len(f) >= 7:
            try:
                z1 = float(f[4].split(",")[2]); z2 = float(f[5].split(",")[2])
            except Exception:
                continue
            if abs(z2-z1) < 5 and z1 < 100:
                plate = f[1]
    eb_api.run("dumpholes", out="eb_fa2h.txt", wait=200)
    ys = sorted(set(round(float(l.split("\t")[5].split(",")[1]), 1)
                    for l in open(os.path.join(PLUG, "eb_fa2h.txt"),
                                  encoding="utf-8-sig", errors="replace")
                    if l.startswith("HOLE") and l.split("\t")[1] == plate))
    log("\n[3] centre holes at x=%.1f, y=%s" % (CX, ys))
    src = bodies()[0]
    for y in ys:
        eb_api.run("drill", hosts=plate, at="%.3f,%.3f,%.3f" % (CX, y, FP_TOP),
                   dia=HOLE, n="0,0,1", wait=45, _log=False)
        eb_api.run("replicate",
                   box="%.2f,%.2f,%.2f,%.2f" % (src["c"][0]-40, src["c"][1]-40,
                                                src["c"][0]+40, src["c"][1]+40),
                   to="%.3f,%.3f,0" % (CX - src["c"][0], y - src["c"][1]),
                   wait=45, _log=False)
    log("    done — 6 floor holes, 6 anchors")

    # ---- 4. wall anchors: head ON the plate face ----
    fa = [b for b in bodies() if b["axis"] == 2]
    src = fa[0]
    L = max(src["sz"])
    log("\n[4] wall anchors — anchor length %.0f" % L)
    log("    plate outer face x=%.1f, so the bolt spans %.1f .. %.1f"
        % (PLATE_OUT, PLATE_OUT - L, PLATE_OUT))

    park = 70000.0
    eb_api.run("replicate",
               box="%.2f,%.2f,%.2f,%.2f" % (src["c"][0]-40, src["c"][1]-40,
                                            src["c"][0]+40, src["c"][1]+40),
               to="%.3f,%.3f,0" % (park - src["c"][0], -src["c"][1]),
               rot=90.0, axis="y", about="%.3f,0,%.3f" % (park, src["c"][2]),
               wait=60)
    hm = [b for b in bodies() if b["c"][0] > 60000]
    if not hm:
        log("    !! master failed"); return
    m = hm[0]
    log("    master %s size %s" % (m["h"], tuple(round(v, 1) for v in m["sz"])))
    # the master's own centre, and where its centre must land:
    tgt_cx = PLATE_OUT - L / 2.0
    log("    每 anchor centre must be x=%.1f" % tgt_cx)

    mb = (m["c"][0]-300, m["c"][1]-300, m["c"][0]+300, m["c"][1]+300)
    centres = [5600.0, 4100.0, 2600.0]
    W_HX, W_HZ = 452.0, 200.0
    cnt = 0
    for z in centres:
        for du in (-W_HX/2.0, W_HX/2.0):
            for dv in (-W_HZ, 0.0, W_HZ):
                if ok(eb_api.run("replicate", box="%.2f,%.2f,%.2f,%.2f" % mb,
                                 to="%.3f,%.3f,%.3f" % (tgt_cx - m["c"][0],
                                                        (CY + du) - m["c"][1],
                                                        (z + dv) - m["c"][2]),
                                 wait=45, _log=False)):
                    cnt += 1
    eb_api.delete(m["h"])
    log("    placed %d/18" % cnt)

    # ---- 5. verify ----
    log("\n[5] VERIFY")
    fin = bodies()
    lens = {}
    bad = 0
    for b in fin:
        lens[round(max(b["sz"]))] = lens.get(round(max(b["sz"])), 0) + 1
        if b["axis"] == 0:                       # horizontal
            if b["mx"][0] > PLATE_OUT + 1.0:
                bad += 1
    log("    anchors: %d   lengths: %s" % (len(fin), lens))
    log("    horizontal anchors sticking out past the plate: %d %s"
        % (bad, "OK" if bad == 0 else "STILL WRONG"))
    hz = [b for b in fin if b["axis"] == 0]
    if hz:
        b = hz[0]
        log("    sample: x %.1f .. %.1f  (concrete face %.1f, plate outer %.1f)"
            % (b["mn"][0], b["mx"][0], FACE_X, PLATE_OUT))
    vt = [b for b in fin if b["axis"] == 2]
    if vt:
        b = vt[0]
        log("    floor:  z %.1f .. %.1f  (slab top 30, plate top 50)"
            % (b["mn"][2], b["mx"][2]))
    log("    " + str(eb_api.run("dumpholes", out="eb_fa3.txt", wait=200)))

    for i in range(5):
        try:
            d = eb_api._app().ActiveDocument
            d.Save(); time.sleep(4); log("    saved"); break
        except Exception:
            time.sleep(3)
    log("\n%.1f min" % ((time.time()-t0)/60))
    open(os.path.join(ROOT, "projects", "שיעור-5", "files", "fix-anchors.log"),
         "w", encoding="utf-8").write("\n".join(LOG))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
