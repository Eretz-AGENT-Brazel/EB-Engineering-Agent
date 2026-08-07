"""
acad.py - AutoCAD / ProSteel control bridge for the Eretz Barzel AI agent.

This is the "hands" of the agent. Claude (the brain) calls these functions to
drive the AutoCAD 2015 instance that is running on this PC, via Windows COM.

Two ways to use it:
  1. As a library:  from acad import Acad ; a = Acad() ; a.circle(0,0,1500)
  2. From the shell: python acad.py circle 0 0 1500
                     python acad.py send "ZOOM E "
                     python acad.py info

Everything here is free (Python + pywin32). No API key, no internet needed.
"""

import sys
import math
import pythoncom
import win32com.client


# ---- low level helpers -------------------------------------------------------

def _pt(x, y, z=0.0):
    """A 3D point as the VARIANT array of doubles that AutoCAD COM expects."""
    return win32com.client.VARIANT(
        pythoncom.VT_ARRAY | pythoncom.VT_R8, [float(x), float(y), float(z)]
    )


def _dbls(values):
    """A flat VARIANT array of doubles (for polylines etc.)."""
    return win32com.client.VARIANT(
        pythoncom.VT_ARRAY | pythoncom.VT_R8, [float(v) for v in values]
    )


# ---- the bridge --------------------------------------------------------------

class Acad:
    """Thin, friendly wrapper around the AutoCAD COM automation API."""

    def __init__(self, launch_if_needed=False):
        try:
            self.app = win32com.client.GetActiveObject("AutoCAD.Application")
        except Exception:
            if not launch_if_needed:
                raise RuntimeError(
                    "AutoCAD is not running. Open AutoCAD 2015 first, "
                    "or call with launch_if_needed=True."
                )
            self.app = win32com.client.Dispatch("AutoCAD.Application")
            self.app.Visible = True
        self.doc = self.app.ActiveDocument
        self.ms = self.doc.ModelSpace

    # --- info / housekeeping ---------------------------------------------

    # --- documents (switching between models) ----------------------------

    def new_drawing(self):
        self.doc = self.app.Documents.Add()
        self.ms = self.doc.ModelSpace
        return f"new drawing: {self.doc.Name}"

    def open_drawing(self, path):
        self.doc = self.app.Documents.Open(path)
        self.ms = self.doc.ModelSpace
        return f"opened: {self.doc.Name}"

    def erase_layer(self, name):
        """Delete every entity on a given layer (used to clean up tests)."""
        n = 0
        for ent in list(self.ms):
            try:
                if ent.Layer == name:
                    ent.Delete()
                    n += 1
            except Exception:
                pass
        return f"erased {n} entities on layer {name!r}"

    def info(self):
        return {
            "application": self.app.Name,
            "version": self.app.Version,
            "drawing": self.doc.Name,
            "path": self.doc.FullName or "(unsaved)",
            "entities_in_modelspace": self.ms.Count,
        }

    def send(self, command):
        """Send a raw AutoCAD command-line string. This is the universal tool:
        it can run ANY AutoCAD command, any AutoLISP, and any ProSteel command.
        Remember a trailing space or \\n acts as Enter, e.g. 'ZOOM E '."""
        self.doc.SendCommand(command)
        return f"sent: {command!r}"

    def zoom_extents(self):
        self.app.ZoomExtents()
        return "zoomed to extents"

    def regen(self):
        self.doc.Regen(1)  # 1 = acActiveViewport
        return "regenerated"

    def count(self):
        return self.ms.Count

    # --- drawing primitives (millimetres, current UCS) -------------------

    def line(self, x1, y1, x2, y2, z=0.0):
        ent = self.ms.AddLine(_pt(x1, y1, z), _pt(x2, y2, z))
        return f"line ({x1},{y1}) -> ({x2},{y2})  handle={ent.Handle}"

    def circle(self, cx, cy, radius, z=0.0):
        ent = self.ms.AddCircle(_pt(cx, cy, z), float(radius))
        return f"circle center=({cx},{cy}) r={radius}  handle={ent.Handle}"

    def arc(self, cx, cy, radius, start_deg, end_deg, z=0.0):
        ent = self.ms.AddArc(
            _pt(cx, cy, z), float(radius),
            math.radians(start_deg), math.radians(end_deg),
        )
        return f"arc center=({cx},{cy}) r={radius} {start_deg}->{end_deg}deg  handle={ent.Handle}"

    def rectangle(self, x1, y1, x2, y2, z=0.0):
        """Closed lightweight polyline rectangle from two opposite corners."""
        pts = [x1, y1, x2, y1, x2, y2, x1, y2]
        pl = self.ms.AddLightWeightPolyline(_dbls(pts))
        pl.Closed = True
        return f"rectangle ({x1},{y1})-({x2},{y2})  handle={pl.Handle}"

    def polyline(self, coords, closed=False, z=0.0):
        """coords = flat list [x1,y1, x2,y2, ...] in the XY plane."""
        pl = self.ms.AddLightWeightPolyline(_dbls(coords))
        pl.Closed = bool(closed)
        return f"polyline {len(coords)//2} pts closed={closed}  handle={pl.Handle}"

    def text(self, x, y, content, height=100.0, z=0.0):
        ent = self.ms.AddText(str(content), _pt(x, y, z), float(height))
        return f"text {content!r} at ({x},{y}) h={height}  handle={ent.Handle}"

    # --- 3D solids (for turning plans into a live 3D model) --------------

    def cylinder(self, cx, cy, cz, radius, height):
        """Vertical cylinder (axis along Z) centered at (cx,cy,cz).
        A tank shell = one cylinder. Height grows symmetrically about cz."""
        ent = self.ms.AddCylinder(_pt(cx, cy, cz), float(radius), float(height))
        return f"cylinder c=({cx},{cy},{cz}) r={radius} h={height}  handle={ent.Handle}"

    def box(self, cx, cy, cz, length, width, height):
        ent = self.ms.AddBox(_pt(cx, cy, cz), float(length), float(width), float(height))
        return f"box c=({cx},{cy},{cz}) {length}x{width}x{height}  handle={ent.Handle}"

    def cone(self, cx, cy, cz, base_radius, height):
        ent = self.ms.AddCone(_pt(cx, cy, cz), float(base_radius), float(height))
        return f"cone c=({cx},{cy},{cz}) r={base_radius} h={height}  handle={ent.Handle}"

    def sphere(self, cx, cy, cz, radius):
        ent = self.ms.AddSphere(_pt(cx, cy, cz), float(radius))
        return f"sphere c=({cx},{cy},{cz}) r={radius}  handle={ent.Handle}"

    def ibeam(self, x0, y0, z0, length, h=500.0, b=300.0, tw=14.5, tf=28.0, axis="x"):
        """Build an I-section steel beam (e.g. HEB) as a unified 3D solid.
        Reference point (x0,y0,z0) = start of the beam, at the BOTTOM-CENTER of
        the section. Beam runs `length` along `axis` ('x' or 'y').
        Default dims are HEB 500 (h=500,b=300,tw=14.5,tf=28), all in mm."""
        h, b, tw, tf, length = float(h), float(b), float(tw), float(tf), float(length)
        x0, y0, z0 = float(x0), float(y0), float(z0)
        web_h = h - 2 * tf
        if axis == "x":
            cx = x0 + length / 2.0
            bf = self.ms.AddBox(_pt(cx, y0, z0 + tf / 2), length, b, tf)
            tfb = self.ms.AddBox(_pt(cx, y0, z0 + h - tf / 2), length, b, tf)
            web = self.ms.AddBox(_pt(cx, y0, z0 + tf + web_h / 2), length, tw, web_h)
        else:  # axis == "y"
            cy = y0 + length / 2.0
            bf = self.ms.AddBox(_pt(x0, cy, z0 + tf / 2), b, length, tf)
            tfb = self.ms.AddBox(_pt(x0, cy, z0 + h - tf / 2), b, length, tf)
            web = self.ms.AddBox(_pt(x0, cy, z0 + tf + web_h / 2), tw, length, web_h)
        try:
            web.Boolean(0, bf)   # 0 = acUnion
            web.Boolean(0, tfb)
            handle = web.Handle
        except Exception:
            handle = "(3 separate boxes - union failed)"
        return (f"I-beam {axis}-axis len={length} h={h} b={b} tw={tw} tf={tf} "
                f"at ({x0},{y0},{z0})  handle={handle}")

    def view_iso(self):
        """Switch to SE isometric 3D view (good for checking a 3D model)."""
        self.doc.SendCommand("-VIEW SEISO \n")
        self.app.ZoomExtents()
        return "set SE isometric view"

    # --- reading / analyzing a drawing (for uploaded DWG plans) ----------

    def extract(self, limit=1000):
        """Enumerate model-space entities so the agent can UNDERSTAND a drawing
        (e.g. a DWG plan you received). Returns a list of dicts."""
        out = []
        for i, e in enumerate(self.ms):
            if i >= limit:
                break
            try:
                kind = e.EntityName
                rec = {"type": kind, "layer": e.Layer}
                if kind == "AcDbLine":
                    rec["start"] = tuple(round(v, 2) for v in e.StartPoint)
                    rec["end"] = tuple(round(v, 2) for v in e.EndPoint)
                elif kind == "AcDbCircle":
                    rec["center"] = tuple(round(v, 2) for v in e.Center)
                    rec["radius"] = round(e.Radius, 2)
                elif kind == "AcDbArc":
                    rec["center"] = tuple(round(v, 2) for v in e.Center)
                    rec["radius"] = round(e.Radius, 2)
                elif kind in ("AcDbText", "AcDbMText"):
                    rec["text"] = e.TextString
                    rec["at"] = tuple(round(v, 2) for v in e.InsertionPoint)
                elif kind == "AcDbPolyline":
                    rec["closed"] = bool(e.Closed)
                    try:
                        rec["points"] = [round(v, 2) for v in e.Coordinates]
                    except Exception:
                        pass
                elif kind in ("AcDbRotatedDimension", "AcDbAlignedDimension",
                              "AcDb3PointAngularDimension", "AcDbDiametricDimension"):
                    rec["measurement"] = round(e.Measurement, 2)
                    try:
                        rec["text"] = e.TextOverride
                    except Exception:
                        pass
                elif kind == "AcDbBlockReference":
                    rec["name"] = e.Name
                    rec["at"] = tuple(round(v, 2) for v in e.InsertionPoint)
                out.append(rec)
            except Exception as ex:
                out.append({"type": "unreadable", "error": str(ex)})
        return out

    def summary(self):
        """High-level tally of what's in the drawing, by entity type and layer."""
        from collections import Counter
        types, layers = Counter(), Counter()
        for e in self.ms:
            try:
                types[e.EntityName] += 1
                layers[e.Layer] += 1
            except Exception:
                pass
        return {"total": self.ms.Count,
                "by_type": dict(types),
                "by_layer": dict(layers)}

    # --- layers ----------------------------------------------------------

    def layer(self, name, color=None):
        lay = self.doc.Layers.Add(name)
        if color is not None:
            lay.color = int(color)
        self.doc.ActiveLayer = lay
        return f"active layer = {name}" + (f" color={color}" if color else "")

    # --- saving ----------------------------------------------------------

    def save(self):
        self.doc.Save()
        return f"saved {self.doc.FullName}"

    def save_as(self, path):
        self.doc.SaveAs(path)
        return f"saved as {path}"


# ---- command line interface --------------------------------------------------

def _main(argv):
    if not argv:
        print(__doc__)
        return
    a = Acad()
    cmd, rest = argv[0], argv[1:]
    if cmd == "info":
        for k, v in a.info().items():
            print(f"{k:24}: {v}")
        return
    if cmd == "send":
        print(a.send(rest[0]))
        return
    # numeric primitives: convert remaining string args to floats where possible
    fn = getattr(a, cmd, None)
    if fn is None:
        print(f"unknown command: {cmd}")
        return
    args = []
    for r in rest:
        try:
            args.append(float(r))
        except ValueError:
            args.append(r)
    print(fn(*args))


if __name__ == "__main__":
    _main(sys.argv[1:])
