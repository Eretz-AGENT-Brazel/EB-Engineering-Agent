# -*- coding: utf-8 -*-
"""
exam5_detail_final.py — rebuild the whole detail with anchors that match Amir's.

Measured, not guessed:
    anchor body length = AnchorBoltDrillLength + 22.5
      drillLen 185 (template default) -> 208
      drillLen 137                    -> 159.5
      drillLen 100                    -> 122.5
    => for Amir's 157mm anchors: drillLen = 134.5
    nut across corners: 34.6 = SW30 -> anchorkey = 30  (template gave SW25)
    anchor diameter: Amir's is 20

Wall anchors are positioned so the HEAD SITS ON THE OUTER FACE OF THE PLATE and
the body runs into the concrete — not centred on the hole, which left them
hanging in mid air.
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

FACE_X, WP_T = 1082.792, 20.0
PLATE_OUT = FACE_X + WP_T
CX, CY = 1252.792, -8780.847
FP, FP_T = 300.0, 20.0
HX, HY, HOLE = 156.0, 162.0, 28.0
W_HX, W_HZ = 452.0, 200.0
CENTRES = [5600.0, 4100.0, 2600.0]
A_DIA, A_KEY, A_DRILL = 20.0, 30.0, 134.5      # -> 157mm anchors with an SW30 nut
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
    for _ in range(6):
        try:
            out = []
            for e in eb_api._app().ActiveDocument.ModelSpace:
                try:
                    if e.ObjectName != "Ks_VolBody":
                        continue
                    mn, mx = e.GetBoundingBox()
                    sz = (mx[0]-mn[0], mx[1]-mn[1], mx[2]-mn[2])
                    if 20 < max(sz) < 1500:
                        out.append({"h": e.Handle, "sz": sz, "mn": mn, "mx": mx,
                                    "c": ((mn[0]+mx[0])/2, (mn[1]+mx[1])/2,
                                          (mn[2]+mx[2])/2),
                                    "axis": sz.index(max(sz))})
                except Exception:
                    pass
            return out
        except Exception:
            time.sleep(2.5)
    return []


def column():
    eb_api.run("dumpfull2", out="eb_df.txt", wait=200)
    for line in open(os.path.join(PLUG, "eb_df.txt"), encoding="utf-8-sig", errors="replace"):
        f = line.rstrip("\n").split("\t")
        if f[0] == "SHAPE" and len(f) >= 7:
            try:
                if abs(float(f[5].split(",")[2]) - float(f[4].split(",")[2])) > 1000:
                    return f[1]
            except Exception:
                pass
    return None


def base_plate():
    eb_api.run("dumpfull2", out="eb_df2.txt", wait=200)
    for line in open(os.path.join(PLUG, "eb_df2.txt"), encoding="utf-8-sig", errors="replace"):
        f = line.rstrip("\n").split("\t")
        if f[0] == "SHAPE" and len(f) >= 7:
            try:
                z1 = float(f[4].split(",")[2]); z2 = float(f[5].split(",")[2])
            except Exception:
                continue
            if abs(z2 - z1) < 5 and z1 < 100:
                return f[1], z1
    return None, 0


def main():
    t0 = time.time()
    log("=" * 74)
    log("DETAIL — anchors matched to Amir's (157mm, SW30 nut), heads on the plate")
    log("=" * 74)

    for b in bodies():
        eb_api.delete(b["h"])
    col = column()
    log("column %s" % col)

    # ---- base plate with the measured anchor values ----
    log("\n[1] base plate + 4 corner anchors  (dia %g, key %g, drill %g)"
        % (A_DIA, A_KEY, A_DRILL))
    eb_api.run("connremove", handle=col, delparts=1, wait=90, _log=False)
    r = eb_api.run("connbase", handle=col, template="default/Standard",
                   l=FP, w=FP, t=FP_T, holedia=HOLE, hx=HX, hy=HY, anchors=1,
                   anchordia=A_DIA, anchorkey=A_KEY, anchordrill=A_DRILL,
                   shorten=1, wait=90)
    log("    " + str(r)[:130])
    a = bodies()
    if a:
        log("    anchor size %s -> length %.1f  (target 157)"
            % (tuple(round(v, 1) for v in a[0]["sz"]), max(a[0]["sz"])))

    # ---- the two centre holes of the drawing ----
    plate, pz = base_plate()
    eb_api.run("dumpholes", out="eb_dfh.txt", wait=200)
    ys = sorted(set(round(float(l.split("\t")[5].split(",")[1]), 1)
                    for l in open(os.path.join(PLUG, "eb_dfh.txt"),
                                  encoding="utf-8-sig", errors="replace")
                    if l.startswith("HOLE") and l.split("\t")[1] == plate))
    log("\n[2] the drawing's centre holes at x=%.1f, y=%s" % (CX, ys))
    src = bodies()[0]
    for y in ys:
        eb_api.run("drill", hosts=plate, at="%.3f,%.3f,%.3f" % (CX, y, pz + FP_T/2),
                   dia=HOLE, n="0,0,1", wait=45, _log=False)
        eb_api.run("replicate",
                   box="%.2f,%.2f,%.2f,%.2f" % (src["c"][0]-40, src["c"][1]-40,
                                                src["c"][0]+40, src["c"][1]+40),
                   to="%.3f,%.3f,0" % (CX - src["c"][0], y - src["c"][1]),
                   wait=45, _log=False)
    log("    6 floor holes, 6 anchors")

    # ---- wall anchors: head on the plate face ----
    fa = [b for b in bodies() if b["axis"] == 2]
    src = fa[0]
    L = max(src["sz"])
    tgt_cx = PLATE_OUT - L/2.0
    log("\n[3] wall anchors: length %.1f, head at x=%.1f, so centre x=%.1f"
        % (L, PLATE_OUT, tgt_cx))
    park = 70000.0
    eb_api.run("replicate",
               box="%.2f,%.2f,%.2f,%.2f" % (src["c"][0]-40, src["c"][1]-40,
                                            src["c"][0]+40, src["c"][1]+40),
               to="%.3f,%.3f,0" % (park - src["c"][0], -src["c"][1]),
               rot=90.0, axis="y", about="%.3f,0,%.3f" % (park, src["c"][2]),
               wait=60, _log=False)
    hm = [b for b in bodies() if b["c"][0] > 60000]
    if not hm:
        log("    !! master failed"); return
    m = hm[0]
    mb = (m["c"][0]-300, m["c"][1]-300, m["c"][0]+300, m["c"][1]+300)
    cnt = 0
    for z in CENTRES:
        for du in (-W_HX/2.0, W_HX/2.0):
            for dv in (-W_HZ, 0.0, W_HZ):
                if ok(eb_api.run("replicate", box="%.2f,%.2f,%.2f,%.2f" % mb,
                                 to="%.3f,%.3f,%.3f" % (tgt_cx - m["c"][0],
                                                        (CY+du) - m["c"][1],
                                                        (z+dv) - m["c"][2]),
                                 wait=45, _log=False)):
                    cnt += 1
    eb_api.delete(m["h"])
    log("    placed %d/18" % cnt)

    # ---- verify ----
    log("\n[4] VERIFY")
    fin = bodies()
    lens = {}
    stick = 0
    for b in fin:
        lens[round(max(b["sz"]))] = lens.get(round(max(b["sz"])), 0) + 1
        if b["axis"] == 0 and b["mx"][0] > PLATE_OUT + 1.0:
            stick += 1
    log("    anchors %d | lengths %s" % (len(fin), lens))
    log("    nut across corners: %.1f  (Amir's 34.6)"
        % sorted(fin[0]["sz"])[1] if fin else 0)
    log("    wall anchors past the plate: %d %s" % (stick, "OK" if stick == 0 else "WRONG"))
    hz = [b for b in fin if b["axis"] == 0]
    vt = [b for b in fin if b["axis"] == 2]
    if hz:
        log("    wall  x %.1f .. %.1f   (concrete face %.1f, plate outer %.1f)"
            % (hz[0]["mn"][0], hz[0]["mx"][0], FACE_X, PLATE_OUT))
    if vt:
        log("    floor z %.1f .. %.1f   (slab top 30, plate top 50)"
            % (vt[0]["mn"][2], vt[0]["mx"][2]))
    log("    " + str(eb_api.run("dumpholes", out="eb_dff.txt", wait=200)))
    for i in range(5):
        try:
            d = eb_api._app().ActiveDocument
            d.Save(); time.sleep(4); log("    saved"); break
        except Exception:
            time.sleep(3)
    log("\n%.1f min" % ((time.time()-t0)/60))
    open(os.path.join(ROOT, "projects", "שיעור-5", "files", "detail-final.log"),
         "w", encoding="utf-8").write("\n".join(LOG))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
