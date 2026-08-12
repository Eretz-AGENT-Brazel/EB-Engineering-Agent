# -*- coding: utf-8 -*-
"""
read_source_deep.py — R2.4: read the ORIGINAL model deeply (READ ONLY).

Reads what the bbox-only reader could never see:
  * every plate's REAL contour  -> which plates are ribs / cut gussets
  * every part's REAL holes     -> count, diameter, round or slotted

Nothing is created, nothing is copied between models. Output goes to
knowledge/recipes/ as the v2 recipe inputs.

Usage: python read_source_deep.py <dwg-path> <tag>
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
        sys.stdout.write(m + "\n")
        sys.stdout.flush()
    except Exception:
        pass


def open_dwg(path):
    app = eb_api._app()
    # already open?
    try:
        for i in range(app.Documents.Count):
            d = app.Documents.Item(i)
            if os.path.normcase(d.FullName) == os.path.normcase(path):
                d.Activate()
                log("   already open, activated")
                return True
    except Exception as e:
        log("   scan docs failed: %s" % e)
    for attempt in range(8):
        try:
            d = app.Documents.Open(path)
            time.sleep(1.5)
            d.Activate()
            return True
        except Exception as e:
            log("   open retry %d: %s" % (attempt + 1, str(e)[:70]))
            time.sleep(3)
    return False


def main():
    dwg = sys.argv[1] if len(sys.argv) > 1 else os.path.join(
        ROOT, "projects", "שיעור-2", "שיעור-2.dwg")
    tag = sys.argv[2] if len(sys.argv) > 2 else "src"

    log("opening (READ ONLY): %s" % dwg)
    if not open_dwg(dwg):
        log("FAILED to open"); return
    log("active: " + str(eb_api.run("whoami", wait=30)))

    log("\n--- holes census (the target we must reach) ---")
    log(str(eb_api.run("dumpholes", out="eb_holes_%s.txt" % tag, maxx=15000, wait=180)))

    log("\n--- plate contours (ribs / cut gussets) ---")
    log(str(eb_api.run("dumppoly", out="eb_poly_%s.txt" % tag, maxx=15000, wait=180)))

    log("\n--- full geometry dump (members/plates/bolts) ---")
    log(str(eb_api.run("dumpfull2", out="eb_full_%s.txt" % tag, wait=180)))

    for f in ["eb_holes_%s.txt" % tag, "eb_poly_%s.txt" % tag, "eb_full_%s.txt" % tag]:
        p = os.path.join(APP, "plugin", f)
        log("%-24s %s bytes" % (f, os.path.getsize(p) if os.path.exists(p) else "MISSING"))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
