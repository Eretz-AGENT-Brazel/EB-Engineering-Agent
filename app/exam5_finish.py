# -*- coding: utf-8 -*-
"""
exam5_finish.py — complete the detail and verify.

Two things the macro does not do for us:
  * the drawing's floor plate has SIX holes (2 columns x 3 rows @81); the
    base-plate macro lays a 2x2 field, so the middle row is drilled explicitly.
  * the wall plates need their anchor bolts (Amir's method: an ordinary plate
    with the anchor bolts copied onto it).
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
HOLE = 28.0
F_HX = 156.0
WP_T = 20.0
W_HX, W_HZ = 452.0, 200.0
LOG = []


def log(m):
    LOG.append(m)
    try:
        sys.stdout.write(m + "\n"); sys.stdout.flush()
    except Exception:
        pass


def ok(r):
    return isinstance(r, str) and r.startswith("EB_OK")


def main():
    t0 = time.time()
    st = json.load(open(ST, encoding="utf-8"))
    cols, centres = st["columns"], st["centres"]
    slab_top = st["slab_top"]

    # ---- 1. the middle row of holes in each floor plate ----
    log("[1] floor plates: adding the drawing's middle row (2 holes each)")
    # find the base plates: flat DIN_FLACH shapes just above the slab
    eb_api.run("dumpfull2", out="eb_e5.txt", wait=150)
    plates = []
    for line in open(os.path.join(PLUG, "eb_e5.txt"), encoding="utf-8-sig", errors="replace"):
        f = line.rstrip("\n").split("\t")
        if f[0] != "SHAPE" or len(f) < 7:
            continue
        try:
            z = float(f[4].split(",")[2])
        except Exception:
            continue
        if abs(z - (slab_top + 10.0)) < 2.0:      # plate centre = slab top + t/2
            p1 = [float(x) for x in f[4].split(",")]
            p2 = [float(x) for x in f[5].split(",")]
            plates.append({"h": f[1], "cx": (p1[0] + p2[0]) / 2.0,
                           "cy": (p1[1] + p2[1]) / 2.0, "z": z})
    log("    found %d base plates" % len(plates))
    added = 0
    for p in plates:
        for dx in (-F_HX / 2.0, F_HX / 2.0):
            r = eb_api.run("drill", hosts=p["h"],
                           at="%.3f,%.3f,%.3f" % (p["cx"] + dx, p["cy"], p["z"]),
                           dia=HOLE, n="0,0,1", wait=30)
            if ok(r):
                added += 1
    log("    middle-row holes added: %d (expect %d)" % (added, len(plates) * 2))

    # ---- 2. anchor bolts through the wall plates ----
    log("\n[2] wall plates: anchor bolts through every hole")
    made = fail = 0
    for c in cols:
        face, coord, along = c["face"], c["coord"], c["along"]
        sgn = 1.0 if face in ("+X", "+Y") else -1.0
        for z in centres:
            for du in (-W_HX / 2.0, W_HX / 2.0):
                for dv in (-W_HZ, 0.0, W_HZ):
                    if face in ("+X", "-X"):
                        # bolt runs along X: from inside the concrete out past the plate
                        a = (coord - sgn * 200.0, along + du, z + dv)
                        b = (coord + sgn * (WP_T + 60.0), along + du, z + dv)
                    else:
                        a = (along + du, coord - sgn * 200.0, z + dv)
                        b = (along + du, coord + sgn * (WP_T + 60.0), z + dv)
                    r = eb_api.run("bolt", p1="%.3f,%.3f,%.3f" % a,
                                   p2="%.3f,%.3f,%.3f" % b, dia=24,
                                   style="DIN6914", len=280, layer="PS_Bolt",
                                   wait=30, _log=False)
                    if ok(r):
                        made += 1
                    else:
                        fail += 1
        log("    col %s : bolts %d, failed %d" % (c["h"], made, fail))
    log("    wall anchor bolts: %d (expect %d), failed %d" % (made, len(cols) * 18, fail))

    # ---- 3. verify ----
    log("\n[3] VERIFY")
    for op, kw in (("dumpfull2", {"out": "eb_e5_final.txt"}),
                   ("dumpholes", {"out": "eb_e5_holes.txt"}),
                   ("dumppoly", {"out": "eb_e5_poly.txt"}),
                   ("connscan", {"out": "eb_e5_conn.txt"})):
        log("    " + str(eb_api.run(op, wait=240, **kw)))

    dias = {}
    for line in open(os.path.join(PLUG, "eb_e5_holes.txt"), encoding="utf-8-sig", errors="replace"):
        f = line.rstrip("\n").split("\t")
        if f[0] == "HOLE":
            d = round(float(f[7]))
            dias[d] = dias.get(d, 0) + 1
    log("    hole diameters: %s" % dias)

    # ---- 4. save ----
    log("\n[4] SAVE")
    try:
        app = eb_api._app()
        eb_api._send(app.ActiveDocument, "\x1b\x1b")
        time.sleep(0.8)
        app.ActiveDocument.SaveAs(DEST)
        time.sleep(5.0)
        log("    saved %s (%d bytes)" % (os.path.basename(DEST), os.path.getsize(DEST)))
    except Exception as e:
        log("    save: %s" % str(e)[:90])

    log("\nelapsed %.1f min" % ((time.time() - t0) / 60))
    out = os.path.join(ROOT, "projects", "שיעור-5", "files", "exam5-finish.log")
    open(out, "w", encoding="utf-8").write("\n".join(LOG))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
