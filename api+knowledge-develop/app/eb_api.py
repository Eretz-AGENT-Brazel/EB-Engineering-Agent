"""
eb_api.py - Python client for the EB Modeling API (native ProSteel from code).

File protocol: write plugin/eb_cmd.txt -> SendCommand RUN_CMD -> read plugin/eb_result.txt
The version lives in DLL and RUN_CMD below and NOWHERE ELSE. This line used to hardcode
EB_RUN6 and went 146 builds stale.
Verified native ops: beam (Ks_Shape), plate (Ks_Plate), bolt (Ks_Bolt, default style
8.8S -- see DEFAULT_BOLT_STYLE), boltfield, conn_bolted, miter cut, workframe.
Plus COM-level view/zoom/copy/delete/undo.

Profile resolver bridges speech ("HEB 500") to DB key ("HE500B", catalog "DIN_HEB").
"""

import io
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
DLL = os.path.join(PLUG, "EBAgentApi199.dll")
RUN_CMD = "EB_RUN199"
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
#
# ⭐ 17/08/2026 -- the pin became a WORK SESSION (app/worksession.py), by Amir's decision
# that the agent and he must be able to work on different models at the same time. Three
# measured holes in the old pin, all closed here:
#   1. It was a default, not a lock: run() injected it only `if "dwg" not in kw`, so an
#      explicit dwg= overrode it in silence. Now a conflict REFUSES.
#   2. It never expired. On the morning of 17/08 it still read `test-2026-08-13.dwg`
#      from the previous day while lesson 7 was the live model.
#   3. It carried a basename only -- two projects may each hold a `test.dwg`. The session
#      carries the full path, and v185 checks it with dwgpath=.
# The legacy one-line file is still written, so anything reading it keeps working.
_PIN_FILE = os.path.join(PLUG, "eb_expect_dwg.txt")

if HERE not in sys.path:
    sys.path.insert(0, HERE)
import worksession as ws                                   # noqa: E402


def _read_pin():
    """Basename of the drawing this process is allowed to touch, or None."""
    ses = ws.current()
    if ses:
        return ses.get("name")
    try:
        v = open(_PIN_FILE, encoding="utf-8").read().strip()
        return v or None
    except Exception:
        return None


EXPECT_DWG = _read_pin()


def use(dwg_name, task=None, project=None, force=False):
    """Enter a model: pin every following op to it. `None` leaves the current one.

    Persisted in the work-session registry, so the pin holds across separate python runs
    AND says who owns the model, when it was entered and what the task is.
    """
    global EXPECT_DWG
    if not dwg_name:
        ws.close_session()
        EXPECT_DWG = None
        try:
            if os.path.exists(_PIN_FILE):
                os.remove(_PIN_FILE)
        except Exception:
            pass
        return None
    ses = ws.open_session(dwg_name, project=project, task=task, force=force)
    EXPECT_DWG = ses["name"]
    try:
        with open(_PIN_FILE, "w", encoding="utf-8") as f:
            f.write(EXPECT_DWG)
    except Exception:
        pass
    return EXPECT_DWG


def session():
    """The work session this process is bound to (dict) or None."""
    return ws.current()


def sessions():
    """Every model currently held, by anyone. Amir's are marked owner=amir."""
    return ws.sessions()


def hands_off(dwg):
    """Amir declares a drawing his. Every op, netload and variable write refuses it."""
    s = ws.claim(dwg)
    return s


# ---- the per-model mailbox (v185) -----------------------------------------
# ONE eb_cmd.txt and ONE eb_result.txt served the whole machine, plus ~40 output files
# with fixed names (eb_list.txt, eb_propfull.txt, eb_holes.txt ...). Only eb_result.txt
# carries a reqid, so with two jobs running the result was safe but every other file was
# not: job A could read the eb_list.txt that job B had just overwritten, silently.
# v185 gives each drawing its own channel, and derives the channel name from the drawing
# on BOTH sides (worksession.slot_of == ApiCmds.SlotOf), so nothing has to be negotiated.
def channel(dwg=None):
    """Directory of the mailbox for a drawing (created on demand).

    Resolved through the work session, so the full path decides -- `EXPECT_DWG` is only a
    label and two projects may each own a `test.dwg`.
    """
    if not dwg:
        ses = ws.current()
        dwg = (ses or {}).get("dwg") or EXPECT_DWG
    if not dwg:
        return PLUG
    return ws.channel_dir(dwg, make=True)


def _cmd_path(dwg=None):
    ch = channel(dwg)
    return CMD if ch == PLUG else os.path.join(ch, "eb_cmd.txt")


def _res_path(dwg=None):
    ch = channel(dwg)
    return RES if ch == PLUG else os.path.join(ch, "eb_result.txt")


# ops that change nothing and are exactly what you need in order to diagnose a refusal
_UNGATED_OPS = ("ping", "whoami", "env", "docs")

# ⛔⛔ THE BUILD TARGET (18/08/2026). Ops that CHANGE the model. A read may wander between the
# models of an assignment set; a build may not. See build_target() below for what this cost.
_WRITING_OPS = (
    "beam", "plate", "plate9", "polyplate", "arcplate", "shape", "solid", "bend", "bendtwo",
    "bendshape", "grid", "gridcolumns", "workframe", "frame", "stiffener", "purlin",
    "bolt", "boltfield", "boltparts", "conn", "conn_bolted", "connbase", "connsplice",
    "connstiff", "connremove", "connset", "drill", "drillfield", "drillspecial", "touchdrill",
    "polycut", "outlet", "detailcut", "planecut", "cutat", "miter", "chamfer", "edgechamfer",
    "boolean", "copy", "mirror", "rotate", "replicate", "clonemodel", "clonedrills",
    "group", "groupauto", "groupedit", "setlayer", "setpoly", "shapeedit", "platepoly",
    "posnum", "posset", "posauto", "anchor", "killholefield", "save", "delete",
)

_BUILD_TARGET = None


def build_target(dwg=None):
    """Declare (or clear) the ONE drawing this process may create or destroy geometry in.

    Amir's assignment says WHICH MODELS the agent may touch. This says which one the CURRENT
    WORK builds into -- a narrower question, and the one that went wrong on 18/08/2026: a
    `props(dwg=<source>)` read switched the session, and the eighteen parts that followed were
    created in Amir's model instead of the rebuild. Nothing failed; the two drawings were
    copies of each other, so every count agreed with expectation.

    Set it to the rebuild before building, and every creating op refuses the moment the
    session is pointing somewhere else. Pass None to clear.
    """
    global _BUILD_TARGET
    _BUILD_TARGET = os.path.basename(str(dwg)) if dwg else None
    return _BUILD_TARGET


def _when(ts):
    try:
        return time.strftime("%d/%m %H:%M", time.localtime(float(ts)))
    except Exception:
        return "?"


def _session_gate(op, kw):
    """Return a refusal string, or None to proceed. This is the lock the old pin was not.

    R3 someone else's model is untouchable · R2 an explicit dwg= may not overrule the
    session · R4 consent expires · R5 while anyone else holds a model, an UNPINNED op is
    refused outright -- that is precisely the state in which a stray op lands in his file.
    """
    if op in _UNGATED_OPS:
        return None
    if _BUILD_TARGET and op in _WRITING_OPS:
        here = (ws.current() or {}).get("name") or ""
        asked_dwg = os.path.basename(str(kw.get("dwg", "") or "")) or here
        if asked_dwg.lower() != _BUILD_TARGET.lower():
            return ("EB_ERR build target: this work builds into '%s' but the op would run in "
                    "'%s' -- refused, nothing was executed. A read of another model switches "
                    "the session (and the active document) with it; re-enter the target with "
                    "eb_api.use(...) before creating anything, or clear it with "
                    "eb_api.build_target(None)." % (_BUILD_TARGET, asked_dwg or "?"))
    st = ws.load()
    ses = ws.current(st)
    asked = os.path.basename(str(kw.get("dwg", "") or "")) or None

    for name in (asked, (ses or {}).get("name")):
        if not name:
            continue
        rec = ws.find(name, st)
        if rec and rec.get("owner") != ws.OWNER_AGENT:
            return ("EB_ERR hands-off: '%s' is held by owner=%s since %s -- refused, "
                    "nothing was executed. Release it with "
                    "`python app/worksession.py release \"%s\"`."
                    % (name, rec.get("owner"), _when(rec.get("opened")), name))

    locked_names = [n.lower() for n in ws.assigned_names(st)]
    if locked_names and asked and asked.lower() not in locked_names:
        return ("EB_ERR assigned: the agent is locked to %s and the op asked for '%s' -- "
                "refused, nothing was executed. Only Amir lifts it: "
                "`python app/worksession.py unassign`."
                % (", ".join("'%s'" % n for n in ws.assigned_names(st)), asked))

    if asked and ses and asked.lower() != str(ses.get("name", "")).lower():
        # ⭐ WITHIN an assignment set, an explicit dwg= is not a conflict -- it is the
        # sanctioned way to pick WHICH of Amir's assigned models this op works. The switch
        # is total (session, pin, channel, activation), not a silent override: the command
        # travels in the chosen model's own mailbox exactly as if use() had been called.
        if asked.lower() in locked_names:
            try:
                ws.switch(asked)
                global EXPECT_DWG
                EXPECT_DWG = (ws.current() or {}).get("name") or asked
                return None
            except Exception as e:
                return "EB_ERR assigned-switch failed: %s" % e
        return ("EB_ERR pin conflict: this process is working in '%s' but the op asked for "
                "'%s' -- refused, nothing was executed. An explicit dwg= no longer overrules "
                "the session (it did until 17/08/2026, silently). Switch with "
                "eb_api.use(<dwg>) or run it from that model's own session."
                % (ses.get("name"), asked))

    if ses:
        stale, why = ws.staleness(ses)
        if stale:
            return ("EB_ERR stale session on '%s' (%s) -- refused, nothing was executed. "
                    "Yesterday's pin is not today's consent: confirm with "
                    "`python app/worksession.py confirm`, or enter another model."
                    % (ses.get("name"), why))
        return None

    foreign = [s for s in st["sessions"].values() if s.get("owner") != ws.OWNER_AGENT]
    if foreign and not asked:
        return ("EB_ERR no work session: %s hold%s %s right now, and an unpinned op is how "
                "work lands in the wrong drawing -- refused. Enter a model first: "
                "eb_api.use(<dwg>)."
                % (", ".join(sorted(set(f.get("owner", "?") for f in foreign))),
                   "" if len(foreign) > 1 else "s",
                   ", ".join(f.get("name", "?") for f in foreign)))
    return None


# ---- serialising several models through one AutoCAD (layer 3) ---------------
# AutoCAD is single threaded: one instance runs one command at a time, and every op
# resolves MdiActiveDocument. So "the agent works on three models at once" means, inside
# one instance, ACTIVATE-then-FIRE repeated - and two python processes doing that at the
# same time can interleave (A activates, B activates, A fires into B's document). The
# guard would refuse rather than corrupt, but a refusal storm is not parallel work.
# One lock per AutoCAD WINDOW, held across activate+fire, makes it orderly. Different
# instances have different windows, so genuinely parallel work stays parallel.
_LOCKDIR = os.path.join(PLUG, "ch", ".locks")
_LOCK_STALE = 180.0


def _instance_key(app=None):
    try:
        return "hwnd%d" % int((app or _app()).HWND)
    except Exception:
        return "global"


def _lock_acquire(key, timeout=90.0):
    try:
        os.makedirs(_LOCKDIR, exist_ok=True)
    except Exception:
        return None
    path = os.path.join(_LOCKDIR, key + ".lock")
    deadline = time.time() + timeout
    while True:
        try:
            fd = os.open(path, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
            os.write(fd, ("%d %s" % (os.getpid(), EXPECT_DWG or "-")).encode("utf-8"))
            os.close(fd)
            return path
        except FileExistsError:
            try:
                age = time.time() - os.path.getmtime(path)
                if age > _LOCK_STALE:        # a killed process must not block the machine
                    os.remove(path)
                    continue
            except Exception:
                pass
            if time.time() > deadline:
                return None
            time.sleep(0.15)
        except Exception:
            return None


def _lock_release(path):
    try:
        if path and os.path.exists(path):
            os.remove(path)
    except Exception:
        pass


class model(object):
    """Bind this process to a model for the duration of a block.

    with eb_api.model(r"...\\lesson-7\\pump.dwg", task="rebuild chamber B"):
        eb_api.run("beam", ...)          # cannot land anywhere else

    Restores the previous binding on exit, so nesting a second model inside a routine
    cannot leave the caller pointing somewhere it did not choose.
    """

    def __init__(self, dwg, task=None, project=None, force=False):
        self.dwg, self.task, self.project, self.force = dwg, task, project, force
        self._prev = None

    def __enter__(self):
        self._prev = ws.current()
        use(self.dwg, task=self.task, project=self.project, force=self.force)
        return self

    def __exit__(self, *exc):
        global EXPECT_DWG
        if self._prev and self._prev.get("dwg"):
            try:
                ws.switch(self._prev["dwg"])
                EXPECT_DWG = self._prev.get("name")
            except Exception:
                pass
        return False


def _active_doc_name(app=None):
    try:
        return os.path.basename((app or _app()).ActiveDocument.Name)
    except Exception:
        return ""


def _activate_pinned(name):
    """Bring OUR document forward inside our own AutoCAD instance.

    This is the document-routing primitive: with several models open in one AutoCAD, an op
    reaches its model by making that document active first (the plugin resolves
    MdiActiveDocument, 71 places -- it has no per-op document argument).
    ⛔ Never switches away from a document someone else holds.
    """
    try:
        app = _app()
        act = ""
        try:
            act = os.path.basename(app.ActiveDocument.Name)
        except Exception:
            pass
        if act and act.lower() == name.lower():
            return True
        # ⛔ Never switch away from a document that is not registered as OURS. An
        # unregistered drawing in front is, by the project's oldest rule, someone else's:
        # "never close what you did not open" (06/08/2026) -- and switching the view of a
        # drawing Amir is reading is the same intrusion as closing it. Every document the
        # agent opens registers itself, so ours are always known.
        rec = ws.find(act) if act else None
        if act and (rec is None or rec.get("owner") != ws.OWNER_AGENT):
            return False
        for i in range(app.Documents.Count):
            d = app.Documents.Item(i)
            if os.path.basename(d.Name).lower() == name.lower():
                d.Activate()
                time.sleep(0.4)
                return os.path.basename(app.ActiveDocument.Name).lower() == name.lower()
    except Exception:
        pass
    return False


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

    🛑 CORRECTED 17/08/2026, measured -- the sentence that used to end this docstring said
    the two instances "cannot be told apart by name -- only by asking each one which
    drawings it holds". That is FALSE, and it mattered: with two AutoCAD processes running,
    the Running Object Table shows TWO entries under the same class moniker
    `!{0B628DE4-07AD-4284-81CA-5B439F67C5E6}` and **both bind to the same instance** -- the
    earliest-registered one. The second process is not reachable over COM at all. Killing
    the first made the second reachable within seconds, so the registration is real but
    shadowed. A DOCUMENT moniker for the second instance's drawing appeared once in the ROT
    and was gone minutes later, so that is not a dependable route either.
    ⇒ This function can return at most ONE instance. See autocad_reachability(), and
    knowledge/learning/findings/PARALLEL-MODELS.md for what that means for working in
    parallel with Amir.
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
            try:
                fingerprint = int(app.HWND)          # the window IS the instance
            except Exception:
                fingerprint = tuple(sorted(app.Documents.Item(i).Name
                                           for i in range(app.Documents.Count)))
            if fingerprint in seen:   # the ROT lists the class moniker twice; one instance
                continue
            seen.add(fingerprint)
            docs = [app.Documents.Item(i).Name for i in range(app.Documents.Count)]
            out.append((app, docs))
        except Exception:
            continue
    return out


def autocad_reachability():
    """How many AutoCADs are RUNNING vs how many can be addressed. Measured, not assumed.

    Returns {"processes": n, "reachable": m, "docs": [...], "hidden": n - m}.
    On this machine m is always 0 or 1: COM hands out the earliest-registered instance and
    shadows every later one (measured 17/08/2026, both directions -- the hidden instance
    became reachable within seconds of the first one exiting).
    """
    import subprocess
    procs = 0
    try:
        procs = len([l for l in subprocess.run(
            ["tasklist", "/FI", "IMAGENAME eq acad.exe", "/NH"],
            capture_output=True, text=True, timeout=30).stdout.splitlines()
            if "acad.exe" in l.lower()])
    except Exception:
        pass
    inst = []
    try:
        inst = acad_instances()
    except Exception:
        pass
    docs = [os.path.basename(d) for (_a, ds) in inst for d in ds]
    out = {"processes": procs, "reachable": len(inst), "docs": docs,
           "hidden": max(0, procs - len(inst))}
    # ⭐ measured 17/08/2026: AutoCAD does not register in the Running Object Table AT ALL
    # while a modal dialog is up. After a forced kill the "Drawing Recovery" dialog comes
    # back with the next launch and holds it there indefinitely -- eight probes over three
    # minutes all said reachable=0 with the process alive and Responding=True.
    # ⇒ processes>0 with reachable=0 means A DIALOG, not a dead AutoCAD.
    if procs > 0 and not inst:
        out["hint"] = ("AutoCAD is running but has not registered with COM -- a modal dialog "
                       "is almost certainly holding it (Drawing Recovery / AutoCAD Error "
                       "Report after a kill). Close it and it registers within seconds. "
                       "⛔ never press 'Send Report'; never 'Recover' -- the saved file is good.")
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
        # ⭐ DE-DUPLICATE ON THE WINDOW HANDLE FIRST. This file already records the measurement
        # (17/08/2026): with two acad.exe running, the ROT publishes TWO entries under the same
        # moniker and **both bind to the same instance** -- identical HWND, identical document
        # list. So "open in 2 instances" was a false positive, and on 19/08/2026 it aborted a
        # model-6 rebuild eleven minutes in, with the two document lists printed side by side
        # and character-for-character identical. The window IS the instance (the fingerprint
        # used elsewhere in this file), so collapse candidates that share one.
        seen = {}
        for a, d in cands:
            try:
                k = int(a.HWND)
            except Exception:
                k = id(a)
            if k not in seen:
                seen[k] = (a, d)
        if len(seen) < len(cands):
            cands = list(seen.values())
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
            extra = ""
            try:
                r = autocad_reachability()
                if r["hidden"]:
                    extra = (" -- and %d further AutoCAD process(es) are running that COM "
                             "CANNOT reach: only the earliest-registered instance is "
                             "addressable (measured 17/08/2026). If '%s' is open in one of "
                             "those, close that AutoCAD or close this one; there is no way "
                             "to address both." % (r["hidden"], want))
            except Exception:
                pass
            raise RuntimeError(
                "attached to the wrong AutoCAD: it holds %s, not '%s' -- refused%s" %
                ([os.path.basename(n) for n in names], want, extra))
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


def bootstrap(timeout=180, dwg=None):
    """Cold-start the whole stack: launch AutoCAD if down, wait ready,
    trust plugin dir, NETLOAD, ping. Returns a status dict.

    ⛔ CORRECTED 13/08/2026 -- the launch passes an existing DRAWING, never a template.
    `/t <template>` creates a new Drawing1, and a new drawing makes ProStructures raise
    the modal "Measurement Unit" prompt, which CANNOT be dismissed from code (six
    attempts, 10/08/2026). That was already recorded in
    knowledge/learning/findings/LETHAL-CALLS-do-not-invoke.md § Recovery, but this
    function still carried the retracted route -- the exact failure the registration
    rule exists to catch. Opening an existing drawing never asks.
    """
    import subprocess
    import pythoncom
    import win32com.client
    # launch if not running
    try:
        win32com.client.GetActiveObject("AutoCAD.Application")
    except Exception:
        if not (dwg and os.path.exists(dwg)):
            return {"ok": False, "error":
                    "AutoCAD is down and no existing drawing was given. Launching from a "
                    "template raises the un-closable 'Measurement Unit' dialog -- call "
                    "bootstrap(dwg=<an existing .dwg>). See LETHAL-CALLS-do-not-invoke.md."}
        subprocess.Popen([ACAD_EXE, dwg, "/p", PS_ARG,
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
    """Robustly obtain (app, doc), retrying transient COM dispatch flakes.

    A REFUSAL is not a flake: "attached to the wrong AutoCAD" will still be true in eight
    seconds, and retrying it only delays the answer (measured: 9.4 s to say something that
    was known immediately).
    """
    last = None
    for _ in range(tries):
        try:
            app = _app()
            doc = app.ActiveDocument
            return app, doc
        except Exception as e:
            last = e
            msg = str(e)
            if ("attached to the wrong AutoCAD" in msg or
                    "no AutoCAD instance has" in msg or
                    "refusing to guess" in msg):
                raise
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
            held = ws.foreign_names()      # 17/08: a model someone else registered is not a stray
            for i in range(app.Documents.Count):
                nm = app.Documents.Item(i).Name
                if os.path.basename(nm).lower() in held:
                    continue
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
    st = bootstrap(dwg=dwg_path if os.path.exists(dwg_path) else None)
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
    """Always re-acquire a fresh app+doc (COM objects go stale after Documents.Add).

    ⭐ 17/08/2026 -- THIS is where the pin used to be lost. `_app()` chooses the instance
    holding the pinned drawing and refuses to guess, but every command actually went out
    through here, which called GetActiveObject directly: whichever AutoCAD registered in
    the Running Object Table FIRST. With two processes that is a coin toss, and the losing
    side is Amir's own session -- `_netload` goes through here too, and it writes FILEDIA
    and NETLOADs a DLL. Routing it through _app() is what makes "work in parallel" safe.
    """
    import pythoncom
    import win32com.client
    pythoncom.CoInitialize()
    try:
        app = _app()
    except Exception:
        if EXPECT_DWG:
            raise                     # pinned and not found: refuse, never fall back
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


def _fire_and_match(reqid, wait, res_path=None):
    """Send EB_RUN and return ONLY a result carrying our reqid (never stale)."""
    if not _send_cmd(RUN_CMD + "\n"):
        return None
    tag = "reqid=" + reqid
    res_path = res_path or _res_path()
    deadline = time.time() + wait + 8
    while time.time() < deadline:
        for p in (res_path, RES):        # v185 answers in the channel; v184 in the root
            try:
                if os.path.exists(p):
                    r = open(p, encoding="utf-8-sig").read().strip()
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


def _my_acad_pids():
    """The pid of the AutoCAD instance THIS process is attached to -- and only that one.

    ⭐ FIXED 19/08/2026, and it is what makes "Amir works in parallel" actually work. The
    guard already narrowed itself to AutoCAD's own dialogs (09/08), but `_acad_pids()` returns
    EVERY acad.exe on the machine -- so the moment Amir opened a ProSteel Copy/Move/Mirror
    dialog in HIS OWN second instance, the agent refused every op. Measured cost: 96 shapes
    and 86 plates silently not created in the middle of a model-6 rebuild, with the notes
    reading `EB_DIALOG ... ProSteel Copy/Move/Mirror/Align` -- his dialog, our stop.
    His instance is invisible to COM by design, so the only honest scope is the instance we
    hold: `app.HWND` IS the instance (the fingerprint used elsewhere in this file), and its
    window's pid is ours. Falls back to the desktop-wide scan when that cannot be read --
    blocking wrongly is still safer than walking into a dialog.
    """
    try:
        import ctypes
        from ctypes import wintypes
        # GetActiveObject, deliberately, and NOT _app(): the ROT exposes exactly ONE
        # addressable instance (measured 17/08 -- a second acad.exe is invisible, not merely
        # indistinguishable), so this is the instance we hold. Going through _app() here would
        # also risk recursing back into this guard.
        import win32com.client as _w
        app = _w.GetActiveObject("AutoCAD.Application")
        hwnd = int(app.HWND)
        pid = wintypes.DWORD()
        ctypes.windll.user32.GetWindowThreadProcessId(wintypes.HWND(hwnd), ctypes.byref(pid))
        return set([pid.value]) if pid.value else set()
    except Exception:
        return set()


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
        pids = _my_acad_pids() or _acad_pids()

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


#: Eretz Barzel's bolt grade, set by Amir on 13/08/2026:
#: "אנחנו עובדים לרוב עם ברגים 8.8. אני צריך שזו תהיה ברירת המחדל שלך."
#: `8.8S` is the AS/NZS snug-tightened category, and it is what his own PS_BOLT run
#: produced ("M20 x 60 8.8/S"). The helpers below used to default to DIN6914 — a
#: 10.9 HV bolt, i.e. the wrong grade entirely.
#: ⚠️ This table pairs on +2 mm clearance ONLY (18→M16, 22→M20, 26→M24) and REFUSES
#: ⌀19 and ⌀23. Full measured matrix:
#:   knowledge/learning/findings/BOLT-STYLES-AND-HOLES.md
DEFAULT_BOLT_STYLE = "8.8S"

#: ops whose `style=` really is a BOLT style (plate9's `style` is something else)
_BOLT_OPS = ("bolt", "boltfield", "boltparts", "boltsingle", "threadedrod",
             "nutonly", "conn_bolted")


def run(op, wait=5.0, _log=True, **kw):
    import uuid
    reqid = uuid.uuid4().hex[:8]
    # ⛔ 13/08/2026: `dbase` on an .mdb spins AutoCAD in an endless loop -- CPU pinned,
    # memory flat, no recovery, the session has to be killed. PsDBaseDatabase reads
    # dBASE (.dbf) files. See LETHAL-CALLS-do-not-invoke.md.
    if op == "dbase":
        f = str(kw.get("file", ""))
        if not f.lower().endswith(".dbf"):
            return ("EB_ERR dbase refuses '%s' -- PsDBaseDatabase takes .dbf only; an .mdb "
                    "HANGS AutoCAD (measured 13/08/2026, session had to be killed). "
                    "-- refused, nothing was executed" % f)
    # the bolt grade is a standing decision, not a per-call choice
    if op in _BOLT_OPS and "style" not in kw:
        kw = dict(kw, style=DEFAULT_BOLT_STYLE)

    # ---- WORK-SESSION GATE (17/08/2026) ----------------------------------
    # Three refusals that used to be silent overrides. Diagnostics are exempt: ping and
    # whoami change nothing and are exactly what you need in order to fix the other two.
    gate = _session_gate(op, kw)
    if gate:
        return gate
    ses = ws.current()
    pin = (ses or {}).get("name") or EXPECT_DWG or _read_pin()
    diag = op in _UNGATED_OPS
    if not diag:
        if pin and "dwg" not in kw:
            kw = dict(kw, dwg=pin)
        if ses and ses.get("dwg") and os.sep in str(ses.get("dwg")) and "dwgpath" not in kw:
            # v185 compares the FULL PATH: two projects may each hold a `test.dwg`
            kw = dict(kw, dwgpath=ses["dwg"])

    # Pre-flight. This used to swallow every failure and fall through to a fire that could
    # not land, so an unreachable AutoCAD came back as EB_TIMEOUT -- the one message that
    # says nothing. Now the reason is the answer.
    app = None
    try:
        app, _ = _app_doc()
    except Exception as e:
        if pin and not diag:
            return "EB_ERR unreachable: %s" % e
    if app is not None:
        try:
            if not _quiescent(app):
                return "EB_BUSY AutoCAD עסוק - לחץ ESC ונסה שוב"
            _dlgs = modal_dialogs()
            if _dlgs:
                return ("EB_DIALOG a dialog is waiting and AutoCAD reports itself idle: %s"
                        % "; ".join(_dlgs))
        except Exception:
            pass
    # A diagnostic goes to the SHARED mailbox on purpose: ping/whoami/docs are what you
    # reach for when the wrong document is in front, and a per-model channel is exactly
    # what the plugin would not find in that state. They are read-only and reqid-matched,
    # and only one instance is ever fired at (see _app()).
    cmd_path = CMD if diag else _cmd_path()
    res_path = RES if diag else _res_path()
    body = "\n".join(["reqid=" + reqid, "op=" + op] +
                     ["%s=%s" % (k, v) for k, v in kw.items()]) + "\n"
    # hold the instance for activate+fire: see _lock_acquire
    lock = None if diag else _lock_acquire(_instance_key())
    try:
        if pin and not diag and not _activate_pinned(pin):
            # could not bring our model forward. If the reason is that someone else's model
            # is in front, say so HERE: firing anyway would answer with a timeout, and a
            # timeout is the one message that means "I have no idea what happened".
            act = _active_doc_name()
            rec = ws.find(act) if act else None
            if act and act.lower() != str(pin).lower() and (
                    rec is None or rec.get("owner") != ws.OWNER_AGENT):
                return ("EB_ERR blocked: this AutoCAD has '%s' in front%s -- refusing to "
                        "switch away from a drawing that is not mine, nothing was executed. "
                        "⚠️ ONE AutoCAD window shows ONE document at a time, so working '%s' "
                        "here would pull the view off '%s'. Three ways out: close/minimise "
                        "that drawing, hand it to me (`worksession.py open \"%s\"`), or -- if "
                        "you want to keep working in it while I model -- open YOUR OWN AutoCAD "
                        "*after* mine, where I cannot reach you at all."
                        % (act, (" and it is held by owner=" + str(rec.get("owner")))
                           if rec else " and it is not registered to anyone", pin, act, act))
        with open(cmd_path, "w", encoding="utf-8") as f:
            f.write(body)
        res = _fire_and_match(reqid, wait, res_path)
    finally:
        _lock_release(lock)
    if res is None:                      # watchdog: maybe command not registered
        lp = ""
        try:
            _, d = _fresh_doc()
            lp = d.GetVariable("LASTPROMPT") or ""
        except Exception:
            pass
        if "Unknown command" in lp or "NETLOAD" in lp:
            _netload()
            with open(cmd_path, "w", encoding="utf-8") as f:
                f.write(body)
            res = _fire_and_match(reqid, wait, res_path)
    # ---- self-heal ONE wrong-drawing refusal by activating the pinned document ----
    # Several models open in one AutoCAD is now normal (Amir, 17/08). The guard is right
    # to refuse, but the fix is mechanical: bring OUR document forward and run again.
    # Only once, only when the target is ours, and never when the active document belongs
    # to someone else -- switching away from a drawing Amir is working in is not ours to do.
    if res and res.startswith("EB_ERR wrongdoc") and pin:
        if _activate_pinned(pin):
            # a NEW reqid: the refusal sitting in the result file carries the old one, and
            # _fire_and_match would match it instantly and call the refusal an answer
            reqid2 = uuid.uuid4().hex[:8]
            body2 = body.replace("reqid=" + reqid, "reqid=" + reqid2, 1)
            with open(cmd_path, "w", encoding="utf-8") as f:
                f.write(body2)
            res2 = _fire_and_match(reqid2, wait, res_path)
            if res2 is not None:
                res = res2
    # The plugin consumes the command file when it reads it; delete it here too, so a
    # command that was never picked up cannot be executed later by an unrelated EB_RUN.
    try:
        if os.path.exists(cmd_path):
            os.remove(cmd_path)
    except Exception:
        pass
    if res is None:
        return "EB_TIMEOUT reqid=%s (no matching result; command may not have executed)" % reqid
    if _log and res.startswith("EB_OK") and op in ("beam", "plate", "bolt", "boltfield", "conn_bolted", "miter"):
        _model_log(op, kw, res)
    return res


def ensure_doc(dwg_path):
    """Guarantee the project's DWG is the ACTIVE document before modeling."""
    want = os.path.basename(dwg_path).lower()
    held = ws.find(dwg_path)
    if held and held.get("owner") != ws.OWNER_AGENT:
        return False                      # someone else's model: not ours to bring up
    r = run("whoami")
    if want in r.lower():
        return True
    try:
        app = _app()
        for d in list(app.Documents):
            if d.Name.lower() == want:
                d.Activate()
                ws.open_session(dwg_path)
                return True
        if os.path.exists(dwg_path):
            app.Documents.Open(dwg_path)
            ws.open_session(dwg_path)
            return True
    except Exception:
        pass
    return False


def open_model(dwg_path, task=None, project=None, activate=True):
    """Open ANOTHER model in the AutoCAD we are already working in, and register it.

    This is how the agent comes to hold several models at once (Amir, 17/08/2026: "לכל
    פרויקט יהיה מודל משלו ואני רוצה שהוא יעבוד על כל אחד בנפרד"). Registering is not
    bookkeeping: an unregistered drawing in front is treated as somebody else's and the
    agent will not switch away from it -- so a model opened silently would freeze the
    instance for everyone.
    """
    app = _app()                              # resolve the instance BEFORE re-pinning
    ses = ws.open_session(dwg_path, project=project, task=task)   # refuses a held model
    global EXPECT_DWG
    EXPECT_DWG = ses["name"]
    for i in range(app.Documents.Count):
        d = app.Documents.Item(i)
        if os.path.basename(d.Name).lower() == ses["name"].lower():
            if activate:
                d.Activate()
                time.sleep(0.4)
            return ses
    if not os.path.exists(ses["dwg"]):
        raise RuntimeError("no such drawing: %s" % ses["dwg"])
    app.Documents.Open(ses["dwg"])
    time.sleep(1.0)
    return ses


def _stamp():
    try:
        return os.path.getmtime(_res_path())
    except OSError:
        return 0


def _model_log(op, kw, res):
    """Phase G: record every native create into the active project's model_log.

    ⚠️ 13/08/2026: this used to `makedirs` the pid unconditionally, and `data/project.txt`
    still held a pre-reorg id (`שיעור-2`) -- so lesson 6's creates RESURRECTED a Hebrew
    folder the 12/08 cleanup had removed, in a repo whose convention is lowercase English.
    A logger must never invent a project directory: log only into one that EXISTS, and
    otherwise fall back to the folder of the pinned drawing.

    ⭐ 17/08/2026 -- and the SAME failure happened again from the other side: `data/project.txt`
    still said `lesson-6`, so a beam created in a sandbox model during the parallel-work test
    was logged into **lesson 6's** model_log. A global "active project" pointer cannot answer
    a per-model question. The work session knows which drawing this op is in, so the log now
    goes to THAT DRAWING'S OWN FOLDER, and the stale pointer is only the last resort.
    """
    try:
        d = ""
        ses = ws.current()
        if ses and ses.get("dwg") and (os.sep in str(ses["dwg"])):
            cand = os.path.dirname(os.path.abspath(ses["dwg"]))
            if os.path.isdir(cand):
                d = cand
        if not d:
            pid = open(ACTIVE, encoding="utf-8").read().strip() if os.path.exists(ACTIVE) else ""
            d = os.path.join(PROJECTS, pid) if pid else ""
        if not d or not os.path.isdir(d):
            pin = EXPECT_DWG or _read_pin() or ""
            alt = os.path.join(PROJECTS, os.path.splitext(os.path.basename(pin))[0]) if pin else ""
            if not (alt and os.path.isdir(alt)):
                return
            d = alt
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



# ===========================================================================
#  v190  BATCH -- many ops in ONE round trip
# ===========================================================================
# Measured 20/08/2026, on the night the bridge model arrived: the file protocol costs
# 0.28 s per op no matter how small the op is (ping 0.336, props 0.284, mods 0.251).
# A 1:1 rebuild of that model is ~43,000 ops -- 3.4 hours of protocol for work the
# transactions do in minutes. The plugin now has a dispatcher it can call in a loop
# (`op=batch file=...`), and this is the client side of it.
#
# ⭐ EVERY ITEM STILL GETS ITS OWN RESULT LINE. A batch that answered only with a total
# would hide WHICH part failed, and on a 1:1 rebuild the failures are the finding.
# ⚠️ The work-session gate, the wrong-drawing guard and the instance lock all apply to
# the batch INVOCATION -- which is right: every item lands in the active document, so
# one guard on the way in protects the whole file.
_TAB = chr(9)


def batch(items, wait=None, file="eb_batch.txt", out="eb_batch_out.txt", stop=False,
          chunk=0, on_chunk=None):
    """items: sequence of (op, kwargs-dict). Returns [(i, op, result_text), ...].

    chunk>0 splits the work into several invocations of that size -- use it when a single
    file would run longer than `wait`, and to get a save in between.
    """
    items = list(items)
    if chunk and len(items) > chunk:
        outp = []
        for a in range(0, len(items), chunk):
            part = batch(items[a:a + chunk], wait=wait, file=file, out=out, stop=stop)
            outp.extend([(a + i, o, r) for (i, o, r) in part])
            if on_chunk:
                on_chunk(a + len(part), len(items), outp)
        return outp
    lines = []
    for op, kw in items:
        kw = dict(kw or {})
        if op in _BOLT_OPS and "style" not in kw:
            kw["style"] = DEFAULT_BOLT_STYLE          # the standing decision, per item
        if op == "dbase":
            raise ValueError("dbase inside a batch is refused -- see LETHAL-CALLS")
        if op == "batch":
            raise ValueError("nested batch is refused")
        lines.append(_TAB.join(["op=" + op] + ["%s=%s" % (k, v) for k, v in kw.items()]))
    ch = channel()
    with io.open(os.path.join(ch, file), "w", encoding="utf-8", newline=chr(10)) as f:
        f.write(chr(10).join(lines) + chr(10))
    if wait is None:
        wait = max(60.0, 0.05 * len(lines) + 30.0)
    res = run("batch", wait=wait, file=file, out=out)
    rows = []
    try:
        with io.open(os.path.join(ch, out), encoding="utf-8-sig") as f:
            for ln in f:
                p3 = ln.rstrip(chr(10)).split(_TAB)
                if len(p3) >= 3:
                    rows.append((int(p3[0]), p3[1], p3[2]))
    except Exception:
        pass
    if not res or not res.startswith("EB_OK"):
        rows.append((-1, "batch", res or "EB_TIMEOUT (no result)"))
    return rows


def batch_progress():
    """How far the batch running right now has got (written every 250 items)."""
    try:
        return io.open(os.path.join(channel(), "eb_batch_progress.txt"),
                       encoding="utf-8-sig").read().strip()
    except Exception:
        return ""


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


def bolt(p1, p2, dia=20, style=DEFAULT_BOLT_STYLE, hosts=""):
    return run("bolt", p1=_pt(p1), p2=_pt(p2), dia=dia, style=style, hosts=hosts)


def boltfield(center, nx=2, ny=2, sx=100, sy=100, dia=20, gap=60, style=DEFAULT_BOLT_STYLE, hosts=""):
    return run("boltfield", center=_pt(center), nx=nx, ny=ny, sx=sx, sy=sy,
               dia=dia, gap=gap, style=style, hosts=hosts)


def conn_bolted(at, pl=220, pw=220, pt=20, gap=60, nx=2, ny=2, sx=100, sy=100, dia=20, style=DEFAULT_BOLT_STYLE):
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

    def _bbox(s):
        """The WORLD bounding box the plugin appends to PLATE and BOLT rows: 'min;max'.

        ⚠️ MEASURED 10/08/2026 (B.21 audit): EVERY plate and EVERY bolt in the model reports
        InsertPoint = 0,0,0 -- all 305 bolts, all 241 plates. B.9's audit fixed the plugin to
        emit a world bbox for exactly this reason, and this parser never read it. So any code
        that located a plate or a bolt through els[i]['insert'] found it at the origin, and any
        "how many bolts are in this region" question answered ZERO for every region of the
        model. A false negative that looks identical to a real one -- the same failure class
        B.9 found in the plugin, sitting here in the client.
        """
        try:
            lo, hi = s.split(";")
            return (_xyz(lo), _xyz(hi))
        except Exception:
            return None

    def _center(bb, fallback):
        if not bb:
            return fallback
        return tuple((bb[0][i] + bb[1][i]) / 2.0 for i in range(3))

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
                bb = _bbox(f[10]) if len(f) >= 11 else None
                ins = _xyz(f[5])
                els.append({"kind": "plate", "handle": f[1], "l": float(f[2] or 0),
                            "w": float(f[3] or 0), "t": float(f[4] or 0),
                            "insert": ins,
                            "poly": [_xyz(p) for p in f[6].split(";") if p],
                            "material": f[7], "name": f[8], "cls": f[9],
                            "bbox": bb, "center": _center(bb, ins)})
            elif k == "BOLT" and len(f) >= 9:
                bb = _bbox(f[9]) if len(f) >= 10 else None
                ins = _xyz(f[6])
                els.append({"kind": "bolt", "handle": f[1], "diameter": float(f[2] or 0),
                            "style": f[3], "count": int(float(f[4] or 0)),
                            "length": float(f[5] or 0), "insert": ins,
                            "name": f[7], "cls": f[8],
                            "bbox": bb, "center": _center(bb, ins)})
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

def delete(handle, dwg=None):
    """Erase one entity by handle via COM (keeps native semantics).

    Uses _app_doc, not _app().ActiveDocument: a COM call issued right after a heavy
    plugin run (a collision check over 132 parts) raises AttributeError on
    ActiveDocument while AutoCAD is still settling. _app_doc retries that away.

    ⛔⛔ GATED SINCE 18/08/2026. This is the most destructive call in the file and it was
    the only one that consulted nothing: it erased from whatever document was in front. An
    op cannot do that -- `_session_gate` refuses another owner's model and `_activate_pinned`
    refuses to switch away from a drawing that is not ours -- so `delete` now goes through
    both, and then READS THE ACTIVE DOCUMENT BACK before erasing anything. Caught while the
    agent was locked to Amir's model and about to wipe a rebuild copy.
    """
    kw = {} if dwg is None else {"dwg": dwg}
    refusal = _session_gate("delete", kw)      # "delete" is in _WRITING_OPS via this call
    if refusal:
        return refusal
    ses = ws.current()
    want = os.path.basename(str(dwg or (ses or {}).get("name") or EXPECT_DWG or ""))
    if not want:
        return ("EB_ERR no work session: delete refuses to erase without one -- "
                "enter the model first with eb_api.use(<dwg>)")
    app, doc = _app_doc()
    if not _quiescent(app):
        return "EB_BUSY"
    if not _activate_pinned(want):
        return ("EB_ERR blocked: could not bring '%s' forward -- refusing to erase, "
                "nothing was executed" % want)
    act = _active_doc_name()
    if not act or act.lower() != want.lower():
        return ("EB_ERR wrongdoc: expected='%s' active='%s' -- refusing to erase, "
                "nothing was executed" % (want, act))
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
    path = _os.path.join(PROJECTS, "sandbox", EXPECT_DWG or "sandbox.dwg")
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
