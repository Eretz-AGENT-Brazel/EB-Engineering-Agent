# -*- coding: utf-8 -*-
"""
exam_fix_baseplate.py — put the base plate ON the floor, the way lesson 4 showed.

What went wrong on the first attempt: I built the connection without
`ShortenShape`, so the plate went from z=-20 to 0 (buried under the floor) and
the column stayed 0..3500. In Amir's own lesson the plate sits 0..14 and the
column starts at 14 — the plate rests ON the floor and the design level at the
top is preserved. The lesson is called "base plate to the FLOOR"; z=0 is the
floor, so nothing belongs below it.

Fix: remove the connection with the parts it made, then rebuild it with
ShortenShape on, and verify by reading the geometry back.
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

PLATE, PLATE_T = 400.0, 20.0
HOLE_DIA, PITCH = 23.0, 300.0


def log(m):
    try:
        sys.stdout.write(m + "\n"); sys.stdout.flush()
    except Exception:
        pass


def shapes():
    eb_api.run("dumpfull2", out="eb_full_fix.txt", wait=120)
    out = []
    for line in open(os.path.join(PLUG, "eb_full_fix.txt"), encoding="utf-8-sig", errors="replace"):
        f = line.rstrip("\n").split("\t")
        if f[0] == "SHAPE" and len(f) >= 7:
            out.append({"h": f[1], "prof": f[2], "name": f[8] if len(f) > 8 else "",
                        "p1": f[4], "p2": f[5], "L": f[6]})
    return out


def main():
    log("=" * 74)
    log("FIX — base plate must sit ON the floor (z=0), column shortened")
    log("=" * 74)
    log(str(eb_api.run("whoami", wait=30)))

    log("\nbefore:")
    col = None
    for s in shapes():
        log("  %-10s %-16s p1=%-20s p2=%-20s L=%s" % (s["prof"], s["name"], s["p1"], s["p2"], s["L"]))
        if "RQ200" in s["prof"] or "SHS" in s["prof"].upper():
            col = s["h"]
    if not col:
        log("!! column not found")
        return

    log("\nremoving the old connection together with its plate and anchors")
    log("  " + str(eb_api.run("connremove", handle=col, delparts=1, wait=60)))

    log("\nrebuilding with ShortenShape ON")
    r = eb_api.run("connbase", handle=col, l=PLATE, w=PLATE, t=PLATE_T,
                   holedia=HOLE_DIA, hx=PITCH, hy=PITCH, anchors=1,
                   shorten=1, wait=60)
    log("  " + str(r)[:170])

    log("\nafter:")
    plate_z = None
    col_z0 = None
    for s in shapes():
        log("  %-10s %-16s p1=%-20s p2=%-20s L=%s" % (s["prof"], s["name"], s["p1"], s["p2"], s["L"]))
        try:
            z = float(s["p1"].split(",")[2])
        except Exception:
            z = None
        if "RQ200" in s["prof"] or "SHS" in s["prof"].upper():
            col_z0 = z
        elif z is not None:
            plate_z = z

    log("\nverification:")
    log("  " + str(eb_api.run("dumpholes", out="eb_holes_fix.txt", wait=120)))
    log("  " + str(eb_api.run("connscan", out="eb_conn_fix.txt", wait=150)))

    okz = plate_z is not None and abs(plate_z - PLATE_T / 2.0) < 1.0
    okc = col_z0 is not None and abs(col_z0 - PLATE_T) < 1.0
    log("  plate centre z = %s   (expected %.0f -> plate spans 0..%.0f)  %s"
        % (plate_z, PLATE_T / 2.0, PLATE_T, "PASS" if okz else "FAIL"))
    log("  column starts at z = %s  (expected %.0f)  %s"
        % (col_z0, PLATE_T, "PASS" if okc else "FAIL"))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8-sig", errors="replace")
    except Exception:
        pass
    main()
