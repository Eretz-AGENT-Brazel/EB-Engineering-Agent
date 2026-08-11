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

A member is JOINED if it carries bolt holes, or is a declared welded part. A member that carries
neither is FLOATING, and a floating main member is a modelling defect no bolt check will report.

⚠️ A welded joint is legitimate -- B.23 established that welds are not creatable from code, so an
end plate welded to a rafter genuinely cannot be bolted. But then it must be DECLARED, part by
part, out loud. Silence is what produced the E.4 frame.

    python app/vfy_joined.py <xmin> <xmax> [--welded H1,H2,...]
"""
import sys
import collections

sys.path.insert(0, __file__.rsplit("\\", 1)[0])
import eb_api as E                                                   # noqa: E402


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

    import re
    rows, floating = [], []
    for e in sorted(members, key=lambda e: e["handle"]):
        r = E.run("holes", handle=e["handle"], wait=45)
        m = re.search(r"count=(\d+)", r)
        n = int(m.group(1)) if m else -1
        name = e.get("profile") or e.get("name") or e["kind"]
        state = "bolted" if n > 0 else ("WELDED (declared)" if e["handle"].upper() in welded
                                        else "*** FLOATING ***")
        rows.append((e["handle"], e["kind"], name, n, state))
        if n == 0 and e["handle"].upper() not in welded:
            floating.append((e["handle"], name))

    print("=== JOINT AUDIT  x %d .. %d ===" % (xmin, xmax))
    print("    %d members, %d bolts" % (len(members), len(bolts)))
    print()
    for h, k, name, n, state in rows:
        print("    %-5s %-6s %-22s holes=%-3d %s" % (h, k, name, n, state))
    print()
    if floating:
        print("*** %d FLOATING MEMBER(S) -- joined to nothing, and no bolt check will say so ***"
              % len(floating))
        for h, name in floating:
            print("      %-5s %s" % (h, name))
        print()
        print("    Either bolt it, or declare the weld explicitly with --welded.")
    else:
        print("    JOINT AUDIT CLEAN -- every member is bolted or declared welded.")
    return floating


if __name__ == "__main__":
    if len(sys.argv) < 3:
        print(__doc__)
        sys.exit(2)
    w = ()
    if "--welded" in sys.argv:
        w = sys.argv[sys.argv.index("--welded") + 1].split(",")
    sys.exit(1 if audit(float(sys.argv[1]), float(sys.argv[2]), w) else 0)
