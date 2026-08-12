# -*- coding: utf-8 -*-
"""
exam5_details.py — the anchorage details, matching the drawing exactly.

FLOOR  300x300x20 : 6 holes dia 28 — 2 columns @156 across, 3 rows @81 up
                    (72|156|72 and 69|81|81|69). The macro lays the 4 corner
                    holes with their anchors; the middle row is drilled and
                    bolted explicitly so the plate matches the drawing.
WALL   600x500x20 : 6 holes dia 28 — 2 columns @452, 3 rows @200
                    (74|452|74 and 50|200|200|50), with an anchor in each.
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

ST = os.path.join(ROOT, "projects", "שיעור-5", "files", "exam5_state.json")
DEST = os.path.join(ROOT, "projects", "שיעור-5", "מבחן-שיעור-5.dwg")
FP, FP_T = 300.0, 20.0
F_HX, F_ROW = 156.0, 81.0
WP_W, WP_H, WP_T = 600.0, 500.0, 20.0
W_HX, W_HZ = 452.0, 200.0
HOLE, ANCHOR = 28.0, 24.0
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
    cols, centres, slab_top = st["columns"], st["centres"], st["slab_top"]
    log("=" * 76)
    log("EXAM 5 — anchorage details per drawing (%d columns)" % len(cols))
    log("=" * 76)

    # ---------- 1. floor base plates via the macro (from a template) ----------
    log("\n[1] floor plates %gx%gx%g, 4 corner holes @%g x %g + anchors"
        % (FP, FP, FP_T, F_HX, 2 * F_ROW))
    n = 0
    for c in cols:
        r = eb_api.run("connbase", handle=c["h"], template="default/Standard",
                       l=FP, w=FP, t=FP_T, holedia=HOLE,
                       hx=F_HX, hy=2 * F_ROW, anchors=1, anchordia=ANCHOR,
                       anchorkey=36, anchorgrip=400, anchordetail=1,
                       shorten=1, wait=60, _log=False)
        if ok(r):
            n += 1
        elif n < 3:
            log("   FAIL %s: %s" % (c["h"], str(r)[:90]))
    log("   base plates: %d/%d" % (n, len(cols)))

    # ---------- 2. the drawing's middle row: 2 more holes + 2 more bolts ----------
    log("\n[2] middle row of the floor plate (the drawing shows 3 rows, not 2)")
    eb_api.run("dumpfull2", out="eb_e5b.txt", wait=240)
    bp = []
    for line in open(os.path.join(PLUG, "eb_e5b.txt"), encoding="utf-8-sig", errors="replace"):
        f = line.rstrip("\n").split("\t")
        if f[0] != "SHAPE" or len(f) < 7:
            continue
        try:
            p1 = [float(x) for x in f[4].split(",")]
            p2 = [float(x) for x in f[5].split(",")]
        except Exception:
            continue
        if abs(p1[2] - (slab_top + FP_T / 2.0)) < 2.0 and abs(p2[2] - p1[2]) < 2.0:
            bp.append({"h": f[1], "cx": (p1[0] + p2[0]) / 2.0, "cy": (p1[1] + p2[1]) / 2.0,
                       "z": p1[2]})
    log("   base plates found: %d" % len(bp))
    dh = db = 0
    for p in bp:
        for dx in (-F_HX / 2.0, F_HX / 2.0):
            at = (p["cx"] + dx, p["cy"], p["z"])
            if ok(eb_api.run("drill", hosts=p["h"], at="%.3f,%.3f,%.3f" % at,
                             dia=HOLE, n="0,0,1", wait=30, _log=False)):
                dh += 1
            if ok(eb_api.run("bolt", p1="%.3f,%.3f,%.3f" % (at[0], at[1], slab_top - 350),
                             p2="%.3f,%.3f,%.3f" % (at[0], at[1], slab_top + FP_T + 60),
                             dia=ANCHOR, style="DIN6914", len=460, layer="PS_Bolt",
                             wait=30, _log=False)):
                db += 1
    log("   middle-row holes %d, bolts %d (expect %d each)" % (dh, db, len(bp) * 2))

    # ---------- 3. wall plates, 3 per column, 6 holes + 6 anchors each ----------
    log("\n[3] wall plates %gx%gx%g x3 per column, 6 holes @%g x %g + anchors"
        % (WP_W, WP_H, WP_T, W_HX, W_HZ))
    wp = wh = wb = 0
    for i, c in enumerate(cols, 1):
        face, coord, along = c["face"], c["coord"], c["along"]
        sgn = 1.0 if face in ("+X", "+Y") else -1.0
        horiz = face in ("+X", "-X")          # plate spans Y when facing X
        for z in centres:
            if horiz:
                ctr = (coord + sgn * WP_T / 2.0, along, z)
                ex, ey, ez, nrm = "0,1,0", "0,0,1", "1,0,0", "1,0,0"
            else:
                ctr = (along, coord + sgn * WP_T / 2.0, z)
                ex, ey, ez, nrm = "1,0,0", "0,0,1", "0,1,0", "0,1,0"
            r = eb_api.run("plate", center="%.3f,%.3f,%.3f" % ctr, l=WP_W, w=WP_H,
                           t=WP_T, ex=ex, ey=ey, ez=ez, layer="PS_Plate",
                           wait=40, _log=False)
            h = H(r)
            if not (ok(r) and h):
                continue
            wp += 1
            for du in (-W_HX / 2.0, W_HX / 2.0):
                for dv in (-W_HZ, 0.0, W_HZ):
                    at = (ctr[0], ctr[1] + du, ctr[2] + dv) if horiz \
                        else (ctr[0] + du, ctr[1], ctr[2] + dv)
                    if ok(eb_api.run("drill", hosts=h, at="%.3f,%.3f,%.3f" % at,
                                     dia=HOLE, n=nrm, wait=30, _log=False)):
                        wh += 1
                    if horiz:
                        a = (coord - sgn * 200.0, at[1], at[2])
                        b = (coord + sgn * (WP_T + 60.0), at[1], at[2])
                    else:
                        a = (at[0], coord - sgn * 200.0, at[2])
                        b = (at[0], coord + sgn * (WP_T + 60.0), at[2])
                    if ok(eb_api.run("bolt", p1="%.3f,%.3f,%.3f" % a,
                                     p2="%.3f,%.3f,%.3f" % b, dia=ANCHOR,
                                     style="DIN6914", len=280, layer="PS_Bolt",
                                     wait=30, _log=False)):
                        wb += 1
        if i % 5 == 0 or i == len(cols):
            log("   col %2d/%d : plates %d, holes %d, bolts %d  (%.1f min)"
                % (i, len(cols), wp, wh, wb, (time.time() - t0) / 60))
    log("   wall: plates %d/%d, holes %d/%d, bolts %d/%d"
        % (wp, len(cols) * 3, wh, len(cols) * 18, wb, len(cols) * 18))

    # ---------- 4. save ----------
    log("\n[4] save")
    try:
        app = eb_api._app()
        eb_api._send(app.ActiveDocument, "\x1b\x1b")
        time.sleep(0.8)
        app.ActiveDocument.SaveAs(DEST)
        time.sleep(6.0)
        log("   saved (%d bytes)" % os.path.getsize(DEST))
    except Exception as e:
        log("   save: %s" % str(e)[:90])

    log("\nelapsed %.1f min" % ((time.time() - t0) / 60))
    open(os.path.join(ROOT, "projects", "שיעור-5", "files", "exam5-details.log"),
         "w", encoding="utf-8").write("\n".join(LOG))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
