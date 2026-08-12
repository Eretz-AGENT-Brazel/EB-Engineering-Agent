# -*- coding: utf-8 -*-
"""
exam5_finish_detail.py — reapply the two approved anchor corrections, in place.

No rebuilding. The rebuild is what kept undoing corrections Amir had already
approved. These are point fixes on the objects that exist:

  floor anchors : sit 128 into the slab, must be 120  -> shift +7.5
  wall anchors  : 172.5 long so the nut floats 10mm clear of the plate
                  -> replace with 165 long, 120 into concrete, nut bearing
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

SLAB_TOP = 30.0
FACE_X, WP_T = 1082.792, 20.0
PLATE_OUT = FACE_X + WP_T
EMBED, NUT = 120.0, 22.5
CY = -8780.847
W_HX, W_HZ = 452.0, 200.0
CENTRES = [5600.0, 4100.0, 2600.0]
SX = 60000.0
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
                                    "c": ((mn[0]+mx[0])/2, (mn[1]+mx[1])/2,
                                          (mn[2]+mx[2])/2),
                                    "axis": sz.index(max(sz))})
                except Exception:
                    pass
            return out
        except Exception:
            time.sleep(2)
    return []


def main():
    t0 = time.time()
    log("=" * 70)
    log("FINISH THE DETAIL — point fixes only, no rebuilding")
    log("=" * 70)

    # ---- 1. floor anchors to exactly 120 ----
    al = anchors()
    floor = [a for a in al if a["axis"] == 2]
    wall = [a for a in al if a["axis"] == 0]
    log("\n[1] floor anchors: %d" % len(floor))
    if floor:
        a = floor[0]
        want_bottom = SLAB_TOP - EMBED
        dz = want_bottom - a["mn"][2]
        log("    bottom z=%.1f, want %.1f -> shift %+.1f"
            % (a["mn"][2], want_bottom, dz))
        if abs(dz) > 0.05:
            n = 0
            for a in floor:
                r = eb_api.run("replicate",
                               box="%.2f,%.2f,%.2f,%.2f" % (a["c"][0]-30, a["c"][1]-30,
                                                            a["c"][0]+30, a["c"][1]+30),
                               to="0,0,%.3f" % dz, wait=45, _log=False)
                if ok(r):
                    eb_api.delete(a["h"])
                    n += 1
            log("    shifted %d" % n)
        else:
            log("    already correct")

    # ---- 2. wall anchors: correct length, nut bearing on the plate ----
    log("\n[2] wall anchors: %d" % len(wall))
    if wall:
        a = wall[0]
        L = max(a["sz"])
        gap = a["mx"][0] - PLATE_OUT - NUT
        log("    length %.1f, nut gap %.1f" % (L, gap))
    if wall and abs(max(wall[0]["sz"]) - 165.0) > 1.0:
        log("    replacing with 165-long anchors")
        for a in wall:
            eb_api.delete(a["h"])
        # harvest a 165 anchor from a scratch column
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
        h = new[0]
        log("    harvested %.1f long" % max(h["sz"]))
        eb_api.run("replicate",
                   box="%.2f,%.2f,%.2f,%.2f" % (h["c"][0]-30, h["c"][1]-30,
                                                h["c"][0]+30, h["c"][1]+30),
                   to="%.3f,%.3f,0" % (PARK - h["c"][0], -h["c"][1]),
                   rot=90.0, axis="y", about="%.3f,0,%.3f" % (PARK, h["c"][2]),
                   wait=60, _log=False)
        hm = [x for x in anchors() if x["c"][0] > PARK - 2000]
        m = hm[0]
        L = max(m["sz"])
        tgt_cx = FACE_X - EMBED + L/2.0
        log("    master %.1f -> centre x=%.1f, spans %.1f .. %.1f"
            % (L, tgt_cx, FACE_X - EMBED, FACE_X - EMBED + L))
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
        log("    placed %d/18" % cnt)
        # clear the scratch zone
        k = 0
        for x in anchors():
            if x["c"][0] > 50000:
                eb_api.delete(x["h"]); k += 1
        eb_api.run("dumpfull2", out="eb_fd.txt", wait=200)
        for line in open(os.path.join(PLUG, "eb_fd.txt"), encoding="utf-8-sig",
                         errors="replace"):
            f = line.rstrip("\n").split("\t")
            if f[0] == "SHAPE" and len(f) >= 5:
                try:
                    if float(f[4].split(",")[0]) > 50000:
                        eb_api.delete(f[1]); k += 1
                except Exception:
                    pass
        log("    scratch cleared: %d objects" % k)

    # ---- 3. verify the finished detail ----
    log("\n[3] VERIFY")
    fin = anchors()
    w = [a for a in fin if a["axis"] == 0]
    fl = [a for a in fin if a["axis"] == 2]
    if fl:
        a = fl[0]
        log("    floor: len %.1f | z %.1f .. %.1f | %.0f into concrete"
            % (max(a["sz"]), a["mn"][2], a["mx"][2], SLAB_TOP - a["mn"][2]))
    if w:
        a = w[0]
        log("    wall : len %.1f | x %.1f .. %.1f | %.0f into concrete | nut gap %.1f"
            % (max(a["sz"]), a["mn"][0], a["mx"][0], FACE_X - a["mn"][0],
               a["mx"][0] - PLATE_OUT - NUT))
    log("    anchors: %d floor + %d wall = %d" % (len(fl), len(w), len(fin)))
    log("    " + str(eb_api.run("dumpholes", out="eb_fdh.txt", wait=200)))
    for i in range(5):
        try:
            d = eb_api._app().ActiveDocument
            d.Save(); time.sleep(4); log("    saved"); break
        except Exception:
            time.sleep(3)
    log("\n%.1f min" % ((time.time()-t0)/60))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
