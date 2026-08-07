# -*- coding: utf-8 -*-
"""
exam5_reset.py — back to the exam's starting point.

Deletes every steel object and leaves Amir's concrete exactly as he modelled it:
the floor slab and the 8 concrete columns (AcDb3dSolid), nothing else touched.
"""
import os
import sys
import time

APP = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(APP)
sys.path.insert(0, APP)
import eb_api  # noqa: E402


def log(m):
    try:
        sys.stdout.write(m + "\n"); sys.stdout.flush()
    except Exception:
        pass


def survey():
    keep, kill = [], []
    for e in eb_api._app().ActiveDocument.ModelSpace:
        try:
            nm = e.ObjectName
        except Exception:
            continue
        if nm in ("AcDb3dSolid", "PcRebarManager"):
            keep.append(nm)
        else:
            try:
                kill.append(e.Handle)
            except Exception:
                pass
    return keep, kill


def main():
    t0 = time.time()
    log("=" * 66)
    log("EXAM 5 — RESET to the starting point (concrete only)")
    log("=" * 66)
    log(str(eb_api.run("whoami", wait=25)))

    keep, kill = survey()
    log("\nkeeping %d concrete objects, deleting %d steel objects"
        % (len(keep), len(kill)))

    n = 0
    for i, h in enumerate(kill, 1):
        r = eb_api.delete(h)
        if isinstance(r, str) and r.startswith("EB_OK"):
            n += 1
        if i % 100 == 0:
            log("   %d/%d deleted (%.1f min)" % (n, len(kill), (time.time()-t0)/60))
    log("   deleted %d/%d" % (n, len(kill)))

    # anything stubborn on a second pass
    keep2, kill2 = survey()
    if kill2:
        log("\nsecond pass on %d leftovers" % len(kill2))
        for h in kill2:
            eb_api.delete(h)
        keep2, kill2 = survey()

    log("\nfinal state:")
    log("   " + str(eb_api.run("whoami", wait=25)))
    log("   concrete objects: %d | steel left: %d" % (len(keep2), len(kill2)))
    log("   " + str(eb_api.run("dumpfull2", out="eb_reset.txt", wait=180)))
    log("   " + str(eb_api.run("dumpholes", out="eb_reset_h.txt", wait=180)))

    # confirm the concrete is intact and untouched
    solids = []
    for e in eb_api._app().ActiveDocument.ModelSpace:
        try:
            if e.ObjectName != "AcDb3dSolid":
                continue
            mn, mx = e.GetBoundingBox()
            solids.append((mx[0]-mn[0], mx[1]-mn[1], mx[2]-mn[2], mn[2]))
        except Exception:
            pass
    tall = [s for s in solids if s[2] > 1000]
    flat = [s for s in solids if s[2] <= 1000]
    log("\n   concrete columns: %d (expect 8) | slab: %d (expect 1)"
        % (len(tall), len(flat)))
    if flat:
        log("   slab %.0f x %.0f x %.0f, top at z=%.0f"
            % (flat[0][0], flat[0][1], flat[0][2], flat[0][3] + flat[0][2]))

    try:
        d = eb_api._app().ActiveDocument
        eb_api._send(d, "\x1b\x1b")
        time.sleep(0.8)
        d.Save()
        time.sleep(4.0)
        log("\n   saved (%d bytes)" % os.path.getsize(d.FullName))
    except Exception as e:
        log("   save: %s" % str(e)[:80])
    log("\nelapsed %.1f min — ready for the retake" % ((time.time()-t0)/60))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
