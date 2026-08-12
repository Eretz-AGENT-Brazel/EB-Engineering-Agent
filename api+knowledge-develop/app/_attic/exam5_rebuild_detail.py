# -*- coding: utf-8 -*-
"""
exam5_rebuild_detail.py — rebuild the whole detail cleanly from the concrete.

Why this is needed: `connremove` takes the base plate away but does NOT restore
the column's length, while `connbase shorten=1` shortens it again. Running that
cycle nine times while testing anchor lengths lifted the column 180mm off the
slab. So the column itself is rebuilt, not just the connection.

ANCHOR EMBEDMENT is left as a single constant at the top — Amir is defining how
deep the bolt goes into the concrete, and that is the only number that should
drive the anchor length.
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

# --- geometry from the model / drawing ---
SLAB_TOP = 30.0
FACE_X = 1082.792                 # concrete face of column 1 (+X)
CX, CY = 1252.792, -8780.847      # steel column centre
COL_NAME, COL_CAT = "HE300B", "DIN_HEB"
COL_LEN = 6000.0
FP, FP_T = 300.0, 20.0
HX, HY, HOLE = 156.0, 162.0, 28.0
WP_W, WP_H, WP_T = 600.0, 500.0, 20.0
W_HX, W_HZ = 452.0, 200.0
PLATE_OUT = FACE_X + WP_T

# --- anchor: driven by the embedment depth Amir gave ---
# An anchor bolt runs: EMBED into the concrete -> through the plate -> and PROUD
# of the plate for the washer and nut. Ending flush with the plate face (what I
# did before) is not a real bolt.
EMBED = 120.0                     # mm of bolt inside the concrete (Amir)
PROUD = 30.0                      # projection past the plate for washer + nut
A_DIA, A_KEY = 20.0, 30.0         # 20mm bolt, SW30 nut (matches Amir's 34.6 a/c)
A_LEN = EMBED + FP_T + PROUD      # 120 + 20 + 30 = 170 total body length
A_DRILL = A_LEN - 22.5            # measured: body length = drillLen + 22.5

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
    for _ in range(6):
        try:
            out = []
            for e in eb_api._app().ActiveDocument.ModelSpace:
                try:
                    nm = e.ObjectName
                    if nm in ("AcDb3dSolid", "PcRebarManager"):
                        continue
                    mn, mx = e.GetBoundingBox()
                    out.append({"h": e.Handle, "cls": nm, "mn": mn, "mx": mx,
                                "sz": (mx[0]-mn[0], mx[1]-mn[1], mx[2]-mn[2]),
                                "c": ((mn[0]+mx[0])/2, (mn[1]+mx[1])/2, (mn[2]+mx[2])/2)})
                except Exception:
                    pass
            return out
        except Exception:
            time.sleep(2.5)
    return []


def anchors():
    return [s for s in ents() if s["cls"] == "Ks_VolBody" and 20 < max(s["sz"]) < 1500]


def main():
    t0 = time.time()
    log("=" * 74)
    log("REBUILD THE DETAIL — clean, from the concrete up")
    log("=" * 74)
    log("anchor embedment into concrete: %g mm  -> drillLen %g" % (EMBED, A_DRILL))

    # ---- 0. clear all steel ----
    al = ents()
    log("\n[0] clearing %d steel objects" % len(al))
    n = sum(1 for s in al if ok(eb_api.delete(s["h"])))
    log("    deleted %d" % n)

    # ---- 1. column, modelled so the FINAL length is 6000 ----
    z_bot, z_top = SLAB_TOP, SLAB_TOP + FP_T + COL_LEN
    log("\n[1] column %s from z=%.0f to z=%.0f" % (COL_NAME, z_bot, z_top))
    r = eb_api.run("beam", name=COL_NAME, catalog=COL_CAT,
                   p1="%.3f,%.3f,%.3f" % (CX, CY, z_bot),
                   p2="%.3f,%.3f,%.3f" % (CX, CY, z_top),
                   layer="PS_Shape", wait=45)
    col = H(r)
    log("    " + str(r)[:110])
    if not col:
        return
    # flanges perpendicular to the concrete face -> rotate 90 about its own axis
    log("    " + str(eb_api.run("rotate", handles=col, rot=90, axis="z",
                                about="%.3f,%.3f,0" % (CX, CY), wait=45))[:90])

    # ---- 2. base plate ONCE, with anchors driven by the embedment ----
    log("\n[2] base plate %gx%gx%g, holes dia %g @ %g x %g, anchors dia %g key %g"
        % (FP, FP, FP_T, HOLE, HX, HY, A_DIA, A_KEY))
    r = eb_api.run("connbase", handle=col, template="default/Standard",
                   l=FP, w=FP, t=FP_T, holedia=HOLE, hx=HX, hy=HY, anchors=1,
                   anchordia=A_DIA, anchorkey=A_KEY, anchordrill=A_DRILL,
                   shorten=1, wait=90)
    log("    " + str(r)[:130])

    # ---- 3. the drawing's two centre holes ----
    eb_api.run("dumpfull2", out="eb_rb.txt", wait=200)
    plate, pz = None, 0
    for line in open(os.path.join(PLUG, "eb_rb.txt"), encoding="utf-8-sig", errors="replace"):
        f = line.rstrip("\n").split("\t")
        if f[0] == "SHAPE" and len(f) >= 7:
            try:
                z1 = float(f[4].split(",")[2]); z2 = float(f[5].split(",")[2])
            except Exception:
                continue
            if abs(z2-z1) < 5 and z1 < 100:
                plate, pz = f[1], z1
    eb_api.run("dumpholes", out="eb_rbh.txt", wait=200)
    ys = sorted(set(round(float(l.split("\t")[5].split(",")[1]), 1)
                    for l in open(os.path.join(PLUG, "eb_rbh.txt"),
                                  encoding="utf-8-sig", errors="replace")
                    if l.startswith("HOLE") and l.split("\t")[1] == plate))
    log("\n[3] centre holes at x=%.1f, y=%s" % (CX, ys))
    src = anchors()[0]
    for y in ys:
        eb_api.run("drill", hosts=plate, at="%.3f,%.3f,%.3f" % (CX, y, pz),
                   dia=HOLE, n="0,0,1", wait=45, _log=False)
        eb_api.run("replicate",
                   box="%.2f,%.2f,%.2f,%.2f" % (src["c"][0]-40, src["c"][1]-40,
                                                src["c"][0]+40, src["c"][1]+40),
                   to="%.3f,%.3f,0" % (CX - src["c"][0], y - src["c"][1]),
                   wait=45, _log=False)
    log("    6 holes, 6 anchors")

    # ---- 4. three wall plates with holes ----
    log("\n[4] 3 wall plates")
    centres = [z_top - 200.0 - WP_H/2.0 - k*1500.0 for k in range(3)]
    log("    centres z: %s" % ", ".join("%.0f" % v for v in centres))
    nh = 0
    for z in centres:
        ctr = (FACE_X + WP_T/2.0, CY, z)
        r = eb_api.run("plate", center="%.3f,%.3f,%.3f" % ctr, l=WP_W, w=WP_H,
                       t=WP_T, ex="0,1,0", ey="0,0,1", ez="1,0,0",
                       layer="PS_Plate", wait=45, _log=False)
        h = H(r)
        if not h:
            continue
        for du in (-W_HX/2.0, W_HX/2.0):
            for dv in (-W_HZ, 0.0, W_HZ):
                if ok(eb_api.run("drill", hosts=h,
                                 at="%.3f,%.3f,%.3f" % (ctr[0], ctr[1]+du, ctr[2]+dv),
                                 dia=HOLE, n="1,0,0", wait=30, _log=False)):
                    nh += 1
    log("    3 plates, %d holes" % nh)

    # ---- 5. wall anchors, heads on the plate face ----
    fa = [b for b in anchors() if b["sz"].index(max(b["sz"])) == 2]
    src = fa[0]
    L = max(src["sz"])
    # the bolt must start EMBED deep in the concrete and project PROUD past the
    # plate, so its centre sits accordingly — not flush with the plate face
    x_start = FACE_X - EMBED               # deepest point, inside the concrete
    tgt_cx = x_start + L/2.0
    log("\n[5] wall anchors: length %.1f" % L)
    log("    %.0f into concrete (from x=%.1f), through the plate, %.0f proud"
        % (EMBED, x_start, L - EMBED - WP_T))
    log("    centre x=%.1f, spans %.1f .. %.1f" % (tgt_cx, x_start, x_start + L))
    park = 70000.0
    eb_api.run("replicate",
               box="%.2f,%.2f,%.2f,%.2f" % (src["c"][0]-40, src["c"][1]-40,
                                            src["c"][0]+40, src["c"][1]+40),
               to="%.3f,%.3f,0" % (park - src["c"][0], -src["c"][1]),
               rot=90.0, axis="y", about="%.3f,0,%.3f" % (park, src["c"][2]),
               wait=60, _log=False)
    hm = [b for b in anchors() if b["c"][0] > 60000]
    if not hm:
        log("    !! master failed"); return
    m = hm[0]
    mb = (m["c"][0]-300, m["c"][1]-300, m["c"][0]+300, m["c"][1]+300)
    cnt = 0
    for z in centres:
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

    # ---- 6. verify ----
    log("\n[6] VERIFY")
    eb_api.run("dumpfull2", out="eb_rb2.txt", wait=200)
    for line in open(os.path.join(PLUG, "eb_rb2.txt"), encoding="utf-8-sig", errors="replace"):
        f = line.rstrip("\n").split("\t")
        if f[0] == "SHAPE":
            log("    %-10s z %s .. %s  L=%s"
                % (f[2], f[4].split(",")[2], f[5].split(",")[2], f[6]))
    fin = anchors()
    lens = {}
    stick = 0
    for b in fin:
        lens[round(max(b["sz"]))] = lens.get(round(max(b["sz"])), 0) + 1
        if b["sz"].index(max(b["sz"])) == 0 and b["mx"][0] > PLATE_OUT + 1:
            stick += 1
    log("    anchors %d | lengths %s | sticking out %d" % (len(fin), lens, stick))
    hz = [b for b in fin if b["sz"].index(max(b["sz"])) == 0]
    vt = [b for b in fin if b["sz"].index(max(b["sz"])) == 2]
    if hz:
        log("    wall  x %.1f .. %.1f  -> %.0f mm into concrete (face %.1f)"
            % (hz[0]["mn"][0], hz[0]["mx"][0], FACE_X - hz[0]["mn"][0], FACE_X))
    if vt:
        log("    floor z %.1f .. %.1f  -> %.0f mm into concrete (slab top %.0f)"
            % (vt[0]["mn"][2], vt[0]["mx"][2], SLAB_TOP - vt[0]["mn"][2], SLAB_TOP))
    log("    " + str(eb_api.run("dumpholes", out="eb_rb2h.txt", wait=200)))
    for i in range(5):
        try:
            d = eb_api._app().ActiveDocument
            d.Save(); time.sleep(4); log("    saved"); break
        except Exception:
            time.sleep(3)
    log("\n%.1f min" % ((time.time()-t0)/60))
    open(os.path.join(ROOT, "projects", "שיעור-5", "files", "rebuild-detail.log"),
         "w", encoding="utf-8").write("\n".join(LOG))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
