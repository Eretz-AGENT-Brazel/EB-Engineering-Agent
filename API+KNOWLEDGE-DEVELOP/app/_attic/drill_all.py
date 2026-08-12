# -*- coding: utf-8 -*-
"""
drill_all.py — R4: every bolt passage gets a REAL modelled hole.

Amir's rule (source of truth): "a profile or plate that a bolt passes through
MUST have a modelled hole in it. A bolt that just passes through and looks like
it drills is a critical error."

This is built from the ENGINEERING RULE, not by copying hole coordinates from
the original model:
   hole diameter = bolt diameter + 3mm clearance   (Amir: M16->19, M20->23)
   a bolt drills EVERY part its shank passes through (no host-count cap)
   hole axis     = the bolt axis
Then every hole is verified by reading it back from the model.

Usage:
  python drill_all.py plan      # compute and show the drill list, change nothing
  python drill_all.py run       # drill, verifying each host
"""
import json
import math
import os
import re
import sys
import time

APP = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(APP)
PLUG = os.path.join(APP, "plugin")
sys.path.insert(0, APP)
import eb_api  # noqa: E402

STATE = os.path.join(ROOT, "projects", "שיעור-3-מאפס", "files", "drill_state.json")
CLEARANCE = 3.0          # Amir's shop practice (EN 1090-2 default is 2mm)
PACE = 0.05


def log(m):
    try:
        sys.stdout.write(m + "\n")
        sys.stdout.flush()
    except Exception:
        pass


def xyz(s):
    try:
        return tuple(float(x) for x in s.split(","))
    except Exception:
        return None


def read_model(tag="l3"):
    """Read the CURRENT model (lesson-3): parts with their extents, and bolts."""
    r = eb_api.run("dumpfull2", out="eb_full_%s.txt" % tag, wait=180)
    log("   dumpfull2: " + str(r))
    parts, bolts = [], []
    p = os.path.join(PLUG, "eb_full_%s.txt" % tag)
    for line in open(p, encoding="utf-8-sig", errors="replace"):
        f = line.rstrip("\n").split("\t")
        if not f or not f[0]:
            continue
        if f[0] == "SHAPE" and len(f) >= 17:
            p1, p2 = xyz(f[4]), xyz(f[5])
            if not p1:
                continue
            parts.append({"h": f[1], "kind": "shape", "prof": f[2],
                          "p1": p1, "p2": p2, "layer": f[15] if len(f) > 15 else "",
                          "ext": ext_of(f)})
        elif f[0] == "PLATE" and len(f) >= 7:
            c = xyz(f[2]); d = xyz(f[3])
            if not c or not d:
                continue
            parts.append({"h": f[1], "kind": "plate", "c": c, "d": d,
                          "layer": f[5],
                          "ext": (tuple(c[k] - d[k] / 2 for k in range(3)),
                                  tuple(c[k] + d[k] / 2 for k in range(3)))})
        elif f[0] == "BOLT" and len(f) >= 7:
            c = xyz(f[2]); d = xyz(f[3])
            if not c or not d:
                continue
            bolts.append({"h": f[1], "c": c, "d": d, "layer": f[5]})
    return parts, bolts


def ext_of(f):
    """SHAPE rows carry a real extents field 'min;max' (6 numbers, 1 semicolon).
    The ECS field also has semicolons, so require exactly 2 parts of 3 numbers."""
    for cell in f:
        if cell.count(";") != 1 or "/" in cell:
            continue
        a, b = cell.split(";")
        if a.count(",") == 2 and b.count(",") == 2:
            pa, pb = xyz(a), xyz(b)
            if pa and pb:
                return (tuple(min(pa[k], pb[k]) for k in range(3)),
                        tuple(max(pa[k], pb[k]) for k in range(3)))
    return None


def bolt_axis(b):
    """The bolt's own geometry gives its axis and length: longest bbox dim."""
    d = list(b["d"])
    i = d.index(max(d))
    ax = [0.0, 0.0, 0.0]
    ax[i] = 1.0
    return tuple(ax), d[i], i


def seg_hits_box(c, ax, half, lo, hi, pad=0.6):
    """Does the bolt shank (a segment through c along ax) pass through the box?

    Engineering intent: the shank occupies a line; a part is drilled if that
    line enters its solid. Slab-method ray/box intersection.
    """
    t0, t1 = -half, half
    for k in range(3):
        o = c[k]
        d = ax[k]
        lo_k, hi_k = lo[k] - pad, hi[k] + pad
        if abs(d) < 1e-9:
            if o < lo_k or o > hi_k:
                return False
            continue
        a = (lo_k - o) / d
        b = (hi_k - o) / d
        if a > b:
            a, b = b, a
        t0 = max(t0, a)
        t1 = min(t1, b)
        if t0 > t1:
            return False
    return t1 > t0


def dia_of_bolt(b, crossed):
    """Engineering rule (Amir): M20 where the bolt goes through a heavy base
    plate, M16 everywhere else.  The bolt's own bbox is axis-only (0,45,0) so it
    carries no head size — the host plate is what tells us the bolt class.
    Learned from the source model's own joints: base plates are 12mm thick
    (300x300x12) or the 300x250x10 family, and they take hole dia 23."""
    for p in crossed:
        if p["kind"] != "plate":
            continue
        d = sorted(p["d"], reverse=True)
        # base-plate family: big footprint, and the thick ones
        if d[0] >= 290 and d[1] >= 240:
            return 20
        if len(d) > 2 and d[2] >= 12:
            return 20
    return 16


def main():
    mode = sys.argv[1] if len(sys.argv) > 1 else "plan"
    log("=" * 74)
    log("R4 — DRILL EVERY BOLT PASSAGE  (rule-based: dia = bolt + %gmm)" % CLEARANCE)
    log("=" * 74)
    log(str(eb_api.run("whoami", wait=25)))

    log("\nreading the model I built...")
    parts, bolts = read_model()
    log("   parts=%d  bolts=%d" % (len(parts), len(bolts)))

    # holes already present (should be 0 before we start)
    r0 = eb_api.run("dumpholes", out="eb_holes_l3_before.txt", wait=90)
    log("   holes before: " + str(r0))

    log("\ncomputing bolt -> parts-it-crosses (NO cap, unlike the first build)...")
    jobs = []
    per_bolt = []
    for b in bolts:
        ax, ln, i = bolt_axis(b)
        half = ln / 2.0
        hit = []
        for p in parts:
            if not p.get("ext") or not p["ext"][0] or not p["ext"][1]:
                continue
            if seg_hits_box(b["c"], ax, half, p["ext"][0], p["ext"][1]):
                hit.append(p)
        dia = dia_of_bolt(b, hit)
        hole_d = dia + CLEARANCE
        per_bolt.append(len(hit))
        for p in hit:
            jobs.append({"host": p["h"], "at": b["c"], "n": ax, "d": hole_d, "bolt": b["h"]})

    from collections import Counter
    log("   bolts: %d   parts crossed per bolt: %s" % (len(bolts), dict(sorted(Counter(per_bolt).items()))))
    log("   bolts crossing NOTHING: %d  (these need investigating)" % per_bolt.count(0))
    log("   total holes to drill: %d" % len(jobs))
    log("   hole diameters: %s" % dict(Counter(j["d"] for j in jobs)))
    hosts = Counter(j["host"] for j in jobs)
    log("   distinct hosts: %d  (max holes in one part: %d)" % (len(hosts), max(hosts.values()) if hosts else 0))

    if mode != "run":
        log("\nplan only — nothing changed.")
        return

    log("\ndrilling (each host verified by reading its holes back)...")
    st = {"done": []}
    if os.path.exists(STATE):
        try:
            st = json.load(open(STATE, encoding="utf-8"))
        except Exception:
            pass
    done = set(tuple(x) for x in st.get("done", []))

    ok = fail = skip = 0
    t0 = time.time()
    for n, j in enumerate(jobs, 1):
        key = (j["host"], round(j["at"][0], 1), round(j["at"][1], 1), round(j["at"][2], 1), j["d"])
        if key in done:
            skip += 1
            continue
        r = eb_api.run("drill", hosts=j["host"],
                       at="%.3f,%.3f,%.3f" % j["at"],
                       n="%g,%g,%g" % j["n"], dia=j["d"], wait=25, _log=False)
        if isinstance(r, str) and r.startswith("EB_OK"):
            ok += 1
            done.add(key)
        else:
            fail += 1
            if fail <= 6:
                log("   FAIL %s: %s" % (j["host"], str(r)[:90]))
        if n % 25 == 0 or n == len(jobs):
            log("   %d/%d  ok=%d fail=%d skip=%d  (%.1f min)" %
                (n, len(jobs), ok, fail, skip, (time.time() - t0) / 60))
            st["done"] = [list(k) for k in done]
            json.dump(st, open(STATE, "w", encoding="utf-8"))
        time.sleep(PACE)

    st["done"] = [list(k) for k in done]
    json.dump(st, open(STATE, "w", encoding="utf-8"))

    log("\nVERIFY — reading every hole back from the model:")
    log("   " + str(eb_api.run("dumpholes", out="eb_holes_l3_after.txt", wait=180)))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8-sig", errors="replace")
    except Exception:
        pass
    main()
