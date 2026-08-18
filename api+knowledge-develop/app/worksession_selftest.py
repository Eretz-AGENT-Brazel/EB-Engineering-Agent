"""worksession_selftest.py - prove the work-session gate REFUSES. No AutoCAD needed.

A guard that has never been seen to fire has never been tested. Same rule as
qc/selftest_consistency.py: every refusal below is triggered on purpose, and a refusal
that does not appear is a failed test -- not a quiet pass.

Run:  python app/worksession_selftest.py
"""

import os
import shutil
import sys
import tempfile
import time

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)

import worksession as ws          # noqa: E402
import eb_api                     # noqa: E402

# ⚠️ 18/08/2026: these used to be paths under a C:\models that does not exist. Then
# `assign` learned to refuse anything that is not an existing .dwg -- and a test running
# against fixtures the real command rejects is not testing the real command. Now they are
# real empty files, still SAME BASENAME IN DIFFERENT FOLDERS, which is the point of A vs B.
FIXTURES = tempfile.mkdtemp(prefix="ws_selftest_")


def _fixture(folder, name):
    d = os.path.join(FIXTURES, folder)
    if not os.path.isdir(d):
        os.makedirs(d)
    p = os.path.join(d, name)
    open(p, "wb").close()
    return p


A = _fixture("alpha", "test.dwg")        # same basename, different folder, on purpose
B = _fixture("beta", "test.dwg")
C = _fixture("gamma", "pump.dwg")

PASS, FAIL = [], []


def check(name, cond, detail=""):
    (PASS if cond else FAIL).append(name)
    print("%s %s%s" % ("  ok  " if cond else "  FAIL", name,
                       ("   <- " + detail) if detail and not cond else ""))


def refusal(op="beam", **kw):
    return eb_api._session_gate(op, kw)


def main():
    # keep the real registry out of harm's way
    backup = None
    if os.path.exists(ws.STATE):
        backup = open(ws.STATE, encoding="utf-8").read()
    pin_backup = None
    if os.path.exists(eb_api._PIN_FILE):
        pin_backup = open(eb_api._PIN_FILE, encoding="utf-8").read()
    os.environ["EB_AGENT_SESSION"] = "selftest-%d" % int(time.time())
    os.environ.pop("EB_MODEL", None)
    try:
        ws.save(ws._blank())

        # --- 0. the slot function is deterministic and separates identical basenames ----
        check("slot is stable", ws.slot_of(A) == ws.slot_of(A))
        check("same basename, different folder -> DIFFERENT mailbox",
              ws.slot_of(A) != ws.slot_of(B))
        check("different name -> different slot", ws.slot_of(A) != ws.slot_of(C))
        check("slot is filesystem-safe",
              all(ch.isalnum() or ch == "_" for ch in ws.slot_of("שוחה 7- DIKE.dwg")))

        # --- 1. no session, nobody else: legacy behaviour, allowed ---------------------
        check("open field: unpinned op allowed when no one holds anything",
              refusal() is None)

        # --- 2. R3 hands-off: Amir holds a model ---------------------------------------
        ws.claim(C, task="עובד על זה עכשיו")
        r = refusal(dwg=os.path.basename(C))
        check("R3 refuses an op aimed at Amir's model", bool(r) and "hands-off" in r, repr(r))

        r = refusal()
        check("R5 refuses an UNPINNED op while Amir holds a model",
              bool(r) and "no work session" in r, repr(r))

        # --- 3. the agent enters its own model ------------------------------------------
        eb_api.use(A, task="lesson 7 rebuild")
        check("agent session is current", (ws.current() or {}).get("dwg") == os.path.abspath(A))
        check("a bare basename resolves to the registered session",
              (ws.find(os.path.basename(A)) or {}).get("dwg") == os.path.abspath(A))
        check("own model is allowed", refusal() is None)
        check("Amir's model is STILL refused from inside our session",
              "hands-off" in (refusal(dwg=os.path.basename(C)) or ""))

        # --- 4. R2 conflict: explicit dwg= may not overrule the session ------------------
        r = refusal(dwg="something-else.dwg")
        check("R2 refuses an explicit dwg= that disagrees with the session",
              bool(r) and "pin conflict" in r, repr(r))
        check("matching dwg= passes", refusal(dwg=os.path.basename(A)) is None)

        # --- 5. R4 consent expires ------------------------------------------------------
        st = ws.load()
        key = ws.slot_of(A)
        st["sessions"][key]["touched"] = time.time() - (ws.STALE_HOURS + 1) * 3600
        ws.save(st)
        r = refusal()
        check("R4 refuses a stale session", bool(r) and "stale session" in r, repr(r))
        ws.confirm(A)
        check("confirm() clears staleness", refusal() is None)

        st = ws.load()
        st["sessions"][key]["agent_session"] = "yesterday"
        ws.save(st)
        r = refusal()
        check("R4 refuses a session opened by another agent run",
              bool(r) and "stale session" in r, repr(r))
        ws.confirm(A)

        # --- 6. diagnostics are never gated ---------------------------------------------
        ws.claim(C)
        check("ping is never gated", refusal(op="ping") is None)
        check("whoami is never gated", refusal(op="whoami") is None)

        # --- 7. the mailbox is per model -------------------------------------------------
        ch_a, ch_c = eb_api.channel(A), eb_api.channel(C)
        check("channels differ per model", ch_a != ch_c)
        check("channel dir is created", os.path.isdir(ch_a))
        check("cmd file lives in the channel",
              os.path.dirname(eb_api._cmd_path()) == ws.channel_dir(A))

        # --- 8. taking over someone else's model requires force --------------------------
        try:
            ws.open_session(C, owner=ws.OWNER_AGENT)
            check("open refuses to take over a held model", False, "no exception raised")
        except RuntimeError:
            check("open refuses to take over a held model", True)

        # --- 8a2. the lock refuses nonsense (both measured on 18/08/2026, first real use) --
        ws.save(ws._blank())
        try:
            ws.assign("Learn and rebuild it 1:1")          # a task text, not a drawing
            check("assign refuses a path that is not a .dwg", False, "no exception")
        except RuntimeError as e:
            check("assign refuses a path that is not a .dwg", "not a drawing" in str(e))
        try:
            ws.assign(os.path.join(FIXTURES, "nope", "ghost.dwg"))
            check("assign refuses a .dwg that is not on disk", False, "no exception")
        except RuntimeError as e:
            check("assign refuses a .dwg that is not on disk", "no such drawing" in str(e))
        ws.save(ws._blank())
        ws.main(["assign", A, "--task", "rebuild it 1:1 beside the original"])
        check("the CLI does not mistake a flag VALUE for a model",
              len(ws.assignment()) == 1, repr(ws.assigned_names()))
        check("...and the task still lands on it",
              (ws.assigned() or {}).get("task") == "rebuild it 1:1 beside the original")

        # --- 8b. THE ASSIGNMENT: Amir locks the agent to one model ------------------------
        ws.release(C)
        ws.save(ws._blank())
        os.environ.pop("EB_MODEL", None)
        ws.assign(A, task="only this one")
        check("assignment is recorded", (ws.assigned() or {}).get("dwg") == os.path.abspath(A))
        check("the assigned model is what the agent will use",
              (ws.current() or {}).get("dwg") == os.path.abspath(A))
        check("an op on the assigned model passes", refusal() is None)
        r = refusal(dwg=os.path.basename(C))
        check("an op on ANY other model is refused", bool(r) and "assigned" in r, repr(r))
        try:
            ws.open_session(C)
            check("the agent cannot ENTER another model", False, "no exception")
        except RuntimeError as e:
            check("the agent cannot ENTER another model", "ASSIGNED" in str(e))
        try:
            ws.switch(C)
            check("the agent cannot SWITCH to another model", False, "no exception")
        except RuntimeError as e:
            check("the agent cannot SWITCH to another model", "ASSIGNED" in str(e))
        os.environ["EB_MODEL"] = C
        check("EB_MODEL cannot bypass the assignment",
              (ws.current() or {}).get("dwg") == os.path.abspath(A))
        os.environ.pop("EB_MODEL", None)
        ws.unassign()
        check("unassign frees the agent", ws.assigned() is None)
        check("...and another model may now be entered",
              ws.open_session(C).get("dwg") == os.path.abspath(C))
        ws.close_session(C)

        # --- 8b2. Amir can always claim, even mid-assignment (measured hole, 18/08) -------
        ws.save(ws._blank())
        ws.assign(A, task="only this one")
        ws.claim(C, task="mine")
        check("Amir can CLAIM another drawing while the agent is assigned",
              (ws.find(os.path.basename(C)) or {}).get("owner") == ws.OWNER_AMIR)
        check("...and the agent still cannot enter it",
              bool(refusal(dwg=os.path.basename(C))))
        ws.release(C)

        # --- 8c. SITUATION 2: a SET of models, each with its own task ---------------------
        ws.save(ws._blank())
        D2 = _fixture("delta", "frame.dwg")
        out = ws.assign([A, C, D2], task=["task A", "task C", "task D"])
        check("a set of three is assigned", len(ws.assignment()) == 3)
        check("each keeps its own task",
              [s.get("task") for s in ws.assignment()] == ["task A", "task C", "task D"])
        check("an op on ANY member of the set passes",
              refusal(dwg=os.path.basename(C)) is None and
              refusal(dwg=os.path.basename(D2)) is None)
        r = refusal(dwg="outside.dwg")
        check("an op OUTSIDE the set is refused", bool(r) and "assigned" in r, repr(r))
        try:
            ws.open_session(r"C:\models\other\outside.dwg")
            check("the agent cannot ENTER a model outside the set", False, "no exception")
        except RuntimeError as e:
            check("the agent cannot ENTER a model outside the set", "ASSIGNED" in str(e))
        os.environ["EB_MODEL"] = C
        check("EB_MODEL selects WITHIN the set",
              (ws.current() or {}).get("dwg") == os.path.abspath(C))
        os.environ["EB_MODEL"] = "outside.dwg"
        check("EB_MODEL cannot select OUTSIDE the set",
              (ws.current() or {}).get("dwg") in
              (os.path.abspath(A), os.path.abspath(C), os.path.abspath(D2)))
        os.environ.pop("EB_MODEL", None)
        ws.switch(D2)
        check("switch works within the set", (ws.current() or {}).get("dwg") == os.path.abspath(D2))
        ws.close_session(D2)
        check("closing an assigned model shrinks the lock to the remaining two",
              len(ws.assignment()) == 2)
        ws.unassign()
        check("unassign clears the whole set", ws.assignment() == [])

        # restore the state stage 9 expects
        ws.save(ws._blank())
        eb_api.use(A, task="lesson 7 rebuild")
        ws.claim(C)

        # --- 9. release gives it back -----------------------------------------------------
        ws.release(C)
        check("after release the model is free again",
              refusal(dwg=os.path.basename(C)) is not None)   # still a pin conflict, not hands-off
        check("...and the refusal is now the conflict, not hands-off",
              "pin conflict" in (refusal(dwg=os.path.basename(C)) or ""))
    finally:
        if backup is not None:
            open(ws.STATE, "w", encoding="utf-8").write(backup)
        elif os.path.exists(ws.STATE):
            os.remove(ws.STATE)
        # ⚠️ 17/08/2026: restoring only the JSON was not enough. `use()` also writes the
        # legacy one-line pin, so a test run left `test.dwg` pinned and the NEXT REAL
        # COMMAND refused with "attached to the wrong AutoCAD" -- I hit it myself minutes
        # after adding this file. A test that leaves state behind is a trap for its author.
        try:
            if pin_backup is not None:
                open(eb_api._PIN_FILE, "w", encoding="utf-8").write(pin_backup)
            elif os.path.exists(eb_api._PIN_FILE):
                os.remove(eb_api._PIN_FILE)
        except Exception:
            pass
        shutil.rmtree(FIXTURES, ignore_errors=True)

    print("\n%d passed, %d failed" % (len(PASS), len(FAIL)))
    if FAIL:
        print("FAILED: " + ", ".join(FAIL))
        return 1
    print("SELFTEST CLEAN - every refusal fired on purpose")
    return 0


if __name__ == "__main__":
    sys.exit(main())
