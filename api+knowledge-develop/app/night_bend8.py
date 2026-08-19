# -*- coding: utf-8 -*-
"""night_bend8.py -- rebuild model 5's eight folded deck pans, and verify each one.

    python app/night_bend8.py <rebuild.dwg> <bent-parsed.json> <source.json>

⭐ THE MEASURED RECIPE (19/08/2026). Each pan is a 4 mm sheet: a flat base with an upstand at
each end and a horizontal return off one of them -- `bendinfo` reports three flanges of
`len=150 ang=90 r=4 off=0/0 lenCalc=kLengthCalcModeAbsolut innerR=False`, and a developed
length of 2756.283 over a folded envelope of 2462 x W x 156.

⚠️⚠️ AND `AddFlange(len)` IS NOT THE DIALOG'S FLANGE LENGTH. Building with the source's own
150 gives an envelope of 2464 x W x **162** -- six millimetres too tall -- and **none of the
four `LengthCalculation` modes changes that** (all four measured identical; `UseInnerRadius=1`
adds a further 2 mm). The envelope comes out EXACTLY right at **upstands 144, return 148**:
    base 2300 + up 144 + up 144 + return 148  ->  2462 x W x 156, centre 0.0000 mm
so the 6 mm is a fixed reference difference (r + t/2 on this plate), not a mode.
⇒ The GEOMETRY is what the shop cuts and what the gates measure, so it is built exact. The
   cost is stated rather than hidden: the rebuilt pan's own flange fields then read 144/148,
   and `props L` reads the BASE length (2300) where the source reads the DEVELOPED 2756.283 --
   an API-built bend plate does not recompute its developed length. **A flat blank taken from
   the rebuild would be short; take it from the source.** Logged for Amir.
"""
import io
import json
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)
import eb_api  # noqa: E402

BASE, UP, RET, RAD = 2300.0, 144.0, 148.0, 4.0


def build_one(rec, tol=0.01):
    """Build one pan from its measured flange edges. Returns (handle, dims, centre)."""
    fl = rec["flanges"]
    t = float(rec["H"])
    zs = [f["e1"][2] for f in fl]
    zbot = min(zs)
    bottom = [f for f in fl if abs(f["e1"][2] - zbot) < 1]
    if not bottom:
        return None, "no bottom fold edge", ""
    b = bottom[0]
    ys = sorted([b["e1"][1], b["e2"][1]])
    ymid = (ys[0] + ys[1]) / 2.0
    x0 = b["e1"][0]
    # which way does the base run? the far top edge is at the other end
    tops = [f for f in fl if f["e1"][2] > zbot + 100]
    far = max(tops, key=lambda f: abs(f["e1"][0] - x0))
    sign = 1.0 if far["e1"][0] > x0 else -1.0
    zm = zbot + t / 2.0
    res = eb_api.run("plate9", mode="rect",
                     at="%.4f,%.4f,%.4f" % (x0 + sign * BASE / 2.0, ymid, zm),
                     l="%.4f" % BASE, w=rec["W"], t=rec["H"],
                     ex="1,0,0", ey="0,1,0", ez="0,0,1", insheight=0)
    m = re.search(r"handle=(\w+)", res)
    if not m:
        return None, res[:90], ""
    h = m.group(1)
    for at in ("%.4f,%.4f,%.4f" % (x0, ymid, zm),
               "%.4f,%.4f,%.4f" % (x0 + sign * BASE, ymid, zm)):
        q = eb_api.run("bend", handle=h, at=at, len=UP, radius=RAD, angle=90,
                       useinner=0, lengthcalc="kLengthCalcModeAbsolut")
        h = (re.search(r"bend handle=(\w+)", q) or ["", h])[1]
    # the return folds off the NEAR upstand's own top edge -- read it back from the part we
    # just built, never from the source: the click point selects WHICH edge, and a corner
    # point picks the perpendicular one (measured: it grew the width by 6 mm instead).
    info = eb_api.run("bendinfo", handle=h, max="6")
    mid = None
    for v in re.findall(r"verts=2 \(([-\d.]+),([-\d.]+),([-\d.]+)\) "
                        r"\(([-\d.]+),([-\d.]+),([-\d.]+)\)", info):
        if float(v[2]) > zbot + 100 and abs(float(v[0]) - x0) < 30:
            mid = "%.4f,%.4f,%.4f" % (float(v[0]), (float(v[1]) + float(v[4])) / 2.0,
                                      float(v[2]))
    if mid:
        q = eb_api.run("bend", handle=h, at=mid, len=RET, radius=RAD, angle=-90 * sign,
                       useinner=0, lengthcalc="kLengthCalcModeAbsolut")
        h = (re.search(r"bend handle=(\w+)", q) or ["", h])[1]
    eb_api.run("dumpfull2")
    for line in io.open(os.path.join(eb_api.channel(), "eb_full2.txt"),
                        encoding="utf-8-sig"):
        q = line.rstrip("\n").split("\t")
        if len(q) > 3 and q[1] == h:
            return h, q[3], q[2]
    return h, "?", "?"


def main(argv):
    rbd, parsed, cache = argv[0], argv[1], argv[2]
    rec = json.load(io.open(parsed, encoding="utf-8"))
    eb_api.use(rbd, task="rebuild the folded deck pans")
    eb_api.build_target(rbd)
    eb_api.run("list")
    who = eb_api.run("whoami")
    if "REBUILD" not in who:
        raise RuntimeError("not in the rebuild: " + who[:110])
    # clear whatever stands in for the pans today: every Ks_BendPlate plus any flat stand-in
    mp_path = os.path.splitext(cache)[0] + "-map.json"
    mp = json.load(io.open(mp_path, encoding="utf-8"))
    for l in io.open(os.path.join(eb_api.channel(), "eb_list.txt"), encoding="utf-8-sig"):
        p = l.rstrip().split("|")
        if len(p) > 1 and p[1].strip() == "Ks_BendPlate":
            eb_api.delete(p[0].strip())
    for src_h in rec:
        old = mp.get(src_h)
        if old:
            eb_api.delete(old)
    print("%-6s %-24s %-24s %s" % ("src", "built dims", "source dims", "verdict"))
    ok = 0
    for src_h in sorted(rec):
        r = rec[src_h]
        h, dims, ctr = build_one(r)
        def num(s):
            return [float(v) for v in s.split(",")]
        good = "-"
        if h and dims not in ("?", ""):
            try:
                dd = max(abs(num(dims)[i] - num(r["dims"])[i]) for i in range(3))
                dc = max(abs(num(ctr)[i] - num(r["c"])[i]) for i in range(3))
                good = "EXACT" if (dd <= 0.01 and dc <= 0.01) else "off %.3f/%.3f" % (dd, dc)
                if good == "EXACT":
                    ok += 1
                    mp[src_h] = h
            except Exception as e:
                good = "cmp! " + str(e)[:30]
        print("%-6s %-24s %-24s %s" % (src_h, dims, r["dims"], good))
    json.dump(mp, io.open(mp_path, "w", encoding="utf-8"))
    eb_api.run("save")
    print("%d of %d pans reproduced exactly" % (ok, len(rec)))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
