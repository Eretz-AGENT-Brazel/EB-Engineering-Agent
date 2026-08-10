# -*- coding: utf-8 -*-
"""
rebuild_full.py — build the COMPLETE model copy: shapes + plates + bolts.

Fixes every error found by self_audit.py:
  1. deletes the previous (skeleton-only, duplicated) copy first
  2. carries insert offsets (offx/offy) — 92 of 175 elements have them
  3. carries the mirror flag — 39 elements
  4. builds the 314 plates and 304 bolts (the connections = the heart)

Usage:
    python rebuild_full.py wipe            # delete everything at X >= SPLIT
    python rebuild_full.py plan            # show what would be built
    python rebuild_full.py run [shapes|plates|bolts|all]
"""
import os
import sys
import time
import math

APP = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(APP)
sys.path.insert(0, APP)

import eb_api          # noqa: E402
import console as C    # noqa: E402

FULL = os.path.join(APP, "plugin", "eb_full.txt")
SPLIT = 15000.0
DX = 15000.0
PACE = 0.25            # fast: hundreds of objects


def say(text):
    pp = C.P()
    if pp:
        C._append(pp["outbox"], {"text": text, "ts": time.strftime("%H:%M:%S")})
        C._append(pp["conv"], {"role": "agent", "text": text, "files": []})
    try:
        sys.stdout.write(text.replace("\n", " ")[:150] + "\n")
        sys.stdout.flush()
    except Exception:
        pass


def xyz(s):
    try:
        return tuple(float(x) for x in s.split(","))
    except Exception:
        return (0.0, 0.0, 0.0)


def load():
    sh, pl, bo = [], [], []
    for line in open(FULL, encoding="utf-8").read().splitlines():
        f = line.split("\t")
        if not f or not f[0]:
            continue
        if f[0] == "SHAPE" and len(f) >= 14:
            off = f[10].split(",")
            sh.append({"h": f[1], "profile": f[2], "catalog": f[3],
                       "p1": xyz(f[4]), "p2": xyz(f[5]), "length": float(f[6] or 0),
                       "rot": float(f[9] or 0),
                       "offx": float(off[0] or 0) if off else 0.0,
                       "offy": float(off[1] or 0) if len(off) > 1 else 0.0,
                       "mirror": f[12] == "1"})
        elif f[0] == "PLATE" and len(f) >= 5:
            pl.append({"h": f[1], "c": xyz(f[2]), "d": xyz(f[3])})
        elif f[0] == "BOLT" and len(f) >= 5:
            bo.append({"h": f[1], "c": xyz(f[2]), "d": xyz(f[3])})
    return sh, pl, bo


def orig(items, key="p1"):
    return [e for e in items if e[key][0] < SPLIT]


def copyside(items, key="p1"):
    return [e for e in items if e[key][0] >= SPLIT]


def off(p):
    return (p[0] + DX, p[1], p[2])


# ---------- plate geometry from extents ----------
def plate_params(p):
    """From a bounding box: thickness = smallest dim, its axis = the normal."""
    d = list(p["d"])
    ti = d.index(min(d))
    t = d[ti]
    rest = [d[i] for i in range(3) if i != ti]
    L, W = max(rest), min(rest)
    normal = [0.0, 0.0, 0.0]
    normal[ti] = 1.0
    return L, W, t, tuple(normal)


def bolt_params(b):
    """Bolt extents are degenerate: one axis carries the length."""
    d = list(b["d"])
    ai = d.index(max(d))
    L = d[ai]
    axis = [0.0, 0.0, 0.0]
    axis[ai] = 1.0
    c = b["c"]
    half = L / 2.0
    p1 = tuple(c[k] - axis[k] * half for k in range(3))
    p2 = tuple(c[k] + axis[k] * half for k in range(3))
    return p1, p2, L


def wipe():
    """Delete every entity whose position is on the copy side."""
    r = eb_api.list_model()
    say("🧹 מוחק את השכפול הפגום (כל מה שמעבר ל-X=%.0f)..." % SPLIT)
    sh, pl, bo = load()
    handles = ([e["h"] for e in copyside(sh)] +
               [e["h"] for e in copyside(pl, "c")] +
               [e["h"] for e in copyside(bo, "c")])
    ok = 0
    for h in handles:
        try:
            res = eb_api.delete(h)
            if isinstance(res, str) and res.startswith("EB_OK"):
                ok += 1
        except Exception:
            pass
    say("🧹 נמחקו %d/%d אובייקטים." % (ok, len(handles)))
    return ok


def build_shapes(items):
    ok = fail = 0
    fails = []
    n = len(items)
    for i, e in enumerate(items, 1):
        r = eb_api.run("beam", name=e["profile"],
                       catalog=(e["catalog"].split(".")[-1] if e["catalog"] else ""),
                       p1=eb_api._pt(off(e["p1"])), p2=eb_api._pt(off(e["p2"])),
                       rot=e["rot"], offx=e["offx"], offy=e["offy"],
                       mirror=("1" if e["mirror"] else "0"), _log=False)
        good = isinstance(r, str) and r.startswith("EB_OK")
        ok += good
        if not good:
            fail += 1
            fails.append((e["profile"], str(r)[:70]))
        if i % 25 == 0 or i == n:
            say("🔧 פרופילים: %d/%d (✅%d ⚠️%d)" % (i, n, ok, fail))
        time.sleep(PACE)
    return ok, fail, fails


def build_plates(items):
    ok = fail = 0
    fails = []
    n = len(items)
    for i, p in enumerate(items, 1):
        L, W, t, nrm = plate_params(p)
        if min(L, W, t) <= 0.5:
            fail += 1
            fails.append(("plate degenerate", "%gx%gx%g" % (L, W, t)))
            continue
        r = eb_api.plate(off(p["c"]), L, W, t, normal=nrm)
        good = isinstance(r, str) and r.startswith("EB_OK")
        ok += good
        if not good:
            fail += 1
            fails.append(("plate %gx%gx%g" % (L, W, t), str(r)[:70]))
        if i % 25 == 0 or i == n:
            say("🔩 פלטות: %d/%d (✅%d ⚠️%d)" % (i, n, ok, fail))
        time.sleep(PACE)
    return ok, fail, fails


def build_bolts(items, dia):
    ok = fail = 0
    fails = []
    n = len(items)
    for i, b in enumerate(items, 1):
        p1, p2, L = bolt_params(b)
        if L <= 1.0:
            fail += 1
            continue
        r = eb_api.bolt(off(p1), off(p2), dia=dia)
        good = isinstance(r, str) and r.startswith("EB_OK")
        ok += good
        if not good:
            fail += 1
            fails.append(("bolt L=%g" % L, str(r)[:70]))
        if i % 25 == 0 or i == n:
            say("🔗 ברגים: %d/%d (✅%d ⚠️%d)" % (i, n, ok, fail))
        time.sleep(PACE)
    return ok, fail, fails


def plan():
    sh, pl, bo = load()
    o_sh, o_pl, o_bo = orig(sh), orig(pl, "c"), orig(bo, "c")
    print("shapes: %d (offsets on %d, mirrored %d)" % (
        len(o_sh), sum(1 for e in o_sh if e["offx"] or e["offy"]),
        sum(1 for e in o_sh if e["mirror"])))
    print("plates: %d" % len(o_pl))
    print("bolts : %d" % len(o_bo))
    print("\nplate sizes:")
    from collections import Counter
    cp = Counter()
    for p in o_pl:
        L, W, t, _ = plate_params(p)
        cp[(round(L), round(W), round(t))] += 1
    for k, v in cp.most_common(12):
        print("  %sx%sx%s -> %d" % k[:3] + (v,) if False else "  %d x %d x %d -> %d" % (k[0], k[1], k[2], v))
    print("\nbolt lengths:")
    cb = Counter()
    for b in o_bo:
        _, _, L = bolt_params(b)
        cb[round(L)] += 1
    for k, v in cb.most_common(10):
        print("  L=%d -> %d" % (k, v))


def run(what="all", dia=16):
    t0 = time.time()
    sh, pl, bo = load()
    o_sh, o_pl, o_bo = orig(sh), orig(pl, "c"), orig(bo, "c")
    tot = {"shapes": (0, 0), "plates": (0, 0), "bolts": (0, 0)}
    allf = []

    say("🚀 **בונה את המודל המלא** — %d פרופילים · %d פלטות · %d ברגים, בהיסט +%.0f מ\"מ.\n"
        "הפעם עם **insert-offsets** (%d אלמנטים) ו-**שיקוף** (%d) — שתי הטעויות שמצאתי בביקורת העצמית."
        % (len(o_sh), len(o_pl), len(o_bo), DX,
           sum(1 for e in o_sh if e["offx"] or e["offy"]),
           sum(1 for e in o_sh if e["mirror"])))

    if what in ("all", "shapes"):
        ok, f, fl = build_shapes(o_sh)
        tot["shapes"] = (ok, f)
        allf += fl
    if what in ("all", "plates"):
        ok, f, fl = build_plates(o_pl)
        tot["plates"] = (ok, f)
        allf += fl
    if what in ("all", "bolts"):
        ok, f, fl = build_bolts(o_bo, dia)
        tot["bolts"] = (ok, f)
        allf += fl

    mins = (time.time() - t0) / 60.0
    lines = ["", "📊 **דוח בנייה מלאה**", ""]
    lines.append("| רכיב | נבנו | נכשלו |")
    lines.append("|---|---|---|")
    for k, he in (("shapes", "פרופילים"), ("plates", "פלטות"), ("bolts", "ברגים")):
        lines.append("| %s | **%d** | %d |" % (he, tot[k][0], tot[k][1]))
    tot_ok = sum(v[0] for v in tot.values())
    tot_f = sum(v[1] for v in tot.values())
    lines.append("")
    lines.append("**סה\"כ %d אובייקטים ב-%.1f דק'** (%d כשלונות)." % (tot_ok, mins, tot_f))
    if allf:
        lines.append("")
        lines.append("כשלונות (עד 8):")
        for d, why in allf[:8]:
            lines.append("- %s → %s" % (d, why))
    say("\n".join(lines))


if __name__ == "__main__":
    a = sys.argv[1:]
    mode = a[0] if a else "plan"
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    if mode == "wipe":
        wipe()
    elif mode == "plan":
        plan()
    elif mode == "run":
        run(a[1] if len(a) > 1 else "all", int(a[2]) if len(a) > 2 else 16)
    else:
        print(__doc__)
