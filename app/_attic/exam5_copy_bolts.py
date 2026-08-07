# -*- coding: utf-8 -*-
"""
exam5_copy_bolts.py — put an anchor bolt in every hole, by COPYING one.

Amir: "why are you complicating this — just copy bolts to the centre of a hole."
That is exactly his own method for the wall plates. The base-plate macro already
produced proper anchors (nut included); every remaining hole gets a copy of one,
placed on the hole centre. For the wall plates the copy is rotated 90 degrees so
the anchor runs horizontally into the concrete.

Native COPY keeps the object exactly as ProSteel built it — nut, thread, layer.
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
LOG = []


def log(m):
    LOG.append(m)
    try:
        sys.stdout.write(m + "\n"); sys.stdout.flush()
    except Exception:
        pass


def lisp(s, pause=0.35):
    for _ in range(4):
        try:
            eb_api._send(eb_api._app().ActiveDocument, "\x1b\x1b" + s)
            time.sleep(pause)
            return True
        except Exception:
            time.sleep(1.5)
    return False


def anchors_and_holes():
    """existing anchors (with their centres) and every hole in the model"""
    doc = eb_api._app().ActiveDocument
    anchors = []
    for e in doc.ModelSpace:
        try:
            if e.ObjectName != "Ks_VolBody":
                continue
            mn, mx = e.GetBoundingBox()
            sz = (mx[0]-mn[0], mx[1]-mn[1], mx[2]-mn[2])
            if max(sz) > 1000 or max(sz) < 50:
                continue
            anchors.append({"h": e.Handle,
                            "c": ((mn[0]+mx[0])/2.0, (mn[1]+mx[1])/2.0, (mn[2]+mx[2])/2.0),
                            "sz": sz,
                            "axis": sz.index(max(sz))})
        except Exception:
            pass
    eb_api.run("dumpholes", out="eb_e5h.txt", wait=240)
    holes = []
    for line in open(os.path.join(PLUG, "eb_e5h.txt"), encoding="utf-8-sig", errors="replace"):
        f = line.rstrip("\n").split("\t")
        if f[0] != "HOLE" or len(f) < 8:
            continue
        s = [float(x) for x in f[5].split(",")]
        e2 = [float(x) for x in f[6].split(",")]
        mid = tuple((s[k] + e2[k]) / 2.0 for k in range(3))
        d = [abs(e2[k] - s[k]) for k in range(3)]
        holes.append({"host": f[1], "mid": mid, "axis": d.index(max(d)),
                      "dia": float(f[7])})
    return anchors, holes


def main():
    t0 = time.time()
    log("=" * 74)
    log("EXAM 5 — an anchor bolt in every hole, by copying")
    log("=" * 74)

    anchors, holes = anchors_and_holes()
    log("existing anchors: %d | holes in model: %d" % (len(anchors), len(holes)))
    if not anchors:
        log("!! no anchor to copy"); return
    byax = {}
    for a in anchors:
        byax.setdefault(a["axis"], []).append(a)
    log("anchors by axis: %s" % {k: len(v) for k, v in byax.items()})

    # which holes already have an anchor sitting on them?
    def occupied(mid, tol=12.0):
        for a in anchors:
            if all(abs(a["c"][k] - mid[k]) < tol for k in (0, 1)) and \
               abs(a["c"][2] - mid[2]) < 260:
                return True
        return False

    # a horizontal source anchor: copy a vertical one aside and rotate it 90 deg
    src_v = byax.get(2, [None])[0]
    log("\nvertical source anchor: %s  size %s" % (src_v["h"], src_v["sz"]))

    src_h = None
    if any(k != 2 for k in byax):
        for k, v in byax.items():
            if k != 2:
                src_h = v[0]
                break
    if src_h is None:
        log("making a horizontal source: copy aside, then ROTATE3D 90 about X")
        far = (40000.0, 0.0, 0.0)
        dx = far[0] - src_v["c"][0]
        dy = far[1] - src_v["c"][1]
        dz = far[2] - src_v["c"][2]
        lisp('_COPY (handent "%s") "" _non 0,0,0 _non %.3f,%.3f,%.3f\n'
             % (src_v["h"], dx, dy, dz), 1.2)
        # find it
        doc = eb_api._app().ActiveDocument
        cand = None
        for e in doc.ModelSpace:
            try:
                if e.ObjectName != "Ks_VolBody":
                    continue
                mn, mx = e.GetBoundingBox()
                if mn[0] > 30000:
                    cand = e
            except Exception:
                pass
        if cand is None:
            log("   !! copy-aside failed")
        else:
            mn, mx = cand.GetBoundingBox()
            cx = ((mn[0]+mx[0])/2.0, (mn[1]+mx[1])/2.0, (mn[2]+mx[2])/2.0)
            lisp('_ROTATE3D (handent "%s") "" _X _non %.3f,%.3f,%.3f 90\n'
                 % (cand.Handle, cx[0], cx[1], cx[2]), 1.5)
            mn, mx = cand.GetBoundingBox()
            sz = (mx[0]-mn[0], mx[1]-mn[1], mx[2]-mn[2])
            src_h = {"h": cand.Handle,
                     "c": ((mn[0]+mx[0])/2.0, (mn[1]+mx[1])/2.0, (mn[2]+mx[2])/2.0),
                     "sz": sz, "axis": sz.index(max(sz))}
            log("   horizontal source %s size %s axis=%d"
                % (src_h["h"], tuple(round(v, 1) for v in sz), src_h["axis"]))

    made = 0
    skipped = 0
    for i, hl in enumerate(holes, 1):
        if hl["axis"] == 2:
            if occupied(hl["mid"]):
                skipped += 1
                continue
            src = src_v
        else:
            src = src_h
            if src is None:
                continue
            if src["axis"] != hl["axis"]:
                # need the other horizontal orientation: rotate about Z later
                pass
        dx = hl["mid"][0] - src["c"][0]
        dy = hl["mid"][1] - src["c"][1]
        dz = hl["mid"][2] - src["c"][2]
        if lisp('_COPY (handent "%s") "" _non 0,0,0 _non %.3f,%.3f,%.3f\n'
                % (src["h"], dx, dy, dz), 0.30):
            made += 1
        if i % 50 == 0:
            log("   %d/%d copied=%d skipped=%d (%.1f min)"
                % (i, len(holes), made, skipped, (time.time()-t0)/60))

    log("\ncopied %d anchors, skipped %d already-anchored holes" % (made, skipped))

    # remove the far-away source helper
    if src_h and src_h["c"][0] > 30000:
        eb_api.delete(src_h["h"])
        log("helper removed")

    log("\nverify:")
    log("  " + str(eb_api.run("whoami", wait=25)))
    try:
        app = eb_api._app()
        eb_api._send(app.ActiveDocument, "\x1b\x1b")
        time.sleep(0.8)
        app.ActiveDocument.Save()
        time.sleep(5.0)
        log("  saved (%d bytes)" % os.path.getsize(DEST))
    except Exception as e:
        log("  save: %s" % str(e)[:80])
    log("\nelapsed %.1f min" % ((time.time()-t0)/60))
    open(os.path.join(ROOT, "projects", "שיעור-5", "files", "exam5-bolts.log"),
         "w", encoding="utf-8").write("\n".join(LOG))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
