# -*- coding: utf-8 -*-
"""Exam 5 step 0 — read Amir's concrete model: the floor slab and the columns,
so the steel can be anchored to the faces that actually exist."""
import os
import sys
import time

APP = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, APP)
import eb_api  # noqa: E402


def log(m):
    try:
        sys.stdout.write(m + "\n"); sys.stdout.flush()
    except Exception:
        pass


def main():
    log(str(eb_api.run("ping", wait=25)))
    log(str(eb_api.run("whoami", wait=25)))
    app = eb_api._app()
    doc = app.ActiveDocument
    log("\nactive: %s" % doc.Name)

    solids = []
    others = {}
    for e in doc.ModelSpace:
        try:
            nm = e.ObjectName
        except Exception:
            continue
        if nm == "AcDb3dSolid":
            try:
                mn, mx = e.GetBoundingBox()
                solids.append({"h": e.Handle, "min": mn, "max": mx,
                               "layer": e.Layer,
                               "size": (mx[0] - mn[0], mx[1] - mn[1], mx[2] - mn[2])})
            except Exception:
                pass
        else:
            others[nm] = others.get(nm, 0) + 1

    log("\nnon-solid entities: %s" % others)
    log("\n%d solids:" % len(solids))
    # slab = the flat one; columns = the tall ones
    for s in sorted(solids, key=lambda q: -q["size"][2]):
        log("  h=%-5s layer=%-12s  min=(%8.1f,%8.1f,%8.1f)  size=%7.1f x %7.1f x %7.1f"
            % (s["h"], s["layer"], s["min"][0], s["min"][1], s["min"][2],
               s["size"][0], s["size"][1], s["size"][2]))

    tall = [s for s in solids if s["size"][2] > 1000]
    flat = [s for s in solids if s["size"][2] <= 1000]
    log("\nclassified: %d concrete COLUMNS (tall), %d SLAB/floor (flat)" % (len(tall), len(flat)))

    if flat:
        f = flat[0]
        log("\nfloor slab: x %.1f..%.1f  y %.1f..%.1f  z %.1f..%.1f (thickness %.1f)"
            % (f["min"][0], f["max"][0], f["min"][1], f["max"][1],
               f["min"][2], f["max"][2], f["size"][2]))
        log("  -> top of slab (steel bears here): z = %.1f" % f["max"][2])

    log("\nconcrete columns, with their faces:")
    for s in sorted(tall, key=lambda q: (q["min"][0], q["min"][1])):
        log("  h=%-5s  x %8.1f..%8.1f   y %8.1f..%8.1f   z %7.1f..%7.1f   (%.0f x %.0f)"
            % (s["h"], s["min"][0], s["max"][0], s["min"][1], s["max"][1],
               s["min"][2], s["max"][2], s["size"][0], s["size"][1]))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
