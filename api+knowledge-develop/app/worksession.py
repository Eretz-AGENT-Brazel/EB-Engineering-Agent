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
    return {"version": 1, "current": None, "assigned": None, "sessions": {}}


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
# ---------------------------------------------------------------- assignment (Amir's lock)
# ⭐ 17/08/2026, Amir: "איך אני מוודא שאני משייך לסוכן את המודל הספציפי שאני רוצה שהוא יעבוד
# עליו ולא יגלוש למודל אחר?"
# Ownership answers "do not touch MINE". Assignment answers the other half: "work on THIS
# one and nothing else". While an assignment stands, the agent cannot enter another model
# at all -- not by use(), not by open_model(), not through the EB_MODEL environment
# variable. Only Amir lifts it (`worksession.py unassign`). The distinction matters: the
# agent may legitimately want a second model open (it asked for that capability); the
# assignment is the way to say "not today".
def assigned_keys(st=None):
    """Slots the agent is locked to. Empty list = free."""
    st = st if st is not None else load()
    a = st.get("assigned")
    if not a:
        return []
    keys = [a] if isinstance(a, str) else list(a)          # a single slot is still valid state
    # ⛔ DO NOT FILTER BY "has a live session". This line used to end
    #     return [k for k in keys if k in st["sessions"]]
    # and it is what really made Amir's assignment shrink: `close_session` pops the session,
    # so a model he had explicitly assigned silently fell out of his own lock the moment its
    # drawing was closed -- and re-entering it was then refused with "ASSIGNED to <the
    # others>". It cost three refusals on 19/08/2026, each one after AutoCAD had died and been
    # relaunched, i.e. exactly when the agent most needs to get back into its own model.
    # An assignment is a DECLARATION about which models may be worked on; whether a drawing
    # happens to be open right now is an operational detail, and `use()` reopens it.
    return keys


def assignment(st=None):
    """The sessions the agent is LOCKED to by Amir (a list, possibly empty)."""
    st = st if st is not None else load()
    return [st["sessions"][k] for k in assigned_keys(st) if k in st.get("sessions", {})]


def assigned(st=None):
    """The FIRST assigned session, or None. Kept for callers that want one."""
    a = assignment(st)
    return a[0] if a else None


def assigned_names(st=None):
    return [s.get("name", "?") for s in assignment(st)]


def assign(dwgs, task=None, project=None):
    """Lock the agent to one model — or to a SET of them, each with its own task.

    ⭐ 17/08/2026: one model is Amir's day-to-day case ("work on this and nothing else").
    A SET is the case he described for the office: three projects in parallel, a different
    instruction for each. Both are the same lock; only the size of the list changes.
    `task` may be one string for all, or a list parallel to `dwgs`.
    """
    if isinstance(dwgs, str):
        dwgs = [dwgs]
    if not dwgs:
        raise RuntimeError("assign takes at least one .dwg")
    for d in dwgs:                                   # the lock refuses nonsense, it does not
        d = str(d)                                   # register it (measured hole, 18/08/2026)
        if not d.lower().endswith(".dwg"):
            raise RuntimeError("not a drawing: %r -- assign takes .dwg paths" % d)
        if (os.sep in d or "/" in d) and not os.path.isfile(d):
            raise RuntimeError("no such drawing on disk: %s" % d)
    tasks = task if isinstance(task, (list, tuple)) else [task] * len(dwgs)
    out = []
    for i, d in enumerate(dwgs):
        out.append(open_session(d, project=project,
                                task=tasks[i] if i < len(tasks) else None,
                                owner=OWNER_AGENT, force=True, _bypass_assignment=True))
    st = load()
    st["assigned"] = [s["slot"] for s in out]
    st["current"] = out[0]["slot"]
    st["assigned_at"] = time.time()
    save(st)
    return out


def unassign():
    st = load()
    prev = assignment(st)
    st["assigned"] = None
    st.pop("assigned_at", None)
    save(st)
    return prev


def open_session(dwg, project=None, task=None, owner=OWNER_AGENT, force=False,
                 _bypass_assignment=False):
    """Enter a model. Returns the session dict.

    Refuses to enter a drawing that someone else owns unless force=True -- entering a
    model Amir declared his own is exactly the accident this file exists to stop.
    Refuses ANY other model while an assignment stands.
    """
    dwg = os.path.abspath(dwg) if os.sep in str(dwg) or "/" in str(dwg) else str(dwg)
    st = load()
    keys = assigned_keys(st)
    # ⭐ 18/08/2026: the assignment binds THE AGENT, so it may only ever refuse the agent.
    # Measured the hard way: Amir opened his own client drawing, I went to protect it with
    # `claim`, and claim was refused because I was assigned elsewhere -- his own hands-off
    # command blocked by my lock. Anyone who is not the agent passes this gate.
    if owner != OWNER_AGENT:
        _bypass_assignment = True
    if not _bypass_assignment and keys and key_of(dwg, st) not in keys:
        raise RuntimeError(
            "ASSIGNED to %s -- refusing to enter '%s'. Amir locked the agent to %s; lift it "
            "with `python app/worksession.py unassign`."
            % (", ".join("'%s'" % n for n in assigned_names(st)),
               os.path.basename(str(dwg)),
               "that one model" if len(keys) == 1 else "those %d models" % len(keys)))
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
    # ⛔ CLOSING A DOCUMENT MUST NOT SHRINK AMIR'S ASSIGNMENT. This used to drop the closed
    # model out of the assigned set, and it cost real work on 19/08/2026: after AutoCAD died
    # and was relaunched, re-entering model 5's SOURCE -- a model Amir had explicitly assigned
    # minutes earlier -- was refused with "ASSIGNED to <the other three>", twice. An assignment
    # is Amir's DECLARATION of what the agent may work on; closing a drawing is an operational
    # act by the agent. Only `unassign` (his) may change the set.
    # The old behaviour existed to avoid "pointing at nothing" -- but an assignment naming a
    # model that happens to be closed is not pointing at nothing: the next `use()` reopens it.
    save(st)
    return ses


def claim(dwg, owner=OWNER_AMIR, task=""):
    """Amir declares a drawing his. The agent will refuse to touch it from here on.

    ⭐ Never blocked by an assignment: `assign` is a leash on the agent, and a leash on the
    agent cannot bind the man holding it (measured 18/08/2026 -- it did, once).
    """
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
    keys = assigned_keys(st)
    if keys:
        # an assignment outranks everything. Within a multi-model assignment, EB_MODEL and
        # `current` choose WHICH of the assigned models this process works -- that is the
        # sanctioned mechanism for working the set -- but nothing outside the set.
        env = os.environ.get("EB_MODEL", "").strip()
        if env:
            k = key_of(env, st)
            if k in keys and k in st["sessions"]:
                return st["sessions"][k]
        cur = st.get("current")
        if cur in keys and cur in st["sessions"]:
            return st["sessions"][cur]
        # ⚠️ A DECLARED SLOT MAY HAVE NO SESSION YET. Since assigned_keys() stopped filtering
        # on "has a live session" (see there -- the filter is what made Amir's lock shrink),
        # every consumer must tolerate a slot whose drawing is simply not open at the moment.
        for k in keys:
            if k in st["sessions"]:
                return st["sessions"][k]
        return None
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
    keys = assigned_keys(st)
    if keys and key not in keys:
        raise RuntimeError(
            "ASSIGNED to %s -- refusing to switch to '%s'. `worksession.py unassign` lifts it."
            % (", ".join("'%s'" % n for n in assigned_names(st)), os.path.basename(str(dwg))))
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


def verify_text():
    """Ask AUTOCAD what it is holding, and compare it to what the registry claims.

    ⭐ This is the answer to "how do I make sure the agent is on MY model and will not
    drift". A registry is a promise; this reads the running software. Never trust the row,
    read the drawing back -- the same rule the whole project runs on.
    """
    st = load()
    locks, cur = assignment(st), current(st)
    lines = []
    if not locks:
        lines.append("ASSIGNED : — (the agent may enter any free model)")
    elif len(locks) == 1:
        lines.append("ASSIGNED : 🔒 %s   (%s)" % (locks[0]["name"], locks[0]["dwg"]))
    else:
        lines.append("ASSIGNED : 🔒 %d models:" % len(locks))
        for l in locks:
            lines.append("           %-40s  task: %s" % (l["name"], l.get("task") or "-"))
    lines.append("WILL USE : %s" % (cur["name"] if cur else "— nothing; ops refuse or run unguarded"))
    try:
        sys.path.insert(0, HERE)
        import eb_api
        r = eb_api.run("docs", wait=12.0)
    except Exception as e:
        r = "EB_ERR " + str(e)
    if not r.startswith("EB_OK"):
        reach = {}
        try:
            reach = eb_api.autocad_reachability()
        except Exception:
            pass
        lines.append("AUTOCAD  : could not be read -> %s" % r[:160])
        if reach:
            lines.append("           reachability: %s" % reach)
            if reach.get("hint"):
                lines.append("           " + reach["hint"])
        return "\n".join(lines)
    active = ""
    if "active=" in r:
        active = r.split("active=", 1)[1].split(" slot=", 1)[0].strip()
    lines.append("ACTIVE IN AUTOCAD: %s" % (active or "?"))
    want = (cur or {}).get("dwg") or ""
    same = bool(active) and bool(want) and \
        os.path.normcase(os.path.abspath(active)) == os.path.normcase(os.path.abspath(want))
    if cur:
        lines.append("MATCH    : %s" % ("✅ the agent's model IS the one in front"
                                        if same else
                                        "⚠️  different — the agent will bring its own model "
                                        "forward before the next op, or refuse if the one in "
                                        "front is not its own"))
    lines.append("")
    lines.append("open drawings in this AutoCAD:")
    body = r.split(" reqid=")[0]          # the answer's own tag is not part of a file name
    for part in body.split(" | ")[1:]:
        p = part.strip()
        star = p.startswith("*")
        p = p.lstrip("*").strip()
        rec = find(p, st)
        who = ("held by " + rec["owner"]) if rec else "not registered"
        if rec and rec.get("slot") in assigned_keys(st):
            who += ", ASSIGNED"
        lines.append("  %s %-46s  %s" % ("▶" if star else " ", os.path.basename(p), who))
    return "\n".join(lines)


def status_text():
    st = load()
    cur = current(st)
    lock = assigned(st)
    out = []
    if lock:
        out.append("🔒 ASSIGNED to %s — the agent cannot enter any other model until "
                   "`worksession.py unassign`." % lock["name"])
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
    return ("usage: worksession.py status                      # who holds what right now\n"
            "       worksession.py verify                      # ASK AUTOCAD and compare\n"
            "\n"
            "   ── Amir's two commands ──\n"
            "       worksession.py assign  <dwg> [<dwg2> ...] [--task \"...\"]\n"
            "                                                   # lock the agent to THESE models\n"
            "       worksession.py unassign                     # lift the lock\n"
            "       worksession.py claim   <dwg> [--task \"...\"]  # this one is MINE: hands off\n"
            "       worksession.py release <dwg>                # ...and give it back\n"
            "\n"
            "   ── the agent's ──\n"
            "       worksession.py open  <dwg> [--task \"...\"] [--project X] [--force]\n"
            "       worksession.py close [<dwg>]\n"
            "       worksession.py switch <dwg>\n"
            "       worksession.py confirm [<dwg>]\n"
            "       worksession.py slot  <dwg>\n")


def main(argv):
    if not argv:
        print(status_text())
        return 0
    cmd = argv[0].lower()
    # A FLAG VALUE IS NOT A MODEL. Measured 18/08/2026, the first real `assign`:
    # `assign <dwg> --task "rebuild it 1:1"` left the task text in the positional list
    # and locked the agent to TWO models -- the second one named after the task.
    VALUE_FLAGS = ("--task", "--project")
    rest, flags, skip = [], [], False
    for a in argv[1:]:
        if skip:
            skip = False
            continue
        if a.startswith("--"):
            flags.append(a)
            skip = a in VALUE_FLAGS          # `--task=X` carries its own value: nothing to skip
        else:
            rest.append(a)

    def flagval(name):
        for i, a in enumerate(argv):
            if a == "--" + name and i + 1 < len(argv):
                return argv[i + 1]
            if a.startswith("--" + name + "="):
                return a.split("=", 1)[1]
        return None

    try:
        if cmd == "status":
            print(status_text())
        elif cmd == "verify":
            print(verify_text())
        elif cmd == "assign":
            out = assign(rest, task=flagval("task"), project=flagval("project"))
            if len(out) == 1:
                print("🔒 ASSIGNED: the agent works on %s and nothing else.\n   %s\n"
                      "   lift with: python app/worksession.py unassign"
                      % (out[0]["name"], out[0]["dwg"]))
            else:
                print("🔒 ASSIGNED to %d models and nothing else:" % len(out))
                for s in out:
                    print("   %-40s  task: %s" % (s["name"], s.get("task") or "-"))
                print("   lift with: python app/worksession.py unassign")
        elif cmd == "unassign":
            prev = unassign()
            print("lock lifted (was %s)" %
                  (", ".join(s["name"] for s in prev) if prev else "not set"))
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
