# -*- coding: utf-8 -*-
"""
audit.py -- the geometric gate. Checks RELATIONSHIPS, not counts.

Why this exists
---------------
Every failure of lessons 2-5 passed a count check and failed reality:
  "24 holes"     while the pattern was rotated 90 deg
  "18/18 placed" while every nut floated 10 mm off the plate
  "304/304 bolts" while the model contained ZERO holes
  "3.0 min, rc=0 on all five scripts" while 76 anchors were duplicated
A correct count is not evidence. A correct relationship is.

Design rules
------------
1. ONE schema-aware reader. The 23 ad-hoc tab-split parsers in the exam scripts each
   invented their own field indices; three of them disagreed. There is one here.
2. Every finding carries the measured number and the handle, so it can be checked by hand.
3. The checker is validated against a KNOWN-BAD fixture whose defects were measured
   independently (BASELINE-מבחן-5.dwg: 76 duplicate anchors, 19 parts with 4 holes
   instead of 6). A checker that cannot find a defect you already know about is worthless.
"""

import os
import re
import math
import collections

APP = os.path.dirname(os.path.abspath(__file__))
PLUG = os.path.join(APP, "plugin")


# --------------------------------------------------------------------------
# ONE reader. Dump files carry a BOM -> utf-8-sig, or row 1 vanishes silently.
# --------------------------------------------------------------------------
def _rows(path):
    if not os.path.isabs(path):
        path = os.path.join(PLUG, path)
    with open(path, encoding="utf-8-sig", errors="replace") as fh:
        for line in fh:
            line = line.rstrip("\n")
            if line:
                yield line.split("\t")


def _xyz(s):
    """'12.5,0,3000' -> (12.5, 0.0, 3000.0); returns None if unparseable."""
    if not s or not s.strip():
        return None
    try:
        p = [float(v) for v in s.split(",")]
        while len(p) < 3:
            p.append(0.0)
        return tuple(p[:3])
    except Exception:
        return None


def _ext(s):
    """'min;max' -> ((x,y,z),(x,y,z))"""
    if not s or ";" not in s:
        return None
    a, b = s.split(";", 1)
    lo, hi = _xyz(a), _xyz(b)
    return (lo, hi) if lo and hi else None


def _ecs(s):
    """'1/0/0;0/1/0;0/0/1' -> (X, Y, Z) unit vectors"""
    if not s or ";" not in s:
        return None
    try:
        axes = []
        for part in s.split(";"):
            v = [float(x) for x in part.split("/")]
            axes.append(tuple(v[:3]))
        return tuple(axes) if len(axes) == 3 else None
    except Exception:
        return None


class Part(object):
    __slots__ = ("kind", "handle", "name", "cls", "layer", "p1", "p2",
                 "centre", "extents", "ecs", "length", "dims")

    def __repr__(self):
        return "<%s %s %s>" % (self.kind, self.handle, self.name or self.cls)

    @property
    def box_centre(self):
        if self.extents:
            lo, hi = self.extents
            return tuple((lo[i] + hi[i]) / 2.0 for i in range(3))
        return self.centre

    @property
    def size(self):
        if self.extents:
            lo, hi = self.extents
            return tuple(hi[i] - lo[i] for i in range(3))
        return None


def read_model(dump="eb_full.txt"):
    """Parse a dumpfull2 file into Part objects. Schema (v32+):

      SHAPE  h name catalog p1 p2 len ? descr ? offs ? ? ecs ? layer extents ? class
      PLATE  h centre dims ecs layer class
      BOLT   h centre dims ecs layer class
      OTHER  h class layer centre extents ecs        <- v32 added the last three
    """
    parts = []
    for r in _rows(dump):
        k = r[0]
        p = Part()
        p.kind = k
        p.handle = r[1] if len(r) > 1 else ""
        p.name = p.cls = p.layer = ""
        p.p1 = p.p2 = p.centre = p.extents = p.ecs = None
        p.length = None
        p.dims = None
        if k == "SHAPE" and len(r) >= 19:
            p.name, p.p1, p.p2 = r[2], _xyz(r[4]), _xyz(r[5])
            try:
                p.length = float(r[6])
            except Exception:
                pass
            p.ecs = _ecs(r[13])
            p.layer = r[15]
            p.extents = _ext(r[16])
            p.cls = r[18]
        elif k in ("PLATE", "BOLT") and len(r) >= 7:
            p.centre, p.dims, p.ecs, p.layer, p.cls = _xyz(r[2]), _xyz(r[3]), _ecs(r[4]), r[5], r[6]
        elif k == "OTHER" and len(r) >= 7:
            p.cls, p.layer, p.centre, p.extents, p.ecs = r[2], r[3], _xyz(r[4]), _ext(r[5]), _ecs(r[6])
        elif k == "OTHER":
            p.cls = r[2] if len(r) > 2 else ""
            p.layer = r[3] if len(r) > 3 else ""
        else:
            continue
        parts.append(p)
    return parts


def read_holes(dump="eb_holes_all.txt"):
    """-> {part_handle: [ {start, end, dia, maxlen, slotted} ]}

    TWO layouts exist and they differ by three columns. Getting this wrong made the
    bolt-in-hole check report "31 of 31 bolts are not in any hole" on a model whose
    bolts sat exactly in their holes -- the checker was wrong, not the model. That is
    the same defect as the 23 disagreeing parsers in the exam scripts, reproduced.
    So: locate the coordinate columns by PARSING, never by a hard-coded index.

      whole-model (op=dumpholes): HOLE h class layer idx start end dia maxlen slot
      single-part (op=holes)    : HOLE h idx start end dia maxlen slot
    """
    holes = collections.defaultdict(list)
    for r in _rows(dump):
        if r[0] != "HOLE" or len(r) < 5:
            continue
        # the first field that parses as an x,y,z triple is 'start'; 'end' follows it
        si = None
        for i in range(2, len(r) - 1):
            if _xyz(r[i]) and "," in r[i] and _xyz(r[i + 1]) and "," in r[i + 1]:
                si = i
                break
        if si is None:
            continue
        h = {"part": r[1], "start": _xyz(r[si]), "end": _xyz(r[si + 1])}
        for off, key in ((2, "dia"), (3, "maxlen")):
            try:
                h[key] = float(r[si + off])
            except Exception:
                h[key] = None
        h["slotted"] = (len(r) > si + 4 and r[si + 4] not in ("", "0", "False", "false", "?"))
        holes[r[1]].append(h)
    return dict(holes)


# --------------------------------------------------------------------------
# Findings
# --------------------------------------------------------------------------
class Finding(object):
    def __init__(self, check, severity, msg, handles=None, measured=None):
        self.check, self.severity, self.msg = check, severity, msg
        self.handles = handles or []
        self.measured = measured

    def __str__(self):
        h = ("  [" + ",".join(self.handles[:6]) + ("..." if len(self.handles) > 6 else "") + "]") \
            if self.handles else ""
        return "%-4s %-22s %s%s" % (self.severity, self.check, self.msg, h)


# --------------------------------------------------------------------------
# CHECK 1 -- duplicates.  The 76-anchor bug (05/08) and the 754-anchor bug.
# Exact AND near: same XY, different Z was the connection-rerun signature.
# --------------------------------------------------------------------------
def check_duplicates(parts, tol=1.0, classes=None):
    out = []
    buckets = collections.defaultdict(list)
    for p in parts:
        c = p.box_centre
        if not c:
            continue
        if classes and p.cls not in classes:
            continue
        key = (p.cls, round(c[0] / tol), round(c[1] / tol), round(c[2] / tol))
        buckets[key].append(p)
    extra = 0
    for key, group in sorted(buckets.items()):
        if len(group) > 1:
            extra += len(group) - 1
            out.append(Finding("duplicates", "FAIL",
                               "%d objects of class %s share position (%.0f,%.0f,%.0f)"
                               % (len(group), key[0], key[1] * tol, key[2] * tol, key[3] * tol),
                               [g.handle for g in group]))
    if extra:
        out.append(Finding("duplicates", "FAIL",
                           "TOTAL %d surplus objects at %d shared positions"
                           % (extra, len([1 for g in buckets.values() if len(g) > 1])),
                           measured=extra))
    return out


# --------------------------------------------------------------------------
# CHECK 2 -- hole-count uniformity across a family.
# The class of fault a count hides completely: "479 right, 1 wrong" reads as 480.
# This is the check that catches the 19 plates with 4 holes instead of 6.
# --------------------------------------------------------------------------
def check_hole_uniformity(parts, holes, min_family=3, expect_holes_per_part=None):
    """Identical parts must be drilled identically.

    THE MAJORITY IS NOT AUTHORITY. Validated against BASELINE-מבחן-5: 19 of 20 floor
    plates carried 4 holes and 1 carried the correct 6, because a replicated connection
    deleted two hand-drilled holes on every copy. A first version of this check reported
    "1 part differs from the majority" -- pointing at the only CORRECT part. So the check
    reports the SPLIT and never crowns a winner; if the caller declares the expected
    count, that decides which side is wrong.
    """
    out = []
    by_family = collections.defaultdict(list)
    for p in parts:
        sz = p.size
        key = (p.kind, p.cls, p.name) if p.kind == "SHAPE" else (p.kind, p.cls)
        if sz:
            key = key + tuple(round(v) for v in sorted(sz))
        by_family[key].append(p)

    for key, group in sorted(by_family.items(), key=lambda kv: -len(kv[1])):
        if len(group) < min_family:
            continue
        counts = collections.Counter(len(holes.get(p.handle, [])) for p in group)
        if len(counts) <= 1:
            continue
        want = None
        if expect_holes_per_part:
            want = expect_holes_per_part.get(key[2] if len(key) > 2 else None)
        if want is not None:
            wrong = [p for p in group if len(holes.get(p.handle, [])) != want]
            out.append(Finding("hole_uniformity", "FAIL",
                               "family %s: %d parts NOT drilled to the declared %d holes "
                               "(actual spread %s)"
                               % (str(key[:3]), len(wrong), want, dict(counts)),
                               [p.handle for p in wrong], measured=len(wrong)))
        else:
            spread = ", ".join("%d parts have %d holes" % (n, c)
                               for c, n in sorted(counts.items(), key=lambda kv: -kv[1]))
            out.append(Finding("hole_uniformity", "FAIL",
                               "family %s is NOT uniform: %d identical parts drilled %d "
                               "different ways -- %s. Which is right is NOT decided by "
                               "the majority; declare the expected count."
                               % (str(key[:3]), len(group), len(counts), spread),
                               [p.handle for p in group], measured=len(counts)))
    return out


# --------------------------------------------------------------------------
# CHECK 3 -- nothing floats.  The column 180 mm in the air, the nut 10 mm off.
# For each part, is there ANY other part whose box it touches or overlaps?
# --------------------------------------------------------------------------
def _gap(a, b):
    """Separation between two boxes: 0 if they touch/overlap, else the distance."""
    if not a or not b:
        return None
    d = 0.0
    for i in range(3):
        lo = b[0][i] - a[1][i]
        hi = a[0][i] - b[1][i]
        s = max(lo, hi, 0.0)
        d += s * s
    return math.sqrt(d)


def check_no_orphans(parts, tol=1.0, ignore_classes=("PcRebarManager",)):
    out = []
    boxed = [p for p in parts if p.extents and p.cls not in ignore_classes]
    for p in boxed:
        best = None
        for q in boxed:
            if q is p:
                continue
            g = _gap(p.extents, q.extents)
            if g is None:
                continue
            if best is None or g < best[0]:
                best = (g, q)
            if g <= tol:
                break
        if best and best[0] > tol:
            out.append(Finding("no_orphans", "WARN",
                               "%s %s touches nothing -- nearest is %s at %.1f mm"
                               % (p.cls, p.handle, best[1].handle, best[0]),
                               [p.handle], measured=round(best[0], 1)))
    return out


# --------------------------------------------------------------------------
# CHECK 4 -- every bolt passes through a real hole.
# The measured dead end: CreateSingleBolt + AddObject(host) drills NOTHING, yet
# "304/304 bolts" was reported on a model with zero holes.
# --------------------------------------------------------------------------
def check_bolts_through_holes(parts, holes, tol=3.0):
    out = []
    bolts = [p for p in parts if p.kind == "BOLT" or (p.layer or "").upper().startswith("PS_BOLT")]
    if not bolts:
        return out
    all_holes = [(ph, h) for ph, lst in holes.items() for h in lst]
    if not all_holes:
        out.append(Finding("bolt_in_hole", "FAIL",
                           "%d bolt-like objects exist and the model has NO holes at all"
                           % len(bolts), measured=len(bolts)))
        return out
    naked = []
    for b in bolts:
        c = b.box_centre
        if not c:
            continue
        hit = False
        for ph, h in all_holes:
            s, e = h.get("start"), h.get("end")
            if not s or not e:
                continue
            mid = tuple((s[i] + e[i]) / 2.0 for i in range(3))
            if all(abs(c[i] - mid[i]) <= max(tol, abs(e[i] - s[i]) / 2.0 + tol) for i in range(3)):
                hit = True
                break
        if not hit:
            naked.append(b.handle)
    if naked:
        out.append(Finding("bolt_in_hole", "FAIL",
                           "%d of %d bolts do not line up with any modelled hole"
                           % (len(naked), len(bolts)), naked, measured=len(naked)))
    return out



# --------------------------------------------------------------------------
# CHECK 6 -- a hole must pass THROUGH its part, not stop inside it.
# MEASURED 06/08/2026 on an SHS 200x200x8:
#   without SetIgnoreInnerContour the hole ran z=100 -> 92, i.e. 8 mm = ONE WALL.
#   with it, z=100 -> -100, i.e. 200 mm = both walls.
# The hole COUNT is identical either way, so counting can never catch this. It is
# invisible until a bolt is fitted on site.
# --------------------------------------------------------------------------
def check_holes_pass_through(parts, holes, min_ratio=0.9):
    """A hole in a HOLLOW section must cross both walls.

    MEASURED 06/08/2026 on an SHS 200x200x8:
      without SetIgnoreInnerContour the hole ran z=100 -> 92  = 8 mm  = ONE WALL
      with it,                       the hole ran z=100 -> -100 = 200 mm = both walls
    The hole COUNT is identical either way, so counting can never catch it.

    SCOPE, and why it is narrow: a first version compared hole depth against the part's
    bounding box for EVERY part, and flagged an IPE300 drilled through its 7.1 mm web as
    a fault -- a false positive, because a web hole SHOULD be web-thick. Bounding-box
    depth is only a valid expectation for a CLOSED section, where a through hole must
    cross the whole box. So this check applies to hollow sections only, recognised by
    profile name, and says so rather than guessing at open sections.
    """
    out = []
    hollow = re.compile(r"(RRO|RHS|SHS|QRO|CHS|RO|ROR|HOL|TUBE|RHP)", re.I)
    by_handle = dict((p.handle, p) for p in parts)
    for h, lst in holes.items():
        p = by_handle.get(h)
        if not p or not p.size or not p.name:
            continue
        if not hollow.search(p.name):
            continue                      # open sections: no defensible expectation here
        for i, x in enumerate(lst):
            s0, e0 = x.get("start"), x.get("end")
            if not s0 or not e0:
                continue
            depth = math.sqrt(sum((e0[k] - s0[k]) ** 2 for k in range(3)))
            axis = [abs(e0[k] - s0[k]) for k in range(3)]
            m = axis.index(max(axis)) if max(axis) > 0 else 2
            span = p.size[m]
            if span <= 0:
                continue
            if depth < span * min_ratio:
                out.append(Finding("hole_through", "FAIL",
                                   "hollow section %s (%s) hole %d is only %.1f mm deep but the "
                                   "section spans %.1f mm on that axis -- drilled on ONE WALL "
                                   "(SetIgnoreInnerContour was not set)"
                                   % (h, p.name, i, depth, span),
                                   [h], measured=round(depth, 1)))
    return out


# --------------------------------------------------------------------------
# CHECK 5 -- expectations declared up front (the detail card, in its simplest form)
# --------------------------------------------------------------------------
def check_expected(parts, holes, expect):
    out = []
    got = {
        "shapes": len([p for p in parts if p.kind == "SHAPE"]),
        "plates": len([p for p in parts if p.kind == "PLATE"]),
        "bolts":  len([p for p in parts if p.kind == "BOLT"]),
        "other":  len([p for p in parts if p.kind == "OTHER"]),
        "holes":  sum(len(v) for v in holes.values()),
        "parts_with_holes": len(holes),
    }
    for k, want in (expect or {}).items():
        have = got.get(k)
        if have is None:
            continue
        if have != want:
            out.append(Finding("expected", "FAIL",
                               "%s = %d, expected %d (%+d)" % (k, have, want, have - want),
                               measured=have))
    return out, got


# --------------------------------------------------------------------------
def run(dump="eb_full.txt", holes_dump="eb_holes_all.txt", expect=None,
        checks=("duplicates", "hole_uniformity", "bolts", "through", "orphans"), verbose=True):
    parts = read_model(dump)
    holes = read_holes(holes_dump)
    findings = []
    if expect:
        f, got = check_expected(parts, holes, expect)
        findings += f
    else:
        _, got = check_expected(parts, holes, None)
    if "duplicates" in checks:
        findings += check_duplicates(parts)
    if "hole_uniformity" in checks:
        findings += check_hole_uniformity(parts, holes)
    if "bolts" in checks:
        findings += check_bolts_through_holes(parts, holes)
    if "through" in checks:
        findings += check_holes_pass_through(parts, holes)
    if "orphans" in checks and len(parts) <= 800:      # O(n^2); skip on big models
        findings += check_no_orphans(parts)

    if verbose:
        print("MODEL: " + " ".join("%s=%s" % (k, v) for k, v in sorted(got.items())))
        fails = [f for f in findings if f.severity == "FAIL"]
        warns = [f for f in findings if f.severity == "WARN"]
        print("GATE : %d FAIL, %d WARN" % (len(fails), len(warns)))
        for f in fails[:40]:
            print("  " + str(f))
        for f in warns[:15]:
            print("  " + str(f))
        if len(warns) > 15:
            print("  ... %d more warnings" % (len(warns) - 15))
    return findings, got


if __name__ == "__main__":
    import sys
    d = sys.argv[1] if len(sys.argv) > 1 else "eb_full.txt"
    h = sys.argv[2] if len(sys.argv) > 2 else "eb_holes_all.txt"
    run(d, h)
