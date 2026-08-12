# -*- coding: utf-8 -*-
"""
exam_shoot.py — visual proof of the exam model, with the anchor bolts showing.

Amir marked 90/100 because the anchors were invisible; he wants to SEE them.
So this shades the model, frames the base-plate detail, and captures it.
"""
import os
import subprocess
import sys
import time

APP = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(APP)
OUT = os.path.join(ROOT, "projects", "שיעור-4", "files")
DWG = os.path.join(ROOT, "projects", "שיעור-4", "מבחן-שיעור-4.dwg")
sys.path.insert(0, APP)
import eb_api  # noqa: E402

SHOT = r'''
Add-Type -AssemblyName System.Windows.Forms, System.Drawing
$b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
$bmp = New-Object System.Drawing.Bitmap $b.Width, $b.Height
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($b.Location, [System.Drawing.Point]::Empty, $b.Size)
$bmp.Save("{path}", [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
'''

FRONT = r'''
$p = Get-Process acad -ErrorAction SilentlyContinue
if ($p) {
  $w = New-Object -ComObject WScript.Shell
  $w.AppActivate($p.Id) | Out-Null
  Start-Sleep -Milliseconds 900
  "fronted"
} else { "no acad" }
'''


def log(m):
    try:
        sys.stdout.write(m + "\n"); sys.stdout.flush()
    except Exception:
        pass


def ps(script, timeout=90):
    r = subprocess.run(["powershell", "-NoProfile", "-Command", script],
                       capture_output=True, text=True, timeout=timeout)
    return (r.stdout or "").strip()


def shot(name):
    p = os.path.join(OUT, name)
    ps(SHOT.format(path=p))
    n = os.path.getsize(p) if os.path.exists(p) else 0
    log("   -> %s (%d bytes)" % (name, n))
    return p


def cmd(s, pause=2.0):
    app = eb_api._app()
    eb_api._send(app.ActiveDocument, "\x1b\x1b" + s)
    time.sleep(pause)


def main():
    os.makedirs(OUT, exist_ok=True)
    log(str(eb_api.run("whoami", wait=30)))

    log("\nmaximise the drawing window and shade it")
    try:
        app = eb_api._app()
        app.ActiveDocument.WindowState = 3
        time.sleep(1.0)
    except Exception as e:
        log("  WindowState: %s" % str(e)[:60])
    cmd('_.VSCURRENT _S\n', 2.5)

    log("\nframe the base-plate detail (plate 400 + ribs + anchors)")
    cmd('_.-VIEW _SWISO\n', 2.5)
    cmd('_.ZOOM _W -420,-420,-450 420,420,700\n', 2.5)

    log("\nbring AutoCAD to the front and capture")
    log("  " + ps(FRONT))
    shot("EXAM-1-baseplate-detail.png")

    log("\nfront view — the anchors run down from the plate")
    cmd('_.-VIEW _FRONT\n', 2.5)
    cmd('_.ZOOM _W -420,-450,420,700\n', 2.5)
    log("  " + ps(FRONT))
    shot("EXAM-2-front-anchors.png")

    log("\nwhole column")
    cmd('_.-VIEW _SWISO\n', 2.5)
    cmd('_.ZOOM _E\n', 2.5)
    log("  " + ps(FRONT))
    shot("EXAM-3-whole-column.png")

    log("\nsave")
    try:
        d = eb_api._app().ActiveDocument
        eb_api._send(d, "\x1b\x1b")
        time.sleep(0.8)
        d.Save()
        time.sleep(4.0)
        log("  saved (%d bytes)" % os.path.getsize(DWG))
    except Exception as e:
        log("  save: %s" % str(e)[:80])


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
