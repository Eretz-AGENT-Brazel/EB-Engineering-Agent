# -*- coding: utf-8 -*-
"""night_read.py -- read a source model ONCE, completely, into a JSON cache.

    python app/night_read.py "<source.dwg>" "<out.json>"

Why a cache instead of reading as you build: inside an assignment set, a read with `dwg=` is a
TOTAL switch -- session, pin, channel AND the active document -- so a read aimed at the source
in the middle of a build sends every following creation into the source. Measured on
18/08/2026, and it cost eighteen parts landing in Amir's own drawing. Read first, build after.

What it captures per part: section + catalogue + endpoints + rot + mirror + bbox for shapes,
centre + dims + axes for plates, every hole (both ends and diameter), every bolt, the
modification counts, and the CUT PLANES -- the last of these only reachable through the COM
wrapper (`Ks_ComCutPlane.GetNormal()` is a METHOD; the `Normal` property raises AttributeError).
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


def iface(app, name, tries=12):
    """`GetInterfaceObject` can answer 'Call was rejected by callee' -- that is AutoCAD BUSY,
    not a missing class. Measured 18/08/2026 on model 4, where the plugin was still settling
    after ~500 `props` calls. Retry with a short back-off instead of losing the whole read."""
    import time as _t
    last = None
    for i in range(tries):
        try:
            return app.GetInterfaceObject(name)
        except Exception as e:          # pywintypes.com_error and friends
            last = e
            _t.sleep(0.5 + 0.5 * i)
    raise RuntimeError("GetInterfaceObject(%s) refused %d times: %s" % (name, tries, last))


def rows(ch, fn):
    p = os.path.join(ch, fn)
    if not os.path.exists(p):
        return []
    return [l.rstrip("\n").split("\t")
            for l in io.open(p, encoding="utf-8-sig") if l.strip()]


def read(dwg, out):
    eb_api.build_target(None)
    eb_api.use(dwg, task="night: read the source")
    eb_api.run("list")                       # gated -> activates the document
    who = eb_api.run("whoami")
    if os.path.basename(dwg).lower() not in who.lower():
        raise RuntimeError("the source is not the active document: " + who[:120])

    eb_api.run("dumpfull2")
    eb_api.run("dumpmodel")
    eb_api.run("dumpholes")
    # ⛔⛔ THE CONTOUR, and it is not optional. Until 18/08/2026 this reader took a plate's
    # L/W/H and nothing else, so every plate was rebuilt as a RECTANGLE. Measured on model 5:
    # of 314 plates only 69 are rectangles -- 237 are not, and 306 carry more than four
    # vertices. Ribs, gussets and cut-corner plates were all coming out square. `dumppoly`
    # answers for the whole model in ONE call and reports nonrect/verts>4 itself.
    eb_api.run("dumppoly")
    ch = eb_api.channel()

    d = {"source": dwg,
         "size": os.path.getsize(dwg), "mtime": os.path.getmtime(dwg),
         "shapes": [], "plates": [], "bolts": [], "holes": [], "cuts": {}, "mods": {}}

    poly = {}
    for r in rows(ch, "eb_poly.txt"):
        if r[0].endswith("POLY") and len(r) > 6:
            # local (x, y, BULGE) per vertex, closed (last == first). A bulge is tan(theta/4);
            # model 5 carries none, but a rounded corner would live here and nowhere else.
            poly[r[1]] = {"n": int(r[4]), "rect": r[5], "pts": r[6]}

    for r in rows(ch, "eb_full2.txt"):
        if r[0] == "SHAPE":
            d["shapes"].append({"h": r[1], "sec": r[2], "cat": r[3], "p1": r[4], "p2": r[5],
                                "len": r[6], "rot": r[9], "off": r[10], "mir": r[12],
                                "bbox": r[16] if len(r) > 16 else ""})
        elif r[0] == "PLATE":
            pl = {"h": r[1], "c": r[2], "dims": r[3], "axes": r[4],
                  "layer": r[5] if len(r) > 5 else ""}
            pl.update(poly.get(r[1], {}))
            d["plates"].append(pl)

    for r in rows(ch, "eb_model.txt"):
        if r[0] == "BOLT":
            d["bolts"].append({"h": r[1], "dia": r[2], "style": r[3], "len": r[5],
                               "name": r[7], "axis": r[9] if len(r) > 9 else ""})

    cur = None
    for l in io.open(os.path.join(ch, "eb_holes_all.txt"), encoding="utf-8-sig"):
        f = l.rstrip("\n").split("\t")
        if f[0] == "OBJ":
            cur = f[1]
        elif f[0] == "HOLE":
            d["holes"].append({"owner": cur, "i": f[4], "a": f[5], "b": f[6], "d": f[7]})

    # PROPS IS THE AUTHORITY on where material sits: L/W/H, the insertion origin, and the
    # section frame X/Y/Z. A bbox cannot say it -- a tilted plate's bbox is bigger than the
    # plate, and an equal angle reads identically in all four rotations. Both traps were paid
    # for on 18/08/2026, so the cache carries props for every part.
    PROPS = r"\b(L|W|H|org|X|Y|Z|mid|wt|pos|count|mir|ymir|mirrored|layer|mat)=('[^']*'|[^ ]+)"
    for part in d["shapes"] + d["plates"]:
        part["props"] = dict(re.findall(PROPS, eb_api.run("props", handle=part["h"])))

    app, doc = eb_api._app_doc()
    for part in d["shapes"] + d["plates"]:
        m = eb_api.run("mods", handle=part["h"])
        counts = dict(re.findall(
            r"(facets|cutPlanes|holeFields|outlets|polyCuts|subBodies)=(\d+)", m))
        nz = dict((k, int(v)) for k, v in counts.items() if v != "0")
        if nz:
            d["mods"][part["h"]] = nz
        if not nz.get("cutPlanes"):
            continue
        # ⚠️ AND THE WHOLE READ MUST NOT DIE ON ONE COM HICCUP. Measured 19/08/2026 on model
        # 6: GetInterfaceObject RETURNED an object whose SetObject was missing
        # (`AttributeError: GetInterfaceObject.SetObject`) -- not a refusal the retry catches,
        # a bad dispatch handed back as success -- and it killed a completed 10-minute read of
        # 1,411 entities at the cut-plane step. One part's cuts are worth far less than the
        # read: record the failure and carry on.
        try:
            em = iface(app, "PSCOMWRAPPER.Ks_ComEditModification")
            em.SetObject(doc.HandleToObject(part["h"]))
            out_cuts = []
            for i in range(em.CutPlaneCount):
                cp = iface(app, "PSCOMWRAPPER.Ks_ComCutPlane")
                em.GetCutPlane(em.GetCutPlaneHandleFromNumber(i), cp)
                out_cuts.append({"InsertPoint": [round(v, 4) for v in cp.InsertPoint],
                                 "Normal": [round(v, 8) for v in cp.GetNormal()],
                                 "Flag": cp.Flag})
            d["cuts"][part["h"]] = out_cuts
        except Exception as e:
            d.setdefault("cut_read_failures", []).append([part["h"], str(e)[:120]])

    with io.open(out, "w", encoding="utf-8") as f:
        f.write(json.dumps(d, ensure_ascii=False, indent=1))
    nrect = len([1 for p in d["plates"] if p.get("rect") == "1"])
    print("shapes %d | plates %d (%d rectangular, %d SHAPED) | bolts %d | holes %d | "
          "parts with cuts %d | mods on %d"
          % (len(d["shapes"]), len(d["plates"]), nrect, len(d["plates"]) - nrect,
             len(d["bolts"]), len(d["holes"]), len(d["cuts"]), len(d["mods"])))
    print("cached -> " + out)
    return d


if __name__ == "__main__":
    if len(sys.argv) < 3:
        print(__doc__)
        sys.exit(2)
    read(sys.argv[1], sys.argv[2])
