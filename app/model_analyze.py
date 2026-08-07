# -*- coding: utf-8 -*-
"""
model_analyze.py — deep geometric analysis of a REAL ProSteel model.

Purpose: learn how Amir actually models — not from theory but from his own
geometry. Extracts: structural roles (columns/beams/braces), the grid he works
on, his coordinate precision, element families, and the connection nodes.

Usage:  python model_analyze.py [path_to_eb_model.txt]
"""
import os
import sys
import math
from collections import Counter, defaultdict

APP = os.path.dirname(os.path.abspath(__file__))
DEFAULT = os.path.join(APP, "plugin", "eb_model.txt")


MAXX = float(os.environ.get("EB_ANALYZE_MAXX", "15000"))


def load(path):
    """Load SHAPE rows. ARTEFACT FILTER: anything at X >= MAXX is a copy or a
    smoke test, never part of the source structure. (A leftover test beam at
    X=15455 once inflated the reported bounding box from 3.0m to 15.3m.)"""
    els = []
    skipped = 0
    for line in open(path, encoding="utf-8").read().splitlines():
        f = line.split("\t")
        if f and f[0] == "SHAPE" and len(f) >= 10:
            try:
                if float(f[4].split(",")[0]) >= MAXX:
                    skipped += 1
                    continue
            except Exception:
                pass
            def xyz(s):
                try:
                    return tuple(float(x) for x in s.split(","))
                except Exception:
                    return (0.0, 0.0, 0.0)
            els.append({"h": f[1], "profile": f[2], "catalog": f[3],
                        "p1": xyz(f[4]), "p2": xyz(f[5]),
                        "length": float(f[6] or 0), "name": f[8]})
    return els


# ---------- structural role from orientation ----------
def role(e):
    p1, p2 = e["p1"], e["p2"]
    dx, dy, dz = p2[0] - p1[0], p2[1] - p1[1], p2[2] - p1[2]
    L = math.sqrt(dx * dx + dy * dy + dz * dz) or 1.0
    vert = abs(dz) / L                       # 1 = fully vertical
    if vert > 0.95:
        return "עמוד (אנכי)"
    if vert < 0.10:
        return "קורה (אופקי)"
    return "אלכסון/מסבך"


def axis_of(e):
    p1, p2 = e["p1"], e["p2"]
    d = [abs(p2[i] - p1[i]) for i in range(3)]
    return "XYZ"[d.index(max(d))]


# ---------- precision: how round are his coordinates ----------
def precision_profile(els):
    vals = []
    for e in els:
        vals.extend(list(e["p1"]) + list(e["p2"]))
    buckets = Counter()
    for v in vals:
        r = round(v, 3)
        if abs(r - round(r)) < 1e-6:
            if round(r) % 1000 == 0:
                buckets["עגול ל-1000 (מטר שלם)"] += 1
            elif round(r) % 100 == 0:
                buckets["עגול ל-100"] += 1
            elif round(r) % 50 == 0:
                buckets["עגול ל-50"] += 1
            elif round(r) % 10 == 0:
                buckets["עגול ל-10"] += 1
            else:
                buckets["מ\"מ שלם"] += 1
        else:
            buckets["שבר עשרוני"] += 1
    return buckets, len(vals)


# ---------- grid detection: recurring coordinate values ----------
def grid(els, axis_idx, tol=1.0):
    vals = []
    for e in els:
        vals.append(e["p1"][axis_idx])
        vals.append(e["p2"][axis_idx])
    vals.sort()
    clusters = []
    for v in vals:
        if clusters and abs(v - clusters[-1][-1]) <= tol:
            clusters[-1].append(v)
        else:
            clusters.append([v])
    lines = [(sum(c) / len(c), len(c)) for c in clusters if len(c) >= 4]
    lines.sort(key=lambda x: -x[1])
    return lines


def spacings(lines, top=12):
    xs = sorted(v for v, _ in lines[:top])
    return [round(b - a, 1) for a, b in zip(xs, xs[1:])]


# ---------- connection nodes: endpoints that meet ----------
def nodes(els, tol=25.0):
    pts = []
    for e in els:
        pts.append((e["p1"], e["h"], e["profile"]))
        pts.append((e["p2"], e["h"], e["profile"]))
    used = [False] * len(pts)
    out = []
    for i in range(len(pts)):
        if used[i]:
            continue
        grp = [i]
        used[i] = True
        for j in range(i + 1, len(pts)):
            if used[j]:
                continue
            a, b = pts[i][0], pts[j][0]
            if (abs(a[0] - b[0]) <= tol and abs(a[1] - b[1]) <= tol
                    and abs(a[2] - b[2]) <= tol):
                grp.append(j)
                used[j] = True
        if len(grp) >= 2:
            members = sorted(set(pts[k][2] for k in grp))
            out.append({"n": len(grp), "at": pts[i][0], "profiles": members})
    return out


def _dist_pt_seg(p, a, b):
    ax, ay, az = a
    bx, by, bz = b
    px, py, pz = p
    dx, dy, dz = bx - ax, by - ay, bz - az
    L2 = dx * dx + dy * dy + dz * dz
    if L2 == 0:
        return math.dist(p, a), 0.0
    t = ((px - ax) * dx + (py - ay) * dy + (pz - az) * dz) / L2
    t = max(0.0, min(1.0, t))
    q = (ax + t * dx, ay + t * dy, az + t * dz)
    return math.dist(p, q), t


def tee_junctions(els, tol=60.0):
    """Endpoint of one element landing on the BODY of another = T-junction.
    This is the dominant connection in platforms/handrails (post welded to a rail)."""
    out = []
    for i, e in enumerate(els):
        for endname, p in (("start", e["p1"]), ("end", e["p2"])):
            for j, f in enumerate(els):
                if i == j:
                    continue
                d, t = _dist_pt_seg(p, f["p1"], f["p2"])
                if d <= tol and 0.02 < t < 0.98:      # lands mid-span, not at a corner
                    out.append({"member": e["profile"], "onto": f["profile"],
                                "at": p, "t": t, "gap": d})
                    break
    return out


def family_spacing(els, profile, length, tol=2.0):
    """For a repeated family, find the pitch between consecutive members —
    reveals design rules (e.g. handrail infill spacing)."""
    grp = [e for e in els if e["profile"] == profile and abs(e["length"] - length) <= tol]
    if len(grp) < 3:
        return None, []
    mids = [tuple((e["p1"][k] + e["p2"][k]) / 2.0 for k in range(3)) for e in grp]
    best = None
    for k, nm in ((0, "X"), (1, "Y"), (2, "Z")):
        vals = sorted(m[k] for m in mids)
        sp = [round(b - a, 1) for a, b in zip(vals, vals[1:]) if (b - a) > 1.0]
        if not sp:
            continue
        var = max(sp) - min(sp)
        if best is None or var < best[2]:
            best = (nm, sp, var)
    if not best:
        return None, []
    return best[0], best[1]


def main():
    path = sys.argv[1] if len(sys.argv) > 1 else DEFAULT
    els = load(path)
    W = []
    W.append("# ניתוח גיאומטרי של המודל — איך אמיר ממדל בפועל")
    W.append("")
    W.append("*נגזר מ-%d פרופילים שנקראו מהמודל (`%s`).*" % (len(els), os.path.basename(path)))
    W.append("")

    # --- roles ---
    roles = Counter(role(e) for e in els)
    W.append("## 1. תפקידים מבניים (לפי כיוון האלמנט)")
    W.append("")
    for r, c in roles.most_common():
        W.append("- **%s** — %d אלמנטים (%.0f%%)" % (r, c, 100.0 * c / len(els)))
    W.append("")

    # --- role x profile: which profile does he use for what ---
    rp = defaultdict(Counter)
    for e in els:
        rp[role(e)][e["profile"]] += 1
    W.append("## 2. איזה פרופיל לאיזה תפקיד — ה\"אוצר מילים\" של אמיר")
    W.append("")
    for r, ctr in rp.items():
        top = ", ".join("%s×%d" % (p, n) for p, n in ctr.most_common(6))
        W.append("- **%s:** %s" % (r, top))
    W.append("")

    # --- axes of horizontal members ---
    horiz = [e for e in els if role(e).startswith("קורה")]
    ax = Counter(axis_of(e) for e in horiz)
    W.append("## 3. צירי עבודה")
    W.append("")
    W.append("קורות לפי ציר עיקרי: " + ", ".join("%s×%d" % (a, n) for a, n in ax.most_common()))
    W.append("")
    for i, nm in ((0, "X"), (1, "Y"), (2, "Z (מפלסים)")):
        lines = grid(els, i)
        if lines:
            sp = spacings(lines)
            W.append("**ציר %s** — %d קווי-רשת חוזרים. ערכים מובילים: %s" % (
                nm, len(lines), ", ".join("%.0f(×%d)" % (v, c) for v, c in lines[:8])))
            if sp:
                W.append("  מרווחים בין קווי-הרשת: %s מ\"מ" % ", ".join(str(s) for s in sp[:10]))
    W.append("")

    # --- precision ---
    buckets, total = precision_profile(els)
    W.append("## 4. דיוק — עד כמה הקואורדינטות \"עגולות\"")
    W.append("")
    for k, v in buckets.most_common():
        W.append("- %s: %.0f%% (%d מתוך %d ערכים)" % (k, 100.0 * v / total, v, total))
    W.append("")

    # --- lengths ---
    lens = sorted(e["length"] for e in els if e["length"])
    lc = Counter(round(l) for l in lens)
    W.append("## 5. אורכי אלמנטים")
    W.append("")
    W.append("טווח: %.0f–%.0f מ\"מ · חציון: %.0f מ\"מ" % (lens[0], lens[-1], lens[len(lens) // 2]))
    W.append("אורכים חוזרים (מעידים על ייצור סדרתי): %s" %
             ", ".join("%d מ\"מ ×%d" % (l, n) for l, n in lc.most_common(8) if n > 1))
    W.append("")

    # --- families: same profile + same length + same role ---
    fam = Counter((e["profile"], round(e["length"]), role(e)) for e in els)
    reps = [(k, v) for k, v in fam.most_common() if v >= 3]
    W.append("## 6. \"משפחות\" חוזרות — מועמדים לאוטומציה")
    W.append("")
    if reps:
        for (prof, L, r), n in reps[:12]:
            W.append("- **%s** באורך %d מ\"מ בתפקיד %s — **%d פעמים**" % (prof, L, r, n))
        W.append("")
        W.append("_כל שורה כזאת = פקודה אחת שיכולה להחליף %d הכנסות ידניות._" % reps[0][1])
    W.append("")

    # --- nodes ---
    nd = nodes(els)
    deg = Counter(x["n"] for x in nd)
    W.append("## 7. צמתים (נקודות חיבור)")
    W.append("")
    W.append("זוהו **%d צמתים** שבהם נפגשים קצות אלמנטים (סבולת 25 מ\"מ)." % len(nd))
    W.append("התפלגות מספר האלמנטים בצומת: " +
             ", ".join("%d אלמנטים×%d צמתים" % (k, v) for k, v in sorted(deg.items())))
    big = sorted(nd, key=lambda x: -x["n"])[:6]
    if big:
        W.append("")
        W.append("הצמתים העמוסים ביותר (שם נמצאות הפלטות והברגים):")
        for b in big:
            W.append("- %d אלמנטים ב-(%.0f, %.0f, %.0f): %s" %
                     (b["n"], b["at"][0], b["at"][1], b["at"][2], ", ".join(b["profiles"][:4])))
    W.append("")

    # --- T-junctions: the real connection type here ---
    tj = tee_junctions(els)
    W.append("## 8. חיבורי T (קצה אלמנט על גוף אלמנט אחר) — סוג החיבור השולט")
    W.append("")
    W.append("זוהו **%d חיבורי T** — קצה של אלמנט נוחת על *אמצע* אלמנט אחר." % len(tj))
    pair = Counter((x["member"], x["onto"]) for x in tj)
    for (m, o), n in pair.most_common(8):
        W.append("- **%s** → מתחבר לגוף **%s** — %d פעמים" % (m, o, n))
    gaps = [x["gap"] for x in tj]
    if gaps:
        W.append("")
        W.append("מרווח הצמדה בצמתים: %.1f–%.1f מ\"מ (חציון %.1f) — "
                 "מעיד על הצמדה לפני החתך, לא לציר." %
                 (min(gaps), max(gaps), sorted(gaps)[len(gaps) // 2]))
    W.append("")

    # --- extents ---
    xs = [c for e in els for c in (e["p1"][0], e["p2"][0])]
    ys = [c for e in els for c in (e["p1"][1], e["p2"][1])]
    zs = [c for e in els for c in (e["p1"][2], e["p2"][2])]
    W.append("## 9. מידות המבנה")
    W.append("")
    W.append("תיבה חוסמת: **%.0f × %.0f × %.0f מ\"מ** (X × Y × Z) — כלומר %.1f × %.1f × %.1f מ'." %
             (max(xs) - min(xs), max(ys) - min(ys), max(zs) - min(zs),
              (max(xs) - min(xs)) / 1000, (max(ys) - min(ys)) / 1000, (max(zs) - min(zs)) / 1000))
    W.append("טווח מפלסים (Z): %.0f עד %.0f מ\"מ." % (min(zs), max(zs)))
    W.append("")

    # --- design rules from family pitch ---
    W.append("## 10. כללי תכן שנחשפו מהמרווחים (התגלית החשובה)")
    W.append("")
    for (prof, L, r), n in reps[:6]:
        axis, sp = family_spacing(els, prof, L)
        if sp:
            uniq = Counter(sp)
            common = ", ".join("%.0f מ\"מ ×%d" % (v, c) for v, c in uniq.most_common(4))
            W.append("- **%s @ %d מ\"מ** (×%d): מסודרים לאורך ציר **%s**, מרווחים: %s"
                     % (prof, L, n, axis, common))
    W.append("")
    return "\n".join(W)


if __name__ == "__main__":
    txt = main()
    out = os.path.join(os.path.dirname(APP), "knowledge", "MODEL_GEOMETRY_ANALYSIS.md")
    open(out, "w", encoding="utf-8").write(txt)
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    print(txt)
    print("\n[saved] " + out)
