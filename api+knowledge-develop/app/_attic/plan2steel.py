"""
plan2steel.py - analyze a client plan and prepare the 3D steel model.

This is the "learn & analyze the plan" engine the spec calls the most important
ability. Two inputs:
  - 2D DWG  -> opened in AutoCAD, entities read via COM (lines, polylines, circles,
               text, dimensions) and turned into a structured plan analysis +
               a list of candidate steel members (centerlines with lengths).
  - PDF     -> the agent reads it directly (vision) to recover grids/dimensions.

The member list then feeds modeling: ProSteel "Insert Shape along Line / Automatic
Insertion" (manual B.8.7) places steel on the lines, or seed-and-copy duplicates a
native shape onto each line.

Usage:
    python plan2steel.py analyze "C:/path/plan.dwg"   # analyze a DWG, print report
    from plan2steel import analyze_dwg
"""

import sys
import math


def _dist(a, b):
    return math.dist(a[:2], b[:2]) if len(a) >= 2 else 0.0


def analyze_dwg(path, acad=None):
    """Open a 2D DWG and return a structured analysis of its contents."""
    if acad is None:
        from acad import Acad
        acad = Acad()
    acad.open_drawing(path)
    ents = acad.extract(limit=20000)

    members, circles, texts, dims = [], [], [], []
    by_layer = {}
    for e in ents:
        by_layer[e.get("layer", "?")] = by_layer.get(e.get("layer", "?"), 0) + 1
        t = e.get("type")
        if t == "AcDbLine":
            s, en = e.get("start"), e.get("end")
            if s and en:
                members.append({"layer": e["layer"], "start": s, "end": en,
                                "length": round(_dist(s, en), 1)})
        elif t == "AcDbPolyline":
            pts = e.get("points", [])
            # split polyline into segments as candidate members
            for i in range(0, len(pts) - 3, 2):
                s = (pts[i], pts[i + 1]); en = (pts[i + 2], pts[i + 3])
                members.append({"layer": e["layer"], "start": s, "end": en,
                                "length": round(_dist(s, en), 1)})
        elif t == "AcDbCircle":
            circles.append({"layer": e["layer"], "center": e.get("center"),
                            "radius": e.get("radius")})
        elif t in ("AcDbText", "AcDbMText"):
            texts.append({"text": e.get("text", ""), "at": e.get("at")})
        elif "Dimension" in (t or ""):
            dims.append({"measurement": e.get("measurement"), "text": e.get("text", "")})

    members.sort(key=lambda m: -m["length"])
    return {
        "drawing": path,
        "entities": len(ents),
        "by_layer": by_layer,
        "members_found": len(members),
        "members": members,
        "circles": circles,          # candidate columns / holes / bolt centers
        "texts": texts,              # annotations (profile callouts, grid labels)
        "dimensions": dims,          # explicit dimensions
    }


def report(analysis):
    """Human-readable markdown summary of a plan analysis."""
    a = analysis
    out = [f"# Plan analysis — {a['drawing']}", ""]
    out.append(f"- Entities: **{a['entities']}**  |  candidate members: **{a['members_found']}**")
    out.append(f"- Layers: " + ", ".join(f"{k} ({v})" for k, v in sorted(a["by_layer"].items())))
    out.append(f"- Circles (columns/holes): {len(a['circles'])}  |  "
               f"Texts: {len(a['texts'])}  |  Dimensions: {len(a['dimensions'])}")
    out.append("")
    if a["members"]:
        out.append("## Longest candidate members (by length)")
        out.append("| # | layer | length | start | end |")
        out.append("|---|---|---|---|---|")
        for i, m in enumerate(a["members"][:25], 1):
            out.append(f"| {i} | {m['layer']} | {m['length']} | {m['start']} | {m['end']} |")
    if a["texts"]:
        out.append("")
        out.append("## Annotations on the plan (profile callouts / grid labels)")
        for t in a["texts"][:30]:
            if t["text"].strip():
                out.append(f"- `{t['text'].strip()}`  @ {t['at']}")
    return "\n".join(out)


if __name__ == "__main__":
    if len(sys.argv) >= 3 and sys.argv[1] == "analyze":
        print(report(analyze_dwg(sys.argv[2])))
    else:
        print(__doc__)
