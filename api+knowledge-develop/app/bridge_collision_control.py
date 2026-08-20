# -*- coding: utf-8 -*-
"""bridge_collision_control.py -- the collision reading on the SOURCE, which is the control.

    python app/bridge_collision_control.py

`collision` creates a `Ks_VolBody` per hit, so it is a WRITE, and Bernie's file may never be
written to. The project's copy of the source cannot be used either -- it carries **the same
filename**, and every guard in this project selects a drawing by name, so "run it on the copy"
resolves to whichever of the two is in front. That is how a promise kept by a filename gets
broken.

⇒ This copies the source to a **distinctly named** file, opens that, measures there, and
leaves Bernie's file and the project copy untouched. The rebuild measured 6,444 collisions;
a number with no control is not a finding.
"""
import io
import os
import re
import shutil
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
DEV = os.path.dirname(HERE)
if HERE not in sys.path:
    sys.path.insert(0, HERE)
import eb_api  # noqa: E402

PROJ = os.path.join(DEV, "projects", "bridge-bernie")
SRC_COPY = os.path.join(PROJ, "bridge model for amir.dwg")
CHECK = os.path.join(PROJ, "bridge model for amir - SOURCE-CHECK.dwg")


def main():
    if not os.path.exists(CHECK):
        shutil.copy2(SRC_COPY, CHECK)
        print("copied -> %s (%d bytes)" % (os.path.basename(CHECK), os.path.getsize(CHECK)))
    eb_api.open_model(CHECK, task="collision control on a copy of the source",
                      project="bridge-bernie")
    eb_api.build_target(CHECK)
    r = eb_api.run("list", wait=1800)
    act = eb_api._active_doc_name() or ""
    print("in: %s  (%s)" % (act, r[:60]))
    if "SOURCE-CHECK" not in act:
        raise RuntimeError("refusing: the active document is %r" % act)
    t = time.time()
    col = eb_api.run("collision", minvol=100, clean=1, wait=7200)
    print("collision on the SOURCE copy, %.0fs:\n   %s" % (time.time() - t, col[:240]))
    m = re.search(r"collisions=(\d+)", col)
    io.open(os.path.join(PROJ, "collision-control.txt"), "w", encoding="utf-8").write(
        col + "\n")
    if m:
        print("\nCONTROL: the source has %s collisions; the rebuild measured 6,444." % m.group(1))
    eb_api.build_target(None)


if __name__ == "__main__":
    main()
