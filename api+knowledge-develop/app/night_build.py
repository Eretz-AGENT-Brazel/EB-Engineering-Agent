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

        # ⏱️ THE IN-BUILD FRAME VERIFY IS GONE, AND THE REASON IS A MEASUREMENT.
        # Reading `props` per member right after a build burst costs ~10 s each on a big model,
        # because most calls come back EB_BUSY and only the last retry lands: 129 of model 6's
        # 472 members took 21 minutes, i.e. an hour of pure waiting for a check that
        # `night_verify` already performs -- on BOTH drawings, with the steadier reader, as the
        # `shape FRAME` gate. A verification duplicated inside the build buys nothing and is
        # charged at the worst possible moment.
        wrong = unread = 0

        print("shapes: %d built, %d frames still disagree, %d unreadable, %.0fs"
              % (len([1 for s in self.d["shapes"] if s["h"] in self.map]), wrong, unread,
                 time.time() - t0))
        self.checkpoint("shapes")

    def plates(self):
        t0 = time.time()
        self.inplane = 0
        self.shaped = 0
        self.at_used = {}
        self.ins_used = {}
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
                self.at_used[p["h"]] = at
                self.ins_used[p["h"]] = round(ins, 4)
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
        # ⭐⭐ AND THEN MEASURE WHERE THEY LANDED. `mode=poly` re-centres the contour about
        # `at`, and predicting that from the contour's bbox centre is right for a symmetric
        # contour and wrong for an asymmetric one -- measured on model 5, 97 plates still
        # landed up to 134 mm out while their shapes were perfect. The cure is not a better
        # theory: `dumpfull2` returns every plate's world centre in ONE call, so build, read,
        # and re-place whoever missed, by the measured delta.
        # ⚠️ Correct against the `at` that was PASSED, never against the resulting bbox
        # midpoint: for a contour ProSteel re-centres, so correcting against the midpoint
        # re-injects the same error on every iteration (this file has that scar already).
        moved = self.replace_pass()
        for _ in range(3):
            if self.replace_pass() == 0:
                break
        if moved:
            print("   re-placed %d plate(s) by the measured delta" % moved)
        print("plates: %d/%d, %d from their real CONTOUR, %d rectangles re-centred, %.0fs"
              % (len([1 for p in self.d["plates"] if p["h"] in self.map]),
                 len(self.d["plates"]), self.shaped, self.inplane, time.time() - t0))
        self.checkpoint("plates")

    def replace_pass(self):
        """One measure-and-correct pass over the plates. Returns how many moved.

        ⭐ IT HAS TO ITERATE, AND ARCS ARE WHY. A contour vertex carries a BULGE in its third
        component, so the real bounding box of a curved plate reaches PAST its vertices by the
        arc's sagitta -- and `mode=poly` centres on that true bbox while the `at` we predict is
        computed from the vertices alone. Measured on model 6: 64 curved plates landed exactly
        **0.14 mm** out, every one of them, and their 64 holes with them. One corrective pass
        removes most of it; the loop runs until nothing moves.
        """
        eb_api.run("dumpfull2")
        got = {}
        for line in io.open(os.path.join(eb_api.channel(), "eb_full2.txt"),
                            encoding="utf-8-sig"):
            f = line.rstrip(chr(10)).split(chr(9))
            if f[0] == "PLATE":
                got[f[1]] = f[2]
        moved = 0
        for p in self.d["plates"]:
            t = self.map.get(p["h"])
            if not t or t not in got or p["h"] not in self.at_used:
                continue
            want, have = num(p["c"]), num(got[t])
            d3 = [want[i] - have[i] for i in range(3)]
            if max(abs(v) for v in d3) <= 0.01:
                continue
            at1 = ",".join("%.4f" % (num(self.at_used[p["h"]])[i] + d3[i]) for i in range(3))
            pr = p["props"]
            eb_api.delete(t)
            if self.fire("plate9", p["h"], mode="poly", pts=p["pts"], at=at1, t=pr["H"],
                         ex=axes(pr, "X"), ey=axes(pr, "Y"), ez=axes(pr, "Z"),
                         insheight=self.ins_used[p["h"]]):
                moved += 1
                self.at_used[p["h"]] = at1
        return moved

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
            # ⚠️ `mods` can answer with an error (a busy AutoCAD, a stale handle), and the
            # regex then matched nothing and this crashed the whole segment TWICE tonight.
            # A field count that cannot be read is not a reason to abandon 800 holes: retry,
            # and if it still will not answer, drill without wiping first.
            n = None
            for attempt in range(3):
                m = re.search(r"holeFields=(\d+)", eb_api.run("mods", handle=t))
                if m:
                    n = int(m.group(1))
                    break
                time.sleep(0.5 + attempt)
            if n is None:
                self.notes.append("part %s: mods unreadable, drilled without clearing" % t)
                n = 0
            for i in range(n - 1, -1, -1):
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
        """⭐⭐ REPRODUCE THE SOURCE'S OWN BOLTS, WITH ITS OWN STYLES AND LENGTHS.

        `boltparts` is the right instrument when ProSteel must DERIVE a joint's bolts from the
        holes -- it chose the source's own two lengths on model 4 without being told. It is the
        wrong instrument for a faithful 1:1 rebuild of a model like this one, and the numbers
        say why: the source carries **304 bolts on 304 distinct axes** in eight types, including
        **130 grade-4.6 M10** through the guardrail tubes' 12 and 13 mm holes. Derived bolting
        produced 158 bolts of six DIFFERENT types (M12x35, M16x75, M16x80, M20x90 ...) and not
        one M10 -- because the 8.8 table pairs on +2 only and has no row for a 12 or 13 mm hole.
        The cache already holds each bolt's own diameter, style, length and axis, so build them.

        ⚠️ The source itself reads `BOLT-NO-HOLE=98` on `vfy_fit`. Reproducing it faithfully
        reproduces that too. That is the instruction -- the source is the answer -- and the
        question is logged for Amir rather than silently "corrected".
        """
        t0 = time.time()
        eb_api.run("dumpmodel")
        old = [l.rstrip(chr(10)).split(chr(9))[1]
               for l in io.open(os.path.join(eb_api.channel(), "eb_model.txt"),
                                encoding="utf-8-sig")
               if l.startswith("BOLT")]
        for h in old:
            eb_api.delete(h)
        made = failed = 0
        self.by_grip = 0
        for b in self.d["bolts"]:
            if ";" not in (b.get("axis") or ""):
                continue
            p1, p2 = b["axis"].split(";")
            dia = float(b.get("dia") or 20)
            ln = float(b.get("len") or 50)
            r = eb_api.run("bolt", p1=p1, p2=p2, dia=dia,
                           style=b.get("style") or "8.8S", len=ln)
            if not r.startswith("EB_OK"):
                # ⭐⭐ A REFUSED STYLE IS USUALLY A REFUSED **GRIP**. `bolt` failed on all 306
                # DIN7991 screws in model 6 -- and `boltsingle`, which is B.15.1's manual
                # insertion where from->to IS THE GRIP LENGTH, built them at once. Measured
                # ladder for M12 DIN7991: grip 10->x30, 20->x40, 30->x50, 45->x65, 60->x80,
                # 62->x80, 65->x85, **70->x90**, 90->x110 -- and **grip 75 REFUSES**, because
                # that row does not exist. So the style was never the problem: a grip that
                # lands between rows is.
                # The grip that reproduces a given bolt is the skill's own rule read backwards:
                #     L = packet + 1.6d   =>   grip = L - 1.6d
                # For M12x90 that is 90 - 19.2 = 70.8, and 70 is exactly what produced x90.
                a, c = num(p1), num(p2)
                v = [c[i] - a[i] for i in range(3)]
                seg = sum(x * x for x in v) ** 0.5 or 1.0
                u = [x / seg for x in v]
                for grip in (ln - 1.6 * dia, ln - 1.6 * dia - 2, ln - 1.6 * dia + 2):
                    if grip <= 0:
                        continue
                    to = ",".join("%.4f" % (a[i] + u[i] * grip) for i in range(3))
                    r = eb_api.run("boltsingle", dia=dia, style=b.get("style") or "8.8S",
                                   **{"from": p1, "to": to})
                    if r.startswith("EB_OK"):
                        self.by_grip += 1
                        break
            if r.startswith("EB_OK"):
                made += 1
            else:
                failed += 1
                if failed <= 5:
                    self.notes.append("bolt %s (%s): %s" % (b["h"], b.get("name"), r[:90]))
        print("bolts: %d removed, %d rebuilt from the source's own styles "
              "(%d of them via boltsingle by GRIP), %d refused, %.0fs"
              % (len(old), made, self.by_grip, failed, time.time() - t0))
        self.checkpoint("bolts")


def repair(b):
    """⭐ BUILD ONLY WHAT IS MISSING. A run can lose parts without losing the model: on
    19/08/2026 a ProSteel dialog belonging to Amir's OWN second AutoCAD instance made the
    guard refuse ops mid-build, and 96 shapes and 86 plates never got created -- with zero
    orphans and everything else correct. Re-running a segment would duplicate the 800 parts
    that are fine, and a full rebuild would cost 40 minutes to recover 182 parts. So: read
    what is live, keep only the source parts that have no counterpart, and run the normal
    segments over that subset."""
    eb_api.run("list")
    live = set()
    for line in io.open(os.path.join(eb_api.channel(), "eb_list.txt"), encoding="utf-8-sig"):
        f = line.rstrip().split("|")
        if len(f) > 1 and f[1].strip().startswith("Ks_"):
            live.add(f[0].strip())
    ms = [x for x in b.d["shapes"] if b.map.get(x["h"]) not in live]
    mp_ = [x for x in b.d["plates"] if b.map.get(x["h"]) not in live]
    print("repair: %d shape(s) and %d plate(s) missing" % (len(ms), len(mp_)))
    if not ms and not mp_:
        return
    keep = set(x["h"] for x in ms) | set(x["h"] for x in mp_)
    all_s, all_p = b.d["shapes"], b.d["plates"]
    all_cuts = b.d["cuts"]
    b.d["shapes"], b.d["plates"] = ms, mp_
    b.d["cuts"] = dict((k, v) for k, v in all_cuts.items() if k in keep)
    holes_all = b.d["holes"]
    b.d["holes"] = [h for h in holes_all if h["owner"] in keep]
    if ms:
        b.shapes()
    if mp_:
        b.plates()
    b.cuts()
    b.holes()
    b.d["shapes"], b.d["plates"], b.d["cuts"], b.d["holes"] = all_s, all_p, all_cuts, holes_all
    b.bolts()                     # bolts() clears and rebuilds them all -- idempotent


def main(argv):
    b = Build(argv[0], argv[1])
    seg = (argv[2] if len(argv) > 2 else "all").lower()
    b.enter()
    if seg == "repair":
        repair(b)
        b.save_map()
        print("census:", eb_api.run("dumpmodel")[:110])
        for n in b.notes[:15]:
            print("   note:", n)
        return 0
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
