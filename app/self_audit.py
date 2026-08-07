# -*- coding: utf-8 -*-
"""
self_audit.py — compare the ORIGINAL model against the copy the agent built.

Split by X: original < SPLIT, agent's copy >= SPLIT. Reports every difference,
so the agent finds its OWN mistakes instead of being told about them.
"""
import os
import sys
from collections import Counter, defaultdict

APP = os.path.dirname(os.path.abspath(__file__))
FULL = os.path.join(APP, "plugin", "eb_full.txt")
SPLIT = 15000.0
DX = 15000.0


def xyz(s):
    try:
        return tuple(float(x) for x in s.split(","))
    except Exception:
        return (0.0, 0.0, 0.0)


def load():
    sh, pl, bo = [], [], []
    for line in open(FULL, encoding="utf-8").read().splitlines():
        f = line.split("\t")
        if not f:
            continue
        if f[0] == "SHAPE" and len(f) >= 14:
            sh.append({"h": f[1], "profile": f[2], "catalog": f[3],
                       "p1": xyz(f[4]), "p2": xyz(f[5]), "length": float(f[6] or 0),
                       "material": f[7], "name": f[8], "rot": float(f[9] or 0),
                       "off": f[10], "ladd": float(f[11] or 0), "mirror": f[12]})
        elif f[0] == "PLATE" and len(f) >= 4:
            pl.append({"h": f[1], "c": xyz(f[2]), "d": xyz(f[3])})
        elif f[0] == "BOLT" and len(f) >= 4:
            bo.append({"h": f[1], "c": xyz(f[2]), "d": xyz(f[3])})
    return sh, pl, bo


def side(p):
    return "copy" if p[0] >= SPLIT else "orig"


def main():
    sh, pl, bo = load()
    o_sh = [e for e in sh if side(e["p1"]) == "orig"]
    c_sh = [e for e in sh if side(e["p1"]) == "copy"]
    o_pl = [e for e in pl if side(e["c"]) == "orig"]
    c_pl = [e for e in pl if side(e["c"]) == "copy"]
    o_bo = [e for e in bo if side(e["c"]) == "orig"]
    c_bo = [e for e in bo if side(e["c"]) == "copy"]

    W = ["# 🔍 ביקורת עצמית — המקור מול השכפול שבניתי", ""]
    W.append("| | מקור | שכפול שלי | פער |")
    W.append("|---|---|---|---|")
    W.append("| פרופילים | %d | %d | %+d |" % (len(o_sh), len(c_sh), len(c_sh) - len(o_sh)))
    W.append("| פלטות | %d | %d | **%+d** |" % (len(o_pl), len(c_pl), len(c_pl) - len(o_pl)))
    W.append("| ברגים | %d | %d | **%+d** |" % (len(o_bo), len(c_bo), len(c_bo) - len(o_bo)))
    W.append("")

    errs = []

    # --- 1. profile counts ---
    oc = Counter(e["profile"] for e in o_sh)
    cc = Counter(e["profile"] for e in c_sh)
    diffs = {p: cc.get(p, 0) - oc.get(p, 0) for p in set(oc) | set(cc)}
    bad = {p: d for p, d in diffs.items() if d != 0}
    W.append("## 1. ספירת פרופילים לפי סוג")
    W.append("")
    if bad:
        for p, d in sorted(bad.items(), key=lambda x: -abs(x[1])):
            W.append("- ⚠️ **%s**: מקור %d · שכפול %d (**%+d**)" % (p, oc.get(p, 0), cc.get(p, 0), d))
            errs.append("ספירה שגויה: %s (%+d)" % (p, d))
    else:
        W.append("✅ כל סוגי הפרופילים תואמים בכמות.")
    W.append("")

    # --- 2. rotation ---
    o_rot = [e["rot"] for e in o_sh]
    c_rot = [e["rot"] for e in c_sh]
    o_nz = sum(1 for r in o_rot if abs(r) > 0.5)
    c_nz = sum(1 for r in c_rot if abs(r) > 0.5)
    W.append("## 2. סיבוב הפרופיל (rotation) — הבדיקה הקריטית")
    W.append("")
    W.append("- במקור: **%d מתוך %d** אלמנטים בסיבוב ≠ 0 (%.0f%%)" %
             (o_nz, len(o_sh), 100.0 * o_nz / max(1, len(o_sh))))
    W.append("- בשכפול שלי: **%d מתוך %d** בסיבוב ≠ 0 (%.0f%%)" %
             (c_nz, len(c_sh), 100.0 * c_nz / max(1, len(c_sh))))
    rot_hist = Counter(round(r / 15.0) * 15 for r in o_rot)
    W.append("- התפלגות הסיבובים במקור: %s" %
             ", ".join("%g°×%d" % (k, v) for k, v in sorted(rot_hist.items())))
    if o_nz and not c_nz:
        W.append("")
        W.append("### ❌ טעות שלי #1 — לא העברתי את הסיבוב")
        W.append("קראתי פרופיל, קטלוג ונקודות — אבל **לא את הסיבוב**. "
                 "כל האלמנטים שלי נוצרו ב-rot=0. בפרופיל א-סימטרי "
                 "(RHS150X100 — 150 מול 100; זוויתן — לאיזה כיוון השוקיים) זו **טעות גיאומטרית אמיתית**.")
        errs.append("סיבוב לא הועבר (rot=0 בכל %d האלמנטים)" % len(c_sh))
    W.append("")

    # --- 3. geometry match per element (nearest-by-offset) ---
    W.append("## 3. התאמה גיאומטרית אלמנט-אלמנט")
    W.append("")
    # GLOBAL 1-1 assignment. (A per-element nearest-match wrongly pairs
    # near-identical neighbours — that bug once reported 44 false deviations.)
    cand = []
    for i, e in enumerate(o_sh):
        w1 = (e["p1"][0] + DX, e["p1"][1], e["p1"][2])
        w2 = (e["p2"][0] + DX, e["p2"][1], e["p2"][2])
        for j, c in enumerate(c_sh):
            if c["profile"] != e["profile"]:
                continue
            d1 = max(max(abs(c["p1"][k] - w1[k]) for k in range(3)),
                     max(abs(c["p2"][k] - w2[k]) for k in range(3)))
            d2 = max(max(abs(c["p2"][k] - w1[k]) for k in range(3)),
                     max(abs(c["p1"][k] - w2[k]) for k in range(3)))
            cand.append((min(d1, d2), i, j))
    cand.sort()
    uo, uc = set(), set()
    matched, offby = 0, []
    for d, i, j in cand:
        if i in uo or j in uc:
            continue
        uo.add(i)
        uc.add(j)
        if d <= 1.0:
            matched += 1
        else:
            offby.append((o_sh[i], d))
    unmatched = [o_sh[i] for i in range(len(o_sh)) if i not in uo]
    stray = [c_sh[j] for j in range(len(c_sh)) if j not in uc]
    if stray:
        W.append("- ⚠️ **%d אלמנטים בשכפול ללא מקבילה במקור** (שאריות בדיקה?): %s" %
                 (len(stray), ", ".join(s["profile"] for s in stray[:5])))
        errs.append("%d אלמנטים עודפים" % len(stray))
    W.append("- ✅ תואמים במדויק (≤1 מ\"מ): **%d / %d**" % (matched, len(o_sh)))
    if offby:
        W.append("- ⚠️ נמצאו אך במיקום שונה: %d (סטייה מקסימלית %.1f מ\"מ)" %
                 (len(offby), max(d for _, d in offby)))
        errs.append("%d אלמנטים במיקום לא מדויק" % len(offby))
    if unmatched:
        W.append("- ❌ **חסרים לגמרי בשכפול: %d**" % len(unmatched))
        for e in unmatched[:6]:
            W.append("  - %s באורך %.0f" % (e["profile"], e["length"]))
        errs.append("%d אלמנטים חסרים" % len(unmatched))
    W.append("")

    # --- 4. duplicates in my copy ---
    key = lambda e: (e["profile"], tuple(round(v, 1) for v in e["p1"]),
                     tuple(round(v, 1) for v in e["p2"]))
    dup = [k for k, n in Counter(key(e) for e in c_sh).items() if n > 1]
    W.append("## 4. כפילויות בשכפול שלי")
    W.append("")
    if dup:
        W.append("### ❌ טעות שלי #2 — יצרתי אלמנטים כפולים")
        for k in dup[:6]:
            W.append("- **%s** ב-(%.0f,%.0f,%.0f) — נוצר יותר מפעם אחת" %
                     (k[0], k[1][0], k[1][1], k[1][2]))
        W.append("")
        W.append("_הסיבה: בדיקת העשן שהרצתי לפני הריצה יצרה אלמנט שגם הריצה יצרה שוב._")
        errs.append("%d כפילויות" % len(dup))
    else:
        W.append("✅ אין כפילויות.")
    W.append("")

    # --- 5. material ---
    o_mat = Counter(e["material"] for e in o_sh)
    c_mat = Counter(e["material"] for e in c_sh)
    W.append("## 5. חומר (material)")
    W.append("")
    W.append("- מקור: %s" % ", ".join("%s×%d" % (k or "(ריק)", v) for k, v in o_mat.most_common(4)))
    W.append("- שכפול: %s" % ", ".join("%s×%d" % (k or "(ריק)", v) for k, v in c_mat.most_common(4)))
    if set(o_mat) != set(c_mat):
        W.append("")
        W.append("### ⚠️ טעות שלי #3 — לא העברתי את החומר")
        errs.append("חומר לא הועבר")
    W.append("")

    # --- 6. length additions / offsets / mirror ---
    o_la = sum(1 for e in o_sh if abs(e["ladd"]) > 0.01)
    o_of = sum(1 for e in o_sh if e["off"] not in ("0,0", "", "0,0.0"))
    o_mi = sum(1 for e in o_sh if e["mirror"] == "1")
    W.append("## 6. הכנות קצה, אופסטים ושיקוף")
    W.append("")
    W.append("במקור: LengthAddition≠0 ב-**%d** אלמנטים · InsertOffset≠0 ב-**%d** · MirrorFlag ב-**%d**"
             % (o_la, o_of, o_mi))
    if o_la or o_of or o_mi:
        W.append("")
        W.append("### ⚠️ טעות שלי #4 — לא העברתי הכנות קצה/אופסטים/שיקוף")
        errs.append("LengthAddition/offsets/mirror לא הועברו")
    W.append("")

    # --- 7. the big one ---
    W.append("## 7. חיבורים — הלב של הקונסטרוקציה")
    W.append("")
    W.append("**המקור: %d פלטות + %d ברגים. השכפול שלי: %d פלטות + %d ברגים.**"
             % (len(o_pl), len(o_bo), len(c_pl), len(c_bo)))
    W.append("")
    if not c_pl and not c_bo:
        W.append("❌ בניתי שלד בלי חיבורים — **קונסטרוקציה שלא ניתן לייצר**.")
        errs.append("חיבורים חסרים לחלוטין: %d פלטות + %d ברגים" % (len(o_pl), len(o_bo)))
    elif len(c_pl) == len(o_pl) and len(c_bo) == len(o_bo):
        W.append("✅ כל החיבורים נבנו — הכמויות תואמות במדויק.")
    else:
        W.append("⚠️ פער בחיבורים: %+d פלטות, %+d ברגים."
                 % (len(c_pl) - len(o_pl), len(c_bo) - len(o_bo)))
        errs.append("פער חיבורים: %+d פלטות %+d ברגים"
                    % (len(c_pl) - len(o_pl), len(c_bo) - len(o_bo)))
    W.append("")

    # --- plate/bolt intelligence for the rebuild ---
    W.append("## 8. מה שאני יודע עכשיו על החיבורים (מהקריאה החדשה)")
    W.append("")
    pd = Counter()
    for p in o_pl:
        d = sorted(p["d"])
        pd[(round(d[0]), round(d[1]), round(d[2]))] += 1
    W.append("**מידות פלטות (תיבה חוסמת, ממוין):**")
    for k, n in pd.most_common(10):
        W.append("- %d × %d × %d מ\"מ — **%d פלטות**" % (k[2], k[1], k[0], n))
    bd = Counter()
    for b in o_bo:
        d = sorted(b["d"])
        bd[(round(d[0]), round(d[2]))] += 1
    W.append("")
    W.append("**מידות ברגים (קוטר~ , אורך~):**")
    for k, n in bd.most_common(8):
        W.append("- ⌀~%d × אורך~%d מ\"מ — **%d ברגים**" % (k[0], k[1], n))
    W.append("")

    W.append("---")
    W.append("## סיכום הטעויות שמצאתי בעצמי")
    W.append("")
    for i, e in enumerate(errs, 1):
        W.append("%d. %s" % (i, e))
    return "\n".join(W)


if __name__ == "__main__":
    txt = main()
    out = os.path.join(os.path.dirname(APP), "knowledge", "SELF_AUDIT_LESSON2.md")
    open(out, "w", encoding="utf-8").write(txt)
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    print(txt)
    print("\n[saved] " + out)
