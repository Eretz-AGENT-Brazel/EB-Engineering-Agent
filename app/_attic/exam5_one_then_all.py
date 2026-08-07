# -*- coding: utf-8 -*-
"""
exam5_one_then_all.py — Amir's method, exactly as he instructed:
"delete all the columns except column 1, build it fully, then replicate."

  1. keep only column 1's steel (concrete untouched), delete the other 19
  2. complete column 1: the 2 middle-row floor anchors and the 18 wall anchors
  3. replicate the WHOLE detail to the other 19 places with one _COPY each,
     rotating about Z by 0 / 90 / 180 / 270 depending on which concrete face
     it serves

One detail built and verified, 19 copies. Not 500 placements.
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
FACE_ANG = {"+X": 0.0, "+Y": 90.0, "-X": 180.0, "-Y": 270.0}


def log(m):
    try:
        sys.stdout.write(m + "\n"); sys.stdout.flush()
    except Exception:
        pass


def cmd(s, pause=0.25):
    for _ in range(5):
        try:
            eb_api._send(eb_api._app().ActiveDocument, "\x1b\x1b" + s)
            time.sleep(pause)
            return True
        except Exception:
            time.sleep(1.5)
    return False


def steel():
    """every non-concrete entity, with its bbox centre"""
    out = []
    for e in eb_api._app().ActiveDocument.ModelSpace:
        try:
            if e.ObjectName in ("AcDb3dSolid", "PcRebarManager"):
                continue
            mn, mx = e.GetBoundingBox()
            out.append({"h": e.Handle, "cls": e.ObjectName,
                        "c": ((mn[0]+mx[0])/2, (mn[1]+mx[1])/2, (mn[2]+mx[2])/2),
                        "mn": mn, "mx": mx})
        except Exception:
            pass
    return out


def copy_set(handles, dx, dy, pause=1.4):
    add = " ".join('(ssadd (handent "%s") ss)' % h for h in handles)
    return cmd('(progn (setq ss (ssadd)) %s (command "_copy" ss "" "_non" "0,0,0"'
               ' "_non" "%.3f,%.3f,0"))\n' % (add, dx, dy), pause)


def rotate_set(handles, cx, cy, ang, pause=1.4):
    add = " ".join('(ssadd (handent "%s") ss)' % h for h in handles)
    return cmd('(progn (setq ss (ssadd)) %s (command "_rotate" ss "" "_non"'
               ' "%.3f,%.3f" "%.1f"))\n' % (add, cx, cy, ang), pause)


def copy_one(h, frm, to, pause=0.22):
    cmd('(command "_copy" (handent "%s") "" "_non" "0,0,0" "_non" "%.3f,%.3f,%.3f")\n'
        % (h, to[0]-frm[0], to[1]-frm[1], to[2]-frm[2]), pause)


def main():
    t0 = time.time()
    st = json.load(open(ST, encoding="utf-8"))
    cols, centres = st["columns"], st["centres"]
    first = cols[0]
    log("=" * 70)
    log("EXAM 5 — build column 1 fully, then replicate to the other %d" % (len(cols)-1))
    log("=" * 70)
    log("column 1: face %s at (%.1f, %.1f)" % (first["face"], first["x"], first["y"]))

    # ---- 1. keep only column 1's steel ----
    log("\n[1] deleting the other columns' steel")
    keep_box = (first["x"] - 400, first["x"] + 400, first["y"] - 400, first["y"] + 400)
    al = steel()
    keep, kill = [], []
    for s in al:
        if keep_box[0] <= s["c"][0] <= keep_box[1] and keep_box[2] <= s["c"][1] <= keep_box[3]:
            keep.append(s)
        else:
            kill.append(s)
    log("    keeping %d objects, deleting %d" % (len(keep), len(kill)))
    n = 0
    for s in kill:
        r = eb_api.delete(s["h"])
        if isinstance(r, str) and r.startswith("EB_OK"):
            n += 1
    log("    deleted %d (%.1f min)" % (n, (time.time()-t0)/60))

    # ---- 2. complete column 1 ----
    log("\n[2] completing column 1's anchors")
    have = [s for s in steel() if s["cls"] == "Ks_VolBody"
            and max(s["mx"][k]-s["mn"][k] for k in range(3)) < 1000]
    log("    existing anchors on it: %d" % len(have))
    src = have[0]
    # floor middle row
    for dx in (-F_HX/2.0, F_HX/2.0):
        copy_one(src["h"], src["c"], (first["x"] + dx, first["y"], src["c"][2]))
    # a horizontal master
    before = set(s["h"] for s in steel())
    copy_one(src["h"], src["c"], (PARK, 0.0, 0.0), 1.2)
    new = [s for s in steel() if s["h"] not in before]
    if not new:
        log("    !! master copy failed"); return
    m = new[0]
    cmd('(command "_rotate3d" (handent "%s") "" "_y" "_non" "%.3f,%.3f,%.3f" "90")\n'
        % (m["h"], m["c"][0], m["c"][1], m["c"][2]), 1.5)
    m = [s for s in steel() if s["h"] == m["h"]][0]
    log("    horizontal master %s size %s" % (m["h"],
        tuple(round(m["mx"][k]-m["mn"][k], 1) for k in range(3))))
    # 18 wall anchors (column 1 faces +X, so plate spans Y)
    for z in centres:
        for du in (-W_HX/2.0, W_HX/2.0):
            for dv in (-W_HZ, 0.0, W_HZ):
                copy_one(m["h"], m["c"],
                         (first["coord"] + WP_T/2.0, first["along"] + du, z + dv))
    eb_api.delete(m["h"])
    log("    column 1 complete (%.1f min)" % ((time.time()-t0)/60))

    # ---- 3. replicate the whole detail ----
    detail = [s["h"] for s in steel()
              if keep_box[0] <= s["c"][0] <= keep_box[1]
              and keep_box[2] <= s["c"][1] <= keep_box[3]]
    log("\n[3] the detail is %d objects — replicating to %d places"
        % (len(detail), len(cols)-1))
    base_ang = FACE_ANG[first["face"]]
    done = 0
    for c in cols[1:]:
        before = set(s["h"] for s in steel())
        if not copy_set(detail, c["x"] - first["x"], c["y"] - first["y"]):
            continue
        new = [s["h"] for s in steel() if s["h"] not in before]
        if not new:
            log("    copy to (%.0f,%.0f) produced nothing" % (c["x"], c["y"]))
            continue
        ang = FACE_ANG[c["face"]] - base_ang
        if abs(ang) > 0.1:
            rotate_set(new, c["x"], c["y"], ang)
        done += 1
        log("    %2d/%d -> (%.0f, %.0f) face %s rot %.0f  (%d objs, %.1f min)"
            % (done, len(cols)-1, c["x"], c["y"], c["face"], ang, len(new),
               (time.time()-t0)/60))

    # ---- 4. verify + save ----
    log("\n[4] verify")
    log("    " + str(eb_api.run("whoami", wait=25)))
    log("    " + str(eb_api.run("dumpfull2", out="eb_e5f.txt", wait=240)))
    log("    " + str(eb_api.run("dumpholes", out="eb_e5fh.txt", wait=240)))
    try:
        app = eb_api._app()
        eb_api._send(app.ActiveDocument, "\x1b\x1b")
        time.sleep(0.8)
        app.ActiveDocument.Save()
        time.sleep(5.0)
        log("    saved (%d bytes)" % os.path.getsize(DEST))
    except Exception as e:
        log("    save: %s" % str(e)[:80])
    log("\nTOTAL %.1f min" % ((time.time()-t0)/60))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
