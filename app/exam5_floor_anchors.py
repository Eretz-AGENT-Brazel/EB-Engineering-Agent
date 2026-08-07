# -*- coding: utf-8 -*-
"""
exam5_floor_anchors.py — seat the floor anchors properly on the base plate.

Same fault as the wall anchors had: the bolt is 172.5 long, so with 120 into the
concrete it projects 32.5 past the plate while the nut is only 22.5 — the nut
floats 10mm above the plate instead of bearing on it.

Correct length = 120 (concrete) + 20 (plate) + 22.5 (nut) = 162.5, and the
software's nearest is 165, which is what the wall anchors already use.

All 120 floor anchors are replaced with 165-long ones positioned so the shank
starts 120 into the slab and the nut sits on the plate.
"""
import math
import os
import re
import sys
import time
from collections import Counter

APP = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(APP)
PLUG = os.path.join(APP, "plugin")
sys.path.insert(0, APP)
import eb_api  # noqa: E402

SLAB_TOP = 30.0
PLATE_TOP = SLAB_TOP + 20.0
EMBED, NUT = 120.0, 22.5
SX, PARK = 60000.0, 70000.0
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


def anchors():
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
                                    "len": max(sz), "axis": sz.index(max(sz)),
                                    "c": ((mn[0]+mx[0])/2, (mn[1]+mx[1])/2,
                                          (mn[2]+mx[2])/2)})
                except Exception:
                    pass
            return out
        except Exception:
            time.sleep(2)
    return []


def main():
    t0 = time.time()
    log("=" * 70)
    log("FLOOR ANCHORS — nut must bear on the plate, 120 into the slab")
    log("=" * 70)

    al = anchors()
    floor = [a for a in al if a["axis"] == 2]
    wall = [a for a in al if a["axis"] == 0]
    log("\nfloor %d (len %s) | wall %d (len %s)"
        % (len(floor), dict(Counter(round(a["len"], 1) for a in floor)),
           len(wall), dict(Counter(round(a["len"], 1) for a in wall))))
    if floor:
        a = floor[0]
        log("   current gap under the nut: %.1f mm"
            % (a["mx"][2] - PLATE_TOP - NUT))

    # keep each anchor's XY, they are already in the right holes
    spots = [(a["c"][0], a["c"][1]) for a in floor]
    log("\n[1] removing %d floor anchors (their XY positions are kept)" % len(floor))
    for a in floor:
        eb_api.delete(a["h"])

    # a correct-length vertical master, harvested from a scratch connection
    log("\n[2] harvesting a 165-long anchor")
    r = eb_api.run("beam", name="HE300B", catalog="DIN_HEB",
                   p1="%.1f,0,0" % SX, p2="%.1f,0,3000" % SX,
                   layer="PS_Shape", wait=45, _log=False)
    sc = H(r)
    before = set(x["h"] for x in anchors())
    eb_api.run("connbase", handle=sc, template="default/Standard",
               l=300, w=300, t=20, holedia=28, hx=156, hy=162, anchors=1,
               anchordia=20, anchorkey=30, anchordrill=140, shorten=1,
               wait=90, _log=False)
    new = [x for x in anchors() if x["h"] not in before]
    if not new:
        log("    !! harvest failed"); return
    m = new[0]
    L = m["len"]
    log("    master %s length %.1f" % (m["h"], L))

    # where its centre must land: shank 120 into the slab
    z_bot = SLAB_TOP - EMBED
    tgt_cz = z_bot + L / 2.0
    log("    span z %.1f .. %.1f -> nut gap %.1f"
        % (z_bot, z_bot + L, z_bot + L - PLATE_TOP - NUT))

    log("\n[3] placing %d anchors" % len(spots))
    mb = (m["c"][0]-30, m["c"][1]-30, m["c"][0]+30, m["c"][1]+30)
    cnt = 0
    for (x, y) in spots:
        if ok(eb_api.run("replicate", box="%.2f,%.2f,%.2f,%.2f" % mb,
                         to="%.3f,%.3f,%.3f" % (x - m["c"][0], y - m["c"][1],
                                                tgt_cz - m["c"][2]),
                         wait=45, _log=False)):
            cnt += 1
    log("    placed %d/%d" % (cnt, len(spots)))

    log("\n[4] clearing the scratch zone")
    k = 0
    for x in anchors():
        if x["c"][0] > 50000:
            eb_api.delete(x["h"]); k += 1
    eb_api.run("dumpfull2", out="eb_fa9.txt", wait=300)
    for line in open(os.path.join(PLUG, "eb_fa9.txt"), encoding="utf-8-sig", errors="replace"):
        f = line.rstrip("\n").split("\t")
        if f[0] == "SHAPE" and len(f) >= 5:
            try:
                if float(f[4].split(",")[0]) > 50000:
                    eb_api.delete(f[1]); k += 1
            except Exception:
                pass
    log("    removed %d" % k)

    log("\n[5] VERIFY")
    fin = anchors()
    fl = [a for a in fin if a["axis"] == 2]
    wl = [a for a in fin if a["axis"] == 0]
    if fl:
        a = fl[0]
        log("    floor: len %.1f | z %.1f .. %.1f | %.0f into concrete | nut gap %.1f"
            % (a["len"], a["mn"][2], a["mx"][2], SLAB_TOP - a["mn"][2],
               a["mx"][2] - PLATE_TOP - NUT))
    if wl:
        a = wl[0]
        log("    wall : len %.1f | %.0f into concrete | nut gap %.1f"
            % (a["len"], 1082.792 - a["mn"][0], a["mx"][0] - 1102.792 - NUT))
    log("    anchors: %d floor + %d wall = %d (want 480)"
        % (len(fl), len(wl), len(fin)))
    log("    " + str(eb_api.run("dumpholes", out="eb_fa9h.txt", wait=300)))
    log("    " + str(eb_api.run("dumpfull2", out="eb_fa9f.txt", wait=300)))
    for i in range(5):
        try:
            d = eb_api._app().ActiveDocument
            d.Save(); time.sleep(5); log("    saved"); break
        except Exception:
            time.sleep(3)
    log("\n%.1f min" % ((time.time()-t0)/60))
    open(os.path.join(ROOT, "projects", "שיעור-5", "files", "floor-anchors.log"),
         "w", encoding="utf-8").write("\n".join(LOG))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
