# -*- coding: utf-8 -*-
"""
exam5_plates.py — base plates (floor) + 3 wall plates per column, per the drawing.

FLOOR  300x300x20 : holes dia 28, 2 columns @156, 3 rows @81  (72|156|72, 69|81|81|69)
WALL   600x500x20 : holes dia 28, 2 columns @452, 3 rows @200 (74|452|74, 50|200|200|50)

The floor connection is built FROM A TEMPLATE — the lesson-5 finding: an empty
PsBaseplateLinkDataMgd has every field at 0, which is why anchors came out
invisible last time. A template carries the anchor diameter and nut size.
The middle row of holes the drawing shows is added with explicit drilling, since
the base-plate macro lays out a 2x2 field.
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
FP, FP_T = 300.0, 20.0
F_HX, F_HY = 156.0, 162.0
WP_W, WP_H, WP_T = 600.0, 500.0, 20.0
W_HX, W_HZ = 452.0, 200.0
HOLE = 28.0
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


def main():
    t0 = time.time()
    st = json.load(open(ST, encoding="utf-8"))
    cols = st["columns"]
    centres = st["centres"]
    log("=" * 74)
    log("EXAM 5 — base plates and wall plates")
    log("=" * 74)

    # ---------- 1. floor base plates, from a template ----------
    log("\n[1] FLOOR base plates 300x300x20, holes dia 28 @156 x 162, from template")
    made = 0
    for c in cols:
        r = eb_api.run("connbase", handle=c["h"], template="default/Standard",
                       l=FP, w=FP, t=FP_T, holedia=HOLE, hx=F_HX, hy=F_HY,
                       anchors=1, anchordia=20, anchorkey=30, anchorgrip=400,
                       anchordetail=1, shorten=1, wait=60)
        ab = re.search(r"anchors_with_body=(\d+)", r or "")
        log("   col %s -> added=%s anchors=%s" %
            (c["h"], (re.search(r"added=(\d+)", r or "") or ["", "?"])[1] if ok(r) else "FAIL",
             ab.group(1) if ab else "?"))
        if ok(r):
            made += 1
    log("   base plates: %d/%d" % (made, len(cols)))

    # ---------- 2. wall plates ----------
    log("\n[2] WALL plates 600x500x20, 3 per column, holes dia 28 @452 x 200")
    wp = 0
    holes = 0
    for c in cols:
        face = c["face"]
        coord = c["coord"]
        along = c["along"]
        # plate centre sits ON the concrete face, 20 thick, projecting outward
        for z in centres:
            if face == "+X":
                ctr = (coord + WP_T / 2.0, along, z)
                ex, ey, ez = "0,1,0", "0,0,1", "1,0,0"
                nrm = "1,0,0"
            elif face == "-X":
                ctr = (coord - WP_T / 2.0, along, z)
                ex, ey, ez = "0,1,0", "0,0,1", "1,0,0"
                nrm = "1,0,0"
            elif face == "+Y":
                ctr = (along, coord + WP_T / 2.0, z)
                ex, ey, ez = "1,0,0", "0,0,1", "0,1,0"
                nrm = "0,1,0"
            else:
                ctr = (along, coord - WP_T / 2.0, z)
                ex, ey, ez = "1,0,0", "0,0,1", "0,1,0"
                nrm = "0,1,0"

            r = eb_api.run("plate", center="%.3f,%.3f,%.3f" % ctr,
                           l=WP_W, w=WP_H, t=WP_T, ex=ex, ey=ey, ez=ez,
                           layer="PS_Plate", wait=40)
            h = H(r)
            if not (ok(r) and h):
                log("   plate FAILED at z=%.0f: %s" % (z, str(r)[:90]))
                continue
            wp += 1
            # 6 holes: 2 columns @452 across, 3 rows @200 up
            for du in (-W_HX / 2.0, W_HX / 2.0):
                for dv in (-W_HZ, 0.0, W_HZ):
                    if face in ("+X", "-X"):
                        at = (ctr[0], ctr[1] + du, ctr[2] + dv)
                    else:
                        at = (ctr[0] + du, ctr[1], ctr[2] + dv)
                    rd = eb_api.run("drill", hosts=h, at="%.3f,%.3f,%.3f" % at,
                                    dia=HOLE, n=nrm, wait=30)
                    if ok(rd):
                        holes += 1
        log("   col %s : plates so far %d, holes %d" % (c["h"], wp, holes))

    log("\n   wall plates %d (expect %d) | wall holes %d (expect %d)"
        % (wp, len(cols) * 3, holes, len(cols) * 18))
    log("\nelapsed %.1f min" % ((time.time() - t0) / 60))
    out = os.path.join(ROOT, "projects", "שיעור-5", "files", "exam5-plates.log")
    open(out, "w", encoding="utf-8").write("\n".join(LOG))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
