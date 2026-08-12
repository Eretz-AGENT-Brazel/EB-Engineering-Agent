# -*- coding: utf-8 -*-
"""
analyze_deep.py — turn the deep read (holes + contours) into engineering facts.

Answers, with numbers:
  1. How many holes are in the source, at which diameters, how many slotted?
  2. Which plates are genuinely NOT rectangles (ribs / cut gussets), by shape?
  3. Which parts carry holes (the connection map)?
"""
import math
import os
import sys
from collections import Counter, defaultdict

APP = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(APP)
PLUG = os.path.join(APP, "plugin")


def log(m):
    try:
        sys.stdout.write(m + "\n")
        sys.stdout.flush()
    except Exception:
        pass


def load_holes(tag):
    """OBJ<tab>handle<tab>class<tab>layer<tab>n ; HOLE<tab>handle<tab>cls<tab>layer<tab>i<tab>s<tab>e<tab>dm<tab>maxlen<tab>slot"""
    objs, holes = {}, []
    p = os.path.join(PLUG, "eb_holes_%s.txt" % tag)
    for line in open(p, encoding="utf-8-sig", errors="replace"):
        f = line.rstrip("\n").split("\t")
        if f[0] == "OBJ" and len(f) >= 5:
            objs[f[1]] = {"cls": f[2], "layer": f[3], "n": int(f[4])}
        elif f[0] == "HOLE" and len(f) >= 9:
            # tag was "handle<tab>cls<tab>layer"
            h, cls, lay = f[1], f[2], f[3]
            try:
                holes.append({"h": h, "cls": cls, "layer": lay, "i": int(f[4]),
                              "s": tuple(float(x) for x in f[5].split(",")),
                              "e": tuple(float(x) for x in f[6].split(",")),
                              "dm": float(f[7]), "maxlen": float(f[8]),
                              "slot": f[9] if len(f) > 9 else "?"})
            except Exception:
                pass
    return objs, holes


def load_poly(tag):
    """POLY<tab>handle<tab>class<tab>layer<tab>nverts<tab>rect<tab>pts"""
    out = []
    p = os.path.join(PLUG, "eb_poly_%s.txt" % tag)
    for line in open(p, encoding="utf-8-sig", errors="replace"):
        f = line.rstrip("\n").split("\t")
        if f[0] != "POLY" or len(f) < 7:
            continue
        pts = []
        for c in f[6].split(";"):
            try:
                pts.append(tuple(float(x) for x in c.split(",")))
            except Exception:
                pass
        out.append({"h": f[1], "cls": f[2], "layer": f[3],
                    "nv": int(f[4]), "rect": f[5], "pts": pts})
    return out


def dedup(pts, tol=0.5):
    """drop the repeated closing vertex and duplicates"""
    u = []
    for p in pts:
        if not any(math.dist(p, q) < tol for q in u):
            u.append(p)
    return u


def is_rect(pts, tol=1.0):
    """geometric test on the deduped contour: 4 corners, right angles"""
    u = dedup(pts)
    if len(u) != 4:
        return False
    for i in range(4):
        a, b, c = u[i], u[(i + 1) % 4], u[(i + 2) % 4]
        v1 = tuple(b[k] - a[k] for k in range(3))
        v2 = tuple(c[k] - b[k] for k in range(3))
        n1 = math.sqrt(sum(x * x for x in v1))
        n2 = math.sqrt(sum(x * x for x in v2))
        if n1 < tol or n2 < tol:
            return False
        dot = sum(v1[k] * v2[k] for k in range(3)) / (n1 * n2)
        if abs(dot) > 0.02:          # ~1.1 degrees off square
            return False
    return True


def shape_sig(pts):
    u = dedup(pts)
    xs = [p[0] for p in u]; ys = [p[1] for p in u]; zs = [p[2] for p in u]
    return len(u), (round(max(xs) - min(xs)), round(max(ys) - min(ys)), round(max(zs) - min(zs)))


def main():
    tag = sys.argv[1] if len(sys.argv) > 1 else "src"
    log("=" * 74)
    log("DEEP ANALYSIS of the SOURCE model — holes and real plate shapes")
    log("=" * 74)

    objs, holes = load_holes(tag)
    log("\n### 1. HOLES")
    log("parts carrying holes : %d" % sum(1 for o in objs.values() if o["n"] > 0))
    log("total holes          : %d" % len(holes))
    dia = Counter(round(h["dm"], 1) for h in holes)
    log("diameters            : %s" % ", ".join("%.1f x%d" % (d, n) for d, n in sorted(dia.items())))
    log("slotted flag         : %s" % dict(Counter(h["slot"] for h in holes)))
    ml = Counter(round(h["maxlen"]) for h in holes)
    log("maximal-length vals  : %s" % dict(sorted(ml.items())[:8]))
    byc = Counter(h["cls"] for h in holes)
    log("holes by host class  : %s" % dict(byc))
    byl = Counter(h["layer"] for h in holes)
    log("holes by host layer  : %s" % dict(byl.most_common(6)))
    # hole length = distance start..end -> tells the grip / plate stack thickness
    grip = Counter(round(math.dist(h["s"], h["e"])) for h in holes)
    log("hole depth (grip)    : %s" % dict(sorted(grip.items())[:10]))

    log("\n### 2. PLATE SHAPES (contour, not bbox)")
    poly = load_poly(tag)
    log("plates read          : %d" % len(poly))
    geo_rect = [p for p in poly if is_rect(p["pts"])]
    geo_non = [p for p in poly if not is_rect(p["pts"])]
    log("PS RectangleMode=0   : %d" % sum(1 for p in poly if p["rect"] == "0"))
    log("GEOMETRICALLY rect   : %d" % len(geo_rect))
    log("GEOMETRICALLY shaped : %d   <-- ribs / cut gussets / notched plates" % len(geo_non))
    nv = Counter(len(dedup(p["pts"])) for p in poly)
    log("unique-vertex counts : %s" % dict(sorted(nv.items())))

    log("\n--- shaped-plate families (verts, bbox) ---")
    fam = Counter()
    for p in geo_non:
        n, bb = shape_sig(p["pts"])
        fam[(n, bb, p["layer"])] += 1
    for (n, bb, lay), c in fam.most_common(18):
        log("  %2d verts  %-22s %-12s x%d" % (n, "x".join(str(v) for v in bb), lay, c))

    log("\n--- the connection targets Fable identified ---")
    # split-column splice at (2755,274,z~3162) and UPN gussets near (2572,3283,6380)
    zones = {"splice_bolted (2755,274,3162)": (2755, 274, 3162),
             "splice_welded (219,274,5898)": (219, 274, 5898),
             "UPN gusset A (2572,3283,6380)": (2572, 3283, 6380),
             "UPN gusset B (2527,8084,6380)": (2527, 8084, 6380)}
    for name, c in zones.items():
        near = []
        for p in poly:
            u = dedup(p["pts"])
            if not u:
                continue
            ctr = tuple(sum(q[k] for q in u) / len(u) for k in range(3))
            if math.dist(ctr, c) < 500:
                n, bb = shape_sig(p["pts"])
                near.append((p["h"], n, bb, is_rect(p["pts"]), p["layer"]))
        nh = sum(o["n"] for hh, o in objs.items() if False)  # placeholder
        log("\n  %s : %d plates within 500mm" % (name, len(near)))
        for h, n, bb, r, lay in near[:12]:
            hol = objs.get(h, {}).get("n", 0)
            log("     %s  %2d verts  %-20s %-9s holes=%d" %
                (h, n, "x".join(str(v) for v in bb), "RECT" if r else "SHAPED", hol))

    log("\n### 3. WHAT THIS MEANS FOR THE FIX")
    log("  * lesson-3 currently has 0 holes; the source has %d  -> drill all of them" % len(holes))
    log("  * %d of %d plates are shaped, not rectangles -> rebuild them from contour"
        % (len(geo_non), len(poly)))
    log("  * slotted holes in source: %s" %
        ("NONE - all round" if all(h["slot"] != "1" for h in holes) else "present"))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8-sig", errors="replace")
    except Exception:
        pass
    main()
