# -*- coding: utf-8 -*-
"""
reshape_ribs.py — turn rectangular stiffener plates into REAL ribs.

The understanding (learned from ProSteel's own stiffener templates, which offer
shape = 0 chamfered / 1 convex / 2 rounded, each half or full):

  A rib is never a plain rectangle. The corners on the side that meets the
  profile are CUT BACK, because
    * the profile has an inner corner radius there — a square rib would not seat,
    * the welder needs continuous access around the rib toe,
    * a sharp re-entrant corner is a stress raiser.
  ProSteel's own chamfered rib cuts 15mm x 15mm off each such corner
  (measured from a rib it built for me: 50,-75 -> -35,-75 -> -50,-60 -> -50,60
   -> -35,75 -> 50,75  =  a 100x150 plate with two 15x15 corners removed).

So the rule I apply, in my own model, from that understanding:
  rib contour = rectangle L x W with a CHAMFER c cut off the two corners on the
  welded edge, c = min(15, 0.2*min(L,W)) rounded to 5mm.

Holes already drilled in the plate are preserved (SetPolygon keeps them).

Usage: python reshape_ribs.py plan | run [limit]
"""
import json
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

STATE = os.path.join(ROOT, "projects", "שיעור-3-מאפס", "files", "rib_state.json")
# plate families that act as stiffeners/ribs on the RHS connections
RIB_FAMILIES = {(100, 100, 10), (150, 70, 10), (185, 90, 10), (140, 140, 10),
                (150, 150, 10), (80, 60, 6), (140, 60, 6)}


def log(m):
    try:
        sys.stdout.write(m + "\n"); sys.stdout.flush()
    except Exception:
        pass


def chamfer_for(L, W):
    """the cut-back that lets the rib seat and be welded all round"""
    c = min(15.0, 0.2 * min(L, W))
    return max(5.0, round(c / 5.0) * 5.0)


def rib_contour(L, W):
    """rectangle L x W centred on 0,0 with two corners chamfered on the -X edge"""
    hx, hy = L / 2.0, W / 2.0
    c = chamfer_for(L, W)
    return [(hx, -hy), (-hx + c, -hy), (-hx, -hy + c),
            (-hx, hy - c), (-hx + c, hy), (hx, hy)]


def load_plates():
    r = eb_api.run("dumppoly", out="eb_poly_now.txt", maxx=15000, wait=180)
    log("   dumppoly: " + str(r))
    out = []
    for line in open(os.path.join(PLUG, "eb_poly_now.txt"), encoding="utf-8-sig", errors="replace"):
        f = line.rstrip("\n").split("\t")
        if f[0] != "POLY" or len(f) < 7:
            continue
        pts = []
        for c in f[6].split(";"):
            try:
                pts.append(tuple(float(x) for x in c.split(",")))
            except Exception:
                pass
        u = []
        for p in pts:
            if not any(abs(p[0] - q[0]) < 0.5 and abs(p[1] - q[1]) < 0.5 for q in u):
                u.append(p)
        xs = [p[0] for p in u]; ys = [p[1] for p in u]
        L = round(max(xs) - min(xs)) if xs else 0
        W = round(max(ys) - min(ys)) if ys else 0
        out.append({"h": f[1], "layer": f[3], "nv": len(u), "rect": f[5],
                    "L": L, "W": W})
    return out


def main():
    mode = sys.argv[1] if len(sys.argv) > 1 else "plan"
    limit = int(sys.argv[2]) if len(sys.argv) > 2 else 10 ** 9
    log("=" * 74)
    log("RESHAPING RIBS — rectangle -> chamfered rib (holes preserved)")
    log("=" * 74)
    log(str(eb_api.run("whoami", wait=25)))

    plates = load_plates()
    log("   plates read: %d" % len(plates))
    shaped = [p for p in plates if p["nv"] > 4]
    log("   already shaped (>4 verts): %d" % len(shaped))

    fams = Counter((p["L"], p["W"]) for p in plates if p["nv"] <= 4)
    log("\n   rectangular plate families (candidates):")
    for (L, W), c in fams.most_common(12):
        mark = ""
        for f in RIB_FAMILIES:
            if sorted([L, W]) == sorted(list(f)[:2]):
                mark = "  <-- rib family"
        log("     %4dx%-4d x%-4d%s" % (L, W, c, mark))

    # pick the rib plates: the small square/rectangular connection plates
    todo = []
    for p in plates:
        if p["nv"] > 4:
            continue
        key = tuple(sorted([p["L"], p["W"]], reverse=True))
        for f in RIB_FAMILIES:
            fl = tuple(sorted(list(f)[:2], reverse=True))
            if key == fl:
                todo.append(p)
                break
    log("\n   ribs to reshape: %d" % len(todo))
    if todo:
        L, W = todo[0]["L"], todo[0]["W"]
        log("   example: %dx%d -> chamfer %gmm -> contour %s"
            % (L, W, chamfer_for(L, W),
               ";".join("%g,%g" % q for q in rib_contour(L, W))))

    if mode != "run":
        log("\nplan only — nothing changed.")
        return

    st = {"done": []}
    if os.path.exists(STATE):
        try:
            st = json.load(open(STATE, encoding="utf-8"))
        except Exception:
            pass
    done = set(st.get("done", []))

    ok = fail = skip = lost = 0
    t0 = time.time()
    for i, p in enumerate(todo[:limit], 1):
        if p["h"] in done:
            skip += 1
            continue
        pts = ";".join("%g,%g" % q for q in rib_contour(p["L"], p["W"]))
        r = eb_api.run("setpoly", handle=p["h"], pts=pts, wait=25, _log=False)
        if isinstance(r, str) and r.startswith("EB_OK"):
            ok += 1
            done.add(p["h"])
            m = re.search(r"holes (\d+)->(\d+)", r)
            if m and int(m.group(2)) < int(m.group(1)):
                lost += 1
        else:
            fail += 1
            if fail <= 5:
                log("   FAIL %s: %s" % (p["h"], str(r)[:100]))
        if i % 20 == 0 or i == min(len(todo), limit):
            log("   %d/%d ok=%d fail=%d holes-lost=%d (%.1f min)"
                % (i, min(len(todo), limit), ok, fail, lost, (time.time() - t0) / 60))
            st["done"] = sorted(done)
            json.dump(st, open(STATE, "w", encoding="utf-8"))
        time.sleep(0.05)

    st["done"] = sorted(done)
    json.dump(st, open(STATE, "w", encoding="utf-8"))
    log("\nVERIFY:")
    log("   " + str(eb_api.run("dumppoly", out="eb_poly_after_ribs.txt", maxx=15000, wait=180)))
    log("   " + str(eb_api.run("dumpholes", out="eb_holes_after_ribs.txt", wait=120)))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8-sig", errors="replace")
    except Exception:
        pass
    main()
