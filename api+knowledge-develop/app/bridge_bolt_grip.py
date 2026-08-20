# -*- coding: utf-8 -*-
"""bridge_bolt_grip.py -- find what `bolt p1/p2` actually wants, then place the refusals.

    python app/bridge_bolt_grip.py probe <source.json> <rebuild.dwg> <source.dwg>
    python app/bridge_bolt_grip.py fix   <source.json> <rebuild.dwg> <source.dwg>

⛔ THE MEASUREMENT THAT STARTED THIS. Placing 5,680 bolts on the source's own axes refused
roughly one in seven, always the same way: `EB_ERR bolt create failed (style '8.8S')`. The
skill already says why -- **a style that refuses is usually a GRIP that refuses** -- and here
is the arithmetic: the dumped axis span EQUALS the bolt's length (M24 x 120 -> 120.0 mm span),
but `PsCreateBolt.CreateSingleBolt(p1, p2, ...)` reads p1->p2 as the **grip**. Asking for a
grip of 120 on an M24 needs a bolt of 120 + 1.6*24 = 158.4 mm, and no table row is that long,
so it fails silently.

⇒ The grip is `KlemmLen`, which lives on the bolt in the source and was never captured (the
cache's property filter did not list it). This reads it, then probes WHERE on the axis the
grip segment sits -- centred, head-anchored or nut-anchored -- by building one bolt each way
and reading back which one lands on the source's own axis.
"""
import io
import json
import math
import os
import re
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)
import eb_api  # noqa: E402


def num(s):
    return [float(v) for v in str(s).split(",")]


def read_klemm(src, bolts):
    """KlemmLen for every source bolt, in one batch (5,680 props ~ seconds)."""
    eb_api.build_target(None)
    eb_api.use(os.path.basename(src), task="READ ONLY - bolt grip lengths")
    eb_api.run("list", wait=900)
    act = eb_api._active_doc_name() or ""
    if "REBUILD" in act:
        raise RuntimeError("that is the rebuild: " + act)
    hs = [b["h"] for b in bolts]
    out = {}
    for a in range(0, len(hs), 4000):
        part = hs[a:a + 4000]
        rows = eb_api.batch([("props", {"handle": h}) for h in part], wait=1800)
        for (i, op, res) in rows:
            if i < 0 or i >= len(part):
                continue
            m = re.search(r"klemm=([-\d.]+)", res)
            if m:
                out[part[i]] = float(m.group(1))
    return out


def variants(b, klemm):
    """The three places a grip of length `klemm` can sit on a bolt's axis."""
    p1, p2 = [num(q) for q in b["axis"].split(";")]
    L = math.dist(p1, p2)
    if L <= 0 or klemm <= 0:
        return []
    u = [(p2[i] - p1[i]) / L for i in range(3)]
    mid = [(p1[i] + p2[i]) / 2.0 for i in range(3)]
    half = klemm / 2.0
    return [
        ("centred", [mid[i] - half * u[i] for i in range(3)],
                    [mid[i] + half * u[i] for i in range(3)]),
        ("from p1", p1, [p1[i] + klemm * u[i] for i in range(3)]),
        ("from p2", [p2[i] - klemm * u[i] for i in range(3)], p2),
    ]


def fmt(p):
    return "%.4f,%.4f,%.4f" % tuple(p)


def enter_rebuild(rbd):
    eb_api.use(os.path.basename(rbd), task="bolt grip fix", project="bridge-bernie")
    eb_api.build_target(rbd)
    eb_api.run("list", wait=900)
    act = eb_api._active_doc_name() or ""
    if "REBUILD" not in act:
        raise RuntimeError("not the rebuild: " + act)


def failures(cache, bolts):
    """The source bolts that are not in the map -- i.e. the ones that refused."""
    mp = json.load(io.open(os.path.splitext(cache)[0] + "-map.json", encoding="utf-8"))
    return [b for b in bolts if b["h"] not in mp], mp


def main(mode, cache, rbd, src):
    d = json.load(io.open(cache, encoding="utf-8"))
    bolts = [b for b in d["bolts"] if ";" in (b.get("axis") or "")]
    left, mp = failures(cache, bolts)
    print("%d bolts refused of %d" % (len(left), len(bolts)))
    if not left:
        return
    kpath = os.path.join(os.path.dirname(cache), "bolt-klemm.json")
    if os.path.exists(kpath):
        klemm = json.load(io.open(kpath, encoding="utf-8"))
    else:
        klemm = read_klemm(src, left)
        json.dump(klemm, io.open(kpath, "w", encoding="utf-8"))
    print("grip read for %d of them" % len(klemm))
    got = [(b, klemm.get(b["h"], 0)) for b in left]
    print("grip vs length, first 5:")
    for b, k in got[:5]:
        p1, p2 = [num(q) for q in b["axis"].split(";")]
        print("   M%s x %s   axis %.1f   klemm %s" % (b["dia"], b["len"],
                                                     math.dist(p1, p2), k))
    enter_rebuild(rbd)

    if mode == "probe":
        # one bolt, all three anchorings, and see which creates
        for b, k in got[:3]:
            print("--- %s  M%s x %s  klemm %s" % (b["h"], b["dia"], b["len"], k))
            for name, a, c in variants(b, k):
                r = eb_api.run("bolt", wait=900, p1=fmt(a), p2=fmt(c), dia=b["dia"],
                               style=b["style"], len=b["len"])
                print("   %-8s %s" % (name, r[:150]))
                m = re.search(r"handle=(\w+)", r)
                if r.startswith("EB_OK") and m:
                    eb_api.run("erase", wait=600, handles=m.group(1))
        return

    # fix: place every refusal with the grip, trying the anchorings in order
    # ⭐ MEASURED, not guessed: all three anchorings CREATE, so the choice is made by
    # geometry. On M12 x 50 with klemm 24 the rebuilt bolt's `org` matched the source's
    # exactly under **from p1** (-2479.926,81465.192,13735 on both) and was 13 mm out
    # centred, 26 mm out from p2. ⇒ the dumped axis STARTS at the bolt's origin and runs its
    # LENGTH; the grip is the first `klemm` millimetres of it.
    order = ["from p1", "centred", "from p2"]
    placed = 0
    t0 = time.time()
    for a in range(0, len(got), 1000):
        chunk = got[a:a + 1000]
        pending = list(chunk)
        for name in order:
            if not pending:
                break
            items, srcs = [], []
            for b, k in pending:
                vs = dict((n, (x, y)) for n, x, y in variants(b, k))
                if name not in vs:
                    continue
                p, q = vs[name]
                kw = dict(p1=fmt(p), p2=fmt(q), dia=b["dia"], style=b["style"])
                if b.get("len"):
                    kw["len"] = b["len"]
                if b.get("layer"):
                    kw["layer"] = b["layer"]
                items.append(("bolt", kw))
                srcs.append(b)
            rows = eb_api.batch(items, wait=3600)
            still = []
            for (i, op, res) in rows:
                if i < 0 or i >= len(srcs):
                    continue
                m = re.search(r"handle=(\w+)", res)
                if res.startswith("EB_OK") and m and m.group(1) != "-":
                    mp[srcs[i]["h"]] = m.group(1)
                    placed += 1
                else:
                    still.append((srcs[i], klemm.get(srcs[i]["h"], 0)))
            print("   %-8s placed %d, %d still refusing, %.0fs"
                  % (name, placed, len(still), time.time() - t0))
            pending = still
        eb_api.run("save", wait=1800)
        json.dump(mp, io.open(os.path.splitext(cache)[0] + "-map.json", "w",
                              encoding="utf-8"))
    print("placed %d of %d refusals" % (placed, len(got)))


def redo_all(cache, rbd, src):
    """Wipe every bolt and place all of them again with the MEASURED grip.

    ⛔ WHY A REDO AND NOT A PATCH. The first pass handed `bolt` the source's own axis, whose
    span is the bolt's LENGTH -- so the 3,467 that did not refuse were created with a grip of
    (for instance) 120 mm on an M24 x 120. They sit in the right place and carry the right
    length, and their GRIP is wrong: `KlemmLen` is what a bolt's packet is, it is what
    `vfy_fit` compares hole depths against, and it is what the shop reads. A part that is
    right in every visible number and wrong in the one that defines the joint is exactly the
    failure this project keeps paying for.
    """
    d = json.load(io.open(cache, encoding="utf-8"))
    bolts = [b for b in d["bolts"] if ";" in (b.get("axis") or "")]
    kpath = os.path.join(os.path.dirname(cache), "bolt-klemm-all.json")
    if os.path.exists(kpath):
        klemm = json.load(io.open(kpath, encoding="utf-8"))
    else:
        klemm = read_klemm(src, bolts)
        json.dump(klemm, io.open(kpath, "w", encoding="utf-8"))
    print("grip read for %d of %d bolts" % (len(klemm), len(bolts)))
    zero = len([1 for b in bolts if not klemm.get(b["h"])])
    print("bolts whose KlemmLen is 0 or unreadable: %d" % zero)
    enter_rebuild(rbd)
    print(eb_api.run("wipe", wait=1800, cls="Ks_Bolt"))
    mp = json.load(io.open(os.path.splitext(cache)[0] + "-map.json", encoding="utf-8"))
    for b in bolts:
        mp.pop(b["h"], None)
    json.dump(mp, io.open(os.path.splitext(cache)[0] + "-map.json", "w", encoding="utf-8"))
    placed = 0
    t0 = time.time()
    for a in range(0, len(bolts), 1000):
        chunk = bolts[a:a + 1000]
        items, srcs = [], []
        for b in chunk:
            k = klemm.get(b["h"], 0)
            vs = dict((n, (x, y)) for n, x, y in variants(b, k))
            p, q = vs.get("from p1", (None, None))
            if p is None:
                # no grip to work with: fall back to the axis, and say so
                p, q = [num(x) for x in b["axis"].split(";")]
            kw = dict(p1=fmt(p), p2=fmt(q), dia=b["dia"], style=b["style"])
            if b.get("len"):
                kw["len"] = b["len"]
            if b.get("layer"):
                kw["layer"] = b["layer"]
            items.append(("bolt", kw))
            srcs.append(b)
        rows = eb_api.batch(items, wait=3600)
        for (i, op, res) in rows:
            if i < 0 or i >= len(srcs):
                continue
            m = re.search(r"handle=(\w+)", res)
            if res.startswith("EB_OK") and m and m.group(1) != "-":
                mp[srcs[i]["h"]] = m.group(1)
                placed += 1
        print("   %d/%d placed, %.0fs" % (placed, len(bolts), time.time() - t0))
        eb_api.run("save", wait=1800)
        json.dump(mp, io.open(os.path.splitext(cache)[0] + "-map.json", "w",
                              encoding="utf-8"))
    print("redo: %d of %d bolts placed with the measured grip" % (placed, len(bolts)))


if __name__ == "__main__":
    if len(sys.argv) < 5:
        print(__doc__)
        sys.exit(2)
    if sys.argv[1] == "redo":
        redo_all(sys.argv[2], sys.argv[3], sys.argv[4])
    else:
        main(sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4])
