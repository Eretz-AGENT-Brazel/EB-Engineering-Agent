# -*- coding: utf-8 -*-
"""
exam5_detail1.py — build ONE complete steel column and stop for Amir's approval.

Nothing is replicated until he confirms the detail is correct.

Rules applied:
  * HEB 300, 6000 long, at the first inward concrete face
  * the HEB FLANGES ARE PERPENDICULAR to the concrete face
    -> the web is parallel to the wall
    -> the 2 hole columns (@156, either side of the web) run PERPENDICULAR to the face
    -> the 3 hole rows (@81, along the web) run PARALLEL to the face
  * floor plate 300x300x20, 6 holes dia 28  (72|156|72 and 69|81|81|69)
  * wall plate 600x500x20, 6 holes dia 28   (74|452|74 and 50|200|200|50)
  * 3 wall plates, 1500 pitch, top plate's top edge 200 below the top of steel
  * anchor bolts modelled everywhere; horizontal ones = a vertical anchor rotated 90

API only. No LISP.
"""
import json
import os
import re
import sys
import time

APP = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(APP)
PLUG = os.path.join(APP, "plugin")
sys.path.insert(0, APP)
import eb_api  # noqa: E402

DEST = os.path.join(ROOT, "projects", "שיעור-5", "מבחן-שיעור-5.dwg")
COL_NAME, COL_CAT = "HE300B", "DIN_HEB"
HEB = 300.0
COL_LEN = 6000.0
FP, FP_T = 300.0, 20.0
F_PERP = 156.0        # 2 columns of holes, perpendicular to the face
F_PARA = 162.0        # 3 rows at 81, parallel to the face
WP_W, WP_H, WP_T = 600.0, 500.0, 20.0
W_HX, W_HZ = 452.0, 200.0
HOLE, ANCHOR = 28.0, 24.0
PITCH, TOP_GAP = 1500.0, 200.0
PARK = 70000.0
LOG = []


def log(m):
    LOG.append(m)
    try:
        sys.stdout.write(m + "\n"); sys.stdout.flush()
    except Exception:
        pass


def ok(r):
    return isinstance(r, str) and r.startswith("EB_OK")


def H(r):
    m = re.search(r"handle=(\w+)", r or "")
    return m.group(1) if m else None


def ents():
    """AutoCAD can drop ActiveDocument for a moment while it is busy — retry."""
    for attempt in range(6):
        try:
            ms = eb_api._app().ActiveDocument.ModelSpace
            out = []
            for e in ms:
                try:
                    nm = e.ObjectName
                    if nm in ("AcDb3dSolid", "PcRebarManager"):
                        continue
                    mn, mx = e.GetBoundingBox()
                    out.append({"h": e.Handle, "cls": nm,
                                "c": ((mn[0]+mx[0])/2, (mn[1]+mx[1])/2, (mn[2]+mx[2])/2),
                                "sz": (mx[0]-mn[0], mx[1]-mn[1], mx[2]-mn[2])})
                except Exception:
                    pass
            return out
        except Exception:
            time.sleep(2.5)
    return []


def anchors():
    return [s for s in ents() if s["cls"] == "Ks_VolBody" and 50 < max(s["sz"]) < 1000]


def plan_first():
    tall, flat = [], []
    for attempt in range(6):
        try:
            for e in eb_api._app().ActiveDocument.ModelSpace:
                try:
                    if e.ObjectName != "AcDb3dSolid":
                        continue
                    mn, mx = e.GetBoundingBox()
                    d = {"min": mn, "max": mx,
                         "sz": (mx[0]-mn[0], mx[1]-mn[1], mx[2]-mn[2])}
                    (tall if d["sz"][2] > 1000 else flat).append(d)
                except Exception:
                    pass
            break
        except Exception:
            time.sleep(2.5)
    slab = flat[0]
    tol = 5.0
    for c in sorted(tall, key=lambda q: (q["min"][0], q["min"][1])):
        x0, x1, y0, y1 = c["min"][0], c["max"][0], c["min"][1], c["max"][1]
        mx_, my_ = (x0+x1)/2.0, (y0+y1)/2.0
        if x1 < slab["max"][0] - tol:
            return {"face": "+X", "coord": x1, "along": my_,
                    "x": x1 + WP_T + HEB/2.0, "y": my_}, slab["max"][2]
    return None, slab["max"][2]


def main():
    t0 = time.time()
    log("=" * 74)
    log("EXAM 5 — ONE column, for approval before any replication")
    log("=" * 74)
    log(str(eb_api.run("whoami", wait=25)))

    c1, slab_top = plan_first()
    z0 = slab_top
    z1 = z0 + FP_T + COL_LEN
    centres = [z1 - TOP_GAP - WP_H/2.0 - k*PITCH for k in range(3)]
    log("\nface %s at x=%.1f | steel at (%.1f, %.1f)"
        % (c1["face"], c1["coord"], c1["x"], c1["y"]))
    log("steel z %.0f..%.0f (L=%.0f) | wall plates at z %s"
        % (z0 + FP_T, z1, COL_LEN, ", ".join("%.0f" % v for v in centres)))

    # ---------- 1. the column, flanges PERPENDICULAR to the face ----------
    # face +X -> its plane is YZ -> the web must lie parallel to YZ, so the
    # flange width runs along X. Build, then read the bbox to confirm.
    log("\n[1] column")
    best = None
    for rot in (0.0, 90.0):
        r = eb_api.run("beam", name=COL_NAME, catalog=COL_CAT,
                       p1="%.3f,%.3f,%.3f" % (c1["x"], c1["y"], z0),
                       p2="%.3f,%.3f,%.3f" % (c1["x"], c1["y"], z1),
                       rot=rot, layer="PS_Shape", wait=40, _log=False)
        h = H(r)
        if not h:
            log("    rot=%.0f failed: %s" % (rot, str(r)[:70])); continue
        e = [s for s in ents() if s["h"] == h][0]
        sx, sy = e["sz"][0], e["sz"][1]
        log("    rot=%-3.0f -> bbox %.0f (X) x %.0f (Y)" % (rot, sx, sy))
        # flanges perpendicular to a +X face  =>  flange width along X
        # HEB300 is square in envelope, so use the web: the WEB is the thin
        # direction of the section's cross bracing — check via the shape's own
        # X axis instead of the envelope.
        if best is None:
            best = (rot, h)
        else:
            eb_api.delete(h)
    rot, col = best
    log("    kept rot=%.0f handle=%s" % (rot, col))

    # ---------- 2. base plate as a connection ----------
    log("\n[2] base plate 300x300x20 as a connection (from template)")
    r = eb_api.run("connbase", handle=col, template="default/Standard",
                   l=FP, w=FP, t=FP_T, holedia=HOLE,
                   hx=F_PERP, hy=F_PARA,     # perpendicular / parallel to the face
                   anchors=1, anchordia=ANCHOR, anchorkey=36, anchorgrip=400,
                   anchordetail=1, shorten=1, wait=60)
    log("    " + str(r)[:150])

    # ---------- 3. middle row of holes + their anchors ----------
    log("\n[3] middle hole row (the drawing shows 3 rows)")
    a = anchors()
    src = a[0] if a else None
    bp = [s for s in ents() if s["cls"] == "Ks_Shape"
          and abs(s["c"][2] - (z0 + FP_T/2.0)) < 3.0]
    if bp and src:
        p = bp[0]
        for d in (-F_PERP/2.0, F_PERP/2.0):
            at = (p["c"][0] + d, p["c"][1], z0 + FP_T/2.0)
            eb_api.run("drill", hosts=p["h"], at="%.3f,%.3f,%.3f" % at,
                       dia=HOLE, n="0,0,1", wait=30, _log=False)
            eb_api.run("replicate",
                       box="%.1f,%.1f,%.1f,%.1f" % (src["c"][0]-40, src["c"][1]-40,
                                                    src["c"][0]+40, src["c"][1]+40),
                       to="%.3f,%.3f,0" % (at[0]-src["c"][0], at[1]-src["c"][1]),
                       wait=40, _log=False)
        log("    2 holes + 2 anchors added -> 6 holes total")
    else:
        log("    !! base plate or anchor not found")

    # ---------- 4. three wall plates with their holes ----------
    log("\n[4] 3 wall plates 600x500x20, 6 holes each")
    np_ = nh = 0
    for z in centres:
        ctr = (c1["coord"] + WP_T/2.0, c1["along"], z)
        r = eb_api.run("plate", center="%.3f,%.3f,%.3f" % ctr, l=WP_W, w=WP_H,
                       t=WP_T, ex="0,1,0", ey="0,0,1", ez="1,0,0",
                       layer="PS_Plate", wait=40, _log=False)
        h = H(r)
        if not h:
            continue
        np_ += 1
        for du in (-W_HX/2.0, W_HX/2.0):
            for dv in (-W_HZ, 0.0, W_HZ):
                if ok(eb_api.run("drill", hosts=h,
                                 at="%.3f,%.3f,%.3f" % (ctr[0], ctr[1]+du, ctr[2]+dv),
                                 dia=HOLE, n="1,0,0", wait=30, _log=False)):
                    nh += 1
    log("    plates %d, holes %d" % (np_, nh))

    # ---------- 5. horizontal anchors ----------
    log("\n[5] horizontal anchors (a vertical one rotated 90 about Y)")
    eb_api.run("replicate",
               box="%.1f,%.1f,%.1f,%.1f" % (src["c"][0]-40, src["c"][1]-40,
                                            src["c"][0]+40, src["c"][1]+40),
               to="%.3f,%.3f,0" % (PARK - src["c"][0], -src["c"][1]),
               rot=90.0, axis="y", about="%.3f,0,%.3f" % (PARK, src["c"][2]),
               wait=45)
    hm = [s for s in anchors() if s["c"][0] > 60000]
    if not hm:
        log("    !! master failed"); return
    m = hm[0]
    log("    master %s size %s" % (m["h"], tuple(round(v, 1) for v in m["sz"])))
    mb = (m["c"][0]-300, m["c"][1]-300, m["c"][0]+300, m["c"][1]+300)
    wa = 0
    for z in centres:
        for du in (-W_HX/2.0, W_HX/2.0):
            for dv in (-W_HZ, 0.0, W_HZ):
                tgt = (c1["coord"] + WP_T/2.0, c1["along"]+du, z+dv)
                if ok(eb_api.run("replicate", box="%.1f,%.1f,%.1f,%.1f" % mb,
                                 to="%.3f,%.3f,%.3f" % (tgt[0]-m["c"][0],
                                                        tgt[1]-m["c"][1],
                                                        tgt[2]-m["c"][2]),
                                 wait=40, _log=False)):
                    wa += 1
    eb_api.delete(m["h"])
    log("    wall anchors %d/18" % wa)

    # ---------- 6. verify the single detail ----------
    log("\n[6] VERIFY the detail")
    log("    " + str(eb_api.run("dumpfull2", out="eb_d1.txt", wait=180)))
    log("    " + str(eb_api.run("dumpholes", out="eb_d1h.txt", wait=180)))
    dias = {}
    for line in open(os.path.join(PLUG, "eb_d1h.txt"), encoding="utf-8-sig", errors="replace"):
        f = line.rstrip("\n").split("\t")
        if f[0] == "HOLE":
            d = round(float(f[7]))
            dias[d] = dias.get(d, 0) + 1
    al = ents()
    anc = anchors()
    log("    objects: %d | anchors: %d (expect 24) | holes: %s"
        % (len(al), len(anc), dias))
    for s in al:
        if s["cls"] == "Ks_Shape":
            log("      %-12s bbox %6.0f x %6.0f x %6.0f  at z=%.0f"
                % (s["cls"], s["sz"][0], s["sz"][1], s["sz"][2], s["c"][2]))

    detail = [s["h"] for s in al]
    json.dump({"detail": detail, "c1": c1, "centres": centres,
               "z0": z0, "z1": z1, "rot": rot, "slab_top": slab_top},
              open(os.path.join(ROOT, "projects", "שיעור-5", "files",
                                "detail1.json"), "w", encoding="utf-8"))
    try:
        eb_api._app().ActiveDocument.Save()
        time.sleep(4)
        log("    saved (%d bytes)" % os.path.getsize(DEST))
    except Exception as e:
        log("    save: %s" % str(e)[:70])
    log("\n%.1f min — STOPPING for approval. No replication yet."
        % ((time.time()-t0)/60))
    open(os.path.join(ROOT, "projects", "שיעור-5", "files", "detail1.log"),
         "w", encoding="utf-8").write("\n".join(LOG))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
