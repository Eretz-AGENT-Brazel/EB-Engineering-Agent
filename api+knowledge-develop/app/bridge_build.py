# -*- coding: utf-8 -*-
"""bridge_build.py -- build a 1:1 rebuild of a LARGE model from bridge_read.py's cache.

    python app/bridge_build.py "<source.json>" "<rebuild.dwg>" [segment ...]

    segments: shapes plates cuts facets polycuts holes bolts   (default: all, in order)

Same pipeline night_build.py proved on models 1-6, driven through the v190 `batch` op: one
round trip per few thousand parts instead of one per part. Everything else is deliberately
unchanged, because every line of it was paid for:

  * the section frame is a PARAMETER (`ax`/`ay`) and `rot` must then be 0 -- the dump's
    rotation is already baked into the axes, so applying it again turns the section twice
  * a plate is its CONTOUR, not L/W/H, and `at` is where the contour's bbox centre must
    land -- `plate9 mode=poly` re-centres about that point
  * a vertex's third component is the BULGE, and it is geometry
  * the flange selector is a binary choice between walls, and 1 is not always right
  * every segment SAVES, so a crash costs one segment

⚠️ RESUMABLE. The source->rebuild handle map is written after every chunk; a part already in
the map is never built twice.
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

CHUNK = 2000


def num(s):
    return [float(v) for v in str(s).replace("/", ",").split(",")]


def axes(p, key):
    v = (p.get(key) or "").replace("/", ",")
    return v if v.count(",") == 2 else None


class Build(object):
    def __init__(self, cache, rebuild):
        self.d = json.load(io.open(cache, encoding="utf-8"))
        self.cache = cache
        self.rbd = rebuild
        self.map_path = os.path.splitext(cache)[0] + "-map.json"
        self.notes_path = os.path.splitext(cache)[0] + "-notes.txt"
        self.at_path = os.path.splitext(cache)[0] + "-at.json"
        self.map = (json.load(io.open(self.map_path, encoding="utf-8"))
                    if os.path.exists(self.map_path) else {})
        self.at_used = (json.load(io.open(self.at_path, encoding="utf-8"))
                        if os.path.exists(self.at_path) else {})
        self.notes = []

    # ---------------------------------------------------------------- plumbing
    def enter(self):
        eb_api.use(os.path.basename(self.rbd),
                   task="1:1 rebuild of the bridge model (night 20/08)",
                   project="bridge-bernie")
        eb_api.build_target(self.rbd)
        # ⚠️ `whoami` IS A DIAGNOSTIC: it goes to the shared mailbox and is NOT gated, so it
        # answers for whatever document happens to be in front -- measured 20/08/2026, when it
        # named the SOURCE while the beams were landing correctly in the rebuild. A gated op
        # activates the pinned document; COM then says which document that actually is.
        eb_api.run("list", wait=300)                     # gated -> activates the pin
        act = eb_api._active_doc_name() or ""
        want = os.path.basename(self.rbd)
        if act.lower() != want.lower():
            raise RuntimeError("active document is %r, not the rebuild %r" % (act, want))
        if "REBUILD" not in act:
            raise RuntimeError("refusing to build into a drawing that is not the REBUILD")
        print("in: %s  (%s)" % (act, eb_api.run("list", wait=300)[:60]))

    def save(self, label):
        before = os.path.getmtime(self.rbd) if os.path.exists(self.rbd) else 0
        r = eb_api.run("save", wait=900)
        after = os.path.getmtime(self.rbd) if os.path.exists(self.rbd) else 0
        json.dump(self.map, io.open(self.map_path, "w", encoding="utf-8"))
        json.dump(self.at_used, io.open(self.at_path, "w", encoding="utf-8"))
        io.open(self.notes_path, "w", encoding="utf-8").write("\n".join(self.notes))
        # ⚠️ "saved" is a file whose mtime moved, not an op that returned EB_OK.
        print("   [save] %-9s %s  (mtime %s, %.1f MB)"
              % (label, "OK" if after > before else "UNCHANGED",
                 time.strftime("%H:%M:%S", time.localtime(after)),
                 os.path.getsize(self.rbd) / 1e6))
        return r

    def fire_batch(self, items, srcs, label):
        """items: [(op, kw)] with a parallel list of source handles. Returns #mapped."""
        done = 0
        t0 = time.time()
        for a in range(0, len(items), CHUNK):
            part_items, part_srcs = items[a:a + CHUNK], srcs[a:a + CHUNK]
            rows = eb_api.batch(part_items, wait=max(300, 0.4 * len(part_items) + 120))
            for (i, op, res) in rows:
                if i < 0:
                    self.notes.append("%s: BATCH FAILED: %s" % (label, res[:200]))
                    continue
                if i >= len(part_srcs):
                    continue
                m = re.search(r"handle=(\w+)", res)
                if res.startswith("EB_OK") and m and m.group(1) != "-":
                    if part_srcs[i]:
                        self.map[part_srcs[i]] = m.group(1)
                    done += 1
                else:
                    self.notes.append("%s %s: %s" % (op, part_srcs[i], res[:160]))
            print("   %s %d/%d built, %.0fs" % (label, done, len(items), time.time() - t0))
            self.save("%s+%d" % (label, a + len(part_items)))
        return done

    # ---------------------------------------------------------------- segments
    def shapes(self):
        items, srcs = [], []
        for s in self.d["shapes"]:
            if s["h"] in self.map:
                continue
            kw = dict(kind="standard", name=s["sec"], catalog=s["cat"],
                      p1=s["p1"], p2=s["p2"], rot=s["rot"], mirror=s["mir"])
            if s.get("layer"):
                kw["layer"] = s["layer"]
            ax, ay = axes(s.get("props", {}), "X"), axes(s.get("props", {}), "Y")
            if ax and ay:
                kw["ax"], kw["ay"], kw["rot"] = ax, ay, 0
            off = (s.get("off") or "").split(",")
            if len(off) == 2 and (off[0] not in ("", "0") or off[1] not in ("", "0")):
                kw["offx"], kw["offy"] = off[0], off[1]
            items.append(("beam", kw))
            srcs.append(s["h"])
        print("shapes: %d to build (%d already mapped)"
              % (len(items), len(self.d["shapes"]) - len(items)))
        self.fire_batch(items, srcs, "shapes")

    def plates(self):
        items, srcs = [], []
        shaped = rect = 0
        for p in self.d["plates"]:
            if p["h"] in self.map:
                continue
            pr = p.get("props", {})
            L, W, H = pr.get("L"), pr.get("W"), pr.get("H")
            ax, ay, az = axes(pr, "X"), axes(pr, "Y"), axes(pr, "Z")
            org = pr.get("org")
            if not (L and W and H and ax and ay and az and org):
                self.notes.append("plate %s: props incomplete, skipped" % p["h"])
                continue
            Z = num(az)
            ins = 0.0
            mid = pr.get("mid", "")
            if "->" in mid:
                a, b = [num(x) for x in mid.split("->")]
                o = num(org)
                ins = sum(((a[i] + b[i]) / 2.0 - o[i]) * Z[i] for i in range(3))
            kw = dict(t=H, ex=ax, ey=ay, ez=az, insheight=round(ins, 4))
            if p.get("layer"):
                kw["layer"] = p["layer"]
            if p.get("pts"):
                # ⭐⭐⭐ `at` IS THE PLATE'S OWN ORIGIN, AND NOTHING ELSE. Measured on the
                # bridge model, 20/08/2026, on 7,807 plates: aiming at the contour's bbox
                # centre (`org + cx*eX + cy*eY`, which is what models 1-6 did) put 6,762 of
                # them out of place -- by 89.0 mm on 1,368, by 21.5 on 772, and by 4,543.5 mm
                # on the members whose contour runs x -5031..-4056, i.e. by EXACTLY the
                # contour's own offset, applied twice.
                #   at = org + centre   ->  landed at org + 2*centre
                #   at = org            ->  landed at org + centre == the source, 0.0000
                # ⇒ `plate9 mode=poly` does NOT re-centre the polygon about `at`: it treats
                # `at` as the origin of the local frame and honours the contour's own
                # coordinates. The two readings agree only when the contour is symmetric
                # about its origin -- which is why 1,045 plates landed right and why model 5,
                # whose contours are near their origins, never showed the difference as more
                # than the small displacement it recorded as "re-centring".
                kw.update(mode="poly", pts=p["pts"], at=org)
                at = org
                self.at_used[p["h"]] = at
                shaped += 1
            else:
                fL, fW, fH = float(L), float(W), float(H)
                X, Y = num(ax), num(ay)

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
                            "plate %s is a BEND PLATE, built FLAT: %sx%sx%s against a folded "
                            "envelope of %s" % (p["h"], L, W, H, want))
                oc, cc = num(org), num(p["c"])
                eX, eY = num(ex), num(ey)
                dx = sum((cc[i] - oc[i]) * eX[i] for i in range(3))
                dy = sum((cc[i] - oc[i]) * eY[i] for i in range(3))
                at = ",".join("%.4f" % (oc[i] + dx * eX[i] + dy * eY[i]) for i in range(3))
                kw.update(mode="rect", at=at, l=L, w=W, ex=ex, ey=ey)
                kw.pop("ez", None)
                self.at_used[p["h"]] = at
                rect += 1
            items.append(("plate9", kw))
            srcs.append(p["h"])
        print("plates: %d to build (%d from a real contour, %d rectangles)"
              % (len(items), shaped, rect))
        self.fire_batch(items, srcs, "plates")
        for n in range(4):
            moved = self.replace_pass()
            print("   re-place pass %d: %d moved" % (n + 1, moved))
            if moved == 0:
                break
        self.save("plates")

    def replace_pass(self):
        """Measure where the plates landed and correct by the measured delta."""
        eb_api.run("dumpfull2", wait=600)
        got = {}
        for line in io.open(os.path.join(eb_api.channel(), "eb_full2.txt"),
                            encoding="utf-8-sig"):
            f = line.rstrip(chr(10)).split(chr(9))
            if f and f[0] == "PLATE":
                got[f[1]] = f[2]
        items, srcs, dels = [], [], []
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
            kw = dict(t=pr["H"], ex=axes(pr, "X"), ey=axes(pr, "Y"), ez=axes(pr, "Z"),
                      at=at1)
            if p.get("layer"):
                kw["layer"] = p["layer"]
            if p.get("pts"):
                kw.update(mode="poly", pts=p["pts"])
            else:
                kw.update(mode="rect", l=pr["L"], w=pr["W"])
                kw.pop("ez", None)
            mid = pr.get("mid", "")
            if "->" in mid:
                a, b = [num(x) for x in mid.split("->")]
                o, Z = num(pr["org"]), num(axes(pr, "Z"))
                kw["insheight"] = round(sum(((a[i] + b[i]) / 2.0 - o[i]) * Z[i]
                                            for i in range(3)), 4)
            dels.append(t)
            items.append(("plate9", kw))
            srcs.append(p["h"])
            self.at_used[p["h"]] = at1
        if not items:
            return 0
        # ⚡ ONE `erase` CALL, NOT ONE COM DELETE PER PLATE. Measured 20/08/2026: deleting
        # 902 plates through COM took over three minutes (~0.2 s of round trip each) while
        # the v193 `erase` op does the same thing inside one transaction in well under a
        # second. On a model this size the plumbing IS the cost.
        for a in range(0, len(dels), 400):
            eb_api.run("erase", wait=600, handles=",".join(dels[a:a + 400]))
        return self.fire_batch(items, srcs, "replace")

    def cuts(self):
        items, srcs, n = [], [], 0
        for src, cs in self.d["cuts"].items():
            t = self.map.get(src)
            if not t:
                continue
            for c in cs:
                n += 1
                if not isinstance(c.get("n"), list):
                    continue
                items.append(("planecut", dict(
                    handle=t, at="%.4f,%.4f,%.4f" % tuple(c["ip"]),
                    normal="%.8f,%.8f,%.8f" % tuple(c["n"]))))
                srcs.append(None)
        print("cuts: %d planes on %d parts" % (n, len(self.d["cuts"])))
        ok = 0
        t0 = time.time()
        for a in range(0, len(items), CHUNK):
            rows = eb_api.batch(items[a:a + CHUNK], wait=max(600, 0.6 * CHUNK))
            ok += len([1 for (i, o, r) in rows if r.startswith("EB_OK")])
            for (i, o, r) in rows:
                if not r.startswith("EB_OK"):
                    self.notes.append("planecut: " + r[:160])
            print("   cuts %d/%d applied, %.0fs" % (ok, len(items), time.time() - t0))
            self.save("cuts+%d" % (a + CHUNK))
        print("cuts: %d/%d applied" % (ok, len(items)))

    def facets(self):
        """Corner chamfers, from the `mods` detail: type, d1, d2 and the edge index.

        ⭐ IDEMPOTENT ON PURPOSE. A chamfer leaves no handle of its own, so there is nothing
        to put in the map -- which means a re-run would happily add a second copy of every
        facet, and a doubled chamfer is invisible in every count except the facet count
        itself. So the segment READS what each part already carries and applies only the
        remainder. (Measured need: a 61-chamfer pilot had already landed before the full
        segment ran, and the source's own parts carry 1-2 facets each.)
        """
        owners = {}
        for src, text in self.d.get("modtext", {}).items():
            t = self.map.get(src)
            if not t:
                continue
            fs = re.findall(
                r"facet\[(\d+)\] type=(\d+) d1=([-\d.]+) d2=([-\d.]+) edge=(-?\d+)", text)
            if fs:
                owners[t] = fs
        if not owners:
            print("facets: nothing to apply")
            return
        hs = list(owners)
        have = {}
        for a in range(0, len(hs), 4000):
            rows = eb_api.batch([("mods", {"handle": h}) for h in hs[a:a + 4000]], wait=1800)
            for (i, op, res) in rows:
                if i < 0 or i >= len(hs[a:a + 4000]):
                    continue
                m = re.search(r"facets=(\d+)", res)
                have[hs[a:a + 4000][i]] = int(m.group(1)) if m else 0
        items = []
        already = 0
        for t, fs in owners.items():
            n = have.get(t, 0)
            already += min(n, len(fs))
            for f in fs[n:]:
                items.append(("chamfer", dict(handle=t, type=f[1], d1=f[2], d2=f[3],
                                              edge=f[4])))
        print("facets: %d to apply on %d parts (%d already present)"
              % (len(items), len(owners), already))
        ok = 0
        t0 = time.time()
        for a in range(0, len(items), CHUNK):
            rows = eb_api.batch(items[a:a + CHUNK], wait=1800)
            ok += len([1 for (i, o, r) in rows if r.startswith("EB_OK")])
            for (i, o, r) in rows:
                if not r.startswith("EB_OK"):
                    self.notes.append("chamfer: " + r[:160])
            print("   facets %d/%d, %.0fs" % (ok, len(items), time.time() - t0))
            self.save("facets+%d" % (a + CHUNK))
        print("facets: %d/%d applied" % (ok, len(items)))

    def polycuts(self):
        """A poly-cut is a polygon in the part's own frame; a CIRCLE arrives as two
        bulge-1.0 vertices and must go back as shape=circle, or it refuses with area=0."""
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

        items, n = [], 0
        for src, cuts in self.d.get("polycuts", {}).items():
            t = self.map.get(src)
            if not t:
                continue
            for c in cuts:
                n += 1
                v = c.get("v") or []
                if len(v) < 2:
                    continue
                circ = as_circle(v)
                if circ:
                    cx, cy, r = circ
                    items.append(("polycut", dict(handle=t, shape="circle",
                                                  at="%.4f,%.4f" % (cx, cy), r="%.4f" % r)))
                else:
                    pts = ";".join(",".join("%.5f" % c2 for c2 in q) for q in v)
                    items.append(("polycut", dict(handle=t, shape="pts", pts=pts)))
        print("poly-cuts: %d to apply on %d parts" % (n, len(self.d.get("polycuts", {}))))
        ok = 0
        t0 = time.time()
        for a in range(0, len(items), 500):
            rows = eb_api.batch(items[a:a + 500], wait=3600)
            ok += len([1 for (i, o, r) in rows if r.startswith("EB_OK")])
            for (i, o, r) in rows:
                if not r.startswith("EB_OK"):
                    self.notes.append("polycut: " + r[:160])
            print("   polycuts %d/%d, %.0fs" % (ok, len(items), time.time() - t0))
            self.save("polycuts+%d" % (a + 500))
        print("poly-cuts: %d/%d applied" % (ok, len(items)))

    def holes(self):
        """Drill from the RESOLVED hole list -- and a slot is one hole, not two.

        ⭐ `dumpholes lhm=` decides what a slotted hole even looks like. Measured on the
        bridge model, 20/08/2026, the same 2,282 parts read three ways:
            lhm=0 (kSingleHole) -> 11,657 holes,     0 flagged slotted  (the flag is LOST)
            lhm=1 (kLongHole)   -> 11,657 holes,   608 flagged slotted  (one row per slot)
            lhm=2 (kDoubleHole) -> 12,265 holes, 1,216 flagged slotted  (608 x 2 END CIRCLES)
        `getMaximalLength` answers 0 in all three, so the slot's travel is not readable as a
        property -- but the DISTANCE BETWEEN THE PAIRED CIRCLES under lhm=2 is exactly it:
        9.0 mm on 318 of them, 10.0 on 179, 7.0 on 65, 8.0 on 16. All 608 paired, none left
        over. ⇒ read lhm=2 to MEASURE the slot, lhm=1 to COUNT the holes.
        ⚠️ And the default is lhm=2, so a reader that trusts the count sees 608 holes that
        are not there.
        """
        path = os.path.join(os.path.dirname(self.cache), "holes_resolved.json")
        want = json.load(io.open(path, encoding="utf-8"))
        items, srcs = [], []
        for owner, lst in want.items():
            t = self.map.get(owner)
            if not t:
                continue
            for h in lst:
                a, b = h["a"], h["b"]
                v = [b[i] - a[i] for i in range(3)]
                L = sum(x * x for x in v) ** 0.5
                if L <= 0:
                    continue
                kw = dict(handle=t, at="%.4f,%.4f,%.4f" % tuple(a),
                          n="%.6f,%.6f,%.6f" % tuple(x / L for x in v),
                          dia=h["d"], play=0)
                if h.get("slot"):
                    kw["slot"] = h["slot"]
                items.append(("drill", kw))
                srcs.append(None)
        print("holes: %d to drill (%d slotted) on %d parts"
              % (len(items), len([1 for i in items if "slot" in i[1]]),
                 len([1 for o in want if o in self.map])))
        ok = 0
        t0 = time.time()
        for a in range(0, len(items), CHUNK):
            rows = eb_api.batch(items[a:a + CHUNK], wait=3600)
            ok += len([1 for (i, o, r) in rows if r.startswith("EB_OK")])
            for (i, o, r) in rows:
                if not r.startswith("EB_OK"):
                    self.notes.append("drill: " + r[:160])
            print("   holes %d/%d, %.0fs" % (ok, len(items), time.time() - t0))
            self.save("holes+%d" % (a + CHUNK))
        print("holes: %d/%d drilled" % (ok, len(items)))

    def bolts(self):
        """One bolt per source bolt, on the source's own axis, with its own style and length.

        The iron rule is satisfied BY CONSTRUCTION here: every bolt sits where the source's
        bolt sits, and the source's holes were drilled in the segment before this one -- so
        `vfy_fit` measuring BOLT-NO-HOLE=0 is the proof, not the intention.
        ⚠️ A style refuses a GRIP it has no table row for, silently (THE-CEILING, 13/08), so
        the length is passed and then forced -- and every refusal is recorded per bolt.
        """
        items, srcs = [], []
        for b in self.d["bolts"]:
            if b["h"] in self.map or ";" not in (b.get("axis") or ""):
                continue
            p1, p2 = b["axis"].split(";")
            kw = dict(p1=p1, p2=p2, dia=b["dia"], style=b["style"])
            if b.get("len"):
                kw["len"] = b["len"]
            if b.get("layer"):
                kw["layer"] = b["layer"]
            items.append(("bolt", kw))
            srcs.append(b["h"])
        print("bolts: %d to place" % len(items))
        self.fire_batch(items, srcs, "bolts")

    def report(self):
        io.open(self.notes_path, "w", encoding="utf-8").write("\n".join(self.notes))
        print("notes: %d -> %s" % (len(self.notes), self.notes_path))


SEGMENTS = ("shapes", "plates", "cuts", "facets", "polycuts", "holes", "bolts")

if __name__ == "__main__":
    if len(sys.argv) < 3:
        print(__doc__)
        sys.exit(2)
    b = Build(sys.argv[1], sys.argv[2])
    b.enter()
    todo = sys.argv[3:] or list(SEGMENTS)
    for seg in todo:
        if seg not in SEGMENTS:
            raise SystemExit("unknown segment: " + seg)
        print("\n=== %s ===" % seg.upper())
        getattr(b, seg)()
    b.report()
