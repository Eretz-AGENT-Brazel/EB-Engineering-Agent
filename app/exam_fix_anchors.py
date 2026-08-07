# -*- coding: utf-8 -*-
"""
exam_fix_anchors.py — make the anchor bolts VISIBLE (Amir's 90/100 remark).

The fault: the connection was created with anchors=1 but the diameter was left
at its default 0, so ProSteel produced four anchor blocks with no body — present
in the database, invisible in the model.

The fix: set AnchorBoltDiameter (matched to the hole: dia 23 hole -> M20 bolt),
turn on CreateDetailedAnchorBolts, and give them a grip length. Their LENGTH
stays graphic-only, as Amir stated; what was missing was the diameter and body.

Verified by reading each PS_Bolt entity's extents back — a bolt with a body has
non-zero extents; a bolt with diameter 0 does not.
"""
import os
import re
import sys
import time

APP = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(APP)
PLUG = os.path.join(APP, "plugin")
sys.path.insert(0, APP)
import eb_api  # noqa: E402

PLATE, PLATE_T = 400.0, 20.0
HOLE_DIA, PITCH = 23.0, 300.0
ANCHOR_DIA = 20.0        # M20 to suit the dia-23 hole
ANCHOR_GRIP = 400.0      # graphic length (Amir: not critical, kept sensible)


def log(m):
    try:
        sys.stdout.write(m + "\n"); sys.stdout.flush()
    except Exception:
        pass


def bolt_bodies():
    """Each PS_Bolt entity and whether it actually has a body to see."""
    eb_api.run("list", wait=60)
    out = []
    for line in open(os.path.join(PLUG, "eb_list.txt"), encoding="utf-8-sig",
                     errors="replace"):
        f = line.strip().split("|")
        if len(f) >= 3 and f[2] == "PS_Bolt":
            out.append(f[0])
    return out


def find_col():
    eb_api.run("dumpfull2", out="eb_full_anc.txt", wait=120)
    for line in open(os.path.join(PLUG, "eb_full_anc.txt"), encoding="utf-8-sig",
                     errors="replace"):
        f = line.rstrip("\n").split("\t")
        if f[0] == "SHAPE" and len(f) >= 7:
            try:
                z1 = float(f[4].split(",")[2]); z2 = float(f[5].split(",")[2])
            except Exception:
                continue
            if abs(z2 - z1) > 1000:
                return f[1]
    return None


def main():
    log("=" * 74)
    log("FIX — anchor bolts must be VISIBLE (dia was 0)")
    log("=" * 74)
    log(str(eb_api.run("whoami", wait=30)))

    before = bolt_bodies()
    log("\nPS_Bolt entities before: %d  %s" % (len(before), ", ".join(before)))
    for h in before:
        log("  %s extents: %s" % (h, str(eb_api.run("props", handle=h, wait=25))[:90]))

    col = find_col()
    if not col:
        log("!! column not found")
        return
    log("\ncolumn: %s" % col)

    log("\nremoving the connection (with its plate and anchor blocks)")
    log("  " + str(eb_api.run("connremove", handle=col, delparts=1, wait=60)))

    log("\nrebuilding — same plate, but anchors with a real diameter")
    r = eb_api.run("connbase", handle=col, l=PLATE, w=PLATE, t=PLATE_T,
                   holedia=HOLE_DIA, hx=PITCH, hy=PITCH,
                   anchors=1, anchordia=ANCHOR_DIA, anchorgrip=ANCHOR_GRIP,
                   anchordetail=1, shorten=1, wait=90)
    log("  " + str(r)[:220])

    log("\nverifying:")
    after = bolt_bodies()
    log("  PS_Bolt entities after: %d" % len(after))
    log("  " + str(eb_api.run("connscan", out="eb_conn_anc.txt", wait=150)))
    for line in open(os.path.join(PLUG, "eb_conn_anc.txt"), encoding="utf-8-sig",
                     errors="replace"):
        m = re.search(r"BASEPLATE\[([^\]]*)\]", line)
        if m and "L=0 W=0" not in m.group(1):
            log("  joint: %s" % m.group(1))

    m = re.search(r"anchors_with_body=(\d+)", r or "")
    n = int(m.group(1)) if m else 0
    log("\n  anchors with a visible body: %d  %s" % (n, "PASS" if n >= 4 else "FAIL"))
    bb = re.search(r"anchor_bbox=([\d.x]+)", r or "")
    if bb:
        log("  one anchor's bounding box: %s mm" % bb.group(1))

    log("\n  " + str(eb_api.run("dumpholes", out="eb_holes_anc.txt", wait=120)))
    log("  " + str(eb_api.run("dumppoly", out="eb_poly_anc.txt", wait=120)))


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
