# -*- coding: utf-8 -*-
"""bridge_verify.py -- the acceptance gates for the bridge rebuild, on BOTH drawings.

    python app/bridge_verify.py <source.json> <rebuild.dwg> <source.dwg> [out.txt]

night_verify.py's gates, with bridge_read as the instrument so a 21,737-entity model can be
measured in minutes instead of hours. Every gate is read on the rebuild AND on the source,
because a gate whose reading on the source is unknown cannot tell "my model is wrong" from
"the checker is strict" -- that distinction is what saved model 2 and what cleared model 3.

⛔⛔ `collision` CREATES a Ks_VolBody per hit, so it is a WRITE -- and the project's COPY of the
source has THE SAME BASENAME as Bernie's original, which is open in the same AutoCAD. Since a
drawing is selected by name, "run it on the copy" would have run it on HIS FILE. So collision
runs on the REBUILD ONLY; the source side gets read-only instruments (`vfy_fit`, `vfy_dupes`)
and `build_target` is never pointed at it. Caught before it fired, 20/08/2026.
"""
import io
import json
import re
import os
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)
import eb_api        # noqa: E402
import bridge_read   # noqa: E402


def num(s):
    return [float(v) for v in str(s).split(",")]


def span(bbox):
    a, b = [num(p) for p in bbox.split(";")]
    return [b[i] - a[i] for i in range(3)]


def dist(a, b):
    return sum((a[i] - b[i]) ** 2 for i in range(3)) ** 0.5


def hole_key(h):
    a, b = num(h["a"]), num(h["b"])
    return ([(a[i] + b[i]) / 2.0 for i in range(3)], dist(a, b), float(h["d"]))


def ring(t):
    v = [tuple(round(float(x), 4) for x in q.split(",")) for q in t.split(";")]
    return v[:-1] if len(v) > 1 and v[0] == v[-1] else v


def shape_of(r):
    cx = (min(q[0] for q in r) + max(q[0] for q in r)) / 2.0
    cy = (min(q[1] for q in r) + max(q[1] for q in r)) / 2.0
    return sorted((round(q[0] - cx, 2), round(q[1] - cy, 2), q[2]) for q in r)


def visibility_gate():
    """⭐⭐ CAN THE MODEL BE SEEN? Nineteen gates measured this rebuild to 0.1 mm and not one of
    them asked that question. Amir opened the file on 21/08/2026 and saw the concrete, the grid
    and a cloud of bolts: 13,967 parts -- 7,806 plates and 6,143 shapes -- carried
    Visible=False. The only visible steel was the 68 shapes and 1 plate created that morning,
    which is how the first build was identified as the source of it.

    Geometry gates cannot catch this. A hidden part has the right section, the right length, the
    right holes and the right weight; it measures perfect and it is not there. A model you
    cannot see is not a model. ⇒ this runs on every verification, and it is a HARD failure.

    `op=classify` reads it (and `classify visible=1` fixes it in about 7 s for 20,000 parts).
    """
    r = eb_api.run("classify", wait=1200)
    if not r.startswith("EB_OK"):
        return ("visible parts", "classify refused: %s" % r[:70], False)
    # No HIDDEN token in the tally means nothing is hidden -- absence is the PASS here, so it
    # must not fall through to a sentinel that reads like a failure.
    m = re.search(r"HIDDEN=(\d+)", r)
    hidden = int(m.group(1)) if m else 0
    n = re.search(r"parts=(\d+)", r)
    total = int(n.group(1)) if n else -1
    return ("visible parts",
            "%d of %d hidden (Visible=False)%s"
            % (hidden, total,
               "" if hidden == 0 else "  <-- run: classify visible=1"),
            hidden == 0)


def gates(src, rbd, mapping):
    out = []
    R = {}
    for p in rbd["shapes"] + rbd["plates"]:
        R[p["h"]] = p

    try:
        out.append(visibility_gate())
    except Exception as e:
        out.append(("visible parts", "could not be read: %s" % str(e)[:80], False))

    mapped = set(mapping.values())
    orphans = len([p for p in (rbd["shapes"] + rbd["plates"]) if p["h"] not in mapped])
    out.append(("orphan parts", "%d in the rebuild with no source part" % orphans, orphans == 0))

    for k in ("shapes", "plates", "bolts", "holes"):
        out.append(("count %s" % k, "%d source / %d rebuild" % (len(src[k]), len(rbd[k])),
                    len(src[k]) == len(rbd[k])))

    worst = missing = 0
    worst = 0.0
    for s in src["shapes"]:
        t = mapping.get(s["h"])
        if not t or t not in R or not s.get("bbox") or not R[t].get("bbox"):
            missing += 1
            continue
        a, b = span(s["bbox"]), span(R[t]["bbox"])
        worst = max(worst, max(abs(a[i] - b[i]) for i in range(3)))
    out.append(("shape bbox SPAN", "worst %.4f mm over %d members (%d unpaired)"
                % (worst, len(src["shapes"]) - missing, missing),
                worst <= 0.01 and missing == 0))

    worst = 0.0
    for s in src["shapes"]:
        t = mapping.get(s["h"])
        if t in R and s.get("bbox") and R[t].get("bbox"):
            worst = max(worst, dist(num(s["bbox"].split(";")[0]),
                                    num(R[t]["bbox"].split(";")[0])))
    out.append(("shape bbox CORNER", "worst %.4f mm" % worst, worst <= 0.01))

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
    out.append(("plate CENTRE", "worst %.4f mm (%d unpaired)" % (wc, nm),
                wc <= 0.01 and nm == 0))
    out.append(("plate DIMS", "worst %.4f mm" % wd, wd <= 0.01))

    ncmp = bad = shifted = 0
    for a in src["plates"]:
        b = R.get(mapping.get(a["h"]))
        if not b or not a.get("pts") or not b.get("pts"):
            continue
        ncmp += 1
        if a["pts"] != b["pts"]:
            if shape_of(ring(a["pts"])) != shape_of(ring(b["pts"])):
                bad += 1
            else:
                shifted += 1
    out.append(("plate CONTOUR", "%d differ in SHAPE of %d (%d only re-centred, %d no contour)"
                % (bad, ncmp, shifted, len(src["plates"]) - ncmp), bad == 0))

    for key, lst in (("shape FRAME", src["shapes"]), ("plate FRAME", src["plates"])):
        n = w = 0
        for a in lst:
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

    ns = sum(len(v) for v in src["cuts"].values())
    nr = sum(len(v) for v in rbd["cuts"].values())
    out.append(("cut planes", "%d source / %d rebuild" % (ns, nr), ns == nr))
    ps = sum(len(v) for v in src.get("polycuts", {}).values())
    pr = sum(len(v) for v in rbd.get("polycuts", {}).values())
    out.append(("poly-cuts", "%d source / %d rebuild" % (ps, pr), ps == pr))

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

    # ---- the whole-model envelope: one number that no per-part gate can fake
    def env(d):
        lo = [1e18] * 3
        hi = [-1e18] * 3
        for e in d["entities"]:
            if not e.get("ext") or ";" not in e["ext"]:
                continue
            a, b = [num(p) for p in e["ext"].split(";")]
            for i in range(3):
                lo[i] = min(lo[i], a[i])
                hi[i] = max(hi[i], b[i])
        return lo, hi
    try:
        ls, hs = env(src)
        lr, hr = env(rbd)
        worst = max(max(abs(ls[i] - lr[i]), abs(hs[i] - hr[i])) for i in range(3))
        out.append(("model ENVELOPE", "worst corner %.3f mm  (source %s..%s)"
                    % (worst, [round(v) for v in ls], [round(v) for v in hs]),
                    worst <= 1.0))
    except Exception as e:
        out.append(("model ENVELOPE", "could not measure: %s" % e, False))
    return out


def ops_on(dwg, target, label):
    """The instrument ops. ⛔⛔ `collision` WRITES -- it creates a Ks_VolBody per hit -- and the
    project's COPY of the source has THE SAME BASENAME as Bernie's original, which is open in
    this same AutoCAD. `use(basename)` cannot tell them apart, so running collision "on the
    copy" would run it on HIS FILE. Caught before it fired, 20/08/2026.

    ⇒ **collision runs on the REBUILD only.** On the source side only read-only instruments
    run (`vfy_fit`, `vfy_dupes`), and `build_target` is never pointed at it.
    """
    eb_api.build_target(None)
    eb_api.use(os.path.basename(dwg), task="verify " + label)
    eb_api.run("list", wait=900)
    act = eb_api._active_doc_name() or ""
    if act.lower() != os.path.basename(dwg).lower():
        raise RuntimeError("not the active document: %r" % act)
    is_rebuild = "REBUILD" in act
    fit = eb_api.run("vfy_fit", wait=1800)
    dup = eb_api.run("vfy_dupes", wait=1800)
    col = "(not run: collision WRITES, and only the rebuild may be written to)"
    if is_rebuild:
        eb_api.build_target(dwg)
        col = eb_api.run("collision", minvol=100, clean=1, wait=7200)
        eb_api.build_target(None)
    return fit.strip(), dup.strip(), col.strip()


def main(argv):
    cache, rebuild, source = argv[0], argv[1], argv[2]
    out_path = argv[3] if len(argv) > 3 else None
    src = json.load(io.open(cache, encoding="utf-8"))
    mp = json.load(io.open(os.path.splitext(cache)[0] + "-map.json", encoding="utf-8"))
    print("reading the rebuild ...")
    rbd = bridge_read.read(rebuild, os.path.splitext(cache)[0] + "-rebuild.json")
    rows = gates(src, rbd, mp)
    lines = []
    failed = 0
    for name, detail, ok in rows:
        if not ok:
            failed += 1
        lines.append("%s  %-20s %s" % ("PASS" if ok else "FAIL", name, detail))
    lines.append("")
    lines.append("gates failed: %d of %d" % (failed, len(rows)))
    for dwg, tgt, lab in ((rebuild, True, "rebuild"), (source, False, "source (read-only)")):
        try:
            fit, dup, col = ops_on(dwg, tgt, lab)
            lines.append("")
            lines.append("[%s] %s" % (lab, fit[:300]))
            lines.append("[%s] %s" % (lab, dup[:300]))
            lines.append("[%s] %s" % (lab, col[:300]))
        except Exception as e:
            lines.append("[%s] could not run: %s" % (lab, e))
    text = "\n".join(lines)
    print(text)
    if out_path:
        io.open(out_path, "w", encoding="utf-8").write(text)
    return 0 if failed == 0 else 1


if __name__ == "__main__":
    if len(sys.argv) < 4:
        print(__doc__)
        sys.exit(2)
    sys.exit(main(sys.argv[1:]))
