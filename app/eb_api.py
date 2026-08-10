"""
eb_api.py - Python client for the EB Modeling API (native ProSteel from code).

File protocol: write plugin/eb_cmd.txt -> SendCommand EB_RUN6 -> read plugin/eb_result.txt
Verified native ops: beam (Ks_Shape), plate (Ks_Plate), bolt (Ks_Bolt DIN6914),
boltfield, conn_bolted, miter cut, workframe. Plus COM-level view/zoom/copy/delete/undo.

Profile resolver bridges speech ("HEB 500") to DB key ("HE500B", catalog "DIN_HEB").
"""

import os
import re
import sys
import time
import json

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
PLUG = os.path.join(HERE, "plugin")
CMD = os.path.join(PLUG, "eb_cmd.txt")
RES = os.path.join(PLUG, "eb_result.txt")
DLL = os.path.join(PLUG, "EBAgentApi152.dll")
RUN_CMD = "EB_RUN152"
# ---- which drawing every op is expected to run on -------------------------
# Twice on 06/08/2026 work landed in the WRONG drawing: first two documents were open
# at once (Amir spotted the two windows), then opening a Bentley sample silently became
# the active document and absorbed every op after it. Nothing was lost, but the model
# being reasoned about was not the model being changed.
# EXPECT_DWG is sent with every op as dwg=<file>; the plugin REFUSES to execute if the
# active document is a different drawing. Set with use(); use(None) disables the guard.
# The pin must survive PROCESS boundaries: nearly every action here is a fresh
# python run, so an in-memory value protects only the script that set it -- which is
# not the case that goes wrong. It is persisted next to the command file.
_PIN_FILE = os.path.join(PLUG, "eb_expect_dwg.txt")


def _read_pin():
    try:
        v = open(_PIN_FILE, encoding="utf-8").read().strip()
        return v or None
    except Exception:
        return None


EXPECT_DWG = _read_pin()


def use(dwg_name):
    """Pin every following op to this drawing (basename), or None to disable.

    Persisted, so the pin holds across separate python runs.
    """
    global EXPECT_DWG
    EXPECT_DWG = os.path.basename(dwg_name) if dwg_name else None
    try:
        if EXPECT_DWG:
            with open(_PIN_FILE, "w", encoding="utf-8") as f:
                f.write(EXPECT_DWG)
        elif os.path.exists(_PIN_FILE):
            os.remove(_PIN_FILE)
    except Exception:
        pass
    return EXPECT_DWG


PROJECTS = os.path.join(ROOT, "projects")
ACTIVE = os.path.join(ROOT, "data", "project.txt")

ACAD_EXE = r"C:\Program Files\Autodesk\AutoCAD 2015\acad.exe"
# METRIC ALWAYS -- Amir, 02/08/2026: "אנחנו עובדים תמיד בשיטה המטרית ... תסמן metric.
# לא לסמן IMPERIAL". acadiso.dwt is AutoCAD's ISO/metric template (acad.dwt is imperial).
# The previous value here was "Ps191_Metric", which DOES NOT EXIST on this machine -- so
# AutoCAD silently fell back to its own default, i.e. possibly imperial. Verified path:
METRIC_DWT = r"C:\Users\User\AppData\Local\Autodesk\AutoCAD 2015\R20.0\enu\Template\acadiso.dwt"
if not os.path.exists(METRIC_DWT):
    METRIC_DWT = r"C:\Program Files\Autodesk\AutoCAD 2015\UserDataCache\Template\acadiso.dwt"
PS_ARG = r"C:\Program Files\Bentley\ProStructures Ss6 R1\AutoCAD 2015\Prg\ProStructures_SS6.1ACAD_E001_409.arg"
PS_WD = r"C:\Program Files\Bentley\ProStructures Ss6 R1\AutoCAD 2015\Dwg"


# ---------- profile resolver ----------
_MAP_FILE = os.path.join(ROOT, "knowledge", "section_catalog_map.json")


def _load_map():
    if os.path.exists(_MAP_FILE):
        try:
            return json.load(open(_MAP_FILE, encoding="utf-8"))
        except Exception:
            pass
    return {}


def resolve_profile(text):
    t = (text or "").upper().replace(" ", "")
    # 1. data-driven map (learned from the real DB) wins
    mp = _load_map()
    key = t.replace("*", "X")
    if key in mp:
        return (mp[key]["name"], mp[key]["catalog"])
    # 2. rule fallbacks for the common DIN families
    m = re.match(r"^HE([ABM])0*(\d+)$", t) or re.match(r"^HE0*(\d+)([ABM])$", t)
    if m:
        g = m.groups()
        letter, size = (g[0], g[1]) if g[0] in "ABM" else (g[1], g[0])
        return ("HE%s%s" % (size, letter), "DIN_HE%s" % letter)
    m = re.match(r"^IPE0*(\d+)$", t)
    if m:
        return ("IPE%s" % m.group(1), "DIN_IPE")
    m = re.match(r"^U(?:PN)?0*(\d+)$", t)
    if m:
        return ("U%s" % m.group(1), "DIN_U")
    return (t, "")


# ---------- COM plumbing ----------
def acad_instances():
    """Every AutoCAD instance reachable over COM, as (com_app, [doc names]).

    Two AutoCAD *processes* can be running at once. On 06/08/2026 Amir had his own
    Drawing1.dwg open in a second instance while the agent worked in sandbox.dwg.
    Both instances publish the SAME class moniker, so they cannot be told apart by
    name -- only by asking each one which drawings it holds.
    """
    import pythoncom
    import win32com.client
    pythoncom.CoInitialize()
    rot = pythoncom.GetRunningObjectTable()
    ctx = pythoncom.CreateBindCtx(0)
    out, seen = [], set()
    for mk in rot:
        try:
            nm = mk.GetDisplayName(ctx, None)
        except Exception:
            continue
        if not nm.startswith("!{"):
            continue
        try:
            obj = rot.GetObject(mk)
            app = win32com.client.Dispatch(obj.QueryInterface(pythoncom.IID_IDispatch))
            if str(getattr(app, "Name", "")) != "AutoCAD":
                continue
            docs = tuple(sorted(app.Documents.Item(i).Name for i in range(app.Documents.Count)))
            if docs in seen:          # both monikers can resolve to one instance
                continue
            seen.add(docs)
            out.append((app, list(docs)))
        except Exception:
            continue
    return out


def _app():
    """Attach to the AutoCAD instance holding the pinned drawing -- never to a guess.

    GetActiveObject returns whichever instance registered in the Running Object Table
    first. That is not deterministic, and picking the wrong one means sending commands
    into the user's own AutoCAD session. When a pin is set (see use()), the instance is
    chosen by which one actually has that drawing open; if none does, this REFUSES
    rather than working somewhere unknown.
    """
    import pythoncom
    import win32com.client
    pythoncom.CoInitialize()
    want = EXPECT_DWG or ""
    if want:
        cands = acad_instances()
        if len(cands) > 1:
            hit = [(a, d) for (a, d) in cands
                   if any(want.lower() in n.lower() for n in d)]
            if len(hit) == 1:
                return hit[0][0]
            if len(hit) == 0:
                raise RuntimeError(
                    "no AutoCAD instance has '%s' open. instances: %s" %
                    (want, [d for (_a, d) in cands]))
            raise RuntimeError(
                "'%s' is open in %d instances -- refusing to guess: %s" %
                (want, len(hit), [d for (_a, d) in hit]))
    app = win32com.client.GetActiveObject("AutoCAD.Application")
    if want:
        try:
            names = [app.Documents.Item(i).Name for i in range(app.Documents.Count)]
        except Exception:
            names = []
        if names and not any(want.lower() in n.lower() for n in names):
            raise RuntimeError(
                "attached to the wrong AutoCAD: it holds %s, not '%s' -- refused" %
                (names, want))
    return app


def _quiescent(app):
    try:
        return bool(app.GetAcadState().IsQuiescent)
    except Exception:
        return True


def _send(doc, cmd, tries=15):
    import pythoncom
    for _ in range(tries):
        try:
            doc.SendCommand(cmd)
            return True
        except pythoncom.com_error:
            time.sleep(3)
    return False


def bootstrap(timeout=180):
    """Cold-start the whole stack: launch AutoCAD if down, wait ready,
    trust plugin dir, NETLOAD, ping. Returns a status dict."""
    import subprocess
    import pythoncom
    import win32com.client
    # launch if not running
    try:
        win32com.client.GetActiveObject("AutoCAD.Application")
    except Exception:
        subprocess.Popen([ACAD_EXE, "/p", PS_ARG, "/t", METRIC_DWT,
                          "/ld", "ProStructuresLoader.arx"], cwd=PS_WD)
    # wait for quiescent
    pythoncom.CoInitialize()
    app = None
    deadline = time.time() + timeout
    while time.time() < deadline:
        try:
            app = win32com.client.GetActiveObject("AutoCAD.Application")
            if _quiescent(app):
                break
        except Exception:
            pass
        time.sleep(5)
    if app is None:
        return {"ok": False, "error": "AutoCAD not ready in %ds" % timeout}
    # ActiveDocument can flake right after launch — retry via the robust helper
    try:
        app, doc = _app_doc(tries=15)
    except Exception as e:
        return {"ok": False, "error": "no active document: %s" % e}
    metric = enforce_metric()
    _netload()
    ping = run("ping")
    return {"ok": ping.startswith("EB_OK"), "ping": ping,
            "drawing": doc.Name, "metric": metric}


def enforce_metric():
    """METRIC ALWAYS. Amir's standing rule -- set it, then READ IT BACK.

    MEASUREMENT = 1  -> metric hatch/linetype files (0 = imperial)
    LUNITS      = 2  -> decimal units          (4 = architectural/inches)
    INSUNITS    = 4  -> millimetres            (1 = inches)
    AUNITS      = 0  -> decimal degrees
    Returns the verified state; never claims success without reading the value back.
    """
    want = {"MEASUREMENT": 1, "LUNITS": 2, "INSUNITS": 4, "AUNITS": 0}
    got, bad = {}, []
    try:
        _, d = _fresh_doc()
    except Exception as e:
        return {"ok": False, "error": "no document: %s" % e}
    for k, v in want.items():
        try:
            d.SetVariable(k, v)
        except Exception:
            pass
    for k, v in want.items():
        try:
            got[k] = int(d.GetVariable(k))
        except Exception:
            got[k] = None
        if got[k] != v:
            bad.append("%s=%s (want %s)" % (k, got[k], v))
    return {"ok": not bad, "vars": got, "mismatch": bad}


def _app_doc(tries=8):
    """Robustly obtain (app, doc), retrying transient COM dispatch flakes."""
    last = None
    for _ in range(tries):
        try:
            app = _app()
            doc = app.ActiveDocument
            return app, doc
        except Exception as e:
            last = e
            time.sleep(1.0)
    raise RuntimeError("COM not ready: %s" % last)



def _close_stray_docs(keep_name):
    """Leave EXACTLY ONE drawing open: keep_name.

    Launching AutoCAD creates a scratch Drawing1.dwg from the template, and opening a
    project adds a second document. Two open drawings is how an op silently modifies the
    WRONG model -- every op resolves MdiActiveDocument independently. Amir spotted the two
    windows on 06/08/2026; this closes them at the source.
    Returns the list of drawing names that were closed.

    REFUSES ENTIRELY when a second AutoCAD process is running (06/08/2026). Amir opened
    Drawing1.dwg in his own instance and said plainly: "this is a file I'm working on,
    don't close it." This routine's rule is "close anything that is not keep_name", and
    COM does not guarantee which instance it is attached to -- so with two instances the
    rule would have saved and closed HIS drawing. A wrong-drawing op is recoverable; a
    closed drawing someone is working in is not. The plugin's own `wrongdoc` guard is
    the real protection now, and it needs no closing at all.
    """
    closed = []
    try:
        import subprocess
        n = len([l for l in subprocess.run(
            ["tasklist", "/FI", "IMAGENAME eq acad.exe", "/NH"],
            capture_output=True, text=True, timeout=30).stdout.splitlines()
            if "acad.exe" in l.lower()])
    except Exception:
        n = 1
    if n > 1:
        print("[eb_api] %d AutoCAD processes are running -- _close_stray_docs REFUSED. "
              "The drawing pin (use('%s')) protects the work instead." % (n, keep_name))
        return closed
    for _ in range(6):
        try:
            app, _d = _fresh_doc()
            victim = None
            for i in range(app.Documents.Count):
                nm = app.Documents.Item(i).Name
                if keep_name.lower() not in nm.lower():
                    victim = app.Documents.Item(i)
                    break
            if victim is None:
                break
            nm = victim.Name
            victim.Activate()
            time.sleep(0.6)
            _app2, d2 = _fresh_doc()
            if keep_name.lower() not in d2.Name.lower():
                # NEVER discard silently. The first version closed with Close(False) and
                # threw away sandbox.dwg the moment a sample drawing was opened. A launch
                # scratch is disposable; anything the user или the agent touched is not.
                saved = False
                try:
                    if d2.Saved:            # untouched since last save -> nothing to lose
                        saved = True
                    else:
                        d2.Save()           # named drawing: persist before closing
                        saved = True
                except Exception:
                    saved = False
                if saved:
                    d2.Close(False)
                    closed.append(nm)
                else:
                    # unsaved AND unnameable (a brand-new Drawing1) -> discard is safe only
                    # if it is still empty
                    try:
                        empty = (d2.ModelSpace.Count == 0)
                    except Exception:
                        empty = False
                    if empty:
                        d2.Close(False)
                        closed.append(nm + " (empty scratch)")
                    else:
                        # refuse; leaving two drawings open is far better than losing one
                        closed.append("REFUSED-to-close-unsaved:" + nm)
                        break
            time.sleep(0.8)
        except Exception:
            # a successful Close disconnects the COM object -- that is not a failure
            time.sleep(0.5)
    return closed

def open_project_cad(dwg_path):
    """Open (or create) a project's dedicated DWG in AutoCAD+ProSteel, plugin loaded.
    Used by the console's 'open project file' button. Retries transient COM flakes."""
    import pythoncom
    import win32com.client
    st = bootstrap()
    if not st.get("ok"):
        return st
    last = None
    for _ in range(10):
        try:
            pythoncom.CoInitialize()
            app = win32com.client.GetActiveObject("AutoCAD.Application")
            docs = app.Documents
            if os.path.exists(dwg_path):
                docs.Open(dwg_path)
                action = "opened"
            else:
                d = docs.Add()
                os.makedirs(os.path.dirname(dwg_path), exist_ok=True)
                d.SaveAs(dwg_path)
                action = "created"
            closed = _close_stray_docs(os.path.basename(dwg_path))
            use(dwg_path)      # every op from here on is pinned to THIS drawing
            return {"ok": True, "dwg": os.path.basename(dwg_path),
                    "action": action, "closed_stray": closed, "expect": EXPECT_DWG}
        except Exception as e:
            last = e
            time.sleep(1.5)
    return {"ok": False, "error": str(last)}


def _fresh_doc():
    """Always re-acquire a fresh app+doc (COM objects go stale after Documents.Add)."""
    import pythoncom
    import win32com.client
    pythoncom.CoInitialize()
    app = win32com.client.GetActiveObject("AutoCAD.Application")
    return app, app.ActiveDocument


def _send_cmd(cmd, tries=8):
    """Send a command line, re-acquiring a fresh doc each attempt (beats staleness)."""
    for _ in range(tries):
        try:
            _, d = _fresh_doc()
            d.SendCommand(cmd)
            return True
        except Exception:
            time.sleep(1)
    return False


def _netload():
    """Load the plugin DLL with NO LISP.

    Was:  a LISP  command  form wrapping _NETLOAD. LISP is banned outright.
    Now:  FILEDIA=0 suppresses the file dialog, so _NETLOAD accepts the path as a plain
          command argument. A command token and a file path: no parentheses, no evaluation.

    UNVERIFIED against a live AutoCAD as of 02/08/2026 -- verify before lesson 6.
    If the plugin fails to load, run() reports 'Unknown command EB_RUN..' and the cause
    is here, not in the op.
    """
    try:
        _, d = _fresh_doc()
        tp = d.GetVariable("TRUSTEDPATHS") or ""
        if PLUG.lower() not in tp.lower():
            # TRUSTEDPATHS is an AutoCAD SECURITY setting. The agent must not relax it
            # silently -- report it and let Amir add the path once via OPTIONS > Files.
            sys.stderr.write(
                "EB_WARN plugin folder is not in TRUSTEDPATHS. Add it once in AutoCAD:\n"
                "        OPTIONS > Files > Trusted Locations > %s\n" % PLUG)
    except Exception:
        pass
    fd = None
    try:
        _, d = _fresh_doc()
        fd = d.GetVariable("FILEDIA")
        d.SetVariable("FILEDIA", 0)
    except Exception:
        pass
    _send_cmd("_NETLOAD\n%s\n" % DLL)
    time.sleep(1.0)
    if fd is not None:
        try:
            _, d = _fresh_doc()
            d.SetVariable("FILEDIA", fd)
        except Exception:
            pass

    # PROVE the command is live before returning. A fixed sleep is a guess: on 06/08/2026
    # this returned None while EB_RUN69 was not yet registered, and the next three ops came
    # back EB_TIMEOUT with no indication that loading was the cause. Same rule as everywhere
    # else in this codebase -- do not report success, measure it.
    for attempt in range(12):
        try:
            if (run("ping") or "").startswith("EB_OK"):
                return "EB_OK netload %s (%s live after %.1fs)" % (
                    os.path.basename(DLL), RUN_CMD, 1.0 + attempt * 0.5)
        except Exception:
            pass
        time.sleep(0.5)
    return ("EB_ERR netload: %s did not answer after loading %s. "
            "Check TRUSTEDPATHS, or whether the DLL name collides with one already loaded."
            % (RUN_CMD, os.path.basename(DLL)))


def _fire_and_match(reqid, wait):
    """Send EB_RUN and return ONLY a result carrying our reqid (never stale)."""
    if not _send_cmd(RUN_CMD + "\n"):
        return None
    tag = "reqid=" + reqid
    deadline = time.time() + wait + 8
    while time.time() < deadline:
        try:
            if os.path.exists(RES):
                r = open(RES, encoding="utf-8-sig").read().strip()
                if tag in r:
                    return r
        except Exception:
            pass
        time.sleep(0.12)
    return None


def connect_only(timeout=25):
    """CONNECT to an ALREADY-RUNNING AutoCAD+ProSteel (never launches it).
    Loads the plugin, verifies with a ping."""
    import pythoncom
    import win32com.client
    pythoncom.CoInitialize()
    app = None
    for _ in range(5):          # patient: AutoCAD may still be loading when clicked
        try:
            app = win32com.client.GetActiveObject("AutoCAD.Application")
            break
        except Exception:
            time.sleep(2)
    if app is None:
        return {"ok": False, "error": "לא נמצא AutoCAD פתוח. פתח ידנית את AutoCAD + ProSteel ולחץ שוב."}
    deadline = time.time() + timeout
    while time.time() < deadline:
        if _quiescent(app):
            break
        time.sleep(2)
    _netload()
    ping = run("ping")
    return {"ok": ping.startswith("EB_OK"), "ping": ping}


def connect_project(dwg_path):
    """Connect to the running programs, then open/create the project's dedicated DWG.
    Does NOT launch AutoCAD — the modeler opens it manually."""
    st = connect_only()
    if not st.get("ok"):
        return st
    last = None
    for _ in range(10):
        try:
            app, doc = _fresh_doc()
            if os.path.exists(dwg_path):
                app.Documents.Open(dwg_path)      # reopen an existing project file
                action = "opened"
            else:
                os.makedirs(os.path.dirname(dwg_path), exist_ok=True)
                doc.SaveAs(dwg_path)              # save the drawing YOU opened as the project file
                action = "linked"
            closed = _close_stray_docs(os.path.basename(dwg_path))
            use(dwg_path)      # every op from here on is pinned to THIS drawing
            return {"ok": True, "dwg": os.path.basename(dwg_path),
                    "action": action, "closed_stray": closed, "expect": EXPECT_DWG}
        except Exception as e:
            last = e
            time.sleep(1.5)
    return {"ok": False, "error": str(last)}



def _acad_pids():
    """Process ids of the running AutoCAD instances, found from their main windows.

    Titles are matched the same way eb_shot.py matches them, so this never depends on
    a process name that could differ between installs.
    """
    pids = set()
    try:
        import ctypes
        from ctypes import wintypes
        u = ctypes.windll.user32
        CB = ctypes.WINFUNCTYPE(ctypes.c_bool, wintypes.HWND, wintypes.LPARAM)
        buf = ctypes.create_unicode_buffer(512)

        def cb(hwnd, _l):
            if not u.IsWindowVisible(hwnd):
                return True
            u.GetWindowTextW(hwnd, buf, 512)
            if "AutoCAD" in buf.value:
                pid = wintypes.DWORD()
                u.GetWindowThreadProcessId(hwnd, ctypes.byref(pid))
                if pid.value:
                    pids.add(pid.value)
            return True

        u.EnumWindows(CB(cb), 0)
    except Exception:
        pass
    return pids


def modal_dialogs():
    """Every top-level dialog owned by AUTOCAD, by title.

    MEASURED 06/08/2026: while ProStructures' "ProSteel Positionflags and Positioning"
    dialog was open and blocking, AutoCAD reported  quiescent=True  and  CMDACTIVE=0.
    Amir saw the dialog on screen; the API did not. So _quiescent() CANNOT be trusted to
    mean "ready" -- a ProSteel dialog is a separate window that leaves AutoCAD looking idle.
    Without this check, any dialog-driven command parks the session silently.

    FIXED 09/08/2026 -- OWNERSHIP, which the docstring always claimed but the code never
    checked. It enumerated EVERY #32770 window on the desktop, so any "Open" or "Save As"
    box Amir had open IN ANY APPLICATION froze the agent completely. Caught live: the
    guard reported "Sheet Information", then "Open", while AutoCAD itself was clean --
    they were Amir's windows. Amir: "אתה רק על האוטוקאד אל תתערב".

    This does NOT relax the check. A real AutoCAD or ProSteel dialog still blocks exactly
    as before; the guard simply stops counting windows that were never AutoCAD's.
    If the AutoCAD pid cannot be determined, it falls back to the old desktop-wide scan --
    blocking wrongly is safer than running into a dialog.
    """
    out = []
    try:
        import ctypes
        from ctypes import wintypes
        u = ctypes.windll.user32
        EnumWindows = u.EnumWindows
        CB = ctypes.WINFUNCTYPE(ctypes.c_bool, wintypes.HWND, wintypes.LPARAM)
        buf = ctypes.create_unicode_buffer(512)
        cls = ctypes.create_unicode_buffer(256)
        pids = _acad_pids()

        def cb(hwnd, _l):
            if not u.IsWindowVisible(hwnd):
                return True
            u.GetClassNameW(hwnd, cls, 256)
            if cls.value != "#32770":          # the standard Windows dialog class
                return True
            if pids:                           # only AutoCAD's own dialogs
                pid = wintypes.DWORD()
                u.GetWindowThreadProcessId(hwnd, ctypes.byref(pid))
                if pid.value not in pids:
                    return True
            u.GetWindowTextW(hwnd, buf, 512)
            t = buf.value.strip()
            if t:
                out.append(t)
            return True

        EnumWindows(CB(cb), 0)
    except Exception:
        pass
    return out


def ready(verbose=False):
    """True only if AutoCAD is quiescent AND no dialog is waiting."""
    dlgs = modal_dialogs()
    q = False
    try:
        q = _quiescent(_app())
    except Exception:
        pass
    if verbose and dlgs:
        sys.stderr.write("EB_WARN dialog open: " + "; ".join(dlgs) + chr(10))
    return q and not dlgs


def run(op, wait=5.0, _log=True, **kw):
    import uuid
    reqid = uuid.uuid4().hex[:8]
    try:
        app, _ = _app_doc()
        if not _quiescent(app):
            return "EB_BUSY AutoCAD עסוק - לחץ ESC ונסה שוב"
        _dlgs = modal_dialogs()
        if _dlgs:
            return ("EB_DIALOG a dialog is waiting and AutoCAD reports itself idle: %s"
                    % "; ".join(_dlgs))
    except Exception:
        pass
    pin = EXPECT_DWG or _read_pin()
    if pin and "dwg" not in kw:
        kw = dict(kw, dwg=pin)
    body = "\n".join(["reqid=" + reqid, "op=" + op] +
                     ["%s=%s" % (k, v) for k, v in kw.items()]) + "\n"
    with open(CMD, "w", encoding="utf-8") as f:
        f.write(body)
    res = _fire_and_match(reqid, wait)
    if res is None:                      # watchdog: maybe command not registered
        lp = ""
        try:
            _, d = _fresh_doc()
            lp = d.GetVariable("LASTPROMPT") or ""
        except Exception:
            pass
        if "Unknown command" in lp or "NETLOAD" in lp:
            _netload()
            with open(CMD, "w", encoding="utf-8") as f:
                f.write(body)
            res = _fire_and_match(reqid, wait)
    if res is None:
        return "EB_TIMEOUT reqid=%s (no matching result; command may not have executed)" % reqid
    if _log and res.startswith("EB_OK") and op in ("beam", "plate", "bolt", "boltfield", "conn_bolted", "miter"):
        _model_log(op, kw, res)
    return res


def ensure_doc(dwg_path):
    """Guarantee the project's DWG is the ACTIVE document before modeling."""
    want = os.path.basename(dwg_path).lower()
    r = run("whoami")
    if want in r.lower():
        return True
    try:
        app = _app()
        for d in list(app.Documents):
            if d.Name.lower() == want:
                d.Activate()
                return True
        if os.path.exists(dwg_path):
            app.Documents.Open(dwg_path)
            return True
    except Exception:
        pass
    return False


def _stamp():
    try:
        return os.path.getmtime(RES)
    except OSError:
        return 0


def _model_log(op, kw, res):
    """Phase G: record every native create into the active project's model_log."""
    try:
        pid = open(ACTIVE, encoding="utf-8").read().strip() if os.path.exists(ACTIVE) else ""
        if not pid:
            return
        d = os.path.join(PROJECTS, pid)
        os.makedirs(d, exist_ok=True)
        h = re.search(r"handle=(\w+)", res)
        rec = {"op": op, "args": kw, "handle": h.group(1) if h else None, "ts": time.strftime("%H:%M:%S")}
        with open(os.path.join(d, "model_log.jsonl"), "a", encoding="utf-8") as f:
            f.write(json.dumps(rec, ensure_ascii=False) + "\n")
    except Exception:
        pass


# ---------- geometry helpers ----------
def _pt(p):
    return "%s,%s,%s" % (p[0], p[1], p[2] if len(p) > 2 else 0)


def handle_of(result):
    m = re.search(r"handle=(\w+)", result or "")
    return m.group(1) if m else None


def fire(cmd):
    """Fire a raw AutoCAD/ProSteel command (e.g. PS_COLLISION). True if sent.
    Used for tools that have no API equivalent yet but Amir uses in practice."""
    try:
        return _send_cmd("\x1b\x1b_" + cmd + " ")
    except Exception:
        return False


# ---------- native modeling verbs ----------
def beam(profile, p1, p2, rot=0, catalog=None):
    """Create a native ProSteel shape.
    catalog: pass the EXACT catalog when known (e.g. read from an existing model
    by dumpmodel — "BRITAIN.BS_CELSIUS_RHS"); the plugin then skips all guessing.
    Leave None to let the resolver infer it from the profile name."""
    if catalog:
        # exact name+catalog straight from a read model: no resolution needed
        cat = catalog.split(".")[-1] if "." in catalog else catalog
        return run("beam", name=profile, catalog=cat, p1=_pt(p1), p2=_pt(p2), rot=rot)
    name, cat = resolve_profile(profile)
    return run("beam", name=name, catalog=cat, p1=_pt(p1), p2=_pt(p2), rot=rot)


def plate(center, length, width, thickness, normal=(0, 0, 1)):
    return run("plate", center=_pt(center), l=length, w=width, t=thickness, normal=_pt(normal))


def bolt(p1, p2, dia=20, style="DIN6914", hosts=""):
    return run("bolt", p1=_pt(p1), p2=_pt(p2), dia=dia, style=style, hosts=hosts)


def boltfield(center, nx=2, ny=2, sx=100, sy=100, dia=20, gap=60, style="DIN6914", hosts=""):
    return run("boltfield", center=_pt(center), nx=nx, ny=ny, sx=sx, sy=sy,
               dia=dia, gap=gap, style=style, hosts=hosts)


def conn_bolted(at, pl=220, pw=220, pt=20, gap=60, nx=2, ny=2, sx=100, sy=100, dia=20, style="DIN6914"):
    return run("conn_bolted", at=_pt(at), pl=pl, pw=pw, pt=pt, gap=gap,
               nx=nx, ny=ny, sx=sx, sy=sy, dia=dia, style=style, wait=6)


def miter(cut_handle, other_handle, type=1):
    return run("miter", cut=cut_handle, other=other_handle, type=type)


def workframe(at=(0, 0, 0), x=6000, y=3000):
    return run("workframe", at=_pt(at), x=x, y=y)


def list_model():
    return run("list", _log=False)


# ---------- model reader (L2) ----------
def dumpmodel(out="eb_model.txt"):
    """Read the FULL semantics of every ProSteel object in the open drawing.
    Returns (reply, elements) where elements is a list of dicts:
      shape: {kind,handle,profile,catalog,p1,p2,length,material,name}
      plate: {kind,handle,l,w,t,insert,poly,material,name}
      bolt:  {kind,handle,diameter,style,count,length,insert,name}
    """
    r = run("dumpmodel", out=out, _log=False)
    fp = os.path.join(PLUG, out)
    els = []
    if not os.path.exists(fp):
        return r, els

    def _xyz(s):
        try:
            return tuple(float(x) for x in s.split(","))
        except Exception:
            return (0.0, 0.0, 0.0)

    for line in open(fp, encoding="utf-8").read().splitlines():
        f = line.split("\t")
        if not f or not f[0]:
            continue
        k = f[0]
        try:
            if k == "SHAPE" and len(f) >= 10:
                els.append({"kind": "shape", "handle": f[1], "profile": f[2], "catalog": f[3],
                            "p1": _xyz(f[4]), "p2": _xyz(f[5]), "length": float(f[6] or 0),
                            "material": f[7], "name": f[8], "cls": f[9]})
            elif k == "PLATE" and len(f) >= 10:
                els.append({"kind": "plate", "handle": f[1], "l": float(f[2] or 0),
                            "w": float(f[3] or 0), "t": float(f[4] or 0),
                            "insert": _xyz(f[5]),
                            "poly": [_xyz(p) for p in f[6].split(";") if p],
                            "material": f[7], "name": f[8], "cls": f[9]})
            elif k == "BOLT" and len(f) >= 9:
                els.append({"kind": "bolt", "handle": f[1], "diameter": float(f[2] or 0),
                            "style": f[3], "count": int(float(f[4] or 0)),
                            "length": float(f[5] or 0), "insert": _xyz(f[6]),
                            "name": f[7], "cls": f[8]})
            elif k in ("OTHER", "ERR"):
                els.append({"kind": k.lower(), "handle": f[1] if len(f) > 1 else "",
                            "cls": f[2] if len(f) > 2 else "",
                            "info": f[3] if len(f) > 3 else ""})
        except Exception:
            pass
    return r, els


# ---------- learning mode ----------
def _learn_dir():
    pid = open(ACTIVE, encoding="utf-8").read().strip() if os.path.exists(ACTIVE) else ""
    if not pid:
        return None
    d = os.path.join(PROJECTS, pid, "learning")
    os.makedirs(d, exist_ok=True)
    return d


_LEARN_STATE = os.path.join(ROOT, "data", "learning.json")


def learn_state():
    try:
        return json.load(open(_LEARN_STATE, encoding="utf-8"))
    except Exception:
        return {"on": False, "log": None}


def _set_learn_state(on, log=None):
    try:
        json.dump({"on": on, "log": log}, open(_LEARN_STATE, "w", encoding="utf-8"))
    except Exception:
        pass


def learn_start():
    d = _learn_dir()
    if not d:
        return {"ok": False, "error": "no active project"}
    log = os.path.join(d, "session_%s.jsonl" % time.strftime("%Y%m%d_%H%M%S"))
    open(log, "a", encoding="utf-8").close()
    r = run("learn_on", log=log.replace("\\", "/"))
    ok = r.startswith("EB_OK")
    if ok:
        _set_learn_state(True, log)
    return {"ok": ok, "log": log, "result": r}


def _log_summary(log):
    cmds = objs = 0
    try:
        for line in open(log, encoding="utf-8-sig"):
            if '"cmd_start"' in line:
                cmds += 1
            elif '"obj_add"' in line:
                objs += 1
    except Exception:
        pass
    return cmds, objs


def learn_stop():
    r = run("learn_off")
    st = learn_state()
    cmds, objs = _log_summary(st.get("log") or "")
    _set_learn_state(False, st.get("log"))
    return {"ok": r.startswith("EB_OK"), "result": r, "cmds": cmds, "objs": objs}


def learn_status():
    return run("learn_status")


# ---------- COM-level ops (no plugin needed) : Phase F/H ----------
_VIEW = {"top": (0, 0, 1), "bottom": (0, 0, -1), "front": (0, -1, 0), "back": (0, 1, 0),
         "left": (-1, 0, 0), "right": (1, 0, 0), "iso": (1, -1, 1)}


def view(name):
    app = _app()
    if not _quiescent(app):
        return "EB_BUSY"
    import pythoncom
    import win32com.client
    vec = _VIEW.get(name.lower(), (1, -1, 1))
    doc = app.ActiveDocument
    vp = doc.ActiveViewport
    vp.Direction = win32com.client.VARIANT(pythoncom.VT_ARRAY | pythoncom.VT_R8, [float(v) for v in vec])
    doc.ActiveViewport = vp
    try:
        app.ZoomExtents()
    except Exception:
        pass
    return "EB_OK view=" + name


def zoom():
    app = _app()
    app.ZoomExtents()
    return "EB_OK zoom"


def _raw(cmd):
    app = _app()
    if not _quiescent(app):
        return "EB_BUSY"
    _send(app.ActiveDocument, cmd)
    return "EB_OK sent"


def undo_mark():
    return _raw("_UNDO _M\n")


def undo_back():
    return _raw("_UNDO _B\n")


def copy(handle, dx, dy, dz=0):
    """Copy an entity by a delta. Routed through the plugin's replicate op.

    The old implementation selected the entity with a LISP  handent  form. LISP is banned
    outright. The plugin does the same job natively, and the correct ProStructures
    primitive is PsMiscTools.ObjectsCopy(PsSelection, PsMatrix).
    """
    return run("replicate", handles=handle, to="%s,%s,%s" % (dx, dy, dz), wait=45)

def position():
    """Production: assign position numbers (ProSteel positioning)."""
    return _raw("_PS_POS ")

def partlist():
    """Production: create a parts list (BOM)."""
    return _raw("_PS_CREATE_PARTLIST ")

def detailing():
    """Production: open DetailCenter for shop drawings."""
    return _raw("_PS_DETCENTER ")

def nc_data():
    """Production: generate NC/CNC data for the factory."""
    return _raw("_PS_NC_DATA ")

def delete(handle):
    """Erase one entity by handle via COM (keeps native semantics).

    Uses _app_doc, not _app().ActiveDocument: a COM call issued right after a heavy
    plugin run (a collision check over 132 parts) raises AttributeError on
    ActiveDocument while AutoCAD is still settling. _app_doc retries that away.
    """
    app, doc = _app_doc()
    if not _quiescent(app):
        return "EB_BUSY"
    try:
        ent = doc.HandleToObject(handle)
        ent.Delete()
        return "EB_OK deleted " + handle
    except Exception as e:
        return "EB_ERR delete " + str(e)


if __name__ == "__main__":
    import sys
    if len(sys.argv) > 1 and sys.argv[1] == "bootstrap":
        print(bootstrap())
    else:
        print("EB Modeling API. verbs: beam plate bolt boltfield conn_bolted miter workframe view")

def save(backup=True):
    """Save the pinned drawing, PROVE it reached the disk, and keep a timestamped copy.

    06/08/2026: Amir said "save the model, in case the machine dies". The drawing on disk
    was then SEVEN HOURS stale -- every op had gone into memory and nothing had been
    written since 10:16. A modelling session that cannot be recovered is not work, it is
    a demonstration.

    Verified by the file's mtime and size, not by the return of Save(): the same rule as
    everywhere else here.
    """
    import shutil
    import os as _os
    # ⚠️ 06/08/2026: the plugin-side route (Database.SaveAs / Editor.Command "_QSAVE") wrote
    # an ESSENTIALLY EMPTY DWG -- 11 KB while the document held 256 entities -- and destroyed
    # the live file. COM's doc.Save() is the route that has actually produced correct files.
    # It is also the route that reports the SIZE, which is the only way to catch this class of
    # failure: a save that "succeeds" and writes nothing.
    path = _os.path.join(PROJECTS, "SANDBOX", EXPECT_DWG or "sandbox.dwg")
    before_size = _os.path.getsize(path) if _os.path.exists(path) else 0
    before = _os.path.getmtime(path) if _os.path.exists(path) else 0
    app, doc = _app_doc(tries=10)
    doc.Save()
    for _ in range(25):
        time.sleep(1)
        if _os.path.exists(path) and _os.path.getmtime(path) > before:
            break
    # A save that SHRINKS the drawing by more than half is a corrupt write, not a save.
    # Refuse to call it a success and refuse to take a backup of it.
    now = _os.path.getsize(path) if _os.path.exists(path) else 0
    if before_size > 50000 and now < before_size * 0.5:
        return ("EB_ERR save: %s SHRANK from %.0f KB to %.0f KB -- that is a corrupt write, "
                "not a save. The previous backup is still good; do NOT overwrite it."
                % (_os.path.basename(path), before_size / 1024.0, now / 1024.0))
    if not (_os.path.exists(path) and _os.path.getmtime(path) > before):
        return "EB_ERR save: %s was NOT written to disk" % _os.path.basename(path)
    out = "EB_OK save %s %.2f MB" % (_os.path.basename(path), _os.path.getsize(path) / 1e6)
    if backup:
        bak = _os.path.join(_os.path.dirname(path),
                            "%s-backup-%s.dwg" % (_os.path.splitext(_os.path.basename(path))[0],
                                                  time.strftime("%Y%m%d-%H%M")))
        try:
            shutil.copy2(path, bak)
            out += " + backup %s" % _os.path.basename(bak)
        except Exception as e:
            out += " (backup failed: %s)" % e
    return out
