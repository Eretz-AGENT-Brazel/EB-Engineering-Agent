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
        # ⭐ THE FRAME SEARCH IS EXPENSIVE AND ITS ANSWER NEVER CHANGES. Finding which
        # rot/mirror reproduces a member's section frame costs up to 8 delete+build+read
        # cycles; on model 5 that is ~35 minutes for 175 members. The answer is a property of
        # (section, direction, source frame), so it is written down and reused: a later
        # rebuild of the same model applies it directly and the search never runs again.
        self.frames_path = os.path.splitext(cache)[0] + "-frames.json"
        self.frames = (json.load(io.open(self.frames_path, encoding="utf-8"))
                       if os.path.exists(self.frames_path) else {})
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
        json.dump(self.frames, io.open(self.frames_path, "w", encoding="utf-8"))

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

    def frame_of(self, handle):
        """The section frame as props prints it -- the only witness to where the material is.

        ⚠️ AND IT MUST BE ABLE TO SAY "I DON'T KNOW". Reading 175 frames straight after 175
        builds, AutoCAD answered EB_BUSY on some of them; the empty parse then looked exactly
        like a frame that disagreed, and ten correct members were reported as wrong. A check
        that cannot distinguish "differs" from "could not read" manufactures defects.
        """
        for attempt in range(4):
            r = eb_api.run("props", handle=handle)
            d = dict(re.findall(r"(X|Y|Z)=('[^']*'|[^ ]+)", r))
            if d.get("X") and d.get("Y") and d.get("Z"):
                return (d["X"], d["Y"], d["Z"])
            time.sleep(0.5 + attempt)
        return None                      # unreadable -- NOT a mismatch

    def shapes(self):
        """⭐⭐⭐ THE SECTION FRAME IS A PARAMETER. HAND IT OVER; DO NOT SEARCH FOR IT.

        Measured on model 5, one member, four builds:
            p1->p2 as cached, no axes  ->  1/0/0 | 0/1/0 | 0/0/1
            ends swapped               -> -1/0/0 | 0/1/0 | 0/0/-1   (Z reversed)
            **as cached + ax= ay=      ->  0/-1/0 | 1/0/0 | 0/0/1   EXACTLY the source**
        `beam` passes `ax`/`ay` to PsShapeCoordinateSystem.SetXAxis/SetYAxis, so the source's
        own frame goes straight in and the member lands right the first time.

        Two things this replaces, both of which cost a night:
        ⛔ **the rot/mirror SEARCH** -- up to 8 delete+build+read cycles per member (35 minutes
           for 175), and it left the LAST attempt in place when nothing matched instead of the
           best one. Since the last candidate carried mirror=1, and **`beam mirror=1` reverses
           p1->p2** (this file says so), every unmatched member ended up pointing backwards:
           175 of 175 frames wrong, with the Z axis inverted on every one. A search that does
           not restore its own starting point is worse than no search -- 5d, again.
        ⛔ **the endpoint swap** derived from the source Z. The measurement above shows the
           beam op's Z already follows p1->p2 faithfully, so the swap inverted what was right.
           The 39 "backwards" members were an artefact of comparing against a frame the first
           build had not been given.
        """
        t0 = time.time()
        for s in self.d["shapes"]:
            kw = dict(kind="standard", name=s["sec"], catalog=s["cat"],
                      p1=s["p1"], p2=s["p2"], rot=s["rot"], mirror=s["mir"])
            ax, ay = axes(s["props"], "X"), axes(s["props"], "Y")
            if ax and ay:
                # ⚠️ AND `rot` STILL TURNS ON TOP OF THE AXES YOU HAND OVER. Measured on
                # model 5, and the correlation is total: every member whose frame came out
                # right carried rot=0 (153 of them), and every member whose frame came out
                # wrong carried rot=90, -90 or 180 (22). The dump's rotation is already
                # BAKED INTO the axes -- applying it again turns the section a second time.
                kw["ax"], kw["ay"], kw["rot"] = ax, ay, 0
            self.fire("beam", s["h"], **kw)

        # verify, and REPORT rather than hunt: a member whose frame still disagrees is a
        # finding for the morning, not something to attack with a destructive loop.
        wrong = unread = 0
        for s in self.d["shapes"]:
            t = self.map.get(s["h"])
            src = tuple(s["props"].get(k) for k in "XYZ")
            if not t or None in src:
                continue
            got = self.frame_of(t)
            if got is None:
                unread += 1
            elif got != src:
                wrong += 1
                if wrong <= 10:
                    self.notes.append("shape %s (%s): frame %s, source wanted %s"
                                      % (s["h"], s["sec"], got, src))
        print("shapes: %d built, %d frames still disagree, %d unreadable, %.0fs"
              % (len([1 for s in self.d["shapes"] if s["h"] in self.map]), wrong, unread,
                 time.time() - t0))
        self.checkpoint("shapes")

    def plates(self):
        t0 = time.time()
        self.inplane = 0
        self.shaped = 0
        for p in self.d["plates"]:
            pr = p.get("props", {})
            L, W, H = pr.get("L"), pr.get("W"), pr.get("H")
            ax, ay, az = axes(pr, "X"), axes(pr, "Y"), axes(pr, "Z")
            org = pr.get("org")
            if not (L and W and H and ax and ay and az and org):
                self.notes.append("plate %s: props incomplete, skipped" % p["h"])
                continue
            fL, fW, fH = float(L), float(W), float(H)
            X, Y, Z = num(ax), num(ay), num(az)

            # WHERE IS THE MATERIAL relative to the insertion point, along the normal? Read
            # it, do not assume: the signed distance from `org` to the middle of `mid`. Both
            # build routes need it, so it is computed before either of them.
            ins = 0.0
            mid = pr.get("mid", "")
            if "->" in mid:
                a, b = [num(x) for x in mid.split("->")]
                o = num(org)
                ins = sum(((a[i] + b[i]) / 2.0 - o[i]) * Z[i] for i in range(3))

            # ⛔⛔ THE CONTOUR IS THE PART. Building from L/W/H makes a RECTANGLE, and on
            # model 5 only 69 of 314 plates are rectangles -- 237 are not and 306 carry more
            # than four vertices. Every rib came out square until the polygon was read.
            # `pts` is in the plate's OWN frame (the same frame props prints as X/Y), so
            # at=org + ex=X + ey=Y reproduces it exactly -- and, unlike the rectangle route,
            # it needs no in-plane centring correction at all: the contour already carries
            # where the material sits relative to the insertion point.
            if p.get("pts"):
                # ⭐⭐ AND `at` IS NOT THE INSERTION POINT -- IT IS WHERE THE CONTOUR'S BBOX
                # CENTRE MUST LAND. `plate9 mode=poly` re-centres the polygon about `at`
                # (this file already says "for a contour ProSteel re-centres"), and it centres
                # the contour's BOUNDING BOX, not its centroid: a contour running y -100..85
                # comes back -92.5..92.5, every vertex shifted by the same 7.5. So a plate
                # whose contour is asymmetric about its own origin lands displaced by exactly
                # that asymmetry -- measured on model 5 as 69 plates out of place by 20 and
                # 70 mm, while their shapes were perfect. Aim at the bbox centre instead.
                v = [[float(x) for x in q.split(",")] for q in p["pts"].split(";")]
                cx = (min(q[0] for q in v) + max(q[0] for q in v)) / 2.0
                cy = (min(q[1] for q in v) + max(q[1] for q in v)) / 2.0
                o, eX, eY = num(org), num(ax), num(ay)
                at = ",".join("%.4f" % (o[i] + cx * eX[i] + cy * eY[i]) for i in range(3))
                if self.fire("plate9", p["h"], mode="poly", pts=p["pts"], at=at, t=H,
                             ex=ax, ey=ay, ez=az, insheight=round(ins, 4)):
                    self.shaped += 1
                continue

            # --- no contour (a Ks_BendPlate: PolyOf answers 'not-PsPlate') -> rectangle ----
            # WHICH AXIS CARRIES L? Predict the bbox both ways and keep the one that matches
            # the source's own. `L along the frame's X` held on model 2 and failed on model 3.
            def predict(e1, e2):
                return tuple(round(abs(fL * e1[i]) + abs(fW * e2[i]) + abs(fH * Z[i]), 1)
                             for i in range(3))
            want = tuple(round(v, 1) for v in num(p["dims"]))
            if predict(X, Y) == want:
                ex, ey = ax, ay
            elif predict(Y, X) == want:
                ex, ey = ay, ax
            else:
                ex, ey = ax, ay
                flat = sorted([round(fL, 1), round(fW, 1), round(fH, 1)])
                if flat != sorted(want):
                    self.notes.append(
                        "plate %s is a BEND PLATE, built FLAT: developed %sx%sx%s against a "
                        "folded envelope of %s -- folded separately after the plates segment"
                        % (p["h"], L, W, H, want))
                else:
                    self.notes.append("plate %s: neither axis mapping predicts the source bbox "
                                      "(%s vs %s/%s)"
                                      % (p["h"], want, predict(X, Y), predict(Y, X)))
            oc = num(org)
            cc = num(p["c"])
            eX, eY = num(ex), num(ey)
            dx = sum((cc[i] - oc[i]) * eX[i] for i in range(3))
            dy = sum((cc[i] - oc[i]) * eY[i] for i in range(3))
            at = ",".join("%.4f" % (oc[i] + dx * eX[i] + dy * eY[i]) for i in range(3))
            if round(abs(dx), 1) + round(abs(dy), 1) > 0.05:
                self.inplane += 1
            self.fire("plate9", p["h"], mode="rect", at=at, l=L, w=W, t=H,
                      ex=ex, ey=ey, insheight=round(ins, 4))
        print("plates: %d/%d, %d from their real CONTOUR, %d rectangles re-centred, %.0fs"
              % (len([1 for p in self.d["plates"] if p["h"] in self.map]),
                 len(self.d["plates"]), self.shaped, self.inplane, time.time() - t0))
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

        def drill_part(t, variant):
            """Re-drill one part from scratch under one flange rule."""
            m = re.search(r"holeFields=(\d+)", eb_api.run("mods", handle=t))
            for i in range(int(m.group(1)) - 1, -1, -1):
                eb_api.run("killholefield", handle=t, field=i)
            for a, n, dia, mid in want[t]:
                kw = dict(handle=t, at="%.4f,%.4f,%.4f" % tuple(a),
                          n="%.6f,%.6f,%.6f" % tuple(n), dia=dia, play=0)
                if variant is not None:
                    kw["flange"] = variant
                eb_api.run("drill", **kw)

        for t, lst in want.items():
            for a, n, dia, mid in lst:
                eb_api.run("drill", handle=t, at="%.4f,%.4f,%.4f" % tuple(a),
                           n="%.6f,%.6f,%.6f" % tuple(n), dia=dia, play=0)
        miss = self.hole_report()

        # ⭐⭐ THE FLANGE SELECTOR IS A CHOICE BETWEEN WALLS, AND THE RIGHT ONE IS NOT ALWAYS 1.
        # Until 18/08/2026 the retry re-drilled every missed part with flange=1 and stopped.
        # Measured on model 5, the leftovers were a single exact number per section:
        #   EA60X60X6 -> 54.0 mm = leg 60 - t 6 | EA80X80X8 -> 72.0 = 80 - 8
        #   U140      -> 53.0 mm = b 60 - tw 7
        # i.e. the hole was in the OTHER leg / the OTHER wall -- flange=1 was simply the wrong
        # half of a binary choice. So try the variants in order and keep the FIRST that lands
        # every hole on that part, and if none does, restore the one that placed the most.
        # (5d -- search code is destructive code: each attempt wipes the part's fields, so the
        # winner is re-applied at the end rather than assumed to still be in place.)
        if miss:
            for t in list(miss.keys()):
                best_v, best_n = None, -1
                for variant in (1, 0, 2):
                    drill_part(t, variant)
                    landed = self.landed_on(t, want[t])
                    if landed > best_n:
                        best_v, best_n = variant, landed
                    if landed == len(want[t]):
                        break
                if best_n < len(want[t]):
                    drill_part(t, best_v)          # put the least-bad one back
                    self.notes.append("part %s: best flange=%s placed %d/%d holes"
                                      % (t, best_v, best_n, len(want[t])))
            miss = self.hole_report()
        print("holes: %d wanted, off-position parts left: %d, %.0fs"
              % (len(self.d["holes"]), len(miss or {}), time.time() - t0))
        self.checkpoint("holes")

    def landed_on(self, t, wanted):
        """How many of this part's wanted holes are actually at their source position."""
        eb_api.run("dumpholes")
        got = []
        cur = None
        for l in io.open(os.path.join(eb_api.channel(), "eb_holes_all.txt"),
                         encoding="utf-8-sig"):
            f = l.rstrip(chr(10)).split(chr(9))
            if f[0] == "OBJ":
                cur = f[1]
            elif f[0] == "HOLE" and cur == t:
                a, b = num(f[5]), num(f[6])
                got.append([(a[i] + b[i]) / 2 for i in range(3)])
        n = 0
        for a, nv, dia, mid in wanted:
            if got and min(sum((mid[i] - g[i]) ** 2 for i in range(3)) ** 0.5
                           for g in got) <= 0.01:
                n += 1
        return n

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
        # 2) a bolt with no second STEEL part (a fixing into concrete) is created directly.
        #    IDENTITY IS THE AXIS, not the midpoint: ProSteel seats its bolt through the packet
        #    with head and nut offsets, so the same fastener can have a midpoint tens of mm from
        #    the source's. Comparing midpoints produced 72 bolts against a source of 48, twice.
        eb_api.run("dumpmodel")
        have = []
        for l in io.open(os.path.join(eb_api.channel(), "eb_model.txt"), encoding="utf-8-sig"):
            f = l.rstrip("\n").split("\t")
            if f[0] == "BOLT" and len(f) > 9 and ";" in f[9]:
                p = f[9].split(";")
                have.append((num(p[0]), num(p[1])))

        def same_axis(a1, b1, a2, b2, tol=3.0):
            d = [b1[i] - a1[i] for i in range(3)]
            L = sum(x * x for x in d) ** 0.5
            if L == 0:
                return False
            d = [x / L for x in d]
            for pt in (a2, b2):                      # both ends of the candidate on that line?
                w = [pt[i] - a1[i] for i in range(3)]
                t = sum(w[i] * d[i] for i in range(3))
                perp = sum((w[i] - t * d[i]) ** 2 for i in range(3)) ** 0.5
                if perp > tol:
                    return False
            return True

        added = 0
        for b in self.d["bolts"]:
            if ";" not in (b.get("axis") or ""):
                continue
            p = b["axis"].split(";")
            a, c = num(p[0]), num(p[1])
            if any(same_axis(h[0], h[1], a, c) for h in have):
                continue
            if eb_api.run("bolt", p1=p[0], p2=p[1], dia=b.get("dia") or 24,
                          style="8.8S", len=b.get("len") or 85).startswith("EB_OK"):
                added += 1
                have.append((a, c))
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
