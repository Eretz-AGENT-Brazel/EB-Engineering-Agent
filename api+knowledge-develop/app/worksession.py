"""worksession.py - WHICH MODEL AM I ALLOWED TO TOUCH, and who else is working.

Amir, 17/08/2026: "אני צריך לעבוד על פרויקטים נוספים במקביל ... אני רוצה שנבצע הסדרה לזה",
and next: "לכל פרויקט יהיה מודל משלו ואני רוצה שהוא יעבוד על כל אחד בנפרד".

Two different needs, one registry:
  1. Amir opens HIS drawing while the agent works in another one. Nothing the agent does
     may reach that drawing -- not an op, not a NETLOAD, not a system variable.
  2. The agent itself holds several models at once, each with its own task.

WHAT WAS BROKEN BEFORE THIS FILE (measured 17/08/2026, from the code and the live state):
  * The pin was ONE basename in ONE file (`eb_expect_dwg.txt`), with no timestamp, no owner
    and no project. On the morning of 17/08 it still read `test-2026-08-13.dwg` (MARTAR)
    while the last op that actually ran carried `lesson 7- Pump Chamber - REBUILD.dwg`.
    A pin that survives into a session it knows nothing about is not protection.
  * `run()` injected the pin only `if "dwg" not in kw` -- so an explicit dwg= silently
    OVERRODE the pin. A lock you can overrule by accident is a default, not a lock.
  * Comparison was by BASENAME. Two projects may hold `test.dwg` in different folders.

The rules this file enforces:
  R1  A model is entered explicitly (open) and left explicitly (close).
  R2  A session carries the FULL PATH. The basename is a label, never the identity.
  R3  Every session has an owner. `amir` means HANDS OFF: the agent refuses to send
      anything at all to that drawing, including a NETLOAD or a variable write.
  R4  A session older than STALE_HOURS, or written by a different agent session, must be
      re-confirmed before the first op. Yesterday's pin is not today's consent.
  R5  The slot (channel id) is derived from the drawing name by a function that is
      implemented IDENTICALLY here and in the plugin, so both sides agree without talking.

State lives in data/worksession.json - machine state, not repo content (gitignored).
"""

import json
import os
import sys
import time
import uuid

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
STATE = os.path.join(ROOT, "data", "worksession.json")
CHROOT = os.path.join(HERE, "plugin", "ch")

STALE_HOURS = 10.0          # a work shift. Longer than a day's session, shorter than a night
OWNER_AGENT = "agent"
OWNER_AMIR = "amir"


# ---------------------------------------------------------------- slot identity
def _fnv1a32(s):
    """FNV-1a, 32 bit. Chosen because it is four lines in BOTH C# and Python.

    The plugin must compute the same slot for the same drawing without being told, so the
    hash cannot be a language built-in (python's hash() is salted per process, C#'s
    GetHashCode is not specified across runtimes).
    """
    h = 0x811C9DC5
    for b in s.encode("utf-8"):
        h ^= b
        h = (h * 0x01000193) & 0xFFFFFFFF
    return h


def slot_of(dwg):
    """Channel id for a drawing. MUST match ApiCmds.SlotOf() in the plugin, char for char.

    Readable prefix + hash tail: the prefix is for a human looking at the folder, the hash
    is what makes it unique. Truncating the prefix alone could collide two long names.

    ⭐ The hash is over the FULL PATH whenever one is known. Hashing the basename would
    give `projects\alpha\test.dwg` and `projects\beta\test.dwg` the SAME mailbox -- the
    exact collision this whole design exists to prevent. AutoCAD's `Document.Name` is a
    full path, so the plugin can compute the same value without being told; when only a
    bare name is known, both sides fall back to the name slot.
    """
    s = str(dwg or "").strip()
    name = os.path.basename(s).lower()
    ident = s.replace("/", "\\").lower() if ("\\" in s or "/" in s) else name
    keep = []
    for ch in name:
        keep.append(ch if ("a" <= ch <= "z" or "0" <= ch <= "9") else "_")
    return "%s_%08x" % ("".join(keep)[:24], _fnv1a32(ident))


def key_of(dwg, st=None):
    """Registry key for a drawing, given either a full path or a bare basename.

    Sessions are keyed by the full-path slot. A caller that knows only `test.dwg` still
    has to find it -- otherwise the hands-off check would look up a key that cannot exist
    and wave the op through, which is the failure mode this file is built to prevent.
    """
    s = str(dwg or "").strip()
    if not s:
        return None
    k = slot_of(s)
    st = st if st is not None else load()
    if k in st.get("sessions", {}):
        return k
    name = os.path.basename(s).lower()
    for key, ses in st.get("sessions", {}).items():
        if str(ses.get("name", "")).lower() == name:
            return key
    return k


def find(dwg, st=None):
    """The session record for a drawing (full path or basename), or None."""
    st = st if st is not None else load()
    k = key_of(dwg, st)
    return st.get("sessions", {}).get(k) if k else None


def channel_dir(dwg, make=False):
    """Mailbox directory for a drawing.

    Resolves through the registry first, so `channel_dir("test.dwg")` and
    `channel_dir(r"C:\\...\\test.dwg")` land in the SAME place once the model is open --
    otherwise the caller that knows only the name would write its command into a mailbox
    the plugin never reads.
    """
    ses = find(dwg)
    if ses and ses.get("dwg"):
        dwg = ses["dwg"]
    d = os.path.join(CHROOT, slot_of(dwg))
    if make:
        try:
            os.makedirs(d, exist_ok=True)
        except Exception:
            pass
    return d


# ---------------------------------------------------------------- state file
def _blank():
    return {"version": 1, "current": None, "sessions": {}}


def load():
    try:
        with open(STATE, encoding="utf-8") as f:
            st = json.load(f)
        if not isinstance(st, dict) or "sessions" not in st:
            return _blank()
        return st
    except Exception:
        return _blank()


def save(st):
    try:
        os.makedirs(os.path.dirname(STATE), exist_ok=True)
        tmp = STATE + ".tmp"
        with open(tmp, "w", encoding="utf-8") as f:
            json.dump(st, f, ensure_ascii=False, indent=2)
        if os.path.exists(STATE):
            os.remove(STATE)
        os.replace(tmp, STATE)
    except Exception as e:
        sys.stderr.write("[worksession] could not save state: %s\n" % e)
    return st


def agent_session_id():
    """Identifies THIS run of the agent. Set EB_AGENT_SESSION to share one id across the
    many short-lived python processes a single conversation spawns."""
    v = os.environ.get("EB_AGENT_SESSION", "").strip()
    if v:
        return v
    # fall back to a per-day id: separate days are separate consents (R4)
    return "day-" + time.strftime("%Y%m%d")


# ---------------------------------------------------------------- open / close
def open_session(dwg, project=None, task=None, owner=OWNER_AGENT, force=False):
    """Enter a model. Returns the session dict.

    Refuses to enter a drawing that someone else owns unless force=True -- entering a
    model Amir declared his own is exactly the accident this file exists to stop.
    """
    dwg = os.path.abspath(dwg) if os.sep in str(dwg) or "/" in str(dwg) else str(dwg)
    st = load()
    key = slot_of(dwg)
    oldkey = key_of(dwg, st)
    prev = st["sessions"].get(oldkey)
    if prev and oldkey != key:
        # the model was first registered by bare name and we now know its path: move the
        # record to the path slot rather than leaving two half-records for one model
        st["sessions"].pop(oldkey, None)
        if st.get("current") == oldkey:
            st["current"] = key
    if prev and prev.get("owner") != owner and not force:
        raise RuntimeError(
            "'%s' is already held by owner=%s (since %s). Refusing to take it over; "
            "close it first or pass force=True." % (os.path.basename(dwg),
                                                    prev.get("owner"), prev.get("opened")))
    now = time.time()
    ses = {
        "dwg": dwg,
        "name": os.path.basename(dwg),
        "slot": key,
        "project": project or _guess_project(dwg),
        "task": task or (prev or {}).get("task") or "",
        "owner": owner,
        "opened": (prev or {}).get("opened") or now,
        "touched": now,
        "agent_session": agent_session_id(),
        "verified_path": (prev or {}).get("verified_path") if prev and prev.get("dwg") == dwg else None,
    }
    st["sessions"][key] = ses
    if owner == OWNER_AGENT:
        st["current"] = key
    save(st)
    channel_dir(dwg, make=True)
    return ses


def close_session(dwg=None):
    """Leave a model. With no argument, leaves the current one."""
    st = load()
    key = key_of(dwg, st) if dwg else st.get("current")
    if not key or key not in st["sessions"]:
        return None
    ses = st["sessions"].pop(key)
    if st.get("current") == key:
        st["current"] = None
    save(st)
    return ses


def claim(dwg, owner=OWNER_AMIR, task=""):
    """Amir declares a drawing his. The agent will refuse to touch it from here on."""
    return open_session(dwg, task=task, owner=owner, force=True)


def release(dwg):
    return close_session(dwg)


def _guess_project(dwg):
    """projects/<name>/x.dwg -> <name>. Keeps the registry readable without asking."""
    try:
        p = os.path.dirname(os.path.abspath(dwg))
        parts = p.replace("/", os.sep).split(os.sep)
        if "projects" in parts:
            i = parts.index("projects")
            if i + 1 < len(parts):
                return parts[i + 1]
        return os.path.basename(p)
    except Exception:
        return ""


# ---------------------------------------------------------------- lookup
def current(st=None):
    """The session this python process must work in.

    EB_MODEL wins: a parallel job sets it per process, which is the only way several
    models can be worked at once without the processes fighting over one 'current'.
    """
    st = st or load()
    env = os.environ.get("EB_MODEL", "").strip()
    if env:
        return find(env, st) or {
            "dwg": os.path.abspath(env) if os.sep in env else env,
            "name": os.path.basename(env), "slot": slot_of(env),
            "owner": OWNER_AGENT, "project": _guess_project(env), "task": "",
            "opened": time.time(), "touched": time.time(),
            "agent_session": agent_session_id(), "verified_path": None,
            "unregistered": True,
        }
    key = st.get("current")
    return st["sessions"].get(key) if key else None


def get(dwg):
    return find(dwg)


def sessions():
    return load()["sessions"]


def foreign_names():
    """Basenames the agent must never touch (owned by anyone but the agent)."""
    return [s["name"].lower() for s in load()["sessions"].values()
            if s.get("owner") != OWNER_AGENT]


def touch(dwg=None):
    st = load()
    key = key_of(dwg, st) if dwg else st.get("current")
    if key and key in st["sessions"]:
        st["sessions"][key]["touched"] = time.time()
        save(st)


def mark_verified(dwg, full_path):
    """Record that the DRAWING ON DISK behind this session was confirmed by reading the
    document back from AutoCAD -- not merely named the same."""
    st = load()
    key = key_of(dwg, st)
    if key in st["sessions"]:
        st["sessions"][key]["verified_path"] = full_path
        st["sessions"][key]["verified_at"] = time.time()
        save(st)


def staleness(ses):
    """(is_stale, reason). R4: consent expires; it is not inherited."""
    if not ses:
        return (False, "")
    age_h = (time.time() - float(ses.get("touched") or 0)) / 3600.0
    if ses.get("agent_session") != agent_session_id():
        return (True, "opened by a different agent session (%s), age %.1fh"
                % (ses.get("agent_session"), age_h))
    if age_h > STALE_HOURS:
        return (True, "untouched for %.1f hours (limit %.1f)" % (age_h, STALE_HOURS))
    return (False, "")


def confirm(dwg=None):
    """Re-consent to a stale session: stamps it with THIS agent session and now."""
    st = load()
    key = key_of(dwg, st) if dwg else st.get("current")
    if not key or key not in st["sessions"]:
        return None
    st["sessions"][key]["agent_session"] = agent_session_id()
    st["sessions"][key]["touched"] = time.time()
    if st["sessions"][key].get("owner") == OWNER_AGENT:
        st["current"] = key
    save(st)
    return st["sessions"][key]


def switch(dwg):
    """Make an already-open session the current one (single-track work)."""
    st = load()
    key = key_of(dwg, st)
    if key not in st["sessions"]:
        raise RuntimeError("no session for '%s' -- open it first" % os.path.basename(str(dwg)))
    if st["sessions"][key].get("owner") != OWNER_AGENT:
        raise RuntimeError("'%s' is owned by %s -- refusing to switch into it"
                           % (os.path.basename(str(dwg)), st["sessions"][key].get("owner")))
    st["current"] = key
    save(st)
    return st["sessions"][key]


# ---------------------------------------------------------------- report
def _age(ts):
    try:
        m = (time.time() - float(ts)) / 60.0
    except Exception:
        return "?"
    return "%.0fm" % m if m < 90 else "%.1fh" % (m / 60.0)


def status_text():
    st = load()
    cur = current(st)
    out = []
    if not st["sessions"]:
        out.append("no work sessions open. Nothing is pinned; ops that carry no dwg= are unguarded.")
    for key, s in sorted(st["sessions"].items(), key=lambda kv: kv[1].get("touched", 0), reverse=True):
        mark = "->" if cur and cur.get("slot") == key else "  "
        stale, why = staleness(s)
        out.append("%s [%s] %s | project=%s | task=%s | touched %s ago%s"
                   % (mark, s.get("owner"), s.get("name"), s.get("project") or "-",
                      s.get("task") or "-", _age(s.get("touched")),
                      "  ** STALE: " + why if stale else ""))
        out.append("      %s" % s.get("dwg"))
    return "\n".join(out)


def _usage():
    return ("usage: worksession.py status\n"
            "       worksession.py open  <dwg> [--task \"...\"] [--project X] [--force]\n"
            "       worksession.py close [<dwg>]\n"
            "       worksession.py claim <dwg> [--task \"...\"]      # Amir takes it: agent hands off\n"
            "       worksession.py release <dwg>\n"
            "       worksession.py switch <dwg>\n"
            "       worksession.py confirm [<dwg>]\n"
            "       worksession.py slot  <dwg>\n")


def main(argv):
    if not argv:
        print(status_text())
        return 0
    cmd = argv[0].lower()
    rest = [a for a in argv[1:] if not a.startswith("--")]
    flags = [a for a in argv[1:] if a.startswith("--")]

    def flagval(name):
        for i, a in enumerate(argv):
            if a == "--" + name and i + 1 < len(argv):
                return argv[i + 1]
        return None

    try:
        if cmd == "status":
            print(status_text())
        elif cmd == "open":
            s = open_session(rest[0], project=flagval("project"), task=flagval("task"),
                             force="--force" in flags)
            print("opened  %s\n  slot=%s  channel=%s" % (s["dwg"], s["slot"],
                                                         channel_dir(s["dwg"])))
        elif cmd == "close":
            s = close_session(rest[0] if rest else None)
            print("closed  %s" % (s["dwg"] if s else "(nothing open)"))
        elif cmd == "claim":
            s = claim(rest[0], task=flagval("task") or "")
            print("claimed by %s: %s  -- the agent will refuse to touch it" % (s["owner"], s["name"]))
        elif cmd == "release":
            s = release(rest[0])
            print("released %s" % (s["name"] if s else "(nothing)"))
        elif cmd == "switch":
            s = switch(rest[0])
            print("current  %s" % s["name"])
        elif cmd == "confirm":
            s = confirm(rest[0] if rest else None)
            print("confirmed %s" % (s["name"] if s else "(nothing)"))
        elif cmd == "slot":
            print(slot_of(rest[0]))
        else:
            print(_usage())
            return 2
    except Exception as e:
        print("REFUSED: %s" % e)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
