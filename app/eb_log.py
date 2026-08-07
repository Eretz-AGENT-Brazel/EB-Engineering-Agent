# -*- coding: utf-8 -*-
"""
eb_log -- read what AutoCAD and ProStructures print to the command line.

Why this exists
---------------
06/08/2026. `SetAsPlateBreakEdgeCut` stored its record, returned no error, and produced
no geometry. The API said nothing. The AutoCAD command line said:

    * WARNING  REQUESTED VOLUME SOLIDS CAN NOT BE PRODUCED.
      Handle of Object Type Ks_Plate is 0
    Use PS_GETOBJHANDLE to identify the Object

That message was only ever seen because a screenshot happened to include the command
line. **ProSteel diagnoses its own failures on a channel the API never returns**, and
this agent had been blind to it through every silent-failure investigation so far --
CreateSingleBolt, PsCreateFastener, set_Facet, TakeoverDrills. Some of those may have
been explaining themselves all along.

AutoCAD's LOGFILEMODE writes the command line to a file. Turning it on makes that
channel readable, so `mark()` / `since()` brackets an operation and returns exactly
what the software said while it ran.

    import eb_log
    eb_log.enable()
    m = eb_log.mark()
    eb.run("edgechamfer", handle=h, layout=1, v1=25)
    print(eb_log.since(m))          # -> the WARNING lines, if any

Note the log is flushed by AutoCAD as it writes; a very recent line can lag by a
moment, so `since()` retries briefly before giving up.
"""

import os
import time

_PATH = None


def _doc():
    import eb_api
    return eb_api._app_doc()[1] if hasattr(eb_api, "_app_doc") else eb_api._app().ActiveDocument


def path():
    """Full path of the current log file, or None if logging is off."""
    global _PATH
    d = _doc()
    try:
        if int(d.GetVariable("LOGFILEMODE")) != 1:
            return None
    except Exception:
        return None
    try:
        _PATH = str(d.GetVariable("LOGFILENAME"))
        return _PATH
    except Exception:
        return None


def enable():
    """Turn the command-line log on. Returns the log path.

    LOGFILEMODE is a per-drawing setting and AutoCAD opens a NEW log file when it is
    switched on, so this is cheap to call repeatedly but the path can change.
    """
    d = _doc()
    try:
        if int(d.GetVariable("LOGFILEMODE")) != 1:
            d.SetVariable("LOGFILEMODE", 1)
            time.sleep(0.3)
    except Exception as e:
        return "EB_ERR log: %s" % e
    return path()


def disable():
    try:
        _doc().SetVariable("LOGFILEMODE", 0)
        return True
    except Exception:
        return False


def mark():
    """Byte offset to read from. Take this immediately BEFORE the operation."""
    p = path()
    if not p or not os.path.exists(p):
        return 0
    try:
        return os.path.getsize(p)
    except Exception:
        return 0


def since(offset, wait=1.2, keep=None):
    """Everything written to the command line since `mark()`.

    keep: optional substring filter, e.g. "WARNING" or "ERROR".
    """
    p = path()
    if not p:
        return ["(logging is off -- call eb_log.enable())"]
    out, deadline = [], time.time() + wait
    while time.time() < deadline:
        try:
            with open(p, "r", encoding="utf-8", errors="replace") as f:
                f.seek(offset)
                out = [l.rstrip("\r\n") for l in f.readlines() if l.strip()]
            if out:
                break
        except Exception:
            pass
        time.sleep(0.2)
    if keep:
        out = [l for l in out if keep.lower() in l.lower()]
    return out


def problems(offset):
    """Only the lines that look like a complaint -- the usual reason to call this."""
    bad = ("warning", "error", "cannot", "can not", "failed", "invalid", "unknown",
           "not found", "ungültig", "fehler")
    return [l for l in since(offset) if any(b in l.lower() for b in bad)]


if __name__ == "__main__":
    print("log:", enable())
    m = mark()
    print("mark:", m)
    print("tail:", since(max(0, m - 4000))[-25:])
