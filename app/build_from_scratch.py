# -*- coding: utf-8 -*-
"""
build_from_scratch.py — build a whole structure from the RECIPE, in a new model.

No cloning, no copying from another drawing: every object is created with
ProSteel creation commands from remembered parameters, assembly by assembly,
and the CONNECTIONS are built as real connections (bolts linked to their host
objects so ProSteel drills actual holes).

Amir's confirmed connection facts (knowledge/AMIR-ANSWERS-CONNECTIONS.md):
  M16 -> hole 19mm | M20 -> hole 23mm | some holes oval (field fit) |
  grating "marug 4+1" -> model as a 4mm plate.

Usage:
  python build_from_scratch.py plan
  python build_from_scratch.py run [assembly_key|all]
"""
import json
import math
import os
import re
import sys
import time

APP = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(APP)
sys.path.insert(0, APP)
import eb_api                                    # noqa: E402

RECIPE = os.path.join(ROOT, "knowledge", "recipes", "lesson2-access-platform.json")
STATE = os.path.join(ROOT, "projects", "שיעור-3-מאפס", "files", "build_state.json")
ORDER = ["01_primary_frame", "02_secondary_channels", "03_angle_frames",
         "04_ladder_rungs", "05_guardrail_posts", "06_handrails",
         "07_flats_stiffeners", "08_other"]
PACE = 0.12

# --- engineering decisions (stated, not hidden) ---
# base plates and the heavy 300x250 family carry M20; everything else M16.
BOLT_M20_PLATE = (300, 250)
GRATING_LAYER = "marug 4+1"
GRATING_T = 4.0          # Amir: model the marug panels as a 4mm plate


def log(m):
    try:
        sys.stdout.write(m + "\n")
        sys.stdout.flush()
    except Exception:
        pass


def H(r):
    m = re.search(r"handle=(\w+)", r or "")
    return m.group(1) if m else None


def ok(r):
    return isinstance(r, str) and r.startswith("EB_OK")


def load_recipe():
    return json.load(open(RECIPE, encoding="utf-8"))


def assembly_of(prof):
    if prof in ("RHS150X100X4", "RHS200X100X4", "SHS100X100X3"):
        return "01_primary_frame"
    if prof == "U140":
        return "02_secondary_channels"
    if prof in ("EA60X60X6", "EA80X80X8"):
        return "03_angle_frames"
    if prof == "BSC21.3X2.5":
        return "04_ladder_rungs"
    if prof in ("40X2.5SHS", "50X3.0SHS"):
        return "05_guardrail_posts"
    if prof in ("26,9X2,6", "42,4X3,2"):
        return "06_handrails"
    if prof in ("300X12", "300X10", "180X10", "BG60X6"):
        return "07_flats_stiffeners"
    return "08_other"


def state_load():
    if os.path.exists(STATE):
        try:
            return json.load(open(STATE, encoding="utf-8"))
        except Exception:
            pass
    return {"members": {}, "plates": {}, "bolts": {}, "done": []}


def state_save(s):
    os.makedirs(os.path.dirname(STATE), exist_ok=True)
    json.dump(s, open(STATE, "w", encoding="utf-8"), ensure_ascii=False)


def mkey(e):
    return "%s|%.1f,%.1f,%.1f|%.1f,%.1f,%.1f" % ((e["prof"],) + tuple(e["p1"]) + tuple(e["p2"]))


def pkey(p):
    return "%.1f,%.1f,%.1f|%.0fx%.0fx%.0f" % (tuple(p["c"]) + tuple(p["d"]))


def bkey(b):
    return "%.1f,%.1f,%.1f|%.0f" % (tuple(b["c"]) + (max(b["d"]),))


# ---------- members ----------
def build_members(rec, st, only=None):
    made = fail = skip = 0
    groups = {}
    for e in rec["members"]:
        groups.setdefault(assembly_of(e["prof"]), []).append(e)
    for asm in ORDER:
        if asm not in groups:
            continue
        if only and only != "all" and only != asm:
            continue
        arr = groups[asm]
        log("\n=== %s : %d members ===" % (asm, len(arr)))
        for i, e in enumerate(arr, 1):
            k = mkey(e)
            if k in st["members"]:
                skip += 1
                continue
            r = None
            for _ in range(3):
                r = eb_api.run("beam", name=e["prof"],
                               catalog=(e["cat"].split(".")[-1] if e["cat"] else ""),
                               p1=eb_api._pt(e["p1"]), p2=eb_api._pt(e["p2"]),
                               rot=e["rot"], offx=e["offx"], offy=e["offy"],
                               layer=e.get("layer") or "PS_Shape",
                               wait=18, _log=False)
                if ok(r):
                    break
                time.sleep(1.2)
            if ok(r):
                st["members"][k] = H(r)
                made += 1
            else:
                fail += 1
                log("   FAIL %s: %s" % (e["prof"], str(r)[:70]))
            if i % 15 == 0 or i == len(arr):
                log("   %d/%d (made %d, fail %d)" % (i, len(arr), made, fail))
                state_save(st)
            time.sleep(PACE)
        st["done"].append(asm)
        state_save(st)
    return made, fail, skip


# ---------- plates ----------
def plate_axes(d):
    dd = list(d)
    ti = dd.index(min(dd))
    rest = [i for i in range(3) if i != ti]
    unit = lambda i: "%g,%g,%g" % tuple(1 if k == i else 0 for k in range(3))
    return dd[rest[0]], dd[rest[1]], dd[ti], unit(rest[0]), unit(rest[1]), unit(ti)


def build_plates(rec, st):
    made = fail = skip = 0
    arr = rec["plate_list"]
    log("\n=== plates : %d ===" % len(arr))
    for i, p in enumerate(arr, 1):
        k = pkey(p)
        if k in st["plates"]:
            skip += 1
            continue
        L, W, T, ex, ey, ez = plate_axes(p["d"])
        lay = p["layer"] or "PS_Plate"
        c = list(p["c"])
        if lay == GRATING_LAYER:            # Amir: grating = 4mm plate, at deck top
            c[2] = p["c"][2] + p["d"][2] / 2.0 - GRATING_T / 2.0
            dd = sorted(p["d"], reverse=True)
            L, W, T = dd[0], dd[1], GRATING_T
            ex, ey, ez = "1,0,0", "0,1,0", "0,0,1"
        r = None
        for _ in range(3):
            r = eb_api.run("plate", center=eb_api._pt(tuple(c)), l=L, w=W, t=T,
                           ex=ex, ey=ey, ez=ez, layer=lay, wait=18, _log=False)
            if ok(r):
                break
            time.sleep(1.2)
        if ok(r):
            st["plates"][k] = H(r)
            made += 1
        else:
            fail += 1
            log("   FAIL plate %gx%gx%g: %s" % (L, W, T, str(r)[:60]))
        if i % 25 == 0 or i == len(arr):
            log("   %d/%d (made %d, fail %d)" % (i, len(arr), made, fail))
            state_save(st)
        time.sleep(PACE)
    state_save(st)
    return made, fail, skip


# ---------- connections: bolts linked to their hosts ----------
def near_plate_handles(b, rec, st, tol=60.0):
    """Which created plates does this bolt pass through?"""
    hs = []
    bc = b["c"]
    for p in rec["plate_list"]:
        h = st["plates"].get(pkey(p))
        if not h:
            continue
        half = [p["d"][k] / 2.0 + tol for k in range(3)]
        if all(abs(bc[k] - p["c"][k]) <= half[k] for k in range(3)):
            hs.append(h)
            if len(hs) >= 2:
                break
    return hs


def _dist_pt_seg(p, a, b):
    d = [b[i] - a[i] for i in range(3)]
    L2 = sum(x * x for x in d)
    if L2 == 0:
        return math.dist(p, a)
    t = max(0.0, min(1.0, sum((p[i] - a[i]) * d[i] for i in range(3)) / L2))
    q = tuple(a[i] + t * d[i] for i in range(3))
    return math.dist(p, q)


def near_member_handles(b, rec, st, tol=120.0):
    hs = []
    for e in rec["members"]:
        h = st["members"].get(mkey(e))
        if not h:
            continue
        if _dist_pt_seg(b["c"], e["p1"], e["p2"]) <= tol:
            hs.append(h)
            if len(hs) >= 2:
                break
    return hs


def bolt_dia(b, rec, st):
    """M20 for the heavy base-plate family, M16 elsewhere (Amir: M16/M20 only)."""
    bc = b["c"]
    for p in rec["plate_list"]:
        d = sorted(p["d"], reverse=True)
        if (round(d[0]), round(d[1])) == BOLT_M20_PLATE:
            half = [p["d"][k] / 2.0 + 60 for k in range(3)]
            if all(abs(bc[k] - p["c"][k]) <= half[k] for k in range(3)):
                return 20
    return 16


def build_bolts(rec, st):
    made = fail = skip = 0
    nohost = 0
    arr = rec["bolt_list"]
    log("\n=== bolts (real connections, hosts drilled) : %d ===" % len(arr))
    for i, b in enumerate(arr, 1):
        k = bkey(b)
        if k in st["bolts"]:
            skip += 1
            continue
        dd = list(b["d"])
        ai = dd.index(max(dd))
        Ln = dd[ai]
        axis = [0.0, 0.0, 0.0]
        axis[ai] = 1.0
        half = Ln / 2.0
        p1 = tuple(b["c"][j] - axis[j] * half for j in range(3))
        p2 = tuple(b["c"][j] + axis[j] * half for j in range(3))
        hosts = near_plate_handles(b, rec, st) + near_member_handles(b, rec, st)
        hosts = [h for h in hosts if h][:4]
        if not hosts:
            nohost += 1
        dia = bolt_dia(b, rec, st)
        r = None
        for _ in range(3):
            r = eb_api.run("bolt", p1=eb_api._pt(p1), p2=eb_api._pt(p2), dia=dia,
                           style="DIN6914", hosts=",".join(hosts), len=Ln,
                           layer=b.get("layer") or "PS_Bolt", wait=20, _log=False)
            if ok(r):
                break
            time.sleep(1.2)
        if ok(r):
            st["bolts"][k] = H(r)
            made += 1
        else:
            fail += 1
        if i % 25 == 0 or i == len(arr):
            log("   %d/%d (made %d, fail %d, no-host %d)" % (i, len(arr), made, fail, nohost))
            state_save(st)
        time.sleep(PACE)
    state_save(st)
    log("   bolts without hosts: %d (should be ~0)" % nohost)
    return made, fail, skip


def plan():
    rec = load_recipe()
    g = {}
    for e in rec["members"]:
        g.setdefault(assembly_of(e["prof"]), []).append(e)
    log("RECIPE: %d members, %d plates, %d bolts" % (
        len(rec["members"]), len(rec["plate_list"]), len(rec["bolt_list"])))
    for a in ORDER:
        if a in g:
            log("  %-24s %3d" % (a, len(g[a])))
    st = state_load()
    log("STATE: built %d members, %d plates, %d bolts" % (
        len(st["members"]), len(st["plates"]), len(st["bolts"])))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    a = sys.argv[1:] or ["plan"]
    if a[0] == "plan":
        plan()
    elif a[0] == "run":
        rec = load_recipe()
        st = state_load()
        what = a[1] if len(a) > 1 else "all"
        t0 = time.time()
        if what in ("all", "members") or what in ORDER:
            m, f, s = build_members(rec, st, None if what in ("all", "members") else what)
            log("\nMEMBERS: made %d fail %d skip %d" % (m, f, s))
        if what in ("all", "plates"):
            m, f, s = build_plates(rec, st)
            log("\nPLATES: made %d fail %d skip %d" % (m, f, s))
        if what in ("all", "bolts"):
            m, f, s = build_bolts(rec, st)
            log("\nBOLTS: made %d fail %d skip %d" % (m, f, s))
        log("\nelapsed %.1f min" % ((time.time() - t0) / 60))
    else:
        log(__doc__)
