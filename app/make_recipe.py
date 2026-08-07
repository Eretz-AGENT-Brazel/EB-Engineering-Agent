# -*- coding: utf-8 -*-
"""
make_recipe.py — turn a read model into a SEMANTIC BUILD RECIPE.

This is the agent's memory of a structure: not a list of objects to clone, but
assemblies, families and connections that can be rebuilt from scratch with
creation commands. Output: knowledge/recipes/<name>.json + a readable .md
"""
import json
import math
import os
import sys
from collections import Counter, defaultdict

APP = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(APP)
MAXX = 15000.0


def xyz(s):
    try:
        return tuple(float(x) for x in s.split(","))
    except Exception:
        return None


def load(path):
    sh, pl, bo = [], [], []
    for line in open(path, encoding="utf-8").read().splitlines():
        f = line.split("\t")
        if not f or not f[0]:
            continue
        if f[0] == "SHAPE" and len(f) >= 17:
            p1, p2 = xyz(f[4]), xyz(f[5])
            if not p1 or p1[0] >= MAXX:
                continue
            off = (f[10] or "0,0").split(",")
            sh.append({"prof": f[2], "cat": f[3], "p1": p1, "p2": p2,
                       "L": float(f[6] or 0), "rot": float(f[9] or 0),
                       "offx": float(off[0] or 0),
                       "offy": float(off[1] or 0) if len(off) > 1 else 0.0,
                       "mir": f[12] == "1", "layer": f[15] if len(f) > 15 else ""})
        elif f[0] == "PLATE" and len(f) >= 7:
            c = xyz(f[2])
            if not c or c[0] >= MAXX:
                continue
            pl.append({"c": c, "d": xyz(f[3]), "layer": f[5]})
        elif f[0] == "BOLT" and len(f) >= 7:
            c = xyz(f[2])
            if not c or c[0] >= MAXX:
                continue
            bo.append({"c": c, "d": xyz(f[3]), "layer": f[5]})
    return sh, pl, bo


def role(e):
    dz = abs(e["p2"][2] - e["p1"][2])
    L = e["L"] or 1.0
    v = dz / L
    if v > 0.95:
        return "vertical"
    if v < 0.10:
        return "horizontal"
    return "diagonal"


def axis_of(e):
    d = [abs(e["p2"][i] - e["p1"][i]) for i in range(3)]
    return "XYZ"[d.index(max(d))]


# ---- assembly classification, learned from the decoded structure ----
def assembly(e):
    p = e["prof"]
    r = role(e)
    if p in ("RHS150X100X4", "RHS200X100X4", "SHS100X100X3"):
        return "01_primary_frame"
    if p == "U140":
        return "02_secondary_channels"
    if p in ("EA60X60X6", "EA80X80X8"):
        return "03_angle_frames"          # incl. ladder stringers
    if p == "BSC21.3X2.5":
        return "04_ladder_rungs"          # 500mm @295 pitch
    if p in ("40X2.5SHS", "50X3.0SHS"):
        return "05_guardrail_posts"
    if p in ("26,9X2,6", "42,4X3,2"):
        return "06_handrails"
    if p in ("300X12", "300X10", "180X10", "BG60X6"):
        return "07_flats_stiffeners"
    return "08_other"


def families(items):
    """group identical members (profile+length+role) and find their pitch"""
    g = defaultdict(list)
    for e in items:
        g[(e["prof"], round(e["L"]), role(e))].append(e)
    out = []
    for k, arr in sorted(g.items(), key=lambda x: -len(x[1])):
        rec = {"profile": k[0], "length": k[1], "role": k[2], "count": len(arr)}
        if len(arr) >= 3:
            mids = [tuple((e["p1"][i] + e["p2"][i]) / 2.0 for i in range(3)) for e in arr]
            best = None
            for ax, nm in ((0, "X"), (1, "Y"), (2, "Z")):
                vals = sorted(m[ax] for m in mids)
                sp = [round(b - a, 1) for a, b in zip(vals, vals[1:]) if b - a > 1]
                if not sp:
                    continue
                spread = max(sp) - min(sp)
                if best is None or spread < best[2]:
                    best = (nm, sp, spread)
            if best and best[2] < 5.0:
                rec["array_axis"] = best[0]
                rec["pitch"] = best[1][0] if best[1] else None
                rec["regular"] = True
        out.append(rec)
    return out


def plate_family(pl):
    g = Counter()
    for p in pl:
        d = tuple(round(v) for v in p["d"])
        g[(d, p["layer"])] += 1
    return [{"dims": list(k[0]), "layer": k[1], "count": v}
            for k, v in g.most_common()]


def bolt_family(bo):
    g = Counter()
    for b in bo:
        g[round(max(b["d"]))] += 1
    return [{"length": k, "count": v} for k, v in g.most_common()]


def main(dump, name):
    sh, pl, bo = load(dump)
    asm = defaultdict(list)
    for e in sh:
        asm[assembly(e)].append(e)

    recipe = {"name": name, "source": os.path.basename(dump),
              "counts": {"shapes": len(sh), "plates": len(pl), "bolts": len(bo)},
              "assemblies": {}, "plates": plate_family(pl), "bolts": bolt_family(bo),
              "members": sh, "plate_list": pl, "bolt_list": bo}
    for k in sorted(asm):
        recipe["assemblies"][k] = {"count": len(asm[k]),
                                   "profiles": dict(Counter(e["prof"] for e in asm[k])),
                                   "families": families(asm[k])}

    outdir = os.path.join(ROOT, "knowledge", "recipes")
    os.makedirs(outdir, exist_ok=True)
    jp = os.path.join(outdir, name + ".json")
    json.dump(recipe, open(jp, "w", encoding="utf-8"), ensure_ascii=False, indent=1)

    W = ["# מתכון בנייה — %s" % name, "",
         "*%d פרופילים · %d פלטות · %d ברגים. מסודר למכלולים לבנייה מאפס (לא שכפול).*"
         % (len(sh), len(pl), len(bo)), ""]
    for k in sorted(recipe["assemblies"]):
        a = recipe["assemblies"][k]
        W.append("## %s — %d אלמנטים" % (k, a["count"]))
        W.append("פרופילים: " + ", ".join("%s×%d" % (p, n) for p, n in a["profiles"].items()))
        reg = [f for f in a["families"] if f.get("regular")]
        if reg:
            W.append("")
            W.append("**משפחות סדורות (מועמדות ל-array/copy בתוך המודל החדש):**")
            for f in reg:
                W.append("- %s @ %d מ\"מ ×%d — לאורך %s בפסיעה %s מ\"מ"
                         % (f["profile"], f["length"], f["count"], f["array_axis"], f["pitch"]))
        big = [f for f in a["families"] if f["count"] >= 3 and not f.get("regular")]
        if big:
            W.append("")
            W.append("חוזרות (לא סדורות): " +
                     ", ".join("%s@%d×%d" % (f["profile"], f["length"], f["count"]) for f in big))
        W.append("")
    W.append("## פלטות לפי משפחה")
    for p in recipe["plates"][:16]:
        W.append("- %d×%d×%d על שכבת `%s` — ×%d"
                 % (p["dims"][0], p["dims"][1], p["dims"][2], p["layer"], p["count"]))
    W.append("")
    W.append("## ברגים לפי אורך")
    W.append(", ".join("L=%d×%d" % (b["length"], b["count"]) for b in recipe["bolts"]))
    W.append("")
    W.append("> קוטרים לפי אמיר: **M16 (חור ⌀19)** ו-**M20 (חור ⌀23)**; חלק מהחורים **אובליים** להקלת הרכבה בשטח. מדרך = פלטה **4 מ\"מ**.")
    mp = os.path.join(outdir, name + ".md")
    open(mp, "w", encoding="utf-8").write("\n".join(W))
    print("recipe -> %s\n           %s" % (jp, mp))
    print("\n".join(W[:40]))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    d = sys.argv[1] if len(sys.argv) > 1 else os.path.join(APP, "plugin", "eb_full.txt")
    n = sys.argv[2] if len(sys.argv) > 2 else "lesson2-access-platform"
    main(d, n)
