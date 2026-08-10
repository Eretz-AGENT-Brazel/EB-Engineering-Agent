# -*- coding: utf-8 -*-
"""
exam5_go.py — one detail, then replicate. Software API only, no LISP.

  1. keep column 1's steel, delete the other 19
  2. finish column 1 : 2 middle-row floor anchors + 18 wall anchors
     (a vertical anchor replicated and rotated 90 about Y to lie horizontal)
  3. replicate the whole detail to the other 19 places, rotating about Z per face
"""
import json
import os
import sys
import time

APP = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(APP)
sys.path.insert(0, APP)
import eb_api  # noqa: E402

ST = os.path.join(ROOT, "projects", "שיעור-5", "files", "exam5_state.json")
DEST = os.path.join(ROOT, "projects", "שיעור-5", "מבחן-שיעור-5.dwg")
F_HX = 156.0
W_HX, W_HZ = 452.0, 200.0
WP_T = 20.0
HALF = 420.0
FACE_ANG = {"+X": 0.0, "+Y": 90.0, "-X": 180.0, "-Y": 270.0}
R = 40.0            # small box half-size to grab a single anchor


def log(m):
    try:
        sys.stdout.write(m + "\n"); sys.stdout.flush()
    except Exception:
        pass


def ok(r):
    return isinstance(r, str) and r.startswith("EB_OK")


def ents():
    out = []
    for e in eb_api._app().ActiveDocument.ModelSpace:
        try:
            if e.ObjectName in ("AcDb3dSolid", "PcRebarManager"):
                continue
            mn, mx = e.GetBoundingBox()
            out.append({"h": e.Handle, "cls": e.ObjectName,
                        "c": ((mn[0]+mx[0])/2, (mn[1]+mx[1])/2, (mn[2]+mx[2])/2),
                        "sz": (mx[0]-mn[0], mx[1]-mn[1], mx[2]-mn[2])})
        except Exception:
            pass
    return out


def rep(box, to, rot=0.0, axis="z", about=None, wait=40):
    kw = {"box": "%.2f,%.2f,%.2f,%.2f" % box,
          "to": "%.3f,%.3f,%.3f" % to}
    if abs(rot) > 0.001:
        kw["rot"] = rot
        kw["axis"] = axis
        kw["about"] = "%.3f,%.3f,%.3f" % (about or (0, 0, 0))
    return eb_api.run("replicate", wait=wait, _log=False, **kw)


def anchors():
    return [s for s in ents() if s["cls"] == "Ks_VolBody" and 50 < max(s["sz"]) < 1000]


def main():
    t0 = time.time()
    st = json.load(open(ST, encoding="utf-8"))
    cols, centres = st["columns"], st["centres"]
    c1 = cols[0]
    log("=" * 70)
    log("EXAM 5 — one detail then replicate (no LISP)")
    log("=" * 70)
    log("col 1: face %s at (%.1f, %.1f)" % (c1["face"], c1["x"], c1["y"]))

    # 1 -------- keep only column 1
    al = ents()
    kill = [s for s in al if not (abs(s["c"][0]-c1["x"]) <= HALF and abs(s["c"][1]-c1["y"]) <= HALF)]
    log("\n[1] steel %d, deleting %d" % (len(al), len(kill)))
    n = sum(1 for s in kill if ok(eb_api.delete(s["h"])))
    log("    deleted %d  (%.1f min)" % (n, (time.time()-t0)/60))

    # 2 -------- finish column 1
    a = anchors()
    log("\n[2] column 1 has %d anchors; adding 2 floor + 18 wall" % len(a))
    src = a[0]
    sb = (src["c"][0]-R, src["c"][1]-R, src["c"][0]+R, src["c"][1]+R)

    for dx in (-F_HX/2.0, F_HX/2.0):
        r = rep(sb, (c1["x"]+dx-src["c"][0], c1["y"]-src["c"][1], 0.0))
        log("    floor %+.0f : %s" % (dx, str(r)[:70]))

    # horizontal master: replicate aside AND rotate 90 about Y in one call
    park = (70000.0, 0.0)
    r = rep(sb, (park[0]-src["c"][0], park[1]-src["c"][1], 0.0),
            rot=90.0, axis="y", about=(park[0], park[1], src["c"][2]))
    log("    master : %s" % str(r)[:80])
    hm = [s for s in anchors() if s["c"][0] > 60000]
    if not hm:
        log("    !! no horizontal master"); return
    m = hm[0]
    log("    master size %s at %s"
        % (tuple(round(v, 1) for v in m["sz"]), tuple(round(v, 1) for v in m["c"])))
    mb = (m["c"][0]-250, m["c"][1]-250, m["c"][0]+250, m["c"][1]+250)

    w = 0
    for z in centres:
        for du in (-W_HX/2.0, W_HX/2.0):
            for dv in (-W_HZ, 0.0, W_HZ):
                tgt = (c1["coord"] + WP_T/2.0, c1["along"] + du, z + dv)
                if ok(rep(mb, (tgt[0]-m["c"][0], tgt[1]-m["c"][1], tgt[2]-m["c"][2]))):
                    w += 1
    log("    wall anchors placed: %d/18  (%.1f min)" % (w, (time.time()-t0)/60))
    eb_api.delete(m["h"])

    # 3 -------- replicate the detail
    det = (c1["x"]-HALF, c1["y"]-HALF, c1["x"]+HALF, c1["y"]+HALF)
    cnt = len([s for s in ents() if abs(s["c"][0]-c1["x"]) <= HALF and abs(s["c"][1]-c1["y"]) <= HALF])
    log("\n[3] detail = %d objects -> replicating to %d places" % (cnt, len(cols)-1))
    base = FACE_ANG[c1["face"]]
    done = 0
    for c in cols[1:]:
        ang = FACE_ANG[c["face"]] - base
        r = rep(det, (c["x"]-c1["x"], c["y"]-c1["y"], 0.0),
                rot=ang, axis="z", about=(c["x"], c["y"], 0.0), wait=90)
        if ok(r):
            done += 1
        log("    %2d/%d (%.0f,%.0f) %s rot%+.0f : %s"
            % (done, len(cols)-1, c["x"], c["y"], c["face"], ang, str(r)[:60]))

    # 4 -------- verify + save
    log("\n[4] verify (%.1f min)" % ((time.time()-t0)/60))
    log("    " + str(eb_api.run("whoami", wait=25)))
    log("    " + str(eb_api.run("dumpfull2", out="eb_go.txt", wait=300)))
    log("    " + str(eb_api.run("dumpholes", out="eb_goh.txt", wait=300)))
    log("    anchors in model: %d (expect %d)" % (len(anchors()), len(cols)*24))
    try:
        app = eb_api._app()
        eb_api._send(app.ActiveDocument, "\x1b\x1b")
        time.sleep(0.8)
        app.ActiveDocument.Save()
        time.sleep(5.0)
        log("    saved (%d bytes)" % os.path.getsize(DEST))
    except Exception as e:
        log("    save: %s" % str(e)[:70])
    log("\nTOTAL %.1f min" % ((time.time()-t0)/60))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
