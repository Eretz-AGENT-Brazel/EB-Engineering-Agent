# -*- coding: utf-8 -*-
"""vfy_joined -- THE JOINT AUDIT. Is every member actually IN a joint?

⚡ WHY THIS EXISTS. On 11/08/2026 E.4 was committed with a portal frame that looked right in the
viewport and reported `vfy_fit bolts=196 OK=196 BOLT-NO-HOLE=0`. Amir opened it and asked whether it
was a sensible connection. It was not: BOTH RAFTERS HAD ZERO HOLES. Every bolt joined a plate to a
column, or a plate to a plate. The plates merely touched the rafter ends. It was not a frame -- it
was two columns with plates bolted on and two rafters resting nearby.

⚠️ AND vfy_fit COULD NOT SEE IT. vfy_fit verifies each BOLT against the parts it is LINKED to. It
cannot see a member that was never in the joint at all. A green check stood in for looking.

So this is the complementary question, asked from the member's side instead of the bolt's:

    for every structural member in a band -- what joins it to anything?

⛔⛔ CORRECTED 11/08/2026 (D.5). THIS FILE USED TO SAY:

    "A member is JOINED if it carries bolt holes, or is a declared welded part."

    THAT DEFINITION WAS WRONG, and it was wrong in the docstring as well as in the code -- so it
    was a mistake of reasoning, not a slip. CARRYING HOLES IS NOT BEING BOLTED. A member that was
    drilled and never bolted is precisely the defect B.20 and B.21 each found the hard way:

        B.20  `dia=22` on a shear plate -> plates made, holes drilled, BOLTS SILENTLY DROPPED
        B.21  both splice templates ship `BoltStyleCRC = 0` -> drilled, and empty

    Both of those would have passed this audit as "bolted".

    Measured, in the D.05 band on 11/08/2026: fourteen plates on which `boltparts` REFUSED, each
    carrying 4 real holes and ZERO bolts, were every one reported "bolted" by the old test. The
    guard written to stop a green check standing in for looking was itself a green check standing
    in for looking.

A member is now JOINED only if a bolt ACTUALLY PASSES THROUGH one of its holes -- matched
geometrically, hole midpoint against bolt body, the same way vfy_fit matches but from the other
end. Three failure states instead of one:

    *** FLOATING ***        no holes at all, not declared welded          (the E.4 rafter)
    *** DRILLED-NOT-BOLTED  holes, and not one of them carries a bolt     (the B.20 / B.21 defect)
    WELDED (declared)       legitimate, and the declaration is the price  (B.23: welds need a mouse)

    python app/vfy_joined.py <xmin> <xmax> [--welded H1,H2,...]
"""
import os
import re
import sys

sys.path.insert(0, __file__.rsplit("\\", 1)[0])
import eb_api as E                                                   # noqa: E402

HOLES_FILE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "plugin", "eb_holes.txt")

# a hole midpoint must fall inside the bolt's own body for the bolt to be "through" it.
# 2 mm covers the read-back rounding without letting a neighbouring bolt row claim the hole.
TOL = 2.0


def _hole_midpoints(handle):
    """Every hole on one part, as (midpoint, diameter) -- with SLOTS COLLAPSED.

    ⛔⛔ 13/08/2026, lesson 6 stage 2. This function reported **zero filled holes on a
    perfectly correct joint**, and the audit therefore printed DRILLED-NOT-BOLTED on
    Amir's slotted end plates. Measured cause, both halves:

      * a `Ks_Bolt`'s bounding box is **DEGENERATE** -- `lo == hi` in the two transverse
        directions (`lo=[9567.265, 408.585, 15.0] hi=[9617.265, 408.585, 15.0]`). The box
        IS the bolt axis, so `_inside` is really "is this point within TOL of the axis".
      * `PsSingleHoleArray` in `kDoubleHole` mode reports a **SLOT as its two END CIRCLES**.
        Each end sits half the slot's travel off the centre line -- 7.5 mm here -- so
        7.5 > TOL and neither end matched. The bolt ran exactly down the slot's centre.

    ⇒ Slots are paired here and represented by the **pair midpoint**, which is where a
    bolt actually passes. A slot counts as ONE hole, not two. Never widen TOL to paper
    over this: TOL is what stops a neighbouring bolt row claiming a hole.
    """
    r = E.run("holes", handle=handle, wait=45)
    m = re.search(r"count=(\d+)", r)
    n = int(m.group(1)) if m else -1
    plain, slots = [], []
    if n > 0 and os.path.exists(HOLES_FILE):
        for line in open(HOLES_FILE, encoding="utf-8-sig").read().splitlines():
            f = line.split("\t")
            if len(f) < 6 or f[0] != "HOLE" or f[1].upper() != handle.upper():
                continue
            try:
                a = [float(v) for v in f[3].split(",")]
                b = [float(v) for v in f[4].split(",")]
                mid = [(a[i] + b[i]) / 2.0 for i in range(3)]
                dia = float(f[5])
                axis = [b[i] - a[i] for i in range(3)]
                is_slot = f[-1].strip() in ("1", "True", "true")
                (slots if is_slot else plain).append((mid, dia, axis))
            except Exception:
                pass

    pts = [(mid, dia) for mid, dia, _ax in plain]
    nslots = 0
    used = set()
    for i in range(len(slots)):
        if i in used:
            continue
        mi, di, ai = slots[i]
        best, bestd = None, None
        for j in range(i + 1, len(slots)):
            if j in used:
                continue
            mj, dj, aj = slots[j]
            if abs(dj - di) > 0.51:
                continue
            # the two ends of one slot share the hole axis and differ in exactly one
            # transverse direction; the partner is simply the nearest such candidate
            if sum(1 for k in range(3) if abs(mj[k] - mi[k]) > 0.01) != 1:
                continue
            d = sum((mj[k] - mi[k]) ** 2 for k in range(3)) ** 0.5
            if bestd is None or d < bestd:
                best, bestd = j, d
        if best is None:
            pts.append((mi, di))          # an unpaired slot end: treat it as a hole
        else:
            mj = slots[best][0]
            pts.append(([(mi[k] + mj[k]) / 2.0 for k in range(3)], di))
            used.add(best)
            nslots += 1
        used.add(i)
    return len(pts), pts, nslots


def _inside(pt, bb, tol=TOL):
    lo, hi = bb
    return all(lo[i] - tol <= pt[i] <= hi[i] + tol for i in range(3))


def audit(xmin, xmax, welded=()):
    _, els = E.dumpmodel()
    welded = set(w.strip().upper() for w in welded if w.strip())

    def cx(e):
        if e["kind"] == "shape":
            return (e["p1"][0] + e["p2"][0]) / 2.0
        c = e.get("center")
        return c[0] if c else None

    band = [e for e in els if cx(e) is not None and xmin < cx(e) < xmax]
    members = [e for e in band if e["kind"] in ("shape", "plate")]
    bolts = [e for e in band if e["kind"] == "bolt"]
    boltboxes = [b["bbox"] for b in bolts if b.get("bbox")]

    rows, floating, drilled = [], [], []
    for e in sorted(members, key=lambda e: e["handle"]):
        n, pts, nslots = _hole_midpoints(e["handle"])
        name = e.get("profile") or e.get("name") or e["kind"]
        if nslots:
            name = (name + " [%dslot]" % nslots)[:22]
        filled = sum(1 for p, _d in pts if any(_inside(p, bb) for bb in boltboxes))

        if n > 0 and filled > 0:
            state = "bolted"
        elif n > 0:
            state = "*** DRILLED-NOT-BOLTED ***"
            drilled.append((e["handle"], name, n))
        elif e["handle"].strip().upper() in welded:   # strip: an unstripped handle once read as FLOATING while declared
            state = "WELDED (declared)"
        else:
            state = "*** FLOATING ***"
            floating.append((e["handle"], name))

        rows.append((e["handle"], e["kind"], name, n, filled, state))

    print("=== JOINT AUDIT  x %d .. %d ===" % (xmin, xmax))
    print("    %d members, %d bolts" % (len(members), len(bolts)))
    print()
    for h, k, name, n, filled, state in rows:
        print("    %-5s %-6s %-22s holes=%-3d filled=%-3d %s" % (h, k, name, n, filled, state))
    print()
    if floating:
        print("*** %d FLOATING MEMBER(S) -- joined to nothing, and no bolt check will say so ***"
              % len(floating))
        for h, name in floating:
            print("      %-5s %s" % (h, name))
        print("    Either bolt it, or declare the weld explicitly with --welded.")
        print()
    if drilled:
        print("*** %d DRILLED-NOT-BOLTED MEMBER(S) -- holes cut, nothing through them ***"
              % len(drilled))
        for h, name, n in drilled:
            print("      %-5s %-22s %d hole(s), 0 filled" % (h, name, n))
        print("    This is B.20's dropped bolt and B.21's empty splice. The steel is cut and the")
        print("    joint does not exist. Check the bolt style against the HOLE diameter (D.5).")
        print()
    if not floating and not drilled:
        print("    JOINT AUDIT CLEAN -- every member has a bolt through it, or is declared welded.")
    return floating + drilled


if __name__ == "__main__":
    if len(sys.argv) < 3:
        print(__doc__)
        sys.exit(2)
    w = ()
    if "--welded" in sys.argv:
        w = sys.argv[sys.argv.index("--welded") + 1].split(",")
    sys.exit(1 if audit(float(sys.argv[1]), float(sys.argv[2]), w) else 0)
