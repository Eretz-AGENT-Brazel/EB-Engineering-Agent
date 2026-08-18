# -*- coding: utf-8 -*-
"""night_bendplates.py -- read the source's bend plates, then fold the rebuild's flat ones.

    python app/night_bendplates.py read  <source.dwg> <out.json>
    python app/night_bendplates.py fold  <in.json> <rebuild.dwg> <cache.json>

A Ks_BendPlate answers `not-PsPlate` to GetPolygon, so `dumppoly` cannot see it and the
plate arrives in the rebuild FLAT. Its real definition lives on PsBendPlateFlange --
Length, Angle, Radius, StartOffset/EndOffset, StartVertex/EndVertex, LengthCalculation and
UseInnerRadius -- which `bendinfo` has always READ and which only v186 can WRITE.
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

FLANGE = re.compile(
    r"\[(\d+)\] len=([-\d.]+) ang=([-\d.]+)deg\([-\d.]+rad\) r=([-\d.]+) "
    r"off=([-\d.]+)/([-\d.]+) vtx=(-?\d+)-(-?\d+) lenCalc=(\w+) innerR=(\w+)")


def read(dwg, out, handles):
    eb_api.build_target(None)
    eb_api.use(dwg, task="night: read the bend plates")
    eb_api.run("list")
    who = eb_api.run("whoami")
    if os.path.basename(dwg).lower() not in who.lower():
        raise RuntimeError("wrong document: " + who[:120])
    d = {}
    for h in handles:
        r = eb_api.run("bendinfo", handle=h, max="8")
        fl = [{"i": int(m[0]), "len": float(m[1]), "ang": float(m[2]), "r": float(m[3]),
               "so": float(m[4]), "eo": float(m[5]), "sv": int(m[6]), "ev": int(m[7]),
               "calc": m[8], "inner": m[9]} for m in FLANGE.findall(r)]
        grips = re.findall(r"grip=([-\d.]+,[-\d.]+,[-\d.]+),([-\d.]+,[-\d.]+,[-\d.]+)", r)
        d[h] = {"raw": r, "flanges": fl, "grips": grips}
        print("%s: %d flange(s) %s" % (h, len(fl),
              " | ".join("len=%g ang=%g r=%g inner=%s calc=%s"
                         % (f["len"], f["ang"], f["r"], f["inner"], f["calc"]) for f in fl)))
    json.dump(d, io.open(out, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    print("cached -> " + out)
    return d


if __name__ == "__main__":
    if len(sys.argv) < 4:
        print(__doc__)
        sys.exit(2)
    if sys.argv[1] == "read":
        hs = sys.argv[4].split(",") if len(sys.argv) > 4 else []
        read(sys.argv[2], sys.argv[3], hs)
