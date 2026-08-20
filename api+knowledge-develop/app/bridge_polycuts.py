# -*- coding: utf-8 -*-
"""bridge_polycuts.py -- read a model's poly-cuts COMPLETELY, and apply them to the rebuild.

    python app/bridge_polycuts.py read  <source.dwg> <source.json> <out.json>
    python app/bridge_polycuts.py apply <rebuild.dwg> <out.json> <source.json>

⛔ WHY IT IS A FILE OF ITS OWN. `bridge_read.py` captured the poly-cut POLYGON and guessed at
the rest, asking `Ks_ComPolyCut` for `Depth`/`Origin` -- neither of which exists. The op then
received no depth, and **3,827 of 3,827 poly-cuts refused with `depth=0`**: `applyRc=0`,
`p0 -> p0`, nothing cut, nothing broken, and every failure identical. The names are
**`InsertPoint`, `EndPoint`, `Xaxis`, `Yaxis`, `Flag`** -- and the DEPTH is the distance
between the two points. A polygon without its plane and its depth is not a cut.

⚠️ The third component of a vertex is the BULGE, not z -- and `polycut shape=pts` takes the
2D pair; the bulge belongs to the arc case, which arrives as two bulge-1.0 vertices and must
go back as `shape=circle`.
"""
import io
import json
import os
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)
import eb_api  # noqa: E402


def as_circle(verts):
    pts, seen = [], set()
    for v in verts:
        k = (round(v[0], 4), round(v[1], 4))
        if k not in seen:
            seen.add(k)
            pts.append(v)
    if len(pts) != 2 or not all(abs(abs(v[2]) - 1.0) < 1e-6 for v in pts):
        return None
    (x0, y0), (x1, y1) = pts[0][:2], pts[1][:2]
    r = ((x1 - x0) ** 2 + (y1 - y0) ** 2) ** 0.5 / 2.0
    return None if r <= 0 else ((x0 + x1) / 2.0, (y0 + y1) / 2.0, r)


def read(dwg, cache, out):
    d = json.load(io.open(cache, encoding="utf-8"))
    owners = [h for h, m in d["mods"].items() if m.get("polyCuts")]
    eb_api.build_target(None)
    eb_api.use(os.path.basename(dwg), task="READ ONLY - poly-cut geometry")
    eb_api.run("list", wait=600)
    act = eb_api._active_doc_name() or ""
    if "REBUILD" in act:
        raise RuntimeError("that is the rebuild, not the source: " + act)
    app, doc = eb_api._app_doc()

    def iface(n, tries=8):
        last = None
        for i in range(tries):
            try:
                return app.GetInterfaceObject(n)
            except Exception as e:
                last = e
                time.sleep(0.3 + 0.3 * i)
        raise RuntimeError(str(last))

    got, fails = {}, []
    t0 = time.time()
    for k, h in enumerate(owners):
        try:
            em = iface("PSCOMWRAPPER.Ks_ComEditModification")
            em.SetObject(doc.HandleToObject(h))
            cuts = []
            for i in range(em.PolyCutCount):
                pc = iface("PSCOMWRAPPER.Ks_ComPolyCut")
                em.GetPolyCut(em.GetPolyCutHandleFromNumber(i), pc)
                pg = iface("PSCOMWRAPPER.Ks_ComPolygon")
                pc.GetPolygon(pg)
                cuts.append(dict(
                    ins=[round(v, 4) for v in pc.InsertPoint],
                    end=[round(v, 4) for v in pc.EndPoint],
                    xaxis=[round(v, 6) for v in pc.Xaxis],
                    yaxis=[round(v, 6) for v in pc.Yaxis],
                    flag=pc.Flag,
                    verts=[[round(c, 5) for c in pg.GetVertex(j)]
                           for j in range(pg.VertexCount)]))
            got[h] = cuts
        except Exception as e:
            fails.append([h, str(e)[:120]])
        if k and k % 500 == 0:
            print("   %d/%d  %.0fs" % (k, len(owners), time.time() - t0))
    json.dump(got, io.open(out, "w", encoding="utf-8"))
    print("read %d owners, %d cuts, %d failed, %.0fs -> %s"
          % (len(got), sum(len(v) for v in got.values()), len(fails), time.time() - t0, out))
    if fails:
        print("   first failure: %s" % fails[0])
    return got


def apply(rebuild, data, cache):
    got = json.load(io.open(data, encoding="utf-8"))
    mp = json.load(io.open(os.path.splitext(cache)[0] + "-map.json", encoding="utf-8"))
    eb_api.use(os.path.basename(rebuild), task="apply the poly-cuts", project="bridge-bernie")
    eb_api.build_target(rebuild)
    eb_api.run("list", wait=600)
    act = eb_api._active_doc_name() or ""
    if "REBUILD" not in act:
        raise RuntimeError("not the rebuild: " + act)
    items, notes = [], []
    circles = 0
    for src_h, cuts in got.items():
        t = mp.get(src_h)
        if not t:
            notes.append("%s: no rebuild counterpart" % src_h)
            continue
        for c in cuts:
            depth = sum((c["end"][i] - c["ins"][i]) ** 2 for i in range(3)) ** 0.5
            kw = dict(handle=t,
                      at="%.4f,%.4f,%.4f" % tuple(c["ins"]),
                      xaxis="%.6f,%.6f,%.6f" % tuple(c["xaxis"]),
                      yaxis="%.6f,%.6f,%.6f" % tuple(c["yaxis"]),
                      depth=round(depth, 4))
            circ = as_circle(c["verts"])
            if circ:
                cx, cy, rad = circ
                if abs(cx) > 1e-4 or abs(cy) > 1e-4:
                    ins, xa, ya = c["ins"], c["xaxis"], c["yaxis"]
                    kw["at"] = "%.4f,%.4f,%.4f" % tuple(
                        ins[i] + cx * xa[i] + cy * ya[i] for i in range(3))
                kw.update(shape="circle", r=round(rad, 4))
                circles += 1
            else:
                kw.update(shape="pts",
                          pts=";".join("%.4f,%.4f" % (v[0], v[1]) for v in c["verts"]))
            items.append(("polycut", kw))
    print("poly-cuts: %d to apply (%d circles) on %d parts"
          % (len(items), circles, len([1 for h in got if h in mp])))
    ok = 0
    t0 = time.time()
    for a in range(0, len(items), 500):
        rows = eb_api.batch(items[a:a + 500], wait=7200)
        ok += len([1 for (i, o, r) in rows if r.startswith("EB_OK")])
        for (i, o, r) in rows:
            if not r.startswith("EB_OK"):
                notes.append("polycut: " + r[:170])
        print("   %d/%d applied, %.0fs" % (ok, len(items), time.time() - t0))
        eb_api.run("save", wait=1800)
    print("poly-cuts: %d/%d applied" % (ok, len(items)))
    io.open(os.path.splitext(data)[0] + "-notes.txt", "w",
            encoding="utf-8").write("\n".join(notes))
    return ok


if __name__ == "__main__":
    if len(sys.argv) < 5:
        print(__doc__)
        sys.exit(2)
    if sys.argv[1] == "read":
        read(sys.argv[2], sys.argv[3], sys.argv[4])
    elif sys.argv[1] == "apply":
        apply(sys.argv[2], sys.argv[3], sys.argv[4])
    else:
        print(__doc__)
        sys.exit(2)
