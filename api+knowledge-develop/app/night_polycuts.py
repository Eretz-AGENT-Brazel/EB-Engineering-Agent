# -*- coding: utf-8 -*-
"""night_polycuts.py -- read a model's poly-cuts, and apply them to the rebuild.

    python app/night_polycuts.py read  <source.dwg> <source.json> <out.json>
    python app/night_polycuts.py apply <rebuild.dwg> <out.json> <source.json>

⛔ WHY THIS EXISTS. `night_read` captured cut PLANES and never poly-cuts, so a rebuild came out
with **zero** of the source's **18** poly-cuts. Fourteen of them are invisible to every gate --
this shop models some holes as poly-cuts (measured on model 1, and Amir: "לא זה לא משפיע") --
but four of them TRIM the plate, and that is what the `plate DIMS` gate caught: a plate whose
contour matched byte for byte and whose centre matched to 0.0000 still read 159.901 against
164.46, because the source's own contour is cut back and the rebuild's is not.

The reader is the COM wrapper, because .NET has no reader for it:
    em = app.GetInterfaceObject("PSCOMWRAPPER.Ks_ComEditModification"); em.SetObject(obj)
    em.PolyCutCount ; em.GetPolyCut(em.GetPolyCutHandleFromNumber(i), pc)
    pc.GetPolygon(pg) ; pg.VertexCount ; pg.GetVertex(k) -> (x, y, BULGE)
⚠️ The third component of a vertex is the BULGE, not z.
"""
import io
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)
import eb_api  # noqa: E402


def read(dwg, cache, out):
    d = json.load(io.open(cache, encoding="utf-8"))
    owners = [h for h, m in d["mods"].items() if m.get("polyCuts")]
    eb_api.build_target(None)
    eb_api.use(dwg, task="read the poly-cuts")
    eb_api.run("list")
    who = eb_api.run("whoami")
    if "REBUILD" in who:
        raise RuntimeError("that is the rebuild, not the source: " + who[:100])
    app, doc = eb_api._app_doc()
    got = {}
    for h in owners:
        try:
            em = app.GetInterfaceObject("PSCOMWRAPPER.Ks_ComEditModification")
            em.SetObject(doc.HandleToObject(h))
            n = em.PolyCutCount
            cuts = []
            for i in range(n):
                pc = app.GetInterfaceObject("PSCOMWRAPPER.Ks_ComPolyCut")
                em.GetPolyCut(em.GetPolyCutHandleFromNumber(i), pc)
                pg = app.GetInterfaceObject("PSCOMWRAPPER.Ks_ComPolygon")
                pc.GetPolygon(pg)
                verts = []
                for k in range(pg.VertexCount):
                    verts.append([round(v, 4) for v in pg.GetVertex(k)])
                cuts.append({
                    "ins": [round(v, 4) for v in pc.InsertPoint],
                    "end": [round(v, 4) for v in pc.EndPoint],
                    "xaxis": [round(v, 6) for v in pc.Xaxis],
                    "yaxis": [round(v, 6) for v in pc.Yaxis],
                    "flag": pc.Flag,
                    "verts": verts,
                })
            got[h] = cuts
            print("%s: %d poly-cut(s), %s" % (h, n, [len(c["verts"]) for c in cuts]))
        except Exception as e:
            print("%s: read failed -- %s" % (h, str(e)[:80]))
    json.dump(got, io.open(out, "w", encoding="utf-8"), indent=1)
    print("read %d owners -> %s" % (len(got), out))
    return got


def apply(rebuild, data, cache):
    got = json.load(io.open(data, encoding="utf-8"))
    mp = json.load(io.open(os.path.splitext(cache)[0] + "-map.json", encoding="utf-8"))
    eb_api.use(rebuild, task="apply the poly-cuts")
    eb_api.build_target(rebuild)
    eb_api.run("list")
    who = eb_api.run("whoami")
    if "REBUILD" not in who:
        raise RuntimeError("not the rebuild: " + who[:100])
    ok = fail = 0
    for src_h, cuts in got.items():
        t = mp.get(src_h)
        if not t:
            print("%s: no rebuild counterpart" % src_h)
            continue
        for c in cuts:
            pts = ";".join("%.4f,%.4f" % (v[0], v[1]) for v in c["verts"])
            depth = ((c["end"][0] - c["ins"][0]) ** 2 + (c["end"][1] - c["ins"][1]) ** 2 +
                     (c["end"][2] - c["ins"][2]) ** 2) ** 0.5
            r = eb_api.run("polycut", handle=t, shape="pts", pts=pts,
                           at="%.4f,%.4f,%.4f" % tuple(c["ins"]),
                           xaxis="%.6f,%.6f,%.6f" % tuple(c["xaxis"]),
                           yaxis="%.6f,%.6f,%.6f" % tuple(c["yaxis"]),
                           depth=round(depth, 4))
            if r.startswith("EB_OK"):
                ok += 1
            else:
                fail += 1
                if fail <= 4:
                    print("   %s -> %s" % (src_h, r[:110]))
    print("poly-cuts applied: %d ok, %d refused" % (ok, fail))
    eb_api.run("save")
    return ok, fail


if __name__ == "__main__":
    if len(sys.argv) < 5:
        print(__doc__)
        sys.exit(2)
    if sys.argv[1] == "read":
        read(sys.argv[2], sys.argv[3], sys.argv[4])
    else:
        apply(sys.argv[2], sys.argv[3], sys.argv[4])
