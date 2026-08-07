# -*- coding: utf-8 -*-
"""
front_and_shoot.py — bring the lesson-3 drawing to the FRONT window and capture.

Lesson learned the hard way: COM Activate() changes the logical ActiveDocument,
but the MDI window Amir is looking at can still be a different drawing — so he
sees nothing happen. Here we also maximise the MDI child and zoom via LISP
(the plugin has no view/zoom op — that error was silently ignored before).
"""
import os
import subprocess
import sys
import time

APP = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(APP)
OUT = os.path.join(ROOT, "projects", "שיעור-3-מאפס", "files")
sys.path.insert(0, APP)
import eb_api  # noqa: E402

L3 = os.path.join(ROOT, "projects", "שיעור-3-מאפס", "שיעור-3.dwg")

SHOT = r'''
Add-Type -AssemblyName System.Windows.Forms, System.Drawing
$b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
$bmp = New-Object System.Drawing.Bitmap $b.Width, $b.Height
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($b.Location, [System.Drawing.Point]::Empty, $b.Size)
$bmp.Save("{path}", [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
'''


def log(m):
    try:
        sys.stdout.write(m + "\n"); sys.stdout.flush()
    except Exception:
        pass


def shot(name):
    p = os.path.join(OUT, name)
    subprocess.run(["powershell", "-NoProfile", "-Command", SHOT.format(path=p)],
                   capture_output=True, timeout=90)
    log("   -> %s (%s bytes)" % (name, os.path.getsize(p) if os.path.exists(p) else "FAIL"))


def cmd(s, pause=1.5):
    """ESC-ESC first: never leave a half-entered command on Amir's command line."""
    app = eb_api._app()
    eb_api._send(app.ActiveDocument, "\x1b\x1b" + s)
    time.sleep(pause)


def main():
    app = eb_api._app()
    log("documents open: %d" % app.Documents.Count)
    target = None
    for i in range(app.Documents.Count):
        d = app.Documents.Item(i)
        log("   [%d] %s" % (i, d.Name))
        if os.path.normcase(d.FullName) == os.path.normcase(L3):
            target = d
    if target is None:
        log("lesson-3 not open — opening")
        target = app.Documents.Open(L3)
        time.sleep(3)

    log("\nbringing it to the FRONT (Activate + maximise + WSCURRENT)")
    target.Activate()
    time.sleep(1.5)
    try:
        app.Visible = True
    except Exception:
        pass
    # 1 = acMax : maximise this MDI child so it is what fills the screen
    try:
        target.WindowState = 3
        time.sleep(1.0)
    except Exception as e:
        log("   WindowState: %s" % str(e)[:60])
    log("   active now: " + str(eb_api.run("whoami", wait=25)))

    log("\nzoom + view via LISP (plugin has no view/zoom op)")
    cmd('_.-VIEW _SWISO \n', 2.0)
    cmd('_.ZOOM _E \n', 2.5)
    shot("S1-iso-all.png")

    log("\nshaded, so plates and holes read properly")
    cmd('_.VSCURRENT _S \n', 2.5)
    shot("S2-iso-shaded.png")

    log("\nzoom to the column feet (base plates + bolts)")
    cmd('_.ZOOM _W 100,100,-300 3200,600,400\n', 2.0)
    shot("S3-baseplates-shaded.png")

    log("\nsame view, bolts frozen -> the HOLES are the proof")
    cmd('_.-LAYER _F PS_Bolt \n', 1.2)
    cmd('_.REGEN \n', 2.5)
    shot("S4-HOLES-bolts-frozen.png")

    log("\nthaw bolts, back to full iso")
    cmd('_.-LAYER _T PS_Bolt \n', 1.2)
    cmd('_.REGEN \n', 2.0)
    cmd('_.ZOOM _E \n', 2.0)
    shot("S5-final-iso.png")
    log("\ndone")


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
