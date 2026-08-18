# -*- coding: utf-8 -*-
"""night_verify.py -- the acceptance gates for a 1:1 rebuild, run on BOTH drawings.

    python app/night_verify.py <source.json> <rebuild.dwg> <source.dwg>

Every gate is measured on the rebuild AND on the source, because a gate whose reading on the
source is unknown cannot tell "my model is wrong" from "the checker is strict". That distinction
is what saved model 2 on 18/08/2026: `vfy_fit` read BOLT-NO-HOLE=12 on the rebuild and 0 on the
source, so the rebuild was wrong; on model 3 the same flag read 24 on BOTH, so it was faithful.

The instrument on both sides is `night_read.read()` -- the same reader, so a difference in the
numbers is a difference in the models and never a difference in how they were measured.

⚠️ `collision` CREATES a Ks_VolBody per hit, so it is a WRITE. It is therefore run under
build_target for the rebuild, and on the source only in the project's COPY of it, which is
never saved.
"""
import io
import json
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)
import eb_api        # noqa: E402
import night_read    # noqa: E402


def num(s):
    return [float(v) for v in str(s).split(",")]


def span(bbox):
    a, b = [num(p) for p in bbox.split(";")]
    return [b[i] - a[i] for i in range(3)]


def dist(a, b):
    return sum((a[i] - b[i]) ** 2 for i in range(3)) ** 0.5


def hole_key(h):
    a, b = num(h["a"]), num(h["b"])
    return ([(a[i] + b[i]) / 2.0 for i in range(3)],      # midpoint
            dist(a, b),                                    # depth
            float(h["d"]))                                 # diameter


def compare(src, rbd, mapping):
    """mapping: source handle -> rebuild handle. Returns (lines, worst numbers)."""
    out = []
    R = {}
    for p in rbd["shapes"]:
        R[p["h"]] = p
    for p in rbd["plates"]:
        R[p["h"]] = p

    # ---- counts
    for k in ("shapes", "plates", "bolts", "holes"):
        out.append(("count %s" % k, "%d source / %d rebuild" % (len(src[k]), len(rbd[k])),
                    len(src[k]) == len(rbd[k])))

    # ---- shapes: the bbox SPAN, which is the only thing that sees a member turned 90 deg
    worst = 0.0
    missing = 0
    for s in src["shapes"]:
        t = mapping.get(s["h"])
        if not t or t not in R or not s["bbox"] or not R[t].get("bbox"):
            missing += 1
            continue
        a, b = span(s["bbox"]), span(R[t]["bbox"])
        worst = max(worst, max(abs(a[i] - b[i]) for i in range(3)))
    out.append(("shape bbox SPAN", "worst %.4f mm over %d members (%d unpaired)"
                % (worst, len(src["shapes"]) - missing, missing),
                worst <= 0.01 and missing == 0))

    # ---- shapes: where the member sits
    worst = 0.0
    for s in src["shapes"]:
        t = mapping.get(s["h"])
        if t in R and s["bbox"] and R[t].get("bbox"):
            a = num(s["bbox"].split(";")[0])
            b = num(R[t]["bbox"].split(";")[0])
            worst = max(worst, dist(a, b))
    out.append(("shape bbox CORNER", "worst %.4f mm" % worst, worst <= 0.01))

    # ---- plates: centre and dimensions
    wc = wd = 0.0
    nm = 0
    for p in src["plates"]:
        t = mapping.get(p["h"])
        if not t or t not in R:
            nm += 1
            continue
        wc = max(wc, dist(num(p["c"]), num(R[t]["c"])))
        a, b = num(p["dims"]), num(R[t]["dims"])
        wd = max(wd, max(abs(a[i] - b[i]) for i in range(3)))
    out.append(("plate CENTRE", "worst %.4f mm (%d unpaired)" % (wc, nm), wc <= 0.01 and nm == 0))
    out.append(("plate DIMS", "worst %.4f mm" % wd, wd <= 0.01))

    # ---- cut planes
    ns = sum(len(v) for v in src["cuts"].values())
    nr = sum(len(v) for v in rbd["cuts"].values())
    # ⛔⛔ THE TWO GATES WHOSE ABSENCE LET A WHOLE MODEL PASS WRONG (18/08/2026).
    # A rib and the rectangle around it agree on every gate above -- bbox, centre, dims,
    # thickness, even hole positions -- so 245 plates were rebuilt square and nothing said so
    # until Amir said it. And an equal angle reads identically in all four rotations, so 112
    # members carried the wrong section frame while the span gate stayed green.
    # A gate that cannot fail is not a gate.
    ncmp = bad = 0
    for a in src["plates"]:
        b = R.get(mapping.get(a["h"]))
        if not b or not a.get("pts") or not b.get("pts"):
            continue
        ncmp += 1
        if a["pts"] != b["pts"]:
            bad += 1
    out.append(("plate CONTOUR", "%d differ of %d compared (%d without a contour)"
                % (bad, ncmp, len(src["plates"]) - ncmp), bad == 0))

    for key, lst_s in (("shape FRAME", src["shapes"]),
                       ("plate FRAME", src["plates"])):
        n = w = 0
        for a in lst_s:
            b = R.get(mapping.get(a["h"]))
            if not b:
                continue
            fa = tuple(a.get("props", {}).get(k) for k in "XYZ")
            fb = tuple(b.get("props", {}).get(k) for k in "XYZ")
            if None in fa or None in fb:
                continue
            n += 1
            if fa != fb:
                w += 1
        out.append((key, "%d of %d differ" % (w, n), w == 0))

    out.append(("cut planes", "%d source / %d rebuild" % (ns, nr), ns == nr))

    # ---- holes: nearest match, and its depth and diameter
    mine = {}
    for h in rbd["holes"]:
        mine.setdefault(h["owner"], []).append(hole_key(h))
    wpos = wdep = 0.0
    baddia = off = orphan = 0
    for h in src["holes"]:
        t = mapping.get(h["owner"])
        if not t:
            orphan += 1
            continue
        mid, dep, dia = hole_key(h)
        cand = mine.get(t, [])
        if not cand:
            off += 1
            continue
        best = min(cand, key=lambda c: dist(mid, c[0]))
        d = dist(mid, best[0])
        wpos = max(wpos, d)
        if d > 0.01:
            off += 1
        wdep = max(wdep, abs(dep - best[1]))
        if abs(dia - best[2]) > 0.01:
            baddia += 1
    out.append(("hole POSITION", "worst %.4f mm, %d off, %d on unpaired parts"
                % (wpos, off, orphan), wpos <= 0.01 and off == 0 and orphan == 0))
    out.append(("hole DEPTH", "worst %.4f mm" % wdep, wdep <= 0.01))
    out.append(("hole DIAMETER", "%d mismatches" % baddia, baddia == 0))

    # ---- bolts: match on the AXIS, never the midpoint (ProSteel seats its own bolt)
    def axis(b):
        if ";" not in (b.get("axis") or ""):
            return None
        p = b["axis"].split(";")
        return num(p[0]), num(p[1])

    have = [a for a in (axis(b) for b in rbd["bolts"]) if a]
    unmatched = 0
    for b in src["bolts"]:
        a = axis(b)
        if not a:
            continue
        if not any(dist(a[0], h[0]) < 3.0 or dist(a[0], h[1]) < 3.0 for h in have):
            unmatched += 1
    out.append(("bolt axes", "%d source bolts with no rebuild bolt on their axis" % unmatched,
                unmatched == 0))
    return out


def ops_on(dwg, target, label):
    """vfy_fit + collision, in one drawing. `target` gates the writes collision performs."""
    eb_api.build_target(None)
    eb_api.use(dwg, task="night: verify " + label)
    eb_api.run("list")                       # gated -> activates the document
    who = eb_api.run("whoami")
    if os.path.basename(dwg).lower() not in who.lower():
        raise RuntimeError("not the active document: " + who[:120])
    fit = eb_api.run("vfy_fit", wait=90)
    if target:
        eb_api.build_target(dwg)
    col = eb_api.run("collision", minvol=100, clean=1, wait=180)
    eb_api.build_target(None)
    return fit.strip(), col.strip()


def main(argv):
    cache, rebuild, source = argv[0], argv[1], argv[2]
    src = json.load(io.open(cache, encoding="utf-8"))
    mp = json.load(io.open(os.path.splitext(cache)[0] + "-map.json", encoding="utf-8"))

    out_json = os.path.join(os.path.dirname(cache), "rebuild.json")
    rbd = night_read.read(rebuild, out_json)

    print("")
    print("%-22s %-56s %s" % ("GATE", "MEASURED", ""))
    fails = 0
    for name, text, ok in compare(src, rbd, mp):
        print("%-22s %-56s %s" % (name, text, "OK" if ok else "**FAIL**"))
        fails += 0 if ok else 1

    for dwg, lab, tgt in ((rebuild, "rebuild", True), (source, "source", False)):
        fit, col = ops_on(dwg, tgt, lab)
        print("")
        print("[%s] %s" % (lab, re.sub(r"\s+", " ", fit)[:180]))
        print("[%s] %s" % (lab, re.sub(r"\s+", " ", col)[:180]))
    print("")
    print("gates failed: %d" % fails)
    return 0


if __name__ == "__main__":
    if len(sys.argv) < 4:
        print(__doc__)
        sys.exit(2)
    sys.exit(main(sys.argv[1:]))
