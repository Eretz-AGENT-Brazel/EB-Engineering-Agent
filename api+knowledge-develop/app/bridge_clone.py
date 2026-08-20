# -*- coding: utf-8 -*-
"""bridge_clone.py -- bring across what this machine cannot BUILD, exactly as it is.

    python app/bridge_clone.py probe  <source.json> <rebuild.dwg> <source.dwg>
    python app/bridge_clone.py run    <source.json> <rebuild.dwg> <source.dwg> [group ...]

    groups: nocat concrete solids annot infra bend assembly     (default: all)

⭐ WHY A CLONE IS THE HONEST ANSWER HERE, and only here. Two different walls:

  1. **A catalogue this machine does not have.** 678 of the bridge model's 6,240 profiles
     name `PANEL`, `AMUDIM` or `HOESCHISODACH`; the shapes database here holds 357 catalogues
     and none of them is one of those. No `beam` call can conjure a section that is not
     installed -- and the fallback is worse than a refusal: `catalog=PANEL name='SVAHA
     1000X30'` silently built 431 members out of `DIN.DIN_FLACH`, because the resolver found
     the same NAME elsewhere. Measured 20/08/2026.
  2. **A class with no creator.** `Ks_ConcretePanel` / `Ks_ConcreteSlab` /
     `Ks_ConcreteShape` (ProConcrete), `AcDb3dSolid`, dimensions, `Ks_Grid` -- the last of
     which the API cannot even touch without killing the session (LETHAL-CALLS, `addUserXaxis`).

Everything the API CAN build parametrically is built parametrically, by bridge_build.py.
This file is for the rest, and every part it moves is declared as moved.

⚠️ Cloning a ProSteel custom entity across databases was never tried before tonight, so
`probe` does one object, alone, on a saved drawing, and reads it back -- the LETHAL-CALLS
protocol, because a 2,700-object loop is the worst possible place to discover a crash.
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

CHUNK = 200

# the classes that have no creator on this machine, grouped so each can be reported apart
GROUPS = {
    "concrete": ("Ks_ConcretePanel", "Ks_ConcreteShape", "Ks_ConcreteSlab"),
    "solids": ("AcDb3dSolid",),
    "annot": ("AcDbRotatedDimension", "AcDbAlignedDimension", "AcDbDiametricDimension",
              "AcDbLine", "AcDbPoint", "AcDbPolyline", "AcDbCircle", "AcDb3dPolyline",
              "AcDbBlockReference"),
    "infra": ("Ks_Grid", "Ks_WorkFrame"),
    "bend": ("Ks_BendShape",),
    "assembly": ("Ks_Assembly",),
}
MISSING_CATALOGUES = ("PANEL", "AMUDIM", "HOESCHISODACH")


def load(cache):
    return json.load(io.open(cache, encoding="utf-8"))


def enter(rbd):
    eb_api.use(os.path.basename(rbd), task="1:1 rebuild: clone what cannot be built",
               project="bridge-bernie")
    eb_api.build_target(rbd)
    eb_api.run("list", wait=300)
    act = eb_api._active_doc_name() or ""
    if act.lower() != os.path.basename(rbd).lower() or "REBUILD" not in act:
        raise RuntimeError("active document is %r, not the rebuild" % act)
    return act


def targets(d, groups):
    """{group: [handles]} -- what to clone, from the cached census."""
    out = {}
    if "nocat" in groups:
        hs = [s["h"] for s in d["shapes"] if s["cat"] in MISSING_CATALOGUES]
        out["nocat"] = hs
    for g in groups:
        if g == "nocat":
            continue
        want = GROUPS[g]
        out[g] = [e["h"] for e in d["entities"] if e["cls"] in want]
    return out


def clone(handles, src, label, mapping):
    ok = fail = 0
    t0 = time.time()
    for a in range(0, len(handles), CHUNK):
        part = handles[a:a + CHUNK]
        r = eb_api.run("xclone", wait=900, **{"from": os.path.basename(src),
                                              "handles": ",".join(part)})
        if not r.startswith("EB_OK"):
            print("   %s: %s" % (label, r[:180]))
            fail += len(part)
            continue
        # the op writes one line per asked handle: <source handle>\t<new handle|->
        try:
            for line in io.open(os.path.join(eb_api.channel(), "eb_xclone.txt"),
                                encoding="utf-8-sig"):
                f = line.rstrip("\n").split("\t")
                if len(f) == 2 and f[1] not in ("-", "MISSING") and not f[1].startswith("ERR"):
                    mapping[f[0]] = f[1]
                    ok += 1
                else:
                    fail += 1
        except Exception as e:
            print("   %s: could not read the map: %s" % (label, e))
        eb_api.run("save", wait=900)
        print("   %-9s %d/%d cloned, %.0fs" % (label, ok, len(handles), time.time() - t0))
    return ok, fail


if __name__ == "__main__":
    if len(sys.argv) < 5:
        print(__doc__)
        sys.exit(2)
    mode, cache, rbd, src = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4]
    d = load(cache)
    map_path = os.path.splitext(cache)[0] + "-map.json"
    mapping = json.load(io.open(map_path, encoding="utf-8")) if os.path.exists(map_path) else {}
    print(enter(rbd))

    if mode == "probe":
        # ONE object, alone, on a saved drawing, read back afterwards.
        h = [e["h"] for e in d["entities"] if e["cls"] == "AcDb3dSolid"][0]
        eb_api.run("save", wait=900)
        before = eb_api.run("list", wait=300)
        r = eb_api.run("xclone", wait=300, **{"from": os.path.basename(src), "handles": h})
        after = eb_api.run("list", wait=300)
        print("probe solid %s\n  %s\n  before: %s\n  after : %s"
              % (h, r[:200], before[:60], after[:60]))
        m = re.search(r"cloned=(\d+)", r)
        if m and int(m.group(1)) == 1:
            newh = io.open(os.path.join(eb_api.channel(), "eb_xclone.txt"),
                           encoding="utf-8-sig").read().strip().split("\t")[-1]
            print("  read back: " + eb_api.run("props", wait=300, handle=newh)[:220])
        sys.exit(0)

    if mode == "redo-nocat":
        # ⛔ THE 431 THAT WERE BUILT OUT OF THE WRONG CATALOGUE HAVE TO GO FIRST.
        # `beam catalog=PANEL name='SVAHA 1000X30'` did not refuse -- it silently resolved the
        # same NAME in DIN.DIN_FLACH and built 431 grating members out of flat bar. They are
        # the right size and the wrong part: a parts list would order flat bar, and the
        # catalogue is what the shop reads. Erase them, forget them, clone the originals.
        hs = [s2["h"] for s2 in d["shapes"] if s2["cat"] in MISSING_CATALOGUES]
        wrong = [mapping[h] for h in hs if h in mapping]
        print("erasing %d members built from a substituted catalogue" % len(wrong))
        for a in range(0, len(wrong), 400):
            print("   " + eb_api.run("erase", wait=900,
                                     handles=",".join(wrong[a:a + 400]))[:100])
        for h in hs:
            mapping.pop(h, None)
        json.dump(mapping, io.open(map_path, "w", encoding="utf-8"))
        eb_api.run("save", wait=900)
        print("map now holds %d parts" % len(mapping))
        sys.exit(0)

    groups = sys.argv[5:] or (["nocat"] + list(GROUPS))
    tg = targets(d, groups)
    total = {}
    for g in groups:
        hs = [h for h in tg.get(g, []) if h not in mapping]
        print("%s: %d to clone (%d already mapped)"
              % (g, len(hs), len(tg.get(g, [])) - len(hs)))
        if not hs:
            continue
        ok, fail = clone(hs, src, g, mapping)
        total[g] = (ok, fail)
        json.dump(mapping, io.open(map_path, "w", encoding="utf-8"))
    print("\n=== cloned ===")
    for g, (ok, fail) in total.items():
        print("  %-9s ok %5d   failed %5d" % (g, ok, fail))
    json.dump(mapping, io.open(map_path, "w", encoding="utf-8"))
