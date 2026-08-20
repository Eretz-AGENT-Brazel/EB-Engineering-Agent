# -*- coding: utf-8 -*-
"""bridge_read.py -- read a LARGE source model completely into one JSON cache.

    python app/bridge_read.py "<source.dwg>" "<out.json>"

Written for the bridge model (21,737 entities) on 20/08/2026, when `night_read.py` -- which
asks `props` and `mods` per part over the file protocol -- came out at **2.1 hours** for the
read alone. Same numbers, different plumbing:

  * `dumpparts`  (v188) -- props + modification COUNTS for every entity in ONE call: 5.3 s
  * `batch`      (v190) -- the per-part `mods` DETAIL (facets, outlets, break edges) for
                 14,044 parts in one round trip instead of 14,044
  * COM          -- cut planes and poly-cut polygons, which have no .NET reader at all
                 (measured 0.032 s and 0.043 s per part: ~1.3 min each, so no plugin op
                 was written for them -- measure before building)

⚠️ READ FIRST, BUILD AFTER. A read aimed at the source with `dwg=` switches session, pin,
channel AND the active document, so a read in the middle of a build sends the next creation
into the SOURCE. That cost eighteen parts landing in Amir's own drawing on 18/08/2026.
"""
import io
import json
import os
import re
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)
import eb_api  # noqa: E402

PROPS = (r"\b(L|W|H|org|X|Y|Z|mid|wt|pos|count|mir|ymir|mirrored|layer|mat|name|key|cat|"
         r"dia|lenAdd|ins|scale|ext|objType|paintArea)=('[^']*'|[^ ]+)")


def rows(ch, fn, sep="\t"):
    p = os.path.join(ch, fn)
    if not os.path.exists(p):
        return []
    return [l.rstrip("\n").split(sep) for l in io.open(p, encoding="utf-8-sig") if l.strip()]


def parse_props(text):
    return dict((k, v.strip("'")) for k, v in re.findall(PROPS, text))


def read(dwg, out):
    t0 = time.time()
    eb_api.build_target(None)
    eb_api.use(os.path.basename(dwg), task="READ ONLY - source of the 1:1 rebuild")
    eb_api.run("list", wait=180)          # gated -> activates the pinned document
    # ⚠️ ASK COM WHICH DOCUMENT IS ACTIVE, NOT `whoami` -- that op is a diagnostic and is not
    # gated, so it answers for whatever window is in front (measured 20/08/2026).
    act = eb_api._active_doc_name() or ""
    if act.lower() != os.path.basename(dwg).lower():
        raise RuntimeError("asked to read %r but the active document is %r"
                           % (os.path.basename(dwg), act))
    # ⛔ AND THE "NOT THE REBUILD" GUARD MUST NOT BLOCK READING THE REBUILD ON PURPOSE.
    # It cost the first gate run of the night: the verifier reads BOTH drawings with this
    # reader, and a flat `if "REBUILD" in who: raise` refused the rebuild it was asked for.
    # The real protection is the name match above; this only catches the mix-up it was
    # written for -- asking for the source while the rebuild is in front.
    if "REBUILD" in act and "REBUILD" not in os.path.basename(dwg):
        raise RuntimeError("asked for the source but the rebuild is active: " + act)
    ch = eb_api.channel()

    # ---- 1. the five bulk readers -----------------------------------------
    for op, kw in (("dumpparts", {}), ("dumpfull2", {}), ("dumppoly", {}),
                   ("dumpholes", {}), ("dumpmodel", {})):
        t = time.time()
        r = eb_api.run(op, wait=900, **kw)
        print("%-10s %6.1fs  %s" % (op, time.time() - t, r[:110]))
        if not r.startswith("EB_OK"):
            raise RuntimeError("bulk read failed: " + r)

    d = {"source": dwg, "size": os.path.getsize(dwg), "mtime": os.path.getmtime(dwg),
         "entities": [], "shapes": [], "plates": [], "bolts": [], "holes": [],
         "cuts": {}, "polycuts": {}, "mods": {}, "modtext": {}}

    # ---- 2. every entity, with props and modification counts --------------
    for r in rows(ch, "eb_parts.txt"):
        if r[0] != "PART" or len(r) < 6:
            continue
        h, cls, layer, ext = r[1], r[2], r[3], r[4]
        p = parse_props(r[5])
        counts = dict((k, int(v)) for k, v in re.findall(
            r"(facets|cutPlanes|holeFields|outlets|polyCuts|subBodies)=(\d+)",
            r[6] if len(r) > 6 else "") if v != "0")
        e = {"h": h, "cls": cls, "layer": layer, "ext": ext, "props": p}
        d["entities"].append(e)
        if counts:
            d["mods"][h] = counts

    # ---- 3. shapes and plates, from the geometry dump ---------------------
    poly = {}
    for r in rows(ch, "eb_poly.txt"):
        if r[0].endswith("POLY") and len(r) > 6:
            poly[r[1]] = {"n": int(r[4]), "rect": r[5], "pts": r[6]}
    by_h = dict((e["h"], e) for e in d["entities"])
    for r in rows(ch, "eb_full2.txt"):
        if r[0] == "SHAPE":
            e = by_h.get(r[1], {})
            d["shapes"].append({"h": r[1], "sec": r[2], "cat": r[3], "p1": r[4], "p2": r[5],
                                "len": r[6], "mat": r[7], "name": r[8], "rot": r[9],
                                "off": r[10], "lenadd": r[11], "mir": r[12],
                                "ecs": r[13] if len(r) > 13 else "",
                                "ip": r[14] if len(r) > 14 else "",
                                "layer": r[15] if len(r) > 15 else e.get("layer", ""),
                                "bbox": r[16] if len(r) > 16 else "",
                                "cls": e.get("cls", ""), "props": e.get("props", {})})
        elif r[0] == "PLATE":
            e = by_h.get(r[1], {})
            pl = {"h": r[1], "c": r[2], "dims": r[3], "axes": r[4],
                  "layer": r[5] if len(r) > 5 else e.get("layer", ""),
                  "cls": e.get("cls", ""), "props": e.get("props", {})}
            pl.update(poly.get(r[1], {}))
            d["plates"].append(pl)
    for r in rows(ch, "eb_model.txt"):
        if r[0] == "BOLT":
            e = by_h.get(r[1], {})
            d["bolts"].append({"h": r[1], "dia": r[2], "style": r[3], "len": r[5],
                               "name": r[7] if len(r) > 7 else "",
                               "axis": r[9] if len(r) > 9 else "",
                               "layer": e.get("layer", ""), "props": e.get("props", {})})

    # ---- 4. every hole ----------------------------------------------------
    cur = None
    for f in rows(ch, "eb_holes_all.txt"):
        if f[0] == "OBJ":
            cur = f[1]
        elif f[0] == "HOLE" and len(f) > 7:
            d["holes"].append({"owner": cur, "i": f[4], "a": f[5], "b": f[6], "d": f[7]})

    print("entities %d | shapes %d | plates %d | bolts %d | holes %d | with mods %d"
          % (len(d["entities"]), len(d["shapes"]), len(d["plates"]), len(d["bolts"]),
             len(d["holes"]), len(d["mods"])))

    # ---- 5. the modification DETAIL, batched ------------------------------
    # facets (corner chamfers), outlets and plate break-edges only exist in the per-part
    # `mods` text. 14,044 round trips at 0.25 s each is an hour; one batch is seconds.
    hs = [h for h, m in d["mods"].items()
          if m.get("facets") or m.get("outlets") or m.get("subBodies")]
    if hs:
        t = time.time()
        got = eb_api.batch([("mods", {"handle": h}) for h in hs], chunk=4000,
                           wait=900)
        for (i, op, res) in got:
            if 0 <= i < len(hs) and res.startswith("EB_OK"):
                d["modtext"][hs[i]] = res
        print("mods detail  %6.1fs  %d parts" % (time.time() - t, len(d["modtext"])))

    # ---- 6. cut planes and poly-cuts, through the COM wrapper -------------
    app, doc = eb_api._app_doc()

    def iface(name, tries=10):
        last = None
        for i in range(tries):
            try:
                return app.GetInterfaceObject(name)
            except Exception as e:
                last = e
                time.sleep(0.4 + 0.4 * i)
        raise RuntimeError("GetInterfaceObject(%s) refused: %s" % (name, last))

    cutters = [h for h, m in d["mods"].items() if m.get("cutPlanes")]
    t = time.time()
    for n, h in enumerate(cutters):
        try:
            em = iface("PSCOMWRAPPER.Ks_ComEditModification")
            em.SetObject(doc.HandleToObject(h))
            got = []
            for i in range(em.CutPlaneCount):
                cp = iface("PSCOMWRAPPER.Ks_ComCutPlane")
                em.GetCutPlane(em.GetCutPlaneHandleFromNumber(i), cp)
                got.append({"ip": [round(v, 4) for v in cp.InsertPoint],
                            "n": [round(v, 8) for v in cp.GetNormal()], "f": cp.Flag})
            d["cuts"][h] = got
        except Exception as e:
            d.setdefault("cut_fail", []).append([h, str(e)[:120]])
        if n and n % 500 == 0:
            print("   cuts %d/%d  %.0fs" % (n, len(cutters), time.time() - t))
    print("cut planes   %6.1fs  %d parts, %d failed"
          % (time.time() - t, len(d["cuts"]), len(d.get("cut_fail", []))))

    cutters = [h for h, m in d["mods"].items() if m.get("polyCuts")]
    t = time.time()
    for n, h in enumerate(cutters):
        try:
            em = iface("PSCOMWRAPPER.Ks_ComEditModification")
            em.SetObject(doc.HandleToObject(h))
            got = []
            for i in range(em.PolyCutCount):
                pc = iface("PSCOMWRAPPER.Ks_ComPolyCut")
                em.GetPolyCut(em.GetPolyCutHandleFromNumber(i), pc)
                pg = iface("PSCOMWRAPPER.Ks_ComPolygon")
                pc.GetPolygon(pg)
                # ⚠️ the third component of a vertex is the BULGE, not z
                vs = [[round(c, 5) for c in pg.GetVertex(k)] for k in range(pg.VertexCount)]
                item = {"v": vs}
                for attr in ("Depth", "Origin", "Xaxis", "Yaxis", "Type"):
                    try:
                        val = getattr(pc, attr)
                        item[attr] = list(val) if hasattr(val, "__len__") else val
                    except Exception:
                        pass
                got.append(item)
            d["polycuts"][h] = got
        except Exception as e:
            d.setdefault("polycut_fail", []).append([h, str(e)[:120]])
        if n and n % 500 == 0:
            print("   polycuts %d/%d  %.0fs" % (n, len(cutters), time.time() - t))
    print("poly-cuts    %6.1fs  %d parts, %d failed"
          % (time.time() - t, len(d["polycuts"]), len(d.get("polycut_fail", []))))

    with io.open(out, "w", encoding="utf-8") as f:
        f.write(json.dumps(d, ensure_ascii=False))
    print("cached -> %s  (%.1f MB, total %.1f min)"
          % (out, os.path.getsize(out) / 1e6, (time.time() - t0) / 60))
    return d


if __name__ == "__main__":
    if len(sys.argv) < 3:
        print(__doc__)
        sys.exit(2)
    read(sys.argv[1], sys.argv[2])
