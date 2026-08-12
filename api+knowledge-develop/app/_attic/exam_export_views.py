# -*- coding: utf-8 -*-
"""
exam_export_views.py — export images FROM AutoCAD, not from the screen.

Screen capture is useless when AutoCAD is in the background (it grabs whatever
window is in front). AutoCAD's own PNGOUT writes the current viewport to a file
regardless of which window has focus — so the proof comes from the drawing
itself.
"""
import os
import sys
import time

APP = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(APP)
OUT = os.path.join(ROOT, "projects", "שיעור-4", "files")
sys.path.insert(0, APP)
import eb_api  # noqa: E402


def log(m):
    try:
        sys.stdout.write(m + "\n"); sys.stdout.flush()
    except Exception:
        pass


def cmd(s, pause=2.2):
    """AutoCAD's COM ActiveDocument can be briefly unavailable while it is busy,
    so re-acquire and retry rather than dying mid-export."""
    for attempt in range(6):
        try:
            app = eb_api._app()
            doc = app.ActiveDocument
            eb_api._send(doc, "\x1b\x1b" + s)
            time.sleep(pause)
            return True
        except Exception as e:
            log("      (retry %d: %s)" % (attempt + 1, str(e)[:50]))
            time.sleep(2.5)
    return False


def png(name, view, zoom, shade="_S"):
    """set a view, frame it, then let AutoCAD write the PNG"""
    p = os.path.join(OUT, name)
    if os.path.exists(p):
        try:
            os.remove(p)
        except Exception:
            pass
    cmd('_.-VIEW %s\n' % view, 2.5)
    cmd('_.VSCURRENT %s\n' % shade, 2.0)
    cmd('_.ZOOM %s\n' % zoom, 2.5)
    # PNGOUT: filename, then ALL objects (empty selection = all), then enter
    cmd('_.PNGOUT\n%s\n\n' % p.replace("\\", "/"), 5.0)
    n = os.path.getsize(p) if os.path.exists(p) else 0
    log("   %-32s %s" % (name, ("%d bytes" % n) if n else "NOT WRITTEN"))
    return n > 0


def main():
    os.makedirs(OUT, exist_ok=True)
    log(str(eb_api.run("whoami", wait=30)))
    log("\nexporting views from AutoCAD itself (PNGOUT):")

    png("V1-iso-detail.png", "_SWISO", "_C 0,0,100 1200")
    png("V2-front-anchors.png", "_FRONT", "_C 0,0,50 1100")
    png("V3-top-plate.png", "_TOP", "_C 0,0,0 700")
    png("V4-whole-column.png", "_SWISO", "_E")

    log("\nfiles now in the project folder:")
    for f in sorted(os.listdir(OUT)):
        if f.lower().endswith(".png"):
            log("   %-34s %d bytes" % (f, os.path.getsize(os.path.join(OUT, f))))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
