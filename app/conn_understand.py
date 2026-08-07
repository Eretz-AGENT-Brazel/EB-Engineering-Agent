# -*- coding: utf-8 -*-
"""
conn_understand.py — read the connections of a model and UNDERSTAND them.

A connection in ProSteel is a parametric object (a "logical link") that owns its
own recipe: plate sizes, hole diameter and spacing, welds, bolts, rib shape.
This turns the raw connscan dump into an engineering catalogue of joint TYPES,
so the model can be rebuilt from joint recipes instead of loose plates.
"""
import os
import re
import sys
from collections import Counter, defaultdict

APP = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(APP)
PLUG = os.path.join(APP, "plugin")

TYPE_NAMES = {}


def log(m):
    try:
        sys.stdout.write(m + "\n")
        sys.stdout.flush()
    except Exception:
        pass


def parse_block(line, tag):
    """pull BASEPLATE[...] / RIB[...] / SPLICE[...] key=val pairs"""
    m = re.search(re.escape(tag) + r"\[([^\]]*)\]", line)
    if not m:
        return None
    d = {}
    for kv in m.group(1).split():
        if "=" in kv:
            k, v = kv.split("=", 1)
            try:
                d[k] = float(v)
            except ValueError:
                d[k] = v
    return d


def nonzero(d):
    return d and any(v not in (0, 0.0, "0") for v in d.values())


def main():
    tag = sys.argv[1] if len(sys.argv) > 1 else "src"
    path = os.path.join(PLUG, "eb_conn_%s.txt" % tag)
    links, membs = [], {}
    for line in open(path, encoding="utf-8-sig", errors="replace"):
        f = line.rstrip("\n").split("\t")
        if f[0] == "LINK" and len(f) >= 6:
            body = f[5]
            t = re.search(r"type=(-?\d+)", body)
            nm = re.search(r"name=(.*?)\s+ident=", body)
            ident = re.search(r"ident=(.*?)\s+desc=", body)
            parts = re.search(r"parts=(\d+) bolts=(\d+) extra=(\d+)", body)
            links.append({
                "host": f[1], "cls": f[2], "layer": f[3], "num": f[4],
                "type": int(t.group(1)) if t else None,
                "name": (nm.group(1).strip() if nm else "?"),
                "ident": (ident.group(1).strip() if ident else ""),
                "parts": int(parts.group(1)) if parts else 0,
                "bolts": int(parts.group(2)) if parts else 0,
                "base": parse_block(body, "BASEPLATE"),
                "rib": parse_block(body, "RIB"),
                "splice": parse_block(body, "SPLICE"),
            })
        elif f[0] == "MEMB" and len(f) >= 4:
            membs[(f[1], f[2])] = line

    real = [l for l in links if l["type"] not in (None, 0) or l["parts"] or l["bolts"]]
    log("=" * 76)
    log("CONNECTION CATALOGUE — what the joints in this model actually are")
    log("=" * 76)
    log("links found: %d   (of which carry a real type/parts/bolts: %d)" % (len(links), len(real)))

    log("\n### joint types present")
    byname = Counter((l["type"], l["name"]) for l in links)
    for (t, n), c in sorted(byname.items(), key=lambda x: -x[1]):
        log("  type=%-3s %-28s x%d" % (t, n, c))

    log("\n### joints that carry parts/bolts (the ones that actually built steel)")
    withwork = [l for l in links if l["parts"] or l["bolts"]]
    log("  count: %d" % len(withwork))
    agg = Counter()
    for l in withwork:
        agg[(l["name"], l["parts"], l["bolts"])] += 1
    for (n, p, b), c in sorted(agg.items(), key=lambda x: -x[1]):
        log("  %-28s parts=%d bolts=%d   x%d" % (n, p, b, c))

    log("\n### BASE PLATE recipes (the real parameters Amir used)")
    seen = Counter()
    for l in links:
        if nonzero(l["base"]):
            d = l["base"]
            key = (d.get("L"), d.get("W"), d.get("t"), d.get("holeDia"),
                   d.get("hx"), d.get("hy"), d.get("anchors"), d.get("anchorDia"))
            seen[key] += 1
    for k, c in seen.most_common():
        log("  %gx%gx%g  holeDia=%g  spacing %gx%g  anchors=%g (dia %g)   x%d"
            % (k[0], k[1], k[2], k[3], k[4], k[5], k[6], k[7], c))
    if not seen:
        log("  (none carried non-zero base-plate data)")

    log("\n### RIB (stiffener) recipes")
    seenr = Counter()
    for l in links:
        if nonzero(l["rib"]):
            d = l["rib"]
            seenr[(d.get("t"), d.get("len"), d.get("shape"), d.get("r"),
                   d.get("flDist"), d.get("webDist"), d.get("ang"))] += 1
    for k, c in seenr.most_common():
        log("  t=%g len=%g shape=%g r=%g flDist=%g webDist=%g angle=%g   x%d"
            % (k[0], k[1], k[2], k[3], k[4], k[5], k[6], c))
    if not seenr:
        log("  (none carried non-zero rib data)")

    log("\n### SPLICE recipes")
    seens = Counter()
    for l in links:
        if nonzero(l["splice"]):
            d = l["splice"]
            seens[(d.get("gap"), d.get("holeDia"), d.get("tWeb"), d.get("tFl"),
                   d.get("nH_web"), d.get("nV_web"), d.get("nH_fl"), d.get("nV_fl"))] += 1
    for k, c in seens.most_common():
        log("  gap=%g holeDia=%g tWeb=%g tFl=%g webHoles=%gx%g flangeHoles=%gx%g   x%d"
            % (k[0], k[1], k[2], k[3], k[4], k[5], k[6], k[7], c))
    if not seens:
        log("  (none carried non-zero splice data)")

    log("\n### per-host summary (which parts carry joints)")
    byhost = defaultdict(list)
    for l in links:
        byhost[l["host"]].append(l)
    multi = [(h, v) for h, v in byhost.items() if len(v) > 1]
    log("  hosts with joints: %d   hosts with >1 joint: %d" % (len(byhost), len(multi)))
    for h, v in sorted(byhost.items())[:14]:
        names = ", ".join("%s(p%d/b%d)" % (x["name"], x["parts"], x["bolts"]) for x in v)
        log("    %s : %s" % (h, names))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8-sig", errors="replace")
    except Exception:
        pass
    main()
