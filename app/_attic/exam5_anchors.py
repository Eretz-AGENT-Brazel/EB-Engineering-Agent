# -*- coding: utf-8 -*-
"""
exam5_anchors.py — finish the anchors, simply.

What is left (Amir's own summary):
  * the middle-row holes in the floor base plates have no bolt
  * none of the wall-plate holes have bolts

Method (his hint): take an anchor the base-plate macro already made, COPY it to
each hole centre, and use 3DROTATE to turn it 90 degrees for the wall plates so
it runs horizontally into the concrete. Native copy keeps the nut and thread.
"""
import json
import os
import sys
import time

APP = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(APP)
sys.path.insert(0, APP)
import eb_api  # noqa: E402

ST = os.path.join(ROOT, "projects", "שיעור-5", "files", "exam5_state.json")
DEST = os.path.join(ROOT, "projects", "שיעור-5", "מבחן-שיעור-5.dwg")
F_HX = 156.0            # floor: 2 columns at 156 apart
W_HX, W_HZ = 452.0, 200.0
FP_T = 20.0
WP_T = 20.0
PARK = 60000.0          # where rotated masters are prepared


def log(m):
    try:
        sys.stdout.write(m + "\n"); sys.stdout.flush()
    except Exception:
        pass


def cmd(s, pause=0.30):
    for _ in range(4):
        try:
            eb_api._send(eb_api._app().ActiveDocument, "\x1b\x1b" + s)
            time.sleep(pause)
            return True
        except Exception:
            time.sleep(1.5)
    return False


def copy_to(handle, frm, to, pause=0.30):
    cmd('_COPY (handent "%s") "" _non 0,0,0 _non %.3f,%.3f,%.3f\n'
        % (handle, to[0] - frm[0], to[1] - frm[1], to[2] - frm[2]), pause)


def volbodies():
    """small Ks_VolBody objects = the anchor bolts"""
    out = []
    for e in eb_api._app().ActiveDocument.ModelSpace:
        try:
            if e.ObjectName != "Ks_VolBody":
                continue
            mn, mx = e.GetBoundingBox()
            sz = (mx[0]-mn[0], mx[1]-mn[1], mx[2]-mn[2])
            if 50 < max(sz) < 1000:
                out.append({"h": e.Handle, "sz": sz,
                            "c": ((mn[0]+mx[0])/2, (mn[1]+mx[1])/2, (mn[2]+mx[2])/2),
                            "axis": sz.index(max(sz))})
        except Exception:
            pass
    return out


def make_master(src, axis_char, park_x):
    """copy the vertical anchor aside and 3DROTATE it 90 deg to lie horizontal"""
    before = set(v["h"] for v in volbodies())
    target = (park_x, 0.0, 0.0)
    copy_to(src["h"], src["c"], target, 1.2)
    new = [v for v in volbodies() if v["h"] not in before]
    if not new:
        return None
    m = new[0]
    cmd('_ROTATE3D (handent "%s") "" _%s _non %.3f,%.3f,%.3f 90\n'
        % (m["h"], axis_char, m["c"][0], m["c"][1], m["c"][2]), 1.5)
    for v in volbodies():
        if v["h"] == m["h"]:
            return v
    return m


def main():
    t0 = time.time()
    st = json.load(open(ST, encoding="utf-8"))
    cols, centres, slab_top = st["columns"], st["centres"], st["slab_top"]
    log("=" * 72)
    log("EXAM 5 — completing the anchor bolts")
    log("=" * 72)

    anch = volbodies()
    vert = [a for a in anch if a["axis"] == 2]
    log("anchors present: %d (vertical %d)" % (len(anch), len(vert)))
    if not vert:
        log("!! no vertical anchor to copy"); return
    src = vert[0]
    log("source anchor %s  size %s" % (src["h"], tuple(round(v, 1) for v in src["sz"])))

    # ---- 1. floor plates: the two middle-row holes ----
    log("\n[1] floor plates — middle row (2 per plate)")
    n = 0
    zc = slab_top + FP_T / 2.0
    for c in cols:
        # base plate is centred on the steel column
        for dx in (-F_HX / 2.0, F_HX / 2.0):
            copy_to(src["h"], src["c"], (c["x"] + dx, c["y"], src["c"][2]))
            n += 1
    log("    copied %d floor anchors (expect %d)" % (n, len(cols) * 2))

    # ---- 2. wall plates: horizontal anchors ----
    log("\n[2] wall plates — rotating a master 90 deg per direction")
    # anchors pointing along X (for +X / -X faces) : rotate about Y
    mx = make_master(src, "Y", PARK)
    # anchors pointing along Y (for +Y / -Y faces) : rotate about X
    my = make_master(src, "X", PARK + 2000)
    log("    master along X: %s" % (tuple(round(v, 1) for v in mx["sz"]) if mx else "FAILED"))
    log("    master along Y: %s" % (tuple(round(v, 1) for v in my["sz"]) if my else "FAILED"))
    if not mx or not my:
        log("!! could not prepare rotated masters"); return

    w = 0
    for i, c in enumerate(cols, 1):
        face, coord, along = c["face"], c["coord"], c["along"]
        sgn = 1.0 if face in ("+X", "+Y") else -1.0
        horiz = face in ("+X", "-X")
        master = mx if horiz else my
        for z in centres:
            for du in (-W_HX / 2.0, W_HX / 2.0):
                for dv in (-W_HZ, 0.0, W_HZ):
                    if horiz:
                        tgt = (coord + sgn * WP_T / 2.0, along + du, z + dv)
                    else:
                        tgt = (along + du, coord + sgn * WP_T / 2.0, z + dv)
                    copy_to(master["h"], master["c"], tgt)
                    w += 1
        if i % 5 == 0 or i == len(cols):
            log("    col %2d/%d : %d wall anchors (%.1f min)"
                % (i, len(cols), w, (time.time() - t0) / 60))
    log("    copied %d wall anchors (expect %d)" % (w, len(cols) * 18))

    # ---- 3. remove the parked masters ----
    log("\n[3] removing the parked masters")
    for v in volbodies():
        if v["c"][0] > PARK - 5000:
            log("    del %s -> %s" % (v["h"], eb_api.delete(v["h"])))

    log("\n[4] verify + save")
    log("    " + str(eb_api.run("whoami", wait=25)))
    total = len([v for v in volbodies()])
    log("    anchor bodies in model: %d (expect %d)"
        % (total, len(cols) * 4 + len(cols) * 2 + len(cols) * 18))
    try:
        app = eb_api._app()
        eb_api._send(app.ActiveDocument, "\x1b\x1b")
        time.sleep(0.8)
        app.ActiveDocument.Save()
        time.sleep(5.0)
        log("    saved (%d bytes)" % os.path.getsize(DEST))
    except Exception as e:
        log("    save: %s" % str(e)[:80])
    log("\nelapsed %.1f min" % ((time.time() - t0) / 60))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
