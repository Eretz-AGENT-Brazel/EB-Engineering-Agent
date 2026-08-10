# -*- coding: utf-8 -*-
"""
exam5_final.py — EXAM 5 retake. One detail, then replicate. API only, no LISP.

Rules as Amir stated them:
  * HEB 300 columns, 6000 long, one at EVERY inward concrete face
    (corner column = 2 faces, edge column = 3)  -> 20 columns
  * the HEB flange is ALWAYS parallel to the concrete column's face
    -> the 3 hole rows (@81) run parallel to the face,
       the 2 hole columns (@156) run perpendicular to it
  * floor plate 300x300x20, 6 holes dia 28 : 72|156|72 and 69|81|81|69
  * wall plate 600x500x20, 6 holes dia 28 : 74|452|74 and 50|200|200|50
  * 3 wall plates per column, 1500 pitch, top plate's top edge 200 below the
    top of the steel
  * anchor bolts modelled; horizontal ones are a vertical anchor rotated 90
  * build ONE detail, then copy/rotate it to the other 19 places
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
F_ACROSS = 156.0          # perpendicular to the face (across the web)
F_ALONG = 162.0           # parallel to the face (3 rows at 81)
WP_W, WP_H, WP_T = 600.0, 500.0, 20.0
W_HX, W_HZ = 452.0, 200.0
HOLE = 28.0
ANCHOR = 24.0
PITCH, TOP_GAP = 1500.0, 200.0
FACE_ANG = {"+X": 0.0, "+Y": 90.0, "-X": 180.0, "-Y": 270.0}
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


def ents(skip_concrete=True):
    out = []
    for e in eb_api._app().ActiveDocument.ModelSpace:
        try:
            nm = e.ObjectName
            if skip_concrete and nm in ("AcDb3dSolid", "PcRebarManager"):
                continue
            mn, mx = e.GetBoundingBox()
            out.append({"h": e.Handle, "cls": nm,
                        "c": ((mn[0]+mx[0])/2, (mn[1]+mx[1])/2, (mn[2]+mx[2])/2),
                        "sz": (mx[0]-mn[0], mx[1]-mn[1], mx[2]-mn[2])})
        except Exception:
            pass
    return out


def concrete():
    tall, flat = [], []
    for e in eb_api._app().ActiveDocument.ModelSpace:
        try:
            if e.ObjectName != "AcDb3dSolid":
                continue
            mn, mx = e.GetBoundingBox()
            d = {"min": mn, "max": mx, "sz": (mx[0]-mn[0], mx[1]-mn[1], mx[2]-mn[2])}
            (tall if d["sz"][2] > 1000 else flat).append(d)
        except Exception:
            pass
    return tall, flat


def plan_columns():
    tall, flat = concrete()
    slab = flat[0]
    sx0, sx1 = slab["min"][0], slab["max"][0]
    sy0, sy1 = slab["min"][1], slab["max"][1]
    tol = 5.0
    out = []
    for c in sorted(tall, key=lambda q: (q["min"][0], q["min"][1])):
        x0, x1, y0, y1 = c["min"][0], c["max"][0], c["min"][1], c["max"][1]
        mx, my = (x0+x1)/2.0, (y0+y1)/2.0
        faces = []
        if x1 < sx1 - tol: faces.append(("+X", x1, my))
        if x0 > sx0 + tol: faces.append(("-X", x0, my))
        if y1 < sy1 - tol: faces.append(("+Y", y1, mx))
        if y0 > sy0 + tol: faces.append(("-Y", y0, mx))
        for face, coord, along in faces:
            if face == "+X":
                sxx, syy = coord + WP_T + HEB/2.0, along
            elif face == "-X":
                sxx, syy = coord - WP_T - HEB/2.0, along
            elif face == "+Y":
                sxx, syy = along, coord + WP_T + HEB/2.0
            else:
                sxx, syy = along, coord - WP_T - HEB/2.0
            out.append({"face": face, "coord": coord, "along": along,
                        "x": sxx, "y": syy})
    return out, slab["max"][2]


def main():
    t0 = time.time()
    log("=" * 74)
    log("EXAM 5 RETAKE — one detail, then replicate (API only)")
    log("=" * 74)
    log(str(eb_api.run("whoami", wait=25)))

    plan, slab_top = plan_columns()
    z0 = slab_top
    z1 = z0 + FP_T + COL_LEN
    centres = [z1 - TOP_GAP - WP_H/2.0 - k*PITCH for k in range(3)]
    log("\n%d steel columns | slab top z=%.0f | steel z %.0f..%.0f (L=%.0f)"
        % (len(plan), slab_top, z0 + FP_T, z1, COL_LEN))
    log("wall-plate centres: %s" % ", ".join("%.0f" % v for v in centres))

    c1 = plan[0]
    log("\n--- DETAIL on column 1: face %s at (%.1f, %.1f) ---"
        % (c1["face"], c1["x"], c1["y"]))
    horiz = c1["face"] in ("+X", "-X")     # face normal along X -> plate spans Y
    sgn = 1.0 if c1["face"] in ("+X", "+Y") else -1.0

    # ---- 1. column, flange parallel to the concrete face ----
    # For a +X face the face plane is YZ, so the flange width must run along Y.
    rot = 0.0 if horiz else 90.0
    r = eb_api.run("beam", name=COL_NAME, catalog=COL_CAT,
                   p1="%.3f,%.3f,%.3f" % (c1["x"], c1["y"], z0),
                   p2="%.3f,%.3f,%.3f" % (c1["x"], c1["y"], z1),
                   rot=rot, layer="PS_Shape", wait=40)
    col = H(r)
    log("  column %s (rot=%.0f) %s" % (col, rot, "OK" if ok(r) else str(r)[:70]))
    if not col:
        return
    # verify the flange direction from the real extents
    ce = [s for s in ents() if s["h"] == col]
    if ce:
        sz = ce[0]["sz"]
        log("  column bbox %.0f x %.0f  -> flange along %s"
            % (sz[0], sz[1], "Y" if sz[1] >= sz[0] else "X"))

    # ---- 2. base plate as a connection, from a template ----
    hx = F_ACROSS if horiz else F_ALONG    # X spacing in model terms
    hy = F_ALONG if horiz else F_ACROSS
    r = eb_api.run("connbase", handle=col, template="default/Standard",
                   l=FP, w=FP, t=FP_T, holedia=HOLE, hx=hx, hy=hy,
                   anchors=1, anchordia=ANCHOR, anchorkey=36, anchorgrip=400,
                   anchordetail=1, shorten=1, wait=60)
    log("  base plate: %s" % str(r)[:130])

    # ---- 3. the drawing's middle row: 2 holes + 2 anchors ----
    a = [s for s in ents() if s["cls"] == "Ks_VolBody" and 50 < max(s["sz"]) < 1000]
    log("  anchors from the macro: %d" % len(a))
    src = a[0] if a else None
    bp = [s for s in ents() if s["cls"] == "Ks_Shape"
          and abs(s["c"][2] - (z0 + FP_T/2.0)) < 2.0]
    if bp and src:
        p = bp[0]
        for d in (-F_ACROSS/2.0, F_ACROSS/2.0):
            at = (p["c"][0] + (d if horiz else 0.0),
                  p["c"][1] + (0.0 if horiz else d), z0 + FP_T/2.0)
            eb_api.run("drill", hosts=p["h"], at="%.3f,%.3f,%.3f" % at,
                       dia=HOLE, n="0,0,1", wait=30, _log=False)
            eb_api.run("replicate",
                       box="%.1f,%.1f,%.1f,%.1f" % (src["c"][0]-40, src["c"][1]-40,
                                                    src["c"][0]+40, src["c"][1]+40),
                       to="%.3f,%.3f,0" % (at[0]-src["c"][0], at[1]-src["c"][1]),
                       wait=40, _log=False)
        log("  middle row: 2 holes + 2 anchors added")

    # ---- 4. three wall plates, 6 holes each ----
    made = holes = 0
    for z in centres:
        if horiz:
            ctr = (c1["coord"] + sgn*WP_T/2.0, c1["along"], z)
            ex, ey, ez, nrm = "0,1,0", "0,0,1", "1,0,0", "1,0,0"
        else:
            ctr = (c1["along"], c1["coord"] + sgn*WP_T/2.0, z)
            ex, ey, ez, nrm = "1,0,0", "0,0,1", "0,1,0", "0,1,0"
        r = eb_api.run("plate", center="%.3f,%.3f,%.3f" % ctr, l=WP_W, w=WP_H,
                       t=WP_T, ex=ex, ey=ey, ez=ez, layer="PS_Plate",
                       wait=40, _log=False)
        h = H(r)
        if not h:
            continue
        made += 1
        for du in (-W_HX/2.0, W_HX/2.0):
            for dv in (-W_HZ, 0.0, W_HZ):
                at = (ctr[0], ctr[1]+du, ctr[2]+dv) if horiz \
                    else (ctr[0]+du, ctr[1], ctr[2]+dv)
                if ok(eb_api.run("drill", hosts=h, at="%.3f,%.3f,%.3f" % at,
                                 dia=HOLE, n=nrm, wait=30, _log=False)):
                    holes += 1
    log("  wall plates %d, holes %d (%.1f min)" % (made, holes, (time.time()-t0)/60))

    # ---- 5. horizontal anchor: rotate a vertical one 90 about Y ----
    park = 70000.0
    eb_api.run("replicate",
               box="%.1f,%.1f,%.1f,%.1f" % (src["c"][0]-40, src["c"][1]-40,
                                            src["c"][0]+40, src["c"][1]+40),
               to="%.3f,%.3f,0" % (park - src["c"][0], -src["c"][1]),
               rot=90.0, axis="y", about="%.3f,0,%.3f" % (park, src["c"][2]),
               wait=45)
    hm = [s for s in ents() if s["cls"] == "Ks_VolBody" and s["c"][0] > 60000]
    if not hm:
        log("  !! horizontal master failed"); return
    m = hm[0]
    log("  horizontal master %s size %s" % (m["h"], tuple(round(v, 1) for v in m["sz"])))
    mb = (m["c"][0]-300, m["c"][1]-300, m["c"][0]+300, m["c"][1]+300)
    wa = 0
    for z in centres:
        for du in (-W_HX/2.0, W_HX/2.0):
            for dv in (-W_HZ, 0.0, W_HZ):
                tgt = (c1["coord"] + sgn*WP_T/2.0, c1["along"]+du, z+dv) if horiz \
                    else (c1["along"]+du, c1["coord"] + sgn*WP_T/2.0, z+dv)
                if ok(eb_api.run("replicate", box="%.1f,%.1f,%.1f,%.1f" % mb,
                                 to="%.3f,%.3f,%.3f" % (tgt[0]-m["c"][0],
                                                        tgt[1]-m["c"][1],
                                                        tgt[2]-m["c"][2]),
                                 wait=40, _log=False)):
                    wa += 1
    eb_api.delete(m["h"])
    log("  wall anchors: %d/18 (%.1f min)" % (wa, (time.time()-t0)/60))

    # ---- 6. freeze the detail as a fixed handle list ----
    detail = [s["h"] for s in ents()
              if abs(s["c"][0]-c1["x"]) <= 420 and abs(s["c"][1]-c1["y"]) <= 420]
    log("\n--- DETAIL = %d objects; replicating to %d places ---"
        % (len(detail), len(plan)-1))
    json.dump({"detail": detail, "plan": plan, "centres": centres,
               "z0": z0, "z1": z1},
              open(os.path.join(ROOT, "projects", "שיעור-5", "files",
                                "exam5_final_state.json"), "w", encoding="utf-8"))

    box = (c1["x"]-420, c1["y"]-420, c1["x"]+420, c1["y"]+420)
    base = FACE_ANG[c1["face"]]
    done = 0
    for c in plan[1:]:
        ang = FACE_ANG[c["face"]] - base
        r = eb_api.run("replicate", box="%.2f,%.2f,%.2f,%.2f" % box,
                       to="%.3f,%.3f,0" % (c["x"]-c1["x"], c["y"]-c1["y"]),
                       rot=ang, axis="z", about="%.3f,%.3f,0" % (c["x"], c["y"]),
                       wait=90, _log=False)
        if ok(r):
            done += 1
        log("   %2d/%d (%7.0f,%8.0f) %s rot%+4.0f  %s"
            % (done, len(plan)-1, c["x"], c["y"], c["face"], ang, str(r)[:52]))

    log("\n--- VERIFY (%.1f min) ---" % ((time.time()-t0)/60))
    log("  " + str(eb_api.run("dumpfull2", out="eb_f5.txt", wait=300)))
    log("  " + str(eb_api.run("dumpholes", out="eb_f5h.txt", wait=300)))
    al = ents()
    anc = [s for s in al if s["cls"] == "Ks_VolBody" and 50 < max(s["sz"]) < 1000]
    log("  anchors: %d (expect %d)" % (len(anc), len(plan)*24))
    try:
        d = eb_api._app().ActiveDocument
        d.Save()
        time.sleep(5)
        log("  saved (%d bytes)" % os.path.getsize(DEST))
    except Exception as e:
        log("  save: %s" % str(e)[:70])
    log("\nTOTAL %.1f min" % ((time.time()-t0)/60))
    open(os.path.join(ROOT, "projects", "שיעור-5", "files", "exam5-final.log"),
         "w", encoding="utf-8").write("\n".join(LOG))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
