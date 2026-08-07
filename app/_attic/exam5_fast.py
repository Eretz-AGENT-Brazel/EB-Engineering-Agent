# -*- coding: utf-8 -*-
"""
exam5_fast.py — finish the anchors Amir's way: one set, then replicate.

The 20 steel columns fall into 4 orientation groups (+X, -X, +Y, -Y), 5 columns
each. So:
  * make ONE horizontal anchor master per direction (copy a vertical one, 3DROTATE)
  * populate the anchor set of the FIRST column in each group (2 floor + 18 wall)
  * then COPY THAT WHOLE SET, in a single _COPY per target, to the other 4 columns
    of the group — same orientation, so a pure displacement

That is 4 sets built + 16 group copies instead of 400 individual placements.
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
F_HX = 156.0
W_HX, W_HZ = 452.0, 200.0
WP_T = 20.0
PARK = 60000.0


def log(m):
    try:
        sys.stdout.write(m + "\n"); sys.stdout.flush()
    except Exception:
        pass


def cmd(s, pause=0.25):
    for _ in range(4):
        try:
            eb_api._send(eb_api._app().ActiveDocument, "\x1b\x1b" + s)
            time.sleep(pause)
            return True
        except Exception:
            time.sleep(1.5)
    return False


def bodies():
    out = {}
    for e in eb_api._app().ActiveDocument.ModelSpace:
        try:
            if e.ObjectName != "Ks_VolBody":
                continue
            mn, mx = e.GetBoundingBox()
            sz = (mx[0]-mn[0], mx[1]-mn[1], mx[2]-mn[2])
            if 50 < max(sz) < 1000:
                out[e.Handle] = {"h": e.Handle, "sz": sz,
                                 "c": ((mn[0]+mx[0])/2, (mn[1]+mx[1])/2, (mn[2]+mx[2])/2),
                                 "axis": sz.index(max(sz))}
        except Exception:
            pass
    return out


def copy_one(h, frm, to, pause=0.25):
    """LISP form — this is the one proven to work (lesson 2)."""
    cmd('(command "_copy" (handent "%s") "" "_non" "0,0,0" "_non" "%.3f,%.3f,%.3f")\n'
        % (h, to[0]-frm[0], to[1]-frm[1], to[2]-frm[2]), pause)


def copy_many(handles, dx, dy, dz, pause=1.2):
    """one _COPY taking the WHOLE set — the replicate step.
    Build a selection set from the handles, then copy it in a single command."""
    add = " ".join('(ssadd (handent "%s") ss)' % h for h in handles)
    cmd('(progn (setq ss (ssadd)) %s (command "_copy" ss "" "_non" "0,0,0"'
        ' "_non" "%.3f,%.3f,%.3f"))\n' % (add, dx, dy, dz), pause)


def rot3d(h, axis_char, c, pause=1.2):
    cmd('(command "_rotate3d" (handent "%s") "" "_%s" "_non" "%.3f,%.3f,%.3f" "90")\n'
        % (h, axis_char.lower(), c[0], c[1], c[2]), pause)


def main():
    t0 = time.time()
    st = json.load(open(ST, encoding="utf-8"))
    cols, centres = st["columns"], st["centres"]
    log("=" * 70)
    log("EXAM 5 — anchors: one set per direction, then replicate")
    log("=" * 70)

    have = bodies()
    vert = [v for v in have.values() if v["axis"] == 2]
    log("anchors present %d (vertical %d)" % (len(have), len(vert)))
    src = vert[0]

    # ---- masters: one horizontal anchor per axis ----
    log("\n[1] rotated masters")
    masters = {}
    for axis_char, key, px in (("Y", "X", PARK), ("X", "Y", PARK + 3000)):
        before = set(bodies())
        copy_one(src["h"], src["c"], (px, 0.0, 0.0), 1.2)
        new = [h for h in bodies() if h not in before]
        if not new:
            log("   master %s FAILED" % key); return
        m = bodies()[new[0]]
        cmd('_ROTATE3D (handent "%s") "" _%s _non %.3f,%.3f,%.3f 90\n'
            % (m["h"], axis_char, m["c"][0], m["c"][1], m["c"][2]), 1.5)
        m = bodies()[m["h"]]
        masters[key] = m
        log("   master along %s : %s  size %s"
            % (key, m["h"], tuple(round(v, 1) for v in m["sz"])))

    # ---- group the columns by orientation ----
    groups = {}
    for c in cols:
        groups.setdefault(c["face"], []).append(c)
    log("\n[2] orientation groups: %s" % {k: len(v) for k, v in groups.items()})

    total = 0
    for face, members in groups.items():
        first = members[0]
        rest = members[1:]
        sgn = 1.0 if face in ("+X", "+Y") else -1.0
        horiz = face in ("+X", "-X")
        master = masters["X"] if horiz else masters["Y"]
        log("\n   --- %s : building the set on column %s, then %d copies ---"
            % (face, first["h"], len(rest)))

        before = set(bodies())
        # 2 floor anchors (the drawing's middle row)
        for dx in (-F_HX / 2.0, F_HX / 2.0):
            copy_one(src["h"], src["c"], (first["x"] + dx, first["y"], src["c"][2]))
        # 18 wall anchors
        for z in centres:
            for du in (-W_HX / 2.0, W_HX / 2.0):
                for dv in (-W_HZ, 0.0, W_HZ):
                    if horiz:
                        tgt = (first["coord"] + sgn * WP_T / 2.0, first["along"] + du, z + dv)
                    else:
                        tgt = (first["along"] + du, first["coord"] + sgn * WP_T / 2.0, z + dv)
                    copy_one(master["h"], master["c"], tgt)
        made = [h for h in bodies() if h not in before]
        total += len(made)
        log("      set built: %d anchors (%.1f min)" % (len(made), (time.time()-t0)/60))

        # replicate the whole set to the other columns of this group
        for c in rest:
            dx = c["x"] - first["x"]
            dy = c["y"] - first["y"]
            copy_many(made, dx, dy, 0.0)
            total += len(made)
        log("      replicated to %d more columns -> %d anchors so far"
            % (len(rest), total))

    log("\n[3] removing parked masters")
    for v in list(bodies().values()):
        if v["c"][0] > PARK - 5000:
            eb_api.delete(v["h"])

    log("\n[4] verify + save")
    fin = bodies()
    log("   anchor bodies now: %d" % len(fin))
    log("   " + str(eb_api.run("whoami", wait=25)))
    try:
        app = eb_api._app()
        eb_api._send(app.ActiveDocument, "\x1b\x1b")
        time.sleep(0.8)
        app.ActiveDocument.Save()
        time.sleep(5.0)
        log("   saved (%d bytes)" % os.path.getsize(DEST))
    except Exception as e:
        log("   save: %s" % str(e)[:80])
    log("\nelapsed %.1f min" % ((time.time()-t0)/60))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
