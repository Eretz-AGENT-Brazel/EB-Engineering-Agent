# -*- coding: utf-8 -*-
"""night_build.py -- build a 1:1 rebuild from a cache written by night_read.py.

    python app/night_build.py "<source.json>" "<rebuild.dwg>" [segment]

    segments:  shapes | plates | cuts | holes | bolts | all      (default: all)

Every segment SAVES and reports before the next one starts, so a crash costs one segment, and
Amir can see progress land in the drawing during the night.

Two self-corrections are built in, because both traps cost a session on 18/08/2026:

  * **a member whose bbox span does not match the source is rebuilt with rot+90.** `rot` as
    printed by the dump round-trips for 90 and -90 but not for 0, and the wrong orientation
    then impersonates three different "API limitations" further down the line.
  * **a hole that does not land where the source has it is re-drilled with the other flange.**
    The drill resolves position from the host's geometry, so the flange selector -- not the
    point -- decides which wall it enters.
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


def num(s):
    return [float(v) for v in str(s).split(",")]


def span(bbox):
    a, b = [num(p) for p in bbox.split(";")]
    return tuple(round(b[i] - a[i], 1) for i in range(3))


def axes(p, key):
    """props prints an axis as 1/0/0 -- turn it into a vector string the ops accept."""
    v = p.get(key, "").replace("/", ",")
    return v if v.count(",") == 2 else None


class Build(object):
    def __init__(self, cache, rebuild):
        self.d = json.load(io.open(cache, encoding="utf-8"))
        self.cache = cache
        self.rbd = rebuild
        self.map_path = os.path.splitext(cache)[0] + "-map.json"
        self.map = (json.load(io.open(self.map_path, encoding="utf-8"))
                    if os.path.exists(self.map_path) else {})
        self.notes = []

    # ---------------------------------------------------------------- plumbing
    def enter(self):
        eb_api.use(self.rbd, task="night: build the rebuild")
        eb_api.build_target(self.rbd)
        eb_api.run("list")                        # gated -> activates the document
        who = eb_api.run("whoami")
        if os.path.basename(self.rbd).lower() not in who.lower():
            raise RuntimeError("the rebuild is not the active document: " + who[:120])

    def save_map(self):
        json.dump(self.map, io.open(self.map_path, "w", encoding="utf-8"))

    def fire(self, op, src_h, **kw):
        r = eb_api.run(op, **kw)
        m = re.search(r"handle=(\w+)", r)
        if r.startswith("EB_OK") and m and m.group(1) != "-":
            if src_h:
                self.map[src_h] = m.group(1)
            return m.group(1)
        self.notes.append("%s %s: %s" % (op, src_h, r[:110]))
        return None

    def checkpoint(self, label):
        eb_api.run("save")
        self.save_map()
        print("   [saved] %s" % label)

    # ---------------------------------------------------------------- segments
    def wipe(self):
        eb_api.run("list")
        hs = [l.rstrip("\n").split("|")[0]
              for l in io.open(os.path.join(eb_api.channel(), "eb_list.txt"),
                               encoding="utf-8-sig")
              if len(l.split("|")) > 1 and l.split("|")[1].startswith("Ks_")]
        for h in hs:
            eb_api.delete(h)
        print("wiped %d copied parts" % len(hs))

    def shapes(self):
        t0 = time.time()
        for s in self.d["shapes"]:
            self.fire("beam", s["h"], kind="standard", name=s["sec"], catalog=s["cat"],
                      p1=s["p1"], p2=s["p2"], rot=s["rot"], mirror=s["mir"])
        # self-correction: any member whose span disagrees with the source is turned 90 deg
        eb_api.run("dumpfull2")
        got = {r[1]: r for r in [l.rstrip().split("\t") for l
               in io.open(os.path.join(eb_api.channel(), "eb_full2.txt"),
                          encoding="utf-8-sig") if l.strip()] if r[0] == "SHAPE"}
        fixed = 0
        for s in self.d["shapes"]:
            t = self.map.get(s["h"])
            if not t or t not in got or not s["bbox"]:
                continue
            if span(s["bbox"]) == span(got[t][16]):
                continue
            eb_api.delete(t)
            rot = (float(s["rot"] or 0) + 90) % 360
            if self.fire("beam", s["h"], kind="standard", name=s["sec"], catalog=s["cat"],
                         p1=s["p1"], p2=s["p2"], rot=rot, mirror=s["mir"]):
                fixed += 1
        print("shapes: %d built, %d re-oriented (rot+90), %.0fs"
              % (len([1 for s in self.d["shapes"] if s["h"] in self.map]), fixed,
                 time.time() - t0))
        if fixed:
            self.notes.append("%d members needed rot+90 -- the dump's rot=0 does not "
                              "round-trip" % fixed)
        self.checkpoint("shapes")

    def plates(self):
        t0 = time.time()
        for p in self.d["plates"]:
            pr = p.get("props", {})
            L, W, H = (pr.get("L"), pr.get("W"), pr.get("H"))
            ex, ey = axes(pr, "X"), axes(pr, "Y")
            org = pr.get("org")
            if not (L and W and H and ex and ey and org):
                self.notes.append("plate %s: props incomplete, skipped" % p["h"])
                continue
            # which way the material grows from the insertion plane: read `mid`, not a guess
            ins = float(H) / 2.0
            mid = pr.get("mid", "")
            if "->" in mid:
                a, b = [num(x) for x in mid.split("->")]
                z = num(axes(pr, "Z") or "0,0,1")
                if sum((b[i] - a[i]) * z[i] for i in range(3)) < 0:
                    ins = -ins
            self.fire("plate9", p["h"], mode="rect", at=org, l=L, w=W, t=H,
                      ex=ex, ey=ey, insheight=ins)
        print("plates: %d/%d, %.0fs"
              % (len([1 for p in self.d["plates"] if p["h"] in self.map]),
                 len(self.d["plates"]), time.time() - t0))
        self.checkpoint("plates")

    def cuts(self):
        n = ok = 0
        for src, cs in self.d["cuts"].items():
            t = self.map.get(src)
            for c in cs:
                n += 1
                if not t or not isinstance(c.get("Normal"), list):
                    continue
                if eb_api.run("planecut", handle=t,
                              at="%.4f,%.4f,%.4f" % tuple(c["InsertPoint"]),
                              normal="%.8f,%.8f,%.8f" % tuple(c["Normal"])).startswith("EB_OK"):
                    ok += 1
        print("cuts: %d/%d" % (ok, n))
        self.checkpoint("cuts")

    def holes(self):
        t0 = time.time()
        want = {}
        for h in self.d["holes"]:
            t = self.map.get(h["owner"])
            if not t:
                continue
            a, b = num(h["a"]), num(h["b"])
            v = [b[i] - a[i] for i in range(3)]
            L = sum(x * x for x in v) ** 0.5
            want.setdefault(t, []).append((a, [x / L for x in v], float(h["d"]),
                                           [(a[i] + b[i]) / 2 for i in range(3)]))
        for t, lst in want.items():
            for a, n, dia, mid in lst:
                eb_api.run("drill", handle=t, at="%.4f,%.4f,%.4f" % tuple(a),
                           n="%.6f,%.6f,%.6f" % tuple(n), dia=dia, play=0)
        miss = self.hole_report()
        # self-correction: whatever missed its mark gets the other flange
        if miss:
            for t, items in miss.items():
                nfields = int(re.search(r"holeFields=(\d+)",
                                        eb_api.run("mods", handle=t)).group(1))
                for i in range(nfields - 1, -1, -1):
                    eb_api.run("killholefield", handle=t, field=i)
                for a, n, dia, mid in want[t]:
                    eb_api.run("drill", handle=t, at="%.4f,%.4f,%.4f" % tuple(a),
                               n="%.6f,%.6f,%.6f" % tuple(n), dia=dia, play=0, flange=1)
            miss = self.hole_report()
            if miss:
                self.notes.append("holes still off on %d part(s) after the flange retry"
                                  % len(miss))
        print("holes: %d wanted, off-position parts left: %d, %.0fs"
              % (len(self.d["holes"]), len(miss or {}), time.time() - t0))
        self.checkpoint("holes")

    def hole_report(self):
        """Which parts carry holes that are not where the source has them."""
        eb_api.run("dumpholes")
        inv = dict((v, k) for k, v in self.map.items())
        mine = {}
        cur = None
        for l in io.open(os.path.join(eb_api.channel(), "eb_holes_all.txt"),
                         encoding="utf-8-sig"):
            f = l.rstrip("\n").split("\t")
            if f[0] == "OBJ":
                cur = f[1]
            elif f[0] == "HOLE":
                a, b = num(f[5]), num(f[6])
                mine.setdefault(cur, []).append([(a[i] + b[i]) / 2 for i in range(3)])
        bad = {}
        for h in self.d["holes"]:
            t = self.map.get(h["owner"])
            if not t:
                continue
            a, b = num(h["a"]), num(h["b"])
            mid = [(a[i] + b[i]) / 2 for i in range(3)]
            cand = mine.get(t, [])
            best = min([sum((mid[i] - m[i]) ** 2 for i in range(3)) ** 0.5
                        for m in cand] or [9e9])
            if best > 0.01:
                bad.setdefault(t, []).append(best)
        return bad

    def bolts(self):
        # 1) let ProSteel derive every bolt it can from the holes -- it places them better
        #    than a hand-made bolt does (measured: OK=44 vs OVERSIZED=56 on model 2)
        lines = {}
        for h in self.d["holes"]:
            t = self.map.get(h["owner"])
            if not t:
                continue
            a, b = num(h["a"]), num(h["b"])
            v = [b[i] - a[i] for i in range(3)]
            L = sum(x * x for x in v) ** 0.5
            n = tuple(abs(round(x / L, 3)) for x in v)
            key = (n, tuple(round(a[i], 1) if n[i] < 0.5 else 0 for i in range(3)))
            lines.setdefault(key, set()).add(t)
        groups = sorted({tuple(sorted(v)) for v in lines.values() if len(v) > 1})
        for g in groups:
            eb_api.run("boltparts", handles=",".join(g), style="8.8S")
        # 2) a bolt with no second STEEL part (a fixing into concrete) is created directly
        eb_api.run("dumpmodel")
        have = []
        for l in io.open(os.path.join(eb_api.channel(), "eb_model.txt"), encoding="utf-8-sig"):
            f = l.rstrip("\n").split("\t")
            if f[0] == "BOLT" and len(f) > 9 and ";" in f[9]:
                p = f[9].split(";")
                a, b = num(p[0]), num(p[1])
                have.append([(a[i] + b[i]) / 2 for i in range(3)])
        added = 0
        for b in self.d["bolts"]:
            if ";" not in (b.get("axis") or ""):
                continue
            p = b["axis"].split(";")
            a, c = num(p[0]), num(p[1])
            mid = [(a[i] + c[i]) / 2 for i in range(3)]
            if any(sum((mid[i] - h[i]) ** 2 for i in range(3)) ** 0.5 < 15 for h in have):
                continue
            if eb_api.run("bolt", p1=p[0], p2=p[1], dia=b.get("dia") or 24,
                          style="8.8S", len=b.get("len") or 85).startswith("EB_OK"):
                added += 1
                have.append(mid)
        print("bolts: %d joints bolted by ProSteel, %d created directly" % (len(groups), added))
        self.checkpoint("bolts")


def main(argv):
    b = Build(argv[0], argv[1])
    seg = (argv[2] if len(argv) > 2 else "all").lower()
    b.enter()
    if seg in ("all", "wipe"):
        b.wipe()
    for name in ("shapes", "plates", "cuts", "holes", "bolts"):
        if seg in ("all", name):
            getattr(b, name)()
    b.save_map()
    print("census:", eb_api.run("dumpmodel")[:110])
    for n in b.notes[:15]:
        print("   note:", n)
    return 0


if __name__ == "__main__":
    if len(sys.argv) < 3:
        print(__doc__)
        sys.exit(2)
    sys.exit(main(sys.argv[1:]))
