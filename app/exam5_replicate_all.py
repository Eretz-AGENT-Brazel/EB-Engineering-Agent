# -*- coding: utf-8 -*-
"""
exam5_replicate_all.py — the approved detail, replicated to all 20 positions.

Amir approved column 1. Now: one `replicate` call per remaining position —
DeepCloneObjects with a displacement and a rotation about Z to suit the concrete
face it serves (+X 0deg, +Y 90, -X 180, -Y 270).

The detail is captured ONCE as a fixed handle list before any copying, so copies
can never be re-selected and cloned again (that produced 642 anchors last time).
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
WP_T, HEB = 20.0, 300.0
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
                                "c": ((mn[0]+mx[0])/2, (mn[1]+mx[1])/2, (mn[2]+mx[2])/2)})
                except Exception:
                    pass
            return out
        except Exception:
            time.sleep(2.5)
    return []


def concrete():
    tall, flat = [], []
    for _ in range(6):
        try:
            for e in eb_api._app().ActiveDocument.ModelSpace:
                try:
                    if e.ObjectName != "AcDb3dSolid":
                        continue
                    mn, mx = e.GetBoundingBox()
                    d = {"min": mn, "max": mx,
                         "sz": (mx[0]-mn[0], mx[1]-mn[1], mx[2]-mn[2])}
                    (tall if d["sz"][2] > 1000 else flat).append(d)
                except Exception:
                    pass
            return tall, flat
        except Exception:
            time.sleep(2.5)
    return tall, flat


def plan():
    tall, flat = concrete()
    slab = flat[0]
    sx0, sx1 = slab["min"][0], slab["max"][0]
    sy0, sy1 = slab["min"][1], slab["max"][1]
    tol = 5.0
    out = []
    for c in sorted(tall, key=lambda q: (q["min"][0], q["min"][1])):
        x0, x1, y0, y1 = c["min"][0], c["max"][0], c["min"][1], c["max"][1]
        mx_, my_ = (x0+x1)/2.0, (y0+y1)/2.0
        faces = []
        if x1 < sx1 - tol: faces.append(("+X", x1, my_))
        if x0 > sx0 + tol: faces.append(("-X", x0, my_))
        if y1 < sy1 - tol: faces.append(("+Y", y1, mx_))
        if y0 > sy0 + tol: faces.append(("-Y", y0, mx_))
        for face, coord, along in faces:
            if face == "+X":
                sx, sy = coord + WP_T + HEB/2.0, along
            elif face == "-X":
                sx, sy = coord - WP_T - HEB/2.0, along
            elif face == "+Y":
                sx, sy = along, coord + WP_T + HEB/2.0
            else:
                sx, sy = along, coord - WP_T - HEB/2.0
            out.append({"face": face, "coord": coord, "along": along,
                        "x": sx, "y": sy})
    return out


def main():
    t0 = time.time()
    log("=" * 74)
    log("REPLICATE the approved detail to every position")
    log("=" * 74)
    log(str(eb_api.run("whoami", wait=25)))

    positions = plan()
    log("\n%d positions (corner columns 2 faces, edge columns 3)" % len(positions))
    for p in positions:
        log("   %-3s at (%8.1f, %9.1f)" % (p["face"], p["x"], p["y"]))

    # the built detail is at position[0]
    c1 = positions[0]
    al = ents()
    detail = [s for s in al
              if abs(s["c"][0]-c1["x"]) <= 500 and abs(s["c"][1]-c1["y"]) <= 500]
    log("\napproved detail: %d objects at (%.1f, %.1f) face %s"
        % (len(detail), c1["x"], c1["y"], c1["face"]))
    kinds = {}
    for s in detail:
        kinds[s["cls"]] = kinds.get(s["cls"], 0) + 1
    log("   %s" % kinds)
    if len(al) != len(detail):
        log("   ! %d steel objects lie outside the detail box — check" % (len(al)-len(detail)))

    # FIXED handle list, captured once. Never a box — a box re-selects the copies
    # that land inside it and the counts snowball (754 anchors instead of 480).
    handles = ",".join(s["h"] for s in detail)
    base = FACE_ANG[c1["face"]]
    log("\nreplicating by explicit handles (one API call per position)")
    done = 0
    for p in positions[1:]:
        ang = FACE_ANG[p["face"]] - base
        r = eb_api.run("replicate", handles=handles,
                       to="%.3f,%.3f,0" % (p["x"]-c1["x"], p["y"]-c1["y"]),
                       rot=ang, axis="z",
                       about="%.3f,%.3f,0" % (p["x"], p["y"]),
                       wait=120, _log=False)
        if ok(r):
            done += 1
        log("   %2d/%d  (%8.1f,%9.1f) %-3s rot%+4.0f   %s"
            % (done, len(positions)-1, p["x"], p["y"], p["face"], ang, str(r)[:48]))

    log("\nVERIFY (%.1f min)" % ((time.time()-t0)/60))
    log("   " + str(eb_api.run("dumpfull2", out="eb_all.txt", wait=300)))
    log("   " + str(eb_api.run("dumpholes", out="eb_allh.txt", wait=300)))
    fin = ents()
    kinds = {}
    for s in fin:
        kinds[s["cls"]] = kinds.get(s["cls"], 0) + 1
    log("   objects now: %s" % kinds)
    exp = {k: v*len(positions) for k, v in
           {"Ks_Shape": 2, "Ks_Plate": 3, "Ks_VolBody": 24}.items()}
    log("   expected   : %s" % exp)
    dias = {}
    for line in open(os.path.join(PLUG, "eb_allh.txt"), encoding="utf-8-sig", errors="replace"):
        f = line.rstrip("\n").split("\t")
        if f[0] == "HOLE":
            d = round(float(f[7]))
            dias[d] = dias.get(d, 0) + 1
    log("   holes: %s   (expect %d of dia 28)" % (dias, 24*len(positions)))

    for i in range(5):
        try:
            d = eb_api._app().ActiveDocument
            d.Save(); time.sleep(5)
            log("   saved (%d bytes)" % os.path.getsize(DEST)); break
        except Exception:
            time.sleep(3)
    log("\nTOTAL %.1f min" % ((time.time()-t0)/60))
    open(os.path.join(ROOT, "projects", "שיעור-5", "files", "replicate-all.log"),
         "w", encoding="utf-8").write("\n".join(LOG))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
