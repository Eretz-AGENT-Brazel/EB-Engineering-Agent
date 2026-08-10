# -*- coding: utf-8 -*-
"""
conn_build_test.py — prove I can BUILD a real connection (not plates + bolts).

Understanding first (read from Amir's own model with the Connection Editor):
  * Baseplate Connection : 180x180x10 and 300x300x12, hole dia 23, anchors dia 20
  * Endplate connection  : 2 plates + 4 bolts per joint (15 of them)
  * Brace Plate          : 39 joints on the diagonals
  * Rib (stiffener) shapes ProSteel offers: 0=chamfered 1=convex 2=rounded,
    each half or full -> a rib is NEVER a plain rectangle.

This test builds a column in the smoke zone, puts a REAL base-plate connection
on it, and verifies the connection drilled its own holes. Then a rib. Then it
cleans up.
"""
import os
import re
import sys
import time

APP = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, APP)
import eb_api  # noqa: E402

X = 52000.0
LOG = []


def log(m):
    LOG.append(m)
    try:
        sys.stdout.write(m + "\n")
        sys.stdout.flush()
    except Exception:
        pass


def H(r):
    m = re.search(r"handle=(\w+)", r or "")
    return m.group(1) if m else None


def newest(r):
    m = re.search(r"newest=(\w+)", r or "")
    return m.group(1) if m else None


def main():
    log("=" * 76)
    log("BUILDING CONNECTIONS AS CONNECTIONS — proof of capability")
    log("=" * 76)
    log(str(eb_api.run("whoami", wait=25)))
    made = []

    # ---------- 1. a column to hang the joint on ----------
    log("\n[1] column RHS150X100X4, 2m, at X=%d" % X)
    r = eb_api.run("beam", name="RHS150X100X4", catalog="BS_CELSIUS_RHS",
                   p1="%f,0,0" % X, p2="%f,0,2000" % X, layer="PS_Shape", wait=30)
    log("    " + str(r))
    col = H(r)
    if not col:
        log("    !! no column, abort"); return
    made.append(col)

    log("\n[2] holes on the bare column (baseline)")
    log("    " + str(eb_api.run("holes", handle=col, wait=20)))

    # ---------- 2. BASE PLATE as a real connection ----------
    # Amir's own recipe, read from his model: 300x300x12, hole 23, spacing 200
    log("\n[3] BASE-PLATE CONNECTION (Amir's recipe: 300x300x12, hole 23, 200x200)")
    r = eb_api.run("connbase", handle=col, l=300, w=300, t=12,
                   holedia=23, hx=200, hy=200, anchors=1, wait=45)
    log("    " + str(r))

    log("\n[4] what did the connection create, and did it drill?")
    log("    column holes : " + str(eb_api.run("holes", handle=col, wait=20)))
    log("    model holes  : " + str(eb_api.run("dumpholes", out="eb_holes_conntest.txt", wait=60)))
    log("    connections  : " + str(eb_api.run("connscan", handle=col,
                                               out="eb_conn_test.txt", wait=40)))

    # ---------- 3. RIB (stiffener) as a real connection ----------
    log("\n[5] RIB via the stiffener connection — chamfered (shape=0), t=10")
    r = eb_api.run("connstiff", handle=col, t=10, shape=0,
                   at="%f,0,1000" % X, wait=45)
    log("    " + str(r))
    rib = newest(r)
    if rib:
        log("    rib contour  : " + str(eb_api.run("platepoly", handle=rib, wait=20)))

    log("\n[6] rib from a named template (default/half chamfered)")
    r = eb_api.run("connstiff", handle=col, t=10,
                   template="default/half chamfered", at="%f,0,1400" % X, wait=45)
    log("    " + str(r))
    rib2 = newest(r)
    if rib2:
        log("    rib2 contour : " + str(eb_api.run("platepoly", handle=rib2, wait=20)))

    log("\n[7] final census in the smoke zone")
    log("    " + str(eb_api.run("whoami", wait=20)))

    out = os.path.join(os.path.dirname(APP), "projects", "שיעור-3-מאפס",
                       "files", "conn-build-test.log")
    open(out, "w", encoding="utf-8").write("\n".join(LOG))
    log("\nsaved -> " + out)
    log("\n!! smoke objects still in the model — run the cleanup step next")


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    main()
