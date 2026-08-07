# -*- coding: utf-8 -*-
"""
shoot_proof.py — capture visual proof of the connection work.

The audit's rule: a screenshot of a bolt crossing a plate proves nothing, because
it looks the same with or without a hole. So the money shot here FREEZES the bolt
layer — what remains visible is the hole itself.
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

PS_SHOT = r'''
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
    subprocess.run(["powershell", "-NoProfile", "-Command", PS_SHOT.format(path=p)],
                   capture_output=True, timeout=90)
    log("   shot -> %s (%s bytes)" % (name, os.path.getsize(p) if os.path.exists(p) else "FAIL"))


def lisp(cmd):
    app = eb_api._app()
    doc = app.ActiveDocument
    eb_api._send(doc, cmd)
    time.sleep(1.2)


def main():
    log(str(eb_api.run("whoami", wait=25)))
    log("\n[1] whole structure, isometric")
    eb_api.run("view", dir="iso", wait=25)
    time.sleep(1.5)
    eb_api.run("zoom", mode="extents", wait=25)
    time.sleep(2.0)
    shot("R-iso-all.png")

    # a base-plate zone: the column feet are at z ~ -100..0, x ~ 200-3000
    log("\n[2] zoom to a base-plate connection (bolts visible)")
    lisp('_.ZOOM _W 2600,100,-200 2950,450,200\n')
    time.sleep(1.5)
    shot("R-baseplate-with-bolts.png")

    log("\n[3] SAME view, bolt layer FROZEN — now the holes are the evidence")
    lisp('_.-LAYER _F PS_Bolt \n')
    time.sleep(1.0)
    lisp('_.REGEN \n')
    time.sleep(2.0)
    shot("R-baseplate-HOLES-no-bolts.png")

    log("\n[4] a reshaped rib, bolts still frozen")
    lisp('_.ZOOM _W 1250,5950,6400 1400,6100,6600\n')
    time.sleep(1.5)
    shot("R-rib-chamfered.png")

    log("\n[5] thaw the bolt layer back")
    lisp('_.-LAYER _T PS_Bolt \n')
    time.sleep(1.0)
    lisp('_.REGEN \n')
    time.sleep(1.5)
    eb_api.run("view", dir="iso", wait=25)
    eb_api.run("zoom", mode="extents", wait=25)
    time.sleep(1.5)
    shot("R-iso-final.png")
    log("\ndone")


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
